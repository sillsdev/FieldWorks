/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GpHashMap.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This provides a set of template collection classes to replace std::map.  Their primary
	reason to exist is to allow explicit checking for internal memory allocation failures.
	They also automate reference counting using smart pointers for generic reference counted
	objects.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef GPHASHMAP_H_INCLUDED
#define GPHASHMAP_H_INCLUDED

#include "HashMap.h"		// for HashObj, EqlObj, HashStrUni, EqlStrUni, HashChars, EqlChars
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Hash map template collection class whose keys are objects of an arbitrary class, customized
	to store generic smart pointers.

	Hungarian: hm[K]q[TVal]
----------------------------------------------------------------------------------------------*/
template<class K, class TVal, class H = HashObj, class Eq = EqlObj> class GpHashMap
{
public:
	typedef GenSmartPtr<TVal> SmartPtr;

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

		HashNode()
			: m_key(K()), m_pval(NULL), m_nHash(0), m_ihsndNext(0)
		{
		}
		HashNode(K & key, TVal * pval, int nHash, int ihsndNext = -1)
			: m_key(key), m_pval(pval), m_nHash(nHash), m_ihsndNext(ihsndNext)
		{
			if (m_pval)
				m_pval->AddRef();
		}
		~HashNode()
		{
			if (m_pval)
			{
				m_pval->Release();
				m_pval = NULL;
			}
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
		void PutValue(TVal * pval)
		{
			if (m_pval)
				m_pval->Release();
			m_pval = pval;
			if (m_pval)
				m_pval->AddRef();
		}
		SmartPtr & GetValue()
		{
			return (SmartPtr &)m_pval;
		}
		void PutNext(int ihsndNext)
		{
			m_ihsndNext = ihsndNext;
		}
		int GetNext()
		{
			return m_ihsndNext;
		}
		void PutHash(int nHash)
		{
			m_nHash = nHash;
		}
		int GetHash()
		{
			return m_nHash;
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
		TVal * m_pval;
		int m_nHash;
		int m_ihsndNext;	// -1 means end of list, -(ihsnd + 3) for free list members
	};

	/*------------------------------------------------------------------------------------------
		This provides an iterator for stepping through all HashNodes stored in the hash map.
		This is useful primarily for saving the contents of a hash map to a file.

		Hungarian: ithm[K]q[TVal]
	------------------------------------------------------------------------------------------*/
	class iterator
	{
	public:
		//:> Constructors/destructors/etc.

		iterator()
			: m_phmParent(NULL), m_ihsnd(0)
		{
		}
		iterator(GpHashMap<K,TVal,H,Eq> * phm, int ihsnd)
			: m_phmParent(phm), m_ihsnd(ihsnd)
		{
		}
		iterator(const iterator & v)
			: m_phmParent(v.m_phmParent), m_ihsnd(v.m_ihsnd)
		{
		}
		~iterator()
		{
		}

		//:> Other public methods

		iterator & operator = (const iterator & ithm)
		{
			m_phmParent = ithm.m_phmParent;
			m_ihsnd = ithm.m_ihsnd;
			return *this;
		}
		SmartPtr & operator * ()
		{
			Assert(m_phmParent);
			Assert(m_phmParent->m_prghsnd);
			Assert(m_ihsnd < m_phmParent->m_ihsndLim);
			return m_phmParent->m_prghsnd[m_ihsnd].GetValue();
		}
		HashNode * operator -> ()
		{
			Assert(m_phmParent);
			Assert(m_phmParent->m_prghsnd);
			Assert(m_ihsnd < m_phmParent->m_ihsndLim);
			return &m_phmParent->m_prghsnd[m_ihsnd];
		}
		iterator & operator ++ ()
		{
			Assert(m_phmParent);
			++m_ihsnd;
			//
			// make sure that this new HashNode is actually in use
			//
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
		SmartPtr & GetValue()
		{
			Assert(m_phmParent);
			Assert(m_phmParent->m_prghsnd);
			Assert(m_ihsnd < m_phmParent->m_ihsndLim);
			return m_phmParent->m_prghsnd[m_ihsnd].GetValue();
		}
		K & GetKey()
		{
			Assert(m_phmParent);
			Assert(m_phmParent->m_prghsnd);
			Assert(m_ihsnd < m_phmParent->m_ihsndLim);
			return m_phmParent->m_prghsnd[m_ihsnd].GetKey();
		}
		int GetHash()
		{
			Assert(m_phmParent);
			Assert(m_phmParent->m_prghsnd);
			Assert(m_ihsnd < m_phmParent->m_ihsndLim);
			return m_phmParent->m_prghsnd[m_ihsnd].GetHash();
		}

	protected:
		//:> Member variables

		GpHashMap<K,TVal,H,Eq> * m_phmParent;
		int m_ihsnd;
	};
	friend class iterator;

	//:> Constructors/destructors/etc.

	GpHashMap();
	~GpHashMap();

	//:> Other public methods

	iterator Begin();
	iterator End();
	void Insert(K & key, TVal * pval, bool fOverwrite = false, int * pihsndOut = NULL);
	bool Retrieve(K & key, SmartPtr & qfooRet);
	bool Delete(K & key);
	void Clear();
	void CopyTo(GpHashMap<K,TVal,H,Eq> & hmKqfoo);
	void CopyTo(GpHashMap<K,TVal,H,Eq> * phmKqfoo);

	bool GetIndex(K & key, int * pihsndRet);
	bool IndexKey(int ihsnd, K * pkeyRet);
	bool IndexValue(int ihsnd, SmartPtr & qfooRet);

	int Size();

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

	// Protected methods
	//:Ignore

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
	// copying a GpHashMap with the copy constructor is *BAD*!!
	GpHashMap(GpHashMap<K,TVal,H,Eq> & vec)
	{
		Assert(false);
	}
	// copying a GpHashMap with the = operator is *BAD*!!
	GpHashMap<K,TVal,H,Eq> & operator = (GpHashMap<K,TVal,H,Eq> & vec)
	{
		Assert(false);
		return *this;
	}
	//:End Ignore
};

/*----------------------------------------------------------------------------------------------
	Hash map template collection class whose keys are StrUni objects (Unicode strings),
	customized to store generic smart pointers.

	Hungarian: hmsuq[TVal]
----------------------------------------------------------------------------------------------*/
template<class TVal, class H = HashStrUni, class Eq = EqlStrUni> class GpHashMapStrUni
	: public GpHashMap<StrUni,TVal,H,Eq>
{
	using GpHashMap<StrUni,TVal,H,Eq>::m_prgihsndBuckets;
	using GpHashMap<StrUni,TVal,H,Eq>::m_cBuckets;
	using GpHashMap<StrUni,TVal,H,Eq>::m_prghsnd;
	using GpHashMap<StrUni,TVal,H,Eq>::m_ihsndLim;
	using GpHashMap<StrUni,TVal,H,Eq>::m_ihsndMax;
	using GpHashMap<StrUni,TVal,H,Eq>::m_ihsndFirstFree;
	using GpHashMap<StrUni,TVal,H,Eq>::FreeListIdx;
public:
	typedef typename GpHashMap<StrUni,TVal,H,Eq>::iterator iterator;
	typedef typename GpHashMap<StrUni,TVal,H,Eq>::SmartPtr SmartPtr;
	typedef typename GpHashMap<StrUni,TVal,H,Eq>::HashNode HashNode;

	//:> Constructors/destructors/etc.

	GpHashMapStrUni();
	~GpHashMapStrUni();

	//:> Other public methods

	iterator Begin();
	iterator End();
	void Insert(StrUni & stuKey, TVal * pval, bool fOverwrite = false,
				   int * pihsndOut = NULL);
	bool Retrieve(StrUni & stuKey, SmartPtr & qfooRet);
	bool Retrieve(BSTR bstrKey, int cchwKey, SmartPtr & qfooRet);
	bool Delete(StrUni & stuKey);
	void Clear();
	void CopyTo(GpHashMapStrUni<TVal,H,Eq> & hmsuqfoo);
	void CopyTo(GpHashMapStrUni<TVal,H,Eq> * phmsuqfoo);

	bool GetIndex(StrUni & stuKey, int * pihsndRet);
	bool IndexKey(int ihsnd, StrUni * pstuKeyRet);
	bool IndexValue(int ihsnd, SmartPtr & qfooRet);

	/*------------------------------------------------------------------------------------------
		Return the number of items (key-value pairs) stored in the GpHashMapStrUni.
	------------------------------------------------------------------------------------------*/
	int Size()
	{
		return GpHashMap<StrUni,TVal,H,Eq>::Size();
	}

	//:Ignore
#ifdef DEBUG
	int _BucketCount()
	{
		return GpHashMap<StrUni,TVal,H,Eq>::_BucketCount();
	}
	int _EmptyBuckets()
	{
		return GpHashMap<StrUni,TVal,H,Eq>::_EmptyBuckets();
	}
	int _BucketsUsed()
	{
		return GpHashMap<StrUni,TVal,H,Eq>::_BucketsUsed();
	}
	int _FullestBucket()
	{
		return GpHashMap<StrUni,TVal,H,Eq>::_FullestBucket();
	}
	bool AssertValid()
	{
		return GpHashMap<StrUni,TVal,H,Eq>::AssertValid();
	}
#endif
	//:End Ignore

private:
	//:Ignore
	// copying a GpHashMapStrUni with the copy constructor is *BAD*!!
	GpHashMapStrUni(GpHashMapStrUni<TVal,H,Eq> & vec)
	{
		Assert(false);
	}
	// copying a GpHashMapStrUni with the = operator is *BAD*!!
	GpHashMapStrUni<TVal,H,Eq> & operator = (GpHashMapStrUni<TVal,H,Eq> & vec)
	{
		Assert(false);
		return *this;
	}
	//:End Ignore
};

/*----------------------------------------------------------------------------------------------
	Hash map template collection class whose keys are C style NUL-terminated character (char)
	strings, customized to store generic smart pointers.

	Hungarian: hmcq[TVal]
----------------------------------------------------------------------------------------------*/
template<class TVal, class H = HashChars, class Eq = EqlChars> class GpHashMapChars
	: public GpHashMap<const char *,TVal,H,Eq>
{
	using GpHashMap<const char *,TVal,H,Eq>::m_prgihsndBuckets;
	using GpHashMap<const char *,TVal,H,Eq>::m_cBuckets;
	using GpHashMap<const char *,TVal,H,Eq>::m_prghsnd;
	using GpHashMap<const char *,TVal,H,Eq>::m_ihsndLim;
	using GpHashMap<const char *,TVal,H,Eq>::m_ihsndMax;
	using GpHashMap<const char *,TVal,H,Eq>::m_ihsndFirstFree;
	using GpHashMap<const char *,TVal,H,Eq>::FreeListIdx;
public:
	typedef typename GpHashMap<const char *,TVal,H,Eq>::iterator iterator;
	typedef typename GpHashMap<const char *,TVal,H,Eq>::SmartPtr SmartPtr;
	typedef typename GpHashMap<const char *,TVal,H,Eq>::HashNode HashNode;

	//:> Constructors/destructors/etc.

	GpHashMapChars();
	~GpHashMapChars();

	//:> Other public methods

	iterator Begin();
	iterator End();
	void Insert(const char * pszKey, TVal * pval, bool fOverwrite = false,
				   int * pihsndOut = NULL);
	bool Retrieve(const char * pszKey, SmartPtr & qfooRet);
	bool Delete(const char * pszKey);
	void Clear();
	void CopyTo(GpHashMapChars<TVal,H,Eq> & hmcqfoo);
	void CopyTo(GpHashMapChars<TVal,H,Eq> * phmcqfoo);

	bool GetIndex(const char * pszKey, int * pihsndRet);
	bool IndexKey(int ihsnd, const char ** ppszkeyRet);
	bool IndexValue(int ihsnd, SmartPtr & qfooRet);

	/*------------------------------------------------------------------------------------------
		Return the number of items (key-value pairs) stored in the GpHashMapChars.
	------------------------------------------------------------------------------------------*/
	int Size()
	{
		return GpHashMap<const char *,TVal,H,Eq>::Size();
	}

	//:Ignore
#ifdef DEBUG
	int _BucketCount()
	{
		return GpHashMap<const char *,TVal,H,Eq>::_BucketCount();
	}
	int _EmptyBuckets()
	{
		return GpHashMap<const char *,TVal,H,Eq>::_EmptyBuckets();
	}
	int _BucketsUsed()
	{
		return GpHashMap<const char *,TVal,H,Eq>::_BucketsUsed();
	}
	int _FullestBucket()
	{
		return GpHashMap<const char *,TVal,H,Eq>::_FullestBucket();
	}
	bool AssertValid()
	{
		return GpHashMap<const char *,TVal,H,Eq>::AssertValid();
	}
#endif
	//:End Ignore

private:
	//:Ignore
	// copying a GpHashMapChars with the copy constructor is *BAD*!!
	GpHashMapChars(GpHashMapChars<TVal,H,Eq> & vec)
	{
		Assert(false);
	}
	// copying a GpHashMapChars with the = operator is *BAD*!!
	GpHashMapChars<TVal,H,Eq> & operator = (GpHashMapChars<TVal,H,Eq> & vec)
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

#endif /*GPHASHMAP_H_INCLUDED*/
