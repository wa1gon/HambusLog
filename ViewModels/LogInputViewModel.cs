namespace HamBusLog.ViewModels;

using Avalonia.Threading;
using HamBusLog.Hardware;
using HamBusLog.Models;

public sealed class LogInputViewModel : ViewModelBase, IDisposable
{
    private static readonly IReadOnlyList<string> AvailableModesStatic =
    [
        "USB", "LSB", "FM", "AM", "FT8", "FT4", "FST4", "FST4W", "Q65", "JT65", "JT9", "MSK144", "WSPR",
        "RTTY", "PSK31", "PSK63", "OLIVIA", "JS8", "MFSK", "PACKET", "PKTUSB", "PKTLSB", "DIGU", "DIGL",
        "HELL", "THOR", "DOMINO", "DIGITAL", "CW"
    ];

    private readonly CallValidator   _callValidator   = new();
    private readonly BandValidator   _bandValidator   = new();
    private readonly ModeValidator   _modeValidator   = new();
    private readonly SectionValidator _sectionValidator = new();
    private readonly ClassValidator  _classValidator  = new();
    private bool _showAllContests;

    // ----- core fields -----
    private string _inputCall    = string.Empty;
    private string _inputDate    = string.Empty;
    private string _inputTimeOn  = string.Empty;
    private string _inputTimeOut = string.Empty;
    private string _inputBand    = string.Empty;
    private string _inputMode    = string.Empty;
    private string _inputFreq    = string.Empty;
    private string _inputSent    = string.Empty;
    private string _inputRec     = string.Empty;
    private string _inputCountry = string.Empty;
    private string _inputName    = string.Empty;
    private string _inputState   = string.Empty;
    private string _inputCounty  = string.Empty;
    private string _inputGrid    = string.Empty;
    private string _inputOperator = string.Empty;
    private string _inputExchange = string.Empty;
    private string _selectedContestKey = ContestCatalog.NormalKey;

    // ----- field day fields -----
    private string _inputFieldDaySection = string.Empty;
    private string _inputFieldDayClass   = string.Empty;

    // ----- validation -----
    private string _callError    = string.Empty;
    private string _bandError    = string.Empty;
    private string _modeError    = string.Empty;
    private string _sectionError = string.Empty;
    private string _classError   = string.Empty;
    private string _contestError = string.Empty;

    // ----- detail row being edited -----
    private string _newDetailField = string.Empty;
    private string _newDetailValue = string.Empty;
    private string _spotRemark = string.Empty;
    private QsoDetailRow? _selectedDetail;
    private AppConfiguration _appConfig = new();
    private string _selectedProfile = "default";
    private readonly ILogTypeSelectionService _logTypeSelectionService;
    private bool _isApplyingGlobalLogType;
    private readonly bool _wsjtAutoPopulateEnabled;
    private DateTime? _suspendRigAutoPopulateUntilUtc;
    private string _wsjtModeOverride = string.Empty;

    // ----- station / operator config -----
    private string _stationCallSign = string.Empty;
    private string _myLocation = string.Empty;
    private string _myStateProvince = string.Empty;
    private string _myGridSquare = string.Empty;
    private string _myLatitude = string.Empty;
    private string _myLongitude = string.Empty;
    private string _myItuZone = string.Empty;
    private string _myCqZone = string.Empty;
    private string _myFieldDaySection = string.Empty;
    private string _myFieldDayClass = string.Empty;

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
        _wsjtAutoPopulateEnabled = _appConfig.Wsjt.AutoPopulateLogInput;
        _logTypeSelectionService = App.LogTypeSelectionService;
        SelectActiveProfile();
        _contestDefinitions = ContestCatalog.GetAll().ToList();
        ApplyContestFilter();
        Details         = [];
        AvailableConnectedRadios = new ObservableCollection<ConnectedRadioOption>();
        _logTypeSelectionService.SelectedContestChanged += OnSelectedContestChanged;
        var storedContestKey = ActiveConfigProfile().LastContestKey;
        var initialContestKey = ResolveInitialContestKey(storedContestKey);
        var globalContestKey = _logTypeSelectionService.SelectedContestKey;
        if (FindContestDefinition(globalContestKey) is not null)
        {
            initialContestKey = globalContestKey;
        }

        SetSelectedContestKey(initialContestKey);
        LoadStationConfig();
        EnsureRstDefaults();
        var nowUtc = DateTime.UtcNow;
        InputDate       = nowUtc.ToString("yyyyMMdd");
        InputTimeOn     = nowUtc.ToString("HHmm");
        InputTimeOut    = nowUtc.ToString("HHmm");
        ApplyActiveRigSnapshot();

        if (_wsjtAutoPopulateEnabled)
            App.WsjtBridgeService.LoggedQsoReceived += OnWsjtLoggedQsoReceived;
    }

    // ── Properties ────────────────────────────────────────────────────
    private readonly List<ContestDefinition> _contestDefinitions;
    public IReadOnlyList<ContestDefinition> ContestDefinitions => _contestDefinitions;
    public IReadOnlyList<ContestDefinition> FilteredContestDefinitions { get; private set; } = [];
    public IReadOnlyList<string> AvailableModes => AvailableModesStatic;
    public bool ShowAllContests
    {
        get => _showAllContests;
        set
        {
            if (!SetProperty(ref _showAllContests, value))
                return;

            ApplyContestFilter();
            var initialContestKey = ResolveInitialContestKey(_selectedContestKey);
            SetSelectedContestKey(initialContestKey);
        }
    }
    public ObservableCollection<QsoDetailRow> Details { get; }

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

    public ContestType SelectedContestType
    {
        get => string.Equals(_selectedContestKey, ContestCatalog.ArrlFieldDayKey, StringComparison.OrdinalIgnoreCase)
            ? ContestType.ArrlFieldDay
            : ContestType.Normal;
        set
        {
            var nextKey = value == ContestType.ArrlFieldDay
                ? ContestCatalog.ArrlFieldDayKey
                : ContestCatalog.NormalKey;
            SetSelectedContestKey(nextKey);
        }
    }

    public ContestDefinition? SelectedContestDefinition
    {
        get => FindContestDefinition(_selectedContestKey);
        set
        {
            if (value is null)
                return;

            SetSelectedContestKey(value.Key);
        }
    }

    private void SetSelectedContestKey(string? contestKey)
    {
        var normalized = string.IsNullOrWhiteSpace(contestKey)
            ? ContestCatalog.NormalKey
            : contestKey.Trim();

        if (FindContestDefinition(normalized) is null)
            normalized = ContestCatalog.NormalKey;

        if (string.Equals(_selectedContestKey, normalized, StringComparison.OrdinalIgnoreCase))
            return;

        _selectedContestKey = normalized;
        OnPropertyChanged(nameof(SelectedContestType));
        OnPropertyChanged(nameof(SelectedContestDefinition));
        OnPropertyChanged(nameof(IsFieldDay));
        OnPropertyChanged(nameof(IsNormalContest));
        OnPropertyChanged(nameof(IsGeneralLogType));
        OnPropertyChanged(nameof(UsesUnifiedExchange));
        OnPropertyChanged(nameof(ShowLegacyNormalExchangeFields));
        OnPropertyChanged(nameof(ShowUnifiedExchangeField));
        OnPropertyChanged(nameof(SupplementalFieldsSectionTitle));
        OnPropertyChanged(nameof(ShowContestMetadata));
        OnPropertyChanged(nameof(ShowLegacyExchangeRequirementLabel));
        OnPropertyChanged(nameof(CurrentContestDefinition));
        OnPropertyChanged(nameof(CurrentContestDisplayName));
        OnPropertyChanged(nameof(CurrentContestAdifId));
        OnPropertyChanged(nameof(ShowExchange));
        OnPropertyChanged(nameof(ExchangeLabel));
        OnPropertyChanged(nameof(ExchangeHelpText));
        OnPropertyChanged(nameof(ShowExchangeHelp));
        OnPropertyChanged(nameof(ExchangeWatermark));
        OnPropertyChanged(nameof(ShowRstSent));
        OnPropertyChanged(nameof(ShowRstRecv));
        OnPropertyChanged(nameof(ShowCountry));
        OnPropertyChanged(nameof(ShowName));
        OnPropertyChanged(nameof(ShowState));
        OnPropertyChanged(nameof(ShowCounty));
        OnPropertyChanged(nameof(ShowGrid));
        OnPropertyChanged(nameof(ShowLocationFields));
        EnforceArkansasCountyRule();
        EnsureRstDefaults();

        if (!_isApplyingGlobalLogType)
            _logTypeSelectionService.SetSelectedContestKey(normalized);
    }

    private void OnSelectedContestChanged(object? sender, EventArgs e)
    {
        var selected = _logTypeSelectionService.SelectedContestKey;
        if (string.IsNullOrWhiteSpace(selected))
            selected = ContestCatalog.NormalKey;

        _isApplyingGlobalLogType = true;
        try
        {
            SetSelectedContestKey(selected);
        }
        finally
        {
            _isApplyingGlobalLogType = false;
        }
    }

    public bool IsNormalContest => CurrentContestDefinition.UsesNormalExchange;
    public bool IsFieldDay => CurrentContestDefinition.UsesFieldDayExchange;
    public bool IsGeneralLogType => IsContestKeyMatch(ContestCatalog.NormalKey);
    public IReadOnlyList<ContestFieldRequirement> EffectiveRequiredFields
        => IsArkansasQsoParty ? ArkansasQsoPartyRequiredFields : CurrentContestDefinition.RequiredFields;
    public bool UsesUnifiedExchange => EffectiveRequiredFields
        .Any(x => string.Equals(x.Key, ContestFieldKeys.Exchange, StringComparison.OrdinalIgnoreCase));
    public bool ShowLegacyNormalExchangeFields => IsNormalContest && !UsesUnifiedExchange;
    public bool ShowUnifiedExchangeField => UsesUnifiedExchange;
    public bool ShowExchange => ShowUnifiedExchangeField;
    public bool ShowRstSent => IsNormalContest || HasRequiredField(ContestFieldKeys.RstSent);
    public bool ShowRstRecv => IsNormalContest || HasRequiredField(ContestFieldKeys.RstRecv);
    public bool ShowLocationFields => IsNormalContest
    || HasRequiredField(ContestFieldKeys.Country)
    || HasRequiredField(ContestFieldKeys.State)
    || HasRequiredField(ContestFieldKeys.County);
    public bool ShowCountry => ShowLocationFields;
    public bool ShowName => ShowLocationFields;
    public bool ShowState => ShowLocationFields && ShowLegacyNormalExchangeFields;
    public bool ShowCounty => ShowLocationFields && ShowLegacyNormalExchangeFields;
    public bool ShowGrid => ShowLocationFields;
    public bool ShowFieldDaySection => HasRequiredField(ContestFieldKeys.FieldDaySection);
    public bool ShowFieldDayClass => HasRequiredField(ContestFieldKeys.FieldDayClass);
    public string SupplementalFieldsSectionTitle => IsGeneralLogType ? "OPTIONAL FIELDS" : "CONTEST REQUIREMENTS";
    public bool ShowContestMetadata => !IsGeneralLogType;
    public bool ShowLegacyExchangeRequirementLabel => !IsGeneralLogType && ShowLegacyNormalExchangeFields;
    public ContestDefinition CurrentContestDefinition => FindContestDefinition(_selectedContestKey)
        ?? ContestCatalog.Get(ContestType.Normal);
    public string CurrentContestDisplayName => CurrentContestDefinition.DisplayName;
    public string CurrentContestAdifId => CurrentContestDefinition.AdifContestId;
    public string ExchangeLabel => IsArkansasQsoParty
        ? (IsArkansasStation ? "County" : "State")
        : "Exchange";
    public string ExchangeHelpText => BuildExchangeHelpText();
    public bool ShowExchangeHelp => IsArkansasQsoParty && ShowUnifiedExchangeField;
    public string ExchangeWatermark => BuildExchangeWatermark();

    public string InputCall
    {
        get => _inputCall;
        set => SetProperty(ref _inputCall, (value ?? string.Empty).ToUpperInvariant());
    }
    public string InputDate    { get => _inputDate;    set => SetProperty(ref _inputDate,    value); }
    public string InputTimeOn  { get => _inputTimeOn;  set => SetProperty(ref _inputTimeOn,  value); }
    public string InputTimeOut { get => _inputTimeOut; set => SetProperty(ref _inputTimeOut, value); }
     public string InputBand    { get => _inputBand;    set { if (SetProperty(ref _inputBand, value)) ValidateBand(); } }
     public string InputMode    { get => _inputMode;    set { if (SetProperty(ref _inputMode, (value ?? string.Empty).ToUpperInvariant())) ValidateMode(); } }
     public string InputFreq    { get => _inputFreq;    set => SetProperty(ref _inputFreq,    value); }
    public string InputSent    { get => _inputSent;    set => SetProperty(ref _inputSent,    value); }
    public string InputRec     { get => _inputRec;     set => SetProperty(ref _inputRec,     value); }
    public string InputCountry { get => _inputCountry; set => SetProperty(ref _inputCountry, (value ?? string.Empty).ToUpperInvariant()); }
    public string InputName    { get => _inputName;    set => SetProperty(ref _inputName,    value ?? string.Empty); }
    public string InputState
    {
        get => _inputState;
        set
        {
            if (SetProperty(ref _inputState, (value ?? string.Empty).ToUpperInvariant()))
                EnforceArkansasCountyRule();
        }
    }

    public string InputCounty
    {
        get => _inputCounty;
        set
        {
            if (SetProperty(ref _inputCounty, (value ?? string.Empty).ToUpperInvariant()))
                EnforceArkansasCountyRule();
        }
    }

    public string InputGrid
    {
        get => _inputGrid;
        set => SetProperty(ref _inputGrid, (value ?? string.Empty).ToUpperInvariant());
    }

    public void ApplyLookupResult(CallsignLookupResult result)
    {
        if (result is null)
            return;

        if (string.IsNullOrWhiteSpace(InputCall) && !string.IsNullOrWhiteSpace(result.CallSign))
            InputCall = result.CallSign;

        if (string.IsNullOrWhiteSpace(InputCountry) && !string.IsNullOrWhiteSpace(result.Country))
            InputCountry = result.Country;

        if (string.IsNullOrWhiteSpace(InputName) && !string.IsNullOrWhiteSpace(result.Name))
            InputName = result.Name;

        if (string.IsNullOrWhiteSpace(InputState) && !string.IsNullOrWhiteSpace(result.State))
            InputState = result.State;

        if (string.IsNullOrWhiteSpace(InputCounty) && !string.IsNullOrWhiteSpace(result.County))
            InputCounty = result.County;

        if (string.IsNullOrWhiteSpace(InputGrid) && !string.IsNullOrWhiteSpace(result.Grid))
            InputGrid = result.Grid;
    }

    public string InputOperator { get => _inputOperator; set => SetProperty(ref _inputOperator, (value ?? string.Empty).ToUpperInvariant()); }
    public string InputExchange { get => _inputExchange; set => SetProperty(ref _inputExchange, (value ?? string.Empty).ToUpperInvariant()); }
    public string StationCallSign => _stationCallSign.Trim().ToUpperInvariant();
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
    public string SpotRemark { get => _spotRemark; set => SetProperty(ref _spotRemark, value ?? string.Empty); }

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
    public string ContestError { get => _contestError; private set { if (SetProperty(ref _contestError, value)) OnPropertyChanged(nameof(HasContestError)); } }

    public bool HasCallError    => !string.IsNullOrWhiteSpace(CallError);
    public bool HasBandError    => !string.IsNullOrWhiteSpace(BandError);
    public bool HasModeError    => !string.IsNullOrWhiteSpace(ModeError);
    public bool HasSectionError => !string.IsNullOrWhiteSpace(SectionError);
    public bool HasClassError   => !string.IsNullOrWhiteSpace(ClassError);
    public bool HasContestError => !string.IsNullOrWhiteSpace(ContestError);

    public bool TryGetDuplicateCallWarning(out string warning)
    {
        warning = string.Empty;

        var call = InputCall.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(call))
            return false;

        var band = ResolveBandForDuplicate(InputBand, TryParseFrequencyMhz(InputFreq));
        var mode = InputMode.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(band) || string.IsNullOrWhiteSpace(mode))
            return false;

        if (!HasDuplicateQso(call, band, mode, InputFreq))
            return false;

        warning = $"Possible duplicate: {call} is already logged"
                  + $" on {band}"
                  + $" ({NormalizeModeFamilyForDuplicate(mode)})"
                  + ".";
        return true;
    }

    private static string NormalizeText(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    private static string NormalizeBandForDuplicate(string? band)
    {
        var normalized = NormalizeText(band)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        if (Regex.IsMatch(normalized, @"^\d+(\.\d+)?$"))
            return normalized + "M";

        return normalized;
    }

    private static bool IsBandMatchForDuplicate(string? existingBand, string? inputBand)
        => string.Equals(
            NormalizeBandForDuplicate(existingBand),
            NormalizeBandForDuplicate(inputBand),
            StringComparison.OrdinalIgnoreCase);

    private static decimal? TryParseFrequencyMhz(string? frequencyText)
    {
        if (!decimal.TryParse(frequencyText, NumberStyles.Number, CultureInfo.InvariantCulture, out var mhz) || mhz <= 0)
            return null;

        return mhz;
    }

    private static string ResolveBandForDuplicate(string? band, decimal? frequencyMhz)
    {
        var normalizedBand = NormalizeBandForDuplicate(band);
        if (!string.IsNullOrWhiteSpace(normalizedBand))
            return normalizedBand;

        if (frequencyMhz is not decimal mhz || mhz <= 0)
            return string.Empty;

        var derivedBand = TryDeriveBandFromMhz(mhz);
        return NormalizeBandForDuplicate(derivedBand);
    }

    private static string NormalizeModeFamilyForDuplicate(string? mode)
    {
        var normalized = NormalizeText(mode);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        if (normalized is "SSB" or "USB" or "LSB" or "AM" or "FM" or "NFM" or "WFM" or "PHONE" or "VOICE")
            return "PHONE";

        if (normalized.Contains("SSB", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("VOICE", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("PHONE", StringComparison.OrdinalIgnoreCase))
            return "PHONE";

        if (normalized is "PKTUSB" or "PKTLSB" or "DIGU" or "DIGL" or "RTTY" or "DATA" or "DATA-U" or "DATA-L"
            or "PSK" or "PSK31" or "PSK63" or "PSK125" or "FT8" or "FT4" or "JT65" or "JT9" or "Q65"
            or "MFSK" or "MSK144" or "FSK" or "OLIVIA" or "THOR" or "DOMINO" or "VARA" or "PACKET")
            return "DIGITAL";

        if (normalized.StartsWith("FT", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("JT", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("PSK", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("DIG", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("DATA", StringComparison.OrdinalIgnoreCase))
            return "DIGITAL";

        return normalized;
    }

    private static bool IsModeMatchForDuplicate(string? existingMode, string? inputMode)
        => string.Equals(
            NormalizeModeFamilyForDuplicate(existingMode),
            NormalizeModeFamilyForDuplicate(inputMode),
            StringComparison.OrdinalIgnoreCase);

    private static bool HasDuplicateQso(string call, string band, string mode, string? frequencyText)
    {
        var normalizedCall = NormalizeText(call);
        var normalizedBand = ResolveBandForDuplicate(band, TryParseFrequencyMhz(frequencyText));
        var targetModeFamily = NormalizeModeFamilyForDuplicate(mode);
        if (string.IsNullOrWhiteSpace(normalizedCall)
            || string.IsNullOrWhiteSpace(normalizedBand)
            || string.IsNullOrWhiteSpace(targetModeFamily))
            return false;

        var candidates = App.DbContext.Qsos
            .AsNoTracking()
            .Where(q => q.Call.Trim().ToUpper() == normalizedCall)
            .Select(q => new { q.Band, q.Mode, q.Freq })
            .ToList();

        return candidates.Any(candidate
            => IsBandMatchForDuplicate(ResolveBandForDuplicate(candidate.Band, candidate.Freq), normalizedBand)
               && IsModeMatchForDuplicate(candidate.Mode, mode));
    }

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

        if (HasDuplicateQso(InputCall, band, mode, InputFreq))
        {
            var dupBand = ResolveBandForDuplicate(band, TryParseFrequencyMhz(InputFreq));
            ContestError = $"Duplicate QSO not allowed: {InputCall.Trim().ToUpperInvariant()} on {dupBand} ({NormalizeModeFamilyForDuplicate(mode)}).";
            errorMessage = ContestError;
            return null;
        }

        if (IsFieldDay)
        {
            var sec = InputFieldDaySection.Trim().ToUpperInvariant();
            var cls = InputFieldDayClass.Trim().ToUpperInvariant();
            var sr = _sectionValidator.Validate(sec);
            if (!sr.IsValid) { SectionError = sr.ErrorMessage; errorMessage = SectionError; return null; }
            var cr = _classValidator.Validate(cls);
            if (!cr.IsValid) { ClassError = cr.ErrorMessage; errorMessage = ClassError; return null; }
        }

        if (!TryValidateContestRequiredFields(out var contestError))
        {
            ContestError = contestError;
            errorMessage = contestError;
            return null;
        }

        var qsoDateUtc = ResolveQsoDateUtc();
        if (!IsWithinContestWindow(qsoDateUtc, CurrentContestDefinition, out var windowError))
        {
            ContestError = windowError;
            errorMessage = windowError;
            return null;
        }

        var freq = decimal.TryParse(InputFreq, System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0m;

        var normalizedState = InputState.Trim().ToUpperInvariant();
        if (UsesUnifiedExchange && TryParseUnifiedExchange(InputExchange, out var normalizedExchange, out var isCounty) && !isCounty)
            normalizedState = normalizedExchange;

        var qso = new Qso
        {
            Id      = Guid.NewGuid(),
            Call    = InputCall.Trim().ToUpperInvariant(),
            StationCallSign = _stationCallSign.Trim().ToUpperInvariant(),
            QsoDate = qsoDateUtc,
            Band    = band,
            Mode    = mode,
            ContestId = CurrentContestAdifId,
            Freq    = freq,
            Country = InputCountry.Trim().ToUpperInvariant(),
            State   = normalizedState,
            RstSent = IsFieldDay ? string.Empty : InputSent.Trim(),
            RstRcvd = IsFieldDay ? string.Empty : InputRec.Trim(),
            Details = new List<QsoDetail>()
        };

        // copy detail rows
        foreach (var row in Details)
            qso.Details.Add(new QsoDetail { FieldName = row.FieldName, FieldValue = row.FieldValue });

        var operatorCall = string.IsNullOrWhiteSpace(InputOperator)
            ? qso.StationCallSign
            : InputOperator.Trim().ToUpperInvariant();
        qso.Details.Add(new QsoDetail { FieldName = "OPERATOR", FieldValue = operatorCall });

        ApplyContestExchangeToQsoDetails(qso);
        AddDetailIfMissing(qso.Details, "COUNTY", InputCounty);
        AddDetailIfMissing(qso.Details, "GRID", InputGrid);

        errorMessage = string.Empty;
        return qso;
    }

    // ── Helpers ───────────────────────────────────────────────────────
    public void StampNow()
    {
        var nowUtc = DateTime.UtcNow;
        InputDate   = nowUtc.ToString("yyyyMMdd");
        InputTimeOn = nowUtc.ToString("HHmm");
        InputTimeOut = nowUtc.ToString("HHmm");
    }

    public void SetInitialCallsign(string? callsign)
    {
        var normalized = callsign?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        InputCall = normalized;
    }

    public void SetInitialSpot(string? callsign, decimal? frequencyMhz, string? spotInfo = null)
    {
        SetInitialCallsign(callsign);

        if (frequencyMhz is not decimal mhz || mhz <= 0)
            return;

        InputFreq = mhz.ToString("0.000", CultureInfo.InvariantCulture);
        var derivedBand = TryDeriveBandFromMhz(mhz);
        if (!string.IsNullOrWhiteSpace(derivedBand) && _bandValidator.Validate(derivedBand).IsValid)
            InputBand = derivedBand;

        var derivedMode = TryDeriveModeFromSpotInfo(spotInfo, mhz);
        if (string.IsNullOrWhiteSpace(derivedMode))
            derivedMode = TryDeriveModeFromMhz(mhz);
        if (!string.IsNullOrWhiteSpace(derivedMode))
            InputMode = derivedMode;

        // Always prefer live rig updates on the refresh cadence.
        EnableAutoRadioPopulate();
    }

    public void EnableAutoRadioPopulate()
    {
        // No-op: live radio values are always applied when a connected rig is selected.
    }

    public void PrepareForNextLogEntry()
    {
        InputCall = string.Empty;
        InputSent = string.Empty;
        InputRec = string.Empty;
        InputCountry = string.Empty;
        InputName = string.Empty;
        InputState = string.Empty;
        InputCounty = string.Empty;
        InputGrid = string.Empty;
        InputOperator = StationCallSign;
        InputExchange = string.Empty;
        InputFieldDaySection = string.Empty;
        InputFieldDayClass = string.Empty;
        SpotRemark = string.Empty;
        EnsureRstDefaults();
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

    public void RefreshAutoFields()
    {
        if (!IsRigAutoPopulateSuspended())
            RefreshSelectedRadioInputs();

        InputTimeOut = DateTime.UtcNow.ToString("HHmm");
    }

    public void ApplyWsjtLoggedQso(WsjtLoggedQso qso)
    {
        if (qso is null || string.IsNullOrWhiteSpace(qso.Call))
            return;

        InputCall = qso.Call;

        if (qso.TimeOnUtc is DateTimeOffset timeOnUtc)
        {
            InputDate = timeOnUtc.UtcDateTime.ToString("yyyyMMdd");
            InputTimeOn = timeOnUtc.UtcDateTime.ToString("HHmm");
        }

        if (!string.IsNullOrWhiteSpace(qso.Band))
            InputBand = qso.Band;

        var mode = NormalizeWsjtMode(qso.Mode, qso.Submode);
        if (!string.IsNullOrWhiteSpace(mode))
        {
            InputMode = mode;
            _wsjtModeOverride = mode.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(qso.FreqMhz))
            InputFreq = qso.FreqMhz;

        if (!string.IsNullOrWhiteSpace(qso.RstSent))
            InputSent = qso.RstSent;

        if (!string.IsNullOrWhiteSpace(qso.RstRcvd))
            InputRec = qso.RstRcvd;

        if (!string.IsNullOrWhiteSpace(qso.Country))
            InputCountry = qso.Country;

        if (!string.IsNullOrWhiteSpace(qso.Name))
            InputName = qso.Name;

        if (!string.IsNullOrWhiteSpace(qso.State))
            InputState = qso.State;

        if (!string.IsNullOrWhiteSpace(qso.County))
            InputCounty = qso.County;

        if (!string.IsNullOrWhiteSpace(qso.GridSquare))
            InputGrid = qso.GridSquare;

        if (!string.IsNullOrWhiteSpace(qso.ExchangeReceived) && string.IsNullOrWhiteSpace(InputExchange))
            InputExchange = qso.ExchangeReceived;

        var operatorCall = !string.IsNullOrWhiteSpace(qso.Operator)
            ? qso.Operator
            : qso.StationCallsign;
        if (!string.IsNullOrWhiteSpace(operatorCall))
            InputOperator = operatorCall;

        // Keep recent WSJT values stable against the periodic rig autofill refresh.
        _suspendRigAutoPopulateUntilUtc = DateTime.UtcNow.AddMinutes(2);
    }

    public void ApplySelectedRadioToInputs()
    {

        var state = SelectedConnectedRadio?.State;
        if (state is null || !state.IsConnected)
            return;

        var preserveRecentWsjtValues = IsRigAutoPopulateSuspended();

        if (string.IsNullOrWhiteSpace(_wsjtModeOverride)
            && !preserveRecentWsjtValues
            && !string.IsNullOrWhiteSpace(state.Mode)
            && state.Mode != "0")
            InputMode = NormalizeRigModeForInput(state.Mode, state.FrequencyMhz);

        if (!preserveRecentWsjtValues && state.FrequencyMhz is decimal mhz && mhz > 0)
        {
            InputFreq = mhz.ToString("0.000000", CultureInfo.InvariantCulture);

            var derivedBand = TryDeriveBandFromMhz(mhz);
            if (!string.IsNullOrWhiteSpace(derivedBand) && _bandValidator.Validate(derivedBand).IsValid)
                InputBand = derivedBand;
        }
    }

    private bool IsRigAutoPopulateSuspended()
        => _suspendRigAutoPopulateUntilUtc is DateTime pauseUntil && DateTime.UtcNow < pauseUntil;

    private void ClearErrors()
    {
        CallError = BandError = ModeError = SectionError = ClassError = ContestError = string.Empty;
    }

    private bool TryValidateContestRequiredFields(out string errorMessage)
    {
        foreach (var requirement in EffectiveRequiredFields)
        {
            if (IsArkansasQsoParty
                && string.Equals(requirement.Key, ContestFieldKeys.State, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(_inputState) && !string.IsNullOrWhiteSpace(_inputCounty))
                {
                    _inputState = "AR";
                    OnPropertyChanged(nameof(InputState));
                }

                if (IsArkansasStation || IsInputStateArkansas())
                    continue;
            }

            if (IsArkansasQsoParty
                && string.Equals(requirement.Key, ContestFieldKeys.County, StringComparison.OrdinalIgnoreCase))
            {
                if (!IsInputStateArkansas())
                    continue;

                var county = InputCounty.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(county))
                {
                    errorMessage = "County is required when State is AR.";
                    return false;
                }

                if (!IsValidArkansasCountyCode(county))
                {
                    errorMessage = "County must be 3 letters (A-Z).";
                    return false;
                }

                continue;
            }

            if (!string.IsNullOrWhiteSpace(GetContestFieldValue(requirement.Key)))
                continue;

            errorMessage = $"{requirement.Label} is required for {CurrentContestDisplayName}.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private string GetContestFieldValue(string key)
    {
        return key switch
        {
            ContestFieldKeys.Exchange => InputExchange.Trim(),
            ContestFieldKeys.RstSent => InputSent.Trim(),
            ContestFieldKeys.RstRecv => InputRec.Trim(),
            ContestFieldKeys.Country => InputCountry.Trim(),
            ContestFieldKeys.Name => InputName.Trim(),
            ContestFieldKeys.State => InputState.Trim(),
            ContestFieldKeys.County => InputCounty.Trim(),
            ContestFieldKeys.FieldDaySection => InputFieldDaySection.Trim(),
            ContestFieldKeys.FieldDayClass => InputFieldDayClass.Trim(),
            _ => string.Empty
        };
    }

    private bool HasRequiredField(string key)
    {
        return EffectiveRequiredFields.Any(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyContestExchangeToQsoDetails(Qso qso)
    {
        foreach (var requirement in EffectiveRequiredFields)
        {
            if (IsArkansasQsoParty
                && string.Equals(requirement.Key, ContestFieldKeys.County, StringComparison.OrdinalIgnoreCase))
            {
                var county = InputCounty.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(county))
                    qso.Details.Add(new QsoDetail { FieldName = "County", FieldValue = county });

                continue;
            }

            if (string.Equals(requirement.Key, ContestFieldKeys.Exchange, StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseUnifiedExchange(InputExchange, out var normalizedExchange, out var isCounty) && isCounty)
                    qso.Details.Add(new QsoDetail { FieldName = "County", FieldValue = normalizedExchange });

                continue;
            }

            if (string.IsNullOrWhiteSpace(requirement.DetailFieldName))
                continue;

            var value = GetContestFieldValue(requirement.Key);
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var normalized = requirement.Key == ContestFieldKeys.Name ? value : value.ToUpperInvariant();
            qso.Details.Add(new QsoDetail { FieldName = requirement.DetailFieldName, FieldValue = normalized });
        }
    }

    private static void AddDetailIfMissing(ICollection<QsoDetail> details, string fieldName, string? value)
    {
        if (details is null || string.IsNullOrWhiteSpace(fieldName))
            return;

        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        var exists = details.Any(x => string.Equals(x.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));
        if (!exists)
            details.Add(new QsoDetail { FieldName = fieldName, FieldValue = normalized.ToUpperInvariant() });
    }

    private static bool TryParseUnifiedExchange(string? rawExchange, out string normalized, out bool isCounty)
    {
        normalized = (rawExchange ?? string.Empty).Trim().ToUpperInvariant();
        isCounty = false;

        if (normalized.Length != 2 && normalized.Length != 3)
            return false;

        if (!normalized.All(char.IsLetter))
            return false;

        isCounty = normalized.Length == 3;
        return true;
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

    private void SelectActiveProfile()
    {
        var active = string.IsNullOrWhiteSpace(_appConfig.ActiveProfile)
            ? "default"
            : _appConfig.ActiveProfile;

        _selectedProfile = active;
    }

    private void LoadStationConfig()
    {
        var p = ActiveConfigProfile();
        _stationCallSign    = p.StationCallSign;
        _myLocation         = p.MyLocation;
        _myStateProvince    = p.MyStateProvince;
        _myGridSquare       = p.MyGridSquare;
        _myLatitude         = p.MyLatitude;
        _myLongitude        = p.MyLongitude;
        _myItuZone          = p.MyItuZone;
        _myCqZone           = p.MyCqZone;
        _myFieldDaySection  = p.MyFieldDaySection;
        _myFieldDayClass    = p.MyFieldDayClass;
        InputOperator = StationCallSign;
        OnPropertyChanged(nameof(StationCallSign));
    }

    private bool IsArkansasQsoParty
        => IsContestKeyMatch("AR-QSO-PARTY") || IsContestKeyMatch("ARQP");

    private bool IsArkansasStation
        => string.Equals(_myStateProvince.Trim(), "AR", StringComparison.OrdinalIgnoreCase);

    private bool IsInputStateArkansas()
        => string.Equals(_inputState.Trim(), "AR", StringComparison.OrdinalIgnoreCase);

    private bool IsContestKeyMatch(string key)
    {
        return string.Equals(CurrentContestDefinition.Key, key, StringComparison.OrdinalIgnoreCase)
               || string.Equals(CurrentContestDefinition.AdifContestId, key, StringComparison.OrdinalIgnoreCase);
    }

    private ContestDefinition? FindContestDefinition(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
                return _contestDefinitions.FirstOrDefault(x =>
                string.Equals(x.Key, ContestCatalog.NormalKey, StringComparison.OrdinalIgnoreCase));

        var trimmed = key.Trim();
        return _contestDefinitions.FirstOrDefault(x =>
            string.Equals(x.Key, trimmed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.AdifContestId, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyContestFilter()
    {
        var filtered = _contestDefinitions.ToList();
        if (!_showAllContests)
        {
            var nowUtc = DateTime.UtcNow;
            filtered = filtered
                .Where(x => IsContestActive(nowUtc, x))
                .ToList();
        }

        var selected = FindContestDefinition(_selectedContestKey)
                       ?? FindContestDefinition(ActiveConfigProfile().LastContestKey)
                       ?? FindContestDefinition(_logTypeSelectionService.SelectedContestKey);
        if (selected is not null && filtered.All(x => !string.Equals(x.Key, selected.Key, StringComparison.OrdinalIgnoreCase)))
            filtered.Add(selected);

        if (filtered.Count == 0)
            filtered = _contestDefinitions.ToList();

        FilteredContestDefinitions = filtered;
        OnPropertyChanged(nameof(FilteredContestDefinitions));
    }

    private string ResolveInitialContestKey(string? storedContestKey)
    {
        if (!string.IsNullOrWhiteSpace(storedContestKey))
        {
            var stored = storedContestKey.Trim();
            var match = _contestDefinitions.FirstOrDefault(x =>
                string.Equals(x.Key, stored, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.AdifContestId, stored, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match.Key;
        }

        return FilteredContestDefinitions.FirstOrDefault()?.Key
               ?? _contestDefinitions.FirstOrDefault()?.Key
               ?? ContestCatalog.NormalKey;
    }

    private static bool IsContestActive(DateTime nowUtc, ContestDefinition contest)
    {
        if (contest.StartUtc is DateTime startUtc && nowUtc < startUtc)
            return false;
        if (contest.EndUtc is DateTime endUtc && nowUtc > endUtc)
            return false;
        return true;
    }

    private void EnsureRstDefaults()
    {
        if (ShowRstSent && string.IsNullOrWhiteSpace(InputSent))
            InputSent = "59";
        if (ShowRstRecv && string.IsNullOrWhiteSpace(InputRec))
            InputRec = "59";
    }

    private bool TryValidateArkansasQsoPartyExchange(out string errorMessage)
    {
        if (!TryParseUnifiedExchange(InputExchange, out var normalized, out _))
        {
            errorMessage = "Exchange must be 2 letters (state/province) or 3 letters (Arkansas county).";
            return false;
        }

        if (IsArkansasStation)
        {
            if (normalized.Length != 3)
            {
                errorMessage = "In-state exchange must be a 3-letter Arkansas county.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        if (normalized.Length != 2)
        {
            errorMessage = "Out-of-state exchange must be a 2-letter state/province.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static readonly IReadOnlyList<ContestFieldRequirement> ArkansasQsoPartyRequiredFields =
    [
        new ContestFieldRequirement(ContestFieldKeys.RstSent, "RST Sent"),
        new ContestFieldRequirement(ContestFieldKeys.RstRecv, "RST Rec"),
        new ContestFieldRequirement(ContestFieldKeys.State, "State"),
        new ContestFieldRequirement(ContestFieldKeys.County, "County")
    ];

    private string BuildExchangeHelpText()
    {
        if (!IsArkansasQsoParty)
            return string.Empty;

        return IsArkansasStation
            ? "In-state exchange: Arkansas county (3 letters, e.g., PUL)."
            : "Out-of-state exchange: state/province (2 letters, e.g., MA).";
    }

    private string BuildExchangeWatermark()
    {
        if (!IsArkansasQsoParty)
            return "MA or PUL";

        return IsArkansasStation ? "PUL" : "MA";
    }

    private DateTime ResolveQsoDateUtc()
    {
        var raw = InputDate + " " + InputTimeOn;
        if (DateTime.TryParseExact(raw, "yyyyMMdd HHmm", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            return parsed;

        return DateTime.UtcNow;
    }

    private static bool IsWithinContestWindow(DateTime qsoUtc, ContestDefinition contest, out string error)
    {
        if (contest.StartUtc is null && contest.EndUtc is null)
        {
            error = string.Empty;
            return true;
        }

        if (contest.StartUtc is DateTime startUtc && qsoUtc < startUtc)
        {
            error = $"QSO time is before the contest start for {contest.DisplayName}.";
            return false;
        }

        if (contest.EndUtc is DateTime endUtc && qsoUtc > endUtc)
        {
            error = $"QSO time is after the contest end for {contest.DisplayName}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void EnforceArkansasCountyRule()
    {
        if (!IsArkansasQsoParty)
            return;

        if (IsArkansasStation && string.IsNullOrWhiteSpace(_inputState))
        {
            _inputState = "AR";
            OnPropertyChanged(nameof(InputState));
        }

        var state = _inputState.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(state) || state.Length < 2)
            return;

        if (string.Equals(state, "AR", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.IsNullOrWhiteSpace(_inputCounty))
            return;

        _inputCounty = string.Empty;
        OnPropertyChanged(nameof(InputCounty));
    }

    private static bool IsValidArkansasCountyCode(string county)
    {
        return county.Length == 3 && county.All(char.IsLetter);
    }

    private ConfigProfile ActiveConfigProfile()
    {
        var key = string.IsNullOrWhiteSpace(_selectedProfile) ? "default" : _selectedProfile;
        if (!_appConfig.Profiles.TryGetValue(key, out var profile))
        {
            profile = new ConfigProfile { Name = key };
            _appConfig.Profiles[key] = profile;
        }
        return profile;
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
            InputMode = NormalizeRigModeForInput(state.Mode, state.FrequencyMhz);

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
        var config = AppConfigurationStore.Load();
        var rigctld = AppConfigurationStore.GetRigctld(config);

        var activeRadioNames = rigctld.ActiveRadioNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var radio in rigctld.Radios.Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.RadioName)))
            activeRadioNames.Add(radio.RadioName);

        var hasActiveFilter = activeRadioNames.Count > 0;

        var connected = snapshot
            .Where(x => x.IsConnected && (!hasActiveFilter || activeRadioNames.Contains(x.RadioName)))
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

    private static string TryDeriveModeFromSpotInfo(string? info, decimal mhz)
    {
        if (string.IsNullOrWhiteSpace(info))
            return string.Empty;

        var text = info.Trim().ToUpperInvariant();

        if (Regex.IsMatch(text, @"\bFT8\b")) return "FT8";
        if (Regex.IsMatch(text, @"\bFT4\b")) return "FT4";
        if (Regex.IsMatch(text, @"\bRTTY\b")) return "RTTY";
        if (Regex.IsMatch(text, @"\bPSK\d*\b")) return "DIGU";
        if (Regex.IsMatch(text, @"\bCW\b")) return "CW";
        if (Regex.IsMatch(text, @"\bUSB\b")) return "USB";
        if (Regex.IsMatch(text, @"\bLSB\b")) return "LSB";
        if (Regex.IsMatch(text, @"\bAM\b")) return "AM";
        if (Regex.IsMatch(text, @"\bFM\b")) return "FM";
        if (Regex.IsMatch(text, @"\bSSB\b")) return mhz < 10m ? "LSB" : "USB";

        return string.Empty;
    }

    private static string TryDeriveModeFromMhz(decimal mhz)
    {
        // Common weak-signal digital calling frequencies.
        if (IsNear(mhz, 1.840m) || IsNear(mhz, 3.573m) || IsNear(mhz, 5.357m)
            || IsNear(mhz, 7.074m) || IsNear(mhz, 10.136m) || IsNear(mhz, 14.074m)
            || IsNear(mhz, 18.100m) || IsNear(mhz, 21.074m) || IsNear(mhz, 24.915m)
            || IsNear(mhz, 28.074m) || IsNear(mhz, 50.313m) || IsNear(mhz, 144.174m))
            return "FT8";

        if (IsNear(mhz, 3.575m) || IsNear(mhz, 7.0475m) || IsNear(mhz, 10.140m)
            || IsNear(mhz, 14.080m) || IsNear(mhz, 18.104m) || IsNear(mhz, 21.140m)
            || IsNear(mhz, 24.919m) || IsNear(mhz, 28.180m) || IsNear(mhz, 50.318m))
            return "FT4";

        // Typical CW portions across HF bands.
        if ((mhz >= 1.8m && mhz <= 2.0m)
            || (mhz >= 3.5m && mhz <= 3.6m)
            || (mhz >= 7.0m && mhz <= 7.1m)
            || (mhz >= 10.1m && mhz <= 10.15m)
            || (mhz >= 14.0m && mhz <= 14.07m)
            || (mhz >= 18.068m && mhz <= 18.1m)
            || (mhz >= 21.0m && mhz <= 21.07m)
            || (mhz >= 24.89m && mhz <= 24.92m)
            || (mhz >= 28.0m && mhz <= 28.07m))
            return "CW";

        // Broad voice-mode defaults when no better hint exists.
        if (mhz >= 28m && mhz < 54m)
            return "USB";
        if (mhz >= 50m)
            return "FM";
        if (mhz >= 1.8m && mhz < 10m)
            return "LSB";
        if (mhz >= 10m)
            return "USB";

        return string.Empty;
    }

    private static bool IsNear(decimal mhz, decimal target, decimal tolerance = 0.003m)
        => Math.Abs(mhz - target) <= tolerance;

    private static string NormalizeWsjtMode(string mode, string submode)
    {
        var normalizedSubmode = (submode ?? string.Empty).Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedSubmode))
            return normalizedSubmode;

        var normalizedMode = (mode ?? string.Empty).Trim().ToUpperInvariant();
        return normalizedMode;
    }

    private static string NormalizeRigModeForInput(string? rawMode, decimal? frequencyMhz)
    {
        var mode = (rawMode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(mode) || mode == "0")
            return string.Empty;

        // Keep the mode picker vocabulary aligned with AvailableModes.
        return mode switch
        {
            "PKTUSB" or "PKTLSB" or "DIGU" or "DIGL" => ResolveDigitalSubmodeFromFrequency(frequencyMhz),
            _ => mode
        };
    }

    private static string ResolveDigitalSubmodeFromFrequency(decimal? frequencyMhz)
    {
        if (frequencyMhz is decimal mhz && mhz > 0)
        {
            // FT8 centers
            if (IsNear(mhz, 1.840m) || IsNear(mhz, 3.573m) || IsNear(mhz, 5.357m)
                || IsNear(mhz, 7.074m) || IsNear(mhz, 10.136m) || IsNear(mhz, 14.074m)
                || IsNear(mhz, 18.100m) || IsNear(mhz, 21.074m) || IsNear(mhz, 24.915m)
                || IsNear(mhz, 28.074m) || IsNear(mhz, 50.313m) || IsNear(mhz, 144.174m))
                return "FT8";

            // FT4 centers
            if (IsNear(mhz, 3.575m) || IsNear(mhz, 7.0475m) || IsNear(mhz, 10.140m)
                || IsNear(mhz, 14.080m) || IsNear(mhz, 18.104m) || IsNear(mhz, 21.140m)
                || IsNear(mhz, 24.919m) || IsNear(mhz, 28.180m) || IsNear(mhz, 50.318m))
                return "FT4";
        }

        // Reasonable default when the rig reports only generic digital mode.
        return "FT8";
    }

    private void OnWsjtLoggedQsoReceived(object? sender, WsjtLoggedQso qso)
    {
        if (!_wsjtAutoPopulateEnabled)
            return;

        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplyWsjtLoggedQso(qso);
            return;
        }

        Dispatcher.UIThread.Post(() => ApplyWsjtLoggedQso(qso));
    }

    public void Dispose()
    {
        _logTypeSelectionService.SelectedContestChanged -= OnSelectedContestChanged;

        if (_wsjtAutoPopulateEnabled)
            App.WsjtBridgeService.LoggedQsoReceived -= OnWsjtLoggedQsoReceived;
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
