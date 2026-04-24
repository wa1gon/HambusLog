namespace HamBusLog.Data;

public sealed class RigCatalogStore : ObservableObject
{
    private ObservableCollection<RigCatalogEntry> _entries = [];
    private string _filePath = string.Empty;
    private string _statusMessage = string.Empty;

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

    public void InitializeFromConfiguration()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        var path = profile.Rigctld.RiglistFilePath;

        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "No rig list file configured.";
            return;
        }

        LoadFromFile(path);
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

            var config = AppConfigurationStore.Load();
            var profile = AppConfigurationStore.GetActiveProfile(config);
            profile.Rigctld.RiglistFilePath = path;
            AppConfigurationStore.Save(config);
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ {ex.Message}";
        }
    }
}

