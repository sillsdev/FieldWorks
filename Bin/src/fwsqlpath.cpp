// fwsqlpath.cpp - source for the 'fwsqlpath' program
// Steve McConnel, 10-Jul-2003 (to facilitate building/debugging extended stored procedure DLLs)

#include <windows.h>
#include <stdio.h>
#include <string.h>
#include <assert.h>

int main(int argc, char ** argv)
{
	if ((argc > 2) || ((argc == 2) && (strcmp(argv[1], "?") == 0)))
	{
		fputs("\
usage: fwsqlpath [\"set FOO=\"]\n\
	The first and only argument is an optional string, usually a command for\n\
	setting an environment variable.\n\
	The concatenation of the (optional) argument followed by the full pathname\n\
	of the ...\\MSSQL$SILFW\\Binn directory is written to the standard output,\n\
	and can thus be redirected to a file.\n\
", stderr);
		return 1;
	}
	char rgchFwSqlPath[MAX_PATH+1];
	HKEY hk;
	long lRet = ::RegOpenKeyExA(HKEY_LOCAL_MACHINE,
		"Software\\Microsoft\\Microsoft SQL Server\\SILFW\\Setup", 0, KEY_QUERY_VALUE, &hk);
	if (lRet == ERROR_SUCCESS)
	{
		DWORD cb = sizeof(rgchFwSqlPath);
		DWORD dwT;
		lRet = ::RegQueryValueExA(hk, "SQLPath", NULL, &dwT, (BYTE *)rgchFwSqlPath, &cb);
		if (lRet == ERROR_SUCCESS)
		{
			assert(dwT == REG_SZ);
			assert(cb + 6 < sizeof(rgchFwSqlPath));

			strncat(rgchFwSqlPath, "\\Binn", sizeof(rgchFwSqlPath) - cb);
			printf("%s%s\n", (argc > 1) ? argv[1] : "", rgchFwSqlPath);
			return 0;
		}
		else
		{
			fputs("\
ERROR: Cannot access the SQLPath variable in the registry!?\n\
", stderr);
			return 1;
		}
	}
	else
	{
		fputs("\
ERROR: The SILFW named instance of Microsoft SQL Server is apparently not installed!?\n\
", stderr);
		return 1;
	}
}

// File settings for GNU Emacs (Please leave for Steve McConnel's sake!)
// Local Variables:
// mode:C++
// c-file-style:"cellar"
// compile-command:"cl.exe /Ox /ML fwsqlpath.cpp /link Advapi32.lib"
// tab-width:4
// End:
