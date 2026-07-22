## [0.1.0] - 2026-07-22

### First Release

- Single-asset data model (`LocalizationConfig` holds all locales/tables/keys).
- Synchronous `Loc` facade: `Initialize` (settings instance), `Get`, `SetLocale`, `LocaleChanged`, `IsReady`.
- Current-locale to default-locale fallback; empty values treated as missing.
- `LocalizedString` serializable reference and `LocalizedText` TMP component.
- `SetLocalizedText` TMP extensions.
- Odin/non-Odin dual support (`SerializedScriptableObject` base when present, custom drawer otherwise).
- UI Toolkit editor window (`Tools > TRnK > Localization Manager`) with Tables, Locales, Import/Export, and Validation tabs.
- CSV import/export pipeline with diff preview, Merge/Replace modes, and undo support.
