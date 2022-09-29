using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TBR.HeadlessServer
{
	public static class HeadlessEditorHelper
	{
		const string LOG_PREFIX = "[Headless Helper]";
		/// <summary>
		/// in HeadlessBuilder
		/// </summary>
		public static bool EnableCleaningInHeadlessBuilder { get; set; } = true;

		/// <summary>
		/// build addressables before build
		/// </summary>
		public static bool EnableBuildBundlesBeforeCompile { get; set; } = true;

		/// <summary>
		/// WARNING! 'true' only with manual/git backup and checking cleaned prefabs in editor. Must understand what you doing
		/// </summary>
		public static bool DisableRestoreFile => true;

		/// <summary>
		/// WARNING! 'true' only with manual/git backup and checking cleaned prefabs in editor. Must understand what you doing
		/// </summary>
		public static bool DisableBackupFile => true;

		public static bool DisableExtraFiles => true;

		public static bool IsNotBatchMode => !IsBatchMode;

		public static bool IsBatchMode => IsApplicationBatchMode;

		public static bool IsApplicationBatchMode => Application.isBatchMode;

		public static bool IsNoGraphics => UnityEngine.SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

		public static bool IsNoGraphicsId => UnityEngine.SystemInfo.graphicsDeviceID == 0;

		public static BuildOptions GetExtraBuildOptions()
		{
			BuildOptions result = 0;

			//* TESTING ONLY. comment for production
			result |= BuildOptions.Development;
			result |= BuildOptions.ConnectWithProfiler;
			result |= BuildOptions.EnableDeepProfilingSupport;
			//result |= BuildOptions.AllowDebugging;
			//result |= BuildOptions.BuildScriptsOnly;

			//*/
			return result;
		}

		public static void PreprocessDefinesExtra()
		{
			var headlessSettings = HeadlessEditor.LoadSettings(HeadlessProfiles.currentProfile, true);
			var profileName = headlessSettings.profileName;
			var buildTarget = headlessSettings.GetBuildTarget();
			PreprocessDefinesExtra(profileName, buildTarget);

			//change other build targets
			var buildTargets = new List<BuildTarget>()
			{
				BuildTarget.Android,
				BuildTarget.iOS,
				BuildTarget.StandaloneWindows64,
				BuildTarget.StandaloneLinux64,
			};
			if (buildTargets.Contains(buildTarget))
				buildTargets.Remove(buildTarget);

			foreach (var otherBuildTarg in buildTargets)
			{
				PreprocessDefinesExtra(profileName, otherBuildTarg);
			}
		}

		public static void PreprocessDefinesExtra(string profileName, BuildTarget buildTarget, bool isHeadless = true)
		{
			var buildGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
			profileName = profileName.ToUpper();

			var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup);
			var symbols = defines.Split(new String[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList();

			var need = new List<string>() {
				$"HEADLESS",
				$"HEADLESS_{profileName}",
				$"DEDICATED",
			};
			if (HeadlessEditorPrefs.IsMultiplay)
			{
				need.Add("MULTIPLAY");
			}
			else
			{
				
			}
			var removes = new List<string>() 
			{
			"NOT_HEADLESS",
			"NOT_DEDICATED",
			};
			
			if (HeadlessEditorPrefs.IsMultiplay)
			{
				need.Add("MULTIPLAY");
			}
			else
			{
				removes.Add("MULTIPLAY");
			}
			
			if (!isHeadless)
			{
				var tmp = need;
				need = removes;
				removes = tmp;
			}
			

			symbols.RemoveAll(r => removes.Contains(r));
			symbols.AddRange(need);
			defines = string.Join(";", symbols.Distinct());

			PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, defines);
			AssetDatabase.SaveAssets();
			Debug.Log($"{LOG_PREFIX} pre process defines headless={isHeadless} for='{profileName}' target={buildTarget} group={buildGroup} defines={string.Join(",", defines)}");
		}

		internal static void ShowBuildReportSummary(BuildReport report)
		{
			Debug.Log($"{LOG_PREFIX} build summary. result={report.summary.result}");
			Debug.Log($"{LOG_PREFIX} build summary. files:\n {string.Join("\n", report.files)}");
			Debug.Log($"{LOG_PREFIX} build summary. steps:\n {string.Join("\n", report.steps)}");
			Debug.Log($"{LOG_PREFIX} build summary. scenes:\n {string.Join("\n", report.scenesUsingAssets.Select( s => s.ToString() ))}");
			Debug.Log($"{LOG_PREFIX} build summary. packed assets:\n {string.Join("\n", report.packedAssets.Select(s => s.ToString()))}");
		}

		public static void ForceRebuild()
		{
			Debug.Log($"{LOG_PREFIX} force rebuild");
			string[] rebuildSymbols = { "RebuildToggle1", "RebuildToggle2" };
			var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
			var definesStringTemp = definesString;
			if (definesStringTemp.Contains(rebuildSymbols[0]))
			{
				definesStringTemp = definesStringTemp.Replace(rebuildSymbols[0], rebuildSymbols[1]);
			}
			else if (definesStringTemp.Contains(rebuildSymbols[1]))
			{
				definesStringTemp = definesStringTemp.Replace(rebuildSymbols[1], rebuildSymbols[0]);
			}
			else
			{
				definesStringTemp += ";" + rebuildSymbols[0];
			}
			PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, definesStringTemp);
			PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, definesString);
		}

		[MenuItem("Utils/TBR Debug/Force garbage collect")]
		public static void ForceGarbageCollect()
		{
			Debug.Log($"{LOG_PREFIX} force garbage collect");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		public static void ForceCleanup()
		{
			Debug.Log($"{LOG_PREFIX} force cleanup");
			EditorApplication.UnlockReloadAssemblies();
			AssetDatabase.AllowAutoRefresh();
			EditorUtility.UnloadUnusedAssetsImmediate();
			AssetDatabase.Refresh(ImportAssetOptions.Default);
			UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
			ForceRebuild();
			ForceGarbageCollect();
		}
	}
}