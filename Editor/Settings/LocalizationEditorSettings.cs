#if UNITY_EDITOR
using System.Collections.Generic;
using TRnK.Toolkit;
using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TRnK.Localization
{
    /// <summary>Project-scoped editor settings for TRnK Localization, stored as an asset in the project.</summary>
    internal sealed class LocalizationEditorSettings : ScriptableObject
    {
        private const string SettingsFolder = "Assets/Plugins/TRnK/Localization/Editor";
        private const string AssetPath = SettingsFolder + "/LocalizationEditorSettings.asset";
        private const string DefaultIgnoredTab = "Rule";

        private static LocalizationEditorSettings s_instance;
        private static LocalizationEditorSettings s_transient;

        // Chosen in the Localization Manager's top bar
        [SerializeField, HideInInspector] private LocalizationConfig _activeConfig;

#if ODIN_INSPECTOR
        [LabelText("Spreadsheet URL")]
#endif
        [SerializeField, Tooltip("Any URL of the Google spreadsheet.")]
        private string _spreadsheetUrl;

#if ODIN_INSPECTOR
        [LabelText("Tables Folder"), FolderPath]
#endif
        [SerializeField, Tooltip("Where the table assets live. Empty uses the config's folder.")]
        private string _tablesFolder;

#if ODIN_INSPECTOR
        [LabelText("Ignored Sheet Tabs")]
#endif
        [SerializeField, Tooltip("Spreadsheet tabs the import skips, e.g. a rules or notes tab.")]
        private List<string> _ignoredTabs = new() { DefaultIgnoredTab };

        /// <summary>Any URL of the Google spreadsheet holding the localization tabs.</summary>
        internal string SpreadsheetUrl
        {
            get => _spreadsheetUrl;
            set
            {
                if (_spreadsheetUrl == value) return;
                _spreadsheetUrl = value;
                MarkChanged();
            }
        }

        /// <summary>Spreadsheet tabs the import skips.</summary>
        internal IReadOnlyList<string> IgnoredTabs => _ignoredTabs;

        internal void SetIgnoredTabs(IEnumerable<string> names)
        {
            _ignoredTabs.Clear();
            _ignoredTabs.AddRange(names);
            MarkChanged();
        }

        /// <summary>Folder for new table assets. Empty means the config's folder.</summary>
        internal string TablesFolder
        {
            get => _tablesFolder;
            set
            {
                if (_tablesFolder == value) return;
                _tablesFolder = value;
                MarkChanged();
            }
        }

        /// <summary>The config the Localization Manager (and editor validation) works against.</summary>
        internal LocalizationConfig ActiveConfig
        {
            get => _activeConfig;
            set
            {
                if (_activeConfig == value) return;
                _activeConfig = value;
                MarkChanged();
            }
        }

        /// <summary>False for the unsaved stand-in handed out while the settings asset is still importing.</summary>
        internal bool IsLoaded => EditorUtility.IsPersistent(this);

        /// <summary>Marks the settings changed in memory; <see cref="Save"/> writes them to disk.</summary>
        internal void MarkChanged()
        {
            if (IsLoaded) EditorUtility.SetDirty(this);
        }

        /// <summary>Writes only this asset, if changed; the unsaved stand-in is never saved.</summary>
        internal void Save()
        {
            if (IsLoaded) AssetDatabase.SaveAssetIfDirty(this);
        }

        internal static LocalizationEditorSettings GetOrCreate() =>
            EditorAssetUtils.GetOrCreateSettings(SettingsFolder, AssetPath, ref s_instance, ref s_transient);
    }
}
#endif
