namespace HamBusLog.ViewModels;

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

    public LogInputViewModel()
    {
        ContestTypes    = [ContestType.Normal, ContestType.ArrlFieldDay];
        Details         = [];
        InputDate       = DateTime.UtcNow.ToString("yyyyMMdd");
        InputTimeOn     = DateTime.UtcNow.ToString("HHmm");
    }

    // ── Properties ────────────────────────────────────────────────────
    public List<ContestType> ContestTypes { get; }
    public ObservableCollection<QsoDetailRow> Details { get; }

    public ContestType SelectedContestType
    {
        get => _selectedContestType;
        set { if (SetProperty(ref _selectedContestType, value)) OnPropertyChanged(nameof(IsFieldDay)); }
    }
    public bool IsFieldDay => SelectedContestType == ContestType.ArrlFieldDay;

    public string InputCall    { get => _inputCall;    set => SetProperty(ref _inputCall,    value); }
    public string InputDate    { get => _inputDate;    set => SetProperty(ref _inputDate,    value); }
    public string InputTimeOn  { get => _inputTimeOn;  set => SetProperty(ref _inputTimeOn,  value); }
    public string InputBand    { get => _inputBand;    set { if (SetProperty(ref _inputBand, value)) ValidateBand(); } }
    public string InputMode    { get => _inputMode;    set { if (SetProperty(ref _inputMode, value)) ValidateMode(); } }
    public string InputFreq    { get => _inputFreq;    set => SetProperty(ref _inputFreq,    value); }
    public string InputSent    { get => _inputSent;    set => SetProperty(ref _inputSent,    value); }
    public string InputRec     { get => _inputRec;     set => SetProperty(ref _inputRec,     value); }
    public string InputFieldDaySection { get => _inputFieldDaySection; set { if (SetProperty(ref _inputFieldDaySection, value)) ValidateSection(); } }
    public string InputFieldDayClass   { get => _inputFieldDayClass;   set { if (SetProperty(ref _inputFieldDayClass,   value)) ValidateClass(); } }

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

        errorMessage = string.Empty;
        return qso;
    }

    // ── Helpers ───────────────────────────────────────────────────────
    public void StampNow()
    {
        InputDate   = DateTime.UtcNow.ToString("yyyyMMdd");
        InputTimeOn = DateTime.UtcNow.ToString("HHmm");
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
}

/// <summary>Mutable detail row displayed in the QsoDetail DataGrid.</summary>
public sealed class QsoDetailRow : ViewModelBase
{
    private string _fieldName  = string.Empty;
    private string _fieldValue = string.Empty;

    public string FieldName  { get => _fieldName;  set => SetProperty(ref _fieldName,  value); }
    public string FieldValue { get => _fieldValue; set => SetProperty(ref _fieldValue, value); }
}

