// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Chats;

/// <summary>
/// Represents the result of a chat completion request.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class ChatResponse
{
    /// <summary>
    /// Gets or sets the assistant message produced by the model.
    /// </summary>
    public ChatMessage? Message { get; set; }

    /// <summary>
    /// Gets or sets raw provider-specific data, if any.
    /// </summary>
    public object? ProviderMetadata { get; set; }
}
