// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Conduit;

/// <summary>
/// Execution environment in which a pipeline is currently running.
/// </summary>
/// <remarks>
/// This is an optional hint that allows steps to adjust behavior if necessary
/// (for example for diagnostics), but steps should not rely on a specific
/// environment in order to function.
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public enum ExecutionEnvironment
{
    /// <summary>
    /// A local, short-lived application (e.g. console, web request).
    /// </summary>
    Application = 0,

    /// <summary>
    /// A local, long-running agent loop or background service.
    /// </summary>
    AgentLocal = 1,

    /// <summary>
    /// A webhook or callback invocation from a remote service (e.g. OpenAI tools).
    /// </summary>
    Webhook = 2,

    /// <summary>
    /// A remote or cloud-hosted agent workflow invoking the pipeline indirectly.
    /// </summary>
    AgentRemote = 3,
}
