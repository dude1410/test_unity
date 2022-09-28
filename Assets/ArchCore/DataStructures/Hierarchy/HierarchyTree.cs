using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArchCore.DataStructures.Hierarchy
{
	public interface IReadOnlyHierarchyTree<out T> : IEnumerable<T>
	{
		T Root { get; }
	}
	
	public class HierarchyTree<T> : IReadOnlyHierarchyTree<T> where T : class
	{
		private struct TreeNodeData
		{
			public T parent;
			public List<T> children;

			public TreeNodeData(T parent)
			{
				this.parent = parent;
				children = new List<T>();
			}
		}
			
		Dictionary<T, TreeNodeData> nodes = new Dictionary<T, TreeNodeData>();
		public T Root => root;

		private T root;
		
		public void RemoveItem(T item)
		{
			var data = nodes[item];
			if (data.parent != null) nodes[data.parent].children.Remove(item);
			foreach (var child in data.children)
			{
				RemoveItem(child);
			}

			nodes.Remove(item);

			if (root == item) root = null;
		}

		public void SetRoot(T newRoot)
		{
			root = newRoot;
			nodes.Clear();
			nodes.Add(newRoot, new TreeNodeData(null));
		}

		public void AddItem(T item, T under)
		{
			nodes[under].children.Add(item);
			nodes[item] = new TreeNodeData(under);
		}

		public IReadOnlyList<T> GetSubItemsOf(T item)
		{
			return nodes[item].children;
		}


		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return nodes.Keys.GetEnumerator();
		}
		
		public IEnumerator GetEnumerator()
		{
			return nodes.Keys.GetEnumerator();
		}
	}
}