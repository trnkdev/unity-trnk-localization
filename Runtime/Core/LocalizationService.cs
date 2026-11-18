using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TRnK.Logger;

#if TRNK_LOCALIZATION_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace TRnK.Localization
{
    internal static class LocalizationService
    {
        private const string AddressPrefix = "Localization/";

        internal static bool IsInitialized => s_config != null;
        internal static string CurrentLocale { get; private set; }
        internal static LocalizationConfig Config => s_config;

        internal static event Action<string> LocaleChanged;

        private static readonly Task<bool> s_loaded = Task.FromResult(true);
        private static readonly Task<bool> s_notLoaded = Task.FromResult(false);

        private static LocalizationConfig s_config;
        private static readonly Dictionary<string, LocalizationTable> s_tables = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Task<bool>> s_pendingLoads = new(StringComparer.Ordinal);

#if TRNK_LOCALIZATION_ADDRESSABLES
        private static readonly Dictionary<string, AsyncOperationHandle<LocalizationTable>> s_handles = new(StringComparer.Ordinal);
#endif

        /// <summary>The Addressables address of a table.</summary>
        internal static string AddressOf(string tableName) => AddressPrefix + tableName;

        internal static void Initialize(LocalizationConfig config, string initialLocale)
        {
            if (config == null)
            {
                Log.Error("Cannot initialize TRnK.Localization: config asset is null.");
                return;
            }

            bool sameConfig = s_config == config;
            if (!sameConfig)
            {
                ReleaseAllHandles();
                s_config = config;
                s_tables.Clear();
                s_pendingLoads.Clear();

                Log.Info($"TRnK.Localization initialized with '{config.name}'.");
            }

            // Same config and no locale: keep the current locale
            if (sameConfig && string.IsNullOrEmpty(initialLocale)) return;

            string previousLocale = CurrentLocale;
            CurrentLocale = ResolveLocale(config, initialLocale);

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

        internal static void AddTable(LocalizationTable table)
        {
            if (!IsInitialized)
            {
                Log.Error("Cannot add table: TRnK.Localization is not initialized. Call Loc.Initialize() first.");
                return;
            }

            if (table == null)
            {
                Log.Error("Cannot add table: table asset is null.");
                return;
            }

            Register(table);
        }

        internal static Task<bool> LoadTableAsync(string tableName)
        {
            if (!IsInitialized)
            {
                Log.Error("Cannot load a table: TRnK.Localization is not initialized. Call Loc.Initialize() first.");
                return s_notLoaded;
            }

            if (string.IsNullOrEmpty(tableName))
            {
                Log.Error("Cannot load a table: name is null or empty.");
                return s_notLoaded;
            }

            if (s_tables.ContainsKey(tableName)) return s_loaded;
            if (s_pendingLoads.TryGetValue(tableName, out var pending)) return pending;

#if TRNK_LOCALIZATION_ADDRESSABLES
            var load = LoadAddressableAsync(tableName);

            // A load that completed synchronously already cleaned up after itself
            if (!load.IsCompleted) s_pendingLoads[tableName] = load;
            return load;
#else
            Log.Warn($"Cannot load table '{tableName}': Addressables is not installed. Reference the table and call Loc.AddTable instead.");
            return s_notLoaded;
#endif
        }

        internal static bool ReleaseTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return false;

            bool removed = s_tables.Remove(tableName);
#if TRNK_LOCALIZATION_ADDRESSABLES
            if (s_handles.Remove(tableName, out var handle) && handle.IsValid())
                Addressables.Release(handle);
#endif
            return removed;
        }

        internal static bool HasTable(string tableName)
            => !string.IsNullOrEmpty(tableName) && s_tables.ContainsKey(tableName);

#if TRNK_LOCALIZATION_ADDRESSABLES
        private static async Task<bool> LoadAddressableAsync(string tableName)
        {
            var config = s_config;
            var handle = Addressables.LoadAssetAsync<LocalizationTable>(AddressOf(tableName));

            try
            {
                await handle.Task;
            }
            finally
            {
                s_pendingLoads.Remove(tableName);
            }

            // Re-initialized with another config while loading
            if (s_config != config)
            {
                Addressables.Release(handle);
                return false;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Log.Error($"Could not load table '{tableName}' from Addressables address '{AddressOf(tableName)}'. Check the table name, or import the spreadsheet again.");
                Addressables.Release(handle);
                return false;
            }

            Register(handle.Result);
            s_handles[tableName] = handle;
            return true;
        }
#endif

        private static void ReleaseAllHandles()
        {
#if TRNK_LOCALIZATION_ADDRESSABLES
            foreach (var handle in s_handles.Values)
                if (handle.IsValid()) Addressables.Release(handle);
            s_handles.Clear();
#endif
        }

        internal static string Get(string tableName, string key)
        {
#if UNITY_EDITOR
            // Edit Mode preview, no Initialize needed
            if (!UnityEngine.Application.isPlaying && s_editorTables != null)
                return Lookup(s_editorTables, s_editorLocale, s_editorDefaultLocale, tableName, key);
#endif
            if (!IsInitialized)
            {
                Log.Warn("TRnK.Localization is not initialized. Returning empty string.");
                return string.Empty;
            }

            return Lookup(s_tables, CurrentLocale, s_config.DefaultLocale, tableName, key);
        }

        private static void Register(LocalizationTable table)
        {
            if (string.IsNullOrEmpty(table.TableName))
            {
                Log.Warn($"Localization table asset '{table.name}' has no table name. Import the spreadsheet again.");
                return;
            }

            if (s_tables.TryGetValue(table.TableName, out var existing) && existing != table)
                Log.Warn($"Table '{table.TableName}' is already registered by '{existing.name}'; replacing it with '{table.name}'.");

            s_tables[table.TableName] = table;
        }

        private static string ResolveLocale(LocalizationConfig config, string requested)
        {
            if (string.IsNullOrEmpty(requested)) return config.DefaultLocale;
            if (config.HasLocale(requested)) return requested;

            Log.Warn($"Locale '{requested}' is not registered in LocalizationConfig. Using default '{config.DefaultLocale}'.");
            return config.DefaultLocale;
        }

        private static string Lookup(IReadOnlyDictionary<string, LocalizationTable> tables,
                                     string locale, string defaultLocale, string tableName, string key)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(key))
                return MissingKey(tableName, key, locale);

            if (!tables.TryGetValue(tableName, out var table))
            {
                Log.Warn($"Table '{tableName}' is not loaded. Call Loc.LoadTableAsync(\"{tableName}\") first.");
                return MissingKey(tableName, key, locale);
            }

            // 1. Requested locale (treats empty value as missing)
            if (table.TryGetValue(key, locale, out string value) && !string.IsNullOrEmpty(value))
                return value;

            // 2. Default locale fallback (only if different)
            if (!string.Equals(locale, defaultLocale, StringComparison.Ordinal)
                && table.TryGetValue(key, defaultLocale, out value)
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
        private static IReadOnlyDictionary<string, LocalizationTable> s_editorTables;
        private static string s_editorLocale;
        private static string s_editorDefaultLocale;

        internal static void SetEditorLookup(IReadOnlyDictionary<string, LocalizationTable> tables,
                                             string localeCode, string defaultLocale)
        {
            s_editorTables = tables;
            s_editorLocale = localeCode;
            s_editorDefaultLocale = defaultLocale;
        }

        internal static void ClearEditorLookup()
        {
            s_editorTables = null;
            s_editorLocale = null;
            s_editorDefaultLocale = null;
        }

        [UnityEditor.InitializeOnEnterPlayMode]
        private static void ResetOnEnterPlayMode(UnityEditor.EnterPlayModeOptions _)
        {
            s_config = null;
            s_tables.Clear();
            s_pendingLoads.Clear();
#if TRNK_LOCALIZATION_ADDRESSABLES
            // Handles from the previous session belong to an Addressables instance that no longer exists
            s_handles.Clear();
#endif
            CurrentLocale = null;
            LocaleChanged = null;
            ClearEditorLookup();
        }
#endif
    }
}
