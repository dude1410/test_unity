using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TBR.Bundles.Editor;
using UnityEditor;
using UnityEngine;

namespace TBR.HeadlessServer
{
	public static class HeadlessCleaner
	{
		static bool isRemoveTags = true; //
		static bool isShowLogs = true;
		public delegate string NormalizePathDelegate(string input);
		public delegate void BackupPathDelegate(string path);
		static string projectFolder = string.Empty;
		const string HEADLESS_REMOVE = "headless_remove";
		const string LogPrefix = "[Headless Cleaner]";

		static string[] processExtensions = {
			".prefab",
		};

		public static async Task CleanEverythingAsync()
		{
			int delay = 2 * 1000;
			Debug.Log($"{LogPrefix} start async cleanup");

			CleanDefines();
			await Task.Delay(delay);

			CleanFolders();
			await Task.Delay(delay);

			await HeadlessPackageManager.RemoveBlockedPackagesAsync();
			//HeadlessCleaner.CleanPackages();
			await Task.Delay(delay);

			CleanPrefabs();
			await Task.Delay(delay);

			AssetDatabase.Refresh();
			await Task.Delay(delay);

			var success = AddressablesBuildUtils.BuildAddressables();
			if (!success)
				throw new Exception($"{LogPrefix} addressables build failed");

			HeadlessEditorHelper.EnableCleaningInHeadlessBuilder = false;
			HeadlessEditorHelper.EnableBuildBundlesBeforeCompile = false;

			Debug.Log($"{LogPrefix} complete async cleanup");
		}

		public static void CleanEverything()
		{
			CleanDefines();
			CleanFolders();
			CleanPackages();
			CleanAddressable();
			CleanPrefabs();
			Debug.Log($"{LogPrefix} clean EVERYTHING complete");
		}

		public static void CleanEverythingExceptPrefabs()
		{
			CleanDefines();
			CleanFolders();
			CleanPackages();
			CleanAddressable();
			Debug.Log($"{LogPrefix} clean EVERYTHING (-prefabs) complete");
		}

		public static void CleanPrefabs()
		{
			CleaningPre();
			HeadlessBuilder.ReplaceAssetsWithDummies();
			CleaningAfter();
			Debug.Log($"{LogPrefix} clean prefabs complete");

			//CleanAllMissingScripts();
		}

		public static void CleanPackages()
		{
			CleaningPre();
			HeadlessPackageManager.RemoveBlockedPackages();
			CleaningAfter();
			Debug.Log($"{LogPrefix} clean packages complete");
		}
		
		public static void CleanAddressable()
		{
			CleaningPre();
			HeadlessPackageManager.RemoveAddressables();
			CleaningAfter();
			Debug.Log($"{LogPrefix} clean addressables complete");
		}

		public static void CleanFolders()
		{
			CleaningPre();
			HeadlessBuilder.RemoveUnusedFolders();
			CleaningAfter();
			Debug.Log($"{LogPrefix} clean folders complete");
		}

		public static void CleanDefines()
		{
			CleaningPre();
			HeadlessEditorHelper.PreprocessDefinesExtra();
			CleaningAfter();
			Debug.Log($"{LogPrefix} clean define complete");
		}

		private static void CleaningPre()
		{
			/*/
			AssetDatabase.StartAssetEditing();
			EditorApplication.LockReloadAssemblies();
			AssetDatabase.SaveAssets();
			AssetDatabase.ReleaseCachedFileHandles();
			AssetDatabase.DisallowAutoRefresh();
			//*/
		}

		private static void CleaningAfter()
		{
			/*/
			AssetDatabase.StopAssetEditing();
			AssetDatabase.Refresh();
			EditorApplication.UnlockReloadAssemblies();
			AssetDatabase.AllowAutoRefresh();
			UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
			//*/
		}

		public static void CleanDirRecursive(string processFolder)
		{
			if (IsDirNeedToClean(processFolder))
			{
				CleanDir(processFolder);
				return;
			}
			var subDirs = Directory.GetDirectories(processFolder);
			foreach (var subDir in subDirs)
			{
				CleanDirRecursive(subDir);
			}
		}

		public static void CleanDir(string processFolder)
		{
			//Directory.Delete(processFolder, true);

			var files = Directory.GetFiles(processFolder);
			foreach (var processFile in files)
			{
				var fileName = Path.GetFileName(processFile);
				if (string.Equals(fileName, HEADLESS_REMOVE))
					continue;

				try
				{
					File.Delete(processFile);
				}
				catch (Exception ex)
				{
					Debug.LogError($"{LogPrefix} unable delete file: {processFile}");
					Debug.LogException(ex);
				}
			}
			var subDirs = Directory.GetDirectories(processFolder);
			foreach (var subDir in subDirs)
			{
				try
				{
					Directory.Delete(subDir, true);
				}
				catch (Exception ex)
				{
					Debug.LogError($"{LogPrefix} unable delete dir: {subDir}");
					Debug.LogException(ex);
				}
			}
			UnityEngine.Debug.Log($"[Headless builder] deleted dir='{processFolder}'. (marked to remove in headless, files={files?.Length} subdirs={subDirs?.Length})");
		}

		public static bool IsDirNeedToClean(string processFolder)
		{
			var removeDirFile = Path.Combine(processFolder, HEADLESS_REMOVE);
			return File.Exists(removeDirFile);
		}

		public static string RelativePath(string absolutePath, string referencePath)
		{
			string result = string.Empty;
			int offset;

			// this is the easy case.  The file is inside of the working directory.
			if (absolutePath.StartsWith(referencePath))
			{
				return absolutePath.Substring(referencePath.Length + 1);
			}

			// the hard case has to back out of the working directory
			string[] baseDirs = referencePath.Split(new char[] { ':', '\\', '/' });
			string[] fileDirs = absolutePath.Split(new char[] { ':', '\\', '/' });

			// if we failed to split (empty strings?) or the drive letter does not match
			if (baseDirs.Length <= 0 || fileDirs.Length <= 0 || baseDirs[0] != fileDirs[0])
			{
				// can't create a relative path between separate harddrives/partitions.
				return absolutePath;
			}

			// skip all leading directories that match
			for (offset = 1; offset < baseDirs.Length; offset++)
			{
				if (baseDirs[offset] != fileDirs[offset])
					break;
			}

			// back out of the working directory
			for (int i = 0; i < (baseDirs.Length - offset); i++)
			{
				result += "..\\";
			}

			// step into the file path
			for (int i = offset; i < fileDirs.Length - 1; i++)
			{
				result += fileDirs[i] + "\\";
			}

			// append the file
			result += fileDirs[fileDirs.Length - 1];

			return result;
		}

		static GameObject LoadAsset(string processFile, NormalizePathDelegate normalizeDelegate)
		{
			var fileExt = GetFileExtension(processFile);
			if (Array.IndexOf(processExtensions, fileExt) == -1)
				return null;

			if (string.IsNullOrWhiteSpace(projectFolder))
			{
				var assetsFolder = normalizeDelegate(Application.dataPath);
				projectFolder = normalizeDelegate(Directory.GetParent(assetsFolder).ToString());
			}

			processFile = RelativePath(processFile, projectFolder);
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(processFile);
			return prefab;
		}

		//TBR fix
		public static string GetFileExtension(string filePath)
		{
#if UNITY_EDITOR_LINUX
		return Path.GetExtension(filePath);
#else
			return Path.GetExtension(filePath).ToLower();
#endif
		}
		
		public static void TryCleanPrefab(string processFile, NormalizePathDelegate normalizeDelegate, BackupPathDelegate backupDelegate)
		{
			if (!isRemoveTags)
				return;

			var prefab = LoadAsset(processFile, normalizeDelegate);

			CleanPrefab(prefab, normalizeDelegate, backupDelegate);
		}
		
		static void Log(object message)
		{
			if (isShowLogs)
			{
				Debug.Log(message);
			}
		}

		static void CleanPrefab(GameObject prefab, NormalizePathDelegate normalizeDelegate, BackupPathDelegate backupDelegate)
		{
			if (!prefab)
				return;

			BackupPrefab(prefab, normalizeDelegate, backupDelegate);
			
			var prefabTag = prefab.GetComponent<HeadlessRemoveTag>();
			if (prefabTag)
			{
				Debug.LogError($"[Headless Cleaner] WARNING! PREFAB MARKED WITH={prefabTag}. CLEANING WILL BREAK PREFAB. ABORT");
				return;
			}

			var removeTags = prefab.GetComponentsInChildren<HeadlessRemoveTag>();
			if (removeTags == null || removeTags.Length < 1)
				return;

			int found = 0;
			
			Log($"[Headless Cleaner] start cleaning prefab={prefab.name}");

			foreach (var tag in removeTags)
			{
				if (tag)
				{
					
					var tagRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(tag.gameObject);
					//tag placed in nested prefab, need to go deep
					if (tagRoot && tagRoot != tag.gameObject)
					{
						var tagRootPrefab = PrefabUtility.GetCorrespondingObjectFromSource(tagRoot);
						if (tagRootPrefab)
						{
							Log($"[Headless Cleaner] need to go deep into prefab ({tagRootPrefab}) to remove ({tag.gameObject})");
						}
					}
					else
					{
						Log($"[Headless Cleaner] remove prefab part=({tag.gameObject}). reason: part of same prefab=({prefab.name})");
						
						found++;
						GameObject.DestroyImmediate(tag.gameObject, true);
					}
				}
			}

			if (found > 0)
			{
				PrefabUtility.SavePrefabAsset(prefab);

				Log($"[Headless Cleaner] cleaned prefab=({prefab.name}) removed tags={found}.");
				
				//PrefabUtility.UnloadPrefabContents(prefab);
			}
		}

		static void BackupPrefab(GameObject prefab, NormalizePathDelegate normalizeDelegate, BackupPathDelegate backupDelegate)
		{
			//TBR injection
			if (HeadlessEditorHelper.DisableBackupFile)
				return;
			
			var prefabPath = AssetDatabase.GetAssetPath(prefab);

			if (string.IsNullOrWhiteSpace(projectFolder))
			{
				var assetsFolder = normalizeDelegate(Application.dataPath);
				projectFolder = normalizeDelegate(Directory.GetParent(assetsFolder).ToString());
			}

			var prefabFullPath = Path.Combine(projectFolder, prefabPath);
			prefabFullPath = normalizeDelegate(prefabFullPath);
			Log($"[Headless Cleaner] backup nested prefab=({prefab}) path=({prefabFullPath})");
			
			backupDelegate(prefabFullPath);
		}

		[MenuItem("Utils/Clean missing scripts")]
		private static void CleanAllMissingScripts()
		{
			string PATH = "Assets/";
			AssetDatabase.Refresh();
			int delCount = 0;
			string[] ids = AssetDatabase.FindAssets("t:Prefab", new string[] { PATH });
			for (int i = 0; i < ids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(ids[i]);
				GameObject prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
				GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

				RecursivelyModifyPrefabChilds(instance, ref delCount);

				if (delCount > 0)
				{
					Debug.Log($"{LogPrefix} removed missing scripts ({delCount}) on {path}", prefab);
					PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction);
				}

				UnityEngine.Object.DestroyImmediate(instance);
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.ClearProgressBar();

			Debug.Log($"{LogPrefix} clean missing scripts complete. deleted={delCount}");
		}

		private static void RecursivelyModifyPrefabChilds(GameObject obj, ref int delCount)
		{
			if (obj.transform.childCount > 0)
			{
				for (int i = 0; i < obj.transform.childCount; i++)
				{
					var _childObj = obj.transform.GetChild(i).gameObject;
					RecursivelyModifyPrefabChilds(_childObj, ref delCount);
				}
			}

			int innerDelCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
			delCount += innerDelCount;
		}

		public static void CleanBuildJunk(string fullBuildPath)
		{
			var il2cppDirs = Directory.GetDirectories(fullBuildPath).Where(s => s.Contains("BackUpThisFolder_ButDontShipItWithYourGame"));
			foreach (var dir in il2cppDirs)
			{
				try
				{
					Debug.Log($"{LogPrefix} clean junk: {dir}");
					Directory.Delete(dir, true);
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
			}
		}

	}
}