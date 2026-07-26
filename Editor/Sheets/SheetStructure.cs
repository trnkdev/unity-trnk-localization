#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace TRnK.Localization
{
    /// <summary>Turns per-tab CSV into the structured form the diff and apply steps consume.</summary>
    internal static class SheetStructure
    {
        /// <summary>
        /// Builds one ParsedCsv from every fetched tab. A synced tab has no Table
        /// column — the tab name is the table — so its header is <c>Key,locale1,…</c>.
        /// </summary>
        internal static CsvDiff.ParsedCsv Combine(Dictionary<string, string> csvByTab)
        {
            var combined = new CsvDiff.ParsedCsv();

            foreach (var (tabName, csv) in csvByTab)
            {
                List<List<string>> rows;
                try
                {
                    rows = CsvParser.Parse(csv);
                }
                catch (Exception e)
                {
                    combined.Warnings.Add($"Tab '{tabName}': {e.Message}");
                    continue;
                }

                AppendTab(combined, tabName, rows);
            }

            return combined;
        }

        private static void AppendTab(CsvDiff.ParsedCsv combined, string tabName, List<List<string>> rows)
        {
            if (rows == null || rows.Count < 1)
            {
                combined.Warnings.Add($"Tab '{tabName}' is empty.");
                return;
            }

            var header = rows[0];
            if (header.Count < 2)
            {
                combined.Warnings.Add($"Tab '{tabName}': header must be 'Key' followed by at least one locale column.");
                return;
            }

            if (!string.Equals(header[0]?.Trim(), "Key", StringComparison.OrdinalIgnoreCase))
                combined.Warnings.Add($"Tab '{tabName}': expected first column to be 'Key', got '{header[0]}'.");

            var locales = new List<string>();
            for (int c = 1; c < header.Count; c++)
            {
                string code = header[c]?.Trim();
                if (!string.IsNullOrEmpty(code)) locales.Add(code);
            }

            // Locale columns are shared across tabs; a tab that adds one extends the set
            foreach (string code in locales)
                if (!combined.LocaleCodes.Contains(code))
                    combined.LocaleCodes.Add(code);

            var tableDict = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            combined.Data[tabName] = tableDict;

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count < 1) continue;

                string key = row[0]?.Trim();
                if (string.IsNullOrEmpty(key)) continue;

                if (tableDict.ContainsKey(key))
                    combined.Warnings.Add($"Tab '{tabName}' row {r + 1}: duplicate key '{key}', last occurrence wins.");

                var keyDict = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int c = 0; c < locales.Count; c++)
                {
                    int colIndex = c + 1; // First column is Key
                    string value = colIndex < row.Count ? row[colIndex] : string.Empty;
                    keyDict[locales[c]] = value ?? string.Empty;
                }

                tableDict[key] = keyDict;
            }
        }
    }
}
#endif
