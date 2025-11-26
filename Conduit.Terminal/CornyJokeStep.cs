// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Conduit.Models;

namespace Genova.Conduit.Terminal;

/// <summary>
/// A pipeline step that asks OpenAI for a corny joke about a given topic
/// and stores the result in the <see cref="PipelineContext"/>.
/// </summary>
public sealed class CornyJokeStep : IPipelineStep
{
    private readonly IChatModelClient _chatModelClient;
    private readonly string _topic;

    /// <summary>
    /// Initializes a new instance of the <see cref="CornyJokeStep"/> class.
    /// </summary>
    /// <param name="chatModelClient">
    /// The chat model client to use for generating the joke.
    /// </param>
    /// <param name="topic">
    /// The topic to generate a joke about.
    /// </param>
    public CornyJokeStep(IChatModelClient chatModelClient, string topic)
    {
        _chatModelClient = chatModelClient ?? throw new ArgumentNullException(nameof(chatModelClient));
        _topic = !string.IsNullOrWhiteSpace(topic)
            ? topic
            : throw new ArgumentException("Topic must be non-empty.", nameof(topic));
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        // Build a simple chat request
        ChatRequest request = new ()
        {
            ModelId = null, // optional; client may have a default
            Messages =
                {
                    new ChatMessage
                    {
                        Role = ChatMessageRole.System,
                        Content = "You are a friendly assistant that tells very corny, family-friendly jokes."
                    },
                    new ChatMessage
                    {
                        Role = ChatMessageRole.User,
                        Content = $"Please tell me one short, corny joke about: {_topic}"
                    }
                },
            MaxTokens = 128,
            Temperature = 0.8
        };

        ChatCompletionResult result = await _chatModelClient.GenerateAsync(request, cancellationToken);

        string joke = result?.Message?.Content ?? string.Empty;

        // Store the joke in the context so the host can read it
        context.SetItem("CornyJoke", joke);
    }
}
