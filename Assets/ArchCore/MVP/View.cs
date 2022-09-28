using System;
using System.Collections.Generic;
using ArchCore.Utils;
using ArchCore.Utils.Executions;
using UnityEngine;

namespace ArchCore.MVP
{

	public abstract class View : MonoBehaviour
	{
		protected bool isLogsShow = false;

		public static string StandardPathFormat => "Views/{0}";
		
		protected Presenter BasePresenter { get; set; }
		

		public virtual IEnumerator<Transform> SubviewContainer => subviewContainer;

		private IEnumerator<Transform> subviewContainer = EnumeratorUtil.Single<Transform>(null);


		public abstract void Init(Presenter presenter);
		
		public BaseExecution Hide()
		{
			OnBeforeClose();
			return TransitionHide > new ActionExe(BeforeDestroy);
		}

		public BaseExecution Show()
		{
			OnBeforeShow();
			return TransitionShow > new ActionExe(OnShowComplete);
		}

		protected virtual BaseExecution TransitionShow => BaseExecution.Null;
		protected virtual BaseExecution TransitionHide => BaseExecution.Null;
		
		
		protected virtual void OnBeforeShow()
		{
			
		}
		
		protected virtual void OnShowComplete()
		{
			
		}
		
		protected virtual void OnBeforeClose()
		{
			
		}
		
		protected  virtual void BeforeDestroy()
		{
			if (isLogsShow)
				Debug.Log($"-[#view] {GetType().Name} Close");

			BasePresenter = null;
			OnBeforeDestroy();
		}
		
		protected virtual void OnBeforeDestroy()
		{
			
		}


	}

	public abstract class View<TPresenter> : View where TPresenter : Presenter
	{
		protected TPresenter Presenter { get; private set; }

		public sealed override void Init(Presenter presenter)
		{
			BasePresenter = presenter;
			Presenter = (TPresenter)presenter;

			if (isLogsShow)
				Debug.Log($"-[#view] {GetType().Name} Init");
		}

		protected sealed override void BeforeDestroy()
		{
			Presenter = null;
			base.BeforeDestroy();
		}
	}
}