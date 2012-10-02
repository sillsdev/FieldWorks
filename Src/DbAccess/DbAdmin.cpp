/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, SIL International. All rights reserved.

File: DbAdmin.cpp
Responsibility: John Thomson

Description:
	Implementation of database administration interfaces.

	This file contains class definitions for the following classes:
		DbAdmin
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#include <sys/stat.h>
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	DbAdmin - Constructor/Destructor.
//:>********************************************************************************************
DbAdmin::DbAdmin()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}


DbAdmin::~DbAdmin()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>    Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.DbAccess.DbAdmin"),
	&CLSID_DbAdmin,
	_T("SIL database access"),
	_T("Apartment"),
	&DbAdmin::CreateCom);


void DbAdmin::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
	{
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	}
	ComSmartPtr<DbAdmin> qzode;
	qzode.Attach(NewObj DbAdmin());		// ref count initially 1
	CheckHr(qzode->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	IDbAdmin - IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP DbAdmin::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IDbAdmin *>(this));
	else if (iid == IID_IDbAdmin)
		*ppv = static_cast<IDbAdmin *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(
			static_cast<IUnknown *>(static_cast<IDbAdmin *>(this)), IID_IDbAdmin);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


STDMETHODIMP_(ULONG) DbAdmin::AddRef(void)
{
	Assert(m_cref > 0);
	::InterlockedIncrement(&m_cref);
	return m_cref;
}

STDMETHODIMP_(ULONG) DbAdmin::Release(void)
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
//:>	IDbAdmin Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Copy a (detached) database to a (detached) destination.
	Source path is the full name without extension of the mdf/log file pair.
	Destination path is the full name without extension of the mdf/log pair to create.
	This does not attach the database or change its internal name.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbAdmin::CopyDatabase(BSTR bstrSrcPathName, BSTR bstrDstPathName)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrSrcPathName);
	ChkComBstrArg(bstrDstPathName);
	StrUni stuMdfSrcName(bstrSrcPathName);
	stuMdfSrcName += L".mdf";
	StrUni stuMdfDstName(bstrDstPathName);
	stuMdfDstName += L".mdf";
	if (::CopyFileW(stuMdfSrcName.Chars(), stuMdfDstName.Chars(), TRUE) == 0) // fail if exists
		return E_FAIL;
	END_COM_METHOD(g_fact, IID_IDbAdmin);
}

/*----------------------------------------------------------------------------------------------
	Attach a database so it can be used.
	The first argument gives the internal name to be assigned to the database.
	The second gives the full path (excluding any extension) to the mdf/log file pair.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbAdmin::AttachDatabase(BSTR bstrDatabaseName, BSTR bstrPathName)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrDatabaseName);
	ChkComBstrArg(bstrPathName);
	IOleDbEncapPtr qode; // Declare before qodc.
	qode.CreateInstance(CLSID_OleDbEncap);
	StrUni stuServer(SilUtil::LocalServerName());
	if (!stuServer.Length())
		stuServer.Assign(L".\\SILFW");
	StrUni stuMaster(L"master");
	// Put up a message box if unable to attach master; no timeout.
	CheckHr(qode->Init(stuServer.Bstr(), stuMaster.Bstr(), m_qfist, koltMsgBox,
		koltvForever));
	// Check whether the log file exists, and build the attach command accordingly.
	StrUni stuLdf;
	stuLdf.Format(L"%s_log.ldf", bstrPathName);
	StrUni stuLdf2;
	StrUni stuPath(bstrPathName); // We'll be using the path up to the db name.
	int cchPath =  stuPath.ReverseFindCh('\\');
	if (cchPath > 0)
	{
		// We also want to copy the backslash (LT-9099)
		stuLdf2.Assign(stuPath.Chars(), cchPath + 1);
		stuLdf2.FormatAppend(L"%s_log.ldf", bstrDatabaseName);
	}
	HRESULT hr = S_OK;
	try
	{
		StrUni stuDbName(bstrDatabaseName);
		WIN32_FIND_DATA wfd;
		HANDLE hFind = ::FindFirstFileW(stuLdf.Chars(), &wfd);
		HANDLE hFind2 = INVALID_HANDLE_VALUE;
		if (stuLdf2.Length() != 0 && hFind == INVALID_HANDLE_VALUE)
		{
			hFind2 = ::FindFirstFileW(stuLdf2.Chars(), &wfd);
		}
		if (hFind != INVALID_HANDLE_VALUE)
		{
			::FindClose(hFind);
			// sp_attach_db will attach whatever transaction log is passed to it. The
			// FW default is to name the transaction log file the same as the database
			// database file name + "_log", such as TestLangProj.mdf and
			// TestLangProj_log.ldf.
			stuPath.Append(L".mdf");
			AttachDatabase(qode, stuDbName.Chars(), stuPath.Chars(), stuLdf.Chars());
		}
		else if (hFind2 != INVALID_HANDLE_VALUE)
		{
			::FindClose(hFind);
			stuPath.Replace(cchPath, stuPath.Length(), bstrDatabaseName);
			stuPath.Append(L".mdf");
			AttachDatabase(qode, stuDbName.Chars(), stuPath.Chars(), stuLdf2.Chars());
		}
		else
		{
			// sp_attach_single_file_db generates a new lof file. Unfortunately, the new log
			// file is named after the database name, not the file name of the database. For
			// example, CopyOfBlankLangProj.mdf and DummyDb_log.ldf. The same is true with
			// using sp_attach_db.
			//
			stuPath.Append(L".mdf");
			AttachDatabase(qode, stuDbName.Chars(), stuPath.Chars(), NULL);
		}
		// The try-catch blocks prevent an assert later when an error occurs here.  See
		// Generic/Throwable.h for an explanation.
	}
	catch (Throwable & thr)
	{
		hr = thr.Result();
	}
	qode.Clear();
	return hr;
	END_COM_METHOD(g_fact, IID_IDbAdmin);
}

/*----------------------------------------------------------------------------------------------
	Detach a database.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbAdmin::DetachDatabase(BSTR bstrDatabaseName)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrDatabaseName);
	IOleDbEncapPtr qode; // Declare before qodc.
	qode.CreateInstance(CLSID_OleDbEncap);
	StrUni stuServer(SilUtil::LocalServerName());
	if (!stuServer.Length())
		stuServer.Assign(L".\\SILFW");
	StrUni stuMaster(L"master");
	// Put up a message box if unable to attach master; no timeout.
	CheckHr(qode->Init(stuServer.Bstr(), stuMaster.Bstr(), m_qfist, koltMsgBox,
		koltvForever));
	if (!DetachDatabase(qode, bstrDatabaseName))
		ThrowHr(E_FAIL);
	qode.Clear();
	// We randomly have SQLServer locking access to the database file in the process of
	// attaching and detaching, so we'll try to sleep awhile after the detach to let
	// everything settle down. It never seems to give trouble when single-stepping. Once
	// locked, however, the only way to unlock it seems to be to stop SQL Server.
	Sleep(1500);
	END_COM_METHOD(g_fact, IID_IDbAdmin);
}

/*----------------------------------------------------------------------------------------------
	Rename a database.
	The database is found in bstrDirName\bstrOldName.mdf.
	This file is renamed to bstrDirName\bstrNewName.mdf.
	If there is a file bstrDirName\bstrOldName_log.ldf, it is deleted. (A new log is typially
	created by attaching the renamed database.)
	If the database is attached to begin with, set fDetachBefore to get it detached before the
	files are renamed. This assumes its internal name is the same as the main body of the
	filename.
	Set fAttachAfter to attach the database afterwards and give it the internal name
	bstrNewName.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbAdmin::RenameDatabase(BSTR bstrDirName, BSTR bstrOldName,
	BSTR bstrNewName, ComBool fDetachBefore, ComBool fAttachAfter)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrDirName);
	ChkComBstrArg(bstrOldName);
	ChkComBstrArg(bstrNewName);

	if (fDetachBefore)
		CheckHr(DetachDatabase(bstrOldName));

	StrUni stuOldMdfPath = bstrDirName;
	stuOldMdfPath += L"\\";
	stuOldMdfPath += bstrOldName;
	StrUni stuOldLogPath = stuOldMdfPath;
	stuOldMdfPath += L".mdf";
	stuOldLogPath += L"_log.ldf";

	StrUni stuNewPath = bstrDirName;
	stuNewPath += L"\\";
	stuNewPath += bstrNewName;
	StrUni stuNewPathMdf = stuNewPath;
	stuNewPathMdf += L".mdf";
	if (0 == ::MoveFile(stuOldMdfPath.Chars(), stuNewPathMdf.Chars()))
		return E_FAIL;
	// Ignore any errors, we did our best. Most likely it had never been attached so the
	// log file did not exist.
	::DeleteFile(stuOldLogPath.Chars());

	if (fAttachAfter)
		CheckHr(AttachDatabase(bstrNewName, stuNewPath.Bstr()));
	END_COM_METHOD(g_fact, IID_IDbAdmin);
}

/*----------------------------------------------------------------------------------------------
	Set a log stream that can be used to report errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbAdmin::putref_LogStream(IStream * pstrm)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstrm);
	m_qfist = pstrm;
	END_COM_METHOD(g_fact, IID_IDbAdmin);
}


/*----------------------------------------------------------------------------------------------
	Get the FW root directory
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbAdmin::get_FwRootDir(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);
	*pbstr = ::SysAllocString(DirectoryFinder::FwRootCodeDir());
	END_COM_METHOD(g_fact, IID_IDbAdmin);
}
/*----------------------------------------------------------------------------------------------
	Get the FW directory that holds data migration scripts.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbAdmin::get_FwMigrationScriptDir(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);
	*pbstr = ::SysAllocString(DirectoryFinder::FwMigrationScriptDir());
	END_COM_METHOD(g_fact, IID_IDbAdmin);
}
/*----------------------------------------------------------------------------------------------
	Get the FW directory that holds FW databases
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbAdmin::get_FwDatabaseDir(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);
	*pbstr = ::SysAllocString(DirectoryFinder::FwDatabaseDir());
	END_COM_METHOD(g_fact, IID_IDbAdmin);
}
/*----------------------------------------------------------------------------------------------
	Get the FW directory that holds templates (e.g., BlankLangProj)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbAdmin::get_FwTemplateDir(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);
	*pbstr = ::SysAllocString(DirectoryFinder::FwTemplateDir());
	END_COM_METHOD(g_fact, IID_IDbAdmin);
}

/*----------------------------------------------------------------------------------------------
	Rename the database wherever it is on the disk.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DbAdmin::SimplyRenameDatabase(BSTR bstrOldName, BSTR bstrNewName)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrOldName);
	ChkComBstrArg(bstrNewName);

	StrUni stuSvrName(SilUtil::LocalServerName());
	StrUni stuMasterDbName(L"master");
	IOleDbEncapPtr qode;
	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(stuSvrName.Bstr(), stuMasterDbName.Bstr(), m_qfist, koltMsgBox,
		koltvForever));
	// Verify that the new database does not exist.
	SmartBstr sbstrNewMdfFile;
	GetMdfFileOfDatabase(qode, bstrNewName, &sbstrNewMdfFile);
	if (sbstrNewMdfFile.Length() != 0)
		return E_FAIL;
	// Verify that the old database exists, and get its filenames.
	SmartBstr sbstrOldMdfFile;
	GetMdfFileOfDatabase(qode, bstrOldName, &sbstrOldMdfFile);
	if (sbstrOldMdfFile.Length() == 0)
		return E_FAIL;
	if (_waccess(sbstrOldMdfFile.Chars(), 0) == -1)
		return E_FAIL;
	StrUni stuDbLogFileExt;
	StrUni stuOldLogFile(sbstrOldMdfFile.Chars());
	int idx = stuOldLogFile.FindStrCI(L".mdf");
	if (idx >= 0)
		stuOldLogFile.Replace(idx, stuOldLogFile.Length(), L".ldf");
	else
		stuOldLogFile.Append(L".ldf");
	if (_waccess(stuOldLogFile.Chars(), 0) == -1)
	{
		idx = stuOldLogFile.FindStr(L".ldf");
		Assert(idx >= 0);
		stuOldLogFile.Replace(idx, stuOldLogFile.Length(), L"_log.ldf");
		if (_waccess(stuOldLogFile.Chars(), 0) == 0)
			stuDbLogFileExt.Assign("_log.ldf");
		else
			stuOldLogFile.Assign("");
	}
	else
	{
		stuDbLogFileExt.Assign(".ldf");
	}
	StrUni stuNewMdfFile(sbstrOldMdfFile.Chars());
	idx = stuNewMdfFile.ReverseFindCh('\\');
	if (idx < 0)
		idx = stuNewMdfFile.ReverseFindCh('/');
	Assert(idx >= 0);
	stuNewMdfFile.Replace(idx + 1, stuNewMdfFile.Length(), bstrNewName);
	stuNewMdfFile.Append(L".mdf");
	if (_waccess(stuNewMdfFile.Chars(), 0) == 0)
		return E_FAIL;		// database file of that name already exists.
	StrUni stuNewLogFile;
	if (stuDbLogFileExt.Length() > 0)
	{
		stuNewLogFile.Assign(stuNewMdfFile);
		idx = stuNewLogFile.FindStr(L".mdf");
		Assert(idx > 0);
		stuNewLogFile.Replace(idx, stuNewLogFile.Length(), stuDbLogFileExt.Chars());
		if (_waccess(stuNewLogFile.Chars(), 0) == 0)
			return E_FAIL;
	}
	bool fAttachDb = false;
	StrUni stuAttachDbName(bstrOldName);
	StrUni stuAttachMdfFile(sbstrOldMdfFile.Chars());
	StrUni stuAttachLdfFile(stuOldLogFile);
	try
	{
		if (!DetachDatabase(qode, bstrOldName))
			return E_FAIL;
		// We randomly have SQLServer locking access to the database file in the process of
		// attaching and detaching, so we'll try to sleep awhile after the detach to let
		// everything settle down. It never seems to give trouble when single-stepping. Once
		// locked, however, the only way to unlock it seems to be to stop SQL Server.
		Sleep(1500);
		fAttachDb = true;
		if (_wrename(sbstrOldMdfFile.Chars(), stuNewMdfFile.Chars()) == 0)
		{
			if (stuOldLogFile.Length() == 0 ||
				_wrename(stuOldLogFile.Chars(), stuNewLogFile.Chars()) == 0)
			{
				stuAttachDbName.Assign(bstrNewName);
				stuAttachMdfFile.Assign(stuNewMdfFile);
				stuAttachLdfFile.Assign(stuNewLogFile);
			}
			else
			{
				// revert the first rename...
				_wrename(stuNewMdfFile.Chars(), sbstrOldMdfFile.Chars());
			}
		}
	}
	catch(...)
	{
	}
	if (fAttachDb)
	{
		AttachDatabase(qode, stuAttachDbName.Chars(), stuAttachMdfFile.Chars(),
			stuAttachLdfFile.Chars());
	}
	return stuAttachDbName == bstrNewName ? S_OK : E_FAIL;

	END_COM_METHOD(g_fact, IID_IDbAdmin);
}


/*----------------------------------------------------------------------------------------------
	Get the base file used to store the database given by pszDatabase.

	@param pode database connection to the "master" database
	@param bstrDatabase name of the database
	@param pbstrMdfFile pointer to the BSTR that receives the filename
----------------------------------------------------------------------------------------------*/
void DbAdmin::GetMdfFileOfDatabase(IOleDbEncap * pode, const BSTR bstrDatabase,
	BSTR * pbstrMdfFile)
{
	*pbstrMdfFile = NULL;
	StrUni stuDb(bstrDatabase);
	StrUni stuSql;
	stuSql.Format(L"SELECT filename FROM master..sysdatabases WHERE name=?", stuDb.Chars());
	IOleDbCommandPtr qodc;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	OLECHAR rgch[MAX_PATH];
	SmartBstr sbstrMdfFile;
	HRESULT hr;
	IgnoreHr(hr = pode->CreateCommand(&qodc));
	if (SUCCEEDED(hr))
		IgnoreHr(hr = qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)(stuDb.Chars()), stuDb.Length() * sizeof(OLECHAR)));
	if (SUCCEEDED(hr))
		IgnoreHr(hr = qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
	if (SUCCEEDED(hr))
		IgnoreHr(hr = qodc->GetRowset(0));
	if (SUCCEEDED(hr))
		IgnoreHr(hr = qodc->NextRow(&fMoreRows));
	if (SUCCEEDED(hr) && fMoreRows)
	{
		IgnoreHr(hr = qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgch), isizeof(rgch),
			&cbSpaceTaken, &fIsNull, 2));
		if (SUCCEEDED(hr))
		{
			if (!fIsNull)
			{
				if (cbSpaceTaken > isizeof(rgch))
				{
					Vector<OLECHAR> vch;
					vch.Resize(cbSpaceTaken / isizeof(OLECHAR));
					IgnoreHr(hr = qodc->GetColValue(1, reinterpret_cast <BYTE *>(vch.Begin()),
						 vch.Size() * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
					if (SUCCEEDED(hr))
					{
						sbstrMdfFile.Assign(vch.Begin());
					}
					else
					{
						qodc.Clear();
						return;
					}
				}
				else
				{
					sbstrMdfFile.Assign(rgch);
				}
				*pbstrMdfFile = sbstrMdfFile.Detach();
			}
		}
	}
	qodc.Clear();
}

/*----------------------------------------------------------------------------------------------
	Detach the given database.

	@param pode database connection to the "master" database
	@param bstrDatabase name of the database

	@return True if successful, false if the detach fails
----------------------------------------------------------------------------------------------*/
bool DbAdmin::DetachDatabase(IOleDbEncap * pode, const BSTR bstrDatabase)
{
	IOleDbCommandPtr qodc;
	HRESULT hr;
	IgnoreHr(hr = pode->CreateCommand(&qodc));
	if (SUCCEEDED(hr))
	{
		StrUni stuCmd(L"EXEC sp_detach_db ?");
		StrUni stuPath(bstrDatabase);
		IgnoreHr(hr = qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)(stuPath.Chars()), stuPath.Length() * 2));
		if (SUCCEEDED(hr))
			IgnoreHr(hr = qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtStoredProcedure));
		qodc.Clear();
	}
	return SUCCEEDED(hr);
}

/*----------------------------------------------------------------------------------------------
	Attach the given database, using the given files.

	@param pode database connection to the "master" database
	@param pszDatabase name of the database
	@param pszMdfFile name of the primary data file used for the database
	@param pszLdfFile name of the log file used for the database (may be NULL)

	TODO: merge this with the interface method, or fix the interface method to call this?
----------------------------------------------------------------------------------------------*/
void DbAdmin::AttachDatabase(IOleDbEncap * pode, const OLECHAR * pszDatabase,
	const OLECHAR * pszMdfFile, const OLECHAR * pszLdfFile)
{
	IOleDbCommandPtr qodc;
	CheckHr(pode->CreateCommand(&qodc));
	// Make sure db files are not readonly. SQL Server does nasty things when they are.
	if (_waccess(pszMdfFile, 06) == -1)
		_wchmod(pszMdfFile, _S_IREAD|_S_IWRITE);
	StrUni stuDb(pszDatabase);
	StrUni stuMdf(pszMdfFile);
	StrUni stuSql;
	CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
		(ULONG *)(stuDb.Chars()), stuDb.Length() * sizeof(OLECHAR)));
	CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
		(ULONG *)(stuMdf.Chars()), stuMdf.Length() * sizeof(OLECHAR)));
	if (pszLdfFile != NULL && wcslen(pszLdfFile) > 0)
	{
		if (_waccess(pszLdfFile, 06) == -1)
			_wchmod(pszLdfFile, _S_IREAD|_S_IWRITE);
		StrUni stuLdf(pszLdfFile);
		CheckHr(qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)(stuLdf.Chars()), stuLdf.Length() * sizeof(OLECHAR)));
		stuSql.Assign(L"EXEC sp_attach_db @dbname=?, @filename1=?, @filename2=?");
	}
	else
	{
		stuSql.Assign(L"EXEC sp_attach_single_file_db @dbname=?, @physname=?");
	}
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
	qodc.Clear();
}
