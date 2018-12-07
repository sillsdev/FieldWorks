// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class represents a Views rootsite control that is used to display a pattern.
	/// It notifies the pattern control about key presses and right clicks.
	/// </summary>
	internal class PatternView : RootSiteControl
	{
		public new event EventHandler SelectionChanged;
		public event EventHandler<RemoveItemsRequestedEventArgs> RemoveItemsRequested;
		public event EventHandler<ContextMenuRequestedEventArgs> ContextMenuRequested;

		private IPatternControl m_patternControl;
		private int m_hvo;
		private PatternVcBase m_vc;
		private int m_rootFrag = -1;
		private ISilDataAccess m_sda;

		/// <summary>
		/// We MUST inherit from this, not from just EditingHelper; otherwise, the right event isn't
		/// connected (in an overide of OnEditingHelperCreated) for us to get selection change notifications.
		/// </summary>
		private sealed class PatternEditingHelper : RootSiteEditingHelper
		{
			public PatternEditingHelper(LcmCache cache, IEditingCallbacks callbacks)
				: base(cache, callbacks)
			{
			}

			public override bool CanCopy()
			{
				return false;
			}

			public override bool CanCut()
			{
				return false;
			}

			public override bool CanPaste()
			{
				return false;
			}

			/// <summary>
			/// We don't want typing to do anything.  On Linux, it does without this method,
			/// causing a crash immediately.  See FWNX-1337.
			/// </summary>
			protected override void CallOnTyping(string str, Keys modifiers)
			{
			}
		}

		protected override EditingHelper CreateEditingHelper()
		{
			// we can't just use the Editable property to disable copy/cut/paste, because we want
			// the view to be read only, so instead we use a custom EditingHelper
			return new PatternEditingHelper(Cache, this);
		}

		public void Init(int hvo, IPatternControl patternControl, PatternVcBase vc, int rootFrag, ISilDataAccess sda)
		{
			m_patternControl = patternControl;
			Cache = PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
			m_hvo = hvo;
			m_vc = vc;
			m_sda = sda;
			m_rootFrag = rootFrag;
			if (m_rootb == null)
			{
				MakeRoot();
			}
			else if (m_hvo != 0)
			{
				m_rootb.SetRootObject(m_hvo, m_vc, m_rootFrag, FwUtils.StyleSheetFromPropertyTable(PropertyTable));
				m_rootb.Reconstruct();
			}
		}

		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}

			base.MakeRoot();
			// the default value of 4 for MaxParasToScan isn't high enough when using the arrow keys to move
			// the cursor between items in a rule when the number of lines in the rule is high, since there might
			// be a large number of non-editable empty lines in a pile
			m_rootb.MaxParasToScan = 10;
			m_rootb.DataAccess = m_sda;
			if (m_hvo != 0)
			{
				m_rootb.SetRootObject(m_hvo, m_vc, m_rootFrag, FwUtils.StyleSheetFromPropertyTable(PropertyTable));
			}
		}

		/// <summary>
		/// override this to allow deleting an item IF the key is Delete.
		/// </summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				RemoveItemsRequested?.Invoke(this, new RemoveItemsRequestedEventArgs(true));
				e.Handled = true;
			}
			base.OnKeyDown(e);
		}

		/// <summary>
		/// override this to allow deleting an item IF the key is Backspace.
		/// </summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == (char) Keys.Back)
			{
				RemoveItemsRequested?.Invoke(this, new RemoveItemsRequestedEventArgs(false));
				e.Handled = true;
			}
			base.OnKeyPress(e);
		}

		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			UpdateSelection(vwselNew);
			SelectionChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Update the new selection. This is called by rule formula view when selection changes.
		/// </summary>
		private void UpdateSelection(IVwSelection vwselNew)
		{
			var sel = SelectionHelper.Create(vwselNew, this);
			if (sel != null)
			{
				var ctxt = m_patternControl.GetContext(sel);
				// if context is null then we are trying to select outside of a single context
				if (ctxt == null)
				{
					if (sel.IsRange)
					{
						// ensure that a range selection only occurs within one context
						var topCtxt = m_patternControl.GetContext(sel, SelectionHelper.SelLimitType.Top);
						var bottomCtxt = m_patternControl.GetContext(sel, SelectionHelper.SelLimitType.Bottom);
						var limit = SelectionHelper.SelLimitType.Top;
						if (topCtxt != null)
						{
							limit = SelectionHelper.SelLimitType.Top;
							ctxt = topCtxt;
						}
						else if (bottomCtxt != null)
						{
							limit = SelectionHelper.SelLimitType.Bottom;
							ctxt = bottomCtxt;
						}

						if (ctxt != null)
						{
							var newSel = SelectCell(ctxt, limit == SelectionHelper.SelLimitType.Bottom, false);
							sel.ReduceToIp(limit);
							var otherSel = sel.SetSelection(this, false, false);
							if (sel.Selection.EndBeforeAnchor)
							{
								RootBox.MakeRangeSelection(limit == SelectionHelper.SelLimitType.Top ? newSel : otherSel, limit == SelectionHelper.SelLimitType.Top ? otherSel : newSel, true);
							}
							else
							{
								RootBox.MakeRangeSelection(limit == SelectionHelper.SelLimitType.Top ? otherSel : newSel, limit == SelectionHelper.SelLimitType.Top ? newSel : otherSel, true);
							}
						}

					}
				}
				else
				{
					AdjustSelection(sel);
				}
			}
		}

		/// <summary>
		/// Adjusts the selection.
		/// </summary>
		private void AdjustSelection(SelectionHelper sel)
		{
			IVwSelection anchorSel;
			int curHvo, curIch, curTag;
			// anchor IP
			if (!GetSelectionInfo(sel, SelectionHelper.SelLimitType.Anchor, out anchorSel, out curHvo, out curIch, out curTag))
			{
				return;
			}

			IVwSelection endSel;
			int curEndHvo, curEndIch, curEndTag;
			// end IP
			if (!GetSelectionInfo(sel, SelectionHelper.SelLimitType.End, out endSel, out curEndHvo, out curEndIch, out curEndTag))
			{
				return;
			}

			// create range selection
			var vwSel = RootBox.MakeRangeSelection(anchorSel, endSel, false);
			if (vwSel != null)
			{
				ITsString tss;
				int ws;
				bool prev;

				// only install the adjusted selection if it is different then the current selection
				int wholeHvo, wholeIch, wholeTag, wholeEndHvo, wholeEndIch, wholeEndTag;
				vwSel.TextSelInfo(false, out tss, out wholeIch, out prev, out wholeHvo, out wholeTag, out ws);
				vwSel.TextSelInfo(true, out tss, out wholeEndIch, out prev, out wholeEndHvo, out wholeEndTag, out ws);

				if (wholeHvo != curHvo || wholeEndHvo != curEndHvo || wholeIch != curIch || wholeEndIch != curEndIch || wholeTag != curTag || wholeEndTag != curEndTag)
				{
					vwSel.Install();
				}
			}
		}

		/// <summary>
		/// Creates a selection IP for the specified limit.
		/// </summary>
		private bool GetSelectionInfo(SelectionHelper sel, SelectionHelper.SelLimitType limit, out IVwSelection vwSel, out int curHvo, out int curIch, out int curTag)
		{
			vwSel = null;
			curHvo = 0;
			curIch = -1;
			curTag = -1;

			var obj = m_patternControl.GetItem(sel, limit);
			if (obj == null)
			{
				return false;
			}

			ITsString curTss;
			int ws;
			bool prev;

			sel.Selection.TextSelInfo(limit == SelectionHelper.SelLimitType.End, out curTss, out curIch, out prev, out curHvo, out curTag, out ws);

			var ctxt = m_patternControl.GetContext(sel);
			var index = m_patternControl.GetItemContextIndex(ctxt, obj);

			if (!sel.IsRange)
			{
				// if the current selection is an IP, check if it is in one of the off-limits areas, and move the IP
				if (curIch == 0 && curTag == PatternVcBase.ktagLeftNonBoundary)
				{
					// the cursor is at a non-selectable left edge of an item, so
					// move to the selectable left edge
					SelectLeftBoundary(ctxt, index, true);
					return false;
				}
				if (curIch == curTss.Length && curTag == PatternVcBase.ktagLeftNonBoundary)
				{
					// the cursor has been moved to the left from the left boundary, so move the
					// cursor to the previous item in the cell or the previous cell
					if (index > 0)
					{
						SelectAt(ctxt, index - 1, false, true, true);
					}
					else
					{
						var prevCtxt = m_patternControl.GetPrevContext(ctxt);
						if (prevCtxt != null)
						{
							SelectCell(prevCtxt, false, true);
						}
						else
						{
							SelectLeftBoundary(ctxt, index, true);
						}

					}
					return false;
				}
				if (curIch == curTss.Length && curTag == PatternVcBase.ktagRightNonBoundary)
				{
					// the cursor is at a non-selectable right edge of an item, so move to the
					// selectable right edge
					SelectRightBoundary(ctxt, index, true);
					return false;
				}
				if (curIch == 0 && curTag == PatternVcBase.ktagRightNonBoundary)
				{
					// the cursor has been moved to the right from the right boundary, so move the
					// cursor to the next item in the cell or the next cell
					if (index < m_patternControl.GetContextCount(ctxt) - 1)
					{
						SelectAt(ctxt, index + 1, true, true, true);
					}
					else
					{
						var nextCtxt = m_patternControl.GetNextContext(ctxt);
						if (nextCtxt != null)
						{
							SelectCell(nextCtxt, true, true);
						}
						else
						{
							SelectRightBoundary(ctxt, index, true);
						}

					}
					return false;
				}
				// when you click to the left of a ZWSP left boundary, Views might place the cursor to the right of the
				// ZWSP. Move the cursor to the proper location before the ZWSP.
				if (curTss.Text == "\u200b" && curIch == 1 && curTag == PatternVcBase.ktagLeftBoundary)
				{
					SelectLeftBoundary(ctxt, index, true);
					return false;
				}

				if (!sel.Selection.IsEditable)
					return false;
			}

			// find the beginning of the currently selected item
			var initialSel = SelectAt(ctxt, index, true, false, false);
			ITsString tss;
			var selCellIndex = index;
			int initialHvo, initialIch, initialTag;
			if (initialSel == null)
			{
				return false;
			}
			initialSel.TextSelInfo(false, out tss, out initialIch, out prev, out initialHvo, out initialTag, out ws);
			// are we at the beginning of an item?
			if ((curHvo == initialHvo && curIch == initialIch && curTag == initialTag) || (curIch == 0 && curTag == PatternVcBase.ktagLeftBoundary))
			{
				// if the current selection is an IP, then don't adjust anything
				if (!sel.IsRange)
				{
					return false;
				}

				// if we are the beginning of the current item, and the current selection is a range, and the end is before the anchor,
				// then do not include the current item in the adjusted range selection
				if (sel.Selection.EndBeforeAnchor && limit == SelectionHelper.SelLimitType.Anchor)
				{
					selCellIndex = index - 1;
				}
			}
			else
			{
				int finalIch, finalHvo, finalTag;
				var finalSel = SelectAt(ctxt, index, false, false, false);
				finalSel.TextSelInfo(false, out tss, out finalIch, out prev, out finalHvo, out finalTag, out ws);
				// are we at the end of an item?
				if ((curHvo == finalHvo && curIch == finalIch && curTag == finalTag) || (curIch == curTss.Length && curTag == PatternVcBase.ktagRightBoundary))
				{
					// if the current selection is an IP, then don't adjust anything
					if (!sel.IsRange)
					{
						return false;
					}

					// if we are the end of the current item, and the current selection is a range, and the anchor is before the end,
					// then do not include the current item in the adjusted range selection
					if (!sel.Selection.EndBeforeAnchor && limit == SelectionHelper.SelLimitType.Anchor)
					{
						selCellIndex = index + 1;
					}
				}
			}
			vwSel = SelectAt(ctxt, selCellIndex, limit == SelectionHelper.SelLimitType.Anchor ? !sel.Selection.EndBeforeAnchor : sel.Selection.EndBeforeAnchor, false, false);
			return vwSel != null;
		}

		private void SelectLeftBoundary(object ctxt, int index, bool install)
		{
			var levels = new List<SelLevInfo>(m_patternControl.GetLevelInfo(ctxt, index));
			try
			{
				RootBox.MakeTextSelection(0, levels.Count, levels.ToArray(), PatternVcBase.ktagLeftBoundary, 0, 0, 0, 0, false, -1, null, install);
			}
			catch
			{
			}
		}

		private void SelectRightBoundary(object ctxt, int index, bool install)
		{
			var levels = m_patternControl.GetLevelInfo(ctxt, index);
			try
			{
				RootBox.MakeTextSelection(0, levels.Length, levels, PatternVcBase.ktagRightBoundary, 0, 1, 1, 0, false, -1, null, install);
			}
			catch
			{
			}
		}

		/// <summary>
		/// Moves the cursor to the specified position in the specified cell.
		/// </summary>
		/// <param name="ctxt">The context.</param>
		/// <param name="index">Index of the item in the cell.</param>
		/// <param name="initial">if <c>true</c> move the cursor to the beginning of the specified item, otherwise it is moved to the end</param>
		/// <param name="editable">if <c>true</c> move the cursor to the first editable position</param>
		/// <param name="install">if <c>true</c> install the selection</param>
		/// <returns>The new selection</returns>
		public IVwSelection SelectAt(object ctxt, int index, bool initial, bool editable, bool install)
		{
			var levels = m_patternControl.GetLevelInfo(ctxt, index);
			if (levels == null)
			{
				var count = m_patternControl.GetContextCount(ctxt);
				if (count == 0)
				{
					var newSel = new SelectionHelper();
					newSel.SetTextPropId(SelectionHelper.SelLimitType.Anchor, m_patternControl.GetFlid(ctxt));
					return newSel.SetSelection(this, install, false);
				}
				levels = m_patternControl.GetLevelInfo(ctxt, initial ? 0 : count - 1);
			}

			return RootBox.MakeTextSelInObj(0, levels.Length, levels, 0, null, initial, editable, false, false, install);
		}

		private IVwSelection SelectCell(object ctxt, bool initial, bool install)
		{
			return SelectAt(ctxt, -1, initial, true, install);
		}

		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			var sel = RootBox.MakeSelAt(pt.X, pt.Y, new Rect(rcSrcRoot.Left, rcSrcRoot.Top, rcSrcRoot.Right, rcSrcRoot.Bottom), new Rect(rcDstRoot.Left, rcDstRoot.Top, rcDstRoot.Right, rcDstRoot.Bottom), false);
			if (sel == null)
			{
				return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot); // no object, so quit and let base handle it
			}
			var e = new ContextMenuRequestedEventArgs(sel);
			ContextMenuRequested?.Invoke(this, e);
			return e.Handled || base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
		}
	}
}
