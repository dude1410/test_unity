using System;
using System.Collections.Generic;
using ArchCore.MVP;
using UnityEngine;
using ArchCore.Utils;

namespace ArchCore.MVP
{
	public abstract class BasePopupManager : AbstractViewManager
	{
		public event Action<Presenter> OnViewShown;
		public IReadOnlyList<Presenter> Popups => popups;
		
		private readonly Queue<Action> popupQueue = new Queue<Action>();
		private List<Presenter> popups = new List<Presenter>();
		private List<(int order, Presenter popup)> orderedPopups = new List<(int order, Presenter popup)>();

		protected BasePopupManager(Transform viewContainer, PresenterFactory presenterFactory) : base(viewContainer, presenterFactory)
		{
		}

		public TPresenter ShowPopup<TPresenter>(int order = 0) where TPresenter : Presenter
		{
			IPresenter presenter = Create<TPresenter>();
			presenter.BaseView.transform.SetParent(viewContainer, false);
			popups.Add((Presenter)presenter);

			int index = 0;
			for (index = 0; index < orderedPopups.Count; index++)
			{
				if (order >= orderedPopups[index].order)
				{
					break;
				}
			}
			presenter.BaseView.transform.SetSiblingIndex(viewContainer.childCount - index - 1);
			orderedPopups.Insert(index, (order, (Presenter)presenter));
			presenter.OnClose += p =>
			{
				popups.Remove((Presenter) p);
				orderedPopups.RemoveAll(op => op.popup == (Presenter) p);
			};
			OnViewShown?.Invoke((TPresenter)presenter);
			return (TPresenter) presenter;
		}
		
		public AsyncTask<TResult> ShowPopup<TPresenter, TArgs, TResult>(TArgs args, int order = 0)
			where TPresenter : Presenter, IPopupPresenter<TArgs, TResult>
			where TArgs : PopupArgs
			where TResult : PopupResult
		{		
			return ShowPopup<TPresenter>(order).Init(args);
		}
		
		public AsyncTask<TResult> ShowPopupQueued<TPresenter, TArgs, TResult>(TArgs args, int order = 0)
			where TPresenter : Presenter, IPopupPresenter<TArgs, TResult>
			where TArgs : PopupArgs
			where TResult : PopupResult
		{
		
			AsyncTask<TResult> task = new AsyncTask<TResult>();
			
			popupQueue.Enqueue(() =>
			{
				ShowPopup<TPresenter>(order)
					.Init(args).OnSuccess(task.Success).OnFail(task.Fail).OnComplete(e =>
					{
						popupQueue.Dequeue();
						popupQueue.Peek()();
					});
			});
			
			if (popupQueue.Count == 0)
				popupQueue.Peek()();
		
			return task;
		}

		protected override void DestroyView(View view)
		{
			UnityEngine.Object.Destroy(view.gameObject);
		}
	}
}