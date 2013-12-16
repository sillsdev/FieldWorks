/*
Copyright (c) 2007-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
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
