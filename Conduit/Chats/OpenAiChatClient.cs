// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Genova.Common.Attributes;

namespace Genova.Conduit.Chats;

/// <summary>
/// Simple implementation of <see cref="IChatClient"/> that calls the
/// OpenAI Chat Completions HTTP API.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class OpenAiChatClient : IChatClient, IDisposable
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Conflicting naming rules")]
    private static readonly JsonSerializerOptions CamelCaseOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly HttpClient _httpClient;
    private readonly string _modelId;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiChatClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">
    /// The HTTP client factory used to create <see cref="HttpClient"/> instances.
    /// </param>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <param name="modelId">The model identifier (e.g. "gpt-4o-mini").</param>
    public OpenAiChatClient(
        IHttpClientFactory httpClientFactory, string apiKey, string modelId = "gpt-4o-mini")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key must be provided.", nameof(apiKey));
        }

        _modelId = modelId ?? throw new ArgumentNullException(nameof(modelId));

        HttpClient client = httpClientFactory.CreateClient("Genova.Conduit.OpenAI.Embeddings");

        if (client.BaseAddress == null)
        {
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
        }

        if (client.DefaultRequestHeaders.Authorization == null)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }

        _httpClient = client;
    }

    /// <inheritdoc/>
    public async Task<ChatResponse> GenerateAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string modelId = request.ModelId ?? _modelId;

        ChatCompletionRequestDto payload = new ()
        {
            Model = modelId,
            Messages = MapMessages(request.Messages),
            MaxTokens = request.MaxTokens,
            Temperature = request.Temperature,
        };

        // Use the cached options instance
        string json = JsonSerializer.Serialize(payload, CamelCaseOptions);

        using StringContent content = new (json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);

        // Read response body now (before EnsureSuccessStatusCode) so we can log it even when non-success
        string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        response.EnsureSuccessStatusCode();

        ChatCompletionResponseDto? completion =
            JsonSerializer.Deserialize<ChatCompletionResponseDto>(
                responseJson,
                CamelCaseOptions);

        ChatCompletionChoiceDto? firstChoice = completion?.Choices != null && completion.Choices.Count > 0
            ? completion.Choices[0]
            : null;

        ChatMessage? message = firstChoice?.Message != null
            ? new ChatMessage
            {
                Role = MapRoleBack(firstChoice.Message.Role),
                Content = firstChoice.Message.Content ?? string.Empty,
            }
            : null;

        return new ChatResponse
        {
            Message = message,
            ProviderMetadata = completion,
        };
    }

    /// <summary>
    /// Disposes the HTTP client.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _httpClient.Dispose();
    }

    private static List<ChatCompletionMessageDto> MapMessages(IList<ChatMessage> messages)
    {
        List<ChatCompletionMessageDto> list = new(messages.Count);

        foreach (ChatMessage message in messages)
        {
            list.Add(new ChatCompletionMessageDto
            {
                Role = MapRole(message.Role),
                Content = message.Content,
            });
        }

        return list;
    }

    private static string MapRole(ChatMessageRole role) =>
        role switch
        {
            ChatMessageRole.System => "system",
            ChatMessageRole.User => "user",
            ChatMessageRole.Assistant => "assistant",
            ChatMessageRole.Tool => "tool",
            _ => "user",
        };

    private static ChatMessageRole MapRoleBack(string? role) =>
        role switch
        {
            "system" => ChatMessageRole.System,
            "assistant" => ChatMessageRole.Assistant,
            "tool" => ChatMessageRole.Tool,
            "user" => ChatMessageRole.User,
            _ => ChatMessageRole.Assistant,
        };

    private sealed class ChatCompletionRequestDto
    {
        public string Model { get; set; } = string.Empty;

        public List<ChatCompletionMessageDto> Messages { get; set; } = [];

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        public double? Temperature { get; set; }
    }

    private sealed class ChatCompletionMessageDto
    {
        public string Role { get; set; } = "user";

        public string? Content { get; set; }
    }

    private sealed class ChatCompletionResponseDto
    {
        public List<ChatCompletionChoiceDto> Choices { get; set; } = [];
    }

    private sealed class ChatCompletionChoiceDto
    {
        public ChatCompletionMessageDto Message { get; set; } = new();
    }
}
