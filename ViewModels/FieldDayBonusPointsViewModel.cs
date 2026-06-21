namespace HamBusLog.ViewModels;

public sealed class FieldDayBonusPointsViewModel : ViewModelBase
{
    private bool _emergencyPower;
    private bool _mediaPublicity;
    private bool _publicLocation;
    private bool _publicInfoTable;
    private bool _messageToSectionManager;
    private int _formalMessagesSent; // up to 100
    private bool _satelliteQso;
    private bool _w1awBulletinCopy;
    private bool _educationalActivity;
    private int _youthParticipation; // 20 points each, up to 100
    private bool _socialMedia;
    private bool _safetyOfficer;

    public bool EmergencyPower
    {
        get => _emergencyPower;
        set => SetBonusProperty(ref _emergencyPower, value);
    }

    public bool MediaPublicity
    {
        get => _mediaPublicity;
        set => SetBonusProperty(ref _mediaPublicity, value);
    }

    public bool PublicLocation
    {
        get => _publicLocation;
        set => SetBonusProperty(ref _publicLocation, value);
    }

    public bool PublicInfoTable
    {
        get => _publicInfoTable;
        set => SetBonusProperty(ref _publicInfoTable, value);
    }

    public bool MessageToSectionManager
    {
        get => _messageToSectionManager;
        set => SetBonusProperty(ref _messageToSectionManager, value);
    }

    public int FormalMessagesSent
    {
        get => _formalMessagesSent;
        set => SetBonusProperty(ref _formalMessagesSent, Math.Min(Math.Max(value, 0), 100));
    }

    public bool SatelliteQso
    {
        get => _satelliteQso;
        set => SetBonusProperty(ref _satelliteQso, value);
    }

    public bool W1awBulletinCopy
    {
        get => _w1awBulletinCopy;
        set => SetBonusProperty(ref _w1awBulletinCopy, value);
    }

    public bool EducationalActivity
    {
        get => _educationalActivity;
        set => SetBonusProperty(ref _educationalActivity, value);
    }

    public int YouthParticipation
    {
        get => _youthParticipation;
        set => SetBonusProperty(ref _youthParticipation, Math.Min(Math.Max(value, 0), 5)); // Up to 5 participants = 100 points
    }

    public bool SocialMedia
    {
        get => _socialMedia;
        set => SetBonusProperty(ref _socialMedia, value);
    }

    public bool SafetyOfficer
    {
        get => _safetyOfficer;
        set => SetBonusProperty(ref _safetyOfficer, value);
    }

    public string TotalBonusPoints => CalculateTotalBonusPoints().ToString(CultureInfo.InvariantCulture);

    private int CalculateTotalBonusPoints()
    {
        int total = 0;

        if (EmergencyPower) total += 100;
        if (MediaPublicity) total += 100;
        if (PublicLocation) total += 100;
        if (PublicInfoTable) total += 100;
        if (MessageToSectionManager) total += 100;
        total += Math.Min(FormalMessagesSent, 100);
        if (SatelliteQso) total += 100;
        if (W1awBulletinCopy) total += 100;
        if (EducationalActivity) total += 100;
        total += YouthParticipation * 20;
        if (SocialMedia) total += 100;
        if (SafetyOfficer) total += 100;

        return total;
    }

    public int GetTotalBonusPoints()
    {
        return CalculateTotalBonusPoints();
    }

    private void SetBonusProperty<T>(ref T field, T value)
    {
        if (SetProperty(ref field, value))
            OnPropertyChanged(nameof(TotalBonusPoints));
    }
}


