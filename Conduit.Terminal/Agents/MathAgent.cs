// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Agents;
using Genova.Conduit.Chats;
using Genova.Conduit.Pipelines;
using Genova.Conduit.Terminal.Pipelines;
using Genova.Conduit.Terminal.Steps;
using Genova.Conduit.Tools;

namespace Genova.Conduit.Terminal.Agents;

/// <summary>
/// Represents a math agent that:
/// 1. Uses an internal pipeline to interpret a user question, select a math
///    operation and operands, and invoke the corresponding math tool.
/// 2. Iteratively increments from 0 until it reaches the computed result,
///    delegating the increment workload to a tool.
/// 3. Waits for human approval, implemented by waiting for a file at
///    C:\Temp\Approval.txt to exist, using a tool and an approval step.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class MathAgent : IAgent
{
    // Agent phase keys and values.
    private const string PhaseKey = "Phase";
    private const string PhaseMathPending = "MathPending";
    private const string PhaseIncrementPending = "IncrementPending";
    private const string PhaseApprovalPending = "ApprovalPending";
    private const string PhaseFinalAnswerPending = "FinalAnswerPending";
    private const string PhaseCompleted = "Completed";

    // State keys for data stored in AgentState.Data.
    private const string UserQuestionStateKey = "UserQuestion";
    private const string FinalAnswerStateKey = "FinalAnswer";
    private const string MathResultStateKey = "MathResult";
    private const string IncrementCurrentStateKey = "IncrementCurrent";
    private const string IncrementAttemptsStateKey = "IncrementAttempts";

    // Pipeline context keys shared with pipelines and steps.
    private const string QuestionContextKey = "UserQuestion";
    private const string ResultContextKey = "MathResult";
    private const string ApprovalResultContextKey = "ApprovalGranted";
    private const string FinalAnswerContextKey = "FinalAnswerText";

    private const int MaxIncrementAttempts = 1000;

    private readonly IPipeline _mathPipeline;
    private readonly IToolRegistry _toolRegistry;
    private readonly IPipelineStep _approvalStep;
    private readonly IPipeline _finalAnswerPipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="MathAgent"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="chatClient">
    /// The chat client used by the internal math pipeline to call the LLM.
    /// </param>
    /// <param name="toolRegistry">
    /// The tool registry used by the internal math pipeline and tools.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is null or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chatClient"/> or <paramref name="toolRegistry"/> is null.
    /// </exception>
    public MathAgent(
        string id,
        IChatClient chatClient,
        IToolRegistry toolRegistry)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Agent id must be non-empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(chatClient);

        ArgumentNullException.ThrowIfNull(toolRegistry);

        Id = id;
        _toolRegistry = toolRegistry;

        _mathPipeline = new MathOperationPipeline(
            chatClient,
            toolRegistry,
            QuestionContextKey,
            ResultContextKey);

        _approvalStep = new MathApprovalStep(
            toolRegistry,
            ApprovalResultContextKey);

        _finalAnswerPipeline = new MathAnswerPipeline(
            chatClient,
            QuestionContextKey,
            ResultContextKey,
            FinalAnswerContextKey);
    }

    /// <summary>
    /// Gets the unique identifier for the agent.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Executes a single agent cycle based on the current phase.
    /// </summary>
    public async Task<AgentRunResult> RunAsync(
        PipelineContext context,
        AgentState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        ArgumentNullException.ThrowIfNull(state);

        string phase = GetCurrentPhase(state);

        return phase switch
        {
            PhaseMathPending =>
                await RunMathPhaseAsync(context, state, cancellationToken)
                      .ConfigureAwait(false),

            PhaseIncrementPending =>
                await RunIncrementPhaseAsync(context, state, cancellationToken)
                      .ConfigureAwait(false),

            PhaseApprovalPending =>
                await RunApprovalPhaseAsync(context, state, cancellationToken)
                      .ConfigureAwait(false),

            PhaseFinalAnswerPending =>
                await RunFinalAnswerPhaseAsync(context, state, cancellationToken)
                      .ConfigureAwait(false),

            PhaseCompleted => new AgentRunResult
            {
                Status = AgentRunStatus.Completed,
                Message = "MathAgent has already completed its work."
            },

            _ => new AgentRunResult
            {
                Status = AgentRunStatus.Failed,
                Message = $"MathAgent encountered an unknown phase '{phase}'."
            },
        };
    }

    private static string GetCurrentPhase(AgentState state)
    {
        if (state.Data.TryGetValue(PhaseKey, out object? rawPhase) &&
            rawPhase is string phaseString &&
            !string.IsNullOrWhiteSpace(phaseString))
        {
            return phaseString;
        }

        return PhaseMathPending;
    }

    private static string? GetUserQuestionFromState(AgentState state)
    {
        if (!state.Data.TryGetValue(UserQuestionStateKey, out object? rawQuestion) ||
            rawQuestion == null ||
            rawQuestion is not string question ||
            string.IsNullOrWhiteSpace(question))
        {
            return null;
        }

        return question;
    }

    private async Task<AgentRunResult> RunMathPhaseAsync(
        PipelineContext context,
        AgentState state,
        CancellationToken cancellationToken)
    {
        string? question = GetUserQuestionFromState(state);

        if (string.IsNullOrWhiteSpace(question))
        {
            return new AgentRunResult
            {
                Status = AgentRunStatus.Failed,
                Message = $"AgentState.Data['{UserQuestionStateKey}'] is missing or invalid."
            };
        }

        context.SetItem(QuestionContextKey, question);

        try
        {
            await _mathPipeline.ExecuteAsync(context, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new AgentRunResult
            {
                Status = AgentRunStatus.Failed,
                Message = $"Math operation pipeline failed: {ex.Message}"
            };
        }

        object? rawResult = context.GetItem<object>(ResultContextKey);

        if (rawResult == null || rawResult is not int resultValue)
        {
            return new AgentRunResult
            {
                Status = AgentRunStatus.Failed,
                Message = $"MathResult was not produced by the math pipeline under key '{ResultContextKey}'."
            };
        }

        state.Data[MathResultStateKey] = resultValue;
        state.Data[IncrementCurrentStateKey] = 0;
        state.Data[IncrementAttemptsStateKey] = 0;
        state.Data[PhaseKey] = PhaseIncrementPending;

        return new AgentRunResult
        {
            Status = AgentRunStatus.PendingExternalEvents,
            Message = $"Math result computed: {resultValue}. Beginning increment phase."
        };
    }

    private async Task<AgentRunResult> RunIncrementPhaseAsync(
        PipelineContext context,
        AgentState state,
        CancellationToken cancellationToken)
    {
        if (!state.Data.TryGetValue(MathResultStateKey, out object? rawTarget) ||
            rawTarget == null ||
            rawTarget is not int targetValue)
        {
            return new AgentRunResult
            {
                Status = AgentRunStatus.Failed,
                Message = $"AgentState.Data['{MathResultStateKey}'] is missing or invalid."
            };
        }

        int currentValue = 0;
        if (state.Data.TryGetValue(IncrementCurrentStateKey, out object? rawCurrent) &&
            rawCurrent is int currentInt)
        {
            currentValue = currentInt;
        }

        int attempts = 0;
        if (state.Data.TryGetValue(IncrementAttemptsStateKey, out object? rawAttempts) &&
            rawAttempts is int attemptsInt)
        {
            attempts = attemptsInt;
        }

        attempts++;
        if (attempts > MaxIncrementAttempts)
        {
            state.Data[PhaseKey] = PhaseCompleted;

            return new AgentRunResult
            {
                Status = AgentRunStatus.Failed,
                Message = $"Increment phase exceeded maximum attempts ({MaxIncrementAttempts})."
            };
        }

        int newCurrent;
        try
        {
            newCurrent = await InvokeIncrementToolAsync(
                    context,
                    currentValue,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new AgentRunResult
            {
                Status = AgentRunStatus.Failed,
                Message = $"Increment tool failed: {ex.Message}"
            };
        }

        state.Data[IncrementCurrentStateKey] = newCurrent;
        state.Data[IncrementAttemptsStateKey] = attempts;

        if (newCurrent == targetValue)
        {
            state.Data[PhaseKey] = PhaseApprovalPending;

            return new AgentRunResult
            {
                Status = AgentRunStatus.PendingExternalEvents,
                Message = $"Increment reached target {targetValue} after {attempts} step(s). Awaiting approval."
            };
        }

        string progressMessage =
            $"Incrementing... current={newCurrent}, target={targetValue}, attempts={attempts}.";

        return new AgentRunResult
        {
            Status = AgentRunStatus.PendingExternalEvents,
            Message = progressMessage
        };
    }

    private async Task<AgentRunResult> RunApprovalPhaseAsync(
        PipelineContext context,
        AgentState state,
        CancellationToken cancellationToken)
    {
        // Run approval step (which uses FileExistsTool)
        try
        {
            await _approvalStep.ExecuteAsync(context, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new AgentRunResult
            {
                Status = AgentRunStatus.Failed,
                Message = $"Approval step failed: {ex.Message}"
            };
        }

        bool approvalGranted = context.GetItem<bool>(ApprovalResultContextKey);

        if (!approvalGranted)
        {
            return new AgentRunResult
            {
                Status = AgentRunStatus.PendingExternalEvents,
                Message = $"Awaiting approval: create C:\\Temp\\Approval.txt to approve the result."
            };
        }

        // Approval granted — proceed to final answer phase
        state.Data[PhaseKey] = PhaseFinalAnswerPending;

        return new AgentRunResult
        {
            Status = AgentRunStatus.PendingExternalEvents,
            Message = "Approval received. Preparing final answer."
        };
    }


    private async Task<AgentRunResult> RunFinalAnswerPhaseAsync(
        PipelineContext context,
        AgentState state,
        CancellationToken cancellationToken)
    {
        // We need both the question and the math result in the context.
        string? question = GetUserQuestionFromState(state);

        if (string.IsNullOrWhiteSpace(question))
        {
            return new AgentRunResult
            {
                Status = AgentRunStatus.Failed,
                Message = $"AgentState.Data['{UserQuestionStateKey}'] is missing or invalid at final answer phase."
            };
        }

        if (!state.Data.TryGetValue(MathResultStateKey, out object? rawTarget) ||
            rawTarget == null ||
            rawTarget is not int targetValue)
        {
            return new AgentRunResult
            {
                Status = AgentRunStatus.Failed,
                Message = $"AgentState.Data['{MathResultStateKey}'] is missing or invalid at final answer phase."
            };
        }

        // Populate the pipeline context for the final answer pipeline.
        context.SetItem(QuestionContextKey, question);
        context.SetItem(ResultContextKey, targetValue);

        try
        {
            await _finalAnswerPipeline.ExecuteAsync(context, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new AgentRunResult
            {
                Status = AgentRunStatus.Failed,
                Message = $"Final answer pipeline failed: {ex.Message}"
            };
        }

        string? finalAnswer = context.GetItem<string>(FinalAnswerContextKey);

        if (string.IsNullOrWhiteSpace(finalAnswer))
        {
            finalAnswer = $"The answer is {targetValue}.";
        }

        state.Data[FinalAnswerStateKey] = finalAnswer;
        state.Data[PhaseKey] = PhaseCompleted;

        return new AgentRunResult
        {
            Status = AgentRunStatus.Completed,
            Message = "Final answer generated successfully."
        };
    }

    private async Task<int> InvokeIncrementToolAsync(
        PipelineContext context,
        int currentValue,
        CancellationToken cancellationToken)
    {
        if (!_toolRegistry.TryGetTool("increment", out ITool? tool) || tool == null)
        {
            throw new InvalidOperationException(
                "The 'increment' tool is not registered in the tool registry.");
        }

        IDictionary<string, object?> arguments =
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["value"] = currentValue
            };

        object? result =
            await tool.InvokeAsync(arguments, context, cancellationToken)
                .ConfigureAwait(false);

        if (result == null || result is not int incremented)
        {
            throw new InvalidOperationException(
                "The increment tool did not return a valid integer result.");
        }

        return incremented;
    }
}
