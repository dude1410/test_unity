using System;
using System.Collections.Generic;
using TBR.Utils.CustomSettings.Editor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace TBR.HeadlessServer
{
    [Serializable]
    public class AddressablesExcludesData
    {
        [Header("Addressables Remove List")]
        [SerializeField]
        private List<AddressableAssetGroup> addressables = new List<AddressableAssetGroup>(10);

        public ReadOnlyList<AddressableAssetGroup> Addressables => addressables;
    }

    [SettingsMenu("Voca Games/Headless/Addressables Remove List", false)]
    public class AddressablesExcludeSettings : Settings<AddressablesExcludeSettings>
    {
        [SerializeField]
        private AddressablesExcludesData settings;

        public ReadOnlyList<AddressableAssetGroup> List => settings.Addressables;
    }
}