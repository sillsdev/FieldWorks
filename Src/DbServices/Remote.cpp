/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, SIL International. All rights reserved.

File: Remote.cpp
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	Implementation of Database Remote Services

	This file contains class definitions for the following class:
		RemoteDbWarn

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


static const int64 kn100NanoSeconds = 10 * 1000 * 1000;

//:>********************************************************************************************
//:>	RemoteDbWarn methods
//:>********************************************************************************************
RemoteDbWarn::RemoteDbWarn()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();

	// Check if an advanced user has already set flags for this:
	HKEY hKey;
	m_fPermissionConfigured = false;
	if (::RegOpenKeyEx(HKEY_LOCAL_MACHINE, _T("Software\\SIL\\FieldWorks"), 0, KEY_ALL_ACCESS,
		&hKey) == ERROR_SUCCESS)
	{
		BYTE b;
		DWORD cb = isizeof(b);
		DWORD dwT;
		if (::RegQueryValueEx(hKey, _T("DbWarnConfigured"), NULL, &dwT, &b, &cb)
			== ERROR_SUCCESS)
		{
			Assert(dwT == REG_BINARY);
			if (bool(b))
			{
				// Some user or other is claiming to have configured the correct permissions
				// already:
				m_fPermissionConfigured = true;
			}
		}
		else // Key value could not be read
		{
			// Write key value as 'false':
			b = 0;
			RegSetValueEx(hKey, _T("DbWarnConfigured"), 0, REG_BINARY, &b, cb);
		}
		RegCloseKey(hKey);
	}
}

RemoteDbWarn::~RemoteDbWarn()
{
	ModuleEntry::ModuleRelease();
	if (m_qctdd)
		m_qctdd->DestroyHwnd();
}

//:>********************************************************************************************
//:>    Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.DbServices.Remote"),
	&CLSID_FwRemote,
	_T("SIL database remote warnings"),
	_T("Apartment"),
	&RemoteDbWarn::CreateCom);


void RemoteDbWarn::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
	{
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	}
	ComSmartPtr<RemoteDbWarn> qzrdbw;
	qzrdbw.Attach(NewObj RemoteDbWarn);		// ref count initially 1
	CheckHr(qzrdbw->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	IRemoteDbWarn - IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP RemoteDbWarn::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IRemoteDbWarn *>(this));
	else if (iid == IID_IRemoteDbWarn)
		*ppv = static_cast<IRemoteDbWarn *>(this);
	else if (iid == IID_IDbWarnSetup)
		*ppv = static_cast<IDbWarnSetup *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(
			static_cast<IUnknown *>(static_cast<IRemoteDbWarn *>(this)), IID_IRemoteDbWarn);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


STDMETHODIMP_(ULONG) RemoteDbWarn::AddRef(void)
{
	Assert(m_cref > 0);
	::InterlockedIncrement(&m_cref);
	return m_cref;
}


STDMETHODIMP_(ULONG) RemoteDbWarn::Release(void)
{
	Assert(m_cref > 0);
	ulong cref = ::InterlockedDecrement(&m_cref);
	if (!cref)
	{
		m_cref = 1;
		delete this;
	}
	return cref;
}


//:>********************************************************************************************
//:>	IRemoteDbWarn Methods
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Issues a warning message for the user to acknowledge.

	@param bstrMessage Context-sensitive message to display.
	@param nFlags Standard MessageBox flags (e.g. MB_OK, MB_YESNO...)
	@param pnResponse MessageBox response id.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RemoteDbWarn::WarnSimple(BSTR bstrMessage, int nFlags, int * pnResponse)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrMessage);
	ChkComOutPtr(pnResponse);

	StrApp str(bstrMessage);
	*pnResponse = ::MessageBox(NULL, str.Chars(), _T(""), nFlags);

	END_COM_METHOD(g_fact, IID_IRemoteDbWarn);
}

/*----------------------------------------------------------------------------------------------
	Issues a warning message with a timeout counter that auto-decrements. Message box is removed
	when the timeout reaches zero, or when a call to ${RemoteDbWarn#Cancel} is made.

	@param bstrMessage Context-sensitive message to display.
	@param nTimeLeft Number of seconds to count down before auto-canceling.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RemoteDbWarn::WarnWithTimeout(BSTR bstrMessage, int nTimeLeft)
{
	BEGIN_COM_METHOD;

	ChkComBstrArgN(bstrMessage);

	// Create count-down dialog:
	m_qctdd.Create();
	m_qctdd->Init(bstrMessage, nTimeLeft);
	m_qctdd->DoModeless(NULL);

	END_COM_METHOD(g_fact, IID_IRemoteDbWarn);
}

/*----------------------------------------------------------------------------------------------
	Destroys current warning dialog.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RemoteDbWarn::Cancel()
{
	BEGIN_COM_METHOD;

	AssertPtr(m_qctdd);
	m_qctdd->Cancel();

	END_COM_METHOD(g_fact, IID_IRemoteDbWarn);
}


//:>********************************************************************************************
//:>	IDbWarnSetup Methods
//:>********************************************************************************************

// Arbitrary GUID for the AppID:
GUID RemoteDbWarn::s_guidAppId =
	{ 0x008B97C0, 0x6C58, 0x4C75, { 0xB0, 0x7F, 0x9C, 0xD5, 0x84, 0x7C, 0x27, 0x77 } };

/*----------------------------------------------------------------------------------------------
	Sets up access permissions for remote users to produce warning dialogs on our computer.
	This is done by writing various data into the registry, specifically the AppID section for
	CLSID_FwRemote.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RemoteDbWarn::PermitRemoteWarnings()
{
	BEGIN_COM_METHOD;

	if (m_fPermissionConfigured)
	{
		// Some user or other is claiming to have configured the correct permissions already:
		return S_OK;
	}

	HKEY hKey;
	StrApp strKey;

	// Make sure that the AppID is referenced under the CLSID for FwRemote:
	strKey.Format(_T("CLSID\\{%g}"), &CLSID_FwRemote);
	if (ERROR_SUCCESS != ::RegCreateKeyEx(HKEY_CLASSES_ROOT, strKey.Chars(), 0, NULL,
		REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, NULL, &hKey, NULL))
	{
		return E_FAIL;
	}
	StrApp strGuid;
	strGuid.Format(_T("{%g}"), &s_guidAppId);
	::RegSetValueEx(hKey, _T("AppId"), 0, REG_SZ, (BYTE *)strGuid.Chars(),
		(strGuid.Length()+1) * isizeof(achar));
	::RegCloseKey(hKey);

	// Set up our AppID key:
	strKey.Format(_T("AppID\\{%g}"), &s_guidAppId);
	if (ERROR_SUCCESS != ::RegCreateKeyEx(HKEY_CLASSES_ROOT, strKey.Chars(), 0, NULL,
		REG_OPTION_VOLATILE, KEY_ALL_ACCESS, NULL, &hKey, NULL))
	{
		// Just return. There is no point in informing the user. Anyone wishing to warn us
		// will discover that we can't be notified.
		return E_FAIL;
	}
	// Initialize data.
	// This is the binary data that represents "Everyone" having permission:
	const BYTE rgb[] = { 0x01, 0x00, 0x04, 0x80, 0x30, 0x00, 0x00, 0x00, 0x4C, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00, 0x02, 0x00, 0x1C, 0x00, 0x01, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x05, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x05, 0x15, 0x00, 0x00, 0x00, 0xA0, 0x5F, 0x84, 0x1F, 0x5E, 0x2E, 0x6B, 0x49,
		0xCE, 0x12, 0x03, 0x03, 0xF4, 0x01, 0x00, 0x00, 0x01, 0x05, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x05, 0x15, 0x00, 0x00, 0x00, 0xA0, 0x5F, 0x84, 0x1F, 0x5E, 0x2E, 0x6B, 0x49,
		0xCE, 0x12, 0x03, 0x03, 0xF4, 0x01, 0x00, 0x00 };
	DWORD dwZero = 0;
	StrApp strUser = _T("Interactive User");
	StrApp strTitle = _T("SIL Remote Services");
	// This title is not absolutely necessary, but it does help us locate our settings:
	::RegSetValueEx(hKey, NULL, 0, REG_SZ, (BYTE *)strTitle.Chars(),
		(strTitle.Length() + 1) * isizeof(achar));
	// This enables everyone to access FwRemote on our machine:
	::RegSetValueEx(hKey, _T("AccessPermission"), 0, REG_BINARY, rgb, isizeof(rgb));
	// This sets the default authentication level (None, I think):
	::RegSetValueEx(hKey, _T("AuthenticationLevel"), 0, REG_DWORD, (BYTE *)&dwZero,
		isizeof(dwZero));
	// This specifies that the default surrogate process (Dllhost.exe) be launched when an
	// FwRemote class is instantiated. Note that specifying a NULL value does not work:
	::RegSetValueEx(hKey, _T("DllSurrogate"), 0, REG_SZ, (BYTE *)_T(""), isizeof(achar));
	// This enables everyone to access FwRemote on our machine:
	::RegSetValueEx(hKey, _T("LaunchPermission"), 0, REG_BINARY, rgb, isizeof(rgb));
	// This enables remote callers to put messages up on our screen:
	::RegSetValueEx(hKey, _T("RunAs"), 0, REG_SZ, (BYTE *)strUser.Chars(),
		(strUser.Length()+1) * isizeof(achar));
	::RegCloseKey(hKey);

	END_COM_METHOD(g_fact, IID_IDbWarnSetup);
}

/*----------------------------------------------------------------------------------------------
	This removes the setting from the registry that would have allowed remote users to produce
	warning dialogs on our computer.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RemoteDbWarn::RefuseRemoteWarnings()
{
	BEGIN_COM_METHOD;

	if (m_fPermissionConfigured)
	{
		// Some user or other is claiming to have configured the correct permissions already:
		return S_OK;
	}

	StrApp strKey;
	strKey.Format(_T("AppID\\{%g}"), &s_guidAppId);
	::RegDeleteKey(HKEY_CLASSES_ROOT, strKey.Chars());

	END_COM_METHOD(g_fact, IID_IDbWarnSetup);
}




//:>********************************************************************************************
//:>	CountdownDlg methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CountdownDlg::CountdownDlg()
{
	m_rid = kridRmtWnCountdownDlg;
	m_pszHelpUrl = NULL;
	m_nTimeRemaining = 0;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CountdownDlg::~CountdownDlg()
{
	::KillTimer(m_hwnd, 1);

	if (m_hfntLargeNumberFont)
	{
		AfGdi::DeleteObjectFont(m_hfntLargeNumberFont);
		m_hfntLargeNumberFont = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Initialization.

	@param bstrMessage Context-sensitive message to display.
	@param nTimeLeft Number of seconds to count down before auto-canceling.
----------------------------------------------------------------------------------------------*/
void CountdownDlg::Init(BSTR bstrMessage, int nTimeLeft)
{
	m_strMessage.Assign(bstrMessage);
	m_nTimeRemaining = nTimeLeft;

	// Create the font we will use for large numbers:
	LOGFONT lfnt;
	HFONT hFont = (HFONT)GetStockObject(SYSTEM_FONT);
	GetObject(hFont, isizeof(LOGFONT), &lfnt);
	lfnt.lfHeight = 20;
	_tcscpy_s(lfnt.lfFaceName, _T("MS Sans Serif"));
	Assert(m_hfntLargeNumberFont == NULL);
	m_hfntLargeNumberFont = AfGdi::CreateFontIndirect(&lfnt);
}


/*----------------------------------------------------------------------------------------------
	Process window messages for the static text font and color, and monitor timer.
----------------------------------------------------------------------------------------------*/
bool CountdownDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(m_hwnd);

	switch (wm)
	{
	case WM_CTLCOLORSTATIC:
		if ((HWND)lp == ::GetDlgItem(m_hwnd, kctidRmtTimeLeft))
		{
			// This enables us to set the color and font of the time-left number.
			::SetTextColor((HDC)wp, kclrRed);
			::SetBkColor((HDC)wp, GetSysColor(COLOR_3DFACE));
			if (m_hfntLargeNumberFont)
				AfGdi::SelectObjectFont((HDC)wp, m_hfntLargeNumberFont);
			// This next line signals to Windows that we've altered the device context,
			// as well as telling it to stick with the dialog color for unused space within
			// static control.
			lnRet = (long)GetSysColorBrush(COLOR_3DFACE);
			return true;
		}
		break;
	case WM_TIMER:
		OnTimer(wp);
		return true;
	default:
		break;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they need
	initial values.)
----------------------------------------------------------------------------------------------*/
bool CountdownDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	Assert(m_hwnd);

	// Load icon:
	HICON hicon	= ::LoadIcon(NULL, IDI_EXCLAMATION);
	if (hicon)
	{
		HWND hwnd = ::GetDlgItem(m_hwnd, kridRmtWnIcon);
		::SendMessage(hwnd, STM_SETICON, (WPARAM)hicon, (LPARAM)0);
	}

	// Write message:
	::SendDlgItemMessage(m_hwnd, kridRmtWnMessage, WM_SETTEXT, 0, (LPARAM)m_strMessage.Chars());

	// Determine time to finish:
	SYSTEMTIME systTime;
	GetLocalTime(&systTime);
	// Change format of local time to one we can use to compare with another:
	FILETIME filtTime;
	SystemTimeToFileTime(&systTime, &filtTime);
	m_nEndTime = *((int64 *)(&filtTime)) + m_nTimeRemaining * kn100NanoSeconds;

	::SetTimer(m_hwnd, 1, 100, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	See if we've run out of time yet.
----------------------------------------------------------------------------------------------*/
void CountdownDlg::OnTimer(UINT)
{
	// Get the current time:
	SYSTEMTIME systTime;
	FILETIME filtTime;
	GetLocalTime(&systTime);
	SystemTimeToFileTime(&systTime, &filtTime);

	if (*((int64 *)(&filtTime)) + m_nTimeRemaining * kn100NanoSeconds > m_nEndTime)
	{
		Assert(m_hwnd);
		m_nTimeRemaining--;
		if (m_nTimeRemaining <= 0)
		{
			// Right. Time's up!
			::KillTimer(m_hwnd, 1);
			SuperClass::OnApply(true);
			return;
		}
		StrApp str;
		str.Format(_T("%d"), m_nTimeRemaining);
		::SendDlgItemMessage(m_hwnd, kctidRmtTimeLeft, WM_SETTEXT, 0, (LPARAM)str.Chars());

		if (m_fCanceled)
		{
			SuperClass::OnApply(true);
			return;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Signal that dialog is to die. (This will be picked up in ${ForcedDisconnectionDlg#OnTimer}
----------------------------------------------------------------------------------------------*/
void CountdownDlg::Cancel()
{
	m_fCanceled = true;
}


/*----------------------------------------------------------------------------------------------
	This is our opportunity to prevent the user from getting rid of our warning message.
----------------------------------------------------------------------------------------------*/
bool CountdownDlg::OnCancel()
{
	if (m_nTimeRemaining > 0)
		return false;

	return true;
}
