using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ArchCore.MVP
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AutoRegisterViewAttribute : Attribute
    {
        private readonly string[] sceneBindings;
        private readonly string customPath;

        public AutoRegisterViewAttribute(string customPath = null, params string[] sceneBindings)
        {
            this.sceneBindings = sceneBindings;
            this.customPath = customPath;
        }

        public AutoRegisterViewAttribute()
        {
            this.sceneBindings = new string[0];
            this.customPath = null;
        }

        public static IEnumerable<(Type view, string path)> GetViews(Assembly[] assemblies, string specificScene = null)
        {

             return assemblies.Select(a => a.GetTypes()
                .Where(type =>
                {
                    var attr = type.GetCustomAttributes(typeof(AutoRegisterViewAttribute), true);
                    return attr.Length > 0; // && ((AutoRegisterViewAttribute)attr[0]).target == viewManager;
                })
                .Where(t => t != typeof(View) && typeof(View).IsAssignableFrom(t))
                .Where(t =>
                {
                    string[] bindings = ((AutoRegisterViewAttribute) t
                            .GetCustomAttributes(typeof(AutoRegisterViewAttribute), true)[0])
                        .sceneBindings;
                    return (bindings.Length == 0 && specificScene == null) || bindings.Contains(specificScene);
                })
                .Select(t => (t, ((AutoRegisterViewAttribute) t
                                     .GetCustomAttributes(typeof(AutoRegisterViewAttribute),
                                         true)[0])
                                 .customPath ?? string.Format(View.StandardPathFormat, t.Name)))).SelectMany(array => array);

        }

    }

}