using System;
using System.Collections.Generic;
using ArchCore.MVP;
using ArchCore.CameraControl;

namespace ArchCore.SceneControl
{
	public interface ISceneController : IDisposable
	{
		void Setup(string name);
		
		TPresenter ShowView<TPresenter>(bool concurrent = true, bool restartIfOpen = true) where TPresenter : Presenter;
		void CloseAll();
		IEnumerable<Presenter> ActivePresenters { get; }

		CameraController Camera { get; }
		string Name { get; }

		bool CheckForPresenter<T>() where T : Presenter;
		bool CheckForView<T>() where T : View;
		
		event Action<Presenter> OnViewOpen;
	}
	
	
}
