/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, SIL International. All rights reserved.

File: OleDbEncap.cpp
Responsibility: Paul Panek
Last reviewed: Not yet.

Description:
	Implementation of database access interfaces.

	This file contains class definitions for the following classes:
		OleDbEncap
		OleDbCommand
		FwMetaDataCache
-------------------------------------------------------------------------------*//*:End Ignore*/
#define DBINITCONSTANTS // needed before including main in exactly one cpp file for sqloledb.h.
#include "main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

HINSTANCE hInst;								// current instance
INT_PTR CALLBACK	DummyDialog(HWND, UINT, WPARAM, LPARAM);

#undef LOG_EVERY_COMMAND
//#define LOG_EVERY_COMMAND 1
#undef DEBUG_ODE_REF_COUNTS
//#define DEBUG_ODE_REF_COUNTS
#undef DEBUG_ODC_REF_COUNTS
//#define DEBUG_ODC_REF_COUNTS

#define kcptVirtual 32 // May be added to any of the above for virtual properties (but only internally)

void CleanUpErrorInfo()
{
	IErrorInfo * pIErrorInfo = NULL;
	GetErrorInfo(0, &pIErrorInfo);
	if(pIErrorInfo){
		pIErrorInfo->Release();
	}
}

//:>********************************************************************************************
//:>	OleDbEncap::RemoteConnectionMonitor Methods
//:>********************************************************************************************

const achar * OleDbEncap::RemoteConnectionMonitor::m_pszMutexName =
	_T("SIL FieldWorks Remote Connection Monitor Mutex");
const achar * OleDbEncap::RemoteConnectionMonitor::m_pszFileMappingName =
	_T("SIL FieldWorks Remote Connection Monitor File Mapping");

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
OleDbEncap::RemoteConnectionMonitor::RemoteConnectionMonitor()
{
	m_hFileMapping = NULL;
	m_hMutex = CreateMutex(NULL, false, m_pszMutexName);
	Assert(m_hMutex);
	m_fRemote = false;
	m_fConnectionNoted = false;
}

/*----------------------------------------------------------------------------------------------
	Desstructor.
----------------------------------------------------------------------------------------------*/
OleDbEncap::RemoteConnectionMonitor::~RemoteConnectionMonitor()
{
	Assert(!m_hFileMapping); // This should have been reset already.
	CloseHandle(m_hMutex);
}

/*----------------------------------------------------------------------------------------------
	Check if the new connection server is remote, and if so, allow remote warnings to be
	received.
	@param bstrServer Server to connect to, including a remote machine name, if appropriate.
	@return true if all went OK.
----------------------------------------------------------------------------------------------*/
bool OleDbEncap::RemoteConnectionMonitor::NewConnection(BSTR bstrServer)
{
	if (m_fConnectionNoted)
		TerminatingConnection();

	m_fConnectionNoted = false;
	m_fRemote = false;

	// Check if the given server is on the local computer:
	StrUni stuLocalServer;
	achar psz[MAX_COMPUTERNAME_LENGTH + 1];
	ulong cch = isizeof(psz);
	::GetComputerName(psz, &cch);
	StrUni stuMachine(psz);
	stuLocalServer.Format(L"%s\\SILFW", stuMachine.Chars());
	StrUni stuDotLocalServer(L".\\SILFW"); // LT-9098
	if (!stuLocalServer.Equals(bstrServer) && !stuDotLocalServer.Equals(bstrServer))
	{
		// This connection is to a remote computer:
		m_fRemote = true;

		// We do not yet own the mutex, so wait for it:
		Assert(m_hMutex);
		if (!m_hMutex)
			return false;
		DWORD dwWaitResult = WaitForSingleObject(m_hMutex, INFINITE);
		if (dwWaitResult != WAIT_OBJECT_0)
			return false;

		// Get a handle to our shared memory, containing the count of remote connections:
		m_hFileMapping = ::CreateFileMapping((HANDLE)0xFFFFFFFF, NULL, PAGE_READWRITE, 0,
			isizeof(int), m_pszFileMappingName);
		bool fMemPreExisted = (GetLastError() == ERROR_ALREADY_EXISTS);
		Assert(m_hFileMapping);
		if (!m_hFileMapping)
		{
			ReleaseMutex(m_hMutex);
			return false;
		}

		// Get a pointer to the shared memory block:
		int * pnRemoteConnections = (int *)MapViewOfFile(m_hFileMapping,
			FILE_MAP_ALL_ACCESS, 0, 0, 0);
		AssertPtr(pnRemoteConnections);
		if (pnRemoteConnections)
		{
			if (fMemPreExisted)
			{
				// There is already at least one remote conneciton, so just increment tally:
				*pnRemoteConnections += 1;
			}
			else
			{
				// This is the only remote connection at the moment:
				*pnRemoteConnections = 1;
				// We must configure permission for remote warnings to reach us:
				PermitRemoteWarnings();
			}
			::UnmapViewOfFile(pnRemoteConnections);
		}
		else // Failed to get pointer to shared memory:
		{
			ReleaseMutex(m_hMutex);
			return false;
		}
		ReleaseMutex(m_hMutex);
	}
	m_fConnectionNoted = true;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Check if permission for remote warnings needs to be revoked.
----------------------------------------------------------------------------------------------*/
bool OleDbEncap::RemoteConnectionMonitor::TerminatingConnection()
{
	if (!m_fConnectionNoted)
		return true;

	m_fConnectionNoted = false;

	// Use of shared memory only occurs when connecting to a remote server:
	if (m_fRemote)
	{
		Assert(m_hFileMapping);
		if (m_hFileMapping)
		{
			// Get ownership of the mutex:
			Assert(m_hMutex);
			DWORD dwWaitResult = WaitForSingleObject(m_hMutex, INFINITE);
			if (dwWaitResult != WAIT_OBJECT_0)
			{
				CloseHandle(m_hFileMapping);
				m_hFileMapping = NULL;
				return false;
			}

			// Get a pointer to the shared memory block:
			int * pnRemoteConnections = (int *)MapViewOfFile(m_hFileMapping,
				FILE_MAP_ALL_ACCESS, 0, 0, 0);
			AssertPtr(pnRemoteConnections);
			if (pnRemoteConnections)
			{
				// Decrement count of remote connections:
				*pnRemoteConnections -= 1;
				// If there are no remote connections left, we must refuse remote warnings:
				if (*pnRemoteConnections == 0)
					RefuseRemoteWarnings();
				::UnmapViewOfFile(pnRemoteConnections);
			}
			CloseHandle(m_hFileMapping);
			m_hFileMapping = NULL;
			ReleaseMutex(m_hMutex);
		}
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Sets up access permissions for remote users to produce warning dialogs on our computer.
	This is done by writing various data into the registry, specifically the AppID section for
	CLSID_FwRemote.
----------------------------------------------------------------------------------------------*/
void OleDbEncap::RemoteConnectionMonitor::PermitRemoteWarnings()
{
	IDbWarnSetupPtr qzdbws;
	qzdbws.CreateInstance(CLSID_FwRemote);
	qzdbws->PermitRemoteWarnings();
}

/*----------------------------------------------------------------------------------------------
	This removes the setting from the registry that would have allowed remote users to produce
	warning dialogs on our computer.
----------------------------------------------------------------------------------------------*/
void OleDbEncap::RemoteConnectionMonitor::RefuseRemoteWarnings()
{
	IDbWarnSetupPtr qzdbws;
	qzdbws.CreateInstance(CLSID_FwRemote);
	qzdbws->RefuseRemoteWarnings();
}



//:>********************************************************************************************
//:>	OleDbEncap - Constructor/Destructor.
//:>********************************************************************************************
OleDbEncap::OleDbEncap()
{
	m_cref = 1;
	m_fTransactionOpen = false;
	m_fInitialized = false;
	ModuleEntry::ModuleAddRef();
#ifdef DEBUG_ODE_REF_COUNTS
	StrApp str;
	StrApp strFmt = "OleDbEncap Constructor: %d, cref=%d\n";
	str.Format(strFmt, (int) this, m_cref);
	OutputDebugString(str.Chars());
#endif DEBUG_ODE_REF_COUNTS
}


OleDbEncap::~OleDbEncap()
{
#ifdef DEBUG_ODE_REF_COUNTS
	StrApp str;
	StrApp strFmt = "OleDbEncap destructor: %d\n";
	str.Format(strFmt, (int) this);
	OutputDebugString(str.Chars());
#endif DEBUG_ODE_REF_COUNTS
	m_rmcm.TerminatingConnection();
	m_qunkSession.Clear();	// Make sure these are released in the proper order.
	if (m_qdbi)
	{
		HRESULT hr;
		IgnoreHr(hr = m_qdbi->Uninitialize());
		if (FAILED(hr))
		{
#ifdef DEBUG_DBACCESS
			StrAnsiBuf stab;
			stab.Format("m_qdbi->Uninitialize() => %s (db = \"%S\")",
				AsciiHresult(hr), m_sbstrDatabase.Chars());
			::MessageBox(NULL, stab.Chars(),
				"DEBUG OleDbEncap::~OleDbEncap() [contact Steve McConnel]", MB_OK);
#endif
		}
		m_qdbi.Clear();
	}

	ModuleEntry::ModuleRelease();
}



//:>********************************************************************************************
//:>    Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.DbAccess.OleDbEncap"),
	&CLSID_OleDbEncap,
	_T("SIL database access"),
	_T("Apartment"),
	&OleDbEncap::CreateCom);


void OleDbEncap::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
	{
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	}
	ComSmartPtr<OleDbEncap> qzode;
	qzode.Attach(NewObj OleDbEncap());		// ref count initially 1
	CheckHr(qzode->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	IOleDbEncap - IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP OleDbEncap::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IOleDbEncap *>(this));
	else if (iid == IID_IOleDbEncap)
		*ppv = static_cast<IOleDbEncap *>(this);
	else if (iid == IID_IUndoGrouper)
		*ppv = static_cast<IUndoGrouper *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(
			static_cast<IUnknown *>(static_cast<IOleDbEncap *>(this)), IID_IOleDbEncap,
			IID_IUndoGrouper);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


STDMETHODIMP_(ULONG) OleDbEncap::AddRef(void)
{
#ifdef DEBUG_ODE_REF_COUNTS
	StrApp str;
	StrApp strFmt = "OleDbEncap AddRef: %d, cref=%d\n";
	for (int i = m_cref; --i >= 0; )
		str.Append("    ");
	str.FormatAppend(strFmt, (int) this, m_cref + 1);
	OutputDebugString(str.Chars());
#endif DEBUG_ODE_REF_COUNTS

	Assert(m_cref > 0);
	::InterlockedIncrement(&m_cref);
	return m_cref;
}


STDMETHODIMP_(ULONG) OleDbEncap::Release(void)
{
	Assert(m_cref > 0);
	ulong cref = ::InterlockedDecrement(&m_cref);
#ifdef DEBUG_ODE_REF_COUNTS
	StrApp str;
	StrApp strFmt = "OleDbEncap Release: %d, cref=%d\n";
	for (int i = m_cref; --i >= 0; )
		str.Append("    ");
	str.FormatAppend(strFmt, (int) this, m_cref);
	OutputDebugString(str.Chars());
#endif DEBUG_ODE_REF_COUNTS
	if (!cref)
	{
		m_cref = 1;
		delete this;
	}
	return cref;
}


//:>********************************************************************************************
//:>	IOleDbEncap Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${IOleDbEncap#BeginTrans}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::BeginTrans()
{
	BEGIN_COM_METHOD;

	if (!m_fInitialized)
		return E_UNEXPECTED;

	Assert(m_fTransactionOpen == false);
	if (m_fTransactionOpen)
		return E_UNEXPECTED;

	m_fTransactionOpen = true;
#ifdef JT_7_25_01_UseTransactionObject
	CheckHr(m_qtranloc->StartTransaction(ISOLATIONLEVEL_READCOMMITTED, 0, NULL, NULL));

	// It appears that the above doesn't really start a transaction. (This can be observed
	// with SQL Profiler.) Instead it does the equivalent of SET IMPLICIT_TRANSACTIONS ON.
	// Most subsequent SQL commands will then start a transaction, but NOT "save trans",
	// which is very often the next thing we do. Instead "save trans" produces an exception.
	// The solution is to do a trivial Select here to force the transaction to really start.
	// Note: I (JT) don't know why PaulP chose to use the code above rather than executing
	// "Begin tran" directly as an SQL statement, so I'm not changing it.
	StrUni stuCmd;
	stuCmd.Format(L"SELECT @@TRANCOUNT");
	// ENHANCE (JohnT): Doing the ExecCommand below causes errors to show up in SQL Profiler.
	// They don't appear to cause a problem for our program, though, and I don't have
	// time to research it more now. (Similar things are showing up for quite a lot of
	// our queries.)
	// They show up as complaints about trying to get a server cursor when not possible
	// (error 16937). I (JT) think this is because we are trying to get a rowset back
	// from a piece of SQL that just produces a number. We may need another option for
	// ExecCommand, ksSqlStmtWithOneCount or similar. (Using knSqlStmtNoResults does not
	// work--it appears that SQLServer figures out that no one wants the result and
	// doesn't execute the SQL, and therefore STILL doesn't really start the transaction.)
	// SteveMi found that using Begin Tran, as in the following line, prevented the
	// SQL Profiler errors; however, when we do that Undo goes into a closed loop that we
	// can't break out of for no obvious reason. Also we are really starting the trans
	// twice, which would be a problem in a system that supported nested ones.
	//stuCmd.Format(L"BEGIN TRAN");
	IOleDbCommandPtr qodc;
	CreateCommand(&qodc);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
#else
	// All the above still doesn't work reliably. Try just making a transaction in SQL.
	StrUni stuCmd;
	stuCmd.Format(L"BEGIN TRAN");
	IOleDbCommandPtr qodc;
	CreateCommand(&qodc);
	// If you find that this BEGIN TRAN command logs in with a new connection,
	// executes the command, and logs out of the connection it usually indicates we haven't
	// properly released a qodc pointer. The problem can be seen with SQL Server Profiler.
	// When the connection is broken, the BEGIN TRAN is lost. (The profiler indicates this
	// with Audit Login and Audit Logout. Then when the COMMIT executes later, an error
	// appears that says it doesn't have a matching BEGIN TRAN. This will happen if a nested
	// qodc is created without clearing the previous qodc.
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
#endif

	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbEncap#CreateCommand}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::CreateCommand(IOleDbCommand ** ppodc)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppodc);

	if (!m_fInitialized)
		return E_UNEXPECTED;

	//static int cc = 0;
	IOleDbCommandPtr qodc;
	OleDbCommand * podc = NewObj OleDbCommand;
	qodc.Attach(podc);
	if (!qodc)
		ThrowOutOfMemory();
	CheckHr(qodc->Init(reinterpret_cast<IUnknown*>(this), m_qfistLog));
	podc->InitTimeoutMode(m_olt);
	*ppodc = qodc.Detach();
	//cc++;

	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbEncap#CommitTrans}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::CommitTrans()
{
	BEGIN_COM_METHOD;

	if (!m_fInitialized)
		return E_UNEXPECTED;

	Assert(m_fTransactionOpen == true);
	if (!m_fTransactionOpen)
		return E_UNEXPECTED;

	// Commit the current transaction.  The first argument in the "->Commit" call (i.e.
	// "fretaining" which is set to FALSE) indicates that the session reverts to auto-commit
	// mode.  This means that all SQL statements issued outside of a transaction will be
	// applied immediately to the database. In order to start another transaction, you must
	// call the BeginTrans method.
	m_fTransactionOpen = false;
#ifdef JT_7_25_01_UseTransactionObject
	CheckHr(m_qtranloc->Commit(FALSE, XACTTC_SYNC, 0));
#else
	StrUni stuCmd;
	stuCmd.Format(L"COMMIT");
	IOleDbCommandPtr qodc;
	CreateCommand(&qodc);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
	qodc.Clear();
#endif

	// ENHANCE PaulP: The following should NOT be necessary, but for some reason, the
	// the ITransactionlocal "Commit" method prevents any further SavePoints from being set
	// which, of course, is unacceptable. (JohnT: therefore it might be a useful optimization
	// to remove it on subsequent versions of SQLServer and see if the problem is fixed.)
	//CheckHr(Init(m_sbstrServer, m_sbstrDatabase, m_qfistLog, m_olt, m_nmsTimeout));

	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbEncap#Init}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::Init(BSTR bstrServer, BSTR bstrDatabase, IStream * pfistLog,
							  OdeLockTimeoutMode olt, int nmsTimeout)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrServer);
	ChkComBstrArgN(bstrDatabase);
	ChkComArgPtrN(pfistLog);

	HRESULT hr;
	IDataInitializePtr qdai;
	IDBInitializePtr qdbi;
	IDBCreateCommandPtr qdcc;
	IDBCreateSessionPtr qdcs;
	IUnknownPtr qunkSession;
	StrUni stu;

	m_fInitialized = false;

	// Store the IStream pointer for logging.
	m_qfistLog = pfistLog;

	// Store the lock timeout mode and length (in milliseconds).
	m_olt = olt;
	m_nmsTimeout = nmsTimeout;

	// If SQL isn't running, try to get it started.
	if (!StartMSDE())
		ThrowHr(WarnHr(E_FAIL));

	// Get an interface pointer to the data access initializer.
	hr = CoCreateInstance(CLSID_MSDAINITIALIZE, NULL, CLSCTX_INPROC_SERVER, IID_IDataInitialize,
		(void **)&qdai);
	if (FAILED(hr))
		ThrowHr(WarnHr(hr), StrUni(kstiddbeDAI));

	// Guard against the assignment of smartBstr to itself.
#ifdef DEBUG_DBACCESS
	StrUniBuf stubOldDatabase(m_sbstrDatabase.Chars());
#endif
	if (m_sbstrServer != (LPCOLESTR)bstrServer)
		m_sbstrServer = bstrServer;
	if (m_sbstrDatabase != (LPCOLESTR)bstrDatabase)
		m_sbstrDatabase = bstrDatabase;

	// Default connect timeout (I think) is 15 seconds. We had around 10 connection failures
	// in the last year, especially when opening a database for the first time after install.
	// E.g., LT-7767, LTB-440, LTB-269, LT-6758, LTB-421, TE-6239, LT-6556, etc.
	// Let's try doubling this time to 30 seconds to see if that solves the problems.
	stu.Format(L"Provider=SQLNCLI; Server=%b; Database=%b; "
		L"Uid=FWDeveloper; Pwd=careful; Connect Timeout=30; ", bstrServer, bstrDatabase);

	// Connect to the selected database using a connection string.
	IgnoreHr(hr = qdai->GetDataSource(NULL, CLSCTX_INPROC_SERVER,
		stu.Chars(), IID_IDBInitialize, reinterpret_cast<IUnknown **>(&qdbi)));
	if (FAILED(hr))
		ThrowHr(WarnHr(hr), StrUni(kstiddbeDatSrc));

	IgnoreHr(hr = qdbi->Initialize());
	if (FAILED(hr))
	{
		// The timeout change above still didn't help in the one test case we could reliably reproduce (TE-6239).
		// The connection failure seems to be very temperamental (mainly showing up on Vista).
		// Simply calling Uninitialize made it work in the TE-6239 case. However, it didn't in TE-8269, but
		// a message box still worked.
		IgnoreHr(hr = qdbi->Uninitialize());
		IgnoreHr(hr = qdbi->Initialize());
		if (FAILED(hr))
		{
			// Waiting with long Sleep, and pumping through all messages didn't unblock Initialize in TE-8269.
			// However, bringing up a MessageBox always seems to clear it. But that causes problems
			// with some code because it intentionally wants this method to fail without a dialog
			// to indicate a database is not present (e.g., DbAdmin tests, DbServices tests, backup/restore).
			// To get around this problem we use this kludgy dialog that is not visible and immediately
			// closes, but in the process it unblocks the Initialize. We don't know why, but it does.
			// Since it immediately closes, it won't break functions that want it to fail.
			DialogBox(ModuleEntry::GetModuleHandle(), MAKEINTRESOURCE(IDD_DBACCESSDUMMYDIALOG),
				GetForegroundWindow(),DummyDialog);

			IgnoreHr(hr = qdbi->Initialize());
			if (FAILED(hr))
			{
				// If none of the above works, resort to a green screen crash report.
				//ThrowHr(WarnHr(hr), StrUni(kstiddbeInit));

				// We are getting all sorts of errors saying the connection failed.
				// See, for example, TE-7874, LT-9062, etc. We would like to get some more
				// exact info about why it is failing.
				switch (hr)
				{
				// The method has initiated asynchronous initialization of the data source object.
				case DB_S_ASYNCHRONOUS:
					// We don't appear to be doing any asynchronus processing in the system.
					// Show a green screen error if we really are.
					ThrowHr(WarnHr(hr), StrUni(kstiddbedb_s_asynchronous));
					break;
				// The data source object or enumerator was initialized, but one or
				// more properties were not set.
				case DB_S_ERRORSOCCURRED:
					// I saw one bit of VB code which skipped DB_S_ERRORSOCCURRED. I
					// don't know if this is harmless or not. In any case, it appears
					// that the provider uses DBPROPSET_PROPERTIESINERROR to return
					// properties in error. If we run into this error, maybe more
					// can be done with DBPROPSET_PROPERTIESINERROR.
					ThrowHr(WarnHr(hr), StrUni(kstiddbedb_s_errorsoccurred));
					break;
				// A provider-specific error occurred.
				case E_FAIL:
					// It would be nice to know how to return the error provided by the
					// SQL Native OLE DB Provider.
					ThrowHr(WarnHr(hr), StrUni(kstiddbee_fail));
					break;
				// The provider was unable to allocate sufficient memory to initialize
				// the data source object or enumerator.
				case E_OUTOFMEMORY:
					ThrowHr(WarnHr(hr), StrUni(kstiddbee_outofmemory));
					break;
				// The data source object is in the process of being initialized
				// asynchronously.
				case E_UNEXPECTED:
					ThrowHr(WarnHr(hr), StrUni(kstiddbee_unexpected));
					break;
				// IDBInitialize::Initialize had already been called for the data
				// source object or enumerator, and an intervening call to
				// IDBInitialize::Uninitialize had not been made.
				case DB_E_ALREADYINITIALIZED:
					ThrowHr(WarnHr(hr), StrUni(kstiddbedb_e_alreadyinitialized));
					break;
				// The provider prompted for additional information and the user selected Cancel.
				case DB_E_CANCELED:
					ThrowHr(WarnHr(hr), StrUni(kstiddbedb_e_canceled));
					break;
				// The data source object or enumerator was initialized, but one or more
				// properties were not set.
				case DB_E_ERRORSOCCURRED:
					ThrowHr(WarnHr(hr), StrUni(kstiddbedb_e_errorsoccurred));
					break;
				// Authentication of the consumer to the data source object or enumerator
				// failed. The data source object or enumerator remains in the uninitialized state.
				case DB_SEC_E_AUTH_FAILED:
					ThrowHr(WarnHr(hr), StrUni(kstiddbedb_sec_e_auth_failed));
					break;
				default:
					ThrowHr(WarnHr(hr), StrUni(kstiddbeInit));
				}
			 }
		}
	}

	// Create a session object from a data source object.
	hr = qdbi->QueryInterface(IID_IDBCreateSession, (void **)&qdcs);
	if (SUCCEEDED(hr))
		IgnoreHr(hr = qdcs->CreateSession(NULL, IID_IUnknown, &qunkSession));
	if (FAILED(hr))
		ThrowHr(WarnHr(hr), StrUni(kstiddbeCreateSession));

	// Obtain and save Transaction interface pointer.
	hr = qunkSession->QueryInterface(IID_IDBCreateCommand, (void **)&qdcc);
#ifdef JT_7_25_01_UseTransactionObject
	// JT: trying to see if it works better to issue SQL "begin trans" rather than
	// using the OLEDB object.
	if (SUCCEEDED(hr))
		hr = qdcc->QueryInterface(IID_ITransactionLocal, (void **)&m_qtranloc);
#endif
	if (FAILED(hr))
		ThrowHr(WarnHr(hr), StrUni(kstiddbeITransact));

	if (m_qunkSession)
	{
		m_qunkSession.Clear();
	}
	// Keep a pointer to the data source and session.
	m_qunkSession = qunkSession;

	if (m_qdbi)
	{
		IgnoreHr(hr = m_qdbi->Uninitialize());
		if (FAILED(hr))
		{
#ifdef DEBUG_DBACCESS
			StrAnsiBuf stab;
			StrAnsiBuf stabTitle;
			stab.Format("m_qdbi->Uninitialize() => %s (old db = \"%S\")",
				AsciiHresult(hr), stubOldDatabase.Chars());
			stabTitle.Format("DEBUG OleDbEncap::Init(\"%B\", \"%B\") [contact Steve McConnel]",
				bstrServer, bstrDatabase);
			::MessageBox(NULL, stab.Chars(), stabTitle.Chars(), MB_OK);
#endif
		}
	}
	m_qdbi = qdbi;

	// Initialize the database savepoint level to zero.
	m_nSavePointLevel = 0;

	m_fInitialized = true;

	// koltNone for lock timeout mode is a bit of a misnomer for SQL Server, which always has
	// some sort of LOCK_TIMEOUT setting. If you use koltNone, the prior LOCK_TIMEOUT setting
	// will be used.
	//
	// If you use something other than koltNone, you will need to send timeout value. The
	// default for SQL Server is -1, "wait forever". A value of 0 means to not wait at all
	// and return as soon a lock is encountered. A value of 20000 means to wait for the lock
	// for 20 seconds. WARNING: If the lock timeout is exceeded, SQL Server will return
	// error 1222, and this needs to be trapped. Otherwise, you'll get a crash.
	//
	// If you don't want a lock timeout, you need to tell SQL Server to wait indefinitely.
	// Instead of using koltNone for the lock timeout mode, use koltMsgBox. This ensures that
	// you get the timeout value that you want. Then set the timeout value to koltvForever.

	// REVIEW (SteveMiller): This may well need refactoring when we get to the point of
	// swapping out data stores.

	if (m_olt != koltNone)
	{
		StrUni stuCmd;
		stuCmd.Format(L"SET LOCK_TIMEOUT %d ", m_nmsTimeout);	// The number represents milli-seconds.
		IOleDbCommandPtr qodc;
		CreateCommand(&qodc);
		hr = qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults);	// Pointless to detect failure.
	}

	m_rmcm.NewConnection(m_sbstrServer);

	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}

HRESULT OleDbEncap::Reinit()
{
	BEGIN_COM_METHOD;

	CheckHr(Init(m_sbstrServer, m_sbstrDatabase, m_qfistLog, m_olt, m_nmsTimeout));

	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}

HRESULT OleDbEncap::GetSession(IUnknown **ppunkSession)
{
	BEGIN_COM_METHOD;

	if (!m_fInitialized)
		return E_UNEXPECTED;

	*ppunkSession = m_qunkSession;
	AddRefObj(*ppunkSession);

	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}

/*----------------------------------------------------------------------------------------------
	${IOleDbEncap#IsTransactionOpen}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::IsTransactionOpen(ComBool * pfTransactionOpen)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfTransactionOpen);

	*pfTransactionOpen = m_fTransactionOpen;

	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbEncap#RollbackTrans}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::RollbackTrans()
{
	BEGIN_COM_METHOD;

	if (!m_fInitialized)
		return E_UNEXPECTED;

	Assert(m_fTransactionOpen == true);
	if (!m_fTransactionOpen)
		return E_UNEXPECTED;

	// Rollback the transaction.
	m_fTransactionOpen = false;
#ifdef JT_7_25_01_UseTransactionObject
	CheckHr(m_qtranloc->Abort(NULL, FALSE, FALSE));
#else
	StrUni stuCmd;
	stuCmd.Format(L"ROLLBACK");
	IOleDbCommandPtr qodc;
	CreateCommand(&qodc);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
#endif

	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbEncap#RollbackSavePoint}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::RollbackSavePoint(BSTR bstrSavePoint)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrSavePoint);

	if (!m_fInitialized)
		return E_UNEXPECTED;

	Assert(m_fTransactionOpen == true);
	if (!m_fTransactionOpen)
		return E_UNEXPECTED;
	Assert(m_nSavePointLevel > 0);
	if (m_nSavePointLevel <= 0)
		return E_UNEXPECTED;

	IOleDbCommandPtr qodc;
	StrUni stuCmd;
	SmartBstr sbstr;
	sbstr = bstrSavePoint;
	stuCmd.Format(L"rollback tran %s", sbstr.Chars());
	CreateCommand(&qodc);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtStoredProcedure));

	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbEncap#SetSavePoint}
	Set the next save point in the transaction and return its name, SPn, where n is a counter
	that increments with each call until a new transaction is begun.

	For the first save point
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::SetSavePoint(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (!m_fInitialized)
		return E_UNEXPECTED;

	Assert(m_fTransactionOpen == true);
	if (!m_fTransactionOpen)
		return E_UNEXPECTED;
	Assert(m_nSavePointLevel >= 0);
	if (m_nSavePointLevel < 0)
		return E_UNEXPECTED;

	IOleDbCommandPtr qodc;
	CreateCommand(&qodc);
	StrUni stuCmd;

	// The apparently pointless "SELECT @@TRANCOUNT" at the beginning of the command appears to
	// be necessary for the "save tran xxx" to reliably recognize that a transaction is already
	// active.  Without it, the following error is frequently caught by FullErrorCheck():
	//
	// HRESULT: 80004005, Minor Code: 628, Source: Microsoft OLE DB Provider for SQL Server,
	//	Description: Cannot issue SAVE TRANSACTION when there is no active transaction.
	// It can also catch an E_FAIL.
	stuCmd.Format(L"SELECT @@TRANCOUNT\n save tran %s%d", kwchSavePointName, m_nSavePointLevel);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtStoredProcedure));

	StrUni stuTemp;
	stuTemp.Format(L"%s%d", kwchSavePointName, m_nSavePointLevel);
	stuTemp.GetBstr(pbstr);
	m_nSavePointLevel++;

	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbEncap#SetSavePointOrBeginTrans}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::SetSavePointOrBeginTrans(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (!m_fInitialized)
		return E_UNEXPECTED;

	if (m_fTransactionOpen == false)
		CheckHr(BeginTrans());
	CheckHr(SetSavePoint(pbstr));

	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}


/*----------------------------------------------------------------------------------------------
	Throws an exception if the HRESULT is a failure code, otherwise hr is returned.
	@param hr HRESULT value from which an exception is thrown.
----------------------------------------------------------------------------------------------*/
inline HRESULT OleDbEncap::XCheckHr(HRESULT hr)
{
	// TODO JohnT(PaulP):  May want to have fancier exception thrown here.
	if (FAILED(hr))
	{
		throw __LINE__;
	}
	return hr;
}


/*----------------------------------------------------------------------------------------------
	Sets hr to E_OUTOFMEMORY and raises an exception if the pointer to the memory block is NULL.
	@param hr HRESULT value from which an exception is thrown.
	@param pv Pointer which would have pointed to a block of memory if we had enough memory to
	allocate.
----------------------------------------------------------------------------------------------*/
inline HRESULT OleDbEncap::XCheckMemory(HRESULT hr, void * pv)
{
	if (!pv)
	{
		hr = E_OUTOFMEMORY;
		XCheckHr(hr);
	}
	return hr;
}


/*----------------------------------------------------------------------------------------------
	Creates an error object and then sets a description which is the formatted string derived
	from the inputs.
	@param rid Resource id of a resource string which is a format control, and two further
	integers for which the defaults are NULL.
----------------------------------------------------------------------------------------------*/
HRESULT OleDbEncap::SetUpErrorInfo(int rid)
{
	ICreateErrorInfoPtr qcei;
	IErrorInfo * pei;
	HRESULT hr;

	// Create error info object.
	hr = CreateErrorInfo(&qcei);
	if (FAILED(hr))
		return hr;

	// Set the textual description of the error.
	StrUni stu(rid);
	hr = qcei->SetDescription((wchar *)stu.Chars());
	if (FAILED(hr))
		return hr;

	// Now get the IErrorInfo interface of the error object and set it for the current thread.
	hr = qcei->QueryInterface(IID_IErrorInfo, (void **)&pei);
	if (FAILED(hr))
		return hr;
	SetErrorInfo(0, pei);
	pei->Release();

	return hr;
}


/*----------------------------------------------------------------------------------------------
	This method is a way to call InitMSDE from the command line using the following command:
	The second line will force an initialization regardless of the registry flag.
		rundll32 DbAccess.dll,ExtInitMSDE
		rundll32 DbAccess.dll,ExtInitMSDE force
----------------------------------------------------------------------------------------------*/
void CALLBACK ExtInitMSDE(HWND hwnd, HINSTANCE hinst, LPSTR lpCmdLine, int nCmdShow)
{
	// Setup our environment to be "Error free" by ignoring any error messages that happen to be
	// hanging around
	CleanUpErrorInfo();

	// Try one-time initialization on MSDE.
	bool fForce;
	// The command line is always char.
	fForce = !_stricmp(lpCmdLine, "force");
	IStreamPtr qfistLog;
	StrUni stuLog;
	try
	{
		CheckHr(::CoInitialize(NULL));

		IOleDbEncapPtr qode;
		qode.CreateInstance(CLSID_OleDbEncap);

		// Create a log file in %TEMP%\ExtInitMSDE.log where we can see what
		//  went wrong if InitMSDE fails.
		OLECHAR rgchPath[MAX_PATH];
		int cenv = ::GetEnvironmentVariable(L"TEMP", rgchPath, MAX_PATH);
		stuLog.Assign(rgchPath, cenv);
		if (stuLog[cenv - 1] != '\\')
			stuLog.Append(L"\\");
		stuLog.Append(L"ExtInitMSDE.log");
		FileStream::Create(stuLog.Chars(), STGM_READWRITE, &qfistLog);
		StrAnsi sta("Start error logging in ExtInitMSDE.\r\n");
		ULONG cch;
		qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch);

		CheckHr(qode->InitMSDE(qfistLog, fForce));
		qfistLog.Clear();
		qode.Clear();
	}
	catch (Throwable & thr)
	{
		qfistLog.Clear();
		// If an error block has been set then display any message within it.
		StrUni stuErr;
		stuErr.Format(L"See %s for explanation of error.\r\n", stuLog.Chars());
		stuErr.Append(thr.Message());
		::MessageBox(NULL, stuErr.Chars(), NULL, MB_ICONERROR);
	}
	CoUninitialize();
}

	// Need to quote single quotes since it will be in a quoted string in the query.
	void QuoteQuotes(StrUni & stu)
	{
		int istr = stu.Length();
		do
		{
			istr = stu.ReverseFindCh(L'\'', istr);
			if (istr >= 0)
			{
				stu.Replace(istr, istr, L"'");
				--istr;
			}
		} while (istr >= 0);
	}

/*----------------------------------------------------------------------------------------------
	${IOleDbEncap#InitMSDE}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::InitMSDE(IStream * pfistLog, ComBool fForce)
{
	BEGIN_COM_METHOD
	ChkComArgPtrN(pfistLog);

	if (!m_qfistLog)
		m_qfistLog = pfistLog;

	// Note: We can't use normal OleDbEncap methods here because it uses the FWDeveloper
	// login which may not be present at this stage. Thus, we use raw OleDb commands.

	StrAppBufPath strRootDir;
	bool fInitMsdeFlagRead = false;
	HRESULT hr = S_OK;
	DWORD dwInitMsde = 1;
	HKEY hk;

	// Check to see if we need to initialize MSDE.
	if (!InitMSDE_GetRegKey(hk, fInitMsdeFlagRead, dwInitMsde) || (!dwInitMsde && !fForce))
	{
		// MSDE already initialized.
		InitMSDE_Finish(fInitMsdeFlagRead, hr, dwInitMsde, fForce, hk);
		return hr;
	}

	// If SQL isn't running, try to get it started.
	if (!StartMSDE())
	{
		SetUpErrorInfo(kstidSQLNotStarted);
		hr = E_FAIL;
		InitMSDE_Finish(fInitMsdeFlagRead, hr, dwInitMsde, fForce, hk);
		return hr;
	}

	IDBCreateCommandPtr qdcc;
	ICommandTextPtr qcdt;

	if (InitMSDE_CreateSession(hr, qdcc, qcdt) && SUCCEEDED(hr))
	{
		hr = InitMSDE_SetupMiscDBStuff(qcdt);
		if (SUCCEEDED(hr))
		{
			// Get all the names of databases found in RootDataDir.
			Vector<StrApp> vstr = InitMSDE_FindDBs(hk, strRootDir);

			// Get list of attached database names.
			Vector<StrApp> vstrAttached;
			hr = InitMSDE_GetAttachedDBs(qdcc, vstr, vstrAttached);

			if (SUCCEEDED(hr))
			{
				hr = InitMSDE_DetachDBs(qdcc, vstr, vstrAttached);
				if (SUCCEEDED(hr))
				{
					// Attach all the .mdf files found in RootDataDir.
					hr = InitMSDE_AttachDBFiles(qdcc, vstr, strRootDir);
				}
			}
		}
	}

	InitMSDE_Finish(fInitMsdeFlagRead, hr, dwInitMsde, fForce, hk);
	return hr;

	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}

/*----------------------------------------------------------------------------------------------
	Get the registry key. Note: we open it in read-only mode here. If we have to
	write we re-open it. This improves things for limited users.
----------------------------------------------------------------------------------------------*/
bool OleDbEncap::InitMSDE_GetRegKey(HKEY & hk, bool & fInitMsdeFlagRead, DWORD &dwInitMsde)
{
	fInitMsdeFlagRead = false;

	long lRet = ::RegCreateKeyEx(HKEY_LOCAL_MACHINE, _T("Software\\SIL\\FieldWorks"), 0,
		_T(""), REG_OPTION_NON_VOLATILE, KEY_QUERY_VALUE, NULL, &hk, NULL);
	if (lRet == ERROR_SUCCESS)
	{
		fInitMsdeFlagRead = true;
		DWORD cb = isizeof(dwInitMsde);
		DWORD dwT;
		long lT;
		// Get the flag from the registry in dwInitMsde.
		lT = ::RegQueryValueEx(hk, _T("InitMSDE"), NULL, &dwT, (BYTE *)&dwInitMsde, &cb);
		if (lT == ERROR_SUCCESS)
			Assert(dwT == REG_DWORD);
		else if (lT != ERROR_FILE_NOT_FOUND) // We'll create one if needed.
		{
			LogError(_T("--Error retrieving InitMSDE value from registry.\r\n"));
			return false;
		}
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Create an MSDE session by connecting to the master database. If the password has not been
	set, set it to the default.
----------------------------------------------------------------------------------------------*/
bool OleDbEncap::InitMSDE_CreateSession(HRESULT & hr, IDBCreateCommandPtr & qdcc,
										   ICommandTextPtr & qcdt)
{
	IDataInitializePtr qdai;
	IDBInitializePtr qdbi;
	IDBCreateSessionPtr qdcs;
	achar psz[MAX_COMPUTERNAME_LENGTH + 1];
	ulong cch = isizeof(psz);
	StrUni stuSvr;
	StrUni stu;
	bool fPasswordOK = true;
	hr = S_OK;

	// Get an interface pointer to the data access initializer.
	try
	{
		CheckHr(hr = CoCreateInstance(CLSID_MSDAINITIALIZE, NULL,
			CLSCTX_INPROC_SERVER, IID_IDataInitialize, (void **)&qdai));
	}
	catch(Throwable &thr)
	{
		thr;
		SetUpErrorInfo(kstiddbeDAI);
		LogError(hr, "InitMSDE_CreateSession.CoCreateInstance", NULL);
		return false;
	}

	// Try to open a connection on master using the standard FieldWorks sa password.
	::GetComputerName(psz, &cch);
	stuSvr = psz;
	stuSvr.Append(L"\\SILFW");
	stu.Format(L"Provider=SQLNCLI; "
		L"Server=%s; Database=master; Uid=sa; Pwd=inscrutable; ", stuSvr.Chars());

	// Connect to the selected database using a connection string.
	try
	{
		CheckHr(qdai->GetDataSource(NULL, CLSCTX_INPROC_SERVER, stu.Chars(),
			IID_IDBInitialize, reinterpret_cast<IUnknown **>(&qdbi)));
		CheckHr(hr = qdbi->Initialize());
	}
	catch(Throwable &thr)
	{
		thr;
		// Try to open a connection on the master table using a blank sa password.
		// This will succeed on newly installed MSDE, but fail otherwise.
		fPasswordOK = false;
		stu.Format(L"Provider=SQLNCLI; Server=%s; "
			L"Database=master; Uid=sa;", stuSvr.Chars());

		// Connect to the selected database using a connection string.
		try
		{
			CheckHr(hr = qdai->GetDataSource(NULL, CLSCTX_INPROC_SERVER,	stu.Chars(),
				IID_IDBInitialize, reinterpret_cast<IUnknown **>(&qdbi)));
			CheckHr(hr = qdbi->Initialize());
		}
		catch(Throwable &thr0)
		{
			thr0;
			SetUpErrorInfo(kstiddbeInit);
			LogError(hr, "InitMSDE_CreateSession.GetDataSource or Initialize", stu);
			return false;
		}
	}

	// Create a session object from a data source object.
	try
	{
		CheckHr(hr = qdbi->QueryInterface(IID_IDBCreateSession, (void **)&qdcs));
		CheckHr(hr = qdcs->CreateSession(NULL, IID_IDBCreateCommand, (IUnknown**)&qdcc));
	}
	catch(Throwable &thr)
	{
		thr;
		SetUpErrorInfo(kstiddbeCreateSession);
		LogError(hr, "InitMSDE_CreateSession.QueryInterface or CreateSession", NULL);
		return false;
	}

	CheckHr(hr = qdcc->CreateCommand(NULL, IID_ICommandText, (IUnknown**)&qcdt));

	if (!fPasswordOK)
	{
		// We need to change the sa password from the original blank.
		StrUni stu;
		stu.Assign(L"exec sp_password @new = 'inscrutable'");
		hr = InitMSDE_Execute(qcdt, stu);
		if (FAILED(hr))
		{
			LogError(hr, "InitMSDE_CreateSession.Execute", stu);
			return false;
		}
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Executes a bunch of query commands on various SQL server system databases.
----------------------------------------------------------------------------------------------*/
HRESULT OleDbEncap::InitMSDE_SetupMiscDBStuff(ICommandTextPtr qcdt)
{
	// If sp_GetFWDBs is already present, remove it.
	HRESULT	hr = InitMSDE_Execute(qcdt,
		L"if exists (select * from sysobjects where id = object_id('master.dbo.sp_GetFWDBs'))"
		L"begin "
			L"drop proc dbo.sp_GetFWDBs "
		L"end");
	if (FAILED(hr))
		return hr;

	// Store the sp_GetFWDBs stored procedure.
	hr = InitMSDE_Execute(qcdt, GetCreateGetFWDBsSQLString());
	if (FAILED(hr))
		return hr;

	// Grant access to the stored procedure to all users.
	// This must be run separately from the previous query.
	hr = InitMSDE_Execute(qcdt, L"grant execute on sp_GetFWDBs to public");
	if (FAILED(hr))
		return hr;

	// If sp_DbStartup is already present, remove it.
	hr = InitMSDE_Execute(qcdt,
		L"if exists (select * from sysobjects where id = object_id('master.dbo.sp_DbStartup'))"
		L"begin "
			L"drop proc dbo.sp_DbStartup "
		L"end");
	if (FAILED(hr))
		return hr;

	// Store the sp_DbStartup stored procedure.
	hr = InitMSDE_Execute(qcdt, GetCreateDbStartupSQLString());
	if (FAILED(hr))
		return hr;

	// Make sp_DbStartup execute whenever SQL starts up.
	hr = InitMSDE_Execute(qcdt, L"exec sp_procoption 'sp_DbStartup', 'startup', 'true'");
	if (FAILED(hr))
		return hr;

	// Make sure advanced settings are set on, should we need them at some point.
	hr = InitMSDE_Execute(qcdt, L"EXEC sp_configure 'show advanced options', 1 "
		L"RECONFIGURE WITH OVERRIDE");
	if (FAILED(hr))
		return hr;

	// Add the FWDevelopwer user.
	// If it already exists, we do nothing. It would be difficult to remove it first and
	// then add it because we first have to clear all of the users on attached databases
	// that might be using FWDeveloper.
	// Note: When using sp_addlogin Vista may not allow careful as a password because of
	// security settings. sp_addlogin is deprecated and Create Login is the new method which
	// allows us to disable the password check.
	hr = InitMSDE_Execute(qcdt,
		L"if (select count(name) from master.dbo.syslogins where name = 'FWDeveloper') = 0 "
		L"begin "
			L"CREATE LOGIN FWDeveloper WITH PASSWORD = 'careful', CHECK_POLICY=off "
		L"end ");
	if (FAILED(hr))
		return hr;

	// Add sysadmin role to FWDeveloper.
	// Note: We tried using dbcreator and bulkadmin roles and not sysadmin, and then creating
	// an FwDeveloper user in each database, but after spending days at trying to get things
	// to work reliably when attaching and restoring files that were detached or backed up on
	// a different server, we simply ran into too many problems, so resorted to sysadmin.
	hr = InitMSDE_Execute(qcdt,  L"exec sp_addsrvrolemember 'FwDeveloper', 'sysadmin'");
	return hr;
}

/*----------------------------------------------------------------------------------------------
	Execute the specified SQL command.
----------------------------------------------------------------------------------------------*/
HRESULT OleDbEncap::InitMSDE_Execute(ICommandTextPtr qcdt, StrUni stuSQL)
{
	HRESULT hr = S_OK;

	IgnoreHr(hr = qcdt->SetCommandText(DBGUID_SQL, stuSQL.Bstr()));
	if (SUCCEEDED(hr))
	{
		IgnoreHr(hr = qcdt->Execute(NULL, IID_NULL, NULL, NULL, NULL));
		if(SUCCEEDED(hr))
			CleanUpErrorInfo();
		else
			LogError(hr, "InitMSDE_Execute.Execute", stuSQL);
	}
	else
	{
		SetUpErrorInfo(kstiddbeSetup);
		LogError(hr, "InitMSDE_Execute.SetCommandText", stuSQL);
	}

	return hr;
}

/*----------------------------------------------------------------------------------------------
	Gets a list of all the database files (i.e. .mdf) found in RootDataDir. The list of
	database names does not include the path or the ".mdf". In other words, the name as
	it is recognized by SQL Server.
----------------------------------------------------------------------------------------------*/
Vector<StrApp> OleDbEncap::InitMSDE_FindDBs(HKEY hk, StrAppBufPath & strRootDir)
{
	achar rgch[MAX_PATH];
	DWORD cb = isizeof(rgch);
	DWORD dwT;
	Vector<StrApp> vstr;

	if (::RegQueryValueEx(hk, _T("RootDataDir"), NULL, &dwT, (BYTE *)rgch, &cb) == ERROR_SUCCESS)
	{
		Assert(dwT == REG_SZ);
		strRootDir.Assign(rgch);
	}

	StrAppBufPath strMask;
	strMask.Assign(strRootDir);
	strMask.Append(strMask[strMask.Length() - 1] != '\\' ? _T("\\") : _T(""));
	strMask.Append("Data\\*.mdf");

	WIN32_FIND_DATA wfd;
	HANDLE hFind = ::FindFirstFile(strMask.Chars(), &wfd);
	if (hFind != INVALID_HANDLE_VALUE)
	{
		do
		{
			StrApp str(wfd.cFileName);
			str.Replace(str.Length() - 4, str.Length(), ""); // Remove .mdf.
			vstr.Push(str);
		} while (::FindNextFile(hFind, &wfd));
		::FindClose(hFind);
	}

	return vstr;
}

/*----------------------------------------------------------------------------------------------
	Goes through the list of all the database files (i.e. .mdf files) found in the RootDataDir
	and builds a list of those files that are attached. When a database file is found not to
	be attached, then an empty string will be added to the list for that databases file.
----------------------------------------------------------------------------------------------*/
HRESULT OleDbEncap::InitMSDE_GetAttachedDBs(IDBCreateCommandPtr qdcc, Vector<StrApp> vstr,
										   Vector<StrApp> & vstrAttached)
{
	HRESULT hr = S_OK;
	ICommandTextPtr qcdt;
	IAccessorPtr qacc;
	IRowsetPtr qrws;
	HACCESSOR hacc = NULL;
	struct
	{
		ULONG status1;
		wchar psz[MAX_PATH];
	} row;

	for (int istr = 0; istr < vstr.Size(); ++istr)
	{
		// Need a new command for each loop.
		IgnoreHr(hr = qdcc->CreateCommand(NULL, IID_ICommandText, (IUnknown**)&qcdt));
		if (FAILED(hr))
		{
			SetUpErrorInfo(kstiddbeSetup);
			LogError(hr, "InitMSDE_GetAttachedDBs.CreateCommand", NULL);
			return hr;
		}

		StrUni stuName(vstr[istr]);
		// See if this file is currently attached.
		StrUni stu;
		QuoteQuotes(stuName);
		stu.Format(L"select filename from master..sysdatabases where name = N'%s'",
			stuName.Chars());
		IgnoreHr(hr = qcdt->SetCommandText(DBGUID_SQL, stu.Bstr()));
		if (FAILED(hr))
		{
			SetUpErrorInfo(kstiddbeSetup);
			LogError(hr, "InitMSDE_GetAttachedDBs.SetCommandText", stu);
			return hr;
		}

		qcdt->QueryInterface(IID_IAccessor, (void **)&qacc);

		// Create the accessor.
		const int kcbs = 1;
		DBBINDING rgdbnd[kcbs] = { 0 }; // Array of binding structures.
		rgdbnd[0].iOrdinal = 1;
		rgdbnd[0].obStatus = 0;
		rgdbnd[0].obValue = rgdbnd[0].obStatus + sizeof(ULONG);
		rgdbnd[0].dwMemOwner = DBMEMOWNER_CLIENTOWNED;
		rgdbnd[0].dwPart = DBPART_STATUS | DBPART_VALUE;
		rgdbnd[0].wType = DBTYPE_WSTR;
		rgdbnd[0].cbMaxLen = (MAX_PATH + 1) * sizeof(WCHAR);
		// Released by end of this method.
		IgnoreHr(hr = qacc->CreateAccessor(DBACCESSOR_ROWDATA, kcbs, rgdbnd, 0, &hacc, NULL));
		if (FAILED(hr))
		{
			qacc->ReleaseAccessor(hacc, NULL);
			SetUpErrorInfo(kstiddbeSetup);
			LogError(hr, "InitMSDE_GetAttachedDBs.CreateAccessor", NULL);
			return hr;
		}

		// Execute the command.
		IgnoreHr(hr = qcdt->Execute(NULL, IID_IRowset, NULL, NULL, (IUnknown **)&qrws));
		if (FAILED(hr))
		{
			qacc->ReleaseAccessor(hacc, NULL);
			SetUpErrorInfo(kstiddbeSetup);
			LogError(hr, "InitMSDE_GetAttachedDBs.Execute", stu);
			return hr;
		}

		CleanUpErrorInfo();

		// Fetch the row.
		ulong crowFetched;
		HROW * prghrow = NewObj HROW[1];
		StrApp str;
		IgnoreHr(hr = qrws->GetNextRows(DB_NULL_HCHAPTER, 0, 1, &crowFetched, &prghrow));
		if (FAILED(hr))
			LogError(hr, "InitMSDE_GetAttachedDBs.GetNextRows", NULL);
		else
		{
			if (crowFetched != 1)
				str.Assign("");
			else
			{
				// Get the filename for the attached file.
				IgnoreHr(hr = qrws->GetData(prghrow[0], hacc, (void *)&row));
				str.Assign(row.psz);
				str.Replace(str.Length() - 4, str.Length(), ""); // Remove .mdf
			}
			vstrAttached.Push(str);
			qrws->ReleaseRows(crowFetched, prghrow, NULL, NULL, NULL);
		}

		qacc->ReleaseAccessor(hacc, NULL);
		hacc = NULL;
		if (prghrow)
			delete prghrow;

		if (FAILED(hr))
		{
			SetUpErrorInfo(kstiddbeSetup);
			LogError(hr, "InitMSDE_GetAttachedDBs.End", NULL);
			return hr;
		}
	}

	Assert(vstr.Size() == vstrAttached.Size());
	return hr;
}

/*----------------------------------------------------------------------------------------------
	Detach all the databases for which there is a corresponding database file in RootDataDir.
----------------------------------------------------------------------------------------------*/
HRESULT OleDbEncap::InitMSDE_DetachDBs(IDBCreateCommandPtr qdcc, Vector<StrApp> vstr,
										   Vector<StrApp> vstrAttached)
{
	HRESULT hr;
	ICommandTextPtr qcdt;

	CheckHr(hr = qdcc->CreateCommand(NULL, IID_ICommandText, (IUnknown**)&qcdt));
	for (int i = vstrAttached.Size(); --i >= 0; )
	{
		// Empty file names mean there was an .mdf file
		// found in RootDataDir but it isn't attached.
		if (vstrAttached[i].Length() == 0)
			continue;

		// The name of the database (not the .mdf file).
		StrUni stuT(vstr[i]);
		QuoteQuotes(stuT);

		// Detach the database.
		StrUni stu;
		stu.Format(L"exec sp_detach_db N'%s'", stuT.Chars());
		IgnoreHr(hr = qcdt->SetCommandText(DBGUID_SQL, stu.Bstr()));
		if (FAILED(hr))
		{
			SetUpErrorInfo(kstidDetachFailure);
			LogError(hr, "InitMSDE_DetachDBs.SetCommandText", stu);
		}
		else
		{
			IgnoreHr(hr = qcdt->Execute(NULL, IID_NULL, NULL, NULL, NULL));
			if(SUCCEEDED(hr))
				CleanUpErrorInfo();
			else
				LogError(hr, "InitMSDE_DetachDBs.Execute", stu);
		}
	}

	return hr;
}

/*----------------------------------------------------------------------------------------------
	Go through the list of database files found in the RootDataDir and attach them.
----------------------------------------------------------------------------------------------*/
HRESULT OleDbEncap::InitMSDE_AttachDBFiles(IDBCreateCommandPtr qdcc,
										   Vector<StrApp> vstr, StrAppBufPath strRootDir)
{
	HRESULT hr = S_OK;
	ICommandTextPtr qcdt;

	HRESULT hrRet = S_OK;
	for (int i = vstr.Size(); --i >= 0; )
	{
		IgnoreHr(hr = qdcc->CreateCommand(NULL, IID_ICommandText, (IUnknown**)&qcdt));
		if (FAILED(hr))
		{
			SetUpErrorInfo(kstiddbeSetup);
			LogError(hr, "InitMSDE_AttachDBFiles.CreateCommand", NULL);
			return hr;
		}

		StrApp strMdf;
		StrApp strLdf;
		strMdf.Format(_T("%s\\Data\\%s.mdf"), strRootDir.Chars(), vstr[i].Chars());
		strLdf.Format(_T("%s\\Data\\%s_log.ldf"), strRootDir.Chars(), vstr[i].Chars());
		StrUni stu;

		StrUni stuMdf(strMdf);
		StrUni stuLdf(strLdf);
		StrUni stuName(vstr[i]);
		// Check whether the log file exists, and build the attach command accordingly.
		WIN32_FIND_DATA wfd;
		HANDLE hFind = ::FindFirstFileW(stuLdf.Chars(), &wfd);
		QuoteQuotes(stuMdf);
		QuoteQuotes(stuName);
		QuoteQuotes(stuLdf);
		if (hFind != INVALID_HANDLE_VALUE)
		{
			::FindClose(hFind);
			stu.Format(L"exec sp_attach_db @dbname=N'%s', @filename1=N'%s', @filename2=N'%s'",
				stuName.Chars(), stuMdf.Chars(), stuLdf.Chars());
		}
		else
		{
			stu.Format(L"exec sp_attach_single_file_db @dbname=N'%s', @physname=N'%s'",
				stuName.Chars(), stuMdf.Chars());
		}

		hr = qcdt->SetCommandText(DBGUID_SQL, stu.Bstr());
		IgnoreHr(hr);
		if (FAILED(hr))
		{
			hrRet = hr;
			LogError(hr, "InitMSDE_AttachDBFiles.SetCommandText", stu);
		}
		else
		{
			IgnoreHr(hr = qcdt->Execute(NULL, IID_NULL, NULL, NULL, NULL));
			if (SUCCEEDED(hr))
				CleanUpErrorInfo();
			else
			{
				LogError(hr, "InitMSDE_AttachDBFiles.Execute", stu);
				hrRet = hr;
			}
		}
	}

	return hrRet;
}

/*----------------------------------------------------------------------------------------------
	Do final tasks (clear the InitMSDE flag in the registry, etc.).
----------------------------------------------------------------------------------------------*/
void OleDbEncap::InitMSDE_Finish(bool fInitMsdeFlagRead, HRESULT hr,
								 DWORD dwInitMsde, ComBool fForce, HKEY hk)
{
	if (fInitMsdeFlagRead)
	{
		// If we completed MSDE initialization, clear the flag in the registry.
		if (SUCCEEDED(hr) && (dwInitMsde || fForce))
		{
			dwInitMsde = 0;

			// Reopen the registry key with write access
			RegCloseKey(hk);
			long lRet = ::RegCreateKeyEx(HKEY_LOCAL_MACHINE,
				_T("Software\\SIL\\FieldWorks"), 0, _T(""), REG_OPTION_NON_VOLATILE,
				KEY_QUERY_VALUE | KEY_SET_VALUE, NULL, &hk, NULL);

			if (lRet != ERROR_SUCCESS)
				LogError("\r\n--Error creating InitMSDE registry key.\r\n");
			else
			{
				lRet = ::RegSetValueEx(hk, _T("InitMSDE"), NULL, REG_DWORD,
					(BYTE *)&dwInitMsde, isizeof(DWORD));

				Assert(lRet == ERROR_SUCCESS);
			}
		}

		RegCloseKey(hk);
	}

	if (FAILED(hr))
		ThrowHr(WarnHr(hr));
}

/*----------------------------------------------------------------------------------------------
	Log error, yet another version.
----------------------------------------------------------------------------------------------*/
void OleDbEncap::LogError(HRESULT hr, StrAnsi methodAndCmd, StrAnsi info)
{
	StrAnsi sta;
	StrAnsi staFmt;
	staFmt.Assign("%n-- ERROR!%nWhere:   %s%nHRESULT: %x%n");

	if (!info.Length())
		sta.Format(staFmt.Chars(), methodAndCmd.Chars(), hr);
	else
	{
		staFmt.Append("Command: %s%n");
		sta.Format(staFmt.Chars(), methodAndCmd.Chars(), hr, info.Chars());
	}

	LogError(sta, NULL, NULL);
}

/*----------------------------------------------------------------------------------------------
	Log specified error.
----------------------------------------------------------------------------------------------*/
void OleDbEncap::LogError(StrAnsi msg)
{
	LogError(msg, NULL, NULL);
}

/*----------------------------------------------------------------------------------------------
	Log specified error with specified HRESULT.
----------------------------------------------------------------------------------------------*/
void OleDbEncap::LogError(StrAnsi msg, HRESULT hr, StrAnsi param)
{
	if (!m_qfistLog)
		return;

	ULONG cch;
	StrAnsi sta;

	if (!hr)
		sta.Assign(msg);
	else if (!param)
		sta.Format(msg.Chars(), hr);
	else
		sta.Format(msg.Chars(), hr, param.Chars());

	CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
}

/*----------------------------------------------------------------------------------------------
	Build the SQL command to build the sp_GetFWDBs stored procedure.
	@return a StrUni containing the SQL string to create the sp_GetFWDBs stored procedure.
----------------------------------------------------------------------------------------------*/
StrUni OleDbEncap::GetCreateGetFWDBsSQLString()
{
	// Note (SteveMiller): The first time this gets executed (in SQL Server), Profiler
	// shows an error 208, which is an Invalid Object. sp_GetFWDBs still executes
	// correctly. (The second time it's run, it also runs correctly, with no error
	// message.) According to the doc, the owner's name needs to be included. For
	// sp_GetFWDBs, the owner is dbo. In tests with SSMSE, including the owner dbo
	// does not make the error go away. The only conclusion I can come to is that
	// that the error is bogus, or at least that the object is not known when
	// SQL Server first encounters it.

	return
		L"create procedure sp_GetFWDBs\n"
		L"as\n"
		L"	declare @sDynSql nvarchar(4000), @nCurDBId int, @fIsNocountOn int\n"
		L"	declare @dbid int\n"
		L"	declare @Err int\n"
		L"	set @fIsNocountOn = @@options & 512\n"
		L"	if @fIsNocountOn = 0 set nocount on\n"
		L"	-- create a temporary table to hold all FieldWorks databases\n"
		L"	create table #dblist ( sDBName sysname )\n"
		L"	set @Err = @@error\n"
		L"	if @Err <> 0 goto LCleanUp2\n"
		L"     \n"
		L"     --( SQL Express takes much longer to open databases than MSDE. Since we're on a\n"
		L"     --( local FW instance, we're going to assume all the databases within the instance\n"
		L"     --( are FW databases.\n"
		L"--	-- get all of the databases associated with this server except the system catalog databases\n"
		L"--	declare cur_DBs cursor local fast_forward for\n"
		L"--	select [dbid]\n"
		L"--	from	master..sysdatabases\n"
		L"--	where has_dbaccess([name]) = 1\n"
		L"--		and [name] not in ('master', 'model', 'tempdb', 'msdb', 'Northwind', 'pubs')\n"
		L"--	-- process each database determining whether or not it's a FieldWorks database\n"
		L"--	open cur_DBs\n"
		L"--	set @Err = @@error\n"
		L"--	if @Err <> 0 goto LCleanUp\n"
		L"--	fetch cur_DBs into @dbid\n"
		L"--	while @@fetch_status = 0 begin\n"
		L"--		set @sDynSql = N'if object_id(N''[' + db_name(@dbid) + N']..Class$'') is not null ' + \n"
		L"--				N'and object_id(N''[' + db_name(@dbid) + N']..Field$'') is not null ' +\n"
		L"--				N'and object_id(N''[' + db_name(@dbid) + N']..ClassPar$'') is not null ' +\n"
		L"--				N'insert into #dblist (sDBName) values (' + \n"
		L"--				N'N''' + db_name(@dbid) + N''')'\n"
		L"--		exec ( @sDynSql )\n"
		L"--		set @Err = @@error\n"
		L"--		if @Err <> 0 begin\n"
		L"--			raiserror('Unable to execute dynamic SQL', 16, 1)\n"
		L"--			goto LCleanUp\n"
		L"--		end\n"
		L"--		fetch cur_DBs into @dbid\n"
		L"--	end\n"
		L"--	select [sDBName] from #dblist \n"
		L"-- close cur_DBs\n"
		L"--  deallocate cur_DBs\n"
		L"	\n"
		L"	insert into #dblist\n"
		L"	select [name]\n"
		L"	from master..sysdatabases\n"
		L"	where upper([name]) not in ('MASTER', 'MODEL', 'TEMPDB', 'MSDB', 'NORTHWIND', 'PUBS', "
		L"          'ETHNOLOGUE', 'REPORTSERVER$SILFW', 'REPORTSERVER$SILFWTEMPDB')\n"
		L"	\n"
		L"	select [sDBName] from #dblist \n"
		L"LCleanUp:\n"
		L"	drop table #dblist\n"
		L"LCleanUp2:\n"
		L"	-- if nocount was turned on turn it off\n"
		L"	if @fIsNocountOn = 0 set nocount off\n"
		L"	return @Err";
}

/*----------------------------------------------------------------------------------------------
	Build the SQL command to build the sp_DbStartup stored procedure.
	@return a StrUni containing the SQL string to create the sp_DbStartup stored procedure.
----------------------------------------------------------------------------------------------*/
StrUni OleDbEncap::GetCreateDbStartupSQLString()
{
	return
		L"CREATE PROCEDURE sp_DbStartup\n"
		L"AS\n"
		L"	--( Make sure advanced settings are on.\n"
		L"	EXEC sp_configure 'show advanced options', 1\n"
		L"	RECONFIGURE WITH OVERRIDE\n"
		L"	--( Suppress SQL messages for performance.\n"
		L"	SET NOCOUNT ON\n"
		// There is a bug in the original version of SQL Server 2000.
		// See: http://support.microsoft.com/default.aspx?scid=kb;EN-US;q286286
		//L"	--( Set the Lock Timeout to 5 seconds.\n"
		//L"	SET LOCK_TIMEOUT 5000\n"
		L"	--( Pre cache some stored procs for performance.\n"
		L"	SET NOEXEC ON\n"
		L"	EXEC master..sp_GetFWDBs\n"
		// These were in, but since these aren't in master, it probably didn't do anything.
		//L"	EXEC GetLinkedObjects$\n"
		//L"	EXEC DeleteObjects$\n"
		//L"	EXEC GetPossibilities\n"
		L"	SET NOEXEC OFF\n"
		L"	--( If a database file isn't there, drop the database.\n"
		L"	DECLARE @fileexists INT\n"
		L"	DECLARE @sql NVARCHAR(1000)\n"
		L"	DECLARE @name SYSNAME\n"
		L"	DECLARE @filename NVARCHAR(260)\n"
		L"	DECLARE curDbs CURSOR LOCAL FAST_FORWARD FOR\n"
		L"		SELECT name, filename FROM master..sysdatabases\n"
		L"	OPEN curDbs\n"
		L"	FETCH NEXT FROM curDbs INTO @name, @filename\n"
		L"	WHILE @@FETCH_STATUS = 0 BEGIN\n"
		L"		EXEC master.dbo.xp_fileexist @filename, @fileexists OUTPUT\n"
		L"		IF @fileexists = 0 BEGIN\n"
		L"			SET @sql = 'DROP DATABASE [' + @name + ']'\n"
		L"			EXEC (@sql)\n"
		L"		END\n"
		L"		FETCH NEXT FROM curDbs INTO @name, @filename\n"
		L"	END\n"
		L"	CLOSE curDbs\n"
		L"	DEALLOCATE curDbs";
}

/*----------------------------------------------------------------------------------------------
	Tries to start MSDE if it isn't already started
	@return True if MSDE is running, false if it couldn't be started. If false, an error
		message has been sent notifying the user.
----------------------------------------------------------------------------------------------*/
bool OleDbEncap::StartMSDE()
{
	StrApp strTitle(kstidSQLFailure);
	StrApp strMsgFmt;
	StrApp strMessage;

	// Test if SQL Server has actually been installed:
	bool fSqlServerInstalled = false;
	LONG lResult;
	HKEY hKey;

	// Open Registry at key containing SQL Server version number:
	lResult = RegOpenKeyEx(HKEY_LOCAL_MACHINE,
		L"SOFTWARE\\Microsoft\\Microsoft SQL Server\\SILFW\\MSSQLServer\\CurrentVersion",
		0, KEY_READ, &hKey);

	if (ERROR_SUCCESS == lResult)
	{
		const wchar_t * kpszValueName = L"CurrentVersion";
		// Fetch required buffer size:
		DWORD cbData = 0;
		lResult = RegQueryValueEx(hKey, kpszValueName, NULL, NULL, NULL, &cbData);
		if (cbData > 0)
		{
			// Read actual data:
			wchar_t * pszVersion = new wchar_t [cbData + 1];
			lResult = RegQueryValueEx(hKey, kpszValueName, NULL, NULL, LPBYTE(pszVersion),
				&cbData);

			if (ERROR_SUCCESS == lResult)
			{
				// As long as version string begins "9." we are OK:
				if (wcsncmp(pszVersion, L"9.", 2) == 0)
					fSqlServerInstalled = true;
			}
			// Clean up:
			delete[] pszVersion;
			pszVersion = NULL;
		}
		// Clean up:
		RegCloseKey(hKey);
		hKey = NULL;
	}
	if (!fSqlServerInstalled)
	{
		// SQL Server is not installed: tell user and quit immediately.
		// (If you just return false, the error will be ingored and this same code
		// will be tried three times.)
		strMessage.Load(kstidSqlNotInstalled);
		::MessageBox(NULL, strMessage.Chars(), strTitle.Chars(),
			MB_SYSTEMMODAL | MB_ICONEXCLAMATION | MB_OK);
		exit(0);
	}

	// Check that none of the database files have been compressed or encrypted:
	static bool fDataCompressed = false; // Static so as not to run test repeatedly.
	if (fDataCompressed)
		return false;

	// Test for compressed or encrypted data:
	StrApp strBadData = GetBadDataReport();
	if (strBadData.Length() > 0)
	{
		// There is data that needs fixing:
		fDataCompressed = true;
		strMessage.Load(kstidDataCompressed);
		StrApp strTempTitle(kstidDataCompressedTitle);

		// Report to user, and see if they want to go ahead with the automatic fix:
		if (::MessageBox(NULL, strMessage.Chars(), strTempTitle.Chars(),
			MB_ICONEXCLAMATION | MB_OKCANCEL | MB_SYSTEMMODAL) == IDOK)
		{
			// User want us to launch Decompressor. Set up data for creating new process:
			BOOL bReturnVal = false;
			STARTUPINFO si;
			PROCESS_INFORMATION process_info;

			ZeroMemory(&si, sizeof(si));
			si.cb = sizeof(si);

			StrUni stuCmd = DirectoryFinder::FwRootCodeDir();
			stuCmd.Append(stuCmd[stuCmd.Length() - 1] != '\\' ? _T("\\") : _T(""));
			stuCmd += _T("Decompressor.exe");

			bReturnVal = CreateProcess(stuCmd.Chars(), NULL, NULL, NULL, false, 0, NULL, NULL,
				&si, &process_info);

			if (bReturnVal)
			{
				CloseHandle(process_info.hThread);
				// Wait until process quits: code based on Microsoft support article 824042:
				// http://support.microsoft.com/kb/824042
				WaitForInputIdle(process_info.hProcess, INFINITE);
				WaitForSingleObject(process_info.hProcess, INFINITE);
			}
			// It doesn't matter too much if we could not create the process. The user will
			// be told the problem isn't fixed - it won't make much difference why this is so.
		}
		// Get a fresh report:
		strBadData = GetBadDataReport();
		if (strBadData.Length() > 0)
		{
			// Show details to user:
			strMsgFmt.Load(kstidDataStillCompressedReport);
			strMessage.Format(strMsgFmt.Chars(), strBadData.Chars());
			::MessageBox(NULL, strMessage.Chars(), strTempTitle.Chars(),
				MB_ICONSTOP | MB_OK | MB_SYSTEMMODAL);

			// We can't continue, as there are compressed or encrypted database files,
			// so abort silently:
			_set_abort_behavior(0, _CALL_REPORTFAULT);
			_set_abort_behavior(0, _WRITE_ABORT_MSG);
			// Calling abort() causes problems with C# apps. It closes all of the main windows, but keeps
			// the C# events from firing which keeps the app running without any windows. (TE-8597)
			//abort();
			::PostQuitMessage(-10203421);
		}
		else
			fDataCompressed = false;
	}

	DWORD dwNeeded = 0;
	DWORD dwErr;
	// Get the services control manager.
	SC_HANDLE hscm = OpenSCManager(NULL, NULL, STANDARD_RIGHTS_READ);
	if (!hscm)
	{
		dwErr = GetLastError();
		strMsgFmt.Load(kstidSCManagerFail);
		strMessage.Format(strMsgFmt.Chars(), dwErr);
		::MessageBox(NULL, strMessage.Chars(), strTitle.Chars(), MB_ICONERROR + MB_OK);
		return false;
	}

	// Open a service we can use to see if SQL Server is running.
	SC_HANDLE hos = OpenService(hscm, L"MSSQL$SILFW", SERVICE_QUERY_STATUS);
	if (!hos)
	{
		dwErr = GetLastError();
		// SERVICE_QUERY_STATUS is not available for Windows guest logons without
		// a password. It is only available to authenticated users. Since FW will
		// still run (though unable to create/delete databases) we'll use this
		// alternative approach to make sure it is running.
		IDataInitializePtr qdai;
		IDBInitializePtr qdbi;
		HRESULT hr;
		if (FAILED(CoCreateInstance(CLSID_MSDAINITIALIZE, NULL, CLSCTX_INPROC_SERVER,
			IID_IDataInitialize, (void **)&qdai)))
		{
			strMsgFmt.Load(kstidNoStatus);
			strMessage.Format(strMsgFmt.Chars(), dwErr);
			::MessageBox(NULL, strMessage.Chars(), strTitle.Chars(), MB_ICONERROR + MB_OK);
			return false;
		}
		// Try to open a connection on master using the standard FieldWorks sa password.
		achar psz[MAX_COMPUTERNAME_LENGTH + 1];
		ulong cch = isizeof(psz);
		::GetComputerName(psz, &cch);
		StrUni stuSvr(psz);
		stuSvr.Append(L"\\SILFW");
		StrUni stu;
		// Try first with our FieldWorks password.
		stu.Format(L"Provider=SQLNCLI; Server=%s; Database=master; Uid=sa; Pwd=inscrutable; ",
			stuSvr.Chars());
		// Removed: Pooling=false; from connection since Pooling isn't valid in this context.
		// Connect to the selected database using a connection string.
		if (SUCCEEDED(hr = qdai->GetDataSource(NULL, CLSCTX_INPROC_SERVER,
			stu.Chars(), IID_IDBInitialize, reinterpret_cast<IUnknown **>(&qdbi))))
			hr = qdbi->Initialize();
		if (SUCCEEDED(hr))
			return true; // Already running with FieldWorks password.

		// Try again with a blank password (only used when DB is being initialized).
		stu.Format(L"Provider=SQLNCLI; Server=%s; Database=master; Uid=sa;", stuSvr.Chars());
		// Removed from connection: Pooling=false; since Pooling isn't valid in this context.

		// Connect to the selected database using a connection string.
		if (SUCCEEDED(hr = qdai->GetDataSource(NULL, CLSCTX_INPROC_SERVER,
			stu.Chars(), IID_IDBInitialize, reinterpret_cast<IUnknown **>(&qdbi))))
			hr = qdbi->Initialize();
		if (SUCCEEDED(hr))
			return true; // Already running with a blank password.

		strMsgFmt.Load(kstidNoStatus);
		strMessage.Format(strMsgFmt.Chars(), dwErr);
		::MessageBox(NULL, strMessage.Chars(), strTitle.Chars(), MB_ICONERROR + MB_OK);
		return false;
	}

	// Allocate space for status report. (If successful, must be released before return.)
	LPSERVICE_STATUS_PROCESS lpsspBuf;
	lpsspBuf = (LPSERVICE_STATUS_PROCESS) LocalAlloc(LPTR, isizeof(SERVICE_STATUS_PROCESS));
	if (!lpsspBuf)
	{
		StrApp strMsg(kstidNoMemory);
		::MessageBox(NULL, strMsg.Chars(), strTitle.Chars(), MB_ICONERROR + MB_OK);
		return false;
	}

	// Get the status of SQL Server.
	bool fOk = QueryServiceStatusEx(hos, SC_STATUS_PROCESS_INFO, (LPBYTE)lpsspBuf,
		isizeof(SERVICE_STATUS_PROCESS), &dwNeeded);
	if (!fOk)
	{
		// Couldn't get the service we needed. Warn user and fail.
		dwErr = GetLastError();
		strMsgFmt.Load(kstidQSSFailed);
		strMessage.Format(strMsgFmt.Chars(), dwErr);
		::MessageBox(NULL, strMessage.Chars(), strTitle.Chars(), MB_ICONERROR + MB_OK);
		LocalFree(lpsspBuf);
		return false;
	}

	// Check to see that it is running.
	bool fRunning = lpsspBuf->dwCurrentState == SERVICE_RUNNING;
	if (!fRunning)
	{
		// Get a service that lets us start SQL Server.
		hos = OpenService(hscm, L"MSSQL$SILFW", SERVICE_START | SERVICE_QUERY_STATUS);
		if (!hos)
		{
			// The user probably doesn't have the privilege to start services.
			dwErr = GetLastError();
			strMsgFmt.Load(kstidCantOpen);
			strMessage.Format(strMsgFmt.Chars(), dwErr);
			::MessageBox(NULL, strMessage.Chars(), strTitle.Chars(), MB_ICONERROR + MB_OK);
			LocalFree(lpsspBuf);
			return false;
		}

		// Try to start SQL Server.
		fOk = StartService(hos, 0, NULL);
		if (!fOk)
		{
			// Something went wrong, so quit.
			dwErr = GetLastError();
			strMsgFmt.Load(kstidCantStart);
			strMessage.Format(strMsgFmt.Chars(), dwErr);
			::MessageBox(NULL, strMessage.Chars(), strTitle.Chars(), MB_ICONERROR + MB_OK);
			LocalFree(lpsspBuf);
			return false;
		}

		// Loop until it is started or we time out after 20 seconds.
		int cSec = 0;
		while (!fRunning)
		{
			fOk = QueryServiceStatusEx(hos, SC_STATUS_PROCESS_INFO, (LPBYTE)lpsspBuf,
				isizeof(SERVICE_STATUS_PROCESS), &dwNeeded);
			if (!fOk)
			{
				// Something went wrong if we can't get the status.
				dwErr = GetLastError();
				strMsgFmt.Load(kstidStatusFailed);
				strMessage.Format(strMsgFmt.Chars(), dwErr);
				::MessageBox(NULL, strMessage.Chars(), strTitle.Chars(), MB_ICONERROR + MB_OK);
				LocalFree(lpsspBuf);
				return false;
			}
			// Check twice a second to avoid hogging processor cycles.
			// On Mike Cocharn's new laptop :Sleep did not seem to be working. We would get the timeout
			// message within a second or two. :SleepEx appears to work reliably on his machine.
			//::Sleep(500);
			::SleepEx(500, FALSE);

			// If we can't get it started after 20 seconds, give up.
			// With SQL Server 2005 it is taking longer to get started, especially on some machines
			// and immediately after a FieldWorks installation. 10 seconds was not long enough.
			if (++cSec % 40 == 0)
			{
				// Failing to initialize probably means the SQL server is not running.
				// We've had some weird timeout problems on the network so offer to try
				// some more.
				StrApp strMsg(kstidSQLStartFailure);
				if (::MessageBox(NULL, strMsg.Chars(), strTitle.Chars(),
					MB_ICONERROR + MB_YESNO) != IDYES)
				{
					LocalFree(lpsspBuf);
					return false;
				}
			}
			fRunning = lpsspBuf->dwCurrentState == SERVICE_RUNNING;
		}
	}
	LocalFree(lpsspBuf);
	return true; // It's running, so we can go on.
}

/*----------------------------------------------------------------------------------------------
	Stops the SQL Server service.
	@return True if service is stopped, false if it could not be stopped.
----------------------------------------------------------------------------------------------*/
bool OleDbEncap::StopSqlServer()
{
	const achar * pszServiceName = _T("MSSQL$SILFW");
	const DWORD dwTimeout = 60000; // One minute timeout
	SC_HANDLE schSCManager;
	SC_HANDLE schService;
	SERVICE_STATUS_PROCESS ssp;
	SERVICE_STATUS ssDummy;
	DWORD dwStartTime = GetTickCount();
	DWORD dwBytesNeeded;
	StrApp strError;

	// Open a handle to the Service Control Manager database:
	schSCManager = OpenSCManager(
		NULL,								// local machine
		NULL,								// ServicesActive database
		STANDARD_RIGHTS_REQUIRED      | \
		SC_MANAGER_CONNECT            | \
		SC_MANAGER_CREATE_SERVICE     | \
		SC_MANAGER_ENUMERATE_SERVICE  | \
		SC_MANAGER_LOCK               | \
		SC_MANAGER_QUERY_LOCK_STATUS);		// access rights

	if (schSCManager == NULL)
	{
		strError.Assign(_T("Could not open service control manager."));
		LogError(strError);
		return false;
	}

	schService = OpenService(schSCManager, pszServiceName, SERVICE_ALL_ACCESS);

	if (schService == NULL)
	{
		strError.Format(_T("Could not open service '%s'."), pszServiceName);
		LogError(strError);
		return false;
	}

	// Make sure the service is not already stopped:
	if (!QueryServiceStatusEx(schService, SC_STATUS_PROCESS_INFO, (LPBYTE)&ssp,
		sizeof(SERVICE_STATUS_PROCESS), &dwBytesNeeded))
	{
		strError.Format(_T("Could not tell if service '%s' already stopped - error %d."),
			pszServiceName, GetLastError());
		LogError(strError);
		return false;
	}

	if (ssp.dwCurrentState == SERVICE_STOPPED)
		return true; // Service already stopped.

	// If a stop is pending, just wait for it:
	while (ssp.dwCurrentState == SERVICE_STOP_PENDING)
	{
		Sleep(ssp.dwWaitHint);
		if (!QueryServiceStatusEx(schService, SC_STATUS_PROCESS_INFO, (LPBYTE)&ssp,
			sizeof(SERVICE_STATUS_PROCESS), &dwBytesNeeded))
		{
			strError.Format(
				_T("Could not tell (2nd time) if service '%s' already stopped - error %d."),
				pszServiceName, GetLastError());
			LogError(strError);
			return false;
		}

		if (ssp.dwCurrentState == SERVICE_STOPPED)
			return true;

		if (GetTickCount() - dwStartTime > dwTimeout)
		{
			strError.Format(_T("Timed out waiting for service '%s' to stop."),
				pszServiceName);
			LogError(strError);
			return false;
		}
	}

	// If the service is running, dependencies must be stopped first (probably aren't any):
	const bool fStopDependencies = true;
	if (fStopDependencies)
	{
		DWORD i;
		DWORD dwBytesNeeded;
		DWORD dwCount;

		LPENUM_SERVICE_STATUS lpDependencies = NULL;
		ENUM_SERVICE_STATUS ess;
		SC_HANDLE hDepService;

		// Pass a zero-length buffer to get the required buffer size:
		if (EnumDependentServices(schService, SERVICE_ACTIVE, lpDependencies, 0,
			&dwBytesNeeded, &dwCount))
		{
			// If the Enum call succeeds, then there are no dependent services so do nothing
		}
		else
		{
			if (GetLastError() != ERROR_MORE_DATA)
			{
				strError.Format(
					_T("Unexpected error %d while trying to obtain buffer size needed to enumerate dependent services of '%s'."),
					GetLastError(), pszServiceName);
				LogError(strError);
				return false; // Unexpected error.
			}

			// Allocate a buffer for the dependencies:
			lpDependencies = (LPENUM_SERVICE_STATUS)HeapAlloc(GetProcessHeap(),
				HEAP_ZERO_MEMORY, dwBytesNeeded);

			if (!lpDependencies)
			{
				strError.Format(
					_T("Unexpected error while trying to create buffer needed to enumerate dependent services of '%s'."),
					GetLastError(), pszServiceName);
				LogError(strError);
				return false; // Unexpected error.
			}

			// Enumerate the dependencies:
			if (!EnumDependentServices(schService, SERVICE_ACTIVE, lpDependencies,
				dwBytesNeeded, &dwBytesNeeded, &dwCount))
			{
				strError.Format(
					_T("Unexpected error %d while trying to enumerate dependent services of '%s'."),
					GetLastError(), pszServiceName);
				LogError(strError);

				HeapFree(GetProcessHeap(), 0, lpDependencies);
				return false; // Unexpected error.
			}

			for (i = 0; i < dwCount; i++)
			{
				ess = *(lpDependencies + i);

				// Open the service:
				hDepService = OpenService(schSCManager, ess.lpServiceName,
					SERVICE_STOP | SERVICE_QUERY_STATUS);
				if (!hDepService)
				{
					strError.Format(
						_T("Unexpected error %d while trying to open dependent service '%s' of '%s'."),
						GetLastError(), ess.lpServiceName, pszServiceName);
					LogError(strError);

					HeapFree(GetProcessHeap(), 0, lpDependencies);
					return false; // Unexpected error.
				}

				// Send a stop code:
				if (!ControlService(hDepService, SERVICE_CONTROL_STOP, &ssDummy))
				{
					strError.Format(
						_T("Unexpected error %d while trying to stop dependent service '%s' of '%s'."),
						GetLastError(), ess.lpServiceName, pszServiceName);
					LogError(strError);

					CloseServiceHandle(hDepService);
					HeapFree(GetProcessHeap(), 0, lpDependencies);
					return false; // Unexpected error.
				}

				// Wait for the service to stop:
				do
				{
					if (!QueryServiceStatusEx(hDepService, SC_STATUS_PROCESS_INFO,
						(LPBYTE)&ssp, sizeof(SERVICE_STATUS_PROCESS), &dwBytesNeeded))
					{
						strError.Format(
							_T("Unexpected error %d while trying to get status of dependent service '%s' of '%s'."),
							GetLastError(), ess.lpServiceName, pszServiceName);
						LogError(strError);

						CloseServiceHandle(hDepService);
						HeapFree(GetProcessHeap(), 0, lpDependencies);
						return false; // Unexpected error.
					}

					if (ssp.dwCurrentState == SERVICE_STOPPED)
						break;

					if (GetTickCount() - dwStartTime > dwTimeout)
					{
						strError.Format(
							_T("Timed out while trying to stop dependent service '%s' of '%s'."),
							ess.lpServiceName, pszServiceName);
						LogError(strError);

						CloseServiceHandle(hDepService);
						HeapFree(GetProcessHeap(), 0, lpDependencies);
						return false;
					}

					Sleep(ssp.dwWaitHint);

				} while (ssp.dwCurrentState != SERVICE_STOPPED);

				// Always release the service handle:
				CloseServiceHandle(hDepService);
			} // Next dependent service

			// Always free the enumeration buffer:
			HeapFree(GetProcessHeap(), 0, lpDependencies);
		}
	}

	// Send a stop code to the main service:
	if (!ControlService(schService, SERVICE_CONTROL_STOP, &ssDummy))
	{
		strError.Format(_T("Unexpected error %d while trying to stop service '%s'."),
			GetLastError(), pszServiceName);
		LogError(strError);

		return false; // Unexpected error.
	}

	// Wait for the service to stop:
	do
	{
		if (!QueryServiceStatusEx(schService, SC_STATUS_PROCESS_INFO, (LPBYTE)&ssp,
			sizeof(SERVICE_STATUS_PROCESS), &dwBytesNeeded))
		{
			strError.Format(
				_T("Unexpected error %d while trying to get status of service '%s'."),
				GetLastError(), pszServiceName);
			LogError(strError);

			return false; // Unexpected error.
		}

		if (ssp.dwCurrentState == SERVICE_STOPPED)
			break;

		if (GetTickCount() - dwStartTime > dwTimeout)
		{
			strError.Format(_T("Timed out while trying to stop service '%s'."), pszServiceName);
			LogError(strError);

			return false;
		}

		Sleep(ssp.dwWaitHint);

	} while (ssp.dwCurrentState != SERVICE_STOPPED);

	// Return success:
	return true;
}

/*----------------------------------------------------------------------------------------------
	If any the .mdf or .ldf files in the Data folder are compressed or encrypted, attempts to
	undo the compressed and encrytped status. If this is not possible, returns a text list
	of the compressed/encrypted items. The same applies to the folder itself.
	This test is necessary because SQL Server fails with compressed or encrypted data files.
	@return text report of compressed and encrypted items.
----------------------------------------------------------------------------------------------*/
StrUni OleDbEncap::GetBadDataReport()
{
	StrUni strOutput;
	StrUni strDataDir;
	achar rgch[MAX_PATH];
	DWORD cb = isizeof(rgch);
	DWORD dwT;
	HKEY hk;

	// Retrieve the path to FW data folder:
	long lRet = ::RegCreateKeyEx(HKEY_LOCAL_MACHINE, _T("Software\\SIL\\FieldWorks"), 0,
		_T(""), REG_OPTION_NON_VOLATILE, KEY_QUERY_VALUE, NULL, &hk, NULL);
	if (lRet == ERROR_SUCCESS)
	{
		if (::RegQueryValueEx(hk, _T("RootDataDir"), NULL, &dwT, (BYTE *)rgch, &cb) == ERROR_SUCCESS)
		{
			// Extend to form path to database files:
			Assert(dwT == REG_SZ);
			strDataDir.Assign(rgch);
			strDataDir.Append(strDataDir[strDataDir.Length() - 1] != '\\' ? _T("\\") : _T(""));
			strDataDir.Append("Data");

			bool fFoundFirstFile = false;

			// Test the database folder itself:
			DWORD dwAttr = ::GetFileAttributes(strDataDir.Chars());
			if ((dwAttr & FILE_ATTRIBUTE_COMPRESSED) || (dwAttr & FILE_ATTRIBUTE_ENCRYPTED))
			{
				// The folder is bad:
				StrUni strDirFmt(kstidDataCompressedDirRef);
				StrUni strDir;
				strDir.Format(strDirFmt, strDataDir.Chars());
				StrUni strDirItself;
				strDirItself.Load(kstidDataCompressedDir);
				strOutput.FormatAppend(_T("%s\n%s"), strDir.Chars(), strDirItself.Chars());

				fFoundFirstFile = true;
			}

			// Test database files in the Data folder:
			SearchForBadData(_T("*.mdf"), strOutput, fFoundFirstFile, strDataDir);
			SearchForBadData(_T("*.ldf"), strOutput, fFoundFirstFile, strDataDir);
		}
	}
	return strOutput;
}

/*----------------------------------------------------------------------------------------------
	Searches for all files matching the given mask, and tests to see if any are compressed or
	encrypted. Appends a suitable list of matches to the given string, basing the syntax of the
	list on the flag passed in to say if any files matched previously.
	@param strMask The file expression to match; can contain DOS ? and * wildcards.
	@param strOutput [in, out] The text report of what this method (and its caller) finds.
	@param fFoundFirstFile [in, out] Flag to say if we've already found a matching file.
	@param strDataDir Folder where the search is to take place.
----------------------------------------------------------------------------------------------*/
void OleDbEncap::SearchForBadData(StrUni strMask, StrUni & strOutput, bool & fFoundFirstFile,
								  StrUni strDataDir)
{
	WIN32_FIND_DATA wfd;
	HANDLE hFind;
	StrUni strFullMask;

	// Make a full path out of the file search pattern:
	strFullMask.Assign(strDataDir.Chars());
	strFullMask.Append(strFullMask[strFullMask.Length() - 1] != '\\' ? _T("\\") : _T(""));
	strFullMask.Append(strMask);

	// Begin searching for matching files:
	hFind = ::FindFirstFile(strFullMask.Chars(), &wfd);
	if (hFind != INVALID_HANDLE_VALUE)
	{
		do
		{
			if ((wfd.dwFileAttributes & FILE_ATTRIBUTE_COMPRESSED) ||
				(wfd.dwFileAttributes & FILE_ATTRIBUTE_ENCRYPTED))
			{
				// We have found an encrypted or compressed file.
				if (!fFoundFirstFile)
				{
					StrUni strDirFmt(kstidDataCompressedDirRef);
					StrUni strDir;
					strDir.Format(strDirFmt, strDataDir.Chars());
					strOutput.FormatAppend(_T("%s\n%s"), strDir.Chars(), wfd.cFileName);

					fFoundFirstFile = true;
				}
				else
					strOutput.FormatAppend(_T(", %s"), wfd.cFileName);
			}
		} while (::FindNextFile(hFind, &wfd));
		::FindClose(hFind);
	}
}

/*----------------------------------------------------------------------------------------------
	Return the amount of free space in the database log file.
	@param nReservespace Specifies how much space to reserve for other programs on the hard disk
	@param pbstrSvr Pointer to a BSTR to accept the server string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::GetFreeLogKb(int nReservespace, int * pnKbFree)
{
	BEGIN_COM_METHOD
	ComBool fIsNull;
	ComBool fMoreRows;
	IOleDbCommandPtr qodc;
	StrUni stuSqlStmt;

	int nLogFileSize;
	int nLogFileSpaceUsed;
	int64 nLogFileMaxSize;
	int nSpaceAvailForLogFile;
	wchar rgchBuffer[MAX_PATH];

	CheckHr(CreateCommand(&qodc));
	stuSqlStmt.Assign(L"exec LogInfo$ ? output, ? output, ? output, ? output, ? output");

	qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_WSTR,
		(ULONG*)&rgchBuffer, isizeof(rgchBuffer));
	qodc->SetParameter(2, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG*)&nLogFileSize,
		sizeof(nLogFileSize));
	qodc->SetParameter(3, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG*)&nLogFileSpaceUsed,
		sizeof(nLogFileSpaceUsed));
	qodc->SetParameter(4, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I8, (ULONG*)&nLogFileMaxSize,
		sizeof(nLogFileMaxSize));
	qodc->SetParameter(5, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG*)&nSpaceAvailForLogFile,
		sizeof(nSpaceAvailForLogFile));
	CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtStoredProcedure));
	qodc->GetParameter(4, (BYTE*)&nLogFileMaxSize, sizeof(nLogFileMaxSize), &fIsNull);
	qodc->GetParameter(5, (BYTE*)&nSpaceAvailForLogFile, sizeof(nSpaceAvailForLogFile), &fIsNull);

	if(nLogFileMaxSize == -1)
		*pnKbFree = nSpaceAvailForLogFile - nReservespace;
	else
		*pnKbFree = nSpaceAvailForLogFile;

	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}


/*----------------------------------------------------------------------------------------------
	Return the server name associated with this instance.
	@param pbstrSvr Pointer to a BSTR to accept the server string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::get_Server(BSTR * pbstrSvr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrSvr);
	m_sbstrServer.Copy(pbstrSvr);
	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}


/*----------------------------------------------------------------------------------------------
	Return the server name associated with this instance.
	@param pbstrSvr Pointer to a BSTR to accept the server string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::get_Database(BSTR * pbstrDb)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrDb);
	m_sbstrDatabase.Copy(pbstrDb);
	END_COM_METHOD(g_fact, IID_IOleDbEncap);
}

/*----------------------------------------------------------------------------------------------
	Begin an Undo/Redo transaction.
	We use the 'handle' here just to keep track of whether a transaction was already open
	(common in test code), in which case, we don't want to close one in EndGroup.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::BeginGroup(int * phandle)
{
	BEGIN_COM_METHOD;

	if (!m_fInitialized)
		return E_UNEXPECTED;

	*phandle = m_fTransactionOpen;
	if (!m_fTransactionOpen)
		return BeginTrans();
	END_COM_METHOD(g_fact, IID_IUndoGrouper);
}
/*----------------------------------------------------------------------------------------------
	End an Undo/Redo transaction.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::EndGroup(int handle)
{
	if (!m_fInitialized)
		return E_UNEXPECTED;

	if (!handle)
		return CommitTrans();
	else
		return S_OK;
}
/*----------------------------------------------------------------------------------------------
	Rollback an Undo/Redo transaction.
	Note: if BeginGroup didn't open a transaction, this won't roll anything back. This could
	well be a problem except that calling BeginGroup when there is a transaction already open
	is something that is only done for testing purposes.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbEncap::CancelGroup(int handle)
{
	if (!m_fInitialized)
		return E_UNEXPECTED;

	if (!handle)
		return RollbackTrans();
	else
		return S_OK;
}


//:>********************************************************************************************
//:>	OleDbCommand - Constructor/Destructor
//:>********************************************************************************************

static DummyFactory g_factCmd(_T("SIL.DbAccess.OleDbCommand"));

OleDbCommand::OleDbCommand()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
#ifdef DEBUG_ODC_REF_COUNTS
	StrApp str;
	str.FormatAppend(_T("OleDbCommand Constructor: %d, cref=%d\n"), this, m_cref);
	OutputDebugString(str.Chars());
#endif DEBUG_ODC_REF_COUNTS

/* This should not be needed because we use NewObj which presets everything to zero.
	m_qunkCommand = NULL;
	m_dbpParams.pData = NULL;
	m_cluParameters = 0;
	for (int i=0; i<knMaxParamPerCommand; i++)
	{
		m_rgParamData[i] = NULL;
	}
	m_qmres = NULL;

	// For Rowset
	m_pRowData = NULL;
	m_rgBindings = NULL;
	m_cColumns = 0;
	m_rghRows = NULL;
	m_cRows = 0;
*/
}


OleDbCommand::~OleDbCommand()
{
#ifdef DEBUG_ODC_REF_COUNTS
	StrApp str;
	str.Format(_T("OleDbCommand destructor: %d\n"), this);
	OutputDebugString(str.Chars());
#endif DEBUG_ODC_REF_COUNTS
	if (m_rghRows)
		delete[] m_rghRows;
	CoTaskMemFree(m_pRowData);
	m_pRowData = NULL;

	for (int i = 0; i < m_cColumns; i++)
	{
		if (m_rgBindings[i].pObject)
			CoTaskMemFree(m_rgBindings[i].pObject);
	}
	CoTaskMemFree(m_rgBindings);
	m_rgBindings = NULL;

	if (m_dbpParams.pData)
	{
		// Clear the values of this structure but don't set m_dbpParams to NULL
		// since it is part of the structure.
		CoTaskMemFree(m_dbpParams.pData);
		m_dbpParams.pData = NULL;
		m_dbpParams.cParamSets = 0;
		m_dbpParams.hAccessor = NULL;
	}

	for (int i=0; i<knMaxParamPerCommand; i++)
	{
		if (m_rgParamData[i])
		{
			CoTaskMemFree(m_rgParamData[i]);
			m_rgParamData[i] = NULL;
		}
		m_rgluParamDataSize[i] = 0;
	}

	m_cluParameters = 0;

	ModuleEntry::ModuleRelease();
}


//:>********************************************************************************************
//:>	OleDbCommand - IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP OleDbCommand::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_IOleDbCommand)
		*ppv = static_cast<IOleDbCommand *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IOleDbCommand);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


STDMETHODIMP_(ULONG) OleDbCommand::AddRef(void)
{
#ifdef DEBUG_ODC_REF_COUNTS
	StrApp str;
	for (int i = m_cref; --i >= 0; )
		str.Append("    ");
	str.FormatAppend(_T("OleDbCommand AddRef: %d, cref=%d\n"), this, m_cref + 1);
	OutputDebugString(str.Chars());
#endif DEBUG_ODC_REF_COUNTS

	Assert(m_cref > 0);
	::InterlockedIncrement(&m_cref);
	return m_cref;
}


STDMETHODIMP_(ULONG) OleDbCommand::Release(void)
{
	Assert(m_cref > 0);
	ulong cref = ::InterlockedDecrement(&m_cref);
#ifdef DEBUG_ODC_REF_COUNTS
	StrApp str;
	for (int i = m_cref; --i >= 0; )
		str.Append("    ");
	str.FormatAppend(_T("OleDbCommand Release: %d, cref=%d\n"), this, m_cref);
	OutputDebugString(str.Chars());
#endif DEBUG_ODC_REF_COUNTS
	if (!cref)
	{
		CloseCommand();
		m_cref = 1;
		delete this;
	}
	return cref;
}


/*----------------------------------------------------------------------------------------------
	${IOleDbCommand#ColValWasNull}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbCommand::ColValWasNull(int * pfIsNull)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfIsNull);

	if (!m_fLastColWasNull)
		*pfIsNull = 0;
	else
		*pfIsNull = -1;

	END_COM_METHOD(g_factCmd, IID_IOleDbCommand);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbCommand#ExecCommand}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbCommand::ExecCommand(BSTR bstrSqlStatement, int nStatementType)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrSqlStatement);

	m_stuCommand.Assign(bstrSqlStatement); // remember for error reporting.
#if 99-99
	StrAnsi staTrace;
	staTrace.Format("OleDbCommand::ExecCommand(\"%B\", %d)%n",
		bstrSqlStatement, nStatementType);
	::OutputDebugStringA(staTrace.Chars());
#endif

	// TODO: (AlistairI) We need a test to see if the query is too long. Excessively long
	// queries cause the program to crash.
	// Although the SQL Server Books Online page entitled "Maximum Capacity Specifications"
	// indicates that we should be able to cope with very long queries,
	// in practice, this is not the case. It may be that OLEDB is the limiting factor.
	// However, it has not been established what the maximum length is, or what to do
	// if we can predict that a query is too long.
	// This attempt isn't sufficient - some Nunit tests asserted here, even though their
	// queries are assumed to be OK:
	//AssertMsg(BstrLen(bstrSqlStatement) < 4000, "Maximum query length exceeded.");

	ULONG cluRequiredBufferSize;
	LONG cRowsAffected = 0;
	ComBool fRowsetObtained;
	ULONG i;
	ULONG j;
	DBSTATUS * pDbStatus;
	ULONG * pLen;
	BYTE ** ppParamData = NULL;
	BYTE * pTemp = NULL;
	DBBINDSTATUS * rgDBBindStatus;
	ULONG * rgParamOrdinals;
	ICommandPropertiesPtr qcmp;
	ICommandTextPtr qcdt;
	ICommandWithParametersPtr qcwp;
	IDBCreateCommandPtr qdcc;
	IRowsetPtr qrws;
	IOleDbEncapPtr qode;
	IUnknownPtr qunkSession;

	HRESULT hr;
	bool retry = false;
	do
	{
		// If the command is being re-executed, release all the interfaces, etc
		// since we have to rebind and get new ones.
		CloseCommandExceptParams();

		// (Use defult parameters to obtain a default rowset: no server cursor).

		// Get the session object from database so that we can create a command.
		CheckHr(m_qunkOde->QueryInterface(IID_IOleDbEncap, (void**) &qode));
		CheckHr(qode->GetSession(&qunkSession));

		// Takes an IUnknown pointer on a session object and attempts to create a command
		// object using the session's IDBCreateCommand interface. Since this interface is
		// optional, this may fail.
		CheckHr(qunkSession->QueryInterface(IID_IDBCreateCommand, (void **)&qdcc));
		CheckHr(qdcc->CreateCommand(NULL, IID_ICommand, &m_qunkCommand));

		// Set the text for this command, using the SQL command text dialect.  We could use
		// the default command text dialect, but since this entire class is assuming that
		// we are relating to a relational database via SQL, there is not much point.
		CheckHr(m_qunkCommand->QueryInterface(IID_ICommandText, (void**)&qcdt));
		CheckHr(qcdt->SetCommandText(DBGUID_SQL, bstrSqlStatement));

#ifdef LOG_EVERY_COMMAND
		// These lines log every executed command.
		if (m_qfistLog)
		{
			StrAnsi sta;
			sta.Assign(bstrSqlStatement);
			sta.Append("%n%n");
			m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), NULL);
		}
#endif

		if (m_cluParameters)
		{
			// Get the ICommandWithParameters interface to set up parameter values.
			// Get command with parameters interface.
			CheckHr(qcdt->QueryInterface(IID_ICommandWithParameters, (void**)&qcwp));

			// Calculate the total buffer size required to store all the parameter data and
			// for each parameter binding, set the byte offsets for Length, Status, and Value.
			cluRequiredBufferSize = 0;
			ppParamData = (BYTE **) m_rgParamData;
			for (i=0; i<m_cluParameters; i++)
			{
				// Set the byte offsets (within the pData block of memory) in the DbBinding
				// structure for the given parameter.
				m_rgdbbParamBindings[i].obValue = sizeof(ULONG) + sizeof(DBSTATUS)
					+ cluRequiredBufferSize;
				m_rgdbbParamBindings[i].obStatus = sizeof(ULONG) + cluRequiredBufferSize;
				m_rgdbbParamBindings[i].obLength = cluRequiredBufferSize;

				// Add the length of the parameter data length to the total and roundup to
				// avoid byte alignment problems.
				cluRequiredBufferSize += sizeof(DBSTATUS) + sizeof(ULONG)
					+ m_rgluParamDataSize[i];
				cluRequiredBufferSize = (((ULONG)(cluRequiredBufferSize) +
					((kluRoundupAmount) - 1)) & ~((kluRoundupAmount) - 1));
			}

			// Allocate memory for the Parameter ordinal array and the DbBindStatus array.
			rgParamOrdinals = reinterpret_cast<ULONG *> (CoTaskMemAlloc(sizeof(ULONG)
				* m_cluParameters));
			if (!rgParamOrdinals)
				ThrowHr(E_OUTOFMEMORY);
			rgDBBindStatus = reinterpret_cast<DBBINDSTATUS *>
				(CoTaskMemAlloc(sizeof(DBBINDSTATUS) * m_cluParameters));
			if (!rgDBBindStatus)
				ThrowHr(E_OUTOFMEMORY);

			// Allocate a continguous block of memory to store all the parameter values and
			// then copy the parameter data values from the m_rgParamData array to this new
			// buffer.
			if (m_dbpParams.pData)
			{
				CoTaskMemFree(m_dbpParams.pData);
			}
			m_dbpParams.pData = reinterpret_cast<void *>(CoTaskMemAlloc(cluRequiredBufferSize));
			if (!m_dbpParams.pData)
				ThrowHr(E_OUTOFMEMORY);
			for (i=0; i<m_cluParameters; i++)
			{
				pLen = (ULONG *)((BYTE *) m_dbpParams.pData + m_rgdbbParamBindings[i].obLength);
				pDbStatus = (DBSTATUS *) ((BYTE *) m_dbpParams.pData
					+ m_rgdbbParamBindings[i].obStatus);
				if (ppParamData[i] == NULL)
				{
					*pLen = 0; // actually not used unless status is OK, but for robustness...
					*pDbStatus = DBSTATUS_S_ISNULL;
				}
				else
				{
					pTemp = ((BYTE *) m_dbpParams.pData) + m_rgdbbParamBindings[i].obValue;
					for (j=0; j<m_rgluParamDataSize[i]; j++)
					{
						*pTemp = (ppParamData[i])[j];
						pTemp++;
					}
					// was cluRequiredBufferSize, but that makes no sense.
					*pLen = m_rgluParamDataSize[i];
					*pDbStatus = DBSTATUS_S_OK;
				}

				// While we're iterating through the parameters, set the parameter ordinal
				// array.
				rgParamOrdinals[i] = i + 1;
			}

			// Set parameter information.
			CheckExtHr(qcwp->SetParameterInfo(m_cluParameters,
				rgParamOrdinals, m_rgdbpbi), qcwp, IID_ICommandWithParameters);
			CoTaskMemFree(rgParamOrdinals);

			// Get the IAccessor interface, then create the accessor for the defined
			// parameters (only).
			// Note that this is NOT a rowdata accessor.
			CheckHr(qcwp->QueryInterface(IID_IAccessor, (void**)&m_qacc));
			CheckExtHr(m_qacc->CreateAccessor(DBACCESSOR_PARAMETERDATA /*|DBACCESSOR_ROWDATA*/,
				m_cluParameters, m_rgdbbParamBindings, cluRequiredBufferSize, &m_dbpParams.hAccessor,
				rgDBBindStatus), m_qacc, IID_IAccessor);
			CoTaskMemFree(rgDBBindStatus);

			// Fill the remaining 2 parts of the DBPARAMS structure for command execution.
			m_dbpParams.cParamSets = 1;
		}

		try
		{
			if (nStatementType == knSqlStmtNoResults)
			{
				// Execute the command and ignore any results.
				if (m_cluParameters)
				{
					hr = Execute1(IID_NULL, &m_dbpParams, NULL, NULL, qcdt);
				}
				else
				{
					hr = Execute1(IID_NULL, NULL, NULL, NULL, qcdt);
				}
				hr = FullErrorCheck(hr, qcdt, IID_ICommandText);

				m_qmres = NULL;
				m_iIndex = kluRowsetStartIndex;
				qrws = NULL;
				fRowsetObtained = TRUE;
			}
			else if ((nStatementType == knSqlStmtSelectWithOneRowset) && !m_cluParameters)
			{
				// ENHANCE JohnT: if we are asked for just one row set, why not get it
				// directly, even if there are parameters? (Old code used this branch only if no
				// params.)
				// Execute the command (with no parameters).  The user could have entered a
				// non-row returning command even though he specified knSqlStmtSelectWithOneRowset
				// so we will check for this just in case.
				// ENHANCE PaulP:  Should notice (or failure) be given if there is no rowset?
				// NOTE: If the query returns "(n row(s) affected)" lines prior to the data, this
				// query will return S_OK but m_qunkRowset will be NULL. To avoid this, set nocount
				// on in your query to disable these extra lines.
				hr = Execute1(IID_IRowset, NULL, NULL, reinterpret_cast<IUnknown **>(&m_qunkRowset),
					qcdt);
				hr = FullErrorCheck(hr, qcdt, IID_ICommandText);

				HRESULT hrCmd = hr;
				if (hrCmd == DB_S_ERRORSOCCURRED)
				{
					try
					{
#if 1
						if (m_qfistLog)
						{
							StrAnsi sta;
							ULONG cch;
							sta.Format("%n%n--hr for Execute = %s%n", AsciiHresult(hr));
							CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
							switch (nStatementType)
							{
							case knSqlStmtNoResults:
								sta.Format("--Statement Type = NoResults%n");
								break;
							case knSqlStmtSelectWithOneRowset:
								sta.Format("--Statement Type = SelectWithOneRowset%n");
								break;
							case knSqlStmtStoredProcedure:
								sta.Format("--Statement Type = StoredProcedure%n");
								break;
							default:
								sta.Format("--Statement Type = %d (??)%n", nStatementType);
								break;
							}
							CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
							sta.Format("--Statement:%n%B%n", bstrSqlStatement);
							CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
						}
#else
						ULONG cch;
						StrUni stuStatement(bstrSqlStatement);
						StrAnsi sta;
						if (hr == DB_S_ERRORSOCCURRED)
							sta.Format("%n%n--hr for Execute = DB_S_ERRORSOCCURRED%n");
						else
							sta.Format("%n%n--hr for Execute = %x%n", hr);
						if (m_qfistLog)
							CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
						sta.Format("--Statement Type = %d%n", nStatementType);
						if (m_qfistLog)
							CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
						sta.Assign("--Statement:\r\n");
						if (m_qfistLog)
							CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
						sta.Assign(stuStatement);
						sta.Append("\r\n");
						if (m_qfistLog)
							CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
#endif
					}
					catch (...)
					{
					}	// Failure to log: no action necessary.
				}

				// NOTE:  This is one-based, not zero-based.
				m_iIndex = kluRowsetStartIndex;
			}
			else
			{
				// Execute the command.  Regardless of whether one specifies
				// knSqlStmtSelectWithOneRowset or knSqlStmtStoredProcedure, we will use the
				// IMultipleResults interface.
				// NOTE: This will return E_FAIL if a previous IRowset isn't released. Since
				// our IOleDbCommand object holds a smart pointer to IRowset, it means we
				// must clear (or reuse) the IOleDbCommandPtr before creating a new
				// IOleDbCommandPtr.
				hr = Execute1(IID_IMultipleResults, &m_dbpParams, &cRowsAffected,
					(IUnknown **)&m_qmres, qcdt);
				hr = FullErrorCheck(hr, qcdt, IID_ICommandText);

				HRESULT hrCmd = hr;
				// Pull out the results and the first rowset.
				fRowsetObtained = FALSE;
				HRESULT hr0 = hr;
				do
				{
					hr0 = m_qmres->GetResult(NULL, 0, IID_IRowset, &cRowsAffected,
						(IUnknown **)&m_qunkRowset);
					FullErrorCheck(hr0, m_qmres, IID_IMultipleResults);
					if (hrCmd == DB_S_ERRORSOCCURRED)
					{
						try
						{
#if 1
							if (m_qfistLog)
							{
								StrAnsi sta;
								ULONG cch;
								sta.Format("%n%n--hr for Execute = %s%n", AsciiHresult(hr));
								CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
								switch (nStatementType)
								{
								case knSqlStmtNoResults:
									sta.Format("--Statement Type = NoResults%n");
									break;
								case knSqlStmtSelectWithOneRowset:
									sta.Format("--Statement Type = SelectWithOneRowset%n");
									break;
								case knSqlStmtStoredProcedure:
									sta.Format("--Statement Type = StoredProcedure%n");
									break;
								default:
									sta.Format("--Statement Type = %d (??)%n", nStatementType);
									break;
								}
								CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
								sta.Format("--Statement:%n%B%n", bstrSqlStatement);
								CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
							}
#else
							ULONG cch;
							StrUni stuStatement(bstrSqlStatement);
							StrAnsi sta;
							if (hr == DB_S_ERRORSOCCURRED)
								sta.Format("%n%n--hr for Execute = DB_S_ERRORSOCCURRED%n");
							else
								sta.Format("%n%n--hr for Execute = %x%n", hr);
							if (m_qfistLog)
								CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
							sta.Format("--Statement Type = %d%n", nStatementType);
							if (m_qfistLog)
								CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
							sta.Assign("--Statement:\r\n");
							if (m_qfistLog)
								CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
							sta.Assign(stuStatement);
							sta.Append("\r\n");
							if (m_qfistLog)
								CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), &cch));
#endif
						}
						catch (...)
						{
						}	// Failure to log: no action necessary.
					}
					if (m_qunkRowset && !fRowsetObtained)
					{
						// NOTE:  This is one-based, not zero-based.
						m_iIndex = kluRowsetStartIndex;
						fRowsetObtained = TRUE;
					}
				} while (hr0 != DB_S_NORESULT && !fRowsetObtained);
				hr = hr0;
			}

			retry = false;
		}
		catch (Throwable & thr)
		{
			// if this is a communications link error, attempt to reconnect to database
			// this is a goofy way to check if this is a comm link error, but it works
			// and it requires minimal changes to the code
			if (!retry && wcsstr(thr.Message(), StrUni(kstidDatabaseCommLink).Chars()))
			{
				// release all open resources associated with this current database connection
				qrws.Clear();
				qcwp.Clear();
				qcmp.Clear();
				qcdt.Clear();
				m_qunkCommand.Clear();
				qdcc.Clear();
				qunkSession.Clear();
				ComBool transOpen;
				qode->IsTransactionOpen(&transOpen);
				StrUni stuCpt(kstidReconnectCpt);

				HRESULT hr1 = E_FAIL;
				// only attempt to reconnect if there is not a transaction currently open
				if (!transOpen)
					IgnoreHr(hr1 = qode->Reinit());
				if (SUCCEEDED(hr1))
				{
					// Reinit succeeded, report reconnect to user
					StrUni stuMsg(kstidReconnect);
					MessageBox(NULL, stuMsg.Chars(), stuCpt.Chars(), MB_ICONWARNING);
					retry = true;
				}
				else
				{
					_set_abort_behavior(0, _CALL_REPORTFAULT);
					_set_abort_behavior(0, _WRITE_ABORT_MSG);
					// Reinit failed, database is down, report shutdown to user
					StrUni stuMsg(kstidReconnectFail);
					MessageBox(NULL, stuMsg.Chars(), stuCpt.Chars(), MB_ICONERROR | MB_SYSTEMMODAL);
					// shutdown
					// Calling abort() causes problems with C# apps. It closes all of the main windows, but keeps
					// the C# events from firing which keeps the app running without any windows. (TE-8597)
					//abort();
					::PostQuitMessage(-10203478);
					retry = false;
				}
			}
			else
			{
				throw thr;
			}
		}
	}
	while (retry);

	if (FAILED(hr))
	{
		// Check that we produced error info. Clients may use CheckHr, which asserts
		// if there is no error info. All paths above should produce it. If not, convert
		// to an internal error so we at least get a stack dump.
		IErrorInfoPtr qerrinfo;
		GetErrorInfo(0, &qerrinfo);
		if (!qerrinfo)
			ThrowHr(WarnHr(hr));
	}

	return hr;

	END_COM_METHOD(g_factCmd, IID_IOleDbCommand);
}

/*----------------------------------------------------------------------------------------------
	${IOleDbCommand#GetColValue}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbCommand::GetColValue(ULONG iluColIndex, BYTE * prgbDataBuffer,
	ULONG cbBufferLength, ULONG * pcbSpaceRequiredForData, ComBool * pfIsNull, int cbPad)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcbSpaceRequiredForData);
	ChkComOutPtr(pfIsNull);
	ChkComArrayArg(prgbDataBuffer, cbBufferLength);
	Assert((uint)cbPad <= 2);
	if ((uint)cbPad > 2)
		ReturnHr(E_INVALIDARG);

	if (!m_qunkRowset)
		ThrowHr(WarnHr(E_UNEXPECTED));

	DBSTATUS dwStatus;
	HACCESSOR hAccessor;
	ULONG cbDataRead = 0;
	ULONG cbDataValueLength = 0;
	void * pvValue;
	IRowsetPtr qrws;
	IAccessorPtr qacc;
	HRESULT hr;

	// Make sure pad request is appropriate to data type. Strings get one byte (or none), wstrs
	// get two or none.  Other stuff gets none, except that a stream may use 0, 1, or 2.
	bool fCbPadOk = cbPad == 0 ||
		(m_rgBindings[iluColIndex - 1].wType == DBTYPE_STR && cbPad == 1) ||
		(m_rgBindings[iluColIndex - 1].wType == DBTYPE_WSTR && cbPad == 2) ||
		(m_rgBindings[iluColIndex - 1].wType == DBTYPE_IUNKNOWN);
	Assert(fCbPadOk);
	if (!fCbPadOk)
		return E_INVALIDARG;

	try
	{
		// Create an accessor to obtain data.
		// Get the data for the row handle. The provider will copy (and convert, if necessary)
		// the data for each of the columns that are described in our accessor
		// into the buffer.
		CheckHr(m_qunkRowset->QueryInterface(IID_IRowset, (void**)&qrws));
		CheckHr(m_qunkRowset->QueryInterface(IID_IAccessor, (void**)&qacc));
		// Released on all exit paths from this method.
		CheckHr(qacc->CreateAccessor(DBACCESSOR_ROWDATA, 1 /*m_cColumns*/,
			&(m_rgBindings[iluColIndex-1]), 0, &hAccessor, NULL));
		IgnoreHr(hr = qrws->GetData(m_rghRows[0], hAccessor, m_pRowData));
		if (FAILED(hr) && hr != DB_E_ERRORSOCCURRED)
			ThrowInternalError(hr);	// Allow a later throw for DB_E_ERRORSOCCURRED.

		// Set some shortcut variables to make the rest of the code a little cleaner.
		dwStatus = *(DBSTATUS *)((BYTE *)m_pRowData + m_rgBindings[iluColIndex - 1].obStatus);
		cbDataValueLength = *(ULONG *)((BYTE *)m_pRowData +
			m_rgBindings[iluColIndex - 1].obLength);
		pvValue = (BYTE *)m_pRowData + m_rgBindings[iluColIndex - 1].obValue;

		*pfIsNull = FALSE;
		switch (dwStatus)
		{
		case DBSTATUS_S_OK:

			// Special handling for BLOB's (eg. text, ntext, image, binary).  We will access
			// the data through an ISequentialStream interface.
			if (m_rgBindings[iluColIndex - 1].wType == DBTYPE_IUNKNOWN)
			{
				// This column has been bound as an ISequentialStream object, therefore the
				// data in pvValue (ie. RowsetRec.pRowData) is a pointer to the object's
				// ISequentialStream interface.
				ComSmartPtr<ISequentialStream> qsst;
				qsst.Attach(*(ISequentialStream**)pvValue);

				// The length of the object in cbDataValueLength has been verified to be
				// correct for all BLOBs that are read in starting up the Data Notebook with
				// TestLangProj database. This in spite of a somewhat ambiguous statement
				// in MSDN/Platform SDK Documentation/MDAC SDK/Microsoft OLEDB/
				// OLEDB Programmers Reference/Part 1/Chapter 6/Binding Data Values/Length.
				// Therefore the approach here is to use that length, and to begin reading
				// the data only when a sufficiently large buffer is supplied.
				// A side-effect of this is that an insufficiently large buffer is returned
				// empty. If it is large enough for requested nulls these are written, thus
				// returning an empty string in that case.
				*pcbSpaceRequiredForData = cbDataValueLength + cbPad;
				if (*pcbSpaceRequiredForData > cbBufferLength)
				{
					if (cbBufferLength >= (ULONG)cbPad && cbPad)
					{
						int ich;
						BYTE * pch = prgbDataBuffer;
						for (ich = 0; ich < cbPad; ++ich)
							*pch++ = 0;
					}
					return S_FALSE;
				}
				else
				{
				// Read data from the stream to the given buffer. There is room.
				CheckHr(qsst->Read(prgbDataBuffer, cbBufferLength, &cbDataRead));

				// Add as many null bytes as requested.
				BYTE * pch = (prgbDataBuffer + cbDataRead);
				BYTE * pchLim = pch + cbPad;

				while (pch < pchLim)
					*pch++ = '\0';
				}
			}
			else // not dbtype unknown (i.e., not a blob)
			{
				if ((m_rgBindings[iluColIndex - 1].wType == DBTYPE_DATE) ||
					(m_rgBindings[iluColIndex - 1].wType == DBTYPE_DBDATE) ||
					(m_rgBindings[iluColIndex - 1].wType == DBTYPE_BSTR) ||
					(m_rgBindings[iluColIndex - 1].wType == DBTYPE_ERROR) ||
					(m_rgBindings[iluColIndex - 1].wType == DBTYPE_NULL) ||
					(m_rgBindings[iluColIndex - 1].wType == DBTYPE_EMPTY))
				{
					ThrowInternalError(E_NOTIMPL, L"Attempt to read unimplemented field type");
				}

				*pcbSpaceRequiredForData = cbDataValueLength + cbPad;
				// If the given buffer is NOT large enough to store the data value, simply
				// return the required amount (plus any null termination requested).
				// ???? (JohnL) I'm not sure that what follows does this.
				if (*pcbSpaceRequiredForData > cbBufferLength)
				{
					// It doesn't fit. As a safety thing put zero as value if that will fit.
					if (cbBufferLength > isizeof(ULONG))
					{
						// If passed empty buffer don't put anything!
						(*prgbDataBuffer) = 0;
					}
					qacc->ReleaseAccessor(hAccessor, NULL);
					return S_FALSE;
				}

				// Copy the data byte-for-byte to the given buffer.
				::memcpy(prgbDataBuffer, (BYTE *) pvValue, cbDataValueLength);

				// And null terminate as requested.
				BYTE * pch = (prgbDataBuffer + cbDataValueLength);
				BYTE * pchLim = pch + cbPad;

				while (pch < pchLim)
					*pch++ = '\0';
			}
			break;

		// The data is NULL, so don't try to display it.
		case DBSTATUS_S_ISNULL:
			memset(prgbDataBuffer, 0, cbBufferLength);
			(*pcbSpaceRequiredForData) = 0;
			*pfIsNull = TRUE;
			break;

		// This should not happen, however, in this case, the data was fetched, but may have
		// been truncated.
		case DBSTATUS_S_TRUNCATED:
			ThrowInternalError(E_UNEXPECTED, L"Column buffer too small");
			break;

		case DBSTATUS_S_DEFAULT:
		default:
			ThrowInternalError(E_UNEXPECTED, L"Unexpected result code from GetData");
			break;
		}

		// Release the accessor.
		qacc->ReleaseAccessor(hAccessor, NULL);
	}
	catch(...)
	{
		// If we got an IAccessor we also obtained an hAccessor and need to release it
		// if anything goes wrong.
		if (qacc)
			qacc->ReleaseAccessor(hAccessor, NULL);
		throw;
	}

	// Set "WasNull" value if the column value was null.
	m_fLastColWasNull = *pfIsNull;

	END_COM_METHOD(g_factCmd, IID_IOleDbCommand);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbCommand#GetInt}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbCommand::GetInt(int iColIndex, int * pnValue)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnValue);

	ULONG cbSpaceRequiredForData;
	ComBool fIsNull;
	ULONG iluColIndex = iColIndex;
	ULONG luValue = 0;
	CheckHr(GetColValue(iluColIndex, (BYTE*)&luValue, isizeof(ULONG), &cbSpaceRequiredForData,
		&fIsNull, 0));
	m_fLastColWasNull = fIsNull;
	*pnValue = luValue;

	END_COM_METHOD(g_factCmd, IID_IOleDbCommand);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbCommand#GetParameter}

	Note: currently this is only used for HVOs. Note the recommended enhancement below if we
	want to use it for strings.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbCommand::GetParameter(ULONG iluColIndex, BYTE * prgbDataBuffer,
	ULONG cbBufferLength, ComBool * pfIsNull)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgbDataBuffer, cbBufferLength);
	ChkComOutPtr(pfIsNull);

	DBSTATUS dwStatus;
	ULONG luParamDataSize;
	BYTE * pbData;
	DBBINDING * pdbpb;

	pdbpb = &(m_rgdbbParamBindings[iluColIndex - 1]);
	pbData = (BYTE *) m_dbpParams.pData;
	dwStatus = *(DBSTATUS *)((BYTE *)pbData + m_rgdbbParamBindings[iluColIndex - 1].obStatus);
	luParamDataSize = m_rgluParamDataSize[iluColIndex - 1];

	// Check the status of the parameter and return the corresponding value.
	if ((dwStatus == DBSTATUS_S_OK) || (dwStatus == DBSTATUS_S_TRUNCATED))
	{
		if (cbBufferLength >= luParamDataSize)
		{
			// Copy the data value byte for byte
			// ENHANCE PaulP:  Probably have to add \0 for strings.
			for (ULONG i=0; i< luParamDataSize; i++)
			{
				*(prgbDataBuffer + i) = *(pbData + pdbpb->obValue + i);
			}
		}
		else
		{
			ThrowInternalError(E_UNEXPECTED, L"Parameter buffer too small");
		}
		*pfIsNull = FALSE;
		return S_OK;
	}
	else if (dwStatus == DBSTATUS_S_ISNULL)
	{
		// OPTIMIZE PaulP:  Not sure whether to NULL the given buffer or just leave it.
		memset(prgbDataBuffer, 0, cbBufferLength);
		*pfIsNull = TRUE;
		return S_OK;
	}
	else
	{
		*pfIsNull = TRUE;
		return E_FAIL;
	}

	*pfIsNull = TRUE;
	return E_FAIL;

	END_COM_METHOD(g_factCmd, IID_IOleDbCommand);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbCommand#GetRowset}

	This method performs the following actions:

	-If this is the first time the method has been called since the command was first executed
	 (i.e. since the ExecCommand method was called), a pointer to the next Rowset is retrieved,
	 as produced by the SQL command.  If it is not the first time that the method was called,
	 memory that was allocated for the previous rowset is freed.

	-Column information for the rowset is retrieved from the database in order to set the
	 values in the DBBINDING structure appropriately.

	-The total size of the row is calculated so memory can be allocated for the row(s) that we
	 will fetch later.

	-An accessor is created.  This is basically a handle to a collection of bindings that tells
	 the OLE DB provider (ie. SQL Server) how to copy (and convert, if necessary)  column data
	 into our row data buffer.  The provider will fill this buffer according to the description
	 contained in the accessor.  Later, we will fetch data from this buffer.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbCommand::GetRowset(int nRowsBuffered)
{
	BEGIN_COM_METHOD;

	long cRowsAffected;
	ULONG cStorageObjs = 0;
	ULONG dwOffset = 0;
	ComBool fMultipleObjs = FALSE;
	ComBool fRowsetObtained;
	long i;
	long iCol;
	IColumnsInfoPtr qcoi;
	LPWSTR pswStringBuffer = NULL;
	DBCOLUMNINFO * rgColumnInfo = NULL;

	try
	{
		// If the index of the current RowsetRec is kluRowsetStartIndex, it indicates that the
		// ExecCommand or ExecParamCommand has just been called and has set the first rowset
		// in the RowsetRec already, so simple skip over this.  If not, get another rowset
		// using the IMultiResults interface.
		if (m_iIndex > kluRowsetStartIndex)
		{
			HRESULT hr;
			fRowsetObtained = FALSE;
			do
			{	// Don't go on with "success" codes other than S_OK and DB_S_ERRORSOCCURRED.
				if (!m_qmres)
					ThrowHr(WarnHr(E_UNEXPECTED));
				FullErrorCheck(hr = m_qmres->GetResult(NULL, 0, IID_IRowset, &cRowsAffected,
					(IUnknown**)&m_qunkRowset), m_qmres, IID_IMultipleResults);
				if (m_qunkRowset && !fRowsetObtained)
				{
					// Set values in the Rowset record.
					CoTaskMemFree(m_pRowData);
					m_pRowData = NULL;
					m_luRowSize = 0;
					for (i = 0; i < m_cColumns; i++)
					{
						if (m_rgBindings[i].pObject)
							CoTaskMemFree(m_rgBindings[i].pObject);
					}
					CoTaskMemFree(m_rgBindings);
					m_rgBindings = NULL;
					m_cColumns = 0;
					delete[] m_rghRows;
					m_rghRows = NULL;
					m_cRows = 0;
					fRowsetObtained = TRUE;
				}
			} while ((hr == S_OK || hr == DB_S_ERRORSOCCURRED) && !fRowsetObtained);
			if (!fRowsetObtained)
			{
				if (m_qfistLog)
				{
					StrAnsi sta;
					sta.Format("%nGetRowset returned S_FALSE after hr = %s%n",
						AsciiHresult(hr));
					CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), NULL));
				}
				return S_FALSE;
			}
			if (hr != S_OK && m_qfistLog)
			{
				StrAnsi sta;
				sta.Format("%nGetRowset encountered hr = %s%n", AsciiHresult(hr));
				CheckHr(m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), NULL));
			}
		}

		// Increment the (rowset) index so that next time this method is called we know
		// that we need to pull out another rowset using the IMultipleResults interface.
		m_iIndex++;

		// Begin the process of creating an accessor.  First, obtain the column information
		// for the rowset; from this, we can find out the following information that we need
		// to construct the bindings array:
		//		-number of columns
		//		-ordinal of each column
		//		-precision and scale of numeric columns
		//		-OLE DB data type of the column
		if (!m_qunkRowset)
			ThrowHr(WarnHr(E_UNEXPECTED));
		CheckHr(m_qunkRowset->QueryInterface(IID_IColumnsInfo,
			reinterpret_cast<void **>(&qcoi)));
		FullErrorCheck(qcoi->GetColumnInfo(reinterpret_cast<ULONG *>(&m_cColumns), &rgColumnInfo,
			&pswStringBuffer), qcoi, IID_IColumnsInfo);

		// Allocate memory for the bindings array.  There is a one-to-one mapping between the
		// columns returned from GetColumnInfo and the bindings.
		m_rgBindings = reinterpret_cast<DBBINDING *>(
			CoTaskMemAlloc(m_cColumns * isizeof(DBBINDING)));
		if (!m_rgBindings)
			ThrowHr(E_OUTOFMEMORY);
		memset(m_rgBindings, 0, m_cColumns * sizeof(DBBINDING));

		// Construct the binding for each column.
		for (iCol = 0; iCol < m_cColumns; iCol++)
		{
			m_rgBindings[iCol].iOrdinal = rgColumnInfo[iCol].iOrdinal;

			// We want the value, length, and status (ie. whether it is NULL or not) of each
			// column we retrieve.
			m_rgBindings[iCol].dwPart = DBPART_VALUE | DBPART_LENGTH | DBPART_STATUS;

			// Set the byte offsets for the column status, length, and data value in the
			// row data buffer (which we will allocate in the future, once we have all the
			// column information).  When we pass this buffer to the SQL Server database,
			// (ie. the OLE DB provider) it will fill in the appropriate values in accordance
			// with the binding we have provided for that column.  Later, we will need the
			// binding offsets so we know where to pull out the data from the buffer.
			m_rgBindings[iCol].obStatus = dwOffset;
			m_rgBindings[iCol].obLength = dwOffset + sizeof(DBSTATUS);
			m_rgBindings[iCol].obValue = dwOffset + sizeof(DBSTATUS) + sizeof(ULONG);

			// Any memory allocated for the data value will be owned by us, the client.
			m_rgBindings[iCol].dwMemOwner = DBMEMOWNER_CLIENTOWNED;

			// This is not a parameter binding.
			m_rgBindings[iCol].eParamIO = DBPARAMIO_NOTPARAM;

			// We want to use the precision and scale of the column.
			m_rgBindings[iCol].bPrecision = rgColumnInfo[iCol].bPrecision;
			m_rgBindings[iCol].bScale = rgColumnInfo[iCol].bScale;

			// Initially, we set the length for this data in our buffer to 0.
			// The correct value for this will be calculated directly below.
			m_rgBindings[iCol].cbMaxLen = 0;

			// Determine the maximum number of bytes required to store data for the column
			// from the OLE DB provider (ie. SQL Server) based on the native data type as
			// retrieved from the column information.  For strings and wstrings, add extra room
			// for a NULL-termination character.  Bind the column type according to the native
			// data type in the database.  (Special action is taken below for BLOB's.)
			m_rgBindings[iCol].wType = rgColumnInfo[iCol].wType;
			switch (rgColumnInfo[iCol].wType)
			{
			case DBTYPE_BOOL:  // bit
				m_rgBindings[iCol].cbMaxLen = sizeof(VARIANT_BOOL);
				break;

			case DBTYPE_DBTIMESTAMP:  // smalldatetime
				m_rgBindings[iCol].cbMaxLen = sizeof(DBTIMESTAMP);
				break;

			case DBTYPE_GUID:  // uniqueidentifier
				m_rgBindings[iCol].cbMaxLen = sizeof(GUID);
				break;

			case DBTYPE_I1:
				m_rgBindings[iCol].cbMaxLen = sizeof(signed char);
				break;

			case DBTYPE_UI1:  // tinyint
				m_rgBindings[iCol].cbMaxLen = sizeof(BYTE);
				break;

			case DBTYPE_I2:  // smallint
				m_rgBindings[iCol].cbMaxLen = sizeof(SHORT);
				break;

			case DBTYPE_UI2:  // smallint
				m_rgBindings[iCol].cbMaxLen = sizeof(USHORT);
				break;

			case DBTYPE_I4:	 // int
				m_rgBindings[iCol].cbMaxLen = sizeof(LONG);
				break;

			case DBTYPE_UI4:
				m_rgBindings[iCol].cbMaxLen = sizeof(ULONG);
				break;

			case DBTYPE_I8:
				m_rgBindings[iCol].cbMaxLen = sizeof(LARGE_INTEGER);
				break;

			case DBTYPE_UI8:
				m_rgBindings[iCol].cbMaxLen = sizeof(ULARGE_INTEGER);
				break;

			case DBTYPE_DECIMAL:
				m_rgBindings[iCol].cbMaxLen = sizeof(DECIMAL);
				break;

			case DBTYPE_DATE:
				m_rgBindings[iCol].cbMaxLen = sizeof(DATE);
				break;

			case DBTYPE_DBDATE:
				m_rgBindings[iCol].cbMaxLen = sizeof(DBDATE);
				break;

			case DBTYPE_NUMERIC:
				m_rgBindings[iCol].cbMaxLen = sizeof(DB_NUMERIC);
				break;

			case DBTYPE_R4:
				m_rgBindings[iCol].cbMaxLen = sizeof(float);
				break;

			case DBTYPE_R8:
				m_rgBindings[iCol].cbMaxLen = sizeof(double);
				break;

			case DBTYPE_BYTES:  // binary, timestamp, varbinary
				m_rgBindings[iCol].cbMaxLen = rgColumnInfo[iCol].ulColumnSize;
				m_rgBindings[iCol].wType = DBTYPE_BYTES;
				break;

			case DBTYPE_BSTR:
				// ENHANCE PaulP:  Maybe actually test this out someday to see if it works :)
				// Add space for the character count in the front of the BSTR.
				m_rgBindings[iCol].cbMaxLen = sizeof(ULONG) +
					(rgColumnInfo[iCol].ulColumnSize * sizeof(WCHAR));
				break;

			case DBTYPE_STR:  // char, varchar
				// Add extra space for a null character terminator, which is not included in
				// the column size.
				m_rgBindings[iCol].cbMaxLen = (rgColumnInfo[iCol].ulColumnSize + 1)
					* sizeof(char);
				break;

			case DBTYPE_WSTR:  // nchar, nvarchar, sysname
				// Add extra space for a null character terminator, which is not included in
				// the column size.
				m_rgBindings[iCol].cbMaxLen = (rgColumnInfo[iCol].ulColumnSize + 1)
					* sizeof(WCHAR);
				break;

			case DBTYPE_ERROR:
				m_rgBindings[iCol].cbMaxLen = sizeof(SCODE);
				break;

			case DBTYPE_NULL:
			case DBTYPE_EMPTY:
			//case DBTYPE_CY:  // money, smallmoney
			default:
				// ENHANCE PaulP:  Not sure whether to return E_FAIL here or just specify a
				// default size.
				m_rgBindings[iCol].cbMaxLen = 30;
				m_rgBindings[iCol].wType = DBTYPE_WSTR;
				break;
			}

			// If the provider's native data type for this column is DBTYPE_IUNKNOWN or this
			// is a BLOB column (eg. the database column is of type "ntext" or "txt") bind
			// this column as an ISequentialStream object.  Overwrite the previous values set
			// for the m_rgBindings.
			// !NOTE1: SQL Server 7 only allows one BLOB column to be selected per SQL query.
			//		   (see PlatformSDK \ Data Access Services \
			//		    MS Data Access Components (MDAC) SDK \ MS OLE DB \ OLE DB Providers \
			//		    SQL Server Provider \ BLOBs and COM Objects)
			// !NOTE2:  SQLServer does not support ISequentialStream:Write for non-NULL values.
			fMultipleObjs = FALSE;
			if ((rgColumnInfo[iCol].wType == DBTYPE_IUNKNOWN ||
				rgColumnInfo[iCol].dwFlags & DBCOLUMNFLAGS_ISLONG) /*&&
				(fMultipleObjs || !cStorageObjs)*/)
			{
				// To create an ISequentialStream object, we will bind this column as
				// DBTYPE_IUNKNOWN to indicate that we are requesting this column as an object.
				m_rgBindings[iCol].wType = DBTYPE_IUNKNOWN;

				// We want to allocate enough space in our buffer for the ISequentialStream
				// pointer we will obtain from the provider.
				m_rgBindings[iCol].cbMaxLen = sizeof(ISequentialStream *);

				// To specify the type of object that we want from the provider, we need to
				// create a DBOBJECT structure and place it in our binding for this column.
				m_rgBindings[iCol].pObject = reinterpret_cast<DBOBJECT *>(
					CoTaskMemAlloc(isizeof(DBOBJECT)));
				if (!m_rgBindings[iCol].pObject)
					ThrowHr(E_OUTOFMEMORY);

				// Direct the provider to create an ISequentialStream object over the data for
				// this column.
				m_rgBindings[iCol].pObject->iid = IID_ISequentialStream;

				// We want read access on the ISequentialStream object that the provider will
				// create for us.
				m_rgBindings[iCol].pObject->dwFlags = STGM_READ;

				// Keep track of the number of (ISequentialStream) storage objects
				// (ie. BLOB columns).
				cStorageObjs++;
			}
			// SQL Server 7 allows only one BLOB column per SQL select query, so raise an
			// exception and return E_UNEXPECTED.
			else if ((cStorageObjs >= 1) && (rgColumnInfo[iCol].dwFlags & DBCOLUMNFLAGS_ISLONG))
			{
				Assert(false);
				CheckHr(E_UNEXPECTED);
			}

			// Update the offset past the end of this column's data so that the next column
			// will begin in the correct place in the buffer.
			dwOffset = m_rgBindings[iCol].cbMaxLen + m_rgBindings[iCol].obValue;

			// Ensure that the data for the next column will be correctly aligned for all
			// platforms.  If this is the last column, still do this alignment in the event
			// that we decide to pull in multiple rows from the database at a time.
			dwOffset = (((ULONG)(dwOffset) + ((kluRoundupAmount) - 1))
				& ~((kluRoundupAmount) - 1));
		}

		// Allocate enough memory to hold 1 row of data.  This is where the actual row data
		// from the OLE DB provider (ie. SQL Server) will be placed.
		// ENHANCE PaulP:  May want to allow the client code to designate the number of rows
		// fetched to customize access.  Unfortunately, the more rows fetched, the more
		// static the rowset will be (and less dynamic) so this would change the rowset
		// behavior.
		nRowsBuffered = 1;		// Currently, set nRowsBuffered=1 always.
		CoTaskMemFree(m_pRowData);
		m_pRowData = NULL;
		m_pRowData = CoTaskMemAlloc(dwOffset * nRowsBuffered);
		if (!m_pRowData)
			ThrowHr(E_OUTOFMEMORY);

		m_luRowSize = dwOffset;
	}
	catch(...)
	{
		// Release references.
		if (rgColumnInfo)
			CoTaskMemFree(rgColumnInfo);
		if (pswStringBuffer)
			CoTaskMemFree(pswStringBuffer);

		throw;
	}

	// Release references.
	if (rgColumnInfo)
		CoTaskMemFree(rgColumnInfo);
	if (pswStringBuffer)
		CoTaskMemFree(pswStringBuffer);

	END_COM_METHOD(g_factCmd, IID_IOleDbCommand);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbCommand#NextRow}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbCommand::NextRow(ComBool * pfMoreRows)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfMoreRows);

	ULONG cRowsObtained;
	IRowsetPtr qrws;
	// Obtain the IRowset interface for use in fetching rows and data.

	CheckHr(m_qunkRowset->QueryInterface(IID_IRowset, (void **)&qrws));

	// Release any row handles that we had previously obtained.
	if (m_rghRows != NULL)
	{
		CheckHr(qrws->ReleaseRows(m_cRows, m_rghRows, NULL, NULL, NULL));

		// Since we are allowing the provider to allocate the memory for the row handle
		// array, we will free this memory and reset the pointer to NULL. If this is not
		// NULL on the next call to this method, the provider will assume that it points
		// to an allocated array of the required size (which may not be the case if we
		// obtained less than m_cRows rows from the last call).
		delete[] m_rghRows;
		m_rghRows = NULL;
		m_cRows = 0;
	}

	m_rghRows = NewObj HROW[1]; // m_cRows

	// Attempt to get m_cRows row handles from the provider.
	FullErrorCheck(qrws->GetNextRows(DB_NULL_HCHAPTER, 0 /* lOffset */, 1 /* m_cRows */,
		&cRowsObtained, &m_rghRows), qrws, IID_IRowset);

	if (cRowsObtained)
	{
		m_cRows = 1;			// Set values in the rowset record.
		*pfMoreRows = true;		// Indicate that we did indeed get at least one row.
	}
	else
	{
		delete[] m_rghRows;
		m_rghRows = NULL;
		m_cRows = 0;
		*pfMoreRows = false;
	}

	END_COM_METHOD(g_factCmd, IID_IOleDbCommand);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbCommand#SetParameter}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbCommand::SetParameter(ULONG iluParamIndex, DWORD dwFlags,
	BSTR bstrParamName, WORD nDataType, ULONG * prgluDataBuffer, ULONG cbBufferLength)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrParamName);
	ChkComArrayArg(prgluDataBuffer, cbBufferLength / isizeof(*prgluDataBuffer));

	if (!m_rgdbpbi || !m_rgdbbParamBindings)
		ThrowHr(WarnHr(E_UNEXPECTED));

	ULONG * pData = NULL;
	DBBINDING * pdbb;
	DBPARAMBINDINFO * pdbpbi;
	ULONG * pluParameterDataSize;
	void ** ppParamDataElement;

	try
	{
		// Do not allow the user to set a parameter index that is greater than the parameter
		// array can hold.
		if (iluParamIndex > knMaxParamPerCommand)
			ThrowHr(WarnHr(E_INVALIDARG));

		pdbpbi = &(m_rgdbpbi[iluParamIndex - 1]);
		pdbb = &(m_rgdbbParamBindings[iluParamIndex - 1]);
		ppParamDataElement = &(m_rgParamData[iluParamIndex - 1]);
		pluParameterDataSize = &(m_rgluParamDataSize[iluParamIndex - 1]);

		// Increment the count of parameters for this CommandRec if the index of the given
		// parameter is higher than the current setting.  This should happen every time, if the
		// client code binds parameters in order.  It is the responsibility of the client code
		// to ensure that there are no gaps.
		if (iluParamIndex > m_cluParameters)
			m_cluParameters = iluParamIndex;

		// Free the memory allocated for the parameter in the case that this parameter was
		// bound previously and now is being set to a new value.
		if (*ppParamDataElement)
		{
			CoTaskMemFree(*ppParamDataElement);
			*ppParamDataElement = NULL;
		}

		// Set the ulParamSize to a default value, which is number of bytes.  For WCHAR's
		// and STR's it is number of characters.  This value may later be overwritten.
		(*pdbpbi).ulParamSize = cbBufferLength;

		// Set the ParameterBindInfo record
		switch (nDataType)
		{
		case DBTYPE_BOOL:  // bit
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_BOOL";
			break;

		case DBTYPE_DBTIMESTAMP:  // smalldatetime
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_DBTIMESTAMP";
			break;

		case DBTYPE_GUID:  // uniqueidentifier
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_GUID";
			break;

		case DBTYPE_I1:
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_I1";
			break;

		case DBTYPE_UI1:  // tinyint
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_UI1";
			break;

		case DBTYPE_I2:  // smallint
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_I2";
			break;

		case DBTYPE_UI2:  // smallint
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_UI2";
			break;

		case DBTYPE_I4:	 // int
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_I4";
			break;

		case DBTYPE_UI4:
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_UI4";
			break;

		case DBTYPE_I8:
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_I8";
			break;

		case DBTYPE_UI8:
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_UI8";
			break;

		case DBTYPE_DECIMAL:
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_DECIMAL";
			break;

		case DBTYPE_DATE:
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_DATE";
			break;

		case DBTYPE_DBDATE:
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_DBDATE";
			break;

		case DBTYPE_NUMERIC:
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_NUMERIC";
			break;

		case DBTYPE_R4:
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_R4";
			break;

		case DBTYPE_R8:
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_R8";
			break;

		case DBTYPE_BYTES:  // binary, timestamp, varbinary
			// ENHANCE JohnT(PaulP): if we ever have any non-varbinary byte data,
			// we need to do something different here... how can we recognize we need to?
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_VARBINARY";
			break;

		case DBTYPE_BSTR:
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_BSTR";
			break;

		case DBTYPE_STR:  // char, varchar
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_VARCHAR";
			(*pdbpbi).ulParamSize = cbBufferLength / sizeof(char);
			break;

		case DBTYPE_WSTR:  // nchar, nvarchar, sysname
			(*pdbpbi).pwszDataSourceType = L"DBTYPE_WVARCHAR";
			(*pdbpbi).ulParamSize = cbBufferLength / sizeof(WCHAR);
			break;

		case DBTYPE_ERROR:
		case DBTYPE_NULL:
		case DBTYPE_EMPTY:
		//case DBTYPE_CY:  // money, smallmoney
		default:
			ThrowInternalError(E_NOTIMPL, L"Attempt to set unimplmented param type");
			break;
		}
		(*pdbpbi).pwszName = bstrParamName;
		(*pdbpbi).dwFlags = dwFlags;
		// ENHANCE PaulP:  Not sure what precision and scale to use here.
		//				Disregarded for non-numeric types.
		// JohnT: This allows 11 decimal digits of precision, and no fractional part for dates and
		// fixed-scale numbers. It seems to work OK for the types we currently use.
		(*pdbpbi).bPrecision = 11;
		(*pdbpbi).bScale = 0;

		// Set the data binding structure.
		(*pdbb).iOrdinal = iluParamIndex;
		// Set the byte offsets later, just before we execute the command; then we will have
		// the length of all the other parameters as well.
		(*pdbb).obLength = 0;
		(*pdbb).obStatus = 0;
		(*pdbb).obValue = 0;
		(*pdbb).pTypeInfo = NULL;
		(*pdbb).pObject = NULL;
		(*pdbb).pBindExt = NULL;
		(*pdbb).dwMemOwner = DBMEMOWNER_CLIENTOWNED;

		if (dwFlags & DBPARAMFLAGS_ISOUTPUT)
		{
			if (dwFlags & DBPARAMFLAGS_ISINPUT)
			{
				(*pdbb).eParamIO = DBPARAMIO_INPUT | DBPARAMIO_OUTPUT;
			}
			else
			{
				(*pdbb).eParamIO = DBPARAMIO_OUTPUT;
			}
		}
		else	// Assume parameter is just input
		{
			(*pdbb).eParamIO =	DBPARAMIO_INPUT;
		}
		(*pdbb).dwPart = DBPART_VALUE | DBPART_STATUS | DBPART_LENGTH;
		(*pdbb).cbMaxLen = cbBufferLength;
		(*pdbb).dwFlags = 0;
		(*pdbb).wType = nDataType;
		(*pdbb).bPrecision = 11;
		(*pdbb).bScale = 0;

		// Set the data value.
		// ENHANCE PaulP:  Seriously consider changing this to a pointer assignment rather than
		// a memory allocation and copy.  Doing this would not allow this encapsulation module
		// to become a COM object however, it could potentially eliminate this expensive
		// operation for BLOBs, etc.
		if (prgluDataBuffer == NULL)
		{
			*ppParamDataElement = NULL;
		}
		else
		{
			pData = reinterpret_cast<ULONG *>(CoTaskMemAlloc(cbBufferLength));
			if (!pData)
				ThrowHr(E_OUTOFMEMORY);
			for (ULONG i=0; i< cbBufferLength; i++)
				*(((BYTE *) pData) + i) = *(((BYTE*)prgluDataBuffer) + i);

			// Set the data array to the new pointer and designate how much space it takes up.
			*ppParamDataElement = pData;
		}
		*pluParameterDataSize = cbBufferLength;
	}
	catch(...)
	{
		// TODO JohnT (PaulP):  Log some error here, I suppose.
		if (pData)
			CoTaskMemFree(pData);
		throw;
	}

	END_COM_METHOD(g_factCmd, IID_IOleDbCommand);
}


/*----------------------------------------------------------------------------------------------
	${IOleDbCommand#SetByteBuffParameter}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbCommand::SetByteBuffParameter(ULONG iluParamIndex, DWORD dwFlags,
	BSTR bstrParamName, BYTE * prgbDataBuffer, ULONG cbBufferLength)
{
	BEGIN_COM_METHOD;
	CheckHr(SetParameter(iluParamIndex, dwFlags, bstrParamName, DBTYPE_BYTES,
		(ULONG*)prgbDataBuffer, cbBufferLength));
	END_COM_METHOD(g_factCmd, IID_IOleDbCommand);
}

/*----------------------------------------------------------------------------------------------
	${IOleDbCommand#SetStringParameter}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbCommand::SetStringParameter(ULONG iluParamIndex, DWORD dwFlags,
	BSTR bstrParamName, OLECHAR * prgchDataBuffer, ULONG cbBufferLength)
{
	BEGIN_COM_METHOD;
	CheckHr(SetParameter(iluParamIndex, dwFlags, bstrParamName, DBTYPE_WSTR,
		(ULONG*)prgchDataBuffer, cbBufferLength * sizeof(OLECHAR)));
	END_COM_METHOD(g_factCmd, IID_IOleDbCommand);
}

/*----------------------------------------------------------------------------------------------
	Initializes the property structure pProp which is used to define the type of result set that
	will be returned.  (See @i{"Rowsets and SQL Server Cursors"} in the SQL Server
	documentation @i{"Books On-Line")}
	@param pProp Pointer to a DBPROP structure which are used to pass property values.
	@param dwPropertyID Property ID value.
	@param vtType Usually allow for the default value on this as I'm not sure what this is.
	@param lValue Usually allow for the default value on this as I'm not sure what this is.
	@param dwOptions Usually allow for the default value on this as I'm not sure what this is.
----------------------------------------------------------------------------------------------*/
void OleDbCommand::AddProperty(DBPROP * pProp, DBPROPID dwPropertyID, VARTYPE vtType,
	LONG lValue, DBPROPOPTIONS dwOptions)
{
	AssertPtr(pProp);

	// Set up the property structure.
	pProp->dwPropertyID = dwPropertyID;
	pProp->dwOptions = dwOptions;
	pProp->dwStatus = DBPROPSTATUS_OK;
	pProp->colid = DB_NULLID;
	V_VT(&pProp->vValue) = vtType;

	// Since VARIANT data is a union, we can place the value in any member (except for
	// VT_DECIMAL, which is a union with the whole VARIANT structure -- but we know we're
	// not passing VT_DECIMAL).
	V_I4(&pProp->vValue) = lValue;
}


/*----------------------------------------------------------------------------------------------
	Closes a command.  (Basically, release memory resources). Closes the associated rowset as
	well. If anything goes wrong ignore it. Called from Release(), so should not throw
	exception.
----------------------------------------------------------------------------------------------*/
void OleDbCommand::CloseCommand()
{
	int i;

	try
	{
		// Clear/Release everything except the parameters.
		CloseCommandExceptParams();

		if (m_dbpParams.pData)
		{
			// Clear the values of this structure but don't set m_dbpParams to NULL
			// since it is part of the structure.
			CoTaskMemFree(m_dbpParams.pData);
			m_dbpParams.pData = NULL;
			m_dbpParams.cParamSets = 0;
			m_dbpParams.hAccessor = NULL;
		}

		for (i=0; i<knMaxParamPerCommand; i++)
		{
			if (m_rgParamData[i])
			{
				CoTaskMemFree(m_rgParamData[i]);
				m_rgParamData[i] = NULL;
			}
			m_rgluParamDataSize[i] = 0;
		}

		m_cluParameters = 0;
	}
	catch(...)
	{
		// TODO JohnT(PaulP):  Maybe log some error
	}
}

/*----------------------------------------------------------------------------------------------
	Interface access to CloseCommandExceptParams
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbCommand::ReleaseExceptParams()
{
	BEGIN_COM_METHOD;
	CloseCommandExceptParams();
	END_COM_METHOD(g_factCmd, IID_IOleDbCommand);
}

/*----------------------------------------------------------------------------------------------
	Releases memory resources, etc for a command except that it does not affect the parameter
	values.  Originally, this was part of CloseCommand but was sectioned out so that client code
	could set new parameter values and re-issue the same command without having to close it.
	Note that it DOES release the accessor for the parameters, since ExecCommand always makes
	a new one of these.
----------------------------------------------------------------------------------------------*/
void OleDbCommand::CloseCommandExceptParams()
{
	HRESULT hr;
	int iCol;
	IRowsetPtr qrws;

	if (m_qacc)
	{
		m_qacc->ReleaseAccessor(m_dbpParams.hAccessor, NULL);
		m_qacc = NULL;
	}

	if (m_pRowData)
	{
		CoTaskMemFree(m_pRowData);
		m_pRowData = NULL;
	}

	m_luRowSize = 0;

	if (m_rgBindings)
	{
		for (iCol=0; iCol < m_cColumns; iCol++)
		{
			if (m_rgBindings[iCol].pObject)
			{
				CoTaskMemFree(m_rgBindings[iCol].pObject);
			}
		}
		CoTaskMemFree(m_rgBindings);
		m_rgBindings = NULL;
		m_cColumns = 0;
	}

	if (m_qunkRowset)
	{
		// Release any row handles that we had previously obtained.
		if (m_rghRows != NULL)
		{
			CheckHr(hr = m_qunkRowset->QueryInterface(IID_IRowset, (void**)&qrws));
			CheckHr(hr = qrws->ReleaseRows(m_cRows, m_rghRows, NULL,
				NULL, NULL));

			// Since we are allowing the provider to allocate the memory for the row
			// handle array, we will free this memory and reset the pointer to NULL.
			delete[] m_rghRows;
			m_rghRows = NULL;
		}
		// New queries will fail if an old rowset interface is still being held.
		m_qunkRowset.Clear();
	}

	m_cRows = 0;
}


/*----------------------------------------------------------------------------------------------
	${IOleDbCommand#Init}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OleDbCommand::Init(IUnknown * punkOde, IStream * pfistLog)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(punkOde);
	ChkComArgPtrN(pfistLog);

	m_qfistLog = pfistLog;
	m_qunkOde = punkOde;

	END_COM_METHOD(g_factCmd, IID_IOleDbCommand);
}

/*----------------------------------------------------------------------------------------------
	Initialize the Lock timeout mode. Separate from Init() to avoid change to interface.
	@param olt Lock Timeout Mode for SQL Server.
----------------------------------------------------------------------------------------------*/
void OleDbCommand::InitTimeoutMode(OdeLockTimeoutMode olt)
{
	m_oltCmd = olt;
}

/*----------------------------------------------------------------------------------------------
	Throws an exception using ThrowHr if the HRESULT is a failure code, otherwise hr is returned.
	@param hrErr HRESULT value from which an exception is thrown.

	Before throwing the exeption, attempts to extract error info from the failed object, punk.

	Usage: FullErrorCheck(pMyObj->SomeMethod(...), pMyObj, IID_MyObjInterface);
	Where the second argument is a pointer to the object whose HR is being tested, and
	IID_MyObjInterface is the IID of the interface containing SomeMethod.
----------------------------------------------------------------------------------------------*/
HRESULT OleDbCommand::FullErrorCheck(HRESULT hrErr, IUnknown * punk, REFIID iid)
{
	// Even if a SUCCESS code is returned by the method, "its success may not be identical to
	// that intended by the application developer" (MSDN on OLEDB Errors). So we ought to look
	// into things a bit if hr is not S_OK.
	if (hrErr == S_OK)
	{
		CleanUpErrorInfo();
		return S_OK;
	}
	HRESULT hr;
	bool bLogEntry = false;	// Flag to say whether a log entry has been begun or attempted.
	int dberr = kdberrGeneral;	// Type of error: default is general database error.
	StrUni stuInfo; // info that becomes part of the error object.
	stuInfo.Format(L"A problem occurred executing the SQL code%n%s%n", m_stuCommand.Chars());
	try
	{
		ComSmartPtr<IErrorInfo> qErrorInfo;
		::GetErrorInfo(0,&qErrorInfo);

		// Get the IErrorRecord interface, and get the count of error recs.
		ComSmartPtr<IErrorRecords> qErrorRecords;
		hr = punk->QueryInterface(IID_IErrorRecords, (void**) &qErrorRecords);
		if (!qErrorRecords)
		{
			// Currently the above approach seems to usually fail, but this one usually works.
			if (qErrorInfo)
				hr = qErrorInfo->QueryInterface(IID_IErrorRecords, (void**) &qErrorRecords);
		}

		if (qErrorRecords)
		{
			// This is generally where we get the really useful information about what's wrong.
			ulong ulNumErrorRecs;
			qErrorRecords->GetRecordCount(&ulNumErrorRecs);

			// Read through error records and display them.
			for (ulong i = 0; i < ulNumErrorRecs; i++)
			{
				// Get basic error information.
				ERRORINFO ErrorInfo;
				qErrorRecords->GetBasicErrorInfo(i, &ErrorInfo);

				// Get error description and source through the
				// IErrorInfo interface pointer on a particular record.
				ComSmartPtr<IErrorInfo> qErrorInfoRec;
				// Default locale does not seem to work, try hard-coding American English.
				DWORD locale = 0x0409;
				// MAKELCID(MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), SORT_DEFAULT);
				// Review JohnT: will 0 work as default locale ID?
				qErrorRecords->GetErrorInfo(i, locale, &qErrorInfoRec);
				SmartBstr sbstrDescriptionOfError;
				SmartBstr sbstrSourceOfError;

				qErrorInfoRec->GetDescription(&sbstrDescriptionOfError);
				qErrorInfoRec->GetSource(&sbstrSourceOfError);

				StrUni stuMsg;
				stuMsg.Format(
					L"%nHRESULT: %x, Minor Code: %u%nSource: %s%nDescription: %s%n",
						ErrorInfo.hrError, ErrorInfo.dwMinor, sbstrSourceOfError.Chars(),
						sbstrDescriptionOfError.Chars());
				StrAnsi staMsg(stuMsg);
				OutputDebugStringA(staMsg.Chars());	// Output to debug window...
				if (m_qfistLog)
				{	// ...and to log.
					m_qfistLog->Write(staMsg.Chars(), (ULONG)staMsg.Length(), NULL);
				}
				stuInfo.Append(stuMsg);
				bLogEntry = true;

				// Now see if there's any SQL Server-specific information.
				ComSmartPtr<ISQLErrorInfo> qsqlei;
				hr = qErrorRecords->GetCustomErrorObject(i,
					IID_ISQLErrorInfo, (IUnknown **) &qsqlei);
				if (SUCCEEDED(hr))
				{
					SmartBstr sbstrSQLState;
					long nNativeError;
					hr = qsqlei->GetSQLInfo(&sbstrSQLState, &nNativeError);
					{
						if (sbstrSQLState.Equals(L"08S01"))
							dberr = kdberrCommLink;	// Probably network failure.
						else if (sbstrSQLState.Equals(L"22001"))
							dberr = kdberrTruncate;	// Not enough room for string.
						stuMsg.Format(L"Information from ISQLErrorInfo:%nSQL Server Error: %d%nSQLSTATE: %s%n",
							nNativeError, sbstrSQLState.Chars());
						if (m_qfistLog)
						{
							staMsg.Assign(stuMsg.Chars());
							m_qfistLog->Write(staMsg.Chars(), (ULONG)staMsg.Length(), NULL);
						}
						stuInfo.Append(stuMsg);
					}
				}

				// Now try to use the ISQLServerErrorInfo interface to obtain further
				// SQL Server-specific error information.
				ComSmartPtr<ISQLServerErrorInfo> qsqlsei;
				hr = qErrorRecords->GetCustomErrorObject(i, IID_ISQLServerErrorInfo,
					(IUnknown **) &qsqlsei);
				if (SUCCEEDED(hr))
				{
					SSERRORINFO * pSSErrorInfo = NULL;
					OLECHAR * pszwSSErrorStrings = NULL;
					hr = qsqlsei->GetErrorInfo(&pSSErrorInfo, &pszwSSErrorStrings);
					{
						if (pSSErrorInfo)
						{
							int nState = (*pSSErrorInfo).bState;
							if (dberr == kdberrGeneral)
							{	// If dberr is still at default then discern Severity.
								switch (nState)
								{
								case 17:
									dberr = kdberrResources;
									break;
								case 22:
								case 23:
									dberr = kdberrIntegrity;
									break;
								case 24:
									dberr = kdberrHardware;
									break;
								default:
									break;
								}
							}
							int nClass = (*pSSErrorInfo).bClass;
							stuMsg.Format(L"Additional information from ISQLServerErrorInfo:%n"
								L"State: %d%nSeverity: %d%n"
								L"Server Instance: %s%n"
								L"Stored Procedure: %s (Line %d)%n",
								nState, nClass, (*pSSErrorInfo).pwszServer,
								(*pSSErrorInfo).pwszProcedure,
								(*pSSErrorInfo).wLineNumber);
							if (m_qfistLog)
							{
								staMsg.Assign(stuMsg.Chars());
								m_qfistLog->Write(staMsg.Chars(), (ULONG)staMsg.Length(),
									NULL);
							}
							CoTaskMemFree(pSSErrorInfo);
							stuInfo.Append(stuMsg);
						}
						if (pszwSSErrorStrings)	// Contain same as 'Description' above.
							CoTaskMemFree(pszwSSErrorStrings);
					}
				}
			}
		}
	}
	catch(...)
	{
		// If anything goes wrong trying to obtain error info, just return the original info.
	}
	if (FAILED(hrErr))
	{
		// Log a stack dump on all failures except DB_E_ABORTLIMITREACHED: we hardly have any!
		// The exception occurs when there is a locking conflict, and where we are in the
		// code is therefore somewhat arbitrary.
		if (hrErr != DB_E_ABORTLIMITREACHED)
		{
			DumpStackHere("Stack Dump:\r\n");
			if (m_qfistLog)
			{
				StrAnsi sta;
				if (!bLogEntry)
					sta.Format("%nHRESULT: %x  ", hrErr);	// Only if no logging from above.
				sta.Append(StackDumper::GetDump());
				m_qfistLog->Write(sta.Chars(), (ULONG)sta.Length(), NULL);
			}
			if (!bLogEntry)
				stuInfo.FormatAppend(L"%nHRESULT: %x  ", hrErr);	// Only if no logging from above.
			stuInfo.Append(StackDumper::GetDump());
		}
		else
		{
			dberr = kdberrLockTimeout;
		}
		IErrorInfoPtr qei;
		hr = SetUpFullErrorInfo(dberr, stuInfo.Chars(), &qei);
		if (SUCCEEDED(hr))
		{
			SmartBstr descr;
			if (qei)
				qei->GetDescription(&descr);
			ThrowHr(hrErr, descr ? descr.Chars() : NULL, -1, qei);	// An error object exists.
		}
		else
			ThrowHr(hrErr);
	}
	return hrErr;
}


/*----------------------------------------------------------------------------------------------
	Creates an error object and then sets a description from a resource id. Also sets a full
	help URL as required by HtmlHelp. Uses ierr as an index for both resource id and help URL.
	@param ierr Index to a set of htm help files (second part of full help URL) and matching
	resource strings for Message Box text.
	@param pei [out] Error info object
----------------------------------------------------------------------------------------------*/
HRESULT OleDbCommand::SetUpFullErrorInfo(int ierr, const OLECHAR * pszInfo, IErrorInfo ** ppei)
{
	ICreateErrorInfoPtr qcei;
	HRESULT hr;
	int ridDescription;

	// Create error info object.
	hr = CreateErrorInfo(&qcei);
	if (FAILED(hr))
		return hr;

	// Set the full help file path and textual description.
	StrUni stuPath(DirectoryFinder::FwRootCodeDir().Chars());
	if (stuPath[stuPath.Length() - 1] != '\\')
	{
		stuPath.Append(_T("\\"));
	}
	stuPath.Append("Helps\\FieldWorks_Errors.chm");

	StrUni stuPath2;
	switch (ierr) {
	case kdberrGeneral:
		stuPath2 = L"Database_Errors/Database_general_error.htm";
		ridDescription = kstidDatabaseGeneral;
		break;
	case kdberrCommLink:
		stuPath2 = L"Database_Errors/Database_network_error.htm";
		ridDescription = kstidDatabaseCommLink;
		break;
	case kdberrResources:
		stuPath2 = L"Database_Errors/Database_resource_error.htm";
		ridDescription = kstidDatabaseResources;
		break;
	case kdberrIntegrity:
		stuPath2 = L"Database_Errors/Database_integrity_error.htm";
		ridDescription = kstidDatabaseIntegrity;
		break;
	case kdberrHardware:
		stuPath2 = L"Database_Errors/Database_hardware_error.htm";
		ridDescription = kstidDatabaseHardware;
		break;
	case kdberrTruncate:
		stuPath2 = L"Database_Errors/Database_text_length_error.htm";
		ridDescription = kstidDatabaseTruncate;
		break;
	case kdberrLockTimeout:
		stuPath2 = L"Database_Errors/Database_general_error.htm";	// For the moment...
		ridDescription = kstidDatabaseLockTim;	//...Should not get this one in Notebook.
		break;
	default:
		stuPath2 = L"Database_Errors/Database_general_error.htm";
		ridDescription = kstidDatabaseGeneral;
		break;
	}

	StrUni stu(ridDescription);
	if (pszInfo && *pszInfo)
	{
		stu.FormatAppend(L"%n%nFurther details:%n");
		stu.Append(pszInfo);
	}
	hr = qcei->SetDescription((wchar *)stu.Chars());

	stuPath.Append(L"::/");
	stuPath.Append(stuPath2);

	hr = qcei->SetHelpFile(const_cast<OLECHAR *>(stuPath.Chars()));

	// Now get the IErrorInfo interface of the error object and set it for the current thread.
	hr = qcei->QueryInterface(IID_IErrorInfo, (void **)ppei);
	if (FAILED(hr))
		return hr;
	SetErrorInfo(0, *ppei);

	return hr;
}

// Method to wrap Execute commands to catch hr == DB_E_ABORTLIMITREACHED.
HRESULT OleDbCommand::Execute1(REFIID IID_Interface, DBPARAMS * pParams, DBROWCOUNT * pcRows,
					   IUnknown ** ppunk, const ICommandTextPtr& qcdt)
{
	HRESULT hr;
	do
	{
		hr = qcdt->Execute(NULL, IID_Interface, pParams, pcRows, ppunk);
		// operation time out
		if (hr == DB_E_ABORTLIMITREACHED)
		{
			Assert(m_oltCmd != koltNone);	// If no timeout set, shouldn't get this error.
			if (m_oltCmd == koltMsgBox)
			{
				if (::MessageBox(NULL, _T("A database operation has timed out. This can be due to two FieldWorks programs interfering with each other, accessing the database over a slow connection, other networked users trying to work on related data, or running the Language Explorer parser in the background.  Sometimes it can be prevented by doing a problematic task a little more slowly. It may help to exit from other FieldWorks programs before clicking OK.\n\n")
					_T("Click OK to try again, which will often resolve the problem, especially if you can stop other programs using the database.\n")
					_T("Click Cancel to give up trying; this unfortunately leads to a program crash, which you are invited to report. Only the change in progress will be lost.\n"),
					_T("Locked"), MB_TASKMODAL | MB_OKCANCEL) == IDCANCEL)
					return DB_E_ABORTLIMITREACHED; // Give up retrying; program will typically crash
			}
			else
			{
				return DB_E_ABORTLIMITREACHED;	// Return the error to the caller.
			}
		}
	}
	while (hr == DB_E_ABORTLIMITREACHED);
	if(hr == S_OK){
		// Stored procedure executes can return info in the IErrorInfo when S_OK.
		// This messes up the CheckHr asserts in Throwable.
		CleanUpErrorInfo();
	}
	return hr;
}

//:>********************************************************************************************
//:>	FwMetaDataCache - Constructor/Destructor
//:>********************************************************************************************

FwMetaDataCache::FwMetaDataCache()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}


FwMetaDataCache::~FwMetaDataCache()
{
	ModuleEntry::ModuleRelease();
}


//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factmdc(
	_T("SIL.DbAccess.FwMetaDataCache"),
	&CLSID_FwMetaDataCache,
	_T("SIL MetaData Cache"),
	_T("Apartment"),
	&FwMetaDataCache::CreateCom);


void FwMetaDataCache::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
	{
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	}
	ComSmartPtr<FwMetaDataCache> qzode;
	qzode.Attach(NewObj FwMetaDataCache());		// ref count initially 1
	CheckHr(qzode->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	FwMetaDataCache - IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP FwMetaDataCache::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_IFwMetaDataCache)
		*ppv = static_cast<IFwMetaDataCache *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IFwMetaDataCache);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


STDMETHODIMP_(ULONG) FwMetaDataCache::AddRef(void)
{
	Assert(m_cref > 0);
	::InterlockedIncrement(&m_cref);
	return m_cref;
}


STDMETHODIMP_(ULONG) FwMetaDataCache::Release(void)
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
	${IFwMetaDataCache#FieldCount}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::get_FieldCount(int * pcflid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcflid);

	*pcflid = m_hmmfr.Size();

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetFieldIds}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetFieldIds(int cflid, ULONG * rgflid)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(rgflid, cflid);

	HashMap<ULONG, FwMetaFieldRec>::iterator it;
	int iflid;
	for (iflid = 0, it = m_hmmfr.Begin(); it != m_hmmfr.End(); ++it, ++iflid)
	{
		if (iflid >= cflid)
			break;
		rgflid[iflid] = it.GetKey();
	}
	for (; iflid < cflid; ++iflid)
		rgflid[iflid] = 0;

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}


/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetOwnClsName}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetOwnClsName(ULONG luFlid, BSTR * pbstrOwnClsName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrOwnClsName);

	// We do not accept 0 as valid.
	if (!luFlid)
	{
		*pbstrOwnClsName = NULL;
		ThrowHr(E_INVALIDARG, L"Unknown flid");
	}

	FwMetaFieldRec mfr;
	FwMetaClassRec mcr;
	if (!m_hmmfr.Retrieve(luFlid, &mfr))
		ReturnHr(E_INVALIDARG);

	if (m_hmmcr.Retrieve(mfr.m_luOwnClsid, &mcr))
	{
		mcr.m_stuClassName.GetBstr(pbstrOwnClsName);
	}
	else
	{
		ThrowHr(E_UNEXPECTED);
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}


/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetDstClsName}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetDstClsName(ULONG luFlid, BSTR * pbstrDstClsName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrDstClsName);

	// We do not accept 0 as valid.
	if (!luFlid)
	{
		*pbstrDstClsName = NULL;
		ThrowHr(E_INVALIDARG, L"Unknown flid");
	}

	FwMetaFieldRec mfr;
	FwMetaClassRec mcr;
	if (!m_hmmfr.Retrieve(luFlid, &mfr))
		ReturnHr(E_INVALIDARG);

	if (m_hmmcr.Retrieve(mfr.m_luDstClsid, &mcr))
	{
		mcr.m_stuClassName.GetBstr(pbstrDstClsName);
	}
	else
	{
		ThrowHr(E_UNEXPECTED);
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}


/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetOwnClsId}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetOwnClsId(ULONG luFlid, ULONG * pluOwnClsid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pluOwnClsid);

	// We do not accept 0 as valid.
	if (!luFlid)
	{
		*pluOwnClsid = 0;
		ThrowHr(E_INVALIDARG, L"Unknown flid");
	}

	FwMetaFieldRec mfr;
	if (m_hmmfr.Retrieve(luFlid, &mfr))
	{
		*pluOwnClsid = mfr.m_luOwnClsid;
	}
	else
	{
		ReturnHr(E_INVALIDARG);
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}


/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetOwnClsId}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetDstClsId(ULONG luFlid, ULONG * pluDstClsid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pluDstClsid);

	// We do not accept 0 as valid.
	if (!luFlid)
	{
		*pluDstClsid = 0;
		ThrowHr(E_INVALIDARG, L"Unknown flid");
	}

	FwMetaFieldRec mfr;
	if (m_hmmfr.Retrieve(luFlid, &mfr))
	{
		*pluDstClsid = mfr.m_luDstClsid;
	}
	else
	{
		ReturnHr(E_INVALIDARG);
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetFieldName}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetFieldNameOrNull(ULONG luFlid, BSTR * pbstrFieldName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrFieldName);

	// We do not accept 0 as valid.
	if (!luFlid)
	{
		*pbstrFieldName = NULL;
		ThrowHr(E_INVALIDARG, L"Unknown flid");
	}

	FwMetaFieldRec mfr;
	if (m_hmmfr.Retrieve(luFlid, &mfr))
	{
		mfr.m_stuFieldName.GetBstr(pbstrFieldName);
	}
	// otherwise just leave null, from ChkComOutPtr.

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetFieldName}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetFieldName(ULONG luFlid, BSTR * pbstrFieldName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrFieldName);

	// We do not accept 0 as valid.
	if (!luFlid)
	{
		*pbstrFieldName = NULL;
		ThrowHr(E_INVALIDARG, L"Unknown flid");
	}

	FwMetaFieldRec mfr;
	if (m_hmmfr.Retrieve(luFlid, &mfr))
	{
		mfr.m_stuFieldName.GetBstr(pbstrFieldName);
	}
	else
	{
		//ReturnHr(E_INVALIDARG);
		// This produces much less garbage in the output window!
		throw ThrowableSd(E_INVALIDARG, L"field not found", 0, " ");
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}
/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetFieldLabel}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetFieldLabel(ULONG luFlid, BSTR * pbstrFieldLabel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrFieldLabel);

	// We do not accept 0 as valid.
	if (!luFlid)
	{
		*pbstrFieldLabel = NULL;
		ThrowHr(E_INVALIDARG, L"Unknown flid");
	}

	FwMetaFieldRec mfr;
	if (m_hmmfr.Retrieve(luFlid, &mfr))
	{
		mfr.m_stuFieldLabel.GetBstr(pbstrFieldLabel);
	}
	else
	{
		ReturnHr(E_INVALIDARG);
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}
/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetFieldHelp}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetFieldHelp(ULONG luFlid, BSTR * pbstrFieldHelp)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrFieldHelp);

	// We do not accept 0 as valid.
	if (!luFlid)
	{
		*pbstrFieldHelp = NULL;
		ThrowHr(E_INVALIDARG, L"Unknown flid");
	}

	FwMetaFieldRec mfr;
	if (m_hmmfr.Retrieve(luFlid, &mfr))
	{
		mfr.m_stuFieldHelp.GetBstr(pbstrFieldHelp);
	}
	else
	{
		ReturnHr(E_INVALIDARG);
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}
/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetFieldXml}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetFieldXml(ULONG luFlid, BSTR * pbstrFieldXml)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrFieldXml);

	// We do not accept 0 as valid.
	if (!luFlid)
	{
		*pbstrFieldXml = NULL;
		ThrowHr(E_INVALIDARG, L"Unknown flid");
	}

	FwMetaFieldRec mfr;
	if (m_hmmfr.Retrieve(luFlid, &mfr))
	{
		mfr.m_stuFieldXml.GetBstr(pbstrFieldXml);
	}
	else
	{
		ReturnHr(E_INVALIDARG);
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetFieldListRoot}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetFieldListRoot(ULONG luFlid, int * piListRoot)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(piListRoot);

	// We do not accept 0 as valid.
	if (!luFlid)
	{
		*piListRoot = 0;
		ThrowHr(E_INVALIDARG, L"Unknown flid");
	}

	FwMetaFieldRec mfr;
	if (m_hmmfr.Retrieve(luFlid, &mfr))
	{
		*piListRoot = mfr.m_iFieldListRoot;
	}
	else
	{
		ReturnHr(E_INVALIDARG);
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetFieldWs}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetFieldWs(ULONG luFlid, int * piWs)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(piWs);

	// We do not accept 0 as valid.
	if (!luFlid)
	{
		*piWs = 0;
		ThrowHr(E_INVALIDARG, L"Unknown flid");
	}

	FwMetaFieldRec mfr;
	if (m_hmmfr.Retrieve(luFlid, &mfr))
	{
		*piWs = mfr.m_iFieldWs;
	}
	else
	{
		ReturnHr(E_INVALIDARG);
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetFieldType}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetFieldType(ULONG luFlid, int * piType)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(piType);

	// We do not accept 0 as valid.
	if (!luFlid)
	{
		*piType = 0;
		ThrowHr(E_INVALIDARG, L"Unknown flid");
	}

	FwMetaFieldRec mfr;
	if (m_hmmfr.Retrieve(luFlid, &mfr))
	{
		*piType = mfr.m_iType & 0x1f; // strip virtual bit
	}
	else if (luFlid >= 1000000000) // kflidStartDummyFlids = 1000000000,
	{
		// It's a dummy--return nil (kcptNil).
		*piType = 0;
	}
	else
	{
		// For this one method, allow a simple 0 result, not an exception,
		// to make it easier to check for valid fields.
		*piType = 0;
		//ReturnHr(E_INVALIDARG);
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}


/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#IsValidClass}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::get_IsValidClass(ULONG luFlid, ULONG luClid, ComBool * pfValid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfValid);

	// We do not accept 0 as valid.
	if (!luFlid)
		ThrowHr(E_INVALIDARG, L"Unknown flid");

	FwMetaFieldRec mfr;
	if (!m_hmmfr.Retrieve(luFlid, &mfr))
		ReturnHr(E_INVALIDARG);

	if (mfr.m_luDstClsid == (ULONG)-1)
		return S_OK; // No destination class.

	// If luFlid can hold luClid, return true;
	if (mfr.m_luDstClsid == luClid)
	{
		*pfValid = true;
		return S_OK;
	}

	// Otherwise we need to see if it can hold any of the superclasses of luClid.
	FwMetaClassRec mcr;
	do
	{
		if (!m_hmmcr.Retrieve(luClid, &mcr))
			ThrowHr(WarnHr(E_UNEXPECTED));
		luClid = mcr.m_luBaseClsid;
		if (mfr.m_luDstClsid == luClid)
		{
			*pfValid = true;
			return S_OK;
		}
	} while (luClid != 0);

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}


/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#ClassCount}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::get_ClassCount(int * pcclid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcclid);

	*pcclid = m_hmmcr.Size();

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetClassIds}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetClassIds(int cclid, ULONG * rgclid)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(rgclid, cclid);

	HashMap<ULONG, FwMetaClassRec>::iterator it;
	int iclid;
	for (iclid = 0, it = m_hmmcr.Begin(); it != m_hmmcr.End(); ++it, ++iclid)
	{
		if (iclid >= cclid)
			break;
		rgclid[iclid] = it.GetKey();
	}
	for (; iclid < cclid; ++iclid)
		rgclid[iclid] = 0;

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetClassName}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetClassName(ULONG luClid, BSTR * pbstrClassName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrClassName);

	FwMetaClassRec mcr;

	if (m_hmmcr.Retrieve(luClid, &mcr))
	{
		mcr.m_stuClassName.GetBstr(pbstrClassName);
	}
	else
	{
		ReturnHr(E_INVALIDARG);
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}


/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetAbstract}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetAbstract(ULONG luClid, ComBool * pfAbstract)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfAbstract);

	FwMetaClassRec mcr;
	if (m_hmmcr.Retrieve(luClid, &mcr))
	{
		*pfAbstract = mcr.m_fAbstract;
	}
	else
	{
		ReturnHr(E_INVALIDARG);
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}


/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetBaseClsId}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetBaseClsId(ULONG luClid, ULONG * pluClid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pluClid);

	FwMetaClassRec mcr;
	if (m_hmmcr.Retrieve(luClid, &mcr))
	{
		*pluClid = mcr.m_luBaseClsid;
	}
	else
	{
		ReturnHr(E_INVALIDARG);
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}


/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#GetBaseClsName}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetBaseClsName(ULONG luClid, BSTR * pbstrBaseClsName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrBaseClsName);

	FwMetaClassRec mcr;
	if (!m_hmmcr.Retrieve(luClid, &mcr))
		ReturnHr(E_INVALIDARG);

	FwMetaClassRec mcrT;
	if (m_hmmcr.Retrieve(mcr.m_luBaseClsid, &mcrT))
	{
		mcrT.m_stuClassName.GetBstr(pbstrBaseClsName);
	}
	else
	{
		ThrowHr(WarnHr(E_UNEXPECTED));
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

// Make a key as used in m_hmsulNameToFlid from the clid and field name
StrUni MakeFlidKey(ULONG clid, BSTR bstrFieldName)
{
	OLECHAR rgchT[3];
	rgchT[0] = (OLECHAR) (clid >> 16);
	rgchT[1] = (OLECHAR) (clid & 0xffff);
	rgchT[2] = 0; // null termination
	StrUni stuResult(rgchT, 2);
	stuResult += bstrFieldName;
	return stuResult;
}

/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#Init}
	Note, this method can be called more than once. This allows FullRefresh to reload the
	cache in case any changes were made (e.g. custom fields added).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::Init(IOleDbEncap * pode)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pode);

	ComBool fIsNull;
	ComBool fMoreRows;
	HRESULT hr;
	ULONG luFlid;
	ULONG luSpaceTaken;
	OLECHAR rgchName[MAX_PATH];
	OLECHAR rgchLabel[MAX_PATH];
	OLECHAR rgchHelp[MAX_PATH];
	int nRootId;
	int nWS;
	OLECHAR rgchXml[MAX_PATH];
	ComBool fAbstract;
	ULONG luClid;
	ULONG luBaseClsid;
	IOleDbCommandPtr qodc;
	StrUni stu;
	ULONG nType;

	// Clear the hashmaps in case we are reinitializing.
	m_hmmfr.Clear();
	m_hmmcr.Clear();
	m_hmsulNameToClid.Clear();
	m_hmsulNameToFlid.Clear();

	// Retain the pointer to the IOleDbEncap interface.
	m_qode = pode;

	// NB: read class info first because when reading field info, we load it into records
	// for the corresponding class.
	/*  SQL statement to get meta class information.
		Id          Base        Abstract Name
		----------- ----------- -------- --------------------
		0           0           1        CmObject
		1           0           1        CmProject
		2           0           0        CmFolder
		3           0           0        CmFolderObject
		5           0           1        CmMajorObject
		7           0           0        CmPossibility
	*/
	stu = L"select Id, Base, Abstract, Name from class$";
	CheckHr(m_qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		FwMetaClassRec mcr;
		CheckHr(qodc->GetColValue(1, (BYTE*)&luClid, sizeof(ULONG), &luSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(2, (BYTE*)&luBaseClsid, sizeof(ULONG), &luSpaceTaken, &fIsNull, 0));
		mcr.m_luBaseClsid = luBaseClsid;
		CheckHr(qodc->GetColValue(3, (BYTE*)&fAbstract,
			isizeof(ComBool), &luSpaceTaken, &fIsNull, 0));
		mcr.m_fAbstract = fAbstract;
		CheckHr(qodc->GetColValue(4, (BYTE*)rgchName, isizeof(rgchName), &luSpaceTaken,
			&fIsNull, 2));
		mcr.m_stuClassName = rgchName;
		m_hmmcr.Insert(luClid, mcr, true);
		m_hmsulNameToClid.Insert(mcr.m_stuClassName, luClid);
		CheckHr(qodc->NextRow(&fMoreRows));
	}

	BuildSubClassInfo();

		/*  SQL statement to get meta field information.
		Id          Type        Class       DstCls      Name       UserLabel  HelpString  ListRootId  WsSelector  XmlUI
		----------- ----------- ----------- ----------- ---------- ---------  ----------  ----------  ----------  -----
		1001        16          1           NULL        Name
		1002        5           1           NULL        TimeCreated
		1003        25          1           2           Folders
		2001        16          2           NULL        Name
	*/
	stu = L"select id, type, class, dstcls, name, userlabel, helpstring, listrootid, wsselector, XmlUI from field$ order by class";
	CheckHr(hr = m_qode->CreateCommand(&qodc));
	CheckHr(hr = qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(hr = qodc->GetRowset(0));
	CheckHr(hr = qodc->NextRow(&fMoreRows));
	ULONG clidCurrent = 0;
	ULONG luDstClsid = 0;
	FldIdVec * pvuFields = NULL;
	while (fMoreRows)
	{
		FwMetaFieldRec mfr;
		CheckHr(qodc->GetColValue(1, (BYTE*)&luFlid, sizeof(ULONG), &luSpaceTaken, &fIsNull, 0));
		CheckHr(qodc->GetColValue(2, (BYTE*)&nType, sizeof(ULONG), &luSpaceTaken, &fIsNull, 0));
		mfr.m_iType = nType;
		CheckHr(qodc->GetColValue(3, (BYTE*)&luClid, sizeof(ULONG), &luSpaceTaken, &fIsNull, 0));
		mfr.m_luOwnClsid = luClid;
		CheckHr(qodc->GetColValue(4, (BYTE*)&luDstClsid, sizeof(ULONG), &luSpaceTaken, &fIsNull, 0));
		// If DstCls is null, set its value to -1.
		mfr.m_luDstClsid = fIsNull ? (ULONG)-1 : luDstClsid;
		CheckHr(qodc->GetColValue(5, (BYTE*)rgchName, isizeof(rgchName), &luSpaceTaken,
			&fIsNull, 2));
		mfr.m_stuFieldName = rgchName;
		CheckHr(qodc->GetColValue(6, (BYTE*)rgchLabel, isizeof(rgchLabel), &luSpaceTaken,
			&fIsNull, 2));
		mfr.m_stuFieldLabel = rgchLabel;
		CheckHr(qodc->GetColValue(7, (BYTE*)rgchHelp, isizeof(rgchHelp), &luSpaceTaken,
			&fIsNull, 2));
		mfr.m_stuFieldHelp = rgchHelp;
		CheckHr(qodc->GetColValue(8, (BYTE*)&nRootId, sizeof(int), &luSpaceTaken, &fIsNull, 0));
		mfr.m_iFieldListRoot = nRootId;
		CheckHr(qodc->GetColValue(9, (BYTE*)&nWS, sizeof(int), &luSpaceTaken, &fIsNull, 0));
		mfr.m_iFieldWs = nWS;
		CheckHr(qodc->GetColValue(10, (BYTE*)rgchXml, isizeof(rgchXml), &luSpaceTaken,
			&fIsNull, 2));
		mfr.m_stuFieldXml = rgchXml;
		m_hmmfr.Insert(luFlid, mfr, true);
		StrUni stuKey = MakeFlidKey(luClid, mfr.m_stuFieldName.Bstr());
		m_hmsulNameToFlid.Insert(stuKey, luFlid);

		// There can be any number of fields per class,
		// but we need a new pvuFields when we switch to a new class.
		if (clidCurrent != luClid)
		{
			FwMetaClassRec mcr;
			if (!m_hmmcr.Retrieve(luClid, &mcr))
			{
				pvuFields = NULL;
				Assert(false);
			}
			else
			{
				if (!mcr.m_qmfl)
				{
					mcr.m_qmfl.Attach(NewObj FwMetaFieldList);
					// Write it back to the map with the new metafieldlist.
					m_hmmcr.Insert(luClid, mcr, true);
					pvuFields = &mcr.m_qmfl->m_vuFields;
					if (luClid == 0)
					{
						// Add CmObject fields.
						pvuFields->Push(101);	// kflidCmObject_Guid
						pvuFields->Push(102);	// kflidCmObject_Class
						pvuFields->Push(103);	// kflidCmObject_Owner
						pvuFields->Push(104);	// kflidCmObject_OwnFlid
						pvuFields->Push(105);	// kflidCmObject_OwnOrd
						//pvuFields->Push(106);	// kflidCmObject_UpdStmp
						//pvuFields->Push(107);	// kflidCmObject_UpdDttm
					}
				}
				else
					pvuFields = &mcr.m_qmfl->m_vuFields;
			}
		}
		clidCurrent = luClid;
		if (pvuFields)
			pvuFields->Push(luFlid);
		qodc->NextRow(&fMoreRows);
	}

	InsertCmObjectFields();
//	mfr.m_stuFieldName = "UpdStmp";
//	luFlid = 106; /*kflidCmObject_UpdStmp*/
//	m_hmmfr.Insert(luFlid, mfr, true);

//	mfr.m_stuFieldName = "UpdDttm";
//	luFlid = 107; /*kflidCmObject_UpdDttm*/
//	m_hmmfr.Insert(luFlid, mfr, true);

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

void FwMetaDataCache::InsertCmObjectFields()
{
	// Add CmObject columns since these values are not included in the field$ table.
	FwMetaFieldRec mfr;
	mfr.m_luOwnClsid = 0;
	mfr.m_luDstClsid = 0;
	ULONG luFlid;

	mfr.m_stuFieldName = "Guid$";
	mfr.m_iType = kcptGuid;
	luFlid = 101; /*kflidCmObject_Guid*/
	m_hmmfr.Insert(luFlid, mfr, true);

	mfr.m_stuFieldName = "Class$";
	mfr.m_iType = kcptInteger;
	luFlid = 102; /*kflidCmObject_Class*/
	m_hmmfr.Insert(luFlid, mfr, true);

	mfr.m_stuFieldName = "Owner$";
	mfr.m_iType = kcptReferenceAtom;
	luFlid = 103; /*kflidCmObject_Owner*/
	m_hmmfr.Insert(luFlid, mfr, true);

	mfr.m_stuFieldName = "OwnFlid$";
	mfr.m_iType = kcptInteger;
	luFlid = 104; /*kflidCmObject_OwnFlid*/
	m_hmmfr.Insert(luFlid, mfr, true);

	mfr.m_stuFieldName = "OwnOrd$";
	mfr.m_iType = kcptInteger;
	luFlid = 105; /*kflidCmObject_OwnOrd*/
	m_hmmfr.Insert(luFlid, mfr, true);
}

// After loading all the main class info, fill in the m_vuDirectSubclasses member
// by looping over the loaded classes.
void FwMetaDataCache::BuildSubClassInfo()
{
	// Make this a separate loop as it is not certain that the above SQL will load base classes
	// before superclasses.
	HashMap<ULONG, FwMetaClassRec>::iterator it = m_hmmcr.Begin();
	for ( ; it != m_hmmcr.End(); ++it)
	{
		ULONG clsIdChild = it.GetKey();
		ULONG clsIdBase = it.GetValue().m_luBaseClsid;
		if (clsIdBase == clsIdChild)
			continue; // CmObject lists itself as is own baseclass, so don't add it to the subclass list.
		FwMetaClassRec mcr;
		if (!m_hmmcr.Retrieve(clsIdBase, &mcr))
			continue; // probably we're dealing with CmObject; or something is wrong...
		mcr.m_vuDirectSubclasses.Push(clsIdChild);
		// Write it back to the map with the modified vector.
		// (Because we are working with a copy of the mcr, otherwise, we only modified the
		// copy and the change will be lost.)
		m_hmmcr.Insert(clsIdBase, mcr, true);
	}
}

const XML_Char * GetAttributeValue(const XML_Char ** prgpszAtts, const char * pszName)
{
	if (!prgpszAtts)
		return NULL;
	for (int i = 0; prgpszAtts[i]; i += 2)
	{
		if (strcmp(prgpszAtts[i], pszName) == 0)
			return prgpszAtts[i+1];
	}
	return NULL;
}

struct ParseData
{
	HashMap<ULONG, FwMetaFieldRec> & m_hmmfr;
	HashMap<ULONG, FwMetaClassRec> & m_hmmcr;
	int m_moduleNumber;
	HashMapStrUni<ULONG> & m_hmsulNameToClid;
	HashMapStrUni<ULONG> & m_hmsulNameToFlid; // First two chars of name are binary Clid
	ULONG m_clsid; // most recent <class> element id, presumed class of prop elements.
	FldIdVec * pvuFields;

	ParseData(HashMap<ULONG, FwMetaFieldRec> & hmmfr,
		HashMap<ULONG, FwMetaClassRec> & hmmcr,
		HashMapStrUni<ULONG> & hmsulNameToClid,
		HashMapStrUni<ULONG> & hmsulNameToFlid) :
			m_hmmfr(hmmfr), m_hmmcr(hmmcr), m_hmsulNameToClid(hmsulNameToClid),
			m_hmsulNameToFlid(hmsulNameToFlid)
	{
		pvuFields = NULL;
	}
};

ULONG GetCommonPropInfo(const XML_Char ** prgpszAtts, ParseData * pd, FwMetaFieldRec & mfr)
{
	mfr.m_stuFieldName = GetAttributeValue(prgpszAtts, "id");
	mfr.m_luOwnClsid = pd->m_clsid;
	mfr.m_stuFieldLabel = GetAttributeValue(prgpszAtts, "label");
	return atoi(GetAttributeValue(prgpszAtts, "num")) + pd->m_clsid*1000;
}

int GetCardinality(const XML_Char ** prgpszAtts)
{
	const XML_Char * pszCard = GetAttributeValue(prgpszAtts, "card");
	if (strcmp("col", pszCard) == 0)
		return kcptOwningCollection - kcptOwningAtom;
	else if (strcmp("seq", pszCard) == 0)
		return kcptOwningSequence - kcptOwningAtom;
	else return 0; // assume atomic
}

void HandleStartTagClass(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	ParseData * pd = reinterpret_cast<ParseData *>(pvUser);
	if (strcmp(pszName, "CellarModule") == 0)
	{
		pd->m_moduleNumber = atoi(GetAttributeValue(prgpszAtts, "num"));
	}
	else if (strcmp(pszName, "class") == 0)
	{
		//if a class id of 0 is found in the xml it is a forward declaration for the xslt
		//we need to ignore it so it doesn't overwrite the real information
		ULONG clsid = atoi(GetAttributeValue(prgpszAtts, "num"));
		if (clsid == 0 && pd->m_moduleNumber != 0)
			return;
		pd->m_clsid = clsid + pd->m_moduleNumber * 1000;

		FwMetaClassRec mcr;
		mcr.m_stuClassName = GetAttributeValue(prgpszAtts, "id");
		mcr.m_fAbstract = strcmp("true", GetAttributeValue(prgpszAtts, "abstract")) == 0;
		StrUni stuBaseClass = GetAttributeValue(prgpszAtts, "base");

		if(!pd->m_hmsulNameToClid.Retrieve(stuBaseClass, &mcr.m_luBaseClsid))
			mcr.m_luBaseClsid = 0; // CmObject, we hope?
		pd->m_hmmcr.Insert(pd->m_clsid, mcr, true);
		pd->m_hmsulNameToClid.Insert(mcr.m_stuClassName, pd->m_clsid, true);
		return;
	}
}

// When this was written (Aug 3 2006), the code generator (cmcg.exe) was not interpreting a ws attribute
// on basic fields at all. This should serve as a model.
// We may eventually want to handle other constants not yet defined in CmTypes.h,
// using the strings specified in LangProject.InitMagicWsToWsId (C#).
int HandleWs(const XML_Char ** prgpszAtts)
{
	const XML_Char * pszWs = GetAttributeValue(prgpszAtts, "ws");
	if (pszWs == NULL)
		return 0;
	if (strcmp("analysis", pszWs) == 0)
		return kwsAnal;
	if (strcmp("vernacular", pszWs) == 0)
		return kwsVern;
	if (strcmp("all analysis", pszWs) == 0)
		return kwsAnals;
	if (strcmp("all vernacular", pszWs) == 0)
		return kwsVerns;
	if (strcmp("analysis vernacular", pszWs) == 0)
		return kwsAnalVerns;
	if (strcmp("vernacular analysis", pszWs) == 0)
		return kwsVernAnals;
	return 0;
}


int SetTypeForSize(int cpt, int cptBig, const XML_Char ** prgpszAtts)
{
	const XML_Char * pszBig = GetAttributeValue(prgpszAtts, "big");
	if (pszBig != NULL && strcmp(pszBig, "true") == 0)
		return cptBig;
	else
		return cpt;
}

void HandleStartTagField(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	FwMetaFieldRec mfr;
	ULONG flid;

	ParseData * pd = reinterpret_cast<ParseData *>(pvUser);
	if (strcmp(pszName, "class") == 0)
	{
		//if a class id of 0 is found in the xml it is a forward declaration for the xslt
		//we need to ignore it so it doesn't overwrite the real information
		ULONG clsid = atoi(GetAttributeValue(prgpszAtts, "num"));
		if (clsid == 0 && pd->m_moduleNumber != 0)
			return;
		pd->m_clsid = clsid + pd->m_moduleNumber * 1000;

		FwMetaClassRec mcr;
		if (!pd->m_hmmcr.Retrieve(pd->m_clsid, &mcr))
			ThrowHr(WarnHr(E_UNEXPECTED));
		mcr.m_qmfl.Attach(NewObj FwMetaFieldList);
		// Write it back to the map with the new metafieldlist.
		pd->m_hmmcr.Insert(pd->m_clsid, mcr, true);
		pd->pvuFields = &mcr.m_qmfl->m_vuFields;
		if (pd->m_clsid == 0)
		{
			// Add CmObject fields.
			pd->pvuFields->Push(101);	// kflidCmObject_Guid
			pd->pvuFields->Push(102);	// kflidCmObject_Class
			pd->pvuFields->Push(103);	// kflidCmObject_Owner
			pd->pvuFields->Push(104);	// kflidCmObject_OwnFlid
			pd->pvuFields->Push(105);	// kflidCmObject_OwnOrd
		}

		return;
	}
	if (strcmp(pszName, "basic") == 0)
	{
		flid = GetCommonPropInfo(prgpszAtts, pd, mfr);
		const XML_Char * pszSig = GetAttributeValue(prgpszAtts, "sig");
		if (strcmp("Boolean", pszSig) == 0)
			mfr.m_iType = kcptBoolean;
		else if (strcmp("Integer", pszSig) == 0)
			mfr.m_iType = kcptInteger;
		else if (strcmp("Time", pszSig) == 0)
			mfr.m_iType = kcptTime;
		else if (strcmp("String", pszSig) == 0)
		{
			mfr.m_iType = SetTypeForSize(kcptString, kcptBigString, prgpszAtts);
		}
		else if (strcmp("MultiString", pszSig) == 0)
		{
			mfr.m_iType = SetTypeForSize(kcptMultiString, kcptMultiBigString, prgpszAtts);
			mfr.m_iFieldWs = HandleWs(prgpszAtts);
		}
		else if (strcmp("Unicode", pszSig) == 0)
		{
			mfr.m_iType = kcptUnicode;
			mfr.m_iType = SetTypeForSize(kcptUnicode, kcptBigUnicode, prgpszAtts);
		}
		else if (strcmp("MultiUnicode", pszSig) == 0)
		{
			mfr.m_iType = SetTypeForSize(kcptMultiUnicode, kcptMultiBigUnicode, prgpszAtts);
			mfr.m_iFieldWs = HandleWs(prgpszAtts);
		}
		else if (strcmp("BigString", pszSig) == 0)
			mfr.m_iType = kcptBigString;
		else if (strcmp("MultiBigString", pszSig) == 0)
		{
			mfr.m_iType = kcptMultiBigString;
			mfr.m_iFieldWs = HandleWs(prgpszAtts);
		}
		else if (strcmp("BigUnicode", pszSig) == 0)
			mfr.m_iType = kcptBigUnicode;
		else if (strcmp("MultiBigUnicode", pszSig) == 0)
		{
			mfr.m_iType = kcptMultiBigUnicode;
			mfr.m_iFieldWs = HandleWs(prgpszAtts);
		}
		else if (strcmp("Guid", pszSig) == 0)
			mfr.m_iType = kcptGuid;
		else if (strcmp("Image", pszSig) == 0)
			mfr.m_iType = kcptImage;
		else if (strcmp("GenDate", pszSig) == 0)
			mfr.m_iType = kcptGenDate;
		else if (strcmp("Binary", pszSig) == 0)
			mfr.m_iType = kcptBinary;
		else if (strcmp("Numeric", pszSig) == 0)
			mfr.m_iType = kcptNumeric;
		else if (strcmp("Float", pszSig) == 0)
			mfr.m_iType = kcptFloat;
	}
	else if (strcmp(pszName, "rel") == 0)
	{
		flid = GetCommonPropInfo(prgpszAtts, pd, mfr);
		const XML_Char * pszSig = GetAttributeValue(prgpszAtts, "sig");
		StrUni stuSig = pszSig;
		pd->m_hmsulNameToClid.Retrieve(stuSig, &mfr.m_luDstClsid);
		mfr.m_iType = kcptReferenceAtom + GetCardinality(prgpszAtts);
	}
	else if (strcmp(pszName, "owning") == 0)
	{
		flid = GetCommonPropInfo(prgpszAtts, pd, mfr);
		const XML_Char * pszSig = GetAttributeValue(prgpszAtts, "sig");
		StrUni stuSig = pszSig;
		pd->m_hmsulNameToClid.Retrieve(stuSig, &mfr.m_luDstClsid);
		mfr.m_iType = kcptOwningAtom + GetCardinality(prgpszAtts);
	}
	else
		return; // not an element we recognize
	pd->m_hmmfr.Insert(flid, mfr);
	if (pd->pvuFields)
		pd->pvuFields->Push(flid);
	StrUni stuKey = MakeFlidKey(pd->m_clsid, mfr.m_stuFieldName.Bstr());
	pd->m_hmsulNameToFlid.Insert(stuKey, flid);

}


#define READ_SIZE 16384

void ParseFile(XML_Parser & parser, BSTR bstrPathname, ParseData *ppd)
{
	IStreamPtr qstrm;
	FileStream::Create(bstrPathname, STGM_READ, &qstrm);
	STATSTG statFile; // get file statistics, particularly total length
	CheckHr(qstrm->Stat(&statFile, STATFLAG_NONAME));
	try
	{
		XML_SetUserData(parser, ppd);

		for (;;)
		{
			void * pBuffer;
			ulong cbRead;

			pBuffer = XML_GetBuffer(parser, READ_SIZE);
			if (!pBuffer)
				ThrowHr(WarnHr(E_OUTOFMEMORY));

			CheckHr(qstrm->Read(pBuffer, READ_SIZE, &cbRead));
			char * pch = (char *)pBuffer;
			if (*pch == 0xFFFFFFEF && *(pch + 1) == 0xFFFFFFBB &&
				*(pch + 2) == 0xFFFFFFBF)
			{
				// We need to skip the UTF marker. I don't think there's any way to adjust
				// the pointer into the buffer, so instead move the contents of the buffer
				// so that what we want is in the expected starting position.
				memmove(pch, pch + 3, cbRead - 3);
				memset(pch + cbRead - 3, 0, 3);
				cbRead -= 3;
			}
			if (!XML_ParseBuffer(parser, cbRead, cbRead == 0))
			{
				// "XML parser detected an XML syntax error"
				//staRes.Load(kstidWpXmlErrMsg023);
				//xid.CreateErrorMessage(staRes);
				break;
			}
			const LARGE_INTEGER libMove = {0,0};
			ULARGE_INTEGER ulibPos;
			CheckHr(qstrm->Seek(libMove, STREAM_SEEK_CUR, &ulibPos));
			if ((ulibPos.HighPart == statFile.cbSize.HighPart) &&
				(ulibPos.LowPart == statFile.cbSize.LowPart))
			{
				// Successfully processed the XML file.
				//Assert(xid.m_celemEnd <= xid.m_celemStart);
				//if (xid.m_celemStart != xid.m_celemEnd)
				//{
				//	// "Error in termination of file"
				//	staRes.Load(kstidWpXmlErrMsg025);
				//	xid.CreateErrorMessage(staRes);
				//}
				break;
			}
		}
		XML_ParserFree(parser);
	}
	catch(...)
	{
		XML_ParserFree(parser);
		throw;
	}
}

/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#InitXml}
	Initialize the cache from an XML representation.
	This is currently only partly implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::InitXml(BSTR bstrPathname, ComBool fClearPrevCache)
{
	BEGIN_COM_METHOD;

	// Clear the hashmaps in case we are reinitializing.
	if (fClearPrevCache)
	{
		m_hmmfr.Clear();
		m_hmmcr.Clear();
		m_hmsulNameToClid.Clear();
		m_hmsulNameToFlid.Clear();
	}

	ParseData pd(m_hmmfr, m_hmmcr, m_hmsulNameToClid, m_hmsulNameToFlid);

	// Parse once with the class handler to get all the class ids we need for signatures.
	XML_Parser parser = XML_ParserCreate(NULL);
	XML_SetElementHandler(parser, HandleStartTagClass, NULL);
	ParseFile(parser, bstrPathname, &pd);

	BuildSubClassInfo();

	// Parse again to get the fields.
	parser = XML_ParserCreate(NULL);
	XML_SetElementHandler(parser, HandleStartTagField, NULL);
	ParseFile(parser, bstrPathname, &pd);

	InsertCmObjectFields();

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	${IFwMetaDataCache#Reload}
	Note, this method can be called more than once. This allows FullRefresh to reload the
	cache in case any changes were made (e.g. custom fields added).  This is what really should
	be called if custom fields are added dynamically in order to preserve any virtual fields
	that the program is using.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::Reload(IOleDbEncap * pode, ComBool fKeepVirtuals)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pode);

	HashMap<ULONG, FwMetaFieldRec> hmmfrVirtual;
	HashMap<ULONG, FwMetaFieldRec>::iterator itmfr;
	HashMapStrUni<ULONG> hmsulToFlidVirtual;
	HashMapStrUni<ULONG>::iterator itsul;
	if (fKeepVirtuals)
	{
		// Save any virtual field information that's been added to the in-memory cache.
		for (itmfr = m_hmmfr.Begin(); itmfr != m_hmmfr.End(); ++itmfr)
		{
			ULONG flid = itmfr.GetKey();
			FwMetaFieldRec mfr = itmfr.GetValue();
			if (mfr.m_iType & kcptVirtual)
				hmmfrVirtual.Insert(flid, mfr);
		}
		for (itsul = m_hmsulNameToFlid.Begin(); itsul != m_hmsulNameToFlid.End(); ++itsul)
		{
			StrUni stu = itsul.GetKey();
			ULONG flid = itsul.GetValue();
			FwMetaFieldRec mfr;
			if (hmmfrVirtual.Retrieve(flid, &mfr))
				hmsulToFlidVirtual.Insert(stu, flid);
		}
	}

	// Reinitialize the in-memory cache from the database.
	CheckHr(Init(pode));

	if (fKeepVirtuals)
	{
		// Restore the virtual field information to the in-memory cache.
		for (itmfr = hmmfrVirtual.Begin(); itmfr != hmmfrVirtual.End(); ++itmfr)
		{
			ULONG flid = itmfr.GetKey();
			FwMetaFieldRec mfr = itmfr.GetValue();
			m_hmmfr.Insert(flid, mfr);
		}
		for (itsul = hmsulToFlidVirtual.Begin(); itsul != hmsulToFlidVirtual.End(); ++itsul)
		{
			StrUni stu = itsul.GetKey();
			ULONG flid = itsul.GetValue();
			m_hmsulNameToFlid.Insert(stu, flid);
		}
	}

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}


/*----------------------------------------------------------------------------------------------
	Gets a list of the fields for the specified class.
	Gets all fields whose types match the specified argument, which should be a combination
	of the fcpt values defined in CmTypes.h, e.g., to get all owning properties
	pass kfcptOwningCollection | kfcptOwningAtom | kfcptOwningSequence.
	Returns E_FAIL if the array is too small. clid 0 may be passed to obtain the required size.
	Fields of superclasses are also returned, if the relevant flag is true.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetFields(ULONG luClid, ComBool fIncludeSuperclasses, int grfcpt,
	int cflidMax, ULONG * prgflid, int * pcflid)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgflid, cflidMax);
	ChkComOutPtr(pcflid);
	int cflid = 0;
	ULONG * pflidOut = prgflid;
	ULONG clidCurrent = luClid;
	// This loop executes once if fIncludeSuperclasses is false, otherwise over luClid
	// and all superclasses.
	for (;;)
	{
		FwMetaClassRec mcr;
		if (!m_hmmcr.Retrieve(clidCurrent, &mcr))
			ThrowHr(E_INVALIDARG, L"Unknown class");
		if (mcr.m_qmfl)
		{
			int cflidClass = mcr.m_qmfl->m_vuFields.Size();
			for (int iflid = 0; iflid < cflidClass; iflid++)
			{
				ULONG flid = mcr.m_qmfl->m_vuFields[iflid];
				if (grfcpt != 528482304) // kgrfcptAll, but that header is not included here
				{
					// Look up field type and see if it matches
					int cpt;
					CheckHr(GetFieldType(flid, &cpt));
					int fcpt = 1 << (cpt & 0x1f); // mask out kcptVirtual
					if (!(grfcpt & fcpt))
						continue; // don't return this one
				}
				cflid++;
				if (cflidMax)
				{
					if (cflid > cflidMax)
						return E_FAIL;
					*pflidOut++ = flid;
				}
			}
		}

		if (!fIncludeSuperclasses)
			break;
		if (clidCurrent == 0) // just processed CmObject
			break;
		// This would be more efficient if we had the right #defines included.
		//if (clidCurrent == kclidCmObject)
		//	break;
		CheckHr(GetBaseClsId(clidCurrent, &clidCurrent));
	}
	*pcflid = cflid;

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	Get the ID of the class having the specified name; return 0 if not found.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetClassId(BSTR bstrClassName, ULONG * pluClid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pluClid);
	ChkComBstrArg(bstrClassName);
	StrUni stuKey(bstrClassName);
	if (!m_hmsulNameToClid.Retrieve(stuKey, pluClid))
		*pluClid = 0;
	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	Gets the field ID given the class and field names. Searches superclasses if needed.
	Returns 0 if not found.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetFieldId(BSTR bstrClassName, BSTR bstrFieldName,
	ComBool fIncludeBaseClasses, ULONG * pluFlid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pluFlid);  // sets to 0 in case we never match.
	ChkComBstrArg(bstrFieldName);
	ChkComBstrArg(bstrClassName);
	ULONG luClid;
	StrUni stuClassKey(bstrClassName);
	if (!m_hmsulNameToClid.Retrieve(stuClassKey, &luClid))
	{
		*pluFlid = 0;
		return S_OK;
	}
	StrUni stuKey = MakeFlidKey(luClid, bstrFieldName);
	if (fIncludeBaseClasses)
	{
		while (luClid && !m_hmsulNameToFlid.Retrieve(stuKey, pluFlid))
		{
			CheckHr(GetBaseClsId(luClid, &luClid));
			stuKey = MakeFlidKey(luClid, bstrFieldName);
		}
	}
	else
	{
		m_hmsulNameToFlid.Retrieve(stuKey, pluFlid);
	}
	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache)
}

/*----------------------------------------------------------------------------------------------
	Gets the field ID given the class id and field name. Searches superclasses if needed.
	Returns 0 if not found.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetFieldId2(ULONG luClid, BSTR bstrFieldName,
	ComBool fIncludeBaseClasses, ULONG * pluFlid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pluFlid); // sets to 0 in case we never match.
	ChkComBstrArg(bstrFieldName);
	StrUni stuKey = MakeFlidKey(luClid, bstrFieldName);
	if (fIncludeBaseClasses)
	{
		while (luClid && !m_hmsulNameToFlid.Retrieve(stuKey, pluFlid))
		{
			CheckHr(GetBaseClsId(luClid, &luClid));
			stuKey = MakeFlidKey(luClid, bstrFieldName);
		}
	}
	else
	{
		m_hmsulNameToFlid.Retrieve(stuKey, pluFlid);
	}
	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	Gets a list of direct subclasses of the specified class. May pass cluMax 0 to retrieve
	number.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetDirectSubclasses(ULONG luClid, int cluMax, int * pcluOut,
		ULONG * prgluSubclasses)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcluOut); // sets to 0 in case we never match.
	ChkComArrayArg(prgluSubclasses, cluMax);

	FwMetaClassRec mcr;
	if (!m_hmmcr.Retrieve(luClid, &mcr))
	{
		ThrowHr(E_INVALIDARG, L"Class not found");
	}

	*pcluOut = mcr.m_vuDirectSubclasses.Size();
	if (cluMax == 0)
		return S_OK;
	if (cluMax < *pcluOut)
		return E_FAIL;
	CopyItems(mcr.m_vuDirectSubclasses.Begin(), prgluSubclasses, *pcluOut);
	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	Gets a list of all subclasses of the specified class (including itself, which will always
	be first).
	May pass cluMax 0 to retrieve number.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::GetAllSubclasses(ULONG luClid, int cluMax, int * pcluOut,
		ULONG * prgluSubclasses)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcluOut); // sets to 0 in case we never match.
	ChkComArrayArg(prgluSubclasses, cluMax);

	int cluSubclasses = 1; // itself
	if (cluMax > 0)
	{
		*prgluSubclasses = luClid; // Always set the first one to the input argument.
	}
	ULONG * plu = prgluSubclasses + 1;
	FldIdVec vuTodo;
	ULONG luClidCurrent = luClid; // class being processed
	// Execute this loop once for the root class, then as long as more subclasses
	// are found in the Todo list. Each iteration adds to the Todo list (and the output)
	// the subclasses of the current node, then takes one subclass to process next.
	do
	{
		FwMetaClassRec mcr;
		if (m_hmmcr.Retrieve(luClidCurrent, &mcr))
		{
			int cluDirect = mcr.m_vuDirectSubclasses.Size();
			if (cluDirect > 0)
			{
				cluSubclasses += cluDirect;
				if (cluMax > 0)
				{
					if (cluSubclasses > cluMax)
						return E_FAIL;
					CopyItems(mcr.m_vuDirectSubclasses.Begin(), plu, cluDirect);
					plu += cluDirect;
				}
				// Insert the new items into the todo list.
				int cluTodo = vuTodo.Size();
				vuTodo.Replace(cluTodo, cluTodo, mcr.m_vuDirectSubclasses.Begin(), cluDirect);
			}
		}
	}
	while(vuTodo.Pop(&luClidCurrent));

	*pcluOut = cluSubclasses;

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	Note a virtual property. The type is the simulated type, one of the original types,
	NOT with the virtual bit OR'd in.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::AddVirtualProp(BSTR bstrClass, BSTR bstrField, ULONG luFlid,
	int nType)
{
	BEGIN_COM_METHOD;
	FwMetaFieldRec mfr;
	mfr.m_iType = nType | kcptVirtual;
	CheckHr(GetClassId(bstrClass, &mfr.m_luOwnClsid));
	mfr.m_luDstClsid = (ULONG)-1;
	mfr.m_stuFieldName = bstrField;
	m_hmmfr.Insert(luFlid, mfr, true);
	StrUni stuKey = MakeFlidKey(mfr.m_luOwnClsid, mfr.m_stuFieldName.Bstr());
	m_hmsulNameToFlid.Insert(stuKey, luFlid);
	FwMetaClassRec mcr;
	if (!m_hmmcr.Retrieve(mfr.m_luOwnClsid, &mcr))
		ThrowHr(E_INVALIDARG, L"Unknown class");
	if (!mcr.m_qmfl)
	{
		mcr.m_qmfl.Attach(NewObj FwMetaFieldList);
		// Write it back to the map with the new metafieldlist.
		m_hmmcr.Insert(mfr.m_luOwnClsid, mcr, true);
	}
	mcr.m_qmfl->m_vuFields.Push(luFlid);

	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

/*----------------------------------------------------------------------------------------------
	Note a virtual property. The type is the simulated type, one of the original types,
	NOT with the virtual bit OR'd in.
---------------------------------------------------------------------------------------------*/
STDMETHODIMP FwMetaDataCache::get_IsVirtual(ULONG luFlid, ComBool * pfVirtual)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfVirtual);

	// We do not accept 0 as valid, and treat as non-virtual.
	if (!luFlid)
	{
		ThrowHr(E_INVALIDARG, L"Unknown flid");
	}

	FwMetaFieldRec mfr;
	if (m_hmmfr.Retrieve(luFlid, &mfr))
	{
		*pfVirtual = (mfr.m_iType & kcptVirtual) != 0;
	}
	else if (luFlid >= 1000000000) // kflidStartDummyFlids = 1000000000,
	{
		// It's a dummy--treat as non-virtual (rather arbitrary, but consistent with old working of GetFieldType).
		*pfVirtual = 0;
	}
	else
	{
		ReturnHr(E_INVALIDARG);
	}
	END_COM_METHOD(g_factmdc, IID_IFwMetaDataCache);
}

const int MSG_NUM = WM_USER+77;
INT_PTR CALLBACK DummyDialog(HWND hDlg, UINT message, WPARAM wParam, LPARAM lParam)
{
	UNREFERENCED_PARAMETER(lParam);
	switch (message)
	{
	case WM_INITDIALOG:
		PostMessage(hDlg, MSG_NUM, 0, 0);
		return (INT_PTR)TRUE;

	case MSG_NUM:
		EndDialog(hDlg, IDOK);
		return (INT_PTR)TRUE;
	}
	return (INT_PTR)FALSE;
}

// Explicit instantiation of hashmap class
#include "HashMap_i.cpp"
#include "Vector_i.cpp"
template Vector<ULONG>; // FldIdVec; // hungarian vu
