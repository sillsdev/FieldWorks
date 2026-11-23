/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2026 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestVwWrapGroup.h
Responsibility:
Last reviewed:

	Unit tests for the VwWrapGroupBox class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestVwWrapGroup_H_INCLUDED
#define TestVwWrapGroup_H_INCLUDED

#pragma once

#include "testViews.h"

#define kfragWrapFlat 1
#define kfragWrapNested 2
#define kfragWrapLongItem 3
#define kfragWrapEmpty 4
#define kfragWrapText 5
#define kfragWrapNestedText 6

namespace TestViews
{
	// Displays wrap groups whose items are paragraphs holding either one fixed-size rectangle
	// each, so every item has the same known width, or a short word.
	class WrapGroupDummyVc : public DummyBaseVc
	{
	public:
		WrapGroupDummyVc() : m_fRtl(false)
		{
		}

		bool m_fRtl;
		ITsStringPtr m_qtssSep;
		ITsStringPtr m_qtssLong;

		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			if (m_fRtl)
				pvwenv->put_IntProperty(ktptRightToLeft, ktpvEnum, 1);
			switch(frag)
			{
			case kfragWrapFlat:
				pvwenv->OpenWrapGroup(m_qtssSep);
				AddItems(pvwenv, 3);
				pvwenv->CloseWrapGroup();
				break;
			case kfragWrapNested:
				pvwenv->OpenWrapGroup(NULL);
				pvwenv->OpenWrapGroup(NULL);
				AddItems(pvwenv, 2);
				pvwenv->CloseWrapGroup();
				AddItems(pvwenv, 1);
				pvwenv->OpenWrapGroup(NULL);
				AddItems(pvwenv, 3);
				pvwenv->CloseWrapGroup();
				pvwenv->CloseWrapGroup();
				break;
			case kfragWrapLongItem:
				pvwenv->OpenWrapGroup(NULL);
				AddItems(pvwenv, 1);
				pvwenv->OpenParagraph();
				pvwenv->AddString(m_qtssLong);
				pvwenv->CloseParagraph();
				pvwenv->CloseWrapGroup();
				break;
			case kfragWrapEmpty:
				pvwenv->OpenWrapGroup(m_qtssSep);
				pvwenv->CloseWrapGroup();
				break;
			case kfragWrapText:
				pvwenv->OpenWrapGroup(m_qtssSep);
				AddWords(pvwenv, 0, 3);
				pvwenv->CloseWrapGroup();
				break;
			case kfragWrapNestedText:
				pvwenv->OpenWrapGroup(NULL);
				pvwenv->OpenWrapGroup(m_qtssSep);
				AddWords(pvwenv, 0, 2);
				pvwenv->CloseWrapGroup();
				AddItems(pvwenv, 1);
				pvwenv->OpenWrapGroup(m_qtssSep);
				AddWords(pvwenv, 2, 1);
				pvwenv->CloseWrapGroup();
				pvwenv->CloseWrapGroup();
				break;
			}
			return S_OK;
		}

		ITsStringPtr m_rgqtssWord[3];

	private:
		// Add words iwordFirst onwards, one paragraph each.
		void AddWords(IVwEnv * pvwenv, int iwordFirst, int cword)
		{
			for (int iword = iwordFirst; iword < iwordFirst + cword; iword++)
			{
				pvwenv->OpenParagraph();
				pvwenv->AddString(m_rgqtssWord[iword]);
				pvwenv->CloseParagraph();
			}
		}

		void AddItems(IVwEnv * pvwenv, int citem)
		{
			for (int iitem = 0; iitem < citem; iitem++)
			{
				pvwenv->OpenParagraph();
				pvwenv->AddSimpleRect(10, MulDiv(20, kdzmpInch, 96), MulDiv(15, kdzmpInch, 96), 0);
				pvwenv->CloseParagraph();
			}
		}
	};

	class TestVwWrapGroup : public unitpp::suite
	{
		IVwRootBoxPtr m_qrootb;
		VwRootBox * m_prootb;
		IVwGraphicsWin32Ptr m_qvg32;
		HDC m_hdc;
		ITsStrFactoryPtr m_qtsf;

		// Build a view of the given fragment, optionally right-to-left and with a separator.
		void MakeView(int frag, bool fRtl = false, const OLECHAR * pszSeparator = NULL)
		{
			m_qtsf.CreateInstance(CLSID_TsStrFactory);
			IVwCacheDaPtr qcda;
			qcda.CreateInstance(CLSID_VwCacheDa);
			qcda->putref_TsStrFactory(m_qtsf);
			ISilDataAccessPtr qsda;
			qcda->QueryInterface(IID_ISilDataAccess, (void **)&qsda);
			qsda->putref_WritingSystemFactory(g_qwsf);

			IRenderEngineFactoryPtr qref;
			qref.Attach(NewObj MockRenderEngineFactory);

			WrapGroupDummyVc * pvc = NewObj WrapGroupDummyVc();
			IVwViewConstructorPtr qvc;
			qvc.Attach(pvc);
			pvc->m_fRtl = fRtl;
			if (pszSeparator)
			{
				m_qtsf->MakeStringRgch(pszSeparator, (int)wcslen(pszSeparator), g_wsEng,
					&pvc->m_qtssSep);
			}
			const OLECHAR * rgpszWord[3] = {L"one", L"two", L"three"};
			for (int iword = 0; iword < 3; iword++)
			{
				m_qtsf->MakeStringRgch(rgpszWord[iword], (int)wcslen(rgpszWord[iword]), g_wsEng,
					&pvc->m_rgqtssWord[iword]);
			}
			StrUni stuLong(L"one two three four five six seven eight nine ten eleven twelve "
				L"thirteen fourteen fifteen sixteen seventeen eighteen nineteen twenty");
			m_qtsf->MakeString(stuLong.Bstr(), g_wsEng, &pvc->m_qtssLong);

			m_qvg32.CreateInstance(CLSID_VwGraphicsWin32);
			m_hdc = GetTestDC();
			m_qvg32->Initialize(m_hdc);

			m_qrootb->putref_DataAccess(qsda);
			m_qrootb->putref_RenderEngineFactory(qref);
			m_qrootb->putref_TsStrFactory(m_qtsf);
			m_qrootb->SetRootObject(101, qvc, frag, NULL);
			DummyRootSitePtr qdrs;
			qdrs.Attach(NewObj DummyRootSite());
			Rect rcSrc(0, 0, 96, 96);
			qdrs->SetRects(rcSrc, rcSrc);
			qdrs->SetGraphics(m_qvg32);
			m_qrootb->SetSite(qdrs);
		}

		void Layout(int dxpWidth)
		{
			CheckHr(m_qrootb->Layout(m_qvg32, dxpWidth));
		}

		VwWrapGroupBox * Group()
		{
			VwWrapGroupBox * pwgbox = dynamic_cast<VwWrapGroupBox *>(m_prootb->FirstBox());
			unitpp::assert_true("the view should start with a wrap group", pwgbox != NULL);
			return pwgbox;
		}

		static VwBox * ChildAt(VwGroupBox * pgbox, int ichild)
		{
			VwBox * pbox = pgbox->FirstBox();
			for (int i = 0; i < ichild && pbox; i++)
				pbox = pbox->NextOrLazy();
			unitpp::assert_true("child should exist", pbox != NULL);
			return pbox;
		}

		static VwWrapGroupBox * GroupAt(VwGroupBox * pgbox, int ichild)
		{
			VwWrapGroupBox * pwgbox = dynamic_cast<VwWrapGroupBox *>(ChildAt(pgbox, ichild));
			unitpp::assert_true("child should be a wrap group", pwgbox != NULL);
			return pwgbox;
		}

		// Top of pbox relative to the root box.
		static int AbsoluteTop(VwBox * pbox)
		{
			int ys = pbox->Top();
			for (VwGroupBox * pgbox = pbox->Container(); pgbox; pgbox = pgbox->Container())
				ys += pgbox->Top();
			return ys;
		}

		// The text Copy would put on the clipboard for a selection of the whole view, with "; "
		// for anything that is not text, without carriage returns or a final line break.
		StrUni CopiedText()
		{
			IVwSelectionPtr qselStart;
			IVwSelectionPtr qselEnd;
			IVwSelectionPtr qsel;
			CheckHr(m_qrootb->MakeSimpleSel(true, false, false, false, &qselStart));
			CheckHr(m_qrootb->MakeSimpleSel(false, false, false, false, &qselEnd));
			CheckHr(m_qrootb->MakeRangeSelection(qselStart, qselEnd, false, &qsel));
			ITsStringPtr qtss;
			StrUni stuNonText(L"; ");
			CheckHr(qsel->GetSelectionString(&qtss, stuNonText.Bstr()));
			SmartBstr sbstr;
			CheckHr(qtss->get_Text(&sbstr));
			StrUni stuResult;
			for (const OLECHAR * pch = sbstr.Chars(); pch && *pch; pch++)
			{
				if (*pch != L'\r')
					stuResult.Append(pch, 1);
			}
			while (stuResult.Length() && stuResult[stuResult.Length() - 1] == L'\n')
				stuResult.Replace(stuResult.Length() - 1, stuResult.Length(), L"");
			return stuResult;
		}

		// Width of the gap between the first two children of a group on one line.
		static int GapAfterFirstChild(VwGroupBox * pgbox)
		{
			return ChildAt(pgbox, 1)->Left() - ChildAt(pgbox, 0)->Right();
		}

		static void AssertStacked(const char * pszWhat, VwGroupBox * pgbox, int cchild)
		{
			for (int ichild = 1; ichild < cchild; ichild++)
			{
				unitpp::assert_eq(pszWhat, ChildAt(pgbox, 0)->Left(), ChildAt(pgbox, ichild)->Left());
				unitpp::assert_true(pszWhat,
					ChildAt(pgbox, ichild)->Top() >= ChildAt(pgbox, ichild - 1)->Bottom());
			}
		}

	public:
		void testFlatGroupSwitchesAtExactFit()
		{
			MakeView(kfragWrapFlat);
			Layout(300);
			VwWrapGroupBox * pwgbox = Group();
			unitpp::assert_true("wide enough for one line", pwgbox->IsOneLine());
			int dxpItem = ChildAt(pwgbox, 0)->Width();
			unitpp::assert_true("items have width", dxpItem > 0);
			unitpp::assert_eq("one-line width", 3 * dxpItem, pwgbox->Width());
			for (int ichild = 0; ichild < 3; ichild++)
			{
				unitpp::assert_eq("left on one line", ichild * dxpItem,
					ChildAt(pwgbox, ichild)->Left());
				unitpp::assert_eq("top on one line", ChildAt(pwgbox, 0)->Top(),
					ChildAt(pwgbox, ichild)->Top());
			}

			Layout(3 * dxpItem);
			unitpp::assert_true("exactly wide enough", Group()->IsOneLine());

			Layout(3 * dxpItem - 1);
			pwgbox = Group();
			unitpp::assert_true("one pixel too narrow", !pwgbox->IsOneLine());
			AssertStacked("stacked", pwgbox, 3);
			unitpp::assert_eq("stacked width", dxpItem, pwgbox->Width());

			Layout(300);
			unitpp::assert_true("back to one line when widened", Group()->IsOneLine());
		}

		void testSeparatorsCountTowardFit()
		{
			MakeView(kfragWrapFlat, false, L", ");
			Layout(300);
			VwWrapGroupBox * pwgbox = Group();
			unitpp::assert_true("wide enough for one line", pwgbox->IsOneLine());
			int dxpItem = ChildAt(pwgbox, 0)->Width();
			int dxpSep = ChildAt(pwgbox, 1)->Left() - dxpItem;
			unitpp::assert_true("separator has width", dxpSep > 0);
			unitpp::assert_eq("separator count", 2, pwgbox->SeparatorPositions().Size());
			unitpp::assert_eq("first separator", dxpItem, pwgbox->SeparatorPositions()[0]);
			unitpp::assert_eq("second separator", 2 * dxpItem + dxpSep,
				pwgbox->SeparatorPositions()[1]);
			int dxpOneLine = 3 * dxpItem + 2 * dxpSep;
			unitpp::assert_eq("one-line width", dxpOneLine, pwgbox->Width());

			Layout(dxpOneLine - 1);
			pwgbox = Group();
			unitpp::assert_true("too narrow once separators count", !pwgbox->IsOneLine());
			unitpp::assert_eq("no separators when stacked", 0, pwgbox->SeparatorPositions().Size());
			AssertStacked("stacked", pwgbox, 3);
		}

		void testNestedGroupsFollowOuterGroup()
		{
			MakeView(kfragWrapNested);
			Layout(1000);
			VwWrapGroupBox * pwgbox = Group();
			VwWrapGroupBox * pwgboxFirst = GroupAt(pwgbox, 0);
			VwWrapGroupBox * pwgboxLast = GroupAt(pwgbox, 2);
			unitpp::assert_true("outer on one line", pwgbox->IsOneLine());
			unitpp::assert_true("first inner on one line", pwgboxFirst->IsOneLine());
			unitpp::assert_true("last inner on one line", pwgboxLast->IsOneLine());
			int ysTop = AbsoluteTop(ChildAt(pwgboxFirst, 0));
			unitpp::assert_eq("middle item shares the line", ysTop,
				AbsoluteTop(ChildAt(pwgbox, 1)));
			unitpp::assert_eq("last item shares the line", ysTop,
				AbsoluteTop(ChildAt(pwgboxLast, 2)));
			unitpp::assert_eq("middle item follows first group", pwgboxFirst->Right(),
				ChildAt(pwgbox, 1)->Left());

			Layout(pwgbox->Width() - 1);
			pwgbox = Group();
			pwgboxFirst = GroupAt(pwgbox, 0);
			pwgboxLast = GroupAt(pwgbox, 2);
			unitpp::assert_true("outer stacked", !pwgbox->IsOneLine());
			unitpp::assert_true("first inner stacked with outer", !pwgboxFirst->IsOneLine());
			unitpp::assert_true("last inner stacked with outer", !pwgboxLast->IsOneLine());
			AssertStacked("outer", pwgbox, 3);
			AssertStacked("first inner", pwgboxFirst, 2);
			AssertStacked("last inner", pwgboxLast, 3);
		}

		void testItemNeedingTwoLinesForcesStack()
		{
			MakeView(kfragWrapLongItem);
			Layout(5000);
			unitpp::assert_true("long item fits on one line when wide", Group()->IsOneLine());

			Layout(100);
			VwWrapGroupBox * pwgbox = Group();
			unitpp::assert_true("long item wraps", ChildAt(pwgbox, 1)->CLines() > 1);
			unitpp::assert_true("a wrapped item stacks the group", !pwgbox->IsOneLine());
			AssertStacked("stacked", pwgbox, 2);
		}

		void testRightToLeftReversesOneLineOrder()
		{
			MakeView(kfragWrapFlat, true);
			Layout(300);
			VwWrapGroupBox * pwgbox = Group();
			unitpp::assert_true("wide enough for one line", pwgbox->IsOneLine());
			int dxpItem = ChildAt(pwgbox, 0)->Width();
			unitpp::assert_eq("first item rightmost", 2 * dxpItem, ChildAt(pwgbox, 0)->Left());
			unitpp::assert_eq("second item in the middle", dxpItem, ChildAt(pwgbox, 1)->Left());
			unitpp::assert_eq("last item leftmost", 0, ChildAt(pwgbox, 2)->Left());
		}

		void testLeftAndRightFollowTheItemsOnScreen()
		{
			MakeView(kfragWrapFlat);
			Layout(300);
			VwWrapGroupBox * pwgbox = Group();
			unitpp::assert_true("left to right, one line", pwgbox->IsOneLine());
			VwBox * pboxMiddle = ChildAt(pwgbox, 1);
			unitpp::assert_true("left to right, left of middle",
				pwgbox->NavLeft(pboxMiddle) == ChildAt(pwgbox, 0));
			unitpp::assert_true("left to right, right of middle",
				pwgbox->NavRight(pboxMiddle) == ChildAt(pwgbox, 2));
			unitpp::assert_true("left to right, nothing left of leftmost",
				pwgbox->NavLeft(ChildAt(pwgbox, 0)) == NULL);

			Layout(ChildAt(pwgbox, 0)->Width());
			pwgbox = Group();
			unitpp::assert_true("stacked", !pwgbox->IsOneLine());
			unitpp::assert_true("stacked, no left", pwgbox->NavLeft(ChildAt(pwgbox, 1)) == NULL);
			unitpp::assert_true("stacked, no right", pwgbox->NavRight(ChildAt(pwgbox, 1)) == NULL);
		}

		void testLeftAndRightFollowTheItemsOnScreenRightToLeft()
		{
			MakeView(kfragWrapFlat, true);
			Layout(300);
			VwWrapGroupBox * pwgbox = Group();
			unitpp::assert_true("right to left, one line", pwgbox->IsOneLine());
			VwBox * pboxMiddle = ChildAt(pwgbox, 1);
			unitpp::assert_true("right to left, left of middle",
				pwgbox->NavLeft(pboxMiddle) == ChildAt(pwgbox, 2));
			unitpp::assert_true("right to left, right of middle",
				pwgbox->NavRight(pboxMiddle) == ChildAt(pwgbox, 0));
			unitpp::assert_true("right to left, nothing left of leftmost",
				pwgbox->NavLeft(ChildAt(pwgbox, 2)) == NULL);
		}

		void testEmptyGroup()
		{
			MakeView(kfragWrapEmpty, false, L", ");
			Layout(300);
			VwWrapGroupBox * pwgbox = Group();
			unitpp::assert_true("empty group counts as one line", pwgbox->IsOneLine());
			unitpp::assert_eq("empty group width", 0, pwgbox->Width());
			unitpp::assert_eq("empty group has no separators", 0,
				pwgbox->SeparatorPositions().Size());
		}

		void testSpaceAfterSeparatorKeepsItsWidth()
		{
			MakeView(kfragWrapText, false, L",");
			Layout(1000);
			unitpp::assert_true("comma alone fits", Group()->IsOneLine());
			int dxpComma = GapAfterFirstChild(Group());

			Teardown();
			Setup();
			MakeView(kfragWrapText, false, L", ");
			Layout(1000);
			unitpp::assert_true("comma and space fit", Group()->IsOneLine());
			unitpp::assert_true("the space after the comma adds width",
				GapAfterFirstChild(Group()) > dxpComma);
		}

		void testCopyOnOneLineUsesTheSeparator()
		{
			MakeView(kfragWrapText, false, L", ");
			Layout(1000);
			unitpp::assert_true("wide enough for one line", Group()->IsOneLine());
			unitpp::assert_true("copied text",
				CopiedText() == StrUni(L"one, two, three"));
		}

		void testCopyWhenStackedPutsEachItemOnItsOwnLine()
		{
			MakeView(kfragWrapText, false, L", ");
			Layout(1000);
			Layout(Group()->Width() - 1);
			unitpp::assert_true("too narrow for one line", !Group()->IsOneLine());
			unitpp::assert_true("copied text",
				CopiedText() == StrUni(L"one\ntwo\nthree"));
		}

		void testCopyAcrossNestedGroupsAddsNoSeparatorBetweenThem()
		{
			MakeView(kfragWrapNestedText, false, L", ");
			Layout(1000);
			unitpp::assert_true("wide enough for one line", Group()->IsOneLine());
			unitpp::assert_true("copied text",
				CopiedText() == StrUni(L"one, two; three"));
		}

		TestVwWrapGroup();

		virtual void Setup()
		{
			CreateTestWritingSystemFactory();
			VwRootBox::CreateCom(NULL, IID_IVwRootBox, (void **)&m_qrootb);
			m_prootb = dynamic_cast<VwRootBox *>(m_qrootb.Ptr());
			m_hdc = 0;
		}
		virtual void Teardown()
		{
			m_qrootb->Close();
			m_qrootb.Clear();
			if (m_qvg32)
				m_qvg32->ReleaseDC();
			m_qvg32.Clear();
			if (m_hdc != 0)
				ReleaseTestDC(m_hdc);
			m_hdc = 0;
			m_qtsf.Clear();
			CloseTestWritingSystemFactory();
		}
	};
}

#endif /*TestVwWrapGroup_H_INCLUDED*/
