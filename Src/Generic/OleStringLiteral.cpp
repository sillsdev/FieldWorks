/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2007 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

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
