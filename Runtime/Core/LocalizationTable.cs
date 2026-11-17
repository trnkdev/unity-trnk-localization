using System;
using System.Collections.Generic;
using TRnK.Logger;
using UnityEngine;

namespace TRnK.Localization
{
    /// <summary>Translations of one spreadsheet tab, written by the sheet sync.</summary>
    public sealed class LocalizationTable : ScriptableObject
    {
        [SerializeField] private string _tableName;
        [SerializeField] private string[] _locales = Array.Empty<string>();
        [SerializeField] private string[] _keys = Array.Empty<string>();

        // Row-major: [row * _locales.Length + column]
        [SerializeField] private string[] _values = Array.Empty<string>();

        [NonSerialized] private Dictionary<string, int> _rowByKey;

        /// <summary>The spreadsheet tab name, used by <c>Loc.Get</c>.</summary>
        public string TableName => _tableName;

        public IReadOnlyList<string> Locales => _locales;
        public IReadOnlyList<string> Keys => _keys;

        public bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return RowByKey.ContainsKey(key);
        }

        /// <summary>Looks up a key in one locale. An empty cell counts as found.</summary>
        public bool TryGetValue(string key, string localeCode, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(localeCode)) return false;

            int column = Array.IndexOf(_locales, localeCode);
            if (column < 0 || !RowByKey.TryGetValue(key, out int row)) return false;

            value = _values[row * _locales.Length + column];
            return true;
        }

        /// <summary>Value at a key row and locale column.</summary>
        public string GetValue(int row, int localeIndex) => _values[row * _locales.Length + localeIndex];

        public int IndexOfLocale(string localeCode) => Array.IndexOf(_locales, localeCode);

        private Dictionary<string, int> RowByKey => _rowByKey ??= BuildIndex();

        private Dictionary<string, int> BuildIndex()
        {
            var index = new Dictionary<string, int>(_keys.Length, StringComparer.Ordinal);

            if (_values.Length != _keys.Length * _locales.Length)
            {
                Log.Error($"Localization table '{_tableName}' is corrupted: {_values.Length} values for {_keys.Length} keys x {_locales.Length} locales. Re-sync it from the spreadsheet.");
                return index;
            }

            for (int row = 0; row < _keys.Length; row++)
                index[_keys[row]] = row;

            return index;
        }

#if UNITY_EDITOR
        /// <summary>Replaces the table's contents. Values are row-major.</summary>
        internal void SetData(string tableName, string[] locales, string[] keys, string[] values)
        {
            _tableName = tableName;
            _locales = locales;
            _keys = keys;
            _values = values;
            _rowByKey = null;
        }

        private void OnValidate() => _rowByKey = null;
#endif
    }
}
