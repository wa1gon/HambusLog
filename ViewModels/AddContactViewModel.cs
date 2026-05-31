using HamBusLog.Models;

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
    private string _inputCountry = string.Empty;
    private string _inputName = string.Empty;
    private string _inputState = string.Empty;
    private string _inputCounty = string.Empty;
    private string _inputGrid = string.Empty;

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

    public string InputCountry
    {
        get => _inputCountry;
        set => SetProperty(ref _inputCountry, (value ?? string.Empty).ToUpperInvariant());
    }

    public string InputName
    {
        get => _inputName;
        set => SetProperty(ref _inputName, value ?? string.Empty);
    }

    public string InputState
    {
        get => _inputState;
        set => SetProperty(ref _inputState, (value ?? string.Empty).ToUpperInvariant());
    }

    public string InputCounty
    {
        get => _inputCounty;
        set => SetProperty(ref _inputCounty, (value ?? string.Empty).ToUpperInvariant());
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

        if (!string.IsNullOrWhiteSpace(result.CallSign))
            InputCall = result.CallSign;

        if (!string.IsNullOrWhiteSpace(result.Country))
            InputCountry = result.Country;

        if (!string.IsNullOrWhiteSpace(result.Name))
            InputName = result.Name;

        if (!string.IsNullOrWhiteSpace(result.State))
            InputState = result.State;

        if (!string.IsNullOrWhiteSpace(result.County))
            InputCounty = result.County;

        if (!string.IsNullOrWhiteSpace(result.Grid))
            InputGrid = result.Grid;
    }
}
