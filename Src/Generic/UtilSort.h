/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilSort.h
Responsibility: Shon Katzenberger
Last reviewed:

	This implements an indirect sort algorithm.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef UtilSort_H
#define UtilSort_H 1

/*----------------------------------------------------------------------------------------------
	This requires that the < operator be defined on the type T.
----------------------------------------------------------------------------------------------*/
template<typename T>
	inline void SortIndirect(T * prgv, int cv, int * prgiv)
{
	AssertArray(prgv, cv);
	AssertArray(prgiv, cv);

	int iv;

	for (iv = cv; --iv >= 0; )
		prgiv[iv] = iv;

	if (cv <= 1)
		return;

	// The recursion depth is <= 1 + log2(size) so 33 is sufficient for any number of entries.
	int * rgpivMin[33];
	int * rgpivLim[33];
	int ipivStack = 0;

	int * pivMin = prgiv;
	int * pivLim = prgiv + cv;

	for (;;)
	{
		if (pivLim - pivMin <= 8)
		{
			// Do an n^2 sort.
			while (++pivMin < pivLim)
			{
				int * pivLo = pivMin - 1;
				for (int * piv = pivMin; piv < pivLim; piv++)
				{
					if (prgv[*piv] < prgv[*pivLo])
						pivLo = piv;
				}
				if (pivLo != pivMin - 1)
					SwapVars(*pivLo, pivMin[-1]);
			}
		}
		else
		{
			// Do quicksort.
			int * pivLast = pivLim - 1;
			int * pivMid = pivMin + (pivLim - pivMin) / 2;

			// Sort *pivMid, *pivMin, *pivLast in that order so that the partition element
			// (the median) is at pivMin, the smallest value is at pivMid and the largest
			// is at pivLast.
			if (prgv[*pivMin] < prgv[*pivMid])
				SwapVars(*pivMin, *pivMid);
			if (prgv[*pivLast] < prgv[*pivMid])
				SwapVars(*pivLast, *pivMid);
			if (prgv[*pivLast] < prgv[*pivMin])
				SwapVars(*pivLast, *pivMin);

			// The partition value is now at pivMin.
			T * pvKey = prgv + *pivMin;

			// These conditions guarantee that the inner while loops don't go too far.
			Assert(!(*pvKey < prgv[*pivMin]));
			Assert(!(prgv[*pivLast] < *pvKey));

			int * pivLo = pivMin;
			int * pivHi = pivLast;

			for (;;)
			{
				Assert(pivMin <= pivLo && pivLo < pivLast);
				Assert(pivMin < pivHi && pivHi <= pivLast);
				Assert(!(*pvKey < prgv[*pivLo]));
				Assert(!(prgv[*pivHi] < *pvKey));

				while (prgv[*++pivLo] < *pvKey)
					Assert(pivLo < pivLim - 1);
				Assert(pivMin < pivLo && pivLo <= pivLast && !(prgv[*pivLo] < *pvKey));

				while (*pvKey < prgv[*--pivHi])
					Assert(pivMin < pivHi);
				Assert(pivMin <= pivHi && pivHi < pivLast && !(*pvKey < prgv[*pivHi]));

				if (pivLo > pivHi)
					break;

				// These asserts are satisfied since pivMin < pivLo <= pivHi < pivLast.
				Assert(pivLo < pivLast);
				Assert(pivMin < pivHi);

				SwapVars(*pivLo, *pivHi);
			}
			Assert(pivMin <= pivHi && pivHi < pivLo && pivLo <= pivLast);
			Assert(!(prgv[*pivLo] < *pvKey));
			Assert(!(*pvKey < prgv[*pivHi]));

			// Put the partition value at pivHi.
			SwapVars(*pivMin, *pivHi);

			// Save the big side for later and "recurse" on the small side.
			if (pivHi - pivMin <= pivLim - pivLo)
			{
				if (pivLim - pivLo > 1)
				{
					rgpivMin[ipivStack] = pivLo;
					rgpivLim[ipivStack] = pivLim;
					ipivStack++;
				}
				if (pivHi - pivMin > 1)
				{
					pivLim = pivHi;
					continue;
				}
			}
			else
			{
				if (pivHi - pivMin > 1)
				{
					rgpivMin[ipivStack] = pivMin;
					rgpivLim[ipivStack] = pivHi;
					ipivStack++;
				}
				if (pivLim - pivLo > 1)
				{
					pivMin = pivLo;
					continue;
				}
			}
		}

		if (--ipivStack < 0)
			break;
		pivMin = rgpivMin[ipivStack];
		pivLim = rgpivLim[ipivStack];
	}
}

#endif // !UtilSort_H
