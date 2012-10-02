/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, SIL International. All rights reserved.

File: SqlDb.h
Responsibility: Steve McConnel (was Shon Katzenberger)
Last reviewed:

	Wraps an ODBC connection to a SQL Server database.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef SqlDb_H
#define SqlDb_H 1
//:End Ignore

/*----------------------------------------------------------------------------------------------
	SqlError provides a means to throw an exception that is identified by an ODBC error code.

	Hungarian: serr
----------------------------------------------------------------------------------------------*/
class SqlError : public Throwable
{
public:
	static void ThrowRc(RETCODE rc);

	//:> TODO ShonK: Handle getting the error message from ODBC.
	// Constructor.
	SqlError(RETCODE rc) : Throwable(E_FAIL)
	{
		m_rc = rc;
	}

protected:
	RETCODE m_rc;
};


//:Associate with "ODBC Utility Functions"
/*----------------------------------------------------------------------------------------------
	Check whether an ODBC return code signals an error, and if so, throw an exception.

	@param rc
----------------------------------------------------------------------------------------------*/
inline RETCODE CheckSqlRc(RETCODE rc)
{
	Assert(rc != SQL_INVALID_HANDLE);
	if (rc < 0)
		SqlError::ThrowRc(rc);
	return rc;
}


/*----------------------------------------------------------------------------------------------
	SqlDb encapsulates a database connection for using ODBC.

	Hungarian: sdb
----------------------------------------------------------------------------------------------*/
class SqlDb
{
public:
	SqlDb();
	virtual ~SqlDb();

	virtual HRESULT Open(const wchar * pszSvr, const wchar * pszDb);
	virtual void Close();

	bool IsOpen()
	{
		return m_hdbc != NULL;
	}

	SQLHDBC Hdbc()
	{
		return m_hdbc;
	}

protected:
	static Mutex s_mutxEnv;		// Used to protect s_henvAll and s_crefEnv.
	static SQLHENV s_henvAll;	// Handle to an ODBC environment.
	static int s_crefEnv;		// Reference count for s_henvAll - the number of SqlDb objects.

	SQLHDBC m_hdbc;				// Handle to an ODBC database connection.

	static void EnsureEnv();
};

/*----------------------------------------------------------------------------------------------
	SqlStatement encapsulates an ODBC/SQL statement handle.

	Hungarian: sstmt
----------------------------------------------------------------------------------------------*/
class SqlStatement
{
public:
	// Constructor.
	SqlStatement::SqlStatement()
	{
		m_hstmt = NULL;
	}

	// Destructor.
	SqlStatement::~SqlStatement()
	{
		Clear();
	}

	void Init(SqlDb & sdb);		//:> This may throw an exception.
	void Clear();

	// Return the ODBC statement handle.
	SQLHSTMT Hstmt()
	{
		return m_hstmt;
	}
protected:
	SQLHSTMT m_hstmt;		// Handle to an ODBC/SQL statement.
};

// Local Variables:
// mode:C++
// End:

#endif // !SqlDb_H
