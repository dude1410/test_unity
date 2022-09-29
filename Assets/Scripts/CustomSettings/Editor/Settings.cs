using System;
using System.Reflection;
using UnityEngine;

namespace TBR.Utils.CustomSettings.Editor
{
    
    /// <summary>
    /// Shows the Scriptable Manager in the appropriate Unity settings menu,
    /// Projects Settings Menu for Project scoped Managers,
    /// Preferences Menu for User scoped Managers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class SettingsMenuAttribute : Attribute
    {
        public string Path { get; }
        public bool Root { get; }

        public SettingsMenuAttribute(string path) : this(path, false) {}
        public SettingsMenuAttribute(string path, bool root)
        {
            this.Path = path;
            this.Root = root;
        }

        public static SettingsMenuAttribute Retrieve(Type type)
        {
            return type.GetCustomAttribute<SettingsMenuAttribute>();
        }
    }

    
    public class Settings : ScriptableObject
    {
        /// <summary>
        /// Dynamically evaluated property to determine whether to include this manager in build or not
        /// </summary>
        protected internal virtual bool IncludeInBuild => true;

        protected virtual void OnEnable()
        {
        }

        /// <summary>
        /// Load method invoked when the Scriptable Manager is loaded in memory
        /// </summary>
        protected virtual void Load()
        {
        }
    }

    public class Settings<T> : Settings
        where T : Settings<T>
    {
        private static T instance;

        public static T Instance
        {
            get
            {
#if UNITY_EDITOR
                if (instance == null)
                {
                    var type = typeof(T);
                    instance = CustomSettings.Retrieve(type) as T;
                }
#endif

                return instance;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            instance = this as T;

            Load();
        }
    }
}