// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Pipelines;

namespace Genova.Conduit.Tools;

/// <summary>
/// Represents a tool that checks whether a specified file exists.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class FileExistsTool : ITool
{
    /// <summary>
    /// Gets the tool name used to reference this tool.
    /// </summary>
    public string Name
    {
        get { return "fileExists"; }
    }

    /// <summary>
    /// Gets a human-readable description of what the tool does.
    /// </summary>
    public string Description
    {
        get { return "Checks whether the file at the specified path exists and returns a boolean result."; }
    }

    /// <summary>
    /// Invokes the file-existence tool using the specified arguments.
    /// Expects a single string argument named 'path'.
    /// </summary>
    /// <param name="arguments">
    /// The invocation arguments, containing a 'path' entry of type <see cref="string"/>.
    /// </param>
    /// <param name="context">The pipeline context for the current execution (unused).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task whose result is a <see cref="bool"/> indicating whether the file exists.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the 'path' argument is missing or not a string.
    /// </exception>
    public Task<object?> InvokeAsync(
        IDictionary<string, object?> arguments,
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (!arguments.TryGetValue("path", out object? rawPath) || rawPath == null)
        {
            throw new ArgumentException("Argument 'path' is required for the fileExists tool.");
        }

        if (rawPath is not string path || string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Argument 'path' must be a non-empty string for the fileExists tool.");
        }

        bool exists = File.Exists(path);
        return Task.FromResult<object?>(exists);
    }
}
