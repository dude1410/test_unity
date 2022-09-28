using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ArchCore.Utils;

namespace ArchCore.MVP
{
    public abstract class AbstractViewManager
    {
        protected delegate Presenter CreateViewDelegate();
        private Dictionary<Type, CreateViewDelegate> createViewActions = new Dictionary<Type, CreateViewDelegate>();
        
        private PresenterFactory presenterFactory;
        protected Transform viewContainer;
        protected AbstractViewManager(Transform viewContainer, PresenterFactory presenterFactory)
        {
            this.presenterFactory = presenterFactory;
            this.viewContainer = viewContainer;
        }
        
        protected void RegisterView<TView, TPresenter>()
            where TView : View<TPresenter>
            where TPresenter : Presenter<TView>
        {
            createViewActions[typeof(TPresenter)] = CreateView<TView, TPresenter>;
        }

        private TPresenter CreateView<TView, TPresenter>()
            where TView : View<TPresenter>
            where TPresenter : Presenter<TView>
        {
            IPresenter presenter = presenterFactory.Create<TPresenter>();
            presenter.Setup(DestroyView);

            return (TPresenter) presenter;
        }
        
        protected void AutoRegisterViews(List<(Type view, Type presenter)> viewPresenterPairs)
        {
            MethodInfo registerMethod = GetType().GetMethod("RegisterView", BindingFlags.Instance | BindingFlags.NonPublic);
            
            if(registerMethod == null) return;

            foreach (var viewPresenterPair in viewPresenterPairs)
            {
                MethodInfo genRegisterMethod = registerMethod.MakeGenericMethod(viewPresenterPair.view, viewPresenterPair.presenter);
                genRegisterMethod.Invoke(this, new object[]{});
            }
        }

        protected T Create<T>() where T : Presenter
        {
            var keyType = typeof(T);
            if (!createViewActions.ContainsKey(keyType))
            {
                throw new ViewNotRegisteredException(keyType, this);
            }

            return (T)createViewActions[keyType]();
        }
        
        protected abstract void DestroyView(View view);

        public override string ToString()
        {
            return $"{GetType()}, Container: {viewContainer?.name}";
        }
    }
}