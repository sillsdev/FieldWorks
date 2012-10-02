/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TssCombo.h
Responsibility: Rand Burgett
Last reviewed:

	This is the base Sdk class of an extended combo box designed for TsStrings.
	This class is used for Sdk applications, and is also the base for an ActiveX control.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TSSCombo_H
#define TSSCombo_H 1

class TssComboEx;
class TssCombo;
class TssComboEdit;
typedef GenSmartPtr<TssComboEx> TssComboExPtr;
typedef GenSmartPtr<TssCombo> TssComboPtr;
typedef GenSmartPtr<TssComboEdit> TssComboEditPtr;

// The reason we are using WM_APP here instead of WM_USER is because on some machines, using
// WM_USER as the base caused messages to be converted to another message somehow. I
// (DarrellZ) don't understand how the messages were getting converted, but using WM_APP
// seems to work.
enum
{
	FW_CB_ADDSTRING = WM_APP + 1,
	FW_CB_FINDSTRING,
	FW_CB_FINDSTRINGEXACT,
	FW_CB_GETLBTEXT,
	FW_CB_INSERTSTRING,
	FW_CB_SELECTSTRING,
	FW_CB_GETTEXT,
	FW_CBEM_GETITEM,
	FW_CBEM_INSERTITEM,
	FW_CBEM_SETITEM,
};


typedef struct
{
	uint mask;
	int iItem;
	ITsStringPtr qtss;
	int iImage;
	int iSelectedImage;
	int iOverlay;
	int iIndent;
	LPARAM lParam;
} FW_COMBOBOXEXITEM;


typedef struct
{
	NMHDR hdr;
	FW_COMBOBOXEXITEM ceItem;
} FW_NMCOMBOBOXEX;


typedef struct
{
	NMHDR hdr;
	int iItemid;
	ITsStringPtr qtss;
} FW_NMCBEDRAGBEGIN;


typedef struct
{
	NMHDR hdr;
	bool fChanged;
	int iNewSelection;
	ITsStringPtr qtss;
	int iWhy;
} FW_NMCBEENDEDIT;


/*----------------------------------------------------------------------------------------------
	This class represents the editable field inside a TssCombo. It handles tool tips and
	typeahead.

	Hungarian: tce
----------------------------------------------------------------------------------------------*/
class TssComboEdit : public AfWnd
{
	typedef AfWnd SuperClass;

	friend class TssComboEx;
	friend class TssCombo;

public:
	// Constructor.
	TssComboEdit()
	{
		m_hwndToolTip = NULL;
	}

	bool DoesTypeAhead();
	bool HasToolTip();
	HWND MainParent();
	int Cid();

	/*------------------------------------------------------------------------------------------
		Attach the given window handle to this object, and attach this object's WndProc handler
		to the window handle.  Also set the tooltip window handle for this object.

		@param hwnd Handle to the combobox window.
		@param hwndToolTip Handle to the combobox's tooltip window.
	------------------------------------------------------------------------------------------*/
	void Subclass(HWND hwnd, HWND hwndToolTip)
	{
		Assert(!m_hwnd);
		SubclassHwnd(hwnd);
		m_hwndToolTip = hwndToolTip;
	}

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	HWND m_hwndToolTip;				// Handle to the tooltip window, if any
	ITsStringPtr m_qtssFocusText;	// Text in window on receiving focus.
	ITsStringPtr m_qtssLastText;	// The last text that was in the combo box.
	bool CmdTypeAhead(Cmd * pcmd);
	CMD_MAP_DEC(TssComboEdit);
};


/*----------------------------------------------------------------------------------------------
	This class represents the actual combo box within a TssComboEx.

	Hungarian: tcdd
----------------------------------------------------------------------------------------------*/
class TssCombo : public AfWnd
{
	typedef AfWnd SuperClass;

	friend class TssComboEx;
	friend class TssComboEdit;

public:

	bool DoesTypeAhead();
	bool HasToolTip();
	HWND MainParent();
	int Cid();

	/*------------------------------------------------------------------------------------------
		Attach the given window handle to this object, and attach this object's WndProc handler
		to the window handle.  Also set the toolbar window handle for this object.

		@param hwnd Handle to the combobox window.
		@param hwndParent Handle to the combobox's enclosing toolbar window or dialog.
	------------------------------------------------------------------------------------------*/
	void Subclass(HWND hwnd, HWND hwndMainParent)
	{
		SubclassHwnd(hwnd);
	}

protected:
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

};


/*----------------------------------------------------------------------------------------------
	This class is the wrapper for a combo box that handles a TsString.
----------------------------------------------------------------------------------------------*/
class TssComboEx : public AfWnd
{
	typedef AfWnd SuperClass;

	friend class TssCombo;
	friend class TssComboEdit;

public:
	TssComboEx();
	~TssComboEx()
	{
	}
	void Create(HWND hwndParent, int cid, HWND hwndToolTip, bool fTypeAhead);

	virtual void CreateAndSubclassHwnd(WndCreateStruct & wcs);

	virtual void SubclassCombo(HWND hwnd);
	virtual void SubclassCombo(HWND hwndDlg, int wid, DWORD dwStyleExtra = 0,
		bool fTypeAhead = false);

	bool DoesTypeAhead()
	{
		return m_fTypeAhead;
	}
	bool HasToolTip()
	{
		return (m_hwndToolTip != NULL);
	}
	int Cid()
	{
		return m_cid;
	}

	int DeleteItem(uint iItem);
	int GetCount();
	int GetCurSel();
	int GetDroppedControlRect(RECT * prc);
	bool GetDroppedState();
	int GetDroppedWidth();
	uint GetEditSel(uint * pichStart, uint * pichEnd);
	bool GetExtendedUI();
	UINT GetHorizontalExtent();
	DWORD GetItemData(int iItem);
	int GetItemHeight(int iItem);
	int GetLBTextLen(int iItem);
	int GetTopIndex();
	int InitStorage(int cItems, uint cb);
	bool LimitText(int cchMax);
	int ResetContent();
	int SetCurSel(int iItem);
	int SetDroppedWidth(uint dxp);
	bool SetEditSel(int ichStart, int ichEnd);
	int SetExtendedUI(bool fExtended = true);
	void SetHorizontalExtent(uint dxpExtent);
	int SetItemData(int iItem, DWORD dwItemData);
	int SetItemHeight(int iItem, uint dypItem);
	int SetTopIndex(int iItem);
	bool ShowDropDown(bool fShowIt = true);

	int GetText(ITsString ** pptss);
	int AddString(ITsString * ptss);
	int FindString(int iItemStart, ITsString * ptss);
	int FindStringExact(int iStartAfter, ITsString * ptss);
	int GetLBText(int iItem, ITsString ** pptss);
	int InsertString(int iItem, ITsString * ptss);
	int SelectString(int iItemStart, ITsString * ptss);
	bool QtssToStr(ITsString * ptss, StrApp & str);

	HWND GetComboControl();
	HWND GetEditControl();
	uint GetExtendedStyle();
	HIMAGELIST GetImageList();
	bool GetItem(FW_COMBOBOXEXITEM * pfcbi);
	bool GetUnicodeFormat();
	bool HasEditChanged();
	int InsertItem(FW_COMBOBOXEXITEM * pfcbi);
	uint SetExtendedStyle(uint nExMask, uint nExStyle);
	HIMAGELIST SetImageList(HIMAGELIST himl);
	bool SetItem(FW_COMBOBOXEXITEM * pfcbi);
	bool SetUnicodeFormat(bool fUnicode);

	// Clipboard operations
	bool Undo();
	void Copy();
	void Cut();
	void Paste();

	/*------------------------------------------------------------------------------------------
		Notification message handlers. Override these in the derived class.
	------------------------------------------------------------------------------------------*/
	virtual bool OnCloseUp(int nID, HWND hwndCombo)
		{ return false; }
	virtual bool OnDblClk(int nID, HWND hwndCombo)
		{ return false; }
	virtual bool OnDropDown(int nID, HWND hwndCombo)
		{ return false; }
	virtual bool OnEditChange(int nID, HWND hwndCombo)
		{ return false; }
	virtual bool EditUpDate(int nID, HWND hwndCombo)
		{ return false; }
	virtual bool OnErrSpace(int nID, HWND hwndCombo)
		{ return false; }
	virtual bool OnKillFocus(int nID, HWND hwndCombo)
		{ return false; }
	virtual bool OnSelChange(int nID, HWND hwndCombo)
		{ return false; }
	virtual bool OnSelEndCancel(int nID, HWND hwndCombo)
		{ return false; }
	virtual bool OnSelEndOK(int nID, HWND hwndCombo)
		{ return false; }
	virtual bool OnSetFocus(int nID, HWND hwndCombo)
		{ return false; }

	virtual bool OnBeginEdit(NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnDeleteItem(FW_NMCOMBOBOXEX * pfnmcb, long & lnRet)
		{ return false; }
	virtual bool OnDragBegin(FW_NMCBEDRAGBEGIN * pfnmdb, long & lnRet)
		{ return false; }
	virtual bool OnEndEdit(FW_NMCBEENDEDIT * pfnmee, long & lnRet)
		{ return false; }
	virtual bool OnGetDispInfo(FW_NMCOMBOBOXEX * pfnmcb, long & lnRet)
		{ return false; }
	virtual bool OnInsertItem(FW_NMCOMBOBOXEX * pfnmcb, long & lnRet)
		{ return false; }
	virtual bool OnSetCursor(NMMOUSE * pnmm, long & lnRet)
		{ return false; }

	virtual bool OnCharEnter(int ctid, NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnCharTab(int ctid, NMHDR * pnmh, long & lnRet)
		{ return false; }
	virtual bool OnCharEscape(int ctid, NMHDR * pnmh, long & lnRet)
		{ return false; }

protected:
	void PreCreateHwnd(CREATESTRUCT & cs);

	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	int m_ws;
	ILgWritingSystemFactoryPtr m_qwsf;

	HWND m_hwndParent;		// enclosing toolbar or dialog
	int m_cid;				// for error message
	HWND m_hwndToolTip;		// tool tip window handle; NULL if none
	bool m_fTypeAhead;		// is the combo is a type-ahead combo?

	enum
	{
		kcchMaxText = 1024,
	};
	static achar s_rgchBuffer[kcchMaxText];

	virtual bool OnNotifyThis(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnCommand(int cid, int nc, HWND hctl);

	bool _OnDeleteItem(NMCOMBOBOXEX * pnmcb, long & lnRet);
	bool _OnDragBegin(NMCBEDRAGBEGIN * pnmdb, long & lnRet);
	bool _OnEndEdit(NMCBEENDEDIT * pnmee, long & lnRet);
	bool _OnGetDispInfo(NMCOMBOBOXEX * pnmcb, long & lnRet);
	bool _OnInsertItem(NMCOMBOBOXEX * pnmcb, long & lnRet);

	bool _CopyItem(const COMBOBOXEXITEM & cbi, FW_COMBOBOXEXITEM & fcbi);
	bool _CopyItem(const FW_COMBOBOXEXITEM & fcbi, COMBOBOXEXITEM & cbi);

	void TurnOnDefaultKeyboard();

	static bool s_fInitialized;

	int WritingSystem();
};

#endif //!TSSCombo_H