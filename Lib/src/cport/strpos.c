/* STRPOS.C - find offset of the first occurrence of a character in a string
 *****************************************************************************
 *
 * int strpos(char * psz, int ch)
 *
 *****************************************************************************
 * Copyright 2000 by SIL International.  All rights reserved.
 */
#include <string.h>

/*****************************************************************************
 * NAME
 *    strpos
 * DESCRIPTION
 *    Search for the first occurrence of the character ch in the string psz.
 *    If the character ch is found in the string, the position
 *    of the first occurrence is returned (where the first character of psz
 *    is considered to be at position 0).  If the character is not found,
 *    the value -1 is returned.  The terminating NUL character is considered
 *    to be part of psz for the purposes of the search, so searching for NUL
 *    returns the position of the terminated NUL (which is equal to the
 *    length of the string), not the value -1.  strpos(psz,'\0') is therefore
 *    equivalent to strlen(psz).
 * RETURN VALUE
 *    position of the first occurrence of ch in psz, or -1 if ch does not
 *    occur in psz
 */
int strpos(const char * psz, int ch)
{
	const char * p;

	if (!psz)
		return -1;
	if (!ch)
		return strlen(psz);
	p = strchr(psz, ch);
	if (p)
		return p - psz;
	else
		return -1;
}
