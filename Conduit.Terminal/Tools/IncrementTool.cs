// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Conduit.Pipelines;
using Genova.Conduit.Tools;

namespace Genova.Conduit.Terminal.Tools;

/// <summary>
/// Represents a tool that increments an integer value by one.
/// </summary>
public sealed class IncrementTool : ITool
{
    /// <summary>
    /// Gets the tool name used to reference this tool.
    /// </summary>
    public string Name
    {
        get { return "increment"; }
    }

    /// <summary>
    /// Gets a human-readable description of what the tool does.
    /// </summary>
    public string Description
    {
        get { return "Increments the specified integer value by one and returns the result."; }
    }

    /// <summary>
    /// Invokes the increment tool using the specified arguments.
    /// Expects a single integer argument named 'value'.
    /// </summary>
    /// <param name="arguments">
    /// The invocation arguments, containing a 'value' entry of type <see cref="int"/>.
    /// </param>
    /// <param name="context">The pipeline context for the current execution (ignored).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task whose result is the incremented integer value.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the 'value' argument is missing or not an integer.
    /// </exception>
    public Task<object?> InvokeAsync(
        IDictionary<string, object?> arguments,
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (!arguments.TryGetValue("value", out object? rawValue) || rawValue == null)
        {
            throw new ArgumentException("Argument 'value' is required for the increment tool.");
        }

        if (rawValue is not int currentValue)
        {
            throw new ArgumentException("Argument 'value' must be an integer for the increment tool.");
        }

        int incremented = checked(currentValue + 1);
        return Task.FromResult<object?>(incremented);
    }
}
