/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2010 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: StringToNumHelpers.h
Responsibility: Calgary
Last reviewed:

-------------------------------------------------------------------------------*//*:End Ignore*/

#pragma once
#ifndef _STRINGTONUMHELPERS_H_
#define _STRINGTONUMHELPERS_H_

/*----------------------------------------------------------------------------------------------
	Convert an explicit UTF16 string to a unsigned long
	On windows this is just a wrapper for wcstoul

	@param string The string to convert to long
	@param result ptr to error character or null terminator.
	@param base (eg. base 10 or base 16)
----------------------------------------------------------------------------------------------*/
inline unsigned long Utf16StrToUL(const OLECHAR * string, const OLECHAR ** result, int base)
{
#if WIN32
	return wcstoul(string, const_cast<OLECHAR **>(result), base);
#else
	// TODO-Linux: Improve - Convert psz to UTF32 and use wcstoul
	char buf[256];
	u_austrcpy(buf, string);
	char * endptr;
	unsigned long ret = strtoul(buf, &endptr, base);
	if (ret == 0 || *endptr != '\0')
		*result = string; // TODO-Linux: should really store ptr to first invalid char.
	else
		*result = NULL;

	return ret;
#endif
}

/*----------------------------------------------------------------------------------------------
	Convert an explicit UTF16 string to a unsigned long

	@param string The string to convert to long
	@param base (eg. base 10 or base 16)
----------------------------------------------------------------------------------------------*/
inline unsigned long Utf16StrToUL(const OLECHAR * string,  int base)
{
	const OLECHAR * result;
	return Utf16StrToUL(string, &result, base);
}

/*----------------------------------------------------------------------------------------------
	Convert an explicit UTF16 string to a long
	On windows this is just a wrapper for wcstol

	@param string The string to convert to long
	@param result ptr to error character or null terminator.
	@param base (eg. base 10 or base 16)
----------------------------------------------------------------------------------------------*/
inline long Utf16StrToL(const OLECHAR * string, const OLECHAR ** result, int base)
{
#if WIN32
	return wcstol(string, const_cast<OLECHAR **>(result), base);
#else
	return Utf16StrToUL(string, result, base);
#endif
}

#endif //_STRINGTONUMHELPERS_H_
