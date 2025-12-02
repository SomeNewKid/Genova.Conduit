// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Conduit.Pipelines;

namespace Genova.Conduit.Utilities;

/// <summary>
/// Helper methods for context management.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public static class ContextHelper
{
    /// <summary>
    /// Retrieves an integer argument from the provided arguments dictionary.
    /// Error messages use a generic "tool" name when no tool name is specified.
    /// </summary>
    /// <param name="arguments">Invocation arguments.</param>
    /// <param name="key">Argument key to retrieve.</param>
    /// <returns>The integer value for the specified key.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the key is missing, null, or not an integer.
    /// </exception>
    public static int GetInteger(IDictionary<string, object?> arguments, string key)
        => GetInteger(arguments, key, "tool");

    /// <summary>
    /// Retrieves an integer argument from the provided arguments dictionary.
    /// </summary>
    /// <param name="arguments">Invocation arguments.</param>
    /// <param name="key">Argument key to retrieve.</param>
    /// <param name="toolName">Short name of the calling tool (e.g. "division"). Used in error messages.</param>
    /// <returns>The integer value for the specified key.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the key is missing, null, or not an integer.
    /// </exception>
    public static int GetInteger(IDictionary<string, object?> arguments, string key, string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            toolName = "tool";
        }

        if (!arguments.TryGetValue(key, out object? raw) || raw == null)
        {
            throw new ArgumentException(
                $"Argument '{key}' is required for the {toolName} tool.");
        }

        if (raw is int intValue)
        {
            return intValue;
        }

        throw new ArgumentException(
            $"Argument '{key}' must be an integer for the {toolName} tool.");
    }

    /// <summary>
    /// Retrieves an integer argument from the provided pipeline context.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <param name="key">Argument key to retrieve.</param>
    /// <returns>The integer value for the specified key.</returns>
    public static int GetInteger(
        PipelineContext context,
        string key) => GetInteger(context, key, key);

    /// <summary>
    /// Retrieves an integer argument from the provided pipeline context.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <param name="key">Argument key to retrieve.</param>
    /// <param name="friendlyName">Friendly name of the key (e.g. "division"). Used in error messages.</param>
    /// <returns>The integer value for the specified key.</returns>
    public static int GetInteger(
        PipelineContext context,
        string key,
        string friendlyName)
    {
        object? raw = context.GetItem<object>(key);

        if (raw == null)
        {
            throw new InvalidOperationException(
                $"{friendlyName} was not found in the pipeline context under key '{key}'.");
        }

        if (raw is int intValue)
        {
            return intValue;
        }

        throw new InvalidOperationException(
            $"{friendlyName} under key '{key}' is not an integer.");
    }
}
