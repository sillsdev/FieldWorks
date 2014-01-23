/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestInsertDiffPara.h
Responsibility: John Thomson
Last reviewed:

	Unit tests for the VwSelection class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTInsertDiff_H_INCLUDED
#define TESTInsertDiff_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{
#define khvoOrigPara1 998
#define khvoOrigPara2 999
#define khvoOrigPara3 997
#define khvoOrigPara4 996
#define khvoOrigPara5 995

#if 0
	// Proposed but never finished for a more complex test.
#define ktagSections 10023
#define ktagHeading 10024;
#define ktagContents 10025;
#define kclidSection 2103; // could use the real one, but minimize the includes...
	// Display of something that has sections, each of which is an StText.
	class SectionVc : public DummyBaseVc
	{
	public:
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case 1: // the root; display the sections.
				pvwenv->AddObjVecItems(ktagSections, this, 2);
			case 2: // section, display the heading and content StTexts
				pvwenv->AddObjProp(ktagHeading, this, 3);
				pvwenv->AddObjProp(ktagBody, this, 3);
			case 3: // StText, display the contents.
				pvwenv->AddObjVecItems(kflidStText_Paragraphs, this, 4);
				break;
			case 4: // StTxtPara, display contents
				pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL);
				break;
			}
			return S_OK;
		}
	};
#endif

	class InsertDiffRootSite : public DummyRootSite
	{
	public: // Class only used in one test, no point in controlling access.
		int m_case; // See the main switch below, various behaviors possible.
		int m_cactInsertDiffPara; // counts how many calls to IDP.
		int m_hvoRoot;
		ComVector<ITsString> m_strings;
		ComVector<ITsTextProps> m_props;
		ITsStringPtr m_qtssTrail;

		InsertDiffRootSite()
		{
			m_case = 0;
			m_cactInsertDiffPara = 0;
		}

		STDMETHOD(OnInsertDiffParas)(IVwRootBox * prootb, ITsTextProps * pttpDest, int cPara,
			ITsTextProps ** prgpttpSrc, ITsString ** prgptssSrc,  ITsString * ptssTrailing,
			VwInsertDiffParaResponse * pidpr)
		{
			m_cactInsertDiffPara++;
			m_strings.Clear();
			m_props.Clear();
			for (int i = 0; i < cPara; i++)
			{
				m_strings.Push(prgptssSrc[i]);
				m_props.Push(prgpttpSrc[i]);
			}
			m_qtssTrail = ptssTrailing;

			switch(m_case)
			{
			case 0:
				*pidpr = kidprDefault;
				return S_OK;
			case 1:
				{
					// Don't actually do anything, but simulate a complete change of the
					// text contents, to verify that destroying the old selection isn't a problem.
					ISilDataAccess * psda;
					m_qrootb->get_DataAccess(&psda);
					int cobj;
					psda->get_VecSize(m_hvoRoot, kflidStText_Paragraphs, &cobj);
					m_qrootb->PropChanged(m_hvoRoot, kflidStText_Paragraphs, 0, cobj, cobj);
					IVwSelectionPtr qsel;
					m_qrootb->MakeSimpleSel(true, true, false, true, &qsel);
					*pidpr = kidprDone;
				}
				return S_OK;
			case 2:
				*pidpr = kidprFail;
				return S_OK;
			}
#if 0 // a much more realistic action on inserting paragraphs, but unnecessary for the
			// simpler cases we're now dealing with. This was designed for an old version
			// of the interface, which inserted one thing at a time. It may still be useful
			// as a source of ideas for a real implementation.

			// Note: for test code, we're not doing CheckHr's on all the COM calls.
			// If the call fails, it is very unlikely it will produce the expected results and
			// make the test pass.
			// defaults.
			*ppselActual = NULL;
			*ppselContinue = NULL;
			*pidpr = kidprContinue;
			switch(m_case)
			{
			case 1: // default behavior
				return S_OK;
			case 2:
				// recognizes a paragraph with style 'heading' as belonging in a heading.
				// If this occurs and the StText is not in a section heading property,
				//	- Truncate the original paragraph where the insertion was made.
				//	- Make a new section and StTexts.
				//	- Put end of original paragraph and remaining paragraphs of old section into new one.
				//	- Make a new empty paragraph for heading of new section.
				//	- Return values telling View to put heading para in heading of new section and
				//		balance in contents.

				// See if this is a heading paragraph. If not we don't know any special behaviors to do.
				ISilDataAccessPtr qsda;
				m_qrootb->get_DataAccess(&qsda);
				ITsTextPropsPtr qttp;
				qsda->get_UnknownProp(hvo, kflidStPara_StyleRules, IID_ITsTextProps,
					(void **) &qttp);
				if (!qttp)
					return S_OK;
				SmartBstr sbstrStyle;
				qttp->GetStrPropValue(kttpNamedStyle, &sbstrStyle);
				if (wcscmp(sbstrStyle.Chars(), L"heading") != 0
					return S_OK;

				// This site knows explicitly about the view constructor in use.
				// In this view constructor, property 0 is the contents of the StTxtPara;
				// property 1 is the paragraphs of the StText;
				// property 2 is either heading or contents.
				HVO hvoSection;
				PropTag tagText; // ktagHeading or ktagContents, prop that owns StText.
				int ihvoText; // always zero, heading and contents are both atomic
				int cpropPrevious; //always zero, nothing in this example repeats same property.
				IVwPropertyStorePtr qvps;
				pselProposed->PropInfo(false, 2, &hvoSection, &tagText, &ihvoText, &cpropPrevious, &qvps);
				if (tagText != ktagHeading)
					return S_OK;

				// OK, we're going to make a new section.

				// Get some information about where the IP is in which paragraph.
				ITsStringPtr qtssOldPara;
				int ich;
				ComBool fAssocPrev;
				int hvoPara;
				int tagParaContents; // always kflidStTxtPara_Contents.
				int ws;
				pselProposed->TextSelInfo(false, &qtssOldPara, &ich, &fAssocPrev, &hvoPara, &tagParaContents, &ws);
				int cchOldPara;
				qtssOldPara->get_Length(&cchOldPara);

				// And some information about where the paragraph is in the StText
				HVO hvoText;
				PropTag tagPara; // always kflidStText_Paragraphs
				int ihvoPara;
				pselProposed->PropInfo(false, 1, &hvoText, &tagPara, &ihvoPara, &cpropPrevious, &qvps);
				int chvoPara;
				qsda->get_VecSize(hvoText, tagPara, &chvoPara);

				// And finally where the Section is in the root.
				HVO hvoRoot;
				PropTag tagSection;
				int ihvoSection;
				pselProposed->PropInfo(false, 3, &hvoRoot, &tagSection, &ihvoSection, &cpropPrevious, &qvps);

				// Insert a section after the current one.
				// Note that this might destroy the selection.
				int hvoNewSection;
				qsda->MakeNewObject(kclidSection, hvoRoot, tagSection, ihvoSection + 1, &hvoNewSection);

				// Insert a new StText with one empty paragraph as its heading. Give it the heading style.
				// (In real life we might set its contents to an empty string in the right ws.)
				int hvoNewHeadingText;
				qsda->MakeNewObject(kclidStText, hvoNewSection, ktagHeading, -2, &hvoNewHeadingText);
				int hvoNewHeadingPara;
				qsda->MakeNewObject(kclidStTxtPara, hvoNewHeadingText, ktagStText_Paragraphs, 0, &hvoNewHeadingPara);
				ITsPropsBldrPtr qtpb;
				qttp->GetBldr(&qtpb);
				StrUni stuHeading("heading");
				qtpb->SetStrPropValue(ktptNamedStyle, stuHeading.Bstr());
				ITsTextProps qttpHeading;
				qtpb->GetProps(&qttpHeading);
				qsda->SetUnknown(hvoNewHeadingPara, kflidStPara_StyleRules, qttpHeading);

				// Insert a new StText as the contents.
				int hvoNewContentText;
				qsda->MakeNewObject(kclidStText, hvoNewSection, ktagContents, -2, &hvoNewContentText);

				int hvoLastNewPara = 0;
				int cchLastNewPara = 0;

				// If there is trailing text in the paragraph we're inserting into, make it a new
				// paragraph in the new contents, and truncate the old paragraph.
				int chvoNewContents = 0;
				if (ich < cchOldPara)
				{
					ITsStrBldrPtr qtsb; // could optimize and skip this if ich==0.
					qtssOldPara->GetBldr(&qtsb);
					qtsb->ReplaceTsString(0, ich, NULL);
					ITsStringPtr qtssNew;
					qtsb->GetString(&qtssNew);
					int hvoNewPara;
					qsda->MakeNewObject(kclidStTxtPara, hvoNewContentText, ktagStText_Paragraphs, 0, &hvoNewPara);
					qsda->SetString(hvoNewPara, qtssNew);
					chvoNewContents++;
					// Make its paragraph style the same as the old para.
					qsda->SetUnknown(hvoNewPara, kflidStPara_StyleRules, qttp);
					// Truncate the old paragraph.
					qtssOldPara->GetBldr(&qtsb);
					qtsb->ReplaceTsString(ich, cch, NULL);
					ITsStringPtr qtssTrunc;
					qtsb->GetString(&qtssTrunc);
					qsda->SetString(hvoPara, qtssTrunc);
					hvoLastNewPara = hvoNewPara;
					cchLastNewPara = cch - ich;
				}

				// If there are more paragraphs in the old section, move them to the new one.
				if (ihvoPara < chvoPara - 1)
				{
					qsda->MoveOwnSeq(hvoText, kflidStText_Paragraphs, ihvoPara + 1, chvoPara - 1,
						hvoNewContentText, kflidStText_Paragraphs, chvoNewContents);
					chvoNewContents += chvoPara - ihvoPara - 1;
					hvoLastNewPara = qsda->get_VecItem(hvoNewContentText, kflidStText_Paragraphs, chvoNewContents - 1);
					ITsStringPtr qtssLastNewPara;
					qsda->get_StringProp(hvoLastNewPara, kflidStTxtPara_Contents);
					qtssLastNewPara->get_Length(&cchLastNewPara);
				}
				// If there are not yet any paragraphs in the new section, make one.
				if (chvoNewContents == 0)
				{
					int hvoNewPara;
					qsda->MakeNewObject(kclidStTxtPara, hvoNewContentText, ktagStText_Paragraphs, 0, &hvoNewPara);
					// In real life we would set its contents to an empty string in the right ws.
					// Issue: if there is more to paste, we'd like not to leave an empty paragraph; but we need it
					// for pselContinue to point to in that case, and so the StText isn't empty if there is no more.
					// Also we need it to hold the 'tail end' of the selection in case there is an incomplete
					// paragraph at the end. Howe can we get rid of it if it isn't needed?
					hvoLastNewPara = hvoNewPara; // leave cch 0.
				}

				// make the return selections.
				// First set up for actual, in heading
				VwSelLevInfo rgvsli[3];
				for (int i = 0; i < 3; i++)
					rgvsli[i].cpropPrevious = 0;
				rgvsli[0].tag = kflidStText_Paragraphs;
				rgvsli[0].ihvo = 0; // first para in new heading StText
				rgvsli[1].tag = ktagHeading;
				rgvsli[1].ihvo = 0; // atomic
				rgvsli[2].tag = ktagSections;
				rgvsli[2].ihvo = ihvoSection + 1; // index of new section.
				m_qrootb->MakeTextSelection(0, // first and only root
					3, rgvsli, // 3 VwSellevInfo objects specify higher level structure
					kflidStTxtPara_Contents, // text property to select in
					0, // no previous occurrences of the property
					0, 0, // IP at start
					0, // WS not meaningful for non-multilingual prop
					true, // direction of IP arbitrary in empty para
					-1, // end is not in a different paragraph
					NULL, // no special properties to apply to subsequent typing
					false, // don't install it as current selection
					ppselActual);
				// Now for continue, at start of contents.
				rgvsli[1].tag = ktagContents;
				m_qrootb->MakeTextSelection(0, // first and only root
					3, rgvsli, // 3 VwSellevInfo objects specify higher level structure
					kflidStTxtPara_Contents, // text property to select in
					0, // no previous occurrences of the property
					0, 0, // IP at start
					0, // WS not meaningful for non-multilingual prop
					false, // this para may not be empty, associate with first character
					-1, // end is not in a different paragraph
					NULL, // no special properties to apply to subsequent typing
					false, // don't install it as current selection
					ppselContinue);
				return S_OK;
			case 3:
				*pidpr = kidprRollback;
				return S_OK;
			}
#endif
			unitpp::assert_true("Bad case", false);
			return E_FAIL;
		}
		STDMETHOD(OnInsertDiffPara)(IVwRootBox * prootb, ITsTextProps * pttpDest,
			ITsTextProps * pttpSrc, ITsString * ptssSrc,  ITsString * ptssTrailing,
			VwInsertDiffParaResponse * pidpr)
		{
			return OnInsertDiffParas(prootb, pttpDest, 1, &pttpSrc, &ptssSrc, ptssTrailing, pidpr);
		}
	};
	DEFINE_COM_PTR(InsertDiffRootSite);

	class TestInsertDiffParas : public unitpp::suite
	{
	public:
		VwTextSelectionPtr m_qzvwsel;
		IVwCacheDaPtr m_qcda;
		ISilDataAccessPtr m_qsda;
		ITsStrFactoryPtr m_qtsf;
		IVwViewConstructorPtr m_qvc;
		VwRootBoxPtr m_qrootb;
		IVwGraphicsWin32Ptr m_qvg32;
		HDC m_hdc;
		InsertDiffRootSitePtr m_qidrs;
		Rect m_rcSrc;
		HVO m_hvoRoot;

		TestInsertDiffParas();

		void VerifyStringProps(int index, OLECHAR * psz, ITsTextProps * pttp)
		{
			unitpp::assert_eq("Didn't get expected props", pttp, m_qidrs->m_props[index]);
			SmartBstr sbstrString;
			m_qidrs->m_strings[index]->get_Text(&sbstrString);
			unitpp::assert_true("Didn't get expected string", wcscmp(psz, sbstrString.Chars()) == 0);
		}

		void VerifyParaStyle(int ipara, ITsTextProps * pttp)
		{
			HVO hvoPara;
			m_qsda->get_VecItem(m_hvoRoot, kflidStText_Paragraphs, ipara, &hvoPara);
			ITsTextPropsPtr qttp;
			IUnknownPtr qunkTtp;
			m_qsda->get_UnknownProp(hvoPara, kflidStPara_StyleRules, &qunkTtp);
			if (qunkTtp)
				CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttp));
			unitpp::assert_eq("Para props not as expected", pttp, qttp.Ptr());
		}

		void VerifyParaContents(int ipara, const OLECHAR * pch)
		{
			VwBox * pbox = m_qrootb->FirstRealBox();
			for (int i = 0; i < ipara; i++)
			{
				pbox = pbox->NextRealBox();
			}
			VwParagraphBox * pvpbox = dynamic_cast<VwParagraphBox *>(pbox);
			int cch;
			pvpbox->Source()->get_Length(&cch);
			OLECHAR buf[1000]; // enough for any tests in this module
			Assert(cch < 1000);
			pvpbox->Source()->Fetch(0, cch, buf);
			buf[cch] = 0;
			StrAnsi staMsg;
			staMsg.Format("para contents is %S, not %S", buf, pch);
			unitpp::assert_true(staMsg.Chars(), wcscmp(pch, buf) == 0);
		}


		void VerifyPropContents(PropTag flid, const OLECHAR * pch)
		{
			ITsStringPtr qtss;
			m_qsda->get_StringProp(khvoBook, flid, &qtss);
			SmartBstr sbstr;
			qtss->get_Text(&sbstr);
			StrAnsi staMsg;
			staMsg.Format("prop %d contents is %S, not %S", flid, sbstr.Chars(), pch);
			unitpp::assert_true(staMsg.Chars(), wcscmp(pch, sbstr.Chars()) == 0);
		}

		void MakeStyleProps(OLECHAR * pszStyle, ITsTextProps ** ppttp, int ws = 0)
		{
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			StrUni stuStyle(pszStyle);
			if (stuStyle.Length() > 0)
				qtpb->SetStrPropValue(ktptNamedStyle, stuStyle.Bstr());
			if (ws != 0)
				qtpb->SetIntPropValues(ktptWs, ktpvDefault, ws);
			qtpb->GetTextProps(ppttp);
		}

		void testInsertDiffParasEmptySecondPara()
		{
			ITsStringPtr qtss;
			// Create test data in a temporary cache.
			// First make some generic objects.
			// Now make two strings, the contents of paragraphs 1 and 3 and
			// make the seconde para empty.
			StrUni stuPara1(L"This is the first test paragraph");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoOrigPara1, kflidStTxtPara_Contents, qtss);
			ITsTextPropsPtr qttpHeading;
			MakeStyleProps(OleStringLiteral(L"Heading"), &qttpHeading);
			m_qcda->CacheUnknown(khvoOrigPara1, kflidStPara_StyleRules, qttpHeading);

			// Now make them the paragraphs of an StText.
			HVO rghvo[1] = {khvoOrigPara1};
			m_hvoRoot = 101;
			m_qcda->CacheVecProp(m_hvoRoot, kflidStText_Paragraphs, rghvo, 1);

			m_qvc.Attach(NewObj DummyParaVc());
			m_qrootb->SetRootObject(m_hvoRoot, m_qvc, kfragStText, NULL);
			m_qrootb->Layout(m_qvg32, 300);

			// Make a selection in the 3 paragraphs
			IVwSelectionPtr qselTemp;
			m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp);

			// Insert some simple text.
			StrUni stuInsert1(L"end para1\r\n\r\nstartpara2");
			ITsStringPtr qtssInsert1;
			m_qtsf->MakeString(stuInsert1.Bstr(), g_wsEng, &qtssInsert1);
			HRESULT hr;
			hr = qselTemp->ReplaceWithTsString(qtssInsert1);
			unitpp::assert_eq("First test returne S_OK", S_OK, hr);
			unitpp::assert_true("Simple insert calls InsertDiffParas",
				m_qidrs->m_cactInsertDiffPara == 1);
			VerifyParaContents(0, OleStringLiteral(L"end para1"));
		}

		void testInsertDiffParas()
		{
			ITsStringPtr qtss;
			// Create test data in a temporary cache.
			// First make some generic objects.
			// Now make two strings, the contents of paragraphs 1 and 2.
			StrUni stuPara1(L"This is the first test paragraph");
			m_qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoOrigPara1, kflidStTxtPara_Contents, qtss);
			StrUni stuPara2(L"This is the second test paragraph");
			m_qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			m_qcda->CacheStringProp(khvoOrigPara2, kflidStTxtPara_Contents, qtss);
			ITsTextPropsPtr qttpNormal;
			ITsTextPropsPtr qttpHeading;
			MakeStyleProps(OleStringLiteral(L"Normal"), &qttpNormal);
			MakeStyleProps(OleStringLiteral(L"Heading"), &qttpHeading);
			m_qcda->CacheUnknown(khvoOrigPara1, kflidStPara_StyleRules, qttpNormal);
			m_qcda->CacheUnknown(khvoOrigPara2, kflidStPara_StyleRules, qttpNormal);

			// Now make them the paragraphs of an StText.
			HVO rghvo[2] = {khvoOrigPara1, khvoOrigPara2};
			m_hvoRoot = 101;
			m_qcda->CacheVecProp(m_hvoRoot, kflidStText_Paragraphs, rghvo, 2);

			m_qvc.Attach(NewObj DummyParaVc());
			m_qrootb->SetRootObject(m_hvoRoot, m_qvc, kfragStText, NULL);

			IVwSelectionPtr qselTemp;
			// Put insertion point at the beginning of the view
			m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp);
			// Insert some simple text.
			StrUni stuInsert1(L"Insert start");
			ITsStringPtr qtssInsert1;
			m_qtsf->MakeString(stuInsert1.Bstr(), g_wsEng, &qtssInsert1);
			HRESULT hr;
			hr = qselTemp->ReplaceWithTsString(qtssInsert1);
			unitpp::assert_eq("First test returne S_OK", S_OK, hr);
			unitpp::assert_true("Simple insert does not acll InsertDiffParas",
				m_qidrs->m_cactInsertDiffPara == 0);
			VerifyParaContents(0, OleStringLiteral(L"Insert startThis is the first test paragraph"));
			// Insert a heading paragraph, but with the root site not interfering.
			ITsStrBldrPtr qtsb;
			qtsb.CreateInstance(CLSID_TsStrBldr);
			StrUni stuHeading(L"Heading Para");
			StrUni stuCrLf(L"\r\n");
			ITsTextPropsPtr qttpEng;
			MakeStyleProps(OleStringLiteral(L""), &qttpEng, g_wsEng);
			qtsb->Replace(0, 0, stuCrLf.Bstr(), qttpHeading);
			qtsb->Replace(0, 0, stuHeading.Bstr(), qttpEng);
			ITsStringPtr qtssHeading;
			qtsb->GetString(&qtssHeading);
			m_qrootb->get_Selection(&qselTemp);
			m_qidrs->SimulateBeginUnitOfWork();
			hr = qselTemp->ReplaceWithTsString(qtssHeading);
			m_qidrs->SimulateEndUnitOfWork();
			unitpp::assert_eq("Second test returne S_OK", S_OK, hr);
			VerifyParaContents(0, OleStringLiteral(L"Insert startHeading Para"));
			VerifyParaContents(1, OleStringLiteral(L"This is the first test paragraph"));
			VerifyParaStyle(0, qttpHeading);
			VerifyParaStyle(1, qttpNormal);
			unitpp::assert_true("InsertDiffParas called",
				m_qidrs->m_cactInsertDiffPara == 1);
			unitpp::assert_eq("expected one string", 1, m_qidrs->m_strings.Size());
			VerifyStringProps(0, OleStringLiteral(L"Heading Para"), qttpHeading);
			int len;
			m_qidrs->m_qtssTrail->get_Length(&len);
			unitpp::assert_eq("no trailing text", 0, len);

			// That should have left the IP at the start of the second paragraph.
			// Insert again, but with InsertDiffPara failing. This should change nothing,
			// except for incrementing the count.
			m_qidrs->m_case = 2;
			m_qrootb->get_Selection(&qselTemp);
			m_qidrs->SimulateBeginUnitOfWork();
			hr = qselTemp->ReplaceWithTsString(qtssHeading);
			m_qidrs->SimulateEndUnitOfWork();
			unitpp::assert_eq("Third test returned E_FAIL", E_FAIL, hr);
			VerifyParaContents(0, OleStringLiteral(L"Insert startHeading Para"));
			VerifyParaContents(1, OleStringLiteral(L"This is the first test paragraph"));
			VerifyParaStyle(0, qttpHeading);
			VerifyParaStyle(1, qttpNormal);
			unitpp::assert_true("InsertDiffParas called 2nd time",
				m_qidrs->m_cactInsertDiffPara == 2);
			unitpp::assert_eq("expected one string", 1, m_qidrs->m_strings.Size());
			VerifyStringProps(0, OleStringLiteral(L"Heading Para"), qttpHeading);

			// Now try again in the 'done' mode, and with a more complex string.
			StrUni stuInsert2(L"Second inserted para");
			qtsb->Replace(0, 0, stuCrLf.Bstr(), qttpNormal);
			qtsb->Replace(0, 0, stuInsert2.Bstr(), qttpEng);
			StrUni stuTrailing(L"Trailing text");
			qtsb->get_Length(&len);
			m_qidrs->SimulateBeginUnitOfWork();
			hr = qtsb->Replace(len, len, stuTrailing.Bstr(), qttpEng);
			m_qidrs->SimulateEndUnitOfWork();
			unitpp::assert_eq("Fourth test returned S_OK", S_OK, hr);
			ITsStringPtr qtssTwoParas;
			qtsb->GetString(&qtssTwoParas);

			m_qidrs->m_case = 1;
			m_qidrs->SimulateBeginUnitOfWork();
			CheckHr(qselTemp->ReplaceWithTsString(qtssTwoParas));
			m_qidrs->SimulateEndUnitOfWork();
			unitpp::assert_eq("expected two strings", 2, m_qidrs->m_strings.Size());
			VerifyStringProps(0, OleStringLiteral(L"Second inserted para"), qttpNormal);
			VerifyStringProps(1, OleStringLiteral(L"Heading Para"), qttpHeading);
			SmartBstr sbstr;
			m_qidrs->m_qtssTrail->get_Text(&sbstr);
			unitpp::assert_true("wrong trailing text",
				wcscmp(sbstr.Chars(), stuTrailing.Chars()) == 0);
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
			VwRootBox::CreateCom(NULL, CLSID_VwRootBox, (void **)&m_qrootb);
			m_hdc = 0;
			m_qvg32.CreateInstance(CLSID_VwGraphicsWin32);
			m_hdc = GetTestDC();
			m_qvg32->Initialize(m_hdc);
			m_qrootb->putref_DataAccess(m_qsda);
			m_qidrs.Attach(NewObj InsertDiffRootSite());
			m_rcSrc = Rect(0, 0, 96, 96);
			m_qidrs->SetRects(m_rcSrc, m_rcSrc);
			m_qidrs->SetGraphics(m_qvg32);
			m_qrootb->SetSite(m_qidrs);
			m_qidrs->SetRootBox(m_qrootb);
		}
		virtual void Teardown()
		{
			m_qzvwsel.Clear();
			m_qcda.Clear();
			m_qsda.Clear();
			m_qtsf.Clear();
			m_qvc.Clear();
			m_qvg32->ReleaseDC();
			ReleaseTestDC(m_hdc);
			m_qrootb->Close();
			m_qrootb.Clear();
			m_qvg32.Clear();
			m_qidrs.Clear();
			CloseTestWritingSystemFactory();
		}
	};
}

#endif /*TESTInsertDiff_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
