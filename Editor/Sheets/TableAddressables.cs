#if UNITY_EDITOR && TRNK_LOCALIZATION_ADDRESSABLES
using System;
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

            var entry = settings.CreateOrMoveEntry(GuidOf(table), FindOrCreateGroup(settings));
            entry.address = LocalizationService.AddressOf(table.TableName);
        }

        /// <summary>Lowercases an existing table's address if it still uses ours in another case; any other address is left alone.</summary>
        internal static void UpdateAddress(LocalizationTable table)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var entry = settings != null ? settings.FindAssetEntry(GuidOf(table)) : null;
            if (entry == null) return;

            string address = LocalizationService.AddressOf(table.TableName);
            if (entry.address != address && string.Equals(entry.address, address, StringComparison.OrdinalIgnoreCase))
                entry.address = address;
        }

        private static string GuidOf(LocalizationTable table)
            => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(table));

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
