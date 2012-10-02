/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LinkedList.h
Responsibility:
Last reviewed:

Description:
	Implements a linked list node template and a linked list base class template.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef LINKEDLIST_H
#define LINKEDLIST_H


/*************************************************************************************
	This contains a pointer to the data node.
*************************************************************************************/
template <typename T> class LLNode
{
private:
	LLNode<T> * m_pllnNext;
	LLNode<T> ** m_ppllnPrev;
	T * m_pt;

public:
	LLNode(LLNode ** ppllnPrev, T * pt)
	{
		AssertPtrN(pt);
		AssertPtr(ppllnPrev);
		AssertPtrN(*ppllnPrev);

		// Store the object.
		m_pt = pt;
		AddRefObj(m_pt);

		// Link this node.
		m_pllnNext = *ppllnPrev;
		if (m_pllnNext)
		{
			Assert(m_pllnNext->m_ppllnPrev == ppllnPrev);
			m_pllnNext->m_ppllnPrev = &m_pllnNext;
		}
		m_ppllnPrev = ppllnPrev;
		*ppllnPrev = this;
	}

	virtual ~LLNode(void)
	{
		AssertPtr(m_ppllnPrev);
		AssertPtrN(m_pllnNext);
		Assert(*m_ppllnPrev == this);

		// Unlink this node.
		if (m_pllnNext)
		{
			Assert(m_pllnNext->m_ppllnPrev == &m_pllnNext);
			m_pllnNext->m_ppllnPrev = m_ppllnPrev;
		}
		*m_ppllnPrev = m_pllnNext;
		m_ppllnPrev = NULL;
		m_pllnNext = NULL;

		// Release the object.
		ReleaseObj(m_pt);
	}

	T * Pobj(void)
	{
		return m_pt;
	}

	LLNode<T> * PllnNext(void)
	{
		return m_pllnNext;
	}
};


/*************************************************************************************
	This class manages a linked list. You link an item to the list by passing,
	either to the Link method or to the constructor, a pointer to the pointer that
	should be made to point to this node. That is, it will be a pointer to a pointer
	to an LLBase object.

	Typically, some object owns the list, and it has an variable of type LLBase<T> *,
	say m_pllb. To insert at the start of the list, call pllbNew->Link(&m_pllb).

	To insert after an existing item, say, pllbOld, call
	pllbNew->Link(&(pllbOld->m_pobjNext));

	The LLBase object stores a direct pointer to the next list item, and a pointer
	to the pointer which points to it, either in the previous node or in some other
	object (if it is the head of the list).

	The object at the end of the list has a null next pointer.

	To remove an item from the list, you can simply destroy it; to remove it
	without destroying it, call Link(NULL).

	The data node should be a subclass of this class. That is, T should derive from
	LLBase<T>.
*************************************************************************************/
template <typename T> class LLBase
{
protected:
	T * m_pobjNext;
	T ** m_ppobjPrev;

	LLBase(T ** ppobjPrev)
	{
		AssertPtrN(ppobjPrev);
		m_pobjNext = NULL;
		m_ppobjPrev = NULL;
		if (ppobjPrev)
			Link(ppobjPrev);
	}

	~LLBase(void)
	{
		Link(NULL);
	}

	void Link(T ** ppobjPrev)
	{
		AssertPtrN(m_ppobjPrev);
		// If we know about a next object, we're in a list, so we should also know about
		// a previous one (or the head of the list in some other object).
		Assert(m_ppobjPrev || !m_pobjNext);

		if (ppobjPrev == m_ppobjPrev)
			return;
		if (m_ppobjPrev)
		{
			// Unlink this node. (Remove it from its old linked list.)
			AssertPtrN(m_pobjNext);
			// The 'previous' value should currently be pointing to this.
			Assert(*m_ppobjPrev == static_cast<T *>(this));
			if (m_pobjNext)
			{
				// If there are any more objects in the list, the next one should be pointing
				// back at this one...
				Assert(m_pobjNext->m_ppobjPrev == &m_pobjNext);
				// ...and should now be made to point at the one before this, since this is being
				// removed from its old list.
				m_pobjNext->m_ppobjPrev = m_ppobjPrev;
			}
			// The pointer in the previous node, or the head of list pointer, now points to what
			// used to follow this.
			*m_ppobjPrev = m_pobjNext;
			// And (for now) we aren't in any list.
			m_ppobjPrev = NULL;
			m_pobjNext = NULL;
		}
		// If we're putting it into a new list...
		if (ppobjPrev)
		{
			// Link this node.
			AssertPtr(ppobjPrev); // Make sure we're linking to something valid
			// And, if the thing it is going to follow already has something after it,
			// that something should be valid.
			AssertPtrN(*ppobjPrev);
			// The thing following us is going to be the thing (if any) that used to
			// follow our (new) predecessor.
			m_pobjNext = *ppobjPrev;
			if (m_pobjNext)
			{
				// The thing following should at this point still be correctly linked to its old
				// predecessor.
				Assert(m_pobjNext->m_ppobjPrev == ppobjPrev);
				// Change it to make us the predecessor.
				m_pobjNext->m_ppobjPrev = &m_pobjNext;
			}
			// Make our own predecessor the argument we were passed.
			m_ppobjPrev = ppobjPrev;
			// And make it point to us.
			*ppobjPrev = static_cast<T *>(this);
		}
	}
};

#endif //!LINKEDLIST_H
