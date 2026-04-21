using System.Collections.ObjectModel;

namespace HamBusLog.ViewModels;

public class GridViewModel
{
    public ObservableCollection<LogEntry> LogEntries { get; }
    
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
        LogEntries = new ObservableCollection<LogEntry>
        {
            new LogEntry { Call = "W5XYZ", DateTime = "2026-04-21 14:30", Frequency = "7.250 MHz", Mode = "SSB", RST = "59", Comments = "Good signal" },
            new LogEntry { Call = "K0ABC", DateTime = "2026-04-21 14:45", Frequency = "14.250 MHz", Mode = "CW", RST = "579", Comments = "Strong signal" },
            new LogEntry { Call = "N1XYZ", DateTime = "2026-04-21 15:00", Frequency = "21.250 MHz", Mode = "SSB", RST = "57", Comments = "Weak but readable" },
            new LogEntry { Call = "VE3XYZ", DateTime = "2026-04-21 15:15", Frequency = "3.650 MHz", Mode = "LSB", RST = "549", Comments = "Canadian contact" },
            new LogEntry { Call = "W4ABC", DateTime = "2026-04-21 15:30", Frequency = "28.400 MHz", Mode = "FM", RST = "59+", Comments = "Crystal clear" },
            new LogEntry { Call = "K5XYZ", DateTime = "2026-04-21 15:45", Frequency = "7.180 MHz", Mode = "CW", RST = "589", Comments = "Excellent contact" },
            new LogEntry { Call = "W6ZZZ", DateTime = "2026-04-21 16:00", Frequency = "14.200 MHz", Mode = "SSB", RST = "55", Comments = "Fading in/out" },
            new LogEntry { Call = "N2ABC", DateTime = "2026-04-21 16:15", Frequency = "3.750 MHz", Mode = "USB", RST = "559", Comments = "Local contact" },
            new LogEntry { Call = "VE2XYZ", DateTime = "2026-04-21 16:30", Frequency = "21.350 MHz", Mode = "CW", RST = "569", Comments = "QSO complete" },
            new LogEntry { Call = "W7ABC", DateTime = "2026-04-21 16:45", Frequency = "10.120 MHz", Mode = "CW", RST = "579", Comments = "Great propagation" }
        };
    }
    
    public void AddNewEntry()
    {
        if (string.IsNullOrWhiteSpace(InputCall))
            return;
        
        var newEntry = new LogEntry
        {
            Call = InputCall,
            Date = InputDate,
            Band = InputBand,
            Mode = InputMode,
            TimeOn = InputTimeOn,
            Sent = InputSent,
            Rec = InputRec,
            Country = InputCountry,
            Name = InputName,
            State = InputState,
            County = InputCounty,
            Frequency = InputFreq,
            DateTime = InputDate,
            Comments = InputComments
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
    public string Frequency { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string TimeOn { get; set; } = string.Empty;
    public string Sent { get; set; } = string.Empty;
    public string Rec { get; set; } = string.Empty;
    public string RST { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
}



