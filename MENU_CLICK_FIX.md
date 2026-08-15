# Fixed: Single-Click Menu Items (was Double-Click Issue)

## Problem
The "Add New Contact" menu item required multiple clicks to open the QSO input window, unlike the grid which responds immediately to a double-click.

## Root Cause
The issue was that both `OnMenuTreeViewPointerPressed` and `OnMenuTreeViewSelectionChanged` event handlers were firing in sequence, causing `OpenNewContactWindow()` to be called twice. Although the method had guards to prevent opening duplicate windows, the UI appeared unresponsive requiring a second click.

## Solution
Implemented a debouncing mechanism with the following improvements:

### 1. **Event Handler Tracking**
Added two private fields to track recently handled menu items:
```csharp
private MenuNode? _lastHandledMenuNode;
private DateTime _lastMenuNodeHandledTime;
```

### 2. **Debouncing in SelectionChanged**
Modified `OnMenuTreeViewSelectionChanged` to ignore duplicate events within 50ms:
```csharp
// Debounce: ignore if we just handled this node in the last 50ms
if (_lastHandledMenuNode == node && 
    DateTime.UtcNow.Subtract(_lastMenuNodeHandledTime).TotalMilliseconds < 50)
    return;
```

### 3. **Early Tracking in PointerPressed**
When a leaf node (menu item without children) is clicked, we now track it immediately:
```csharp
_lastHandledMenuNode = node;
_lastMenuNodeHandledTime = DateTime.UtcNow;
```

### 4. **Enhanced Logging**
Added Serilog instrumentation to track menu interactions:
- Menu item clicks are logged at Debug level
- Button clicks are logged at Debug level  
- Window opening is logged at Information level
- Window reuse is logged at Debug level

## Files Modified
- `/home/darryl/github/Hambus/HamBusLog/Views/MainWindowLogic.cs`
  - Added debounce tracking fields (lines 27-28)
  - Enhanced `OnMenuTreeViewSelectionChanged` with debouncing (lines 107-123)
  - Enhanced `OnMenuTreeViewPointerPressed` with node tracking (lines 145-147)
  - Added logging to menu handler (line 155)
  - Added logging to button click handlers (lines 217-227)
  - Added logging to window opening method (lines 506-527)

## Testing
The fix ensures that:
- ✅ Menu items respond on first click
- ✅ Grid double-click still works (unchanged)
- ✅ No duplicate window openings
- ✅ Logs track all menu interactions for debugging

## Debugging
To debug menu click issues, enable Debug-level logging:
```bash
cat $HOME/HamBusLog/applogs/hambuslog-*.log | grep "Menu item clicked"
```

Or check for window opening:
```bash
cat $HOME/HamBusLog/applogs/hambuslog-*.log | grep "Opening new contact"
```

