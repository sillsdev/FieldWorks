/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestPossList.h
Responsibility:
Last reviewed:

	Unit tests for the functions/classes from AfApp.cpp
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTPOSSLIST_H_INCLUDED
#define TESTPOSSLIST_H_INCLUDED

#pragma once

#include "testAfLib.h"
#include "NotebkTlb.h"		// for CLSID_ResearchNotebook

namespace TestAfLib
{
	class TestPossList : public unitpp::suite
	{
		void testFindPss()
		{
			HRESULT hr;
			const int hvoLangProj = 1;
			int hvoEventTypeList;
			unitpp::assert_eq("AfApp::Papp() == &g_app", (long)AfApp::Papp(), (long)&g_app);
			AfDbInfoPtr qdbi = g_app.GetDbInfo(g_szDbName, g_szSvrName);

			// Load the Events Type list
			IOleDbEncapPtr qode;
			IOleDbCommandPtr qodc;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			qdbi->GetDbAccess(&qode);
			StrUni stu(L"select obj from CmMajorObject_Name where Txt = 'Event Types'");
			hr = qode->CreateCommand(&qodc);
			unitpp::assert_eq("qode->CreateCommand hr", S_OK, hr);
			hr = qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset);
			unitpp::assert_eq("qodc->ExecCommand hr", S_OK, hr);
			hr = qodc->GetRowset(0);
			unitpp::assert_eq("qodc->GetRowset hr", S_OK, hr);
			hr = qodc->NextRow(&fMoreRows);
			unitpp::assert_eq("qodc->NextRow hr", S_OK, hr);
			hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoEventTypeList),
				isizeof(HVO), &cbSpaceTaken, &fIsNull, 0);
			unitpp::assert_eq("qodc->GetColValue hr", S_OK, hr);
			AfLpInfoPtr qlpi = qdbi->GetLpInfo(hvoLangProj);
			PossListInfoPtr qpli;
			// Make sure we look for English in case someone is using a different
			// top analysis language.
			int ws;
			ILgWritingSystemFactoryPtr qwsf;
			qdbi->GetLgWritingSystemFactory(&qwsf);
			unitpp::assert_true("WS Factory for PossList", qwsf.Ptr());
			SmartBstr sbstrWs(L"en");
			hr = qwsf->GetWsFromStr(sbstrWs, &ws);
			unitpp::assert_eq("qwsf->GetWsFromStr hr", S_OK, hr);
			unitpp::assert_true("qwsf->GetWsFromStr ws", ws);
			qlpi->LoadPossList(hvoEventTypeList, ws, &qpli, true);
			PossItemInfo * ppii;
			Locale loc("en");
			int ipii;

			// Simple test to find an item without using hierarchy.
			ppii = qpli->FindPss(L"pe", loc, kpntName, &ipii, false);
			StrUni stuName;
			ppii->GetName(stuName, kpntName);
			unitpp::assert_true("Flat Performance poss item", stuName.Equals(L"Performance", 11));

			// Make sure it fails when looking for something that doesn't exist.
			ppii = qpli->FindPss(L"v", loc, kpntName, &ipii, false);
			unitpp::assert_true("Missing poss item", !ppii);

			// Simple test to find an item with hierarchy.
			ComBool fExact;
			ppii = qpli->FindPssHier(L"o:p", loc, kpntName, fExact);
			ppii->GetName(stuName, kpntName);
			unitpp::assert_true("Hierarchical Performance poss item", stuName.Equals(L"Performance", 11));

			qlpi->CleanUp();
		}

	public:
		TestPossList();
	};
}

#endif /*TESTPOSSLIST_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkaflib-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
