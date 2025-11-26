// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Agents;

/// <summary>
/// Represents an agent that can process tasks or events using one or more pipelines.
/// </summary>
/// <remarks>
/// Agents are long-lived logical entities whose state is persisted between
/// invocations. Host code is responsible for scheduling when an agent runs.
/// </remarks>
public interface IAgent
{
    /// <summary>
    /// Gets the unique identifier of the agent.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Executes a single unit of work for this agent using the specified
    /// <paramref name="context"/> and <paramref name="state"/>.
    /// </summary>
    /// <param name="context">
    /// A pipeline context that carries input, intermediate data, and output
    /// for this particular execution.
    /// </param>
    /// <param name="state">
    /// The current persistent state of the agent, which may be updated
    /// and subsequently stored by the orchestrator.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    Task RunAsync(
        PipelineContext context,
        AgentState state,
        CancellationToken cancellationToken = default);
}
