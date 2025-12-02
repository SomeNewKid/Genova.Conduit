// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Genova.Common.Attributes;
using Genova.Conduit.Pipelines;

namespace Genova.Conduit.Agents;

/// <summary>
/// Represents an autonomous agent capable of performing work over time.
/// An agent receives a <see cref="PipelineContext"/>, its persisted
/// <see cref="AgentState"/>, performs one cycle of work, and returns an
/// <see cref="AgentRunResult"/> indicating the outcome.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Gets the unique identifier for the agent.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Executes a single cycle of agent work using the provided
    /// <see cref="PipelineContext"/> and persisted <see cref="AgentState"/>.
    /// The agent updates the state and returns an <see cref="AgentRunResult"/>
    /// indicating whether it has completed work for the cycle, is waiting on
    /// external events, or has encountered a failure.
    /// </summary>
    /// <param name="context">The pipeline context for this agent run.</param>
    /// <param name="state">The persisted agent state.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>
    /// A task whose result is an <see cref="AgentRunResult"/> describing the outcome.
    /// </returns>
    Task<AgentRunResult> RunAsync(
        PipelineContext context,
        AgentState state,
        CancellationToken cancellationToken = default);
}
