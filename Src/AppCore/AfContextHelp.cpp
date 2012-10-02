/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfContextHelp.cpp
Responsibility: Darrell Zook
Last reviewed:

Description:
	Provides context-sensitive help for the application. When the user clicks on the question
	mark icon in a dialog box or the main window, he gets a special mouse cursor. When the
	user clicks the mouse again, a small popup balloon help window will be displayed, which
	gives information about the control or window the user clicked on. The user can then click
	or type any key, and this balloon window disappears.

	This file contains class definitions for the following classes:
		ContextHelpVc : VwBaseVc - This is the view constructor for the other two classes.
		ToolTipVc : VwBaseVc - This is the view constructor for the other two classes.
		AfContextHelpWnd : AfVwWnd - This is the popup window that contains the help text.
		AfToolTipWnd : AfVwWnd - This is the popup window that contains the tooltip text.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


/***********************************************************************************************
	ContextHelpVc methods.
***********************************************************************************************/

static DummyFactory g_factContextHelp(_T("SIL.AppCore.ContextHelpVc"));

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.
	The help window only consists of a single TsString.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ContextHelpVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);
	Assert(frag == ContextHelpVc::kfrCaption);

	HWND hwndDesk = ::GetDesktopWindow();
	HDC hdc = ::GetDC(hwndDesk);
	int ypLogPixels = ::GetDeviceCaps(hdc, LOGPIXELSY);
	if (!ypLogPixels)
		ypLogPixels = 96;
	::ReleaseDC(hwndDesk, hdc);

	// Get font name and font size information.
	HFONT hfont = (HFONT)::GetStockObject(DEFAULT_GUI_FONT);
	LOGFONT lf;
	if (!::GetObject(hfont, isizeof(lf), &lf))
		ThrowHrEx(E_FAIL);
	StrUni stuFont(lf.lfFaceName);
	int dympFont = -MulDiv(lf.lfHeight, kdzmpInch, ypLogPixels);
	CheckHr(pvwenv->put_StringProperty(ktptFontFamily, stuFont.Bstr()));
	CheckHr(pvwenv->put_IntProperty(ktptFontSize, ktpvMilliPoint, dympFont));

	// Set properties to make the view window act like a tooltip.
	CheckHr(pvwenv->put_IntProperty(ktptParaColor, ktpvDefault,
		::GetSysColor(COLOR_INFOBK)));
	CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));

	// The margins are for spacing around the text. The right and bottom edges also
	// have extra space for the shading.
	int dzmp = AfContextHelpWnd::kdzpMargin * kdzmpInch / ypLogPixels;
	CheckHr(pvwenv->put_IntProperty(kspMarginLeading, ktpvMilliPoint, dzmp));
	CheckHr(pvwenv->put_IntProperty(ktptMarginTop, ktpvMilliPoint, dzmp));
	dzmp += AfContextHelpWnd::kdzpShadow * kdzmpInch / ypLogPixels;
	CheckHr(pvwenv->put_IntProperty(kspMarginTrailing, ktpvMilliPoint, dzmp));
	CheckHr(pvwenv->put_IntProperty(kspMarginBottom, ktpvMilliPoint, dzmp));

	// Everything is set up now... Add the string.
	CheckHr(pvwenv->OpenParagraph());
	CheckHr(pvwenv->OpenInnerPile());
	CheckHr(pvwenv->OpenParagraph());
	CheckHr(pvwenv->AddStringProp(ContextHelpVc::kflidCaption, this));
	CheckHr(pvwenv->CloseParagraph());
	CheckHr(pvwenv->CloseInnerPile());
	CheckHr(pvwenv->CloseParagraph());

	END_COM_METHOD(g_factContextHelp, IID_IVwViewConstructor);
}


/***********************************************************************************************
	ToolTipVc methods.
***********************************************************************************************/

static DummyFactory g_factToolTip(_T("SIL.AppCore.ToolTipVc"));

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.
	The help window only consists of a single TsString.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP ToolTipVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);
	Assert(frag == ToolTipVc::kfrCaption);

	HWND hwndDesk = ::GetDesktopWindow();
	HDC hdc = ::GetDC(hwndDesk);
	int ypLogPixels = ::GetDeviceCaps(hdc, LOGPIXELSY);
	if (!ypLogPixels)
		ypLogPixels = 96;
	::ReleaseDC(hwndDesk, hdc);

	// Set properties to make the view window act like a tooltip.
	CheckHr(pvwenv->put_IntProperty(ktptParaColor, ktpvDefault,
		::GetSysColor(COLOR_INFOBK)));
	CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));

	// The margins are for spacing around the text. The right and bottom edges also
	// have extra space for the shading.
	int dzmp = AfToolTipWnd::kdxpMargin * kdzmpInch / ypLogPixels;
	CheckHr(pvwenv->put_IntProperty(kspMarginLeading, ktpvMilliPoint, dzmp));

	// Everything is set up now... Add the string.
	CheckHr(pvwenv->OpenParagraph());
	CheckHr(pvwenv->OpenInnerPile());
	CheckHr(pvwenv->OpenParagraph());
	CheckHr(pvwenv->AddStringProp(ToolTipVc::kflidCaption, this));
	CheckHr(pvwenv->CloseParagraph());
	CheckHr(pvwenv->CloseInnerPile());
	CheckHr(pvwenv->CloseParagraph());

	END_COM_METHOD(g_factToolTip, IID_IVwViewConstructor);
}


/***********************************************************************************************
	AfContextHelpWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfContextHelpWnd::AfContextHelpWnd()
{
	m_hwndParent = NULL;
	m_fSetCapture = false;
}


/*----------------------------------------------------------------------------------------------
	Creates and displays the help window. This includes loading the string resource and
	sizing the window so that it just fits the string.

	Documentation about HELPINFO is difficult to locate, so it is summarized here for reference:
		typedef  struct  tagHELPINFO {
			UINT	cbSize;			// Structure size, in bytes.
			int		iContextType;	// Context for which help is requested:
									//	HELPINFO_MENUITEM = menu item.
									//	HELPINFO_WINDOW   = control or window.
			int		iCtrlId;		// Id of menu item, control or window.
			HANDLE	hItemHandle;	// Handle of menu item, control or window
			DWORD	dwContextId;	// Help context identifier of the window or control.
			POINT	MousePos;		// Screen coords of mouse cursor.
		} HELPINFO;
----------------------------------------------------------------------------------------------*/
void AfContextHelpWnd::Create(HWND hwndPar, HELPINFO * phi, bool fHorizCenter)
{
	AssertPtr(phi);

	if (phi->iContextType != HELPINFO_WINDOW)
		return;

	bool fEnabled = ::IsWindowEnabled((HWND)phi->hItemHandle);

	// Retrieve the string from the resource file
	achar rgch[MAX_PATH];
	if (!::GetClassName((HWND)phi->hItemHandle, rgch, isizeof(rgch) / isizeof(achar)))
		return;
	if (phi->iCtrlId == -1 || lstrcmpi(rgch, _T("static")) == 0) // IDC_STATIC = -1.
	{
		// Static controls (i.e. labels) don't have help text. We can look at the next window,
		// though, to get the help text for that window. Since static controls are usually used
		// as labels for other controls, this lets the user click on the static control and get
		// the help for the non-static control that directly follows it.
		HWND hwndNext = ::GetWindow((HWND)phi->hItemHandle, GW_HWNDNEXT);
		fEnabled = ::IsWindowEnabled(hwndNext);
		phi->iCtrlId = ::GetDlgCtrlID(hwndNext);
		if (phi->iCtrlId < 1)
		{
			// We either couldn't find another control or it was another static control, so
			// we don't have any choice but to return without showing a help window.
			return;
		}
	}

	StrApp str;
	AfUtil::GetResourceStr(fEnabled ? krstWhatsThisEnabled : krstWhatsThisDisabled,
		phi->iCtrlId, str);

	// If we have a string to show, create the help window.
	if (str.Length() > 0)
	{
		ITsStringPtr qtss;
		StrUni stu(str);
		ConvertFormatting(stu, &qtss);
		Create(hwndPar, qtss, phi->MousePos, fHorizCenter);
	}
}


/*----------------------------------------------------------------------------------------------
	Create the help window showing the given text at the given point (in screen coordinates).
----------------------------------------------------------------------------------------------*/
void AfContextHelpWnd::Create(HWND hwndPar, ITsString * ptss, Point pt, bool fHorizCenter)
{
	AssertPtr(ptss);

	// Add the string (with a dummy id and tag) to our cache for the view window to use.
	m_qvcd.CreateInstance(CLSID_VwCacheDa);
	m_qvcd->CacheStringProp(ContextHelpVc::khvoCaption, ContextHelpVc::kflidCaption, ptss);

	// Create the window.
	WndCreateStruct wcs;
	wcs.lpszClass = _T("AfVwWnd");
	wcs.hwndParent = hwndPar;
	wcs.style = WS_POPUP | WS_CLIPSIBLINGS;
	CreateHwnd(wcs);

	m_hwndParent = hwndPar;

	// This is the only way I could figure out how to get the actual
	// width of the string. Everything else I tried returned the layout width,
	// or the width of the window, neither of which is correct.
	{
		HoldGraphics hg(this);
		CheckHr(m_qrootb->Layout(hg.m_qvg, kdxpMax));
	}
	RECT rcSel;
	IVwSelectionPtr qsel;
	CheckHr(m_qrootb->MakeSimpleSel(true, false, true, false, &qsel));
	CheckHr(qsel->GetParaLocation(&rcSel));
	int dxp = rcSel.right - rcSel.left + kdzpMargin + kdzpMargin + kdzpShadow;
	int dyp = rcSel.bottom - rcSel.top + kdzpMargin + kdzpMargin + kdzpShadow;

	// Make sure the window will be visible on the screen.
	// Center the tooltip.
	int xpLeft = pt.x;
	if (fHorizCenter)
		xpLeft -= (dxp / 2);
	Rect rc(xpLeft, pt.y, xpLeft + dxp, pt.y + dyp);
	AfGfx::EnsureVisibleRect(rc);
	::MoveWindow(m_hwnd, rc.left, rc.top, rc.Width(), rc.Height(), true);
	::ShowWindow(m_hwnd, SW_SHOW);

	// Capture the mouse, so that FWndProc will activate no matter where the mouse is.
	::SetCapture(m_hwnd);
	m_fSetCapture = true;

	// Fix the mouse cursor (since it was probably in "What's This" mode).
	::SetCursor(::LoadCursor(NULL, IDC_ARROW));
}


/*----------------------------------------------------------------------------------------------
	Replace formatting codes in the string with the corresponding text property.
	This can also handle 'stacked' formatting like '<i><b>text</b></i>'.
	Currently, the only options are bold, italic, and underline, although other formatting
	could be added later if needed.
----------------------------------------------------------------------------------------------*/
void AfContextHelpWnd::ConvertFormatting(StrUni & stu, ITsString ** pptss)
{
	AssertPtr(pptss);

	const wchar * pwszText = stu.Chars();
	AssertPsz(pwszText);

	// Initialize the builder with the string that was passed in.
	ITsStrBldrPtr qtsb;
	qtsb.CreateInstance(CLSID_TsStrBldr);
	CheckHr(qtsb->Replace(0, 0, stu.Bstr(), NULL));

	// This is the section that defines which formatting options are supported.
	const int kfmt = 3;
	const wchar * rgpszOpen[kfmt] = { L"<i>", L"<b>", L"<u>" };
	const wchar * rgpszClose[kfmt] = { L"</i>", L"</b>", L"</u>" };
	const int rgtpt[kfmt] = { ktptItalic, ktptBold, ktptUnderline };

	// Loop through the different kinds of formatting.
	for (int ifmt = 0; ifmt < kfmt; ifmt++)
	{
		const wchar * pwszOpen = rgpszOpen[ifmt];
		const wchar * pwszClose = rgpszClose[ifmt];
		int cchOpen = wcslen(pwszOpen);
		int cchClose = wcslen(pwszClose);

		// Keep looping until we don't find the open string for this format anymore.
		const wchar * pwszMin = wcsstr(pwszText, pwszOpen);
		while (pwszMin)
		{
			// Make sure we find the close string for this format.
			const wchar * pwszLim = wcsstr(pwszMin, pwszClose);
			if (pwszLim > pwszMin)
			{
				// Get the character index of the beginning of the open tag.
				int ichMin = pwszMin - pwszText;
				// Get the index of the character following the end of the close tag.
				int ichLim = pwszLim - pwszText + cchClose;
				// Calculate the length of the text part (excluding the open and close tags).
				int cchReplace = pwszLim - pwszMin - cchOpen;

				// Get the properties at the start of the text part.
				ITsTextPropsPtr qttp;
				ITsPropsBldrPtr qtpb;
				CheckHr(qtsb->get_PropertiesAt(ichMin + cchOpen, &qttp));
				CheckHr(qttp->GetBldr(&qtpb));

				// Add the new formatting to the properties.
				if (ifmt < 2) // bold, italic
					CheckHr(qtpb->SetIntPropValues(rgtpt[ifmt], ktpvEnum, kttvForceOn));
				else if (ifmt == 2) // underline
					CheckHr(qtpb->SetIntPropValues(rgtpt[ifmt], ktpvEnum, kuntSingle));
				CheckHr(qtpb->GetTextProps(&qttp));

				// Remove the open and close tags and replace with the new text properties.
				CheckHr(qtsb->ReplaceRgch(ichMin, ichLim, pwszMin + cchOpen, cchReplace, qttp));
				stu.Replace(ichMin, ichLim, pwszMin + cchOpen, cchReplace);
				// Since pwszText is no longer valid (because of the previous line),
				// we need to reset it here to the new string.
				pwszText = stu.Chars();
			}
			pwszMin = wcsstr(pwszText, pwszOpen);
		}
	}

	CheckHr(qtsb->GetString(pptss));
}


/*----------------------------------------------------------------------------------------------
	Make the root box.
----------------------------------------------------------------------------------------------*/
void AfContextHelpWnd::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
	IVwRootBox ** pprootb)
{
	AssertPtr(pvg);
	AssertPtrN(pwsf);
	AssertPtr(pprootb);

	*pprootb = NULL;

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this));
	HVO hvo = ContextHelpVc::khvoCaption;
	int frag = ContextHelpVc::kfrCaption;

	// Set up a new view constructor.
	ContextHelpVcPtr qchvc;
	qchvc.Attach(NewObj ContextHelpVc);

	ISilDataAccessPtr qsdaTemp;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));
	if (pwsf)
		CheckHr(qsdaTemp->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qsdaTemp));

	IVwViewConstructor * pvvc = qchvc;
	CheckHr(qrootb->SetRootObjects(&hvo, &pvvc, &frag, NULL, 1));
	*pprootb = qrootb.Detach();
}


/*----------------------------------------------------------------------------------------------
	Peek through each message sent to this window for a user action (such as a key typed
	or a mouse button pressed.) This includes any mouse click anywhere on the screen, thanks
	to the SetCapture command. Upon such an action, destroy the window.
----------------------------------------------------------------------------------------------*/
bool AfContextHelpWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	HWND hwndParent = m_hwndParent;
	bool fEatMessage = true;

	// For some reason when the help window is called for an item on a menu, the capture
	// doesn't get set correctly. So this recaptures the window if it needs to.
	if (m_fSetCapture && ::GetCapture() != m_hwnd)
	{
		Assert(m_hwnd);
		::SetCapture(m_hwnd);
	}

	switch (wm)
	{
	case WM_LBUTTONUP:
	case WM_MBUTTONUP:
	case WM_RBUTTONUP:
	case WM_MOUSEMOVE:
	case WM_SETFOCUS:
		// WARNING: Don't pass these messages on to the superclass window procedure.
		// If these get passed up, the caret will be shown (which we don't want).
		// Also, in the case of WM_MOUSEMOVE, nasty asserts will happen because this
		// window isn't embedded inside of a SplitChild, which the superclass OnMouseMove
		// expects.
		return false;

	case WM_LBUTTONDOWN:
	case WM_MBUTTONDOWN:
	case WM_RBUTTONDOWN:
		{
			// Translate mouse coordinates to those of the parent window.
			Point pt = MakePoint(lp);
			::ClientToScreen(m_hwnd, &pt);
			hwndParent = ::WindowFromPoint(pt);

			// If the user clicked within our help window, don't pass the message on.
			Rect rc;
			::GetWindowRect(m_hwnd, &rc);
			fEatMessage = ::PtInRect(&rc, pt);

			::ScreenToClient(hwndParent, &pt);
			if (pt.y < 0)
			{
				// We are in the caption area of the window, so change the message type.
				wm = WM_NCLBUTTONDOWN;
				::ClientToScreen(hwndParent, &pt);
				wp = ::SendMessage(hwndParent, WM_NCHITTEST, 0, MAKELPARAM(pt.x, pt.y));
			}
			lp = MAKELPARAM(pt.x, pt.y);
		}
		// Fall through.

	case WM_KEYDOWN:
	case WM_SYSKEYDOWN:
	case WM_CANCELMODE:
		{
			// Undo the SetCapture command from Create().
			m_fSetCapture = false;
			::ReleaseCapture();

#define LISTVIEW_REFRESH_BUG 0
#ifdef LISTVIEW_REFRESH_BUG
			// It seems that listview controls do not properly refresh themselves under the
			// following conditions:
			// 1) The listview currently has the focus.
			// 2) The help box is contained entirely inside the listview window.
			// To solve this problem, we check the bottom left corner of the help box to see if
			// it is inside the listview control. If it is, we invalidate the rectangle of the
			// listview that the help box occupied.
			// This is actually performing this action for every window, not just listview
			// windows, but it would probably be more work to figure out if it is a listview
			// window than it's worth, since we're only really redrawing the window, which
			// shouldn't be much (if any) of a performance hit.
			Rect rc;
			::GetWindowRect(m_hwnd, &rc);
#endif // LISTVIEW_REFRESH_BUG

			::DestroyWindow(m_hwnd);

#ifdef LISTVIEW_REFRESH_BUG
			Point pt(rc.left, rc.bottom);
			HWND hwndPar = ::WindowFromPoint(pt);
			if (hwndPar)
			{
				// Redraw the area of the screen that was covered by the help window.
				::MapWindowPoints(NULL, hwndPar, (POINT *)&rc, 2);
				::RedrawWindow(hwndPar, &rc, NULL, RDW_ERASE | RDW_FRAME | RDW_INVALIDATE);
			}
#endif // LISTVIEW_REFRESH_BUG

			if (!fEatMessage)
			{
				// Let the parent process the message (Word, others behave this way).
				// For some reason, this needs to post the message instead of sending it. We
				// were having problems when the user clicked on a combobox on a toolbar when
				// we were sending the message. This seemed to take care of the problem.
				::PostMessage(hwndParent, wm, wp, lp);
			}
			return true;
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Paints the client area of the window. This only handles drawing the border and the shadow
	at the bottom and right edges of the window. It calls the view code to actually draw the
	help text.
----------------------------------------------------------------------------------------------*/
bool AfContextHelpWnd::OnPaint(HDC hdcDef)
{
	Assert(!hdcDef);

	// Prepare to paint.
	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_pwndSubclass->Hwnd(), &ps);
	::SetBkMode(hdc, TRANSPARENT);

	Rect rc;
	m_pwndSubclass->GetClientRect(rc);

	// Draw the context help background.
	rc.right -= kdzpShadow;
	rc.bottom -= kdzpShadow;
	AfGfx::FillSolidRect(hdc, rc, ::GetSysColor(COLOR_INFOBK));

	// Draw the text from the view window.
	Draw(hdc, rc);

	// Draw the window border.
	::MoveToEx(hdc, rc.left, rc.top, NULL);
	::LineTo(hdc, rc.right, rc.top);
	::LineTo(hdc, rc.right, rc.bottom);
	::LineTo(hdc, rc.left, rc.bottom);
	::LineTo(hdc, rc.left, rc.top);

	// Draw the shadow.
	WORD rgbits[8] = { 0x0055, 0x00AA, 0x0055, 0x00AA, 0x0055, 0x00AA, 0x0055, 0x00AA };
	HBITMAP hbmp = AfGdi::CreateBitmap(8, 8, 1, 1, &rgbits);
	HBRUSH hbr = AfGdi::CreatePatternBrush(hbmp);
	HBRUSH hbrOld = AfGdi::SelectObjectBrush(hdc, hbr);
	::BitBlt(hdc, rc.left + kdzpShadow, rc.bottom, rc.Width(), kdzpShadow,
		hdc, rc.left + kdzpShadow, rc.bottom, MERGECOPY);
	::BitBlt(hdc, rc.right, rc.top + kdzpShadow, kdzpShadow, rc.Height() - kdzpShadow,
		hdc, rc.right, rc.top + kdzpShadow, MERGECOPY);
	AfGdi::SelectObjectBrush(hdc, hbrOld, AfGdi::OLD);
	AfGdi::DeleteObjectBrush(hbr);
	AfGdi::DeleteObjectBitmap(hbmp);

	::EndPaint(m_pwndSubclass->Hwnd(), &ps);

	return true;
}


/***********************************************************************************************
	AfToolTipWnd methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfToolTipWnd::AfToolTipWnd()
{
	m_hwndParent = NULL;

	m_qvcd.CreateInstance(CLSID_VwCacheDa);
}


/*----------------------------------------------------------------------------------------------
	Subclass the tooltip window.
----------------------------------------------------------------------------------------------*/
void AfToolTipWnd::SubclassToolTip(HWND hwndToolTip)
{
	SubclassHwnd(hwndToolTip);

	CREATESTRUCT cs;
	SuperClass::OnCreate(&cs);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void AfToolTipWnd::UpdateText(ITsString * ptss)
{
	AssertPtr(ptss);

	m_qvcd->CacheStringProp(ToolTipVc::khvoCaption, ToolTipVc::kflidCaption, ptss);

	ISilDataAccessPtr qsda;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda));
	qsda->PropChanged(m_qrootb, kpctNotifyAll, ToolTipVc::khvoCaption,
		ToolTipVc::kflidCaption, 0, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Make the root box.
----------------------------------------------------------------------------------------------*/
void AfToolTipWnd::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
	IVwRootBox ** pprootb)
{
	AssertPtr(pvg);
	AssertPtrN(pwsf);
	AssertPtr(pprootb);

	*pprootb = NULL;

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this));
	HVO hvo = ToolTipVc::khvoCaption;
	int frag = ToolTipVc::kfrCaption;

	// Set up a new view constructor.
	ToolTipVcPtr qttvc;
	qttvc.Attach(NewObj ToolTipVc);

	ISilDataAccessPtr qsdaTemp;
	CheckHr(m_qvcd->QueryInterface(IID_ISilDataAccess, (void **)&qsdaTemp));
	if (pwsf)
		CheckHr(qsdaTemp->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qsdaTemp));

	IVwViewConstructor * pvvc = qttvc;
	CheckHr(qrootb->SetRootObjects(&hvo, &pvvc, &frag, NULL, 1));
	*pprootb = qrootb.Detach();
}


/*----------------------------------------------------------------------------------------------
	Peek through each message sent to this window for a user action (such as a key typed
	or a mouse button pressed.) This includes any mouse click anywhere on the screen, thanks
	to the SetCapture command. Upon such an action, destroy the window.
----------------------------------------------------------------------------------------------*/
bool AfToolTipWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_LBUTTONUP:
	case WM_MBUTTONUP:
	case WM_RBUTTONUP:
	case WM_MOUSEMOVE:
	case WM_SETFOCUS:
		// WARNING: Don't pass these messages on to the superclass window procedure.
		// If these get passed up, the caret will be shown (which we don't want).
		// Also, in the case of WM_MOUSEMOVE, nasty asserts will happen because this
		// window isn't embedded inside of a SplitChild, which the superclass OnMouseMove
		// expects.
		return false;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Paints the client area of the window. This only handles drawing the border and the shadow
	at the bottom and right edges of the window. It calls the view code to actually draw the
	help text.
----------------------------------------------------------------------------------------------*/
bool AfToolTipWnd::OnPaint(HDC hdcDef)
{
	Assert(!hdcDef);

	// Prepare to paint.
	PAINTSTRUCT ps;
	HDC hdc = ::BeginPaint(m_pwndSubclass->Hwnd(), &ps);
	::SetBkMode(hdc, TRANSPARENT);

	Rect rc;
	m_pwndSubclass->GetClientRect(rc);

	// Draw the context help background.
	AfGfx::FillSolidRect(hdc, rc, ::GetSysColor(COLOR_INFOBK));

	// Draw the text from the view window.
	Draw(hdc, rc);

	// Draw the window border.
	/*::MoveToEx(hdc, rc.left, rc.top, NULL);
	::LineTo(hdc, rc.right, rc.top);
	::LineTo(hdc, rc.right, rc.bottom);
	::LineTo(hdc, rc.left, rc.bottom);
	::LineTo(hdc, rc.left, rc.top);*/

	::EndPaint(m_pwndSubclass->Hwnd(), &ps);

	return true;
}
