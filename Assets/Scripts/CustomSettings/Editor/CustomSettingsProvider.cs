using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TBR.Utils.CustomSettings.Editor
{
    public class CustomSettingsProvider
        : SettingsProvider
    {
        private readonly Type type;
        private Settings asset;
        private UnityEditor.Editor inspector;

        private readonly GenericMenu context;

        private void Validate()
        {
            asset = CustomSettings.Retrieve(type);

            if (inspector == null || inspector.target != asset)
                inspector = UnityEditor.Editor.CreateEditor(asset);
        }

        public override void OnTitleBarGUI()
        {
            base.OnTitleBarGUI();

            var style = (GUIStyle)"MiniPopup";
            var content = EditorGUIUtility.TrIconContent("_Popup");

            if (GUILayout.Button(content, style))
                context.ShowAsContext();
        }

        private void Reset() => CustomSettings.Reset(type);
        private void Reload() => CustomSettings.Reload(type);

        public override void OnGUI(string search)
        {
            base.OnGUI(search);

            Validate();

            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();
            inspector.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                CustomSettings.Save(asset);
            }

            GUI.enabled = true;
        }

        public CustomSettingsProvider(string path, SettingsScope scope, Type type) : base(path, scope)
        {
            this.type = type;

            //Create Generic Menu
            {
                context = new GenericMenu();
                context.AddItem(new GUIContent("Reset"), false, Reset);
                context.AddItem(new GUIContent("Reload"), false, Reload);
            }
        }

        //Static Utility
        [SettingsProviderGroup]
        private static SettingsProvider[] Register()
        {
            var list = new List<SettingsProvider>();

            foreach (var type in CustomSettingsManager.IterateAll())
            {
                var menu = SettingsMenuAttribute.Retrieve(type);
                if (menu == null) continue;

                var provider = Create(type, menu);
                list.Add(provider);
            }

            return list.ToArray();
        }

        public static CustomSettingsProvider Create(Type type, SettingsMenuAttribute menu)
        {
            var path = menu.Root ? menu.Path : PrefixPath(menu.Path, SettingsScope.Project);


            return new CustomSettingsProvider(path, SettingsScope.Project, type);
        }

        public static string PrefixPath(string path, SettingsScope scope)
        {
            switch (scope)
            {
                case SettingsScope.Project:
                    return $"Project/{path}";

                case SettingsScope.User:
                    return $"Preferences/{path}";
            }

            throw new NotImplementedException();
        }
    }
}