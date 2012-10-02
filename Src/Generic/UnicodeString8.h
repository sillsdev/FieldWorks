/*----------------------------------------------------------------------------------------------
Copyright 2004, SIL International. All rights reserved.

File: UnicodeString8.h
Responsibility: Andrew Weaver
Last reviewed: Neil Mayhew, 29 Jan 2004

	Definition of Unicode UTF-8 String class. Inherits from std::string. Performs
	conversion of UTF-16/32 strings to UTF-8 using a UnicodeConverter class method.

----------------------------------------------------------------------------------------------*/

#ifndef _UNICODESTRING8_H_
#define _UNICODESTRING8_H_

#ifndef __GNUC__
#pragma once
#endif // __GNUC__

#include <string>
#include "unicode/utypes.h"   // Basic ICU data types

class UnicodeString8 : public std::string
{
public:
	UnicodeString8(const UChar* source, int sourceLen = -1);
#if !WIN32
	UnicodeString8(const wchar_t* source, int sourceLen = -1);
#endif
};

#endif // _UNICODESTRING8_H_
