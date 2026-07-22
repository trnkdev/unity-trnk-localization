using System.Collections.Generic;
using System.IO;
using TRnK.Logger;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    internal sealed class ImportExportTab : ITab
    {
        public string Title => "Import / Export";
        public VisualElement Root { get; }

        private LocalizationSettings _settings;
        private VisualElement _body;

        private CsvDiff.ParsedCsv _pendingCsv;
        private CsvDiff.DiffResult _pendingDiff;
        private ImportMode _mode = ImportMode.MergeCsvWins;

        private enum ImportMode { MergeCsvWins, ReplaceAll }

        internal ImportExportTab()
        {
            Root = new VisualElement { name = "tab-importExport-root" };
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
            _pendingCsv = null;
            _pendingDiff = null;
            Rebuild();
        }

        public void OnSelected() => Rebuild();

        private void Rebuild()
        {
            _body.Clear();

            if (_settings == null)
            {
                _body.Add(new Label("Select a LocalizationSettings asset above.") { style = { marginTop = 16 } });
                return;
            }

            BuildExportSection();
            _body.Add(Divider());
            BuildImportSection();

            if (_pendingDiff != null)
                BuildPreviewSection();
        }

        // ---------------- Export ----------------

        private void BuildExportSection()
        {
            var header = new Label("Export");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 13;
            header.style.marginBottom = 4;
            _body.Add(header);

            _body.Add(new Label("Exports all tables and locales to a single CSV file.")
            { style = { marginBottom = 6, whiteSpace = WhiteSpace.Normal } });

            var exportBtn = new Button(DoExport) { text = "Export to CSV…" };
            exportBtn.style.width = 160;
            _body.Add(exportBtn);
        }

        private void DoExport()
        {
            var path = EditorUtility.SaveFilePanel("Export Localization CSV", "",
                $"{_settings.name}.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;

            var rows = BuildExportRows();
            var csv = CsvWriter.Write(rows);

            try
            {
                File.WriteAllText(path, csv, new System.Text.UTF8Encoding(true)); // BOM for Excel
                Log.Info($"Exported localization to '{path}'.");
                EditorUtility.RevealInFinder(path);
            }
            catch (System.Exception e)
            {
                Log.Error($"Export failed: {e.Message}");
                EditorUtility.DisplayDialog("Export Failed", e.Message, "OK");
            }
        }

        private List<List<string>> BuildExportRows()
        {
            var rows = new List<List<string>>();

            var header = new List<string> { "Table", "Key" };
            var localeCodes = new List<string>();
            foreach (var l in _settings.Locales)
            {
                if (string.IsNullOrEmpty(l.Code)) continue;
                localeCodes.Add(l.Code);
                header.Add(l.Code);
            }
            rows.Add(header);

            foreach (var table in _settings.Tables)
            {
                foreach (var entry in table.Entries)
                {
                    var row = new List<string> { table.Name, entry.Key };
                    foreach (var code in localeCodes)
                    {
                        string value = string.Empty;
                        foreach (var lv in entry.Values)
                        {
                            if (lv.LocaleCode == code) { value = lv.Value ?? string.Empty; break; }
                        }
                        row.Add(value);
                    }
                    rows.Add(row);
                }
            }

            return rows;
        }

        // ---------------- Import ----------------

        private void BuildImportSection()
        {
            var header = new Label("Import");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 13;
            header.style.marginBottom = 4;
            _body.Add(header);

            var modeField = new EnumField("Import Mode", _mode);
            modeField.RegisterValueChangedCallback(evt => _mode = (ImportMode)evt.newValue);
            modeField.tooltip =
                "Merge — CSV Wins: add new keys, overwrite existing values from CSV, keep anything not in the CSV.\n" +
                "Replace All: CSV becomes the source of truth; entries not in the CSV are removed.";
            _body.Add(modeField);

            var selectBtn = new Button(SelectCsvAndPreview) { text = "Select CSV & Preview…" };
            selectBtn.style.width = 200;
            selectBtn.style.marginTop = 4;
            _body.Add(selectBtn);
        }

        private void SelectCsvAndPreview()
        {
            var path = EditorUtility.OpenFilePanel("Select Localization CSV", "", "csv");
            if (string.IsNullOrEmpty(path)) return;

            string text;
            try { text = File.ReadAllText(path); }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Read Failed", e.Message, "OK");
                return;
            }

            List<List<string>> grid;
            try { grid = CsvParser.Parse(text); }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Parse Failed", e.Message, "OK");
                return;
            }

            _pendingCsv = CsvDiff.Structure(grid);
            _pendingDiff = CsvDiff.Compute(_settings, _pendingCsv);
            Rebuild();
        }

        // ---------------- Preview ----------------

        private void BuildPreviewSection()
        {
            _body.Add(Divider());

            var header = new Label("Preview");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 13;
            header.style.marginBottom = 4;
            _body.Add(header);

            int removeCount = _mode == ImportMode.ReplaceAll ? _pendingDiff.Removed.Count : 0;
            var summary = new Label(
                $"+ {_pendingDiff.Added.Count} added   " +
                $"~ {_pendingDiff.Updated.Count} updated   " +
                $"- {removeCount} removed");
            summary.style.marginBottom = 4;
            _body.Add(summary);

            if (_pendingCsv.Warnings.Count > 0)
                _body.Add(new HelpBox(string.Join("\n", _pendingCsv.Warnings), HelpBoxMessageType.Warning));

            if (_pendingDiff.LocalesNotInSettings.Count > 0)
            {
                _body.Add(new HelpBox(
                    "CSV contains locales not in settings (will be skipped): " +
                    string.Join(", ", _pendingDiff.LocalesNotInSettings),
                    HelpBoxMessageType.Info));
            }

            if (_mode == ImportMode.MergeCsvWins && _pendingDiff.LocalesInSettingsNotInCsv.Count > 0)
            {
                _body.Add(new HelpBox(
                    "These settings locales aren't in the CSV and will be preserved: " +
                    string.Join(", ", _pendingDiff.LocalesInSettingsNotInCsv),
                    HelpBoxMessageType.Info));
            }

            var scroll = new ScrollView { style = { maxHeight = 200, marginTop = 4, marginBottom = 8 } };
            scroll.style.borderTopWidth = 1;
            scroll.style.borderBottomWidth = 1;
            scroll.style.borderTopColor = new Color(0, 0, 0, 0.2f);
            scroll.style.borderBottomColor = new Color(0, 0, 0, 0.2f);

            foreach (var c in _pendingDiff.Added)
                scroll.Add(ChangeRow($"+ {c.Table}.{c.Key} [{c.LocaleCode}]", c.NewValue, new Color(0.4f, 0.8f, 0.4f)));
            foreach (var c in _pendingDiff.Updated)
                scroll.Add(ChangeRow($"~ {c.Table}.{c.Key} [{c.LocaleCode}]", $"{c.OldValue} → {c.NewValue}", new Color(0.9f, 0.8f, 0.4f)));
            if (_mode == ImportMode.ReplaceAll)
                foreach (var c in _pendingDiff.Removed)
                    scroll.Add(ChangeRow($"- {c.Table}.{c.Key} [{c.LocaleCode}]", c.OldValue, new Color(0.9f, 0.5f, 0.5f)));

            _body.Add(scroll);

            var actions = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var applyBtn = new Button(ApplyImport) { text = "Apply Import" };
            var cancelBtn = new Button(CancelImport) { text = "Cancel" };
            applyBtn.SetEnabled(_pendingDiff.TotalChanges > 0 ||
                                (_mode == ImportMode.ReplaceAll && _pendingDiff.Removed.Count > 0));
            actions.Add(applyBtn);
            actions.Add(cancelBtn);
            _body.Add(actions);
        }

        private static VisualElement ChangeRow(string label, string detail, Color color)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingLeft = 4, paddingTop = 1, paddingBottom = 1 } };
            var l = new Label(label) { style = { width = 220, color = color } };
            var d = new Label(detail) { style = { flexGrow = 1, whiteSpace = WhiteSpace.NoWrap, overflow = Overflow.Hidden } };
            row.Add(l);
            row.Add(d);
            return row;
        }

        // ---------------- Apply ----------------

        private void ApplyImport()
        {
            if (_settings == null || _pendingCsv == null) return;

            Undo.RecordObject(_settings, "Import Localization CSV");

            if (_mode == ImportMode.ReplaceAll)
                ApplyReplace();
            else
                ApplyMerge();

            _settings.InvalidateIndex();
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssetIfDirty(_settings);

            Log.Info("Localization CSV import applied.");
            _pendingCsv = null;
            _pendingDiff = null;
            Rebuild();
        }

        private void ApplyMerge()
        {
            var so = new SerializedObject(_settings);
            so.Update();

            foreach (var (tableName, csvTable) in _pendingCsv.Data)
            {
                var tableProp = FindOrCreateTable(so, tableName);
                var entriesProp = tableProp.FindPropertyRelative("_entries");

                foreach (var (key, csvKey) in csvTable)
                {
                    var entryProp = FindOrCreateEntry(entriesProp, key);
                    var valuesProp = entryProp.FindPropertyRelative("_values");

                    foreach (var (locale, value) in csvKey)
                    {
                        if (!_settings.HasLocale(locale)) continue;
                        SetLocaleValue(valuesProp, locale, value);
                    }
                }
            }

            so.ApplyModifiedProperties();
        }

        private void ApplyReplace()
        {
            var so = new SerializedObject(_settings);
            so.Update();

            var tablesProp = so.FindProperty("_tables");
            tablesProp.ClearArray();

            foreach (var (tableName, csvTable) in _pendingCsv.Data)
            {
                int ti = tablesProp.arraySize;
                tablesProp.InsertArrayElementAtIndex(ti);
                var tableProp = tablesProp.GetArrayElementAtIndex(ti);
                tableProp.FindPropertyRelative("_name").stringValue = tableName;
                var entriesProp = tableProp.FindPropertyRelative("_entries");
                entriesProp.ClearArray();

                foreach (var (key, csvKey) in csvTable)
                {
                    int ei = entriesProp.arraySize;
                    entriesProp.InsertArrayElementAtIndex(ei);
                    var entryProp = entriesProp.GetArrayElementAtIndex(ei);
                    entryProp.FindPropertyRelative("_key").stringValue = key;
                    var valuesProp = entryProp.FindPropertyRelative("_values");
                    valuesProp.ClearArray();

                    foreach (var (locale, value) in csvKey)
                    {
                        if (!_settings.HasLocale(locale)) continue;
                        int vi = valuesProp.arraySize;
                        valuesProp.InsertArrayElementAtIndex(vi);
                        var v = valuesProp.GetArrayElementAtIndex(vi);
                        v.FindPropertyRelative("LocaleCode").stringValue = locale;
                        v.FindPropertyRelative("Value").stringValue = value;
                    }
                }
            }

            so.ApplyModifiedProperties();
        }

        private static SerializedProperty FindOrCreateTable(SerializedObject so, string tableName)
        {
            var tablesProp = so.FindProperty("_tables");
            for (int i = 0; i < tablesProp.arraySize; i++)
            {
                var t = tablesProp.GetArrayElementAtIndex(i);
                if (t.FindPropertyRelative("_name").stringValue == tableName)
                    return t;
            }

            int idx = tablesProp.arraySize;
            tablesProp.InsertArrayElementAtIndex(idx);
            var newTable = tablesProp.GetArrayElementAtIndex(idx);
            newTable.FindPropertyRelative("_name").stringValue = tableName;
            newTable.FindPropertyRelative("_entries").ClearArray();
            return newTable;
        }

        private static SerializedProperty FindOrCreateEntry(SerializedProperty entriesProp, string key)
        {
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                var e = entriesProp.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("_key").stringValue == key)
                    return e;
            }

            int idx = entriesProp.arraySize;
            entriesProp.InsertArrayElementAtIndex(idx);
            var newEntry = entriesProp.GetArrayElementAtIndex(idx);
            newEntry.FindPropertyRelative("_key").stringValue = key;
            newEntry.FindPropertyRelative("_values").ClearArray();
            return newEntry;
        }

        private static void SetLocaleValue(SerializedProperty valuesProp, string locale, string value)
        {
            for (int i = 0; i < valuesProp.arraySize; i++)
            {
                var v = valuesProp.GetArrayElementAtIndex(i);
                if (v.FindPropertyRelative("LocaleCode").stringValue == locale)
                {
                    v.FindPropertyRelative("Value").stringValue = value;
                    return;
                }
            }

            int idx = valuesProp.arraySize;
            valuesProp.InsertArrayElementAtIndex(idx);
            var newVal = valuesProp.GetArrayElementAtIndex(idx);
            newVal.FindPropertyRelative("LocaleCode").stringValue = locale;
            newVal.FindPropertyRelative("Value").stringValue = value;
        }

        private void CancelImport()
        {
            _pendingCsv = null;
            _pendingDiff = null;
            Rebuild();
        }

        private static VisualElement Divider()
        {
            var d = new VisualElement();
            d.style.height = 1;
            d.style.marginTop = 10;
            d.style.marginBottom = 10;
            d.style.backgroundColor = new Color(0, 0, 0, 0.2f);
            return d;
        }
    }
}
