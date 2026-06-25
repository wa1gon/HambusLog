namespace HamBusLog.Services;

public sealed class LogTypeSelectionService : ILogTypeSelectionService
{
    private readonly object _sync = new();
    private string _selectedContestKey;

    public LogTypeSelectionService()
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);
        _selectedContestKey = NormalizeContestKey(profile.LastContestKey);
    }

    public event EventHandler? SelectedContestChanged;

    public string SelectedContestKey
    {
        get
        {
            lock (_sync)
                return _selectedContestKey;
        }
    }

    public IReadOnlyList<ContestDefinition> GetAvailableContests()
        => ContestCatalog.GetAll();

    public ContestDefinition GetSelectedContestDefinition()
        => ContestCatalog.GetByKey(SelectedContestKey)
           ?? ContestCatalog.Get(ContestType.Normal);

    public void SetSelectedContestKey(string? contestKey)
    {
        var normalized = NormalizeContestKey(contestKey);

        lock (_sync)
        {
            if (string.Equals(_selectedContestKey, normalized, StringComparison.OrdinalIgnoreCase))
                return;

            _selectedContestKey = normalized;
        }

        PersistSelection(normalized);
        SelectedContestChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string NormalizeContestKey(string? contestKey)
    {
        var normalized = string.IsNullOrWhiteSpace(contestKey)
            ? ContestCatalog.NormalKey
            : contestKey.Trim();

        return ContestCatalog.GetByKey(normalized) is null
            ? ContestCatalog.NormalKey
            : normalized;
    }

    private static void PersistSelection(string selectedContestKey)
    {
        var config = AppConfigurationStore.Load();
        var profile = AppConfigurationStore.GetActiveProfile(config);

        if (string.Equals(profile.LastContestKey, selectedContestKey, StringComparison.OrdinalIgnoreCase))
            return;

        profile.LastContestKey = selectedContestKey;
        AppConfigurationStore.Save(config);
    }
}

