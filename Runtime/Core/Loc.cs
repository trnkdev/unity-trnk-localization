using System;
using System.Threading.Tasks;

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

        /// <summary>Initializes the service with a config and an optional starting locale.</summary>
        public static void Initialize(LocalizationConfig config, string initialLocale = null)
            => LocalizationService.Initialize(config, initialLocale);

        /// <summary>Switches the active locale. Fires <see cref="LocaleChanged"/> if the locale changes.</summary>
        public static void SetLocale(string localeCode)
            => LocalizationService.SetLocale(localeCode);

        /// <summary>Loads a table from Addressables by name. Completes with false if it could not be loaded.</summary>
        public static Task<bool> LoadTableAsync(string tableName)
            => LocalizationService.LoadTableAsync(tableName);

        /// <summary>Unregisters a table and releases it if it was loaded from Addressables.</summary>
        public static bool ReleaseTable(string tableName)
            => LocalizationService.ReleaseTable(tableName);

        /// <summary>Registers a table the game references or loaded itself.</summary>
        public static void AddTable(LocalizationTable table)
            => LocalizationService.AddTable(table);

        /// <summary>Whether a table is registered.</summary>
        public static bool HasTable(string tableName)
            => LocalizationService.HasTable(tableName);

        /// <summary>Returns the translated string for a table/key pair, falling back to the default locale when missing.</summary>
        public static string Get(string tableName, string key)
            => LocalizationService.Get(tableName, key);

        /// <summary>Returns the translated string with named {placeholder} tokens replaced by the given arguments.</summary>
        public static string Get(string tableName, string key, params LocArg[] args)
            => PlaceholderFormatter.Format(LocalizationService.Get(tableName, key), args);
    }
}
