#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    /// <summary>Read-only browser for the synced tables, keys and translations.</summary>
    internal sealed class TablesTab : ITab
    {
        private const string ReadOnlyNotice =
            "Read-only — the spreadsheet is the source of truth. Edit there, then Sync.";

        public string Title => "Tables";
        public VisualElement Root { get; }

        private LocalizationConfig _config;

        private ListView _tableList;
        private readonly List<string> _tableNames = new();
        private int _selectedTableIndex = -1;

        private VisualElement _rightPane;
        private MultiColumnListView _grid;
        private ToolbarSearchField _search;
        private string _searchFilter = string.Empty;

        private readonly List<int> _visibleEntryIndices = new();
        private VisualElement _emptyState;

        internal TablesTab()
        {
            Root = new VisualElement { name = "tab-tables-root" };
            Root.style.flexGrow = 1;
            Root.style.flexDirection = FlexDirection.Row;

            BuildLeftPane();
            BuildRightPane();
        }

        private void BuildLeftPane()
        {
            var left = new VisualElement { name = "tables-left" };
            left.style.width = 180;
            left.style.borderRightWidth = 1;
            left.style.borderRightColor = new Color(0, 0, 0, 0.3f);

            var header = new Label("Tables");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.paddingLeft = 6;
            header.style.paddingTop = 6;
            header.style.paddingBottom = 4;
            left.Add(header);

            _tableList = new ListView
            {
                itemsSource = _tableNames,
                fixedItemHeight = 20,
                selectionType = SelectionType.Single,
                makeItem = () => new Label { style = { paddingLeft = 6, unityTextAlign = TextAnchor.MiddleLeft } },
                bindItem = (e, i) => ((Label)e).text = _tableNames[i],
            };
            _tableList.style.flexGrow = 1;
            _tableList.selectionChanged += _ => OnTableSelected(_tableList.selectedIndex);
            left.Add(_tableList);

            Root.Add(left);
        }

        private void BuildRightPane()
        {
            _rightPane = new VisualElement { name = "tables-right" };
            _rightPane.style.flexGrow = 1;

            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingTop = 4, paddingBottom = 4, paddingLeft = 4 } };

            _search = new ToolbarSearchField();
            _search.style.flexGrow = 1;
            _search.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue ?? string.Empty;
                RebuildGrid();
            });
            toolbar.Add(_search);
            _rightPane.Add(toolbar);

            _rightPane.Add(LocalizationStyles.Hint(ReadOnlyNotice));

            Root.Add(_rightPane);
        }

        public void OnConfigChanged(LocalizationConfig config)
        {
            _config = config;
            RefreshTableList();
            RebuildGrid();
        }

        public void OnSelected()
        {
            RefreshTableList();
            RebuildGrid();
        }

        private void RefreshTableList()
        {
            _tableNames.Clear();
            if (_config != null)
            {
                foreach (var table in _config.Tables)
                    _tableNames.Add(string.IsNullOrEmpty(table.Name) ? "(unnamed)" : table.Name);
            }

            _tableList?.RefreshItems();

            if (_tableNames.Count > 0)
            {
                _selectedTableIndex = Mathf.Clamp(_selectedTableIndex, 0, _tableNames.Count - 1);
                _tableList.SetSelectionWithoutNotify(new[] { _selectedTableIndex });
            }
            else
            {
                _selectedTableIndex = -1;
            }
        }

        private void OnTableSelected(int index)
        {
            _selectedTableIndex = index;
            RebuildGrid();
        }

        private void RebuildGrid()
        {
            if (_grid != null && _grid.parent != null)
                _grid.parent.Remove(_grid);
            _grid = null;

            if (_config == null || _selectedTableIndex < 0 || _selectedTableIndex >= _config.Tables.Count)
            {
                ShowEmptyState();
                return;
            }

            RemoveEmptyState();
            BuildVisibleIndices();

            var columns = new Columns();

            columns.Add(new Column
            {
                title = "Key",
                width = 160,
                makeCell = MakeCell,
                bindCell = (e, row) => BindKeyCell((Label)e, row),
            });

            var locales = _config.Locales;
            for (int li = 0; li < locales.Count; li++)
            {
                string localeCode = locales[li].Code;
                columns.Add(new Column
                {
                    title = string.IsNullOrEmpty(localeCode) ? "(?)" : localeCode,
                    width = 180,
                    makeCell = MakeCell,
                    bindCell = (e, row) => BindValueCell((Label)e, row, localeCode),
                });
            }

            _grid = new MultiColumnListView(columns)
            {
                itemsSource = _visibleEntryIndices,
                selectionType = SelectionType.Single,
                showBoundCollectionSize = false,
            };
            _grid.style.flexGrow = 1;
            _rightPane.Add(_grid);
        }

        private static VisualElement MakeCell()
        {
            var label = new Label();
            label.style.paddingLeft = 4;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.overflow = Overflow.Hidden;
            return label;
        }

        private void BuildVisibleIndices()
        {
            _visibleEntryIndices.Clear();
            var table = _config.Tables[_selectedTableIndex];

            for (int i = 0; i < table.Entries.Count; i++)
            {
                if (string.IsNullOrEmpty(_searchFilter)) { _visibleEntryIndices.Add(i); continue; }

                var entry = table.Entries[i];
                if (!string.IsNullOrEmpty(entry.Key)
                    && entry.Key.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    _visibleEntryIndices.Add(i);
            }
        }

        private TableEntry GetEntry(int row)
        {
            if (row < 0 || row >= _visibleEntryIndices.Count) return null;
            var entries = _config.Tables[_selectedTableIndex].Entries;
            int entryIndex = _visibleEntryIndices[row];
            return entryIndex < entries.Count ? entries[entryIndex] : null;
        }

        private void BindKeyCell(Label label, int row)
        {
            var entry = GetEntry(row);
            label.text = entry != null ? entry.Key : string.Empty;
        }

        private void BindValueCell(Label label, int row, string localeCode)
        {
            var entry = GetEntry(row);
            if (entry == null) { label.text = string.Empty; return; }

            string value = string.Empty;
            foreach (var lv in entry.Values)
            {
                if (lv.LocaleCode == localeCode) { value = lv.Value ?? string.Empty; break; }
            }

            label.text = value;
            label.style.opacity = string.IsNullOrEmpty(value) ? 0.4f : 1f;
        }

        private void ShowEmptyState()
        {
            if (_emptyState != null) return;

            _emptyState = new VisualElement();
            _emptyState.style.flexGrow = 1;
            _emptyState.style.alignItems = Align.Center;
            _emptyState.style.justifyContent = Justify.Center;

            var msg = new Label(_config == null
                ? "Select a LocalizationConfig asset above."
                : "No tables yet. Sync from your spreadsheet to fill this config.");
            msg.style.unityTextAlign = TextAnchor.MiddleCenter;
            _emptyState.Add(msg);
            _rightPane.Add(_emptyState);
        }

        private void RemoveEmptyState()
        {
            if (_emptyState != null && _emptyState.parent != null)
                _emptyState.parent.Remove(_emptyState);
            _emptyState = null;
        }
    }
}
#endif
