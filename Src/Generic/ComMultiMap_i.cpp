/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: ComMultiMap_i.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This file provides the implementations of methods for the ComMultiMap template collection
	classes.  It is used as an #include file in any file which explicitly instantiates any
	particular type of ComMultiMap<K,T>.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef COMMULTIMAP_I_C_INCLUDED
#define COMMULTIMAP_I_C_INCLUDED

/***********************************************************************************************
	Methods
***********************************************************************************************/
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	ComMultiMap<K,IFoo,H,EqK>::ComMultiMap()
{
	m_prgihsndBuckets = NULL;
	m_cBuckets = 0;
	m_prghsnd = NULL;
	m_ihsndLim = 0;
	m_ihsndMax = 0;
	m_ihsndFirstFree = FreeListIdx(-1);
	m_chsndFree = 0;
	m_cUnqKeys = 0;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	ComMultiMap<K,IFoo,H,EqK>::~ComMultiMap()
{
	AssertObj(this);
	Clear();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that references the first key and value stored in the ComMultiMap.
	If the ComMultiMap is empty, Begin returns the same value as End.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	typename ComMultiMap<K,IFoo,H,EqK>::iterator ComMultiMap<K,IFoo,H,EqK>::Begin()
{
	AssertObj(this);
	for (int ie = 0; ie < m_cBuckets; ++ie)
	{
		if (m_prgihsndBuckets[ie] != -1)
		{
			iterator ithm(this, m_prgihsndBuckets[ie]);
			return ithm;
		}
	}
	return End();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that marks the last key and value stored in the ComMultiMap.
	If the ComMultiMap is empty, End returns the same value as Begin.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	typename ComMultiMap<K,IFoo,H,EqK>::iterator ComMultiMap<K,IFoo,H,EqK>::End()
{
	AssertObj(this);
	iterator ithm(this, m_ihsndLim);
	return ithm;
}

/*----------------------------------------------------------------------------------------------
	Add one key and value to the ComMultiMap.  Insert potentially invalidates existing
	iterators for this ComMultiMap.  An exception is thrown if there are any errors.
	There can be many copies of one key associated with copies of different or duplicate values.

	@param key Reference to the key object.  An internal copy is made of this object.
	@param pfoo The value (a COM interface pointer) associated with the key.
	@param pihsndOut Optional pointer to an integer for returning the internal index where the
				key-value pair is stored.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	void ComMultiMap<K,IFoo,H,EqK>::Insert(K & key, IFoo * pfoo, int * pihsndOut)
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
	// Check whether this key is already used.
	// If it is, remember the (first) location in the array of HashNodes.
	H hasher;
	EqK equal;
	int ihsnd;
	int ihsndInsertAfter = -1;
	int nHash = hasher(&key, isizeof(K));
	int ie = (unsigned)nHash % m_cBuckets;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if (nHash == m_prghsnd[ihsnd].GetHash() &&
			equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K)))
		{
			// Even if more buckets are allocated below, and the HashNodes redistributed,
			// this index value is still valid since it accesses the allocated array of
			// HashNodes, not the particular bucket.
			ihsndInsertAfter = ihsnd;
			break;
		}
		else
		{
			// Skip over any nodes with the same key as the current one.
			ihsnd = _GetLastDupKey(ihsnd);
		}
	}
	// Check whether to increase the number of buckets to redistribute the wealth.
	// Calculate the average depth of hash collection chains.
	// If greater than or equal to two, and the number of unique keys is greater than the
	// number of buckets, increase the number of buckets.
	int chsndAvgDepth = (m_ihsndLim - m_chsndFree) / m_cBuckets;
	if (chsndAvgDepth > 2 && m_cUnqKeys > m_cBuckets)
	{
		int cNewBuckets = GetPrimeNear(4 * m_cBuckets);
		if (cNewBuckets && cNewBuckets > m_cBuckets)
		{
			int * pNewBuckets = (int *)realloc(m_prgihsndBuckets, cNewBuckets * isizeof(int));
			if (pNewBuckets)
			{
				memset(pNewBuckets, -1, cNewBuckets * isizeof(int));
				m_cBuckets = cNewBuckets;
				m_prgihsndBuckets = pNewBuckets;
				for (ihsnd = 0; ihsnd < m_ihsndLim; ++ihsnd)
				{
					if (m_prghsnd[ihsnd].InUse())
						_Insert(ihsnd, m_prghsnd[ihsnd].GetHash(), m_prghsnd[ihsnd].GetKey());
				}
				// Recompute the new entry's slot so that it can be stored properly.
				ie = (unsigned)nHash % m_cBuckets;
			}
		}
	}
	// Get the index of an available hash node to put the key and value in.
	if (m_ihsndLim < m_ihsndMax)
	{
		ihsnd = m_ihsndLim;
		++m_ihsndLim;
	}
	else if (FreeListIdx(m_ihsndFirstFree) != -1)
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
	if (ihsndInsertAfter == -1)
	{
		// The caller's key was not in the ComMultiMap; it becomes first in the bucket's list.
		new((void *)&m_prghsnd[ihsnd]) HashNode(key, pfoo, nHash, m_prgihsndBuckets[ie]);
		m_prgihsndBuckets[ie] = ihsnd;
		// since this is the first node using this key, increment unique key count
		m_cUnqKeys++;
	}
	else
	{
		// The caller's key is a duplicate, so insert after the same keys.
		new((void *)&m_prghsnd[ihsnd])
				HashNode(key, pfoo, nHash, m_prghsnd[ihsndInsertAfter].GetNext());
		m_prghsnd[ihsndInsertAfter].PutNext(ihsnd);
	}
	if (pihsndOut)
		*pihsndOut = ihsnd;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Search the ComMultiMap for the given key, and set beginning and ending iterators
	appropriately.

	@param key Reference to the key object.
	@param pitMin Pointer to an iterator which is set to the first value stored for the key.
	@param pitLim Pointer to an iterator which is set just past the last value stored for the
			key.

	@return True if the key is found, or false if the key is not found.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	bool ComMultiMap<K,IFoo,H,EqK>::Retrieve(K & key, iterator * pitMin, iterator * pitLim)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	EqK equal;
	int nHash = hasher(&key, isizeof(K));
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if (nHash == m_prghsnd[ihsnd].GetHash() &&
			equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K)))
		{
			new((iterator *)pitMin) iterator(this, ihsnd);
			new((iterator *)pitLim) iterator(this, _GetLastDupKey(ihsnd));
			++(*pitLim);				// We need the Lim, not the Max!
			return true;
		}
	}
	new((iterator *)pitMin) iterator(this, m_ihsndLim);
	new((iterator *)pitLim) iterator(this, m_ihsndLim);
	return false;
}

/*----------------------------------------------------------------------------------------------
	Remove the element(s) with the given key from the stored ComMultiMap.  This potentially
	invalidates existing iterators for this ComMultiMap.  If the key is not found in the
	ComMultiMap, then nothing is deleted.  If the key is stored multiple times, all entries with
	the key are deleted.

	@param key Reference to a copy of the key object.

	@return True if the key is found, and something is actually deleted; otherwise, false.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	bool ComMultiMap<K,IFoo,H,EqK>::Delete(K & key)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	EqK equal;
	int nHash = hasher(&key, isizeof(K));
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	int ihsndPrev = -1;
	for (ihsnd = m_prgihsndBuckets[ie];
		 ihsnd != -1;
		 ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if (nHash == m_prghsnd[ihsnd].GetHash() &&
			equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K)))
		{
			// Found a key match - the rest of the HashNodes with this key follow immediately.
			int ihsndNext;
			do
			{
				// Relink this HashNode onto the free list.
				ihsndNext = m_prghsnd[ihsnd].GetNext();
				m_prghsnd[ihsnd].~HashNode();	// Ensure member destructors are called.
				memset(&m_prghsnd[ihsnd], 0, isizeof(HashNode));
				m_prghsnd[ihsnd].PutNext(m_ihsndFirstFree);
				m_ihsndFirstFree = FreeListIdx(ihsnd);
				m_chsndFree++;
				ihsnd = ihsndNext;
			} while (ihsnd != -1 && equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K)));
			// Relink the used HashNodes.
			if (ihsndPrev == -1)
				m_prgihsndBuckets[ie] = ihsnd;
			else
				m_prghsnd[ihsndPrev].PutNext(ihsnd);
			AssertObj(this);
			// decrement unique key count
			m_cUnqKeys--;
			return true;
		}
		ihsndPrev = ihsnd;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Remove the element with the given key and value from the stored ComMultiMap.  This
	potentially invalidates existing iterators for this ComMultiMap.  If the key-value pair is
	not found in the ComMultiMap, then nothing is deleted.  At most one key-value pair is
	deleted from the ComMultiMap.

	@param key Reference to a copy of the key object.
	@param pfoo The value (a COM interface pointer) associated with the key.

	@return True if the key is found, and something is actually deleted; otherwise, false.

	@null{	TODO SteveMc: should this delete all nodes with the given key and value, or just
	one (as it does now)?	}
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	bool ComMultiMap<K,IFoo,H,EqK>::Delete(K & key, IFoo * pfoo)
{
	AssertObj(this);
	AssertPtrN(pfoo);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	EqK equal;
	int nHash = hasher(&key, isizeof(K));
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	int ihsndPrev = -1;
	IUnknownPtr qunk;
	if (pfoo)
	{
		CheckHr(pfoo->QueryInterface(IID_IUnknown, (void **)&qunk));
	}
	for (ihsnd = m_prgihsndBuckets[ie];
		 ihsnd != -1;
		 ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if (nHash == m_prghsnd[ihsnd].GetHash() &&
			equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K)))
		{
			// Found a key match - the rest of the HashNodes with this key follow immediately.
			bool first = true;
			do
			{
				IUnknownPtr qunk2;
				DoAssert(SUCCEEDED(m_prghsnd[ihsnd].GetValue()->QueryInterface(IID_IUnknown,
					(void **)&qunk2)));
				// If the value objects are equal, delete this one and quit.
				if (SameObject(qunk, qunk2))
				{
					// check and see if this is the last node using this key
					// if it is, decrement unique key count
					int ihsndNext = m_prghsnd[ihsnd].GetNext();
					if (first && (ihsndNext == -1 || (nHash != m_prghsnd[ihsndNext].GetHash()
						|| !equal(&key, &m_prghsnd[ihsndNext].GetKey(), isizeof(K)))))
					{
						m_cUnqKeys--;
					}
					// Relink around this node.
					if (ihsndPrev == -1)
						m_prgihsndBuckets[ie] = m_prghsnd[ihsnd].GetNext();
					else
						m_prghsnd[ihsndPrev].PutNext(m_prghsnd[ihsnd].GetNext());
					// Erase this node and add it to the free list.
					m_prghsnd[ihsnd].~HashNode();
					memset(&m_prghsnd[ihsnd], 0, isizeof(HashNode));
					m_prghsnd[ihsnd].PutNext(m_ihsndFirstFree);
					m_ihsndFirstFree = FreeListIdx(ihsnd);
					m_chsndFree++;
					AssertObj(this);
					return true;
				}
				// step to the next HashNode
				ihsndPrev = ihsnd;
				ihsnd = m_prghsnd[ihsnd].GetNext();
				first = false;
			} while (ihsnd != -1 &&
				nHash == m_prghsnd[ihsnd].GetHash() &&
				equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K)));
			return false;
		}
		ihsndPrev = ihsnd;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Free all the memory used by the ComMultiMap.  When done, only the minimum amount of
	bookkeeping memory is still taking up space, and any internal pointers all been set
	to NULL.  The appropriate destructor is called for all key and value objects stored
	in the ComMultiMap before the memory space is freed.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	void ComMultiMap<K,IFoo,H,EqK>::Clear()
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
	m_cUnqKeys = 0;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Copy the content of this ComMultiMap to another ComMultiMap.  An exception is thrown if
	there are any errors.

	@param mmKqfoo Reference to the other ComMultiMap.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	void ComMultiMap<K,IFoo,H,EqK>::CopyTo(ComMultiMap<K,IFoo,H,EqK> & mmKqfoo)
{
	AssertObj(this);
	AssertObj(&mmKqfoo);
	mmKqfoo.Clear();
	iterator itmm;
	for (itmm = Begin(); itmm != End(); ++itmm)
		mmKqfoo.Insert(itmm->GetKey(), itmm->GetValue());
}

/*----------------------------------------------------------------------------------------------
	Copy the content of this ComMultiMap to another ComMultiMap.  An exception is thrown if
	there are any errors.

	@param pmmKqfoo Pointer to the other ComMultiMap.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	void ComMultiMap<K,IFoo,H,EqK>::CopyTo(ComMultiMap<K,IFoo,H,EqK> * pmmKqfoo)
{
	if (!pmmKqfoo)
		ThrowHr(WarnHr(E_POINTER));
	CopyTo(*pmmKqfoo);
}

/*----------------------------------------------------------------------------------------------
	If the given key is found in the ComMultiMap, return true, and if the provided index pointer
	is not NULL, also store an internal index value in the indicated memory location.  If the
	given key is NOT found in the ComMultiMap, return false and ignore the provided index
	pointer.  If the key is stored multiple times, the index returned is arbitrarily picked from
	the duplicates of the key.

	@param key Reference to a copy of the key object.
	@param pihsndRet Pointer to an integer for returning the internal index where the
				given key is stored.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	bool ComMultiMap<K,IFoo,H,EqK>::GetIndex(K & key, int * pihsndRet)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hasher;
	EqK equal;
	int nHash = hasher(&key, isizeof(K));
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie];
		 ihsnd != -1;
		 ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if (nHash == m_prghsnd[ihsnd].GetHash() &&
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
	If the given internal ComMultiMap index is valid, return true, and if the provided pointer
	to a key object is not NULL, also copy the indexed key to the indicated memory location.
	If the given internal index is NOT valid, return false, and ignore the provided key object
	pointer.

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param pkeyRet Pointer to an empty key object for storing a copy of the key found at the
				indexed location.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	bool ComMultiMap<K,IFoo,H,EqK>::IndexKey(int ihsnd, K * pkeyRet)
{
	AssertObj(this);
	if (ihsnd < 0 || ihsnd >= m_ihsndLim)
		return false;
	if (m_prghsnd[ihsnd].InUse())
	{
		if (pkeyRet)
			*pkeyRet = m_prghsnd[ihsnd].GetKey();
		return true;
	}
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	If the given internal ComMultiMap index is valid, return true, and also store the indexed
	value (a COM interface pointer) in the indicated "smart pointer".
	If the given internal index is NOT valid, return false, and ignore the provided "smart
	pointer".

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param qfooRet Reference to a "smart pointer" for storing a copy of the value found at the
				indexed location.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	bool ComMultiMap<K,IFoo,H,EqK>::IndexValue(int ihsnd,
	typename ComMultiMap<K,IFoo,H,EqK>::SmartPtr & qfooRet)
{
	AssertObj(this);
	if (ihsnd < 0 || ihsnd >= m_ihsndLim)
		return false;
	if (m_prghsnd[ihsnd].InUse())
	{
		qfooRet = m_prghsnd[ihsnd].GetValue();
		return true;
	}
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Return the number of stored elements with the same key as the indexed element.  If the index
	is invalid, return zero.

	@param ihsndIn Internal index value returned earlier by GetIndex or Insert.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	int ComMultiMap<K,IFoo,H,EqK>::KeyCount(int ihsndIn)
{
	AssertObj(this);
	if (ihsndIn < 0 || ihsndIn >= m_ihsndLim)
		return 0;
	int nHash = m_prghsnd[ihsndIn].GetHash();
	K & key = m_prghsnd[ihsndIn].GetKey();
	EqK equal;
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie];
		 ihsnd != -1;
		 ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if (nHash == m_prghsnd[ihsnd].GetHash() &&
			equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K)))
		{
			int ckey = 0;
			do
			{
				++ckey;
				ihsnd = m_prghsnd[ihsnd].GetNext();
			} while (ihsnd != -1 && equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K)));
			return ckey;
		}
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Return the number of unique keys stored in the ComMultiMap.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	int ComMultiMap<K,IFoo,H,EqK>::CountUniqueKeys()
{
	AssertObj(this);
	//int ckey = 0;
	//int ie;
	//int ihsnd;
	//for (ie = 0; ie < m_cBuckets; ++ie)				// Look in each bucket.
	//{
	//	for (ihsnd = m_prgihsndBuckets[ie];
	//		 ihsnd != -1;
	//		 ihsnd = m_prghsnd[ihsnd].GetNext())	// Run through each list.
	//	{
	//		ihsnd = _GetLastDupKey(ihsnd);			// Count each key only once.
	//		++ckey;
	//	}
	//}
	//return ckey;
	return m_cUnqKeys;
}

/*----------------------------------------------------------------------------------------------
	Return the number of items (key-value pairs) stored in the ComMultiMap.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	int ComMultiMap<K,IFoo,H,EqK>::Size()
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return 0;
	return m_ihsndLim - m_chsndFree;
}

//:Ignore
#ifdef DEBUG
/*----------------------------------------------------------------------------------------------
	Return the number of buckets (hash slots) that do not point to a list of HashNode objects.
	This is useful only for debugging the hash map mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	int ComMultiMap<K,IFoo,H,EqK>::_EmptyBuckets()
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
	objects in the ComMultiMap.  This is useful only for debugging the ComMultiMap mechanism
	itself.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	int ComMultiMap<K,IFoo,H,EqK>::_BucketsUsed()
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
	of the ComMultiMap.  This is useful only for debugging the ComMultiMap mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	int ComMultiMap<K,IFoo,H,EqK>::_FullestBucket()
{
	AssertObj(this);
	int chsndMax = 0;
	int chsnd;
	int ie;
	int ihsnd;
	for (ie = 0; ie < m_cBuckets; ++ie)
	{
		chsnd = 0;
		for (ihsnd = m_prgihsndBuckets[ie];
			 ihsnd != -1;
			 ihsnd = m_prghsnd[ihsnd].GetNext())
		{
			++chsnd;
		}
		if (chsndMax < chsnd)
			chsndMax = chsnd;
	}
	return chsndMax;
}
#endif

/*----------------------------------------------------------------------------------------------
	Add one key and value to the ComMultiMap.  _Insert() is called only by Insert().
	No memory is allocated since the hash node already exists.
	There can be many copies of one key associated with copies of different or duplicate values.
	This is used by the public Insert method to reoganize buckets. The buckets need to be
	allocated already.

	@param ihsndIn Index of the hash node to be inserted into a ComMultiMap bucket.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	void ComMultiMap<K,IFoo,H,EqK>::_Insert(int ihsndIn, int nHash, K & key)
{
	AssertObj(this);
	Assert(m_cBuckets);
	AssertPtr(m_prgihsndBuckets);
	AssertPtr(m_prghsnd);
	Assert(0 <= ihsndIn && ihsndIn < m_ihsndLim);
	Assert(nHash == m_prghsnd[ihsndIn].GetHash());
	EqK equal;
	Assert(equal(&key, &m_prghsnd[ihsndIn].GetKey(), isizeof(K)));

	int ihsnd;
	int ihsndInsertAfter = -1;
	int ie = (unsigned)nHash % m_cBuckets;
	for (ihsnd = m_prgihsndBuckets[ie];
		 ihsnd != -1;
		 ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if (nHash == m_prghsnd[ihsnd].GetHash() &&
			equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K)))
		{
			ihsndInsertAfter = ihsnd;
			break;
		}
		// Calling _GetLastDupKey() doesn't save any time here: it takes more time.
		// (See Insert() for example of where calling _GetLastDupKey() saves time.)
	}
	if (ihsndInsertAfter == -1)
	{
		// The caller's key was not in the ComMultiMap; it becomes first in the bucket's list.
		m_prghsnd[ihsndIn].PutNext(m_prgihsndBuckets[ie]);
		m_prgihsndBuckets[ie] = ihsndIn;
	}
	else
	{
		// The caller's key is a duplicate, so insert after the first matching key.
		m_prghsnd[ihsndIn].PutNext(m_prghsnd[ihsndInsertAfter].GetNext());
		m_prghsnd[ihsndInsertAfter].PutNext(ihsndIn);
	}
}

/*----------------------------------------------------------------------------------------------
	If the given hash node index is valid, return the hash node index of the last duplicate
	key from the bucket's list.  If the key is unique, its hash node index is returned.

	@param ihsndFirst Index of a hash node stored in the ComMultiMap.
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H, class EqK>
	int ComMultiMap<K,IFoo,H,EqK>::_GetLastDupKey(int ihsndFirst)
{
	AssertObj(this);
	Assert(0 <= ihsndFirst && ihsndFirst < m_ihsndLim);
	AssertPtr(m_prghsnd);
	Assert(m_prghsnd[ihsndFirst].InUse());

	// The rest of the HashNodes with this key should follow immediately.
	int ihsnd;
	int ihsndLast = ihsndFirst;
	K & key = m_prghsnd[ihsndFirst].GetKey();
	EqK equal;
	for (ihsnd = m_prghsnd[ihsndFirst].GetNext();
		 ihsnd != -1 && equal(&key, &m_prghsnd[ihsnd].GetKey(), isizeof(K));
		 ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		ihsndLast = ihsnd;
	}
	return ihsndLast;
}
//:End Ignore

// Local Variables:
// mode:C++
// c-file-style:"cellar"
// tab-width:4
// End:

#endif /*COMMULTIMAP_I_C_INCLUDED*/
