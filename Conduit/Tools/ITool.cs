// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Conduit.Pipelines;

namespace Genova.Conduit.Tools;

/// <summary>
/// Represents a tool that can be invoked by a model or agent to perform
/// an external action (e.g., API call, database query, file operation).
/// </summary>
public interface ITool
{
    /// <summary>
    /// Gets the tool name, used to reference the tool from prompts or
    /// tool-calling metadata.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a human-readable description of what the tool does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Invokes the tool using the specified arguments and pipeline context.
    /// </summary>
    /// <param name="arguments">Tool-specific invocation arguments.</param>
    /// <param name="context">
    /// The pipeline context for the current execution, which may be
    /// used to read or update shared state.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task that completes when the tool has finished executing.
    /// Implementations may return a structured result or store
    /// results in the <paramref name="context"/>.
    /// </returns>
    Task<object?> InvokeAsync(
        IDictionary<string, object?> arguments,
        PipelineContext context,
        CancellationToken cancellationToken = default);
}
