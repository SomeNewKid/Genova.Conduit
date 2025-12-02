// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Pipelines;
using Genova.Conduit.Tools;

namespace Genova.Conduit.Steps;

/// <summary>
/// Represents a base class for pipeline steps that invoke tools via an
/// <see cref="IToolRegistry"/> using arguments from a <see cref="PipelineContext"/>
/// and storing the result back into the context.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public abstract class InvokeToolStepBase : IPipelineStep
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvokeToolStepBase"/> class.
    /// </summary>
    /// <param name="toolRegistry">
    /// The registry used to resolve tool instances. Must not be <c>null</c>.
    /// </param>
    /// <param name="argumentsKey">
    /// The key in the <see cref="PipelineContext.Items"/> dictionary whose value
    /// must be an <see cref="IDictionary{String, Object}"/> of tool arguments.
    /// Must not be <c>null</c> or whitespace.
    /// </param>
    /// <param name="resultKey">
    /// The key under which the result of the tool invocation will be stored
    /// in the <see cref="PipelineContext.Items"/> dictionary.
    /// Must not be <c>null</c> or whitespace.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="toolRegistry"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any string parameter is <c>null</c> or whitespace.
    /// </exception>
    protected InvokeToolStepBase(
        IToolRegistry toolRegistry,
        string argumentsKey,
        string resultKey)
    {
        ArgumentNullException.ThrowIfNull(toolRegistry);

        if (string.IsNullOrWhiteSpace(argumentsKey))
        {
            throw new ArgumentException("Arguments key must be non-empty.", nameof(argumentsKey));
        }

        if (string.IsNullOrWhiteSpace(resultKey))
        {
            throw new ArgumentException("Result key must be non-empty.", nameof(resultKey));
        }

        ToolRegistry = toolRegistry;
        ArgumentsKey = argumentsKey;
        ResultKey = resultKey;
    }

    /// <summary>
    /// Gets the tool registry used to resolve tools by name.
    /// </summary>
    protected IToolRegistry ToolRegistry { get; }

    /// <summary>
    /// Gets the context key that identifies the tool arguments.
    /// </summary>
    protected string ArgumentsKey { get; }

    /// <summary>
    /// Gets the context key that identifies where the tool result is stored.
    /// </summary>
    protected string ResultKey { get; }

    /// <summary>
    /// Executes the step by resolving the tool, reading arguments, invoking the tool,
    /// and storing the result into the context.
    /// </summary>
    /// <param name="context">The shared pipeline context.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? toolName = GetToolName(context);
        if (string.IsNullOrWhiteSpace(toolName))
        {
            OnNoToolSelected(context);
            return;
        }

        if (!ToolRegistry.TryGetTool(toolName, out ITool? tool) || tool == null)
        {
            throw new InvalidOperationException(
                $"Tool '{toolName}' was not found in the registry.");
        }

        IDictionary<string, object?> arguments = GetArguments(context);

        object? result = await tool.InvokeAsync(
            arguments,
            context,
            cancellationToken).ConfigureAwait(false);

        context.SetItem(ResultKey, result);
    }

    /// <summary>
    /// Gets the tool name to invoke for the current context.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>The tool name, or <c>null</c> if no tool should be invoked.</returns>
    protected abstract string? GetToolName(PipelineContext context);

    /// <summary>
    /// Invoked when <see cref="GetToolName"/> returns <c>null</c> or whitespace.
    /// The default implementation does nothing.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    protected virtual void OnNoToolSelected(PipelineContext context)
    {
        // Default is to do nothing when no tool is selected.
    }

    /// <summary>
    /// Gets the arguments for the tool invocation from the context.
    /// The default implementation expects an <see cref="IDictionary{String, Object}"/>
    /// stored under <see cref="ArgumentsKey"/>.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>A dictionary of arguments for the tool.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the arguments are missing or not of the expected type.
    /// </exception>
    protected virtual IDictionary<string, object?> GetArguments(PipelineContext context)
    {
        object? rawArgs = context.GetItem<object>(ArgumentsKey);
        if (rawArgs == null)
        {
            throw new InvalidOperationException(
                $"Pipeline context does not contain arguments under key '{ArgumentsKey}'.");
        }

        if (rawArgs is not IDictionary<string, object?> args)
        {
            throw new InvalidOperationException(
                $"Context item '{ArgumentsKey}' is not an IDictionary<string, object?>.");
        }

        return args;
    }
}
