/*----------------------------------------------------------------------------------------------
Copyright (C) 2000 by SIL International.  All rights reserved.

File: CreateDb.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This Win32 console application creates a database, and optionally initializes it as a
	FieldWorks database.
----------------------------------------------------------------------------------------------*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "Main.h"
#pragma hdrstop

#include <sys/stat.h>		// For struct stat definition and stat() declaration.
#include "Vector_i.cpp"
extern "C" char * optarg;
extern "C" int getopt(int argc, char * const argv[], const char * opts);

#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	Check the result of an SQL function, displaying any user information that results.
----------------------------------------------------------------------------------------------*/
void VerifySqlRc(RETCODE rc, SQLHANDLE hstmt, const char * pszCmd)
{
	if (rc == SQL_ERROR || rc == SQL_SUCCESS_WITH_INFO)
	{
		SQLCHAR sqst[6];
		SQLINTEGER ntverr;
		SQLCHAR szBuf[512];
		SQLSMALLINT cb;
		SQLGetDiagRec(SQL_HANDLE_STMT, hstmt, 1, sqst, &ntverr, szBuf, isizeof(szBuf) - 1, &cb);
		sqst[5] = 0;
		szBuf[isizeof(szBuf) - 1] = 0;
		if (rc == SQL_ERROR)
		{
			if (pszCmd && *pszCmd)
				printf("ERROR %s executing SQL command:\n%s\n----------------\n%s\n",
					sqst, pszCmd, szBuf);
			else
				printf("ERROR %s executing SQL function:\n%s\n", sqst, szBuf);
		}
		else
		{
			char * psz;
			psz = reinterpret_cast<char *>(szBuf);
			if (!strncmp(psz, "[Microsoft]", 11))
				psz += 11;
			if (!strncmp(psz, "[ODBC SQL Server Driver]", 24))
				psz += 24;
			if (!strncmp(psz, "[SQL Server]", 12))
				psz += 12;
/*The total row size (16037) for table 'MultiStr$' exceeds the maximum number of bytes per row (8060). Rows that exceed the maximum number of bytes will not be added.*/
			if (!strncmp(psz, "The total row size (", 20))
			{
				StrAnsiBuf stab;
				long cbHave;
				long cbWant = strtol(psz + 20, &psz, 10);
				if (!strncmp(psz, ") for table '", 13))
				{
					psz += 13;
					stab.Assign(psz, strcspn(psz, "'"));
					psz += stab.Length();
					if (!strncmp(psz, "' exceeds the maximum number of bytes per row (", 47))
					{
						cbHave = strtol(psz + 47, &psz, 10);
						if (!strncmp(psz,
						   "). Rows that exceed the maximum number of bytes will not be added.",
							66))
						{
							psz = reinterpret_cast<char *>(szBuf);
							sprintf(psz,
						   "creating table %s - potential row size = %ld > %ld",
								stab.Chars(), cbWant, cbHave);
						}
					}
				}
			}
			printf("%s\n", psz);
		}
	}
	CheckSqlRc(rc);
}

/*----------------------------------------------------------------------------------------------
	Initialize the database.
----------------------------------------------------------------------------------------------*/
void InitializeDB(const wchar * pszwServer, const wchar * pszwDB, const char * pszInitScript)
{
	SqlDb sdb;
	SqlStatement sstmt;
	HRESULT hr;
	char * pszCmd;
	char * psz;
	char * pchEnd;
	int chLeading;
	int chTrailing;
	Vector<char> vchScript;
	FILE * pfile;
	RETCODE rc;
	struct stat statInit;

	// osql -U sa -E -n -b -d %1 <%SQLSCRIPT%

	if (stat(pszInitScript, &statInit))
	{
		fprintf(stderr, "Cannot open Initialization SQL file \"%s\"!\n", pszInitScript);
		ThrowHr(WarnHr(E_FAIL));
	}
	vchScript.Resize(statInit.st_size + 1);
	pfile = fopen(pszInitScript, "rb");
	if (!pfile)
	{
		fprintf(stderr, "Cannot open Initialization SQL file \"%s\"!\n", pszInitScript);
		ThrowHr(WarnHr(E_FAIL));
	}
	fread(vchScript.Begin(), 1, statInit.st_size, pfile);
	fclose(pfile);
	pchEnd = vchScript.Begin() + statInit.st_size;
	*pchEnd = '\0';

	hr = sdb.Open(pszwServer, pszwDB);
	CheckHr(hr);
	sstmt.Init(sdb);

	// This is needed to allow double quotes in dynamic SQL, which is used by some of the
	// stored procedures that FieldWorks defines.
	pszCmd = "SET QUOTED_IDENTIFIER OFF";
	rc = SQLExecDirectA(sstmt.Hstmt(), reinterpret_cast<SQLCHAR *>(pszCmd), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), pszCmd);
	sstmt.Clear();

	// Find next "go" keyword, if any.
	// Note that we have to skip over any comments or quoted strings while searching.
	// Also we need to convert non-embedded double quotes to single quotes.
	bool fSingleQuoted;
	bool fDoubleQuoted;
	char * pszOpen;
	bool fInLineComment;
	bool fInsideComment;
	int ch;
	int ch2;
	for (pszCmd = vchScript.Begin(); pszCmd < pchEnd; pszCmd = psz)
	{
		// Skip leading whitespace.
		pszCmd += strspn(pszCmd, " \t\r\n\f\v");
		if (pszCmd == pchEnd)
			break;
		fSingleQuoted = false;
		fDoubleQuoted = false;
		pszOpen = NULL;
		fInLineComment = false;
		fInsideComment = false;
		for (psz = pszCmd; psz < pchEnd; ++psz)
		{
			ch = *psz;
			ch2 = *(psz + 1);
			if (fInLineComment)
			{
				if (ch == '\n')
					fInLineComment = false;
				continue;
			}
			if (fInsideComment)
			{
				if (ch == '*' && ch2 == '/')
				{
					fInsideComment = false;
					++psz;
				}
				continue;
			}
			if (!fSingleQuoted && !fDoubleQuoted)
			{
				if (ch == '-' && ch2 == '-')
				{
					fInLineComment = true;
					++psz;
					continue;
				}
				else if (ch == '/' && ch2 == '*')
				{
					fInsideComment = true;
					++psz;
					continue;
				}
			}
			if (ch == '\'')
			{
				if (fSingleQuoted)
				{
					fSingleQuoted = false;
				}
				else if (fDoubleQuoted)
				{
					// Retain outer double quotes if embedded single quote.
					pszOpen = NULL;
				}
				else
				{
					fSingleQuoted = true;
				}
			}
			else if (ch == '"')
			{
				if (fSingleQuoted)
				{
					// Do nothing. (?)
				}
				else if (fDoubleQuoted)
				{
					if (pszOpen)
					{
						// Convert double quotes to single quotes if not embedded.
						*pszOpen = '\'';
						*psz = '\'';
						pszOpen = NULL;
					}
					fDoubleQuoted = false;
				}
				else
				{
					fDoubleQuoted = true;
					pszOpen = psz;
				}
			}
			else if ((ch == 'g' || ch == 'G') && (ch2 == 'o' || ch2 == 'O') &&
				!fSingleQuoted && !fDoubleQuoted)
			{
				chLeading = (psz > pszCmd) ? *(psz - 1) : ' ';
				chTrailing = (psz + 2 < pchEnd) ? *(psz + 2) : ' ';
				if (isascii(chLeading) && isspace(chLeading) &&
					isascii(chTrailing) && isspace(chTrailing))
				{
					*psz = '\0';
					psz += 2;
					break;
				}
			}
		}

		sstmt.Init(sdb);
		rc = SQLExecDirectA(sstmt.Hstmt(), reinterpret_cast<SQLCHAR *>(pszCmd), strlen(pszCmd));
		VerifySqlRc(rc, sstmt.Hstmt(), pszCmd);
		sstmt.Clear();
	}
	sdb.Close();
}

/*----------------------------------------------------------------------------------------------
	Create the database.  Return 0 if successful, or a nonzero value if an error occurs.
----------------------------------------------------------------------------------------------*/
int CreateDB(const char * pszServer, const char * pszDB, const char * pszOutputDir,
	const char * pszInitScript,	bool fForceCreate)
{
	HRESULT hr;

	static const wchar szwMaster[] = L"master";
	StrUniBuf stubServer(pszServer);
	StrUniBuf stubDatabase(pszDB);

	if (stubServer.Overflow() || stubDatabase.Overflow())
	{
		fprintf(stderr, "Out of memory filling static buffers??\n");
		return __LINE__;
	}
	try
	{
		SqlDb sdb;
		SqlStatement sstmt;
		StrAnsiBufBig stabCmd;
		hr = sdb.Open(stubServer.Chars(), stubDatabase.Chars());
		if (SUCCEEDED(hr))
		{
			sdb.Close();
			if (fForceCreate)
			{
				// osql /U sa /E /n /b /Q "DROP DATABASE %1"
				hr = sdb.Open(stubServer.Chars(), szwMaster);
				if (FAILED(hr))
					ThrowHr(WarnHr(hr));
				sstmt.Init(sdb);
				stabCmd.Format("DROP DATABASE %s", pszDB);
				RETCODE rc;
				rc = SQLExecDirectA(sstmt.Hstmt(),
					reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
				VerifySqlRc(rc, sstmt.Hstmt(), stabCmd.Chars());
				sstmt.Clear();
				sdb.Close();
			}
			else
			{
				fprintf(stderr, "The database \"%s\" already exists on the server \"%s\".\n",
					pszDB, pszServer);
				fprintf(stderr,
					"Use the -f command line flag to force recreating this database.\n");
				return 1;
			}
		}
		// osql -U sa -E -n -b -Q "CREATE DATABASE %1 ON
		// (NAME=%1,FILENAME='%OUTPUT_DIR%\%1.mdf',FILEGROWTH=5MB) LOG ON
		// (NAME='%1_Log', FILENAME='%OUTPUT_DIR%\%1_log.ldf',FILEGROWTH=5MB)"
		hr = sdb.Open(stubServer.Chars(), szwMaster);
		CheckHr(hr);
		sstmt.Init(sdb);
		if (pszOutputDir)
			stabCmd.Format("CREATE DATABASE %s ON (NAME='%s',FILENAME='%s\\%s.mdf') \
LOG ON (NAME='%s_Log',FILENAME='%s\\%s_log.ldf')",
			pszDB, pszDB, pszOutputDir, pszDB, pszDB, pszOutputDir, pszDB);
		else
			stabCmd.Format("CREATE DATABASE %s ", pszDB);

		RETCODE rc;
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), stabCmd.Chars());
		sstmt.Clear();
		sdb.Close();
		if (pszInitScript)
		{
			InitializeDB(stubServer.Chars(), stubDatabase.Chars(), pszInitScript);
		}
	}
	catch (Throwable & thr)
	{
		fprintf(stderr, "Error %s caught creating database \"%s\" on server \"%s\"!\n",
			AsciiHresult(thr.Error()), pszDB, pszServer);
		return __LINE__;
	}
	catch (...)
	{
		fprintf(stderr, "Error caught creating database \"%s\" on server \"%s\"!\n",
			pszDB, pszServer);
		return __LINE__;
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Main procedure for this console application: creating a FieldWorks database from XML.
----------------------------------------------------------------------------------------------*/
int main(int argc, char ** argv)
{
	// Temporary (?) hack to sidestep bug in Microsoft's C runtime library.
	_set_sbh_threshold(0);

	// Check for memory leaks
	_CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);

	char * pszOutputDB = NULL;
	char * pszOutputDir = NULL;
	char * pszServer = NULL;
	char * pszInitFile = NULL;
	bool fForceCreate = false;
	int ch;
	bool fError = false;
	char szLocal[MAX_COMPUTERNAME_LENGTH + 1];

	while ((ch = getopt(argc, argv, "d:fi:s:o:")) != EOF)
	{
		switch (ch)
		{
		case 'd':
			pszOutputDB = optarg;
			break;
		case 'o':
			pszOutputDir = optarg;
			break;
		case 'f':
			fForceCreate = true;
			break;
		case 'i':
			pszInitFile = optarg;
			break;
		case 's':
			pszServer = optarg;
			break;
		default:
			fError = true;
			break;
		}
	}
	if (!pszOutputDB || fError)
	{
		printf("\
Usage: CreateDb -d outputdb [-f] [-i init.sql] [-s server]\n\
   -d outputdb  = the output database (no default)\n\
   -o outputdir = the dir where the database files are created(default: default data dir)\n\
   -f           = force recreating an existing database\n\
   -i init.sql  = the initialization SQL script (default: no initialization)\n\
   -s server    = the server where the database is located (default: local system)\n\
");
		exit(1);
	}

	char * p = strrchr(pszOutputDB, '.');
	if (p != NULL)
	{
		fprintf(stderr,
			"Illegal output database name \"%s\": should not have a period in it!\n",
			pszOutputDB);
		exit(1);
	}
	if (!stricmp(pszOutputDB, "master") ||
		!stricmp(pszOutputDB, "model") ||
		!stricmp(pszOutputDB, "tempdb") ||
		!stricmp(pszOutputDB, "pubs") ||
		!stricmp(pszOutputDB, "Northwind") ||
		!stricmp(pszOutputDB, "msdb"))
	{
		fprintf(stderr, "Surely you jest! \"%s\" is a standard system database!\n",
			pszOutputDB);
		exit(1);
	}
	if (!pszServer)
	{
		DWORD cbLocal = isizeof(szLocal);
		if (!GetComputerNameA(szLocal, &cbLocal))
			strcpy(szLocal, "(local)");
		pszServer = szLocal;
	}
	if (FAILED(CoInitialize(NULL)))
	{
		fprintf(stderr, "Cannot initialize COM subsystem! (CoInitialize() failed??)\n");
		exit(1);
	}

	int cErrors = CreateDB(pszServer, pszOutputDB, pszOutputDir, pszInitFile, fForceCreate);

	CoUninitialize();

	return cErrors;
}

// Local Variables:
// compile-command:"cmd.exe /e:4096 /c mkcre.bat "
// End:
