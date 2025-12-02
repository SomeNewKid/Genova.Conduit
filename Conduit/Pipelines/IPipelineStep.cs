// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Pipelines;

/// <summary>
/// Represents a single, deterministic unit of work within a pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Implementations must be execution-agnostic. A step should not assume
/// whether it is being invoked from a local one-shot app, a long-running agent,
/// or a cloud callback. All necessary information should be provided via
/// <see cref="PipelineContext"/> and injected services.
/// </para>
/// </remarks>
public interface IPipelineStep
{
    /// <summary>
    /// Executes the step against the specified <paramref name="context"/>.
    /// </summary>
    /// <param name="context">
    /// The context instance that carries input, intermediate data,
    /// and output across the pipeline.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that may be used to observe cancellation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default);
}
