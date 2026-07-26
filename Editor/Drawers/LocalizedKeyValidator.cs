#if UNITY_EDITOR
using UnityEngine;

namespace TRnK.Localization
{
    internal enum LocalizedKeyState
    {
        NotValidated,
        NoConfig,
        Missing,
        Valid
    }

    internal readonly struct LocalizedKeyResult
    {
        internal readonly LocalizedKeyState State;
        internal readonly string Message;

        internal LocalizedKeyResult(LocalizedKeyState state, string message)
        {
            State = state;
            Message = message;
        }

        internal bool IsValid => State == LocalizedKeyState.Valid;
    }

    /// <summary>Checks LocalizedString table/key pairs against the active config and refreshes edited LocalizedText components.</summary>
    internal static class LocalizedKeyValidator
    {
        // Field tints — #4FC354 / #D93638
        internal static readonly Color ValidColor = new(0.310f, 0.765f, 0.329f);
        internal static readonly Color ErrorColor = new(0.851f, 0.212f, 0.220f);

        // Row fills — #3A5E35 / #6A2B2C, kept translucent so the row reads on both editor skins
        internal static readonly Color ValidBackground = new(0.227f, 0.369f, 0.208f, 0.5f);
        internal static readonly Color ErrorBackground = new(0.416f, 0.169f, 0.173f, 0.5f);

        internal const string NoConfigMessage =
            "No localization data. Open Tools > TRnK > Localization Manager and select a config.";

        internal static LocalizedKeyResult Validate(string table, string key)
        {
            var config = LocalizationEditorSettings.GetOrCreate().ActiveConfig;
            if (config == null)
                return new LocalizedKeyResult(LocalizedKeyState.NoConfig, NoConfigMessage);

            bool noTable = string.IsNullOrWhiteSpace(table);
            bool noKey = string.IsNullOrWhiteSpace(key);

            if (noTable && noKey)
                return new LocalizedKeyResult(LocalizedKeyState.Missing, "Table and Key are empty.");
            if (noTable)
                return new LocalizedKeyResult(LocalizedKeyState.Missing, "Table is empty.");
            if (noKey)
                return new LocalizedKeyResult(LocalizedKeyState.Missing, "Key is empty.");

            if (!config.TableExists(table))
                return new LocalizedKeyResult(LocalizedKeyState.Missing, $"Table '{table}' not found in '{config.name}'.");

            if (!config.KeyExists(table, key))
                return new LocalizedKeyResult(LocalizedKeyState.Missing, $"Key '{key}' not found in table '{table}'.");

            return new LocalizedKeyResult(LocalizedKeyState.Valid, $"'{table}/{key}' found in '{config.name}'.");
        }

        /// <summary>Refreshes the component's TMP text from the config — used after a successful validation.</summary>
        internal static void RefreshText(LocalizedText component)
        {
            if (component == null) return;

            var config = LocalizationEditorSettings.GetOrCreate().ActiveConfig;
            if (config == null) return;

            bool previewActive = LocalePreview.ActiveLocale != null;

            if (!previewActive)
                LocalizationService.SetEditorPreview(config, config.DefaultLocale);

            component.RefreshEditorPreview();

            if (!previewActive)
                LocalizationService.ClearEditorPreview();
        }
    }
}
#endif
