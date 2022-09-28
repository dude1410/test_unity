using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArchCore.Utils;

namespace ArchCore.MVP
{
	public interface IPopupPresenter<in TArgs, TResult> 
		where TArgs : PopupArgs
		where TResult : PopupResult
	{
		AsyncTask<TResult> Init(TArgs args);
	}
	
	public abstract class PopupPresenter<TView, TArgs, TResult> 
		: Presenter<TView>, IPopupPresenter<TArgs, TResult>
		where TView : View
		where TArgs : PopupArgs
		where TResult : PopupResult
	{
		
		protected AsyncTask<TResult> popupTask;

		protected PopupPresenter(TView view) : base(view)
		{
		}

		public abstract AsyncTask<TResult> Init(TArgs args);
	}

	public class PopupArgs
	{
		
	}
	
	public class PopupResult
	{
		
	}
}