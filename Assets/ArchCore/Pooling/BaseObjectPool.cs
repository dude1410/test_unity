using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArchCore.Pooling
{
	public abstract class BaseObjectPool
	{
		protected int minSize, maxSize;
		protected abstract void Fill();
		public abstract void Clear();
		protected abstract void Destroy(IPoolObject poolObject);
		
		protected readonly Stack<IPoolObject> pool;
		
		public abstract MonoBehaviour Instantiate();

		protected BaseObjectPool(int minSize, int maxSize)
		{
			this.minSize = minSize;
			this.maxSize = maxSize;
			pool = new Stack<IPoolObject>();
		}
	}

}
