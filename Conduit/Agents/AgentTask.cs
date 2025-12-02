// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Agents;

/// <summary>
/// Represents a minimal deterministic implementation of <see cref="IAgentTask"/>.
/// Suitable for unit testing agent behavior without external dependencies.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class AgentTask : IAgentTask
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentTask"/> class
    /// with the specified identifiers and an optional description.
    /// </summary>
    /// <param name="id">The unique identifier for the task.</param>
    /// <param name="agentId">The identifier of the agent that owns this task.</param>
    /// <param name="description">An optional human-readable description of the task.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> or <paramref name="agentId"/> is null or whitespace.
    /// </exception>
    public AgentTask(
        string id,
        string agentId,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Task Id must be non-empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("AgentId must be non-empty.", nameof(agentId));
        }

        Id = id;
        AgentId = agentId;
        Description = description;
        Status = AgentTaskStatus.Pending;
        Data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the unique identifier for the task.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the identifier of the agent that owns this task.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets or sets an optional human-readable description of the task.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the current status of the task.
    /// Defaults to <see cref="AgentTaskStatus.Pending"/>.
    /// </summary>
    public AgentTaskStatus Status { get; set; }

    /// <summary>
    /// Gets a dictionary containing task-specific data.
    /// </summary>
    public IDictionary<string, object?> Data { get; }
}
