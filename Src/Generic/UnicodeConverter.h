/*----------------------------------------------------------------------------------------------
Copyright 2004, SIL International. All rights reserved.

File: UnicodeConverter.h
Responsibility: Andrew Weaver
Last reviewed: Neil Mayhew, 29 Jan 2004

	Definition of Unicode Converter class. Uses ICU functions to convert from UTF-8 to
	UTF-16 and from UTF-16 to UTF-8.

	The ifdef'd out code is not needed for the relatively straightforward conversions being
	performed at present, but it might be of use in the future. (It is required for the
	Singleton design pattern and use of an ICU converter.)

----------------------------------------------------------------------------------------------*/

#ifndef _UNICODECONVERTER_H_
#define _UNICODECONVERTER_H_

#ifndef __GNUC__
#pragma once
#endif // __GNUC__

#include "unicode/utypes.h"   // Basic ICU data types
#include <stdlib.h>

class UnicodeConverter
{
public:

//	The following two methods use overloading to perform conversion from UTF-8
//	to UTF-16 and from UTF-16 to UTF-8.
	static int Convert(const char* source, int sourceLen, UChar* target, int targetLen);
	static int Convert(const UChar* source, int sourceLen, char* target, int targetLen);

#if !WIN32 // SIZEOF_WCHAR_T != 2
//	The following two methods use overloading to perform conversion from UTF-16
//	to UTF-32 and from UTF-32 to UTF-16.
	static int Convert(const UChar* source, int sourceLen, wchar_t* target, int targetLen);
	static int Convert(const wchar_t* source, int sourceLen, UChar* target, int targetLen);

//	The following two methods use overloading to perform conversion from UTF-8
//	to UTF-32 and from UTF-32 to UTF-8.
	static int Convert(const char* source, int sourceLen, wchar_t* target, int targetLen);
	static int Convert(const wchar_t* source, int sourceLen, char* target, int targetLen);
#endif // !WIN32 // SIZEOF_WCHAR_T != 2

#if 0	// Singleton

protected:
	UnicodeConverter();
public:
	~UnicodeConverter();

	static UnicodeConverter* Instance();

private:
	static UnicodeConverter* s_instance;
	struct UConverter* m_converter;

#endif	// Singleton

};

#endif // _UNICODECONVERTER_H_
