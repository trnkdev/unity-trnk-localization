using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    internal sealed class ValidationTab : ITab
    {
        public string Title => "Validation";
        public VisualElement Root { get; }

        private LocalizationSettings _settings;
        private VisualElement _body;

        internal ValidationTab()
        {
            Root = new VisualElement { name = "tab-validation-root" };
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

            var runBtn = new Button(RunValidation) { text = "Run Validation" };
            runBtn.style.width = 140;
            runBtn.style.marginBottom = 8;
            _body.Add(runBtn);

            RunValidation();
        }

        private void RunValidation()
        {
            while (_body.childCount > 1)
                _body.RemoveAt(1);

            var report = Analyze();

            var coverageHeader = new Label("Coverage");
            coverageHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            coverageHeader.style.marginBottom = 4;
            _body.Add(coverageHeader);

            foreach (var (locale, filled, total) in report.Coverage)
            {
                float pct = total == 0 ? 0f : (float)filled / total;
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
                row.Add(new Label(locale) { style = { width = 60 } });

                var bar = new VisualElement { style = { width = 160, height = 12, backgroundColor = new Color(0, 0, 0, 0.25f), marginRight = 8 } };
                var fill = new VisualElement
                {
                    style =
                    {
                        width = Length.Percent(pct * 100f),
                        height = 12,
                        backgroundColor = pct >= 1f
                            ? new Color(0.4f, 0.8f, 0.4f)
                            : (pct >= 0.5f ? new Color(0.9f, 0.8f, 0.4f) : new Color(0.9f, 0.5f, 0.5f))
                    }
                };
                bar.Add(fill);
                row.Add(bar);
                row.Add(new Label($"{filled}/{total} ({pct * 100f:0}%)"));
                _body.Add(row);
            }

            var issuesHeader = new Label("Issues");
            issuesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            issuesHeader.style.marginTop = 10;
            issuesHeader.style.marginBottom = 4;
            _body.Add(issuesHeader);

            if (report.Issues.Count == 0)
            {
                _body.Add(new HelpBox("No issues found. All entries are fully translated.", HelpBoxMessageType.Info));
                return;
            }

            var scroll = new ScrollView { style = { flexGrow = 1, marginTop = 2 } };
            scroll.style.borderTopWidth = 1;
            scroll.style.borderBottomWidth = 1;
            scroll.style.borderTopColor = new Color(0, 0, 0, 0.2f);
            scroll.style.borderBottomColor = new Color(0, 0, 0, 0.2f);

            foreach (var issue in report.Issues)
            {
                var l = new Label(issue) { style = { paddingLeft = 4, paddingTop = 1, paddingBottom = 1, whiteSpace = WhiteSpace.Normal } };
                scroll.Add(l);
            }
            _body.Add(scroll);
        }

        private Report Analyze()
        {
            var report = new Report();

            var localeCodes = new List<string>();
            foreach (var l in _settings.Locales)
                if (!string.IsNullOrEmpty(l.Code)) localeCodes.Add(l.Code);

            var filledPerLocale = new Dictionary<string, int>();
            foreach (var code in localeCodes) filledPerLocale[code] = 0;

            int totalEntries = 0;
            var seenKeysPerTable = new Dictionary<string, HashSet<string>>();

            foreach (var table in _settings.Tables)
            {
                if (string.IsNullOrWhiteSpace(table.Name))
                    report.Issues.Add("A table has an empty name.");

                if (!seenKeysPerTable.TryGetValue(table.Name ?? "", out var seenKeys))
                {
                    seenKeys = new HashSet<string>();
                    seenKeysPerTable[table.Name ?? ""] = seenKeys;
                }

                foreach (var entry in table.Entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.Key))
                    {
                        report.Issues.Add($"[{table.Name}] An entry has an empty key.");
                        continue;
                    }

                    if (!seenKeys.Add(entry.Key))
                        report.Issues.Add($"[{table.Name}] Duplicate key: '{entry.Key}'.");

                    totalEntries++;

                    foreach (var code in localeCodes)
                    {
                        string value = null;
                        foreach (var lv in entry.Values)
                            if (lv.LocaleCode == code) { value = lv.Value; break; }

                        if (!string.IsNullOrEmpty(value))
                            filledPerLocale[code]++;
                        else
                            report.Issues.Add($"[{table.Name}] '{entry.Key}' missing translation for '{code}'.");
                    }
                }
            }

            foreach (var code in localeCodes)
                report.Coverage.Add((code, filledPerLocale[code], totalEntries));

            return report;
        }

        private sealed class Report
        {
            public List<(string locale, int filled, int total)> Coverage = new();
            public List<string> Issues = new();
        }
    }
}
