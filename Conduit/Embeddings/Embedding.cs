// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Embeddings;

/// <summary>
/// Represents a single embedding vector.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class Embedding
{
    /// <summary>
    /// Gets or sets the floating-point values that comprise the embedding.
    /// </summary>
    public IReadOnlyList<float> Values { get; set; } = Array.Empty<float>();
}
