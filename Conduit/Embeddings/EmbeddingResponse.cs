// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Embeddings;

/// <summary>
/// Represents the result of an embedding request.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class EmbeddingResponse
{
    /// <summary>
    /// Gets or sets the embedding vectors, one per input.
    /// </summary>
    public IList<Embedding> Embeddings { get; set; } = [];

    /// <summary>
    /// Gets or sets raw provider-specific data, if any.
    /// </summary>
    public object? ProviderMetadata { get; set; }
}
