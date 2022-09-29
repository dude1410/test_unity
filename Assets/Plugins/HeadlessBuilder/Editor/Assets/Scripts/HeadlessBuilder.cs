/* 
 * Headless Builder
 * (c) Salty Devs, 2019
 * 
 * Please do not publish or pirate this code.
 * We worked really hard to make it.
 * 
 */

using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using TBR.HeadlessServer;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

// This class does the actual building.
public static class HeadlessBuilder
{
	const string LOG_PREFIX = "[HeadlessBuilder]";

	[Flags]
	private enum BuildOptionsFixed
	{
		Development = 1,
		None = 2,
		AutoRunPlayer = 4,
		ShowBuiltPlayer = 8,
		BuildAdditionalStreamedScenes = 16,
		AcceptExternalModificationsToPlayer = 32,
		InstallInBuildFolder = 64,
		WebPlayerOfflineDeployment = 128,
		ConnectWithProfiler = 256,
		AllowDebugging = 512,
		SymlinkLibraries = 1024,
		UncompressedAssetBundle = 2048,
		ConnectToHost = 4096,
		DeployOnline = 8192,
		EnableHeadlessMode = 16384,
	}

	private static string assetsFolder;
	private static string projectFolder;
	private static string backupFolder;
	private static string dummyFolder;
	private static string[] dummyExtensions;

	private static string buildExecutable;
	private static string buildExtension;
	private static string buildName = "Core";
	private static string packageName = "";
	public static bool cloudBuild = false;
	public static bool batchBuild = false;
	public static bool manualBuild = false;
	public static bool developmentBuild = false;
	public static bool debugBuild = false;
	public static bool queueBuild = false;
	public static int queueID = 0;

	private static BuildTargetGroup regularTargetGroup = BuildTargetGroup.Unknown;
	private static BuildTarget regularTarget = BuildTarget.NoTarget;
	private static List<string> sceneAssets;
	private static string[] sceneList;
	private static string currentScene;
	private static string buildPath;
	private static BuildTarget buildTarget;
	private static BuildOptions buildOptions;
	public static bool buildError;
	private static List<string> skipDummy;
	private static long sizeOriginal;
	private static long sizeDummy;
	private static int replaceCount;

	private static HeadlessSettings headlessSettings;

	private static HeadlessProgress progressWindow;

	// The functions below act as entry points to this class and one of them
	// should be called so that everything is initialized properly.
	public static void ManualBuildQueue(int nextID = 0)
	{
		if (nextID == 0)
		{
			ResetBuild();
		}

		HeadlessProfiles.FindProfiles();

		queueID = nextID;
		int i = 0;
		bool found = false;
		foreach (KeyValuePair<string, string> profile in HeadlessProfiles.GetProfileList())
		{
			if (i == nextID)
			{
				HeadlessProfiles.SetProfile(profile.Key);
				found = true;
			}
			i++;
		}

		if (found)
		{
			ManualBuild(true);
		}
		else
		{
			UnityEngine.Debug.Log("Finished building all profiles!\nHeadless Builder (v" + Headless.version + ")");
		}

	}

	public static void ResetBuild()
	{
		manualBuild = false;
		batchBuild = false;
		cloudBuild = false;
		queueBuild = false;

		queueID = 0;
		buildError = false;
	}

	public static void ManualBuild(bool hasQueue = false)
	{
		if (!hasQueue)
		{
			ResetBuild();
		}

		queueBuild = hasQueue;
		manualBuild = true;

		Build();
	}

	public static void BatchBuild()
	{
		ResetBuild();

		batchBuild = true;

		Build();
	}

	public static void CloudBuild()
	{
		ResetBuild();

		cloudBuild = true;

		Build();
	}

	public static void DebugBuild()
	{
		debugBuild = true;
		Build();
	}

	// This function overarches the entire build process
	private static void Build()
	{

		buildError = false;


		if (batchBuild)
		{

			// Process the relevant command line arguments

			string[] args = System.Environment.GetCommandLineArgs();
			string batchPath = null;
			string batchProfile = null;
			for (int i = 0; i < args.Length - 1; i++)
			{
				if (args[i] == "-headlessPath")
				{
					batchPath = args[i + 1];
				}
				if (args[i] == "-headlessProfile")
				{
					batchProfile = args[i + 1];
				}
			}

			if (batchPath != null && Directory.Exists(batchPath))
			{
				buildPath = batchPath;
			}
			else
			{
				UnityEngine.Debug.LogError("Use the -headlessPath command line parameter to set a valid destination path for the headless build\nHeadless Builder (v" + Headless.version + ")");
				return;
			}

			if (batchProfile != null)
			{
				bool found = false;
				HeadlessProfiles.FindProfiles();

				foreach (KeyValuePair<string, string> profile in HeadlessProfiles.GetProfileList())
				{
					if (profile.Value.Equals(batchProfile))
					{
						found = true;
						HeadlessProfiles.SetProfile(profile.Key);
					}
				}
				if (!found)
				{
					UnityEngine.Debug.LogError("The profile specified by the -headlessProfile command line parameter was not found\nHeadless Builder (v" + Headless.version + ")");
					return;
				}
			}
		}

		if (manualBuild || debugBuild)
		{
			if (HeadlessEditorHelper.IsNotBatchMode)
			{
				if (!EditorWindow.GetWindow<HeadlessEditor>().IsDocked())
				{
					EditorWindow.GetWindow<HeadlessEditor>().Close();
				}
			}
		}

		// Load the settings (from file, not from memory)
		PrepareVariables();



		Headless.SetBuildingHeadless(true, headlessSettings.profileName);


		bool asyncExecute = manualBuild || debugBuild;

		if (asyncExecute)
		{
			Debug.Log("[Headless Builder] start ASYNC build");
			HeadlessRoutine.start(InnerBuildAsync());
		}
		else
		{
			Debug.Log("[Headless Builder] start SYNC build");
			InnerBuildSync();
		}
	}

	// This function calls all the build steps in a synchronous fashion
	static void InnerBuildSync()
	{
		if (!InitializeBuild())
		{
			buildError = true;
			FinalizeBuildSync(false);
			return;
		}

		if (!PrepareBuild())
		{
			buildError = true;
			FinalizeBuildSync(false);
			return;
		}

		if (!PreprocessBuild())
		{
			buildError = true;
			FinalizeBuildSync(true);
			return;
		}

		if (!PerformBuild())
		{
			buildError = true;
			FinalizeBuildSync(true);
			return;
		}

		if (!PlayBuild())
		{
			buildError = true;
			FinalizeBuildSync(true);
			return;
		}

		FinalizeBuildSync(true);

	}

	// This function calls all the build steps in an asynchronous fashion
	static IEnumerator InnerBuildAsync()
	{
		Debug.Log($"[Headless Builder] building. start initilalize");
		if (!InitializeBuild())
		{
			Debug.LogError($"[Headless Builder] building error: initilalize");
			buildError = true;
			FinalizeBuildSync(false);
			yield break;
		}
		Debug.Log($"[Headless Builder] building. complete initilalize");
		yield return new WaitForSecondsRealtime(1);

		Debug.Log($"[Headless Builder] building. start prepare");
		if (!PrepareBuild())
		{
			Debug.LogError($"[Headless Builder] building error: prepare");
			buildError = true;
			FinalizeBuildSync(false);
			yield break;
		}
		Debug.Log($"[Headless Builder] building. complete prepare");
		yield return new WaitForSecondsRealtime(1);

		Debug.Log($"[Headless Builder] building. start preprocess");
		if (!PreprocessBuild())
		{
			Debug.LogError($"[Headless Builder] building error: preprocess");
			buildError = true;
			FinalizeBuildSync(true);
			yield break;
		}
		Debug.Log($"[Headless Builder] building. complete preprocess");
		yield return new WaitForSecondsRealtime(1);

		Debug.Log($"[Headless Builder] building. start build");
		if (!PerformBuild())
		{
			Debug.LogError($"[Headless Builder] building error: build");
			buildError = true;
			FinalizeBuildSync(true);
			yield break;
		}
		Debug.Log($"[Headless Builder] building. complete build");
		yield return new WaitForSecondsRealtime(1);


		Debug.Log($"[Headless Builder] building. start play");
		if (!PlayBuild())
		{
			Debug.LogError($"[Headless Builder] building error: play");
			buildError = true;
			FinalizeBuildSync(true);
			yield break;
		}
		Debug.Log($"[Headless Builder] building. complete play");
		yield return new WaitForSecondsRealtime(1);

		Debug.Log($"[Headless Builder] building. start finalize");
		FinalizeBuildSync(true);
		Debug.Log($"[Headless Builder] building. complete finalize");
	}

	// This function cleans a build up in a synchronized fashion
	// and is called regardless of whether the build was successful
	static void FinalizeBuildSync(bool restore)
	{

		if (restore)
		{
			try
			{
				RestoreBuild();
			}
			catch (Exception e)
			{
				buildError = true;
				UnityEngine.Debug.LogError(e);
			}

			try
			{
				RevertBuild();
			}
			catch (Exception e)
			{
				buildError = true;
				UnityEngine.Debug.LogError(e);
			}
		}

		if (buildError)
		{
			UnityEngine.Debug.LogError("Build failed!\nHeadless Builder (v" + Headless.version + ")");
			Headless.SetBuildingHeadless(false, HeadlessProfiles.currentProfile);
			HeadlessCallbacks.InvokeCallbacks("HeadlessBuildFailed");
			if (manualBuild || debugBuild)
			{
				SetProgress(null);
			}
		}
		else
		{
			UnityEngine.Debug.Log("Build success!\nHeadless Builder (v" + Headless.version + ")");
			Headless.SetBuildingHeadless(false, HeadlessProfiles.currentProfile);
			if (manualBuild || debugBuild)
			{
				SetProgress(null);
			}
			int finishedBuilds = EditorPrefs.GetInt("HEADLESSBUILDER_FINISHEDBUILDS", 0);
			if (finishedBuilds < int.MaxValue - 64)
			{
				EditorPrefs.SetInt("HEADLESSBUILDER_FINISHEDBUILDS", finishedBuilds + 1);
			}
		}

		if (queueBuild)
		{
			if (!buildError)
			{
				ManualBuildQueue(queueID + 1);
			}
		}

	}


	// BUILD STEPS:

	static bool InitializeBuild()
	{
		try
		{

			HeadlessCallbacks.InvokeCallbacks("HeadlessBuildBefore");

			if (manualBuild || debugBuild)
			{
				// Hide progress window if visible
				HeadlessProgress.Hide();
			}

			if (manualBuild)
			{
				// If this is a manual build, store the currently selected build target
				// so we can revert to that later
				regularTarget = EditorUserBuildSettings.activeBuildTarget;
				regularTargetGroup = BuildPipeline.GetBuildTargetGroup(regularTarget);
			}

			if (!batchBuild)
			{
				// Get the current build path for regular builds so we can use it as a default value
				try
				{
					buildPath = Directory.GetParent(EditorUserBuildSettings.GetBuildLocation(EditorUserBuildSettings.activeBuildTarget)).ToString();
				}
				catch (Exception e)
				{
					if (e.Message.Equals("never"))
					{
						// These errors are no problem at all, so we use this workaround to supress them
						UnityEngine.Debug.LogWarning(e);
					}
				}
			}

			if (manualBuild || debugBuild)
			{

				if (EditorApplication.isCompiling)
				{
					EditorUtility.DisplayDialog("Headless Builder",
						"You can't build while the editor is compiling.\n\n" +
						"Please wait for compilation to finish and try again.", "OK");

					return false;
				}

				if (EditorApplication.isPlaying)
				{
					EditorUtility.DisplayDialog("Headless Builder",
						"You can't build while the editor is in play mode.\n\n" +
						"Please exit play mode by clicking the stop button and try again.", "OK");

					return false;
				}

				if (headlessSettings.valueDummy && !headlessSettings.valueSkipConfirmation)
				{

					if (!EditorUtility.DisplayDialog("Headless Builder",
						"You have enabled a feature that replaces your visual and audio assets with dummies.\n\n" +
						"This will greatly enhance your build's performance,\n" +
						"but will also force Unity to re-import all assets, which might take a long time.\n\n" +
						"Are you sure you want to continue?", "Yes", "Cancel"))
					{

						return false;

					}
				}

				if (headlessSettings.valueGI &&
					UnityEditor.EditorSettings.serializationMode != UnityEditor.SerializationMode.ForceText)
				{

					if (EditorUtility.DisplayDialog("Headless Builder",
						"You have enabled a feature that requires Unity's serialization mode to be set to 'Force Text',\n" +
						"but your current serialization mode is something else.\n\n" +
						"Should we change the serialization mode for you?\n" +
						"This might take a long time.\n\n" +
						"If you choose to not change the serialization mode, the relevant features will be skipped.", "Change to 'Force Text'", "Ignore"))
					{

						UnityEditor.EditorSettings.serializationMode = UnityEditor.SerializationMode.ForceText;

					}
				}


				// If this is a manual build, make sure the scene is saved, because we'll reload it later
				EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

				for (int i = 0; i < EditorSceneManager.sceneCount; i++)
				{
					if (EditorSceneManager.GetSceneAt(i).isDirty)
					{
						UnityEngine.Debug.LogError("Any changes to scenes must be changed before doing a headless build\nHeadless Builder (v" + Headless.version + ")");
						return false;
					}
				}

				if (manualBuild)
				{
					// Get the build path used for previous headless builds and use it as a default, if there is any
					bool foundPreviousPath = false;
					if (headlessSettings.buildPath != null && headlessSettings.buildPath.Length > 3)
					{
						if (Directory.Exists(headlessSettings.buildPath))
						{
							buildPath = headlessSettings.buildPath;
							foundPreviousPath = true;
						}
					}

					// Ask output folder
					if (!foundPreviousPath || !headlessSettings.valueRememberPath)
					{
						buildPath = EditorUtility.SaveFolderPanel("Choose Destination For Headless Build (" + headlessSettings.profileName + ")", buildPath, "");
						if (buildPath.Length == 0)
						{
							UnityEngine.Debug.LogError("You must choose a destination path for the headless build\nHeadless Builder (v" + Headless.version + ")");
							return false;
						}
					}
				}

			}

			// Set the build number
			headlessSettings.buildPath = buildPath;
			if (!debugBuild)
			{
				if (headlessSettings.buildID < int.MaxValue - 128)
				{
					headlessSettings.buildID++;
				}
			}

			if (manualBuild || debugBuild)
			{
				// If this is a manual build, show the progress bar window
				progressWindow = HeadlessProgress.Init();
			}

			SetProgress("INIT");

			return true;
		}
		catch (Exception e)
		{
			UnityEngine.Debug.LogError(e);
			return false;
		}

	}

	static void PrepareVariables()
	{
		headlessSettings = HeadlessEditor.LoadSettings(HeadlessProfiles.currentProfile, true);

		assetsFolder = NormalizePath(Application.dataPath);
		projectFolder = NormalizePath(Directory.GetParent(assetsFolder).ToString());
		dummyFolder = NormalizePath(HeadlessExtensions.GetHeadlessBuilderPath(true) + "/Editor/Assets/Dummy");


		if (!HeadlessEditorHelper.DisableBackupFile)
			Directory.CreateDirectory(projectFolder + "/headless_builder_backup");

		backupFolder = NormalizePath(projectFolder + "/headless_builder_backup");
	}

	static bool PrepareBuild()
	{

		try
		{

			if (manualBuild || debugBuild)
			{
				if (File.Exists(backupFolder + "/lock.txt"))
				{
					if (!EditorUtility.DisplayDialog("Headless Builder",
						"A previous build has modified some project files\n" +
						"and the backup in the 'headless_builder_backup' folder has not yet been restored.\n\n" +
						"We recommend you manually restore the backup first.\n" +
						"Please refer to the Troubleshooting chapter in the documentation on how to do this.\n\n" +
						"If you choose to ignore this, the previously made backup will be overwritten.\n\n" +
						"Are you sure you want to continue?", "Yes", "Cancel"))
					{

						return false;
					}
					else
					{
						if (!EditorUtility.DisplayDialog("Headless Builder",
							"Are you absolutely sure that you have no need for the backup stored in the 'headless_builder_backup' folder?\n\n" +
							"If you continue this previous backup will be overwritten.\n\n" +
							"Are you sure you want to continue?", "Yes", "Cancel"))
						{

							return false;
						}
					}
				}
			}

			try
			{
				if (Directory.Exists(backupFolder))
				{
					Directory.Delete(backupFolder, true);
				}
				Directory.CreateDirectory(backupFolder);
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogError(e);
				UnityEngine.Debug.LogError("Headless Builder does not have write permissions for " + projectFolder + "\n" + "Please read the Permissions chapter in the documentation.");
				return false;
			}


			// Get the scenes that should be included in this build
			sceneAssets = new List<string>();
			if (!headlessSettings.valueOverrideScenes)
			{
				foreach (var sceneAsset in EditorBuildSettings.scenes)
				{
					string scenePath = sceneAsset.path;
					if (!string.IsNullOrEmpty(scenePath))
					{
						if (File.Exists(projectFolder + "/" + scenePath))
						{
							sceneAssets.Add(scenePath);
						}
					}
				}
			}
			else
			{
				foreach (var sceneAsset in headlessSettings.valueSceneAssets)
				{
					string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
					if (!string.IsNullOrEmpty(scenePath))
					{
						sceneAssets.Add(scenePath);
					}
				}
			}
			sceneList = sceneAssets.ToArray();
			currentScene = EditorSceneManager.GetActiveScene().path;

			if (sceneList.Length == 0)
			{
				UnityEngine.Debug.LogError("There are no (existing) scenes in the Unity build settings!\nHeadless Builder (v" + Headless.version + ")");
				return false;
			}

			//if (!debugBuild)
			//{

			// If needed, set the build path to a certain value based on Editor settings
			string[] args = Environment.GetCommandLineArgs();
			bool commandTarget = false;
			if (Array.IndexOf(args, "-buildTarget") >= 0)
			{
				commandTarget = true;
			}
			if (cloudBuild || commandTarget || !headlessSettings.valueOverrideTarget)
			{
				if (EditorUserBuildSettings.activeBuildTarget.Equals(BuildTarget.StandaloneWindows))
				{
					headlessSettings.valuePlatform = HeadlessEditor.WINDOWS;
					headlessSettings.valueArchitecture = HeadlessEditor.X86;
				}
				else if (EditorUserBuildSettings.activeBuildTarget.Equals(BuildTarget.StandaloneWindows64))
				{
					headlessSettings.valuePlatform = HeadlessEditor.WINDOWS;
					headlessSettings.valueArchitecture = HeadlessEditor.X64;
				}
				else if (EditorUserBuildSettings.activeBuildTarget.Equals(HeadlessAPI.VersionedBuildTargetOSX()))
				{
					headlessSettings.valuePlatform = HeadlessEditor.OSX;
					headlessSettings.valueArchitecture = HeadlessEditor.X64;
				}
				else if (EditorUserBuildSettings.activeBuildTarget.Equals(BuildTarget.StandaloneLinux))
				{
					headlessSettings.valuePlatform = HeadlessEditor.LINUX;
					headlessSettings.valueArchitecture = HeadlessEditor.X86;
				}
				else if (EditorUserBuildSettings.activeBuildTarget.Equals(BuildTarget.StandaloneLinux64))
				{
					headlessSettings.valuePlatform = HeadlessEditor.LINUX;
					headlessSettings.valueArchitecture = HeadlessEditor.X64;
				}
				else
				{
					UnityEngine.Debug.LogError("This build target is not supported by Headless Builder!\nHeadless Builder (v" + Headless.version + ")");
					return false;
				}
			}

			// Set the various file names, file types and other stuff according to the target build
			String platformName = "";
			String architectureName = "";
			if (headlessSettings.valuePlatform == HeadlessEditor.WINDOWS)
			{
				platformName = "Windows";
			}
			if (headlessSettings.valuePlatform == HeadlessEditor.OSX)
			{
				platformName = "Mac OS X";
			}
			if (headlessSettings.valuePlatform == HeadlessEditor.LINUX)
			{
				platformName = "Linux";
			}
			if (headlessSettings.valueArchitecture == HeadlessEditor.X64)
			{
				architectureName = "64-bit";
			}
			if (headlessSettings.valueArchitecture == HeadlessEditor.X86)
			{
				architectureName = "32-bit";
			}
			if (headlessSettings.valueArchitecture == HeadlessEditor.UNIVERSAL)
			{
				architectureName = "universal";
			}

			UnityEngine.Debug.Log("Commencing build for " + platformName + " (" + architectureName + ") with profile \"" + headlessSettings.profileName + "\"\nHeadless Builder (v" + Headless.version + ")");


			packageName = PlayerSettings.productName.Replace(" ", String.Empty) + "Headless_" + headlessSettings.profileName;
			packageName = CleanFileName(packageName);

			if (headlessSettings.valuePlatform == HeadlessEditor.WINDOWS)
			{
				buildExtension = ".exe";
			}
			else if (headlessSettings.valuePlatform == HeadlessEditor.LINUX &&
					 headlessSettings.valueArchitecture == HeadlessEditor.X64)
			{
				buildExtension = ".x86_64";
			}
			else if (headlessSettings.valuePlatform == HeadlessEditor.LINUX &&
					 headlessSettings.valueArchitecture == HeadlessEditor.X86)
			{
				buildExtension = ".x86";
			}
			else
			{
				buildExtension = "";
			}
			buildName = CleanFileName(headlessSettings.profileName);//TBR MOD: >> + "_Core");
			buildExecutable = buildName + buildExtension;


			buildTarget = headlessSettings.GetBuildTarget();

			var buildTargetGroup = headlessSettings.GetBuildTargetGroup();
			var scriptingImplementation = headlessSettings.GetScriptingImplementation();

			PlayerSettings.SetScriptingBackend(buildTargetGroup, scriptingImplementation);

			var il2CppConfig = headlessSettings.GetIl2CppCompilerConfiguration();
			PlayerSettings.SetIl2CppCompilerConfiguration(buildTargetGroup, il2CppConfig);

			buildOptions = 0;
			if (headlessSettings.valuePlatform == HeadlessEditor.LINUX)
			{
				buildOptions |= BuildOptions.EnableHeadlessMode;
			}

			if(developmentBuild)
				buildOptions |= HeadlessEditorHelper.GetExtraBuildOptions();

			//}

			SetProgress("PREPROCESS");

			return true;
		}
		catch (Exception e)
		{
			UnityEngine.Debug.LogError(e);
			return false;
		}

	}

	static bool PreprocessBuild()
	{

		try
		{
			Debug.Log($"{LOG_PREFIX} pre process build. started");

			// Save the settings cause we might have changed them
			// This will also propagate the runtime settings so they can be included in the build
			HeadlessEditor.SaveSettings(headlessSettings, HeadlessProfiles.currentProfile);

			Debug.Log($"{LOG_PREFIX} pre process build. saved settings");

			//TBR injection
			if (!HeadlessEditorHelper.DisableBackupFile)
			{
				System.IO.File.WriteAllText(backupFolder + "/lock.txt", "The existence of this file means that a backup was created and not (yet) reverted.");
			}


			// Backup the graphics settings cause changing the platform sometimes modifies them
			BackupFile(projectFolder + "/ProjectSettings/GraphicsSettings.asset");

			Debug.Log($"{LOG_PREFIX} pre process build. backup");

			if (headlessSettings.valueAudio)
			{
				// Disable audio if this feature is enabled in the settings

				BackupFile(projectFolder + "/ProjectSettings/AudioManager.asset");

				var audioManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset")[0];
				var serializedManager = new SerializedObject(audioManager);
				var prop = serializedManager.FindProperty("m_DisableAudio");
				prop.boolValue = true;
				serializedManager.ApplyModifiedProperties();

				Debug.Log($"{LOG_PREFIX} Disabled audio rendering\nHeadless Builder (v{Headless.version})");
			}

			if (headlessSettings.valueGI)
			{
				// Disable global illumination if this feature is enabled in the settings

				if (UnityEditor.EditorSettings.serializationMode == UnityEditor.SerializationMode.ForceText)
				{

					foreach (var sceneAsset in sceneAssets)
					{
						BackupFile(projectFolder + "/" + sceneAsset);
						ProcessScene(projectFolder + "/" + sceneAsset);
					}

					Debug.Log($"{LOG_PREFIX} Disabled global illumination\nHeadless Builder (v{Headless.version})");

				}
				else
				{
					Debug.LogWarning($"{LOG_PREFIX} Disabling global illumination was skipped, because the serialization mode is not set to 'Force Text'\nHeadless Builder (v" + Headless.version + ")");
				}
			}

			if (!debugBuild && headlessSettings.valueDummy)
			{
				// Replace assets with dummies if this feature is enabled in the settings
				//ReplaceAssetsWithDummies();
				Debug.Log($"{LOG_PREFIX} pre process build. dummy replace disabled");
			}

			// Backup the project settings to revert defines later
			BackupFile(projectFolder + "/ProjectSettings/ProjectSettings.asset");

			if (HeadlessEditorHelper.EnableCleaningInHeadlessBuilder)
				HeadlessCleaner.CleanEverything();

			Debug.Log($"{LOG_PREFIX} pre process build. cleaning complete");

			// Reload the assets to reflect the changes
			AssetDatabase.Refresh();

			Debug.Log($"{LOG_PREFIX} pre process build. asset database refresh complete");


			if (HeadlessEditorHelper.EnableBuildBundlesBeforeCompile)
			{
				var success = TBR.Bundles.Editor.AddressablesBuildUtils.BuildAddressables();
				if (!success)
					throw new Exception($"{LOG_PREFIX} addressables build failed");
			}

			if (manualBuild && currentScene.Length > 0)
			{
				// If this is a manual build, reload the scene if a scene was loaded
				EditorSceneManager.OpenScene(currentScene);
			}

			SetProgress("BUILD");

			Debug.Log($"{LOG_PREFIX} pre process build. complete");

			return true;
		}
		catch (Exception e)
		{
			Debug.LogError(e);
			return false;
		}
	}

	static bool PerformBuild()
	{

		try
		{
			
			var buildFile = NormalizePath(buildPath + "/" + buildExecutable);
			Debug.Log($"{LOG_PREFIX} perform build. could={cloudBuild} debug={debugBuild} file={buildFile} scenes={string.Join(",", sceneList)} target={buildTarget} options={buildOptions}");
			if (!cloudBuild && !debugBuild)
			{
				// If this is not a cloud build, start building the player
				// Unity Cloud Build will do this by itself, so we don't have to
				Debug.Log($"{LOG_PREFIX} perform build. start");

				var report = BuildPipeline.BuildPlayer(sceneList, buildFile, buildTarget, buildOptions);

				Debug.Log($"{LOG_PREFIX} perform build. complete. result={report.summary.result}");

				HeadlessEditorHelper.ShowBuildReportSummary(report);

#if UNITY_2018_1_OR_NEWER
				if (((BuildReport)report).summary.result != BuildResult.Succeeded)
				{
					return false;
				}
#else
				if (((String) error).Length > 0) {
					return false;
				}
#endif
				SetProgress("BACKUP");
			}
			else
			{
				Debug.Log($"{LOG_PREFIX} perform build. skipped");
			}

			return true;
		}
		catch (Exception e)
		{
			UnityEngine.Debug.LogError(e);
			return false;
		}
	}

	static bool PlayBuild()
	{
		try
		{
			if (debugBuild)
			{
				HeadlessProgress.Hide();
				HeadlessDebug.StartDebug();
			}

			return true;
		}
		catch (Exception e)
		{
			UnityEngine.Debug.LogError(e);
			return false;
		}
	}

	//TBR inject (extracted method)
	public static void ReplaceAssetsWithDummies()
	{
		if (string.IsNullOrEmpty(dummyFolder))
			PrepareVariables();

		UnityEngine.Debug.Log($"[Headless builder] start replacing assets with dummies");
		sizeOriginal = 0;
		sizeDummy = 0;
		replaceCount = 0;
		skipDummy = new List<string>();

		Texture2D[] icons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
		foreach (Texture2D icon in icons)
		{
			skipDummy.Add(NormalizePath(projectFolder + "/" + AssetDatabase.GetAssetPath(icon)));
		}
		icons = PlayerSettings.GetIconsForTargetGroup(BuildPipeline.GetBuildTargetGroup(buildTarget));
		foreach (Texture2D icon in icons)
		{
			skipDummy.Add(NormalizePath(projectFolder + "/" + AssetDatabase.GetAssetPath(icon)));
		}

		FindDummies(dummyFolder);
		ProcessDirectory(assetsFolder);
		UnityEngine.Debug.Log("Replaced " + replaceCount + " assets totaling " + (Mathf.Round(sizeOriginal / 1024 / 1024)) + " MB with " + (Mathf.Round(sizeDummy / 1024 / 1024)) + " MB worth of dummy files\nHeadless Builder (v" + Headless.version + ")");
	}

	//TBR inject
	public static void RemoveUnusedFolders()
	{
		if (string.IsNullOrEmpty(assetsFolder))
			PrepareVariables();

		UnityEngine.Debug.Log($"[Headless builder] start removeing folders");
		HeadlessCleaner.CleanDirRecursive(assetsFolder);
		UnityEngine.Debug.Log($"[Headless builder] complete removeing folders");
	}

	public static bool RestoreBuild()
	{
		try
		{
			if (!cloudBuild)
			{
				// If this is not a cloud build, restore the backup
				// Cloud builds discard the project folder after building, so restoring the backup is not needed

				if (backupFolder == null)
				{
					PrepareVariables();
				}

				RestoreDirectory(backupFolder);

				if (headlessSettings.valueBackup)
				{
					Directory.Delete(backupFolder, true);
				}
				if (File.Exists(backupFolder + "/lock.txt"))
				{
					File.Delete(backupFolder + "/lock.txt");
				}

				Debug.Log("Successfully preserved assets and scenes\nHeadless Builder (v" + Headless.version + ")");

				//AssetDatabase.Refresh();
			}

			if (manualBuild && !debugBuild && currentScene.Length > 0)
			{
				// If this is a manual build, reload the scene if a scene was loaded
				EditorSceneManager.OpenScene(currentScene);
			}

			SetProgress("REVERT");

			return true;
		}
		catch (Exception e)
		{
			UnityEngine.Debug.LogError(e);
			return false;
		}
	}

	static bool RevertBuild()
	{
		try
		{
			if (!cloudBuild && !debugBuild)
			{
				// If this is not a cloud build, revert to the original build target if we saved one earlier

				if (regularTarget != BuildTarget.NoTarget && regularTargetGroup != BuildTargetGroup.Unknown)
				{
					if (!regularTarget.Equals(EditorUserBuildSettings.activeBuildTarget))
					{
						HeadlessAPI.VersionedSwitchActiveBuildTarget(regularTargetGroup, regularTarget);
					}
				}
			}

			return true;
		}
		catch (Exception e)
		{
			UnityEngine.Debug.LogError(e);
			return false;
		}
	}

	public static void Postprocess(string path)
	{
		HeadlessCleaner.CleanBuildJunk(path);

		if (HeadlessEditorHelper.DisableExtraFiles)
			return;

		SetProgress("POSTPROCESS");

		if (cloudBuild)
		{
			// If this is a cloud build, rename files and folders
			var subDirs = Directory.GetDirectories(path);
			foreach (var subDir in subDirs)
			{
				// For Windows and Linux:
				if (subDir.EndsWith("_Data"))
				{
					String oldName = NormalizePath(subDir).Replace(NormalizePath(path), "");
					oldName = oldName.Substring(1, oldName.IndexOf("_data") - 1);
					if (!oldName.Equals(buildName))
					{
						if (File.Exists(path + "/" + oldName + buildExtension))
						{
							File.Move(path + "/" + oldName + buildExtension, path + "/" + buildExecutable);
							Directory.Move(subDir, path + "/" + buildName + "_Data");
						}
					}
				}
				// For OSX:
				if (subDir.EndsWith(".app"))
				{
					String oldName = NormalizePath(subDir).Replace(NormalizePath(path), "");
					oldName = oldName.Substring(1, oldName.IndexOf(".app") - 1);
					if (!oldName.Equals(buildName))
					{
						if (File.Exists(subDir + "/Contents/MacOS/" + oldName))
						{
							File.Move(subDir + "/Contents/MacOS/" + oldName, subDir + "/Contents/MacOS/" + buildExecutable);
							Directory.Move(subDir, path + "/" + buildName + ".app");
						}
					}
				}
			}
		}

		if (!debugBuild)
		{
			string oldBuildExecutable = buildExecutable;
			if (headlessSettings.valuePlatform == HeadlessEditor.WINDOWS)
			{
				// For Windows builds, rename the executable to prevent direct execution

				if (File.Exists(path + "/" + buildExecutable))
				{

					string newBuildExecutable = buildName + ".bin";
					if (File.Exists(path + "/" + newBuildExecutable))
					{
						File.Delete(path + "/" + newBuildExecutable);
					}
					File.Move(path + "/" + buildExecutable, path + "/" + newBuildExecutable);

					StreamWriter binaryWriter = new StreamWriter(path + "/" + buildExecutable, false);
					binaryWriter.Write("MZ");
					binaryWriter.Close();

					buildExecutable = newBuildExecutable;

				}
			}

			string consoleSuffix = "";
			if (headlessSettings.valueConsole)
			{
				consoleSuffix = " -logFile";
			}


			// Create readme file
			String nl = "";
			if (headlessSettings.valuePlatform == HeadlessEditor.WINDOWS)
			{
				nl = "\r\n";
			}
			else
			{
				nl = "\n";
			}

			StreamWriter readmeWriter = new StreamWriter(path + "/" + "Readme_" + headlessSettings.profileName + ".txt", false);
			readmeWriter.Write("This build only supports headless mode." + nl);

			if (headlessSettings.valuePlatform != HeadlessEditor.LINUX)
			{
				readmeWriter.Write("Do not run " + oldBuildExecutable + " directly." + nl);
			}

			readmeWriter.Write("" + nl);
			readmeWriter.Write("INSTRUCTIONS" + nl);
			readmeWriter.Write("To start " + PlayerSettings.productName + " in headless mode, run:" + nl);

			if (headlessSettings.valuePlatform == HeadlessEditor.WINDOWS)
			{
				readmeWriter.Write("\t" + packageName + ".bat" + nl);
			}
			else if (headlessSettings.valuePlatform == HeadlessEditor.OSX)
			{
				readmeWriter.Write("\t" + "sh " + packageName + ".sh" + nl);
			}
			else if (headlessSettings.valuePlatform == HeadlessEditor.LINUX)
			{
				readmeWriter.Write("\t" + "./" + packageName + ".sh" + nl);
			}
			readmeWriter.Write("or run:" + nl);
			if (headlessSettings.valuePlatform == HeadlessEditor.WINDOWS)
			{
				readmeWriter.Write("\t" + buildExecutable + " -batchmode -nographics" + consoleSuffix + nl);
			}
			else if (headlessSettings.valuePlatform == HeadlessEditor.OSX)
			{
				readmeWriter.Write("\t" + "./" + buildExecutable + ".app/Contents/MacOS/" + buildExecutable + " -batchmode -nographics" + consoleSuffix + nl);
			}
			else if (headlessSettings.valuePlatform == HeadlessEditor.LINUX)
			{
				readmeWriter.Write("\t" + "./" + buildExecutable + " -batchmode -nographics" + consoleSuffix + nl);
			}

			if (headlessSettings.valuePlatform == HeadlessEditor.LINUX)
			{
				readmeWriter.Write("" + nl);
				readmeWriter.Write("You might have to grant execute permissions first by running:" + nl);
				readmeWriter.Write("\t" + "sudo chmod +x " + packageName + ".sh" + nl);
				readmeWriter.Write("\t" + "sudo chmod +x " + buildExecutable + nl);
			}

			readmeWriter.Close();


			// Create script files
			if (headlessSettings.valuePlatform == HeadlessEditor.WINDOWS)
			{

				StreamWriter scriptWriter = new StreamWriter(path + "/" + packageName + ".bat", false);
				scriptWriter.Write("@ECHO OFF\r\n");
				scriptWriter.Write(buildExecutable + " -batchmode -nographics" + consoleSuffix + "\r\n");
				scriptWriter.Close();

			}
			else if (headlessSettings.valuePlatform == HeadlessEditor.OSX || headlessSettings.valuePlatform == HeadlessEditor.LINUX)
			{

				StreamWriter scriptWriter = new StreamWriter(path + "/" + packageName + ".sh", false);
				scriptWriter.Write("#!/bin/bash\n");
				if (headlessSettings.valuePlatform == HeadlessEditor.OSX)
				{
					scriptWriter.Write("./" + buildExecutable + ".app/Contents/MacOS/" + buildExecutable + " -batchmode -nographics" + consoleSuffix + "\n");
				}
				else if (headlessSettings.valuePlatform == HeadlessEditor.LINUX)
				{
					scriptWriter.Write("./" + buildExecutable + " -batchmode -nographics" + consoleSuffix + "\n");
				}
				scriptWriter.Close();

				/*if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor) {
					ProcessStartInfo procBasic = new ProcessStartInfo ();
					procBasic.FileName = "open";
					procBasic.WorkingDirectory = path;
					procBasic.Arguments = "chmod +x " + packageName + ".sh";
					procBasic.WindowStyle = ProcessWindowStyle.Minimized;
					procBasic.CreateNoWindow = true;
					Process.Start (procBasic);

					ProcessStartInfo procSudo = new ProcessStartInfo ();
					procSudo.FileName = "open";
					procSudo.WorkingDirectory = path;
					procSudo.Arguments = "sudo chmod +x " + packageName + ".sh";
					procSudo.WindowStyle = ProcessWindowStyle.Minimized;
					procSudo.CreateNoWindow = true;
					Process.Start (procSudo);
				}*/

			}
		}

		if (manualBuild)
		{
			// If this is a manual build, open the target folder
			HeadlessExplore.Open(path);
		}

		HeadlessCallbacks.InvokeCallbacks("HeadlessBuildSuccess");
	}


	// HELPERS

	// This function finds dummies inside the dummy folder and remembers them for later use
	private static void FindDummies(string dummyFolder)
	{
		List<string> dummyAssets = new List<string>();

		var files = Directory.GetFiles(dummyFolder);
		foreach (var dummyFile in files)
		{
			String extension = GetFileExtension(dummyFile);
			if (!extension.Equals(".meta") && !extension.Equals(".txt"))
			{
				dummyAssets.Add(extension);
			}
		}

		dummyExtensions = dummyAssets.ToArray();

	}

	// This function processes a directory so its qualified contents may be replaced with dummies
	private static void ProcessDirectory(string processFolder)
	{
		var files = Directory.GetFiles(processFolder);
		foreach (var processFile in files)
		{
			ProcessFile(processFile);
		}
		var subDirs = Directory.GetDirectories(processFolder);
		foreach (var subDir in subDirs)
		{
			ProcessDirectory(subDir);
		}
	}

	// This function processes a file so that if it qualifies it may be replaced with a dummy
	private static void ProcessFile(string processFile)
	{
		if (!skipDummy.Contains(NormalizePath(processFile)))
		{
			if (!File.Exists(processFile + ".headless~"))
			{
				//remove maybe
				/*if (System.Array.IndexOf(dummyExtensions, GetFileExtension(processFile)) > -1)
				{
					ReplaceWithDummy(processFile);
				}*/
				
				//TBR INJECTION
				if(GetFileExtension(processFile) == ".prefab")
					HeadlessCleaner.TryCleanPrefab(processFile, NormalizePath, BackupFile);
			}
		}
	}

	// This function replaces a file with a dummy
	private static void ReplaceWithDummy(string processFile)
	{

		String dummyFile = dummyFolder + "/bin" + GetFileExtension(processFile);

		if (NormalizePath(processFile).Equals(NormalizePath(dummyFile)))
		{
			return;
		}

		BackupFile(processFile);

		sizeOriginal += new FileInfo(processFile).Length;
		sizeDummy += new FileInfo(dummyFile).Length;
		replaceCount++;

		FileUtil.ReplaceFile(dummyFile, processFile);

	}

	// This functions creates a backup of a file that is about to be altered
	private static void BackupFile(string processFile)
	{
		//TBR injection
		if (HeadlessEditorHelper.DisableBackupFile)
			return;

		if (!GetFileExtension(processFile).Equals(".meta"))
		{
			String metaFile = processFile + ".meta";
			if (File.Exists(metaFile))
			{
				BackupFile(metaFile);
			}
		}

		String destinationFile = NormalizePath(processFile).Replace(projectFolder, backupFolder);
		DirectoryInfo destinationFolder = Directory.GetParent(destinationFile);
		if (!destinationFolder.Exists)
		{
			destinationFolder.Create();
		}
		destinationFile = destinationFolder + "/" + Path.GetFileName(processFile);

		FileUtil.CopyFileOrDirectory(processFile, destinationFile);

	}

	// This function normalizes a path so that it can be compared against another path
	private static string NormalizePath(string path)
	{
		//TBR fix
#if UNITY_EDITOR_LINUX
#else
		path = path.ToLower();
#endif
		string normalized = path.Replace('\\', '/');
		string result = "";

		while (normalized.StartsWith("/"))
		{
			result += "/";
			normalized = normalized.Substring(1);
		}

		string[] parts = normalized.Split('/');
		for (int i = 0; i < parts.Length; i++)
		{
			if (i > 0)
			{
				result += "/";
			}
			result += parts[i];

			result = GetCaseSensitivePath(result);
		}

		return result;
	}

	internal static string GetCaseSensitivePath(string path)
	{
#pragma warning disable 0168
		try
		{
			var root = Path.GetPathRoot(path);
			try
			{
				foreach (var name in path.Substring(root.Length).Split(Path.DirectorySeparatorChar))
					root = Directory.GetFileSystemEntries(root, name).First();
			}
			catch (Exception e)
			{
				// UnityEngine.Debug.Log("Path not found: " + path);
				root += path.Substring(root.Length);
			}
			return root;
		}
		catch (Exception e)
		{
			UnityEngine.Debug.LogWarning("Skipped path verification for: '" + path + "'\nHeadless Builder (v" + Headless.version + ")");
			return path;
		}
#pragma warning restore 0168
	}

	// This function restores a backup
	private static void RestoreDirectory(string restoreFolder)
	{
		if (HeadlessEditorHelper.DisableBackupFile)
		{
			Debug.Log($"{LOG_PREFIX} restore directory disabled. use git");
			return;
		}
		var files = Directory.GetFiles(restoreFolder);
		foreach (var restoreFile in files)
		{
			RestoreFile(restoreFile);
		}
		var subDirs = Directory.GetDirectories(restoreFolder);
		foreach (var subDir in subDirs)
		{
			RestoreDirectory(subDir);
		}
	}

	// This function restores a file inside a backup
	private static void RestoreFile(string restoreFile)
	{
		if (restoreFile.EndsWith("lock.txt"))
		{
			return;
		}
		//TBR injection
		if (HeadlessEditorHelper.DisableRestoreFile)
			return;

		String destinationFile = NormalizePath(restoreFile).Replace(backupFolder, projectFolder);
		DirectoryInfo destinationFolder = Directory.GetParent(destinationFile);
		destinationFile = destinationFolder + "/" + Path.GetFileName(restoreFile);

		FileUtil.ReplaceFile(restoreFile, destinationFile);
	}

	// This function processes a scene and applies modifications to it
	private static void ProcessScene(string sceneFile)
	{

		String[] contents = File.ReadAllLines(sceneFile);

		for (int i = 0; i < contents.Length; i++)
		{
			if (headlessSettings.valueGI)
			{
				contents[i] = contents[i].Replace("m_EnableBakedLightmaps: 1", "m_EnableBakedLightmaps: 0");
				contents[i] = contents[i].Replace("m_EnableRealtimeLightmaps: 1", "m_EnableRealtimeLightmaps: 0");
				contents[i] = contents[i].Replace("m_AlbedoBoost: 1", "m_AlbedoBoost: 0");
				contents[i] = contents[i].Replace("m_GIWorkflowMode: 0", "m_GIWorkflowMode: 1");
			}
		}

		File.WriteAllLines(sceneFile, contents);

	}

	// This functions changes the progress bar window
	private static void SetProgress(string newStep)
	{
		if (progressWindow == null)
		{
			return;
		}

		if (!cloudBuild && !batchBuild)
		{
			progressWindow.minSize = new Vector2(100, 50);
			progressWindow.SetProgress(newStep);
			progressWindow.Repaint();
		}
	}

	// This function removes any illegal characters from a filename
	public static string CleanFileName(string fileName)
	{
		return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
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

}