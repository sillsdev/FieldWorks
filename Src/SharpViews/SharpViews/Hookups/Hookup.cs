// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// A Hookup represents the connection between the domain (a property of an object) and a section of a view (one or more boxes or strings).
	/// Typically it knows how to connect to an event (and disconnect when disposed) that is raised when a particular property changes,
	/// how to get the value, and how to inform the view of the change.
	///
	/// Some subtypes may (eventually) know how to perform the reverse operation, that is, when something in the view is edited, the Hookup
	/// knows what should be changed in the domain model.
	/// </summary>
	public class Hookup : IHookup, IHookupInternal, IDisposable, IReceivePropChanged
	{
		/// <summary>
		/// The object that has the property we are connected to. This is typically an FDO object but the SharpViews code is designed
		/// not to require it.
		/// </summary>
		public object Target { get; private set; }

		/// <summary>
		/// This field is generally available to the client to use, typically to identify what
		/// part of the Target the hookup is showing. Hookups created automatically from FDO properties
		/// use it to store the field identifier.
		/// </summary>
		public int Tag { get; set;}

		/// <summary>
		/// This is a more powerful way of identifying hookups, and can store any desired object.
		/// It is not currently used at all by the SharpViews system.
		/// </summary>
		public object Label { get; set; }

		public GroupHookup ParentHookup { get; private set; }

		void IHookupInternal.SetParentHookup(GroupHookup parent)
		{
			ParentHookup = parent;
		}

		public Hookup(object target)
		{
			Target = target;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~Hookup()
		{
			Dispose(false);
		}

		/// <summary>
		/// Should be sent when the particular property that this hookup manages changes. Subclasses override
		/// as needed.
		/// </summary>
		public virtual void PropChanged(object sender, EventArgs args)
		{
		}

		public virtual void PropChanged(object sender, ObjectSequenceEventArgs args)
		{
		}

		protected virtual void Dispose(bool beforeDestructor)
		{

		}

		/// <summary>
		/// Enumerate all the parents of the hookup, from closest to most remote.
		/// </summary>
		public IEnumerable<GroupHookup> Parents
		{
			get
			{
				for (var item = ParentHookup; item != null; item = item.ParentHookup)
					yield return item;

			}
		}

		/// <summary>
		/// Make an insertion point at the end of the data covered by the hookup.
		/// This base implementation doesn't have any way to do so.
		/// </summary>
		public virtual InsertionPoint SelectAtEnd()
		{
			return null;
		}
		/// <summary>
		/// Make an insertion point at the start of the data covered by the hookup.
		/// This base implementation doesn't have any way to do so.
		/// </summary>
		public virtual InsertionPoint SelectAtStart()
		{
			return null;
		}
	}
}
