using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEditor;
using System;

public class Dump
{
    [MenuItem("Tools/Dump Search Paths")]
    public static void DumpSearchPaths()
    {
        var editorAssembly = typeof(MenuItem).Assembly;
        var mlh = editorAssembly.GetType("UnityEditor.Scripting.ScriptCompilation.MonoLibraryHelpers");
        Debug.Log($"mlh = {mlh != null} Assembly = {editorAssembly.Location}");
        var gsrd = mlh.GetMethod("GetSystemReferenceDirectories", BindingFlags.Static  | BindingFlags.Public, null, new [] { typeof(UnityEditor.ApiCompatibilityLevel) }, null);

        var del= (Func<UnityEditor.ApiCompatibilityLevel, string[]>) gsrd.CreateDelegate(typeof(Func<UnityEditor.ApiCompatibilityLevel, string[]>));

        var ebs = typeof(EditorUserBuildSettings);
        var abtg  = ebs.GetProperty("activeBuildTargetGroup", BindingFlags.Static  | BindingFlags.NonPublic);

        var btg = (BuildTargetGroup) abtg.GetValue(null);
        var paths = del(PlayerSettings.GetApiCompatibilityLevel(btg));
        Debug.Log($"Search Paths:\n------------------------------------------------------------\n{string.Join("\n", paths)}");
    }
}