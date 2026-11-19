## [0.9.0] - 2026-11-19

### Lookups

- Add `Loc.Get(table, key, arg0 … arg7)` and matching `SetLocalizedText` string overloads with positional `{0}`–`{7}` placeholders.
- Add `Loc.TryGet` for lookups that never log or return a missing marker.
- Table names, keys and language codes now ignore case. Addressables addresses are lowercase; importing again updates existing tables.
- A typed `\n` in a sheet cell becomes a line break on import.
- Fixed one `LocaleChanged` listener that throws stopping every listener after it.
- Listeners whose object was destroyed without unsubscribing are removed with a warning.
- The first `Loc.Initialize` no longer raises `LocaleChanged`.
- **Breaking:** named placeholders and `LocArg` are removed. Rewrite `{name}` as `{0}` in the sheet and pass values in order.

## [0.8.0] - 2026-11-17

### Addressables

- Add `Loc.LoadTableAsync` to load tables on demand through Addressables (optional dependency).
- The import makes each new table Addressable in a `Localization` group, one bundle per table, and leaves existing tables' Addressables setup alone.
- `LocalizationConfig` holds settings only; the game registers tables with `Loc.AddTable` or `Loc.LoadTableAsync`.
- Rename `Loc.RemoveTable` to `Loc.ReleaseTable`, which also releases the Addressables handle.
- The Tables Folder mirrors the sheet: a tab removed from the sheet has its asset moved to the trash.
- The Import tab uses Odin Inspector when installed.
- Fixed a new `LocalizationConfig` warning about its default locale before its first import.

## [0.7.0] - 2026-11-17

### Simple Importer

- Replace the Sync tab with an **Import** tab: Spreadsheet URL, Tables Folder, Ignored Tabs and one button.
- Download the whole spreadsheet as `.xlsx`, so cells with quotes or line breaks import correctly and every tab is found automatically.
- Skip tabs whose first header isn't `Key`.
- Remove the diff preview, Validation tab and CSV export.

## [0.6.1] - 2026-11-17

### Sync Isolation

- Fixed sync overwriting a table that belongs to another config with the same tab name.

## [0.6.0] - 2026-11-16

### Per-Table Storage

- Store each spreadsheet tab as its own `LocalizationTable` asset; loading is about 4x faster than JSON.
- Add `Loc.AddTable`, `Loc.RemoveTable` and `Loc.HasTable`.
- `Loc.Initialize(config, initialLocale)` applies a saved language in the same call.
- **Breaking:** remove `TableEntry` and `LocaleValue`; import the spreadsheet again.

## [0.4.0] - 2026-07-26

### Single Source of Truth

- Add Google Sheets sync by URL and tab name, with no API key or OAuth.
- Locales come from the spreadsheet header.
- Add a **Selected Language** preview dropdown to `LocalizedText`.
- Remove inline editing, locale editing and manual CSV import; the spreadsheet is the only place to author text.

## [0.3.0] - 2026-07-26

### Developer Workflow

- Add named placeholders to `Loc.Get`.
- Add zero-allocation `SetLocalizedText` overloads using TMP's formatter.
- Add live key validation on `LocalizedString` fields.
- Store editor settings as a project asset.

## [0.2.0] - 2026-07-22

### Authoring Workflow

- Add the Localization Manager window with Tables, Locales, Import/Export and Validation tabs.
- Add CSV import and export.

## [0.1.0] - 2026-07-22

### Runtime Foundation

- `Loc` facade: `Initialize`, `Get`, `SetLocale`, `LocaleChanged`, `IsReady`.
- Fallback from the current to the default locale.
- `LocalizedString`, `LocalizedText` and `SetLocalizedText` TMP extensions.
- Odin Inspector support.
