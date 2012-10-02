// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwDataEntryFieldEditor.cs
// Responsibility: TomB
// Last reviewed:
//
// <remarks>
// Defines the base for all data entry field editors.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.Common.Framework
{
	#region DataEntryTreeState enum
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Possible expansion states of a field editor.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum DataEntryTreeState
	{
		/// <summary> </summary>
		kdtsFixed,
		/// <summary> </summary>
		kdtsExpanded,
		/// <summary> </summary>
		kdtsCollapsed,
	};
	#endregion

	#region FwDataEntryFieldEditor class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This is a base class for all field editors. Data entry windows hold a vector of these
	/// editors -- each editor represents one field which may or may not be editable. Each
	/// editor has a label which may be shown in the tree view on the left, and is responsible
	/// for drawing its contents on the right.
	/// When a field editor becomes active (normally by the user clicking in the data on the
	/// right), BeginEdit() is called. The editor then creates a window that allows editing to
	/// take place.
	/// When the user moves to another field, EndEdit() is called, which closes the window,
	/// causing the editor to return to displaying the contents directly on the device context
	/// of AfDeSplitChild.
	/// If an active editor cannot be closed due to illegal data, it needs to implement
	/// IsOkToClose() so that it returns false.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class FwDataEntryFieldEditor
	{
		#region FwDataEntryFieldEditor Data members
		/// <summary>Holds the hwnd for the active editor. Only one field editor is active at
		/// once. This is unused for non-editable field editors.</summary>
		protected Form m_ActiveEditor; //was: HWND m_hwnd;
		/// <summary>Id of object we are editing (the object that has m_flid).</summary>
		protected int m_hvoObj;
		/// <summary>Id of the field we are editing.</summary>
		protected int m_flid;
		/// <summary>Level of nesting in the tree. 0 is top.</summary>
		protected int m_nIndent;
		/// <summary>Pixel height of font</summary>
		protected int m_dypFontHeight;
		/// <summary>Computed pixel height of the field at the current width. This should allow
		/// for one pixel above and below the text, plus one pixel for the bottom field divider
		/// line.</summary>
		protected int m_dypHeight;
		/// <summary>Pixel width when last height was calculated.</summary>
		protected int m_dxpWidth;
		/// <summary>The label to show in the tree for this field.</summary>
		protected ITsString m_tssLabel;
		/// <summary>The "What's this" help string for this field.</summary>
		protected ITsString m_tssHelp;
		/// <summary>The owning FwDataEntryForm.</summary>
		protected FwDataEntryForm m_Parent; // was: m_qadw
		/// <summary>Primary writing system for field contents.</summary>
		protected int m_ws;
		/// <summary>Magic writing system for field contents.</summary>
		protected int m_wsMagic;
		/// <summary>Primary old writing system for field contents.</summary>
		/// <summary>Primary character rendering properties, including font name, fore/back
		/// colors, etc.</summary>
		protected LgCharRenderProps m_CharacterRenderingProps; // was: m_chrp
		/// <summary>Brush for painting field background</summary>
		protected Brush m_BackgroundBrush; // was: m_hbrBkg
		/// <summary>The font to use for displays.</summary>
		protected Font m_Font; // was: m_hfont
		/// <summary>The mark handle from the action handler when a temp edit is in progress.
		/// </summary>
		protected int m_hMark;
		/// <summary>The FldSpec that defines this field.</summary>
		protected UserViewField m_FieldSpec; // was: FldSpec m_qfsp
		#endregion

		#region Construction and initialization

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hvoObj">Id of object we are editing (object that has m_flid).</param>
		/// <param name="flid">Id of the field we are editing.</param>
		/// <param name="nIndent">Level of nesting in the tree. 0 is top.</param>
		/// <param name="tssLabel">The label to show in the tree for this field.</param>
		/// <param name="tssHelp">The "What's this" help string for with this field.</param>
		/// <param name="parent">The owning form.</param>
		/// <param name="fieldSpec">The field specification that defines this field
		/// (null is acceptable, but not recommended).</param>
		/// ------------------------------------------------------------------------------------
		public FwDataEntryFieldEditor(int hvoObj, int flid, int nIndent, ITsString tssLabel,
			ITsString tssHelp, FwDataEntryForm parent, UserViewField fieldSpec)
		{
			m_dypFontHeight = 0;
			Debug.Assert(parent != null);
			Debug.Assert(hvoObj != 0);
			Debug.Assert(flid != 0);

			// Store member variables.
			m_hvoObj = hvoObj;
			m_flid = flid;
			m_nIndent = nIndent;
			m_tssLabel = tssLabel;
			m_tssHelp = tssHelp;
			m_Parent = parent;
			// Make a dummy FldSpec if one wasn't supplied.
			if (fieldSpec != null)
				m_FieldSpec = fieldSpec;
			else
				m_FieldSpec = new UserViewField();

			// Get writing system.
			int wsReal = m_FieldSpec.WritingSystemRAHvo;
			int wsMagic = m_FieldSpec.WsSelector;
			if (wsMagic != 0)
				m_wsMagic = wsMagic;
			else if (wsReal != 0)
				m_wsMagic = wsReal;
			else
				m_wsMagic = LangProject.kwsAnal;
			m_ws = parent.LangProj.ActualWs(m_wsMagic, m_hvoObj, m_flid);

			MakeCharProps();
		}
		#endregion

		#region properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The indent level of the editor (0 = none).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Indent
		{
			get
			{
				return m_nIndent;
			}
			set
			{
				Debug.Assert(value < 100); // Upper limit is arbitrary;
				m_nIndent = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Id for the object that has the property for this field.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Object
		{
			get
			{
				return m_hvoObj;
			}
			set
			{
				Debug.Assert(value != 0);
				m_hvoObj = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The field id that holds the contents being displayed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Flid
		{
			get
			{
				return m_flid;
			}
			set
			{
				Debug.Assert(value != 0);
				m_flid = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Height of field in pixels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Height
		{
			get
			{
				return m_dypHeight;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Width of field in pixels
		/// </summary>
		/// <remarks>
		/// When this value is set, the height is recalculated
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public int Width
		{
			get
			{
				return m_dxpWidth;
			}
			set
			{
				Debug.Assert(value > 0);
				SetWidthAndRecalcHeight(value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the display font based on character property settings.
		/// </summary>
		/// <remarks>
		/// The original FW implementation of this also set the editor height as a side-effect.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public Font DataEntryFont
		{
			get
			{
				if (m_Font == null)
				{
					FontStyle fontStyle = FontStyle.Regular;
					if (m_CharacterRenderingProps.ttvItalic ==
						(int)FwTextToggleVal.kttvForceOn)
						fontStyle |= FontStyle.Italic;
					if (m_CharacterRenderingProps.ttvBold ==
						(int)FwTextToggleVal.kttvForceOn)
						fontStyle |= FontStyle.Bold;
					// REVIEW TomB: See if this call to ToString really works. It's doubtful.
					m_Font = new Font((string)m_CharacterRenderingProps.szFaceName.ToString(),
						m_CharacterRenderingProps.dympHeight / 1000, fontStyle);
				}
				return m_Font;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The height of the font + padding (in pixels).
		/// </summary>
		/// <remarks>
		/// For one-line fields, this is like Height except Height includes the thickness of the
		/// separator line as well.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public int PaddedFontHeight
		{
			get
			{
				if (m_dypFontHeight == 0)
				{
					Graphics g = m_Parent.CreateGraphics();
					m_dypFontHeight = (int)DataEntryFont.GetHeight(g.DpiY);
					g.Dispose();
				}
				return m_dypFontHeight + m_Parent.FontPaddingAbove + m_Parent.FontPaddingBelow;
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tree label text
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString TreeLabel
		{
			get
			{
				return m_tssLabel;
			}
			set
			{
				m_tssLabel = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Help text
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString HelpText
		{
			get
			{
				return m_tssHelp;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Data Entry Form window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwDataEntryForm DataEntryForm
		{
			get
			{
				return m_Parent;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The UserViewField defining this field editor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UserViewField FieldSpec
		{
			get
			{
				return m_FieldSpec;
			}
		}
		#endregion

		#region Overridable Properties to be implemented on subclasses that can expand
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expansion state of tree nodes
		/// </summary>
		/// <remarks>
		/// This must be overridden for editors that can expand.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual DataEntryTreeState ExpansionState
		{
			get
			{
				return DataEntryTreeState.kdtsFixed; // Default is not expandable.
			}
		}
		#endregion

		#region Overridable Properties to be implemented on subclasses that use embedded objects
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Object id of outer object that has the property holding the intermediate object
		/// (when a node contains embedded objects)
		/// </summary>
		/// <remarks>
		/// In some simple cases of ownership, we don't want to use a tree node to display
		/// the full ownership hierarchy. In these cases, what appears as a single field to
		/// the user is actually a field in an owned object. One example is RnRoledPartic.
		/// RnEvent
		///    kflidRnEvent_Participants owning atomic RnRoledPartic
		///       kflidRnRoledPartic_Participants ref collection CmPossibility
		/// In this case, we show a single field editor with the inner contents being
		/// CmPossibility and the outer contents being RnRoledPartic. In this case:
		///   GetObj() returns the id for RnRoledPartic
		///   GetFlid() returns kflidRnRoledPartic_Participants
		///   GetOwner() returns the id for RnEvent
		///   GetOwnerFlid() returns kflidRnEvent_Participants.
		/// In most fields, there are no embedded objects, so both properties return the same
		/// results. If a field uses embedded objects, it needs to override this method.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual int HvoOwner
		{
			get
			{
				return m_hvoObj;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Field id on GetOwner() that holds the intermediate object (when a node contains
		/// embedded objects)
		/// </summary>
		/// <remarks>
		/// HvoOwner and OwnerFlid provide access to the intermediate object. See
		/// <see cref="HvoOwner"/> for details. If a field uses embedded objects, it needs to
		/// override this method.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual int OwnerFlid
		{
			get
			{
				return m_flid;
			}
		}
		#endregion

		#region Overridable Properties to be implemented on subclasses that can have selected text
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True if there is selected text
		/// </summary>
		/// <remarks>
		/// Override this property in any subclass that can have selected text.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsTextSelected
		{
			get
			{
				return false;
			}
		}
		#endregion

		#region Overridable Properties to be implemented on subclasses that support editing


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True if this field can be made editable
		/// </summary>
		/// <remarks>
		/// Override this property in any subclass that can be edited.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsEditable
		{
			get
			{
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True if this field is dirty (i.e., editable and changed)
		/// </summary>
		/// <remarks>
		/// Override this property in any subclass that can be edited.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsDirty
		{
			get
			{
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicate whether the data in the field meets the requirements of the UserViewField.
		/// Possible values are:
		/// <list type="bullet">
		/// <item>kFTReqNotReq if the all requirements are met</item>
		/// <item>kFTReqWs if data is missing, but it is encouraged</item>
		/// <item>kFTReqReq if data is missing, but it is required</item>
		/// </list>
		/// </summary>
		/// <remarks>
		/// Override this property if the subclass can display fields that require or encourage
		/// data to be present.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual FldReq HasRequiredData
		{
			get
			{
				return FldReq.kFTReqNotReq;
			}
		}
		#endregion

		#region Protected Overridable Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Stylesheet used for displaying the contents of the fields in this field editor.
		/// </summary>
		/// <remarks>
		/// Override this property for field editors that do no use the default stylesheet of
		/// the language project (e.g., when the Scripture stylesheet is required)
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual IVwStylesheet DeStyleSheet
		{
			get
			{
				LangProject lp = m_Parent.LangProj;
				if (lp != null)
				{
					FwStyleSheet stylesheet = new FwStyleSheet();
					stylesheet.Init(lp.Cache, lp.Hvo,
						(int)LangProject.LangProjectTags.kflidStyles);
					return stylesheet;
				}
				else
				{
					return null;
				}
			}
		}
		#endregion

		#region Public Overridable Methods with Default Implementations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a stylesheet change. Embedded root boxes have already been notified.
		/// </summary>
		///
		/// <remarks>
		/// If m_Font exists it is updated, along with the field's height. Subclasses which
		/// user m_hfont but do not use the field height it calculates should override.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void OnStylesheetChange()
		{
			MakeCharProps();
			if (m_Font != null) // If we have a font update it.
			{
				m_dypFontHeight = 0; // Force recalculation
				m_dypHeight = PaddedFontHeight + m_Parent.FieldEditorSeparatorWeight;
			}
		}
		#endregion

		#region Abstract methods (must be implemented on every subclass)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the width of the editor to the given value (in pixels) and recalculate the
		/// height at this new width.
		/// </summary>
		/// <param name="dxpWidth">The width in pixels of the data pane of the parent form
		/// </param>
		/// <remarks>
		/// In FW, this was originally SetHeightAt. This version, however, is not intended to
		/// be called directly. Instead, use the Width property to set the width and the
		/// Height property to get the newly calculated height.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected abstract void SetWidthAndRecalcHeight(int dxpWidth);

		// @param hdc The device context of the AfDeSplitChild.
		// @param rcClip

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw the field contents using the given Graphics object. This is used only when the
		/// editor is not active.
		/// </summary>
		/// <param name="graphics">The Graphics object</param>
		/// <param name="clipRectangle">The rectangle available for drawing (parent form client
		/// coordinates)</param>
		/// ------------------------------------------------------------------------------------
		public abstract void Draw(Graphics graphics, Rect clipRectangle);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the field contents. This is sent to fields when the underlying data changes.
		/// The editor needs to get the latest value from the cache and redraw its contents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public abstract void UpdateField();

		#endregion

		#region Public Overridable Methods to be implemented on subclasses that support editing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes any selected text
		/// </summary>
		/// <remarks>
		/// Override this method in any subclass that can have selected text.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void DeleteSelectedText()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an editing window and sets m_ActiveEditor to reference it. Places insertion
		/// point at the beginning of the top line.
		/// </summary>
		/// <remarks>
		/// This should always be called from overloaded methods.
		/// </remarks>
		/// <param name="rc">Rectangle for the window based on parent window coordinates</param>
		/// <returns>True if it succeeded</returns>
		/// ------------------------------------------------------------------------------------
		public bool BeginEdit(Rect rc)
		{
			return BeginEdit(rc, 0, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an editing window and sets m_ActiveEditor to reference it. Places insertion
		/// point on the top line.
		/// </summary>
		/// <remarks>
		/// This should always be called from overloaded methods.
		/// </remarks>
		/// <param name="rc">Rectangle for the window based on parent window coordinates</param>
		/// <param name="dxpCursor">Pixel offset from the left edge of the contents to the
		/// location where the insertion point should be placed</param>
		/// <returns>True if it succeeded</returns>
		/// ------------------------------------------------------------------------------------
		public bool BeginEdit(Rect rc, int dxpCursor)
		{
			return BeginEdit(rc, dxpCursor, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an editing window and sets m_ActiveEditor to reference it.
		/// </summary>
		/// <remarks>
		/// This should always be called from overloaded methods.
		/// </remarks>
		/// <param name="rc">Rectangle for the window based on parent window coordinates</param>
		/// <param name="dxpCursor">Pixel offset from the left edge of the contents to the
		/// location where the insertion point should be placed</param>
		/// <param name="fTopCursor">True if the insertion point should be placed on the top
		/// line; false if it is to be placed on the bottom line</param>
		/// <returns>True if it succeeded</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool BeginEdit(Rect rc, int dxpCursor, bool fTopCursor)
		{
			m_Parent.BeginEdit(this);
			SaveFullCursorInfo(); // Need this to update header and caption bar.
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called when the user clicks inside a field editor that is active but which
		/// has not filled its entire area with a child window (if it does fill its entire area,
		/// this can't happen).
		/// </summary>
		/// <param name="pt">Pint (in screen coordinates) where the mouse-click occurred</param>
		/// <returns>true if the click was handled; false for default click handling</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool ActiveClick(Point pt)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves changes but keeps the editing window open. If it is possible to have bad data,
		/// the caller should check value of IsOkToClose first.
		/// </summary>
		/// <returns>True if successful</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool SaveEdit()
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves changes and destroys the editor window. If it is possible to have bad data,
		/// the caller should check value of IsOkToClose first.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void EndEdit()
		{
			EndEdit(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the editor window. If it is possible to have bad data, the caller should
		/// check value of IsOkToClose first (unless forcing without save).
		/// </summary>
		/// <param name="fForce">Force the editor to close without making any validity checks or
		/// saving any changes. This is necessary in certain situations such as synchronization
		/// where temporary changes in an editor may conflict with the database and crash if
		/// allowed to save. One example is using type-ahead to add an existing item in the
		/// notebook researcher field, then before moving out of the field, use a list editor to
		/// merge this item with another item. When the sync process tries to update the
		/// notebook, it will fail when trying to save the researcher field because it is trying
		/// to send a replace to the database where the old value is no longer in the database.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public virtual void EndEdit(bool fForce)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move the window to the new location bounded by rcClip. It is assumed that the
		/// caller has already set the Width so that it knows how much room is needed.
		/// This is used to update an active editor, while Draw is used for inactive editors.
		/// </summary>
		/// <param name="rcClip">The new location rect in FwDataEntryForm coordinates</param>
		/// ------------------------------------------------------------------------------------
		public virtual void MoveWnd(Rect rcClip)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validate the data. If invalid, the method should indicate to the user the problem
		/// that needs to be corrected.
		/// </summary>
		/// <remarks>
		/// This only needs to be overridden if data for this field can be in an invalid state
		/// (e.g., a date string that doesn't parse into a date).
		/// </remarks>
		/// <returns>True if edited data is valid</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsOkToClose()
		{
			return IsOkToClose(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validate the data.
		/// </summary>
		/// <remarks>
		/// This only needs to be overridden if data for this field can be in an invalid state
		/// (e.g., a date string that doesn't parse into a date).
		/// </remarks>
		/// <param name="fWarn">Specfies whether or not this method should indicate to the user
		/// the problem that needs to be corrected if edited data is invalid</param>
		/// <returns>True if edited data is valid</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsOkToClose(bool fWarn)
		{
			return true;
		}

		// REVIEW: TomB: Determine whether this summary is accurate.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the current cursor information in FwMainWnd
		/// </summary>
		/// <remarks>
		/// Most overrides just need to store the cursor index in FwMainWnd.m_ichCur. Structured
		/// texts, however, also insert the appropriate hvos and flids for the StText classes in
		/// m_vhvoPath and m_vflidPath. Other editors may need to do other things.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void SaveCursorInfo()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This attempts to restore the insertion point in the editor to the location described
		/// by following the path of hvos and flids and using the indicated character position.
		/// </summary>
		/// <remarks>
		/// In FW, this was originally called RestoreCursor
		/// </remarks>
		/// <param name="vhvo">Vector of HVOs of objects in the chain that lead down to the
		/// property that should contain the insertion point</param>
		/// <param name="vflid">Vector of flids of objects in the chain that lead down to the
		/// property that should contain the insertion point</param>
		/// <param name="ichCur">Character offset where insertion point should be placed</param>
		/// ------------------------------------------------------------------------------------
		public virtual void RestoreInsertionPoint(int[] vhvo, int[] vflid, int ichCur)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set things up for editing with a temporary data cache by marking the point to return
		/// to in the undo stack. This can be called multiple times. If the mark is already set,
		/// it does nothing.
		/// </summary>
		/// <remarks>
		/// Should be overwritten for editors that use temporary caches.
		/// </remarks>
		/// <returns>The action handler which should be installed in the temporary cache
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual IActionHandler BeginTempEdit()
		{
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// End editing with a temporary data cache by clearing stuff out down to the mark
		/// created by BeginTempEdit. This can be called any number of times. If a mark is not
		/// in progress, it does nothing.
		/// </summary>
		/// <remarks>
		/// Should be overwritten for editors that use temporary caches.
		/// </remarks>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual IActionHandler EndTempEdit()
		{
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up undo/redo labels for an undo task when editing is about to begin in the data
		/// entry window. Then calls BeginUndoTask on the CustViewDa to create the undo task.
		/// </summary>
		/// <remarks>
		/// Previously called BeginChangesToLabel
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void BeginUndoTask()
		{
			// Make up names for the overall Undo/Redo, something like "Undo changes to <label>"
			string sLabel = m_tssLabel.Text;
			string sUndoFmt;
			string sRedoFmt;
			ResourceHelper.MakeUndoRedoLabels("kstidUndoChangesTo", out sUndoFmt, out sRedoFmt);
			m_Parent.LangProj.Cache.MainCacheAccessor.BeginUndoTask(
				string.Format(sUndoFmt, sLabel), string.Format(sRedoFmt, sLabel));
		}
		#endregion

		#region Other public methods
		// TODO TomB: Come up with better summary that distinguishes this from SaveCursorInfo
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves all information to restore the cursor to a given location in a field. It
		/// doesn't support selection ranges.
		/// </summary>
		/// <remarks>
		/// This information is stored in FwMainWnd.m_vhvoPath, m_vflidPath, and m_ichCur.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void SaveFullCursorInfo()
		{
			// TODO TomB: Implement
			// Store the current record/subrecord and field info.
//			LpMainWnd * plmw = dynamic_cast<LpMainWnd *>(m_qadw->MainWindow());
//			if (!plmw)
//				return;
//			HVO hvoRoot = plmw->GetRootObj();
//			Vector<int> & vflid = plmw->GetFlidPath();
//			Vector<HVO> & vhvo = plmw->GetHvoPath();
//			vhvo.Clear();
//			vflid.Clear();
//			// Save cursor specifics within a field.
//			SaveCursorInfo();
//			// Now save information leading up to the field.
//			CustViewDaPtr qcvd;
//			plmw->GetLpInfo()->GetDataAccess(&qcvd);
//			AssertPtr(qcvd);
//			HVO hvo = GetOwner();
//			int flid = GetOwnerFlid();
//			while (hvo && hvo != hvoRoot)
//			{
//				vhvo.Insert(0, hvo);
//				vflid.Insert(0, flid);
//				HVO hvoT;
//				CheckHr(qcvd->get_ObjOwner(hvo, &hvoT));
//				CheckHr(qcvd->get_ObjOwnFlid(hvo, &flid));
//				hvo = hvoT;
//			}
//			if (!hvo)
//			{
//				// This is to cover flukes in M3ModelEditor.
//				vhvo.Clear();
//				vflid.Clear();
//			}
//			// Also update the status bar and caption bar.
//			plmw->UpdateStatusBar();
//			plmw->UpdateCaptionBar();
		}
		#endregion

		#region Other protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up a Chrp consistent with the current style sheet
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeCharProps()
		{
			LangProject lp = m_Parent.LangProj;
			// Make a text property with the named style.
			ITsPropsBldr tsPropsBuilder = TsPropsBldrClass.Create();
			tsPropsBuilder.SetStrPropValue((int)VwStyleProperty.kspNamedStyle,
				m_FieldSpec.Style);
			tsPropsBuilder.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, m_ws);
			ITsTextProps tsTextProps = tsPropsBuilder.GetTextProps();

			// Fill out the LgCharRenderProps.
			ILgWritingSystemFactory lgEncFactory;
			IVwPropertyStore vwPropertyStore =
				VwPropertyStoreClass.Create();
			IVwStylesheet vwStylesheet = DeStyleSheet;
			if (vwStylesheet != null)
			{
				vwPropertyStore.Stylesheet = vwStylesheet;
			}
			if (lp != null)
			{
				lgEncFactory = lp.Cache.LanguageWritingSystemFactoryAccessor;
			}
			else
			{
				// Get default registry-based factory.
				lgEncFactory = LgWritingSystemFactoryClass.Create();
			}
			Debug.Assert(lgEncFactory != null);
			vwPropertyStore.WritingSystemFactory = lgEncFactory;
			m_CharacterRenderingProps = vwPropertyStore.get_ChrpFor(tsTextProps);
			IWritingSystem writingSystem = lgEncFactory.get_EngineOrNull(m_CharacterRenderingProps.ws);
			Debug.Assert(writingSystem != null);
			writingSystem.InterpretChrp(ref m_CharacterRenderingProps);

			// For our purposes here, we don't want transparent backgrounds.
			if ((int)m_CharacterRenderingProps.clrBack == (int)FwTextColor.kclrTransparent)
				m_CharacterRenderingProps.clrBack = (uint)SystemColors.Window.ToArgb();

			// Make a brush for the background.
			m_BackgroundBrush = new SolidBrush(Color.FromArgb((int)m_CharacterRenderingProps.clrBack));
		}
		#endregion
	}
	#endregion
}
