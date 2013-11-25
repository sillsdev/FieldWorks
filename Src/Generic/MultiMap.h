/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: MultiMap.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This provides a template collection class to replace std::multimap.  Its  primary reason
	to exist is to allow explicit checking for internal memory allocation failures.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef MULTIMAP_H_INCLUDED
#define MULTIMAP_H_INCLUDED
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Multi-map template collection class whose nonunique keys are objects of an arbitrary class.

	Hungarian: mm[K][T]
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H=HashObj, class EqK=EqlObj, class EqT=EqlObj> class MultiMap
{
public:
	//:> Member classes

	/*------------------------------------------------------------------------------------------
		This is the basic data structure for storing one key-value pair in a MultiMap.  In
		order to handle hash collisions (or multiple values per key), this structure is a
		member of a linked list.  This implies that keys and values need not be unique.

		Hungarian: hsnd
	------------------------------------------------------------------------------------------*/
	class HashNode
	{
	public:
		// Default Constructor.
		HashNode(void)
			: m_key(K()), m_value(T()), m_nHash(0), m_ihsndNext(0)
		{
		}
		// Constructor.
		HashNode(K & key, T & value, int nHash, int ihsndNext = -1)
			: m_key(key), m_value(value), m_nHash(nHash), m_ihsndNext(ihsndNext)
		{
		}
		// Destructor.
		~HashNode()
		{
		}

		//:> Member variable access

		void PutKey(K & key)
		{
			m_key = key;
		}
		K & GetKey()
		{
			return m_key;
		}
		void PutValue(T & value)
		{
			m_value = value;
		}
		T & GetValue()
		{
			return m_value;
		}
		void PutHash(int nHash)
		{
			m_nHash = nHash;
		}
		int GetHash()
		{
			return m_nHash;
		}
		void PutNext(int ihsndNext)
		{
			m_ihsndNext = ihsndNext;
		}
		int GetNext()
		{
			return m_ihsndNext;
		}

		/*--------------------------------------------------------------------------------------
			Check whether the given HashNode is being used.

			@return True if this HashNode is being used, or false otherwise.
		--------------------------------------------------------------------------------------*/
		bool InUse()
		{
			return m_ihsndNext >= -1;
		}

	protected:
		//:> Member variables

		K m_key;
		T m_value;
		int	m_nHash;
		int	m_ihsndNext;	// -1 means end of list, -(ihsnd + 3) for free list members.
	};

	/*------------------------------------------------------------------------------------------
		This provides an iterator for stepping through all HashNodes stored in the MultiMap.
		This is useful for saving the contents of a MultiMap to a file, and for stepping through
		all stored nodes with the same key.

		Hungarian: itmm[K][T]
	------------------------------------------------------------------------------------------*/
	class iterator
	{
	public:
		//:> Constructors/destructors/etc.
		iterator()
			: m_pmmParent(NULL), m_ihsnd(0)
		{
		}
		iterator(MultiMap<K,T,H,EqK,EqT> * pmm, int ihsnd)
			: m_pmmParent(pmm), m_ihsnd(ihsnd)
		{
		}
		iterator(const iterator & v)
			: m_pmmParent(v.m_pmmParent), m_ihsnd(v.m_ihsnd)
		{
		}
		~iterator()
		{
		}

		//:> Other public methods
		iterator & operator = (const iterator & itmm)
		{
			m_pmmParent = itmm.m_pmmParent;
			m_ihsnd = itmm.m_ihsnd;
			return *this;
		}
		T & operator * (void)
		{
			AssertPtr(m_pmmParent);
			AssertObj(m_pmmParent);
			AssertPtr(m_pmmParent->m_prghsnd);
			Assert(0 <= m_ihsnd && m_ihsnd < m_pmmParent->m_ihsndLim);
			return m_pmmParent->m_prghsnd[m_ihsnd].GetValue();
		}
		HashNode * operator -> (void)
		{
			AssertPtr(m_pmmParent);
			AssertObj(m_pmmParent);
			AssertPtr(m_pmmParent->m_prghsnd);
			Assert(0 <= m_ihsnd && m_ihsnd < m_pmmParent->m_ihsndLim);
			return &m_pmmParent->m_prghsnd[m_ihsnd];
		}
		iterator & operator ++ (void)
		{
			AssertPtr(m_pmmParent);
			AssertObj(m_pmmParent);
			Assert(0 <= m_ihsnd && m_ihsnd <= m_pmmParent->m_ihsndLim);
			// Note that this iterator works differently than the HashMap iterator, since
			// this iterator is used for stepping through the range of stored values that
			// have the same key as well as stepping through the entire range of stored
			// values.
			if (m_ihsnd < m_pmmParent->m_ihsndLim)
			{
				// step to the HashNode in the chain; if we hit the end of the chain, find
				// the next Bucket that is not empty and use its first HashNode.
				int ihsndNext = m_pmmParent->m_prghsnd[m_ihsnd].GetNext();
				if (ihsndNext == -1)
				{
					int nHash = m_pmmParent->m_prghsnd[m_ihsnd].GetHash();
					int ie = (unsigned)nHash % m_pmmParent->m_cBuckets;
					while (++ie < m_pmmParent->m_cBuckets)
					{
						ihsndNext = m_pmmParent->m_prgihsndBuckets[ie];
						if (ihsndNext != -1)
						{
							Assert(m_pmmParent->m_prghsnd[ihsndNext].InUse());
							break;
						}
					}
					if (ie == m_pmmParent->m_cBuckets)
						ihsndNext = m_pmmParent->m_ihsndLim;
				}
				m_ihsnd = ihsndNext;
			}
			return *this;
		}
		bool operator == (const iterator & itmm)
		{
			return (m_pmmParent == itmm.m_pmmParent) && (m_ihsnd == itmm.m_ihsnd);
		}
		bool operator != (const iterator & itmm)
		{
			return (m_pmmParent != itmm.m_pmmParent) || (m_ihsnd != itmm.m_ihsnd);
		}
		T & GetValue(void)
		{
			AssertPtr(m_pmmParent);
			AssertObj(m_pmmParent);
			AssertPtr(m_pmmParent->m_prghsnd);
			Assert(0 <= m_ihsnd && m_ihsnd < m_pmmParent->m_ihsndLim);
			return m_pmmParent->m_prghsnd[m_ihsnd].GetValue();
		}
		K & GetKey(void)
		{
			AssertPtr(m_pmmParent);
			AssertObj(m_pmmParent);
			AssertPtr(m_pmmParent->m_prghsnd);
			Assert(0 <= m_ihsnd && m_ihsnd < m_pmmParent->m_ihsndLim);
			return m_pmmParent->m_prghsnd[m_ihsnd].GetKey();
		}
		int GetHash()
		{
			AssertPtr(m_pmmParent);
			AssertObj(m_pmmParent);
			AssertPtr(m_pmmParent->m_prghsnd);
			Assert(0 <= m_ihsnd && m_ihsnd < m_pmmParent->m_ihsndLim);
			return m_pmmParent->m_prghsnd[m_ihsnd].GetHash();
		}

	protected:
		//:> Member variables
		MultiMap<K,T,H,EqK,EqT> * m_pmmParent;
		int m_ihsnd;
	};
	friend class iterator;

	//:> Constructors/destructors/etc.

	MultiMap();
	~MultiMap();

	//:> Other public methods

	iterator Begin();
	iterator End();
	void Insert(K & key, T & value, int * pihsndOut = NULL);
	bool Retrieve(K & key, iterator * pitMin, iterator * pitLim);
	bool Delete(K & key);
	bool Delete(K & key, T & value);
	void Clear();
	void CopyTo(MultiMap<K,T,H,EqK,EqT> & mmKT);
	void CopyTo(MultiMap<K,T,H,EqK,EqT> * pmmKT);

	bool GetIndex(K & key, int * pihsndRet);
	bool IndexKey(int ihsnd, K * pkeyRet);
	int KeyCount(int ihsnd);
	bool IndexValue(int ihsnd, T * pvalueRet);
	int CountUniqueKeys();

	int Size();

	//:Ignore
#ifdef DEBUG
	int _BucketCount()
	{
		AssertObj(this);
		return m_cBuckets;
	}
	int _EmptyBuckets();
	int _BucketsUsed();
	int _FullestBucket();
	int * _GetBuckets()
	{
		AssertObj(this);
		return m_prgihsndBuckets;
	}
	bool AssertValid()
	{
		AssertPtrN(m_prgihsndBuckets);
		Assert(m_prgihsndBuckets || !m_cBuckets);
		Assert(!m_prgihsndBuckets || m_cBuckets);
		AssertArray(m_prgihsndBuckets, m_cBuckets);
		AssertPtrN(m_prghsnd);
		Assert(m_prghsnd || !m_ihsndMax);
		Assert(!m_prghsnd || m_ihsndMax);
		AssertArray(m_prghsnd, m_ihsndMax);
		Assert(0 <= m_ihsndLim && m_ihsndLim <= m_ihsndMax);
		Assert(-1 <= FreeListIdx(m_ihsndFirstFree));
		Assert(FreeListIdx(m_ihsndFirstFree) < m_ihsndLim);
		return true;
	}
#endif
	//:End Ignore

protected:
	//:> Member variables
	int * m_prgihsndBuckets;
	int m_cBuckets;
	HashNode * m_prghsnd;
	int m_ihsndLim;
	int m_ihsndMax;
	int m_ihsndFirstFree;		// stores -(ihsnd + 3)
	int m_chsndFree;

	//:> Protected methods
	//:Ignore
	void _Insert(int ihsndIn, int nHash, K & key);
	int _GetLastDupKey(int ihsnd);

	/*------------------------------------------------------------------------------------------
		Map between real index and "free list" index.  Note that this mapping is bidirectional.
	------------------------------------------------------------------------------------------*/
	int FreeListIdx(int ihsnd)
	{
		return -(ihsnd + 3);
	}
	//:End Ignore

private:
	//:Ignore
	// copying a MultiMap with the copy constructor is *BAD*!!
	MultiMap(MultiMap<K,T,H,EqK,EqT> & mm)
	{
		Assert(false);
	}
	// copying a MultiMap with the = operator is *BAD*!!
	MultiMap<K,T,H,EqK,EqT> & operator = (MultiMap<K,T,H,EqK,EqT> & mm)
	{
		Assert(false);
		return *this;
	}
	//:End Ignore
};

// Local Variables:
// mode:C++
// c-file-style:"cellar"
// tab-width:4
// End:

#endif /*MULTIMAP_H_INCLUDED*/
