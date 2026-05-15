namespace HamBusLog.ViewModels;

public sealed class CabrilloExportViewModel : ViewModelBase
{
    private readonly ConfigProfile _profile;

    public CabrilloExportViewModel()
    {
        var config = AppConfigurationStore.Load();
        _profile = AppConfigurationStore.GetActiveProfile(config);
        Contests = new ObservableCollection<CabrilloContestOption>(BuildContestOptions());
        HeaderFields = new ObservableCollection<CabrilloHeaderFieldViewModel>();
        SelectedContest = Contests.FirstOrDefault(x => x.IsSupported) ?? Contests.FirstOrDefault();
        ApplyContestDefaults();
    }

    public ObservableCollection<CabrilloContestOption> Contests { get; }
    public ObservableCollection<CabrilloHeaderFieldViewModel> HeaderFields { get; }

    private CabrilloContestOption? _selectedContest;
    public CabrilloContestOption? SelectedContest
    {
        get => _selectedContest;
        set
        {
            if (SetProperty(ref _selectedContest, value))
            {
                OnPropertyChanged(nameof(CanExport));
                ApplyContestDefaults();
            }
        }
    }

    public bool CanExport => SelectedContest?.IsSupported == true;
    public CabrilloExportSettings BuildSettings()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in HeaderFields)
        {
            var value = field.GetExportValue();
            if (string.IsNullOrWhiteSpace(value))
                continue;

            headers[field.Key] = value;
        }

        return new CabrilloExportSettings(headers);
    }

    private static List<CabrilloContestOption> BuildContestOptions()
    {
        var contests = CabrilloContestCatalog.GetAll()
            .Select(def => new CabrilloContestOption(def))
            .OrderByDescending(x => x.IsSupported)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return contests.Count > 0
            ? contests
            : [new CabrilloContestOption(new CabrilloContestDefinition("ARQP", "AR-QSO-PARTY", "AR-QSO-PARTY", ["AR-QSO-PARTY"], "ARQP", []))];
    }

    private void ApplyContestDefaults()
    {
        HeaderFields.Clear();

        var contest = SelectedContest?.Definition;
        if (contest is null)
        {
            OnPropertyChanged(nameof(HeaderFields));
            return;
        }

        foreach (var field in contest.HeaderFields)
        {
            var defaultValue = ResolveDefaultValue(field);
            HeaderFields.Add(new CabrilloHeaderFieldViewModel(field, defaultValue));
        }

        OnPropertyChanged(nameof(HeaderFields));
    }

    private string ResolveDefaultValue(CabrilloHeaderFieldDefinition field)
    {
        var source = field.DefaultSource?.Trim().ToLowerInvariant();
        var value = source switch
        {
            "profile.stationcallsign" => _profile.StationCallSign,
            "profile.mystateprovince" => _profile.MyStateProvince,
            "profile.myfielddayclass" => _profile.MyFieldDayClass,
            "profile.myfielddaysection" => _profile.MyFieldDaySection,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(value))
            value = field.DefaultValue ?? string.Empty;

        return value ?? string.Empty;
    }
}

public sealed class CabrilloContestOption
{
    public CabrilloContestOption(CabrilloContestDefinition definition)
    {
        Definition = definition;
        IsSupported = CabrilloExportService.IsSupportedExporter(definition.ExporterKey);
    }

    public CabrilloContestDefinition Definition { get; }
    public string Key => Definition.Key;
    public string DisplayName => Definition.DisplayName;
    public string AdifContestId => Definition.AdifContestId;
    public bool IsSupported { get; }

    public string Display => IsSupported ? DisplayName : $"{DisplayName} (not supported)";
}

public sealed class CabrilloHeaderFieldViewModel : ViewModelBase
{
    public CabrilloHeaderFieldViewModel(CabrilloHeaderFieldDefinition definition, string? initialValue)
    {
        Key = definition.Key;
        Label = definition.Label;
        IsRequired = definition.IsRequired;
        IsUppercase = definition.IsUppercase;
        IsMultiline = definition.IsMultiline;
        InputType = string.IsNullOrWhiteSpace(definition.InputType) ? "text" : definition.InputType.Trim();
        _value = NormalizeValue(initialValue ?? string.Empty);
        SeedDateTime(initialValue);
    }

    public string Key { get; }
    public string Label { get; }
    public bool IsRequired { get; }
    public bool IsUppercase { get; }
    public bool IsMultiline { get; }
    public string InputType { get; }
    public bool IsDateTime => string.Equals(InputType, "datetime", StringComparison.OrdinalIgnoreCase);
    public bool IsTextInput => !IsDateTime;
    public string DisplayLabel => IsRequired ? $"{Label} *" : Label;
    public double InputMinHeight => IsMultiline ? 80 : 0;

    private DateTimeOffset? _selectedDateOffset;
    public DateTimeOffset? SelectedDateOffset
    {
        get => _selectedDateOffset;
        set => SetProperty(ref _selectedDateOffset, value);
    }

    private TimeSpan? _selectedTime;
    public TimeSpan? SelectedTime
    {
        get => _selectedTime;
        set => SetProperty(ref _selectedTime, value);
    }

    private string _value = string.Empty;
    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, NormalizeValue(value));
    }

    public string GetExportValue()
    {
        if (IsDateTime)
        {
            if (SelectedDateOffset is null)
                return string.Empty;

            var time = SelectedTime ?? TimeSpan.Zero;
            var utcDate = DateTime.SpecifyKind(SelectedDateOffset.Value.Date, DateTimeKind.Utc);
            var combined = utcDate + time;
            return combined.ToString("yyyy-MM-dd HHmm", CultureInfo.InvariantCulture);
        }

        return Value?.Trim() ?? string.Empty;
    }

    private string NormalizeValue(string value)
    {
        var normalized = value ?? string.Empty;
        if (IsUppercase)
            normalized = normalized.ToUpperInvariant();

        return normalized;
    }

    private void SeedDateTime(string? initialValue)
    {
        if (!IsDateTime || string.IsNullOrWhiteSpace(initialValue))
            return;

        if (!DateTimeOffset.TryParse(initialValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return;

        SelectedDateOffset = parsed.Date;
        SelectedTime = parsed.UtcDateTime.TimeOfDay;
    }
}

public sealed class CabrilloExportSettings
{
    public CabrilloExportSettings(IReadOnlyDictionary<string, string> headers)
    {
        Headers = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public string? GetHeaderValue(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        return Headers.TryGetValue(key, out var value) ? value : null;
    }
}

