// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Chats;

/// <summary>
/// Represents a single message in a chat interaction.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class ChatMessage
{
    /// <summary>
    /// Gets or sets the role of the message.
    /// </summary>
    public ChatMessageRole Role { get; set; }

    /// <summary>
    /// Gets or sets the textual content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
