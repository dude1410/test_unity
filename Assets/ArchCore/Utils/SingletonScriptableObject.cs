using System.Linq;
using UnityEngine;

namespace ArchCore.Utils
{
	public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
	{
		static T _instance;

		private void OnEnable()
		{
			_instance = this as T;
		}

		public static T Instance
		{
			get
			{
				if (!_instance)
				{
					var objs = Resources.FindObjectsOfTypeAll<T>();

					if (objs.Length == 0)
					{
						Debug.LogError($"There is no instance of {typeof(T)}");
					}
					else if (objs.Length > 1)
					{
						Debug.LogError($"There is multiple instances of {typeof(T)}");
					}


					_instance = objs.FirstOrDefault();
				}

				return _instance;
			}
		}
	}

}
