/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestVwSync.h
Responsibility:
Last reviewed:

	Unit tests for the VwSync class (and the synchronization process generally).
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestVwSync_H_INCLUDED
#define TestVwSync_H_INCLUDED

#pragma once

#include "testViews.h"
#include "BasicVc.h"
#include "TestLazyBox.h"

namespace TestViews
{
#define kdympBorder 3000
#define khvoPara1 1
#define khvoPara2 2
#define khvoPara3 3
#define khvoPara4 4
#define khvoRootText 101
#define kflidFake			  999
#define kfragRoot			 1001
#define kfragPara			 1002
#define kfragFake			 1003
#define kfragRootLazy		 1004
#define kfragRootWithBorder  1005
#define kfragFakeWithBorder  1006
#define kfragRootWithNumbers 1007
#define kfragFakeWithNumbers 1008

	class ParasVc : public BasicVc
	{
	public:
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case kfragRootLazy: // the root; display the paras of the StText lazily.
				pvwenv->AddLazyVecItems(kflidStText_Paragraphs, this, kfragPara);
				break;
			case kfragRoot: // the root; display the paras of the StText.
				pvwenv->AddObjVecItems(kflidStText_Paragraphs, this, kfragPara);
				break;
			case kfragPara: // StTxtPara, display contents
				pvwenv->AddStringProp(kflidStTxtPara_Contents, this);
				break;
			}
			return S_OK;
		}
	};

	class OtherPropVc : public BasicVc
	{
	public:
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case kfragRootLazy: // the root; display the paras of the StText.
				pvwenv->AddLazyVecItems(kflidStText_Paragraphs, this, kfragFake);
				break;
			case kfragRootWithBorder: // the root; display the paras of the StText.
				pvwenv->AddLazyVecItems(kflidStText_Paragraphs, this, kfragFakeWithBorder);
				break;
			case kfragRootWithNumbers: // the root; display the paras of the StText.
				pvwenv->AddLazyVecItems(kflidStText_Paragraphs, this, kfragFakeWithNumbers);
				break;
			case kfragRoot: // the root; display the paras of the StText.
				pvwenv->AddObjVecItems(kflidStText_Paragraphs, this, kfragFake);
				break;
			case kfragFake: // StTxtPara, display contents
				pvwenv->AddStringProp(kflidFake, this);
				break;
			case kfragFakeWithBorder: // StTxtPara, display contents with a border around
				pvwenv->put_IntProperty(ktptBorderTop, ktpvMilliPoint, kdympBorder);
				pvwenv->put_IntProperty(ktptBorderBottom, ktpvMilliPoint, kdympBorder);
				pvwenv->put_IntProperty(ktptBorderLeading, ktpvMilliPoint, kdympBorder);
				pvwenv->put_IntProperty(ktptBorderTrailing, ktpvMilliPoint, kdympBorder);
				pvwenv->AddStringProp(kflidFake, this);
				break;
			case kfragFakeWithNumbers: // StTxtPara, display numbered contents
				pvwenv->put_IntProperty(ktptBulNumScheme, ktpvEnum, kvbnArabic);
				pvwenv->put_IntProperty(ktptBulNumStartAt, ktpvDefault, INT_MIN);
				pvwenv->AddStringProp(kflidFake, this);
				break;
			}
			return S_OK;
		}
	};

	class TestVwSync : public unitpp::suite
	{
	public:
		TestVwSync();

		// Verify that corresponding boxes in the two roots are aligned.
		// Also that the two roots have the same overall size.
		void verifyAlignment(VwBox * pbox1, VwBox * pbox2, int allowableGap = 0,
			VwBox * pboxPrev1 = NULL, VwBox * pboxPrev2 = NULL)
		{
			// Verify that the views are the same size.
			// No longer required, because if the last boxes in each view are different sizes
			// or bottom margins, the overall view height can difer slightly.
			//int dypFirstViewAlignedHeight;
			//prootb1->get_Height(&dypFirstViewAlignedHeight);
			//int dyp2ndViewAlignedHeight;
			//prootb2->get_Height(&dyp2ndViewAlignedHeight);
			//unitpp::assert_true("aligned views same size",
			//	dyp2ndViewAlignedHeight == dypFirstViewAlignedHeight);

			unitpp::assert_true("should have matching boxes", pbox1 && pbox2);
			unitpp::assert_eq("boxes should be aligned", pbox1->Top(), pbox2->Top());
			if (pboxPrev1)
			{
				int gap1 = pbox1->Top() - pboxPrev1->Bottom();
				int gap2 = pbox2->Top() - pboxPrev2->Bottom();
				unitpp::assert_true("Box 1 should not overlap previous box",
					gap1 >= 0);
				unitpp::assert_true("Box 2 should not overlap previous box",
					gap2 >= 0);
				unitpp::assert_true("Top of Boxes too far down (too much gap)",
					Min(gap1, gap2) <= allowableGap);
			}
			VwPileBox * ppile1 = dynamic_cast<VwPileBox *>(pbox1);
			VwPileBox * ppile2 = dynamic_cast<VwPileBox *>(pbox2);
			static uint s_paraHeight = 0;
			if (dynamic_cast<VwParagraphBox *>(pbox1))
				s_paraHeight = (uint)Max(pbox1->Height(), pbox2->Height());

			if ((ppile1 && !ppile2) || (!ppile1 && ppile2))
				unitpp::assert_true("Group box found without match", false);

			if (ppile1)
			{
				pbox1 = ppile1->FirstBox();
				pbox2 = ppile2->FirstBox();
				pboxPrev1 = pboxPrev2 = NULL;
				while (pbox1)
				{
					verifyAlignment(pbox1, pbox2, allowableGap, pboxPrev1, pboxPrev2);
					pboxPrev1 = pbox1;
					pboxPrev2 = pbox2;
					pbox1 = pbox1->NextOrLazy();
					pbox2 = pbox2->NextOrLazy();
				}
				unitpp::assert_true("Bottoms of last boxes should never be different by more than a paragraph height.",
					Abs(pboxPrev1->Bottom() - pboxPrev2->Bottom()) <= s_paraHeight);
				unitpp::assert_true("Bottoms of containers should never be different by more than a paragraph height.",
					Abs(ppile1->Bottom() - ppile2->Bottom()) <= s_paraHeight);
				unitpp::assert_true("found extra boxes on first view", pbox1 == NULL);
				unitpp::assert_true("found extra boxes on second view", pbox2 == NULL);
			}
		}

		void CreateTestData()
		{
			// Create test data in a temporary cache.
			// First make some generic objects.
			ITsStringPtr qtss;
			// Now make four paragraphs, each with regular contents and kflidFake.
			m_stuPara1.Append(L"This is the first test paragraph");
			m_qtsf->MakeString(m_stuPara1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoPara1, kflidStTxtPara_Contents, qtss);
			StrUni stuFake1(L"This is the first fake back trans");
			m_qtsf->MakeString(stuFake1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoPara1, kflidFake, qtss);

			StrUni stuPara2(L"Short para");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoPara2, kflidStTxtPara_Contents, qtss);
			m_stuFake2.Append(L"This is a much longer back translation, so we can see what happens when the"
				L" back translation is much longer than the original. Hopefully this is long enough"
				L" to take up more lines than 'Short para'");
			m_qtsf->MakeString(m_stuFake2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoPara2, kflidFake, qtss);

			StrUni stuPara3(L"This is the third test paragraph, which is quite a bit longer than the others. "
				L"In fact it goes on for several lines, so it should make a good alignment test.");
			m_qtsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoPara3, kflidStTxtPara_Contents, qtss);
			m_stuFake3.Append(L"This is the third fake back trans");
			m_qtsf->MakeString(m_stuFake3.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoPara3, kflidFake, qtss);

			StrUni stuPara4(L"Another short para");
			m_qtsf->MakeString(stuPara4.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoPara4, kflidStTxtPara_Contents, qtss);
			StrUni stuFake4(L"This is the fourth fake back trans");
			m_qtsf->MakeString(stuFake4.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoPara4, kflidFake, qtss);

			// Now make them the paragraphs of an StText.
			HVO rghvo[] = {khvoPara1, khvoPara2, khvoPara3, khvoPara4};
			m_qcda->CacheVecProp(m_hvoRoot, kflidStText_Paragraphs, rghvo, isizeof(rghvo)/isizeof(HVO));
		}

		void testSync_NonLazy()
		{
			ITsStringPtr qtss;

			// Create test data in a temporary cache.
			CreateTestData();

			// Now make the root boxes and view constructors and Graphics object.
			m_qrootb1->SetRootObject(m_hvoRoot, m_qvc1, kfragRoot, NULL);
			m_qrootb2->SetRootObject(m_hvoRoot, m_qvc2, kfragRoot, NULL);

			// Lay out the first one.
			HRESULT hr = m_qrootb1->Layout(m_qvg32, 300);
			unitpp::assert_true("First layout succeeded", hr == S_OK);

			// Save overall display size and positions of paragraphs.
			int dypNaturalHeightView1;
			m_qrootb1->get_Height(&dypNaturalHeightView1);

			// Lay out the second one.
			hr = m_qrootb2->Layout(m_qvg32, 300);
			unitpp::assert_true("Second layout succeeded", hr == S_OK);

			// Verify that first view size increased.
			int dypFirstViewAlignedHeight;
			m_qrootb1->get_Height(&dypFirstViewAlignedHeight);
			unitpp::assert_true("Aligned layout increased first view height",
				dypNaturalHeightView1 < dypFirstViewAlignedHeight);

			// Verify that paragraphs in both views are aligned.
			VwRootBox * prootb1 = dynamic_cast<VwRootBox *>(m_qrootb1.Ptr());
			VwRootBox * prootb2 = dynamic_cast<VwRootBox *>(m_qrootb2.Ptr());
			verifyAlignment(prootb1, prootb2);

			// Replace a string with a longer one and verify both views get longer.
			StrUni stuLongRep(L"This is a long paragraph which should force the views to get longer "
				L"when I put it into the first column opposite a short string.");
			m_qtsf->MakeString(stuLongRep.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoPara1, kflidStTxtPara_Contents, qtss);
			m_qsda->PropChanged(NULL, kpctNotifyAll, khvoPara1, kflidStTxtPara_Contents,
				0, stuLongRep.Length(), m_stuPara1.Length());
			verifyAlignment(prootb1, prootb2);

			int dypHeightAfterMakeLonger;
			m_qrootb1->get_Height(&dypHeightAfterMakeLonger);
			unitpp::assert_true("Inserting longer string increased view height",
				dypFirstViewAlignedHeight < dypHeightAfterMakeLonger);

			// Todo: Replace a long string with a shorter one and verify both views get shorter.
			StrUni stuShortRep(L"This is short.");
			m_qtsf->MakeString(stuShortRep.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoPara2, kflidFake, qtss);
			m_qsda->PropChanged(NULL, kpctNotifyAll, khvoPara2, kflidFake,
				0, stuShortRep.Length(), m_stuFake2.Length());
			verifyAlignment(prootb1, prootb2);

			int dypHeightAfterMakeShorter;
			m_qrootb1->get_Height(&dypHeightAfterMakeShorter);
			unitpp::assert_true("Replacing with shorter string decreased view height",
				dypHeightAfterMakeShorter < dypHeightAfterMakeLonger);

			// Todo: Replace a string whose pair is quite long with one not quite as long
			// and verify overall size does NOT change.
			StrUni stuMedRep(L"This is a fairly long string, more than the old fake3, but not as "
				L" long as para 3.");
			m_qtsf->MakeString(stuMedRep.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoPara3, kflidFake, qtss);
			m_qsda->PropChanged(NULL, kpctNotifyAll, khvoPara3, kflidFake,
				0, stuMedRep.Length(), m_stuFake3.Length());
			verifyAlignment(prootb1, prootb2);

			int dypHeightAfterThirdRep;
			m_qrootb1->get_Height(&dypHeightAfterThirdRep);
			unitpp::assert_true("Replacing shorter string parallel to long doesn't change height",
				dypHeightAfterMakeShorter == dypHeightAfterThirdRep);
		}

		void testSync_Lazy()
		{
			ITsStringPtr qtss;

			// Create test data in a temporary cache.
			CreateTestData();

			// Now make the root boxes and view constructors and Graphics object.
			m_qrootb1->SetRootObject(m_hvoRoot, m_qvc1, kfragRoot, NULL);
			m_qrootb2->SetRootObject(m_hvoRoot, m_qvc2, kfragRoot, NULL);

			m_qrootb1->Layout(m_qvg32, 300);
			m_qrootb2->Layout(m_qvg32, 300);

			// ******** Now try some tests with laziness involved. ********
			// It's important to get back to having a LONG paragraph for the second BT.
			m_qrootb1->Reconstruct();
			m_qrootb2->Reconstruct();
			int dypNonLazyHeight;
			m_qrootb1->get_Height(&dypNonLazyHeight);

			m_qrootb1->SetRootObject(m_hvoRoot, m_qvc1, kfragRootLazy, NULL);
			m_qrootb2->SetRootObject(m_hvoRoot, m_qvc2, kfragRootLazy, NULL);

			VwRootBox * prootb1 = dynamic_cast<VwRootBox *>(m_qrootb1.Ptr());
			VwRootBox * prootb2 = dynamic_cast<VwRootBox *>(m_qrootb2.Ptr());

			// Lay them both out and verify 'alignment' of the one lazy box in each.
			HRESULT hr = m_qrootb1->Layout(m_qvg32, 300);
			unitpp::assert_true("First lazy layout succeeded", hr == S_OK);
			hr = m_qrootb2->Layout(m_qvg32, 300);
			unitpp::assert_true("Second lazy layout succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2);

			// A selection in one of the paragraphs will force some expansion.
			// We'll make an IP after the first character in the second paragraph
			// of the first view.
			VwSelLevInfo vsli;
			vsli.tag = kflidStText_Paragraphs; // selection will be in this property
			vsli.cpropPrevious = 0; // specifically the first (and only) occurrence of it
			vsli.ihvo = 1; // we will select in the second paragraph.
			IVwSelectionPtr qsel;
			hr = prootb1->MakeTextSelection(
				0, // only one root object in this view
				1, // need one VwSelLevInfo
				&vsli, // simulate an array of length 1
				kflidStTxtPara_Contents, // this property of the StTxtPara
				0, // first and only occurrence
				1, 1, // IP at position 1
				0, // ws is irrelevant for non-multlingual property.
				true, // arbitrarily choose to associate with previous char
				-1, // not spanning paragraphs
				NULL, // Don't want to override props to use on typing.
				false, // Don't need to install in root, which would produce drawing
				& qsel); // get the result (though we don't use it)
			unitpp::assert_true("Making first sel succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2);

			// Try again in the other root, last paragraph
			vsli.ihvo = 3; // we will select in the fourth paragraph.
			hr = prootb2->MakeTextSelection(
				0, // only one root object in this view
				1, // need one VwSelLevInfo
				&vsli, // simulate an array of length 1
				kflidFake, // this property of the StTxtPara
				0, // first and only occurrence
				1, 1, // IP at position 1
				0, // ws is irrelevant for non-multlingual property.
				true, // arbitrarily choose to associate with previous char
				-1, // not spanning paragraphs
				NULL, // Don't want to override props to use on typing.
				false, // Don't need to install in root, which would produce drawing
				& qsel); // get the result (though we don't use it)
			unitpp::assert_true("Making 2nd sel succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2);

			// Now prepare to draw the first root, which with a large enough clip rect
			// will force everything to expand (since the m_hdc is for the whole screen).
			VwPrepDrawResult vpdr;
			hr = prootb1->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &vpdr);
			unitpp::assert_true("PrepareToDraw succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2);

			int dypLazyHeight;
			m_qrootb1->get_Height(&dypLazyHeight);
			unitpp::assert_eq("Lazy view expands to same height as original",
				dypNonLazyHeight, dypLazyHeight);
		}

		void testSync_LazyWithBorder()
		{
			ITsStringPtr qtss;

			// Create test data in a temporary cache.
			CreateTestData();

			int dpiY;
			m_qvg32->get_YUnitsPerInch(&dpiY);

			m_qrootb1->SetRootObject(m_hvoRoot, m_qvc1, kfragRootLazy, NULL);
			m_qrootb2->SetRootObject(m_hvoRoot, m_qvc2, kfragRootWithBorder, NULL);

			VwRootBox * prootb1 = dynamic_cast<VwRootBox *>(m_qrootb1.Ptr());
			VwRootBox * prootb2 = dynamic_cast<VwRootBox *>(m_qrootb2.Ptr());

			// Lay them both out and verify 'alignment' of the one lazy box in each.
			HRESULT hr = m_qrootb1->Layout(m_qvg32, 300);
			unitpp::assert_true("First lazy layout succeeded", hr == S_OK);
			hr = m_qrootb2->Layout(m_qvg32, 300);
			unitpp::assert_true("Second lazy layout succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2, kdympBorder * dpiY / 72000);

			// A selection in one of the paragraphs will force some expansion.
			// We'll make an IP after the first character in the second paragraph
			// of the first view.
			VwSelLevInfo vsli;
			vsli.tag = kflidStText_Paragraphs; // selection will be in this property
			vsli.cpropPrevious = 0; // specifically the first (and only) occurrence of it
			vsli.ihvo = 1; // we will select in the second paragraph.
			IVwSelectionPtr qsel;
			hr = prootb1->MakeTextSelection(
				0, // only one root object in this view
				1, // need one VwSelLevInfo
				&vsli, // simulate an array of length 1
				kflidStTxtPara_Contents, // this property of the StTxtPara
				0, // first and only occurrence
				1, 1, // IP at position 1
				0, // ws is irrelevant for non-multlingual property.
				true, // arbitrarily choose to associate with previous char
				-1, // not spanning paragraphs
				NULL, // Don't want to override props to use on typing.
				false, // Don't need to install in root, which would produce drawing
				& qsel); // get the result (though we don't use it)
			unitpp::assert_true("Making first sel succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2, kdympBorder * dpiY / 72000);

			// Try again in the other root, last paragraph
			vsli.ihvo = 3; // we will select in the fourth paragraph.
			hr = prootb2->MakeTextSelection(
				0, // only one root object in this view
				1, // need one VwSelLevInfo
				&vsli, // simulate an array of length 1
				kflidFake, // this property of the StTxtPara
				0, // first and only occurrence
				1, 1, // IP at position 1
				0, // ws is irrelevant for non-multlingual property.
				true, // arbitrarily choose to associate with previous char
				-1, // not spanning paragraphs
				NULL, // Don't want to override props to use on typing.
				false, // Don't need to install in root, which would produce drawing
				& qsel); // get the result (though we don't use it)
			unitpp::assert_true("Making 2nd sel succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2);

			// Now prepare to draw the first root, which with a large enough clip rect
			// will force everything to expand (since the m_hdc is for the whole screen).
			VwPrepDrawResult vpdr;
			hr = prootb1->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &vpdr);
			unitpp::assert_true("PrepareToDraw succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2);
		}

		void testSync_LazyWithNumber()
		{
			ITsStringPtr qtss;

			// Create test data in a temporary cache.
			CreateTestData();

			m_qrootb1->SetRootObject(m_hvoRoot, m_qvc1, kfragRootLazy, NULL);
			m_qrootb2->SetRootObject(m_hvoRoot, m_qvc2, kfragRootWithNumbers, NULL);

			VwRootBox * prootb1 = dynamic_cast<VwRootBox *>(m_qrootb1.Ptr());
			VwRootBox * prootb2 = dynamic_cast<VwRootBox *>(m_qrootb2.Ptr());

			// Lay them both out and verify 'alignment' of the one lazy box in each.
			HRESULT hr = m_qrootb1->Layout(m_qvg32, 300);
			unitpp::assert_true("First lazy layout succeeded", hr == S_OK);
			hr = m_qrootb2->Layout(m_qvg32, 300);
			unitpp::assert_true("Second lazy layout succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2);

			// A selection in one of the paragraphs will force some expansion.
			// We'll make an IP after the first character in the second paragraph
			// of the first view.
			VwSelLevInfo vsli;
			vsli.tag = kflidStText_Paragraphs; // selection will be in this property
			vsli.cpropPrevious = 0; // specifically the first (and only) occurrence of it
			vsli.ihvo = 1; // we will select in the second paragraph.
			IVwSelectionPtr qsel;
			hr = prootb1->MakeTextSelection(
				0, // only one root object in this view
				1, // need one VwSelLevInfo
				&vsli, // simulate an array of length 1
				kflidStTxtPara_Contents, // this property of the StTxtPara
				0, // first and only occurrence
				1, 1, // IP at position 1
				0, // ws is irrelevant for non-multlingual property.
				true, // arbitrarily choose to associate with previous char
				-1, // not spanning paragraphs
				NULL, // Don't want to override props to use on typing.
				false, // Don't need to install in root, which would produce drawing
				& qsel); // get the result (though we don't use it)
			unitpp::assert_true("Making first sel succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2);

			// Try again in the other root, last paragraph
			vsli.ihvo = 3; // we will select in the fourth paragraph.
			hr = prootb2->MakeTextSelection(
				0, // only one root object in this view
				1, // need one VwSelLevInfo
				&vsli, // simulate an array of length 1
				kflidFake, // this property of the StTxtPara
				0, // first and only occurrence
				1, 1, // IP at position 1
				0, // ws is irrelevant for non-multlingual property.
				true, // arbitrarily choose to associate with previous char
				-1, // not spanning paragraphs
				NULL, // Don't want to override props to use on typing.
				false, // Don't need to install in root, which would produce drawing
				& qsel); // get the result (though we don't use it)
			unitpp::assert_true("Making 2nd sel succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2);

			// Now prepare to draw the first root, which with a large enough clip rect
			// will force everything to expand (since the m_hdc is for the whole screen).
			VwPrepDrawResult vpdr;
			hr = prootb1->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &vpdr);
			unitpp::assert_true("PrepareToDraw succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2);
		}

		void testSync_LazyWithDivs()
		{
			ITsStringPtr qtss;
			ITsStringPtr qtssLeft;

			// Create test data in a temporary cache.
			HVO rghvoSec[16 * 40];
			TestLazyBox::CreateTestBooksWithSections(m_qcda, rghvoSec, kflidStTxtPara_Contents, 40);
			HVO rghvoFakeSec[16 * 40];
			TestLazyBox::CreateTestBooksWithSections(m_qcda, rghvoFakeSec, kflidFake, 40);

			// replace some of the paragraphs with longer ones
			StrUni stuPara(L"This is a much longer back translation, so we can see what happens when the"
				L" back translation is much longer than the original. Hopefully this is long enough"
				L" to take up more lines than the other paragraph.");
			m_qtsf->MakeString(stuPara.Bstr(), g_wsEng, &qtss);
			StrUni stuPara3(L"This is a test paragraph, which is quite a bit longer than the others. "
				L"In fact it goes on for several lines, so it should make a good alignment test.");
			m_qtsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtssLeft);
			// Create a longer paragraph in each section (1st paragraph in each section on the right
			// side, 3rd para on left side)
			for (int i = 0; i < 16; i++)
			{
//				m_qcda->CacheStringProp(khvoParaMin + i * 16, kflidStTxtPara_Contents, qtss);
				m_qcda->CacheStringProp(khvoParaMin + i * 16 + 2, kflidFake, qtssLeft);
			}

			// Create lots of longer paragraphs in each section of each book on the right side
			int paraHvo = khvoParaMin;
			for (int ib = 0; ib < 40; ib++)
			{
				for (int is = 0; is < 16; is++)
				{
					for (int ip = 0; ip < is; ip++)
					{
						m_qcda->CacheStringProp(paraHvo++, kflidStTxtPara_Contents, qtss);
					}
				}
			}

			m_qvc1.Clear();
			m_qvc2.Clear();
			m_qvc1.Attach(NewObj DummyVcBkSecParaDiv());
			m_qvc2.Attach(NewObj DummyVcBkSecParaDiv(kflidFake));

			m_qrootb1->SetRootObject(khvoScripture, m_qvc1, kfragScripture, NULL);
			m_qrootb2->SetRootObject(khvoScripture, m_qvc2, kfragScripture, NULL);

			VwRootBox * prootb1 = dynamic_cast<VwRootBox *>(m_qrootb1.Ptr());
			VwRootBox * prootb2 = dynamic_cast<VwRootBox *>(m_qrootb2.Ptr());

			// Lay them both out and verify 'alignment' of the one lazy box in each.
			HRESULT hr = m_qrootb1->Layout(m_qvg32, 300);
			unitpp::assert_true("First lazy layout succeeded", hr == S_OK);
			hr = m_qrootb2->Layout(m_qvg32, 300);
			unitpp::assert_true("Second lazy layout succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2);

			// set a scroll position at the top
			Rect rcDest = m_rcSrc;

			// Expand one screen full
			VwPrepDrawResult xpdr;
			hr = m_qrootb1->PrepareToDraw(m_qvg32, m_rcSrc, rcDest, &xpdr);
			verifyAlignment(prootb1, prootb2);

			// Try again 1/3 of the way down in the other root
			int rootHeight;
			prootb2->get_Height(&rootHeight);
			rcDest.Offset(0, -rootHeight / 3);

			// Expand one screen full
			hr = m_qrootb2->PrepareToDraw(m_qvg32, m_rcSrc, rcDest, &xpdr);
			verifyAlignment(prootb1, prootb2);

			// Try other places in the first root
			prootb1->get_Height(&rootHeight);
			rcDest = m_rcSrc;
			rcDest.Offset(0, -rootHeight / 2);
			hr = m_qrootb1->PrepareToDraw(m_qvg32, m_rcSrc, rcDest, &xpdr);
			verifyAlignment(prootb1, prootb2);

			for (int i = 0; i < 4; i++)
			{
				rcDest.Offset(0, -6000);
				hr = m_qrootb1->PrepareToDraw(m_qvg32, m_rcSrc, rcDest, &xpdr);
				verifyAlignment(prootb1, prootb2);
			}
		}

		void testSync_LazyWithDivsWidthChange()
		{
			ITsStringPtr qtss;
			ITsStringPtr qtssLeft;

			// Create test data in a temporary cache.
			HVO rghvoSec[16 * 40];
			TestLazyBox::CreateTestBooksWithSections(m_qcda, rghvoSec, kflidStTxtPara_Contents, 40);
			HVO rghvoFakeSec[16 * 40];
			TestLazyBox::CreateTestBooksWithSections(m_qcda, rghvoFakeSec, kflidFake, 40);

			// replace some of the paragraphs with longer ones
			StrUni stuPara(L"This is a much longer back translation, so we can see what happens when the"
				L" back translation is much longer than the original. Hopefully this is long enough"
				L" to take up more lines than the other paragraph.");
			m_qtsf->MakeString(stuPara.Bstr(), g_wsEng, &qtss);
			StrUni stuPara3(L"This is a test paragraph, which is quite a bit longer than the others. "
				L"In fact it goes on for several lines, so it should make a good alignment test.");
			m_qtsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtssLeft);
			// Create a longer paragraph in each section (1st paragraph in each section on the right
			// side, 3rd para on left side)
			for (int i = 0; i < 16; i++)
			{
//				m_qcda->CacheStringProp(khvoParaMin + i * 16, kflidStTxtPara_Contents, qtss);
				m_qcda->CacheStringProp(khvoParaMin + i * 16 + 2, kflidFake, qtssLeft);
			}

			// Create lots of longer paragraphs in each section of each book on the right side
			int paraHvo = khvoParaMin;
			for (int ib = 0; ib < 40; ib++)
			{
				for (int is = 0; is < 16; is++)
				{
					for (int ip = 0; ip < is; ip++)
					{
						m_qcda->CacheStringProp(paraHvo++, kflidStTxtPara_Contents, qtss);
					}
				}
			}

			m_qvc1.Clear();
			m_qvc2.Clear();
			m_qvc1.Attach(NewObj DummyVcBkSecParaDiv());
			m_qvc2.Attach(NewObj DummyVcBkSecParaDiv(kflidFake));

			m_qrootb1->SetRootObject(khvoScripture, m_qvc1, kfragScripture, NULL);
			m_qrootb2->SetRootObject(khvoScripture, m_qvc2, kfragScripture, NULL);

			VwRootBox * prootb1 = dynamic_cast<VwRootBox *>(m_qrootb1.Ptr());
			VwRootBox * prootb2 = dynamic_cast<VwRootBox *>(m_qrootb2.Ptr());

			// Lay them both out and verify 'alignment' of the one lazy box in each.
			HRESULT hr = m_qrootb1->Layout(m_qvg32, 300);
			unitpp::assert_true("First lazy layout succeeded", hr == S_OK);
			hr = m_qrootb2->Layout(m_qvg32, 100);
			unitpp::assert_true("Second lazy layout succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2);

			// set a scroll position at the top
			Rect rcDest = m_rcSrc;

			// Expand one screen full
			VwPrepDrawResult xpdr;
			int rootHeight;
			prootb1->get_Height(&rootHeight);
			rcDest.Offset(0, -rootHeight + 1000);
			hr = m_qrootb1->PrepareToDraw(m_qvg32, m_rcSrc, rcDest, &xpdr);
			verifyAlignment(prootb1, prootb2);

			// widen the first root (simulates hiding the other synched root)
			hr = m_qrootb1->Layout(m_qvg32, 400);
			unitpp::assert_true("Layout at new width succeeded", hr == S_OK);
			hr = m_qrootb2->Layout(m_qvg32, 100);
			unitpp::assert_true("Second lazy layout succeeded", hr == S_OK);
			verifyAlignment(prootb1, prootb2);
		}

		virtual void Setup()
		{
			m_hvoRoot = 1001;
			m_hdc = 0;
			CreateTestWritingSystemFactory();

			m_qtsf.CreateInstance(CLSID_TsStrFactory);

			m_qcda.CreateInstance(CLSID_VwCacheDa);
			m_qcda->QueryInterface(IID_ISilDataAccess, (void **)&m_qsda);
			m_qsda->putref_WritingSystemFactory(g_qwsf);

			m_qrootb1.CreateInstance(CLSID_VwRootBox);
			m_qrootb2.CreateInstance(CLSID_VwRootBox);

			// Initialize the graphics to use the display m_hdc...with the methods we call
			// it shouldn't do any actual drawing.
			m_qvg32.CreateInstance(CLSID_VwGraphicsWin32);
			m_hdc = GetTestDC();
			m_qvg32->Initialize(m_hdc);

			// Set the clip rectangle so that we have a defined one that the tests expects. This
			// is important when running from a service and probably also when running with
			// differing screen resolutions
			Rect clipRect(0, 0, 1680, 1050);
			m_qvg32->SetClipRect(&clipRect);

			// Create the two view constructors for the two views.
			m_qvc1.Attach(NewObj ParasVc());
			m_qvc2.Attach(NewObj OtherPropVc());

			// Initialize the root boxes with their data access object, roots, view constructors, etc.
			m_qrootb1->putref_DataAccess(m_qsda);
			m_qrootb2->putref_DataAccess(m_qsda);

			// Make a dummy root site which can provide the views with coordinate rects
			// and Graphics object but doesn't do much else. Initialize the views with it.
			DummyRootSitePtr qdrs;
			qdrs.Attach(NewObj DummyRootSite());
			m_rcSrc = Rect (0, 0, 96, 96);
			qdrs->SetRects(m_rcSrc, m_rcSrc);
			qdrs->SetGraphics(m_qvg32);
			m_qrootb1->SetSite(qdrs);
			m_qrootb2->SetSite(qdrs);

			// Make a synchronizer and hook the views to it.
			m_qsync.CreateInstance(CLSID_VwSynchronizer);
			m_qsync->AddRoot(m_qrootb1);
			m_qsync->AddRoot(m_qrootb2);
		}
		virtual void Teardown()
		{
			m_qsync.Clear();
			m_qvc1.Clear();
			m_qvc2.Clear();

			if (m_qvg32)
				m_qvg32->ReleaseDC();
			if (m_hdc != 0)
				ReleaseTestDC(m_hdc);
			m_qrootb1->Close();
			m_qrootb2->Close();

			CloseTestWritingSystemFactory();
			m_stuPara1.Clear();
			m_stuFake2.Clear();
			m_stuFake3.Clear();

			m_qtsf.Clear();
			m_qcda.Clear();
			m_qsda.Clear();
			m_qrootb1.Clear();
			m_qrootb2.Clear();
			m_qvg32.Clear();
		}

		HVO m_hvoRoot;
		ITsStrFactoryPtr m_qtsf;
		Rect m_rcSrc;
		IVwCacheDaPtr m_qcda;
		ISilDataAccessPtr m_qsda;
		IVwRootBoxPtr m_qrootb1;
		IVwRootBoxPtr m_qrootb2;
		IVwGraphicsWin32Ptr m_qvg32;
		HDC m_hdc;
		IVwViewConstructorPtr m_qvc1;
		IVwViewConstructorPtr m_qvc2;
		IVwSynchronizerPtr m_qsync;
		StrUni m_stuPara1;
		StrUni m_stuFake2;
		StrUni m_stuFake3;
	};
}

#endif /*TestVwSync_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
