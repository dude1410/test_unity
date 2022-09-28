using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArchCore.MVP
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AutoRegisterServiceAttribute : Attribute
    {
        public static IEnumerable<Type> GetServices()
        {
            return null;
        }
    }
}