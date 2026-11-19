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

        /// <summary>Sets the TMP text to the localized value, formatting its positional {0} placeholder without string allocation.</summary>
        public static void SetLocalizedText(this TMP_Text text, string tableName, string key, float arg0)
        {
            if (text == null) return;
            text.SetText(Loc.Get(tableName, key), arg0);
        }

        /// <summary>Sets the TMP text to the localized value, formatting its positional {0}/{1} placeholders without string allocation.</summary>
        public static void SetLocalizedText(this TMP_Text text, string tableName, string key, float arg0, float arg1)
        {
            if (text == null) return;
            text.SetText(Loc.Get(tableName, key), arg0, arg1);
        }

        /// <summary>Sets the TMP text to the localized value, formatting its positional {0}/{1}/{2} placeholders without string allocation.</summary>
        public static void SetLocalizedText(this TMP_Text text, string tableName, string key, float arg0, float arg1, float arg2)
        {
            if (text == null) return;
            text.SetText(Loc.Get(tableName, key), arg0, arg1, arg2);
        }

        /// <summary>Sets the TMP text to the localized value with {0} replaced by the argument.</summary>
        public static void SetLocalizedText(this TMP_Text text, string tableName, string key, string arg0)
        {
            if (text == null) return;
            text.text = Loc.Get(tableName, key, arg0);
        }

        /// <summary>Sets the TMP text to the localized value with {0}–{1} replaced by the arguments.</summary>
        public static void SetLocalizedText(this TMP_Text text, string tableName, string key, string arg0, string arg1)
        {
            if (text == null) return;
            text.text = Loc.Get(tableName, key, arg0, arg1);
        }

        /// <summary>Sets the TMP text to the localized value with {0}–{2} replaced by the arguments.</summary>
        public static void SetLocalizedText(this TMP_Text text, string tableName, string key, string arg0, string arg1,
                                            string arg2)
        {
            if (text == null) return;
            text.text = Loc.Get(tableName, key, arg0, arg1, arg2);
        }

        /// <summary>Sets the TMP text to the localized value with {0}–{3} replaced by the arguments.</summary>
        public static void SetLocalizedText(this TMP_Text text, string tableName, string key, string arg0, string arg1,
                                            string arg2, string arg3)
        {
            if (text == null) return;
            text.text = Loc.Get(tableName, key, arg0, arg1, arg2, arg3);
        }

        /// <summary>Sets the TMP text to the localized value with {0}–{4} replaced by the arguments.</summary>
        public static void SetLocalizedText(this TMP_Text text, string tableName, string key, string arg0, string arg1,
                                            string arg2, string arg3, string arg4)
        {
            if (text == null) return;
            text.text = Loc.Get(tableName, key, arg0, arg1, arg2, arg3, arg4);
        }

        /// <summary>Sets the TMP text to the localized value with {0}–{5} replaced by the arguments.</summary>
        public static void SetLocalizedText(this TMP_Text text, string tableName, string key, string arg0, string arg1,
                                            string arg2, string arg3, string arg4, string arg5)
        {
            if (text == null) return;
            text.text = Loc.Get(tableName, key, arg0, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>Sets the TMP text to the localized value with {0}–{6} replaced by the arguments.</summary>
        public static void SetLocalizedText(this TMP_Text text, string tableName, string key, string arg0, string arg1,
                                            string arg2, string arg3, string arg4, string arg5, string arg6)
        {
            if (text == null) return;
            text.text = Loc.Get(tableName, key, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>Sets the TMP text to the localized value with {0}–{7} replaced by the arguments.</summary>
        public static void SetLocalizedText(this TMP_Text text, string tableName, string key, string arg0, string arg1,
                                            string arg2, string arg3, string arg4, string arg5, string arg6, string arg7)
        {
            if (text == null) return;
            text.text = Loc.Get(tableName, key, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
    }
}
