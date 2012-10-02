using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.SharpViews.Builders;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	public class LazyHookup<T> : IndependentSequenceHookup<T> where T: class
	{
		public LazyHookup(object target, GroupBox containingBox, Func<IEnumerable<T>> fetcher,
			Action<IReceivePropChanged> hookEvent, Action<IReceivePropChanged> unhookEvent,
			Action<ViewBuilder, T> displayOneT)
			: base(target, containingBox, fetcher, hookEvent, unhookEvent, displayOneT)
		{
		}

		public override void PropChanged(object sender, EventArgs args)
		{
			var builder = ContainingBox.Builder;
			builder.CurrentHookup = this;
			var newItems = Fetcher().ToArray();
			// See how much we can keep at the start of the sequence;
			int firstChildToReplace = 0;
			int firstItemToReplace = 0;
			while (firstChildToReplace < Children.Count)
			{
				var group = ((IItemsHookup) Children[firstChildToReplace]).ItemGroup;
				if (!DoesGroupMatch(group, newItems, firstItemToReplace))
					break;
				firstChildToReplace++;
				firstItemToReplace += group.Length;
			}
			// See how much we can keep at the end of the sequence;
			int limChildToReplace = Children.Count;
			int limItemToReplace = newItems.Length;
			while (limChildToReplace > firstChildToReplace)
			{
				var group = ((IItemsHookup)Children[limChildToReplace - 1]).ItemGroup;
				if (!DoesGroupMatch(group, newItems, limItemToReplace - group.Length))
					break;
				limChildToReplace--;
				limItemToReplace -= group.Length;
			}
			Box firstBoxToRemove = null;
			for (int i = firstChildToReplace; i < limChildToReplace && firstBoxToRemove == null; i++ )
			{
				firstBoxToRemove = ((IItemsHookup) Children[i]).FirstBox;
			}
			Box lastBoxToRemove = GetLastBoxInRange(firstChildToReplace, limChildToReplace);
			Children.RemoveRange(firstChildToReplace, limChildToReplace - firstChildToReplace);
			if (firstBoxToRemove != null)
				ContainingBox.RemoveBoxes(firstBoxToRemove, lastBoxToRemove);
			if (limItemToReplace > firstItemToReplace)
			{
				var lazyBox = new LazyBox<T>(builder.NestedBoxStyles, this,
					newItems.Skip(firstItemToReplace).Take(limItemToReplace - firstItemToReplace));
				ContainingBox.InsertBox(lazyBox, GetLastBoxInRange(0, firstChildToReplace));
				Children.Insert(firstChildToReplace, lazyBox);
				using (var gh = ContainingBox.Root.Site.DrawingInfo)
				{
					lazyBox.RelayoutWithParents(gh);
				}
			} else if (limChildToReplace > firstChildToReplace)
			{
				// pure deletion.
				using (var gh = ContainingBox.Root.Site.DrawingInfo)
				{
					ContainingBox.RelayoutWithParents(gh);
				}
			}
		}

		private Box GetLastBoxInRange(int firstChild, int limChild)
		{
			Box lastBoxToRemove = null;
			for (int i = limChild - 1; i >= firstChild && lastBoxToRemove == null; i--)
			{
				lastBoxToRemove = ((IItemsHookup)Children[i]).LastBox;
			}
			return lastBoxToRemove;
		}

		/// <summary>
		/// Return true if the objects in items starting at index match the objects in group.
		/// </summary>
		private bool DoesGroupMatch(object[] group, T[] items, int index)
		{
			if (index < 0 || index + group.Length > items.Count())
				return false;
			for (int i = 0; i < group.Length; i++)
			{
				if (group[i] != items[i + index])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Expand part of the specified lazy box.
		/// Todo: implement firstItemToExpand, limItemToExpand.
		/// </summary>
		internal void ExpandItems(LazyBox<T> source, int firstItemToExpand, int limItemToExpand,
			int topOfFirstItemToExpand, int bottomOfLastItemToExpand)
		{
			var root = ContainingBox.Root;
			int oldRootHeight = root.Height;
			var builder = ContainingBox.Builder;
			builder.CurrentHookup = this;
			var objects = new T[limItemToExpand - firstItemToExpand];
			source.Items.CopyTo(firstItemToExpand, objects, 0, limItemToExpand - firstItemToExpand);
			int insertAt = Children.IndexOf(source); // default, insert in place of source (or before it).
			if (firstItemToExpand > 0)
			{
				// we will keep source to represent the unexpanded items at the start.
				insertAt += 1; // insert right after source.
				if (limItemToExpand < source.Items.Count)
				{
					// We will have to make a new lazy box to insert after the expanded items
					var newLazyBox = new LazyBox<T>(source.Style, this,
						source.Items.Skip(limItemToExpand));
					Children.Insert(insertAt, newLazyBox);
					ContainingBox.InsertBox(newLazyBox, source);
					// We will still insert the expanded items after source, before newLazyBox.
				}
				// Remove the items no longer considered part of source.
				source.RemoveItems(firstItemToExpand, source.Items.Count - firstItemToExpand);
			}
			else if (limItemToExpand < source.Items.Count)
			{
				// We will keep source to represent the unexpanded items at the end.
				source.RemoveItems(0, limItemToExpand);
			}
			else
			{
				// Everything is being expanded: get rid of source altogether.
				ContainingBox.RemoveBoxes(source, source);
				Children.RemoveAt(insertAt); // get rid of the lazy box from hookup collection
			}

			// Generate the new items.
			for (int i = 0; i < objects.Length; i++)
				BuildAnItemDisplay(builder, objects[i], insertAt + i);
			using (var gh = ContainingBox.Root.Site.DrawingInfo)
			{
				ContainingBox.RelayoutWithParents(gh, true);
			}

			// Notify root of changed overall size and possibly scroll position.
			root.RaiseLazyExpanded(new RootBox.LazyExpandedEventArgs() { EstimatedTop = topOfFirstItemToExpand,
				EstimatedBottom = bottomOfLastItemToExpand, DeltaHeight = root.Height - oldRootHeight });
		}
	}
}
