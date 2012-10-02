/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfVwWnd.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	A superclass for client windows that consist entirely of a single view.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

BEGIN_CMD_MAP(AfAtlVwScrollWnd)
	ON_CID_ALL(kcidEditCut, &AfAtlVwScrollWnd::CmdEditCut, &AfAtlVwScrollWnd::CmsEditCut)
	ON_CID_ALL(kcidEditCopy, &AfAtlVwScrollWnd::CmdEditCopy, &AfAtlVwScrollWnd::CmsEditCopy)
	ON_CID_ALL(kcidEditPaste, &AfAtlVwScrollWnd::CmdEditPaste, &AfAtlVwScrollWnd::CmsEditPaste)
	//ON_CID_ALL(kcidEditDel, &AfAtlVwScrollWnd::CmdEditDel, &AfAtlVwScrollWnd::CmsEditDel)
	ON_CID_ALL(kcidEditSelAll, &AfAtlVwScrollWnd::CmdEditSelAll, &AfAtlVwScrollWnd::CmsEditSelAll)
	ON_CID_ALL(kcidInsPic, &AfAtlVwScrollWnd::CmdInsertPic, NULL)
	ON_CID_ALL(kcidFmtFnt, &AfAtlVwScrollWnd::CmdFmtFnt, &AfAtlVwScrollWnd::CmsCharFmt)
	ON_CID_ALL(kcidFmtWrtgSys, &AfAtlVwScrollWnd::CmdFmtWrtSys, &AfAtlVwScrollWnd::CmsCharFmt)
	ON_CID_ALL(kcidFmtPara, &AfAtlVwScrollWnd::CmdFmtPara, &AfAtlVwScrollWnd::CmsFmtPara)
	ON_CID_ALL(kcidFmtStyles, &AfAtlVwScrollWnd::CmdFmtStyles, NULL)
	ON_CID_ALL(kcidFmttbStyle, &AfAtlVwScrollWnd::CmdCharFmt, &AfAtlVwScrollWnd::CmsCharFmt)
	ON_CID_ALL(kcidApplyNormalStyle, &AfAtlVwScrollWnd::CmdApplyNormalStyle, NULL)
	ON_CID_ALL(kcidFmttbWrtgSys, &AfAtlVwScrollWnd::CmdCharFmt, &AfAtlVwScrollWnd::CmsCharFmt)
	ON_CID_ALL(kcidFmttbFnt, &AfAtlVwScrollWnd::CmdCharFmt, &AfAtlVwScrollWnd::CmsCharFmt)
	ON_CID_ALL(kcidFmttbFntSize, &AfAtlVwScrollWnd::CmdCharFmt, &AfAtlVwScrollWnd::CmsCharFmt)
	ON_CID_ALL(kcidFmttbBold, &AfAtlVwScrollWnd::CmdCharFmt, &AfAtlVwScrollWnd::CmsCharFmt)
	ON_CID_ALL(kcidFmttbItal, &AfAtlVwScrollWnd::CmdCharFmt, &AfAtlVwScrollWnd::CmsCharFmt)
	ON_CID_ALL(kcidFmttbAlignLeft, &AfAtlVwScrollWnd::CmdCharFmt, &AfAtlVwScrollWnd::CmsCharFmt)
	ON_CID_ALL(kcidFmttbAlignCntr, &AfAtlVwScrollWnd::CmdCharFmt, &AfAtlVwScrollWnd::CmsCharFmt)
	ON_CID_ALL(kcidFmttbAlignRight, &AfAtlVwScrollWnd::CmdCharFmt, &AfAtlVwScrollWnd::CmsCharFmt)
	ON_CID_ALL(kcidFmttbApplyBgrndColor, &AfAtlVwScrollWnd::CmdCharFmt, &AfAtlVwScrollWnd::CmsCharFmt)
	ON_CID_ALL(kcidFmttbApplyFgrndColor, &AfAtlVwScrollWnd::CmdCharFmt, &AfAtlVwScrollWnd::CmsCharFmt)
	ON_CID_ALL(kcidFmtBulNum, &AfAtlVwScrollWnd::CmdFmtBulNum, &AfAtlVwScrollWnd::CmsFmtBulNum)
	ON_CID_ALL(kcidFmtBdr, &AfAtlVwScrollWnd::CmdFmtBdr, &AfAtlVwScrollWnd::CmsFmtBdr)
END_CMD_MAP_NIL()

/***********************************************************************************************
	AfAtlVwScrollWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfAtlVwScrollWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	Assert(!lnRet);

	if (AfVwScrollWndBase::FWndProc(wm, wp, lp, lnRet))
		return true;

	return SuperClass::FWndProc(wm, wp, lp, lnRet); // Pass message on.
}


/*----------------------------------------------------------------------------------------------
	Scroll to the specified offsets.
	Does NOT update the scroll bars. That must already have been done.
	Scrolls down by dxdOffset, right by dydOffset (or reverse if negative).
	That is, they are the amounts being added to the scroll bar position.
----------------------------------------------------------------------------------------------*/
void AfAtlVwScrollWnd::ScrollBy(int dxdOffset, int dydOffset)
{
	try
	{
		int dx = - dxdOffset;
		int dy = - dydOffset;
		if (dx == 0 && dy == 0)
			return; // no change.

		if (dx != 0 && dy != 0)
		{
			// GetScrollRect and ScrollWindow don't handle both directions at once; do a complete redraw.
			::InvalidateRect(m_hwnd, NULL, false);
		}
		else
		{
			Rect rc;
			GetScrollRect(dx, dy, rc);
			// Smoother effect with ScrollWindow.
			::ScrollWindowEx(m_hwnd, dx, dy,
				&rc, // Whole client rectangle
				&rc,  // Clip just to client rectangle--can this be null??
				NULL,	// Don't care what region is invalidated
				NULL,	// Also don't care what rectangle is invalidated
				SW_ERASE + SW_INVALIDATE + SW_SCROLLCHILDREN); // Move child windows automatically.
		}
		::UpdateWindow(m_hwnd);
	}
	catch (...)
	{
		// Nothing we can usefully do, just don't propagate the error.
	}
}


/*----------------------------------------------------------------------------------------------
	Process sizing (WM_SIZE). wst is the type of sizing requested. dxp is the new width, dyp
	is the new height.
----------------------------------------------------------------------------------------------*/
bool AfAtlVwScrollWnd::OnSize(int wst, int dxp, int dyp)
{
	AfVwScrollWndBase::OnSize(wst, dxp, dyp);
	return SuperClass::OnSize(wst, dxp, dyp);
}

void AfAtlVwScrollWnd::GetScrollOffsets(int * pdxd, int * pdyd)
{
	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_POS, 0, 0, 0, 0 };
	m_pwndSubclass->GetScrollInfo(SB_HORZ, &sinfo);
	*pdxd = sinfo.nPos;
	m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);
	*pdyd = sinfo.nPos;
}


/*----------------------------------------------------------------------------------------------
	Classes created by ATL may not have all variables automatically zeroed. So, clear out
	anything that might not be initialized in the parent class, too.
----------------------------------------------------------------------------------------------*/
AfAtlVwScrollWnd::AfAtlVwScrollWnd()
	: AfVwScrollWndBase(this, true)
{
}