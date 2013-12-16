/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: UtilView.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This file provides various code fragments useful to code both implementing and using the
	views subsystem.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef UTIL_VIEW_INCLUDED
#define UTIL_VIEW_INCLUDED 1
/*----------------------------------------------------------------------------------------------
	Return -1, 0, or 1 according to whether the first guid is <, =, or > the other.
	How the comparison is done is arbitrary, we just want to impose an order and use it
	consistently. This comparison is used for char sequences in text tagging (TsTextProps
	string property ktptTags) to impose a consistent order on the guids associated
	with a particular run of text.
----------------------------------------------------------------------------------------------*/
inline int CompareGuids(OLECHAR * prgchGuid1, OLECHAR * prgchGuid2)
{
	OLECHAR * pchLim = prgchGuid1 + kcchGuidRepLength;
	for (; prgchGuid1 < pchLim && *prgchGuid1 == *prgchGuid2; prgchGuid1++, prgchGuid2++)
		;
	if (prgchGuid1 >= pchLim)
		return 0;
	if (*prgchGuid1 < *prgchGuid2)
		return -1;
	return 1;
}
#endif // !UTIL_VIEW_INCLUDED
