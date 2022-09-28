using System;
using System.Collections.Generic;
using ArchCore.Utils;
using ArchCore.Utils.Executions;
using UnityEngine;

namespace ArchCore.MVP
{
	public interface IPresenter
	{
		event Action<IPresenter> OnClose;
		void Setup(Action<View> closeAction);
		IEnumerator<Transform> SubviewContainer { get; }
		View BaseView { get; }
	}

	public abstract class Presenter : IDisposablesContainer, IDisposable, IPresenter
	{
		readonly bool isLogsShow = false;

		protected List<IDisposable> disposables = new List<IDisposable>();
	
		private View baseView;
		private Action<View> viewClosing;

		View IPresenter.BaseView
		{
			get
			{
				if (baseView == null)
				{
					throw new Exception("View not created or already destroyed");
				}

				return baseView;
			}
		}

		IEnumerator<Transform> IPresenter.SubviewContainer => baseView.SubviewContainer;
		
		protected Presenter(View view)
		{
			if (isLogsShow)
				Debug.Log($"-[#presenter] {GetType().Name} Created");

			baseView = view;
		}
		
		public event Action<IPresenter> OnClose;
		
		void IPresenter.Setup(Action<View> closeAction)
		{
			viewClosing = closeAction;
			Init();
			baseView.Init(this);
			baseView.Show();
		}
		
		protected virtual void Init()
		{
		}

		protected virtual void Closing()
		{
		}

		private void ViewClosed()
		{
			((IDisposable) this).Dispose();
			baseView = null;
		}
		
		void IDisposable.Dispose() {
			for (int i = 0; i < disposables.Count; i++) {
				disposables[i].Dispose();
			}
			disposables.Clear();
		}

		public void RegisterForDispose(IDisposable disposable)
		{
			disposables.Add(disposable);
		}
		
		~Presenter()
		{
			if (isLogsShow)
				Debug.Log($"-[#presenter] {GetType().Name} Destroyed");
		}

		public void Close()
		{
			
			if (!baseView)
			{
				throw new Exception("View not created or already destroyed");
			}


			if (isLogsShow)
				Debug.Log($"-[#presenter] {GetType().Name} Closing");
			
			Closing();
			
			var closing = baseView.Hide() > new ActionExe(()=>viewClosing(baseView)) > new ActionExe(ViewClosed);

			closing.Execute();
			
			OnClose?.Invoke(this);
		}
		
	}

	public abstract class Presenter<TView> : Presenter
		where TView : View
	{
		private TView view;

		protected TView View
		{
			get
			{
				if (!view)
				{
					throw new Exception("View (" + typeof(TView) + ") not created or already destroyed");
				}

				return view;
			}
		}

		public bool ViewExists => view != null;
		
		protected Presenter(TView view) : base(view)
		{
			this.view = view;
		}

	}
}