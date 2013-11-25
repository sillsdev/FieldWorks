// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

/*
 *    StringTable.cpp
 *
 *    Implements a file-based alternative to string table resources that are embedded in
 *    the application.
 *
 *    Andrew Weaver - 2007-02-05
 *
 *    $Id$
 */

#ifndef _StringTable_H_
#define _StringTable_H_ 1

#ifndef __GNUC__
#pragma once
#endif // __GNUC__

#include "BasicTypes.h"
#include <map>
#include <vector>
#include <string>

class StringTable
{
public:
	static void GetString(int key, const OLECHAR* & ptr, int & len);

private:
	static void BuildMap();
	static int ConvertKeyToInt(const std::string& _key);
	static void SubstituteNewlines(std::string& _value);
	static std::vector<OLECHAR> ConvertUtf8ToUtf16(const std::string& _value);
	typedef std::map<int, std::vector<OLECHAR> > StringTableCache;
	static StringTableCache cache;
	static bool mapExists;
};

#endif // _StringTable_H_
