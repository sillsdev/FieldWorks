/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: HashMap.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This provides a set of template collection classes to replace std::map.  Their primary
	reason to exist is to allow explicit checking for internal memory allocation failures.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef HASHMAP_H_INCLUDED
#define HASHMAP_H_INCLUDED
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Functor class for computing a hash value from an arbitrary object.

	Hungarian: hsho
----------------------------------------------------------------------------------------------*/
class HashObj
{
public:
	int operator () (void * pKey, int cbKey);
};

/*----------------------------------------------------------------------------------------------
	Functor class for comparing two arbitrary objects (of the same class) for equality.

	Hungarian: eqlo
----------------------------------------------------------------------------------------------*/
class EqlObj
{
public:
	bool operator () (void * pKey1, void * pKey2, int cbKey);
};

/*----------------------------------------------------------------------------------------------
	Hash map template collection class whose keys are objects of an arbitrary class.

	Hungarian: hm[K][T]
----------------------------------------------------------------------------------------------*/
template<class K, class T, class H = HashObj, class Eq = EqlObj> class HashMap
{
public:
	//:> Member classes

	/*------------------------------------------------------------------------------------------
		This is the basic data structure for storing one key-value pair in a hash map.  In
		order to handle hash collisions, this structure is a member of a linked list.
		Hungarian: hsnd
	------------------------------------------------------------------------------------------*/
	class HashNode
	{
	public:
		//:> Constructors/destructors/etc.

		HashNode(void)
			: m_key(K()), m_value(T()), m_nHash(0), m_ihsndNext(0)
		{
		}
		HashNode(K & key, T & value, int nHash, int ihsndNext = -1)
			: m_key(key), m_value(value), m_nHash(nHash), m_ihsndNext(ihsndNext)
		{
		}
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
		int	m_ihsndNext;	// -1 means end of list, -(ihsnd + 3) for free list members
	};

	/*------------------------------------------------------------------------------------------
		This provides an iterator for stepping through all HashNodes stored in the hash map.
		This is useful primarily for saving the contents of a hash map to a file.

		Hungarian: ithm[K][T]
	------------------------------------------------------------------------------------------*/
	class iterator
	{
	public:
		// Constructors/destructors/etc.

		iterator() : m_phmParent(NULL), m_ihsnd(0)
		{
		}
		iterator(HashMap<K,T,H,Eq> * phm, int ihsnd) : m_phmParent(phm), m_ihsnd(ihsnd)
		{
		}
		iterator(const iterator & v) : m_phmParent(v.m_phmParent), m_ihsnd(v.m_ihsnd)
		{
		}
		~iterator()
		{
		}

		// Other public methods

		iterator & operator = (const iterator & ithm)
		{
			m_phmParent = ithm.m_phmParent;
			m_ihsnd = ithm.m_ihsnd;
			return *this;
		}
		T & operator * (void)
		{
			Assert(m_phmParent);
			Assert(m_phmParent->m_prghsnd);
			Assert(m_ihsnd < m_phmParent->m_ihsndLim);
			return m_phmParent->m_prghsnd[m_ihsnd].GetValue();
		}
		HashNode * operator -> (void)
		{
			Assert(m_phmParent);
			Assert(m_phmParent->m_prghsnd);
			Assert(m_ihsnd < m_phmParent->m_ihsndLim);
			return &m_phmParent->m_prghsnd[m_ihsnd];
		}
		iterator & operator ++ (void)
		{
			Assert(m_phmParent);
			++m_ihsnd;
			// make sure that this new HashNode is actually in use
			while (m_ihsnd < m_phmParent->m_ihsndLim)
			{
				if (m_phmParent->m_prghsnd[m_ihsnd].InUse())
					return *this;
				// skip to the next one and check it
				++m_ihsnd;
			}
			if (m_ihsnd > m_phmParent->m_ihsndLim)
				m_ihsnd = m_phmParent->m_ihsndLim;
			return *this;
		}
		bool operator == (const iterator & ithm)
		{
			return (m_phmParent == ithm.m_phmParent) && (m_ihsnd == ithm.m_ihsnd);
		}
		bool operator != (const iterator & ithm)
		{
			return (m_phmParent != ithm.m_phmParent) || (m_ihsnd != ithm.m_ihsnd);
		}
		T & GetValue(void)
		{
			Assert(m_phmParent);
			Assert(m_phmParent->m_prghsnd);
			Assert(m_ihsnd < m_phmParent->m_ihsndLim);
			Assert(m_phmParent->m_prghsnd[m_ihsnd].InUse());
			return m_phmParent->m_prghsnd[m_ihsnd].GetValue();
		}
		K & GetKey(void)
		{
			Assert(m_phmParent);
			Assert(m_phmParent->m_prghsnd);
			Assert(m_ihsnd < m_phmParent->m_ihsndLim);
			Assert(m_phmParent->m_prghsnd[m_ihsnd].InUse());
			return m_phmParent->m_prghsnd[m_ihsnd].GetKey();
		}
		int GetHash()
		{
			Assert(m_phmParent);
			Assert(m_phmParent->m_prghsnd);
			Assert(m_ihsnd < m_phmParent->m_ihsndLim);
			Assert(m_phmParent->m_prghsnd[m_ihsnd].InUse());
			return m_phmParent->m_prghsnd[m_ihsnd].GetHash();
		}
		int GetIndex()
		{
			Assert(m_phmParent);
			Assert(m_phmParent->m_prghsnd);
			Assert(m_ihsnd < m_phmParent->m_ihsndLim);
			Assert(m_phmParent->m_prghsnd[m_ihsnd].InUse());
			return m_ihsnd;
		}

	protected:
		//:> Member variables

		HashMap<K,T,H,Eq> * m_phmParent;
		int m_ihsnd;
	};
	friend class iterator;

	//:> Constructors/destructors/etc.

	HashMap();
	~HashMap();
	HashMap(HashMap<K,T,H,Eq> & hm);

	//:> Other public methods

	iterator Begin();
	iterator End();
	void Insert(K & key, T & value, bool fOverwrite = false, int * pihsndOut = NULL);
	bool Retrieve(K & key, T * pvalueRet);
	bool Delete(K & key);
	void Clear();
	void CopyTo(HashMap<K,T,H,Eq> & hmKT);
	void CopyTo(HashMap<K,T,H,Eq> * phmKT);

	bool GetIndex(K & key, int * pihsndRet);
	bool IndexKey(int ihsnd, K * pkeyRet);
	bool IndexValue(int ihsnd, T * pvalueRet);

	int Size();

	/*------------------------------------------------------------------------------------------
		The assignment operator allows an entire hashmap to be assigned as the value of another
		hashmap.  It throws an error if it runs out of memory.

		@return a reference to this hashmap.  (That is how the assignment operator is defined!)

		@param hm is a reference to the other hashmap.
	------------------------------------------------------------------------------------------*/
	HashMap<K,T,H,Eq> & operator = (HashMap<K,T,H,Eq> & hm)
	{
		hm.CopyTo(this);
		return *this;
	}

	//:Ignore
#ifdef DEBUG
	int _BucketCount();
	int _EmptyBuckets();
	int _BucketsUsed();
	int _FullestBucket();
	bool AssertValid()
	{
		AssertPtrN(m_prgihsndBuckets);
		Assert(m_prgihsndBuckets || !m_cBuckets);
		Assert(!m_prgihsndBuckets || m_cBuckets);
		// JohnT: these AssertArray tests are VERY expensive for large arrays,
		// and have not been very productive in catching bugs.
		//AssertArray(m_prgihsndBuckets, m_cBuckets);
		AssertPtrN(m_prghsnd);
		Assert(m_prghsnd || !m_ihsndMax);
		Assert(!m_prghsnd || m_ihsndMax);
		//AssertArray(m_prghsnd, m_ihsndMax);
		Assert(0 <= m_ihsndLim && m_ihsndLim <= m_ihsndMax);
		Assert(-1 <= FreeListIdx(m_ihsndFirstFree));
		Assert(FreeListIdx(m_ihsndFirstFree) < m_ihsndLim);
		return true;
	}
#endif
	//:End Ignore

protected:
	//:> Member variables

	// JohnT: added my analysis of how the variables are used and the key algorithms.
	//m_cBuckets -- number of 'buckets' allocated, starts as 7 (prime near 10), grows to prime near (old value * 4) when average depth exceeds 2.
	//m_prgihsndBuckets - m_cBuckets * size of int, initially all -1. looked up by hash index, determines first hash node to try in m_prghsnd.
	//m_ihsndMax -- initially 32, number of HashNodes allocated
	//m_prghsnd - array of ihsndMax HashNodes
	//m_ihsndLim - possibly number of HashNodes in use in m_prghsnd? Initially zero...actually limit of range 'in use' including free list...
	//m_ihsndFirstFree - start of chain of free nodes; index into m_prghsnd, linked by m_ihsndNext in HashNodes.  Initially FreeListIdx(-1).
	//m_chsndFree - how many things are in the free list
	//
	//To find where to put something:
	//
	//ie = hash value % m_cBuckets
	//initial index into hashnodes is found from m_prgihsndBuckets[ie]
	//look for key in HashNode at current index if hash matches hash in HashNode;
	//otherwise, use GetNext function of HashNode to find next index to try (which just gives m_ihsndNext from the node).
	//
	//When averate depth too great:
	//realloc m_prgihsndBuckets (~4x larger)
	//set all to -1
	//re-insert each in-use HashNode; algorithm is that each item we encounter that
	// ideally goes at index ie in bucket array becomes the one that goes there,
	// and gets linked to the one previously there if any.
	//
	//Then we figure an index for a node to use. If there's one available at the end we use it
	//(that is, if m_ihsndLim< m_ihsndMax we just increment m_ihsndMax).
	//Otherwise if there's a free list use the first node from it.
	//Otherwise we double the number of Hashnodes and realloc().
	//Then we make that the head node of the bucket.
	//Note that this approach allows the free list to get VERY long if a mixture of insertions and deletions
	//is in progress; hence the free list count, to save having to frequently (each insertion!) loop through it.

	int * m_prgihsndBuckets;
	int m_cBuckets;
	HashNode * m_prghsnd;
	int m_ihsndLim;
	int m_ihsndMax;
	int m_ihsndFirstFree;		// stores -(ihsnd + 3)
	int m_chsndFree; // count of items in free list.

	//:> Protected methods
	//:Ignore

	/*------------------------------------------------------------------------------------------
		Map between real index and "free list" index.  Note that this mapping is bidirectional.
	------------------------------------------------------------------------------------------*/
	int FreeListIdx(int ihsnd)
	{
		return -(ihsnd + 3);
	}
	//:End Ignore
};

/*----------------------------------------------------------------------------------------------
	Functor class for computing a hash value from a StrUni object (Unicode string).

	Hungarian: hshsu
----------------------------------------------------------------------------------------------*/
class HashStrUni
{
public:
	int operator () (StrUni & stuKey);
	int operator () (BSTR bstrKey, int cchwKey = -1);
	int operator () (StrUni * pstuKey, int cbKey);
};

/*----------------------------------------------------------------------------------------------
	Functor class for comparing two StrUni objects (Unicode strings) for equality.

	Hungarian: eqlsu
----------------------------------------------------------------------------------------------*/
class EqlStrUni
{
public:
	bool operator () (StrUni & stuKey1, StrUni & stuKey2);
	bool operator () (StrUni & stuKey1, BSTR bstrKey2, int cchwKey2 = -1);
	bool operator () (StrUni * pstuKey1, StrUni * pstuKey2, int cbKey);
};

/*----------------------------------------------------------------------------------------------
	Hash map template collection class whose keys are StrUni objects (Unicode strings).

	Hungarian: hmsu[T]
----------------------------------------------------------------------------------------------*/
template<class T, class H = HashStrUni, class Eq = EqlStrUni> class HashMapStrUni
	: public HashMap<StrUni, T, H, Eq>
{
	using HashMap<StrUni, T, H, Eq>::m_prgihsndBuckets;
	using HashMap<StrUni, T, H, Eq>::m_cBuckets;
	using HashMap<StrUni, T, H, Eq>::m_prghsnd;
	using HashMap<StrUni, T, H, Eq>::m_ihsndLim;
	using HashMap<StrUni, T, H, Eq>::m_ihsndMax;
	using HashMap<StrUni, T, H, Eq>::m_ihsndFirstFree;
	using HashMap<StrUni, T, H, Eq>::FreeListIdx;
	using HashMap<StrUni, T, H, Eq>::m_chsndFree;
public:
	typedef typename HashMap<StrUni, T, H, Eq>::iterator iterator;
	typedef typename HashMap<StrUni, T, H, Eq>::HashNode HashNode;

	//:> Constructors/destructors/etc.

	HashMapStrUni();
	~HashMapStrUni();

	//:> Other public methods

	iterator Begin();
	iterator End();
	void Insert(StrUni & stuKey, T & value, bool fOverwrite = false, int * pihsndOut = NULL);
	bool Retrieve(StrUni & stuKey, T * pvalueRet);
	bool Delete(StrUni & stuKey);
	void Clear();
	void CopyTo(HashMapStrUni<T,H,Eq> & hmsuT);
	void CopyTo(HashMapStrUni<T,H,Eq> * phmsuT);

	bool GetIndex(StrUni & stuKey, int * pihsndRet);
	bool IndexKey(int ihsnd, StrUni * pstuKeyRet);
	bool IndexValue(int ihsnd, T * pvalueRet);

	bool Retrieve(BSTR bstrKey, int cchwKey, T * pvalueRet);

	/*------------------------------------------------------------------------------------------
		Return the number of items (key-value pairs) stored in the HashMapStrUni.
	------------------------------------------------------------------------------------------*/
	int Size()
	{
		return HashMap<StrUni,T,H,Eq>::Size();
	}

	//:Ignore
#ifdef DEBUG
	int _BucketCount()
	{
		return HashMap<StrUni,T,H,Eq>::_BucketCount();
	}
	int _EmptyBuckets()
	{
		return HashMap<StrUni,T,H,Eq>::_EmptyBuckets();
	}
	int _BucketsUsed()
	{
		return HashMap<StrUni,T,H,Eq>::_BucketsUsed();
	}
	int _FullestBucket()
	{
		return HashMap<StrUni,T,H,Eq>::_FullestBucket();
	}
	bool AssertValid()
	{
		return HashMap<StrUni,T,H,Eq>::AssertValid();
	}
#endif
	//:End Ignore

private:
	//:Ignore
	// copying a HashMapStrUni with the copy constructor is *BAD*!!
	HashMapStrUni(HashMapStrUni<T,H,Eq> & vec)
	{
		Assert(false);
	}
	// copying a HashMapStrUni with the = operator is *BAD*!!
	HashMapStrUni<T,H,Eq> & operator = (HashMapStrUni<T,H,Eq> & vec)
	{
		Assert(false);
		return *this;
	}
	//:End Ignore
};

/*----------------------------------------------------------------------------------------------
	Functor class for computing a hash value from a C style NUL-terminated character (char)
	string

	Hungarian: hshc
----------------------------------------------------------------------------------------------*/
class HashChars
{
public:
	int operator () (const char * pszKey);
	int operator () (const char ** ppszKey, int cbKey);
};

/*----------------------------------------------------------------------------------------------
	Functor class for comparing two C style NUL-terminated character (char) strings for
	equality.

	Hungarian: eqlc
----------------------------------------------------------------------------------------------*/
class EqlChars
{
public:
	bool operator () (const char * pszKey1, const char * pszKey2);
	bool operator () (const char ** ppszKey1, const char ** ppszKey2, int cbKey);
};

/*----------------------------------------------------------------------------------------------
	Hash map template collection class whose keys are C style NUL-terminated character (char)
	strings.

	Hungarian: hmc[T]
----------------------------------------------------------------------------------------------*/
template<class T, class H = HashChars, class Eq = EqlChars> class HashMapChars
	: public HashMap<const char *, T, H, Eq>
{
	using HashMap<const char *, T, H, Eq>::m_prgihsndBuckets;
	using HashMap<const char *, T, H, Eq>::m_cBuckets;
	using HashMap<const char *, T, H, Eq>::m_prghsnd;
	using HashMap<const char *, T, H, Eq>::m_ihsndLim;
	using HashMap<const char *, T, H, Eq>::m_ihsndMax;
	using HashMap<const char *, T, H, Eq>::m_ihsndFirstFree;
	using HashMap<const char *, T, H, Eq>::FreeListIdx;
	using HashMap<const char *, T, H, Eq>::m_chsndFree;
public:
	typedef typename HashMap<const char *, T, H, Eq>::iterator iterator;
	typedef typename HashMap<const char *, T, H, Eq>::HashNode HashNode;

	//:> Constructors/destructors/etc.

	HashMapChars();
	~HashMapChars();

	//:> Other public methods

	iterator Begin();
	iterator End();
	void Insert(const char * pszKey, T & value, bool fOverwrite = false,
				   int * pihsndOut = NULL);
	bool Retrieve(const char * pszKey, T * pvalueRet);
	bool Delete(const char * pszKey);
	void Clear();
	void CopyTo(HashMapChars<T,H,Eq> & hmcT);
	void CopyTo(HashMapChars<T,H,Eq> * phmcT);

	bool GetIndex(const char * pszKey, int * pihsndRet);
	bool IndexKey(int ihsnd, const char ** ppszKeyRet);
	bool IndexValue(int ihsnd, T * pvalueRet);

	/*------------------------------------------------------------------------------------------
		Return the number of items (key-value pairs) stored in the HashMapChars.
	------------------------------------------------------------------------------------------*/
	int Size()
	{
		return HashMap<const char *,T,H,Eq>::Size();
	}

	//:Ignore
#ifdef DEBUG
	int _BucketCount()
	{
		return HashMap<const char *,T,H,Eq>::_BucketCount();
	}
	int _EmptyBuckets()
	{
		return HashMap<const char *,T,H,Eq>::_EmptyBuckets();
	}
	int _BucketsUsed()
	{
		return HashMap<const char *,T,H,Eq>::_BucketsUsed();
	}
	int _FullestBucket()
	{
		return HashMap<const char *,T,H,Eq>::_FullestBucket();
	}
	bool AssertValid()
	{
		return HashMap<const char *,T,H,Eq>::AssertValid();
	}
#endif
	//:End Ignore

private:
	//:Ignore
	// copying a HashMapChars with the copy constructor is *BAD*!!
	HashMapChars(HashMapChars<T,H,Eq> & hmc)
	{
		Assert(false);
	}
	// copying a HashMapChars with the = operator is *BAD*!!
	HashMapChars<T,H,Eq> & operator = (HashMapChars<T,H,Eq> & hmc)
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

#endif /*HASHMAP_H_INCLUDED*/
