/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ComVector.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This file provides the method definitions for BaseOfComVecter, the class on which all
	ComVector<IFoo> template collection classes are based.
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

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
	BaseOfComVector::BaseOfComVector()
{
	m_peFirst = NULL;
	m_ce = 0;
	m_ceAlloc = 0;
	m_ceGrowthFactor = -kceDefaultInitialGrowthFactor;
	m_fFreeOnExit = true;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
BaseOfComVector::~BaseOfComVector()
{
	if (m_fFreeOnExit)
		Clear();
}

/*----------------------------------------------------------------------------------------------
	Remove the last element from the ComVector.  If the provided pointer is not NULL, copy the
	stored COM interface pointer to that location and don't call Release.  Otherwise, Release
	is called before the memory location is cleared.
----------------------------------------------------------------------------------------------*/
bool BaseOfComVector::Pop(IUnknown ** ppunkRet)
{
	AssertObj(this);
	AssertPtrN(ppunkRet);
	if (m_ce > 0)
	{
		IUnknown ** peLast = m_peFirst + m_ce - 1;
		if (NULL != ppunkRet)
			*ppunkRet = *peLast;
		else
			_DestroyRange(peLast, peLast + 1);
		--m_ce;
		return true;
	}
	else
		return false;
}

/*----------------------------------------------------------------------------------------------
	Ensure that the ComVector can add at least the given number of elements before another
	memory allocation will be needed.  If the second argument is 'true', then make sure that
	exactly the given number of elements can be added before allocating memory.
	Note that "EnsureSpace(0, true)" can be used to free unused memory when a ComVector has
	reached its maximum size.
----------------------------------------------------------------------------------------------*/
void BaseOfComVector::EnsureSpace(int ce, bool fFitExactly)
{
	AssertObj(this);
	Assert(ce >= 0);
	int ceUnused = m_ceAlloc - m_ce;
	int ceAllowNew = m_ceAlloc;
	int ceGrow;
	if (!fFitExactly)
	{
		// REVIEW SteveMc: is this the correct allocation strategy algorithm?
		if (ce <= ceUnused)
			return;
		if (m_ceGrowthFactor == 0)
			m_ceGrowthFactor = -kceDefaultInitialGrowthFactor;
		if (m_ceGrowthFactor < 0)
			ceGrow = -m_ceGrowthFactor;
		else
			ceGrow = m_ceGrowthFactor;
		ceAllowNew += ceGrow;
		if (ce > ceAllowNew - m_ce)
			ceAllowNew = m_ce + ce + ceGrow;
	}
	else if (ceUnused != ce)
	{
		ceAllowNew = m_ce + ce;
		// admittedly, the following is somewhat paranoid. :-)
		if (ceAllowNew == 0)
		{
			if (m_peFirst != NULL)
			{
				ceGrow = m_ceGrowthFactor;
				Clear();
				m_ceGrowthFactor = ceGrow;
			}
			return;
		}
	}
	if (ceAllowNew != m_ceAlloc)
	{
		int cbAlloc = ceAllowNew * isizeof(IUnknown *);
		IUnknown ** peNewFirst;
		if (m_peFirst)
			peNewFirst = (IUnknown **)realloc(m_peFirst, cbAlloc);
		else
			peNewFirst = (IUnknown **)malloc(cbAlloc);
		if (peNewFirst == NULL)
			ThrowHr(WarnHr(E_OUTOFMEMORY));
		m_peFirst = peNewFirst;
		m_ceAlloc = ceAllowNew;
		if (m_ceGrowthFactor < 0)
			m_ceGrowthFactor *= 2;
	}
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Change the number of elements in the ComVector to the given count.  This may entail adding
	more elements (using the given COM interface pointer, and calling AddRef as often as needed
	if that pointer is not NULL), or removing elements (calling Release for all non-NULL stored
	pointers that are removed).
----------------------------------------------------------------------------------------------*/
void BaseOfComVector::Resize(int ce, IUnknown * punk)
{
	AssertObj(this);
	if (ce < 0)
		ThrowHr(WarnHr(E_INVALIDARG));
	if (m_ce < ce)
		_InsertMultiple(m_peFirst + m_ce, ce - m_ce, punk);
	else if (ce < m_ce)
		_Erase(m_peFirst + ce, m_peFirst + m_ce);
}

/*----------------------------------------------------------------------------------------------
	Add an element at the given location in the ComVector, first shifting the following
	elements up one place to make room.  AddRef is called if the added COM interface pointer
	is not NULL.
----------------------------------------------------------------------------------------------*/
void BaseOfComVector::Insert(int ie, IUnknown * punk)
{
	AssertObj(this);
	Assert((0 <= ie) && (ie <= m_ce));
	_InsertMultiple(m_peFirst + ie, 1, punk);
}

/*----------------------------------------------------------------------------------------------
	Remove the element at the given location in the ComVector (calling Release if it is not
	NULL), shifting the following elements to fill in the gap.
----------------------------------------------------------------------------------------------*/
void BaseOfComVector::Delete(int ie)
{
	AssertObj(this);
	Assert((0 <= ie) & (ie < m_ce));
	IUnknown ** pe = m_peFirst + ie;
	_DestroyRange(pe, pe + 1);
	int ieLim = ie + 1;
	if (ieLim < m_ce)
		memmove(pe, pe + 1, (m_ce - ieLim) * isizeof(IUnknown *));
	--m_ce;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Remove all the elements from the ComVector (calling Release for each non-NULL element), and
	free all memory allocated for the ComVector.
	REVIEW SteveMc: policy for resetting m_ceGrowthFactor
----------------------------------------------------------------------------------------------*/
void BaseOfComVector::Clear()
{
	AssertObj(this);
	if (m_peFirst)
	{
		_DestroyRange(m_peFirst, m_peFirst + m_ce);
		free(m_peFirst);
		m_peFirst = NULL;
		m_ce = 0;
		m_ceAlloc = 0;
	}
	if (m_ceGrowthFactor < 0)
		m_ceGrowthFactor = 0;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Replace a range of zero or more elements with zero or more other values, either using a
	provided array of replacement values, or calling the constructor to provide the replacement
	values.  This can cause the ComVector to shrink, grow, or stay the same size.  Release and
	AddRef are called as needed.
----------------------------------------------------------------------------------------------*/
void BaseOfComVector::Replace(int ieMin, int ieLimDel, IUnknown ** prgpunkIns, int ceIns)
{
	AssertObj(this);
	Assert((0 <= ieMin) && (ieMin <= ieLimDel) && (ieLimDel <= m_ce));
	AssertPtrN(prgpunkIns);
	AssertArray(prgpunkIns, ceIns);

	// make sure we have enough memory allocated
	int ceDel = ieLimDel - ieMin;
	int ceNew = m_ce + (ceIns - ceDel);
	if (ceNew > m_ceAlloc)
		EnsureSpace(ceNew - m_ce);

	// handle the pointers being deleted
	if (ceDel > 0)
	{
		_DestroyRange(m_peFirst + ieMin, m_peFirst + ieLimDel);
	}
	// adjust the gap for inserting after deleting
	if (ceIns != ceDel)
	{
		int cbTail = (m_ce - ieLimDel) * isizeof(IUnknown *);
		memmove(m_peFirst + ieMin + ceIns, m_peFirst + ieLimDel, cbTail);
		m_ce = ceNew;
	}
	// handle the pointers being inserted
	// if prgpunkIns is not NULL, copy from prgpunkIns to the gap
	// otherwise, set each pointer in the gap to NULL
	if (ceIns > 0)
	{
		IUnknown ** ppunkDst;
		if (prgpunkIns)
		{
			IUnknown ** ppunkSrc = prgpunkIns;
			IUnknown * punk;
			for (ppunkDst = m_peFirst + ieMin; 0 < ceIns; --ceIns, ++ppunkDst, ++ppunkSrc)
			{
				punk = *ppunkSrc;
				if (punk)
					punk->AddRef();
				*ppunkDst = punk;
			}
		}
		else
		{
			for (ppunkDst = m_peFirst + ieMin; 0 < ceIns; --ceIns, ++ppunkDst)
				*ppunkDst = NULL;
		}
	}
	AssertObj(this);
}

void BaseOfComVector::CopyTo(BaseOfComVector * pvec)
{
	AssertObj(this);
	if (pvec == NULL)
		ThrowHr(WarnHr(E_POINTER));
	AssertObj(pvec);
	pvec->Clear();
	pvec->EnsureSpace(m_ce);

	for (int ie = 0; ie < m_ce; ++ie)
		pvec->Insert(ie, m_peFirst[ie]);
}

/*----------------------------------------------------------------------------------------------
	This internal method calls Release and sets the stored pointer to NULL for a range of
	stored COM interface pointers.
----------------------------------------------------------------------------------------------*/
void BaseOfComVector::_DestroyRange(IUnknown ** peMin, IUnknown ** peLim)
{
	Assert((m_peFirst <= peMin) && (peMin <= peLim) && (peLim <= (m_peFirst + m_ce)));
	IUnknown ** pe;
	IUnknown * punk;
	for (pe = peMin; pe != peLim; ++pe)
	{
		punk = *pe;
		if (punk)
		{
			punk->Release();
			*pe = NULL;
		}
	}
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	This internal method inserts one or more copies of the given COM interface pointer at the
	given location in the ComVector, calling AddRef as many times as necessary.
----------------------------------------------------------------------------------------------*/
void BaseOfComVector::_InsertMultiple(IUnknown ** pe, int ceIns, IUnknown * punkIns)
{
	Assert((m_peFirst <= pe) && (pe <= (m_peFirst + m_ce)));
	Assert(ceIns >= 0);
	if (ceIns == 0)
		return;
	// make sure we have enough memory allocated
	int ceNew = m_ce + ceIns;
	if (ceNew > m_ceAlloc)
	{
		int ie = pe - m_peFirst;
		EnsureSpace(ceIns);
		pe = m_peFirst + ie;
	}
	memmove(pe + ceIns, pe, ((m_peFirst + m_ce) - pe) * isizeof(IUnknown *));
	m_ce = ceNew;
	for (; 0 < ceIns; --ceIns, ++pe)
	{
		*pe = punkIns;
		if (punkIns)
			punkIns->AddRef();
	}
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	This internal method removes a range of elements from the ComVector, calling Release as
	often as needed.
----------------------------------------------------------------------------------------------*/
void BaseOfComVector::_Erase(IUnknown ** peMin, IUnknown ** peLim)
{
	_DestroyRange(peMin, peLim);
	memmove(peMin, peLim, ((m_peFirst + m_ce) - peLim) * isizeof(IUnknown *));
	m_ce -= peLim - peMin;
	AssertObj(this);
}
//:End Ignore
