// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Genova.Common.Attributes;
using Genova.Conduit.Chats;
using Genova.Conduit.Pipelines;
using Genova.Conduit.Terminal.Steps;

namespace Genova.Conduit.Terminal.Pipelines;

/// <summary>
/// Represents a pipeline that generates a user-friendly final answer
/// using the LLM, based on the original question and numeric result
/// stored in the <see cref="PipelineContext"/>.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1859:Use concrete types when possible for improved performance",
    Justification = "Using interfaces expected by artificial intelligence code generation.")]
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class MathAnswerPipeline : IPipeline
{
    private readonly IPipelineStep _finalAnswerStep;

    /// <summary>
    /// Initializes a new instance of the <see cref="MathAnswerPipeline"/> class.
    /// </summary>
    /// <param name="chatClient">
    /// The chat client used by the final answer step to call the LLM.
    /// </param>
    /// <param name="questionKey">
    /// The key in <see cref="PipelineContext.Items"/> that contains the user question.
    /// </param>
    /// <param name="resultKey">
    /// The key in <see cref="PipelineContext.Items"/> that contains the numeric result.
    /// </param>
    /// <param name="finalAnswerKey">
    /// The key under which the generated final answer will be stored.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chatClient"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any key parameter is <c>null</c> or whitespace.
    /// </exception>
    public MathAnswerPipeline(
        IChatClient chatClient,
        string questionKey,
        string resultKey,
        string finalAnswerKey)
    {
        ArgumentNullException.ThrowIfNull(chatClient);

        if (string.IsNullOrWhiteSpace(questionKey))
        {
            throw new ArgumentException("Question key must be non-empty.", nameof(questionKey));
        }

        if (string.IsNullOrWhiteSpace(resultKey))
        {
            throw new ArgumentException("Result key must be non-empty.", nameof(resultKey));
        }

        if (string.IsNullOrWhiteSpace(finalAnswerKey))
        {
            throw new ArgumentException("Final answer key must be non-empty.", nameof(finalAnswerKey));
        }

        _finalAnswerStep = new MathAnswerStep(
            chatClient,
            questionKey,
            resultKey,
            finalAnswerKey);
    }

    /// <summary>
    /// Executes the final answer step.
    /// </summary>
    /// <param name="context">The shared pipeline context.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    public async Task ExecuteAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _finalAnswerStep.ExecuteAsync(context, cancellationToken)
            .ConfigureAwait(false);
    }
}
