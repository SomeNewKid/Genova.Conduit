// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Genova.Common.Attributes;
using Genova.Conduit.Chats;
using Genova.Conduit.Pipelines;

namespace Genova.Conduit.Terminal.Steps;

/// <summary>
/// Represents a pipeline step that uses an LLM to interpret a user's
/// algebraic question and select a math operation (addition, subtraction,
/// multiplication, division) along with two integer operands.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class MathOperationSelectionStep : IPipelineStep
{
    private readonly IChatClient _chatClient;
    private readonly string _questionKey;
    private readonly string _operationKey;
    private readonly string _operandAKey;
    private readonly string _operandBKey;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MathOperationSelectionStep"/> class.
    /// </summary>
    /// <param name="chatClient">
    /// The chat client used to call the LLM (typically backed by the OpenAI Responses API).
    /// </param>
    /// <param name="questionKey">
    /// The key in <see cref="PipelineContext.Items"/> under which the user's question
    /// is stored as a <see cref="string"/>.
    /// </param>
    /// <param name="operationKey">
    /// The key under which the selected operation name will be stored.
    /// Expected values are "addition", "subtraction", "multiplication",
    /// "division", or "no_match".
    /// </param>
    /// <param name="operandAKey">
    /// The key under which the first integer operand will be stored.
    /// </param>
    /// <param name="operandBKey">
    /// The key under which the second integer operand will be stored.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chatClient"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any key parameter is <c>null</c> or whitespace.
    /// </exception>
    public MathOperationSelectionStep(
        IChatClient chatClient,
        string questionKey,
        string operationKey,
        string operandAKey,
        string operandBKey)
    {
        ArgumentNullException.ThrowIfNull(chatClient);

        if (string.IsNullOrWhiteSpace(questionKey))
        {
            throw new ArgumentException("Question key must be non-empty.", nameof(questionKey));
        }

        if (string.IsNullOrWhiteSpace(operationKey))
        {
            throw new ArgumentException("Operation key must be non-empty.", nameof(operationKey));
        }

        if (string.IsNullOrWhiteSpace(operandAKey))
        {
            throw new ArgumentException("Operand A key must be non-empty.", nameof(operandAKey));
        }

        if (string.IsNullOrWhiteSpace(operandBKey))
        {
            throw new ArgumentException("Operand B key must be non-empty.", nameof(operandBKey));
        }

        _chatClient = chatClient;
        _questionKey = questionKey;
        _operationKey = operationKey;
        _operandAKey = operandAKey;
        _operandBKey = operandBKey;

        JsonSerializerOptions options = new ()
        {
            PropertyNameCaseInsensitive = true
        };

        _jsonOptions = options;
    }

    /// <summary>
    /// Executes the step by reading the user question from the context,
    /// calling the LLM to classify the operation and extract two integer
    /// operands, and storing the results back into the context.
    /// </summary>
    /// <param name="context">The shared pipeline context.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    public async Task ExecuteAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? question = context.GetItem<string>(_questionKey);
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new InvalidOperationException(
                $"Pipeline context does not contain a user question under key '{_questionKey}'.");
        }

        OperationSelectionResponse selection =
            await CallModelToSelectOperationAsync(question, cancellationToken)
                .ConfigureAwait(false);

        string operation = NormalizeOperation(selection.Operation);

        if (operation == "no_match" || selection.A == null || selection.B == null)
        {
            context.SetItem(_operationKey, "no_match");
            context.RemoveItem(_operandAKey);
            context.RemoveItem(_operandBKey);
            return;
        }

        context.SetItem(_operationKey, operation);
        context.SetItem(_operandAKey, selection.A.Value);
        context.SetItem(_operandBKey, selection.B.Value);
    }

    private async Task<OperationSelectionResponse> CallModelToSelectOperationAsync(
        string question,
        CancellationToken cancellationToken)
    {
        ChatRequest request = new ()
        {
            ModelId = null,
            MaxTokens = 256,
            Temperature = 0.0
        };

        request.Messages.Add(
            new ChatMessage
            {
                Role = ChatMessageRole.System,
                Content =
                    "You are a strict math operation classifier. " +
                    "Given a user's question, you must decide whether it describes " +
                    "an integer addition, subtraction, multiplication, or division " +
                    "between exactly two integers. " +
                    "If it does, extract the operation and the two integers. " +
                    "If it does not, return a 'no_match' result."
            });

        request.Messages.Add(
            new ChatMessage
            {
                Role = ChatMessageRole.System,
                Content =
                    "Respond ONLY with valid JSON in the following schema:\n" +
                    "{ \"operation\": \"addition\" | \"subtraction\" | \"multiplication\" | \"division\" | \"no_match\", \"a\": integer or null, \"b\": integer or null }"
            });

        request.Messages.Add(
            new ChatMessage
            {
                Role = ChatMessageRole.User,
                Content = question
            });

        ChatResponse response =
            await _chatClient.GenerateAsync(request, cancellationToken)
                .ConfigureAwait(false);

        string? content = response.Message?.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            return new OperationSelectionResponse
            {
                Operation = "no_match"
            };
        }

        try
        {
            OperationSelectionResponse? parsed =
                JsonSerializer.Deserialize<OperationSelectionResponse>(
                    content,
                    _jsonOptions);

            if (parsed == null)
            {
                return new OperationSelectionResponse
                {
                    Operation = "no_match"
                };
            }

            return parsed;
        }
        catch (JsonException)
        {
            return new OperationSelectionResponse
            {
                Operation = "no_match"
            };
        }
    }

    private static string NormalizeOperation(string? operation)
    {
        if (string.IsNullOrWhiteSpace(operation))
        {
            return "no_match";
        }

        string trimmed = operation.Trim().ToLowerInvariant();

        if (trimmed == "addition" ||
            trimmed == "subtraction" ||
            trimmed == "multiplication" ||
            trimmed == "division")
        {
            return trimmed;
        }

        return "no_match";
    }

    /// <summary>
    /// Represents the structured response from the LLM when selecting a math operation.
    /// </summary>
    private sealed class OperationSelectionResponse
    {
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = "no_match";

        [JsonPropertyName("a")]
        public int? A { get; set; }

        [JsonPropertyName("b")]
        public int? B { get; set; }
    }
}
