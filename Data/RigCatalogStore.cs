namespace HamBusLog.Data;

public sealed class RigCatalogStore : ObservableObject
{
    private ObservableCollection<RigCatalogEntry> _entries = [];
    private string _filePath = string.Empty;
    private string _statusMessage = string.Empty;
    private int? _activeRigNum;

    public ObservableCollection<RigCatalogEntry> Entries
    {
        get => _entries;
        private set => SetProperty(ref _entries, value);
    }

    public string FilePath
    {
        get => _filePath;
        private set => SetProperty(ref _filePath, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public int? ActiveRigNum
    {
        get => _activeRigNum;
        private set => SetProperty(ref _activeRigNum, value);
    }

    public RigCatalogEntry? ActiveRig => ActiveRigNum is int rigNum
        ? Entries.FirstOrDefault(x => x.RigNum == rigNum)
        : null;

    public void InitializeFromConfiguration()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var path = profile.Rigctld.RiglistFilePath;
        ActiveRigNum = profile.Rigctld.ActiveRigNum;

        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "No rig list file configured.";
            return;
        }

        LoadFromFile(path);
        OnPropertyChanged(nameof(ActiveRig));
    }

    public void LoadFromFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                FilePath = path;
                Entries = [];
                StatusMessage = string.IsNullOrWhiteSpace(path)
                    ? "No rig list file selected."
                    : $"✗ File not found: {path}";
                return;
            }

            var text = File.ReadAllText(path);
            var parsed = RigctldRadioCatalogService.ParseRigList(text);
            Entries = new ObservableCollection<RigCatalogEntry>(parsed);
            FilePath = path;
            StatusMessage = $"✓ Loaded {parsed.Count} entries from {Path.GetFileName(path)}";

            if (Entries.Count > 0)
            {
                if (ActiveRigNum is not int activeRigNum || !Entries.Any(x => x.RigNum == activeRigNum))
                    ActiveRigNum = Entries[0].RigNum;
            }
            else
            {
                ActiveRigNum = null;
            }
            OnPropertyChanged(nameof(ActiveRig));

            var config = AppConfigurationStore.Load();
            var profile = AppConfigurationStore.GetActiveProfile(config);
            profile.Rigctld.RiglistFilePath = path;
            profile.Rigctld.ActiveRigNum = ActiveRigNum;
            AppConfigurationStore.Save(config);
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ {ex.Message}";
        }
    }

    public void SetActiveRig(int? rigNum)
    {
        var normalized = rigNum;
        if (normalized is int value && !Entries.Any(x => x.RigNum == value))
            normalized = null;

        if (ActiveRigNum == normalized)
            return;

        ActiveRigNum = normalized;
        OnPropertyChanged(nameof(ActiveRig));

        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        profile.Rigctld.ActiveRigNum = ActiveRigNum;
        AppConfigurationStore.Save(config);
    }

    
    
    public void Clear()
    {
        Entries = [];
        FilePath = string.Empty;
        StatusMessage = string.Empty;
        ActiveRigNum = null;
        OnPropertyChanged(nameof(ActiveRig));
    }
}

