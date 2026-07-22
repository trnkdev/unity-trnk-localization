#if !ODIN_INSPECTOR
using UnityEditor;
using UnityEngine;

namespace TRnK.Localization
{
    [CustomPropertyDrawer(typeof(LocalizedString))]
    internal sealed class LocalizedStringDrawer : PropertyDrawer
    {
        const float Gap = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                var row = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

                var tableProp = property.FindPropertyRelative("_table");
                var keyProp   = property.FindPropertyRelative("_key");

                float half = (row.width - Gap) * 0.5f;
                var tableRect = new Rect(row.x,              row.y, half, row.height);
                var keyRect   = new Rect(row.x + half + Gap, row.y, half, row.height);

                EditorGUI.PropertyField(tableRect, tableProp, GUIContent.none);
                EditorGUI.PropertyField(keyRect,   keyProp,   GUIContent.none);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;
    }
}
#endif
