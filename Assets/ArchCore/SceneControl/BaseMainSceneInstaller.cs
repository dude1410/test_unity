using System;
using System.Collections.Generic;
using System.Reflection;
using ArchCore.MVP;
using ArchCore.MVP.Utils;
using Zenject;

namespace ArchCore.SceneControl
{
    public abstract class BaseMainSceneInstaller : MonoInstaller<BaseMainSceneInstaller>
    {
	    protected (List<(Type view, Type presenter)> views, List<(Type view, Type presenter)> popups) AutoViewInstall()
	    {
		    MethodInfo bindWrapper = typeof(BaseMainSceneInstaller).GetMethod("BindWrapper", BindingFlags.Instance | BindingFlags.NonPublic);

		    var viewRegistrationList = new List<(Type, Type)>();
		    var popupRegistrationList = new List<(Type, Type)>();

		    var bindings = AutoRegisterViewAttribute.GetViews(new []{GetType().Assembly});

		    foreach (var binding in bindings)
		    {
			    Type type = binding.view;
			    Type presenterType = type.BaseType.GetGenericArguments()[0];

			    MethodInfo bindingMethod = bindWrapper.MakeGenericMethod(type, presenterType);
			    bindingMethod.Invoke(this, new object[] {binding.path});


			    if (!CheckForPopup())
				    viewRegistrationList.Add((type, presenterType));
			    else
				    popupRegistrationList.Add((type, presenterType));

			    bool CheckForPopup()
			    {
				    Type tType = presenterType;

				    do
				    {
					    if (tType.IsGenericType && tType.GetGenericTypeDefinition() == typeof(PopupPresenter<,,>))
						    return true;

					    tType = tType.BaseType;
				    } while (tType != null);

				    return false;
			    }
		    }

		    return (viewRegistrationList, popupRegistrationList);
	    }

	    void BindWrapper<TView, TPresenter>(string path)
	    {
		    Container.BindViewPresenter<TView, TPresenter>(path);
	    }
	}
}