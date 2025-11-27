// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Chats;

namespace Genova.Conduit.Storage;

/// <summary>
/// Represents a stored conversation consisting of one or more messages.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class Conversation
{
    /// <summary>
    /// Gets or sets the unique identifier of the conversation.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the messages that comprise the conversation history.
    /// </summary>
    public IList<ChatMessage> Messages { get; set; } = [];
}
