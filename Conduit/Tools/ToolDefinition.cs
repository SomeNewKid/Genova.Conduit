// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Tools;

/// <summary>
/// Represents a tool definition that can be exposed to the LLM in a Responses API
/// request, using the function-calling style. The <see cref="ParametersJsonSchema"/>
/// property should contain a JSON Schema describing the tool's parameters.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class ToolDefinition
{
    /// <summary>
    /// Gets or sets the unique name of the tool, as it will be referenced by the model.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a human-readable description of what the tool does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON Schema string describing the parameters for the tool.
    /// This schema will be sent to the Responses API in the <c>tools</c> section.
    /// </summary>
    public string ParametersJsonSchema { get; set; } = "{}";
}
