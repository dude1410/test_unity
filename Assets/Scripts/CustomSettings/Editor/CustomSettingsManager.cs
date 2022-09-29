using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TBR.Utils.CustomSettings.Editor
{
    public static class CustomSettingsManager
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnLoad()
        {
            //Manually Load all Scriptable Settings on Runtime
            LoadAll();
        }

        /// <summary>
        /// Manually Loads all managers
        /// </summary>
        public static void LoadAll()
        {
            foreach (var type in IterateAll())
                CustomSettings.Retrieve(type);
        }

        public static IEnumerable<Type> IterateAll()
        {
            //var types = TypeCache.GetTypesWithAttribute<ScriptableManager.GlobalAttribute>();
            var types = typeof(Settings).GetDerivedTypesInAllAssemblies();

            for (var i = 0; i < types.Length; i++)
            {
                if (typeof(Settings).IsAssignableFrom(types[i]) == false)
                {
                    Debug.LogWarning(
                        $"Type {types[i]} Needs to Inherit from {typeof(Settings)} to Accept the Attribute");
                    continue;
                }

                yield return types[i];
            }
        }

        public static Type[] GetDerivedTypesInAllAssemblies(this Type baseType, bool includeAbstract = false)
        {
            var allTypesChached = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes());
            return allTypesChached.Where(pp =>
                    baseType.IsAssignableFrom(pp) && !pp.IsInterface && pp.IsClass &&
                    (!includeAbstract || !pp.IsAbstract))
                .ToArray();
        }
    }
}