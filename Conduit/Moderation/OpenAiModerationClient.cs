// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Genova.Common.Attributes;

namespace Genova.Conduit.Moderation;

/// <summary>
/// OpenAI implementation of <see cref="IModerationClient"/> using
/// the OpenAI Moderation API.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class OpenAiModerationClient : IModerationClient
{
    private const string DefaultModel = "omni-moderation-latest";
    private const string ModerationsEndpoint = "moderations";

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiModerationClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="apiKey">The OpenAI API key.</param>
    public OpenAiModerationClient(
        IHttpClientFactory httpClientFactory,
        string apiKey)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        _httpClient = httpClientFactory.CreateClient("Genova.Conduit.OpenAI.Moderation");

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        }

        if (_httpClient.DefaultRequestHeaders.Authorization == null)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    /// <inheritdoc />
    public async Task<ModerationResponse> ModerateAsync(
        ModerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        OpenAiModerationRequestDto payload = new OpenAiModerationRequestDto
        {
            Input = request.Input,
            Model = request.ModelId ?? DefaultModel,
        };

        string json = JsonSerializer.Serialize(payload, _jsonOptions);

        using StringContent content =
            new StringContent(json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response =
            await _httpClient.PostAsync(
                ModerationsEndpoint,
                content,
                cancellationToken)
            .ConfigureAwait(false);

        string responseJson =
            await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        OpenAiModerationResponseDto? dto =
            JsonSerializer.Deserialize<OpenAiModerationResponseDto>(
                responseJson,
                _jsonOptions);

        ModerationResponse result = new ModerationResponse
        {
            ProviderMetadata = dto,
        };

        if (dto?.Results != null && dto.Results.Count > 0)
        {
            OpenAiModerationResultDto first = dto.Results[0];

            result.Flagged = first.Flagged;

            if (first.Categories != null)
            {
                foreach (KeyValuePair<string, bool> pair in first.Categories)
                {
                    result.Categories[pair.Key] = pair.Value;
                }
            }
        }

        return result;
    }

    private sealed class OpenAiModerationRequestDto
    {
        public string Input { get; set; } = string.Empty;

        public string Model { get; set; } = DefaultModel;
    }

    private sealed class OpenAiModerationResponseDto
    {
        public List<OpenAiModerationResultDto>? Results { get; set; }
    }

    private sealed class OpenAiModerationResultDto
    {
        public bool Flagged { get; set; }

        public Dictionary<string, bool>? Categories { get; set; }
    }
}
