#if UNITY_EDITOR
using System.Collections.Generic;
using TRnK.Toolkit;
using UnityEngine;

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

        [SerializeField] private LocalizationConfig _activeConfig;

        [SerializeField] private string _spreadsheetUrl;

        [SerializeField] private List<string> _ignoredTabs = new() { DefaultIgnoredTab };

        [SerializeField] private string _tablesFolder;

        /// <summary>Any URL of the Google spreadsheet holding the localization tabs.</summary>
        internal string SpreadsheetUrl
        {
            get => _spreadsheetUrl;
            set
            {
                if (_spreadsheetUrl == value) return;
                _spreadsheetUrl = value;
                EditorAssetUtils.MarkDirtyAndSave(this);
            }
        }

        /// <summary>Spreadsheet tabs the import skips.</summary>
        internal IReadOnlyList<string> IgnoredTabs => _ignoredTabs;

        internal void SetIgnoredTabs(IEnumerable<string> names)
        {
            _ignoredTabs.Clear();
            _ignoredTabs.AddRange(names);
            EditorAssetUtils.MarkDirtyAndSave(this);
        }

        /// <summary>Folder for new table assets. Empty means the config's folder.</summary>
        internal string TablesFolder
        {
            get => _tablesFolder;
            set
            {
                if (_tablesFolder == value) return;
                _tablesFolder = value;
                EditorAssetUtils.MarkDirtyAndSave(this);
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
                EditorAssetUtils.MarkDirtyAndSave(this);
            }
        }

        internal static LocalizationEditorSettings GetOrCreate() =>
            EditorAssetUtils.GetOrCreateSettings(SettingsFolder, AssetPath, ref s_instance, ref s_transient);
    }
}
#endif
