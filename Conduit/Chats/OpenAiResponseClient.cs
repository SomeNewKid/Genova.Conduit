// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Genova.Common.Attributes;
using Genova.Conduit.Tools;

namespace Genova.Conduit.Chats;

/// <summary>
/// Represents an implementation of <see cref="IChatClient"/> that uses the
/// OpenAI Responses API to generate chat completions, including optional
/// tool definitions.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class OpenAiResponseClient : IChatClient, IDisposable
{
    private const string ResponsesEndpoint = "responses";

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Conflicting naming rules.")]
    private static readonly JsonSerializerOptions ToolParametersJsonOptions = new ()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly string _defaultModel;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiResponseClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory used to create <see cref="HttpClient"/> instances.</param>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <param name="defaultModel">
    /// The default model identifier to use when <see cref="ChatRequest.ModelId"/> is <c>null</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClientFactory"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="apiKey"/> or <paramref name="defaultModel"/> is <c>null</c> or whitespace.
    /// </exception>
    public OpenAiResponseClient(
        IHttpClientFactory httpClientFactory,
        string apiKey,
        string defaultModel = "gpt-4o-mini")
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key must be provided.", nameof(apiKey));
        }

        if (string.IsNullOrWhiteSpace(defaultModel))
        {
            throw new ArgumentException("Default model must be provided.", nameof(defaultModel));
        }

        _defaultModel = defaultModel;

        HttpClient client = httpClientFactory.CreateClient("Genova.Conduit.OpenAI.Responses");

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

        JsonSerializerOptions options = new ()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        _jsonOptions = options;
    }

    /// <summary>
    /// Generates a chat response for the specified <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The chat request to process.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The chat response produced by the model.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <c>null</c>.
    /// </exception>
    public async Task<ChatResponse> GenerateAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string modelId = request.ModelId ?? _defaultModel;

        ResponsesCreateRequestDto payload = new ()
        {
            Model = modelId,
            Input = MapInputItems(request.Messages),
            MaxOutputTokens = request.MaxTokens,
            Temperature = request.Temperature,
            Tools = MapTools(request.Tools),
        };

        string json = JsonSerializer.Serialize(payload, _jsonOptions);

        using StringContent content = new (json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response =
            await _httpClient.PostAsync(ResponsesEndpoint, content, cancellationToken)
                .ConfigureAwait(false);

        string responseJson =
            await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        ResponsesCreateResponseDto? dto =
            JsonSerializer.Deserialize<ResponsesCreateResponseDto>(responseJson, _jsonOptions);

        if (dto == null || dto.Output == null || dto.Output.Count == 0)
        {
            return new ChatResponse
            {
                Message = null,
                ProviderMetadata = dto,
            };
        }

        // For simplicity, treat the first message output as the assistant reply.
        ResponsesOutputMessageDto messageDto = dto.Output[0];

        ChatMessage assistantMessage = new ()
        {
            Role = MapRoleBack(messageDto.Role),
            Content = messageDto.GetCombinedText(),
        };

        return new ChatResponse
        {
            Message = assistantMessage,
            ProviderMetadata = dto,
        };
    }

    /// <summary>
    /// Releases resources used by this <see cref="OpenAiResponseClient"/>.
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

    private static List<ResponsesInputItemDto> MapInputItems(IList<ChatMessage> messages)
    {
        List<ResponsesInputItemDto> list = new (messages.Count);

        for (int i = 0; i < messages.Count; i++)
        {
            ChatMessage message = messages[i];

            ResponsesInputItemDto item = new ()
            {
                Type = "message",
                Role = MapRole(message.Role),
                Content = message.Content,
            };

            list.Add(item);
        }

        return list;
    }

    private static List<ResponsesToolDefinitionDto>? MapTools(IList<ToolDefinition> tools)
    {
        if (tools == null || tools.Count == 0)
        {
            return null;
        }

        List<ResponsesToolDefinitionDto> list = new (tools.Count);

        for (int i = 0; i < tools.Count; i++)
        {
            ToolDefinition tool = tools[i];

            if (string.IsNullOrWhiteSpace(tool.Name))
            {
                continue;
            }

            ResponsesToolDefinitionDto dto = new ()
            {
                Type = "function",
                Function = new ResponsesToolFunctionDto
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    Parameters = JsonSerializer.Deserialize<object>(
                        tool.ParametersJsonSchema,
                        ToolParametersJsonOptions) ?? new { },
                },
            };

            list.Add(dto);
        }

        return list.Count == 0 ? null : list;
    }

    private static string MapRole(ChatMessageRole role)
    {
        return role switch
        {
            ChatMessageRole.System => "system",
            ChatMessageRole.User => "user",
            ChatMessageRole.Assistant => "assistant",
            ChatMessageRole.Tool => "tool",
            _ => "user",
        };
    }

    private static ChatMessageRole MapRoleBack(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return ChatMessageRole.Assistant;
        }

        return role switch
        {
            "system" => ChatMessageRole.System,
            "user" => ChatMessageRole.User,
            "assistant" => ChatMessageRole.Assistant,
            "tool" => ChatMessageRole.Tool,
            _ => ChatMessageRole.Assistant,
        };
    }

    private sealed class ResponsesCreateRequestDto
    {
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the input sequence for the Responses API. For chat-style use cases
        /// this is typically an array of message items.
        /// </summary>
        public List<ResponsesInputItemDto> Input { get; set; } = [];

        [JsonPropertyName("max_output_tokens")]
        public int? MaxOutputTokens { get; set; }

        public double? Temperature { get; set; }

        /// <summary>
        /// Gets or sets optional tool definitions to be sent to the Responses API.
        /// </summary>
        public List<ResponsesToolDefinitionDto>? Tools { get; set; }
    }

    private sealed class ResponsesInputItemDto
    {
        public string Type { get; set; } = "message";

        public string Role { get; set; } = "user";

        public string? Content { get; set; }
    }

    private sealed class ResponsesToolDefinitionDto
    {
        public string Type { get; set; } = "function";

        public ResponsesToolFunctionDto Function { get; set; } =
            new ResponsesToolFunctionDto();
    }

    private sealed class ResponsesToolFunctionDto
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public object Parameters { get; set; } = new { };
    }

    private sealed class ResponsesCreateResponseDto
    {
        public List<ResponsesOutputMessageDto>? Output { get; set; }
    }

    private sealed class ResponsesOutputMessageDto
    {
        public string Role { get; set; } = "assistant";

        /// <summary>
        /// Gets or sets the content segments of the output message.
        /// For simplicity we assume the output is text; if your schema supports
        /// structured output segments, extend this DTO accordingly.
        /// </summary>
        public List<ResponsesTextSegmentDto>? Content { get; set; }

        public string GetCombinedText()
        {
            if (Content == null || Content.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new ();

            for (int i = 0; i < Content.Count; i++)
            {
                ResponsesTextSegmentDto segment = Content[i];
                builder.Append(segment.Text);
            }

            return builder.ToString();
        }
    }

    private sealed class ResponsesTextSegmentDto
    {
        public string Type { get; set; } = "output_text";

        public string Text { get; set; } = string.Empty;
    }
}
