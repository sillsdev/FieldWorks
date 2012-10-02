using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// This abstract class provides a default implementation of IParagraphOperations.
	/// Minimally the methods that read and write the property that contains the text must be implemented.
	/// Enhance JohnT: may want to break out a subclass so that MakeListItem is abstract and the baseclass
	/// does not require a zero-argument constructor for T.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class BaseParagraphOperations<T> : IParagraphOperations<T>, IParaOpsList<T>
	{
		/// <summary>
		/// The list of objects which paragraph operations modifies.
		/// </summary>
		public IList<T> List { get; set; }

		public SequenceHookup<T> Hookup { get; set; }

		/// <summary>
		/// Insert a new paragraph, after the user types Enter, where the previous selection
		/// is an insertion point at the end of the paragraph.
		/// </summary>
		public virtual bool InsertFollowingParagraph(InsertionPoint ip, out Action makeSelection)
		{
			int index = ItemIndex(ip) + 1;
			MakeListItem(index);
			makeSelection = () => SelectionBuilder.In(Hookup)[index].Offset(0).Install();
			return true;
		}

		/// <summary>
		/// Determine the index of the item containing the IP
		/// </summary>
		/// <param name="ip"></param>
		/// <returns></returns>
		internal int ItemIndex(InsertionPoint ip)
		{
			return Hookup.IndexOfChild(ip);
		}

		/// <summary>
		/// This method must be provided to make new things in the list, unless all callers are overridden.
		/// It makes a new item and inserts it at the specified index.
		/// </summary>
		public abstract T MakeListItem(int index);

		public virtual bool InsertFollowingParagraph(RangeSelection range, out Action makeSelection)
		{
			throw new NotImplementedException();
		}

		public bool SplitParagraph(InsertionPoint ip, out Action makeSelection)
		{
			int index = ItemIndex(ip);
			MakeListItem(index + 1);
			var oldItem = List[index];
			var newItem = List[index + 1];
			MoveMaterialAfterIpToStart(ip, oldItem, newItem);
			makeSelection = () => SelectionBuilder.In(Hookup)[index + 1].Offset(0).Install();
			return true;
		}

		/// <summary>
		/// Move whatever comes after the given IP to the start of the destination object.
		/// </summary>
		public virtual void MoveMaterialAfterIpToStart(InsertionPoint ip, T source, T destination)
		{
			var stringHookup = ip.Hookup as StringHookup;
			if (stringHookup != null)
			{
				var run = stringHookup.ParaBox.Source.ClientRuns[stringHookup.ClientRunIndex] as StringClientRun;
				if (run != null) // should always be true, I think.
				{
					string oldValue = run.Contents;
					string move = oldValue.Substring(ip.StringPosition);
					string keep = oldValue.Substring(0, ip.StringPosition);
					stringHookup.Writer(keep);
					InsertAtStartOfNewObject(ip, move, destination);
				}
				return;
			}
			var tssHookup = ip.Hookup as TssHookup;
			if (tssHookup != null)
			{
				var run = tssHookup.ParaBox.Source.ClientRuns[tssHookup.ClientRunIndex] as TssClientRun;
				if (run != null) // should always be true, I think
				{
					var oldValue = run.Tss;
					var move = oldValue.GetSubstring(ip.StringPosition, oldValue.Length);
					var keep = oldValue.GetSubstring(0, ip.StringPosition);
					tssHookup.Writer(keep);
					InsertAtStartOfNewObject(ip, move, destination);
				}
			}
		}

		/// <summary>
		/// Insert the indicated text at the start of the new object. The IP indicates where it came from,
		/// which may be significant for subclasses where the display of T is complex. This is used for
		/// moving the tail end of a split string, when inserting a line break into a paragraph. The destination
		/// may be assumed newly created and empty.
		/// </summary>
		public virtual void InsertAtStartOfNewObject(InsertionPoint source, string move, T destination)
		{
			SetString(destination, move);
		}

		/// <summary>
		/// Insert the indicated text at the start of the new object. The IP indicates where it came from,
		/// which may be significant for subclasses where the display of T is complex. This is used for
		/// moving the tail end of a split string, when inserting a line break into a paragraph. The destination
		/// may be assumed newly created and empty.
		/// </summary>
		public virtual void InsertAtStartOfNewObject(InsertionPoint source, ITsString move, T destination)
		{
			SetString(destination, move);
		}

		/// <summary>
		/// This is the simplest method to override. It sets the paragraph content property to the given string.
		/// If you do not override this you need to override all its senders (or else the TsString version,
		/// if your main content is TsSTrings). This method would be abstract, except that some clients may
		/// prefer to override callers, and some need to override the TsString overload instead.
		/// </summary>
		public virtual void SetString(T destination, string val)
		{
			throw new NotImplementedException("subclass must override callers of SetString or implement it");
		}

		/// <summary>
		/// This is the simplest method to override for TsString props.
		/// It sets the paragraph content property to the given TsString.
		/// If you do not override this you need to override all its senders (or else the string version,
		/// if your main content is strings). This method would be abstract, except that some clients may
		/// prefer to override callers, and some need to override the string overload instead.
		/// </summary>
		public virtual void SetString(T destination, ITsString val)
		{
			throw new NotImplementedException("subclass must override callers of SetString or implement it");
		}

		public virtual bool InsertPrecedingParagraph(InsertionPoint ip, out Action makeSelection)
		{
			int index = ItemIndex(ip);
			MakeListItem(index);
			// The selection is still at the start of the original paragraph. No makeSelection action is needed.
			makeSelection = () => DoNothing();
			return true;
		}

		void DoNothing()
		{}


		/// <summary>
		/// Replace the text in the range, presumed to extend across more than one paragraph,
		/// with the supplied text.
		/// This one serves also for backspace at start of paragraph and delete at end, since
		/// these can readily be transformed into deleting a range from the end of one para
		/// to the start of the next. The given range is replace by a simple string.
		/// Enhance JohnT: it would probably make sense to break this up, more like SplitParagraph,
		/// into further virtual methods that subclasses could override. But I haven't figured out how yet.
		/// </summary>
		public bool InsertString(RangeSelection range, string insert, out Action makeSelection)
		{
			makeSelection = () => DoNothing(); // a default.
			var firstIp = range.Start;
			var lastIp = range.End;
			int firstItemIndex = ItemIndex(firstIp);
			int lastItemIndex = ItemIndex(lastIp);
			if (firstItemIndex == lastItemIndex)
				throw new ArgumentException("Should not need to use ParagraphOps.InsertString for range in same item");
			int startPosition = firstIp.StringPosition;
			if (!MergeTextAfterSecondIntoFirst(firstIp, lastIp, insert))
				return false;
			DeleteItems(firstItemIndex + 1, lastItemIndex);
			makeSelection = () => SelectionBuilder.In(Hookup)[firstItemIndex].Offset(startPosition + insert.Length).Install();
			return true;
		}

		/// <summary>
		/// Delete the specified range of items from the list.
		/// Enhance JohnT: it would generally be much preferable to delete all of them and then do one change notification.
		/// </summary>
		public virtual void DeleteItems(int firstIndex, int lastIndex)
		{
			for (int i = firstIndex; i <= lastIndex; i++)
				List.RemoveAt(firstIndex); // not at i, they keep moving up
		}

		/// <summary>
		/// Change the property containing firstIp to contain its current text before firstIp followed by
		/// the inserted text followed by the text after lastIp in that IP's property.
		/// Return true if successful. Typically used as part of
		/// deleting the range in between the two IPs and inserting the specified text (if any).
		/// </summary>
		public virtual bool MergeTextAfterSecondIntoFirst(InsertionPoint firstIp, InsertionPoint lastIp, string insert)
		{
			var firstStringHookup = firstIp.Hookup as StringHookup;
			var lastStringHookup = lastIp.Hookup as StringHookup;
			if (firstStringHookup != null && lastStringHookup != null)
			{
				var firstRun = firstStringHookup.ParaBox.Source.ClientRuns[firstStringHookup.ClientRunIndex] as StringClientRun;
				var lastRun = lastStringHookup.ParaBox.Source.ClientRuns[lastStringHookup.ClientRunIndex] as StringClientRun;
				if (firstRun == null || lastRun == null)
					return false; //
				string tailend = lastRun.Contents.Substring(lastIp.StringPosition);
				string leadIn = firstRun.Contents.Substring(0, firstIp.StringPosition);
				string newContents = leadIn + insert + tailend; // what if they're different writing systems??
				firstStringHookup.Writer(newContents);
				return true;
			}
			var firstTssHookup = firstIp.Hookup as TssHookup;
			var lastTssHookup = lastIp.Hookup as TssHookup;
			if (firstTssHookup != null && lastTssHookup != null)
			{
				var firstRun = firstTssHookup.ParaBox.Source.ClientRuns[firstTssHookup.ClientRunIndex] as TssClientRun;
				var lastRun = lastTssHookup.ParaBox.Source.ClientRuns[lastTssHookup.ClientRunIndex] as TssClientRun;
				if (firstRun == null || lastRun == null)
					return false; //
				var tailend = lastRun.Tss.GetSubstring(lastIp.StringPosition, lastRun.Tss.Length);
				var leadIn = firstRun.Tss.GetSubstring(0, firstIp.StringPosition);
				var bldr = leadIn.GetBldr();
				// Enhance JohnT: get props from start of selected text, if any.
				bldr.Replace(bldr.Length, bldr.Length, insert, null);
				bldr.ReplaceTsString(bldr.Length, bldr.Length, tailend);
				var newContents = bldr.GetString();
				firstTssHookup.Writer(newContents);
				return true;
			}
			return false;
		}
	}

	public abstract class ParagraphOperations<T> : BaseParagraphOperations<T>, IParaOpsList<T> where T : new()
	{
		/// <summary>
		/// This should often be overridden, because (except for special lists like FdoVectors or
		/// MonitoredList) inserting an item like this will not generate any event.
		/// </summary>
		public override T MakeListItem(int index)
		{
			var result = new T();
			List.Insert(index, result);
			return result;
		}
	}
}
