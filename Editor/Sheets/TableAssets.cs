#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace TRnK.Localization
{
    /// <summary>The table assets in the Tables Folder, where editor tools read tables from.</summary>
    internal static class TableAssets
    {
        private static readonly List<LocalizationTable> s_none = new();

        // Editor tools ask on every repaint, so the folder is only scanned again after the project changes
        private static string s_cachedFolder;
        private static List<LocalizationTable> s_cached;

        [InitializeOnLoadMethod]
        private static void ListenForChanges() => EditorApplication.projectChanged += Invalidate;

        /// <summary>The configured Tables Folder, or the config's own folder.</summary>
        internal static string ResolveFolder(LocalizationConfig config, string configuredFolder)
        {
            if (!string.IsNullOrEmpty(configuredFolder)) return configuredFolder;
            return Path.GetDirectoryName(AssetDatabase.GetAssetPath(config))?.Replace('\\', '/');
        }

        /// <summary>The tables in the Tables Folder set for this project and config.</summary>
        internal static IReadOnlyList<LocalizationTable> For(LocalizationConfig config)
            => config == null ? s_none : InFolder(ResolveFolder(config, LocalizationEditorSettings.GetOrCreate().TablesFolder));

        /// <summary>The tables in a folder and its subfolders, sorted by table name.</summary>
        internal static IReadOnlyList<LocalizationTable> InFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder)) return s_none;
            if (s_cached != null && s_cachedFolder == folder) return s_cached;

            var tables = new List<LocalizationTable>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(LocalizationTable)}", new[] { folder }))
            {
                var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(AssetDatabase.GUIDToAssetPath(guid));
                if (table != null) tables.Add(table);
            }

            tables.Sort((a, b) => string.CompareOrdinal(a.TableName, b.TableName));
            s_cachedFolder = folder;
            s_cached = tables;
            return tables;
        }

        internal static void Invalidate() => s_cached = null;
    }
}
#endif
