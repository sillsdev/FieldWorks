using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// A GroupHookup subclass is used for any hookup that can be a parent of other hookups.
	/// For example, a SequenceHookup has a child hookup for each item in the sequence;
	/// an ItemHookup has children which are the hookups for the various properties displayed.
	/// It keeps track of its children in order.
	/// It also knows a containing box in which its parts are placed.
	/// </summary>
	public abstract class GroupHookup : Hookup
	{
		public GroupBox ContainingBox { get; private set; }
		internal List<IHookup> Children { get; private set; }
		protected GroupHookup(object target, GroupBox containingBox) : base(target)
		{
			ContainingBox = containingBox;
			Children = new List<IHookup>();
		}

		/// <summary>
		/// Get the last child hookup in the group (or null, if there are no children).
		/// </summary>
		public IHookup LastChild
		{
			get
			{
				if (Children.Count == 0)
					return null;
				return Children[Children.Count - 1];
			}
		}

		internal void InsertChildHookup(IHookup child, int insertAt)
		{
			Children.Insert(insertAt, child);
			((IHookupInternal)child).SetParentHookup(this);
		}
		/// <summary>
		/// Dispose any disposable children and clear the collection.
		/// </summary>
		/// <param name="beforeDestructor"></param>
		protected override void Dispose(bool beforeDestructor)
		{
			if (beforeDestructor)
			{
				foreach (var hookup in Children)
				{
					var disposeHookup = hookup as IDisposable;
					if (disposeHookup != null)
						disposeHookup.Dispose();
				}
				Children.Clear();
			}
			base.Dispose(beforeDestructor);
		}

		/// <summary>
		/// Return the index of the one of your child hookups that contains the given selection,
		/// or -1 if not found.
		/// </summary>
		internal int IndexOfChild(InsertionPoint ip)
		{
			var child = ChildContaining(ip);
			if (child == null)
				return -1;
			return Children.IndexOf(child);
		}

		/// <summary>
		/// Return the one of your direct children which contains the given IP (or null if none does).
		/// </summary>
		internal Hookup ChildContaining(InsertionPoint ip)
		{
			return ChildContaining(ip.Hookup);
		}

		/// <summary>
		/// Return the one of your direct children which is or is a parent of the given child hookup
		/// (or null if no such exists).
		/// </summary>
		internal Hookup ChildContaining(Hookup child)
		{
			if (child.ParentHookup == this)
				return child;
			return (from hookup in child.Parents where hookup.ParentHookup == this select hookup).FirstOrDefault();
		}

		/// <summary>
		/// Make an insertion point at the end of the data covered by the hookup.
		/// GroupHookup delegates to its last child.
		/// Enhance JohnT: possibly try the second-last child, if the last one cannot, and so on?
		/// </summary>
		public override InsertionPoint SelectAtEnd()
		{
			if (Children.Count == 0)
				return null;
			return Children.Last().SelectAtEnd();
		}
	}

	/// <summary>
	/// This hookup serves as the connector for everything in a root box. There is no target object.
	/// </summary>
	public class RootHookup : GroupHookup
	{
		public RootHookup(GroupBox containingBox) : base(null, containingBox)
		{
		}
	}
}
