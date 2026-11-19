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

        public string DefaultLocale => _defaultLocale;
        public IReadOnlyList<Locale> Locales => _locales;

        public bool HasLocale(string code) => FindLocaleCode(code) != null;

        /// <summary>The config's spelling of a locale code matched ignoring case, or null.</summary>
        internal string FindLocaleCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            foreach (var locale in _locales)
                if (string.Equals(locale.Code, code, StringComparison.OrdinalIgnoreCase))
                    return locale.Code;
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
            // A new config has no locales until its first import
            if (_locales.Count > 0 && !string.IsNullOrEmpty(_defaultLocale) && !HasLocale(_defaultLocale))
                Log.Warn($"DefaultLocale '{_defaultLocale}' is not in the Locales list on '{name}'.");
        }
#endif
    }
}
