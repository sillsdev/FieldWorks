/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, SIL International. All rights reserved.

File: SqlDb.cpp
Responsibility: Steve McConnel (was Shon Katzenberger)
Last reviewed:

----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


/***********************************************************************************************
	Static variables.
***********************************************************************************************/
Mutex SqlDb::s_mutxEnv;
SQLHENV SqlDb::s_henvAll;
int SqlDb::s_crefEnv;
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Static function to throw an ODBC error.

	@param rc ODBC return code identifying the specific error.
----------------------------------------------------------------------------------------------*/
void SqlError::ThrowRc(RETCODE rc)
{
	throw SqlError(rc);
}


/*----------------------------------------------------------------------------------------------
	Static function to allocate the ODBC environment if it isn't already.  This may throw an
	error.
----------------------------------------------------------------------------------------------*/
void SqlDb::EnsureEnv()
{
	LockMutex(s_mutxEnv);

	if (s_henvAll)
		return;

	RETCODE rc;

	try
	{
		// Allocate the ODBC Environment.
		rc = SQLAllocHandle(SQL_HANDLE_ENV, NULL, &s_henvAll);
		CheckSqlRc(rc);

		// Let ODBC know this is an ODBC 3.0 application.
		rc = SQLSetEnvAttr(s_henvAll, SQL_ATTR_ODBC_VERSION, (SQLPOINTER)SQL_OV_ODBC3,
			SQL_IS_INTEGER);
		CheckSqlRc(rc);
	}
	catch(...)
	{
		if (s_henvAll)
		{
			SQLFreeHandle(SQL_HANDLE_ENV, s_henvAll);
			s_henvAll = NULL;
		}
		throw;
	}
}


/*----------------------------------------------------------------------------------------------
	Constructor.  This increments the global ref count for the ODBC environment.
----------------------------------------------------------------------------------------------*/
SqlDb::SqlDb()
{
	m_hdbc = NULL;
	LockMutex(s_mutxEnv);
	Assert(s_crefEnv >= 0);
	s_crefEnv++;
}


/*----------------------------------------------------------------------------------------------
	Destructor.  This decrements the global ref count for the ODBC environment, and nukes the
	environment if the ref count is zero.
----------------------------------------------------------------------------------------------*/
SqlDb::~SqlDb()
{
	Close();
	LockMutex(s_mutxEnv);
	Assert(s_crefEnv > 0);
	if (!--s_crefEnv && s_henvAll)
	{
		SQLFreeHandle(SQL_HANDLE_ENV, s_henvAll);
		s_henvAll = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Open the given database on the given server.

	@param pszSvr Name of the database server machine.
	@param pszDb Name of the database.

	@return S_OK, E_UNEXPECTED, or E_FAIL
----------------------------------------------------------------------------------------------*/
HRESULT SqlDb::Open(const wchar * pszSvr, const wchar * pszDb)
{
	AssertPsz(pszSvr);
	AssertPsz(pszDb);

	HRESULT hr;
	RETCODE rc;

	if (m_hdbc)
		Close();

	try
	{
		StrUniBuf stub;
		wchar rgch[1000];
		SQLSMALLINT cchT;

		if (!s_henvAll)
			EnsureEnv();

		rc = SQLAllocHandle(SQL_HANDLE_DBC, s_henvAll, &m_hdbc);
		CheckSqlRc(rc);

		if(CURRENTDB == MSSQL) {
			DoAssert(stub.Format(L"DRIVER={Sql Server};SERVER=%s;DATABASE=%s;UID=FWDeveloper;PWD=careful;AutoTranslate=no;",
			pszSvr, pszDb));
			//fprintf(stdout, "MSSQL\n");
		}
		else if(CURRENTDB == FB) {
			DoAssert(stub.Format(L"Driver=Firebird/InterBase(r) driver;Uid=SYSDBA;Pwd=inscrutable;DbName=%s;",pszDb));
			//fprintf(stdout, "FB\n");
		}

		rc = SQLDriverConnectW(m_hdbc, NULL, const_cast<wchar *>(stub.Chars()), (SQLSMALLINT)stub.Length(),
			rgch, SizeOfArray(rgch), &cchT, SQL_DRIVER_NOPROMPT);
		CheckSqlRc(rc);
		hr = S_OK;
	}
	catch (SqlError & sqle)
	{
		Close();
		hr = sqle.Error();
	}
	catch (...)
	{
		Close();
		hr = WarnHr(E_UNEXPECTED);
	}
	//fprintf(stdout,"hr=%s\n",AsciiHresult(hr));
	return hr;
}


/*----------------------------------------------------------------------------------------------
	Close the database connection.
----------------------------------------------------------------------------------------------*/
void SqlDb::Close()
{
	if (m_hdbc)
	{
		SQLTCHAR sqst[6];
		SQLINTEGER ntverr;
		SQLTCHAR rgchBuf[1024];
		SQLSMALLINT cb;
		memset(sqst, 0, SizeOfArray(sqst));
		memset(rgchBuf, 0, SizeOfArray(rgchBuf));
		SQLRETURN rc = SQLDisconnect(m_hdbc);
		if (rc == SQL_ERROR)
		{
			SQLGetDiagRec(SQL_HANDLE_DBC, m_hdbc, 1, sqst, &ntverr, rgchBuf, SizeOfArray(rgchBuf)-1,
				&cb);
			sqst[5] = 0;
			rgchBuf[SizeOfArray(rgchBuf)-1] = 0;
		}
		else if (rc == SQL_INVALID_HANDLE)
		{
			// Not much we can do here!
		}
		else
		{
			if (rc == SQL_SUCCESS_WITH_INFO)
			{
				SQLGetDiagRec(SQL_HANDLE_DBC, m_hdbc, 1, sqst, &ntverr, rgchBuf,
					SizeOfArray(rgchBuf)-1, &cb);
				sqst[5] = 0;
				rgchBuf[SizeOfArray(rgchBuf)-1] = 0;
			}
			rc = SQLFreeHandle(SQL_HANDLE_DBC, m_hdbc);
		}
		m_hdbc = 0;
	}
}


/*----------------------------------------------------------------------------------------------
	Allocate a new ODBC/SQL statement handle.  If one was already allocated, the existing
	one is freed first.

	@param sdb Reference to the ODBC database object.
----------------------------------------------------------------------------------------------*/
void SqlStatement::Init(SqlDb & sdb)
{
	Clear();
	if (!sdb.IsOpen())
		ThrowHr(WarnHr(E_UNEXPECTED));
	CheckSqlRc(SQLAllocHandle(SQL_HANDLE_STMT, sdb.Hdbc(), &m_hstmt));
}


/*----------------------------------------------------------------------------------------------
	Free the ODBC/SQL statement handle if one has been allocated.
----------------------------------------------------------------------------------------------*/
void SqlStatement::Clear()
{
	if (m_hstmt)
	{
		RETCODE rc = SQLFreeHandle(SQL_HANDLE_STMT, m_hstmt);
//		if (rc < 0)
//			MessageBox(NULL, "Failed to Free SQL Handle", "ERROR", MB_OK);
		CheckSqlRc(rc);
		m_hstmt = 0;
	}
}

// Local Variables:
// compile-command:"cmd.exe /E:4096 /C ..\\..\\Bin\\mkcel.bat"
// End:
