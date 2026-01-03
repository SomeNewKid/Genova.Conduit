// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Moderation;

/// <summary>
/// Represents the result of a moderation evaluation.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class ModerationResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the content was flagged.
    /// </summary>
    public bool Flagged { get; set; }

    /// <summary>
    /// Gets the set of categories that were flagged.
    /// </summary>
    public IDictionary<string, bool> Categories { get; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets provider-specific metadata returned by the moderation service.
    /// </summary>
    public object? ProviderMetadata { get; set; }
}
