// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Models;

/// <summary>
/// Represents the result of an embedding request.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class EmbeddingResult
{
    /// <summary>
    /// Gets or sets the embedding vectors, one per input.
    /// </summary>
    public IList<Embedding> Embeddings { get; set; } = new List<Embedding>();

    /// <summary>
    /// Gets or sets raw provider-specific data, if any.
    /// </summary>
    public object? ProviderMetadata { get; set; }
}
