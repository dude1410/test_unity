using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using System.Threading.Tasks;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;


namespace TBR.HeadlessServer
{
	public class HeadlessPackageManager : EditorWindow
	{
		const string LogPrefix = "[Headless Package Manager]";
		static string[] disabledPackages = { };

		static Dictionary<string, bool> packages = new Dictionary<string, bool>();

		[MenuItem("Tools/Headless Builder Utility", priority = 150)]
		public static void ShowWindow()
		{
			LoadManifest();
			var window = GetWindow<HeadlessPackageManager>("Headless Builder Utility");
		}

		private void OnEnable()
		{
			HeadlessEditorPrefs.IsDevBuild = HeadlessEditorPrefs.IsDevBuild;
		}

		private static JObject GetManifest()
		{
			var manifestJson = File.ReadAllText(ManifestFile);
			var manifest = JObject.Parse(manifestJson);
			return manifest;
		}

		private static void LoadManifest()
		{
			LoadManifestBlocked();
			var manifest = GetManifest();
			var dependencies = manifest["dependencies"].Children().ToList();
			foreach (var token in dependencies)
			{
				if (token.Type == JTokenType.Property)
				{
					var tokenProp = token.ToObject<JProperty>();
					var packageName = tokenProp.Name;
					var packageDisabled = disabledPackages.Contains(packageName);
					packages[packageName] = !packageDisabled;
				}
			}
			Debug.Log($"{LogPrefix} manifest loaded");
		}

		private static void ResetManifestBlocked()
		{
			if (File.Exists(ManifestFileBlocked))
				File.Delete(ManifestFileBlocked);

			disabledPackages = new string[0];
			Debug.Log($"{LogPrefix} manifest blocked reset");
		}

		private static void LoadManifestBlocked()
		{
			if (!File.Exists(ManifestFileBlocked))
				return;

			var manifestJson = File.ReadAllText(ManifestFileBlocked);
			disabledPackages = JsonConvert.DeserializeObject<string[]>(manifestJson);
			Debug.Log($"{LogPrefix} manifest blocked loaded");
		}

		private void SaveManifestBlocked()
		{
			disabledPackages = packages
				.Where(w => w.Value == false)
				.Select(s => s.Key)
				.ToArray();

			var manifestJson = JsonConvert.SerializeObject(disabledPackages, Formatting.Indented);
			File.WriteAllText(ManifestFileBlocked, manifestJson);
			Debug.Log($"{LogPrefix} manifest blocked saved");
		}

		void OnGUI()
		{
			EditorGUILayout.LabelField("Build Profiles", EditorStyles.boldLabel);
			HeadlessEditorPrefs.IsDevBuild = EditorGUILayout.Toggle("Development Build", HeadlessEditorPrefs.IsDevBuild);
			HeadlessEditorPrefs.IsMultiplay = EditorGUILayout.Toggle("Multyplay Build", HeadlessEditorPrefs.IsMultiplay);

			var profiles = HeadlessProfiles.GetProfileList();

			foreach (var p in profiles)
			{
				if (GUILayout.Button($"Build {p.Value}", GUILayout.Height(36)))
				{
					
					HeadlessEditorPrefs.LastProfile = p.Key;
					HeadlessBuilder.ManualBuild();
				}
			}
				
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Headless Cleaner", EditorStyles.boldLabel);
			
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("cleaning", EditorStyles.boldLabel);
			if (GUILayout.Button("Defines"))
			{
				HeadlessCleaner.CleanDefines();
			}
			if (GUILayout.Button("Folders"))
			{
				HeadlessCleaner.CleanFolders();
			}
			if (GUILayout.Button("Packages"))
			{
				HeadlessCleaner.CleanPackages();

				LoadManifest();
			}
			
			if (GUILayout.Button("Addressables"))
			{
				HeadlessCleaner.CleanAddressable();

				LoadManifest();
			}
			if (GUILayout.Button("Prefabs"))
			{
				HeadlessCleaner.CleanPrefabs();
			}
			if (GUILayout.Button("All -prefabs"))
			{
				HeadlessCleaner.CleanEverythingExceptPrefabs();

				LoadManifest();
			}

			if (GUILayout.Button("All"))
			{
				HeadlessCleaner.CleanEverything();

				LoadManifest();
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Clean packages async (test)"))
			{
				RemoveBlockedPackagesCoroutine();

				LoadManifest();
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(15);

			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("blocked packages", EditorStyles.boldLabel);
			if (GUILayout.Button("Reload"))
			{
				LoadManifest();
				Debug.Log($"{LogPrefix} reload complete");
			}
			if (GUILayout.Button("Save"))
			{
				SaveManifestBlocked();
				Debug.Log($"{LogPrefix} save complete");
			}
			if (GUILayout.Button("Reset"))
			{
				ResetManifestBlocked();
				LoadManifest();

				Debug.Log($"{LogPrefix} reset complete");
			}
			GUILayout.EndHorizontal();

			ShowPackagesScroll();
		}

		private Vector2 scrollPosition;
		private void ShowPackagesScroll()
		{
			GUILayout.BeginVertical();
			var h = GUI.skin.horizontalScrollbar;
			var v = GUI.skin.verticalScrollbar;
			var b = GUI.skin.label;

			var height = GUILayout.ExpandHeight(true);
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false, h, v, b, height);

			var toggles = new Dictionary<string, bool>();

			foreach (var pair in packages)
			{
				var packageName = pair.Key;
				var packageEnabled = pair.Value;
				toggles[packageName] = ShowPackage(packageName, packageEnabled);
			}
			packages = toggles;

			EditorGUILayout.EndScrollView();

			GUILayout.EndVertical();
		}

		private bool ShowPackage(string packageName, bool packageEnabled)
		{
			var origFontStyle = EditorStyles.label.fontStyle;
			var origFontColor = EditorStyles.label.normal.textColor;
			var origColorHover = EditorStyles.label.hover.textColor;
			var origColorFocus = EditorStyles.label.focused.textColor;
			var origColorActive = EditorStyles.label.active.textColor;
			var origHeight = EditorStyles.label.fixedHeight;

			float originalWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 400;

			if (!packageEnabled)
			{
				//EditorStyles.label.fontStyle = FontStyle.Bold;
				EditorStyles.label.normal.textColor = Color.red;
			}
			var toggleHeight = GUILayout.MaxHeight(20);
			var toggled = EditorGUILayout.Toggle(packageName, packageEnabled, EditorStyles.toggle, toggleHeight);
			EditorStyles.label.fontStyle = origFontStyle;
			EditorStyles.label.normal.textColor = origFontColor;
			EditorStyles.label.hover.textColor = origColorHover;
			EditorStyles.label.focused.textColor = origColorFocus;
			EditorStyles.label.active.textColor = origColorActive;
			EditorStyles.label.fixedHeight = origHeight;

			EditorGUIUtility.labelWidth = originalWidth;
			return toggled;
		}

		public static void RemoveBlockedPackagesCoroutine()
		{
			HeadlessRoutine.start(RemoveBlockedPackagesRoutine());
		}

		public static async Task RemoveBlockedPackagesAsync()
		{
			Debug.Log($"{LogPrefix} start removing blocked packages async");
			LoadManifest();
			var requests = new List<Request>();

			foreach (var disabledPackege in disabledPackages)
			{
				Debug.Log($"{LogPrefix} request remove package: {disabledPackege} total={requests?.Count}");
				var request = UnityEditor.PackageManager.Client.Remove(disabledPackege);
				requests.Add(request);
			}

			var total = requests.Count;
			int completed = 0;
			Debug.Log($"{LogPrefix} start waiting async");

			do
			{
				Debug.Log($"-- wait --");
				await Task.Yield();
				completed = requests.Count(r => r.IsCompleted);
			}
			while (completed < total);

			Debug.Log($"{LogPrefix} removed blocked packages: {total}/{disabledPackages.Length}");
		}

		private static IEnumerator RemoveBlockedPackagesRoutine()
		{
			Debug.Log($"{LogPrefix} start removing blocked packages routine");
			LoadManifest();
			var requests = new List<Request>();

			foreach (var disabledPackege in disabledPackages)
			{
				Debug.Log($"{LogPrefix} request remove package: {disabledPackege} total={requests?.Count}");
				var request = UnityEditor.PackageManager.Client.Remove(disabledPackege);
				requests.Add(request);
			}

			var total = requests.Count;
			int completed = 0;
			Debug.Log($"{LogPrefix} start waiting");
			var wait = new WaitForSecondsRealtime(0.5f);
			do
			{
				Debug.Log($"-- wait --");
				yield return wait;
				completed = requests.Count(r => r.IsCompleted);
			}
			while (completed < total);

			Debug.Log($"{LogPrefix} removed blocked packages: {total}/{disabledPackages.Length}");
		}

		public static void RemoveBlockedPackages()
		{
			try
			{
				LoadManifest();
				Debug.Log($"{LogPrefix} start removing blocked packages");
				var manifest = GetManifest();
				var dependencies = manifest["dependencies"].Children().ToList();
				var blockedTokens = manifest["dependencies"].Children()
					.Where(w => w.Type == JTokenType.Property && disabledPackages.Contains(w.ToObject<JProperty>().Name))
					.ToArray();

				foreach (var token in blockedTokens)
				{
					token.Remove();
				}

				Debug.Log($"{LogPrefix} removed blocked packages: {blockedTokens.Length}/{disabledPackages.Length}. result={manifest}");
				File.WriteAllText(ManifestFile, manifest.ToString());

				//force rebuild packages : https://docs.unity3d.com/Manual/upm-conflicts-auto.html
				if (File.Exists(PackagesLockFile))
					File.Delete(PackagesLockFile);

				//AssetDatabase.Refresh();
				Debug.Log($"{LogPrefix} complete removing blocked packages");
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}
		
		public static void RemoveAddressables()
		{
			try
			{
				Debug.Log($"{LogPrefix} start RemoveAddressables");
				var a = AddressablesExcludeSettings.Instance.List;
				foreach (var group in AddressablesExcludeSettings.Instance.List)
				{
					if (group == null)
						continue;
					if(!group.HasSchema<BundledAssetGroupSchema>() )
						continue;
					group.GetSchema<BundledAssetGroupSchema>().IncludeInBuild = false;
					EditorUtility.SetDirty(group);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}

		static string packagesFolder;
		static string PackagesFolder
		{
			get
			{
				if (string.IsNullOrEmpty(packagesFolder))
				{
					var assetsFolder = Application.dataPath;
					var projectFolder = Directory.GetParent(assetsFolder).ToString();
					packagesFolder = Path.Combine(projectFolder, "Packages");
				}
				return packagesFolder;
			}
		}

		static string manifestFileBlocked;
		static string ManifestFileBlocked
		{
			get
			{
				if (string.IsNullOrEmpty(manifestFileBlocked))
				{
					manifestFileBlocked = Path.Combine(PackagesFolder, "manifest-headless-blocked.json");
				}
				return manifestFileBlocked;
			}
		}

		static string packagesLockFile;
		static string PackagesLockFile
		{
			get
			{
				if (string.IsNullOrEmpty(packagesLockFile))
				{
					packagesLockFile = Path.Combine(PackagesFolder, "packages-lock.json");
				}
				return packagesLockFile;
			}
		}

		static string manifestFile;
		static string ManifestFile
		{
			get
			{
				if (string.IsNullOrEmpty(manifestFile))
				{
					manifestFile = Path.Combine(PackagesFolder, "manifest.json");
				}
				return manifestFile;
			}
		}

	}
}