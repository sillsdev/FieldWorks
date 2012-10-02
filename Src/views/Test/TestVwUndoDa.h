/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestVwUndoDa.h
Responsibility:
Last reviewed:

	Unit tests for the VwUndoDa class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestVwUndoDa_H_INCLUDED
#define TestVwUndoDa_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{

	// Now we get to the actual tests.
	class TestVwUndoDa : public unitpp::suite
	{
		IActionHandlerPtr m_qacth;
		ISilDataAccessPtr m_qsda; // a VwUndoDa
		IVwCacheDaPtr m_qcda; // another interface on m_qsda
		ITsStrFactoryPtr m_qtsf;

		void VerifyObjSeq(HVO hvoObj, int tag, HVO * prghvoExpected, int chvoExpected)
		{
			HVO rghvo[50];
			int chvo;
			m_qsda->VecProp(hvoObj, tag, 50, &chvo, rghvo);
			unitpp::assert_eq("vec length", chvo, chvoExpected);
			for (int i = 0; i < chvo; i++)
				unitpp::assert_eq("item in vec", prghvoExpected[i], rghvo[i]);
		}

		/*--------------------------------------------------------------------------------------
			Test string save methods for proper handling of normalizion.
		--------------------------------------------------------------------------------------*/
		void testUndoRedo()
		{
			HRESULT hr;
			StrUni stuFirst(L"string");
			ITsStringPtr qtssFirst;

			const int ktagString = 1001;
			const int ktagInt = 1002;
			const int ktagRefAtom = 1003;
			const int ktagOwnAtom = 1004;
			const int ktagRefSeq = 1005;
			const int ktagOwnSeq = 1006;
			const int ktagMsa = 1007;
			const HVO khvoObj = 7000;
			const HVO khvoObj1 = 7001;
			const HVO khvoObj2 = 7002;
			const HVO khvoObj3 = 7003;
			const HVO khvoObj4 = 7004;
			HVO rghvoFirst[] = {khvoObj1, khvoObj2, khvoObj3};

			// Cache some initial property values.
			m_qtsf->MakeString(stuFirst.Bstr(), g_wsEng, &qtssFirst);
			m_qcda->CacheStringAlt(khvoObj, ktagMsa, g_wsEng, qtssFirst);
			m_qcda->CacheStringProp(khvoObj, ktagString, qtssFirst);
			m_qcda->CacheIntProp(khvoObj, ktagInt, 79);
			m_qcda->CacheObjProp(khvoObj, ktagRefAtom, khvoObj1);
			m_qcda->CacheObjProp(khvoObj, ktagOwnAtom, khvoObj4);
			m_qcda->CacheVecProp(khvoObj, ktagRefSeq, rghvoFirst, 3);
			m_qcda->CacheVecProp(khvoObj, ktagOwnSeq, rghvoFirst, 3);

			// Set some values, in three groups.
			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");
			m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			StrUni stuSecond(L"Second");
			ITsStringPtr qtssSecond;
			m_qtsf->MakeString(stuSecond.Bstr(), g_wsEng, &qtssSecond);
			hr = m_qsda->SetString(khvoObj, ktagString, qtssSecond);
			m_qsda->SetObjProp(khvoObj, ktagRefAtom, khvoObj2);
			m_qsda->SetObjProp(khvoObj2, ktagRefAtom, khvoObj3);
			HVO hvoNewSeq;
			m_qsda->MakeNewObject(50, // class id is arbitrary for this cache
				khvoObj, ktagOwnSeq, 1, // put it in position 1
				&hvoNewSeq);

			m_qsda->EndUndoTask();

			// Second group of actions.
			m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			hr = m_qsda->SetMultiStringAlt(khvoObj, ktagMsa, g_wsEng, qtssSecond);
			StrUni stuThird(L"three");
			ITsStringPtr qtssThird;
			m_qtsf->MakeString(stuThird.Bstr(), g_wsEng, &qtssThird);
			m_qsda->SetString(khvoObj, ktagString, qtssThird);
			HVO hvoTemp = khvoObj4;
			m_qsda->Replace(khvoObj, ktagRefSeq, 0, 2, &hvoTemp, 1);
			HVO hvoNewAtom;
			m_qsda->MakeNewObject(50, // class id is arbitrary for this cache
				khvoObj, ktagOwnAtom, -2, // owning atomic
				&hvoNewAtom);
			m_qsda->EndUndoTask();

			// Third group.
			m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr());
			m_qsda->SetInt(khvoObj, ktagInt, 98);
			m_qsda->EndUndoTask();

			// Now Undo once.
			ITsStringPtr qtss;
			int n;
			UndoResult ures;
			m_qacth->Undo(&ures);
			// Verify a few things not changed by one Undo
			m_qsda->get_StringProp(khvoObj, ktagString, &qtss);
			unitpp::assert_eq("1st undo did not affect string", qtssThird, qtss);
			hr = m_qsda->get_MultiStringAlt(khvoObj, ktagMsa, g_wsEng, &qtss);
			unitpp::assert_eq("msa not reset by 1st undo", qtssSecond, qtss);
			HVO rghvoRefSeqOther[] = {khvoObj4, khvoObj3};
			VerifyObjSeq(khvoObj, ktagRefSeq, rghvoRefSeqOther, 2);
			// And the one thing that is undone.
			m_qsda->get_IntProp(khvoObj, ktagInt, &n);
			unitpp::assert_eq("int reset", 79, n);

			// Second Undo.
			m_qacth->Undo(&ures);
			// These should be in the state set by the first group of actions.
			m_qsda->get_StringProp(khvoObj, ktagString, &qtss);
			unitpp::assert_eq("string 2nd undo", qtssSecond, qtss);
			HVO hvo;
			m_qsda->get_ObjectProp(khvoObj, ktagRefAtom, &hvo);
			unitpp::assert_eq("ref atom redo", khvoObj2, hvo);
			HVO rghvoOwn2[] = {khvoObj1, hvoNewSeq, khvoObj2, khvoObj3};
			VerifyObjSeq(khvoObj, ktagOwnSeq, rghvoOwn2, 4);
			// And these back in their initial states.
			hr = m_qsda->get_MultiStringAlt(khvoObj, ktagMsa, g_wsEng, &qtss);
			unitpp::assert_eq("msa reset", qtssFirst, qtss);
			m_qsda->get_ObjectProp(khvoObj, ktagOwnAtom, &hvo);
			unitpp::assert_eq("ref atom reset", khvoObj4, hvo);

			// Third Undo.
			m_qacth->Undo(&ures);
			// All values back in initial state.
			m_qsda->get_StringProp(khvoObj, ktagString, &qtss);
			unitpp::assert_eq("string reset", qtssFirst, qtss);
			m_qsda->get_IntProp(khvoObj, ktagInt, &n);
			unitpp::assert_eq("int reset", 79, n);
			hr = m_qsda->get_MultiStringAlt(khvoObj, ktagMsa, g_wsEng, &qtss);
			unitpp::assert_eq("msa reset", qtssFirst, qtss);
			m_qsda->get_ObjectProp(khvoObj, ktagRefAtom, &hvo);
			unitpp::assert_eq("ref atom reset", khvoObj1, hvo);
			m_qsda->get_ObjectProp(khvoObj2, ktagRefAtom, &hvo);
			unitpp::assert_eq("empty ref atom reset", 0, hvo);
			m_qsda->get_ObjectProp(khvoObj, ktagOwnAtom, &hvo);
			unitpp::assert_eq("ref atom reset", khvoObj4, hvo);
			VerifyObjSeq(khvoObj, ktagRefSeq, rghvoFirst, 3);
			VerifyObjSeq(khvoObj, ktagOwnSeq, rghvoFirst, 3);

			// Now Redo. This should put objects in the state after the first group of actions
			m_qacth->Redo(&ures);
			m_qsda->get_StringProp(khvoObj, ktagString, &qtss);
			unitpp::assert_eq("string redo", qtssSecond, qtss);
			m_qsda->get_ObjectProp(khvoObj, ktagRefAtom, &hvo);
			unitpp::assert_eq("ref atom redo", khvoObj2, hvo);
			m_qsda->get_ObjectProp(khvoObj2, ktagRefAtom, &hvo);
			unitpp::assert_eq("empty ref atom redo", khvoObj3, hvo);
			VerifyObjSeq(khvoObj, ktagOwnSeq, rghvoOwn2, 4);

			// Redo second group
			m_qacth->Redo(&ures);
			m_qsda->get_StringProp(khvoObj, ktagString, &qtss);
			unitpp::assert_eq("2nd string redo", qtssThird, qtss);
			hr = m_qsda->get_MultiStringAlt(khvoObj, ktagMsa, g_wsEng, &qtss);
			unitpp::assert_eq("2nd redo msa", qtssSecond, qtss);
			VerifyObjSeq(khvoObj, ktagRefSeq, rghvoRefSeqOther, 2);
			m_qsda->get_ObjectProp(khvoObj, ktagOwnAtom, &hvo);
			unitpp::assert_eq("ref atom redo", hvoNewAtom, hvo);

			// Redo final action.
			m_qacth->Redo(&ures);
			m_qsda->get_StringProp(khvoObj, ktagString, &qtss);
			unitpp::assert_eq("3rd redo did not affect string", qtssThird, qtss);
			m_qsda->get_IntProp(khvoObj, ktagInt, &n);
			unitpp::assert_eq("int redo", 98, n);
		}


	public:
		TestVwUndoDa();

		virtual void Setup()
		{
			CreateTestWritingSystemFactory();
			m_qcda.CreateInstance(CLSID_VwUndoDa);
			m_qcda->QueryInterface(IID_ISilDataAccess, (void **)&m_qsda);
			m_qsda->putref_WritingSystemFactory(g_qwsf);
			m_qtsf.CreateInstance(CLSID_TsStrFactory);
			m_qsda->GetActionHandler(&m_qacth);
		}

		virtual void Teardown()
		{
			m_qtsf.Clear();
			m_qsda.Clear();
			m_qcda.Clear();
			m_qacth.Clear();
			CloseTestWritingSystemFactory();
		}
	};
}

#endif /*TestVwUndoDa_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
