/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: StrAnsiHash.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Functor classes to allow us to use StrAnsi as the key of a hash map.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef STRHASH_INCLUDED
#define STRHASH_INCLUDED


/*----------------------------------------------------------------------------------------------
	Functor class for computing a hash value from a StrAnsi object (ANSI string).
	Modelled after HashStrUni functor class.
----------------------------------------------------------------------------------------------*/
class HashStrAnsi
{
public:
	// Constructors/destructors/etc.

	HashStrAnsi()
	{
	}
	~HashStrAnsi()
	{
	}

	// Other public methods

	int operator () (StrAnsi & staKey)
	{
		const schar * psz = staKey.Chars();
		int cchs = staKey.Length();
		int nHash = 0;

		while (cchs--)
			nHash += (nHash << 4) + *psz++;
		return nHash;
	}

//	int operator () (BSTR bstrKey, int cchwKey = -1);

	int operator () (StrAnsi * pstaKey, int cbKey)
	{
		Assert(pstaKey);
		Assert(cbKey == isizeof(StrAnsi));
		return operator()(*pstaKey);
	}
};

/*----------------------------------------------------------------------------------------------
	Functor class for comparing two StrAnsi objects (ANSI strings) for equality.
	Modelled after EqlStrUni functor class.
----------------------------------------------------------------------------------------------*/
class EqlStrAnsi
{
public:
	// Constructors/destructors/etc.

	EqlStrAnsi()
	{
	}
	~EqlStrAnsi()
	{
	}

	// Other public methods

	bool operator () (StrAnsi & staKey1, StrAnsi & staKey2)
	{
		const schar * psz1 = staKey1.Chars();
		const schar * psz2 = staKey2.Chars();
		if (psz1 == psz2)
			return true;
		if ((NULL == psz1) || (NULL == psz2))
			return false;

		int cchs1 = staKey1.Length();
		int cchs2 = staKey2.Length();
		if (cchs1 != cchs2)
			return false;
		return (0 == memcmp(psz1, psz2, cchs1 * isizeof(schar)));
	}

//	bool operator () (StrAnsi & staKey1, BSTR bstrKey2, int cchwKey2 = -1);

	bool operator () (StrAnsi * pstaKey1, StrAnsi * pstaKey2, int cbKey)
	{
		Assert(pstaKey1);
		Assert(pstaKey2);
		Assert(cbKey == isizeof(StrAnsi));
		return operator()(*pstaKey1, *pstaKey2);
	}

};


#endif // STRHASH_INCLUDED
