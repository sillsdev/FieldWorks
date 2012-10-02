/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: RecMainWndSupportWnds.cpp
Responsibility: Darrell Zook
Last reviewed:

Description:
	This file contains class definitions for the following classes:
		AfListBar : AfWnd
		AfTreeBar : TssTreeView, AfViewBarBase
		AfOverlayListBar : AfListBar
		AfViewBar : AfWnd
		AfViewBarShell : AfWnd
		AfCaptionBar : AfWnd
		AfMdiClientWnd : AfWnd
		AfClientWnd : AfSplitFrame
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	The command map for a generic list bar window.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(AfListBar)
	ON_CID_GEN(kcidVBarLargeIcons, &AfListBar::CmdVbChangeSize, &AfListBar::CmsVbUpdateSize)
	ON_CID_GEN(kcidVBarSmallIcons, &AfListBar::CmdVbChangeSize, &AfListBar::CmsVbUpdateSize)
	ON_CID_GEN(kcidHideVBar, &AfListBar::CmdHideVb, NULL)
END_CMD_MAP_NIL()

BEGIN_CMD_MAP(AfRecListBar)
END_CMD_MAP_NIL()
/*----------------------------------------------------------------------------------------------
	The command map for a generic tree bar window.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(AfTreeBar)
	ON_CID_GEN(kcidHideVBar, &AfTreeBar::CmdHideVb, NULL)
END_CMD_MAP_NIL()


/***********************************************************************************************
	MDI client window code.
***********************************************************************************************/


void AfMdiClientWnd::SetCaptionBar(AfCaptionBar * pcpbr, DWORD dwFlags)
{
	AssertPtr(pcpbr);
	// This should not be called by a subclass if the subclass created its own viewbar.
	Assert(!m_qcpbr);
	Assert(!pcpbr->Hwnd());
	m_qcpbr = pcpbr;
	m_qcpbr->Create(m_hwnd, kwidCaption, m_qrmw->GetImageList(true), dwFlags);
}

/*----------------------------------------------------------------------------------------------
	Get the main window pointer out of the CREATESTRUCT structure.
----------------------------------------------------------------------------------------------*/
void AfMdiClientWnd::PreCreateHwnd(CREATESTRUCT & cs)
{
	Assert(cs.lpCreateParams);
	m_qrmw = (RecMainWnd *)cs.lpCreateParams;
	AssertObj(m_qrmw);
}


/*----------------------------------------------------------------------------------------------
	Adds a new child to the client window. Returns the index of the new child.
----------------------------------------------------------------------------------------------*/
int AfMdiClientWnd::AddChild(AfClientWnd * pafcw)
{
	AssertPtr(pafcw);
	m_vqafcw.Push(pafcw);
	return m_vqafcw.Size() - 1;
}


/*----------------------------------------------------------------------------------------------
	Return the child index associated with the given window id. Returns -1 if not found.
----------------------------------------------------------------------------------------------*/
int AfMdiClientWnd::GetChildIndexFromWid(int wid)
{
	int cwnd = m_vqafcw.Size();

	for (int iwnd = 0; iwnd < cwnd; iwnd++)
	{
		if (m_vqafcw[iwnd]->GetWindowId() == wid)
			return iwnd;
	}

	return -1;
}


/*----------------------------------------------------------------------------------------------
	Returns a pointer to a child window given its index.
----------------------------------------------------------------------------------------------*/
AfClientWnd * AfMdiClientWnd::GetChildFromIndex(int iwnd)
{
	Assert((uint)iwnd < (uint)m_vqafcw.Size());
	return m_vqafcw[iwnd];
}


/*----------------------------------------------------------------------------------------------
	Returns a pointer to a child window given its window id.
----------------------------------------------------------------------------------------------*/
AfClientWnd * AfMdiClientWnd::GetChildFromWid(int wid)
{
	int iwnd = GetChildIndexFromWid(wid);

	if (iwnd < 0)
		return NULL;

	return m_vqafcw[iwnd];
}


/*----------------------------------------------------------------------------------------------
	Delete the child window given its window id.
----------------------------------------------------------------------------------------------*/
void AfMdiClientWnd::DelChildWnd(int wid)
{
	int iwnd = GetChildIndexFromWid(wid);
	if (iwnd == -1)
		return;

	AfClientWndPtr qafcw = m_vqafcw[iwnd];
	if (qafcw == m_qafcwCur)
	{
		if (m_vqafcw.Size() == 1)
			m_qafcwCur.Clear();
		else if (iwnd == m_vqafcw.Size() - 1)
			SetCurChildFromIndex(iwnd - 1);
		else
//			SetCurChildFromIndex(iwnd + 1);
			SetCurChildFromIndex(-1);
	}

	::DestroyWindow(qafcw->Hwnd());
	m_vqafcw.Delete(iwnd, iwnd + 1);
}


/*----------------------------------------------------------------------------------------------
	Delete all the child windows.
----------------------------------------------------------------------------------------------*/
void AfMdiClientWnd::DelAllChildWnds()
{
			for (int iwnd = GetChildCount(); --iwnd >= 0; )
			{
				AfClientWndPtr qafcw = m_vqafcw[iwnd];
				::DestroyWindow(qafcw->Hwnd());
				m_vqafcw.Delete(iwnd, iwnd + 1);
			}
			m_qafcwCur.Clear();
}


/*----------------------------------------------------------------------------------------------
	Set the current child given its index.
	May also pass -1 to (usually temporarily) set it to no window at all.
----------------------------------------------------------------------------------------------*/
bool AfMdiClientWnd::SetCurChildFromIndex(int iwnd)
{
	Assert(iwnd == -1 || (uint)iwnd < (uint)m_vqafcw.Size());

	AfClientWndPtr qafcw;
	if (iwnd >= 0)
		qafcw = m_vqafcw[iwnd];

	if (qafcw == m_qafcwCur)
		return false;

	// Give the current window a chance to error check, save, etc.
	if (m_qafcwCur)
	{
		if (!m_qafcwCur->IsOkToChange())
			return false;
		else
			m_qafcwCur->PrepareToHide();
	}

	if (qafcw && !qafcw->Hwnd())
	{
		WndCreateStruct wcs;
		wcs.InitChild(_T("AfClientWnd"), m_hwnd, qafcw->GetWindowId());
		wcs.style &= ~WS_CLIPCHILDREN;
		qafcw->CreateHwnd(wcs);
	}

	AfClientWndPtr qafcwOld = m_qafcwCur;
	m_qafcwCur = qafcw;

	if (m_qafcwCur)
	{
		Rect rc;
		::GetClientRect(m_hwnd, &rc);
		OnSize(kwstRestored, rc.Width(), rc.Height());
	}

	if (qafcwOld && qafcwOld->Hwnd())
		::ShowWindow(qafcwOld->Hwnd(), SW_HIDE);
	if (m_qafcwCur)
	{
		m_qafcwCur->PrepareToShow();
		::ShowWindow(m_qafcwCur->Hwnd(), SW_SHOW);
		// Only set the focus to the child window if we are the active window.
		if (AfApp::Papp()->GetCurMainWnd() == m_qrmw)
			::SetFocus(m_qafcwCur->Hwnd());
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Set the currently active child given its wid.
----------------------------------------------------------------------------------------------*/
bool AfMdiClientWnd::SetCurChildFromWid(int wid)
{
	int iwnd = GetChildIndexFromWid(wid);

	if (iwnd < 0)
	{
		AssertMsg(false, "Child window not found");
		return false;
	}

	return SetCurChildFromIndex(iwnd);
}


/*----------------------------------------------------------------------------------------------
	Ask the current child window to redraw itself.
----------------------------------------------------------------------------------------------*/
void AfMdiClientWnd::Refresh()
{
	if (m_qafcwCur && m_qafcwCur->Hwnd())
		InvalidateRect(m_qafcwCur->Hwnd(), NULL, false);
}


/*----------------------------------------------------------------------------------------------
	When resized, resize the current child window.
----------------------------------------------------------------------------------------------*/
bool AfMdiClientWnd::OnSize(int wst, int dxp, int dyp)
{
	int ypTop = 0;

	if (m_qcpbr && ::IsWindowVisible(m_qcpbr->Hwnd()))
	{
		::MoveWindow(m_qcpbr->Hwnd(), 0, 0, dxp, kdypStdCaptionBarHeight, true);
		ypTop += kdypStdCaptionBarHeight;
	}

	if (m_qafcwCur)
		::MoveWindow(m_qafcwCur->Hwnd(), 0, ypTop, dxp, dyp - ypTop, true);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfMdiClientWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(!lnRet);

	switch (wm)
	{
	case WM_SETFOCUS:
		// Set the focus to the current child window if there is one.
		if (m_qafcwCur && m_qafcwCur->Hwnd())
			::SetFocus(m_qafcwCur->Hwnd());
		break;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Release smart pointers. This is called from the WM_NCDESTROY message.
----------------------------------------------------------------------------------------------*/
void AfMdiClientWnd::OnReleasePtr()
{
	m_qcpbr.Clear();
	for (int iafcw = m_vqafcw.Size(); --iafcw >= 0; )
		m_vqafcw[iafcw].Clear();
	m_vqafcw.Clear();
	m_qafcwCur.Clear();
	m_qrmw.Clear();
	SuperClass::OnReleasePtr();
}


/***********************************************************************************************
	Default client window code.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Method to create a new client window.
----------------------------------------------------------------------------------------------*/
void AfClientWnd::Create(Pcsz pszView, int imag, int wid)
{
	AssertPsz(pszView);

	m_strName = pszView;
	m_imag = imag;
	m_wid = wid;
}


/*----------------------------------------------------------------------------------------------
	Synchronize this client with any changes made in the database.
	@param sync -> The information describing a given change.
----------------------------------------------------------------------------------------------*/
bool AfClientWnd::Synchronize(SyncInfo & sync)
{
	// At this point do nothing here. Subclasses will normally handle this if interested
	return true;
}


/*----------------------------------------------------------------------------------------------
	Reload and redraw window and child dialogs.
----------------------------------------------------------------------------------------------*/
bool AfClientWnd::FullRefresh()
{
	// At this point do nothing here. Subclasses will normally handle this if interested
	return true;
}


/***********************************************************************************************
	AfOverlayListBar methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	When we change the selection, update the overlay tool windows that are being shown.
----------------------------------------------------------------------------------------------*/
void AfOverlayListBar::SetSelection(Set<int> & sisel)
{
	int iselNone = 0;
	if (sisel.IsMember(iselNone))
	{
		// If the No Overlay item is in the current set of selected items, then we
		// just selected a new item, so remove the No Overlay item from the set.
		if (m_sisel.IsMember(iselNone))
		{
			sisel.Delete(iselNone);
		}
		else
		{
			// Otherwise, we just selected the No Overlay item, so make sure the only
			// item in the set is the No Overlay item.
			sisel.Clear();
			sisel.Insert(iselNone);
		}
	}
	if (sisel.Size() == 0)
	{
		// No other items were selected, so add the No Overlay item.
		sisel.Insert(iselNone);
	}
	SuperClass::SetSelection(sisel);

	Assert(m_hwndOwner);
	int ctot = m_qlpi->GetOverlayCount();
	for (int itot = 0; itot < ctot; itot++)
	{
		// We are looking at itot + 1 because the first item is the No Overlay item.
		int iovrItem = itot + 1;
		m_qlpi->ShowOverlay(itot, this, m_hwndOwner, sisel.IsMember(iovrItem));
	}
}


/*----------------------------------------------------------------------------------------------
	A new overlay is being added to the list bar.
----------------------------------------------------------------------------------------------*/
void AfOverlayListBar::AddItem(const achar * pszName, int imag)
{
	SuperClass::AddItem(pszName, imag);
	m_vrc.Push(Rect(-1));
}


/*----------------------------------------------------------------------------------------------
	Close a specific overlay and update the selection state of its icon in the list bar.
----------------------------------------------------------------------------------------------*/
void AfOverlayListBar::HideOverlay(AfTagOverlayTool * ptot)
{
	AssertPtr(ptot);

	int itot = m_qlpi->GetOverlayIndex(ptot);
	Set<int> sisel;
	_CopySet(m_sisel, sisel);
	// We are looking at itot + 1 because the first item is the No Overlay item.
	int iovrItem = itot + 1;
	sisel.Delete(iovrItem);
	SetSelection(sisel);
}


/*----------------------------------------------------------------------------------------------
	Make all the selected overlays in this list bar invisible.
----------------------------------------------------------------------------------------------*/
void AfOverlayListBar::HideAllOverlays()
{
	Assert(m_hwndOwner);

	int ctot = m_qlpi->GetOverlayCount();
	for (int itot = 0; itot < ctot; itot++)
	{
		AppOverlayInfo & aoi = m_qlpi->GetOverlayInfo(itot);
		if (aoi.m_qtot && aoi.m_qtot->Hwnd())
			::GetWindowRect(aoi.m_qtot->Hwnd(), &m_vrc[itot]);

		m_qlpi->ShowOverlay(itot, this, m_hwndOwner, false);
	}
}


/*----------------------------------------------------------------------------------------------
	Make all the selected overlays in this list bar visible.
----------------------------------------------------------------------------------------------*/
void AfOverlayListBar::ShowAllOverlays()
{
	Assert(m_hwndOwner);

	int ctot = m_qlpi->GetOverlayCount();
	for (int itot = 0; itot < ctot; itot++)
	{
		Rect & rc = m_vrc[itot];
		// We are looking at itot + 1 because the first item is the No Overlay item.
		int iovrItem = itot + 1;
		if (m_sisel.IsMember(iovrItem))
		{
			AfTagOverlayTool * ptot = m_qlpi->GetOverlayInfo(itot).m_qtot;
			if (ptot && ::IsWindowVisible(ptot->Hwnd()))
				continue;
			if (rc.left == -1)
				m_qlpi->ShowOverlay(itot, this, m_hwndOwner, true, NULL);
			else
				m_qlpi->ShowOverlay(itot, this, m_hwndOwner, true, &rc);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Return the string resource ID for when user clicks on the ListBar (away from any specific
	item) requesting What's This help.
----------------------------------------------------------------------------------------------*/
int AfOverlayListBar::GetGeneralWhatsThisHelpId()
{
	return kstidOverlaysGenWhatsThisHelp;
}


/*----------------------------------------------------------------------------------------------
	Return the string resource ID for when user clicks on any item in the ListBar requesting
	What's This help.
----------------------------------------------------------------------------------------------*/
int AfOverlayListBar::GetItemWhatsThisHelpId(int iItem)
{
	if (iItem == 0)
		return kstidOverlaysNoneWhatsThisHelp;
	return kstidOverlaysItemWhatsThisHelp;
}



/***********************************************************************************************
	AfViewBarShell methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
AfViewBarShell::AfViewBarShell()
{
	m_ibtn = -1;
	m_fLbDown = false;
}


/*----------------------------------------------------------------------------------------------
	Create a pager bar shell window and connect it to this. If pvwbr is NULL, a default
	AfViewBar is created. Otherwise, pvwbr will be used as the view bar for this shell.
	NOTE: If pvwbr is not NULL, the Create method should not have been called on it before
	passing it to this method, because this method will call it.
----------------------------------------------------------------------------------------------*/
void AfViewBarShell::Create(HWND hwndPar, int wid, AfViewBar * pvwbr)
{
	Assert(hwndPar);
	AssertPtrN(pvwbr);

	INITCOMMONCONTROLSEX iccex = { isizeof(iccex), ICC_PAGESCROLLER_CLASS | ICC_BAR_CLASSES };
	::InitCommonControlsEx(&iccex);

	WndCreateStruct wcs;
	wcs.InitChild(_T("STATIC"), hwndPar, wid);
	wcs.style |= WS_VISIBLE | SS_NOTIFY;
	wcs.dwExStyle = WS_EX_CLIENTEDGE;
	CreateAndSubclassHwnd(wcs);

	// Create and add the pager bar to the pager bar shell.
	if (pvwbr)
		m_qvwbr = pvwbr;
	else
		m_qvwbr.Create();
	m_qvwbr->Create(m_hwnd, PGS_VERT, wid, this);

	AfApp::Papp()->AddCmdHandler(this, 1, kgrfcmmAll);
}


/*----------------------------------------------------------------------------------------------
	Resize the embedded pager bar.
----------------------------------------------------------------------------------------------*/
bool AfViewBarShell::OnSize(int wst, int dxp, int dyp)
{
	::MoveWindow(m_qvwbr->Hwnd(), 0, kdypButton * (m_qvwbr->GetCurrentList() + 1), dxp,
		dyp - (kdypButton * m_qvwbr->GetListCount()), true);
	::InvalidateRect(m_hwnd, NULL, true);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfViewBarShell::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_LBUTTONDOWN:
		return OnLButtonDown(wp, MakePoint(lp));

	case WM_LBUTTONUP:
		return OnLButtonUp(wp, MakePoint(lp));

	case WM_MOUSEMOVE:
		return OnMouseMove(wp, MakePoint(lp));

	default:
		return SuperClass::FWndProc(wm, wp, lp, lnRet);
	}
}


/*----------------------------------------------------------------------------------------------
	Trap when the user clicks in the view bar shell window.
----------------------------------------------------------------------------------------------*/
bool AfViewBarShell::OnLButtonDown(uint grfmk, Point pt)
{
	// Don't allow this if an editor can't be closed.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);
	if (!prmw->IsOkToChange())
		return false;

	m_fLbDown = true;
	::SetCapture(m_hwnd);

	return OnMouseMove(grfmk, pt);
}


/*----------------------------------------------------------------------------------------------
	Select the new list based on the mouse position.
----------------------------------------------------------------------------------------------*/
bool AfViewBarShell::OnLButtonUp(uint grfmk, Point pt)
{
	// Make sure the user clicked in the viewbar shell instead of clicking somewhere else and
	// dragging into the viewbar shell before releasing the mouse button.
	if (!m_fLbDown)
		return true;

	m_fLbDown = false;
	::ReleaseCapture();

	if (m_ibtn != -1)
		SetCurrentList(m_ibtn);
	m_ibtn = -1;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Figure out which list is selected by a click at the specified point (in client coords).
	Return -1 if no list is selected.
----------------------------------------------------------------------------------------------*/
int AfViewBarShell::GetListIndexFromPoint(Point pt)
{
	Rect rc;
	GetClientRect(rc);
	if (!::PtInRect(&rc, pt))
		return -1;
	int cbtn = m_qvwbr->GetListCount();
	int ibtnCur = m_qvwbr->GetCurrentList();
	int ibtn;
	int dypOffset = kdypButton;
	for (ibtn = 0; ibtn < cbtn; ibtn++)
	{
		if (ibtn == ibtnCur)
		{
			Rect rcList;
			::GetWindowRect(m_qvwbr->Hwnd(), &rcList);
			dypOffset += rcList.Height();
		}
		if (pt.y < dypOffset)
			return ibtn;
		dypOffset += kdypButton;
	}

	return -1; // Don't think we can get here, but make compiler happy.
}

/*----------------------------------------------------------------------------------------------
	Depress the view button associated with the current mouse position.
----------------------------------------------------------------------------------------------*/
bool AfViewBarShell::OnMouseMove(uint grfmk, Point pt)
{
	if (!m_fLbDown)
		return true;

	// Find out which group the mouse is currently over.
	int ibtn = GetListIndexFromPoint(pt);

	if (ibtn != m_ibtn)
		::InvalidateRect(m_hwnd, NULL, true);
	m_ibtn = ibtn;
	::UpdateWindow(m_hwnd);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Return a string containing the help string for the icon at the specified point.

	@param pt Screen location of a mouse click.
	@param pptss Address of a pointer to an ITsString COM object for returning the help string.

	@return True if successful, false if no help string is available for the given screen
					location.
----------------------------------------------------------------------------------------------*/
bool AfViewBarShell::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	Point ptClient(pt);
	::ScreenToClient(m_hwnd, &ptClient);

	int ibtn = GetListIndexFromPoint(ptClient);
	if (ibtn < 0)
		return false; // can this happen? Play safe.
	AfViewBarBase * pvbb = m_qvwbr->GetList(ibtn);

	int stid = pvbb->GetGeneralWhatsThisHelpId();
	StrUni stuMsg(stid);
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	CheckHr(qtsf->MakeString(stuMsg.Bstr(), MainWindow()->UserWs(), pptss));
	return true;
}

/*----------------------------------------------------------------------------------------------
	Draw the buttons.
----------------------------------------------------------------------------------------------*/
bool AfViewBarShell::OnPaint(HDC hdcDef)
{
	Assert(!hdcDef); // REVIEW DarrellZ: Do we want to handle painting to another DC?

	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps);

	Rect rc;
	GetClientRect(rc);
	rc.bottom = rc.top + kdypButton;

	Rect rcList;
	::GetWindowRect(m_qvwbr->Hwnd(), &rcList);

	COLORREF clr = ::GetSysColor(COLOR_3DFACE);
	int ibtnCur = m_qvwbr->GetCurrentList();

	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
	::SetBkColor(hdc, clr);

	// Draw the buttons for each group.
	int cbtn = m_qvwbr->GetListCount();
	for (int ibtn = 0; ibtn < cbtn; ibtn++)
	{
		AfGfx::FillSolidRect(hdc, rc, clr);
		AfViewBarBase * pvbb = m_qvwbr->GetList(ibtn);
		::DrawText(hdc, pvbb->GetName(), -1, &rc, DT_SINGLELINE | DT_CENTER | DT_VCENTER |
			DT_END_ELLIPSIS);
		::DrawEdge(hdc, &rc, m_fLbDown && ibtn == m_ibtn ? BDR_SUNKENOUTER : BDR_RAISEDINNER,
			BF_RECT);
		if (ibtn == ibtnCur) // Skip the height of the AfViewBar window.
			rc.Offset(0, rcList.Height());
		rc.Offset(0, rc.Height());
	}

	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);

	::EndPaint(m_hwnd, &ps);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Release smart pointers. This is called from the WM_NCDESTROY message.
----------------------------------------------------------------------------------------------*/
void AfViewBarShell::OnReleasePtr()
{
	m_qvwbr.Clear();
	AfApp::Papp()->RemoveCmdHandler(this, 1);
	SuperClass::OnReleasePtr();
}


/*----------------------------------------------------------------------------------------------
	Change the list/group that is currently showing.
----------------------------------------------------------------------------------------------*/
void AfViewBarShell::SetCurrentList(int ilist)
{
// Changing this to one gives an animated effect to changing the view bars. However,
// on some machines it is inconsistent, running real slowly at times. David Coward and
// Marlon Hovland had the biggest problems. This problem needs to be solved before
// enabling it again.
#if 0
	Rect rc;
	GetClientRect(rc);
	int dypList = rc.Height() - (kdypButton * m_qvwbr->GetListCount());
	int ilistOld = GetCurrentList();

	::SendMessage(m_hwnd, WM_SETREDRAW, false, 0);

	// Move the pager window to the proper spot.
	::MoveWindow(m_qvwbr->Hwnd(), 0, kdypButton * (ilist + 1), rc.Width(),
		dypList, true);

	m_qvwbr->SetCurrentList(ilist);

	if (ilistOld != -1 && ilist != ilistOld)
	{
		Rect rcList;
		m_qvwbr->GetClientRect(rcList);
		Rect rcReplace;
		rcReplace = rcList;
		if (ilist < ilistOld)
		{
			rcReplace.bottom += kdypButton * (ilistOld - ilist);
			rcReplace.Offset(0, kdypButton * (ilist + 1));
		}
		else
		{
			rcReplace.bottom += kdypButton * (ilist - ilistOld);
			rcReplace.Offset(0, kdypButton * (ilistOld + 1));
		}
		int dxpMem = rc.Width();
		int dypMem = rcReplace.Height() + rcList.Height();
		int dypMemReplace = rcReplace.Height();
		int dypList = rcList.Height();

		// Animate the changing lists.
		// Create a memory DC to draw everything on.
		HDC hdc = ::GetDC(m_hwnd);
		if (!hdc)
			ThrowHr(WarnHr(E_FAIL));
		// Get a snapshot of the current list.
		HDC hdcMem = AfGdi::CreateCompatibleDC(hdc);
		HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, dxpMem, dypMem);
		HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
		if (ilist < ilistOld)
			::BitBlt(hdcMem, 0, dypList, dxpMem, dypMemReplace, hdc, 0, rcReplace.top, SRCCOPY);
		else
			::BitBlt(hdcMem, 0, 0, dxpMem, dypMemReplace, hdc, 0, rcReplace.top, SRCCOPY);

		// Draw the new (not yet visible) list in the empty space.
		HFONT hfont = ::GetStockObject(DEFAULT_GUI_FONT);
		HFONT hfontOld = AfGdi::SelectObjectFont(hdcMem, hfont);
		int ypNewOrg = rcList.top;
		if (ilist > ilistOld)
			ypNewOrg += rcReplace.Height();

		::SendMessage(m_hwnd, WM_SETREDRAW, true, 0);

		::SetViewportOrgEx(hdcMem, 0, ypNewOrg, NULL);
		::SendMessage(m_qvwbr->GetList(ilist)->Hwnd(), WM_PAINT, (WPARAM)hdcMem, 0);
		::SetViewportOrgEx(hdcMem, 0, 0, NULL);

		// Slide the window.
		const int kdypSlide = 200;
		DWORD dwNow = timeGetTime();
		DWORD dwStop = dwNow + kdypSlide;
		/*StrAnsi sta;
		sta.Format("Slide start: now = %d, stop = %d\n", dwNow, dwStop);
		::OutputDebugString(sta.Chars());*/
		int ypSrc;
		while (dwNow <= dwStop)
		{
			ypSrc = (dwStop - dwNow) * dypList / kdypSlide;
			if (ilist > ilistOld)
				ypSrc = dypList - ypSrc;
			::BitBlt(hdc, 0, rcReplace.top, dxpMem, dypMemReplace, hdcMem, 0, ypSrc, SRCCOPY);
			dwNow = timeGetTime();
			/*sta.Format("time = %d\n", dwNow);
			::OutputDebugString(sta.Chars());*/
		}
		// Move it to the exact final position.
		::BitBlt(hdc, 0, rcReplace.top, dxpMem, dypMemReplace, hdcMem, 0, ilist < ilistOld ? 0 : dypList, SRCCOPY);
		/*sta.Format("Finished: time = %d\n", timeGetTime());
		OutputDebugString(sta.Chars());*/

		// Clean up everything.
		HFONT hfontDebug;
		hfontDebug = AfGdi::SelectObjectFont(hdcMem, hfontOld, AfGdi::OLD);
		Assert(hfontDebug && hfontDebug != HGDI_ERROR);
		Assert(hfontDebug == hfont);
		// Do not need to DeleteObject hfont since it is a default fault

		HBITMAP hbmpDebug;
		hbmpDebug = AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);
		Assert(hbmpDebug && hbmpDebug != HGDI_ERROR);
		Assert(hbmpDebug == hbmp);

		BOOL fSuccess;
		fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
		Assert(fSuccess);

		fSuccess = AfGdi::DeleteDC(hdcMem);
		Assert(fSuccess);

		int iSuccess;
		iSuccess = ::ReleaseDC(m_hwnd, hdc);
		Assert(iSuccess);
	}
	::SendMessage(m_hwnd, WM_SETREDRAW, true, 0);
	::InvalidateRect(m_hwnd, NULL, true);
#else
	Rect rc;
	GetClientRect(rc);
	int dypList = rc.Height() - (kdypButton * m_qvwbr->GetListCount());
	// Move the pager window to the proper spot.
	::MoveWindow(m_qvwbr->Hwnd(), 0, kdypButton * (ilist + 1), rc.Width(),
		dypList, true);
	m_qvwbr->SetCurrentList(ilist);
	::InvalidateRect(m_hwnd, NULL, true);
#endif
}


/*----------------------------------------------------------------------------------------------
	Show or hide the viewbar.
----------------------------------------------------------------------------------------------*/
void AfViewBarShell::ShowViewBar(bool fShow)
{
	::ShowWindow(m_hwnd, fShow ? SW_SHOW : SW_HIDE);
	// Why are we resizing the main window here? It causes a crash in FullRefresh in CLE because
	// the Doc window has not been fully initialized yet when it tries to redraw things.
	// ----------------------------------------------------------------------------------------
	// We need the resize in order for the neighboring window to expand or contract when the
	// viewbar disappears or appears.  (SteveMc, August 29, 2002)
	::SendMessage(::GetParent(m_hwnd), WM_SIZE, kwstRestored, 0);
}


/***********************************************************************************************
	AfViewBar methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfViewBar::AfViewBar()
{
	m_ilistCur = -1;
}


/*----------------------------------------------------------------------------------------------
	Create a pager bar window and connect it to this.
----------------------------------------------------------------------------------------------*/
void AfViewBar::Create(HWND hwndPar, int style, int wid, AfViewBarShell * pvwbrs)
{
	Assert(hwndPar);
	AssertPtr(pvwbrs);

	WndCreateStruct wcs;
	wcs.InitChild(WC_PAGESCROLLER, hwndPar, wid);
	wcs.style |= WS_VISIBLE | style;
	wcs.style &= ~WS_CLIPCHILDREN;

	CreateAndSubclassHwnd(wcs);

	Pager_SetButtonSize(m_hwnd, 15);

	m_qvwbrs = pvwbrs;
}


/*----------------------------------------------------------------------------------------------
	Add a new list to the view bar.
----------------------------------------------------------------------------------------------*/
void AfViewBar::AddList(const achar * pszName, HIMAGELIST himlLarge,
	HIMAGELIST himlSmall, bool fMultiple, AfListBar * plstbr)
{
	AssertPsz(pszName);
	Assert(himlLarge && himlSmall);
	AssertPtrN(plstbr);

	// Create and add the listbar to the pager bar.
	AfListBarPtr qlstbr;
	if (plstbr)
		qlstbr = plstbr;
	else
		qlstbr.Create();
	qlstbr->Create(m_hwnd, m_vpvbb.Size() + 1, pszName, himlLarge, himlSmall, fMultiple,
		m_qvwbrs);

	// Add the listbar to the pager bar.
	AfViewBarBase * pvbb = qlstbr;
	m_vpvbb.Push(pvbb);
	m_viScrollPos.Push(0);
}


/*----------------------------------------------------------------------------------------------
	Add a new tree to the view bar.
----------------------------------------------------------------------------------------------*/
int AfViewBar::AddTree(const achar * pszName, HIMAGELIST himl,
	DWORD dwStyle, AfTreeBar * ptrbr)
{
	AssertPsz(pszName);
	AssertPtrN(ptrbr);

	// Create and add the treebar to the pager bar.
	AfTreeBarPtr qtrbr;
	if (ptrbr)
		qtrbr = ptrbr;
	else
		qtrbr.Create();
	qtrbr->Create(m_hwnd, m_vpvbb.Size() + 1, pszName, himl, dwStyle, m_qvwbrs);

	// Add the treebar to the pager bar.
	AfViewBarBase * pvbb = qtrbr;
	m_vpvbb.Push(pvbb);
	m_viScrollPos.Push(0);

	return (m_vpvbb.Size() - 1);
}


/*----------------------------------------------------------------------------------------------
	Delete an item from an existing list.
----------------------------------------------------------------------------------------------*/
void AfViewBar::DeleteListItem(int ilist, const achar * pszName)
{
	AssertPsz(pszName);
	Assert((uint)ilist < (uint)m_vpvbb.Size());

	AfListBar * plstbr = dynamic_cast<AfListBar *>(m_vpvbb[ilist]);
	AssertPtr(plstbr);
	plstbr->DeleteItem(pszName);
	Pager_RecalcSize(m_hwnd);
}


/*----------------------------------------------------------------------------------------------
	Add a new item to an existing list.
----------------------------------------------------------------------------------------------*/
void AfViewBar::AddListItem(int ilist, const achar * pszName, int imag)
{
	AssertPsz(pszName);
	Assert((uint)ilist < (uint)m_vpvbb.Size());

	AfListBar * plstbr = dynamic_cast<AfListBar *>(m_vpvbb[ilist]);
	AssertPtr(plstbr);
	plstbr->AddItem(pszName, imag);
	Pager_RecalcSize(m_hwnd);
}


/*----------------------------------------------------------------------------------------------
	Handle notifications.
----------------------------------------------------------------------------------------------*/
bool AfViewBar::OnNotifyThis(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	AfListBar * plstbr = dynamic_cast<AfListBar *>(m_vpvbb[m_ilistCur]);
	AssertPtrN(plstbr);
	if (!plstbr)
		return false;

	if (PGN_CALCSIZE == pnmh->code)
	{
		NMPGCALCSIZE * pnmpcs = (NMPGCALCSIZE *)pnmh;
		if (PGF_CALCHEIGHT == pnmpcs->dwFlag)
		{
			pnmpcs->iHeight = plstbr->CalcSize();
			return true;
		}
	}
	else if (PGN_SCROLL == pnmh->code)
	{
		NMPGSCROLL * pnmps = (NMPGSCROLL *)pnmh;
		// Find out how many icons will fit, then scroll by one less than that.
		int dypItem = plstbr->IsShowingLargeIcons() ? AfListBar::kdypLargeImages : AfListBar::kdypSmallImages;
		int cItems = (pnmps->rcParent.bottom - pnmps->rcParent.top) / dypItem;
		if (!cItems)
			++cItems;
		pnmps->iScroll = cItems * dypItem;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Resize the listbar inside.
----------------------------------------------------------------------------------------------*/
bool AfViewBar::OnSize(int wst, int dxp, int dyp)
{
	::MoveWindow(m_vpvbb[m_ilistCur]->GetHwnd(), 0, 0, dxp, dyp, true);
	return false;
}


/*----------------------------------------------------------------------------------------------
	Release smart pointers. This is called from the WM_NCDESTROY message.
----------------------------------------------------------------------------------------------*/
void AfViewBar::OnReleasePtr()
{
/*	int clstbr = m_vpvbb.Size();
	for (int ilstbr = 0; ilstbr < clstbr; ilstbr++)
		ReleaseObj(m_vpvbb[ilstbr]);*/
	m_vpvbb.Clear();
	m_qvwbrs.Clear();
	SuperClass::OnReleasePtr();
}


/*----------------------------------------------------------------------------------------------
	Clear the list bar so that it can be reloaded.
----------------------------------------------------------------------------------------------*/
void AfViewBar::Clear()
{
	int clstbr = m_vpvbb.Size();
	for (int ilstbr = 0; ilstbr < clstbr; ilstbr++)
	{
		::DestroyWindow(m_vpvbb[ilstbr]->GetHwnd());
		//m_vpvbb[ilstbr].Clear();
	}
	m_vpvbb.Clear();
	m_viScrollPos.Clear();
	m_ilistCur = -1;
}


/*----------------------------------------------------------------------------------------------
	Set the currently visible list.
----------------------------------------------------------------------------------------------*/
void AfViewBar::SetCurrentList(int ilist)
{
	Assert((uint)ilist < (uint)m_vpvbb.Size());
	if (m_ilistCur == ilist)
		return;

	HWND hwndNew =  m_vpvbb[ilist]->GetHwnd();
	::ShowWindow(hwndNew, SW_SHOW);

	if (m_ilistCur != -1)
	{
		m_viScrollPos[m_ilistCur] = Pager_GetPos(m_hwnd);
		::ShowWindow(m_vpvbb[m_ilistCur]->GetHwnd(), SW_HIDE);
	}
	m_ilistCur = ilist;
	Pager_SetChild(m_hwnd, m_vpvbb[ilist]->GetHwnd());
	Pager_SetPos(m_hwnd, m_viScrollPos[ilist]);
	// The following InvalidateRect and UpdateWindow is required to repaint the tree if it has
	// more thatn one font used in it. For some reason, it does not repaint correctly unless
	// you do UpdateWindow twice.
	::UpdateWindow(hwndNew);
	::InvalidateRect(hwndNew, NULL, FALSE);
	::UpdateWindow(hwndNew);
}


/*----------------------------------------------------------------------------------------------
	The selection has changed in the list bar.
----------------------------------------------------------------------------------------------*/
bool AfViewBar::OnSelChanged(AfViewBarBase * pvbb, Set<int> & siselOld, Set<int> & siselNew)
{
	AssertObj(dynamic_cast<AfWnd *>(pvbb));

	// Pass the message on to the main window.
	int clist = m_vpvbb.Size();
	for (int ilist = 0; ilist < clist; ilist++)
	{
		if (m_vpvbb[ilist] == pvbb)
		{
			RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
			AssertObj(prmw);
			return prmw->OnViewBarChange(ilist, siselOld, siselNew);
		}
	}

	Assert(false); // This should never happen.
	return false;
}


/***********************************************************************************************
	AfViewBarBase methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Change the selection of items in the list. The new selection will be exactly sisel, not
	the current selection plus the items in sisel.
----------------------------------------------------------------------------------------------*/
void AfViewBarBase::SetSelection(Set<int> & sisel)
{
	::InvalidateRect(GetHwnd(), NULL, false);
	Set<int> siselT;
	_CopySet(m_sisel, siselT);
	_CopySet(sisel, m_sisel);

	AfViewBarPtr qvwbr = m_qvwbrs->GetViewBar();
	AssertObj(qvwbr);
	qvwbr->OnSelChanged(this, siselT, sisel);
}


/*----------------------------------------------------------------------------------------------
	This method copies items from one set to a new set.
	TODO SteveMc: Shouldn't this be a method (i.e., copy constructor) on the Set template?
----------------------------------------------------------------------------------------------*/
void AfViewBarBase::_CopySet(Set<int> & siselFrom, Set<int> & siselTo)
{
	siselTo.Clear();
	Set<int>::iterator it = siselFrom.Begin();
	Set<int>::iterator itStop = siselFrom.End();
	while (it != itStop)
	{
		int isel = *it;
		siselTo.Insert(isel);
		++it;
	}
}


/***********************************************************************************************
	AfListBar methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Construction
----------------------------------------------------------------------------------------------*/
AfListBar::AfListBar()
{
	m_fLBDown = false;
	m_fHilight = true;
	m_fShowLargeIcons = true;
	m_himlLarge = NULL;
	m_himlSmall = NULL;
	m_iCurSel = m_iContextSel = -1;
	m_fMultipleSel = false;
	m_fContextMenuVisible = false;
}

/*----------------------------------------------------------------------------------------------
	Create a list bar.
----------------------------------------------------------------------------------------------*/
void AfListBar::Create(HWND hwndPar, int wid, const achar * pszName, HIMAGELIST himlLarge,
	HIMAGELIST himlSmall, bool fMultiple, AfViewBarShell * pvwbrs)
{
	Assert(hwndPar);
	Assert(himlLarge);
	Assert(himlSmall);
	AssertPtr(pvwbrs);

	WndCreateStruct wcs;
	wcs.InitChild(_T("LISTBOX"), hwndPar, wid);
	wcs.style |= WS_CHILD | WS_VISIBLE | WS_TABSTOP | LBS_OWNERDRAWVARIABLE |
		LBS_NOINTEGRALHEIGHT | LBS_HASSTRINGS;

	CreateAndSubclassHwnd(wcs);

	::SendMessage(m_hwnd, WM_SETFONT, (WPARAM)GetStockObject(DEFAULT_GUI_FONT), 0);

	// Initialize the image lists.
	m_himlLarge = himlLarge;
	m_himlSmall = himlSmall;

	m_staName = pszName;
	m_qvwbrs = pvwbrs;
	m_fMultipleSel = fMultiple;

	AfApp::Papp()->AddCmdHandler(this, 1, kgrfcmmAll);
}


/*----------------------------------------------------------------------------------------------
	Add an item to the list bar.
----------------------------------------------------------------------------------------------*/
void AfListBar::AddItem(const achar * pszName, int imag)
{
	AssertPsz(pszName);

	int iitem = ::SendMessage(m_hwnd, LB_ADDSTRING, 0, (long)pszName);
	::SendMessage(m_hwnd, LB_SETITEMDATA, iitem, imag);
}


/*----------------------------------------------------------------------------------------------
	Remove an item from the list bar.
----------------------------------------------------------------------------------------------*/
void AfListBar::DeleteItem(const achar * pszName)
{
	int iItem = ::SendMessage(m_hwnd, LB_FINDSTRINGEXACT, (WPARAM)-1, (long)pszName);
	::SendMessage(m_hwnd, LB_DELETESTRING, iItem, 0);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfListBar::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_ERASEBKGND:
		return true;
	case WM_LBUTTONDOWN:
		OnLButtonDown(wp, Point(LOWORD(lp), HIWORD(lp)));
		return true;
	case WM_LBUTTONUP:
		OnLButtonUp(wp, Point(LOWORD(lp), HIWORD(lp)));
		return true;
	case WM_RBUTTONDOWN:
		m_iContextSel = m_iCurSel;
		return true;
	case WM_MOUSEMOVE:
		OnMouseMove(wp, Point(LOWORD(lp), HIWORD(lp)));
		return true;
	case WM_TIMER:
		OnTimer(wp);
		return true;
	default:
		return SuperClass::FWndProc(wm, wp, lp, lnRet);
	}
}


/*----------------------------------------------------------------------------------------------
	Switches the view to the mode where it shows small or large icons based on fLarge.
----------------------------------------------------------------------------------------------*/
void AfListBar::ChangeIconSize(bool fLarge)
{
	int dypHeight;
	if (fLarge == m_fShowLargeIcons)
		return;

	m_fShowLargeIcons = fLarge;
	if (fLarge)
		dypHeight = kdypLargeImages;
	else
		dypHeight = kdypSmallImages;

	int citems = ::SendMessage(m_hwnd, LB_GETCOUNT, 0, 0);
	for (int i = 0; i < citems; ++i)
		::SendMessage(m_hwnd, LB_SETITEMHEIGHT, i, dypHeight);
	::InvalidateRect(m_hwnd, NULL, false);

	Pager_RecalcSize(::GetParent(m_hwnd));
}


/*----------------------------------------------------------------------------------------------
	Checks mouse movement and sets timer.
----------------------------------------------------------------------------------------------*/
void AfListBar::OnMouseMove(UINT nFlags, POINT pt)
{
	if (m_fContextMenuVisible)
		return;

	m_fHilight = true;
	::SetTimer(m_hwnd, 1, 100, NULL);

	int iItem = GetItemFromPoint(pt);
	if (iItem != m_iCurSel)
	{
		m_iCurSel = iItem;
		::InvalidateRect(m_hwnd, NULL, false);
	}
}


/*----------------------------------------------------------------------------------------------
	Sets m_fLBDown member to true, and sets current selection
----------------------------------------------------------------------------------------------*/
void AfListBar::OnLButtonDown(UINT nFlags, POINT pt)
{
	// Don't allow this if an editor can't be closed.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);
	if (!prmw->IsOkToChange())
		return;

	m_fLBDown = true;
	::SetCapture(m_hwnd);

	int iItem = GetItemFromPoint(pt);
	if (iItem != m_iCurSel)
		m_iCurSel = iItem;
	::InvalidateRect(m_hwnd, NULL, false);
}


/*----------------------------------------------------------------------------------------------
	Sets m_fLBDown member to false, and sets current selection
----------------------------------------------------------------------------------------------*/
void AfListBar::OnLButtonUp(UINT nFlags, POINT pt)
{
	// Make sure the user clicked in the listbar instead of clicking somewhere else and
	// dragging into the listbar before releasing the mouse button.
	if (!m_fLBDown)
		return;

	m_fLBDown = false;
	::ReleaseCapture();
	if (m_iCurSel == -1)
		return;

	// If the cursor is outside of the window, remove the current selection.
	Rect rcWindow;
	::GetWindowRect(m_hwnd, &rcWindow);
	::ClientToScreen(m_hwnd, &pt);
	if (::PtInRect(&rcWindow, pt))
	{
		Set<int> sisel;
		if (m_fMultipleSel)
		{
			_CopySet(m_sisel, sisel);
			if (sisel.IsMember(m_iCurSel))
			{
				// Remove it from the set of selected items.
				sisel.Delete(m_iCurSel);
			}
			else
			{
				// Add it to the set of selected items.
				sisel.Insert(m_iCurSel);
			}
		}
		else
		{
			sisel.Insert(m_iCurSel);
		}
		SetSelection(sisel);
	}
}


/*----------------------------------------------------------------------------------------------
	Returns the index of the item from the given point. If no item exists at the given point,
	-1 is returned.
----------------------------------------------------------------------------------------------*/
int AfListBar::GetItemFromPoint(POINT pt)
{
	int nT = ::SendMessage(m_hwnd, LB_ITEMFROMPOINT, 0, MAKELPARAM(pt.x, pt.y));
	if (HIWORD(nT) == 0)
	{
		// We are inside the client area of the list box.
		int iItem = LOWORD(nT);
		Rect rc;
		::SendMessage(m_hwnd, LB_GETITEMRECT, iItem, (LPARAM)&rc);
		if (::PtInRect(&rc, pt))
		{
			// We are within the rectangle for the item, so return its index.
			return iItem;
		}
	}

	return -1;
}


/*----------------------------------------------------------------------------------------------
	Sets the hilite flag based on cursor position.
----------------------------------------------------------------------------------------------*/
void AfListBar::OnTimer(UINT nIDEvent)
{
	AfApp::Papp()->SuppressIdle();

	if (m_fContextMenuVisible)
		return;

	RECT rcWindow;
	POINT pt;

	m_fHilight = false;

	// Get the rect of the current window & the current cursor position.
	::GetWindowRect(m_hwnd, &rcWindow);
	::GetCursorPos(&pt);

	// If the mouse is in the rect of this window, set hilite flag to true, get the item that
	// the mouse is over.
	if (::PtInRect(&rcWindow, pt))
	{
		POINT ptClient = pt;
		::ScreenToClient(m_hwnd, &ptClient);
		int iItem = GetItemFromPoint(ptClient);
		if (iItem != -1)
		{
			m_fHilight = true;
			if (iItem != m_iCurSel)
			{
				m_iCurSel = iItem;
				::InvalidateRect(m_hwnd, NULL, false);
			}
		}
	}
	if (!m_fHilight)
	{
		// If the mouse is not in the rect of this window, kill the timer, set hilite flag
		// to false, and return.
		if (m_iCurSel != -1)
		{
			m_iCurSel = -1;
			::InvalidateRect(m_hwnd, NULL, false);
		}
		::KillTimer(m_hwnd, 1);
	}
}

/*----------------------------------------------------------------------------------------------
	Return a string containing the help string for the icon at the specified point.

	@param pt Screen location of a mouse click.
	@param pptss Address of a pointer to an ITsString COM object for returning the help string.

	@return True if successful, false if no help string is available for the given screen
					location.
----------------------------------------------------------------------------------------------*/
bool AfListBar::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	Point ptClient(pt);
	::ScreenToClient(m_hwnd, &ptClient);

	int iItem = GetItemFromPoint(ptClient);
	if (iItem < 0)
		return false;

	StrUni stuMsg(GetItemWhatsThisHelpId(iItem));
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	CheckHr(qtsf->MakeString(stuMsg.Bstr(), MainWindow()->UserWs(), pptss));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Return the string resource ID for when user clicks on the ListBar (away from any specific
	item) requesting What's This help.
	This virtual function should be defined for any derived class, as this version will return
	a string indicating an error.
----------------------------------------------------------------------------------------------*/
int AfListBar::GetGeneralWhatsThisHelpId()
{
	return kstidViewBarShellChooseList;
}


/*----------------------------------------------------------------------------------------------
	Return the string resource ID for when user clicks on any item in the ListBar requesting
	What's This help.
	This virtual function should be defined for any derived class, as this version will return
	a string indicating an error.
----------------------------------------------------------------------------------------------*/
int AfListBar::GetItemWhatsThisHelpId(int)
{
	return kstidListBarSelect;
}


/*----------------------------------------------------------------------------------------------
	Show the context menu for the pager bar.
----------------------------------------------------------------------------------------------*/
bool AfListBar::OnContextMenu(HWND hwnd, Point pt)
{
	HMENU hmenuPopup = ::CreatePopupMenu();

	StrApp str;
	AfUtil::GetResourceStr(krstItem, kcidVBarLargeIcons, str);
	::AppendMenu(hmenuPopup, MF_STRING, kcidVBarLargeIcons, str.Chars());
	AfUtil::GetResourceStr(krstItem, kcidVBarSmallIcons, str);
	::AppendMenu(hmenuPopup, MF_STRING, kcidVBarSmallIcons, str.Chars());
	::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);

	int cid = GetConfigureCid();
	if (cid)
	{
		AfUtil::GetResourceStr(krstItem, cid, str);
		::AppendMenu(hmenuPopup, MF_STRING, cid, str.Chars());
		::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
	}

	AfUtil::GetResourceStr(krstItem, kcidHideVBar, str);
	::AppendMenu(hmenuPopup, MF_STRING, kcidHideVBar, str.Chars());

	m_fContextMenuVisible = true;
	TrackPopupWithHelp(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.x, pt.y,
		MainWindow()->UserWs());
	m_fContextMenuVisible = false;

	::DestroyMenu(hmenuPopup);
	// Comands to fix: kcidViewViewsConfig, kcidViewFltrsConfig, kcidViewSortsConfig, kcidViewOlaysConfig
	return true;
}


/*----------------------------------------------------------------------------------------------
	Initial draw routine, draws button on mouse over, on mouse press, and on mouse out.
----------------------------------------------------------------------------------------------*/
bool AfListBar::OnDrawThisItem(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);
	Assert((uint)pdis->itemID < (uint)::SendMessage(m_hwnd, LB_GETCOUNT, 0, 0));

	// Device context for drawing
	HDC hdc = pdis->hDC;
	SmartPalette spal(hdc);

	HIMAGELIST himl;
	int dzsMargin = 3; // Margin between image and button.
	int dysTopMargin;
	DWORD dtFlags = DT_END_ELLIPSIS | DT_SINGLELINE;

	// Retrieve the rectangle for the entire list item (image and text and margins).
	Rect rcItem(pdis->rcItem);
	Rect rcButton;
	Rect rcText;
	POINT ptImage;
	int dzsIcon;

	if (m_fShowLargeIcons)
	{
		// The button is the upper portion of this rectangle. It should allow a small margin
		// larger than that needed to display the icon. It is positioned dysTopMargin down
		// from the top of the rcItem, in order to allow some spacing between items.
		dzsIcon = 32;
		dysTopMargin = 7;
		himl = m_himlLarge;
		int dzsButton = dzsIcon + (dzsMargin * 2);
		POINT ptButtonCenter = { (rcItem.right - rcItem.left) / 2,
			rcItem.top + dzsIcon / 2 + dzsMargin + dysTopMargin };
		rcButton.left = ptButtonCenter.x - dzsButton / 2;
		rcButton.top = ptButtonCenter.y - dzsButton / 2;
		rcButton.bottom = rcButton.top + dzsButton;
		rcButton.right = rcButton.left + dzsButton;

		// The position to display the icon is the top-left corner of the image.
		ptImage.x = ptButtonCenter.x - dzsIcon / 2;
		ptImage.y = ptButtonCenter.y - dzsIcon / 2;

		// Calculate the text rectangle. It is positioned just below the button.
		rcText = rcItem;
		rcText.top += dzsButton + dysTopMargin + dzsMargin;

		dtFlags |= DT_CENTER | DT_WORDBREAK;
	}
	else
	{
		// The button is the left portion of this rectangle. It should allow a small margin
		// larger than that needed to display the icon. It is positioned dysTopMargin down from
		// the top of the rcItem, in order to allow some spacing between items.
		dzsIcon = 16;
		dysTopMargin = 4;
		himl = m_himlSmall;
		int dzsButton = dzsIcon + (dzsMargin * 2);
		rcButton.left = rcItem.left + dzsMargin;
		rcButton.top = rcItem.top + dysTopMargin;
		rcButton.bottom = rcButton.top + dzsButton;
		rcButton.right = rcButton.left + dzsButton;

		// The position to display the icon is the top-left corner of the image.
		ptImage.x = rcButton.left + dzsMargin;
		ptImage.y = rcButton.top + dzsMargin;

		// Calculate the text rectangle. It is positioned just right of the button.
		rcText = rcItem;
		rcText.top += dysTopMargin + dzsMargin + 2;
		rcText.left = rcButton.right + dzsMargin;
	}

	// Retrieve the client window data that we will display.
	int iItem = pdis->itemID;
	achar rgch[MAX_PATH];
	Assert(::SendMessage(m_hwnd, LB_GETTEXTLEN, iItem, 0) + 1 < isizeof(rgch));
	::SendMessage(m_hwnd, LB_GETTEXT, iItem, (LPARAM)rgch);
	int imag = ::SendMessage(m_hwnd, LB_GETITEMDATA, iItem, 0);
	Assert((uint)imag < (uint)ImageList_GetImageCount(himl));

	// Set the text color to white, and background mode to transparent.
	::SetTextColor(hdc, ::GetSysColor(COLOR_WINDOW));
	::SetBkMode(hdc, TRANSPARENT);

	// Simple case: just draw the icon and the text (no selection, etc.)
	// REVIEW: This condition and the instructions to execute if true appear to be irrelevant,
	// as the same code is executed regardless, at the end of this method.
	if (pdis->itemAction & ODA_DRAWENTIRE)
	{
		ImageList_DrawEx(himl, imag, hdc, ptImage.x, ptImage.y, dzsIcon, dzsIcon, CLR_NONE,
			CLR_NONE, ILD_NORMAL);
		::DrawText(hdc, rgch, -1, &rcText, dtFlags);
	}

	int isel = pdis->itemID;

	AfGfx::FillSolidRect(hdc, rcItem, ::GetSysColor(COLOR_3DSHADOW));

	if (m_sisel.IsMember(isel))
	{
		::DrawEdge(hdc, &rcButton, BDR_SUNKENINNER, BF_RECT);
	}
	else if (isel == m_iCurSel)
	{
		if (m_fLBDown)
			::DrawEdge(hdc, &rcButton, BDR_SUNKENINNER, BF_RECT);
		else
			::DrawEdge(hdc, &rcButton, BDR_RAISEDOUTER, BF_RECT);
	}
	ImageList_DrawEx(himl, imag, hdc, ptImage.x, ptImage.y, dzsIcon, dzsIcon, CLR_NONE,
		CLR_NONE, ILD_NORMAL);
	::DrawText(hdc, rgch, -1, &rcText, dtFlags);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Sets item height.
----------------------------------------------------------------------------------------------*/
bool AfListBar::OnMeasureThisItem(MEASUREITEMSTRUCT * pmis)
{
	AssertPtr(pmis);

	pmis->itemHeight = m_fShowLargeIcons ? kdypLargeImages : kdypSmallImages;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handles the WM_PAINT message. Uses an offscreen dc to fill the rectangle and send to
	DefWindowProc, in order to reduce flickering.
----------------------------------------------------------------------------------------------*/
bool AfListBar::OnPaint(HDC hdc)
{
	SmartDc sdc(m_hwnd);
	Rect rc;
	GetClientRect(rc);

	HDC hdcMem;
	HBITMAP hbmp;
	HBITMAP hbmpOld;
	if (hdc)
	{
		hdcMem = hdc;
		//::InvalidateRect(m_hwnd, NULL, true);
	}
	else
	{
		hdcMem = AfGdi::CreateCompatibleDC(sdc);
		hbmp = AfGdi::CreateCompatibleBitmap(sdc, rc.Width(), rc.Height());
		hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
	}

	AfGfx::FillSolidRect(hdcMem, rc, ::GetSysColor(COLOR_BTNSHADOW));

	// The MeasureItem/DrawItem methods should be called.
	DefWndProc(WM_PAINT, (WPARAM)hdcMem, 0);

	if (!hdc)
	{
		::BitBlt(sdc, rc.left, rc.top, rc.Width(), rc.Height(), hdcMem, 0, 0, SRCCOPY);

		HBITMAP hbmpDebug;
		hbmpDebug = AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);
		Assert(hbmpDebug && hbmpDebug != HGDI_ERROR);
		Assert(hbmpDebug == hbmp);

		BOOL fSuccess;
		fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
		Assert(fSuccess);

		fSuccess = AfGdi::DeleteDC(hdcMem);
		Assert(fSuccess);
	}

	::ValidateRect(m_hwnd, NULL);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Release smart pointers. This is called from the WM_NCDESTROY message.
----------------------------------------------------------------------------------------------*/
void AfListBar::OnReleasePtr()
{
	m_qvwbrs.Clear();
	AfApp::Papp()->RemoveCmdHandler(this, 1);
	SuperClass::OnReleasePtr();
}


/*----------------------------------------------------------------------------------------------
	Change the size of the icons in the view bar.
----------------------------------------------------------------------------------------------*/
bool AfListBar::CmdVbChangeSize(Cmd * pcmd)
{
	AssertObj(pcmd);
	ChangeIconSize(pcmd->m_cid == kcidVBarLargeIcons);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Set the state of the view bar toolbar/menu item.
----------------------------------------------------------------------------------------------*/
bool AfListBar::CmsVbUpdateSize(CmdState & cms)
{
	bool fLarge = IsShowingLargeIcons();
	cms.SetCheck(cms.Cid() == kcidVBarLargeIcons ? fLarge : !fLarge);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Toggle the visibility of the list bar.
----------------------------------------------------------------------------------------------*/
bool AfListBar::CmdHideVb(Cmd * pcmd)
{
	AssertObj(pcmd);

	m_qvwbrs->ShowViewBar(false);
	return true;
}


/***********************************************************************************************
	AfRecListBar methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Return the 'Configure' command ID for the context menu.
---------------------------------------------------------------------------------------------*/
int AfRecListBar::GetConfigureCid()
{
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(AfApp::Papp()->GetCurMainWnd());
	Assert(prmw);
	int ivblt = prmw->GetViewbarListType(m_itype);

	switch (ivblt)
	{
	case kvbltView:
		return kcidViewViewsConfig;
	case kvbltFilter:
		return kcidViewFltrsConfig;
	case kvbltSort:
		return kcidViewSortsConfig;
	}
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Return the string resource ID for when user clicks on the ListBar (away from any specific
	item) requesting What's This help.
----------------------------------------------------------------------------------------------*/
int AfRecListBar::GetGeneralWhatsThisHelpId()
{
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(AfApp::Papp()->GetCurMainWnd());
	Assert(prmw);
	int ivblt = prmw->GetViewbarListType(m_itype);

	switch (ivblt)
	{
	case kvbltTree:
		return kstidListGenWhatsThisHelp;
	case kvbltView:
		return kstidViewsGenWhatsThisHelp;
	case kvbltFilter:
		return kstidFiltersGenWhatsThisHelp;
	case kvbltSort:
		return kstidSortGenWhatsThisHelp;
	case kvbltOverlay:
		return kstidOverlaysGenWhatsThisHelp;
	default:
		Assert(false);
	}
	return kstidViewBarShellChooseList;
}


/*----------------------------------------------------------------------------------------------
	Return the string resource ID for when user clicks on any item in the ListBar requesting
	What's This help.
----------------------------------------------------------------------------------------------*/
int AfRecListBar::GetItemWhatsThisHelpId(int iItem)
{
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(AfApp::Papp()->GetCurMainWnd());
	Assert(prmw);
	int ivblt = prmw->GetViewbarListType(m_itype);

	switch (ivblt)
	{
	case kvbltTree:
		return kstidListItemWhatsThisHelp;
	case kvbltView:
		return kstidViewsItemWhatsThisHelp;
	case kvbltFilter:
		if (iItem == 0)
			return kstidFiltersNoneWhatsThisHelp;
		return kstidFiltersItemWhatsThisHelp;
	case kvbltSort:
		return kstidSortItemWhatsThisHelp;
	case kvbltOverlay:
		if (iItem == 0)
			return kstidOverlaysNoneWhatsThisHelp;
		return kstidOverlaysItemWhatsThisHelp;
	default:
		Assert(false);
	}
	return kstidListBarSelect;
}

/***********************************************************************************************
	AfCaptionBar methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfCaptionBar::AfCaptionBar()
{
	m_himl = NULL;
	m_hfont = AfGdi::CreateFont(20, 0, 0, 0, 700, false, false, 0, ANSI_CHARSET,
		OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY,
		DEFAULT_PITCH | FF_SWISS, _T("Arial"));
	m_hwndToolTip = NULL;
	m_fNeedToRecalc = true;
	m_fEnhanced3DTop = false;
	m_dzsBorderThickness = kdzsBorderThickness;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfCaptionBar::~AfCaptionBar()
{
	if (m_hfont)
	{
		AfGdi::DeleteObjectFont(m_hfont);
		m_hfont = NULL;
	}
#ifdef JohnT_1_21_2002_Invalid
	// It appears that Windows destroys child tooltip windows automatically.
	// At least, BoundsChecker complains that m_hwndToolTip is invalid.
	if (m_hwndToolTip)
	{
		::DestroyWindow(m_hwndToolTip);
		m_hwndToolTip = NULL;
	}
#endif
}


/*----------------------------------------------------------------------------------------------
	Create a caption bar window and connect it to this.
----------------------------------------------------------------------------------------------*/
void AfCaptionBar::Create(HWND hwndPar, int wid, HIMAGELIST himl, DWORD dwFlags)
{
	Assert(hwndPar);

	WndCreateStruct wcs;
	wcs.InitChild(_T("STATIC"), hwndPar, wid);
	wcs.style |= WS_CHILD | WS_VISIBLE | SS_NOTIFY | SS_OWNERDRAW;

	CreateAndSubclassHwnd(wcs);
	m_himl = himl;

	m_hwndToolTip = ::CreateWindowEx(0, TOOLTIPS_CLASS, NULL, TTS_ALWAYSTIP | TTS_NOPREFIX,
		0, 0, 0, 0, m_hwnd, 0, 0, NULL);

	// If this flag is true it means an added dark line is added above the caption bar
	// to enhance the 3D effect of its top edge. When the caption bar is tucked up against
	// the top of its parent window's sunken 3D border (which is probably the case most of
	// the time) a true value for this flag isn't really necessary since the parent's
	// border provides the necessary line to make the caption bar look good. This flag is
	// used mainly when the caption bar is tucked up against a borderless window.
	m_fEnhanced3DTop = (dwFlags & kfcbcEnhance3DTopCaptionBrdr);

	// True will show the NS arrow mouse cursor when moving over the caption's top border.
	// It will also send mouse move messages to the parent if the user is dragging the top
	// border of the caption. It's up to the parent to perform the vertical sizing.
	m_fCaptionSizesParent = (dwFlags & kfcbcAllowCaptionToSizeParent);

	// True will send mouse move messages to the parent if the user is dragging the
	// caption bar. It's up to the parent to preform the move.
	m_fCaptionMovesParent = (dwFlags & kfcbcAllowCaptionToMoveParent);

	// True will cause focus to be forwarded to the caption bar's parent when the
	// user clicks on the caption bar.
	m_fNotifyParentOnClick = (dwFlags & kfcbcNotifyParentOnClick);

	// By default, the caption's background color is the same as the button shadow color
	// but there may be times (like when a caption's owner has focus) when the implementor
	// wants to change it. Therefore, it's stored in a variable and changed via
	// SetCaptionBackColor.
	m_clrCaptionBackColor = ::GetSysColor(COLOR_BTNSHADOW);
}

/*----------------------------------------------------------------------------------------------
	Add another button to the caption bar. Returns the index of the new button.
----------------------------------------------------------------------------------------------*/
int AfCaptionBar::AddIcon(int imag)
{
	m_vimag.Push(imag);
	StrAnsi sta;
	m_vstaText.Push(sta); // Add an empty string to our tooltip vector. It will get set later.
	return m_vimag.Size() - 1;
}

/*----------------------------------------------------------------------------------------------
	This allows an implementor of a caption bar to override the default font created in the
	caption bar's constructor.
----------------------------------------------------------------------------------------------*/
HFONT const AfCaptionBar::SetCaptionFont(LOGFONT * plf)
{
	// Make sure that if a font has already been created it
	// gets destroyed before being recreated.
	if (m_hfont)
		AfGdi::DeleteObjectFont(m_hfont);

	m_hfont = AfGdi::CreateFontIndirect(plf);
	return m_hfont;
}
/*----------------------------------------------------------------------------------------------
	Change the text that is displayed.
----------------------------------------------------------------------------------------------*/
void AfCaptionBar::SetCaptionText(Pcsz pszCaption)
{
	AssertPsz(pszCaption);
	m_strCaption = pszCaption;
	::InvalidateRect(m_hwnd, NULL, false);
}

/*----------------------------------------------------------------------------------------------
	Change the icon that is displayed.
----------------------------------------------------------------------------------------------*/
void AfCaptionBar::SetIconImage(int iicon, int imag)
{
	Assert((uint)iicon < (uint)m_vimag.Size());
	m_vimag[iicon] = imag;
	m_vstaText[iicon].Clear();

	::InvalidateRect(m_hwnd, NULL, false);
}


/*----------------------------------------------------------------------------------------------
	Go through each of the icons, recompute their sizes and locations, and add tools for each
	of them to the tooltip control that is embedded in the caption bar.
----------------------------------------------------------------------------------------------*/
void AfCaptionBar::RecalcToolTip()
{
	m_fNeedToRecalc = false;

	// Don't bother doing anything if there's no image list.
	if (m_himl == NULL)
		return;

	TOOLINFO ti = { isizeof(ti) };
	ti.hwnd = m_hwnd;
	ti.lpszText = LPSTR_TEXTCALLBACK;

	// Clean out all the old tools.
	int ctool = ::SendMessage(m_hwndToolTip, TTM_GETTOOLCOUNT, 0, 0);
	for (int itool = 0; itool < ctool; itool++)
	{
		ti.uId = itool;
		::SendMessage(m_hwndToolTip, TTM_DELTOOL, 0, (LPARAM)&ti);
	}

	// Get the size of one of the images.
	int dxpImage;
	int dypImage;
	ImageList_GetIconSize(m_himl, &dxpImage, &dypImage);
	// If this is bigger than kdzpIcon, then the icon will not display properly.
	Assert(dxpImage <= kdzpIcon);

	Rect rc;
	GetClientRect(rc);
	Rect rcTool(rc.right - dxpImage - kdxpOffset, rc.top + (rc.Height() - dypImage) / 2);
	rcTool.right = rcTool.left + dxpImage;
	rcTool.bottom = rcTool.top + dypImage;

	// Add the new tools.
	for (int ibtn = m_vimag.Size(); --ibtn >= 0; )
	{
		ti.rect = rcTool;
		ti.uId = ibtn;
		::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);
		rcTool.Offset(-(dxpImage + kdxpMargin), 0);
	}
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfCaptionBar::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (m_hwndToolTip && wm != WM_DESTROY && wm != WM_NCDESTROY)
	{
		// Forward messages to the tool tip window (except WM_DESTROY...BoundsChecker says
		// the tooltip is an invalid handle by that time, probably Windows has destroyed
		// it automatically as a child window).
		MSG msg = { m_hwnd, wm, wp, lp };
		::SendMessage(m_hwndToolTip, TTM_RELAYEVENT, 0, (LPARAM)&msg);
	}

	Point pt = MakePoint(lp);

	// Check if this caption's parent wants to be sized vertically and if the mouse is
	// within the proper top border margin to show the vertical sizing mouse cursor.
	if (m_fCaptionSizesParent && pt.y <= kdzpBorderWidthForSizingCursor)
	{
		switch (wm)
		{
		// The mouse is over the top border so show the vertical window sizing cursor.
		case WM_MOUSEMOVE:
			::SetCursor(::LoadCursor(NULL, IDC_SIZENS));
			return true;
			break;

		// Tell the caption's parent window to get ready to size itself.
		case WM_LBUTTONDOWN:
			(AfWnd *)Parent()->OnChildEvent(kceidStartDraggingBorder, (AfWnd *)this);
			return true;
			break;

		default:;
		}
	}
	else if (m_fNotifyParentOnClick &&
		(wm == WM_LBUTTONDOWN || wm == WM_RBUTTONDOWN || wm == WM_MBUTTONDOWN))
	{
		MSG msg;
		msg.hwnd = Hwnd();
		msg.message = wm;
		msg.wParam = wp;
		msg.lParam = lp;
		msg.pt = pt;
		(AfWnd *)Parent()->OnChildEvent(kfcbcNotifyParentOnClick, (AfWnd *)this, &msg);
		return true;
	}

	if (wm == WM_LBUTTONUP || wm == WM_MBUTTONUP || wm == WM_RBUTTONUP)
	{
		// Find out which icon the mouse is over (if any) at the right side of the window.
		Point pt = MakePoint(lp);
		Point ptTopLeft;
		int ibtn = ButtonFromPoint(pt, &ptTopLeft);
		if (ibtn != -1)
		{
			::ClientToScreen(m_hwnd, &ptTopLeft);
			ShowContextMenu(ibtn, ptTopLeft);
			return true;
		}
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Force a redraw of the window when it is resized.
----------------------------------------------------------------------------------------------*/
bool AfCaptionBar::OnSize(int wst, int dxp, int dyp)
{
	::InvalidateRect(m_hwnd, NULL, false);
	::UpdateWindow(m_hwnd);
	RecalcToolTip();
	return false;
}

/*----------------------------------------------------------------------------------------------
	Set the caption's background color.

	@param clr Color to set the captions background to.
	@param fRedraw Flag indicating weather or not to redraw caption after setting its color.
----------------------------------------------------------------------------------------------*/
void AfCaptionBar::SetCaptionBackColor(COLORREF clr, bool fRedraw)
{
	if (clr >= 0)
		m_clrCaptionBackColor = clr;

	if (fRedraw)
		::InvalidateRect(m_hwnd, NULL, false);
}

/*----------------------------------------------------------------------------------------------
	Set the caption's border thickness.

	@param clr Color to set the captions background to.
	@param fRedraw Flag indicating weather or not to redraw caption after setting its color.
----------------------------------------------------------------------------------------------*/
void AfCaptionBar::SetCaptionBorderWidth(int dzsWidth, bool fRedraw)
{
	if (dzsWidth >= 0)
		m_dzsBorderThickness = dzsWidth;

	if (fRedraw)
		::InvalidateRect(m_hwnd, NULL, false);
}

/*----------------------------------------------------------------------------------------------
	Return the index of the button that is under pt (in client coordinates).
	Returns -1 if pt does not correspond to any button.
----------------------------------------------------------------------------------------------*/
int AfCaptionBar::ButtonFromPoint(POINT pt, POINT * pptTopLeft)
{
	AssertPtrN(pptTopLeft);

	// Get the size of one of the images.
	int dxpImage;
	int dypImage;
	ImageList_GetIconSize(m_himl, &dxpImage, &dypImage);

	Rect rc;
	::GetWindowRect(m_hwnd, &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);

	int dxpImageWithMargin = dxpImage + kdxpMargin;
	int dxpIcons = dxpImageWithMargin * m_vimag.Size();

//	Rect rcIcon(rc.right - kdzsBorderThickness - dxpIcons,
//		rc.top + (rc.Height() - dypImage) / 2, 0, 0);

	Rect rcIcon(rc.right - m_dzsBorderThickness - dxpIcons,
		rc.top + (rc.Height() - dypImage) / 2, 0, 0);

	rcIcon.right = rcIcon.left + dxpIcons - kdxpMargin;
	rcIcon.bottom = rcIcon.top + dypImage;

	if (::PtInRect(&rcIcon, pt))
	{
		// The user clicked inside of the areas where the icons are being drawn, so now find
		// out which icon the user clicked on.
		int ibtn = (pt.x - rcIcon.left) / dxpImageWithMargin;
		if (pptTopLeft)
			*pptTopLeft = Point(rcIcon.left + ibtn * dxpImageWithMargin, rcIcon.bottom);
		return ibtn;
	}
	return -1;
}


/*----------------------------------------------------------------------------------------------
	Handle window notification messages.
----------------------------------------------------------------------------------------------*/
bool AfCaptionBar::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	if (pnmh->code == TTN_GETDISPINFO)
	{
		// Get the tooltip text.
		NMTTDISPINFO * pttdi = (NMTTDISPINFO *)pnmh;
		pttdi->hinst = NULL;
		Assert((uint)pnmh->idFrom < (uint)m_vstaText.Size());
		StrApp & str = m_vstaText[pnmh->idFrom];
		if (str.Length() == 0)
		{
			// The tooltip string for this button hasn't been cached yet, so get it now.
			GetIconName(pnmh->idFrom, str);
		}
		pttdi->lpszText = NULL;
		if (str.Length() > 0)
			pttdi->lpszText = const_cast<achar *>(str.Chars());
		return true;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Draw the text and icon(s).
----------------------------------------------------------------------------------------------*/
bool AfCaptionBar::OnDrawThisItem(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);

	HDC hdc = pdis->hDC;

	if (m_fNeedToRecalc)
		RecalcToolTip();

	// Get the client rect for this control.
	Rect rc;
	GetClientRect(rc);

	HDC hdcMem;
	hdcMem = AfGdi::CreateCompatibleDC(hdc);
	Assert(hdcMem);
	HBITMAP hbmp;
	hbmp = AfGdi::CreateCompatibleBitmap(hdc, rc.Width(), rc.Height());
	Assert(hbmp);
	HBITMAP hbmpOld;
	hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
	Assert(hbmpOld);

	// Paint the background
	::SetBkMode(hdcMem, TRANSPARENT);
	AfGfx::FillSolidRect(hdcMem, rc, ::GetSysColor(COLOR_BTNFACE));

	// If caller wants the extra COLOR_BTNFACE line to enhance the caption bar's
	// 3D top border, then draw the edge one pixel below the top since the top
	// row of pixels needs to be left alone (i.e. left with a color of COLOR_BTNFACE).
	// The DrawEdge will give the top of the rectangle a light row of pixels.
	if (m_fEnhanced3DTop)
		rc.top++;

	::DrawEdge(hdcMem, &rc, BDR_RAISEDINNER, BF_RECT);

	// Paint the surrounding border
	Rect rcBackGnd(rc);
	::InflateRect(&rcBackGnd, -m_dzsBorderThickness, -m_dzsBorderThickness);
	AfGfx::FillSolidRect(hdcMem, rcBackGnd, m_clrCaptionBackColor);

	int dxpIcons = 0;

	// Draw the image(s) to the right
	if (NULL != m_himl && m_vimag.Size() != 0)
	{
		// Get the size of one of the images.
		int dxpImage;
		int dypImage;
		ImageList_GetIconSize(m_himl, &dxpImage, &dypImage);
		// If this is bigger than kdzpIcon, then the icon will not display properly.
		Assert(dxpImage <= kdzpIcon);

		Point ptImage(rc.right - dxpImage - kdxpOffset, rc.top + (rc.Height() - dypImage) / 2);
		for (int ibtn = m_vimag.Size(); --ibtn >= 0; )
		{
			ImageList_Draw(m_himl, m_vimag[ibtn], hdcMem, ptImage.x, ptImage.y, ILD_NORMAL);
			ptImage.x -= dxpImage + kdxpMargin;
		}

		dxpIcons = (dxpImage + kdxpMargin) * m_vimag.Size();
	}

	// Draw the text
	Rect rcText(rc);
	::InflateRect(&rcText, -2, -2);
	rcText.left += kdxpOffset;
	rcText.right -= kdxpOffset + dxpIcons;
	::SetTextColor(hdcMem, ::GetSysColor(COLOR_WINDOW));
	HFONT hfontOld = AfGdi::SelectObjectFont(hdcMem, m_hfont);
	::DrawText(hdcMem, m_strCaption.Chars(), m_strCaption.Length(), &rcText,
		DT_LEFT | DT_VCENTER | DT_SINGLELINE | DT_END_ELLIPSIS);
	AfGdi::SelectObjectFont(hdcMem, hfontOld, AfGdi::OLD);

	// Before displaying the memory bitmap, check to see if the rectangle's top needs
	// to be bumped up by one since it could have been bumped down by one above. Not
	// doing this results in only part of the rectangle getting written.
	if (m_fEnhanced3DTop)
		rc.top--;

	::BitBlt(hdc, rc.left, rc.top, rc.Width(), rc.Height(), hdcMem, 0, 0, SRCCOPY);

	HBITMAP hbmpDebug;
	hbmpDebug = AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);
	Assert(hbmpDebug && hbmpDebug != HGDI_ERROR);
	Assert(hbmpDebug == hbmp);

	BOOL fSuccess;
	fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
	Assert(fSuccess);

	fSuccess = AfGdi::DeleteDC(hdcMem);
	Assert(fSuccess);

	return true;
}

/***********************************************************************************************
	AfRecCaptionBar methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfRecCaptionBar::AfRecCaptionBar(RecMainWnd * prmwMain)
{
	AssertPtr(prmwMain);
	m_prmwMain = prmwMain;
}


/*----------------------------------------------------------------------------------------------
	Create one of our caption bars that has an icon for each item in the view bar.

	@param hwndPar handle of parent window
	@param wid window id
	@param himl handle to captionbar imagelist
----------------------------------------------------------------------------------------------*/
void AfRecCaptionBar::Create(HWND hwndPar, int wid, HIMAGELIST himl, DWORD dwFlags)
{
	AssertPtr(m_prmwMain);
	Assert(hwndPar);
	Assert(himl);
	SuperClass::Create(hwndPar, wid, himl, dwFlags);

	// Add buttons.
	m_prmwMain->SetCaptionBarIcons(this);
}


/*----------------------------------------------------------------------------------------------
	Get the text that represents this icon. It will be shown in a tooltip.

	@param ibtn captionbar index for the button
	@param str Out Icon Name of button
----------------------------------------------------------------------------------------------*/
void AfRecCaptionBar::GetIconName(int ibtn, StrApp & str)
{
	str.Clear();

	AfViewBarShell * pvwbrs = m_prmwMain->GetViewBarShell();
	AssertPtr(pvwbrs);
	Set<int> sisel;
	pvwbrs->GetSelection(ibtn, sisel);

	AfLpInfo * plpi = m_prmwMain->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	AssertPtr(m_prmwMain);
	int ivblt = m_prmwMain->GetViewbarListType(ibtn);

	switch (ivblt)
	{
	case kvbltTree:
		// TODO
		str = "Tree List";
		break;
	case kvbltView:
		{
			Assert(sisel.Size() == 1);
			int iview = *sisel.Begin();
			AfClientWnd * pafcw = m_prmwMain->GetMdiClientWnd()->GetChildFromIndex(iview);
			AssertPtr(pafcw);
			str = pafcw->GetViewName();
		}
		break;
	case kvbltFilter:
		{
			Assert(sisel.Size() == 1);
			int iflt = pdbi->ComputeFilterIndex(*sisel.Begin(), m_prmwMain->GetRecordClid());
			if (iflt < 0)
			{
				str.Load(kstidNoFilter);
			}
			else
			{
				AppFilterInfo & afi = pdbi->GetFilterInfo(iflt);
				str = afi.m_stuName;
			}
		}
		break;
	case kvbltSort:
		{
			Assert(sisel.Size() == 1);
			int isrt = pdbi->ComputeSortIndex(*sisel.Begin(), m_prmwMain->GetRecordClid());
			if (isrt < 0)
			{
				str.Load(kstidDefaultSort);
			}
			else
			{
				AppSortInfo & asi = pdbi->GetSortInfo(isrt);
				str = asi.m_stuName;
			}
		}
		break;
	case kvbltOverlay:
		{
			Assert(sisel.Size() >= 1);
			Set<int>::iterator sit = sisel.Begin();
			Set<int>::iterator sitLim = sisel.End();
			if (*sit == 0)
			{
				str.Load(kstidNoOverlay);
			}
			else
			{
				StrUni stu;
				bool fFirst = true;
				int iovr = *sit - 1;
				while (++sit != sitLim)
				{
					if (!fFirst)
						stu.Append(L", ");
					AppOverlayInfo & aoi = plpi->GetOverlayInfo(iovr);
					stu.Append(aoi.m_stuName);
					fFirst = false;
					iovr = *sit - 1;
				}

				if (sisel.Size() > 1)
					stu.Append(L" and ");
				AppOverlayInfo & aoi = plpi->GetOverlayInfo(iovr);
				stu.Append(aoi.m_stuName);

				str = stu;
			}
		}
		break;
	default:
		Assert(false); // Should never happen.
		break;
	}
}


/*----------------------------------------------------------------------------------------------
	Show help information for the tree item that the mouse is over.
	@param pt Point relative to the screen.
	@param pptss Pointer to receive the help string.
	@return True if the mouse is over a field label and we are returning a string.
		False if we aren't over a field label.
----------------------------------------------------------------------------------------------*/
bool AfRecCaptionBar::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	::ScreenToClient(m_hwnd, &pt);
	Point ptTopLeft;
	int ibtn = ButtonFromPoint(pt, &ptTopLeft);
	if (ibtn == -1)
		return false;
	int ivblt = m_prmwMain->GetViewbarListType(ibtn);

	StrApp str;
	switch (ivblt)
	{
	case kvbltTree:
		str.Load(kstidCaptionBarTree);
		break;
	case kvbltView:
		str.Load(kstidCaptionBarViews);
		break;
	case kvbltFilter:
		str.Load(kstidCaptionBarFilters);
		break;
	case kvbltSort:
		str.Load(kstidCaptionBarSort);
		break;
	case kvbltOverlay:
		str.Load(kstidCaptionBarOverlay);
		break;
	default:
		return false;
	}

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(str);
	CheckHr(qtsf->MakeString(stu.Bstr(), m_prmwMain->UserWs(), pptss));
	return true;
}

/***********************************************************************************************
	AfTreeBar methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Construction
----------------------------------------------------------------------------------------------*/
AfTreeBar::AfTreeBar()
{
}


/*----------------------------------------------------------------------------------------------
	Create a tree bar.
----------------------------------------------------------------------------------------------*/
void AfTreeBar::Create(HWND hwndPar, int wid, const achar * pszName, HIMAGELIST himl,
		DWORD dwStyle, AfViewBarShell * pvwbrs)
{
	Assert(hwndPar);
	AssertPtr(pvwbrs);

	// Create the tree.
	WndCreateStruct wcs;
	wcs.InitChild(WC_TREEVIEW, hwndPar, wid);
	wcs.style = dwStyle;

	CreateAndSubclassHwnd(wcs);

	::SendMessage(m_hwnd, WM_SETFONT, (WPARAM)GetStockObject(DEFAULT_GUI_FONT), 0);

	// Initialize the image lists.
	m_himl = himl;

	m_staName = pszName;
	m_qvwbrs = pvwbrs;

	AfApp::Papp()->AddCmdHandler(this, 1, kgrfcmmAll);
}


/*----------------------------------------------------------------------------------------------
	Return the string resource ID for when user clicks on the TreeBar (away from any specific
	item) requesting What's This help.
----------------------------------------------------------------------------------------------*/
int AfTreeBar::GetGeneralWhatsThisHelpId()
{
	return kstidListGenWhatsThisHelp;
}


/*----------------------------------------------------------------------------------------------
	Return the string resource ID for when user clicks on any item in the TreeBar requesting
	What's This help.
----------------------------------------------------------------------------------------------*/
int AfTreeBar::GetItemWhatsThisHelpId(int iItem)
{
	return kstidListItemWhatsThisHelp;
}


/*----------------------------------------------------------------------------------------------
	Toggle the visibility of the tree bar.
----------------------------------------------------------------------------------------------*/
bool AfTreeBar::CmdHideVb(Cmd * pcmd)
{
	AssertObj(pcmd);

	m_qvwbrs->ShowViewBar(false);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Delegate this to the containing AfSplitterClientWnd; a subclass of that should have
	implemented it.
----------------------------------------------------------------------------------------------*/
void AfSubClientSplitterWnd::CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew)
{
	m_pafscwParent->CreateChild(psplcCopy, psplcNew);
}

/*----------------------------------------------------------------------------------------------
	Delegate this to the containing AfSplitterClientWnd; a subclass of that may have
	overridden it, expecting to override the method on the spliter.
	@fChkReq True if we should also check Required/Encouraged data. False otherwise.
	@return True if it is OK to close the DE window.
----------------------------------------------------------------------------------------------*/
bool AfSubClientSplitterWnd::IsOkToChange(bool fChkReq)
{
	return m_pafscwParent->IsOkToChange(fChkReq);
}

/*----------------------------------------------------------------------------------------------
	Set up the contents of the window. In this case create the child splitter window.
----------------------------------------------------------------------------------------------*/
void AfSplitterClientWnd::PostAttach(void)
{
	m_qsplf.Attach(NewObj AfSubClientSplitterWnd(this));
	if (m_fScrollHoriz)
	{
		m_qsplf->EnableHScroll();
	}

	// Create the main data window and attach it.
	WndCreateStruct wcs;
	wcs.InitChild(_T("AfClientWnd"), m_hwnd, 1000);
	wcs.style &= ~WS_CLIPCHILDREN; // Allows the gray splitter bar to draw over the child windows.
	m_qsplf->CreateHwnd(wcs);
	::ShowWindow(m_qsplf->Hwnd(), SW_SHOW);
}

/*----------------------------------------------------------------------------------------------
	Pass the OnSize down to the splitter.
----------------------------------------------------------------------------------------------*/
bool AfSplitterClientWnd::OnSize(int wst, int dxp, int dyp)
{
	::MoveWindow(m_qsplf->Hwnd(), 0, 0, dxp, dyp, true);
	return SuperClass::OnSize(wst, dxp, dyp);
}

/*----------------------------------------------------------------------------------------------
	Handle window messages. The only interesting thing to do is to pass focus down to the
	embedded window.
----------------------------------------------------------------------------------------------*/
bool AfSplitterClientWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{

	case WM_SETFOCUS:
		::SetFocus(m_qsplf->Hwnd());
		return true;

	default:
		return SuperClass::FWndProc(wm, wp, lp, lnRet);
	}
}


/*----------------------------------------------------------------------------------------------
	Called when the frame window is gaining or losing activation.
	@param fActivating true if gaining activation, false if losing activation.
----------------------------------------------------------------------------------------------*/
void AfSplitterClientWnd::OnPreActivate(bool fActivating)
{
	SuperClass::OnPreActivate(fActivating);
	// Pass activation message to the current pane.
	CurrentPane()->OnPreActivate(fActivating);
}

/*----------------------------------------------------------------------------------------------
	Get the split and scroll info for the window.
	This saves the info then RestoreVwInfo restores it.
----------------------------------------------------------------------------------------------*/
void AfSplitterClientWnd::GetVwSpInfo(WndSettings * pwndSet)
{
	pwndSet->vstuName = GetViewName();
	pwndSet->viTopP = TopPaneHeight();

	GetPane(0)->GetScrollInfo(SB_VERT, &pwndSet->siT);

	if (pwndSet->viTopP > 0)
		GetPane(1)->GetScrollInfo(SB_VERT, &pwndSet->siB);
}


/*----------------------------------------------------------------------------------------------
	Restore the split and scroll info for the window that was saved with TmpSaveInf().
----------------------------------------------------------------------------------------------*/
void AfSplitterClientWnd::RestoreVwInfo(WndSettings wndSet)
{
	if ((TopPaneHeight() == 0) && (wndSet.viTopP > 0))
		SplitWindow(wndSet.viTopP);

	AfDeSplitChildPtr qadsc = dynamic_cast<AfDeSplitChild *>(GetPane(0));
	if (qadsc)
	{
		// It is a data entry view
		qadsc->SetTreeWidth(wndSet.viTreeW);
		qadsc->SetScrollInfo(SB_VERT, &wndSet.siT, true);

		if (wndSet.viTopP > 0)
		{
			qadsc = dynamic_cast<AfDeSplitChild *>(GetPane(1));
			qadsc->SetTreeWidth(wndSet.viTreeW);
			qadsc->SetScrollInfo(SB_VERT, &wndSet.siB, true);
		}
	}
	else
	{
		// It is not a data entry view
		AfVwSplitChildPtr qaspw = dynamic_cast<AfVwSplitChild*>(GetPane(0));
		qaspw->SetScrollInfo(SB_VERT, &wndSet.siT, true);
		// JT 4/26/01 no longer needed, draws at the position indicated by the scroll bar.
		//qaspw->ScrollTo(0, wndSet.siT.nPos);

		if (wndSet.viTopP > 0)
		{
			qaspw = dynamic_cast<AfVwSplitChild*>(GetPane(1));
			qaspw->SetScrollInfo(SB_VERT, &wndSet.siB, true);
			// JT 4/26/01 no longer needed, draws at the position indicated by the scroll bar.
			//qaspw->ScrollTo(0, wndSet.siB.nPos);
		}
	}

}

/*----------------------------------------------------------------------------------------------
	Synchronize all panes in this client with any changes made in the database.
	@param sync -> The information describing a given change.
----------------------------------------------------------------------------------------------*/
bool AfSplitterClientWnd::Synchronize(SyncInfo & sync)
{
	if (!m_qsplf)
		return true; // Not initialized.
	if (!GetPane(0)->Synchronize(sync))
		return false;
	AfSplitChild * pasc = GetPane(1);
	if (pasc && !pasc->Synchronize(sync))
		return false;
	return true;
}
