// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Models;

/// <summary>
/// Defines a request to a chat-capable model.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class ChatRequest
{
    /// <summary>
    /// Gets or sets the sequence of messages that make up the chat history.
    /// </summary>
    public IList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    /// <summary>
    /// Gets or sets an optional model identifier to use for this request.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Gets or sets an optional maximum token limit for the response.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets an optional temperature value controlling response variability.
    /// </summary>
    public double? Temperature { get; set; }
}
