// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Genova.Common.Attributes;

namespace Genova.Conduit.Chats;

/// <summary>
/// Implementation of <see cref="IChatClient"/> that uses
/// OpenAI's /v1/responses endpoint instead of /v1/chat/completions.
/// </summary>
/// <remarks>
/// This client maps <see cref="ChatRequest"/> to the Responses API format
/// and returns a <see cref="ChatResponse"/> with an aggregated text output.
/// The raw response object is exposed via <see cref="ChatResponse.ProviderMetadata"/>.
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class OpenAiResponseClient : IChatClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _defaultModel;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiResponseClient"/> class.
    /// </summary>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <param name="defaultModel">
    /// The default model to use when <see cref="ChatRequest.ModelId"/> is null.
    /// For example, <c>\"gpt-4o-mini\"</c>.
    /// </param>
    public OpenAiResponseClient(string apiKey, string defaultModel = "gpt-4o-mini")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key must be provided.", nameof(apiKey));
        }

        if (string.IsNullOrWhiteSpace(defaultModel))
        {
            throw new ArgumentException("Default model must be provided.", nameof(defaultModel));
        }

        _defaultModel = defaultModel;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.openai.com/v1/"),
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GenerateAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string modelId = request.ModelId ?? _defaultModel;

        // Map ChatRequest.Messages -> Responses API "input" items
        ResponsesCreateRequestDto payload = new ()
        {
            Model = modelId,
            Input = MapInputItems(request),
            MaxOutputTokens = request.MaxTokens,
            Temperature = request.Temperature,
        };

        string json = JsonSerializer.Serialize(payload, _jsonOptions);

        using StringContent content = new (json, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _httpClient.PostAsync("responses", content, cancellationToken);

        string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        response.EnsureSuccessStatusCode();

        ResponsesCreateResponseDto? dto = JsonSerializer.Deserialize<ResponsesCreateResponseDto>(
            responseJson,
            _jsonOptions);

        // Aggregate text from response.output[*].output_text.content
        string aggregatedText = ExtractOutputText(dto);

        // The Responses API already has the notion of "role", but for simplicity
        // we treat the final aggregated text as a single assistant message.
        ChatMessage message = new ()
        {
            Role = ChatMessageRole.Assistant,
            Content = aggregatedText,
        };

        ChatResponse result = new ()
        {
            Message = message,
            ProviderMetadata = dto,
        };

        return result;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _httpClient.Dispose();
    }

    private static List<ResponsesInputItemDto> MapInputItems(ChatRequest request)
    {
        List<ResponsesInputItemDto> items = new (request.Messages.Count);

        foreach (ChatMessage msg in request.Messages)
        {
            items.Add(new ResponsesInputItemDto
            {
                Type = "message", // required by Responses API for message items
                Role = MapRole(msg.Role),
                Content = msg.Content ?? string.Empty,
            });
        }

        return items;
    }

    private static string MapRole(ChatMessageRole role) =>
        role switch
        {
            ChatMessageRole.System => "system",
            ChatMessageRole.User => "user",
            ChatMessageRole.Assistant => "assistant",
            ChatMessageRole.Tool => "tool",
            _ => "user"
        };

    /// <summary>
    /// Aggregates all assistant text from the Responses API output into a single string.
    /// </summary>
    private static string ExtractOutputText(ResponsesCreateResponseDto? dto)
    {
        if (dto?.Output == null || dto.Output.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sb = new ();

        foreach (ResponsesOutputMessageDto message in dto.Output)
        {
            // We expect type == "message" for chat-style responses.
            if (!string.Equals(message.Type, "message", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (message.Content == null)
            {
                continue;
            }

            foreach (ResponsesContentItemDto content in message.Content)
            {
                // We're interested in items where type == "output_text"
                if (!string.Equals(content.Type, "output_text", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(content.Text))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.AppendLine().AppendLine();
                }

                sb.Append(content.Text);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Request body for the Responses API.
    /// </summary>
    private sealed class ResponsesCreateRequestDto
    {
        /// <summary>
        /// Gets or sets the model identifier to use.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the input sequence for the model. For chat-like use cases this
        /// is typically an array of message items.
        /// </summary>
        [JsonPropertyName("input")]
        public List<ResponsesInputItemDto> Input { get; set; } = [];

        /// <summary>
        /// Gets or sets the optional maximum number of tokens in the model's output.
        /// </summary>
        [JsonPropertyName("max_output_tokens")]
        public int? MaxOutputTokens { get; set; }

        /// <summary>
        /// Gets or sets the optional temperature controlling response variability.
        /// </summary>
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }
    }

    /// <summary>
    /// Input item for the Responses API. Here we model only "message" items.
    /// </summary>
    private sealed class ResponsesInputItemDto
    {
        /// <summary>
        /// Gets or sets the item type. For chat-style messages this should be "message".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "message";

        /// <summary>
        /// Gets or sets the role for this message (system, user, assistant, tool).
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        /// <summary>
        /// Gets or sets the textual content of the message.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response body from the Responses API.
    /// </summary>
    private sealed class ResponsesCreateResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sequence of output items returned by the model.
        /// For chat-like use cases, this will be an array of "message" objects.
        /// </summary>
        [JsonPropertyName("output")]
        public List<ResponsesOutputMessageDto> Output { get; set; } = [];
    }

    /// <summary>
    /// A single output message from the Responses API output array.
    /// </summary>
    private sealed class ResponsesOutputMessageDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the "message" for chat-style output.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the role of the message author, e.g. "assistant".
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = "assistant";

        /// <summary>
        /// Gets or sets the content items within this message (e.g., output_text).
        /// </summary>
        [JsonPropertyName("content")]
        public List<ResponsesContentItemDto> Content { get; set; } = [];
    }

    /// <summary>
    /// A single content item inside an output message.
    /// For simple text output, type == "output_text" and Text contains the joke.
    /// </summary>
    private sealed class ResponsesContentItemDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        // The API also returns annotations and logprobs; we ignore them for now.
    }

    /// <summary>
    /// A single output item from the Responses API.
    /// </summary>
    private sealed class ResponsesOutputItemDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("output_text")]
        public ResponsesOutputTextDto? OutputText { get; set; }
    }

    /// <summary>
    /// The text payload of an output item when type == "output_text".
    /// </summary>
    private sealed class ResponsesOutputTextDto
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "assistant";

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
