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

        /// <summary>Fired when the active locale changes, with the new code. A listener that throws doesn't stop the others.</summary>
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

        /// <summary>Looks up a translation without logging or returning a missing-key marker.</summary>
        public static bool TryGet(string tableName, string key, out string value)
            => LocalizationService.TryGet(tableName, key, out value);

        /// <summary>Returns the translated string with {0} replaced by the argument.</summary>
        public static string Get(string tableName, string key, string arg0)
            => Format(tableName, key, new LocArgs(1, arg0));

        /// <summary>Returns the translated string with {0}–{1} replaced by the arguments.</summary>
        public static string Get(string tableName, string key, string arg0, string arg1)
            => Format(tableName, key, new LocArgs(2, arg0, arg1));

        /// <summary>Returns the translated string with {0}–{2} replaced by the arguments.</summary>
        public static string Get(string tableName, string key, string arg0, string arg1, string arg2)
            => Format(tableName, key, new LocArgs(3, arg0, arg1, arg2));

        /// <summary>Returns the translated string with {0}–{3} replaced by the arguments.</summary>
        public static string Get(string tableName, string key, string arg0, string arg1, string arg2, string arg3)
            => Format(tableName, key, new LocArgs(4, arg0, arg1, arg2, arg3));

        /// <summary>Returns the translated string with {0}–{4} replaced by the arguments.</summary>
        public static string Get(string tableName, string key, string arg0, string arg1, string arg2, string arg3,
                                 string arg4)
            => Format(tableName, key, new LocArgs(5, arg0, arg1, arg2, arg3, arg4));

        /// <summary>Returns the translated string with {0}–{5} replaced by the arguments.</summary>
        public static string Get(string tableName, string key, string arg0, string arg1, string arg2, string arg3,
                                 string arg4, string arg5)
            => Format(tableName, key, new LocArgs(6, arg0, arg1, arg2, arg3, arg4, arg5));

        /// <summary>Returns the translated string with {0}–{6} replaced by the arguments.</summary>
        public static string Get(string tableName, string key, string arg0, string arg1, string arg2, string arg3,
                                 string arg4, string arg5, string arg6)
            => Format(tableName, key, new LocArgs(7, arg0, arg1, arg2, arg3, arg4, arg5, arg6));

        /// <summary>Returns the translated string with {0}–{7} replaced by the arguments.</summary>
        public static string Get(string tableName, string key, string arg0, string arg1, string arg2, string arg3,
                                 string arg4, string arg5, string arg6, string arg7)
            => Format(tableName, key, new LocArgs(8, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7));

        private static string Format(string tableName, string key, in LocArgs args)
            => PlaceholderFormatter.Format(LocalizationService.Get(tableName, key), args);
    }
}
