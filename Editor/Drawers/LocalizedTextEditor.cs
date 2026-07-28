#if UNITY_EDITOR
using System.Globalization;
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
        private const string LanguageLabel = "Selected Language";
        private const string LanguageTooltip = "Language shown in the Scene view. Editor preview only — the game sets its own locale at runtime.";

        private SerializedProperty _tableProp;
        private SerializedProperty _keyProp;
        private SerializedProperty _previewProp;

        private LocalizedKeyResult _result;

        private void OnEnable()
        {
            var localizedProp = serializedObject.FindProperty("_localized");
            _tableProp = localizedProp.FindPropertyRelative("_table");
            _keyProp = localizedProp.FindPropertyRelative("_key");
            _previewProp = serializedObject.FindProperty("_previewLocale");

            // Show the current state on selection rather than an empty box
            RunValidation();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();
            DrawLanguageSelector();
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

        private void DrawLanguageSelector()
        {
            if (_previewProp == null) return;

            var config = LocalizationEditorSettings.GetOrCreate().ActiveConfig;
            if (config == null || config.Locales.Count == 0) return;

            var labels = new GUIContent[config.Locales.Count];
            int current = -1;

            for (int i = 0; i < config.Locales.Count; i++)
            {
                var locale = config.Locales[i];
                labels[i] = new GUIContent($"[{locale.Code}] {LocaleDisplayName(locale)}");

                if (locale.Code == _previewProp.stringValue) current = i;
            }

            // Unset, or a locale the sheet no longer has — fall back to the default locale
            if (current < 0)
            {
                current = DefaultLocaleIndex(config);
                _previewProp.stringValue = config.Locales[current].Code;
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup(
                new GUIContent(LanguageLabel, LanguageTooltip), current, labels);

            if (!EditorGUI.EndChangeCheck()) return;

            _previewProp.stringValue = config.Locales[selected].Code;
            serializedObject.ApplyModifiedProperties();
            RefreshPreview();
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
                RefreshPreview();

            Repaint();
        }

        private void RefreshPreview()
        {
            if (serializedObject.isEditingMultipleObjects) return;
            LocalizedKeyValidator.RefreshText(target as LocalizedText, _previewProp?.stringValue);
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

        private static int DefaultLocaleIndex(LocalizationConfig config)
        {
            for (int i = 0; i < config.Locales.Count; i++)
                if (config.Locales[i].Code == config.DefaultLocale) return i;
            return 0;
        }

        // Prefers the authored name, then the culture's English name, then the raw code
        private static string LocaleDisplayName(Locale locale)
        {
            if (!string.IsNullOrEmpty(locale.Name) && locale.Name != locale.Code)
                return locale.Name;

            try
            {
                return CultureInfo.GetCultureInfo(locale.Code).EnglishName;
            }
            catch (CultureNotFoundException)
            {
                return locale.Code;
            }
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
