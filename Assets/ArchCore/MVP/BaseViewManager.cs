using System;
using ArchCore.DataStructures.Hierarchy;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArchCore.MVP
{
	public abstract class BaseViewManager : AbstractViewManager
	{
		public event Action<Presenter> OnViewShown;
		
		private readonly HierarchyTree<Presenter> viewHierarchy = new HierarchyTree<Presenter>();
		public IReadOnlyHierarchyTree<Presenter> Hierarchy => viewHierarchy;

		protected BaseViewManager(Transform viewContainer, PresenterFactory presenterFactory) : base(viewContainer, presenterFactory)
		{
		}
		
		public TPresenter ShowView<TPresenter>() where TPresenter : Presenter
		{
			CloseRootView();
			var view = CreateView<TPresenter>();
			viewHierarchy.SetRoot(view);
			OnViewShown?.Invoke(view);
			return view;
		}

		public TPresenter ShowSubView<TPresenter>(Presenter parent) where TPresenter : Presenter
		{
			var view = CreateView<TPresenter>(parent);
			viewHierarchy.AddItem(view, parent);
			OnViewShown?.Invoke(view);
			return view;
		}

		public void CloseRootView()
		{
			viewHierarchy.Root?.Close();
		}
		
		public void CloseSubViews(Presenter view)
		{
			var subs = viewHierarchy.GetSubItemsOf(view);
			while (subs.Count > 0)
			{
				subs[0].Close();
			}
		}
		
		private Transform GetViewContainer(IPresenter parentView)
		{
			if (parentView == null) return viewContainer;
			if (parentView.SubviewContainer == null) return viewContainer;
			parentView.SubviewContainer.MoveNext();
			var container = parentView.SubviewContainer.Current;
			return container ? container : viewContainer;
		}
		
		private TPresenter CreateView<TPresenter>(Presenter parentView = null) where TPresenter : Presenter
		{
			Transform container = GetViewContainer(parentView);
			
			TPresenter presenter = Create<TPresenter>();
			IPresenter ipresenter = presenter;
			ipresenter.BaseView.transform.SetParent(container, false);
			ipresenter.BaseView.transform.SetAsLastSibling();

			presenter.OnClose += p =>
			{
				CloseSubViews((Presenter)p);
				viewHierarchy.RemoveItem(presenter);
			};
			
			return presenter;
		}

		protected override void DestroyView(View view)
		{
			Object.Destroy(view.gameObject);
		}
	}
}