// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Genova.Common.Attributes;
using Genova.Conduit.Pipelines;
using Genova.Conduit.Tools;

namespace Genova.Conduit.Terminal.Tools;

/// <summary>
/// Represents a simple local tool that returns the current date
/// as a formatted string.
/// </summary>
/// <remarks>
/// This tool runs entirely locally and does not make any network calls.
/// It supports optional arguments:
/// <list type="bullet">
/// <item>
/// <description>
/// <c>"kind"</c> – either <c>"local"</c> (default) or <c>"utc"</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>"format"</c> – a .NET date format string. Defaults to <c>"d"</c>
/// (short date pattern).
/// </description>
/// </item>
/// </list>
/// </remarks>
[CodeQuality(Public = true, Justification = "Intended for use by libraries and applications.")]
public sealed class LocalDateTool : ITool
{
    private const string KindArgumentName = "kind";
    private const string FormatArgumentName = "format";

    /// <summary>
    /// Gets the tool name used to reference this tool.
    /// </summary>
    public string Name
    {
        get { return "LocalDate"; }
    }

    /// <summary>
    /// Gets a human-readable description of what the tool does.
    /// </summary>
    public string Description
    {
        get { return "Returns the current date as a formatted string."; }
    }

    /// <summary>
    /// Invokes the tool using the specified arguments and updates or reads
    /// from the provided pipeline context as necessary.
    /// </summary>
    /// <param name="arguments">
    /// The arguments provided by the caller. Recognized keys are <c>"kind"</c> and <c>"format"</c>.
    /// </param>
    /// <param name="context">
    /// The pipeline context for the current execution. This implementation does not modify the context.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that may be used to observe cancellation.
    /// </param>
    /// <returns>
    /// A task whose result is a string representing the current date.
    /// </returns>
    public Task<object?> InvokeAsync(
        IDictionary<string, object?> arguments,
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        DateTime now = GetNow(arguments);
        string format = GetFormat(arguments);
        string formatted = now.ToString(format, CultureInfo.InvariantCulture);

        return Task.FromResult<object?>(formatted);
    }

    private static DateTime GetNow(IDictionary<string, object?> arguments)
    {
        if (arguments.TryGetValue(KindArgumentName, out object? value) &&
            value is string kindString)
        {
            if (string.Equals(kindString, "utc", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.UtcNow.Date;
            }

            if (string.Equals(kindString, "local", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.Now.Date;
            }
        }

        return DateTime.Now.Date;
    }

    private static string GetFormat(IDictionary<string, object?> arguments)
    {
        if (arguments.TryGetValue(FormatArgumentName, out object? value) &&
            value is string formatString &&
            !string.IsNullOrWhiteSpace(formatString))
        {
            return formatString;
        }

        return "d";
    }
}
