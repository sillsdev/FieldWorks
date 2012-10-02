/*
 *    Copyright 2007, SIL International. All rights reserved.
 *
 *    StringTable.cpp
 *
 *    Implements a file-based alternative to string table resources that are embedded in
 *    the application.
 *
 *    Andrew Weaver - 2007-02-05
 *
 *    $Id$
 */

#include "StringTable.h"
#include "BasicTypes.h"
#include "UnicodeConverter.h"

#include <fstream>
#include <map>
#include <string>
#include <vector>
#include <stdlib.h>

StringTable::StringTableCache StringTable::cache;
bool StringTable::mapExists = false;

/**
 * Retrieves a UTF-16 string from a std::map, given an integer key. The first time this
 * method is called, it populates the std::map (containing key/value pairs) from data held
 * in a text file.
 * @param key input integer string id key
 * @param ptr output UTF-16 string
 * @param len output integer string length
 */
void StringTable::GetString(int key, const OLECHAR* & ptr, int & len)
{
	if (!StringTable::mapExists)  // Is map initialisation required?
	{
		StringTable::BuildMap();
	}

	// Search for key in the previously built map.
	StringTableCache::const_iterator iter = cache.find(key);
	if (iter != cache.end())  // Key found in map
	{
		const std::vector<OLECHAR>& value = (*iter).second;
		ptr = &value[0];
		len = value.size();
	}
	else  // Key not found in map
	{
		ptr = NULL;
		len = 0;
	}

	return;
}

/**
 * Creates a std::map from key/value pairs held in a text file.
 */
void StringTable::BuildMap()
{
	std::ifstream input;
	std::string line;

	try
	{
		input.open("strings-en.txt");

		while (!getline(input, line).fail())
		{
			std::string::size_type separatorPos = line.find("\t");
			if (separatorPos != std::string::npos)
			{
				try
				{
					int key = ConvertKeyToInt(line.substr(0, separatorPos));

					std::string value8 = line.substr(separatorPos + 1);
					SubstituteNewlines(value8);
					std::vector<OLECHAR> value16 = ConvertUtf8ToUtf16(value8);

					cache[key] = value16;
				}
				catch (...)  // A data conversion error has occurred.
				{
					// Take no action. Simply omit this key/value pair from map.
				}
			}
		}
	}
	catch (...)  // An I/O error has occurred.
	{
		// Take no action. At worst we'll have an empty map.
	}

	input.close();

	StringTable::mapExists = true;
}

/**
 * Converts a std::string representation of the map key to an integer.
 * @param _key input std::string key
 * @return integer key
 * @throws char* if the supplied key contains non-numeric characters
 */
int StringTable::ConvertKeyToInt(const std::string& _key)
{
	char* endPtr;

	int key = strtol(_key.c_str(), &endPtr, 10);

	if (*endPtr != '\0')  // Source string contains a non-numeric character.
	{
		throw "Invalid key!";
	}

	return key;
}

/**
 * Replaces all occurrences of the string "\n" found in the map value with newline.
 * @param _value input/output std::string (UTF-8) value
 */
void StringTable::SubstituteNewlines(std::string& _value)
{
	std::string newlineToken("\\n");

	std::string::size_type tokenPos = _value.find(newlineToken);
	while (tokenPos != std::string::npos)
	{
		_value.replace(tokenPos, newlineToken.size(), "\n");
		tokenPos = _value.find(newlineToken);
	}
}

/**
 * Converts a UTF-8 representation of the map value to UTF-16.
 * @param _value input std::string (UTF-8) value
 * @return UTF-16 value as a std::vector of OLECHARs
 */
std::vector<OLECHAR> StringTable::ConvertUtf8ToUtf16(const std::string& _value)
{
	std::vector<OLECHAR> value16;

	// Pre-flight conversion to resize target buffer to correct length.
	value16.resize(UnicodeConverter::Convert(_value.data(), _value.size(),
		&value16[0], value16.size()));  // Note: Target buffer length is 0 for pre-flight.

	// Repeat conversion with target buffer of correct length.
	UnicodeConverter::Convert(_value.data(), _value.size(), &value16[0], value16.size());

	return value16;
}
