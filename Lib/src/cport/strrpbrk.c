/* STRRPBRK.C - find the last occurrence in 1st string of any char from 2nd
 *****************************************************************************
 *
 * char *strrpbrk(char * psz, char * pszSet)
 *
 *****************************************************************************
 * Copyright 2000 by SIL International.  All rights reserved.
 */

#ifndef NULL
#define NULL (char *)0
#endif

/************************************************************************
 * NAME
 *    strrpbrk
 * ARGUMENTS
 *    psz - address of NUL-terminated character string
 *    pszSet - address of NUL-terminated set of characters to search for
 * DESCRIPTION
 *    strrpbrk() searches the NUL-terminated string psz for occurrences of
 *    characters from the NUL-terminated string pszSet.  The second argument
 *    is regarded as a set of characters; the order of the characters, or
 *    whether there are duplications, does not matter.  If such a character
 *    is found within psz, then a pointer to the last such character is
 *    returned.  If no character within psz occurs in pszSet, then a null
 *    character pointer (NULL) is returned.  See also strpbrk(), which
 *    searches for the first character in psz that is also in pszSet.
 * RETURN VALUE
 *    address of the last occurrence in psz of any character from pszSet,
 *    or NULL if no character from pszSet occurs in psz
 */
char * strrpbrk(const char * psz, const char * pszSet)
{
	const char * pch;
	char * pszRet;
	char ch;

	if (!psz || !pszSet)
		return NULL;

	for (pszRet = NULL; *psz; ++psz)
	{
		for (pch = pszSet, ch = *psz; *pch; ++pch)
		{
			if (*pch == ch)
			{
				pszRet = (char *)psz;
				break;
			}
		}
	}
	return pszRet;
}
