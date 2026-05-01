namespace HamBusLog.ViewModels;

using HamBusLog.Hardware;

public sealed class LogInputViewModel : ViewModelBase
{
    private readonly CallValidator   _callValidator   = new();
    private readonly BandValidator   _bandValidator   = new();
    private readonly ModeValidator   _modeValidator   = new();
    private readonly SectionValidator _sectionValidator = new();
    private readonly ClassValidator  _classValidator  = new();

    // ----- core fields -----
    private string _inputCall    = string.Empty;
    private string _inputDate    = string.Empty;
    private string _inputTimeOn  = string.Empty;
    private string _inputBand    = string.Empty;
    private string _inputMode    = string.Empty;
    private string _inputFreq    = string.Empty;
    private string _inputSent    = string.Empty;
    private string _inputRec     = string.Empty;
    private ContestType _selectedContestType = ContestType.Normal;

    // ----- field day fields -----
    private string _inputFieldDaySection = string.Empty;
    private string _inputFieldDayClass   = string.Empty;

    // ----- validation -----
    private string _callError    = string.Empty;
    private string _bandError    = string.Empty;
    private string _modeError    = string.Empty;
    private string _sectionError = string.Empty;
    private string _classError   = string.Empty;

    // ----- detail row being edited -----
    private string _newDetailField = string.Empty;
    private string _newDetailValue = string.Empty;
    private QsoDetailRow? _selectedDetail;
    private AppConfiguration _appConfig = new();
    private string _selectedProfile = "default";

    // ----- active rig snapshot for status display -----
    private string _activeRigStatus = "No active rig";
    private string _activeRigLabel = string.Empty;
    private string _activeRigMode = string.Empty;
    private string _activeRigFrequency = string.Empty;
    private bool _isActiveRigConnected;
    private ObservableCollection<ConnectedRadioOption> _availableConnectedRadios = [];
    private ConnectedRadioOption? _selectedConnectedRadio;

    public LogInputViewModel()
    {
        _appConfig = AppConfigurationStore.Load();
        ContestTypes    = [ContestType.Normal, ContestType.ArrlFieldDay];
        Details         = [];
        AvailableProfiles = new ObservableCollection<string>();
        AvailableConnectedRadios = new ObservableCollection<ConnectedRadioOption>();
        LoadProfiles();
        InputDate       = DateTime.UtcNow.ToString("yyyyMMdd");
        InputTimeOn     = DateTime.UtcNow.ToString("HHmm");
        ApplyActiveRigSnapshot();
    }

    // ── Properties ────────────────────────────────────────────────────
    public List<ContestType> ContestTypes { get; }
    public ObservableCollection<QsoDetailRow> Details { get; }
    public ObservableCollection<string> AvailableProfiles { get; }

    public ObservableCollection<ConnectedRadioOption> AvailableConnectedRadios
    {
        get => _availableConnectedRadios;
        private set => SetProperty(ref _availableConnectedRadios, value);
    }

    public ConnectedRadioOption? SelectedConnectedRadio
    {
        get => _selectedConnectedRadio;
        set
        {
            var previousName = _selectedConnectedRadio?.RadioName;
            var nextName = value?.RadioName;

            if (!SetProperty(ref _selectedConnectedRadio, value))
                return;

            if (!string.Equals(previousName, nextName, StringComparison.OrdinalIgnoreCase))
                ApplySelectedRadioToInputs();

            UpdateActiveRigDisplay(SelectedConnectedRadio?.State ?? App.RigctldConnectionManager.GetPrimaryActiveState());
        }
    }

    public string ActiveRigStatus
    {
        get => _activeRigStatus;
        private set => SetProperty(ref _activeRigStatus, value);
    }

    public string ActiveRigLabel
    {
        get => _activeRigLabel;
        private set => SetProperty(ref _activeRigLabel, value);
    }

    public string ActiveRigMode
    {
        get => _activeRigMode;
        private set => SetProperty(ref _activeRigMode, value);
    }

    public string ActiveRigFrequency
    {
        get => _activeRigFrequency;
        private set => SetProperty(ref _activeRigFrequency, value);
    }

    public bool IsActiveRigConnected
    {
        get => _isActiveRigConnected;
        private set
        {
            if (!SetProperty(ref _isActiveRigConnected, value))
                return;

            OnPropertyChanged(nameof(IsActiveRigDisconnected));
        }
    }

    public bool IsActiveRigDisconnected => !IsActiveRigConnected;

    public string SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (!SetProperty(ref _selectedProfile, value ?? string.Empty))
                return;

            PersistSelectedProfile();
        }
    }

    public ContestType SelectedContestType
    {
        get => _selectedContestType;
        set { if (SetProperty(ref _selectedContestType, value)) OnPropertyChanged(nameof(IsFieldDay)); }
    }
    public bool IsFieldDay => SelectedContestType == ContestType.ArrlFieldDay;

    public string InputCall
    {
        get => _inputCall;
        set => SetProperty(ref _inputCall, (value ?? string.Empty).ToUpperInvariant());
    }
    public string InputDate    { get => _inputDate;    set => SetProperty(ref _inputDate,    value); }
    public string InputTimeOn  { get => _inputTimeOn;  set => SetProperty(ref _inputTimeOn,  value); }
    public string InputBand    { get => _inputBand;    set { if (SetProperty(ref _inputBand, value)) ValidateBand(); } }
    public string InputMode    { get => _inputMode;    set { if (SetProperty(ref _inputMode, value)) ValidateMode(); } }
    public string InputFreq    { get => _inputFreq;    set => SetProperty(ref _inputFreq,    value); }
    public string InputSent    { get => _inputSent;    set => SetProperty(ref _inputSent,    value); }
    public string InputRec     { get => _inputRec;     set => SetProperty(ref _inputRec,     value); }
    public string InputFieldDaySection
    {
        get => _inputFieldDaySection;
        set
        {
            if (SetProperty(ref _inputFieldDaySection, (value ?? string.Empty).ToUpperInvariant()))
                ValidateSection();
        }
    }

    public string InputFieldDayClass
    {
        get => _inputFieldDayClass;
        set
        {
            if (SetProperty(ref _inputFieldDayClass, (value ?? string.Empty).ToUpperInvariant()))
                ValidateClass();
        }
    }

    public string NewDetailField { get => _newDetailField; set => SetProperty(ref _newDetailField, value); }
    public string NewDetailValue { get => _newDetailValue; set => SetProperty(ref _newDetailValue, value); }

    public QsoDetailRow? SelectedDetail
    {
        get => _selectedDetail;
        set => SetProperty(ref _selectedDetail, value);
    }

    // Validation
    public string CallError    { get => _callError;    private set { if (SetProperty(ref _callError,    value)) OnPropertyChanged(nameof(HasCallError));    } }
    public string BandError    { get => _bandError;    private set { if (SetProperty(ref _bandError,    value)) OnPropertyChanged(nameof(HasBandError));    } }
    public string ModeError    { get => _modeError;    private set { if (SetProperty(ref _modeError,    value)) OnPropertyChanged(nameof(HasModeError));    } }
    public string SectionError { get => _sectionError; private set { if (SetProperty(ref _sectionError, value)) OnPropertyChanged(nameof(HasSectionError)); } }
    public string ClassError   { get => _classError;   private set { if (SetProperty(ref _classError,   value)) OnPropertyChanged(nameof(HasClassError));   } }

    public bool HasCallError    => !string.IsNullOrWhiteSpace(CallError);
    public bool HasBandError    => !string.IsNullOrWhiteSpace(BandError);
    public bool HasModeError    => !string.IsNullOrWhiteSpace(ModeError);
    public bool HasSectionError => !string.IsNullOrWhiteSpace(SectionError);
    public bool HasClassError   => !string.IsNullOrWhiteSpace(ClassError);

    // ── Detail Table Actions ──────────────────────────────────────────
    public bool AddDetail()
    {
        if (string.IsNullOrWhiteSpace(NewDetailField))
            return false;

        Details.Add(new QsoDetailRow { FieldName = NewDetailField.Trim(), FieldValue = NewDetailValue.Trim() });
        NewDetailField = string.Empty;
        NewDetailValue = string.Empty;
        return true;
    }

    public bool RemoveSelectedDetail()
    {
        if (SelectedDetail is null)
            return false;
        Details.Remove(SelectedDetail);
        SelectedDetail = null;
        return true;
    }

    // ── Build QSO ─────────────────────────────────────────────────────
    /// <summary>
    /// Returns a new <see cref="Qso"/> if validation passes; null otherwise.
    /// Caller should check the out-parameter error message.
    /// </summary>
    public Qso? TryBuildQso(out string errorMessage)
    {
        ClearErrors();

        var callResult = _callValidator.Validate(InputCall.Trim().ToUpperInvariant());
        if (!callResult.IsValid) { CallError = callResult.ErrorMessage; errorMessage = CallError; return null; }

        var band = InputBand.Trim().ToUpperInvariant();
        var bandResult = _bandValidator.Validate(band);
        if (!bandResult.IsValid) { BandError = bandResult.ErrorMessage; errorMessage = BandError; return null; }

        var mode = InputMode.Trim().ToUpperInvariant();
        var modeResult = _modeValidator.Validate(mode);
        if (!modeResult.IsValid) { ModeError = modeResult.ErrorMessage; errorMessage = ModeError; return null; }

        if (IsFieldDay)
        {
            var sec   = InputFieldDaySection.Trim().ToUpperInvariant();
            var cls   = InputFieldDayClass.Trim().ToUpperInvariant();
            var sr    = _sectionValidator.Validate(sec);
            if (!sr.IsValid)  { SectionError = sr.ErrorMessage; errorMessage = SectionError; return null; }
            var cr    = _classValidator.Validate(cls);
            if (!cr.IsValid)  { ClassError   = cr.ErrorMessage; errorMessage = ClassError;   return null; }
        }

        var qsoDate = DateTime.TryParseExact(InputDate + " " + InputTimeOn, "yyyyMMdd HHmm",
                          null, System.Globalization.DateTimeStyles.None, out var dt)
                      ? dt
                      : DateTime.UtcNow;

        var freq = decimal.TryParse(InputFreq, System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0m;

        var qso = new Qso
        {
            Call    = InputCall.Trim().ToUpperInvariant(),
            QsoDate = qsoDate,
            Band    = band,
            Mode    = mode,
            Freq    = freq,
            RstSent = IsFieldDay ? string.Empty : InputSent.Trim(),
            RstRcvd = IsFieldDay ? string.Empty : InputRec.Trim(),
            Details = new List<QsoDetail>()
        };

        // copy detail rows
        foreach (var row in Details)
            qso.Details.Add(new QsoDetail { FieldName = row.FieldName, FieldValue = row.FieldValue });

        // field-day exchange stored as details
        if (IsFieldDay)
        {
            qso.Details.Add(new QsoDetail { FieldName = "Section", FieldValue = InputFieldDaySection.Trim().ToUpperInvariant() });
            qso.Details.Add(new QsoDetail { FieldName = "Class",   FieldValue = InputFieldDayClass.Trim().ToUpperInvariant()   });
        }

        // Attach selected connected rig metadata when available.
        var activeRig = GetSelectedOrPrimaryRigState();
        if (activeRig is not null)
        {
            qso.Details.Add(new QsoDetail { FieldName = "radio_name", FieldValue = activeRig.RadioName });
            qso.Details.Add(new QsoDetail { FieldName = "radio_label", FieldValue = activeRig.Label });
            if (!string.IsNullOrWhiteSpace(activeRig.Mode) && activeRig.Mode != "0")
                qso.Details.Add(new QsoDetail { FieldName = "radio_mode", FieldValue = activeRig.Mode });
            if (activeRig.FrequencyHz is long hz && hz > 0)
                qso.Details.Add(new QsoDetail { FieldName = "radio_freq_hz", FieldValue = hz.ToString(CultureInfo.InvariantCulture) });
        }

        errorMessage = string.Empty;
        return qso;
    }

    // ── Helpers ───────────────────────────────────────────────────────
    public void StampNow()
    {
        InputDate   = DateTime.UtcNow.ToString("yyyyMMdd");
        InputTimeOn = DateTime.UtcNow.ToString("HHmm");
    }

    public void PrepareForNextLogEntry()
    {
        InputCall = string.Empty;
        InputFieldDaySection = string.Empty;
        InputFieldDayClass = string.Empty;
    }

    public void RefreshActiveRigSnapshot()
    {
        RefreshConnectedRadios();
        var state = SelectedConnectedRadio?.State ?? App.RigctldConnectionManager.GetPrimaryActiveState();
        UpdateActiveRigDisplay(state);
    }

    public void RefreshSelectedRadioInputs()
    {
        RefreshActiveRigSnapshot();
        ApplySelectedRadioToInputs();
    }

    public void ApplySelectedRadioToInputs()
    {
        var state = SelectedConnectedRadio?.State;
        if (state is null || !state.IsConnected)
            return;

        if (!string.IsNullOrWhiteSpace(state.Mode) && state.Mode != "0")
            InputMode = state.Mode;

        if (state.FrequencyMhz is decimal mhz && mhz > 0)
        {
            InputFreq = mhz.ToString("0.000000", CultureInfo.InvariantCulture);

            var derivedBand = TryDeriveBandFromMhz(mhz);
            if (!string.IsNullOrWhiteSpace(derivedBand) && _bandValidator.Validate(derivedBand).IsValid)
                InputBand = derivedBand;
        }
    }

    private void ClearErrors()
    {
        CallError = BandError = ModeError = SectionError = ClassError = string.Empty;
    }

    private void ValidateBand()
    {
        var r = _bandValidator.Validate(InputBand.Trim().ToUpperInvariant());
        BandError = r.IsValid ? string.Empty : r.ErrorMessage;
    }

    private void ValidateMode()
    {
        var r = _modeValidator.Validate(InputMode.Trim().ToUpperInvariant());
        ModeError = r.IsValid ? string.Empty : r.ErrorMessage;
    }

    private void ValidateSection()
    {
        var r = _sectionValidator.Validate(InputFieldDaySection.Trim().ToUpperInvariant());
        SectionError = r.IsValid ? string.Empty : r.ErrorMessage;
    }

    private void ValidateClass()
    {
        var r = _classValidator.Validate(InputFieldDayClass.Trim().ToUpperInvariant());
        ClassError = r.IsValid ? string.Empty : r.ErrorMessage;
    }

    private void LoadProfiles()
    {
        AvailableProfiles.Clear();
        foreach (var key in _appConfig.Profiles.Keys)
            AvailableProfiles.Add(key);

        if (AvailableProfiles.Count == 0)
            AvailableProfiles.Add("default");

        var active = string.IsNullOrWhiteSpace(_appConfig.ActiveProfile)
            ? "default"
            : _appConfig.ActiveProfile;

        if (!AvailableProfiles.Contains(active))
            AvailableProfiles.Add(active);

        _selectedProfile = active;
        OnPropertyChanged(nameof(SelectedProfile));
    }

    private void PersistSelectedProfile()
    {
        if (string.IsNullOrWhiteSpace(_selectedProfile))
            return;

        if (!_appConfig.Profiles.ContainsKey(_selectedProfile))
            _appConfig.Profiles[_selectedProfile] = new ConfigProfile { Name = _selectedProfile };

        _appConfig.ActiveProfile = _selectedProfile;
        AppConfigurationStore.Save(_appConfig);
    }

    private void ApplyActiveRigSnapshot()
    {
        var state = GetSelectedOrPrimaryRigState();
        if (state is null || !state.IsConnected)
        {
            RefreshActiveRigSnapshot();
            return;
        }

        if (string.IsNullOrWhiteSpace(InputMode) && !string.IsNullOrWhiteSpace(state.Mode))
            InputMode = state.Mode;

        if (string.IsNullOrWhiteSpace(InputFreq) && state.FrequencyMhz is decimal mhz)
            InputFreq = mhz.ToString("0.000000", CultureInfo.InvariantCulture);

        RefreshActiveRigSnapshot();
    }

    private RadioRuntimeState? GetSelectedOrPrimaryRigState()
    {
        return SelectedConnectedRadio?.State ?? App.RigctldConnectionManager.GetPrimaryActiveState();
    }

    private void RefreshConnectedRadios()
    {
        var snapshot = App.RigctldConnectionManager.GetSnapshot();
        var selectedName = SelectedConnectedRadio?.RadioName;

        var connected = snapshot
            .Where(x => x.IsConnected)
            .OrderBy(x => x.RadioName, StringComparer.OrdinalIgnoreCase)
            .Select(x => new ConnectedRadioOption(x))
            .ToList();

        AvailableConnectedRadios = new ObservableCollection<ConnectedRadioOption>(connected);

        if (connected.Count == 0)
        {
            SelectedConnectedRadio = null;
            return;
        }

        SelectedConnectedRadio = connected.FirstOrDefault(x =>
            string.Equals(x.RadioName, selectedName, StringComparison.OrdinalIgnoreCase))
            ?? connected.First();
    }

    private void UpdateActiveRigDisplay(RadioRuntimeState? state)
    {
        if (state is null || !state.IsConnected)
        {
            ActiveRigStatus = "No active rig";
            ActiveRigLabel = "No active rig";
            ActiveRigMode = string.Empty;
            ActiveRigFrequency = string.Empty;
            IsActiveRigConnected = false;
            return;
        }

        ActiveRigStatus = "Connected";
        ActiveRigLabel = string.IsNullOrWhiteSpace(state.Label) ? state.RadioName : state.Label;
        ActiveRigMode = state.Mode ?? string.Empty;
        ActiveRigFrequency = state.FrequencyMhz is decimal mhz
            ? mhz.ToString("0.000000", CultureInfo.InvariantCulture) + " MHz"
            : string.Empty;
        IsActiveRigConnected = true;
    }

    private static string TryDeriveBandFromMhz(decimal mhz)
    {
        return mhz switch
        {
            >= 1.8m and <= 2.0m => "160M",
            >= 3.5m and <= 4.0m => "80M",
            >= 5.3305m and <= 5.4065m => "60M",
            >= 7.0m and <= 7.3m => "40M",
            >= 10.1m and <= 10.15m => "30M",
            >= 14.0m and <= 14.35m => "20M",
            >= 18.068m and <= 18.168m => "17M",
            >= 21.0m and <= 21.45m => "15M",
            >= 24.89m and <= 24.99m => "12M",
            >= 28.0m and <= 29.7m => "10M",
            >= 50.0m and <= 54.0m => "6M",
            >= 144.0m and <= 148.0m => "2M",
            >= 420.0m and <= 450.0m => "70CM",
            _ => string.Empty
        };
    }
}

/// <summary>Mutable detail row displayed in the QsoDetail DataGrid.</summary>
public sealed class QsoDetailRow : ViewModelBase
{
    private string _fieldName  = string.Empty;
    private string _fieldValue = string.Empty;

    public string FieldName  { get => _fieldName;  set => SetProperty(ref _fieldName,  value); }
    public string FieldValue { get => _fieldValue; set => SetProperty(ref _fieldValue, value); }
}

public sealed class ConnectedRadioOption
{
    public ConnectedRadioOption(RadioRuntimeState state)
    {
        State = state;
    }

    public RadioRuntimeState State { get; }
    public string RadioName => State.RadioName;
    public string Display
    {
        get
        {
            var name = State.RadioName?.Trim() ?? string.Empty;
            var label = State.Label?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(label))
                return name;
            if (string.Equals(label, name, StringComparison.OrdinalIgnoreCase))
                return name;
            if (label.Contains(name, StringComparison.OrdinalIgnoreCase))
                return label;
            if (name.Contains(label, StringComparison.OrdinalIgnoreCase))
                return name;

            return $"{label} ({name})";
        }
    }

    public override string ToString() => Display;
}

