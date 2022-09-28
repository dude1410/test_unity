using ArchCore.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArchCore.Pooling
{
    public class Pool
	{
		private readonly Transform storeLocation;
		private readonly Dictionary<string, BaseObjectPool> pools;
		public bool IsReady { get; private set; }

		public Pool(Transform storeLocation)
		{
			this.storeLocation = storeLocation;
			pools = new Dictionary<string, BaseObjectPool>();
		}

		private Queue<YieldTask> fillQueue = new Queue<YieldTask>();
		private YieldTask currentFill;

		public void Append(PoolData[] poolDatas, bool smoothFill = true, Action onFilled = null)
		{
			var task = new YieldTask(Fill(poolDatas, smoothFill, onFilled));

			if (!smoothFill)
			{
				task.Start();
				return;
			}

			if (currentFill == null)
			{
				currentFill = task;
				currentFill.Start();
			}
			else
				fillQueue.Enqueue(task);
		}

		IEnumerator Fill(PoolData[] poolDatas, bool smoothFill, Action onFilled)
		{
			var start = DateTime.UtcNow;
			foreach (var data in poolDatas)
			{
				if (!data.monoSample)
				{		
					Debug.LogError($"Sample for key {data.name} is empty");
				}
				else if (!(data.monoSample is IPoolObject))
				{
					Debug.LogError($"Sample for key {data.name} of type {data.monoSample.GetType()} is not derived from {nameof(IPoolObject)}");
				}
				else
				{
					RegisterGameObjectPool(data.minSize, data.maxSize, data.monoSample, data.name);
				}
				
				if (smoothFill && (DateTime.UtcNow - start).Milliseconds > 10)
				{
					yield return null;
					start = DateTime.UtcNow;
				}
			}
			
			
			onFilled?.Invoke();

			if (fillQueue.Count > 0)
			{
				currentFill = fillQueue.Dequeue();
				currentFill.Start();
			}
			else
			{
				currentFill = null;
			}
			
			yield break;
		}



		private void RegisterGameObjectPool(int minSize, int maxSize, MonoBehaviour sample, string poolName)
		{	
			var key = poolName;
			
			if(pools.ContainsKey(key)) return;
			
			var pool = new GameObjectPool(minSize, maxSize, sample, storeLocation);

			pools[key] = pool;
		}
		
	
		public T Instantiate<T>(string poolName) where T : class, IPoolObject
		{
			if (!IsReady)
			{
				Debug.LogWarning($"{this} can't instantiate element with pool name: {poolName}. Pool noy ready!");
				return default;
			}

			var key = poolName;
			
			if (pools.TryGetValue(key, out var pool))
			{
				return pool.Instantiate() as T;
			}

			Debug.LogError($"No object pooled with name {poolName} of type {typeof(T)}.");
			
			return default;
		}

		public void Flush()
		{
			foreach (var pool in pools.Values)
			{
				pool.Clear();
			}
			pools.Clear();
		}

		public void SetReady()
		{
			IsReady = true;
		}
		
	}

	
}