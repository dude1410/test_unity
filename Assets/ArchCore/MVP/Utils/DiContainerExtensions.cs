using System;
using UnityEngine;
using Zenject;

namespace ArchCore.MVP.Utils
{
	public static class DiContainerExtensions
	{

		public static void BindViewPresenter<TView, TPresenter>(this DiContainer container, string viewPath)
		{
			container.Bind<TView>().FromComponentInNewPrefabResource(viewPath).AsTransient();
			container.Bind<TPresenter>().AsTransient();
		}
		
		public static void BindSceneViewPresenter<TView, TPresenter>(this DiContainer container, TView view) 
		{
			container.Bind<TView>().FromInstance(view);//.AsSingle();
			container.Bind<TPresenter>().AsTransient();
		}

	}
}