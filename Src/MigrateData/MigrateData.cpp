/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, 2005, SIL International. All rights reserved.

File: MigrateData.cpp
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	Implementation of database migration interface.

	This file contains class definitions for the following class:
		MigrateData

To use, you can either run as a COM object from within a program, or by using rundll32 from
outside.
To use as a COM object, add something like this to your code:
	#include "..\MigrateData\Main.h"
	...
	IOleDbEncapPtr qode;
	...(initialize qode)
	IMigrateDataPtr qmd;
	qmd.CreateInstance(CLSID_MigrateData);
	qmd->Migrate(qode, nSomeVersion);
To use from outside a program, e.g. on the command line, use something like this:
	rundll32 MigrateData.dll,ExtMigrate TestLangProj, 4
The syntax for the rundll32 is fairly flexible in that you can use spaces and/or a comma to
separate the database name from the intended version argument.

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

#include <tchar.h>
#include <sys/stat.h>		// For struct stat definition and stat() declaration.

#undef LOG_EVERY_COMMAND
//#define LOG_EVERY_COMMAND 1

// Struct for holding a row of a reference table.
// Hungarian: sd.
struct SrcDst
{
	HVO m_hvoSrc;
	HVO m_hvoDst;
};

//:>********************************************************************************************
//:>	DLL Method
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	This method is a way to call Migrate from the command line using the following command:
		rundll32 MigrateData.dll,ExtMigrate <database-name> <final-version-number>
----------------------------------------------------------------------------------------------*/
void CALLBACK ExtMigrate(HWND hwnd, HINSTANCE hinst, LPTSTR lpCmdLine, int nCmdShow)
{
	MigrateData md;

	// Extract the user's parameters: database name and desired version number:
	TCHAR * chSpace = _tcsrchr(lpCmdLine, (TCHAR)(' '));
	TCHAR * chComma = _tcsrchr(lpCmdLine, (TCHAR)(','));
	if (!chSpace && !chComma)
	{
		StrApp strCmdLine(lpCmdLine);
		md.ErrorBox(kstidMgdUsage, strCmdLine.Chars());
		return;
	}
	TCHAR * ch = chComma;
	if (chSpace > chComma)
		ch = chSpace;

	// Get desired version number:
	int nDestVersion = _tstoi(ch + 1);

	*ch = 0;
	while (!_istalnum(*(--ch)))
		*ch = 0;

	StrUni stuDatabase(lpCmdLine);
	int nSourceVersion = 0;
	if (S_OK != md._Migrate(stuDatabase.Bstr(), nDestVersion, &nSourceVersion))
	{
		StrApp strDatabase(stuDatabase.Chars());
		if (!nSourceVersion)
			md.ErrorBox(kstidMgdExtError2, strDatabase.Chars(), nDestVersion);
		else
		{
			md.ErrorBox(kstidMgdExtError, strDatabase.Chars(), nSourceVersion, nDestVersion);
		}
	}
	CoUninitialize();
}


//:>********************************************************************************************
//:>    Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Fw.MigrateData"),
	&CLSID_MigrateData,
	_T("SIL database upgrader"),
	_T("Apartment"),
	&MigrateData::CreateCom);


//:>********************************************************************************************
//:>	MigrateData methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
MigrateData::MigrateData()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();

	// Determine the root path for any files:
	m_stuFwData = DirectoryFinder::FwDatabaseDir();
	Assert(m_stuFwData.Length());
	if (!m_stuFwData.Length())
		m_stuFwData.Assign(L"C:\\");		// Set a reasonable default

	// Determine the local database server name.
	m_stuServer = SilUtil::LocalServerName();
	Assert(m_stuServer.Length());
	if (!m_stuServer.Length())
		m_stuServer = L".\\SILFW";
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
MigrateData::~MigrateData()
{
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
void MigrateData::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
	{
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	}
	ComSmartPtr<MigrateData> qzmd;
	qzmd.Attach(NewObj MigrateData);		// ref count initially 1
	CheckHr(qzmd->QueryInterface(riid, ppv));
}


/*----------------------------------------------------------------------------------------------
	QueryInterface - standard IUnknown method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP MigrateData::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IMigrateData *>(this));
	else if (iid == IID_IMigrateData)
		*ppv = static_cast<IMigrateData *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(
			static_cast<IUnknown *>(static_cast<IMigrateData *>(this)), IID_IMigrateData);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	AddRef - standard IUnknown method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) MigrateData::AddRef(void)
{
	Assert(m_cref > 0);
	::InterlockedIncrement(&m_cref);
	return m_cref;
}


/*----------------------------------------------------------------------------------------------
	Release - standard IUnknown method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) MigrateData::Release(void)
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


/*----------------------------------------------------------------------------------------------
	This method is called via the IMigrateData COM interface.

	@param pode Pointer to a database to be upgraded.
	@param nDestVersion The desired version to change the database to.
	@param fStillValid True if pode still valid afterwards.
	Some upgrades may fail if the initial reference count on pode is more than 1.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP MigrateData::Migrate(BSTR bstrDbName, int nDestVersion, IStream * pfist)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrDbName);
//	ChkComArgPtr(pfist);

	m_qfist = pfist;

	HRESULT hr;
	try
	{
		CheckHr(hr = _Migrate(bstrDbName, nDestVersion));
	}
	catch (Throwable& thr)
	{
		hr = thr.Result();
	}
	if (m_qprog)
	{
		m_qprog->DestroyHwnd();
		m_qprog.Clear();
	}
	return hr;

	END_COM_METHOD(g_fact, IID_IMigrateData);
}

#if DEBUG
/*----------------------------------------------------------------------------------------------
	Returns the AssertMessageBox value from the registry; if not set returns true
----------------------------------------------------------------------------------------------*/
bool GetShowAssertMessageBox()
{
	DWORD fShowAssertMessageBox = true;
	HKEY hk;
	if (::RegOpenKeyEx(HKEY_LOCAL_MACHINE, _T("Software\\SIL\\FieldWorks"), 0,
			KEY_QUERY_VALUE, &hk) == ERROR_SUCCESS)
	{
		DWORD cb = sizeof(fShowAssertMessageBox);
		DWORD dwT;
		::RegQueryValueEx(hk, _T("AssertMessageBox"), NULL, &dwT, (LPBYTE)&fShowAssertMessageBox,
			&cb);
		RegCloseKey(hk);
	}
	return fShowAssertMessageBox ? true : false; // otherwise we get a performance warning
}
#endif

/*----------------------------------------------------------------------------------------------
	Report error messages.
	@param rid The resource ID of the text message describing the error.
----------------------------------------------------------------------------------------------*/
void MigrateData::ErrorBox(int rid, ...)
{
	StrApp strTitle(kstidMgdError);
	StrApp strFormat(rid);

	// Format the string:
	StrApp strMessage;
	va_list argList;
	va_start(argList, rid);
	strMessage.FormatCore(strFormat.Chars(), strFormat.Length(),
		argList);
	va_end(argList);
#if DEBUG
	if (!::GetShowAssertMessageBox())
	{
		::OutputDebugString(strMessage.Chars());
		return;
	}
#endif
	::MessageBox(NULL, strMessage.Chars(), strTitle.Chars(), MB_ICONSTOP | MB_OK);
}


/*----------------------------------------------------------------------------------------------
	Load the version number from the database, using the provided command object.

	@param podc pointer to an OLEDB command object.
	@param pnVersion pointer to the version number (output)
----------------------------------------------------------------------------------------------*/
HRESULT MigrateData::GetVersionNumber(IOleDbCommand * podc, int * pnVersion)
{

	StrUni stuSql(L"select max(DbVer) from Version$");
	CheckHr(podc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(podc->GetRowset(1));

	ComBool fMoreRows;
	CheckHr(podc->NextRow(&fMoreRows));
	if (!fMoreRows)
		return E_FAIL;

	ComBool fIsNull;
	ULONG luSpaceTaken = 0;
	CheckHr(podc->GetColValue(1, reinterpret_cast <BYTE *>(pnVersion),
		sizeof(int), &luSpaceTaken, &fIsNull, 0));
	if (luSpaceTaken < sizeof(int) || fIsNull)
		return E_FAIL;

	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	This method is called internally, and is the one that actually does the work of upgrading.
	@param bstrDbName a database to be upgraded.
	@param nDestVersion [in] The desired version to change the database to.
	@param pnSourceVersion [out] The discovered version of the database prior to upgrading.
----------------------------------------------------------------------------------------------*/
HRESULT MigrateData::_Migrate(BSTR bstrDbName, int nDestVersion, int * pnSourceVersion)
{
	AssertPtr(bstrDbName);

	IOleDbEncapPtr qode; // Declare before qodc.
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(m_stuServer.Bstr(), bstrDbName, NULL, koltMsgBox, koltvForever));
	StrApp strDbName(bstrDbName);
	StrUni stuDbName(bstrDbName);

	IOleDbCommandPtr qodc;
	StrUni stuSql;
	ComBool fIsNull;
	ComBool fMoreRows;
	int nSourceVersion = 0;

	try
	{
		CheckHr(qode->CreateCommand(&qodc));
	}
	catch (Throwable& thr)
	{
		ErrorBox(kstidMgdQodcError);
		return thr.Result();
	}

	// Obtain the database version number (from Version$ table of the database):
	try
	{
		CheckHr(GetVersionNumber(qodc, &nSourceVersion));
	}
	catch (Throwable& thr)
	{
		ErrorBox(kstidMgdVersionError);
		return thr.Result();
	}

	// If anyone has a database with a version from 200001 through 200004, there's nothing we
	// can do (or more precisely, nothing we can be bothered to do). We didn't release any such
	// version outside our office, so goodness knows how they got it!
	if (nSourceVersion >= 200001 && nSourceVersion <= 200004)
	{
		ErrorBox(kstidMgdErr201To204Gap, nSourceVersion);
		return E_FAIL;
	}

	// Make sure the incremental path exists (all the SQL scripts, etc):
	WaitCursor wc;

	// Get a list and count of the intermediate flies to run.
	// this will allow checking and updating the progress bar with correct numbers
	bool done = false;
	Vector<StrApp> vfNames;			// vector of file names

	// The lowest incremental script needed depends on what the user's database version starts
	// at. If they start at 200000 or lower, we will get them to 200006 before the incemental
	// system kicks in. If they start at 200005 or higher, then the current version number is
	// used directly to determine the first script:
	int nStartVersionSearch;
	if (nSourceVersion <= 200000)
		nStartVersionSearch = 200006;
	else
		nStartVersionSearch = nSourceVersion;

	TCHAR strToVersion[7];
	// We use this buffer to copy counted characters into, so we need to guarantee a NULL
	// at the end:
	strToVersion[6] = 0;
	StrApp strFwRoot = DirectoryFinder::FwMigrationScriptDir();
	StrApp strFilePattern;
	WIN32_FIND_DATA wfd;
	HANDLE hFind;

	while (!done)
	{
		// expects incremental file updates to be named: FromVerToDestVer.sql
		// For example: 200012To200013.sql
		strFilePattern.Format(_T("%s\\%dTo*.sql"), strFwRoot.Chars(), nStartVersionSearch);
		hFind = ::FindFirstFile(strFilePattern.Chars(), &wfd);
		if (hFind == INVALID_HANDLE_VALUE)
		{
			done = true;
			break;
		}
		::FindClose(hFind);

		// pull the version information from the file name (caseless)
		StrApp sta(wfd.cFileName);
		sta.ToLower();
		const TCHAR * pstrToVersion = _tcsstr(sta.Chars(), _T("to"));
		Assert(pstrToVersion);

		pstrToVersion += 2;
		_tcsncpy_s(strToVersion, pstrToVersion, 6);	// copy the to version
		int nToVersion = _tstoi(strToVersion);
		if (nToVersion > nDestVersion)
		{
			// if there are additional data migration scripts in the directory we just
			// ignore them.
			done = true;
			break;
		}

		nStartVersionSearch = nToVersion;
		vfNames.Push(wfd.cFileName);		// save the file name
	}

	if (nStartVersionSearch != nDestVersion)
	{
		// couldn't go from current to appversion - missing files...
		ErrorBox(kstidMgdMissingIncrementalFile, nSourceVersion, nDestVersion,
			nStartVersionSearch);
		return E_FAIL;
	}

	if (nSourceVersion == 3)
	{
		// if the database version is the old version numbering scheme (3) then switch to
		// the new version number scheme (5000).  This is only to update the data notebook
		// milestone 5 which had a 3 as the version number.
		nSourceVersion = 5000;
	}
	else if (nSourceVersion == 100004)
	{
		// The temporary version 100004 is the same database format as the one
		// we finally settled on as 150000.
		nSourceVersion = 150000;
	}
	else if (nSourceVersion == 150001)
	{
		// The version was upped from 150001 to 200000 for release two with no
		// significant changes to the model.
		nSourceVersion = 200000;
	}


	if (pnSourceVersion)
		*pnSourceVersion = nSourceVersion;

	if (nSourceVersion >= nDestVersion)
	{
		ErrorBox(kstidMgdTooNewError, strDbName.Chars(), nDestVersion, nSourceVersion);
		return E_FAIL;
	}
	if (nSourceVersion < 5000)
	{
		Assert(false);
		return E_FAIL;
	}

	// get the current active window to center on.
	HWND hwndActive = ::GetActiveWindow();

	m_qprog.Create(); // Get progress dialog ready for different flow paths
	m_qprog->DoModeless(NULL);
	RECT rc;
	::GetWindowRect(m_qprog->Hwnd(), &rc);

	// Center the progress dialog over the current active window
	if (hwndActive != NULL)
	{
		RECT rcActive;
		::GetWindowRect(hwndActive, &rcActive);

		::SetWindowPos(m_qprog->Hwnd(), HWND_TOP,
			(rcActive.left + rcActive.right - rc.right + rc.left) / 2,
			(rcActive.bottom + rcActive.top - rc.bottom + rc.top) / 2,
			0, 0, SWP_NOSIZE);
	}
	// Make sure the progress dialog comes to the front. This is needed when the progress dialog
	// comes up after the splash screen.
	::SetForegroundWindow(m_qprog->Hwnd());
	StrApp strTitle(kstidMigratingTitle);
	strTitle.Append(L": ");
	strTitle.Append(stuDbName);
	m_qprog->SetTitle(strTitle.Chars());
	m_qprog->SetStep(1);
	switch (nSourceVersion)
	{
		case 5000:		m_qprog->SetRange(0, 12 + vfNames.Size());	break;
		case 100000:	m_qprog->SetRange(0, 10 + vfNames.Size());	break;
		case 150000:	m_qprog->SetRange(0, 6 + vfNames.Size());	break;
		// It should always be one of the above!
		default:		m_qprog->SetRange(0,20 + vfNames.Size());	break;
	}
	if (nSourceVersion >= 200002)
		m_qprog->SetRange(0, 3 + vfNames.Size());

	StrApp strMsg(kstidMigrateBackup);
	m_qprog->SetMessage(strMsg.Chars());

	// Make a backup of the current database:
	StrUni stuBackup(m_stuFwData);
	stuBackup.FormatAppend(L"\\%s-v%d.bak", bstrDbName, nSourceVersion);
	StrApp strBackup(stuBackup.Chars());
	DeleteFile(strBackup.Chars());
	StrUtil::FixForSqlQuotedString(stuBackup);	// See LT-9115.
	stuSql.Format(L"BACKUP DATABASE [%s] TO DISK = '%s' WITH INIT",
		bstrDbName, stuBackup.Chars());
	try
	{
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
	}
	catch(Throwable& thr)
	{
		ErrorBox(kstidMgdBackupError);
		return thr.Result();
	}
	qodc.Clear();
	if (m_qprog)
		m_qprog->StepIt();

	bool fMigrateOK = true;

	// The following comment does not apply after version number 200002 - we're supplying
	// upgrade scripts for incremental versions:
	// According to RAID #3876:
	// The plan is that any version number that ends with 000 should provide code to upgrade the
	// database, and should thus be in the condition chain. Any other version will simply state
	// something to the effect that the versions are different and can't be upgraded
	// automatically. [This occurs in ${AfDbApp#CheckDbVerCompatibility}]
	// We don't have the time to provide automatic upgrade paths for every
	// incremental change, yet we want to warn in-house testers that something has changed that
	// could invalidate older databases. The user can use a DOS command (db setversion 100001)
	// to change an older database and it may or may not work, depending on what change was
	// made.
	if (nSourceVersion == 5000) // Application version 0 Milestone 5
	{
		if (nDestVersion == 5000)
			return S_OK;
		StrApp strMsg(kstidMigrateM5toV1);
		m_qprog->SetMessage(strMsg.Chars());
		if (!M5toM6(qode)) // Historic name - takes us to version 100000
			fMigrateOK = false;
		nSourceVersion = 100000; // version 1; may be further upgraded.
		if (m_qprog)
			m_qprog->StepIt();
	}

	if (fMigrateOK && nSourceVersion == 100000)
	{
		StrApp strMsg(kstidMigrateV1toV15);
		m_qprog->SetMessage(strMsg.Chars());
		fMigrateOK = MigrateEncToWs(qode, bstrDbName);
		if (fMigrateOK)
			fMigrateOK = NormalizeUnicode(qode);
		nSourceVersion = 150000;
		if (m_qprog)
			m_qprog->StepIt();
	}

	if (fMigrateOK && nSourceVersion <= 100999) // Application version 1 and 1.1
	{
		if (nDestVersion <= 100999)
			return S_OK;

		// Somebody has made an update to the database without providing Migrate code:
		Assert(false);
	}

	if (fMigrateOK && nSourceVersion < 200000) // Application version 2.0
	{
		StrApp strMsg(kstidMigrateV15toV2);
		m_qprog->SetMessage(strMsg.Chars());
		fMigrateOK = MigrateWsCodeToId(qode, bstrDbName);	// Convert from 150000 to 200000.
		if (fMigrateOK)
			nSourceVersion = 200000;
		if (m_qprog)
			m_qprog->StepIt();
	}

	if (fMigrateOK && nSourceVersion == 200000) // Application version 2.0
	{
		StrApp strMsg(kstidMigrateV2toV200006);
		m_qprog->SetMessage(strMsg.Chars());
		// convert from 200000 to 200006:
		fMigrateOK = UpdateFrom200000To200006(qode, bstrDbName);
		if (fMigrateOK)
			fMigrateOK = LoadVersion2_6Data(qode, bstrDbName);
		if (fMigrateOK)
			nSourceVersion = 200006;
		if (m_qprog)
			m_qprog->StepIt();
	}

	// If the user's database has a version in the range 200001 to 200004 inclusive, we have
	// already told them we can't help them.

	// Begin the incremental upgrades, if everything before here went OK.
	int nVersion = 0;
	if (fMigrateOK)
	{
		StrApp strFmt(kstidMigrateIncremental);
		StrApp strMsg;
		strMsg.Format(strFmt, nSourceVersion, nDestVersion);
		m_qprog->SetMessage(strMsg.Chars());
		while (vfNames.Size() > 0)
		{
			StrApp fname = vfNames[0];	// use first element

			// Run the update script, which copies the data from the old database to the new,
			// and does a lot of the conversion work.
			bool fOk = RunSql(qode, fname.Chars());
			if (!fOk)
			{
				fMigrateOK = false;
				break;
			}
			if (m_qprog)
				m_qprog->StepIt();

			try
			{
				CheckHr(qode->CreateCommand(&qodc));
			}
			catch(Throwable& thr)
			{
				ErrorBox(kstidMgdQodcError);
				return thr.Result();
			}
			// Read the version number from the database, and compute the desired version
			// number from the filename.  (filenames look like "200100To200101.sql")
			GetVersionNumber(qodc, &nVersion);
			qodc.Clear();
			strMsg.Format(strFmt, nVersion, nDestVersion);
			m_qprog->SetMessage(strMsg.Chars());
			int nNewVersion = _tcstol(fname.Chars() + 8, NULL, 10);
			if (nVersion != nNewVersion)
			{
				ErrorBox(kstidMgdFailed, strDbName.Chars(), nSourceVersion, nDestVersion,
					nVersion);
				fMigrateOK = false;
				break;
			}
			vfNames.Delete(0);				// remove element from vector
			ProcessPossibleXmlUpdateFiles(qode, nVersion);
		}
	}

	// Now check the final version number to make sure that everything really is okay.
	if (fMigrateOK)
	{
		try
		{
			CheckHr(qode->CreateCommand(&qodc));
		}
		catch(Throwable& thr)
		{
			ErrorBox(kstidMgdQodcError);
			return thr.Result();
		}
		int nFinalVersion = 0;
		GetVersionNumber(qodc, &nFinalVersion);
		qodc.Clear();
		if (nFinalVersion != nDestVersion)
		{
			ErrorBox(kstidMgdFailed, strDbName.Chars(), nSourceVersion, nDestVersion,
				nFinalVersion);
			fMigrateOK = false;
		}
	}

	if (fMigrateOK && nSourceVersion < 200182)
	{
		// Fix an XML import bug that divided the BulNumFontInfo fontsize by 16.
		fMigrateOK = FixBulNumFontInfoFontSize(qode);
	}

	// Now for the final, version-independent cleanup, which really shouldn't ever be needed,
	// but we're all paranoid, right?
	if (fMigrateOK)
	{
		// This removes all objects with invalid Flid or Clid values, plus anything else that is
		// deemed appropriate.  See the SQL file for details.
		fMigrateOK = RunSql(qode, L"FinalCleanup.sql");
	}

	if (m_qprog)
	{
		StrApp strFmt(fMigrateOK ? kstidMigrationSucceededFmt : kstidMigrationFailedFmt);
		StrApp strDb(bstrDbName);
		StrApp strMsg;
		strMsg.Format(strFmt.Chars(), strDb.Chars());
		m_qprog->SetMessage(strMsg.Chars());
	}

/*	REVIEW: This probably isn't needed any more.
	// After all the updating is done, make sure that any languages in the database are
	// installed for use by ICU.
	if (fMigrateOK)
	{
		StrApp strMsg(kstidMigrateInstLangs);
		m_qprog->SetMessage(strMsg.Chars());

		fMigrateOK = InstallLanguages(qode);

		if (m_qprog)
			m_qprog->StepIt();
	}
*/
	if (fMigrateOK)
	{
		// We may have a bogus writing system XML file named "all analysis.xml".  If it exists,
		// delete it!
		StrUni stuBadFile = DirectoryFinder::FwRootDataDir();
		if (stuBadFile[stuBadFile.Length() - 1] != '\\')
			stuBadFile.Append(L"\\");
		stuBadFile.Append(L"Languages\\all analysis.xml");
		::DeleteFileW(stuBadFile.Chars());
	}

	if (!fMigrateOK)
	{
		// We're going to try to restore the temporary backup. First, we have to disconnect
		// from the existing version.
		qodc.Clear();
		qode.Clear();

		// Get a connection to the master database. Construct the server name:
		StrUni stuMasterDB = L"master";

		try
		{
			IOleDbEncapPtr qodeMaster; // Declare before qodc.
			qodeMaster.CreateInstance(CLSID_OleDbEncap);
			CheckHr(qodeMaster->Init(m_stuServer.Bstr(), stuMasterDB.Bstr(), NULL, koltMsgBox,
					koltvForever));
			CheckHr(qodeMaster->CreateCommand(&qodc));
			// Try to restore the backup:
			stuSql.Format(L"RESTORE DATABASE [%s] FROM DISK = '%s' WITH REPLACE",
				bstrDbName, stuBackup.Chars());
			CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
			if (m_qprog)
				m_qprog->DestroyHwnd();
		}
		catch (Throwable& thr)
		{
			ErrorBox(kstidMgdRestoreError, strBackup.Chars());
			return thr.Result();
		}

		return E_FAIL;
	}

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Upgrades data from version 1 to version 1.5 -- the primary change is merging LgEncoding
	and WritingSystem, with resulting changes from enc to ws as the main abbreviation.

	@param qode (in,out) Reference to a database to be upgraded.
	@param bstrOldDbName Name of the old database.

	@return True if all went well, otherwise false.
----------------------------------------------------------------------------------------------*/
bool MigrateData::MigrateEncToWs(IOleDbEncapPtr & qode,	BSTR bstrOldDbName)
{
	// Create a new database named Version150DataMigration from the NewLangProj150.sql script.
	StrUni stuNewDbName = L"Version150DataMigration";
	if (!CreateNewDb(L"NewLangProj150", stuNewDbName.Chars()))
		return false;
	if (m_qprog)
		m_qprog->StepIt();

	// Run the main script that does the bulk of the conversion.
	if (!RunSql(qode, L"V100toV150.sql"))
		return false;
	if (m_qprog)
		m_qprog->StepIt();

	IOleDbEncapPtr qodeCopy;
	qodeCopy.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qodeCopy->Init(m_stuServer.Bstr(), stuNewDbName.Bstr(), NULL, koltMsgBox,
		koltvForever));

	try
	{
		PostScriptUpdateEncTows(qodeCopy);
		qodeCopy.Clear(); // Can't detach while connected to it.
	}
	catch(...)
	{
		StrApp str(kstidMgdTagError);
		CheckHr(m_qfist->Write(str.Chars(), (ULONG)str.Length(), NULL));
		return false;
	}
	if (m_qprog)
		m_qprog->StepIt();

	// Upgrade is done. Now detach and delete the old database.
	// Enhance JohnT: nicer to put a DeleteDatabase method in IDbAdmin, and have it use
	// SQL to have SQLServer delete it.
	qode.Clear(); // Must destroy object pointed to so we can detach.
	// Pause long enough for the server to know the database connection has been closed?
	IDbAdminPtr qmda;
	qmda.CreateInstance(CLSID_DbAdmin);
	try
	{
		CheckHr(qmda->DetachDatabase(bstrOldDbName));
	}
	catch(Throwable&)
	{
		return false;
	}

	StrUni stuOldDbPath = m_stuFwData.Chars();
	stuOldDbPath += L"\\";
	stuOldDbPath += bstrOldDbName;
	StrUni stuOldDbPathMdf = stuOldDbPath;
	stuOldDbPathMdf += ".mdf";
	::DeleteFile(stuOldDbPathMdf.Chars());
	StrUni stuOldDbPathLdf = stuOldDbPath;
	stuOldDbPathLdf += "_log.ldf";
	::DeleteFile(stuOldDbPathLdf.Chars());

	//  Rename the new one and attach it to take the place of the old.
	try
	{
		CheckHr(qmda->RenameDatabase(m_stuFwData.Bstr(),
		stuNewDbName.Bstr(), bstrOldDbName, true, true));
	}
	catch(Throwable&)
	{
		return false;
	}
	// Make a new OleDbEncap in case later conversions need it.
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(m_stuServer.Bstr(), bstrOldDbName, NULL, koltMsgBox, koltvForever));
	return true;
}

/*----------------------------------------------------------------------------------------------
	Upgrades data from version 5 to version 6
	@param pode Pointer to a database to be upgraded.
	@return True if all went well, otherwise false.
----------------------------------------------------------------------------------------------*/
// TODO: rename method - this name has no meaning anymore!
bool MigrateData::M5toM6(IOleDbEncap * pode)
{
	try
	{
		// Create the expected path to the file:
		Assert(m_qfist);
		SmartBstr sbstrServer;
		SmartBstr sbstrDatabase;
		CheckHr(pode->get_Server(&sbstrServer));
		CheckHr(pode->get_Database(&sbstrDatabase));
		StrUni stuSvrName(sbstrServer.Chars());
		StrUni stuDbName(sbstrDatabase.Chars());
		PreScriptUpdateM5toM6(stuSvrName, stuDbName, m_qfist);
		if (m_qprog)
			m_qprog->StepIt();
	}
	catch(...)
	{
		StrApp str(kstidMgdTagError);
		CheckHr(m_qfist->Write(str.Chars(), (ULONG)str.Length(), NULL));
		return false;
	}

	return RunSql(pode, L"M5toM6.sql");
}

/*----------------------------------------------------------------------------------------------
	Execute the SQL script in the file indicated by the pathname argument against the database
	indicated by the first argument.
	@param pode Pointer to a database to be upgraded.
	@return True if all went well, otherwise false.
----------------------------------------------------------------------------------------------*/
bool MigrateData::RunSql(IOleDbEncap * pode, const wchar * pszSql)
{
	IOleDbCommandPtr qodc;
	StrUni stuSql;
	StrUni stuPath(DirectoryFinder::FwMigrationScriptDir());
	stuPath.FormatAppend(L"\\%s", pszSql);
	SmartBstr sbstrServer;
	pode->get_Server(&sbstrServer);
	SmartBstr sbstrDatabase;
	pode->get_Database(&sbstrDatabase);
	StrUni stuDb(sbstrDatabase.Chars(), sbstrDatabase.Length());
	StrUtil::FixForSqlQuotedString(stuDb);	// See LT-9115.

	// ENHANCE: (SteveMi) Alistair says he could do this without a user or password.
	// It doesn't happen on Rand's machine. Would like to know how Alistair's allows it.
	// Note the N is required for Unicode file names.
	stuSql.Format(L"EXEC master..xp_cmdshell N'osql -S%s -d\"%s\" -UFwDeveloper -Pcareful "
		L"-i\"%s\" -n'", sbstrServer.Chars(), stuDb.Chars(), stuPath.Chars());

	try
	{
		CheckHr(pode->CreateCommand(&qodc));
	}
	catch(Throwable&)
	{
		ErrorBox(kstidMgdQodcError);
		return false;
	}
	try
	{
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
	}
	catch(Throwable&)
	{
		StrApp strArg(pszSql);
		ErrorBox(kstidMgdSqlError, strArg.Chars());
		return false;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Copy one writing system's worth of info from Src to Dst and update the pointers.
	The source is in the old format with a redundant pair of null characters representing
	the OldWritingSystem code.
----------------------------------------------------------------------------------------------*/
static void CopyOneOldWsFontInfo(const OLECHAR * & pchSrc, OLECHAR * & pchDst)
{
	// Copy ws info and length of font family name and font family name itself
	// and # int props and int props themselves.
	int cchFF = *(pchSrc + 4); // Follows ws & ows.
	int cpropInt = SignedInt(pchSrc[5 + cchFF]); // Follows ws, ows, cchFF and ff itself.
	int cchStrProps = 0;
	if (cpropInt < 0)
	{
		// Additional string properties.
		int cpropStr = cpropInt * -1;
		cchStrProps = 1; // counter
		// Point at the data right after the ws (2), ows(2) char count for FF, FF itself, and
		// the cprop that turned out to be a cpropStr.
		OLECHAR * pchTmp = const_cast<OLECHAR *>(pchSrc) + 6 + cchFF;
		for (int iprop = 0; iprop < cpropStr; iprop++)
		{
			int cch = *pchTmp;
			cchStrProps += 1 + cch;
			pchTmp += 1 + cch;
		}
		// The character following the extra strings is the real integer property count.
		cpropInt = *pchTmp;
	}
	// Copy the ws.
	MoveItems(pchSrc, pchDst, 2);
	pchSrc += 4; // past ws & ows
	pchDst += 2; // past ws just copied
	//  2 = 1 (cchFF) + 1 (cprop).
	int cchCopy = 2 + cchFF + cchStrProps + (cpropInt * 4);
	MoveItems(pchSrc, pchDst, cchCopy);
	pchSrc += cchCopy;
	pchDst += cchCopy;
}

/*----------------------------------------------------------------------------------------------
	Convert an old writing system styles string to a new one (version 1.0 to 1.5).
----------------------------------------------------------------------------------------------*/
static void CopyOldWsStylesToNew(BSTR bstrOld, SmartBstr & sbstrNew)
{
	if (!BstrLen(bstrOld))
		return;
	OLECHAR rgch[30000]; // Where we will build up the style; enough for about 1000 ws's.
	OLECHAR * pch = rgch;
	const OLECHAR * pchOld = bstrOld;
	const OLECHAR * pchOldLim = pchOld + BstrLen(bstrOld);
	// Each iteration writes info about one old writing system to the destination.
	while (pchOld < pchOldLim)
	{
		CopyOneOldWsFontInfo(pchOld, pch);
	}
	sbstrNew.Assign(rgch, pch - rgch);
}
/*----------------------------------------------------------------------------------------------
	Handle some database changes for the EncToWs migration that are more easily coded in C++.

	Specifically the change here is to update all the StStyle.Rules properties. Each of these
	has a string that represents font property information encoded in a binary form. There is
	information about each encoding/writing system in the system. In the old scheme, each
	enc/ws is identified by four characters, two giving the enc/ws code, and two nulls which
	were intended eventually to allow us to vary the (old) writing system as well as the enc/ws.
	Our goal is to remove the two extra null characters, which would confuse the version 1.5
	style rule parser.

	@param pode Pointer to an active OLEDB encapsulation object.
----------------------------------------------------------------------------------------------*/

void MigrateData::PostScriptUpdateEncTows(IOleDbEncap * pode)
{
	IOleDbCommandPtr qodc;
	StrUni stuCmd;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	Vector<byte> vbData;
	vbData.Resize(5000);

	CheckHr(pode->CreateCommand(&qodc));

	stuCmd.Format(L"select stt.[id], stt.[rules] from StStyle stt");
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	ITsPropsFactoryPtr qtpf;
	qtpf.CreateInstance(CLSID_TsPropsFactory);

	while (fMoreRows)
	{
		// Read column 1, the object ID.
		int id;
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&id), sizeof(int),
			&cbSpaceTaken, &fIsNull, 0));
		if (fIsNull)
			continue;

		// Read column 2, the binary representation of the TsTextProps representing
		// the properties of the style.
		CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(vbData.Begin()),
			vbData.Size(), &cbSpaceTaken, &fIsNull, 0));
		//  If buffer was too small, reallocate and try again.
		if ((int)cbSpaceTaken > vbData.Size() && (!fIsNull))
		{
			vbData.Resize(cbSpaceTaken + 500);
			CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(vbData.Begin()),
				vbData.Size(), &cbSpaceTaken, &fIsNull, 0));
		}

		ITsTextPropsPtr qttp;
		if ((!fIsNull) && (cbSpaceTaken > 0))
		{
			// We got some data, try to interpret it.
			int cbDataInt = cbSpaceTaken;
			CheckHr(qtpf->DeserializePropsRgb(vbData.Begin(), &cbDataInt, &qttp));
		}

		// Extract from the TsTextProps the particular string property we need to adjust.
		SmartBstr sbstrOld;
		SmartBstr sbstrNew;
		CheckHr(qttp->GetStrPropValue(ktptWsStyle, & sbstrOld));

		// Fix it.
		CopyOldWsStylesToNew(sbstrOld, sbstrNew);

		// Make a new TsTextProps with the modified ws style property.
		ITsPropsBldrPtr qtpb;
		CheckHr(qttp->GetBldr(&qtpb));
		CheckHr(qtpb->SetStrPropValue(ktptWsStyle, sbstrNew));
		CheckHr(qtpb->GetTextProps(&qttp));

		// Convert the TsTextProps back to binary.
		int cbFmtSpaceTaken;
		CheckHr(qttp->SerializeRgb(vbData.Begin(), vbData.Size(), &cbFmtSpaceTaken));
		if (cbFmtSpaceTaken > vbData.Size())
		{
			vbData.Resize(cbFmtSpaceTaken + 500);
			CheckHr(qttp->SerializeRgb(vbData.Begin(), vbData.Size(), &cbFmtSpaceTaken));
		}

		// Write the result back to the database.
		IOleDbCommandPtr qodcUpdt;
		StrUni stuCmdUpdt;
		CheckHr(pode->CreateCommand(&qodcUpdt));
		stuCmdUpdt.Format(L"update StStyle set Rules = ? where [id]=%d", id);
		CheckHr(qodcUpdt->SetParameter(1,
			DBPARAMFLAGS_ISINPUT,
			NULL,
			DBTYPE_BYTES,
			reinterpret_cast<ULONG *>(vbData.Begin()),
			cbFmtSpaceTaken));
		CheckHr(qodcUpdt->ExecCommand(stuCmdUpdt.Bstr(), knSqlStmtNoResults));

		// Move on to next record.
		CheckHr(qodc->NextRow(&fMoreRows));
	}
}

/*----------------------------------------------------------------------------------------------
	The database and application are basically compatible.  Now, make any changes to the
	database that are needed to bring it up to the latest conceptual model.

	@param pode Pointer to an active OLEDB encapsulation object.
----------------------------------------------------------------------------------------------*/
// TODO: rename method - this name has no meaning anymore!
void MigrateData::PreScriptUpdateM5toM6(StrUni & stuServer, StrUni & stuDB, IStream * pstrmLog)
{
	// Replace the CmOverlayTag class and table with the CmOverlay_PossItems table and new
	// fields in the CmPossibility table.
#ifndef kflidCmOverlay_Items
#define kflidCmOverlay_Items 21003		// obsolete (deleted) field in still existing class
#endif
#ifndef kclidCmOverlayTag
#define kclidCmOverlayTag 22			// obsolete (deleted) class
#endif
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stuCmd;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	HashMap<GUID,GUID> hmguidTagguidPss;
	HashMap<GUID,HVO> hmguidhvoPss;
	Vector<HVO> vhvoTag;
	bool fCmOverlayTagFixed = false;
	try
	{
		qode.CreateInstance(CLSID_OleDbEncap);	// Note: this can throw an exception.
		CheckHr(qode->Init(stuServer.Bstr(), stuDB.Bstr(), pstrmLog, koltMsgBox,
			koltvForever));
		CheckHr(qode->CreateCommand(&qodc));

		// Update all overlay tags embedded in strings to point to the CmPossibility rather
		// than the obsolete CmOverlayTag.

		stuCmd.Format(L"select [id] from sysobjects where name='CmOverlayTag' and type='U'");
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			int nT;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&nT), sizeof(nT),
				&cbSpaceTaken, &fIsNull, 0));
			stuCmd.Format(L"SELECT co1.Guid$,co2.Guid$,co2.[Id],cot.[id]%n"
				L"  FROM CmOverlayTag cot%n"
				L"  JOIN CmObject co1 ON co1.[Id] = cot.[id]%n"
				L"  JOIN CmObject co2 ON co2.[id] = cot.PossItem");
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			GUID uidTag;
			GUID uidPss;
			HVO hvo;
			HVO hvoOld;
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			while (fMoreRows)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&uidTag), sizeof(GUID),
					&cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
				{
					CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&uidPss),
						sizeof(GUID), &cbSpaceTaken, &fIsNull, 0));
					if (!fIsNull)
					{
						hmguidTagguidPss.Insert(uidTag, uidPss);
						CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&hvo),
							sizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
						if (!fIsNull)
						{
							if (hmguidhvoPss.Retrieve(uidPss, &hvoOld))
							{
								// What should we do here?  This Assert should never complain.
								Assert(hvo == hvoOld);
							}
							else
							{
								hmguidhvoPss.Insert(uidPss, hvo);
							}
						}
					}
				}
				CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&hvo), sizeof(hvo),
					&cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
					vhvoTag.Push(hvo);
				CheckHr(qodc->NextRow(&fMoreRows));
			}
		}
		else
		{
			// If no CmOverlayTag table, then assume it's already been fixed.
			fCmOverlayTagFixed = true;
		}
		qodc.Clear();
		qode.Clear();
	}
	catch (...)
	{
		// If an error occurs, then hope it's already been fixed.
		fCmOverlayTagFixed = true;
	}
	if (!fCmOverlayTagFixed)
	{
		try
		{
			if (hmguidTagguidPss.Size())
			{
				FwDbChangeOverlayTags dsc(hmguidTagguidPss);
				dsc.SetAntique(true);
				IAdvInd3Ptr qadvi3;
				if (dsc.Init(stuServer, stuDB, m_qfist,qadvi3))
				{
					dsc.ResetConnection();
					dsc.CreateCommand();
					dsc.DoAll(kstidFixCmOverlayTagPhaseOne, kstidFixCmOverlayTagPhaseTwo,
						false);
				}
				else
				{
					// Error message?
				}
				dsc.ResetConnection();
				dsc.Terminate(1);		// value doesn't matter with no relaunch!
			}
			qode.CreateInstance(CLSID_OleDbEncap);	// Note: this can throw an exception.
			CheckHr(qode->Init(stuServer.Bstr(), stuDB.Bstr(), pstrmLog, koltMsgBox,
				koltvForever));
			CheckHr(qode->CreateCommand(&qodc));

			// Update the RnGenericRec_PhraseTags table as needed

			DbStringCrawler::FillPhraseTagsTable(qodc, L"RnGenericRecord", L"PhraseTags",
				hmguidhvoPss, 0);

			////////////////////////////////////////////////////////////////////////////////////
			//  THE REMAINDER OF THIS SHOULD BE DONE IN A STORED PROCEDURE.
			////////////////////////////////////////////////////////////////////////////////////

			// Copy information from the obsolete CmOverlayTag table to the CmPossibility table.
			// It doesn't hurt if this command is repeated.

			stuCmd.Format(L"UPDATE cmPossibility%n"
				L"SET%n"
				L"	ForeColor = ot.ForeColor,%n"
				L"	BackColor = ot.BackColor,%n"
				L"	UnderColor = ot.UnderColor,%n"
				L"	UnderStyle = ot.UnderStyle,%n"
				L"	Hidden = ot.Hidden%n"
				L"FROM CmOverlayTag ot, CmPossibility%n"
				L"WHERE ot.[PossItem] = CmPossibility.[Id]%n");
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

			// Fill in the new CmOverlay_PossItems table, avoiding duplicate entries.

			stuCmd.Format(L"SELECT o.[id],ot.[PossItem] FROM CmOverlay o%n"
				L"    JOIN CmOverlay_Items oi ON oi.[Src] = o.[id]%n"
				L"    JOIN CmOverlayTag ot ON ot.[id] = oi.[Dst]%n"
				L"    JOIN CmPossibility p ON p.[id] = ot.[PossItem]%n"
				L"ORDER BY o.[id],ot.[PossItem]");
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			Vector<SrcDst> vsdOld;
			SrcDst sd;
			while (fMoreRows)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&sd.m_hvoSrc),
					sizeof(sd.m_hvoSrc), &cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
				{
					CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&sd.m_hvoDst),
								sizeof(sd.m_hvoDst), &cbSpaceTaken, &fIsNull, 0));
					if (!fIsNull)
						vsdOld.Push(sd);
				}
				CheckHr(qodc->NextRow(&fMoreRows));
			}

			if (vsdOld.Size())
			{
				stuCmd.Assign(
					L"SELECT [Src],[Dst] FROM CmOverlay_PossItems ORDER BY [Src],[Dst]");
				CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));
				Vector<SrcDst> vsdNew;
				while (fMoreRows)
				{
					CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&sd.m_hvoSrc),
						sizeof(sd.m_hvoSrc), &cbSpaceTaken, &fIsNull, 0));
					if (!fIsNull)
					{
						CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&sd.m_hvoDst),
							sizeof(sd.m_hvoDst), &cbSpaceTaken, &fIsNull, 0));
						if (!fIsNull)
							vsdNew.Push(sd);
					}
					CheckHr(qodc->NextRow(&fMoreRows));
				}
				bool fInsertAll = true;
				if (vsdNew.Size())
				{
					// Note: both vsdOld and vsdNew are ordered by Src,Dst.
					int iOld;
					int iNew;
					for (iOld = 0, iNew = 0; iNew < vsdNew.Size(); ++iNew)
					{
						while (iOld < vsdOld.Size())
						{
							if (vsdOld[iOld].m_hvoSrc == vsdNew[iNew].m_hvoSrc &&
								vsdOld[iOld].m_hvoDst == vsdNew[iNew].m_hvoDst)
							{
								// This already exists in CmOverlay_PossItems.
								fInsertAll = false;
								vsdOld.Delete(iOld);
								break;
							}
							else if (vsdOld[iOld].m_hvoSrc <= vsdNew[iNew].m_hvoSrc &&
								vsdOld[iOld].m_hvoDst < vsdNew[iNew].m_hvoDst)
							{
								// We'll have to add this to CmOverlay_PossItems
								++iOld;
							}
							else
							{
								break;
							}
						}
					}
				}
				if (fInsertAll)
				{
					// This is the easiest way to do it.
					stuCmd.Format(L"INSERT INTO CmOverlay_PossItems%n"
						L"SELECT o.[Id], p.[Id]%n"
						L"FROM CmOverlay o%n"
						L"	JOIN CmOverlay_Items oi ON oi.src = o.[Id]%n"
						L"	JOIN CmOverlayTag ot ON ot.[Id] = oi.[Dst]%n"
						L"	JOIN CmPossibility p ON p.[Id] = ot.[PossItem]%n");
					CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
				}
				else if (vsdOld.Size())
				{
					stuCmd.Clear();
					// Insert only what is needed.  Group commands in 10 rows apiece.
					for (int i = 0; i < vsdOld.Size(); ++i)
					{
						if (i % 10 == 0)
						{
							if (stuCmd.Length())
							{
								CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
							}
							stuCmd.Clear();
						}
						stuCmd.FormatAppend(L"INSERT CmOverlay_PossItems VALUES (%d,%d);%n",
							vsdOld[i].m_hvoSrc, vsdOld[i].m_hvoDst);
					}
					if (stuCmd.Length())
					{
						CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
					}
				}
			}

			// Remove the obsolete CmOverlayTag table and objects from the database.
			int chvo = vhvoTag.Size();
			if (chvo)
			{
				const int kchvoInc = 20;
				int ihvo;
				int ihvoLim = kchvoInc;
				if (ihvoLim > chvo)
					ihvoLim = chvo;
				for (ihvo = 0; ihvo < chvo; )
				{
					stuCmd.Clear();
					for (; ihvo < ihvoLim; ++ihvo)
						stuCmd.FormatAppend(L"EXEC DeleteObjects '%d';%n", vhvoTag[ihvo]);
					CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
					ihvoLim += kchvoInc;
					if (ihvoLim > chvo)
						ihvoLim = chvo;
				}
			}

			//  delete the kflidCmOverlay_Items row from Field$
			//  This also removes the CmOverlay_Items view.
			stuCmd.Format(L"DELETE FROM Field$ WHERE Id = %d", kflidCmOverlay_Items);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

			// delete the kflidCmOverlayTag_* rows from Field$
			stuCmd.Format(
				L"ALTER TABLE CmOverlayTag DROP CONSTRAINT _CK_CmOverlayTag_UnderStyle; "
				L"DELETE FROM Field$ WHERE Class = %d", kclidCmOverlayTag);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

			// delete the kclidCmOVerlayTag rows from ClassPar$
			stuCmd.Format(L"DELETE FROM ClassPar$ WHERE [Src] = %d OR [Dst] = %d;",
				kclidCmOverlayTag, kclidCmOverlayTag);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

			// delete the kclidCmOVerlayTag row from Class$
			stuCmd.Format(L"DELETE FROM Class$ WHERE [Id] = %d", kclidCmOverlayTag);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

			//  remove the CmOverlayTag table.
			stuCmd.Format(L"DROP TABLE CmOverlayTag");
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

			//  remove the MakeObj_CmOverlayTag stored procedure.
			stuCmd.Format(L"DROP PROC MakeObj_CmOverlayTag");
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

			//  remove the CmOverlayTag_ view.
			stuCmd.Format(L"DROP VIEW CmOverlayTag_");
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

			// anything else that i've forgotten?

		}
		catch (Throwable & thr)
		{
			// ERROR MESSAGE?
			thr;
		}
		catch (...)
		{
		}
	} // end if (!fOverlayTagFixed)
}

/*----------------------------------------------------------------------------------------------
	Normalize all Unicode data in a multilingual formatted string table to NFD.

	@param podc Pointer to database command object.
	@param ptsf Pointer to TsString factory object.
	@param pszTable Name of the database table (probably L"MultiStr$" or L"MultiBigStr$")
----------------------------------------------------------------------------------------------*/
static void NormalizeMultiStr(IOleDbCommand * podc, ITsStrFactory * ptsf,
	const wchar * pszTable)
{
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	StrUni stuCmd;

	stuCmd.Format(L"SELECT Obj,Flid,Ws,Txt,Fmt FROM %s", pszTable);
	CheckHr(podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(podc->GetRowset(0));
	CheckHr(podc->NextRow(&fMoreRows));
	ULONG hvo;
	ULONG flid;
	ULONG ws;
	Vector<wchar> vchTxt;
	Vector<byte> vbFmt;
	vchTxt.Resize(4000);
	vbFmt.Resize(4000);
	int cch;
	int cbFmt;
	Vector<ULONG> vhvo;
	Vector<ULONG> vflid;
	Vector<ULONG> vws;
	ComVector<ITsString> vqtss;
	while (fMoreRows)
	{
		CheckHr(podc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), sizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
		if (!fIsNull)
		{
			CheckHr(podc->GetColValue(2, reinterpret_cast<BYTE *>(&flid), sizeof(flid), &cbSpaceTaken, &fIsNull, 0));
		}
		if (!fIsNull)
		{
			CheckHr(podc->GetColValue(3, reinterpret_cast<BYTE *>(&ws), sizeof(ws), &cbSpaceTaken, &fIsNull, 0));
		}
		cch = 0;
		if (!fIsNull)
		{
			CheckHr(podc->GetColValue(4, reinterpret_cast<BYTE *>(vchTxt.Begin()),
				vchTxt.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				cch = cbSpaceTaken / isizeof(wchar);
				if (cch > vchTxt.Size())
				{
					vchTxt.Resize(cch + 1000);
					CheckHr(podc->GetColValue(4, reinterpret_cast<BYTE *>(vchTxt.Begin()),
						vchTxt.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
					Assert(!fIsNull);
					Assert((int)cch == (int)(cbSpaceTaken / isizeof(wchar)));
				}
			}
		}
		cbFmt = 0;
		if (!fIsNull)
		{
			CheckHr(podc->GetColValue(5, reinterpret_cast<BYTE *>(vbFmt.Begin()),
				vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				cbFmt = cbSpaceTaken;
				if (cbFmt > vbFmt.Size())
				{
					vbFmt.Resize(cbFmt + 1000);
					CheckHr(podc->GetColValue(5, reinterpret_cast<BYTE *>(vbFmt.Begin()),
						vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
					Assert(!fIsNull);
					Assert((int)cbFmt == (int)cbSpaceTaken);
				}
			}
		}
		if (!fIsNull && cch)
		{
			Assert(cbFmt);

			UErrorCode uerr = U_ZERO_ERROR;
			UNormalizationCheckResult x = unorm_quickCheck(vchTxt.Begin(), cch, UNORM_NFD,
				&uerr);
			Assert(U_SUCCESS(uerr));
			if (x != UNORM_YES)
			{
				ITsStringPtr qtss;
				CheckHr(ptsf->DeserializeStringRgch(vchTxt.Begin(), &cch, vbFmt.Begin(), &cbFmt,
					&qtss));
				ITsStringPtr qtssNFD;
				CheckHr(qtss->get_NormalizedForm(knmNFD, &qtssNFD));
				ComBool fEqual;
				CheckHr(qtss->Equals(qtssNFD, &fEqual));
				if (!fEqual)
				{
					// Alas, we have a change that must be stored to update later.
					vhvo.Push(hvo);
					vflid.Push(flid);
					vws.Push(ws);
					vqtss.Push(qtssNFD);
				}
			}
		}
		CheckHr(podc->NextRow(&fMoreRows));
	}
	// Serialize each of the NFD string values that have changed due to normalization.
	for (int i = 0; i < vqtss.Size(); ++i)
	{
		CheckHr(vqtss[i]->get_Length(&cch));
		if (cch > vchTxt.Size())
			vchTxt.Resize(cch);
		CheckHr(vqtss[i]->FetchChars(0, cch, vchTxt.Begin()));
		CheckHr(vqtss[i]->SerializeFmtRgb(vbFmt.Begin(), vbFmt.Size(), &cbFmt));
		if (cbFmt > vbFmt.Size())
		{
			vbFmt.Resize(cbFmt);
			CheckHr(vqtss[i]->SerializeFmtRgb(vbFmt.Begin(), vbFmt.Size(), &cbFmt));
		}
		stuCmd.Format(L"UPDATE %s SET Txt=?, Fmt=? WHERE Obj=%d AND Flid=%d AND Ws=%d",
			pszTable, vhvo[i], vflid[i], vws[i]);
		CheckHr(podc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			reinterpret_cast<ULONG *>(vchTxt.Begin()), cch * isizeof(wchar)));
		CheckHr(podc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
			reinterpret_cast<ULONG *>(vbFmt.Begin()), cbFmt));
		CheckHr(podc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
	}
}

/*----------------------------------------------------------------------------------------------
	Normalize all Unicode data in a monolingual formatted string field to NFD.

	@param podc Pointer to database command object.
	@param ptsf Pointer to TsString factory object.
	@param pszClass Name of the class table to update.
	@param pszField Base name of the table field to update.
----------------------------------------------------------------------------------------------*/
static void NormalizeStringField(IOleDbCommand * podc, ITsStrFactory * ptsf,
	const wchar * pszClass, const wchar * pszField)
{
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	StrUni stuCmd;
	Vector<wchar> vchTxt;
	Vector<byte> vbFmt;
	vchTxt.Resize(4000);
	vbFmt.Resize(4000);
	int cch;
	int cbFmt;

	stuCmd.Format(L"SELECT [Id], [%s], [%s_Fmt] FROM [%s]", pszField, pszField, pszClass);
	CheckHr(podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(podc->GetRowset(0));
	CheckHr(podc->NextRow(&fMoreRows));
	ULONG hvo;
	Vector<ULONG> vhvo;
	ComVector<ITsString> vqtss;
	while (fMoreRows)
	{
		CheckHr(podc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
		if (!fIsNull)
		{
			CheckHr(podc->GetColValue(2, reinterpret_cast<BYTE *>(vchTxt.Begin()),
				vchTxt.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				cch = cbSpaceTaken / isizeof(wchar);
				if (cch > vchTxt.Size())
				{
					vchTxt.Resize(cch + 1000);
					CheckHr(podc->GetColValue(2, reinterpret_cast<BYTE *>(vchTxt.Begin()),
						vchTxt.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
					Assert(!fIsNull);
					Assert((int)cch == (int)(cbSpaceTaken / isizeof(wchar)));
				}
			}
		}
		if (!fIsNull)
		{
			CheckHr(podc->GetColValue(3, reinterpret_cast<BYTE *>(vbFmt.Begin()),
				vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				cbFmt = cbSpaceTaken;
				if (cbFmt > vbFmt.Size())
				{
					vbFmt.Resize(cbFmt + 1000);
					CheckHr(podc->GetColValue(3, reinterpret_cast<BYTE *>(vbFmt.Begin()),
						vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
					Assert(!fIsNull);
					Assert((int)cbFmt == (int)cbSpaceTaken);
				}
			}
		}
		if (!fIsNull && cch)
		{
			Assert(cbFmt);

			UErrorCode uerr = U_ZERO_ERROR;
			UNormalizationCheckResult x = unorm_quickCheck(vchTxt.Begin(), cch, UNORM_NFD,
				&uerr);
			Assert(U_SUCCESS(uerr));
			if (x != UNORM_YES)
			{
				ITsStringPtr qtss;
				CheckHr(ptsf->DeserializeStringRgch(vchTxt.Begin(), &cch, vbFmt.Begin(), &cbFmt,
					&qtss));
				ITsStringPtr qtssNFD;
				CheckHr(qtss->get_NormalizedForm(knmNFD, &qtssNFD));
				ComBool fEqual;
				CheckHr(qtss->Equals(qtssNFD, &fEqual));
				if (!fEqual)
				{
					// Alas, we have a change that must be stored to update later.
					vhvo.Push(hvo);
					vqtss.Push(qtssNFD);
				}
			}
		}
		CheckHr(podc->NextRow(&fMoreRows));
	}

	// Serialize each of the NFD string values that have changed due to normalization.
	for (int i = 0; i < vqtss.Size(); ++i)
	{
		CheckHr(vqtss[i]->get_Length(&cch));
		if (cch > vchTxt.Size())
			vchTxt.Resize(cch);
		CheckHr(vqtss[i]->FetchChars(0, cch, vchTxt.Begin()));
		CheckHr(vqtss[i]->SerializeFmtRgb(vbFmt.Begin(), vbFmt.Size(), &cbFmt));
		if (cbFmt > vbFmt.Size())
		{
			vbFmt.Resize(cbFmt);
			CheckHr(vqtss[i]->SerializeFmtRgb(vbFmt.Begin(), vbFmt.Size(), &cbFmt));
		}
		stuCmd.Format(L"UPDATE [%s] SET [%s]=?, [%s_Fmt]=? WHERE [Id]=%d",
			pszClass, pszField, pszField, vhvo[i]);
		CheckHr(podc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			reinterpret_cast<ULONG *>(vchTxt.Begin()), cch * isizeof(wchar)));
		CheckHr(podc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
			reinterpret_cast<ULONG *>(vbFmt.Begin()), cbFmt));
		CheckHr(podc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
	}
}

/*----------------------------------------------------------------------------------------------
	Normalize all Unicode data in a multilingual plain Unicode string table to NFD.

	@param podc Pointer to database command object.
	@param pszTable Name of the database table (probably L"MultiTxt$" or L"MultiBigTxt$")
----------------------------------------------------------------------------------------------*/
static void NormalizeMultiTxt(IOleDbCommand * podc, const wchar * pszTable)
{
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	StrUni stuCmd;

	stuCmd.Format(L"SELECT Obj,Flid,Ws,Txt FROM %s", pszTable);
	CheckHr(podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(podc->GetRowset(0));
	CheckHr(podc->NextRow(&fMoreRows));
	ULONG hvo;
	ULONG flid;
	ULONG ws;
	Vector<wchar> vchTxt;
	vchTxt.Resize(4000);
	int cch;
	Vector<ULONG> vhvo;
	Vector<ULONG> vflid;
	Vector<ULONG> vws;
	Vector<StrUni> vstu;
	while (fMoreRows)
	{
		CheckHr(podc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), sizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
		if (!fIsNull)
		{
			CheckHr(podc->GetColValue(2, reinterpret_cast<BYTE *>(&flid), sizeof(flid), &cbSpaceTaken, &fIsNull, 0));
		}
		if (!fIsNull)
		{
			CheckHr(podc->GetColValue(3, reinterpret_cast<BYTE *>(&ws), sizeof(ws), &cbSpaceTaken, &fIsNull, 0));
		}
		cch = 0;
		if (!fIsNull)
		{
			CheckHr(podc->GetColValue(4, reinterpret_cast<BYTE *>(vchTxt.Begin()),
				vchTxt.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				cch = cbSpaceTaken / isizeof(wchar);
				if (cch > vchTxt.Size())
				{
					vchTxt.Resize(cch + 1000);
					CheckHr(podc->GetColValue(4, reinterpret_cast<BYTE *>(vchTxt.Begin()),
						vchTxt.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
					Assert(!fIsNull);
					Assert((int)cch == (int)(cbSpaceTaken / isizeof(wchar)));
				}
			}
		}
		if (!fIsNull && cch)
		{
			UErrorCode uerr = U_ZERO_ERROR;
			UNormalizationCheckResult x = unorm_quickCheck(vchTxt.Begin(), cch, UNORM_NFD,
				&uerr);
			Assert(U_SUCCESS(uerr));
			if (x != UNORM_YES)
			{
				StrUni stu(vchTxt.Begin(), cch);
				StrUni stuNFD(stu);
				StrUtil::NormalizeStrUni(stuNFD, UNORM_NFD);
				if (stu != stuNFD)
				{
					// Alas, we have a change that must be stored to update later.
					vhvo.Push(hvo);
					vflid.Push(flid);
					vws.Push(ws);
					vstu.Push(stuNFD);
				}
			}
		}
		CheckHr(podc->NextRow(&fMoreRows));
	}
	// Serialize each of the NFD string values that have changed due to normalization.
	for (int i = 0; i < vstu.Size(); ++i)
	{
		stuCmd.Format(L"UPDATE %s SET Txt=? WHERE Obj=%d AND Flid=%d AND Ws=%d",
			pszTable, vhvo[i], vflid[i], vws[i]);
		CheckHr(podc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)(vstu[i].Chars()), vstu[i].Length() * isizeof(wchar)));
		CheckHr(podc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
	}
}

/*----------------------------------------------------------------------------------------------
	Normalize all Unicode data in a monolingual plain Unicode string field to NFD.

	@param podc Pointer to database command object.
	@param pszClass Name of the class table to update.
	@param pszField Name of the table field to update.
----------------------------------------------------------------------------------------------*/
static void NormalizeUnicodeField(IOleDbCommand * podc, const wchar * pszClass,
	const wchar * pszField)
{
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	StrUni stuCmd;
	Vector<wchar> vchTxt;

	vchTxt.Resize(4000);

	int cch;

	stuCmd.Format(L"SELECT [Id], [%s] FROM [%s]", pszField, pszClass);
	CheckHr(podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(podc->GetRowset(0));
	CheckHr(podc->NextRow(&fMoreRows));
	ULONG hvo;
	Vector<ULONG> vhvo;
	Vector<StrUni> vstu;
	while (fMoreRows)
	{
		CheckHr(podc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
		if (!fIsNull)
		{
			CheckHr(podc->GetColValue(2, reinterpret_cast<BYTE *>(vchTxt.Begin()),
				vchTxt.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				cch = cbSpaceTaken / isizeof(wchar);
				if (cch > vchTxt.Size())
				{
					vchTxt.Resize(cch + 1000);
					CheckHr(podc->GetColValue(2, reinterpret_cast<BYTE *>(vchTxt.Begin()),
						vchTxt.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
					Assert(!fIsNull);
					Assert((int)cch == (int)(cbSpaceTaken / isizeof(wchar)));
				}
			}
		}
		if (!fIsNull && cch)
		{
			UErrorCode uerr = U_ZERO_ERROR;
			UNormalizationCheckResult x = unorm_quickCheck(vchTxt.Begin(), cch, UNORM_NFD,
				&uerr);
			Assert(U_SUCCESS(uerr));
			if (x != UNORM_YES)
			{
				StrUni stu(vchTxt.Begin(), cch);
				StrUni stuNFD(stu);
				StrUtil::NormalizeStrUni(stuNFD, UNORM_NFD);
				if (stu != stuNFD)
				{
					// Alas, we have a change that must be stored to update later.
					vhvo.Push(hvo);
					vstu.Push(stuNFD);
				}
			}
		}
		CheckHr(podc->NextRow(&fMoreRows));
	}

	// Serialize each of the NFD string values that have changed due to normalization.
	for (int i = 0; i < vstu.Size(); ++i)
	{
		stuCmd.Format(L"UPDATE [%s] SET [%s]=? WHERE [Id]=%d",
			pszClass, pszField, vhvo[i]);
		CheckHr(podc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)(vstu[i].Chars()), vstu[i].Length() * isizeof(wchar)));
		CheckHr(podc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
	}
}


/*----------------------------------------------------------------------------------------------
	Normalize all Unicode data to NFD.  This included both formatted and plain Unicode strings.

	@param pode Pointer to an active OLEDB encapsulation object.

	@return true if successful, false if any errors occur.
----------------------------------------------------------------------------------------------*/
bool MigrateData::NormalizeUnicode(IOleDbEncap * pode)
{
	try
	{
		StrUtil::InitIcuDataDir();

		SmartBstr sbstrServer;
		SmartBstr sbstrDatabase;
		CheckHr(pode->get_Server(&sbstrServer));
		CheckHr(pode->get_Database(&sbstrDatabase));

		IOleDbCommandPtr qodc;

		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);

		// 1. Load all MultiLingual formatted strings, normalize them as needed, and update any
		//    that changed.
		CheckHr(pode->CreateCommand(&qodc));
		NormalizeMultiStr(qodc, qtsf, L"MultiStr$");
		NormalizeMultiStr(qodc, qtsf, L"MultiBigStr$");

		// 2. Find all monolingual formatted strings in the database, load them, normalize them,
		//    and update any that changed.
		// Note that the standard metadata cache does not provide the information we need.
		CheckHr(pode->CreateCommand(&qodc));
		Vector<StrUni> vstuField;
		Vector<StrUni> vstuClass;
		const int rgnStrTypes[] = { kcptString, kcptBigString };
		const int cStrTypes = isizeof(rgnStrTypes) / isizeof(int);
		DbStringCrawler::GetFieldsForTypes(qodc, rgnStrTypes, cStrTypes, vstuClass, vstuField);
		CheckHr(pode->CreateCommand(&qodc));
		int istu;
		int cField = vstuField.Size();
		for (istu = 0; istu < cField; ++istu)
			NormalizeStringField(qodc, qtsf, vstuClass[istu].Chars(), vstuField[istu].Chars());

		// 3. Load all MultiLingual plain strings, normalize them as needed, and update any that
		//    changed.
		CheckHr(pode->CreateCommand(&qodc));
		NormalizeMultiTxt(qodc, L"MultiTxt$");
		NormalizeMultiTxt(qodc, L"MultiBigTxt$");

		// 4. Find all monolingual plain strings in the database, load them, normalize them, and
		//    update any that changed.
		// Note that the standard metadata cache does not provide the information we need.
		CheckHr(pode->CreateCommand(&qodc));
		vstuField.Clear();
		vstuClass.Clear();
		const int rgnTxtTypes[] = { kcptUnicode, kcptBigUnicode };
		const int cTxtTypes = isizeof(rgnTxtTypes) / isizeof(int);
#ifndef kflidCmPossibilityList_HelpFile
#define kflidCmPossibilityList_HelpFile 8011
#endif
#ifndef kflidCmFilename_Filename
#define kflidCmFilename_Filename 33001
#endif
#ifndef kflidText_SoundFilePath
#define kflidText_SoundFilePath 5054003
#endif
#ifndef kflidLangProject_ExtLinkRootDir
#define kflidLangProject_ExtLinkRootDir 6001042
#endif
		const int rgflidPaths[] = {	kflidCmPossibilityList_HelpFile, kflidCmFilename_Filename,
			kflidText_SoundFilePath, kflidLangProject_ExtLinkRootDir };
		const int cflidPaths = isizeof(rgflidPaths) / isizeof(int);
		DbStringCrawler::GetFieldsForTypes(qodc, rgnTxtTypes, cTxtTypes, vstuClass, vstuField,
			rgflidPaths, cflidPaths);
		cField = vstuField.Size();
		CheckHr(pode->CreateCommand(&qodc));
		for (istu = 0; istu < cField; ++istu)
			NormalizeUnicodeField(qodc, vstuClass[istu].Chars(), vstuField[istu].Chars());

		return true;
	}
	catch(...)
	{
		return false;
	}
}


/*----------------------------------------------------------------------------------------------
	Convert the writing system code string psz to an integer (e.g., 'ENG' = 740664001).
	0 is also a legal writing system. (See ${StrUtil#ParseWs}).

	@h3{Return value}
	@code{
		0, if the string contains an illegal character.
		Otherwise, the resulting integer is returned.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar> int ParseWs(const XChar * psz)
{
	AssertPsz(psz);

	int cch = 0;
	while (psz[cch])
		++cch;
	int n;
	return ParseWs(psz, cch, &n, NULL) ? n : 0;
}

/*----------------------------------------------------------------------------------------------
	Convert the writing system code string (prgch, cch) to an integer (e.g., 'ENG' = 740664001).
	The result is placed in pn. When non-NULL, pcchRead returns the number of characters
	consumed from the input. This may be less than cch if an illegal character occurs in
	the string. The function returns 'true' when cch characters are consumed.

	@code{
	This function accepts the following two inputs:

		1) ___[[0]0][_][[0]0].
		2) AAA[[d]d][a][[D]d]

	where:

		[..] means that the enclosed items are optional.
		'A' means an uppercase letter.
		'a' means a lower case letter or '_'.
		'd' means a decimal digit.
		'D' means a base 9 digit (0-8).
	}

	In the first case, the resulting writing system value is 0 (the null writing system). In the second
	case:
	@code{
		1) Missing digits are assumed zero.
		2) A missing lowercase letter is assumed to be '_'.
	}

	In this way we treat the input as if all 8 characters are present. Note that it is the
	first digit of the pair that is assumed missing if there is only one digit in a slot.

	@code{
	Each character has a value according to:

		1) Uppercase letters map to the ascii value minus 'A'.
		3) '_' maps to zero.
		4) Lowercase letters map to ((the ascii value minus 'a') plus 1).
		2) Digits map to the ascii value minus '0'.

	Each position has a base according to:

		0) Base 26.
		1) Base 26.
		2) Base 26.
		3) Base 10.
		4) Base 10.
		5) Base 27.
		6) Base 9.
		7) Base 10.

	The returned writing system value is computed as follows:

		1) Start with ws = 0.
		2) For each position, multiply the current value of ws times the base of
			the position then add the value of the character in the position.
		3) Add 1.

	In normal use,
		AAA is the required language code from the Ethnologue. (___ for missing.)
		dd is the optional dialect code (0-99).
		a is the optional writing system (e.g., standard orthography, phonetic orthography,
			etc.). '_' is used when missing. This is required if Dd is present.
		Dd is an optional version number (0-89).
	}

	NOTE: the proper way to sort writing system IDs is alphabetically. This means that when
	comparing the integer IDs, they should be treated as UNSIGNED ints. This is because IDs
	toward the end of the alphabet will be represented by negative numbers if they are
	treated as signed, and therefore will be sorted incorrectly.

	@h3{Return value}
	@code{
		true, when cch characters are consumed.
		false, when less than cch characters are consumed.
	}
----------------------------------------------------------------------------------------------*/
template<typename XChar> bool ParseWs(const XChar * prgch, int cch, int * pws, int * pcchRead)
{
	AssertArray(prgch, cch);
	AssertPtr(pws);
	AssertPtrN(pcchRead);

	const XChar * pch = prgch;
	const XChar * pchLim = pch + cch;
	uint uT;
	uint uenc = 0;

	if (cch < 3)
		goto LExit;

	// Three uppercase characters - all required.
	// Three uppercase characters - all required.
	uT = prgch[0] - 'A';
	if (uT >= 26)
	{
		// Allow "___[[0]0][_][[0]0]" to mean the null writing system.
		if (prgch[0] != '_' || prgch[1] != '_' && prgch[2] != '_')
			goto LExit;
		pch = prgch + 3;
		if (pch < pchLim && *pch == '0')
		{
			pch++;
			if (pch < pchLim && *pch == '0')
				pch++;
		}
		if (pch < pchLim && *pch == '_')
			pch++;
		if (pch < pchLim && *pch == '0')
		{
			pch++;
			if (pch < pchLim && *pch == '0')
				pch++;
		}
		goto LExit;
	}

	uenc = uenc * 26 + uT;
	uT = prgch[1] - 'A';
	if (uT >= 26)
		goto LExit;
	uenc = uenc * 26 + uT;
	uT = prgch[2] - 'A';
	if (uT >= 26)
		goto LExit;
	uenc = uenc * 26 + uT;

	// Up to 2 digits.
	pch += 3;
	uT = 0;
	if (pch < pchLim && *pch >= '0' && *pch <= '9')
	{
		uT = uT * 10 + *pch - '0';
		pch++;
		if (pch < pchLim && *pch >= '0' && *pch <= '9')
		{
			uT = uT * 10 + *pch - '0';
			pch++;
		}
	}
	Assert(uT < 100);
	uenc = uenc * 100 + uT;

	// An optional _ or lowercase letter.
	uT = 0;
	if (pch < pchLim && *pch == '_')
	{
		pch++;
	}
	if (pch < pchLim && *pch >= 'a' && *pch <= 'z')
	{
		uT = *pch - 'a' + 1;
		pch++;
	}
	uenc = uenc * 27 + uT;

	// Up to two digits, but less than '90'
	uT = 0;
	if (pch < pchLim && *pch >= '0' && *pch <= '8')
	{
		uT = uT * 10 + *pch - '0';
		pch++;
		if (pch < pchLim && *pch >= '0' && *pch <= '9')
		{
			uT = uT * 10 + *pch - '0';
			pch++;
		}
	}
	else if (pch < pchLim && *pch == '9')
	{
		// Just a 9.
		uT = uT * 10 + *pch - '0';
		pch++;
	}
	uenc = uenc * 90 + uT + 1; // Adding 1 ensures the result is not 0.

LExit:
	*pws = uenc;
	if (pcchRead)
		*pcchRead = pch - prgch;
	return pch == pchLim;
}

/*------------------------------------------------------------------------------------------
	Convert the writing system code schar (8-bit) / wchar (16-bit) string to an integer.
	For example, "ENG" => 740664001.
------------------------------------------------------------------------------------------*/
template bool ParseWs<schar>(const schar * prgch, int cch, int * pn, int * pcchRead);
template bool ParseWs<wchar>(const wchar * prgch, int cch, int * pn, int * pcchRead);
template int ParseWs<schar>(const schar * psz);
template int ParseWs<wchar>(const wchar * psz);


/*------------------------------------------------------------------------------------------
	Expand the writing system code integer to a string.  For example, 740664001 => 'ENG'.
	This code used to be in the template function
		template<typename XChar, typename Pfn>
			void TextFormatter<XChar, Pfn>::FormatCore(const uint * prguData)
	to interpret %E and %e in format strings.

	@param nWs Writing system code integer.
	@param false Flag whether to perform full expansion, or a short form expansion.

	@return Pointer to a static buffer containing the string representation of the writing
					system code integer, or NULL if the input code is invalid.  The return
					value must be stored before calling this function again.
------------------------------------------------------------------------------------------*/
const wchar * ExpandWs(int nWs, bool fExpandFully = false)
{
	static wchar rgchTerm[16];
	unsigned uWs = static_cast<unsigned>(nWs);

	if (fExpandFully)
	{
		// Int as a long writing system string, e.g., ENG01a02.
		if (!uWs)
		{
			rgchTerm[0] = '_';
			rgchTerm[1] = '_';
			rgchTerm[2] = '_';
			rgchTerm[3] = '0';
			rgchTerm[4] = '0';
			rgchTerm[5] = '_';
			rgchTerm[6] = '0';
			rgchTerm[7] = '0';
			rgchTerm[8] = 0;
			return rgchTerm;
		}

		uWs--;

		rgchTerm[7] = (wchar)(uWs % 10 + '0');
		uWs /= 10;

		rgchTerm[6] = (wchar)(uWs % 9 + '0');
		uWs /= 9;

		unsigned uWs2 = uWs % 27;
		rgchTerm[5] = (wchar)(uWs2 ? (uWs2 - 1 + 'a') : '_');
		uWs /= 27;

		rgchTerm[4] = (wchar)(uWs % 10 + '0');
		uWs /= 10;

		rgchTerm[3] = (wchar)(uWs % 10 + '0');
		uWs /= 10;

		rgchTerm[2] = (wchar)(uWs % 26 + 'A');
		uWs /= 26;

		rgchTerm[1] = (wchar)(uWs % 26 + 'A');
		uWs /= 26;

		rgchTerm[0] = (wchar)(uWs % 26 + 'A');
		uWs /= 26;

		if (uWs)
			return NULL;
	}
	else
	{
		// Int as a short writing system string, e.g., ENGa.
		if (!uWs)
		{
			rgchTerm[0] = '_';
			rgchTerm[1] = '_';
			rgchTerm[2] = '_';
			rgchTerm[3] = 0;
			return rgchTerm;
		}

		uWs--;

		unsigned u0 = uWs % 90;
		uWs /= 90;
		unsigned u1 = uWs % 27;
		uWs /= 27;
		unsigned u2 = uWs % 100;
		uWs /= 100;

		rgchTerm[2] = (wchar)(uWs % 26 + 'A');
		uWs /= 26;
		rgchTerm[1] = (wchar)(uWs % 26 + 'A');
		uWs /= 26;
		rgchTerm[0] = (wchar)(uWs % 26 + 'A');
		uWs /= 26;

		if (uWs)
			return NULL;

		int ich = 3;
		if (u2)
		{
			if (u2 / 10)
				rgchTerm[ich++] = (wchar)(u2 / 10 + '0');
			rgchTerm[ich++] = (wchar)(u2 % 10 + '0');
		}

		if (u1 || u0)
			rgchTerm[ich++] = (wchar)(u1 ? (u1 - 1 + 'a') : '_');

		if (u0)
		{
			if (u0 / 10)
				rgchTerm[ich++] = (wchar)(u0 / 10 + '0');
			rgchTerm[ich++] = (wchar)(u0 % 10 + '0');
		}
		rgchTerm[ich] = 0;
	}

	return rgchTerm;
}


/*----------------------------------------------------------------------------------------------
	Upgrades data from version 1.5 to version 2.0 (aka 1.5x) -- the primary change is changing
	from a WritingSystem Code field value to using the WritingSystem Id field value, and
	using the WritingSystem ICULocale field value instead of the string representation of the
	Code field value.

	@param qode Reference to the active OLEDB encapsulation object.
	@param bstrOldDbName Name of the old database.

	@return true if successful, false if any errors occur.
----------------------------------------------------------------------------------------------*/
bool MigrateData::MigrateWsCodeToId(IOleDbEncapPtr & qode,	BSTR bstrOldDbName)
{
	// Create a new database named Version151DataMigration from the NewLangProj151.sql script.
	StrUni stuNewDbName = L"Version151DataMigration";
	if (!CreateNewDb(L"NewLangProj151", stuNewDbName.Chars()))
		return false;
	if (m_qprog)
		m_qprog->StepIt();

	// Run the update script, which copies the data from the old database to the new, and does a
	// lot of the conversion work.
	bool fOk = RunSql(qode, L"V150toV151.sql");
	if (!fOk)
		return false;
	if (m_qprog)
		m_qprog->StepIt();

	try
	{
		PostScriptUpdateWsCodeToId(stuNewDbName.Bstr(), bstrOldDbName);
	}
	catch(...)
	{
		StrApp str(kstidMgdTagError);
		CheckHr(m_qfist->Write(str.Chars(), (ULONG)str.Length(), NULL));
		return false;
	}
	if (m_qprog)
		m_qprog->StepIt();

	// Upgrade is done. Now detach and delete the old database.
	// Enhance JohnT: nicer to put a DeleteDatabase method in IDbAdmin, and have it use
	// SQL to have SQLServer delete it.
	qode.Clear(); // Must destroy object pointed to so we can detach.
	// Pause long enough for the server to know the database connection has been closed?
	IDbAdminPtr qmda;
	qmda.CreateInstance(CLSID_DbAdmin);
	try
	{
		CheckHr(qmda->DetachDatabase(bstrOldDbName));
	}
	catch(Throwable&)
	{
		return false;
	}
	StrUni stuOldDbPath = m_stuFwData.Chars();
	stuOldDbPath += L"\\";
	stuOldDbPath += bstrOldDbName;
	StrUni stuOldDbPathMdf = stuOldDbPath;
	stuOldDbPathMdf += ".mdf";
	::DeleteFile(stuOldDbPathMdf.Chars());
	StrUni stuOldDbPathLdf = stuOldDbPath;
	stuOldDbPathLdf += "_log.ldf";
	::DeleteFile(stuOldDbPathLdf.Chars());

	//  Rename the new one and attach it to take the place of the old.
	try
	{
		CheckHr(qmda->RenameDatabase(m_stuFwData.Bstr(),
			stuNewDbName.Bstr(), bstrOldDbName, true, true));
	}
	catch(Throwable&)
	{
		return false;
	}
	// Make a new OleDbEncap in case later conversions need it.
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(m_stuServer.Bstr(), bstrOldDbName, NULL, koltMsgBox, koltvForever));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Upgrades data from version 2.0 to version 200006 -- the primary change is replacing the
	single MultiTxt$ table with separate tables for each MultiUnicode field. There are also
	further changes reflecting various changes from 200004 to 200006.

	@param qode Reference to the active OLEDB encapsulation object.
	@param bstrOldDbName Name of the old database.

	@return true if successful, false if any errors occur.
----------------------------------------------------------------------------------------------*/
bool MigrateData::UpdateFrom200000To200006(IOleDbEncapPtr & qode, BSTR bstrOldDbName)
{
	// Create a new database named Version202DataMigration from the NewLangProj206.sql script.
	StrUni stuNewDbName = L"Version206DataMigration";
	if (!CreateNewDb(L"NewLangProj206", stuNewDbName.Chars()))
		return false;

	// Run the main script that does the bulk of the conversion.
	if (!RunSql(qode, L"V200toV206.sql"))
		return false;

	IOleDbEncapPtr qodeCopy;
	qodeCopy.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qodeCopy->Init(m_stuServer.Bstr(), stuNewDbName.Bstr(), NULL, koltMsgBox,
		koltvForever));

	try
	{
		PostScriptUpdateFrom200000(qodeCopy);
		qodeCopy.Clear(); // Can't detach while connected to it.
	}
	catch(...)
	{
		StrApp str(kstidMgdError);
		CheckHr(m_qfist->Write(str.Chars(), (ULONG)str.Length(), NULL));
		return false;
	}

	// Upgrade is done. Now detach and delete the old database.
	// Enhance JohnT: nicer to put a DeleteDatabase method in IDbAdmin, and have it use
	// SQL to have SQLServer delete it.
	qode.Clear(); // Must destroy object pointed to so we can detach.
	// Pause long enough for the server to know the database connection has been closed?
	IDbAdminPtr qmda;
	qmda.CreateInstance(CLSID_DbAdmin);
	try
	{
		CheckHr(qmda->DetachDatabase(bstrOldDbName));
	}
	catch(Throwable&)
	{
		return false;
	}
	StrUni stuOldDbPath = m_stuFwData.Chars();
	stuOldDbPath += L"\\";
	stuOldDbPath += bstrOldDbName;
	StrUni stuOldDbPathMdf = stuOldDbPath;
	stuOldDbPathMdf += ".mdf";
	::DeleteFile(stuOldDbPathMdf.Chars());
	StrUni stuOldDbPathLdf = stuOldDbPath;
	stuOldDbPathLdf += "_log.ldf";
	::DeleteFile(stuOldDbPathLdf.Chars());

	//  Rename the new one and attach it to take the place of the old.
	try
	{
		CheckHr(qmda->RenameDatabase(m_stuFwData.Bstr(),
			stuNewDbName.Bstr(), bstrOldDbName, true, true));
	}
	catch(Throwable&)
	{
		return false;
	}
	// Make a new OleDbEncap in case later conversions need it.
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(m_stuServer.Bstr(), bstrOldDbName, NULL, koltMsgBox, koltvForever));

	return true;
}


/*----------------------------------------------------------------------------------------------
	Final step in upgrading data from version 2.0 to version 200006 -- generate format field
	contents for the Description fields migrated from CmAnnotationDefn (MultiTxt$) to
	CmPossibility (MultiStr$).

	@param pode Pointer to the active OLEDB encapsulation object.
----------------------------------------------------------------------------------------------*/
void MigrateData::PostScriptUpdateFrom200000(IOleDbEncap * pode)
{
	IOleDbCommandPtr qodc;
	StrUni stuCmd;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;

	// Read the relevant current data into memory, and create TsStrings with the proper
	// minimal formatting information.
	stuCmd.Format(L"select [Obj], [Flid], [Ws], [Txt] from MultiStr$ where [Fmt] is null");
	CheckHr(pode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	ITsPropsFactoryPtr qtpf;
	qtpf.CreateInstance(CLSID_TsPropsFactory);
	int hobj;
	int flid;
	int ws;
	Vector<wchar> vchT;
	vchT.Resize(4001);
	int cch;
	ITsStringPtr qtss;
	Vector<int> vhobj;
	Vector<int> vflid;
	Vector<int> vws;
	ComVector<ITsString> vqtss;
	ITsIncStrBldrPtr qtisb;
	qtisb.CreateInstance(CLSID_TsIncStrBldr);
	while (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hobj), sizeof(hobj),
			&cbSpaceTaken, &fIsNull, 0));
		if (!fIsNull)
		{
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&flid), sizeof(flid),
				&cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&ws), sizeof(ws),
					&cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
				{
					CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(vchT.Begin()),
						vchT.Size() * sizeof(wchar), &cbSpaceTaken, &fIsNull, 2));
					if (!fIsNull)
					{
						cch = cbSpaceTaken / isizeof(wchar);
						CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
						CheckHr(qtisb->AppendRgch(vchT.Begin(), cch));
						CheckHr(qtisb->GetString(&qtss));
						vhobj.Push(hobj);
						vflid.Push(flid);
						vws.Push(ws);
						vqtss.Push(qtss);
						qtisb.Clear();
					}
				}
			}
		}
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	qodc.Clear();
	// Write the format fields out, one at a time.
	vchT.Clear();
	if (vhobj.Size())
	{
		Vector<byte> vbT;
		vbT.Resize(8000);
		int cb;
		CheckHr(pode->CreateCommand(&qodc));
		for (int i = 0; i < vhobj.Size(); ++i)
		{
			stuCmd.Format(
				L"Update MultiStr$ SET Fmt = ? WHERE [Obj] = %d AND [Flid] = %d AND [Ws] = %d",
				vhobj[i], vflid[i], vws[i]);
			CheckHr(vqtss[i]->SerializeFmtRgb(vbT.Begin(), vbT.Size(), &cb));
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(vbT.Begin()), cb));
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		}
		qodc.Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	Finish upgrading data from version 1.5 to version 2.0 by loading the new data added to
	the LangProject in version two: LexDB, PartsOfSpeech, TranslationTags, and
	PhonologicalData.  Also create an empty WordformInventory object in the LangProject.
	This "version 2" data cannot be loaded earlier because the FwXmlData object loads the
	metadata from Field$, and that changed sometime during version 200002.  We actually load
	the version 200006 data if the version 200000 data is needed.

	Finish upgrading data from version 2.0 to version 200006 by loading the new data added to
	the LangProject soon after version two: AnnotationDefs, SemanticDomainList, and
	possibly LexDB/Status.

	@param qode Reference to the active OLEDB encapsulation object.
	@param bstrOldDbName Name of the old database.

	@return true if successful, false if any errors occur.
----------------------------------------------------------------------------------------------*/
bool MigrateData::LoadVersion2_6Data(IOleDbEncapPtr & qode, BSTR bstrOldDbName)
{
	StrUni stuDir(DirectoryFinder::FwMigrationScriptDir());
	try
	{
		// Unzip the data XML files.
		StrUni stuZipFile;
		stuZipFile.Format(L"%s\\v206data.zip", stuDir.Chars());
		StrUni stuXmlFiles("*-v2_6.xml");
		// Initialize zip system data:
		ZipData zipd;
		zipd.m_sbstrDevice.Assign(stuDir.Chars(), 3);	// eg, "C:\"
		zipd.m_sbstrPath = stuDir;
		XceedZip xczUnzipper;
		// Initialize Xceed Zip module:
		if (!xczUnzipper.Init(&zipd))
			return false;
		xczUnzipper.SetPreservePaths(false);
		xczUnzipper.SetProcessSubfolders(false);
		xczUnzipper.SetZipFilename(stuZipFile.Bstr());
		xczUnzipper.SetFilesToProcess(stuXmlFiles.Bstr());
		xczUnzipper.SetUnzipToFolder(stuDir.Bstr());
		// Unzip the XML files.
		long nUnzip = xczUnzipper.Unzip();
		// Ignore warnings. Xceedzip 4.5.81.0 returns warnings (526) here where 4.5.77.0 didn't.
		// We already ignore these in BackupHandler::UnzipForRestore.
		if (nUnzip != 0 && nUnzip != 526)
		{
			// REVIEW: Display error message?
			return false;
		}
		// Initialize for loading data.  This is almost certainly required for at least the
		// semantic domain list.
		IFwXmlDataPtr qfwxd;
		IFwXmlData2Ptr qfwxd2;
		qfwxd.CreateInstance(CLSID_FwXmlData, CLSCTX_INPROC_SERVER);
		CheckHr(qfwxd->QueryInterface(IID_IFwXmlData2, (void **)&qfwxd2));
		CheckHr(qfwxd2->Open(m_stuServer.Bstr(), bstrOldDbName));
		StrUni stuFile;
		IAdvIndPtr qadvi;
		if (m_qprog)
			m_qprog->QueryInterface(IID_IAdvInd, (void **)&qadvi);

		IOleDbCommandPtr qodc;
		StrUni stuCmd;
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;

		// Get the database id value for the LangProject.  It must always exist.

		stuCmd.Format(L"select Id from CmObject where Class$ = %d AND Owner$ IS NULL",
			kclidLangProject);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		int hvoLangProj = 0;
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoLangProj),
						sizeof(hvoLangProj), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				hvoLangProj = 0;
		}
		if (hvoLangProj == 0)
			return false;

		// Make sure that the LexDB exists.  If it does, we assume it's up to at least
		// version 2.0 standards.  If not, we create it and load in the version 200006 data.

		stuCmd.Format(L"SELECT [Id] FROM CmObject "
			L"WHERE Owner$ = %d AND OwnFlid$ = %d AND Class$ = %d",
			hvoLangProj, kflidLangProject_LexDb, kclidLexDb);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		int hvoLexDb = 0;
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoLexDb), sizeof(hvoLexDb),
				&cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				hvoLexDb = 0;
		}
		stuFile.Format(L"%s\\LexicalDatabase6001-v2_6.xml", stuDir.Chars());
		if (hvoLexDb == 0)
		{
			StrApp strMsg(kstidMgdLoadingLexDb);
			if (m_qprog)
				m_qprog->SetMessage(strMsg.Chars());
			// Create an empty LexDB object.
			stuCmd.Format(L"declare @hvo int%n"
				L"DECLARE @guid uniqueidentifier%n"
				L"EXEC CreateOwnedObject$ %d, @hvo out, @guid out, %d, %d, %d",
				kclidLexDb, hvoLangProj, kflidLangProject_LexDb,
				kcptOwningAtom);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
			// Load the default lexical database contents from the XML file.
			CheckHr(qfwxd2->ImportXmlObject(stuFile.Bstr(), hvoLangProj,
				kflidLangProject_LexDb, qadvi));
		}
		else
		{
			// Is there anything besides Status5005 that we need to check to bring the
			// LexDB up to date with 200006?
		}
		::DeleteFileW(stuFile.Chars());
		stuFile.Format(L"%s\\LexicalDatabase6001-v2_6-Import.log", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());

		// Make sure that the PartsOfSpeech list exists, and that it is not empty.

		stuCmd.Format(L"SELECT [Id] FROM CmObject "
			L"WHERE Owner$ = %d AND OwnFlid$ = %d AND Class$ = %d",
			hvoLangProj, kflidLangProject_PartsOfSpeech, kclidCmPossibilityList);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		int cListItems = 0;
		int hvoList = 0;
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoList), sizeof(hvoList),
				&cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				hvoList = 0;
		}
		if (hvoList != 0)
		{
			stuCmd.Format(L"SELECT COUNT(*) FROM CmObject "
				L"WHERE Owner$ = %d AND OwnFlid$ = %d AND Class$ = %d",
				hvoList, kflidCmPossibilityList_Possibilities, kclidPartOfSpeech);
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&cListItems),
				sizeof(cListItems), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				cListItems = 0;
		}
		stuFile.Format(L"%s\\PartsOfSpeech6001-v2_6.xml", stuDir.Chars());
		if (cListItems == 0)
		{
			StrApp strMsg(kstidMgdLoadingPartsOfSpeech);
			if (m_qprog)
				m_qprog->SetMessage(strMsg.Chars());
			if (hvoList == 0)
			{
				// First create an empty PartsOfSpeech possibility list.
				stuCmd.Format(L"declare @hvo int%n"
					L"DECLARE @guid uniqueidentifier%n"
					L"EXEC CreateOwnedObject$ %d, @hvo out, @guid out, %d, %d, %d",
					kclidCmPossibilityList, hvoLangProj, kflidLangProject_PartsOfSpeech,
					kcptOwningAtom);
				CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
			}
			// Load the default parts of speech from the XML file.
			CheckHr(qfwxd2->ImportXmlObject(stuFile.Bstr(), hvoLangProj,
				kflidLangProject_PartsOfSpeech, qadvi));
		}
		::DeleteFileW(stuFile.Chars());
		stuFile.Format(L"%s\\PartsOfSpeech6001-v2_6-Import.log", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());

		// Make sure that the TranslationTags list exists, and that it is not empty.

		stuCmd.Format(L"SELECT [Id] FROM CmObject "
			L"WHERE Owner$ = %d AND OwnFlid$ = %d AND Class$ = %d",
			hvoLangProj, kflidLangProject_TranslationTags, kclidCmPossibilityList);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		cListItems = 0;
		hvoList = 0;
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoList), sizeof(hvoList),
						&cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				hvoList = 0;
		}
		if (hvoList != 0)
		{
			stuCmd.Format(L"SELECT COUNT(*) FROM CmObject "
				L"WHERE Owner$ = %d AND OwnFlid$ = %d AND Class$ = %d",
				hvoList, kflidCmPossibilityList_Possibilities, kclidCmPossibility);
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&cListItems),
				sizeof(cListItems), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				cListItems = 0;
		}
		stuFile.Format(L"%s\\TranslationTags6001-v2_6.xml", stuDir.Chars());
		if (cListItems == 0)
		{
			StrApp strMsg(kstidMgdLoadingTranslationTags);
			if (m_qprog)
				m_qprog->SetMessage(strMsg.Chars());
			if (hvoList == 0)
			{
				// First create an empty TranslationTags possibility list.
				// This has fixed guid = D7F71649-E8CF-11D3-9764-00C04F186933.
				stuCmd.Format(L"declare @hvo int%n"
					L"DECLARE @guid uniqueidentifier%n"
					L"SET @guid = 'D7F71649-E8CF-11D3-9764-00C04F186933'%n"
					L"EXEC CreateOwnedObject$ %d, @hvo out, @guid out, %d, %d, %d",
					kclidCmPossibilityList, hvoLangProj, kflidLangProject_TranslationTags,
					kcptOwningAtom);
				CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
			}
			// Load the default translation tags from the XML file.
			CheckHr(qfwxd2->ImportXmlObject(stuFile.Bstr(), hvoLangProj,
				kflidLangProject_TranslationTags, qadvi));
		}
		::DeleteFileW(stuFile.Chars());
		stuFile.Format(L"%s\\TranslationTags6001-v2_6-Import.log", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());

		// Make sure that the WordformInventory exists.

		stuCmd.Format(L"DECLARE @hvo INT, @guid UNIQUEIDENTIFIER%n"
			L"SELECT @hvo = [Id] FROM CmObject "
			L"WHERE Owner$ = %<0>d AND OwnFlid$ = %<1>d AND Class$ = %<2>d%n"
			L"IF @@rowcount = 0 BEGIN%n"
			L"	EXEC CreateOwnedObject$ %<2>d, @hvo out, @guid out, %<0>d, %<1>d, %<3>d%n"
			L"END",
			hvoLangProj, kflidLangProject_WordformInventory, kclidWordformInventory,
			kcptOwningAtom);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

		// Make sure that the PhonologicalData list exists, and that it is not empty.

		stuCmd.Format(L"SELECT [Id] FROM CmObject "
			L"WHERE Owner$ = %d AND OwnFlid$ = %d AND Class$ = %d",
			hvoLangProj, kflidLangProject_PhonologicalData, kclidPhPhonData);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		cListItems = 0;
		hvoList = 0;
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoList), sizeof(hvoList),
				&cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				hvoList = 0;
		}
		if (hvoList != 0)
		{
			stuCmd.Format(L"SELECT COUNT(*) FROM CmObject "
				L"WHERE Owner$ = %d AND OwnFlid$ = %d AND Class$ = %d",
				hvoList, kflidPhPhonData_PhonemeSets, kclidPhPhonemeSet);
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&cListItems),
				sizeof(cListItems), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				cListItems = 0;
		}
		stuFile.Format(L"%s\\PhonologicalData6001-v2_6.xml", stuDir.Chars());
		if (cListItems == 0)
		{
			StrApp strMsg(kstidMgdLoadingPhonologicalData);
			if (m_qprog)
				m_qprog->SetMessage(strMsg.Chars());
			if (hvoList == 0)
			{
				// First create an empty PhonologicalData object.
				stuCmd.Format(L"declare @hvo int%n"
					L"DECLARE @guid uniqueidentifier%n"
					L"EXEC CreateOwnedObject$ %d, @hvo out, @guid out, %d, %d, %d",
					kclidPhPhonData, hvoLangProj, kflidLangProject_PhonologicalData,
					kcptOwningAtom);
				CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
			}
			// Load the default PhonologicalData contents from the XML file.
			CheckHr(qfwxd2->ImportXmlObject(stuFile.Bstr(), hvoLangProj,
				kflidLangProject_PhonologicalData, qadvi));
		}
		// Make sure the phonology names/abbreviations have the vernacular writing system
		stuCmd = L"declare @ws int "
			L"select top 1 @ws = dst from LanguageProject_CurrentVernacularWritingSystems order by ord; "
			L"update PhTerminalUnit_Name set ws = @ws; "
			L"update PhTerminalUnit_Description set ws = @ws;";
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		::DeleteFileW(stuFile.Chars());
		stuFile.Format(L"%s\\PhonologicalData6001-v2_6-Import.log", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());

		// Make sure that the AnnotationDefs list exists (added in 200001), and that it
		// is not empty.

		stuCmd.Format(L"SELECT [Id] FROM CmObject "
			L"WHERE Owner$ = %d AND OwnFlid$ = %d AND Class$ = %d",
			hvoLangProj, kflidLangProject_AnnotationDefs, kclidCmPossibilityList);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		cListItems = 0;
		hvoList = 0;
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoList), sizeof(hvoList),
				&cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				hvoList = 0;
		}
		if (hvoList != 0)
		{
			stuCmd.Format(L"SELECT COUNT(*) FROM CmObject "
				L"WHERE Owner$ = %d AND OwnFlid$ = %d AND Class$ = %d",
				hvoList, kflidCmPossibilityList_Possibilities, kclidCmAnnotationDefn);
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&cListItems),
				sizeof(cListItems), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				cListItems = 0;
		}
		stuFile.Format(L"%s\\AnnotationDefinitions6001-v2_6.xml", stuDir.Chars());
		if (cListItems == 0)
		{
			StrApp strMsg(kstidMgdLoadingAnnotationDefns);
			if (m_qprog)
				m_qprog->SetMessage(strMsg.Chars());
			if (hvoList == 0)
			{
				// First create an empty AnnotationDefs possibility list.
				// This has fixed guid = EA346C01-022F-4F34-B938-219CE7B65B73.
				stuCmd.Format(L"declare @hvo int%n"
					L"DECLARE @guid uniqueidentifier%n"
					L"SET @guid = 'EA346C01-022F-4F34-B938-219CE7B65B73'%n"
					L"EXEC CreateOwnedObject$ %d, @hvo out, @guid out, %d, %d, %d",
					kclidCmPossibilityList, hvoLangProj,
					kflidLangProject_AnnotationDefs, kcptOwningAtom);
				CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
			}
			// Load the default annotation definitions from the XML file.
			CheckHr(qfwxd2->ImportXmlObject(stuFile.Bstr(), hvoLangProj,
				kflidLangProject_AnnotationDefs, qadvi));
		}
		::DeleteFileW(stuFile.Chars());
		stuFile.Format(L"%s\\AnnotationDefinitions6001-v2_6-Import.log", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());

		// Make sure that the Sense Status list exists (added in 200004), and that it is not
		// empty.  This step is not needed if we loaded RnGenericRec6001-v2_6.xml earlier
		// (indicated by hvoLexDb being zero), because Status5005-v2_6 was included in
		// LexicalDatabase6001-v2_6.
		stuFile.Format(L"%s\\Status5005-v2_6.xml", stuDir.Chars());
		if (hvoLexDb != 0)
		{
			hvoList = 0;
			cListItems = 0;
			stuCmd.Format(L"SELECT [Id] FROM CmObject "
				L"WHERE Owner$ = %d AND OwnFlid$ = %d AND Class$ = %d",
				hvoLexDb, kflidLexDb_Status, kclidCmPossibilityList);
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (fMoreRows)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoList),
					sizeof(hvoList), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				hvoList = 0;
			}
			if (hvoList != 0)
			{
				stuCmd.Format(L"SELECT COUNT(*) FROM CmObject "
					L"WHERE Owner$ = %d AND OwnFlid$ = %d AND Class$ = %d",
					hvoList, kflidCmPossibilityList_Possibilities, kclidCmPossibility);
				CheckHr(qode->CreateCommand(&qodc));
				CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&cListItems),
					sizeof(cListItems), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				cListItems = 0;
			}
			if (cListItems == 0)
			{
				// Load the default sense status list from the XML file.
				if (hvoList == 0)
				{
					// First create an empty Status possibility list.
					stuCmd.Format(L"declare @hvo int%n"
						L"DECLARE @guid uniqueidentifier%n"
						L"EXEC CreateOwnedObject$ %d, @hvo out, @guid out, %d, %d, %d",
						kclidCmPossibilityList, hvoLexDb, kflidLexDb_Status,
						kcptOwningAtom);
					CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
				}
				// Load the default lexical database status possibilities from the XML file.
				CheckHr(qfwxd2->ImportXmlObject(stuFile.Bstr(), hvoLexDb,
					kflidLexDb_Status, qadvi));
			}
		}
		::DeleteFileW(stuFile.Chars());
		stuFile.Format(L"%s\\Status5005-v2_6-Import.log", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());

		// Make sure that the semantic domain list exists (added in 200006), and that it is
		// not empty.
		hvoList = 0;
		cListItems = 0;
		stuCmd.Format(L"SELECT [Id] FROM CmObject "
			L"WHERE Owner$ = %d AND OwnFlid$ = %d AND Class$ = %d",
			hvoLangProj, kflidLangProject_SemanticDomainList, kclidCmPossibilityList);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoList), sizeof(hvoList),
				&cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				hvoList = 0;
		}
		if (hvoList != 0)
		{
			stuCmd.Format(L"SELECT COUNT(*) FROM CmObject "
				L"WHERE Owner$ = %d AND OwnFlid$ = %d AND Class$ = %d",
				hvoList, kflidCmPossibilityList_Possibilities, kclidCmSemanticDomain);
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&cListItems),
				sizeof(cListItems), &cbSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				cListItems = 0;
		}
		stuFile.Format(L"%s\\SemanticDomainList6001-v2_6.xml", stuDir.Chars());
		if (cListItems == 0)
		{
			StrApp strMsg(kstidMgdLoadingSemanticDomains);
			if (m_qprog)
				m_qprog->SetMessage(strMsg.Chars());
			if (hvoList == 0)
			{
				// First create an empty Semantic Domain possibility list.
				// This has fixed guid = C924BFCE-BEED-4382-95E8-62B54951C83D.
				stuCmd.Format(L"declare @hvo int%n"
					L"DECLARE @guid uniqueidentifier%n"
					L"SET @guid = 'C924BFCE-BEED-4382-95E8-62B54951C83D'%n"
					L"EXEC CreateOwnedObject$ %d, @hvo out, @guid out, %d, %d, %d",
					kclidCmPossibilityList, hvoLangProj,
					kflidLangProject_SemanticDomainList, kcptOwningAtom);
				CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
			}
			// Load the default semantic domains from the XML file.
			CheckHr(qfwxd2->ImportXmlObject(stuFile.Bstr(), hvoLangProj,
				kflidLangProject_SemanticDomainList, qadvi));
		}
		::DeleteFileW(stuFile.Chars());
		stuFile.Format(L"%s\\SemanticDomainList6001-v2_6-Import.log", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());

		return true;
	}
	catch (...)
	{
		// Delete any of the files that may have been unzipped but not yet deleted, but leave
		// any log files in case they may prove useful for debugging...
		StrUni stuFile;
		stuFile.Format(L"%s\\LexicalDatabase6001-v2_6.xml", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());
		stuFile.Format(L"%s\\PartsOfSpeech6001-v2_6.xml", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());
		stuFile.Format(L"%s\\TranslationTags6001-v2_6.xml", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());
		stuFile.Format(L"%s\\PhonologicalData6001-v2_6.xml", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());
		stuFile.Format(L"%s\\AnnotationDefinitions6001-v2_6.xml", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());
		stuFile.Format(L"%s\\Status5005-v2_6.xml", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());
		stuFile.Format(L"%s\\SemanticDomainList6001-v2_6.xml", stuDir.Chars());
		::DeleteFileW(stuFile.Chars());

		return false;
	}
}


/*----------------------------------------------------------------------------------------------
	This class implements a string crawler that changes each occurrence of a writing system code
	in a text format to the corresponding writing system database object id.

	Hungarian: cwci
----------------------------------------------------------------------------------------------*/
class ChangeWsCodeToId : public DbStringCrawler
{
	typedef DbStringCrawler SuperClass;
public:
	// We want to process only the formatting information, and do it as efficiently as possible!
	ChangeWsCodeToId()
		: DbStringCrawler(false, true, true, false)
	{
		m_wsCodeEng = ParseWs("ENG");	// Prior to this version English was always the user ws.
		m_wsDefault = 0;
	}
	~ChangeWsCodeToId()
	{
	}

	virtual bool ProcessFormatting(ComVector<ITsTextProps> & vpttp);
	virtual bool ProcessBytes(Vector<byte> & vb);
	void InsertMapping(int wsCode, int wsId)
	{
		m_hmwsid.Insert(wsCode, wsId);
		if (wsCode == m_wsCodeEng)
			m_wsDefault = wsId;
	}

	// Terminate the string crawler, releasing any memory that it has allocated.
	void Terminate()
	{
		// The argument to Terminate doesn't matter in this context (Data Migration).
		SuperClass::Terminate(1);
		m_hmwsid.Clear();
	}

protected:
	HashMap<int,int> m_hmwsid;
	int m_wsDefault;
	int m_wsCodeEng;
};

/*----------------------------------------------------------------------------------------------
	Method to update text property objects.  Not used in this implementation.
----------------------------------------------------------------------------------------------*/
bool ChangeWsCodeToId::ProcessFormatting(ComVector<ITsTextProps> & vpttp)
{
	Assert(false);
	return false;
}

/*----------------------------------------------------------------------------------------------
	Scan the serialized string format (vbFmt) for runs that have a kscpWs property.  If found,
	the value of that property is changed from the old style Code value to the corresponding
	database object id that we now use.

	@param vbFmt Reference to a byte vector containing the formatting information for the
					string.

	@return True if one or more runs had their writing system changed, otherwise false.
----------------------------------------------------------------------------------------------*/
bool ChangeWsCodeToId::ProcessBytes(Vector<byte> & vbFmt)
{
	Assert(sizeof(int) == 4);
	int * pn = reinterpret_cast<int *>(vbFmt.Begin());
	int crun = *pn++;
	int crunChg = 0;
	// 2 for char-min followed by prop offset
	int cbOffsets = isizeof(int) + (2 * crun * isizeof(int));
	int itip;
	int ctip;
	int ctsp;
	int irun;
	int wsOld;
	int wsNew;
	int scp;

	const byte * pbProp;
	const byte * pb;
	const byte * pbNext;
	// Property indexes may be redundant, so store them in a set rather than a vector so that
	// we process each property only once.
	Set<int> setibProp;
	for (irun = 0; irun < crun; ++irun)
		setibProp.Insert(pn[2 * irun + 1]);
	Set<int>::iterator it;
	for (it = setibProp.Begin(); it != setibProp.End(); ++it)
	{
		pbProp = vbFmt.Begin() + cbOffsets + it.GetValue();
		pb = pbProp;
		ctip = *pb++;
		ctsp = *pb++;
		for (itip = 0; itip < ctip; ++itip)
		{
			scp = TextProps::DecodeScp(pb, vbFmt.End() - pb, &pbNext);
			if (scp == kscpWsAndOws)
			{
				Assert(scp != kscpWsAndOws);
				ThrowHr(E_UNEXPECTED);
			}
			if (scp == kscpWs)
			{
				Assert((scp & 0x3) == 2);
				wsOld = *(reinterpret_cast<const int *>(pbNext));
				if (!m_hmwsid.Retrieve(wsOld, &wsNew))
					wsNew = m_wsDefault;
				byte * pbEnc = const_cast<byte *>(pbNext);
				*(reinterpret_cast<int *>(pbEnc)) = wsNew;
				++crunChg;
			}
			switch (scp & 0x3)
			{
			case 0:
				pb = pbNext + 1;
				break;
			case 1:
				pb = pbNext + 2;
				break;
			case 2:
				pb = pbNext + 4;
				break;
			case 3:
				pb = pbNext + 8;
				break;
			}
		}
	}
	return (crunChg > 0);
}


/*----------------------------------------------------------------------------------------------
	Handle some database changes for the WsCodeToId migration that are more easily coded in C++.

	1. Change internal Ws values inside formatting from old codes to object ids.
	2. Ensure that ICULocale is defined uniquely for every WritingSystem.
	3. Fix the binary Rules field of the StStyle table and the binary StyleRules field of the
	   StPara table.

	@param pode Pointer to an active OLEDB encapsulation object.
----------------------------------------------------------------------------------------------*/
void MigrateData::PostScriptUpdateWsCodeToId(BSTR bstrNewDbName, BSTR bstrOldDbName)
{
	// First, handle the changes to writing systems embedded in the various binary format
	// fields.
	ChangeWsCodeToId cwci;
	// The mapping initialization comes from the old version database.
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	ComBool fMoreRows;
	ComBool fIsNull;
	ULONG cbSpaceTaken;
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(m_stuServer.Bstr(), bstrOldDbName, m_qfist, koltMsgBox, koltvForever));
	CheckHr(qode->CreateCommand(&qodc));
	StrUni stuCmd(L"select [Code],[Id],[ICULocale] from [LgWritingSystem]");
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	int wsCode;
	int wsId;
	int wsDefault = 0;
	int wsCodeEng = ParseWs("ENG");
	wchar rgchICULocale[4001];
	StrUni stuICULocale;
	Vector<int> vwsCode;
	Vector<int> vwsId;
	Vector<StrUni> vstuICULocale;
	HashMap<int,int> hmwsid;
	while (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&wsCode), sizeof(wsCode),
			&cbSpaceTaken, &fIsNull, 0));
		if (!fIsNull)
		{
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&wsId),
				sizeof(wsId), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
			{
				cwci.InsertMapping(wsCode, wsId);
				hmwsid.Insert(wsCode, wsId);
				if (wsCode == wsCodeEng)
					wsDefault = wsId;
				CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(rgchICULocale),
					sizeof(rgchICULocale), &cbSpaceTaken, &fIsNull, 2));
				if (!fIsNull && cbSpaceTaken > 2)		// 2 bytes for trailing NUL.
					stuICULocale.Assign(rgchICULocale);
				else
					stuICULocale.Clear();
				// Now, store the information for use later.
				vwsCode.Push(wsCode);
				vwsId.Push(wsId);
				vstuICULocale.Push(stuICULocale);
			}
		}
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	qodc.Clear();
	qode.Clear();
	if (!wsDefault)
	{
		cwci.Terminate();
		return;
	}

	// The crawler works on the new version database.
	StrUni stuDatabase(bstrNewDbName);
	if (!cwci.Init(m_stuServer, stuDatabase, m_qfist))
	{
		cwci.Terminate();
		return;
	}
	cwci.ResetConnection();		// necessary: resets, not clears (Init() connects to master!??)

	// The arguments don't matter because we're not displaying progress.
	cwci.DoAll(0, 0);
	cwci.Terminate();

	// Second, ensure that ICULocale is defined, and defined uniquely, for every writing system.
	int i;
	Vector<StrUni> vstuNewICULocale;
	vstuNewICULocale.Resize(vstuICULocale.Size());
	Set<StrUni> setstuLocale;
	for (i = 0; i < vwsId.Size(); ++i)
	{
		StrUni & stu = vstuICULocale[i];
		if (stu.Length() && !setstuLocale.IsMember(stu))
			setstuLocale.Insert(stu);
		else
		{
			StrUni stuNew  = SilUtil::ConvertEthnologueToISO(ExpandWs(vwsCode[i]));
			// This is a kludge to allow the basic writing systems to come through correctly
			// so that the rest of the migration works (e.g., loading various lists, etc.).
			// The real problem is that the Ethnologue database does not contain the 14th
			// edition code equivalents, so we don't have a reliable way to convert earlier
			// codes to new codes. We should probably add this info to the Ethnologue db,
			// but no time for Flex Beta 0.8.
			if (stuNew.Equals(L"xfrn"))
				stuNew = L"fr";
			else if (stuNew.Equals(L"xspn"))
				stuNew = L"es";
			vstuNewICULocale[i] = stuNew.Chars();
		}
	}
	for (i = 0; i < vwsId.Size(); ++i)
	{
		StrUni & stu = vstuNewICULocale[i];
		if (stu.Length())
		{
			// Ensure that it's unique!
			while (setstuLocale.IsMember(stu))
				stu.Append(L"a");
			setstuLocale.Insert(stu);
		}
	}
	// We don't need these anymore.
	vstuICULocale.Clear();
	vwsCode.Clear();
	// Remove unneeded values from vwsId and vstuNewICULocale.
	for (i = 0; i < vwsId.Size(); ++i)
	{
		if (!vstuNewICULocale[i].Length())
		{
			vwsId.Delete(i);
			vstuNewICULocale.Delete(i);
			--i;
		}
	}
	// Now we can update the LgWritingSystem table with the new ICU Locale values.
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(m_stuServer.Bstr(), bstrNewDbName, m_qfist, koltMsgBox, koltvForever));
	if (vwsId.Size())
	{
		CheckHr(qode->CreateCommand(&qodc));
		for (int i = 0; i < vstuNewICULocale.Size(); ++i)
		{
			stuCmd.Format(L"UPDATE LgWritingSystem SET ICULocale=? WHERE [Id]=%d", vwsId[i]);
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)(vstuNewICULocale[i].Chars()),
				vstuNewICULocale[i].Length() * isizeof(wchar)));
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		}
		qodc.Clear();
	}

	// Third, update the Rules field of the StStyle table and the StyleRules field of the StPara
	// table.

	// 4. select id,StyleRules from StPara where StyleRules is not null;
	// 5. munge the binary data from StyleRules
	// 6. update StPara set Rules = NewRules[i] where Id = Id[i]

	HVO hvo;
	BYTE rgbRule[8000];
	ULONG cbRule;
	int nType;
	Vector<BYTE> vbRule;
	Vector<HVO> vhvo;
	Vector< Vector<BYTE> > vvbRules;
	stuCmd.Format(L"SELECT [Id],[Rules],[Type] FROM StStyle WHERE [Rules] IS NOT NULL");
	CheckHr(qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		// Get the data for one row of the table.
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), sizeof(hvo),
			&cbSpaceTaken, &fIsNull, 0));
		Assert(!fIsNull);
		CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(rgbRule), sizeof(rgbRule),
			&cbRule, &fIsNull, 0));
		Assert(!fIsNull);
		nType = 0;
		CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&nType), sizeof(nType),
			&cbSpaceTaken, &fIsNull, 0));
		Assert(!fIsNull);

		// Update the Rules value.
		if (UpdateRulesWsCodeToId(rgbRule, cbRule, vbRule, hmwsid, nType == 0))
		{
			// Store the information for use later.
			vhvo.Push(hvo);
			vvbRules.Push(vbRule);
		}
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	if (vhvo.Size())
	{
		CheckHr(qode->CreateCommand(&qodc));
		for (int i = 0; i < vhvo.Size(); ++i)
		{
			stuCmd.Format(L"UPDATE StStyle SET Rules=? WHERE [Id]=%d", vhvo[i]);
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(vvbRules[i].Begin()), vvbRules[i].Size()));
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		}
		qodc.Clear();
	}
	vhvo.Clear();
	vvbRules.Clear();

	stuCmd.Format(L"SELECT [Id],[StyleRules] FROM StPara WHERE [StyleRules] IS NOT NULL");
	CheckHr(qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		// Get the data for one row of the table.
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), sizeof(hvo),
			&cbSpaceTaken, &fIsNull, 0));
		Assert(!fIsNull);
		CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(rgbRule), sizeof(rgbRule),
			&cbRule, &fIsNull, 0));
		Assert(!fIsNull);

		// Update the StyleRules value.
		if (UpdateRulesWsCodeToId(rgbRule, cbRule, vbRule, hmwsid, true))
		{
			// Store the information for use later.
			vhvo.Push(hvo);
			vvbRules.Push(vbRule);
		}
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	if (vhvo.Size())
	{
		CheckHr(qode->CreateCommand(&qodc));
		for (int i = 0; i < vhvo.Size(); ++i)
		{
			stuCmd.Format(L"UPDATE StPara SET StyleRules=? WHERE [Id]=%d", vhvo[i]);
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(vvbRules[i].Begin()), vvbRules[i].Size()));
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		}
		qodc.Clear();
	}
}

/*----------------------------------------------------------------------------------------------
	Add this text property to the vector if it is not already there.

	@param vtxip Vector of text properties
	@param txip text property to add if missing

	@return index of the property in vtxip when finished.
----------------------------------------------------------------------------------------------*/
static int MergeTextIntProp(Vector<TextProps::TextIntProp> & vtxip,
	TextProps::TextIntProp & txip)
{
	for (int itxip = 0; itxip < vtxip.Size(); ++itxip)
	{
		if (vtxip[itxip].m_tpt == txip.m_tpt)
			return itxip;
	}
	vtxip.Push(txip);
	return vtxip.Size() - 1;
}

/*----------------------------------------------------------------------------------------------
	Add this text property to the vector if it is not already there.

	@param vtxsp Vector of text properties
	@param txsp text property to add if missing

	@return index of the property in vtxsp when finished.
----------------------------------------------------------------------------------------------*/
static int MergeTextStrProp(Vector<TextProps::TextStrProp> & vtxsp,
	TextProps::TextStrProp & txsp)
{
	for (int itxsp = 0; itxsp < vtxsp.Size(); ++itxsp)
	{
		if (vtxsp[itxsp].m_tpt == txsp.m_tpt)
			return itxsp;
	}
	vtxsp.Push(txsp);
	return vtxsp.Size() - 1;
}

/*----------------------------------------------------------------------------------------------
	Update the binary Rules data contained in pbRule[0..cbRule], writing the updated Rules data
	to vbRule.

	@param pbRule Pointer to the original Rules binary data.
	@param cbRule Size (in bytes) of the original Rules binary data.
	@param vbRule Reference to a vector for returning revised Rules binary data.
	@param hmwsid Map from the old style writing sytem code to the writing system object id.
	@param fForPara Flag whether this or not this Rule applies to paragraphs.

	@return true if something changed, false if the Rules data did not change.
----------------------------------------------------------------------------------------------*/
bool MigrateData::UpdateRulesWsCodeToId(BYTE * pbRule, ULONG cbRule, Vector<BYTE> & vbRule,
	HashMap<int,int> & hmwsid, bool fForPara)
{
	AssertArray(pbRule, cbRule);

	vbRule.Clear();
	if (cbRule < 2)
		return false;
	DataReaderRgb drr(pbRule, cbRule);	// makes reading integers from bytes easier.

	// Get the number of integer and "string" valued properties in this rule.
	byte rgbCnt[2];
	drr.ReadBuf(rgbCnt, 2);
	int ctip = rgbCnt[0];
	int ctsp = rgbCnt[1];
	Assert((int)(byte)ctip == ctip);
	Assert((int)(byte)ctsp == ctsp);
	if (ctip + ctsp == 0)
		return false;

	// Extract the properties from the binary data.
	TextProps::TextIntProp txip;
	Vector<TextProps::TextIntProp> vtxip;
	int itip;
	for (itip = 0; itip < ctip; ++itip)
	{
		TextProps::ReadTextIntProp(&drr, &txip);
		vtxip.Push(txip);
	}
	TextProps::TextStrProp txsp;
	Vector<TextProps::TextStrProp> vtxsp;
	StrUni stuWsStyles;
	int itsp;
	for (itsp = 0; itsp < ctsp; ++itsp)
	{
		TextProps::ReadTextStrProp(&drr, &txsp);
		if (txsp.m_tpt == ktptWsStyle)
			stuWsStyles = txsp.m_stuVal;
		else
			vtxsp.Push(txsp);
	}
	if (!stuWsStyles.Length())
		return false;			// The WsStyles is the only item that gives trouble!

	// Decode the WsStyles property.
	Vector<WsStyleInfo> vesi;
	Vector<int> vws;
	FwStyledText::DecodeFontPropsString(stuWsStyles.Bstr(), vesi, vws);

	// Store the properties for the English WsStyle at the top level for use as defaults.
	// Record their indexes in vtxip and vtxsp after storing.
	// Note that ktptFontFamily and ktptFontVariations do not move to the top level.
	bool fChanged = false;
	int i;
	int wsCodeEng = ParseWs("ENG");		// Always user interface ws in previous versions.
	int itspFontFamily = -1;
	int itipFontSize = -1;
	int itipForeColor = -1;
	int itipBackColor = -1;
	int itipUnderColor = -1;
	int itipUnderline = -1;
	int itipBold = -1;
	int itipItalic = -1;
	int itipSuperscript = -1;
	int itipOffset = -1;
	for (i = 0; i < vesi.Size(); ++i)
	{
		if (vesi[i].m_ws == wsCodeEng)
		{
			if (vesi[i].m_mpSize != knNinch)
			{
				txip.m_scp = kscpFontSize;
				txip.m_tpt = ktptFontSize;
				txip.m_nVal = vesi[i].m_mpSize;
				txip.m_nVar = ktpvMilliPoint;
				itipFontSize = MergeTextIntProp(vtxip, txip);
				fChanged = true;
				vesi[i].m_mpSize = knNinch;
			}
			if (vesi[i].m_clrFore != (COLORREF)knNinch)
			{
				txip.m_scp = kscpForeColor;
				txip.m_tpt = ktptForeColor;
				txip.m_nVal = (int)vesi[i].m_clrFore;
				txip.m_nVar = ktpvDefault;
				itipForeColor = MergeTextIntProp(vtxip, txip);
				fChanged = true;
				vesi[i].m_clrFore = (COLORREF)knNinch;
			}
			if (vesi[i].m_clrBack != (COLORREF)knNinch)
			{
				txip.m_scp = kscpBackColor;
				txip.m_tpt = ktptBackColor;
				txip.m_nVal = (int)vesi[i].m_clrBack;
				txip.m_nVar = ktpvDefault;
				itipBackColor = MergeTextIntProp(vtxip, txip);
				fChanged = true;
				vesi[i].m_clrBack = (COLORREF)knNinch;
			}
			if (vesi[i].m_clrUnder != (COLORREF)knNinch)
			{
				txip.m_scp = kscpUnderColor;
				txip.m_tpt = ktptUnderColor;
				txip.m_nVal = (int)vesi[i].m_clrUnder;
				txip.m_nVar = ktpvDefault;
				itipUnderColor = MergeTextIntProp(vtxip, txip);
				fChanged = true;
				vesi[i].m_clrUnder = (COLORREF)knNinch;
			}
			if (vesi[i].m_unt != knNinch)
			{
				txip.m_scp = kscpUnderline;
				txip.m_tpt = ktptUnderline;
				txip.m_nVal = vesi[i].m_unt;
				txip.m_nVar = ktpvEnum;
				itipUnderline = MergeTextIntProp(vtxip, txip);
				fChanged = true;
				vesi[i].m_unt = knNinch;
			}
			if (vesi[i].m_fBold != knNinch)
			{
				txip.m_scp = kscpBold;
				txip.m_tpt = ktptBold;
				txip.m_nVal = vesi[i].m_fBold;
				txip.m_nVar = ktpvEnum;
				itipBold = MergeTextIntProp(vtxip, txip);
				fChanged = true;
				vesi[i].m_fBold = knNinch;
			}
			if (vesi[i].m_fItalic != knNinch)
			{
				txip.m_scp = kscpItalic;
				txip.m_tpt = ktptItalic;
				txip.m_nVal = vesi[i].m_fItalic;
				txip.m_nVar = ktpvEnum;
				itipItalic = MergeTextIntProp(vtxip, txip);
				fChanged = true;
				vesi[i].m_fItalic = knNinch;
			}
			if (vesi[i].m_ssv != knNinch)
			{
				txip.m_scp = kscpSuperscript;
				txip.m_tpt = ktptSuperscript;
				txip.m_nVal = vesi[i].m_ssv;
				txip.m_nVar = ktpvEnum;
				itipSuperscript = MergeTextIntProp(vtxip, txip);
				fChanged = true;
				vesi[i].m_ssv = knNinch;
			}
			if (vesi[i].m_mpOffset != knNinch)
			{
				txip.m_scp = kscpOffset;
				txip.m_tpt = ktptOffset;
				txip.m_nVal = vesi[i].m_mpOffset;
				txip.m_nVar = ktpvMilliPoint;
				itipOffset = MergeTextIntProp(vtxip, txip);
				fChanged = true;
				vesi[i].m_mpOffset = knNinch;
			}
			if (vesi[i].m_stuFontFamily.Length())
			{
				if (vesi[i].m_stuFontFamily == L"<default serif>" ||
					vesi[i].m_stuFontFamily == L"<default sans serif>" ||
					vesi[i].m_stuFontFamily == L"<default monospace>")
				{
					txsp.m_tpt = ktptFontFamily;
					txsp.m_stuVal = vesi[i].m_stuFontFamily;
					itspFontFamily = MergeTextStrProp(vtxsp, txsp);
					fChanged = true;
					vesi[i].m_stuFontFamily.Clear();
				}
			}
			if (!vesi[i].m_stuFontFamily.Length() && !vesi[i].m_stuFontVar.Length())
			{
				vesi.Delete(i);
				fChanged = true;
			}
			break;
		}
	}

	// Now, remove any properties which are redundant, that is, the same as the top level
	// properties.
	int cEmpty;
	int cProps;
	for (i = 0; i < vesi.Size(); ++i)
	{
		if (vesi[i].m_ws == 0)
		{
			vesi.Delete(i);
			--i;
			continue;
		}

		cEmpty = 0;
		cProps = 0;

		if (vesi[i].m_stuFontFamily.Length())
		{
			if (itspFontFamily != -1 &&
				vtxsp[itspFontFamily].m_stuVal == vesi[i].m_stuFontFamily)
			{
				vesi[i].m_stuFontFamily.Clear();
				++cEmpty;
			}
		}
		else
		{
			++cEmpty;
		}
		++cProps;

		if (!vesi[i].m_stuFontVar.Length())
			++cEmpty;
		++cProps;

		if (vesi[i].m_mpSize != knNinch)
		{
			if (itipFontSize != -1 && vtxip[itipFontSize].m_nVal == vesi[i].m_mpSize)
			{
				vesi[i].m_mpSize = knNinch;
				++cEmpty;
			}
		}
		else
		{
			++cEmpty;
		}
		++cProps;

		if (vesi[i].m_clrFore != knNinch)
		{
			if (itipForeColor != -1 && vtxip[itipForeColor].m_nVal == (int)vesi[i].m_clrFore)
			{
				vesi[i].m_clrFore = (COLORREF)knNinch;
				++cEmpty;
			}
		}
		else
		{
			++cEmpty;
		}
		++cProps;

		if (vesi[i].m_clrBack != knNinch)
		{
			if (itipBackColor != -1 && vtxip[itipBackColor].m_nVal == (int)vesi[i].m_clrBack)
			{
				vesi[i].m_clrBack = (COLORREF)knNinch;
				++cEmpty;
			}
		}
		else
		{
			++cEmpty;
		}
		++cProps;

		if (vesi[i].m_clrUnder != knNinch)
		{
			if (itipUnderColor != -1 && vtxip[itipUnderColor].m_nVal == (int)vesi[i].m_clrUnder)
			{
				vesi[i].m_clrUnder = (unsigned)knNinch;
				++cEmpty;
			}
		}
		else
		{
			++cEmpty;
		}
		++cProps;

		if (vesi[i].m_unt != knNinch)
		{
			if (itipUnderline != -1 && vtxip[itipUnderline].m_nVal == vesi[i].m_unt)
			{
				vesi[i].m_unt = knNinch;
				++cEmpty;
			}
		}
		else
		{
			++cEmpty;
		}
		++cProps;

		if (vesi[i].m_fBold != knNinch)
		{
			if (itipBold != -1 && vtxip[itipBold].m_nVal == vesi[i].m_fBold)
			{
				vesi[i].m_fBold = knNinch;
				++cEmpty;
			}
		}
		else
		{
			++cEmpty;
		}
		++cProps;

		if (vesi[i].m_fItalic != knNinch)
		{
			if (itipItalic != -1 && vtxip[itipItalic].m_nVal == vesi[i].m_fItalic)
			{
				vesi[i].m_fItalic = knNinch;
				++cEmpty;
			}
		}
		else
		{
			++cEmpty;
		}
		++cProps;

		if (vesi[i].m_ssv != knNinch)
		{
			if (itipSuperscript != -1 && vtxip[itipSuperscript].m_nVal == vesi[i].m_ssv)
			{
				vesi[i].m_ssv = knNinch;
				++cEmpty;
			}
		}
		else
		{
			++cEmpty;
		}
		++cProps;

		if (vesi[i].m_mpOffset != knNinch)
		{
			if (itipOffset != -1 && vtxip[itipOffset].m_nVal == vesi[i].m_mpOffset)
			{
				vesi[i].m_mpOffset = knNinch;
				++cEmpty;
			}
		}
		else
		{
			++cEmpty;
		}
		++cProps;

		if (cEmpty == cProps)
		{
			// If the WsStyle is subsumed by the defaults, delete it.
			vesi.Delete(i);
			--i;
		}
		else
		{
			// Update the writing system value from the code to the id.  If it doesn't map,
			// delete it.
			int ws;
			if (hmwsid.Retrieve(vesi[i].m_ws, &ws))
			{
				vesi[i].m_ws = ws;
			}
			else
			{
				vesi.Delete(i);
				--i;
			}
		}
		fChanged = true;
	}
	if (!fChanged)
		return false;

	if (vesi.Size())
	{
		// Encode the revised WsStyles property, and add it to vtxsp.
		txsp.m_tpt = ktptWsStyle;
		txsp.m_stuVal = FwStyledText::EncodeFontPropsString(vesi, fForPara);
		vtxsp.Push(txsp);
	}

	// Encode the revised binary Rules data from vtxip and vtxsp.
	// RUles can't be more than 8000 bytes long (and probably are much shorter).
	vbRule.Resize(8000);
	DataWriterRgb dwr(vbRule.Begin(), vbRule.Size());

	rgbCnt[0] = static_cast<byte>(vtxip.Size());
	rgbCnt[1] = static_cast<byte>(vtxsp.Size());
	Assert((int)rgbCnt[0] == vtxip.Size());
	Assert((int)rgbCnt[1] == vtxsp.Size());
	dwr.WriteBuf(rgbCnt, 2);

	for (itip = 0; itip < vtxip.Size(); ++itip)
		TextProps::WriteTextIntProp(&dwr, &vtxip[itip]);

	for (itsp = 0; itsp < vtxsp.Size(); ++itsp)
		TextProps::WriteTextStrProp(&dwr, &vtxsp[itsp]);

	// Resize the binary Rules data vector to its actual size.
	vbRule.Resize(dwr.IbCur());

	return true;
}


/*----------------------------------------------------------------------------------------------
	Create a new database with the given name, using the indicated initialization SQL script.
	The SQL script is stored in a ZIP file with the same basename in the data migration folder.

	@param pszInitScript Name of the SQL initialization script (minus the trailing ".sql")
	@param pszDbName Name of the new (temporary?) database to be initialized with that script.
----------------------------------------------------------------------------------------------*/
bool MigrateData::CreateNewDb(const wchar * pszInitScript, const wchar * pszDbName)
{
	AssertPsz(pszInitScript);
	AssertPsz(pszDbName);

	// Unzip the initialization script file.
	StrUni stuZipDir(DirectoryFinder::FwMigrationScriptDir());
	StrUni stuZipFile;
	stuZipFile.Format(L"%s\\%s.zip", stuZipDir.Chars(), pszInitScript);
	StrUni stuSqlFile;
	stuSqlFile.Format(L"%s.sql", pszInitScript);
	try
	{
		// Initialize zip system data:
		ZipData zipd;
		zipd.m_sbstrDevice.Assign(stuZipDir.Chars(), 3);	// eg, "C:\"
		zipd.m_sbstrPath = stuZipDir;
		XceedZip xczUnzipper;
		// Initialize Xceed Zip module:
		if (!xczUnzipper.Init(&zipd))
			return false;
		xczUnzipper.SetPreservePaths(false);
		xczUnzipper.SetProcessSubfolders(false);
		xczUnzipper.SetZipFilename(stuZipFile.Bstr());
		xczUnzipper.SetFilesToProcess(stuSqlFile.Bstr());
		xczUnzipper.SetUnzipToFolder(stuZipDir.Bstr());

		// Unzip the SQL script.
		long nUnzip = xczUnzipper.Unzip();
		// Ignore warnings. Xceedzip 4.5.81.0 returns warnings (526) here where 4.5.77.0 didn't.
		// We already ignore these in BackupHandler::UnzipForRestore.
		if (nUnzip != 0 && nUnzip != 526)
		{
			// REVIEW: Display error message?
			return false;
		}
	}
	catch (...)
	{
		return false;
	}

	StrUni stuDbDir(m_stuFwData);
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stuCmd;

	// Create the bare database, first deleting any existing database with the same name.
	StrUni stuDB(L"master");
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(m_stuServer.Bstr(), stuDB.Bstr(), m_qfist, koltMsgBox, koltvForever));
	CheckHr(qode->CreateCommand(&qodc));
	stuCmd.Format(
		L"if ((select [name] from [sysdatabases] where [name] = N'%<0>s') is not null)%n"
		L"    drop database [%<0>s]", pszDbName);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
	// In some cases after a failure, the database files may be present but are not attached.
	// In this case the above doesn't delete the files, so to make sure, we'll delete them
	// here to make sure they are gone.
	StrUni stuFile;
	stuFile.Format(L"%<0>s\\%<1>s.mdf", stuDbDir.Chars(), pszDbName);
	::DeleteFileW(stuFile.Chars());
	stuFile.Format(L"%<0>s\\%<1>s_log", stuDbDir.Chars(), pszDbName);
	::DeleteFileW(stuFile.Chars());
	// Now create the new database.
	stuCmd.Format(
		L"create database [%<0>s] ON (NAME = '%<0>s', FILENAME = '%<1>s\\%<0>s.mdf') "
		L"LOG ON ( NAME = '%<0>s_log', FILENAME = '%<1>s\\%<0>s_log.ldf',"
		L"SIZE = 10MB,MAXSIZE = UNLIMITED,FILEGROWTH = 5MB )",
		pszDbName, stuDbDir.Chars());
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
	qodc.Clear();
	qode.Clear();

	// Now, initialize the newly created database.
	qode.CreateInstance(CLSID_OleDbEncap);
	SmartBstr sbstrDb(pszDbName);
	CheckHr(qode->Init(m_stuServer.Bstr(), sbstrDb, m_qfist, koltMsgBox, koltvForever));
	bool fOk = RunSql(qode, stuSqlFile.Chars());

	// Now that we're done with it, delete the SQL script file that we unzipped earlier.
	// First, get the full pathname of the SQL script file.
	stuSqlFile.Format(L"%s\\%s.sql", stuZipDir.Chars(), pszInitScript);
	::DeleteFileW(stuSqlFile.Chars());

	return fOk;
}


/*----------------------------------------------------------------------------------------------
	Install any languages in the database that have not yet been installed for use by ICU.

	@param pode Pointer to a database connection.
----------------------------------------------------------------------------------------------*/
bool MigrateData::InstallLanguages(IOleDbEncap * pode)
{
	try
	{
		ILgWritingSystemFactoryPtr qwsf;
		ILgWritingSystemFactoryBuilderPtr qwsfb;
		qwsfb.CreateInstance(CLSID_LgWritingSystemFactoryBuilder);
		CheckHr(qwsfb->GetWritingSystemFactory(pode, NULL, &qwsf));

		int cws;
		Vector<int> vws;
		CheckHr(qwsf->get_NumberOfWs(&cws));
		vws.Resize(cws);
		CheckHr(qwsf->GetWritingSystems(vws.Begin(), cws));
		for (int iws = 0; iws < cws; ++iws)
		{
			IWritingSystemPtr qws;
			// Getting an engine will also install the language.
			CheckHr(qwsf->get_EngineOrNull(vws[iws], &qws));
			AssertPtr(qws.Ptr());
		}
		CheckHr(qwsf->Shutdown()); // required after calling GetWritingSystemFactory.
		return true;
	}
	catch (...)
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	If any XML files with a name matching the version number exist, process them to update a
	list in the database.

	@param pode Pointer to a database connection.
	@param nVersion version number
----------------------------------------------------------------------------------------------*/
void MigrateData::ProcessPossibleXmlUpdateFiles(IOleDbEncap * pode, int nVersion)
{
	Vector<StrUni> vstuFiles;			// vector of file names
	StrUni stuFwRoot = DirectoryFinder::FwMigrationScriptDir();
	StrUni stuFilePattern;
	stuFilePattern.Format(_T("%s\\%d-*.xml"), stuFwRoot.Chars(), nVersion);
	WIN32_FIND_DATAW wfd;
	HANDLE hFind = ::FindFirstFileW(stuFilePattern.Chars(), &wfd);
	if (hFind == INVALID_HANDLE_VALUE)
		return;
	for (BOOL fOk = true; fOk; fOk = ::FindNextFileW(hFind, &wfd));
	{
		StrUni stuFile;
		stuFile.Format(L"%s\\%s", stuFwRoot.Chars(), wfd.cFileName);
		vstuFiles.Push(stuFile);
	}
	::FindClose(hFind);
	for (int i = 0; i < vstuFiles.Size(); ++i)
	{
		ProcessXmlUpdateFile(pode, vstuFiles[i]);
	}
}

/*----------------------------------------------------------------------------------------------
	Process the given list update XML file.

	@param pode Pointer to a database connection.
	@param stuFile reference to the filename string
----------------------------------------------------------------------------------------------*/
void MigrateData::ProcessXmlUpdateFile(IOleDbEncap * pode, const StrUni & stuFile)
{
	// Parse the filename to get the list XML tag.

	const wchar * pszFileName = wcsrchr(stuFile.Chars(), '\\');
	Assert(pszFileName != NULL);
	++pszFileName;

	const wchar * pszFieldTag = wcschr(pszFileName, '-');
	Assert(pszFieldTag != NULL);
	if (pszFieldTag == NULL)
		return;
	StrUni stuVersion(pszFileName, pszFieldTag - pszFileName);
	++pszFieldTag;
	const wchar * pszPeriod = wcschr(pszFieldTag, '.');
	Assert(pszPeriod != NULL);
	if (pszPeriod == NULL)
		return;
	StrUni stuFieldTag(pszFieldTag, pszPeriod - pszFieldTag);
	int cchFieldName = wcscspn(stuFieldTag.Chars(), L"0123456789");
	if (cchFieldName == stuFieldTag.Length())
		return;			// missing class id number, must be for someother purpose...
	StrUni stuFieldName(pszFieldTag, cchFieldName);
	wchar * pszPeriod2;
	ULONG cid = wcstoul(pszFieldTag + cchFieldName, &pszPeriod2, 10);
	Assert(pszPeriod == pszPeriod2);
	if (pszPeriod != pszPeriod2)
		return;			// something funny here...

	// Get the field id, the class name, and the list owner database id.

	IFwMetaDataCachePtr qmdc;
	qmdc.CreateInstance(CLSID_FwMetaDataCache, CLSCTX_INPROC_SERVER);
	AssertPtr(qmdc);
	CheckHr(qmdc->Init(pode));
	ULONG fidList;
	CheckHr(qmdc->GetFieldId2(cid, stuFieldName.Bstr(), true, &fidList));
	SmartBstr sbstrClass;
	CheckHr(qmdc->GetClassName(cid, &sbstrClass));

	StrUni stuQuery;
	stuQuery.Format(L"SELECT TOP 1 [Id] FROM %s", sbstrClass.Chars());
	int hvoListOwner = ReadOneIntFromDatabase(pode, stuQuery.Bstr());

	SmartBstr sbstrServer;
	SmartBstr sbstrDatabase;
	CheckHr(pode->get_Server(&sbstrServer));
	CheckHr(pode->get_Database(&sbstrDatabase));

	StrUni stuFmt(kstidMigrateIncrementalXml);
	StrUni stuMsg;
	// "Updating list (%s %s) for version %s."
	stuMsg.Format(stuFmt.Chars(), sbstrClass.Chars(), stuFieldName.Chars(), stuVersion.Chars());
	m_qprog->put_Message(stuMsg.Bstr());
	IAdvIndPtr qadvi;
	CheckHr(m_qprog->QueryInterface(IID_IAdvInd, (void **)&qadvi));

	// Update the list.

	IFwXmlDataPtr qfwxd;
	qfwxd.CreateInstance(CLSID_FwXmlData, CLSCTX_INPROC_SERVER);
	AssertPtr(qfwxd);
	IFwXmlData2Ptr qfwxd2;
	CheckHr(qfwxd->QueryInterface(IID_IFwXmlData2, (void **)&qfwxd2));
	CheckHr(qfwxd2->Open(sbstrServer, sbstrDatabase));

	CheckHr(qfwxd2->UpdateListFromXml(stuFile.Bstr(), hvoListOwner, (int)fidList, qadvi));

	CheckHr(qfwxd2->Close());
}


/*----------------------------------------------------------------------------------------------
	Read one integer from the database, using the supplied query.

	@param pode Pointer to a database connection.
	@param bstrQuery

	@return the integer value read from the database, or 0 if the query result is null
----------------------------------------------------------------------------------------------*/
int MigrateData::ReadOneIntFromDatabase(IOleDbEncap * pode, BSTR bstrQuery)
{
	IOleDbCommandPtr qodc;
	CheckHr(pode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(bstrQuery, knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	ComBool fMoreRows;
	CheckHr(qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		ComBool fIsNull;
		ULONG cbSpaceTaken;
		int nVal;
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&nVal), sizeof(nVal),
			&cbSpaceTaken, &fIsNull, 0));
		if (fIsNull || cbSpaceTaken == 0)
			return 0;
		else
			return nVal;
	}
	else
	{
		return 0;
	}
}


/*----------------------------------------------------------------------------------------------
	Check the style rules for invalid BulNumFontInfo font sizes, and fix any errors found.

	@param pode Pointer to a database connection.

	@return true if successful, false if an error occurs
----------------------------------------------------------------------------------------------*/
bool MigrateData::FixBulNumFontInfoFontSize(IOleDbEncap * pode)
{
	IOleDbCommandPtr qodc;
	HVO hvo;
	BYTE rgbRule[8000];
	ULONG cbRule;
	Vector<BYTE> vbRule;
	Vector<HVO> vhvo;
	Vector< Vector<BYTE> > vvbRules;
	StrUni stuCmd(L"SELECT [Id], Rules FROM StStyle WHERE Type = 0 AND Rules IS NOT NULL");
	CheckHr(pode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	ComBool fMoreRows;
	ComBool fIsNull;
	ULONG cbSpaceTaken;
	CheckHr(qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		// Get the data for one row of the table.
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), sizeof(hvo),
			&cbSpaceTaken, &fIsNull, 0));
		Assert(!fIsNull);
		CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(rgbRule), sizeof(rgbRule),
			&cbRule, &fIsNull, 0));
		Assert(!fIsNull);

		// Update the Rules value.
		if (UpdateBulNumFontInfoFontSize(rgbRule, cbRule, vbRule))
		{
			// Store the information for use later.
			vhvo.Push(hvo);
			vvbRules.Push(vbRule);
		}
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	if (vhvo.Size())
	{
		CheckHr(pode->CreateCommand(&qodc));
		for (int i = 0; i < vhvo.Size(); ++i)
		{
			stuCmd.Format(L"UPDATE StStyle SET Rules=? WHERE [Id]=%d", vhvo[i]);
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(vvbRules[i].Begin()), vvbRules[i].Size()));
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		}
	}
	vhvo.Clear();
	vvbRules.Clear();
	qodc.Clear();

	return true;
}

/*----------------------------------------------------------------------------------------------
	Check the style rules for invalid BulNumFontInfo font sizes, and fix any errors found.

	@param pointer to byte array containing style rule
	@param number of bytes in the array
	@param reference to a byte vector to contain output if rule changes

	@return true if data changed, false otherwise
----------------------------------------------------------------------------------------------*/
bool MigrateData::UpdateBulNumFontInfoFontSize(BYTE * pbRule, ULONG cbRule,
	Vector<BYTE> & vbRule)
{
	AssertArray(pbRule, cbRule);

	vbRule.Clear();
	if (cbRule < 2)
		return false;
	DataReaderRgb drr(pbRule, cbRule);	// makes reading integers from bytes easier.

	// Get the number of integer and "string" valued properties in this rule.
	byte rgbCnt[2];
	drr.ReadBuf(rgbCnt, 2);
	int ctip = rgbCnt[0];
	int ctsp = rgbCnt[1];
	Assert((int)(byte)ctip == ctip);
	Assert((int)(byte)ctsp == ctsp);
	if (ctip + ctsp == 0)
		return false;

	// Extract the properties from the binary data.
	TextProps::TextIntProp txip;
	Vector<TextProps::TextIntProp> vtxip;
	int itip;
	for (itip = 0; itip < ctip; ++itip)
	{
		TextProps::ReadTextIntProp(&drr, &txip);
		vtxip.Push(txip);
	}
	TextProps::TextStrProp txsp;
	Vector<TextProps::TextStrProp> vtxsp;
	StrUni stuBulNumFontInfo;
	int itsp;
	for (itsp = 0; itsp < ctsp; ++itsp)
	{
		TextProps::ReadTextStrProp(&drr, &txsp);
		if (txsp.m_tpt == ktptBulNumFontInfo)
			stuBulNumFontInfo = txsp.m_stuVal;
		else
			vtxsp.Push(txsp);
	}
	if (!stuBulNumFontInfo.Length())
		return false;			// The WsStyles is the only item that gives trouble!

	// Now we need to decode stuBulNumFontInfo, and decide whether we have a problem.
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	FmtBulNumDlg::DecodeFontInfo(stuBulNumFontInfo, qtpb);
	int nVar = 0;
	int nVal = 0;
	CheckHr(qtpb->GetIntPropValues(ktptFontSize, &nVar, &nVal));
	if (nVar == -1)
		return false;
	if (nVal == 0 || nVal >= 8000)
		return false;

	// Multiply by 16, then round off to nearest 1000.
	nVal <<= 4;
	nVal += 100;
	nVal /= 1000;
	if (nVal < 8)
		nVal = 8;
	else if (nVal > 72)
		nVal = 72;
	CheckHr(qtpb->SetIntPropValues(ktptFontSize, nVar, nVal * 1000));
	ITsTextPropsPtr qttp;
	CheckHr(qtpb->GetTextProps(&qttp));

	// Encode the revised BulNumFontInfo property, and add it to vtxsp.
	txsp.m_tpt = ktptBulNumFontInfo;
	txsp.m_stuVal = FmtBulNumDlg::EncodeFontInfo(qttp);
	vtxsp.Push(txsp);

	// Encode the revised binary Rules data from vtxip and vtxsp.
	// RUles can't be more than 8000 bytes long (and probably are much shorter).
	vbRule.Resize(8000);
	DataWriterRgb dwr(vbRule.Begin(), vbRule.Size());

	rgbCnt[0] = static_cast<byte>(vtxip.Size());
	rgbCnt[1] = static_cast<byte>(vtxsp.Size());
	Assert((int)rgbCnt[0] == vtxip.Size());
	Assert((int)rgbCnt[1] == vtxsp.Size());
	dwr.WriteBuf(rgbCnt, 2);

	for (itip = 0; itip < vtxip.Size(); ++itip)
		TextProps::WriteTextIntProp(&dwr, &vtxip[itip]);

	for (itsp = 0; itsp < vtxsp.Size(); ++itsp)
		TextProps::WriteTextStrProp(&dwr, &vtxsp[itsp]);

	// Resize the binary Rules data vector to its actual size.
	vbRule.Resize(dwr.IbCur());

	return true;
}


#include "Vector_i.cpp"
#include "Hashmap_i.cpp"
#include "Set_i.cpp"

// Local Variables:
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkmig.bat"
// End: (These 4 lines are useful to Steve McConnel.)
