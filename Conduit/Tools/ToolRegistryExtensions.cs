// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Tools;

/// <summary>
/// Provides helper methods for working with <see cref="IToolRegistry"/> instances.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public static class ToolRegistryExtensions
{
    /// <summary>
    /// Gets the collection of <see cref="ToolDefinition"/> instances for tools in the registry
    /// that implement <see cref="IToolDescriptor"/>.
    /// </summary>
    /// <param name="registry">The tool registry from which definitions are retrieved.</param>
    /// <returns>
    /// A list of <see cref="ToolDefinition"/> instances describing tools that can be
    /// exposed to the LLM.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registry"/> is <c>null</c>.
    /// </exception>
    public static IList<ToolDefinition> GetToolDefinitions(this IToolRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        IList<ToolDefinition> definitions = [];

        foreach (ITool tool in registry.GetAllTools())
        {
            if (tool is IToolDescriptor descriptor)
            {
                ToolDefinition definition = descriptor.GetDefinition();
                if (!string.IsNullOrWhiteSpace(definition.Name))
                {
                    definitions.Add(definition);
                }
            }
        }

        return definitions;
    }
}
