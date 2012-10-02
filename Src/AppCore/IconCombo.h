/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: IconCombo.h
Responsibility: Darrell Zook
Last reviewed: Not yet.

Description:
	This file contains class declarations for the following classes:
		IconComboCombo : AfWnd - This class creates an ownerdraw button that behaves like a
			normal combobox, except it pops up a IconComboPopup window when the down arrow is
			clicked. It shows the currently selected icon in the 'edit' area of the combobox.
		IconComboPopup : AfWnd - This class creates a popup window that allows the user to select
			one of the listed icons.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef IconCombo_H
#define IconCombo_H
#pragma once

class IconComboCombo;
class IconComboPopup;
typedef GenSmartPtr<IconComboCombo> IconComboComboPtr;
typedef GenSmartPtr<IconComboPopup> IconComboPopupPtr;
typedef GenSmartPtr<UiToolTip> UiToolTipPtr;


/*----------------------------------------------------------------------------------------------
	Provides a control that looks like a combo box, whose drop down is a menu of icons
	(from the color table).
	NOTE: This is implemented by using an ownerdraw button, not a combo box.
	NOTE: This class is untested; currently only the IconComboPopup class is used.
	(Used only in the toolbar, which does not require a distinct class to manage
	the button.)
	This class manages a variable owned by the client and keeps its value adjusted as
	the control is used.
	Hungarian: xcmb
----------------------------------------------------------------------------------------------*/
class IconComboCombo : public AfWnd
{
typedef AfWnd SuperClass;

public:
	// Construction
	IconComboCombo();

	void SubclassButton(HWND hwnd, int * pval, bool fShowText = true);

	// Attributes
	int GetVal()
	{
		AssertPtr(m_pival);

		return *m_pival;
	}
	void SetVal(int val)
	{
		AssertPtr(m_pival);

		*m_pival = val;
		if (::IsWindow(m_hwnd))
			::InvalidateRect(m_hwnd, NULL, TRUE);
	}

	void SetWindowSize();

protected:
	// Attributes
	int * m_pival; // Currently chosen icon.
	RECT m_rcArrowButton; // Rectangle for the drop-down button.
	UINT m_wid;
	bool m_fPushed;
	bool m_fShowText;

	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	bool OnCommand(int cid, int nc, HWND hctl);
	bool OnDrawThisItem(DRAWITEMSTRUCT * pdis);
	bool OnSelEndOK();
	bool OnSelEndCancel();
	bool OnSelChange();
};


/*----------------------------------------------------------------------------------------------
	Provides a popup menu to use with the combo box (if we finish implementing it)
	or with a toolbar icon combo button (currently the only one is the format borders button).
	In the DoPopup call, supply a pointer to an int which is the icon index.
	Hungarian: xpop
----------------------------------------------------------------------------------------------*/
class IconComboPopup : public AfWnd
{
typedef AfWnd SuperClass;

public:
	IconComboPopup();
	~IconComboPopup();

	bool DoPopup(WndCreateStruct & wcs, int * pival, POINT pt, int cval, int rid,
		int cColumns, bool * prgfPressed, HIMAGELIST himl);

	int GetVal()
	{
		AssertPtr(m_pival);

		return *m_pival;
	}

	void SetVal(int ival)
	{
		AssertPtr(m_pival);

		*m_pival = ival;
		if (IsWindow(m_hwnd))
			InvalidateRect(m_hwnd, NULL, TRUE);
	}

protected:
	// Attributes
	int m_cvals; // Number of values (icons) we have.
	int m_cColumns; // Number of columns in the popup menu.
	int m_cRows; // Number of rows in the popup menu.
	int m_ivalHot; // Currently selected cell (button).
	int m_ivalOld; // Cell corresponding to m_clrInitial.
	int * m_pival; // Currently selected icon value.
	int m_dzsBorder; // Width of the window's borders.
	int m_dxsButton; // Width of one complete button.
	int m_dysButton; // Height of one complete button.
	bool * m_prgfPressed;  // Array of booleans to determine button pressed
	HWND m_hwndToolTip; // Provides the name for each icon.
	POINT m_pt; // Position to show the popup.
	bool m_fIgnoreButtonUp;
	UINT m_wid;
	HWND m_hwndParent;
	bool m_fMouseDown;
	bool m_fCanceled;
	HIMAGELIST m_himl; // List of images to show

	// Constants
	enum
	{
		kdzsButton = 18 // Size (width, height) of each button.
	};

	enum
	{
		kdxsIcon = 16,
		kdysIcon = 15 // Size (width, height) of each button.
	};

	// Methods
	void PreCreateHwnd(CREATESTRUCT & cs);

	void GetCellRect(int iclr, Rect & rc);
	void ChangeSelection(int iSel);
	void EndSelection(int nMessage);
	void DrawCell(HDC hdc, int iclr, bool fHot, bool fOld);
	int GetRowFromColor(int iclr) const;
	int GetColFromColor(int iclr) const;
	int GetRowFromPt(const Point & pt);
	int GetColFromPt(const Point & pt);
	void GetPtFromRowCol(Point & pt, int row, int col);
	int GetTableIndexFromRowCol(int row, int col) const;

	// Notifications
	bool OnLButtonUp(UINT nFlags, POINT pt);
	void OnPaint();
	void OnMouseMove(UINT nFlags, int xp, int yp);
	bool OnKeyDown(int vk, int cact);
	bool OnQueryNewPalette();
	void OnPaletteChanged(HWND hwndFocus);
	bool OnKillFocus(HWND hwndNew);
};

#endif // !IconCombo_H