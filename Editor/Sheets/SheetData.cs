#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace TRnK.Localization
{
    /// <summary>Localization tables read from spreadsheet tabs, with the warnings found on the way.</summary>
    internal sealed class SheetData
    {
        private const string KeyHeader = "Key";
        private const string TypedLineBreak = "\\n";
        private const string WindowsLineBreak = "\r\n";
        private const string LineBreak = "\n";

        // tableName -> key -> localeCode -> value; names, keys and codes ignore case like the runtime
        internal readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> Tables = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Locale codes across all tabs, in first-seen column order and spelling.</summary>
        internal readonly List<string> LocaleCodes = new();

        internal readonly List<string> Warnings = new();

        /// <summary>Adds a tab whose header is 'Key' followed by locale codes; any other tab is skipped.</summary>
        internal void AddTab(string tabName, List<List<string>> rows)
        {
            if (rows.Count == 0 || rows[0].Count == 0
                || !string.Equals(rows[0][0]?.Trim(), KeyHeader, StringComparison.OrdinalIgnoreCase))
            {
                Warnings.Add($"Tab '{tabName}' skipped: its first column header is not '{KeyHeader}'.");
                return;
            }

            var header = rows[0];
            var columns = new List<(int Index, string Code)>();
            for (int c = 1; c < header.Count; c++)
            {
                string code = header[c]?.Trim();
                if (string.IsNullOrEmpty(code)) continue;

                columns.Add((c, CanonicalCode(code)));
            }

            if (columns.Count == 0)
            {
                Warnings.Add($"Tab '{tabName}' skipped: it has no locale columns.");
                return;
            }

            var table = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                string key = row.Count > 0 ? row[0]?.Trim() : null;
                if (string.IsNullOrEmpty(key)) continue;

                if (table.ContainsKey(key))
                    Warnings.Add($"Tab '{tabName}' row {r + 1}: duplicate key '{key}' (keys ignore case), the last one wins.");

                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (index, code) in columns)
                    values[code] = index < row.Count ? NormalizeLineBreaks(row[index]) : string.Empty;

                table[key] = values;
            }

            if (Tables.ContainsKey(tabName))
                Warnings.Add($"Tab '{tabName}' has the same name as another tab (names ignore case), the last one wins.");

            Tables[tabName] = table;
        }

        // The first spelling seen wins, so 'EN' in a later tab still maps to 'en'
        private string CanonicalCode(string code)
        {
            foreach (string known in LocaleCodes)
                if (string.Equals(known, code, StringComparison.OrdinalIgnoreCase))
                    return known;

            LocaleCodes.Add(code);
            return code;
        }

        // A typed \n becomes a real line break, so text reads the same outside TMP
        private static string NormalizeLineBreaks(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace(WindowsLineBreak, LineBreak).Replace(TypedLineBreak, LineBreak);
        }
    }
}
#endif
