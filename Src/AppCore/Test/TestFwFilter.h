/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestFwFilter.h
Responsibility:
Last reviewed:

	Unit tests for the functions/classes from AppCore/FwFilter.cpp
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTFWFILTER_H_INCLUDED
#define TESTFWFILTER_H_INCLUDED

#pragma once

#include "testAfLib.h"
#include "NotebkTlb.h"		// for CLSID_ResearchNotebook
//#include "CMDataObject.h"	// for CMDataObject

namespace TestAfLib
{
	const OLECHAR g_szDbName[] = L"TestLangProj";
	const OLECHAR g_szSvrName[] = L".\\SILFW";
	const OLECHAR g_szLpName[] = L"Kalaba";

	class TestFilterUtil : public unitpp::suite
	{
		void testLoadFilters()
		{
			unitpp::assert_eq("AfApp::Papp() == &g_app", (long)AfApp::Papp(), (long)&g_app);
			AfDbInfo * pdbi = g_app.GetDbInfo(g_szDbName, g_szSvrName);

			GUID guidApp = CLSID_TsStrFactory;
			bool fOk = FilterUtil::LoadFilters(pdbi, &guidApp);
			int cfltr = pdbi->GetFilterCount();
			unitpp::assert_true("FilterUtil::LoadFilters(CLSID_TsStrFactory)", fOk);
			unitpp::assert_eq("pdbi->GetFilterCount() == 0", cfltr, 0);

			guidApp = CLSID_ResearchNotebook;
			fOk = FilterUtil::LoadFilters(pdbi, &guidApp);
			cfltr = pdbi->GetFilterCount();
			unitpp::assert_true("FilterUtil::LoadFilters(CLSID_ResearchNotebook)", fOk);
			unitpp::assert_true("pdbi->GetFilterCount() > 0", cfltr > 0);

			StrAnsi str;
			for (int ifltr = 0; ifltr < cfltr; ++ifltr)
			{
				AppFilterInfo & afi = pdbi->GetFilterInfo(ifltr);
				str.Format("[%d]afi.m_stuName", ifltr);
				unitpp::assert_true(str.Chars(), afi.m_stuName.Length() != 0);
				str.Format("[%d]afi.m_stuColInfo", ifltr);
				unitpp::assert_true(str.Chars(), afi.m_stuColInfo.Length() != 0);
				str.Format("[%d]afi.m_clidRec == kclidRnGenericRec", ifltr);
				unitpp::assert_true(str.Chars(), afi.m_clidRec == kclidRnGenericRec);
			}
		}

//		This functionality now requires a database connection, which is too much work for the
//		benefit.
//		void test ParseWritingSystem()
//		{
//			wchar szKey1[] = L"This is a test";
//			wchar szKey2[] = L"[ENG]test";
//			wchar szKey3[] = L"    [ENG] test";
//			wchar szKey4[] = L"This is a [ENG] test";
//			int ws;
//			ILgWritingSystemFactoryPtr qwsf;
//			SmartBstr sbstrEng(L"en");
//			int wsENG = qwsf->GetWsFromStr(sbstrEng, &wsEng);
//			wchar * psz;
//
//			psz = szKey1;
//			ws = FilterUtil::ParseWritingSystem(psz);
//			unitpp::assert_eq("ParseWritingSystem(L\"This is a test\") ws", ws, 0);
//			unitpp::assert_eq("ParseWritingSystem(L\"This is a test\") psz", psz, szKey1);
//
//			psz = szKey2;
//			ws = FilterUtil::ParseWritingSystem(psz);
//			unitpp::assert_eq("ParseWritingSystem(L\"[ENG]test\") ws", ws, wsENG);
//			unitpp::assert_eq("ParseWritingSystem(L\"[ENG]test\") psz", psz, szKey2 + 5);
//
//			psz = szKey3;
//			ws = FilterUtil::ParseWritingSystem(psz);
//			unitpp::assert_eq("ParseWritingSystem(L\"    [ENG] test\") ws", ws, wsENG);
//			unitpp::assert_eq("ParseWritingSystem(L\"    [ENG] test\") psz", psz, szKey3 + 9);
//
//			psz = szKey4;
//			ws = FilterUtil::ParseWritingSystem(psz);
//			unitpp::assert_eq("ParseWritingSystem(L\"This is a [ENG] test\") ws", ws, 0);
//			unitpp::assert_eq("ParseWritingSystem(L\"This is a [ENG] test\") psz", psz, szKey4);
//		}

	public:
		TestFilterUtil();
	};
}

#endif /*TESTFWFILTER_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkaflib-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
