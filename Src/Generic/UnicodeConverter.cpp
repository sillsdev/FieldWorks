/*----------------------------------------------------------------------------------------------
Copyright (c) 2004-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: UnicodeConverter.cpp
Responsibility: Andrew Weaver
Last reviewed: Neil Mayhew, 29 Jan 2004

	Implementation of Unicode Converter class. Uses ICU functions to convert from UTF-8 to
	UTF-16 and from UTF-16 to UTF-8. These conversions do not require an ICU converter, nor
	is it necessary to create an instance of this class (the Convert methods are static).

	The Commented out code at the end implements the Singleton design pattern and uses an ICU
	converter. None of this is needed for the relatively straightforward conversions being
	performed at present, but it might be of use in the future.

----------------------------------------------------------------------------------------------*/

#include "UnicodeConverter.h"

#include <cassert>
#include <stdexcept>

#if 0	// Singleton
#include "unicode/ucnv.h"     // ICU Converter API
#endif	// Singleton

#include "unicode/utypes.h"   // Basic ICU data types
#include "unicode/ustring.h"  // u_str{From,To}UTF8 & u_str{From,To}WCS

/*----------------------------------------------------------------------------------------------
	This method uses an ICU function to convert a string from UTF-8 to UTF-16.

	Assumptions:
		If sourceLen is -1, it will be computed (by ICU)

	Exit conditions:
		<text>

	Parameters:
		<text>

	Return value:
		The number of characters required to store the fully-converted string
			(which may be greater than targetLen)
----------------------------------------------------------------------------------------------*/
int UnicodeConverter::Convert(const char* source, int sourceLen,
	UChar* target, int targetLen)
{
	UErrorCode status = U_ZERO_ERROR;
	int32_t spaceRequiredForData;

	u_strFromUTF8(target, targetLen, &spaceRequiredForData, source, sourceLen, &status);

	if (U_FAILURE(status) && status != U_BUFFER_OVERFLOW_ERROR)
		throw std::runtime_error("Unable to convert from UTF-8 to UTF-16");

	return spaceRequiredForData;
}

/*----------------------------------------------------------------------------------------------
	This method uses an ICU function to convert a string from UTF-16 to UTF-8.

	Assumptions:
		If sourceLen is -1, it will be computed (by ICU)

	Exit conditions:
		<text>

	Parameters:
		<text>

	Return value:
		The number of characters required to store the fully-converted string
			(which may be greater than targetLen)
----------------------------------------------------------------------------------------------*/
int UnicodeConverter::Convert(const UChar* source, int sourceLen,
	char* target, int targetLen)
{
	UErrorCode status = U_ZERO_ERROR;
	int32_t spaceRequiredForData;

	u_strToUTF8(target, targetLen, &spaceRequiredForData, source, sourceLen, &status);

	if (U_FAILURE(status) && status != U_BUFFER_OVERFLOW_ERROR)
		throw std::runtime_error("Unable to convert from UTF-16 to UTF-8");

	return spaceRequiredForData;
}

#if !WIN32 // SIZEOF_WCHAR_T != 2

/*----------------------------------------------------------------------------------------------
	This method uses an ICU function to convert a string from UTF-16 to UTF-32.

	Assumptions:
		If sourceLen is -1, it will be computed (by ICU)

	Exit conditions:
		<text>

	Parameters:
		<text>

	Return value:
		The number of characters needed to store the fully-converted result
			(which may be greater than targetLen)
----------------------------------------------------------------------------------------------*/
int UnicodeConverter::Convert(const UChar* source, int sourceLen,
	wchar_t* target, int targetLen)
{
	UErrorCode status = U_ZERO_ERROR;
	int32_t spaceRequiredForData;

	u_strToWCS(target, targetLen, &spaceRequiredForData, source, sourceLen, &status);

	if (U_FAILURE(status) && status != U_BUFFER_OVERFLOW_ERROR)
		throw std::runtime_error("Unable to convert from UTF-16 to UTF-32");

	return spaceRequiredForData;
}

/*----------------------------------------------------------------------------------------------
	This method uses an ICU function to convert a string from UTF-32 to UTF-16.

	Assumptions:
		If sourceLen is -1, it will be computed (by ICU)

	Exit conditions:
		<text>

	Parameters:
		<text>

	Return value:
		The number of characters needed to store the fully-converted result
			(which may be greater than targetLen)
----------------------------------------------------------------------------------------------*/
int UnicodeConverter::Convert(const wchar_t* source, int sourceLen,
	UChar* target, int targetLen)
{
	UErrorCode status = U_ZERO_ERROR;
	int32_t spaceRequiredForData;

	u_strFromWCS(target, targetLen, &spaceRequiredForData, source, sourceLen, &status);

	if (U_FAILURE(status) && status != U_BUFFER_OVERFLOW_ERROR)
		throw std::runtime_error("Unable to convert from UTF-32 to UTF-16");

	return spaceRequiredForData;
}

/*----------------------------------------------------------------------------------------------
	This method uses a C library function to convert a string from UTF-32 to UTF-8.

	Assumptions:
		If sourceLen is -1, it will be computed (by the library function)
		The current locale is used, and is assumed to be a UTF-8 one

	Exit conditions:
		<text>

	Parameters:
		<text>

	Return value:
		The number of characters required to store the fully-converted string
			(which may be greater than targetLen)
----------------------------------------------------------------------------------------------*/

int UnicodeConverter::Convert(const wchar_t* source, int sourceLen,
	char* target, int targetLen)
{
		// Assume locale is UTF-8
		return wcstombs(target, source, targetLen);
}

/*----------------------------------------------------------------------------------------------
	This method uses a C library function to convert a string from UTF-8 to UTF-32.

	Assumptions:
		If sourceLen is -1, it will be computed (by the library function)
		The current locale is used, and is assumed to be a UTF-8 one

	Exit conditions:
		<text>

	Parameters:
		<text>

	Return value:
		The number of characters required to store the fully-converted string
			(which may be greater than targetLen)
----------------------------------------------------------------------------------------------*/

int UnicodeConverter::Convert(const char* source, int sourceLen,
	wchar_t* target, int targetLen)
{
		// Assume locale is UTF-8
		return mbstowcs(target, source, targetLen);
}

#endif // !WIN32 // SIZEOF_WCHAR_T != 2

#if 0	// Singleton

//	=================================================
//	singleton & UConverter stuff not currently needed
//	=================================================

UnicodeConverter* UnicodeConverter::s_instance = 0;

/*----------------------------------------------------------------------------------------------
	This public method is the only means to create an instance of this class. It employs the
	Singleton design pattern to prevent more than one UnicodeConverter object from being
	created.

	Assumptions:
		<text>

	Exit conditions:
		<text>

	Parameters:
		None
----------------------------------------------------------------------------------------------*/
UnicodeConverter* UnicodeConverter::Instance()
{
	if (s_instance == 0)
		s_instance = new UnicodeConverter();

	return s_instance;
}

/*----------------------------------------------------------------------------------------------
	This private constructor can be called only by the Instance method.

	Assumptions:
		<text>

	Exit conditions:
		<text>

	Parameters:
		None
----------------------------------------------------------------------------------------------*/
UnicodeConverter::UnicodeConverter()
{
	UErrorCode status = U_ZERO_ERROR;

	m_converter = ucnv_open("UTF-8", &status);  // open the converter

	assert(status == U_ZERO_ERROR);

	if (status != U_ZERO_ERROR)
	{
		TRACE("Unable to allocate an ICU converter\n");
		if (m_converter != 0)
		{
			ucnv_close(m_converter);  // close the converter
			m_converter = 0;
		}
		//throw std::runtime_error("Unable to allocate an ICU converter");
	}
}

/*----------------------------------------------------------------------------------------------
	The class destructor.

	Assumptions:
		<text>

	Exit conditions:
		<text>

	Parameters:
		None
----------------------------------------------------------------------------------------------*/
UnicodeConverter::~UnicodeConverter()
{
	assert(!"necessary to explicitly delete sole instance");

	if (m_converter != 0)
		ucnv_close(m_converter);  // close the converter

	if (s_instance == this)
		s_instance = 0;
}

/*----------------------------------------------------------------------------------------------
	This method uses an ICU converter to convert a string from UTF-8 to UTF-16.

	Assumptions:
		<text>

	Exit conditions:
		<text>

	Parameters:
		<text>

	Return value:
		The number of characters required to store the fully-converted string
			(which may be greater than targetLen)
----------------------------------------------------------------------------------------------*/
int UnicodeConverter::Convert(const char* source, int sourceLen,
	UChar* target, int targetLen)
{
	UErrorCode status = U_ZERO_ERROR;

	int spaceRequiredForData = ucnv_toUChars(m_converter,
		target, targetLen, source, sourceLen, &status);

	if (U_FAILURE(status) && status != U_BUFFER_OVERFLOW_ERROR)
	{
		TRACE("Unable to convert from UTF-8 to UTF-16 (" << status << ")\n");
		//throw std::runtime_error("Unable to convert from UTF-8 to UTF-16");
	}

	return spaceRequiredForData;
}

/*----------------------------------------------------------------------------------------------
	This method uses an ICU converter to convert a string from UTF-16 to UTF-8.

	Assumptions:
		<text>

	Exit conditions:
		<text>

	Parameters:
		<text>

	Return value:
		The number of characters required to store the fully-converted string
			(which may be greater than targetLen)
----------------------------------------------------------------------------------------------*/
int UnicodeConverter::Convert(const UChar* source, int sourceLen,
	char* target, int targetLen)
{
	UErrorCode status = U_ZERO_ERROR;

	int spaceRequiredForData = ucnv_fromUChars(m_converter,
		target, targetLen, source, sourceLen, &status);

	if (U_FAILURE(status) && status != U_BUFFER_OVERFLOW_ERROR)
	{
		TRACE("Unable to convert from UTF-16 to UTF-8 (" << status << ")\n");
		//throw std::runtime_error("Unable to convert from UTF-16 to UTF-8");
	}

	return spaceRequiredForData;
}

#endif	// Singleton
