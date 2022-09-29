namespace Favorites
{
	using UnityEditor;
	using UnityEngine;

	public interface IDragAndDrop
	{
		Object[] Objects { get; set; }
		string[] Paths { get; set; }
		DragAndDropVisualMode VisualMode { get; set; }

		void AcceptDrag();
		void PrepareStartDrag();
		void StartDrag( string title );
	}
}