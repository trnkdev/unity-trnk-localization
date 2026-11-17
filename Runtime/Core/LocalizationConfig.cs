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

        /// <summary>Finds a listed table by its table name, or null.</summary>
        internal LocalizationTable FindTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return null;
            foreach (var table in _tables)
                if (table != null && string.Equals(table.TableName, tableName, StringComparison.Ordinal))
                    return table;
            return null;
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
            if (!string.IsNullOrEmpty(_defaultLocale) && !HasLocale(_defaultLocale))
                Log.Warn($"DefaultLocale '{_defaultLocale}' is not in the Locales list on '{name}'.");
        }
#endif
    }
}
