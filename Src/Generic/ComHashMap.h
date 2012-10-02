/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ComHashMap.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This provides a set of template collection classes to replace std::map.  Their primary
	reason to exist is to allow explicit checking for internal memory allocation failures.
	They also automate calling AddRef and Release for most uses.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef COMHASHMAP_H_INCLUDED
#define COMHASHMAP_H_INCLUDED

#include "HashMap.h"		// for HashObj, EqlObj, HashStrUni, EqlStrUni, HashChars, EqlChars
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Hash map templace collection class whose keys are objects of an arbitrary class, customized
	to store COM interface pointers.

	Hungarian: hm[K]q[Foo]
----------------------------------------------------------------------------------------------*/
template<class K, class IFoo, class H = HashObj, class Eq = EqlObj> class ComHashMap
{
public:
	typedef ComSmartPtr<IFoo> SmartPtr;

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
			: m_key(K()), m_pfoo(NULL), m_nHash(0), m_ihsndNext(0)
		{
		}
		HashNode(K & key, IFoo * pfoo, int nHash, int ihsndNext = -1)
			: m_key(key), m_pfoo(pfoo), m_nHash(nHash), m_ihsndNext(ihsndNext)
		{
			if (m_pfoo)
				m_pfoo->AddRef();
		}
		~HashNode()
		{
			if (m_pfoo)
			{
				IFoo * pfoo = m_pfoo;

				// Do this first in case Release causes us to try delete ourselves again
				m_pfoo = NULL;

				pfoo->Release();
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
		void PutValue(IFoo * pfoo)
		{
			if (m_pfoo)
				m_pfoo->Release();
			m_pfoo = pfoo;
			if (m_pfoo)
				m_pfoo->AddRef();
		}
		SmartPtr & GetValue()
		{
			return (SmartPtr &)m_pfoo;
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
		IFoo * m_pfoo;
		int m_nHash;
		int m_ihsndNext;	// -1 means end of list, -(ihsnd + 3) for free list members
	};

	/*------------------------------------------------------------------------------------------
		This provides an iterator for stepping through all HashNodes stored in the hash map.
		This is useful primarily for saving the contents of a hash map to a file.

		Hungarian: ithm[K]q[Foo]
	------------------------------------------------------------------------------------------*/
	class iterator
	{
	public:
		//:> Constructors/destructors/etc.

		iterator()
			: m_phmParent(NULL), m_ihsnd(0)
		{
		}
		iterator(ComHashMap<K,IFoo,H,Eq> * phm, int ihsnd)
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

		ComHashMap<K,IFoo,H,Eq> * m_phmParent;
		int m_ihsnd;
	};
	friend class iterator;

	//:> Constructors/destructors/etc.

	ComHashMap();
	~ComHashMap();

	//:> Other public methods

	iterator Begin();
	iterator End();
	void Insert(K & key, IFoo * pfoo, bool fOverwrite = false, int * pihsndOut = NULL);
	bool Retrieve(K & key, SmartPtr & qfooRet);
	bool Delete(K & key);
	void Clear();
	void CopyTo(ComHashMap<K,IFoo,H,Eq> & hmKqfoo);
	void CopyTo(ComHashMap<K,IFoo,H,Eq> * phmKqfoo);

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
	int m_chsndFree; // count of things in ihsndFirstFree list

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

private:
	//:Ignore
	// copying a ComHashMap with the copy constructor is *BAD*!!
	ComHashMap(ComHashMap<K,IFoo,H,Eq> & vec)
	{
		Assert(false);
	}
	// copying a ComHashMap with the = operator is *BAD*!!
	ComHashMap<K,IFoo,H,Eq> & operator = (ComHashMap<K,IFoo,H,Eq> & vec)
	{
		Assert(false);
		return *this;
	}
	//:End Ignore
};

/*----------------------------------------------------------------------------------------------
	Hash map template collection class whose keys are StrUni objects (Unicode strings),
	customized to store COM interface pointers.

	Hungarian: hmsuq[Foo]
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H = HashStrUni, class Eq = EqlStrUni> class ComHashMapStrUni
	: public ComHashMap<StrUni,IFoo,H,Eq>
{
	using ComHashMap<StrUni,IFoo,H,Eq>::m_prgihsndBuckets;
	using ComHashMap<StrUni,IFoo,H,Eq>::m_cBuckets;
	using ComHashMap<StrUni,IFoo,H,Eq>::m_prghsnd;
	using ComHashMap<StrUni,IFoo,H,Eq>::m_ihsndLim;
	using ComHashMap<StrUni,IFoo,H,Eq>::m_ihsndMax;
	using ComHashMap<StrUni,IFoo,H,Eq>::m_ihsndFirstFree;
	using ComHashMap<StrUni,IFoo,H,Eq>::FreeListIdx;
	using ComHashMap<StrUni,IFoo,H,Eq>::m_chsndFree;
public:
	typedef typename ComHashMap<StrUni,IFoo,H,Eq>::iterator iterator;
	typedef typename ComHashMap<StrUni,IFoo,H,Eq>::SmartPtr SmartPtr;
	typedef typename ComHashMap<StrUni,IFoo,H,Eq>::HashNode HashNode;

	//:> Constructors/destructors/etc.

	ComHashMapStrUni();
	~ComHashMapStrUni();

	//:> Other public methods

	iterator Begin();
	iterator End();
	void Insert(StrUni & stuKey, IFoo * pfoo, bool fOverwrite = false,
				   int * pihsndOut = NULL);
	bool Retrieve(StrUni & stuKey, SmartPtr & qfooRet);
	bool Retrieve(BSTR bstrKey, int cchwKey, SmartPtr & qfooRet);
	bool Delete(StrUni & stuKey);
	void Clear();
	void CopyTo(ComHashMapStrUni<IFoo,H,Eq> & hmsuqfoo);
	void CopyTo(ComHashMapStrUni<IFoo,H,Eq> * phmsuqfoo);

	bool GetIndex(StrUni & stuKey, int * pihsndRet);
	bool IndexKey(int ihsnd, StrUni * pstuKeyRet);
	bool IndexValue(int ihsnd, SmartPtr & qfooRet);

	/*------------------------------------------------------------------------------------------
		Return the number of items (key-value pairs) stored in the ComHashMapStrUni.
	------------------------------------------------------------------------------------------*/
	int Size()
	{
		return ComHashMap<StrUni,IFoo,H,Eq>::Size();
	}

	//:Ignore
#ifdef DEBUG
	int _BucketCount()
	{
		return ComHashMap<StrUni,IFoo,H,Eq>::_BucketCount();
	}
	int _EmptyBuckets()
	{
		return ComHashMap<StrUni,IFoo,H,Eq>::_EmptyBuckets();
	}
	int _BucketsUsed()
	{
		return ComHashMap<StrUni,IFoo,H,Eq>::_BucketsUsed();
	}
	int _FullestBucket()
	{
		return ComHashMap<StrUni,IFoo,H,Eq>::_FullestBucket();
	}
	bool AssertValid()
	{
		return ComHashMap<StrUni,IFoo,H,Eq>::AssertValid();
	}
#endif
	//:End Ignore

private:
	//:Ignore
	// copying a ComHashMapStrUni with the copy constructor is *BAD*!!
	ComHashMapStrUni(ComHashMapStrUni<IFoo,H,Eq> & vec)
	{
		Assert(false);
	}
	// copying a ComHashMapStrUni with the = operator is *BAD*!!
	ComHashMapStrUni<IFoo,H,Eq> & operator = (ComHashMapStrUni<IFoo,H,Eq> & vec)
	{
		Assert(false);
		return *this;
	}
	//:End Ignore
};

/*----------------------------------------------------------------------------------------------
	Hash map template collection class whose keys are C style NUL-terminated character (char)
	strings, customized to store COM interface pointers.

	Hungarian: hmcq[Foo]
----------------------------------------------------------------------------------------------*/
template<class IFoo, class H = HashChars, class Eq = EqlChars> class ComHashMapChars
	: public ComHashMap<const char *,IFoo,H,Eq>
{
	using ComHashMap<const char *,IFoo,H,Eq>::m_prgihsndBuckets;
	using ComHashMap<const char *,IFoo,H,Eq>::m_cBuckets;
	using ComHashMap<const char *,IFoo,H,Eq>::m_prghsnd;
	using ComHashMap<const char *,IFoo,H,Eq>::m_ihsndLim;
	using ComHashMap<const char *,IFoo,H,Eq>::m_ihsndMax;
	using ComHashMap<const char *,IFoo,H,Eq>::m_ihsndFirstFree;
	using ComHashMap<const char *,IFoo,H,Eq>::FreeListIdx;
	using ComHashMap<const char *,IFoo,H,Eq>::m_chsndFree;
public:
	typedef typename ComHashMap<const char *,IFoo,H,Eq>::iterator iterator;
	typedef typename ComHashMap<const char *,IFoo,H,Eq>::SmartPtr SmartPtr;
	typedef typename ComHashMap<const char *,IFoo,H,Eq>::HashNode HashNode;

	//:> Constructors/destructors/etc.

	ComHashMapChars();
	~ComHashMapChars();

	//:> Other public methods

	iterator Begin();
	iterator End();
	void Insert(const char * pszKey, IFoo * pfoo, bool fOverwrite = false,
				   int * pihsndOut = NULL);
	bool Retrieve(const char * pszKey, SmartPtr & qfooRet);
	bool Delete(const char * pszKey);
	void Clear();
	void CopyTo(ComHashMapChars<IFoo,H,Eq> & hmcqfoo);
	void CopyTo(ComHashMapChars<IFoo,H,Eq> * phmcqfoo);

	bool GetIndex(const char * pszKey, int * pihsndRet);
	bool IndexKey(int ihsnd, const char ** ppszkeyRet);
	bool IndexValue(int ihsnd, SmartPtr & qfooRet);

	/*------------------------------------------------------------------------------------------
		Return the number of items (key-value pairs) stored in the ComHashMapChars.
	------------------------------------------------------------------------------------------*/
	int Size()
	{
		return ComHashMap<const char *,IFoo,H,Eq>::Size();
	}

	//:Ignore
#ifdef DEBUG
	int _BucketCount()
	{
		return ComHashMap<const char *,IFoo,H,Eq>::_BucketCount();
	}
	int _EmptyBuckets()
	{
		return ComHashMap<const char *,IFoo,H,Eq>::_EmptyBuckets();
	}
	int _BucketsUsed()
	{
		return ComHashMap<const char *,IFoo,H,Eq>::_BucketsUsed();
	}
	int _FullestBucket()
	{
		return ComHashMap<const char *,IFoo,H,Eq>::_FullestBucket();
	}
	bool AssertValid()
	{
		return ComHashMap<const char *,IFoo,H,Eq>::AssertValid();
	}
#endif
	//:End Ignore

private:
	//:Ignore
	// copying a ComHashMapChars with the copy constructor is *BAD*!!
	ComHashMapChars(ComHashMapChars<IFoo,H,Eq> & vec)
	{
		Assert(false);
	}
	// copying a ComHashMapChars with the = operator is *BAD*!!
	ComHashMapChars<IFoo,H,Eq> & operator = (ComHashMapChars<IFoo,H,Eq> & vec)
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

#endif /*COMHASHMAP_H_INCLUDED*/
