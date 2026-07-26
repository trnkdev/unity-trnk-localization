using TMPro;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TRnK.Localization
{
    [AddComponentMenu("TRnK Localization/Localized Text")]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText :
#if ODIN_INSPECTOR
        SerializedMonoBehaviour
#else
        MonoBehaviour
#endif
    {
#if ODIN_INSPECTOR
        [InlineProperty, HideLabel]
#endif
        [SerializeField] private LocalizedString _localized = new();

        private TMP_Text _text;

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            Loc.LocaleChanged += OnLocaleChanged;
            Refresh();
        }

        private void OnDisable() => Loc.LocaleChanged -= OnLocaleChanged;

        public void Refresh()
        {
            if (_text == null || !Loc.IsReady) return;
            _text.text = _localized.Get();
        }

        private void OnLocaleChanged(string _) => Refresh();

#if UNITY_EDITOR
        /// <summary>Editor-only. Refreshes this text from the active Edit-Mode preview locale.</summary>
        internal void RefreshEditorPreview()
        {
            // _text is unset in Edit Mode (Awake has not run)
            if (!TryGetComponent<TMP_Text>(out var text)) return;
            text.text = _localized.Get();
        }
#endif
    }
}
