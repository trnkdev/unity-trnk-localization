using System;
using TRnK.Logger;

namespace TRnK.Localization
{
    internal static class LocalizationService
    {
        internal static bool IsInitialized => s_config != null;
        internal static string CurrentLocale { get; private set; }
        internal static LocalizationConfig Config => s_config;

        internal static event Action<string> LocaleChanged;

        private static LocalizationConfig s_config;

        internal static void Initialize(LocalizationConfig config)
        {
            if (config == null)
            {
                Log.Error("Cannot initialize TRnK.Localization: config asset is null.");
                return;
            }

            // Idempotent — re-initializing with the same asset is a no-op
            if (s_config == config) return;

            string previousLocale = CurrentLocale;

            s_config = config;
            CurrentLocale = config.DefaultLocale;
            Log.Info($"TRnK.Localization initialized with '{config.name}'. Default locale: '{CurrentLocale}'.");

            // Swapping configs can change the locale; listeners must refresh or they keep the old language
            if (!string.Equals(previousLocale, CurrentLocale, StringComparison.Ordinal))
                LocaleChanged?.Invoke(CurrentLocale);
        }

        internal static void SetLocale(string localeCode)
        {
            if (!IsInitialized)
            {
                Log.Error("Cannot set locale: TRnK.Localization is not initialized. Call Loc.Initialize() first.");
                return;
            }

            if (string.IsNullOrEmpty(localeCode))
            {
                Log.Warn("Cannot set locale: code is null or empty.");
                return;
            }

            if (string.Equals(CurrentLocale, localeCode, StringComparison.Ordinal)) return;

            if (!s_config.HasLocale(localeCode))
            {
                Log.Warn($"Locale '{localeCode}' is not registered in LocalizationConfig. Ignoring.");
                return;
            }

            CurrentLocale = localeCode;
            LocaleChanged?.Invoke(CurrentLocale);
        }

        internal static string Get(string tableName, string key)
        {
#if UNITY_EDITOR
            // Editor tooling resolves against a config directly, without requiring Initialize
            if (!UnityEngine.Application.isPlaying && s_editorConfig != null)
                return GetFrom(s_editorConfig, s_editorLocale, tableName, key);
#endif
            if (!IsInitialized)
            {
                Log.Warn("TRnK.Localization is not initialized. Returning empty string.");
                return string.Empty;
            }

            return GetFrom(s_config, CurrentLocale, tableName, key);
        }

        private static string GetFrom(LocalizationConfig config, string locale, string tableName, string key)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(key))
                return MissingKey(tableName, key, locale);

            if (!config.TableExists(tableName))
            {
                Log.Warn($"Table '{tableName}' does not exist in LocalizationConfig.");
                return MissingKey(tableName, key, locale);
            }

            // 1. Requested locale (treats empty value as missing)
            if (config.TryGet(tableName, key, locale, out string value)
                && !string.IsNullOrEmpty(value))
                return value;

            // 2. Default locale fallback (only if different)
            var defaultLocale = config.DefaultLocale;
            if (!string.Equals(locale, defaultLocale, StringComparison.Ordinal)
                && config.TryGet(tableName, key, defaultLocale, out value)
                && !string.IsNullOrEmpty(value))
                return value;

            return MissingKey(tableName, key, locale);
        }

        private static string MissingKey(string tableName, string key, string locale)
        {
            var label = string.IsNullOrEmpty(tableName) ? key : $"{tableName}.{key}";
#if UNITY_EDITOR
            Log.Warn($"Missing localization key: '{label}' [locale: {locale}].");
            return $"#{label}";
#else
            return string.Empty;
#endif
        }

#if UNITY_EDITOR
        // Editor-tooling lookup override — set only by the editor assembly, inert while playing
        private static LocalizationConfig s_editorConfig;
        private static string s_editorLocale;

        internal static void SetEditorLookup(LocalizationConfig config, string localeCode)
        {
            s_editorConfig = config;
            s_editorLocale = localeCode;
        }

        internal static void ClearEditorLookup()
        {
            s_editorConfig = null;
            s_editorLocale = null;
        }

        [UnityEditor.InitializeOnEnterPlayMode]
        private static void ResetOnEnterPlayMode(UnityEditor.EnterPlayModeOptions _)
        {
            s_config = null;
            CurrentLocale = null;
            LocaleChanged = null;
            s_editorConfig = null;
            s_editorLocale = null;
        }
#endif
    }
}
