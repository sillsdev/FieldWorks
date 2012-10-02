/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UiColor.h
Responsibility: Darrell Zook
Last reviewed: Not yet.

Description:
	This file contains class declarations for the following classes:
		UiColorCombo : AfWnd - This class creates an ownerdraw button that behaves like a
			normal combobox, except it pops up a UiColorPopup window when the down arrow is
			clicked. It shows the currently selected color in the 'edit' area of the combobox.
		UiColorPopup : AfWnd - This class creates a popup window that allows the user to select
			one of our 40 predefined colors (listed in AfColorTable.h).
		UiToolTip : AfWnd - This class provides tooltips for the UiColorPopup window.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef UICOLOR_H
#define UICOLOR_H
#pragma once

class UiColorCombo;
class UiColorPopup;
class UiToolTip;
typedef GenSmartPtr<UiColorCombo> UiColorComboPtr;
typedef GenSmartPtr<UiColorPopup> UiColorPopupPtr;
typedef GenSmartPtr<UiToolTip> UiToolTipPtr;


/*----------------------------------------------------------------------------------------------
	This class is needed to get tooltips for the UiColorPopup to work properly. I have no idea
	why this is required, but I (DarrellZ) couldn't get it to work without it. The problem
	exists because the UiColorPopup uses a modal loop so that its DoPopup method doesn't
	return until the user has either selected a new color or cancelled the box. It seems that
	messages don't get routed properly to the tooltip. After I overrode the TTM_WINDOWFROMPOINT
	message to change the return parameter, it seemed to work for some reason.
----------------------------------------------------------------------------------------------*/
class UiToolTip : public AfWnd
{
typedef AfWnd SuperClass;

public:
	HWND Create(HWND hwndParent);
	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

protected:
	HWND m_hwndParent;
};


/*----------------------------------------------------------------------------------------------
	Provides a control that looks like a combo box, whose drop down is a menu of colors
	(from the color table).
	NOTE: This is implemented by using an ownerdraw button, not a combo box.
	Hungarian: ccmb
----------------------------------------------------------------------------------------------*/
class UiColorCombo : public AfWnd
{
typedef AfWnd SuperClass;

public:
	// Construction
	UiColorCombo();

	void SubclassButton(HWND hwnd, COLORREF * pclr, bool fShowText = true);

	// Attributes
	COLORREF GetColor()
	{
		AssertPtr(m_pclr);

		return *m_pclr;
	}
	void SetColor(COLORREF clr)
	{
		AssertPtr(m_pclr);

		*m_pclr = clr;
		if (::IsWindow(m_hwnd))
			::InvalidateRect(m_hwnd, NULL, TRUE);
	}

	void SetLabelColor(COLORREF clr)
	{
		if (m_clrLabel != clr)
		{
			m_clrLabel = clr;
			if (::IsWindow(m_hwnd))
				::InvalidateRect(m_hwnd, NULL, TRUE);
		}
	}

	void SetWindowSize();

protected:
	// Attributes
	COLORREF * m_pclr; // Currently chosen color.
	RECT m_rcArrowButton; // Rectangle for the drop-down button.
	UINT m_wid;
	bool m_fPushed;
	bool m_fShowText;
	COLORREF m_clrLabel;  // color with which to write label

	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	bool OnCommand(int cid, int nc, HWND hctl);
	bool OnDrawThisItem(DRAWITEMSTRUCT * pdis);
	bool OnSelEndOK();
	bool OnSelEndCancel();
	bool OnSelChange();
	bool OnKillFocus();
};


/*----------------------------------------------------------------------------------------------
	Provides a popup menu to use with the combo box or with a toolbar color button.
	Hungarian: cop
----------------------------------------------------------------------------------------------*/
class UiColorPopup : public AfWnd
{
typedef AfWnd SuperClass;

public:
	UiColorPopup();
	~UiColorPopup();

	bool DoPopup(WndCreateStruct & wcs, COLORREF * pclr, POINT pt);

	COLORREF GetColor()
	{
		AssertPtr(m_pclr);

		return *m_pclr;
	}

	void SetColor(COLORREF clr)
	{
		AssertPtr(m_pclr);

		*m_pclr = clr;
		if (IsWindow(m_hwnd))
			InvalidateRect(m_hwnd, NULL, TRUE);
	}

protected:
	// Attributes
	int m_cColumns; // Number of columns in the popup menu.
	int m_cRows; // Number of rows in the popup menu.
	int m_iclrHot; // Currently selected cell (button).
	int m_iclrOld; // Cell corresponding to m_clrInitial.
	COLORREF * m_pclr; // Currently selected color value.
	COLORREF m_clrOld; // Initial color when window was created.
	int m_dzsBorder; // Width of the window's borders.
	HWND m_hwndToolTip; // Provides the name for each color.
	POINT m_pt; // Position to show the popup.
	bool m_fIgnoreButtonUp;
	UINT m_wid;
	HWND m_hwndParent;
	int m_dysNinchRow;
	bool m_fMouseDown;
	bool m_fCanceled;

	// Constants
	enum
	{
		kcolMax =  8, // Number of columns in the popup menu table.
		krowMax =  6, // Number of rows in the popup table.
		kdzsButton = 18 // Size (width, height) of each button.
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

#endif // !UICOLOR_H