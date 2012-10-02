/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFieldEditor.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	Defines the base for all data entry field editors.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDE_FIELD_EDITOR_INCLUDED
#define AFDE_FIELD_EDITOR_INCLUDED 1

#define kSubitemLevel 1

/*----------------------------------------------------------------------------------------------
	This is a base class for all field editors. Data entry windows hold a vector of these
	editors--each editor represents one field which may or may not be editable. Each editor
	has a label to show in the tree view on the left, and is responsible for drawing its
	contents on the right.

	When a field editor becomes active (normally by the user clicking in the data on the right),
	BeginEdit() is called. The editor then creates a window that allows editing to take place.
	When the user moves to another field, EndEdit() is called, which closes the window, causing
	the editor to return to displaying the contents directly on the device context of AfDeSplitChild.
	If an active editor cannot be closed due to illegal data, it needs to implement
	IsOkToClose() so that it returns false.

	@h3{Hungarian: dfe}
----------------------------------------------------------------------------------------------*/

class AfDeFieldEditor : public GenRefObj
{
public:
	AfDeFieldEditor();
	virtual ~AfDeFieldEditor();

	virtual void Initialize(int obj, int flid, int nIndent, ITsString * ptssLabel,
		ITsString * ptssHelp, AfDeSplitChild * padsc, FldSpec * pfsp);
	void CreateFont();

	// Return the indent level of the editor (0 = none).
	// @return Indent level (0 = none).
	int GetIndent()
	{
		return m_nIndent;
	}

	// Set the indent level to n and return the previous indent.
	// @param n Indent level (0 = none).
	// @return The previous indent level.
	int SetIndent(int n)
	{
		Assert((uint)n < 100); // Upper limit is arbitrary;
		int nT = m_nIndent;
		m_nIndent = n;
		return nT;
	}

	// Set the Id for the object that has the property for this field.
	// @param hvo Object id.
	void SetObj(HVO hvo)
	{
		m_hvoObj = hvo;
	}

	// Get the Id for the object that holds the contents being displayed.
	// There are rare cases, such as fake RnRoledPartic, where this may not
	// be a valid HVO in the database. See ${AfDeFieldEditor#GetOwner} for details.
	// @return Object id of intermediate object.
	HVO GetObj()
	{
		return m_hvoObj;
	}

	// Set the field id for this field.
	// @param flid Field id.
	void SetFlid(int flid)
	{
		m_flid = flid;
	}

	// Get the field id that holds the contents being displayed. GetObj() and GetFlid()
	// give the inner contents of the field. See ${AfDeFieldEditor#GetOwner} for details.
	// @return field id on GetObj() that holds the inner object.
	int GetFlid()
	{
		return m_flid;
	}

	// In some simple cases of ownership, we don't want to use a tree node to display
	// the full ownership hierarchy. In these cases, what appears as a single field to
	// the user is actually a field in an owned object. One example is RnRoledPartic.
	// RnEvent
	//    kflidRnEvent_Participants owning atomic RnRoledPartic
	//       kflidRnRoledPartic_Participants ref collection CmPossibility
	// In this case, we show a single field editor with the inner contents being
	// CmPossibility and the outer contents being RnRoledPartic. In this case:
	//   GetObj() returns the id for RnRoledPartic
	//   GetFlid() returns kflidRnRoledPartic_Participants
	//   GetOwner() returns the id for RnEvent
	//   GetOwnerFlid() returns kflidRnEvent_Participants.
	// In most fields, there are no embedded objects, so both properties return the same
	// results. If a field uses embedded objects, it needs to redefine this method.
	// Get the Id for the outer object that has the property holding the intermediate object.
	// @return Object id of outer object.
	virtual HVO GetOwner()
	{
		return m_hvoObj;
	}

	// Get the field id that holds the intermediate object. GetOwner() and GetOwnerFlid()
	// give the intermediate object. See ${AfDeFieldEditor#GetOwner} for details. If a
	// field uses embedded objects, it needs to redefine this method.
	// @return field id on GetOwner() that holds the intermediate object.
	virtual int GetOwnerFlid()
	{
		return m_flid;
	}

	// Set the field height to dyp pixels.
	// @b{Note:} This should only be used for testing purposes. Normally use SetHeightAt().
	// param dyp Height of field in pixels.
	void SetHeight(int dyp)
	{
		Assert(dyp > 0);
		m_dypHeight = dyp;
	}

	// Get the field height.
	// @return Height of field in pixels.
	int GetHeight()
	{
		return m_dypHeight;
	}

	// Set the field width to dxp pixels.
	// @b{Note:} This should only be used for testing purposes. Normally use SetHeightAt().
	// @param dxp Width of field in pixels.
	void SetWidth(int dxp)
	{
		Assert(dxp > 0);
		m_dxpWidth = dxp;
	}

	// Get the width of the field in pixels.
	// @return Width of the field in pixels.
	int GetWidth()
	{
		return m_dxpWidth;
	}

	// Get the height of the font + 2 (1 pixel above and below) in pixels.
	// @return height of font in pixels + 2 pixels for padding above and below. For
	// one line fields this is like GetHeight except GetHeight returns one more pixel
	// for the separator line.
	int GetFontHeight();

	// Set the tree label text
	// @param achar Pointer to the label string.
	void SetLabel(ITsString * ptssLabel)
	{
		AssertPtrN(ptssLabel);
		m_qtssLabel = ptssLabel;
	}

	// Get the label text.
	// @param pptss Pointer to receive the label TsString.
	void GetLabel(ITsString ** pptss)
	{
		AssertPtr(pptss);
		*pptss = m_qtssLabel;
		AddRefObj(*pptss);
	}

	// Get the help text.
	// @param pptss Pointer to receive the help TsString.
	void GetHelp(ITsString ** pptss)
	{
		AssertPtr(pptss);
		*pptss = m_qtssHelp;
		AddRefObj(*pptss);
	}

	// Get the style string.
	// @return Pointer to the characters of the style name.
	const wchar * GetStyle()
	{
		return m_qfsp->m_stuSty.Chars();
	}

	// Return the HWND of the window (NULL when not active).
	// @return HWND of the active editor window.
	HWND Hwnd()
	{
		return m_hwnd;
	}

	// Returns true if there is selected text.
	// Over ride this method in any subclass that can have selected text.
	virtual bool IsTextSelected()
	{
		return false;
	}

	// Over ride this method in any subclass that can have selected text.
	virtual void DeleteSelectedText()
	{
	}

	// Get the pointer to the data entry window.
	// @return Pointer to the data entry window.
	AfDeSplitChild * GetDeWnd()
	{
		return m_qadsc;
	}

	// Process the WM_CTLCOLOREDIT message. This is called when an edit box is about to
	// display. We need to set the foreground/background colors and return the background brush.
	// @param hdc The device context for the edit box.
	// @param lnRet Return for a brush handle to use to paint the background of the edit box.
	// @return True if processed (always true here).
	bool OnColorEdit(HDC hdc, long & lnRet)
	{
		AfGfx::SetBkColor(hdc, m_chrp.clrBack);
		AfGfx::SetTextColor(hdc, m_chrp.clrFore);
		lnRet = (BOOL)m_hbrBkg;
		return true;
	}

	// Return the expansion state of tree nodes.
	// This must be overwritten for editors that can expand.
	// @return A tree state enum.
	virtual DeTreeState GetExpansion()
	{
		return kdtsFixed; // Default is not expandable.
	}

	// Clear pointers to other windows so its destructor will be called.
	virtual void OnReleasePtr();

	// Field editors should override this if they want to change the mouse appearance, etc.
	// @param grfmk Indicates shift key and mouse button states.
	// @param xp The x-coord of the mouse relative to the upper-left corner of the client.
	// @param yp The y-coord of the mouse relative to the upper-left corner of the client.
	// @return True if processed.
	virtual bool OnMouseMove(uint grfmk, int xp, int yp)
	{
		return false;
	}

	virtual void OnStylesheetChange();

	// This implements the "Find In Dictionary" function, and is called by a parent window.
	virtual bool CmdFindInDictionary(Cmd * pcmd)
	{
		return false;
	}

	// This enables/disables the "Find In Dictionary" function, and is called by a parent
	// window.  It is disabled by default.
	virtual bool CmsFindInDictionary(CmdState & cms)
	{
		cms.Enable(false);
		return true;		// Indicates we have handled it.
	}

	//:>****************************************************************************************
	//:> The following methods must be implemented on every subclass of AfDeFieldEditor.
	//:>****************************************************************************************

	// Calculates the height for a given width and stores both in member variables.
	// @param dxpWidth The width in pixels of the data part of the AfDeSplitChild.
	// @return The height of the data in pixels.
	virtual int SetHeightAt(int dxpWidth) = 0;

	// Draw the field contents on the AfDeSplitChild DC. This is used when the editor is not active.
	// MoveWnd is used when the editor is active.
	// When active, the child window nanages the drawing.
	// @param hdc The device context of the AfDeSplitChild.
	// @param rcClip The rectangle available for drawing (AfDeSplitChild client coordinates)
	virtual void Draw(HDC hdc, const Rect & rcClip) = 0;

	// Update the field contents. This is sent to fields when the underlying data changes.
	// The editor needs to get the latest value from the cache and redraw its contents.
	virtual void UpdateField() = 0;

	//:>****************************************************************************************
	//:> The following methods must be implemented on subclasses that support editing.
	//:>****************************************************************************************

	// Can this field be made editable? Overwrite this on subclasses that can be edited.
	// @return True if the field can be made editable.
	virtual bool IsEditable()
	{
		return false;
	}

	// Is this field dirty, that is, editable and changed?
	// Editable subclasses need to override this.
	// @return True if the data has changed.
	virtual bool IsDirty()
	{
		return false;
	}

	// Create an editing window and set m_hwnd to the hwnd of the active window.
	// Note: This should always be called from overloaded methods.
	// @param hwnd The parent window (AfDeSplitChild).
	// @param rc is the rectangle for the window based on parent window coordinates.
	// @param dxpCursor Pixel offset from the left edge of the contents to the location
	//		where the insertion point should be placed.
	// @param fTopCursor True if the insertion point should be placed on the top line
	//		or false if it is to be placed on the bottom line.
	// @return True if it succeeded.
	virtual bool BeginEdit(HWND hwnd, Rect & rc, int dxpCursor = 0, bool fTopCursor = true);

	// This is called when the user clicks inside a field editor that is active but which
	// has not filled its entire area with a child window (if it does fill its entire
	// area, this can't happen). The point passed is in SCREEN coordinates. Should return
	// true if it handled the click, false for default click handling.
	virtual bool ActiveClick(POINT pt)
	{
		return false;
	}

	// Saves changes but keeps the editing window open.
	// If it is possible to have bad data, the user should call IsOkToClose() first.
	// @return True if successful.
	virtual bool SaveEdit()
	{
		return false;
	}

	// Saves changes, destroys the editor window, and sets m_hwnd to 0.
	// If it is possible to have bad data, the user should call IsOkToClose() first.
	// Note: This should always be called from overloaded methods.
	// @param fForce True if we want to force the editor closed without making any
	//	validity checks or saving any changes. This is necessary in certain situations
	//	such as synchronization where temporary changes in an editor may conflict with
	//	the database and crash if allowed to save. One example is using type-ahead to add
	//	an existing item in the notebook researcher field, then before moving out of the
	//	field, use a list editor to merge this item with another item. When the sync
	//	process tries to update the notebook, it will fail when trying to save the
	//	researcher field because it is trying to send a replace to the database where
	//	the old value is no longer in the database.
	virtual void EndEdit(bool fForce = false);

	// Move the window to the new location bounded by rcClip. It is assumed that the
	// caller has already called SetHeightAt() so that it knows how much room is needed.
	// This is used to update an active editor, while Draw is used for inactive editors.
	// @param rcClip The new location rect in AfDeSplitChild coordinates.
	virtual void MoveWnd(const Rect & rcClip)
	{
	}

	// Validate the data and return true if it is valid. If invalid, the method should
	// raise an error for the user indicating the problem that needs to be corrected.
	// This only needs to be overwritten if data for this field can be in an invalid state
	// (e.g., a date string that doesn't parse into a date).
	// @param fWarn true if a warning should be displayed.
	//		false if the warning should be skipped.
	// @return True if data is valid.
	virtual bool IsOkToClose(bool fWarn = true)
	{
		return true;
	}

	// This method saves the current cursor information in RecMainWnd. Normally it just
	// stores the cursor index in RecMainWnd::m_ichCur. For structured texts, however,
	// it also inserts the appropriate hvos and flids for the StText classes in
	// m_vhvoPath and m_vflidPath. Other editors may need to do other things.
	virtual void SaveCursorInfo() {}

	// This attempts to place the cursor as defined in RecMainWnd m_vhvoPath, m_vflidPath,
	// and m_ichCur.
	// @param ihvoPath gives the index into m_vhvoPath where we are to start. This is
	// normally m_vhvoPath.Size(), but for structured texts, or other complex classes,
	// it may be less since it would point to embedded objects in the field.
	virtual void RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur) {}

	// Saves all information to restore the cursor to a given location in a field. It
	// doesn't support selection ranges. This information is stored in RecMainWnd m_vhvoPath,
	// m_vflidPath, and m_ichCur.
	virtual void SaveFullCursorInfo();


	// If the field needs to have data, this method should be overwritten to check the
	// requirments from the FldSpec, and verify that data in the field meets this requirement.
	// It returns:
	//	kFTReqNotReq if the all requirements are met.
	//	kFTReqWs if data is missing, but it is encouraged.
	//	kFTReqReq if data is missing, but it is required.
	//	kFTReqWsHidden if "encouraged" data is "hidden" by changing anal/vern ws selection.
	//	kFTReqReqReqHidden if "required" data is "hidden" by changing anal/vern ws selection.
	enum FldReqHidden
	{
		kFTReqWsHidden = kFTReqLim + 1,
		kFTReqReqHidden
	};

	virtual FldReq HasRequiredData()
	{
		return kFTReqNotReq;
	}

	// Return the FldSpec defining this field editor.
	FldSpec * GetFldSpec()
	{
		return m_qfsp;
	}

	// These should be overwritten for editors that use temporary caches.
	virtual IActionHandler * BeginTempEdit()
	{
		return NULL;
	}
	virtual IActionHandler * EndTempEdit()
	{
		return NULL;
	}

	void BeginChangesToLabel();
	// The Ok button on the chooser was clicked. We need to process the results. This gets
	// around the problem we have when all field editors are closed and reopened during a
	// sync process while a list chooser is open. The list chooser can't depend on a normal
	// return for processing the results since the calling editor may be gone.
	// This needs to be overwritten for any field editor that calls up a chooser.
	// @param pplc Pointer to the dialog box being closed.
	virtual void ChooserApplied(PossChsrDlg * pplc) {}

	int ActualWs()
	{
		return m_ws;
	}

	int MagicWs()
	{
		return m_wsMagic;
	}

	int UserWs()
	{
		return GetLpInfo()->GetDbInfo()->UserWs();
	}

protected:
	// Holds the hwnd for the active editor. Only one field editor is active at once.
	// This is unused for non-editable field editors.
	HWND m_hwnd;
	HVO m_hvoObj; // Id of object we are editing (the object that has m_flid).
	int m_flid; // Id of the field we are editing.
	int m_nIndent; // Level of nesting in the tree. 0 is top.
	int m_dypFontHeight; // Pixel height of font + 2 pixels (1 pixel above and below)

	// Computed pixel height of the field at the current width. This should allow for one pixel
	// above and below the text, plus one pixel for the bottom field divider line.
	int m_dypHeight;
	int m_dxpWidth; // Pixel width when last height was calculated.
	ITsStringPtr m_qtssLabel; // The label to show in the tree for this field.
	ITsStringPtr m_qtssHelp; // The "What's this" help string associated with this field.
	AfDeSplitChildPtr m_qadsc; // Pointer to the owning AfDeSplitChild window class.
	int m_ws; // Primary writing system for field contents.
	int m_wsMagic; // writing system (Anal, Vern, etc.)
	// Primary character rendering properties, including font name, fore/back colors, etc.
	LgCharRenderProps m_chrp;
	HBRUSH m_hbrBkg; // Brush for painting field background.
	HFONT m_hfont; // The font to use for displays.
	int m_hMark; // The mark handle from the action handler when a temp edit is in progress.
	FldSpecPtr m_qfsp; // The FldSpec that defines this field.

	// other protected methods.
	void MakeCharProps(int ws);

	HVO CreatePss(HVO hvoPssl, const OLECHAR * psz, PossNameType pnt, bool fHier);

	// Return the messages to use for the Find or Replace operation. Here we are searching
	// a single field at a time, so use messages indicating that.
	virtual void GetFindReplMsgs(int * pstidNoMatches, int * pstidReplaceN)
	{
		*pstidNoMatches = kstidNoMatchesField;
		*pstidReplaceN = kstidReplaceNField;
	}

	// These are redirecting methods that ask m_qadsc for information.
	AfMainWnd * MainWindow();
	void GetDataAccess(CustViewDa ** ppcvd);
	AfLpInfo * GetLpInfo();
};

#endif // AFDE_FIELD_EDITOR_INCLUDED
