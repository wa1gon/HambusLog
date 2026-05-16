# Contest Definitions

This document explains how to add or customize Cabrillo contest exports in HamBusLog.

## Overview

Cabrillo contests are now data-driven. The app loads contest definitions from JSON and
uses them to:

- Populate the Cabrillo export contest list.
- Render the header fields in the export UI.
- Route the export to the correct formatter.

## Where to Put the JSON

You can add or override contest definitions in either location:

1) User config (preferred for local changes)

- Path: `~/.config/hambuslog/cabrillo-contests.json`

2) Bundled defaults (used when no user file exists)

- Path in repo: `Assets/cabrillo-contests.json`
- At runtime it is copied to the app output as `cabrillo-contests.json`.

## JSON Schema (Simplified)

```json
{
  "contests": [
    {
      "key": "ARQP",
      "displayName": "AR-QSO-PARTY Arkansas QSO Party",
      "adifContestId": "AR-QSO-PARTY",
      "adifContestIds": ["AR-QSO-PARTY", "ARQP"],
      "exporterKey": "ARQP",
      "headerFields": [
        {
          "key": "CALLSIGN",
          "label": "Callsign",
          "defaultSource": "profile.stationCallSign",
          "defaultValue": "",
          "isRequired": true,
          "isUppercase": true,
          "isMultiline": false
        }
      ]
    }
  ]
}
```

### Contest Fields

- `key`: Unique contest key used by the UI.
- `displayName`: Visible contest name in the export dropdown.
- `adifContestId`: Primary ADIF contest id to match on.
- `adifContestIds`: Optional alternate ADIF ids to match on.
- `exporterKey`: Routes to an exporter (currently `ARQP` or `ARRL-FD`).
- `headerFields`: List of Cabrillo headers to show in the UI and include in output.

### Header Field Properties

- `key`: Cabrillo header name (ex: `CALLSIGN`, `CATEGORY`).
- `label`: UI label for the field.
- `defaultSource`: Built-in defaults from profile:
  - `profile.stationCallSign`
  - `profile.myStateProvince`
  - `profile.myFieldDayClass`
  - `profile.myFieldDaySection`
- `defaultValue`: Static fallback when no `defaultSource` value exists.
- `isRequired`: Display a `*` indicator in the UI.
- `isUppercase`: Auto-uppercase user input.
- `isMultiline`: Render a multi-line textbox (used for `SOAPBOX`).

## Adding a New Contest (Data Only)

1) Add a new contest object to `~/.config/hambuslog/cabrillo-contests.json`.
2) Set `adifContestId` and any `adifContestIds` that should match your QSOs.
3) Set `exporterKey` to an existing exporter (`ARQP` or `ARRL-FD`).
4) Add `headerFields` matching the contest rules.

If the contest uses standard Cabrillo formatting similar to ARQP, you can often
reuse the `ARQP` exporter and just change headers.

## Adding a New Exporter (Code)

If the contest has unique QSO formatting or special headers, add a new exporter:

1) Implement a formatter in `Data/CabrilloExportService.cs`.
2) Add the exporter key in `IsSupportedExporter` and `ExportToFileAsync`.
3) Set `exporterKey` in the JSON to match the new exporter.

## Notes

- The export UI is fully driven by `headerFields`, so leaving a header out means it
  will not appear in the UI and will not be exported.
- `CLAIMED-SCORE` is optional; if omitted, the exporter uses the QSO count.

