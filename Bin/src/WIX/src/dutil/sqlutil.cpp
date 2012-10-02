//-------------------------------------------------------------------------------------------------
// <copyright file="sqlutil.cpp" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
//    SQL helper funtions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// okay, this may look a little weird, but sqlutil.h cannot be in the
// pre-compiled header because we need to #define these things so the
// correct GUID's get pulled into this object file
#include <initguid.h>
#define DBINITCONSTANTS
#include "sqlutil.h"

// private prototypes
static HRESULT FileSpecToString(
	__in SQL_FILESPEC* psf,
	__out LPWSTR* ppwz
	);


/********************************************************************
 SqlConnectDatabase - establishes a connection to a database

 NOTE: wzInstance is optional
	   if fIntegratedAuth is set then wzUser and wzPassword are ignored
********************************************************************/
extern "C" HRESULT DAPI SqlConnectDatabase(
	__in LPCWSTR wzServer,
	__in LPCWSTR wzInstance,
	__in LPCWSTR wzDatabase,
	__in BOOL fIntegratedAuth,
	__in LPCWSTR wzUser,
	__in LPCWSTR wzPassword,
	__out IDBCreateSession** ppidbSession
	)
{
	Assert(wzServer && wzDatabase && *wzDatabase && ppidbSession);

	HRESULT hr = S_OK;
	IDBInitialize* pidbInitialize = NULL;
	IDBProperties* pidbProperties = NULL;

	LPWSTR pwzServerInstance = NULL;
	DBPROP rgdbpInit[4];
	DBPROPSET rgdbpsetInit[1];
	ULONG cProperties = 0;

	memset(rgdbpInit, 0, sizeof(rgdbpInit));
	memset(rgdbpsetInit, 0, sizeof(rgdbpsetInit));

	//obtain access to the SQLOLEDB provider
	hr = ::CoCreateInstance(CLSID_SQLOLEDB, NULL, CLSCTX_INPROC_SERVER,
							IID_IDBInitialize, (LPVOID*)&pidbInitialize);
	ExitOnFailure(hr, "failed to create IID_IDBInitialize object");

	// if there is an instance
	if (wzInstance && *wzInstance)
		hr = StrAllocFormatted(&pwzServerInstance, L"%s\\%s", wzServer, wzInstance);
	else
		hr = StrAllocString(&pwzServerInstance, wzServer, 0);
	ExitOnFailure(hr, "failed to allocate memory for the server instance");

	// server[\instance]
	rgdbpInit[cProperties].dwPropertyID = DBPROP_INIT_DATASOURCE;
	rgdbpInit[cProperties].dwOptions = DBPROPOPTIONS_REQUIRED;
	rgdbpInit[cProperties].colid = DB_NULLID;
	::VariantInit(&rgdbpInit[cProperties].vValue);
	rgdbpInit[cProperties].vValue.vt = VT_BSTR;
	rgdbpInit[cProperties].vValue.bstrVal = ::SysAllocString(pwzServerInstance);
	cProperties++;

	// database
	rgdbpInit[cProperties].dwPropertyID = DBPROP_INIT_CATALOG;
	rgdbpInit[cProperties].dwOptions = DBPROPOPTIONS_REQUIRED;
	rgdbpInit[cProperties].colid = DB_NULLID;
	::VariantInit(&rgdbpInit[cProperties].vValue);
	rgdbpInit[cProperties].vValue.vt = VT_BSTR;
	rgdbpInit[cProperties].vValue.bstrVal= ::SysAllocString(wzDatabase);
	cProperties++;

	if (fIntegratedAuth)
	{
		// username
		rgdbpInit[cProperties].dwPropertyID = DBPROP_AUTH_INTEGRATED;
		rgdbpInit[cProperties].dwOptions = DBPROPOPTIONS_REQUIRED;
		rgdbpInit[cProperties].colid = DB_NULLID;
		::VariantInit(&rgdbpInit[cProperties].vValue);
		rgdbpInit[cProperties].vValue.vt = VT_BSTR;
		rgdbpInit[cProperties].vValue.bstrVal = ::SysAllocString(L"SSPI");   // default windows authentication
		cProperties++;
	}
	else
	{
		// username
		rgdbpInit[cProperties].dwPropertyID = DBPROP_AUTH_USERID;
		rgdbpInit[cProperties].dwOptions = DBPROPOPTIONS_REQUIRED;
		rgdbpInit[cProperties].colid = DB_NULLID;
		::VariantInit(&rgdbpInit[cProperties].vValue);
		rgdbpInit[cProperties].vValue.vt = VT_BSTR;
		rgdbpInit[cProperties].vValue.bstrVal = ::SysAllocString(wzUser);
		cProperties++;

		// password
		rgdbpInit[cProperties].dwPropertyID = DBPROP_AUTH_PASSWORD;
		rgdbpInit[cProperties].dwOptions = DBPROPOPTIONS_REQUIRED;
		rgdbpInit[cProperties].colid = DB_NULLID;
		::VariantInit(&rgdbpInit[cProperties].vValue);
		rgdbpInit[cProperties].vValue.vt = VT_BSTR;
		rgdbpInit[cProperties].vValue.bstrVal = ::SysAllocString(wzPassword);
		cProperties++;
	}

	// put the properties into a set
	rgdbpsetInit[0].guidPropertySet = DBPROPSET_DBINIT;
	rgdbpsetInit[0].rgProperties = rgdbpInit;
	rgdbpsetInit[0].cProperties = cProperties;

	// create and set the property set
	hr = pidbInitialize->QueryInterface(IID_IDBProperties, (LPVOID*)&pidbProperties);
	ExitOnFailure(hr, "failed to get IID_IDBProperties object");
	hr = pidbProperties->SetProperties(1, rgdbpsetInit);
	ExitOnFailure(hr, "failed to set properties");

	//initialize connection to datasource
	hr = pidbInitialize->Initialize();
	ExitOnFailure1(hr, "failed to initialize connection to database: %S", wzDatabase);

	hr = pidbInitialize->QueryInterface(IID_IDBCreateSession, (LPVOID*)ppidbSession);

LExit:
	for (; 0 < cProperties; cProperties--)
		::VariantClear(&rgdbpInit[cProperties - 1].vValue);

	ReleaseObject(pidbProperties);
	ReleaseObject(pidbInitialize);
	ReleaseStr(pwzServerInstance);

	return hr;
}


/********************************************************************
 SqlStartTransaction - Starts a new transaction that must be ended

*********************************************************************/
extern "C" HRESULT DAPI SqlStartTransaction(
	__in IDBCreateSession* pidbSession,
	__out IDBCreateCommand** ppidbCommand,
	__out ITransaction** ppit
	)
{
	Assert(pidbSession && ppit);

	HRESULT hr;

	hr = pidbSession->CreateSession(NULL, IID_IDBCreateCommand, (IUnknown**)ppidbCommand);
	ExitOnFailure(hr, "unable to create command from session");

	hr = (*ppidbCommand)->QueryInterface(IID_ITransactionLocal, (LPVOID*)ppit);
	ExitOnFailure(hr, "Unable to QueryInterface session to get ITransactionLocal");

	hr = ((ITransactionLocal*)*ppit)->StartTransaction(ISOLATIONLEVEL_SERIALIZABLE, 0, NULL, NULL);

LExit:

	return hr;
}

/********************************************************************
 SqlEndTransaction - Ends the transaction

 NOTE: if fCommit, will commit the transaction, otherwise rolls back
*********************************************************************/
extern "C" HRESULT DAPI SqlEndTransaction(
	__in ITransaction* pit,
	__in BOOL fCommit
	)
{
	Assert(pit);

	HRESULT hr;

	if (fCommit)
	{
		hr = pit->Commit(FALSE, XACTTC_SYNC, 0);
		ExitOnFailure(hr, "commit of transaction failed");
	}
	else
	{
		hr = pit->Abort(NULL, FALSE, FALSE);
		ExitOnFailure(hr, "abort of transaction failed");
	}

LExit:

	return hr;
}


/********************************************************************
 SqlDatabaseExists - determines if database exists

 NOTE: wzInstance is optional
	   if fIntegratedAuth is set then wzUser and wzPassword are ignored
	   returns S_OK if database exist
	   returns S_FALSE if database does not exist
	   returns E_* on error
********************************************************************/
extern "C" HRESULT DAPI SqlDatabaseExists(
	__in LPCWSTR wzServer,
	__in LPCWSTR wzInstance,
	__in LPCWSTR wzDatabase,
	__in BOOL fIntegratedAuth,
	__in LPCWSTR wzUser,
	__in LPCWSTR wzPassword,
	__out BSTR* pbstrErrorDescription
	)
{
	Assert(wzServer && wzDatabase && *wzDatabase);

	HRESULT hr = S_OK;
	IDBCreateSession* pidbSession = NULL;

	hr = SqlConnectDatabase(wzServer, wzInstance, L"master", fIntegratedAuth, wzUser, wzPassword, &pidbSession);
	ExitOnFailure1(hr, "failed to connect to 'master' database on server %S", wzServer);

	hr = SqlSessionDatabaseExists(pidbSession, wzDatabase, pbstrErrorDescription);

LExit:
	ReleaseObject(pidbSession);

	return hr;
}


/********************************************************************
 SqlSessionDatabaseExists - determines if database exists

 NOTE: pidbSession must be connected to master database
	   returns S_OK if database exist
	   returns S_FALSE if database does not exist
	   returns E_* on error
********************************************************************/
extern "C" HRESULT DAPI SqlSessionDatabaseExists(
	__in IDBCreateSession* pidbSession,
	__in LPCWSTR wzDatabase,
	__out BSTR* pbstrErrorDescription
	)
{
	Assert(pidbSession && wzDatabase && *wzDatabase);

	HRESULT hr = S_OK;

	LPWSTR pwzQuery = NULL;
	IRowset* pirs = NULL;

	DBCOUNTITEM cRows = 0;
	HROW rghRows[1];
	HROW* prow = rghRows;

	//
	// query to see if the database exists
	//
	hr = StrAllocFormatted(&pwzQuery, L"SELECT name FROM sysdatabases WHERE name='%s'", wzDatabase);
	ExitOnFailure(hr, "failed to allocate query string to ensure database exists");

	hr = SqlSessionExecuteQuery(pidbSession, pwzQuery, &pirs, NULL, pbstrErrorDescription);
	ExitOnFailure(hr, "failed to get database list from 'master' database");
	Assert(pirs);

	//
	// check to see if the database was returned
	//
	hr = pirs->GetNextRows(DB_NULL_HCHAPTER, 0, 1, &cRows, &prow);
	ExitOnFailure(hr, "failed to get row with database name");

	// succeeded but no database
	if ((DB_S_ENDOFROWSET == hr) || (0 == cRows))
		hr = S_FALSE;

LExit:
	ReleaseObject(pirs);
	ReleaseStr(pwzQuery);

	return hr;
}


/********************************************************************
 SqlDatabaseEnsureExists - creates a database if it does not exist

 NOTE: wzInstance is optional
	   if fIntegratedAuth is set then wzUser and wzPassword are ignored
********************************************************************/
extern "C" HRESULT DAPI SqlDatabaseEnsureExists(
	__in LPCWSTR wzServer,
	__in LPCWSTR wzInstance,
	__in LPCWSTR wzDatabase,
	__in BOOL fIntegratedAuth,
	__in LPCWSTR wzUser,
	__in LPCWSTR wzPassword,
	__in SQL_FILESPEC* psfDatabase,
	__in SQL_FILESPEC* psfLog,
	__out BSTR* pbstrErrorDescription
	)
{
	Assert(wzServer && wzDatabase && *wzDatabase);

	HRESULT hr = S_OK;
	IDBCreateSession* pidbSession = NULL;

	//
	// connect to the master database to create the new database
	//
	hr = SqlConnectDatabase(wzServer, wzInstance, L"master", fIntegratedAuth, wzUser, wzPassword, &pidbSession);
	ExitOnFailure1(hr, "failed to connect to 'master' database on server %S", wzServer);

	hr = SqlSessionDatabaseEnsureExists(pidbSession, wzDatabase, psfDatabase, psfLog, pbstrErrorDescription);
	ExitOnFailure1(hr, "failed to create database: %S", wzDatabase);

	Assert(S_OK == hr);
LExit:
	ReleaseObject(pidbSession);

	return hr;
}


/********************************************************************
 SqlSessionDatabaseEnsureExists - creates a database if it does not exist

 NOTE: pidbSession must be connected to the master database
********************************************************************/
extern "C" HRESULT DAPI SqlSessionDatabaseEnsureExists(
	__in IDBCreateSession* pidbSession,
	__in LPCWSTR wzDatabase,
	__in SQL_FILESPEC* psfDatabase,
	__in SQL_FILESPEC* psfLog,
	__out BSTR* pbstrErrorDescription
	)
{
	Assert(pidbSession && wzDatabase && *wzDatabase);

	HRESULT hr = S_OK;

	hr = SqlSessionDatabaseExists(pidbSession, wzDatabase, pbstrErrorDescription);
	ExitOnFailure1(hr, "failed to determine if exists, database: %S", wzDatabase);

	if (S_FALSE == hr)
	{
		hr = SqlSessionCreateDatabase(pidbSession, wzDatabase, psfDatabase, psfLog, pbstrErrorDescription);
		ExitOnFailure1(hr, "failed to create database: %1", wzDatabase);
	}
	// else database already exists, return S_FALSE

	Assert(S_OK == hr);
LExit:

	return hr;
}


/********************************************************************
 SqlCreateDatabase - creates a database on the server

 NOTE: wzInstance is optional
	   if fIntegratedAuth is set then wzUser and wzPassword are ignored
********************************************************************/
extern "C" HRESULT DAPI SqlCreateDatabase(
	__in LPCWSTR wzServer,
	__in LPCWSTR wzInstance,
	__in LPCWSTR wzDatabase,
	__in BOOL fIntegratedAuth,
	__in LPCWSTR wzUser,
	__in LPCWSTR wzPassword,
	__in SQL_FILESPEC* psfDatabase,
	__in SQL_FILESPEC* psfLog,
	__out BSTR* pbstrErrorDescription
	)
{
	Assert(wzServer && wzDatabase && *wzDatabase);

	HRESULT hr = S_OK;
	IDBCreateSession* pidbSession = NULL;

	//
	// connect to the master database to create the new database
	//
	hr = SqlConnectDatabase(wzServer, wzInstance, L"master", fIntegratedAuth, wzUser, wzPassword, &pidbSession);
	ExitOnFailure1(hr, "failed to connect to 'master' database on server %S", wzServer);

	hr = SqlSessionCreateDatabase(pidbSession, wzDatabase, psfDatabase, psfLog, pbstrErrorDescription);
	ExitOnFailure1(hr, "failed to create database: %S", wzDatabase);

	Assert(S_OK == hr);
LExit:
	ReleaseObject(pidbSession);

	return hr;
}


/********************************************************************
 SqlSessionCreateDatabase - creates a database on the server

 NOTE: pidbSession must be connected to the master database
********************************************************************/
extern "C" HRESULT DAPI SqlSessionCreateDatabase(
	__in IDBCreateSession* pidbSession,
	__in LPCWSTR wzDatabase,
	__in SQL_FILESPEC* psfDatabase,
	__in SQL_FILESPEC* psfLog,
	__out BSTR* pbstrErrorDescription
	)
{
	HRESULT hr = S_OK;
	LPWSTR pwzDbFile = NULL;
	LPWSTR pwzLogFile = NULL;
	LPWSTR pwzQuery = NULL;

	if (psfDatabase)
	{
		hr = FileSpecToString(psfDatabase, &pwzDbFile);
		ExitOnFailure(hr, "failed to convert db filespec to string");
	}

	if (psfLog)
	{
		hr = FileSpecToString(psfLog, &pwzLogFile);
		ExitOnFailure(hr, "failed to convert log filespec to string");
	}

	hr = StrAllocFormatted(&pwzQuery, L"CREATE DATABASE %s %s%s %s%s", wzDatabase, pwzDbFile ? L"ON " : L"", pwzDbFile ? pwzDbFile : L"", pwzLogFile ? L"LOG ON " : L"", pwzLogFile ? pwzLogFile : L"");
	ExitOnFailure1(hr, "failed to allocate query to create database: %S", wzDatabase);

	hr = SqlSessionExecuteQuery(pidbSession, pwzQuery, NULL, NULL, pbstrErrorDescription);
	ExitOnFailure1(hr, "failed to create database: %S", wzDatabase);

LExit:
	ReleaseStr(pwzQuery);
	ReleaseStr(pwzLogFile);
	ReleaseStr(pwzDbFile);
	return hr;
}


/********************************************************************
 SqlDropDatabase - removes a database from a server if it exists

 NOTE: wzInstance is optional
	   if fIntegratedAuth is set then wzUser and wzPassword are ignored
********************************************************************/
extern "C" HRESULT DAPI SqlDropDatabase(
	__in LPCWSTR wzServer,
	__in LPCWSTR wzInstance,
	__in LPCWSTR wzDatabase,
	__in BOOL fIntegratedAuth,
	__in LPCWSTR wzUser,
	__in LPCWSTR wzPassword,
	__out BSTR* pbstrErrorDescription
	)
{
	Assert(wzServer && wzDatabase && *wzDatabase);

	HRESULT hr = S_OK;
	IDBCreateSession* pidbSession = NULL;

	//
	// connect to the master database to search for wzDatabase
	//
	hr = SqlConnectDatabase(wzServer, wzInstance, L"master", fIntegratedAuth, wzUser, wzPassword, &pidbSession);
	ExitOnFailure(hr, "Failed to connect to 'master' database");

	hr = SqlSessionDropDatabase(pidbSession, wzDatabase, pbstrErrorDescription);

LExit:
	ReleaseObject(pidbSession);

	return hr;
}


/********************************************************************
 SqlSessionDropDatabase - removes a database from a server if it exists

 NOTE: pidbSession must be connected to the master database
********************************************************************/
extern "C" HRESULT DAPI SqlSessionDropDatabase(
	__in IDBCreateSession* pidbSession,
	__in LPCWSTR wzDatabase,
	__out BSTR* pbstrErrorDescription
	)
{
	Assert(pidbSession && wzDatabase && *wzDatabase);

	HRESULT hr = S_OK;
	LPWSTR pwzQuery = NULL;

	hr = SqlSessionDatabaseExists(pidbSession, wzDatabase, pbstrErrorDescription);
	ExitOnFailure1(hr, "failed to determine if exists, database: %S", wzDatabase);

	if (S_OK == hr)
	{
		hr = StrAllocFormatted(&pwzQuery, L"DROP DATABASE %s", wzDatabase);
		ExitOnFailure1(hr, "failed to allocate query to drop database: %S", wzDatabase);

		hr = SqlSessionExecuteQuery(pidbSession, pwzQuery, NULL, NULL, pbstrErrorDescription);
		ExitOnFailure(hr, "Failed to drop database");
	}

LExit:
	ReleaseStr(pwzQuery);

	return hr;
}


/********************************************************************
 SqlSessionExecuteQuery - executes a query and returns the results if desired

 NOTE: ppirs and pcRoes and pbstrErrorDescription are optional
********************************************************************/
extern "C" HRESULT DAPI SqlSessionExecuteQuery(
	__in IDBCreateSession* pidbSession,
	__in LPCWSTR wzSql,
	__out_opt IRowset** ppirs,
	__out_opt DBROWCOUNT* pcRows,
	__inout BSTR* pbstrErrorDescription
	)
{
	Assert(pidbSession);

	HRESULT hr = S_OK;
	IDBCreateCommand* pidbCommand = NULL;
	ICommandText* picmdText = NULL;
	ICommand* picmd = NULL;
	DBROWCOUNT cRows = 0;

	if (pcRows)
		*pcRows = NULL;

	//
	// create the command
	//
	hr = pidbSession->CreateSession(NULL, IID_IDBCreateCommand, (IUnknown**)&pidbCommand);
	ExitOnFailure(hr, "failed to create database session");
	hr = pidbCommand->CreateCommand(NULL, IID_ICommand, (IUnknown**)&picmd);
	ExitOnFailure(hr, "failed to create command to execute session");

	//
	// set the sql text into the command
	//
	hr = picmd->QueryInterface(IID_ICommandText, (LPVOID*)&picmdText);
	ExitOnFailure(hr, "failed to get command text object for command");
	hr = picmdText->SetCommandText(DBGUID_DEFAULT , wzSql);
	ExitOnFailure1(hr, "failed to set SQL string: %S", wzSql);

	//
	// execute the command
	//
	hr = picmd->Execute(NULL, (ppirs) ? IID_IRowset : IID_NULL, NULL, &cRows, reinterpret_cast<IUnknown**>(ppirs));
	ExitOnFailure1(hr, "failed to execute SQL string: %S", wzSql);

	if (DB_S_ERRORSOCCURRED == hr)
		hr = E_FAIL;

	if (pcRows)
		*pcRows = cRows;

LExit:

	if (FAILED(hr) && picmd && pbstrErrorDescription)
	{
		HRESULT hrGetErrors = SqlGetErrorInfo(picmd, IID_ICommandText, 0x409, NULL, pbstrErrorDescription); // TODO: use current locale instead of always American-English
		if (FAILED(hrGetErrors))
		{
			ReleaseNullBSTR(*pbstrErrorDescription);
		}
	}

	ReleaseObject(picmd);
	ReleaseObject(picmdText);
	ReleaseObject(pidbCommand);

	return hr;
}


/********************************************************************
 SqlCommandExecuteQuery - executes a SQL command and returns the results if desired

 NOTE: ppirs and pcRoes are optional
********************************************************************/
extern "C" HRESULT DAPI SqlCommandExecuteQuery(
	__in IDBCreateCommand* pidbCommand,
	__in LPCWSTR wzSql,
	__out IRowset** ppirs,
	__out DBROWCOUNT* pcRows
	)
{
	Assert(pidbCommand);

	HRESULT hr = S_OK;
	ICommandText* picmdText = NULL;
	ICommand* picmd = NULL;
	DBROWCOUNT cRows = 0;

	if (pcRows)
		*pcRows = NULL;

	//
	// create the command
	//
	hr = pidbCommand->CreateCommand(NULL, IID_ICommand, (IUnknown**)&picmd);
	ExitOnFailure(hr, "failed to create command to execute session");

	//
	// set the sql text into the command
	//
	hr = picmd->QueryInterface(IID_ICommandText, (LPVOID*)&picmdText);
	ExitOnFailure(hr, "failed to get command text object for command");
	hr = picmdText->SetCommandText(DBGUID_DEFAULT , wzSql);
	ExitOnFailure1(hr, "failed to set SQL string: %S", wzSql);

	//
	// execute the command
	//
	hr = picmd->Execute(NULL, (ppirs) ? IID_IRowset : IID_NULL, NULL, &cRows, reinterpret_cast<IUnknown**>(ppirs));
	ExitOnFailure1(hr, "failed to execute SQL string: %S", wzSql);

	if (DB_S_ERRORSOCCURRED == hr)
		hr = E_FAIL;

	if (pcRows)
		*pcRows = cRows;

LExit:
	ReleaseObject(picmd);
	ReleaseObject(picmdText);

	return hr;
}


/********************************************************************
 SqlGetErrorInfo - gets error information from the last SQL function call

 NOTE: pbstrErrorSource and pbstrErrorDescription are optional
********************************************************************/
extern "C" HRESULT DAPI SqlGetErrorInfo(
	__in IUnknown* pObjectWithError,
	__in REFIID IID_InterfaceWithError,
	__in DWORD dwLocaleId,
	__out_opt BSTR* pbstrErrorSource,
	__out BSTR* pbstrErrorDescription
	)
{
	HRESULT hr = S_OK;
	Assert(pObjectWithError);

	// interfaces needed to extract error information out
	ISupportErrorInfo* pISupportErrorInfo = NULL;
	IErrorInfo* pIErrorInfoAll = NULL;
	IErrorRecords* pIErrorRecords = NULL;
	IErrorInfo* pIErrorInfoRecord = NULL;

	// only ask for error information if the interface supports it.
	hr = pObjectWithError->QueryInterface(IID_ISupportErrorInfo,(void**)&pISupportErrorInfo);
	ExitOnFailure(hr, "No error information was found for object.");

	hr = pISupportErrorInfo->InterfaceSupportsErrorInfo(IID_InterfaceWithError);
	ExitOnFailure(hr, "InterfaceWithError is not supported for object with error");

	// ignore the return of GetErrorInfo it can succeed and return a NULL pointer in pIErrorInfoAll anyway
	hr = ::GetErrorInfo(0, &pIErrorInfoAll);
	ExitOnFailure(hr, "failed to get error info");

	if (S_OK == hr && pIErrorInfoAll)
	{
		// see if it's a valid OLE DB IErrorInfo interface that exposes a list of records
		hr = pIErrorInfoAll->QueryInterface(IID_IErrorRecords, (void**)&pIErrorRecords);
		if (SUCCEEDED(hr))
		{
			ULONG cErrors = 0;
			pIErrorRecords->GetRecordCount(&cErrors);

			// get the error information for each record
			for (ULONG i = 0; i < cErrors; i++)
			{
				hr = pIErrorRecords->GetErrorInfo(i, dwLocaleId, &pIErrorInfoRecord);
				if (SUCCEEDED(hr))
				{
					if (pbstrErrorSource)
						pIErrorInfoRecord->GetSource(pbstrErrorSource);
					if (pbstrErrorDescription)
						pIErrorInfoRecord->GetDescription(pbstrErrorDescription);

					ReleaseNullObject(pIErrorInfoRecord);

					break; // TODO: return more than one error in the future!
				}
			}

			ReleaseNullObject(pIErrorRecords);
		}
		else // we have a simple error record
		{
			if (pbstrErrorSource)
				pIErrorInfoAll->GetSource(pbstrErrorSource);
			if (pbstrErrorDescription)
				pIErrorInfoAll->GetDescription(pbstrErrorDescription);
		}
	}
	else
	{
		hr = E_NOMOREITEMS;
	}

LExit:
	ReleaseObject(pIErrorInfoRecord);
	ReleaseObject(pIErrorRecords);
	ReleaseObject(pIErrorInfoAll);
	ReleaseObject(pISupportErrorInfo);

	return hr;
}


//
// private
//

/********************************************************************
 FileSpecToString

*********************************************************************/
static HRESULT FileSpecToString(
	__in SQL_FILESPEC* psf,
	__out LPWSTR* ppwz
	)
{
	Assert(psf && ppwz);

	HRESULT hr = S_OK;
	LPWSTR pwz = NULL;

	hr = StrAllocString(&pwz, L"(", 1024);
	ExitOnFailure(hr, "failed to allocate string for database file info");

	ExitOnNull(*psf->wzName, hr, E_INVALIDARG, "logical name not specified in database file info");
	ExitOnNull(*psf->wzFilename, hr, E_INVALIDARG, "filename not specified in database file info");

	hr = StrAllocFormatted(&pwz, L"%sNAME=%s", pwz, psf->wzName);
	ExitOnFailure1(hr, "failed to format database file info name: %S", psf->wzName);

	hr = StrAllocFormatted(&pwz, L"%s, FILENAME='%s'", pwz, psf->wzFilename);
	ExitOnFailure1(hr, "failed to format database file info filename: %S", psf->wzFilename);

	if (0 != psf->wzSize[0])
	{
		hr = StrAllocFormatted(&pwz, L"%s, SIZE=%s", pwz, psf->wzSize);
		ExitOnFailure1(hr, "failed to format database file info size: %s", psf->wzSize);
	}

	if (0 != psf->wzMaxSize[0])
	{
		hr = StrAllocFormatted(&pwz, L"%s, MAXSIZE=%s", pwz, psf->wzMaxSize);
		ExitOnFailure1(hr, "failed to format database file info maxsize: %s", psf->wzMaxSize);
	}

	if (0 != psf->wzGrow[0])
	{
		hr = StrAllocFormatted(&pwz, L"%s, FILEGROWTH=%s", pwz, psf->wzGrow);
		ExitOnFailure1(hr, "failed to format database file info growth: %s", psf->wzGrow);
	}

	hr = StrAllocFormatted(&pwz, L"%s)", pwz);
	ExitOnFailure(hr, "failed to allocate string for file spec");

	*ppwz = pwz;
	pwz = NULL;  // null here so it doesn't get freed below

LExit:
	ReleaseStr(pwz);
	return hr;
}
