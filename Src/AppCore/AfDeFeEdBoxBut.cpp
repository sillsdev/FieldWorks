/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeEdBoxBut.cpp
Responsibility: Ken Zook
Last reviewed: never

Description:
	This class provides a data entry editor that consists of an edit box and a list selection
	button.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

const int kdxpButtonHeight = 15;

/* Not used now, but save as example.
HHOOK AfDeFeEdBoxBut::DeEdit::s_hhk;
Rect AfDeFeEdBoxBut::DeEdit::s_rc;
AfDeFieldEditor * AfDeFeEdBoxBut::DeEdit::s_pdfe; */

BEGIN_CMD_MAP(AfDeFeEdBoxBut::DeEdit)
	ON_CID_CHILD(kcidEditCut, &AfDeFeEdBoxBut::DeEdit::CmdEdit,
		&AfDeFeEdBoxBut::DeEdit::CmsEditUpdate)
	ON_CID_CHILD(kcidEditCopy, &AfDeFeEdBoxBut::DeEdit::CmdEdit,
		&AfDeFeEdBoxBut::DeEdit::CmsEditUpdate)
	ON_CID_CHILD(kcidEditPaste, &AfDeFeEdBoxBut::DeEdit::CmdEdit,
		&AfDeFeEdBoxBut::DeEdit::CmsEditUpdate)
	ON_CID_CHILD(kcidEditDel, &AfDeFeEdBoxBut::DeEdit::CmdEdit,
		&AfDeFeEdBoxBut::DeEdit::CmsEditUpdate)
	ON_CID_CHILD(kcidEditSelAll, &AfDeFeEdBoxBut::DeEdit::CmdEdit,
		&AfDeFeEdBoxBut::DeEdit::CmsEditUpdate)
	ON_CID_CHILD(kcidEditUndo, &AfDeFeEdBoxBut::DeEdit::CmdEditUndo,
		&AfDeFeEdBoxBut::DeEdit::CmsEditUndo)
	ON_CID_CHILD(kcidEditRedo, &AfDeFeEdBoxBut::DeEdit::CmdEditRedo,
		&AfDeFeEdBoxBut::DeEdit::CmsEditRedo)

	// Since we want to disable formatting commands, we need to have our own command map in
	// order to override and disable the buttons/comboboxes.
	ON_CID_CHILD(kcidFmtFnt, &AfVwWnd::CmdFmtFnt, &AfDeFeEdBoxBut::DeEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmtStyles, &AfVwWnd::CmdFmtStyles, &AfDeFeEdBoxBut::DeEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbStyle, &AfVwWnd::CmdCharFmt, &AfDeFeEdBoxBut::DeEdit::CmsCharFmt)
	ON_CID_CHILD(kcidApplyNormalStyle, &AfVwWnd::CmdApplyNormalStyle, NULL)
	ON_CID_CHILD(kcidFmttbWrtgSys, &AfVwWnd::CmdCharFmt, &AfDeFeEdBoxBut::DeEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFnt, &AfVwWnd::CmdCharFmt, &AfDeFeEdBoxBut::DeEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFntSize, &AfVwWnd::CmdCharFmt, &AfDeFeEdBoxBut::DeEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbBold, &AfVwWnd::CmdCharFmt, &AfDeFeEdBoxBut::DeEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbItal, &AfVwWnd::CmdCharFmt, &AfDeFeEdBoxBut::DeEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyBgrndColor, &AfVwWnd::CmdCharFmt,
		&AfDeFeEdBoxBut::DeEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyFgrndColor, &AfVwWnd::CmdCharFmt,
		&AfDeFeEdBoxBut::DeEdit::CmsCharFmt)
END_CMD_MAP_NIL()

const int kwidEditChild = 1; // An identifier for an edit box.


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDeFeEdBoxBut::AfDeFeEdBoxBut()
	: AfDeFieldEditor()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeFeEdBoxBut::~AfDeFeEdBoxBut()
{
}


//:>********************************************************************************************
//:>	AfDeFeEdBoxBut methods.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Initialize font after superclass initialization is done.
----------------------------------------------------------------------------------------------*/
void AfDeFeEdBoxBut::Init()
{
	Assert(m_hvoObj); // Initialize should have been called first.

	CreateFont();
	ILgWritingSystemFactoryPtr qwsf;
	IWritingSystemPtr qws;
	GetLpInfo()->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);
	ComBool fRTL = FALSE;
	CheckHr(qwsf->get_EngineOrNull(m_ws, &qws));
	if (qws)
		CheckHr(qws->get_RightToLeft(&fRTL));
	m_fRtl = bool(fRTL);
}


/*----------------------------------------------------------------------------------------------
Deletes the selected text in this this control.
----------------------------------------------------------------------------------------------*/
void AfDeFeEdBoxBut::DeleteSelectedText()
{
//	m_fDelFromDialog = true;
//	::SendMessage(m_hwnd, WM_CUT, 0, 0);
}


/*----------------------------------------------------------------------------------------------
Returns true if there is selected text in this this control.
@return True if there is text selected.
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::IsTextSelected()
{
	int ichAnchor = 0;
	int ichEnd = 0;
	::SendMessage(m_hwnd, EM_GETSEL, (WPARAM)&ichAnchor, (LPARAM)&ichEnd);
	if (ichEnd - ichAnchor)
		return true;
	return false;
}


/*----------------------------------------------------------------------------------------------
	Draw to the given clip rectangle.
	@param hdc
	@param rcpClip the given clip rectangle

	TODO: use views code to display the field contents!
----------------------------------------------------------------------------------------------*/
void AfDeFeEdBoxBut::Draw(HDC hdc, const Rect & rcpClip)
{
	Assert(hdc);
	int ws;
	SmartBstr sbstr;
	if (m_qtss)
	{
		CheckHr(m_qtss->get_Text(&sbstr));
		int nVar;
		ITsTextPropsPtr qttp;
		CheckHr(m_qtss->get_PropertiesAt(0, &qttp));
		CheckHr(qttp->GetIntPropValues(ktptWs, &nVar, &ws));
		MakeCharProps(ws);
		CreateFont();
}

	AfGfx::FillSolidRect(hdc, rcpClip, m_chrp.clrBack);
	COLORREF clrBgOld = AfGfx::SetBkColor(hdc, m_chrp.clrBack);
	COLORREF clrFgOld = AfGfx::SetTextColor(hdc, m_chrp.clrFore);
	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, m_hfont);
#if 1
	UINT uAlignPrev = ::GetTextAlign(hdc);
	if (m_fRtl)
	{
		::SetTextAlign(hdc, (uAlignPrev & ~(TA_CENTER|TA_LEFT)) | TA_RIGHT | TA_RTLREADING);
		::TextOutW(hdc, rcpClip.right - 19, rcpClip.top + 1, sbstr.Chars(), sbstr.Length());
	}
	else
	{
		::SetTextAlign(hdc, (uAlignPrev & ~(TA_CENTER|TA_RIGHT)) | TA_LEFT);
		::TextOutW(hdc, rcpClip.left + 2, rcpClip.top + 1, sbstr.Chars(), sbstr.Length());
	}
	::SetTextAlign(hdc, uAlignPrev);
#else
	::TextOutW(hdc, rcpClip.left + 2, rcpClip.top + 1, sbstr.Chars(), sbstr.Length());
#endif
	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	AfGfx::SetBkColor(hdc, clrBgOld);
	AfGfx::SetTextColor(hdc, clrFgOld);
}


/*----------------------------------------------------------------------------------------------
	Make an edit box to allow editing. hwnd is the parent hwnd. rc is the size of the child
	window. Store the hwnd and return true.
	@param hwnd
	@param rc
	@param dxpCursor
	@param fTopCursor
	@param tpte
	@return true if successful

	TODO: To handle extra long field contents (and RTL?) the button cannot be a child of the
	edit control, because they must occupy seperate rectangles inside the field proper.
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::BeginEdit(HWND hwnd, Rect &rc, int dxpCursor, bool fTopCursor,
	TptEditable tpte)
{
	if (!SuperClass::BeginEdit(hwnd, rc, dxpCursor, fTopCursor))
		return false;
	DeEditPtr qde;
	qde.Create();
	qde->SetEditable(tpte);
	qde->m_pdee = this;
	IActionHandler * pacth = BeginTempEdit();
	ILgWritingSystemFactoryPtr qwsf;
	GetLpInfo()->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);
#if 1
	int nRet = 0;
	ComBool fRTL = FALSE;
	IWritingSystemPtr qws;
	CheckHr(qwsf->get_EngineOrNull(m_ws, &qws));
	if (qws)
		CheckHr(qws->get_RightToLeft(&fRTL));
	if (fRTL)
		qde->Create(hwnd, kwidEditChild, WS_CHILD | ES_RIGHT | ES_AUTOHSCROLL, NULL, m_qtss,
			qwsf, m_ws, pacth);
	else
#endif
	qde->Create(hwnd, kwidEditChild, WS_CHILD | ES_LEFT | ES_AUTOHSCROLL, NULL, m_qtss,
		qwsf, m_ws, pacth);
	m_hwnd = qde->Hwnd();
	Rect rcT(rc.left + 2, rc.top + 1, rc.right, rc.bottom);
	nRet = ::MoveWindow(m_hwnd, rcT.left, rcT.top, rcT.Width(), rcT.Height(), true);
#if 99-99
	Rect rcParentClient;
	Rect rcParent;
	::GetClientRect(hwnd, &rcParentClient);
	::GetWindowRect(hwnd, &rcParent);
	Rect rcEdit;
	Rect rcEditClient;
	::GetWindowRect(m_hwnd, &rcEdit);
	::GetClientRect(m_hwnd, &rcEditClient);
#endif
	Rect rcTb;
	::GetClientRect(m_hwnd, &rcTb);
	rcTb.left = rcTb.right - 16;
	rcTb.bottom = rcTb.top + Min((int)rcTb.bottom - (int)rcTb.top, (int)kdxpButtonHeight);

	WndCreateStruct wcsButton;
	wcsButton.InitChild(_T("BUTTON"), m_hwnd, kwidEditChild);
	wcsButton.style |= WS_VISIBLE | BS_OWNERDRAW;
	wcsButton.SetRect(rcTb);

	DeButtonPtr qdb;
	qdb.Create();
	qdb->CreateAndSubclassHwnd(wcsButton);
	qdb->m_pdee = this;
	m_hwndButton = qdb->Hwnd();

#if 1-1
	// Resize the edit control window to exclude the button
	nRet = ::MoveWindow(m_hwnd, rcT.left, rcT.top, rcT.Width() - 18, rcT.Height(), true);
#endif
	// Add text to the window.
	::SendMessage(m_hwnd, FW_EM_SETSTYLE, m_chrp.clrBack, (LPARAM)&m_qfsp->m_stuSty);
	::SendMessage(m_hwnd, EM_SETMARGINS, EC_RIGHTMARGIN | EC_LEFTMARGIN, MAKELPARAM(0, 18));
	::ShowWindow(m_hwnd, SW_SHOW);
	//::SendMessage(m_hwnd, WM_SETFONT, (WPARAM)::GetStockObject(DEFAULT_GUI_FONT), 0);
	// Foreground/background colors are set via WM_CTLCOLOREDIT in AfDeFeWnd.
	// Set cursor to desired offset.
	//int ich;
	//ich = LOWORD(::SendMessage(m_hwnd, EM_CHARFROMPOS, 0, dxpCursor));
	// For some reason the above always comes back with -1 instead of the correct index.
	// Is this a bug in TssEdit or am I doing something wrong?
	//::SendMessage(m_hwnd, EM_SETSEL, ich, ich);
	//::mouse_event(MOUSEEVENTF_LEFTDOWN,0,0,0,0); // Send LButton to place cursor in edit ctrl.
#if 99-99
	Rect rcEditNew;
	Rect rcEditNewClient;
	Rect rcBut;
	Rect rcButClient;
	::GetWindowRect(m_hwnd, &rcEditNew);
	::GetClientRect(m_hwnd, &rcEditNewClient);
	::GetWindowRect(m_hwndButton, &rcBut);
	::GetClientRect(m_hwndButton, &rcButClient);
#endif

	return true;
}

/*
LONGEST NAME ALLOWED:
This is a list item that has an exceedingly long and generally meaningless name in order to test how the code would hand
*/

/*----------------------------------------------------------------------------------------------
	Close the current editor, saving changes that were made. hwnd is the editor hwnd.
	@param fForce True if we want to force the editor closed without making any
		validity checks or saving any changes.
----------------------------------------------------------------------------------------------*/
void AfDeFeEdBoxBut::EndEdit(bool fForce)
{
	SuperClass::EndEdit(fForce);
	if (!fForce)
	{
		if (!SaveEdit())
		{
			Assert(false); // Should have called IsOkToClose() first.
		}
		EndTempEdit();
		SaveFullCursorInfo();
	}
	// Get rid of the edit box and button.
	::DestroyWindow(m_hwnd);
	m_hwnd = 0;
	m_hwndButton = 0;
}


/*----------------------------------------------------------------------------------------------
	Move/resize the edit and button windows.
	@param rcClip
----------------------------------------------------------------------------------------------*/
void AfDeFeEdBoxBut::MoveWnd(const Rect & rcClip)
{
	::MoveWindow(m_hwnd, rcClip.left + 2, rcClip.top + 1,
		rcClip.Width() - 2, rcClip.Height() - 1, true);
	Rect rc;
	::GetClientRect(m_hwnd, &rc);
	rc.left = rc.right - 16;
	rc.bottom = rc.top + Min((int)rc.bottom - (int)rc.top, (int)kdxpButtonHeight);
	::MoveWindow(m_hwndButton, rc.left, rc.top, rc.Width(), rc.Height(), true);

#if 1-1
	// Now resize the edit window so it doesn't actually include the button, just for laughs.
	::MoveWindow(m_hwnd, rcClip.left + 2, rcClip.top + 1,
		rcClip.Width() - 20, rcClip.Height() - 1,	// width - 2 (for margin) - 18 (for button)
		true);
#endif
#if 99-99
	Rect rcEditNew;
	Rect rcEditNewClient;
	Rect rcBut;
	Rect rcButClient;
	::GetWindowRect(m_hwnd, &rcEditNew);
	::GetClientRect(m_hwnd, &rcEditNewClient);
	::GetWindowRect(m_hwndButton, &rcBut);
	::GetClientRect(m_hwndButton, &rcButClient);
#endif
}


/*----------------------------------------------------------------------------------------------
	Set the height for the specified width, and return it.
	@param dxpWidth
	@return height
----------------------------------------------------------------------------------------------*/
int AfDeFeEdBoxBut::SetHeightAt(int dxpWidth)
{
	Assert(dxpWidth > 0);
	if (dxpWidth != m_dxpWidth)
	{
		m_dxpWidth = dxpWidth;
		// The height is set when the field is initialized and doesn't change here.
	}
	return m_dypHeight;
}


/*----------------------------------------------------------------------------------------------
	This method saves the current cursor information in RecMainWnd. Normally it just
	stores the cursor index in RecMainWnd::m_ichCur. For structured texts, however,
	it also inserts the appropriate hvos and flids for the StText classes in
	m_vhvoPath and m_vflidPath. Other editors may need to do other things.
----------------------------------------------------------------------------------------------*/
void AfDeFeEdBoxBut::SaveCursorInfo()
{
	// Store the current record/subrecord and field info.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (!prmw)
		return;
	// On BeginEdit we come in here before we have an edit box, so return 0.
	int ichAnchor = 0;
	int ichEnd = 0;
	if (m_hwnd)
		::SendMessage(m_hwnd, EM_GETSEL, (WPARAM)&ichAnchor, (LPARAM)&ichEnd);
	prmw->SetCursorIndex(Min(ichAnchor, ichEnd));
}


/*----------------------------------------------------------------------------------------------
	This attempts to place the cursor as defined in RecMainWnd m_vhvoPath, m_vflidPath,
	and m_ichCur.
	@param vhvo Vector of ids inside the field.
	@param vflid Vector of flids inside the field.
	@param ichCur Character offset in the final field for the cursor.
----------------------------------------------------------------------------------------------*/
void AfDeFeEdBoxBut::RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur)
{
	// Store the current record/subrecord and field info.
	::SendMessage(m_hwnd, EM_SETSEL, ichCur, ichCur);
}


//:>********************************************************************************************
//:>	DeEdit methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Process window messages for edit box. Return true if processed.
	@param wm
	@param wp
	@param lp
	@param lnRet
	@return
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeEdit::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_SYSKEYDOWN:
		// Alt+Up and Alt+Down opens chooser.
		if (wp == VK_DOWN || wp == VK_UP)
		{
			m_pdee->ProcessChooser();
			return true;
		}
		break;

	case WM_KEYDOWN:
		{
			m_ch = wp;
			switch (wp)
			{
			case VK_TAB:
				if (::GetKeyState(VK_SHIFT) < 0)
					// Shift Tab to previous editor.
					m_pdee->GetDeWnd()->OpenPreviousEditor(DxpCursorOffset(), true);
				else
					// Tab to next editor.
					m_pdee->GetDeWnd()->OpenNextEditor(DxpCursorOffset());
				return true;

			case VK_LEFT:
			case VK_RIGHT:
				{
					if (OnKeyDown(wp, LOWORD(lp), HIWORD(lp)))
					{
						int ichAnchor;
						int ichEnd;
						GetSel(&ichAnchor, &ichEnd);
						m_cchMatched = Min(ichAnchor, ichEnd);
					}
					else
					{
						if (wp == VK_LEFT)
							m_pdee->GetDeWnd()->OpenPreviousEditor(9999, false);
						else
							m_pdee->GetDeWnd()->OpenNextEditor();
					}
					return true;
				}

			case VK_UP:
				// Up arrow to previous editor.
				m_pdee->GetDeWnd()->OpenPreviousEditor(DxpCursorOffset(), false);
				return true;

			case VK_DOWN:
				// Down arrow to next editor.
				m_pdee->GetDeWnd()->OpenNextEditor(DxpCursorOffset());
				return true;

			case VK_END:
				SuperClass::FWndProc(wm, wp, lp, lnRet);
				m_cchMatched = GetTextLength();
				return true;

			case VK_HOME:
				SuperClass::FWndProc(wm, wp, lp, lnRet);
				m_cchMatched = 0;
				return true;

			case VK_PRIOR:
			case VK_NEXT:
				// Scroll the entire data entry window up or down one page.
				return m_pdee->GetDeWnd()->ScrollKey(wp, lp);

			default:
				break;
			}
			break; // We don't want to fall through on normal typing.
		}

	case WM_LBUTTONUP:
		{
			SuperClass::FWndProc(wm, wp, lp, lnRet);
			int ichAnchor;
			int ichEnd;
			GetSel(&ichAnchor, &ichEnd);
			m_cchMatched = Min(ichAnchor, ichEnd);
			return true;
		}

	default:
		break;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	@return the horizontal pixel offset of the cursor.
----------------------------------------------------------------------------------------------*/
int AfDeFeEdBoxBut::DeEdit::DxpCursorOffset()
{
	Assert(m_qrootb);

	int dxp;
	CheckHr(m_qrootb->get_XdPos(&dxp));
	return dxp;
}


/*----------------------------------------------------------------------------------------------
	The edit box changed. We need to validate what was done.
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeEdit::OnChange()
{
	return m_pdee->OnChange(this);
}


/*----------------------------------------------------------------------------------------------
	Setting focus on the editor.
	@param hwndOld The handle of the window losing focus.
	@param fTbControl ?
	@return superclass' OnSetFocus success state
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeEdit::OnSetFocus(HWND hwndOld, bool fTbControl)
{
	AfApp::Papp()->AddCmdHandler(this, 1, kgrfcmmAll);
/*	Keep this code for now in case we ever want to switch to a MouseProc
	// We may get duplicate OnFocus messages.
	if (!s_hhk)
	{
		// Note: using ModuleEntry::GetModuleHandle(), NULL for last two parameters caused very
		// unstable conditions with screen flashing colors, etc. MSDN says you are supposed to
		// be able to cancel clicks anywhere in the current thread, but in reality, it only
		// blocks clicks in the same window.
		s_hhk = ::SetWindowsHookEx(WH_MOUSE, &AfDeFeEdBoxBut::DeEdit::MouseProc, NULL,
			GetCurrentThreadId());
		Assert(s_hhk);
		::GetWindowRect(m_hwnd, &s_rc);
		s_pdfe = m_pdee;
	}*/
	m_pdee->BeginTempEdit();
	// Using SuperClass below is ambiguous.
	return ScrollSuperClass::OnSetFocus(hwndOld, fTbControl);
}


/*----------------------------------------------------------------------------------------------
	Clearing focus on the editor
	@param hwndNew The handle of the window gaining focus.
	@return superclass' OnKillFocus success state
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeEdit::OnKillFocus(HWND hwndNew)
{
	AfApp::Papp()->RemoveCmdHandler(this, 1);
/*	Keep this code for now in case we ever want to switch to a MouseProc
	if (s_hhk)
	{
		::UnhookWindowsHookEx(s_hhk);
		s_hhk = NULL;
		s_pdfe = NULL;
	}*/
	if (m_pdee->IsOkToClose(false)) // Don't raise an error message here.
		m_pdee->SaveEdit();
	m_pdee->EndTempEdit();
	return SuperClass::OnKillFocus(hwndNew);
}


/*----------------------------------------------------------------------------------------------
	Hook to catch mouse messages to allow us to abort things with an error message.
	We can only have one of these hooks open at a time. So the hook should normally be
	enabled (SetWindowsHookEx) and disabled (UnhookWindowsHookEx) in the OnSetFocus and
	OnKillFocus methods that need this checking.

	We are currently not using this, but may want to try again at some point, so I'm leaving
	it in for now. It did a good job of blocking all actions on the main window. But when
	we had two notebook windows open, even though it was called for the second window, it would
	not block the mouse action from doing something in the second window. As a result, there
	were a few cases where we could still get into trouble.
----------------------------------------------------------------------------------------------
LRESULT CALLBACK AfDeFeEdBoxBut::DeEdit::MouseProc(int nc, WPARAM wp, LPARAM lp)
{
	MOUSEHOOKSTRUCT * pmhs;
	pmhs = reinterpret_cast<MOUSEHOOKSTRUCT *>(lp);
	if (nc == HC_ACTION)
	{
		switch (wp)
		{
		case WM_LBUTTONDOWN:
		case WM_NCLBUTTONDOWN:
		case WM_LBUTTONDBLCLK:
		case WM_NCLBUTTONDBLCLK:
		case WM_RBUTTONDOWN:
		case WM_NCRBUTTONDOWN:
		case WM_RBUTTONDBLCLK:
		case WM_NCRBUTTONDBLCLK:
			{
				//OutputDebugString("Hooked button down\n");
				//StrApp str;
				//str.Format("width: %d %d %d, height: %d %d %d\n", s_rc.left, pmhs->pt.x,
				//	s_rc.right, s_rc.top, pmhs->pt.y, s_rc.bottom);
				//OutputDebugString(str.Chars());
				if (!::PtInRect(&s_rc, pmhs->pt))
				{
					// Raise an error message if it is not OK to close.
					if (!s_pdfe->IsOkToClose())
						// Throw out mouse click. But this only works for clicks in the current
						// frame window. We process clicks here for other windows in the same
						// process, but the clicks are not thrown out. This hook doesn't get
						// called at all for windows in other processes.
						return -1;
				}
			}
			break;

		default:
			break;
		}
	}
	return CallNextHookEx(s_hhk, nc, wp, lp); // Pass message on.
}*/


/*----------------------------------------------------------------------------------------------
	Enable/Disable Edit buttons. Copy, Paste, Select All always enabled. Cut enabled whenever
	there is a selection.
	@param cms
	@return true
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeEdit::CmsEditUpdate(CmdState & cms)
{
	switch (cms.Cid())
	{
	case kcidEditCut:
	case kcidEditCopy:
		{
			int ichAnchor = 0;
			int ichEnd = 0;
			// When we click on another editor, the outgoing editor gets closed before the
			// command handler empties its queue (apparently). In any case, we come in here
			// with m_hwnd == NULL. So we need to catch this here, unless there is a better way.
			if (m_hwnd)
				GetSel(&ichAnchor, &ichEnd);
			cms.Enable(ichAnchor != ichEnd);
		}
		break;

	case kcidEditPaste:
	case kcidEditDel:
	case kcidEditSelAll:
		cms.Enable(true);
		break;

	default:
		Assert(false);
		cms.Enable(false);
		break;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle Edit commands
	@param pcmd
	@return true if successful
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeEdit::CmdEdit(Cmd * pcmd)
{
	AssertObj(pcmd);

	switch (pcmd->m_cid)
	{
	case kcidEditCut:
		m_ch = VK_DELETE;
		Cut();
		break;
	case kcidEditCopy:
		Copy();
		break;
	case kcidEditPaste:
		m_ch = 0; // Special code to indicate paste.
		Paste();
		break;
	case kcidEditDel:
//		::SendMessage(m_hwnd, WM_CLEAR, 0, 0);
		// TODO KenZ: We have a problem here since kcidEditDel is used both here for text
		// deletion as well as in RnDeSplitChild for entry deletions. At this point it hits
		// this one first and if we return true, it never gets to the other one. We need to
		// get smarter and ask the user what should be deleted and then do the appropriate
		// thing.
		return false;
		break;
	case kcidEditSelAll:
		SetSel(0, -1);
		m_cchMatched = 0;
		break;
	default:
		Assert(false); // We shouldn't get here.
		return false;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Enable/Disable Format toolbar buttons (and the format font command).
	All these are disabled in a tags field; there is nothing here we should be editing, except
	pseudo-editing to select something.
	@param cms The command state object.
	@return True indicating it was handled.
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeEdit::CmsCharFmt(CmdState & cms)
{
	if (cms.Cid() == kcidFmtStyles)
		cms.Enable(true);
	else
		cms.Enable(false);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Process commands. Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeEdit::OnCommand(int cid, int nc, HWND hctl)
{
	if (nc == BN_CLICKED)
		m_pdee->ProcessChooser();

	else if (nc == EN_KILLFOCUS)
	{
		int x;
		x = 3;
	}

	return SuperClass::OnCommand(cid, nc, hctl);
}


/*----------------------------------------------------------------------------------------------
	Enable/disable the undo menu item. Because of complexities in undo/redoing these changes,
	for now we disable undo/redo if any typing has been done in this field.
	@param cms menu command state
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeEdit::CmsEditUndo(CmdState & cms)
{
	// At this point we've sent a Mark to the action handler and nasty things happen if we try
	// to undo/redo things. So until this problem can be resolved, we will not allow any
	// undo/redo actions while in this field.
//	if (m_pdee->IsDirty())
//	{
		StrApp staLabel(kstidRedoFieldDisabled);
		cms.SetText(staLabel, staLabel.Length());
		cms.Enable(false);
		return true;
//	}
//	else
//	{
//		RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(MainWindow());
//		Assert(pwnd);
//		return pwnd->CmsEditUndo(cms);
//	}
}


/*----------------------------------------------------------------------------------------------
	Enable/disable the redo menu item. Because of complexities in undo/redoing these changes,
	for now we disable undo/redo if any typing has been done in this field.
	@param cms menu command state
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeEdit::CmsEditRedo(CmdState & cms)
{
	// At this point we've sent a Mark to the action handler and nasty things happen if we try
	// to undo/redo things. So until this problem can be resolved, we will not allow any
	// undo/redo actions while in this field.
//	if (m_pdee->IsDirty())
//	{
		StrApp staLabel(kstidRedoFieldDisabled);
		cms.SetText(staLabel, staLabel.Length());
		cms.Enable(false);
		return true;
//	}
//	else
//	{
//		RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(MainWindow());
//		Assert(pwnd);
//		return pwnd->CmsEditRedo(cms);
//	}
}


/*----------------------------------------------------------------------------------------------
	Handle the undo command by passing it on to the main window.
	@param pcmd menu command
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeEdit::CmdEditUndo(Cmd * pcmd)
{
	m_pdee->EndTempEdit();
	RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(pwnd);
	pwnd->CmdEditUndo(pcmd);
	// Note, due to FullRefresh, this field editor is deleted at this point.
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle the redo command by passing it on to the main window.
	@param pcmd menu command
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeEdit::CmdEditRedo(Cmd * pcmd)
{
	m_pdee->EndTempEdit();
	RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(pwnd);
	pwnd->CmdEditRedo(pcmd);
	// Note, due to FullRefresh, this field editor is deleted at this point.
	return true;
}


//:>********************************************************************************************
//:>	DeButton methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Handle window painting (WM_PAINT).
	@param pdis
	@return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeButton::OnDrawThisItem(DRAWITEMSTRUCT * pdis)
{
	// Note: Using a Rectangle over an exisitng background didn't work on one monitor.
	// Also, using a standard button didn't work properly when it was clicked.
	AssertObj(this);
	AssertPtr(pdis);
	HDC hdc = pdis->hDC;
	// Draw the button.
	Rect rc(pdis->rcItem);
	Rect rcDot;
	Rect rcT;
	if (pdis->itemState & ODS_SELECTED)
	{
		AfGfx::FillSolidRect(hdc, rc, ::GetSysColor(COLOR_3DFACE));
		::DrawEdge(hdc, &rc, EDGE_SUNKEN, BF_RECT);
		rcDot.left = rc.Width() / 2 - 4;
		rcDot.top = rc.bottom - 5;
	}
	else
	{
		AfGfx::FillSolidRect(hdc, rc, ::GetSysColor(COLOR_3DFACE));
		::DrawEdge(hdc, &rc, EDGE_RAISED, BF_RECT);
		rcDot.left = rc.Width() / 2 - 5;
		rcDot.top = rc.bottom - 6;
	}

	// Draw the dots.
	const int kclrText = ::GetSysColor(COLOR_BTNTEXT);
	rcDot.right = rcDot.left + 2;
	rcDot.bottom = rcDot.top + 2;
	AfGfx::FillSolidRect(hdc, rcDot, kclrText);
	rcDot.Offset(4, 0);
	AfGfx::FillSolidRect(hdc, rcDot, kclrText);
	rcDot.Offset(4, 0);
	AfGfx::FillSolidRect(hdc, rcDot, kclrText);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Return the what's-this help string for the Date Dialog ellipsis button.

	@param pt not used.
	@param pptss Address of a pointer to an ITsString COM object for returning the help string.

	@return true.
----------------------------------------------------------------------------------------------*/
bool AfDeFeEdBoxBut::DeButton::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	StrApp str;
	str.Load(kstidEllipsisBtnDateWhatsThisHelp); // No context help available
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(str);
	CheckHr(qtsf->MakeString(stu.Bstr(), m_pdee->UserWs(), pptss));
	return true;
}


/*----------------------------------------------------------------------------------------------
	The field has changed, so make sure it is updated.
----------------------------------------------------------------------------------------------*/
void AfDeFeEdBoxBut::UpdateField()
{
	// Subclasses need to do something useful here.
}


/*----------------------------------------------------------------------------------------------
	Check the requirments of the FldSpec, and verify that data in the field meets the
	requirement.
	@return kFTReqNotReq if the all requirements are met.
			kFTReqWs if data is missing, but it is encouraged.
			kFTReqReq if data is missing, but it is required.
----------------------------------------------------------------------------------------------*/
FldReq AfDeFeEdBoxBut::HasRequiredData()
{
	if (m_qfsp->m_fRequired == kFTReqNotReq)
		return kFTReqNotReq;
	int cch = 0;
	if (m_qtss)
		m_qtss->get_Length(&cch);
	if (!cch)
		return m_qfsp->m_fRequired;
	else
		return kFTReqNotReq;
}


/*----------------------------------------------------------------------------------------------
	Set things up for editing with a temporary data cache by marking the point to return to
	in the undo stack. Also returns the action handler which should be installed in the
	temporary cache. This can be called multiple times. If the mark is already set, it does
	nothing.
	@return
----------------------------------------------------------------------------------------------*/
IActionHandler * AfDeFeEdBoxBut::BeginTempEdit()
{
	// Get your action handler.
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	IActionHandlerPtr qacth;
	CheckHr(qcvd->GetActionHandler(&qacth));
	if (!m_hMark)
		CheckHr(qacth->Mark(&m_hMark));
	return qacth;
}


/*----------------------------------------------------------------------------------------------
	End editing with a temporary data cache by clearing stuff out down to the mark created by
	BeginTempEdit. This can be called any number of times. If a mark is not in progress, it
	does nothing.
	@return
----------------------------------------------------------------------------------------------*/
IActionHandler * AfDeFeEdBoxBut::EndTempEdit()
{
	// Get your action handler.
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	IActionHandlerPtr qacth;
	CheckHr(qcvd->GetActionHandler(&qacth));
	// Clear out any temporary Undo items relating to this window.
	if (m_hMark)
	{
		// Calling DiscardToMark() when the depth is greater than zero crashes.  (See DN-786.)
		int nDepth;
		CheckHr(qacth->get_CurrentDepth(&nDepth));
		if (nDepth == 0)
		{
			CheckHr(qacth->DiscardToMark(0));
			m_hMark = 0;
		}
	}
	return qacth;
}
