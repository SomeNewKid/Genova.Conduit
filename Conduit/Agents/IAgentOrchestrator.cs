// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Agents;

/// <summary>
/// Represents a coordinator responsible for executing a single run cycle
/// of an agent. The orchestrator loads the agent's state, prepares any
/// required context, invokes the agent, persists updated state, and
/// returns the <see cref="AgentRunResult"/> to the caller.
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Executes one cycle of work for the agent identified by <paramref name="agentId"/>.
    /// This includes loading and persisting state before and after the agent run.
    /// </summary>
    /// <param name="agentId">The identifier of the agent to run.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>
    /// A task whose result is an <see cref="AgentRunResult"/> describing the outcome
    /// of the agent's run cycle.
    /// </returns>
    Task<AgentRunResult> RunOnceAsync(
        string agentId,
        CancellationToken cancellationToken = default);
}
