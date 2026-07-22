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

**1. Create the settings asset**

`Assets > Create > TRnK Localization > Settings` — place it anywhere inside a `Resources` folder (e.g. `Assets/Resources/LocalizationSettings.asset`).

**2. Author your data**

Open `Tools > TRnK > Localization Manager`:
- **Locales tab** — register your languages (`en`, `vi`, `ja`, …) and pick the default
- **Tables tab** — add tables (e.g. `UI`), add keys, type translations in the spreadsheet grid

**3. Initialize from your game bootstrap**

`Loc.Initialize` takes a `LocalizationSettings` instance — load it however your project loads assets (Resources, Addressables, a direct Inspector reference, …) and pass it in.

```csharp
using TRnK.Localization;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] LocalizationSettings _localizationSettings;

    void Awake()
    {
        Loc.Initialize(_localizationSettings);

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

// One-shot TMP extension
_label.SetLocalizedText("UI", "play_button");

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

## CSV Workflow

`Tools > TRnK > Localization Manager` → **Import / Export** tab.

**Format:**
```
Table,Key,en,vi,ja
UI,play_button,Play,Chơi,再生
UI,quit_button,Quit,Thoát,終了
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
- Single-asset data model (`LocalizationSettings` holds all locales/tables/keys)
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

### v0.3 — Developer Workflow (planned)
- Smart strings: `Loc.Get("Combat", "deal_damage", ("damage", 42), ("enemy", "Goblin"))` with named placeholders
- Zero-allocation hot path: `Loc.GetInto(tmpText, table, key, arg0…arg3)` using TMP's `SetText(format, args)` — for HP bars / score counters updating every frame (positional placeholders)
- In-editor locale preview: switch language in Edit Mode without entering Play Mode
- Validation extended: unused-key detection

### v0.4 — Polish (planned)
- `LocalizedTMPFont` component: per-locale font swap via direct references (no Addressables — right-sized for indie scope)
- Editor UX refinement based on real usage

### v0.5 — Production Polish (planned)
- Sample scenes and common patterns
- Full API documentation

### v1.0 — Release (planned)
- Codegen key constants (`Keys.UI.PlayButton`) — compile-time safety, build fails on missing keys
- Google Sheets sync (optional, opt-in)

### Explicitly out of scope (by design)
- Addressables for strings — strings are tiny; per-locale streaming adds complexity for negligible memory savings
- ICU plural rules, RTL support, XLIFF, post-ship remote updates — enterprise-scale features; use Unity Localization if you need these

## Requirements

- Unity 6 or later
- [TRnK Toolkit](https://github.com/trnkdev/unity-trnk-toolkit) (`com.trnkdev.unitytoolkit`)
- TextMeshPro

## License

See [LICENSE.md](LICENSE.md) for license info.
