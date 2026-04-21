using System;
using System.Collections.ObjectModel;
using HamBlocks.Library.Models;

namespace HamBusLog.ViewModels;

public class GridViewModel
{
    public ObservableCollection<Qso> LogEntries { get; }
    
    // Input fields
    public string InputCall { get; set; } = string.Empty;
    public string InputDate { get; set; } = string.Empty;
    public string InputBand { get; set; } = string.Empty;
    public string InputMode { get; set; } = string.Empty;
    public string InputTimeOn { get; set; } = string.Empty;
    public string InputSent { get; set; } = string.Empty;
    public string InputRec { get; set; } = string.Empty;
    public string InputCountry { get; set; } = string.Empty;
    public string InputName { get; set; } = string.Empty;
    public string InputState { get; set; } = string.Empty;
    public string InputCounty { get; set; } = string.Empty;
    public string InputFreq { get; set; } = string.Empty;
    public string InputComments { get; set; } = string.Empty;

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
            Country = InputCountry,
            State = InputState,
            Freq = freq
        };
        
        LogEntries.Add(newEntry);
        ClearInputs();
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
        InputCountry = string.Empty;
        InputName = string.Empty;
        InputState = string.Empty;
        InputCounty = string.Empty;
        InputFreq = string.Empty;
        InputComments = string.Empty;
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



