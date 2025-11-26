// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Tools;

/// <summary>
/// Registry abstraction for discovering and resolving tools by name.
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Tries to retrieve a tool instance by its <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The tool name to locate.</param>
    /// <param name="tool">
    /// When this method returns, contains the tool instance if found;
    /// otherwise <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a tool with the specified name was found; otherwise <c>false</c>.
    /// </returns>
    bool TryGetTool(string name, out ITool? tool);

    /// <summary>
    /// Gets all tools currently registered in the registry.
    /// </summary>
    /// <returns>An enumeration of registered tools.</returns>
    IEnumerable<ITool> GetAllTools();
}
