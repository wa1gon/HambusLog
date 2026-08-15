# Menu Click Issue - Improved Fix (v2)

## Problem
The "Add New Contact" menu item required multiple clicks to open the QSO input window.

## Improved Solution

### 1. **Stricter Event Deduplication**
Added a synchronous flag `_isHandlingMenuClick` that completely blocks duplicate handling:
- When `PointerPressed` fires, set the flag immediately
- Any simultaneous `SelectionChanged` event is ignored
- Flag is cleared after 100ms to allow subsequent clicks
- This prevents both events from executing simultaneously

### 2. **Better Window Activation**
Enhanced `ShowLogInputWindow` to:
- Check IsVisible before attempting to show
- Use Dispatcher.UIThread.Post for proper UI thread scheduling
- Call both `Activate()` and `Focus()` for better foreground activation

### 3. **Enhanced Logging**
Added debug logging for all menu click paths to help diagnose any remaining issues

## Code Changes

**File**: `Views/MainWindowLogic.cs`
- Added `_isHandlingMenuClick` field for synchronous deduplication
- Updated `OnMenuTreeViewSelectionChanged` to skip if already handling
- Updated `OnMenuTreeViewPointerPressed` to set flag and prevent duplicates
- Enhanced `ShowLogInputWindow` with better window focus handling
- Added debug logging throughout

## Testing

Run the application and:
1. Click "Add New Contact" in the menu - should open immediately
2. Click again - should activate existing window
3. Check logs for any debug messages: `grep "Menu item clicked" $HOME/HamBusLog/applogs/*.log`

## Build Status ✅
- No warnings
- No errors
- Ready for testing

---

## Still Need Clarification

**Regarding "digi in the mode"**: Could you please clarify what you mean by this? Does the mode field:
- Show "DIGI" as a value?
- Show "digi" (lowercase) somewhere?
- Include "digi" as part of a mode selection?
- Show abbreviated modes?

Examples would help:
- "The mode field shows 'DIGI' but should show 'DIGU'"
- "When I click the mode dropdown, 'digi' appears as a suggestion"
- etc.

