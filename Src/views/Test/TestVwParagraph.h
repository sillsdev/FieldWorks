/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestVwParagaph.h
Responsibility:
Last reviewed:

	Unit tests for the VwParagraphBox class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestVwParagaph_H_INCLUDED
#define TestVwParagaph_H_INCLUDED

#pragma once

#include "testViews.h"

#define COMBINING_DIAERESIS L"\x0308" // cc 230
#define COMBINING_MACRON L"\x0304" // cc 230
#define A_WITH_DIAERESIS_AND_MACRON L"\x01DE"	// decomposes to 00C4 0304 and hence to
												// 0041 0308 0304
#define LATIN_SMALL_LETTER_E_WITH_ACUTE L"\x00E9"

#define khvoString1 998
#define khvoString2 999
#define khvoString3 997

#define kfragBase 50
#define kfragContents 51

// needs to be in the enchant namespace because it needs to be a friend of enchant::Dict
// and it's bad enough that class needs to know about the class name without trying to
// make it aware of the namespace TestViews.
namespace enchant
{
	class MockDict : public enchant::Dict
	{
		uint m_badLen;
	public:
		MockDict(int badLen) : Dict()
		{
			m_badLen = badLen;
		}
		virtual bool check (const std::string & utf8word) {
			return utf8word.length() != m_badLen;
		}

	};
}

namespace TestViews
{
	class NormalizeDummyVc : public DummyBaseVc
	{
	public:
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case kfragBase: // the root; display the subitems, all in one paragraph, twice over.
				pvwenv->OpenParagraph();
				pvwenv->AddObjVecItems(kflidStText_Paragraphs, this, kfragContents);
				pvwenv->CloseParagraph();
				pvwenv->OpenParagraph();
				pvwenv->AddObjVecItems(kflidStText_Paragraphs, this, kfragContents);
				pvwenv->CloseParagraph();
				break;
			case kfragContents: // StTxtPara, display contents with border.
				pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL);
				break;
			}
			return S_OK;
		}
	};

	class InnerPileDummyVc : public DummyBaseVc
	{
		// Enhance JohnT: make this methods smarter if we have to test on non-9-dpi screens.
		static int PixToMpX(int dx)
		{
			return MulDiv(dx, kdzmpInch, 96);
		}
		static int PixToMpY(int dy)
		{
			return MulDiv(dy, kdzmpInch, 96);
		}
		// add count rectangles, each of specified height and offset. They have an
		// arbitrary color (10) and width (5), all in 96ths of an inch
		void AddRects(IVwEnv* pvwenv, int count, int height, int offset, int color = 10)
		{
			for (int i = 0; i < count; i++)
			{
				pvwenv->AddSimpleRect(color, PixToMpX(5), PixToMpY(height), PixToMpY(offset));
			}

		}
	public:
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case kfragBase: // the root; display the subitems, all in one paragraph, twice over.
				{
					pvwenv->OpenParagraph();
					// Simulate a 'label' pile with seven items, all 15 pixels high, with baseline at 10.
					pvwenv->OpenInnerPile();
					AddRects(pvwenv, 7, 15, -5, 11);
					pvwenv->CloseInnerPile();

					// Add a non-inner-pile. This verifies alignment with other material. Baseline is 2, less
					// than any inner pile.
					pvwenv->AddSimpleRect(99, 5, PixToMpY(15), PixToMpY(-13));

					// Add second inner pile. All baselines are less than label row, verifying that
					// we can move boxes down. Basically, all boxes have a height of 14 and baseline of 9.
					pvwenv->OpenInnerPile();
					AddRects(pvwenv, 7, 14, -5, 12);
					pvwenv->CloseInnerPile();

					// Add third inner pile. In this one, there are 8 lines, four of them inside another
					// layer that is the third line of the outer pile. Natural baselines are the same
					// as the second pile
					pvwenv->OpenInnerPile();
					AddRects(pvwenv, 2, 14, -5, 13);
					pvwenv->OpenParagraph();
						pvwenv->OpenInnerPile();
						AddRects(pvwenv, 4, 14, -5, 14);
						pvwenv->CloseInnerPile();
					pvwenv->CloseParagraph();
					AddRects(pvwenv, 2, 14, -5, 15);
					pvwenv->CloseInnerPile();

					// Add fourth inner pile. In this one, there are 8 lines, four of them inside another
					// layer that is the fourth line of the outer pile. Two further complicate things,
					// it is a second inner pile of the line 4 paragraph that is four lines high.
					// Also, the first three lines are 16 pixels high; the fourth line (owing to the
					// second inner pile) is 18 high, the fifth 18 high; the others small.
					pvwenv->OpenInnerPile();
					AddRects(pvwenv, 3, 16, -5, 16);
					pvwenv->OpenParagraph();
						pvwenv->OpenInnerPile();
						AddRects(pvwenv, 3, 14, -5, 17);
						pvwenv->CloseInnerPile();
						pvwenv->OpenInnerPile();
						AddRects(pvwenv, 1, 16, -5, 18);
						AddRects(pvwenv, 1, 18, -5, 19);
						AddRects(pvwenv, 2, 10, -5, 20);
						pvwenv->CloseInnerPile();
					pvwenv->CloseParagraph();
					AddRects(pvwenv, 1, 10, -5, 21);
					pvwenv->CloseInnerPile();

					// Another non-inner-pile, this one with baseline 30, greater than any inner pile.
					// This therefore determines the actual baseline for the first row: 30.
					pvwenv->AddSimpleRect(98, 5, PixToMpY(15), PixToMpY(15));

					pvwenv->CloseParagraph();
				}
				break;
			}
			return S_OK;
		}
	};

#define kfragBaseLiteral 55
#define kfragBaseDouble 56
	// Test class for spelling check. Makes a nested div box containing a paragraph containing the string.
	// An alternative fragment inserts the string twice, first marked as not checkable.
	// Another alternative inserts a literal string.
	class NestedStringDummyVc : public DummyBaseVc
	{
	public:
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case kfragBase: // the root; display the paragraph embedded in a div.
				pvwenv->OpenDiv();
				pvwenv->OpenParagraph();
				pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL);
				pvwenv->CloseParagraph();
				pvwenv->CloseDiv();
				break;
			case kfragBaseLiteral: // the root; display the paragraph embedded in a div, with preceding label.
				{
					pvwenv->OpenDiv();
					pvwenv->OpenParagraph();
					ITsStrFactoryPtr qtsf;
					ITsStringPtr qtssLit;
					qtsf.CreateInstance(CLSID_TsStrFactory);
					qtsf->MakeStringRgch(OleStringLiteral(L"xyzyuyky "), 9, g_wsEng, &qtssLit); // must be 8-letter bad English word.
					pvwenv->AddString(qtssLit);
					pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL);
					pvwenv->CloseParagraph();
					pvwenv->CloseDiv();
				}
				break;
			case kfragBaseDouble: // the root; display the paragraph embedded in a div, containing string twice.
				pvwenv->OpenDiv();
				pvwenv->OpenParagraph();
				pvwenv->put_IntProperty(ktptSpellCheck, ktpvEnum, ksmDoNotCheck);
				pvwenv->OpenSpan();
				pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL);
				pvwenv->CloseSpan();
				pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL);
				pvwenv->CloseParagraph();
				pvwenv->CloseDiv();
				break;
			}
			return S_OK;
		}
	};

	// Substitutes for root box and creates mock dictionaries which dislike
	// 8-letter words in English and 7-letter words for everything else.
	class MockDictRootBox : public VwRootBox
	{
	public:
		enchant::Dict * GetDictionary(const OLECHAR * pszId)
		{
			if (!wcscmp(pszId, OleStringLiteral(L"en")))
				return new enchant::MockDict(8);
			else
				return new enchant::MockDict(7);
		}
	};

	class TestVwParagraph : public unitpp::suite
	{
		IVwRootBoxPtr m_qrootb;

		// This test fails probably because of some changes in VwTextBoxes. JohnT will have a
		// look at it (thanks John!)
		void testNormalizeNfd()
		{
			HRESULT hr;
			// Create test data in a temporary cache.
			// First make some generic objects.
			IVwCacheDaPtr qcda;
			qcda.CreateInstance(CLSID_VwCacheDa);
			ISilDataAccessPtr qsda;
			qcda->QueryInterface(IID_ISilDataAccess, (void **)&qsda);
			qsda->putref_WritingSystemFactory(g_qwsf);

			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			ITsStringPtr qtss;
			// Now make two strings, the contents of paragraphs 1 and 2.
			StrUni stuPara1(L"This is the first" A_WITH_DIAERESIS_AND_MACRON L" test string");
			qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoString1, kflidStTxtPara_Contents, qtss);
			StrUni stuPara2(L"This is the second" A_WITH_DIAERESIS_AND_MACRON L" test string");
			qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoString2, kflidStTxtPara_Contents, qtss);
			StrUni stuPara3(L"This is the third" A_WITH_DIAERESIS_AND_MACRON L" test string");
			qtsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoString3, kflidStTxtPara_Contents, qtss);

			// Now make them the paragraphs of an StText.
			HVO rghvo[3] = {khvoString1, khvoString2, khvoString3};
			HVO hvoRoot = 101;
			qcda->CacheVecProp(hvoRoot, kflidStText_Paragraphs, rghvo, 3);

			// Now make the root box and view constructor and Graphics object.
			IVwRootBoxPtr qrootb;
#ifdef WIN32
			qrootb.CreateInstance(CLSID_VwRootBox);
#else
			VwRootBox::CreateCom(NULL, IID_IVwRootBox, (void **)&qrootb);
#endif
			IVwGraphicsWin32Ptr qvg32;
			HDC hdc = 0;
			try
			{
				qvg32.CreateInstance(CLSID_VwGraphicsWin32);
				hdc = ::GetDC(NULL);
				qvg32->Initialize(hdc);

				IVwViewConstructorPtr qvc;
				qvc.Attach(NewObj NormalizeDummyVc());
				qrootb->putref_DataAccess(qsda);
				qrootb->SetRootObject(hvoRoot, qvc, kfragBase, NULL);
				DummyRootSitePtr qdrs;
				qdrs.Attach(NewObj DummyRootSite());
				Rect rcSrc(0, 0, 96, 96);
				qdrs->SetRects(rcSrc, rcSrc);
				qdrs->SetGraphics(qvg32);
				qrootb->SetSite(qdrs);
				// We need to lay it out or some of the selections don't get made.
				hr = qrootb->Layout(qvg32, 300);

				IVwSelectionPtr qselInitial;
				// Make a selection at the start, where editable, not a range, don't install.
				hr = qrootb->MakeSimpleSel(true, true, false, false, &qselInitial);
				IVwSelectionPtr qselFinal;
				// Make a selection at the end, where editable, not a range, don't install.
				hr = qrootb->MakeSimpleSel(false, true, false, false, &qselFinal);
				IVwSelectionPtr qselFirstString;
				// Make a selection at the start, where editable, a range (all of 1st string), don't install.
				hr = qrootb->MakeSimpleSel(true, true, true, false, &qselFirstString);

				VwSelLevInfo vsli;
				vsli.tag = kflidStText_Paragraphs;
				vsli.cpropPrevious = 0; // first para, i.e., first occurrence of text paragraphs property.
				vsli.ihvo = 1; // second string (2nd StTxtPara).

				IVwSelectionPtr qselString2;
				hr = qrootb->MakeTextSelInObj(
					0, // First (and only) object in root.
					1, &vsli, // simulate an array of one item for first object to select.
					0, NULL, // No range of objects.
					true, // select at start of that object
					true, // (require it to be editable)
					true, // select a range, in this case the whole string
					false, // don't treat as a whole-object selection, though in a sense it is
					false, // don't install it as active
					&qselString2);

				vsli.ihvo = 2;
				IVwSelectionPtr qselString3;
				hr = qrootb->MakeTextSelInObj(
					0, // First (and only) object in root.
					1, &vsli, // simulate an array of one item for first object to select.
					0, NULL, // No range of objects.
					true, // select at start of that object
					true, // (require it to be editable)
					true, // select a range, in this case the whole string
					false, // don't treat as a whole-object selection, though in a sense it is
					false, // don't install it as active
					&qselString3);

				IVwSelectionPtr qselString3Para2;
				vsli.cpropPrevious = 1; // second occurrence of the text paragraphs property.
				hr = qrootb->MakeTextSelInObj(
					0, // First (and only) object in root.
					1, &vsli, // simulate an array of one item for first object to select.
					0, NULL, // No range of objects.
					true, // select at start of that object
					true, // (require it to be editable)
					true, // select a range, in this case the whole string
					false, // don't treat as a whole-object selection, though in a sense it is
					false, // don't install it as active
					&qselString3Para2);

				int ichMagic3 = 17; // position of magic character in 3d string.

				// Now select the 'a with diaresis and macron' in the third para
				vsli.cpropPrevious = 0; // back to first para.
				IVwSelectionPtr qselThirdParaMagicChar;
				hr = qrootb->MakeTextSelection(
					0, // First (and only) object in root.
					1, &vsli, // simulate an array of one item for first object to select.
					kflidStTxtPara_Contents, // this (the only) property of that object
					0, // first (and only) occurrence)
					ichMagic3, ichMagic3 + 1, // range to select
					0, // not multilingual, don't care about ws
					false, // range, don't care about association direction
					-1, // selection ends in same StTxtPara
					NULL, // no special insertion point props
					false, // don't install it
					& qselThirdParaMagicChar);

				IVwSelectionPtr qselBeforeThirdParaMagicChar;
				hr = qrootb->MakeTextSelection(
					0, // First (and only) object in root.
					1, &vsli, // simulate an array of one item for first object to select.
					kflidStTxtPara_Contents, // this (the only) property of that object
					0, // first (and only) occurrence)
					ichMagic3, ichMagic3, // range (position) to select
					0, // not multilingual, don't care about ws
					false, // Using as an end point, direction is arbitrary.
					-1, // selection ends in same StTxtPara
					NULL, // no special insertion point props
					false, // don't install it
					& qselBeforeThirdParaMagicChar);

				IVwSelectionPtr qselIn2ndString2ndPara;
				vsli.cpropPrevious = 1; // makes it second para
				vsli.ihvo = 1; // makes it second string
				hr = qrootb->MakeTextSelection(
					0, // First (and only) object in root.
					1, &vsli, // simulate an array of one item for first object to select.
					kflidStTxtPara_Contents, // this (the only) property of that object
					0, // first (and only) occurrence)
					15, 15, // range (position) to select
					0, // not multilingual, don't care about ws
					false, // Using as an end point, direction is arbitrary.
					-1, // selection ends in same StTxtPara
					NULL, // no special insertion point props
					false, // don't install it
					& qselIn2ndString2ndPara);

				IVwSelectionPtr qselCrossPara;
				hr = qrootb->MakeRangeSelection(qselBeforeThirdParaMagicChar,
					qselIn2ndString2ndPara,
					false,
					&qselCrossPara);
				IVwSelectionPtr qselCrossParaReverse;
				hr = qrootb->MakeRangeSelection(qselIn2ndString2ndPara,
					qselBeforeThirdParaMagicChar,
					false,
					&qselCrossParaReverse);

				unitpp::assert_true("qrootb.Ptr() should not null", qrootb.Ptr() != NULL);
				VwRootBox * prootb = dynamic_cast<VwRootBox *>(qrootb.Ptr());
				unitpp::assert_true("prootb is not a VwRootBox * ", prootb != NULL);
				VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(prootb->FirstBox());
				pvpbox->MakeSourceNfd();

				int cchPara1 = stuPara1.Length();
				int cchPara2 = stuPara2.Length();
				int ichPara2 = cchPara1;
				int cchPara3 = stuPara3.Length();
				int ichPara3 = ichPara2 + cchPara2;
				int ichEnd = ichPara3 + cchPara3;
				int cchPara1N = cchPara1 + 2; // normalization increases it by 2.
				//int cchPara2N = cchPara2 + 2; // normalization increases it by 2.
				//int cchPara3N = cchPara3 + 2; // normalization increases it by 2.
				int ichPara2N = ichPara2 + 2;
				int ichPara3N = ichPara3 + 4;
				int ichEndN = ichEnd + 6;

				VwTextSelection * pselInitial = dynamic_cast<VwTextSelection *>(qselInitial.Ptr());
				unitpp::assert_eq("Initial sel not moved", 0, pselInitial->AnchorOffset());
				unitpp::assert_eq("Initial sel not moved", 0, pselInitial->EndOffset());


				// This is in the second paragraph, unchanged.
				VwTextSelection * pselFinal = dynamic_cast<VwTextSelection *>(qselFinal.Ptr());
				unitpp::assert_eq("Final sel not moved", ichEnd, pselFinal->AnchorOffset());
				unitpp::assert_eq("Final sel not moved", ichEnd, pselFinal->EndOffset());

				VwTextSelection * pselFirstString = dynamic_cast<VwTextSelection *>(qselFirstString.Ptr());
				unitpp::assert_eq("Start of first string not moved", 0, pselFirstString->AnchorOffset());
				unitpp::assert_eq("End of first string moved", cchPara1N, pselFirstString->EndOffset());

				VwTextSelection * pselString2 = dynamic_cast<VwTextSelection *>(qselString2.Ptr());
				unitpp::assert_eq("Start of 2nd string moved", ichPara2N, pselString2->AnchorOffset());
				unitpp::assert_eq("End of 2nd string moved more", ichPara3N, pselString2->EndOffset());

				VwTextSelection * pselString3 = dynamic_cast<VwTextSelection *>(qselString3.Ptr());
				unitpp::assert_eq("Start of 3rd string moved", ichPara3N, pselString3->AnchorOffset());
				unitpp::assert_eq("End of 3rd string moved more", ichEndN, pselString3->EndOffset());

				VwTextSelection * pselString3Para2 = dynamic_cast<VwTextSelection *>(qselString3Para2.Ptr());
				unitpp::assert_eq("Start of string 3 para 2 not moved", ichPara3, pselString3Para2->AnchorOffset());
				unitpp::assert_eq("End of string 3 para 2 not moved", ichEnd, pselString3Para2->EndOffset());

				VwTextSelection * pselThirdParaMagicChar = dynamic_cast<VwTextSelection *>(qselThirdParaMagicChar.Ptr());
				unitpp::assert_eq("Magic char in para 3 moved (start)", ichPara3N + ichMagic3, pselThirdParaMagicChar->AnchorOffset());
				unitpp::assert_eq("Magic char in para 3 moved (end)", ichPara3N + ichMagic3 + 3, pselThirdParaMagicChar->EndOffset());

				VwTextSelection * pselCrossPara = dynamic_cast<VwTextSelection *>(qselCrossPara.Ptr());
				unitpp::assert_eq("Start of cross-para sel moved", ichPara3N + ichMagic3, pselCrossPara->AnchorOffset());
				unitpp::assert_eq("End of cross-para sel not moved", ichPara2 + 15, pselCrossPara->EndOffset());

				VwTextSelection * pselCrossParaReverse = dynamic_cast<VwTextSelection *>(qselCrossParaReverse.Ptr());
				unitpp::assert_eq("Start of reverse cross-para sel moved", ichPara2 + 15, pselCrossParaReverse->AnchorOffset());
				unitpp::assert_eq("End of reverse cross-para sel not moved", ichPara3N + ichMagic3, pselCrossParaReverse->EndOffset());
			}
			catch(...)
			{
				if (qvg32)
					qvg32->ReleaseDC();
				if (hdc != 0)
					::ReleaseDC(NULL, hdc);
				qrootb->Close();
				throw;
			}

			// Cleanup
			qvg32->ReleaseDC();
			::ReleaseDC(NULL, hdc);
			qrootb->Close();
		}

		// Assert that the baseline of the specified box is as expected.
		void VerifyBaseline(int baseline, VwBox * pboxVerify)
		{
			int result = pboxVerify->Baseline();
			for (VwBox * pbox = pboxVerify->Container(); pbox; pbox = pbox->Container())
				result += pbox->Top();
			if (result != baseline)
				unitpp::assert_eq("baseline position", baseline, result);
		}

		// Assert the correct baseline for the ichild'th child of pipbox.
		// If ichild2 is not -1, the ichildth child is itself a paragraph, whose ichildt'th child
		// is an inner pile, and we verify its ichild3th child.
		void VerifyBaseline(int baseline, VwInnerPileBox * pipbox, int ichild, int ichild2 = -1, int ichild3 = -1)
		{
			int i = 0;
			VwBox * pbox;
			for (pbox = pipbox->FirstBox(); pbox && i < ichild; pbox = pbox->NextRealBox(), i++)
				;
			if (ichild2 == -1)
			{
				VerifyBaseline(baseline, pbox);
				return;
			}
			i = 0;
			VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pbox);
			for (pbox = pvpbox->FirstBox(); pbox && i < ichild2; pbox = pbox->NextRealBox(), i++)
				;
			i = 0;
			VwInnerPileBox * pipbox2 = dynamic_cast<VwInnerPileBox *>(pbox);
			for (pbox = pipbox2->FirstBox(); pbox && i < ichild3; pbox = pbox->NextRealBox(), i++)
				;
			VerifyBaseline(baseline, pbox);
		}

		// Test the special code for aligning things in interlinear piles on the same row.
		void testInnerPileLayout()
		{
			// Now make the root box and view constructor and Graphics object.
			IVwCacheDaPtr qcda;
			qcda.CreateInstance(CLSID_VwCacheDa);
			ISilDataAccessPtr qsda;
			qcda->QueryInterface(IID_ISilDataAccess, (void **)&qsda);
			qsda->putref_WritingSystemFactory(g_qwsf);

			IVwRootBoxPtr qrootb;
#ifdef WIN32
			qrootb.CreateInstance(CLSID_VwRootBox);
#else
			VwRootBox::CreateCom(NULL, IID_IVwRootBox, (void **)&qrootb);
#endif
			VwRootBox * prootb = dynamic_cast<VwRootBox *>(qrootb.Ptr());
			unitpp::assert_true("prootb should not be null", prootb != NULL);
			IVwGraphicsWin32Ptr qvg32;
			HDC hdc = 0;
			try
			{
				qvg32.CreateInstance(CLSID_VwGraphicsWin32);
				hdc = ::GetDC(NULL);
				qvg32->Initialize(hdc);

				IVwViewConstructorPtr qvc;
				qvc.Attach(NewObj InnerPileDummyVc());
				qrootb->putref_DataAccess(qsda);
				qrootb->SetRootObject(hvoRoot, qvc, kfragBase, NULL);
				DummyRootSitePtr qdrs;
				qdrs.Attach(NewObj DummyRootSite());
				Rect rcSrc(0, 0, 96, 96);
				qdrs->SetRects(rcSrc, rcSrc);
				qdrs->SetGraphics(qvg32);
				qrootb->SetSite(qdrs);
				qrootb->Layout(qvg32, 3000);

				// Now see where things are.
				// The baselines are at:
				//	- 30 (because of the last stand-alone box)
				//  - 46 (in the 4th inner pile, first rectangle descends 5, next ascends 11)
				//	- 62 (4th inner pile, second descends 5, third ascends 11)
				//	- 78 (4th IP, third descends 5; second inner IP, first ascends 11)
				//	- 96 (4th IP, second inner IP: first descends 5, second ascends 13)
				//	- 111 (1st IP, 5th descends 5, 6th ascends 10)
				//	- 126 (1st IP, 6th descends 5, 7th ascends 10)
				//	- 140 (2nd IP, 7th descends 5, 8th ascends 9)
				int baselines[] = {30, 46, 62, 78, 96, 111, 126, 140};
				//int kclines = isizeof(baselines) / isizeof(int);
				VwParagraphBox * pxpbox = dynamic_cast<VwParagraphBox *>(prootb->FirstBox());
				// Get the level 2 boxes (inside the main paragraph)
				VwInnerPileBox * pipbox1 = dynamic_cast<VwInnerPileBox *>(pxpbox->FirstBox());
				VwBox * pbox2 = pipbox1->NextRealBox();
				VwInnerPileBox * pipbox2 = dynamic_cast<VwInnerPileBox *>(pbox2->NextRealBox());
				VwInnerPileBox * pipbox3 = dynamic_cast<VwInnerPileBox *>(pipbox2->NextRealBox());
				VwInnerPileBox * pipbox4 = dynamic_cast<VwInnerPileBox *>(pipbox3->NextRealBox());
				VwBox * pbox5 = pipbox4->NextRealBox();
				// check the baselienes for the direct children of pile 1
				VwBox * pbox;
				int i = 0;
				for (pbox = pipbox1->FirstBox(); pbox; pbox = pbox->NextRealBox(), i++)
					VerifyBaseline(baselines[i], pbox);
				// similarly for children of pile 2.
				i = 0;
				for (pbox = pipbox2->FirstBox(); pbox; pbox = pbox->NextRealBox(), i++)
					VerifyBaseline(baselines[i], pbox);

				VerifyBaseline(baselines[7], pipbox3, 4); // 8th line in 3rd pile
				VerifyBaseline(baselines[3], pipbox3, 2, 0, 1); // 4th line in 3rd pile is row 2 of first pile at row 3
				VerifyBaseline(baselines[3], pipbox4, 3, 0, 0); // 4th line in 4th pile is row 1 of first pile at row 4
				VerifyBaseline(baselines[3], pipbox4, 3, 1, 0); // 4th line in 4th pile is row 1 of second pile at row 4
				VerifyBaseline(baselines[6], pipbox4, 3, 1, 3); // 4th line in 4th pile is row 4 of second pile at row 4
				VerifyBaseline(baselines[7], pipbox4, 4); // 8th line in 4th pile
				VerifyBaseline(baselines[0], pbox5);
			}
			catch(...)
			{
				if (qvg32)
					qvg32->ReleaseDC();
				if (hdc != 0)
					::ReleaseDC(NULL, hdc);
				qrootb->Close();
				throw;
			}

			// Cleanup
			qvg32->ReleaseDC();
			::ReleaseDC(NULL, hdc);
			qrootb->Close();

		}

		// test spell checking
		void testSpellCheck()
		{
			//std::string wsId("en_US");
			// reinstate this if things change so that it is *supposed* to be possible to have
			// more than one Dict instance in existence at the same time. If that happens we should also
			// reinstate the code that hangs on to them.
			//enchant::Dict * pdic1 = enchant::Broker::instance()->request_dict(wsId);
			//enchant::Dict * pdic2 = enchant::Broker::instance()->request_dict(wsId);
			//delete pdic1;
			//delete pdic2;
			IVwCacheDaPtr qcda;
			qcda.CreateInstance(CLSID_VwCacheDa);
			ISilDataAccessPtr qsda;
			qcda->QueryInterface(IID_ISilDataAccess, (void **)&qsda);
			qsda->putref_WritingSystemFactory(g_qwsf);

			IVwRootBoxPtr qrootb;
			qrootb.Attach(NewObj MockDictRootBox());		// ref count initialy 1
			IVwGraphicsWin32Ptr qvg32;
			HDC hdc = 0;
			// Note: in case this test is ever run with a real dictionary, the second word should be
			// clearly bad English. To make it fail with the English mock, it must have exactly 8 letters.
			StrUni testData(L"The xzklymgz string");
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			ITsStringPtr qtss;
			qtsf->MakeString(testData.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(hvoRoot, kflidStTxtPara_Contents, qtss);

			try
			{
				qvg32.CreateInstance(CLSID_VwGraphicsWin32);
				hdc = ::GetDC(NULL);
				qvg32->Initialize(hdc);

				IVwViewConstructorPtr qvc;
				qvc.Attach(NewObj NestedStringDummyVc());
				qrootb->putref_DataAccess(qsda);
				qrootb->SetRootObject(hvoRoot, qvc, kfragBase, NULL);
				DummyRootSitePtr qdrs;
				qdrs.Attach(NewObj DummyRootSite());
				Rect rcSrc(0, 0, 96, 96);
				qdrs->SetRects(rcSrc, rcSrc);
				qdrs->SetGraphics(qvg32);
				qrootb->SetSite(qdrs);
				qrootb->Layout(qvg32, 3000);
				ComBool fDone = false;
				while (!fDone)
				{
					HRESULT hr = qrootb->DoSpellCheckStep(&fDone);
					unitpp::assert_eq("DoSpellCheckStep should succeed", S_OK, hr);
				}
				VwDivBox * pdbRoot = dynamic_cast<VwDivBox *>(qrootb.Ptr());
				VwDivBox * pdbInner = dynamic_cast<VwDivBox *>(pdbRoot->FirstBox());
				VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pdbInner->FirstBox());
				int unt;
				COLORREF clr;
				int ichLim;
				pvpbox->Source()->GetUnderlineInfo(0, &unt, &clr, &ichLim);
				unitpp::assert_eq("first run should be 4 chars", 4, ichLim);
				unitpp::assert_eq("first word should not have squiggle", kuntNone, unt);
				pvpbox->Source()->GetUnderlineInfo(4, &unt, &clr, &ichLim);
				unitpp::assert_eq("2nd run should be 8 chars", 12, ichLim);
				unitpp::assert_eq("bad word should have squiggle", kuntSquiggle, unt);
				unitpp::assert_eq("squiggle should be red", kclrRed, clr);
				pvpbox->Source()->GetUnderlineInfo(12, &unt, &clr, &ichLim);
				unitpp::assert_eq("3rd run should be rest of string", 19, ichLim);
				unitpp::assert_eq("3rd word should not have squiggle", kuntNone, unt);

				// Now, if we change the string and retest, it should go away.
				StrUni stuGood(L"The good string");
				UpdateString(stuGood.Bstr(), g_wsEng, qtsf, qcda, qsda);
				fDone = false;
				while (!fDone)
				{
					HRESULT hr = qrootb->DoSpellCheckStep(&fDone);
					unitpp::assert_eq("DoSpellCheckStep should succeed", S_OK, hr);
				}				pdbRoot = dynamic_cast<VwDivBox *>(qrootb.Ptr());
				pdbInner = dynamic_cast<VwDivBox *>(pdbRoot->FirstBox());
				pvpbox = dynamic_cast<VwParagraphBox *>(pdbInner->FirstBox());
				pvpbox->Source()->GetUnderlineInfo(0, &unt, &clr, &ichLim);
				unitpp::assert_eq("should be just one run", stuGood.Length(), ichLim);
				unitpp::assert_true("should remove spelling override",
					dynamic_cast<VwSpellingOverrideTxtSrc *>(pvpbox->Source()) == NULL);

				// This makes sure it can find a problem at the start of the string, also that WS
				// makes a difference.
				// Note: in case this test is ever run with a real dictionary, the first word should be
				// clearly bad French. To make it fail with the French mock, it must have exactly 7 letters.
				StrUni stuBadFrn(L"xyzhjlk une " LATIN_SMALL_LETTER_E_WITH_ACUTE L"cole");
				UpdateString(stuBadFrn.Bstr(), g_wsFrn, qtsf, qcda, qsda);
				fDone = false;
				while (!fDone)
				{
					HRESULT hr = qrootb->DoSpellCheckStep(&fDone);
					unitpp::assert_eq("DoSpellCheckStep should succeed", S_OK, hr);
				}
				pdbRoot = dynamic_cast<VwDivBox *>(qrootb.Ptr());
				pdbInner = dynamic_cast<VwDivBox *>(pdbRoot->FirstBox());
				pvpbox = dynamic_cast<VwParagraphBox *>(pdbInner->FirstBox());
				pvpbox->Source()->GetUnderlineInfo(0, &unt, &clr, &ichLim);
				unitpp::assert_eq("first run should be 7 chars", 7, ichLim);
				unitpp::assert_eq("first Frn word should have squiggle", kuntSquiggle, unt);
				unitpp::assert_eq("Frn squiggle should be red", kclrRed, clr);
				pvpbox->Source()->GetUnderlineInfo(7, &unt, &clr, &ichLim);
				unitpp::assert_eq("2nd run should be rest of Frn string", stuBadFrn.Length(), ichLim);

				// Now we want to try the bizarre case of a 'word' that's in two languages.
				// The string is still L"The xzklymgz string".
				// We make the "ly" in the middle French.
				ITsStrBldrPtr qtsb;
				qtss->GetBldr(&qtsb);
				qtsb->SetIntPropValues(7, 9, ktptWs, ktpvDefault, g_wsFrn);
				ITsStringPtr qtssMixed;
				qtsb->GetString(&qtssMixed);
				qcda->CacheStringProp(hvoRoot, kflidStTxtPara_Contents, qtssMixed);
				qsda->PropChanged(NULL, kpctNotifyAll, hvoRoot, kflidStTxtPara_Contents, 0, 0, 0);
				fDone = false;
				while (!fDone)
				{
					HRESULT hr = qrootb->DoSpellCheckStep(&fDone);
					unitpp::assert_eq("DoSpellCheckStep should succeed", S_OK, hr);
				}
				pdbRoot = dynamic_cast<VwDivBox *>(qrootb.Ptr());
				pdbInner = dynamic_cast<VwDivBox *>(pdbRoot->FirstBox());
				pvpbox = dynamic_cast<VwParagraphBox *>(pdbInner->FirstBox());
				pvpbox->Source()->GetUnderlineInfo(0, &unt, &clr, &ichLim);
				unitpp::assert_eq("first word in mixed-language is fine", kuntNone, unt);
				pvpbox->Source()->GetUnderlineInfo(5, &unt, &clr, &ichLim);
				unitpp::assert_eq("should still report mixed-language word(5)", kuntSquiggle, unt);
				unitpp::assert_eq("mixed-language squiggle should be blue", kclrBlue, clr);
				unitpp::assert_eq("mixed-language squiggle first run ends at 7", 7, ichLim);
				pvpbox->Source()->GetUnderlineInfo(7, &unt, &clr, &ichLim);
				unitpp::assert_eq("should still report mixed-language word(7)", kuntSquiggle, unt);
				unitpp::assert_eq("mixed-language squiggle should be blue(7)", kclrBlue, clr);
				unitpp::assert_eq("mixed-language squiggle french run ends at 9", 9, ichLim);
				pvpbox->Source()->GetUnderlineInfo(9, &unt, &clr, &ichLim);
				unitpp::assert_eq("should still report mixed-language word(9)", kuntSquiggle, unt);
				unitpp::assert_eq("mixed-language squiggle should be blue(9)", kclrBlue, clr);
				unitpp::assert_eq("mixed-language squiggle french run ends at 12", 12, ichLim);
				pvpbox->Source()->GetUnderlineInfo(12, &unt, &clr, &ichLim);
				unitpp::assert_eq("rest of mixed-language string is fine", kuntNone, unt);
				unitpp::assert_eq("end of mixed-language string", 19, ichLim);

				// Now check on read-only text. First, back to the default string which has one error.
				qcda->CacheStringProp(hvoRoot, kflidStTxtPara_Contents, qtss);
				// But, now display with the VC inserting a literal.
				qrootb->SetRootObject(hvoRoot, qvc, kfragBaseLiteral, NULL);
				fDone = false;
				while (!fDone)
				{
					HRESULT hr = qrootb->DoSpellCheckStep(&fDone);
					unitpp::assert_eq("DoSpellCheckStep should succeed", S_OK, hr);
				}
				pdbRoot = dynamic_cast<VwDivBox *>(qrootb.Ptr());
				pdbInner = dynamic_cast<VwDivBox *>(pdbRoot->FirstBox());
				pvpbox = dynamic_cast<VwParagraphBox *>(pdbInner->FirstBox());
				pvpbox->Source()->GetUnderlineInfo(0, &unt, &clr, &ichLim);
				int cchLit = 9; // length of initial literal
				pvpbox->Source()->GetUnderlineInfo(2, &unt, &clr, &ichLim);
				unitpp::assert_eq("should not report read-only word", kuntNone, unt);
				pvpbox->Source()->GetUnderlineInfo(cchLit + 6, &unt, &clr, &ichLim);
				unitpp::assert_eq("should report bad word following literal", kuntSquiggle, unt);

				// And now, text that is editable, but prohibited by a special text prop.
				qrootb->SetRootObject(hvoRoot, qvc, kfragBaseDouble, NULL);
				fDone = false;
				while (!fDone)
				{
					HRESULT hr = qrootb->DoSpellCheckStep(&fDone);
					unitpp::assert_eq("DoSpellCheckStep should succeed", S_OK, hr);
				}
				pdbRoot = dynamic_cast<VwDivBox *>(qrootb.Ptr());
				pdbInner = dynamic_cast<VwDivBox *>(pdbRoot->FirstBox());
				pvpbox = dynamic_cast<VwParagraphBox *>(pdbInner->FirstBox());
				pvpbox->Source()->GetUnderlineInfo(0, &unt, &clr, &ichLim);
				pvpbox->Source()->GetUnderlineInfo(6, &unt, &clr, &ichLim);
				unitpp::assert_eq("should not report no-spell-check word", kuntNone, unt);
				pvpbox->Source()->GetUnderlineInfo(testData.Length() + 6, &unt, &clr, &ichLim);
				unitpp::assert_eq("should report bad word following literal", kuntSquiggle, unt);
			}
			catch(...)
			{
				if (qvg32)
					qvg32->ReleaseDC();
				if (hdc != 0)
					::ReleaseDC(NULL, hdc);
				qrootb->Close();
				throw;
			}

			// Cleanup
			qvg32->ReleaseDC();
			::ReleaseDC(NULL, hdc);
			qrootb->Close();
		}

		void UpdateString(BSTR pchTxt, int ws, ITsStrFactory * ptsf, IVwCacheDa * pcda, ISilDataAccess * psda)
		{
			ITsStringPtr qtss;
			ptsf->MakeString(pchTxt, ws, &qtss);
			pcda->CacheStringProp(hvoRoot, kflidStTxtPara_Contents, qtss);
			psda->PropChanged(NULL, kpctNotifyAll, hvoRoot, kflidStTxtPara_Contents, 0, 0, 0);
		}

	public:
		TestVwParagraph();

		virtual void Setup()
		{
			VwRootBox::CreateCom(NULL, IID_IVwRootBox, (void **)&m_qrootb);
			CreateTestWritingSystemFactory();
		}
		virtual void Teardown()
		{
			m_qrootb->Close();
			m_qrootb.Clear();
			CloseTestWritingSystemFactory();
		}
	};
}

#endif /*TestVwParagaph_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
