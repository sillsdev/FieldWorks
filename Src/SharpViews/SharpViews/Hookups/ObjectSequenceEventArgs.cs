using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// This class is used to implement events which indicate that an object sequence has changed,
	/// and explicitly provide SharpViews with information about which objects in the sequence changed.
	/// This may be done either to improve performance, or so that the client can force the display
	/// to update for a particular sequence of objects even though the sequence may NOT actually
	/// have changed.
	/// The arguments indicate the first object in the value when the display was last updated
	/// that has been deleted, or (if numberDeleted is zero) the one before which objects
	/// have been added.
	/// The other two arguments indicate how many objects in the current display have been removed
	/// and how many (in the current value of the property) are considered new.
	/// </summary>
	public class ObjectSequenceEventArgs : EventArgs
	{
		public ObjectSequenceEventArgs(int firstChange, int numberAdded, int numberDeleted)
		{
			FirstChange = firstChange;
			NumberAdded = numberAdded;
			NumberDeleted = numberDeleted;
		}
		public int FirstChange { get; private set; }
		public int NumberDeleted { get; private set; }
		public int NumberAdded { get; private set; }
	}
}
