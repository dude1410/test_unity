using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArchCore.DataStructures.Hierarchy
{
	public class Hierarchy<T> : IEnumerable<T> where T : class
	{
		private LinkedList<T> nodes = new LinkedList<T>();

		public T LastNode => nodes.Last?.Value;

		public T FirstNode => nodes.First?.Value;

		public int Count => nodes.Count;

		public void RemoveItem(T item)
		{
			if (!nodes.Contains(item))
			{
				Debug.LogError("Hierarchy does not contain item!");
				return;
			}

			T removedView;
			do
			{
				removedView = nodes.Last.Value;
				nodes.RemoveLast();
			} while (removedView != item && nodes.Count > 0);
		}

		public void RemoveLast()
		{
			nodes.RemoveLast();
		}

		public void AddItem(T view)
		{
			nodes.AddLast(view);
		}

		public T Get(T item)
		{
			return nodes.FindLast(item)?.Value;
		}

		public T GetNextOf(T item)
		{
			var linkedListNode = nodes.FindLast(item)?.Next;
			return linkedListNode?.Value;
		}

		public void Clear()
		{
			nodes.Clear();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return nodes.GetEnumerator();
		}

		public IEnumerator GetEnumerator()
		{
			return nodes.GetEnumerator();
		}
	}
}