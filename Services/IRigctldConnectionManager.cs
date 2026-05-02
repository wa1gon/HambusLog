namespace HamBusLog.Services;

/// <summary>
/// Service for managing connections to rigctld radio control daemon instances.
/// </summary>
public interface IRigctldConnectionManager : IDisposable
{
    /// <summary>
    /// Raised when radio connection states change.
    /// </summary>
    event EventHandler? StatesChanged;

    /// <summary>
    /// Sets the frequency for a radio by its tag name.
    /// </summary>
    Task<string> SetFrequencyByTagAsync(string tagName, decimal frequencyMhz, CancellationToken ct = default);

    /// <summary>
    /// Sets the frequency for a radio by its name.
    /// </summary>
    Task<string> SetFrequencyByNameAsync(string radioName, decimal frequencyMhz, CancellationToken ct = default);

    /// <summary>
    /// Sets the mode for a radio by its tag name.
    /// </summary>
    Task<string> SetModeByTagAsync(string tagName, string mode, CancellationToken ct = default);

    /// <summary>
    /// Sets the mode for a radio by its name.
    /// </summary>
    Task<string> SetModeByNameAsync(string radioName, string mode, CancellationToken ct = default);

    /// <summary>
    /// Refreshes active connections based on the current configuration.
    /// </summary>
    Task RefreshActiveConnectionsAsync();

    /// <summary>
    /// Gets a snapshot of all radio runtime states.
    /// </summary>
    IReadOnlyList<RadioRuntimeState> GetSnapshot();

    /// <summary>
    /// Gets the primary active radio state (connected radios first, sorted by last update time).
    /// </summary>
    RadioRuntimeState? GetPrimaryActiveState();
}

