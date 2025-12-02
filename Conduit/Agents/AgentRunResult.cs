// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Agents;

/// <summary>
/// Represents the result of executing a single agent run cycle,
/// including a status and optional diagnostic information.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class AgentRunResult
{
    /// <summary>
    /// Gets or sets the outcome status of the agent run.
    /// </summary>
    public AgentRunStatus Status { get; set; }

    /// <summary>
    /// Gets or sets an optional human-readable message describing the result,
    /// which may include diagnostic details or error information.
    /// </summary>
    public string? Message { get; set; }
}
