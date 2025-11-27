// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Tools;

namespace Genova.Conduit.Steps;

/// <summary>
/// Represents a pipeline step that invokes a tool by name using an <see cref="IToolRegistry"/>,
/// passing in arguments from the <see cref="PipelineContext"/>, and storing the result
/// back into the context under a caller-specified key.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class InvokeToolStep : IPipelineStep
{
    private readonly IToolRegistry _toolRegistry;
    private readonly string _toolName;
    private readonly string _argumentsKey;
    private readonly string _resultKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvokeToolStep"/> class.
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
    /// must be an <see cref="IDictionary{String,Object}"/> of tool arguments.
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
    public InvokeToolStep(
        IToolRegistry toolRegistry,
        string toolName,
        string argumentsKey,
        string resultKey)
    {
        ArgumentNullException.ThrowIfNull(toolRegistry);

        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("Tool name must be non-empty.", nameof(toolName));
        }

        if (string.IsNullOrWhiteSpace(argumentsKey))
        {
            throw new ArgumentException("Arguments key must be non-empty.", nameof(argumentsKey));
        }

        if (string.IsNullOrWhiteSpace(resultKey))
        {
            throw new ArgumentException("Result key must be non-empty.", nameof(resultKey));
        }

        _toolRegistry = toolRegistry;
        _toolName = toolName;
        _argumentsKey = argumentsKey;
        _resultKey = resultKey;
    }

    /// <summary>
    /// Executes the step by looking up the tool, retrieving arguments from the context,
    /// invoking the tool, and storing the result back into the context.
    /// </summary>
    /// <param name="context">
    /// The shared context for the current pipeline execution.
    /// Must contain an entry under <see cref="_argumentsKey"/> whose value is
    /// an <see cref="IDictionary{String,Object}"/> representing tool arguments.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that may be used to observe cancellation.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the tool cannot be found or when the context does not contain
    /// arguments of the expected type.
    /// </exception>
    public async Task ExecuteAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_toolRegistry.TryGetTool(_toolName, out ITool? tool) || tool == null)
        {
            throw new InvalidOperationException(
                $"Tool '{_toolName}' was not found in the registry.");
        }

        object? rawArgs = context.GetItem<object>(_argumentsKey);
        if (rawArgs == null)
        {
            throw new InvalidOperationException(
                $"Pipeline context does not contain arguments under key '{_argumentsKey}'.");
        }

        if (rawArgs is not IDictionary<string, object?> args)
        {
            throw new InvalidOperationException(
                $"Context item '{_argumentsKey}' is not an IDictionary<string, object?>.");
        }

        object? result = await tool.InvokeAsync(
            args,
            context,
            cancellationToken).ConfigureAwait(false);

        context.SetItem(_resultKey, result);
    }
}
