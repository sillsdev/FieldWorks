/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestNotifier.h
Responsibility:
Last reviewed:

	Unit tests for the VwRootBox class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestNotifier_H_INCLUDED
#define TestNotifier_H_INCLUDED

#pragma once

#include "testViews.h"
#include "TestLazyBox.h"

namespace TestViews
{
#define kflidTestDummy 999
	class NotifierVc : public DummyBaseVc
	{
	public:
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case 1: // the root; display the paragraphs lazily.
				CheckHr(pvwenv->AddLazyVecItems(kflidStText_Paragraphs, this, 3));
				break;
			case 3: // StTxtPara, display contents
				CheckHr(pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL));
				break;
			}
			return S_OK;
		}
		STDMETHOD(EstimateHeight)(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight)
		{
			*pdyHeight = 15 + hvo * 2; // just give any arbitrary number
			return S_OK;
		}
		STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
			int tag, int frag, int ihvoMin)
		{
			return S_OK;
		}
	};

	class TestNotifier : public unitpp::suite
	{
	static const HVO hvoRoot = 101;
	static const int kcPara = 10;
	static const int kcParaExtra = 5;
	static const int kcParaHvoBase = 1001;
	public:
		// Tests that deleting all paras inside of a DivBox updates the last box of the notifier
		// correctly (i.e. should point to DivBox).
		void testDeleteAllParasWithDivs()
		{
			// Create one book with 16 sections, each section with 16 paragraphs
			HVO rghvoSec[16];
			TestLazyBox::CreateTestBooksWithSections(m_qcda, rghvoSec, kflidStTxtPara_Contents, 1);

			m_qvc.Attach(NewObj DummyVcBkSecParaDiv(kflidStTxtPara_Contents, false, false));
			m_qrootb->SetRootObject(khvoBook, m_qvc, kfragBook, NULL);
			HRESULT hr;
			CheckHr(hr = m_qrootb->Layout(m_qvg32, 300));
			unitpp::assert_true("Layout succeeded", hr == S_OK);

			// Delete all of the paragraphs of the first section in the first book
			CheckHr(m_qcda->CacheReplace(rghvoSec[0], kflidParas, 0, 16, NULL, 0));
			CheckHr(m_qsda->PropChanged(NULL, kpctNotifyAll, rghvoSec[0], kflidParas, 0, 0, 16));

			// Verify that the notifier has correct last box
			VwDivBox* pboxBook = dynamic_cast<VwDivBox*>(m_qrootb->FirstBox());
			VwDivBox* pboxFirstSection = dynamic_cast<VwDivBox*>(pboxBook->FirstBox());
			NotifierVec vpanote;
			pboxBook->GetNotifiers(pboxFirstSection, vpanote);
			unitpp::assert_eq("Unexpected number of notifiers", 2, vpanote.Size());

			VwNotifier * pnote = dynamic_cast<VwNotifier *>(vpanote[0].Ptr());
			unitpp::assert_true("Didn't find a VwNotifier", pnote);

			unitpp::assert_eq("Last box of notifier doesn't point to first section",
				pboxFirstSection, pnote->LastTopLevelBox());
		}

		// This test pounds on the special case in PropChanged where the property is a lazy
		// sequence.
		void testLazyPropChanged()
		{
			// Create a dummy StText with original and extra paragraphs.
			for (int i = 0; i < kcPara + kcParaExtra; i++)
			{
				ITsStringPtr qtss;
				StrUni stuPara;
				stuPara.Format(L"Para %d", i);
				CheckHr(m_qtsf->MakeString(stuPara.Bstr(), g_wsEng, &qtss));
				m_rghvoParas[i] = kcParaHvoBase + i;
				CheckHr(m_qcda->CacheStringProp(m_rghvoParas[i], kflidStTxtPara_Contents, qtss));
			}

			// Make the paragraphs belong to an StText.
			CheckHr(m_qcda->CacheVecProp(hvoRoot, kflidStText_Paragraphs, m_rghvoParas, kcPara));

			m_qvc.Attach(NewObj NotifierVc());
			CheckHr(m_qrootb->SetRootObject(hvoRoot, m_qvc, 1, NULL));
			HRESULT hr;
			CheckHr(hr = m_qrootb->Layout(m_qvg32, 300));
			unitpp::assert_true("Layout succeeded", hr == S_OK);
			// At this point we have a single lazy box containing all 10 paragraphs.
			VwLazyBox * plzb = dynamic_cast<VwLazyBox *>(m_qrootb->FirstBox());
			unitpp::assert_true("One lazy box", plzb != NULL);
			unitpp::assert_eq("10 paras all in lazy box", 10, plzb->CItems());
			// Try expanding various things, then restoring the original list.
			MakeNthParaReal(0, m_rghvoParas[0], "first");
			RestoreOriginalList("first");
			MakeNthParaReal(kcPara - 1, m_rghvoParas[kcPara - 1], "last");
			RestoreOriginalList("last");
			MakeNthParaReal(5, m_rghvoParas[5], "fifth");
			RestoreOriginalList("fifth");
			MakeNthParaReal(0, m_rghvoParas[0], "first try 22");
			MakeNthParaReal(kcPara - 1, m_rghvoParas[kcPara - 1], "last try 2");
			MakeNthParaReal(5, m_rghvoParas[5], "fifth try 2");
			MakeNthParaReal(6, m_rghvoParas[6], "sixth");
			RestoreOriginalList("first, last, and middle");
			// Try a variety of deletions, insertions, and replacements.
			Restore3To7Lazy();
			TestReplace(0, 0, 1); // delete first object
			Restore3To7Lazy();
			TestReplace(0, 1, 0); // insert extra object at start.
			Restore3To7Lazy();
			TestReplace(2, 0, 1); // delete object right before lazy box
			Restore3To7Lazy();
			TestReplace(2, 3, 0); // insert 3 objects one place before lazy box
			Restore3To7Lazy();
			TestReplace(3, 0, 2); // delete first 2 objects in lazy box
			Restore3To7Lazy();
			TestReplace(3, 2, 0); // insert 2 objects at start of lazy box
			Restore3To7Lazy();
			TestReplace(4, 0, 2); // delete 2 in middle
			Restore3To7Lazy();
			TestReplace(4, 1, 0); // insert 1 in middle
			Restore3To7Lazy();
			TestReplace(7, 0, 1); // delete last object in lazy
			Restore3To7Lazy();
			TestReplace(8, 3, 0); // insert 3 objects right at end
			Restore3To7Lazy();
			TestReplace(kcPara - 1, 0, 1); // delete very last object
			Restore3To7Lazy();
			TestReplace(kcPara, 1, 0); // insert object at very end
			Restore3To7Lazy();
			TestReplace(3, 0, 5); // delete exactly the 5 objects in our lazy box
			Restore3To7Lazy();
			TestReplace(2, 0, 6); // delete lazy and one more before
			Restore3To7Lazy();
			TestReplace(3, 0, 6); // delete lazy and one more after
			Restore3To7Lazy();
			TestReplace(2, 0, 7); // delete lazy and one before AND after
			Restore3To7Lazy();
			TestReplace(3, 2, 5); // replace all of lazy with two others
			Restore3To7Lazy();
			TestReplace(2, 2, 4); // replace preceding and part of lazy with 2
			Restore3To7Lazy();
			TestReplace(0, 2, 5); // replace all of one lazy box, one para, and part of another
			Restore3To7Lazy();
			try{
				// This will throw because we are trying to replace more paragraphs than we have.
				TestReplace(6, 2, 5); // replace part of lazy, one para, part of another, in middle.
				unitpp::assert_eq("Delete more than exists should fail", true, false);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("Delete more than exists", E_INVALIDARG, thr.Result());
			}
			Restore3To7Lazy();
			TestReplace(6, 2, 4); // replace part of lazy, one para, part of another, in middle.
			Restore3To7Lazy();
			TestReplace(0, 1, 1); // replace first object, don't change length
			Restore3To7Lazy();
			TestReplace(5, 2, 2); // replace in middle, same length
		}

		// Replace cDel objects at index iMin with cIns objects (up to kcParaExtra).
		// Issue the corresponding PropChanged.
		void TestReplace(int iMin, int cIns, int cDel)
		{
			CheckHr(m_qcda->CacheReplace(hvoRoot, kflidStText_Paragraphs, iMin, iMin + cDel, m_rghvoParas + kcPara, cIns));
			CheckHr(m_qsda->PropChanged(NULL, kpctNotifyAll, hvoRoot, kflidStText_Paragraphs, iMin, cIns, cDel));

			HVO rghvo[kcPara + kcParaExtra]; // the current value
			int chvo;
			CheckHr(m_qsda->VecProp(hvoRoot, kflidStText_Paragraphs, kcPara + kcParaExtra, &chvo, rghvo));
			int ihvo = 0;
			for (VwBox * pbox = m_qrootb->FirstBox(); pbox; pbox = pbox->NextOrLazy())
			{
				VwLazyBox * plzb = dynamic_cast<VwLazyBox *>(pbox);
				if (plzb)
				{
					int chvoLazy = plzb->CItems();
					if (plzb->MinObjIndex() != ihvo)
					{
						StrAnsi sta;
						sta.Format("Replace %d items at index %d with %d: lazy box has wrong MinObjIndex", cDel, iMin, cIns);
						unitpp::assert_eq(sta.Chars(), ihvo, plzb->MinObjIndex());
					}
					for (int i = 0; i < chvoLazy; i++)
					{
						if (plzb->NthItem(i) != rghvo[i + ihvo])
						{
							StrAnsi sta;
							sta.Format("Replace %d items at index %d with %d: lazy box has wrong object at %d", cDel, iMin, cIns, i);
							unitpp::assert_eq(sta.Chars(), rghvo[i + ihvo], plzb->NthItem(i));
						}
					}

					ihvo += chvoLazy;
				}
				else
				{
					// Find the (one and only) notifier for rghvo[ihvo]
					// which is also the only level-2 notifier for this paragraph;
					NotifierVec vpanote;
					m_qrootb->GetNotifiers(pbox, vpanote);
					VwNotifier * pnoteBox = NULL;
					for (int i = 0; i < vpanote.Size(); i++)
					{
						VwNotifier * pnote = dynamic_cast<VwNotifier *>(vpanote[i].Ptr());
						if (pnote->Level() == 2)
						{
							pnoteBox = pnote;
							break;
						}
					}
					if (pnoteBox == NULL || pnoteBox->ObjectIndex() != ihvo || pnoteBox->Object() != rghvo[ihvo])
					{
						StrAnsi sta;
						sta.Format("Replace %d items at index %d with %d: no notifier found for object %d at index %d",
							cDel, iMin, cIns, ihvo, rghvo[ihvo]);
						unitpp::assert_true(sta.Chars(), pnoteBox != NULL);
						sta.Format("Replace %d items at index %d with %d: notifier for object %d at index %d has wrong index",
							cDel, iMin, cIns, ihvo, rghvo[ihvo]);
						unitpp::assert_eq(sta.Chars(), ihvo, pnoteBox->ObjectIndex());
						sta.Format("Replace %d items at index %d with %d: notifier for object %d at index %d has wrong object",
							cDel, iMin, cIns, ihvo, rghvo[ihvo]);
						unitpp::assert_eq(sta.Chars(), rghvo[ihvo], pnoteBox->Object());
					}
					ihvo++;
				}
			}
		}

		// this sets up a situation in which there is a lazy box that extends from original paragraphs 3 to 7.
		// It returns that lazy box.
		void Restore3To7Lazy()
		{
			RestoreOriginalList("3 to 7");
			MakeNthParaReal(2, m_rghvoParas[0], "first try 22");
			MakeNthParaReal(8, m_rghvoParas[6], "sixth");
			//VwBox * pboxFirst = m_qrootb->FirstBox(); // Surviving lazy box for item 0, 1
			//VwBox * pbox2 = pboxFirst->NextOrLazy(); // Real box for para 2
			//VwBox * plzb = dynamic_cast<VwLazyBox *>(pbox2->NextOrLazy());
			//unitpp::assert_true("3 to 7 made lazy box", plzb != NULL);
			//return plzb;
		}

		// Make the indicated paragraph a real one. Check for success and the right hvo.
		void MakeNthParaReal(int ipara, HVO hvoExpected, const char * pmsg)
		{
			VwSelLevInfo rgvsli[1];
			rgvsli[0].ihvo = ipara;
			rgvsli[0].tag = kflidStText_Paragraphs;
			rgvsli[0].cpropPrevious = 0;
			IVwSelectionPtr qsel;
			try{
				CheckHr(m_qrootb->MakeTextSelInObj(0, 1, rgvsli, 0, NULL,
					false, false, false, true, false, &qsel));
			}
			catch(Throwable& thr){
				StrAnsi sta;
				sta.Format("MakeTextSelInObj failed in test %s (index %d)", pmsg, ipara);
				unitpp::assert_eq(sta.Chars(), S_OK, thr.Result());
			}
		}

		// Replace the complete list with itself, which should delete all stuff we expanded and
		// restore us to a single lazy box with 10 items.
		void RestoreOriginalList(const char * pmsg)
		{
			// Get the current size of the property to use in PropChanged
			int cpara;
			CheckHr(m_qsda->get_VecSize(hvoRoot, kflidStText_Paragraphs, &cpara));
			// Restore the original list
			CheckHr(m_qcda->CacheVecProp(hvoRoot, kflidStText_Paragraphs, m_rghvoParas, kcPara));
			// Tell anyone who cares that it changed...this regenerates the entire property
			// and should make a new lazy box
			CheckHr(m_qsda->PropChanged(NULL, kpctNotifyAll, hvoRoot, kflidStText_Paragraphs, 0, kcPara, cpara));
			// Make sure it did, and that it has the correct number of objects.
			VwLazyBox * plzb = dynamic_cast<VwLazyBox *>(m_qrootb->FirstBox());
			if (plzb == NULL || plzb->CItems() != kcPara)
			{
				StrAnsi sta;
				sta.Format("Restore list %s did not produce a lazy box", pmsg);
				unitpp::assert_true(sta.Chars(), plzb != NULL);
				sta.Format("Restore list %s did not produce %d items", pmsg, kcPara);
				unitpp::assert_eq(sta.Chars(), kcPara, plzb->CItems());
			}
			// Finally verify that it has the right list of objects (the value of the property
			// it is displaying lazily).
			for (int i = 0; i < kcPara; i++)
			{
				if (plzb->NthItem(i) != m_rghvoParas[i])
				{
					StrAnsi sta;
					sta.Format("Restore list %s made a lazy box with the wrong object at index %d", pmsg, i);
					unitpp::assert_eq(sta.Chars(), m_rghvoParas[i], plzb->NthItem(i));
				}
			}
		}

		// Verify that there is a notifier for the specified hvo where pbox is the
		// first box of property iprop which has the specified tag. pbox must not be the root.
		void VerifyNotifier(VwBox * pbox, HVO hvo, PropTag tag, int iprop)
		{
			NotifierVec vpanote;
			pbox->Container()->GetNotifiers(pbox, vpanote);
			for (int inote = 0; inote < vpanote.Size(); inote++)
			{
				VwNotifier * pnote = dynamic_cast<VwNotifier *>(vpanote[inote].Ptr());
				if (!pnote)
					continue; // only interested in real notifiers.
				if (pnote->Object() != hvo)
					continue;
				int cprop = pnote->CProps();
				if (iprop >= cprop)
					continue;
				if (pnote->Boxes()[iprop] != pbox)
					continue;
				if (pnote->Tags()[iprop] != tag)
					continue;
				return;
			}
			unitpp::assert_true("failed to find expected notifier", false);
		}

	public:
		TestNotifier();

		virtual void Setup()
		{
			CreateTestWritingSystemFactory();
			m_qcda.CreateInstance(CLSID_VwCacheDa);
			CheckHr(m_qcda->QueryInterface(IID_ISilDataAccess, (void **)&m_qsda));
			CheckHr(m_qsda->putref_WritingSystemFactory(g_qwsf));

			m_qtsf.CreateInstance(CLSID_TsStrFactory);
			IVwRootBoxPtr qrootb;
			// When we create the root box with CreateInstance, it is created by the actual
			// views DLL. This results in a heap validation failure: some memory allocated
			// on the Views DLL heap gets freed by a method that is somehow linke as part of the
			// test program heap. (Each link that includes the C runtime memory allocation
			// code creates a separate heap.) By calling CreateCom directly, the root box
			// is created using the copy of the code linked into the test program, and all
			// memory allocation and deallocation takes place in the test program's copy of
			// the C runtime.
			//qrootb.CreateInstance(CLSID_VwRootBox);
			VwRootBox::CreateCom(NULL, IID_IVwRootBox, (void **) &qrootb);

			m_qrootb = dynamic_cast<VwRootBox *>(qrootb.Ptr());
			m_hdc = 0; // So we know not to release it if something goes wrong.
			m_qvg32.CreateInstance(CLSID_VwGraphicsWin32);
			m_hdc = GetTestDC();
			CheckHr(m_qvg32->Initialize(m_hdc));
			CheckHr(m_qrootb->putref_DataAccess(m_qsda));
			m_qdrs.Attach(NewObj DummyRootSite());
			m_rcSrc = Rect(0, 0, 96, 96);
			m_qdrs->SetRects(m_rcSrc, m_rcSrc);
			m_qdrs->SetGraphics(m_qvg32);
			CheckHr(m_qrootb->SetSite(m_qdrs));
		}
		virtual void Teardown()
		{
			if (m_qvg32)
			{
				m_qvg32->ReleaseDC();
				m_qvg32.Clear();
			}
			if (m_hdc)
				ReleaseTestDC(m_hdc);
			if (m_qrootb)
			{
				m_qrootb->Close();
				m_qrootb.Clear();
			}
			m_qtsf.Clear();
			m_qsda.Clear();
			m_qcda.Clear();
			m_qvc.Clear();
			m_qdrs.Clear();
			CloseTestWritingSystemFactory();
		}

		IVwCacheDaPtr m_qcda;
		ISilDataAccessPtr m_qsda;
		ITsStrFactoryPtr m_qtsf;
		VwRootBoxPtr m_qrootb;
		IVwGraphicsWin32Ptr m_qvg32;
		HDC m_hdc;
		IVwViewConstructorPtr m_qvc;
		DummyRootSitePtr m_qdrs;
		Rect m_rcSrc;
		HVO m_rghvoParas[kcPara + kcParaExtra]; // original test 'paragraphs'
	};
}

#endif /*TestNotifier_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
