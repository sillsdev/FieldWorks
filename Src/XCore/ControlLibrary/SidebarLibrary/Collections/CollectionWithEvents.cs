
using System;
using System.Collections;

namespace SidebarLibrary.Collections
{
	public class CollectionWithEvents : CollectionBase
	{
		// Declare the event signatures
		public delegate void CollectionClear();
		public delegate void CollectionChange(int index, object value);

		// Collection change events
		public event CollectionClear Clearing;
		public event CollectionClear Cleared;
		public event CollectionChange Inserting;
		public event CollectionChange Inserted;
		public event CollectionChange Removing;
		public event CollectionChange Removed;

		// Overrides for generating events
		protected override void OnClear()
		{
			// Any attached event handlers?
			if (Clearing != null)
			{
				// Raise event to notify all contents removed
				Clearing();
			}
		}

		protected override void OnClearComplete()
		{
			// Any attached event handlers?
			if (Cleared != null)
			{
				// Raise event to notify all contents removed
				Cleared();
			}
		}

		protected override void OnInsert(int index, object value)
		{
			// Any attached event handlers?
			if (Inserting != null)
			{
				// Raise event to notify new content added
				Inserting(index, value);
			}
		}

		protected override void OnInsertComplete(int index, object value)
		{
			// Any attached event handlers?
			if (Inserted != null)
			{
				// Raise event to notify new content added
				Inserted(index, value);
			}
		}

		protected override void OnRemove(int index, object value)
		{
			// Any attached event handlers?
			if (Removing != null)
			{
				// Raise event to notify content has been removed
				Removing(index, value);
			}
		}

		protected override void OnRemoveComplete(int index, object value)
		{
			// Any attached event handlers?
			if (Removed != null)
			{
				// Raise event to notify content has been removed
				Removed(index, value);
			}
		}

		protected int IndexOf(object value)
		{
			// Find the 0 based index of the requested entry
			return base.List.IndexOf(value);
		}
	}
}
