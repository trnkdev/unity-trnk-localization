#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    /// <summary>Read-only browser for the synced tables, keys and translations.</summary>
    internal sealed class TablesTab : ITab
    {
        private const string MissingValueMark = "—";
        private const string SearchTooltip = "Filter rows by key name.";
        private const float KeyColumnWidth = 200f;
        // Floors chosen so wrapped text still fits a few words per line
        private const float KeyColumnMinWidth = 140f;
        private const float LocaleColumnMinWidth = 160f;
        private const string KeyColumnTitle = "Key";
        private const string CellLabelName = "cell-label";
        private const int HorizontalScrollLocaleCount = 4;
        private const float AutoFitPadding = 20f;
        private const float AutoFitMaxWidth = 600f;

        public string Title => "Tables";
        public VisualElement Root { get; }

        private LocalizationConfig _config;

        private ListView _tableList;
        private readonly List<string> _tableNames = new();
        private int _selectedTableIndex = -1;

        private VisualElement _rightPane;
        private VisualElement _gridHost;
        private Label _statusLabel;
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

            _search = new ToolbarSearchField { tooltip = SearchTooltip };
            _search.style.flexGrow = 1;
            _search.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue ?? string.Empty;
                RebuildGrid();
            });
            toolbar.Add(_search);
            _rightPane.Add(toolbar);

            // The grid lives in its own host so the status bar below is never rebuilt or scrolled
            _gridHost = new VisualElement { style = { flexGrow = 1 } };
            _rightPane.Add(_gridHost);

            _statusLabel = new Label();
            _statusLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            _statusLabel.style.paddingRight = 6;
            _statusLabel.style.paddingTop = 3;
            _statusLabel.style.paddingBottom = 3;
            _statusLabel.style.opacity = 0.6f;
            _statusLabel.style.flexShrink = 0;
            _statusLabel.style.borderTopWidth = 1;
            _statusLabel.style.borderTopColor = LocalizationStyles.ColumnDivider;
            _rightPane.Add(_statusLabel);

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
                UpdateStatus();
                return;
            }

            RemoveEmptyState();
            BuildVisibleIndices();

            // Grow (not GrowAndFill) so columns keep their width past the viewport and scroll horizontally
            var stretchMode = _config.Locales.Count > HorizontalScrollLocaleCount
                ? Columns.StretchMode.Grow
                : Columns.StretchMode.GrowAndFill;

            var columns = new Columns { stretchMode = stretchMode, resizable = true };

            columns.Add(new Column
            {
                title = KeyColumnTitle,
                width = KeyColumnWidth,
                minWidth = KeyColumnMinWidth,
                stretchable = false,
                resizable = true,
                makeCell = MakeKeyCell,
                bindCell = BindKeyCell,
            });

            var locales = _config.Locales;
            for (int li = 0; li < locales.Count; li++)
            {
                string localeCode = locales[li].Code;
                columns.Add(new Column
                {
                    title = string.IsNullOrEmpty(localeCode) ? "(?)" : localeCode,
                    minWidth = LocaleColumnMinWidth,
                    stretchable = true,
                    resizable = true,
                    makeCell = MakeCell,
                    bindCell = (e, row) => BindValueCell(e, row, localeCode),
                });
            }

            _grid = new MultiColumnListView(columns)
            {
                itemsSource = _visibleEntryIndices,
                selectionType = SelectionType.None,
                showBoundCollectionSize = false,
                // Rows grow to fit wrapped text as columns are narrowed
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                // Striping the row (not the cell) keeps bands whole when rows differ in height
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
            };
            _grid.style.flexGrow = 1;
            _gridHost.Add(_grid);

            RegisterAutoFit(columns);
            UpdateStatus();
        }

        // Unity has no built-in fit-to-content, so a double-click on a header divider
        // measures the column's widest text and resizes to it, like a spreadsheet.
        private void RegisterAutoFit(Columns columns)
        {
            var header = _grid.Q("unity-multi-column-header");
            if (header == null) return;

            var handles = header.Query(className: "unity-multi-column-header__column-resize-handle").ToList();

            for (int i = 0; i < handles.Count && i < columns.Count; i++)
            {
                var column = columns[i];
                handles[i].RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.clickCount < 2) return;

                    column.width = MeasureColumnWidth(column);
                    evt.StopPropagation();
                });
            }
        }

        private float MeasureColumnWidth(Column column)
        {
            bool isKeyColumn = column.title == KeyColumnTitle;
            var style = isKeyColumn ? EditorStyles.boldLabel : EditorStyles.label;

            float widest = style.CalcSize(new GUIContent(column.title.ToString())).x;
            var entries = _config.Tables[_selectedTableIndex].Entries;

            foreach (int entryIndex in _visibleEntryIndices)
            {
                if (entryIndex >= entries.Count) continue;
                var entry = entries[entryIndex];

                string text = isKeyColumn ? entry.Key : ValueFor(entry, column.title.ToString());
                if (string.IsNullOrEmpty(text)) continue;

                float width = style.CalcSize(new GUIContent(text)).x;
                if (width > widest) widest = width;
            }

            return Mathf.Clamp(widest + AutoFitPadding, LocaleColumnMinWidth, AutoFitMaxWidth);
        }

        private static string ValueFor(TableEntry entry, string localeCode)
        {
            foreach (var lv in entry.Values)
                if (lv.LocaleCode == localeCode) return lv.Value;
            return null;
        }

        // Updates the pinned status bar; it is created once, so it never duplicates or scrolls away
        private void UpdateStatus()
        {
            if (_statusLabel == null) return;

            if (_config == null || _selectedTableIndex < 0 || _selectedTableIndex >= _config.Tables.Count)
            {
                _statusLabel.text = string.Empty;
                return;
            }

            int shown = _visibleEntryIndices.Count;
            int total = _config.Tables[_selectedTableIndex].Entries.Count;

            _statusLabel.text = shown == total ? $"{total} keys" : $"{shown} of {total} keys";
        }

        // The divider lives on a full-height wrapper; a label sizes to its own text,
        // so a border on it would stop short on rows where a neighbour wrapped.
        private static VisualElement MakeCell()
        {
            var cell = new VisualElement();
            cell.style.flexGrow = 1;
            cell.style.borderRightWidth = 1;
            cell.style.borderRightColor = LocalizationStyles.ColumnDivider;

            var label = new Label { name = CellLabelName };
            label.style.paddingLeft = 6;
            label.style.paddingRight = 6;
            label.style.paddingTop = 3;
            label.style.paddingBottom = 3;
            label.style.unityTextAlign = TextAnchor.UpperLeft;

            // Long values wrap instead of being clipped when a column is narrowed
            label.style.whiteSpace = WhiteSpace.Normal;

            cell.Add(label);
            return cell;
        }

        // The key is the row's identifier, so it reads as a header rather than another value
        private static VisualElement MakeKeyCell()
        {
            var cell = MakeCell();
            var label = CellLabel(cell);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = LocalizationStyles.KeyText;

            // Identifiers are single tokens — wrapping one mid-word hurts more than clipping it
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.overflow = Overflow.Hidden;
            return cell;
        }

        private static Label CellLabel(VisualElement cell) => (Label)cell.Q(CellLabelName);

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

        private void BindKeyCell(VisualElement cell, int row)
        {
            var label = CellLabel(cell);
            var entry = GetEntry(row);
            label.text = entry != null ? entry.Key : string.Empty;
        }

        private void BindValueCell(VisualElement cell, int row, string localeCode)
        {
            var label = CellLabel(cell);
            var entry = GetEntry(row);
            if (entry == null) { label.text = string.Empty; return; }

            string value = string.Empty;
            foreach (var lv in entry.Values)
            {
                if (lv.LocaleCode == localeCode) { value = lv.Value ?? string.Empty; break; }
            }

            // An em dash makes a gap visible; blank space reads as a narrow column
            bool missing = string.IsNullOrEmpty(value);
            label.text = missing ? MissingValueMark : value;
            label.style.opacity = missing ? 0.35f : 1f;
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
            _gridHost.Add(_emptyState);
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
