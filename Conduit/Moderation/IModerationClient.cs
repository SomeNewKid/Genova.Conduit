// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Moderation;

/// <summary>
/// Defines an abstraction over a content moderation service,
/// such as the OpenAI Moderation API.
/// </summary>
public interface IModerationClient
{
    /// <summary>
    /// Evaluates the specified text for policy violations.
    /// </summary>
    /// <param name="request">The moderation request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A moderation response describing any detected issues.</returns>
    Task<ModerationResponse> ModerateAsync(
        ModerationRequest request,
        CancellationToken cancellationToken = default);
}
