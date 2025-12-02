// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Pipelines;

namespace Genova.Conduit.Agents;

/// <summary>
/// Represents a simple implementation of <see cref="IAgentOrchestrator"/> that
/// coordinates agent execution by loading state, preparing a pipeline context,
/// invoking the agent, persisting updated state, and returning the result of
/// the agent's run cycle.
/// </summary>
/// <remarks>
/// This orchestrator assumes that agents are identified by unique identifiers
/// and that agent state is stored and retrieved using an <see cref="IAgentStateStore"/>.
/// It does not perform scheduling; the host application is responsible for calling
/// <see cref="RunOnceAsync"/> as often as needed.
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class BasicAgentOrchestrator : IAgentOrchestrator
{
    private readonly IAgentStateStore _stateStore;
    private readonly IReadOnlyDictionary<string, IAgent> _agents;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicAgentOrchestrator"/> class.
    /// </summary>
    /// <param name="stateStore">
    /// The state store used to persist and retrieve agent state.
    /// </param>
    /// <param name="agents">
    /// A dictionary of agents keyed by their identifiers.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stateStore"/> or <paramref name="agents"/> is <c>null</c>.
    /// </exception>
    public BasicAgentOrchestrator(
        IAgentStateStore stateStore,
        IReadOnlyDictionary<string, IAgent> agents)
    {
        ArgumentNullException.ThrowIfNull(stateStore);

        ArgumentNullException.ThrowIfNull(agents);

        _stateStore = stateStore;
        _agents = agents;
    }

    /// <summary>
    /// Executes one cycle of work for the agent identified by <paramref name="agentId"/>.
    /// This includes loading the agent's state, creating a pipeline context,
    /// invoking the agent, persisting updated state, and returning the result.
    /// </summary>
    /// <param name="agentId">The identifier of the agent to run.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>
    /// A task whose result is an <see cref="AgentRunResult"/> describing the outcome
    /// of the agent's run cycle.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="agentId"/> is <c>null</c> or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no agent is registered with the specified <paramref name="agentId"/>.
    /// </exception>
    public async Task<AgentRunResult> RunOnceAsync(
        string agentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("Agent identifier must be non-empty.", nameof(agentId));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        IAgent agent = GetAgent(agentId);

        AgentState? state =
            await _stateStore.GetAsync(agentId, cancellationToken)
                .ConfigureAwait(false);

        state ??= new AgentState
            {
                AgentId = agentId,
            };

        PipelineContext context = new (ExecutionEnvironment.AgentLocal);

        AgentRunResult agentResult;

        try
        {
            agentResult =
                await agent.RunAsync(context, state, cancellationToken)
                    .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Respect cooperative cancellation.
            throw;
        }
        catch (Exception ex)
        {
            // Convert unexpected exceptions into a failed run result.
            AgentRunResult failed = new ()
            {
                Status = AgentRunStatus.Failed,
                Message = ex.Message,
            };

            return failed;
        }

        await _stateStore.SaveAsync(state, cancellationToken)
            .ConfigureAwait(false);

        return agentResult;
    }

    private IAgent GetAgent(string agentId)
    {
        if (!_agents.TryGetValue(agentId, out IAgent? agent) || agent == null)
        {
            throw new InvalidOperationException(
                $"No agent is registered with the identifier '{agentId}'.");
        }

        return agent;
    }
}
