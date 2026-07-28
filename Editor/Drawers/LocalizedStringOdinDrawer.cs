#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace TRnK.Localization
{
    /// <summary>Tints LocalizedString fields by key validity and refreshes edited LocalizedText components.</summary>
    internal sealed class LocalizedStringOdinDrawer : OdinValueDrawer<LocalizedString>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var value = ValueEntry.SmartValue;

            // Tinting is applied only where a validation verdict exists (LocalizedText's inspector)
            bool tint = value != null
                        && Property.Tree.WeakTargets.Count == 1
                        && Property.Tree.WeakTargets[0] is not LocalizedText;

            bool tinted = false;
            if (tint)
            {
                var result = LocalizedKeyValidator.Validate(value.Table, value.Key);
                if (result.IsValid)
                {
                    GUIHelper.PushColor(SirenixGUIStyles.GreenValidColor, false);
                    tinted = true;
                }
                else if (result.State == LocalizedKeyState.Missing)
                {
                    GUIHelper.PushColor(SirenixGUIStyles.RedErrorColor, false);
                    tinted = true;
                }
            }

            CallNextDrawer(label);

            if (tinted) GUIHelper.PopColor();
        }
    }
}
#endif
