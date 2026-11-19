#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    /// <summary>Read-only browser for the imported tables, keys and translations.</summary>
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
        private const float CellPaddingX = 6f;
        private const float CellPaddingY = 3f;
        private const float MissingValueOpacity = 0.35f;
        private const float AutoFitPadding = 8f;
        private const float AutoFitMaxWidth = 600f;
        private const string ResizeHandleClass = "unity-multi-column-header__column-resize-handle";

        public string Title => "Tables";
        public VisualElement Root { get; }

        private LocalizationConfig _config;

        private ListView _tableList;
        private IReadOnlyList<LocalizationTable> _tables = new List<LocalizationTable>();
        private readonly List<string> _tableNames = new();
        private int _selectedTableIndex = -1;

        private VisualElement _gridHost;
        private Label _statusLabel;
        private MultiColumnListView _grid;
        private string _gridLocales;
        private Label _measureLabel;
        private Label _measureKeyLabel;
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

        public void OnConfigChanged(LocalizationConfig config)
        {
            _config = config;
            RefreshTableList();
            ShowSelectedTable();
        }

        public void OnSelected()
        {
            RefreshTableList();
            ShowSelectedTable();
        }

        /// <summary>Selects a table if it is one of the config's tables.</summary>
        internal void Select(LocalizationTable table)
        {
            RefreshTableList();

            for (int i = 0; i < _tables.Count; i++)
            {
                if (_tables[i] != table) continue;

                _tableList.SetSelection(i);
                return;
            }
        }

        private void BuildLeftPane()
        {
            var left = new VisualElement { name = "tables-left" };
            left.style.width = 180;
            left.style.borderRightWidth = 1;
            left.style.borderRightColor = LocalizationStyles.ColumnDivider;

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
            var right = new VisualElement { name = "tables-right" };
            right.style.flexGrow = 1;

            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingTop = 4, paddingBottom = 4, paddingLeft = 4 } };

            var search = new ToolbarSearchField { tooltip = SearchTooltip };
            search.style.flexGrow = 1;
            search.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue ?? string.Empty;
                ShowSelectedTable();
            });
            toolbar.Add(search);
            right.Add(toolbar);

            // The grid lives in its own host so the status bar below is never rebuilt or scrolled
            _gridHost = new VisualElement { style = { flexGrow = 1 } };
            right.Add(_gridHost);

            // Hidden labels styled like the cells, so fit-to-content measures with the same font
            _measureLabel = MakeMeasureLabel(FontStyle.Normal);
            _measureKeyLabel = MakeMeasureLabel(FontStyle.Bold);
            right.Add(_measureLabel);
            right.Add(_measureKeyLabel);

            _statusLabel = new Label();
            _statusLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            _statusLabel.style.paddingRight = 6;
            _statusLabel.style.paddingTop = 3;
            _statusLabel.style.paddingBottom = 3;
            _statusLabel.style.opacity = 0.6f;
            _statusLabel.style.flexShrink = 0;
            _statusLabel.style.borderTopWidth = 1;
            _statusLabel.style.borderTopColor = LocalizationStyles.ColumnDivider;
            right.Add(_statusLabel);

            Root.Add(right);
        }

        private void RefreshTableList()
        {
            _tableNames.Clear();
            _tables = TableAssets.For(_config);
            foreach (var table in _tables)
                _tableNames.Add(string.IsNullOrEmpty(table.TableName) ? "(unnamed)" : table.TableName);

            _tableList.RefreshItems();

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
            ShowSelectedTable();
            if (_grid != null && _visibleEntryIndices.Count > 0) _grid.ScrollToItem(0);
        }

        // Rows refresh in place, so column widths survive searching and switching tables
        private void ShowSelectedTable()
        {
            if (SelectedTable == null)
            {
                RemoveGrid();
                ShowEmptyState();
                UpdateStatus();
                return;
            }

            RemoveEmptyState();
            BuildVisibleIndices();
            EnsureGrid();
            _grid.RefreshItems();
            UpdateStatus();
        }

        // Columns follow the config's locales, so the grid is only rebuilt when those change
        private void EnsureGrid()
        {
            var locales = _config.Locales;
            var codes = new string[locales.Count];
            for (int i = 0; i < codes.Length; i++) codes[i] = locales[i].Code;

            string signature = string.Join("\n", codes);
            if (_grid != null && signature == _gridLocales) return;

            RemoveGrid();
            _gridLocales = signature;

            // Grow (not GrowAndFill) so columns keep their width past the viewport and scroll horizontally
            var columns = new Columns
            {
                stretchMode = codes.Length > HorizontalScrollLocaleCount ? Columns.StretchMode.Grow : Columns.StretchMode.GrowAndFill,
                resizable = true,
            };

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

            foreach (string code in codes)
            {
                string localeCode = code;
                columns.Add(new Column
                {
                    name = localeCode,
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

            // The header and its resize handles exist only once the grid is laid out
            _grid.RegisterCallback<GeometryChangedEvent>(RegisterAutoFit);
            _gridHost.Add(_grid);
        }

        private void RemoveGrid()
        {
            _grid?.RemoveFromHierarchy();
            _grid = null;
            _gridLocales = null;
        }

        // Unity has no built-in fit-to-content, so a double-click on a header divider resizes to the widest text
        private void RegisterAutoFit(GeometryChangedEvent evt)
        {
            var grid = (MultiColumnListView)evt.target;
            grid.UnregisterCallback<GeometryChangedEvent>(RegisterAutoFit);

            var handles = grid.Query(className: ResizeHandleClass).ToList();
            var columns = grid.columns;

            for (int i = 0; i < handles.Count && i < columns.Count; i++)
            {
                var column = columns[i];
                handles[i].RegisterCallback<MouseDownEvent>(down =>
                {
                    if (down.clickCount < 2) return;

                    column.width = MeasureColumnWidth(column);
                    down.StopPropagation();
                });
            }
        }

        private float MeasureColumnWidth(Column column)
        {
            bool isKeyColumn = string.IsNullOrEmpty(column.name);
            var probe = isKeyColumn ? _measureKeyLabel : _measureLabel;
            var table = SelectedTable;

            float widest = MeasureText(probe, column.title);
            foreach (int row in _visibleEntryIndices)
            {
                string text = isKeyColumn ? table.Keys[row] : ValueFor(table, row, column.name);
                if (!string.IsNullOrEmpty(text)) widest = Mathf.Max(widest, MeasureText(probe, text));
            }

            float minWidth = isKeyColumn ? KeyColumnMinWidth : LocaleColumnMinWidth;
            return Mathf.Clamp(widest + CellPaddingX * 2f + AutoFitPadding, minWidth, AutoFitMaxWidth);
        }

        private static float MeasureText(TextElement probe, string text)
            => probe.MeasureTextSize(text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;

        private static string ValueFor(LocalizationTable table, int row, string localeCode)
        {
            int column = table.IndexOfLocale(localeCode);
            return column >= 0 ? table.GetValue(row, column) : null;
        }

        private LocalizationTable SelectedTable =>
            _config != null && _selectedTableIndex >= 0 && _selectedTableIndex < _tables.Count
                ? _tables[_selectedTableIndex]
                : null;

        // Updates the pinned status bar; it is created once, so it never duplicates or scrolls away
        private void UpdateStatus()
        {
            var table = SelectedTable;
            if (table == null)
            {
                _statusLabel.text = string.Empty;
                return;
            }

            int shown = _visibleEntryIndices.Count;
            int total = table.Keys.Count;

            _statusLabel.text = shown == total ? $"{total} keys" : $"{shown} of {total} keys";
        }

        // Tags and escapes show as typed, so a translator can check them against the sheet
        private static Label MakeCellLabel()
        {
            var label = new Label { name = CellLabelName, enableRichText = false, parseEscapeSequences = false };
            label.style.paddingLeft = CellPaddingX;
            label.style.paddingRight = CellPaddingX;
            label.style.paddingTop = CellPaddingY;
            label.style.paddingBottom = CellPaddingY;
            label.style.unityTextAlign = TextAnchor.UpperLeft;
            return label;
        }

        private static Label MakeMeasureLabel(FontStyle fontStyle)
        {
            var label = MakeCellLabel();
            label.style.unityFontStyleAndWeight = fontStyle;
            label.style.position = Position.Absolute;
            label.style.visibility = Visibility.Hidden;
            return label;
        }

        // The divider lives on a full-height wrapper; a label sizes to its own text,
        // so a border on it would stop short on rows where a neighbour wrapped.
        private static VisualElement MakeCell()
        {
            var cell = new VisualElement();
            cell.style.flexGrow = 1;
            cell.style.borderRightWidth = 1;
            cell.style.borderRightColor = LocalizationStyles.ColumnDivider;

            var label = MakeCellLabel();

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

            // Identifiers are single tokens — wrapping one mid-word hurts more than clipping it
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.overflow = Overflow.Hidden;
            return cell;
        }

        private static Label CellLabel(VisualElement cell) => (Label)cell.Q(CellLabelName);

        private void BuildVisibleIndices()
        {
            _visibleEntryIndices.Clear();
            var keys = SelectedTable.Keys;

            for (int i = 0; i < keys.Count; i++)
            {
                if (string.IsNullOrEmpty(_searchFilter)) { _visibleEntryIndices.Add(i); continue; }

                if (!string.IsNullOrEmpty(keys[i])
                    && keys[i].IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    _visibleEntryIndices.Add(i);
            }
        }

        // Maps a visible grid row to the table's key row, or -1
        private int TableRow(int gridRow)
        {
            var table = SelectedTable;
            if (table == null || gridRow < 0 || gridRow >= _visibleEntryIndices.Count) return -1;
            int row = _visibleEntryIndices[gridRow];
            return row < table.Keys.Count ? row : -1;
        }

        private void BindKeyCell(VisualElement cell, int gridRow)
        {
            var label = CellLabel(cell);
            int row = TableRow(gridRow);
            label.text = row >= 0 ? SelectedTable.Keys[row] : string.Empty;
        }

        private void BindValueCell(VisualElement cell, int gridRow, string localeCode)
        {
            var label = CellLabel(cell);
            int row = TableRow(gridRow);
            if (row < 0) { label.text = string.Empty; return; }

            string value = ValueFor(SelectedTable, row, localeCode) ?? string.Empty;

            // An em dash makes a gap visible; blank space reads as a narrow column
            bool missing = string.IsNullOrEmpty(value);
            label.text = missing ? MissingValueMark : value;
            label.style.opacity = missing ? MissingValueOpacity : 1f;
        }

        // Recreated each time, so the message follows the current config
        private void ShowEmptyState()
        {
            RemoveEmptyState();
            _emptyState = new HelpBox(_config == null
                ? "Select a LocalizationConfig asset above."
                : "No tables yet. Import your spreadsheet to fill this config.", HelpBoxMessageType.Info);
            _emptyState.style.marginTop = 8;
            _gridHost.Add(_emptyState);
        }

        private void RemoveEmptyState()
        {
            _emptyState?.RemoveFromHierarchy();
            _emptyState = null;
        }
    }
}
#endif
