/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: UtilString.cpp
Responsibility: LarryW
Last reviewed: 27Sep99

	Code for string utilities.
----------------------------------------------------------------------------------------------*/

#include "common.h"

//:>********************************************************************************************
//:>	StrUtil methods.
//:>********************************************************************************************

namespace StrUtil
{
	// On Linux, the ICU_DATA environment variable is defined to specify where to find the ICU
	// data.  The internal ICU code also uses this environment variable to initialize where it
	// finds its data.  So when we first try to initialize ICU in FieldWorks, it may already
	// be halfway there.  So this variable flags that we really do need to call SilInitIcu()
	// even though u_setDataDirectory() may have already been called.
	static bool s_fSilIcuInitCalled = false;

#if WIN32
bool GetIcuDir(HKEY hkRoot, char* dir, DWORD size)
{
	bool fRes = false;
	HKEY hk;
	long lRet = ::RegOpenKeyExA(hkRoot, "Software\\SIL", 0, KEY_QUERY_VALUE, &hk);
	if (lRet == ERROR_SUCCESS)
	{
		DWORD dwType;
		long lRet = ::RegQueryValueExA(hk, "Icu50DataDir", NULL, &dwType, (BYTE*) dir, &size);
		if (lRet == ERROR_SUCCESS && dwType == REG_SZ)
			fRes = true;
		::RegCloseKey(hk);
	}
	return fRes;
}
#endif

/*----------------------------------------------------------------------------------------------
	Initialize the ICU Data directory based on the registry setting (or UNIX environment).  It is safe to
	call this static method more than once, but necessary only to call it at least once.
----------------------------------------------------------------------------------------------*/
void InitIcuDataDir()
{
	const char * pszDir = u_getDataDirectory();
	char rgchDataDirectory[MAX_PATH];
#if WIN32
	if (!pszDir || !*pszDir)
	{
		// The ICU Data Directory is not yet set.  Get the root directory from the registry
		// and set the ICU data directory based on that value.
		DWORD dwSize = sizeof(rgchDataDirectory);
		bool fRetrievedDir = GetIcuDir(HKEY_CURRENT_USER, rgchDataDirectory, dwSize);
		if (!fRetrievedDir)
			fRetrievedDir = GetIcuDir(HKEY_LOCAL_MACHINE, rgchDataDirectory, dwSize);

		if (fRetrievedDir)
		{
			// Remove any trailing \ from the registry value.
			int cch = strlen(rgchDataDirectory);
			if (rgchDataDirectory[cch - 1] == '\\')
				rgchDataDirectory[cch - 1] = 0;
			u_setDataDirectory(rgchDataDirectory);
			s_fSilIcuInitCalled = false;	// probably redundant, but to be safe ...
		}
	}
#else //not WIN32
	if (!pszDir || !*pszDir)
	{
		// The ICU Data Directory is not yet set. Set it from the environment.
		pszDir = getenv("ICU_DATA");
		if (NULL != pszDir)
		{
			u_setDataDirectory(pszDir);
			s_fSilIcuInitCalled = false;	// probably redundant, but to be safe ...
		}
	}
#endif//WIN32

	// ICU docs say to do this after the directory is set, but before others are called.
	// And it can be called n times with little hit, but is Required for multi-threaded
	// use of ICU.
	UErrorCode status = U_ZERO_ERROR;
	u_init(&status);

	if (status != U_ZERO_ERROR)
	{
#if WIN32
		ThrowInternalError(E_UNEXPECTED, "Error Initalizing Icu. Check HKLM\\Software\\SIL\\Icu50DataDir is set in the registry.");
#else
		ThrowInternalError(E_UNEXPECTED, "Error Initalizing Icu. Check ICU_DATA is set.");
#endif
	}

	pszDir = u_getDataDirectory();
	if (!pszDir || !*pszDir)
	{
#if WIN32
		ThrowInternalError(E_UNEXPECTED, "Error No Icu Data Directory. Check HKLM\\Software\\SIL\\Icu50DataDir is set in the registry.");
#else
		ThrowInternalError(E_UNEXPECTED, "Error No Icu Data Directory. Check ICU_DATA is set.");
#endif
	}

	if (!s_fSilIcuInitCalled)
	{
		// This is somewhat time consuming; it has to allocate memory and read a file. Do it only once.
		StrAnsi staPath(pszDir);
		staPath.Append("/"); // Works on all OS's we care about, I think. Not sure about Mac OS.
		staPath.Append("UnicodeDataOverrides.txt");
		SilIcuInit(staPath.Chars());
		s_fSilIcuInitCalled = true;
	}
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
