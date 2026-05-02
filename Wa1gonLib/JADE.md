# JADE — JSON Amateur Data Exchange

JADE is a JSON-based interchange format for amateur radio QSO data.

Preferred file extension: `.json`

## Goals

- Preserve the familiar ADIF field names where possible
- Make QSO data easy to inspect, validate, and process with modern tools
- Support stable record identity with `UUID`
- Remain simple enough for exports, backups, APIs, and inter-app exchange

## Core Rules

JADE follows ADIF-style field names and conventions with one key difference:

- `UUID` is required in JADE
- `GUID` is accepted as an import alias for compatibility

Each JADE file contains:

- `format` — must be `JADE`
- `version` — current version is `1.0`
- `schema` — metadata describing required fields and formatting rules
- `records` — array of QSO objects

## Required Record Fields

Every JADE record must include:

- `UUID` or `GUID`
- `CALL`
- `MY_CALL`
- `QSO_DATE`
- `BAND` and/or `FREQ`

## Field Conventions

- `QSO_DATE` uses `yyyyMMdd`
- `TIME_ON` uses `HHmm` or `HHmmss`
- `FREQ` is stored as an MHz decimal string
- ADIF-compatible names are used whenever possible (`MODE`, `RST_SENT`, `DXCC`, etc.)

## Benefits of JADE

### 1. Easier Validation
JSON is straightforward to validate programmatically. JADE can provide record-level errors for malformed fields like UUIDs, dates, times, and frequencies.

### 2. Better Tooling
JADE works naturally with:

- web APIs
- JSON schema tools
- modern editors
- scripting languages
- backups and sync systems

### 3. Stable Record Identity
Unlike classic ADIF, JADE requires `UUID`, making deduplication, synchronization, merge workflows, and cross-system tracking much easier.

### 4. Human Readability
JADE files are easy to read and inspect directly, especially when exported with indentation.

### 5. ADIF Compatibility
JADE keeps the familiar ADIF field names, reducing mapping friction when moving between ADIF-oriented tools and JSON-based workflows.

## Example

```json
{
  "format": "JADE",
  "version": "1.0",
  "schema": {
    "required": ["UUID", "CALL", "MY_CALL", "QSO_DATE", "BAND_OR_FREQ"],
    "date_format": "yyyyMMdd",
    "time_format": "HHmm or HHmmss",
    "freq_format": "MHz decimal string",
    "adif_field_names": true,
    "uuid_required": true
  },
  "records": [
    {
      "UUID": "2f3dcb4f-0adb-4dfc-bf95-72d11b3761e4",
      "CALL": "JA1ABC",
      "MY_CALL": "N0CALL",
      "QSO_DATE": "20260502",
      "TIME_ON": "183015",
      "BAND": "20M",
      "FREQ": "14.074",
      "MODE": "FT8",
      "RST_SENT": "-10",
      "RST_RCVD": "-08"
    }
  ]
}
```

## When to Use JADE

Use JADE when you want:

- structured exports for downstream software
- safer round-trip imports/exports
- reliable record identity
- easier validation than plain ADIF
- JSON-native interchange across services or applications

