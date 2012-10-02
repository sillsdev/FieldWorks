/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilString.cpp
Responsibility: LarryW
Last reviewed: 27Sep99

	Code for string utilities.
----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#include <crtdbg.h>
#endif
#ifndef UTILSTRING_I_CPP
#define UTILSTRING_I_CPP

#include "UtilString.h"

#pragma hdrstop
//#undef THIS_FILE
//DEFINE_THIS_FILE

namespace gr
{

/*----------------------------------------------------------------------------------------------
	Create a new internal buffer of size cch and return a pointer to the characters.
	This preserves the characters currently in the string, up the the min of the old and
	new sizes. It is expected that the caller will fill in any newly allocated characters.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::SetSize(int cchNew, XChar ** pprgch)
{
	AssertObj(this);
	AssertPtr(pprgch);

	if (!cchNew)
	{
		_SetBuf(&s_bufEmpty);
		*pprgch = NULL;
		return;
	}

	int cchCur = m_pbuf->Cch();

	if (cchNew != cchCur || m_pbuf->m_crefMinusOne)
	{
		StrBuffer * pbuf = StrBuffer::Create(cchNew);
		if (cchCur > 0)
			std::copy(m_pbuf->m_rgch, m_pbuf->m_rgch + (cchCur < cchNew ? cchCur : cchNew),  pbuf->m_rgch);
//			CopyItems(m_pbuf->m_rgch, pbuf->m_rgch, cchCur < cchNew ? cchCur : cchNew);
		_AttachBuf(pbuf);
	}

	*pprgch = m_pbuf->m_rgch;

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Set the character at index ich to ch.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::SetAt(int ich, XChar ch)
{
	AssertObj(this);
	Assert(ich >= 0);
	Assert((unsigned int)ich < (unsigned int)m_pbuf->Cch());

	// If ch is the same as the character already at ich, return.
	if (ch == m_pbuf->m_rgch[ich])
		return;

	// If this string object is sharing a buffer, then you must not change the other string
	// sharing the buffer.  Rather, copy it.
	if (m_pbuf->m_crefMinusOne > 0)
		_Copy();

	AssertObj(m_pbuf);
	Assert(m_pbuf->m_crefMinusOne == 0);
	m_pbuf->m_rgch[ich] = ch;

	AssertObj(this);
}

#ifdef GR_FW
/*----------------------------------------------------------------------------------------------
	Convert characters to lower case. If this string object is sharing a buffer, then the
	existing characters are copied into a new buffer solely owned by this StrBase<>.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::ToLower(void)
{
	AssertObj(this);

	if (!m_pbuf->Cch())
		return;

	// If this string object is sharing a buffer, then you must not change the other string
	// sharing the buffer. Rather, copy it.
	if (m_pbuf->m_crefMinusOne > 0)
		_Copy();

	AssertObj(m_pbuf);
	Assert(m_pbuf->m_crefMinusOne == 0);
	gr::ToLower(m_pbuf->m_rgch, m_pbuf->Cch());

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Convert characters to upper case. If this string object is sharing a buffer, then the
	existing characters are copied into a new buffer solely owned by this StrBase<>.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::ToUpper(void)
{
	AssertObj(this);

	if (!m_pbuf->Cch())
		return;

	// If this string object is sharing a buffer, then you must not change the other string
	// sharing the buffer. Rather, copy it.
	if (m_pbuf->m_crefMinusOne > 0)
		_Copy();

	AssertObj(m_pbuf);
	Assert(m_pbuf->m_crefMinusOne == 0);
	::ToUpper(m_pbuf->m_rgch, m_pbuf->Cch());

	AssertObj(this);
}
#endif

/*----------------------------------------------------------------------------------------------
	Copy the existing characters into a new buffer solely owned by this StrBase<>. This is
	so we can modify characters in place.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::_Copy(void)
{
	AssertObj(this);
	Assert(m_pbuf->m_crefMinusOne > 0);
	Assert(m_pbuf->Cch() > 0);

	// Allocate the new buffer.
	int cch = m_pbuf->Cch();
	StrBuffer * pbuf = StrBuffer::Create(cch);

	std::copy(m_pbuf->m_rgch, m_pbuf->m_rgch + cch, pbuf->m_rgch);
//	CopyItems(m_pbuf->m_rgch, pbuf->m_rgch, cch);

	// Set our buffer to the new one.
	_AttachBuf(pbuf);

	AssertObj(this);
}


/*----------------------------------------------------------------------------------------------
	Replace the range [ichMin, ichLim) with the given characters of the same type.

	WARNING: We need to take care not to free the existing buffer until the operation succeeds.
	This is in case the input uses the existing buffer.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::_Replace(int ichMin, int ichLim,
		const XChar * prgchIns, XChar chIns, int cchIns)
{
	AssertObj(this);
	Assert(cchIns >= 0);
	AssertArrayN(prgchIns, cchIns);
	Assert(!chIns || !prgchIns);

	int cchCur = m_pbuf->Cch();
	Assert((unsigned int)ichMin <= (unsigned int)ichLim && (unsigned int)ichLim <= (unsigned int)cchCur);

	if (!cchIns)
	{
		// Nothing's being inserted.
		if (ichMin == ichLim)
		{
			// Nothing's being deleted either so just return.
			return;
		}
		if (!ichMin && ichLim == cchCur)
		{
			// Everything is being deleted so clear the string.
			_SetBuf(&s_bufEmpty);
			return;
		}
	}

	StrBuffer * pbuf;
	int cchNew = cchCur + cchIns - ichLim + ichMin;

	if (cchNew == cchCur && !m_pbuf->m_crefMinusOne)
	{
		// The buffer size is staying the same and we own the characters.
		pbuf = m_pbuf;
	}
	else
	{
		// Allocate the new buffer.
		pbuf = StrBuffer::Create(cchNew);
	}

	// Copy the text.
	if (ichMin > 0 && pbuf != m_pbuf)
		std::copy(m_pbuf->m_rgch, m_pbuf->m_rgch + ichMin, pbuf->m_rgch);
//		CopyItems(m_pbuf->m_rgch, pbuf->m_rgch, ichMin);
	if (cchIns > 0)
	{
		if (prgchIns)
			std::copy(prgchIns, prgchIns + cchIns, pbuf->m_rgch + ichMin);
//			CopyItems(prgchIns, pbuf->m_rgch + ichMin, cchIns);
		else
			std::fill_n(pbuf->m_rgch + ichMin, cchIns, chIns);
	}
	if (pbuf != m_pbuf)
	{
		if (ichLim < cchCur)
			std::copy(m_pbuf->m_rgch + ichLim, m_pbuf->m_rgch + cchCur, pbuf->m_rgch + ichMin + cchIns);
//			CopyItems(m_pbuf->m_rgch + ichLim, pbuf->m_rgch + ichMin + cchIns, cchCur - ichLim);
		// Set our buffer to the new one.
		_AttachBuf(pbuf);
	}

	AssertObj(this);
}

/*----------------------------------------------------------------------------------------------
	Replace the range [ichMin, ichLim) with the given characters of the other type. Use the given
	codepage to convert between Unicode and 8-bit data.

	WARNING: We need to take care not to free the existing buffer until the operation succeeds.
	This is in case the input uses the existing buffer.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void StrBase<XChar>::_Replace(int ichMin, int ichLim,
		const YChar * prgchIns, YChar chIns, int cchIns)
{
	AssertObj(this);
	Assert(cchIns >= 0);
	AssertArray(prgchIns, cchIns);

	// These are used ony when prgchIns is NULL.
	const int kcchMaxChar = 8;
	XChar rgchChar[kcchMaxChar];
	int cchChar;

	int cchCur = m_pbuf->Cch();
	Assert((unsigned int)ichMin <= (unsigned int)ichLim && (unsigned int)ichLim <= (unsigned int)cchCur);

	// Determine the number of characters we're inserting.
	int cchDst;

	if (cchIns)
	{
		if (prgchIns)
		{
			cchDst = ConvertText(prgchIns, cchIns, (XChar *)NULL, 0);
			if (!cchDst)
				ThrowHr(WarnHr(E_FAIL));
			Assert(cchCur + cchDst > cchCur);
		}
		else
		{
			cchChar = ConvertText(&chIns, 1, rgchChar, kcchMaxChar);
			if (!cchChar)
				ThrowHr(WarnHr(E_FAIL));
			Assert((unsigned int)cchChar <= (unsigned int)kcchMaxChar);
			cchDst = cchChar * cchIns;
			Assert(cchCur + cchDst > cchCur);
		}
	}
	else
		cchDst = 0;

	// Allocate the new buffer.
	StrBuffer * pbuf;
	int cchNew = cchCur + cchDst - ichLim + ichMin;

	if (cchNew == cchCur && !m_pbuf->m_crefMinusOne)
	{
		// The buffer size is staying the same and we own the characters.
		pbuf = m_pbuf;
	}
	else
	{
		// Allocate the new buffer.
		pbuf = StrBuffer::Create(cchNew);
	}

	// Copy and convert the text.
	if (ichMin > 0 && pbuf != m_pbuf)
		std::copy(m_pbuf->m_rgch, m_pbuf->m_rgch + ichMin, pbuf->m_rgch);
//		CopyItems(m_pbuf->m_rgch, pbuf->m_rgch, ichMin);
	if (cchDst > 0)
	{
		if (prgchIns)
			ConvertText(prgchIns, cchIns, pbuf->m_rgch + ichMin, cchDst);
		else
		{
			XChar * pch = pbuf->m_rgch + ichMin;
			if (cchChar == 1)
				std::fill_n(pch, cchIns, rgchChar[0]);
//				FillChars(pch, rgchChar[0], cchIns);
			else
			{
				int cchT;
				for (cchT = cchIns; --cchT >= 0; )
				{
					std::copy(rgchChar, rgchChar + cchChar, pch);
//					CopyItems(rgchChar, pch, cchChar);
					pch += cchChar;
				}
			}
		}
	}
	if (pbuf != m_pbuf)
	{
		if (ichLim < cchCur)
			std::copy(m_pbuf->m_rgch + ichLim, m_pbuf->m_rgch + cchCur, pbuf->m_rgch + ichMin + cchDst);
//			CopyItems(m_pbuf->m_rgch + ichLim, pbuf->m_rgch + ichMin + cchDst, cchCur - ichLim);
		// Set our buffer to the new one.
		_AttachBuf(pbuf);
	}

	AssertObj(this);
}

} //namespace gr

#if !defined(GR_NAMESPACE)
using namespace gr;
#endif

#endif /*UTILSTRING_I_CPP*/
