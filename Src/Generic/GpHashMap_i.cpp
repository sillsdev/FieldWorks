/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GpHashMap_i.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This file provides the implementations of methods for the GpHashMap template collection
	classes.  It is used as an #include file in any file which explicitly instantiates any
	particular type of GpHashMap<K,TVal>, GpHashMapStrUni<TVal>, or GpHashMapChars<TVal>.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef GPHASHMAP_I_C_INCLUDED
#define GPHASHMAP_I_C_INCLUDED

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "GpHashMap.h"

/***********************************************************************************************
	Methods
***********************************************************************************************/
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	GpHashMap<K,TVal,H,Eq>::GpHashMap()
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
template<class K, class TVal, class H, class Eq>
	GpHashMap<K,TVal,H,Eq>::~GpHashMap()
{
	Clear();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that references the first key and value stored in the GpHashMap.
	If the GpHashMap is empty, Begin returns the same value as End.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	typename GpHashMap<K,TVal,H,Eq>::iterator GpHashMap<K,TVal,H,Eq>::Begin()
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
	Return an iterator that marks the end of the set of keys and values stored in the GpHashMap.
	If the GpHashMap is empty, End returns the same value as Begin.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	typename GpHashMap<K,TVal,H,Eq>::iterator GpHashMap<K,TVal,H,Eq>::End()
{
	AssertObj(this);
	iterator ithm(this, m_ihsndLim);
	return ithm;
}

/*----------------------------------------------------------------------------------------------
	Add one key and value to the GpHashMap.  Insert potentially invalidates existing iterators
	for this GpHashMap.  An exception is thrown if there are any errors.

	@param key Reference to the key object.  An internal copy is made of this object.
	@param pval Pointer to a generic reference counted object associated with the key.  AddRef
					is called if this pointer is not NULL.
	@param fOverwrite Optional flag (defaults to false) to allow a value already associated
					with this key to be replaced by this value.
	@param pihsndOut Optional pointer to an integer for returning the internal index where the
					key-value pair is stored.

	@exception E_INVALIDARG if fOverwrite is not true and the key already is stored with a value
					in this GpHashMap.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	void GpHashMap<K,TVal,H,Eq>::Insert(K & key, TVal * pval, bool fOverwrite,
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
				m_prghsnd[ihsnd].PutValue(pval);
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
	m_prghsnd[ihsnd].PutValue(pval);	// calls Release() and AddRef() as needed
	m_prghsnd[ihsnd].PutHash(nHash);
	m_prghsnd[ihsnd].PutNext(m_prgihsndBuckets[ie]);
	m_prgihsndBuckets[ie] = ihsnd;
	if (pihsndOut)
		*pihsndOut = ihsnd;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Search the GpHashMap for the given key, and return true if the key is found or false if the
	key is not found.  If the key is found, copy the associated pointer to a generic reference
	counted object to the given smart pointer.  (This implicitly calls AddRef for non-NULL
	object pointers.)

	@param key Reference to a key object.
	@param qval Reference to a "smart pointer" for storing a copy of the value associated with
				the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	bool GpHashMap<K,TVal,H,Eq>::Retrieve(K & key,
	typename GpHashMap<K,TVal,H,Eq>::SmartPtr & qval)
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
			qval = m_prghsnd[ihsnd].GetValue();
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Remove the element with the given key from the stored GpHashMap.  This potentially
	invalidates existing iterators for this GpHashMap.  If the key is not found in the
	GpHashMap, then nothing is deleted.  Release is called as needed.

	@param key Reference to a key object.

	@return True if the key is found, and something is actually deleted; otherwise, false.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	bool GpHashMap<K,TVal,H,Eq>::Delete(K & key)
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
	Free all the memory used by the GpHashMap.  When done, only the minimum amount of
	bookkeeping memory is still taking up space, and any internal pointers all been set
	to NULL.  Before the memory space is freed, the appropriate destructor is called for all
	key objects stored in the GpHashMap, and Release is called for all non-NULL object
	pointers stored in the GpHashMap.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	void GpHashMap<K,TVal,H,Eq>::Clear()
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
	Copy the content of one GpHashMap to another.  An exception is thrown if there are any
	errors.

	@param hmKqval Reference to the other GpHashMap.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	void GpHashMap<K,TVal,H,Eq>::CopyTo(GpHashMap<K,TVal,H,Eq> & hmKqval)
{
	AssertObj(this);
	AssertObj(&hmKqval);
	hmKqval.Clear();
	iterator itmm;
	for (itmm = Begin(); itmm != End(); ++itmm)
		hmKqval.Insert(itmm->GetKey(), itmm->GetValue());
}

/*----------------------------------------------------------------------------------------------
	Copy the content of one GpHashMap to another.  An exception is thrown if there are any
	errors.

	@param phmKqval Pointer to the other GpHashMap.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	void GpHashMap<K,TVal,H,Eq>::CopyTo(GpHashMap<K,TVal,H,Eq> * phmKqval)
{
	if (!phmKqval)
		ThrowHr(WarnHr(E_POINTER));
	CopyTo(*phmKqval);
}

/*----------------------------------------------------------------------------------------------
	If the given key is found in the GpHashMap, return true, and if the provided index pointer
	is not NULL, also store the internal index value in the indicated memory location.
	If the given key is NOT found in the GpHashMap, return false and ignore the provided index
	pointer.

	@param key Reference to a key object.
	@param pihsndRet Pointer to an integer for returning the internal index where the
					key-value pair is stored.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	bool GpHashMap<K,TVal,H,Eq>::GetIndex(K & key, int * pihsndRet)
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
	If the given internal GpHashMap index is valid, return true, and if the provided
	pointer to a key object is not NULL, also copy the indexed key to the indicated memory
	location.  If the given internal index is NOT valid, return false, and ignore the provided
	key object pointer.

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param pkeyRet Pointer to an empty key object for storing a copy of the key found at the
				indexed location.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	bool GpHashMap<K,TVal,H,Eq>::IndexKey(int ihsnd, K * pkeyRet)
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
	If the given internal GpHashMap index is valid, return true, and also store the indexed
	value (a pointer to a generic reference counted object) in the indicated "smart pointer".
	If the given internal index is NOT valid, return false, and ignore the provided "smart
	pointer".

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param qvalRet Reference to a "smart pointer" for storing a copy of the value associated
				with the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	bool GpHashMap<K,TVal,H,Eq>::IndexValue(int ihsnd,
	typename GpHashMap<K,TVal,H,Eq>::SmartPtr & qvalRet)
{
	AssertObj(this);
	if ((ihsnd < 0) || (ihsnd >= m_ihsndLim))
		return false;
	if (m_prghsnd[ihsnd].InUse())
	{
		qvalRet = m_prghsnd[ihsnd].GetValue();
		return true;
	}
	else
		return false;
}

/*----------------------------------------------------------------------------------------------
	Return the number of items (key-value pairs) stored in the hash map.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	int GpHashMap<K,TVal,H,Eq>::Size()
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
template<class K, class TVal, class H, class Eq>
	int GpHashMap<K,TVal,H,Eq>::_BucketCount()
{
	AssertObj(this);
	return m_cBuckets;
}

/*----------------------------------------------------------------------------------------------
	Return the number of buckets (hash slots) that do not point to a list of hashsnd objects.
	This is useful only for debugging the hash map mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H, class Eq>
	int GpHashMap<K,TVal,H,Eq>::_EmptyBuckets()
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
template<class K, class TVal, class H, class Eq>
	int GpHashMap<K,TVal,H,Eq>::_BucketsUsed()
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
template<class K, class TVal, class H, class Eq>
	int GpHashMap<K,TVal,H,Eq>::_FullestBucket()
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
template<class TVal, class H, class Eq>
	GpHashMapStrUni<TVal,H,Eq>::GpHashMapStrUni()
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
template<class TVal, class H, class Eq>
	GpHashMapStrUni<TVal,H,Eq>::~GpHashMapStrUni()
{
	Clear();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that references the first key and value stored in the GpHashMapStrUni.
	If the GpHashMapStrUni is empty, Begin returns the same value as End.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	typename GpHashMapStrUni<TVal,H,Eq>::iterator GpHashMapStrUni<TVal,H,Eq>::Begin()
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
	GpHashMapStrUni.  If the GpHashMapStrUni is empty, End returns the same value as Begin.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	typename GpHashMapStrUni<TVal,H,Eq>::iterator GpHashMapStrUni<TVal,H,Eq>::End()
{
	AssertObj(this);
	iterator ithmsu(this, m_ihsndLim);
	return ithmsu;
}

/*----------------------------------------------------------------------------------------------
	Add one key and value to the GpHashMapStrUni.  Insert potentially invalidates existing
	iterators for this GpHashMapStrUni.  An exception is thrown if there are any errors.

	@param key Reference to the key StrUni object.  An internal copy is made of this object.
	@param pval Pointer to a generic reference counted object associated with the key.  AddRef
					is called if this pointer is not NULL.
	@param fOverwrite Optional flag (defaults to false) to allow a value already associated
					with this key to be replaced by this value.
	@param pihsndOut Optional pointer to an integer for returning the internal index where the
					key-value pair is stored.

	@exception E_INVALIDARG if fOverwrite is not true and the key already is stored with a value
					in this GpHashMapStrUni.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	void GpHashMapStrUni<TVal,H,Eq>::Insert(StrUni & stuKey, TVal * pval, bool fOverwrite,
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
				m_prghsnd[ihsnd].PutValue(pval);	// calls Release() and AddRef() as needed
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
					if (m_prghsnd[ihsnd].InUse())
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
	m_prghsnd[ihsnd].PutValue(pval);	// calls Release() and AddRef() as needed
	m_prghsnd[ihsnd].PutHash(nHash);
	m_prghsnd[ihsnd].PutNext(m_prgihsndBuckets[ie]);
	m_prgihsndBuckets[ie] = ihsnd;
	if (pihsndOut)
		*pihsndOut = ihsnd;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Search the GpHashMapStrUni for the given key, and return true if the key is found or false
	if the key is not found.  If the key is found, copy the associated pointer to a generic
	reference counted object to the given smart pointer.  (This implicitly calls AddRef for
	non-NULL object pointers.)

	@param stuKey Reference to a key StrUni object.
	@param qval Reference to a "smart pointer" for storing a copy of the value associated with
				the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	bool GpHashMapStrUni<TVal,H,Eq>::Retrieve(StrUni & stuKey,
	typename GpHashMapStrUni<TVal,H,Eq>::SmartPtr & qval)
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
			qval = m_prghsnd[ihsnd].GetValue();
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Search the GpHashMapStrUni for the given key, and return true if the key is found or false
	if the key is not found.  If the key is found, copy the associated pointer to a generic
	reference counted object to the given smart pointer.  (This implicitly calls AddRef for
	non-NULL object pointers.)

	@param bstrKey Either a BSTR or a pointer to an array of Unicode characters.
	@param cchwKey Number of wide characters in bstrKey, which may be greater than the number
					of actual Unicode characters due to surrogate pairs.  (-1 means to use the
					size stored in the BSTR.)
	@param qval Reference to a "smart pointer" for storing a copy of the value associated with
				the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	bool GpHashMapStrUni<TVal,H,Eq>::Retrieve(BSTR bstrKey, int cchwKey,
	typename GpHashMapStrUni<TVal,H,Eq>::SmartPtr & qval)
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
			qval = m_prghsnd[ihsnd].GetValue();
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Remove the element with the given key from the stored GpHashMapStrUni.  This potentially
	invalidates existing iterators for this GpHashMapStrUni.  If the key is not found in the
	GpHashMapStrUni, then nothing is deleted.  Release is called as needed.

	@param stuKey Reference to a key StrUni object.

	@return True if the key is found, and something is actually deleted; otherwise, false.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	bool GpHashMapStrUni<TVal,H,Eq>::Delete(StrUni & stuKey)
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
	Free all the memory used by the GpHashMapStrUni.  When done, only the minimum amount of
	bookkeeping memory is still taking up space, and any internal pointers all been set
	to NULL.  Before the memory space is freed, the appropriate destructor is called for all
	key StrUni objects stored in the GpHashMapStrUni, and Release is called for all non-NULL
	object pointers stored in the GpHashMapStrUni.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	void GpHashMapStrUni<TVal,H,Eq>::Clear()
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
	Copy the content of one GpHashMapStrUni to another.  An exception is thrown if there are any
	errors.

	@param hmsuqval Reference to the other GpHashMapStrUni.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	void GpHashMapStrUni<TVal,H,Eq>::CopyTo(GpHashMapStrUni<TVal,H,Eq> & hmsuqval)
{
	AssertObj(this);
	AssertObj(&hmsuqval);
	hmsuqval.Clear();
	iterator itmm;
	for (itmm = Begin(); itmm != End(); ++itmm)
		hmsuqval.Insert(itmm->GetKey(), itmm->GetValue());
}

/*----------------------------------------------------------------------------------------------
	Copy the content of one GpHashMapStrUni to another.  An exception is thrown if there are any
	errors.

	@param phmsuqval Pointer to the other GpHashMapStrUni.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	void GpHashMapStrUni<TVal,H,Eq>::CopyTo(GpHashMapStrUni<TVal,H,Eq> * phmsuqval)
{
	if (!phmsuqval)
		ThrowHr(WarnHr(E_POINTER));
	CopyTo(*phmsuqval);
}

/*----------------------------------------------------------------------------------------------
	If the given key is found in the GpHashMapStrUni, return true, and if the provided index
	pointer is not NULL, also store the internal index value in the indicated memory location.
	If the given key is NOT found in the GpHashMapStrUni, return false and ignore the provided
	index pointer.

	@param stuKey Reference to a key StrUni object.
	@param pihsndRet Pointer to an integer for returning the internal index where the
					key-value pair is stored.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	bool GpHashMapStrUni<TVal,H,Eq>::GetIndex(StrUni & stuKey, int * pihsndRet)
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
	If the given internal GpHashMapStrUni index is valid, return true, and if the provided
	pointer to a key object is not NULL, also copy the indexed key to the indicated memory
	location.  If the given internal index is NOT valid, return false, and ignore the provided
	key object pointer.

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param pkeyRet Pointer to an empty key object for storing a copy of the key found at the
				indexed location.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	bool GpHashMapStrUni<TVal,H,Eq>::IndexKey(int ihsnd, StrUni * pstuKeyRet)
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
	If the given internal GpHashMapStrUni index is valid, return true, and also store the
	indexed value (a pointer to a generic reference counted object) in the indicated "smart
	pointer".  If the given internal index is NOT valid, return false, and ignore the provided
	"smart pointer".

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param qvalRet Reference to a "smart pointer" for storing a copy of the value associated
				with the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	bool GpHashMapStrUni<TVal,H,Eq>::IndexValue(int ihsnd,
	typename GpHashMapStrUni<TVal,H,Eq>::SmartPtr & qvalRet)
{
	AssertObj(this);
	if ((ihsnd < 0) || (ihsnd >= m_ihsndLim))
		return false;
	if (m_prghsnd[ihsnd].InUse())
	{
		qvalRet = m_prghsnd[ihsnd].GetValue();
		return true;
	}
	else
		return false;
}

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	GpHashMapChars<TVal,H,Eq>::GpHashMapChars()
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
template<class TVal, class H, class Eq>
	GpHashMapChars<TVal,H,Eq>::~GpHashMapChars()
{
	Clear();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that references the first key and value stored in the GpHashMapChars.
	If the GpHashMapChars is empty, Begin returns the same value as End.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	typename GpHashMapChars<TVal,H,Eq>::iterator GpHashMapChars<TVal,H,Eq>::Begin()
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
	GpHashMapChars.  If the GpHashMapChars is empty, End returns the same value as Begin.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	typename GpHashMapChars<TVal,H,Eq>::iterator GpHashMapChars<TVal,H,Eq>::End()
{
	AssertObj(this);
	iterator ithmc(this, m_ihsndLim);
	return ithmc;
}

/*----------------------------------------------------------------------------------------------
	Add one key and value to the GpHashMapChars.  Insert potentially invalidates existing
	iterators for this GpHashMapChars.  An exception is thrown if there are any errors.

	@param key Reference to the key object.  An internal copy is made of this object.
	@param pval Pointer to a generic reference counted object associated with the key.  AddRef
					is called if this pointer is not NULL.
	@param fOverwrite Optional flag (defaults to false) to allow a value already associated
					with this key to be replaced by this value.
	@param pihsndOut Optional pointer to an integer for returning the internal index where the
					key-value pair is stored.

	@exception E_INVALIDARG if fOverwrite is not true and the key already is stored with a value
					in this GpHashMapChars.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	void GpHashMapChars<TVal,H,Eq>::Insert(const char * pszKey, TVal * pval,
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
				m_prghsnd[ihsnd].PutValue(pval);	// calls Release() and AddRef() as needed
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
	m_prghsnd[ihsnd].PutValue(pval);	// calls Release() and AddRef() as needed
	m_prghsnd[ihsnd].PutHash(nHash);
	m_prghsnd[ihsnd].PutNext(m_prgihsndBuckets[ie]);
	m_prgihsndBuckets[ie] = ihsnd;
	if (pihsndOut)
		*pihsndOut = ihsnd;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Search the GpHashMapChars for the given key, and return true if the key is found or false
	if the key is not found.  If the key is found, copy the associated pointer to a generic
	reference counted object to the given smart pointer.  (This implicitly calls AddRef for
	non-NULL object pointers.)

	@param pszKey Pointer to a key character string.
	@param qval Reference to a "smart pointer" for storing a copy of the value associated with
				the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	bool GpHashMapChars<TVal,H,Eq>::Retrieve(const char * pszKey,
	typename GpHashMapChars<TVal,H,Eq>::SmartPtr & qval)
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
			qval = m_prghsnd[ihsnd].GetValue();
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Remove the element with the given key from the stored GpHashMapChars.  This potentially
	invalidates existing iterators for this GpHashMapChars.  If the key is not found in the
	GpHashMapChars, then nothing is deleted.  Release is called as needed.

	@param pszKey Pointer to a key character string.

	@return True if the key is found, and something is actually deleted; otherwise, false.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	bool GpHashMapChars<TVal,H,Eq>::Delete(const char * pszKey)
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
			m_chsndFree++;
			AssertObj(this);
			return true;
		}
		ihsndPrev = ihsnd;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Free all the memory used by the GpHashMapChars.  When done, only the minimum amount of
	bookkeeping memory is still taking up space, and any internal pointers all been set
	to NULL.  Before the memory space is freed, Release is called for all non-NULL object
	pointers stored in the GpHashMapChars.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	void GpHashMapChars<TVal,H,Eq>::Clear()
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
	Copy the content of one GpHashMapChars to another.  An exception is thrown if there are any
	errors.

	@param hmcqval Reference to the other GpHashMapChars.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	void GpHashMapChars<TVal,H,Eq>::CopyTo(GpHashMapChars<TVal,H,Eq> & hmcqval)
{
	AssertObj(this);
	AssertObj(&hmcqval);
	hmcqval.Clear();
	iterator itmm;
	for (itmm = Begin(); itmm != End(); ++itmm)
		hmcqval.Insert(itmm->GetKey(), itmm->GetValue());
}

/*----------------------------------------------------------------------------------------------
	Copy the content of one GpHashMapChars to another.  An exception is thrown if there are any
	errors.

	@param phmcqval Pointer to the other GpHashMapChars.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	void GpHashMapChars<TVal,H,Eq>::CopyTo(GpHashMapChars<TVal,H,Eq> * phmcqval)
{
	if (!phmcqval)
		ThrowHr(WarnHr(E_POINTER));
	CopyTo(*phmcqval);
}

/*----------------------------------------------------------------------------------------------
	If the given key is found in the GpHashMapChars, return true, and if the provided index
	pointer is not NULL, also store the internal index value in the indicated memory location.
	If the given key is NOT found in the GpHashMapChars, return false and ignore the provided
	index pointer.

	@param pszKey Pointer to a key character string.
	@param pihsndRet Pointer to an integer for returning the internal index where the
					key-value pair is stored.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	bool GpHashMapChars<TVal,H,Eq>::GetIndex(const char * pszKey, int * pihsndRet)
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
	If the given internal GpHashMapChars index is valid, return true, and if the provided
	pointer to a key object is not NULL, also copy the indexed key to the indicated memory
	location.  If the given internal index is NOT valid, return false, and ignore the provided
	key object pointer.

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param pkeyRet Pointer to an empty key object for storing a copy of the key found at the
				indexed location.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	bool GpHashMapChars<TVal,H,Eq>::IndexKey(int ihsnd, const char ** ppszKeyRet)
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
	If the given internal GpHashMapChars index is valid, return true, and also store the indexed
	value (a pointer to a generic reference counted object) in the indicated "smart pointer".
	If the given internal index is NOT valid, return false, and ignore the provided "smart
	pointer".

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param qvalRet Reference to a "smart pointer" for storing a copy of the value associated
				with the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class TVal, class H, class Eq>
	bool GpHashMapChars<TVal,H,Eq>::IndexValue(int ihsnd,
	typename GpHashMapChars<TVal,H,Eq>::SmartPtr & qvalRet)
{
	AssertObj(this);
	if ((ihsnd < 0) || (ihsnd >= m_ihsndLim))
		return false;
	if (m_prghsnd[ihsnd].InUse())
	{
		qvalRet = m_prghsnd[ihsnd].GetValue();
		return true;
	}
	return false;
}

// Local Variables:
// mode:C++
// c-file-style:"cellar"
// tab-width:4
// End:

#endif /*GPHASHMAP_I_C_INCLUDED*/
