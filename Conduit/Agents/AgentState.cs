// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Agents;

/// <summary>
/// Represents the state of an agent, including goals, tasks,
/// and any additional metadata required to continue execution.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class AgentState
{
    /// <summary>
    /// Gets or sets the unique identifier for the agent.
    /// </summary>
    public string AgentId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the current goal or high-level description
    /// of what the agent is trying to achieve.
    /// </summary>
    public string? Goal { get; set; }

    /// <summary>
    /// Gets or sets arbitrary serialized state for the agent.
    /// </summary>
    public IDictionary<string, object?> Data { get; set; } =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
}
