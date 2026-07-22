using System;
using TRnK.Logger;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TRnK.Localization
{
    [Serializable]
    public sealed class LocalizedString
    {
#if ODIN_INSPECTOR
        [HorizontalGroup("loc", Width = 0.45f), LabelText("Table")]
#endif
        [SerializeField] private string _table;

#if ODIN_INSPECTOR
        [HorizontalGroup("loc"), LabelText("Key")]
#endif
        [SerializeField] private string _key;

        public string Table => _table;
        public string Key   => _key;

        /// <summary>Returns the localized value using the current locale.</summary>
        public string Get()
        {
            if (string.IsNullOrWhiteSpace(_table) || string.IsNullOrWhiteSpace(_key))
            {
                Log.Warn("LocalizedString has an empty Table or Key.");
                return string.Empty;
            }

            return Loc.Get(_table, _key);
        }

        public override string ToString() => Get();
    }
}
