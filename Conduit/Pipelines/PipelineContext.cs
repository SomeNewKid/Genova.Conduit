// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Pipelines;

/// <summary>
/// Shared, mutable context that flows through all steps in a pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The context is intentionally generic. Pipelines and steps may agree on
/// conventions for keys and strongly-typed properties, but the core abstraction
/// avoids prescribing a specific data model.
/// </para>
/// <para>
/// Host code is responsible for constructing the initial context and
/// interpreting the final state after the pipeline has executed.
/// </para>
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class PipelineContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineContext"/> class.
    /// </summary>
    /// <param name="environment">
    /// Optional execution environment hint; defaults to <see cref="ExecutionEnvironment.Application"/>.
    /// </param>
    public PipelineContext(ExecutionEnvironment environment = ExecutionEnvironment.Application)
    {
        Environment = environment;
        Items = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the execution environment in which the pipeline is running.
    /// </summary>
    public ExecutionEnvironment Environment { get; }

    /// <summary>
    /// Gets a bag of arbitrary values shared by steps in the pipeline.
    /// </summary>
    /// <remarks>
    /// Convention-based keys can be used to pass data between steps.
    /// Where stronger typing is desired, wrapper properties or extension
    /// methods may be defined in higher-level libraries.
    /// </remarks>
    public IDictionary<string, object?> Items { get; }

    /// <summary>
    /// Gets or sets an optional correlation identifier used to trace
    /// a pipeline execution end-to-end across hosts and systems.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Retrieves a value from <see cref="Items"/> using the specified key
    /// and attempts to cast it to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="key">The lookup key.</param>
    /// <returns>
    /// The value cast to <typeparamref name="T"/> if present and of the correct type;
    /// otherwise <c>default</c>.
    /// </returns>
    public T? GetItem<T>(string key)
    {
        if (Items.TryGetValue(key, out object? value) && value is T typed)
        {
            return typed;
        }

        return default;
    }

    /// <summary>
    /// Sets a value in <see cref="Items"/> for the specified key.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The lookup key.</param>
    /// <param name="value">The value to set.</param>
    public void SetItem<T>(string key, T value)
    {
        Items[key] = value;
    }

    /// <summary>
    /// Removes an item from <see cref="Items"/> with the specified key.
    /// </summary>
    /// <param name="key">The lookup key.</param>
    public void RemoveItem(string key)
    {
        Items.Remove(key);
    }
}
