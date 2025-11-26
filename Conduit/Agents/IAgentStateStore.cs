// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Agents;

/// <summary>
/// Abstraction for persisting and retrieving agent state.
/// </summary>
public interface IAgentStateStore
{
    /// <summary>
    /// Retrieves the state for the agent with the specified <paramref name="agentId"/>.
    /// </summary>
    /// <param name="agentId">The agent identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The agent state if found; otherwise <c>null</c>.</returns>
    Task<AgentState?> GetAsync(
        string agentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the specified <paramref name="state"/>.
    /// </summary>
    /// <param name="state">The agent state to store.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    Task SaveAsync(
        AgentState state,
        CancellationToken cancellationToken = default);
}
