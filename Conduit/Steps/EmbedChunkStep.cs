// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Embeddings;
using Genova.Conduit.Storage;

namespace Genova.Conduit.Steps;

/// <summary>
/// Represents a pipeline step that generates embeddings for a collection of text chunks
/// using an <see cref="IEmbeddingClient"/>. The embeddings are written back into the
/// <see cref="PipelineContext"/> under a caller-specified key.
/// </summary>
/// <remarks>
/// This step does not perform storage of embeddings into an <see cref="IVectorStore"/>.
/// A separate step should be used for that purpose.
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class EmbedChunkStep : IPipelineStep
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly string _chunksKey;
    private readonly string _embeddingsKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbedChunkStep"/> class.
    /// </summary>
    /// <param name="embeddingClient">
    /// The embedding client used to generate embeddings for text inputs.
    /// </param>
    /// <param name="chunksKey">
    /// The context key under which a list of text chunks (<see cref="IList{String}"/>) is stored.
    /// </param>
    /// <param name="embeddingsKey">
    /// The context key under which the resulting <see cref="EmbeddingResponse"/> will be stored.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="embeddingClient"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any string parameter is <c>null</c> or whitespace.
    /// </exception>
    public EmbedChunkStep(
        IEmbeddingClient embeddingClient,
        string chunksKey,
        string embeddingsKey)
    {
        if (embeddingClient == null)
        {
            throw new ArgumentNullException(nameof(embeddingClient));
        }

        if (string.IsNullOrWhiteSpace(chunksKey))
        {
            throw new ArgumentException("Chunks key must be non-empty.", nameof(chunksKey));
        }

        if (string.IsNullOrWhiteSpace(embeddingsKey))
        {
            throw new ArgumentException("Embeddings key must be non-empty.", nameof(embeddingsKey));
        }

        _embeddingClient = embeddingClient;
        _chunksKey = chunksKey;
        _embeddingsKey = embeddingsKey;
    }

    /// <summary>
    /// Executes the step by reading text chunks from the pipeline context,
    /// generating embeddings for them, and writing the resulting embedding response
    /// back to the context.
    /// </summary>
    /// <param name="context">The shared pipeline context.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the context does not contain the expected chunks collection.
    /// </exception>
    public async Task ExecuteAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // Retrieve the chunks from the context
        object? rawChunks = context.GetItem<object>(_chunksKey);

        if (rawChunks == null)
        {
            throw new InvalidOperationException(
                $"Pipeline context does not contain any text chunks under key '{_chunksKey}'.");
        }

        IList<string>? chunks = rawChunks as IList<string>;

        if (chunks == null)
        {
            throw new InvalidOperationException(
                $"Context item '{_chunksKey}' is not an IList<string>.");
        }

        if (chunks.Count == 0)
        {
            throw new InvalidOperationException(
                $"Context item '{_chunksKey}' contains zero chunks to embed.");
        }

        // Build the embedding request
        EmbeddingRequest request = new EmbeddingRequest
        {
            Inputs = chunks,
            ModelId = null, // Use the embedding client's default model
        };

        // Generate embeddings via the client
        EmbeddingResponse response =
            await _embeddingClient.GenerateEmbeddingsAsync(request, cancellationToken)
                .ConfigureAwait(false);

        // Store the embeddings in the context
        context.SetItem(_embeddingsKey, response);
    }
}
