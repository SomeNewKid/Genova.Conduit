// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Conduit.Pipelines;
using Genova.Conduit.Tools;
using Genova.Conduit.Utilities;

namespace Genova.Conduit.Terminal.Tools;

/// <summary>
/// Represents a tool that performs integer addition using two integer inputs.
/// </summary>
public sealed class AdditionTool : ITool
{
    /// <summary>
    /// Gets the unique tool name.
    /// </summary>
    public string Name
    {
        get { return "addition"; }
    }

    /// <summary>
    /// Gets a human-readable description of the tool.
    /// </summary>
    public string Description
    {
        get { return "Adds two integer values and returns the sum."; }
    }

    /// <summary>
    /// Invokes the addition tool with two integer arguments: 'a' and 'b'.
    /// </summary>
    /// <param name="arguments">Invocation arguments containing 'a' and 'b'.</param>
    /// <param name="context">The pipeline context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The integer sum of a and b.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when arguments do not contain two integer values.
    /// </exception>
    public Task<object?> InvokeAsync(
        IDictionary<string, object?> arguments,
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        int a = ContextHelper.GetInteger(arguments, "a");
        int b = ContextHelper.GetInteger(arguments, "b");

        int result = a + b;
        return Task.FromResult<object?>(result);
    }
}
