// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Genova.Common.Attributes;

namespace Genova.Conduit.Embeddings;

/// <summary>
/// Represents an implementation of <see cref="IEmbeddingClient"/> that uses the OpenAI
/// Embeddings API to generate vector embeddings from text inputs.
/// </summary>
/// <remarks>
/// This client supports batch embedding requests (multiple input strings) and
/// returns one embedding per input in the order provided. The default embedding
/// model is <c>text-embedding-3-small</c>, though another model can be specified
/// when constructing the client.
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class OpenAiEmbeddingClient : IEmbeddingClient, IDisposable
{
    private const string EmbeddingsEndpoint = "embeddings";
    private readonly HttpClient _httpClient;
    private readonly string _defaultModel;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiEmbeddingClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">
    /// The HTTP client factory used to create <see cref="HttpClient"/> instances.
    /// </param>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <param name="defaultModel">
    /// The default embedding model identifier. Typically <c>text-embedding-3-small</c>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="apiKey"/> or <paramref name="defaultModel"/> is <c>null</c> or empty.
    /// </exception>
    public OpenAiEmbeddingClient(
        IHttpClientFactory httpClientFactory, string apiKey, string defaultModel = "text-embedding-3-small")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key must be provided.", nameof(apiKey));
        }

        if (string.IsNullOrWhiteSpace(defaultModel))
        {
            throw new ArgumentException("Model identifier must be provided.", nameof(defaultModel));
        }

        _defaultModel = defaultModel;

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

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };
    }

    /// <summary>
    /// Generates embeddings for the specified <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The embedding request containing input texts.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>
    /// A task whose result is an <see cref="EmbeddingResponse"/> containing one embedding
    /// per input string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <c>null</c>.
    /// </exception>
    public async Task<EmbeddingResponse> GenerateEmbeddingsAsync(
        EmbeddingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Inputs == null || request.Inputs.Count == 0)
        {
            throw new ArgumentException(
                "EmbeddingRequest.Inputs must contain at least one item.",
                nameof(request));
        }

        string modelId = request.ModelId ?? _defaultModel;

        EmbeddingsRequestDto payload = new ()
        {
            Model = modelId,
            Input = request.Inputs,
        };

        string json = JsonSerializer.Serialize(payload, _jsonOptions);

        using StringContent content =
            new (json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response =
            await _httpClient.PostAsync(EmbeddingsEndpoint, content, cancellationToken)
                .ConfigureAwait(false);

        string responseJson =
            await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        EmbeddingsResponseDto? dto =
            JsonSerializer.Deserialize<EmbeddingsResponseDto>(
                responseJson,
                _jsonOptions);

        if (dto == null || dto.Data == null)
        {
            throw new InvalidOperationException(
                "The OpenAI embeddings response could not be deserialized.");
        }

        List<Embedding> embeddings = new(dto.Data.Count);

        for (int i = 0; i < dto.Data.Count; i++)
        {
            EmbeddingsResponseItemDto item = dto.Data[i];

            if (item.Embedding == null)
            {
                throw new InvalidOperationException(
                    $"The embeddings array for item index {i} is null.");
            }

            Embedding embedding = new ()
            {
                Values = item.Embedding,
            };

            embeddings.Add(embedding);
        }

        EmbeddingResponse result = new ()
        {
            Embeddings = embeddings,
            ProviderMetadata = dto,
        };

        return result;
    }

    /// <summary>
    /// Releases resources used by this <see cref="OpenAiEmbeddingClient"/>.
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

    /// <summary>
    /// Represents the JSON body for a request to the OpenAI embeddings endpoint.
    /// </summary>
    private sealed class EmbeddingsRequestDto
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public IList<string> Input { get; set; } = [];
    }

    /// <summary>
    /// Represents the JSON response from the OpenAI embeddings endpoint.
    /// </summary>
    private sealed class EmbeddingsResponseDto
    {
        [JsonPropertyName("object")]
        public string? ObjectType { get; set; }

        [JsonPropertyName("data")]
        public IList<EmbeddingsResponseItemDto>? Data { get; set; }
    }

    /// <summary>
    /// Represents one element in the "data" array returned by OpenAI embeddings.
    /// </summary>
    private sealed class EmbeddingsResponseItemDto
    {
        [JsonPropertyName("object")]
        public string? ObjectType { get; set; }

        [JsonPropertyName("embedding")]
        public IReadOnlyList<float>? Embedding { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}
