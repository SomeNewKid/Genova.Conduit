// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit.Agents;

/// <summary>
/// Represents the lifecycle status of an agent task.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public enum AgentTaskStatus
{
    /// <summary>
    /// The task is pending and has not yet been started.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The task is currently in progress.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// The task has been completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The task was attempted but failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The task has been cancelled and will not be processed.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// The task is waiting on an external event, such as a long-running
    /// background process or human approval, before it can continue.
    /// </summary>
    WaitingOnExternalEvent = 5,
}
