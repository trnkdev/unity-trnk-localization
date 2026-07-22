using System;
using System.Collections.Generic;

namespace TRnK.Localization
{
    /// <summary>Converts raw parsed CSV grids into structured data and diffs them against a <see cref="LocalizationSettings"/> asset.</summary>
    internal static class CsvDiff
    {
        /// <summary>Result of converting a parsed CSV grid into structured form.</summary>
        internal sealed class ParsedCsv
        {
            // tableName -> key -> localeCode -> value
            internal Dictionary<string, Dictionary<string, Dictionary<string, string>>> Data
                = new(StringComparer.Ordinal);

            // Locale codes detected in the header row, in order.
            internal List<string> LocaleCodes = new();

            // Warnings collected during parsing (e.g. duplicate keys, malformed rows).
            internal List<string> Warnings = new();
        }

        /// <summary>Result of diffing parsed CSV against a settings asset.</summary>
        internal sealed class DiffResult
        {
            internal List<ChangeEntry> Added   = new();
            internal List<ChangeEntry> Updated = new();
            internal List<ChangeEntry> Removed = new(); // Only meaningful in Replace mode
            internal List<string>      LocalesNotInSettings = new();
            internal List<string>      LocalesInSettingsNotInCsv = new();

            internal int TotalChanges => Added.Count + Updated.Count + Removed.Count;
        }

        internal struct ChangeEntry
        {
            internal string Table;
            internal string Key;
            internal string LocaleCode;
            internal string OldValue;
            internal string NewValue;
        }

        /// <summary>Converts a raw CSV grid into structured data. Expects header row <c>Table,Key,locale1,locale2,...</c>.</summary>
        internal static ParsedCsv Structure(List<List<string>> rows)
        {
            var result = new ParsedCsv();
            if (rows == null || rows.Count < 1) return result;

            var header = rows[0];
            if (header.Count < 3)
            {
                result.Warnings.Add("Header row must contain at least: Table, Key, and one locale column.");
                return result;
            }

            // Validate header columns 0 and 1
            if (!string.Equals(header[0]?.Trim(), "Table", StringComparison.OrdinalIgnoreCase))
                result.Warnings.Add($"Expected first column to be 'Table', got '{header[0]}'.");
            if (!string.Equals(header[1]?.Trim(), "Key", StringComparison.OrdinalIgnoreCase))
                result.Warnings.Add($"Expected second column to be 'Key', got '{header[1]}'.");

            // Locale codes from columns 2..n
            for (int c = 2; c < header.Count; c++)
            {
                var code = header[c]?.Trim();
                if (!string.IsNullOrEmpty(code)) result.LocaleCodes.Add(code);
            }

            // Data rows
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count < 2 || (string.IsNullOrEmpty(row[0]) && string.IsNullOrEmpty(row[1])))
                    continue; // Skip blank rows

                var table = row[0]?.Trim();
                var key   = row[1]?.Trim();

                if (string.IsNullOrEmpty(table))
                {
                    result.Warnings.Add($"Row {r + 1}: empty Table column, skipped.");
                    continue;
                }

                if (string.IsNullOrEmpty(key))
                {
                    result.Warnings.Add($"Row {r + 1}: empty Key column, skipped.");
                    continue;
                }

                if (!result.Data.TryGetValue(table, out var tableDict))
                {
                    tableDict = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
                    result.Data[table] = tableDict;
                }

                if (tableDict.ContainsKey(key))
                {
                    result.Warnings.Add($"Row {r + 1}: duplicate key '{key}' in table '{table}', last occurrence wins.");
                }

                var keyDict = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int c = 0; c < result.LocaleCodes.Count; c++)
                {
                    int colIndex = c + 2; // First two columns are Table and Key
                    string value = colIndex < row.Count ? row[colIndex] : string.Empty;
                    keyDict[result.LocaleCodes[c]] = value ?? string.Empty;
                }

                tableDict[key] = keyDict;
            }

            return result;
        }

        /// <summary>Computes a diff between parsed CSV data and the current settings asset.</summary>
        internal static DiffResult Compute(LocalizationSettings settings, ParsedCsv csv)
        {
            var result = new DiffResult();
            if (settings == null || csv == null) return result;

            // Locale validation
            var settingsLocales = new HashSet<string>(StringComparer.Ordinal);
            foreach (var locale in settings.Locales)
                if (!string.IsNullOrEmpty(locale.Code)) settingsLocales.Add(locale.Code);

            var csvLocales = new HashSet<string>(csv.LocaleCodes, StringComparer.Ordinal);

            foreach (var code in csvLocales)
                if (!settingsLocales.Contains(code))
                    result.LocalesNotInSettings.Add(code);

            foreach (var code in settingsLocales)
                if (!csvLocales.Contains(code))
                    result.LocalesInSettingsNotInCsv.Add(code);

            // Build current state lookup: tableName -> key -> locale -> value
            var current = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.Ordinal);
            foreach (var table in settings.Tables)
            {
                if (string.IsNullOrEmpty(table.Name)) continue;
                var tableDict = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
                foreach (var entry in table.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Key)) continue;
                    var keyDict = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var lv in entry.Values)
                    {
                        if (!string.IsNullOrEmpty(lv.LocaleCode))
                            keyDict[lv.LocaleCode] = lv.Value ?? string.Empty;
                    }
                    tableDict[entry.Key] = keyDict;
                }
                current[table.Name] = tableDict;
            }

            // Compare CSV against current — Added & Updated
            foreach (var (tableName, csvTable) in csv.Data)
            {
                current.TryGetValue(tableName, out var currentTable);

                foreach (var (key, csvKey) in csvTable)
                {
                    Dictionary<string, string> currentKey = null;
                    if (currentTable != null) currentTable.TryGetValue(key, out currentKey);

                    foreach (var (locale, newValue) in csvKey)
                    {
                        // Skip locales not in settings — they're not applicable
                        if (!settingsLocales.Contains(locale)) continue;

                        string oldValue = null;
                        bool hadValue = currentKey != null && currentKey.TryGetValue(locale, out oldValue);

                        if (!hadValue)
                        {
                            if (!string.IsNullOrEmpty(newValue))
                                result.Added.Add(Make(tableName, key, locale, null, newValue));
                        }
                        else if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
                        {
                            result.Updated.Add(Make(tableName, key, locale, oldValue, newValue));
                        }
                    }
                }
            }

            // Compare current against CSV — Removed (only meaningful in Replace mode)
            foreach (var (tableName, currentTable) in current)
            {
                csv.Data.TryGetValue(tableName, out var csvTable);

                foreach (var (key, currentKey) in currentTable)
                {
                    bool keyInCsv = csvTable != null && csvTable.ContainsKey(key);
                    if (!keyInCsv)
                    {
                        foreach (var (locale, value) in currentKey)
                        {
                            if (settingsLocales.Contains(locale) && !string.IsNullOrEmpty(value))
                                result.Removed.Add(Make(tableName, key, locale, value, null));
                        }
                    }
                }
            }

            return result;
        }

        static ChangeEntry Make(string table, string key, string locale, string oldValue, string newValue)
        {
            return new ChangeEntry
            {
                Table = table,
                Key = key,
                LocaleCode = locale,
                OldValue = oldValue,
                NewValue = newValue,
            };
        }
    }
}
