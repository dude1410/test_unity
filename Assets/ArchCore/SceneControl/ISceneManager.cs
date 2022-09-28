using System;
using System.Collections;
using System.Collections.Generic;
using ArchCore.Utils;
using UnityEngine;

namespace ArchCore.SceneControl
{

	public interface ISceneManager
	{
		ISceneController CurrentScene { get; }
		string CurrentSceneName { get; }
		ProgressAsyncTask<ISceneController> LoadScene(string name);
		event Action<ISceneController> OnSceneChanged;
	}
}