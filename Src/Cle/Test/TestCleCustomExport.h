/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestCleCustomExport.h
Responsibility:
Last reviewed:

	Unit tests for the CleCustomExport class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTCLEEXPORTDLG_H_INCLUDED
#define TESTCLEEXPORTDLG_H_INCLUDED

#pragma once

#include "testCle.h"

namespace TestCle
{
	class TestCleCustomExport : public unitpp::suite
	{
		CleCustomExportPtr m_qclcex;

		void testGetEnumString()
		{
			// This test is bogus, and will be replaced.
			unitpp::assert_true("Non-null m_qcexd after setup", m_qclcex.Ptr() != 0);
			SmartBstr sbstr;
			StrUni stu;
			HRESULT hr;
			int flid = kflidCmPerson_Gender;
			hr = m_qclcex->GetEnumString(flid, 0, &sbstr);
			stu = sbstr.Chars();
			unitpp::assert_eq("GetEnumString(kflidCmPerson_Gender,0) HRESULT", S_OK, hr);
			unitpp::assert_true("GetEnumString(kflidCmPerson_Gender,0)", stu == "unknown");
			hr = m_qclcex->GetEnumString(flid, 1, &sbstr);
			stu = sbstr.Chars();
			unitpp::assert_eq("GetEnumString(kflidCmPerson_Gender,1) HRESULT", S_OK, hr);
			unitpp::assert_true("GetEnumString(kflidCmPerson_Gender,1)", stu == "male");
			hr = m_qclcex->GetEnumString(flid, 2, &sbstr);
			stu = sbstr.Chars();
			unitpp::assert_eq("GetEnumString(kflidCmPerson_Gender,2) HRESULT", S_OK, hr);
			unitpp::assert_true("GetEnumString(kflidCmPerson_Gender,2)", stu == "female");
			hr = m_qclcex->GetEnumString(flid, 3, &sbstr);
			stu = sbstr.Chars();
			unitpp::assert_eq("GetEnumString(kflidCmPerson_Gender,3) HRESULT", S_OK, hr);
			unitpp::assert_true("GetEnumString(kflidCmPerson_Gender,3)", stu == "");

			flid = kflidCmPerson_IsResearcher;
			hr = m_qclcex->GetEnumString(flid, 0, &sbstr);
			stu = sbstr.Chars();
			unitpp::assert_eq("GetEnumString(kflidCmPerson_IsResearcher,0) HRESULT", S_OK, hr);
			unitpp::assert_true("GetEnumString(kflidCmPerson_IsResearcher,0)", stu == "no");
			hr = m_qclcex->GetEnumString(flid, 1, &sbstr);
			stu = sbstr.Chars();
			unitpp::assert_eq("GetEnumString(kflidCmPerson_IsResearcher,1) HRESULT", S_OK, hr);
			unitpp::assert_true("GetEnumString(kflidCmPerson_IsResearcher,1)", stu == "yes");
			hr = m_qclcex->GetEnumString(flid, 2, &sbstr);
			stu = sbstr.Chars();
			unitpp::assert_eq("GetEnumString(kflidCmPerson_IsResearcher,1) HRESULT",
				S_OK, hr);
			unitpp::assert_true("GetEnumString(kflidCmPerson_IsResearcher,2)", stu == "");

#if WantWWStuff
			flid = kflidMoInflAffixSlot_Optional;
			hr = m_qclcex->GetEnumString(flid, 0, &sbstr);
			stu = sbstr.Chars();
			unitpp::assert_eq("GetEnumString(kflidMoInflAffixSlot_Optional,0) HRESULT",
				S_OK, hr);
			unitpp::assert_true("GetEnumString(kflidMoInflAffixSlot_Optional,0)", stu == "no");
			hr = m_qclcex->GetEnumString(flid, 1, &sbstr);
			stu = sbstr.Chars();
			unitpp::assert_eq("GetEnumString(kflidMoInflAffixSlot_Optional,1) HRESULT",
				S_OK, hr);
			unitpp::assert_true("GetEnumString(kflidMoInflAffixSlot_Optional,1)", stu == "yes");
			hr = m_qclcex->GetEnumString(flid, 2, &sbstr);
			stu = sbstr.Chars();
			unitpp::assert_eq("GetEnumString(kflidMoInflAffixSlot_Optional,2) HRESULT",
				S_OK, hr);
			unitpp::assert_true("GetEnumString(kflidMoInflAffixSlot_Optional,2)", stu == "");
#endif

			hr = m_qclcex->GetEnumString(0, 0, &sbstr);
			unitpp::assert_eq("GetEnumString(0,0) HRESULT", E_INVALIDARG, hr);
			hr = m_qclcex->GetEnumString(1, 0, &sbstr);
			stu = sbstr.Chars();
			unitpp::assert_eq("GetEnumString(1,0) HRESULT", S_FALSE, hr);
			unitpp::assert_true("GetEnumString(1,0)", stu == "");
		}
	public:
		TestCleCustomExport();
		void Setup()
		{
			m_qclcex.Attach(NewObj CleCustomExport);
		}
		void Teardown()
		{
			m_qclcex.Clear();
		}
	};
}

#endif /*TESTCLEEXPORTDLG_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkcle-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
