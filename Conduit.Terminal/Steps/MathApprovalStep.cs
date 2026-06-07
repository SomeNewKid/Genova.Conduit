// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Pipelines;
using Genova.Conduit.Tools;

namespace Genova.Conduit.Terminal.Steps;

/// <summary>
/// Represents a pipeline step that checks for human approval by verifying
/// the existence of a configured file path. The
/// result is written into the pipeline context as a boolean value.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class MathApprovalStep : IPipelineStep
{
    private const string FileExistsToolName = "fileExists";

    private readonly IToolRegistry _toolRegistry;
    private readonly string _approvalResultKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="MathApprovalStep"/> class.
    /// </summary>
    /// <param name="toolRegistry">
    /// The tool registry used to resolve the file-existence tool.
    /// </param>
    /// <param name="approvalResultKey">
    /// The key in <see cref="PipelineContext.Items"/> under which the boolean
    /// approval result will be stored.
    /// </param>
    /// <param name="approvalFilePath">
    /// The path of the file whose presence grants approval.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="toolRegistry"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="approvalResultKey"/> is <c>null</c> or whitespace.
    /// </exception>
    public MathApprovalStep(
        IToolRegistry toolRegistry,
        string approvalResultKey,
        string? approvalFilePath = null)
    {
        ArgumentNullException.ThrowIfNull(toolRegistry);

        if (string.IsNullOrWhiteSpace(approvalResultKey))
        {
            throw new ArgumentException("Approval result key must be non-empty.", nameof(approvalResultKey));
        }

        _toolRegistry = toolRegistry;
        _approvalResultKey = approvalResultKey;
        ApprovalFilePath = Path.GetFullPath(approvalFilePath ?? Path.Combine(AppContext.BaseDirectory, "Approval.txt"));
    }

    /// <summary>
    /// Gets the path of the file whose presence grants approval.
    /// </summary>
    public string ApprovalFilePath { get; }

    /// <summary>
    /// Executes the step by invoking the file-existence tool to check
    /// the configured approval file and writing the boolean result into
    /// the pipeline context.
    /// </summary>
    /// <param name="context">The shared pipeline context.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <c>null</c>.
    /// </exception>
    public async Task ExecuteAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_toolRegistry.TryGetTool(FileExistsToolName, out ITool? tool) || tool == null)
        {
            throw new InvalidOperationException(
                $"The '{FileExistsToolName}' tool is not registered in the tool registry.");
        }

        IDictionary<string, object?> arguments =
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["path"] = ApprovalFilePath
            };

        object? result =
            await tool.InvokeAsync(arguments, context, cancellationToken)
                .ConfigureAwait(false);

        bool approvalGranted = false;

        if (result is bool boolResult)
        {
            approvalGranted = boolResult;
        }

        context.SetItem(_approvalResultKey, approvalGranted);
    }
}
