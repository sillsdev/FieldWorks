/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestLazyBox.h
Responsibility:
Last reviewed:

	Unit tests for the VwRootBox class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestLazyBox_H_INCLUDED
#define TestLazyBox_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{
#define kflidTestDummy 999
	class DummyVc : public DummyBaseVc
	{
	public:
		int m_dympParaTrailingMargin;
		int m_dympParaLeadingMargin;
		SmartBstr m_StyleName;

		DummyVc()
		{
			m_dympParaTrailingMargin = 0;
			m_dympParaLeadingMargin = 0;
		}

		DummyVc(BSTR styleName)
		{
			m_dympParaTrailingMargin = 0;
			m_dympParaLeadingMargin = 0;
			m_StyleName = styleName;
		}

		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case 1: // the root; display the subitems, first using non-lazy view, then lazy one.
				pvwenv->AddObjVecItems(kflidTestDummy, this, 4);
				pvwenv->AddObjVecItems(kflidTestDummy, this, 2);
				break;
			case 2: // An StText, display paragraphs lazily
				pvwenv->AddLazyVecItems(kflidStText_Paragraphs, this, 3);
				break;
			case 3: // StTxtPara, display contents with border
				pvwenv->put_IntProperty(ktptBorderTop, ktpvMilliPoint, 3000);
				pvwenv->put_IntProperty(ktptBorderBottom, ktpvMilliPoint, 3000);
				pvwenv->put_IntProperty(ktptBorderLeading, ktpvMilliPoint, 3000);
				pvwenv->put_IntProperty(ktptBorderTrailing, ktpvMilliPoint, 3000);
				if (m_dympParaLeadingMargin != 0)
					pvwenv->put_IntProperty(ktptMarginLeading, ktpvMilliPoint, m_dympParaLeadingMargin);
				if (m_dympParaTrailingMargin != 0)
					pvwenv->put_IntProperty(ktptMarginTrailing, ktpvMilliPoint, m_dympParaTrailingMargin);

				pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL);
				break;
			case 4: // An StText, display paragraphs not lazily.
				pvwenv->AddObjVecItems(kflidStText_Paragraphs, this, 3);
				break;
			case 5: // 2 bar boxes in a row
				pvwenv->AddSimpleRect(0xC0000000, -1, 20000, 0);
				pvwenv->AddSimpleRect(0xC0000000, -1, 20000, 0);
				break;

			// the following 6 frags mimic what we do for smushed footnotes in FootnoteVc.cs
			case 6: // an StText. Display paragraphs as text objects (see FootnoteVc.cs)
				pvwenv->AddObjVec(kflidTestDummy, this, 7);
				break;
			case 8:
				pvwenv->AddObj(hvo, this, 9);
				pvwenv->AddObjVecItems(kflidStText_Paragraphs, this, 10);
				break;
			case 9:
				{
				pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable);
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				ITsStringPtr qtss;
				SmartBstr bstr(L"* ");
				qtsf->MakeString(bstr, g_wsFrn, &qtss);
				pvwenv->AddString(qtss);
				break;
				}
			case 10:
				pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL);
				break;
			// next 3 just display the paragraphs...
			case 11:
				if (m_StyleName.Length() > 0)
					pvwenv->put_StringProperty(ktptNamedStyle, m_StyleName);
				pvwenv->AddObjVecItems(kflidTestDummy, this, 12);
				break;
			case 12: // An StText, display paragraphs not lazily.
				pvwenv->AddObjVecItems(kflidStText_Paragraphs, this, 13);
				break;
			case 13: // StTxtPara, display contents without border
				pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL);
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
		STDMETHOD(DisplayVec)(IVwEnv * pvwenv, HVO hvo, int tag, int frag)
		{
			switch (frag)
			{
			case 7:
				{
					ISilDataAccessPtr qsda;
					pvwenv->get_DataAccess(&qsda);
					HVO rgHvos[2];
					qsda->get_VecItem(hvo, tag, 0, &rgHvos[0]);
					qsda->get_VecItem(hvo, tag, 1, &rgHvos[1]);

					pvwenv->OpenMappedTaggedPara();
					// Add content of first para as string
					pvwenv->AddObj(rgHvos[0], this, 8);

					// Add space between
					ITsStrFactoryPtr qtsf;
					qtsf.CreateInstance(CLSID_TsStrFactory);
					ITsStringPtr qtss;
					SmartBstr bstr(L"  ");
					qtsf->MakeString(bstr, g_wsEng, &qtss);
					pvwenv->AddString(qtss);

					// Add content of second para as string
					pvwenv->AddObj(rgHvos[1], this, 8);
					pvwenv->CloseParagraph();
				break;
				}
			default:
				Assert(false);
				break;
			}
			return S_OK;
		}

	};
	class DummyVc2 : public DummyBaseVc
	{
	public:
		bool m_fShowParaContents;

		DummyVc2()
		{
			m_fShowParaContents = true;
		}
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case 1: // the root; display the subitems lazily.
				pvwenv->AddObjVecItems(kflidTestDummy, this, 2);
				break;
			case 2: // An StText, display paragraphs lazily
				pvwenv->AddLazyVecItems(kflidStText_Paragraphs, this, 3);
				break;
			case 3: // StTxtPara, display contents with border
				if (m_fShowParaContents)
				{
					pvwenv->put_IntProperty(ktptBorderTop, ktpvMilliPoint, 3000);
					pvwenv->put_IntProperty(ktptBorderBottom, ktpvMilliPoint, 3000);
					pvwenv->put_IntProperty(ktptBorderLeading, ktpvMilliPoint, 3000);
					pvwenv->put_IntProperty(ktptBorderTrailing, ktpvMilliPoint, 3000);
					pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL);
				}
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

	// This one is used for testing replacement of a real box with a lazy one.
	class DummyVc3 : public DummyBaseVc
	{
	public:
		int m_dypMarginTop;
		int m_dypMarginBottom;
		int m_dypMswMarginTop;

		DummyVc3()
		{
			m_dypMswMarginTop = m_dypMarginBottom = m_dypMarginTop = 0;
		}
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case 1: // the root, an StText; display the subitems lazily.
				pvwenv->AddLazyVecItems(kflidStText_Paragraphs, this, 2);
				break;
			case 2: // StTxtPara, display contents with specified margins
				if (m_dypMarginTop)
					pvwenv->put_IntProperty(ktptMarginTop, ktpvMilliPoint, m_dypMarginTop);
				if (m_dypMarginBottom)
					pvwenv->put_IntProperty(ktptMarginBottom, ktpvMilliPoint, m_dypMarginBottom);
				if (m_dypMswMarginTop)
					pvwenv->put_IntProperty(ktptMswMarginTop, ktpvMilliPoint, m_dypMswMarginTop);
				pvwenv->AddStringProp(kflidStTxtPara_Contents, this);
				break;
			}
			return S_OK;
		}
		STDMETHOD(EstimateHeight)(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight)
		{
			*pdyHeight = 500; // We want it to be very far off if not corrected.
			return S_OK;
		}
		STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
			int tag, int frag, int ihvoMin)
		{
			return S_OK;
		}
	};

#define kfragBook 2001
#define kfragSection 2002
#define kfragParagraphs 2003
#define kfragScripture 2004
#define kflidSections 3001
#define kflidParas 3002
#define kflidBooks 3003
	// This one simulates a book made of sections made of paragraphs, with sections
	// and paragraphs both lazy. We use the real kflidStTxtPara_Contents for the text.
	class DummyVcBkSecPara : public DummyBaseVc
	{
	public:
		DummyVcBkSecPara() : DummyBaseVc()
		{
			m_dypEstSectionHeight = 100; // Default value useful for main test.
		}

		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case kfragBook: // the root; display the sections lazily.
				pvwenv->AddLazyVecItems(kflidSections, this, kfragSection);
				break;
			case kfragSection: // A section, display paragraphs lazily
				pvwenv->AddLazyVecItems(kflidParas, this, kfragParagraphs);
				break;
			case kfragParagraphs: // StTxtPara, display contents
				pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL);
				break;

			}
			return S_OK;
		}
		STDMETHOD(EstimateHeight)(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight)
		{
			if (frag == kfragSection)
				*pdyHeight = m_dypEstSectionHeight;
			else
				*pdyHeight = 20;
			return S_OK;
		}
		STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
			int tag, int frag, int ihvoMin)
		{
			return S_OK;
		}
		int m_dypEstSectionHeight;
	};
	DEFINE_COM_PTR(DummyVcBkSecPara);

	// This also makes a 'book' of sections of paragraphs, but the whole book is
	// a div, and so is each section.
	class DummyVcBkSecParaDiv : public DummyBaseVc
	{
	public:
		HVO rgLoadedSections[100];
		int iLoadedSectionCount;
		int m_flidPara;
		bool m_fShowSeparators;
		bool m_fLazy;

		DummyVcBkSecParaDiv(int flidPara = kflidStTxtPara_Contents, bool fShowSeparators = true,
			bool fDisplayLazy = true)
		{
			m_flidPara = flidPara;
			iLoadedSectionCount = 0;
			m_fShowSeparators = fShowSeparators;
			m_fLazy = fDisplayLazy;
		}

		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case kfragScripture: // display the books lazily
				if (m_fLazy)
					pvwenv->AddLazyVecItems(kflidBooks, this, kfragBook);
				else
					pvwenv->AddObjVecItems(kflidBooks, this, kfragBook);
				break;
			case kfragBook: // display the sections lazily inside a div.
				pvwenv->OpenDiv();
				if (m_fLazy)
					pvwenv->AddLazyVecItems(kflidSections, this, kfragSection);
				else
					pvwenv->AddObjVecItems(kflidSections, this, kfragSection);
				if (m_fShowSeparators)
					pvwenv->AddSimpleRect(0, -1, 20000, 0);
				pvwenv->CloseDiv();
				break;
			case kfragSection: // A section, display paragraphs lazily inside a div
				pvwenv->OpenDiv();
				if (m_fLazy)
					pvwenv->AddLazyVecItems(kflidParas, this, kfragParagraphs);
				else
					pvwenv->AddObjVecItems(kflidParas, this, kfragParagraphs);
				pvwenv->CloseDiv();
				break;
			case kfragParagraphs: // StTxtPara, display contents
				pvwenv->OpenParagraph();
				pvwenv->AddStringProp(m_flidPara, NULL);
				pvwenv->CloseParagraph();
				break;
			}
			return S_OK;
		}
		STDMETHOD(EstimateHeight)(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight)
		{
			// Height is in points!
			if (frag == kfragSection)
				*pdyHeight = 225;  // 300 px
			else if (frag == kfragBook)
				*pdyHeight = 2250; // 3000 px
			else
				*pdyHeight = 15;   // 20 px
			return S_OK;
		}
		STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
			int tag, int frag, int ihvoMin)
		{
			if (tag == kflidSections)
			{
				for (int i = 0; i < chvo; i++)
					rgLoadedSections[iLoadedSectionCount++] = prghvo[i];
			}
			return S_OK;
		}
	};
	DEFINE_COM_PTR(DummyVcBkSecParaDiv);

	class TestLazyBox : public unitpp::suite
	{
	public:
		void testExpandingBoxedParagraphs()
		{
			//unitpp::assert_true("Non-null m_qrootb after setup", m_qrootb.Ptr() != 0);
			//HRESULT hr;

			// Now make two strings, the contents of paragraphs 1 and 2.
			ITsStringPtr qtss;
			StrUni stuPara1(L"This is the first test paragraph");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss);
			StrUni stuPara2(L"This is the second test paragraph");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			HVO hvoPara2 = 2;
			m_qcda->CacheStringProp(hvoPara2, kflidStTxtPara_Contents, qtss);

			// Now make each of them the paragraphs of an StText.
			HVO hvoText1 = 101;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, &hvoPara1, 1);
			HVO hvoText2 = 102;
			m_qcda->CacheVecProp(hvoText2, kflidStText_Paragraphs, &hvoPara2, 1);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[2] = {101, 102};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 2);

			m_qvc.Attach(NewObj DummyVc());
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 1, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout should succeed", hr == S_OK);
			VwPrepDrawResult xpdr;
			hr = m_qrootb->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &xpdr);
			unitpp::assert_true("PrepareToDraw should succeed", hr == S_OK);
			VwParagraphBox * pbox = dynamic_cast<VwParagraphBox *>(m_qrootb->FirstBox());
			unitpp::assert_true("Should find a paragraph box", pbox != NULL);
		}

		// This test was motivated by TE-618, and showed how the problem occurred, by
		// expanding the second box of a lazy box which had a border. As a result,
		// while laying out the expanded paragraph, the original lazy box got expanded
		// and destroyed.
		void testExpanding2ndBoxedParagraph()
		{
			//unitpp::assert_true("Non-null m_qrootb after setup", m_qrootb.Ptr() != 0);
			//HRESULT hr;

			// Now make two strings, the contents of paragraphs 1 and 2.
			ITsStringPtr qtss;
			StrUni stuPara1(L"This is the first test paragraph");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss);
			StrUni stuPara2(L"This is the second test paragraph");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			HVO hvoPara2 = 2;
			m_qcda->CacheStringProp(hvoPara2, kflidStTxtPara_Contents, qtss);

			HVO rghvoParas[2] = {hvoPara1, hvoPara2};

			// Now make each of them the paragraphs of an StText.
			HVO hvoText1 = 101;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, rghvoParas, 2);

			// And the StTexts to the contents of a dummy property.
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, &hvoText1, 1);

			m_qvc.Attach(NewObj DummyVc2());
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 1, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);

			VwBox * pbox = m_qrootb->FirstBox();
			for (; pbox && !dynamic_cast<VwLazyBox *>(pbox); pbox = pbox->NextOrLazy())
				;
			unitpp::assert_true("Found lazy box", pbox != NULL);
			VwLazyBox *plzbox = dynamic_cast<VwLazyBox *>(pbox);
			plzbox->ExpandItems(1, 2);
			AssertObj(m_qrootb);
			plzbox = dynamic_cast<VwLazyBox *>(m_qrootb->FirstBox());
			unitpp::assert_true("Should find a lazy box for the first box", plzbox != NULL);
			VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(plzbox->Next());
			unitpp::assert_true("Should find a paragraph box for the second box", pvpbox != NULL);
		}

		void testFindBoxClicked_LazyBoxIsFirstBox()
		{
			// Now make two strings, the contents of paragraphs 1 and 2.
			ITsStringPtr qtss;
			StrUni stuPara1(L"This is the first test paragraph");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss);
			StrUni stuPara2(L"This is the second test paragraph");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			HVO hvoPara2 = 2;
			m_qcda->CacheStringProp(hvoPara2, kflidStTxtPara_Contents, qtss);

			HVO rghvoParas[2] = {hvoPara1, hvoPara2};

			// Now make each of them the paragraphs of an StText.
			HVO hvoText1 = 101;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, rghvoParas, 2);

			// And the StTexts to the contents of a dummy property.
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, &hvoText1, 1);

			m_qvc.Attach(NewObj DummyVc2());
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 1, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);
			unitpp::assert_true("For this test, first box should be a lazy box",
				dynamic_cast<VwLazyBox *>(m_qrootb->FirstBox()) != NULL);
			Rect rcSrc, rcDst;
			VwBox * pbox = m_qrootb->FindBoxClicked(m_qvg32, 10, 15, m_rcSrc, m_rcSrc, &rcSrc, &rcDst);
			unitpp::assert_true("Should not find a lazy box",
				dynamic_cast<VwLazyBox *>(pbox) == NULL);
			VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox*>(m_qrootb->FirstBox());
			unitpp::assert_eq("The box found should be the first string box",
				pvpbox->FirstBox(), pbox);
		}

		void testFindBoxClicked_LazyBoxIsNotFirstBox()
		{
			// Now make two strings, the contents of paragraphs 1 and 2.
			ITsStringPtr qtss;
			StrUni stuPara1(L"This is the first test paragraph");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss);
			StrUni stuPara2(L"This is the second test paragraph");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			HVO hvoPara2 = 2;
			m_qcda->CacheStringProp(hvoPara2, kflidStTxtPara_Contents, qtss);

			HVO rghvoParas[2] = {hvoPara1, hvoPara2};

			// Now make each of them the paragraphs of an StText.
			HVO hvoText1 = 101;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, rghvoParas, 2);

			// And the StTexts to the contents of a dummy property.
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, &hvoText1, 1);

			m_qvc.Attach(NewObj DummyVc2());
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 1, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);
			VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(m_qrootb->FirstBox());
			unitpp::assert_true("For this test, first box should be a lazy box", plzbox != NULL);
			plzbox->ExpandItems(0, 1);

			Rect rcSrc, rcDst;
			int firstHeight = m_qrootb->FirstBox()->Height();
			VwBox * pbox = m_qrootb->FindBoxClicked(m_qvg32, 10, firstHeight + 5, m_rcSrc, m_rcSrc, &rcSrc, &rcDst);
			unitpp::assert_true("Should not find a lazy box",
				dynamic_cast<VwLazyBox *>(pbox) == NULL);
			VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox*>(m_qrootb->FirstBox()->Next());
			unitpp::assert_eq("The box found should be the first string box of the second paragraph",
				pvpbox->FirstBox(), pbox);
		}

		void testFindBoxClicked_LazyBoxExpandsToNothing()
		{
			// Now make each of them the paragraphs of an StText.
			HVO rghvoParas[2] = {1, 2};
			HVO hvoText1 = 101;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, rghvoParas, 2);

			// And the StTexts to the contents of a dummy property.
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, &hvoText1, 1);

			DummyVc2* pvc = NewObj DummyVc2();
			m_qvc.Attach(pvc);
			pvc->m_fShowParaContents = false;

			m_qrootb->SetRootObject(hvoRoot, m_qvc, 1, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);
			VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(m_qrootb->FirstBox());
			unitpp::assert_true("For this test, first box should be a lazy box", plzbox != NULL);

			Rect rcSrc, rcDst;
			VwBox * pbox = m_qrootb->FindBoxClicked(m_qvg32, 10, 15, m_rcSrc, m_rcSrc, &rcSrc, &rcDst);
			unitpp::assert_true("Should not find a box in an empty view :)", pbox == NULL);
		}

		void testFindBoxClicked_SecondLazyBoxExpandsToNothing()
		{
			// Now make each of them the paragraphs of an StText.
			HVO rghvoParas[2] = {1, 2};
			HVO hvoText1 = 101;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, rghvoParas, 2);

			// And the StTexts to the contents of a dummy property.
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, &hvoText1, 1);

			DummyVc2* pvc = NewObj DummyVc2();
			m_qvc.Attach(pvc);

			m_qrootb->SetRootObject(hvoRoot, m_qvc, 1, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);
			VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(m_qrootb->FirstBox());
			unitpp::assert_true("For this test, first box should be a lazy box", plzbox != NULL);
			plzbox->ExpandItems(0, 1);
			pvc->m_fShowParaContents = false;

			Rect rcSrc, rcDst;
			int firstHeight = m_qrootb->FirstBox()->Height();
			VwBox * pbox = m_qrootb->FindBoxClicked(m_qvg32, 10, firstHeight + 5, m_rcSrc,
				m_rcSrc, &rcSrc, &rcDst);
			unitpp::assert_true("Should not find a box when clicking in empty lazy box :)",
				pbox == NULL);
		}

		void testEmptySequence()
		{
			// Make two strings, the contents of paragraphs 1 and 2.
			ITsStringPtr qtss;
			StrUni stuPara1(L"This is the first test paragraph");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss);

			// Now make each of them the paragraphs of an StText.
			HVO hvoText1 = 101;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, &hvoPara1, 1);
			HVO hvoText2 = 102;
			m_qcda->CacheVecProp(hvoText2, kflidStText_Paragraphs, NULL, 0);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[2] = {101, 102};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 2);

			m_qvc.Attach(NewObj DummyVc());
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 1, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);
			VwPrepDrawResult xpdr;
			hr = m_qrootb->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &xpdr);
			unitpp::assert_true("PrepareToDraw succeeded", hr == S_OK);

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

#define khvoSecMin 500
#define khvoParaMin 1500
#define khvoBook 400
#define khvoScripture 450
#define kcSection 6

		void testReverseLaziness1()
		{
			// Create test data. We simulate a book with 6 sections containing 1 to 6 paragraphs
			// each.
			//_CrtSetDbgFlag(_CRTDBG_CHECK_ALWAYS_DF);
			ITsStringPtr qtss;
			StrUni stuPara;
			int hvoPara = khvoParaMin;
			HVO rghvoPara[kcSection];
			HVO rghvoSec[kcSection];
			for (int isec = 0; isec < kcSection; isec++)
			{
				for (int i = 0; i < isec + 1; i++)
				{
					stuPara.Format(L"This is paragraph %d", i);
					m_qtsf->MakeString(stuPara.Bstr(), g_wsEng, &qtss);
					m_qcda->CacheStringProp(hvoPara, kflidStTxtPara_Contents, qtss);
					rghvoPara[i] = hvoPara;
					hvoPara++;
				}
				m_qcda->CacheVecProp(isec + khvoSecMin, kflidParas, rghvoPara, isec + 1);
				rghvoSec[isec] = isec + khvoSecMin;
			}
			m_qcda->CacheVecProp(khvoBook, kflidSections, rghvoSec, kcSection);

			m_qvc.Attach(NewObj DummyVcBkSecPara());
			m_qrootb->SetRootObject(khvoBook, m_qvc, kfragBook, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);
			VwPrepDrawResult xpdr;
			// This should expand everything fully. The sample is only about 360 pixels high,
			// much less than any likely developer's screen.
			//unitpp::assert_true("memory OK", _CrtCheckMemory());
			hr = m_qrootb->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &xpdr);
			//unitpp::assert_true("memory OK", _CrtCheckMemory());
			unitpp::assert_true("PrepareToDraw succeeded", hr == S_OK);
			// Arbitrarily pick the 4th paragraph.
			VwBox * pbox = m_qrootb->FirstBox();
			for (int ipara = 0; ipara < 3; ipara++)
			{
				pbox = pbox->NextOrLazy();
			}
			Rect rdPara4 = pbox->GetBoundsRect(m_qvg32, m_rcSrc, m_rcSrc);
			int ydTop = rdPara4.TopLeft().y;
			int ydBottom = rdPara4.BottomRight().y;
			m_qdrs->SetVisRanges(ydTop + 1, ydBottom - 1, INT_MAX, INT_MAX);
			//unitpp::assert_true("memory OK", _CrtCheckMemory());
			m_qrootb->MaximizeLaziness();

			// Verify that we have the expected set of lazy boxes (except for para 4).
			VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(m_qrootb->FirstBox());
			unitpp::assert_true("Got a lazy box from reconverting", plzbox != NULL);
			// The pattern is section 1: 1 para; section 2: 2 paras.
			// So, before para 4, the first in section 3, we hope to find a single
			// lazy box for the first two sections.
			unitpp::assert_eq("first lazy box starts index 0", 0, plzbox->MinObjIndex());
			unitpp::assert_eq("first lazy box covers 2 sections", 2, plzbox->LimObjIndex());
			unitpp::assert_eq("first lazy box object", khvoBook, plzbox->Object());
			VerifyNotifier(plzbox, khvoBook, kflidSections, 0);

			VwBox * pboxMid = plzbox->NextOrLazy();
			unitpp::assert_eq("para 4 survives after lazy box", pbox, pboxMid);
			VerifyNotifier(pboxMid, rghvoSec[2], kflidParas, 0);

			VwLazyBox * plzbox2 = dynamic_cast<VwLazyBox *>(pbox->NextOrLazy());
			unitpp::assert_true("Got 2nd lazy box from reconverting", plzbox2 != NULL);
			// There are three paragraphs in section 3; this one should cover the last 2.
			unitpp::assert_eq("2nd lazy box starts index 1", 1, plzbox2->MinObjIndex());
			unitpp::assert_eq("2nd lazy box covers 2 sections", 3, plzbox2->LimObjIndex());
			unitpp::assert_eq("2nd lazy box object", rghvoSec[2], plzbox2->Object());
			VwLazyBox * plzbox3 = dynamic_cast<VwLazyBox *>(plzbox2->NextOrLazy());
			unitpp::assert_true("Got 3rd lazy box from reconverting", plzbox3 != NULL);
			// There are three more sections
			unitpp::assert_eq("3rd lazy box starts index 3", 3, plzbox3->MinObjIndex());
			unitpp::assert_eq("3rd lazy box covers 3 sections", 6, plzbox3->LimObjIndex());
			unitpp::assert_eq("3rd lazy box object", khvoBook, plzbox3->Object());
			unitpp::assert_true("3rd lazy box ends root", plzbox3 == m_qrootb->LastBox());
			unitpp::assert_true("Nothing follows 3rd lazy box", plzbox3->NextOrLazy() == NULL);

			// Now expand fully again.
			hr = m_qrootb->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &xpdr);
			unitpp::assert_true("PrepareToDraw2 succeeded", hr == S_OK);
			VwBox * pbox5 = pboxMid->NextOrLazy();
			VwBox * pbox8 = pbox5->NextOrLazy()->NextOrLazy()->NextOrLazy();

			Rect rdPara5 = pbox5->GetBoundsRect(m_qvg32, m_rcSrc, m_rcSrc);
			Rect rdPara8 = pbox8->GetBoundsRect(m_qvg32, m_rcSrc, m_rcSrc);
			m_qdrs->SetVisRanges(rdPara8.TopLeft().y + 1, rdPara8.BottomRight().y - 1,
				rdPara5.TopLeft().y + 1, rdPara5.BottomRight().y - 1);

			m_qrootb->MaximizeLaziness();

			// This time, we have the 5th and 8th paras left behind.
			// So, we have two sections, then one para, in two lazy boxes;
			// then one real para (5);
			// then para 6, the last in section 3, as a lazy box;
			// then para 7, the first in section 4, as a lazy;
			// then real para 8;
			// then paras 9 and 10 as a lazy box;
			// and finally sections 5 and 6
			VwLazyBox * plzbox1b = dynamic_cast<VwLazyBox *>(m_qrootb->FirstBox());
			unitpp::assert_true("Got lazy box 4 from reconverting", plzbox1b != NULL);
			unitpp::assert_eq("Lazy box 1b starts index 0", 0, plzbox1b->MinObjIndex());
			unitpp::assert_eq("Lazy box 1b covers 2 sections", 2, plzbox1b->LimObjIndex());
			unitpp::assert_eq("Lazy box 1b object", khvoBook, plzbox1b->Object());
			VerifyNotifier(plzbox1b, khvoBook, kflidSections, 0);

			VwLazyBox * plzbox2b = dynamic_cast<VwLazyBox *>(plzbox1b->NextOrLazy());
			unitpp::assert_true("Got lazy box 2b from reconverting", plzbox2b != NULL);
			unitpp::assert_eq("Lazy box 2b starts index 0", 0, plzbox2b->MinObjIndex());
			unitpp::assert_eq("Lazy box 2b covers 1 para", 1, plzbox2b->LimObjIndex());
			unitpp::assert_eq("Lazy box 2b object", rghvoSec[2], plzbox2b->Object());

			VwBox * pboxMid1b = plzbox2b->NextOrLazy();
			unitpp::assert_eq("para 5 survives after lazy box", pbox5, pboxMid1b);

			VwLazyBox * plzbox3b = dynamic_cast<VwLazyBox *>(pboxMid1b->NextOrLazy());
			unitpp::assert_true("Lazy box 3b from reconverting", plzbox3b != NULL);
			unitpp::assert_eq("Lazy box 3b starts index 2", 2, plzbox3b->MinObjIndex());
			unitpp::assert_eq("Lazy box 3b covers 1 para", 3, plzbox3b->LimObjIndex());
			unitpp::assert_eq("Lazy box 3b object", rghvoSec[2], plzbox3b->Object());

			VwLazyBox * plzbox4b = dynamic_cast<VwLazyBox *>(plzbox3b->NextOrLazy());
			unitpp::assert_true("Lazy box 4b from reconverting", plzbox4b != NULL);
			unitpp::assert_eq("Lazy box 4b starts index 0", 0, plzbox4b->MinObjIndex());
			unitpp::assert_eq("Lazy box 4b covers 1 para", 1, plzbox4b->LimObjIndex());
			unitpp::assert_eq("Lazy box 4b object", rghvoSec[3], plzbox4b->Object());
			VerifyNotifier(plzbox4b, rghvoSec[3], kflidParas, 0);

			VwBox * pboxMid2b = plzbox4b->NextOrLazy();
			unitpp::assert_eq("para 8 survives after lazy box", pbox8, pboxMid2b);

			VwLazyBox * plzbox5b = dynamic_cast<VwLazyBox *>(pboxMid2b->NextOrLazy());
			unitpp::assert_true("Lazy box 5b from reconverting", plzbox5b != NULL);
			unitpp::assert_eq("Lazy box 5b starts index 2", 2, plzbox5b->MinObjIndex());
			unitpp::assert_eq("Lazy box 5b covers 2 paras", 4, plzbox5b->LimObjIndex());
			unitpp::assert_eq("Lazy box 5b object", rghvoSec[3], plzbox5b->Object());

			VwLazyBox * plzbox6b = dynamic_cast<VwLazyBox *>(plzbox5b->NextOrLazy());
			unitpp::assert_true("Lazy box 6b from reconverting", plzbox6b != NULL);
			unitpp::assert_eq("Lazy box 6b starts index 4", 4, plzbox6b->MinObjIndex());
			unitpp::assert_eq("Lazy box 6b covers 2 sections", 6, plzbox6b->LimObjIndex());
			unitpp::assert_eq("Lazy box 6b object", khvoBook, plzbox6b->Object());

			// Now, make sure we can leave the first and last boxes.
			hr = m_qrootb->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &xpdr);
			unitpp::assert_true("PrepareToDraw2 succeeded", hr == S_OK);
			VwBox * pboxFirst = m_qrootb->FirstBox();
			VwBox * pboxLast = m_qrootb->LastBox();
			Rect rdParaFirst = pboxFirst->GetBoundsRect(m_qvg32, m_rcSrc, m_rcSrc);
			Rect rdParaLast = pboxLast->GetBoundsRect(m_qvg32, m_rcSrc, m_rcSrc);
			m_qdrs->SetVisRanges(rdParaFirst.TopLeft().y + 1, rdParaFirst.BottomRight().y - 1,
				rdParaLast.TopLeft().y + 1, rdParaLast.BottomRight().y - 1);

			m_qrootb->MaximizeLaziness();

			// OK, that leaves the first para,
			// Then a 4-section lazy box (first para is a whole section)
			// Then a 5-para lazy box
			// then the old last paragraph.
			unitpp::assert_eq("first para survives", pboxFirst, m_qrootb->FirstBox());
			VwLazyBox * plzbox1c = dynamic_cast<VwLazyBox *>(pboxFirst->NextOrLazy());
			unitpp::assert_true("Lazy box 1c from reconverting", plzbox1c != NULL);
			unitpp::assert_eq("Lazy box 1c starts index 1", 1, plzbox1c->MinObjIndex());
			unitpp::assert_eq("Lazy box 1c covers 4 sections", 5, plzbox1c->LimObjIndex());
			unitpp::assert_eq("Lazy box 1c object", khvoBook, plzbox1c->Object());

			VwLazyBox * plzbox2c = dynamic_cast<VwLazyBox *>(plzbox1c->NextOrLazy());
			unitpp::assert_true("Lazy box 2c from reconverting", plzbox2c != NULL);
			unitpp::assert_eq("Lazy box 2c starts index 0", 0, plzbox2c->MinObjIndex());
			unitpp::assert_eq("Lazy box 2c covers 5 paras", 5, plzbox2c->LimObjIndex());
			unitpp::assert_eq("Lazy box 2c object", rghvoSec[5], plzbox2c->Object());

			unitpp::assert_eq("last para survives", pboxLast, plzbox2c->NextOrLazy());
			unitpp::assert_eq("last para is last", pboxLast, m_qrootb->LastBox());

			// Now expand fully again.
			hr = m_qrootb->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &xpdr);
			unitpp::assert_true("PrepareToDraw2 succeeded", hr == S_OK);

			m_qdrs->SetVisRanges(rdPara8.TopLeft().y + 1, rdPara8.BottomRight().y - 1,
				INT_MAX, INT_MAX);

			pbox5 = m_qrootb->FirstBox();
			for (int ipara = 0; ipara < 4; ipara++)
			{
				pbox5 = pbox5->NextOrLazy();
			}
			VwBox * pbox6 = pbox5->NextOrLazy();
			pbox8 = pbox6->NextOrLazy()->NextOrLazy();

			m_qrootb->MaximizeLaziness(pbox5, pbox6->NextOrLazy());

			// paragraphs 5 and 6 should survive because of the arguments passed to
			// MaximizeLaziness, paragraph 8 should survive because of the visible range.
			// Therefore we get a result very like the second test, except there isn't a lazy
			// box standing for the last para of section 502.
			VwLazyBox * plzbox1d = dynamic_cast<VwLazyBox *>(m_qrootb->FirstBox());
			unitpp::assert_true("Got lazy box 1d from reconverting", plzbox1d != NULL);
			unitpp::assert_eq("Lazy box 1d starts index 0", 0, plzbox1d->MinObjIndex());
			unitpp::assert_eq("Lazy box 1d covers 2 sections", 2, plzbox1d->LimObjIndex());
			unitpp::assert_eq("Lazy box 1d object", khvoBook, plzbox1d->Object());
			VerifyNotifier(plzbox1d, khvoBook, kflidSections, 0);

			VwLazyBox * plzbox2d = dynamic_cast<VwLazyBox *>(plzbox1d->NextOrLazy());
			unitpp::assert_true("Got lazy box 2d from reconverting", plzbox2d != NULL);
			unitpp::assert_eq("Lazy box 2d starts index 0", 0, plzbox2d->MinObjIndex());
			unitpp::assert_eq("Lazy box 2d covers 1 para", 1, plzbox2d->LimObjIndex());
			unitpp::assert_eq("Lazy box 2d object", rghvoSec[2], plzbox2d->Object());

			VwBox * pboxMid1d = plzbox2d->NextOrLazy();
			unitpp::assert_eq("para 5 survives after lazy box", pbox5, pboxMid1d);
			VwBox * pboxMid2d = pboxMid1d->NextOrLazy();
			unitpp::assert_eq("para 6 survives after lazy box", pbox6, pboxMid2d);

			// There's no lazy box 3..this code adapted from case 2, which has one more.

			VwLazyBox * plzbox4d = dynamic_cast<VwLazyBox *>(pboxMid2d->NextOrLazy());
			unitpp::assert_true("Lazy box 4d from reconverting", plzbox4d != NULL);
			unitpp::assert_eq("Lazy box 4d starts index 0", 0, plzbox4d->MinObjIndex());
			unitpp::assert_eq("Lazy box 4d covers 1 para", 1, plzbox4d->LimObjIndex());
			unitpp::assert_eq("Lazy box 4d object", rghvoSec[3], plzbox4d->Object());
			VerifyNotifier(plzbox4d, rghvoSec[3], kflidParas, 0);

			VwBox * pboxMid3d = plzbox4d->NextOrLazy();
			unitpp::assert_eq("para 8 survives after lazy box", pbox8, pboxMid3d);

			VwLazyBox * plzbox5d = dynamic_cast<VwLazyBox *>(pboxMid3d->NextOrLazy());
			unitpp::assert_true("Lazy box 5d from reconverting", plzbox5d != NULL);
			unitpp::assert_eq("Lazy box 5d starts index 2", 2, plzbox5d->MinObjIndex());
			unitpp::assert_eq("Lazy box 5d covers 2 paras", 4, plzbox5d->LimObjIndex());
			unitpp::assert_eq("Lazy box 5d object", rghvoSec[3], plzbox5d->Object());

			VwLazyBox * plzbox6d = dynamic_cast<VwLazyBox *>(plzbox5d->NextOrLazy());
			unitpp::assert_true("Lazy box 6d from reconverting", plzbox6d != NULL);
			unitpp::assert_eq("Lazy box 6d starts index 4", 4, plzbox6d->MinObjIndex());
			unitpp::assert_eq("Lazy box 6d covers 2 sections", 6, plzbox6d->LimObjIndex());
			unitpp::assert_eq("Lazy box 6d object", khvoBook, plzbox6d->Object());

			// Now expand fully again. We're going to try selections to prevent conversion
			hr = m_qrootb->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &xpdr);
			unitpp::assert_true("PrepareToDraw2 succeeded", hr == S_OK);

			m_qdrs->SetVisRanges(INT_MAX, INT_MAX, INT_MAX, INT_MAX);

			// Make an insertion point at the start
			IVwSelectionPtr qsel1;
			m_qrootb->MakeSimpleSel(true, true, false, false, &qsel1);
			VwBox * pbox7 = m_qrootb->FirstBox();
			for (int ipara = 0; ipara < 6; ipara++)
			{
				pbox7 = pbox7->NextOrLazy();
			}
			VwBox * pbox10 = pbox7->NextOrLazy()->NextOrLazy()->NextOrLazy();
			VwTextSelectionPtr qsel2;
			qsel2.Attach(NewObj VwTextSelection(dynamic_cast<VwParagraphBox *>(pbox7),
				0, 5, false, dynamic_cast<VwParagraphBox *>(pbox10)));
			pboxFirst = m_qrootb->FirstBox();

			m_qrootb->MaximizeLaziness();

			// paragraphs 1 and 7-10 (section 4, object 503) should survive because selected,
			// So we get paragraph 1, a lazy box for sections 501-502, paras 710,
			// and a lazy box for 504-505.
			unitpp::assert_eq("selection test keeps first para",
				pboxFirst, m_qrootb->FirstBox());

			VwLazyBox * plzbox1e = dynamic_cast<VwLazyBox *>(pboxFirst->NextOrLazy());
			unitpp::assert_true("Got lazy box 1e from reconverting", plzbox1e != NULL);
			unitpp::assert_eq("Lazy box 1e starts index 1", 1, plzbox1e->MinObjIndex());
			unitpp::assert_eq("Lazy box 1e covers 2 sections", 3, plzbox1e->LimObjIndex());
			unitpp::assert_eq("Lazy box 1e object", khvoBook, plzbox1e->Object());

			unitpp::assert_eq("selection test keeps para 7", pbox7, plzbox1e->NextOrLazy());

			VwBox * pboxMid1e = pbox7->NextOrLazy()->NextOrLazy()->NextOrLazy();
			unitpp::assert_eq("selection test keeps para 10", pbox10, pboxMid1e);

			VwLazyBox * plzbox2e = dynamic_cast<VwLazyBox *>(pboxMid1e->NextOrLazy());
			unitpp::assert_true("Got lazy box 2e from reconverting", plzbox2e != NULL);
			unitpp::assert_eq("Lazy box 2e starts index 4", 4, plzbox2e->MinObjIndex());
			unitpp::assert_eq("Lazy box 2e covers 2 secs", 6, plzbox2e->LimObjIndex());
			unitpp::assert_eq("Lazy box 2e object", khvoBook, plzbox2e->Object());

			unitpp::assert_true("lazy box 2e ends seq", plzbox2e->NextOrLazy() == NULL);

			// Verify we can still expand it all again. This helps ensure we collapsed it right!
			hr = m_qrootb->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &xpdr);
			unitpp::assert_true("PrepareToDraw2 succeeded", hr == S_OK);
		}

		void VerifytestReverseLaziness2Results(VwRootBox * prootb, VwDivBox * pdboxBook,
			VwDivBox * pdboxSec3, VwBox * pboxPara4, HVO hvoSec2)
		{
			// Verify that we have the expected set of lazy boxes (except for para 4).
			unitpp::assert_eq("book div box still present", pdboxBook, prootb->FirstBox());
			unitpp::assert_eq("book div box still whole contents",
				pdboxBook, prootb->LastBox());
			VwLazyBox * plzbox = dynamic_cast<VwLazyBox *>(pdboxBook->FirstBox());
			unitpp::assert_true("Got a lazy box from start of div", plzbox != NULL);
			// The pattern is section 1: 1 para; section 2: 2 paras.
			// So, before para 4, the first in section 3, we hope to find a single
			// lazy box for the first two sections.
			unitpp::assert_eq("first lazy box starts index 0", 0, plzbox->MinObjIndex());
			unitpp::assert_eq("first lazy box covers 2 sections", 2, plzbox->LimObjIndex());
			unitpp::assert_eq("first lazy box object", khvoBook, plzbox->Object());

			VwDivBox * pdboxMid = dynamic_cast<VwDivBox *>(plzbox->NextOrLazy());
			unitpp::assert_eq("div after lazy box", pdboxSec3, pdboxMid);
			unitpp::assert_eq("real para starts sec 3 div", pboxPara4, pdboxMid->FirstBox());
			VwLazyBox * plzbox2 = dynamic_cast<VwLazyBox *>(pdboxMid->FirstBox()->NextOrLazy());
			unitpp::assert_true("Got 2nd lazy box from reconverting", plzbox2 != NULL);
			// There are three paragraphs in section 3; this one should cover the last 2.
			unitpp::assert_eq("2nd lazy box starts index 1", 1, plzbox2->MinObjIndex());
			unitpp::assert_eq("2nd lazy box covers 2 paras", 3, plzbox2->LimObjIndex());
			unitpp::assert_eq("2nd lazy box object", hvoSec2, plzbox2->Object());
			unitpp::assert_true("Nothing follows 2nd lazy box", plzbox2->NextOrLazy() == NULL);

			VwLazyBox * plzbox3 = dynamic_cast<VwLazyBox *>(pdboxMid->NextOrLazy());
			unitpp::assert_true("Got 3rd lazy box from reconverting", plzbox3 != NULL);
			// There are three more sections
			unitpp::assert_eq("3rd lazy box starts index 3", 3, plzbox3->MinObjIndex());
			unitpp::assert_eq("3rd lazy box covers 3 sections", 6, plzbox3->LimObjIndex());
			unitpp::assert_eq("3rd lazy box object", khvoBook, plzbox3->Object());
			unitpp::assert_eq("3rd lazy box object ends div", plzbox3, pdboxBook->LastBox());
			unitpp::assert_true("Nothing follows 3rd lazy box", NULL == plzbox3->NextOrLazy());
		}

		void testReverseLaziness2()
		{
			// Create test data. We simulate a book with 6 sections containing 1 to 6 paragraphs
			// each.
			ITsStringPtr qtss;
			StrUni stuPara;
			int hvoPara = khvoParaMin;
			HVO rghvoPara[kcSection];
			HVO rghvoSec[kcSection];
			for (int isec = 0; isec < kcSection; isec++)
			{
				for (int i = 0; i < isec + 1; i++)
				{
					stuPara.Format(L"This is paragraph %d", i);
					m_qcda->CacheStringProp(hvoPara, kflidStTxtPara_Contents, qtss);
					rghvoPara[i] = hvoPara;
					hvoPara++;
				}
				m_qcda->CacheVecProp(isec + khvoSecMin, kflidParas, rghvoPara, isec + 1);
				rghvoSec[isec] = isec + khvoSecMin;
			}
			m_qcda->CacheVecProp(khvoBook, kflidSections, rghvoSec, kcSection);

			m_qvc.Attach(NewObj DummyVcBkSecParaDiv(kflidStTxtPara_Contents, false));
			m_qrootb->SetRootObject(khvoBook, m_qvc, kfragBook, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);

			IVwRootBoxPtr qrootbSyncT;
			try
			{
				// Make a second root box with the same data and synchronize it with the main one
				// to test keeping things synchronized.
				VwRootBox::CreateCom(NULL, IID_IVwRootBox, (void **) &qrootbSyncT);

				VwRootBoxPtr qrootbSync = dynamic_cast<VwRootBox *>(qrootbSyncT.Ptr());
				qrootbSync->putref_DataAccess(m_qsda);
				qrootbSync->SetRootObject(khvoBook, m_qvc, kfragBook, NULL);

				// It needs its own dummy root site, otherwise, coordinate adjustments happen twice in the site.
				DummyRootSitePtr qdrsSync;
				qdrsSync.Attach(NewObj DummyRootSite());
				qdrsSync->SetRects(m_rcSrc, m_rcSrc);
				qdrsSync->SetGraphics(m_qvg32);
				qrootbSync->SetSite(qdrsSync);

				hr = qrootbSync->Layout(m_qvg32, 300);
				unitpp::assert_true("Layout succeeded", hr == S_OK);

				IVwSynchronizerPtr qsync;
				VwSynchronizer::CreateCom(NULL, IID_IVwSynchronizer, (void **) &qsync);
				qsync->AddRoot(m_qrootb);
				qsync->AddRoot(qrootbSync);

				VwPrepDrawResult xpdr;
				// This should expand everything fully. The sample is only about 360 pixels high, much less
				// than any likely developer's screen.
				hr = m_qrootb->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &xpdr);
				unitpp::assert_true("PrepareToDraw succeeded", hr == S_OK);
				// Arbitrarily pick the 4th paragraph.
				VwDivBox * pdboxBook = dynamic_cast<VwDivBox *>(m_qrootb->FirstBox());
				unitpp::assert_true("Got a div box for book", pdboxBook != NULL);
				VwDivBox * pdboxSec3 = dynamic_cast<VwDivBox *>(
					pdboxBook->FirstBox()->NextOrLazy()->NextOrLazy());
				unitpp::assert_true("Got a div box for sec3", pdboxSec3 != NULL);
				VwBox * pboxPara4 = pdboxSec3->FirstBox();
				unitpp::assert_true("Got a box for para 4", pboxPara4 != NULL);

				VwDivBox * pdboxBookSync = dynamic_cast<VwDivBox *>(qrootbSync->FirstBox());
				unitpp::assert_true("Got a div box for book sync", pdboxBookSync != NULL);
				VwDivBox * pdboxSec3Sync = dynamic_cast<VwDivBox *>(
					pdboxBookSync->FirstBox()->NextOrLazy()->NextOrLazy());
				unitpp::assert_true("Got a div box for sec3 sync", pdboxSec3Sync != NULL);
				VwBox * pboxPara4Sync = pdboxSec3Sync->FirstBox();
				unitpp::assert_true("Got a box for para 4 sync", pboxPara4Sync != NULL);

				Rect rdPara4 = pboxPara4->GetBoundsRect(m_qvg32, m_rcSrc, m_rcSrc);
				m_qdrs->SetVisRanges(rdPara4.TopLeft().y + 1, rdPara4.BottomRight().y - 1,
					INT_MAX, INT_MAX);

				m_qrootb->MaximizeLaziness();
				// Verify that we have the expected set of lazy boxes (except for para 4).
				VerifytestReverseLaziness2Results(m_qrootb, pdboxBook,
					pdboxSec3, pboxPara4, rghvoSec[2]);
				VerifytestReverseLaziness2Results(qrootbSync, pdboxBookSync,
					pdboxSec3Sync, pboxPara4Sync, rghvoSec[2]);

				// Try using a deeply-embedded large selection to prevent conversion.
				hr = m_qrootb->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &xpdr);
				unitpp::assert_true("PrepareToDraw2 succeeded", hr == S_OK);

				m_qdrs->SetVisRanges(INT_MAX, INT_MAX, INT_MAX, INT_MAX);

				VwDivBox * pdboxSec502 = dynamic_cast<VwDivBox *>(pdboxBook->FirstBox()->NextOrLazy()->NextOrLazy());
				VwDivBox * pdboxSec503 = dynamic_cast<VwDivBox *>(pdboxSec502->NextOrLazy());
				VwBox * pboxPara1505 = pdboxSec502->LastBox();
				VwBox * pboxPara1506 = pdboxSec503->FirstBox();

				// We expect that everything is expanded now, so we shouldn't find any lazy boxes
				unitpp::assert_true("First section shouldn't be lazy",
					dynamic_cast<VwLazyBox*>(pdboxBook->FirstBox()) == NULL);
				unitpp::assert_true("Second section shouldn't be lazy",
					dynamic_cast<VwLazyBox*>(pdboxBook->FirstBox()->NextOrLazy()) == NULL);
				unitpp::assert_true("Third section shouldn't be lazy",
					dynamic_cast<VwLazyBox*>(pdboxSec502) == NULL);
				unitpp::assert_true("Fourth section shouldn't be lazy",
					dynamic_cast<VwLazyBox*>(pdboxSec503) == NULL);

				VwTextSelectionPtr qsel;
				qsel.Attach(NewObj VwTextSelection(dynamic_cast<VwParagraphBox *>(pboxPara1506),
					0, 5, false, dynamic_cast<VwParagraphBox *>(pboxPara1505)));

				// Under current rules this prevents converting everything in sections 3 and 4.
				// Note: in principle it would be OK to convert paras 1503, 1504, and 1507-1509.
				// We currently don't do this just to keep the code a little simpler.
				m_qrootb->MaximizeLaziness();

				unitpp::assert_eq("book div box still present b", pdboxBook, m_qrootb->FirstBox());
				unitpp::assert_eq("book div box still whole contents b", pdboxBook, m_qrootb->LastBox());
				VwLazyBox * plzbox1b = dynamic_cast<VwLazyBox *>(pdboxBook->FirstBox());
				unitpp::assert_true("Got a lazy box from start of div 1b", plzbox1b != NULL);
				unitpp::assert_eq("lazy box 1b starts index 0", 0, plzbox1b->MinObjIndex());
				unitpp::assert_eq("lazy box 1b covers 2 sections", 2, plzbox1b->LimObjIndex());
				unitpp::assert_eq("lazy box 1b object", khvoBook, plzbox1b->Object());

				VwDivBox * pdboxMid1b = dynamic_cast<VwDivBox *>(plzbox1b->NextOrLazy());
				unitpp::assert_eq("div after lazy box 1b", pdboxSec502, pdboxMid1b);
				unitpp::assert_eq("sec 3 div survives 1b", pdboxSec503, pdboxMid1b->NextOrLazy());

				VwLazyBox * plzbox2b = dynamic_cast<VwLazyBox *>(pdboxSec503->NextOrLazy());
				unitpp::assert_true("Got a lazy box 2b", plzbox2b != NULL);
				unitpp::assert_eq("lazy box 2b starts index 4", 4, plzbox2b->MinObjIndex());
				unitpp::assert_eq("lazy box 2b covers 2 sections", 6, plzbox2b->LimObjIndex());
				unitpp::assert_eq("lazy box 2b object", khvoBook, plzbox2b->Object());

				unitpp::assert_true("Nothing follows lazy box2b", NULL == plzbox2b->NextOrLazy());

				unitpp::assert_eq("embedded para at end not destroyed", pboxPara1505, pdboxSec502->LastBox());
				unitpp::assert_eq("embedded para at start not destroyed", pboxPara1506, pdboxSec503->FirstBox());

				// Verify we can still expand it all again. This helps ensure we collapsed it right!
				hr = m_qrootb->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &xpdr);
				unitpp::assert_true("PrepareToDraw2 succeeded", hr == S_OK);
			}
			catch(...)
			{
				qrootbSyncT->Close();
				throw;
			}

			// Do some clean-up
			qrootbSyncT->Close();
		}

		// Tests that we are expanding only one div box
		void testExpandingOneDivBox()
		{
			// Create test data. We simulate two books with 16 sections containing 16 paragraphs
			// each.
			HVO rghvoSec[16 * 2];
			CreateTestBooksWithSections(m_qcda, rghvoSec);

			DummyVcBkSecParaDivPtr qdvbsvc;
			qdvbsvc.Attach(NewObj DummyVcBkSecParaDiv());
			m_qvc = qdvbsvc;
			m_qrootb->SetRootObject(khvoScripture, m_qvc, kfragScripture, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);

			VwPrepDrawResult xpdr;
			// set a scroll position
			Rect rcDest = m_rcSrc;
			rcDest.Offset(0, -4000);

			// Expand one screen full
			hr = m_qrootb->PrepareToDraw(m_qvg32, m_rcSrc, rcDest, &xpdr);

			// ENHANCE: if this fails, make sure top of secondary monitor is no higher than
			// the top of the primary monitor because this will result in a negative Y
			// component of the clip rectangle.
			// Could possibly also be affected by different screen resolutions (was
			// made at 1600x1200)
			unitpp::assert_eq("First expansion should be the 4th section", rghvoSec[19],
				qdvbsvc->rgLoadedSections[0]);
			unitpp::assert_eq("Second expansion should be the 5th section", rghvoSec[20],
				qdvbsvc->rgLoadedSections[1]);
			unitpp::assert_eq("Third expansion should be the 6th section", rghvoSec[21],
				qdvbsvc->rgLoadedSections[2]);
			unitpp::assert_eq("Fourth expansion should be the 4th section", rghvoSec[22],
				qdvbsvc->rgLoadedSections[3]);
		}

		// This test reveals a bug (TE-348) that occurred when the last item in a sequence
		// that is displayed lazily generates no boxes. The example here is that the sequence of
		// sections is displayed lazily, the display of a section is just a sequence of
		// paragraphs, and the last section has no paragraphs. The problem only occurs if the
		// last item is expanded in isolation, that is, earlier items have already been
		// expanded.
		void testEmptyLastLazyItem()
		{
			// Create test data. We simulate a book with 2 sections, but only the first has a
			// paragraph.
			//_CrtSetDbgFlag(_CRTDBG_CHECK_ALWAYS_DF);
			ITsStringPtr qtss;
			StrUni stuPara;
			HVO rghvoSec[2];
			stuPara.Format(L"This is paragraph 1");
			m_qtsf->MakeString(stuPara.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoParaMin, kflidStTxtPara_Contents, qtss);
			HVO hvoPara = khvoParaMin;
			m_qcda->CacheVecProp(khvoSecMin, kflidParas, &hvoPara, 1);
			rghvoSec[0] = khvoSecMin;
			rghvoSec[1] = khvoSecMin + 1;
			// section[1] has no paragraphs so we needn't initialize that property.
			m_qcda->CacheVecProp(khvoBook, kflidSections, rghvoSec, 2);

			DummyVcBkSecParaPtr qdvbsvc;
			qdvbsvc.Attach(NewObj DummyVcBkSecPara());
			m_qvc = qdvbsvc;
			// Estimate that sections are very large, so initially the code only tries to expand
			// one.  Then when it isn't really that big, it expands the last one separately.
			qdvbsvc->m_dypEstSectionHeight = 60000;
			m_qrootb->SetRootObject(khvoBook, m_qvc, kfragBook, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);
			VwPrepDrawResult xpdr;
			// This should expand everything fully. The sample is only about 20 pixels high,
			// much less than any likely developer's screen.
			hr = m_qrootb->PrepareToDraw(m_qvg32, m_rcSrc, m_rcSrc, &xpdr);
			unitpp::assert_true("PrepareToDraw succeeded", hr == S_OK);
		}

		static void CreateTestBooksWithSections(IVwCacheDa* pcda, HVO* rghvoSec,
			int paraFlid = kflidStTxtPara_Contents, int cBooks = 2)
		{
			const int nSections = 16;
			ITsStringPtr qtss;
			StrUni stuPara;
			int hvoPara = khvoParaMin;
			unitpp::assert_true("Max 66 books. Bad test!", cBooks <= 66);
			HVO rghvoBooks[66];
			HVO rghvoPara[nSections];
			for (int ibook = 0; ibook < cBooks; ibook++)
			{
				for (int isec = 0; isec < nSections; isec++)
				{
					for (int i = 0; i < nSections; i++)
					{
						stuPara.Format(L"This is paragraph %d", i);
						pcda->CacheStringProp(hvoPara, paraFlid, qtss);
						rghvoPara[i] = hvoPara;
						hvoPara++;
					}
					rghvoSec[nSections * ibook + isec] = khvoSecMin + nSections * ibook + isec;
					pcda->CacheVecProp(rghvoSec[nSections * ibook + isec], kflidParas, rghvoPara,
						nSections);
				}
				pcda->CacheVecProp(khvoBook + ibook, kflidSections, &rghvoSec[nSections * ibook],
					nSections);
				rghvoBooks[ibook] = khvoBook + ibook;
			}
			pcda->CacheVecProp(khvoScripture, kflidBooks, rghvoBooks, cBooks);
		}

		// Return the first lazy box in the root (if any).
		VwLazyBox * FindALazyBox()
		{
			VwBox * pbox = m_qrootb->FirstBox();
			for (; pbox && !dynamic_cast<VwLazyBox *>(pbox); pbox = pbox->NextOrLazy())
				;
			return dynamic_cast<VwLazyBox *>(pbox);
		}

		// Test that a PropChanged that causes a real box to turn back into a lazy one
		// does not change the overall size of the root box (and hence should not change
		// anything else in the view at all).
		// JohnT, 1 Feb 2008. The above still seems like a good idea, but I found a simpler
		// solution to the problem I was solving. Got this to the point where it seems a good
		// test (it correctly verifies that the first PropChanged DOES change the size of
		// the root box. However, I never implemented the actual enhancement. It will be a
		// somewhat tricky one, probably focused around the end of VwNotifier::UpdateLazyProp,
		// in the case where chvoIns and chvoDel are both 1, there is something to delete,
		// and the replacement is a lazy box. The tricky thing is reversing the calculation
		// that lays the boxes out in their pile so as to figure out the exact size the lazy
		// box needs to be.
		//void testConvertingRealToLazy()
		//{
		//	// Now make three strings, the contents of paragraphs 1 - 3.
		//	ITsStringPtr qtss;
		//	StrUni stuPara1(L"This is the first test paragraph");
		//	m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
		//	HVO hvoPara1 = 1;
		//	m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss);
		//	StrUni stuPara2(L"This is the second test paragraph");
		//	m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
		//	HVO hvoPara2 = 2;
		//	m_qcda->CacheStringProp(hvoPara2, kflidStTxtPara_Contents, qtss);
		//	StrUni stuPara3(L"This is the third test paragraph");
		//	m_qtsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtss);
		//	HVO hvoPara3 = 3;
		//	m_qcda->CacheStringProp(hvoPara3, kflidStTxtPara_Contents, qtss);

		//	HVO rghvoParas[3] = {hvoPara1, hvoPara2, hvoPara3};

		//	// Now make each of them the paragraphs of an StText.
		//	HVO hvoText1 = 101;
		//	m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, rghvoParas, 3);

		//	m_qvc.Attach(NewObj DummyVc3());
		//	DummyVc3 * pvc3 = dynamic_cast<DummyVc3 *>(m_qvc.Ptr());
		//	m_qrootb->SetRootObject(hvoText1, m_qvc, 1, NULL);
		//	HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
		//	unitpp::assert_true("Layout succeeded", hr == S_OK);

		//	VwBox * pbox = FindALazyBox();
		//	unitpp::assert_true("Found lazy box", pbox != NULL);
		//	VwLazyBox *plzbox = dynamic_cast<VwLazyBox *>(pbox);
		//	plzbox->ExpandItems(0, 3);
		//	AssertObj(m_qrootb);
		//	unitpp::assert_true("All expanded", FindALazyBox() == NULL);
		//	int dyOriginalHeight;
		//	m_qrootb->get_Height(&dyOriginalHeight);

		//	// ----simple middle----
		//	m_qrootb->PropChanged(hvoText1, kflidStText_Paragraphs, 1, 1, 1);

		//	int dypNewHeight;
		//	m_qrootb->get_Height(&dypNewHeight);
		//	unitpp::assert_eq("Simple replace middle did not change height", dyOriginalHeight, dypNewHeight);

		//	// Remove the lazy box for next test
		//	plzbox->ExpandItems(0, 1);
		//	unitpp::assert_true("All expanded", FindALazyBox() == NULL);

		//	// ----simple first----
		//	m_qrootb->PropChanged(hvoText1, kflidStText_Paragraphs, 0, 1, 1);

		//	m_qrootb->get_Height(&dypNewHeight);
		//	unitpp::assert_eq("Simple replace first did not change height", dyOriginalHeight, dypNewHeight);

		//	// Remove the lazy box for next test
		//	plzbox->ExpandItems(0, 1);
		//	unitpp::assert_true("All expanded", FindALazyBox() == NULL);

		//	// ----simple last----
		//	m_qrootb->PropChanged(hvoText1, kflidStText_Paragraphs, 2, 1, 1);

		//	m_qrootb->get_Height(&dypNewHeight);
		//	unitpp::assert_eq("Simple replace last did not change height", dyOriginalHeight, dypNewHeight);

		//	// Try with margins
		//	pvc3->m_dypMarginBottom = 5000;
		//	pvc3->m_dypMarginTop = 4000;
		//	m_qrootb->Reconstruct();
		//	FindALazyBox()->ExpandItems(0, 3);
		//	m_qrootb->get_Height(&dyOriginalHeight);

		//	m_qrootb->PropChanged(hvoText1, kflidStText_Paragraphs, 1, 1, 1);

		//	m_qrootb->get_Height(&dypNewHeight);
		//	unitpp::assert_eq("Replace middle w. top and bottom margins did not change height", dyOriginalHeight, dypNewHeight);

		//	// And with mswMargin...
		//	pvc3->m_dypMswMarginTop = 3000;
		//	m_qrootb->Reconstruct();
		//	FindALazyBox()->ExpandItems(0, 3);
		//	m_qrootb->get_Height(&dyOriginalHeight);

		//	m_qrootb->PropChanged(hvoText1, kflidStText_Paragraphs, 1, 1, 1);

		//	m_qrootb->get_Height(&dypNewHeight);
		//	unitpp::assert_eq("Replace middle w. all three margins did not change height", dyOriginalHeight, dypNewHeight);
		//}
	public:
		TestLazyBox();

		virtual void Setup()
		{
			CreateTestWritingSystemFactory();
			m_qcda.CreateInstance(CLSID_VwCacheDa);
			m_qcda->QueryInterface(IID_ISilDataAccess, (void **)&m_qsda);
			m_qsda->putref_WritingSystemFactory(g_qwsf);

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
			m_qvg32->Initialize(m_hdc);
			m_qrootb->putref_DataAccess(m_qsda);
			m_qdrs.Attach(NewObj DummyRootSite());
			m_rcSrc = Rect(0, 0, 96, 96);
			m_qdrs->SetRects(m_rcSrc, m_rcSrc);
			m_qdrs->SetGraphics(m_qvg32);
			m_qrootb->SetSite(m_qdrs);

			// Set the clip rectangle so that we have a defined one that the tests expects. This
			// is important when running from a service and probably also when running with
			// differing screen resolutions
			Rect clipRect(0, 0, 1680, 1050);
			m_qvg32->SetClipRect(&clipRect);

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
	};
}

#endif /*TestLazyBox_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
