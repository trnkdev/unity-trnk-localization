#if UNITY_EDITOR
using TRnK.Toolkit;
using UnityEngine;

namespace TRnK.Localization
{
    /// <summary>Project-scoped editor settings for TRnK Localization, stored as an asset in the project.</summary>
    internal sealed class LocalizationEditorSettings : ScriptableObject
    {
        private const string SettingsFolder = "Assets/Plugins/TRnK/Localization/Editor";
        private const string AssetPath = SettingsFolder + "/LocalizationEditorSettings.asset";

        private static LocalizationEditorSettings s_instance;
        private static LocalizationEditorSettings s_transient;

        [SerializeField] private LocalizationConfig _activeConfig;

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
