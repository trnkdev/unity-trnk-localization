#if UNITY_EDITOR && TRNK_LOCALIZATION_ADDRESSABLES
using TRnK.Logger;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace TRnK.Localization
{
    /// <summary>Makes newly imported tables Addressable at the address the runtime loads them from.</summary>
    internal static class TableAddressables
    {
        private const string GroupName = "Localization";

        /// <summary>Adds a new table to the Localization group.</summary>
        internal static void Mark(LocalizationTable table)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
                Log.Info("Created Addressables settings for the localization tables.");
            }

            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(table));
            var entry = settings.CreateOrMoveEntry(guid, FindOrCreateGroup(settings));
            entry.address = LocalizationService.AddressOf(table.TableName);
        }

        private static AddressableAssetGroup FindOrCreateGroup(AddressableAssetSettings settings)
        {
            var group = settings.FindGroup(GroupName);
            if (group != null) return group;

            group = settings.CreateGroup(GroupName, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            // One bundle per table, so releasing a table frees its memory
            var bundled = group.GetSchema<BundledAssetGroupSchema>();
            bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            EditorUtility.SetDirty(bundled);
            return group;
        }
    }
}
#endif
