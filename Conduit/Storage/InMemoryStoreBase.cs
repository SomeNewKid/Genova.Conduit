// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Storage;

/// <summary>
/// Represents a base class for in-memory stores that hold a limited number
/// of items and evict the oldest items first when the capacity is exceeded.
/// </summary>
/// <typeparam name="TValue">The type of the stored values.</typeparam>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public abstract class InMemoryStoreBase<TValue>
{
    private readonly int _capacity;
    private readonly object _syncRoot;
    private readonly Dictionary<string, InMemoryStoreItem<TValue>> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryStoreBase{TValue}"/> class.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of items that can be stored at any time. When the capacity
    /// is exceeded, the oldest items (by timestamp) are removed first.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="capacity"/> is less than or equal to zero.
    /// </exception>
    protected InMemoryStoreBase(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                "Capacity must be greater than zero.");
        }

        _capacity = capacity;
        _syncRoot = new object();
        _items = new Dictionary<string, InMemoryStoreItem<TValue>>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the maximum number of items that can be stored at any time.
    /// </summary>
    protected int Capacity
    {
        get { return _capacity; }
    }

    /// <summary>
    /// Tries to get a value from the store by its key.
    /// </summary>
    /// <param name="key">The key associated with the stored value.</param>
    /// <param name="value">
    /// When this method returns, contains the stored value if found;
    /// otherwise, the default value for <typeparamref name="TValue"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a value was found for the specified key; otherwise, <c>false</c>.
    /// </returns>
    protected bool TryGetValue(string key, out TValue? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            value = default;
            return false;
        }

        lock (_syncRoot)
        {
            if (_items.TryGetValue(key, out InMemoryStoreItem<TValue>? item) && item != null)
            {
                value = item.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Sets the value for the specified key, updating the timestamp and evicting
    /// the oldest items if the capacity is exceeded.
    /// </summary>
    /// <param name="key">The key associated with the stored value.</param>
    /// <param name="value">The value to store.</param>
    protected void SetValue(string key, TValue value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key must be non-empty.", nameof(key));
        }

        lock (_syncRoot)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            InMemoryStoreItem<TValue> item = new ()
            {
                Value = value,
                Timestamp = now,
            };

            _items[key] = item;

            if (_items.Count > _capacity)
            {
                EvictOldestItems();
            }
        }
    }

    /// <summary>
    /// Gets a snapshot of all stored values.
    /// </summary>
    /// <returns>
    /// A list containing the current values in the store.
    /// </returns>
    protected IList<TValue> GetAllValues()
    {
        lock (_syncRoot)
        {
            List<TValue> values = new List<TValue>(_items.Count);
            foreach (KeyValuePair<string, InMemoryStoreItem<TValue>> pair in _items)
            {
                values.Add(pair.Value.Value);
            }

            return values;
        }
    }

    private void EvictOldestItems()
    {
        while (_items.Count > _capacity)
        {
            string? oldestKey = null;
            DateTimeOffset oldestTimestamp = DateTimeOffset.MaxValue;

            foreach (KeyValuePair<string, InMemoryStoreItem<TValue>> pair in _items)
            {
                if (pair.Value.Timestamp < oldestTimestamp)
                {
                    oldestTimestamp = pair.Value.Timestamp;
                    oldestKey = pair.Key;
                }
            }

            if (oldestKey == null)
            {
                // Should not occur, but break to avoid infinite loop.
                break;
            }

            _items.Remove(oldestKey);
        }
    }
}
