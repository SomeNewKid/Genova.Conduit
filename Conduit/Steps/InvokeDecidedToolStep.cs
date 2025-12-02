// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Pipelines;
using Genova.Conduit.Tools;

namespace Genova.Conduit.Steps;

/// <summary>
/// Represents a pipeline step that reads a tool name and argument dictionary
/// from the <see cref="PipelineContext"/>, resolves the tool using an
/// <see cref="IToolRegistry"/>, invokes the tool, and stores the result
/// back into the context.
/// </summary>
/// <remarks>
/// This step is intended to be used after a model-driven decision step
/// such as <see cref="DecideToolStep"/>, which populates the tool name
/// and arguments in the context.
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class InvokeDecidedToolStep : InvokeToolStepBase
{
    private readonly string _toolNameKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvokeDecidedToolStep"/> class.
    /// </summary>
    /// <param name="toolRegistry">
    /// The registry used to resolve the tool instance by name.
    /// </param>
    /// <param name="toolNameKey">
    /// The key in <see cref="PipelineContext.Items"/> containing the selected
    /// tool name as a <see cref="string"/>.
    /// </param>
    /// <param name="argumentsKey">
    /// The key in <see cref="PipelineContext.Items"/> containing the selected
    /// tool arguments as an <see cref="IDictionary{String, Object}"/>.
    /// </param>
    /// <param name="resultKey">
    /// The key under which the result of the tool invocation will be stored
    /// in <see cref="PipelineContext.Items"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="toolRegistry"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any string parameter is <c>null</c> or whitespace.
    /// </exception>
    public InvokeDecidedToolStep(
        IToolRegistry toolRegistry,
        string toolNameKey,
        string argumentsKey,
        string resultKey)
        : base(toolRegistry, argumentsKey, resultKey)
    {
        ArgumentNullException.ThrowIfNull(toolRegistry);

        if (string.IsNullOrWhiteSpace(toolNameKey))
        {
            throw new ArgumentException("Tool name key must be non-empty.", nameof(toolNameKey));
        }

        _toolNameKey = toolNameKey;
    }

    /// <summary>
    /// Gets the tool name from the pipeline context.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>
    /// The selected tool name from the context, or <c>null</c> if no tool was selected.
    /// </returns>
    protected override string? GetToolName(PipelineContext context)
    {
        string? selectedToolName = context.GetItem<string>(_toolNameKey);
        return selectedToolName;
    }

    /// <summary>
    /// Called when no tool name is present in the context. This implementation
    /// performs no action, allowing the pipeline to continue without invoking a tool.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    protected override void OnNoToolSelected(PipelineContext context)
    {
        // Intentionally do nothing when no tool is selected.
    }

    /// <summary>
    /// Gets the arguments for the selected tool from the context.
    /// If no arguments are present, an empty dictionary is returned.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>A dictionary of arguments for the tool.</returns>
    protected override IDictionary<string, object?> GetArguments(PipelineContext context)
    {
        object? rawArguments = context.GetItem<object>(ArgumentsKey);
        if (rawArguments == null)
        {
            return new Dictionary<string, object?>();
        }

        if (rawArguments is not IDictionary<string, object?> cast)
        {
            throw new InvalidOperationException(
                $"Context item '{ArgumentsKey}' is not an IDictionary<string, object?>.");
        }

        return cast;
    }
}
