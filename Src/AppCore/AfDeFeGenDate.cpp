/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeGenDate.cpp
Responsibility: Ken Zook
Last reviewed: never

Description:
	This class provides the base for all data entry field editors.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

AfDeFeGenDate::AfDeFeGenDate()
{
}


AfDeFeGenDate::~AfDeFeGenDate()
{
}


//:>********************************************************************************************
//:>	AfDeFeGenDate methods.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Returns false if the date string does not parse correctly.
----------------------------------------------------------------------------------------------*/
bool AfDeFeGenDate::IsOkToClose(bool fWarn)
{
	ITsStringPtr qtss;
	::SendMessage(m_hwnd, FW_EM_GETTEXT, 0, (LPARAM)&qtss);
	SmartBstr sbstr;
	if (qtss)
		qtss->get_Text(&sbstr);
	int gdat = StrUtil::ParseGenDate(sbstr.Chars());

	// If it fails to parse, produce a dialog and return false.
	if (!gdat && sbstr.Length())
	{
		if (fWarn)
		{
			achar rgchFmt[81];
			int cchFmt;
			cchFmt = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SSHORTDATE, rgchFmt, 80);
			ToUpper(rgchFmt,cchFmt);
			StrApp str(kstridBadDate);
			str.Append(rgchFmt);
			str.Append(".  ");
			StrApp strInvDt(kstridInvalidDate);
			::MessageBox(m_hwnd,str.Chars(), strInvDt,	MB_OK | MB_ICONWARNING);
			ProcessChooser();
		}
		return false;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Return true if any changes have been made.
----------------------------------------------------------------------------------------------*/
bool AfDeFeGenDate::IsDirty()
{
	if (!m_hwnd)
		return false; // Editor not active.
	ITsStringPtr qtssNew;
	::SendMessage(m_hwnd, FW_EM_GETTEXT, 0, (LPARAM)&qtssNew);
	SmartBstr sbstr;
	if (qtssNew)
		qtssNew->get_Text(&sbstr);
	int gdat = StrUtil::ParseGenDate(sbstr.Chars());
	// If it failed to parse, it must be dirty.
	if (!gdat && sbstr.Length())
		return true;

	// If the item has changed, save it to the data cache.
	int gdatOrig;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	CheckHr(qcvd->get_IntProp(m_hvoObj, m_flid, &gdatOrig));
	return gdatOrig != gdat;
}


/*----------------------------------------------------------------------------------------------
	Save changes that have been made to the current editor.
----------------------------------------------------------------------------------------------*/
bool AfDeFeGenDate::SaveEdit()
{
	if (!IsOkToClose())
		return false;
	EndTempEdit();

	ITsStringPtr qtssNew;
	::SendMessage(m_hwnd, FW_EM_GETTEXT, 0, (LPARAM)&qtssNew);
	SmartBstr sbstr;
	if (qtssNew)
		qtssNew->get_Text(&sbstr);
	m_gdat = StrUtil::ParseGenDate(sbstr.Chars());

	// If the item has changed, save it to the data cache.
	int gdatOrig;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	CheckHr(qcvd->get_IntProp(m_hvoObj, m_flid, &gdatOrig));
	if (gdatOrig != m_gdat)
	{
		// Check if the record has been edited by someone else since we first loaded the data.
		HRESULT hrTemp;
		if ((hrTemp = qcvd->CheckTimeStamp(m_hvoObj)) != S_OK)
		{
			// If it was changed and the user does not want to overwrite it, perform a refresh
			// so the displayed field will revert to it's original value.
			m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
			qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1);
			goto LFinish;
		}

		// Update the value in the cache and refresh views.
		BeginChangesToLabel();
		qcvd->SetInt(m_hvoObj, m_flid, m_gdat);
		m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
		qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1);
		CheckHr(qcvd->EndUndoTask());
	}
	else
	{
		// Make sure it is displayed in standard form.
		InitContents(m_gdat);
	}
LFinish:
	// We need to leave in a state that cancels undo actions since this may be called
	// without actually closing the edit box.
	BeginTempEdit();
	return true;
}


/*----------------------------------------------------------------------------------------------
	This initializes the string based on the input date.
----------------------------------------------------------------------------------------------*/
void AfDeFeGenDate::InitContents(int gdat)
{
	SuperClass::Init(); // Initialize the superclass.

	m_gdat = gdat;

	ITsStringPtr qtss;
	ITsStrFactoryPtr qtsf;
	StrUni stu;
	stu.Format(L"%1D", m_gdat);

	qtsf.CreateInstance(CLSID_TsStrFactory);
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_ws, &qtss);
	m_qtss = qtss;
}


/*----------------------------------------------------------------------------------------------
	Refresh the field from the data cache.
----------------------------------------------------------------------------------------------*/
void AfDeFeGenDate::UpdateField()
{
	// Get the date from the cache.
	int gdat;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	CheckHr(qcvd->get_IntProp(m_hvoObj, m_flid, &gdat));

	// Set it to standard form.
	InitContents(gdat);
	// If we have an edit box, update the contents.
	if (m_hwnd)
		::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)m_qtss.Ptr());
}

/*----------------------------------------------------------------------------------------------
	Process commands. Return true if processed.
----------------------------------------------------------------------------------------------*/
void AfDeFeGenDate::ProcessChooser()
{
	ITsStringPtr qtss;
	::SendMessage(m_hwnd, FW_EM_GETTEXT, 0, (LPARAM)&qtss);
	SmartBstr sbstr;
	if (qtss)
		qtss->get_Text(&sbstr);
	int gdat = StrUtil::ParseGenDate(sbstr.Chars());

	// At one point we had a message box here telling the user it couldn't parse, but
	// David said this wasn't necessary. We should just do the best we can.
	m_gdat = gdat;

	typedef GenSmartPtr<DatePickDlg> DatePickDlgPtr;
	DatePickDlgPtr qdpk;
	qdpk.Attach(NewObj DatePickDlg(DPK_ALLOW_PRECISION | DPK_ALLOW_BC));
	AfDialogShellPtr qdlgShell;
	qdlgShell.Create();
	qdpk->DecodeGDate(m_gdat);
	StrApp strDate(kstidDate);
	if (qdlgShell->CreateDlgShell(qdpk, strDate.Chars(), m_hwnd) == kctidOk)
	{
		InitContents(qdpk->EncodeGDate());		// set to date standard form
		if (m_hwnd)							// if edit box, update it
			::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)m_qtss.Ptr());
	}
}


/*----------------------------------------------------------------------------------------------
	Make an edit box to allow editing. hwnd is the parent hwnd. rc is the size of the child
	window. Store the hwnd and return true.
----------------------------------------------------------------------------------------------*/
bool AfDeFeGenDate::BeginEdit(HWND hwnd, Rect &rc, int dxpCursor, bool fTopCursor,
	TptEditable tpte)
{
	if (!SuperClass::BeginEdit(hwnd, rc, dxpCursor, fTopCursor, ktptSemiEditable))
		return false;

	::SendMessage(m_hwnd, EM_SETSEL, 0, 0);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Check the requirments of the FldSpec, and verify that data in the field meets the
	requirement. It returns:
		kFTReqNotReq if the all requirements are met.
		kFTReqWs if data is missing, but it is encouraged.
		kFTReqReq if data is missing, but it is required.
----------------------------------------------------------------------------------------------*/
FldReq AfDeFeGenDate::HasRequiredData()
{
	if (!m_gdat)
		return m_qfsp->m_fRequired;
	else
		return kFTReqNotReq;
}


//:>********************************************************************************************
//:>	DatePickDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
DatePickDlg::DatePickDlg(int nMode) : AfDialogView()
{
	static bool calendar_registered = false;
	if (!calendar_registered)
	{
		static INITCOMMONCONTROLSEX icex = { sizeof(INITCOMMONCONTROLSEX), ICC_DATE_CLASSES };
		InitCommonControlsEx(&icex);
		calendar_registered = true;
	}

	m_nMode = nMode;
	m_rid = kridDatePickDlg;
	m_nRecurse = 0;
	m_pszHelpUrl =
		_T("Beginning_Tasks/Entering_Data_in_Fields/Enter_data_in_a_date_field.htm");
	memset(&m_systime, 0, sizeof(m_systime));
	m_systime.wYear = (WORD)~0;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool DatePickDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	m_nRecurse = 0;
	SetSizesPositions();
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidDpkYear);
	::SendMessage(hwnd, EM_LIMITTEXT, m_kYearMaxLen, 0);

	if (m_systime.wYear != (WORD)~0)
	{
		m_nYear = m_systime.wYear;
		m_nMonth = m_systime.wMonth;
		m_nDay = m_systime.wDay;
	}
	CalcDaysInMonth();
	OnEventPrecision(DPK_INIT);
	OnEventDay(DPK_INIT);
	OnEventMonth(DPK_INIT);
	OnEventYear(DPK_INIT, 0);
	OnEventYearSpin(DPK_INIT, NULL);
	OnEventCalendar(DPK_INIT);
	UpdateControls();
	UpdateCalendar();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Set control initial sizes and positions based on mode and language
----------------------------------------------------------------------------------------------*/
void DatePickDlg::SetSizesPositions()
{
	RECT rp, rc, rx;
	HWND hwnd;
	int x, y;
	static int kCtl[] = { kctidDpkMonth, kctidDpkDay, kctidDpkYear, kctidDpkYearSpin, 0 };
	HWND hwndPrec   = ::GetDlgItem(m_hwnd, kctidDpkPrecision);
	HWND hwndCal    = ::GetDlgItem(m_hwnd, kctidDpkCalendar);
	HWND hwndParent = ::GetParent(hwndPrec);
	::GetWindowRect(hwndParent, &rp);

	if (!(m_nMode & DPK_ALLOW_PRECISION))
	{
		::ShowWindow(hwndPrec, SW_HIDE); // Hide Precision
		// Then evenly space horizontally controls above calendar
		int gap = 10, widsum = - 2*gap - 2;
		for (int i=0; kCtl[i] != 0; i++)
		{
			hwnd = ::GetDlgItem(m_hwnd, kCtl[i]);
			::GetWindowRect(hwnd, &rc);
			widsum += gap + rc.right - rc.left; // sum total width needed
		}
		x = ((rp.right - rp.left) - widsum) / 2 - 2;
		y = rc.top - rp.top; // keep original vertical position
		for (int i=0; kCtl[i] != 0; i++)
		{
			hwnd = ::GetDlgItem(m_hwnd, kCtl[i]);
			::GetWindowRect(hwnd, &rc);
			::MoveWindow(hwnd, x, y, rc.right-rc.left, rc.bottom-rc.top, false);
			int g = (kCtl[i] != kctidDpkYear) ? gap : -2; // no gap between year & spinner
			x += (rc.right - rc.left) + g;
		}
	}

	// Set Calendar size and position to adjust for different languages
	::GetWindowRect(hwndCal, &rc);
	::SendMessage(hwndCal, MCM_GETMINREQRECT, 0, (LPARAM)&rx);
	x = ((rp.right - rp.left) - (rx.right - rx.left)) / 2 - 2; // center it horizontally
	y = rc.top - rp.top; // keep original vertical position
	::MoveWindow(hwndCal, x, y, rx.right-rx.left, rx.bottom-rx.top, false);
}

/*----------------------------------------------------------------------------------------------
	Process a WM_COMMAND message.

	@param ctid The control identifier code.
	@param event The notification code from the control.
	@param hctl The handle to the control.

	@return True if the command has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool DatePickDlg::OnCommand(int ctrl_id, int event, HWND hctl)
{
	m_hCtl = hctl;
	switch (ctrl_id)
	{
	case kctidDpkPrecision:
		switch (event)
		{
		case CBN_SELCHANGE:
			return OnEventPrecision(DPK_SELCHANGE);
		}
		break;
	case kctidDpkYear:
		switch (event)
		{
		case EN_CHANGE:
			return OnEventYear(DPK_CHANGE, 0);
		case EN_KILLFOCUS:
			return OnEventYear(DPK_KILLFOCUS, 0);
		}
		break;
	case kctidDpkMonth:
		switch (event)
		{
		case CBN_SELCHANGE:
			return OnEventMonth(DPK_SELCHANGE);
		}
		break;
	case kctidDpkDay:
		switch (event)
		{
		case CBN_SELCHANGE:
			return OnEventDay(DPK_SELCHANGE);
		}
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message.

	@param ctidFrom Identifies the control sending the message.
	@param pnmhdr Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool DatePickDlg::OnNotifyChild(int ctidFrom, NMHDR* pnmhdr, long& lnRet)
{
	AssertPtr(pnmhdr);
	m_hCtl = GetDlgItem(m_hwnd, ctidFrom);
	switch (pnmhdr->idFrom)
	{
	case kctidDpkYearSpin:
		switch (pnmhdr->code)
		{
		case UDN_DELTAPOS:
			return OnEventYearSpin(DPK_DELTAPOS, pnmhdr);
		}
		break;
	case kctidDpkCalendar:
		switch (pnmhdr->code)
		{
		case MCN_SELCHANGE:
		case MCN_SELECT: // Only this catches click on today
			return OnEventCalendar(DPK_SELCHANGE);
		}
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Process all events relating to Precision control
----------------------------------------------------------------------------------------------*/

bool DatePickDlg::OnEventPrecision(int event)
{
	LoadDateQualifiers();
	Assert(g_fDoneDateQual);

	static struct
	{
		const achar * text;
		int data;
	} t[] = {
		{ g_rgchDatePrec[0],  m_kDateBefore },
		{ g_rgchDatePrec[1],  m_kDateOn     },
		{ g_rgchDatePrec[2],  m_kDateAbout  },
		{ g_rgchDatePrec[3],  m_kDateAfter  },
		{ g_rgchDateBlank[0], -1            } };

	int i, count, index;
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidDpkPrecision);
	switch (event)
	{
	case DPK_INIT:
		::SendMessage(hwnd, CB_RESETCONTENT, 0, 0);
		for (i = 0 ; i < SizeOfArray(t); i++)
			::SendMessage(hwnd, CB_ADDSTRING, 0, (LPARAM)t[i].text);
		for (i = 0; i < SizeOfArray(t); i++)
			::SendMessage(hwnd, CB_SETITEMDATA, i, (LPARAM)t[i].data);
		// fall through
	case DPK_UPDATE:
		count = ::SendMessage(hwnd, CB_GETCOUNT, 0, 0);
		for (index=0; index < SizeOfArray(t); index++)
		{
			if (t[index].data == m_nPrecision)
				::SendMessage(hwnd, CB_SETCURSEL, index, 0);
		}
		return true;
	case DPK_SELCHANGE:
		count = ::SendMessage(hwnd, CB_GETCOUNT, 0, 0);
		index = ::SendMessage(hwnd, CB_GETCURSEL, 0, 0);
		m_nPrecision = ::SendMessage(hwnd, CB_GETITEMDATA, (WPARAM)index, 0);
		UpdateCalendar();
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Process all events relating to Year control

	@param event Notification code: DPK_INIT, DPK_UPDATE, or DPK_KILLFOCUS.
	@param nChar Not used by this method.

	@return True if the event is handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool DatePickDlg::OnEventYear(int event, UINT nChar)
{
	int year;
	achar y[m_kYearMaxLen];
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidDpkYear);
	switch (event)
	{
	case DPK_INIT:
	case DPK_UPDATE:
		FormatYear(y, m_nYear, m_nMonth);
		::SendMessage(hwnd, WM_SETTEXT, 0, (LPARAM)y);
		return true;
	case DPK_KILLFOCUS:
		::SendMessage(hwnd, WM_GETTEXT, m_kYearMaxLen, (LPARAM)y);
		if ((year = ParseYear(y)) == 0)
		{
			SetActiveWindow(hwnd);
			Beep(1000, 50); // ENHANCE:  is there a better way to beep
		}
		else
			m_nYear = year;
		CalcDaysInMonth();
		UpdateControls();
		UpdateCalendar();
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Process all events relating to Year Spin control

	@param event Notification code: DPK_INIT, DPK_UPDATE, or DPK_DELTAPOS.
	@param pnmhdr Pointer to the notification message information.

	@return True if the event is handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool DatePickDlg::OnEventYearSpin(int event, LPNMHDR pnmhdr)
{
	static UDACCEL uda; uda.nSec = 0; uda.nInc = 1;
	int delta;
	achar y[m_kYearMaxLen];
	switch (event)
	{
	case DPK_INIT:
		::SendMessage(m_hCtl, UDM_SETACCEL, 1, (WPARAM)&uda);
		::SendMessage(m_hCtl, UDM_SETRANGE, 0, MAKELONG(-6000,9999));
		// fall through
	case DPK_UPDATE:
		::SendMessage(m_hCtl, UDM_SETPOS, 0, m_nYear);
		return true;
	case DPK_DELTAPOS: // Spin control is activated.
		delta = -((NMUPDOWN*)pnmhdr)->iDelta;
		m_nYear += delta;
		if (m_nMode & DPK_ALLOW_BC)
		{
			if (m_nYear == 0)
				m_nYear = (delta < 0) ? -1 : 1;
		}
		else
		{
			if (m_nYear <= 0)
				m_nYear = 1;
		}
		::SendMessage(m_hCtl, UDM_SETPOS, 0, m_nYear);
		FormatYear(y, m_nYear, m_nMonth);
		UpdateControls();
		UpdateCalendar();
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Process all events relating to Month control

	@param event Notification code: DPK_INIT, DPK_UPDATE, or DPK_SELCHANGE.

	@return True if the event is handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool DatePickDlg::OnEventMonth(int event)
{
	int i, index, count;
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidDpkMonth);
	switch (event)
	{
	case DPK_INIT:
		::SendMessage(hwnd, CB_RESETCONTENT, 0, 0);
		for (i=1; i<=12; i++)
		{
			StrApp str(StrUtil::GetMonthStr(i,false)->Chars());
			::SendMessage(hwnd, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		}
		::SendMessage(hwnd, CB_ADDSTRING, 0, (LPARAM)g_rgchDateBlank[1]);
		return true;
	case DPK_UPDATE:
		hwnd = ::GetDlgItem(m_hwnd, kctidDpkMonth);
		count = ::SendMessage(hwnd, CB_GETCOUNT, 0, 0);
		index = (m_nMonth == 0) ? -1 : m_nMonth-1;
		::SendMessage(hwnd, CB_SETCURSEL, index, 0);
		return true;
	case DPK_SELCHANGE:
		count = ::SendMessage(hwnd, CB_GETCOUNT, 0, 0);
		index = ::SendMessage(hwnd, CB_GETCURSEL, 0, 0);
		m_nMonth = (index < count-1) ?  index+1 : 0;
		if (!m_nMonth)
		{
			index = -1;
			::SendMessage(hwnd, CB_SETCURSEL, index, 0);
			m_nDay = 0;
			HWND hwndDay = ::GetDlgItem(m_hwnd, kctidDpkDay);
			::SendMessage(hwndDay, CB_SETCURSEL, index, 0);
		}
		CalcDaysInMonth();
		UpdateCalendar();
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Process all events relating to Day control

	@param event Notification code: DPK_INIT, DPK_UPDATE, or DPK_SELCHANGE.

	@return True if the event is handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool DatePickDlg::OnEventDay(int event)
{
	int i, count, index;
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidDpkDay);
	switch (event)
	{
	case DPK_INIT:
		::SendMessage(hwnd, CB_RESETCONTENT, 0, 0);
		for (i = 1; i <= m_nDaysInMonth; i++)
		{
			achar rgch[20];
			_stprintf_s(rgch, _T("%d"), i);
			::SendMessage(hwnd, CB_ADDSTRING, 0, (LPARAM)rgch);
		}
		::SendMessage(hwnd, CB_ADDSTRING, 0, (LPARAM)g_rgchDateBlank[2]);
		return true;
	case DPK_UPDATE:
		count = ::SendMessage(hwnd, CB_GETCOUNT, 0, 0);
		index = (m_nDay > 0) ? m_nDay-1 : -1;
		::SendMessage(hwnd, CB_SETCURSEL, index, 0);
		return true;
	case DPK_SELCHANGE:
		count = ::SendMessage(hwnd, CB_GETCOUNT, 0, 0);
		index = ::SendMessage(hwnd, CB_GETCURSEL, 0, 0);
		m_nDay = (index < count-1) ?  index+1 : 0;
		if (!m_nDay)
		{
			index = -1;
			::SendMessage(hwnd, CB_SETCURSEL, index, 0);
		}
		UpdateCalendar();
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Process all events relating to calendar control
	Brings the calendar control into sync with the separate year, month, and day controls,
	and with the separate year, month, and day values stored for the overall dialog.

	@param event Notification code: DPK_INIT, DPK_UPDATE, DPK_SELCHANGE.

	@return True if the event is handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool DatePickDlg::OnEventCalendar(int event)
{
	SYSTEMTIME s, s2[2];
	HWND hwndNoCal = ::GetDlgItem(m_hwnd, kctidDpkNoCal);
	HWND hwndCal = ::GetDlgItem(m_hwnd, kctidDpkCalendar);
	HWND hwndGroup = ::GetDlgItem(m_hwnd, kctidDpkGroup);
	switch (event)
	{
	case DPK_INIT:
		::SendMessage(hwndCal, MCM_SETUNICODEFORMAT, true, 0);
		::SendMessage(hwndCal, MCM_GETRANGE, 0, (LPARAM)s2);
		m_nMinCalYyyyMm = (s2[0].wYear == 0) ? 175209 : 100*s2[0].wYear+s2[0].wMonth;
		return true;
	case DPK_UPDATE:
		s.wYear = (WORD)m_nYear;

		if (m_nMonth)
			s.wMonth = (WORD)m_nMonth;
		else
			s.wMonth = (WORD)1;

		if (m_nDay)
			s.wDay = (WORD)m_nDay;
		else
			s.wDay = (WORD)1;

		s.wHour = s.wMinute = s.wSecond = s.wMilliseconds = 0;

		if ((100*m_nYear + m_nMonth < m_nMinCalYyyyMm) || (m_nPrecision == -1))
		{
			// Not a valid date so show no calender.
			::ShowWindow(hwndCal,SW_HIDE);
			::ShowWindow(hwndGroup,SW_SHOW);
			::ShowWindow(hwndNoCal,SW_SHOW);
		}
		else
		{
			::SendMessage(hwndCal, MCM_SETCURSEL, 0, (LPARAM)&s); // no change if not valid date
			::ShowWindow(hwndCal,SW_SHOW);
			::ShowWindow(hwndGroup,SW_HIDE);
			::ShowWindow(hwndNoCal,SW_HIDE);
			SetCalendarColors();
		}
		return true;
	case DPK_SELCHANGE:
		::SendMessage(hwndCal, MCM_GETCURSEL, 0, (LPARAM)&s);
		if (m_nPrecision == m_kDateBlank)
			m_nPrecision = m_kDateOn;
		m_nYear = s.wYear;
		m_nMonth = s.wMonth;
		m_nDay = s.wDay;
		CalcDaysInMonth();
		UpdateControls();
		SetCalendarColors();
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Set Calendar colors based on pre-Gregorian date or not
----------------------------------------------------------------------------------------------*/
void DatePickDlg::SetCalendarColors()
{
	#define T(clr) COLOR_##clr
	static COLORREF clr[2][6] = {
		{ T(BTNFACE),   T(WINDOW),T(GRAYTEXT),  T(INACTIVECAPTION),T(CAPTIONTEXT),T(GRAYTEXT) },
		{ T(BACKGROUND),T(WINDOW),T(WINDOWTEXT),T(ACTIVECAPTION),  T(CAPTIONTEXT),T(GRAYTEXT) } };
	#undef T

	HWND hwnd = ::GetDlgItem(m_hwnd, kctidDpkCalendar);
	LONG yyyymm = 100*m_nYear + m_nMonth;
	// Set colors based on pre-Gregorian or not
	int i = yyyymm >= m_nMinCalYyyyMm
			&& m_nYear != 0 && m_nMonth != 0 && m_nDay != 0 && m_nPrecision != m_kDateBlank;
	#define SM(m,c) ::SendMessage(hwnd, MCM_SETCOLOR, m, (LPARAM)(GetSysColor(clr[i][c])))
	SM(MCSC_BACKGROUND,   0); // background between months
	SM(MCSC_MONTHBK,      1); // month background
	SM(MCSC_TEXT,         2); // month text
	SM(MCSC_TITLEBK,      3); // title background
	SM(MCSC_TITLETEXT,    4); // title text
	SM(MCSC_TRAILINGTEXT, 5); // days outside month
	#undef SM
	// Display today circle only for current month
	SYSTEMTIME s;
	::GetLocalTime(&s);
	LONG ows = ::GetWindowLong(hwnd, GWL_STYLE);
	ows = (yyyymm == 100*s.wYear + s.wMonth) ? ows & ~MCS_NOTODAYCIRCLE : ows | MCS_NOTODAYCIRCLE;
	::SetWindowLong(hwnd, GWL_STYLE, ows);
}

/*----------------------------------------------------------------------------------------------
	Update the calendar when one of the controls changes
----------------------------------------------------------------------------------------------*/
void DatePickDlg::UpdateCalendar()
{
	if (m_nRecurse > 0)
		return;
	m_nRecurse++;
	OnEventCalendar(DPK_UPDATE);

	if (m_nPrecision == -1)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidDpkYear), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidDpkMonth), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidDpkDay), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidDpkYearSpin), FALSE);
	}
	else
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidDpkYear), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidDpkMonth), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidDpkDay), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidDpkYearSpin), TRUE);
	}

	UpdateSystime();
	m_nRecurse--;
}

/*----------------------------------------------------------------------------------------------
	Update the controls when the calendar changes
----------------------------------------------------------------------------------------------*/
void DatePickDlg::UpdateControls()
{
	if (m_nRecurse > 0)
		return;
	m_nRecurse++;
	OnEventPrecision(DPK_UPDATE);
	OnEventDay(DPK_UPDATE);
	OnEventMonth(DPK_UPDATE);
	OnEventYear(DPK_UPDATE, 0);
	OnEventYearSpin(DPK_UPDATE, NULL);
	UpdateSystime();
	m_nRecurse--;
}

/*----------------------------------------------------------------------------------------------
	Update m_systime when any value changes
----------------------------------------------------------------------------------------------*/
void DatePickDlg::UpdateSystime()
{
	m_systime.wYear = (WORD)m_nYear;
	m_systime.wMonth = (WORD)m_nMonth;
	m_systime.wDay = (WORD)m_nDay;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
int DatePickDlg::EncodeGDate()
{
	int year, bc, ans;
	if (m_nYear < 0)
		year = -m_nYear, bc = -1;
	else
		year = m_nYear, bc = 1;
	if (m_nPrecision == m_kDateBlank)
		ans = 0;
	else
		ans = bc * (((year * 100L + m_nMonth) * 100L + m_nDay) * 10L + m_nPrecision);
	return ans;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void DatePickDlg::DecodeGDate(int gdate)
{
	if (gdate == 0)
	{
		SYSTEMTIME s;
		::GetLocalTime(&s);
		m_nPrecision = m_kDateOn;
		m_nDay = s.wDay;
		m_nMonth = s.wMonth;
		m_nYear = s.wYear;
	}
	else
	{
		int bc = 1;
		if (gdate < 0)
			bc = -1, gdate = -gdate;
		m_nPrecision = (gdate == 0) ? m_kDateBlank : gdate % 10;
		gdate /= 10;
		m_nDay = gdate % 100;
		gdate /= 100;
		m_nMonth = gdate % 100;
		gdate /= 100;
		m_nYear = bc * gdate;
	}
}

/*----------------------------------------------------------------------------------------------
	Set the separate year, month, and day values to today's date.
----------------------------------------------------------------------------------------------*/
void DatePickDlg::GetToday()
{
	SYSTEMTIME s;
	::GetLocalTime(&s);
	m_nYear = s.wYear;
	m_nMonth = s.wMonth;
	m_nDay = s.wDay;
	CalcDaysInMonth();
}

/*----------------------------------------------------------------------------------------------
	Calculate the number of days in the currently selected year and month, and update the month
	control accordingly if necessary.
----------------------------------------------------------------------------------------------*/
void DatePickDlg::CalcDaysInMonth()
{
	if (m_nRecurse > 0)
		return;
	m_nRecurse++;
	int prev_days_in_month = m_nDaysInMonth;

	if (100*m_nYear + m_nMonth == 175209)
		m_nDaysInMonth = 19;		// the month the calendar was changed
	else
	{
		static int m[12] = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
		m[1] = (((m_nYear%4) == 0 && (m_nYear%100) != 0) || (m_nYear%1000) == 0) ? 29 : 28;
		m_nDaysInMonth = m[m_nMonth-1];
	}

	if (prev_days_in_month != m_nDaysInMonth)
	{
		if (m_nDay > m_nDaysInMonth)
			m_nDay = m_nDaysInMonth;
		OnEventDay(DPK_INIT);		// Set number of days in month control
		OnEventDay(DPK_UPDATE);	// Restore combo selection after re-building it
	}
	m_nRecurse--;
}

/*----------------------------------------------------------------------------------------------
	Parse the string to decode the year.

	@param char y[m_kYearMaxLen] Input year string (may be modified by this method).

	@return The numeric value of the year (negative means BC).
----------------------------------------------------------------------------------------------*/
int DatePickDlg::ParseYear(achar y[m_kYearMaxLen])
{
	int i1, i2=0, year=0;
	achar z[m_kYearMaxLen];
	for (i1=0; i1 < (int)_tcslen(y); i1++)
	{
		if (y[i1] >= '0' && y[i1] <= '9')
			year = 10*year + (y[i1] - '0'); // convert numerals to int
		else if (y[i1] >= 'a' && y[i1] <= 'z')
			z[i2++] = (char)(y[i1] - 'a' + 'A'); // to upper case
		else if (y[i1] > ' ')
			z[i2++] = y[i1]; // anything else that is not blank
	}
	z[i2] = '\0';
	if (year == 0)
		year = 1;
	LoadDateQualifiers();
	Assert(g_fDoneDateQual);
	if ((m_nMode & DPK_ALLOW_BC) && _tcscmp(z, g_rgchDateBC_AD[0]) == 0)
		year = -year;
	else if (z[0] != '\0' && _tcscmp(z, g_rgchDateBC_AD[1]) != 0) // "AD" optional
		year = 0; // error
	else if (year < m_kMinYear || year > m_kMaxYear)
		year = 0; // out of range
	return year;
}

/*----------------------------------------------------------------------------------------------
	Format the year value appropriate in string form.

	@param char y[m_kYearMaxLen] Output buffer for containing the year string.
	@param year Desired year (negative means BC).
	@param month Desired month in that year.
----------------------------------------------------------------------------------------------*/
void DatePickDlg::FormatYear(achar y[m_kYearMaxLen], int year, int month)
{
	LoadDateQualifiers();
	Assert(g_fDoneDateQual);
	if (year < 0)
		_stprintf_s(y, m_kYearMaxLen, _T("%d %s"), -year, g_rgchDateBC_AD[0]);
	else if (100*year+month < m_nMinCalYyyyMm)
		_stprintf_s(y, m_kYearMaxLen, _T("%s %d"), g_rgchDateBC_AD[1], year);
	else
		_stprintf_s(y, m_kYearMaxLen, _T("%d"), year);
}
