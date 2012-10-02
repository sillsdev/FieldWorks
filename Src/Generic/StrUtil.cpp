/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2005 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

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

/*----------------------------------------------------------------------------------------------
	Initialize the ICU Data directory based on the registry setting (or UNIX environment).  It is safe to
	call this static method more than once, but necessary only to call it at least once.
----------------------------------------------------------------------------------------------*/
void InitIcuDataDir()
{
#if WIN32
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

#else //WIN32

	const char * pszDir = u_getDataDirectory();
	if (!pszDir || !*pszDir)
	{
		// The ICU Data Directory is not yet set. Set it from the environment.
		const char * desiredDir = getenv("ICUDATADIR");
		if (NULL != desiredDir)
			u_setDataDirectory(desiredDir);
	}

#endif//WIN32

	// ICU docs say to do this after the directory is set, but before others are called.
	// And it can be called n times with little hit, but is Required for multi-threaded
	// use of ICU.
	UErrorCode status = U_ZERO_ERROR;
	u_init(&status);

	if (status != U_ZERO_ERROR)
	{
		ThrowInternalError(E_UNEXPECTED, "Error Initalizing Icu. Check ICU_DATA is set.");
	}

	pszDir = u_getDataDirectory();
	if (!pszDir || !*pszDir)
	{
		ThrowInternalError(E_UNEXPECTED, "Error No Icu Data Directory. Check ICU_DATA is set.");
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