using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using NativeSM = UnityEngine.SceneManagement.SceneManager;
using Object = System.Object;
using ArchCore.Utils;

namespace ArchCore.SceneControl
{

	public class SceneManager : ISceneManager
	{
		private bool isLoading;
		private string currentScene;
		private ISceneController currentSceneController;

		public ISceneController CurrentScene => currentSceneController;
		public string CurrentSceneName => CurrentScene.Name;
		
		public event Action<ISceneController> OnSceneChanged;

		public SceneManager()
		{
		}
		
		public ProgressAsyncTask<ISceneController> LoadScene(string name)
		{
			ProgressAsyncTask<ISceneController> task = new ProgressAsyncTask<ISceneController>();

			if (isLoading)
			{
				Debug.LogError($"Can't load new scene '{name}' while loading other");
				task.Fail(new Exception());
			}
			
			//TODO: SAFE CHECK IF SCENE EXISTS
			
			isLoading = true;
			CurrentScene?.Dispose();
			AsyncOperation operation = NativeSM.LoadSceneAsync(name, LoadSceneMode.Single);
			operation.completed +=
				aop =>
				{
					isLoading = false;
					currentScene = name;

					GameObject[] roots = NativeSM.GetSceneByName(name).GetRootGameObjects();
					ISceneController sceneController = null;

					foreach (GameObject gameObject in roots)
					{
						sceneController = gameObject.GetComponent<ISceneController>();
						if (sceneController != null)
						{
							break;
						}
					}
					sceneController.Setup(name);
					currentSceneController = sceneController;
					task.Success(sceneController);
					OnSceneChanged?.Invoke(sceneController);
				};
			
			ProgressAsyncTask pt = new ProgressAsyncTask();
			pt.OnProgress(task.Progress);
			new YieldTask(operation.TrackProgress(pt)).Start();
			
			return task;
		}
		
		[Obsolete("There is no need to unload when using LoadSceneMode.Single.")]
		public ProgressAsyncTask UnloadCurrentScene()
		{

			ProgressAsyncTask task = new ProgressAsyncTask();

			if (currentScene.IsNullOrEmpty())
			{
				task.Success();
				return task;
			}

			AsyncOperation operation = NativeSM.UnloadSceneAsync(currentScene);

			new YieldTask(operation.TrackProgress(task)).Start();

			operation.completed +=
				aop =>
				{
					currentScene = null;
					task.Success();
				};
			
			return task;
		}

		
	}
}