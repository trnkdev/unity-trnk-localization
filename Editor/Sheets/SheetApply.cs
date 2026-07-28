#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;

namespace TRnK.Localization
{
    /// <summary>Writes synced spreadsheet data into the config, replacing its tables wholesale.</summary>
    internal static class SheetApply
    {
        /// <summary>
        /// The spreadsheet is the only author, so the config's tables are rebuilt
        /// from it — no merging, and anything absent from the sheet is gone.
        /// </summary>
        internal static void ReplaceAll(LocalizationConfig config, CsvDiff.ParsedCsv csv)
        {
            var so = new SerializedObject(config);
            so.Update();

            ApplyLocales(so, csv);

            var tablesProp = so.FindProperty("_tables");
            tablesProp.ClearArray();

            foreach (var (tableName, csvTable) in csv.Data)
            {
                int tableIndex = tablesProp.arraySize;
                tablesProp.InsertArrayElementAtIndex(tableIndex);

                var tableProp = tablesProp.GetArrayElementAtIndex(tableIndex);
                tableProp.FindPropertyRelative("_name").stringValue = tableName;

                var entriesProp = tableProp.FindPropertyRelative("_entries");
                entriesProp.ClearArray();

                foreach (var (key, csvKey) in csvTable)
                {
                    int entryIndex = entriesProp.arraySize;
                    entriesProp.InsertArrayElementAtIndex(entryIndex);

                    var entryProp = entriesProp.GetArrayElementAtIndex(entryIndex);
                    entryProp.FindPropertyRelative("_key").stringValue = key;

                    var valuesProp = entryProp.FindPropertyRelative("_values");
                    valuesProp.ClearArray();

                    foreach (var (locale, value) in csvKey)
                    {
                        int valueIndex = valuesProp.arraySize;
                        valuesProp.InsertArrayElementAtIndex(valueIndex);

                        var valueProp = valuesProp.GetArrayElementAtIndex(valueIndex);
                        valueProp.FindPropertyRelative("LocaleCode").stringValue = locale;
                        valueProp.FindPropertyRelative("Value").stringValue = value;
                    }
                }
            }

            so.ApplyModifiedProperties();
        }

        // The spreadsheet header defines the locales — there is no other place to author them.
        // Existing entries keep their display name; the default locale falls back to the first column.
        private static void ApplyLocales(SerializedObject so, CsvDiff.ParsedCsv csv)
        {
            if (csv.LocaleCodes.Count == 0) return;

            var existingNames = new Dictionary<string, string>(StringComparer.Ordinal);
            var localesProp = so.FindProperty("_locales");

            for (int i = 0; i < localesProp.arraySize; i++)
            {
                var element = localesProp.GetArrayElementAtIndex(i);
                string code = element.FindPropertyRelative("Code").stringValue;
                if (!string.IsNullOrEmpty(code))
                    existingNames[code] = element.FindPropertyRelative("Name").stringValue;
            }

            localesProp.ClearArray();

            foreach (string code in csv.LocaleCodes)
            {
                int index = localesProp.arraySize;
                localesProp.InsertArrayElementAtIndex(index);

                var element = localesProp.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("Code").stringValue = code;
                element.FindPropertyRelative("Name").stringValue =
                    existingNames.TryGetValue(code, out string name) && !string.IsNullOrEmpty(name) && name != code
                        ? name
                        : DisplayNameFor(code);
            }

            var defaultProp = so.FindProperty("_defaultLocale");
            if (string.IsNullOrEmpty(defaultProp.stringValue) || !csv.LocaleCodes.Contains(defaultProp.stringValue))
                defaultProp.stringValue = csv.LocaleCodes[0];
        }

        // "en" -> "English"; unknown codes keep the code itself
        private static string DisplayNameFor(string code)
        {
            try
            {
                return CultureInfo.GetCultureInfo(code).EnglishName;
            }
            catch (CultureNotFoundException)
            {
                return code;
            }
        }
    }
}
#endif
