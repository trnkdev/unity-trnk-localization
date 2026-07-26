using System;
using System.Collections.Generic;
using TRnK.Logger;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TRnK.Localization
{
    [CreateAssetMenu(fileName = "LocalizationConfig", menuName = "TRnK/Localization/Localization Config")]
    public sealed class LocalizationConfig :
#if ODIN_INSPECTOR
        SerializedScriptableObject
#else
        ScriptableObject
#endif
    {
#if ODIN_INSPECTOR
        [BoxGroup("Locales"), LabelText("Default Locale"), ValueDropdown(nameof(GetLocaleCodes))]
#endif
        [SerializeField] private string _defaultLocale = "en";

#if ODIN_INSPECTOR
        [BoxGroup("Locales"), TableList(AlwaysExpanded = true, ShowIndexLabels = false)]
#endif
        [SerializeField] private List<Locale> _locales = new();

#if ODIN_INSPECTOR
        [BoxGroup("Tables"), ListDrawerSettings(ShowFoldout = true)]
#endif
        [SerializeField] private List<LocalizationTable> _tables = new();

        public string DefaultLocale => _defaultLocale;
        public IReadOnlyList<Locale> Locales => _locales;
        public IReadOnlyList<LocalizationTable> Tables => _tables;

        public bool HasLocale(string code)
        {
            if (string.IsNullOrEmpty(code)) return false;
            foreach (var locale in _locales)
                if (string.Equals(locale.Code, code, StringComparison.Ordinal))
                    return true;
            return false;
        }

        // Lazy index: tableName -> (key -> (localeCode -> value))
        private Dictionary<string, Dictionary<string, Dictionary<string, string>>> _index;

        internal bool TryGet(string tableName, string key, string localeCode, out string value)
        {
            if (_index == null) BuildIndex();

            if (_index.TryGetValue(tableName, out var tableIndex)
                && tableIndex.TryGetValue(key, out var keyIndex)
                && keyIndex.TryGetValue(localeCode, out value))
                return true;

            value = null;
            return false;
        }

        internal bool TableExists(string tableName)
        {
            if (_index == null) BuildIndex();
            return _index.ContainsKey(tableName);
        }

        internal bool KeyExists(string tableName, string key)
        {
            if (_index == null) BuildIndex();
            return _index.TryGetValue(tableName, out var tableIndex) && tableIndex.ContainsKey(key);
        }

        internal void InvalidateIndex() => _index = null;

        private void BuildIndex()
        {
            _index = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(
                _tables.Count, StringComparer.Ordinal);

            foreach (var table in _tables)
            {
                if (string.IsNullOrWhiteSpace(table.Name)) continue;

                if (_index.ContainsKey(table.Name))
                {
                    Log.Warn($"Duplicate table name '{table.Name}' in LocalizationConfig '{name}'.");
                    continue;
                }

                var tableDict = new Dictionary<string, Dictionary<string, string>>(
                    table.Entries.Count, StringComparer.Ordinal);

                foreach (var entry in table.Entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.Key)) continue;

                    if (tableDict.ContainsKey(entry.Key))
                    {
                        Log.Warn($"Duplicate key '{entry.Key}' in table '{table.Name}'.");
                        continue;
                    }

                    var keyDict = new Dictionary<string, string>(
                        entry.Values.Count, StringComparer.Ordinal);

                    foreach (var lv in entry.Values)
                    {
                        if (string.IsNullOrWhiteSpace(lv.LocaleCode)) continue;
                        keyDict[lv.LocaleCode] = lv.Value;
                    }

                    tableDict[entry.Key] = keyDict;
                }

                _index[table.Name] = tableDict;
            }
        }

#if ODIN_INSPECTOR
        private IEnumerable<string> GetLocaleCodes()
        {
            foreach (var locale in _locales)
                if (!string.IsNullOrWhiteSpace(locale.Code))
                    yield return locale.Code;
        }
#endif

#if UNITY_EDITOR
        private void OnValidate()
        {
            InvalidateIndex();

            if (!string.IsNullOrEmpty(_defaultLocale) && !HasLocale(_defaultLocale))
                Log.Warn($"DefaultLocale '{_defaultLocale}' is not in the Locales list on '{name}'.");
        }

#endif
    }
}
