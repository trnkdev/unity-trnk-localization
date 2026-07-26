#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TRnK.Localization
{
    /// <summary>Inspector for LocalizedText: stacked Table/Key fields with an explicit Validate action and result feedback.</summary>
    [CustomEditor(typeof(LocalizedText))]
    [CanEditMultipleObjects]
    internal sealed class LocalizedTextEditor : Editor
    {
        private const float RowPadding = 4f;
        private const float CornerRadius = 4f;
        private const float BorderWidth = 1f;
        private const float ValidateButtonWidth = 86f;
        private const string ValidateLabel = "Validate Key";
        private const string ValidateTooltip = "Check the Table and Key against the config selected in the Localization Manager.";

        private SerializedProperty _tableProp;
        private SerializedProperty _keyProp;

        private LocalizedKeyResult _result;

        private void OnEnable()
        {
            var localizedProp = serializedObject.FindProperty("_localized");
            _tableProp = localizedProp.FindPropertyRelative("_table");
            _keyProp = localizedProp.FindPropertyRelative("_key");

            // Show the current state on selection rather than an empty box
            RunValidation();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();
            EditorGUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();
            DrawFields();
            bool edited = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            // Editing invalidates the previous verdict — the user re-validates when ready
            if (edited)
                _result = default;

            DrawMessage();
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }

        private void DrawFields()
        {
            bool noConfig = LocalizationEditorSettings.GetOrCreate().ActiveConfig == null;

            var background = noConfig
                ? LocalizedKeyValidator.ErrorBackground
                : _result.State switch
                {
                    LocalizedKeyState.Valid => LocalizedKeyValidator.ValidBackground,
                    LocalizedKeyState.Missing => LocalizedKeyValidator.ErrorBackground,
                    _ => Color.clear
                };

            var border = noConfig
                ? LocalizedKeyValidator.ErrorColor
                : _result.State switch
                {
                    LocalizedKeyState.Valid => LocalizedKeyValidator.ValidColor,
                    LocalizedKeyState.Missing => LocalizedKeyValidator.ErrorColor,
                    _ => Color.clear
                };

            var area = EditorGUILayout.BeginVertical();
            if (background != Color.clear)
                DrawRoundedBox(Inflate(area), background, border);

            GUILayout.Space(RowPadding);
            EditorGUILayout.PropertyField(_tableProp, new GUIContent("Table"));
            DrawKeyRow();
            GUILayout.Space(RowPadding);

            EditorGUILayout.EndVertical();
        }

        private void DrawKeyRow()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_keyProp, new GUIContent("Key"));

                // Multi-edit verdicts would be ambiguous across differing values
                using (new EditorGUI.DisabledScope(serializedObject.isEditingMultipleObjects))
                {
                    if (GUILayout.Button(new GUIContent(ValidateLabel, ValidateTooltip), GUILayout.Width(ValidateButtonWidth)))
                        Validate();
                }
            }
        }

        private void DrawMessage()
        {
            // Missing localization data is a project-level problem — always surfaced, click or not
            if (LocalizationEditorSettings.GetOrCreate().ActiveConfig == null)
            {
                EditorGUILayout.HelpBox(LocalizedKeyValidator.NoConfigMessage, MessageType.Warning);
                return;
            }

            if (_result.State == LocalizedKeyState.NotValidated) return;

            EditorGUILayout.HelpBox(_result.Message,
                _result.IsValid ? MessageType.Info : MessageType.Error);
        }

        private void Validate()
        {
            RunValidation();

            if (_result.IsValid)
                LocalizedKeyValidator.RefreshText(target as LocalizedText);

            Repaint();
        }

        private void RunValidation()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                _result = default;
                return;
            }

            _result = LocalizedKeyValidator.Validate(_tableProp.stringValue, _keyProp.stringValue);
        }

        // EditorGUILayout rects stop at the content edge; widen them to cover the row's padding
        private static Rect Inflate(Rect rect)
            => new(rect.x - RowPadding, rect.y, rect.width + RowPadding * 2f, rect.height);

        // DrawTexture is the only built-in that supports corner radii; Repaint-only, per IMGUI rules
        private static void DrawRoundedBox(Rect rect, Color fill, Color border)
        {
            if (Event.current.type != EventType.Repaint) return;

            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill,
                alphaBlend: true, imageAspect: 0f,
                color: fill, borderWidth: 0f, borderRadius: CornerRadius);

            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill,
                alphaBlend: true, imageAspect: 0f,
                color: border, borderWidth: BorderWidth, borderRadius: CornerRadius);
        }
    }
}
#endif
