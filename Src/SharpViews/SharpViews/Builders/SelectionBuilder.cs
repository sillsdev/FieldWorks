using System;
using System.Diagnostics;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Builders
{

	/// <summary>
	/// SelectionBuilder supports a fluent interface for making selections.
	/// </summary>
	public class SelectionBuilder
	{
		/// <summary>
		/// This tracks the object that is currently specified as containing the anchor of
		/// the selection.
		/// </summary>
		Hookup AnchorHookup { get; set; }
		/// <summary>
		/// This tracks a character offset within the current anchor. -1 if never set.
		/// </summary>
		int AnchorOffset { get; set; }

		InsertionPoint Anchor { get; set; }

		private SelectionBuilder()
		{
			AnchorOffset = -1;
		}

		/// <summary>
		/// An entry point for making a selection, setting the current position to the specified hookup.
		/// </summary>
		public static SelectionBuilder In(Hookup startHookup)
		{
			return new SelectionBuilder() {AnchorHookup = startHookup};
		}

		public static SelectionBuilder In(RootBox root)
		{
			return In(root.RootHookup);
		}

		/// <summary>
		/// Refine the current target to the index'th item of the old current target (or null).
		/// Returns 'this' to let the fluent chain continue.
		/// </summary>
		public SelectionBuilder this[int index]
		{
			get
			{
				var groupHookup = AnchorHookup as GroupHookup;
				if (groupHookup != null && groupHookup.Children.Count > index)
					AnchorHookup = groupHookup.Children[index] as Hookup; // Todo: expand if lazy box
				else
					AnchorHookup = null;
				return this;
			}
		}

		/// <summary>
		/// In the fluent language call sequence, this marks the transition from specifying the
		/// anchor to specifying the DragEnd. The current state of the builder is saved as the anchor;
		/// subsequent changes will affect the DragEnd, and the overall result will consequently be
		/// a range (if some change is made).
		/// </summary>
		public SelectionBuilder To
		{
			get
			{
				Debug.Assert(Anchor == null);
				Anchor = Ip;
				Debug.Assert(Anchor != null);
				return this;
			}
		}

		public SelectionBuilder Offset(int ich)
		{
			AnchorOffset = ich;
			return this;
		}
		/// <summary>
		/// Make and return an actual Selection. This is what it's all about.
		/// </summary>
		public Selection Selection
		{
			get
			{
				if (Anchor == null)
					return Ip;
				// Enhance JohnT: probably we should just return an IP, if Anchor is the same.
				return new RangeSelection(Anchor, Ip);
			}
		}

		/// <summary>
		/// Make and return an InsertionPoint based on the current settings.
		/// </summary>
		InsertionPoint Ip
		{
			get
			{
				// Todo: many more cases.
				if (AnchorOffset == -1)
				{
					throw new NotImplementedException("for now must have string offset");
				}
				var tryHookup = AnchorHookup;
				while (tryHookup != null)
				{
					var sHookup = tryHookup as LiteralStringParaHookup;
					if (sHookup != null)
					{
						return new InsertionPoint(sHookup, AnchorOffset, false);
					}
					var gHookup = tryHookup as GroupHookup;
					if (gHookup == null || gHookup.Children.Count < 1)
						return null; // Todo: maybe try depth-first search? try the next hookup in the parent, etc.?
					tryHookup = gHookup.Children[0] as Hookup; // Todo: expand if lazy
				}
				return null; // not actually reachable, I think, but the compiler wants something.
			}
		}

		/// <summary>
		/// This is a shortcut which both gets the selection and installs it.
		/// </summary>
		/// <returns></returns>
		public Selection Install()
		{
			var result = Selection;
			if (result == null)
				return null;
			result.Install();
			return result;
		}
	}
}
