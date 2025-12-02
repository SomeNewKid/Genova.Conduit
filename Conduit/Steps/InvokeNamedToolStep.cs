// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Pipelines;
using Genova.Conduit.Tools;

namespace Genova.Conduit.Steps;

/// <summary>
/// Represents a pipeline step that invokes a specific tool by name using an
/// <see cref="IToolRegistry"/>, passing in arguments from the <see cref="PipelineContext"/>,
/// and storing the result back into the context under a caller-specified key.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class InvokeNamedToolStep : InvokeToolStepBase
{
    private readonly string _toolName;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvokeNamedToolStep"/> class.
    /// </summary>
    /// <param name="toolRegistry">
    /// The registry used to resolve the tool instance. Must not be <c>null</c>.
    /// </param>
    /// <param name="toolName">
    /// The name of the tool to invoke. Tool names are case-insensitive.
    /// Must not be <c>null</c> or whitespace.
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
    public InvokeNamedToolStep(
        IToolRegistry toolRegistry,
        string toolName,
        string argumentsKey,
        string resultKey)
        : base(toolRegistry, argumentsKey, resultKey)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("Tool name must be non-empty.", nameof(toolName));
        }

        _toolName = toolName;
    }

    /// <summary>
    /// Gets the tool name that this step always invokes.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>The fixed tool name provided in the constructor.</returns>
    protected override string? GetToolName(PipelineContext context)
    {
        return _toolName;
    }
}
