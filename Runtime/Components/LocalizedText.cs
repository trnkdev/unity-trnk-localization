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
        [SerializeField] private LocalizedString _localized;

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
    }
}
