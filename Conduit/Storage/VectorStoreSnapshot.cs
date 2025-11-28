// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Storage;

/// <summary>
/// Represents a snapshot of vector records persisted to or loaded from a JSON file.
/// The snapshot may be used to populate an in-memory vector store at application startup.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class VectorStoreSnapshot
{
    /// <summary>
    /// Gets or sets the unique identifier of the embedding model used to generate
    /// the embeddings contained within this snapshot.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp indicating when this snapshot was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the vector records contained in the snapshot.
    /// </summary>
    public IList<VectorRecord> Records { get; set; } = [];
}
