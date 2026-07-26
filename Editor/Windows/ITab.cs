#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    /// <summary>Contract for a tab in the Localization Manager window.</summary>
    internal interface ITab
    {
        /// <summary>Display name shown in the tab bar (also matches button name in UXML).</summary>
        string Title { get; }

        /// <summary>Root visual element for this tab — built once, reused on tab switch.</summary>
        VisualElement Root { get; }

        /// <summary>Called when the active config asset changes (including null).</summary>
        void OnConfigChanged(LocalizationConfig config);

        /// <summary>Called when the tab becomes the active one. Use to refresh stale data.</summary>
        void OnSelected();
    }
}
#endif
