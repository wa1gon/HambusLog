namespace HamBusLog.ViewModels;

public partial class AddContactViewModel : ViewModelBase
{
    private string _inputCall = string.Empty;
    private string _inputDate = string.Empty;
    private string _inputTimeOn = string.Empty;
    private string _inputBand = string.Empty;
    private string _inputMode = string.Empty;
    private string _inputFreq = string.Empty;
    private string _inputSent = string.Empty;
    private string _inputRec = string.Empty;
    private string _inputFieldDaySection = string.Empty;
    private string _inputFieldDayClass = string.Empty;

    public AddContactViewModel()
    {
        InputDate = DateTime.UtcNow.ToString("yyyyMMdd");
        InputTimeOn = DateTime.UtcNow.ToString("HHmm");
    }

    public string InputCall
    {
        get => _inputCall;
        set => SetProperty(ref _inputCall, (value ?? string.Empty).ToUpperInvariant());
    }

    public string InputDate
    {
        get => _inputDate;
        set => SetProperty(ref _inputDate, value ?? string.Empty);
    }

    public string InputTimeOn
    {
        get => _inputTimeOn;
        set => SetProperty(ref _inputTimeOn, value ?? string.Empty);
    }

    public string InputBand
    {
        get => _inputBand;
        set => SetProperty(ref _inputBand, value ?? string.Empty);
    }

    public string InputMode
    {
        get => _inputMode;
        set => SetProperty(ref _inputMode, value ?? string.Empty);
    }

    public string InputFreq
    {
        get => _inputFreq;
        set => SetProperty(ref _inputFreq, value ?? string.Empty);
    }

    public string InputSent
    {
        get => _inputSent;
        set => SetProperty(ref _inputSent, value ?? string.Empty);
    }

    public string InputRec
    {
        get => _inputRec;
        set => SetProperty(ref _inputRec, value ?? string.Empty);
    }

    public string InputFieldDaySection
    {
        get => _inputFieldDaySection;
        set => SetProperty(ref _inputFieldDaySection, (value ?? string.Empty).ToUpperInvariant());
    }

    public string InputFieldDayClass
    {
        get => _inputFieldDayClass;
        set => SetProperty(ref _inputFieldDayClass, (value ?? string.Empty).ToUpperInvariant());
    }
}
