// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
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
		private int m_stringPosition = 0;
		internal LiteralStringParaHookup Hookup { get; private set; }
		public IStyle StyleToBeApplied { get; private set; }

		public override ISelectionRestoreData RestoreData(Selection dataToSave)
		{
			if (dataToSave.IsInsertionPoint)
				return new InsertionPointRestoreData((InsertionPoint)dataToSave);
			return new RangeRestoreData((RangeSelection)dataToSave);
		}

		public override bool IsValid
		{
			get
			{
				var para = Hookup.ParaBox;
				var index = Hookup.ClientRunIndex;
				if (para == null)
				{
					return false;
				}
				var runs = para.Source.ClientRuns;
				GroupBox current = para;
				while (true)
				{
					if (current.Container == null)
						return false;
					if (!current.Container.Children.Contains(current))
						return false;
					if (current.Container is RootBox)
						break;
					current = current.Container;
				}
				if (index >= runs.Count)
					return false;
				if (!(runs[index] is TextClientRun))
					return false;
				if (StringPosition > runs[index].Length)
					return false;
				if ((runs[index] as TextClientRun).Hookup != Hookup)
					return false;
				return true;
			}
		}

		/// <summary>
		/// The position of the IP, relative to the string represented by its hookup. This is in logical characters.
		/// </summary>
		public int StringPosition
		{
			get { return m_stringPosition; }
			internal set
			{
				StyleToBeApplied = null;
				m_stringPosition = value;
			}
		}

		public InsertionPoint NextIp(int distance)
		{
			InsertionPoint newIp;
			IClientRun run = ContainingRun;
			ParaBox box = run.Hookup.ParaBox;
			if (StringPosition + distance > run.Text.Length)
			{
				distance -= run.Text.Length - StringPosition;
				if (box.Source.ClientRuns[run.Hookup.ClientRunIndex] != box.Source.ClientRuns.Last())
					run = box.Source.ClientRuns[run.Hookup.ClientRunIndex + 1];
				else
				{
					box = box.NextParaBox;
					if (box == null)
						return null;
					run = box.Source.ClientRuns[0];
				}
				newIp = run.SelectAtStart(box);
				newIp = newIp.NextIp(distance);
			}
			else
			{
				newIp = new InsertionPoint(Hookup, StringPosition + distance, AssociatePrevious);
			}
			return newIp;
		}

		/// <summary>
		/// This variable is used only for IPs created by up or down arrow. It records the center X position
		/// from which the movement stared, relative to the left of the root box in layout pixels.
		/// This is the starting X position for further up or down movement.
		/// </summary>
		private int m_upDownArrowX = int.MinValue;

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
				case Keys.Home:
					if (args.Control)
						result = Para.Root.SelectAtStart();
					else
						result = Para.SelectAtStart();
					break;
				case Keys.End:
					if (args.Control)
						result = Para.Root.SelectAtEnd();
					else
						result = Para.SelectAtEnd();
					break;
				case Keys.Down:
					result = MoveUpOrDown(3);
					break;
				case Keys.Up:
					result = MoveUpOrDown(-3);
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

		/// <summary>
		/// Move the insertion point up or down (depending on the sign of delta).
		/// Records an x position in the selection, so further moves down
		/// from the new IP as a starting point stay aligned as nearly as possible with the start position.
		/// </summary>
		private InsertionPoint MoveUpOrDown(int delta)
		{
			Rectangle originalLocation;
			int x;
			using (var gh = Para.Root.Site.DrawingInfo)
			{
				originalLocation = GetSelectionLocation(gh.VwGraphics, gh.Transform);
				x = (originalLocation.Right + originalLocation.Left)/2;
				if (m_upDownArrowX > int.MinValue)
					x = gh.Transform.ToPaintX(m_upDownArrowX);
			}

			int y = (originalLocation.Bottom + originalLocation.Top)/2;
			// Find the string box obtained by clicking right in the middle of the original selection.
			// We want to be in a box below this, so we need to know which it is.
			Box origBox;
			using (var gh = Para.Root.Site.DrawingInfo)
			{
				PaintTransform leafTransform;
				origBox = Para.Root.FindBoxAt(new Point(x, y), gh.Transform, out leafTransform);
			}
			int yMax = int.MaxValue; // adjusted within loop; this allows at least one iteration
			int yMin = int.MinValue; // also adjusted within loop.
			for (y = delta > 0 ? originalLocation.Bottom : originalLocation.Top; yMin < y && y < yMax ; y += delta)
			{
				using (var gh = Para.Root.Site.DrawingInfo)
				{
					PaintTransform leafTransform;
					var where = new Point(x, y);
					var targetBox = Para.Root.FindBoxAt(where, gh.Transform, out leafTransform);
					// We adjust this each time in anticipation that eventually we may need to expand lazy boxes,
					// which might change the limit.
					yMax = gh.Transform.ToPaintY(Para.Root.Height);
					yMin = gh.Transform.ToPaintY(0);
					if (!(targetBox is StringBox))
						continue; // don't know how to select here (at least not yet)
					if (targetBox == origBox)
						continue; // haven't actually moved, yet.
					// Review JohnT: could it be anything else? If so, do we want to return it, return null, or
					// keep going?
					var result = targetBox.MakeSelectionAt(where, gh.VwGraphics, leafTransform) as InsertionPoint;
					result.m_upDownArrowX = gh.Transform.ToLayoutX(x);
					return result;
				}
			}
			return null;
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
		public IClientRun ContainingRun
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

		public RangeSelection ExpandToWord()
		{
			var cpe = LgIcuCharPropEngineClass.Create();
			RangeSelection rangeSel = new RangeSelection(this, new InsertionPoint(Hookup, StringPosition + 1, AssociatePrevious));
			var backwardSel = new InsertionPoint(Hookup, StringPosition, AssociatePrevious);
			var forwardSel = new InsertionPoint(Hookup, StringPosition, AssociatePrevious);
			while (true)
			{
				if (backwardSel.StringPosition == 0)
				{
					break;
				}
				backwardSel.StringPosition--;
				char testChar = backwardSel.ContainingRun.Text[backwardSel.StringPosition];
				int testInt = testChar;
				if (Surrogates.IsTrailSurrogate(testChar))
				{
					backwardSel.StringPosition--;
					testInt = Surrogates.Int32FromSurrogates(backwardSel.ContainingRun.Text[backwardSel.StringPosition], testChar);
				}
				else if (Surrogates.IsLeadSurrogate(testChar))
				{
					testInt = Surrogates.Int32FromSurrogates(testChar, backwardSel.ContainingRun.Text[backwardSel.StringPosition+1]);
				}
				if (!cpe.get_IsNumber(testInt) && !cpe.get_IsWordForming(testInt) ||
					backwardSel.ContainingRun.WritingSystemAt(backwardSel.StringPosition) != ContainingRun.WritingSystemAt(StringPosition))
				{
					backwardSel.StringPosition++;
					break;
				}
				rangeSel = new RangeSelection(backwardSel, this);
			}
			backwardSel = rangeSel.Anchor;
			while (true)
			{
				if (forwardSel.StringPosition == forwardSel.ContainingRun.Length)
				{
					if (backwardSel.StringPosition == forwardSel.StringPosition)
						return null;
					break;
				}
				char testChar = forwardSel.ContainingRun.Text[forwardSel.StringPosition];
				int testInt = testChar;
				if (Surrogates.IsLeadSurrogate(testChar))
				{
					forwardSel.StringPosition++;
					testInt = Surrogates.Int32FromSurrogates(testChar, forwardSel.ContainingRun.Text[forwardSel.StringPosition]);
					testChar = (char)testInt;
				}
				else if (Surrogates.IsTrailSurrogate(testChar))
				{
					testInt = Surrogates.Int32FromSurrogates(forwardSel.ContainingRun.Text[forwardSel.StringPosition-1], testChar);
					testChar = (char)testInt;
				}
				if (!cpe.get_IsNumber(testInt) && !cpe.get_IsWordForming(testInt) ||
					forwardSel.ContainingRun.WritingSystemAt(forwardSel.StringPosition) != ContainingRun.WritingSystemAt(StringPosition))
				{
					if (testChar.Equals(" ".ToCharArray()[0]))
					{
						forwardSel.StringPosition++;
						rangeSel = new RangeSelection(backwardSel, forwardSel);
					}
					break;
				}
				forwardSel.StringPosition++;
				rangeSel = new RangeSelection(backwardSel, forwardSel);
			}
			return rangeSel;
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

		public override bool InsertRtfString(string input)
		{
			if (!CanInsertText)
				return false; // cannot modify; should we report this somehow?
			Invalidate(); // Hide the old selection while we still know where it is.
			Action makeSelection;
			if(ParagraphOperations == null)
				return false;
			if (!ParagraphOperations.InsertRTF(this, input, out makeSelection))
				return false;
			RootBox.Site.PerformAfterNotifications(makeSelection);
			return true;
		}

		public override bool InsertTsString(ITsString input)
		{
			if (!CanInsertText || input == null)
				return false; // cannot modify; should we report this somehow?
			Invalidate(); // Hide the old selection while we still know where it is.
			if (input.Text.Contains("\r") || input.Text.Contains("\n"))
			{
				MultiLineInsertData inputInsertData = new MultiLineInsertData(this, new List<ITsString> {input}, null);
				Action makeSelection;
				ParagraphOperations.InsertLines(inputInsertData, out makeSelection);
				RootBox.Site.PerformAfterNotifications(makeSelection);
				return true;
			}
			Hookup.InsertText(this, input);
			StringPosition += input.Length;
			return true;
		}

		/// <summary>
		/// Insert the specified text at the insertion point.
		/// Enhance JohnT: normalize the string after the edit, maintaining the posititon of the IP correctly.
		/// </summary>
		public override bool InsertText(string input)
		{
			if (!CanInsertText)
				return false; // cannot modify; should we report this somehow?
			Invalidate(); // Hide the old selection while we still know where it is.
			if (input.Contains("\r") || input.Contains("\n"))
			{
				MultiLineInsertData inputInsertData = new MultiLineInsertData(this, input, null);
				Action makeSelection;
				ParagraphOperations.InsertLines(inputInsertData, out makeSelection);
				RootBox.Site.PerformAfterNotifications(makeSelection);
				return true;
			}
			Hookup.InsertText(this, input);
			StringPosition += input.Length;
			return true;
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
		private void CopyFrom(InsertionPoint other)
		{
			Hookup = other.Hookup;
			StringPosition = other.StringPosition;
			AssociatePrevious = other.AssociatePrevious;
		}

		/// <summary>
		/// Checks whether the Selection's style can be changed
		/// </summary>
		public override bool CanApplyStyle(string style)
		{
			return Hookup.CanApplyStyle(this, this, style);
		}

		/// <summary>
		/// Changes the Selection's style
		/// </summary>
		public override void ApplyStyle(string style)
		{
			IStyle styleToBeApplied = RootBox.Style.Stylesheet.Style(style);
			if(styleToBeApplied == null)
				return;
			if (styleToBeApplied.IsParagraphStyle)
			{
				ISelectionRestoreData restoreData = RestoreData(RootBox.Selection);
				IParagraphOperations paragraphOps;
				GroupHookup parentHookup;
				int index;
				if (!GetParagraphOps(out paragraphOps, out parentHookup, out index))
					return;
				paragraphOps.ApplyParagraphStyle(index, 1, style);
				restoreData.RestoreSelection().Install();
			}
			else
			{
				StyleToBeApplied = styleToBeApplied;
			}
		}

		/// <summary>
		/// Delete the selected material, or whatever else is appropriate when the Delete key is pressed.
		/// (Insertion Point deletes the following character.)
		/// </summary>
		public override void Delete()
		{
			Invalidate(); // while we still know the old position.
			if (Hookup == null)
				return;
			if (StringPosition == Hookup.Text.Length)
			{
				if (Hookup.ClientRunIndex == Para.Source.ClientRuns.Count - 1)
				{
					DeleteLineBreak();
					return;
				}
				if (Para.Source.ClientRuns[Hookup.ClientRunIndex + 1] is TextClientRun)
				{
					// Delete at end of previous run.
					var nextClientRun = Para.Source.NonEmptyStringClientRunBeginningAfter(Hookup.ClientRunIndex);
					if (nextClientRun == null)
						return;
					// Enhance JohnT: maybe some kind of hookup can merge with previous para or delete an embedded object?
					CopyFrom(nextClientRun.SelectAtStart(Para));
					//Debug.Assert(StringPosition != Hookup.Text.Length - 1, "should have selected at the START of a non-empty run");
				}
			}
			var insertionPointEnd = new InsertionPoint(Hookup, StringPosition + 1, AssociatePrevious);
			if (!Hookup.CanDelete(this, insertionPointEnd))
				return;
			Hookup.Delete(this, insertionPointEnd);
		}

		private void DeleteLineBreak()
		{
			if (Hookup is StringHookup || Hookup is TssHookup)
			{
				IParagraphOperations paragraphOps;
				GroupHookup parentHookup;
				int index;
				if (!GetParagraphOps(out paragraphOps, out parentHookup, out index))
					return;
				if (index == parentHookup.Children.IndexOf(parentHookup.LastChild))
				{
					// at the end of the sequence.
					// Enhance JohnT: could look for another paragraph ops higher? additional interface on paragraphOps?
					return;
				}
				var nextItem = parentHookup.Children[index + 1];
				var nextIp = nextItem.SelectAtStart();
				if (nextIp == null)
					return; // for some reason we can't do it (maybe a box at end of last para?)
				var range = new RangeSelection(this, nextIp);
				Action makeSelection;
				paragraphOps.InsertString(range, "", out makeSelection);
				RootBox.Site.PerformAfterNotifications(makeSelection);
			}
		}

		/// <summary>
		/// Return true if Delete() will delete something. Default is that it will not.
		/// </summary>
		public override bool CanDelete()
		{
			var sel = new InsertionPoint(Hookup, StringPosition, AssociatePrevious);
			sel = sel.MoveByKey(new KeyEventArgs(Keys.Right))as InsertionPoint;
			return Hookup.CanDelete(this, sel);
		}

		/// <summary>
		/// Implement the backspace key function (delete one character, or merge two paragraphs).
		/// </summary>
		public void Backspace()
		{
			Invalidate(); // while we still know the old position.
			if (Hookup == null)
				return;
			if (StringPosition == 0)
			{
				if (Hookup.ClientRunIndex == 0)
				{
					BackspaceDeleteLineBreak();
					return;
				}
				if (Para.Source.ClientRuns[Hookup.ClientRunIndex - 1] is TextClientRun)
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
			if (Hookup is StringHookup || Hookup is TssHookup)
			{
				IParagraphOperations paragraphOps;
				GroupHookup parentHookup;
				int index;
				if(!GetParagraphOps(out paragraphOps, out parentHookup, out index))
					return;
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

		public bool GetParagraphOps(out IParagraphOperations paragraphOps, out GroupHookup parentHookup, out int index)
		{
			paragraphOps = null;
			ItemHookup itemHookup = null;
			parentHookup = null;
			foreach (var hookup in Hookup.Parents)
			{
				var isoHookup = hookup as IHaveParagagraphOperations;
				if (isoHookup != null && isoHookup.GetParagraphOperations() != null)
				{
					parentHookup = hookup;
					paragraphOps = isoHookup.GetParagraphOperations();
					break;
				}
				itemHookup = hookup as ItemHookup;
			}
			if (paragraphOps == null || itemHookup == null)
			{
				index = -1;
				return false;
			}
			index = parentHookup.Children.IndexOf(itemHookup);
			return true;
		}

		internal IParagraphOperations ParagraphOperations
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
			IStyle style = null;
			if(RootBox != null && RootBox.Style != null && RootBox.Style.Stylesheet != null)
				style = RootBox.Style.Stylesheet.Style(Hookup.GetStyleNameAt(this));
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
			StyleToBeApplied = style;
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
