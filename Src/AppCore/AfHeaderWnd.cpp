/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfHeaderWnd.cpp
Responsibility: Waxhaw Team
Last reviewed:

Description:
	This file contains implementations for the following classes:
		AfHeaderWnd : AfWnd
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

static bool s_fInChangeColWidth;

/***********************************************************************************************
	HeaderWndCaptionBar stuff.
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
HeaderWndCaptionBar::HeaderWndCaptionBar() : SuperClass()
{
	// Get the system's caption font and set the caption bar's font to the same
	// face name, but set it to a larger size, like Outlook's.
	NONCLIENTMETRICS ncm;
	ncm.cbSize = sizeof(NONCLIENTMETRICS);
	SystemParametersInfo(SPI_GETNONCLIENTMETRICS, sizeof(NONCLIENTMETRICS), &ncm, 0);
	ncm.lfCaptionFont.lfHeight = 20;
	SetCaptionFont(&ncm.lfCaptionFont);
}

/*----------------------------------------------------------------------------------------------
	Handle window messages. Return true if handled.
	See ${AfWnd#FWndProc} for parameter and return descriptions.
----------------------------------------------------------------------------------------------*/
bool HeaderWndCaptionBar::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	bool fRet;

	// Let the header window deal with these messages, so send them on.
	switch(wm)
	{
	case WM_LBUTTONDOWN:
		fRet = SuperClass::FWndProc(wm, wp, lp, lnRet);
		if (fRet)
		{
			return true;
			break;
		}

	case WM_MBUTTONDOWN:
	case WM_RBUTTONDOWN:
	case WM_SETFOCUS:
		::SendMessage(Parent()->Hwnd(), WM_SETFOCUS, wp, lp);
		return true;
		break;

	case WM_KILLFOCUS:
		::SendMessage(Parent()->Hwnd(), wm, wp, lp);
		return true;
		break;

	default:
		return SuperClass::FWndProc(wm, wp, lp, lnRet);
		break;
	}
}

/***********************************************************************************************
	AfHeaderWnd stuff.
***********************************************************************************************/
AfHeaderWnd::AfHeaderWnd()
{
	m_hwndColHeading = 0;
	m_fDraggingTopCaptionBorder = false;
}

/*----------------------------------------------------------------------------------------------
	This allows the owner to add columns to the column heading control. Columns will be added
	at the position of icol.
----------------------------------------------------------------------------------------------*/
void AfHeaderWnd::AddColumn(int icol, const achar * strHeading, int dxpWidth, int dxpMinWidth,
	bool fSizeableByDrag, bool fStretchable)
{
	Assert(m_hwndColHeading);
	HDITEM hdi = { HDI_TEXT | HDI_WIDTH };
	hdi.pszText = const_cast<achar *>(strHeading);
	hdi.cxy = dxpWidth;
	::SendMessage(m_hwndColHeading, HDM_INSERTITEM, icol, (LPARAM)&hdi);
	ColWidthInfo cwiWidInfo;
	cwiWidInfo.dxpPrefer = cwiWidInfo.dxpCurrent = dxpWidth;
	cwiWidInfo.dxpMin = dxpMinWidth;
	cwiWidInfo.fSizeableByDrag = fSizeableByDrag;
	cwiWidInfo.fStretchable = fStretchable;
	m_vcwiColInfo.Push(cwiWidInfo);
	AdjustColumnWidths();
}

/*----------------------------------------------------------------------------------------------
	Turn the caption on or off. Note that the window must have been given a caption bar when
	created.
----------------------------------------------------------------------------------------------*/
void  AfHeaderWnd::ShowCaptionBar(bool fShow)
{
	AssertObj(m_qcpbr);
	::ShowWindow(m_qcpbr->Hwnd(), fShow ? SW_SHOW : SW_HIDE);
}

/*----------------------------------------------------------------------------------------------
	Test visibility (and existence) of caption bar. (This can't be inline because the
	declaration of CaptionBar comes after our header.)
----------------------------------------------------------------------------------------------*/
bool AfHeaderWnd::IsCaptionBarVisible()
{
	return (m_qcpbr ? ::IsWindowVisible(m_qcpbr->Hwnd()) : false);
}

/*----------------------------------------------------------------------------------------------
	Adjusts the Columns widths of the Column Headers either from an OnSize, OnNotifyChild,
	or AddColumn.
	iColNumber = Default is -1, all columns need to be checked for adjustment. If it's a positive
	number start from there and check about changing remaining columns.
----------------------------------------------------------------------------------------------*/
void AfHeaderWnd::AdjustColumnWidths(int iColNumber)
{
	bool fColChanged = false;

	// If this is true it means we're here from the call to
	// Header_SetItem in ChangeColWidth. If that's the case, get out now.
	if (s_fInChangeColWidth)
		return;

	// Get Width of Window
	Rect rcClient; // area with header border
	::GetClientRect(m_hwnd, &rcClient);
	int dxpMaxWidth = rcClient.Width();

	bool fDraggingColumn = true;
	if (-1 == iColNumber)
	{
		fDraggingColumn = false;
		iColNumber = 0;
	}

	// Subtract the Current width of any columns not being changed
	for (int icwi = 0; icwi < iColNumber; icwi++)
	{
		dxpMaxWidth -= m_vcwiColInfo[icwi].dxpCurrent;
		// This is untested at this point - REMcG 7/5/01
		if (m_vcwiColInfo[icwi].dxpCurrent > m_vcwiColInfo[icwi].dxpPrefer)
			m_vcwiColInfo[icwi].dxpPrefer = m_vcwiColInfo[icwi].dxpCurrent;
	}

	// When this happens it's likely it's because we're adjusting columns
	// in a window whose size hasn't initially been set or the window is extremely small.
	if (dxpMaxWidth <= 0)
		return;

	// Add up preferred widths of columns
	int dxpTotalPrefer = 0;
	int dxpTotalCurrent = 0;
	int dxpTotalMin = 0;
	int dxpTotalStretchable = 0; //Total width of stretchable columns
	int dxpTotalStretched = 0;

	for (int icwi = iColNumber; icwi < m_vcwiColInfo.Size(); icwi++)
	{
		ColWidthInfo &cwi = m_vcwiColInfo[icwi];

		// Don't allow Prefer size to be less than Min for the column
		if (cwi.dxpPrefer < cwi.dxpMin)
			cwi.dxpPrefer = cwi.dxpMin;

		dxpTotalPrefer += cwi.dxpPrefer;
		dxpTotalCurrent += cwi.dxpCurrent;
		dxpTotalMin += cwi.dxpMin;
		if (cwi.fStretchable)
		{
			dxpTotalStretchable += cwi.dxpPrefer;
			if (cwi.dxpPrefer < cwi.dxpCurrent)
			{
				dxpTotalStretched += cwi.dxpCurrent - cwi.dxpPrefer;
			}
		}
	}

	if (dxpTotalPrefer <= dxpMaxWidth && dxpTotalStretchable > 0)
	{
		// if window is resized distribute excess space to the stretchable columns
		// if column is resized adjust column and adjust size of stretchable columns
		int dxpExtra = dxpMaxWidth - dxpTotalPrefer;
		for (int icwi = iColNumber; icwi < m_vcwiColInfo.Size(); icwi++)
		{
			ColWidthInfo &cwi = m_vcwiColInfo[icwi];
			if (cwi.fStretchable && dxpTotalStretchable > 0)
			{
				int dxpGrow = dxpExtra * cwi.dxpPrefer / dxpTotalStretchable;
				dxpExtra -= dxpGrow;
				dxpTotalStretchable -= cwi.dxpPrefer;
				fColChanged |= ChangeColWidthCond(icwi, cwi.dxpPrefer + dxpGrow);
			}
			else if (cwi.dxpCurrent != cwi.dxpPrefer)
			{
				ChangeColWidth(icwi, cwi.dxpPrefer);
				fColChanged = true;
			}
		}
		Assert (dxpExtra == 0);
	}
	else if (dxpTotalCurrent < dxpMaxWidth) // dxpTotalPrefer > dxpMaxWidth
	{
		// if the window size is increasing, first restore any columns which were previously
		// shrunk below their preference.

		// Total amount by which shrunk columns can actually be restored
		int dxpRestore = dxpMaxWidth - dxpTotalCurrent;
		// Total amount by which columns were previously shrunk
		int dxpTotalShrunk = dxpTotalPrefer - dxpTotalCurrent;

		for (int icwi = 0; icwi < m_vcwiColInfo.Size() && dxpTotalShrunk; icwi++)
		{
			ColWidthInfo &cwi = m_vcwiColInfo[icwi];
			if (cwi.dxpCurrent < cwi.dxpPrefer)
			{
				int dxpGrow = dxpRestore * (cwi.dxpPrefer - cwi.dxpCurrent) / dxpTotalShrunk;
				dxpRestore -= dxpGrow;
				dxpTotalShrunk -= cwi.dxpPrefer - cwi.dxpCurrent;
				dxpTotalCurrent += dxpGrow;
				fColChanged |= ChangeColWidthCond(icwi, cwi.dxpCurrent + dxpGrow);
			}
		}
	}
	else if (fDraggingColumn &&
		m_vcwiColInfo[iColNumber].dxpCurrent > m_vcwiColInfo[iColNumber].dxpPrefer)
	{
		// A column is being shrunk; grow columns to its right from their current widths
		// toward their prefered widths. Amount we want to grow is
		// m_vcwiColInfo[iColNumber].dxpCurrent - m_vcwiColInfo[iColNumber].dxpPrefer.

		ColWidthInfo &cwi = m_vcwiColInfo[iColNumber];

		// Total amount by which shrunk columns can actually be restored
		int dxpRestore = cwi.dxpCurrent - cwi.dxpPrefer;
		dxpTotalPrefer -= cwi.dxpPrefer;
		dxpTotalCurrent -= cwi.dxpCurrent;
		ChangeColWidth(iColNumber, cwi.dxpPrefer);
		fColChanged = true;

		// Total amount by which columns were previously shrunk
		int dxpTotalShrunk = dxpTotalPrefer - dxpTotalCurrent;

		for (int icwi = iColNumber + 1; icwi < m_vcwiColInfo.Size() && dxpTotalShrunk > 0; icwi++)
		{
			ColWidthInfo &cwi = m_vcwiColInfo[icwi];
			if (cwi.dxpCurrent < cwi.dxpPrefer)
			{
				int dxpGrow = dxpRestore * (cwi.dxpPrefer - cwi.dxpCurrent) / dxpTotalShrunk;
				dxpRestore -= dxpGrow;
				dxpTotalShrunk -= cwi.dxpPrefer - cwi.dxpCurrent;
				dxpTotalCurrent += dxpGrow;
				fColChanged |= ChangeColWidthCond(icwi, cwi.dxpCurrent + dxpGrow);
			}
		}
		Assert (dxpRestore == 0);
	}
	else
	{
		// The window size is decreasing or column is growing, shrink columns from current widths toward their min
		// widths. Amount we want to shrink is dxpTotalCurrent - dxpMaxWidth after we adjust column being changed.
		// Amount we can shrink is bounded by dxpTotalCurrent - dxpTotalMin
		int icwi = iColNumber;

		if (fDraggingColumn)
		{
			ColWidthInfo &cwi = m_vcwiColInfo[icwi];
			if (cwi.fStretchable)
				dxpTotalStretchable -= cwi.dxpPrefer;

			dxpTotalCurrent -= cwi.dxpCurrent;
			dxpTotalPrefer -= cwi.dxpPrefer;
			dxpTotalMin -= cwi.dxpMin;
			fColChanged |= ChangeColWidthCond(icwi, cwi.dxpPrefer);
			dxpMaxWidth -= cwi.dxpCurrent;
			icwi++;
		}

		// distribute shortage of space to the shrinkable columns (col less than preferred)
		int dxpOver = dxpTotalCurrent - dxpMaxWidth; // the amount we would like to shrink
		int dxpRestore = Min(dxpOver, dxpTotalStretched);

		for (int icwi2 = icwi; icwi2 < m_vcwiColInfo.Size() && dxpRestore; icwi2++)
		{
			ColWidthInfo &cwi = m_vcwiColInfo[icwi2];
			if (cwi.fStretchable && dxpTotalStretchable > 0)
			{
				int dxpShrink = dxpRestore * cwi.dxpPrefer / dxpTotalStretchable;
				dxpRestore -= dxpShrink;
				dxpOver -= dxpShrink;
				dxpTotalCurrent -= dxpShrink;
				dxpTotalStretchable -= cwi.dxpPrefer;
				fColChanged |= ChangeColWidthCond(icwi2, cwi.dxpCurrent - dxpShrink);
			}
		}

		int dxpMaxShrink = dxpTotalCurrent - dxpTotalMin; // the most we can shrink by
		if (dxpOver > dxpMaxShrink)
			dxpOver = dxpMaxShrink; // Amount we will shrink by
		for (; icwi < m_vcwiColInfo.Size() && dxpMaxShrink > 0; icwi++)
		{
			ColWidthInfo &cwi = m_vcwiColInfo[icwi];
			int dxpAvailShrink = cwi.dxpCurrent - cwi.dxpMin;
			int dxpShrink = dxpOver * dxpAvailShrink / dxpMaxShrink;
			dxpOver -= dxpShrink;
			dxpMaxShrink -= dxpAvailShrink;
			fColChanged |= ChangeColWidthCond(icwi, cwi.dxpCurrent - dxpShrink);
		}
		Assert (dxpOver == 0);
	}

	if (fColChanged)
		ReDrawColumns();
}

/*----------------------------------------------------------------------------------------------
	Change the column width. Override this method if you want this information to make the
	contents of the column conform to this header. This method should not be called unless
	the column's width is actually changing.
----------------------------------------------------------------------------------------------*/
void AfHeaderWnd::ChangeColWidth(int icwi, int dxpNew)
{

	// If this is true it means we're here recursively from the
	// Header_SetItem call below. If that's the case, get out now.
	if (s_fInChangeColWidth)
		return;

	s_fInChangeColWidth = true;
	ColWidthInfo &cwi = m_vcwiColInfo[icwi];
	Assert(cwi.dxpCurrent != dxpNew);

	// adjust the control
	HDITEM hdi = { HDI_WIDTH };
	hdi.cxy = dxpNew;
	Header_SetItem(m_hwndColHeading, icwi, &hdi);

	// fix cwi.dxpCurrent
	cwi.dxpCurrent = dxpNew;
	s_fInChangeColWidth = false;
}

/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
void AfHeaderWnd::ReDrawColumns(void)
{
	Rect rc; // area with header border
	::GetClientRect(m_hwnd, &rc);
	rc.top += m_ypTopOfClient;
	::InvalidateRect(m_hwnd, &rc, true);
}

/*----------------------------------------------------------------------------------------------
	If the new column width is different from the current, go ahead and call the routine that
	really does the column width change.
----------------------------------------------------------------------------------------------*/
bool AfHeaderWnd::ChangeColWidthCond(int icwi, int dxpNew)
{
	if (m_vcwiColInfo[icwi].dxpCurrent == dxpNew)
		return false;
	Assert(dxpNew >= m_vcwiColInfo[icwi].dxpMin);

	ChangeColWidth(icwi, dxpNew);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Method to create a new client window.

	fFlags can be any combination of the following:

	kfColHeadings - Tells the header window to create a column headings window.

	kfEnhanced3DTop - Tells the header window to draw a button face-colored line above
						the caption header to enhance it's top border's 3D appearance.

	kfEtchedColHeadingBrdr - Tells the header window to draw a line (with the 3D highlight
							color) above the column heading window. This gives an etched
							appearance between the column heading window and whatever is
							above it (assuming the bottom of what is above it is the 3D
							shadow color).
----------------------------------------------------------------------------------------------*/
void AfHeaderWnd::Create(Pcsz pszCaption, HIMAGELIST himlCaption, DWORD dwFlags, int wid,
	int dypCaptionHeight, DWORD dwCaptionFlags)
{
	Assert(!m_wid);		// Only call this once.
	AssertPszN(pszCaption);
	m_wid = wid;
	m_fCaption = (pszCaption ? true : false);
	m_strCaption = pszCaption;
	m_himlCaption = himlCaption;
	m_dwCaptionFlags = dwCaptionFlags;
	m_fColHeading = (dwFlags & kfhwcColHeadings);

	// Allow this to be true only if there are column headings.
	m_fEnhancedCol3DTop = ((dwFlags & kfhwcEnhanced3DTop) && m_fColHeading);

	// Allow this to be true only if there is a column headings.
	m_fEtchedColHeadingTop = ((dwFlags & kfhwcEtchedColHeadingTop) && m_fColHeading);

	// If there's supposed to be a caption, then set its height. If the caller didn't
	// pass a height (or passed an invalid height) then set the height to the default
	// value. Otherwise set it to what the caller passed.
	if (m_fCaption)
		m_dypCaptionHeight = (dypCaptionHeight < 0 ?
			kdypStdCaptionBarHeight : dypCaptionHeight);
	else
		m_dypCaptionHeight = 0;

	// Set the top of the column heading so they appear just beneath the caption bar.
	// If there's no caption bar, the column heading is at the top of its owner.
	m_ypTopOfColHeading = m_dypCaptionHeight;

	// If this header window is supposed to have an enhancing line at the top, then bump
	// the column heading down by one pixel.
	if (m_fEnhancedCol3DTop)
		m_ypTopOfColHeading++;

	// If the column heading shows an extra line to produce an etched look between it and
	// the caption bar, then bump the column heading down by one pixel.
	if (m_fEtchedColHeadingTop)
		m_ypTopOfColHeading++;
}

/*----------------------------------------------------------------------------------------------
	Handle notification messages.

	@param ctid Id of the control that issued the windows command.
	@param pnmh Windows command that is being passed.
	@param lnRet return value to be returned to the windows command.
	@return true if command is handled.
	See ${AfWnd#OnNotifyChild}
----------------------------------------------------------------------------------------------*/
bool AfHeaderWnd::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	if (m_hwndColHeading != pnmh->hwndFrom)
		return SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet);

	// Never do this: NMHEADER * pnhdr = (NMHEADER *)pnmh; unless pnmh->code
	// indicates that pmnh is a NMHEADER structure.

	AssertPtr(pnmh);

	static bool s_fTracking = false;
	static bool s_fAllowTrack = false;

	switch (pnmh->code)
	{

	// REVIEW DavidO (DavidO): Don't understand why Darrell did what he did.
	// Come back and try something simpler.
	case HDN_BEGINTRACK:
		{
			NMHEADER * pnhdr = (NMHEADER *)pnmh;
			ColWidthInfo &cwi = m_vcwiColInfo[pnhdr->iItem];
			if (cwi.fSizeableByDrag)
			{
				s_fTracking = true;
				s_fAllowTrack = true;
			}
			else
			{
				lnRet = true;
			}
			return true;
		}

	case HDN_ENDTRACK:
		s_fTracking = false;
		return true;

	case HDN_TRACK:
		// OnHeaderTrack((NMHEADER *)pnmh);
		return true;
	// REVIEW DarrellZ: For some stupid reason, this doesn't ever seem to get called.
	// We should be trapping HDN_TRACK instead of HDN_ITEMCHANGING, so once Microsoft fixes
	// their bug, make the change.
	/*case HDN_TRACK:
		OnHeaderTrack((NMHEADER *)pnmh);
		return true;*/

	case HDN_ITEMCHANGING:
		if (s_fAllowTrack)
		{
			NMHEADER * pnhdr = (NMHEADER *)pnmh;
			// Add up current widths for columns to left of pnhdr->iItem.
			int xpCurrentWidths = 0;
			int iColNumber;
			for (iColNumber = 0; iColNumber < pnhdr->iItem; iColNumber++)
			{
				xpCurrentWidths += m_vcwiColInfo[iColNumber].dxpCurrent;
			}

			// Add up min widths for columns to right of pnhdr->iItem.
			int xpMinWidths = 0;
			for (iColNumber = pnhdr->iItem +1; iColNumber < m_vcwiColInfo.Size(); iColNumber++)
			{
				xpMinWidths += m_vcwiColInfo[iColNumber].dxpMin;
			}

			// Get Width of Window
			Rect rcClient; // area with header border
			::GetClientRect(m_hwnd, &rcClient);
			int dxpMaxWidth = rcClient.Width();

			// Determine the maximum possible width of column pnhdr->iItem.
			dxpMaxWidth -= xpCurrentWidths + xpMinWidths;

			if (pnhdr->pitem->cxy > dxpMaxWidth)
			{
				pnhdr->pitem->cxy = dxpMaxWidth;
			}
			else if (dxpMaxWidth <= 0)
			{
				lnRet = true;
				s_fAllowTrack = s_fTracking;
			}
			else if (pnhdr->pitem->cxy < m_vcwiColInfo[pnhdr->iItem].dxpMin)
			{
				pnhdr->pitem->cxy = m_vcwiColInfo[pnhdr->iItem].dxpMin;
			}
		}
		return true;

	case HDN_ITEMCHANGED:
		if (s_fAllowTrack)
		{
			NMHEADER * pnhdr = (NMHEADER *)pnmh;
			ColWidthInfo &cwi = m_vcwiColInfo[pnhdr->iItem];
			Assert(pnhdr->pitem->mask & HDI_WIDTH);
			int dxpNew = pnhdr->pitem->cxy;
			// The user can not change the last column, the framework automatically
			// generates one of these notifications when the user drags some other column.
			if (dxpNew != cwi.dxpCurrent && pnhdr->iItem < m_vcwiColInfo.Size() - 1)
				cwi.dxpPrefer = dxpNew;
			AdjustColumnWidths(pnhdr->iItem);
		}
		s_fAllowTrack = s_fTracking;
		return true;
	}

	return SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Paint the client area of the window.

	@param hdcDef Always NULL, so ignored.

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfHeaderWnd::OnPaint(HDC hdcDef)
{
	if (!m_fCaption && !m_fColHeading)
		return false;

	SuperClass::OnPaint(hdcDef);

	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps);
	RECT rc(ps.rcPaint);

	rc.top = m_dypCaptionHeight;
	rc.bottom = m_dypCaptionHeight + 1;

	// Draw a dark line above at the top of the header window. This provision is so
	// if there's a caption bar and this window is tucked up to a neighboring window
	// with no visible border, this line gives the caption bar a more distinct 3D
	// border.
	if (m_fEnhancedCol3DTop)
	{
		::FillRect(hdc, &rc, (HBRUSH)(COLOR_3DSHADOW + 1));
		rc.top++;
		rc.bottom++;
	}

	// Draw a light line between the column heading and whatever is above it. This
	// produces an etched look (assuming what's above is the correct color) between
	// the column headings and and what's above it.
	if (m_fEtchedColHeadingTop)
		::FillRect(hdc, &rc, (HBRUSH)(COLOR_3DHIGHLIGHT + 1));

	::EndPaint(m_hwnd, &ps);

	return true;
}

/*----------------------------------------------------------------------------------------------
	When resized, resize the caption and column heading control (if they're visible). Sub
	classes should normally override this method and arrange their contents below
	m_ypTopOfClient.
----------------------------------------------------------------------------------------------*/
bool AfHeaderWnd::OnSize(int wst, int dxp, int dyp)
{
	if (m_fCaption)
		::MoveWindow(m_qcpbr->Hwnd(), 0, 0, dxp, m_dypCaptionHeight, true);

	if (m_fColHeading)
		AdjustColumnWidths();

	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfHeaderWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	// If the user is dragging the top of the caption bar's border then process
	// certain mouse messages related to the sizing action.
	if (m_fDraggingTopCaptionBorder)
	{
		switch(wm)
		{
		// While dragging, make sure the mouse cursor maintains the look of the
		// vertical sizing arrows.
		case WM_MOUSEMOVE:
			::SetCursor(::LoadCursor(NULL, IDC_SIZENS));
			return true;
			break;

		// User has ended sizing operation so stop forcing mouse messages to this window.
		case WM_LBUTTONUP:
			m_fDraggingTopCaptionBorder = false;
			::ReleaseCapture();
			return true;
			break;

		default:
			break;
		}
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Processes events from child windows.

	@param ceid	An event id defined in the ChildEventID enumeration in AfWnd.h
	@param pAfWnd A pointer to the child window sending the event.
	@param lpInfo A pointer to any extra data the child wishes to send its parent.

	@return True by default. The importance of what's returned is up to the child
			sending the event and the parent receiving it.
----------------------------------------------------------------------------------------------*/
bool AfHeaderWnd::OnChildEvent(int ceid, AfWnd *pAfWnd, void *lpInfo)
{
	// If the child that sent us this event is not the caption bar, then do nothing
	// since that's the only child event we care about, at this point.
	if (dynamic_cast<HeaderWndCaptionBar *>(pAfWnd) != m_qcpbr)
		return true;

	// If the caption bar window is calling this because the user is holding down the
	// primary mouse button on the caption bar's top border, then prepare to go into the
	// sizing mode. This means all mouse messages should now be routed through AfHeaderWnd
	// (not AfCaptionBar) until the user releases the primary mouse button.
	if (ceid == kceidStartDraggingBorder)
	{
		m_fDraggingTopCaptionBorder = true;
		::SetCapture(m_hwnd);
		::SetCursor(::LoadCursor(NULL, IDC_SIZENS));
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Initialize the browse view window.
----------------------------------------------------------------------------------------------*/
void AfHeaderWnd::PostAttach(void)
{
	if (m_fCaption)
	{
		// Set the top of the client so it falls just below the caption bar.
		m_ypTopOfClient = m_dypCaptionHeight;
		m_qcpbr.Attach(NewObj HeaderWndCaptionBar);
		m_qcpbr->SetCaptionText(m_strCaption.Chars());
		m_qcpbr->Create(m_hwnd, kwidCaption, m_himlCaption, m_dwCaptionFlags);
	}

	// 10000 seems to be the limit on W9x machines. The header control didn't show up when
	// I (DarrellZ) used values >= SHRT_MAX.
	// Sets the column heading 1 pixel to the right so the borders line up
	Rect rc(1, 0, 10000, 1000);

	if (m_fColHeading)
	{
		// Create the header control
		INITCOMMONCONTROLSEX iccex = { sizeof(iccex), ICC_LISTVIEW_CLASSES };
		::InitCommonControlsEx(&iccex);
		m_hwndColHeading = ::CreateWindowEx(0, WC_HEADER, NULL, WS_CHILD | /*HDS_BUTTONS | */
			HDS_FULLDRAG | HDS_HORZ, 0, 0, 0, 0, m_hwnd, 0, 0, 0);
		::SendMessage(m_hwndColHeading, WM_SETFONT, (WPARAM)::GetStockObject(DEFAULT_GUI_FONT),
			true);

		HDLAYOUT hdl;
		WINDOWPOS wp;
		hdl.prc = &rc;
		hdl.pwpos = &wp;
		::SendMessage(m_hwndColHeading, HDM_LAYOUT, 0, (LPARAM)&hdl);

		// Set the size, position, and visibility of the header control.
		::SetWindowPos(m_hwndColHeading, wp.hwndInsertAfter,
			wp.x, m_ypTopOfColHeading, wp.cx, wp.cy,
			wp.flags | SWP_SHOWWINDOW);

		m_dypColHeadingHeight = wp.cy;

		// Set the top of the client so it falls just below the column headings.
		m_ypTopOfClient = m_ypTopOfColHeading + m_dypColHeadingHeight - 1;
	}

	SuperClass::PostAttach();
}
