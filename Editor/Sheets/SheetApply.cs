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
    /// <summary>Writes imported spreadsheet data into the table assets of the Tables Folder.</summary>
    internal static class SheetApply
    {
        private const string UndoName = "Import Localization";
        private const char InvalidFileNameReplacement = '_';

        /// <summary>Rewrites every table from the sheet, updating existing assets in place and trashing dropped ones.</summary>
        internal static void ReplaceAll(LocalizationConfig config, SheetData data, string tablesFolder)
        {
            // One undo step per import, even when several run in a frame
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            int undoGroup = Undo.GetCurrentGroup();

            var existing = ByName(TableAssets.InFolder(tablesFolder));
            string[] locales = data.LocaleCodes.ToArray();

            foreach (var (tableName, rows) in data.Tables)
            {
                bool isNew = !existing.TryGetValue(tableName, out var table);
                table = WriteTable(table, tableName, locales, rows, tablesFolder);

#if TRNK_LOCALIZATION_ADDRESSABLES
                // Existing tables keep whatever Addressables setup the project gave them
                if (isNew) TableAddressables.Mark(table);
                else TableAddressables.UpdateAddress(table);
#endif
            }

            var so = new SerializedObject(config);
            so.Update();
            ApplyLocales(so, data);
            so.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);

            // The folder mirrors the sheet; a stale table would otherwise still show and could still ship
            foreach (var (tableName, table) in existing)
            {
                if (data.Tables.ContainsKey(tableName)) continue;

                string path = AssetDatabase.GetAssetPath(table);
                if (AssetDatabase.MoveAssetToTrash(path))
                    Log.Info($"Table '{tableName}' is no longer in the spreadsheet: moved '{path}' to the trash.");
            }

            TableAssets.Invalidate();
        }

        private static LocalizationTable WriteTable(LocalizationTable table, string tableName, string[] locales,
                                                    Dictionary<string, Dictionary<string, string>> rows, string tablesFolder)
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

            EditorAssetUtils.EnsureFolderPath(tablesFolder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{tablesFolder}/{SafeFileName(tableName)}.asset");
            AssetDatabase.CreateAsset(table, path);
            Undo.RegisterCreatedObjectUndo(table, UndoName);

            Log.Info($"Created localization table '{tableName}' at '{path}'.");
            return table;
        }

        private static Dictionary<string, LocalizationTable> ByName(IReadOnlyList<LocalizationTable> tables)
        {
            var byName = new Dictionary<string, LocalizationTable>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in tables)
            {
                if (string.IsNullOrEmpty(table.TableName)) continue;

                if (byName.TryGetValue(table.TableName, out var kept))
                {
                    Log.Warn($"Two assets hold table '{table.TableName}': '{AssetDatabase.GetAssetPath(kept)}' is updated, '{AssetDatabase.GetAssetPath(table)}' is ignored.");
                    continue;
                }

                byName[table.TableName] = table;
            }

            return byName;
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
