// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Storage;

/// <summary>
/// Represents the result of a similarity search operation.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class VectorSearchResult
{
    /// <summary>
    /// Gets or sets the record that matched the query.
    /// </summary>
    public VectorRecord Record { get; set; } = new VectorRecord();

    /// <summary>
    /// Gets or sets a similarity score where higher means more similar.
    /// </summary>
    public double Score { get; set; }
}
