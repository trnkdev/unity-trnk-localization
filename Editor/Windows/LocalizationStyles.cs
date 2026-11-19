#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    /// <summary>Shared colors and small UI parts used across the Localization Manager tabs.</summary>
    internal static class LocalizationStyles
    {
        /// <summary>Vertical rule between grid columns.</summary>
        internal static readonly Color ColumnDivider = new(0f, 0f, 0f, 0.25f);

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
