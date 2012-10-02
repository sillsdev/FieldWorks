// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PubEditingHelper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using System.Diagnostics;
using System.Drawing;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.PrintLayout
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An EditingHelper that is used in PublicationControl. It can hold a derived EditingHelper
	/// and delegates calls to it.
	/// </summary>
	/// <remarks>We implemented this class because we need some special behavior for
	/// print layouts (e.g. Ctrl-Home/End goes to the beginning/end of the first/last main
	/// division, not just the current rootbox. On the other hand, this might not be the end of
	/// the page), but we still might need application specific functionality (e.g. provided in
	/// TeEditingHelper).</remarks>
	/// ----------------------------------------------------------------------------------------
	public class PubEditingHelper: EditingHelper
	{
		#region Data members
		private EditingHelper m_decoratedEditingHelper;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PubEditingHelper"/> class.
		/// </summary>
		/// <param name="decoratedEditingHelper">The editing helper we decorate.</param>
		/// ------------------------------------------------------------------------------------
		public PubEditingHelper(EditingHelper decoratedEditingHelper):
			this(decoratedEditingHelper, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PubEditingHelper"/> class.
		/// </summary>
		/// <param name="decoratedEditingHelper">The decorated editing helper.</param>
		/// <param name="callbacks">The callbacks.</param>
		/// ------------------------------------------------------------------------------------
		public PubEditingHelper(EditingHelper decoratedEditingHelper, IEditingCallbacks callbacks)
			: base(callbacks)
		{
			m_decoratedEditingHelper = decoratedEditingHelper;

			Debug.Assert(callbacks is PublicationControl);
		}

		#endregion

		#region Dispose
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_decoratedEditingHelper != null)
					m_decoratedEditingHelper.Dispose();
			}

			m_decoratedEditingHelper = null;

			base.Dispose(disposing);
		}
		#endregion

		#region Overriden public methods that delegate to the decorated EditingHelper
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the caption props.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override ITsTextProps CaptionProps
		{
			get
			{
				if (m_decoratedEditingHelper != null)
					return m_decoratedEditingHelper.CaptionProps;
				return base.CaptionProps;
			}
		}

		/// <summary>
		/// Pass it on...
		/// </summary>
		public override void OnAboutToEdit()
		{
			if (m_decoratedEditingHelper != null)
				m_decoratedEditingHelper.OnAboutToEdit();
			else
				base.OnAboutToEdit();
		}

		/// <summary>
		/// Pass it on...
		/// </summary>
		public override void OnFinishedEdit()
		{
			if (m_decoratedEditingHelper != null)
				m_decoratedEditingHelper.OnFinishedEdit();
			else
				base.OnFinishedEdit();
		}

		/// <summary>
		/// Pass it on...
		/// </summary>
		public override bool KeepCollectingInput(int nextChar)
		{
			if (m_decoratedEditingHelper != null)
				return m_decoratedEditingHelper.KeepCollectingInput(nextChar);
			return base.KeepCollectingInput(nextChar);
		}

		/// <summary>
		/// Pass it on...
		/// </summary>
		public override bool MonitorTextEdits
		{
			get
			{
				if (m_decoratedEditingHelper != null)
					return m_decoratedEditingHelper.MonitorTextEdits;
				return base.MonitorTextEdits;
			}
		}

		/// <summary>
		/// Delegate to the main editing helper...otherwise anything that casts as (e.g.)
		/// TeEditingHelper can modify the defaultCursor of the result without changing
		/// ours, and vice versa.
		/// </summary>
		public override Cursor DefaultCursor
		{
			get
			{
				return m_decoratedEditingHelper.DefaultCursor;
			}
			set
			{
				m_decoratedEditingHelper.DefaultCursor = value;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Apply the paragraph style, that is modify the TsTextProps object that is the style
		/// of each of the selected (or partly selected) paragraphs and clear all explicit
		/// paragraph formatting.
		/// </summary>
		/// <param name="strNewVal">the name of a new style</param>
		/// <returns>
		/// Returns <c>false</c> if the paragraph properties can't be retrieved,
		/// otherwise <c>true</c>.
		/// </returns>
		/// <remarks>
		/// 	<p>Formerly <c>AfVwRootSite::FormatParas</c>.</p>
		/// 	<p>The functionality for the other cases when FormatParas was called still needs
		/// to be ported when we need it. They should be put in separate methods.</p>
		/// 	<p>ApplyParagraphStyle begins by getting the paragraph properties from the
		/// selection. If paragraph properties cannot be retrieved through a selection,
		/// ApplyParagraphStyle returns false. If no text properties are retrieved in vttp,
		/// ApplyParagraphStyle returns true since there is nothing to do.</p>
		/// 	<p>Next, immediate changes are made to paragraph properties retrieved from the
		/// selection, and the variable vttp is updated.</p>
		/// 	<p>If ApplyParagraphStyle has not returned as described above, it narrows the range
		/// of TsTextProps to those that are not <c>null</c>. Then, it saves the view selection
		/// level information by calling AllTextSelInfo on the selection. To "fake" a property
		/// change, PropChanged is called on the SilDataAccess pointer. Finally, the selection
		/// is restored by a call to MakeTextSelection on the RootBox pointer.</p>
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override bool ApplyParagraphStyle(string strNewVal)
		{
			if (m_decoratedEditingHelper != null)
				return m_decoratedEditingHelper.ApplyParagraphStyle(strNewVal);
			return base.ApplyParagraphStyle(strNewVal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Apply the selected style with only the specified style name.
		/// </summary>
		/// <param name="sStyleToApply">Style name (this could be a paragraph or character
		/// style).</param>
		/// ------------------------------------------------------------------------------------
		public override void ApplyStyle(string sStyleToApply)
		{
			if (m_decoratedEditingHelper != null)
				m_decoratedEditingHelper.ApplyStyle(sStyleToApply);
			else
				base.ApplyStyle(sStyleToApply);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Apply the selected style
		/// </summary>
		/// <param name="sStyleToApply">Style name</param>
		/// <param name="vwsel">Selection</param>
		/// <param name="vttpPara">Paragraph properties</param>
		/// <param name="vttpChar">Character properties</param>
		/// ------------------------------------------------------------------------------------
		public override void ApplyStyle(string sStyleToApply, IVwSelection vwsel,
			ITsTextProps[] vttpPara, ITsTextProps[] vttpChar)
		{
			if (m_decoratedEditingHelper != null)
				m_decoratedEditingHelper.ApplyStyle(sStyleToApply, vwsel, vttpPara, vttpChar);
			else
				base.ApplyStyle(sStyleToApply, vwsel, vttpPara, vttpChar);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the copying of text into the clipboard is possible.
		/// </summary>
		/// <returns>
		/// Returns <c>true</c> if copying is possible.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool CanCopy()
		{
			if (m_decoratedEditingHelper != null)
				return m_decoratedEditingHelper.CanCopy();
			return base.CanCopy();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the cutting of text into the clipboard is possible.
		/// </summary>
		/// <returns>
		/// Returns <c>true</c> if cutting is possible.
		/// </returns>
		/// <remarks>Formerly <c>AfVwRootSite::CanCut()</c>.</remarks>
		/// ------------------------------------------------------------------------------------
		public override bool CanCut()
		{
			if (m_decoratedEditingHelper != null)
				return m_decoratedEditingHelper.CanCut();
			return base.CanCut();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the deleting of text is possible.
		/// </summary>
		/// <returns>
		/// Returns <c>true</c> if cutting is possible.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool CanDelete()
		{
			if (m_decoratedEditingHelper != null)
				return m_decoratedEditingHelper.CanDelete();
			return base.CanDelete();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <returns>true if we are in an editable location</returns>
		/// ------------------------------------------------------------------------------------
		public override bool CanEdit()
		{
			if (m_decoratedEditingHelper != null)
				return m_decoratedEditingHelper.CanEdit();
			return base.CanEdit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="vwsel"></param>
		/// <returns>
		/// true if editing would be possible for the specified selection
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool CanEdit(IVwSelection vwsel)
		{
			if (m_decoratedEditingHelper != null)
				return m_decoratedEditingHelper.CanEdit(vwsel);
			return base.CanEdit(vwsel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if pasting of text from the clipboard is possible.
		/// </summary>
		/// <returns>
		/// Returns <c>true</c> if pasting is possible.
		/// </returns>
		/// <remarks>Formerly <c>AfVwRootSite::CanPaste()</c>.</remarks>
		/// ------------------------------------------------------------------------------------
		public override bool CanPaste()
		{
			if (m_decoratedEditingHelper != null)
				return m_decoratedEditingHelper.CanPaste();
			return base.CanPaste();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a SelectionHelper object set to the current selection in the view
		/// (updated any time the selection changes)
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override SelectionHelper CurrentSelection
		{
			get
			{
				if (m_decoratedEditingHelper != null)
					return m_decoratedEditingHelper.CurrentSelection;
				return base.CurrentSelection;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the current selection contains one run with a character style or multiple runs
		/// with the same character style this method returns the character style; otherwise
		/// returns the paragraph style unless multiple paragraphs are selected that have
		/// different paragraph styles.
		/// </summary>
		/// <param name="styleName">Gets the styleName</param>
		/// <returns>
		/// The styleType or -1 if no style type can be found or multiple style types
		/// are in the selection.  Otherwise returns the styletype
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetStyleNameFromSelection(out string styleName)
		{
			if (m_decoratedEditingHelper != null)
				return m_decoratedEditingHelper.GetStyleNameFromSelection(out styleName);
			return base.GetStyleNameFromSelection(out styleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a key press.
		/// </summary>
		/// <param name="keyChar">The pressed character key</param>
		/// <param name="fCalledFromKeyDown">true if this method gets called from OnKeyDown
		/// (to handle Delete)</param>
		/// <param name="modifiers">key modifies - shift status, etc.</param>
		/// <param name="graphics">graphics object for process input</param>
		/// ------------------------------------------------------------------------------------
		public override void HandleKeyPress(char keyChar, bool fCalledFromKeyDown,
			Keys modifiers, IVwGraphics graphics)
		{
			if (m_decoratedEditingHelper != null)
				m_decoratedEditingHelper.HandleKeyPress(keyChar, fCalledFromKeyDown, modifiers, graphics);
			else
				base.HandleKeyPress(keyChar, fCalledFromKeyDown, modifiers, graphics);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hook method for doing application specific processing on a mouse down event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void HandleMouseDown()
		{
			if (m_decoratedEditingHelper != null)
				m_decoratedEditingHelper.HandleMouseDown();
			else
				base.HandleMouseDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new object, given a text representation (e.g., from the clipboard).
		/// </summary>
		/// <param name="cache">FDO cache representing the DB connection to use</param>
		/// <param name="sTextRep">Text representation of object</param>
		/// <param name="selDst">Provided for information in case it's needed to generate
		/// the new object (E.g., footnotes might need it to generate the proper sequence
		/// letter)</param>
		/// <param name="kodt">The object data type to use for embedding the new object</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override Guid MakeObjFromText(FdoCache cache, string sTextRep, IVwSelection selDst,
			out int kodt)
		{
			if (m_decoratedEditingHelper != null)
				return m_decoratedEditingHelper.MakeObjFromText(cache, sTextRep, selDst, out kodt);
			return base.MakeObjFromText(cache, sTextRep, selDst, out kodt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is used to get a notification when an owning object-replacement character
		/// (ORC) is deleted. ORCs are used to mark locations in the text of things like pictures
		/// or footnotes. In the case of footnotes, when an owning footnote ORC is deleted, we
		/// need to find the corresponding footnote and delete it.
		/// </summary>
		/// <param name="guid">The GUID of the footnote being deleted.</param>
		/// ------------------------------------------------------------------------------------
		public override void ObjDeleted(ref Guid guid)
		{
			if (m_decoratedEditingHelper != null)
				m_decoratedEditingHelper.ObjDeleted(ref guid);
			else
				base.ObjDeleted(ref guid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a WM_CHAR message.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="modifiers"></param>
		/// <param name="graphics"></param>
		/// ------------------------------------------------------------------------------------
		public override void OnKeyPress(KeyPressEventArgs e, Keys modifiers, IVwGraphics graphics)
		{
			if (m_decoratedEditingHelper != null)
				m_decoratedEditingHelper.OnKeyPress(e, modifiers, graphics);
			else
				base.OnKeyPress(e, modifiers, graphics);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paste data from the clipboard into the view.
		/// </summary>
		/// <param name="pasteOneParagraph">true to only paste the first paragraph
		/// of the selection</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override void PasteClipboard(bool pasteOneParagraph)
		{
			if (m_decoratedEditingHelper != null)
				m_decoratedEditingHelper.PasteClipboard(pasteOneParagraph);
			else
				base.PasteClipboard(pasteOneParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Notifies the site that something about the selection has changed.
		/// Change the system keyboard when the selection changes.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew">Selection</param>
		/// <remarks>When overriding you should call the base class first.</remarks>
		/// ------------------------------------------------------------------------------------
		public override void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			if (m_decoratedEditingHelper != null)
				m_decoratedEditingHelper.SelectionChanged(prootb, vwselNew);
			else
				base.SelectionChanged(prootb, vwselNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allow certain style names to be given special sematic meaning so they will be
		/// skipped over when removing character styles.
		/// </summary>
		/// <param name="name">style name to check</param>
		/// <returns>
		/// true to apply meaning to the style and skip it.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool SpecialSemanticsCharacterStyle(string name)
		{
			if (m_decoratedEditingHelper != null)
				return m_decoratedEditingHelper.SpecialSemanticsCharacterStyle(name);
			return base.SpecialSemanticsCharacterStyle(name);
		}
		#endregion

		#region Overriden methods that require special handling for Publications
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User pressed a key.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="graphics"></param>
		/// <returns>
		/// 	<c>true</c> if we handled the key, <c>false</c> otherwise (e.g. we're
		/// already at the end of the rootbox and the user pressed down arrow key).
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool OnKeyDown(KeyEventArgs e, IVwGraphics graphics)
		{
			if ((e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown) &&
				((e.Modifiers & Keys.Control) == Keys.Control))
			{
				GoToPageTopOrBottom(e, graphics);
			}
			else if ((e.KeyCode == Keys.Home || e.KeyCode == Keys.End) && (e.Modifiers & Keys.Control) != 0)
			{
				// Ctrl-Home needs to go to beginning of first division
				// Ctrl-End needs to go to end of last division
				PubControl.FocusedStream = PubControl.Divisions[
					(e.KeyCode == Keys.Home) ? 0 : PubControl.Divisions.Count - 1].MainLayoutStream;

				if (PubControl.FocusedRootBox.Selection == null)
					PubControl.FocusedRootBox.MakeSimpleSel(e.KeyCode == Keys.Home, true, false, true);

				using (new WaitCursor(PubControl.ParentForm))
				{
					IVwGraphics vg = PubControl.PrinterGraphics;
					try
					{
						while (PubControl.LayoutToPageIfNeeded((e.KeyCode == Keys.Home) ? 0 :
							PubControl.Pages.Count - 1, vg.XUnitsPerInch, vg.YUnitsPerInch))
						{
						}
					}
					finally
					{
						PubControl.ReleaseGraphics(vg);
					}
				}
				base.OnKeyDown(e, graphics);
			}
			else
			{
				if (!base.OnKeyDown(e, graphics))
				{
					// this means that we are already at the beginning or end of the
					// division. However, if we have more divisions we try there...
					int iOldDiv = PubControl.DivisionIndexForMainStream(PubControl.FocusedStream);
					if (iOldDiv < 0)
						return true;

					int iNewDiv;
					bool fUpwards = false;
					switch (e.KeyCode)
					{
						case Keys.PageUp:
						case Keys.Up:
						case Keys.Left:
							iNewDiv = iOldDiv - 1;
							fUpwards = true;
							break;
						case Keys.Down:
						case Keys.PageDown:
						case Keys.Right:
							iNewDiv = iOldDiv + 1;
							break;
						default:
							return false;
					}
					if (iNewDiv < 0 || iNewDiv >= PubControl.Divisions.Count)
						return true;

					PubControl.FocusedStream = PubControl.Divisions[iNewDiv].MainLayoutStream;
					// we better destroy the selection or we end up somewhere in the
					// middle of the rootbox if the selection was previously there.
					if (fUpwards)
						PubControl.FocusedRootBox.MakeSimpleSel(false, true, false, true);
					else
						PubControl.FocusedRootBox.MakeSimpleSel(true, true, false, true);

					if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Right ||
						e.KeyCode == Keys.Up || e.KeyCode == Keys.Left)
					{
						// for Down and Up keys we're still not at the right position,
						// but at least in the correct line.
						PubControl.ScrollSelectionIntoView(null, VwScrollSelOpts.kssoDefault);
					}
					else
					{
						if (!base.OnKeyDown(e, graphics))
						{
							PubControl.FocusedStream = PubControl.Divisions[iOldDiv].MainLayoutStream;
							return false;
						}
					}
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert this instance to type T.
		/// </summary>
		/// <typeparam name="T">Desired type to cast to.</typeparam>
		/// <returns>Editing helper cast as T.</returns>
		/// ------------------------------------------------------------------------------------
		public override T CastAs<T>()
		{
			if (this is T)
				return this as T;
			return DecoratedEditingHelper as T;
		}
		#endregion

		#region Overriden protected properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Special handling for Ctrl-Home/End and scrolling the selection into view
		/// </summary>
		/// <param name="e"></param>
		/// <param name="ss"></param>
		/// ------------------------------------------------------------------------------------
		protected override void HandleKeyDown(KeyEventArgs e, VwShiftStatus ss)
		{
			// We dealt with Ctrl-Home/End already in OnKeyDown, so we just make sure that the
			// selection is visible here.
			PubControl.ScrollSelectionIntoView(null, VwScrollSelOpts.kssoDefault);
		}
		#endregion

		#region Other properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the publication control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected PublicationControl PubControl
		{
			get { return Callbacks as PublicationControl; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the decorated editing helper.
		/// </summary>
		/// <value>The decorated editing helper.</value>
		/// ------------------------------------------------------------------------------------
		public EditingHelper DecoratedEditingHelper
		{
			get
			{
				CheckDisposed();
				if (m_decoratedEditingHelper != null)
					return m_decoratedEditingHelper;
				return this;
			}
		}
		#endregion

		#region Other methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Goes to the top (beginning of the main stream of the first division) of the current
		/// page.
		/// </summary>
		/// <param name="e">The <see cref="T:System.Windows.Forms.KeyEventArgs"/> instance
		/// containing the event data.</param>
		/// <param name="graphics">The graphics.</param>
		/// ------------------------------------------------------------------------------------
		protected void GoToPageTopOrBottom(KeyEventArgs e, IVwGraphics graphics)
		{
			bool fTop = (e.KeyCode == Keys.PageUp);

			// Find the page we're on
			int iDiv;
			bool fEndBeforeAnchor;
			int dyPageScreen;
			IVwSelection vwsel = PubControl.FocusedRootBox.Selection;
			if (vwsel == null || !vwsel.IsValid)
				return;

			Rectangle rcSel = PubControl.GetSelectionRectangle(vwsel, graphics, out fEndBeforeAnchor);
			// JohnT: I think the idea here is that if it's an IP we want it's center, otherwise, we want
			// the endpoint.
			int y = vwsel.IsRange ? (fEndBeforeAnchor ? rcSel.Top : rcSel.Bottom) :
				(rcSel.Bottom + rcSel.Top) / 2;
			int x = vwsel.IsRange ? (fEndBeforeAnchor ? rcSel.Left : rcSel.Right) :
				(rcSel.Left + rcSel.Right) / 2;
			Page page = PubControl.PageFromPrinterY(x, y, !fTop, PubControl.FocusedStream,
				out dyPageScreen, out iDiv);

			if (iDiv < 0 || iDiv >= PubControl.Divisions.Count || page == null)
				return;

			// Destroy the current selection
			PubControl.Divisions[iDiv].MainRootBox.DestroySelection();

			// Now we know the page, so we can put the IP on the first/last line of this page.
			IVwSelection sel = fTop ? page.TopOfPageSelection.Selection :
				page.BottomOfPageSelection.Selection;
			sel.Install();
			sel.RootBox.Activate(VwSelectionState.vssEnabled);
			PubControl.FocusedRootBox = sel.RootBox;
			PubControl.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
		}
		#endregion
	}
}
