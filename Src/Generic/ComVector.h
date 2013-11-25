/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: ComVector.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This provides a template collection class to replace std::vector for storing COM interface
	pointers.  Its reason to exist is to allow explicit checking for memory allocation failures,
	and to avoid using methods that could result in throwing an exception.  It also automates
	calling AddRef and Release for most uses.
REVIEW SteveMc: design using single base class vs. using explicit instantiation
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef COMVECTOR_H_INCLUDED
#define COMVECTOR_H_INCLUDED

/*----------------------------------------------------------------------------------------------
	This is a base class used to derive ComVector<T>.  It is only an implementation
	optimization to minimize code bloat.

	This class checks explicitly for memory allocation actually succeeding, and its methods
	throw exceptions when memory allocation might occur.  It uses malloc() and realloc() for
	memory allocation, memmove() to shuffle the contents of allocated memory around, and
	memset() to clear unused portions of allocated memory.

	When insertion requires a reallocation to make room for new elements,
	the reallocation proceeds as follows:

		1. set ceNew to the new number of elements in the vector
		2. if ceNew is less than or equal to m_ceAlloc, then we have enough space
		3. otherwise, do the following:
		3A. if m_ceGrowthFactor is zero, set it to -kceDefaultInitialGrowthFactor
		3B. if m_ceGrowthFactor is less than zero, set ceAllocNew to
			m_ceAlloc + -m_ceGrowthFactor
		3C. otherwise, set ceAllocNew to m_ceAlloc + m_ceGrowthFactor
		3D. if ceAllocNew is less than ceNew,
			set ceAllocNew to ceNew + kceDefaultInitialGrowthFactor
		3E. allocate ceAllocNew elements for the vector, and do all the necessary
			bookkeeping
		3F. if m_ceGrowthFactor is less than zero, double it for the next time

	Hungarian: NONE (this class should never be used directly)
----------------------------------------------------------------------------------------------*/
class BaseOfComVector
{
protected:
	enum { kceDefaultInitialGrowthFactor = 4 };

	// Member variables

	IUnknown ** m_peFirst;
	int m_ce;
	int m_ceAlloc;
	int m_ceGrowthFactor;

	// TODO-Linux: this is neccessary because this type is sometimes scoped as a static/global object.
	bool m_fFreeOnExit;

	// Constructors/destructors/etc.

	BaseOfComVector();
	~BaseOfComVector();

	// Other protected methods

	bool Pop(IUnknown ** ppunkRet = NULL);
	void EnsureSpace(int ce, bool fFitExactly = false);
	void Resize(int ce, IUnknown * punk = NULL);
	void Insert(int ie, IUnknown * punk);
	void Delete(int ie);
	void Clear();
	void Replace(int ieMin, int ieLim, IUnknown ** prgpunkIns, int ceIns);
	void CopyTo(BaseOfComVector * pvec);

	void _DestroyRange(IUnknown ** peMin, IUnknown ** peLim);
	void _InsertMultiple(IUnknown ** pe, int ceIns, IUnknown * punkIns);
	void _Erase(IUnknown ** peMin, IUnknown ** peLim);
#ifdef DEBUG
	bool AssertValid()
	{
		AssertPtrN(m_peFirst);
		Assert((0 <= m_ce) && (m_ce <= m_ceAlloc));
		Assert((m_peFirst != NULL) || (m_ceAlloc == 0));
		Assert((m_peFirst == NULL) || (m_ceAlloc > 0));
		AssertArray(m_peFirst, m_ceAlloc);
		return true;
	}
#endif

private:
	// copying a BaseOfComVector with the copy constructor is *BAD*!!
	BaseOfComVector(BaseOfComVector & vec)
	{
		Assert(false);
	}
	// copying a BaseOfComVector with the = operator is *BAD*!!
	BaseOfComVector & operator = (BaseOfComVector & vec)
	{
		Assert(false);
		return *this;
	}
};
//:End Ignore

/*----------------------------------------------------------------------------------------------
	vector (one-dimensional array) collection class customized for storing COM interface
	pointers.

	This class checks explicitly for memory allocation actually succeeding, and its methods
	throw an exception (E_OUTOFMEMORY) when memory allocation might occur.  It uses malloc()
	and realloc() for memory allocation, and memmove() to shuffle the contents of allocated
	memory around.

	When insertion requires a reallocation to make room for new elements,
	the reallocation proceeds as follows:

@line	1. set ceNew to the new number of elements in the vector
@line	2. if ceNew is less than or equal to m_ceAlloc, then we have enough space
@line	3. otherwise, do the following:
@line	3A. if m_ceGrowthFactor is zero, set it to -kceDefaultInitialGrowthFactor
@line	3B. if m_ceGrowthFactor is less than zero, set ceAllocNew to
			m_ceAlloc + -m_ceGrowthFactor
@line	3C. otherwise, set ceAllocNew to m_ceAlloc + m_ceGrowthFactor
@line	3D. if ceAllocNew is less than ceNew,
			set ceAllocNew to ceNew + kceDefaultInitialGrowthFactor
@line	3E. allocate ceAllocNew elements for the vector, and do all the necessary
			bookkeeping
@line	3F. if m_ceGrowthFactor is less than zero, double it for the next time

	Hungarian: vq[Foo]
----------------------------------------------------------------------------------------------*/
template<class IFoo> class ComVector : public BaseOfComVector
{
public:
	//:Ignore

	//:> Helpful typedefs

	typedef ComSmartPtr<IFoo> SmartPtr;
	typedef SmartPtr * iterator;

	ComVector()
	{
	}
	~ComVector()
	{
	}
	//:End Ignore

	//:> Member variable access

	/*------------------------------------------------------------------------------------------
		GetGrowthFactor returns the number of elements by which the stored vector will grow the
		next time more memory is needed.  If the returned value is zero, then a fixed default
		value (currently 4) is used, and then stored as a negative number.  If the returned
		value is negative, then the absolute value is actually used, and the value is doubled
		each time memory is allocated.  If the returned value is greater than zero, then that
		same number is used repeatedly.
	------------------------------------------------------------------------------------------*/
	int GetGrowthFactor()
	{
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
		m_ceGrowthFactor = ceGrowthFactor;
	}

	//:> Other public methods

	/*------------------------------------------------------------------------------------------
		Begin returns an iterator pointing to the first element of the stored vector.  If the
		vector is empty, this method returns the same value as End.
	------------------------------------------------------------------------------------------*/
	iterator Begin()
	{
		AssertObj(this);
		return (iterator)m_peFirst;
	}

	/*------------------------------------------------------------------------------------------
		End returns an iterator pointing to the first address past the last element of the
		stored vector.  This is useful for terminating loops that use iterators.  If the stored
		vector is empty, this method returns the same value as Begin.
	------------------------------------------------------------------------------------------*/
	iterator End()
	{
		AssertObj(this);
		return (iterator)(m_peFirst + m_ce);
	}

	/*------------------------------------------------------------------------------------------
		Top returns an iterator pointing to the last element of the stored vector.  This may be
		useful when the vector is being used as a stack.  If the vector is empty, Top returns
		NULL.
	------------------------------------------------------------------------------------------*/
	iterator Top()
	{
		AssertObj(this);
		return (iterator)(m_ce > 0 ? m_peFirst + m_ce - 1 : NULL);
	}

	/*------------------------------------------------------------------------------------------
		Size returns the number of elements stored in the vector, which may be less than the
		number of elements that could be stored in the currently allocated block of memory.
	------------------------------------------------------------------------------------------*/
	int Size()
	{
		AssertObj(this);
		return m_ce;
	}

	/*------------------------------------------------------------------------------------------
		ExcessSpace returns the number of elements that can be added to the stored vector
		without causing any additional memory to be allocated.  It is always greater than or
		equal to zero.
	------------------------------------------------------------------------------------------*/
	int ExcessSpace()
	{
		AssertObj(this);
		return m_ceAlloc - m_ce;
	}

	/*------------------------------------------------------------------------------------------
		The subscript operator provides array style direct access to elements stored in the
		vector.  This version effectively allows both read and write access.

		Using this method to set an entry in a ComVector handles the reference counting
		automatically.  However, using this to copy an entry from the CodeVector to a pointer
		variable does not affect the reference count: the programmer must handle that detail
		explicitly.

		@return a reference to the vector element at the given index

		@param ie is a zero-based index into the stored vector.  It must be in range.
	------------------------------------------------------------------------------------------*/
	SmartPtr & operator[](int ie)
	{
		AssertObj(this);
		Assert((uint)ie < (uint)m_ce);
		return (SmartPtr &)*(m_peFirst + ie);
	}

	/*------------------------------------------------------------------------------------------
		Delete removes one element from the stored vector, shifting the following elements in
		the stored vector to maintain compact storage.  It potentially invalidates existing
		iterators for this ComVector.

		If the deleted COM interface pointer is not NULL, then its Release method is called
		before overwriting (or clearing) its location in a ComVector.

		@param ie is a zero-based index into the vector, indicating which element to remove.
						ie must be in range.
	------------------------------------------------------------------------------------------*/
	void Delete(int ie)
	{
		BaseOfComVector::Delete(ie);
	}

	/*------------------------------------------------------------------------------------------
		Delete removes zero or more elements from the stored vector, shifting the following
		elements in the stored vector to maintain compact storage.  It potentially invalidates
		existing iterators for this ComVector.

		If a deleted COM interface pointer is not NULL, then its Release method is called
		before overwriting (or clearing) its location in a ComVector.

		@param ieMin is a zero-based index into the stored vector, indicating the first element
				in a range of elements to delete.  ieMin must be in range.
		@param ieLimDel is a zero-based index into the stored vector, indicating the element
				that replaces the first one deleted (ieMin).  In other words, ieLimDel is one
				past the last element deleted: exactly ieLimDel - ieMin elements are deleted.
				ieLimDel must be in range.  If ieLimDel is less than or equal to ieMin, then
				the vector is left unchanged.
	------------------------------------------------------------------------------------------*/
	void Delete(int ieMin, int ieLimDel)
	{
		Replace(ieMin, ieLimDel, NULL, 0);
	}

	/*------------------------------------------------------------------------------------------
		Clear frees all the memory used by the stored vector.  When it is done, only the minimum
		amount of bookkeeping memory is still taking up space, and any internal pointers have
		all been set to NULL.

		If the growth factor value is negative, it is reset to zero.  If it is positive, then
		it is left unchanged.

		Release is called for every non-NULL COM interface pointer stored in the ComVector
		before the vector memory is freed.
	------------------------------------------------------------------------------------------*/
	void Clear()
	{
		BaseOfComVector::Clear();
	}

	/*------------------------------------------------------------------------------------------
		Push adds an element to the end of the stored vector.  vqx.Push(px) is equivalent to
		vqx.Insert(vqx.Size(), px).  Push facilitates using the vector as a stack.

		@param pfoo is a the COM pointer to store in the vector.
	------------------------------------------------------------------------------------------*/
	void Push(IFoo * pfoo)
	{
		Insert(Size(), pfoo);
	}

	/*------------------------------------------------------------------------------------------
		Pop erases the last element stored in the vector.

		If the deleted COM interface pointer is not NULL, then its Release method is called
		before overwriting (or clearing) its location in a ComVector if ppfooRet is NULL.

		@param ppfooRet is an optional pointer to a COM interface pointer that receives a copy
				of the popped element.  If it is NULL, then the popped element is discarded
				without any copy being made.

		@return true if an element was removed from the vector, false if the vector was already
				empty.
	------------------------------------------------------------------------------------------*/
	bool Pop(IFoo ** ppfooRet = NULL)
	{
		return BaseOfComVector::Pop((IUnknown **)ppfooRet);
	}

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
		BaseOfComVector::EnsureSpace(ce, fFitExactly);
	}

	/*------------------------------------------------------------------------------------------
		Resize ensures that the vector has space allocated for at least the given number of
		elements, and that exactly that many elements are initialized.  This may or may not
		result in memory being allocated, and may result in pointers being initialized (set to
		NULL), or deinitialized (set to NULL after calling Release).

		Increasing the size of the vector causes the new elements to be initialized with the
		the provided value.  If pfoo is not NULL, then its AddRef method is called the proper
		number of times.  Decreasing the size of the vector causes Release to be called for each
		non-NULL pointer removed from the ComVector.

		If Resize returns without error, then Size will return ce until the next COM interface
		pointer is added to or removed from the ComVector.

		@param ce is the number of elements desired to be in the vector.
		@param pfoo is an option COM interface pointer to store in the vector.
	------------------------------------------------------------------------------------------*/
	void Resize(int ce, IFoo * pfoo = NULL)
	{
		BaseOfComVector::Resize(ce, (IUnknown *)pfoo);
	}

	/*------------------------------------------------------------------------------------------
		Insert adds one item to the stored vector, shifting any elements at the given location
		or following to make room for it.  This potentially invalidates existing iterators for
		this ComVector.

		@param ie is a zero-based index into the vector, indicating where to add the element.
					ie must be in range
		@param pfoo is a COM pointer to store in the vector.  If pfoo is not NULL, then its
					AddRef method is called.
	------------------------------------------------------------------------------------------*/
	void Insert(int ie, IFoo * pfoo)
	{
		BaseOfComVector::Insert(ie, (IUnknown *)pfoo);
	}

	/*------------------------------------------------------------------------------------------
		Replace allows a number of elements to be replaced in a single operation.  This
		function calls the appropriate AddRef and Release methods as needed.

		@param ieMin is the zero-based index into the stored vector for the first element to be
			replaced.  ieMin must be in range.
		@param ieLimDel is the zero-based index into the stored vector just past the last
			element to be replaced.  ieLimDel must be in range, and should be greater than or
			equal to ieMin.
		@param prgpfooIns points to an optional array of elements to use as the replacement
			values.  If it is NULL, then each COM interface pointer is replaced with NULL.  If
			prgpfooIns is not NULL, then it must have ieLimDel - ieMin elements.
	------------------------------------------------------------------------------------------*/
	void Replace(int ieMin, int ieLimDel, IFoo ** prgpfooIns)
	{
		Replace(ieMin, ieLimDel, prgpfooIns, ieLimDel - ieMin);
	}

	/*------------------------------------------------------------------------------------------
		Replace allows a number of elements to be inserted, deleted, or replaced in a single
		operation.  This function calls the appropriate AddRef and Release methods as needed.

		@param ieMin is the zero-based index into the stored vector for the first element to be
			replaced.  ieMin must be in range.
		@param ieLimDel is the zero-based index into the stored vector just past the last
			element to be replaced.  ieLimDel must be in range.
		@param prgpfooIns points to an optional array of elements to use as the replacement
			values.  If it is NULL, then NULL is used.  If prgpfooIns is not NULL, then it must
			have ceIns elements.
		@param ceIns is the count of how many elements replace those between ieMin and ieLimDel.
			It must be greater than or equal to zero.  If prgpfooIns is NULL and ceIns is not
			zero, then NULL is used for each new element stored into the ComVector.
	------------------------------------------------------------------------------------------*/
	void Replace(int ieMin, int ieLimDel, IFoo ** prgpfooIns, int ceIns)
	{
		BaseOfComVector::Replace(ieMin, ieLimDel, (IUnknown **)prgpfooIns, ceIns);
	}

	/*------------------------------------------------------------------------------------------
		CopyTo copies the content of this ComVector to another ComVector, replacing any
		previous content of the other ComVector.

		@param vec is a reference to the other vector.
	------------------------------------------------------------------------------------------*/
	void CopyTo(ComVector<IFoo> & vec)
	{
		BaseOfComVector::CopyTo(static_cast<BaseOfComVector *>(&vec));
	}

	/*------------------------------------------------------------------------------------------
		CopyTo copies the content of this ComVector to another ComVector, replacing any
		previous content of the other ComVector.

		@param pvec is a pointer to the other vector.
	------------------------------------------------------------------------------------------*/
	void CopyTo(ComVector<IFoo> * pvec)
	{
		BaseOfComVector::CopyTo(static_cast<BaseOfComVector *>(pvec));
	}

private:
	//:Ignore

	// copying a ComVector with the copy constructor is *BAD*!!
	ComVector(ComVector<IFoo> & vec)
	{
		Assert(false);
	}

	// copying a ComVector with the = operator is *BAD*!!
	ComVector<IFoo> & operator = (ComVector<IFoo> & vec)
	{
		Assert(false);
		return *this;
	}

	//:End Ignore
};

// StaticComVector is that same as ComVector except it doesn't free its clear its list on deletion
template<class IFoo> class StaticComVector : public ComVector<IFoo>
{
	public:

	StaticComVector()
	{
		ComVector<IFoo>::m_fFreeOnExit = false;
	}
};


// Local Variables:
// mode:C++
// c-file-style:"cellar"
// tab-width:4
// End:

#endif /*COMVECTOR_H_INCLUDED*/
