// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	internal class XmlBrowseView : XmlBrowseViewBase
	{
		private bool m_fInSelectionChanged;

		/// <summary />
		internal XmlBrowseView()
		{
			AccessibleName = "XmlBrowseView";
			// tab should move the cursor between cells in the table.
			AcceptsTab = true;
		}

		/// <summary>
		/// Hook this even to receive a notification of the word and HVO clicked on when the user clicks
		/// in the view.
		/// </summary>
		public event ClickCopyEventHandler ClickCopy;

		/// <summary>
		/// Return the VC. It has some important functions related to interpreting fragment IDs
		/// that the filter bar needs.
		/// </summary>
		internal override XmlBrowseViewVc Vc
		{
			get
			{
				if (m_xbvvc == null)
				{
					m_xbvvc = new XmlBrowseViewVc(m_nodeSpec, MainTag, this);
				}
				return base.Vc;
			}
		}

		#region Overrides of XmlBrowseViewBase

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				PropertyTable?.GetValue<IFwMainWnd>(FwUtils.window)?.IdleQueue?.Remove(FocusMeAgain);
				Subscriber.Unsubscribe(GetCorrespondingPropertyName("readOnlyBrowse"), SetSelectedRowHighlighting);
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// Cause the behavior to switch to the current setting of ReadOnlyBrowse.
		/// Override if the behavior should be different than this.
		/// </summary>
		private void SetSelectedRowHighlighting(object newValue)
		{
			SetSelectedRowHighlighting();
		}

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			Subscriber.Subscribe(GetCorrespondingPropertyName("readOnlyBrowse"), SetSelectedRowHighlighting);
		}

		#endregion

		private static ITsString StripTrailingNewLine(ITsString tss)
		{
			var val = tss.Text ?? string.Empty;
			var cchStrip = 0;
			var cch = val.Length;
			if (cch == 0)
			{
				return tss;
			}
			var ch = val[cch - 1];
			if (ch == '\n' || ch == '\r')
			{
				cchStrip++;
				if (cch > 1)
				{
					ch = val[cch - 2];
					if (ch == '\n' || ch == '\r')
					{
						cchStrip++;
					}
				}
			}
			if (cchStrip == 0)
			{
				return tss;
			}
			var tsb = tss.GetBldr();
			tsb.ReplaceTsString(cch - cchStrip, cch, null);
			return tsb.GetString();
		}

		/// <summary>
		/// If we are in ReadOnlySelect mode, this handles the up and down arrow keys to change
		/// the selection.
		/// </summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (ReadOnlySelect && !e.Handled && m_selectedIndex != -1)
			{
				var cobj = SpecialCache.get_VecSize(m_hvoRoot, MainTag);
				switch (e.KeyCode)
				{
					case Keys.Down:
						if (m_selectedIndex < cobj - 1)
						{
							SelectedIndex = m_selectedIndex + 1;
						}
						e.Handled = true;
						break;
					case Keys.Up:
						if (m_selectedIndex > 0)
						{
							SelectedIndex = m_selectedIndex - 1;
						}
						e.Handled = true;
						break;
				}
			}
			// OnKeyDown may move us to a new record, and the associated DataTree will try hard to steal the focus.
			// This should not happen following a key stroke, so work hard to keep the focus here. (LT-11792).
			FocusMe();
		}

		/// <summary>
		/// If we are in ReadOnlySelect mode, intercept the click, and select the appropriate row
		/// without installing a normal selection.
		/// </summary>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			//If XmlBrowseView did not receive a mouse down event then we do not want to
			//do anything on the mouseUp because the mouseUp would have come from clicking
			//somewhere else. LT-8939
			if (!m_fMouseUpEnabled)
			{
				return;
			}
			try
			{
				if (m_selectedIndex == -1)
				{
					return; // Can't do much in an empty list, so quit.
				}
				m_fHandlingMouseUp = true;
				// Note all the stuff we might want to know about what was clicked that we will
				// use later. We want to get this now before anything changes, because there can
				// be scrolling effects from converting dummy objects to real.
				var vwsel = MakeSelectionAt(e);
				var newSelectedIndex = GetRowIndexFromSelection(vwsel, true);
				// If we're changing records, we need to do some tricks to keep the selection in the place
				// clicked and the focus here. Save the information we will need.
				SelectionHelper clickSel = null;
				if (newSelectedIndex != SelectedIndex && vwsel != null && SelectionHelper.IsEditable(vwsel))
				{
					clickSel = SelectionHelper.Create(vwsel, this);
				}
				ITsString tssWord = null; // word clicked for click copy
				ITsString tssSource = null; // whole source string of clicked cell for click copy
				var hvoNewSelRow = 0; // hvo of new selected row (only for click copy)
				var ichStart = 0; // of tssWord in tssSource
				if (ClickCopy != null && e.Button == MouseButtons.Left && newSelectedIndex >= 0)
				{
					if (vwsel != null && vwsel.SelType == VwSelType.kstText)
					{
						var icol = vwsel.get_BoxIndex(false, 3);
						if (icol != Vc.OverrideAllowEditColumn + 1)
						{
							var vwSelWord = vwsel.GrowToWord();
							// GrowToWord() can return a null -- see LT-9163 and LT-9349.
							if (vwSelWord != null)
							{
								vwSelWord.GetSelectionString(out tssWord, " ");
								tssWord = StripTrailingNewLine(tssWord);
								hvoNewSelRow = SpecialCache.get_VecItem(m_hvoRoot, MainTag, newSelectedIndex);
								int hvoObj, tag, ws;
								bool fAssocPrev;
								vwSelWord.TextSelInfo(false, out tssSource, out ichStart, out fAssocPrev, out hvoObj, out tag, out ws);
							}
						}
					}
				}
				// We need to manually change the index for ReadOnly views.
				// SimpleRootSite delegates RightMouseClickEvent to our RecordBrowseView parent,
				// which also makes the selection for us..
				if (ReadOnlySelect && e.Button != MouseButtons.Right)
				{
					if (m_xbvvc.HasSelectColumn && e.X < m_xbvvc.SelectColumnWidth)
					{
						base.OnMouseUp(e); // allows check box to operate.
					}
				}
				else
				{
					// If we leave this set, the base method call's side effects like updating the WS combo
					// don't happen.
					m_fHandlingMouseUp = false;
					base.OnMouseUp(e); // normal behavior.
					m_fHandlingMouseUp = true;
				}
				SetSelectedIndex(newSelectedIndex);
				if (tssWord != null)
				{
					// We're doing click copies; generate an event.
					// Do this AFTER other actions which may change the current line.
					ClickCopy(this, new ClickCopyEventArgs(tssWord, hvoNewSelRow, tssSource, ichStart));
				}
				if (clickSel == null)
				{
					return;
				}
				IVwSelection finalSel = null;
				// There seem to be some cases where the selection helper can't restore the selection.
				// One that came up in FWR-3666 was clicking on a check box.
				// If we can't re-establish an editiable selection just let the default behavior continue.
				try
				{
					finalSel = clickSel.MakeRangeSelection(RootBox, false);
				}
				catch (Exception)
				{
				}
				if (finalSel != null && SelectionHelper.IsEditable(finalSel))
				{
					finalSel.Install();
					FocusMe();
				}
			}
			finally
			{
				m_fHandlingMouseUp = false;
				m_fMouseUpEnabled = false;
			}
		}

		private void FocusMe()
		{
			if (!IsDisposed)
			{
				Focus();
			}
			// Typically we get one idle event before the one in which the DataTree tries to focus its first
			// possible slice. We need to do it one more time for it to stick.
			// Try five times to really get the focus!
			m_idleFocusCount = 5;
			PropertyTable.GetValue<IFwMainWnd>(FwUtils.window).IdleQueue.Add(IdleQueuePriority.High, FocusMeAgain);
		}

		private int m_idleFocusCount;

		private bool FocusMeAgain(object arg)
		{
			if (IsDisposed)
			{
				throw new InvalidOperationException("Thou shalt not call methods after I am disposed!");
			}
			Focus();
			if (m_idleFocusCount == 0)
			{
				return true; // idle task complete
			}
			m_idleFocusCount--;
			return false;
		}

		/// <summary>
		/// For testing we allow this to be simulated.
		/// </summary>
		public void SimulateDoubleClick(EventArgs e)
		{
			OnDoubleClick(e);
		}

		/// <summary>
		/// Process mouse double click
		/// </summary>
		protected override void OnDoubleClick(EventArgs e)
		{
			if (!ReadOnlySelect)
			{
				base.OnDoubleClick(e);
			}
			else if (SelectedIndex != -1)
			{
				var e1 = new FwObjectSelectionEventArgs(SelectedObject, SelectedIndex);
				m_bv.OnDoubleClick(e1);
			}
		}

		/// <summary>
		/// We override this method to make a selection in all of the views that are in a
		/// synced group. This fixes problems where the user changes the selection in one of
		/// the slaves, but the master is not updated. Thus the view is not scrolled as the
		/// groups scroll position only scrolls the master's selection into view. (TE-3380)
		/// </summary>
		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			// Guard against recursive calls, typically caused by the MakeTextSelection call below.
			if (m_fInSelectionChanged)
			{
				return;
			}
			try
			{
				m_fInSelectionChanged = true;
				// Make sure we're not handling MouseDown since it
				// handles SelectionChanged and DoSelectionSideEffects.
				// No need to do it again.
				if (ReadOnlySelect || m_fHandlingMouseUp)
				{
					return;
				}
				if (vwselNew == null)
				{
					return;
				}
				// The selection can apparently be invalid on rare occasions, and will lead
				// to a crash below trying to call CLevels.  See LT-10301.  Treat it the
				// same as a null selection.
				if (!vwselNew.IsValid)
				{
					return;
				}
				base.HandleSelectionChange(rootb, vwselNew);
				m_wantScrollIntoView = false; // It should already be visible here.
				// Collect all the information we can about the selection.
				int ihvoRoot;
				int tagTextProp;
				int cpropPrevious;
				int ichAnchor;
				int ichEnd;
				int ws;
				bool fAssocPrev;
				int ihvoEnd;
				ITsTextProps ttpBogus;
				var cvsli = vwselNew.CLevels(false) - 1;
				if (cvsli < 0)
				{
					return;// Nothing useful we can do.
				}
				// Main array of information retrieved from sel that made combo.
				var rgvsli = SelLevInfo.AllTextSelInfo(vwselNew, cvsli, out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd, out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);
				// The call to the base implementation can invalidate the selection. It's rare, but quite
				// possible. (See the comment in EditingHelper.SelectionChanged() following
				// Commit().) This test fixes LT-4731.
				if (vwselNew.IsValid)
				{
					DoSelectionSideEffects(vwselNew);
				}
				else
				{
					// But if the selection is invalid, and we do nothing about it, then we can
					// type only one character at a time in a browse cell because we no longer
					// have a valid selection.  See LT-6443.
					rootb.MakeTextSelection(ihvoRoot, cvsli, rgvsli, tagTextProp, cpropPrevious, ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, ttpBogus, true);
					DoSelectionSideEffects(rootb.Selection);
				}
				m_wantScrollIntoView = true;
			}
			finally
			{
				m_fInSelectionChanged = false;
			}
		}

		/// <summary>
		/// In Mono, the window is created after the index has already been set, so restore
		/// the index if it gets reset.  This code is safe on Windows, where the index is set
		/// after the window is created, and having it active protects against the .Net
		/// runtime changing its order of events.
		/// </summary>
		/// <remarks>
		/// See FWNX-1076.
		/// </remarks>
		protected override void OnHandleCreated(EventArgs e)
		{
			var oldIndex = m_selectedIndex;
			var oldHvoRoot = m_hvoRoot;
			base.OnHandleCreated(e);
			if (oldIndex >= 0 && oldIndex != m_selectedIndex && m_hvoRoot > 0 && oldHvoRoot == m_hvoRoot)
			{
				var newCount = SpecialCache.get_VecSize(m_hvoRoot, MainTag);
				if (oldIndex < newCount)
				{
					SelectedIndex = oldIndex;
				}
			}
		}
	}
}