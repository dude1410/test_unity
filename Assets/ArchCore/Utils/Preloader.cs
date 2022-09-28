using UnityEngine;
using UnityEditor;
using System.Collections;

namespace ArchCore.Utils
{

    static class Preloader
    {
        private const string RUNTIME_PRELOAD_ASSETS_PATH = "RUNTIME_PRELOAD_ASSETS";
        private const string EDITOR_PRELOAD_ASSETS_PATH = "EDITOR_PRELOAD_ASSETS";
        
        
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void PreloadEditor()
        {
            Resources.Load<AssetContainer>(EDITOR_PRELOAD_ASSETS_PATH);
        }
#endif
        [RuntimeInitializeOnLoadMethod]
        static void PreloadRuntime()
        {
            Resources.Load<AssetContainer>(RUNTIME_PRELOAD_ASSETS_PATH);
        }
    }
}
