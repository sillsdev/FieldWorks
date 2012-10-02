/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilSil.cpp
Responsibility:
Last reviewed:

Description:
	Code for SIL utilities.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

#if !WIN32
#include <netdb.h>
#include <unistd.h>
#include <sys/wait.h>
#include <time.h>
#endif

/*----------------------------------------------------------------------------------------------
	Get the name of the local FieldWorks database server.
----------------------------------------------------------------------------------------------*/
const wchar * SilUtil::LocalServerName()
{
	static StrUni s_stuLocalServer;
	if (!s_stuLocalServer.Length())
	{
#if WIN32
		wchar rgchComputer[MAX_COMPUTERNAME_LENGTH + 1];
		DWORD cch = MAX_COMPUTERNAME_LENGTH + 1;
		::GetComputerNameW(rgchComputer, &cch);
		s_stuLocalServer.Format(L"%s\\SILFW", rgchComputer);
#else
		const int max_compname_len = 4096; // TODOLinix this should be set to the maximun host length name
		char bufHost[max_compname_len + 1];
		size_t len = sizeof(bufHost);
		if(gethostname(bufHost, len) != 0)
		{
			hostent * host = gethostbyname(bufHost);
			s_stuLocalServer = host->h_name;
			s_stuLocalServer.Append("/SILFW");
		}
		else
		{
			s_stuLocalServer.Format(L"%S", bufHost);
			s_stuLocalServer.Append("/SILFW");
		}
#endif //WIN32
	}
	return s_stuLocalServer.Chars();
}

/*----------------------------------------------------------------------------------------------
	Execute the command in the given string and, if fWaitTillExit is set, wait for the
	launched process to exit.

	@param pszCmd command line to execute.
	@param fInvisible flag whether to show the app's window (if a console app).
	@param fWaitTillExit flag whether to wait for the app to finish executing.
	@param pdwExitCode return value from the process (negative implies some sort of failure)

	@return true if successful in creating the process, false otherwise.
----------------------------------------------------------------------------------------------*/
bool SilUtil::ExecCmd(LPCOLESTR pszCmd, bool fInvisible, bool fWaitTillExit, DWORD * pdwExitCode)
{
#if WIN32
	// Set up data for creating new process:
	if (pdwExitCode)
		*pdwExitCode = (DWORD)-1;

	SECURITY_ATTRIBUTES saProcess;
	saProcess.nLength = sizeof(saProcess);
	saProcess.lpSecurityDescriptor = NULL;
	saProcess.bInheritHandle = TRUE;

	SECURITY_ATTRIBUTES saThread;
	saThread.nLength = sizeof(saThread);
	saThread.lpSecurityDescriptor = NULL;
	saThread.bInheritHandle = FALSE;

	STARTUPINFO si;
	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);

	PROCESS_INFORMATION process_info;

	// Launch new process:
	DWORD dwCreateFlags = fInvisible ? CREATE_NO_WINDOW : 0;
	BOOL bReturnVal = ::CreateProcess(NULL, const_cast<LPTSTR>(pszCmd), &saProcess, &saThread,
		false, dwCreateFlags, NULL, NULL, &si, &process_info);

	if (bReturnVal)
	{
		::CloseHandle(process_info.hThread);
		if (fWaitTillExit)
		{
			::WaitForSingleObject(process_info.hProcess, INFINITE);
			DWORD dwExitCode;
			::GetExitCodeProcess(process_info.hProcess, &dwExitCode);
			if (pdwExitCode)
				*pdwExitCode = dwExitCode;
		}
		::CloseHandle(process_info.hProcess);
	}
	return bReturnVal;
#else // if !WIN32
	//11/Sept/2008 - Previous implemention using execvp didn't work when Application had
	//arguments. - So option was to wither parse the pszCmd into cmd + args or
	//use system. - Due to how this method is currently implemented, this  is less efficient.

	StrAnsi str(pszCmd);
	const char * cmd = str.Chars();
	const char * cmdv[] = {"/bin/sh", "-c", cmd, 0};
	pid_t pid, w;
	pid = fork();
	if(pid == -1)
		return false;
	if(pid == 0)
	{
		// child
		execvp("/bin/sh", const_cast<char * const *>(cmdv));
		// currently, if execv fails the parent will falsely tell
		// the caller that execv succeded
		exit(EXIT_FAILURE);
	}
	else
	{
		// parent
		if (fWaitTillExit)
		{
			int exitCode = 0, status = 0;
			for(;;)
			{
				w = waitpid(pid,&status,0);

				if (w == -1)
				{
					perror("waitpid returned -1");
					break;
				}

				else if(w == pid && WIFEXITED(status))
				{
					exitCode = WEXITSTATUS(status);
					if (pdwExitCode)
						*pdwExitCode = (DWORD)exitCode;
					break;
				}
				else
				{
					fprintf(stderr, "WIFEXITED for %s returned false\n w=%d pid=%d", cmdv[2], w, pid);
				}
			}
		}
	}

	return true;
#endif // if !WIN32
}

/*----------------------------------------------------------------------------------------------
	Compare two system times with in a given number of seconds of slack.
	Return true if the two times are within XXseconds of each other, false otherwise.
----------------------------------------------------------------------------------------------*/
bool SilUtil::CompareTimesWithinXXSeconds(SYSTEMTIME stA, SYSTEMTIME stB, int XXseconds)
{
#if WIN32
#define _SECOND ((int64) 10000000)

	FILETIME ftA, ftB;
	SystemTimeToFileTime(&stA, &ftA);
	SystemTimeToFileTime(&stB, &ftB);

	ULONGLONG qwA = (((ULONGLONG) ftA.dwHighDateTime) << 32) + ftA.dwLowDateTime;
	ULONGLONG qwB = (((ULONGLONG) ftB.dwHighDateTime) << 32) + ftB.dwLowDateTime;
	ULONGLONG qwDiff;
	if (qwA > qwB)
		qwDiff = qwA - qwB;
	else
		qwDiff = qwB - qwA;

	ULONGLONG qwSeconds = qwDiff / _SECOND;
#else
	time_t tA, tB;
	struct tm tm_A, tm_B;

	tm_A.tm_year = stA.wYear - 1900;
	tm_A.tm_mon = stA.wMonth - 1;
	tm_A.tm_mday = stA.wDay;
	tm_A.tm_hour = stA.wHour;
	tm_A.tm_min = stA.wMinute;
	tm_A.tm_sec = stA.wSecond;

	tm_B.tm_year = stB.wYear - 1900;
	tm_B.tm_mon = stB.wMonth - 1;
	tm_B.tm_mday = stB.wDay;
	tm_B.tm_hour = stB.wHour;
	tm_B.tm_min = stB.wMinute;
	tm_B.tm_sec = stB.wSecond;

	tA = mktime(&tm_A);
	tB = mktime(&tm_B);
	double qwSeconds;

	if (tB > tA)
		qwSeconds = difftime(tB, tA);
	else
		qwSeconds = difftime(tA, tB);
#endif //WIN32

	if (qwSeconds > XXseconds)
		return false;
	return true;
}

/*----------------------------------------------------------------------------------------------
	This routine returns TRUE if the caller's process is a member of the Administrators local
	group. Caller is NOT expected to be impersonating anyone and is expected to be able to open
	its own process and process token. This method is adapted from an example in MSDN.

	Return true if the Caller is in the Administrators local group, false otherwise.
----------------------------------------------------------------------------------------------*/
bool SilUtil::IsAdminUser()
{
#ifdef WIN32
	BOOL b;
	SID_IDENTIFIER_AUTHORITY NtAuthority = SECURITY_NT_AUTHORITY;
	PSID AdministratorsGroup;
	b = ::AllocateAndInitializeSid(&NtAuthority,
		2,
		SECURITY_BUILTIN_DOMAIN_RID,
		DOMAIN_ALIAS_RID_ADMINS,
		0, 0, 0, 0, 0, 0,
		&AdministratorsGroup);
	if (b)
	{
		if (!::CheckTokenMembership(NULL, AdministratorsGroup, &b))
		{
			b = false;
		}
		::FreeSid(AdministratorsGroup);
	}

	return b;
#else
	// TODO-Linux: port
	return false;
#endif
}

/*----------------------------------------------------------------------------------------------
	This routine returns TRUE if the caller's process is a member of the Power Users local
	group. Caller is NOT expected to be impersonating anyone and is expected to be able to open
	its own process and process token. This method is adapted from an example in MSDN.

	Return true if the Caller is in the Power Users local group, false otherwise.
----------------------------------------------------------------------------------------------*/
bool SilUtil::IsPowerUser()
{
#ifdef WIN32
	BOOL b;
	SID_IDENTIFIER_AUTHORITY NtAuthority = SECURITY_NT_AUTHORITY;
	PSID AdministratorsGroup;
	b = ::AllocateAndInitializeSid(&NtAuthority,
		2,
		SECURITY_BUILTIN_DOMAIN_RID,
		DOMAIN_ALIAS_RID_POWER_USERS,
		0, 0, 0, 0, 0, 0,
		&AdministratorsGroup);
	if (b)
	{
		if (!::CheckTokenMembership(NULL, AdministratorsGroup, &b))
		{
			b = false;
		}
		::FreeSid(AdministratorsGroup);
	}

	return b;
#else
	// TODO-Linux: port
	return false;
#endif
}

/*----------------------------------------------------------------------------------------------
	This routine returns true if the path looks like it is rooted, not relative.
----------------------------------------------------------------------------------------------*/
bool SilUtil::IsPathRooted(const wchar * pszPath)
{
	if (pszPath == NULL || *pszPath == 0)
		return false;
	if (pszPath[0] == '\\' || pszPath[0] == '/')
		return true;
#ifdef WIN32
	size_t cchPath = wcslen(pszPath);
	if (cchPath >= 3 && iswalpha(pszPath[0]) && pszPath[1] == ':' &&
		(pszPath[2] == '\\' || pszPath[2] == '/'))
	{
		return true;
	}
#endif
	return false;
}



/*----------------------------------------------------------------------------------------------
	This routine combines the root directory and the filename to form a full pathname.
----------------------------------------------------------------------------------------------*/
const wchar * SilUtil::PathCombine(const wchar * pszRootDir, const wchar * pszFile)
{
	if (SilUtil::IsPathRooted(pszFile))
		return pszFile;
	static wchar rgchCombined[4000];
	wcscpy_s(rgchCombined, 4000, pszRootDir);
	size_t cchLast = wcslen(pszRootDir);
	if (cchLast > 0)
		--cchLast;
	if (cchLast > 4000)
		cchLast = 3998;
	if (rgchCombined[cchLast] != '\\' && rgchCombined[cchLast] != '/')
#ifdef WIN32
		wcscat_s(rgchCombined, 4000, L"\\");
#else
	{
		static const OleStringLiteral slash = L"/";
		wcscat_s(rgchCombined, 4000, slash);
	}
#endif
	wcscat_s(rgchCombined, 4000, pszFile);
	return rgchCombined;
}


/*----------------------------------------------------------------------------------------------
	This routine checks whether the given file exists.
----------------------------------------------------------------------------------------------*/
bool SilUtil::FileExists(const wchar * pszPath)
{
	DWORD dwAtts = ::GetFileAttributesW(pszPath);
	if (dwAtts == INVALID_FILE_ATTRIBUTES)
	{
#ifdef WIN32 // TODO-Linux: ::GetLastError not implemented
		dwAtts = ::GetLastError();	// flush this from the system.
#endif
		return false;
	}
	else
	{
		return true;
	}
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkgenlib.bat"
// End: (These 4 lines are useful to Steve McConnel.)
