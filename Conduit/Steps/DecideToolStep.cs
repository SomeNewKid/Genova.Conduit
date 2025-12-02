// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Genova.Common.Attributes;
using Genova.Conduit.Chats;
using Genova.Conduit.Pipelines;
using Genova.Conduit.Tools;

namespace Genova.Conduit.Steps;

/// <summary>
/// Represents a pipeline step that asks a chat model to decide which tool
/// to invoke based on the user's request and the available tools,
/// and stores the selected tool name and arguments in the <see cref="PipelineContext"/>.
/// </summary>
/// <remarks>
/// This step is model-driven: the model is given a description of the available tools
/// and the user's query, and is asked to return a JSON object describing the tool
/// to call and the arguments to use. The parsed result is stored in the context
/// under the keys specified in the constructor.
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class DecideToolStep : IPipelineStep
{
    private readonly IChatClient _chatClient;
    private readonly IToolRegistry _toolRegistry;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _userQueryKey;
    private readonly string _selectedToolNameKey;
    private readonly string _selectedToolArgumentsKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="DecideToolStep"/> class.
    /// </summary>
    /// <param name="chatClient">
    /// The chat client used to ask the model which tool to invoke.
    /// This is typically an <see cref="OpenAiResponseClient"/> instance.
    /// </param>
    /// <param name="toolRegistry">
    /// The registry used to enumerate available tools and later resolve them.
    /// </param>
    /// <param name="userQueryKey">
    /// The key in <see cref="PipelineContext.Items"/> containing the user's
    /// natural language query as a <see cref="string"/>.
    /// </param>
    /// <param name="selectedToolNameKey">
    /// The key under which the selected tool name will be stored in the context.
    /// </param>
    /// <param name="selectedToolArgumentsKey">
    /// The key under which the selected tool arguments will be stored in the context
    /// as an <see cref="IDictionary{String, Object}"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chatClient"/> or <paramref name="toolRegistry"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any string parameter is <c>null</c> or whitespace.
    /// </exception>
    public DecideToolStep(
        IChatClient chatClient,
        IToolRegistry toolRegistry,
        string userQueryKey,
        string selectedToolNameKey,
        string selectedToolArgumentsKey)
    {
        ArgumentNullException.ThrowIfNull(chatClient);

        ArgumentNullException.ThrowIfNull(toolRegistry);

        if (string.IsNullOrWhiteSpace(userQueryKey))
        {
            throw new ArgumentException("User query key must be non-empty.", nameof(userQueryKey));
        }

        if (string.IsNullOrWhiteSpace(selectedToolNameKey))
        {
            throw new ArgumentException("Selected tool name key must be non-empty.", nameof(selectedToolNameKey));
        }

        if (string.IsNullOrWhiteSpace(selectedToolArgumentsKey))
        {
            throw new ArgumentException("Selected tool arguments key must be non-empty.", nameof(selectedToolArgumentsKey));
        }

        _chatClient = chatClient;
        _toolRegistry = toolRegistry;
        _userQueryKey = userQueryKey;
        _selectedToolNameKey = selectedToolNameKey;
        _selectedToolArgumentsKey = selectedToolArgumentsKey;

        JsonSerializerOptions options = new ()
        {
            PropertyNameCaseInsensitive = true,
        };

        _jsonOptions = options;
    }

    /// <summary>
    /// Executes the step by asking the model which tool to call and with what arguments,
    /// and storing the decision in the pipeline context.
    /// </summary>
    /// <param name="context">The shared pipeline context.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? userQuery = context.GetItem<string>(_userQueryKey);
        if (string.IsNullOrWhiteSpace(userQuery))
        {
            // Nothing to decide; user query is missing.
            return;
        }

        string toolsDescription = BuildToolsDescription(_toolRegistry.GetAllTools());

        ChatRequest request = new ()
        {
            ModelId = null,
            MaxTokens = 256,
            Temperature = 0.0,
        };

        request.Messages.Add(
            new ChatMessage
            {
                Role = ChatMessageRole.System,
                Content =
                    """
                    You are a tool selector. You decide which tool (if any) should be invoked to help answer the user's question.

                    Available tools:
                    """
                    + toolsDescription +
                    """
                    
                    Respond ONLY with valid JSON using this schema:
                    { "toolName": string | null, "arguments": { string: string } }
                    If no tool is appropriate, use "toolName": null and an empty arguments object.
                    """,
            });

        request.Messages.Add(
            new ChatMessage
            {
                Role = ChatMessageRole.User,
                Content = "User question: " + userQuery,
            });

        ChatResponse response = await _chatClient.GenerateAsync(request, cancellationToken)
            .ConfigureAwait(false);

        string? jsonText = response.Message?.Content;

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            // Model did not return anything usable.
            return;
        }

        ToolSelection? selection = DeserializeToolSelection(jsonText);
        if (selection == null || string.IsNullOrWhiteSpace(selection.ToolName))
        {
            // No tool selected or parsing failed.
            return;
        }

        // Convert string arguments into an IDictionary<string, object?> for tools.
        Dictionary<string, object?> toolArguments = new (StringComparer.OrdinalIgnoreCase);

        if (selection.Arguments != null)
        {
            foreach (KeyValuePair<string, string?> pair in selection.Arguments)
            {
                toolArguments[pair.Key] = pair.Value;
            }
        }

        context.SetItem(_selectedToolNameKey, selection.ToolName);
        context.SetItem(_selectedToolArgumentsKey, toolArguments);
    }

    private static string BuildToolsDescription(IEnumerable<ITool> tools)
    {
        StringBuilder builder = new ();

        foreach (ITool tool in tools)
        {
            if (tool == null)
            {
                continue;
            }

            builder.Append("- ");
            builder.Append(tool.Name);
            builder.Append(": ");
            builder.Append(tool.Description);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private ToolSelection? DeserializeToolSelection(string jsonText)
    {
        try
        {
            ToolSelection? selection =
                JsonSerializer.Deserialize<ToolSelection>(jsonText, _jsonOptions);

            return selection;
        }
        catch (JsonException)
        {
            // If parsing fails, we simply treat it as "no decision".
            return null;
        }
    }

    /// <summary>
    /// Represents the model's decision about which tool to call and with what arguments.
    /// </summary>
    private sealed class ToolSelection
    {
        /// <summary>
        /// Gets or sets the name of the tool to invoke, or <c>null</c> if no tool is needed.
        /// </summary>
        [JsonPropertyName("toolName")]
        public string? ToolName { get; set; }

        /// <summary>
        /// Gets or sets the arguments to pass to the selected tool as string key/value pairs.
        /// </summary>
        [JsonPropertyName("arguments")]
        public Dictionary<string, string?> Arguments { get; set; } = new (StringComparer.OrdinalIgnoreCase);
    }
}
