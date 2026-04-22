using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Wa1gonLib.Abstractions;
using Wa1gonLib.Internal;
using Wa1gonLib.Models;

namespace Wa1gonLib;

public sealed class RigctldRadioCatalogService
{
    private static readonly Regex ColumnSplit = new("\\s{2,}", RegexOptions.Compiled);
    private readonly ICommandRunner _commandRunner;
    private readonly string _rigctldExecutable;

    public RigctldRadioCatalogService(ICommandRunner? commandRunner = null, string rigctldExecutable = "rigctld")
    {
        _commandRunner = commandRunner ?? new ProcessCommandRunner();
        _rigctldExecutable = rigctldExecutable;
    }

    public async Task<IReadOnlyList<RigCatalogEntry>> GetAllRadiosAsync(CancellationToken cancellationToken = default)
    {
        var result = await _commandRunner.RunAsync(_rigctldExecutable, "-l", cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"'{_rigctldExecutable} -l' failed with exit code {result.ExitCode}: {result.StandardError}");
        }

        return ParseRigList(result.StandardOutput);
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
}

