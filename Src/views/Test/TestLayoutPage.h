/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestLayoutPage.h
Responsibility:

	Unit tests for the LayoutPageMethod class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLAYOUTPAGE_H_INCLUDED
#define TESTLAYOUTPAGE_H_INCLUDED

#pragma once

#include "testViews.h"
#include "Vector_i.cpp"
#include "TestVwSync.h"

#define khvoTlpPara1 1001
#define khvoTlpPara2 1002
#define khvoTlpPara3 1003
#define khvoTlpPara4 1004

namespace TestViews
{
	// {8D159FEC-73AE-48da-AB76-A1CFC247E43D}
	DEFINE_GUID(kguidDummyFootnote1,
		0x8d159fec, 0x73ae, 0x48da, 0xab, 0x76, 0xa1, 0xcf, 0xc2, 0x47, 0xe4, 0x3d);
	// {94F992EA-3F9A-4d51-B081-189BAF57FBC7}
	DEFINE_GUID(kguidDummyFootnote2,
		0x94f992ea, 0x3f9a, 0x4d51, 0xb0, 0x81, 0x18, 0x9b, 0xaf, 0x57, 0xfb, 0xc7);
	// {1A4EA9B7-4D65-4e21-AE2D-EDB5BD9E0937}
	DEFINE_GUID(kguidDummyFootnote3,
		0x1a4ea9b7, 0x4d65, 0x4e21, 0xae, 0x2d, 0xed, 0xb5, 0xbd, 0x9e, 0x9, 0x37);
	// {8C282EEF-D50B-4743-A8DF-8FE38F41070C}
	DEFINE_GUID(kguidDummyFootnote4,
		0x8c282eef, 0xd50b, 0x4743, 0xa8, 0xdf, 0x8f, 0xe3, 0x8f, 0x41, 0x7, 0xc);

	const HVO hvoRoot = 101;

	struct AddDependentObjectsArgs
	{
		IVwLayoutStream * play;
		int hPage;
		int cguid;
		Vector<GUID> vguid;
		ComBool fAllowFail;
		int dysAvailHeight;
	};
#if WIN32
	template Vector<AddDependentObjectsArgs>;
#endif

	class DummyLayoutMgr : public IVwLayoutManager
	{
	public:
		long m_cref;
		Vector<AddDependentObjectsArgs> m_vAddDepObjsArgs;
		int m_cCall;
		int m_nScenario;

		virtual ~DummyLayoutMgr()
		{
			ModuleEntry::ModuleRelease();
		}
		STDMETHOD(QueryInterface)(REFIID riid, void ** ppv)
		{
			AssertPtr(ppv);
			if (!ppv)
				return WarnHr(E_POINTER);
			*ppv = NULL;

			if (riid == IID_IUnknown)
				*ppv = static_cast<IUnknown *>(this);
			else if (riid == IID_IVwLayoutManager)
				*ppv = static_cast<IVwLayoutManager *>(this);
			else
				return E_NOINTERFACE;

			AddRef();
			return NOERROR;
		}
		STDMETHOD_(UCOMINT32, AddRef)(void)
		{
			return InterlockedIncrement(&m_cref);
		}
		STDMETHOD_(UCOMINT32, Release)(void)
		{
			long cref = InterlockedDecrement(&m_cref);
			if (cref == 0) {
				m_cref = 1;
				delete this;
			}
			return cref;
		}

		DummyLayoutMgr()
		{
			m_cref = 1;
			ModuleEntry::ModuleAddRef();
			m_vAddDepObjsArgs.Clear();
			m_cCall = 0;
			m_nScenario = 0;
		}


		STDMETHOD(AddDependentObjects)(
			IVwLayoutStream * play,
			IVwGraphics * pvg,
			int hPage,
			int cguid,
			GUID * prgguidObj,
			ComBool fAllowFail,
			ComBool * pfFailed,
			int * pdysAvailHeight)
		{
			AddDependentObjectsArgs args;
			args.play = play;
			args.hPage = hPage;
			args.cguid = cguid;
			args.vguid.Resize(cguid);
			memcpy(args.vguid.Begin(), prgguidObj, 16 * cguid);
			args.fAllowFail = fAllowFail;
			args.dysAvailHeight = *pdysAvailHeight;

			m_vAddDepObjsArgs.Push(args);

			if (m_nScenario == 0)
			{
				switch (m_cCall++)
				{
				// PAGE 1
				case 0: // two footnotes (1st para)
					*pfFailed = false;
					// Pretend this footnote takes almost all available height
					*pdysAvailHeight = 1;
					break;
				// PAGE 2
				case 1: // footnote at end of first para
					*pfFailed = true;
					break;
				// PAGE 3
				case 2: // This is a retry of the previous failed attempt
					*pfFailed = false;
					// Pretend this footnote takes 10% of available height
					*pdysAvailHeight = (int)(*pdysAvailHeight * 0.9);
					break;
				case 3: // two footnotes (3rd para)
					*pfFailed = false;
					// Pretend this footnote takes 20% of available height
					*pdysAvailHeight = (int)(*pdysAvailHeight * 0.8);
					break;
				case 4: // final footnote
					*pfFailed = false;
					// Pretend this footnote takes 10% of available height
					*pdysAvailHeight = (int)(*pdysAvailHeight * 0.9);
					break;
				}
			}
			else if (m_nScenario == 1)
			{
				// we pretend each call succeeds, and every footnote is 17 pixels high
				*pfFailed = false;
				*pdysAvailHeight -= 17;
			}
			else if (m_nScenario == 2) // used in testBreakingOffsetTableRows
			{
				switch (m_cCall++)
				{
				// PAGE 1
				case 0: // two footnotes (1st line of first  in left columnpara)
					*pfFailed = false;
					// Pretend this footnote takes almost all available height
					*pdysAvailHeight = 1;
					break;
				// PAGE 2
				case 1: // offset first line in second cell.
					*pfFailed = false;
					// Pretend this footnote takes 10% of available height
					*pdysAvailHeight = (int)(*pdysAvailHeight * 0.9);
					break;
				case 2: // footnote at end of first para in left column
					*pfFailed = true;
					break;
				// PAGE 3
				case 3: // This is a retry of the previous failed attempt
					*pfFailed = false;
					// Pretend this footnote takes 10% of available height
					*pdysAvailHeight = (int)(*pdysAvailHeight * 0.9);
					break;
				default: // let the rest succeed
					*pfFailed = false;
					// Pretend this footnote takes 20% of available height
					*pdysAvailHeight = (int)(*pdysAvailHeight * 0.8);
					break;
				}
			}
			return S_OK;
		}

		STDMETHOD(PageBroken)(
			IVwLayoutStream * play,
			int hPage)
		{
			return E_NOTIMPL;
		}

		STDMETHOD(PageBoundaryMoved)(
			IVwLayoutStream * play,
			int hPage,
			int ichOld)
		{
			return E_NOTIMPL;
		}

		STDMETHOD(EstimateHeight)(
			int dxpWidth,
			int * pdxpHeight)
		{
			return E_NOTIMPL;
		}
	};
	DEFINE_COM_PTR(DummyLayoutMgr);

	class TestLayoutPage : public unitpp::suite
	{
		IVwCacheDaPtr m_qcda;
		ISilDataAccessPtr m_qsda;
		ITsStrFactoryPtr m_qtsf;
		IVwViewConstructorPtr m_qvc;
		IVwGraphicsWin32Ptr m_qvg32;
		HDC m_hdc;
		DummyRootSitePtr m_qdrs;
		Rect m_rcSrc;
		VwLayoutStreamPtr m_qlay;

	public:
		TestLayoutPage();

		// Create a couple single-run strings.
		void CreateBoringStrings()
		{
			// Now make two strings, the contents of two paragraphs.
			StrUni stuPara1(L"This is the first test paragraph that we are trying to lay out. We hope it's too long to fit on one line.");
			ITsStringPtr qtss;
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara1, kflidStTxtPara_Contents, qtss);

			StrUni stuPara2(L"This is another test paragraph");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara2, kflidStTxtPara_Contents, qtss);
		}

		void AddFootnoteToBldr(ITsStrBldr * ptsb, const GUID & guid, int ich)
		{
			OLECHAR rgchData[9];
			rgchData[0] = kodtOwnNameGuidHot;
			memcpy(&rgchData[1], &guid, 16);
			ITsTextPropsPtr qttp;
			TsRunInfo tri;
			ptsb->FetchRunInfo(0, &tri, &qttp);
			ITsPropsBldrPtr qtpb;
			qttp->GetBldr(&qtpb);
			StrUni stuData(rgchData, 9);
			qtpb->SetStrPropValue(ktptObjData, stuData.Bstr());
			qtpb->GetTextProps(&qttp);
			ptsb->ReplaceRgch(ich, ich, OleStringLiteral(L"\xfffc"), 1, qttp);
		}

		// Create strings containing footnotes. A longish string is cached as the TxtPara Contents of khvoTlpPara1,
		// with kguidDummyFootnote1 at offset 4, kguidDummyFootnote2 at 6, and kguidDummyFootnote3 at the end.
		// Also a short string is made the contents of the 'txtpara' khvoTlpPara2.
		void CreateStringsWithFootnotes()
		{
			StrUni stuPara1(L"This is the first test paragraph that we are trying to lay out. We hope it's too long to fit on one line.");
			ITsStringPtr qtss;
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			ITsStrBldrPtr qtsb;
			qtss->GetBldr(&qtsb);
			AddFootnoteToBldr(qtsb, kguidDummyFootnote1, 4);
			AddFootnoteToBldr(qtsb, kguidDummyFootnote2, 6);
			AddFootnoteToBldr(qtsb, kguidDummyFootnote3, stuPara1.Length() + 2); // The very end, after adding 2.
			qtsb->GetString(&qtss);
			m_qcda->CacheStringProp(khvoTlpPara1, kflidStTxtPara_Contents, qtss);

			StrUni stuPara2(L"This is another test paragraph");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara2, kflidStTxtPara_Contents, qtss);
		}

		// Create similar strings to CreateStringsWithFootnotes, but just one footnote near the beginning of the first string,
		// the contents of khvoTlpPara3: kguidDummyFootnote2 at offset 7.
		void CreateMoreStringsWithFootnotes()
		{
			StrUni stuPara1(L"This is the first test paragraph that we are trying to lay out. We hope it's too long to fit on one line.");
			ITsStringPtr qtss;
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			ITsStrBldrPtr qtsb;
			qtss->GetBldr(&qtsb);
			AddFootnoteToBldr(qtsb, kguidDummyFootnote4, 7);
			qtsb->GetString(&qtss);
			m_qcda->CacheStringProp(khvoTlpPara3, kflidStTxtPara_Contents, qtss);

			StrUni stuPara2(L"This is another test paragraph");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara4, kflidStTxtPara_Contents, qtss);
		}

		//Set up a couple useful StTexts (before calling this, create and cache strings).
		void CreateTestStTexts(int nParas)
		{
			// Now make them the paragraphs of an StText.
			HVO hvoStText1 = 501;
			HVO hvoStText2 = 502;
			HVO rghvoPara[4] = {khvoTlpPara1, khvoTlpPara2, khvoTlpPara3, khvoTlpPara4};
			m_qcda->CacheVecProp(hvoStText1, kflidStText_Paragraphs, rghvoPara, nParas);
			m_qcda->CacheVecProp(hvoStText2, kflidStText_Paragraphs, rghvoPara, nParas);

			// Create a section to own these two StTexts
			m_qcda->CacheObjProp(hvoRoot, ktagSection_Heading, hvoStText1);
			m_qcda->CacheObjProp(hvoRoot, ktagSection_Content, hvoStText2);
		}

		// A variant that puts paras 1 and 2 in the heading, 3 and 4 in the contents.
		void CreateTestStTextsForTable()
		{
			// Now make them the paragraphs of an StText.
			HVO hvoStText1 = 501;
			HVO hvoStText2 = 502;
			HVO rghvoPara[4] = {khvoTlpPara1, khvoTlpPara2, khvoTlpPara3, khvoTlpPara4};
			m_qcda->CacheVecProp(hvoStText1, kflidStText_Paragraphs, rghvoPara, 2);
			m_qcda->CacheVecProp(hvoStText2, kflidStText_Paragraphs, rghvoPara + 2, 2);

			// Create a section to own these two StTexts
			m_qcda->CacheObjProp(hvoRoot, ktagSection_Heading, hvoStText1);
			m_qcda->CacheObjProp(hvoRoot, ktagSection_Content, hvoStText2);
		}

		//Set up rootbox with a view constructor that uses non-zero margins.
		DummyParaVc * SetupRootWithMargins()
		{
			m_qvc.Attach(NewObj DummyParaVc());
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympDivBottomMargin = 4000;
			pdvc->m_dympDivTopMargin = 5000;
			pdvc->m_dympParaBottomMargin = 3000;
			pdvc->m_dympParaTopMargin = 2000;
			m_qlay->SetRootObject(hvoRoot, m_qvc, kfragDiv, NULL);
			return pdvc;
		}

		//Set up rootbox with a view constructor that uses no margins.
		DummyParaVc * SetupRootWithoutMargins(int rootFrag = kfragDiv)
		{
			m_qvc.Attach(NewObj DummyParaVc());
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympDivBottomMargin = 0;
			pdvc->m_dympDivTopMargin = 0;
			pdvc->m_dympParaBottomMargin = 0;
			pdvc->m_dympParaTopMargin = 0;
			m_qlay->SetRootObject(hvoRoot, m_qvc, rootFrag, NULL);
			return pdvc;
		}

		//Set up rootbox with a view constructor that uses lazy boxes.
		ParasVc * SetupRootWithLazyBoxes()
		{
			HVO hvoStText1 = 501;
			m_qvc.Attach(NewObj ParasVc());
			ParasVc * pdvc = dynamic_cast<ParasVc *>(m_qvc.Ptr());
			m_qlay->SetRootObject(hvoStText1, m_qvc, kfragRootLazy, NULL);
			return pdvc;
		}

		// There's a special case we need to be sure to handle where an explicit line
		// height setting causes a paragraph height to be greater than the sum of the height
		// of its contents plus its bottom margin.
		void testFindPageBreakLineHeight()
		{
			CreateBoringStrings();
			CreateTestStTexts(2);
			DummyParaVc * pvc = SetupRootWithoutMargins();
			pvc->m_dympLineSpace = 30000; // 30 point line space makes first para large.

			int dpiY;
			m_qvg32->get_YUnitsPerInch(&dpiY);

			const int kdxpPageWidth = 2000;
			m_qlay->ConstructAndLayout(m_qvg32, kdxpPageWidth);
			unitpp::assert_true("ConstructAndLayout constructed root box", m_qlay->PrivateIsConstructed());
			unitpp::assert_true("para 1 fits on 1 line", kdxpPageWidth > m_qlay->Width());

			// Check that the box at height 0 and 1 is the top paragraph.
			VwDivBox * pboxDiv1 = dynamic_cast<VwDivBox *>(m_qlay->FirstBox());
			VwParagraphBox * pboxFirst = dynamic_cast<VwParagraphBox *>(pboxDiv1->FirstBox());
			unitpp::assert_true("first box is para", pboxFirst != NULL);
			int dysOffsetIntoBox;
			VwBox * pboxZero = m_qlay->FindNonPileChildAtOffset(0, dpiY, &dysOffsetIntoBox);
			unitpp::assert_eq("box at position 0 is first para", pboxFirst,
				dynamic_cast<VwParagraphBox *>(pboxZero));
			unitpp::assert_eq("attempted break is exactly at top of box", 0, dysOffsetIntoBox);
			VwBox * pboxTwo = m_qlay->FindNonPileChildAtOffset(20, dpiY, &dysOffsetIntoBox);
			unitpp::assert_eq("box at position 20 is second para", pboxFirst->NextOrLazy(),
				dynamic_cast<VwParagraphBox *>(pboxTwo));
		}

		// Tests the FindNonPileChildAtOffset method when the view contains only one lazy box
		void testFindNonPileChildAtOffset_onlyOneLazyBox()
		{
			CreateBoringStrings();
			CreateTestStTexts(2);
			SetupRootWithLazyBoxes();

			int dpiY;
			m_qvg32->get_YUnitsPerInch(&dpiY);

			const int kdxpPageWidth = 2000;
			m_qlay->ConstructAndLayout(m_qvg32, kdxpPageWidth);
			unitpp::assert_true("ConstructAndLayout should construct root box", m_qlay->PrivateIsConstructed());

			int dysOffsetIntoBox;
			VwBox * pboxFound = m_qlay->FindNonPileChildAtOffset(0, dpiY, &dysOffsetIntoBox);
			unitpp::assert_true("should have found a box", pboxFound != NULL);
		}

		void testFindPageBreakStuffNoMargins()
		{
			CreateBoringStrings();
			CreateTestStTexts(2);
			SetupRootWithoutMargins();

			int dpiY;
			m_qvg32->get_YUnitsPerInch(&dpiY);

			int dysUsedHeight, ysStartNextPage;
			const int kdxpPageWidth = 200;
			m_qlay->ConstructAndLayout(m_qvg32, kdxpPageWidth);
			unitpp::assert_true("ConstructAndLayout constructed root box", m_qlay->PrivateIsConstructed());
			unitpp::assert_eq("ConstructAndLayout set right width", kdxpPageWidth, m_qlay->Width());

			// Check that the box at height 0 and 1 is the top paragraph.
			VwDivBox * pboxDiv1 = dynamic_cast<VwDivBox *>(m_qlay->FirstBox());
			VwParagraphBox * pboxFirst = dynamic_cast<VwParagraphBox *>(pboxDiv1->FirstBox());
			unitpp::assert_true("first box is para", pboxFirst != NULL);
			int dysOffsetIntoBox;
			VwBox * pboxZero = m_qlay->FindNonPileChildAtOffset(0, dpiY, &dysOffsetIntoBox);
			unitpp::assert_eq("box at position 0 is first para", pboxFirst,
				dynamic_cast<VwParagraphBox *>(pboxZero));
			unitpp::assert_eq("attempted break is exactly at top of box", 0, dysOffsetIntoBox);
			VwBox * pboxOne = m_qlay->FindNonPileChildAtOffset(1, dpiY, &dysOffsetIntoBox);
			unitpp::assert_eq("box at position 1 is first para", pboxFirst,
				dynamic_cast<VwParagraphBox *>(pboxOne));
			unitpp::assert_eq("attempted break is 1 pixel from top of box", 1, dysOffsetIntoBox);
			// Check ability to find the right line at height 0 and 1
			VwBox * pboxFirstOnLine;
			VwBox * pboxLastOnLine;
			int ysTopOfLine, ysBottomOfLine;
			Vector<int> vColHeights;
			Vector<int> vColOverlaps;
			LayoutPageMethod lpm1(m_qlay, m_qvg32, kdxpPageWidth, 400, 0, 0, 1, &vColHeights, &vColOverlaps, &dysUsedHeight,
				&ysStartNextPage);
			lpm1.FindFirstLineOnPage(0, &pboxFirstOnLine, &pboxLastOnLine, &ysTopOfLine, &ysBottomOfLine);
			unitpp::assert_eq("Found first string box on first line", pboxFirst->FirstBox(), pboxFirstOnLine);
			unitpp::assert_eq("Only one string box on first line", pboxFirstOnLine, pboxLastOnLine);
			unitpp::assert_eq("First line on page is at top of document", 0, ysTopOfLine);
			unitpp::assert_eq("Line 1 ends at bottom of text of first line", pboxFirstOnLine->Bottom(), ysBottomOfLine);
			lpm1.FindFirstLineOnPage(1, &pboxFirstOnLine, &pboxLastOnLine, &ysTopOfLine, &ysBottomOfLine);
			unitpp::assert_eq("Found first string box on first line", pboxFirst->FirstBox(), pboxFirstOnLine);
			unitpp::assert_eq("First line on page is at top of document", 0, ysTopOfLine);
			unitpp::assert_eq("Line 1 ends at bottom of text of first line", pboxFirstOnLine->Bottom(), ysBottomOfLine);

			// Check that one line down into the paragraph still finds that paragraph.
			VwBox * pboxFirstLinePara1 = pboxFirst->FirstBox();
			VwBox * pboxSecondLinePara1 = pboxFirstLinePara1->NextRealBox();
			int dysTopSecondLine = pboxSecondLinePara1->Top();
			VwBox * pboxLine2 = m_qlay->FindNonPileChildAtOffset(dysTopSecondLine, dpiY, &dysOffsetIntoBox);
			unitpp::assert_eq("box at top of line 2 is first para", pboxFirst,
				dynamic_cast<VwParagraphBox *>(pboxLine2));
			// And very near the bottom of it.
			VwBox * pboxEndPara1 = m_qlay->FindNonPileChildAtOffset(pboxFirst->Height() - 1, dpiY, &dysOffsetIntoBox);
			unitpp::assert_eq("box at bottom of para 1 is first para", pboxFirst,
				dynamic_cast<VwParagraphBox *>(pboxEndPara1));
			// Check ability to find the second line
			lpm1.FindFirstLineOnPage(dysTopSecondLine, &pboxFirstOnLine, &pboxLastOnLine, &ysTopOfLine, &ysBottomOfLine);
			unitpp::assert_eq("Found first string box on second line", pboxFirst->FirstBox()->NextRealBox(), pboxFirstOnLine);
			unitpp::assert_eq("Second line on page is at top of line 2", dysTopSecondLine, ysTopOfLine);
			unitpp::assert_eq("Line 2 ends at bottom of second line", pboxFirstOnLine->Bottom(), ysBottomOfLine);

			// Check that a height in the second paragraph finds that.
			VwParagraphBox * pboxPara2 = dynamic_cast<VwParagraphBox *>(pboxFirst->NextRealBox());
			int dysTopPara2 = pboxPara2->Top();
			VwBox * pboxTopPara2 = m_qlay->FindNonPileChildAtOffset(dysTopPara2, dpiY, &dysOffsetIntoBox);
			unitpp::assert_eq("box at top of para 2 is 2nd para", pboxPara2,
				dynamic_cast<VwParagraphBox *>(pboxTopPara2));
			lpm1.FindFirstLineOnPage(dysTopPara2, &pboxFirstOnLine, &pboxLastOnLine, &ysTopOfLine, &ysBottomOfLine);
			unitpp::assert_eq("Found first string box in second paragraph", pboxPara2->FirstBox(), pboxFirstOnLine);
			unitpp::assert_eq("Found top of Paragraph 2", dysTopPara2, ysTopOfLine);
			unitpp::assert_eq("Line 3 ends at bottom of first line in Paragraph 2",
				pboxPara2->Top() + pboxPara2->FirstBox()->Bottom(), ysBottomOfLine);

			// Check that the top of the second division returns the first paragraph in it.
			VwDivBox * pboxDiv2 = dynamic_cast<VwDivBox *>(m_qlay->FirstBox()->NextRealBox());
			VwParagraphBox * pboxDiv2Para1 = dynamic_cast<VwParagraphBox *>(pboxDiv2->FirstBox());
			VwBox * pboxTopDiv2 = m_qlay->FindNonPileChildAtOffset(m_qlay->FirstBox()->Height() + 1, dpiY, &dysOffsetIntoBox);
			unitpp::assert_eq("box at top of division 2 is 1st section content para", pboxDiv2Para1,
				dynamic_cast<VwParagraphBox *>(pboxTopDiv2));
			unitpp::assert_eq("attempted break is 1 pixel from top of box", 1, dysOffsetIntoBox);

			// Check returns null when too large.
			// This would happen trying to lay out unnecessary pages simulated because of laziness.
			VwBox * pboxNull = m_qlay->FindNonPileChildAtOffset(m_qlay->Height() + 5, dpiY, &dysOffsetIntoBox);
			unitpp::assert_true("box beyond end is null", NULL == pboxNull);

			// Todo: try it with laziness, ensuring that ysStartPage gets adjusted.
			// Todo: try it with div boxes above the paragraph layer.



			//IVwSelectionPtr qselTemp;
			//// Select all of first paragraph
			//m_qrootb->MakeSimpleSel(true, true, true, true, &qselTemp);
			//// Get the selection string
			//ITsStringPtr qtssClip;
			//qselTemp->GetSelectionString(&qtssClip, L" ");
			//VerifyOrcData(qtssClip, 5, L"XX", g_pszFakeObjTextRep1, kodtEmbeddedObjectData);
			//VerifyOrcData(qtssClip, 11, L"XX", g_pszFakeObjTextRep2, kodtEmbeddedObjectData);
			//// Now paste the string back in, at the start.
			//m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp);
			//qselTemp->ReplaceWithTsString(qtssClip);
			//ITsStringPtr qtssResult;
			//StrUni stuGuid1((OLECHAR *)&g_GuidForMakeObjFromText1, 8); // makes null-terminated copy of guid.
			//StrUni stuGuid2((OLECHAR *)&g_GuidForMakeObjFromText2, 8);
			//ComBool f;
			//m_qrootb->Selection()->Commit(&f); // So we can retrieve current text from cache.
			//m_qsda->get_StringProp(khvoTlpPara1, kflidStTxtPara_Contents, &qtssResult);
			//VerifyOrcData(qtssResult, 5, L"\xfffc", stuGuid1.Chars(), kodtOwnNameGuidHot);
			//VerifyOrcData(qtssResult, 10, L"\xfffc", stuGuid2.Chars(), kodtOwnNameGuidHot);

			//// Offset within first line of paragraph finds the start of that line.
			//LayoutPageMethod(pvg, dxsAvailWidth, dysAvailHeight, pysStartPageBoundary, hPage, 1,
			//	pdysUsedHeight, pysStartPageBoundary).Run();
			//
			// Offset within second line of paragraph finds start of that line.

			// Offset in 2nd line of 'keep together' paragraph finds start of first line.

			// Offset in ist line of para before 'keep with next' + 'keep together' paragraph
			// Finds start of preceding paragraph.

			// 2nd line of KT para following two KT KWN paras finds start of first para.

			// Possibly: test finding a break point just before last line of para 1, where
			// para 1 is KWN (but, improbably, not KT) and para 2 is KT. (Do we really care
			// about paras that are KWN but not KT?)

			// Eventually: add tests and implementation for offsets in tables.

			//m_qlay->SetRootObject(m_configurer.MainObjectId, NewObj StVc(), kfrText,
			//	NULL);
		}

		void testFindPageBreakStuffMargins()
		{
			CreateBoringStrings();
			CreateTestStTexts(2);
			DummyParaVc * pdvc = SetupRootWithMargins();

			int dpiY;
			m_qvg32->get_YUnitsPerInch(&dpiY);
			//int dysDivBottomMargin = pdvc->m_dympDivBottomMargin * dpiY / 72000;
			int dysDivTopMargin = pdvc->m_dympDivTopMargin * dpiY / 72000;
			//int dysParaBottomMargin = pdvc->m_dympParaBottomMargin * dpiY / 72000;
			int dysParaTopMargin = pdvc->m_dympParaTopMargin * dpiY / 72000;
			int dysUsedHeight, ysStartNextPage;
			const int kdxpLayoutWidth = 200;
			m_qlay->ConstructAndLayout(m_qvg32, kdxpLayoutWidth);
			unitpp::assert_true("ConstructAndLayout constructed root box", m_qlay->PrivateIsConstructed());
			// Create test data in a temporary cache.
			unitpp::assert_eq("ConstructAndLayout set right width", 200, m_qlay->Width());

			// Check that the box at height 0 and 1 is the top paragraph.
			// (This amounts to a position in the margin of the division. The first non-pile box
			// is still found, but the offset indicates a position above it. Therefore the offset
			// relative to the box is negative.
			VwDivBox * pboxDiv1 = dynamic_cast<VwDivBox *>(m_qlay->FirstBox());
			VwParagraphBox * pboxFirst = dynamic_cast<VwParagraphBox *>(pboxDiv1->FirstBox());
			unitpp::assert_true("first box is para", pboxFirst != NULL);
			int dysOffsetIntoBox;
			VwBox * pboxZero = m_qlay->FindNonPileChildAtOffset(0, dpiY, &dysOffsetIntoBox);
			unitpp::assert_eq("box at position 0 is first para", pboxFirst,
				dynamic_cast<VwParagraphBox *>(pboxZero));
			unitpp::assert_eq("break at top of doc is div margin below doc", -dysDivTopMargin, dysOffsetIntoBox);
			VwBox * pboxOne = m_qlay->FindNonPileChildAtOffset(1, dpiY, &dysOffsetIntoBox);
			unitpp::assert_eq("box at position 1 is first para", pboxFirst,
				dynamic_cast<VwParagraphBox *>(pboxOne));
			unitpp::assert_eq("attempted break is 1 pixel from top of box", 1 - dysDivTopMargin, dysOffsetIntoBox);
			// Check ability to find the right line at height 1
			VwBox * pboxFirstOnLine;
			VwBox * pboxLastOnLine;
			int ysTopOfLine, ysBottomOfLine;
			Vector<int> vColHeights;
			Vector<int> vColOverlaps;
			LayoutPageMethod lpm1(m_qlay, m_qvg32, kdxpLayoutWidth, 200, 0, 0, 1, &vColHeights, & vColOverlaps, &dysUsedHeight,
				&ysStartNextPage);
			lpm1.FindFirstLineOnPage(1, &pboxFirstOnLine, &pboxLastOnLine, &ysTopOfLine, &ysBottomOfLine);
			unitpp::assert_eq("Found first string box on first line", pboxFirst->FirstBox(), pboxFirstOnLine);
			unitpp::assert_eq("Only one string box on first line", pboxFirstOnLine, pboxLastOnLine);
			unitpp::assert_eq("First line on page is at top of document", dysDivTopMargin + dysParaTopMargin, ysTopOfLine);
			unitpp::assert_eq("Line 1 ends at bottom of text of first line",
				dysDivTopMargin + pboxFirstOnLine->Bottom(), ysBottomOfLine);
			VwBox * pboxEndLine1;
			int ysBottomOfLine1 = lpm1.PrintableBottomOfLine(pboxFirstOnLine, &pboxEndLine1);
			unitpp::assert_eq("end of line 1 is also start", pboxFirstOnLine, pboxEndLine1);
			unitpp::assert_eq("bottom of line 1", dysDivTopMargin + pboxFirstOnLine->Bottom(), ysBottomOfLine1);
			// Check ability to find 1st line of 2nd div given a position in the margin above it.
			VwDivBox * pboxDiv2 = dynamic_cast<VwDivBox *>(pboxDiv1->NextRealBox());
			VwParagraphBox * pboxPara1Div2 = dynamic_cast<VwParagraphBox *>(pboxDiv2->FirstBox());
			lpm1.FindFirstLineOnPage(pboxDiv1->Bottom() - MulDiv(pboxDiv1->MarginBottom(), dpiY, kdzmpInch) + 1,
				&pboxFirstOnLine, &pboxLastOnLine, &ysTopOfLine, &ysBottomOfLine);
			unitpp::assert_eq("Found first line of div 2 from above",
				pboxPara1Div2->FirstBox(), pboxFirstOnLine);
			unitpp::assert_eq("Position of top of 1st line div2",
				pboxDiv2->Top() + dysDivTopMargin + dysParaTopMargin, ysTopOfLine);
			unitpp::assert_eq("Position of bottom of 1st line div2",
				pboxDiv2->Top() + dysDivTopMargin + pboxPara1Div2->FirstBox()->Bottom(), ysBottomOfLine);

			// Check ability to find a line that is not the first in the paragraph.
			// We'll use the second line of the first paragraph of div 2.
			// Also checks using a position within the range occupied by the line.
			int ysTarget = pboxDiv2->Top() + dysDivTopMargin + pboxPara1Div2->FirstBox()->Bottom() + 2;
			lpm1.FindFirstLineOnPage(ysTarget, &pboxFirstOnLine, &pboxLastOnLine, &ysTopOfLine, &ysBottomOfLine);
			VwBox * pboxLine2Para1Div2 = pboxPara1Div2->FirstBox()->NextRealBox();
			unitpp::assert_eq("Found 2nd line of div 2 from within",
				pboxLine2Para1Div2, pboxFirstOnLine);
			unitpp::assert_eq("Position of top of 2nd line div2",
				pboxDiv2->Top() + dysDivTopMargin + pboxPara1Div2->FirstBox()->Bottom(), ysTopOfLine);
			unitpp::assert_eq("Position of bottom of 2nd line div2",
				pboxDiv2->Top() + dysDivTopMargin + pboxLine2Para1Div2->Bottom(),
				ysBottomOfLine);

			VwBox * pboxEndLine2Para1Div2;
			int ysBottomOfL2p1d2 = lpm1.PrintableBottomOfLine(pboxLine2Para1Div2, &pboxEndLine2Para1Div2);
			unitpp::assert_eq("end of line 2 p1d2 is also start", pboxLine2Para1Div2, pboxEndLine2Para1Div2);
			unitpp::assert_eq("bottom of l2p1d2",
				pboxDiv2->Top() + dysDivTopMargin + pboxLine2Para1Div2->Bottom(), ysBottomOfL2p1d2);

			// Check ability to find line that is not in 1st para of div.
			// Use line 1 (probably the only line) of para2 of div 2.
			ysTarget = pboxDiv2->Top() + pboxPara1Div2->Bottom() + 2;
			lpm1.FindFirstLineOnPage(ysTarget, &pboxFirstOnLine, &pboxLastOnLine, &ysTopOfLine, &ysBottomOfLine);
			VwParagraphBox * pboxPara2Div2 = dynamic_cast<VwParagraphBox *>(pboxPara1Div2->NextRealBox());
			VwBox * pboxLine1Para2Div2 = pboxPara2Div2->FirstBox();
			unitpp::assert_eq("Found 1st line of para 2 of div 2 from within",
				pboxLine1Para2Div2, pboxFirstOnLine);
			unitpp::assert_eq("Position of top of 1st line para 2 div2",
				pboxDiv2->Top() + pboxPara2Div2->Top() + dysParaTopMargin, ysTopOfLine);

			int yTop, yBottom;
			Rect rcSrc(0,0,1,1); //Identity transform
			pboxPara2Div2->GetLineTopAndBottom(m_qvg32,pboxFirstOnLine,&yTop,&yBottom, rcSrc, rcSrc);

			unitpp::assert_eq("Position of bottom of 1st line para 2 div2",
				//pboxDiv2->Top() + pboxPara2Div2->Top() + pboxLine1Para2Div2->Bottom(),
				yBottom,
				ysBottomOfLine);

			// Check ability to NOT find line that is in very bottom margin area.
			ysTarget = pboxDiv2->Bottom() - 1;
			lpm1.FindFirstLineOnPage(ysTarget, &pboxFirstOnLine, &pboxLastOnLine, &ysTopOfLine, &ysBottomOfLine);
			unitpp::assert_true("Did NOT find a box", NULL == pboxFirstOnLine);

			// Check ability to NOT find line that is way past the end of the document.
			lpm1.FindFirstLineOnPage(8000, &pboxFirstOnLine, &pboxLastOnLine, &ysTopOfLine, &ysBottomOfLine);
			unitpp::assert_true("Did NOT find a box", NULL == pboxFirstOnLine);
		}

		void testBreakingAlignedTableRows()
		{
			// Set up a test view, a table with one row and two cells. The first cell contains the
			// heading StText, with khvoPara1 and khvoPara2, the first of which has three footnotes.
			// The second cell has the contents of the root section, with khvoPara3 (with a footnote)
			// and khvoPara4.
			CreateStringsWithFootnotes();
			CreateMoreStringsWithFootnotes();
			CreateTestStTextsForTable();
			SetupRootWithoutMargins(kfragAlignedTable);
			int dpiY;
			m_qvg32->get_YUnitsPerInch(&dpiY);
			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight, ysStartNextPage;

			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 200, 200, &ysStartThisPage, 0, 1, &dysUsedHeight,
				&ysStartNextPage);

			int cCalls = qlayoutMgr->m_vAddDepObjsArgs.Size();

			// By default the dummy layout manager, on the first call, claims the footnotes on the first line
			// used almost all the available space. Adding the second line is therefore not possible even
			// without the third footnote, so we only get one call on the first page.
			unitpp::assert_eq("AddDependentObjects was called exactly once for page 1", 1, cCalls);

			unitpp::assert_true("Layout stream was not null", qlayoutMgr->m_vAddDepObjsArgs[0].play);
			unitpp::assert_eq("Call was for page 0", 0, qlayoutMgr->m_vAddDepObjsArgs[0].hPage);

			// In this table test, we should also get the first line of the other column, so we get three guids at once.
			unitpp::assert_eq("First call (p1) received three object guids", 3, qlayoutMgr->m_vAddDepObjsArgs[0].cguid);
			unitpp::assert_true("First call (p1) received correct 1st guid",
				kguidDummyFootnote1 == qlayoutMgr->m_vAddDepObjsArgs[0].vguid[0]); // can't use _eq because doesn't do guids.
			unitpp::assert_true("First call (p1) received correct 2nd guid",
				kguidDummyFootnote2 == qlayoutMgr->m_vAddDepObjsArgs[0].vguid[1]); // can't use _eq because doesn't do guids.
			unitpp::assert_true("First call (p1) received correct 3rd guid",
				kguidDummyFootnote4 == qlayoutMgr->m_vAddDepObjsArgs[0].vguid[2]); // can't use _eq because doesn't do guids.
			unitpp::assert_true("First call (p1) does not allow failure", !qlayoutMgr->m_vAddDepObjsArgs[0].fAllowFail);
			unitpp::assert_true("First call (p1) should happen for first line of text",
				qlayoutMgr->m_vAddDepObjsArgs[0].dysAvailHeight > 180);

			unitpp::assert_true("Used some space on first page",
				dysUsedHeight > 0);
			unitpp::assert_true("Only fit one line on first page (big footnotes!)",
				dysUsedHeight <= 20);
			unitpp::assert_true("Page 2 should not overlap page 1's data",
				ysStartNextPage >= ysStartThisPage + dysUsedHeight);
// PAGE 2
			qlayoutMgr->m_vAddDepObjsArgs.Clear();

			ysStartThisPage = ysStartNextPage;
			m_qlay->LayoutPage(m_qvg32, 200, 200, &ysStartThisPage, 1, 1, &dysUsedHeight,
				&ysStartNextPage);

			cCalls = qlayoutMgr->m_vAddDepObjsArgs.Size();

			// The second call to AddDependentObjects is set up to fail, that is, to pretend the footnote
			// at the end of the first paragraph did not fit. The paragraph 1 string is designed to be at least
			// three lines long, so failure should be allowed (that is, there's at least one line that DOES fit before
			// we get to the one with the footnote).
			unitpp::assert_eq("AddDependentObjects was called exactly once for page 2", 1, cCalls);

			unitpp::assert_true("Layout stream was not null", qlayoutMgr->m_vAddDepObjsArgs[0].play);
			unitpp::assert_eq("Call was for page 1", 1, qlayoutMgr->m_vAddDepObjsArgs[0].hPage);
			unitpp::assert_eq("First call (p2) received one object guid", 1, qlayoutMgr->m_vAddDepObjsArgs[0].cguid);
			unitpp::assert_true("First call (p2) received correct guid",
				kguidDummyFootnote3 == qlayoutMgr->m_vAddDepObjsArgs[0].vguid[0]); // can't use _eq because doesn't do guids.
			unitpp::assert_true("First call (p2) allows failure", qlayoutMgr->m_vAddDepObjsArgs[0].fAllowFail);
			unitpp::assert_true("First call (p2) some height is available",
				qlayoutMgr->m_vAddDepObjsArgs[0].dysAvailHeight > 0);

			unitpp::assert_true("Used some space on second page",
				dysUsedHeight > 0);
			unitpp::assert_true("At least one more line would have fit if this footnote hadn't bumped it to next page",
				dysUsedHeight < 186);
			unitpp::assert_true("Page 3 should not overlap page 2's data",
				ysStartNextPage >= ysStartThisPage + dysUsedHeight);
// PAGE 3
			qlayoutMgr->m_vAddDepObjsArgs.Clear();

			ysStartThisPage = ysStartNextPage;
			m_qlay->LayoutPage(m_qvg32, 200, 200, &ysStartThisPage, 2, 1, &dysUsedHeight,
				&ysStartNextPage);

			cCalls = qlayoutMgr->m_vAddDepObjsArgs.Size();

			unitpp::assert_eq("AddDependentObjects was called once for page 3", 1, cCalls);

			unitpp::assert_true("Layout stream was not null", qlayoutMgr->m_vAddDepObjsArgs[0].play);

			unitpp::assert_eq("Call was for page 3", 2, qlayoutMgr->m_vAddDepObjsArgs[0].hPage);
			unitpp::assert_eq("First call (p3) received one object guid", 1, qlayoutMgr->m_vAddDepObjsArgs[0].cguid);
			unitpp::assert_true("First call (p3) received correct guid",
				kguidDummyFootnote3 == qlayoutMgr->m_vAddDepObjsArgs[0].vguid[0]); // can't use _eq because doesn't do guids.
			unitpp::assert_true("First call (p3) does not allow failure", !qlayoutMgr->m_vAddDepObjsArgs[0].fAllowFail);
			unitpp::assert_true("First call (p3) available height is not ridiculously small",
				qlayoutMgr->m_vAddDepObjsArgs[0].dysAvailHeight > 180);

			unitpp::assert_true("Used some space on third page",
				dysUsedHeight > 0);
			unitpp::assert_true("Didn't use too much space on third page",
				dysUsedHeight <= 200);
			unitpp::assert_eq("All the data fit in three pages", 0, ysStartNextPage);
		}

		void testBreakingOffsetTableRows()
		{
			// Set up a test view, a table with one row and two cells. The first cell contains the
			// heading StText, with khvoPara1 and khvoPara2, the first of which has three footnotes.
			// The second cell has the contents of the root section, with khvoPara3 (with a footnote)
			// and khvoPara4.
			CreateStringsWithFootnotes();
			CreateMoreStringsWithFootnotes();
			CreateTestStTextsForTable();
			SetupRootWithoutMargins(kfragOffsetTable);
			int dpiY;
			m_qvg32->get_YUnitsPerInch(&dpiY);
			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			qlayoutMgr->m_nScenario = 2; // our private "scenario"
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight, ysStartNextPage;

			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 200, 200, &ysStartThisPage, 0, 1, &dysUsedHeight,
				&ysStartNextPage);

			int cCalls = qlayoutMgr->m_vAddDepObjsArgs.Size();

			// In scenario 2 the dummy layout manager, on the first call, claims the footnotes on the first line
			// (left col) used almost all the available space. Adding the second line is therefore not possible even
			// though it is mostly aligned, so we only get one call on the first page.
			unitpp::assert_eq("AddDependentObjects was called exactly once for page 1", 1, cCalls);

			unitpp::assert_true("Layout stream was not null", qlayoutMgr->m_vAddDepObjsArgs[0].play);
			unitpp::assert_eq("Call was for page 0", 0, qlayoutMgr->m_vAddDepObjsArgs[0].hPage);

			// In this offset table test, we don't also get the first line of the other column, so only get two guids.
			unitpp::assert_eq("First call (p1) received two object guids", 2, qlayoutMgr->m_vAddDepObjsArgs[0].cguid);
			unitpp::assert_true("First call (p1) received correct 1st guid",
				kguidDummyFootnote1 == qlayoutMgr->m_vAddDepObjsArgs[0].vguid[0]); // can't use _eq because doesn't do guids.
			unitpp::assert_true("First call (p1) received correct 2nd guid",
				kguidDummyFootnote2 == qlayoutMgr->m_vAddDepObjsArgs[0].vguid[1]); // can't use _eq because doesn't do guids.
			unitpp::assert_true("First call (p1) does not allow failure", !qlayoutMgr->m_vAddDepObjsArgs[0].fAllowFail);
			unitpp::assert_true("First call (p1) should happen for first line of text",
				qlayoutMgr->m_vAddDepObjsArgs[0].dysAvailHeight > 180);

			unitpp::assert_true("Used some space on first page",
				dysUsedHeight > 0);
			unitpp::assert_true("Only fit one line on first page (big footnotes!)",
				dysUsedHeight <= 20);
			int dysOverlap;
			m_qlay->ColumnOverlapWithPrevious(0, &dysOverlap);
			unitpp::assert_true("Page 1 should not overlap 'previous' page",
				dysOverlap == 0);
			unitpp::assert_true("Starting point for page 2 should be below bottom of page 1",
				ysStartNextPage >= ysStartThisPage + dysUsedHeight);
// PAGE 2
			qlayoutMgr->m_vAddDepObjsArgs.Clear();

			int prevStartThisPage = ysStartThisPage = ysStartNextPage;
			m_qlay->LayoutPage(m_qvg32, 200, 200, &ysStartThisPage, 1, 1, &dysUsedHeight,
				&ysStartNextPage);

			unitpp::assert_eq("Computing second page should not change page boundary determined by first",
				prevStartThisPage, ysStartThisPage);

			m_qlay->ColumnOverlapWithPrevious(0, &dysOverlap);
			unitpp::assert_true("Page 2 should overlap page 1", dysOverlap > 0);

			VwTableBox * ptbox = dynamic_cast<VwTableBox *>(m_qlay->FirstBox());
			VwTableRowBox * prbox = dynamic_cast<VwTableRowBox *>(ptbox->FirstBox());
			VwTableCellBox * pcbox2 = dynamic_cast<VwTableCellBox *>(prbox->FirstBox()->NextOrLazy());
			VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pcbox2->FirstBox());
			int dysTopOfSecondLine = pvpbox->FirstBox()->TopToTopOfDocument();

			unitpp::assert_eq("overlap should equal distance from top of second line to bottom of first",
				prevStartThisPage - dysTopOfSecondLine, dysOverlap);

			cCalls = qlayoutMgr->m_vAddDepObjsArgs.Size();

			// The second call to AddDependentObjects allows the first line of the right column to fit, as it must.
			// The third call is set up to fail, that is, to pretend the footnote
			// at the end of the first paragraph in the left column did not fit. So we won't put any more on the
			// page, and get only two calls.
			unitpp::assert_eq("AddDependentObjects was called twice for page 2", 2, cCalls);

			unitpp::assert_true("Layout stream was not null", qlayoutMgr->m_vAddDepObjsArgs[0].play);
			unitpp::assert_eq("Call was for page 1", 1, qlayoutMgr->m_vAddDepObjsArgs[0].hPage);
			unitpp::assert_eq("First call (p2) received one object guid", 1, qlayoutMgr->m_vAddDepObjsArgs[0].cguid);
			unitpp::assert_true("First call (p2) received correct guid",
				kguidDummyFootnote4 == qlayoutMgr->m_vAddDepObjsArgs[0].vguid[0]); // can't use _eq because doesn't do guids.
			unitpp::assert_true("First call (p2) does not allow failure", !qlayoutMgr->m_vAddDepObjsArgs[0].fAllowFail);
			unitpp::assert_true("First call (p2) some height is available",
				qlayoutMgr->m_vAddDepObjsArgs[0].dysAvailHeight > 0);
			unitpp::assert_eq("Second call (p2) received one object guid", 1, qlayoutMgr->m_vAddDepObjsArgs[1].cguid);
			unitpp::assert_true("Second call (p2) received correct guid",
				kguidDummyFootnote3 == qlayoutMgr->m_vAddDepObjsArgs[1].vguid[0]); // can't use _eq because doesn't do guids.

			unitpp::assert_true("Used some space on second page",
				dysUsedHeight > 0);
			unitpp::assert_true("At least one more line would have fit if this footnote hadn't bumped it to next page",
				dysUsedHeight < 186);
			unitpp::assert_true("Page 3 should not overlap page 2's data by more than offset",
				ysStartNextPage >= ysStartThisPage + dysUsedHeight - dysOverlap);

			// test checking whether line is on page.
			int x = 10; // left column;
			int y = dysTopOfSecondLine + 2;
			ComBool fOnPrevPage;
			int xLeft, xRight;
			m_qlay->IsInPageAbove(x, y, ysStartThisPage, m_qvg32, &xLeft, &xRight, &fOnPrevPage);
			unitpp::assert_true("line in left column is on previous page", fOnPrevPage);
			unitpp::assert_true("got useful range on prev page", xLeft < xRight && xRight < 100);
			x = 150; // second column
			m_qlay->IsInPageAbove(x, y, ysStartThisPage, m_qvg32, &xLeft, &xRight, &fOnPrevPage);
			unitpp::assert_true("line in right column is on current page", !fOnPrevPage);
			unitpp::assert_true("got useful range for last line on prev page", xLeft < xRight && xRight < 100);

// PAGE 3
			qlayoutMgr->m_vAddDepObjsArgs.Clear();

			ysStartThisPage = ysStartNextPage;
			m_qlay->LayoutPage(m_qvg32, 200, 200, &ysStartThisPage, 2, 1, &dysUsedHeight,
				&ysStartNextPage);

			cCalls = qlayoutMgr->m_vAddDepObjsArgs.Size();

			unitpp::assert_eq("AddDependentObjects was called once for page 3", 1, cCalls);

			unitpp::assert_true("Layout stream was not null", qlayoutMgr->m_vAddDepObjsArgs[0].play);

			unitpp::assert_eq("Call was for page 3", 2, qlayoutMgr->m_vAddDepObjsArgs[0].hPage);
			unitpp::assert_eq("First call (p3) received one object guid", 1, qlayoutMgr->m_vAddDepObjsArgs[0].cguid);
			unitpp::assert_true("First call (p3) received correct guid",
				kguidDummyFootnote3 == qlayoutMgr->m_vAddDepObjsArgs[0].vguid[0]); // can't use _eq because doesn't do guids.
			unitpp::assert_true("First call (p3) does not allow failure", !qlayoutMgr->m_vAddDepObjsArgs[0].fAllowFail);
			unitpp::assert_true("First call (p3) available height is not ridiculously small",
				qlayoutMgr->m_vAddDepObjsArgs[0].dysAvailHeight > 180);

			unitpp::assert_true("Used some space on third page",
				dysUsedHeight > 0);
			unitpp::assert_true("Didn't use too much space on third page",
				dysUsedHeight <= 200);
			unitpp::assert_eq("All the data fit in three pages", 0, ysStartNextPage);
		}

		void testAddDependentObjectsGetsCalled()
		{
			CreateStringsWithFootnotes();
			CreateTestStTexts(2);
			SetupRootWithoutMargins();

			int dpiY;
			m_qvg32->get_YUnitsPerInch(&dpiY);
			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight, ysStartNextPage;

// PAGE 1
			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 200, 200, &ysStartThisPage, 0, 1, &dysUsedHeight,
				&ysStartNextPage);

			int cCalls = qlayoutMgr->m_vAddDepObjsArgs.Size();

			// By default the dummy layout manager, on the first call, claims the footnotes on the first line
			// used almost all the available space. Adding the second line is therefore not possible even
			// without the third footnote, so we only get one call on the first page.
			unitpp::assert_eq("AddDependentObjects was called exactly once for page 1", 1, cCalls);

			unitpp::assert_true("Layout stream was not null", qlayoutMgr->m_vAddDepObjsArgs[0].play);
			unitpp::assert_eq("Call was for page 0", 0, qlayoutMgr->m_vAddDepObjsArgs[0].hPage);
			unitpp::assert_eq("First call (p1) received two object guids", 2, qlayoutMgr->m_vAddDepObjsArgs[0].cguid);
			unitpp::assert_true("First call (p1) received correct 1st guid",
				kguidDummyFootnote1 == qlayoutMgr->m_vAddDepObjsArgs[0].vguid[0]); // can't use _eq because doesn't do guids.
			unitpp::assert_true("First call (p1) received correct 2nd guid",
				kguidDummyFootnote2 == qlayoutMgr->m_vAddDepObjsArgs[0].vguid[1]); // can't use _eq because doesn't do guids.
			unitpp::assert_true("First call (p1) does not allow failure", !qlayoutMgr->m_vAddDepObjsArgs[0].fAllowFail);
			unitpp::assert_true("First call (p1) should happen for first line of text",
				qlayoutMgr->m_vAddDepObjsArgs[0].dysAvailHeight > 180);

			unitpp::assert_true("Used some space on first page",
				dysUsedHeight > 0);
			unitpp::assert_true("Only fit one line on first page (big footnotes!)",
				dysUsedHeight <= 20);
			unitpp::assert_true("Page 2 should not overlap page 1's data",
				ysStartNextPage >= ysStartThisPage + dysUsedHeight);

// PAGE 2
			qlayoutMgr->m_vAddDepObjsArgs.Clear();

			ysStartThisPage = ysStartNextPage;
			m_qlay->LayoutPage(m_qvg32, 200, 200, &ysStartThisPage, 1, 1, &dysUsedHeight,
				&ysStartNextPage);

			cCalls = qlayoutMgr->m_vAddDepObjsArgs.Size();

			// The second call to AddDependentObjects is set up to fail, that is, to pretend the footnote
			// at the end of the first paragraph did not fit. The paragraph 1 string is designed to be at least
			// three lines long, so failure should be allowed (that is, there's at least one line that DOES fit before
			// we get to the one with the footnote).
			unitpp::assert_eq("AddDependentObjects was called exactly once for page 2", 1, cCalls);

			unitpp::assert_true("Layout stream was not null", qlayoutMgr->m_vAddDepObjsArgs[0].play);
			unitpp::assert_eq("Call was for page 1", 1, qlayoutMgr->m_vAddDepObjsArgs[0].hPage);
			unitpp::assert_eq("First call (p2) received one object guid", 1, qlayoutMgr->m_vAddDepObjsArgs[0].cguid);
			unitpp::assert_true("First call (p2) received correct guid",
				kguidDummyFootnote3 == qlayoutMgr->m_vAddDepObjsArgs[0].vguid[0]); // can't use _eq because doesn't do guids.
			unitpp::assert_true("First call (p2) allows failure", qlayoutMgr->m_vAddDepObjsArgs[0].fAllowFail);
			unitpp::assert_true("First call (p2) some height is available",
				qlayoutMgr->m_vAddDepObjsArgs[0].dysAvailHeight > 0);

			unitpp::assert_true("Used some space on second page",
				dysUsedHeight > 0);
			unitpp::assert_true("At least one more line would have fit if this footnote hadn't bumped it to next page",
				dysUsedHeight < 186);
			unitpp::assert_true("Page 3 should not overlap page 2's data",
				ysStartNextPage >= ysStartThisPage + dysUsedHeight);

// PAGE 3
			qlayoutMgr->m_vAddDepObjsArgs.Clear();

			ysStartThisPage = ysStartNextPage;
			m_qlay->LayoutPage(m_qvg32, 200, 200, &ysStartThisPage, 2, 1, &dysUsedHeight,
				&ysStartNextPage);

			cCalls = qlayoutMgr->m_vAddDepObjsArgs.Size();

			unitpp::assert_eq("AddDependentObjects was called three times for page 3", 3, cCalls);

			for (int i = 0; i < cCalls; i++)
			{
				unitpp::assert_true("Layout stream was not null", qlayoutMgr->m_vAddDepObjsArgs[i].play);
				if (i > 0)
				{
					unitpp::assert_true("Nth call available height is less than for N-1th call",
						qlayoutMgr->m_vAddDepObjsArgs[i].dysAvailHeight < qlayoutMgr->m_vAddDepObjsArgs[i-1].dysAvailHeight);
					unitpp::assert_true("Second and later calls allow failure", qlayoutMgr->m_vAddDepObjsArgs[i].fAllowFail);
				}

				unitpp::assert_eq("Call was for page 3", 2, qlayoutMgr->m_vAddDepObjsArgs[i].hPage);
				switch (i)
				{
				case 0:
					unitpp::assert_eq("First call (p3) received one object guid", 1, qlayoutMgr->m_vAddDepObjsArgs[i].cguid);
					unitpp::assert_true("First call (p3) received correct guid",
						kguidDummyFootnote3 == qlayoutMgr->m_vAddDepObjsArgs[i].vguid[0]); // can't use _eq because doesn't do guids.
					unitpp::assert_true("First call (p3) does not allow failure", !qlayoutMgr->m_vAddDepObjsArgs[i].fAllowFail);
					unitpp::assert_true("First call (p3) available height is not ridiculously small",
						qlayoutMgr->m_vAddDepObjsArgs[i].dysAvailHeight > 180);
					break;
				case 1:
					unitpp::assert_eq("Second call (p3) received two object guids", 2, qlayoutMgr->m_vAddDepObjsArgs[i].cguid);
					unitpp::assert_true("Second call (p3) received correct first guid",
						kguidDummyFootnote1 == qlayoutMgr->m_vAddDepObjsArgs[i].vguid[0]); // can't use _eq because doesn't do guids.
					unitpp::assert_true("Second call (p3) received correct 2nd guid",
						kguidDummyFootnote2 == qlayoutMgr->m_vAddDepObjsArgs[i].vguid[1]); // can't use _eq because doesn't do guids.
					break;
				case 2:
					unitpp::assert_eq("Third call (p3) received one object guid", 1, qlayoutMgr->m_vAddDepObjsArgs[i].cguid);
					unitpp::assert_true("Third call (p3) received correct guid",
						kguidDummyFootnote3 == qlayoutMgr->m_vAddDepObjsArgs[i].vguid[0]);
					break;
				}
			}

			unitpp::assert_true("Used some space on third page",
				dysUsedHeight > 0);
			unitpp::assert_true("Didn't use too much space on third page",
				dysUsedHeight <= 200);
			unitpp::assert_eq("All the data fit in three pages", 0, ysStartNextPage);
		}

		void testStartOfNextLine()
		{
			CreateBoringStrings();
			CreateTestStTexts(2);
			SetupRootWithMargins();

			int dysUsedHeight, ysStartNextPage;
			const int kdxpLayoutWidth = 200;
			m_qlay->ConstructAndLayout(m_qvg32, kdxpLayoutWidth);
			unitpp::assert_true("ConstructAndLayout constructed root box", m_qlay->PrivateIsConstructed());

			VwDivBox * pboxDiv1 = dynamic_cast<VwDivBox *>(m_qlay->FirstBox());
			VwParagraphBox * pboxFirst = dynamic_cast<VwParagraphBox *>(pboxDiv1->FirstBox());
			VwParagraphBox * pboxPara2 = dynamic_cast<VwParagraphBox *>(pboxFirst->NextRealBox());
			VwStringBox * pboxFirstLine = dynamic_cast<VwStringBox *>(pboxFirst->FirstBox());
			Vector<int> vColHeights;
			Vector<int> vColOverlaps;
			LayoutPageMethod lpm1(m_qlay, m_qvg32, kdxpLayoutWidth, 200, 0, 0, 1, &vColHeights, &vColOverlaps,
				&dysUsedHeight, &ysStartNextPage);
			VwBox * pboxLine2 = lpm1.StartOfNextLine(pboxFirstLine);
			unitpp::assert_eq("found second line from first", pboxLine2, pboxFirstLine->NextRealBox());
			VwBox * pboxLine1Para2 = lpm1.StartOfNextLine(pboxLine2);
			while (pboxLine1Para2->Container() == pboxFirst)
				pboxLine1Para2 = lpm1.StartOfNextLine(pboxLine1Para2);
			unitpp::assert_eq("found first line in 2nd para from last in first",
				pboxPara2->FirstBox(), pboxLine1Para2);
		}

		VwStringBox* GetBoxAtY(VwGroupBox* pbox, int yPos)
		{
			VwStringBox * psbox = dynamic_cast<VwStringBox *>(pbox->FirstBox());
			if (psbox)
			{
				int dyOffset = pbox->Container()->Top() + pbox->Top();
				for (; psbox; psbox = dynamic_cast<VwStringBox *>(psbox->Next()))
				{
					if (psbox->Top() + dyOffset >= yPos || psbox->Bottom() + dyOffset >= yPos)
						return psbox;
				}
				return NULL;
			}

			for (VwGroupBox* pChild = dynamic_cast<VwGroupBox*>(pbox->FirstBox());
				pChild;
				pChild = dynamic_cast<VwGroupBox*>(pChild->Next()))
			{
				VwStringBox* pRet = GetBoxAtY(pChild, yPos);
				if (pRet)
					return pRet;
			}
			return NULL;
		}

		void testTwoColumns()
		{
#ifdef WIN32 // TODO-Linux: FWNX-453
			CreateBoringStrings();
			CreateTestStTexts(2);
			SetupRootWithoutMargins();
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympParaTopMarginInitial = 3000;
			pdvc->m_dympLineSpace = -10000;
			pdvc->m_nInitialParas = 1;

			int dpiY;
			m_qvg32->get_YUnitsPerInch(&dpiY);
			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight, ysStartNextPage;

// PAGE 1
			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 0, 2, &dysUsedHeight,
				&ysStartNextPage);

			// we expect that dysUsedHeight is the height of the tallest column on the page.
			VwStringBox * psboxLastInFirstColumn = GetBoxAtY(m_qlay, dysUsedHeight);
			VwStringBox * psboxLastInSecondColumn = GetBoxAtY(m_qlay, dysUsedHeight * 2);

			// Check end of first column
			int dyOffset = psboxLastInFirstColumn->Container()->Container()->Top() +
				psboxLastInFirstColumn->Container()->Top();
			unitpp::assert_true("Text cut off at bottom of column 1",
				psboxLastInFirstColumn->Bottom() + dyOffset <= ysStartThisPage + dysUsedHeight);
			int dysColHeight1;
			m_qlay->ColumnHeight(0, &dysColHeight1);
			unitpp::assert_eq("Wrong height reported for column 1",
				psboxLastInFirstColumn->Bottom() + dyOffset - ysStartThisPage, dysColHeight1);

			// Check end of second column
			dyOffset = psboxLastInSecondColumn->Container()->Container()->Top() +
				psboxLastInSecondColumn->Container()->Top();
			unitpp::assert_true("Text cut off at bottom of column 2",
				psboxLastInSecondColumn->Bottom() + dyOffset <= ysStartThisPage + dysUsedHeight * 2);
			int dysColHeight2;
			m_qlay->ColumnHeight(1, &dysColHeight2);
			unitpp::assert_eq("Wrong height reported for column 2",
				psboxLastInSecondColumn->Bottom() + dyOffset - ysStartThisPage - dysColHeight1,
				dysColHeight2);

			// Check start of next page
			VwStringBox* psboxFirstOnSecondPage = dynamic_cast<VwStringBox *>(psboxLastInSecondColumn->Next());
			dyOffset = psboxFirstOnSecondPage->Container()->Container()->Top() +
				psboxFirstOnSecondPage->Container()->Top();
			unitpp::assert_eq("First box on second page should be at top of second page",
				psboxFirstOnSecondPage->Top() + dyOffset, ysStartNextPage);
#endif
		}


		void testTwoColumns_OneLineFitOnEachColumn()
		{
#ifdef WIN32 // TODO-Linux: FWNX-453
			CreateBoringStrings();
			CreateTestStTexts(2);
			SetupRootWithoutMargins();
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympParaTopMarginInitial = 3000;
			pdvc->m_dympLineSpace = -10000; // We're using exact line spacing!
			pdvc->m_nInitialParas = 1;

			int dpiY;
			m_qvg32->get_YUnitsPerInch(&dpiY);
			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight, ysStartNextPage;

// PAGE 1
			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 100, 20, &ysStartThisPage, 0, 2, &dysUsedHeight,
				&ysStartNextPage);

			// we expect that dysUsedHeight is the height of the tallest column on the page.
			VwStringBox * psboxFirstInFirstColumn = GetBoxAtY(m_qlay, 0);
			VwStringBox * psboxLastInFirstColumn = GetBoxAtY(m_qlay, dysUsedHeight);
			VwStringBox * psboxLastInSecondColumn = GetBoxAtY(m_qlay, dysUsedHeight * 2);

			// we expect to get only one line per column
			unitpp::assert_eq("Got more than one line in column 1",
				psboxFirstInFirstColumn, psboxLastInFirstColumn);
			unitpp::assert_eq("Got more than one line in column 2",
				psboxLastInFirstColumn->Next(), psboxLastInSecondColumn);

			// Check end of first column
			int dyOffset = psboxLastInFirstColumn->Container()->Container()->Top() +
				psboxLastInFirstColumn->Container()->Top();
			unitpp::assert_true("Text cut off at bottom of column 1",
				psboxLastInFirstColumn->Bottom() + dyOffset <= ysStartThisPage + dysUsedHeight);
			int dysColHeight1;
			m_qlay->ColumnHeight(0, &dysColHeight1);
			unitpp::assert_eq("Wrong height reported for column 1",
				psboxLastInFirstColumn->Bottom() + dyOffset - ysStartThisPage, dysColHeight1);

			// Check end of second column
			dyOffset = psboxLastInSecondColumn->Container()->Container()->Top() +
				psboxLastInSecondColumn->Container()->Top();
			unitpp::assert_true("Text cut off at bottom of column 2",
				psboxLastInSecondColumn->Bottom() + dyOffset <= ysStartThisPage + dysUsedHeight * 2);
			int dysColHeight2;
			m_qlay->ColumnHeight(1, &dysColHeight2);
			unitpp::assert_eq("Wrong height reported for column 2",
				psboxLastInSecondColumn->Bottom() + dyOffset - ysStartThisPage - dysColHeight1,
				dysColHeight2);

			// Check start of next page
			VwStringBox* psboxFirstOnSecondPage = dynamic_cast<VwStringBox *>(psboxLastInSecondColumn->Next());
			dyOffset = psboxFirstOnSecondPage->Container()->Container()->Top() +
				psboxFirstOnSecondPage->Container()->Top() + psboxFirstInFirstColumn->Top();
			unitpp::assert_eq("First box on second page should be at top of second page",
				// Because of exact line spacing the top of each line is 2 pixels off,
				// so we have to adjust for that
				psboxFirstOnSecondPage->Top() + dyOffset,
				ysStartNextPage);
#endif
		}


		// Tests KeepWithNext if we have one column text (TE-5571)
		void testKeepWithNext_OneColumn()
		{
			StrUni stuPara1(L"This is the first test paragraph that we are trying to lay out.");
			ITsStringPtr qtss;
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara1, kflidStTxtPara_Contents, qtss);

			StrUni stuPara2(L"This is the second test paragraph that we are.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara2, kflidStTxtPara_Contents, qtss);

			StrUni stuPara3(L"This is the third test paragraph that we are trying to lay out. We hope it's too long to fit on one line.");
			m_qtsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara3, kflidStTxtPara_Contents, qtss);

			// set keep with next for second para
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			qtpb->SetIntPropValues(ktptKeepWithNext, ktpvEnum, kttvForceOn);

			ITsTextPropsPtr qttp;
			qtpb->GetTextProps(&qttp);
			m_qcda->CacheUnknown(khvoTlpPara2, kflidStPara_StyleRules, qttp);

			// This will create a section (two texts with three paragraphs each).
			CreateTestStTexts(3);

			SetupRootWithoutMargins();
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympParaTopMarginInitial = 3000;
			pdvc->m_dympLineSpace = -10000;
			pdvc->m_nInitialParas = 1;

			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight1, dysUsedHeight2, ysStartNextPage1, ysStartNextPage2;

			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 0, 1, &dysUsedHeight1,
				&ysStartNextPage1);
			ysStartThisPage = ysStartNextPage1;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 1, 1, &dysUsedHeight2,
				&ysStartNextPage2);

			// We expect to find the first line of the second paragraph on the bottom of the first page,
			// the second line together with the third paragraph on the next page.
			VwStringBox * psboxLastOnPage = GetBoxAtY(m_qlay, dysUsedHeight1);
			unitpp::assert_true("Didn't find box", psboxLastOnPage);
			unitpp::assert_eq("Last line on first page should belong to second para",
				dynamic_cast<VwGroupBox*>(m_qlay->FirstBox())->FirstBox()->Next(), psboxLastOnPage->Container());
			unitpp::assert_true("Last line of second para should be on second page",
				!psboxLastOnPage->Next()->Next());
			unitpp::assert_true("Last line of second para should be able to fit on first page",
				psboxLastOnPage->Next()->Height() + dysUsedHeight1 < 100);
		}

		// Tests that Keep With Next works between columns
		void testKeepWithNext_TwoColumns()
		{
			StrUni stuPara1(L"This is the first test paragraph that we are trying to lay out.");
			ITsStringPtr qtss;
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara1, kflidStTxtPara_Contents, qtss);

			StrUni stuPara2(L"This is the second test paragraph that we are.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara2, kflidStTxtPara_Contents, qtss);

			StrUni stuPara3(L"This is the third test paragraph that we are trying to lay out. We hope it's too long to fit on one line.");
			m_qtsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara3, kflidStTxtPara_Contents, qtss);

			// set keep with next for second para
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			qtpb->SetIntPropValues(ktptKeepWithNext, ktpvEnum, kttvForceOn);

			ITsTextPropsPtr qttp;
			qtpb->GetTextProps(&qttp);
			m_qcda->CacheUnknown(khvoTlpPara2, kflidStPara_StyleRules, qttp);

			// This will create a section (two texts with three paragraphs each).
			CreateTestStTexts(3);

			SetupRootWithoutMargins();
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympParaTopMarginInitial = 3000;
			pdvc->m_dympLineSpace = -10000;
			pdvc->m_nInitialParas = 1;

			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight1, dysUsedHeight2, ysStartNextPage1, ysStartNextPage2;

			// Layout the first two pages. We have two columns, each 100 pixels wide.
			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 0, 2, &dysUsedHeight1,
				&ysStartNextPage1);
			int dysHeightFirstColumn;
			m_qlay->ColumnHeight(0, &dysHeightFirstColumn);
			ysStartThisPage = ysStartNextPage1;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 1, 2, &dysUsedHeight2,
				&ysStartNextPage2);

			// We expect to find the first line of the second paragraph on the bottom of the first column,
			// the second line together with the third paragraph in the next column.
			VwStringBox * psboxLastOnPage = GetBoxAtY(m_qlay, dysHeightFirstColumn);
			unitpp::assert_true("Didn't find box", psboxLastOnPage);
			unitpp::assert_eq("Last line of first column should belong to second para",
				dynamic_cast<VwGroupBox*>(m_qlay->FirstBox())->FirstBox()->Next(), psboxLastOnPage->Container());
			unitpp::assert_true("Last line of second para should be in second column",
				!psboxLastOnPage->Next()->Next());
			unitpp::assert_true("Last line of second para should be able to fit in first column",
				psboxLastOnPage->Next()->Height() + dysHeightFirstColumn < 100);
		}

		// Tests that we don't put a keep-with-next para in the next column if it does
		// fit in the current column together with the first line of the next para.
		void testKeepWithNext_FitInColumn()
		{
			StrUni stuPara1(L"This is the first test paragraph that we are trying to lay out.");
			ITsStringPtr qtss;
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara1, kflidStTxtPara_Contents, qtss);

			StrUni stuPara2(L"Short paragraph.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara2, kflidStTxtPara_Contents, qtss);

			StrUni stuPara3(L"This is the third test paragraph that we are trying to lay out. We hope it's too long to fit on one line.");
			m_qtsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara3, kflidStTxtPara_Contents, qtss);

			// set keep with next for second para
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			qtpb->SetIntPropValues(ktptKeepWithNext, ktpvEnum, kttvForceOn);

			ITsTextPropsPtr qttp;
			qtpb->GetTextProps(&qttp);
			m_qcda->CacheUnknown(khvoTlpPara2, kflidStPara_StyleRules, qttp);

			// This will create a section (two texts with three paragraphs each).
			CreateTestStTexts(3);

			SetupRootWithoutMargins();
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympParaTopMarginInitial = 3000;
			pdvc->m_dympLineSpace = -10000;
			pdvc->m_nInitialParas = 1;

			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight1, dysUsedHeight2, ysStartNextPage1, ysStartNextPage2;

			// Layout the first two pages. We have two columns, each 100 pixels wide.
			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 0, 2, &dysUsedHeight1,
				&ysStartNextPage1);
			int dysHeightFirstColumn;
			m_qlay->ColumnHeight(0, &dysHeightFirstColumn);
			ysStartThisPage = ysStartNextPage1;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 1, 2, &dysUsedHeight2,
				&ysStartNextPage2);

			VwStringBox * psboxLastOnPage = GetBoxAtY(m_qlay, dysHeightFirstColumn);
			unitpp::assert_true("Didn't find box", psboxLastOnPage);
			unitpp::assert_eq("Last line of first column should belong to third para",
				dynamic_cast<VwGroupBox*>(m_qlay->FirstBox())->FirstBox()->Next()->Next(),
				psboxLastOnPage->Container());
		}

		// This tests KeepWithNext in the case where we have several one-line paragraphs that
		// all have keep-with-next set. The paragraphs don't fit all on one page.
		// What we expect to happen is that we put the first line of the first non-keep-with-next
		// paragraph on a page together with as many as fit on the page previous paragraphs.
		// The other paragraphs before that ignore keep-with-next and are on the previous page.
		// (TE-5979)
		void testKeepWithNext_DontFitOnPage()
		{
			// Create props builder with keep with next set
			ITsStringPtr qtss;
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			qtpb->SetIntPropValues(ktptKeepWithNext, ktpvEnum, kttvForceOn);

			// create one-line paragraphs with keep-with-next
			const int khvoPara = 2001;
			int hvoPara = khvoPara;
			for (int i = 0; i < 10; i++)
			{
				StrUni stuPara(L"Short paragraph.");
				m_qtsf->MakeString(stuPara.Bstr(), g_wsEng, &qtss);
				m_qcda->CacheStringProp(hvoPara, kflidStTxtPara_Contents, qtss);

				ITsTextPropsPtr qttp;
				qtpb->GetTextProps(&qttp);
				m_qcda->CacheUnknown(hvoPara, kflidStPara_StyleRules, qttp);

				hvoPara++;
			}

			StrUni stuPara3(L"This is the test paragraph that we are trying to lay out. We hope it's too long to fit on one line.");
			m_qtsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara3, kflidStTxtPara_Contents, qtss);

			// Create a section (no heading, content with 11 paragraphs).
			HVO hvoStText2 = 502;

			HVO rghvoContentPara[11];
			for (int i = 0; i < 10; i++)
				rghvoContentPara[i] = khvoPara + i;
			rghvoContentPara[10] = khvoTlpPara3;
			m_qcda->CacheVecProp(hvoStText2, kflidStText_Paragraphs, rghvoContentPara, 11);

			m_qcda->CacheObjProp(hvoRoot, ktagSection_Content, hvoStText2);

			SetupRootWithoutMargins();
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympParaTopMarginInitial = 3000;
			pdvc->m_dympLineSpace = -10000;
			pdvc->m_nInitialParas = 1;

			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight1, dysUsedHeight2, ysStartNextPage1, ysStartNextPage2;

			// Layout the first two pages. We have one column 100 pixels wide.
			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 0, 1, &dysUsedHeight1,
				&ysStartNextPage1);
			int dysHeightFirstColumn;
			m_qlay->ColumnHeight(0, &dysHeightFirstColumn);
			ysStartThisPage = ysStartNextPage1;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 1, 1, &dysUsedHeight2,
				&ysStartNextPage2);

			VwStringBox * psboxLastOnPage = GetBoxAtY(m_qlay, dysHeightFirstColumn);
			unitpp::assert_true("Didn't find box", psboxLastOnPage);
			unitpp::assert_true("Should fill first page", ysStartNextPage1 >= 85);
		}

		// Tests that Keep Together works between columns and pages (TE-5570)
		void testKeepTogether_TwoColumns()
		{
			StrUni stuPara1(L"This is the first pretty long test paragraph that we are trying to lay out.");
			ITsStringPtr qtss;
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara1, kflidStTxtPara_Contents, qtss);

			StrUni stuPara2(L"This is the second pretty long test paragraph that we are trying to lay out.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara2, kflidStTxtPara_Contents, qtss);

			StrUni stuPara3(L"This is the third pretty long test paragraph that we are trying to lay out.");
			m_qtsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara3, kflidStTxtPara_Contents, qtss);

			// set keep together for all paras
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			qtpb->SetIntPropValues(ktptKeepTogether, ktpvEnum, kttvForceOn);

			ITsTextPropsPtr qttp;
			qtpb->GetTextProps(&qttp);
			m_qcda->CacheUnknown(khvoTlpPara1, kflidStPara_StyleRules, qttp);
			m_qcda->CacheUnknown(khvoTlpPara2, kflidStPara_StyleRules, qttp);
			m_qcda->CacheUnknown(khvoTlpPara3, kflidStPara_StyleRules, qttp);

			// This will create a section (two texts with three paragraphs each).
			CreateTestStTexts(3);

			SetupRootWithoutMargins();
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympParaTopMarginInitial = 3000;
			pdvc->m_dympLineSpace = -10000;
			pdvc->m_nInitialParas = 1;

			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight1, dysUsedHeight2, ysStartNextPage1, ysStartNextPage2;

			// Layout the first two pages. We have two columns, each 100 pixels wide.
			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 0, 2, &dysUsedHeight1,
				&ysStartNextPage1);
			int dysHeightFirstColumn;
			int dysHeightSecondColumn;
			m_qlay->ColumnHeight(0, &dysHeightFirstColumn);
			m_qlay->ColumnHeight(1, &dysHeightSecondColumn);
			ysStartThisPage = ysStartNextPage1;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 1, 2, &dysUsedHeight2,
				&ysStartNextPage2);
			int dysHeightSecondPage;
			m_qlay->ColumnHeight(0, &dysHeightSecondPage);

			// We expect that the first column only contains the first paragraph. The second column
			// contains the second paragraph, and the second page contains the third paragraph.
			VwStringBox * psboxLastInColumn1 = GetBoxAtY(m_qlay, dysHeightFirstColumn);
			unitpp::assert_true("Didn't find last box in first column", psboxLastInColumn1);
			unitpp::assert_eq("Last line of first column should belong to first para",
				dynamic_cast<VwGroupBox*>(m_qlay->FirstBox())->FirstBox(), psboxLastInColumn1->Container());
			unitpp::assert_true("Last line in first column should be last line of first para",
				!psboxLastInColumn1->Next());

			VwStringBox * psboxLastInColumn2 = GetBoxAtY(m_qlay,
				dysHeightFirstColumn + dysHeightSecondColumn);
			unitpp::assert_true("Didn't find last box in second column", psboxLastInColumn2);
			unitpp::assert_eq("Last line of second column should belong to second para",
				dynamic_cast<VwGroupBox*>(m_qlay->FirstBox())->FirstBox()->Next(),
				psboxLastInColumn2->Container());
			unitpp::assert_true("Last line in second column should be last line of second para",
				!psboxLastInColumn2->Next());
		}

		// Tests Keep Together if the paragraph doesn't fit in one column
		void testKeepTogether_DoesntFit()
		{
			StrUni stuPara1(L"This is the first short test paragraph.");
			ITsStringPtr qtss;
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara1, kflidStTxtPara_Contents, qtss);

			StrUni stuPara2(L"This is the second pretty long test paragraph that we are trying to lay out. \
It should have enough text so that it doesn't fit in one column. We expect that the views  \
code ignores the keep together attribute in this case and does what it can to make it look \
nice. This is probably long enough now. We add some more gibberish text so that it doesn't even fit\
on one page - or even on one-and-a-half pages. lkajsdflj lkasdflkasjdfl aslkfjaslf klasfjlkjasdflasdf \
ajlkjlksjafjlkasjdf klasfj askfjsdlfjas flkajdfjafklasjdflkasj flakjsf aklsfj asklfjas lkfj alsf.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara2, kflidStTxtPara_Contents, qtss);

			// set keep together for all paras
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			qtpb->SetIntPropValues(ktptKeepTogether, ktpvEnum, kttvForceOn);

			ITsTextPropsPtr qttp;
			qtpb->GetTextProps(&qttp);
			m_qcda->CacheUnknown(khvoTlpPara1, kflidStPara_StyleRules, qttp);
			m_qcda->CacheUnknown(khvoTlpPara2, kflidStPara_StyleRules, qttp);

			// This will create a section (two texts with two paragraphs each).
			CreateTestStTexts(2);

			SetupRootWithoutMargins();
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympParaTopMarginInitial = 3000;
			pdvc->m_dympLineSpace = -10000;
			pdvc->m_nInitialParas = 1;

			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight1, dysUsedHeight2, ysStartNextPage1, ysStartNextPage2;

			// Layout the first two pages. We have two columns, each 100 pixels wide.
			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 0, 2, &dysUsedHeight1,
				&ysStartNextPage1);
			int dysHeightFirstColumn;
			int dysHeightSecondColumn;
			m_qlay->ColumnHeight(0, &dysHeightFirstColumn);
			m_qlay->ColumnHeight(1, &dysHeightSecondColumn);
			ysStartThisPage = ysStartNextPage1;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 1, 2, &dysUsedHeight2,
				&ysStartNextPage2);
			int dysHeightSecondPage;
			m_qlay->ColumnHeight(0, &dysHeightSecondPage);

			// We expect that the first column contains the first paragraph. The second
			// column should start with the second paragraph. The second page should contain
			// more of the second paragraph.
			VwStringBox * psboxLastInColumn1 = GetBoxAtY(m_qlay, dysHeightFirstColumn);
			unitpp::assert_true("Didn't find last box in first column", psboxLastInColumn1);
			unitpp::assert_eq("Last line of first column should belong to first para",
				dynamic_cast<VwGroupBox*>(m_qlay->FirstBox())->FirstBox(),
				psboxLastInColumn1->Container());

			VwStringBox * psboxLastInColumn2 = GetBoxAtY(m_qlay,
				dysHeightFirstColumn + dysHeightSecondColumn);
			unitpp::assert_true("Didn't find last box in second column", psboxLastInColumn2);
			unitpp::assert_true("Didn't put anything in second column", dysHeightSecondColumn > 0);
			unitpp::assert_eq("Last line of second column should belong to second para",
				dynamic_cast<VwGroupBox*>(m_qlay->FirstBox())->FirstBox()->Next(),
				psboxLastInColumn2->Container());

			VwStringBox * psboxFirstOnPage2 = GetBoxAtY(m_qlay, ysStartNextPage2);
			unitpp::assert_true("Didn't find last box on second page", psboxFirstOnPage2);
			unitpp::assert_eq("First line of second page should belong to second para",
				dynamic_cast<VwGroupBox*>(m_qlay->FirstBox())->FirstBox()->Next(),
				psboxFirstOnPage2->Container());
		}

		// Tests that we don't put the first line of a paragraph on the bottom of the column
		// (TE-5569)
		void testOrphan_MoveToNextColumn()
		{
			StrUni stuPara1(L"This is the first test paragraph that we are trying to lay out. Some more text.");
			ITsStringPtr qtss;
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara1, kflidStTxtPara_Contents, qtss);

			StrUni stuPara2(L"Short paragraph.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara2, kflidStTxtPara_Contents, qtss);

			StrUni stuPara3(L"This is the third test paragraph that we are trying to lay out. We hope it's too long to fit on one line.");
			m_qtsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara3, kflidStTxtPara_Contents, qtss);

			// set widow/orphan control for third para
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			qtpb->SetIntPropValues(ktptWidowOrphanControl, ktpvEnum, kttvForceOn);

			ITsTextPropsPtr qttp;
			qtpb->GetTextProps(&qttp);
			m_qcda->CacheUnknown(khvoTlpPara3, kflidStPara_StyleRules, qttp);

			// This will create a section (two texts with three paragraphs each).
			CreateTestStTexts(3);

			SetupRootWithoutMargins();
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympParaTopMarginInitial = 3000;
			pdvc->m_dympLineSpace = -10000;
			pdvc->m_nInitialParas = 1;

			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight1, dysUsedHeight2, ysStartNextPage1, ysStartNextPage2;

			// Layout the first two pages. We have two columns, each 100 pixels wide.
			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 0, 2, &dysUsedHeight1,
				&ysStartNextPage1);
			int dysHeightFirstColumn;
			int dysHeightSecondColumn;
			m_qlay->ColumnHeight(0, &dysHeightFirstColumn);
			m_qlay->ColumnHeight(0, &dysHeightSecondColumn);
			ysStartThisPage = ysStartNextPage1;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 1, 2, &dysUsedHeight2,
				&ysStartNextPage2);

			// The second paragraph should be the last line in the first column, and it
			// should also be the last line of the second paragraph.
			VwStringBox * psboxLastOnPage = GetBoxAtY(m_qlay, dysHeightFirstColumn);
			unitpp::assert_true("Didn't find box", psboxLastOnPage);
			unitpp::assert_eq("Last line of first column should belong to second para",
				dynamic_cast<VwGroupBox*>(m_qlay->FirstBox())->FirstBox()->Next(),
				psboxLastOnPage->Container());
			unitpp::assert_true("Last line of first column should be last line of second para",
				!psboxLastOnPage->NextOrLazy());
		}

		// Tests that the first two lines of a paragraph are put at the bottom of the column
		// (TE-5569)
		void testOrphan_FitInColumn()
		{
			StrUni stuPara1(L"This is the first test paragraph that we are trying to lay out. Some more text.");
			ITsStringPtr qtss;
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara1, kflidStTxtPara_Contents, qtss);

			StrUni stuPara2(L"This is the second test paragraph that we are trying to lay out. We hope it's too long to fit on one line.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara2, kflidStTxtPara_Contents, qtss);

			// set widow/orphan control for second para
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			qtpb->SetIntPropValues(ktptWidowOrphanControl, ktpvEnum, kttvForceOn);

			ITsTextPropsPtr qttp;
			qtpb->GetTextProps(&qttp);
			m_qcda->CacheUnknown(khvoTlpPara2, kflidStPara_StyleRules, qttp);

			// This will create a section (two texts with three paragraphs each).
			CreateTestStTexts(3);

			SetupRootWithoutMargins();
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympParaTopMarginInitial = 3000;
			pdvc->m_dympLineSpace = -10000;
			pdvc->m_nInitialParas = 1;

			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight1, dysUsedHeight2, ysStartNextPage1, ysStartNextPage2;

			// Layout the first two pages. We have two columns, each 100 pixels wide.
			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 0, 2, &dysUsedHeight1,
				&ysStartNextPage1);
			int dysHeightFirstColumn;
			int dysHeightSecondColumn;
			m_qlay->ColumnHeight(0, &dysHeightFirstColumn);
			m_qlay->ColumnHeight(0, &dysHeightSecondColumn);
			ysStartThisPage = ysStartNextPage1;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 1, 2, &dysUsedHeight2,
				&ysStartNextPage2);

			// The last line of the first column should be the second line of the second
			// paragraph.
			VwStringBox * psboxLastOnPage = GetBoxAtY(m_qlay, dysHeightFirstColumn);
			unitpp::assert_true("Didn't find box", psboxLastOnPage);
			unitpp::assert_eq("Last line of first column should belong to second para",
				dynamic_cast<VwGroupBox*>(m_qlay->FirstBox())->FirstBox()->Next(),
				psboxLastOnPage->Container());
			unitpp::assert_eq("Last line of first column should be second line of second para",
				psboxLastOnPage->Container()->FirstBox()->Next(), psboxLastOnPage);
		}

		// Tests that we don't leave the last line of a paragraph alone at the top of the column
		// (TE-5568)
		void testWidow_BreakEarly()
		{
			StrUni stuPara1(L"This is the first test paragraph that we are trying to lay out.");
			ITsStringPtr qtss;
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara1, kflidStTxtPara_Contents, qtss);

			StrUni stuPara2(L"This is the second test paragraph that we are trying to lay out.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara2, kflidStTxtPara_Contents, qtss);

			// set widow/orphan control for second para
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			qtpb->SetIntPropValues(ktptWidowOrphanControl, ktpvEnum, kttvForceOn);

			ITsTextPropsPtr qttp;
			qtpb->GetTextProps(&qttp);
			m_qcda->CacheUnknown(khvoTlpPara2, kflidStPara_StyleRules, qttp);

			// This will create a section (two texts with two paragraphs each).
			CreateTestStTexts(2);

			SetupRootWithoutMargins();
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympParaTopMarginInitial = 3000;
			pdvc->m_dympLineSpace = -10000;
			pdvc->m_nInitialParas = 1;

			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight1, dysUsedHeight2, ysStartNextPage1, ysStartNextPage2;

			// Layout the first two pages. We have two columns, each 100 pixels wide.
			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 0, 2, &dysUsedHeight1,
				&ysStartNextPage1);
			int dysHeightFirstColumn;
			int dysHeightSecondColumn;
			m_qlay->ColumnHeight(0, &dysHeightFirstColumn);
			m_qlay->ColumnHeight(0, &dysHeightSecondColumn);
			ysStartThisPage = ysStartNextPage1;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 1, 2, &dysUsedHeight2,
				&ysStartNextPage2);

			// The second paragraph should be the last line in the first column.
			// The second column should have the last two lines of the second paragraph.
			VwStringBox * psboxLastOnPage = GetBoxAtY(m_qlay, dysHeightFirstColumn);
			unitpp::assert_true("Didn't find box", psboxLastOnPage);
			unitpp::assert_eq("Last line of first column should belong to second para",
				dynamic_cast<VwGroupBox*>(m_qlay->FirstBox())->FirstBox()->Next(),
				psboxLastOnPage->Container());
			unitpp::assert_true("Last line of first column should have two more lines in next column",
				psboxLastOnPage->NextOrLazy()->NextOrLazy());
		}

		// Test that footnotes don't overlap the text with widow/orphan control.
		void testWidowOrphan_WithFootnotes()
		{
			StrUni stuPara1(L"This is the first test paragraph.");
			ITsStringPtr qtss;
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoTlpPara1, kflidStTxtPara_Contents, qtss);

			StrUni stuPara2(L"This is the second test paragraph that we are trying to lay out. It has more \
text so that it continues on the next page.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			ITsStrBldrPtr qtsb;
			qtss->GetBldr(&qtsb);
			// Add a footnote at the beginning of para 2 and one in the middle
			AddFootnoteToBldr(qtsb, kguidDummyFootnote1, 0);
			AddFootnoteToBldr(qtsb, kguidDummyFootnote2, 20);
			qtsb->GetString(&qtss);
			m_qcda->CacheStringProp(khvoTlpPara2, kflidStTxtPara_Contents, qtss);

			// set widow/orphan control for second para
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			qtpb->SetIntPropValues(ktptWidowOrphanControl, ktpvEnum, kttvForceOn);

			ITsTextPropsPtr qttp;
			qtpb->GetTextProps(&qttp);
			m_qcda->CacheUnknown(khvoTlpPara2, kflidStPara_StyleRules, qttp);

			// This will create a section (two texts with two paragraphs each).
			CreateTestStTexts(2);

			SetupRootWithoutMargins();
			DummyParaVc * pdvc = dynamic_cast<DummyParaVc *>(m_qvc.Ptr());
			pdvc->m_dympParaTopMarginInitial = 3000;
			pdvc->m_dympLineSpace = -10000;
			pdvc->m_nInitialParas = 1;

			DummyLayoutMgrPtr qlayoutMgr;
			qlayoutMgr.Attach(NewObj DummyLayoutMgr());
			qlayoutMgr->m_nScenario = 1;
			m_qlay->SetManager(qlayoutMgr);

			int dysUsedHeight1, ysStartNextPage1;

			// Layout the first page. We have one column, 100 pixels wide.
			int ysStartThisPage = 0;
			m_qlay->LayoutPage(m_qvg32, 100, 100, &ysStartThisPage, 0, 1, &dysUsedHeight1,
				&ysStartNextPage1);

			// The two footnotes reduced the available height for the paragraphs by 2 * 17 = 34.
			// The used height should be less than 100 - 34 = 66.
			unitpp::assert_true("Footnotes should reduce used height",
				dysUsedHeight1 <= 66);
		}

	public:
		virtual void Setup()
		{
			CreateTestWritingSystemFactory();
			m_qcda.CreateInstance(CLSID_VwCacheDa);
			m_qcda->QueryInterface(IID_ISilDataAccess, (void **)&m_qsda);
			m_qsda->putref_WritingSystemFactory(g_qwsf);
			m_qtsf.CreateInstance(CLSID_TsStrFactory);
			// Now make the root box and view constructor and Graphics object.
			m_qlay.Attach(NewObj VwLayoutStream());
			m_hdc = 0;
			m_qvg32.CreateInstance(CLSID_VwGraphicsWin32);
			m_hdc = GetTestDC();
			m_qvg32->Initialize(m_hdc);
			m_qlay->putref_DataAccess(m_qsda);
			m_qdrs.Attach(NewObj DummyRootSite());
			m_rcSrc = Rect(0, 0, 96, 96);
			m_qdrs->SetRects(m_rcSrc, m_rcSrc);
			m_qdrs->SetGraphics(m_qvg32);
			m_qlay->SetSite(m_qdrs);
			m_qdrs->SetRootBox(m_qlay);
		}
		virtual void Teardown()
		{
			m_qcda.Clear();
			m_qsda.Clear();
			m_qtsf.Clear();
			m_qvc.Clear();
			m_qvg32->ReleaseDC();
			ReleaseTestDC(m_hdc);
			m_qlay->Close();
			m_qlay.Clear();
			m_qvg32.Clear();
			m_qdrs.Clear();
			CloseTestWritingSystemFactory();
		}
	};
}

#endif /*TESTLAYOUTPAGE_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
