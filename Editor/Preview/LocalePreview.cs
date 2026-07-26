#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TRnK.Localization
{
    /// <summary>Edit-Mode locale preview: overrides lookups and refreshes every LocalizedText in open scenes and the prefab stage.</summary>
    internal static class LocalePreview
    {
        /// <summary>The locale code being previewed, or null when preview is off.</summary>
        internal static string ActiveLocale { get; private set; }

        internal static void Apply(LocalizationConfig config, string localeCode)
        {
            if (config == null || string.IsNullOrEmpty(localeCode)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            LocalizationService.SetEditorPreview(config, localeCode);
            ActiveLocale = localeCode;
            RefreshAll();
        }

        /// <summary>Turns preview off, restoring open scenes to the config's default locale.</summary>
        internal static void Clear(LocalizationConfig config)
        {
            if (ActiveLocale == null) return;

            if (config != null && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                LocalizationService.SetEditorPreview(config, config.DefaultLocale);
                RefreshAll();
            }

            LocalizationService.ClearEditorPreview();
            ActiveLocale = null;
        }

        // Preview writes TMP text directly without dirtying scenes; even if a scene is saved
        // mid-preview, LocalizedText overwrites the value again on every runtime OnEnable.
        private static void RefreshAll()
        {
            var components = Object.FindObjectsByType<LocalizedText>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var component in components)
                component.RefreshEditorPreview();

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.prefabContentsRoot != null)
            {
                foreach (var component in stage.prefabContentsRoot.GetComponentsInChildren<LocalizedText>(true))
                    component.RefreshEditorPreview();
            }
        }
    }
}
#endif
