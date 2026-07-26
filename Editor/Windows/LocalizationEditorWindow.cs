#if UNITY_EDITOR
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
        private const string PreviewOff = "Off";

        private ObjectField _configField;
        private DropdownField _previewField;
        private Label _statusLabel;
        private VisualElement _content;
        private Dictionary<string, Button> _tabButtons;

        private ITab[] _tabs;
        private ITab _activeTab;

        private LocalizationConfig _config;

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
            UpdatePreviewChoices();
        }

        private void BindUI()
        {
            _configField = rootVisualElement.Q<ObjectField>("settingsField");
            _configField.objectType = typeof(LocalizationConfig);
            _configField.RegisterValueChangedCallback(evt =>
                ApplyConfig(evt.newValue as LocalizationConfig));

            var locateBtn = rootVisualElement.Q<Button>("locateButton");
            locateBtn.clicked += () =>
            {
                if (_config != null) EditorGUIUtility.PingObject(_config);
            };

            _previewField = rootVisualElement.Q<DropdownField>("previewLocaleField");
            _previewField.style.minWidth = 90;
            _previewField.RegisterValueChangedCallback(evt => OnPreviewLocaleChanged(evt.newValue));

            _statusLabel = rootVisualElement.Q<Label>("statusLabel");
            _content = rootVisualElement.Q<VisualElement>("content");

            _tabButtons = new Dictionary<string, Button>
            {
                { "Sync",       rootVisualElement.Q<Button>("tab-sync") },
                { "Tables",     rootVisualElement.Q<Button>("tab-tables") },
                { "Validation", rootVisualElement.Q<Button>("tab-validation") },
            };
        }

        private void InitTabs()
        {
            _tabs = new ITab[]
            {
                new SyncTab(),
                new TablesTab(),
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

        private void ApplyConfig(LocalizationConfig config)
        {
            // Swapping the config while previewing would leave a stale override active
            LocalePreview.Clear(_config);

            _config = config;

            LocalizationEditorSettings.GetOrCreate().ActiveConfig = config;

            // Sync the field UI without re-firing the change callback
            if (_configField != null && _configField.value != config)
                _configField.SetValueWithoutNotify(config);

            UpdateStatus();
            UpdatePreviewChoices();
            BroadcastToTabs();
        }

        private void OnDisable()
        {
            // Closing the window (or a domain reload) always ends the preview
            LocalePreview.Clear(_config);
        }

        private void UpdatePreviewChoices()
        {
            if (_previewField == null) return;

            var choices = new List<string> { PreviewOff };
            if (_config != null)
            {
                foreach (var locale in _config.Locales)
                    if (!string.IsNullOrWhiteSpace(locale.Code))
                        choices.Add(locale.Code);
            }

            _previewField.choices = choices;
            _previewField.SetEnabled(_config != null);

            var active = LocalePreview.ActiveLocale;
            _previewField.SetValueWithoutNotify(
                active != null && choices.Contains(active) ? active : PreviewOff);
        }

        private void OnPreviewLocaleChanged(string choice)
        {
            if (string.IsNullOrEmpty(choice) || string.Equals(choice, PreviewOff, System.StringComparison.Ordinal))
                LocalePreview.Clear(_config);
            else
                LocalePreview.Apply(_config, choice);
        }

        private void RestoreLastSettings()
        {
            var config = LocalizationEditorSettings.GetOrCreate().ActiveConfig;
            if (config == null) { UpdateStatus(); return; }

            ApplyConfig(config);
        }

        private void BroadcastToTabs()
        {
            if (_tabs == null) return;
            foreach (var tab in _tabs) tab.OnConfigChanged(_config);
        }

        private void UpdateStatus()
        {
            if (_statusLabel == null) return;

            if (_config == null)
            {
                _statusLabel.text = "No config selected";
                return;
            }

            int locales = _config.Locales?.Count ?? 0;
            int tables = _config.Tables?.Count ?? 0;
            int keys = 0;
            foreach (var t in _config.Tables) keys += t.Entries?.Count ?? 0;

            _statusLabel.text = $"{locales} locales · {tables} tables · {keys} keys";
        }
    }
}
#endif
