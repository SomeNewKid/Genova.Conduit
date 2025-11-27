// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Genova.Conduit.Storage;

/// <summary>
/// Abstraction for storing and retrieving binary or textual file content.
/// </summary>
public interface IFileStore
{
    /// <summary>
    /// Saves a new file or overwrites an existing file with the specified identifier.
    /// </summary>
    /// <param name="fileId">The logical identifier for the file.</param>
    /// <param name="content">The file content as a byte array.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    Task SaveFileAsync(
        string fileId,
        byte[] content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the content of a file by its <paramref name="fileId"/>.
    /// </summary>
    /// <param name="fileId">The logical identifier of the file to load.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// The file content as a byte array if found; otherwise <c>null</c>.
    /// </returns>
    [SuppressMessage(
        "StyleCop.CSharp.SpacingRules",
        "SA1011:Closing square brackets should be spaced correctly",
        Justification = "Conflicting style rules.")]
    Task<byte[]?> LoadFileAsync(
        string fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file by its <paramref name="fileId"/>, if it exists.
    /// </summary>
    /// <param name="fileId">The logical identifier of the file to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    Task DeleteFileAsync(
        string fileId,
        CancellationToken cancellationToken = default);
}
