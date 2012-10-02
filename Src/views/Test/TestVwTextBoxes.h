/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2006 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestVwTextBoxes.h
Responsibility:
Last reviewed:

	Unit tests for the VwBox derived classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestVwTextBoxes_H_INCLUDED
#define TestVwTextBoxes_H_INCLUDED

#pragma once

#include "testViews.h"
#include "resource.h"

namespace TestViews
{
	// Display a literal
	class PictureInExactLineSpacingParaVc : public DummyBaseVc
	{
	public:
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case 1: // Display the literal in an exact line-spacing paragraph.
				pvwenv->put_IntProperty(ktptLineHeight, ktpvMilliPoint, -15000);
				pvwenv->OpenParagraph();
				pvwenv->AddString(m_qtssLit);
				pvwenv->CloseParagraph();
				break;

			case 2: // Display literal string in two separate paragraphs, followed by one with the picture.
				pvwenv->put_IntProperty(ktptLineHeight, ktpvMilliPoint, -15000);
				pvwenv->OpenParagraph();
				pvwenv->AddString(m_qtssLit);
				pvwenv->CloseParagraph();
				pvwenv->put_IntProperty(ktptLineHeight, ktpvMilliPoint, -15000);
				pvwenv->put_IntProperty(ktptFontSize, ktpvMilliPoint, 72000);
				pvwenv->OpenParagraph();
				pvwenv->AddString(m_qtssLit);
				pvwenv->CloseParagraph();
				pvwenv->put_IntProperty(ktptLineHeight, ktpvMilliPoint, -15000);
				pvwenv->OpenParagraph();
				pvwenv->AddPicture(m_qPicture, 987, 0, 0);
				pvwenv->CloseParagraph();
				break;
			}
			return S_OK;
		}
		PictureInExactLineSpacingParaVc(ITsString * ptssLit, IPicturePtr qPicture)
		{
			m_qtssLit = ptssLit;
			m_qPicture = qPicture;
		}
		ITsStringPtr m_qtssLit;
		IPicturePtr m_qPicture;
	};

	class TestVwParagraphBox : public unitpp::suite
	{
		// Maximum number of previous strings to add before testing strings
		// in text sources for the CompareSourceStrings tests
		static const int kmaxPrevStrings = 2;
	public:
		// Exposes DoPartialLayout for testing
		class TestingBox: public VwParagraphBox
		{
		public:
			int RightEdge()
			{
				return m_dxsRightEdge;
			}
			void DoPartialLayout(IVwGraphics * pvg, VwBox * pboxStart, int cLinesToSave,
				int dyStart, int dyPrevDescent, int ichMinDiff, int ichLimDiff,
				int cchLenDiff)
			{
				VwParagraphBox::DoPartialLayout(pvg, pboxStart, cLinesToSave, dyStart, dyPrevDescent,
					ichMinDiff, ichLimDiff, cchLenDiff);
			}

			static void CompareSourceStrings(VwTxtSrc * pts1, VwTxtSrc * pts2, int itss,
				int * pichwMinDiff, int * pichwLimDiff)
			{
				VwParagraphBox::CompareSourceStrings(pts1, pts2, itss, pichwMinDiff, pichwLimDiff);
			}
		};

		// Tests that doing a full layout of a paragraph gets the right value for the
		// right edge. (TE-2787)
		void testLayout_RightEdge()
		{
			// Now make a long string as the content of paragraph 1.
			ITsStringPtr qtss;
			StrUni stuPara1(L"This is the first test paragraph. This is the first test paragraph. This is the first test paragraph. This is the first test paragraph. ");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss);

			// Now make each of them the paragraphs of an StText.
			HVO hvoText1 = 101;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, &hvoPara1, 1);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[1] = {101};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 1);

			m_qvc.Attach(NewObj DummyVc());
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 1, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);

			VwParagraphBox * pboxFirst = dynamic_cast<VwParagraphBox *>(m_qrootb->FirstBox());
			unitpp::assert_true("first box is para", pboxFirst != NULL);
			// the paragraph has a border around with 3pt and trailing border of 10pt = 13pt.
			// The width was 300px. With dpi settings of 96dpi this gives a right edge of
			// 300px - 13pt * 96dpi/72ppi = 283px.
			unitpp::assert_eq("Right edge is wrong", 283, ((TestingBox*)pboxFirst)->RightEdge());
		}

		// Tests that doing a partial layout of a paragraph gets the right value for the
		// right edge. (TE-2787)
		void testPartialLayout_RightEdge()
		{
			// Make a long string as the content of paragraph 1.
			ITsStringPtr qtss;
			StrUni stuPara1(L"This is the first test paragraph. This is the first test paragraph. This is the first test paragraph. This is the first test paragraph. ");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss);

			// Now make each of them the paragraphs of an StText.
			HVO hvoText1 = 101;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, &hvoPara1, 1);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[1] = {101};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 1);

			m_qvc.Attach(NewObj DummyVc());
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 1, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);

			VwParagraphBox * pboxFirst = dynamic_cast<VwParagraphBox *>(m_qrootb->FirstBox());
			unitpp::assert_true("first box is para", pboxFirst != NULL);

			((TestingBox*)pboxFirst)->DoPartialLayout(m_qvg32, pboxFirst, 0, 0, 0, 1, 2, 1);

			// the paragraph has a border around with 3pt and trailing border of 10pt = 13pt.
			// The width was 300px. With dpi settings of 96dpi this gives a right edge of
			// 300px - 13pt * 96dpi/72ppi = 283px.
			unitpp::assert_eq("Right edge is wrong", 283, ((TestingBox*)pboxFirst)->RightEdge());
		}

		// Tests laying out a picture when exact line spacing is specified for the
		// paragraph. (TE-3391)
		void testPictureLayoutWithExactLineSpacing()
		{
			// Make a string.
			ITsStringPtr qtss;
			StrUni stuPara1(L"Para");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);

			// Create a picture.
			HRSRC hBitmap = ::FindResource(NULL, MAKEINTRESOURCE(IDB_NICEGUY), L"BINARY");
			unitpp::assert_true("Found bitmap", hBitmap);
			HGLOBAL hglobal = ::LoadResource(NULL, hBitmap);
			unitpp::assert_true("Loaded bitmap", hglobal);
			byte * pbData = (byte *)::LockResource(hglobal);
			IPicturePtr qpicture;
			CheckHr(m_qvg32->MakePicture(pbData, ::SizeofResource(NULL, hBitmap), &qpicture));

			// Establish the baseline: layout the view without the picture
			m_qvc.Attach(NewObj PictureInExactLineSpacingParaVc(qtss, qpicture));
			CheckHr(m_qrootb->SetRootObject(1223, m_qvc, 1, NULL));
			CheckHr(m_qrootb->Layout(m_qvg32, 300));
			int dyHeightWithoutPicture = m_qrootb->Height();
			unitpp::assert_true("Height is exactly 15 points (20 pixels).", dyHeightWithoutPicture == 20);

			// Now re-layout the view with the picture
			CheckHr(m_qrootb->SetRootObject(1223, m_qvc, 2, NULL));
			int dyHeightWithPicture = m_qrootb->Height();
			unitpp::assert_true("Height is increased by the right amount (two lines + picture height, rounded to nearest multiple of line height.",
				140 == dyHeightWithPicture);
			VwBox * box = m_qrootb->FirstBox();
			VwBox * pictureBox = ((VwGroupBox *)box->Next()->Next())->FirstBox();
			unitpp::assert_true("Picture box is at correct location.", 0 == pictureBox->Top());
			unitpp::assert_true("Paragraph containgin picture box has correct height.", 100 == pictureBox->Container()->Height());
		}

		// Tests the VwBox::IsPointInside() method when layed out at screen resolution.
		void testIsPointInside_ScreenResolution()
		{
			ITsStringPtr qtss1;
			StrUni stuPara1(L"String1");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss1);
			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss1);
			ITsStringPtr qtss2;
			StrUni stuPara2(L"String2");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss2);
			HVO hvoPara2 = 2;
			m_qcda->CacheStringProp(hvoPara2, kflidStTxtPara_Contents, qtss2);

			// Now make the paragraphs into StTexts.
			HVO hvoText1 = 101;
			HVO hvoText2 = 102;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, &hvoPara1, 1);
			m_qcda->CacheVecProp(hvoText2, kflidStText_Paragraphs, &hvoPara2, 1);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[] = {hvoText1, hvoText2};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 2);

			m_qvc.Attach(NewObj DummyVc());
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 6, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);

			// we did a layout with screen resolution (96dpi).
			// The text is a little more than one inch wide (102).
			// If we call IsPointInside for all our (4) boxes and pass in exactly 1 inch,
			// it should return true only for the last box.
			Rect rcSrc(0, 0, 96, 96);
			// First box
			VwBox* pbox = dynamic_cast<VwGroupBox*>(m_qrootb->FirstBox())->FirstBox();
			unitpp::assert_true("Point shouldn't be considered inside of first box",
				!pbox->IsPointInside(96, 7, rcSrc, rcSrc));
			// Second box
			pbox = pbox->NextInRootSeq();
			unitpp::assert_true("Point shouldn't be considered inside of second box",
				!pbox->IsPointInside(96, 7, rcSrc, rcSrc));
			// Third box
			pbox = pbox->NextInRootSeq();
			unitpp::assert_true("Point shouldn't be considered inside of third box",
				!pbox->IsPointInside(96, 7, rcSrc, rcSrc));
			// Fourth box
			pbox = pbox->NextInRootSeq();
			unitpp::assert_true("Point should be considered inside of fourth box",
				pbox->IsPointInside(96, 7, rcSrc, rcSrc));
		}

		// Tests the VwBox::IsPointInside() method when layed out with printer resolution.
		// Point we test is in screen resolution (TE-5956).
		void testIsPointInside_PrinterResolution()
		{
			ITsStringPtr qtss1;
			StrUni stuPara1(L"String1");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss1);
			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss1);
			ITsStringPtr qtss2;
			StrUni stuPara2(L"String2");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss2);
			HVO hvoPara2 = 2;
			m_qcda->CacheStringProp(hvoPara2, kflidStTxtPara_Contents, qtss2);

			// Now make the paragraphs into StTexts.
			HVO hvoText1 = 101;
			HVO hvoText2 = 102;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, &hvoPara1, 1);
			m_qcda->CacheVecProp(hvoText2, kflidStText_Paragraphs, &hvoPara2, 1);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[] = {hvoText1, hvoText2};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 2);

			// Set printer resolution on graphics object
			Rect rcPrinter(0, 0, 1200, 1200);
			Rect rcScreen(0, 0, 96, 96);
			m_qdrs->SetRects(rcPrinter, rcScreen);
			m_qvg32->put_XUnitsPerInch(1200);
			m_qvg32->put_YUnitsPerInch(1200);

			m_qvc.Attach(NewObj DummyVc());
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 6, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 4000);
			unitpp::assert_true("Layout succeeded", hr == S_OK);

			// we did a layout with screen resolution (1200dpi).
			// The text is a little more than one inch wide (1310).
			// If we call IsPointInside for all our (4) boxes and pass in exactly 1 inch
			// at screen resolution, it should return true only for the last box.
			Rect rcSrc(0, 0, 1200, 1200);
			Rect rcDst(0, 0, 96, 96);
			// First box
			VwBox* pbox = dynamic_cast<VwGroupBox*>(m_qrootb->FirstBox())->FirstBox();
			unitpp::assert_true("Point shouldn't be considered inside of first box",
				!pbox->IsPointInside(96, 7, rcSrc, rcDst));
			// Second box
			pbox = pbox->NextInRootSeq();
			unitpp::assert_true("Point shouldn't be considered inside of second box",
				!pbox->IsPointInside(96, 7, rcSrc, rcDst));
			// Third box
			pbox = pbox->NextInRootSeq();
			unitpp::assert_true("Point shouldn't be considered inside of third box",
				!pbox->IsPointInside(96, 7, rcSrc, rcDst));
			// Fourth box
			pbox = pbox->NextInRootSeq();
			unitpp::assert_true("Point should be considered inside of fourth box",
				pbox->IsPointInside(96, 7, rcSrc, rcDst));
		}

		// Tests the VwParagraphBox::ExtraHeightIfNotFollowedByPara() method when the paragraph
		// consist of a single line and we don't use exact line spacing.
		void testExtraHeightForDropCap_SingleLinePara()
		{
			ITsPropsBldrPtr qtpb;
			m_qttpParaStyle->GetBldr(&qtpb);
			qtpb->SetIntPropValues(ktptLineHeight, ktpvMilliPoint, 24000); // 24 pt line spacing
			qtpb->GetTextProps(&m_qttpParaStyle);
			m_qvss->PutStyle(m_paragraphStyleName, NULL, 0, 0, 0, 1, false, false, m_qttpParaStyle);

			// Create a paragraph
			ITsStringPtr qtss1;
			StrUni stuPara1(L"1Short para.");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss1);

			ITsStrBldrPtr qtsb;
			qtss1->GetBldr(&qtsb);
			qtsb->SetStrPropValue(0, 1, ktptNamedStyle, m_chapterNumberStyle);
			qtsb->GetString(&qtss1);

			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss1);

			// Now make the paragraphs into StTexts.
			HVO hvoText1 = 101;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, &hvoPara1, 1);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[] = {hvoText1};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 1);

			// Set printer resolution on graphics object
			Rect rcPrinter(0, 0, 1200, 1200);
			Rect rcScreen(0, 0, 96, 96);
			m_qdrs->SetRects(rcPrinter, rcScreen);
			m_qvg32->put_XUnitsPerInch(rcPrinter.Width());
			m_qvg32->put_YUnitsPerInch(rcPrinter.Height());

			m_qvc.Attach(NewObj DummyVc(m_paragraphStyleName));
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 11, m_qvss);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 4000);
			unitpp::assert_true("Layout should return S_OK", hr == S_OK);

			VwParagraphBox* pvpbox = dynamic_cast<VwParagraphBox*>(m_qrootb->FirstBox());
			int expectedExtraHeight = MulDiv(pvpbox->Style()->LineHeight(), rcPrinter.Height(), kdzmpInch);
			unitpp::assert_eq("Extra height should be one line for single-line para",
				expectedExtraHeight, pvpbox->ExtraHeightIfNotFollowedByPara());
		}

		// Tests the VwParagraphBox::ExtraHeightIfNotFollowedByPara() method when the paragraph
		// consist of a single line and we use exact line spacing.
		void testExtraHeightForDropCap_SingleLinePara_ExactLineSpacing()
		{
			// Create a paragraph
			ITsStringPtr qtss1;
			StrUni stuPara1(L"1Short para.");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss1);

			ITsStrBldrPtr qtsb;
			qtss1->GetBldr(&qtsb);
			qtsb->SetStrPropValue(0, 1, ktptNamedStyle, m_chapterNumberStyle);
			qtsb->GetString(&qtss1);

			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss1);

			// Now make the paragraphs into StTexts.
			HVO hvoText1 = 101;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, &hvoPara1, 1);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[] = {hvoText1};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 1);

			// Set printer resolution on graphics object
			Rect rcPrinter(0, 0, 1200, 1200);
			Rect rcScreen(0, 0, 96, 96);
			m_qdrs->SetRects(rcPrinter, rcScreen);
			m_qvg32->put_XUnitsPerInch(rcPrinter.Width());
			m_qvg32->put_YUnitsPerInch(rcPrinter.Height());

			m_qvc.Attach(NewObj DummyVc(m_paragraphStyleName));
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 11, m_qvss);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 4000);
			unitpp::assert_true("Layout should return S_OK", hr == S_OK);

			VwParagraphBox* pvpbox = dynamic_cast<VwParagraphBox*>(m_qrootb->FirstBox());
			int expectedExtraHeight = MulDiv(pvpbox->Style()->LineHeight(), rcPrinter.Height(), kdzmpInch);
			unitpp::assert_eq("Extra height should be one line for single-line para",
				expectedExtraHeight, pvpbox->ExtraHeightIfNotFollowedByPara());
		}

		// Tests the VwParagraphBox::ExtraHeightIfNotFollowedByPara() method when the paragraph
		// consist of multiple lines.
		void testExtraHeightForDropCap_MultiLinePara()
		{
			// Create a paragraph
			ITsStringPtr qtss1;
			StrUni stuPara1(L"1This is the test paragraph that we are trying to lay out. We hope it's too long to fit on one line.");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss1);

			ITsStrBldrPtr qtsb;
			qtss1->GetBldr(&qtsb);
			qtsb->SetStrPropValue(0, 1, ktptNamedStyle, m_chapterNumberStyle);
			qtsb->GetString(&qtss1);

			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss1);

			// Now make the paragraphs into StTexts.
			HVO hvoText1 = 101;
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, &hvoPara1, 1);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[] = {hvoText1};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 1);

			// Set printer resolution on graphics object
			Rect rcPrinter(0, 0, 1200, 1200);
			Rect rcScreen(0, 0, 96, 96);
			m_qdrs->SetRects(rcPrinter, rcScreen);
			m_qvg32->put_XUnitsPerInch(rcPrinter.Width());
			m_qvg32->put_YUnitsPerInch(rcPrinter.Height());

			m_qvc.Attach(NewObj DummyVc(m_paragraphStyleName));
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 11, m_qvss);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 4000);
			unitpp::assert_true("Layout should return S_OK", hr == S_OK);

			VwParagraphBox* pvpbox = dynamic_cast<VwParagraphBox*>(m_qrootb->FirstBox());
			unitpp::assert_eq("Extra height should be 0 for multi-line para",
				0, pvpbox->ExtraHeightIfNotFollowedByPara());
		}

		// Tests the VwParagraphBox::ExtraHeightIfNotFollowedByPara() method when the paragraph
		// consists of a single line and there are more paragraphs
		void testExtraHeightForDropCap_MultiPara()
		{
			// Create a paragraph
			ITsStringPtr qtss1;
			StrUni stuPara1(L"1Short para.");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss1);

			ITsStrBldrPtr qtsb;
			qtss1->GetBldr(&qtsb);
			qtsb->SetStrPropValue(0, 1, ktptNamedStyle, m_chapterNumberStyle);
			qtsb->GetString(&qtss1);

			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss1);

			ITsStringPtr qtss2;
			StrUni stuPara2(L"This is the second paragraph that contains more text. It doesn't really matter if it is on one or more lines.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss2);

			HVO hvoPara2 = 2;
			m_qcda->CacheStringProp(hvoPara2, kflidStTxtPara_Contents, qtss2);

			// Now make the paragraphs into StTexts.
			HVO hvoText1 = 101;
			HVO rghvoParas[] = { hvoPara1, hvoPara2};
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, rghvoParas, 2);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[] = {hvoText1};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 1);

			// Set printer resolution on graphics object
			Rect rcPrinter(0, 0, 1200, 1200);
			Rect rcScreen(0, 0, 96, 96);
			m_qdrs->SetRects(rcPrinter, rcScreen);
			m_qvg32->put_XUnitsPerInch(rcPrinter.Width());
			m_qvg32->put_YUnitsPerInch(rcPrinter.Height());

			m_qvc.Attach(NewObj DummyVc(m_paragraphStyleName));
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 11, m_qvss);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 4000);
			unitpp::assert_true("Layout should return S_OK", hr == S_OK);

			VwParagraphBox* pvpbox = dynamic_cast<VwParagraphBox*>(m_qrootb->FirstBox());
			unitpp::assert_eq("Extra height should be 0 if para is followed by another para",
				0, pvpbox->ExtraHeightIfNotFollowedByPara());
		}

		// Tests the VwParagraphBox::ExtraHeightIfNotFollowedByPara() method when the paragraph
		// consists of a single line and is followed by a paragraph with a chapter number
		void testExtraHeightForDropCap_MultiParaWithChapterNumber()
		{
			// Create the first paragraph
			ITsStringPtr qtss1;
			StrUni stuPara1(L"1Short para.");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss1);

			ITsStrBldrPtr qtsb;
			qtss1->GetBldr(&qtsb);
			qtsb->SetStrPropValue(0, 1, ktptNamedStyle, m_chapterNumberStyle);
			qtsb->GetString(&qtss1);

			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss1);

			// Create the second paragraph
			ITsStringPtr qtss2;
			StrUni stuPara2(L"2This is the second paragraph that contains more text. It doesn't really matter if it is on one or more lines.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss2);

			qtss2->GetBldr(&qtsb);
			qtsb->SetStrPropValue(0, 1, ktptNamedStyle, m_chapterNumberStyle);
			qtsb->GetString(&qtss2);

			HVO hvoPara2 = 2;
			m_qcda->CacheStringProp(hvoPara2, kflidStTxtPara_Contents, qtss2);

			// Now make the paragraphs into StTexts.
			HVO hvoText1 = 101;
			HVO rghvoParas[] = { hvoPara1, hvoPara2};
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, rghvoParas, 2);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[] = {hvoText1};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 1);

			// Set printer resolution on graphics object
			Rect rcPrinter(0, 0, 1200, 1200);
			Rect rcScreen(0, 0, 96, 96);
			m_qdrs->SetRects(rcPrinter, rcScreen);
			m_qvg32->put_XUnitsPerInch(rcPrinter.Width());
			m_qvg32->put_YUnitsPerInch(rcPrinter.Height());

			m_qvc.Attach(NewObj DummyVc(m_paragraphStyleName));
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 11, m_qvss);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 4000);
			unitpp::assert_true("Layout should return S_OK", hr == S_OK);

			VwParagraphBox* pvpbox = dynamic_cast<VwParagraphBox*>(m_qrootb->FirstBox());
			int expectedExtraHeight = MulDiv(pvpbox->Style()->LineHeight(), rcPrinter.Height(), kdzmpInch);
			unitpp::assert_eq("Extra height should be one line for single-line para followed by drop cap para",
				expectedExtraHeight, pvpbox->ExtraHeightIfNotFollowedByPara());
		}

		// Tests the VwParagraphBox::KeepWithNext() method when the paragraph
		// consists of a single line and is followed by a paragraph without a chapter number
		void testKeepWithNext_MultiPara()
		{
			// Create the first paragraph
			ITsStringPtr qtss1;
			StrUni stuPara1(L"1Short para.");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss1);

			ITsStrBldrPtr qtsb;
			qtss1->GetBldr(&qtsb);
			qtsb->SetStrPropValue(0, 1, ktptNamedStyle, m_chapterNumberStyle);
			qtsb->GetString(&qtss1);

			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss1);

			// Create the second paragraph
			ITsStringPtr qtss2;
			StrUni stuPara2(L"This is the second paragraph that contains more text. It doesn't really matter if it is on one or more lines.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss2);

			HVO hvoPara2 = 2;
			m_qcda->CacheStringProp(hvoPara2, kflidStTxtPara_Contents, qtss2);

			// Now make the paragraphs into StTexts.
			HVO hvoText1 = 101;
			HVO rghvoParas[] = { hvoPara1, hvoPara2};
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, rghvoParas, 2);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[] = {hvoText1};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 1);

			// Set printer resolution on graphics object
			Rect rcPrinter(0, 0, 1200, 1200);
			Rect rcScreen(0, 0, 96, 96);
			m_qdrs->SetRects(rcPrinter, rcScreen);
			m_qvg32->put_XUnitsPerInch(rcPrinter.Width());
			m_qvg32->put_YUnitsPerInch(rcPrinter.Height());

			m_qvc.Attach(NewObj DummyVc(m_paragraphStyleName));
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 11, m_qvss);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 4000);
			unitpp::assert_true("Layout should return S_OK", hr == S_OK);

			VwParagraphBox* pvpbox = dynamic_cast<VwParagraphBox*>(m_qrootb->FirstBox());
			unitpp::assert_true("KeepWithNext should return true if para overlaps with next para",
				pvpbox->KeepWithNext());
		}

		// Tests the VwParagraphBox::KeepWithNext() method when the paragraph
		// consists of a multiple lines and is followed by a paragraph without a chapter number
		void testKeepWithNext_MultiLinePara()
		{
			// Create the first paragraph
			ITsStringPtr qtss1;
			StrUni stuPara1(L"1This is the first paragraph that contains more text. It doesn't really matter if it is on one or more lines.");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss1);

			ITsStrBldrPtr qtsb;
			qtss1->GetBldr(&qtsb);
			qtsb->SetStrPropValue(0, 1, ktptNamedStyle, m_chapterNumberStyle);
			qtsb->GetString(&qtss1);

			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss1);

			// Create the second paragraph
			ITsStringPtr qtss2;
			StrUni stuPara2(L"This is the second paragraph.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss2);

			HVO hvoPara2 = 2;
			m_qcda->CacheStringProp(hvoPara2, kflidStTxtPara_Contents, qtss2);

			// Now make the paragraphs into StTexts.
			HVO hvoText1 = 101;
			HVO rghvoParas[] = { hvoPara1, hvoPara2};
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, rghvoParas, 2);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[] = {hvoText1};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 1);

			// Set printer resolution on graphics object
			Rect rcPrinter(0, 0, 1200, 1200);
			Rect rcScreen(0, 0, 96, 96);
			m_qdrs->SetRects(rcPrinter, rcScreen);
			m_qvg32->put_XUnitsPerInch(rcPrinter.Width());
			m_qvg32->put_YUnitsPerInch(rcPrinter.Height());

			m_qvc.Attach(NewObj DummyVc(m_paragraphStyleName));
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 11, m_qvss);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 4000);
			unitpp::assert_true("Layout should return S_OK", hr == S_OK);

			VwParagraphBox* pvpbox = dynamic_cast<VwParagraphBox*>(m_qrootb->FirstBox());
			unitpp::assert_true("KeepWithNext should return false if para doesn't overlaps and KeepWithNext isn't set in style",
				!pvpbox->KeepWithNext());
		}

		// Tests the VwParagraphBox::KeepWithNext() method when the paragraph
		// consists of a single line and is followed by a paragraph with a chapter number
		void testKeepWithNext_SingleLineParaFollowedByChapterNumber()
		{
			// Create the first paragraph
			ITsStringPtr qtss1;
			StrUni stuPara1(L"1Short paragraph.");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss1);

			ITsStrBldrPtr qtsb;
			qtss1->GetBldr(&qtsb);
			qtsb->SetStrPropValue(0, 1, ktptNamedStyle, m_chapterNumberStyle);
			qtsb->GetString(&qtss1);

			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss1);

			// Create the second paragraph
			ITsStringPtr qtss2;
			StrUni stuPara2(L"2Second paragraph.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss2);

			qtss2->GetBldr(&qtsb);
			qtsb->SetStrPropValue(0, 1, ktptNamedStyle, m_chapterNumberStyle);
			qtsb->GetString(&qtss2);

			HVO hvoPara2 = 2;
			m_qcda->CacheStringProp(hvoPara2, kflidStTxtPara_Contents, qtss2);

			// Now make the paragraphs into StTexts.
			HVO hvoText1 = 101;
			HVO rghvoParas[] = { hvoPara1, hvoPara2};
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, rghvoParas, 2);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[] = {hvoText1};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 1);

			// Set printer resolution on graphics object
			Rect rcPrinter(0, 0, 1200, 1200);
			Rect rcScreen(0, 0, 96, 96);
			m_qdrs->SetRects(rcPrinter, rcScreen);
			m_qvg32->put_XUnitsPerInch(rcPrinter.Width());
			m_qvg32->put_YUnitsPerInch(rcPrinter.Height());

			m_qvc.Attach(NewObj DummyVc(m_paragraphStyleName));
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 11, m_qvss);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 4000);
			unitpp::assert_true("Layout should return S_OK", hr == S_OK);

			VwParagraphBox* pvpbox = dynamic_cast<VwParagraphBox*>(m_qrootb->FirstBox());
			unitpp::assert_true("KeepWithNext should return false if next para has chapter number and KeepWithNext isn't set in style",
				!pvpbox->KeepWithNext());
		}

		// Tests the VwParagraphBox::KeepWithNext() method when the paragraph
		// consists of a single line and is followed by a paragraph with a chapter number
		// and the paragraph style has keep-with-next enabled
		void testKeepWithNext_SingleLineParaFollowedByChapterNumber_StyleSet()
		{
			ITsPropsBldrPtr qtpb;
			m_qttpParaStyle->GetBldr(&qtpb);
			qtpb->SetIntPropValues(ktptKeepWithNext, ktpvEnum, kttvForceOn);
			qtpb->GetTextProps(&m_qttpParaStyle);
			m_qvss->PutStyle(m_paragraphStyleName, NULL, 0, 0, 0, 1, false, false, m_qttpParaStyle);

			// Create the first paragraph
			ITsStringPtr qtss1;
			StrUni stuPara1(L"1Short paragraph.");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss1);

			ITsStrBldrPtr qtsb;
			qtss1->GetBldr(&qtsb);
			qtsb->SetStrPropValue(0, 1, ktptNamedStyle, m_chapterNumberStyle);
			qtsb->GetString(&qtss1);

			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss1);

			// Create the second paragraph
			ITsStringPtr qtss2;
			StrUni stuPara2(L"2Second paragraph.");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss2);

			qtss2->GetBldr(&qtsb);
			qtsb->SetStrPropValue(0, 1, ktptNamedStyle, m_chapterNumberStyle);
			qtsb->GetString(&qtss2);

			HVO hvoPara2 = 2;
			m_qcda->CacheStringProp(hvoPara2, kflidStTxtPara_Contents, qtss2);

			// Now make the paragraphs into StTexts.
			HVO hvoText1 = 101;
			HVO rghvoParas[] = { hvoPara1, hvoPara2};
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, rghvoParas, 2);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[] = {hvoText1};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 1);

			// Set printer resolution on graphics object
			Rect rcPrinter(0, 0, 1200, 1200);
			Rect rcScreen(0, 0, 96, 96);
			m_qdrs->SetRects(rcPrinter, rcScreen);
			m_qvg32->put_XUnitsPerInch(rcPrinter.Width());
			m_qvg32->put_YUnitsPerInch(rcPrinter.Height());

			m_qvc.Attach(NewObj DummyVc(m_paragraphStyleName));
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 11, m_qvss);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 4000);
			unitpp::assert_true("Layout should return S_OK", hr == S_OK);

			VwParagraphBox* pvpbox = dynamic_cast<VwParagraphBox*>(m_qrootb->FirstBox());
			unitpp::assert_true("KeepWithNext should return true if KeepWithNext is set in style",
				pvpbox->KeepWithNext());
		}

		// Tests the VwParagraphBox::KeepWithNext() method when the paragraph
		// is the last paragraph
		void testKeepWithNext_LastPara()
		{
			// Create the first paragraph
			ITsStringPtr qtss1;
			StrUni stuPara1(L"1Short paragraph.");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss1);

			ITsStrBldrPtr qtsb;
			qtss1->GetBldr(&qtsb);
			qtsb->SetStrPropValue(0, 1, ktptNamedStyle, m_chapterNumberStyle);
			qtsb->GetString(&qtss1);

			HVO hvoPara1 = 1;
			m_qcda->CacheStringProp(hvoPara1, kflidStTxtPara_Contents, qtss1);

			// Now make the paragraphs into StTexts.
			HVO hvoText1 = 101;
			HVO rghvoParas[] = { hvoPara1 };
			m_qcda->CacheVecProp(hvoText1, kflidStText_Paragraphs, rghvoParas, 1);

			// And the StTexts to the contents of a dummy property.
			HVO rghvo[] = {hvoText1};
			HVO hvoRoot = 1001;
			m_qcda->CacheVecProp(hvoRoot, kflidTestDummy, rghvo, 1);

			// Set printer resolution on graphics object
			Rect rcPrinter(0, 0, 1200, 1200);
			Rect rcScreen(0, 0, 96, 96);
			m_qdrs->SetRects(rcPrinter, rcScreen);
			m_qvg32->put_XUnitsPerInch(rcPrinter.Width());
			m_qvg32->put_YUnitsPerInch(rcPrinter.Height());

			m_qvc.Attach(NewObj DummyVc(m_paragraphStyleName));
			((DummyVc*)m_qvc.Ptr())->m_dympParaTrailingMargin = 10000;
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 11, m_qvss);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 4000);
			unitpp::assert_true("Layout should return S_OK", hr == S_OK);

			VwParagraphBox* pvpbox = dynamic_cast<VwParagraphBox*>(m_qrootb->FirstBox());
			unitpp::assert_true("KeepWithNext should return false if there are no more paragraph",
				!pvpbox->KeepWithNext());
		}



		// Used for the CompareSourceStrings method tests to easily create and add
		// strings to the specified text source
		void addStringToTxtSrc(VwMappedTxtSrc * pts, VwPropertyStore * pzvps, StrUni stu
			, int ichPropStart, int ichPropLim, int prevStringCount = 0, int customWs = 0)
		{
			ITsStringPtr qtss;
			ITsStrBldrPtr qtsb;
			qtsb.CreateInstance(CLSID_TsStrBldr);
			for (int i = 0; i < prevStringCount; i++)
			{
				qtsb->Clear(); // clear the builder for the new string
				qtsb->Replace(0, 0, StrUni(L"Previous text.").Bstr(), NULL);
				qtsb->GetString(&qtss);
				pts->AddString(qtss, pzvps, NULL);
			}

			qtsb->Clear(); // clear the builder for the new string
			qtsb->Replace(0, 0, stu.Bstr(), NULL);
			if (ichPropStart > -1 && ichPropStart > -1)
			{
				qtsb->SetIntPropValues(ichPropStart, ichPropLim, ktptWs, ktpvDefault
					, (customWs != 0 ? customWs : g_wsFrn));
			}
			qtsb->GetString(&qtss);
			pts->AddString(qtss, pzvps, NULL);
		}

		// Tests the CompareSourceStrings method when the text sources both have only one string
		// and there are only text differences
		void testCompareSourceStrings_diffText()
		{
			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore());
			qzvps->putref_WritingSystemFactory(g_qwsf);

			for (int iPrevText = 0; iPrevText < kmaxPrevStrings; iPrevText++)
			{
				VwMappedTxtSrcPtr qts1;
				qts1.Attach(NewObj VwMappedTxtSrc());
				qts1->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts1, qzvps, StrUni(L"This is the text of the string."), -1, -1, iPrevText);

				VwMappedTxtSrcPtr qts2;
				qts2.Attach(NewObj VwMappedTxtSrc());
				qts2->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts2, qzvps, StrUni(L"This is the tet f the tring."), -1, -1);

				int ichwMinDiff;
				int ichwLimDiff;
				TestingBox::CompareSourceStrings(qts1, qts2, iPrevText, &ichwMinDiff, &ichwLimDiff);
				unitpp::assert_eq("Min difference wrong", 14, ichwMinDiff);
				unitpp::assert_eq("Lim difference wrong", 22, ichwLimDiff);
			}
		}

		// Tests the CompareSourceStrings method when the text sources both have only one string
		// and the strings are the same except for their lengths. This tests the case where
		// the string in the second text source is the beginning of the string of the first
		// text source.
		void testCompareSourceStrings_diffLengthsBeg()
		{
			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore());
			qzvps->putref_WritingSystemFactory(g_qwsf);

			for (int iPrevText = 0; iPrevText < kmaxPrevStrings; iPrevText++)
			{
				// 'of the' has a WS of french
				VwMappedTxtSrcPtr qts1;
				qts1.Attach(NewObj VwMappedTxtSrc());
				qts1->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts1, qzvps, StrUni(L"This is the text of the string."), 17, 23, iPrevText);

				// 'of the' has a WS of french
				VwMappedTxtSrcPtr qts2;
				qts2.Attach(NewObj VwMappedTxtSrc());
				qts2->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts2, qzvps, StrUni(L"This is the text of the"), 17, 23);

				int ichwMinDiff;
				int ichwLimDiff;
				TestingBox::CompareSourceStrings(qts1, qts2, iPrevText, &ichwMinDiff, &ichwLimDiff);
				unitpp::assert_eq("Min difference wrong", 23, ichwMinDiff);
				unitpp::assert_eq("Lim difference wrong", 23, ichwLimDiff);
			}
		}

		// Tests the CompareSourceStrings method when the text sources both have only one string
		// and the strings are the same except for their lengths. This tests the case where
		// the string in the first text source is the beginning of the string of the second
		// text source.
		void testCompareSourceStrings_diffLengthsBeg2()
		{
			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore());
			qzvps->putref_WritingSystemFactory(g_qwsf);

			for (int iPrevText = 0; iPrevText < kmaxPrevStrings; iPrevText++)
			{
				// 'of the' has a WS of french
				VwMappedTxtSrcPtr qts1;
				qts1.Attach(NewObj VwMappedTxtSrc());
				qts1->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts1, qzvps, StrUni(L"This is the text of the"), 17, 23, iPrevText);

				// 'of the string' has a WS of french
				VwMappedTxtSrcPtr qts2;
				qts2.Attach(NewObj VwMappedTxtSrc());
				qts2->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts2, qzvps, StrUni(L"This is the text of the string"), 17, 30);

				int ichwMinDiff;
				int ichwLimDiff;
				TestingBox::CompareSourceStrings(qts1, qts2, iPrevText, &ichwMinDiff, &ichwLimDiff);
				unitpp::assert_eq("Min difference wrong", 23, ichwMinDiff);
				unitpp::assert_eq("Lim difference wrong", 30, ichwLimDiff);
			}
		}

		// Tests the CompareSourceStrings method when the text sources both have only one string
		// and the strings are the same except for their lengths. This tests the case where
		// the string in the second text source is the end of the string of the first
		// text source.
		void testCompareSourceStrings_diffLengthsEnd()
		{
			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore());
			qzvps->putref_WritingSystemFactory(g_qwsf);

			for (int iPrevText = 0; iPrevText < kmaxPrevStrings; iPrevText++)
			{
				// 'is' has a WS of french
				VwMappedTxtSrcPtr qts1;
				qts1.Attach(NewObj VwMappedTxtSrc());
				qts1->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts1, qzvps, StrUni(L"This is the text of the string."), 5, 7, iPrevText);

				// 'is' has a WS of french
				VwMappedTxtSrcPtr qts2;
				qts2.Attach(NewObj VwMappedTxtSrc());
				qts2->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts2, qzvps, StrUni(L"is the text of the string."), 0, 2);

				int ichwMinDiff;
				int ichwLimDiff;
				TestingBox::CompareSourceStrings(qts1, qts2, iPrevText, &ichwMinDiff, &ichwLimDiff);
				unitpp::assert_eq("Min difference wrong", 0, ichwMinDiff);
				unitpp::assert_eq("Lim difference wrong", 0, ichwLimDiff);
			}
		}

		// Tests the CompareSourceStrings method when the text sources both have only one string
		// and the strings are the same except for their lengths. This tests the case where
		// the string in the first text source is the end of the string of the second
		// text source.
		void testCompareSourceStrings_diffLengthsEnd2()
		{
			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore());
			qzvps->putref_WritingSystemFactory(g_qwsf);

			for (int iPrevText = 0; iPrevText < kmaxPrevStrings; iPrevText++)
			{
				// 'is' has a WS of french
				VwMappedTxtSrcPtr qts1;
				qts1.Attach(NewObj VwMappedTxtSrc());
				qts1->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts1, qzvps, StrUni(L"is the text of the string."), 0, 2, iPrevText);

				// 'is' has a WS of french
				VwMappedTxtSrcPtr qts2;
				qts2.Attach(NewObj VwMappedTxtSrc());
				qts2->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts2, qzvps, StrUni(L"This is the text of the string."), 5, 7);

				int ichwMinDiff;
				int ichwLimDiff;
				TestingBox::CompareSourceStrings(qts1, qts2, iPrevText, &ichwMinDiff, &ichwLimDiff);
				unitpp::assert_eq("Min difference wrong", 0, ichwMinDiff);
				unitpp::assert_eq("Lim difference wrong", 5, ichwLimDiff);
			}
		}

		// Tests the CompareSourceStrings method when the text sources both have only one string
		// and the first string has a length of zero.
		void testCompareSourceStrings_zeroLength()
		{
			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore());
			qzvps->putref_WritingSystemFactory(g_qwsf);

			for (int iPrevText = 0; iPrevText < kmaxPrevStrings; iPrevText++)
			{
				VwMappedTxtSrcPtr qts1;
				qts1.Attach(NewObj VwMappedTxtSrc());
				qts1->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts1, qzvps, StrUni(L""), -1, -1, iPrevText);

				VwMappedTxtSrcPtr qts2;
				qts2.Attach(NewObj VwMappedTxtSrc());
				qts2->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts2, qzvps, StrUni(L"This is the text of the string."), -1, -1);

				int ichwMinDiff;
				int ichwLimDiff;
				TestingBox::CompareSourceStrings(qts1, qts2, iPrevText, &ichwMinDiff, &ichwLimDiff);
				unitpp::assert_eq("Min difference wrong", 0, ichwMinDiff);
				unitpp::assert_eq("Lim difference wrong", 31, ichwLimDiff);
			}
		}

		// Tests the CompareSourceStrings method when the text sources both have only one string
		// and the second string has a length of zero.
		void testCompareSourceStrings_zeroLength2()
		{
			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore());
			qzvps->putref_WritingSystemFactory(g_qwsf);

			for (int iPrevText = 0; iPrevText < kmaxPrevStrings; iPrevText++)
			{
				VwMappedTxtSrcPtr qts1;
				qts1.Attach(NewObj VwMappedTxtSrc());
				qts1->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts1, qzvps, StrUni(L"This is the text of the string."), -1, -1, iPrevText);

				VwMappedTxtSrcPtr qts2;
				qts2.Attach(NewObj VwMappedTxtSrc());
				qts2->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts2, qzvps, StrUni(L""), -1, -1);

				int ichwMinDiff;
				int ichwLimDiff;
				TestingBox::CompareSourceStrings(qts1, qts2, iPrevText, &ichwMinDiff, &ichwLimDiff);
				unitpp::assert_eq("Min difference wrong", 0, ichwMinDiff);
				unitpp::assert_eq("Lim difference wrong", 0, ichwLimDiff);
			}
		}

		// Tests the CompareSourceStrings method when the text sources both have only one string
		// and both strings have a length of zero but with different props.
		void testCompareSourceStrings_zeroLengthProps()
		{
			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore());
			qzvps->putref_WritingSystemFactory(g_qwsf);

			for (int iPrevText = 0; iPrevText < kmaxPrevStrings; iPrevText++)
			{
				VwMappedTxtSrcPtr qts1;
				qts1.Attach(NewObj VwMappedTxtSrc());
				qts1->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts1, qzvps, StrUni(L""), -1, -1, iPrevText);

				VwMappedTxtSrcPtr qts2;
				qts2.Attach(NewObj VwMappedTxtSrc());
				qts2->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts2, qzvps, StrUni(L""), 0, 0);

				int ichwMinDiff;
				int ichwLimDiff;
				TestingBox::CompareSourceStrings(qts1, qts2, iPrevText, &ichwMinDiff, &ichwLimDiff);
				unitpp::assert_eq("Min difference wrong", 0, ichwMinDiff);
				unitpp::assert_eq("Lim difference wrong", 0, ichwLimDiff);
			}
		}

		// Tests the CompareSourceStrings method when both text sources have only one string
		// and only differ by props.
		void testCompareSourceStrings_withProps()
		{
			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore());
			qzvps->putref_WritingSystemFactory(g_qwsf);

			for (int iPrevText = 0; iPrevText < kmaxPrevStrings; iPrevText++)
			{
				// 'the text' has a WS of french
				VwMappedTxtSrcPtr qts1;
				qts1.Attach(NewObj VwMappedTxtSrc());
				qts1->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts1, qzvps, StrUni(L"This is the text of the string."), 8, 16, iPrevText);

				// 'text of' has a WS of french
				VwMappedTxtSrcPtr qts2;
				qts2.Attach(NewObj VwMappedTxtSrc());
				qts2->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts2, qzvps, StrUni(L"This is the text of the string."), 12, 19);

				int ichwMinDiff;
				int ichwLimDiff;
				TestingBox::CompareSourceStrings(qts1, qts2, iPrevText, &ichwMinDiff, &ichwLimDiff);
				unitpp::assert_eq("Min difference wrong", 8, ichwMinDiff);
				unitpp::assert_eq("Lim difference wrong", 19, ichwLimDiff);
			}
		}

		// Test the CompareSourceStrings method when both text sources only have one string
		// and there are only prop differences that occur in the same place in the text for
		// each string.
		void testCompareSourceStrings_withProps2()
		{
			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore());
			qzvps->putref_WritingSystemFactory(g_qwsf);

			for (int iPrevText = 0; iPrevText < kmaxPrevStrings; iPrevText++)
			{
				// 'the text' has a WS of german
				VwMappedTxtSrcPtr qts1;
				qts1.Attach(NewObj VwMappedTxtSrc());
				qts1->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts1, qzvps, StrUni(L"This is the text of the string."), 8, 16
					, iPrevText, g_wsGer);

				// 'the text' has a WS of french
				VwMappedTxtSrcPtr qts2;
				qts2.Attach(NewObj VwMappedTxtSrc());
				qts2->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts2, qzvps, StrUni(L"This is the text of the string."), 8, 16);

				int ichwMinDiff;
				int ichwLimDiff;
				TestingBox::CompareSourceStrings(qts1, qts2, iPrevText, &ichwMinDiff, &ichwLimDiff);
				unitpp::assert_eq("Min difference wrong", 8, ichwMinDiff);
				unitpp::assert_eq("Lim difference wrong", 16, ichwLimDiff);
			}
		}

		// Test the CompareSourceStrings method when both text sources only have one string
		// and there are text and prop differences
		void testCompareSourceStrings_withProps3()
		{
			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore());
			qzvps->putref_WritingSystemFactory(g_qwsf);

			for (int iPrevText = 0; iPrevText < kmaxPrevStrings; iPrevText++)
			{
				// 'the text' has a WS of french
				VwMappedTxtSrcPtr qts1;
				qts1.Attach(NewObj VwMappedTxtSrc());
				qts1->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts1, qzvps, StrUni(L"This is the text of the string."), 8, 16, iPrevText);

				// 'text f' has a WS of french
				VwMappedTxtSrcPtr qts2;
				qts2.Attach(NewObj VwMappedTxtSrc());
				qts2->SetWritingSystemFactory(g_qwsf);
				addStringToTxtSrc(qts2, qzvps, StrUni(L"This is the text f the tring."), 12, 18);

				int ichwMinDiff;
				int ichwLimDiff;
				TestingBox::CompareSourceStrings(qts1, qts2, iPrevText, &ichwMinDiff, &ichwLimDiff);
				unitpp::assert_eq("Min difference wrong", 8, ichwMinDiff);
				unitpp::assert_eq("Lim difference wrong", 23, ichwLimDiff);
			}
		}

	public:
		TestVwParagraphBox();

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
			// on the Views DLL heap gets freed by a method that is somehow linked as part of the
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
			m_hdc = ::GetDC(NULL);
			m_qvg32->Initialize(m_hdc);
			m_qrootb->putref_DataAccess(m_qsda);
			m_qdrs.Attach(NewObj DummyRootSite());
			m_rcSrc = Rect(0, 0, 96, 96);
			m_qdrs->SetRects(m_rcSrc, m_rcSrc);
			m_qdrs->SetGraphics(m_qvg32);
			m_qrootb->SetSite(m_qdrs);

			// Create a stylesheet and a Chapter Number style
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			m_chapterNumberStyle = SmartBstr(L"Chapter Number");
			ITsTextPropsPtr qttp;
			qtpb->GetTextProps(&qttp);
			m_qvss.Attach(NewObj VwStylesheet());
			m_qvss->PutStyle(m_chapterNumberStyle, NULL, 0, 0, 0, 0, false, false, qttp);

			// Add the Paragraph style, which uses <default pub font>
			m_paragraphStyleName = SmartBstr(L"Paragraph");
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			SmartBstr font(L"<default pub font>");
			qtpb->SetStrPropValue(ktptFontFamily, font);
			qtpb->SetIntPropValues(ktptLineHeight, ktpvMilliPoint, -24000); // Exactly 24 pt line spacing
			qtpb->GetTextProps(&m_qttpParaStyle);
			m_qvss->PutStyle(m_paragraphStyleName, NULL, 0, 0, 0, 1, false, false, m_qttpParaStyle);
		}
		virtual void Teardown()
		{
			if (m_qvg32)
			{
				m_qvg32->ReleaseDC();
				m_qvg32.Clear();
			}
			if (m_hdc)
				::ReleaseDC(NULL, m_hdc);
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
			m_qvss.Clear();
			m_qttpParaStyle.Clear();
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
		SmartBstr m_chapterNumberStyle;
		ComSmartPtr<VwStylesheet> m_qvss;
		ITsTextPropsPtr m_qttpParaStyle;
		SmartBstr m_paragraphStyleName;
	};

	class TestVwBarBox : public unitpp::suite
	{
	public:
		// Tests that trying to make a selection in a VwBarBox doesn't result in an infinite loop
		// if the bar box is at the end of the view and the previous box is also a VwBarBox.
		void testBarBox_InfiniteLoop()
		{
			m_qvc.Attach(NewObj DummyVc());
			m_qrootb->SetRootObject(hvoRoot, m_qvc, 5, NULL);
			HRESULT hr = m_qrootb->Layout(m_qvg32, 300);
			unitpp::assert_true("Layout succeeded", hr == S_OK);

			IVwSelectionPtr qsel;
			hr = m_qrootb->MakeSelAt(20, 50, m_rcSrc, m_rcSrc, false, &qsel);
			unitpp::assert_true("Shouldn't make a selection in bar boxes", hr == S_FALSE);
		}

		TestVwBarBox();

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
			m_hdc = ::GetDC(NULL);
			m_qvg32->Initialize(m_hdc);
			m_qrootb->putref_DataAccess(m_qsda);
			m_qdrs.Attach(NewObj DummyRootSite());
			m_rcSrc = Rect(0, 0, 96, 96);
			m_qdrs->SetRects(m_rcSrc, m_rcSrc);
			m_qdrs->SetGraphics(m_qvg32);
			m_qrootb->SetSite(m_qdrs);
		}
		virtual void Teardown()
		{
			if (m_qvg32)
			{
				m_qvg32->ReleaseDC();
				m_qvg32.Clear();
			}
			if (m_hdc)
				::ReleaseDC(NULL, m_hdc);
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
#endif /*TestVwTextBoxes_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
