// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Storage;

/// <summary>
/// Represents a single entry in a vector store.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class VectorRecord
{
    /// <summary>
    /// Gets or sets the unique identifier for this record.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the embedding vector associated with this record.
    /// </summary>
    public IReadOnlyList<float> Embedding { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Gets or sets optional metadata associated with this record.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; set; } =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
}
