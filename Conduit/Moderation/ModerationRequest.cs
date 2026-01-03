// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Moderation;

/// <summary>
/// Represents a request to evaluate text using a moderation model.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class ModerationRequest
{
    /// <summary>
    /// Gets or sets the text to evaluate.
    /// </summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model identifier to use.
    /// If null, a default moderation model is used.
    /// </summary>
    public string? ModelId { get; set; }
}
