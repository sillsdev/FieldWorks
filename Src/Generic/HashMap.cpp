/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: HashMap.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This file contains the default implementations of the default hashing and equality
	functors for the various hash map collection classes.
----------------------------------------------------------------------------------------------*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Methods
***********************************************************************************************/
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Compute a hash value from the bits of an arbitrary object, and return the computed value.

	@param pKey Pointer to a block of memory (presumably an object of some sort).
	@param cbKey Number of bytes in the block of memory.
----------------------------------------------------------------------------------------------*/
int HashObj::operator () (void * pKey, int cbKey)
{
	if ((NULL == pKey) || (cbKey <= 0))
		return 0;
	int nHash = 0;
	int i;
	if (0 == (cbKey % isizeof(int)))
	{
		int cn = cbKey / isizeof(int);
		int * pn = (int *)pKey;
		for (i = 0; i < cn; ++i)
			nHash += (nHash << 4) + *pn++;
	}
	else if (0 == (cbKey % isizeof(short)))
	{
		int csu = cbKey / isizeof(short);
		ushort * psu = (ushort *)pKey;
		for (i = 0; i < csu; ++i)
			nHash += (nHash << 4) + *psu++;
	}
	else
	{
		byte * pb = (byte *)pKey;
		for (i = 0; i < cbKey; ++i)
			nHash += (nHash << 4) + *pb++;
	}
	return nHash;
}

/*----------------------------------------------------------------------------------------------
	Compare the bits of two objects for being the same, returning true if the two objects have
	exactly the same bits, and otherwise returning false.

	@param pKey1 Pointer to a block of memory (presumably an object of some sort).
	@param pKey2 Pointer to another block of memory (presumably an object of some sort).
	@param cbKey Number of bytes in each block of memory.
----------------------------------------------------------------------------------------------*/
bool EqlObj::operator () (void * pKey1, void * pKey2, int cbKey)
{
	if (pKey1 == pKey2)
		return true;
	if ((NULL == pKey1) || (NULL == pKey2))
		return false;
	if (cbKey <= 0)
		return true;
	return (0 == memcmp(pKey1, pKey2, cbKey));
}

/*----------------------------------------------------------------------------------------------
	Compute a hash value from the Unicode character data stored in a StrUni object, and return
	the computed value.

	@param stuKey Reference to a StrUni object.
----------------------------------------------------------------------------------------------*/
int HashStrUni::operator () (StrUni & stuKey)
{
	const wchar * pwsz = stuKey.Chars();
	int cchw = stuKey.Length();
	int nHash = 0;

	while (cchw--)
		nHash += (nHash << 4) + *pwsz++;
	return nHash;
}

/*----------------------------------------------------------------------------------------------
	Compute a hash value from the Unicode character data stored in a BSTR, and return the
	computed value.

	@param bstrKey Either a BSTR or a pointer to an array of Unicode characters.
	@param cchwKey Number of wide characters in bstrKey, which may be greater than the number
					of actual Unicode characters due to surrogate pairs.  (-1 means to use the
					size stored in the BSTR.)
----------------------------------------------------------------------------------------------*/
int HashStrUni::operator () (BSTR bstrKey, int cchwKey)
{
	if (!bstrKey)
		return 0;
	int cchw = BstrLen(bstrKey);
	if (cchwKey == -1)
		cchwKey = cchw;
	if (!cchwKey)
		return 0;
	Assert(cchwKey > 0 && cchwKey <= cchw);
	const wchar * pwsz = bstrKey;
	int nHash = 0;
	while (cchwKey--)
		nHash += (nHash << 4) + *pwsz++;
	return nHash;
}

/*----------------------------------------------------------------------------------------------
	Compute a hash value from the Unicode character data stored in a StrUni object, and return
	the computed value.  This method is designed to be compatible with the HashObj functor.

	@param pstuKey Pointer to a StrUni object.
	@param cbKey Number of bytes in a StrUni object.
----------------------------------------------------------------------------------------------*/
int HashStrUni::operator () (StrUni * pstuKey, int cbKey)
{
	AssertPtr(pstuKey);
	Assert(cbKey == isizeof(StrUni));
	return operator()(*pstuKey);
}

/*----------------------------------------------------------------------------------------------
	Compare the Unicode character data stored in two StrUni objects for lexical equality.
	Return true if the two Unicode strings are equal, otherwise return false.

	@param stuKey1 Reference to a StrUni object.
	@param stuKey2 Reference to another StrUni object.
----------------------------------------------------------------------------------------------*/
bool EqlStrUni::operator () (StrUni & stuKey1, StrUni & stuKey2)
{
	const wchar * pwsz1 = stuKey1.Chars();
	const wchar * pwsz2 = stuKey2.Chars();
	if (pwsz1 == pwsz2)
		return true;
	if ((NULL == pwsz1) || (NULL == pwsz2))
		return false;

	int cchw1 = stuKey1.Length();
	int cchw2 = stuKey2.Length();
	if (cchw1 != cchw2)
		return false;
	return (0 == memcmp(pwsz1, pwsz2, cchw1 * isizeof(wchar)));
}

/*----------------------------------------------------------------------------------------------
	Compare the Unicode character data stored in a StrUni object with that in a BSTR for lexical
	equality.  Return true if the two Unicode strings are equal, otherwise return false.

	@param stuKey1 Reference to a StrUni object.
	@param bstrKey2 Either a BSTR or a pointer to an array of Unicode characters.
	@param cchwKey2 Number of wide characters in bstrKey2, which may be greater than the number
					of actual Unicode characters due to surrogate pairs.  (-1 means to use the
					size stored in the BSTR.)
----------------------------------------------------------------------------------------------*/
bool EqlStrUni::operator () (StrUni & stuKey1, BSTR bstrKey2, int cchwKey2)
{
	const wchar * pwsz1 = stuKey1.Chars();
	if (pwsz1 == bstrKey2)
		return true;
	if (!pwsz1 || !bstrKey2)
		return false;
	int cchw1 = stuKey1.Length();
	int cchw2 = cchwKey2;
	if (cchw2 == -1)
		cchw2 = BstrLen(bstrKey2);
	if (cchw1 != cchw2)
		return false;
	return !memcmp(pwsz1, bstrKey2, cchw1 * isizeof(wchar));
}

/*----------------------------------------------------------------------------------------------
	Compare the Unicode character data stored in two StrUni objects for lexical equality,
	returning true if the two Unicode strings are equal, and otherwise returning false.
	This method is designed to be compatible with the EqlObj functor.

	@param pstuKey1 Pointer to a StrUni object.
	@param pstuKey2 Pointer to another StrUni object.
	@param cbKey Number of bytes in a StrUni object.
----------------------------------------------------------------------------------------------*/
bool EqlStrUni::operator () (StrUni * pstuKey1, StrUni * pstuKey2, int cbKey)
{
	AssertPtr(pstuKey1);
	AssertPtr(pstuKey2);
	Assert(cbKey == isizeof(StrUni));
	return operator()(*pstuKey1, *pstuKey2);
}

/*----------------------------------------------------------------------------------------------
	Compute a hash value from a C style NUL-terminated character (char) string, and return the
	computed value.

	@param pszKey Pointer to a C style NUL-terminated character (char) string.
----------------------------------------------------------------------------------------------*/
int HashChars::operator () (const char * pszKey)
{
	if (NULL == pszKey)
		return 0;
	int cb = strlen(pszKey);
	if (0 == cb)
		return 0;
	int nHash = 0;
	int i;
	for (i = 0; i < cb; ++i)
		nHash += (nHash << 4) + (byte)pszKey[i];
	return nHash;
}

/*----------------------------------------------------------------------------------------------
	Compute a hash value from a C style NUL-terminated character (char) string, and return the
	computed value.  This method is designed to be compatible with the HashObj functor.

	@param ppszKey Address of a pointer to a C style NUL-terminated character (char) string.
	@param cbKey Number of bytes in a string pointer.
----------------------------------------------------------------------------------------------*/
int HashChars::operator () (const char ** ppszKey, int cbKey)
{
	AssertPtr(ppszKey);
	Assert(cbKey == isizeof(const char *));
	return operator()(*ppszKey);
}

/*----------------------------------------------------------------------------------------------
	Compare two C style character string for lexical equality, returning true if the two
	strings are equal, and otherwise returning false.

	@param pszKey1 Pointer to a C style NUL-terminated character (char) string.
	@param pszKey2 Pointer to another C style NUL-terminated character (char) string.
----------------------------------------------------------------------------------------------*/
bool EqlChars::operator () (const char * pszKey1, const char * pszKey2)
{
	if (pszKey1 == pszKey2)
		return true;
	if ((NULL == pszKey1) || (NULL == pszKey2))
		return false;
	return (0 == strcmp(pszKey1, pszKey2));
}

/*----------------------------------------------------------------------------------------------
	Compare two C style character string for lexical equality, returning true if the two strings
	are equal, and otherwise returning false.  This method is designed to be compatible with the
	EqlObj functor.

	@param ppszKey1 Address of a pointer to a C style NUL-terminated character (char) string.
	@param ppszKey2 Address of a pointer to another C style string.
	@param cbKey Number of bytes in a string pointer.
----------------------------------------------------------------------------------------------*/
bool EqlChars::operator () (const char ** ppszKey1, const char ** ppszKey2, int cbKey)
{
	AssertPtr(ppszKey1);
	AssertPtr(ppszKey2);
	Assert(cbKey == isizeof(const char *));
	return operator()(*ppszKey1, *ppszKey2);
}
