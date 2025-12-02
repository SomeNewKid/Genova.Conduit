// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Chats;
using Genova.Conduit.Pipelines;
using Genova.Conduit.Steps;
using Genova.Conduit.Terminal.Steps;
using Genova.Conduit.Tools;
using Genova.Conduit.Utilities;

namespace Genova.Conduit.Terminal.Pipelines;

/// <summary>
/// Represents a pipeline that, given a user math question in the context,
/// uses an LLM to select an operation and operands, then invokes the
/// corresponding math tool and stores the numeric result in the context.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class MathOperationPipeline : IPipeline
{
    private const string OperationKey = "SelectedOperation";
    private const string OperandAKey = "OperandA";
    private const string OperandBKey = "OperandB";
    private const string ToolNameKey = "SelectedToolName";
    private const string ArgumentsKey = "SelectedToolArguments";

    private readonly IPipelineStep _selectionStep;
    private readonly IPipelineStep _invokeStep;

    /// <summary>
    /// Initializes a new instance of the <see cref="MathOperationPipeline"/> class.
    /// </summary>
    /// <param name="chatClient">
    /// The chat client used by the selection step to call the LLM.
    /// </param>
    /// <param name="toolRegistry">
    /// The tool registry used by the invocation step to resolve math tools.
    /// </param>
    /// <param name="questionKey">
    /// The key in <see cref="PipelineContext.Items"/> under which the user
    /// question is stored as a <see cref="string"/>.
    /// </param>
    /// <param name="resultKey">
    /// The key under which the numeric result of the math operation will be
    /// stored in the <see cref="PipelineContext"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chatClient"/> or <paramref name="toolRegistry"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="questionKey"/> or <paramref name="resultKey"/> is <c>null</c> or whitespace.
    /// </exception>
    public MathOperationPipeline(
        IChatClient chatClient,
        IToolRegistry toolRegistry,
        string questionKey,
        string resultKey)
    {
        ArgumentNullException.ThrowIfNull(chatClient);

        ArgumentNullException.ThrowIfNull(toolRegistry);

        if (string.IsNullOrWhiteSpace(questionKey))
        {
            throw new ArgumentException("Question key must be non-empty.", nameof(questionKey));
        }

        if (string.IsNullOrWhiteSpace(resultKey))
        {
            throw new ArgumentException("Result key must be non-empty.", nameof(resultKey));
        }

        _selectionStep = new MathOperationSelectionStep(
            chatClient,
            questionKey,
            OperationKey,
            OperandAKey,
            OperandBKey);

        _invokeStep = new InvokeDecidedToolStep(
            toolRegistry,
            ToolNameKey,
            ArgumentsKey,
            resultKey);
    }

    /// <summary>
    /// Executes the pipeline by:
    /// 1. Running the selection step (LLM-based operation classification).
    /// 2. Preparing the tool name and argument dictionary.
    /// 3. Invoking the selected tool using <see cref="InvokeDecidedToolStep"/>.
    /// </summary>
    /// <param name="context">The shared pipeline context.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no valid operation or operands are available for execution.
    /// </exception>
    public async Task ExecuteAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 1. Run the selection step.
        await _selectionStep.ExecuteAsync(context, cancellationToken)
            .ConfigureAwait(false);

        // 2. Read selected operation and operands from context.
        string? operation = context.GetItem<string>(OperationKey);

        if (string.IsNullOrWhiteSpace(operation) ||
            string.Equals(operation, "no_match", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "No valid math operation was selected for execution.");
        }

        int a = ContextHelper.GetInteger(context, OperandAKey, "Operand A");
        int b = ContextHelper.GetInteger(context, OperandBKey, "Operand B");

        // 3. Prepare tool name and arguments for InvokeDecidedToolStep.
        string toolName = operation.ToLowerInvariant();
        context.SetItem(ToolNameKey, toolName);

        IDictionary<string, object?> arguments =
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["a"] = a,
                ["b"] = b
            };

        context.SetItem(ArgumentsKey, arguments);

        // 4. Invoke the selected tool.
        await _invokeStep.ExecuteAsync(context, cancellationToken)
            .ConfigureAwait(false);

        // At this point, the tool result is stored under _resultKey.
    }
}
