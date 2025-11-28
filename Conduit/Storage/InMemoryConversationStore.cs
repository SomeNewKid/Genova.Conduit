// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Storage;

/// <summary>
/// Represents an in-memory implementation of <see cref="IConversationStore"/>
/// that holds a limited number of conversations and evicts the oldest
/// conversations first when the capacity is exceeded.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class InMemoryConversationStore : InMemoryStoreBase<Conversation>, IConversationStore
{
    private const int DefaultCapacity = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryConversationStore"/> class
    /// with the default capacity.
    /// </summary>
    public InMemoryConversationStore()
        : base(DefaultCapacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryConversationStore"/> class
    /// with the specified capacity.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of conversations to store. When the capacity is exceeded,
    /// the oldest conversations (by last updated timestamp) are removed first.
    /// </param>
    public InMemoryConversationStore(int capacity)
        : base(capacity)
    {
    }

    /// <summary>
    /// Retrieves a conversation by its identifier.
    /// </summary>
    /// <param name="conversationId">The identifier of the conversation to retrieve.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>
    /// A task whose result is the conversation if found; otherwise, <c>null</c>.
    /// </returns>
    public Task<Conversation?> GetConversationAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource<Conversation?> tcs = new TaskCompletionSource<Conversation?>();
            tcs.SetCanceled(cancellationToken);
            return tcs.Task;
        }

        Conversation? conversation;
        bool found = TryGetValue(conversationId, out conversation);

        if (!found)
        {
            return Task.FromResult<Conversation?>(null);
        }

        return Task.FromResult(conversation);
    }

    /// <summary>
    /// Persists the specified conversation in the store, updating its timestamp
    /// and evicting older conversations if necessary.
    /// </summary>
    /// <param name="conversation">The conversation to store.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="conversation"/> is <c>null</c>.
    /// </exception>
    public Task SaveConversationAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource tcs = new TaskCompletionSource();
            tcs.SetCanceled(cancellationToken);
            return tcs.Task;
        }

        if (string.IsNullOrWhiteSpace(conversation.Id))
        {
            throw new ArgumentException(
                "Conversation must have a non-empty Id.",
                nameof(conversation));
        }

        SetValue(conversation.Id, conversation);
        return Task.CompletedTask;
    }
}
