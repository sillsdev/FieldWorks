/*
 *    Copyright 2007, SIL International. All rights reserved.
 *
 *    TestStringTable.h
 *
 *    Definition of a class that tests the StringTable class.
 *
 *    Andrew Weaver - 2007-02-05
 *
 *    $Id$
 */

#include "common.h"

class TestStringTable
{
public:
	TestStringTable();
	void GetResourceString(const wchar ** pprgch, int * pcch, int stid);
};
