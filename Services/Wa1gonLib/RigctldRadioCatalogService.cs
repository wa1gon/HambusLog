namespace Wa1gonLib;

public sealed class RigctldRadioCatalogService
{
    private static readonly Regex ColumnSplit = new("\\s{2,}", RegexOptions.Compiled);

    public RigctldRadioCatalogService()
    {
    }

    public Task<IReadOnlyList<RigCatalogEntry>> GetAllRadiosAsync(CancellationToken cancellationToken = default)
    {
        // Flatpak sandboxes can block direct process execution of rigctld.
        // Keep runtime path safe by returning an empty result and use ParseRigList
        // when output is provided by a host-side integration.
        return Task.FromResult<IReadOnlyList<RigCatalogEntry>>(Array.Empty<RigCatalogEntry>());
    }

    public IReadOnlyList<RigCatalogEntry> GetAllRadiosFromText(string? rigctldListOutput)
    {
        return ParseRigList(rigctldListOutput);
    }

    public static IReadOnlyList<RigCatalogEntry> ParseRigList(string? output)
    {
        var entries = new List<RigCatalogEntry>();
        if (string.IsNullOrWhiteSpace(output))
            return entries;

        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            if (!char.IsDigit(line[0]))
                continue;

            var columns = ColumnSplit.Split(line);
            if (columns.Length < 6)
                continue;

            if (!int.TryParse(columns[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rigNum))
                continue;

            entries.Add(new RigCatalogEntry
            {
                RigNum = rigNum,
                Mfg = columns[1],
                Model = columns[2],
                Version = columns[3],
                Status = columns[4],
                Macro = columns[5]
            });
        }

        return entries;
    }

    public static IReadOnlyList<RigCatalogEntry> FilterByModel(IEnumerable<RigCatalogEntry> entries, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return entries.ToList();

        var term = searchText.Trim();
        return entries
            .Where(entry => entry.Model.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.Model.StartsWith(term, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(entry => entry.Model, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Mfg, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string CreateRigctldCommandLine(
        RigCatalogEntry? entry,
        string host = "127.0.0.1",
        int port = 4532,
        string? serialPortName = null,
        string executable = "rigctld",
        string? argumentsTemplate = null,
        string? additionalArguments = null)
    {
        if (entry is null)
            return string.Empty;

        var safeHost = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim();
        var safePort = port <= 0 ? 4532 : port;
        var safeExecutable = string.IsNullOrWhiteSpace(executable) ? "rigctld" : executable.Trim();
        var safeTemplate = string.IsNullOrWhiteSpace(argumentsTemplate)
            ? "-m {rigNum} -T {host} -t {port}{serialArg}"
            : argumentsTemplate.Trim();
        var safeAdditionalArguments = string.IsNullOrWhiteSpace(additionalArguments)
            ? string.Empty
            : additionalArguments.Trim();

        var safeSerialPortName = serialPortName?.Trim() ?? string.Empty;
        var serialArg = string.IsNullOrWhiteSpace(safeSerialPortName) ? string.Empty : $" -r {QuoteArgument(safeSerialPortName)}";
        var arguments = safeTemplate
            .Replace("{rigNum}", entry.RigNum.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{host}", safeHost, StringComparison.OrdinalIgnoreCase)
            .Replace("{port}", safePort.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{serialPort}", QuoteArgument(safeSerialPortName), StringComparison.OrdinalIgnoreCase)
            .Replace("{serialArg}", serialArg, StringComparison.OrdinalIgnoreCase)
            .Replace("{additionalArgs}", safeAdditionalArguments, StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(safeAdditionalArguments)
            && !safeTemplate.Contains("{additionalArgs}", StringComparison.OrdinalIgnoreCase))
            arguments = $"{arguments} {safeAdditionalArguments}";

        var command = $"{safeExecutable} {arguments}".Trim();

        return command;
    }

    private static string QuoteArgument(string value)
    {
        if (value.Length == 0)
            return "\"\"";

        var escaped = value.Replace("\"", "\\\"");
        var needsQuotes = false;
        foreach (var ch in escaped)
        {
            if (!char.IsWhiteSpace(ch))
                continue;

            needsQuotes = true;
            break;
        }

        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }
}

