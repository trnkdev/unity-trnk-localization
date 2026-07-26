## [0.3.0] - 2026-07-26

### Developer Workflow

- **Smart strings** — `Loc.Get("table", "key", ("name", value), ...)` with named placeholders `{name}`, supporting string/long/double/bool arguments; unknown placeholders stay literal (editor warns).
- **Zero-allocation hot path** — `tmpText.SetLocalizedText(table, key, arg0…arg2)` using TMP's `SetText(format, args)` for per-frame counters (HP bars, scores) without GC allocation.
- **Edit-Mode locale preview** — **Preview** dropdown in the Localization Manager toolbar; refreshes every `LocalizedText` in open scenes and prefab stage without entering Play Mode.
- **Live key validation** — `LocalizedString` fields tint green (key exists) or red (missing) in the inspector; editing to a valid key auto-refreshes the `LocalizedText`'s TMP text. Odin's own palette when Odin is installed.
- **Project-scoped editor settings** — Localization Manager remembers the active config and settings as a project asset, no machine-global `EditorPrefs`.

## [0.2.0] - 2026-07-22

### Authoring Workflow

- UI Toolkit editor window (`Tools > TRnK > Localization Manager`) with Tables, Locales, Import/Export, and Validation tabs.
- Tables tab: spreadsheet grid (`MultiColumnListView`), inline editing, add/remove keys & tables, search.
- Locales tab: manage locales, set default-locale, Sync Entries.
- Import/Export tab: CSV export (Excel-compatible, BOM); CSV import with diff preview, Merge/Replace modes, undo support.
- Validation tab: per-locale coverage bars, missing translations, duplicate keys, empty names.
- Robust CSV pipeline: BOM, quoted/multi-line fields, escaped quotes, comma/semicolon auto-detection.

## [0.1.0] - 2026-07-22


## [0.1.0] - 2026-07-22

### Runtime Foundation

- Single-asset data model (`LocalizationConfig` holds all locales/tables/keys).
- Synchronous `Loc` facade: `Initialize`, `Get`, `SetLocale`, `LocaleChanged`, `IsReady`.
- Current-locale → default-locale fallback; empty values treated as missing.
- `LocalizedString` serializable reference and `LocalizedText` TMP component.
- `SetLocalizedText` TMP extensions (one-shot text assignment).
- Odin/non-Odin dual support (`SerializedScriptableObject` when Odin present, vanilla `ScriptableObject` otherwise).
