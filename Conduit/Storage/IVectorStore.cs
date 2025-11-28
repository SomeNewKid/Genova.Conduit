// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Storage;

/// <summary>
/// Abstraction over a vector store used for semantic search and retrieval.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Adds or updates vector records in the store.
    /// </summary>
    /// <param name="records">The records to upsert.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    Task UpsertAsync(
        IEnumerable<VectorRecord> records,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a similarity search against the store using the specified
    /// <paramref name="embedding"/>, returning at most <paramref name="topK"/> results.
    /// </summary>
    /// <param name="embedding">The query embedding.</param>
    /// <param name="topK">The maximum number of results to return.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of search results ordered by similarity.</returns>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        IReadOnlyList<float> embedding,
        int topK,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a similarity search against the store using the specified
    /// <paramref name="embedding"/>, returning all results whose similarity
    /// score is greater than or equal to <paramref name="minConfidence"/>,
    /// limited to at most <paramref name="maxResults"/> items.
    /// </summary>
    /// <param name="embedding">The query embedding.</param>
    /// <param name="minConfidence">
    /// The minimum cosine similarity score required for a result to be included.
    /// Expected range is between 0.0 and 1.0. A typical starting value is 0.2,
    /// which tends to filter out obviously irrelevant matches while retaining
    /// potentially relevant results.
    /// </param>
    /// <param name="maxResults">
    /// The maximum number of results to return after applying the confidence filter.
    /// A typical starting value is 5, which provides sufficient context without
    /// overwhelming the language model or incurring unnecessary token costs.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A collection of search results ordered by similarity. The collection may be empty
    /// if no records meet the minimum confidence.
    /// </returns>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        IReadOnlyList<float> embedding,
        float minConfidence = 0.2f,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
}
