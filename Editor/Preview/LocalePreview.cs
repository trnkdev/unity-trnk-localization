#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TRnK.Localization
{
    /// <summary>Switches the previewed locale: overrides lookups in Edit Mode, or the live locale while playing.</summary>
    internal static class LocalePreview
    {
        /// <summary>The locale code being previewed, or null when preview is off.</summary>
        internal static string ActiveLocale { get; private set; }

        internal static void Apply(LocalizationConfig config, string localeCode)
        {
            if (config == null || string.IsNullOrEmpty(localeCode)) return;

            ActiveLocale = localeCode;

            // In Play Mode the running game owns the locale — switch it for real
            if (EditorApplication.isPlaying)
            {
                Loc.SetLocale(localeCode);
                return;
            }

            LocalizationService.SetEditorPreview(config, localeCode);
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
