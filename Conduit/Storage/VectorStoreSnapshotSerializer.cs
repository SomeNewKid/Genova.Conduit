// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Genova.Common.Attributes;

namespace Genova.Conduit.Storage;

/// <summary>
/// Provides helper methods for importing and exporting <see cref="VectorStoreSnapshot"/>
/// instances to and from JSON streams.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public static class VectorStoreSnapshotSerializer
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Conflicting naming rules.")]
    private static readonly JsonSerializerOptions _jsonOptions;

    static VectorStoreSnapshotSerializer()
    {
        JsonSerializerOptions options = new ()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        _jsonOptions = options;
    }

    /// <summary>
    /// Serializes the specified <see cref="VectorStoreSnapshot"/> into JSON
    /// and writes it to the provided <see cref="Stream"/>.
    /// </summary>
    /// <param name="snapshot">The snapshot to serialize.</param>
    /// <param name="destination">The output stream to write JSON into.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="snapshot"/> or <paramref name="destination"/> is <c>null</c>.
    /// </exception>
    public static async Task ExportAsync(
        VectorStoreSnapshot snapshot,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(destination);

        // Convert snapshot → JSON → write to the provided stream.
        await JsonSerializer.SerializeAsync(
                destination,
                snapshot,
                _jsonOptions,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deserializes a <see cref="VectorStoreSnapshot"/> from the provided JSON stream.
    /// </summary>
    /// <param name="source">The stream containing JSON data.</param>
    /// <param name="cancellationToken">A token that may be used to observe cancellation.</param>
    /// <returns>
    /// A task whose result is the deserialized <see cref="VectorStoreSnapshot"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the JSON cannot be deserialized into a snapshot.
    /// </exception>
    public static async Task<VectorStoreSnapshot> ImportAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        VectorStoreSnapshot? snapshot =
            await JsonSerializer.DeserializeAsync<VectorStoreSnapshot>(
                    source,
                    _jsonOptions,
                    cancellationToken)
                .ConfigureAwait(false);

        if (snapshot == null)
        {
            throw new InvalidOperationException(
                "The JSON stream could not be deserialized into a VectorStoreSnapshot.");
        }

        return snapshot;
    }
}
