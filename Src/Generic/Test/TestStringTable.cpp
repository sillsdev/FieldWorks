/*
Copyright (c) 2007-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
 *
 *    TestStringTable.cpp
 *
 *    Implementation of a class that tests the StringTable class.
 *
 *    Andrew Weaver - 2007-02-05
 *
 *    $Id$
 */

#include "common.h"
#include "StringTable.h"
#include "TestStringTable.h"
#include "UnicodeConverter.h"

#include <iostream>
#include <vector>

TestStringTable::TestStringTable() { }

/**
 * Get a pointer and cch (count of characters) for a string with id stid defined in a
 * resource header file. (Modelled on UtilString::GetResourceString().)
 * @param pprgch output UTF-16 string
 * @param pcch output integer string length
 * @param stid input integer string id, e.g., kstidComment
 */
void TestStringTable::GetResourceString(const wchar ** pprgch, int * pcch, int stid)
{
	std::cout << "TestStringTable::GetResourceString()" << std::endl;

	const OLECHAR* ptr;
	int len;

	StringTable::GetString(stid, ptr, len);
	if (len)
	{
		std::vector<char> value8(len);
		value8.resize(UnicodeConverter::Convert(ptr, len, &value8[0], value8.size()));
		if (value8.size() > len)
			UnicodeConverter::Convert(ptr, len, &value8[0], value8.size());
		std::string value8Str;
		value8Str.assign(value8.begin(), value8.begin() + value8.size());
		std::cout << "length: " << len << " value: " << value8Str << std::endl;
	}
	else
	{
		std::cout << "length: " << len << " value: NULL" << std::endl;
	}

	StringTable::GetString(0, ptr, len);
	if (len)
	{
		std::vector<char> value8(len);
		value8.resize(UnicodeConverter::Convert(ptr, len, &value8[0], value8.size()));
		if (value8.size() > len)
			UnicodeConverter::Convert(ptr, len, &value8[0], value8.size());
		std::string value8Str;
		value8Str.assign(value8.begin(), value8.begin() + value8.size());
		std::cout << "length: " << len << " value: " << value8Str << std::endl;
	}
	else
	{
		std::cout << "length: " << len << " value: NULL" << std::endl;
	}

	StringTable::GetString(stid, ptr, len);
	if (len)
	{
		std::vector<char> value8(len);
		value8.resize(UnicodeConverter::Convert(ptr, len, &value8[0], value8.size()));
		if (value8.size() > len)
			UnicodeConverter::Convert(ptr, len, &value8[0], value8.size());
		std::string value8Str;
		value8Str.assign(value8.begin(), value8.begin() + value8.size());
		std::cout << "length: " << len << " value: " << value8Str << std::endl;
	}
	else
	{
		std::cout << "length: " << len << " value: NULL" << std::endl;
	}
}

/**
 * Main method
 * @param argc input integer number of command line arguments
 * @param argv input array of character strings containing command line arguments
 */
int main(int argc, char* argv[])
{
	std::cout << "main" << std::endl;

	TestStringTable rstrm;

	const wchar* prgch;
	int cch;
	int stid = 42;

	rstrm.GetResourceString(&prgch, &cch, stid);

	stid = 93;

	rstrm.GetResourceString(&prgch, &cch, stid);

	stid = 4;

	rstrm.GetResourceString(&prgch, &cch, stid);

	return 0;
}
