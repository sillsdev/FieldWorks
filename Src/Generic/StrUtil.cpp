/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2005 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilString.cpp
Responsibility: LarryW
Last reviewed: 27Sep99

	Code for string utilities.
----------------------------------------------------------------------------------------------*/

#include <Windows.h>
#include <ctype.h>
#include "IcuCommon.h"

// Should we use a named mutex instead of a critical section? There are multiple DLLs that
// statically link in this code. This creates a separate instance of the critical section
// structure for each DLL, so access to ICU is not completely synchronized across all DLLs.
// A named mutex ensures that the same mutex is used across all DLLs, but would introduce a
// small performance hit.
//
// As of 4/2009, there are only two threads used in FW, the main thread and the
// parser thread. The parser calls ICU through the Language.dll, using the C# ICU wrapper class
// in FwUtils.dll. The main thread restarts ICU in a number of different DLLs, but primarily
// using the IcuCleanupManager class in the FwKernel.dll. This is the reason why we must
// ensure synchronization across multiple DLLs. At some point, we should probably move these ICU
// functions to the FwKernel.dll, so that it would not be necessary to use a named mutex.
#define USE_MUTEX

#define	START_BE_SAFE		InitAndLockIcuMutex();	__try {
#define END_BE_SAFE		} __finally { ReleaseIcuMutex(); }

//:>********************************************************************************************
//:>	StrUtil methods.
//:>********************************************************************************************

typedef wchar_t wchar;
namespace StrUtil
{

#ifdef USE_MUTEX
static HANDLE m_hMutex = 0;
#else
static CRITICAL_SECTION m_csICU;	// critical section for ICU
#endif
static long m_bFirstCall = 1;

void InitAndLockIcuMutex()
{
#ifdef USE_MUTEX
	long oldValue = InterlockedCompareExchange(&m_bFirstCall, 2, 1);	// if we're one go to two
	while (m_bFirstCall != 3)
	{
		if (oldValue == 1 && m_bFirstCall == 2)
		{
			m_hMutex = CreateMutex(NULL, FALSE, "FW_ICU_MUTEX");
			InterlockedCompareExchange(&m_bFirstCall, 3, 2);	// at value of 3, know it's created
			break;
		}
		Sleep(0);
	}
	WaitForSingleObject(m_hMutex, INFINITE);
#else
	// TODO: this code is not completly safe for initial creation
	if (m_bFirstCall == 1)
	{
		m_bFirstCall = 0;
		InitializeCriticalSection(&m_csICU);
	}
	EnterCriticalSection(&m_csICU);
#endif
}


void ReleaseIcuMutex()
{
#ifdef USE_MUTEX
	ReleaseMutex(m_hMutex);
#else
	LeaveCriticalSection(&m_csICU);
#endif
}

/*----------------------------------------------------------------------------------------------
	Initialize the ICU Data directory based on the registry setting.  It is safe to
	call this static method more than once, but necessary only to call it at least once.
----------------------------------------------------------------------------------------------*/
void InitIcuDataDir()
{
	START_BE_SAFE

	const char * pszDir = u_getDataDirectory();
	if (!pszDir || !*pszDir)
	{
		// The ICU Data Directory is not yet set.  Get the root directory from the registry
		// and set the ICU data directory based on that value.
		HKEY hk;
		long lRet = ::RegOpenKeyExA(HKEY_LOCAL_MACHINE, "Software\\SIL", 0, KEY_QUERY_VALUE,
			&hk);
		if (lRet == ERROR_SUCCESS)
		{
			char rgch[MAX_PATH];
			DWORD cb = sizeof(rgch);
			DWORD dwT;
			// Note: trying to refactor this using
			// StrAnsi staIcuDir(DirectoryFinder::IcuDir());
			// u_setDataDirectory(staIcuDir.Chars());
			// broke Ecobj/Teso. At some point we might want to figure out how to solve the build problems.
			lRet = ::RegQueryValueExA(hk, "Icu40DataDir", NULL, &dwT, (BYTE *)rgch, &cb);
			if (lRet == ERROR_SUCCESS && dwT == REG_SZ)
			{
				// Remove any trailing \ from the registry value.
				int cch = strlen(rgch);
				if (rgch[cch - 1] == '\\')
					rgch[cch - 1] = 0;
				u_setDataDirectory(rgch);
			}
			::RegCloseKey(hk);
		}
	}
	// ICU docs say to do this after the directory is set, but before others are called.
	// And it can be called n times with little hit, but is Required for multi-threaded
	// use of ICU.
	UErrorCode status = U_ZERO_ERROR;
	u_init(&status);
	END_BE_SAFE
}


/*----------------------------------------------------------------------------------------------
	Close down the resources used in the ICU library.  This method is to be used with caution
	according to the ICU docs.  This method is wrapped here so we can start to get a handle on
	it's usage and syncronize it's access.
	WARNNING: Do NOT use this function directly if running in the main FW process. Rather,
	create an instance of IIcuCleanupManager and call its Cleanup() method. This allows
	objects that retain ICU objects to be notified of the cleanup.
----------------------------------------------------------------------------------------------*/
void IcuCleanup()
{
	START_BE_SAFE
	u_cleanup();
	END_BE_SAFE
}

void RestartIcu()
{
	START_BE_SAFE
	IcuCleanup();
	InitIcuDataDir();
	END_BE_SAFE
}

char* IcuGetDataDirectory()
{
	START_BE_SAFE
	return (char*) u_getDataDirectory();
	END_BE_SAFE
}

/*----------------------------------------------------------------------------------------------
	Compare two strings using a collator, returning indication of where they differ.
	Note: Both strings are assumed to have the same normalization. Comparison level is
	determined by the collator passed in.
	@param prgchA Pointer to first array of characters.
	@param cchA Length of first array.
	@param prgchB Pointer to second array of characters.
	@param cchB Length of second array.
	@param prbc Pointer to the ICU collator used for comparion.
	@param pchMatched Optional pointer to receive the number of collation elements that matched.
		If this is NULL, the value is not returned.
		Note: this is not necessarily characters or 16-bit code points.
	@return A number indicating equality:
		< 0 means prgchA < prgchB
		> 0 means prgchA > prgchB
		= 0 means prgchA == prgchB
----------------------------------------------------------------------------------------------*/
int Compare(const OLECHAR * prgchA, int cchA, const OLECHAR * prgchB, int cchB,
	RuleBasedCollator * prbc, int * pcMatched)
{
	UCharCharacterIterator ucciA(prgchA, cchA);
	UCharCharacterIterator ucciB(prgchB, cchB);
	CollationElementIterator * ceiA = prbc->createCollationElementIterator(ucciA);
	CollationElementIterator * ceiB = prbc->createCollationElementIterator(ucciB);
	int cele = 0;
	int orderA = 0;
	int orderB = 0;
	UErrorCode uerr = U_ZERO_ERROR;
	while (U_SUCCESS(uerr))
	{
		orderA = ceiA->next(uerr);
		orderB = ceiB->next(uerr);
		if (orderA != orderB)
			break; // Strings differ at this point.
		if (orderA == CollationElementIterator::NULLORDER ||
			orderB == CollationElementIterator::NULLORDER)
		{
			break; // Reached the end of one string
		}
		++cele;
	}
	delete ceiA;
	delete ceiB;
	if (pcMatched)
		*pcMatched = cele;
	return orderA - orderB;
}


/*----------------------------------------------------------------------------------------------
	Trim spaces from beginning of string.

	@h3{Return value}
	@code{
		pointer to trimmed string.
	}
----------------------------------------------------------------------------------------------*/
const char * SkipLeadingWhiteSpace(const char * psz)
{
	while (isascii(*psz) && isspace(*psz))
		psz++;
	return psz;
}

const wchar * SkipLeadingWhiteSpace(const wchar * psz)
{
	UnicodeString us(psz);
	int ich;
	bool fSurr;
	for (ich=0; ich < us.length(); ich++)
	{
		fSurr = false;
		UChar32 ch = us.charAt(ich);
		if (U16_IS_SURROGATE(ch))
		{
			fSurr = true;
			ch = us.char32At(ich);
		}
		if (! u_isUWhiteSpace(ch))
			break;
		if (fSurr)
			ich++;
	}
	return psz + ich;
}

/*----------------------------------------------------------------------------------------------
	Trim spaces from end of string.

	@h3{Return value}
	@code{
		length of trimmed string.
	}
----------------------------------------------------------------------------------------------*/
unsigned LengthLessTrailingWhiteSpace(const char * psz)
{
	const char * pszLim = psz + strlen(psz);
	while (pszLim > psz && isascii(pszLim[-1]) && isspace(pszLim[-1]))
		pszLim--;

	return pszLim - psz;
}

unsigned LengthLessTrailingWhiteSpace(const wchar * psz)
{
	UnicodeString us(psz);
	int ich;
	bool fSurr;
	for (ich=us.length(); ich > 0; --ich)
	{
		fSurr = false;
		UChar32 ch = us.charAt(ich-1);
		if (U16_IS_SURROGATE(ch))
		{
			fSurr = true;
			if (ich > 1)
				ch = us.char32At(ich-2);
		}
		if (! u_isUWhiteSpace(ch))
			break;
		if (fSurr)
			--ich;
	}
	return ich;
}

};
