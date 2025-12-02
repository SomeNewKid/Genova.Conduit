// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Agents;

/// <summary>
/// Represents the outcome status of a single agent execution cycle.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public enum AgentRunStatus
{
    /// <summary>
    /// The agent completed its current work for this cycle.
    /// </summary>
    Completed = 0,

    /// <summary>
    /// The agent is waiting on one or more external events,
    /// such as long-running processes or human approvals, before
    /// it can make further progress.
    /// </summary>
    PendingExternalEvents = 1,

    /// <summary>
    /// The agent encountered an error and could not complete its work.
    /// </summary>
    Failed = 2,
}
