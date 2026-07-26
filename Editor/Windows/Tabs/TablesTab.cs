#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    internal sealed class TablesTab : ITab
    {
        public string Title => "Tables";
        public VisualElement Root { get; }

        private LocalizationConfig _config;
        private SerializedObject _so;

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

            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingTop = 2 } };
            var addBtn = new Button(AddTable) { text = "+", tooltip = "Add table" };
            var removeBtn = new Button(RemoveSelectedTable) { text = "-", tooltip = "Remove selected table" };
            addBtn.style.flexGrow = 1;
            removeBtn.style.flexGrow = 1;
            buttons.Add(addBtn);
            buttons.Add(removeBtn);
            left.Add(buttons);

            Root.Add(left);
        }

        private void BuildRightPane()
        {
            _rightPane = new VisualElement { name = "tables-right" };
            _rightPane.style.flexGrow = 1;

            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingTop = 4, paddingBottom = 4, paddingLeft = 4 } };
            var addKey = new Button(AddKey) { text = "Add Key" };
            var removeKey = new Button(RemoveSelectedKey) { text = "Remove Key" };
            _search = new ToolbarSearchField();
            _search.style.flexGrow = 1;
            _search.style.marginLeft = 6;
            _search.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue ?? string.Empty;
                RebuildGrid();
            });
            toolbar.Add(addKey);
            toolbar.Add(removeKey);
            toolbar.Add(_search);
            _rightPane.Add(toolbar);

            Root.Add(_rightPane);
        }

        public void OnConfigChanged(LocalizationConfig config)
        {
            _config = config;
            _so = config != null ? new SerializedObject(config) : null;
            RefreshTableList();
            RebuildGrid();
        }

        public void OnSelected()
        {
            if (_config != null)
            {
                _so?.Update();
                RefreshTableList();
                RebuildGrid();
            }
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

        private void AddTable()
        {
            if (_config == null) return;

            Undo.RecordObject(_config, "Add Localization Table");
            _so.Update();
            var tablesProp = _so.FindProperty("_tables");
            int newIndex = tablesProp.arraySize;
            tablesProp.InsertArrayElementAtIndex(newIndex);
            var newTable = tablesProp.GetArrayElementAtIndex(newIndex);
            newTable.FindPropertyRelative("_name").stringValue = $"Table{newIndex}";
            newTable.FindPropertyRelative("_entries").ClearArray();
            _so.ApplyModifiedProperties();

            _config.InvalidateIndex();
            _selectedTableIndex = newIndex;
            RefreshTableList();
            RebuildGrid();
        }

        private void RemoveSelectedTable()
        {
            if (_config == null || _selectedTableIndex < 0) return;

            Undo.RecordObject(_config, "Remove Localization Table");
            _so.Update();
            var tablesProp = _so.FindProperty("_tables");
            if (_selectedTableIndex < tablesProp.arraySize)
            {
                tablesProp.DeleteArrayElementAtIndex(_selectedTableIndex);
                _so.ApplyModifiedProperties();
                _config.InvalidateIndex();
            }

            _selectedTableIndex = Mathf.Max(0, _selectedTableIndex - 1);
            RefreshTableList();
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
                makeCell = MakeTextField,
                bindCell = (e, row) => BindKeyCell((TextField)e, row),
            });

            var locales = _config.Locales;
            for (int li = 0; li < locales.Count; li++)
            {
                string localeCode = locales[li].Code;
                columns.Add(new Column
                {
                    title = string.IsNullOrEmpty(localeCode) ? "(?)" : localeCode,
                    width = 180,
                    makeCell = MakeTextField,
                    bindCell = (e, row) => BindValueCell((TextField)e, row, localeCode),
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

        private static VisualElement MakeTextField()
        {
            var tf = new TextField { isDelayed = true };
            tf.style.marginTop = 1;
            tf.style.marginBottom = 1;
            tf.style.marginLeft = 2;
            tf.style.marginRight = 2;
            return tf;
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
                    && entry.Key.IndexOf(_searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    _visibleEntryIndices.Add(i);
            }
        }

        private SerializedProperty GetEntryProp(int entryIndex)
        {
            var tablesProp = _so.FindProperty("_tables");
            var tableProp = tablesProp.GetArrayElementAtIndex(_selectedTableIndex);
            var entriesProp = tableProp.FindPropertyRelative("_entries");
            return entriesProp.GetArrayElementAtIndex(entryIndex);
        }

        private void BindKeyCell(TextField field, int row)
        {
            if (row < 0 || row >= _visibleEntryIndices.Count) return;
            int entryIndex = _visibleEntryIndices[row];

            _so.Update();
            var keyProp = GetEntryProp(entryIndex).FindPropertyRelative("_key");
            field.SetValueWithoutNotify(keyProp.stringValue);

            field.UnregisterCallback<ChangeEvent<string>>(OnKeyChanged);
            field.userData = entryIndex;
            field.RegisterCallback<ChangeEvent<string>>(OnKeyChanged);
        }

        private void OnKeyChanged(ChangeEvent<string> evt)
        {
            var field = (TextField)evt.target;
            int entryIndex = (int)field.userData;

            Undo.RecordObject(_config, "Edit Localization Key");
            _so.Update();
            GetEntryProp(entryIndex).FindPropertyRelative("_key").stringValue = evt.newValue;
            _so.ApplyModifiedProperties();
            _config.InvalidateIndex();
        }

        private void BindValueCell(TextField field, int row, string localeCode)
        {
            if (row < 0 || row >= _visibleEntryIndices.Count) return;
            int entryIndex = _visibleEntryIndices[row];

            _so.Update();
            var valuesProp = GetEntryProp(entryIndex).FindPropertyRelative("_values");

            int slot = FindLocaleSlot(valuesProp, localeCode);
            string current = slot >= 0
                ? valuesProp.GetArrayElementAtIndex(slot).FindPropertyRelative("Value").stringValue
                : string.Empty;

            field.SetValueWithoutNotify(current);

            field.UnregisterCallback<ChangeEvent<string>>(OnValueChanged);
            field.userData = new ValueCellContext { EntryIndex = entryIndex, LocaleCode = localeCode };
            field.RegisterCallback<ChangeEvent<string>>(OnValueChanged);
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            var field = (TextField)evt.target;
            var ctx = (ValueCellContext)field.userData;

            Undo.RecordObject(_config, "Edit Localization Value");
            _so.Update();
            var valuesProp = GetEntryProp(ctx.EntryIndex).FindPropertyRelative("_values");

            int slot = FindLocaleSlot(valuesProp, ctx.LocaleCode);
            if (slot < 0)
            {
                slot = valuesProp.arraySize;
                valuesProp.InsertArrayElementAtIndex(slot);
                var newElem = valuesProp.GetArrayElementAtIndex(slot);
                newElem.FindPropertyRelative("LocaleCode").stringValue = ctx.LocaleCode;
            }

            valuesProp.GetArrayElementAtIndex(slot).FindPropertyRelative("Value").stringValue = evt.newValue;
            _so.ApplyModifiedProperties();
            _config.InvalidateIndex();
        }

        private static int FindLocaleSlot(SerializedProperty valuesProp, string localeCode)
        {
            for (int i = 0; i < valuesProp.arraySize; i++)
            {
                var elem = valuesProp.GetArrayElementAtIndex(i);
                if (elem.FindPropertyRelative("LocaleCode").stringValue == localeCode)
                    return i;
            }
            return -1;
        }

        private void AddKey()
        {
            if (_config == null || _selectedTableIndex < 0) return;

            Undo.RecordObject(_config, "Add Localization Key");
            _so.Update();
            var tablesProp = _so.FindProperty("_tables");
            var tableProp = tablesProp.GetArrayElementAtIndex(_selectedTableIndex);
            var entriesProp = tableProp.FindPropertyRelative("_entries");

            int newIndex = entriesProp.arraySize;
            entriesProp.InsertArrayElementAtIndex(newIndex);
            var entry = entriesProp.GetArrayElementAtIndex(newIndex);
            entry.FindPropertyRelative("_key").stringValue = $"new_key_{newIndex}";

            var valuesProp = entry.FindPropertyRelative("_values");
            valuesProp.ClearArray();
            for (int li = 0; li < _config.Locales.Count; li++)
            {
                valuesProp.InsertArrayElementAtIndex(li);
                var v = valuesProp.GetArrayElementAtIndex(li);
                v.FindPropertyRelative("LocaleCode").stringValue = _config.Locales[li].Code;
                v.FindPropertyRelative("Value").stringValue = string.Empty;
            }

            _so.ApplyModifiedProperties();
            _config.InvalidateIndex();

            _searchFilter = string.Empty;
            _search?.SetValueWithoutNotify(string.Empty);
            RebuildGrid();
        }

        private void RemoveSelectedKey()
        {
            if (_config == null || _selectedTableIndex < 0 || _grid == null) return;
            int selectedRow = _grid.selectedIndex;
            if (selectedRow < 0 || selectedRow >= _visibleEntryIndices.Count) return;

            int entryIndex = _visibleEntryIndices[selectedRow];

            Undo.RecordObject(_config, "Remove Localization Key");
            _so.Update();
            var tablesProp = _so.FindProperty("_tables");
            var tableProp = tablesProp.GetArrayElementAtIndex(_selectedTableIndex);
            tableProp.FindPropertyRelative("_entries").DeleteArrayElementAtIndex(entryIndex);
            _so.ApplyModifiedProperties();
            _config.InvalidateIndex();

            RebuildGrid();
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
                : "No table selected. Add a table to begin.");
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

        private struct ValueCellContext
        {
            public int EntryIndex;
            public string LocaleCode;
        }
    }
}
#endif
