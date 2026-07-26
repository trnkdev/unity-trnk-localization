# TRnK Localization

A lightweight, synchronous, text-first localization package for Unity. `Loc.Get()` is a plain dictionary lookup — no coroutines, no Addressables coupling, no async complexity.

## Installation

### Via Git URL

1. Install TRnK.Toolkit first via Unity Package Manager:

```
https://github.com/trnkdev/unity-trnk-toolkit.git
```

2. Then add TRnK.Localization:

```
https://github.com/trnkdev/unity-trnk-localization.git
```

## Quick Start

**1. Create the config asset**

`Assets > Create > TRnK > Localization > Config` — place it anywhere inside a `Resources` folder (e.g. `Assets/Resources/LocalizationConfig.asset`).

**2. Author your data**

Open `Tools > TRnK > Localization Manager`:
- **Locales tab** — register your languages (`en`, `vi`, `ja`, …) and pick the default
- **Tables tab** — add tables (e.g. `UI`), add keys, type translations in the spreadsheet grid

**3. Initialize from your game bootstrap**

`Loc.Initialize` takes a `LocalizationConfig` instance — load it however your project loads assets (Resources, Addressables, a direct Inspector reference, …) and pass it in.

```csharp
using TRnK.Localization;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] LocalizationConfig _localizationConfig;

    void Awake()
    {
        Loc.Initialize(_localizationConfig);

        var save = SaveSystem.Load();
        if (!string.IsNullOrEmpty(save.languageCode))
            Loc.SetLocale(save.languageCode);
    }
}
```

**4. Use it**

```csharp
using TRnK.Localization;

// In code
string label = Loc.Get("UI", "play_button");

// Smart strings — named placeholders
// "deal_damage" = "Deal {damage} damage to {enemy}"
string msg = Loc.Get("Combat", "deal_damage", ("damage", 42), ("enemy", "Goblin"));

// One-shot TMP extension
_label.SetLocalizedText("UI", "play_button");

// Zero-allocation per-frame text — positional {0} placeholders, up to 3 float args
// "score" = "Score: {0}"
_scoreLabel.SetLocalizedText("UI", "score", score);

// Format specifiers work too (TMP's own syntax)
// "hp_bar" = "{0:0}/{1:0} HP"
_hpLabel.SetLocalizedText("UI", "hp_bar", current, max);

// Auto-refreshing component (no code): Add Component > TRnK Localization > Localized Text
// Set Table + Key in the Inspector. Refreshes automatically on locale change.

// Serializable reference
[SerializeField] LocalizedString _tooltip;
void Show() => _ui.SetText(_tooltip.Get());

// React to language change
void OnEnable()  => Loc.LocaleChanged += OnLocale;
void OnDisable() => Loc.LocaleChanged -= OnLocale;

// Language switcher
public void SetLanguage(string code)
{
    Loc.SetLocale(code);
    var save = SaveSystem.Load();
    save.languageCode = code;
    SaveSystem.Save(save);
}
```

## Behavior Reference

**Missing key fallback chain:**
1. Try current locale (empty string counts as missing)
2. Fall back to default locale (if different)
3. Editor: logs a warning and returns `#Table.key` · Player builds: returns `""`

**Initialization:** idempotent — calling `Initialize` twice with the same asset is a no-op and preserves the current locale. Initializing with a different asset resets to that asset's default locale.

**`SetLocale`** with an unregistered code is ignored (with a warning). Setting the same locale twice doesn't re-fire `LocaleChanged`.

**Smart strings:** unknown `{placeholder}` names stay literal in the output (editor logs a warning); `{{` and `}}` escape literal braces; numeric arguments format with invariant culture.

**Named vs positional:** `Loc.Get` uses **named** placeholders (`{damage}`) and returns a string. The TMP `SetLocalizedText` overloads use TMP's **positional** placeholders (`{0}`, `{1:00}`) and write straight into TMP's buffer — same package, two syntaxes, because the zero-allocation path is TMP's formatter, not ours.

**Locale preview:** the **Preview** dropdown in the Localization Manager's top bar refreshes every `LocalizedText` in open scenes and the prefab stage without entering Play Mode. Selecting **Off** (or closing the window, switching configs, or entering Play Mode) ends the preview; texts return to the default locale.

**Key validation:** `LocalizedString` fields tint green when the (table, key) pair exists in the config selected in the Localization Manager, red when it doesn't (or is empty), and stay neutral when no config is selected. Green means the key *exists* — per-locale coverage lives in the Validation tab. Editing a `LocalizedText` to a valid key refreshes its TMP text immediately; invalid keys never touch the text.

## CSV Workflow

`Tools > TRnK > Localization Manager` → **Import / Export** tab.

**Source of truth** — in the Localization Manager, each table is a
spreadsheet: one row per key, one column per locale.

Table `UI`:

| Key | en | vi | ja |
|---|---|---|---|
| `play_button` | Play | Chơi | 再生 |
| `quit_button` | Quit | Thoát | 終了 |

Table `Combat`:

| Key | en | vi | ja |
|---|---|---|---|
| `victory` | Victory! | Chiến thắng! | 勝利！ |
| `defeat` | Defeat | Thất bại | 敗北 |

Export/import flattens every table into a single exchange file, with the
table name as the first column.

**Supported exchange formats:**

**Local CSV** (currently the only format; Google Sheets sync is planned for v0.4):

```
Table,Key,en,vi,ja
UI,play_button,Play,Chơi,再生
UI,quit_button,Quit,Thoát,終了
Combat,victory,Victory!,Chiến thắng!,勝利！
Combat,defeat,Defeat,Thất bại,敗北
```

- First two columns are always `Table` and `Key`; remaining columns are locale codes
- Semicolon-separated files (European/Vietnamese Excel exports) are auto-detected
- Handles BOM, quoted fields, escaped quotes (`""`), multi-line values, mixed line endings

**Import modes:**
- **Merge — CSV Wins** (default): adds new keys, overwrites values present in the CSV, preserves everything else
- **Replace All**: the CSV becomes the source of truth; keys not in the CSV are removed

Every import shows a diff preview (added / updated / removed) before applying, and the apply is undoable (Ctrl+Z).

Locales in the CSV that aren't registered in settings are skipped with a notice. Locales in settings missing from the CSV are preserved in Merge mode.

## Roadmap

### v0.1 — Runtime Foundation ✅ (complete, tested)
- Single-asset data model (`LocalizationConfig` holds all locales/tables/keys)
- Synchronous `Loc` facade: `Initialize` (settings instance), `Get`, `SetLocale`, `LocaleChanged`, `IsReady`
- Current-locale → default-locale fallback; empty values treated as missing
- `LocalizedString` serializable reference + `LocalizedText` TMP component
- `SetLocalizedText` TMP extensions
- Odin/non-Odin dual support (`SerializedScriptableObject` base when present, custom drawer otherwise)

### v0.2 — Authoring Workflow ✅ (code-complete; editor UI pending first Unity verification)
- UI Toolkit editor window (`Tools > TRnK > Localization Manager`)
- Tables tab: spreadsheet grid (`MultiColumnListView`), one row per key, one column per locale, inline editing, search, add/remove keys & tables
- Locales tab: manage locales, default-locale dropdown, Sync Entries
- Import/Export tab: CSV export (Excel-compatible, BOM); CSV import with diff preview, Merge/Replace modes, undo support
- Validation tab: per-locale coverage bars, missing translations, duplicate keys, empty names
- Robust CSV pipeline: BOM, quoted/multi-line fields, escaped quotes, comma/semicolon auto-detection (93 tests passing)

### v0.3 — Developer Workflow ✅ (code-complete; pending first Unity verification)
- Smart strings: `Loc.Get("Combat", "deal_damage", ("damage", 42), ("enemy", "Goblin"))` with named placeholders
- Zero-allocation hot path: `tmpText.SetLocalizedText(table, key, arg0…arg2)` via TMP's `SetText(format, args)` — for HP bars / score counters updating every frame (positional placeholders, up to 3 float args)
- Edit-Mode locale preview: locale dropdown in the Localization Manager toolbar — refreshes every `LocalizedText` in open scenes and the prefab stage without entering Play Mode
- Live key validation in every `LocalizedString` inspector: green = key exists, red = missing (Odin's own palette when Odin is installed); editing to a valid key auto-refreshes the `LocalizedText`'s TMP text
- Editor settings stored as a project asset (`Assets/Plugins/TRnK/Localization/Editor/`) — no machine-global `EditorPrefs`

### v0.4 — Single Source of Truth (planned)

**The spreadsheet becomes the only place translations are authored.** The config
asset is a build artifact: synced from the sheet, never hand-edited. No merge
modes, no reconciliation — the sheet always wins because nothing else writes.

The window drops to three tabs:

| Tab | Purpose | Editable |
|---|---|---|
| **Sync** | Sheet list + Sync; Export (seeds a new sheet / text backup) | sheet list only |
| **Tables** | Browse tables, keys and translations, with search | read-only |
| **Validation** | Coverage, missing values, duplicate keys — reported as *sheet* defects to fix upstream | read-only |

**Setup:** paste the spreadsheet URL once, then list the tab names to sync —
one tab per table, named as you want the table named:

```
Spreadsheet URL   https://docs.google.com/spreadsheets/d/<id>/edit
Tabs              UI, Combat, Event
```

Each tab is fetched by name, no API key or OAuth:
`https://docs.google.com/spreadsheets/d/<id>/gviz/tq?tqx=out:csv&sheet=<tab>`

- Sync: fetch every listed tab as CSV over HTTPS → existing parse → existing diff preview → existing undoable apply. Always Replace-All; the download never writes anything, only **Apply** does
- **All-or-nothing:** if any tab fails to fetch or parse, nothing reaches the preview — a partially-fetched sync under Replace-All would read as "that table was deleted"
- Non-blocking fetch via `EditorApplication.update` polling — the editor never freezes; timeout plus cancel through Unity's background `Progress` API
- Clear, named failures: tab not found, no network, sheet not link-shared (Google returns an HTML login page), empty response
- Renaming a tab is a deliberate two-step: sync fails on the old name, you update the name in the list, and Replace-All carries the data to the new table
- Removed: inline table/key editing, locale list editing, Merge-vs-Replace modes, manual CSV import — every one of them a second writer
- Preview dropdown also switches locale in **Play Mode** (via `Loc.SetLocale`), not just Edit Mode

**Requires** the spreadsheet shared as *Anyone with the link → Viewer*.

### v0.5 — Rename Safety (planned)

Renaming a spreadsheet tab renames its table. Code references break at compile
time (good), but `LocalizedText` components store the table as a serialized
string in scenes and prefabs — those go red in the inspector and must currently
be fixed by hand.

- Codegen table constants (`LocTable.CommonUI`) on sync — stale **code** references become compile errors naming every file and line
- Rename detection in the sync diff preview: when the table set changes from `{UI, …}` to `{Common UI, …}`, offer a migration that rewrites the stored table string across all scenes and prefabs
- Sample scenes and common patterns
- Full API documentation

### v1.0 — Release (planned)
- Codegen key constants (`Keys.UI.PlayButton`) — compile-time safety, build fails on missing keys

### Explicitly out of scope (by design)
- Addressables for strings — strings are tiny; per-locale streaming adds complexity for negligible memory savings
- ICU plural rules, RTL support, XLIFF, post-ship remote updates — enterprise-scale features; use Unity Localization if you need these

## Requirements

- Unity 6 or later
- [TRnK Toolkit](https://github.com/trnkdev/unity-trnk-toolkit) (`com.trnkdev.unitytoolkit`)
- TextMeshPro

## License

See [LICENSE.md](LICENSE.md) for license info.
