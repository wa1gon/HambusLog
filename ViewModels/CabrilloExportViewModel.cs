namespace HamBusLog.ViewModels;

public sealed class CabrilloExportViewModel : ViewModelBase
{
    private readonly ConfigProfile _profile;

    public CabrilloExportViewModel()
    {
        var config = AppConfigurationStore.Load();
        _profile = AppConfigurationStore.GetActiveProfile(config);
        Contests = new ObservableCollection<CabrilloContestOption>(BuildContestOptions());
        SelectedContest = Contests.FirstOrDefault(x => x.IsSupported) ?? Contests.FirstOrDefault();
        ResetDefaults();
    }

    public ObservableCollection<CabrilloContestOption> Contests { get; }

    private CabrilloContestOption? _selectedContest;
    public CabrilloContestOption? SelectedContest
    {
        get => _selectedContest;
        set
        {
            if (SetProperty(ref _selectedContest, value))
            {
                OnPropertyChanged(nameof(CanExport));
                OnPropertyChanged(nameof(IsArrlFieldDaySelected));
                OnPropertyChanged(nameof(IsArqpSelected));
                OnPropertyChanged(nameof(ShowArqpCategoryFields));
                OnPropertyChanged(nameof(ShowFieldDayCategoryFields));
                OnPropertyChanged(nameof(ShowFieldDayNameField));
                OnPropertyChanged(nameof(ShowFieldDayAddressFields));
            }
        }
    }

    public bool CanExport => SelectedContest?.IsSupported == true;

    public bool IsArrlFieldDaySelected => SelectedContest?.IsArrlFieldDay() == true;
    public bool IsArqpSelected => SelectedContest?.IsArqp() == true;
    public bool ShowArqpCategoryFields => IsArqpSelected;
    public bool ShowFieldDayCategoryFields => IsArrlFieldDaySelected;
    public bool ShowFieldDayNameField => IsArrlFieldDaySelected;
    public bool ShowFieldDayAddressFields => IsArrlFieldDaySelected;

    private string _callSign = string.Empty;
    public string CallSign
    {
        get => _callSign;
        set => SetProperty(ref _callSign, (value ?? string.Empty).Trim().ToUpperInvariant());
    }

    private string _categoryOperator = "SINGLE-OP";
    public string CategoryOperator
    {
        get => _categoryOperator;
        set => SetProperty(ref _categoryOperator, (value ?? string.Empty).Trim().ToUpperInvariant());
    }

    private string _categoryAssisted = "NON-ASSISTED";
    public string CategoryAssisted
    {
        get => _categoryAssisted;
        set => SetProperty(ref _categoryAssisted, (value ?? string.Empty).Trim().ToUpperInvariant());
    }

    private string _categoryBand = "ALL";
    public string CategoryBand
    {
        get => _categoryBand;
        set => SetProperty(ref _categoryBand, (value ?? string.Empty).Trim().ToUpperInvariant());
    }

    private string _categoryMode = "MIXED";
    public string CategoryMode
    {
        get => _categoryMode;
        set => SetProperty(ref _categoryMode, (value ?? string.Empty).Trim().ToUpperInvariant());
    }

    private string _categoryPower = "LOW";
    public string CategoryPower
    {
        get => _categoryPower;
        set => SetProperty(ref _categoryPower, (value ?? string.Empty).Trim().ToUpperInvariant());
    }

    private string _categoryTransmitter = "ONE";
    public string CategoryTransmitter
    {
        get => _categoryTransmitter;
        set => SetProperty(ref _categoryTransmitter, (value ?? string.Empty).Trim().ToUpperInvariant());
    }

    private string _location = string.Empty;
    public string Location
    {
        get => _location;
        set => SetProperty(ref _location, (value ?? string.Empty).Trim().ToUpperInvariant());
    }

    private string _operators = string.Empty;
    public string Operators
    {
        get => _operators;
        set => SetProperty(ref _operators, (value ?? string.Empty).Trim().ToUpperInvariant());
    }

    private string _claimedScore = string.Empty;
    public string ClaimedScore
    {
        get => _claimedScore;
        set => SetProperty(ref _claimedScore, (value ?? string.Empty).Trim());
    }

    private string _club = string.Empty;
    public string Club
    {
        get => _club;
        set => SetProperty(ref _club, (value ?? string.Empty).Trim());
    }

    private string _soapbox = string.Empty;
    public string Soapbox
    {
        get => _soapbox;
        set => SetProperty(ref _soapbox, value ?? string.Empty);
    }

    private string _category = string.Empty;
    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, (value ?? string.Empty).Trim().ToUpperInvariant());
    }

    private string _arrlSection = string.Empty;
    public string ArrlSection
    {
        get => _arrlSection;
        set => SetProperty(ref _arrlSection, (value ?? string.Empty).Trim().ToUpperInvariant());
    }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, (value ?? string.Empty).Trim());
    }

    private string _address = string.Empty;
    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value ?? string.Empty);
    }

    private string _addressCity = string.Empty;
    public string AddressCity
    {
        get => _addressCity;
        set => SetProperty(ref _addressCity, (value ?? string.Empty).Trim());
    }

    private string _addressStateProvince = string.Empty;
    public string AddressStateProvince
    {
        get => _addressStateProvince;
        set => SetProperty(ref _addressStateProvince, (value ?? string.Empty).Trim().ToUpperInvariant());
    }

    private string _addressPostalCode = string.Empty;
    public string AddressPostalCode
    {
        get => _addressPostalCode;
        set => SetProperty(ref _addressPostalCode, (value ?? string.Empty).Trim());
    }

    private string _addressCountry = string.Empty;
    public string AddressCountry
    {
        get => _addressCountry;
        set => SetProperty(ref _addressCountry, (value ?? string.Empty).Trim().ToUpperInvariant());
    }

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, (value ?? string.Empty).Trim());
    }

    public void ResetDefaults()
    {
        CallSign = _profile.StationCallSign;
        Operators = string.IsNullOrWhiteSpace(_profile.StationCallSign)
            ? string.Empty
            : _profile.StationCallSign;
        Location = _profile.MyStateProvince;
        Category = _profile.MyFieldDayClass;
        ArrlSection = _profile.MyFieldDaySection;
        ClaimedScore = string.Empty;
        Club = string.Empty;
        Soapbox = string.Empty;
        Name = string.Empty;
        Address = string.Empty;
        AddressCity = string.Empty;
        AddressStateProvince = string.Empty;
        AddressPostalCode = string.Empty;
        AddressCountry = string.Empty;
        Email = string.Empty;
    }

    public CabrilloExportSettings BuildSettings()
    {
        return new CabrilloExportSettings(
            CallSign,
            CategoryOperator,
            CategoryAssisted,
            CategoryBand,
            CategoryMode,
            CategoryPower,
            CategoryTransmitter,
            Location,
            Operators,
            ClaimedScore,
            Club,
            Soapbox,
            Category,
            ArrlSection,
            Name,
            Address,
            AddressCity,
            AddressStateProvince,
            AddressPostalCode,
            AddressCountry,
            Email);
    }

    private static List<CabrilloContestOption> BuildContestOptions()
    {
        var contests = ContestCatalog.GetAll()
            .Select(def => new CabrilloContestOption(def))
            .OrderByDescending(x => x.IsSupported)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return contests.Count > 0 ? contests : [new CabrilloContestOption("AR-QSO-PARTY", "AR-QSO-PARTY")];
    }
}

public sealed class CabrilloContestOption
{
    public CabrilloContestOption(ContestDefinition definition)
    {
        Key = definition.Key;
        DisplayName = definition.DisplayName;
        AdifContestId = definition.AdifContestId;
        IsSupported = IsSupportedContest(definition.Key, definition.AdifContestId);
    }

    public CabrilloContestOption(string key, string displayName)
    {
        Key = key;
        DisplayName = displayName;
        AdifContestId = key;
        IsSupported = IsSupportedContest(key, key);
    }

    public string Key { get; }
    public string DisplayName { get; }
    public string AdifContestId { get; }
    public bool IsSupported { get; }

    public string Display => IsSupported ? DisplayName : $"{DisplayName} (not supported)";

    public bool IsArrlFieldDay()
    {
        return string.Equals(Key, "ARRL-FD", StringComparison.OrdinalIgnoreCase)
               || string.Equals(AdifContestId, "ARRL-FD", StringComparison.OrdinalIgnoreCase)
               || string.Equals(Key, "ARRL-FIELD-DAY", StringComparison.OrdinalIgnoreCase)
               || string.Equals(AdifContestId, "ARRL-FIELD-DAY", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsArqp()
    {
        return string.Equals(Key, "AR-QSO-PARTY", StringComparison.OrdinalIgnoreCase)
               || string.Equals(AdifContestId, "AR-QSO-PARTY", StringComparison.OrdinalIgnoreCase)
               || string.Equals(Key, "ARQP", StringComparison.OrdinalIgnoreCase)
               || string.Equals(AdifContestId, "ARQP", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedContest(string? key, string? adifId)
    {
        return IsArqp(key, adifId) || IsArrlFieldDay(key, adifId);
    }

    private static bool IsArqp(string? key, string? adifId)
    {
        return string.Equals(key, "AR-QSO-PARTY", StringComparison.OrdinalIgnoreCase)
               || string.Equals(adifId, "AR-QSO-PARTY", StringComparison.OrdinalIgnoreCase)
               || string.Equals(key, "ARQP", StringComparison.OrdinalIgnoreCase)
               || string.Equals(adifId, "ARQP", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsArrlFieldDay(string? key, string? adifId)
    {
        return string.Equals(key, "ARRL-FD", StringComparison.OrdinalIgnoreCase)
               || string.Equals(adifId, "ARRL-FD", StringComparison.OrdinalIgnoreCase)
               || string.Equals(key, "ARRL-FIELD-DAY", StringComparison.OrdinalIgnoreCase)
               || string.Equals(adifId, "ARRL-FIELD-DAY", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record CabrilloExportSettings(
    string CallSign,
    string CategoryOperator,
    string CategoryAssisted,
    string CategoryBand,
    string CategoryMode,
    string CategoryPower,
    string CategoryTransmitter,
    string Location,
    string Operators,
    string ClaimedScore,
    string Club,
    string Soapbox,
    string Category,
    string ArrlSection,
    string Name,
    string Address,
    string AddressCity,
    string AddressStateProvince,
    string AddressPostalCode,
    string AddressCountry,
    string Email);
