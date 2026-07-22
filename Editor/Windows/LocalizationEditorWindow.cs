using System.Collections.Generic;
using System.IO;
using TRnK.Logger;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    internal sealed class LocalizationEditorWindow : EditorWindow
    {
        private const string MenuPath = "Tools/TRnK/Localization Manager";
        private const string TitleText = "TRnK Localization";
        private const string LastAssetGuidPref = "TRnK.Localization.LastAssetGuid";

        private ObjectField _settingsField;
        private Label _statusLabel;
        private VisualElement _content;
        private Dictionary<string, Button> _tabButtons;

        private ITab[] _tabs;
        private ITab _activeTab;

        private LocalizationSettings _settings;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            var win = GetWindow<LocalizationEditorWindow>(TitleText);
            win.minSize = new Vector2(720, 420);
            win.Show();
        }

        private void CreateGUI()
        {
            // Resolve UXML and USS paths relative to this script
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var dir = Path.GetDirectoryName(scriptPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir))
            {
                Log.Error("TRnK.Localization: Could not resolve editor window asset path.");
                return;
            }

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dir}/LocalizationEditorWindow.uxml");
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dir}/LocalizationEditorWindow.uss");

            if (uxml == null)
            {
                Log.Error($"TRnK.Localization: Failed to load UXML at '{dir}/LocalizationEditorWindow.uxml'.");
                return;
            }

            uxml.CloneTree(rootVisualElement);
            if (uss != null) rootVisualElement.styleSheets.Add(uss);

            BindUI();
            InitTabs();
            RestoreLastSettings();
        }

        private void BindUI()
        {
            _settingsField = rootVisualElement.Q<ObjectField>("settingsField");
            _settingsField.objectType = typeof(LocalizationSettings);
            _settingsField.RegisterValueChangedCallback(evt =>
                ApplySettings(evt.newValue as LocalizationSettings));

            var locateBtn = rootVisualElement.Q<Button>("locateButton");
            locateBtn.clicked += () =>
            {
                if (_settings != null) EditorGUIUtility.PingObject(_settings);
            };

            _statusLabel = rootVisualElement.Q<Label>("statusLabel");
            _content = rootVisualElement.Q<VisualElement>("content");

            _tabButtons = new Dictionary<string, Button>
            {
                { "Tables",          rootVisualElement.Q<Button>("tab-tables") },
                { "Locales",         rootVisualElement.Q<Button>("tab-locales") },
                { "Import / Export", rootVisualElement.Q<Button>("tab-importExport") },
                { "Validation",      rootVisualElement.Q<Button>("tab-validation") },
            };
        }

        private void InitTabs()
        {
            _tabs = new ITab[]
            {
                new TablesTab(),
                new LocalesTab(),
                new ImportExportTab(),
                new ValidationTab(),
            };

            foreach (var tab in _tabs)
            {
                if (_tabButtons.TryGetValue(tab.Title, out var btn))
                {
                    var captured = tab; // Avoid closure-on-loop-variable issue
                    btn.clicked += () => SelectTab(captured);
                }
            }

            SelectTab(_tabs[0]);
        }

        private void SelectTab(ITab tab)
        {
            if (tab == null) return;

            _activeTab = tab;

            // Update button styles
            foreach (var (title, btn) in _tabButtons)
            {
                if (string.Equals(title, tab.Title, System.StringComparison.Ordinal))
                    btn.AddToClassList("tab-button--active");
                else
                    btn.RemoveFromClassList("tab-button--active");
            }

            // Swap content
            _content.Clear();
            _content.Add(tab.Root);

            tab.OnSelected();
        }

        private void ApplySettings(LocalizationSettings settings)
        {
            _settings = settings;

            if (settings != null)
            {
                var path = AssetDatabase.GetAssetPath(settings);
                var guid = AssetDatabase.AssetPathToGUID(path);
                EditorPrefs.SetString(LastAssetGuidPref, guid);
            }
            else
            {
                EditorPrefs.DeleteKey(LastAssetGuidPref);
            }

            // Sync the field UI without re-firing the change callback
            if (_settingsField != null && _settingsField.value != settings)
                _settingsField.SetValueWithoutNotify(settings);

            UpdateStatus();
            BroadcastToTabs();
        }

        private void RestoreLastSettings()
        {
            var guid = EditorPrefs.GetString(LastAssetGuidPref, string.Empty);
            if (string.IsNullOrEmpty(guid)) { UpdateStatus(); return; }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) { UpdateStatus(); return; }

            var asset = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(path);
            ApplySettings(asset);
        }

        private void BroadcastToTabs()
        {
            if (_tabs == null) return;
            foreach (var tab in _tabs) tab.OnSettingsChanged(_settings);
        }

        private void UpdateStatus()
        {
            if (_statusLabel == null) return;

            if (_settings == null)
            {
                _statusLabel.text = "No settings selected";
                return;
            }

            int locales = _settings.Locales?.Count ?? 0;
            int tables = _settings.Tables?.Count ?? 0;
            int keys = 0;
            foreach (var t in _settings.Tables) keys += t.Entries?.Count ?? 0;

            _statusLabel.text = $"{locales} locales · {tables} tables · {keys} keys";
        }
    }
}
