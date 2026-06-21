namespace HamBusLog.ViewModels;

public sealed class CabrilloExportViewModel : ViewModelBase
{
    private readonly ConfigProfile _profile;
    private bool _isUpdatingClaimedScore;

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
            if (field.IsComputed)
                continue;

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
            var fieldViewModel = new CabrilloHeaderFieldViewModel(field, defaultValue);
            fieldViewModel.PropertyChanged += OnHeaderFieldPropertyChanged;
            HeaderFields.Add(fieldViewModel);
        }

        UpdateClaimedScore();
        OnPropertyChanged(nameof(HeaderFields));
    }

    private void OnHeaderFieldPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isUpdatingClaimedScore || sender is not CabrilloHeaderFieldViewModel field)
            return;

        if (e.PropertyName is not (nameof(CabrilloHeaderFieldViewModel.Value)
            or nameof(CabrilloHeaderFieldViewModel.SelectedDateOffset)
            or nameof(CabrilloHeaderFieldViewModel.SelectedTime)))
        {
            return;
        }

        if (field.Key is not ("CONTEST-START" or "CONTEST-END"))
            return;

        UpdateClaimedScore();
    }

    private void UpdateClaimedScore()
    {
        var claimedScoreField = HeaderFields.FirstOrDefault(field =>
            string.Equals(field.Key, "CLAIMED-SCORE", StringComparison.OrdinalIgnoreCase));
        var contest = SelectedContest?.Definition;

        if (claimedScoreField is null || contest is null)
            return;

        var score = CalculateClaimedScore(contest);

        _isUpdatingClaimedScore = true;
        try
        {
            claimedScoreField.Value = score.ToString(CultureInfo.InvariantCulture);
        }
        finally
        {
            _isUpdatingClaimedScore = false;
        }
    }

    private int CalculateClaimedScore(CabrilloContestDefinition contest)
    {
        try
        {
            var connectionString = string.IsNullOrWhiteSpace(_profile.ConnectionString)
                ? "Data Source=hambuslog.db"
                : _profile.ConnectionString;
            var sinceUtc = ParseContestDateTime(GetHeaderValue("CONTEST-START"));
            var untilUtc = ParseContestDateTime(GetHeaderValue("CONTEST-END"));
            var contestIds = contest.AdifContestIds.Count > 0
                ? contest.AdifContestIds
                : [contest.AdifContestId];
            var normalizedContestIds = contestIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim().ToUpperInvariant())
                .ToHashSet(StringComparer.Ordinal);

            using var db = HamBusLogDbContextFactory.Create(DatabaseProvider.Sqlite, connectionString);
            var query = db.Qsos
                .AsNoTracking()
                .Where(q => q.ContestId != null && normalizedContestIds.Contains(q.ContestId.ToUpper()));

            if (sinceUtc is not null)
                query = query.Where(q => q.QsoDate >= sinceUtc.Value);

            if (untilUtc is not null)
                query = query.Where(q => q.QsoDate <= untilUtc.Value);

            var qsos = query
                .Select(q => new { q.Mode })
                .ToList();

            return string.Equals(contest.ExporterKey, "ARRL-FD", StringComparison.OrdinalIgnoreCase)
                ? qsos.Sum(q => CalculateFieldDayQsoPoints(q.Mode))
                : qsos.Count;
        }
        catch
        {
            return 0;
        }
    }

    private string? GetHeaderValue(string key)
    {
        return HeaderFields
            .FirstOrDefault(field => string.Equals(field.Key, key, StringComparison.OrdinalIgnoreCase))
            ?.GetExportValue();
    }

    private static int CalculateFieldDayQsoPoints(string? mode)
    {
        return NormalizeFieldDayMode(mode) == "PHONE" ? 1 : 2;
    }

    private static string NormalizeFieldDayMode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "PHONE";

        var mode = raw.Trim().ToUpperInvariant();
        if (mode is "CW" or "MORSE")
            return "CW";

        if (mode is "SSB" or "AM" or "FM" or "LSB" or "USB")
            return "PHONE";

        if (mode is "FT8" or "FT4" or "RTTY" or "RY" or "PSK" or "PSK31" or "PSK63" or "OLIVIA" or "HELL" or "DSTAR" or "ATV" or "JS8" or "MFSK" or "PACKET" or "THOR" or "DOMINO" or "DIGITAL")
            return "DIGITAL";

        if (mode.Contains("DIGITAL", StringComparison.Ordinal)
            || mode.Contains("DATA", StringComparison.Ordinal)
            || mode.Contains("PACKET", StringComparison.Ordinal)
            || mode.Contains("JT", StringComparison.Ordinal)
            || mode.Contains("FT", StringComparison.Ordinal))
        {
            return "DIGITAL";
        }

        return "PHONE";
    }

    private static DateTime? ParseContestDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (DateTime.TryParseExact(trimmed, "yyyy-MM-dd HHmm", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            return parsed;

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
            return parsed;

        return null;
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
        Options = definition.Options;
        _value = NormalizeValue(initialValue ?? string.Empty);
        SeedDateTime(initialValue);
    }

    public string Key { get; }
    public string Label { get; }
    public bool IsRequired { get; }
    public bool IsUppercase { get; }
    public bool IsMultiline { get; }
    public string InputType { get; }
    public IReadOnlyList<string> Options { get; }
    public bool IsDateTime => string.Equals(InputType, "datetime", StringComparison.OrdinalIgnoreCase);
    public bool IsComputed => string.Equals(InputType, "computed", StringComparison.OrdinalIgnoreCase);
    public bool IsSelection => string.Equals(InputType, "select", StringComparison.OrdinalIgnoreCase) || Options.Count > 0;
    public bool IsTextInput => !IsDateTime && !IsSelection && !IsComputed;
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
    public CabrilloExportSettings(IReadOnlyDictionary<string, string> headers, int bonusPoints = 0)
    {
        Headers = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        BonusPoints = bonusPoints;
    }

    public IReadOnlyDictionary<string, string> Headers { get; }
    
    public int BonusPoints { get; }

    public string? GetHeaderValue(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        return Headers.TryGetValue(key, out var value) ? value : null;
    }
}

