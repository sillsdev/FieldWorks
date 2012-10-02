/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestTsPropsBldr.h
Responsibility:
Last reviewed:

	Unit tests for the TsPropsBldr class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTTSPROPSBLDR_H_INCLUDED
#define TESTTSPROPSBLDR_H_INCLUDED

#pragma once

#include "testFwKernel.h"

namespace TestFwKernel
{
	class TestTsPropsBldr : public unitpp::suite
	{
		ITsPropsBldrPtr m_qtpb;

		void testSetStrPropValueRgch()
		{
			GUID uid;
			uid.Data1 = 0x12345678;
			uid.Data2 = 0x90ab;
			uid.Data3 = 0xcdef;
			::memcpy(&uid.Data4, "abcdefgh", sizeof(uid.Data4));
			byte rgByte[sizeof(uid)+2];
			rgByte[0] = 2;
			rgByte[1] = 1;
			::memcpy(rgByte+2, &uid, sizeof(uid));

			m_qtpb->SetStrPropValueRgch(1, rgByte, sizeof(uid)+2);

			SmartBstr sbstr;
			m_qtpb->GetStrPropValue(1, &sbstr);
			unitpp::assert_eq("Length is different", 9, sbstr.Length());
			byte rgByteNew[100];
			int nLength = sbstr.Length() * sizeof(OLECHAR);
			::memcpy(rgByteNew, (BSTR)sbstr, nLength);
			int cmp = ::memcmp(&uid, &rgByteNew[2], sizeof(uid));
			unitpp::assert_true("uid is different", cmp == 0);
			unitpp::assert_eq("first byte is different", 2, rgByteNew[0]);
			unitpp::assert_eq("second byte is different", 1, rgByteNew[1]);
		}
		void testSetStrPropValue()
		{
			GUID uid;
			uid.Data1 = 0x12345678;
			uid.Data2 = 0x90ab;
			uid.Data3 = 0xcdef;
			::memcpy(&uid.Data4, "abcdefgh", sizeof(uid.Data4));
			StrUni stuData;
			OLECHAR * prgchData;
			// Make large enough for a guid plus the type character at the start.
			stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
			*prgchData = kodtNameGuidHot;
			memmove(prgchData + 1, &uid, isizeof(uid));

			m_qtpb->SetStrPropValue(1, stuData.Bstr());

			SmartBstr sbstr;
			m_qtpb->GetStrPropValue(1, &sbstr);
			unitpp::assert_eq("Length is different", 9, sbstr.Length());
			byte rgByteNew[100];
			int nLength = sbstr.Length() * sizeof(OLECHAR);
			::memcpy(rgByteNew, (BSTR)sbstr, nLength);
			int cmp = ::memcmp(&uid, &rgByteNew[2], sizeof(uid));
			unitpp::assert_true("uid is different", cmp == 0);
			unitpp::assert_eq("Guid Markers are different", kodtNameGuidHot, sbstr[0]);
		}
	public:
		TestTsPropsBldr();
		virtual void Setup()
		{
			m_qtpb.CreateInstance(CLSID_TsPropsBldr);
		}
		virtual void Teardown()
		{
			m_qtpb = NULL;
		}
	};
}

#endif /*TESTTSPROPSBLDR_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkfwk-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
