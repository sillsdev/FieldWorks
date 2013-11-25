/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: ComHashMap_i.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This file provides the implementations of methods for the ComHashMap template collection
	classes.  It is used as an #include file in any file which explicitly instantiates any
	particular type of ComHashMap<K,IFoo>, ComHashMapStrUni<IFoo>, or ComHashMapChars<IFoo>.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef COMHASHMAP_I_C_INCLUDED
#define COMHASHMAP_I_C_INCLUDED

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "ComHashMap.h"

/***********************************************************************************************
	Methods
***********************************************************************************************/
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	ComHashMap<K,IFoo,H,Eq>::ComHashMap()
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
	Destructor.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	ComHashMap<K,IFoo,H,Eq>::~ComHashMap()
{
	Clear();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that references the first key and value stored in the ComHashMap.
	If the ComHashMap is empty, Begin returns the same value as End.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	typename ComHashMap<K,IFoo,H,Eq>::iterator ComHashMap<K,IFoo,H,Eq>::Begin()
{
	AssertObj(this);
	int ihsnd;
	for (ihsnd = 0; ihsnd < m_ihsndLim; ++ihsnd)
	{
		if (m_prghsnd[ihsnd].InUse())
		{
			iterator ithm(this, ihsnd);
			return ithm;
		}
	}
	return End();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that marks the end of the set of keys and values stored in the
	ComHashMap.  If the ComHashMap is empty, End returns the same value as Begin.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	typename ComHashMap<K,IFoo,H,Eq>::iterator ComHashMap<K,IFoo,H,Eq>::End()
{
	AssertObj(this);
	iterator ithm(this, m_ihsndLim);
	return ithm;
}

/*----------------------------------------------------------------------------------------------
	Add one key and value to the ComHashMap.  Insert potentially invalidates existing iterators
	for this ComHashMap.  An exception is thrown if there are any errors.

	@param key Reference to the key object.  An internal copy is made of this object.
	@param pfoo COM interface pointer associated with the key.  AddRef is called if
					this pointer is not NULL.
	@param fOverwrite Optional flag (defaults to false) to allow a value already associated
					with this key to be replaced by this value.
	@param pihsndOut Optional pointer to an integer for returning the internal index where the
					key-value pair is stored.

	@exception E_INVALIDARG if fOverwrite is not true and the key already is stored with a value
					in this ComHashMap.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	void ComHashMap<K,IFoo,H,Eq>::Insert(K & key, IFoo * pfoo, bool fOverwrite,
		int * pihsndOut)
{
	AssertObj(this);
	// check for initial allocation of memory
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
	// check whether this key is already used
	// if it is, store the value if overwriting is allowed, otherwise complain
	H hasher;
	Eq equal;
	int ihsnd;
	int nHash = hasher(&key, isizeof(K));
	int ie = (unsigned)nHash % m_cBuckets;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K)))
		{
			if (fOverwrite)
			{
				// PutValue() calls Release() and AddRef() as needed
				m_prghsnd[ihsnd].PutValue(pfoo);
				if (pihsndOut)
					*pihsndOut = ihsnd;
				AssertObj(this);
				return;
			}
			else
			{
				ThrowHr(WarnHr(E_INVALIDARG));
			}
		}
	}
	// check whether to increase the number of buckets to redistribute the wealth
	// calculate the average depth of hash collection chains
	// if greater than or equal to two, increase the number of buckets
	int chsndAvgDepth = (m_ihsndLim - m_chsndFree) / m_cBuckets;
	if (chsndAvgDepth > 2)
	{
		int cNewBuckets = GetPrimeNear(4 * m_cBuckets);
		if ((cNewBuckets) && (cNewBuckets > m_cBuckets))
		{
			int * pNewBuckets = (int *)realloc(m_prgihsndBuckets, cNewBuckets * isizeof(int));
			if (pNewBuckets)
			{
				memset(pNewBuckets, -1, cNewBuckets * isizeof(int));
				m_cBuckets = cNewBuckets;
				m_prgihsndBuckets = pNewBuckets;
				for (int i = 0; i < m_ihsndLim; ++i)
				{
					if (m_prghsnd[i].InUse())
					{
						ie = (unsigned)m_prghsnd[i].GetHash() % m_cBuckets;
						m_prghsnd[i].PutNext(m_prgihsndBuckets[ie]);
						m_prgihsndBuckets[ie] = i;
					}
				}
				// recompute the new entry's slot so that it can be stored properly
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
	new((void *)&m_prghsnd[ihsnd]) HashNode();
	m_prghsnd[ihsnd].PutKey(key);
	m_prghsnd[ihsnd].PutValue(pfoo);	// calls Release() and AddRef() as needed
	m_prghsnd[ihsnd].PutHash(nHash);
	m_prghsnd[ihsnd].PutNext(m_prgihsndBuckets[ie]);
	m_prgihsndBuckets[ie] = ihsnd;
	if (pihsndOut)
		*pihsndOut = ihsnd;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Search the ComHashMap for the given key, and return true if the key is found or false if the
	key is not found.  If the key is found, copy the associated COM interface pointer to the
	given smart pointer.  (This implicitly calls AddRef for non-NULL COM interface pointers.)

	@param key Reference to a key object.
	@param qfoo Reference to a "smart pointer" for storing a copy of the value associated with
				the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	bool ComHashMap<K,IFoo,H,Eq>::Retrieve(K & key,
	typename ComHashMap<K,IFoo,H,Eq>::SmartPtr & qfoo)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	Eq equal;
	int nHash = hasher(&key, isizeof(K));
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K)))
		{
			qfoo = m_prghsnd[ihsnd].GetValue();
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Remove the element with the given key from the stored ComHashMap.  This potentially
	invalidates existing iterators for this ComHashMap.  If the key is not found in the
	ComHashMap, then nothing is deleted.  Release is called as needed.

	@param key Reference to a key object.

	@return True if the key is found, and something is actually deleted; otherwise, false.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	bool ComHashMap<K,IFoo,H,Eq>::Delete(K & key)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	Eq equal;
	int nHash = hasher(&key, isizeof(K));
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	int ihsndPrev = -1;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K)))
		{
			if (-1 == ihsndPrev)
				m_prgihsndBuckets[ie] = m_prghsnd[ihsnd].GetNext();
			else
				m_prghsnd[ihsndPrev].PutNext(m_prghsnd[ihsnd].GetNext());

			m_prghsnd[ihsnd].~HashNode();		// calls Release() as needed
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
	Free all the memory used by the ComHashMap.  When done, only the minimum amount of
	bookkeeping memory is still taking up space, and any internal pointers all been set
	to NULL.  Before the memory space is freed, the appropriate destructor is called for all
	key objects stored in the ComHashMap, and Release is called for all non-NULL COM interface
	pointers stored in the ComHashMap.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	void ComHashMap<K,IFoo,H,Eq>::Clear()
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return;

	int ihsnd;
	for (ihsnd = 0; ihsnd < m_ihsndLim; ++ihsnd)
	{
		if (m_prghsnd[ihsnd].InUse())
			m_prghsnd[ihsnd].~HashNode();		// calls Release() as needed
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
	Copy the content of one ComHashMap to another.  An exception is thrown if there are any
	errors.

	@param hmKqfoo Reference to the other ComHashMap.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	void ComHashMap<K,IFoo,H,Eq>::CopyTo(ComHashMap<K,IFoo,H,Eq> & hmKqfoo)
{
	AssertObj(this);
	AssertObj(&hmKqfoo);
	hmKqfoo.Clear();
	iterator itmm;
	for (itmm = Begin(); itmm != End(); ++itmm)
		hmKqfoo.Insert(itmm->GetKey(), itmm->GetValue());
}

/*----------------------------------------------------------------------------------------------
	Copy the content of one ComHashMap to another.  An exception is thrown if there are any
	errors.

	@param phmKqfoo Pointer to the other ComHashMap.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	void ComHashMap<K,IFoo,H,Eq>::CopyTo(ComHashMap<K,IFoo,H,Eq> * phmKqfoo)
{
	if (!phmKqfoo)
		ThrowHr(WarnHr(E_POINTER));
	CopyTo(*phmKqfoo);
}

/*----------------------------------------------------------------------------------------------
	If the given key is found in the ComHashMap, return true, and if the provided index pointer
	is not NULL, also store the internal index value in the indicated memory location.
	If the given key is NOT found in the ComHashMap, return false and ignore the provided index
	pointer.

	@param key Reference to a key object.
	@param pihsndRet Pointer to an integer for returning the internal index where the
					key-value pair is stored.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	bool ComHashMap<K,IFoo,H,Eq>::GetIndex(K & key, int * pihsndRet)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	Eq equal;
	int nHash = hasher(&key, isizeof(K));
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K)))
		{
			if (pihsndRet)
				*pihsndRet = ihsnd;
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	If the given internal ComHashMap index is valid, return true, and if the provided
	pointer to a key object is not NULL, also copy the indexed key to the indicated memory
	location.  If the given internal index is NOT valid, return false, and ignore the provided
	key object pointer.

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param pkeyRet Pointer to an empty key object for storing a copy of the key found at the
				indexed location.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	bool ComHashMap<K,IFoo,H,Eq>::IndexKey(int ihsnd, K * pkeyRet)
{
	AssertObj(this);
	if ((ihsnd < 0) || (ihsnd >= m_ihsndLim))
		return false;
	if (m_prghsnd[ihsnd].InUse())
	{
		if (pkeyRet)
			*pkeyRet = m_prghsnd[ihsnd].GetKey();
		return true;
	}
	else
		return false;
}

/*----------------------------------------------------------------------------------------------
	If the given internal ComHashMap index is valid, return true, and also store the indexed
	value (a COM interface pointer) in the indicated "smart pointer".
	If the given internal index is NOT valid, return false, and ignore the provided "smart
	pointer".

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param qfooRet Reference to a "smart pointer" for storing a copy of the value found at the
				indexed location.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	bool ComHashMap<K,IFoo,H,Eq>::IndexValue(int ihsnd,
	typename ComHashMap<K,IFoo,H,Eq>::SmartPtr & qfooRet)
{
	AssertObj(this);
	if ((ihsnd < 0) || (ihsnd >= m_ihsndLim))
		return false;
	if (m_prghsnd[ihsnd].InUse())
	{
		qfooRet = m_prghsnd[ihsnd].GetValue();
		return true;
	}
	else
		return false;
}

/*----------------------------------------------------------------------------------------------
	Return the number of items (key-value pairs) stored in the hash map.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	int ComHashMap<K,IFoo,H,Eq>::Size()
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return 0;

	return m_ihsndLim - m_chsndFree;
}

//:Ignore
#ifdef DEBUG
/*----------------------------------------------------------------------------------------------
	Return the number of buckets (hash slots) currently allocated for the hash map.  This is
	useful only for debugging the hash map mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	int ComHashMap<K,IFoo,H,Eq>::_BucketCount()
{
	AssertObj(this);
	return m_cBuckets;
}

/*----------------------------------------------------------------------------------------------
	Return the number of buckets (hash slots) that do not point to a list of hashsnd objects.
	This is useful only for debugging the hash map mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	int ComHashMap<K,IFoo,H,Eq>::_EmptyBuckets()
{
	AssertObj(this);
	int ceUnused = 0;
	int ie;
	for (ie = 0; ie < m_cBuckets; ++ie)
	{
		if (-1 == m_prgihsndBuckets[ie])
			++ceUnused;
	}
	return ceUnused;
}

/*----------------------------------------------------------------------------------------------
	Return the number of buckets (hash slots) that currently point to a list of hashsnd
	objects in the hash map.  This is useful only for debugging the hash map mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	int ComHashMap<K,IFoo,H,Eq>::_BucketsUsed()
{
	AssertObj(this);
	int ceUsed = 0;
	int ie;
	for (ie = 0; ie < m_cBuckets; ++ie)
	{
		if (-1 != m_prgihsndBuckets[ie])
			++ceUsed;
	}
	return ceUsed;
}

/*----------------------------------------------------------------------------------------------
	Return the length of the longest list of hashsnd objects stored in any bucket (hash slot)
	of the hash map.  This is useful only for debugging the hash map mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class Eq>
	int ComHashMap<K,IFoo,H,Eq>::_FullestBucket()
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
#endif
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	ComHashMapStrUni<IFoo,H,Eq>::ComHashMapStrUni()
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
	Destructor.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	ComHashMapStrUni<IFoo,H,Eq>::~ComHashMapStrUni()
{
	Clear();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that references the first key and value stored in the ComHashMapStrUni.
	If the ComHashMapStrUni is empty, Begin returns the same value as End.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	typename ComHashMapStrUni<IFoo,H,Eq>::iterator ComHashMapStrUni<IFoo,H,Eq>::Begin()
{
	AssertObj(this);
	int ihsnd;
	for (ihsnd = 0; ihsnd < m_ihsndLim; ++ihsnd)
	{
		if (m_prghsnd[ihsnd].InUse())
		{
			iterator ithmsu(this, ihsnd);
			return ithmsu;
		}
	}
	return End();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that marks the end of the set of keys and values stored in the
	ComHashMapStrUni.  If the ComHashMapStrUni is empty, End returns the same value as Begin.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	typename ComHashMapStrUni<IFoo,H,Eq>::iterator ComHashMapStrUni<IFoo,H,Eq>::End()
{
	AssertObj(this);
	iterator ithmsu(this, m_ihsndLim);
	return ithmsu;
}

/*----------------------------------------------------------------------------------------------
	Add one key and value to the ComHashMapStrUni.  Insert potentially invalidates existing
	iterators for this ComHashMapStrUni.  An exception is thrown if there are any errors.

	@param stuKey Reference to the key StrUni object.  An internal copy is made of this object.
	@param pfoo COM interface pointer associated with the key.  AddRef is called if
					 this pointer is not NULL.
	@param fOverwrite Optional flag (defaults to false) to allow a value already associated
					with this key to be replaced by this value.
	@param pihsndOut Optional pointer to an integer for returning the internal index where the
					key-value pair is stored.

	@exception E_INVALIDARG if fOverwrite is not true and the key already is stored with a value
					in this ComHashMapStrUni.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	void ComHashMapStrUni<IFoo,H,Eq>::Insert(StrUni & stuKey, IFoo * pfoo, bool fOverwrite,
												int * pihsndOut)
{
	AssertObj(this);
	// check for initial allocation of memory
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
	// check whether this key is already used
	// if it is, store the value if overwriting is allowed, otherwise complain
	H hasher;
	Eq equal;
	int ihsnd;
	int nHash = hasher(stuKey);
	int ie = (unsigned)nHash % m_cBuckets;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(stuKey, m_prghsnd[ihsnd].GetKey()))
		{
			if (fOverwrite)
			{
				m_prghsnd[ihsnd].PutValue(pfoo);	// calls Release() and AddRef() as needed
				if (pihsndOut)
					*pihsndOut = ihsnd;
				AssertObj(this);
				return;
			}
			else
			{
				ThrowHr(WarnHr(E_INVALIDARG));
			}
		}
	}
	// check whether to increase the number of buckets to redistribute the wealth
	// calculate the average depth of hash collection chains
	// if greater than or equal to two, increase the number of buckets
	int chsndAvgDepth = (m_ihsndLim - m_chsndFree) / m_cBuckets;
	if (chsndAvgDepth > 2)
	{
		int cNewBuckets = GetPrimeNear(4 * m_cBuckets);
		if ((cNewBuckets) && (cNewBuckets > m_cBuckets))
		{
			int * pNewBuckets = (int *)realloc(m_prgihsndBuckets, cNewBuckets * isizeof(int));
			if (pNewBuckets)
			{
				memset(pNewBuckets, -1, cNewBuckets * isizeof(int));
				m_cBuckets = cNewBuckets;
				m_prgihsndBuckets = pNewBuckets;
				for (int i = 0; i < m_ihsndLim; ++i)
				{
					if (m_prghsnd[i].InUse())
					{
						ie = (unsigned)m_prghsnd[i].GetHash() % m_cBuckets;
						m_prghsnd[i].PutNext(m_prgihsndBuckets[ie]);
						m_prgihsndBuckets[ie] = i;
					}
				}
				// recompute the new entry's slot so that it can be stored properly
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
	new((void *)&m_prghsnd[ihsnd]) HashNode();
	m_prghsnd[ihsnd].PutKey(stuKey);
	m_prghsnd[ihsnd].PutValue(pfoo);	// calls Release() and AddRef() as needed
	m_prghsnd[ihsnd].PutHash(nHash);
	m_prghsnd[ihsnd].PutNext(m_prgihsndBuckets[ie]);
	m_prgihsndBuckets[ie] = ihsnd;
	if (pihsndOut)
		*pihsndOut = ihsnd;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Search the ComHashMapStrUni for the given key, and return true if the key is found or false
	if the key is not found.  If the key is found, copy the associated COM interface pointer to
	the given smart pointer.  (This implicitly calls AddRef for non-NULL COM interface
	pointers.)

	@param stuKey Reference to a key StrUni object.
	@param qfoo Reference to a "smart pointer" for storing a copy of the value associated with
				the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	bool ComHashMapStrUni<IFoo,H,Eq>::Retrieve(StrUni & stuKey,
	typename ComHashMapStrUni<IFoo,H,Eq>::SmartPtr & qfoo)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	Eq equal;
	int nHash = hasher(stuKey);
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(stuKey, m_prghsnd[ihsnd].GetKey()))
		{
			qfoo = m_prghsnd[ihsnd].GetValue();
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Search the ComHashMapStrUni for the given key, and return true if the key is found or false
	if the key is not found.  If the key is found, copy the associated COM interface pointer to
	the given smart pointer.  (This implicitly calls AddRef for non-NULL COM interface
	pointers.)

	@param bstrKey Either a BSTR or a pointer to an array of Unicode characters.
	@param cchwKey Number of wide characters in bstrKey, which may be greater than the number
					of actual Unicode characters due to surrogate pairs.  (-1 means to use the
					size stored in the BSTR.)
	@param qfoo Reference to a "smart pointer" for storing a copy of the value associated with
				the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	bool ComHashMapStrUni<IFoo,H,Eq>::Retrieve(BSTR bstrKey, int cchwKey,
	typename ComHashMapStrUni<IFoo,H,Eq>::SmartPtr & qfoo)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	Eq equal;
	int nHash = hasher(bstrKey, cchwKey);
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(m_prghsnd[ihsnd].GetKey(), bstrKey, cchwKey))
		{
			qfoo = m_prghsnd[ihsnd].GetValue();
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Remove the element with the given key from the stored ComHashMapStrUni.  This potentially
	invalidates existing iterators for this ComHashMapStrUni.  If the key is not found in the
	ComHashMapStrUni, then nothing is deleted.  Release is called as needed.

	@param stuKey Reference to a key StrUni object.

	@return True if the key is found, and something is actually deleted; otherwise, false.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	bool ComHashMapStrUni<IFoo,H,Eq>::Delete(StrUni & stuKey)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	Eq equal;
	int nHash = hasher(stuKey);
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	int ihsndPrev = -1;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(stuKey, m_prghsnd[ihsnd].GetKey()))
		{
			if (-1 == ihsndPrev)
				m_prgihsndBuckets[ie] = m_prghsnd[ihsnd].GetNext();
			else
				m_prghsnd[ihsndPrev].PutNext(m_prghsnd[ihsnd].GetNext());

			m_prghsnd[ihsnd].~HashNode();			// calls Release() as needed
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
	Free all the memory used by the ComHashMapStrUni.  When done, only the minimum amount of
	bookkeeping memory is still taking up space, and any internal pointers all been set
	to NULL.  Before the memory space is freed, the appropriate destructor is called for all
	key StrUni objects stored in the ComHashMapStrUni, and Release is called for all non-NULL
	COM interface pointers stored in the ComHashMapStrUni.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	void ComHashMapStrUni<IFoo,H,Eq>::Clear()
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return;

	int ihsnd;
	for (ihsnd = 0; ihsnd < m_ihsndLim; ++ihsnd)
	{
		if (m_prghsnd[ihsnd].InUse())
		{
			m_prghsnd[ihsnd].~HashNode();	// calls Release() as needed
		}
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
	Copy the content of one ComHashMapStrUni to another.  An exception is thrown if there are
	any errors.

	@param hmsuqfoo Reference to the other ComHashMapStrUni.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	void ComHashMapStrUni<IFoo,H,Eq>::CopyTo(ComHashMapStrUni<IFoo,H,Eq> & hmsuqfoo)
{
	AssertObj(this);
	AssertObj(&hmsuqfoo);
	hmsuqfoo.Clear();
	iterator itmm;
	for (itmm = Begin(); itmm != End(); ++itmm)
		hmsuqfoo.Insert(itmm->GetKey(), itmm->GetValue());
}

/*----------------------------------------------------------------------------------------------
	Copy the content of one ComHashMapStrUni to another.  An exception is thrown if there are
	any errors.

	@param phmsuqfoo Pointer to the other ComHashMapStrUni.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	void ComHashMapStrUni<IFoo,H,Eq>::CopyTo(ComHashMapStrUni<IFoo,H,Eq> * phmsuqfoo)
{
	if (!phmsuqfoo)
		ThrowHr(WarnHr(E_POINTER));
	CopyTo(*phmsuqfoo);
}

/*----------------------------------------------------------------------------------------------
	If the given key is found in the ComHashMapStrUni, return true, and if the provided index
	pointer is not NULL, also store the internal index value in the indicated memory location.
	If the given key is NOT found in the ComHashMapStrUni, return false and ignore the provided
	index pointer.

	@param stuKey Reference to a key StrUni object.
	@param pihsndRet Pointer to an integer for returning the internal index where the
					key-value pair is stored.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	bool ComHashMapStrUni<IFoo,H,Eq>::GetIndex(StrUni & stuKey, int * pihsndRet)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	Eq equal;
	int nHash = hasher(stuKey);
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(stuKey, m_prghsnd[ihsnd].GetKey()))
		{
			if (pihsndRet)
				*pihsndRet = ihsnd;
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	If the given internal ComHashMapStrUni index is valid, return true, and if the provided
	pointer to a key object is not NULL, also copy the indexed key to the indicated memory
	location.  If the given internal index is NOT valid, return false, and ignore the provided
	key object pointer.

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param pstuKeyRet Pointer to an empty StrUni object for storing a copy of the key found at
				the indexed location.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	bool ComHashMapStrUni<IFoo,H,Eq>::IndexKey(int ihsnd, StrUni * pstuKeyRet)
{
	AssertObj(this);
	if ((ihsnd < 0) || (ihsnd >= m_ihsndLim))
		return false;
	if (m_prghsnd[ihsnd].InUse())
	{
		if (pstuKeyRet)
			*pstuKeyRet = m_prghsnd[ihsnd].GetKey();
		return true;
	}
	else
		return false;
}

/*----------------------------------------------------------------------------------------------
	If the given internal ComHashMapStrUni index is valid, return true, and also store the
	indexed value (a COM interface pointer) in the indicated "smart pointer".
	If the given internal index is NOT valid, return false, and ignore the provided "smart
	pointer".

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param qfooRet Reference to a "smart pointer" for storing a copy of the value found at the
				indexed location.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	bool ComHashMapStrUni<IFoo,H,Eq>::IndexValue(int ihsnd,
	typename ComHashMapStrUni<IFoo,H,Eq>::SmartPtr & qfooRet)
{
	AssertObj(this);
	if ((ihsnd < 0) || (ihsnd >= m_ihsndLim))
		return false;
	if (m_prghsnd[ihsnd].InUse())
	{
		qfooRet = m_prghsnd[ihsnd].GetValue();
		return true;
	}
	else
		return false;
}

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	ComHashMapChars<IFoo,H,Eq>::ComHashMapChars()
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
	Destructor.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	ComHashMapChars<IFoo,H,Eq>::~ComHashMapChars()
{
	Clear();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that references the first key and value stored in the ComHashMapChars.
	If the ComHashMapChars is empty, Begin returns the same value as End.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	typename ComHashMapChars<IFoo,H,Eq>::iterator ComHashMapChars<IFoo,H,Eq>::Begin()
{
	AssertObj(this);
	int ihsnd;
	for (ihsnd = 0; ihsnd < m_ihsndLim; ++ihsnd)
	{
		if (m_prghsnd[ihsnd].InUse())
		{
			iterator ithmc(this, ihsnd);
			return ithmc;
		}
	}
	return End();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that marks the end of the set of keys and values stored in the
	ComHashMapChars.  If the ComHashMapChars is empty, End returns the same value as Begin.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	typename ComHashMapChars<IFoo,H,Eq>::iterator ComHashMapChars<IFoo,H,Eq>::End()
{
	AssertObj(this);
	iterator ithmc(this, m_ihsndLim);
	return ithmc;
}

/*----------------------------------------------------------------------------------------------
	Add one key and value to the ComHashMapChars.  Insert potentially invalidates existing
	iterators for this ComHashMapChars.  An exception is thrown if there are any errors.

	@param pszKey Pointer to the key character string.  An internal copy is made of this string.
	@param pfoo COM interface pointer associated with the key.  AddRef is called if
					 this pointer is not NULL.
	@param fOverwrite Optional flag (defaults to false) to allow a value already associated
					with this key to be replaced by this value.
	@param pihsndOut Optional pointer to an integer for returning the internal index where the
					key-value pair is stored.

	@exception E_INVALIDARG if fOverwrite is not true and the key already is stored with a value
					in this ComHashMap.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	void ComHashMapChars<IFoo,H,Eq>::Insert(const char * pszKey, IFoo * pfoo,
	bool fOverwrite, int * pihsndOut)
{
	AssertObj(this);
	// check for initial allocation of memory
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
	// check whether this key is already used
	// if it is, store the value if overwriting is allowed, otherwise complain
	H hasher;
	Eq equal;
	int ihsnd;
	int nHash = hasher(pszKey);
	int ie = (unsigned)nHash % m_cBuckets;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(pszKey, m_prghsnd[ihsnd].GetKey()))
		{
			if (fOverwrite)
			{
				m_prghsnd[ihsnd].PutValue(pfoo);	// calls Release() and AddRef() as needed
				if (pihsndOut)
					*pihsndOut = ihsnd;
				AssertObj(this);
				return;
			}
			else
			{
				ThrowHr(WarnHr(E_INVALIDARG));
			}
		}
	}
	// check whether to increase the number of buckets to redistribute the wealth
	// calculate the average depth of hash collection chains
	// if greater than or equal to two, increase the number of buckets
	int chsndAvgDepth = (m_ihsndLim - m_chsndFree) / m_cBuckets;
	if (chsndAvgDepth > 2)
	{
		int cNewBuckets = GetPrimeNear(4 * m_cBuckets);
		if ((cNewBuckets) && (cNewBuckets > m_cBuckets))
		{
			int * pNewBuckets = (int *)realloc(m_prgihsndBuckets, cNewBuckets * isizeof(int));
			if (pNewBuckets)
			{
				memset(pNewBuckets, -1, cNewBuckets * isizeof(int));
				m_cBuckets = cNewBuckets;
				m_prgihsndBuckets = pNewBuckets;

				for (int i = 0; i < m_ihsndLim; ++i)
				{
					if (m_prghsnd[i].InUse())
					{
						ie = (unsigned)m_prghsnd[i].GetHash() % m_cBuckets;
						m_prghsnd[i].PutNext(m_prgihsndBuckets[ie]);
						m_prgihsndBuckets[ie] = i;
					}
				}
				// recompute the new entry's slot so that it can be stored properly
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
	new((void *)&m_prghsnd[ihsnd]) HashNode();
	char * psz = strdup(pszKey);
	if (!psz)
		ThrowHr(WarnHr(E_OUTOFMEMORY));
	m_prghsnd[ihsnd].PutKey(psz);
	m_prghsnd[ihsnd].PutValue(pfoo);	// calls Release() and AddRef() as needed
	m_prghsnd[ihsnd].PutHash(nHash);
	m_prghsnd[ihsnd].PutNext(m_prgihsndBuckets[ie]);
	m_prgihsndBuckets[ie] = ihsnd;
	if (pihsndOut)
		*pihsndOut = ihsnd;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Search the ComHashMapChars for the given key, and return true if the key is found or false
	if the key is not found.  If the key is found, copy the associated COM interface pointer to
	the given smart pointer.  (This implicitly calls AddRef for non-NULL COM interface
	pointers.)

	@param pszKey Pointer to a key character string.
	@param qfoo Reference to a "smart pointer" for storing a copy of the value associated with
				the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	bool ComHashMapChars<IFoo,H,Eq>::Retrieve(const char * pszKey,
	typename ComHashMapChars<IFoo,H,Eq>::SmartPtr & qfoo)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	Eq equal;
	int nHash = hasher(pszKey);
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(pszKey, m_prghsnd[ihsnd].GetKey()))
		{
			qfoo = m_prghsnd[ihsnd].GetValue();
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Remove the element with the given key from the stored ComHashMapChars.  This potentially
	invalidates existing iterators for this ComHashMapChars.  If the key is not found in the
	ComHashMapChars, then nothing is deleted.  Release is called as needed.

	@param pszKey Pointer to a key character string.

	@return True if the key is found, and something is actually deleted; otherwise, false.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	bool ComHashMapChars<IFoo,H,Eq>::Delete(const char * pszKey)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	Eq equal;
	int nHash = hasher(pszKey);
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	int ihsndPrev = -1;
	const char * psz;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(pszKey, m_prghsnd[ihsnd].GetKey()))
		{
			if (-1 == ihsndPrev)
				m_prgihsndBuckets[ie] = m_prghsnd[ihsnd].GetNext();
			else
				m_prghsnd[ihsndPrev].PutNext(m_prghsnd[ihsnd].GetNext());

			psz = m_prghsnd[ihsnd].GetKey();	// delete key string (not done in destructor)
			if (psz)
			{
				free((void *)psz);
				psz = NULL;
				m_prghsnd[ihsnd].PutKey(psz);
			}
			m_prghsnd[ihsnd].~HashNode();		// calls Release() as needed
			memset(&m_prghsnd[ihsnd], 0, isizeof(HashNode));
			m_prghsnd[ihsnd].PutNext(m_ihsndFirstFree);
			m_ihsndFirstFree = FreeListIdx(ihsnd);
			m_chsndFree++
			AssertObj(this);
			return true;
		}
		ihsndPrev = ihsnd;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Free all the memory used by the ComHashMapChars.  When done, only the minimum amount of
	bookkeeping memory is still taking up space, and any internal pointers all been set
	to NULL.  Before the memory space is freed, Release is called for all non-NULL COM
	interface pointers stored in the ComHashMapChars.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	void ComHashMapChars<IFoo,H,Eq>::Clear()
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return;

	int ihsnd;
	const char * psz;
	for (ihsnd = 0; ihsnd < m_ihsndLim; ++ihsnd)
	{
		if (m_prghsnd[ihsnd].InUse())
		{
			psz = m_prghsnd[ihsnd].GetKey();	// delete key string (not done in destructor)
			if (psz)
			{
				free((void *)psz);
				psz = NULL;
				m_prghsnd[ihsnd].PutKey(psz);
			}
			m_prghsnd[ihsnd].~HashNode();		// calls Release() as needed
		}
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
	Copy the content of one ComHashMapChars to another.  An exception is thrown if there are any
	errors.

	@param hmcqfoo Reference to the other ComHashMapChars.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	void ComHashMapChars<IFoo,H,Eq>::CopyTo(ComHashMapChars<IFoo,H,Eq> & hmcqfoo)
{
	AssertObj(this);
	AssertObj(&hmcqfoo);
	hmcqfoo.Clear();
	iterator itmm;
	for (itmm = Begin(); itmm != End(); ++itmm)
		hmcqfoo.Insert(itmm->GetKey(), itmm->GetValue());
}

/*----------------------------------------------------------------------------------------------
	Copy the content of one ComHashMapChars to another.  An exception is thrown if there are any
	errors.

	@param phmcqfoo Pointer to the other ComHashMapChars.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	void ComHashMapChars<IFoo,H,Eq>::CopyTo(ComHashMapChars<IFoo,H,Eq> * phmcqfoo)
{
	if (!phmcqfoo)
		ThrowHr(WarnHr(E_POINTER));
	CopyTo(*phmcqfoo);
}

/*----------------------------------------------------------------------------------------------
	If the given key is found in the ComHashMapChars, return true, and if the provided index
	pointer is not NULL, also store the internal index value in the indicated memory location.
	If the given key is NOT found in the ComHashMapChars, return false and ignore the provided
	index pointer.

	@param pszKey Pointer to a key character string.
	@param pihsndRet Pointer to an integer for returning the internal index where the
					key-value pair is stored.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	bool ComHashMapChars<IFoo,H,Eq>::GetIndex(const char * pszKey, int * pihsndRet)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	Eq equal;
	int nHash = hasher(pszKey);
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(pszKey, m_prghsnd[ihsnd].GetKey()))
		{
			if (pihsndRet)
				*pihsndRet = ihsnd;
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	If the given internal ComHashMapChars index is valid, return true, and if the provided
	address of a character string pointer is not NULL, also copy a pointer to the indexed key
	to the indicated memory location.  If the given internal index is NOT valid, return false,
	and ignore the provided pointer.

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param ppszKeyRet Address of an empty character string pointer for storing a pointer to the
				key string found at the indexed location.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	bool ComHashMapChars<IFoo,H,Eq>::IndexKey(int ihsnd, const char ** ppszKeyRet)
{
	AssertObj(this);
	if ((ihsnd < 0) || (ihsnd >= m_ihsndLim))
		return false;
	if (m_prghsnd[ihsnd].InUse())
	{
		if (ppszKeyRet)
			*ppszKeyRet = m_prghsnd[ihsnd].GetKey();
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	If the given internal ComHashMapChars index is valid, return true, and also store the
	indexed value (a COM interface pointer) in the indicated "smart pointer".
	If the given internal index is NOT valid, return false, and ignore the provided "smart
	pointer".

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param qfooRet Reference to a "smart pointer" for storing a copy of the value found at the
				indexed location.
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H, class Eq>
	bool ComHashMapChars<IFoo,H,Eq>::IndexValue(int ihsnd,
	typename ComHashMapChars<IFoo,H,Eq>::SmartPtr & qfooRet)
{
	AssertObj(this);
	if ((ihsnd < 0) || (ihsnd >= m_ihsndLim))
		return false;
	if (m_prghsnd[ihsnd].InUse())
	{
		qfooRet = m_prghsnd[ihsnd].GetValue();
		return true;
	}
	return false;
}

// Local Variables:
// mode:C++
// c-file-style:"cellar"
// tab-width:4
// End:

#endif /*COMHASHMAP_I_C_INCLUDED*/
