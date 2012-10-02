/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDialog.cpp
Responsibility: Darrell Zook
Last reviewed:

Description:
	This file contains code for dialogs. It contains the following classes:
		AfDialog : AfWnd - This is the base class for all dialogs (both modal or modeless).
		AfDialogView : AfDialog - This is the base class for dialogs that should be contained
			within another dialog. This is mainly used for dialogs that show up on tab controls,
			where the user can switch between dialogs. This class doesn't actually do anything
			but provide virtual methods that should be overridden to take the appropriate
			action when called. These dialogs are always modeless.
		HelpAboutDlg : AfDialog - This is a generic Help dialog for an application.
		AfButton : AfWnd - This class provides our special button functionality, including
			an icon to the left of the text or a down arrow to the right of the text used for
			popup menus.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


// When enabled, this pops up control tool tips when mouse hovers over control.
#ifdef INCLUDE_TOOL_TIPS
// This variable is global because the static method that intercepts messages to child
// windows in the dialog needs to access member variables in the dialog class. This is set
// when a dialog is activated and cleared when a dialog is deactivated.
AfDialogPtr g_qdlg;
#endif INCLUDE_TOOL_TIPS

// These are required so that modeless dialogs handle accelerator keys properly.
HWND AfDialog::s_hwndCurModelessDlg;
HHOOK AfDialog::s_hhook;
IHelpTopicProviderPtr AfDialog::s_qhtprovHelpUrls;

/***********************************************************************************************
	AfDialog methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDialog::AfDialog(void)
{
	#ifdef INCLUDE_TOOL_TIPS
		m_hhook = NULL;
		m_hwndToolTip = NULL;
	#endif INCLUDE_TOOL_TIPS

	m_hwnd = NULL;
	m_rid = 0;
	m_pszHelpUrl = NULL;
	m_pszHelpFile = NULL;
	m_fModeless = false;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDialog::~AfDialog()
{
	#ifdef INCLUDE_TOOL_TIPS
		if (m_hhook)
			UnhookWindowsHookEx(m_hhook);
		if (m_hwndToolTip)
			DestroyWindow(m_hwndToolTip);
	#endif INCLUDE_TOOL_TIPS
}

/*----------------------------------------------------------------------------------------------
	Put up a modeless dialog. The dialog will not automatically be visible unless the Visible
	dialog style is set, so call ::ShowWindow to show the dialog after you call this method.

	NOTE: This method returns as soon as the dialog is created. It doesn't wait until the dialog
	is closed, like DoModal does.

	NOTE: Creating a dialog with this method means that it should not be closed with the
	::EndDialog method. It should be closed with the ::DestroyWindow method.
----------------------------------------------------------------------------------------------*/
void AfDialog::DoModeless(HWND hwndPar, int rid, void * pv)
{
	AssertObj(this);

	AfWndCreate wcs = { this, pv };

	if (rid == 0)
	{
		Assert(m_rid);
		rid = m_rid;
	}

	m_fModeless = true;

	HWND hwnd;
	hwnd = ::CreateDialogParam(ModuleEntry::GetModuleHandle(), MAKEINTRESOURCE(rid), hwndPar,
		&AfDialog::DlgProc, (LPARAM)&wcs);
	DWORD dwError;
	dwError = ::GetLastError();
}

/*----------------------------------------------------------------------------------------------
	Put up a modal dialog.
	NOTE: This method does not return until the dialog is closed.

	Returns (from the MSDN documentation):
		If the function succeeds, the return value is the value of the nResult parameter
		specified in the call to the EndDialog function used to terminate the dialog box.

		If the function fails because the hwndPar parameter is invalid, the return value is
		zero. The function returns zero in this case for compatibility with previous versions of
		Windows. If the function fails for any other reason, the return value is -1. To get
		extended error information, call GetLastError.
----------------------------------------------------------------------------------------------*/
int AfDialog::DoModal(HWND hwndPar, int rid, void * pv)
{
	AssertObj(this);
	// In extremely rare cases (such as a modal dialog by the backup software when we don't
	// have any windows open) the parent may be missing. But normally this is an indication
	// of an error since the dialog will not be modal without it.
#if DEBUG
	if (!hwndPar)
		Warn("This dialog is missing hwndPar, so it isn't modal.");
#endif //DEBUG

	AfWndCreate wcs = { this, pv };

	if (rid == 0)
	{
		Assert(m_rid);
		rid = m_rid;
	}
	int nRet = ::DialogBoxParam(ModuleEntry::GetModuleHandle(), MAKEINTRESOURCE(rid), hwndPar,
		&AfDialog::DlgProc, (LPARAM)&wcs);
#if DEBUG
	if (nRet == -1 || nRet == 0)
	{
		DWORD dwErr = ::GetLastError();
		achar * pszMsgBuf;
		::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM|FORMAT_MESSAGE_ALLOCATE_BUFFER,
			NULL, dwErr, 0, reinterpret_cast<achar *>(&pszMsgBuf), 0, NULL);
		::MessageBox(hwndPar, pszMsgBuf, _T("DEBUG"), MB_OK | MB_ICONWARNING);
		::LocalFree(pszMsgBuf);
	}
#endif //DEBUG
	return nRet;
}

/*----------------------------------------------------------------------------------------------
	Static dialog proc.
----------------------------------------------------------------------------------------------*/
BOOL CALLBACK AfDialog::DlgProc(HWND hwnd, uint wm, WPARAM wp, LPARAM lp)
{
	Assert(hwnd);

	AfDialogPtr qdlg = (AfDialog *)GetWindowLong(hwnd, GWL_USERDATA);
	AssertPtrN(qdlg);

	if (!qdlg)
	{
		if (wm != WM_INITDIALOG)
			return false;

		// This should be reached when the dialog is first being created.
		AfWndCreate * pwcs = (AfWndCreate *)lp;
		AssertPtr(pwcs);

		qdlg = (AfDialog *)pwcs->pwnd;
		AssertPtr(qdlg);
		Assert(!qdlg->m_hwnd);

		try
		{
			qdlg->AttachHwnd(hwnd);
		}
		catch (...)
		{
			Warn("Exception attaching dialog to hwnd");
			if (qdlg->m_fModeless)
				::DestroyWindow(hwnd);
			else
				::EndDialog(hwnd, -1);
			return false;
		}

		// Put the user-defined value back into lp so that the subclass will have it.
		lp = (LPARAM)pwcs->pv;
	}

	Assert(qdlg->m_hwnd == hwnd);

	long lnRet = 0;
	bool fRet = false;

	#ifdef INCLUDE_TOOL_TIPS
		if (wm == WM_ACTIVATE)
		{
			if (LOWORD(wp) == WA_INACTIVE)
				g_qdlg = NULL;
			else
				g_qdlg = qdlg;
		}
	#endif INCLUDE_TOOL_TIPS

	// If a top-level modeless dialog is becoming activated or deactivated, set up or remove
	// the hook procedure that traps messages meant for the modeless dialog.
	if (qdlg->m_fModeless && wm == WM_ACTIVATE && !dynamic_cast<AfDialogView *>(qdlg.Ptr()))
	{
		qdlg->HandleDlgMessages(qdlg->Hwnd(), wp, lp);
	}

	if (wm == WM_NCDESTROY && s_hwndCurModelessDlg == hwnd)
	{
		// Somehow this modeless dialog is getting destroyed without
		// getting a WM_ACTIVATE message, so we need to release the hook.
		// Without this here, we get an Assert the next time a modeless
		// dialog is created.
		qdlg->HandleDlgMessages(qdlg->Hwnd(), wp, lp);
	}

	try
	{
		// Call the pre non-virtual WndProc defined on AfWnd.
		fRet = qdlg->FWndProcPre(wm, wp, lp, lnRet);

		// Call the virtual dialog proc.
		if (!fRet)
			fRet = qdlg->FWndProc(wm, wp, lp, lnRet);

		if (wm == WM_INPUTLANGCHANGEREQUEST)
		{
			// Quoting from MSDN,
			// "This message is posted, not sent, to the application, so the return value is
			// ignored. To accept the change, the application should pass the message to
			// DefWindowProc. To reject the change, the application should return zero without
			// calling DefWindowProc."
			lnRet = ::DefWindowProc(hwnd, wm, wp, lp);
		}
		else
		{
			qdlg->WndProcPost(true, hwnd, wm, wp, lp, lnRet);
		}
	}
	catch (...)
	{
		Warn("Exception caught in dialog proc");
	}

	// lnRet goes in the DWL_MSGRESULT field. See the description of DialogProc in MSDN.
	if (fRet && lnRet)
		switch (wm)
		{
		case WM_CHARTOITEM:
		case WM_COMPAREITEM:
		case WM_CTLCOLORBTN:
		case WM_CTLCOLORDLG:
		case WM_CTLCOLOREDIT:
		case WM_CTLCOLORLISTBOX:
		case WM_CTLCOLORSCROLLBAR:
		case WM_CTLCOLORSTATIC:
		case WM_INITDIALOG:
		case WM_QUERYDRAGICON:
		case WM_VKEYTOITEM:
			return lnRet; // These exceptions return lnRet directly.
		default:
			::SetWindowLong(hwnd, DWL_MSGRESULT, lnRet);
			break;
		}

	return fRet;
}

/*----------------------------------------------------------------------------------------------
	Handle window messages.

	@param wm Window Message

	@param wp Handle to the control to receive the default keyboard focus. The system assigns
	the default keyboard focus only if the dialog box procedure returns TRUE.

	@param lp Specifies additional initialization data. This data is passed to the system as
	the lParam parameter in a call to the CreateDialogIndirectParam, CreateDialogParam,
	DialogBoxIndirectParam, or DialogBoxParam function used to create the dialog box. For
	property sheets, this parameter is a pointer to the PROPSHEETPAGE structure used to create
	the page. This parameter is zero if any other dialog box creation function is used.

	@param lnRet Return value
----------------------------------------------------------------------------------------------*/
bool AfDialog::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_ACTIVATEAPP:
		{
			if (!wp)
				break; // don't care if being deactivated.
			// Resynch any changes made to the database by other apps.
			// NOTE: we must first check for existence of global application pointer and
			// MainWindow, because Backup has neither, yet uses this class.
			AfDbApp * papp = dynamic_cast<AfDbApp *>(AfApp::Papp());
			if (papp)
			{
				AfMainWnd * pafw = MainWindow();
				if (pafw)
				{
					AfLpInfo * plpi = pafw->GetLpInfo();
					if (plpi)
						papp->DbSynchronize(plpi);
				}
			}
		}
		break;
	case WM_INITDIALOG:
		return OnInitDlg((HWND)wp, lp);
	case WM_ACTIVATE:
		return OnActivate((LOWORD(wp) != WA_INACTIVE), lp);
	case WM_HELP:
		{
			// If the Shift key is down (Shift + F1) or the left mouse button is down, we
			// want to show the What's This help instead of opening the help file.
			if (::GetKeyState(VK_SHIFT) < 0 || ::GetKeyState(VK_LBUTTON) < 0)
				return OnHelpInfo((HELPINFO *)lp);
			else
				return OnHelp();
		}
	case WM_SETFOCUS:
		return OnSetFocus();
	// default: nothing special.
	}

	return AfWnd::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Center the dialog in the parent window when it is created.
----------------------------------------------------------------------------------------------*/
bool AfDialog::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	#ifdef INCLUDE_TOOL_TIPS
		InitializeToolTip(kdxpDefTipWidth);
	#endif // INCLUDE_TOOL_TIPS

	CenterInWindow(::GetParent(m_hwnd));
	return UpdateData(false);
}

/*----------------------------------------------------------------------------------------------
	Called when the window is activated.
----------------------------------------------------------------------------------------------*/
bool AfDialog::OnActivate(bool fActivating, LPARAM lp)
{
	return false;
}

/*----------------------------------------------------------------------------------------------
	Called when the window is painted.
----------------------------------------------------------------------------------------------*/
//bool AfDialog::OnPaint(HDC hdc)
//{
//	bool f = SuperClass::OnPaint(hdc);
//
//	return f;
//}

/*----------------------------------------------------------------------------------------------
	Called when the window gets the focus.
----------------------------------------------------------------------------------------------*/
//bool AfDialog::OnSetFocus()
//{
//	TurnOnDefaultKeyboard();
//	return true;
//}

/*----------------------------------------------------------------------------------------------
	Process notifications for this dialog from some event on a control.

	@param ctid Id of the control that issued the windows command.
	@param pnmh Windows command that is being passed.
	@param lnRet return value to be returned to the windows command.
	@return true if command is handled.

	See ${AfWnd#OnNotifyChild}
----------------------------------------------------------------------------------------------*/
bool AfDialog::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	if (pnmh->code == CBN_SETFOCUS || pnmh->code == EN_SETFOCUS)
		// Prepare to type in a field.
		// ENHANCE (SharonC): it may be useful to look at the control that received the
		// focus and not turn on the default keyboard if it is a TssEdit or TssCombo.
		TurnOnDefaultKeyboard();

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Show help information for the requested control.
----------------------------------------------------------------------------------------------*/
bool AfDialog::OnHelpInfo(HELPINFO * phi)
{
	AssertPtr(phi);

	// Get the coordinates of the control and center the tooltip underneath the control.
	Rect rc;
	::GetWindowRect((HWND)phi->hItemHandle, &rc);
	phi->MousePos.x = rc.left + (rc.Width() / 2);
	phi->MousePos.y = rc.bottom + 1;

	AfContextHelpWndPtr qchw;
	qchw.Attach(NewObj AfContextHelpWnd);
	qchw->Create(m_hwnd, phi);
	return true;
}

/*----------------------------------------------------------------------------------------------
	This method centers the dialog in the first non-child window that contains the window
	specified by hwnd. If hwnd is NULL, the dialog is centered in the desktop.
----------------------------------------------------------------------------------------------*/
void AfDialog::CenterInWindow(HWND hwndPar)
{
	Assert(m_hwnd);

	// Find the first non-child parent window.
	if (hwndPar == NULL)
	{
		hwndPar = ::GetDesktopWindow();
	}
	else
	{
		HWND hwndT = hwndPar;
		while (hwndT)
		{
			hwndPar = hwndT;
			if (!(::GetWindowLong(hwndT, GWL_STYLE) & WS_CHILD))
				break;
			hwndT = ::GetParent(hwndT);
		}
	}
	Assert(hwndPar);

	// Calculate the new coordinates for this dialog and move it there.
	Rect rcParent;
	::GetWindowRect(hwndPar, &rcParent);
	Rect rc;
	::GetWindowRect(m_hwnd, &rc);
	Rect rcDlg(rcParent.left + (rcParent.Width() - rc.Width()) / 2,
		rcParent.top + (rcParent.Height() - rc.Height()) / 2);
	rcDlg.right = rcDlg.left + rc.Width();
	rcDlg.bottom = rcDlg.top + rc.Height();
	AfGfx::EnsureVisibleRect(rcDlg);
	::MoveWindow(m_hwnd, rcDlg.left, rcDlg.top, rcDlg.Width(), rcDlg.Height(), true);
}

/*----------------------------------------------------------------------------------------------
	Find the top level dialog (returns "this" if the current dialog is the top one).
----------------------------------------------------------------------------------------------*/
AfDialog * AfDialog::GetTopDialog()
{
	AfDialog * pdlg = this;
	AfDialog * pdlgT = this;
	while (pdlgT && ::GetWindowLong(pdlg->Hwnd(), GWL_STYLE) & WS_CHILDWINDOW)
	{
		pdlgT = dynamic_cast<AfDialog *>(AfWnd::GetAfWnd(::GetParent(pdlg->Hwnd())));
		if (pdlgT != NULL)
			pdlg = pdlgT;
	}
	return pdlg;
}

/*----------------------------------------------------------------------------------------------
	This method will automatically get called for modeless dialogs when they get activated or
	inactivated. It allows them to handle keyboard functionality (hot keys and tabbing between
	controls). This also can be used for non-dialog windows that want to have the same
	functionality.

	NOTE: It is VERY important that this gets called when the window is activated AND
	inactivated. Otherwise, you'll get assertion failures, and the program could hang.
----------------------------------------------------------------------------------------------*/
void AfDialog::HandleDlgMessages(HWND hwndDlg, bool fActivate)
{
	if (fActivate)
	{
		Assert(!s_hwndCurModelessDlg);
		s_hwndCurModelessDlg = hwndDlg;
		Assert(!s_hhook);
		s_hhook = ::SetWindowsHookEx(WH_GETMESSAGE, &AfDialog::GetMsgProc, NULL,
			::GetCurrentThreadId());
	}
	else
	{
		Assert(s_hwndCurModelessDlg);
		s_hwndCurModelessDlg = NULL;
		Assert(s_hhook);
		::UnhookWindowsHookEx(s_hhook);
		s_hhook = NULL;
	}
}

/*----------------------------------------------------------------------------------------------
	Convert WM_COMMAND messages from child windows to WM_NOTIFY messages.
----------------------------------------------------------------------------------------------*/
bool AfDialog::OnCommand(int cid, int nc, HWND hctl)
{
	// For some STUPID reason, this method gets called when a treeview or listview
	// creates an edit box to allow in-place editing of an item in the control. Since the
	// edit window has an id of 1 (kctidOk), this ended up calling OnApply and closing the
	// dialog. So, to get around this problem, I'm (DarrellZ) making sure the parent of the
	// control is actually the dialog before calling OnApply or OnCancel. This seems to work
	// because the parent of the edit control is the treeview or listview control.

	BOOL fHctlIsWindow = false;
	if (hctl)
		fHctlIsWindow = ::IsWindow(hctl);
	HWND hwndParent = NULL;
	if (fHctlIsWindow)
		hwndParent = ::GetParent(hctl);
	if (!hctl || (fHctlIsWindow && (hwndParent == m_hwnd)))
	{
		// None of these methods should be called for an AfDialogView window. Instead, it should
		// forward the message to the parent (AfDialog) window. The reason this was added was
		// because a multiline edit control in a dialog view would close the dialog view when
		// the Escape key was pressed instead of closing the top-level dialog window containing
		// the dialog view.
		if (!hctl &&
			(cid == kctidOk || cid == kctidCancel) &&
			dynamic_cast<AfDialogView *>(this))
		{
			AfDialog * pdlg = dynamic_cast<AfDialog *>(AfWnd::GetAfWnd(::GetParent(m_hwnd)));
			if (pdlg)
				return pdlg->OnCommand(cid, nc, hctl);
		}

		if (cid == kctidOk && OnApply(true))
			return true;
		if (cid == kctidCancel && OnCancel())
			return true;
		if (cid == kctidHelp && OnHelp())
			return true;
	}

	if (hctl && hwndParent)
	{
		// Convert to a notify message.
		// NOTE: In some cases, the parent of hctl might not be this dialog. It seems in some
		// of our 'embedded' dialogs, the command messages we get from a button go to the top
		// level dialog instead of to the dialog that contains the button. So, to fix this
		// problem, send the WM_NOTIFY message to the parent of the control instead of calling
		// OnNotifyChild.
		NMHDR nmh;
		nmh.hwndFrom = hctl;
		nmh.idFrom = cid;
		nmh.code = nc;
		::SendMessage(hwndParent, WM_NOTIFY, cid, (LPARAM)&nmh);
		return true;
	}

	return SuperClass::OnCommand(cid, nc, hctl);
}

/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the OK or the Apply Now button.
	When the framework calls this method, changes made in the dialog are accepted.
	The default OnApply closes the dialog.

	@param fClose not used here
	@return true if Successful
----------------------------------------------------------------------------------------------*/
bool AfDialog::OnApply(bool fClose)
{
	bool fT = UpdateData(true);
	if (m_fModeless)
	{
		::DestroyWindow(m_hwnd);
		m_hwnd = NULL;
	}
	else
		::EndDialog(m_hwnd, kctidOk);
	return fT;
}

/*----------------------------------------------------------------------------------------------
	The default OnCancel closes the dialog. It returns kctidCancel from the DoModal method if it
	is a modal dialog.
----------------------------------------------------------------------------------------------*/
bool AfDialog::OnCancel()
{
	if (m_fModeless)
	{
		::DestroyWindow(m_hwnd);
		m_hwnd = NULL;
	}
	else
		::EndDialog(m_hwnd, kctidCancel);
	return true;
}

/*----------------------------------------------------------------------------------------------
	The default OnHelp shows the help page for the dialog if there is one.
----------------------------------------------------------------------------------------------*/
bool AfDialog::OnHelp()
{
#ifdef DEBUG
	if (!m_pszHelpUrl)
		::MessageBox(NULL, _T("Missing a help page for this dialog."), NULL,
			MB_OK | MB_ICONWARNING);
#endif
	if (m_pszHelpUrl != NULL)
	{
		if (AfApp::Papp() != NULL)
		{
			return AfApp::Papp()->ShowHelpFile(m_pszHelpUrl);
		}
		else if (m_pszHelpFile != NULL)
		{
			StrAppBufPath strbpHelp;
			strbpHelp.Format(_T("%s::/%s"), m_pszHelpFile, m_pszHelpUrl);
			HtmlHelp(::GetDesktopWindow(), strbpHelp.Chars(), HH_DISPLAY_TOPIC, NULL);
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	This method is part of figuring out the initial state of various formatting dialogs.
	If we had a single run/paragraph to contend with, we would want to know the value of that
	property (if any) specified in the TsTextProps that the dialog is editing, and the value
	that the property actually has (if it is not specified in the TsTextProps, to show in gray).

	With multiple runs or paragraphs, we call this method repeatedly, once for each run.
	The first time, we return the 'old' value (what's in the TsTextProps before the dialog
	runs) and the 'current' value (what's actually displayed).

	On subsequent runs, we check whether both values are the same as before, and if not, set
	to knConflicting, indicating that for the current selection there is no unique value for
	the property.

	Here's the original comment, which may still help explain some special cases.

	Derive an old and current value for property tpt. The two are the same except that the old
	value is either obtained from pttp or knUnspecified; the current value is obtained from pttp
	if possible, otherwise from pvps.
	If the wrong variation is found in pttp, set both to knConflicting.
	If fFirst is true (the first pttp/pvs processed), then ignore the initial values of nValOld
	and nValCur; if it is false, then if this pass does not yield the same result as passed in,
	set both to knConflicting.
	If fFirst is false and values are already conflicting, don't bother reading.

	When called from hard-formatting dialogs, we generally send either the pttp or the pvps.
	In this situation, nValCur will receive the "soft" value from the pvps and nValOld will
	receive the "hard" value from the the pttp; they are kept strictly separate.

	Note: When called from FmtParaDlg, in some cases pttp may be null. This should be
	interpreted as finding no value recorded for the property.
----------------------------------------------------------------------------------------------*/
void AfDialog::MergeIntProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
	int varExpected, int & nValOld, int & nValCur, bool fFirst)
{
	if ((!fFirst) && nValOld == FwStyledText::knConflicting)
		return;
	int nVarThis; // The variant of the property from pttp, or knUnspecified.
	int nValThis; // The value of the property as specified in pttp, or knUnspecified
	int nCur; // The value from pttp if there is one, or if not the value from pvps,
				// or knUnspecified if pvps is null. The actual property displayed currently.

	nCur = nValThis = FwStyledText::knUnspecified;

	HRESULT hr = S_FALSE;
	if (pttp)
		CheckHr(hr = pttp->GetIntPropValues(tpt, &nVarThis, &nValThis));
	if (hr == S_FALSE)
	{
		nValThis = FwStyledText::knUnspecified;
		if (pvps)
			CheckHr(pvps->get_IntProperty(tpt, &nCur));
		if (tpt == ktptBold)
		{
			// The VwPropertyStore uses a special enumeration for bold.
			switch (nCur)
			{
			case 400:
				nCur = kttvOff;
				break;
			case 700:
				nCur = kttvInvert;
				break;
			default:
				// If the environment specifies something else, this dialog can't handle it.
				nCur = nValThis = FwStyledText::knUnspecified;
				break;
			}
		}
	}
	else // hr is not S_FALSE, we found the property in the pttp.
	{
		// We got a value from pttp in nOld; use for nCur as well.
		nCur = nValThis;
		// Set both to conflicting if using a variation we do not know how to represent:
		// this indicates to the user that we don't have a definite, consistent value for the
		// property.
		if (nVarThis != varExpected)
			nValThis = nCur = FwStyledText::knConflicting;
	}

	if (fFirst)
	{
		nValOld = nValThis;
		nValCur = nCur;
	}
	else
	{
		if (nValOld != nValThis)
			nValOld = FwStyledText::knConflicting;
		if (nValCur != nCur)
			nValCur = FwStyledText::knConflicting;
		if (pttp && pvps &&
			(nValOld == FwStyledText::knConflicting || nValCur == FwStyledText::knConflicting))
		{
			// Not keeping separate track of hard-formatting:
			nValOld = nValCur = FwStyledText::knConflicting;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Derive an old and current value for property tpt. The two are the same except that the old
	value is either obtained from pttp or knUnspecified; the current value is obtained from pttp
	if possible, otherwise from pvps.
	Unlike the simpler MergeIntProp, this routine handles a property where we are using multiple
	variations. There is a similar nVarOld and nVarCur, which are treated in the same way.
	If fFirst is true (the first pttp/pvs processed), then ignore the initial values of nValOld,
	nValCur, nVarOld, and nVarCur; if it is false, then if this pass does not yield the same
	result as passed in, set all four to knConflicting.
	If fFirst is false and values are already conflicting, don't bother reading the prop.
	There are special cases for various properties to decide what info to get from pvps as
	default.
----------------------------------------------------------------------------------------------*/
void AfDialog::MergeMvIntProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
	int & nValOld, int & nValCur, int & nVarOld, int & nVarCur, bool fFirst)
{
	if ((!fFirst) && nValOld == FwStyledText::knConflicting)
		return;
	// Get values for old and current value and variation, as if this were the only property.
	int nVarCurThis, nValCurThis, nVarOldThis, nValOldThis;
	HRESULT hr = S_FALSE;
	// If we have a props at all see if it has a value for this property.
	if (pttp)
		CheckHr(hr = pttp->GetIntPropValues(tpt, &nVarOldThis, &nValOldThis));
	if (hr == S_FALSE)
	{
		// The ttp did not specify it. Therefore both old values are unspecified.
		nValOldThis = nVarOldThis = FwStyledText::knUnspecified;
		// And we obtain the current value from the pvps.
		if (pvps)
			CheckHr(pvps->get_IntProperty(tpt, &nValCurThis));
		switch (tpt)
		{
		default:
			Assert(tpt != ktptBold); // otherwise, we need to convert the weight
			nVarCurThis = ktpvMilliPoint;
			break;
		case ktptLineHeight:
			// If a relative height was specified, use it.
			int nRelHeight = 0;
			if (pvps)
				CheckHr(pvps->get_IntProperty(kspRelLineHeight, &nRelHeight));
			if (nRelHeight)
			{
				nVarCurThis = ktpvRelative;
				nValCurThis = nRelHeight;
				break;
			}
			// By default, we have no min spacing; interpret this as single-space.
			if (!nValCurThis)
			{
				nVarCurThis = ktpvRelative;
				nValCurThis = kdenTextPropRel;
				break;
			}
			// Otherwise interpret as absolute. Use the value we already got from pvps.
			nVarCurThis = ktpvMilliPoint;
			break;
		}
	}
	else
	{
		// We got a value from pttp for old; use for current as well.
		nValCurThis = nValOldThis;
		nVarCurThis = nVarOldThis;
	}

	// Now, if this is the first time, just use the values we got from this.
	if (fFirst)
	{
		nValOld = nValOldThis;
		nValCur = nValCurThis;
		nVarOld = nVarOldThis;
		nVarCur = nVarCurThis;
	}
	// Otherwise, if all values are the same do nothing; but if any value is different, set
	// everything to conflicting.
	else
	{
		if (nValOld != nValOldThis || nVarOld != nVarOldThis)
			nValOld = nVarOld = FwStyledText::knConflicting;
		if (nValCur != nValCurThis || nVarCur != nVarCurThis)
			nValCur = nVarCur = FwStyledText::knConflicting;
		if (pttp && pvps &&
			(nValOld == FwStyledText::knConflicting || nValCur == FwStyledText::knConflicting))
		{
			// Not keeping separate track of hard-formatting:
			nValOld = nValCur = nVarOld = nVarCur = FwStyledText::knConflicting;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Derive an old and current value for inverting property tpt. The two are the same except that
	the old value is either obtained from pttp or knUnspecified; the current value is obtained
	from pvps.
	If the wrong variation is found in pttp, set both to knConflicting.
	If fFirst is true (the first pttp/pvs processed), then ignore the initial values of nValOld
	and nValCur; if it is false, then if this pass does not yield the same result as passed in,
	set both to knConflicting.
	If fFirst is false and values are already conflicting, don't bother reading.
	nValCur is always set to either kttvOff or kttvInvert
	JohnT (10 Sep 2002): variable names in this method are out of date and all messed up.
	nOldRet is supposed to be set to the hard formatting value, and nCurRet to the soft one,
	if I've figured it out right.
	If the hard value is 'inverting', then we need the soft value to figure out what it really
	is. I changed the main call so we get it.
----------------------------------------------------------------------------------------------*/
void AfDialog::MergeInvertingProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
	int varExpected, int & nOldRet, int & nCurRet, bool fFirst)
{
	if ((!fFirst) && nOldRet == FwStyledText::knConflicting)
		return;
	int nVar, nOld, nCur;
	Assert(pvps != NULL); // we need this too!

	// Compute the soft (inherited) value as invert (used for some historical reason to mean
	// 'on') or off.
	CheckHr(pvps->get_IntProperty(tpt, &nCur));
	if (tpt == ktptBold)
		nCur = (nCur >= 550) ? kttvInvert : kttvOff;
	else if (nCur != kttvOff)
		nCur = kttvInvert;

	nOld = FwStyledText::knUnspecified;
	if (pttp)
	{
		HRESULT hr = S_FALSE;
		CheckHr(hr = pttp->GetIntPropValues(tpt, &nVar, &nOld));
		// Set both to conflicting if using a variation we do not know how to represent:
		// this indicates to the user that we don't have a definite, consistent value for the
		// property.
		if (hr == S_FALSE)
			nOld = FwStyledText::knUnspecified;
		else if (nVar != varExpected)
			nOld = FwStyledText::knConflicting;
#ifdef JohnT_10_Sep_02_Unused
		// If we have pttp, we're figuring hard formatting and don't care about soft.
		else
		{
			nCur = nOld;
		}
#endif
		if (nOld == kttvInvert)
		{
			// The value we got from the ttp is 'invert'. What it actually means depends
			// on what it is inheriting from.
			nOld = (nCur == kttvOff ? kttvInvert : kttvOff);
		}
	}

	if (fFirst)
	{
		nOldRet = nOld;
		nCurRet = nCur;
	}
	else
	{
		if (nOldRet != nOld)
			nOldRet = FwStyledText::knConflicting;
		if (nCurRet != nCur)
			nCurRet = FwStyledText::knConflicting;
	}
}

/*----------------------------------------------------------------------------------------------
	Derive an old and current value for property tpt. The two are the same except that the old
	value is either obtained from pttp or knUnspecified; the current value is obtained from pttp
	if possible, otherwise from pvps.
	This function applies to string properties.

	Note: When called from FmtParaDlg, in some cases pttp may be null. This should be
	interpreted as inheriting the property.
----------------------------------------------------------------------------------------------*/
void AfDialog::MergeStringProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
	StrUni & stuValOld, StrUni & stuValCur, bool fFirst, OLECHAR * pszConflict)
{
	StrUni stuOld, stuCur;
	SmartBstr sbstr;

	if ((!fFirst) && stuValOld == pszConflict)
		return; // Already conflicting.

	HRESULT hr = S_FALSE;
	if (pttp)
		CheckHr(hr = pttp->GetStrPropValue(tpt, &sbstr));

	// If there is an explicit name in the pttp, stuOld and stuCur are both set to it; if not,
	// stuOld is set to <inherit> and stuCur is set to the actual inherited value for this run.
	if (hr == S_FALSE)
	{
		stuOld.Load(kstidInherit);
		if (pvps)
			CheckHr(pvps->get_StringProperty(tpt, &sbstr));
		//stuCur.Assign(sbstr.Chars(), sbstr.Length());
	}
	else
	{
		// Use Assign in case string contains nulls
		stuOld.Assign(sbstr.Chars(), sbstr.Length()); // From ttp.
	}

	stuCur.Assign(sbstr.Chars(), sbstr.Length()); // From ttp or vps, as the case may be.

	if (fFirst)
	{
		stuValOld = stuOld;
		stuValCur = stuCur;
	}
	else
	{
		if (stuValOld != stuOld)
			stuValOld = pszConflict;
		if (stuValCur != stuCur)
			stuValCur = pszConflict;
		if (pttp && pvps &&
			(stuValOld == pszConflict || stuValCur == pszConflict))
		{
			// Not keeping separate track of hard-formatting:
			stuValOld = pszConflict;
			stuValCur = pszConflict;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Merge the integer properties into the variables for the formatting dialog.
	Keep track of the explicit and inherited states.
----------------------------------------------------------------------------------------------*/
void AfDialog::MergeFmtDlgIntProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
	int tpv, COLORREF & clrHard, COLORREF & clrSoft, bool fFirst,
	int & xChrpi, bool fHard)
{
	int nHard = (int)clrHard;
	int nSoft = (int)clrSoft;
	int nVarHardBogus, nVarSoftBogus;
	MergeFmtDlgIntProp(pttp, pvps, tpt, tpv, nHard, nSoft, nVarHardBogus, nVarSoftBogus,
		fFirst, xChrpi, fHard, false, false);
	clrHard = (COLORREF)nHard;
	clrSoft = (COLORREF)nSoft;
}

void AfDialog::MergeFmtDlgIntProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
	int tpv, int & nHard, int & nSoft, bool fFirst,
	int & xChrpi, bool fHard, bool fInverting)
{
	int nVarHardBogus, nVarSoftBogus;
	MergeFmtDlgIntProp(pttp, pvps, tpt, tpv, nHard, nSoft, nVarHardBogus, nVarSoftBogus,
		fFirst, xChrpi, fHard, fInverting, false);
}

void AfDialog::MergeFmtDlgIntProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
	int tpv, int & nHard, int & nSoft, int & nVarHard, int & nVarSoft, bool fFirst,
	int & xChrpi, bool fHard, bool fInverting, bool fMv)
{
	Assert(fHard || pttp == NULL);

	int nCurBogus, nCurVarBogus;
	// Don't touch the inherited value if we are getting the explicit one:
	int & nCurTmp = (fHard) ? nCurBogus : nSoft;
	int & nCurTmpVar = (fHard) ? nCurVarBogus : nVarSoft;

	if (fMv)
		MergeMvIntProp(pttp, pvps, tpt, nHard, nCurTmp, nVarHard, nCurTmpVar, fFirst);
	else if (fInverting)
		MergeInvertingProp(pttp, pvps, tpt, tpv, nHard, nCurTmp, fFirst);
	else
		MergeIntProp(pttp, pvps, tpt, tpv, nHard, nCurTmp, fFirst);

	if (fHard)
	{
		if (fFirst)
		{
			// If it is anything but unspecified in the first run, it is no longer
			// inherited, the starting value, but explicit.
			if (nHard != FwStyledText::knUnspecified)
				xChrpi = kxExplicit;
		}
		else
		{
			// JT: Don't understand the following comment:
			// We can't tell the difference between a conflicting value that was a result
			// of the value being unspecified in the current run and a true conflict of hard
			// values. Check the ttp again.
			// JT: To correctly show conflicting when the first paragraph has a bullet and
			// the second has none, we needed to add the last test to the condition.
			// I'm not sure why the other two conditions are needed, though.
			// (Also changed to compare xChrpi with kxExplicit and kxInherited instead of
			// kHard and kSoft, though they have the same actual values, because xChrpi
			// is meant to be one of the kx values.)
			int nVar, nVal;
			HRESULT hr = S_FALSE;
			if (pttp)
				CheckHr(hr = pttp->GetIntPropValues(tpt, &nVar, &nVal));
			if ((xChrpi == kxExplicit && hr == S_FALSE) ||
				(xChrpi == kxInherited && hr == S_OK) ||
				nHard == knConflicting )
			{
				xChrpi = kxConflicting;
//				nSoft = FwStyledText::knConflicting; // no, remember the soft value so
													// unspecified can still use it
			}

		}
	}
}


/*----------------------------------------------------------------------------------------------
	Merge the string properties into the variables for the formatting dialog.
	Keep track of the explicit and inherited states.
----------------------------------------------------------------------------------------------*/
void AfDialog::MergeFmtDlgStrProp(ITsTextProps * pttp, IVwPropertyStore * pvps, int tpt,
	StrUni & stuHard, StrUni & stuSoft, bool fFirst, int & xChrpi, bool fHard)
{
	StrUni stuInherit = kstidInherit;

	StrUni stuBogus;
	// Don't touch the inherited value if we are getting the explicit one:
	StrUni & stuCurTmp = (fHard) ? stuBogus : stuSoft;

	MergeStringProp(pttp, pvps, tpt, stuHard, stuCurTmp, fFirst, L"");

	if (fHard)
	{
		if (fFirst)
		{
			if (stuHard != stuInherit)
				xChrpi = kxHard;
		}
		else
		{
			// We can't tell the difference between a conflicting value that was
			// a result of the value being unspecified in the current run and
			// a true conflict of hard values. Check the ttp again.
			SmartBstr sbstrTmp;
			HRESULT hr;
			CheckHr(hr = pttp->GetStrPropValue(tpt, &sbstrTmp));
			if ((xChrpi == kxHard && hr == S_FALSE) ||
				(xChrpi == kxSoft && hr == S_OK))
			{
				xChrpi = kxConflicting;
				//stuSoft = L""; // no, remember the soft value so unspecified can still use it
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Handle keyboard messages for modeless dialogs.
----------------------------------------------------------------------------------------------*/
LRESULT CALLBACK AfDialog::GetMsgProc(int code, WPARAM wp, LPARAM lp)
{
	MSG * pmsg = (MSG *)lp;
	AssertPtr(pmsg);
	Assert(s_hhook);

	// See if the currently active window is a modeless dialog.
	if (s_hwndCurModelessDlg != NULL && code >= 0 && (PM_REMOVE & wp))
	{
		// When Shift+F1 is pressed, the main application window loop converts it into
		// kcidHelpWhatsThis by translating the accelerator message. We don't want the main
		// application going into the help mode while we're currently in a modeless dialog.
		// So, when we detect the Shift+F1 key, we keep the main loop from detecting it.
		if (pmsg->message == WM_KEYDOWN && pmsg->wParam == VK_F1 && ::GetKeyState(VK_SHIFT) < 0)
			pmsg->message = WM_NULL;

		// Don't translate non-input events.
		// We need to process the WM_SYS... messages to allow accelerator keys to work
		// properly. We also need to process the tab key to change the current focus.
		// NOTE: The commented-out line processed every message between WM_KEYFIRST and
		// WM_KEYLAST. This didn't work in cases, such as the Find dialog, when an AfButton on
		// a modeless dialog opened a popup menu. Arrow keys did not work as expected in the
		// menu, but got passed to the dialog instead, which ended up changing the focus to
		// a different control. To fix this, we're now ignoring arrow keys.
		// 12/11/01 - I (DarrellZ) added the VK_RETURN so the Enter key wouldn't be ignored
		// on modeless dialogs (Raid bug 2205).
		//if (pmsg->message >= WM_KEYFIRST && pmsg->message <= WM_KEYLAST)
		if (pmsg->message == WM_SYSKEYDOWN ||
			pmsg->message == WM_SYSKEYUP ||
			pmsg->message == WM_SYSCHAR ||
			pmsg->wParam == VK_TAB ||
			pmsg->wParam == VK_RETURN ||
			pmsg->wParam == VK_ESCAPE)
		{
			// If it is a dialog message, it's already been handled, so don't pass it on to the
			// next hook procedure.
			if (::IsDialogMessage(s_hwndCurModelessDlg, pmsg))
			{
				// The value returned from this hookproc is ignored,
				// and it cannot be used to tell Windows the message has been handled.
				// To avoid further processing, convert the message to WM_NULL
				// before returning.
				pmsg->message = WM_NULL;
			}
		}
	}

	return ::CallNextHookEx(s_hhook, code, wp, lp);
}


#ifdef INCLUDE_TOOL_TIPS

/*----------------------------------------------------------------------------------------------
	Create a tooltip and add all the controls in a dialog to it.
----------------------------------------------------------------------------------------------*/
void AfDialog::InitializeToolTip(int dxpMax)
{
	Assert(!m_hhook);
	Assert(!m_hwndToolTip);

	INITCOMMONCONTROLSEX iccex = { isizeof(iccex), ICC_BAR_CLASSES };
	InitCommonControlsEx(&iccex);

	m_hwndToolTip = ::CreateWindow(TOOLTIPS_CLASS, NULL, TTS_ALWAYSTIP, 0, 0, 0, 0, m_hwnd,
		0, ModuleEntry::GetModuleHandle(), NULL);
	if (!m_hwndToolTip)
		ThrowHr(E_FAIL);

	// Enumerate the child windows to register them with the tooltip control.
	::EnumChildWindows(m_hwnd, (WNDENUMPROC)&AfDialog::EnumChildProc, (LPARAM)this);

	::SendMessage(m_hwndToolTip, TTM_SETMAXTIPWIDTH, 0, dxpMax);

	// Install a hook procedure to monitor the message stream for mouse
	// messages intended for the controls in the dialog box.
	g_qdlg = this;
	m_hhook = ::SetWindowsHookEx(WH_GETMESSAGE, &AfDialog::GetMsgProcTip, 0,
		::GetCurrentThreadId());
	if (!m_hhook)
		ThrowHr(E_FAIL);
}

/*----------------------------------------------------------------------------------------------
	Static method to enumerate all the child windows in a dialog and add them to the tooltip.
----------------------------------------------------------------------------------------------*/
BOOL AfDialog::EnumChildProc(HWND hwndCtrl, LPARAM lp)
{
	AfDialogPtr qdlg = (AfDialog *)lp;
	AssertPtr(qdlg);

	achar pszClass[64];

	// Skip static controls.
	::GetClassName(hwndCtrl, pszClass, isizeof(pszClass) / isizeof(achar));
	if (lstrcmpi(pszClass, _T("STATIC")) != 0)
	{
		TOOLINFO ti = { isizeof(ti), TTF_IDISHWND };
		ti.hwnd = qdlg->m_hwnd;
		ti.uId = (uint)hwndCtrl;
		ti.lpszText = reinterpret_cast<achar *>(::GetDlgCtrlID(hwndCtrl));
		SendMessage(qdlg->m_hwndToolTip, TTM_ADDTOOL, 0, (LPARAM)&ti);
	}
	return TRUE;
}

/*----------------------------------------------------------------------------------------------
	Static method to monitor the message stream for mouse messages intended for a control in
	the dialog and forward them to the tooltip control.
----------------------------------------------------------------------------------------------*/
LRESULT CALLBACK AfDialog::GetMsgProcTip(int nCode, WPARAM wp, LPARAM lp)
{
	AssertPtrN(g_qdlg);
	if (!g_qdlg)
		return 0;
	Assert(g_qdlg->m_hhook);

	MSG * pmsg = (MSG *)lp;
	if (nCode < 0 || !(::IsChild(g_qdlg->m_hwnd, pmsg->hwnd)))
		return (::CallNextHookEx(g_qdlg->m_hhook, nCode, wp, lp));

	switch (pmsg->message)
	{
		case WM_MOUSEMOVE:
		case WM_LBUTTONDOWN:
		case WM_LBUTTONUP:
		case WM_RBUTTONDOWN:
		case WM_RBUTTONUP:
			if (g_qdlg->m_hwndToolTip)
				::SendMessage(g_qdlg->m_hwndToolTip, TTM_RELAYEVENT, 0, (LPARAM)pmsg);
			break;
		default:
			break;
	}
	return (::CallNextHookEx(g_qdlg->m_hhook, nCode, wp, lp));
}

#endif // INCLUDE_TOOL_TIPS

/*----------------------------------------------------------------------------------------------
	Uses a mechanism similar to MFC for saving values from controls into member variables.
----------------------------------------------------------------------------------------------*/
bool AfDialog::UpdateData(bool fSave)
{
	Assert(::IsWindow(m_hwnd)); // calling UpdateData before DoModal?

	AfDataExchange adx(this, fSave);

	try
	{
		DoDataExchange(&adx);
	}
	catch (...)
	{
		return false;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Set the keyboard to the system default.
----------------------------------------------------------------------------------------------*/
void AfDialog::TurnOnDefaultKeyboard()
{
	// For comparison, we want only the LANGID portion of the HKL.
	HKL hklCurr = reinterpret_cast<HKL>(LANGIDFROMLCID(::GetKeyboardLayout(0)));
	LCID lcidDefault = AfApp::GetDefaultKeyboard();
	// For keyboard selection, we want only the LANGID portion of the LCID.
	HKL hklDefault = reinterpret_cast<HKL>(LANGIDFROMLCID(lcidDefault));
	if (hklCurr != hklDefault)
	{
#if 99
		StrAnsi sta;
		sta.Format("AfDialog::TurnOnDefaultKeyboard() -"
			" ::ActivateKeyboardLayout(%x, KLF_SETFORPROCESS);\n",
			hklDefault);
		::OutputDebugStringA(sta.Chars());
#endif
		::ActivateKeyboardLayout(hklDefault, KLF_SETFORPROCESS);
	}
}


/***********************************************************************************************
	AfDataExchange methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDataExchange::AfDataExchange(AfDialog * pdlg, bool fSave)
{
	AssertPtr(pdlg);

	m_fSave = fSave;
	m_qdlg = pdlg;
	m_hwndLastControl = NULL;
}

/*----------------------------------------------------------------------------------------------
	Call this in a user-defined DDX_ function when the control that is being modified is an
	edit box.
----------------------------------------------------------------------------------------------*/
HWND AfDataExchange::PrepareEditCtrl(int cid)
{
	HWND hwnd = PrepareCtrl(cid);
	Assert(hwnd);

	m_fEditLastControl = true;
	return hwnd;
}

/*----------------------------------------------------------------------------------------------
	Call this in a user-defined DDX_ function when the control that is being modified is not an
	edit box.
----------------------------------------------------------------------------------------------*/
HWND AfDataExchange::PrepareCtrl(int cid)
{
	Assert(cid != 0 && cid != -1);
	AssertPtr(m_qdlg);

	HWND hwnd = ::GetDlgItem(m_qdlg->Hwnd(), cid);
	Assert(hwnd);

	m_hwndLastControl = hwnd;
	m_fEditLastControl = false; // not an edit item by default
	return hwnd;
}

/*----------------------------------------------------------------------------------------------
	If a control has an invalid value, set focus to that control.
----------------------------------------------------------------------------------------------*/
void AfDataExchange::Fail()
{
	if (!m_fSave)
	{
		Warn("Warning: AfDataExchange::Fail called when not validating.\n");
		// Throw the exception anyway.
	}
	else if (m_hwndLastControl)
	{
		// Restore focus and selection to offending field.
		::SetFocus(m_hwndLastControl);
		if (m_fEditLastControl) // Select edit item.
			::SendMessage(m_hwndLastControl, EM_SETSEL, 0, -1);
	}
	else
	{
		Warn("Error: fail validation with no control to restore focus to.\n");
	}
	throw AfUserException();
}

/***********************************************************************************************
	Notes for implementing dialog data exchange and validation procs:
	* Always start with PrepareCtrl or PrepareEditCtrl.
	* Always start with 'padx->m_fSave' check.
	* padx->Fail() will throw an exception - so be prepared.
	* Validation procs should only act if 'm_fSave' is true.
	* Use the suffices:
		DDX_ = Exchange proc
		DDV_ = Validation proc
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Only supports '%d', '%u', '%sd', '%su', '%ld' and '%lu'
----------------------------------------------------------------------------------------------*/
bool AfDialog::SimpleScanf(Psz pszText, Pcsz pszFormat, va_list pData)
{
	AssertPsz(pszText);
	AssertPsz(pszFormat);
	Assert(*pszFormat == '%');
	pszFormat++; // skip '%'

	bool fLong = false;
	bool fShort = false;
	if (*pszFormat == 'l')
	{
		fLong = true;
		pszFormat++;
	}
	else if (*pszFormat == 's')
	{
		fShort = true;
		pszFormat++;
	}

	Assert(*pszFormat == 'd' || *pszFormat == 'u');
	Assert(pszFormat[1] == '\0');

	while (*pszText == ' ' || *pszText == '\t')
		pszText++;
	achar chFirst = pszText[0];
	long lT;
	if (*pszFormat == 'd')
	{
		// Signed
		lT = (int)_tcstol(pszText, &pszText, 10);
	}
	else
	{
		// Unsigned
		if (*pszText == '-')
			return false;
		lT = (unsigned int)_tcstoul(pszText, &pszText, 10);
	}
	if (lT == 0 && chFirst != '0')
		return false; // Could not convert.

	while (*pszText == ' ' || *pszText == '\t')
		pszText++;
	if (*pszText != '\0')
		return false; // not terminated properly

	if (fShort)
	{
		if ((short)lT != lT)
			return false;   // too big for short
		*va_arg(pData, short *) = (short)lT;
	}
	else
	{
		Assert(sizeof(long) == sizeof(int));
		*va_arg(pData, long *) = lT;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Only supports windows output formats - no floating point.
----------------------------------------------------------------------------------------------*/
void AfDialog::DDX_TextWithFormat(AfDataExchange * padx, int cid, LPCTSTR lpszFormat,
	UINT ridPrompt, ...)
{
	va_list pData;
	va_start(pData, ridPrompt);

	HWND hwnd = padx->PrepareEditCtrl(cid);
	achar rgchT[1024];
	if (padx->m_fSave)
	{
		// The following works for %d, %u, %ld, %lu
		::GetWindowText(hwnd, rgchT, isizeof(rgchT) / isizeof(achar));
		if (!SimpleScanf(rgchT, lpszFormat, pData))
		{
			// TODO DarrellZ: Fix this for multiple arguments.
			//StrAnsi sta(ridPrompt);
			//MessageBox(padx->m_pafd->Hwnd(), ridPrompt);
			padx->Fail(); // throws exception
		}
	}
	else
	{
		wvsprintf(rgchT, lpszFormat, pData);
		::SetWindowText(hwnd, rgchT);
	}

	va_end(pData);
}

/*----------------------------------------------------------------------------------------------
	Simple formatting to text item
----------------------------------------------------------------------------------------------*/
void AfDialog::DDX_Text(AfDataExchange * padx, int cid, BYTE & b)
{
	int nT = (int)b;

	if (padx->m_fSave)
	{
		DDX_TextWithFormat(padx, cid, _T("%u"), DDX_PARSE_BYTE, &nT);
		if (nT > 255)
		{
			// TODO DarrellZ: error handling - notify user?
			//MessageBox(padx->m_pafd->Hwnd(), DDX_PARSE_BYTE);
			padx->Fail(); // throws exception
		}
		b = (BYTE)nT;
	}
	else
	{
		DDX_TextWithFormat(padx, cid, _T("%u"), DDX_PARSE_BYTE, b);
	}
}

void AfDialog::DDX_Text(AfDataExchange * padx, int cid, short & sn)
{
	if (padx->m_fSave)
		DDX_TextWithFormat(padx, cid, _T("%sd"), DDX_PARSE_INT, &sn);
	else
		DDX_TextWithFormat(padx, cid, _T("%hd"), DDX_PARSE_INT, sn);
}

void AfDialog::DDX_Text(AfDataExchange * padx, int cid, int & n)
{
	if (padx->m_fSave)
		DDX_TextWithFormat(padx, cid, _T("%d"), DDX_PARSE_INT, &n);
	else
		DDX_TextWithFormat(padx, cid, _T("%d"), DDX_PARSE_INT, n);
}

void AfDialog::DDX_Text(AfDataExchange * padx, int cid, uint & u)
{
	if (padx->m_fSave)
		DDX_TextWithFormat(padx, cid, _T("%u"), DDX_PARSE_UINT, &u);
	else
		DDX_TextWithFormat(padx, cid, _T("%u"), DDX_PARSE_UINT, u);
}

void AfDialog::DDX_Text(AfDataExchange * padx, int cid, long & ln)
{
	if (padx->m_fSave)
		DDX_TextWithFormat(padx, cid, _T("%ld"), DDX_PARSE_INT, &ln);
	else
		DDX_TextWithFormat(padx, cid, _T("%ld"), DDX_PARSE_INT, ln);
}

void AfDialog::DDX_Text(AfDataExchange * padx, int cid, StrUni & stu)
{
	HWND hwnd = padx->PrepareEditCtrl(cid);
	if (padx->m_fSave)
	{
		int cch = ::GetWindowTextLength(hwnd);
		Psz pszT = NewObj achar[cch + 1];
		if (!pszT)
			padx->Fail();
		::GetWindowText(hwnd, pszT, cch + 1);
		try
		{
			stu = pszT;
		}
		catch (...)
		{
			delete pszT;
			padx->Fail();
		}
		delete[] pszT;
	}
	else
	{
		StrApp str = stu;
		::SetWindowText(hwnd, (Psz)str.Chars());
	}
}

// TODO DarrellZ: Uncomment this when TssEdit is completed.
#if 0
void AfDialog::DDX_Text(AfDataExchange * padx, int cid, ITsString ** pptss)
{
	AssertPtr(pptss);

	HWND hwnd = padx->PrepareCtrl(cid);
	TssEditPtr qte = AfWnd::GetAfWnd(hwnd);
	AssertObj(qte); // Make sure the window is really a TssEdit.

	if (padx->m_fSave)
	{
		Assert(!*pptss);
		qte->GetText(pptss);
	}
	else
	{
		AssertPtr(*pptss);
		qte->SetText(*pptss);
	}
}
#endif


/***********************************************************************************************
	Setting common Windows control types.
***********************************************************************************************/

void AfDialog::DDX_Check(AfDataExchange * padx, int cid, int & n)
{
	HWND hwnd = padx->PrepareCtrl(cid);
	if (padx->m_fSave)
	{
		n = (int)SendMessage(hwnd, BM_GETCHECK, 0, 0);
		Assert(n >= 0 && n <= 2);
	}
	else
	{
		if (n < 0 || n > 2)
		{
			Warn("Warning: Dialog data checkbox value out of range.\n");
			n = 0; // default to off
		}
		SendMessage(hwnd, BM_SETCHECK, (WPARAM)n, 0);
	}
}

/*----------------------------------------------------------------------------------------------
	Must be first in a group of auto radio buttons.
----------------------------------------------------------------------------------------------*/
void AfDialog::DDX_Radio(AfDataExchange * padx, int cid, int & n)
{
	HWND hwnd = padx->PrepareCtrl(cid);
	Assert(GetWindowLong(hwnd, GWL_STYLE) & WS_GROUP);
	Assert(SendMessage(hwnd, WM_GETDLGCODE, 0, 0) & DLGC_RADIOBUTTON);

	if (padx->m_fSave)
		n = -1; // value if none found

	// Walk all children in group
	int iButton = 0;
	do
	{
		if (SendMessage(hwnd, WM_GETDLGCODE, 0, 0) & DLGC_RADIOBUTTON)
		{
			// Control in group is a radio button
			if (padx->m_fSave)
			{
				if (SendMessage(hwnd, BM_GETCHECK, 0, 0) != 0)
				{
					Assert(n == -1); // Only set once.
					n = iButton;
				}
			}
			else
			{
				// Select button.
				SendMessage(hwnd, BM_SETCHECK, (iButton == n), 0);
			}
			iButton++;
		}
		else
		{
			Warn("Warning: skipping non-radio button in group.\n");
		}
		hwnd = GetWindow(hwnd, GW_HWNDNEXT);
	} while (hwnd != NULL && !(GetWindowLong(hwnd, GWL_STYLE) & WS_GROUP));
}

void AfDialog::DDX_LBIndex(AfDataExchange * padx, int cid, int & n)
{
	HWND hwnd = padx->PrepareCtrl(cid);
	if (padx->m_fSave)
		n = (int)SendMessage(hwnd, LB_GETCURSEL, 0, 0);
	else
		SendMessage(hwnd, LB_SETCURSEL, n, 0);
}

void AfDialog::DDX_CBIndex(AfDataExchange * padx, int cid, int & n)
{
	HWND hwnd = padx->PrepareCtrl(cid);
	if (padx->m_fSave)
		n = (int)SendMessage(hwnd, CB_GETCURSEL, 0, 0);
	else
		SendMessage(hwnd, CB_SETCURSEL, n, 0);
}

void AfDialog::DDX_Scroll(AfDataExchange * padx, int cid, int & n)
{
	HWND hwnd = padx->PrepareCtrl(cid);
	if (padx->m_fSave)
		n = GetScrollPos(hwnd, SB_CTL);
	else
		SetScrollPos(hwnd, SB_CTL, n, TRUE);
}

void AfDialog::DDX_Slider(AfDataExchange * padx, int cid, int & n)
{
	HWND hwnd = padx->PrepareCtrl(cid);
	if (padx->m_fSave)
		n = (int) SendMessage(hwnd, TBM_GETPOS, 0, 0);
	else
		SendMessage(hwnd, TBM_SETPOS, TRUE, n);
}


/*----------------------------------------------------------------------------------------------
	Synchronize all windows in this application with any changes made in the database.
	@param sync -> The information describing a given change.
----------------------------------------------------------------------------------------------*/
bool AfDialog::Synchronize(SyncInfo & sync)
{
	return true;
}


/***********************************************************************************************
	Range Dialog Data Validation
***********************************************************************************************/

// TODO DarrellZ: Fix all the validation stuff.
#if 0
AFX_STATIC void _AfxFailMinMaxWithFormat(AfDataExchange * padx, long minVal, long maxVal,
	LPCTSTR lpszFormat, UINT cidPrompt)
	// error string must have '%1' and '%2' strings for min and max values
	// wsprintf formatting uses long values (format should be '%ld' or '%lu')
{
	ASSERT(lpszFormat != NULL);

	if (!padx->m_fSave)
	{
		TRACE0("Warning: initial dialog data is out of range.\n");
		return;     // don't stop now
	}
	TCHAR szMin[32];
	TCHAR szMax[32];
	wsprintf(szMin, lpszFormat, minVal);
	wsprintf(szMax, lpszFormat, maxVal);
	CString prompt;
	AfxFormatString2(prompt, cidPrompt, szMin, szMax);
	AfxMessageBox(prompt, MB_ICONEXCLAMATION, cidPrompt);
	prompt.Empty(); // exception prep
	padx->Fail();
}

//NOTE: don't use overloaded function names to avoid type ambiguities
void DDV_MinMaxByte(AfDataExchange * padx, BYTE value, BYTE minVal, BYTE maxVal)
{
	ASSERT(minVal <= maxVal);
	if (value < minVal || value > maxVal)
		_AfxFailMinMaxWithFormat(padx, (long)minVal, (long)maxVal, _T("%u"),
			DDX_PARSE_INT_RANGE);
}

void DDV_MinMaxShort(AfDataExchange * padx, short value, short minVal, short maxVal)
{
	ASSERT(minVal <= maxVal);
	if (value < minVal || value > maxVal)
		_AfxFailMinMaxWithFormat(padx, (long)minVal, (long)maxVal, _T("%ld"),
			DDX_PARSE_INT_RANGE);
}

void DDV_MinMaxInt(AfDataExchange * padx, int value, int minVal, int maxVal)
{
	ASSERT(minVal <= maxVal);
	if (value < minVal || value > maxVal)
		_AfxFailMinMaxWithFormat(padx, (long)minVal, (long)maxVal, _T("%ld"),
			DDX_PARSE_INT_RANGE);
}

void DDV_MinMaxLong(AfDataExchange * padx, long value, long minVal, long maxVal)
{
	ASSERT(minVal <= maxVal);
	if (value < minVal || value > maxVal)
		_AfxFailMinMaxWithFormat(padx, (long)minVal, (long)maxVal, _T("%ld"),
			DDX_PARSE_INT_RANGE);
}

void DDV_MinMaxUInt(AfDataExchange * padx, UINT value, UINT minVal, UINT maxVal)
{
	ASSERT(minVal <= maxVal);
	if (value < minVal || value > maxVal)
		_AfxFailMinMaxWithFormat(padx, (long)minVal, (long)maxVal, _T("%lu"),
			DDX_PARSE_INT_RANGE);
}

void DDV_MinMaxDWord(AfDataExchange * padx, DWORD value, DWORD minVal, DWORD maxVal)
{
	ASSERT(minVal <= maxVal);
	if (value < minVal || value > maxVal)
		_AfxFailMinMaxWithFormat(padx, (long)minVal, (long)maxVal, _T("%lu"),
			DDX_PARSE_INT_RANGE);
}

void DDV_MinMaxSlider(AfDataExchange * padx, DWORD value, DWORD minVal, DWORD maxVal)
{
	ASSERT(minVal <= maxVal);

	if (!padx->m_fSave)
	{
		if (minVal > value || maxVal < value)
		{
#ifdef _DEBUG
			int cid = GetWindowLong(padx->m_hWndLastControl, GWL_ID);
			TRACE1("Warning: initial dialog data is out of range in control ID %d.\n", cid);
#endif
			return;     // don't stop now
		}
	}

	SendMessage(padx->m_hWndLastControl, TBM_SETRANGEMIN, FALSE, (LPARAM) minVal);
	SendMessage(padx->m_hWndLastControl, TBM_SETRANGEMIN, TRUE, (LPARAM) maxVal);
}

/////////////////////////////////////////////////////////////////////////////
// Max Chars Dialog Data Validation

void DDV_MaxChars(AfDataExchange * padx, CString const& value, int nChars)
{
	ASSERT(nChars >= 1);        // allow them something
	if (padx->m_fSave && value.GetLength() > nChars)
	{
		TCHAR szT[32];
		wsprintf(szT, _T("%d"), nChars);
		CString prompt;
		AfxFormatString1(prompt, DDX_PARSE_STRING_SIZE, szT);
		AfxMessageBox(prompt, MB_ICONEXCLAMATION, DDX_PARSE_STRING_SIZE);
		prompt.Empty(); // exception prep
		padx->Fail();
	}
	else if (padx->m_hWndLastControl != NULL && padx->m_bEditLastControl)
	{
		// limit the control max-chars automatically
		SendMessage(padx->m_hWndLastControl, EM_LIMITTEXT, nChars, 0);
	}
}

/////////////////////////////////////////////////////////////////////////////
// Special DDX_ proc for subclassing controls

void DDX_Control(AfDataExchange * padx, int cid, CWnd& rControl)
{
	if (rControl.m_hWnd == NULL)    // not subclassed yet
	{
		ASSERT(!padx->m_fSave);

		HWND hwnd = padx->PrepareCtrl(cid);

		if (!rControl.SubclassWindow(hwnd))
		{
			ASSERT(FALSE);      // possibly trying to subclass twice?
			AfxThrowNotSupportedException();
		}
#ifndef _AFX_NO_OCC_SUPPORT
		else
		{
			// If the control has reparented itself (e.g., invisible control),
			// make sure that the CWnd gets properly wired to its control site.
			if (padx->m_pDlgWnd->m_hWnd != GetParent(rControl.m_hWnd))
				rControl.AttachControlSite(padx->m_pDlgWnd);
		}
#endif //!_AFX_NO_OCC_SUPPORT

	}
}

/////////////////////////////////////////////////////////////////////////////
// Global failure dialog helpers (used by database classes)

void AfxFailMaxChars(AfDataExchange * padx, int nChars)
{
	TCHAR lpszTemp[32];
	wsprintf(lpszTemp, _T("%d"), nChars);
	CString prompt;
	AfxFormatString1(prompt, DDX_PARSE_STRING_SIZE, lpszTemp);
	AfxMessageBox(prompt, MB_ICONEXCLAMATION, DDX_PARSE_STRING_SIZE);
	prompt.Empty(); // exception prep
	padx->Fail();
}

void AfxFailRadio(AfDataExchange * padx)
{
	CString prompt;
	AfxFormatStrings(prompt, DDX_PARSE_RADIO_BUTTON, NULL, 0);
	AfxMessageBox(prompt, MB_ICONEXCLAMATION, DDX_PARSE_RADIO_BUTTON);
	prompt.Empty(); // exception prep
	padx->Fail();
}

/////////////////////////////////////////////////////////////////////////////
#endif // Validation stuff.


/***********************************************************************************************
	AfDialogShell methods.
***********************************************************************************************/

int AfDialogShell::CreateDlgShell(AfDialogView * pdlgv, Pcsz pszTitle, HWND hwndPar, void * pv)
{
	AssertPtr(pdlgv);
	Assert(pdlgv->GetResourceId());

	m_qdlgv = pdlgv;
	m_pszTitle = pszTitle;
	return DoModal(hwndPar, kridShellDlg, pv);
}

int AfDialogShell::CreateNoHelpDlgShell(AfDialogView * pdlgv, Pcsz pszTitle, HWND hwndPar,
	void * pv)
{
	AssertPtr(pdlgv);
	Assert(pdlgv->GetResourceId());

	m_qdlgv = pdlgv;
	m_pszTitle = pszTitle;
	return DoModal(hwndPar, kridNoHelpShellDlg, pv);
}


/*----------------------------------------------------------------------------------------------
	Initialize the dialog.
----------------------------------------------------------------------------------------------*/
bool AfDialogShell::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	::SetWindowText(m_hwnd, m_pszTitle);
	m_qdlgv->DoModeless(m_hwnd);
	::ShowWindow(m_qdlgv->Hwnd(), SW_SHOW);

	// Get dimensions of the child dialog.
	Rect rcChild;
	::GetWindowRect(m_qdlgv->Hwnd(), &rcChild);
	Rect rcButton;
	::GetWindowRect(::GetDlgItem(m_hwnd, IDOK), &rcButton);
	Rect rcDialog;
	::GetWindowRect(m_hwnd, &rcDialog);

	const int kdzsDialogUnits = 7; // This is in dialog units.
	Rect rcT(kdzsDialogUnits);
	MapDialogRect(m_hwnd, &rcT);
	const int kdzs = rcT.left;

	// Find the width and height of the shell dialog.
	int dxs = rcChild.Width();
	int dys = rcChild.Height() + (rcChild.top - rcDialog.top) + rcButton.Height() + kdzs;

	// Move the OK, Cancel, and (optional) Help buttons to their proper places.
	int ysCancel = rcChild.Height();
	int xsHelp = dxs - kdzs - rcButton.Width();
	HWND hwndHelp = ::GetDlgItem(m_hwnd, kctidHelp);
	if (hwndHelp != NULL)
	{
		::MoveWindow(hwndHelp, xsHelp, ysCancel, rcButton.Width(),
			rcButton.Height(), true);
	}
	int xsCancel = xsHelp - kdzs - rcButton.Width();
	::MoveWindow(::GetDlgItem(m_hwnd, kctidCancel), xsCancel, ysCancel, rcButton.Width(),
		rcButton.Height(), true);
	int xsOk = xsCancel - kdzs - rcButton.Width();
	::MoveWindow(::GetDlgItem(m_hwnd, kctidOk), xsOk, ysCancel, rcButton.Width(),
		rcButton.Height(), true);

	// Subclass the Help button if it exists.
	if (hwndHelp != NULL)
	{
		AfButtonPtr qbtn;
		qbtn.Create();
		qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);
	}

	// Resize the shell dialog.
	::SetWindowPos(m_hwnd, NULL, 0, 0, dxs + 2, dys, SWP_NOZORDER | SWP_NOMOVE);

	if (!m_qdlgv->SetActive())
		return AfDialog::OnCancel();

	return AfDialog::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	The OK button was pushed.
----------------------------------------------------------------------------------------------*/
bool AfDialogShell::OnApply(bool fClose)
{
	AssertObj(m_qdlgv);

	if (m_qdlgv->QueryClose(AfDialogView::kqctOk) && m_qdlgv->Apply())
		return AfDialog::OnApply(fClose);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Pass the message on to the current sub dialog.
----------------------------------------------------------------------------------------------*/
bool AfDialogShell::OnHelp()
{
	AssertPtr(m_qdlgv);
	m_qdlgv->Help();
	return true;
}

/*----------------------------------------------------------------------------------------------
	The Cancel button was pushed.
----------------------------------------------------------------------------------------------*/
bool AfDialogShell::OnCancel()
{
	AssertObj(m_qdlgv);

	if (m_qdlgv->QueryClose(AfDialogView::kqctCancel))
		return AfDialog::OnCancel();
	return true;
}


/***********************************************************************************************
	HelpAboutDlg methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
HelpAboutDlg::HelpAboutDlg()
{
	m_rid = kridHelpAboutDlg;
	m_hfontAppName = NULL;
	m_hfontSuiteName = NULL;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
HelpAboutDlg::~HelpAboutDlg()
{
	if (m_hfontAppName)
	{
		AfGdi::DeleteObjectFont(m_hfontAppName);
		m_hfontAppName = NULL;
	}

	if (m_hfontSuiteName)
	{
		AfGdi::DeleteObjectFont(m_hfontSuiteName);
		m_hfontSuiteName = NULL;
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the controls on the dialog.
----------------------------------------------------------------------------------------------*/
bool HelpAboutDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	StrApp strAppName(kstidAppName);
	achar * pszFilename = const_cast<achar *>(ModuleEntry::GetModulePathName());

	// Set the caption of the dialog.
	StrApp strT(kstidHelpAbout);
	strT.Append(" SIL FieldWorks ");
	strT += strAppName;
	::SetWindowText(m_hwnd, strT.Chars());

	// FieldWorks title appears above the name of the application
	HWND hwndSuiteName = ::GetDlgItem(m_hwnd, kctidSuiteName);
	Assert(NULL != hwndSuiteName);
	Assert(m_hfontSuiteName == NULL);
	m_hfontSuiteName = AfGdi::CreateFont(20, 0, 0, 0, FW_REGULAR, 0, 0, 0, ANSI_CHARSET,
		OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, 0, DEFAULT_PITCH , _T("Arial"));
	::SendMessage(hwndSuiteName, WM_SETFONT, (WPARAM)m_hfontSuiteName, 0);

	// Set the name of the application.
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidName);
	Assert(NULL != hwnd);

	::SetWindowText(hwnd, strAppName.Chars());

	Assert(m_hfontAppName == NULL);
	m_hfontAppName = AfGdi::CreateFont(34, 0, 0, 0, FW_SEMIBOLD, 0, 0, 0, ANSI_CHARSET,
		OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, 0, DEFAULT_PITCH , _T("Arial"));
	::SendMessage(hwnd, WM_SETFONT, (WPARAM)m_hfontAppName, 0);

	// Set the memory information.
	MEMORYSTATUS ms;
	::GlobalMemoryStatus(&ms);
	StrApp strF(kstidDiskSpace);
	strT.Format(strF.Chars(), ms.dwAvailPhys / 1024, ms.dwTotalPhys / 1024);
	::SetWindowText(::GetDlgItem(m_hwnd, kctidMemory), strT.Chars());

	// Set the available disk space information.
	achar szRoot[4] = { pszFilename[0], pszFilename[1], '\\', 0 };
	DWORD cbKbFree = 0;

	if (0 == cbKbFree)
	{
		// There was a problem with GetDiskFreeSpaceEx, so use GetDiskFreeSpace instead.
		DWORD cSectorsPerCluster, cBytesPerSector, cFreeClusters, cTotalClusters;
		::GetDiskFreeSpace(szRoot, &cSectorsPerCluster, &cBytesPerSector, &cFreeClusters,
			&cTotalClusters);
		cbKbFree = (DWORD)(((int64)cFreeClusters * cSectorsPerCluster * cBytesPerSector) >> 10);
	}

	strF.Load(kstidFreeSpace);
	strT.Format(strF.Chars(), cbKbFree, szRoot);
	::SetWindowText(::GetDlgItem(m_hwnd, kctidDiskSpace), strT.Chars());

	// Set the version information.
	DWORD dwT;
	DWORD cbT = ::GetFileVersionInfoSize(pszFilename, &dwT);
	Vector<BYTE> vbVerInfo;
	vbVerInfo.Resize(cbT);
	if (::GetFileVersionInfo(pszFilename, 0, cbT, vbVerInfo.Begin()))
	{
		VS_FIXEDFILEINFO * pffi;
		uint nT;
		::VerQueryValue(vbVerInfo.Begin(), _T("\\"), (void **)&pffi, &nT);

		// See FILEVERSION in the application's .rc file for info on the file version.
		strF.Load(kstidAboutVersion);
		strT.Format(strF.Chars(), HIWORD(pffi->dwFileVersionMS),
			LOWORD(pffi->dwFileVersionMS), HIWORD(pffi->dwFileVersionLS),
			LOWORD(pffi->dwFileVersionLS));

#ifdef DEBUG
		strT.Append(_T(" (Debug version)"));
#endif

		::SetWindowText(::GetDlgItem(m_hwnd, kctidVersion), strT.Chars());
	}

	// Set the Registration Number.
	// No longer being used, first comment out, future remove completely.
	//HKEY hkey;
	//if (::RegOpenKeyEx(HKEY_LOCAL_MACHINE, _T("SOFTWARE\\SIL\\FieldWorks"), 0, KEY_READ,
	//		&hkey) == ERROR_SUCCESS)
	//{
	//	DWORD dwT;
	//	achar rgch[20];
	//	DWORD cb = isizeof(rgch);
	//	LONG lRet = ::RegQueryValueEx(hkey, _T("FwUserReg"), NULL, &dwT, (BYTE *)rgch, &cb);
	//	StrApp strRegNum;
	//	if (lRet == ERROR_SUCCESS)
	//	{
	//		Assert(dwT == REG_SZ);
	//		strRegNum.Assign(rgch);
	//	}
	//	else if (lRet == ERROR_MORE_DATA)
	//	{
	//		Vector<achar> vch;
	//		vch.Resize((cb / isizeof(achar)) + 1);
	//		cb = vch.Size() * isizeof(achar);
	//		lRet = ::RegQueryValueEx(hkey, _T("FwUserReg"), NULL, &dwT,
	//			(BYTE *)vch.Begin(), &cb);
	//		if (lRet == ERROR_SUCCESS)
	//		{
	//			Assert(dwT == REG_SZ);
	//			strRegNum.Assign(vch.Begin());
	//		}
	//	}
	//	if (strRegNum.Length())
	//		::SetWindowText(::GetDlgItem(m_hwnd, kctidRegistrationNumber), strRegNum.Chars());
	//	::RegCloseKey(hkey);
	//}

	// Set the expiration date (if any)
	SilTime stimDD = AfApp::Papp()->DropDeadDate();
	if (stimDD.Year() > 2999)
	{
		// Treat as no expiration
		::SetWindowText(::GetDlgItem(m_hwnd, kctidExpiration), NULL);
	}
	else
	{
		achar rgchBuf[MAX_PATH];
		::GetWindowText(::GetDlgItem(m_hwnd, kctidExpiration), rgchBuf, MAX_PATH);
		StrApp strExpire(rgchBuf);
		strExpire.FormatAppend(_T(" %d/%d/%d"), stimDD.Month(), stimDD.Date(), stimDD.Year());
		::SetWindowText(::GetDlgItem(m_hwnd, kctidExpiration), strExpire.Chars());
	}

	return true;
}


/***********************************************************************************************
	AfButton methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfButton::AfButton()
{
	m_himl = NULL;
	m_fimlCreated = false;
	m_imag = 0;
	m_fPopupMenu = false;
	m_widDef = 0;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfButton::~AfButton()
{
	if (m_fimlCreated && m_himl)
	{
		AfGdi::ImageList_Destroy(m_himl);
		m_himl = NULL;
	}
}

/*----------------------------------------------------------------------------------------------
	This "subclasses" an existing button control. It actually destroys the old button and
	creates a new one with the owner draw style applied to it. It copies the style, position,
	and z-order of the original button.
----------------------------------------------------------------------------------------------*/
void AfButton::SubclassButton(HWND hwndDlg, int wid, BtnType btntyp, HIMAGELIST himl, int imag)
{
	HWND hwndT = ::GetDlgItem(hwndDlg, wid);
	Rect rc;
	::GetWindowRect(hwndT, &rc);
	::MapWindowPoints(NULL, hwndDlg, (POINT *)&rc, 2);

	achar rgch[50];
	::GetDlgItemText(hwndDlg, wid, rgch, isizeof(rgch) / isizeof(achar));

	HWND hwndPrev = ::GetWindow(hwndT, GW_HWNDPREV);
	DWORD dwStyle = ::GetWindowLong(hwndT, GWL_STYLE);
	::DestroyWindow(hwndT);
	Create(hwndDlg, wid, rgch, btntyp, himl, imag, dwStyle);

	::SetDlgItemText(hwndDlg, wid, rgch);
	::SetWindowPos(m_hwnd, hwndPrev, rc.left, rc.top, rc.Width(), rc.Height(), 0);
}

/*----------------------------------------------------------------------------------------------
	Create a button window.
----------------------------------------------------------------------------------------------*/
void AfButton::Create(HWND hwndPar, int wid, const achar * pszCaption, BtnType btntyp,
	HIMAGELIST himl, int imag, DWORD dwStyle)
{
	AssertPsz(pszCaption);
	m_btntyp = btntyp;

	static bool s_fRegistered = false;
	if (!s_fRegistered)
	{
		WNDCLASS wc;
		if (!::GetClassInfo(NULL, _T("BUTTON"), &wc))
			ThrowHr(WarnHr(E_FAIL));
		wc.lpszClassName = _T("AfButton");
		wc.style &= ~CS_DBLCLKS;
		::RegisterClass(&wc);
		s_fRegistered = true;
	}

	HWND hwnd = ::CreateWindow(_T("AfButton"), _T(""),
		dwStyle | BS_OWNERDRAW | BS_PUSHBUTTON | BS_TEXT, 0, 0, 0, 0, hwndPar, (HMENU)wid,
		NULL, NULL);
	SubclassHwnd(hwnd);
	Assert(m_hwnd == hwnd);

	HBITMAP hbmp;

	switch (btntyp)
	{
	default:
		Assert(false); // This should never happen.
		return;
	case kbtImage:
		if (m_fimlCreated && m_himl)
			AfGdi::ImageList_Destroy(m_himl);
		m_fimlCreated = false;
		m_himl = himl;
		m_imag = imag;
		m_fPopupMenu = false;
		break;
	case kbtHelp:
	case kbtFont:
		if (!m_fimlCreated || !m_himl)
		{
			m_himl = AfGdi::ImageList_Create(16, 15, ILC_COLORDDB | ILC_MASK, 0, 0);
			if (m_himl)
				m_fimlCreated = true;
		}
		if (!m_himl)
			ThrowHr(WarnHr(E_FAIL));
		hbmp = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(), MAKEINTRESOURCE(kridStdBtns));
		if (!hbmp)
			ThrowHr(WarnHr(E_FAIL));
		if (::ImageList_AddMasked(m_himl, hbmp, kclrPink) == -1)
			ThrowHr(WarnHr(E_FAIL));
		AfGdi::DeleteObjectBitmap(hbmp);

		m_imag = btntyp == kbtHelp ? 0 : 1;
		m_fPopupMenu = false;
		break;
	case kbtPopMenu:
	case kbtMore:
	case kbtLess:
		m_fPopupMenu = true;
		break;
	}
	m_strCaption = pszCaption;
}

/*----------------------------------------------------------------------------------------------
	Change the arrow type. Only a switch between more and less is valid
----------------------------------------------------------------------------------------------*/
void AfButton::SetArrowType(BtnType btntyp)
{
	Assert(m_fPopupMenu);
	Assert(btntyp == kbtMore || btntyp == kbtLess);
	m_btntyp = btntyp;
}

/*----------------------------------------------------------------------------------------------
	The button is owner-draw, so this method draws the button.
----------------------------------------------------------------------------------------------*/
bool AfButton::OnDrawThisItem(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);

	// DavidO: Will need this when drawing theme-enabled button.
	//bool fThemed = (IsThemeActive() && IsAppThemed());

	const int kdzpBorder = 4;
	int dzpBorder = kdzpBorder;

	// Create a memory DC and initialize it.
	Rect rc(pdis->rcItem);
	HDC hdcMem = AfGdi::CreateCompatibleDC(pdis->hDC);
	int dxp = rc.Width();
	int dyp = rc.Height();
	HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(pdis->hDC, dxp, dyp);
	HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);

	// DavidO: Will need this when drawing theme-enabled button.
	//HTHEME htheme;
	//int nState = 1;
	//if (fThemed)
	//{
	//	if (m_widDef != 0)
	//		nState = 5;
	//	else if (pdis->itemState & ODS_SELECTED)
	//		nState = 3;
	//	else if (pdis->itemState & ODS_DISABLED)
	//		nState = 4;

	//	htheme = OpenThemeData(m_hwnd, L"BUTTON");
	//	DrawThemeParentBackground(m_hwnd, hdcMem, &pdis->rcItem);
	//	DrawThemeBackground(htheme, hdcMem, 1, nState, &pdis->rcItem, NULL);
	//}
	//else
	{
		AfGfx::SetBkColor(hdcMem, ::GetSysColor(COLOR_3DFACE));
		AfGfx::FillSolidRect(hdcMem, rc, ::GetSysColor(COLOR_3DFACE));

		// Draw the border.
		Rect rcFrame(rc);
		if (m_widDef != 0) // Cannot use ODS_DEFAULT for owner-draw controls.
		{
			::FrameRect(hdcMem, &rcFrame, (HBRUSH)::GetStockObject(BLACK_BRUSH));
			rcFrame.Inflate(-1, -1);
		}
		if (pdis->itemState & ODS_SELECTED)
			dzpBorder++;
		if (m_widDef != 0 && (pdis->itemState & ODS_SELECTED))
			::FrameRect(hdcMem, &rcFrame, (HBRUSH)::GetStockObject(DKGRAY_BRUSH));
		else
			::DrawFrameControl(hdcMem, &rcFrame, DFC_BUTTON, DFCS_BUTTONPUSH);
	}

	// Draw the icon if there is one.
	Rect rcText(rc);
	rcText.left += kdzpBorder * 2;
	if (m_himl)
	{
		if (pdis->itemState & ODS_DISABLED)
		{
			HICON hicon = ImageList_GetIcon(m_himl, m_imag, ILD_NORMAL);
			BOOL fSuccess = ::DrawState(hdcMem, NULL, NULL, (LPARAM)hicon, 0, dzpBorder,
				dzpBorder, 0, 0, DST_ICON | DSS_DISABLED);
			if (!fSuccess)
			{
				Assert(fSuccess);
			}
			::DestroyIcon(hicon);
		}
		else
		{
			ImageList_Draw(m_himl, m_imag, hdcMem, dzpBorder, dzpBorder, ILD_TRANSPARENT);
		}
		rcText.left += 16;
	}

	// Draw the menu (or similar) arrow if there is one.
	if (m_fPopupMenu)
	{
		HPEN hpen = 0;
		HPEN hpenOld = 0;
		if (pdis->itemState & ODS_DISABLED)
		{
			hpen = ::CreatePen(PS_SOLID, 0, ::GetSysColor(COLOR_GRAYTEXT));
			hpenOld = (HPEN)::SelectObject(hdcMem, hpen);
		}

		// Initially generate a position that is the top left of the popup arrow (4 high, 7 wide).
		int xpArrow = rc.right - kdzpBorder - 9;
		int ypArrow = (rc.top + (rc.bottom - rc.top) / 2) - 2; // Menu arrow is 4 lines high.
		// Initially set a dx that makes it get narrower each time, starting at 7 for 4 lines.
		int dxStep = 1;
		int dxLine = 7;
		int cline = 4;
		if (m_btntyp == kbtMore)
		{
			// We draw two down triangles, each 3 high, 5 wide; overal height is six.
			// So, draw it one pixel right, one higher
			xpArrow ++;
			ypArrow --;
			cline = 6;
			dxLine = 5;
		}
		else if (m_btntyp == kbtLess)
		{
			xpArrow += 3; // First draw one-pixel line at top
			ypArrow --; // double arrow
			dxStep = -1;  // gets wider
			dxLine = 1; // first line is one pixel
			cline = 6;
		}
		rcText.right = xpArrow - kdzpBorder;

		if (pdis->itemState & ODS_SELECTED)
		{
			xpArrow++;
			ypArrow++;
		}
		for (int line = 0; line < cline; line++)
		{
			::MoveToEx(hdcMem, xpArrow, ypArrow, NULL);
			::LineTo(hdcMem, xpArrow + dxLine, ypArrow);
			xpArrow += dxStep;
			dxLine -= dxStep + dxStep;
			ypArrow++;
			if (line == 2)
			{
				switch (m_btntyp)
				{
				case kbtPopMenu: // do nothing
					break;
				case kbtMore:
					// Back to full width
					xpArrow -= 3;
					dxLine = 5;
					break;
				case kbtLess:
					// Back to one pixel
					xpArrow += 3;
					dxLine = 1;
					break;
				default:
					Assert(false);
					break;
				}
			}
		}
		if (hpenOld)
			::SelectObject(hdcMem, hpenOld);
		if (hpen)
			::DeleteObject(hpen);
	}

	// Draw the text.
	{
		HFONT hfontOld = AfGdi::SelectObjectFont(hdcMem, ::GetStockObject(DEFAULT_GUI_FONT));
		DWORD dtFlag = DT_SINGLELINE | DT_VCENTER | DT_END_ELLIPSIS;
		if (::SendMessage(m_hwnd, WM_QUERYUISTATE, 0, 0) & UISF_HIDEACCEL)
			dtFlag |= DT_HIDEPREFIX;

		StrApp strCaption;
		GetCaption(hdcMem, rcText, strCaption);

		if (pdis->itemState & ODS_DISABLED)
		{
			rcText.Offset(1, 1);
			::SetBkMode(hdcMem, TRANSPARENT);
			::SetTextColor(hdcMem, ::GetSysColor(COLOR_3DHILIGHT));
			::DrawText(hdcMem, strCaption.Chars(), strCaption.Length(), &rcText, dtFlag);
			rcText.Offset(-1, -1);
			::SetTextColor(hdcMem, ::GetSysColor(COLOR_GRAYTEXT));
		}
		else if (pdis->itemState & ODS_SELECTED)
		{
			rcText.Offset(1, 1);
		}
		::DrawText(hdcMem, strCaption.Chars(), strCaption.Length(), &rcText, dtFlag);
		AfGdi::SelectObjectFont(hdcMem, hfontOld, AfGdi::OLD);
	}

	if (pdis->itemState & ODS_FOCUS)
	{
		Rect rcT(pdis->rcItem);
		rcT.Inflate(-4, -4);
		::DrawFocusRect(hdcMem, &rcT);
	}

	// TODO:  TimP
	//HFONT hfontOld2 = (HFONT)::GetCurrentObject(hdcMem, OBJ_FONT);
	// The code below is sometimes selecting a non-default font.

	// Copy the image from memory onto the screen.
	::BitBlt(pdis->hDC, 0, 0, dxp, dyp, hdcMem, 0, 0, SRCCOPY);

	// The code above is sometimes selecting a non-default font.
	//HFONT hfontNew2 = AfGdi::SelectObjectFont(hdcMem, hfontOld2, AfGdi::CLUDGE_OLD);
	//if (hfontOld2 != hfontNew2)
	//{
	//	BOOL fSuccess;
	//	fSuccess = AfGdi::DeleteObjectFont(hfontNew2);
	//	Assert(fSuccess);
	//}

	// Clean up.
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

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfButton::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AfDialog * pdlg;
	switch (wm)
	{
	case WM_MOVE:
	case WM_SIZE:
		::InvalidateRect(m_hwnd, NULL, false);
		::UpdateWindow(m_hwnd);
		break;

	case WM_ERASEBKGND:
		// The painting takes care of this.
		return true;

	case WM_KEYDOWN:
	case WM_SYSKEYDOWN:
		if (m_fPopupMenu && wp == VK_DOWN)
		{
			::PostMessage(::GetParent(m_hwnd), WM_COMMAND,
				MAKEWPARAM(::GetWindowLong(m_hwnd, GWL_ID), BN_CLICKED), (LPARAM)m_hwnd);
		}
		break;

	case WM_LBUTTONDOWN:
		if (m_fPopupMenu)
		{
			// We want the popup menu to show as soon as the user clicks on the button,
			// not when the user lets go of the mouse button.
			lnRet = DefWndProc(WM_LBUTTONDOWN, wp, lp);
			::ReleaseCapture();
			::PostMessage(::GetParent(m_hwnd), WM_COMMAND,
				MAKEWPARAM(::GetWindowLong(m_hwnd, GWL_ID), BN_CLICKED), (LPARAM)m_hwnd);
			return true;
		}
		break;

	case WM_LBUTTONUP:
		if (m_fPopupMenu)
		{
			// We don't want to get two WM_COMMAND messages. We already sent one up above in
			// the handler for WM_LBUTTONDOWN.
			::ReleaseCapture();
			return true;
		}
		break;

	case WM_GETTEXT:
		{	// Return the text of the button. This is needed for keyboard accelerators to work.
			achar * psz = reinterpret_cast<achar *>(lp);
			AssertArray(psz, wp);
			StrCpyN(psz, m_strCaption.Chars(), wp);
			lnRet = m_strCaption.Length();
		}
		return true;

	case WM_SETTEXT:
		{	// Set the text of the button.
			achar * psz = reinterpret_cast<achar *>(lp);
			AssertPsz(psz);
			m_strCaption = psz;
			::InvalidateRect(m_hwnd, NULL, true);
		}
		break;

	case WM_UPDATEUISTATE:
		// This is used in Windows 2000 to show/hide the underscore on the accelerator
		// character used for the button. When this message gets sent, we need to redraw
		// the button.
		::InvalidateRect(m_hwnd, NULL, false);
		break;

	// Windows does not ordinarily allow Owner-drawn buttons to be the default,
	// i.e. BS_OWNERDRAW must not be combined with any other styleslike BS_DEFPUSHBUTTON.
	// In addition to this, changing the default button by sending a DM_SETDEFID message
	// does not guarantee that the default style of the normal default button is removed,
	// thus leaving the possibility of showing two buttons as default at the same time.
	// Hence the following code, and below in case WM_KILLFOCUS.
	case WM_SETFOCUS:
		m_widDef = 0;
		pdlg = dynamic_cast<AfDialog *>(AfWnd::GetAfWnd(::GetParent(m_hwnd)));
		if (pdlg)
		{	// If the parent window is a dialog. (It may not be).
			AfDialog * pdlgTop = pdlg->GetTopDialog();	// Immediate parent may be nested.
			AssertPtr(pdlgTop);
			m_widDef = ::SendMessage(pdlgTop->Hwnd(), DM_GETDEFID, 0, 0);
			m_widDef &= 0xFFFF;	// Lop off DC_HASDEFID in high word.
			::SendMessage(pdlg->Hwnd(), DM_SETDEFID, ::GetDlgCtrlID(m_hwnd), 0);
			if (m_widDef)	// i.e. if there had been a default button (assuming id != 0).
			{
				// Set the style of the normal default button to be "not default".
				// Note that this must be done AFTER the DM_SETDEFID message has been sent.
				::SendMessage(::GetDlgItem(pdlgTop->Hwnd(), m_widDef), BM_SETSTYLE,
					BS_PUSHBUTTON, true);
			}
		}
		break;

	case WM_KILLFOCUS:
		pdlg = dynamic_cast<AfDialog *>(AfWnd::GetAfWnd(::GetParent(m_hwnd)));
		if (pdlg)
		{
			if (m_widDef)	// Assume button id can't be 0.
			{
				AfDialog * pdlgTop = pdlg->GetTopDialog();	// Immediate parent may be nested.
				AssertPtr(pdlgTop);
				::SendMessage(pdlgTop->Hwnd(), DM_SETDEFID, m_widDef, 0); // Reset default.
			}
			if (m_fPopupMenu)
			{
				// If we were showing a popup menu, and the button lost focus for whatever
				// reason, we want to hide the popup menu.
				::SendMessage(pdlg->Hwnd(), WM_CANCELMODE, 0, 0);
			}
		}
		m_widDef = 0;
		break;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/***********************************************************************************************
	AfStaticText methods.
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Static method to subclass the static text item BEFORE wid in the dialog.
----------------------------------------------------------------------------------------------*/
AfStaticText::AfStaticText(HWND hwndDlg, int wid)
{
	m_hwndDlg = hwndDlg;

	// The rest of the constructor code will build a string containing the static text
	// control's text without the ampersand mnemnonic.
	HWND hwndItem = ::GetWindow(::GetDlgItem(hwndDlg, wid), GW_HWNDPREV);
	TCHAR text[300];
	::GetWindowText(hwndItem, text, sizeof(text));

	// Find the ampersand in the string.
	// REVIEW: Do we need to check the case of two ampersands in a row?
	TCHAR *ampersand = wcschr(text, '&');
	if (ampersand)
	{
		*ampersand = 0;
		wcscpy_s(m_nonMnemnonicText, text);
		wcscat_s(m_nonMnemnonicText, ++ampersand);

		// For some reason, when visual styles are enabled and the text is requested from
		// (via a WM_GETTEXT message), returning a string that's shorter than the length
		// of the original string causes tiny blocks to be added up to the length of the
		// original string. Therefore, we add a space at the end of the string to replace
		// the removed ampersand.
		wcscat_s(m_nonMnemnonicText, L" ");
	}
}

/*----------------------------------------------------------------------------------------------
	Static method to subclass the static text item BEFORE wid in the dialog.
----------------------------------------------------------------------------------------------*/
void AfStaticText::FixEnabling(HWND hwndDlg, int wid)
{
	AfStaticTextPtr qast;
	qast.Attach(NewObj AfStaticText(hwndDlg, wid));
	HWND hwndItem = ::GetWindow(::GetDlgItem(hwndDlg, wid), GW_HWNDPREV);
	qast->SubclassHwnd(hwndItem);
}

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfStaticText::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_GETTEXT:
		// See whether the next control is enabled. If not return a non
		// mnemonic version of the text.
		HWND hwndNext = ::GetWindow(m_hwnd, GW_HWNDNEXT);
		if (hwndNext && !::IsWindowEnabled(hwndNext))
		{
			// Return a string sans the ampersand so there is no mnemnonic to activate
			// some other control. The old way of doing this was to just return an empty
			// string but that really causes problems when Windows XP visual styles are
			// enabled.
			achar * pch = reinterpret_cast<achar *>(lp);
			AssertArray(pch, 1);

			// I'm know there is are more FW ways of dealing with strings, but I'm not
			// familiar enough with them, nor to I have the motivation to dig up the
			// information to make it work. I know how to use the basic C functions,
			// and they work.
			if (wp >= wcslen(m_nonMnemnonicText) + 1)
				wcscpy_s(pch, wp, m_nonMnemnonicText);
			else
			{
				wmemset(pch, 0, wp);
				wcsncpy_s(pch, wp, m_nonMnemnonicText, wp - 1);
			}

			lnRet = wcslen(pch);
			return true;
		}

		// Otherwise, and for all other messages, fall through to default behavior.
		break;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}
