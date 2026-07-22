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

            s_config = config;
            CurrentLocale = config.DefaultLocale;
            Log.Info($"TRnK.Localization initialized with '{config.name}'. Default locale: '{CurrentLocale}'.");
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
            if (!IsInitialized)
            {
                Log.Warn("TRnK.Localization is not initialized. Returning empty string.");
                return string.Empty;
            }

            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(key))
                return MissingKey(tableName, key);

            if (!s_config.TableExists(tableName))
            {
                Log.Warn($"Table '{tableName}' does not exist in LocalizationConfig.");
                return MissingKey(tableName, key);
            }

            // 1. Current locale (treats empty value as missing)
            if (s_config.TryGet(tableName, key, CurrentLocale, out string value)
                && !string.IsNullOrEmpty(value))
                return value;

            // 2. Default locale fallback (only if different)
            var defaultLocale = s_config.DefaultLocale;
            if (!string.Equals(CurrentLocale, defaultLocale, StringComparison.Ordinal)
                && s_config.TryGet(tableName, key, defaultLocale, out value)
                && !string.IsNullOrEmpty(value))
                return value;

            return MissingKey(tableName, key);
        }

        private static string MissingKey(string tableName, string key)
        {
            var label = string.IsNullOrEmpty(tableName) ? key : $"{tableName}.{key}";
#if UNITY_EDITOR
            Log.Warn($"Missing localization key: '{label}' [locale: {CurrentLocale}].");
            return $"#{label}";
#else
            return string.Empty;
#endif
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnEnterPlayMode]
        private static void ResetOnEnterPlayMode(UnityEditor.EnterPlayModeOptions _)
        {
            s_config = null;
            CurrentLocale = null;
            LocaleChanged = null;
        }
#endif
    }
}
