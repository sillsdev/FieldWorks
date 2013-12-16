/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: UtilInt.h
Responsibility: Steve McConnel (was Shon Katzenberger)
Last reviewed:

	Integer utilities.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef UtilInt_H
#define UtilInt_H 1


const int knMax = 0x7FFFFFFF;


#if defined(_MSC_VER)

/***********************************************************************************************
	Intel 80386 routines.
***********************************************************************************************/
#define MulDiv MulDivImp

inline int MulDiv(int n, int nMul, int nDiv)
{
	Assert(nDiv != 0);

	__asm
	{
		mov		eax,n
		imul	nMul
		idiv	nDiv
		mov		n,eax
	}
	return n;
}


inline int MulDivMod(int n, int nMul, int nDiv, int *pnRem)
{
	Assert(nDiv != 0);
	AssertPtr(pnRem);

	__asm
	{
		mov		eax,n
		imul	nMul
		idiv	nDiv
		mov		ecx,pnRem
		mov		DWORD PTR[ecx],edx
		mov		n,eax
	}
	return n;
}

#else // defined(_MSC_VER)

inline int MulDiv(int n, int nMul, int nDiv)
{
	Assert(nDiv != 0);
	return int64(n) * nMul / nDiv;
}

inline int MulDivMod(int n, int nMul, int nDiv, int *pnRem)
{
	Assert(nDiv != 0);
	AssertPtr(pnRem);

	int64 m = int64(n) * nMul;
	*pnRem = m % nDiv;
	return m / nDiv;
}

#endif // defined(_MSC_VER)


/***********************************************************************************************
	These arithmetic functions assert that the result doesn't overflow.
***********************************************************************************************/


/*----------------------------------------------------------------------------------------------
	Multiply two integers and assert on overflow.
----------------------------------------------------------------------------------------------*/
template<typename T>
	inline int Mul(T t1, T t2)
{
	Assert(!t1 || (t1 * t2) / t1 == t2);
	return t1 * t2;
}


/*----------------------------------------------------------------------------------------------
	Add two integers and assert on overflow.
----------------------------------------------------------------------------------------------*/
template<typename T>
	inline int Add(T t1, T t2)
{
	Assert((t1 + t2 < t2) == (t1 < 0));
	return t1 + t2;
}


/***********************************************************************************************
	Arithmetic functions.
***********************************************************************************************/


/*----------------------------------------------------------------------------------------------
	Return the floor(tNum / tDen) where floor(x) is defined as the the greatest integer
	that is less than or equal to the number. This only works for signed integer types.
----------------------------------------------------------------------------------------------*/
template<typename T>
	inline T FloorDiv(T tNum, T tDen)
{
	Assert(tDen != 0);
	return tNum / tDen - ((tNum ^ tDen) < 0 && (tNum % tDen));
}


/*----------------------------------------------------------------------------------------------
	Return the positive modulus of tNum mod tDen. That is, the return result is always
	non-negative congruent to tNum modulo tDen. This only works for signed integer types.
----------------------------------------------------------------------------------------------*/
template<typename T>
	inline T ModPos(T tNum, T tDen)
{
	Assert(tDen != 0);
	if (tNum < 0)
	{
		// return tNum % tDen + tDen;	// This can return tDen, which is WRONG! - SMc
		T t = tNum % tDen;
		if (t < 0)
			t += tDen;
		return t;
	}
	return tNum % tDen;
}


/*----------------------------------------------------------------------------------------------
	Return the absolute value of the given integer.
----------------------------------------------------------------------------------------------*/
inline uint Abs(int n)
{
	return n < 0 ? -n : n;
}


/*----------------------------------------------------------------------------------------------
	Compute the greatest common divisor of two values.
----------------------------------------------------------------------------------------------*/
uint GetGcdU(uint u1, uint u2);

inline int GetGcd(int n1, int n2)
{
	return GetGcdU(Abs(n1), Abs(n2));
}


/***********************************************************************************************
	Hash functions.
***********************************************************************************************/
uint ComputeHashRgb(const byte * prgb, int cb, uint uHash = 0);
uint CaseSensitiveComputeHash(LPCOLESTR psz, uint uHash = 0);
uint CaseSensitiveComputeHashCch(const OLECHAR * prgch, int cch, uint uHash = 0);
uint CaseInsensitiveComputeHash(LPCOLESTR psz, uint uHash = 0);
uint CaseInsensitiveComputeHashCch(const OLECHAR * prgch, int cch, uint uHash = 0);


/***********************************************************************************************
	Getting primes.
***********************************************************************************************/

// Looks for a prime near u. The primes are gotten from a table in Util.cpp.
uint GetPrimeNear(uint u);

// Looks for a prime larger than u. If u is larger than the largest prime in the table, we
// just return that largest prime.
uint GetLargerPrime(uint u);

// Looks for a prime smaller than u. If u is smaller than the smallest prime in the table,
// we just return that smallest prime.
uint GetSmallerPrime(uint u);


/***********************************************************************************************
	Max and Min.
***********************************************************************************************/
template<typename T> T Max(T t1, T t2)
{
	return (t1 >= t2) ? t1 : t2;
}


template<typename T> T Min(T t1, T t2)
{
	return (t1 <= t2) ? t1 : t2;
}


inline int NMax(int n1, int n2)
{
	return (n1 >= n2) ? n1 : n2;
}


inline int NMin(int n1, int n2)
{
	return (n1 <= n2) ? n1 : n2;
}


/*----------------------------------------------------------------------------------------------
	If t < tMin, this returns tMin. Otherwise if t > tMax, it returns tMax. Otherwise it
	returns t.
----------------------------------------------------------------------------------------------*/
template<typename T> T Bound(T t, T tMin, T tMax)
{
	return t < tMin ? tMin : t > tMax ? tMax : t;
}


inline int NBound(int n, int nMin, int nMax)
{
	return n < nMin ? nMin : n > nMax ? nMax : n;
}


/*----------------------------------------------------------------------------------------------
	This returns true iff tMin <= t && t < tLim.
----------------------------------------------------------------------------------------------*/
template<typename T> bool InInterval(T t, T tMin, T tLim)
{
	return tMin <= t && t < tLim;
}


/*----------------------------------------------------------------------------------------------
	Adjusts an index after an edit. *piv is the index to adjust, ivMin is the index where the
	edit occurred, ivLimDel is the (old) lim of deleted elements and cvIns is the number of
	things inserted. Returns true iff *piv is not in (ivMin, ivLimDel). If *piv is in this
	interval, mins *piv with ivMin + cvIns and returns false.
----------------------------------------------------------------------------------------------*/
inline bool FAdjustIndex(int * piv, int ivMin, int ivLimDel, int cvIns)
{
	AssertPtr(piv);
	Assert(0 <= ivMin && ivMin <= ivLimDel);
	Assert(0 <= cvIns);

	if (*piv <= ivMin)
		return true;

	if (*piv < ivLimDel)
	{
		*piv = NMin(*piv, ivMin + cvIns);
		return false;
	}

	*piv += cvIns - ivLimDel + ivMin;
	return true;
}


inline int SignedInt(OLECHAR ch)
{
	if (ch & 0x00008000)
	{
		// Negative number.
		int nRet = (ch | 0xFFFF0000);
		return nRet;
	}
	else
		return (int)ch;
}


/***********************************************************************************************
	Cookie validation and cookie to index mapping.
***********************************************************************************************/


/*----------------------------------------------------------------------------------------------
	Returns true iff hv is a valid cookie. A valid cookie is either 0 (the null cookie) or odd.
	This is so cookies can be easily distinguished from aligned pointers (which are always
	divisible by 4).
----------------------------------------------------------------------------------------------*/
inline bool ValidCookie(int hv)
{
	return !hv || (hv & 1);
}


/*----------------------------------------------------------------------------------------------
	Converts an index to a cookie.
----------------------------------------------------------------------------------------------*/
inline int CookieFromIndex(int iv)
{
	return (iv << 1) | 1;
}


/*----------------------------------------------------------------------------------------------
	Converts a cookie to an index.
----------------------------------------------------------------------------------------------*/
inline int IndexFromCookie(int hv)
{
	Assert(hv & 1);
	return hv >> 1;
}

#endif // !UtilInt_H
