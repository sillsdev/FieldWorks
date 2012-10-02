/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfHeaderWnd.h
Responsibility: Waxhaw Team
Last reviewed:

Description:
	This file contains class declarations for the following classes:
		AfHeaderWnd : AfWnd - This is an abstract base class that should be derived off
			of to create windows that show up in the MDI client window.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFHEADERWND_H
#define AFHEADERWND_H 1

class AfHeaderWnd;
class AfCaptionBar;
class HeaderWndCaptionBar;

typedef GenSmartPtr<AfHeaderWnd> AfHeaderWndPtr;
typedef GenSmartPtr<HeaderWndCaptionBar> HeaderWndCaptionBarPtr;

struct ColWidthInfo
{
	int dxpPrefer;		// A column's perferred width. Persistent? some day
	int dxpCurrent;		// A column's current width.
	int dxpMin;			// A column's minimum width.
	bool fSizeableByDrag;	// User can resize this column
	bool fStretchable;	// Program may adjust width to fill window
};

/*----------------------------------------------------------------------------------------------
	Bitmask flags for passing to the AfHeaderWnd Create function.

	Hungarian: fhwc
----------------------------------------------------------------------------------------------*/
enum HeaderWndCreationFlags
{
	kfhwcColHeadings = 1,
	kfhwcEnhanced3DTop = 2,
	kfhwcEtchedColHeadingTop = 4,
};


typedef Vector<ColWidthInfo> ColWidthVec; // Hungarian: vcwi
#define kdxpDefMinColWid 4

/*----------------------------------------------------------------------------------------------
	Header Window's Caption bar.

	Hungarian: hwcbr
----------------------------------------------------------------------------------------------*/
class HeaderWndCaptionBar : public AfCaptionBar
{
public:
	typedef AfCaptionBar SuperClass;

	HeaderWndCaptionBar();
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
};

/*----------------------------------------------------------------------------------------------
	Header window. This window can contain a large caption bar and column heading control,
	Both are optional, however.

	Hungarian: afhw
----------------------------------------------------------------------------------------------*/
class AfHeaderWnd : public AfWnd
{
public:
	typedef AfWnd SuperClass;

	AfHeaderWnd();
	virtual ~AfHeaderWnd() // Need for subclasses.
	{}

	void PostAttach(void);
	void ShowCaptionBar(bool fShow);
	bool IsCaptionBarVisible();

	virtual void Create(Pcsz pszCaption, HIMAGELIST himlCaption, DWORD dwFlags,
		int wid, int dypCaptionHeight = -1, DWORD dwCaptionFlags = 0);

	void AddColumn(int icol, const achar * strHeading, int dxpWidth, int dxpMinWidth = kdxpDefMinColWid,
		bool fSizeableByDrag = true, bool fStretchable = false);

	int GetWindowId()
		{return m_wid;}

	// NOTE: This doesn't return a reference count.
	HeaderWndCaptionBar * GetCaptionBar()
		{return m_qcpbr;}

	bool IsColHeadingVisible()
		{return m_hwndColHeading;}

	int GetCaptionBarHeight()
		{return m_dypCaptionHeight;}

protected:
	HeaderWndCaptionBarPtr m_qcpbr;
	HWND m_hwndColHeading;
	HIMAGELIST m_himlCaption;		// List of images for the icons in the caption bar.
	DWORD m_dwCaptionFlags;			// Flags for creating the caption bar window.
	bool m_fCaption;				// True if owner wants a caption bar window.
	StrApp m_strCaption;			// Text to go in caption window.
	int m_dypCaptionHeight;			// The height of the caption bar if visible.
	int m_dypColHeadingHeight;		// The height of the column heading control.
	int m_wid;
	ColWidthVec m_vcwiColInfo;		// Width info of columns in heading

	// True if owner wants a column heading control bar or column heading.
	// Which ever of those two is on top gets the enhanced top border.
	bool m_fColHeading;

	// True if border above column headings should appear to be etched.
	bool m_fEtchedColHeadingTop;

	// True when user wants a dark 3D border above the column headings. If user wants
	// this for the caption bar then that's part of m_dwCaptionFlags.
	bool m_fEnhancedCol3DTop;

	// The combined height of the caption and column header windows (if they're showing).
	// This is used to determine where to draw this window's client stuff.
	int m_ypTopOfClient;

	// This is the height of the caption bar plus a pixel to produce the etched look
	// between the caption bar and the column heading.
	int m_ypTopOfColHeading;

	// When true, it means the user is holding down the primary mouse button over the
	// caption bar's top border, while dragging the mouse.
	bool m_fDraggingTopCaptionBorder;

	void AdjustColumnWidths(int iColNumber = -1);
	bool ChangeColWidthCond(int icwi, int dxpNew);
	virtual bool OnSize(int wst, int dxp, int dyp);
	virtual bool OnPaint(HDC hdc);
	virtual void ChangeColWidth(int icwi, int dxpNew);
	virtual void ReDrawColumns();
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnChildEvent(int ceid, AfWnd *pAfWnd, void *lpInfo = NULL);
};


#endif // !AFHEADERWND_H
