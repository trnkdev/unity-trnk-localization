#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TRnK.Logger;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    /// <summary>Syncs the spreadsheet into the config: fetch, preview the diff, then apply.</summary>
    internal sealed class SyncTab : ITab
    {
        private const string TabsTooltip =
            "Spreadsheet tab names, one per line. Each becomes a table of the same name.";
        private const string UrlPlaceholder =
            "https://docs.google.com/spreadsheets/d/…";
        private const string TabsPlaceholder = "UI\nCombat\nItems";
        private const string SheetHelp =
            "Each tab needs a 'Key' column followed by one column per locale code. " +
            "Share the spreadsheet as 'Anyone with the link → Viewer'.";

        public string Title => "Sync";
        public VisualElement Root { get; }

        private LocalizationConfig _config;
        private VisualElement _body;

        private CsvDiff.ParsedCsv _pendingCsv;
        private CsvDiff.DiffResult _pendingDiff;
        private bool _isFetching;

        internal SyncTab()
        {
            Root = new VisualElement { name = "tab-sync-root" };
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
            ClearPending();
            Rebuild();
        }

        public void OnSelected() => Rebuild();

        private void Rebuild()
        {
            _body.Clear();

            if (_config == null)
            {
                _body.Add(new Label("Select a LocalizationConfig asset above.") { style = { marginTop = 16 } });
                return;
            }

            BuildSourceSection();
            _body.Add(LocalizationStyles.Divider());
            BuildActionSection();

            if (_pendingDiff != null)
                BuildPreviewSection();
        }

        // ---------------- Source ----------------

        private void BuildSourceSection()
        {
            var settings = LocalizationEditorSettings.GetOrCreate();

            _body.Add(LocalizationStyles.Header("Spreadsheet"));
            _body.Add(new HelpBox(SheetHelp, HelpBoxMessageType.Info));

            var urlField = new TextField("Spreadsheet URL") { value = settings.SpreadsheetUrl };
            urlField.textEdition.placeholder = UrlPlaceholder;
            urlField.textEdition.hidePlaceholderOnFocus = false;
            urlField.style.marginTop = 4;
            urlField.RegisterValueChangedCallback(evt =>
                LocalizationEditorSettings.GetOrCreate().SpreadsheetUrl = evt.newValue);
            _body.Add(urlField);

            var tabsField = new TextField("Tab Names")
            {
                multiline = true,
                value = string.Join("\n", settings.TabNames),
                tooltip = TabsTooltip
            };
            tabsField.textEdition.placeholder = TabsPlaceholder;
            tabsField.textEdition.hidePlaceholderOnFocus = false;
            tabsField.style.minHeight = 60;
            tabsField.RegisterValueChangedCallback(evt =>
                LocalizationEditorSettings.GetOrCreate().SetTabNames(SplitTabs(evt.newValue)));
            _body.Add(tabsField);
        }

        private static List<string> SplitTabs(string raw)
        {
            var names = new List<string>();
            if (string.IsNullOrWhiteSpace(raw)) return names;

            foreach (string line in raw.Split('\n'))
            {
                string name = line.Trim();
                if (name.Length > 0) names.Add(name);
            }
            return names;
        }

        // ---------------- Actions ----------------

        private void BuildActionSection()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var syncBtn = new Button(StartSync) { text = "Sync from Spreadsheet" };
            syncBtn.style.height = 24;
            syncBtn.SetEnabled(!_isFetching);
            row.Add(syncBtn);

            var exportBtn = new Button(DoExport) { text = "Export CSV…" };
            exportBtn.tooltip = "Write the current config to a CSV file — for seeding a new spreadsheet or keeping a text backup.";
            exportBtn.style.height = 24;
            row.Add(exportBtn);

            _body.Add(row);

            if (_isFetching)
                _body.Add(new Label("Fetching…") { style = { marginTop = 4 } });
        }

        private void StartSync()
        {
            if (_isFetching || _config == null) return;

            var settings = LocalizationEditorSettings.GetOrCreate();
            _isFetching = true;
            ClearPending();
            Rebuild();

            SheetFetcher.FetchAll(settings.SpreadsheetUrl, settings.TabNames, result =>
            {
                _isFetching = false;

                if (!result.Success)
                {
                    EditorUtility.DisplayDialog("Sync Failed", result.Error, "OK");
                    Rebuild();
                    return;
                }

                _pendingCsv = SheetStructure.Combine(result.Csv);
                _pendingDiff = CsvDiff.Compute(_config, _pendingCsv);
                Rebuild();
            });
        }

        // ---------------- Preview ----------------

        private void BuildPreviewSection()
        {
            _body.Add(LocalizationStyles.Divider());
            _body.Add(LocalizationStyles.Header("Preview"));

            var summary = new Label(
                $"+ {_pendingDiff.Added.Count} added   " +
                $"~ {_pendingDiff.Updated.Count} updated   " +
                $"- {_pendingDiff.Removed.Count} removed");
            summary.style.marginBottom = 4;
            _body.Add(summary);

            if (_pendingCsv.Warnings.Count > 0)
                _body.Add(new HelpBox(string.Join("\n", _pendingCsv.Warnings), HelpBoxMessageType.Warning));

            var scroll = new ScrollView { style = { maxHeight = 200, marginTop = 4, marginBottom = 8 } };
            scroll.style.borderTopWidth = 1;
            scroll.style.borderBottomWidth = 1;
            scroll.style.borderTopColor = new Color(0, 0, 0, 0.2f);
            scroll.style.borderBottomColor = new Color(0, 0, 0, 0.2f);

            foreach (var c in _pendingDiff.Added)
                scroll.Add(ChangeRow($"+ {c.Table}.{c.Key} [{c.LocaleCode}]", c.NewValue, LocalizationStyles.Added));
            foreach (var c in _pendingDiff.Updated)
                scroll.Add(ChangeRow($"~ {c.Table}.{c.Key} [{c.LocaleCode}]", $"{c.OldValue} → {c.NewValue}", LocalizationStyles.Updated));
            foreach (var c in _pendingDiff.Removed)
                scroll.Add(ChangeRow($"- {c.Table}.{c.Key} [{c.LocaleCode}]", c.OldValue, LocalizationStyles.Removed));

            _body.Add(scroll);

            var actions = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var applyBtn = new Button(ApplySync) { text = "Apply" };
            applyBtn.SetEnabled(_pendingDiff.TotalChanges > 0);
            var cancelBtn = new Button(CancelSync) { text = "Cancel" };
            actions.Add(applyBtn);
            actions.Add(cancelBtn);
            _body.Add(actions);
        }

        private static VisualElement ChangeRow(string label, string detail, Color color)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingLeft = 4, paddingTop = 1, paddingBottom = 1 } };
            row.Add(new Label(label) { style = { width = 220, color = color } });
            row.Add(new Label(detail) { style = { flexGrow = 1, whiteSpace = WhiteSpace.NoWrap, overflow = Overflow.Hidden } });
            return row;
        }

        // ---------------- Apply ----------------

        private void ApplySync()
        {
            if (_config == null || _pendingCsv == null) return;

            Undo.RecordObject(_config, "Sync Localization");
            SheetApply.ReplaceAll(_config, _pendingCsv);

            _config.InvalidateIndex();
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssetIfDirty(_config);

            Log.Info("Localization synced from spreadsheet.");
            ClearPending();
            Rebuild();
        }

        private void CancelSync()
        {
            ClearPending();
            Rebuild();
        }

        private void ClearPending()
        {
            _pendingCsv = null;
            _pendingDiff = null;
        }

        // ---------------- Export ----------------

        private void DoExport()
        {
            string path = EditorUtility.SaveFilePanel("Export Localization CSV", "", $"{_config.name}.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;

            string csv = CsvWriter.Write(BuildExportRows());

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
            foreach (var l in _config.Locales)
            {
                if (string.IsNullOrEmpty(l.Code)) continue;
                localeCodes.Add(l.Code);
                header.Add(l.Code);
            }
            rows.Add(header);

            foreach (var table in _config.Tables)
            {
                foreach (var entry in table.Entries)
                {
                    var row = new List<string> { table.Name, entry.Key };
                    foreach (string code in localeCodes)
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
    }
}
#endif
