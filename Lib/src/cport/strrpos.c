/* STRRPOS.C - find offset of the last occurrence of a character in a string
 *****************************************************************************
 *
 * int strrpos(char * psz, int ch)
 *
 *****************************************************************************
 * Copyright 2000 by SIL International.  All rights reserved.
 */
#include <string.h>

/*****************************************************************************
 * NAME
 *    strrpos
 * DESCRIPTION
 *    strrpos() searches the NUL-terminated string psz for the last
 *    occurrence of the character ch.  If the character ch is found in the
 *    string, the position of the last occurrence is returned (where the
 *    first character of psz is considered to be at position 0).  If the
 *    character is not found, the value -1 is returned.  The terminating
 *    NUL character is considered to be part of psz for the purposes of
 *    the search, so searching for NUL returns the position of the
 *    terminating NUL (which is equal to the length of the string), not
 *    the value -1.  strrpos(psz,'\0') is the same as strpos(psz,'\0'), and
 *    equivalent to strlen(psz).
 * RETURN VALUE
 *    position of the last occurrence of ch in psz, or -1 if ch does not
 *    occur in psz
 */
int strrpos(const char * psz, int ch)
{
const char * p;

if (!psz)
	return -1;
if (!ch)
	return strlen(psz);

p = strrchr(psz, ch);
if (!p)
	return -1;
else
	return p - psz;
}
