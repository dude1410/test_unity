using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using System;
using UnityEngine;
using UnityEditor.AddressableAssets;

namespace TBR.Bundles.Editor
{
    public static class AddressablesBuildUtils
    {
        const string LOG_PREFIX = "[Bundles Build]";

        [MenuItem("Utils/Build addressables")]
        public static bool BuildAddressables()
        {
            Debug.Log($"{LOG_PREFIX} clean content");
            AddressableAssetSettings.CleanPlayerContent();

            Debug.Log($"{LOG_PREFIX} build content");
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            bool success = string.IsNullOrEmpty(result.Error);

            if (!success)
            {
                Debug.LogError($"{LOG_PREFIX} Addressables build error encountered: " + result.Error);
            }
            else
            {
                Debug.Log($"{LOG_PREFIX} build addressables success");
            }
            return success;
        }

        //public static string build_script = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";
        //public static string build_script = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedPlayMode.asset";
        public static string build_script = "Assets/AddressableAssetsData/CustomNameBuildScriptPacked.asset";
        public static string settings_asset = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
        public static string profile_name = "Default";
        private static AddressableAssetSettings settings;

        public static void LoadSettingsDefault()
        {
            settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                Debug.LogError($"{LOG_PREFIX} default settings couldn't be found or isn't a settings object.");
            else
                Debug.Log($"{LOG_PREFIX} settings loadeded={settings}");
        }

        public static void LoadSettings(string settingsAsset)
        {
            settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(settingsAsset) as AddressableAssetSettings;
            if (settings == null)
                Debug.LogError($"{LOG_PREFIX} {settingsAsset} couldn't be found or isn't a settings object.");
            else
                Debug.Log($"{LOG_PREFIX} settings loadeded={settings}. asset={settingsAsset}");
        }

        public static void SetProfile(string profile)
        {
            string profileId = settings.profileSettings.GetProfileId(profile);
            if (string.IsNullOrEmpty(profileId))
            {
                Debug.LogError($"{LOG_PREFIX} couldn't find a profile named, {profile}, using current profile instead.");
            }
            else
            {
                settings.activeProfileId = profileId;
                Debug.Log($"{LOG_PREFIX} set profile={profile}. id={profileId}");
            }
        }

        public static void SetBuilder(IDataBuilder builder)
        {
            int index = settings.DataBuilders.IndexOf((ScriptableObject)builder);
            if (index > 0)
            {
                settings.ActivePlayerDataBuilderIndex = index;
                Debug.Log($"{LOG_PREFIX} set builder={builder}. index={index}");
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX} {builder} must be added to the DataBuilders list before it can be made active. Using last run builder instead.");
            }
        }

        static bool BuildAddressablesFromSettings()
        {
            LoadSettingsDefault();
            //GetSettingsObject(settings_asset);

            SetProfile(profile_name);
            IDataBuilder builderScript = AssetDatabase.LoadAssetAtPath<ScriptableObject>(build_script) as IDataBuilder;

            if (builderScript == null)
            {
                Debug.LogError($"{LOG_PREFIX} {build_script} couldn't be found or isn't a build script.");
                return false;
            }

            SetBuilder(builderScript);

            return BuildAddressables();
        }

        //[MenuItem("Window/Asset Management/Addressables/Build Addressables and Player")]
        static void BuildAddressablesAndPlayer()
        {
            bool contentBuildSucceeded = BuildAddressablesFromSettings();

            if (contentBuildSucceeded)
            {
                var options = new BuildPlayerOptions();
                BuildPlayerOptions playerSettings = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(options);
                BuildPipeline.BuildPlayer(playerSettings);
            }
        }


    }
}