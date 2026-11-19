#if UNITY_EDITOR
using System.Globalization;
using UnityEditor;
using UnityEngine.UIElements;

namespace TRnK.Localization
{
    /// <summary>Inspector for table assets: a summary instead of every cell, with a link to the Tables tab.</summary>
    [CustomEditor(typeof(LocalizationTable))]
    internal sealed class LocalizationTableEditor : Editor
    {
        private const string OpenLabel = "Open in Localization Manager";
        private const string ListedHint = "Imported from the spreadsheet. View its text in the Localization Manager.";
        private const string NotListedHint =
            "This table is not in the Tables Folder of the config selected in the Localization Manager.";
        private const float Spacing = 6f;

        public override VisualElement CreateInspectorGUI()
        {
            var table = (LocalizationTable)target;
            var root = new VisualElement();

            root.Add(ReadOnlyField("Table Name", table.TableName));
            root.Add(ReadOnlyField("Keys", table.Keys.Count.ToString(CultureInfo.InvariantCulture)));
            root.Add(ReadOnlyField("Languages", string.Join(", ", table.Locales)));

            bool listed = IsListed(table);

            var hint = new HelpBox(listed ? ListedHint : NotListedHint, HelpBoxMessageType.Info);
            hint.style.marginTop = Spacing;
            root.Add(hint);

            var open = new Button(() => LocalizationEditorWindow.ShowTable(table)) { text = OpenLabel };
            open.style.marginTop = Spacing;
            open.SetEnabled(listed);
            root.Add(open);

            return root;
        }

        private static TextField ReadOnlyField(string label, string value)
            => new(label) { value = value ?? string.Empty, isReadOnly = true };

        private static bool IsListed(LocalizationTable table)
        {
            foreach (var listed in TableAssets.For(LocalizationEditorSettings.GetOrCreate().ActiveConfig))
                if (listed == table) return true;
            return false;
        }
    }
}
#endif
