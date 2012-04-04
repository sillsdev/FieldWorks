// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BrowseView.cs
// Responsibility: WordWorks
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class XmlBrowseView : XmlBrowseViewBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:XmlBrowseView"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlBrowseView()
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the VC. It has some important functions related to interpreting fragment IDs
		/// that the filter bar needs.
		/// </summary>
		/// <value>The vc.</value>
		/// ------------------------------------------------------------------------------------
		public override XmlBrowseViewBaseVc Vc
		{
			get
			{
				CheckDisposed();

				if (m_xbvvc == null)
				{
					m_xbvvc = new XmlBrowseViewVc(m_nodeSpec, m_fakeFlid, m_stringTable, this);
				}
				return base.Vc;
			}
		}

		/// <summary>
		/// This is invoked by the PropertyTable (because XmlBrowseView is a mediator).
		/// </summary>
		/// <param name="propName"></param>
		public override void OnPropertyChanged(string propName)
		{
			CheckDisposed();

			if (propName == GetCorrespondingPropertyName("readOnlyBrowse"))
				SetSelectedRowHighlighting();

			base.OnPropertyChanged(propName);
		}

		private ITsString StripTrailingNewLine(ITsString tss)
		{
			string val = tss.Text;
			if (val == null)
				val = "";
			int cchStrip = 0;
			int cch = val.Length;
			if (cch == 0)
				return tss;
			char ch = val[cch - 1];
			if (ch == '\n' || ch == '\r')
			{
				cchStrip ++;
				if (cch > 1)
				{
					ch = val[cch - 2];
					if (ch == '\n' || ch == '\r')
					{
						cchStrip ++;
					}
				}
			}
			if (cchStrip == 0)
				return tss;
			ITsStrBldr tsb = tss.GetBldr();
			tsb.ReplaceTsString(cch - cchStrip, cch, null);
			return tsb.GetString();
		}

		/// <summary>
		/// If we are in ReadOnlySelect mode, this handles the up and down arrow keys to change
		/// the selection.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (ReadOnlySelect && !e.Handled && m_selectedIndex != -1)
			{
				int cobj = m_sda.get_VecSize(m_hvoRoot, m_fakeFlid);
				switch (e.KeyCode)
				{
					case Keys.Down:
						if (m_selectedIndex < cobj - 1)
							SelectedIndex = m_selectedIndex + 1;
						e.Handled = true;
						break;
					case Keys.Up:
						if (m_selectedIndex > 0)
							SelectedIndex = m_selectedIndex - 1;
						e.Handled = true;
						break;
				}
			}
		}

		/// <summary>
		/// If we are in ReadOnlySelect mode, intercept the click, and select the appropriate row
		/// without installing a normal selection.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseUp(MouseEventArgs e)
		{
			//If XmlBrouseView did not receive a mouse down event then we do not want to
			//do anything on the mouseUp because the mouseUp would have come from clicking
			//somewhere else. LT-8939
			if (!m_fMouseUpEnabled)
				return;

			try
			{
				if (m_selectedIndex == -1)
					return; // Can't do much in an empty list, so quit.
				m_fHandlingMouseUp = true;
#pragma warning disable 219
				int oldSelectedIndex = m_selectedIndex;
#pragma warning restore 219

				// Note all the stuff we might want to know about what was clicked that we will
				// use later. We want to get this now before anything changes, because there can
				// be scrolling effects from converting dummy objects to real.
				IVwSelection vwsel = MakeSelectionAt(e);
				int newSelectedIndex = GetRowIndexFromSelection(vwsel, true);
				// If we're changing records, we need to do some tricks to keep the selection in the place
				// clicked and the focus here. Save the information we will need.
				SelectionHelper clickSel = null;
				if (newSelectedIndex != SelectedIndex && vwsel != null && SelectionHelper.IsEditable(vwsel))
					clickSel = SelectionHelper.Create(vwsel, this);
				ITsString tssWord = null; // word clicked for click copy
				ITsString tssSource = null; // whole source string of clicked cell for click copy
				int hvoNewSelRow = 0; // hvo of new selected row (only for click copy)
				int ichStart = 0; // of tssWord in tssSource
				if (ClickCopy != null && e.Button == MouseButtons.Left && newSelectedIndex >= 0)
				{
					if (vwsel != null && vwsel.SelType == VwSelType.kstText)
					{
						int icol = vwsel.get_BoxIndex(false, 3);
						if (icol != Vc.OverrideAllowEditColumn + 1)
						{
							IVwSelection vwSelWord = vwsel.GrowToWord();
							// GrowToWord() can return a null -- see LT-9163 and LT-9349.
							if (vwSelWord != null)
							{
								vwSelWord.GetSelectionString(out tssWord, " ");
								tssWord = StripTrailingNewLine(tssWord);
								hvoNewSelRow = m_sda.get_VecItem(m_hvoRoot, m_fakeFlid,
																						newSelectedIndex);
								int hvoObj, tag, ws;
								bool fAssocPrev;
								vwSelWord.TextSelInfo(false, out tssSource, out ichStart, out fAssocPrev, out hvoObj,
													  out tag, out ws);
							}
						}
					}
				}

				// We need to manually change the index for ReadOnly views.
				// SimpleRootSite delegates RightMouseClickEvent to our RecordBrowseView parent,
				// which also makes the selection for us..
				if (ReadOnlySelect && e.Button != MouseButtons.Right)
				{
					if (this.m_xbvvc.HasSelectColumn && e.X < m_xbvvc.SelectColumnWidth)
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
				if (clickSel != null)
				{
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
						Focus();
						Application.Idle += FocusMe;
					}
				}
			}
			finally
			{
				m_fHandlingMouseUp = false;
				m_fMouseUpEnabled = false;
			}
		}

		void FocusMe(object sender, EventArgs e)
		{
			Application.Idle -= FocusMe;
			if (!IsDisposed)
				Focus();
			// Typically we get one idle event before the one in which the DataTree tries to focus its first
			// possible slice. We need to do it one more time for it to stick.
			Application.Idle += FocusMeAgain;
		}


		void FocusMeAgain(object sender, EventArgs e)
		{
			Application.Idle -= FocusMeAgain;
			if (!IsDisposed)
				Focus();
		}

		/// <summary>
		/// For testing we allow this to be simulated.
		/// </summary>
		/// <param name="e"></param>
		public void SimulateDoubleClick(EventArgs e)
		{
			OnDoubleClick(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process mouse double click
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDoubleClick(EventArgs e)
		{
			if (!ReadOnlySelect)
			{
				base.OnDoubleClick(e);
			}
			else if (SelectedIndex != -1)
			{
				FwObjectSelectionEventArgs e1 =
					new FwObjectSelectionEventArgs(SelectedObject, SelectedIndex);
				m_bv.OnDoubleClick(e1);
			}
		}
		bool m_fInSelectionChanged = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We override this method to make a selection in all of the views that are in a
		/// synced group. This fixes problems where the user changes the selection in one of
		/// the slaves, but the master is not updated. Thus the view is not scrolled as the
		/// groups scroll position only scrolls the master's selection into view. (TE-3380)
		/// </summary>
		/// <param name="rootb">The rootbox whose selection changed</param>
		/// <param name="vwselNew">The new selection</param>
		/// ------------------------------------------------------------------------------------
		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			// Guard against recursive calls, typically caused by the MakeTextSelection call below.
			if (m_fInSelectionChanged)
				return;
			try
			{
				m_fInSelectionChanged = true;

				// Make sure we're not handling MouseDown since it
				// handles SelectionChanged and DoSelectionSideEffects.
				// No need to do it again.
				if (!ReadOnlySelect && !m_fHandlingMouseUp)
				{
					if (vwselNew == null)
						return;
					// The selection can apparently be invalid on rare occasions, and will lead
					// to a crash below trying to call CLevels.  See LT-10301.  Treat it the
					// same as a null selection.
					if (!vwselNew.IsValid)
						return;

					base.HandleSelectionChange(rootb, vwselNew);

					m_wantScrollIntoView = false; // It should already be visible here.

					// Collect all the information we can about the selection.
					int ihvoRoot = 0;
					int tagTextProp = 0;
					int cpropPrevious = 0;
					int ichAnchor = 0;
					int ichEnd = 0;
					int ws = 0;
					bool fAssocPrev = false;
					int ihvoEnd = 0;
					ITsTextProps ttpBogus = null;
					SelLevInfo[] rgvsli = new SelLevInfo[0];
					int cvsli = vwselNew.CLevels(false) - 1;
					if (cvsli < 0)
						return;// Nothing useful we can do.

					// Main array of information retrived from sel that made combo.
					rgvsli = SelLevInfo.AllTextSelInfo(vwselNew, cvsli,
						out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
						out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);

					//for (int i = 0; i < cvsli; ++i)
					//{
					//	Debug.Write(String.Format("XmlBrowseView.SelectionChanged(): rgvsli[{0}].hvo={1}, ivho={2}, tag={3}, cpropPrevious={4}, ich={5}, ws={6}",
					//		i, rgvsli[i].hvo, rgvsli[i].ihvo, rgvsli[i].tag, rgvsli[i].cpropPrevious, rgvsli[i].ich, rgvsli[i].ws));
					//	Debug.WriteLine(String.Format("; ihvoRoot={0}, ihvoEnd={1}, ichEnd={2}",
					//		ihvoRoot, ihvoEnd, ichEnd));
					//}

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
						rootb.MakeTextSelection(ihvoRoot, cvsli, rgvsli, tagTextProp, cpropPrevious,
							ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, ttpBogus, true);
						DoSelectionSideEffects(rootb.Selection);
					}
					m_wantScrollIntoView = true;
				}
			}
			finally
			{
				m_fInSelectionChanged = false;
			}
		}
	}

	/// <summary>
	/// This class is the arguments for a ClickCopyEventHandler.
	/// </summary>
	public class ClickCopyEventArgs : EventArgs
	{
		ITsString m_tssWord;
		ITsString m_tssSource;
		int m_ichStartWord;
		int m_hvo;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ClickCopyEventArgs"/> class.
		/// </summary>
		/// <param name="tssWord">The TSS word.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tssSource">The TSS source.</param>
		/// <param name="ichStartWord">The ich start word.</param>
		/// ------------------------------------------------------------------------------------
		public ClickCopyEventArgs(ITsString tssWord, int hvo, ITsString tssSource, int ichStartWord)
		{
			m_tssWord = tssWord;
			m_hvo = hvo;
			m_tssSource = tssSource;
			m_ichStartWord = ichStartWord;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hvo.
		/// </summary>
		/// <value>The hvo.</value>
		/// ------------------------------------------------------------------------------------
		public int Hvo
		{
			get { return m_hvo; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the word.
		/// </summary>
		/// <value>The word.</value>
		/// ------------------------------------------------------------------------------------
		public ITsString Word
		{
			get { return m_tssWord; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ich start word.
		/// </summary>
		/// <value>The ich start word.</value>
		/// ------------------------------------------------------------------------------------
		public int IchStartWord
		{
			get { return m_ichStartWord; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the source.
		/// </summary>
		/// <value>The source.</value>
		/// ------------------------------------------------------------------------------------
		public ITsString Source
		{
			get { return m_tssSource; }
		}
	}

	/// <summary>
	/// This is used for a slice to ask the data tree to display a context menu.
	/// </summary>
	public delegate void ClickCopyEventHandler (object sender, ClickCopyEventArgs e);

}
