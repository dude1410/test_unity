using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditorInternal;
#endif

namespace TBR.Utils.CustomSettings.Editor
{
    //Source https://pastebin.com/W8Q12q7K
    public static class CustomSettings
    {
        private static readonly Dictionary<Type, Settings> dictionary;

        public static string FormatPath(Type type)
        {
            var name = FormatID(type);

            var directory = FormatPathDirectory(SettingsScope.Project);
            Directory.CreateDirectory(directory);

            return Path.Combine(directory, $"{name}.asset");
        }
        
        public static string FormatID(Type type)
        {
            return type.Name;
        }
        

        public static string FormatPathDirectory(SettingsScope scope)
        {
            switch (scope)
            {
                case SettingsScope.Project:
                    return "ProjectSettings/VocaGames/";

                case SettingsScope.User:
                    var parent = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    return Path.Combine(parent,
                        $"Unity/Editor-5.x/Preferences/MB/{InternalEditorUtility.GetUnityDisplayVersion()}/");
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Saves a settings object to disk
        /// </summary>
        /// <param name="asset"></param>
        public static void Save(Settings asset)
        {
            var type = asset.GetType();
            var path = FormatPath(type);

            dictionary[type] = asset;

            var array = new Object[] { asset };
            InternalEditorUtility.SaveToSerializedFileAndForget(array, path, true);
        }

        /// <summary>
        /// Loads a settings object from disk or from memory if already loaded,
        /// guaranteed to return the same instance within the same assembly reload
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Settings Load(Type type)
        {
            if (dictionary.TryGetValue(type, out var asset) && asset != null)
                return asset;

            var path = FormatPath(type);

            asset = InternalEditorUtility.LoadSerializedFileAndForget(path).FirstOrDefault() as Settings;
            if (asset == null) return null;

            dictionary[type] = asset;
            Setup(asset);

            return asset;
        }

        /// <summary>
        /// Creates a settings object instance and saves it
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Settings Create(Type type)
        {
            if (type.IsAbstract || type.IsGenericType)
                return null;
            var asset = ScriptableObject.CreateInstance(type) as Settings;

            asset.name = FormatID(type);

            dictionary[type] = asset;
            Setup(asset);
            Save(asset);

            return asset;
        }

        /// <summary>
        /// Retrieves an instance of a settings object whether by loading it or creating
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Settings Retrieve(Type type)
        {
            var asset = Load(type);

            if (asset == null) asset = Create(type);

            return asset;
        }

        public static Settings Reset(Type type)
        {
            Destroy(type);
            return Create(type);
        }

        public static Settings Reload(Type type)
        {
            Destroy(type);
            return Retrieve(type);
        }

        public static bool Destroy(Type type)
        {
            if (dictionary.TryGetValue(type, out var asset) == false)
                return false;

            Object.DestroyImmediate(asset);

            return true;
        }

        private static void Setup(Settings asset)
        {
            asset.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
        }

        private static void PreAssemblyReloadCallback()
        {
            //Destroy assets before we lose reference to it
            foreach (var asset in dictionary.Values)
                Object.DestroyImmediate(asset);

            dictionary.Clear();
        }

        static CustomSettings()
        {
            dictionary = new Dictionary<Type, Settings>();
            AssemblyReloadEvents.beforeAssemblyReload += PreAssemblyReloadCallback;
        }
    }
}