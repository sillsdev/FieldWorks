/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: Set.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This provides a template collection class to replace std::set.  Its primary reason
	to exist is to allow explicit checking for internal memory allocation failures.
----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef SET_H_INCLUDED
#define SET_H_INCLUDED
//:End Ignore

#include "UtilHashMap.h"

namespace gr
{

/*----------------------------------------------------------------------------------------------
	Set template collection class for storing unique objects of an arbitrary class.

	Hungarian: set[T]
----------------------------------------------------------------------------------------------*/
template<class T, class H = HashObj, class Eq = EqlObj> class Set
{
public:
	//:> Internal helper classes.

	/*------------------------------------------------------------------------------------------
		This is the basic data structure for storing a value in a set.  In order to handle hash
		collisions, this structure is a member of a linked list.  It should not be used outside
		the implementation of Set<T, H, Eq> itself.

		Hungarian: hsnd
	------------------------------------------------------------------------------------------*/
	class HashNode
	{
	public:
		// Default Constructor.
		HashNode(void)
			: m_value(T()), m_nHash(0), m_ihsndNext(0)
		{
		}
		// Constructor.
		HashNode(T & value, int nHash, int ihsndNext = -1)
			: m_value(value), m_nHash(nHash), m_ihsndNext(ihsndNext)
		{
		}
		// Destructor.
		~HashNode()
		{
		}

		//:> Member variable access.

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
		//:> Member variables.

		T m_value;
		int	m_nHash;
		int	m_ihsndNext;	// -1 means end of list, -(ihsnd + 3) for free list members.
	};

	/*------------------------------------------------------------------------------------------
		This provides an iterator for stepping through all HashNodes stored in the set.
		This is useful primarily for saving the contents of a set to a file.

		Hungarian: itset[T]
	------------------------------------------------------------------------------------------*/
	class iterator
	{
	public:
		//:> Constructors/destructors/etc.

		iterator() : m_psetParent(0L), m_irghsnd(0)
		{
		}
		iterator(Set<T,H,Eq> * pset, int irghsnd) : m_psetParent(pset), m_irghsnd(irghsnd)
		{
		}
		iterator(const iterator & v) : m_psetParent(v.m_psetParent), m_irghsnd(v.m_irghsnd)
		{
		}
		~iterator()
		{
		}

		//:> Other public methods.

		iterator & operator = (const iterator & itseto)
		{
			m_psetParent = itseto.m_psetParent;
			m_irghsnd = itseto.m_irghsnd;
			return *this;
		}
		T & operator * (void)
		{
			AssertPtr(m_psetParent);
			AssertObj(m_psetParent);
			AssertPtr(m_psetParent->m_prghsnd);
			Assert(0 <= m_irghsnd && m_irghsnd < m_psetParent->m_ihsndLim);
			return m_psetParent->m_prghsnd[m_irghsnd].GetValue();
		}
		HashNode * operator -> (void)
		{
			AssertPtr(m_psetParent);
			AssertObj(m_psetParent);
			AssertPtr(m_psetParent->m_prghsnd);
			Assert(0 <= m_irghsnd && m_irghsnd < m_psetParent->m_ihsndLim);
			return &m_psetParent->m_prghsnd[m_irghsnd];
		}
		iterator & operator ++ (void)
		{
			AssertPtr(m_psetParent);
			AssertObj(m_psetParent);
			AssertPtr(m_psetParent->m_prghsnd);
			Assert(0 <= m_irghsnd && m_irghsnd <= m_psetParent->m_ihsndLim);
			++m_irghsnd;
			//
			// Make sure that this new HashNode is actually in use.
			//
			while (m_irghsnd < m_psetParent->m_ihsndLim)
			{
				if (m_psetParent->m_prghsnd[m_irghsnd].InUse())
					return *this;
				// Skip to the next one and check it.
				++m_irghsnd;
			}
			if (m_irghsnd > m_psetParent->m_ihsndLim)
				m_irghsnd = m_psetParent->m_ihsndLim;
			return *this;
		}
		bool operator == (const iterator & itseto)
		{
			return (m_psetParent == itseto.m_psetParent) && (m_irghsnd == itseto.m_irghsnd);
		}
		bool operator != (const iterator & itseto)
		{
			return (m_psetParent != itseto.m_psetParent) || (m_irghsnd != itseto.m_irghsnd);
		}
		T & GetValue(void)
		{
			AssertPtr(m_psetParent);
			AssertObj(m_psetParent);
			AssertPtr(m_psetParent->m_prghsnd);
			Assert(0 <= m_irghsnd && m_irghsnd < m_psetParent->m_ihsndLim);
			Assert(m_psetParent->m_prghsnd[m_irghsnd].InUse());
			return m_psetParent->m_prghsnd[m_irghsnd].GetValue();
		}
		int GetHash()
		{
			AssertPtr(m_psetParent);
			AssertObj(m_psetParent);
			AssertPtr(m_psetParent->m_prghsnd);
			Assert(0 <= m_irghsnd && m_irghsnd < m_psetParent->m_ihsndLim);
			Assert(m_psetParent->m_prghsnd[m_irghsnd].InUse());
			return m_psetParent->m_prghsnd[m_irghsnd].GetHash();
		}
		int GetIndex()
		{
			AssertPtr(m_psetParent);
			AssertObj(m_psetParent);
			AssertPtr(m_psetParent->m_prghsnd);
			Assert(0 <= m_irghsnd && m_irghsnd < m_psetParent->m_ihsndLim);
			Assert(m_psetParent->m_prghsnd[m_irghsnd].InUse());
			return m_irghsnd;
		}

	protected:
		// Member variables.

		Set<T,H,Eq> * m_psetParent;
		int m_irghsnd;
	};
	friend class iterator;

	//:> Constructors/destructors/etc.

	Set();
	~Set();

	//:> Other public methods.

	iterator Begin();
	iterator End();
	void Insert(T & value, int * pihsndOut = 0L);
	bool IsMember(T & value);
	bool Delete(T & value);
	void Clear();
	bool GetIndex(T & value, int * pihsndRet);
	bool IndexValue(int ihsnd, T * pvalueRet);
	int Size();
	bool Equals(Set<T, H, Eq> & itset);


	//:Ignore
#ifdef DEBUG
	// For debugging.

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
	//:> Member variables.

	int * m_prgihsndBuckets;
	int m_cBuckets;
	HashNode * m_prghsnd;
	int m_ihsndLim;
	int m_ihsndMax;
	int m_ihsndFirstFree;

	//:> Protected methods.

	/*------------------------------------------------------------------------------------------
		Map between real index and "free list" index.  Note that this mapping is bidirectional.

		@param ihsnd
	------------------------------------------------------------------------------------------*/
	int FreeListIdx(int ihsnd)
	{
		return -(ihsnd + 3);
	}
};



} // namesapce gr

#if !defined(GR_NAMESPACE)
using namespace gr;
#endif

#include "Set_i.cpp"

#endif /*SET_H_INCLUDED*/
