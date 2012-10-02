/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2010 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestSmartBstr.h
Authorship History: MarkS
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestSmartBstr_H_INCLUDED
#define TestSmartBstr_H_INCLUDED

#include <sys/stat.h>
#include "testGenericLib.h"

namespace TestGenericLib
{
	/**
	 * Unit tests for Generic/SmartBstr.h.
	 * Note that this unit test suite is unrelated to the similarly-named file TestSmartBstr.cpp.
	 */
	class TestSmartBstr : public unitpp::suite
	{
		void testEqualityToLiteral()
		{
			SmartBstr bstr(L"pineapple");
			unitpp::assert_true("bstr should == literal", bstr == L"pineapple");
		}

	public:
		TestSmartBstr();
	};
}

#endif /*TestSmartBstr_H_INCLUDED*/