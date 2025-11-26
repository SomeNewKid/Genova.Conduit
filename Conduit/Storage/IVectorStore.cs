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
    /// <paramref name="embedding"/>.
    /// </summary>
    /// <param name="embedding">The query embedding.</param>
    /// <param name="topK">The maximum number of results to return.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of search results ordered by similarity.</returns>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        IReadOnlyList<float> embedding,
        int topK,
        CancellationToken cancellationToken = default);
}
