/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: UtilInt.h
Responsibility: Steve McConnel (was Shon Katzenberger)
Last reviewed:

	Integer utilities.
----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef UtilInt_H
#define UtilInt_H 1

#include "GrDebug.h"
namespace gr
{

const int knMax = 0x7FFFFFFF;


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
	Return the absolute value of the given integer.
----------------------------------------------------------------------------------------------*/
inline unsigned int Abs(int n)
{
	return n < 0 ? -n : n;
}

/***********************************************************************************************
	Hash functions.
***********************************************************************************************/
/*
unsigned int ComputeHashRgb(const byte * prgb, int cb, unsigned int uHash = 0);
unsigned int CaseSensitiveComputeHash(LPCOLESTR psz, unsigned int uHash = 0);
unsigned int CaseSensitiveComputeHashCch(const OLECHAR * prgch, int cch, unsigned int uHash = 0);
unsigned int CaseInsensitiveComputeHash(LPCOLESTR psz, unsigned int uHash = 0);
unsigned int CaseInsensitiveComputeHashCch(const OLECHAR * prgch, int cch, unsigned int uHash = 0);
*/

/***********************************************************************************************
	Getting primes.
***********************************************************************************************/

// Looks for a prime near u. The primes are gotten from a table in Util.cpp.
unsigned int GetPrimeNear(unsigned int u);

// Looks for a prime larger than u. If u is larger than the largest prime in the table, we
// just return that largest prime.
unsigned int GetLargerPrime(unsigned int u);

// Looks for a prime smaller than u. If u is smaller than the smallest prime in the table,
// we just return that smallest prime.
unsigned int GetSmallerPrime(unsigned int u);


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


inline int SignedInt(wchar_t ch)
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

}// namespace gr

#endif // !UtilInt_H
