/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TssListBox.h
Responsibility: Darrell Zook
Last reviewed:

	This is the base Sdk class of a listbox designed for TsStrings.
	This class is used for Sdk applications, and is also the base for an ActiveX control.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TSSLISTBOX_H
#define TSSLISTBOX_H 1

typedef enum
{
	cssNone,
	cssSingle,
	cssMultiple,
	cssExtended,
} CtlSelStyle;

// The reason we are using WM_APP here instead of WM_USER is because on some machines, using
// WM_USER as the base caused messages to be converted to another message somehow. I
// (DarrellZ) don't understand how the messages were getting converted, but using WM_APP
// seems to work.
enum
{
	FW_LB_ADDSTRING = WM_APP + 1,
	FW_LB_GETTEXT,
	FW_LB_INSERTSTRING,
	FW_LB_SELECTSTRING,
	FW_LB_FINDSTRING,
	FW_LB_FINDSTRINGEXACT,
};

/*----------------------------------------------------------------------------------------------
	This class represents our list box window.
	Hungarian: tlb
----------------------------------------------------------------------------------------------*/
class TssListBox : public AfWnd
{
typedef AfWnd SuperClass;

public:
	// Constructor and destructor.
	TssListBox();

	virtual void SubclassListBox(HWND hwnd);

/***********************************************************************************************
	MFC-like methods
***********************************************************************************************/
// Attributes
	// For entire listbox
	int GetCount();
	int GetTopIndex();
	int SetTopIndex(int iItem);
	int InitStorage(int cItems, uint cBytes);
	uint ItemFromPoint(POINT pt);

	// For single-selection listboxes
	int GetCurSel();
	int SetCurSel(int iSelect);

	// For multiple-selection listboxes
	int GetSel(int iItem); // also works for single-selection
	int SetSel(int iItem, bool fSelect = true);
	int GetSelCount();
	int GetSelItems(int cItems, int * prgnIndex);
	void SetAnchorIndex(int iItem);
	int GetAnchorIndex();

	// For listbox items
	DWORD GetItemData(int iItem);
	int SetItemData(int iItem, DWORD dwItemData);
	int GetItemRect(int iItem, RECT * prc);
	int GetText(int iItem, ITsString ** pptss);
	int GetTextLen(int iItem);

	// Settable only attributes
	void SetColumnWidth(int cxWidth);
	bool SetTabStops(int cTabStops, int * prgnTabStops);
	void SetTabStops();
	bool SetTabStops(const int & duEachStop); // takes an 'int'

	int GetItemHeight(int iItem);
	int GetCaretIndex();
	int SetCaretIndex(int iItem, bool fScroll = true);

// Operations
	// Manipulating listbox items
	int AddString(ITsString * ptss);
	int DeleteString(uint iItem);
	int InsertString(int iItem, ITsString * ptss);
	void ResetContent();

	// Selection helpers
	int FindString(int iStartAfter, ITsString * ptss);
	int FindStringExact(int iStartAfter, ITsString * ptss);
	int SelectString(int iStartAfter, ITsString * ptss);
	int SelItemRange(bool fSelect, int iFirstItem, int iLastItem);

// Events. Override these in the derived class.
	virtual bool OnSelChange(int cid, HWND hctl)
		{ return false; }
	virtual bool OnDblClick(int cid, HWND hctl)
		{ return false; }
	virtual bool OnSelCancel(int cid, HWND hctl)
		{ return false; }

protected:
	ComVector<ITsString> m_vItems; // Vector of items in the listbox.

	// These are used for look-ahead typing.
	OLECHAR m_rgchLookAhead[10];
	int m_cchLookAhead;

	void PreCreateHwnd(CREATESTRUCT & cs);

	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	virtual int OnChar(int ch);
	virtual int OnTimer(UINT nIDEvent);
	virtual bool OnDrawThisItem(DRAWITEMSTRUCT * pdis);
	virtual bool OnMeasureThisItem(MEASUREITEMSTRUCT * pmis);
	virtual bool OnNotifyThis(int id, NMHDR * pnmh, long & lnRet);
};

typedef GenSmartPtr<TssListBox> TssListBoxPtr;

#endif //!TSSLISTBOX_H