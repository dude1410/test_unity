using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArchCore.Pooling
{
	public interface IPoolObject
	{
		event Action<IPoolObject> PoolDestroy;


		void Destroy();
		
		void OnCreated();
		void OnDestroyed();
		void OnSpawned();
		void OnDespawned();
		
	}
}
