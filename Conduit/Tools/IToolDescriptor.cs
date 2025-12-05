// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Tools;

/// <summary>
/// Represents an optional interface that tools can implement to provide
/// their own <see cref="ToolDefinition"/> for exposure to the LLM when
/// using the Responses API.
/// </summary>
public interface IToolDescriptor
{
    /// <summary>
    /// Gets the <see cref="ToolDefinition"/> that describes this tool's name,
    /// description, and parameter schema for LLM tool-calling purposes.
    /// </summary>
    /// <returns>A <see cref="ToolDefinition"/> instance.</returns>
    ToolDefinition GetDefinition();
}
