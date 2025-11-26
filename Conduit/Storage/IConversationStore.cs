// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Storage;

/// <summary>
/// Abstraction for persisting and retrieving conversation history.
/// </summary>
public interface IConversationStore
{
    /// <summary>
    /// Retrieves a conversation by its <paramref name="conversationId"/>.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// The conversation if found; otherwise <c>null</c>.
    /// </returns>
    Task<Conversation?> GetConversationAsync(
        string conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the specified <paramref name="conversation"/>.
    /// </summary>
    /// <param name="conversation">The conversation to store.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    Task SaveConversationAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default);
}
