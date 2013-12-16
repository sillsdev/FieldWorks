/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2010-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

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