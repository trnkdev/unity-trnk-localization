# TRnK Localization

Lightweight, text-first localization for Unity. Translations live in a Google Sheet, import into table assets with one button, and are read with a plain synchronous `Loc.Get`.

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

**Optional:** install **Addressables** (`com.unity.addressables`) to load tables on demand. The package detects it automatically.

## Features

- **Google Sheets import** — one tab per table, one column per language; no API key or OAuth.
- **Synchronous lookups** — `Loc.Get("UI", "play")` with fallback to the default language.
- **Placeholders** — `{0}`–`{7}` filled from string arguments.
- **TMP integration** — `SetLocalizedText` extensions, including a zero-allocation path for per-frame text.
- **`LocalizedText` component** — set a table and key in the Inspector; refreshes on language change.
- **On-demand tables** — load and release level chunks or seasonal events through Addressables.
- **Editor tooling** — Localization Manager window, key validation in the Inspector, per-component language preview.
- **Odin Inspector support** when Odin is installed.

## Quick Start

### 1. Author the spreadsheet

One spreadsheet, one tab per table. Each tab has a `Key` column followed by one column per language code:

| Key | en | ja | vi |
|---|---|---|---|
| `play` | Play | プレイ | Chơi |
| `welcome` | Welcome back, {0}! | おかえりなさい、{0}さん！ | Chào mừng trở lại, {0}! |

Share it as *Anyone with the link → Viewer*.

### 2. Create the config

`Assets > Create > TRnK > Localization > Localization Config`. The config holds the language list and the default language, both filled in by the import.

### 3. Import

Open `Tools > TRnK > Localization Manager`, select the config, then on the **Import** tab paste the spreadsheet URL and press **Import From Spreadsheet**. Each tab becomes a `LocalizationTable` asset (`UI.asset`, `Battle.asset`, …).

### 4. Initialize at boot

```csharp
using TRnK.Localization;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private LocalizationConfig _config;
    [SerializeField] private LocalizationTable _uiTable; // always loaded

    private void Awake()
    {
        // A null or unknown saved code falls back to the config's default language
        Loc.Initialize(_config, SaveSystem.Load().languageCode);
        Loc.AddTable(_uiTable);
    }
}
```

## Getting Text

```csharp
string label = Loc.Get("UI", "play");

// Placeholders {0}–{7}; format numbers yourself
// "attack_hit" = "{0} attacks {1} for {2} damage."
string line = Loc.Get("Battle", "attack_hit", "Aria", "Goblin", damage.ToString());

// Silent lookup: no warning, no missing marker
if (Loc.TryGet("Battle", "skill_meteor", out string skill)) Show(skill);
```

- Lookups try the current language, then the default one. An empty cell counts as missing.
- A missing key logs a warning and returns `#Table.key` in the Editor, `""` in builds.
- Table names, keys and language codes ignore case.
- `{{` and `}}` write literal braces. A placeholder without an argument stays as written and logs a warning.

## TextMeshPro

```csharp
_label.SetLocalizedText("UI", "play");
_goldLabel.SetLocalizedText("UI", "gold", gold.ToString());   // up to 8 string args
_label.SetLocalizedText(_localizedString);

// Zero-allocation for text that changes every frame, using TMP's own formatter (up to 3 floats)
// "hp_bar" = "{0:0}/{1:0} HP"
_hpLabel.SetLocalizedText("UI", "hp_bar", current, max);
```

These set the text once. Use `LocalizedText`, or listen to `LocaleChanged`, to refresh on language change.

## LocalizedText and LocalizedString

**`LocalizedText`** — `Add Component > TRnK Localization > Localized Text` on a TMP object, then set the table and key. It reads its text when enabled and whenever the language changes.

**`LocalizedString`** — a serializable table + key pair for your own components:

```csharp
[SerializeField] private LocalizedString _tooltip;

private void Show() => _tooltipLabel.text = _tooltip.Get();
```

## Changing Language

```csharp
public void SetLanguage(string code)
{
    Loc.SetLocale(code);
    SaveSystem.Save(code);
}

// Build a language menu from the config
foreach (Locale locale in _config.Locales)
    AddButton(locale.Name, locale.Code);   // "Japanese", "ja"
```

Listen to `LocaleChanged` to refresh text you set yourself:

```csharp
private void OnEnable()
{
    Loc.LocaleChanged += OnLocaleChanged;
    Refresh();   // a disabled object misses changes, so refresh on enable too
}

private void OnDisable() => Loc.LocaleChanged -= OnLocaleChanged;

private void OnLocaleChanged(string code) => Refresh();
```

- `SetLocale` with a code the config doesn't list is ignored with a warning.
- The first `Initialize` doesn't raise `LocaleChanged`; later language changes do.
- A listener that throws is logged and doesn't stop the others. A listener whose object was destroyed without unsubscribing is removed with a warning.

## Loading Tables

`Loc.Get` only reads registered tables. Add the ones you reference directly, and load the rest on demand:

```csharp
// Always-loaded tables, referenced from a component
Loc.AddTable(_uiTable);

// On demand (requires Addressables)
if (await Loc.LoadTableAsync("Level_101_200"))
    Loc.ReleaseTable("Level_1_100");

bool ready = Loc.HasTable("Event_Halloween");
```

- `LoadTableAsync` completes with `false` if the table can't be loaded. Two calls for the same table share one load.
- Load a table before showing UI that uses it, and release it after leaving that UI.
- Each table holds every language, so `SetLocale` never reloads anything.

## API Reference

### Loc

| Member | Description |
|---|---|
| `Initialize(config, initialLocale = null)` | Sets the config and starting language. Call once at boot. |
| `IsReady` | Whether `Initialize` has run. |
| `CurrentLocale` | Active language code. |
| `SetLocale(code)` | Switches language and raises `LocaleChanged`. |
| `LocaleChanged` | `event Action<string>` with the new code. |
| `Get(table, key)` | Translated text, falling back to the default language. |
| `Get(table, key, arg0 … arg7)` | Same, with `{0}`–`{7}` replaced. |
| `TryGet(table, key, out value)` | Silent lookup; `false` when missing. |
| `AddTable(table)` | Registers a table asset. |
| `LoadTableAsync(table)` | Loads a table from Addressables; completes with `true` when registered. |
| `ReleaseTable(table)` | Unregisters a table and releases its Addressables handle. |
| `HasTable(table)` | Whether a table is registered. |

### Other types

| Type | Description |
|---|---|
| `LocalizationConfig` | `Locales` (code + name), `DefaultLocale`, `HasLocale(code)`. |
| `LocalizationTable` | One imported tab: `TableName`, `Keys`, `Locales`, `ContainsKey`, `TryGetValue`. |
| `LocalizedString` | Serializable table + key; `Get()`. |
| `LocalizedText` | TMP component that refreshes on language change; `Refresh()`. |
| `TMP_Text.SetLocalizedText(...)` | One-shot setters: no args, 1–8 strings, 1–3 floats, or a `LocalizedString`. |

## Localization Manager

Open via `Tools > TRnK > Localization Manager` and pick the config in the top bar.

**Import tab**

| Field | Description |
|---|---|
| Spreadsheet URL | Any URL of the spreadsheet. |
| Tables Folder | Where the table assets live. Empty uses the config's folder. |
| Ignored Sheet Tabs | Tabs to skip, e.g. `Rule`. |

- Every tab that isn't ignored becomes a table of the same name. A tab whose first header isn't `Key` is skipped with a warning.
- The language columns define the config's languages.
- A typed `\n` in a cell becomes a line break.
- Importing again updates existing assets in place, so references to them survive. A tab removed from the sheet has its asset moved to the trash. Use a separate Tables Folder per config.
- Ctrl+Z undoes an import.

**Tables tab** — read-only view of every table as a key × language grid, with search.

**With Addressables** — each table the import creates is made Addressable at `Localization/<tablename>` (lowercase) in a `Localization` group, one bundle per table. After that the import leaves its Addressables setup alone, so you can move it to another group, or remove it from Addressables to reference it directly.

**In the Inspector** — `LocalizedString` fields turn green when the key exists in the selected config and red when it doesn't. Each `LocalizedText` has a **Selected Language** dropdown that previews its text in Edit Mode.

## Requirements

- Unity 6 or later
- [TRnK Toolkit](https://github.com/trnkdev/unity-trnk-toolkit) (`com.trnkdev.unitytoolkit`)
- TextMeshPro
- Optional: Addressables, Odin Inspector

## License

See [LICENSE.md](LICENSE.md) for license info.
