using System;
using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TRnK.Localization
{
    [Serializable]
    public sealed class LocalizationTable
    {
        [SerializeField] private string _name;

#if ODIN_INSPECTOR
        [TableList(AlwaysExpanded = true, DrawScrollView = true, ShowIndexLabels = false)]
#endif
        [SerializeField] private List<TableEntry> _entries = new();

        public string Name
        {
            get => _name;
            internal set => _name = value;
        }

        public IReadOnlyList<TableEntry> Entries => _entries;

        internal List<TableEntry> EditEntries => _entries;
    }
}
