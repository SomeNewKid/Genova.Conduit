// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Tools;

/// <summary>
/// Represents an in-memory implementation of <see cref="IToolRegistry"/>
/// that stores tools in a dictionary keyed by name.
/// </summary>
/// <remarks>
/// This registry is suitable for local applications and simple demos,
/// where the available tools are known at startup and do not change
/// dynamically at runtime.
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class InMemoryToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _toolsByName;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryToolRegistry"/> class.
    /// </summary>
    /// <param name="tools">
    /// The collection of tools to register. Tool names are treated in a
    /// case-insensitive manner.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tools"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when two tools share the same name, ignoring case.
    /// </exception>
    public InMemoryToolRegistry(IEnumerable<ITool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        _toolsByName = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase);

        foreach (ITool tool in tools)
        {
            if (tool == null)
            {
                continue;
            }

            string name = tool.Name ?? string.Empty;

            if (_toolsByName.ContainsKey(name))
            {
                throw new ArgumentException(
                    $"A tool with the name '{name}' has already been registered.",
                    nameof(tools));
            }

            _toolsByName[name] = tool;
        }
    }

    /// <summary>
    /// Tries to retrieve a tool instance by its name.
    /// </summary>
    /// <param name="name">The tool name to locate.</param>
    /// <param name="tool">
    /// When this method returns, contains the tool instance if found;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a tool with the specified name was found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetTool(string name, out ITool? tool)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            tool = null;
            return false;
        }

        return _toolsByName.TryGetValue(name, out tool);
    }

    /// <summary>
    /// Gets all tools currently registered in the registry.
    /// </summary>
    /// <returns>
    /// An enumerable collection of the registered tools.
    /// </returns>
    public IEnumerable<ITool> GetAllTools()
    {
        return _toolsByName.Values;
    }
}
