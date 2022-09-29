using System.Collections;
using System.Collections.Generic;
using ArchCore.Utils;
using UnityEngine;

namespace TBR.HeadlessServer
{
	public class HeadlessRemoveTag : MonoBehaviour
	{
		[SerializeField]
		[TextArea(5, 5)]
		string readMe = @"this is dummy component used as mark for headless builder. this and all childs of this gameobjects will be removed from prefab when building";

		void Awake()
		{
			readMe = string.Empty;
			
#if UNITY_EDITOR && !HEADLESS
			if (HeadlessHelper.IsHeadless() && gameObject)
				Destroy(gameObject);
#elif UNITY_EDITOR
			if (HeadlessHelper.IsHeadless())
				Debug.LogError($"[{GetType().Name}] found inside headless build on {transform.GetPathHierarchy()}. Some bug happened.");
#else
			if (HeadlessHelper.IsHeadless())
				Debug.LogWarning($"[{GetType().Name}] found inside headless build on {transform.GetPathHierarchy()}. Some bug happened.");
#endif
		}
	} 
}