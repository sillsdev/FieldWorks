/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Disconnect.cpp
Responsibility: Alistair Imrie
Last reviewed: never

Description:
	This file contains the implementation for interface IDisconnectDb, which enables users to
	disconnect others from their database.

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE


static const int64 kn100NanoSeconds = 10 * 1000 * 1000;


// Handle instantiating collection class methods.
#include "Vector_i.cpp"

//:>********************************************************************************************
//:>	DisconnectDb methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
DisconnectDb::DisconnectDb()
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;

	m_rid = kridConnectionsDlg;
	m_pszHelpUrl = _T("Basic_Tasks/Collaborating_with_Others/Fieldworks_Shutdown_Progress.htm");
	m_hfntLargeNumberFont = NULL;
	m_himlNetwork = NULL;
	m_fOfferNotify = false;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
DisconnectDb::~DisconnectDb()
{
	::KillTimer(m_hwnd, 1);
	if (m_hfntLargeNumberFont)
	{
		AfGdi::DeleteObjectFont(m_hfntLargeNumberFont);
		m_hfntLargeNumberFont = NULL;
	}
	if (m_himlNetwork)
	{
		AfGdi::ImageList_Destroy(m_himlNetwork);
		m_himlNetwork = NULL;
	}
	m_qodc.Clear();
	m_qode.Clear();

	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.DbServices.Disconnect"),
	&CLSID_FwDisconnect,
	_T("SIL database disconnector"),
	_T("Apartment"),
	&DisconnectDb::CreateCom);


void DisconnectDb::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
	{
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	}
	ComSmartPtr<DisconnectDb> qzdscdb;
	qzdscdb.Attach(NewObj DisconnectDb);		// ref count initially 1
	CheckHr(qzdscdb->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	DisconnectDb - IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP DisconnectDb::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IDisconnectDb *>(this));
	else if (iid == IID_IDisconnectDb)
		*ppv = static_cast<IDisconnectDb *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(
			static_cast<IUnknown *>(static_cast<IDisconnectDb *>(this)), IID_IDisconnectDb);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


STDMETHODIMP_(ULONG) DisconnectDb::AddRef(void)
{
	Assert(m_cref > 0);
	::InterlockedIncrement(&m_cref);
	return m_cref;
}


STDMETHODIMP_(ULONG) DisconnectDb::Release(void)
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
//:>	IDisconnectDb Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize class members.
	@param bstrDatabase Name of database to disconnect people from.
	@param bstrServer Name of server where database is located.
	@param bstrReason The reason to give to user to explain disconnection.
	@param bstrExternalReason The explanation to give remote users for the disconnection.
	@param fConfirmCancel True if user must confirm a cancelation request.
	@param bstrCancelQuestion The question to ask if user must confirm cancelation request.
	@param hwndParent Handle to parent window to use for dialog(s).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DisconnectDb::Init(BSTR bstrDatabase, BSTR bstrServer,
								BSTR bstrReason, BSTR bstrExternalReason,
								ComBool fConfirmCancel, BSTR bstrCancelQuestion,
								int hwndParent)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrDatabase);
	ChkComBstrArg(bstrServer);
	ChkComBstrArg(bstrReason);
	ChkComBstrArg(bstrExternalReason);
	ChkComBstrArgN(bstrCancelQuestion);

	m_stuDatabase.Assign(bstrDatabase);
	m_strReason.Assign(bstrReason);
	m_fConfirmCancel = (bool)fConfirmCancel;
	m_strCancelQuestion.Assign(bstrCancelQuestion);
	m_hwndParent = (HWND)hwndParent;

	StrUni stuFmt(kstidRmtWnWarning);
	m_stuWarning.Format(stuFmt.Chars(), bstrDatabase, bstrExternalReason);

	m_vcondat.Clear();
	m_fOfferNotify = false;

	// Get access to the master database on the specified server:
	HRESULT hr;
	StrUni stuMasterDB = L"master";
	m_qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(hr = m_qode->Init(bstrServer, stuMasterDB.Bstr(), NULL, koltMsgBox,
		koltvForever));
	CheckHr(hr = m_qode->CreateCommand(&m_qodc));

	// Create the font we will use for large numbers:
	LOGFONT lfnt;
	HFONT hFont = (HFONT)GetStockObject(SYSTEM_FONT);
	GetObject(hFont, isizeof(LOGFONT), &lfnt);
	lfnt.lfHeight = 20;
	_tcscpy_s(lfnt.lfFaceName, _T("MS Sans Serif"));
	Assert(m_hfntLargeNumberFont == NULL);
	m_hfntLargeNumberFont = AfGdi::CreateFontIndirect(&lfnt);

	// Get our own name:
	DWORD nBufSize = MAX_COMPUTERNAME_LENGTH + 1;
	achar rgchBuffer[MAX_COMPUTERNAME_LENGTH + 1];
	GetComputerName(rgchBuffer, &nBufSize);
	m_stuHostName.Assign(rgchBuffer);

	// Get the image list, so we can display 'computer' icons:
	if (!m_himlNetwork)
		m_himlNetwork = AfGdi::ImageList_Create(17, 17, ILC_COLORDDB | ILC_MASK, 5, 5);
	HBITMAP hbmpImageNetwork = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(),
		MAKEINTRESOURCE(kridImagesNetwork));
	ImageList_AddMasked(m_himlNetwork, hbmpImageNetwork, RGB(255,255,255));
	AfGdi::DeleteObjectBitmap(hbmpImageNetwork);

	END_COM_METHOD(g_fact, IID_IDisconnectDb);
}

/*----------------------------------------------------------------------------------------------
	Check who is connected to our database.
	@param pnResponse [out] An enumerated type e.g. kNobodyConnected, kOnlyMeConnected etc.
	(Could also return kError.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DisconnectDb::CheckConnections(int * pnResponse)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnResponse);

	*pnResponse = kError; // Expect the worst!
	StrUni stuSql;

	// Clear connection counts:
	for (int i = 0; i < m_vcondat.Size(); i++)
		m_vcondat[i].m_nNumConnections = 0;
	m_nNumOwnConnections = 0;

	// Find out which other machines already have connections to the database:
	StrUni stuDb(m_stuDatabase);
	StrUtil::FixForSqlQuotedString(stuDb);	// See LT-9115.
	stuSql.Format(L"select rtrim([sproc].[hostname]) [computer], "
		L"rtrim([sproc].[nt_domain]) + '\\' + rtrim([sproc].[nt_username]) "
		L"from sysprocesses [sproc] "
		L"join sysdatabases [sdb] "
		L"on [sdb].[dbid] = [sproc].[dbid] and [name] = '%s' order by [computer]",
		stuDb.Chars());
	HRESULT hr;
	CheckHr(hr = m_qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(hr = m_qodc->GetRowset(0));
	ComBool fIsNull;
	ComBool fMoreRows;
	UINT cbSpaceTaken;
	CheckHr(hr = m_qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		wchar rgchMachineName[MAX_PATH];
		CheckHr(hr = m_qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchMachineName),
			isizeof(rgchMachineName), &cbSpaceTaken, &fIsNull, 2));
		if (!fIsNull)
		{
			// Check if we have already recorded a connection from this machine:
			bool fFound = false;
			if (m_stuHostName.EqualsCI(rgchMachineName))
			{
				fFound = true;
				m_nNumOwnConnections++;
			}
			else
			{
				for (int i = 0; !fFound && i < m_vcondat.Size(); i++)
				{
					if (m_vcondat[i].m_stuHostName.EqualsCI(rgchMachineName))
					{
						m_vcondat[i].m_nNumConnections++;
						fFound = true;
					}
				}
			}
			if (!fFound)
			{
				// add to array:
				ConnectionData condat;
				condat.m_nNumConnections = 1;
				condat.m_stuHostName = rgchMachineName;
				wchar rgchNTName[MAX_PATH];
				CheckHr(hr = m_qodc->GetColValue(2, reinterpret_cast <BYTE *>(rgchNTName),
					isizeof(rgchNTName), &cbSpaceTaken, &fIsNull, 2));
				if (!fIsNull)
					condat.m_stuNTDomainUser = rgchNTName;
				m_vcondat.Push(condat);
			}
		}
		// Get the next row:
		CheckHr(hr = m_qodc->NextRow(&fMoreRows));
	}

	// Check for recently disappeared connections (counter will be zero):
	for (int i = 0; i < m_vcondat.Size(); i++)
	{
		if (m_vcondat[i].m_nNumConnections == 0)
		{
			// Cancel the remote warning:
			if (m_vcondat[i].m_qzrdbw && m_vcondat[i].m_fWarned)
				m_vcondat[i].m_qzrdbw->Cancel();
		}
	}

	// Collect garbage:
	for (int i = m_vcondat.Size() - 1; i >= 0; i--)
		if (m_vcondat[i].m_nNumConnections == 0)
			m_vcondat.Delete(i);

	// See if there were no other users on our database:
	if (m_vcondat.Size() == 0 && m_nNumOwnConnections == 0)
		*pnResponse = kNobodyConnected;
	else if (m_vcondat.Size() != 0 && m_nNumOwnConnections == 0)
		*pnResponse = kOnlyOutsidersConnected;
	else if (m_vcondat.Size() == 0 && m_nNumOwnConnections != 0)
		*pnResponse = kOnlyMeConnected;
	else
		*pnResponse = kMeAndOutsidersConnected;

	END_COM_METHOD(g_fact, IID_IDisconnectDb);
}

/*----------------------------------------------------------------------------------------------
	Tells user about other people connected to the database that some destructive task is about
	to happen, and gives those other users an opportunity to exit gracefully. Finally, it kicks
	off insufferably stubborn users.
	@param pfResult True if all people got disconnected, false if something failed.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DisconnectDb::DisconnectAll(ComBool * pfResult)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfResult);

	int nResponse;
	CheckConnections(&nResponse);
	switch (nResponse)
	{
	case kOnlyOutsidersConnected: // Fall through
	case kMeAndOutsidersConnected:
		m_fOfferNotify = true;
		break;
	case kNobodyConnected:
		// There is no need for this dialog, as nobody is connected:
		*pfResult = (ComBool)true;
		return S_OK;
	case kOnlyMeConnected:
		// No special treatment:
		*pfResult = (ComBool)true;
		return S_OK;
	case kError:
		// Pretend user canceled:
		*pfResult = (ComBool)false;
		return S_OK;
	default:
		Assert(false);
		*pfResult = (ComBool)false;
		return S_OK;
	}
	// Run the Disconnection dialog:
	if (DoModal(m_hwndParent) == 2)
	{
		// User canceled:
		*pfResult = (ComBool)false;
	}
	else
	{
		*pfResult = (ComBool)true;
		CheckConnections(&nResponse);
		if (nResponse != kNobodyConnected)
		{
			// Could not disconnect everyone, for some unknown reason.
			StrAppBuf strbMessage(kstidPurgeConnsFail);
			StrAppBuf strbTitle(kstidFwTitle);
			::MessageBox(m_hwndParent, strbMessage.Chars(), strbTitle.Chars(), MB_ICONSTOP | MB_OK);
			*pfResult = (ComBool)false;
		}
	}
	END_COM_METHOD(g_fact, IID_IDisconnectDb);
}

/*----------------------------------------------------------------------------------------------
	Forceably disconnect all users from our database.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DisconnectDb::ForceDisconnectAll()
{
	BEGIN_COM_METHOD;

	StrUni stuSql;
	StrUni stuSqlKill;

	if (m_hwnd)
	{
		StrApp str(kstidPurgeConnections);
		::SendDlgItemMessage(m_hwnd, kctidDisconnectWarnText, WM_SETTEXT, 0, (long)str.Chars());
	}

	// Turn on the hourglass (wait) cursor. This will automatically disappear when this
	// function is exited (via the objects' destructors).
	WaitCursor wc;

	// Make a list of MSDE system process IDs to the database:
	Vector<short> vnSpIDs;

	stuSql.Assign(L"select distinct [spid] from sysprocesses [sproc] "
		L"join sysdatabases [sdb] "
		L"on [sdb].[dbid] = [sproc].[dbid] and [name] = ?");
	HRESULT hr;
	// It's safest to pass the database name as a parameter, because it may contain apostrophes.
	// See LT-8910.
	CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
		(BYTE *)(m_stuDatabase.Chars()), m_stuDatabase.Length() * sizeof(OLECHAR)));
	CheckHr(hr = m_qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(hr = m_qodc->GetRowset(0));
	ComBool fIsNull;
	ComBool fMoreRows;
	UINT cbSpaceTaken;
	CheckHr(hr = m_qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		short nSpID;
		CheckHr(hr = m_qodc->GetColValue(1, reinterpret_cast <BYTE *> (&nSpID), isizeof(nSpID),
			&cbSpaceTaken, &fIsNull, 0));
		if (!fIsNull)
			// add to array...
			vnSpIDs.Push(nSpID);
		// Get the next row
		CheckHr(hr = m_qodc->NextRow(&fMoreRows));
	}
	// Now kill all connections listed in our array:
	for (int i = 0; i < vnSpIDs.Size(); i++)
	{
		stuSqlKill.Format(L"KILL %d", (int)(vnSpIDs[i]));
		CheckHr(hr = m_qodc->ExecCommand(stuSqlKill.Bstr(), knSqlStmtNoResults));
	}

	// Now wait until the processes are actually killed before going on.
	do
	{
		// The next statement takes 10 seconds to execute. I'm (Ken) not sure
		// if that is because it takes several seconds to kill the connection, or whether we
		// are not getting m_qodc initialized properly. It might be better to pass in a
		// qode instead of a qodc, but since it seems to work as is, I'll let it go for now.
		CheckHr(hr = m_qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(hr = m_qodc->GetRowset(0));
		CheckHr(hr = m_qodc->NextRow(&fMoreRows));
	} while (fMoreRows);

	END_COM_METHOD(g_fact, IID_IDisconnectDb);
}

/*----------------------------------------------------------------------------------------------
	Process window messages for the static text font and color, and keep an eye on who is still
	connected to 'our' database.
----------------------------------------------------------------------------------------------*/
bool DisconnectDb::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(m_hwnd);

	switch (wm)
	{
	case WM_CTLCOLORSTATIC:
		if ((HWND)lp == ::GetDlgItem(m_hwnd, kctidTimeLeft))
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
bool DisconnectDb::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	Assert(m_hwnd);

	HICON hicon = ::LoadIcon(NULL, IDI_EXCLAMATION);
	if (hicon)
	{
		HWND hwnd = ::GetDlgItem(m_hwnd, kctidDisconnectWarnIcon);
		::SendMessage(hwnd, STM_SETICON, (WPARAM)hicon, (LPARAM)0);
	}

	// Make some controls invisible, depending on the state of m_fOfferNotify:
	NotificationOption(m_fOfferNotify);

	// Set up list view to display current connections:
	LVCOLUMN lvc;
	::ZeroMemory(&lvc, isizeof(lvc));
	lvc.mask = LVCF_WIDTH | LVCF_TEXT;
	HWND hwndList = ::GetDlgItem(m_hwnd, kctidConnectionList);

	// Get usable width of listview control:
	Rect rc;
	::GetClientRect(hwndList, &rc);
	int nWidth = rc.Width() - ::GetSystemMetrics(SM_CXVSCROLL);

	lvc.cx = (int)(nWidth * 0.6); // Use 60% of full width
	StrApp strTitle(kstidRemoteComputer);
	lvc.pszText = const_cast<achar *>(strTitle.Chars());
	ListView_InsertColumn(hwndList, 0, &lvc);

	lvc.mask |= LVCF_SUBITEM;
	lvc.iSubItem = 1;
	lvc.cx = (int)(nWidth * 0.4); // Use 40% of full width
	strTitle.Load(kstidRemoteStatus);
	lvc.pszText = const_cast<achar *>(strTitle.Chars());
	ListView_InsertColumn(hwndList, 1, &lvc);

	// Add the image list:
	if (m_himlNetwork)
	{
		HIMAGELIST himlOld = ListView_SetImageList(hwndList, m_himlNetwork, LVSIL_SMALL);
		if (himlOld)
			if (himlOld != m_himlNetwork)
				AfGdi::ImageList_Destroy(himlOld);
	}

	FillListView();

	if (!m_fOfferNotify)
	{
		// Automatically warn remotely-connected users of impending disconnection:
		NotifyAndCountDown();
	}

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Specify "not implemented" help topic if the class does not specify a help topic.
----------------------------------------------------------------------------------------------*/
SmartBstr DisconnectDb::GetHelpTopic()
{
	return _T("khtpDisconnectDb");
}

/*----------------------------------------------------------------------------------------------
	Notifies all remotely-connected users of impending doom, then commences the count-down
	operation.
----------------------------------------------------------------------------------------------*/
void DisconnectDb::NotifyAndCountDown()
{
	int i;

	// Notify remote users:
	for (i = 0; i < m_vcondat.Size(); i++)
	{
		ConnectionData condat = m_vcondat[i];
		HRESULT hr;

		MULTI_QI qi = {&IID_IRemoteDbWarn, NULL, S_OK};
		COSERVERINFO csi = {0, condat.m_stuHostName.Bstr(), NULL, 0};
		hr = CoCreateInstanceEx(CLSID_FwRemote, NULL, CLSCTX_REMOTE_SERVER, &csi, 1, &qi);
		m_vcondat[i].m_fWarnAttempted = true;
		if (SUCCEEDED(hr))
		{
			IRemoteDbWarn * pInt = static_cast <IRemoteDbWarn *>(qi.pItf);
			m_vcondat[i].m_qzrdbw.Attach(pInt);
			hr = m_vcondat[i].m_qzrdbw->WarnWithTimeout(m_stuWarning.Bstr(), knTimeOut);
			if (SUCCEEDED(hr))
				m_vcondat[i].m_fWarned = true;
		}
	}

	// Commence count-down:
	m_nTimeRemaining = knTimeOut;
	SYSTEMTIME systTime;
	GetLocalTime(&systTime);
	// Change format of local time to one we can use to compare with another:
	FILETIME filtTime;
	SystemTimeToFileTime(&systTime, &filtTime);
	// Determine time to finish with warning:
	m_nEndTime = *((int64 *)(&filtTime)) + m_nTimeRemaining * kn100NanoSeconds;

	::SetTimer(m_hwnd, 1, 100, 0);
}

/*----------------------------------------------------------------------------------------------
	Determines which of the overlapping controls should be visible and active.
	@param fFlag true if Notify button and explantory text should be shown.
	If fFlag is false, the countdown text and Force Now button will be shown.
----------------------------------------------------------------------------------------------*/
void DisconnectDb::NotificationOption(bool fFlag)
{
	HWND hwndCtrl;

	if (fFlag)
	{
		// Disable countdown text and Force Now button:
		hwndCtrl = ::GetDlgItem(m_hwnd, kctidDisconnectForceText1);
		::ShowWindow(hwndCtrl, SW_HIDE);
		::EnableWindow(hwndCtrl, false);
		hwndCtrl = ::GetDlgItem(m_hwnd, kctidDisconnectForceText2);
		::ShowWindow(hwndCtrl, SW_HIDE);
		::EnableWindow(hwndCtrl, false);
		hwndCtrl = ::GetDlgItem(m_hwnd, kctidTimeLeft);
		::ShowWindow(hwndCtrl, SW_HIDE);
		::EnableWindow(hwndCtrl, false);
		hwndCtrl = ::GetDlgItem(m_hwnd, kctidForceNow);
		::ShowWindow(hwndCtrl, SW_HIDE);
		::EnableWindow(hwndCtrl, false);

		// Enable Notify button and explantory text:
		hwndCtrl = ::GetDlgItem(m_hwnd, kctidDisconnectExplnText);
		::ShowWindow(hwndCtrl, SW_SHOW);
		::EnableWindow(hwndCtrl, true);
		hwndCtrl = ::GetDlgItem(m_hwnd, kctidDisconnectNotify);
		::ShowWindow(hwndCtrl, SW_SHOW);
		::EnableWindow(hwndCtrl, true);
	}
	else
	{
		// Disable Notify button and explantory text:
		hwndCtrl = ::GetDlgItem(m_hwnd, kctidDisconnectExplnText);
		::ShowWindow(hwndCtrl, SW_HIDE);
		::EnableWindow(hwndCtrl, false);
		hwndCtrl = ::GetDlgItem(m_hwnd, kctidDisconnectNotify);
		::ShowWindow(hwndCtrl, SW_HIDE);
		::EnableWindow(hwndCtrl, false);

		// Enable countdown text and Force Now button:
		hwndCtrl = ::GetDlgItem(m_hwnd, kctidDisconnectForceText1);
		::ShowWindow(hwndCtrl, SW_SHOW);
		::EnableWindow(hwndCtrl, true);
		hwndCtrl = ::GetDlgItem(m_hwnd, kctidDisconnectForceText2);
		::ShowWindow(hwndCtrl, SW_SHOW);
		::EnableWindow(hwndCtrl, true);
		hwndCtrl = ::GetDlgItem(m_hwnd, kctidTimeLeft);
		::ShowWindow(hwndCtrl, SW_SHOW);
		::EnableWindow(hwndCtrl, true);
		hwndCtrl = ::GetDlgItem(m_hwnd, kctidForceNow);
		::ShowWindow(hwndCtrl, SW_SHOW);
		::EnableWindow(hwndCtrl, true);
	}
}

/*----------------------------------------------------------------------------------------------
	Handle notifications.
----------------------------------------------------------------------------------------------*/
bool DisconnectDb::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertObj(this);
	AssertPtr(pnmh);
	Assert(m_hwnd);

	if (SuperClass::OnNotifyChild(ctid, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctid)
		{
		case kctidForceNow:
			// Force all connections off database:
			if (SUCCEEDED(ForceDisconnectAll()))
			{
				::KillTimer(m_hwnd, 1);
				SuperClass::OnApply(true);
			}
			else
			{
				SuperClass::OnCancel();
			}
			return true;
		case kctidDisconnectNotify:
			// Warn remote users and begin count-down:
			NotificationOption(false);
			NotifyAndCountDown();
			m_fOfferNotify = false;
			return true;
		default:
			// Do nothing:
			break;
		}
		break;
	case NM_CUSTOMDRAW:
		// This enables us to set colors for the list-view text:
		{ // Begin block
			NMCUSTOMDRAW * pnmcd = reinterpret_cast<NMCUSTOMDRAW *>(pnmh);
			if (pnmcd->dwDrawStage == CDDS_PREPAINT)
			{
				lnRet = CDRF_NOTIFYITEMDRAW;
				return true;
			}
			if (pnmcd->dwDrawStage == CDDS_ITEMPREPAINT)
			{
				lnRet = CDRF_NOTIFYSUBITEMDRAW;
				return true;
			}
			if (pnmcd->dwDrawStage & (CDDS_SUBITEM | CDDS_PREPAINT))
			{
				NMLVCUSTOMDRAW * pnmlvcd = reinterpret_cast<NMLVCUSTOMDRAW *>(pnmh);
				if (pnmlvcd->iSubItem)
				{
					int nIndex = pnmlvcd->nmcd.lItemlParam;
					if (nIndex >= 0 && nIndex < m_vcondat.Size())
					{
						if (m_vcondat[nIndex].m_fWarned)
							pnmlvcd->clrText = kclrGreen;
						else
							pnmlvcd->clrText = kclrRed;
						lnRet = CDRF_NEWFONT;
						return true;
					}
				}
			}
		} // End block
		return false;
	default:
		// Do nothing:
		break;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	User pressed cancel. See if they meant it.
----------------------------------------------------------------------------------------------*/
bool DisconnectDb::OnCancel()
{
	if (m_fConfirmCancel)
	{
		Assert(m_hwnd);
		StrAppBuf strbTitle(kstidFwTitle);

		if (::MessageBox(m_hwnd, m_strCancelQuestion.Chars(), strbTitle.Chars(),
			MB_ICONQUESTION | MB_YESNO) != IDYES)
		{
			return false;
		}
	}
	return SuperClass::OnCancel();
}


/*----------------------------------------------------------------------------------------------
	See if we've run out of time yet.
----------------------------------------------------------------------------------------------*/
void DisconnectDb::OnTimer(UINT)
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
			// Right. Time's up! Everybody out:
			::KillTimer(m_hwnd, 1);
			HRESULT hr;
			IgnoreHr(hr = ForceDisconnectAll());
			if (FAILED(hr))
				SuperClass::OnCancel();
			else
				SuperClass::OnApply(true);
			return;
		}

		FillListView();
		StrApp str;
		str.Format(_T("%d"), m_nTimeRemaining);
		::SendDlgItemMessage(m_hwnd, kctidTimeLeft, WM_SETTEXT, 0, (long)str.Chars());

		int nResponse;
		CheckConnections(&nResponse);
		if (nResponse == kNobodyConnected)
		{
			SuperClass::OnApply(true);
			return;
		}
		// See if any connections have recently appeared:
		for (int i = 0; i < m_vcondat.Size(); i++)
		{
			// Check for new connections (m_qzrdbw will be NULL):
			if (!m_vcondat[i].m_qzrdbw)
			{
				// Issue warning with whatever time is left minus 5 seconds safety gap:
				HRESULT hr;
				MULTI_QI qi = {&IID_IRemoteDbWarn, NULL, S_OK};
				COSERVERINFO csi = {0, m_vcondat[i].m_stuHostName.Bstr(), NULL, 0};
				hr = CoCreateInstanceEx(CLSID_FwRemote, NULL, CLSCTX_REMOTE_SERVER, &csi, 1,
					&qi);
				m_vcondat[i].m_fWarnAttempted = true;
				if (SUCCEEDED(hr))
				{
					IRemoteDbWarn * pInt = static_cast <IRemoteDbWarn *>(qi.pItf);
					m_vcondat[i].m_qzrdbw.Attach(pInt);
					hr = m_vcondat[i].m_qzrdbw->WarnWithTimeout(m_stuWarning.Bstr(),
						m_nTimeRemaining - 5);
					if (SUCCEEDED(hr))
						m_vcondat[i].m_fWarned = true;
				}
			} // End if connection is new
		} // Next connection record
	} // End if there is still time to go
}


/*----------------------------------------------------------------------------------------------
	Put the details of who's connected into our list box.
----------------------------------------------------------------------------------------------*/
void DisconnectDb::FillListView()
{
	Assert(m_hwnd);

	HWND hwndList = ::GetDlgItem(m_hwnd, kctidConnectionList);
	ListView_DeleteAllItems(hwndList);
	int nOwnOffset = 0;

	if (m_nNumOwnConnections > 0)
	{
		nOwnOffset = 1;
		LVITEM lvi = { LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM };
		StrApp str(m_stuHostName.Chars());
		lvi.iItem = 0;
		lvi.iSubItem = 0;
		lvi.iImage = kridImageComputer;
		lvi.pszText = const_cast<achar *>(str.Chars());
		lvi.lParam = -1;
		ListView_InsertItem(hwndList, &lvi);

		StrApp strYou(kstidDisconnectYou);
		ListView_SetItemText(hwndList, 0, 1, const_cast<achar *>(strYou.Chars()));
	}

	// Add each connection to list view:
	for (int i = 0; i < m_vcondat.Size(); i++)
	{
		LVITEM lvi = { LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM };
		ConnectionData condat = m_vcondat[i];
		lvi.iItem = i + nOwnOffset;
		lvi.iSubItem = 0;
		lvi.iImage = kridImageComputer;
		StrApp strHost(condat.m_stuHostName);
		lvi.pszText = const_cast<achar *>(strHost.Chars());
		lvi.lParam = i;
		ListView_InsertItem(hwndList, &lvi);

		StrApp strNotification;
		if (condat.m_fWarned)
			strNotification.Load(kstidRemoteNotified);
		else if (condat.m_fWarnAttempted)
			strNotification.Load(kstidRemoteUnableNotify);
		else
			strNotification.Load(kstidRemoteNotYetNotified);
		ListView_SetItemText(hwndList, i + nOwnOffset, 1,
			const_cast<achar *>(strNotification.Chars()));
	}

	// Now do the messages at the top of the list:
	StrApp strMessage;
	if (m_fOfferNotify)
	{
		strMessage.Assign(m_strReason);
	}
	else
	{
		int nConnections = m_vcondat.Size();
		if (m_nNumOwnConnections > 0)
			nConnections++;
		if (nConnections > 0)
		{
			StrApp strFormat(kstidDisconnectWait);
			StrApp strWait;
			strWait.Format(strFormat.Chars(), nConnections);
			strMessage.Append(strWait);
		}
	}
	::SendDlgItemMessage(m_hwnd, kctidDisconnectWarnText, WM_SETTEXT, 0, (long)strMessage.Chars());
}
