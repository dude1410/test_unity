using System.Collections;
using UnityEditor;
using UnityEngine;

namespace TBR.HeadlessServer
{
	public static class HeadlessEditorPrefs
	{
		const string KEY_DEVBUILD = "HEADLESS_BUILD_DEV_MODE";
		public static bool IsDevBuild
		{
			get => EditorPrefs.GetBool(KEY_DEVBUILD);
			set
			{
				EditorPrefs.SetBool(KEY_DEVBUILD, value);
				HeadlessBuilder.developmentBuild = value;
			}
		}

		const string KEY_PROFILE = "HEADLESSBUILDER_LASTPROFILE";

		public static string LastProfile
		{
			get => EditorPrefs.GetString(KEY_PROFILE, HeadlessProfiles.defaultProfile);
			set
			{
				EditorPrefs.SetString(KEY_PROFILE, value);
				HeadlessProfiles.currentProfile = value;
			}
		}
		
		
		const string KEY_MULTIPLAY = "HEADLESSBUILDER_MULTIPLAY";

		public static bool IsMultiplay
		{
			get => EditorPrefs.GetBool(KEY_MULTIPLAY,false);
			set
			{
				EditorPrefs.SetBool(KEY_MULTIPLAY, value);
				HeadlessBuilder.developmentBuild = value;
			}
		}
	}
}