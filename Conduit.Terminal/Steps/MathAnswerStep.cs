// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Chats;
using Genova.Conduit.Pipelines;

namespace Genova.Conduit.Terminal.Steps;

/// <summary>
/// Represents a pipeline step that uses an LLM to generate a
/// user-friendly final answer from the original question and
/// the computed numeric result.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class MathAnswerStep : IPipelineStep
{
    private readonly IChatClient _chatClient;
    private readonly string _questionKey;
    private readonly string _resultKey;
    private readonly string _finalAnswerKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="MathAnswerStep"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used to call the LLM.</param>
    /// <param name="questionKey">
    /// The key in <see cref="PipelineContext.Items"/> under which the user
    /// question is stored as a <see cref="string"/>.
    /// </param>
    /// <param name="resultKey">
    /// The key in <see cref="PipelineContext.Items"/> under which the numeric
    /// result is stored as an <see cref="int"/>.
    /// </param>
    /// <param name="finalAnswerKey">
    /// The key under which the generated final answer will be stored.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chatClient"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any key parameter is <c>null</c> or whitespace.
    /// </exception>
    public MathAnswerStep(
        IChatClient chatClient,
        string questionKey,
        string resultKey,
        string finalAnswerKey)
    {
        ArgumentNullException.ThrowIfNull(chatClient);

        if (string.IsNullOrWhiteSpace(questionKey))
        {
            throw new ArgumentException("Question key must be non-empty.", nameof(questionKey));
        }

        if (string.IsNullOrWhiteSpace(resultKey))
        {
            throw new ArgumentException("Result key must be non-empty.", nameof(resultKey));
        }

        if (string.IsNullOrWhiteSpace(finalAnswerKey))
        {
            throw new ArgumentException("Final answer key must be non-empty.", nameof(finalAnswerKey));
        }

        _chatClient = chatClient;
        _questionKey = questionKey;
        _resultKey = resultKey;
        _finalAnswerKey = finalAnswerKey;
    }

    /// <summary>
    /// Executes the step by generating a user-friendly final answer using
    /// the LLM, based on the question and numeric result from the context.
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

        object? rawResult = context.GetItem<object>(_resultKey);
        if (rawResult == null || rawResult is not int resultValue)
        {
            throw new InvalidOperationException(
                $"Pipeline context does not contain an integer result under key '{_resultKey}'.");
        }

        ChatRequest request = new ()
        {
            ModelId = null,
            MaxTokens = 128,
            Temperature = 0.2
        };

        request.Messages.Add(
            new ChatMessage
            {
                Role = ChatMessageRole.System,
                Content =
                    "You are a helpful assistant. " +
                    "Given the user's original math question and the computed numeric result, " +
                    "produce a short, clear, user-friendly answer. " +
                    "The answer should be self-sufficient and not rely on external context."
            });

        request.Messages.Add(
            new ChatMessage
            {
                Role = ChatMessageRole.User,
                Content =
                    "Question: " + question + "\n" +
                    "Numeric result: " + resultValue.ToString()
            });

        ChatResponse response =
            await _chatClient.GenerateAsync(request, cancellationToken)
                .ConfigureAwait(false);

        string? finalAnswer = response.Message?.Content;

        if (string.IsNullOrWhiteSpace(finalAnswer))
        {
            finalAnswer = $"The answer is {resultValue}.";
        }

        context.SetItem(_finalAnswerKey, finalAnswer);
    }
}
