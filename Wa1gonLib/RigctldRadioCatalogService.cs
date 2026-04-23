using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Wa1gonLib.Models;

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
}

