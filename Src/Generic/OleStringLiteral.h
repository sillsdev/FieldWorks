/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2007 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: OleStringLiteral.h
Responsibility: Neil Mayhew
Last reviewed:

	OleStringLiteral definition

	OleStringLiteral wraps a string literal so that it can be used as an OLE string in a
	platform-independent way.
	On Windows a wchar_t is 16 bits (UTF-16) and on Unix it is 32 bits (UTF-32).

	However, many APIs and functions that have been ported from Windows rely on using
	OLE strings, which are UTF-16. Existing code that passes a string literal as an
	OLE string argument breaks on Unix, since the character width is incompatible there.
	To gain compatibility without losing performance, OleStringLiteral converts the
	string literal to UTF-16 once and caches the result. Using an implicit conversion operator,
	it returns the converted result and so may be given in place of its string literal whenever
	an OLE string is needed.

	To achieve cross-platform code, OleStringLiteral also has a Windows implementation that
	returns the unconverted string literal. This means that the following code examples will
	work correctly on all platforms.

	Usage:

		From Windows-only:
			static const wchar_t * g_szBlah = L"foo";
		To Platform-independent:
			static const OleStringLiteral g_szBlah = L"foo";

		From Windows-only:
			baz(_T("blah"));
			foo(L"blah");
		To Platform-independent:
			static const OleStringLiteral blah = L"blah";
			baz(blah);
			foo(blah);

		Use a static variable rather than just foo(OleStringLiteral(L"blah")), for
		performance reasons.

-------------------------------------------------------------------------------*//*:End Ignore*/

#pragma once
#ifndef _OLESTRINGLITERAL_H_
#define _OLESTRINGLITERAL_H_

class OleStringLiteral
{
public:
#if WIN32
	typedef wchar_t uchar_t;
#else
	typedef unsigned short uchar_t;
#endif

	OleStringLiteral(const wchar_t* s)
		: m_original(s), m_copy(0)
	{
	}

	const wchar_t* original() const
	{
		return m_original;
	}
	const uchar_t* copy() const
	{
		if (!m_copy)
			m_copy = convert(m_original);
		return m_copy;
	}

	operator const wchar_t* () const
	{
		return original();
	}
#if !WIN32
	operator const uchar_t* () const
	{
		return copy();
	}
#endif
	// Some clients may want a non-const pointer (although they shouldn't)
	operator uchar_t* () const
	{
		return const_cast<uchar_t*>(copy());
	}

	static const uchar_t empty[1];
private:
			const wchar_t* m_original;
	mutable const uchar_t* m_copy;

	static const uchar_t* convert(const wchar_t*);
};

#endif //_OLESTRINGLITERAL_H_
