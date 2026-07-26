#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace TRnK.Localization
{
    /// <summary>Converts raw parsed CSV grids into structured data and diffs them against a <see cref="LocalizationConfig"/> asset.</summary>
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
            internal List<ChangeEntry> Removed = new();

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

        /// <summary>Computes a diff between parsed CSV data and the current config asset.</summary>
        internal static DiffResult Compute(LocalizationConfig config, ParsedCsv csv)
        {
            var result = new DiffResult();
            if (config == null || csv == null) return result;

            // Build current state lookup: tableName -> key -> locale -> value
            var current = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.Ordinal);
            foreach (var table in config.Tables)
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
                            if (!string.IsNullOrEmpty(value))
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
#endif
