// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Agents;

/// <summary>
/// Represents an in-memory implementation of <see cref="IAgentTaskStore"/> that
/// stores tasks in a thread-safe dictionary keyed by task identifier.
/// </summary>
/// <remarks>
/// This implementation is suitable for demos, development, and simple local agents.
/// For production scenarios, a persistent implementation (file-based, database-backed,
/// or cloud-backed) should be provided.
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class InMemoryAgentTaskStore : IAgentTaskStore
{
    private readonly object _syncRoot;
    private readonly Dictionary<string, IAgentTask> _tasks;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAgentTaskStore"/> class.
    /// </summary>
    public InMemoryAgentTaskStore()
    {
        _syncRoot = new object();
        _tasks = new Dictionary<string, IAgentTask>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Retrieves a collection of tasks that are ready to be processed by
    /// the specified agent. This implementation returns tasks whose
    /// <see cref="IAgentTask.Status"/> is <see cref="AgentTaskStatus.Pending"/>.
    /// </summary>
    /// <param name="agentId">The identifier of the agent requesting tasks.</param>
    /// <param name="maxCount">The maximum number of tasks to return.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A list of pending tasks for the specified agent. The list may be empty
    /// if no tasks are available.
    /// </returns>
    public Task<IReadOnlyList<IAgentTask>> GetPendingTasksAsync(
        string agentId,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource<IReadOnlyList<IAgentTask>> tcs = new();
            tcs.SetCanceled(cancellationToken);
            return tcs.Task;
        }

        if (string.IsNullOrWhiteSpace(agentId) || maxCount <= 0)
        {
            return Task.FromResult<IReadOnlyList<IAgentTask>>(
                Array.Empty<IAgentTask>());
        }

        List<IAgentTask> pending = [];

        lock (_syncRoot)
        {
            foreach (KeyValuePair<string, IAgentTask> pair in _tasks)
            {
                IAgentTask task = pair.Value;

                if (!string.Equals(task.AgentId, agentId, StringComparison.Ordinal) &&
                    !string.Equals(task.AgentId, agentId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (task.Status != AgentTaskStatus.Pending)
                {
                    continue;
                }

                pending.Add(task);

                if (pending.Count >= maxCount)
                {
                    break;
                }
            }
        }

        return Task.FromResult<IReadOnlyList<IAgentTask>>(pending);
    }

    /// <summary>
    /// Retrieves a task by its identifier, if it exists.
    /// </summary>
    /// <param name="taskId">The identifier of the task to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task whose result is the task if found; otherwise, <c>null</c>.
    /// </returns>
    public Task<IAgentTask?> GetTaskAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource<IAgentTask?> tcs = new ();
            tcs.SetCanceled(cancellationToken);
            return tcs.Task;
        }

        if (string.IsNullOrWhiteSpace(taskId))
        {
            return Task.FromResult<IAgentTask?>(null);
        }

        lock (_syncRoot)
        {
            if (_tasks.TryGetValue(taskId, out IAgentTask? task) && task != null)
            {
                return Task.FromResult<IAgentTask?>(task);
            }
        }

        return Task.FromResult<IAgentTask?>(null);
    }

    /// <summary>
    /// Adds or updates a task in the store.
    /// </summary>
    /// <param name="task">The task to add or update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="task"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="IAgentTask.Id"/> or <see cref="IAgentTask.AgentId"/> is
    /// <c>null</c> or whitespace.
    /// </exception>
    public Task SaveTaskAsync(
        IAgentTask task,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource tcs = new ();
            tcs.SetCanceled(cancellationToken);
            return tcs.Task;
        }

        ArgumentNullException.ThrowIfNull(task);

        if (string.IsNullOrWhiteSpace(task.Id))
        {
            throw new ArgumentException(
                "Agent task must have a non-empty Id.", nameof(task));
        }

        if (string.IsNullOrWhiteSpace(task.AgentId))
        {
            throw new ArgumentException(
                "Agent task must have a non-empty AgentId.", nameof(task));
        }

        lock (_syncRoot)
        {
            _tasks[task.Id] = task;
        }

        return Task.CompletedTask;
    }
}
