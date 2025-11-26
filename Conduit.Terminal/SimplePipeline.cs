// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Terminal;

/// <summary>
/// A minimal sequential pipeline implementation that executes a fixed set of steps.
/// </summary>
public sealed class SimplePipeline : IPipeline
{
    private readonly IReadOnlyList<IPipelineStep> _steps;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimplePipeline"/> class with the specified steps.
    /// </summary>
    /// <param name="steps">
    /// The steps to be executed in the pipeline.
    /// </param>
    public SimplePipeline(params IPipelineStep[] steps)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        foreach (IPipelineStep step in _steps)
        {
            await step.ExecuteAsync(context, cancellationToken);
        }
    }
}
