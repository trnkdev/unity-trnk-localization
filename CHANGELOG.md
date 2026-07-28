## [0.4.0] - 2026-07-26

### Single Source of Truth

The spreadsheet is now the only place translations are authored. The config asset is a build artifact: synced from the sheet, never hand-edited.

**Added**

- **Google Sheets sync** — paste the spreadsheet URL once and list the tab names; each tab is fetched as CSV by name (`gviz` endpoint, no API key or OAuth) and becomes a table of the same name.
- Non-blocking fetch via `EditorApplication.update` polling — the editor never freezes; 20s timeout and cancel through Unity's background `Progress` bar.
- All-or-nothing sync: if any tab fails to fetch or parse, nothing reaches the preview — a partial fetch under Replace-All would read as "that table was deleted".
- Named failures: tab not found, no network, empty response, and the not-link-shared case (Google returns an HTML login page instead of CSV).
- Locales are defined by the spreadsheet header — sync registers them, names them from `CultureInfo` (`en` → English), and sets the default locale when unset.
- **Per-component language preview** — a **Selected Language** dropdown on `LocalizedText` rewrites its TMP text in the chosen language, so each component previews independently in the Scene view. Editor-only; never read at runtime.

**Changed**

- Window reduced to three tabs: **Sync**, **Tables** (read-only browser with search), **Validation** (now reporting spreadsheet defects to fix upstream).
- Import is always Replace-All — with one writer there is nothing to reconcile.
- Window styling shares the `LocalizedText` inspector palette; hints use Unity's own secondary-text styling.
- `Validate Key` refreshes the TMP text in the component's selected language rather than always the default.

**Removed**

- Inline table/key editing, locale list editing, Merge-vs-Replace import modes, and manual CSV import — each was a second writer competing with the spreadsheet.
- The global Edit-Mode preview dropdown from the Manager toolbar — replaced by the per-component selector above, which is scoped to what you are looking at and holds no global state.
- Export remains, for seeding a new spreadsheet or keeping a text backup.

## [0.3.0] - 2026-07-26

### Developer Workflow

- **Smart strings** — `Loc.Get("table", "key", ("name", value), ...)` with named placeholders `{name}`, supporting string/long/double/bool arguments; unknown placeholders stay literal (editor warns).
- **Zero-allocation hot path** — `tmpText.SetLocalizedText(table, key, arg0…arg2)` using TMP's `SetText(format, args)` for per-frame counters (HP bars, scores) without GC allocation.
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

### Runtime Foundation

- Single-asset data model (`LocalizationConfig` holds all locales/tables/keys).
- Synchronous `Loc` facade: `Initialize`, `Get`, `SetLocale`, `LocaleChanged`, `IsReady`.
- Current-locale → default-locale fallback; empty values treated as missing.
- `LocalizedString` serializable reference and `LocalizedText` TMP component.
- `SetLocalizedText` TMP extensions (one-shot text assignment).
- Odin/non-Odin dual support (`SerializedScriptableObject` when Odin present, vanilla `ScriptableObject` otherwise).
