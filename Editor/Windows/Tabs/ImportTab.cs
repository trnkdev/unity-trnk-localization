#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TRnK.Logger;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    /// <summary>Imports every spreadsheet tab, except ignored ones, into table assets.</summary>
    internal sealed class ImportTab : ITab
    {
        private const string UrlPlaceholder = "https://docs.google.com/spreadsheets/d/…";
        private const string IgnoredPlaceholder = "Sheet tab name";
        private const string IgnoredTooltip = "Spreadsheet tabs the import skips, e.g. a rules or notes tab.";
        private const string FolderTooltip =
            "Where the import creates new table assets. Existing tables are updated wherever they are. Empty uses the config's folder.";
        private const string FolderPickerTitle = "Folder for New Localization Tables";
        private const string SheetHelp =
            "Each tab becomes a table of the same name. Its first row is 'Key' followed by one column per locale code. " +
            "Share the spreadsheet as 'Anyone with the link → Viewer'.";
        private const string FailedTitle = "Import Failed";
        private const float ButtonHeight = 24f;

        public string Title => "Import";
        public VisualElement Root { get; }

        private readonly VisualElement _body;
        private LocalizationConfig _config;
        private bool _isImporting;

        internal ImportTab()
        {
            Root = new VisualElement { name = "tab-import-root" };
            Root.style.flexGrow = 1;
            Root.style.paddingTop = 8;
            Root.style.paddingLeft = 8;
            Root.style.paddingRight = 8;

            _body = new VisualElement { style = { flexGrow = 1 } };
            Root.Add(_body);
        }

        public void OnConfigChanged(LocalizationConfig config)
        {
            _config = config;
            Rebuild();
        }

        public void OnSelected() => Rebuild();

        private void Rebuild()
        {
            _body.Clear();

            if (_config == null)
            {
                _body.Add(new HelpBox("Select a LocalizationConfig asset above.", HelpBoxMessageType.Info));
                return;
            }

            var settings = LocalizationEditorSettings.GetOrCreate();

            _body.Add(LocalizationStyles.Header("Spreadsheet"));
            _body.Add(new HelpBox(SheetHelp, HelpBoxMessageType.Info));
            _body.Add(BuildUrlField(settings));
            _body.Add(BuildFolderRow(settings));
            _body.Add(BuildIgnoredList(settings));
            _body.Add(LocalizationStyles.Divider());

            var importButton = new Button(Import) { text = _isImporting ? "Importing…" : "Import from Spreadsheet" };
            importButton.style.height = ButtonHeight;
            importButton.SetEnabled(!_isImporting);
            _body.Add(importButton);
        }

        private static TextField BuildUrlField(LocalizationEditorSettings settings)
        {
            var field = new TextField("Spreadsheet URL") { value = settings.SpreadsheetUrl };
            field.textEdition.placeholder = UrlPlaceholder;
            field.textEdition.hidePlaceholderOnFocus = false;
            field.style.marginTop = 4;
            field.RegisterValueChangedCallback(evt => LocalizationEditorSettings.GetOrCreate().SpreadsheetUrl = evt.newValue?.Trim());
            return field;
        }

        private VisualElement BuildFolderRow(LocalizationEditorSettings settings)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };

            var field = new TextField("Tables Folder") { value = settings.TablesFolder, isDelayed = true, tooltip = FolderTooltip };
            field.textEdition.placeholder = SheetApply.ResolveNewTablesFolder(_config, null);
            field.textEdition.hidePlaceholderOnFocus = false;
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt =>
                LocalizationEditorSettings.GetOrCreate().TablesFolder = evt.newValue?.Trim().TrimEnd('/'));
            row.Add(field);

            row.Add(new Button(() =>
            {
                string absolute = EditorUtility.OpenFolderPanel(FolderPickerTitle, Application.dataPath, string.Empty);
                if (string.IsNullOrEmpty(absolute)) return;

                string relative = ToAssetsRelative(absolute);
                if (relative == null)
                {
                    EditorUtility.DisplayDialog("Invalid Folder", "Pick a folder inside this project's Assets folder.", "OK");
                    return;
                }

                field.value = relative;
            }) { text = "Browse…" });

            return row;
        }

        private static string ToAssetsRelative(string absolutePath)
        {
            string path = absolutePath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (!path.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase)) return null;
            return "Assets" + path.Substring(dataPath.Length);
        }

        private static ListView BuildIgnoredList(LocalizationEditorSettings settings)
        {
            // Working copy: the ListView mutates it and every change is saved
            var names = new List<string>(settings.IgnoredTabs);

            var list = new ListView(names)
            {
                headerTitle = "Ignored Tabs",
                showFoldoutHeader = true,
                showAddRemoveFooter = true,
                showBoundCollectionSize = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                tooltip = IgnoredTooltip,
                makeItem = () => MakeNameRow(names),
                bindItem = (element, index) =>
                {
                    var field = (TextField)element;
                    field.userData = index;
                    field.SetValueWithoutNotify(names[index] ?? string.Empty);
                },
            };

            list.itemsAdded += _ => SaveIgnored(names);
            list.itemsRemoved += _ => SaveIgnored(names);
            list.style.marginTop = 4;
            return list;
        }

        private static TextField MakeNameRow(List<string> names)
        {
            var field = new TextField { isDelayed = true };
            field.textEdition.placeholder = IgnoredPlaceholder;

            // Rows are recycled, so the index is read when edited, not captured when bound
            field.RegisterValueChangedCallback(evt =>
            {
                if (field.userData is not int index || index >= names.Count) return;

                names[index] = evt.newValue?.Trim() ?? string.Empty;
                SaveIgnored(names);
            });

            return field;
        }

        private static void SaveIgnored(List<string> names)
        {
            var cleaned = new List<string>(names.Count);
            foreach (string name in names)
                if (!string.IsNullOrWhiteSpace(name)) cleaned.Add(name.Trim());

            LocalizationEditorSettings.GetOrCreate().SetIgnoredTabs(cleaned);
        }

        private async void Import()
        {
            if (_isImporting || _config == null) return;

            var config = _config;
            var settings = LocalizationEditorSettings.GetOrCreate();

            string spreadsheetId = SheetWorkbook.ExtractSpreadsheetId(settings.SpreadsheetUrl);
            if (spreadsheetId == null)
            {
                EditorUtility.DisplayDialog(FailedTitle, "The spreadsheet URL is empty or not a Google Sheets link.", "OK");
                return;
            }

            _isImporting = true;
            Rebuild();

            try
            {
                var tabs = await SheetWorkbook.DownloadAsync(spreadsheetId);

                var data = new SheetData();
                foreach (var (name, rows) in tabs)
                    if (!IsIgnored(name, settings.IgnoredTabs)) data.AddTab(name, rows);

                foreach (string warning in data.Warnings) Log.Warn(warning);

                // An empty result would drop every table from the config
                if (data.Tables.Count == 0)
                {
                    EditorUtility.DisplayDialog(FailedTitle, "No tab starts with a 'Key' header. Nothing was imported.", "OK");
                    return;
                }

                SheetApply.ReplaceAll(config, data, SheetApply.ResolveNewTablesFolder(config, settings.TablesFolder));
                Log.Info($"Imported {data.Tables.Count} tables and {data.LocaleCodes.Count} locales into '{config.name}'.");
            }
            catch (Exception e)
            {
                Log.Error($"Localization import failed: {e.Message}");
                EditorUtility.DisplayDialog(FailedTitle, e.Message, "OK");
            }
            finally
            {
                _isImporting = false;
                Rebuild();
            }
        }

        private static bool IsIgnored(string tabName, IReadOnlyList<string> ignoredTabs)
        {
            foreach (string ignored in ignoredTabs)
                if (string.Equals(ignored?.Trim(), tabName?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
    }
}
#endif
