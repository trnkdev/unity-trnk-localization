using System;

namespace TRnK.Localization
{
    /// <summary>Public entry point for TRnK.Localization.</summary>
    public static class Loc
    {
        /// <summary>Whether the localization service is ready to serve translations.</summary>
        public static bool IsReady => LocalizationService.IsInitialized;

        /// <summary>The currently active locale code (e.g. "en", "ja", "vi").</summary>
        public static string CurrentLocale => LocalizationService.CurrentLocale;

        /// <summary>Fired when the active locale changes. The argument is the new locale code.</summary>
        public static event Action<string> LocaleChanged
        {
            add => LocalizationService.LocaleChanged += value;
            remove => LocalizationService.LocaleChanged -= value;
        }

        /// <summary>Initializes the localization service with a settings instance.</summary>
        public static void Initialize(LocalizationSettings settings)
            => LocalizationService.Initialize(settings);

        /// <summary>Switches the active locale. Fires <see cref="LocaleChanged"/> if the locale changes.</summary>
        public static void SetLocale(string localeCode)
            => LocalizationService.SetLocale(localeCode);

        /// <summary>Returns the translated string for a table/key pair, falling back to the default locale when missing.</summary>
        public static string Get(string tableName, string key)
            => LocalizationService.Get(tableName, key);
    }
}
