/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeEdBoxBut.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	This is a data entry field editor base class for editors that use a TSS edit box for
	display and typing, plus a button to call up a dialog for setting the field contents.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDEFE_EDBOXBUT_INCLUDED
#define AFDEFE_EDBOXBUT_INCLUDED 1


/*----------------------------------------------------------------------------------------------
	This node is used to provide tree structures in data entry editors.
	Hungarian: dear.
----------------------------------------------------------------------------------------------*/

class AfDeFeEdBoxBut : public AfDeFieldEditor
{
public:
	typedef AfDeFieldEditor SuperClass;

	AfDeFeEdBoxBut();
	~AfDeFeEdBoxBut();

	virtual void Init();
	virtual void Draw(HDC hdc, const Rect & rcpClip);
	int SetHeightAt(int dxpWidth);
	virtual bool IsTextSelected();
	virtual void DeleteSelectedText();

	// This field can be made editable
	virtual bool IsEditable()
	{
		return true;
	}

	class DeEdit : public TssEdit
	{
	public:
		typedef TssEdit SuperClass;
		AfDeFeEdBoxBut * m_pdee;
		uint m_ch; // Last character for special Backspace handling.
		int m_cchMatched; // The number of matched characters typed by the user.
	protected:
		CMD_MAP_DEC(DeEdit);

		virtual bool OnChange();
		virtual bool OnKillFocus(HWND hwndNew);
		virtual bool OnSetFocus(HWND hwndOld, bool fTbControl = false);

		virtual bool OnCommand(int cid, int nc, HWND hctl);
		virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
		virtual bool CmdEdit(Cmd * pcmd);
		virtual bool CmsEditUpdate(CmdState & cms);
		virtual bool CmsCharFmt(CmdState & cms);
		virtual bool CmdEditUndo(Cmd * pcmd);
		virtual bool CmdEditRedo(Cmd * pcmd);
		virtual bool CmsEditUndo(CmdState & cms);
		virtual bool CmsEditRedo(CmdState & cms);
		int DxpCursorOffset();
		/* Not used at this point, but save in case we want to use later.
		static LRESULT CALLBACK MouseProc(int nc, WPARAM wp, LPARAM lp);
		static HHOOK s_hhk; // Hook to catch mouse events.
		static Rect s_rc; // The rectangle of the editor window.
		static AfDeFieldEditor * s_pdfe; // Pointer to current field editor.
		*/

		virtual void GetFindReplMsgs(int * pstidNoMatches, int * pstidReplaceN)
		{
			*pstidNoMatches = kstidNoMatchesField;
			*pstidReplaceN = kstidReplaceNField;
		}
	};

	// These will need to be overwritten in the subclass.
	virtual bool BeginEdit(HWND hwnd, Rect & rc, int dxpCursor = 0, bool fTopCursor = true)
	{
		return BeginEdit(hwnd, rc, dxpCursor, fTopCursor, ktptIsEditable);
	}
	virtual bool BeginEdit(HWND hwnd, Rect & rc, int dxpCursor = 0, bool fTopCursor = true,
		TptEditable tpte = ktptIsEditable);
	virtual void EndEdit(bool fForce = false);
	virtual void MoveWnd(const Rect & rcClip);
	virtual void UpdateField();
	virtual void ProcessChooser() {};
	virtual bool OnChange(AfDeFeEdBoxBut::DeEdit * pedit)
	{
		return false;
	}
	virtual FldReq HasRequiredData();
	virtual IActionHandler * BeginTempEdit();
	virtual IActionHandler * EndTempEdit();
	virtual void SaveCursorInfo();
	virtual void RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur);

	class DeButton : public AfWnd
	{
	public:
		typedef AfWnd SuperClass;
		AfDeFeEdBoxBut * m_pdee;
		bool m_fClicked; // Display should show depressed button.
	protected:
		bool OnDrawThisItem(DRAWITEMSTRUCT * pdis);
		bool GetHelpStrFromPt(Point pt, ITsString ** pptss);
	};

protected:
	HWND m_hwndButton; // Windows handle for the button.
	ITsStringPtr m_qtss; // The text to display/edit for this field.
	// Used to avoid an extra SaveEdit() when the delete dialog is raised.
	bool m_fDelFromDialog;
#if 1
	bool m_fRtl;
#endif

	// Return the messages to use for the Find or Replace operation. Here we are searching
	// a single field at a time, so use messages indicating that.
	virtual void GetFindReplMsgs(int * pstidNoMatches, int * pstidReplaceN)
	{
		*pstidNoMatches = kstidNoMatchesField;
		*pstidReplaceN = kstidReplaceNField;
	}
};

typedef GenSmartPtr<AfDeFeEdBoxBut::DeEdit> DeEditPtr;
typedef GenSmartPtr<AfDeFeEdBoxBut::DeButton> DeButtonPtr;


#endif // AFDEFE_EDBOXBUT_INCLUDED
