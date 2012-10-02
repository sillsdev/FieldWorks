using System;

namespace SIL.FieldWorks.SharpViews.Utilities
{
	/// <summary>
	/// This enumeration gives the possible values of the RootBox.DragState property.
	/// </summary>
	[Flags]
	public enum WindowDragState
	{
		// No drag targeting this window is in progress.
		None,
		// Drag targeting this window is in progress (DragEnter called, valid target, DragLeave not called)
		DraggingHere,
		// We have detected (in OnQueryContinueDrag) that a drop/move that is about to occur in this window
		// also has its source in this window. Accordingly, OnQueryContinueDrag did not do the usual deletion
		// of the source, but will leave it for drop to do.
		InternalMove
	}
}