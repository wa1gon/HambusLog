# RigctldConnectionManager Service

## Overview
The `RigctldConnectionManager` is now a proper service that can be injected throughout the application. It manages connections to rigctld (radio control daemon) instances and provides a centralized way to interact with multiple radios.

## Service Interface
The service is exposed through the `IRigctldConnectionManager` interface located in `HamBusLog.Services` namespace:

```csharp
public interface IRigctldConnectionManager : IDisposable
{
    event EventHandler? StatesChanged;
    
    Task<string> SetFrequencyByTagAsync(string tagName, decimal frequencyMhz, CancellationToken ct = default);
    Task<string> SetFrequencyByNameAsync(string radioName, decimal frequencyMhz, CancellationToken ct = default);
    Task<string> SetModeByTagAsync(string tagName, string mode, CancellationToken ct = default);
    Task<string> SetModeByNameAsync(string radioName, string mode, CancellationToken ct = default);
    Task RefreshActiveConnectionsAsync();
    IReadOnlyList<RadioRuntimeState> GetSnapshot();
    RadioRuntimeState? GetPrimaryActiveState();
}
```

## Current Usage Patterns

### Global Access via App Static Property
The service is still accessible globally as a singleton:

```csharp
// Access from anywhere in the application
var primaryRadio = App.RigctldConnectionManager.GetPrimaryActiveState();
var allRadios = App.RigctldConnectionManager.GetSnapshot();

// Set radio frequency
await App.RigctldConnectionManager.SetFrequencyByNameAsync("Radio1", 14.250m);

// Set radio mode
await App.RigctldConnectionManager.SetModeByNameAsync("Radio1", "USB");

// Subscribe to state changes
App.RigctldConnectionManager.StatesChanged += (sender, e) => 
{
    var states = App.RigctldConnectionManager.GetSnapshot();
    // Update UI or perform other actions
};
```

### Injected into ViewModels
The service can be injected into ViewModels for dependency injection:

```csharp
public class MyViewModel
{
    private readonly IRigctldConnectionManager _rigctldConnectionManager;
    
    public MyViewModel(IRigctldConnectionManager rigctldConnectionManager)
    {
        _rigctldConnectionManager = rigctldConnectionManager;
    }
    
    public async Task DoSomethingWithRadio()
    {
        var state = _rigctldConnectionManager.GetPrimaryActiveState();
        if (state?.IsConnected == true)
        {
            await _rigctldConnectionManager.SetFrequencyByNameAsync(
                state.RadioName, 
                14.250m
            );
        }
    }
}
```

## Service Lifecycle
- **Initialization**: The service is initialized in `App.OnFrameworkInitializationCompleted()`
- **Active Connections**: Refreshed when configuration changes
- **Cleanup**: Disposed when the application exits

## Key Features
- **Multi-radio Support**: Manages connections to multiple rigctld instances
- **Connection Pooling**: One worker per unique endpoint to avoid conflicts
- **Reconnection**: Automatic reconnection with configurable interval
- **State Management**: Maintains current state for all connected radios
- **Event Notifications**: Raises `StatesChanged` event when any radio state changes
- **Thread-Safe**: Uses lock-based synchronization for thread safety

## Configuration
The service reads configuration from `AppConfigurationStore`:
- Radio endpoints (host:port)
- Radio names
- Reconnection interval settings

## Future Enhancement Opportunities
With the service interface now in place, future improvements can include:
- Dependency injection container integration
- Mock implementations for testing
- Service locator pattern support
- Async initialization patterns
- Better lifecycle management

