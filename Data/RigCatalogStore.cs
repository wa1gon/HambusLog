namespace HamBusLog.Data;

public sealed class RigCatalogStore : ObservableObject
{
    private ObservableCollection<RigCatalogEntry> _entries = [];
    private string _filePath = string.Empty;
    private string _statusMessage = string.Empty;
    private int? _activeRigNum;
    private ObservableCollection<int> _activeRigNums = [];

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

    public ObservableCollection<int> ActiveRigNums
    {
        get => _activeRigNums;
        private set => SetProperty(ref _activeRigNums, value);
    }

    public RigCatalogEntry? ActiveRig => ActiveRigNum is int rigNum
        ? Entries.FirstOrDefault(x => x.RigNum == rigNum)
        : null;

    public void InitializeFromConfiguration()
    {
        var config = AppConfigurationStore.Load();
        var radio = AppConfigurationStore.GetActiveRigctldRadio(config);
        var path = radio.RiglistFilePath;
        var activeList = (radio.ActiveRigNums ?? [])
            .Where(x => x > 0)
            .Distinct()
            .ToList();
        ActiveRigNums = new ObservableCollection<int>(activeList);
        ActiveRigNum = activeList.FirstOrDefault();
        if (ActiveRigNum is null)
            ActiveRigNum = radio.ActiveRigNum;

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
                var valid = ActiveRigNums.Where(x => Entries.Any(e => e.RigNum == x)).Distinct().ToList();
                if (valid.Count == 0 && ActiveRigNum is int single)
                    valid.Add(single);
                ActiveRigNums = new ObservableCollection<int>(valid);
            }
            else
            {
                ActiveRigNum = null;
                ActiveRigNums = [];
            }
            OnPropertyChanged(nameof(ActiveRig));

            var config = AppConfigurationStore.Load();
            var radio = AppConfigurationStore.GetActiveRigctldRadio(config);
            radio.RiglistFilePath = path;
            radio.ActiveRigNum = ActiveRigNum;
            radio.ActiveRigNums = ActiveRigNums.ToList();
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
        ActiveRigNums = normalized is int selectedRigNum ? [selectedRigNum] : [];
        OnPropertyChanged(nameof(ActiveRig));

        var config = AppConfigurationStore.Load();
        var radio = AppConfigurationStore.GetActiveRigctldRadio(config);
        radio.ActiveRigNum = ActiveRigNum;
        radio.ActiveRigNums = ActiveRigNums.ToList();
        AppConfigurationStore.Save(config);
    }

    public void SetActiveRigs(IEnumerable<int>? rigNums)
    {
        var selected = (rigNums ?? [])
            .Where(x => Entries.Any(e => e.RigNum == x))
            .Distinct()
            .ToList();

        ActiveRigNums = new ObservableCollection<int>(selected);
        ActiveRigNum = selected.FirstOrDefault();
        OnPropertyChanged(nameof(ActiveRig));

        var config = AppConfigurationStore.Load();
        var radio = AppConfigurationStore.GetActiveRigctldRadio(config);
        radio.ActiveRigNums = selected;
        radio.ActiveRigNum = ActiveRigNum;
        AppConfigurationStore.Save(config);
    }
}
