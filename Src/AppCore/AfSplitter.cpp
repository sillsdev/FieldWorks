/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfSplitter.cpp
Responsibility: Darrell Zook
Last reviewed:

Description:
	This file contains class definitions for the following classes:
		AfSplitFrame : AfWnd - This class embeds one or more AfSplitChild windows inside. It
			also creates a vertical scrollbar (AfSplitScroll) for each child window. It
			provides a way for the user to split a window horizontally.
		AfSplitChild : AfWnd - This window represents one pane inside of an AfSplitFrame window.
		AfSplitScroll: AfWnd - This class is used for the scrollbars inside an AfSplitFrame
			window.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


static int s_dypOffset;


/***********************************************************************************************
	AfSplitFrame methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfSplitFrame::AfSplitFrame(bool fScrollHoriz)
{
	m_fDragging = false;
	m_ypLastDragPos = kypLastDragPosInvalid;
	m_hbrHalfTone = NULL;
	m_dypTopPane = 0;
	m_iPaneWithFocus = 0;
	m_fScrollHoriz = fScrollHoriz;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfSplitFrame::~AfSplitFrame(void)
{
	if (m_hbrHalfTone)
	{
		AfGdi::DeleteObjectBrush(m_hbrHalfTone);
		m_hbrHalfTone = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfSplitFrame::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_MOUSEMOVE:
		return OnMouseMove(wp, MakePoint(lp));

	case WM_LBUTTONDOWN:
		return OnLButtonDown(wp, MakePoint(lp));

	case WM_LBUTTONUP:
		return OnLButtonUp(wp, MakePoint(lp));

	case WM_HSCROLL:
		return OnHScroll(LOWORD(wp), HIWORD(wp), (HWND)lp);

	case WM_VSCROLL:
		return OnVScroll(LOWORD(wp), HIWORD(wp), (HWND)lp);

	case WM_KEYDOWN:
		return OnKeyDown(wp, lp);

	case WM_ERASEBKGND:
		return true;

	case WM_SETFOCUS:
		if (!m_fDragging)
		{
			// Find out which pane last had the focus and set it again.
			if (!m_rgqsplc[m_iPaneWithFocus])
			{
				Assert(m_iPaneWithFocus == 1);
				m_iPaneWithFocus = 0;
			}
			AssertPtr(m_rgqsplc[m_iPaneWithFocus]);
			::SetFocus(m_rgqsplc[m_iPaneWithFocus]->Hwnd());
		}
		return true;

	default:
		return SuperClass::FWndProc(wm, wp, lp, lnRet);
	}
}


/*----------------------------------------------------------------------------------------------
	The splitter frame has been created.
----------------------------------------------------------------------------------------------*/
void AfSplitFrame::PostAttach(void)
{
	AfSplitScrollPtr qspls;
	qspls.Create();
	m_rghwndScrollV[0] = qspls->Create(this, SBS_VERT);
	if (m_fScrollHoriz)
	{
		qspls.Create();
		m_hwndScrollH = qspls->Create(this, SBS_HORZ);
	}

	// Create the first child.
	CreateChild(NULL, &m_rgqsplc[0]);

	// Create a half-tone brush to use while dragging the splitter.
	WORD rgT[8];
	for (int i = 0; i < 8; i++)
		rgT[i] = (WORD)(0x5555 << (i & 1));
	HBITMAP hbmpGray = AfGdi::CreateBitmap(8, 8, 1, 1, &rgT);
	if (hbmpGray != NULL)
	{
		Assert(m_hbrHalfTone == NULL);
		if (m_hbrHalfTone)
			AfGdi::DeleteObjectBrush(m_hbrHalfTone);
		m_hbrHalfTone = AfGdi::CreatePatternBrush(hbmpGray);
		AfGdi::DeleteObjectBitmap(hbmpGray);
	}
}


/*----------------------------------------------------------------------------------------------
	Resize the child windows.
----------------------------------------------------------------------------------------------*/
bool AfSplitFrame::OnSize(int wst, int dxp, int dyp)
{
	AssertObj(m_rgqsplc[0]);

	int dxpScroll = ::GetSystemMetrics(SM_CXVSCROLL);
	int dypScroll = 0;
	if (m_fScrollHoriz)
	{
		dypScroll = ::GetSystemMetrics(SM_CYHSCROLL);
		::MoveWindow(m_hwndScrollH, 0, dyp - dypScroll, dxp, dypScroll, true);
	}

	if (m_rgqsplc[1])
	{
		// We currently have two panes visible.
		Assert(m_rghwndScrollV[1]);

		// Move the top pane.
		int dypTopPane = Min(m_dypTopPane, dyp - kdypSplitter);
		::MoveWindow(m_rgqsplc[0]->Hwnd(), 0, 0, dxp - dxpScroll, dypTopPane, true);
		::MoveWindow(m_rghwndScrollV[0], dxp - dxpScroll, 0, dxpScroll, dypTopPane, true);

		// Move the bottom pane.
		int ypBottomPane = m_dypTopPane + kdypSplitter;
		::MoveWindow(m_rgqsplc[1]->Hwnd(), 0, ypBottomPane, dxp - dxpScroll,
			dyp - ypBottomPane - dypScroll, true);
		::MoveWindow(m_rghwndScrollV[1], dxp - dxpScroll, ypBottomPane, dxpScroll,
			dyp - ypBottomPane - dypScroll, true);
	}
	else
	{
		// We currently have only one pane visible.
		::MoveWindow(m_rgqsplc[0]->Hwnd(), 0, 0, dxp - dxpScroll, dyp - dypScroll, true);
		::MoveWindow(m_rghwndScrollV[0], dxp - dxpScroll, kdypSplitter, dxpScroll,
			dyp - kdypSplitter - dypScroll, true);
	}

	::InvalidateRect(m_hwnd, NULL, true);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Paint the splitter bar.
----------------------------------------------------------------------------------------------*/
bool AfSplitFrame::OnPaint(HDC hdcDef)
{
	Rect rc;
	GetClientRect(rc);

	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps);
	Rect rcEdge;

	if (m_rgqsplc[1])
	{
		// We currently have two panes visible, so draw the bar across the middle of the window.
		int dypTopPane = Min(m_dypTopPane, rc.Height() - kdypSplitter);
		rcEdge.Set(0, dypTopPane, rc.right, dypTopPane + kdypSplitter);
	}
	else
	{
		// We currently have only one pane visible, so draw the bar at the top-right corner.
		int dxpScroll = ::GetSystemMetrics(SM_CXVSCROLL);
		rcEdge.Set(rc.right - dxpScroll, 0, rc.right, kdypSplitter);
	}
	::DrawEdge(hdc, &rcEdge, EDGE_RAISED, BF_RECT | BF_MIDDLE);

	::EndPaint(m_hwnd, &ps);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Track the mouse position to find where the window should be split.
----------------------------------------------------------------------------------------------*/
bool AfSplitFrame::OnMouseMove(UINT nFlags, Point pt)
{
	::SetCursor(::LoadCursor(NULL, IDC_SIZENS));
	if (m_fDragging)
	{
		if (m_ypLastDragPos != kypLastDragPosInvalid)
			DrawGhostBar(); // Erase the last position.
		Rect rc;
		GetClientRect(rc);
		m_ypLastDragPos = Min(Max(0, (int)pt.y - s_dypOffset), rc.Height() - kdypSplitterBar);
		DrawGhostBar();
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Start tracking the mouse to find where the window should be split.
----------------------------------------------------------------------------------------------*/
bool AfSplitFrame::OnLButtonDown(UINT nFlags, Point pt)
{
	// Don't allow a split if current pane isn't allowed to change.
	if (!CurrentPane()->IsOkToChange())
		return true;

	m_fDragging = true;
	::SetCapture(m_hwnd);
	::SetCursor(::LoadCursor(NULL, IDC_SIZENS));
	::SetFocus(m_hwnd);

	Rect rc;
	GetClientRect(rc);
	s_dypOffset = pt.y - Min(m_dypTopPane, rc.Height() - kdypSplitter) - 1;
	return OnMouseMove(nFlags, pt);
}


/*----------------------------------------------------------------------------------------------
	If there is currently only one child window, create a new window and resize both of them.
	Otherwise, just resize the two existing child windows.
----------------------------------------------------------------------------------------------*/
bool AfSplitFrame::OnLButtonUp(UINT nFlags, Point pt)
{
	if (m_fDragging)
	{
		::ReleaseCapture();
		m_fDragging = false;
		m_dypTopPane = Max(0, m_ypLastDragPos - 1);

		// Clear the last focus rect that was drawn.
		DrawGhostBar();
		m_ypLastDragPos = kypLastDragPosInvalid;

		Rect rc;
		GetClientRect(rc);
		int dypMinPaneHeight = m_rgqsplc[0]->GetMinPaneHeight();
		if (m_rgqsplc[1])
		{
			Rect rcPane2;
			::GetWindowRect(m_rgqsplc[1]->Hwnd(), &rcPane2);
			::MapWindowPoints(NULL, m_hwnd, (POINT *)&rcPane2, 2);

			if (m_dypTopPane < dypMinPaneHeight) // Close the top pane.
				UnsplitWindow(false);
			else if (m_dypTopPane > rcPane2.bottom - dypMinPaneHeight) // Close the bottom pane.
				UnsplitWindow(true);
			else
				OnSize(kwstRestored, rc.Width(), rc.Height());
		}
		else
		{
			SplitWindow(m_dypTopPane);
		}
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Pass the scroll message on to the appropriate splitter child.
----------------------------------------------------------------------------------------------*/
bool AfSplitFrame::OnHScroll(int wst, int yp, HWND hwndSbar)
{
	Assert(m_fScrollHoriz);
	Assert(hwndSbar == m_hwndScrollH);
	::SendMessage(m_rgqsplc[0]->Hwnd(), WM_HSCROLL, MAKEWPARAM(wst, yp), (LPARAM)hwndSbar);
	if (m_rgqsplc[1])
		::SendMessage(m_rgqsplc[1]->Hwnd(), WM_HSCROLL, MAKEWPARAM(wst, yp), (LPARAM)hwndSbar);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Pass the scroll message on to the appropriate splitter child.
----------------------------------------------------------------------------------------------*/
bool AfSplitFrame::OnVScroll(int wst, int yp, HWND hwndSbar)
{
	Assert(hwndSbar == m_rghwndScrollV[0] || hwndSbar == m_rghwndScrollV[1]);
	AssertPtr(m_rgqsplc[hwndSbar == m_rghwndScrollV[1]]);
	::SendMessage(m_rgqsplc[hwndSbar == m_rghwndScrollV[1]]->Hwnd(), WM_VSCROLL,
		MAKEWPARAM(wst, yp), (LPARAM)hwndSbar);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Process key down messages (WM_KEYDOWN). Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfSplitFrame::OnKeyDown(WPARAM wp, LPARAM lp)
{
	// If we are splitting the window, cancel the operation.
	if (wp == VK_ESCAPE && m_fDragging)
	{
		m_fDragging = false;
		::ReleaseCapture();

		// Clear the last focus rect that was drawn.
		DrawGhostBar();
		m_ypLastDragPos = kypLastDragPosInvalid;

		::SetFocus(m_rgqsplc[m_iPaneWithFocus]->Hwnd());

		return true; // Don't pass the message on.
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Release smart pointers.
----------------------------------------------------------------------------------------------*/
void AfSplitFrame::OnReleasePtr()
{
	m_rgqsplc[0].Clear();
	m_rgqsplc[1].Clear();
}


/*----------------------------------------------------------------------------------------------
	Draw a ghost bar that shows the user where the split will happen.
----------------------------------------------------------------------------------------------*/
void AfSplitFrame::DrawGhostBar()
{
	Rect rc;
	GetClientRect(rc);

	// Invert the brush pattern (looks just like frame window sizing)
	HDC hdc = ::GetDC(m_hwnd);
	HBRUSH hbrOld = AfGdi::SelectObjectBrush(hdc, m_hbrHalfTone);
	::PatBlt(hdc, 0, m_ypLastDragPos, rc.Width(), kdypSplitterBar, PATINVERT);
	AfGdi::SelectObjectBrush(hdc, hbrOld, AfGdi::OLD);
	int iSuccess;
	iSuccess = ::ReleaseDC(m_hwnd, hdc);
	Assert(iSuccess);
}


/*----------------------------------------------------------------------------------------------
	Get scroll information for a specific splitter child.
----------------------------------------------------------------------------------------------*/
bool AfSplitFrame::GetScrollInfo(AfSplitChild * psplc, int nBar, SCROLLINFO * psi)
{
	AssertPtr(psi);
	AssertPtr(psplc);
	Assert(psplc == m_rgqsplc[0] || psplc == m_rgqsplc[1]);
	if (nBar == SB_VERT)
		return ::GetScrollInfo(m_rghwndScrollV[psplc == m_rgqsplc[1]], SB_CTL, psi);

	if (nBar == SB_HORZ && m_fScrollHoriz)
		return ::GetScrollInfo(m_hwndScrollH, SB_CTL, psi);

	psi->nMin = psi->nPage = psi->nPos = psi->nTrackPos = 0;
	psi->nMax = 1;
	return false;
}


/*----------------------------------------------------------------------------------------------
	Set scroll information for a specific splitter child.
----------------------------------------------------------------------------------------------*/
int AfSplitFrame::SetScrollInfo(AfSplitChild * psplc, int nBar, SCROLLINFO * psi, bool fRedraw)
{
	AssertPtr(psi);
	AssertPtr(psplc);
	Assert(psplc == m_rgqsplc[0] || psplc == m_rgqsplc[1]);
	if (nBar == SB_VERT)
		return ::SetScrollInfo(m_rghwndScrollV[psplc == m_rgqsplc[1]], SB_CTL, psi, fRedraw);

	if (nBar == SB_HORZ && m_fScrollHoriz)
		return ::SetScrollInfo(m_hwndScrollH, SB_CTL, psi, fRedraw);

	return false;
}

void AfSplitFrame::GetScrollOffsets(AfSplitChild * psplc, int * pdxd, int * pdyd)
{
	AssertPtr(psplc);
	Assert(psplc == m_rgqsplc[0] || psplc == m_rgqsplc[1]);

	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_POS, 0, 0, 0, 0 };

	if (m_fScrollHoriz)
	{
		::GetScrollInfo(m_hwndScrollH, SB_HORZ, &sinfo);
		*pdxd = sinfo.nPos;
	}
	else
	{
		*pdxd = 0;
	}

	::GetScrollInfo(m_rghwndScrollV[psplc == m_rgqsplc[1]], SB_VERT, &sinfo);
	*pdyd = sinfo.nPos;
}


/*----------------------------------------------------------------------------------------------
	Set the current active pane.
----------------------------------------------------------------------------------------------*/
void AfSplitFrame::SetCurrentPane(AfSplitChild * psplc)
{
	AssertPtr(psplc);
	Assert(psplc == m_rgqsplc[0] || psplc == m_rgqsplc[1]);
	m_iPaneWithFocus = psplc == m_rgqsplc[1];
}


/*----------------------------------------------------------------------------------------------
	Return the handle to the scrollbar that corresponds to the given split child.
----------------------------------------------------------------------------------------------*/
HWND AfSplitFrame::GetScrollBarFromPane(AfSplitChild * psplc)
{
	AssertPtr(psplc);
	Assert(psplc == m_rgqsplc[0] || psplc == m_rgqsplc[1]);
	return m_rghwndScrollV[psplc == m_rgqsplc[1]];
}


/*----------------------------------------------------------------------------------------------
	Split the window at the given position. This is only valid if the window is not already
	split. If ypSplit is -1, the window is split in half horizontally.
----------------------------------------------------------------------------------------------*/
void AfSplitFrame::SplitWindow(int ypSplit)
{
	Assert(m_rgqsplc[0].Ptr() != NULL);
	Assert(m_rgqsplc[1].Ptr() == NULL);

	Rect rc;
	GetClientRect(rc);

	if (ypSplit == -1)
		ypSplit = rc.Height() / 2;

	int dypMinPaneHeight = m_rgqsplc[0]->GetMinPaneHeight();
	if (ypSplit <= dypMinPaneHeight || m_dypTopPane > rc.Height() - dypMinPaneHeight)
	{
		// The space to create the new pane in is too small, so return without creating
		// a new window.
		m_dypTopPane = 0;
		return;
	}

	m_dypTopPane = ypSplit;

	AfSplitScrollPtr qspls;
	qspls.Create();
	m_rghwndScrollV[1] = qspls->Create(this, SBS_VERT);

	CreateChild(m_rgqsplc[0], &m_rgqsplc[1]);

	// Set the scroll position of the second pane to the scroll position of the first pane.
	SCROLLINFO si = { isizeof(si), SIF_ALL };
	::GetScrollInfo(m_rghwndScrollV[0], SB_CTL, &si);
	::SetScrollInfo(m_rghwndScrollV[1], SB_CTL, &si, true);
	si.nTrackPos = si.nPos;
	si.fMask = SIF_TRACKPOS;
	::SendMessage(m_rgqsplc[1]->Hwnd(), WM_VSCROLL, MAKEWPARAM(SB_THUMBTRACK, si.nPos), 0);

	// Force a resize so the panes get moved to the correct positions.
	OnSize(kwstRestored, rc.Width(), rc.Height());

	::SetFocus(m_rgqsplc[0]->Hwnd());

	AfVwSplitChildPtr qaspw = dynamic_cast<AfVwSplitChild *>(GetPane(0));
	if (qaspw)
	{
		// Not a data entry view.
		// Scroll the selection into view in the top pane, and rescroll the bottom pane to
		// match.
		// Review (SharonC): Find a way to do this so we don't have to
		// scroll the bottom pane twice.
		qaspw->ScrollSelectionIntoView(NULL, kssoDefault);

		SCROLLINFO si2 = { isizeof(si2), SIF_ALL };
		::GetScrollInfo(m_rghwndScrollV[0], SB_CTL, &si2);
		// Only adjust the position, not min, max or anything else. Ideally we should be
		// using the special method that is overridden on RnBrowseSplitChild, but that didn't
		// seem to work for some reason.
		si2.fMask = SIF_POS; // only adjust the position
		::SetScrollInfo(m_rghwndScrollV[1], SB_CTL, &si2, true);
		si2.nTrackPos = si2.nPos;
		si2.fMask = SIF_TRACKPOS;
		::SendMessage(m_rgqsplc[1]->Hwnd(), WM_VSCROLL, MAKEWPARAM(SB_THUMBTRACK, si2.nPos),
			0);
		OnSize(kwstRestored, rc.Width(), rc.Height());
	}
}


/*----------------------------------------------------------------------------------------------
	Unsplits the window. This is only valid if the window is already split. If fCloseBottom is
	true, the bottom pane will be destroyed, otherwise the top pane will be destroyed.
----------------------------------------------------------------------------------------------*/
void AfSplitFrame::UnsplitWindow(bool fCloseBottom)
{
	Assert(m_rgqsplc[0].Ptr() != NULL);
	Assert(m_rgqsplc[1].Ptr() != NULL);

	if (fCloseBottom)
	{
		::SendMessage(m_rgqsplc[1]->Hwnd(), WM_CLOSE, 0, 0);
		::SendMessage(m_rghwndScrollV[1], WM_CLOSE, 0, 0);
	}
	else
	{
		::SendMessage(m_rgqsplc[0]->Hwnd(), WM_CLOSE, 0, 0);
		m_rgqsplc[0] = m_rgqsplc[1];
		::SendMessage(m_rghwndScrollV[0], WM_CLOSE, 0, 0);
		m_rghwndScrollV[0] = m_rghwndScrollV[1];
	}

	m_rgqsplc[1].Clear();
	m_rghwndScrollV[1] = NULL;
	::SetFocus(m_rgqsplc[0]->Hwnd());
	m_dypTopPane = 0;

	// Force a resize so the panes get moved to the correct positions.
	Rect rc;
	GetClientRect(rc);
	OnSize(kwstRestored, rc.Width(), rc.Height());
}


/***********************************************************************************************
	AfSplitChild methods.
***********************************************************************************************/

static DummyFactory g_fact(_T("SIL.AppCore.AfSplitChild"));

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfSplitChild::AfSplitChild()
{
	m_himlDrag = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfSplitChild::~AfSplitChild()
{
	if (m_himlDrag)
	{
		AfGdi::ImageList_Destroy(m_himlDrag);
		m_himlDrag = NULL;
	}
}

AfSplitterClientWnd * AfSplitChild::GetSplitterClientWnd()
{
	return dynamic_cast<AfSplitterClientWnd*>(Parent()->Parent());
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfSplitChild::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_SETFOCUS)
		m_qsplf->SetCurrentPane(this);

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	The splitter child has been created.
----------------------------------------------------------------------------------------------*/
void AfSplitChild::PostAttach(void)
{
	m_qsplf = dynamic_cast<AfSplitFrame *>(AfWnd::GetAfWnd(::GetParent(m_hwnd)));
	AssertObj(m_qsplf);
	// Register this window as accepting drops from drag/drop.
	IDropTargetPtr qdt;
	QueryInterface(IID_IDropTarget, (void **)&qdt);
	RegisterDragDrop(m_hwnd, qdt);
}


/*----------------------------------------------------------------------------------------------
	Redraw the splitter child when it is resized.
----------------------------------------------------------------------------------------------*/
bool AfSplitChild::OnSize(int wst, int dxp, int dyp)
{
	::InvalidateRect(m_hwnd, NULL, true);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Release smart pointers.
----------------------------------------------------------------------------------------------*/
void AfSplitChild::OnReleasePtr()
{
	RevokeDragDrop(m_hwnd);
	m_qsplf.Clear();
}


/*----------------------------------------------------------------------------------------------
	Ask the parent to get the scroll information for this window.
----------------------------------------------------------------------------------------------*/
bool AfSplitChild::GetScrollInfo(int nBar, SCROLLINFO * psi)
{
	AssertPtr(psi);
	AssertObj(m_qsplf);
	return m_qsplf->GetScrollInfo(this, nBar, psi);
}


/*----------------------------------------------------------------------------------------------
	Ask the parent to set the scroll information for this window.
----------------------------------------------------------------------------------------------*/
int AfSplitChild::SetScrollInfo(int nBar, SCROLLINFO * psi, bool fRedraw)
{
	AssertPtr(psi);
	AssertObj(m_qsplf);
#ifdef JohnT_20010423_NOT_SAFE
	// Darrell seemed to think this would help with something, but an inaccurate range can
	// confuse some of the lazy evaluation stuff, which tries to increase and decrease it
	// as things get expanded.
	if (psi->fMask & (SIF_RANGE | SIF_PAGE) && (int)psi->nPage > psi->nMax - psi->nMin)
	{
		// REVIEW DarrellZ: Is it OK to change the min and max in this case?
		// This is to try to keep the scrollbar from allowing any scroll at all. It seems the
		// page has to be set to one greater than the maximum scroll position for this to work.
		psi->nMin = 0;
		psi->nMax = INT_MAX - 1;
		psi->nPage = INT_MAX;
	}
#endif
	return m_qsplf->SetScrollInfo(this, nBar, psi, fRedraw);
}

/*----------------------------------------------------------------------------------------------
	Get your scroll offsets.
----------------------------------------------------------------------------------------------*/
void AfSplitChild::GetScrollOffsets(int * pdxd, int * pdyd)
{
	AssertPtr(pdxd);
	AssertPtr(pdyd);
	AssertObj(m_qsplf);
	m_qsplf->GetScrollOffsets(this, pdxd, pdyd);
}

/*----------------------------------------------------------------------------------------------
	Scroll to the specified offsets.
	Does NOT update the scroll bars. That MUST be done before calling this.
	Scrolls down by dxdOffset, right by dydOffset (or reverse if negative).
	That is, they are the amounts being added to the scroll bar position.
----------------------------------------------------------------------------------------------*/
void AfSplitChild::ScrollBy(int dxdOffset, int dydOffset, Rect * prc)
{
	AssertPtrN(prc);
	try
	{
		int dx = - dxdOffset;
		int dy = - dydOffset;
		if (dx == 0 && dy == 0)
			return; // no change.

		RECT rect;
		if (prc)
			rect = *prc;
		else
			::GetClientRect(m_hwnd, &rect);
		if (dx != 0 && dy != 0)
		{
			// ScrollWindow doesn't handle both directions at once; do a complete redraw
			::InvalidateRect(m_hwnd, NULL, false);
		}
		else
		{
			// smoother effect with ScrollWindow
			::ScrollWindowEx(m_hwnd, dx, dy,
				&rect, // whole client rectangle
				&rect,  // clip just to client rectangle--can this be null??
				NULL,	// don't care what region is invalidated
				NULL,	// also don't care what rectangle is invalidated
				SW_ERASE + SW_INVALIDATE + SW_SCROLLCHILDREN); // An child windows moved automatically.
		}
		::UpdateWindow(m_hwnd);
	}
	catch (...)
	{
		// Nothing we can usefully do, just don't propagate the error.
	}
}


/*----------------------------------------------------------------------------------------------
	Returns a pointer to a specified interface on an object to which a client currently holds
	an interface pointer.
	@param riid Identifier of the requested interface.
	@param ppv Address of output variable that receives the interface pointer requested in riid.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfSplitChild::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IDropSource *>(this));
	else if (riid == IID_IDropSource)
		*ppv = static_cast<IDropSource *>(this);
	else if (riid == IID_IDropTarget)
		*ppv = static_cast<IDropTarget *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(
			static_cast<IUnknown *>(static_cast<IDropSource *>(this)), IID_IDropSource);
//		*ppv = NewObj CSupportErrorInfo(this, IID_IDropTarget);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Determines whether a drag-and-drop operation should be continued, canceled, or completed.
	You do not call this method directly. The OLE DoDragDrop function calls this method during
	a drag-and-drop operation.
	@param fEscapePressed Status of escape key since previous call.
	@grfKeyState Current state of keyboard modifier keys.
	@return S_OK Operation succeeded. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfSplitChild::QueryContinueDrag(BOOL fEscapePressed, DWORD grfKeyState)
{
	BEGIN_COM_METHOD;
	//static int n;
	//StrAnsi sta;
	//sta.Format("QueryContinueDrag: %d, %x, %d\n", fEscapePressed, grfKeyState, n++);
	//OutputDebugString(sta.Chars());

	// Keep going unless Esc pressed, or the left moust button was released.
	if (!fEscapePressed && (grfKeyState & MK_LBUTTON))
		return S_OK;

	// Clear the drag image.
	::ImageList_EndDrag();
	::ImageList_DragLeave(NULL);
	if (m_himlDrag)
	{
		AfGdi::ImageList_Destroy(m_himlDrag);
		m_himlDrag = NULL;
	}

	// ESC was pressed: stop the drag
	if (fEscapePressed)
		return DRAGDROP_S_CANCEL;

	// Left mouse no longer down: drop the data where it is
	return DRAGDROP_S_DROP;

	END_COM_METHOD(g_fact, IID_IDropSource);
}


/*----------------------------------------------------------------------------------------------
	Enables a source application to give visual feedback to the end user during a drag-and-drop
	operation by providing the DoDragDrop function with an enumeration value specifying the
	visual effect.
	@param dwEffect The DROPEFFECT value returned by the most recent call to
		IDropTarget::DragEnter, IDropTarget::DragOver, or IDropTarget::DragLeave.
		Possible values are:
		DROPEFFECT_NONE - Drop target cannot accept the data.
		DROPEFFECT_COPY - Drop results in a copy. The original data is untouched by the
			drag source.
		DROPEFFECT_MOVE - Drag source should remove the data.
		DROPEFFECT_LINK - Drag source should create a link to the original data.
		DROPEFFECT_SCROLL -Scrolling is about to start or is currently occurring in the target.
			This value is used in addition to the other values.
	@return S_OK Operation succeeded. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfSplitChild::GiveFeedback(DWORD dwEffect)
{
	BEGIN_COM_METHOD;

	//StrAnsi sta;
	//sta.Format("GiveFeedback: %x\n", dwEffect);
	//OutputDebugString(sta.Chars());
	return DRAGDROP_S_USEDEFAULTCURSORS;

	END_COM_METHOD(g_fact, IID_IDropSource);
}


/*----------------------------------------------------------------------------------------------
	Indicates whether a drop can be accepted, and, if so, the effect of the drop.
	@param pdobj Pointer to the interface of the source data object.
	@param grfKeyState Current state of keyboard modifier keys.
	@param pt Current mouse position in long screen coordinates.
	@param pdwEffect Pointer to the effect of the drag-and-drop operation.
	@return S_OK Operation succeeded. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfSplitChild::DragEnter(IDataObject * pdobj, DWORD grfKeyState, POINTL pt,
	DWORD * pdwEffect)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pdwEffect);
	ChkComArgPtr(pdobj);
	//StrAnsi sta;
	//sta.Format("DragEnter: %x, %d, %d, %x\n", grfKeyState, pt.x, pt.y, *pdwEffect);
	//OutputDebugString(sta.Chars());

	*pdwEffect = DROPEFFECT_NONE;

	m_hvoDrag = 0; // Flag for DragOver to say we are not ready to drop.

	FORMATETC fmte;
	IEnumFORMATETCPtr qefe;
	pdobj->EnumFormatEtc(DATADIR_GET, &qefe);
	AssertPtr(qefe);
	ULONG cfmte;
	// Enumerate through FORMATETC to find one we can use.
	do
	{
		qefe->Next(1, &fmte, &cfmte);
		if (!cfmte)
			// Can't find a format we can handle, so we are done.
			return S_OK;
		if (fmte.cfFormat == CmDataObject::GetClipboardType() &&
			fmte.tymed & TYMED_HGLOBAL)
			break;
	} while (cfmte);


	// Once found, call this to determine whether the data object can render the data on
	// the target by examining the formats and medium specified for the data object.
	STGMEDIUM medium;
	try {
		CheckHr(pdobj->QueryGetData(&fmte));
		CheckHr(pdobj->GetData(&fmte, &medium));
	}
	catch(Throwable & thr) {
		::OutputDebugString(thr.Message());
		return S_OK;
	}

	// We got our data type. Get the information out of it.
	HVO hvo;
	wchar * prgch;
	prgch = (wchar *)::GlobalLock(medium.hGlobal);
	wchar wch0;
	wchar wch1;
	wch0 = *prgch++;
	wch1 = *prgch++;
	hvo = (HVO)(wch1 << 16 | wch0);
	Assert(hvo); // Need to have an HVO to work.
	wch0 = *prgch++;
	wch1 = *prgch++;
	m_clidDrag = (int)(wch1 << 16 | wch0);
	Assert(m_clidDrag); // Need to have a clid to work.
	wch0 = *prgch++;
	wch1 = *prgch++;
	m_pidDrag = (int)(wch1 << 16 | wch0);
	Assert(m_pidDrag); // Need to have a pid to work.
	wchar pszSvr[500]; // Assume name will never be longer than this.
	wchar * pch = pszSvr;
	// Get Server name.
	while (*prgch)
		*pch++ = *prgch++;
	*pch++ = *prgch++;
	pch = pszSvr;
	// Get Db name.
	m_stuSvrDrag = pszSvr;
	while (*prgch)
		*pch++ = *prgch++;
	*pch++ = *prgch++;
	m_stuDbDrag = pszSvr;
	pch = pszSvr;
	// Get TsString text.
	while (*prgch)
		*pch++ = *prgch++;
	*pch++ = *prgch++; // Copy null.
	StrUni stu = pszSvr;
	// Get TsString format.
	int cb = *prgch++;
	byte * rgb = NewObj byte[cb];
	memcpy(rgb, prgch, cb);
	// Make TsString.
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	int cch = stu.Length();
	try{
		CheckHr(qtsf->DeserializeStringRgch(stu.Chars(), &cch, rgb, &cb, &m_qtssDrag));
	}
	catch(...){
		delete[] rgb;
		throw;
	}
	delete[] rgb;

	::GlobalUnlock(medium.hGlobal);
	::ReleaseStgMedium(&medium);

	// Get the size of the drag text.
	HDC hdc = ::GetDC(m_hwnd);
	SIZE size;
	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
	::GetTextExtentPoint32W(hdc, stu.Chars(), stu.Length(), &size);
	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);

	// Create a bitmap with the drag image.
	HDC hdcMem = AfGdi::CreateCompatibleDC(hdc);
	HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, size.cx, size.cy);
	HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
	::SetROP2(hdcMem, R2_BLACK);
	::SetTextColor(hdcMem, kclrDarkGray);
	::SetBkColor(hdcMem, kclrWhite);
	hfontOld = AfGdi::SelectObjectFont(hdcMem, ::GetStockObject(DEFAULT_GUI_FONT));
	::TextOutW(hdcMem, 0, 0, stu.Chars(), stu.Length());
	AfGdi::SelectObjectFont(hdcMem, hfontOld, AfGdi::OLD);
	AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);
	BOOL fSuccess;
	fSuccess = AfGdi::DeleteDC(hdcMem);
	Assert(fSuccess);

	int iSuccess;
	iSuccess = ::ReleaseDC(m_hwnd, hdc);
	Assert(iSuccess);

	// Create a masked drag image list using our bitmap.
	if (m_himlDrag)
	{
		AfGdi::ImageList_Destroy(m_himlDrag);
		m_himlDrag = NULL;
	}
	m_himlDrag = AfGdi::ImageList_Create(size.cx, size.cy, ILC_COLORDDB | ILC_MASK, 0, 0);
	Assert(m_himlDrag);
	::ImageList_AddMasked(m_himlDrag, hbmp, kclrWhite);
	fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
	Assert(fSuccess);

	// Adjust the image to be below and to the right of the mouse. That way it is clearly
	// visible.
	::ImageList_BeginDrag(m_himlDrag, 0, -15, -5);

	// Start the image drag. NULL uses the desktop so the drag image works smoothly across
	// the screen. The second two parameters are meaningless here. The image is actually
	// positioned and controlled by ImageList_DragMove.
	::ImageList_DragEnter(NULL, 0, 0);

	// At this point we do not allow transfers between databases, so exit if we are not in
	// the same database. Since this is below the ImageList, we will still show the drag text.
	AfMainWnd * pafw = MainWindow();
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	StrUni stuT = pdbi->ServerName();
	if (stuT.Length() != m_stuSvrDrag.Length() ||
		wcsncmp(stuT.Chars(), m_stuSvrDrag.Chars(), stuT.Length()))
	{
		return S_OK;
	}
	stuT = pdbi->DbName();
	if (stuT.Length() != m_stuDbDrag.Length() ||
		wcsncmp(stuT.Chars(), m_stuDbDrag.Chars(), stuT.Length()))
	{
		return S_OK;
	}

	m_hvoDrag = hvo; // Now we can save this so we can drop.

	END_COM_METHOD(g_fact, IID_IDropTarget);
}


/*----------------------------------------------------------------------------------------------
	Provides target feedback to the user and communicates the drop's effect to the DoDragDrop
	function so it can communicate the effect of the drop back to the source.
	@param grfKeyState Current state of keyboard modifier keys.
	@param pt Current mouse position in long screen coordinates.
	@param pdwEffect Pointer to the effect of the drag-and-drop operation. On input, this
		gives the possibilities given by DoDragDrop dwOKEffects. This should return:
		DROPEFFECT_NONE Drop target cannot accept the data.
		DROPEFFECT_COPY Drop results in a copy. The original data is untouched by the drag src.
		DROPEFFECT_MOVE Drag source should remove the data.
		DROPEFFECT_LINK Drag source should create a link to the original data.
		DROPEFFECT_SCROLL Scrolling is about to start or is currently occurring in the target.
			This value is used in addition to the other values.
	@return S_OK Operation succeeded. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfSplitChild::DragOver(DWORD grfKeyState, POINTL pt, DWORD * pdwEffect)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pdwEffect);

	//StrAnsi sta;
	//sta.Format("DragOver: %x, %d, %d, %x\n", grfKeyState, pt.x, pt.y, *pdwEffect);
	//OutputDebugString(sta.Chars());
	*pdwEffect = DROPEFFECT_NONE;
	::ImageList_DragMove(pt.x, pt.y);

	END_COM_METHOD(g_fact, IID_IDropTarget);
}


/*----------------------------------------------------------------------------------------------
	Removes target feedback and releases the data object. To implement IDropTarget::DragLeave,
	you must remove any target feedback that is currently displayed. You must also release any
	references you hold to the data transfer object.
	@return S_OK Operation succeeded.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfSplitChild::DragLeave()
{
	BEGIN_COM_METHOD;
	//StrAnsi sta;
	//sta.Format("DragLeave\n");
	//OutputDebugString(sta.Chars());

	m_hvoDrag = 0;
	m_clidDrag = 0;
	::ImageList_EndDrag();
	::ImageList_DragLeave(NULL);
	if (m_himlDrag)
	{
		AfGdi::ImageList_Destroy(m_himlDrag);
		m_himlDrag = NULL;
	}

	END_COM_METHOD(g_fact, IID_IDropTarget);
}


/*----------------------------------------------------------------------------------------------
	Incorporates the source data into the target window, removes target feedback, and releases
	the data object. This can occur after a DragOver indicates we want to drop, or when the
	mouse button is released before DragOver occurs. We need to remove any target feedback
	and release any reference counts on the data object.
	@param pDataObject Pointer to the interface for the source data.
	@param grfKeyState Current state of keyboard modifier keys.
	@param pt Current mouse position in long screen coordinates.
	@param pdwEffect Pointer to the effect of the drag-and-drop operation.
		On input, this depends on the original OnDragDrop dwOKEffects. It has nothing to
		do with the last thing returned by DragOver. On return, this indicates to OnDragDrop
		what actually happened during the drop.
	@return S_OK Operation succeeded. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfSplitChild::Drop(IDataObject * pDataObject, DWORD grfKeyState, POINTL pt,
	DWORD * pdwEffect)
{
	BEGIN_COM_METHOD;
	//StrAnsi sta;
	//sta.Format("Drop: %x, %d, %d, %x\n", grfKeyState, pt.x, pt.y, *pdwEffect);
	//OutputDebugString(sta.Chars());
	ChkComArgPtrN(pDataObject);
	ChkComArgPtrN(pdwEffect);

	m_hvoDrag = 0;
	m_clidDrag = 0;
	if (m_himlDrag)
	{
		AfGdi::ImageList_Destroy(m_himlDrag);
		m_himlDrag = NULL;
	}

	END_COM_METHOD(g_fact, IID_IDropTarget);
}


/*----------------------------------------------------------------------------------------------
	Synchronize all windows in this application with any changes made in the database.
	@param sync -> The information describing a given change.
----------------------------------------------------------------------------------------------*/
bool AfSplitChild::Synchronize(SyncInfo & sync)
{
	return true;
}


/***********************************************************************************************
	AfSplitScroll methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Create a scrollbar.
----------------------------------------------------------------------------------------------*/
HWND AfSplitScroll::Create(AfSplitFrame * psplf, DWORD dwStyle)
{
	AssertObj(psplf);

	WndCreateStruct wcs;
	wcs.InitChild(_T("SCROLLBAR"), psplf->Hwnd(), 0);
	wcs.style = WS_CHILD | WS_VISIBLE | dwStyle;
	CreateAndSubclassHwnd(wcs);

	m_qsplf = psplf;

	return m_hwnd;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfSplitScroll::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_VSCROLL || wm == WM_HSCROLL)
	{
		// Pass scroll messages to the AfSplitFrame window.
		AssertObj(m_qsplf);
		::SendMessage(m_qsplf->Hwnd(), wm, wp, (LPARAM)m_hwnd);
		return true;
	}

	return AfWnd::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Release smart pointers.
----------------------------------------------------------------------------------------------*/
void AfSplitScroll::OnReleasePtr()
{
	m_qsplf.Clear();
}
