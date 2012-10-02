// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SequenceHookup.cs
// Responsibility: Thomson
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// A SequenceHookup relates a sequence of objects (of class T) to part of the view.
	/// It knows a delegate which it applies to its root object to obtain the current contents
	/// of the sequence.
	/// It knows the contents of the sequence at the time the view was built.
	/// It stores delegates which know how to connect and disconnect it from an event which
	/// occurs when the sequence changes.
	/// It is an abstract class, with an abstract method to handle notification that the
	/// sequence has changed. Subclasses may rebuild the display of the whole sequence, or,
	/// knowing that items are independent, may only rebuild what has changed. A subclass
	/// could distinguish item displays from separators (e.g., punctuation).
	/// A SequenceHookup can also know about a parent GroupBox into which a display of its
	/// items should be inserted. If it has a parent hookup, it can also look for preceding
	/// hookups which insert things into the same box, and if any are found, it will correctly
	/// place its items after any belonging to a preceding hookup.
	/// Todo: figure how that notion plays out if the items are runs in a paragraph.
	/// </summary>
	public class SequenceHookup<T> : GroupHookup, IReceivePropChanged
	{
		/// <summary>
		/// The function that returns the current list of objects to display.
		/// </summary>
		internal Func<IEnumerable<T>> Fetcher { get; private set;}

		public SequenceHookup(object target, GroupBox containingBox, Func<IEnumerable<T>> fetcher,
			Action<IReceivePropChanged> hookEvent, Action<IReceivePropChanged> unhookEvent)
			: base(target, containingBox)
		{
			Fetcher = fetcher;
			if (hookEvent != null)
				hookEvent(this);
			RemoveHook = unhookEvent;
		}

		/// <summary>
		/// Sent when the contents of the property we are monitoring changes.
		/// </summary>
		public override void PropChanged(object sender, EventArgs args)
		{

		}

		/// <summary>
		/// Sent when the contents of the property we are monitoring changes.
		/// </summary>
		public override void PropChanged(object sender, ObjectSequenceEventArgs args)
		{

		}

		private Action<IReceivePropChanged> RemoveHook { get; set; }

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (RemoveHook != null)
					RemoveHook(this);
			}
		}
	}
}
