// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Embeddings;

/// <summary>
/// Defines a request for embedding one or more inputs into vector space.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class EmbeddingRequest
{
    /// <summary>
    /// Gets or sets the textual inputs to embed.
    /// </summary>
    public IList<string> Inputs { get; set; } = [];

    /// <summary>
    /// Gets or sets an optional model identifier to use for this request.
    /// </summary>
    public string? ModelId { get; set; }
}
