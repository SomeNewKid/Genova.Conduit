// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Conduit.Pipelines;
using Genova.Conduit.Tools;
using Genova.Conduit.Utilities;

namespace Genova.Conduit.Terminal.Tools;

/// <summary>
/// Represents a tool that performs integer division using two integer inputs.
/// </summary>
public sealed class DivisionTool : ITool
{
    /// <summary>
    /// Gets the unique tool name.
    /// </summary>
    public string Name
    {
        get { return "division"; }
    }

    /// <summary>
    /// Gets a human-readable description of the tool.
    /// </summary>
    public string Description
    {
        get { return "Divides integer a by integer b and returns the quotient."; }
    }

    /// <summary>
    /// Invokes the division tool with two integer arguments: 'a' and 'b'.
    /// </summary>
    /// <param name="arguments">Invocation arguments containing 'a' and 'b'.</param>
    /// <param name="context">The pipeline context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The integer result of a / b.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when arguments do not contain two integer values or when b is zero.
    /// </exception>
    public Task<object?> InvokeAsync(
        IDictionary<string, object?> arguments,
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        int a = ContextHelper.GetInteger(arguments, "a");
        int b = ContextHelper.GetInteger(arguments, "b");

        if (b == 0)
        {
            throw new ArgumentException("Division by zero is not allowed.");
        }

        int result = a / b;
        return Task.FromResult<object?>(result);
    }
}
