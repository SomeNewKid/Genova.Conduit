// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Embeddings;
using Genova.Conduit.Storage;

namespace Genova.Conduit.Steps;

/// <summary>
/// Represents a pipeline step that performs a semantic search against an
/// <see cref="IVectorStore"/> using a query embedding stored in the
/// <see cref="PipelineContext"/>.
/// </summary>
/// <remarks>
/// This step expects that a previous step has placed an <see cref="EmbeddingResponse"/>
/// containing at least one embedding into the context using a well-known key.
/// The first embedding in that response is used as the query vector.
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class SearchVectorStoreStep : IPipelineStep
{
    private readonly IVectorStore _vectorStore;
    private readonly string _queryEmbeddingKey;
    private readonly string _searchResultsKey;
    private readonly int _topK;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchVectorStoreStep"/> class.
    /// </summary>
    /// <param name="vectorStore">
    /// The vector store to be queried for similar records.
    /// </param>
    /// <param name="queryEmbeddingKey">
    /// The key in <see cref="PipelineContext.Items"/> that contains the
    /// <see cref="EmbeddingResponse"/> representing the query embedding.
    /// </param>
    /// <param name="searchResultsKey">
    /// The key under which the resulting <see cref="VectorSearchResult"/> collection
    /// will be stored in <see cref="PipelineContext.Items"/>.
    /// </param>
    /// <param name="topK">
    /// The maximum number of results to return from the vector store.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="vectorStore"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="queryEmbeddingKey"/> or <paramref name="searchResultsKey"/>
    /// is <c>null</c> or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="topK"/> is less than or equal to zero.
    /// </exception>
    public SearchVectorStoreStep(
        IVectorStore vectorStore,
        string queryEmbeddingKey,
        string searchResultsKey,
        int topK)
    {
        if (vectorStore == null)
        {
            throw new ArgumentNullException(nameof(vectorStore));
        }

        if (string.IsNullOrWhiteSpace(queryEmbeddingKey))
        {
            throw new ArgumentException("Query embedding key must be non-empty.", nameof(queryEmbeddingKey));
        }

        if (string.IsNullOrWhiteSpace(searchResultsKey))
        {
            throw new ArgumentException("Search results key must be non-empty.", nameof(searchResultsKey));
        }

        if (topK <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topK),
                topK,
                "topK must be greater than zero.");
        }

        _vectorStore = vectorStore;
        _queryEmbeddingKey = queryEmbeddingKey;
        _searchResultsKey = searchResultsKey;
        _topK = topK;
    }

    /// <summary>
    /// Executes the step by retrieving the query embedding from the context,
    /// performing a similarity search against the vector store, and storing
    /// the results back into the context.
    /// </summary>
    /// <param name="context">The shared pipeline context.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the context does not contain a valid <see cref="EmbeddingResponse"/>
    /// under the expected key, or when the response does not contain any embeddings.
    /// </exception>
    public async Task ExecuteAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        object? rawEmbeddingResponse = context.GetItem<object>(_queryEmbeddingKey);
        if (rawEmbeddingResponse == null)
        {
            throw new InvalidOperationException(
                $"Pipeline context does not contain an EmbeddingResponse under key '{_queryEmbeddingKey}'.");
        }

        EmbeddingResponse? embeddingResponse = rawEmbeddingResponse as EmbeddingResponse;
        if (embeddingResponse == null)
        {
            throw new InvalidOperationException(
                $"Context item '{_queryEmbeddingKey}' is not an EmbeddingResponse.");
        }

        if (embeddingResponse.Embeddings == null ||
            embeddingResponse.Embeddings.Count == 0)
        {
            throw new InvalidOperationException(
                $"EmbeddingResponse under key '{_queryEmbeddingKey}' does not contain any embeddings.");
        }

        // Use the first embedding as the query vector.
        Embedding queryEmbedding = embeddingResponse.Embeddings[0];
        if (queryEmbedding.Values == null)
        {
            throw new InvalidOperationException(
                "The query Embedding has a null Values collection.");
        }

        IReadOnlyList<float> queryVector = queryEmbedding.Values;

        IReadOnlyList<VectorSearchResult> results =
            await _vectorStore.SearchAsync(queryVector, _topK, cancellationToken)
                .ConfigureAwait(false);

        context.SetItem(_searchResultsKey, results);
    }
}
