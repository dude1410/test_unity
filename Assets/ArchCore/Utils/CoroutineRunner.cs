using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArchCore.Utils
{
	public class CoroutineRunner : MonoBehaviour
	{
		static CoroutineRunner instance;

		private void Awake()
		{
			instance = this;
			StartCoroutine(Synchroniser());
		}

		public static Coroutine RunCoroutine(IEnumerator task)
		{
			return instance.StartCoroutine(task);
		}

		public static void Stop(Coroutine coroutine)
		{
			if(instance!=null)
				instance.StopCoroutine(coroutine);
		}

		private Queue<Action> syncCalls = new Queue<Action>();

		public static void SyncCall(Action call)
		{
			instance.syncCalls.Enqueue(call);
		}

		private IEnumerator Synchroniser()
		{
			while (true)
			{
				while (syncCalls.Count > 0)
				{
					syncCalls.Dequeue()();
				}

				yield return null;
			}
		}
	}
}