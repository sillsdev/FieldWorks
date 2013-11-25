/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: Vector.h
Responsibility: Steve McConnel
Last reviewed: April 13, 1999 (further review needed)

Description:
	This provides a template collection class to replace std::vector.  Its reason to exist is
	to allow explicit checking for memory allocation failures, and to avoid using methods
	that could result in throwing an exception.
REVIEW SteveMc: choice of methods to be inline
				value of kceDefaultInitialGrowthFactor
				use of Asserts
				variations on the Replace method
				Hungarian tag for Vector<T>
				documentation (printed and HTML)

	WARNING: Be careful not to use the & operator on elements of type T. Otherwise vectors of
	smart pointers won't work. Use AddrOf() instead.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef VECTOR_H_INCLUDED
#define VECTOR_H_INCLUDED


/***********************************************************************************************
	Element destruction.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Invokes the destructor on a range.
----------------------------------------------------------------------------------------------*/
template<typename T>
	inline void DestroyRange(T * prge, int ce)
{
	AssertArray(prge, ce);

	T * pe = prge;
	T * peLim = prge + ce;

	for ( ; pe < peLim; ++pe)
		pe->~T();
}


inline void DestroyRange(char * p, int ce)
{
}
inline void DestroyRange(short * p, int ce)
{
}
inline void DestroyRange(int * p, int ce)
{
}
inline void DestroyRange(long * p, int ce)
{
}
inline void DestroyRange(unsigned char * p, int ce)
{
}
inline void DestroyRange(unsigned short * p, int ce)
{
}
inline void DestroyRange(unsigned int * p, int ce)
{
}
inline void DestroyRange(unsigned long * p, int ce)
{
}
inline void DestroyRange(float * p, int ce)
{
}
inline void DestroyRange(int64 * p, int ce)
{
}
inline void DestroyRange(double * p, int ce)
{
}
inline void DestroyRange(bool * p, int ce)
{
}


/***********************************************************************************************
	Vector class.
********************************************************************************//*:End Ignore*/

/*----------------------------------------------------------------------------------------------
	vector (one-dimensional array) template collection class

	This class checks explicitly for memory allocation actually succeeding, and its methods
	throw an exception (E_OUTOFMEMORY) when memory allocation fails.  It uses realloc() for
	memory allocation and memmove() (disguised as MoveItems()) to shuffle the contents of
	allocated memory around.

	When insertion requires a reallocation to make room for new elements,
	the reallocation proceeds as follows:

@line	1. set ceNew to the new number of elements in the vector
@line	2. if ceNew is less than or equal to m_ieMax, then we have enough space
@line	3. otherwise, do the following:
@line	3A. if m_ceGrowthFactor is zero, set it to -kceDefaultInitialGrowthFactor
@line	3B. if m_ceGrowthFactor is less than zero, set ceAllocNew to
			m_ieMax + -m_ceGrowthFactor
@line	3C. otherwise, set ceAllocNew to m_ieMax + m_ceGrowthFactor
@line	3D. if ceAllocNew is less than ceNew,
			set ceAllocNew to ceNew + kceDefaultInitialGrowthFactor
@line	3E. allocate ceAllocNew elements for the vector, and do all the necessary
			bookkeeping
@line	3F. if m_ceGrowthFactor is less than zero, double it for the next time

	Hungarian prefix: v
----------------------------------------------------------------------------------------------*/
template<class T> class Vector
{
public:
	/*-------------------------------------------------------------------------------*//*:Ignore
		Constructors and destructor.
	------------------------------------------------------------------------------------------*/
	Vector(void);
	Vector(const Vector<T> & vec);
	~Vector(void);
	//:End Ignore

	/*------------------------------------------------------------------------------------------
		The assignment operator allows an entire vector to be assigned as the value of another
		vector.

		@return a reference to this vector.  (That is how the assignment operator is defined!)

		@param vec is a reference to the other vector.
	------------------------------------------------------------------------------------------*/
	Vector<T> & operator = (const Vector<T> & vec)
	{
		AssertObj(this);
		AssertObj(&vec);
		if (&vec != this)
			Replace(0, m_ieLim, vec.m_prge, vec.m_ieLim);
		return *this;
	}

	//:> Member variable access

	/*------------------------------------------------------------------------------------------
		GetGrowthFactor returns the number of elements by which the stored vector will grow the
		next time more memory is needed.  If the returned value is zero, then a fixed default
		value (currently 4) is used, and then stored as a negative number.  If the returned
		value is negative, then the absolute value is actually used, and the value is doubled
		each time memory is allocated.  If the returned value is greater than zero, then that
		same number is used repeatedly.
	------------------------------------------------------------------------------------------*/
	int GetGrowthFactor() const
	{
		AssertObj(this);
		return m_ceGrowthFactor;
	}

	/*------------------------------------------------------------------------------------------
		SetGrowthFactor sets the number of elements by which the stored vector will grow the
		next time more memory is needed.

		@param ceGrowthFactor determines the number of elements by which the stored vector will
		grow the next time more memory is needed.  If this value is zero, then a fixed default
		value (currently 4) will be used the next time memory is allocated, and then stored as
		a negative number.  If ceGrowthFactor is negative, then its absolute value will be used,
		and the magnitude of the value doubled each time memory is allocated.  If ceGrowthFactor
		is greater than zero, then that same number will be used repeatedly.
	------------------------------------------------------------------------------------------*/
	void SetGrowthFactor(int ceGrowthFactor)
	{
		AssertObj(this);
		m_ceGrowthFactor = ceGrowthFactor;
	}

	//:> Other public methods

	/*------------------------------------------------------------------------------------------
		Begin returns an iterator pointing to the first element of the stored vector.  If the
		vector is empty, this method returns the same value as End.
	------------------------------------------------------------------------------------------*/
	T * Begin()
	{
		AssertObj(this);
		return m_prge;
	}

	/*------------------------------------------------------------------------------------------
		End returns an iterator pointing to the first address past the last element of the
		stored vector.  This is useful for terminating loops that use iterators.  If the stored
		vector is empty, this method returns the same value as Begin.
	------------------------------------------------------------------------------------------*/
	T * End()
	{
		AssertObj(this);
		return m_prge + m_ieLim;
	}

	/*------------------------------------------------------------------------------------------
		Top returns an iterator pointing to the last element of the stored vector.  This may be
		useful when the vector is being used as a stack.  If the vector is empty, Top returns
		NULL.
	------------------------------------------------------------------------------------------*/
	T * Top()
	{
		AssertObj(this);
		return m_ieLim > 0 ? m_prge + m_ieLim - 1 : NULL;
	}

	/*------------------------------------------------------------------------------------------
		Bottom returns an iterator pointing to the first element of the stored vector.  This is
		basically the same as Begin(), but is better to understand when used together with
		Top().
	------------------------------------------------------------------------------------------*/
	T * Bottom()
	{
		return Begin();
	}

	/*------------------------------------------------------------------------------------------
		Size returns the number of elements stored in the vector, which may be less than the
		number of elements that could be stored in the currently allocated block of memory.
	------------------------------------------------------------------------------------------*/
	int Size() const
	{
		AssertObj(this);
		return m_ieLim;
	}

	/*------------------------------------------------------------------------------------------
		ExcessSpace returns the number of elements that can be added to the stored vector
		without causing any additional memory to be allocated.  It is always greater than or
		equal to zero.
	------------------------------------------------------------------------------------------*/
	int ExcessSpace() const
	{
		AssertObj(this);
		return m_ieMax - m_ieLim;
	}

	/*------------------------------------------------------------------------------------------
		The subscript operator provides array style direct access to elements stored in the
		vector.  This version effectively allows both read and write access.

		@return a reference to the vector element at the given index

		@param ie is a zero-based index into the stored vector.  It must be in range.
	------------------------------------------------------------------------------------------*/
	T & operator[](int ie)
	{
		AssertObj(this);
		Assert((uint)ie < (uint)m_ieLim);
		if ((uint)ie >= (uint)m_ieLim)
			throw std::invalid_argument("ie");
		return m_prge[ie];
	}

	/*------------------------------------------------------------------------------------------
		The subscript operator provides array style direct access to elements stored in the
		vector.  This version effectively only read access to a read only ("const") vector.

		@return a reference to the vector element at the given index

		@param ie is a zero-based index into the stored vector.  It must be in range.
	------------------------------------------------------------------------------------------*/
	const T & operator[](int ie) const
	{
		AssertObj(this);
		Assert((uint)ie < (uint)m_ieLim);
		if ((uint)ie >= (uint)m_ieLim)
			throw std::invalid_argument("ie");
		return m_prge[ie];
	}

	/*------------------------------------------------------------------------------------------
		Push adds an element to the end of the stored vector.  vx.Push(x) is equivalent to
		vx.Insert(vx.Size(), x).  Push facilitates using the vector as a stack.

		@param eIns is a reference to the element to store in the vector.
	------------------------------------------------------------------------------------------*/
	void Push(const T & eIns)
	{
		AssertObj(this);
		Insert(m_ieLim, 1, eIns);
	}

	//:Ignore

	bool Pop(T * peRet = NULL);

	void Resize(int ce, const T & eIns);
	void Resize(int ce);

	void Insert(int ieIns, int ceIns, const T & eIns);
	void InsertMulti(int ie, int ce, const T * prge);

	void Delete(int ieMin, int ieLim, T * prge = NULL);
	void Clear();

	void Replace(int ieMin, int ieLimDel, const T * prgeIns, int ceIns);
	void CopyTo(Vector<T> & vec);
	void RawSlide(int ieMin, int ieLim, int die);

#ifdef DEBUG
	bool AssertValid() const
	{
		Assert(0 <= m_ieLim && m_ieLim <= m_ieMax);
		Assert((m_prge == NULL) == (m_ieMax == 0));
		AssertArray(m_prge, m_ieMax);
		return true;
	}
#endif // DEBUG

	//:End Ignore

	/*------------------------------------------------------------------------------------------
		EnsureSpace adjusts the amount of memory allocated for the stored vector to provide
		space for (at least) the desired number of unused elements.  If it succeeds, then
		ExcessSpace would return a value at least as large as ce until the next element is
		added to the stored vector.

		@param ce is the number of elements for which space must be allocated but not yet used.
		@param fFitExactly is an optional argument that indicates whether exactly ce excess
				elements are to be allocated.  This is useful for freeing memory that is no
				longer needed once a vector reaches its final size.
	------------------------------------------------------------------------------------------*/
	void EnsureSpace(int ce, bool fFitExactly = false)
	{
		AssertObj(this);
		Assert(ce >= 0);
		// Checks for overflow.
		Assert(ce + m_ieLim >= ce);

		int ieLim = m_ieLim + ce;

		if (ieLim > m_ieMax || fFitExactly && ieLim != m_ieMax)
			_Realloc(ieLim, fFitExactly);
	}

	/*------------------------------------------------------------------------------------------
		Insert adds one item to the stored vector, shifting any elements at the given location
		or following to make room for it.  This potentially invalidates existing iterators for
		this vector.

		@param ieIns is a zero-based index into the vector, indicating where to add the element.
						ieIns must be in range
		@param eIns is a reference to the element to store in the vector.
	------------------------------------------------------------------------------------------*/
	void Insert(int ieIns, const T & eIns)
	{
		Insert(ieIns, 1, eIns);
	}

	/*------------------------------------------------------------------------------------------
		Delete removes one element from the stored vector, shifting the following elements in
		the stored vector to maintain compact storage.  It potentially invalidates existing
		iterators for this vector.

		@param ie is a zero-based index into the vector, indicating which element to remove.
						ie must be in range.
		@param pe is an optional pointer to a place for storing a copy of the deleted element.
	------------------------------------------------------------------------------------------*/
	void Delete(int ie, T * pe = NULL)
	{
		Delete(ie, ie + 1, pe);
	}

protected:
	enum { kceDefaultInitialGrowthFactor = 4 };

	// Pointer to the allocated vector of elements.
	T * m_prge;
	// The number of elements in the vector.
	int m_ieLim;
	// The number of elements for which space has been allocated.
	int m_ieMax;
	// The number of elements to add the vector when growing.  If this number is negative, the
	// absolute value is used, and it is doubled after each use.
	int m_ceGrowthFactor;

	//:Ignore

	void _Realloc(int ieLim, bool fFitExactly);

	//:End Ignore
};

#endif // !VECTOR_H_INCLUDED
