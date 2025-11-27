// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Chats;

/// <summary>
/// Client abstraction for interacting with a chat-capable model.
/// </summary>
/// <remarks>
/// Implementations may target OpenAI, local models, or other providers.
/// </remarks>
public interface IChatClient
{
    /// <summary>
    /// Generates a chat completion for the specified <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The chat request to process.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The chat completion result produced by the model.</returns>
    Task<ChatResponse> GenerateAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);
}
