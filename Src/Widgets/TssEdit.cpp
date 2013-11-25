/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TssEdit.cpp
Responsibility: Rand Burgett
Last reviewed:

	Implementation of TssEdit.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

const int ktmrResetSearch = 1;

const int knToolTipTimer = 7;

/***********************************************************************************************
	TssEdit methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
TssEdit::TssEdit()
{
	m_dxpMarginLeft = 0;
	m_dxpMarginRight = 0;
	m_dypMarginTop = 0;
	m_dxpScrollOffset = 0;
	m_fVScrollEnabled = m_fHScrollEnabled = false;
	m_clrBack = ::GetSysColor(COLOR_WINDOW); // otherwise it defaults to zero, black.
//	m_clrBack = kclrWhite; // otherwise it defaults to zero, black.
	m_nEditable = ktptSemiEditable; // otherwise you can't edit at all in the edit control!

	m_fInDialog = false;
	m_hwndToolTip = NULL;
}


/*----------------------------------------------------------------------------------------------
	This "subclasses" an existing edit control. It actually destroys the old control and
	creates a new view window. It copies the text, style, position, and z-order of the original
	control.
	@param dwStyleExtra additional styles to AND with those from the dialog resource.
----------------------------------------------------------------------------------------------*/
void TssEdit::SubclassEdit(HWND hwndDlg, int cid, ILgWritingSystemFactory * pwsf, int ws,
	DWORD dwStyleExtra)
{
	AssertPtr(pwsf);
	m_fInDialog = true;
	// Set margins to leave room for sunken border effect, plus one pixel white space.
	// Yet one more pixel on the leading edge keeps the IP clear of the border.
	SIZE sizeMargins = { ::GetSystemMetrics(SM_CXEDGE), ::GetSystemMetrics(SM_CYEDGE) };
	m_dxpMarginLeft = sizeMargins.cx + 2;
	m_dxpMarginRight = sizeMargins.cx + 1;
	m_dypMarginTop = sizeMargins.cy + 1;

	HWND hwndOld = ::GetDlgItem(hwndDlg, cid);

	// Get window coordinates relative to the dialog.
	Rect rc;
	::GetWindowRect(hwndOld, &rc);
	::MapWindowPoints(NULL, hwndDlg, (POINT *)&rc, 2);

	const int kcchMax = 2048;
	achar rgch[kcchMax];
	::GetDlgItemText(hwndDlg, cid, rgch, kcchMax);

	// Get information on old window.
	HWND hwndPrev = ::GetWindow(hwndOld, GW_HWNDPREV);
	DWORD dwStyleEx = ::GetWindowLong(hwndOld, GWL_EXSTYLE);
	DWORD dwStyle = ::GetWindowLong(hwndOld, GWL_STYLE);
	::DestroyWindow(hwndOld);

	// Create the new window and set the styles appropriately.
	Create(hwndDlg, cid, dwStyle, NULL, rgch, pwsf, ws, NULL);
	::SetWindowLong(m_hwnd, GWL_EXSTYLE, dwStyleEx | dwStyleExtra);
	::SetWindowPos(m_hwnd, hwndPrev, rc.left, rc.top, rc.Width(), rc.Height(), 0);
}


/*----------------------------------------------------------------------------------------------
	Create a new TssEdit. psz can be NULL if the control should start out empty.
----------------------------------------------------------------------------------------------*/
void TssEdit::Create(HWND hwndPar, int cid, DWORD dwStyle, HWND hwndToolTip, const achar * psz,
	ILgWritingSystemFactory * pwsf, int ws, IActionHandler * pacth)
{
	AssertPtrN(psz);
	AssertPtr(pwsf);

	ITsStringPtr qtss;
	if (psz)
	{
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		StrUni stu(psz);
		CheckHr(qtsf->MakeString(stu.Bstr(), ws, &qtss));
	}
	Create(hwndPar, cid, dwStyle, hwndToolTip, qtss, pwsf, ws, pacth);
}


/*----------------------------------------------------------------------------------------------
	Create a new TssEdit. ptss can be NULL if the control should start out empty.
----------------------------------------------------------------------------------------------*/
void TssEdit::Create(HWND hwndPar, int cid, DWORD dwStyle, HWND hwndToolTip, ITsString * ptss,
	ILgWritingSystemFactory * pwsf, int ws, IActionHandler * pacth)
{
	AssertPtr(pwsf);
	PreCreate(pwsf, ws, ptss, pacth);

	m_cid = cid;
	m_hwndToolTip = hwndToolTip;

	m_wsBase = ws;
	m_qwsf = pwsf;
	if (!m_wsBase)
		CheckHr(pwsf->get_UserWs(&m_wsBase));	// get the user interface writing system id.

	// Create the window.
	WndCreateStruct wcs;
	wcs.lpszClass = _T("AfVwWnd");
	wcs.hwndParent = hwndPar;
	wcs.SetWid(cid);
	wcs.style = dwStyle;
	CreateHwnd(wcs);

	// Add a tool tip.
	if (HasToolTip())
	{
		// Add the combo information to the tooltip.
		TOOLINFO ti = { isizeof(ti), TTF_IDISHWND };
#ifdef DEBUG
		static StrApp s_str;
		s_str.Format(_T("Missing a tooltip for edit control with ID %d"), m_cid);
		ti.lpszText = const_cast<achar *>(s_str.Chars());
#else // !DEBUG
		ti.lpszText = _T("Dummy text");
#endif // !DEBUG

		ti.hwnd = Hwnd();
		ti.uId = (uint)ti.hwnd;
		::GetClientRect(Hwnd(), &ti.rect);
		::SendMessage(m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);
	}

	PostCreate(ptss);
}


/*----------------------------------------------------------------------------------------------
	Initialize variables prior to creating the control, i.e. create/attach a ISilDataAccess
	and add TsString to cache
	(may be explicitly called when created by ATL, because in this case TssEdit::Create() will
	not be called)
----------------------------------------------------------------------------------------------*/
void TssEdit::PreCreate(ILgWritingSystemFactory * pwsf, int ws, ITsString * ptss,
	IActionHandler * pacth)
{
	AssertPtr(pwsf);
	AssertPtrN(ptss);

	ITsStringPtr qtss = ptss;
	if (!ptss)
	{
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		CheckHr(qtsf->MakeStringRgch(L"", 0, ws, &qtss));
	}

	if (pacth)
	{
		// We want actions in this edit box to be undoable.
		VwUndoDaPtr quda;
		quda.Attach(NewObj VwUndoDa);
		CheckHr(quda->SetActionHandler(pacth));
		m_qcda.Attach(quda.Detach());
	}
	else
	{
		m_qcda.Attach(NewObj VwCacheDa);
	}
	m_qcda->putref_WritingSystemFactory(pwsf);
	m_qcda->CacheStringProp(khvoString, ktagString, qtss);
}

/*----------------------------------------------------------------------------------------------
	Display TsString in the control
	(may be explicitly called when created by ATL, because in this case TssEdit::Create() will
	not be called)
----------------------------------------------------------------------------------------------*/
void TssEdit::PostCreate(ITsString * ptss)
{
	AssertPtrN(ptss);

	if (ptss)
	{
		int cch;
		ITsStringPtr qtss = ptss;
		CheckHr(qtss->get_Length(&cch));
		// Treat as inserting all the characters, since the cache previously had nothing.
		CheckHr(m_qcda->PropChanged(NULL, kpctNotifyAll, khvoString, ktagString, 0, cch, 0));
		OnUpdate();
		::UpdateWindow(m_hwnd);
		OnChange();
	}
}
/*----------------------------------------------------------------------------------------------
	Make the root box.
----------------------------------------------------------------------------------------------*/
void TssEdit::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf, IVwRootBox ** pprootb)
{
	AssertPtr(pvg);
	AssertPtrN(pwsf);
	AssertPtr(pprootb);

	*pprootb = NULL;

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this));
	HVO hvo = khvoString;
	int frag = kfrString;

	// Set up a new view constructor.
	ComBool fRTL = FALSE;
	IWritingSystemPtr qws;
	Assert(pwsf == m_qwsf.Ptr());
	CheckHr(pwsf->get_EngineOrNull(m_wsBase, &qws));
	if (qws)
		CheckHr(qws->get_RightToLeft(&fRTL));
	TssEditVcPtr qtevc;
	qtevc.Attach(NewObj TssEditVc(this, m_nEditable, m_fShowTags, fRTL));

	CheckHr(m_qcda->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(m_qcda));

	AfMainWnd * pafw = MainWindow();
	AssertPtrN(pafw);
	AfStylesheet * pss = NULL;
	AfLpInfo * plpi = pafw->GetLpInfo();
	if (plpi)
	{
		pss = plpi->GetAfStylesheet();
		// This allows it to receive updates to style defns.
		pafw->RegisterRootBox(qrootb);
	}
	IVwViewConstructor * pvvc = qtevc;
	CheckHr(qrootb->SetRootObjects(&hvo, &pvvc, &frag, pss, 1));

	*pprootb = qrootb.Detach();
}


/*----------------------------------------------------------------------------------------------
	This processes Windows messages on the window. In general, it normally calls the
	appropriate method on the edit class.
----------------------------------------------------------------------------------------------*/
bool TssEdit::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	bool fRet;

	switch (wm)
	{
	case WM_GETDLGCODE:
		// This is essential when embedded in a dialog to tell the dialog manager that it
		// wants to get key strokes. (We could try DLGC_WANTALLKEYS but I think we would then
		// get the Tab and Return keys...we may get them anyway with this combination...)
		// The last value tells Windows that when tabbing to this control we should use
		// EM_SETSEL to select all the text.
		lnRet = DLGC_WANTCHARS | DLGC_WANTARROWS | DLGC_HASSETSEL;
		return true;
	case EM_GETLINE:	// Use FW_EM_GETLINE.
	case EM_REPLACESEL:	// Use FW_EM_REPLACESEL.
		// We don't support these methods. Use the replacement TsString versions instead.
		Assert(false);
		lnRet = LB_ERR;
		return true;

	// NOTE: DO NOT send this message to a TssEdit if you want the actual text. Send the
	// FW_EM_GETTEXT message instead. This method is required for TssEdit controls on a
	// dialog because Windows will send the message to the control anytime the user hits a
	// key.
	case WM_GETTEXT:
		{
			ITsStringPtr qtss;
			GetText(&qtss);
			const wchar * pwrgch;
			int cch;
			HRESULT hr;
			IgnoreHr(hr = qtss->LockText(&pwrgch, &cch));
			if (FAILED(hr))
				return true;
			StrApp str(pwrgch, cch);
			qtss->UnlockText(pwrgch);
			lnRet = Min(cch + 1, (int)wp);
			achar * psz = reinterpret_cast<achar *>(lp);
			StrCpyN(psz, str.Chars(), lnRet);
		}
		return true;

	// NOTE: You should be sending an FW_EM_SETTEXT message instead of this.
	case WM_SETTEXT:
		{
			achar * psz = reinterpret_cast<achar *>(lp);
			StrUni stu(psz);
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			ITsStringPtr qtss;
			CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsBase, &qtss));
			SetText(qtss);
		}
		return true;

	case EM_CANUNDO:
	case EM_CHARFROMPOS:
	case EM_EMPTYUNDOBUFFER:
	case EM_FMTLINES:
	case EM_GETFIRSTVISIBLELINE:
	case EM_GETHANDLE:
	case EM_GETMODIFY:
	case EM_GETPASSWORDCHAR:
	case EM_GETRECT:
	case EM_GETTHUMB:
	case EM_GETWORDBREAKPROC:
	case EM_POSFROMCHAR:
	case EM_SETHANDLE:
	case EM_SETMODIFY:
	case EM_SETPASSWORDCHAR:
	case EM_SETRECT:
	case EM_SETRECTNP:
	case EM_SETTABSTOPS:
	case EM_SETWORDBREAKPROC:
	case EM_UNDO:
	case WM_GETFONT:
	case WM_SETFONT:
		// We don't support these methods.
		Assert(false);
		lnRet = LB_ERR;
		return true;

	case EM_GETLIMITTEXT:
		lnRet = GetLimitText();
		return true;

	case FW_EM_GETLINE:
		lnRet = GetLine(wp, (ITsString **)lp);
		return true;

	case EM_GETLINECOUNT:
		lnRet = GetLineCount();
		return true;

	case EM_GETMARGINS:
		lnRet = GetMargins();
		return true;

	case FW_EM_GETSTYLE:
		GetStyle((StrUni *)lp, (COLORREF *)wp);
		return true;

	case EM_GETSEL:
		lnRet = GetSel((int *)wp, (int *)lp);
		return true;

	case EM_LINEFROMCHAR:
		lnRet = LineFromChar(wp);
		return true;

	case EM_LINEINDEX:
		lnRet = LineIndex(wp);
		return true;

	case EM_LINELENGTH:
		lnRet = LineLength(wp);
		return true;

	case EM_LINESCROLL:
		LineScroll(lp, wp);
		return true;

	case FW_EM_REPLACESEL:
		ReplaceSel((ITsString *)lp);
		return true;

	case EM_SCROLL:
		lnRet = ::SendMessage(m_hwnd, WM_VSCROLL, LOWORD(wp), 0);
		return true;

	case EM_SCROLLCARET:
		ScrollCaret();
		return true;

	case EM_SETLIMITTEXT:
		SetLimitText(wp);
		return true;

	case EM_SETMARGINS:
		SetMargins(wp, LOWORD(lp), HIWORD(lp));
		return true;

	case EM_SETREADONLY:
		SetReadOnly(wp);
		return true;

	case EM_SETSEL:
		SetSel(wp, lp);
		return true;

	case FW_EM_SETSTYLE:
		SetStyle((StrUni *)lp, (COLORREF)wp);
		return true;

	case WM_GETTEXTLENGTH:
		lnRet = GetTextLength();
		return true;

	case FW_EM_GETTEXT:
		GetText((ITsString **)lp);
		return true;

	case FW_EM_SETTEXT:
		SetText((ITsString *)lp);
		return true;

	case WM_COPY:
		Copy();
		return true;

	case WM_CUT:
		Cut();
		return true;

	case WM_PASTE:
		Paste();
		return true;

	case WM_HSCROLL:
		if (!OnHScroll(LOWORD(wp), HIWORD(wp), (HWND)lp))
		{
			::SendMessage(::GetParent(m_hwnd), WM_COMMAND,
				MAKEWPARAM(::GetDlgCtrlID(m_hwnd), EN_HSCROLL), (LPARAM)m_hwnd);
		}
		return true;

	case WM_VSCROLL:
		if (!OnVScroll(LOWORD(wp), HIWORD(wp), (HWND)lp))
		{
			::SendMessage(::GetParent(m_hwnd), WM_COMMAND,
				MAKEWPARAM(::GetDlgCtrlID(m_hwnd), EN_VSCROLL), (LPARAM)m_hwnd);
		}
		return true;

	case WM_KILLFOCUS:
		if (!OnKillFocus((HWND)wp))
		{
			::SendMessage(::GetParent(m_hwnd), WM_COMMAND,
				MAKEWPARAM(::GetDlgCtrlID(m_hwnd), EN_KILLFOCUS), (LPARAM)m_hwnd);
		}
		return true;

	case WM_SETFOCUS:
		if (!OnSetFocus((HWND)wp))
		{
			::SendMessage(::GetParent(m_hwnd), WM_COMMAND,
				MAKEWPARAM(::GetDlgCtrlID(m_hwnd), EN_SETFOCUS), (LPARAM)m_hwnd);
		}
		return true;
		// Calling SuperClass here causes two OnSetFocus calls for each OnKillFocus.
		//return SuperClass::FWndProc(wm, wp, lp, lnRet);

	case WM_CHAR:
		fRet = false;
		if (wp == VK_TAB)  // '\t'
		{
			fRet = OnCharTab();
		}
		else if (wp == VK_RETURN) // '\r'
		{
			fRet = OnCharEnter();
		}
		else if (wp == VK_ESCAPE) // '\33'
		{
			fRet = OnCharEscape();
		}
		if (fRet)
			return fRet;
		else
			return SuperClass::FWndProc(wm, wp, lp, lnRet);

	case WM_LBUTTONDOWN:
	case WM_LBUTTONUP:
	case WM_MBUTTONDOWN:
	case WM_MBUTTONUP:
	case WM_RBUTTONDOWN:
	case WM_RBUTTONUP:
	case WM_MOUSEMOVE:
		if (HasToolTip())
		{
			// Notify the tooltip belonging to the parent toolbar of the mouse message.
			Assert(m_hwndToolTip);
			MSG msg;
			msg.hwnd = m_hwnd; // ::GetParent(m_hwnd);
			msg.message = wm;
			msg.wParam = wp;
			msg.lParam = lp;
			::SendMessage(m_hwndToolTip, TTM_RELAYEVENT, 0, (LPARAM)&msg);
		}
		break;

	default:
		break;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Handle notifications.

	@param ctid Identifier of the common control sending the message.
	@param pnmh Pointer to an NMHDR structure containing notification code and additional info.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool TssEdit::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	if (SuperClass::OnNotifyChild(id, pnmh, lnRet))
		return true;

	bool fToolTip = HasToolTip();

	HWND hwndParent = ::GetParent(m_hwnd);

	if (fToolTip && pnmh->code == TTN_POP)
	{
		// Wait 1/2 second after the tooltip disappears before resetting the text on the
		// status bar.
		::SetTimer(hwndParent, knToolTipTimer, 500, NULL);
		return true;
	}
	else if (fToolTip && pnmh->code == TTN_SHOW)
	{
		// This flag keeps the tooltip from recursively appearing and crashing the program.
		static bool s_fIgnore = false;
		if (!s_fIgnore)
		{
			// If another tooltip shows up in the 1/2 second time interval set above, cancel
			// the timer, so the status bar doesn't get changed back to the idle string.
			::KillTimer(hwndParent, knToolTipTimer);

			// Create a new notification message and forward it to the parent in order to get
			// the default response for a normal tooltip (which is currently defined in
			// AfMainWnd::OnNotifyChild).
			NMTTDISPINFO nmtdi;
			nmtdi.hdr.hwndFrom = (HWND)id;
			nmtdi.hdr.code = TTN_GETDISPINFO;
			nmtdi.hdr.idFrom = ::GetDlgCtrlID((HWND)id);
			*nmtdi.szText = 0;
			::SendMessage(::GetParent(m_hwnd), WM_NOTIFY, nmtdi.hdr.idFrom, (LPARAM)&nmtdi);

			// Update the status bar here rather than above after ::KillTimer() so that the
			// string for the new command is already set.
			AfMainWnd * pafw = MainWindow();
			AssertPtr(pafw);
			AfStatusBar * pstat = pafw->GetStatusBarWnd();
			if (pstat)
				pstat->DisplayHelpText();

			if (*nmtdi.szText)
			{
				// Now we have the text for the control, so update the text in the tooltip.
				TOOLINFO ti = { isizeof(ti) };
				ti.hwnd = (HWND)id;
				ti.uId = (uint)ti.hwnd;
				ti.lpszText = nmtdi.szText;
				::SendMessage(pnmh->hwndFrom, TTM_UPDATETIPTEXT, 0, (LPARAM)&ti);

				// This is required so the tooltip gets resized properly.
				s_fIgnore = true;
				::SendMessage(pnmh->hwndFrom, TTM_UPDATE, 0, 0);
				s_fIgnore = false;
				return true;
			}
		}
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Trap a character so that we can call the OnUpdate and OnChange methods.
----------------------------------------------------------------------------------------------*/
void TssEdit::OnChar(UINT nChar, UINT nRepCnt, UINT nFlags)
{
	SuperClass::OnChar(nChar, nRepCnt, nFlags);

	IVwSelectionPtr qvwsel;
	AssertPtr(m_qrootb);
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (qvwsel)
	{
		ComBool fOk;
		CheckHr(qvwsel->Commit(&fOk));
	}

	OnUpdate();
	::UpdateWindow(m_hwnd);
	OnChange();
}


/*----------------------------------------------------------------------------------------------
	The edit box is receiving the focus.
----------------------------------------------------------------------------------------------*/
bool TssEdit::OnSetFocus(HWND hwndOld, bool fTbControl)
{
	// Using SuperClass below is ambiguous.
	return ScrollSuperClass::OnSetFocus(hwndOld, fTbControl);
}

/*----------------------------------------------------------------------------------------------
	Returns the length of the text.
	Message: WM_GETTEXTLENGTH.
----------------------------------------------------------------------------------------------*/
int TssEdit::GetTextLength()
{
	ITsStringPtr qtss;
	return GetText(&qtss);
}


/*----------------------------------------------------------------------------------------------
	Returns the current text limit, in characters.
	Message: EM_GETLIMITTEXT.
----------------------------------------------------------------------------------------------*/
uint TssEdit::GetLimitText()
{
	// ENHANCE
	return (uint)-1;
}


/*----------------------------------------------------------------------------------------------
	Sets pptss to the specified line of text. It returns the number of characters in the line.
	Message: FW_EM_GETLINE.
----------------------------------------------------------------------------------------------*/
int TssEdit::GetLine(int iLine, ITsString ** pptss)
{
	AssertPtr(pptss);
	Assert(!*pptss);
	// ENHANCE: Currently this returns the whole string, not just the requested line.
	return GetText(pptss);
}


/*----------------------------------------------------------------------------------------------
	Returns the number of lines.
	Message: EM_GETLINECOUNT.
----------------------------------------------------------------------------------------------*/
int TssEdit::GetLineCount()
{
	// ENHANCE
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Return the left and right margins.
	Message: EM_GETMARGINS.
----------------------------------------------------------------------------------------------*/
uint TssEdit::GetMargins()
{
	return MAKELONG(m_dxpMarginLeft, m_dxpMarginRight);
}


/*----------------------------------------------------------------------------------------------
	Return the view style.
	Message: FW_EM_GETSTYLE.
----------------------------------------------------------------------------------------------*/
void TssEdit::GetStyle(StrUni * pstu, COLORREF * pclrBack)
{
	AssertPtr(pstu);
	AssertPtr(pclrBack);
	*pstu = m_stuStyle;
	*pclrBack = m_clrBack;
}


/*----------------------------------------------------------------------------------------------
	Retrieve the anchor and end character positions of the current selection.
	NOTE: *pichAnchor could be greater than *pichEnd.
	Returns true if there is a selection.
	Message: EM_GETSEL.
----------------------------------------------------------------------------------------------*/
bool TssEdit::GetSel(int * pichAnchor, int * pichEnd, bool * pfAssocPrev)
{
	AssertPtrN(pichAnchor);
	AssertPtrN(pichEnd);
	AssertPtrN(pfAssocPrev);
	AssertPtr(m_qrootb);

	int ichDummy;
	bool fDummy;
	if (!pichAnchor)
		pichAnchor = &ichDummy;
	if (!pichEnd)
		pichEnd = &ichDummy;
	if (!pfAssocPrev)
		pfAssocPrev = &fDummy;
	*pichAnchor = 0;
	*pichEnd = 0;
	*pfAssocPrev = true;

	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
		return false;

	int ihvoRoot;
	PropTag tagTextProp;
	int cpropPrevious;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;

	CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, 0, NULL, &tagTextProp,
		&cpropPrevious, pichAnchor, pichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
	*pfAssocPrev = (bool)fAssocPrev;

	return true;
}


/*----------------------------------------------------------------------------------------------
	Returns the index of the line that contains the specified character index. A character
	index is the number of characters from the beginning of the edit control.
	Message: EM_LINEFROMCHAR.
----------------------------------------------------------------------------------------------*/
int TssEdit::LineFromChar(int iLine)
{
	// TODO
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Returns the character index of the specified line. The character index is the number of
	characters from the beginning of the edit control to the specified line.
	Message: EM_LINEINDEX.
----------------------------------------------------------------------------------------------*/
int TssEdit::LineIndex(int iLine)
{
	// TODO
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Returns the length of a line, in characters.
	Message: EM_LINELENGTH.
----------------------------------------------------------------------------------------------*/
int TssEdit::LineLength(int iLine)
{
	// TODO
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Scrolls the text vertically and/or horizontally in a multiline edit control.
	Message: EM_LINESCROLL.
----------------------------------------------------------------------------------------------*/
bool TssEdit::LineScroll(int cLines, int cch)
{
	// TODO
	return false;
}


/*----------------------------------------------------------------------------------------------
	Replace the current selection with the specified text.
	Message: FW_EM_REPLACESEL.
----------------------------------------------------------------------------------------------*/
void TssEdit::ReplaceSel(ITsString * ptss)
{
	AssertPtr(ptss);
	AssertPtr(m_qrootb);

	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
	{
		// If there's not a selection, try to create one at the beginning of the string!
		CheckHr(m_qrootb->MakeTextSelection(0, 0, NULL, ktagString, 0, 0, 0, 0, true, -1, NULL,
			true, &qvwsel));
		if (!qvwsel)
			return;
	}
	CheckHr(qvwsel->ReplaceWithTsString(ptss));

	// ReplaceWithTsString should handle this (JohnT, 8-22-01).
	//CheckHr(m_qcda->PropChanged(NULL, kpctNotifyAll, khvoString, ktagString, 0, 0, 0));
	OnUpdate();
	::UpdateWindow(m_hwnd);
	OnChange();
}


/*----------------------------------------------------------------------------------------------
	Scrolls the caret into view.
	Message: EM_SCROLLCARET.
----------------------------------------------------------------------------------------------*/
void TssEdit::ScrollCaret()
{
	MakeSelectionVisible1();
}


/*----------------------------------------------------------------------------------------------
	Sets the text limit. The text limit is the maximum amount of text, in characters, that the
	edit control can contain.
	Message: EM_SETLIMITTEXT.
----------------------------------------------------------------------------------------------*/
void TssEdit::SetLimitText(UINT nMax)
{
	// TODO
}


/*----------------------------------------------------------------------------------------------
	Set the left and/or right margins.
	Message: EM_SETMARGINS.
----------------------------------------------------------------------------------------------*/
void TssEdit::SetMargins(int fwMargin, int dxpLeft, int dxpRight)
{
	if (fwMargin & EC_LEFTMARGIN)
		m_dxpMarginLeft = dxpLeft;
	if (fwMargin & EC_RIGHTMARGIN)
		m_dxpMarginRight = dxpRight;

	if (m_qrootb)
		CheckHr(m_qrootb->Reconstruct());
}


/*----------------------------------------------------------------------------------------------
	Set or remove the read-only style.
	Message: EM_SETREADONLY.
----------------------------------------------------------------------------------------------*/
void TssEdit::SetReadOnly(bool fReadOnly)
{
	// TODO figure out how to make a TssEdit READONLY
/*	DWORD style = ::GetWindowLong(m_hwnd,GWL_STYLE);
	style |= WS_DISABLED;
	::SetWindowLong(m_hwnd,GWL_STYLE, style);
	StrUni stu("");
	::SendMessage(m_hwnd, FW_EM_SETSTYLE, (WPARAM)::GetSysColor(COLOR_3DFACE), (LPARAM)&stu);
*/
}


/*----------------------------------------------------------------------------------------------
	Selects a range of characters.
	Message: EM_SETSEL.
----------------------------------------------------------------------------------------------*/
void TssEdit::SetSel(int ichAnchor, int ichEnd)
{
	Assert(m_qrootb);

	int cch = 0;
	ITsStringPtr qtss;
	CheckHr(m_qcda->get_StringProp(khvoString, ktagString, &qtss));
	if (qtss)
		CheckHr(qtss->get_Length(&cch));
	if (ichAnchor < 0)
		ichAnchor = cch;
	if (ichEnd < 0)
		ichEnd = cch;
	if (ichAnchor > cch)
		ichAnchor = cch;
	// This can happen; apparently when a tab brings the focus to this window, Windows passes
	// a large number rather than -1 to set the end of the range.
	if (ichEnd > cch)
		ichEnd = cch;
	CheckHr(m_qrootb->MakeTextSelection(0, 0, NULL, ktagString, 0, ichAnchor, ichEnd, 0, true,
		-1, NULL, true, NULL));
}


/*----------------------------------------------------------------------------------------------
	Set the view style.
	Message: FW_EM_SETSTYLE.
----------------------------------------------------------------------------------------------*/
void TssEdit::SetStyle(StrUni * pstu, COLORREF clrBack)
{
	AssertPtr(pstu);
	m_stuStyle = *pstu;
	m_clrBack = clrBack;
	if (m_qrootb)
		CheckHr(m_qrootb->Reconstruct());
}


/*----------------------------------------------------------------------------------------------
	Copy selected text to the clipboard.
	Message: WM_COPY.
	It appears the Copy method is never being called, because the command map intercepts
	the keystroke and calls CmdEditCopy.
----------------------------------------------------------------------------------------------*/
void TssEdit::Copy()
{
	Cmd cmd;  // bogus
	CmdEditCopy(&cmd);
}

bool TssEdit::CmdEditCopy1(Cmd * pcmd)
{
	bool f = SuperClass::CmdEditCopy1(pcmd);
	OnUpdate();
	::UpdateWindow(m_hwnd);
	// Since a copy should never change anything, we (KenZ, DarrellZ) don't understand why
	// this is needed. It actually causes problems with AfDeFeCliRef and AfDeFeComboBox
	// subclasses because they do work that removes the selection of copied text. So unless
	// there is a good reason for needing this, we should leave it out. If we need to put it
	// back, the problems mentioned will need to be solved some other way.
	//OnChange();
	return f;
}

/*----------------------------------------------------------------------------------------------
	Copy selected text to the clipboard and then delete it.
	Message: WM_CUT.
	It appears the Cut method is never being called, because the command map intercepts
	the keystroke and calls CmdEditCut.
----------------------------------------------------------------------------------------------*/
void TssEdit::Cut()
{
	Cmd cmd;  // bogus
	CmdEditCut(&cmd);
}

bool TssEdit::CmdEditCut1(Cmd * pcmd)
{
	bool f = SuperClass::CmdEditCut1(pcmd);
	OnUpdate();
	::UpdateWindow(m_hwnd);
	OnChange();
	return f;
}


/*----------------------------------------------------------------------------------------------
	Paste text from the clipboard.
	Message: WM_PASTE.
	It appears the Paste method is never being called, because the command map intercepts
	the keystroke and calls CmdEditPaste.
----------------------------------------------------------------------------------------------*/
void TssEdit::Paste()
{
	Cmd cmd;  // bogus
	CmdEditPaste(&cmd);
}

bool TssEdit::CmdEditPaste1(Cmd * pcmd)
{
	bool f = SuperClass::CmdEditPaste1(pcmd);
	OnUpdate();
	::UpdateWindow(m_hwnd);
	OnChange();
	return f;
}


/*----------------------------------------------------------------------------------------------
	Set pptss to the string being shown in the edit box. Return the length of the string.
	Message: FW_EM_GETTEXT.
----------------------------------------------------------------------------------------------*/
int TssEdit::GetText(ITsString ** pptss)
{
	AssertPtr(pptss);
	Assert(!*pptss);

	// Commit the selection so that we can access the data from the cache.
	IVwSelectionPtr qvwsel;
	ComBool fOk;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (qvwsel)
		CheckHr(qvwsel->Commit(&fOk));

	CheckHr(m_qcda->get_StringProp(khvoString, ktagString, pptss));
	int cch;
	CheckHr((*pptss)->get_Length(&cch));
	return cch;
}


/*----------------------------------------------------------------------------------------------
	Set the text of the control to be equal to ptss. If ptss is NULL, the edit box is cleared.
	Message: FW_EM_SETTEXT.
----------------------------------------------------------------------------------------------*/
void TssEdit::SetText(ITsString * ptss)
{
	AssertPtrN(ptss); // NULL can be used to clear string.
	Assert(m_qcda);

	ITsStringPtr qtss = ptss;
	if (!ptss)
	{
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		CheckHr(qtsf->MakeStringRgch(L"", 0, m_wsBase, &qtss));
	}
	ITsStringPtr qtssOld;
	CheckHr(m_qcda->get_StringProp(khvoString, ktagString, &qtssOld));
	int cchOld;
	CheckHr(qtssOld->get_Length(&cchOld));
	CheckHr(m_qcda->CacheStringProp(khvoString, ktagString, qtss));
	int cchNew;
	CheckHr(qtss->get_Length(&cchNew));
	// Pretend the whole length has been deleted and the whole new inserted.
	CheckHr(m_qcda->PropChanged(NULL, kpctNotifyAll, khvoString, ktagString, 0, cchNew, cchOld));
	OnUpdate();
	::UpdateWindow(m_hwnd);
	OnChange();
}

/***********************************************************************************************
	These overrides allow the edit control window to scroll horizontally as the selection moves,
	without having a scroll bar.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Get your scroll offsets.
----------------------------------------------------------------------------------------------*/
void TssEdit::GetScrollOffsets(int * pdxd, int * pdyd)
{
	*pdxd = m_dxpScrollOffset;
	*pdyd = 0;
}

/*----------------------------------------------------------------------------------------------
	Return the layout width for the window.
	The return result is in pixels.
----------------------------------------------------------------------------------------------*/
int TssEdit::LayoutWidth()
{
	DWORD dwStyle = ::GetWindowLong(m_hwnd, GWL_STYLE);
	if (dwStyle & ES_MULTILINE)
	{
		// Return the width of the window when we are using multiline edit boxes.
		Rect rc;
		::GetClientRect(m_hwnd, &rc);
		return rc.Width();// - m_dxpMarginLeft - m_dxpMarginRight;
	}
	else
	{
		// This is effectively infinite for single-line edit boxes, since it scrolls
		// horizontally rather than wrapping. We don't use INT_MAX because it can cause
		// certain things to overflow (for example, if the width of the box is INT_MAX,
		// inflating it two pixels for an invalidate produces a rectangle with a negative
		// width that does not work well).
		return INT_MAX / 2;
	}
}

/*----------------------------------------------------------------------------------------------
	Scroll to make the selection visible.
	In general, scroll the minimum distance to make it entirely visible.
	If the selection is higher than the window, scroll the minimum distance to make it
	fill the window.
	If the window is too small to show both primary and secondary, show primary.
	Note: subclasses for which scrolling is disabled should override.
	If psel is null, make the current selection visible.
----------------------------------------------------------------------------------------------*/
void TssEdit::MakeSelectionVisible1(IVwSelection * psel)
{
	//Assert(m_fVScrollEnabled);
	IVwSelectionPtr qvwsel;
	if (!m_qrootb)
	{
		return; // For paranoia.
	}
	if (psel)
		qvwsel = psel;
	else
	{
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
		{
			return; // Nothing we can do.
		}
	}
	Rect rdPrimary;
	Rect rdSecondary;
	ComBool fSplit;
	ComBool fEndBeforeAnchor;
	Rect rcSrcRoot;
	Rect rcDstRoot;
	Rect rdIdeal;
	HoldGraphics hg(this);
	GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
	CheckHr(qvwsel->Location(m_qvg, rcSrcRoot, rcDstRoot, &rdPrimary, &rdSecondary, &fSplit,
		&fEndBeforeAnchor));
	rdIdeal = rdPrimary;

	Rect rcClient;
	m_pwndSubclass->GetClientRect(rcClient);
	if (fSplit)
	{
		rdIdeal.Sum(rdSecondary);
		if (rdIdeal.Width() > rcClient.Width())
			rdIdeal = rdPrimary;
	}
	// OK, we want rdIdeal to be visible.

	// dx gets added to the scroll offset. This means a positive dx causes there to be more
	// of the view hidden left of the screen. This is the same effect as clicking a
	// right arrow, which paradoxically causes the window contents to move left.
	int dx = 0;
	int xdLeft = m_dxpScrollOffset; // Where the window thinks it is now.
	rdIdeal.Offset(xdLeft, 0); // Was in drawing coords, adjusted by left.
	int xdRight = xdLeft + rcClient.Width();

	// Is the selection partly off the left of the screen?
	if (rdIdeal.left < xdLeft)
	{
		// Is it bigger than the screen?
		if (rdIdeal.Width() > rcClient.Width() && !fEndBeforeAnchor)
		{
			// Left is off, and though it is too big to show entirely, we can show
			// more. Move the window contents right (negative dx).
			dx = rdIdeal.right - xdRight;
		}
		else
		{
			// Partly off left, and fits: move window contents right (less is hidden,
			// neg dx).
			dx = rdIdeal.left - xdLeft;
		}
	}
	else
	{
		// Left of selection is right of (or at) the left side of the screen.
		// Is right of selection right of the right side of the screen?
		if (rdIdeal.right > xdRight)
		{
			if (rdIdeal.Width() > rcClient.Width() && fEndBeforeAnchor)
			{
				// Left is visible, right isn't: move until lefts coincide to show as much
				// as possible. This is hiding more text left of the window: positive dx.
				dx = rdIdeal.left - xdLeft;
			}
			else
			{
				// Fits entirely: scroll left minimum to make right visible. This involves
				// hiding more text at the left: positive dx.
				dx = rdIdeal.right - xdRight;
			}
		}
		// Else it is already entirely visible, do nothing.
	}
	if (dx + m_dxpScrollOffset < 0)
		dx = -m_dxpScrollOffset; // make offset 0 if it would have been less than that
	if (dx)
	{
		// Update the actual position.
		m_dxpScrollOffset += dx;
	}
	ScrollBy(dx, 0);
}

/*----------------------------------------------------------------------------------------------
	Should not be needed for an edit box, but just in case, let it do something reasonable.
----------------------------------------------------------------------------------------------*/
void TssEdit::ScrollSelectionNearTop1(IVwSelection * psel)
{
	MakeSelectionVisible1(psel);
}

/*----------------------------------------------------------------------------------------------
	With no scroll bar, the only way this happens is when the user drags outside the window.
	We need to override because the superclass does nothing when there is no scroll bar.
----------------------------------------------------------------------------------------------*/
bool TssEdit::OnHScroll(int nSBCode, int nPos, HWND hwndSbar)
{
	// NB - DON'T use nPos; it has only a 16-bit range.
	int dxdPos = m_dxpScrollOffset; // Where the window thinks it is now.
	// ENHANCE JohnT: use actual resolution.
	int dxdLine = 30 * 96/72; // 30 points seems a useful size increment.

	switch (nSBCode)
	{
	case SB_LINELEFT:
		dxdPos -= dxdLine;
		break;
	case SB_LINERIGHT:
		dxdPos += dxdLine;
		break;
	default:
		dxdPos = 0;
		Assert(false); // others should not happen
		break;
	}
	// Try to stop it scrolling too far. This is unfortunately not easy to do. Try getting
	// the width of a selection of the whole thing and limit it to a bit more than that.
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->MakeSimpleSel(true, false, true, false, &qvwsel));
	HoldGraphics hg(this);
	Rect rdPrimary;
	Rect rdSecondary;
	ComBool fSplit;
	ComBool fEndBeforeAnchor;
	CheckHr(qvwsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rdPrimary,
		&rdSecondary, &fSplit, &fEndBeforeAnchor));
	Rect rcClient;
	::GetClientRect(m_hwnd, &rcClient);
	int dxpMax = rdPrimary.Width() - rcClient.Width() + 20;
	if (dxdPos > dxpMax)
		dxdPos = dxpMax;

	// In this class we don't have to worry about a max.
	if (dxdPos < 0)
		dxdPos = 0;

	int dxdScrollBy = dxdPos - m_dxpScrollOffset;

	// Update the scroll position.
	m_dxpScrollOffset = dxdPos;

	ScrollBy(dxdScrollBy, 0);
	return true;
}

/***********************************************************************************************
	TssEditVc methods.
***********************************************************************************************/

static DummyFactory g_fact(_T("SIL.Widgets.TssEditVc"));

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.
	The TssEdit window only consists of a single TsString.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TssEditVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	Assert(frag == TssEdit::kfrString);

	HWND hwndDesk = ::GetDesktopWindow();
	HDC hdc = ::GetDC(hwndDesk);
	int ypLogPixels = ::GetDeviceCaps(hdc, LOGPIXELSY);
	int xpLogPixels = ::GetDeviceCaps(hdc, LOGPIXELSX);
	int iSuccess;
	iSuccess = ::ReleaseDC(hwndDesk, hdc);
	Assert(iSuccess);

	int dxmpLeft = m_pte->m_dxpMarginLeft * kdzmpInch / xpLogPixels;
	int dxmpRight = m_pte->m_dxpMarginRight * kdzmpInch / xpLogPixels;
	int dympTop = m_pte->m_dypMarginTop * kdzmpInch / ypLogPixels;

	CheckHr(pvwenv->put_IntProperty(ktptRightToLeft, ktpvEnum, m_fRtl));
	if (m_fRtl)
	{
		CheckHr(pvwenv->put_IntProperty(kspMarginLeading, ktpvMilliPoint, dxmpRight));
		CheckHr(pvwenv->put_IntProperty(kspMarginTrailing, ktpvMilliPoint, dxmpLeft));
	}
	else
	{
		CheckHr(pvwenv->put_IntProperty(kspMarginLeading, ktpvMilliPoint, dxmpLeft));
		CheckHr(pvwenv->put_IntProperty(kspMarginTrailing, ktpvMilliPoint, dxmpRight));
	}
	CheckHr(pvwenv->put_IntProperty(kspMarginTop, ktpvMilliPoint, dympTop));
	CheckHr(pvwenv->put_StringProperty(kspNamedStyle, m_pte->m_stuStyle.Bstr()));
	CheckHr(pvwenv->put_IntProperty(ktptParaColor, ktpvDefault, m_pte->m_clrBack));
	CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, m_tptEditable));

	if (m_fShowTags)
		CheckHr(pvwenv->OpenTaggedPara());
	else
		CheckHr(pvwenv->OpenParagraph());
	CheckHr(pvwenv->AddStringProp(TssEdit::ktagString, this));
	CheckHr(pvwenv->CloseParagraph());

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}
