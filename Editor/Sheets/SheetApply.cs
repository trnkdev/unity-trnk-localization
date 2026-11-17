#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TRnK.Logger;
using TRnK.Toolkit;
using UnityEditor;
using UnityEngine;

namespace TRnK.Localization
{
    /// <summary>Writes imported spreadsheet data into table assets and lists them in the config.</summary>
    internal static class SheetApply
    {
        private const string UndoName = "Import Localization";
        private const char InvalidFileNameReplacement = '_';

        /// <summary>Rewrites every imported table from the sheet, updating existing assets in place.</summary>
        internal static void ReplaceAll(LocalizationConfig config, SheetData data, string newTablesFolder)
        {
            // One undo step per import, even when several run in a frame
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            int undoGroup = Undo.GetCurrentGroup();

            var existing = FindTableAssets(config);
            string[] locales = data.LocaleCodes.ToArray();
            var tables = new List<LocalizationTable>(data.Tables.Count);

            foreach (var (tableName, rows) in data.Tables)
            {
                existing.TryGetValue(tableName, out var table);
                tables.Add(WriteTable(table, tableName, locales, rows, newTablesFolder));
            }

            foreach (var listed in config.Tables)
                if (listed != null && !data.Tables.ContainsKey(listed.TableName))
                    Log.Info($"Table '{listed.TableName}' is no longer in the spreadsheet: removed from the config, its asset is kept.");

            var so = new SerializedObject(config);
            so.Update();

            ApplyLocales(so, data);

            var tablesProp = so.FindProperty("_tables");
            tablesProp.arraySize = tables.Count;
            for (int i = 0; i < tables.Count; i++)
                tablesProp.GetArrayElementAtIndex(i).objectReferenceValue = tables[i];

            so.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
        }

        /// <summary>The configured folder for new tables, or the config's own folder.</summary>
        internal static string ResolveNewTablesFolder(LocalizationConfig config, string configuredFolder)
        {
            if (!string.IsNullOrEmpty(configuredFolder)) return configuredFolder;
            return Path.GetDirectoryName(AssetDatabase.GetAssetPath(config))?.Replace('\\', '/');
        }

        private static LocalizationTable WriteTable(LocalizationTable table, string tableName, string[] locales,
                                                    Dictionary<string, Dictionary<string, string>> rows, string newTablesFolder)
        {
            var keys = new string[rows.Count];
            var values = new string[rows.Count * locales.Length];

            int row = 0;
            foreach (var (key, byLocale) in rows)
            {
                keys[row] = key;
                for (int column = 0; column < locales.Length; column++)
                    values[row * locales.Length + column] =
                        byLocale.TryGetValue(locales[column], out string value) ? value : string.Empty;
                row++;
            }

            if (table != null)
            {
                Undo.RecordObject(table, UndoName);
                table.SetData(tableName, (string[])locales.Clone(), keys, values);
                EditorUtility.SetDirty(table);
                return table;
            }

            table = ScriptableObject.CreateInstance<LocalizationTable>();
            table.SetData(tableName, (string[])locales.Clone(), keys, values);

            EditorAssetUtils.EnsureFolderPath(newTablesFolder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{newTablesFolder}/{SafeFileName(tableName)}.asset");
            AssetDatabase.CreateAsset(table, path);
            Undo.RegisterCreatedObjectUndo(table, UndoName);

            Log.Info($"Created localization table '{tableName}' at '{path}'.");
            return table;
        }

        // Config tables first, then unlisted project assets, so a dropped table is reused, not duplicated
        private static Dictionary<string, LocalizationTable> FindTableAssets(LocalizationConfig config)
        {
            var byName = new Dictionary<string, LocalizationTable>(StringComparer.Ordinal);

            foreach (var table in config.Tables)
                if (table != null && !string.IsNullOrEmpty(table.TableName))
                    byName.TryAdd(table.TableName, table);

            var otherConfigsTables = TablesListedByOtherConfigs(config);

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(LocalizationTable)}"))
            {
                var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(AssetDatabase.GUIDToAssetPath(guid));
                if (table == null || string.IsNullOrEmpty(table.TableName) || otherConfigsTables.Contains(table)) continue;

                if (byName.TryGetValue(table.TableName, out var kept) && kept != table)
                {
                    Log.Warn($"Two assets hold table '{table.TableName}': '{AssetDatabase.GetAssetPath(kept)}' is updated, '{AssetDatabase.GetAssetPath(table)}' is ignored.");
                    continue;
                }

                byName[table.TableName] = table;
            }

            return byName;
        }

        private static HashSet<LocalizationTable> TablesListedByOtherConfigs(LocalizationConfig config)
        {
            var tables = new HashSet<LocalizationTable>();

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(LocalizationConfig)}"))
            {
                var other = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (other == null || other == config) continue;

                foreach (var table in other.Tables)
                    if (table != null) tables.Add(table);
            }

            return tables;
        }

        private static string SafeFileName(string tableName)
        {
            var chars = tableName.ToCharArray();
            var invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < chars.Length; i++)
                if (Array.IndexOf(invalid, chars[i]) >= 0) chars[i] = InvalidFileNameReplacement;
            return new string(chars);
        }

        // The spreadsheet header defines the locales — there is no other place to author them.
        // Existing entries keep their display name; the default locale falls back to the first column.
        private static void ApplyLocales(SerializedObject so, SheetData data)
        {
            if (data.LocaleCodes.Count == 0) return;

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

            foreach (string code in data.LocaleCodes)
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
            if (string.IsNullOrEmpty(defaultProp.stringValue) || !data.LocaleCodes.Contains(defaultProp.stringValue))
                defaultProp.stringValue = data.LocaleCodes[0];
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
