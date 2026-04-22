using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using HamBlocks.Library.Models;

namespace HamBusLog.ViewModels;

public enum ContestType
{
    Normal,
    ArrlFieldDay
}

public class GridViewModel
{
    public ObservableCollection<Qso> LogEntries { get; }
    public List<ContestType> ContestTypes { get; } = new() { ContestType.Normal, ContestType.ArrlFieldDay };
    
    private ContestType _selectedContestType = ContestType.Normal;
    
    public event EventHandler<PropertyChangedEventArgs>? PropertyChanged;
    
    public ContestType SelectedContestType
    {
        get => _selectedContestType;
        set
        {
            if (_selectedContestType != value)
            {
                _selectedContestType = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedContestType)));
            }
        }
    }
    
    private readonly Dictionary<string, bool> _sortAscendingByColumn = new();
    
    // Common input fields
    public string InputCall { get; set; } = string.Empty;
    public string InputDate { get; set; } = string.Empty;
    public string InputBand { get; set; } = string.Empty;
    public string InputMode { get; set; } = string.Empty;
    public string InputTimeOn { get; set; } = string.Empty;
    public string InputSent { get; set; } = string.Empty;
    public string InputRec { get; set; } = string.Empty;
    public string InputFreq { get; set; } = string.Empty;
    
    // ARRL Field Day specific
    public string InputFieldDaySection { get; set; } = string.Empty;
    public string InputFieldDayClass { get; set; } = string.Empty;
    
    public GridViewModel()
    {
        LogEntries = new ObservableCollection<Qso>
        {
            new Qso { Call = "W5XYZ", QsoDate = Convert.ToDateTime("2026-04-21 14:30"), Freq = 7.250m, Mode = "SSB", RstRcvd = "59" },
            new Qso { Call = "K0ABC", QsoDate = Convert.ToDateTime("2026-04-21 14:45"), Freq = 14.250m, Mode = "CW", RstRcvd = "579" },
            new Qso { Call = "N1XYZ", QsoDate = Convert.ToDateTime("2026-04-21 15:00"), Freq = 21.250m, Mode = "SSB", RstRcvd = "57" },
            new Qso { Call = "VE3XYZ", QsoDate = Convert.ToDateTime("2026-04-21 15:15"), Freq = 3.650m, Mode = "LSB", RstRcvd = "549" },
            new Qso { Call = "W4ABC", QsoDate = Convert.ToDateTime("2026-04-21 15:30"), Freq = 28.400m, Mode = "FM", RstRcvd = "59+" },
            new Qso { Call = "K5XYZ", QsoDate = Convert.ToDateTime("2026-04-21 15:45"), Freq = 7.180m, Mode = "CW", RstRcvd = "589" },
            new Qso { Call = "W6ZZZ", QsoDate = Convert.ToDateTime("2026-04-21 16:00"), Freq = 14.200m, Mode = "SSB", RstRcvd = "55" },
            new Qso { Call = "N2ABC", QsoDate = Convert.ToDateTime("2026-04-21 16:15"), Freq = 3.750m, Mode = "USB", RstRcvd = "559" },
            new Qso { Call = "VE2XYZ", QsoDate = Convert.ToDateTime("2026-04-21 16:30"), Freq = 21.350m, Mode = "CW", RstRcvd = "569" },
            new Qso { Call = "W7ABC", QsoDate = Convert.ToDateTime("2026-04-21 16:45"), Freq = 10.120m, Mode = "CW", RstRcvd = "579" }
        };
    }
    
    public void AddNewEntry()
    {
        if (string.IsNullOrWhiteSpace(InputCall))
            return;

        var qsoDate = DateTime.TryParse(InputDate, out var parsedDate)
            ? parsedDate
            : DateTime.Now;
        var freq = decimal.TryParse(InputFreq, out var parsedFreq)
            ? parsedFreq
            : 0m;
        
        var newEntry = new Qso
        {
            Call = InputCall,
            QsoDate = qsoDate,
            Band = InputBand,
            Mode = InputMode,
            RstSent = InputSent,
            RstRcvd = InputRec,
            Freq = freq
        };
        
        // Store contest-specific fields in QsoDetails
        if (SelectedContestType == ContestType.ArrlFieldDay && 
            (!string.IsNullOrWhiteSpace(InputFieldDaySection) || !string.IsNullOrWhiteSpace(InputFieldDayClass)))
        {
            newEntry.Details = new List<QsoDetail>();
            if (!string.IsNullOrWhiteSpace(InputFieldDaySection))
                newEntry.Details.Add(new QsoDetail { FieldName = "Section", FieldValue = InputFieldDaySection });
            if (!string.IsNullOrWhiteSpace(InputFieldDayClass))
                newEntry.Details.Add(new QsoDetail { FieldName = "Class", FieldValue = InputFieldDayClass });
        }
        
        LogEntries.Add(newEntry);
        ClearInputs();
    }

    public void SortBy(string column)
    {
        if (string.IsNullOrWhiteSpace(column) || LogEntries.Count == 0)
            return;

        var ascending = !_sortAscendingByColumn.GetValueOrDefault(column, false);
        _sortAscendingByColumn[column] = ascending;

        IEnumerable<Qso> sorted = column switch
        {
            "Call" => ascending
                ? LogEntries.OrderBy(x => x.Call)
                : LogEntries.OrderByDescending(x => x.Call),
            "QsoDate" => ascending
                ? LogEntries.OrderBy(x => x.QsoDate)
                : LogEntries.OrderByDescending(x => x.QsoDate),
            "Freq" => ascending
                ? LogEntries.OrderBy(x => x.Freq)
                : LogEntries.OrderByDescending(x => x.Freq),
            "Mode" => ascending
                ? LogEntries.OrderBy(x => x.Mode)
                : LogEntries.OrderByDescending(x => x.Mode),
            "RstRcvd" => ascending
                ? LogEntries.OrderBy(x => x.RstRcvd)
                : LogEntries.OrderByDescending(x => x.RstRcvd),
            _ => LogEntries
        };

        var snapshot = sorted.ToList();
        LogEntries.Clear();
        foreach (var item in snapshot)
            LogEntries.Add(item);
    }
    
    private void ClearInputs()
    {
        InputCall = string.Empty;
        InputDate = string.Empty;
        InputBand = string.Empty;
        InputMode = string.Empty;
        InputTimeOn = string.Empty;
        InputSent = string.Empty;
        InputRec = string.Empty;
        InputFreq = string.Empty;
        InputFieldDaySection = string.Empty;
        InputFieldDayClass = string.Empty;
    }
}

public class LogEntry
{
    public string Call { get; set; } = string.Empty;
    public string DateTime { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Band { get; set; } = string.Empty;
    public decimal Freq { get; set; } = 0.0m;
    public string Mode { get; set; } = string.Empty;
    public string RstRcvd { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
}



