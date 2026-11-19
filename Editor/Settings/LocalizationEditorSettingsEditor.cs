#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TRnK.Localization
{
    /// <summary>Read-only inspector for the settings asset; the Localization Manager edits and saves it.</summary>
    [CustomEditor(typeof(LocalizationEditorSettings))]
    internal sealed class LocalizationEditorSettingsEditor : Editor
    {
        private const string EditHint = "Edit these settings in Tools > TRnK > Localization Manager > Import.";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(EditHint, MessageType.Info);

            using (new EditorGUI.DisabledScope(true))
            {
                DrawProp("_spreadsheetUrl", "Spreadsheet URL");
                DrawProp("_tablesFolder", "Tables Folder");
                DrawProp("_ignoredTabs", "Ignored Sheet Tabs");
            }
        }

        private void DrawProp(string propertyName, string label)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
                EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }
    }
}
#endif
