// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Agents;

/// <summary>
/// Represents an in-memory implementation of <see cref="IAgentStateStore"/> that
/// stores agent states in a thread-safe dictionary with an optional capacity limit.
/// When the capacity is exceeded, the oldest states are evicted.
/// </summary>
/// <remarks>
/// This implementation is suitable for demos, development, and simple service hosts.
/// For production scenarios, a persistent implementation (file-based, database-backed,
/// or cloud-backed) should be provided.
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class InMemoryAgentStateStore : IAgentStateStore
{
    private readonly int _capacity;
    private readonly object _syncRoot;
    private readonly Dictionary<string, TimestampedAgentState> _states;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAgentStateStore"/> class
    /// with a default capacity of 100 agent states.
    /// </summary>
    public InMemoryAgentStateStore()
        : this(100)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAgentStateStore"/> class
    /// with the specified maximum capacity.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of agent states to store. When the capacity is exceeded,
    /// the oldest state (by last update timestamp) will be evicted.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="capacity"/> is less than or equal to zero.
    /// </exception>
    public InMemoryAgentStateStore(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                "Capacity must be greater than zero.");
        }

        _capacity = capacity;
        _syncRoot = new object();
        _states = new Dictionary<string, TimestampedAgentState>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Retrieves an agent state by its identifier, or <c>null</c> if no state exists.
    /// </summary>
    /// <param name="agentId">The identifier of the agent.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>
    /// A task whose result is the agent state, or <c>null</c> if no state is found.
    /// </returns>
    public Task<AgentState?> GetAsync(
        string agentId,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource<AgentState?> tcs = new();
            tcs.SetCanceled(cancellationToken);
            return tcs.Task;
        }

        if (string.IsNullOrWhiteSpace(agentId))
        {
            return Task.FromResult<AgentState?>(null);
        }

        lock (_syncRoot)
        {
            if (_states.TryGetValue(agentId, out TimestampedAgentState? entry) && entry != null)
            {
                return Task.FromResult<AgentState?>(entry.State);
            }
        }

        return Task.FromResult<AgentState?>(null);
    }

    /// <summary>
    /// Saves (creates or updates) an agent state in the store.
    /// </summary>
    /// <param name="state">The agent state to persist.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="state"/> or <paramref name="state.Id"/> is <c>null</c>.
    /// </exception>
    public Task SaveAsync(
        AgentState state,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource tcs = new ();
            tcs.SetCanceled(cancellationToken);
            return tcs.Task;
        }

        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(state.AgentId))
        {
            throw new ArgumentException(
                "AgentState must have a non-empty Id.",
                nameof(state));
        }

        lock (_syncRoot)
        {
            TimestampedAgentState entry = new ()
            {
                State = state,
                Timestamp = DateTimeOffset.UtcNow,
            };

            _states[state.AgentId] = entry;

            if (_states.Count > _capacity)
            {
                EvictOldestStates();
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes states until the store's size is within the configured capacity.
    /// The oldest states (by last update timestamp) are evicted first.
    /// </summary>
    private void EvictOldestStates()
    {
        while (_states.Count > _capacity)
        {
            string? oldestKey = null;
            DateTimeOffset oldestTimestamp = DateTimeOffset.MaxValue;

            foreach (KeyValuePair<string, TimestampedAgentState> pair in _states)
            {
                if (pair.Value.Timestamp < oldestTimestamp)
                {
                    oldestTimestamp = pair.Value.Timestamp;
                    oldestKey = pair.Key;
                }
            }

            if (oldestKey == null)
            {
                break;
            }

            _states.Remove(oldestKey);
        }
    }

    /// <summary>
    /// Private helper class that wraps an <see cref="AgentState"/> with a timestamp.
    /// </summary>
    private sealed class TimestampedAgentState
    {
        /// <summary>
        /// Gets or sets the agent state.
        /// </summary>
        public AgentState State { get; set; } = null!;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}
