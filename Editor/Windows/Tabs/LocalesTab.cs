using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    internal sealed class LocalesTab : ITab
    {
        public string Title => "Locales";
        public VisualElement Root { get; }

        private LocalizationSettings _settings;
        private SerializedObject _so;

        private ListView _list;
        private readonly List<int> _indices = new();
        private DropdownField _defaultDropdown;
        private VisualElement _body;

        internal LocalesTab()
        {
            Root = new VisualElement { name = "tab-locales-root" };
            Root.style.flexGrow = 1;
            Root.style.paddingTop = 8;
            Root.style.paddingLeft = 8;
            Root.style.paddingRight = 8;

            _body = new VisualElement { style = { flexGrow = 1 } };
            Root.Add(_body);
        }

        public void OnSettingsChanged(LocalizationSettings settings)
        {
            _settings = settings;
            _so = settings != null ? new SerializedObject(settings) : null;
            Rebuild();
        }

        public void OnSelected()
        {
            _so?.Update();
            Rebuild();
        }

        private void Rebuild()
        {
            _body.Clear();

            if (_settings == null)
            {
                var msg = new Label("Select a LocalizationSettings asset above.");
                msg.style.marginTop = 16;
                _body.Add(msg);
                return;
            }

            var defaultRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
            defaultRow.Add(new Label("Default Locale:") { style = { width = 110, unityFontStyleAndWeight = FontStyle.Bold } });

            _defaultDropdown = new DropdownField();
            _defaultDropdown.style.flexGrow = 1;
            RefreshDefaultChoices();
            _defaultDropdown.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(_settings, "Change Default Locale");
                _so.Update();
                _so.FindProperty("_defaultLocale").stringValue = evt.newValue;
                _so.ApplyModifiedProperties();
            });
            defaultRow.Add(_defaultDropdown);
            _body.Add(defaultRow);

            var header = new Label("Registered Locales");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 4;
            _body.Add(header);

            RefreshIndices();
            _list = new ListView
            {
                itemsSource = _indices,
                fixedItemHeight = 24,
                selectionType = SelectionType.Single,
                makeItem = MakeLocaleRow,
                bindItem = BindLocaleRow,
            };
            _list.style.flexGrow = 1;
            _list.style.borderTopWidth = 1;
            _list.style.borderBottomWidth = 1;
            _list.style.borderTopColor = new Color(0, 0, 0, 0.2f);
            _list.style.borderBottomColor = new Color(0, 0, 0, 0.2f);
            _body.Add(_list);

            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 6 } };
            var addBtn = new Button(AddLocale) { text = "Add Locale" };
            var removeBtn = new Button(RemoveSelectedLocale) { text = "Remove Selected" };
            var syncBtn = new Button(SyncEntries)
            {
                text = "Sync Entries With Locales",
                tooltip = "Add empty value slots to every entry for any newly added locale."
            };
            buttons.Add(addBtn);
            buttons.Add(removeBtn);
            buttons.Add(syncBtn);
            _body.Add(buttons);
        }

        private void RefreshIndices()
        {
            _indices.Clear();
            for (int i = 0; i < _settings.Locales.Count; i++) _indices.Add(i);
        }

        private void RefreshDefaultChoices()
        {
            var choices = new List<string>();
            foreach (var l in _settings.Locales)
                if (!string.IsNullOrEmpty(l.Code)) choices.Add(l.Code);

            _defaultDropdown.choices = choices;
            _defaultDropdown.SetValueWithoutNotify(_settings.DefaultLocale);
        }

        private VisualElement MakeLocaleRow()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var codeField = new TextField { isDelayed = true, name = "code" };
            codeField.style.width = 100;
            codeField.style.marginRight = 6;

            var nameField = new TextField { isDelayed = true, name = "name" };
            nameField.style.flexGrow = 1;

            row.Add(new Label("Code") { style = { width = 34 } });
            row.Add(codeField);
            row.Add(new Label("Name") { style = { width = 40 } });
            row.Add(nameField);
            return row;
        }

        private void BindLocaleRow(VisualElement element, int row)
        {
            int localeIndex = _indices[row];
            _so.Update();
            var localesProp = _so.FindProperty("_locales");
            if (localeIndex >= localesProp.arraySize) return;

            var elem = localesProp.GetArrayElementAtIndex(localeIndex);
            var codeProp = elem.FindPropertyRelative("Code");
            var nameProp = elem.FindPropertyRelative("Name");

            var codeField = element.Q<TextField>("code");
            var nameField = element.Q<TextField>("name");

            codeField.SetValueWithoutNotify(codeProp.stringValue);
            nameField.SetValueWithoutNotify(nameProp.stringValue);

            codeField.userData = localeIndex;
            nameField.userData = localeIndex;

            codeField.UnregisterCallback<ChangeEvent<string>>(OnCodeChanged);
            nameField.UnregisterCallback<ChangeEvent<string>>(OnNameChanged);
            codeField.RegisterCallback<ChangeEvent<string>>(OnCodeChanged);
            nameField.RegisterCallback<ChangeEvent<string>>(OnNameChanged);
        }

        private void OnCodeChanged(ChangeEvent<string> evt)
        {
            var field = (TextField)evt.target;
            int index = (int)field.userData;

            Undo.RecordObject(_settings, "Edit Locale Code");
            _so.Update();
            _so.FindProperty("_locales").GetArrayElementAtIndex(index)
                .FindPropertyRelative("Code").stringValue = evt.newValue;
            _so.ApplyModifiedProperties();
            _settings.InvalidateIndex();
            RefreshDefaultChoices();
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            var field = (TextField)evt.target;
            int index = (int)field.userData;

            Undo.RecordObject(_settings, "Edit Locale Name");
            _so.Update();
            _so.FindProperty("_locales").GetArrayElementAtIndex(index)
                .FindPropertyRelative("Name").stringValue = evt.newValue;
            _so.ApplyModifiedProperties();
        }

        private void AddLocale()
        {
            Undo.RecordObject(_settings, "Add Locale");
            _so.Update();
            var localesProp = _so.FindProperty("_locales");
            int idx = localesProp.arraySize;
            localesProp.InsertArrayElementAtIndex(idx);
            var elem = localesProp.GetArrayElementAtIndex(idx);
            elem.FindPropertyRelative("Code").stringValue = string.Empty;
            elem.FindPropertyRelative("Name").stringValue = string.Empty;
            _so.ApplyModifiedProperties();
            _settings.InvalidateIndex();

            RefreshIndices();
            _list.RefreshItems();
            RefreshDefaultChoices();
        }

        private void RemoveSelectedLocale()
        {
            int row = _list.selectedIndex;
            if (row < 0 || row >= _indices.Count) return;
            int localeIndex = _indices[row];

            Undo.RecordObject(_settings, "Remove Locale");
            _so.Update();
            _so.FindProperty("_locales").DeleteArrayElementAtIndex(localeIndex);
            _so.ApplyModifiedProperties();
            _settings.InvalidateIndex();

            RefreshIndices();
            _list.RefreshItems();
            RefreshDefaultChoices();
        }

        private void SyncEntries()
        {
            if (_settings == null) return;
            _settings.SyncEntriesWithLocales();
            AssetDatabase.SaveAssetIfDirty(_settings);
            EditorUtility.DisplayDialog("Sync Complete",
                "All entries now have value slots for every registered locale.", "OK");
        }
    }
}
