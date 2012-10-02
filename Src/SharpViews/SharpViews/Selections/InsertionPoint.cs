using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Paragraphs;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.Selections
{
	/// <summary>
	/// An insertion point represents a specific position in text between two characters.
	/// It can also represent one end of a range. It is implemented as a position relative to a particular
	/// StringParaHookup, which in turn links it to a particular paragraph box and client run
	/// </summary>
	public class InsertionPoint : TextSelection
	{
		internal LiteralStringParaHookup Hookup { get; private set; }

		/// <summary>
		/// The position of the IP, relative to the string represented by its hookup. This is in logical characters.
		/// </summary>
		public int StringPosition { get; internal set; }

		internal InsertionPoint(LiteralStringParaHookup hookup, int position, bool fAssocPrev)
		{
			Hookup = hookup;
			StringPosition = position;
			AssociatePrevious = fAssocPrev;
		}

		/// <summary>
		/// Position of the IP within its paragraph in logical characters.
		/// </summary>
		public int LogicalParaPosition
		{
			get
			{
				return Hookup.ParaBox.Source.ClientRuns.Take(Hookup.ClientRunIndex).Sum(run => run.Length) + StringPosition;
			}
		}

		public int RenderParaPosition
		{
			get
			{
				return Hookup.ParaBox.Source.LogToRen(LogicalParaPosition);
			}
		}

		/// <summary>
		/// Return the limit character position in the rendered text of the paragraph that is shown as
		/// selected. Typically this is the same as RenderParaPosition, indicating a normal IP.
		/// When a substitute is being displayed (only if the run has one AND is empty) add its length.
		/// </summary>
		public int LastRenderParaPosition
		{
			get
			{
				var run = ContainingRun as TextClientRun;
				if (run == null || !run.ShouldUseSubstitute)
					return RenderParaPosition;
				// displaying a substitute!
				return RenderParaPosition + run.Substitute.Length;
			}
		}

		public override string ToString()
		{
			try
			{
				return "IP after '" + Para.Source.GetRenderText(0, RenderParaPosition)
					   + "' and before '" + Para.Source.GetRenderText(RenderParaPosition, Para.Source.Length)
					   + "' associating " + (AssociatePrevious ? "previous" : "following");
			}
			catch (Exception)
			{
				return "Insertion Point in a bad state";
			}
		}

		/// <summary>
		/// Return the selection that should be produced by a KeyDown event with the specified
		/// arguments. If the argument does not specify a key-moving event or it is not possible
		/// to move (e.g., left arrow at start of document), return null.
		/// </summary>
		public override Selection MoveByKey(KeyEventArgs args)
		{
			InsertionPoint result;
			switch (args.KeyCode)
			{
				case Keys.Left:
					result = MoveBack();
					break;
				case Keys.Right:
					result = MoveForward();
					break;
				default:
					return null;
			}
			if (result == null)
				return null;
			if (args.Shift)
				return new RangeSelection(this, result);
			return result;
		}

		private InsertionPoint MoveForward()
		{
			int runIndex = Hookup.ClientRunIndex;
			var targetRun = (TextClientRun)Para.Source.ClientRuns[runIndex];
			int ichRun = StringPosition + 1;
			if (ichRun > targetRun.Length)
			{
				// Find a following TextClientRun if possible; otherwise, try for a following paragraph.
				for (; ; )
				{
					if (runIndex == Para.Source.ClientRuns.Count - 1)
					{
						var nextBox = Para.NextInSelectionSequence(false);
						while (nextBox != null && !(nextBox is ParaBox))
							nextBox = nextBox.NextInSelectionSequence(true);
						if (nextBox == null)
							return null; // no way to move forward.
						return nextBox.SelectAtStart();
					}
					runIndex++;
					targetRun = Para.Source.ClientRuns[runIndex] as TextClientRun;
					if (targetRun != null)
						break;
				}
				ichRun = Math.Min(1, targetRun.Length);
			}
			// We'd like to select at ichRun in targetRun, if that is a valid position. Otherwise, keep moving forward.
			while (ichRun < targetRun.Length && !IsValidInsertionPoint(runIndex, ichRun))
				ichRun++;
			return targetRun.SelectAt(Para, ichRun, true);
		}

		private InsertionPoint MoveBack()
		{
			int runIndex = Hookup.ClientRunIndex;
			var targetRun = (TextClientRun)Para.Source.ClientRuns[runIndex];
			int ichRun = StringPosition - 1;
			if (ichRun < 0)
			{
				// Find a previous TextClientRun if possible; otherwise, try for a previous paragraph.
				for (; ; )
				{
					if (runIndex == 0)
					{
						var prevBox = Para.PreviousInSelectionSequence;
						while (prevBox != null && !(prevBox is ParaBox))
							prevBox = prevBox.PreviousInSelectionSequence;
						if (prevBox == null)
							return null; // no way to move back.
						return prevBox.SelectAtEnd();
					}
					runIndex--;
					targetRun = Para.Source.ClientRuns[runIndex] as TextClientRun;
					if (targetRun != null)
						break;
				}
				ichRun = Math.Max(targetRun.Length - 1, 0);
			}
			// We'd like to select at ichRun in targetRun, if that is a valid position. Otherwise, keep moving back.
			while (ichRun > 0 && !IsValidInsertionPoint(runIndex, ichRun))
				ichRun--;

			return targetRun.SelectAt(Para, ichRun, false);
		}

		private bool IsValidInsertionPoint(int runIndex, int ichRun)
		{
			if (Surrogates.IsTrailSurrogate(Para.Source.ClientRuns[runIndex].Text[ichRun]))
				return false;
			int prevLogicalChars = Para.Source.ClientRuns.Take(runIndex).Sum(run => run.Length);
			int ichRen = Para.Source.LogToRen(Para.Source.LogToRen(prevLogicalChars + ichRun));
			for (var box = Para.FirstBox; box != null; box = box.Next)
			{
				var sbox = box as StringBox;
				if (sbox == null)
					continue;
				if (sbox.IchMin > ichRen)
					return true; // we've passed any box that might object
				if (sbox.IchMin + sbox.RenderLength >= ichRen)
				{
					// This one will typically determine it.
					// Enhance JohnT: remove the VwGraphics argument from IsValidInsertionPoint;
					// no implementation uses it. As we switch to the new Graphite interface, we may
					// drop this altogether, and skip diacritics just based on unicode properties.
					var isValidInsertionPoint = sbox.Segment.IsValidInsertionPoint(sbox.IchMin, null, ichRen);
					if (isValidInsertionPoint == LgIpValidResult.kipvrBad)
						return false; // this box is sure it is no good.
					if (isValidInsertionPoint == LgIpValidResult.kipvrOK)
						return true; // this box is sure it is fine
					// otherwise possibly try one more: we may be at a boundary.
				}
			}
			return true; // assume OK unless vetoed.
		}

		/// <summary>
		/// The paragraph containing the IP.
		/// </summary>
		public ParaBox Para
		{
			get
			{
				return Hookup.ParaBox;
			}
		}

		/// <summary>
		/// True if the insertion point associates with the previous character. This affects the default properties of the next character
		/// typed (if true, they are the properties of the previous character, otherwise, of the following one). At BiDi text direction
		/// boundaries, it also affects drawing: the primary IP is drawn adjacent to the associated character, the secondary one (if
		/// possible) adjacent to the non-associated one.
		/// If the paragraph is empty, it is always (arbitrarily) false. This is convenient because it typically causes us to seek properties
		/// at character position 0 (where the following character would be if there were one) rather than at position -1, which is invalid.
		/// </summary>
		public bool AssociatePrevious { get; private set; }

		internal override void Draw(IVwGraphics vg, PaintTransform ptrans)
		{
			Para.DrawIp(this, vg, Para.Container.ChildTransformFromRootTransform(ptrans));
		}

		/// <summary>
		/// The root box that contains the selection.
		/// </summary>
		public override RootBox RootBox
		{
			get { return Para.Root; }
		}

		/// <summary>
		/// True for insertion point.
		/// </summary>
		public override bool IsInsertionPoint
		{
			get { return true; }
		}

		/// <summary>
		/// An insertion point usually should flash. However, when we display one as a substitute string
		/// to indicate what is missing where, it should not!
		/// </summary>
		public override bool ShouldFlash
		{
			get
			{
				return RenderParaPosition == LastRenderParaPosition;
			}
		}
		ClientRun ContainingRun
		{
			get
			{
				if (Para == null || Hookup == null)
					return null;
				return Para.Source.ClientRuns[Hookup.ClientRunIndex];
			}
		}

		//public bool IsShowingEmptySubstitute
		//{
		//    get
		//    {
		//        var run = ContainingRun as StringClientRun;
		//        return run != null && run.Substitute != null;
		//    }
		//}

		///// <summary>
		///// Invalidate the selection, that is, mark the rectangle it occupies as needing to be painted in the containing control.
		///// </summary>
		//internal override void Invalidate()
		//{
		//    base.Invalidate();
		//    // Todo: invalidate the secondary rectangle, if that isn't included in the GetSelectionLocation one.
		//}

		/// <summary>
		/// Get the location, in the coordinates indicated by the transform, of a rectangle that contains the
		/// primary insertion point.
		/// Todo JohnT: there should be a parallel routine to get the location of the secondary rectangle.
		/// </summary>
		public override Rectangle GetSelectionLocation(IVwGraphics graphics, PaintTransform transform)
		{
			return Para.GetIpLocation(this, graphics, transform);
		}

		/// <summary>
		/// True if the two selections are at the same place (ignoring AssociatePrevious).
		/// </summary>
		public bool SameLocation(InsertionPoint other)
		{
			return other != null && Para == other.Para && LogicalParaPosition == other.LogicalParaPosition;
		}

		internal InsertionPoint Associate(bool previous)
		{
			if (previous == AssociatePrevious)
				return this;
			return new InsertionPoint(Hookup, StringPosition, previous);
		}

		/// <summary>
		/// Insert the specified text at the insertion point.
		/// Enhance JohnT: normalize the string after the edit, maintaining the posititon of the IP correctly.
		/// </summary>
		public void InsertText(string input)
		{
			if (Hookup == null)
				return;
			if (!Hookup.CanInsertText(this))
				return; // cannot modify; should we report this somehow?
			Invalidate(); // Hide the old selection while we still know where it is.
			Hookup.InsertText(this, input);
			StringPosition += input.Length;
		}

		public bool CanInsertText
		{
			get
			{
				if (Hookup == null)
					return false;
				return Hookup.CanInsertText(this);
			}
		}

		/// <summary>
		/// Make your state equivalent to the other IP.
		/// </summary>
		/// <param name="other"></param>
		private void CopyFrom(InsertionPoint other)
		{
			Hookup = other.Hookup;
			StringPosition = other.StringPosition;
			AssociatePrevious = other.AssociatePrevious;
		}

		/// <summary>
		/// Implement the backspace key function (delete one character, or merge two paragraphs).
		/// </summary>
		public void Backspace()
		{
			Invalidate(); // while we still know the old position.
			if (StringPosition == 0)
			{
				if (Hookup != null)
				{
					if (Hookup.ClientRunIndex == 0)
					{
						BackspaceDeleteLineBreak();
						return;
					}
					else if (Para.Source.ClientRuns[Hookup.ClientRunIndex - 1] is TextClientRun)
					{
						// Delete at end of previous run.
						var prevClientRun = Para.Source.NonEmptyStringClientRunEndingBefore(Hookup.ClientRunIndex);
						if (prevClientRun == null)
							return;
						// Enhance JohnT: maybe some kind of hookup can merge with previous para or delete an embedded object?
						CopyFrom(prevClientRun.SelectAtEnd(Para));
						Debug.Assert(StringPosition != 0, "should have selected at the END of a non-empty run");
					}
				}
			}
			if (Hookup == null)
				return;
			string oldValue = Hookup.Text;
			int newPos = Surrogates.PrevChar(oldValue, StringPosition); // Enhance JohnT: should we delete back to a base?
			var start = new InsertionPoint(Hookup, newPos, false);
			if (!Hookup.CanDelete(start, this))
				return;
			Hookup.Delete(start, this);
			StringPosition = newPos;
		}

		private void BackspaceDeleteLineBreak()
		{
			IParagraphOperations paragraphOps = null;
			if (Hookup is StringHookup || Hookup is TssHookup)
			{
				ItemHookup itemHookup = null;
				GroupHookup parentHookup = null;
				foreach (var hookup in Hookup.Parents)
				{
					var isoHookup = hookup as IHaveParagagraphOperations;
					if (isoHookup != null && isoHookup.GetParagraphOperations() != null)
					{
						parentHookup = hookup as GroupHookup;
						paragraphOps = isoHookup.GetParagraphOperations();
						break;
					}
					itemHookup = hookup as ItemHookup;
				}
				if (paragraphOps == null || itemHookup == null)
					return; // don't have a scenario where we have a clue how to join paragraphs.
				int index = parentHookup.Children.IndexOf(itemHookup);
				if (index == 0)
				{
					// at the start of the sequence.
					// Enhance JohnT: could look for another paragraph ops higher? additional interface on paragraphOps?
					return;
				}
				var prevItem = parentHookup.Children[index - 1];
				var prevIp = prevItem.SelectAtEnd();
				if (prevIp == null)
					return; // for some reason we can't do it (maybe a box at end of last para?)
				var range = new RangeSelection(prevIp, this);
				Action makeSelection;
				paragraphOps.InsertString(range, "", out makeSelection);
				RootBox.Site.PerformAfterNotifications(makeSelection);
			}
		}

		IParagraphOperations ParagraphOperations
		{
			get
			{
				foreach (var hookup in Hookup.Parents)
				{
					var isoHookup = hookup as IHaveParagagraphOperations;
					if (isoHookup != null && isoHookup.GetParagraphOperations() != null)
						return isoHookup.GetParagraphOperations();
				}
				return null;
			}
		}

		internal void InsertLineBreak()
		{
			var paragraphOps = ParagraphOperations;
			if (paragraphOps == null)
				return;
			Action makeSelection;
			if (IsAtEndOfPara)
				paragraphOps.InsertFollowingParagraph(this, out makeSelection);
			else if (IsAtStartOfPara)
				paragraphOps.InsertPrecedingParagraph(this, out makeSelection);
			else
				paragraphOps.SplitParagraph(this, out makeSelection);
			RootBox.Site.PerformAfterNotifications(makeSelection);
		}

		private bool IsAtEndOfPara
		{
			get
			{
				return Hookup.ClientRunIndex == Para.Source.ClientRuns.Count - 1
					   && Para.Source.ClientRuns[Hookup.ClientRunIndex].Length == StringPosition;
			}
		}

		private bool IsAtStartOfPara
		{
			get
			{
				return Hookup.ClientRunIndex == 0
					   && StringPosition == 0;
			}
		}
	}
}
