using TMPro;

namespace TRnK.Localization
{
    /// <summary>One-shot localized text setters for TMP components — do not auto-refresh on locale change.</summary>
    public static class LocalizedTMPExtensions
    {
        /// <summary>Sets the TMP text to the localized value of (table, key) using the current locale.</summary>
        public static void SetLocalizedText(this TMP_Text text, string tableName, string key)
        {
            if (text == null) return;
            text.text = Loc.Get(tableName, key);
        }

        /// <summary>Sets the TMP text to the localized value of the given <see cref="LocalizedString"/>.</summary>
        public static void SetLocalizedText(this TMP_Text text, LocalizedString reference)
        {
            if (text == null || reference == null) return;
            text.text = reference.Get();
        }
    }
}
