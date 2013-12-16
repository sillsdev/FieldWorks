/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: Vector_i.cpp
Responsibility: Steve McConnel
Last reviewed: April 13, 1999 (further review needed)

Description:
	This file provides the implementations of methods for the Vector template collection class.
	It is used as an #include file in any file which explicitly instantiates any particular
	type of Vector<T>.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef VECTOR_I_C_INCLUDED
#define VECTOR_I_C_INCLUDED
#pragma warning(disable: 4345) // new((void *)peDst) T(); is OK


/***********************************************************************************************
	Methods
********************************************************************************//*:End Ignore*/

/*----------------------------------------------------------------------------------------------
	Default constructor.
----------------------------------------------------------------------------------------------*/
template<class T>
	Vector<T>::Vector(void)
{
	m_prge = NULL;
	m_ieLim = 0;
	m_ieMax = 0;

	// Negative growth factor causes successive doubling.
	m_ceGrowthFactor = -kceDefaultInitialGrowthFactor;
}


/*----------------------------------------------------------------------------------------------
	Copy constructor.
----------------------------------------------------------------------------------------------*/
template<class T>
	Vector<T>::Vector(const Vector<T> & vec)
{
	m_prge = NULL;
	m_ieLim = 0;
	m_ieMax = 0;

	// Negative growth factor causes successive doubling.
	m_ceGrowthFactor = -kceDefaultInitialGrowthFactor;

	if (vec.m_ieLim > 0)
		InsertMulti(0, vec.m_ieLim, vec.m_prge);
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
template<class T>
	Vector<T>::~Vector(void)
{
	Clear();
}


//:Ignore
/*----------------------------------------------------------------------------------------------
	Resize the vector so it can hold ieLim total elements.
	The caller should have already verified that a resize is needed.
	If fFitExactly is false, m_ieMax will always increase.
	REVIEW SteveMc: does this use the correct allocation strategy algorithm?
----------------------------------------------------------------------------------------------*/
template<class T>
	void Vector<T>::_Realloc(int ieLim, bool fFitExactly)
{
	AssertObj(this);
	Assert(ieLim >= 0);
	// Checks for overflow.
	Assert(ieLim > m_ieMax || fFitExactly && ieLim != m_ieMax);
	if (ieLim < 0 || (ieLim <= m_ieMax && !fFitExactly) || ieLim == m_ieMax)
		throw std::invalid_argument("ieLim");

	int ieMax;

	if (!fFitExactly)
	{
		if (m_ceGrowthFactor == 0)
			m_ceGrowthFactor = -kceDefaultInitialGrowthFactor;
		ieMax = m_ieMax + (m_ceGrowthFactor < 0 ? -m_ceGrowthFactor : m_ceGrowthFactor);
		if (ieMax < ieLim)
			ieMax = ieLim;
	}
	else if (!ieLim)
	{
		if (m_prge)
		{
			int ceGrow = m_ceGrowthFactor;
			Clear();
			m_ceGrowthFactor = ceGrow;
		}
		return;
	}
	else
		ieMax = ieLim;

	Assert(ieMax != m_ieMax);

	int cbNew = ieMax * isizeof(T);
	T * prge = reinterpret_cast<T *>(realloc(m_prge, cbNew));
	if (!prge)
		ThrowHr(WarnHr(E_OUTOFMEMORY));

	m_prge = prge;
	m_ieMax = ieMax;
	if (m_ceGrowthFactor < 0)
		m_ceGrowthFactor *= 2;

	AssertObj(this);
}
//:End Ignore


/*----------------------------------------------------------------------------------------------
	Resize ensures that the vector has space allocated for at least the given number of
	elements, and that exactly that many elements are initialized.  This may or may not result
	in memory being allocated, and may result in objects being initialized or deinitialized
	(constructed or destructed).

	Increasing the size of the vector causes the new elements to be initialized with the copy
	constructor using the provided value.  Decreasing the size of the vector causes the
	destructor to be called for each of the deleted objects.  (If the stored elements are
	primitive types or pointers, there is no constructor or destructor to call.)

	If Resize returns without error, then Size will return ce until the next element is added or
	removed from the vector.

	@param ce is the number of elements desired to be in the vector.
	@param eIns is a reference to the element to store in the vector.
----------------------------------------------------------------------------------------------*/
template<class T>
	void Vector<T>::Resize(int ce, const T & eIns)
{
	AssertObj(this);
	Assert(ce >= 0);
	if (ce < 0)
		throw std::invalid_argument("ce");

	if (m_ieLim < ce)
		Insert(m_ieLim, ce - m_ieLim, eIns);
	else if (ce < m_ieLim)
		Delete(ce, m_ieLim);
}


/*----------------------------------------------------------------------------------------------
	Resize ensures that the vector has space allocated for at least the given number of
	elements, and that exactly that many elements are initialized.  This may or may not result
	in memory being allocated, and may result in objects being initialized or deinitialized
	(constructed or destructed).

	Increasing the size of the vector causes the new elements to be initialized with the default
	constructor.  Decreasing the size of the vector causes the destructor to be called for each
	of the deleted objects.  (If the stored elements are primitive types or pointers, there is
	no constructor or destructor to call.)

	If Resize returns without error, then Size will return ce until the next element is added or
	removed from the vector.

	@param ce is the number of elements desired to be in the vector.
----------------------------------------------------------------------------------------------*/
template<class T>
	void Vector<T>::Resize(int ce)
{
	AssertObj(this);
	Assert(ce >= 0);
	if (ce < 0)
		throw std::invalid_argument("ce");

	if (m_ieLim < ce)
		Insert(m_ieLim, ce - m_ieLim, T());
	else if (ce < m_ieLim)
		Delete(ce, m_ieLim);
}


/*----------------------------------------------------------------------------------------------
	Insert adds the given item to the stored vector the given number of times at the given
	location.  It shifts any elements at the given location or following to make room for the
	inserted elements.  It potentially invalidates existing iterators for this vector.

	@param ieIns is a zero-based index into the vector, indicating where to add the element(s).
				ieIns must be in range.
	@param ceIns is the number of copies of eIns to insert into the vector.
	@parame eIns is the element to store in the vector.
----------------------------------------------------------------------------------------------*/
template<class T>
	void Vector<T>::Insert(int ieIns, int ceIns, const T & eIns)
{
	AssertObj(this);
	Assert((uint)ieIns <= (uint)m_ieLim);
	Assert(ceIns >= 0);
	if ((uint)ieIns > (uint)m_ieLim)
		throw std::invalid_argument("ieIns");
	if (ceIns < 0)
		throw std::invalid_argument("ceIns");

	// Checks for overflow.
	Assert(ceIns + m_ieLim >= ceIns);

	if (!ceIns)
		return;

	// Make sure we have enough memory allocated.
	EnsureSpace(ceIns);

	if (ieIns < m_ieLim)
		MoveItems(m_prge + ieIns, m_prge + ieIns + ceIns, m_ieLim - ieIns);
	m_ieLim += ceIns;

	T * pe = m_prge + ieIns;
	T * peLim = pe + ceIns;

	for ( ; pe < peLim; ++pe)
	{
		// Call constructor on previously allocated memory.
		new((void *)pe) T(eIns);
	}

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	InsertMulti inserts a range of elements into the vector, shifting any elements at the given
	location or following to make room for them.  It potentially invalidates existing iterators
	for this vector.

	@param ieIns is a zero-based index into the vector, indicating where to add the elements.
			It must be in range.
	@param ceIns is the number of elements in prgeIns.
	@param prgeIns points to an array of elements to insert into the vector.
----------------------------------------------------------------------------------------------*/
template<class T>
	void Vector<T>::InsertMulti(int ieIns, int ceIns, const T * prgeIns)
{
	AssertObj(this);
	Assert((uint)ieIns <= (uint)m_ieLim);
	Assert(ceIns >= 0);
	if ((uint)ieIns > (uint)m_ieLim)
		throw std::invalid_argument("ieIns");
	if (ceIns < 0)
		throw std::invalid_argument("ceIns");

	// Checks for overflow.
	Assert(ceIns + m_ieLim >= ceIns);

	if (!ceIns)
		return;

	// Make sure we have enough memory allocated.
	EnsureSpace(ceIns);

	MoveItems(m_prge, m_prge + ceIns, m_ieLim - ieIns);
	m_ieLim += ceIns;

	T * pe = m_prge + ieIns;
	T * peLim = pe + ceIns;

	for ( ; pe < peLim; ++pe, ++prgeIns)
	{
		// Call constructor on previously allocated memory.
		new((void *)pe) T(*prgeIns);
	}

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Delete removes one or more elements from the stored vector, shifting the following elements
	in the stored vector to maintain compact storage.  It potentially invalidates existing
	iterators for this vector.

	The appropriate destructor is called before overwriting (or clearing) an object's location
	in a vector.  (If the stored elements are primitive types or pointers, there is no
	destructor to call.)

	@param ieMin is a zero-based index into the stored vector, indicating the first element in
				a range of elements to delete.  ieMin must be in range.
	@param ieLim is a zero-based index into the stored vector, indicating the element that
				replaces the first one deleted (ieMin).  In other words, ieLim is one past the
				last element deleted: exactly ieLim - ieMin elements are deleted.  ieLim must
				be in range.  If ieLim is less than or equal to ieMin, then the vector is left
				unchanged.
	@param prge is an optional pointer to a range of elements for storing a copy of the deleted
				elements.  If it is not NULL, the indicated range must have room for at least
				ieLim - ieMin elements.
----------------------------------------------------------------------------------------------*/
template<class T>
	void Vector<T>::Delete(int ieMin, int ieLim, T * prge)
{
	AssertObj(this);
	Assert((uint)ieMin <= (uint)ieLim && (uint)ieLim <= (uint)m_ieLim);
	AssertArrayN(prge, ieLim - ieMin);
	if ((uint)ieLim > (uint)m_ieLim)
		throw std::invalid_argument("ieLim");
	if ((uint)ieMin > (uint)ieLim)
		throw std::invalid_argument("ieMin");

	if (ieMin == ieLim)
		return;

	if (prge)
	{
		// More efficient to destroy the existing objects at the destination, then do a raw
		// binary copy across than to invoke assignment operators.
		::DestroyRange(prge, ieLim - ieMin);
		CopyItems(m_prge + ieMin, prge, ieLim - ieMin);
	}
	else
		::DestroyRange(m_prge + ieMin, ieLim - ieMin);

	if (ieLim < m_ieLim)
		MoveItems(m_prge + ieLim, m_prge + ieMin, m_ieLim - ieLim);
	m_ieLim -= ieLim - ieMin;

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Pop erases the last element stored in the vector.  vx.Pop(pe) is equivalent to
	vx.Delete(vx.Size() - 1, pe) when vx.Size() is greater than zero.

	@param peRet is an optional pointer to an element that receives a copy of the popped
			element.  If it is NULL, then the popped element is discarded without any copy being
			made.

	@return true if an element was removed from the vector, false if the vector was already
			empty.
----------------------------------------------------------------------------------------------*/
template<class T>
	bool Vector<T>::Pop(T * peRet)
{
	AssertObj(this);
	AssertPtrN(peRet);

	if (!m_ieLim)
		return false;

	Delete(m_ieLim - 1, m_ieLim, peRet);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Clear frees all the memory used by the stored vector.  When it is done, only the minimum
	amount of bookkeeping memory is still taking up space, and any internal pointers have all
	been set to NULL.

	If the growth factor value is negative, it is reset to zero.  If it is positive, then it is
	left unchanged.

	The appropriate destructor is called for every element stored in a vector before the vector
	memory is freed.

@null{	REVIEW SteveMc: policy for resetting m_ceGrowthFactor	}
----------------------------------------------------------------------------------------------*/
template<class T>
	void Vector<T>::Clear()
{
	AssertObj(this);

	if (m_prge)
	{
		::DestroyRange(m_prge, m_ieLim);
		free(m_prge);
		m_prge = NULL;
		m_ieLim = 0;
		m_ieMax = 0;
	}
	if (m_ceGrowthFactor < 0)
		m_ceGrowthFactor = 0;

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Replace allows a number of elements to be inserted, deleted, or replaced in a single
	operation.

	@param ieMin is the zero-based index into the stored vector for the first element to be
			replaced.  ieMin must be in range.
	@param ieLimDel is the zero-based index into the stored vector just past the last element to
			be replaced.  ieLimDel must be in range.
	@param prgeIns points to an optional array of elements to use as the replacement values.  If
			it is NULL, then the default constructor value is used.  If prgeIns is not NULL,
			then it must have ceIns elements.
	@param ceIns is the count of how many elements replace those between ieMin and ieLimDel.  It
			must be greater than or equal to zero.  If prge is NULL and ceIns is not zero, then
			the default constructor is used for each new element stored into the vector.
----------------------------------------------------------------------------------------------*/
template<class T>
	void Vector<T>::Replace(int ieMin, int ieLimDel, const T * prgeIns, int ceIns)
{
	AssertObj(this);
	Assert((uint)ieMin <= (uint)ieLimDel && (uint)ieLimDel <= (uint)m_ieLim);
	Debug( if (prgeIns) AssertArray(prgeIns, ceIns); )

	if ((uint)ieLimDel > (uint)m_ieLim)
		throw std::invalid_argument("ieLimDel");
	if ((uint)ieMin > (uint)ieLimDel)
		throw std::invalid_argument("ieMin");

	int ceDel = ieLimDel - ieMin;
	int ceNew = m_ieLim + (ceIns - ceDel);

	// Make sure we have enough memory allocated
	if (ceNew > m_ieMax)
		EnsureSpace(ceNew - m_ieLim);

	// handle the elements being deleted
	if (ceDel > 0)
		::DestroyRange(m_prge + ieMin, ieLimDel - ieMin);

	// Adjust the gap for inserting after deleting.
	if (ceIns != ceDel)
	{
		MoveItems(m_prge + ieLimDel, m_prge + ieMin + ceIns, m_ieLim - ieLimDel);
		m_ieLim = ceNew;
	}

	// Handle the elements being inserted. If prgeIns is not NULL, copy from prgeIns to gap.
	// Otherwise, set each element in gap to default value.
	if (ceIns > 0)
	{
		T * peDst = m_prge + ieMin;
		T * peLim = peDst + ceIns;

		if (prgeIns)
		{
			const T * peSrc;
			for (peSrc = prgeIns; peDst < peLim; ++peDst, ++peSrc)
			{
				// Call constructor on previously allocated memory.
				new((void *)peDst) T(*peSrc);
			}
		}
		else
		{
			for ( ; peDst < peLim; ++peDst)
			{
				// Call constructor on previously allocated memory.
				new((void *)peDst) T();
			}
		}
	}

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	RawSlide slides a range of values along the vector.  The values from ieMin to ieLim are
	moved as a block so that the new index of the element previously at ieMin is now
	ieMin + die.  Note that some elements are overwritten without being destructed, and others
	are copied without use of a copy constructor!  This can safely be used only for things like
	ints that have no important constructor / destructor behavior.

	@param ieMin is a zero-based index into the stored vector for the first element to move.  It
			must be in range.
	@param ieLim is a zero-based index into the stored vector just past the last element to be
			moved.  It must be in range, and must be greater than or equal to ieMin.
	@param die is the number of places within the vector to move, either positive or negative.
----------------------------------------------------------------------------------------------*/
template<class T>
	void Vector<T>::RawSlide(int ieMin, int ieLim, int die)
{
	AssertObj(this);
	Assert((uint)ieMin <= (uint)ieLim && (uint)ieLim <= (uint)m_ieLim);
	// The destination should lie within the vector.
	Assert(-ieMin <= die && die <= m_ieLim - ieLim);

	if ((uint)ieLim > (uint)m_ieLim)
		throw std::invalid_argument("ieLim");
	if ((uint)ieMin > (uint)ieLim)
		throw std::invalid_argument("ieMin");

	// No movement is allowed.
	if (die && ieLim != ieMin)
		MoveItems(m_prge + ieMin, m_prge + ieMin + die, ieLim - ieMin);
}


/*----------------------------------------------------------------------------------------------
	CopyTo copies the content of this vector to another vector, replacing any previous content
	of the other vector.  The copying is in the opposite direction of the assignment operator.

	@param vec is a reference to the other vector.
----------------------------------------------------------------------------------------------*/
template<class T>
	void Vector<T>::CopyTo(Vector<T> & vec)
{
	AssertObj(this);
	AssertObj(&vec);
	if (&vec != this)
		vec.Replace(0, vec.m_ieLim, m_prge, m_ieLim);
}

#endif /*VECTOR_I_C_INCLUDED*/
