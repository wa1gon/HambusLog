namespace HamBusLog.Services;

public interface ILogTypeSelectionService
{
    event EventHandler? SelectedContestChanged;

    string SelectedContestKey { get; }

    IReadOnlyList<ContestDefinition> GetAvailableContests();

    ContestDefinition GetSelectedContestDefinition();

    void SetSelectedContestKey(string? contestKey);
}

