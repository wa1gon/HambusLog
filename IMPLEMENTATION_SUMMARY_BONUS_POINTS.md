# Field Day Bonus Points Implementation

## Summary

Implemented a comprehensive bonus points system for ARRL Field Day Cabrillo exports. Users are now prompted to select bonus achievements before exporting, and bonus points are automatically added to the final score.

## Changes Made

### 1. New Files Created

#### `ViewModels/FieldDayBonusPointsViewModel.cs`
- ViewModel for managing Field Day bonus points selection
- Supports 12 different bonus categories:
  - **100-point bonuses** (8 options):
    - Emergency Power
    - Media Publicity
    - Public Location
    - Public Information Table
    - Message to Section Manager
    - Satellite QSO
    - W1AW Bulletin Copy
    - Educational Activity
    - Social Media
    - Safety Officer
  - **Variable bonuses**:
    - Formal Messages Sent (0-100 points)
    - Youth Participation (5 participants max = 100 points; 20 points each)
- Real-time calculation of total bonus points
- Display of running total as user selects bonuses

#### `Views/FieldDayBonusPointsWindow.axaml`
- New Avalonia XAML UI for bonus points dialog
- Organized layout with checkboxes for selection
- Numeric up/down controls for variable bonuses
- Real-time display of calculated total bonus points
- OK/Cancel buttons to confirm or discard selections

#### `Views/FieldDayBonusPointsWindow.axaml.cs`
- Code-behind for bonus points dialog
- Returns selected bonus points via `BonusPoints` property
- Dialog result handling (OK returns true, Cancel returns false)

#### `HamBusLog.Tests/FieldDayBonusPointsViewModelTests.cs`
- Comprehensive unit tests for bonus points calculation (9 tests)
- Tests cover:
  - Zero bonus calculation
  - Single 100-point bonus
  - All 100-point bonuses combined
  - Formal messages calculation (including max capping)
  - Youth participation calculation (including max capping)
  - Mixed bonus combinations
  - Property notification updates

### 2. Modified Files

#### `Views/CabrilloExportWindow.axaml.cs`
- Added bonus dialog flow for Field Day contests
- Shows `FieldDayBonusPointsWindow` before export when ARRL-FD contest is selected
- Captures bonus points from dialog and passes to export service
- Added `IsFieldDayContest()` helper method to identify Field Day contests
- Handles async dialog closure properly

#### `ViewModels/CabrilloExportViewModel.cs`
- Updated `CabrilloExportSettings` constructor to accept `bonusPoints` parameter
- Added `BonusPoints` property to settings object

#### `Data/CabrilloExportService.cs`
- Updated `BuildArrlFieldDayCabrillo()` to extract bonus points from settings
- Modified `CalculateFieldDayScore()` to accept `bonusPoints` parameter and include in total
- Enhanced `NormalizeCabrilloMode()` to more comprehensively classify modes:
  - Added specific digital mode keywords (FT8, FT4, JS8, MFSK, PACKET, THOR, DOMINO, RY, DSTAR, ATV)
  - Returns standardized Cabrillo abbreviations: "CW", "DG" (digital), "PH" (phone)
- Enhanced `NormalizeFieldDayMode()` consistency with more comprehensive digital mode list
- Bonus points are added to QSO score before export

## Scoring Formula

```
Total Score = (CW QSOs × 2) + (Digital QSOs × 2) + (Phone QSOs × 1) + Bonus Points
```

### Mode Classification
- **CW (2 points)**: CW, MORSE
- **Digital (2 points)**: FT8, FT4, RTTY, PSK, PSK31, PSK63, OLIVIA, JS8, MFSK, PACKET, HELL, THOR, DOMINO, DSTAR, ATV, and any mode containing "DIGITAL", "DATA", "PACKET", "JT", "FT"
- **Phone (1 point)**: SSB, AM, FM, LSB, USB, and any unrecognized mode

### Bonus Points
- Each 100-point checkbox: 100 points
- Emergency Power: 100 points
- Media Publicity: 100 points
- Public Location: 100 points
- Public Information Table: 100 points
- Message to Section Manager: 100 points
- Satellite QSO: 100 points
- W1AW Bulletin Copy: 100 points
- Educational Activity: 100 points
- Social Media: 100 points
- Safety Officer: 100 points
- Formal Messages Sent: 0-100 points (user entry, capped at 100)
- Youth Participation: 0-100 points (20 points per youth, up to 5 youth)

## User Workflow

1. User selects ARRL Field Day contest in Cabrillo export dialog
2. Clicks "Export" button
3. `FieldDayBonusPointsWindow` dialog appears with all bonus options
4. User checks/unchecks bonuses and enters variable bonus values
5. Real-time total bonus points display updates
6. User clicks "OK" to proceed with export or "Cancel" to abort
7. If OK, user selects export file location
8. Final score = QSO points + bonus points

## Export File Changes

The Cabrillo export file now includes:
- `CLAIMED-SCORE: [QSO Points] + [Bonus Points]`
  - Example: `CLAIMED-SCORE: 850` (500 from QSOs + 350 from bonuses)

## Testing

- Build: ✓ Success (0 errors, 4 warnings - pre-existing SQLite CVE warnings)
- Tests: ✓ 66 passed, 2 pre-existing failures
- New Tests: ✓ 9 new tests for bonus points logic all passing

## Backward Compatibility

- Existing exports without ARRL-FD contest: no changes
- ARRL-FD exports without bonus selection: bonus points default to 0
- Mode normalization improvements maintain existing behavior for non-FD contests

