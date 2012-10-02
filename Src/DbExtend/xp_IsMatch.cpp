#include "main.h"

#define XP_NOERROR              0
#define XP_ERROR                1
#define MAXCOLNAME				25
#define MAXNAME					25
#define MAXTEXT					255

#ifdef __cplusplus
extern "C" {
#endif

ULONG __declspec(dllexport) __GetXpVersion();

RETCODE __declspec(dllexport) xp_IsMatch(SRV_PROC * pSrvProc);

#ifdef __cplusplus
}
#endif

void printError(SRV_PROC * pSrvProc, char * szErrorMsg);
bool FindMatch(wchar_t * pszPattern, wchar_t * pszString);

#define SRV_MAXERROR		20000
#define GETTABLE_ERROR		SRV_MAXERROR + 1
#define XP_ISMATCH_ERROR	SRV_MAXERROR + 2


/*----------------------------------------------------------------------------------------------
	"It is highly recommended that all Microsoft® SQL Server (7.0 and greater) extended stored
	procedure DLLs implement and export __GetXpVersion.  For more information see SQL Server
	Books Online."
----------------------------------------------------------------------------------------------*/
ULONG __declspec(dllexport) __GetXpVersion()
{
	return ODS_VERSION;
}

/*----------------------------------------------------------------------------------------------
	Extended stored procedure to search a string with a pattern to find whether a match exists.

	Parameter 1: the Pattern (nvarchar)
	Parameter 2: the String (nvarchar or ntext)
	Parameter 3 [output]: the result (bit)
----------------------------------------------------------------------------------------------*/
RETCODE __declspec(dllexport) xp_IsMatch(SRV_PROC * pSrvProc)
{
	// Get number of parameters
	int paramnum = srv_rpcparams(pSrvProc);

	// Check number of parameters
	if (paramnum != 3)
	{
		// Send error message and return
		srv_sendmsg(pSrvProc, SRV_MSG_ERROR, GETTABLE_ERROR, SRV_INFO, (DBTINYINT)0, NULL, 0, 0,
			"Error executing extended stored procedure xp_IsMatch2: Need exactly 3 parameters",
			SRV_NULLTERM);
		// A SRV_DONE_MORE instead of a SRV_DONE_FINAL must complete the
		// result set of an Extended Stored Procedure.
		srv_senddone(pSrvProc, (SRV_DONE_ERROR | SRV_DONE_MORE), 0, 0);
		return(XP_ERROR);
	}

	// srv_paraminfo parameters
/*
  SRVINT1		tinyint		1-byte tinyint data type.
  SRVINT2		smallint	2-byte smallint data type.
  SRVINT4		Int			4-byte int data type.

  SRVNTEXT		ntext		Unicode text data type.
  SRVNVARCHAR	nvarchar	Unicode variable-length character data type.

  SRVVARBINARY	varbinary	Variable-length binary data type.
  SRVIMAGE		image		image data type.
*/
	BYTE bType;
	ULONG lnMaxLen1;
	ULONG lnActualLen1;
	ULONG lnMaxLen2;
	ULONG lnActualLen2;
	BOOL fNull;
	wchar_t * pszString;
	wchar_t * pszPattern;

	//==== Parameter 1: The Pattern ====//
	if (srv_paraminfo(pSrvProc, 1, &bType, &lnMaxLen1, &lnActualLen1, (BYTE *)NULL, &fNull)
		== FAIL)
	{
		printError(pSrvProc, "srv_paraminfo 1 failed...");
		return (XP_ERROR);
	}
	if (bType != SRVNTEXT && bType != SRVNVARCHAR)
	{
		printError(pSrvProc, "invalid type for parameter 1...");
		return (XP_ERROR);
	}
	pszPattern = (wchar_t *)malloc(lnActualLen1 + sizeof(wchar_t));
	if (pszPattern == NULL)
	{
		printError(pSrvProc, "out of memory...");
		return (XP_ERROR);
	}
	if (srv_paraminfo(pSrvProc, 1, &bType, &lnMaxLen1, &lnActualLen1, (BYTE *)pszPattern, &fNull)
		== FAIL)
	{
		printError(pSrvProc, "srv_paraminfo 1 failed...");
		free(pszPattern);
		return (XP_ERROR);
	}
	pszPattern[lnActualLen1 / sizeof(wchar_t)] = 0;

	//==== Parameter 2: The String ====//
	if (srv_paraminfo(pSrvProc, 2, &bType, &lnMaxLen2, &lnActualLen2, (BYTE *)NULL, &fNull)
		== FAIL)
	{
		printError(pSrvProc, "srv_paraminfo 2 failed...");
		free(pszPattern);
		return (XP_ERROR);
	}
	if (bType != SRVNTEXT && bType != SRVNVARCHAR)
	{
		printError(pSrvProc, "invalid type for parameter 2...");
		free(pszPattern);
		return (XP_ERROR);
	}
	pszString = (wchar_t *)malloc(lnActualLen2 + sizeof(wchar_t));
	if (pszString == NULL)
	{
		printError(pSrvProc, "out of memory...");
		free(pszPattern);
		return (XP_ERROR);
	}
	if (srv_paraminfo(pSrvProc, 2, &bType, &lnMaxLen2, &lnActualLen2, (BYTE *)pszString, &fNull)
		== FAIL)
	{
		printError(pSrvProc, "srv_paraminfo 2 failed...");
		free(pszPattern);
		free(pszString);
		return (XP_ERROR);
	}
	pszString[lnActualLen2 / sizeof(wchar_t)] = 0;

	DBBIT fMatches = FindMatch(pszPattern, pszString);

	// Set the output parameter
	if (srv_paramsetoutput(pSrvProc, 3, (BYTE *)&fMatches, sizeof(fMatches), FALSE) == FAIL)
		{
		printError (pSrvProc, "srv_paramsetoutput failed...");
		return (XP_ERROR);
		}

	DBINT cSent = 0;
#if 99-99
	// Set up the output column name.
	srv_describe(pSrvProc, 1, "Matched", SRV_NULLTERM, SRVBIT, sizeof(DBBIT), SRVBIT,
		sizeof(DBBIT), 0);
	// Update field 1 "ID"
	srv_setcoldata(pSrvProc, 1, &fMatches);

	srv_describe(pSrvProc, 2, "Pattern", SRV_NULLTERM, SRVNVARCHAR, 400, SRVNVARCHAR, 0, NULL);
	if (srv_setcollen(pSrvProc, 2, (int)(wcslen(pszPattern) * sizeof(wchar_t))) == FAIL)
	{
		printError (pSrvProc, "srv_setcollen 2 failed...");
		return (XP_ERROR);
	}
	if (srv_setcoldata(pSrvProc, 2, pszPattern) == FAIL)
	{
		printError (pSrvProc, "srv_setcoldata 2 failed...");
		return (XP_ERROR);
	}
	srv_describe(pSrvProc, 3, "String", SRV_NULLTERM, SRVNVARCHAR, 4000, SRVNVARCHAR, 0, NULL);
	if (srv_setcollen(pSrvProc, 3, (int)(wcslen(pszString) * sizeof(wchar_t))) == FAIL)
	{
		printError (pSrvProc, "srv_setcollen 3 failed...");
		return (XP_ERROR);
	}
	if (srv_setcoldata(pSrvProc, 3, pszString) == FAIL)
	{
		printError (pSrvProc, "srv_setcoldata 3 failed...");
		return (XP_ERROR);
	}
	srv_describe(pSrvProc, 4, "MaxLen1", SRV_NULLTERM, SRVINT2, sizeof(DBSMALLINT), SRVINT2,
		sizeof(DBSMALLINT), 0);
	srv_setcoldata(pSrvProc, 4, &lnMaxLen1);
	srv_describe(pSrvProc, 5, "ActualLen1", SRV_NULLTERM, SRVINT2, sizeof(DBSMALLINT), SRVINT2,
		sizeof(DBSMALLINT), 0);
	srv_setcoldata(pSrvProc, 5, &lnActualLen1);
	srv_describe(pSrvProc, 6, "MaxLen2", SRV_NULLTERM, SRVINT2, sizeof(DBSMALLINT), SRVINT2,
		sizeof(DBSMALLINT), 0);
	srv_setcoldata(pSrvProc, 6, &lnMaxLen2);
	srv_describe(pSrvProc, 7, "ActualLen2", SRV_NULLTERM, SRVINT2, sizeof(DBSMALLINT), SRVINT2,
		sizeof(DBSMALLINT), 0);
	srv_setcoldata(pSrvProc, 7, &lnActualLen2);

	// Send the entire row (only 1 column)
	srv_sendrow(pSrvProc);
	cSent = 1;
#endif
	// Now return the number of rows processed
	srv_senddone(pSrvProc, SRV_DONE_MORE | SRV_DONE_COUNT, (DBUSMALLINT)0, cSent);

	free(pszPattern);
	free(pszString);

	return XP_NOERROR;
}


/*----------------------------------------------------------------------------------------------
	Send szErrorMsg to client.
----------------------------------------------------------------------------------------------*/
void printError(SRV_PROC * pSrvProc, CHAR * szErrorMsg)
{
	srv_sendmsg(pSrvProc, SRV_MSG_ERROR, XP_ISMATCH_ERROR, SRV_INFO, 1, NULL, 0,
		(DBUSMALLINT)__LINE__, szErrorMsg, SRV_NULLTERM);

	srv_senddone(pSrvProc, (SRV_DONE_ERROR | SRV_DONE_MORE), 0, 0);
}

/*----------------------------------------------------------------------------------------------
	Check whether the pattern is found in the string.  This is much more complicated than you
	think!  Return true if found, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FindMatch(wchar_t * pszPattern, wchar_t * pszString)
{
	UnicodeString usPat(pszPattern);
	usPat.toLower();

	UnicodeString usString(pszString);
	usString.toLower();

	const wchar_t * psz = wcsstr(usString.getTerminatedBuffer(), usPat.getTerminatedBuffer());
	return psz ? true : false;
}

// Local Variables:
// compile-command:"cmd.exe /E:4096 /C ..\\..\\Bin\\mkdbex.bat"
// End:
