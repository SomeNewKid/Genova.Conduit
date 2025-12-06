// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

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
/// tool definitions and function-call style outputs.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class OpenAiResponseClient : IChatClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _defaultModel;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    private const string ResponsesEndpoint = "responses";

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
        if (httpClientFactory == null)
        {
            throw new ArgumentNullException(nameof(httpClientFactory));
        }

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

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        string modelId = request.ModelId ?? _defaultModel;

        ResponsesCreateRequestDto payload = new ResponsesCreateRequestDto
        {
            Model = modelId,
            Input = MapInputItems(request.Messages),
            MaxOutputTokens = request.MaxTokens,
            Temperature = request.Temperature,
            Tools = MapTools(request.Tools)
        };

        string json = JsonSerializer.Serialize(payload, _jsonOptions);

        File.WriteAllText(@"C:\temp\debug4.txt", json);

        using StringContent content =
            new StringContent(json, Encoding.UTF8, "application/json");

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
                ProviderMetadata = dto
            };
        }

        ResponsesOutputItemDto output = dto.Output[0];

        ChatMessage? assistantMessage = MapOutputToChatMessage(output);

        return new ChatResponse
        {
            Message = assistantMessage,
            ProviderMetadata = dto
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

    #region DTOs and mapping helpers

    private sealed class ResponsesCreateRequestDto
    {
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// The input sequence for the Responses API. For chat-style use cases
        /// this is typically an array of message items.
        /// </summary>
        public List<ResponsesInputItemDto> Input { get; set; } =
            new List<ResponsesInputItemDto>();

        [JsonPropertyName("max_output_tokens")]
        public int? MaxOutputTokens { get; set; }

        public double? Temperature { get; set; }

        /// <summary>
        /// Optional tool definitions to be sent to the Responses API.
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

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public object Parameters { get; set; } = new { };
    }

    private sealed class ResponsesCreateResponseDto
    {
        public List<ResponsesOutputItemDto>? Output { get; set; }
    }

    private sealed class ResponsesOutputItemDto
    {
        public string Type { get; set; } = "message";

        public string? Status { get; set; }

        public string? Role { get; set; }

        public List<ResponsesTextSegmentDto>? Content { get; set; }

        public string? Name { get; set; }

        public string? Arguments { get; set; }

        public string GetCombinedText()
        {
            if (Content == null || Content.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < Content.Count; i++)
            {
                ResponsesTextSegmentDto segment = Content[i];
                if (!string.IsNullOrEmpty(segment.Text))
                {
                    builder.Append(segment.Text);
                }
            }

            return builder.ToString();
        }
    }

    private sealed class ResponsesTextSegmentDto
    {
        public string Type { get; set; } = "output_text";

        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Internal DTO used when we synthesize a tool request payload for Mossbot
    /// from a Responses API function_call output. This matches the shape expected
    /// by higher-level pipeline code (e.g., { "tool": "...", "keys": "..." }).
    /// </summary>
    private sealed class ToolRequestPayload
    {
        [JsonPropertyName("tool")]
        public string? Tool { get; set; }

        [JsonPropertyName("keys")]
        public string? Keys { get; set; }
    }

    private List<ResponsesInputItemDto> MapInputItems(IList<ChatMessage> messages)
    {
        List<ResponsesInputItemDto> list = new List<ResponsesInputItemDto>(messages.Count);

        for (int i = 0; i < messages.Count; i++)
        {
            ChatMessage message = messages[i];

            ResponsesInputItemDto item = new ResponsesInputItemDto
            {
                Type = "message",
                Role = MapRole(message.Role),
                Content = message.Content
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

        List<ResponsesToolDefinitionDto> list = new List<ResponsesToolDefinitionDto>(tools.Count);

        for (int i = 0; i < tools.Count; i++)
        {
            ToolDefinition tool = tools[i];

            if (string.IsNullOrWhiteSpace(tool.Name))
            {
                continue;
            }

            object parameters;

            try
            {
                parameters = JsonSerializer.Deserialize<object>(
                                 tool.ParametersJsonSchema,
                                 new JsonSerializerOptions
                                 {
                                     PropertyNameCaseInsensitive = true
                                 }) ?? new { };
            }
            catch
            {
                parameters = new { };
            }

            ResponsesToolDefinitionDto dto = new ResponsesToolDefinitionDto
            {
                Type = "function",
                Name = tool.Name,
                Description = tool.Description,
                Parameters = parameters
            };

            list.Add(dto);
        }

        return list.Count == 0 ? null : list;
    }

    private ChatMessage? MapOutputToChatMessage(ResponsesOutputItemDto output)
    {
        if (string.Equals(output.Type, "function_call", StringComparison.OrdinalIgnoreCase))
        {
            // Tool call output: synthesize a payload that higher-level code can
            // interpret as a tool request, e.g. { "tool": "dot-notation", "keys": "..." }.

            string toolName = output.Name ?? string.Empty;
            string? keys = ExtractKeysFromArguments(output.Arguments);

            ToolRequestPayload payload = new ToolRequestPayload
            {
                Tool = toolName,
                Keys = keys
            };

            string contentJson = JsonSerializer.Serialize(payload, _jsonOptions);

            ChatMessage toolMessage = new ChatMessage
            {
                Role = ChatMessageRole.Assistant,
                Content = contentJson
            };

            return toolMessage;
        }

        // Normal message output: combine text segments.
        string text = output.GetCombinedText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        ChatMessageRole role = MapRoleBack(output.Role);

        ChatMessage assistantMessage = new ChatMessage
        {
            Role = role,
            Content = text
        };

        return assistantMessage;
    }

    private string? ExtractKeysFromArguments(string? argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return null;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(argumentsJson);
            JsonElement root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("keys", out JsonElement keysElement))
            {
                if (keysElement.ValueKind == JsonValueKind.String)
                {
                    return keysElement.GetString();
                }

                if (keysElement.ValueKind == JsonValueKind.Array)
                {
                    List<string> parts = new List<string>();

                    foreach (JsonElement child in keysElement.EnumerateArray())
                    {
                        if (child.ValueKind == JsonValueKind.String)
                        {
                            string? value = child.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                parts.Add(value);
                            }
                        }
                    }

                    if (parts.Count > 0)
                    {
                        return string.Join(", ", parts);
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, fall through and return null.
        }

        return null;
    }

    private static string MapRole(ChatMessageRole role)
    {
        switch (role)
        {
            case ChatMessageRole.System:
                return "system";
            case ChatMessageRole.User:
                return "user";
            case ChatMessageRole.Assistant:
                return "assistant";
            case ChatMessageRole.Tool:
                return "tool";
            default:
                return "user";
        }
    }

    private static ChatMessageRole MapRoleBack(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return ChatMessageRole.Assistant;
        }

        switch (role)
        {
            case "system":
                return ChatMessageRole.System;
            case "user":
                return ChatMessageRole.User;
            case "assistant":
                return ChatMessageRole.Assistant;
            case "tool":
                return ChatMessageRole.Tool;
            default:
                return ChatMessageRole.Assistant;
        }
    }

    #endregion
}
