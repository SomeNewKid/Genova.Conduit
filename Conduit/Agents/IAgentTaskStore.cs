// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Agents;

/// <summary>
/// Abstraction for storing and retrieving tasks to be processed by agents.
/// </summary>
public interface IAgentTaskStore
{
    /// <summary>
    /// Retrieves a collection of tasks that are ready to be processed by
    /// the specified agent.
    /// </summary>
    /// <param name="agentId">The identifier of the agent requesting tasks.</param>
    /// <param name="maxCount">The maximum number of tasks to return.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A list of tasks that are pending or otherwise ready to be processed.
    /// The list may be empty if no tasks are available.
    /// </returns>
    Task<IReadOnlyList<IAgentTask>> GetPendingTasksAsync(
        string agentId,
        int maxCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a task by its identifier, if it exists.
    /// </summary>
    /// <param name="taskId">The identifier of the task to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// The task if found; otherwise, <c>null</c>.
    /// </returns>
    Task<IAgentTask?> GetTaskAsync(
        string taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a task in the store.
    /// </summary>
    /// <param name="task">The task to add or update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveTaskAsync(
        IAgentTask task,
        CancellationToken cancellationToken = default);
}
