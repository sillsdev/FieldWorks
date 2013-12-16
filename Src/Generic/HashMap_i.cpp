/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: HashMap_i.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This file provides the implementations of methods for the HashMap template collection
	classes.  It is used as an #include file in any file which explicitly instantiates any
	particular type of HashMap<K,T>, HashMapStrUni<T>, or HashMapChars<T>.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef HASHMAP_I_C_INCLUDED
#define HASHMAP_I_C_INCLUDED

/***********************************************************************************************
	Methods
***********************************************************************************************/
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	HashMap<K,T,H,Eq>::HashMap()
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
	Copy constructor.  It throws an error if it runs out of memory.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	HashMap<K,T,H,Eq>::HashMap(HashMap<K,T,H,Eq> & hm)
{
	m_prgihsndBuckets = NULL;
	m_cBuckets= 0;
	m_prghsnd = NULL;
	m_ihsndLim = 0;
	m_ihsndMax = 0;
	m_ihsndFirstFree = FreeListIdx(-1);
	m_chsndFree = 0;
	hm.CopyTo(this);
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	HashMap<K,T,H,Eq>::~HashMap()
{
	Clear();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that references the first key and value stored in the HashMap.
	If the hash map is empty, Begin returns the same value as End.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	typename HashMap<K,T,H,Eq>::iterator HashMap<K,T,H,Eq>::Begin()
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
	Return an iterator that marks the end of the set of keys and values stored in the HashMap.
	If the HashMap is empty, End returns the same value as Begin.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	typename HashMap<K,T,H,Eq>::iterator HashMap<K,T,H,Eq>::End()
{
	AssertObj(this);
	iterator ithm(this, m_ihsndLim);
	return ithm;
}

/*----------------------------------------------------------------------------------------------
	Add one key and value to the HashMap.  Insert potentially invalidates existing iterators
	for this HashMap.  An exception is thrown if there are any errors.

	@param key Reference to the key object.  An internal copy is made of this object.
	@param value Reference to the object associated with the key.  An internal copy is
					 made of this object.
	@param fOverwrite Optional flag (defaults to false) to allow a value already associated
					with this key to be replaced by this value.
	@param pihsndOut Optional pointer to an integer for returning the internal index where the
					key-value pair is stored.

	@exception E_INVALIDARG if fOverwrite is not true and the key already is stored with a value
					in this HashMap.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	void HashMap<K,T,H,Eq>::Insert(K & key, T & value, bool fOverwrite, int * pihsndOut)
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
	// If it is, store the value if overwriting is allowed, otherwise return an error value.
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
				m_prghsnd[ihsnd].PutValue(value);
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
	// Check whether to increase the number of buckets to redistribute the wealth.
	// Calculate the average depth of hash collection chains.
	// If greater than or equal to two, increase the number of buckets.
	int chsndAvgDepth = (m_ihsndLim - m_chsndFree) / m_cBuckets;
	if (chsndAvgDepth > 2)
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
	new((void *)&m_prghsnd[ihsnd]) HashNode(key, value, nHash, m_prgihsndBuckets[ie]);
	m_prgihsndBuckets[ie] = ihsnd;
	if (pihsndOut)
		*pihsndOut = ihsnd;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Search the HashMap for the given key, and return true if the key is found or false if the
	key is not found.  If the key is found and the given pointer is not NULL, copy the
	associated value to that memory location.

	@param key Reference to a key object.
	@param pvalueRet Pointer to an empty object for storing a copy of the value associated with
					the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	bool HashMap<K,T,H,Eq>::Retrieve(K & key, T * pvalueRet)
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
			if (pvalueRet)
				*pvalueRet = m_prghsnd[ihsnd].GetValue();
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Remove the element with the given key from the stored HashMap.  This potentially
	invalidates existing iterators for this HashMap.  If the key is not found in the
	HashMap, then nothing is deleted.

	@param key Reference to a key object.

	@return True if the key is found, and something is actually deleted; otherwise, false.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	bool HashMap<K,T,H,Eq>::Delete(K & key)
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
	Free all the memory used by the HashMap.  When done, only the minimum amount of
	bookkeeping memory is still taking up space, and any internal pointers all been set
	to NULL.  The appropriate destructor is called for all key and value objects stored
	in the HashMap before the memory space is freed.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	void HashMap<K,T,H,Eq>::Clear()
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
	Copy the content of one HashMap to another.  An exception is thrown if there are any errors.

	@param hmKT Reference to the other HashMap.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	void HashMap<K,T,H,Eq>::CopyTo(HashMap<K,T,H,Eq> & hmKT)
{
	AssertObj(this);
	AssertObj(&hmKT);
	hmKT.Clear();
	iterator itmm;
	for (itmm = Begin(); itmm != End(); ++itmm)
		hmKT.Insert(itmm->GetKey(), itmm->GetValue());
}

/*----------------------------------------------------------------------------------------------
	Copy the content of one HashMap to another.  An exception is thrown if there are any errors.

	@param phmKT Pointer to the other HashMap.

	@exception E_POINTER if phmKT is NULL.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	void HashMap<K,T,H,Eq>::CopyTo(HashMap<K,T,H,Eq> * phmKT)
{
	if (!phmKT)
		ThrowHr(WarnHr(E_POINTER));
	CopyTo(*phmKT);
}

/*----------------------------------------------------------------------------------------------
	If the given key is found in the HashMap, return true, and if the provided index pointer
	is not NULL, also store the internal index value in the indicated memory location.
	If the given key is NOT found in the HashMap, return false and ignore the provided index
	pointer.

	@param key Reference to a key object.
	@param pihsndRet Pointer to an integer for returning the internal index where the
					key-value pair is stored.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	bool HashMap<K,T,H,Eq>::GetIndex(K & key, int * pihsndRet)
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
	If the given internal HashMap index is valid, return true, and if the provided pointer to a
	key object is not NULL, also copy the indexed key to the indicated memory location.
	If the given internal index is NOT valid, return false, and ignore the provided key
	object pointer.

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param pkeyRet Pointer to an empty key object for storing a copy of the key found at the
				indexed location.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	bool HashMap<K,T,H,Eq>::IndexKey(int ihsnd, K * pkeyRet)
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
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	If the given internal HashMap index is valid, return true, and if the provided pointer to an
	object is not NULL, also copy the indexed value to the indicated memory location.
	If the given internal index is NOT valid, return false, and ignore the provided
	object pointer.

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param pvalueRet Pointer to an empty object for storing a copy of the value found at the
				indexed location.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	bool HashMap<K,T,H,Eq>::IndexValue(int ihsnd, T * pvalueRet)
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
	Return the number of items (key-value pairs) stored in the HashMap.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	int HashMap<K,T,H,Eq>::Size()
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
template<class K, class T, class H, class Eq>
	int HashMap<K,T,H,Eq>::_BucketCount()
{
	AssertObj(this);
	return m_cBuckets;
}

/*----------------------------------------------------------------------------------------------
	Return the number of buckets (hash slots) that do not point to a list of HashNode objects.
	This is useful only for debugging the hash map mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	int HashMap<K,T,H,Eq>::_EmptyBuckets()
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
	objects in the hash map.  This is useful only for debugging the hash map mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	int HashMap<K,T,H,Eq>::_BucketsUsed()
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
	of the hash map.  This is useful only for debugging the hash map mechanism itself.
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H, class Eq>
	int HashMap<K,T,H,Eq>::_FullestBucket()
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
template<class T, class H, class Eq>
	HashMapStrUni<T,H,Eq>::HashMapStrUni()
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
template<class T, class H, class Eq>
	HashMapStrUni<T,H,Eq>::~HashMapStrUni()
{
	Clear();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that references the first key and value stored in the HashMapStrUni.
	If the hash map is empty, Begin returns the same value as End.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	typename HashMapStrUni<T,H,Eq>::iterator HashMapStrUni<T,H,Eq>::Begin()
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
	HashMapStrUni.  If the hash map is empty, End returns the same value as Begin.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	typename HashMapStrUni<T,H,Eq>::iterator HashMapStrUni<T,H,Eq>::End()
{
	AssertObj(this);
	iterator ithmsu(this, m_ihsndLim);
	return ithmsu;
}

/*----------------------------------------------------------------------------------------------
	Add one key and value to the HashMapStrUni.  Insert potentially invalidates existing
	iterators for this HashMapStrUni.  An exception is thrown if there are any errors.

	@param stuKey Reference to the key StrUni object.  An internal copy is made of this object.
	@param value Reference to the object associated with the key.  An internal copy is
					 made of this object.
	@param fOverwrite Optional flag (defaults to false) to allow a value already associated
					with this key to be replaced by this value.
	@param pihsndOut Optional pointer to an integer for returning the internal index where the
					key-value pair is stored.

	@exception E_INVALIDARG if fOverwrite is not true and the key already is stored with a value
					in this HashMapStrUni.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	void HashMapStrUni<T,H,Eq>::Insert(StrUni & stuKey, T & value, bool fOverwrite,
										  int * pihsndOut)
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
	// check whether this key is already used
	// if it is, store the value if overwriting is allowed, otherwise complain
	H hshsu;
	Eq eqlsu;
	int ihsnd;
	int nHash = hshsu(stuKey);
	int ie = (unsigned)nHash % m_cBuckets;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			eqlsu(stuKey, m_prghsnd[ihsnd].GetKey()))
		{
			if (fOverwrite)
			{
				m_prghsnd[ihsnd].PutValue(value);
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
		if (cNewBuckets && cNewBuckets > m_cBuckets)
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
		if ((!pNewNodes) && (iNewMax > 32))
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
	// call constructor on previously allocated memory
	new((void *)&m_prghsnd[ihsnd]) HashNode(stuKey, value, nHash, m_prgihsndBuckets[ie]);
	m_prgihsndBuckets[ie] = ihsnd;
	if (pihsndOut)
		*pihsndOut = ihsnd;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Search the HashMapStrUni for the given key, and return true if the key is found or false if
	the key is not found.  If the key is found and the given pointer is not NULL, copy the
	associated value to that memory location.

	@param stuKey Reference to a key StrUni object.
	@param pvalueRet Pointer to an empty object for storing a copy of the value associated with
					the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool HashMapStrUni<T,H,Eq>::Retrieve(StrUni & stuKey, T * pvalueRet)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hshsu;
	Eq eqlsu;
	int nHash = hshsu(stuKey);
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			eqlsu(stuKey, m_prghsnd[ihsnd].GetKey()))
		{
			if (pvalueRet)
				*pvalueRet = m_prghsnd[ihsnd].GetValue();
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Search the HashMapStrUni for the given key, and return true if the key is found or false if
	the key is not found.  If the key is found and the given pointer is not NULL, copy the
	associated value to that memory location.

	@param bstrKey Either a BSTR or a pointer to an array of Unicode characters.
	@param cchwKey Number of wide characters in bstrKey, which may be greater than the number
					of actual Unicode characters due to surrogate pairs.  (-1 means to use the
					size stored in the BSTR.)
	@param pvalueRet Pointer to an empty object for storing a copy of the value associated with
					the key, if one exists.  This may be NULL.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool HashMapStrUni<T,H,Eq>::Retrieve(BSTR bstrKey, int cchwKey, T * pvalueRet)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hshsu;
	Eq eqlsu;
	int nHash = hshsu(bstrKey, cchwKey);
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			eqlsu(m_prghsnd[ihsnd].GetKey(), bstrKey, cchwKey))
		{
			if (pvalueRet)
				*pvalueRet = m_prghsnd[ihsnd].GetValue();
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Remove the element with the given key from the stored HashMapStrUni.  This potentially
	invalidates existing iterators for this HashMapStrUni.  If the key is not found in the
	HashMapStrUni, then nothing is deleted.

	@param stuKey Reference to a key StrUni object.

	@return True if the key is found, and something is actually deleted; otherwise, false.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool HashMapStrUni<T,H,Eq>::Delete(StrUni & stuKey)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hshsu;
	Eq eqlsu;
	int nHash = hshsu(stuKey);
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	int ihsndPrev = -1;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			eqlsu(stuKey, m_prghsnd[ihsnd].GetKey()))
		{
			if (-1 == ihsndPrev)
				m_prgihsndBuckets[ie] = m_prghsnd[ihsnd].GetNext();
			else
				m_prghsnd[ihsndPrev].PutNext(m_prghsnd[ihsnd].GetNext());
			m_prghsnd[ihsnd].~HashNode();		// ensures member destructors are called
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
	Free all the memory used by the HashMapStrUni.  When done, only the minimum amount of
	bookkeeping memory is still taking up space, and any internal pointers all been set
	to NULL.  The appropriate destructor is called for all key and value objects stored
	in the HashMapStrUni before the memory space is freed.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	void HashMapStrUni<T,H,Eq>::Clear()
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return;
	int ihsnd;
	for (ihsnd = 0; ihsnd < m_ihsndLim; ++ihsnd)
	{
		if (m_prghsnd[ihsnd].InUse())
		{
			m_prghsnd[ihsnd].~HashNode();	// ensures member destructors are called
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
	Copy the content of one HashMapStrUni to another.  An exception is thrown if there are any
	errors.

	@param hmsuT Reference to the other HashMapStrUni.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	void HashMapStrUni<T,H,Eq>::CopyTo(HashMapStrUni<T,H,Eq> & hmsuT)
{
	AssertObj(this);
	AssertObj(&hmsuT);
	hmsuT.Clear();
	iterator itmm;
	for (itmm = Begin(); itmm != End(); ++itmm)
		hmsuT.Insert(itmm->GetKey(), itmm->GetValue());
}

/*----------------------------------------------------------------------------------------------
	Copy the content of one HashMapStrUni to another.  An exception is thrown if there are any
	errors.

	@param phmsuT Pointer to the other HashMapStrUni.

	@exception E_POINTER if phmsuT is NULL.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	void HashMapStrUni<T,H,Eq>::CopyTo(HashMapStrUni<T,H,Eq> * phmsuT)
{
	if (!phmsuT)
		ThrowHr(WarnHr(E_POINTER));
	CopyTo(*phmsuT);
}

/*----------------------------------------------------------------------------------------------
	If the given key is found in the HashMapStrUni, return true, and if the provided index
	pointer is not NULL, also store the internal index value in the indicated memory location.
	If the given key is NOT found in the HashMapStrUni, return false and ignore the provided
	index pointer.

	@param stuKey Reference to a key StrUni object.
	@param pihsndRet Pointer to an integer for returning the internal index where the
					key-value pair is stored.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool HashMapStrUni<T,H,Eq>::GetIndex(StrUni & stuKey, int * pihsndRet)
{
	AssertObj(this);
	if (!m_prgihsndBuckets)
		return false;
	H hshsu;
	Eq eqlsu;
	int nHash = hshsu(stuKey);
	int ie = (unsigned)nHash % m_cBuckets;
	int ihsnd;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			eqlsu(stuKey, m_prghsnd[ihsnd].GetKey()))
		{
			if (pihsndRet)
				*pihsndRet = ihsnd;
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	If the given internal HashMapStrUni index is valid, return true, and if the provided pointer
	to a StrUni object is not NULL, also copy the indexed key to the indicated memory location.
	If the given internal index is NOT valid, return false, and ignore the provided StrUni
	object pointer.

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param pstuKeyRet Pointer to an empty key StrUni object for storing a copy of the key found
				at the indexed location.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool HashMapStrUni<T,H,Eq>::IndexKey(int ihsnd, StrUni * pstuKeyRet)
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
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	If the given internal HashMapStrUni index is valid, return true, and if the provided pointer
	to an object is not NULL, also copy the indexed value to the indicated memory location.
	If the given internal index is NOT valid, return false, and ignore the provided
	object pointer.

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param pvalueRet Pointer to an empty object for storing a copy of the value found at the
				indexed location.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool HashMapStrUni<T,H,Eq>::IndexValue(int ihsnd, T * pvalueRet)
{
	AssertObj(this);
	if ((ihsnd < 0) || (ihsnd >= m_ihsndLim))
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
	Constructor.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	HashMapChars<T,H,Eq>::HashMapChars()
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
template<class T, class H, class Eq>
	HashMapChars<T,H,Eq>::~HashMapChars()
{
	Clear();
}

/*----------------------------------------------------------------------------------------------
	Return an iterator that references the first key and value stored in the HashMapChars.
	If the hash map is empty, Begin returns the same value as End.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	typename HashMapChars<T,H,Eq>::iterator HashMapChars<T,H,Eq>::Begin()
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
	HashMapChars.  If the hash map is empty, End returns the same value as Begin.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	typename HashMapChars<T,H,Eq>::iterator HashMapChars<T,H,Eq>::End()
{
	AssertObj(this);
	iterator ithmc(this, m_ihsndLim);
	return ithmc;
}

/*----------------------------------------------------------------------------------------------
	Add one key and value to the HashMapChars.  Insert potentially invalidates existing
	iterators for this HashMapChars.  An exception is thrown if there are any errors.

	@param pszKey Pointer to the key character string.  An internal copy is made of this string.
	@param value Reference to the object associated with the key.  An internal copy is
					 made of this object.
	@param fOverwrite Optional flag (defaults to false) to allow a value already associated
					with this key to be replaced by this value.
	@param pihsndOut Optional pointer to an integer for returning the internal index where the
					key-value pair is stored.

	@exception E_INVALIDARG if fOverwrite is not true and the key already is stored with a value
					in this HashMapChars.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	void HashMapChars<T,H,Eq>::Insert(const char * pszKey, T & value, bool fOverwrite,
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
	int nHash = hasher(pszKey);
	int ie = (unsigned)nHash % m_cBuckets;
	for (ihsnd = m_prgihsndBuckets[ie]; ihsnd != -1; ihsnd = m_prghsnd[ihsnd].GetNext())
	{
		if ((nHash == m_prghsnd[ihsnd].GetHash()) &&
			equal(pszKey, m_prghsnd[ihsnd].GetKey()))
		{
			if (fOverwrite)
			{
				m_prghsnd[ihsnd].PutValue(value);
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
		if ((!pNewNodes) && (iNewMax > 32))
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
#ifndef WIN32
	const char * psz = strdup(pszKey);
#else
	const char * psz = _strdup(pszKey);
#endif
	if (!psz)
		ThrowHr(WarnHr(E_OUTOFMEMORY));
	// call constructor on previously allocated memory
	new((void *)&m_prghsnd[ihsnd]) HashNode(psz, value, nHash, m_prgihsndBuckets[ie]);
	m_prgihsndBuckets[ie] = ihsnd;
	if (pihsndOut)
		*pihsndOut = ihsnd;
	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Search the HashMapChars for the given key, and return true if the key is found or false if
	the key is not found.  If the key is found and the given pointer is not NULL, copy the
	associated value to that memory location.

	@param pszKey Pointer to a key character string.
	@param pvalueRet Pointer to an empty object for storing a copy of the value associated with
					the key, if one exists.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool HashMapChars<T,H,Eq>::Retrieve(const char * pszKey, T * pvalueRet)
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
			if (pvalueRet)
				*pvalueRet = m_prghsnd[ihsnd].GetValue();
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Remove the element with the given key from the stored HashMapChars.  This potentially
	invalidates existing iterators for this HashMapChars.  If the key is not found in the
	HashMapChars, then nothing is deleted.

	@param pszKey Pointer to a key character string.

	@return True if the key is found, and something is actually deleted; otherwise, false.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool HashMapChars<T,H,Eq>::Delete(const char * pszKey)
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
			m_prghsnd[ihsnd].~HashNode();		// ensures member destructors are called
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
	Free all the memory used by the HashMapChars.  When done, only the minimum amount of
	bookkeeping memory is still taking up space, and any internal pointers all been set
	to NULL.  The appropriate destructor is called for all value objects stored
	in the HashMapChars before the memory space is freed.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	void HashMapChars<T,H,Eq>::Clear()
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
			m_prghsnd[ihsnd].~HashNode();	// ensures member destructors are called
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
	Copy the content of one HashMapChars to another.  An exception is thrown if there are any
	errors.

	@param hmcT Reference to the other HashMapChars.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	void HashMapChars<T,H,Eq>::CopyTo(HashMapChars<T,H,Eq> & hmcT)
{
	AssertObj(this);
	AssertObj(&hmcT);
	hmcT.Clear();
	iterator itmm;
	for (itmm = Begin(); itmm != End(); ++itmm)
		hmcT.Insert(itmm->GetKey(), itmm->GetValue());
}

/*----------------------------------------------------------------------------------------------
	Copy the content of one HashMapChars to another.  An exception is thrown if there are any
	errors.

	@param phmcT Pointer to the other HashMapChars.

	@exception E_POINTER if phmcT is NULL.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	void HashMapChars<T,H,Eq>::CopyTo(HashMapChars<T,H,Eq> * phmcT)
{
	if (!phmcT)
		ThrowHr(WarnHr(E_POINTER));
	CopyTo(*phmcT);
}

/*----------------------------------------------------------------------------------------------
	If the given key is found in the HashMapChars, return true, and if the provided index
	pointer is not NULL, also store the internal index value in the indicated memory location.
	If the given key is NOT found in the HashMapChars, return false and ignore the provided
	index pointer.

	@param pszKey Pointer to a key character string.
	@param pihsndRet Pointer to an integer for returning the internal index where the
					key-value pair is stored.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool HashMapChars<T,H,Eq>::GetIndex(const char * pszKey, int * pihsndRet)
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
	If the given internal HashMapChars index is valid, return true, and if the provided address
	of a character string pointer is not NULL, also copy a pointer to the indexed key to the
	indicated memory location.  If the given internal index is NOT valid, return false, and
	ignore the provided pointer.

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param ppszKeyRet Address of an empty character string pointer for storing a pointer to the
				key string found at the indexed location.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool HashMapChars<T,H,Eq>::IndexKey(int ihsnd, const char ** ppszKeyRet)
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
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	If the given internal HashMapChars index is valid, return true, and if the provided pointer
	to an object is not NULL, also copy the indexed value to the indicated memory location.
	If the given internal index is NOT valid, return false, and ignore the provided
	object pointer.

	@param ihsnd Internal index value returned earlier by GetIndex or Insert.
	@param pvalueRet Pointer to an empty object for storing a copy of the value found at the
				indexed location.
----------------------------------------------------------------------------------------------*/
template<class T, class H, class Eq>
	bool HashMapChars<T,H,Eq>::IndexValue(int ihsnd, T * pvalueRet)
{
	AssertObj(this);
	if ((ihsnd < 0) || (ihsnd >= m_ihsndLim))
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

// Local Variables:
// mode:C++
// c-file-style:"cellar"
// tab-width:4
// End:

#endif /*HASHMAP_I_C_INCLUDED*/
