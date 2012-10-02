/*----------------------------------------------------------------------------------------------
Copyright 2004, SIL International. All rights reserved.

File: UnicodeString8.cpp
Responsibility: Andrew Weaver
Last reviewed: Neil Mayhew, 29 Jan 2004

	Implementation of Unicode UTF-8 String class. Inherits from std::string. Performs
	conversion of UTF-16 strings to UTF-8 using a UnicodeConverter class method.

----------------------------------------------------------------------------------------------*/

#include "UnicodeString8.h"
#include "UnicodeConverter.h"

#include "unicode/utypes.h"   // Basic ICU data types
#include <vector>
#include <algorithm>

/*----------------------------------------------------------------------------------------------
	This constructor uses a UnicodeConverter class method to convert a string from UTF-16 to
	to UTF-8. A char vector is used to receive the result of the conversion. (This is
	considered the safest way to work with the C API, u_strToUTF8(), within
	UnicodeConverter::Convert.)

	Assumptions:
		None

	Exit conditions:
		<text>

	Parameters:
		<text>
----------------------------------------------------------------------------------------------*/
UnicodeString8::UnicodeString8(const UChar* source, int sourceLen)
{
	std::vector<char> vc(std::max(sourceLen, 1));

	vc.resize(UnicodeConverter::Convert(source, sourceLen, &vc[0], vc.size()));

	if (vc.size() > std::max(sourceLen, 1))  // was original buffer too small?
		UnicodeConverter::Convert(source, sourceLen, &vc[0], vc.size());

	assign(vc.begin(), vc.begin() + vc.size());
}

#if !WIN32

/*----------------------------------------------------------------------------------------------
	This constructor uses a UnicodeConverter class method to convert a string from UTF-32 to
	to UTF-8. A char vector is used to receive the result of the conversion. (This is
	considered the safest way to work with the C API, u_strToUTF8(), within
	UnicodeConverter::Convert.)

	Assumptions:
		None

	Exit conditions:
		<text>

	Parameters:
		<text>
----------------------------------------------------------------------------------------------*/
UnicodeString8::UnicodeString8(const wchar_t* source, int sourceLen)
{
	std::vector<char> vc(std::max(sourceLen, 1));

	vc.resize(UnicodeConverter::Convert(source, sourceLen, &vc[0], vc.size()));

	if (vc.size() > std::max(sourceLen, 1))  // was original buffer too small?
		UnicodeConverter::Convert(source, sourceLen, &vc[0], vc.size());

	assign(vc.begin(), vc.begin() + vc.size());
}

#endif
