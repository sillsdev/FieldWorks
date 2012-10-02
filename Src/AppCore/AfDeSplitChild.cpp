/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeSplitChild.cpp
Responsibility: Ken Zook
Last reviewed: never

Description:
	Implements the base for data entry functions.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

// At the moment we need this here rather than in ExplicitInstantiation to avoid problems
// with WorldPad.
#include "Vector_i.cpp"
template Vector<AfDeFieldEditor *>;

#undef THIS_FILE
DEFINE_THIS_FILE

#undef DEBUG_THIS_FILE
//# d efine DEBUG_THIS_FILE 1

// Used to store the tree width in case ESC is pressed to cancel a drag.
static int s_dxpOldTreeWidth;

// Used to determine whether the tool tip is currently showing.
static bool s_fShowToolTip;

// Timer identifier used to hide the tooltip if the mouse moves out of the window.
const int knToolTipTimer = 1;

/*----------------------------------------------------------------------------------------------
	The command map for our AfDeSplitChild.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(AfDeSplitChild)
	ON_CID_CHILD(kcidExpShow, &AfDeSplitChild::CmdExpContextMenu,
		&AfDeSplitChild::CmsExpContextMenu)
END_CMD_MAP_NIL()

/*----------------------------------------------------------------------------------------------
	The command map for our AfDeRecSplitChild.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(AfDeRecSplitChild)
	ON_CID_CHILD(kcidContextPromote, &AfDeRecSplitChild::CmdPromote,
		&AfDeRecSplitChild::CmsPromote)
END_CMD_MAP_NIL()

//:>********************************************************************************************
//:>	AfDeSplitChild methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDeSplitChild::AfDeSplitChild(bool fAlignFieldsToTree) :
	m_fAlignFieldsToTree(fAlignFieldsToTree)
{
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeSplitChild::~AfDeSplitChild()
{
	Assert(!m_pdfe); // CloseAllEditors should have been called first
	Assert(!m_vdfe.Size());
}

static DummyFactory g_fact(_T("SIL.AppCore.AfDeSplitChild"));

/*----------------------------------------------------------------------------------------------
	Finish initializing the window now that is has been created. Initializes several member
	variables, creates a tooltip window, and sets a timer for tooltip operation.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::PostAttach()
{
	SuperClass::PostAttach();

	// Set the tree font height.
	TEXTMETRIC tm;
	HDC hdc = ::GetDC(m_hwnd);
	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
	::GetTextMetrics(hdc, &tm);
	HFONT hfontXNew;
	hfontXNew = AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	Assert(hfontXNew);
	Assert(hfontXNew == ::GetStockObject(DEFAULT_GUI_FONT));

	m_dypTreeFont = tm.tmHeight;
	// Fields allow 1 pixel borders and a one pixel bottom field divider line.
	m_dypDefFieldHeight = m_dypTreeFont + 3;
	int iSuccess;
	iSuccess = ::ReleaseDC(m_hwnd, hdc);
	Assert(iSuccess);

	// Create a child tooltip window.
	m_hwndToolTip = ::CreateWindow(TOOLTIPS_CLASS, NULL, TTS_ALWAYSTIP, 0, 0, 0, 0, m_hwnd,
	//m_hwndToolTip = ::CreateWindow(TOOLTIPS_CLASS, NULL, TTS_ALWAYSTIP, 0, 0, 200, 200, NULL,
		0, ModuleEntry::GetModuleHandle(), NULL);
	if (!m_hwndToolTip)
		ThrowHr(E_FAIL);
	// NOTE: TTTOOLINFOA_V2_SIZE must be used here (with _WIN32_WINNT >= 0x0501) instead of
	// isizeof(ti) for tooltips to work. Microsoft apparently added an additional parameter to
	// TOOLINFO, but didn't complete the implementation. So using the actual size breaks the
	// tooltips (as of 11/30/01).
//	TOOLINFO ti = { isizeof(ti), TTF_IDISHWND | TTF_TRACK | TTF_ABSOLUTE | TTF_TRANSPARENT };
//	TOOLINFO ti = { isizeof(ti), TTF_IDISHWND | TTF_SUBCLASS | TTF_ABSOLUTE | TTF_TRANSPARENT };
	TOOLINFO ti = { TTTOOLINFOA_V2_SIZE,
		TTF_IDISHWND | TTF_TRACK | TTF_ABSOLUTE | TTF_TRANSPARENT };
	ti.hwnd = m_hwnd;
	ti.uId = (uint)m_hwnd;
	ti.lpszText = _T("dummy text");
	::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);
	//::SendMessage(m_hwndToolTip, TTM_TRACKACTIVATE, true, (LPARAM)&ti);

	// Get a pointer to the language project information.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	if (prmw) // May fail, in a control
	{
		m_qlpi = prmw->GetLpInfo();
		Assert(m_qlpi);
		AfDbInfo * pdbi = m_qlpi->GetDbInfo();
		AssertPtr(pdbi);
		ILgWritingSystemFactoryPtr qwsf;
		pdbi->GetLgWritingSystemFactory(&qwsf);
		AssertPtr(qwsf);
		CheckHr(qwsf->get_UserWs(&m_wsUser));
		Assert(m_wsUser);

		// Get the initial tree width and force it into range.
		Rect rc;
		GetClientRect(rc);
		int dxp = Max((int)kdxpMinTreeWidth, prmw->TreeWidth());
		SetTreeWidth(dxp);

		// Get view index.
		AfMdiClientWndPtr qmdic = prmw->GetMdiClientWnd();
		AfClientRecDeWnd * pcrde = dynamic_cast<AfClientRecDeWnd *>(Parent()->Parent());
		Assert(pcrde);
		pcrde->OnTreeWidthChanged(m_dxpTreeWidth, this);
		int wid = pcrde->GetWindowId();
		int iv = qmdic->GetChildIndexFromWid(wid);

		// Setup the vector of custom view specs.
		UserViewSpecVec & vuvs = m_qlpi->GetDbInfo()->GetUserViewSpecs();
		Assert(vuvs[iv]->m_vwt == kvwtDE);
		m_quvs = vuvs[iv];
	}

	// Set a timer every 1/10 second to see if the mouse has moved out of the window.
	::SetTimer(m_hwnd, knToolTipTimer, 100, NULL);
}


/*----------------------------------------------------------------------------------------------
	If there is another editable field below the currently open field, this closes the current
	field and opens the next editable field. dxpCursor allows the insertion point in the new
	field to be located at some point other than the left margin. The insertion point is
	always placed on the top line.
	@param dxpCursor Horiz. pixels from the left field margin for the insertion point.
	@return True if it succeeded in opening a new field. False if the current field editor
		can't be closed or if there are no editable fields below the current field.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OpenNextEditor(int dxpCursor)
{
	Assert(dxpCursor >= 0);

	// Make sure we can close the current editor, if there is one.
	if (m_pdfe && !m_pdfe->IsOkToClose())
		return false; // Can't close the current editor.

	AfDeFieldEditorPtr qdfe;
	int dypFieldTop; // The vertical pixels from the window top to the top of the field.
	int dypField; // The pixel height needed for a field.
	int dypMin = GetMinFieldHeight(); // Minimum height for a field.
	int idfe = GetEditorIndex(&dypFieldTop); // Get the current editor index.
	if (idfe >= 0)
		dypFieldTop += Max(dypMin, m_pdfe->GetHeight());
	// Open the first editable field going toward the bottom of the record.
	while (++idfe < m_vdfe.Size())
	{
		qdfe = FieldAt(idfe);
		dypField = Max(dypMin, qdfe->GetHeight());
		if (qdfe->IsEditable())
		{
			CloseEditor();
			// This assert makes sure the closing process hasn't opened another
			// editor. This will result in an invalid state with an extra editor
			// open. The CloseEditor process should never open a new editor in the
			// process. For example, synchronization needs to occur outside of
			// CloseEditor() to avoid unwanted recursion.
			Assert(!m_pdfe);
			// Account for scrolling.
			SCROLLINFO si = {isizeof(si), SIF_PAGE | SIF_POS | SIF_RANGE};
			GetScrollInfo(SB_VERT, &si);
			dypFieldTop -= si.nPos;

			// Open the next editor (lower down).
			Rect rc;
			GetClientRect(rc);
			// The -1 is for the bottom field separator line.
			Rect rcEdit(GetBranchWidth(qdfe), dypFieldTop, rc.right, dypFieldTop + dypField - 1);
			if (qdfe->BeginEdit(m_hwnd, rcEdit, dxpCursor))
			{
				m_pdfe = qdfe;
				// Only set the focus if we are a child of the current top-level window,
				// or if we can't figure what the top-level window is.
				if ((!AfApp::Papp()) || AfApp::Papp()->GetCurMainWnd() == MainWindow())
					::SetFocus(m_pdfe->Hwnd());
				int dyp = dypFieldTop + dypMin + 3; // 3 = horz. margin + bottom line.
				// If new editor doesn't show, scroll to show the first line.
				if (dyp > rc.bottom)
				{
					si.nPos += dyp - rc.bottom;
					SetScrollInfo(SB_VERT, &si, true);
					::InvalidateRect(m_hwnd, NULL, true);
				}
				return true;
			}
			// Couldn't open an editor for some reason. We've alredy closed the current editor,
			// so recover the best we can.
			m_pdfe = NULL;
			return false;
		}
		else
			dypFieldTop += dypField;
	}
	return false; // Did not find another editable field.
}


/*----------------------------------------------------------------------------------------------
	If there is another editable field above the currently open field, this closes the current
	field and opens the previous editable field. dxpCursor allows the insertion point in
	the new field to be located at some point other than the left margin. fTopCursor determines
	whether the insertion point is placed on the top or bottom line.
	@param dxpCursor Horiz. pixels from the left field margin for the insertion point.
	@param fTopCursor True if the insertion point is to be placed on the top field line.
	@return True if it succeeded in opening a new field. False if the current field editor
		can't be closed or if there are no editable fields above the current field.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OpenPreviousEditor(int dxpCursor, bool fTopCursor)
{
	Assert(dxpCursor >= 0);

	// Make sure we can close the current editor, if there is one.
	if (m_pdfe && !m_pdfe->IsOkToClose())
		return false; // Can't close the current editor.

	AfDeFieldEditorPtr qdfe;
	int dypFieldTop; // The vertical pixels from the window top to the top of the field.
	int dypField; // The pixel height needed for a field.
	int dypMin = GetMinFieldHeight(); // Minimum height for a field.
	int idfe = GetEditorIndex(&dypFieldTop); // Get the current editor index.
	// If there is no open editor, open the first one.
	if (idfe < 0)
		return OpenNextEditor();

	// Open the first editable field going toward the top of the record.
	while (--idfe >= 0)
	{
		qdfe = FieldAt(idfe);
		dypField = Max(dypMin, qdfe->GetHeight());
		dypFieldTop -= dypField;
		if (qdfe->IsEditable())
		{
			CloseEditor();
			// Account for scrolling.
			SCROLLINFO si = {isizeof(si), SIF_PAGE | SIF_POS | SIF_RANGE};
			GetScrollInfo(SB_VERT, &si);
			dypFieldTop -= si.nPos;

			// Open the next editor (higher up).
			Rect rc;
			GetClientRect(rc);
			Rect rcEdit(GetBranchWidth(qdfe), dypFieldTop, rc.right, dypFieldTop + dypField - 1);
			if (qdfe->BeginEdit(m_hwnd, rcEdit, dxpCursor, fTopCursor))
			{
				m_pdfe = qdfe;
				// Only set the focus if we are a child of the current top-level window,
				// or if we can't figure what the top-level window is.
				if ((!AfApp::Papp()) || AfApp::Papp()->GetCurMainWnd() == MainWindow())
					::SetFocus(m_pdfe->Hwnd());
				// Estimate top of bottom line in field.
				int dyp = dypFieldTop + dypField - dypMin - 2;// 2 = horz. margin.
				// If new editor doesn't show, scroll to show the cursor line.
				if (fTopCursor && dypFieldTop < 0)
				{
					// Top line must be visible.
					si.nPos += dypFieldTop;
					SetScrollInfo(SB_VERT, &si, true);
					::InvalidateRect(m_hwnd, NULL, true);
				}
				else if (!fTopCursor && dyp < 0)
				{
					// Bottom line must be visible
					si.nPos += dyp;
					SetScrollInfo(SB_VERT, &si, true);
					::InvalidateRect(m_hwnd, NULL, true);
				}
				return true;
			}
			// Couldn't open an editor for some reason. We've alredy closed the current editor,
			// so recover the best we can.
			m_pdfe = NULL;
			return false;
		}
	}
	return false; // Did not find an earlier editable field.
}

int AfDeSplitChild::IndexOfField(AfDeFieldEditor * pdfe)
{
	for (int idfe = 0; idfe < m_vdfe.Size(); ++idfe)
	{
		if (m_vdfe[idfe] == pdfe)
			return idfe;
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Try to open the field at idfe for editing. If it is not editable, and fSearch is true,
	keep looking at following editors and open the first one that is editable.
	@param idfe Index of the field we want to open.
	@param fSearch True if we should keep looking at following fields for the first
		editable field.
	@return True if it succeeded in opening a new field. False if the current field editor
		can't be closed or if there are no editable fields at or below idfe.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OpenEditor(int idfe, bool fSearch)
{
	Assert((uint)idfe < (uint)m_vdfe.Size());

	// Make sure we can close the current editor, if there is one.
	if (m_pdfe && !m_pdfe->IsOkToClose())
		return false; // Can't close the current editor.

	// If specified field isn't editable and caller doesn't want to iterate, we can't go on.
	if (!FieldAt(idfe)->IsEditable() && !fSearch)
		return false;

	AfDeFieldEditorPtr qdfe;
	int dypFieldTop = 0; // The vertical pixels from the window top to the top of the field.
	int dypField; // The pixel height needed for a field.
	int dypMin = GetMinFieldHeight(); // Minimum height for a field.

	// Add up the pixel distance from the top of record to the specified editor
	for (int idfeT = 0; idfeT < idfe; ++idfeT)
		dypFieldTop += Max(dypMin, FieldAt(idfeT)->GetHeight());

	// Open the first editable field going toward the bottom of the record.
	for (; idfe < m_vdfe.Size(); ++idfe)
	{
		qdfe = FieldAt(idfe);
		dypField = Max(dypMin, qdfe->GetHeight());
		if (qdfe->IsEditable())
		{
			CloseEditor();
			// Account for scrolling.
			SCROLLINFO si = {isizeof(si), SIF_PAGE | SIF_POS | SIF_RANGE};
			GetScrollInfo(SB_VERT, &si);
			dypFieldTop -= si.nPos;

			// Open the next editor (lower down).
			Rect rc;
			GetClientRect(rc);
			// The -1 is for the bottom field separator line.
			Rect rcEdit(GetBranchWidth(qdfe), dypFieldTop, rc.right, dypFieldTop + dypField - 1);
			if (qdfe->BeginEdit(m_hwnd, rcEdit, 0))
			{
				m_pdfe = qdfe;
				// Only set the focus if we are a child of the current top-level window,
				// or if we can't figure what the top-level window is.
				if ((!AfApp::Papp()) || AfApp::Papp()->GetCurMainWnd() == MainWindow())
					::SetFocus(m_pdfe->Hwnd());
				int dyp = dypFieldTop + dypMin + 3; // 3 = horz. margin + bottom line.
				// If new editor doesn't show, scroll to show the first line.
				if (dyp > rc.bottom)
				{
					si.nPos += dyp - rc.bottom;
					SetScrollInfo(SB_VERT, &si, true);
					::InvalidateRect(m_hwnd, NULL, true);
				}
				return true;
			}
			// Couldn't open an editor for some reason. We've alredy closed the current editor,
			// so recover the best we can.
			m_pdfe = NULL;
			return false;
		}
		else
			dypFieldTop += dypField;
	}
	return false; // Did not find another editable field.
}


/*----------------------------------------------------------------------------------------------
	Gets information for the active editor. If pdyp is non-NULL, it sets pdyp to the pixel
	offset of the top of the active field editor from the top of the record, or zero if no
	editor is open.
	@param pdyp Pointer to receive vertical pixel offset. NULL if not used.
	@return An index into the field editors for the currently open editor, or -1 if there is
		no open editor.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::GetEditorIndex(int * pdyp)
{
	AssertPtrN(pdyp);

	int dypMin = GetMinFieldHeight(); // Minimum pixel height of fields.
	int dypFieldTop = 0; // Pixels from top of window to top of field.
	int idfe = 0; // Keep compiler happy.

	if (!m_pdfe)
		goto LNoEditor;

	// Add up the pixel distance from the top of record to the active editor.
	for (; idfe < m_vdfe.Size(); ++idfe)
	{
		if (FieldAt(idfe) == m_pdfe)
		{
			if (pdyp)
				*pdyp = dypFieldTop;
			return idfe;
		}
		dypFieldTop += Max(dypMin, m_vdfe[idfe]->GetHeight());
	}
	Assert(false); // Whoa! We have a m_pdfe that isn't in m_vdfe. Somebody goofed?

LNoEditor:
	if (pdyp)
		*pdyp = 0;
	return -1;
}


/*----------------------------------------------------------------------------------------------
	Release pointers to AfDeSplitChild and field editors so it can be destroyed.
	This is called from AfWnd::WndProcPost on the WM_NCDESTROY message. By the time this is
	called, some things, such as AfDbInfo are already cleared.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::OnReleasePtr()
{
	// Release pointers on all field editors.
	for (int i = m_vdfe.Size(); --i >= 0; )
		if (m_vdfe[i])
			m_vdfe[i]->OnReleasePtr();
	m_qlpi.Clear();
	m_vbsp.Clear();
	SuperClass::OnReleasePtr();
}


/*----------------------------------------------------------------------------------------------
	Close all the embedded editors since the DE window is closing down. It is no good to do this
	in OnReleasePtr() or the destructor, because by that time, child windows have been
	destroyed, and so (for example) we can't recover the current string from an active
	edit box.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::OnDestroy()
{
	CloseAllEditors();
}


/*----------------------------------------------------------------------------------------------
	Handle window messages. Return true if handled.
	See ${AfWnd#FWndProc} for parameter and return descriptions.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(!lnRet);

	switch (wm)
	{
	case WM_LBUTTONDOWN:
		return OnLButtonDown(wp, (int)(short)LOWORD(lp), (int)(short)HIWORD(lp));

	case WM_LBUTTONDBLCLK:
		return OnLButtonDblClk(wp, (int)(short)LOWORD(lp), (int)(short)HIWORD(lp));

	case WM_LBUTTONUP:
		return OnLButtonUp(wp, (int)(short)LOWORD(lp), (int)(short)HIWORD(lp));

	case WM_RBUTTONDOWN:
		return OnRButtonDown(wp, (int)(short)LOWORD(lp), (int)(short)HIWORD(lp));

	case WM_ERASEBKGND:
		return true; // Don't pass message on.

	case WM_MOUSEMOVE:
		return OnMouseMove(wp, (int)(short)LOWORD(lp), (int)(short)HIWORD(lp));

#if 0 // TODO KenZ: Get mouse wheel to work on decrepit mice like Coward's.
	case WM_MOUSEWHEEL:
		{
			int i;
			i = wm;
			lnRet = 1;
			int zDelta;
			zDelta = GET_WHEEL_DELTA_WPARAM(wp);
			int xPos;
			xPos = LOWORD(lp);
			int yPos;
			yPos = HIWORD(lp);
			return true;
		}
#endif

	case WM_VSCROLL:
		return OnVScroll(LOWORD(wp), HIWORD(wp), (HWND)lp);

	case WM_KEYDOWN:
		return OnKeyDown(wp, lp);

	case WM_DESTROY:
		OnDestroy();
		break;

	case WM_TIMER:
		return OnTimer(wp);

	case WM_SYSCOMMAND:
		break;

	case WM_INITMENU:
		return true;

	case WM_COMMAND:
		break;

	case WM_CTLCOLOREDIT:
		// This is called by edit boxes in field editors. It is used to set fore/back colors.
		if (m_pdfe && m_pdfe->Hwnd() == (HWND)lp)
			return m_pdfe->OnColorEdit((HDC)wp, lnRet);
		break;

	case WM_MENUSELECT:
		if (!lp && HIWORD(wp) == 0xFFFF)
		{
			// Menu was closed.
			AfApp::GetMenuMgr()->OnMenuClose();
		}
		return MainWindow()->OnMenuSelect((int)LOWORD(wp), (UINT)HIWORD(wp), (HMENU)lp);

	default:
		break;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet); // Pass message on.
}


/*----------------------------------------------------------------------------------------------
	Getting focus from a higher level (e.g., a window in front of our app closed).
	Pass it down to the active editor, if any.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnSetFocus()
{
	// Note: this has a potential for getting in a loop when an editor is deleted. When an
	// editor is deleted (in EndEdit) the editor window is destroyed. That causes the focus
	// to be switched back to the parent, so we come in here. If m_pdfe is still set at this
	// point, we come in here and try to set the focus back to the field editor. The field
	// editor immediately kills focus which puts the focus back here, etc. until the field
	// editor window actually disappears. This can cause several cycles before it stops.
	// Also, when it finally stops, we frequently fail to process the final kill focus on
	// the editor, which can have bad effects. To get around the problem, immediately prior
	// to calling EndEdit, m_pdfe should first be set to NULL.
	if (m_pdfe)
	{
		::SetFocus(m_pdfe->Hwnd());
	}
	return SuperClass::OnSetFocus();
}



/*----------------------------------------------------------------------------------------------
	This closes the active editor.
	@return true if successful, false if it couldn't be closed.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::CloseEditor()
{
	// Note: this has a potential for getting in a loop when an editor is deleted. When an
	// editor is deleted (in EndEdit) the editor window is destroyed. That causes the focus
	// to be switched back to the parent, so we come in here. If m_pdfe is still set at this
	// point, we come in here and try to set the focus back to the field editor. The field
	// editor immediately kills focus which puts the focus back here, etc. until the field
	// editor window actually disappears. This can cause several cycles before it stops.
	// Also, when it finally stops, we frequently fail to process the final kill focus on
	// the editor, which can have bad effects. To get around the problem, immediately prior
	// to calling EndEdit, m_pdfe should first be set to NULL.
	if (m_pdfe)
	{
		if (!m_pdfe->IsOkToClose())
			return false; // Can't close editor.
		AfDeFieldEditor * pdfe = m_pdfe;
		m_pdfe = NULL;
		pdfe->EndEdit();
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Process the WM_TIMER message used to control tooltips.
	@param nId Identification of timer being processed.
	@return True if the message is processed, or false otherwise.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnTimer(int nId)
{
	Assert(nId == knToolTipTimer); // We only use one timer at this point.


/* Junk for testing tooltips.
			TOOLINFO ti = { isizeof(ti), TTF_IDISHWND | TTF_TRACK | TTF_ABSOLUTE | TTF_TRANSPARENT };
			ti.hwnd = m_hwnd;
			RECT rc = {10, 10, 100, 100};
			ti.rect = rc;
			ti.uId = (uint)m_hwnd;//m_hwndToolTip;
			ti.lpszText = "This is a tooltip test.";
			::ShowWindow(m_hwndToolTip, SW_SHOW);
			::MoveWindow(m_hwndToolTip, 100, 100, 200, 100, true);
			::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, 0, (LPARAM)&ti);
			::SendMessage(m_hwndToolTip, TTM_TRACKPOSITION, 0, MAKELPARAM(10, 10));
			::SendMessage(m_hwndToolTip, TTM_TRACKACTIVATE, true, (LPARAM)&ti);
*/

	// If we are showing tooltips, update the tooltip text.
	if (s_fShowToolTip)
	{
		Rect rc;
		GetClientRect(rc);

		Point pt;
		::GetCursorPos(&pt);
		::ScreenToClient(m_hwnd, &pt);

		// If the mouse is no longer inside the client area, hide the tooltip just in case it
		// is still showing.
		if (!::PtInRect(&rc, pt))
		{
			TOOLINFO ti = { TTTOOLINFOA_V2_SIZE };
			//TOOLINFO ti = { isizeof(ti) };
			ti.hwnd = m_hwnd;
			ti.uId = (uint)m_hwnd;
			::SendMessage(m_hwndToolTip, TTM_TRACKACTIVATE, false, (LPARAM)&ti);
//OutputDebugString("Turn active off\n");
			s_fShowToolTip = false;
			if (AfApp::Papp())
				AfApp::Papp()->SuppressIdle(); // Suppress OnIdle() for TTM_TRACKACTIVATE.
		}
	}
	if (AfApp::Papp())
		AfApp::Papp()->SuppressIdle(); // Suppress OnIdle() for WM_TIMER.
	return true;
}


/*----------------------------------------------------------------------------------------------
	Process commands. Return true if processed.
	See ${AfWnd#OnCommand} for parameter and return descriptions.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnCommand(int cid, int nc, HWND hctl)
{
	// If we have a control handle, call OnNotifyChild to process command.
	if (hctl)
	{
		// Convert to a notify message.
		NMHDR nmh;
		nmh.hwndFrom = hctl;
		nmh.idFrom = cid;
		nmh.code = nc;
		long lnT;
		if (OnNotifyChild(nmh.idFrom, &nmh, lnT))
			return true;
	}

	return SuperClass::OnCommand(cid, nc, hctl);
}


/*----------------------------------------------------------------------------------------------
	Show help (What's This Help) information for the tree item that the mouse is over.
	@param pt Point relative to the screen.
	@param pptss Pointer to receive the help string.
	@return True if the mouse is over a field label and we are returning a string.
		False if we aren't over a field label.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	*pptss = NULL;

	::ScreenToClient(m_hwnd, &pt);
	// If we are over the tree.
	if (pt.x < GetBranchWidthAt(pt.y))
	{
		int idfe = GetField(pt.y);
		// If we are over a field label.
		if (idfe < m_vdfe.Size())
		{
			AfDeFieldEditorPtr qdfe = FieldAt(idfe);
			AssertObj(qdfe);

			// String contains bold label, period, and help string from field editor.
			ITsIncStrBldrPtr qtisb;
			qtisb.CreateInstance(CLSID_TsIncStrBldr);
			ITsStringPtr qtss;
			qdfe->GetLabel(&qtss);
			// To get bold to "take" we either need to append Unicode characters, or
			// switch to using a string builder and setting the bold property.
			const OLECHAR * pch;
			int cch;
			CheckHr(qtss->LockText(&pch, &cch));
			CheckHr(qtisb->SetIntPropValues(ktptBold, ktpvEnum, kttvForceOn));
			CheckHr(qtisb->AppendRgch(pch, cch));
			CheckHr(qtss->UnlockText(pch));
			CheckHr(qtisb->AppendRgch(L" ", 1));
			CheckHr(qtisb->SetIntPropValues(ktptBold, ktpvEnum, kttvOff));
			qdfe->GetHelp(&qtss);
			CheckHr(qtisb->AppendTsString(qtss));
			CheckHr(qtisb->GetString(pptss)); // Return the TsString.
			return true;
		}
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Process cange in window size (WM_SIZE).
	See ${AfWnd#OnSize} for parameter and return descriptions.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnSize(int wst, int dxp, int dyp)
{
	// If the change decreases the data pane below its minimum value, decrease the tree pane,
	// if possible, to maintain the minimum-sized data pane.
	if (m_dxpTreeWidth + kdxpMinDataWidth > dxp && dxp - kdxpMinDataWidth >= kdxpMinTreeWidth)
		m_dxpTreeWidth = dxp - kdxpMinDataWidth;
	SetHeight(); // Calculate the new height of fields.
	::InvalidateRect(m_hwnd, NULL, true);
	return true; // Don't pass message on.
}


/*----------------------------------------------------------------------------------------------
	Process key down messages (WM_KEYDOWN). Handles ESC to cancel tree/field drags and PgUp
	and PgDown keys for scrolling.
	@param wp Virtual key code.
	@param lp Key data: scan code, repeat info, flags.
	@return Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnKeyDown(WPARAM wp, LPARAM lp)
{
	// If we are dragging the tree width, cancel the drag.
	if (wp == VK_ESCAPE && m_fChangingTreeWid)
	{
		m_dxpTreeWidth = s_dxpOldTreeWidth;
		SetHeight();
		::InvalidateRect(m_hwnd, NULL, false);
		OnLButtonUp(0, 0, 0);
		return true; // Don't pass the message on.
	}
	// PageUp key scrolls up a page.
	else if (wp == VK_PRIOR)
	{
		OnVScroll(SB_PAGEUP, 0, 0); // Last two args are not used.
		return true;
	}
	// PageDown key scrolls down a page.
	else if (wp == VK_NEXT)
	{
		OnVScroll(SB_PAGEDOWN, 0, 0); // Last two args are not used.
		return true;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Process left button down (WM_LBUTTONUP). Handles release of tree/field drags.
	@param grfmk Indicates whether various virtual keys are down.
	@param xp The x-coord of the mouse relative to the upper-left corner of the client.
	@param yp The y-coord of the mouse relative to the upper-left corner of the client.
	@return Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnLButtonUp(uint grfmk, int xp, int yp)
{
	// If we are dragging the tree width, release the mouse capture.
	if (m_fChangingTreeWid)
	{
		::ReleaseCapture();
		m_fChangingTreeWid = false;
		::ClipCursor(NULL);
		if (m_pdfe)
			::SetFocus(m_pdfe->Hwnd());
		return true; // Don't pass message on.
	}
	return false; // Pass message on.
}

/*----------------------------------------------------------------------------------------------
	Returns the width of a single tree branch given the pointer to the corresponding field
	editor.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::GetBranchWidth(AfDeFieldEditor * pdfe)
{
	if (!m_fAlignFieldsToTree)
	{
		return m_dxpTreeWidth;
	}
	else
	{
		ITsStringPtr qtss;
		pdfe->GetLabel(&qtss);
		SmartBstr sbstr;
		qtss->get_Text(&sbstr);

		int dxpStringWidth = 0;
		int cch = sbstr.Length();
		if (cch)
		{
			SIZE size;
			HDC hdc = ::GetDC(NULL);
			HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));

			if (!::GetTextExtentPoint32W(hdc, sbstr.Chars(), cch, &size))
			{
				DWORD error = ::GetLastError();
				AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
				int iSuccess;
				iSuccess = ::ReleaseDC(NULL, hdc);
				Assert(iSuccess);
				Warn("GetTextExtent failed");
				// re-set last error so that we can get the description for it...
				::SetLastError(error);
				ReturnHrEx(E_FAIL);
			}

			dxpStringWidth = size.cx;
			AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
			int iSuccess;
			iSuccess = ::ReleaseDC(NULL, hdc);
			Assert(iSuccess);
			// REVIEW RonM (DavidO): We might want to add some padding pixels (kdxpRtTreeGap) we may want to add more....
			return kdxpLeftMargin + (pdfe->GetIndent() + 1) * kdxpIndDist + dxpStringWidth +
				kdxpRtTreeGap;
		}

		// REVIEW RonM (DavidO): We might want to add some padding pixels (kdxpRtTreeGap) we may want to add more....
		return kdxpLeftMargin + (pdfe->GetIndent() + 1) * kdxpIndDist;
	}
}

/*----------------------------------------------------------------------------------------------
	Returns the width of a single tree branch given the index of the corresponding field editor.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::GetBranchWidth(int idfe)
{
	if (!m_fAlignFieldsToTree)
	{
		return m_dxpTreeWidth;
	}
	else
	{
		AfDeFieldEditor * pdfe = FieldAt(idfe);
		return GetBranchWidth(pdfe);
	}
}

/*----------------------------------------------------------------------------------------------
	Returns the width of a single tree branch given the vertical coordinate (as passed to
	routines like OnLButtonDown). Returns 0 if there are no branches.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::GetBranchWidthAt(int yp)
{
	if (!m_fAlignFieldsToTree)
	{
		return m_dxpTreeWidth;
	}
	else if (m_vdfe.Size() == 0)
	{
		return 0;
	}
	else
	{
		int idfe = GetField(yp);
		if (idfe >= m_vdfe.Size())
			return 0;
		else
			return GetBranchWidth(idfe);
	}
}

/*----------------------------------------------------------------------------------------------
	Create an active editor window for the chosen field. The previous editor should have been
	closed prior to this.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::CreateActiveEditorWindow(AfDeFieldEditor * pdfe, int dypFieldTop)
{
	Rect rc;
	::GetClientRect(m_hwnd, &rc);

	int dypMin = GetMinFieldHeight();
	// Get the size of the edit area, excluding bottom divider line.
	Rect rcEdit(GetBranchWidth(pdfe), dypFieldTop, rc.right,
		dypFieldTop + Max(dypMin, pdfe->GetHeight()) - 1);

	Assert(!m_pdfe);
	if (pdfe->BeginEdit(m_hwnd, rcEdit))
		m_pdfe = pdfe;
}

/*----------------------------------------------------------------------------------------------
	Override this if L mouse button in an editor should do something besides
	creating an active editor window.
	By the time this is called, the old active editor is closed and we've
	dealt with expanding, dragging, etc.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::OnLButtonDownInEditor(int idfe, AfDeFieldEditor * pdfe, int dypFieldTop,
	uint grfmk, int xp, int yp)
{
	CreateActiveEditorWindow(pdfe, dypFieldTop);
	// Now, we need to simulate a mouse down in the new window to place cursor.
	// This simulates a hardware mouse down, so if the buttons are reversed, we need to
	// simulate a right click instead.
#if 0
	::mouse_event(
		(GetSystemMetrics(SM_SWAPBUTTON) ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_LEFTDOWN),
		0, 0, 0, 0);
#else
	if (pdfe->Hwnd())
	{
		// There is an active editor...simulate a click in it.
		POINT pt = {xp, yp};
		::ClientToScreen(Hwnd(), &pt);
		::ScreenToClient(pdfe->Hwnd(), &pt);
		::SendMessage(pdfe->Hwnd(), WM_LBUTTONDOWN, grfmk, MAKELPARAM(pt.x, pt.y));

		pdfe->SaveFullCursorInfo();
	}

#endif
}


/*----------------------------------------------------------------------------------------------
	Override this if R mouse button in an editor should do something besides
	creating an active editor window.
	By the time this is called, the old active editor is closed and we've
	dealt with expanding, dragging, etc.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::OnRButtonDownInEditor(int idfe, AfDeFieldEditor * pdfe, int dypFieldTop,
	uint grfmk, int xp, int yp)
{
	CreateActiveEditorWindow(pdfe, dypFieldTop);
	// Now, we need to simulate a mouse down in the new window to place cursor.
	// This simulates a hardware mouse down, so if the buttons are reversed, we need to
	// simulate a right click instead.
#if 0
	::mouse_event(
		(GetSystemMetrics(SM_SWAPBUTTON) ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_RIGHTDOWN),
		0, 0, 0, 0);
#else
	if (pdfe->Hwnd())
	{
		// There is an active editor...simulate a click in it.
		POINT pt = {xp, yp};
		::ClientToScreen(Hwnd(), &pt);
		::ScreenToClient(pdfe->Hwnd(), &pt);
		::SendMessage(pdfe->Hwnd(), WM_RBUTTONDOWN, grfmk, MAKELPARAM(pt.x, pt.y));
		//::SendMessage(pdfe->Hwnd(), WM_LBUTTONUP, grfmk, MAKELPARAM(pt.x, pt.y));
	}

#endif
}


/*----------------------------------------------------------------------------------------------
	Process left button down (WM_LBUTTONDOWN). Starts a tree drag, a label drag, outline toggle,
	and activating an editor window.
	@param grfmk Indicates whether various virtual keys are down.
	@param xp The x-coord of the mouse relative to the upper-left corner of the client.
	@param yp The y-coord of the mouse relative to the upper-left corner of the client.
	@return Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnLButtonDown(uint grfmk, int xp, int yp)
{
	// If we are close to the tree edge, start a drag of the edge, setting the capture so
	// that we can limit the dragging area.
	if (CloseToTreeSeparator(xp))
	{
		s_dxpOldTreeWidth = m_dxpTreeWidth; // Save old value in case ESC is hit.
		m_fChangingTreeWid = true;
		::SetCapture(m_hwnd);
		::SetFocus(m_hwnd); // Allows us to catch ESC for cancel.
		Rect rc;
		::GetClientRect(m_hwnd, &rc);
		::MapWindowPoints(m_hwnd, NULL, (POINT *)&rc, 2);
		if (rc.Width() > kdxpMinTreeWidth + kdxpMinDataWidth)
		{
			// The window is wide enough to allow the border to be adjusted
			rc.left += kdxpMinTreeWidth;
			rc.right -= kdxpMinDataWidth;
		}
		else
		{
			// Lock the mouse so it can't be dragged, since the window is too narrow.
			// This allows a slight jump before it locks, since the mouse
			// drag active area is more than one pixel. It also causes the cursor to
			// jump to the left if the data pane is too small. Normally this shouldn't be
			// observed since the minimum size of the window will keep it from happening.
			rc.left += xp + 1;
			rc.right = rc.left + 1;
		}
		// Keep the mouse inside the client window.
		// Caution! Dell Inspiration notebooks running Windows 2000 accept ClipCursor and
		// return the correct rectangle from GetClipCursor prior to leaving this function,
		// but by the time we get a WM_MOUSEMOVE message, GetClipCursor resorts to the
		// full screen instead of what we set here. So we cannot rely on ClipCursor working
		// on all machines. Argh!
		::ClipCursor(&rc);
		::SetCursor(::LoadCursor(NULL, IDC_SIZEWE));
		return true; // Don't pass message on.
	}

	int dypFieldTop;
	int idfe = GetField(yp, &dypFieldTop);

	// Ignore clicks below field editors.
	if (idfe >= m_vdfe.Size())
		return false; // Pass message on.

	AfDeFieldEditor * pdfe = FieldAt(idfe); // Get current editor.

	if (pdfe == m_pdfe && xp >= GetBranchWidth(pdfe))
	{
		// This should have been a click inside the active window that replaces
		// the field editor. However, this one apparently didn't make a window that
		// entirely fills its allocated space. Give it a chance to handle the click.
		// (For an example of a field editor that needs this, see AfDeFeComboBox.
		// A click on the very left edge caused a problem.)
		POINT pt = {xp, yp};
		::ClientToScreen(m_hwnd, &pt);

		return m_pdfe->ActiveClick(pt);
	}

	// Close previous editor, if open.
	if (!CloseEditor())
		return false;
	// This assert makes sure the closing process hasn't opened another
	// editor. This will result in an invalid state with an extra editor
	// open. The CloseEditor process should never open a new editor in the
	// process. For example, synchronization needs to occur outside of
	// CloseEditor() to avoid unwanted recursion.
	Assert(!m_pdfe);

	// xpText is the left side of the tree label.
	int xpText = kdxpLeftMargin + kdxpIndDist + pdfe->GetIndent() * kdxpIndDist;
	// If we are over the +/- box...
	if (xp < xpText && xp >= xpText - kdxpIndDist)
	{
		// Handle label expansion and contraction.
		if (pdfe->GetExpansion() != kdtsFixed)
			return ToggleExpansionAndScroll(idfe, dypFieldTop);
	}
	// Else if we are over an editable field editor and the previous editor closed OK...
	else if (xp > GetBranchWidth(pdfe) && pdfe->IsEditable() && !m_pdfe)
	{
		SwitchFocusHere();
		OnLButtonDownInEditor(idfe, pdfe, dypFieldTop, grfmk, xp, yp);
		return true; // Don't pass message on.
	}
	else
		return OnBeginDragDrop(pdfe, xp, yp);

	return false; // Pass message on.
}


/*----------------------------------------------------------------------------------------------
	Process right button down (WM_LBUTTONDOWN). Activates an editor window if needed.
	@param grfmk Indicates whether various virtual keys are down.
	@param xp The x-coord of the mouse relative to the upper-left corner of the client.
	@param yp The y-coord of the mouse relative to the upper-left corner of the client.
	@return Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnRButtonDown(uint grfmk, int xp, int yp)
{
	// If we are close to the tree edge, pass the message on.
	if (CloseToTreeSeparator(xp))
		return false;

	int dypFieldTop;
	int idfe = GetField(yp, &dypFieldTop);

	// Ignore clicks below field editors.
	if (idfe >= m_vdfe.Size())
		return false; // Pass message on.

	AfDeFieldEditor * pdfe = FieldAt(idfe); // Get current editor.

	if (pdfe == m_pdfe && xp >= GetBranchWidth(pdfe))
	{
		// This should have been a click inside the active window that replaces
		// the field editor. However, this one apparently didn't make a window that
		// entirely fills its allocated space. Give it a chance to handle the click.
		// (For an example of a field editor that needs this, see AfDeFeComboBox.
		// A click on the very left edge caused a problem.)
		POINT pt = {xp, yp};
		::ClientToScreen(m_hwnd, &pt);

		return m_pdfe->ActiveClick(pt);
	}
	// Close previous editor, if open.
	if (!CloseEditor())
		return false;

	// If we are over an editable field editor and the previous editor closed OK...
	if (xp > GetBranchWidth(pdfe) && pdfe->IsEditable() && !m_pdfe)
	{
		OnRButtonDownInEditor(idfe, pdfe, dypFieldTop, grfmk, xp, yp);
		return true; // Don't pass message on.
	}

	return false; // Pass message on.
}


/*----------------------------------------------------------------------------------------------
	Process left button double clicks (WM_LBUTTONDBLCLK). If a user expands and contracts
	outlines quickly, sometimes the system interprets their action as a double click instead
	of a single click. Thus we need to handle outline expansion both in single click and double
	click handlers.
	@param grfmk Indicates whether various virtual keys are down.
	@param xp The x-coord of the mouse relative to the upper-left corner of the client.
	@param yp The y-coord of the mouse relative to the upper-left corner of the client.
	@return Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnLButtonDblClk(uint grfmk, int xp, int yp)
{
	int dypFieldTop;
	int idfe = GetField(yp, &dypFieldTop);
	// If the mouse is over a field.
	if (idfe < m_vdfe.Size())
	{
		AfDeFieldEditor * pdfe = FieldAt(idfe); // Get current editor.

		// If the mouse is over a checkbox, toggle the expansion state.
		int xpText = kdxpLeftMargin + kdxpIndDist + pdfe->GetIndent() * kdxpIndDist;
		if (xp < GetBranchWidth(pdfe) && xp > xpText - kdxpIndDist)
		{
			if (pdfe->GetExpansion() != kdtsFixed)
				return ToggleExpansionAndScroll(idfe, dypFieldTop);
		}
	}

	return false; // Pass message on.
}


/*----------------------------------------------------------------------------------------------
	Start an object drag drop operation that occurs when a person starts to drag a label.
	@param pdfe Pointer to the field editor on which we clicked.
	@param xp The x-coord of the mouse relative to the upper-left corner of the client.
	@param yp The y-coord of the mouse relative to the upper-left corner of the client.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnBeginDragDrop(AfDeFieldEditor * pdfe, int xp, int yp)
{
	AssertPtr(pdfe);

	// Assume the user wants to begin to drag an entry or subentry to a reference.
	DWORD dwEffect;
	IDropSourcePtr qdsrc;
	HRESULT hr;
	IgnoreHr(hr = QueryInterface(IID_IDropSource, (void **)&qdsrc));
	if (FAILED(hr))
		return false;

	HVO hvo = GetDragObject(pdfe);

	// Get the class of the object we want to drag.
	CustViewDaPtr qcvd;
	m_qlpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	int clid;
	CheckHr(qcvd->get_ObjClid(hvo, &clid));

	ITsStringPtr qtss;

	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(prmw);
	prmw->GetDragText(hvo, clid, &qtss);

	AfDbInfo * pdbi = m_qlpi->GetDbInfo();

	// Store information on the object we are dragging.
	IDataObjectPtr qdobj;
	CmDataObject::Create(pdbi->ServerName(), pdbi->DbName(), hvo, clid, qtss,
		(int)::GetCurrentProcessId(), &qdobj);

	// Do the DragDrop operation.
	// The return value depends on what the drop method returned.
	CheckHr(hr = DoDragDrop(qdobj, qdsrc, DROPEFFECT_COPY | DROPEFFECT_MOVE | DROPEFFECT_LINK,
		&dwEffect));
	// If we do a move, we need to clean up windows in this application.
	// Actually, we only need to do this if it is a different application from the target,
	// but how do we know that?
	if (hr == DRAGDROP_S_DROP && dwEffect & DROPEFFECT_MOVE)
	{
		int x;
		x = 3;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Get the object we want to drag, given the target field.
	@param pdfe The field editor over which the user clicked.
	@return The HVO of the object we want to drag.
----------------------------------------------------------------------------------------------*/
HVO AfDeSplitChild::GetDragObject(AfDeFieldEditor * pdfe)
{
	AssertPtr(pdfe);

	// Get the object we are dragging. Tree nodes drag the object in the tree. all others
	// drag the object owning the field.
	HVO hvo;
	AfDeFeTreeNode * pdetn = dynamic_cast<AfDeFeTreeNode *>(pdfe);
	if (pdetn)
		hvo = pdetn->GetTreeObj();
	else
		hvo = pdfe->GetOwner();

	return hvo;
}


/*----------------------------------------------------------------------------------------------
	Toggle the expansion state of a tree node.
	@param idfe Index of the field editor node we are toggling.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::ToggleExpansion(int idfe)
{
	AfDeFeNode * pden = dynamic_cast<AfDeFeNode *>(FieldAt(idfe));
	AssertPtr(pden); // Shouldn't call this on a non-tree item.

	DeTreeState dts = pden->GetExpansion();
	pden->SetExpansion(dts == kdtsExpanded ? kdtsCollapsed : kdtsExpanded);
	int nInd = pden->GetIndent();

	if (dts == kdtsCollapsed)
	{
		// Get the main custom view DA shared by all windows
		CustViewDaPtr qcvd;
		m_qlpi->GetDataAccess(&qcvd);
		AfDeFeTreeNode * pdetn = dynamic_cast<AfDeFeTreeNode *>(pden);
		if (pdetn)
		{
			ClsLevel clev(pdetn->GetClsid(), 0);
			AddFields(pdetn->GetTreeObj(), clev, qcvd, idfe + 1, nInd + 1);
		}
		else
		{
			AfDeFeVectorNode * pdevn = dynamic_cast<AfDeFeVectorNode *>(pden);
			AssertPtr(pdevn);	// It better be a vector node.
			int idfeSub = idfe;
			HVO hvoOwner = pdevn->GetOwner();
			int clsid;
			CheckHr(qcvd->get_IntProp(hvoOwner, kflidCmObject_Class, &clsid));
			ClsLevel clev(clsid, 0);
			RecordSpecPtr qrsp;
			m_quvs->m_hmclevrsp.Retrieve(clev, qrsp);
			AssertPtr(qrsp);
			BlockSpec * pbsp;
			for (int ibsp = 0; ibsp < qrsp->m_vqbsp.Size(); ++ibsp)
			{
				pbsp = qrsp->m_vqbsp[ibsp];
				if (pbsp->m_flid == pdevn->GetOwnerFlid() && pbsp->m_ft != kftSubItems)
					break;
			}
			AddField(hvoOwner, clsid, 0, pbsp->m_vqfsp[0], qcvd, ++idfeSub, nInd + 1, true);
		}
	}
	else
	{
		// Release all indented field editors, if there are any.
		if (idfe < m_vdfe.Size() - 1
			&& m_vdfe[idfe + 1]->GetIndent() == nInd + 1)	// See if next one is indented.
		{
			int idfeLast = LastFieldAtSameIndent(idfe + 1);
			for (; idfeLast > idfe; --idfeLast)
			{
				if (m_vdfe[idfe])
				{
					m_vdfe[idfeLast]->OnReleasePtr();
					m_vdfe[idfeLast]->Release();
				}
				m_vdfe.Delete(idfeLast);
			}
		}
	}
	SetTreeHeader(pden);
}


/*----------------------------------------------------------------------------------------------
	Toggle the expansion state of a node, then attempt to scroll to get the expanded record
	fully visible on the screen.
	@param idfe Index of the field editor node we are toggling.
	@param dypFieldTop The pixels from the top of the record to the top of this field.
	@return Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::ToggleExpansionAndScroll(int idfe, int dypFieldTop)
{
	// Have the subclass expand or contract the fields.
	ToggleExpansion(idfe);
	Rect rc;
	::GetClientRect(m_hwnd, &rc);
	SetHeight(); // Set heights on all fields.
	// If we expanded a node, scroll down, if necessary, to get the subrecord
	// to fit on the screen. If it is larger than the screen, position it at
	// the top of the screen.
	if (FieldAt(idfe)->GetExpansion() == kdtsExpanded)
	{
		// Calculate the height of the subrecord
		// This needs to allow for the possibility that expanding it didn't actually create
		// any more records.
		int idfeMax = idfe;
		if (idfe + 1 < m_vdfe.Size() &&  m_vdfe[idfe + 1] &&
			m_vdfe[idfe + 1]->GetIndent() > m_vdfe[idfe]->GetIndent())
		{
			idfeMax = LastFieldAtSameIndent(idfe + 1);
		}
		int dypSubRec = 0;
		for (int idfeT = idfe; idfeT <= idfeMax; ++idfeT)
			dypSubRec += FieldAt(idfeT)->GetHeight();
		// Now see if we have room in the window.
		SCROLLINFO si = {isizeof(si), SIF_PAGE | SIF_POS | SIF_RANGE};
		GetScrollInfo(SB_VERT, &si);
		int dypSubRecMax = dypFieldTop + dypSubRec;
		if (dypSubRecMax > rc.bottom)
		{
			// The subrecord won't fit.
			if (dypSubRec > rc.Height())
				// The subrecord is too big for the window, so start it at the top.
				si.nPos += dypFieldTop;
			else
				// The subrecord will fit the window. Move it up enough to fit.
				si.nPos += dypSubRecMax - rc.bottom;
			SetScrollInfo(SB_VERT, &si, true);
		}
	}
	::InvalidateRect(m_hwnd, NULL, false);
	return true; // Don't pass message on.
}


/*----------------------------------------------------------------------------------------------
	Process mouse move (WM_MOUSEMOVE).
	@param grfmk Indicates shift key and mouse button states.
	@param xp The x-coord of the mouse relative to the upper-left corner of the client.
	@param yp The y-coord of the mouse relative to the upper-left corner of the client.
	@return Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnMouseMove(uint grfmk, int xp, int yp)
{
	AssertObj(this);

	// If we are changing the tree width, move the border and update the window.
	if (m_fChangingTreeWid)
	{
		Assert(!m_fAlignFieldsToTree);
		RECT rc;
		::GetClientRect(m_hwnd, &rc);
		::SetCursor(::LoadCursor(NULL, IDC_SIZEWE));
		// NOTE: Even though we have ClipCursor in effect, Dell Inspiration notebooks running
		// Windows 2000 have lost track of that fact by the time we get here. Therefore, to
		// handle these brain-dead computers, we need to force xp into range before using it.
		xp = max(xp, kdxpMinTreeWidth);
		xp = min(xp, max(kdxpMinTreeWidth, rc.right - kdxpMinDataWidth));
		m_dxpTreeWidth = xp;
		SetHeight();
		::InvalidateRect(m_hwnd, NULL, true);
		::UpdateWindow(m_hwnd);
		// Need to go two parents up, since one level up is the AfSubClientSplitterWnd.
		AfClientRecDeWnd * pcrde = dynamic_cast<AfClientRecDeWnd *>(Parent()->Parent());
		AssertPtr(pcrde);
		pcrde->OnTreeWidthChanged(m_dxpTreeWidth, this);
		return true;
	}

	Rect rc;
	GetClientRect(rc);
	rc.right = rc.left + GetBranchWidthAt(yp);

	TOOLINFO ti = { TTTOOLINFOA_V2_SIZE };
	//TOOLINFO ti = { isizeof(ti) };
	ti.hwnd = m_hwnd;
	ti.uId = (uint)m_hwnd;

	Point pt(xp, yp);
	int dypStart;
	int idfe = GetField(pt.y, &dypStart);

//StrApp strx;
	s_fShowToolTip = false;
	// If the mouse is over the tree pane and over a field editor.
	if (::PtInRect(&rc, pt) && idfe < m_vdfe.Size())
	{
		AfDeFieldEditorPtr qdfe = FieldAt(idfe);
		AssertObj(qdfe);
		StrAnsi sta;

		int nIndent = qdfe->GetIndent();
		// Get the width of the label text.
		SmartBstr sbstr;
		ITsStringPtr qtss;
		qdfe->GetLabel(&qtss);
		qtss->get_Text(&sbstr);
		SIZE size;
		HDC hdc = ::GetDC(m_hwnd);
		HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
		::GetTextExtentPoint32W(hdc, sbstr.Chars(), sbstr.Length(), &size);
		AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
		int iSuccess;
		iSuccess = ::ReleaseDC(m_hwnd, hdc);
		Assert(iSuccess);
		int xpLabel = kdxpLeftMargin + (nIndent + 1) * kdxpIndDist;

		// If the mouse is over a tree label, dypStart will be the top point of
		// label's text. Since tree labels are vertically centered within the height
		// of the first line of a field, we need to make sure dypStart takes that
		// fact into consideration. Hence the following calculation. - DavidO
		if (qdfe->GetFontHeight() > m_dypDefFieldHeight)
			dypStart += (((qdfe->GetFontHeight() - m_dypDefFieldHeight) / 2));

		// Only show the popup if the mouse is over the label text and the tree width
		// is too small to show the entire label.
		if (pt.x >= xpLabel &&
			pt.y >= dypStart && pt.y < dypStart + m_dypDefFieldHeight &&
			xpLabel + size.cx + kdxpRtTreeGap > GetBranchWidthAt(yp))
		{
			s_fShowToolTip = true;

			StrAppBuf strb = sbstr.Chars();
			ti.lpszText = const_cast<achar *>(strb.Chars());
			::SendMessage(m_hwndToolTip, TTM_UPDATETIPTEXT, 0, (LPARAM)&ti);
			//::SendMessage(m_hwndToolTip, TTM_UPDATE, 0, 0);
//strx.Format("setting text: %s\n", strb.Chars());
//OutputDebugString(strx.Chars());
			Rect rcLabel(xpLabel, dypStart);
			Point ptLabel(rcLabel.TopLeft());
			::ClientToScreen(m_hwnd, &ptLabel);
			::SendMessage(m_hwndToolTip, TTM_TRACKPOSITION, 0,
				MAKELPARAM(ptLabel.x, ptLabel.y));
		}
	}

	// Turn the tool tip on or off.
	::SendMessage(m_hwndToolTip, TTM_TRACKACTIVATE, s_fShowToolTip, (LPARAM)&ti);
//strx.Format("activate:%d\n", s_fShowToolTip);
//OutputDebugString(strx.Chars());
	// If we are close to the tree border, set the mouse to a sizing cursor.
	if (CloseToTreeSeparator(xp))
	{
		::SetCursor(::LoadCursor(NULL, IDC_SIZEWE));
		s_fShowToolTip = false;
//OutputDebugString("setting false\n");
	}
	// Else if we are over the tree, set the mouse to an arrow cursor.
	else if (xp < GetBranchWidthAt(yp))
		::SetCursor(::LoadCursor(NULL, IDC_ARROW));
	else if (idfe < m_vdfe.Size())
	{
		// If we are over a field.
		AfDeFieldEditor * pdfe = FieldAt(idfe);
		AssertPtr(pdfe);
		// Allow field editor to override mouse cursor.
		if (pdfe->OnMouseMove(grfmk, xp, yp))
			return true; // Cursor is already handled.
		if (pdfe->IsEditable())
			// Over an editable field, change cursor to I-beam.
			::SetCursor(::LoadCursor(NULL, IDC_IBEAM));
		else
			::SetCursor(::LoadCursor(NULL, IDC_ARROW));
	}
	else
		::SetCursor(::LoadCursor(NULL, IDC_ARROW));

	return true; // Don't pass message on.
}


/*----------------------------------------------------------------------------------------------
	Continue processing DragOver while dragging a field over the tree. Check for a valid target,
	and if found, produce a highlight on, above, or below the target label. This also sets
	various member variables indicating information about the target.
	@param xp The x-coord of the mouse relative to the upper-left corner of the client.
	@param yp The y-coord of the mouse relative to the upper-left corner of the client.
	@return True if we have a valid drop target, or false otherwise.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnDragOverTree(int xp, int yp)
{
	HDC hdc;
	int nrop;
	SIZE size;
	HFONT hfontOld;
	LOGBRUSH lbr = {BS_SOLID, kclrWhite, 0};
	HBRUSH hbr;
	HBRUSH hbrOld;
	HPEN hpen;
	HPEN hpenOld;
	ITsStringPtr qtss;
	SmartBstr sbstr;

	Point pt(xp, yp);
	::ClientToScreen(m_hwnd, &pt);

	// Save previous target info for comparison.
	int drp = m_drp;
	int idfeDst = m_idfeDst;

	// Determine whether we have a valid target.
	if (!GetDropTarget(yp))
	{
		ClearTarget(pt.x, pt.y);
		return false;
	}

	// If the target hasn't changed, we don't need to do anything else.
	if (idfeDst == m_idfeDst && drp == m_drp && m_xpMrkLeft)
		return true;

	// If we come into the window from the bottom, we can get pdfeDst = m_vdfe.Size().
	AfDeFieldEditor * pdfeDst = idfeDst < m_vdfe.Size() ? m_vdfe[idfeDst] : 0;

	// If we have two targets together at the same indent, and we are moving from the
	// top of one to the bottom of the other, then skip the second target.
	if (drp == kdrpAbove && m_drp == kdrpBelow)
	{
		if (m_vdfe.Size() > m_idfeDst + 1)
		{
			AfDeFieldEditor * pdfe = m_vdfe[m_idfeDst + 1];
			if (pdfe == pdfeDst && pdfe->GetIndent() == m_pdfeDst->GetIndent())
				return true;
		}
	}
	// If we have two targets together at the same indent, and we are moving from the
	// bottom of one to the top of the other, then skip the second target.
	if (drp == kdrpBelow && m_drp == kdrpAbove)
	{
		if (m_idfeDst)
		{
			AfDeFieldEditor * pdfe = m_vdfe[m_idfeDst - 1];
			if (pdfe == pdfeDst && pdfe->GetIndent() == m_pdfeDst->GetIndent())
				return true;
		}
	}

	// We have a new valid move target.
	AfDeFieldEditor * pdfe = dynamic_cast<AfDeFieldEditor *>(m_vdfe[m_idfeDst]);
	ClearTarget(xp, yp); // Clear out the old marker.
	hdc = ::GetDC(m_hwnd);
	// Get the size of the label.
	hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));
	pdfe->GetLabel(&qtss);
	qtss->get_Text(&sbstr);
	::GetTextExtentPoint32W(hdc, sbstr.Chars(), sbstr.Length(), &size);
	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);

	// Set up the pen and brush.
	// Note at least one machine I tested (FwTester) failed to use XOR on pens, but used
	// it properly on brushes. Thus, for consistent operation across all machines I've
	// made the pen invisible and only use the brush for this rectangle.
	hbr = AfGdi::CreateBrushIndirect(&lbr);
	hbrOld = AfGdi::SelectObjectBrush(hdc, hbr);
	hpen = ::CreatePen(PS_NULL, 0, ::GetSysColor(COLOR_BTNTEXT));
	hpenOld = (HPEN)::SelectObject(hdc, hpen);
	nrop = ::SetROP2(hdc, R2_XORPEN); // Use XOR to mark the target.

	// Mark the destination target with a line above the label when dropping above,
	// a line below the label when dropping below, and highlighting the label if dropping
	// in a label (e.g., appending the moved field to the end of any other fields).
	switch (m_drp)
	{
	case kdrpOn:
		// The drop target is in the specified field. Highlight the label.
		m_xpMrkLeft = kdxpLeftMargin - 1 + (pdfe->GetIndent() + 1) * kdxpIndDist;
		m_ypMrkTop = m_ypTopDst + 1;
		m_xpMrkRight = min(GetBranchWidth(pdfe), m_xpMrkLeft + size.cx + 3);
		m_ypMrkBottom = m_ypMrkTop + size.cy + 1;
		break;
	case kdrpBelow:
	case kdrpAtEnd:
		// The drop target is below the specified field. Draw a line under the label.
		m_xpMrkLeft = kdxpLeftMargin + pdfe->GetIndent() * kdxpIndDist;
		m_ypMrkTop = m_ypTopDst + size.cy;
		m_xpMrkRight = GetBranchWidth(pdfe) - 3;
		m_ypMrkBottom = m_ypMrkTop + 4;
		break;
	case kdrpAbove:
		// The drop target is above the specified field. Draw a line above the label.
		m_xpMrkLeft = kdxpLeftMargin + pdfe->GetIndent() * kdxpIndDist;
		m_ypMrkTop = m_ypTopDst - 2;
		m_xpMrkRight = GetBranchWidth(pdfe) - 3;
		m_ypMrkBottom = m_ypTopDst + 2;
		break;
	default:
		Assert(false);
		break;
	}

	// Mark the new target location.
	::ImageList_DragLeave(NULL);
	::Rectangle(hdc, m_xpMrkLeft, m_ypMrkTop, m_xpMrkRight, m_ypMrkBottom);
	::ImageList_DragEnter(NULL, 0, 0);
	::ImageList_DragMove(pt.x, pt.y);

	// Clean up resources.
	::SelectObject(hdc, hpenOld);
	AfGdi::SelectObjectBrush(hdc, hbrOld, AfGdi::OLD);
	::DeleteObject(hpen);
	AfGdi::DeleteObjectBrush(hbr);
	::SetROP2(hdc, nrop);
	int iSuccess;
	iSuccess = ::ReleaseDC(m_hwnd, hdc);
	Assert(iSuccess);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Determines whether the current location is a valid drop location.
	There are a few places where this will still provide a valid target
	that results in moving an item to its same location. (e.g., moving the last subentry into
	its major entry). However, this code is already very complex, and I don't think the
	added complexity to catch remaining situations is worth the effort since moving an item
	to the same location will simply do a slight amount of unnecessary work.

	@param yp Vertical mouse position in client pixels.
	@return true if the target field and cursor location is a valid place to drop the moved
		field.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::GetDropTarget(int yp)
{
	// Get the target field.
	int dypFieldTop; // The top pixel of the field
	int dypExtra; // The cursor position from the top of the field.
	int idfe = GetField(yp, &dypFieldTop, &dypExtra);

	// Save the results, even if not valid, so that the target display is updated.
	m_idfeDst = idfe;
	int cdfe = m_vdfe.Size();

	// If we are part of a collection, we don't want to allow a drop.
	// In reality, we might want to drop an item into a collection if it came from
	// another owner/flid, but that may never become a reality, so for now we'll
	// take the simpler approach.
	if (idfe < cdfe)
	{
		AfDeFieldEditor * pdfe = FieldAt(idfe);
		if (pdfe->GetFldSpec()->m_ft == kftObjOwnCol)
			return false; // We're directly on a collection property
		if (pdfe->GetIndent())
		{
			// Find the next higher node.
			int nInd = pdfe->GetIndent();
			for (int idfeT = idfe; --idfeT >= 0; )
			{
				if (m_vdfe[idfeT]->GetIndent() < nInd)
				{
					// This is the next higher node. If it is a collection, don't
					// allow a drop.
					if (m_vdfe[idfeT]->GetFldSpec()->m_ft == kftObjOwnCol)
						return false;
					else
						break;
				}
			}
		}
	}

	IFwMetaDataCachePtr qmdc;
	m_qlpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
	AssertPtr(qmdc);
	ComBool fValid = false;
	CustViewDaPtr qcvd;
	m_qlpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	HVO hvo;

	// If we are below the last field, or if we are on the last field and it is not a valid
	// target, fail if there are valid drop targets above.
	if (idfe >= cdfe - 1)
	{
		if (idfe < cdfe)
		{	// If we are on the last field and it is a valid target, we need to use
			// normal processing below instead of this special process.
			AfDeFeNode * pden = dynamic_cast<AfDeFeNode *>(m_vdfe[idfe]);
			if (pden)
				qmdc->get_IsValidClass(pden->GetFlid(), m_clidDrag, &fValid);
		}
		if (!fValid)
		{
			int idfeT;
			for (idfeT = 0; idfeT < cdfe; ++idfeT)
			{
				AfDeFeNode * pden = dynamic_cast<AfDeFeNode *>(m_vdfe[idfeT]);
				if (!pden)
					continue;
				qmdc->get_IsValidClass(pden->GetFlid(), m_clidDrag, &fValid);
				if (fValid)
					break;
			}
			if (idfeT != cdfe)
				return false; // We have other drop targets that weren't selected.

			if (cdfe == 0)
				return false; // Not sure what to do here, but we don't want to crash below.

			hvo = m_vdfe[0]->GetOwner();
			while (hvo)
			{
				if (hvo == m_hvoDrag)
					return false; // The drop item owns the target.
				HVO hvoT = hvo;
				CheckHr(qcvd->get_ObjOwner(hvoT, &hvo));
			}

			// We need to provide a drop target for a top-level entry at the bottom of the fields.
			// We indicate this by putting the last field in m_pdfeDst (so we can find the
			// destination AfDeSplitChild), and set m_drp to kdrpAtEnd to indicate this condition.
			// Adding a subentry to an existing subentry can be done by dropping on the original
			// label.
			m_idfeDst = m_vdfe.Size() - 1;
			m_pdfeDst = m_vdfe[m_idfeDst];
			m_ypTopDst = dypFieldTop - m_pdfeDst->GetHeight();
			if (idfe < cdfe)
				m_ypTopDst += m_vdfe[m_idfeDst]->GetHeight();

			if (m_pdfeDst && m_pdfeDst->GetFontHeight() > m_dypTreeFont)
				m_ypTopDst += ((m_pdfeDst->GetFontHeight() - m_dypTreeFont) / 2);

			m_drp = kdrpAtEnd;
			return true;
		}
	}

	AfDeFeNodePtr qdenDst = dynamic_cast<AfDeFeNode *>(m_vdfe[idfe]);

	// Save the results, even if not valid, so that the target display is updated.
	m_pdfeDst = qdenDst;
	m_ypTopDst = dypFieldTop;

	if (m_pdfeDst && m_pdfeDst->GetFontHeight() > m_dypTreeFont)
		m_ypTopDst += ((m_pdfeDst->GetFontHeight() - m_dypTreeFont) / 2);

	// If the target field is not a tree node, we can't make a drop.
	if (!qdenDst)
		return false;
	// If the target field won't accept the incoming class, we can't make a drop.
	qmdc->get_IsValidClass(qdenDst->GetFlid(), m_clidDrag, &fValid);
	if (!fValid)
		return false;

	// We have a valid drop. Determine whether we want to drop above, below, or on the target.
	// The top and bottom 4 pixels cause a drop above or below.
	if (dypExtra <= 4)
		m_drp = kdrpAbove;
	else if (m_pdfeDst->GetHeight() - dypExtra <= 4)
		m_drp = kdrpBelow;
	else
		m_drp = kdrpOn;

	// If we are on the same field, we can't make a drop.
	if (qdenDst->GetTreeObj() == m_hvoDrag)
		return false;

	// If the target owner is owned by the dragged item, we can't make a drop.
	HVO hvoTargOwner = m_drp == kdrpOn ? qdenDst->GetTreeObj() : qdenDst->GetOwner();
	hvo = hvoTargOwner;
	while (hvo)
	{
		if (hvo == m_hvoDrag)
			return false; // The drop item owns the target.
		HVO hvoT = hvo;
		CheckHr(qcvd->get_ObjOwner(hvoT, &hvo));
	}

	// We have a good drop point if we are in the middle of this field.
	if (m_drp == kdrpOn)
		return true;

	// We are dropping above the destination field. If the field above this (at the same
	// indent) is the source field, the drop would be meaningless.
	int chvo;
	CheckHr(qcvd->get_VecSize(hvoTargOwner, qdenDst->GetOwnerFlid(), &chvo));
	int ihvo;
	if (m_drp == kdrpAbove)
	{
		for (ihvo = chvo; --ihvo; )
		{
			CheckHr(qcvd->get_VecItem(hvoTargOwner, qdenDst->GetOwnerFlid(), ihvo, &hvo));
			if (hvo == qdenDst->GetTreeObj())
				break;
		}
		if (--ihvo < 0)
		{
			AfDeFeVectorNode * pdevn = dynamic_cast<AfDeFeVectorNode *>(FieldAt(idfe));
			// If we are immediately above a vector node, we don't want to allow a drop.
			// Otherwise, there isn't another field, so it's Ok.
			return (!pdevn);
		}
		CheckHr(qcvd->get_VecItem(hvoTargOwner, qdenDst->GetOwnerFlid(), ihvo, &hvo));
		return (m_hvoDrag != hvo);
	}

	// We are dropping below the destination field. If the field below that (at the same
	// indent) is the source field, the drop would be meaningless.
	for (ihvo = 0; ihvo < chvo; ++ihvo)
	{
		CheckHr(qcvd->get_VecItem(hvoTargOwner, qdenDst->GetOwnerFlid(), ihvo, &hvo));
		if (hvo == qdenDst->GetTreeObj())
			break;
	}
	if (++ihvo >= chvo)
	{
		AfDeFeVectorNode * pdevn = dynamic_cast<AfDeFeVectorNode *>(FieldAt(idfe));
		// If we are immediately below a vector node, we don't want to allow a drop.
		// Otherwise, there isn't another field, so it's Ok.
		return (!pdevn);
	}
	CheckHr(qcvd->get_VecItem(hvoTargOwner, qdenDst->GetOwnerFlid(), ihvo, &hvo));
	return (m_hvoDrag != hvo);
}


/*----------------------------------------------------------------------------------------------
	If a field drop target is highlighted on the screen, this clears it out.
	@param xp The x-coord of the mouse relative to the upper-left corner of the screen.
	@param yp The y-coord of the mouse relative to the upper-left corner of the screen.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::ClearTarget(int xp, int yp)
{
	if (!m_xpMrkLeft)
		return;

	LOGBRUSH lbr = {BS_SOLID, kclrWhite, 0};
	HDC hdc = ::GetDC(m_hwnd);
	// Set up the pen and brush.
	// Note at least one machine I tested (FwTester) failed to use XOR on pens, but used
	// it properly on brushes. Thus, for consistent operation across all machines I've
	// made the pen invisible and only use the brush for this rectangle.
	HBRUSH hbr = AfGdi::CreateBrushIndirect(&lbr);
	HBRUSH hbrOld = AfGdi::SelectObjectBrush(hdc, hbr);
	HPEN hpen = ::CreatePen(PS_NULL, 0, kclrBlack);
	HPEN hpenOld = (HPEN)::SelectObject(hdc, hpen);
	int nrop = ::SetROP2(hdc, R2_XORPEN); // Use XOR to unmark the target.

	// Erase the old marker
	::ImageList_DragLeave(NULL);
	::Rectangle(hdc, m_xpMrkLeft, m_ypMrkTop, m_xpMrkRight, m_ypMrkBottom);
	::ImageList_DragEnter(NULL, 0, 0);
	::ImageList_DragMove(xp, yp);

	// Clean up resources.
	::SelectObject(hdc, hpenOld);
	AfGdi::SelectObjectBrush(hdc, hbrOld, AfGdi::OLD);
	::DeleteObject(hpen);
	AfGdi::DeleteObjectBrush(hbr);
	::SetROP2(hdc, nrop);
	int iSuccess;
	iSuccess = ::ReleaseDC(m_hwnd, hdc);
	Assert(iSuccess);
	m_xpMrkLeft = 0; // Used as a flag indicating we no longer have a mark.
}


/*----------------------------------------------------------------------------------------------
	Process a vertical scroll message (WM_VSCROLL).
	@param nId Identifies the scroll request.
	@param yp The current position of the scroll box for SB_THUMBPOSITION or SB_THUMBTRACK.
	@param hwndSbar Handle to the scroll bar if the message came from the scroll bar.
	@return True if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnVScroll(int nId, int yp, HWND hwndSbar)
{
	SCROLLINFO si = {isizeof(si), SIF_POS | SIF_TRACKPOS};
	GetScrollInfo(SB_VERT, &si);
	Rect rc;
	GetClientRect(rc);
	bool fNeedChange = true;

	switch (nId)
	{
	case SB_LINEDOWN:
		si.nPos += m_dypDefFieldHeight;
		break;

	case SB_LINEUP:
		si.nPos -= m_dypDefFieldHeight;
		break;

	case SB_PAGEDOWN:
		si.nPos += rc.Height();
		break;

	case SB_PAGEUP:
		si.nPos -= rc.Height();
		break;

	case SB_THUMBTRACK:
		si.nPos = yp;
		break;

	default:
		fNeedChange = false;
	}

	if (fNeedChange)
	{
		SetScrollInfo(SB_VERT, &si, true);
		::InvalidateRect(m_hwnd, NULL, true);
	}

	return true; // Don't pass message on.
}


/*----------------------------------------------------------------------------------------------
	Handle window painting (WM_PAINT). To save time, we only paint the portion of the screen
	that is dirty. To avoid flicker, we write to a memory DC, then BitBlt to the screen.
	@param hdcDef NULL with WM_PAINT, but otherwise it could be a DC to use for painting.
	@return True if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnPaint(HDC hdcDef)
{
	AssertObj(this);

	Rect rcpClient;
	GetClientRect(rcpClient);

	// If we are given a DC, draw directly on that, assuming it has already been opened.
	if (hdcDef)
	{
		Draw(hdcDef, rcpClient, rcpClient);
		return true; // Don't pass message on.
	}

	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_hwnd, &ps);

	// Set up a rect for the area that actually needs to be updated.
	Rect rcpClip = rcpClient;
	rcpClip.Intersect(ps.rcPaint);
	// Use a memory DC for drawing to avoid flicker. For debugging paint problems it may
	// be helpful to comment out these next two lines, plus the cleanup at the end.
	// Otherwise you don't see individual paint results until everything is done.
	HDC hdcMem = AfGdi::CreateCompatibleDC(hdc);
	HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, rcpClip.Width(), rcpClip.Height());
	HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
	// Offset the origin so that drawing methods work normally regardless of clipping.
	::SetViewportOrgEx(hdcMem, -rcpClip.left, -rcpClip.top, NULL);
	AfGfx::FillSolidRect(hdcMem, rcpClip, ::GetSysColor(COLOR_WINDOW)); // Paint background
	Draw(hdcMem, rcpClient, rcpClip); // Do the main drawing.

	// Splat the memory DC onto the screen.
	::BitBlt(hdc, rcpClip.left, rcpClip.top, rcpClip.Width(), rcpClip.Height(),
		hdcMem, rcpClip.left, rcpClip.top, SRCCOPY);

	// Clean up memory DC.
	HBITMAP hbmpDebug;
	hbmpDebug = AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);
	Assert(hbmpDebug && hbmpDebug != HGDI_ERROR);
	Assert(hbmpDebug == hbmp);

	BOOL fSuccess;
	fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
	Assert(fSuccess);

	fSuccess = AfGdi::DeleteDC(hdcMem);
	Assert(fSuccess);

	::EndPaint(m_hwnd, &ps);

	return true; // Don't pass message on.
}

/*----------------------------------------------------------------------------------------------
	Find the top of the specified field (in pixels from top of whole window contents).
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::TopOfField(int idfe)
{
	return TopOfField(FieldAt(idfe));
}

/*----------------------------------------------------------------------------------------------
	Find the top of the specified field (in pixels from top of whole window contents).
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::TopOfField(AfDeFieldEditor * pdfeTarget)
{
	int dypFieldTop = 0; // Pixels from the top of the virtual screen to the top of the editor.
	int dypMin = GetMinFieldHeight(); // Min pixel height of field.
	// Calculate dypField and dypFieldTop for the current editor.
	for (int idfe = 0; idfe < m_vdfe.Size(); ++idfe)
	{
		AfDeFieldEditor * pdfe = m_vdfe[idfe];
		if (pdfeTarget == pdfe)
			return dypFieldTop;
		int dypField = Max(dypMin, (pdfe ? pdfe->GetHeight() : DefaultFieldHeight()));
		dypFieldTop += dypField;
	}
	Assert(false); // We should find it.
	return dypFieldTop; // arbitrary.
}

/*----------------------------------------------------------------------------------------------
	The active editor window needs more vertical space. Adjust fields and scroll if needed.
	This is called by a field editor when it needs an additional line. Before calling
	this the editor should have set its height to the new value so that it will
	respond properly when this class calls GetHeight() during the redraw process.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::EditorResizing()
{
	AssertPtr(m_pdfe); // We should only call this with an active editor.
	Rect rc;
	GetClientRect(rc);
	SetHeight(); // Calculate the height of all fields.

	int dypField; // Pixel height of field editor.
	int idfe = 0;
	int dypFieldTop = 0; // Pixels from the top of the virtual screen to the top of the editor.
	int dypMin = GetMinFieldHeight(); // Min pixel height of field.
	// Calculate dypField and dypFieldTop for the current editor.
	for (; idfe < m_vdfe.Size(); ++idfe)
	{
		AfDeFieldEditor * pdfe = m_vdfe[idfe];
		dypField = Max(dypMin, (pdfe ? pdfe->GetHeight() : DefaultFieldHeight()));
		if (m_pdfe == pdfe)
			break;
		else
			dypFieldTop += dypField;
	}
	Assert(idfe < m_vdfe.Size()); // We should have found an editor before the end.

	// Account for the current scroll position.
	SCROLLINFO si = {isizeof(si), SIF_PAGE | SIF_POS | SIF_RANGE};
	GetScrollInfo(SB_VERT, &si);

	// If there isn't room at the bottom of the screen, scroll up.
	if (dypFieldTop + dypField - si.nPos > rc.bottom)
	{
		si.nPos = dypFieldTop + dypField - rc.bottom;
		SetScrollInfo(SB_VERT, &si, true);
		::InvalidateRect(m_hwnd, NULL, true); // Repaint entire window.
	}
	else
	{
		rc.top = dypFieldTop - si.nPos;
		::InvalidateRect(m_hwnd, &rc, true); // Repaint lower part of screen
	}
}


/*----------------------------------------------------------------------------------------------
	Draw the data entry window, including the tree on the left, the vertical line, and the
	field contents on the right. When rcpClip is being used, the origin of the hdc has
	been shifted (logical units) so that draw operations can assume normal coordinates from
	rcpClient. Although Windows will ignore anything we write outside the clip area, we should
	save time by limiting our work, within reason, to only write within the clip area.
	@param hdc The DC we are writing to.
	@param rcpClient The window rectangle.
	@param rcpClip The rectangle in the window that is dirty and needs updating.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::Draw(HDC hdc, const Rect & rcpClient, const Rect & rcpClip)
{
	Assert(hdc);
	AfDeFieldEditor * pdfe;
	const COLORREF kclrLine = GetLineColor();
	const COLORREF kclrBkgrnd = GetTreeBackgroundColor();

	// Account for scrolling.
	SCROLLINFO si = {isizeof(si), SIF_POS | SIF_RANGE};
	GetScrollInfo(SB_VERT, &si);
	// Vertical pixels from the top of the screen. (negative if scrolled off the top.)
	int dyp = -si.nPos;

	// If tree width is not uniform, we don't want to shade the tree part or draw the separator.
	if (!m_fAlignFieldsToTree)
	{
		// Shade the tree if any part is in the clipping rect.
		if (rcpClip.left < m_dxpTreeWidth)
		{
			Rect rcTree(0, rcpClip.top, m_dxpTreeWidth, rcpClip.bottom);
			AfGfx::FillSolidRect(hdc, rcTree, kclrBkgrnd);
		}

		// Draw the right edge (vertical line) of the tree "pane" if it is in the clipping rect..
		if (m_dxpTreeWidth > rcpClip.left && m_dxpTreeWidth <= rcpClip.right)
		{
			::MoveToEx(hdc, m_dxpTreeWidth - 1, rcpClip.top, NULL);
			::LineTo(hdc, m_dxpTreeWidth - 1, rcpClip.bottom);
		}
	}

	// Draw the data
	// Note: SelectClipRgn below uses device units instead of logical, so we need to store
	// device units here.
	HRGN hrgn = ::CreateRectRgn(0, 0, rcpClip.right - rcpClip.left,
		rcpClip.bottom - rcpClip.top);
	Assert(hrgn);
	ClipRgnWrap rwrClip(hrgn, hdc);

	int dypMin = GetMinFieldHeight();

	// On initial draw may have no active one, but still want to be lazy.
	// If there is an active editor, we need to set this false to make sure the active
	// editor window doesn't overlap another field.
	bool fPastActiveEd = (m_pdfe == 0);

	// Process from the first field through the last field needed for the clip rectangle.
	for (int idfe = 0; idfe < m_vdfe.Size(); ++idfe)
	{
		pdfe = m_vdfe[idfe];
		if (!fPastActiveEd)
			fPastActiveEd = pdfe == m_pdfe;
		int dypField = dypMin;
		if (pdfe)
			dypField = Max(dypMin, pdfe->GetHeight()); // The pixel height of a field.
		int ypTopField = dyp; // Pixels from the top of the data to the top of the field.
		dyp += dypField; // dyp is the top of the next field.

		// If the field is above the clip rect, we don't need to do anything else
		// for this field.
		if (dyp < rcpClip.top)
		{
			// Except, an active field editor that is now above the clip region
			// may still have its window in the visible area, so we need to check
			// for this and move it, if it is.
			if (pdfe && pdfe == m_pdfe)
			{
				Rect rcEdit;
				::GetClientRect(pdfe->Hwnd(), &rcEdit);
				Point pt(rcEdit.left, rcEdit.bottom); // Get a point
				::ClientToScreen(pdfe->Hwnd(), &pt); // Translate point to screen points
				::ScreenToClient(m_hwnd, &pt); // Translate point to new window
				if (pt.y >= rcpClip.top)
				{
					Rect rcField(GetBranchWidth(pdfe), ypTopField, rcpClient.right, dyp - 1);
					pdfe->MoveWnd(rcField); // Move the active editor window.
				}
			}
			continue;
		}

		// If we are below the clip rect, we can skip any remaining fields.
		if (ypTopField >= rcpClip.bottom)
		{
			// Except, an active field editor that is now below the clip region
			// may still have its window in the visible area, so we need to check
			// for this and move it, if it is.
			if (pdfe && pdfe == m_pdfe)
			{
				Rect rcEdit;
				::GetClientRect(pdfe->Hwnd(), &rcEdit);
				Point pt(rcEdit.left, rcEdit.top); // Get a point
				::ClientToScreen(pdfe->Hwnd(), &pt); // Translate point to screen points
				::ScreenToClient(m_hwnd, &pt); // Translate point to new window
				if (pt.y <= rcpClip.bottom)
				{
					Rect rcField(GetBranchWidth(pdfe), ypTopField, rcpClient.right, dyp - 1);
					pdfe->MoveWnd(rcField); // Move the active editor window.
				}
				break;
			}
			// Stop when we are past the active editor. Otherwise, keep going.
			if (fPastActiveEd)
				break;
			continue;
		}

		pdfe = FieldAt(idfe); // Now we really need the field.

		DeTreeState dts = pdfe->GetExpansion();
		::SetBkColor(hdc, kclrBkgrnd);

		// Draw a portion of the tree, as long as the tree is in the clip rect.
		if (rcpClip.left < GetBranchWidth(pdfe) - kdxpLeftMargin)
		{
			// Restrict drawing to tree boundary.
			::IntersectClipRect(hdc, 0, rcpClip.top, GetBranchWidth(pdfe) - kdxpRtTreeGap,
				rcpClip.bottom);

			int nIndent = pdfe->GetIndent();

			// Go through the indents, drawing the correct tree structure at each level.
			// We write out actual dots since there isn't a reliable way on Win9x to
			// produce a dotted line with one pixel on and one off.
			for (int nInd = 0; nInd <= nIndent; ++nInd)
			{
				// Round up to even value to maintain proper spacing of vertical dots.
				int ypTreeTop = (ypTopField + 1) & 0xFFFFFFFE;
				int xpBoxLeft = kdxpLeftMargin + nInd * kdxpIndDist;
				int xpBoxCtr = xpBoxLeft + kdxpBoxCtr;
				int dypLeftOver = Max((int)kdypBoxHeight, pdfe->GetFontHeight()) - kdypBoxHeight;
				int ypBoxTop = ypTopField + dypLeftOver / 2;
				int ypBoxCtr = ypBoxTop + kdypBoxCtr;
				int xpRtLineEnd = xpBoxCtr + kdxpLongLineLen;

				// There are two possible locations for the start and stop points for the
				// vertical line. That will produce three different results which I have
				// attempted to illustrate below. In case that's unclear they are:
				// an L - shaped right angle, a T - shape rotated counter-clockwise by
				// 90 degrees and an inverted L shape (i.e. flipped vertically).
				//
				// |_  > ypStart = top of field, ypStop = center point of +/- box.
				// |-  > ypStart = top of field, ypStop = bottom of field.
				// |¯  > ypStart = center point of +/- box, ypStop = bottom of field.
				//
				// Draw the vertical line.
				if (nInd == nIndent || NextFieldAtIndent(nInd, idfe))
				{
					int ypStop = (NextFieldAtIndent(nInd, idfe) ? dyp : ypBoxCtr + 1);
					int ypStart = (!idfe && NextFieldAtIndent(nInd, idfe) ? ypBoxCtr : ypTreeTop);
					// NOTE:  SetPixel is returning 4294967295 (at times) below; this seems to
					//        be because we are outside the clipping area; ignore error for now.
					for (int yp = ypStart; yp < ypStop; yp += 2)
						::SetPixel(hdc, xpBoxCtr, yp, kclrLine);
				}

				// Draw the line to the right of the box.
				if (nInd == nIndent)
					for (int xp = xpBoxCtr + 2; xp < xpRtLineEnd; xp += 2)
						::SetPixel(hdc, xp, ypBoxCtr, kclrLine);

				// Process a terminal level with a box.
				if (nInd == nIndent && dts != kdtsFixed)
				{
					// Draw the box.
					Rect rcBox(xpBoxLeft, ypBoxTop, xpBoxLeft + kdxpBoxWid, ypBoxTop + kdypBoxHeight);
					AfGfx::FillSolidRect(hdc, rcBox, kclrLine);
					rcBox.Inflate(-1, -1);
					AfGfx::FillSolidRect(hdc, rcBox, kclrBkgrnd);

					// Draw the minus sign.
					int xpLeftMinus = xpBoxLeft + 1 + kdzpIconGap;
					::MoveToEx(hdc, xpLeftMinus, ypBoxCtr, NULL);
					::LineTo(hdc, xpLeftMinus + kdxpIconWid, ypBoxCtr);

					if (dts == kdtsCollapsed)
					{
						// Draw the vertical part of the plus, if we are collapsed.
						int ypTopPlus = ypBoxTop + 1 + kdzpIconGap;
						::MoveToEx(hdc, xpBoxCtr, ypTopPlus, NULL);
						::LineTo(hdc, xpBoxCtr, ypTopPlus + kdypIconHeight);
					}
				}
			}

			// Write out the label text.
			ITsStringPtr qtss;
			pdfe->GetLabel(&qtss);
			SmartBstr sbstr;
			qtss->get_Text(&sbstr);
			HFONT hfontOld = AfGdi::SelectObjectFont(hdc, ::GetStockObject(DEFAULT_GUI_FONT));

			int ypLabelTop = ypTopField;
			if (pdfe->GetFontHeight() > m_dypTreeFont)
				ypLabelTop = ypTopField + ((pdfe->GetFontHeight() - m_dypTreeFont) / 2);

			::TextOutW(hdc, kdxpLeftMargin + (pdfe->GetIndent() + 1) * kdxpIndDist, ypLabelTop,
				sbstr.Chars(), sbstr.Length());

			AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);

			// Restore normal clipping region.
			int iSuccess = ::SelectClipRgn(hdc, rwrClip);
			if (!iSuccess || ERROR == iSuccess)
				ThrowHr(WarnHr(E_FAIL));
		}

		// Draw the field contents, as long as the clip rect includes data.
		if (rcpClip.right >= GetBranchWidth(pdfe))
		{
			// Draw the field contents.
			Rect rcField(GetBranchWidth(pdfe), ypTopField, rcpClient.right, dyp - 1);
			// Draw the text if an editor isn't open; otherwise move the edit window.
			if (pdfe == m_pdfe)
			{
				pdfe->MoveWnd(rcField);
			}
			else
				pdfe->Draw(hdc, rcField);
		}
	}
	// Shade any area below fields.
	int dxpWidthLeftOfSeparator = m_fAlignFieldsToTree ? 0 : m_dxpTreeWidth;
	if (rcpClip.right >= dxpWidthLeftOfSeparator)
	{
		Rect rcTree(dxpWidthLeftOfSeparator, dyp, rcpClip.right, rcpClip.bottom);
		AfGfx::FillSolidRect(hdc, rcTree, kclrBkgrnd);
	}

	dyp = -si.nPos;
	for (int idfe = 0; idfe < m_vdfe.Size(); ++idfe)
	{
		pdfe = m_vdfe[idfe];
		if (!fPastActiveEd)
			fPastActiveEd = pdfe == m_pdfe;
		int dypField = dypMin;
		if (pdfe)
			dypField = Max(dypMin, pdfe->GetHeight()); // The pixel height of a field.
		dyp += dypField; // dyp is the top of the next field.

		// Draw the field contents, as long as the clip rect includes data.
		if (rcpClip.right >= GetBranchWidth(pdfe))
		{
			// Draw a horizontal line for the bottom of the field.
			int dxpDotSpacing = GetSeparatorLineDotSpacing() + 1;
			COLORREF clr = GetSeparatorLineColor();

			// Doing it this way allows for dotted lines of different sparseness,
			// Including solid. If a derived class doesn't override the function
			// GetSeparatorLineDotSpacing, the line will be solid.
			for (int xp = GetBranchWidth(pdfe); xp < rcpClient.right; xp += dxpDotSpacing)
				::SetPixel(hdc, xp, dyp - 1, clr);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Return the next field index that is at the specified indent level, or zero if there are no
	fields following this one that are at the specified level in the tree. This is normally
	used to find the beginning of the next subrecord when we have a sequence of subrecords,
	and possibly sub-subrecords, with some being expanded and others not.
	@param nInd The indent level we want.
	@param idfe An index to the current field. We start looking at the next field.
	@return The index of the next field or 0 if none.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::NextFieldAtIndent(int nInd, int idfe)
{
	Assert((uint)idfe < (uint)m_vdfe.Size());
	int cdfe = m_vdfe.Size();

	// Start at the next editor and work down, skipping more nested editors.
	for (++idfe; idfe < cdfe; ++idfe)
	{
		int nIndCur = FieldAt(idfe)->GetIndent();
		if (nIndCur == nInd) // We found another item at this level, so return it.
			return idfe;
		if (nIndCur < nInd) // We came out to a higher level, so return zero.
			return 0;
	}
	return 0; // Reached the end without finding one at the specified level.
}


/*----------------------------------------------------------------------------------------------
	Return the last field index that is at the same indent level as the current field.
	This is normally used to find the last field for an expanded subrecord. Note the value
	returned may be a nested field at a higher indent.
	@param idfe An index to the current field.
	@return The index of the last field that is at the same indent level, or the last field.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::LastFieldAtSameIndent(int idfe)
{
	Assert((uint)idfe < (uint)m_vdfe.Size());
	int cdfe = m_vdfe.Size();
	int nInd = FieldAt(idfe)->GetIndent(); // The indent level of the current field.

	// Start at the next editor and work down, skipping more nested editors.
	for (++idfe; idfe < cdfe; ++idfe)
		// When we get to a higher indent level, return the previous editor index.
		if (m_vdfe[idfe] && m_vdfe[idfe]->GetIndent() < nInd)
			return --idfe;
	return --idfe; // Didn't find a higher level, so return the last field.
}


/*----------------------------------------------------------------------------------------------
	Return the index of the first field being displayed in the closest tree node of which idfe
	is a part. It does not return the tree node itself, but the first indented field after the
	node. If it is a main record, it returns the first field of the record.
	@param idfe An index to the current field.
	@return The index of the first field of the tree node.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::FirstFieldOfTreeNode(int idfe)
{
	Assert((uint)idfe < (uint)m_vdfe.Size());
	int nInd = FieldAt(idfe)->GetIndent(); // The indent level of the current field.

	// Start at the previous editor and work up, skipping more nested editors.
	while (--idfe >= 0)
	{
		AfDeFeNode * pden = dynamic_cast<AfDeFeNode *>(FieldAt(idfe));
		if (pden && pden->GetIndent() < nInd)
			return ++idfe; // We are on the target node, so go back up to first field.
	}
	return 0; // The first field is the one we want for main records.
}


/*----------------------------------------------------------------------------------------------
	Resets the height of all editors, sets m_dypEditors and resets scroll info.
	This tries to keep the the same position in the same field at the top of the window.
	@param fNoScroll If true, don't scroll to make the bottom of a long record fit in the
		window. (E.g., you might want to keep the +/- outline buttons on the same line as
		text is expanded and contracted.) If false, scroll so that the bottom field just
		fits at the bottom of the window.
	@param fNoPaint If true, don't do any painting (useful when expanding a lazy box above
		or below what is visible)
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::SetHeight(bool fNoScroll, bool fNoPaint)
{
	Rect rc;
	::GetClientRect(m_hwnd, &rc);

	// AfWnd::FWndProcPre initially calls this with all zeros. Don't do anything in this case.
	if (rc.right == 0)
		return;

	// Get info on the field currently displayed at the top of the screen.
	int dypStart; // The pixel offset from the top of the display to the top of the field.
	int dypExtra; // The pixel offset from the top of the field from the top of the screen.
	int idfe = GetField(0, &dypStart, &dypExtra);

	// Now reset all field heights, and find the new info for the top field.
	int cdfe = m_vdfe.Size();
	int dypMin = DefaultFieldHeight();
	int dyp = 0;
	// If any sizes change we need to invalidate so changes actually appear.
	bool fInvalidate = false;
	for (int i = 0; i < cdfe; ++i)
	{
		if (i == idfe)
			dypStart = dyp; // Save the new starting point for the top field.
		if (m_vdfe[i])
		{
			int dxpField = rc.right - GetBranchWidth(m_vdfe[i]); // Width of field data.
			int dypHeightOld = m_vdfe[i]->GetHeight();
			int dypHeightNew = Max(dypMin, m_vdfe[i]->SetHeightAt(dxpField));
			dyp += dypHeightNew;
			if (dypHeightNew != dypHeightOld)
				fInvalidate = true;
		}
		else
			dyp += dypMin;
	}

	m_dypEditors = dyp;

	SCROLLINFO si = {isizeof(si), SIF_PAGE | SIF_POS | SIF_RANGE};
	GetScrollInfo(SB_VERT, &si);
	// If we are not at the top, try to maintain the same field position at the top of the
	// window.
	si.nMin = 0;
	// The extra 1 triggers special action in AfSplitChild::SetScrollInfo.
	si.nPage = min(rc.bottom, dyp) + 1;
	si.nPos = dypStart + dypExtra;
	// If nMax ends up less than the bottom of the screen, it will cause a scroll.
	if (fNoScroll)
		// We don't want automatic scrolls here, so nMax can't be less than the bottom
		// of the screen.
		si.nMax = max(dyp, si.nPos + rc.bottom);
	else
		si.nMax = dyp;
	SetScrollInfo(SB_VERT, &si, true);

	if (fInvalidate && !fNoPaint)
		::InvalidateRect(m_hwnd, NULL, false);

	/* Useful for debugging.
	StrAppBuf strb;
	strb.Format("dypStart=%d dypExtra=%d rc.bottom=%d si.nMin=%d si.nPos=%d si.nPage=%d si.nMax=%d dyp=%d\n",
		dypStart, dypExtra, rc.bottom, si.nMin, si.nPos, si.nPage, si.nMax, dyp);
	OutputDebugString(strb.Chars());
	*/
}


/*----------------------------------------------------------------------------------------------
	Given a pixel offset from the top of the client window, return information on the field that
	corresponds to this position.
	@param ypIn Pixel offset from the top of the client window to a point of interest.
	@param dypStart Pointer to receive the pixel offset from the top of a record to the top
		of the target field. If NULL, it is ignored.
	@param dypExtra Pointer to receive the pixel offset from the top of the target field
		to the point of interest. If NULL, it is ignored.
	@return The index of the target field. NOTE: If the point of interest is beyond the last
		field, return m_vdfe.Size(), so the caller must check for this to ensure a valid field.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::GetField(int ypIn, int * dypStart, int * dypExtra)
{
	AssertPtrN(dypStart);
	AssertPtrN(dypExtra);

	// Account for scrolling.
	SCROLLINFO si = {isizeof(si), SIF_PAGE | SIF_POS | SIF_RANGE};
	GetScrollInfo(SB_VERT, &si);

	int dypFieldTop = -si.nPos;
	int idfe = 0;
	int dypField;
	int cdfe = m_vdfe.Size();
	int dypMin = GetMinFieldHeight();

	// Loop through fields until the right one is found.
	for (; idfe < cdfe; ++idfe)
	{
		dypField = Max(dypMin, FieldAt(idfe)->GetHeight());
		if (dypFieldTop + dypField >= ypIn)
			break;
		dypFieldTop += dypField;
	}

	// Return the desired information.
	if (dypStart)
		*dypStart = dypFieldTop;
	if (dypExtra)
		*dypExtra = ypIn - dypFieldTop;
	return idfe;
}


/*----------------------------------------------------------------------------------------------
	Force the active editor to close, then delete all field editors.
	@param fForce True if we want to force the editors to close without making any
		validity checks or saving any changes.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::CloseAllEditors(bool fForce)
{
	// Close the active field editor.
	// Note: you should normally test that it is Ok to close the active editor before calling
	// this. This is done by calling IsOkToChange().
	if (m_pdfe)
	{
		AfDeFieldEditor * pdfeT = m_pdfe;
		m_pdfe = NULL;
		pdfeT->EndEdit(fForce);
	}
	for (int idfe = m_vdfe.Size(); --idfe >= 0; )
	{
		if (m_vdfe[idfe])
		{
			m_vdfe[idfe]->OnReleasePtr();
			m_vdfe[idfe]->Release();
		}
	}
	m_vdfe.Clear();
}


/*----------------------------------------------------------------------------------------------
	Close the active editor. It is assumed at this point that we are in the shutdown process
	and it is too late to abort gracefully if it is illegal to close. Thus, at some point prior
	to this, IsOkToChange() should have	been called and the operation aborted if it wasn't
	valid to close.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::PrepareToHide()
{
	if (m_pdfe)
	{
		Assert(m_pdfe->IsOkToClose());
		AfDeFieldEditor * pdfeT = m_pdfe;
		m_pdfe = NULL;
		pdfeT->EndEdit();
	}
	AfApp::Papp()->RemoveCmdHandler(this, 1);
}


/*----------------------------------------------------------------------------------------------
	This should be called before making any changes to the DE window (splitting, switching
	records, closing, etc.) If the current field editor has illegal contents, or if a required
	field is empty, this notifies the user of the problem and then returns false. If the current
	field editor contents are valid and all required fields have data, then it returns true.
	@fChkReq True if we should also check Required/Encouraged data. False otherwise.
	@return True if it is OK to close the DE window.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::IsOkToChange(bool fChkReq)
{
	// Return false if the current field editor has illegal contents.
	if (m_pdfe && !m_pdfe->IsOkToClose(true)) // Warn the user before returning.
		return false;

	if (fChkReq)
		return HasRequiredData();
	else
		return true;
}


/*----------------------------------------------------------------------------------------------
	This can be called if any field size changes independantly of editing in a field editor.
	It resizes all editors, adjusts scroll bar as needed, and repaints the window (if fVisible
	is true--otherwise, it is assumed the change is invisible, such as expanding a lazy box).
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::FieldSizeChanged(bool fNoPaint)
{
	// If we can't paint, we also can't change the scroll position.
	SetHeight(fNoPaint, fNoPaint);
	Rect rc;
	if (!fNoPaint)
	{
		::GetClientRect(m_hwnd, &rc);
		::InvalidateRect(m_hwnd, &rc, false);
	}
}

/*----------------------------------------------------------------------------------------------
	For a given field, determine the tree node it is a part of, then return the owner and flid
	of that node. For example, if idfe is a field in a level 1 subentry, this will return
	the id for the main entry and the flid holding the subentry. Both values are set to zero
	if the field is not part of a tree node (e.g., it is part of the main object).
	@param idfe The index to the field of interest.
	@param phvoOwner Pointer to receive the owning object id.
	@param pflid Pointer to receive the field id.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::GetObjOwnerAndProp(int idfe, HVO * phvoOwner, int * pflid)
{
	AssertPtr(phvoOwner);
	AssertPtr(pflid);
	Assert((uint)idfe < (uint)m_vdfe.Size());

	HVO hvo = FieldAt(idfe)->GetOwner(); // Get owner of the current field.

	// Go back through the editors until one has a different owner. That
	// should be the object that owns the object in hvo.
	while (--idfe >= 0)
	{
		HVO hvoT = FieldAt(idfe)->GetOwner();
		if (hvo != hvoT)
		{
			*phvoOwner = hvoT;
			*pflid = m_vdfe[idfe]->GetOwnerFlid();
			return;
		}
	}
	*phvoOwner = 0;
	*pflid = 0;
}


/*----------------------------------------------------------------------------------------------
	Close any active editor, find a tree node with object hvo and delete it along with
	any expanded fields. It starts at idfeLim and searches to the end of the editors when
	looking for the node to delete. If pdfe is non-NULL, after the deletion it returns the
	new index for this field. If not found, it will return -1.
	@param hvo The object id represented by a tree node (e.g, a subentry).
	@param idfeLim The editor index where we want to start searching.
	@param pdfe A field editor we want to locate after the deletion. If NULL, it is ignored.
	@return The index to field pdfe after the deletion takes place. If pdfe is NULL, if the
		active editor can't be closed, or if pdfe is not found after the deletion, return -1.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::DeleteTreeNode(HVO hvo, int idfeLim, AfDeFieldEditor * pdfe)
{
	Assert(hvo); // Zero is not a valid hvo.
	Assert((uint)idfeLim < (uint)m_vdfe.Size());
	AssertPtrN(pdfe);
	int idfeLast;

	// If there is an active editor, try to close it.
	if (!CloseEditor())
		return -1; // Can't close editor, so quit.

	AfDeFeTreeNodePtr qdetn;
	int cdfe = m_vdfe.Size();

	// Try to find a tree node starting at idfeLim that represents hvo.
	int idfe;
	for (idfe = idfeLim; idfe < cdfe; ++idfe)
	{
		qdetn = dynamic_cast<AfDeFeTreeNode *>(m_vdfe[idfe]);
		if (!qdetn)
			continue; // Not a tree node.
		if (qdetn->GetTreeObj() == hvo)
			break; // We found it.
	}
	if (idfe == cdfe)
		goto LExit; // Didn't find one.

	// We found the node, now delete it along with any expanded fields.
	if (m_vdfe[idfe]->GetExpansion() == kdtsExpanded &&
		m_vdfe[idfe]->GetIndent() + 1 == m_vdfe[idfe + 1]->GetIndent())
	{
		// We have an expanded node with at least one indented field.
		// Set to the last field of the node.
		idfeLast = LastFieldAtSameIndent(idfe + 1);
	}
	else
		// We have a node without any indented fields.
		idfeLast = idfe;
	// Delete all of the editors for the node.
	for (; idfeLast >= idfe; --idfeLast)
	{
		if (m_vdfe[idfeLast])
		{
			m_vdfe[idfeLast]->OnReleasePtr();
			m_vdfe[idfeLast]->Release();
		}
		m_vdfe.Delete(idfeLast);
	}
	ResetNodeHeaders(); // Adjust all remaining node headers.
	::InvalidateRect(m_hwnd, NULL, false); // Repaint the entire window.

LExit:
	// If pdfe is non-NULL, return its index in the updated vector.
	cdfe = m_vdfe.Size();
	if (!pdfe)
		return -1;
	// Find the specified field editor.
	for (idfe = cdfe; --idfe >= 0; )
	{
		if (m_vdfe[idfe] == pdfe)
			return idfe;
	}
	return -1;
}


/*----------------------------------------------------------------------------------------------
	Check to see if this field is in the tree and is specified with a visibility of kFTVisIfData.
	If so then add it to the tree, and add the current data to that field.

	@param hvoOwn The id of the object to which the field belongs (e.g., the main record).
	@param flid The node field id. (e.g., RnGenericRec_Subrecords).
	@param hvoNode The object id for the node (e.g., subrecord), or 0 if not a node.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::CheckTreeFld(HVO hvoOwn, int flid, HVO hvoNode)
{
	CustViewDaPtr qcvd;
	m_qlpi->GetDataAccess(&qcvd);
	int cdfe = m_vdfe.Size();
	// If we delete the last field, this method can get called with m_vdfe == cdfe, in which
	// case we do nothing. We can't call GetRecordSpec because it assumes a valid index.
	if (m_idfe >= cdfe)
		return;
	RecordSpec * prsp = GetRecordSpec(m_idfe);

	int idfe;
	for (idfe = 0; idfe < cdfe; ++idfe)
	{
		// If the field is already showing then do nothing.
		if (FieldAt(idfe)->GetOwnerFlid() == flid)
			return;
	}

	BlockSpec * pbsp;
	int ibsp;
	int cbsp = prsp->m_vqbsp.Size();
	for (ibsp = 0; ibsp < cbsp; ++ibsp)
	{
		// Find this field in the block spec.
		pbsp = prsp->m_vqbsp[ibsp];
		if (pbsp->m_flid == flid)
			break;
	}
	if (ibsp > cbsp)
		return;
	// If the field type is not kFTVisIfData then do nothing.
	if (pbsp->m_eVisibility != kFTVisIfData)
		return;

	// Find the starting field for this object.
	idfe = FirstFieldOfTreeNode(m_idfe);

	// Now look through this object's fields and find the insertion place.
	int flidDfe;
	int flidBsp;
	// We need to use the indent from the first field, not the current
	// field, since the current field (e.g., RnEvent_Participants) may have a
	// phony indent.
	int nInd = m_vdfe[idfe]->GetIndent();
	// We use this to get the owner of the field in case it has a phony owner
	// (e.g., RnRoledPartic). Drag object should return a reasonable owner.
	HVO hvo = GetDragObject(m_vdfe[idfe]);
	int nIndT;
	AfDeFieldEditor * pdfe;
	BlockSpec * pbspT;
	for (ibsp = 0; ibsp < cbsp; ++ibsp)
	{
		// Skip past any fields that match this block spec.
		pbspT = prsp->m_vqbsp[ibsp];
		flidBsp = pbspT->m_flid;
		bool fFound = false;
		for (; idfe < cdfe; ++idfe)
		{
			pdfe = FieldAt(idfe);
			nIndT = pdfe->GetIndent();
			if (nIndT > nInd)
				continue; // Skip nested fields.
			flidDfe = pdfe->GetOwnerFlid();
			// Exit loop if we've reached the end of this object's fields
			// or we've reached the last field matching flidBsp.
			if (nIndT < nInd || flidDfe != flidBsp)
				break;
			fFound = true;
		}
		if (fFound)
			continue;
		else if (pbsp == pbspT || nIndT < nInd)
			break; // idfe is at the point where we should insert.
	}

	// Add the field.
	int idfeT = idfe;
	AddField(hvo, prsp->m_clsid, prsp->m_nLevel, pbsp, qcvd, idfeT, nInd, true);
	SetHeight(); // Set height on all fields.
}


/*----------------------------------------------------------------------------------------------
	Find a field editor for the given owner and flid (if it exists) and update that field.
	For tree nodes, this just updates the heading. This should not be called until the cache
	has been updated.
	@param hvoOwn The id of the object to which the field belongs (e.g., the main record).
	@param flid The node field id. (e.g., RnGenericRec_Subrecords).
	@param hvoNode The object id for the node (e.g., subrecord), or 0 if not a node.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::UpdateField(HVO hvoOwn, int flid, HVO hvoNode)
{
	Assert(hvoOwn);
	Assert(flid);

#ifdef DEBUG_THIS_FILE
	StrAnsi sta;
	sta.Format("AfDeSplitChild::UpdateField:  hvoOwn=%d; flid=%d; hvoNode=%d; m_vdfe.Size()=%d.\n",
											  hvoOwn,    flid,    hvoNode,    m_vdfe.Size());
	OutputDebugString(sta.Chars());
#endif

	// Go through the fields in reverse order.
	for (int idfe = m_vdfe.Size(); --idfe >= 0; )
	{
		AfDeFieldEditor * pdfe = m_vdfe[idfe];
#ifdef DEBUG_THIS_FILE
		if (pdfe)
			sta.Format("AfDeSplitChild::UpdateField:  idfe=%d; OwnerFlid()=%d; Owner()=%d; flid=%d; Obj=%d.\n",
													  idfe,    pdfe->GetOwnerFlid(),    pdfe->GetOwner(),
															   pdfe->GetFlid(),         pdfe->GetObj());
		else
			sta.Format("AfDeSplitChild::UpdateField:  idfe=%d; pdfe id NULL.\n",
													  idfe);
		OutputDebugString(sta.Chars());
#endif
		// In the immediately following "if" stmt, cannot use GetOwnerFlid(), GetOwner() because this
		// code must be able to match an individual roled participant.

		if (pdfe && pdfe->GetFlid() == flid && pdfe->GetObj() == hvoOwn)
		{
			// There may be multiple tree nodes, so make sure it is the right one.
			if (hvoNode)
			{
				AfDeFeNode * pden = dynamic_cast<AfDeFeNode *>(pdfe);
				if (!pden || pden->GetTreeObj() != hvoNode)
					continue;
			}
			// We've found the specified field, so update it and repaint the entire window.
			pdfe->UpdateField();
			::InvalidateRect(m_hwnd, NULL, false);
			break; // We are done.
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Update the specified field on all open DE windows on this language project.
	For tree nodes, this just updates the heading. This should not be called until the cache
	has been updated.
	@param hvoOwn The id of the object to which the field belongs (e.g., the main record).
	@param flid The node field id. (e.g., RnGenericRec_Subrecords).
	@param hvoNode The object id for the node (e.g., subrecord), or 0 if not a node.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::UpdateAllDEWindows(HVO hvoOwn, int flid, HVO hvoNode)
{
	// ENHANCE JohnT: When we have a COM interface representing the application, use that
	// mechanism to implement this.
	// Also, consider whether there is some way to avoid pulling the AfApp class into
	// components that won't have one.
	// Check other occurrences of AfApp in this file.
	if (!AfApp::Papp())
		return;

	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	int cafw = vqafw.Size();
	// Go through all open windows for the application.
	for (int iafw = 0; iafw < cafw; iafw++)
	{
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(vqafw[iafw].Ptr());
		AssertPtr(prmw);
		if (prmw->GetLpInfo() == m_qlpi)
		{
			// It's the same language project.
			AfMdiClientWndPtr qmdic = prmw->GetMdiClientWnd();
			AssertPtr(qmdic);
			int ccwnd = qmdic->GetChildCount();
			// Go through all DE windows for the main window.
			for (int icwnd = 0; icwnd < ccwnd; ++icwnd)
			{
				AfSplitterClientWndPtr qacw =
					dynamic_cast<AfSplitterClientWnd *>(qmdic->GetChildFromIndex(icwnd));
				if (!qacw || !qacw->Hwnd())
					continue; // Certainly not an active data entry window.
				// Update both panes for this view.
				AfDeSplitChildPtr qadsc;
				qadsc = dynamic_cast<AfDeSplitChild *>(qacw->GetPane(0));
				if (qadsc)
				{
					qadsc->CheckTreeFld(hvoOwn, flid, hvoNode);
					qadsc->UpdateField(hvoOwn, flid, hvoNode);
				}
				qadsc = dynamic_cast<AfDeSplitChild *>(qacw->GetPane(1));
				if (qadsc)
				{
					qadsc->CheckTreeFld(hvoOwn, flid, hvoNode);
					qadsc->UpdateField(hvoOwn, flid, hvoNode);
				}
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Add a copy of a collapsed tree node at the appropriate place in the field editor hierarchy.
	This should not be called until the cache has been updated.
	@param pdetnSrc A pointer to the tree node to copy.
	@param iOrd The position within the flid vector for the new node.
	@param prsp Pointer to a RecordSpec that tells us where the node should be inserted.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::AddTreeNode(AfDeFeTreeNode * pdetnSrc, int iOrd, RecordSpec * prsp)
{
	AssertPtr(pdetnSrc);
	HVO hvoOwn = pdetnSrc->GetOwner(); // The id of the object owning the node.
	int flid = pdetnSrc->GetOwnerFlid(); // The flid in which the node is owned.
	HVO hvoNode = pdetnSrc->GetTreeObj(); // The id of the object represented by the node.
	Assert(hvoOwn);
	Assert(flid);
	Assert(hvoNode);
	Assert(iOrd >= 0);

	int nInd;
	// Find the point to insert the node.
	int idfe = FindInsertionIndex(hvoOwn, flid, iOrd, hvoNode, prsp, &nInd);
	if (idfe < 0)
		return; // We don't need to insert anything.

	// Make a copy of the input node and insert it at idfe in a collapsed state.
	// Note: Although pdetn is reference counted, we can't just refer to the original
	// because tree nodes hold unique expansion states.
	AfDeFeTreeNode * pdetn = pdetnSrc->CloneNode(this, nInd);
	AssertPtr(pdetn);
	m_vdfe.Insert(idfe, pdetn); // Add the new copy.
	ResetNodeHeaders(); // Adjust all tree headers since outline numbers may have changed.
	// Force height calculations on new editors.
	SetHeight();
	::InvalidateRect(m_hwnd, NULL, false); // Repaint entire window.
	return;
}


/*----------------------------------------------------------------------------------------------
	Given an owner and the flid for a subentry, and its order within similar subentries,
	return the index of where to insert this field in the current set of field editors.
	To determine this, we need to first see if the owner node (may be top entry) is present
	and expanded. If so, then we need to find the proper place to insert the new node.
	If the node is already present, we don't need to add it.
	@param hvoOwn The id of the object to which the field belongs.
	@param flid The node field id.
	@param iOrd The position within the flid vector for the new node.
	@param prsp Pointer to a RecordSpec that tells us where the node should be inserted.
	@param pnInd Pointer to receive the indent level of the inserted node.
	@return The index where the node should be inserted (or -1 if no insertion needed).
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::FindInsertionIndex(HVO hvoOwn, int flid, int iOrd, HVO hvoNode,
	RecordSpec * prsp, int * pnInd)
{
	Assert(hvoOwn);
	Assert(flid);
	Assert(iOrd >= 0);
	AssertPtr(pnInd);
	AssertPtr(prsp);

	int idfeNode = -1; // The index of the first field in the owning node.
	int pnIndNode; // The indent of the fields in the owning node.
	int cdfe = m_vdfe.Size();
	int idfe;
	AfDeFeNode * pden;
	AfDeFieldEditor * pdfe;

	// Get information on the beginning node (the one we are inserting into)
	if (cdfe > 0 && FieldAt(0)->GetOwner() == hvoOwn)
	{
		// Our beginning node is the root object.
		idfeNode = 0;
		pnIndNode = 0;
	}
	else
	{
		// Find the first node field that can own the new node.
		for (idfe = 0; idfe < cdfe; ++idfe)
		{
			pden = dynamic_cast<AfDeFeNode *>(FieldAt(idfe));
			if (!pden)
				continue; // Not a node.
			if (pden->GetTreeObj() == hvoOwn)
			{
				// We have the right owning node.
				if (pden->GetExpansion() == kdtsExpanded)
				{
					// We have an expanded node that can accept the new node.
					idfeNode = idfe + 1;
					pnIndNode = pden->GetIndent() + 1;
				}
				// If we found the owning node, but it is collapsed, idfeNode is unchanged.
				break;
			}
		}
	}
	if (idfeNode < 0)
		return idfeNode; // We don't have any place to insert the new node.

	int iOrdT = 0;
	int cbsp = prsp->m_vqbsp.Size();
	Vector<FldSpec *> vfsp;
	int ibsp;
	BlockSpec * pbsp;
	// Make a flat list of FldSpecs including the nested ones.
	for (ibsp = 0; ibsp < cbsp; ++ibsp)
	{
		pbsp = prsp->m_vqbsp[ibsp];
		vfsp.Push(pbsp);
		int cfsp = pbsp->m_vqfsp.Size();
		if (cfsp)
		{
			// We're not prepared to handle more than one nested FldSpec in a
			// sequenc or collection.
			Assert(cfsp == 1); // We're not prepared to handle more than one.
			vfsp.Push(pbsp->m_vqfsp[0]);
		}
	}
	int cfsp = vfsp.Size();
	int ifsp;
	int pnIndT;
	int flidBsp;
	idfe = idfeNode;
	int nNested = 0; // We are inside a nested collection/sequence.
	FldSpec * pfsp;
	bool fFoundFlid = false;

	// Now look through this object's fields and find the correct place for a subentry.
	// The block specs indicate the relative position of the field we are inserting.
	// Go through the block specs.
	for (ifsp = 0; ifsp < cfsp; ++ifsp)
	{
		pfsp = vfsp[ifsp];
		if (nNested)
		{
			if (nNested == 1)
				++nNested;
			else
			{
				// We've reached the end of the nested field, so get back to normal.
				nNested = false;
				--pnIndNode;
			}
		}
		// Skip past any fields that match this block spec.
		flidBsp = pfsp->m_flid;
		for (; idfe < cdfe; ++idfe)
		{
			pdfe = FieldAt(idfe);
			/*if (pbsp->m_vqfsp.Size())
			{
				// BlockSpecs holding vectors (e.g., Senses) will have a single FldSpec
				// that defines the nodes that will show up when the node is expanded.
				// These require some special processing to work correctly.
				AfDeFeNode * pdenT = dynamic_cast<AfDeFeNode *>(pdfe);
				AssertPtr(pdenT);
				if (pdenT->GetExpansion() == kdtsExpanded && pdenT->GetOwnerFlid() == flid)
				{
					pdfe = FieldAt(++idfe);
					++pnIndNode;
					//++iOrdT; Works dragging down, but not up.
				}
			}*/
			pnIndT = pdfe->GetIndent();
			int flidT = pdfe->GetOwnerFlid();
			if (pfsp->m_ft == kftObjOwnSeq || pfsp->m_ft == kftObjOwnCol &&
				pdfe->GetFldSpec() == pfsp)
			{
				nNested = 1;
				++pnIndNode;
				break; // Skip the fake vector node.
			}
			if (pnIndT < pnIndNode)
				break; // We've reached the end of this object's fields.
			if (pnIndT > pnIndNode)
				continue; // Skip nested fields.
			if (flidT != flidBsp)
				break; // We've reached the last field matching flidBsp.
			else if (flidT == flid)
			{
				// In rare cases where we drag a main entry to a subentry, and source
				// entry was immediately preceding the target entry, by the time this
				// method gets called, the node is already present, so we don't want to
				// add it again.
				fFoundFlid = true;
				pden = dynamic_cast<AfDeFeNode *>(pdfe);
				AssertPtr(pden);
				if (hvoNode == pden->GetTreeObj())
					return -1; // The node is already present.
				if (iOrdT == iOrd)
					break; // Exit loop if we are at the right level.
				++iOrdT;
			}
		}
		if (nNested == 1 && pdfe->GetFldSpec() == pfsp)
		{
			++idfe;
			continue; // We're skipping by the fake collection field.
		}
		// We've found our spot if we've reached the end of the object's fields
		// or we've passed the location of the requested field.
		if (fFoundFlid && (pnIndT < pnIndNode) || flidBsp == flid && iOrdT == iOrd)
			break;
	}
	Assert(pnIndT < pnIndNode || flidBsp == flid && iOrdT == iOrd); // flid is invalid.
	*pnInd = pnIndNode;
	return idfe;
}


/*----------------------------------------------------------------------------------------------
	Return the field editor index for a new node with the specified flid and an owner of hvo.
	Where there are already nodes with the specified flid, this returns an index that will
	allow appending to the end of the specified nodes.
	This should not be called if we may not have a valid place for insertion.
	@param hvo The id of the object to which the field belongs.
	@param flid The field id.
	@param prsp Pointer to a RecordSpec that lets us find the correct location according to
		user preferences.
	@return An index indicating where we should insert the new node.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::EndOfNodes(HVO hvo, int flid, RecordSpec * prsp)
{
	Assert(hvo);
	Assert(flid);
	AssertPtr(prsp);

	int nInd;
	int idfe;
	int cdfe = m_vdfe.Size();
	AfDeFieldEditor * pdfe;

	for (idfe = 0; idfe < cdfe; ++idfe)
	{
		// Find the first field with the correct owner.
		pdfe = FieldAt(idfe);
		if (pdfe->GetOwner() != hvo)
			continue;
		nInd = pdfe->GetIndent(); // Save the indent level.
		break;
	}

	Assert(idfe != cdfe); // hvo was illegal if we can't find a field with this owner.

	int cbsp = prsp->m_vqbsp.Size();
	int ibsp;
	int nIndT;
	int flidBsp;

	// Now look through this object's fields and find the correct place for a subentry.
	// The block specs indicate the relative position of the field to insert.
	// Go through the block specs.
	for (ibsp = 0; ibsp < cbsp; ++ibsp)
	{
		// Skip past any fields that match this block spec.
		flidBsp = prsp->m_vqbsp[ibsp]->m_flid;
		for (; idfe < cdfe; ++idfe)
		{
			pdfe = FieldAt(idfe);
			nIndT = pdfe->GetIndent();
			if (nIndT < nInd)
				break; // We've reached the end of this object's fields.
			if (nIndT > nInd)
				continue; // Skip nested fields.
			if (pdfe->GetOwnerFlid() != flidBsp)
				break; // We've reached the last field matching flidBsp.
		}
		// We've found our spot if we've reached the end of the object's fields
		// or we've passed the location of the requested field.
		if (nIndT < nInd || flidBsp == flid)
			break;
	}
	Assert(nIndT < nInd || flidBsp == flid); // flid must have been invalid.
	return idfe;
}


/*----------------------------------------------------------------------------------------------
	Return the field editor index of the node that contains a target field.
	@param pdfe Pointer to the target field editor, or NULL to default to the active editor.
	@return The field editor index of the node, or -1 if not owned by a node.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::GetCurNodeIndex(AfDeFieldEditor * pdfe)
{
	AssertPtrN(pdfe);

	if (!pdfe)
		pdfe = m_pdfe;
	int idfe;
	// Find the specified field.
	for (idfe = m_vdfe.Size(); --idfe >= 0; )
	{
		if (pdfe == m_vdfe[idfe])
			break;
	}
	// Now back up to the next node.
	while (--idfe >= 0)
	{
		AfDeFeNode * pden = dynamic_cast<AfDeFeNode *>(m_vdfe[idfe]);
		if (pden)
			break;
	}
	return idfe;
}


/*----------------------------------------------------------------------------------------------
	Resets all node headers for this window. This is needed since node
	movement/addition/deletion can cause other node headers to be renumbered.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::ResetNodeHeaders()
{
	// Loop through all editors.
	for (int idfe = m_vdfe.Size(); --idfe >= 0; )
	{
		AfDeFeNode * pden = dynamic_cast<AfDeFeNode *>(m_vdfe[idfe]);
		if (pden)
			// The editor is a node, so reset the header.
			SetTreeHeader(pden);
	}
}


/*----------------------------------------------------------------------------------------------
	Clear out the window contents, and especially any active database connections, when the
	project is (about to be) changed. Return true if the close is OK.

	@return true if the close is OK.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::CloseProj()
{
	// Close current editor if there is one.
	if (!CloseEditor())
		return false;
	// Save any data that changed.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(prmw);
	// Conditionally update the modification date of the last record edited.
	AfDeRecSplitChild * parscCur = dynamic_cast<AfDeRecSplitChild*>(this);
	if (parscCur)
		prmw->UpdateRecordDate(parscCur->GetLastObj());
	prmw->SaveData();
	// We don't need to call OnReleasePtr here. It gets called in AfWnd::WndProcPost on
	// the WM_NCDESTROY message.
	return true;
}


/*----------------------------------------------------------------------------------------------
	Save any open edits, but don't close the editor.
	@return True if successful, false otherwise.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::Save()
{
	// Save current editor if there is one.
	bool fRet = true;
	if (m_pdfe)
	{
		if (!m_pdfe->IsOkToClose())
			return false; // Can't close for some reason. User was notified of error.
		fRet = m_pdfe->SaveEdit();
	}
	// Conditionally update the modification date of the last record edited.
	AfDeRecSplitChild * parscCur = dynamic_cast<AfDeRecSplitChild*>(this);
	if (parscCur)
	{
		HVO hvoLast = parscCur->GetLastObj();
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
		AssertPtr(prmw);
		prmw->UpdateRecordDate(hvoLast);
	}
	return fRet;
}


/*----------------------------------------------------------------------------------------------
	Override this if you want a field height for the uncreated editors that is
	more than the default. Don't answer less than SuperClass::DefaultFieldHeight().
	The only reason to override in AfDeSplitChild would be to get a larger minimum height.
	In AfLazyDeWnd, override to change the default height of a field editor that has
	not yet been created.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::DefaultFieldHeight()
{
	return GetMinFieldHeight();
//	return Max((int)kdypBoxHeight, m_dypTreeFont);
}


/*----------------------------------------------------------------------------------------------
	Attempt to open to a field in a nested object. This does nothing if it cannot follow
	the path to the desired object. It assumes that the top level fields have already been
	added. It also assumes that any nested objects are displayed through expanding a tree node.
	@param vhvoPath This is a path of objects to traverse to get to the nested object. The
		first HVO in the list should be the top-level object.
	@param vflidPath This is a path of flids that corresponds to the object list. The first
		flid should be the flid on the first HVO in vhvoPath that contains the next object in
		hvoPath, if there is one. If a final flid is given, we try to open that field. Otherwise
		we open the first editable field in the last nested object.
	@param ichCur The cursor index within the field.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::OpenPath(Vector<HVO> & vhvoPath, Vector<int> & vflidPath, int ichCur)
{
	// REVIEW KenZ(RandyR): What should be done for AfDeFeVectorNode?

	int chvo = vhvoPath.Size();
	if (!chvo)
		return; // Nothing to do.

	if (!m_vdfe.Size())
		return; // Nothing to do. (Can happen during FullRefresh in list editor).

	// Previous editor needs to be closed prior to this or it will mess up the path.
	Assert(!m_pdfe);
	// First HVO in path should be top object, except in list editor where we have a tree
	// view for nested items but only show the top item.
	// There are too many exceptions in M3me, etc. for this Assert to work reliably.
	//Assert(m_vdfe[0]->GetOwner() == vhvoPath[0] ||
	//	vflidPath.Size() && vflidPath[0] == kflidCmPossibility_SubPossibilities);
	int idfe;
	int ihvo;

	// Expand tree nodes to get to the final object.
	for (ihvo = 0; ihvo < vhvoPath.Size() - 1; ++ihvo)
	{
		if (vflidPath[ihvo] == kflidStText_Paragraphs)
		{
			--ihvo;
			break; // We are at a StText.
		}
		for (idfe = m_vdfe.Size(); --idfe >= 0; )
		{
			AfDeFeNode * pden = dynamic_cast<AfDeFeNode *>(m_vdfe[idfe]);
			if (!pden)
				continue; // Skip non-tree nodes.
			if (pden->GetOwner() == vhvoPath[ihvo] && pden->GetTreeObj() == vhvoPath[ihvo + 1])
			{
				// We have the right tree node. If it isn't expanded, expand it now.
				if (pden->GetExpansion() != kdtsExpanded)
					ToggleExpansion(idfe);
				break; // Go to next item in path.
			}
		}
	}

	SetHeight(); // This is needed to get views properly initialized
	HVO hvo = vhvoPath[ihvo];
	if (ihvo < vflidPath.Size())
	{
		// A final field was specified, so try to open it.
		int flid = vflidPath[ihvo];
		for (idfe = m_vdfe.Size(); --idfe >= 0; )
		{
			AfDeFieldEditor * pdfe = m_vdfe[idfe];
			if (pdfe->GetOwner() == hvo && pdfe->GetOwnerFlid() == flid && pdfe->IsEditable())
			{
				// Since OpenEditor resets cursor parameters in MainWnd, we need to
				// make a copy here to restore the cursor.
				Vector<int> vflid;
				Vector<HVO> vhvo;
				Assert(vhvoPath.Size() == vflidPath.Size());
				for (ihvo; ihvo < vhvoPath.Size(); ++ihvo)
				{
					vhvo.Push(vhvoPath[ihvo]);
					vflid.Push(vflidPath[ihvo]);
				}
				bool fOpen = OpenEditor(idfe, false);
				if (fOpen)
				{
					pdfe->RestoreCursor(vhvo, vflid, ichCur);
					return; // We succeeded in opening this field.
				}
			}
		}
	}
	// Try to open the first editable field in the final object.
	int cdfe = m_vdfe.Size();
	for (idfe = 0; idfe < cdfe; ++idfe)
	{
		if (m_vdfe[idfe] && m_vdfe[idfe]->GetOwner() == hvo)
		{
			OpenEditor(idfe, true); // Open first editable editor.
			return;
		}
	}
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
STDMETHODIMP AfDeSplitChild::DragOver(DWORD grfKeyState, POINTL pt, DWORD * pdwEffect)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pdwEffect);

	//StrAnsi sta;
	//sta.Format("DragOver: %x, %d, %d, %x\n", grfKeyState, pt.x, pt.y, *pdwEffect);
	//OutputDebugString(sta.Chars());

	DWORD dwEffect = *pdwEffect;
	*pdwEffect = DROPEFFECT_NONE;

	::ImageList_DragMove(pt.x, pt.y);

	// If we don't have an object, we don't want to go any further.
	if (!m_hvoDrag)
		return S_OK;

	POINT ptT;
	ptT.x = pt.x;
	ptT.y = pt.y;
	::ScreenToClient(m_hwnd, &ptT);

	if (ptT.x < GetBranchWidthAt(ptT.y))
	{
		// We don't allow moves across processes.
		if (dwEffect & DROPEFFECT_MOVE && m_pidDrag == (int)::GetCurrentProcessId() &&
			OnDragOverTree(ptT.x, ptT.y))
		{
			*pdwEffect = DROPEFFECT_MOVE;
		}
		return S_OK;
	}

	// Get rid of any tree target left on the screen.
	ClearTarget(pt.x, pt.y);

	if (!(dwEffect & DROPEFFECT_LINK))
		return S_OK;

	int idfe = GetField(ptT.y);

	if (idfe == m_vdfe.Size())
		return S_OK; // Nothing to do if below last field.

	AfDeFeRefs * pdfr = dynamic_cast<AfDeFeRefs *>(m_vdfe[idfe]);
	if (!pdfr)
		return S_OK; // Not over a reference field.

	IFwMetaDataCachePtr qmdc;
	m_qlpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
	AssertPtr(qmdc);
	ComBool fValid;
	qmdc->get_IsValidClass(pdfr->GetFlid(), m_clidDrag, &fValid);
	if (!fValid)
		return S_OK; // Not over a valid reference field for the class we are dragging.

	// We have a valid link target.
	*pdwEffect = DROPEFFECT_LINK;
	return S_OK;

	END_COM_METHOD(g_fact, IID_IDropTarget);
}


/*----------------------------------------------------------------------------------------------
	Removes target feedback and releases the data object. To implement IDropTarget::DragLeave,
	you must remove any target feedback that is currently displayed. You must also release any
	references you hold to the data transfer object.
	@return S_OK Operation succeeded.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeSplitChild::DragLeave(void)
{
	BEGIN_COM_METHOD

	ClearTarget(0, 0);
	return SuperClass::DragLeave();

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
STDMETHODIMP AfDeSplitChild::Drop(IDataObject * pDataObject, DWORD grfKeyState, POINTL pt,
	DWORD * pdwEffect)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pDataObject);
	ChkComArgPtr(pdwEffect);

	//StrAnsi sta;
	//sta.Format("Drop: %x, %d, %d, %x\n", grfKeyState, pt.x, pt.y, *pdwEffect);
	//OutputDebugString(sta.Chars());
	DWORD dwEffect = *pdwEffect;
	*pdwEffect = DROPEFFECT_NONE;
	AfDeFeRefs * pdfr;
	int idfe;

	ClearTarget(pt.x, pt.y);
	POINT ptT; // Cursor in client coordinates.
	ptT.x = pt.x;
	ptT.y = pt.y;
	::ScreenToClient(m_hwnd, &ptT);

	// For now, assume if we aren't dropping our own object, we do nothing.
	if (!m_hvoDrag)
		goto LExit;

	if (ptT.x < GetBranchWidthAt(ptT.y))
	{
		if (dwEffect & DROPEFFECT_MOVE)
		{
			// We need to make the move here and notify other windows.
			MoveRecord(m_hvoDrag, m_clidDrag, m_idfeDst, m_drp);
			*pdwEffect = DROPEFFECT_MOVE; // Notify caller of move.
		}
		goto LExit;
	}

	if (!(dwEffect & DROPEFFECT_LINK))
		goto LExit; // Linking is not being requested, so do nothing.

	idfe = GetField(ptT.y);
	if (idfe == m_vdfe.Size())
		goto LExit; // Do nothing if below last field.

	pdfr = dynamic_cast<AfDeFeRefs *>(m_vdfe[idfe]);
	if (!pdfr)
		goto LExit; // Do nothing if we aren't over a reference field.

	// We need to do a link here.
	pdfr->DropObject(m_hvoDrag, m_clidDrag, pt);
	*pdwEffect = DROPEFFECT_LINK; // Notify caller of link.

LExit:
	m_hvoDrag = 0;
	m_clidDrag = 0;
	if (m_himlDrag)
	{
		AfGdi::ImageList_Destroy(m_himlDrag);
		m_himlDrag = NULL;
	}
	return S_OK;

	END_COM_METHOD(g_fact, IID_IDropTarget);
}


/*----------------------------------------------------------------------------------------------
	This launches a dialog to choose an item for a reference field.

	This method should be overwritten to do something more useful.

	@param pdfr The reference field editor making this request
	@return An object id to add to the reference field. 0 for none.
----------------------------------------------------------------------------------------------*/
HVO AfDeSplitChild::LaunchRefChooser(AfDeFeRefs * pdfr)
{
	// REVIEW KenZ (RandyR): Since some ref. attrs hold more than one HVO,
	// shouldn't the return value be a vector of HVOs, rather than just one of them?
	StrApp strMsg(kstidLaunchRefChooser);
	StrApp strHead(kstidLaunchRefHeader);
	::MessageBox(m_hwnd, strMsg.Chars(), strHead.Chars(), MB_ICONINFORMATION);
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Indicates whether a drop can be accepted, and, if so, the effect of the drop.
	@param pdobj Pointer to the interface of the source data object.
	@param grfKeyState Current state of keyboard modifier keys.
	@param pt Current mouse position in long screen coordinates.
	@param pdwEffect Pointer to the effect of the drag-and-drop operation.
	@return S_OK Operation succeeded. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeSplitChild::DragEnter(IDataObject * pdobj, DWORD grfKeyState, POINTL pt,
	DWORD * pdwEffect)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pdobj);
	ChkComArgPtr(pdwEffect);

	HRESULT hr;
	IgnoreHr(hr = SuperClass::DragEnter(pdobj, grfKeyState, pt, pdwEffect));
	if (FAILED(hr))
		return hr;

	// Close active editor before allowing drag.
	if (m_pdfe)
	{
		if (!IsOkToChange())
		{
			m_hvoDrag = 0; // Don't allow drag if we can't close editor.
			return S_OK;
		}
		CloseEditor();
	}
	return S_OK;

	END_COM_METHOD(g_fact, IID_IDropTarget);
}


/*----------------------------------------------------------------------------------------------
	Show the right-click menu.
	@param hwnd Handle to the window over which the mouse was right-clicked.
	@param pt Mouse location in screen coordinates.
	@return true to prevent the message from being sent to other windows.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::OnContextMenu(HWND hwnd, Point pt)
{
	if (!m_vdfe.Size())
		return false; // Don't show menu when we have nothing showing.

	HMENU hmenuPopup = ::CreatePopupMenu();

	bool fMouseOp = true;
	if (pt.x == -1 && pt.y == -1)
	{
		// Coordinates at -1,-1 is the indication that the user triggered the menu with the
		// keyboard, and not the mouse:
		fMouseOp = false;
	}

	Point ptClient;
	if (fMouseOp)
	{
		ptClient = pt; // Point in AfDeSplitChild coordinates.
		::ScreenToClient(m_hwnd, &ptClient);
		// Save the current field index:
		m_idfe = GetField(ptClient.y);
	}
	else
	{
		// Mouse not used. Menu should go in top left of client window:
		ptClient.Set(0, 0);
		pt = ptClient;
		::ClientToScreen(m_hwnd, &pt);
		// Find the current field index:
		for (m_idfe = m_vdfe.Size(); --m_idfe >= 0;)
		{
			if (m_pdfe == m_vdfe[m_idfe])
				break;
		}
	}

	// If below last field, use last field.
	if (m_idfe >= m_vdfe.Size())
		m_idfe = m_vdfe.Size() - 1;
	if (m_idfe < 0)
		m_idfe = 0;

	StrApp str;
	bool fTreePane = true;
	if (fMouseOp)
	{
		if (ptClient.x >= GetBranchWidthAt(ptClient.y))
			fTreePane = false;
	}

	if (fTreePane)
	{
		// Add menus for tree pane.
		HMENU hmenuInsert = ::CreatePopupMenu();
		str.Load(kstidContextInsert);
		::AppendMenu(hmenuPopup, MF_POPUP, (uint)hmenuInsert, str.Chars());
		AddContextInsertItems(hmenuInsert);
		HMENU hmenuShow = ::CreatePopupMenu();
		str.Load(kstidContextShow);
		::AppendMenu(hmenuPopup, MF_POPUP, (uint)hmenuShow, str.Chars());
		::AppendMenu(hmenuShow, MF_STRING, kcidExpShow, NULL);
//		::EnableMenuItem(hmenuPopup, (uint)hmenuShow, MF_BYCOMMAND | MF_GRAYED);
		str.Load(kstidContextPromote);
		::AppendMenu(hmenuPopup, MF_STRING, kcidContextPromote, str.Chars());
		str.Load(kstidContextDelete);
		::AppendMenu(hmenuPopup, MF_STRING, kcidEditDel, str.Chars());
/*Enhancement for Version 2
		str.Load(kstidContextHelp);
		::AppendMenu(hmenuPopup, MF_STRING, kcidContextHelp, str.Chars());
Enhancement for Version 2*/
	}
	else
	{
		// Add menus for data pane.
		str.Load(kstidContextCut);
		::AppendMenu(hmenuPopup, MF_STRING, kcidEditCut, str.Chars());
		str.Load(kstidContextCopy);
		::AppendMenu(hmenuPopup, MF_STRING, kcidEditCopy, str.Chars());
		str.Load(kstidContextPaste);
		::AppendMenu(hmenuPopup, MF_STRING, kcidEditPaste, str.Chars());
/*Enhancement for Version 2
		::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
		str.Load(kstidContextWritingSystem);
		::AppendMenu(hmenuPopup, MF_STRING, kcidContextWritingSystem, str.Chars());
		::AppendMenu(hmenuPopup, MF_SEPARATOR, 0, NULL);
		str.Load(kstidContextHelp);
		::AppendMenu(hmenuPopup, MF_STRING, kcidContextHelp, str.Chars());
Enhancement for Version 2*/
	}
	AddExtraContextMenuItems(hmenuPopup);

	// Make sure the handlers get to this window.
	MainWindow()->SetContextInfo(this, pt);
	// Process the command as a popup before we leave this method.
	// Using m_hwnd as the second to last argument currently breaks the what's this help. Using the hwnd
	// for the main window is a kludge that works for the time being.
	//int nCmd = TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_LEFTBUTTON | TPM_RIGHTBUTTON | TPM_RETURNCMD,
	//	pt.x, pt.y, 0, MainWindow()->Hwnd(), NULL);
	// However, not using m_hwnd can disable the Insert command sub-entries when used more than
	// once. The what's this help seems to be OK now with m_hwnd. So restore it (JohnL).
	int nCmd = TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_LEFTBUTTON | TPM_RIGHTBUTTON | TPM_RETURNCMD,
		pt.x, pt.y, 0, m_hwnd, NULL);
	if (nCmd)
		::SendMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(nCmd, kPopupMenu), NULL);

	::DestroyMenu(hmenuPopup);
	return true;
}


/*----------------------------------------------------------------------------------------------
	This method is used for three purposes.
	1)	If pcmd->m_rgn[0] == AfMenuMgr::kmaExpandItem, it is being called to expand the dummy
		item by adding new items.
	2)	If pcmd->m_rgn[0] == AfMenuMgr::kmaGetStatusText, it is being called to get the status
		bar string for an expanded item.
	3)	If pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand, it is being called because the user
		selected an expandable menu item.

	Expanding items:
		pcmd->m_rgn[1] -> Contains the handle to the menu (HMENU) to add items to.
		pcmd->m_rgn[2] -> Contains the index in the menu where you should start inserting items.
		pcmd->m_rgn[3] -> This value must be set to the number of items that you inserted.
	The expanded items will automatically be deleted when the menu is closed. The dummy
	menu item will be deleted for you, so don't do anything with it here.

	Getting the status bar text:
		pcmd->m_rgn[1] -> Contains the index of the expanded/inserted item to get text for.
		pcmd->m_rgn[2] -> Contains a pointer (StrApp *) to the text for the inserted item.
	If the menu item does not have any text to show on the status bar, return false.

	Performing the command:
		pcmd->m_rgn[1] -> Contains the handle of the menu (HMENU) containing the expanded items.
		pcmd->m_rgn[2] -> Contains the index of the expanded/inserted item to get text for.

	@param pcmd Ptr to menu command

	@return
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::CmdExpContextMenu(Cmd * pcmd)
{
	AssertPtr(pcmd);
	//StrApp str;
	//str.Format("Wnd = %x, m_cid = %d, vbsp = %d\n", m_hwnd, pcmd->m_cid, m_vbsp.Size());
	//OutputDebugString(str.Chars());

	// Don't allow menus if we can't close the current editor.
	if (!IsOkToChange())
		return false;

	int ma = pcmd->m_rgn[0];
	if (ma == AfMenuMgr::kmaExpandItem)
	{
		// We need to expand the dummy menu item.
		HMENU hmenu = (HMENU)pcmd->m_rgn[1];
		int imni = pcmd->m_rgn[2];
		int & cmniAdded = pcmd->m_rgn[3];

		switch (pcmd->m_cid)
		{
		case kcidExpShow:
			{
				// Get the RecordSpec for the current object.
				RecordSpec * prsp = GetRecordSpec(m_idfe);
				Assert(prsp);
				// Fill vector with BlockSpecs that are "If Data Present", but are not
				// currently showing.

				// Find the first field for the current object.
				int idfe = FirstFieldOfTreeNode(m_idfe);

				// Now look through this object's fields and find the missing
				// "If Data Present" fields.
				int cbsp = prsp->m_vqbsp.Size();
				int cdfe = m_vdfe.Size();
				int ibsp;
				int flidDfe;
				int flidBsp;
				// We need to use the indent from the first field, not the current
				// field, since the current field (e.g., RnEvent_Participants) may have a
				// phony indent.
				int nInd = m_vdfe[idfe]->GetIndent();
				AfDeFieldEditor * pdfe;
				BlockSpec * pbsp;
				m_vbsp.Clear();
				for (ibsp = 0; ibsp < cbsp; ++ibsp)
				{
					// Skip past any fields that match this block spec.
					pbsp = prsp->m_vqbsp[ibsp];
					flidBsp = pbsp->m_flid;
					bool fFound = false;
					for (; idfe < cdfe; ++idfe)
					{
						pdfe = FieldAt(idfe);
						int nIndT = pdfe->GetIndent();
						if (nIndT > nInd)
							continue; // Skip nested fields.
						flidDfe = pdfe->GetOwnerFlid();
						if (nIndT < nInd)
						{
							idfe = cdfe; // Disable any further checking.
							break; // We've reached the end of this object's fields.
						}
						if (flidDfe == flidBsp)
						{
							fFound = true;
							continue;
						}
						else
							break; // We've reached the last field matching flidBsp.
					}
					// Don't show any fields that are already present or that don't
					// have "If data present" selected.
					// We can't include subentries in the menu or we'll get a crash
					// later when we try to insert a blank field editor. Subentries will
					// already be shown if there are any. If there aren't any, they can't
					// be added in this way, but must be added using an insert option.
					if (fFound || pbsp->m_eVisibility != kFTVisIfData ||
						pbsp->m_ft == kftSubItems)
					{
						continue;
					}
					// We have a block speck to add. First find the appropriate insertion
					// spot so we keep the list sorted.
					int ibspT;
					StrUni stuNew;
					ITsStringPtr qtss = pbsp->m_qtssLabel;
					const wchar * prgch;
					int cch;
					if (SUCCEEDED(qtss->LockText(&prgch, &cch)))
					{
						stuNew.Assign(prgch, cch);
						CheckHr(qtss->UnlockText(prgch));
					}
					for (ibspT = 0; ibspT < m_vbsp.Size(); ++ibspT)
					{
						StrUni stu;
						qtss = m_vbsp[ibspT]->m_qtssLabel;
						if (SUCCEEDED(qtss->LockText(&prgch, &cch)))
						{
							stu.Assign(prgch, cch);
							CheckHr(qtss->UnlockText(prgch));
						}
						if (stuNew.Compare(stu) < 0)
							break;
					}
					m_vbsp.Insert(ibspT, pbsp); // Add the new hidden field.
				}

				// Now add menu items for any fields currently hidden due to lack of data.
				for (ibsp = 0; ibsp < m_vbsp.Size(); ++ibsp)
				{
					ITsStringPtr qtss = m_vbsp[ibsp]->m_qtssLabel;
					const wchar * prgch;
					int cch;
					StrApp str;
					if (SUCCEEDED(qtss->LockText(&prgch, &cch)))
					{
						str.Assign(prgch, cch);
						CheckHr(qtss->UnlockText(prgch));
					}
					::InsertMenu(hmenu, imni + ibsp, MF_BYPOSITION, kcidMenuItemDynMin + ibsp,
						str.Chars());
				}
				cmniAdded = ibsp;
				AddContextShowItems(hmenu);
				return true;
			}
		}
	}
	else if (ma == AfMenuMgr::kmaGetStatusText)
	{
		// We need to return the text for the expanded menu item.
		//    m_rgn[1] holds the index of the selected item.
		//    m_rgn[2] holds a pointer to the string to set

		int imni = pcmd->m_rgn[1];
		StrApp * pstr = (StrApp *)pcmd->m_rgn[2];
		AssertPtr(pstr);

		switch (pcmd->m_cid)
		{
		case kcidExpShow:
			if (!m_vbsp.Size() || imni >= m_vbsp.Size())
				return false; // Mouse is outside of the menu.
			StrApp str(kstidShowBody);
			ITsStringPtr qtss = m_vbsp[imni]->m_qtssLabel;
			const wchar * prgch;
			int cch;
			if (SUCCEEDED(qtss->LockText(&prgch, &cch)))
			{
				str.Append(prgch, cch);
				CheckHr(qtss->UnlockText(prgch));
			}
			pstr->Assign(str.Chars());
			return true;
		}
	}
	else if (ma == AfMenuMgr::kmaDoCommand)
	{
		// The user selected an expanded menu item, so perform the command now.
		//    m_rgn[1] holds the menu handle.
		//    m_rgn[2] holds the index of the selected item.

		int iitem;
		iitem = pcmd->m_rgn[2];
		switch (pcmd->m_cid)
		{
		case kcidExpShow:
			{
				// Close active editor before showing a new field.
				if (!CloseEditor())
					return false;
				// Add the new field at the correct location for the block spec.
				RecordSpec * prsp = GetRecordSpec(m_idfe);
				BlockSpec * pbsp = m_vbsp[iitem];

				// Find the starting field for this object.
				int idfe = FirstFieldOfTreeNode(m_idfe);

				/* This sort of works, but opens up expectations in the user that we cannot
				reasonably fulfill. For example, they would expect all other windows to show
				the same thing, but those windows have no idea that this is temporarily turned
				on. They would also expect undo/redo to work. They would expect the same thing
				to happen in participant fields, although other panes may not even have the
				participants fields expanded. They would also expect a temporarily unhidden
				view to show up when splitting a window. Outside of persisting an actual change
				to the visibility of the FldSpec, it doesn't seem we have a workable solution.
				// Show the field in the other pane, if there is one.
				RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
				AfMdiClientWnd * pmdic = prmw->GetMdiClientWnd();
				AfSplitterClientWnd * pacw =
					dynamic_cast<AfSplitterClientWnd *>(pmdic->GetCurChild());
				AfDeSplitChild * padsc;
				padsc = dynamic_cast<AfDeSplitChild *>(pacw->GetPane(pacw->GetPane(0) == this ? 1 : 0));
				if (padsc)
				{
					padsc->ShowField(prsp, pbsp, idfe);
					padsc->SetHeight();
					::InvalidateRect(padsc->Hwnd(), NULL, true);
				} */

				// Now show the field in the current pane and activate the field.
				idfe = ShowField(prsp, pbsp, idfe);
				ActivateField(idfe);
				return true;
			}
		}
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Show a hidden field.
	@param prsp The RecordSpec used for this record
	@param pbsp The BlockSpec for the field we want to show.
	@param idfe Index of the first field for the object owning the field we want to show.
	@return Index to the newly inserted field.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::ShowField(RecordSpec * prsp, BlockSpec * pbsp, int idfe)
{
	// Add the new field at the correct location for the block spec.
	CustViewDaPtr qcvd;
	m_qlpi->GetDataAccess(&qcvd);

	// Now look through this object's fields and find the insertion place.
	int cbsp = prsp->m_vqbsp.Size();
	int cdfe = m_vdfe.Size();
	int ibsp;
	int flidDfe;
	int flidBsp;
	// We need to use the indent from the first field, not the current
	// field, since the current field (e.g., RnEvent_Participants) may have a
	// phony indent.
	int nInd = m_vdfe[idfe]->GetIndent();
	// We use this to get the owner of the field in case it has a phony owner
	// (e.g., RnRoledPartic). Drag object should return a reasonable owner.
	HVO hvo = GetDragObject(m_vdfe[idfe]);
	int nIndT;
	AfDeFieldEditor * pdfe;
	BlockSpec * pbspT;
	for (ibsp = 0; ibsp < cbsp; ++ibsp)
	{
		// Skip past any fields that match this block spec.
		pbspT = prsp->m_vqbsp[ibsp];
		flidBsp = pbspT->m_flid;
		bool fFound = false;
		for (; idfe < cdfe; ++idfe)
		{
			pdfe = FieldAt(idfe);
			nIndT = pdfe->GetIndent();
			if (nIndT > nInd)
				continue; // Skip nested fields.
			flidDfe = pdfe->GetOwnerFlid();
			// Exit loop if we've reached the end of this object's fields
			// or we've reached the last field matching flidBsp.
			if (nIndT < nInd || flidDfe != flidBsp)
				break;
			fFound = true;
		}
		if (fFound)
			continue;
		else if (pbsp == pbspT || nIndT < nInd)
			break; // idfe is at the point where we should insert.
	}

	// Add the field and make it the active editor.
	int idfeT = idfe;
	AddField(hvo, prsp->m_clsid, prsp->m_nLevel, pbsp, qcvd, idfeT, nInd, true);
	return idfe;
}


/*----------------------------------------------------------------------------------------------
	Get editor index, indent level, and owner.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::GetIndexAndIndent(bool fPopupMenu, int iflid, bool &fFlid, int & idfe,
	int & nInd, HVO & hvoOwner)
{
	// REVIEW KenZ(RandyR): What should be done for AfDeFeVectorNode?
	// REVIEW KenZ(RandyR): See if you want to use this method in RN & CLE.
	// LexEd & Mme use it.
	Assert(iflid);
	fFlid = false;
	idfe = nInd = hvoOwner = 0;
	// If we have a current editor
	if (m_pdfe)
	{
		if (!fPopupMenu)
		{
			// Save index and indent for the current editor before closing.
			nInd = m_pdfe->GetIndent();
			for (idfe = 0; idfe < m_vdfe.Size(); ++idfe)
				if (m_vdfe[idfe] == m_pdfe)
					break;
		}
		// Now close the current editor or abort if we can't.
		if (!CloseEditor())
			return false;
	}

	// If we are coming from a right-click menu, we get the field info for the field
	// over which the user right-clicked. If it is a subentry node, assume we add to the
	// subentry.
	if (fPopupMenu)
	{
		Assert(false);
		AfDeFeNode * pden = dynamic_cast<AfDeFeNode *>(m_vdfe[m_idfe]);
		if (pden && pden->GetOwnerFlid() == iflid)
		{
			if (pden->GetExpansion() == kdtsCollapsed)
				ToggleExpansion(m_idfe);
			idfe = m_idfe + 1;
			nInd = pden->GetExpansion() + 1;
			fFlid = true;
		}
		else
		{
			idfe = m_idfe;
			nInd = m_vdfe[idfe]->GetIndent();
		}
	}
	hvoOwner = m_vdfe[idfe]->GetOwner();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Insert subitem of the main record being shown.

	@param clsidOwner Class Id of the owner of the new object.
	@param flidOwner flid where the new object will be placed.
	@param flidOwnerModified flid on owner for modified date, or '0' for none.
	@param flidType flid type where the new object will be placed.
	@param hvoOwner Id of the owner of the new object.
	@param clsidNew Class id of object to be created.
	@param stidUndoRedo Id of the string resource to be used for 'Undo/Redo' message.
	@param stidLabel Id of the string resource to be used for tree label.
	@param nIndent Indent level for new item.

	@return true if object was created, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::InsertSubItem(int clsidOwner, int flidOwner, int flidOwnerModified,
			int flidType, HVO hvoOwner,
			int clsidNew, int stidUndoRedo, int stidLabel, int nIndent)
{
	int ihvoOld;
	HVO hvoNew;
	int idfe;
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(prmw);
	CustViewDaPtr qcvd = prmw->MainDa();	// Get size before adding new item.
	Assert(qcvd);

	try
	{
		switch (flidType)
		{
		default:
			Assert(false);
			return false;
			break;
		case kcptOwningAtom:
			ihvoOld = -2;
			break;
		case kcptOwningCollection:
			ihvoOld = -1;
			break;
		case kcptOwningSequence:
			CheckHr(qcvd->get_VecSize(hvoOwner, flidOwner, &ihvoOld));
			break;
		}
		hvoNew = prmw->CreateUndoableObject(hvoOwner, flidOwner, flidOwnerModified,
				clsidNew, stidUndoRedo, ihvoOld);
		if (hvoNew == 0)
			return false;

		// Get the RecordSpec for owner.
		RecordSpecPtr qrsp;
		BlockSpecPtr qbsp;
		ClsLevel clevOwner(clsidOwner, 0);
		m_quvs->m_hmclevrsp.Retrieve(clevOwner, qrsp);
		AssertPtr(qrsp);
		// Get BlockSpec.
		int cbsp = qrsp->m_vqbsp.Size();
		for (int ibsp = 0; ibsp < cbsp; ++ibsp)
		{
			if (qrsp->m_vqbsp[ibsp]->m_flid == flidOwner)
			{
				qbsp = qrsp->m_vqbsp[ibsp];
				break;
			}
		}
		AssertPtr(qbsp);

		// Add a new expanded tree node to the end of the field editors
		// with fields for a new object.
		ITsStringPtr qtssLabel;
		AfUtil::GetResourceTss(stidLabel, prmw->UserWs(), &qtssLabel);
		AfDeFeTreeNode * pdetn = NewObj AfDeFeTreeNode;
		pdetn->Initialize(hvoOwner, flidOwner, nIndent, qtssLabel,
			qbsp->m_qtssHelp, this, qbsp);
		pdetn->Init();
		pdetn->SetClsid(clsidNew);
		pdetn->SetTreeObj(hvoNew);
		SetTreeHeader(pdetn); // Initialize the template with a header value.

		// Add the new object to any other data entry windows that need it.
		Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
		int cafw = vqafw.Size();
		for (int iafw = 0; iafw < cafw; iafw++)
		{
			prmw = dynamic_cast<RecMainWnd *>(vqafw[iafw].Ptr());
			AssertPtr(prmw);
			if (prmw->GetLpInfo() == m_qlpi)
			{
				AfMdiClientWndPtr qmdic = prmw->GetMdiClientWnd();
				AssertPtr(qmdic);
				int ccwnd = qmdic->GetChildCount();
				// Go through all DE windows.
				for (int icwnd = 0; icwnd < ccwnd; ++icwnd)
				{
					AfSplitterClientWndPtr qacw = dynamic_cast<AfSplitterClientWnd *>(
						qmdic->GetChildFromIndex(icwnd));
					if (!qacw || !qacw->Hwnd())
						continue; // Not an active data entry window.
					// Make sure it is added to both panes (but not to the current pane).
					AfDeSplitChildPtr qadsc;
					qadsc = dynamic_cast<AfDeSplitChild *>(qacw->GetPane(0));
					if (qadsc && qadsc != this)
						qadsc->AddTreeNode(pdetn, ihvoOld, qrsp);
					qadsc = dynamic_cast<AfDeSplitChild *>(qacw->GetPane(1));
					if (qadsc && qadsc != this)
						qadsc->AddTreeNode(pdetn, ihvoOld, qrsp);
				}
			}
		}

		// Find the location to add the new object.
		idfe = EndOfNodes(hvoOwner, flidOwner, qrsp);
		m_vdfe.Insert(idfe, pdetn);
		pdetn->SetExpansion(kdtsExpanded);
		ClsLevel clev;
		clev.m_clsid = clsidNew;
		clev.m_nLevel = 0;
		AddFields(hvoNew, clev, qcvd, idfe + 1, nIndent + 1);
		// Force height calculations on new editors.
		SetHeight();
		ResetNodeHeaders(); // Adjust all node headers.
		::InvalidateRect(m_hwnd, NULL, false);

		// Find first editable field.
		int idfeEdit;
		int idfeLim = LastFieldAtSameIndent(idfe) + 1;
		for (idfeEdit = idfe; idfeEdit < idfeLim; ++idfeEdit)
		{
			if (m_vdfe[idfeEdit]->IsEditable())
				break;
		}
		if (idfeEdit == idfeLim)
				return true; // There weren't any editable fields.

		// Get the top offset of the editable field.
		int dypMin = Max((int)kdypBoxHeight, m_dypTreeFont);
		int dypTop = 0;
		for (int idfeT = 0; idfeT < idfeEdit; ++idfeT)
			dypTop += Max(dypMin, m_vdfe[idfeT]->GetHeight());

		// Activate the editor
		Rect rc;
		GetClientRect(rc);
		Rect rcEdit(m_dxpTreeWidth, dypTop, rc.right, dypTop + m_vdfe[idfeEdit]->GetHeight() - 1);
		if (m_vdfe[idfeEdit]->BeginEdit(m_hwnd, rcEdit))
		{
			m_pdfe = m_vdfe[idfeEdit];
			::SetFocus(m_pdfe->Hwnd());
			EditorResizing(); // Force it to scroll into view.
			return true;
		}
		else
			return false;
	}
	catch (...)
	{
		// TODO ???(RandyR): Do something else here.
		Assert(false);
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Make the specified field the active editor, and scroll it into view, if needed.
	@param idfe Index to the field we want to activate.
	@param fAlwaysVisible If true, show field regardless of pbsp->m_eVisibility
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::ActivateField(int idfe)
{
	Assert(idfe < m_vdfe.Size());
	Assert(!m_pdfe); // Previous editor should have been closed prior to calling this.

	SetHeight(); // Set height on all fields.
	AfDeFieldEditor * pdfe = m_vdfe[idfe];
	int dypFieldTop = 0;
	int idfeLim = idfe + 1;
	int idfeT;
	for (idfeT = 0; idfeT < idfeLim; ++idfeT)
		dypFieldTop += m_vdfe[idfeT]->GetHeight();
	// Account for scrolling.
	SCROLLINFO si = {isizeof(si), SIF_PAGE | SIF_POS | SIF_RANGE};
	GetScrollInfo(SB_VERT, &si);
	dypFieldTop -= si.nPos;
	Rect rc;
	GetClientRect(rc);
	// The -1 is for the bottom field separator line.
	int dypFieldBottom = dypFieldTop + pdfe->GetHeight();
	if (dypFieldBottom > rc.bottom)
	{
		si.nPos += dypFieldBottom + 1 - rc.bottom; // Scroll data up.
		SetScrollInfo(SB_VERT, &si, true);
	}
	else if (dypFieldTop < rc.top)
	{
		si.nPos += dypFieldTop - pdfe->GetHeight(); // Scroll data down.
		SetScrollInfo(SB_VERT, &si, true);
	}
	if (pdfe->IsEditable())
	{
		// Open the field for editing.
		CreateActiveEditorWindow(pdfe, dypFieldTop);
		::SetFocus(m_pdfe->Hwnd());
	}
	::InvalidateRect(m_hwnd, NULL, true);
}


/*----------------------------------------------------------------------------------------------
	Set the state for an expanded menu item.
	cms.GetExpMenuItemIndex() returns the index of the item to set the state for.
	To get the menu handle and the old ID of the dummy item that was replaced, call
	AfMenuMgr::GetLastExpMenuInfo.

	@param cms menu command state

	@return true
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::CmsExpContextMenu(CmdState & cms)
{
	int iitem;
	iitem = cms.GetExpMenuItemIndex();

	switch (cms.Cid())
	{
	case kcidExpShow:
/*
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
		AssertPtr(prmw);
		m_qvwbrs->GetSelection(prmw->GetViewbarListIndex(kvbltFilter), sisel);
*/
		break;
	}

	cms.Enable(true);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Returns the RecordSpec and optional FldSpec for the given field.
	@param idfe Index of field of interest.
	@param ppfsp Pointer to receive FldSpec associated with the field of interest.
		This is ignored if it is NULL.
	@return The RecordSpec holding the BlockSpec associated with the field of interest.
----------------------------------------------------------------------------------------------*/
RecordSpec * AfDeSplitChild::GetRecordSpec(int idfe, FldSpec ** ppfsp)
{
	Assert(idfe < m_vdfe.Size());
	AfDeFieldEditor * pdfe = m_vdfe[idfe]; // The field of interest.
	AssertPtr(pdfe);

	// Get the record spec associated with the field.
	CustViewDaPtr qcvd;
	m_qlpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	ClsLevel clevOwn;
	qcvd->get_ObjClid(pdfe->GetOwner(), &clevOwn.m_clsid);
	// If we are indented, see if there is a level 1 RecordSpec, otherwise look for a level 0.
	clevOwn.m_nLevel = pdfe->GetIndent() > 0 ? 1 : 0;
	RecordSpecPtr qrsp;
	m_quvs->m_hmclevrsp.Retrieve(clevOwn, qrsp);
	if (!qrsp && clevOwn.m_nLevel == 1)
	{
		clevOwn.m_nLevel = 0;
		m_quvs->m_hmclevrsp.Retrieve(clevOwn, qrsp);
	}
	AssertPtr(qrsp);
	if (ppfsp)
	{
		FldSpec * pfsp = pdfe->GetFldSpec();
#if DEBUG
		// Make sure we have the right RecordSpec
		int ibsp;
		for (ibsp = qrsp->m_vqbsp.Size(); --ibsp >= 0; )
		{
			if (qrsp->m_vqbsp[ibsp] == pfsp)
				break;
		}
		Assert(ibsp >= 0);
#endif
		*ppfsp = pfsp;
	}
	return qrsp;
}


/*----------------------------------------------------------------------------------------------
	Add the field editors for a given object at the location and indent specified.
	This is called at the top level to display an entire record, or it may be called
	when we expand a tree item to display the fields for the expanded node.
	@param hvoRoot Id of the root object that holds the fields we want to display.
	@param clev Class and level of the root object.
	@param pcvd Pointer to the CustViewDa specifying what fields to display.
	@param idfe Index where the new fields are to be inserted.
	@param nInd Indent of the fields to be added.
	@return Index to the next available field after fields have been added.
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::AddFields(HVO hvoRoot, ClsLevel & clev, CustViewDa * pcvd, int idfe, int nInd)
{
	AssertPtr(pcvd);

	// Add some records--based on info in block spec.
	RecordSpecPtr qrsp;
	m_quvs->m_hmclevrsp.Retrieve(clev, qrsp);
	int cbsp = qrsp->m_vqbsp.Size();
	for (int ibsp = 0; ibsp < cbsp; ++ibsp)
	{
		BlockSpecPtr qbsp =  qrsp->m_vqbsp[ibsp];
		// If the user never wants to see this field, skip it.
		if (qbsp->m_eVisibility == kFTVisNever)
			continue;
		AddField(hvoRoot, clev.m_clsid, clev.m_nLevel, qbsp, pcvd, idfe, nInd);
	}
	return idfe;
}


/*----------------------------------------------------------------------------------------------
	Add a field editor for a given field at the location and indent specified.

	@param hvoRoot Id of the root object that holds the fields we want to display.
	@param clid Class of the root object.
	@param nLev Level (main/sub) of the root object in the window.
	@param pfsp The FldSpec that defines this field.
	@param pcvd Pointer to the CustViewDa specifying what fields to display.
	@param idfe Index where the new fields are to be inserted. On return it contains
		an index to the field following any inserted fields.
	@param nInd Indent of the fields to be added.
	@param fAlwaysVisible If true, show field regardless of pbsp->m_eVisibility
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::AddField(HVO hvoRoot, int clid, int nLev, FldSpec * pfsp, CustViewDa * pcvd,
	int & idfe, int nInd, bool fAlwaysVisible)
{
	AssertPtr(pcvd);
	AssertPtr(pfsp);

	AfDeFeSt * padfs;
	AfDeFeCliRef * pdecr;
	AfDeFeComboBox * pdecb;
	ITsStringPtr qtss;
	bool fCheckMissingData = fAlwaysVisible ? false : pfsp->m_eVisibility == kFTVisIfData;
	AfLpInfo * plpi = pcvd->GetLpInfo();
	AssertPtr(plpi);
	AfStatusBarPtr qstbr = MainWindow()->GetStatusBarWnd();
	Assert(qstbr);

	switch (pfsp->m_ft)
	{
	default:
		// Fall through.
	case kftGroup:	// Should not get groups in DE spec.
		// Fall through.
	case kftEnum:	// This must be handled in a subclass.
		// Fall through.
	case kftExpandable:	// Fall through.
	case kftSubItems:	// This must be handled in a subclass.
		Assert(false);
		break;

	case kftObjOwnSeq:	// Fall through.
	case kftObjOwnCol:
		{
			int flid = pfsp->m_flid;
			AfDeFeVectorNode * pdevn = NewObj AfDeFeVectorNode;
			pdevn->Initialize(hvoRoot, flid, nInd, pfsp->m_qtssLabel,
				pfsp->m_qtssHelp, this, pfsp);
			pdevn->Init();
			//pdevn->SetClsid(clsid);
			pdevn->SetTreeObj(hvoRoot);
			pdevn->SetExpansion(pfsp->m_fExpand ? kdtsExpanded : kdtsCollapsed);
			SetTreeHeader(pdevn);
			m_vdfe.Insert(idfe++, pdevn);
			// Automatically expand the node if the option is set.
			/*
			if (pfsp->m_fExpand)
			{
				ClsLevel clev(clsid, 0);
				idfe = AddFields(hvoSub, clev, pcvd, idfe, nInd + 1);
			}
			*/
			break;
		}
	case kftRefAtomic:
		{
			HVO hvoRef;
			CheckHr(pcvd->get_ObjectProp(hvoRoot, pfsp->m_flid, &hvoRef));
			if (fCheckMissingData && !hvoRef)
				return;
			pdecr = NewObj AfDeFeCliRef;
			pdecr->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel,
				pfsp->m_qtssHelp, this, pfsp);
			pdecr->SetItem(hvoRef);
			Assert(pfsp->m_hvoPssl);
			pdecr->SetList(pfsp->m_hvoPssl);
			pdecr->InitContents(pfsp->m_fHier, pfsp->m_pnt);
			m_vdfe.Insert(idfe++, pdecr);
		}
		break;

	case kftRefCombo:
		{
			HVO hvoRef;
			CheckHr(pcvd->get_ObjectProp(hvoRoot, pfsp->m_flid, &hvoRef));
			if (fCheckMissingData && !hvoRef)
				return;
			pdecb = NewObj AfDeFeComboBox();
			pdecb->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel,
				pfsp->m_qtssHelp, this, pfsp);
			pdecb->Init(pfsp->m_pnt);
			Assert(pfsp->m_hvoPssl);
			PossListInfoPtr qpli;
			plpi->LoadPossList(pfsp->m_hvoPssl, pfsp->m_ws, &qpli);
			if (qpli)
			{
				pdecb->SetPssl(pfsp->m_hvoPssl);
				if (hvoRef)
				{
					// If not null, select the appropriate item.
					int itss = qpli->GetIndexFromId(hvoRef);
					pdecb->SetIndex(itss);
				}
			}
			m_vdfe.Insert(idfe++, pdecb);
		}
		break;

	case kftString:
		{
			ITsStringPtr qtss;
			CheckHr(pcvd->get_StringProp(hvoRoot, pfsp->m_flid, &qtss));
			int cch;
			CheckHr(qtss->get_Length(&cch));

			if (fCheckMissingData && !cch)
				return;
			// If the string is empty and has no writing system, change to our default.
			int encT, nVar;
			ITsTextPropsPtr qttp;
			CheckHr(qtss->get_Properties(0, &qttp));
			CheckHr(qttp->GetIntPropValues(ktptWs, &nVar, &encT));
			if (encT == 0 || encT == -1)
			{
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				CheckHr(qtsf->MakeStringRgch(L"", 0, plpi->ActualWs(pfsp->m_ws), &qtss));
				pcvd->CacheStringProp(hvoRoot, pfsp->m_flid, qtss);
			}
			AfDeFeStringPtr qdes = NewObj AfDeFeString;
			qdes->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel,
				pfsp->m_qtssHelp, this, pfsp);
			qdes->Init();
			m_vdfe.Insert(idfe++, qdes);
		}
		break;

	case kftRefSeq:
		{
			if (fCheckMissingData)
			{
				int chvo;
				CheckHr(pcvd->get_VecSize(hvoRoot, pfsp->m_flid, &chvo));
				if (!chvo)
					return;
			}
			Vector<HVO> vpssl;
			bool fMultiList = false;
			Assert(pfsp->m_hvoPssl);
			vpssl.Push(pfsp->m_hvoPssl);
			AfDeFeTags * pdft;
			pdft = NewObj AfDeFeTags;
			pdft->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel,
				pfsp->m_qtssHelp, this, pfsp);
			pdft->Init(vpssl, fMultiList, pfsp->m_fHier, pfsp->m_pnt);
			m_vdfe.Insert(idfe++, pdft);
		}
		break;

	case kftMsa:	// Fall through.
	case kftMta:
		{
			Vector<int> vwsT;
			Vector<int> & vws = vwsT;
			switch (pfsp->m_ws)
			{
			case kwsAnals:
				vws = plpi->AnalWss();
				break;
			case kwsVerns:
				vws = plpi->VernWss();
				break;
			case kwsAnal:
				vwsT.Push(plpi->AnalWs());
				break;
			case kwsVern:
				vwsT.Push(plpi->VernWs());
				break;
			case kwsAnalVerns:
				vws = plpi->AnalVernWss();
				break;
			case kwsVernAnals:
				vws = plpi->VernAnalWss();
				break;
			default:
				vwsT.Push(pfsp->m_ws);
				Assert(pfsp->m_ws); // We should always have an writing system.
				break;
			}

			if (fCheckMissingData)
			{
				ITsStringPtr qtss;
				int iws;
				for (iws = vws.Size(); --iws >= 0; )
				{
					CheckHr(pcvd->get_MultiStringAlt(hvoRoot, pfsp->m_flid, vws[iws],
						&qtss));
					int cch;
					CheckHr(qtss->get_Length(&cch));
					if (cch)
						break;
				}
				if (iws < 0)
					return; // All encodings are empty.
			}

			// An extra ref count is created here which is eventually assigned to the vector.
			AfDeFeStringPtr qdfs = NewObj AfDeFeString;
			qdfs->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel, pfsp->m_qtssHelp,
				this, pfsp);

			if (pfsp->m_ft == kftMta)
				qdfs->Init(&vws, ktptSemiEditable);
			else
				qdfs->Init(&vws, ktptIsEditable);

			m_vdfe.Insert(idfe++, qdfs);
		}
		break;

	case kftStText:
		if (fCheckMissingData)
		{
			HVO hvoText;
			CheckHr(pcvd->get_ObjectProp(hvoRoot, pfsp->m_flid, &hvoText));
			if (!hvoText)
				return;
			int chvo;
			CheckHr(pcvd->get_VecSize(hvoText, kflidStText_Paragraphs, &chvo));
			if (!chvo)
				return;
			if (chvo == 1)
			{
				// If only one paragraph, count as empty if no text.
				HVO hvoPara;
				CheckHr(pcvd->get_VecItem(hvoText, kflidStText_Paragraphs, 0, &hvoPara));
				ITsStringPtr qtss;
				CheckHr(pcvd->get_StringProp(hvoPara, kflidStTxtPara_Contents, &qtss));
				// ENHANCE JohnT (version 2) when we have tables, may need a further check here.
				int cch;
				CheckHr(qtss->get_Length(&cch));
				if (!cch)
					return;
			}
		}
		// Structured text. Use specialized field editor.
		padfs = NewObj AfDeFeSt;
		HVO hvoText;
		pcvd->get_ObjectProp(hvoRoot, pfsp->m_flid, &hvoText);
		padfs->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel, pfsp->m_qtssHelp,
			this, pfsp);
		padfs->Init(pcvd, hvoText);
		m_vdfe.Insert(idfe++, padfs);
		break;

	case kftDateRO:
		{
			if (fCheckMissingData)
			{
				int64 nTime;
				CheckHr(pcvd->get_TimeProp(hvoRoot, pfsp->m_flid, &nTime));
				if (!nTime)
					return;
			}
			AfDeFeTimePtr qdeti = NewObj AfDeFeTime;
			qdeti->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel,
				pfsp->m_qtssHelp, this, pfsp);
			qdeti->Init();
			m_vdfe.Insert(idfe++, qdeti);
			break;
		}

	case kftGenDate:
		{
			int gdat;
			CheckHr(pcvd->get_IntProp(hvoRoot, pfsp->m_flid, &gdat));
			if (fCheckMissingData && !gdat)
				return;
			AfDeFeGenDatePtr qdegd = NewObj AfDeFeGenDate;
			qdegd->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel, pfsp->m_qtssHelp,
				this, pfsp);
			qdegd->InitContents(gdat);
			m_vdfe.Insert(idfe++, qdegd);
			break;
		}

	case kftUnicode:
		{
			SmartBstr sbstr;
			CheckHr(pcvd->get_UnicodeProp(hvoRoot, pfsp->m_flid, &sbstr));
			if (fCheckMissingData && !sbstr.Length())
				return;
			AfDeFeUniPtr qdeu = NewObj AfDeFeUni;
			qdeu->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel, pfsp->m_qtssHelp,
				this, pfsp);
			qdeu->Init();
			m_vdfe.Insert(idfe++, qdeu);
			break;
		}

	case kftInteger:
		{
			int nVal;
			CheckHr(pcvd->get_IntProp(hvoRoot, pfsp->m_flid, &nVal));
			// There is always data, so we don't check for lack of data.
			AfDeFeIntPtr qdei = NewObj AfDeFeInt;
			qdei->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel, pfsp->m_qtssHelp,
				this, pfsp);
			qdei->Init();
			m_vdfe.Insert(idfe++, qdei);
			break;
		}

	case kftObjRefAtomic:	// Fall through.
	case kftObjRefSeq:
		{
			bool fMultiRefs;
			if (pfsp->m_ft == kftObjRefAtomic)
			{
				HVO hvoRef;
				CheckHr(pcvd->get_ObjectProp(hvoRoot, pfsp->m_flid, &hvoRef));
				fMultiRefs = false;
				if (fCheckMissingData && !hvoRef)
					return;
			}
			else
			{
				fMultiRefs = true;
				if (fCheckMissingData) // otherwise no need to check
				{
					int chvo;
					CheckHr(pcvd->get_VecSize(hvoRoot, pfsp->m_flid, &chvo));
					if (!chvo)
						return;
				}
			}
			AfDeFeRefs * pdfr = NewObj AfDeFeRefs;
			// You will normally need to handle this in a subclass in order to
			// give the custom view constructor.
			ObjVc * povc = NewObj ObjVc;
			pdfr->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel,
				pfsp->m_qtssHelp, this, pfsp);
			pdfr->Init(povc, fMultiRefs);
			povc->Release(); // The AfDeFeRefs class holds the single reference count.
			m_vdfe.Insert(idfe++, pdfr);
		}
		break;

	}
	qstbr->StepProgressBar(10);
	return;
}


/*----------------------------------------------------------------------------------------------
	Notify all field editors of the stylesheet change.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::OnStylesheetChange()
{
	bool fHeightChanged = false;
	for (int i = 0; i < m_vdfe.Size(); i++)
	{
		if (m_vdfe[i])
		{
			int dypHeightOld = m_vdfe[i]->GetHeight();
			m_vdfe[i]->OnStylesheetChange();
			fHeightChanged |= (m_vdfe[i]->GetHeight() != dypHeightOld);
		}
	}
	if (fHeightChanged)
		SetHeight();
	::InvalidateRect(m_hwnd, NULL, false); // Need this to get some field editors refreshed.
}


/*----------------------------------------------------------------------------------------------
	Check all fields (lazily) and validate they have specified data. If not, notify the user
	and get their response.
	@return True if everything is valid. False if something is wrong and the user aborted.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::HasRequiredData()
{
	int cdfe = m_vdfe.Size();
	bool skip = false;
	for (int idfe = 0; idfe < cdfe; idfe++)
	{
		if (m_vdfe[idfe])
		{
			FldReq fr = m_vdfe[idfe]->HasRequiredData();
			if (fr == kFTReqNotReq)
			{
				// Field has adequate data or doesn't need any.
				continue;
			}

			// Get the name of the field.
			ITsStringPtr qtss;
			m_vdfe[idfe]->GetLabel(&qtss);
			SmartBstr sbstr;
			CheckHr(qtss->get_Text(&sbstr));
			StrApp strLabel(sbstr.Chars());
			if (fr == kFTReqReq)
			{
				// Field has required data that is missing.
				StrApp strFmt(kstidRequiredMsg);
				StrApp strTitle(kstidRequiredTitle);
				StrApp strMsg;
				strMsg.Format(strFmt.Chars(), strLabel.Chars());

				StrApp strHelpUrl(AfApp::Papp()->GetHelpFile());
				strHelpUrl.Append(
					_T("::/Beginning_Tasks/Entering_Data/Missing_Required_Data.htm"));
				AfMainWndPtr qafwTop = MainWindow();		// AfApp::Papp()->GetCurMainWnd();
				qafwTop->SetFullHelpUrl(strHelpUrl.Chars());

				::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(),
					MB_OK | MB_ICONEXCLAMATION | MB_HELP);

				qafwTop->ClearFullHelpUrl();

				CloseEditor();
				ActivateField(idfe);
				return false;
			}
			else if (fr == (FldReq)AfDeFieldEditor::kFTReqReqHidden)
			{
				StrApp strFmt(kstidRequiredHiddenMsg);
				StrApp strTitle(kstidRequiredHiddenTitle);
				StrApp strMsg;
				strMsg.Format(strFmt.Chars(), strLabel.Chars());

				StrApp strHelpUrl(AfApp::Papp()->GetHelpFile());
				strHelpUrl.Append(_T("::/EffectsOfChangingADefaultWriti.htm"));
				AfMainWndPtr qafwTop = MainWindow();		// AfApp::Papp()->GetCurMainWnd();
				qafwTop->SetFullHelpUrl(strHelpUrl.Chars());

				::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(),
					MB_OK | MB_ICONEXCLAMATION | MB_HELP);

				qafwTop->ClearFullHelpUrl();
				CloseEditor();
				ActivateField(idfe);
				return false;
			}
			if (!skip)
			{
				// Field is encouraged to have data but it is missing.
				StrApp strMsg;
				StrApp strTitle;
				const achar * pszHelpUrl = NULL;
				if (fr == kFTReqWs)
				{
					strTitle.Load(kstidEncouragedTitle);
					StrApp strFmt(kstidEncouragedMsg);
					strMsg.Format(strFmt.Chars(), strLabel.Chars());
					pszHelpUrl = _T("Beginning_Tasks/Entering_Data/Missing_Data.htm");
				}
				else if (fr == (FldReq)AfDeFieldEditor::kFTReqWsHidden)
				{
					strTitle.Load(kstidEncouragedHiddenTitle);
					StrApp strFmt(kstidEncouragedHiddenMsg);
					strMsg.Format(strFmt.Chars(), strLabel.Chars());
					pszHelpUrl = _T("EffectsOfChangingADefaultWriti.htm");
				}
				if (strTitle.Length())
				{
					MssngDtPtr qmd;
					qmd.Create();
					qmd->SetDialogValues(strMsg, strTitle);
					if (pszHelpUrl)
						qmd->SetHelpUrl(pszHelpUrl);
					qmd->DoModal(m_hwnd);
					HVO hvoButton = qmd->GetButtonHvo();
					if (hvoButton == 0)
					{
						CloseEditor();
						ActivateField(idfe);
						return false;
					}
					else if (hvoButton == 2)
					{
						// Don't bother asking about other "encouraged" fields.
						skip = true;
					}
				}
			}
		}
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Called when the frame window is gaining or losing activation.
	@param fActivating true if gaining activation, false if losing activation.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::OnPreActivate(bool fActivating)
{
	SuperClass::OnPreActivate(fActivating);
/* At one point I tried using this approach, but ran into some problems which may have been
solved with other fixes. At the moment, the actions these actions are handled in OnSetFocus
and OnKillFocus.
	// If we are deactivating the main window, and we have a field editor open, save the data
	// to make sure the undo system doesn't get messed up. It currenly only allows one save
	// point being active at a time, so we can't have two unsaved fields open at once. This
	// was especially a problem with AfDeGenDate.
	if (fActivating)
	{
		OutputDebugString("Activating editor\n");
//		if (m_pdfe)
//			m_pdfe->BeginTempEdit(); // Restore editor undo.
	}
	else
	{
		OutputDebugString("Deactivating editor\n");
//		if (m_pdfe && m_pdfe->IsOkToClose())
//		{
//			m_pdfe->SaveEdit(); // Save any changes
//			m_pdfe->EndTempEdit(); // Disable editor undo.
//		}
	}*/
}


/*----------------------------------------------------------------------------------------------
	Return vectors of HVOs and flids to identify a path from the root to the current field,
	and an index into the characters in that field.
	For example, if the cursor is on the 5th character of the title of subentry 1582, this
	would return:
	vhvoPath: 1579, 1581, 1582
	vflidPath: 4004009, 4004009, 4004001
	return = 4
	1579 is the main record. 1581 is a subentry in the 4004009 flid of 1579. 1582 is a
	subentry in the 4004009 flid of 1581. 4004001 is the Title field of 1582, and 4 indicates
	an index into the title of 4 (before the 5th character).
	@param vhvo Vector of object ids
	@param vflid Vector of field ids
	@return cursor index in the final field
----------------------------------------------------------------------------------------------*/
int AfDeSplitChild::GetLocation(Vector<HVO> & vhvo, Vector<int> & vflid)
{
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Make sure we keep the current pane up to date.
----------------------------------------------------------------------------------------------*/
void AfDeSplitChild::SwitchFocusHere()
{
	if (m_qsplf->CurrentPane() != this)
	{
		dynamic_cast<AfDeSplitChild *>(m_qsplf->CurrentPane())->CloseEditor();
		m_qsplf->SetCurrentPane(this);
	}
}


/*----------------------------------------------------------------------------------------------
	Synchronize all clients and dialogs with any changes made in the database.
	@param sync -> The information describing a given change.
----------------------------------------------------------------------------------------------*/
bool AfDeSplitChild::Synchronize(SyncInfo & sync)
{
	switch (sync.msg)
	{
	case ksyncDelPss:
		// When an pss is deleted, it will change the timestamp in the database. In order
		// to maintain the cached timestamp, we need to update it. We don't know which ones
		// are being changed, so we'll just update the timestamp for all major objects. This
		// is not a guaranteed solution. If a person has an unexpanded sub-subentry that has
		// a reference deleted, a person could still get a record changed message. However, to
		// avoid that we would need to reload the entire record, which takes longer and
		// involves quite a bit more code. It would be simple to call SetRootObj, but then we
		// would close all expanded fields. For now we'll take this faster approach.
		{
			Set<HVO> shvo;
			CustViewDaPtr qcvd;
			m_qlpi->GetDataAccess(&qcvd);
			AssertPtr(qcvd);

			for (int idfe = m_vdfe.Size(); --idfe >= 0; )
			{
				// We only want to load the timestamp once for each object.
				AfDeFieldEditor * pdfe = m_vdfe[idfe];
				if (!pdfe)
					continue;
				HVO hvo = pdfe->GetObj();
				if (shvo.IsMember(hvo))
					continue;
				shvo.Insert(hvo);
				CheckHr(qcvd->CacheCurrTimeStamp(hvo));
			}
			return true;
		}

	default:
		break;
	}
	return true;
}



//:>********************************************************************************************
//:> AfLazyDeWnd methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/

AfLazyDeWnd::AfLazyDeWnd(bool fAlignFieldsToTree) :
	AfDeSplitChild(fAlignFieldsToTree)
{
}

/*----------------------------------------------------------------------------------------------
	Get the editor at the specified index. If there isn't one, create it now.
----------------------------------------------------------------------------------------------*/
AfDeFieldEditor * AfLazyDeWnd::FieldAt(int idfe)
{
	if (!m_vdfe[idfe])
	{
		MakeEditorAt(idfe);
		Rect rc;
		::GetClientRect(m_hwnd, &rc);

		if (rc.right != 0)
		{
			int dypMin = DefaultFieldHeight();
			int dxpField = rc.right - GetBranchWidth(idfe); // Width of field data.
			int dyp = m_vdfe[idfe]->SetHeightAt(dxpField);
			if (dyp > dypMin)
				SetHeight();
		}
	}
	return m_vdfe[idfe];
}


//:>********************************************************************************************
//:> AfDeRecSplitChild methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/

AfDeRecSplitChild::AfDeRecSplitChild(bool fAlignFieldsToTree) :
	AfDeSplitChild(fAlignFieldsToTree)
{
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeRecSplitChild::~AfDeRecSplitChild()
{
}


/*----------------------------------------------------------------------------------------------
	Set clsid to the class of the item or subitem in which the cursor is located.
	Sets nLevel to 0 for item, and 1 for subitem.

	@param pclsid [out] Class ID (or -1 if there are no records)
	@param pnLevel [out] 0 for main item in window, or 1 for subitem in window.
----------------------------------------------------------------------------------------------*/
void AfDeRecSplitChild::GetCurClsLevel(int * pclsid, int * pnLevel)
{
	AssertPtr(pclsid);
	AssertPtr(pnLevel);

	// Find the subitem that we are part of, if there is one.
	int idfe = GetCurNodeIndex(m_pdfe);
	while (idfe >= 0)
	{
		AfDeFeTreeNode * pdetn = dynamic_cast<AfDeFeTreeNode *>(m_vdfe[idfe]);
		if (pdetn && IsSubitemFlid(pdetn->GetOwnerFlid()))
		{
			*pclsid = pdetn->GetClsid();
			*pnLevel = 1;
			return;
		}
		idfe = GetCurNodeIndex(pdetn);
	}

	// We have a top level entry which may or may not be an actual subentry.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(prmw);
	HvoClsidVec & vhcRecords = prmw->Records();
	int ihvoCurr = prmw->CurRecIndex();
	// Get the clsid from the current record.
	*pclsid = vhcRecords.Size() ? vhcRecords[ihvoCurr].clsid : -1;
	*pnLevel = 0;
}


/*----------------------------------------------------------------------------------------------
	Move a record to the desired location.
	@param hvo Object id of record to move.
	@param clid Class id of record to move.
	@param idfe Index to the target field in m_vdfe.
	@param drp Indicates target location relative to idfe.
----------------------------------------------------------------------------------------------*/
void AfDeRecSplitChild::MoveRecord(HVO hvo, int clid, int idfe, int drp)
{
	Assert(hvo);
	Assert(clid);
	Assert(idfe < m_vdfe.Size());
	Assert((uint)drp < (uint)kdrpLim);
	Assert(!m_pdfe); // Any active editors should have been closed first.

	IFwMetaDataCachePtr qmdc;
	m_qlpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
	AssertPtr(qmdc);
	ComBool fValid;
	CustViewDaPtr qcvd;
	m_qlpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);

	// Start undo action (e.g., "Undo Move Subanalysis xxxx...").
	const int kchmax = 25; // Take a maximum of 25 characters of drag text.
	SmartBstr sbstr;
	ITsStringPtr qtss;
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(prmw);
	prmw->GetDragText(hvo, clid, &qtss);
	int ilim;
	CheckHr(qtss->get_Length(&ilim));
	ilim = Min(Max(ilim - 1, 0), kchmax);
	CheckHr(qtss->GetChars(0, ilim, &sbstr));
	if (ilim == kchmax)
		sbstr.Append(L"...");
	StrUni stuUndoFmt;
	StrUni stuRedoFmt;
	StrUtil::MakeUndoRedoLabels(kstidUndoMoveX, &stuUndoFmt, &stuRedoFmt);
	StrUni stuUndo;
	StrUni stuRedo;
	stuUndo.Format(stuUndoFmt.Chars(), sbstr.Chars());
	stuRedo.Format(stuRedoFmt.Chars(), sbstr.Chars());
	CheckHr(qcvd->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));

	HVO hvoSrcOwner; // Owner of hvo.
	int flidSrc; // The flid on hvoSrcOwner holding hvo.
	int ihvoSrc; // The index in flidSrc where hvo is located.
	HVO hvoDstOwner; // The target owner for hvo.
	int flidDst; // The target flid on hvoDstOwner.
	int ihvoDst; // The index in flidSrc where hvo is to be placed.

	// Note, hvo may be from another application, so there is no guarantee that anything
	// is in the cache. Get hvoSrcOwner, flidSrc, and ihvoSrc for the source.
	CheckHr(qcvd->get_ObjOwner(hvo, &hvoSrcOwner));
	Assert(hvoSrcOwner);
	CheckHr(qcvd->get_ObjOwnFlid(hvo, &flidSrc));
	Assert(flidSrc);
	int nType;
	CheckHr(qmdc->GetFieldType(flidSrc, &nType));
	if (nType == kcptOwningCollection || nType == kcptOwningSequence)
	{
		int chvo;
		CheckHr(qcvd->get_VecSize(hvoSrcOwner, flidSrc, &chvo));
		// Assume that if the vector has something in it, then it is loaded and current.
		// Otherwise load the vector. Of course, if the vector is already loaded, but
		// another user has added something to it, then this will get the latest, which
		// could mess up the current display which is based on the old contents...
		if (!chvo)
		{
			SmartBstr sbstrName;
			SmartBstr sbstrProp;
			CheckHr(qmdc->GetOwnClsName(flidSrc, &sbstrName));
			CheckHr(qmdc->GetFieldName(flidSrc, &sbstrProp));
			StrUni stuSql;
			IDbColSpecPtr qdcs;
			stuSql.Format(L"select Src, Dst from %s_%s "
				L"where src = %d", sbstrName.Chars(), sbstrProp.Chars(),
				hvoSrcOwner);
			if (nType == kcptOwningSequence)
				stuSql.Append(L" order by ord");
			qdcs.CreateInstance(CLSID_DbColSpec);
			qdcs->Push(koctBaseId, 0, 0, 0);
			qdcs->Push(koctObjVec, 1, flidSrc, 0);
			// Execute the query and store results in the cache.
			CheckHr(qcvd->Load(stuSql.Bstr(), qdcs, hvoSrcOwner, 0, NULL, NULL));
			CheckHr(qcvd->get_VecSize(hvoSrcOwner, flidSrc, &chvo));
		}
		// Get the offset of the source object we want to move.
		qcvd->GetObjIndex(hvoSrcOwner, flidSrc, hvo, &ihvoSrc);
		Assert((uint)ihvoSrc < (uint)chvo); // If item not in vector, something is wrong!
	}
	else
	{
		Assert(nType == kfcptOwningAtom);
		ihvoSrc = 0;
	}

	// Now we need to get hvoDstOwner, flidDst, and ihvoDst for the target.
	AfDeFieldEditor * pdfe = m_vdfe[idfe];
	hvoDstOwner = pdfe->GetOwner();
	ITsStringPtr qtssLabel;
	flidDst = GetDstFlidAndLabel(hvoDstOwner, clid, &qtssLabel);

	// Determine the destination index for the cache (ihvoDst).
	switch (drp)
	{
	case kdrpAbove:
	case kdrpBelow:
		{
			// We shouldn't get here for an AfDeFeVectorNode.
			AfDeFeTreeNode * pdetn = dynamic_cast<AfDeFeTreeNode *>(pdfe);
			AssertPtr(pdetn);
			CheckHr(qcvd->GetObjIndex(hvoDstOwner, flidDst, pdetn->GetTreeObj(), &ihvoDst));
			if (drp == kdrpBelow)
				++ihvoDst;
			break;
		}
	case kdrpOn:
		{
			AfDeFeNode * pden = dynamic_cast<AfDeFeNode *>(pdfe);
			AssertPtr(pden);
			hvoDstOwner = pden->GetTreeObj();
			// The flid for subitems may be different as well (e.g., LexMajorEntry/LexSense).
			flidDst = GetDstFlidAndLabel(hvoDstOwner, clid, &qtssLabel);
			Assert(hvoDstOwner);
			// New item goes to end.
			CheckHr(qcvd->get_VecSize(hvoDstOwner, flidDst, &ihvoDst));
			break;
		}
	case kdrpAtEnd:
		{
			ihvoDst = 0;
			break;
		}
	default:
		Assert(false);
	}

	// Now move the item in the cache.
	CheckHr(qcvd->MoveOwnSeq(hvoSrcOwner, flidSrc, ihvoSrc, ihvoSrc, hvoDstOwner, flidDst,
		ihvoDst));
	CheckHr(qcvd->PropChanged(NULL, kpctNotifyAll, hvoSrcOwner,
		flidSrc, ihvoSrc, 0, 1));
	CheckHr(qcvd->PropChanged(NULL, kpctNotifyAll, hvoDstOwner,
		flidDst, ihvoDst, 1, 0));

	// Now update all DE windows. To do this, we remove the
	// subentry if it is showing, and reinsert it if it is needed. In the process
	// the subentry will be collapsed. This should be acceptable.
	// It is the approach Windows Explorer takes. (In fact it doesn't even
	// maintain expansion in the destination window.) Trying to decide what to do with
	// expansion and then doing it seems unnecessarily complex.

	// Get the field spec associated with the target field.
	FldSpec * pfsp;
	// We can't use the FieldSpec returned from GetRecordSpec because it may not be a tree
	// node (e.g., dragging to the bottom of a record without nodes). Also, if we are
	// moving a sense from a lex entry to a subsense of a sense, the RecordSpec we need is
	// the RecordSpec for the object we are dropping on, not the RecordSpec for the owner
	// of the node.
	RecordSpecPtr qrsp;
	ClsLevel clevOwn;
	qcvd->get_ObjClid(hvoDstOwner, &clevOwn.m_clsid);
	// If we are indented, see if there is a level 1 RecordSpec, otherwise look for a level 0.
	if (drp == kdrpOn)
		clevOwn.m_nLevel = 1;
	else
		clevOwn.m_nLevel = m_vdfe[idfe]->GetIndent() > 0 ? 1 : 0;
	m_quvs->m_hmclevrsp.Retrieve(clevOwn, qrsp);
	if (!qrsp && clevOwn.m_nLevel == 1)
	{
		clevOwn.m_nLevel = 0;
		m_quvs->m_hmclevrsp.Retrieve(clevOwn, qrsp);
	}
	AssertPtr(qrsp);
	int ibsp;
	for (ibsp = qrsp->m_vqbsp.Size(); --ibsp >= 0; )
	{
		BlockSpec * pbsp = qrsp->m_vqbsp[ibsp];
		// At this point we assume that any item in the FldSpec vector will do.
		if (pbsp->m_vqfsp.Size())
			pfsp = pbsp->m_vqfsp[0];
		else
			pfsp = pbsp;
		if (pfsp->m_flid == flidDst)
			break;
	}
	Assert(ibsp >= 0);
	// Create a new field editor template for the moved node.
	AfDeFeTreeNode * pdetn = NewObj AfDeFeTreeNode;
	pdetn->Initialize(hvoDstOwner, flidDst, 0, qtssLabel, pfsp->m_qtssHelp, this, pfsp);
	pdetn->Init();
	pdetn->SetClsid(clid);
	pdetn->SetTreeObj(hvo);
	SetTreeHeader(pdetn);

	// If the operation is in the same vector on the same object, and the source is above
	// the destination, we need to decrement the destination since it will be one less
	// after the deletion.
	if (hvoSrcOwner == hvoDstOwner && flidSrc == flidDst && ihvoSrc < ihvoDst)
		--ihvoDst;

	// Go through all windows and update the views.
	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	int cafw = vqafw.Size();
	for (int iafw = 0; iafw < cafw; iafw++)
	{
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(vqafw[iafw].Ptr());
		AssertPtr(prmw);
		if (prmw->GetLpInfo() == m_qlpi)
		{
			// It's the same language project.
			AfMdiClientWndPtr qmdic = prmw->GetMdiClientWnd();
			AssertPtr(qmdic);
			int ccwnd = qmdic->GetChildCount();
			// Go through all windows.
			for (int icwnd = 0; icwnd < ccwnd; ++icwnd)
			{
				AfSplitterClientWndPtr qacw =
					dynamic_cast<AfSplitterClientWnd *>(qmdic->GetChildFromIndex(icwnd));
				if (!qacw || !qacw->Hwnd())
					continue; // Not an active window.
				// Update both panes for this view.
				AfDeRecSplitChildPtr qarsc = dynamic_cast<AfDeRecSplitChild *>(qacw->GetPane(0));
				if (qarsc)
				{
					if (prmw->GetRootObj() == hvoSrcOwner)
						qarsc->MoveRecordFromMainItems(hvo);
					else
						qarsc->DeleteTreeNode(hvo, 0);
					qarsc->AddTreeNode(pdetn, ihvoDst, qrsp);
					qarsc->ResetNodeHeaders();
					qarsc = dynamic_cast<AfDeRecSplitChild *>(qacw->GetPane(1));
					if (qarsc)
					{
						if (prmw->GetRootObj() == hvoSrcOwner)
							qarsc->MoveRecordFromMainItems(hvo);
						else
							qarsc->DeleteTreeNode(hvo, 0);
						qarsc->AddTreeNode(pdetn, ihvoDst, qrsp);
						qarsc->ResetNodeHeaders();
					}
				}
				else if (prmw->GetRootObj() == hvoSrcOwner)
				{
					// For Doc/Browse windows we need to clean up the main list when we
					// are moving an item from the main list.
					prmw->DeleteMainRecord(hvo, true);
				}
			}
		}
	}
	CheckHr(qcvd->EndUndoTask());
	pdetn->Release();
}


/*----------------------------------------------------------------------------------------------
	Remove hvo from the master list of items, then if we are currently showing this record,
	switch to the next record (or previous if we are at the end). This is called after moving
	a major entry to a subentry, so we just need to clean up after the move. If a filter is
	active, we only update the status and caption bar since a filtered item can still be in the
	master list, and moving the entry shouldn't have affected the filter status.
	@param hvo The id of the item to delete from the master list of records.
----------------------------------------------------------------------------------------------*/
void AfDeRecSplitChild::MoveRecordFromMainItems(HVO hvo)
{
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(prmw);

	HvoClsidVec & vhcRecords = prmw->Records();
	int ihvoCurr = prmw->CurRecIndex();
	bool fDelCurRec = vhcRecords[ihvoCurr].hvo == hvo;

	// Remove the item from the member variables and the cache.
	prmw->DeleteMainRecord(hvo, true);

	// If we have a filter turned on, we only want to update the status and caption bar
	// so that it shows that it has now changed to a subentry. If filters are off and we
	// are currently showing the record we are removing, we need to display another record.
	if (!prmw->IsFilterActive() &&!prmw->IsSortMethodActive() && fDelCurRec)
	{
		AfMdiClientWndPtr qmdic = prmw->GetMdiClientWnd();
		AssertPtr(qmdic);
		AfClientRecWndPtr qafcrw = dynamic_cast<AfClientRecWnd *>(qmdic->GetCurChild());
		qafcrw->DispCurRec(0, 0);
		return; // DispCurRec already takes care of caption and status bar.
	}
	prmw->UpdateCaptionBar();
	prmw->UpdateStatusBar();
}


/*----------------------------------------------------------------------------------------------
	Add hvo to the master list of items. This is called after moving a subentry to a major
	entry.
	@param hvo The id of the item to add to the master list of records.
	@param clid The class of hvo.
----------------------------------------------------------------------------------------------*/
void AfDeRecSplitChild::MoveRecordToMainItems(HVO hvo, int clid)
{
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(prmw);

	// Add the new item to appropriate vectors.
	prmw->AddMainRecord(hvo, clid);

	HvoClsidVec & vhcRecords = prmw->Records();
	int ihvoCurr = prmw->CurRecIndex();
	if (vhcRecords[ihvoCurr].hvo == hvo)
	{
		// We are showing the record we are promoting, so we need to update caption
		// and status bars.
		prmw->UpdateCaptionBar();
		prmw->UpdateStatusBar();
	}
}


/*----------------------------------------------------------------------------------------------
	This handles deleting an object, whether a top level object, or a subitem.

	@param pcmd Menu command info.
	@return true to stop further processing.
----------------------------------------------------------------------------------------------*/
bool AfDeRecSplitChild::CmdDeleteObject(Cmd * pcmd)
{
	// REVIEW KenZ (RandyR): If this works well, then it can be used by all apps.
	// If it does replace the current CmdDelete, then the various subclass maps
	// need to use it, rather than their own.
	// LexEd and Mme already use this method, and its related method 'ConfirmDeletion'.

	AssertObj(pcmd);
	bool fPopupMenu = pcmd->m_rgn[0] == kPopupMenu;

	// Abort if the any editor can't be changed.  Because we don't currently have a way force a
	// close without an assert, and I want to keep the assert there to catch programming errors.
	// We could add a boolean argument to EndEdit to override the assert, but this hardly seems
	// worth it, especially considering above comments. So for now, we force users to produce
	// valid data prior to deleting a record. Also, since updating other windows may cause their
	// editors to close, we also need to check that they are legal.
	RecMainWnd * prmw;
	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	int cafw = vqafw.Size();
	int iafw;
	for (iafw = 0; iafw < cafw; iafw++)
	{
		prmw = dynamic_cast<RecMainWnd *>(vqafw[iafw].Ptr());
		AssertPtr(prmw);
		if (!prmw->IsOkToChange())
		{
			// Bring the bad window to the top.
			::SetForegroundWindow(prmw->Hwnd());
			return false;
		}
	}

	// Get the current record we are working with.
	prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(prmw);
	CustViewDaPtr qcvd = prmw->MainDa();
	HvoClsidVec & vhcRecords = prmw->Records();
	int ihvoCurr = prmw->CurRecIndex();
	// Get the object we are deleting, its owner, and its owning flid.
	HVO hvo = vhcRecords[ihvoCurr].hvo; // The object to delete (default to main object).
	HVO hvoOwn; // Owner of object to delete.
	int flid; // Property of owner that holds object to delete.
	if (fPopupMenu)
	{
		// For popup menu, if we right-clicked on a tree node, use the object of the tree.
		// Otherwise, use the object that has the property on which we right-clicked.
		AfDeFeTreeNode * pdetn = dynamic_cast<AfDeFeTreeNode *>(m_vdfe[m_idfe]);
		hvo = pdetn ? pdetn->GetTreeObj() : m_vdfe[m_idfe]->GetOwner();
	}
	else if (m_pdfe)
	{
		// Otherwise, if we have an active editor, use the object with this field.
		hvo = m_pdfe->GetOwner();
	}
	CheckHr(qcvd->get_ObjOwner(hvo, &hvoOwn));
	CheckHr(qcvd->get_ObjOwnFlid(hvo, &flid));

	bool fAtomic;
	bool fTopLevelObj;
	int kstid;
	bool fHasRefs;
	// Subclasses should override ConfirmDeletion to handle confirmation.
	// If they don't, then the one on this class returns 'false',
	// and we quit.
	if (!ConfirmDeletion(flid, fAtomic, fTopLevelObj, kstid, fHasRefs))
		return true;

	// Update the data cache
	int ihvo;
	int chvo;
	if (fAtomic)
		ihvo = -2;
	else
	{
		CheckHr(qcvd->get_VecSize(hvoOwn, flid, &chvo));
		if (chvo)
		{
			// Find the index to the item we are deleting.
			for (ihvo = 0; ihvo < chvo; ++ihvo)
			{
				HVO hvoT;
				CheckHr(qcvd->get_VecItem(hvoOwn, flid, ihvo, &hvoT));
				if (hvoT == hvo)
					break;
			}
		}
		else
			ihvo = -1;	// Signal that the owner's properties are not cached, just this one.
	}
	// Get class of owner.
	int clid;
	CheckHr(qcvd->get_IntProp(hvoOwn, kflidCmObject_Class, &clid));
	// Delete the object, remove it from the vector, and notify that the prop changed.
	// flidOwnMod should be '0' (zero), if owning class has no modified date property.
	int flidOwnMod = GetOwnerModifiedFlid(clid);
	Assert(flidOwnMod >= 0);
	prmw->DeleteUndoableObject(hvoOwn, flid, flidOwnMod, hvo, kstid, ihvo, fHasRefs);

	// Delete the (sub)record from the vector of visible records.
	// Let every top-level window that is showing the same object know about the change.
	AfLpInfo * plpi = prmw->GetLpInfo();
	for (iafw = 0; iafw < cafw; iafw++)
	{
		RecMainWnd * prmwLoop = dynamic_cast<RecMainWnd *>(vqafw[iafw].Ptr());
		AssertPtr(prmwLoop);
		if (prmwLoop->GetLpInfo() == plpi)
		{
			if (fTopLevelObj)
				prmwLoop->OnDeleteRecord(flid, hvo);
			else
				prmwLoop->OnDeleteSubitem(flid, hvo);
		}
	}
// ENHANCE: RandyR: Generalize this and enable it.
// [Note: It can't be done, until it can get app-specific RnFilterNoMatchDlgPtr.]
/*
	if (!prmw->RecordCount())
	{
		// We just deleted the last item: check whether we have a filter turned on.
		AfViewBarShellPtr qvwbrs = prmw->GetViewBarShell();
		// We need to check for qvwbrs here. If a person deletes the last record, the
		// OnDeleteRecord call above pops up a dialog allowing the user to add a new entry
		// or exit. If they hit exit, qvwbrs is cleared prior to finishing out this method.
		if (!qvwbrs)
			return true;
		Set<int> siselFilter;
		qvwbrs->GetSelection(prmw->GetViewbarListIndex(kvbltFilter), siselFilter);
		int iflt = 0;
		if (siselFilter.Size())
			iflt = *siselFilter.Begin();
		if (iflt)
		{
			--iflt;							// adjust for first filter being "No filter"
			prmw->UpdateStatusBar();		// This turns the filter status pane on.
			StrUni stuNoMatch;
			stuNoMatch.Load(kstidStBar_NoFilterMatch);
			AfStatusBarPtr qstbr = prmw->GetStatusBarWnd();
			Assert(qstbr);
			qstbr->SetRecordInfo(NULL, stuNoMatch.Chars(), NULL);
			qstbr->SetLocationStatus(0, 0);

			// There are no longer any records that match the filter, so let the user
			// make a choice as to what they want to do.

			RnFilterNoMatchDlgPtr qfltnm;
			qfltnm.Create();
			qfltnm->SetDialogValues(iflt, prmw);
			int ifltNew = 0;
			if (qfltnm->DoModal(m_hwnd) == kctidOk)
			{
				qfltnm->GetDialogValues(ifltNew);
				// Since the first filter is actually the No Filter, we have to add one
				// to the number retrieved from the dialog.
				ifltNew++;
			}
			if (ifltNew != -1)
			{
				Set<int> sisel;
				sisel.Insert(ifltNew);
				qvwbrs->SetSelection(prmw->GetViewbarListIndex(kvbltFilter), sisel);
			}
		}
		else
		{
			// "No filter" is selected, we've just deleted the last record in the database!
		}
	}
*/
	return true;
}


/*----------------------------------------------------------------------------------------------
	Subclasses need to override this to handle the two 'output' parameters properly.

	@param flid Owning field id for object to be deleted.
	@param fAtomic true if flid is atomic, otherwise false.
	@param fTopLevelObj true, if the object being deleted is a top level objecct in the app.
	@param kstid The resource id used for undo string.
----------------------------------------------------------------------------------------------*/
bool AfDeRecSplitChild::ConfirmDeletion(int flid, bool & fAtomic, bool & fTopLevelObj,
										int & kstid, bool & fHasRefs)
{
	fHasRefs = true;
	fAtomic = false;
	fTopLevelObj = false;
	kstid = 0;
	return false;	// No deletion permitted, if caller checks.
}

/*----------------------------------------------------------------------------------------------
	Respond to the promote popup menu item.

	@param pcmd Menu command.
	@return true to stop further processing.
----------------------------------------------------------------------------------------------*/
bool AfDeRecSplitChild::CmdPromote(Cmd * pcmd)
{
	AssertObj(pcmd);
	bool fPopupMenu = pcmd->m_rgn[0] == kPopupMenu;

	// Abort if editor can't be closed.
	RecMainWnd * prmw;
	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	int cafw = vqafw.Size();
	int iafw;
	for (iafw = 0; iafw < cafw; iafw++)
	{
		prmw = dynamic_cast<RecMainWnd *>(vqafw[iafw].Ptr());
		AssertPtr(prmw);
		if (!prmw->IsOkToChange())
		{
			// Bring the bad window to the top.
			::SetForegroundWindow(prmw->Hwnd());
			return false;
		}
	}

	// Get the current record we are working with.
	prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(prmw);
	CustViewDaPtr qcvd = prmw->MainDa();
	HvoClsidVec & vhcRecords = prmw->Records();
	int ihvoSrcCurr = prmw->CurRecIndex();
	// Get the object we are promoting, its owner, and its owning flidSrc.
	HVO hvoSrc = vhcRecords[ihvoSrcCurr].hvo; // The object to promote (default to main object).
	HVO hvoSrcOwner; // Owner of object to promote.
	int flidSrc; // Property of owner that holds object to promote.
	HVO hvoDstOwner; // Owner of hvoSrcOwner. This will be the new owner of hvoSrc.
	int flidDst; // Property of hvoDstOwner. This will be the new flidSrc holding hvoSrc.
	int ihvoSrc; // Original position of hvoSrc in flidSrc.
	int ihvoDst; // New position of hvoSrc in flidDst.
	// The active field (where mouse was right-clicked, or active editor).
	int idfe = m_idfe; // Default to the index where the user right-clicked.

	if (fPopupMenu)
	{
		// For popup menu, if we right-clicked on a tree node, use the object of the tree.
		// Otherwise, use the object that has the property on which we right-clicked.
		AfDeFeTreeNode * pdetn = dynamic_cast<AfDeFeTreeNode *>(m_vdfe[m_idfe]);
		hvoSrc = pdetn ? pdetn->GetTreeObj() : m_vdfe[m_idfe]->GetOwner();
	}
	else if (m_pdfe)
	{
		// Otherwise, if we have an active editor, use the object with this field.
		hvoSrc = m_pdfe->GetOwner();
		for (idfe = m_vdfe.Size(); --idfe >= 0; )
			if (m_vdfe[idfe] == m_pdfe)
				break;
		Assert(idfe >= 0);
	}
	CheckHr(qcvd->get_ObjOwner(hvoSrc, &hvoSrcOwner));
	Assert(hvoSrcOwner);
	CheckHr(qcvd->get_ObjOwnFlid(hvoSrc, &flidSrc));
	CheckHr(qcvd->get_ObjOwner(hvoSrcOwner, &hvoDstOwner));
	Assert(hvoDstOwner);
	CheckHr(qcvd->get_ObjOwnFlid(hvoSrcOwner, &flidDst));
	if (prmw->GetRootObj() == hvoSrcOwner)
		return true; // We can't promote any higher. Should catch via menu enable.

	// Determine the indexes for the old and new items within their flids.
	int chvoSrc;
	CheckHr(qcvd->get_VecSize(hvoSrcOwner, flidSrc, &chvoSrc));
	for (ihvoSrc = chvoSrc; --ihvoSrc >= 0; )
	{
		HVO hvoSrcT;
		CheckHr(qcvd->get_VecItem(hvoSrcOwner, flidSrc, ihvoSrc, &hvoSrcT));
		if (hvoSrcT == hvoSrc)
			break;
	}
	CheckHr(qcvd->get_VecSize(hvoDstOwner, flidDst, &ihvoDst)); // Always goes to end.

	// Start undo action (e.g., "Undo Promote Subanalysis xxxx...").
	const int kchmax = 25; // Take a maximum of 25 characters of drag text.
	int clid;
	CheckHr(qcvd->get_ObjClid(hvoSrc, &clid));
	SmartBstr sbstr;
	ITsStringPtr qtss;
	prmw->GetDragText(hvoSrc, clid, &qtss);
	int ilim;
	CheckHr(qtss->get_Length(&ilim));
	ilim = Min(Max(ilim - 1, 0), kchmax);
	CheckHr(qtss->GetChars(0, ilim, &sbstr));
	if (ilim == kchmax)
		sbstr.Append(L"...");
	StrUni stuUndoFmt;
	StrUni stuRedoFmt;
	StrUtil::MakeUndoRedoLabels(kstidUndoPromoteX, &stuUndoFmt, &stuRedoFmt);
	StrUni stuUndo;
	StrUni stuRedo;
	stuUndo.Format(stuUndoFmt.Chars(), sbstr.Chars());
	stuRedo.Format(stuRedoFmt.Chars(), sbstr.Chars());
	CheckHr(qcvd->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));

	// If filters or sorting are active, this could be very confusing for the program to
	// figure out how to find the promoted entry. The promoted entry may end up being
	// excluded from the filter, or the subentry which owns it may be excluded from the
	// filter, there may be multiple copies of the promoted entry (one looking like a
	// major entry and others nested in one or more other entries, depending on the
	// subentry depth), etc. Even if the program could figure out what to do, it would
	// likely be quite confusing to users. So to avoid this complexity, whenever an entry
	// is promoted, we first notify the user that we are turning off filters and sorting,
	// then we go ahead with the promote.
	if (prmw->IsFilterActive() || prmw->IsSortMethodActive())
	{
		StrApp strText(kstidPromoteWarning);
		StrApp strCaption(kstidPromoteCaption);
		::MessageBox(m_hwnd, strText.Chars(), strCaption.Chars(), MB_OK | MB_ICONWARNING);
		prmw->DisableFilterAndSort();
	}

	// Now move the item in the cache.
	CheckHr(qcvd->MoveOwnSeq(hvoSrcOwner, flidSrc, ihvoSrc, ihvoSrc, hvoDstOwner, flidDst,
		ihvoDst));
	// Save the new owner in the cache.
	qcvd->CacheObjProp(hvoSrc, kflidCmObject_Owner, hvoDstOwner);
	CheckHr(qcvd->EndUndoTask());

	// Close active editors. Otherwise we clobber the results of PromoteSetup.
	prmw->CloseActiveEditors();
	// Set the startup path for this window to show the promoted entry.
	PromoteSetup(hvoSrc);
	// Now update everything, including reloading the cache.
	SyncInfo sync(ksyncPromoteEntry, prmw->GetRootObj(), 0);
	m_qlpi->StoreAndSync(sync);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Enable/disable the promote menu.
	@param cms menu command state.
	@return true.
----------------------------------------------------------------------------------------------*/
bool AfDeRecSplitChild::CmsPromote(CmdState & cms)
{
	// If we ever implement Promote on the main menu, we'll need to add smarts here to detect
	// the difference.
	bool fPopupMenu = true;

	// Get the current record we are working with.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	if (!prmw)
		return true; // Apparently this is a valid state in WLC. Don't go any further.
	CustViewDaPtr qcvd = prmw->MainDa();
	HvoClsidVec & vhcRecords = prmw->Records();
	if (!vhcRecords.Size())
		return true; // Don't go any further on an empty database.

	int ihvoSrcCurr = prmw->CurRecIndex();
	// Get the object we are promoting, its owner, and its owning flidSrc.
	HVO hvoSrc = vhcRecords[ihvoSrcCurr].hvo; // The object to delete (default to main object).
	HVO hvoSrcOwner; // Owner of object to delete.

	if (fPopupMenu)
	{
		// For popup menu, if we right-clicked on a tree node, use the object of the tree.
		// Otherwise, use the object that has the property on which we right-clicked.
		AfDeFeTreeNode * pdetn = dynamic_cast<AfDeFeTreeNode *>(m_vdfe[m_idfe]);
		hvoSrc = pdetn ? pdetn->GetTreeObj() : m_vdfe[m_idfe]->GetOwner();
	}
	else if (m_pdfe)
	{
		// Otherwise, if we have an active editor, use the object with this field.
		hvoSrc = m_pdfe->GetOwner();
	}
	CheckHr(qcvd->get_ObjOwner(hvoSrc, &hvoSrcOwner));
	// We can only promote if the owner of the source is not the top object.
	cms.Enable(prmw->GetRootObj() != hvoSrcOwner);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Display the indicated item. Load the data from the database if it has not been loaded yet.

	If the RecMainWnd has specific path information, we want to expand to that location.

	@param hcRoot HVO of item to be displayed.
	@param fNeedToReadData True if we have already loaded the data from the database.
----------------------------------------------------------------------------------------------*/
void AfDeRecSplitChild::SetRootObj(HvoClsid & hcRoot, bool fNeedToReadData)
{
	// Get the main window we are part of. If we can't find it, we have not yet been attached
	// to a real hwnd, so need not do anything. Another SetRootObj call will occur when
	// we get attached.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	if (!prmw)
		return;

	// Get the database connection.
	IOleDbEncapPtr qode;
	AfLpInfo * plpi = dynamic_cast<AfLpInfo *>(prmw->GetLpInfo());
	AssertPtr(plpi);
	plpi->GetDbInfo()->GetDbAccess(&qode);
	if (!qode)
		// Review JohnT: should we return E_FAIL or S_FALSE?
		return;	// No current session, can't display anything.

	// ENHANCE JohnT: Save if anything has changed? (Or ask the user?)
	// Should we keep track of the current object? But we may be called from PostAttach
	// without changing the object, yet need to build the data structures. Currently,
	// DispCurrRec takes care of not calling this if nothing changed.

	// Get the data about the current record into the DA object.
	HvoClsidVec vhcT;
	vhcT.Push(hcRoot);

	// Get the main custom view DA shared by the project.
	CustViewDaPtr qcvd = prmw->MainDa();
	AssertPtr(qcvd);
/*		This asserts in CLE. My (RandyR) question is, why is it being tested
		ahead of loading? Rn had it in its version.
	HVO hvoOwn;
	qcvd->get_ObjectProp(hcRoot.hvo, kflidCmObject_Owner, &hvoOwn);
	Assert(hvoOwn);
*/
	int nLevel = 0;
	ClsLevel clev(hcRoot.clsid, nLevel);

	AfStatusBarPtr qstbr = prmw->GetStatusBarWnd();
	Assert(qstbr);
	bool fProgBar = qstbr->IsProgressBarActive();
	if (!fProgBar)
	{
		StrApp strMsg(kstidStBar_LoadingData);
		qstbr->StartProgressBar(strMsg.Chars(), 0, 70, 1);
	}
	qstbr->StepProgressBar();

	// Read the data from the database if we haven't already loaded it.
	if (fNeedToReadData)
	{
		RecordSpecPtr qrsp;
		m_quvs->m_hmclevrsp.Retrieve(clev, qrsp);
		AssertPtr(qrsp);

		TagFlids tf;
		tf.clid = hcRoot.clsid;
		prmw->GetTagFlids(tf);
		qcvd->SetTags(tf.flidRoot, tf.flidCreated);
		qcvd->LoadData(vhcT, m_quvs, qstbr, tf.fRecurse);
		LoadOtherData(qode, qcvd, hcRoot);
	}

	CloseAllEditors();

	qstbr->StepProgressBar();

	AddFields(hcRoot.hvo, clev, qcvd, 0, 0);

	// We need to check this because FullRefresh, etc. may call this when it isn't the
	// the current client.
	AfClientRecDeWnd * pcrde = dynamic_cast<AfClientRecDeWnd *>(Parent()->Parent());
	// If we need to get to a particular field, try to do that.
	if (prmw->GetHvoPath().Size() && prmw->GetHvoPath()[0] == hcRoot.hvo)
	{
		// GetAncestor allows this to work when a list chooser is open.
		// We should be able to open an independent editor in each window, but without the
		// following test, we end up with multiple cursors blinking at the same time. So until
		// that can be solved, we only open an editor on the active window.
		if (prmw->Hwnd() == ::GetAncestor(::GetActiveWindow(), GA_ROOTOWNER) &&
			prmw->GetMdiClientWnd()->GetCurChild() == pcrde && m_qsplf->CurrentPane() == this)
		{
			OpenPath(prmw->GetHvoPath(), prmw->GetFlidPath(), prmw->GetCursorIndex());
			if (!fProgBar)
				qstbr->EndProgressBar();
			return;
		}
	}

	SCROLLINFO si = {isizeof(si), SIF_POS};
	GetScrollInfo(SB_VERT, &si);
	si.nPos = 0;
	SetScrollInfo(SB_VERT, &si, false);

	qstbr->StepProgressBar();

	SetHeight();
	// Open an editor if this is the currently active pane of the current main window.
	if (prmw->Hwnd() == GetActiveWindow() && prmw->GetMdiClientWnd()->GetCurChild() == pcrde &&
		m_qsplf->CurrentPane() == this)
	{
		OpenNextEditor();
	}
	::InvalidateRect(m_hwnd, NULL, false);

	qstbr->StepProgressBar();
	if (!fProgBar)
		qstbr->EndProgressBar();
}


/*----------------------------------------------------------------------------------------------
	Enable various menu items.
----------------------------------------------------------------------------------------------*/
bool AfDeRecSplitChild::CmsHaveRecord(CmdState & cms)
{
	RecMainWnd * prmw = dynamic_cast<RecMainWnd*>(MainWindow());
	Assert(prmw);
	return prmw->CmsHaveRecord(cms);
}


/*----------------------------------------------------------------------------------------------
	Enable various menu items.
----------------------------------------------------------------------------------------------*/
bool AfDeRecSplitChild::CmsInsertSubentry(CmdState & cms)
{
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow()); // from CmdDelete
	AssertPtr(prmw);

	AfMdiClientWndPtr qmdic = prmw->GetMdiClientWnd();
	AssertPtr(qmdic);

	AfClientWnd * pafcw = dynamic_cast<AfClientWnd *>(qmdic->GetCurChild());
	AssertPtr(pafcw);

	int iview = qmdic->GetChildIndexFromWid(pafcw->GetWindowId());

	UserViewSpecVec & vuvs = prmw->GetLpInfo()->GetDbInfo()->GetUserViewSpecs();

	if ((kvwtDoc == vuvs[iview]->m_vwt) || (kvwtBrowse == vuvs[iview]->m_vwt))
	{
		cms.Enable(false);
		return true;
	}

	return CmsHaveRecord(cms);
}
