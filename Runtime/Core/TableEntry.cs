using System;
using System.Collections.Generic;
using UnityEngine;

namespace TRnK.Localization
{
    [Serializable]
    public sealed class TableEntry
    {
        [SerializeField] private string _key;
        [SerializeField] private List<LocaleValue> _values = new();

        public string Key
        {
            get => _key;
            internal set => _key = value;
        }

        public IReadOnlyList<LocaleValue> Values => _values;

        internal List<LocaleValue> EditValues => _values;
    }
}
