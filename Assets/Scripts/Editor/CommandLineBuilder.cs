using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CommandLineBuilder
{
	public static void Build()
	{
		try
		{
			Debug.Log($"start build");
			string[] sceneList = {
				"Assets/Scenes/SampleScene.unity"
			};

			var buildPath = GetArgumentString("-headlessPath");
			if (!Directory.Exists(buildPath))
			{
				Directory.CreateDirectory(buildPath);
			}

			var buildName = "build";
			var buildExtension = ".x86_64";
			var buildExecutable = buildName + buildExtension;
			var buildFile = NormalizePath(buildPath + "/" + buildExecutable);

			BuildTarget buildTarget = BuildTarget.StandaloneLinux64;
			BuildOptions buildOptions = BuildOptions.EnableHeadlessMode;
			var report = BuildPipeline.BuildPlayer(sceneList, buildFile, buildTarget, buildOptions);
			
			ShowBuildReportSummary(report);
			Debug.Log($"complete build");
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		finally
		{
			Exit();
		}
	}

	static void Exit()
	{
		Debug.Log($"exit");
		EditorApplication.Exit(0);
	}


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
			UnityEngine.Debug.LogWarning($"ERROR: {e}");
			return path;
		}
#pragma warning restore 0168
	}

	static void ShowBuildReportSummary(BuildReport report)
	{
		Debug.Log($"build summary. result={report.summary.result}");
		Debug.Log($"build summary. files:\n {string.Join("\n", report.files)}");
		Debug.Log($"build summary. steps:\n {string.Join("\n", report.steps)}");
		Debug.Log($"build summary. scenes:\n {string.Join("\n", report.scenesUsingAssets.Select(s => s.ToString()))}");
		Debug.Log($"build summary. packed assets:\n {string.Join("\n", report.packedAssets.Select(s => s.ToString()))}");
	}


	static string GetArgumentString(string val, string defaultValue = "")
	{
		var args = Environment.GetCommandLineArgs();
		var index = Array.IndexOf(args, val);
		if (index < 0 || index >= args.Length - 1)
			return defaultValue;

		return args[index + 1];
	}
}