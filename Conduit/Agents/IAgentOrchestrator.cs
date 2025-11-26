// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Agents;

/// <summary>
/// Coordinates the execution of one or more agents, including loading
/// and saving state and selecting which pipelines or steps to run.
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Executes a single orchestration cycle for the agent with the given identifier.
    /// </summary>
    /// <param name="agentId">The identifier of the agent to orchestrate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task that completes when the orchestration cycle has finished.
    /// </returns>
    Task RunOnceAsync(
        string agentId,
        CancellationToken cancellationToken = default);
}
