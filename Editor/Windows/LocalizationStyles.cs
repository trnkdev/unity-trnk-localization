#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    /// <summary>Shared colors and small UI parts used across the Localization Manager tabs.</summary>
    internal static class LocalizationStyles
    {
        // Matches the LocalizedText inspector — #4FC354 / #D93638
        internal static readonly Color Valid = new(0.310f, 0.765f, 0.329f);
        internal static readonly Color Error = new(0.851f, 0.212f, 0.220f);
        internal static readonly Color Warning = new(0.900f, 0.750f, 0.300f);

        internal static readonly Color Added = Valid;
        internal static readonly Color Updated = Warning;
        internal static readonly Color Removed = Error;

        /// <summary>Muted secondary text, matching Unity's own hint styling.</summary>
        internal static Label Hint(string text)
        {
            var label = new Label(text);
            label.AddToClassList("hint-text");
            return label;
        }

        internal static Label Header(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 13;
            label.style.marginBottom = 4;
            return label;
        }

        internal static VisualElement Divider()
        {
            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.marginTop = 10;
            divider.style.marginBottom = 10;
            divider.style.backgroundColor = new Color(0, 0, 0, 0.2f);
            return divider;
        }
    }
}
#endif
