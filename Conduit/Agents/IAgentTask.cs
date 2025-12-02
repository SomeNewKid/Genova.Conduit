// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Conduit.Agents;

/// <summary>
/// Abstraction representing a unit of work that an agent can process,
/// such as a step in a larger goal or an independent task.
/// </summary>
public interface IAgentTask
{
    /// <summary>
    /// Gets the unique identifier for the task.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the identifier of the agent that owns this task.
    /// </summary>
    string AgentId { get; }

    /// <summary>
    /// Gets or sets a human-readable description of the task.
    /// </summary>
    string? Description { get; set; }

    /// <summary>
    /// Gets or sets the current status of the task.
    /// </summary>
    AgentTaskStatus Status { get; set; }

    /// <summary>
    /// Gets a dictionary that can hold arbitrary task-specific data,
    /// such as parameters, progress markers, or external identifiers.
    /// </summary>
    IDictionary<string, object?> Data { get; }
}
