using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ArchCore.MVP;
using ArchCore.MVP.Utils;
using ArchCore.CameraControl;
using Zenject;

namespace ArchCore.SceneControl
{
	public class SceneInstaller : MonoInstaller<SceneInstaller>, ISceneController
	{
		private class SceneViewManager : AbstractViewManager
		{
			private List<Presenter> activePresenters = new List<Presenter>();
			
			

			public IEnumerable<Presenter> ActivePresenters => activePresenters;
			
			public SceneViewManager(List<(Type view,Type presenter)> viewPresenterPairs, Transform viewContainer, PresenterFactory presenterFactory) : base(viewContainer, presenterFactory)
			{
				AutoRegisterViews(viewPresenterPairs);
			}
						
			public TPresenter ShowView<TPresenter>(bool concurrent, bool restartIfOpen) where TPresenter : Presenter
			{
				//Debug.Log($"{this} ShowView={typeof(TPresenter)} concurent={concurrent} restart={restartIfOpen}");
				if (concurrent)
				{
					Presenter sameOpen = null;
					if (!restartIfOpen)
					{
						sameOpen = activePresenters.Find(p => p is TPresenter);
						activePresenters.Remove(sameOpen);
					}
					
					CloseAllActiveViews();

					if (sameOpen != null)
					{
						activePresenters.Add(sameOpen);
						return (TPresenter) sameOpen;
					}
				}

				var existPresenter = activePresenters.Find(p => p is TPresenter);
				if (existPresenter != null)
				{
					if (restartIfOpen)
						existPresenter.Close();
					else
						return (TPresenter) existPresenter;
				}

				IPresenter presenter = Create<TPresenter>();
				presenter.BaseView.gameObject.SetActive(true);
				activePresenters.Add((Presenter) presenter);
				presenter.OnClose += p =>
				{
					activePresenters.Remove((Presenter) p);
				};
				return (TPresenter) presenter;
			}

			public void CloseAllActiveViews()
			{
				while (activePresenters.Count > 0)
				{
					activePresenters[0].Close();
				}
			}

			protected override void DestroyView(View view)
			{
				view.gameObject.SetActive(false);
			}
		}
		
		[SerializeField] private CameraController sceneCamera;
		

		public CameraController Camera => sceneCamera;
		public string Name { get; private set; }
		
		public event Action<Presenter> OnViewOpen;

		[SerializeField] private List<View> sceneBindings;

		
		private SceneViewManager viewManager; 
		private PresenterFactory presenterFactory;
		private List<(Type view, Type presenter)> registrationList;

		public void Setup(string name)
		{
			Name = name;
		}

		public bool CheckForPresenter<T>() where T : Presenter
		{
			return registrationList.Exists(p => p.presenter == typeof(T));
		}
		
		public bool CheckForView<T>() where T : View
		{
			return registrationList.Exists(p => p.view == typeof(T));
		}
		
		public override void InstallBindings()
		{
			presenterFactory = new PresenterFactory(Container);
			Container.Bind<PresenterFactory>().FromInstance(presenterFactory).AsSingle().NonLazy();
			
			MethodInfo bindWrapper = GetType().GetMethod("BindWrapper", BindingFlags.Instance | BindingFlags.NonPublic);

			registrationList = new List<(Type view, Type presenter)>();
			
			foreach (var binding in sceneBindings)
			{
				if(!binding) continue;
				
				Type type = binding.GetType();
				Type presenterType = type.BaseType.GetGenericArguments()[0];
				
				MethodInfo bindingMethod = bindWrapper.MakeGenericMethod(type, presenterType);
				bindingMethod.Invoke(this, new object[]{binding});
				
				registrationList.Add((type, presenterType));
			}
			
			bindWrapper = GetType().GetMethod("BindResourceWrapper", BindingFlags.Instance | BindingFlags.NonPublic);

//			TODO: implement prefab bindings
//			var prefabBindings = AutoRegisterViewAttribute.GetViews(gameObject.scene.name);
//			
//			
//			foreach (var binding in prefabBindings)
//			{
//				Debug.LogError("Prefab bindings are not fully implemented!");
//				
//				Type type = binding.view;
//				Type presenterType = type.BaseType.GetGenericArguments()[0];
//				
//				MethodInfo bindingMethod = bindWrapper.MakeGenericMethod(type, presenterType);
//				bindingMethod.Invoke(this, new object[]{binding.path});
//				
//				registrationList.Add((type, presenterType));
//			}
			
			viewManager = new SceneViewManager(registrationList, null, presenterFactory);
			
		}

		void BindWrapper<TView, TPresenter>(TView view)
		{
			Container.BindSceneViewPresenter<TView, TPresenter>(view);
		}

		void BindResourceWrapper<TView, TPresenter>(string path)
		{
			Container.BindViewPresenter<TView, TPresenter>(path);
		}

		public TPresenter ShowView<TPresenter>(bool concurrent = true, bool restartIfOpen = true) where TPresenter : Presenter
		{
			var presenter = viewManager.ShowView<TPresenter>(concurrent, restartIfOpen);
			OnViewOpen?.Invoke(presenter);
			return presenter;
		}	
		
		public void CloseAll()
		{
			viewManager.CloseAllActiveViews();
		}

		public IEnumerable<Presenter> ActivePresenters => viewManager.ActivePresenters;
		
		public void Dispose()
		{
			CloseAll();
		}

	}


}