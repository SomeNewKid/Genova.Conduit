// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Pipelines;

/// <summary>
/// Represents a pipeline that executes a sequence or graph of reusable steps
/// over a shared <see cref="PipelineContext"/>.
/// </summary>
/// <remarks>
/// Pipelines are execution-agnostic: they may be invoked from local apps,
/// long-running agents, or remote callbacks (e.g., OpenAI tool calls).
/// Host code is responsible for constructing the context and invoking the pipeline.
/// </remarks>
public interface IPipeline
{
    /// <summary>
    /// Executes the pipeline against the specified <paramref name="context"/>.
    /// </summary>
    /// <param name="context">
    /// The mutable context shared across all steps in the pipeline.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that may be used to observe cancellation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default);
}
