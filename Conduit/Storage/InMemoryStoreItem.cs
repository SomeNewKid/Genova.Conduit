// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Storage;

/// <summary>
/// Represents an item stored in an in-memory store, together with
/// the timestamp at which it was last added or updated.
/// </summary>
/// <typeparam name="TValue">The type of the stored value.</typeparam>
internal sealed class InMemoryStoreItem<TValue>
{
    /// <summary>
    /// Gets or sets the stored value.
    /// </summary>
    public TValue Value { get; set; } = default!;

    /// <summary>
    /// Gets or sets the timestamp at which the value was last added or updated.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
