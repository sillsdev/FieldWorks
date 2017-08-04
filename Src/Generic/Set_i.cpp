/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: Set_i.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This file provides the implementations of the methods for the Set template collection class.
	It is used as an #include file in any file which explicitly instantiates any particular type
	of Set<T>.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef SET_I_C_INCLUDED
#define SET_I_C_INCLUDED

/***********************************************************************************************
	Methods.
***********************************************************************************************/
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	Set<T,H,Eq>::Set()
{
	m_prgihsndBuckets = NULL;
	m_cBuckets = 0;
	m_prghsnd = NULL;
	m_ihsndLim = 0;
	m_ihsndMax = 0;
	m_ihsndFirstFree = FreeListIdx(-1);
	m_chsndFree = 0;
}

/*----------------------------------------------------------------------------------------------
	Copy constructor.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	Set<T,H,Eq>::Set(const Set<T,H,Eq> & set)
{
	m_prgihsndBuckets = NULL;
	m_cBuckets = 0;
	m_prghsnd = NULL;
	m_ihsndLim = 0;
	m_ihsndMax = 0;
	m_ihsndFirstFree = FreeListIdx(-1);
	m_chsndFree = 0;

#if WIN32
	Set<T,H,Eq>::iterator it;
#else // !WIN32
	iterator it;
#endif // !WIN32
	for (it = const_cast< Set<T,H,Eq> & > (set).Begin();
		it != const_cast< Set<T,H,Eq> & > (set).End();
		++it)
	{
		T x = it->GetValue();
		this->Insert(x);
	}
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	Set<T,H,Eq>::~Set()
{
	Clear();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that references the first value stored in the set.
	If the set is empty, Begin returns the same value as End.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	typename Set<T,H,Eq>::iterator Set<T,H,Eq>::Begin()
{
	int ihsnd;
	for (ihsnd = 0; ihsnd < m_ihsndLim; ++ihsnd)
	{
		if (m_prghsnd[ihsnd].InUse())
		{
			iterator itset(this, ihsnd);
			return itset;
		}
	}
	return End();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that marks the end of the set of values stored in the set.
	If the set is empty, End returns the same value as Begin.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	typename Set<T,H,Eq>::iterator Set<T,H,Eq>::End()
{
	iterator itset(this, m_ihsndLim);
	return itset;
}

/*----------------------------------------------------------------------------------------------
	Add one value to the set.  Insert potentially invalidates existing iterators for this
	set.  An exception is thrown if there are any errors.

	@param value Reference to a copy of the object to store in the set.  An internal copy is
				made of this object.
	@param pihsndOut Optional pointer to an integer for returning the internal index where the
				object is stored.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	void Set<T,H,Eq>::Insert(T & value, int * pihsndOut)
{
	AssertObj(this);
	// Check for initial allocation of memory.
	if (!m_cBuckets)
	{
		int cBuckets = GetPrimeNear(10);
		m_prgihsndBuckets = (int *)malloc(cBuckets * isizeof(int));
		if (!m_prgihsndBuckets)
			ThrowHr(WarnHr(E_OUTOFMEMORY));
		memset(m_prgihsndBuckets, -1, cBuckets * isizeof(int));
		m_cBuckets = cBuckets;
	}
	if (!m_ihsndMax)
	{
		int iMax = 32;
		m_prghsnd = (HashNode *)malloc(iMax * isizeof(HashNode));
		if (!m_prghsnd)
			ThrowHr(WarnHr(E_OUTOFMEMORY));
		memset(m_prghsnd, 0, iMax * isizeof(HashNode));
		m_ihsndLim = 0;
		m_ihsndMax = iMax;
		m_ihsndFirstFree = FreeListIdx(-1);
		m_chsndFree = 0;
	}
	// Check whether this value is already present. If so, do nothing.
	H hasher;
	Eq equal;
	int ihsnd;
	int nHash = hasher(&value, isizeof(T));
	int ie = (unsigned)nHash % m_cBuckets;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(&value, &m_prghsnd[ihsnd].GetValue(), isizeof(T)))
		{
			return;
		}
	}
	// Check whether to increase the number of buckets to redistribute the wealth.
	// Calculate the average depth of hash collection chains.
	// If greater than or equal to two, increase the number of buckets.
	int chsndAvgDepth = (m_ihsndLim - m_chsndFree) / m_cBuckets;
	if (chsndAvgDepth > 2)
	{
		int cNewBuckets = GetPrimeNear(4 * m_cBuckets);
		if (cNewBuckets && cNewBuckets > m_cBuckets)
		{
			int * pNewBuckets = NULL;
			if (cNewBuckets)
				pNewBuckets = (int *)realloc(m_prgihsndBuckets, cNewBuckets * isizeof(int));
			if (pNewBuckets)
			{
				memset(pNewBuckets, -1, cNewBuckets * isizeof(int));
				m_cBuckets = cNewBuckets;
				m_prgihsndBuckets = pNewBuckets;
				for (ihsnd = 0; ihsnd < m_ihsndLim; ++ihsnd)
				{
					if (m_prghsnd[ihsnd].InUse())
					{
						ie = (unsigned)m_prghsnd[ihsnd].GetHash() % m_cBuckets;
						m_prghsnd[ihsnd].PutNext(m_prgihsndBuckets[ie]);
						m_prgihsndBuckets[ie] = ihsnd;
					}
				}
				// Recompute the new entry's slot so that it can be stored properly.
				ie = (unsigned)nHash % m_cBuckets;
			}
		}
	}
	if (m_ihsndLim < m_ihsndMax)
	{
		ihsnd = m_ihsndLim;
		++m_ihsndLim;
	}
	else if (m_ihsndFirstFree != FreeListIdx(-1))
	{
		ihsnd = FreeListIdx(m_ihsndFirstFree);
		m_ihsndFirstFree = m_prghsnd[ihsnd].GetNext();
		m_chsndFree--;
	}
	else
	{
		int iNewMax = (!m_ihsndMax) ? 32 : 2 * m_ihsndMax;
		HashNode * pNewNodes = (HashNode *)realloc(m_prghsnd, iNewMax * isizeof(HashNode));
		if (!pNewNodes && iNewMax > 32)
		{
			iNewMax = m_ihsndMax + (m_ihsndMax / 2);
			pNewNodes = (HashNode *)realloc(m_prghsnd, iNewMax * isizeof(HashNode));
			if (!pNewNodes)
				ThrowHr(WarnHr(E_OUTOFMEMORY));
		}
		m_prghsnd = pNewNodes;
		m_ihsndMax = iNewMax;
		Assert(m_ihsndLim < m_ihsndMax);
		ihsnd = m_ihsndLim;
		++m_ihsndLim;
	}
	// Call constructor on previously allocated memory.
	new((void *)&m_prghsnd[ihsnd]) HashNode(value, nHash, m_prgihsndBuckets[ie]);
	m_prgihsndBuckets[ie] = ihsnd;
	if (pihsndOut)
		*pihsndOut = ihsnd;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Return true if the given value is found in the set, or false if it is not found.

	@param value Reference to a copy of the object to search for in the set.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool Set<T,H,Eq>::IsMember(T & value)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	Eq equal;
	int nHash = hasher(&value, isizeof(T));
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(&value, &m_prghsnd[ihsnd].GetValue(), isizeof(T)))
		{
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Remove the element with the given value from the stored set.  This potentially
	invalidates existing iterators for this set.  If the value is not found in the set,
	then nothing is deleted.

	@param value Reference to a copy of the object to delete from the set.

	@return True if the value is found, and something is actually deleted; otherwise, false.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool Set<T,H,Eq>::Delete(T & value)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	Eq equal;
	int nHash = hasher(&value, isizeof(T));
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	int ihsndPrev = -1;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(&value, &m_prghsnd[ihsnd].GetValue(), isizeof(T)))
		{
			if (ihsndPrev == -1)
				m_prgihsndBuckets[ie] = m_prghsnd[ihsnd].GetNext();
			else
				m_prghsnd[ihsndPrev].PutNext(m_prghsnd[ihsnd].GetNext());

			m_prghsnd[ihsnd].~HashNode();		// Ensure member destructors are called.
			memset(&m_prghsnd[ihsnd], 0, isizeof(HashNode));
			m_prghsnd[ihsnd].PutNext(m_ihsndFirstFree);
			m_ihsndFirstFree = FreeListIdx(ihsnd);
			m_chsndFree++;
			AssertObj(this);
			return true;
		}
		ihsndPrev = ihsnd;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Free all the memory used by the set.  When done, only the minimum amount of
	bookkeeping memory is still taking up space, and any internal pointers all been set
	to NULL.  The appropriate destructor is called for all key and value objects stored
	in the set before the memory space is freed.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	void Set<T,H,Eq>::Clear()
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return;
	int ihsnd;
	for (ihsnd = 0; ihsnd < m_ihsndLim; ++ihsnd)
	{
		if (m_prghsnd[ihsnd].InUse())
			m_prghsnd[ihsnd].~HashNode();	// Ensure member destructors are called.
	}
	free(m_prgihsndBuckets);
	free(m_prghsnd);
	m_prgihsndBuckets = NULL;
	m_cBuckets = 0;
	m_prghsnd = NULL;
	m_ihsndLim = 0;
	m_ihsndMax = 0;
	m_ihsndFirstFree = FreeListIdx(-1);
	m_chsndFree = 0;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	If the given value is found in the set, return true, and if the provided index pointer
	is not NULL, also store the value's internal index in the indicated memory location.
	If the given value is NOT found in the set, return false and ignore the provided index
	pointer.

	@param value Reference to a copy of the object to search for in the set.
	@param pihsndRet Optional pointer to an integer for storing the internal index value.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool Set<T,H,Eq>::GetIndex(T & value, int * pihsndRet)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	Eq equal;
	int nHash = hasher(&value, isizeof(T));
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(&value, &m_prghsnd[ihsnd].GetValue(), isizeof(T)))
		{
			if (pihsndRet)
				*pihsndRet = ihsnd;
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	If the given index is valid, return true, and if the provided pointer to a
	T object is not NULL, also copy the indexed value to the indicated memory location.
	If the given index is NOT valid, return false, and ignore the provided value
	object pointer.

	@param ihsnd Index into the set's internal data structure.
	@param pvalueRet Optional pointer to a place for storing a copy of the stored value.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool Set<T,H,Eq>::IndexValue(int ihsnd, T * pvalueRet)
{
	AssertObj(this);
	if (ihsnd < 0 || ihsnd >= m_ihsndLim)
		return false;
	if (m_prghsnd[ihsnd].InUse())
	{
		if (pvalueRet)
			*pvalueRet = m_prghsnd[ihsnd].GetValue();
		return true;
	}
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Return the number of items stored in the set.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	int Set<T,H,Eq>::Size()
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return 0;

	return m_ihsndLim - m_chsndFree;
}

/*----------------------------------------------------------------------------------------------
	Return true if the sets are equal, in the sense that they are the same size, and every
	member of *this is a member of the argument set.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool Set<T,H,Eq>::Equals(Set<T, H, Eq> & itset)
{
	AssertObj(this);
	if (Size() != itset.Size())
		return false;
	iterator it = Begin();
	iterator itEnd = End();
	for ( ; it != itEnd; ++it)
	{
		if (!itset.IsMember(*it))
			return false;
	}
	return true;
}


#ifdef DEBUG
/*----------------------------------------------------------------------------------------------
	Return the number of buckets (hash slots) currently allocated for the set.  This is
	useful only for debugging the set mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	int Set<T,H,Eq>::_BucketCount()
{
	AssertObj(this);
	return m_cBuckets;
}

/*----------------------------------------------------------------------------------------------
	Return the number of buckets (hash slots) that do not point to a list of HashNode objects.
	This is useful only for debugging the set mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	int Set<T,H,Eq>::_EmptyBuckets()
{
	AssertObj(this);
	int ceUnused = 0;
	int ie;
	for (ie = 0; ie < m_cBuckets; ++ie)
	{
		if (m_prgihsndBuckets[ie] == -1)
			++ceUnused;
	}
	return ceUnused;
}

/*----------------------------------------------------------------------------------------------
	Return the number of buckets (hash slots) that currently point to a list of HashNode
	objects in the set.  This is useful only for debugging the set mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	int Set<T,H,Eq>::_BucketsUsed()
{
	AssertObj(this);
	int ceUsed = 0;
	int ie;
	for (ie = 0; ie < m_cBuckets; ++ie)
	{
		if (m_prgihsndBuckets[ie] != -1)
			++ceUsed;
	}
	return ceUsed;
}

/*----------------------------------------------------------------------------------------------
	Return the length of the longest list of HashNode objects stored in any bucket (hash slot)
	of the set.  This is useful only for debugging the set mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	int Set<T,H,Eq>::_FullestBucket()
{
	AssertObj(this);
	int chsndMax = 0;
	int chsnd;
	int ie;
	int ihsnd;
	for (ie = 0; ie < m_cBuckets; ++ie)
	{
		chsnd = 0;
		for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
			++chsnd;
		if (chsndMax < chsnd)
			chsndMax = chsnd;
	}
	return chsndMax;
}
#endif /*DEBUG*/

// Local Variables:
// mode:C++
// c-file-style:"cellar"
// tab-width:4
// End:

#endif /*SET_I_C_INCLUDED*/
