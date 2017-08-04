/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2007-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: OleStringLiteral.cpp
Responsibility: Neil Mayhew
Last reviewed:

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "OleStringLiteral.h"
#include "UnicodeConverter.h"
#include <cwchar>
#include <algorithm>

const OleStringLiteral::uchar_t* OleStringLiteral::convert(const wchar_t* w)
{
#if WIN32
	return w;
#else
	uchar_t* u = 0;
	size_t n = UnicodeConverter::Convert(w, -1, u, 0) + 1;
	u = new uchar_t[n];
	UnicodeConverter::Convert(w, -1, u, n);
	return u;
#endif
};

const OleStringLiteral::uchar_t OleStringLiteral::empty[1] = { 0 };
