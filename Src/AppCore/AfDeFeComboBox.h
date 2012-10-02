/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeComboBox.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	This is a data entry field editor for combo-box fields.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDEFE_COMBOBOX_INCLUDED
#define AFDEFE_COMBOBOX_INCLUDED 1

//class DecbCombo;

class AfDeFeComboBox;
typedef GenSmartPtr<AfDeFeComboBox> AfDeFeComboBoxPtr;

enum
{
	kitssEmpty = -1
};

/*----------------------------------------------------------------------------------------------
	This node is used to provide tree structures in data entry editors.
	Hungarian: decb.
----------------------------------------------------------------------------------------------*/

class AfDeFeComboBox : public AfDeFieldEditor, public PossListNotify
{
public:
	typedef AfDeFieldEditor SuperClass;
	class DecbFrame;
	typedef GenSmartPtr<DecbFrame> DecbFramePtr;
	class DecbEdit;
	typedef GenSmartPtr<DecbEdit> DecbEditPtr;
	class DecbButton;
	typedef GenSmartPtr<DecbButton> DecbButtonPtr;
	class DecbListBox;
	typedef GenSmartPtr<DecbListBox> DecbListBoxPtr;


	AfDeFeComboBox();
	~AfDeFeComboBox();

	void Init(PossNameType pnt = kpntName);
	void Draw(HDC hdc, const Rect & rcpClip);
	int SetHeightAt(int dxpWidth);
	int SetItem(int itss);

	void SetIndex(int itss)
	{
#ifdef DEBUG
		bool fOk = (itss == kitssEmpty);
		if (m_hvoPssl)
		{
			PossListInfoPtr qpli;
			GetLpInfo()->LoadPossList(m_hvoPssl, m_wsMagic, &qpli);
			fOk |= (uint)itss < (uint)qpli->GetCount();
		}
		else
			fOk |= (uint)itss < (uint)m_vtss.Size();
		Assert(fOk);
#endif
		m_itss = itss;
	}

	int GetIndex()
	{
		return m_itss;
	}

	ComVector<ITsString> * GetVec()
	{
		return &m_vtss;
	}

	HVO GetPssl()
	{
		return m_hvoPssl;
	}

	void SetPssl(HVO hvoPssl);

	// This field can be made editable
	virtual bool IsEditable()
	{
		return true;
	}

	virtual bool BeginEdit(HWND hwnd, Rect & rc, int dxpCursor = 0, bool fTopCursor = true)
	{
		return BeginEdit(hwnd, rc, dxpCursor, fTopCursor, ktptSemiEditable);
	}

	virtual bool BeginEdit(HWND hwnd, Rect & rc, int dxpCursor = 0, bool fTopCursor = true,
		TptEditable tpte = ktptIsEditable);
	virtual bool IsTextSelected();
	virtual void DeleteSelectedText();
	virtual bool IsDirty();
	virtual bool SaveEdit();
	virtual void EndEdit(bool fForce = false);
	virtual void MoveWnd(const Rect & rcClip);
	virtual void UpdateField();
	virtual bool ActiveClick(POINT pt);
	virtual FldReq HasRequiredData();
	virtual void OnReleasePtr();
	virtual IActionHandler * BeginTempEdit();
	virtual IActionHandler * EndTempEdit();
	virtual void SaveCursorInfo();
	virtual void RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur);
	virtual void ListChanged(int nAction, HVO hvoPssl, HVO hvoSrc, HVO hvoDst, int ipssSrc,
		int ipssDst);

	// Used as part of the "combo box".
	class DecbListBox : public TssListBox
	{
		typedef TssListBox SuperClass;
	public:
		friend AfDeFeComboBox;
		friend DecbFrame;
		typedef TssListBox SuperClass;
	protected:
		bool OnCommand(int cid, int nc, HWND hctl);
		virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
		void PostAttach(void);
		void OnMouseMove(UINT nFlags, POINT point);
		virtual void OnReleasePtr();

		AfDeFeComboBoxPtr m_qdecb;
		int m_itssOrg; // The original item. Esc returns to this.
	};

	// Without this frame, a popup listbox will not display any text.
	class DecbFrame : public AfWnd
	{
		typedef AfWnd SuperClass;
	public:
		friend AfDeFeComboBox;
		friend DecbListBox;
		friend DecbEdit;
		typedef AfWnd SuperClass;
	protected:
		bool m_fKeyPressed;	// Used by list box to signal a key was processed to change a
							// selection.
		bool OnCommand(int cid, int nc, HWND hctl);
		virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
		void PostAttach(void);
		virtual void OnReleasePtr();

		DecbEditPtr m_qcbed; // Pointer to the edit box.
		AfDeFeComboBoxPtr m_qdecb;
	};

	class DecbEdit : public TssEdit
	{
		typedef TssEdit SuperClass;
	public:
		~DecbEdit()
			{ } // Do nothing
		friend AfDeFeComboBox;
		friend DecbListBox;
		friend DecbFrame;
		typedef TssEdit SuperClass;
		bool OnButtonClk();
	protected:
		CMD_MAP_DEC(DecbEdit);

		virtual bool OnChange();
		virtual bool OnKillFocus(HWND hwndNew);
		virtual bool OnSetFocus(HWND hwndOld, bool fTbControl = false);
		virtual void OnReleasePtr();

		virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
		virtual bool CmdEdit(Cmd * pcmd);
		virtual bool CmsEditUpdate(CmdState & cms);
		virtual bool CmsCharFmt(CmdState & cms);
		virtual void PostAttach(void);
		virtual bool CmdEditUndo(Cmd * pcmd);
		virtual bool CmdEditRedo(Cmd * pcmd);
		virtual bool CmsEditUndo(CmdState & cms);
		virtual bool CmsEditRedo(CmdState & cms);
		bool FindPliItem(StrUni & stubTyped, StrUni & stuFound, int * pipss,
			int * pwspss, ComBool fExactMatch = false);
		bool FindVecItem(StrUni & stubTyped, StrUni & stuFound, int * pipss);
		int DxpCursorOffset();

		AfDeFeComboBoxPtr m_qdecb;
		DecbFramePtr m_qdef;
		uint m_ch; // Last character for special Backspace handling.
		bool m_fRecurse; // Flag to stop recursion.
		int m_cchMatched; // The number of matched characters typed by the user.
		bool m_fSkipBnClicked; // Used to skip a BN_CLICKED message.
	};

	// This is the button for the "combo box".
	class DecbButton : public AfWnd
	{
		typedef AfWnd SuperClass;
	public:
		friend AfDeFeComboBox;
		typedef AfWnd SuperClass;
	protected:
		bool OnDrawThisItem(DRAWITEMSTRUCT * pdis);
		bool GetHelpStrFromPt(Point pt, ITsString ** pptss);
		bool OnCommand(int cid, int nc, HWND hctl);
		virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
		void PostAttach(void);
		virtual void OnReleasePtr();

		AfDeFeComboBoxPtr m_qdecb;
	};

	friend DecbEdit;
	friend DecbListBox;
	friend DecbFrame;

protected:
	HWND m_hwndButton; // Windows handle for the button.
	int m_itss; // Index to the selected TsString; -1 if possibility item not set.

	// We use one or the other of these two. If hvoPssl is set, m_vtss is ignored.
	ComVector<ITsString> m_vtss; // Vector of strings.
	HVO m_hvoPssl; // The id of the possibility list being shown (if there is one)

	PossNameType m_pnt; // Determines whether we show name, abbr, or both for poss items.
	DecbEditPtr m_qde; // The embedded edit box.
	// This flag is used to skip notifications from PossListInfo when we are in the
	// middle of adding a new item. Without this, the update temporarily flashes the old back
	// while we are processing the new list.
	bool m_fSaving;
};


#endif // AFDEFE_COMBOBOX_INCLUDED
