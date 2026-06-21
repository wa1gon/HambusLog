# Changes Summary - ARRL FD Contest & Mode Uppercase & Mode Dropdown

## Issues Fixed

### 1. Mode Field Not Forced to Uppercase
**Problem:** The Mode input field was not being normalized to uppercase, unlike other fields (Call, Country, State, etc.)

**Solution:** Updated `LogInputViewModel.cs` line 294 to force Mode to uppercase:
```csharp
public string InputMode    { get => _inputMode;    
    set { if (SetProperty(ref _inputMode, (value ?? string.Empty).ToUpperInvariant())) ValidateMode(); } }
```

**Test:** Added `InputMode_ForcesUppercase` test to verify the behavior.

### 2. ARRL-FD Contest Input Fields Not Showing
**Problem:** When selecting ARRL Field Day contest, the Field Day Section and Class input fields were not appearing in the UI.

**Root Cause:** The config file had incorrect `AdifContestId` for the ARRL-FD contest ("ARRL-FD" instead of "ARRL-FIELD-DAY"). This prevented proper recognition of the contest as a Field Day type.

**Solution:** Updated `~/.config/hambuslog.json` to use correct AdifContestId:
```json
{
  "Key": "ARRL-FD",
  "DisplayName": "ARRL Field Day",
  "AdifContestId": "ARRL-FIELD-DAY",  // Was "ARRL-FD"
  ...
}
```

**Verification:** The ARRL-FD contest configuration now properly includes:
- `ExchangeType: "fieldday"`
- `RequiredFields` array with:
  - `fd_section` (Field Day Section)
  - `fd_class` (Field Day Class)

### 3. Mode Field Converted to Dropdown
**Problem:** Mode entry was free-form text that required users to type mode values.

**Solution:** Replaced Mode TextBox with ComboBox dropdown containing predefined modes:
- USB, LSB, FM, AM, FT8, FT4, RTTY, PSK31, PSK63, OLIVIA, JS8, MFSK, PACKET, HELL, THOR, DOMINO, DIGITAL, CW

**Changes Made:**
1. Added static readonly list `AvailableModesStatic` in `LogInputViewModel.cs`
2. Added public property `AvailableModes` to expose modes to UI
3. Updated `LogInputWindow.axaml` to use ComboBox instead of TextBox for Mode field
4. ComboBox has `IsTextSearchEnabled="True"` for quick selection

**Benefits:**
- Consistent mode values across all QSOs
- No typing errors or typos
- Faster data entry
- Easier lookup/filtering

## Files Modified

1. **ViewModels/LogInputViewModel.cs**
   - Lines 7-12: Added static list of available modes
   - Line 102: Added AvailableModes property
   - Line 294: Updated InputMode property to force uppercase

2. **Views/LogInputWindow.axaml**
   - Lines 168-173: Replaced Mode TextBox with ComboBox dropdown

3. **HamBusLog.Tests/LogInputViewModelTests.cs**
   - Added `AvailableModes_ContainsExpectedModes` test method
   - Added `InputMode_ForcesUppercase` test method

4. **~/.config/hambuslog.json** (User Config)
   - Updated ARRL-FD contest's AdifContestId from "ARRL-FD" to "ARRL-FIELD-DAY"

## UI Components Affected

The LogInputWindow.axaml now displays:
- Mode as a ComboBox dropdown with predefined modes
- Field Day Section and Class inputs (after ARRL-FD fix)
- All fields with consistent uppercase normalization

## Test Results

- **Total Tests:** 59 (up from 57)
- **Passing:** 57 (up from 55)
- **Failing:** 2 (pre-existing failures unrelated to these changes)
- **New Tests:** 
  - `AvailableModes_ContainsExpectedModes` - PASSING ✓
  - `InputMode_ForcesUppercase` - PASSING ✓

Build: Success with 0 warnings

## Behavior Changes

### Before
- Mode field would accept any text input (lowercase, mixed case, typos)
- ARRL-FD contest would not show Field Day Section/Class input fields
- Mode values in database could be mixed case or inconsistent

### After
- Mode field is a dropdown with predefined modes
- All modes are automatically uppercase
- ARRL-FD contest properly displays Field Day Section and Class input fields
- Consistent mode values across all QSOs
- ComboBox allows fast selection or type-ahead search

## Backward Compatibility

- All changes are backward compatible
- Existing contests and data are unaffected
- ComboBox will accept values that match the predefined list
- Mode normalization applies to all input methods (dropdown selection, programmatic, rig updates)



