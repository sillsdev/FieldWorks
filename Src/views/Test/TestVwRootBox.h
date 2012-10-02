/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestVwRootBox.h
Responsibility:
Last reviewed:

	Unit tests for the VwRootBox class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTVWROOTBOX_H_INCLUDED
#define TESTVWROOTBOX_H_INCLUDED

#pragma once

#include "testViews.h"
#include "testVwSelection.h"

DEFINE_COM_PTR(IAccessible);

static const GUID s_rgguid[] = {
	// {E50D2B4B-8AE8-4537-BEA4-242693228898}
	{ 0xe50d2b4b, 0x8ae8, 0x4537, { 0xbe, 0xa4, 0x24, 0x26, 0x93, 0x22, 0x88, 0x98 } },
	// {08D3C8A0-5E08-4a5e-9D9A-6E8DCE4B5692}
	{ 0x8d3c8a0, 0x5e08, 0x4a5e, { 0x9d, 0x9a, 0x6e, 0x8d, 0xce, 0x4b, 0x56, 0x92 } },
	// {EBC8DD5C-87C4-4001-81A6-E0CE2E556866}
	{ 0xebc8dd5c, 0x87c4, 0x4001, { 0x81, 0xa6, 0xe0, 0xce, 0x2e, 0x55, 0x68, 0x66 } },
	// {71A8AC2B-0A6F-40f9-A7F4-89E802C840D4}
	{ 0x71a8ac2b, 0xa6f, 0x40f9, { 0xa7, 0xf4, 0x89, 0xe8, 0x2, 0xc8, 0x40, 0xd4 } },
	// {70B2F8FF-B55D-44ec-AB00-6F020368976F}
	{ 0x70b2f8ff, 0xb55d, 0x44ec, { 0xab, 0x0, 0x6f, 0x2, 0x3, 0x68, 0x97, 0x6f } },
	// {113C3588-3955-4452-AFA8-B1B4D8ED7C06}
	{ 0x113c3588, 0x3955, 0x4452, { 0xaf, 0xa8, 0xb1, 0xb4, 0xd8, 0xed, 0x7c, 0x6 } },
	// {0D641F8C-DBF9-42bb-BC03-1F51D2F230CD}
	{ 0xd641f8c, 0xdbf9, 0x42bb, { 0xbc, 0x3, 0x1f, 0x51, 0xd2, 0xf2, 0x30, 0xcd } },
};
static const int s_cguid = sizeof(s_rgguid)/sizeof(GUID);

namespace TestViews
{
	class MockNotifyObjCharDeletion : public IVwNotifyObjCharDeletion
	{
	public:
		int m_rgcNotified[s_cguid];
		bool m_fGotUnexpected;

		MockNotifyObjCharDeletion()
		{
			Reset();
		}
		void Reset()
		{
			for (int i = 0; i < s_cguid; ++i)
				m_rgcNotified[i] = 0;
			m_fGotUnexpected = false;
		}

		STDMETHOD(QueryInterface)(REFIID riid, void ** ppv)
		{
			if (riid == IID_IUnknown)
				*ppv = static_cast<IUnknown *>(this);
			else if (riid == IID_IVwNotifyObjCharDeletion)
				*ppv = static_cast<IVwNotifyObjCharDeletion *>(this);
			else
				return E_NOINTERFACE;

			AddRef();
			return S_OK;
		}

		// Dummy implementations, we'll make this on-stack.
		STDMETHOD_(ULONG, AddRef)(void)
		{
			return 1;
		}
		STDMETHOD_(ULONG, Release)(void)
		{
			return 1;
		}

		STDMETHODIMP ObjDeleted(GUID * pguid)
		{
			for (int i = 0; i < s_cguid; ++i)
			{
				if (InlineIsEqualGUID(*pguid, s_rgguid[i]))
				{
					m_rgcNotified[i]++;
					return S_OK;
				}
			}

			m_fGotUnexpected = true;
			return S_OK;
		}

		void VerifyResults(int rgExpected[s_cguid])
		{
			for (int i = 0; i < s_cguid; ++i)
			{
				StrAnsi sta;
				sta.Format("notification [%d]", i);
				unitpp::assert_eq(sta.Chars(), rgExpected[i], m_rgcNotified[i]);
			}
			unitpp::assert_eq("no unexpected notifications", false, m_fGotUnexpected);
		}
	};

	class TestVwRootBox : public unitpp::suite
	{
		IVwRootBoxPtr m_qrootb;

		void testNullArgs()
		{
			unitpp::assert_true("Non-NULL m_qrootb after setup", m_qrootb.Ptr() != 0);
			HRESULT hr;
			VARIANT v = { 0 };
			RECT rc = { 0, 0, 0, 0 };
			try
			{
				CheckHr(hr = m_qrootb->SetSite(NULL));
				unitpp::assert_eq("SetSite(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("SetSite(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->putref_DataAccess(NULL));
				unitpp::assert_eq("putref_DataAccess(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("putref_DataAccess(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->get_DataAccess(NULL));
				unitpp::assert_eq("get_DataAccess(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_DataAccess(NULL) HRESULT", E_POINTER, thr.Result());
			}

			CheckHr(hr = m_qrootb->SetRootObjects(NULL, NULL, NULL, NULL, 0));
			unitpp::assert_eq("SetRootObjects(NULL, NULL, NULL, NULL, 0) HRESULT",
			S_OK, hr);

			try
			{
				CheckHr(hr = m_qrootb->SetRootObject(0, NULL, 0, NULL));
				unitpp::assert_eq("SetRootObject(0, NULL, 0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("SetRootObject(0, NULL, 0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->SetRootVariant(v, NULL, NULL, 0));
				unitpp::assert_eq("SetRootVariant(v, NULL, NULL, 0) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("SetRootVariant(v, NULL, NULL, 0) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->SetRootString(NULL, NULL, NULL, 0));
				unitpp::assert_eq("SetRootString(NULL, NULL, NULL, 0) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("SetRootString(NULL, NULL, NULL, 0) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->get_Overlay(NULL));
				unitpp::assert_eq("get_Overlay(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_Overlay(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->Serialize(NULL));
				unitpp::assert_eq("Serialize(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("Serialize(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->Deserialize(NULL));
				unitpp::assert_eq("Deserialize(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("Deserialize(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->WriteWpx(NULL));
				unitpp::assert_eq("WriteWpx(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("WriteWpx(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->get_Selection(NULL));
				unitpp::assert_eq("get_Selection(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_Selection(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->MakeTextSelection(0, 0, NULL, 0, 0, 0, 0, 0, FALSE, 0, NULL, FALSE,
				NULL));

				unitpp::assert_eq("MakeTextSelection(..., NULL, ..., NULL, ..., NULL) HRESULT",
				E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("MakeTextSelection(..., NULL, ..., NULL, ..., NULL) HRESULT",
				E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->MakeRangeSelection(NULL, NULL, FALSE, NULL));

				unitpp::assert_eq("MakeRangeSelection(NULL, NULL, FALSE, NULL) HRESULT",
				E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("MakeRangeSelection(NULL, NULL, FALSE, NULL) HRESULT",
				E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->MakeSimpleSel(FALSE, FALSE, FALSE, FALSE, NULL));

				unitpp::assert_eq("MakeSimpleSel(FALSE, FALSE, FALSE, FALSE, NULL) HRESULT",
				E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("MakeSimpleSel(FALSE, FALSE, FALSE, FALSE, NULL) HRESULT",
				E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->MakeTextSelInObj(0, 0, NULL, 0, NULL, FALSE, FALSE, FALSE, FALSE,
				FALSE, NULL));

				unitpp::assert_eq("MakeTextSelInObj(..., NULL, ..., NULL, ..., NULL) HRESULT",
				E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("MakeTextSelInObj(..., NULL, ..., NULL, ..., NULL) HRESULT",
				E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->MakeSelAt(0, 0, rc, rc, FALSE, NULL));
				unitpp::assert_eq("MakeSelAt(0, 0, rc, rc, FALSE, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("MakeSelAt(0, 0, rc, rc, FALSE, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->get_IsClickInText(0, 0, rc, rc, NULL));
				unitpp::assert_eq("get_IsClickInText(0, 0, rc, rc, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_IsClickInText(0, 0, rc, rc, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->get_IsClickInObject(0, 0, rc, rc, NULL, NULL));

				unitpp::assert_eq("get_IsClickInObject(0, 0, rc, rc, NULL, NULL) HRESULT",
				E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_IsClickInObject(0, 0, rc, rc, NULL, NULL) HRESULT",
				E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->get_IsClickInOverlayTag(0, 0, rc, rc, NULL, NULL, NULL, NULL, NULL,
				NULL));

				unitpp::assert_eq(
				"get_IsClickInOverlayTag(..., NULL, NULL, NULL, NULL, NULL, NULL) HRESULT",
				E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq(
				"get_IsClickInOverlayTag(..., NULL, NULL, NULL, NULL, NULL, NULL) HRESULT",
				E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->OnTyping(NULL, NULL, 0, 0, 0, NULL));
				unitpp::assert_eq("OnTyping(NULL, NULL, 0, 0, 0, NULL) HRESULT",
				E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("OnTyping(NULL, NULL, 0, 0, 0, NULL) HRESULT",
				E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->PrepareToDraw(NULL, rc, rc, NULL));
				unitpp::assert_eq("PrepareToDraw(NULL, rc, rc, NULL) HRESULT", E_UNEXPECTED, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("PrepareToDraw(NULL, rc, rc, NULL) HRESULT", E_UNEXPECTED, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->DrawRoot(NULL, rc, rc, FALSE));
				unitpp::assert_eq("DrawRoot(NULL, rc, rc, FALSE) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("DrawRoot(NULL, rc, rc, FALSE) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->Layout(NULL, 0));
				unitpp::assert_eq("Layout(NULL, 0) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("Layout(NULL, 0) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->get_Height(NULL));
				unitpp::assert_eq("get_Height(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_Height(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->get_Width(NULL));
				unitpp::assert_eq("get_Width(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_Width(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->Print(NULL, NULL));
				unitpp::assert_eq("Print(NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("Print(NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->get_Site(NULL));
				unitpp::assert_eq("get_Site(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_Site(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->LoseFocus(NULL));
				unitpp::assert_eq("LoseFocus(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("LoseFocus(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->AddSelChngListener(NULL));
				unitpp::assert_eq("AddSelChngListener(NULL) HRESULT", S_OK, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("AddSelChngListener(NULL) HRESULT", S_OK, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->DelSelChngListener(NULL));
				unitpp::assert_eq("DelSelChngListener(NULL) HRESULT", S_OK, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("DelSelChngListener(NULL) HRESULT", S_OK, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->get_Stylesheet(NULL));
				unitpp::assert_eq("get_Stylesheet(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_Stylesheet(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->IsDirty(NULL));
				unitpp::assert_eq("IsDirty(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("IsDirty(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try
			{
				CheckHr(hr = m_qrootb->get_XdPos(NULL));
				unitpp::assert_eq("get_XdPos(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_XdPos(NULL) HRESULT", E_POINTER, thr.Result());
			}
//			hr = m_qrootb->putref_Overlay(NULL);		// requires m_qvrs to be valid.
//			hr = m_qrootb->SetTableColWidths(NULL, 0);	// requires m_qvrs to be valid.
//			hr = m_qrootb->GetRootVariant(NULL);	// Not yet implemented: has Assert(false);
		}

		void testTypeEnter()
		{
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
			StrUni stuPara1(L"This is the first test paragraph");
			qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoOrigPara1, kflidStTxtPara_Contents, qtss);
			StrUni stuPara2(L"This is the second test paragraph");
			qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoOrigPara2, kflidStTxtPara_Contents, qtss);

			// Now make them the paragraphs of an StText.
			HVO rghvo[2] = {khvoOrigPara1, khvoOrigPara2};
			HVO hvoRoot = 101;
			qcda->CacheVecProp(hvoRoot, kflidStText_Paragraphs, rghvo, 2);

			// Now make the root box and view constructor and Graphics object.
			IVwRootBoxPtr qrootb;
			qrootb.CreateInstance(CLSID_VwRootBox);
			IVwGraphicsWin32Ptr qvg32;
			HDC hdc = 0;
			try
			{
				qvg32.CreateInstance(CLSID_VwGraphicsWin32);
				hdc = ::GetDC(NULL);
				qvg32->Initialize(hdc);

				IVwViewConstructorPtr qvc;
				qvc.Attach(NewObj DummyParaVc());
				qrootb->putref_DataAccess(qsda);
				qrootb->SetRootObject(hvoRoot, qvc, kfragStText, NULL);
				DummyRootSitePtr qdrs;
				qdrs.Attach(NewObj DummyRootSite());
				Rect rcSrc(0, 0, 96, 96);
				qdrs->SetRects(rcSrc, rcSrc);
				qdrs->SetGraphics(qvg32);
				qrootb->SetSite(qdrs);

				int chw = 13;
				IVwSelectionPtr qselTemp;
				// Put insertion point at the beginning of the view
				qrootb->MakeSimpleSel(true, true, false, true, &qselTemp);

				// Simulate an Enter key being pressed
				if (qrootb->OnExtendedKey(chw, kfssNone, 0) == S_FALSE)
				{
					SmartBstr bstr(L"\r", 1);
					int wsPending = -1;
					qrootb->OnTyping(qvg32, bstr, 0, 0, (wchar)13, &wsPending);
				}
				int chvoPara;
				qsda->get_VecSize(hvoRoot, kflidStText_Paragraphs, &chvoPara);
				unitpp::assert_true("Should have three paragraphs now", chvoPara == 3);
				// Check that we are in the second paragraph now.
				ITsStringPtr qtss;
				int ich;
				ComBool fAssocPrev;
				PropTag tag;
				int ws;
				HVO hvoPara;
				qrootb->get_Selection(&qselTemp);
				qselTemp->TextSelInfo(false, &qtss, &ich, &fAssocPrev, &hvoPara, &tag, &ws);
				unitpp::assert_true("Should be at the beginning of (new) second para", ich == 0);
				unitpp::assert_true("New Para 2 should be the original para 1",
					hvoPara == khvoOrigPara1);
				int cchw;
				const OLECHAR * pwrgch;
				qtss->LockText(&pwrgch, &cchw);
				unitpp::assert_true("New para should not be empty", cchw > 0);
				unitpp::assert_true("New second para should contain contents of original first para",
					wcscmp(pwrgch, stuPara1.Chars()) == 0);
				qtss->UnlockText(pwrgch);

				// Move to the first para and check that it isn't either of the original ones
				qrootb->MakeSimpleSel(true, true, false, true, &qselTemp);
				VwTextSelectionPtr qvwsel = dynamic_cast<VwTextSelection *>(qselTemp.Ptr());
				unitpp::assert_true("Non-NULL m_qvwsel after MakeSimpleSel", qvwsel);
				qvwsel->TextSelInfo(false, &qtss, &ich, &fAssocPrev, &hvoPara, &tag, &ws);
				qtss->get_Length(&cchw);
				unitpp::assert_true("New (first) para should be empty", cchw == 0);
				unitpp::assert_true("New (first) para should have a different hvo from the originals",
					hvoPara != khvoOrigPara1 && hvoPara != khvoOrigPara2);

				// Move to the last para and check that it is still 2
				qrootb->MakeSimpleSel(false, true, false, true, &qselTemp);
				qvwsel = dynamic_cast<VwTextSelection *>(qselTemp.Ptr());
				qvwsel->TextSelInfo(false, &qtss, &ich, &fAssocPrev, &hvoPara, &tag, &ws);
				unitpp::assert_true("Last para's HVO should not have changed",
					hvoPara == khvoOrigPara2);
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

		void testTypeShiftEnter()
		{
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
			StrUni stuPara1(L"This is the first test paragraph");
			qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoOrigPara1, kflidStTxtPara_Contents, qtss);
			StrUni stuPara2(L"This is the second test paragraph");
			qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoOrigPara2, kflidStTxtPara_Contents, qtss);

			// Now make them the paragraphs of an StText.
			HVO rghvo[2] = {khvoOrigPara1, khvoOrigPara2};
			HVO hvoRoot = 101;
			qcda->CacheVecProp(hvoRoot, kflidStText_Paragraphs, rghvo, 2);

			// Now make the root box and view constructor and Graphics object.
			IVwRootBoxPtr qrootb;
			qrootb.CreateInstance(CLSID_VwRootBox);
			IVwGraphicsWin32Ptr qvg32;
			HDC hdc = 0;
			try
			{
				qvg32.CreateInstance(CLSID_VwGraphicsWin32);
				hdc = ::GetDC(NULL);
				qvg32->Initialize(hdc);

				IVwViewConstructorPtr qvc;
				qvc.Attach(NewObj DummyParaVc());
				qrootb->putref_DataAccess(qsda);
				qrootb->SetRootObject(hvoRoot, qvc, kfragStText, NULL);
				DummyRootSitePtr qdrs;
				qdrs.Attach(NewObj DummyRootSite());
				Rect rcSrc(0, 0, 96, 96);
				qdrs->SetRects(rcSrc, rcSrc);
				qdrs->SetGraphics(qvg32);
				qrootb->SetSite(qdrs);
				HRESULT hr = qrootb->Layout(qvg32, 300);
				unitpp::assert_true("TypeShiftEnter Layout succeeded", hr == S_OK);
				VwPrepDrawResult xpdr;
				hr = qrootb->PrepareToDraw(qvg32, rcSrc, rcSrc, &xpdr);
				unitpp::assert_true("TypeShiftEnter PrepareToDraw succeeded", hr == S_OK);

				IVwSelectionPtr qselTemp;
				// Put insertion point at the beginning of the view
				qrootb->MakeSimpleSel(true, true, false, true, &qselTemp);

				// Simulate a Shift-Enter key being pressed
				if (qrootb->OnExtendedKey(13, kfssShift, 0) == S_FALSE)
				{
					SmartBstr bstr(L"\r", 1);
					int wsPending = -1;
					qrootb->OnTyping(qvg32, bstr, 0, 0, (wchar)13, &wsPending);
				}

				int chvoPara;
				qsda->get_VecSize(hvoRoot, kflidStText_Paragraphs, &chvoPara);
				unitpp::assert_true("Should have two paragraphs still", chvoPara == 2);
				// Check that we are in the first paragraph still.
				ITsStringPtr qtss;
				int ich;
				ComBool fAssocPrev;
				PropTag tag;
				int ws;
				HVO hvoPara;
				qrootb->get_Selection(&qselTemp);
				unitpp::assert_true("Should have a selection", qselTemp);
				qselTemp->TextSelInfo(false, &qtss, &ich, &fAssocPrev, &hvoPara, &tag, &ws);
				// TODO: Maybe this test could be more explicit.
				unitpp::assert_true("Should be at the beginning of the second line of the first para",
					ich == 1);
				unitpp::assert_true("First para's HVO should not have changed",
					hvoPara == khvoOrigPara1);
				int cchw;
				const OLECHAR * pwrgch;
				qtss->LockText(&pwrgch, &cchw);
				unitpp::assert_true("Modified para should not be empty", cchw > 0);
				StrUni stuMod = stuPara1;
				wchar chw = kchwHardLineBreak;
				StrUni stuHardLineBreakChar(&chw, 1);
				stuMod.Replace(0, 0, stuHardLineBreakChar);
				int nDiff = wcscmp(pwrgch, stuMod.Chars());
				qtss->UnlockText(pwrgch); // do this before the assert
				unitpp::assert_true("Should have prepended U+2028 to contents of original first para.",
					nDiff == 0);

				// Attempt to go up a line. This should change selection to be at the beginning
				// of the para.
				hr = qrootb->OnExtendedKey(kecUpArrowKey, kfssNone, 0);
				unitpp::assert_eq("OnExtendedKey(kecUpArrowKey...) did not succeed", S_OK, hr);

				qrootb->get_Selection((IVwSelection**)&qselTemp);
				unitpp::assert_true("Non-NULL text selection after OnExtendedKey",
					dynamic_cast<VwTextSelection *>(qselTemp.Ptr()));
				qselTemp->TextSelInfo(false, &qtss, &ich, &fAssocPrev, &hvoPara, &tag, &ws);
				unitpp::assert_true("Should still be in first para",
					hvoPara == khvoOrigPara1);
				unitpp::assert_true("Should be at the beginning of the first line of the first para",
					ich == 0);
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

		void testAccessible()
		{
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
			StrUni stuPara1(L"This is the first test paragraph");
			qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoOrigPara1, kflidStTxtPara_Contents, qtss);
			StrUni stuPara2(L"This is the second test paragraph");
			qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoOrigPara2, kflidStTxtPara_Contents, qtss);
			StrUni stuPara3(L"This is the third test paragraph, which is quite long. "
				L"This is to make sure it has more than one string box.");
			qtsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoOrigPara3, kflidStTxtPara_Contents, qtss);

			// Now make them the paragraphs of an StText.
			HVO rghvo[3] = {khvoOrigPara1, khvoOrigPara2, khvoOrigPara3};
			HVO hvoRoot = 101;
			qcda->CacheVecProp(hvoRoot, kflidStText_Paragraphs, rghvo, 3);

			// Now make the root box and view constructor and Graphics object.
			IVwRootBoxPtr qrootb;
			qrootb.CreateInstance(CLSID_VwRootBox);
			IVwGraphicsWin32Ptr qvg32;
			HDC hdc = 0;
			try
			{
				qvg32.CreateInstance(CLSID_VwGraphicsWin32);
				hdc = ::GetDC(NULL);
				qvg32->Initialize(hdc);

				IVwViewConstructorPtr qvc;
				qvc.Attach(NewObj DummyParaVc());
				qrootb->putref_DataAccess(qsda);
				qrootb->SetRootObject(hvoRoot, qvc, kfragStText, NULL);
				DummyRootSitePtr qdrs;
				qdrs.Attach(NewObj DummyRootSite());
				Rect rcSrc(0, 0, 96, 96);
				qdrs->SetRects(rcSrc, rcSrc);
				qdrs->SetGraphics(qvg32);
				qrootb->SetSite(qdrs);
				HRESULT hr = qrootb->Layout(qvg32, 300);
				unitpp::assert_true("Accessible Layout succeeded", hr == S_OK);
				VwPrepDrawResult xpdr;
				hr = qrootb->PrepareToDraw(qvg32, rcSrc, rcSrc, &xpdr);
				unitpp::assert_true("Accessible PrepareToDraw succeeded", hr == S_OK);

				ComSmartPtr<IServiceProvider> qsp;
				hr = qrootb->QueryInterface(IID_IServiceProvider, (void **) &qsp);
				WarnHr(hr);
				unitpp::assert_true("Got service provider", hr == S_OK);

				ComSmartPtr<IAccessible> qacc;
				hr = qsp->QueryService(GUID_NULL, IID_IAccessible, (void **) &qacc);
				unitpp::assert_true("Got IAccessible", hr == S_OK);

				hr = qacc->QueryInterface(IID_IServiceProvider, (void **) &qsp);
				unitpp::assert_true("Got service provider from IAccessible", hr == S_OK);

				IVwRootBoxPtr qrootbT;
				hr = qsp->QueryService(GUID_NULL, IID_IVwRootBox, (void **) &qrootbT);
				unitpp::assert_true("Got root box back", hr == S_OK);

				unitpp::assert_eq("Got same root box back", qrootb.Ptr(), qrootbT.Ptr());

				// Now beat on the IAccessible

				// accNavigate
				// The current impl doesn't care what the start argument is, but I think
				// this is the right thing to pass in case it ever does.
				VARIANT vtSelf;
				vtSelf.vt = VT_I4;
				vtSelf.intVal = CHILDID_SELF;
				SmartVariant svt;
				hr = qacc->accNavigate(NAVDIR_DOWN, vtSelf, &svt);
				unitpp::assert_eq("Nowhere is down from root", VT_EMPTY, svt.vt);
				hr = qacc->accNavigate(NAVDIR_UP, vtSelf, &svt);
				unitpp::assert_eq("Nowhere is up from root", VT_EMPTY, svt.vt);
				hr = qacc->accNavigate(NAVDIR_LEFT, vtSelf, &svt);
				unitpp::assert_eq("Nowhere is left from root", VT_EMPTY, svt.vt);
				hr = qacc->accNavigate(NAVDIR_RIGHT, vtSelf, &svt);
				unitpp::assert_eq("Nowhere is right from root", VT_EMPTY, svt.vt);
				hr = qacc->accNavigate(NAVDIR_NEXT, vtSelf, &svt);
				unitpp::assert_eq("Nowhere is after root", VT_EMPTY, svt.vt);
				hr = qacc->accNavigate(NAVDIR_PREVIOUS, vtSelf, &svt);
				unitpp::assert_eq("Nowhere is before root", VT_EMPTY, svt.vt);

				IAccessiblePtr qaccFirst;
				hr = qacc->accNavigate(NAVDIR_FIRSTCHILD, vtSelf, &svt);
				unitpp::assert_eq("Something is first child of root", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccFirst);
				unitpp::assert_true("First child of root is IAccessible", qaccFirst.Ptr() != NULL);
				unitpp::assert_true("First child of root is not root", qaccFirst.Ptr() != qacc.Ptr());

				IAccessiblePtr qaccLast;
				hr = qacc->accNavigate(NAVDIR_LASTCHILD, vtSelf, &svt);
				unitpp::assert_eq("Something is last child of root", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccLast);
				unitpp::assert_true("Last child of root is IAccessible", qaccLast.Ptr() != NULL);
				unitpp::assert_true("Last child of root is not root", qaccLast.Ptr() != qacc.Ptr());
				unitpp::assert_true("Last child of root is not first", qaccLast.Ptr() != qaccFirst.Ptr());

				IAccessiblePtr qaccMid;
				hr = qaccFirst->accNavigate(NAVDIR_NEXT, vtSelf, &svt);
				unitpp::assert_eq("Something is next from 1st part", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccMid);
				unitpp::assert_true("2nd child of root is IAccessible", qaccMid.Ptr() != NULL);
				unitpp::assert_true("2nd child of root is not root", qaccMid.Ptr() != qacc.Ptr());
				unitpp::assert_true("2nd child of root is not first", qaccMid.Ptr() != qaccFirst.Ptr());
				unitpp::assert_true("2nd child of root is not last", qaccMid.Ptr() != qaccLast.Ptr());

				IAccessiblePtr qaccT;
				hr = qaccMid->accNavigate(NAVDIR_NEXT, vtSelf, &svt);
				unitpp::assert_eq("Something is next from 2nd para", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("next from 2nd child is IAccessible", qaccT.Ptr() != NULL);
				unitpp::assert_true("next from 2nd child is last", qaccT.Ptr() == qaccLast.Ptr());

				hr = qaccMid->accNavigate(NAVDIR_DOWN, vtSelf, &svt);
				unitpp::assert_eq("Something is down from 2nd para", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("down from 2nd child of root is last", qaccT.Ptr() == qaccLast.Ptr());

				hr = qaccLast->accNavigate(NAVDIR_DOWN, vtSelf, &svt);
				unitpp::assert_eq("Nothing is down from last para", VT_EMPTY, svt.vt);
				hr = qaccLast->accNavigate(NAVDIR_NEXT, vtSelf, &svt);
				unitpp::assert_eq("Nothing is next from last para", VT_EMPTY, svt.vt);

				hr = qaccMid->accNavigate(NAVDIR_PREVIOUS, vtSelf, &svt);
				unitpp::assert_eq("Something is prev from 2nd para", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("prev from 2nd child of root is first", qaccT.Ptr() == qaccFirst.Ptr());

				hr = qaccMid->accNavigate(NAVDIR_UP, vtSelf, &svt);
				unitpp::assert_eq("Something is up from 2nd para", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("up from 2nd child of root is first", qaccT.Ptr() == qaccFirst.Ptr());

				hr = qaccFirst->accNavigate(NAVDIR_PREVIOUS, vtSelf, &svt);
				unitpp::assert_eq("Nothing is prev from first para", VT_EMPTY, svt.vt);
				hr = qaccFirst->accNavigate(NAVDIR_UP, vtSelf, &svt);
				unitpp::assert_eq("Nothing is up from first para", VT_EMPTY, svt.vt);

				// Deliberately don't verify left and right from things in pile...might decide to
				// make them synonymns of up and down. But make sure they don't crash.
				hr = qaccFirst->accNavigate(NAVDIR_LEFT, vtSelf, &svt);
				hr = qaccFirst->accNavigate(NAVDIR_RIGHT, vtSelf, &svt);

				// Now down into 3rd paragraph...
				IAccessiblePtr qaccStr1;
				hr = qaccLast->accNavigate(NAVDIR_FIRSTCHILD, vtSelf, &svt);
				unitpp::assert_eq("Something is 1st child of last para", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccStr1);
				unitpp::assert_true("1st child of last para is IAccessible", qaccStr1.Ptr() != NULL);

				// Should be at least two children...
				IAccessiblePtr qaccStr2;
				hr = qaccStr1->accNavigate(NAVDIR_NEXT, vtSelf, &svt);
				unitpp::assert_eq("Something is next from 1st str", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccStr2);
				unitpp::assert_true("next from 1st str is IAccessible", qaccStr2.Ptr() != NULL);
				unitpp::assert_true("next from 1st child is different", qaccStr2.Ptr() != qaccStr1.Ptr());

				hr = qaccStr1->accNavigate(NAVDIR_RIGHT, vtSelf, &svt);
				unitpp::assert_eq("Something is right from 1st str", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("right from 1st str is 2nd", qaccStr2.Ptr() == qaccT.Ptr());

				hr = qaccStr2->accNavigate(NAVDIR_PREVIOUS, vtSelf, &svt);
				unitpp::assert_eq("Something is prev from 2nd str", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("prev from 2nd str is 1st", qaccStr1.Ptr() == qaccT.Ptr());

				hr = qaccStr2->accNavigate(NAVDIR_LEFT, vtSelf, &svt);
				unitpp::assert_eq("Something is left from 2nd str", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("left from 2nd str is 1st", qaccStr1.Ptr() == qaccT.Ptr());

				// Now check out parent
				IDispatchPtr qdisp;
				hr = qaccFirst->get_accParent(&qdisp);
				hr = qdisp->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("parent of first is root", qacc.Ptr() == qaccT.Ptr());

				hr = qaccMid->get_accParent(&qdisp);
				hr = qdisp->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("parent of 2nd is root", qacc.Ptr() == qaccT.Ptr());

				hr = qaccStr2->get_accParent(&qdisp);
				hr = qdisp->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("parent of 2nd str is last para", qaccLast.Ptr() == qaccT.Ptr());

				hr = qacc->get_accParent(&qdisp);
				unitpp::assert_true("parent of root is NULL", qdisp.Ptr() == NULL);

				// Try child.
				VARIANT vtInd;
				vtInd.vt = VT_I4;
				vtInd.intVal = 0;
				hr = qacc->get_accChild(vtInd, &qdisp);
				hr = qdisp->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("child[0] of root is 1st child", qaccT.Ptr() == qaccFirst.Ptr());

				hr = qaccLast->get_accChild(vtInd, &qdisp);
				hr = qdisp->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("child[0] of last para is 1st str", qaccT.Ptr() == qaccStr1.Ptr());

				vtInd.intVal = 1;
				hr = qacc->get_accChild(vtInd, &qdisp);
				hr = qdisp->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("child[1] of root is 2nd child", qaccT.Ptr() == qaccMid.Ptr());

				hr = qaccLast->get_accChild(vtInd, &qdisp);
				hr = qdisp->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("child[1] of last para is 2nd str", qaccT.Ptr() == qaccStr2.Ptr());

				vtInd.intVal = 2;
				hr = qacc->get_accChild(vtInd, &qdisp);
				hr = qdisp->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("child[2] of root is 3rd child", qaccT.Ptr() == qaccLast.Ptr());

				// child count
				long cchild;
				hr = qacc->get_accChildCount(&cchild);
				unitpp::assert_eq("root has 3 children", 3, cchild);

				//Description
				SmartBstr bstrDesc;
				hr = qaccFirst->get_accDescription(vtSelf, &bstrDesc);
				unitpp::assert_true("Expected description 1st para", wcscmp(bstrDesc, stuPara1.Chars()) == 0);

				// I'm not sure just how long much will fit on a line, so just compare the first 15.
				hr = qaccStr1->get_accDescription(vtSelf, &bstrDesc);
				unitpp::assert_true("Expected description 1st seg of last para",
					wcsncmp(bstrDesc, stuPara3.Chars(), 15)== 0);

				// Name (get_accName)
				hr = qacc->get_accName(vtSelf, &bstrDesc);
				unitpp::assert_true("Name of Root", wcscmp(bstrDesc, L"Root") == 0);

				hr = qaccFirst->get_accName(vtSelf, &bstrDesc);
				unitpp::assert_true("Name of para", wcscmp(bstrDesc, L"Paragraph") == 0);

				hr = qaccStr1->get_accName(vtSelf, &bstrDesc);
				unitpp::assert_true("Name of string", wcscmp(bstrDesc, L"String") == 0);

				// Role (get_accRole)
				hr = qacc->get_accRole(vtSelf, &svt);
				unitpp::assert_eq("Role returns int", VT_I4, svt.vt);
				unitpp::assert_eq("Role of root", ROLE_SYSTEM_GROUPING, svt.intVal);

				hr = qaccMid->get_accRole(vtSelf, &svt);
				unitpp::assert_eq("Role returns int", VT_I4, svt.vt);
				unitpp::assert_eq("Role of para", ROLE_SYSTEM_TEXT, svt.intVal);

				hr = qaccStr1->get_accRole(vtSelf, &svt);
				unitpp::assert_eq("Role returns int", VT_I4, svt.vt);
				unitpp::assert_eq("Role of para", ROLE_SYSTEM_TEXT, svt.intVal);

				// State (get_accState)
				hr = qacc->get_accState(vtSelf, &svt);
				unitpp::assert_eq("State returns int", VT_I4, svt.vt);
				unitpp::assert_eq("State of root", 0, svt.intVal);

				hr = qaccLast->get_accState(vtSelf, &svt);
				unitpp::assert_eq("State returns int", VT_I4, svt.vt);
				unitpp::assert_eq("State of root", STATE_SYSTEM_SELECTABLE, svt.intVal);
				// Don't try the strings, I'm not sure what their answer should be.

				// accLocation
				long leftRoot, topRoot, widthRoot, heightRoot;
				hr = qacc->accLocation(&leftRoot, &topRoot, &widthRoot, &heightRoot, vtSelf);
				int iT;
				hr = qrootb->get_Height(&iT);
				unitpp::assert_eq("Height of root", iT, heightRoot);
				hr = qrootb->get_Width(&iT);
				unitpp::assert_eq("Height of root", iT, widthRoot);

				long left, top, width, height;
				hr = qaccFirst->accLocation(&left, &top, &width, &height, vtSelf);
				unitpp::assert_eq("Top of root and First", topRoot, top);
				unitpp::assert_eq("Left of root and First", leftRoot, left);
				// JT: used to be equal, but now paragraphs take their true size.
				unitpp::assert_true("Width of root and First", widthRoot > width);
				unitpp::assert_true("First para smaller than root", height < heightRoot);

				long bottomFirst = top + height;
				long heightFirst = height;
				hr = qaccMid->accLocation(&left, &top, &width, &height, vtSelf);
				unitpp::assert_eq("Top of Mid and bottom of First", bottomFirst, top);
				unitpp::assert_eq("Left of root and Mid", leftRoot, left);
				unitpp::assert_true("Width of root and Mid", widthRoot > width);
				unitpp::assert_true("2 paras smaller than root", height + heightFirst < heightRoot);

				long bottomMid = top + height;
				long heightMid = height;
				hr = qaccLast->accLocation(&left, &top, &width, &height, vtSelf);
				unitpp::assert_eq("Top of Last and bottom of Mid", bottomMid, top);
				unitpp::assert_eq("Left of root and Last", leftRoot, left);
				unitpp::assert_eq("Width of root and Last", widthRoot, width);
				unitpp::assert_true("3 paras root", height + heightFirst + heightMid == heightRoot);

				long heightLast = height;
				hr = qaccStr1->accLocation(&left, &top, &width, &height, vtSelf);
				unitpp::assert_eq("Top of Last and top of 1st seg", bottomMid, top);
				unitpp::assert_true("seg < para", height < heightLast);

				// accHitTest
				hr = qacc->accHitTest(leftRoot + 1, topRoot + 1, &svt);
				unitpp::assert_eq("Something is hit near top of root", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("Hit first child near top of root", qaccFirst.Ptr() == qaccT.Ptr());

				hr = qacc->accHitTest(leftRoot + 1, bottomFirst + 1, &svt);
				unitpp::assert_eq("Something is hit near mid of root", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("Hit mid child near mid of root", qaccMid.Ptr() == qaccT.Ptr());

				hr = qacc->accHitTest(leftRoot + widthRoot - 1, topRoot + heightRoot - 1, &svt);
				unitpp::assert_eq("Something is hit near bottom  right of root", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("Hit mid child near mid of root", qaccLast.Ptr() == qaccT.Ptr());

				hr = qaccLast->accHitTest(leftRoot + 1, bottomMid + 1, &svt);
				unitpp::assert_eq("Something is hit near top last para", VT_DISPATCH, svt.vt);
				hr = svt.pdispVal->QueryInterface(IID_IAccessible, (void **)&qaccT);
				unitpp::assert_true("Hit mid child near mid of root", qaccStr1.Ptr() == qaccT.Ptr());
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

		void AttachGuidToChar(ITsString * ptss, int ich, const GUID * pguid, ITsString ** pptss)
		{
			ITsStrBldrPtr qtsb;
			ptss->GetBldr(&qtsb);
			StrUni stuVal;
			OLECHAR * pchVal;
			stuVal.SetSize(1 + isizeof(GUID)/isizeof(OLECHAR), &pchVal);
			*pchVal++ = kodtOwnNameGuidHot;
			memcpy(pchVal, pguid, isizeof(GUID));
			qtsb->SetStrPropValue(ich, ich + 1, ktptObjData, stuVal.Bstr());
			qtsb->GetString(pptss);
			OLECHAR ch;
			qtsb->FetchChars(ich, ich + 1, &ch);
			unitpp::assert_eq("Guid attached to object char", L'\xfffc', ch);
		}

		void SetParaContents(ITsStrFactory * ptsf, IVwCacheDa * pcda)
		{
			ITsStringPtr qtss;
			ITsStringPtr qtssT;

			// Now make five strings, the contents of paragraphs. Each has an ORC
			StrUni stuPara1(L"This is the first test paragraph\xfffc");
			ptsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtssT);
			AttachGuidToChar(qtssT, stuPara1.Length() - 1, &s_rgguid[0], &qtss);
			pcda->CacheStringProp(khvoOrigPara1, kflidStTxtPara_Contents, qtss);

			StrUni stuPara2(L"This\xfffc is the second test paragraph\xfffc");
			ptsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			AttachGuidToChar(qtss, 4, &s_rgguid[1], &qtssT);
			AttachGuidToChar(qtssT, stuPara2.Length() - 1, &s_rgguid[2], &qtss);
			pcda->CacheStringProp(khvoOrigPara2, kflidStTxtPara_Contents, qtss);

			StrUni stuPara3(L"\xfffcThis\xfffc is the third test paragraph");
			ptsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtss);
			AttachGuidToChar(qtss, 0, &s_rgguid[3], &qtssT);
			AttachGuidToChar(qtssT, 5, &s_rgguid[4], &qtss);
			pcda->CacheStringProp(khvoOrigPara3, kflidStTxtPara_Contents, qtss);

			StrUni stuPara4(L"\xfffcThis\xfffc is the fourth test paragraph");
			ptsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtss);
			AttachGuidToChar(qtss, 0, &s_rgguid[5], &qtssT);
			AttachGuidToChar(qtssT, 5, &s_rgguid[6], &qtss);
			pcda->CacheStringProp(khvoOrigPara4, kflidStTxtPara_Contents, qtss);

			StrUni stuPara5(L"\xfffc para5");
			ptsf->MakeString(stuPara5.Bstr(), g_wsEng, &qtssT);
			AttachGuidToChar(qtssT, 0, &s_rgguid[7], &qtss);
			pcda->CacheStringProp(khvoOrigPara5, kflidStTxtPara_Contents, qtss);
		}

		void testObjCharNotification()
		{
			// Create test data in a temporary cache.
			// First make some generic objects.
			IVwCacheDaPtr qcda;
			qcda.CreateInstance(CLSID_VwCacheDa);
			ISilDataAccessPtr qsda;
			qcda->QueryInterface(IID_ISilDataAccess, (void **)&qsda);
			qsda->putref_WritingSystemFactory(g_qwsf);

			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);

			SetParaContents(qtsf, qcda);

			// Now make them the paragraphs of an StText.
			HVO rghvo[5] = {khvoOrigPara1, khvoOrigPara2, khvoOrigPara3,
				khvoOrigPara4, khvoOrigPara5};
			HVO hvoRoot = 101;
			qcda->CacheVecProp(hvoRoot, kflidStText_Paragraphs, rghvo, 5);

			// Now make the root box and view constructor and Graphics object.
			IVwRootBoxPtr qrootb;
			qrootb.CreateInstance(CLSID_VwRootBox);
			IVwGraphicsWin32Ptr qvg32;
			HDC hdc = 0;
			try
			{
				qvg32.CreateInstance(CLSID_VwGraphicsWin32);
				hdc = ::GetDC(NULL);
				qvg32->Initialize(hdc);

				IVwViewConstructorPtr qvc;
				qvc.Attach(NewObj DummyParaVc());
				qrootb->putref_DataAccess(qsda);
				qrootb->SetRootObject(hvoRoot, qvc, kfragStText, NULL);
				DummyRootSitePtr qdrs;
				qdrs.Attach(NewObj DummyRootSite());
				Rect rcSrc(0, 0, 96, 96);
				qdrs->SetRects(rcSrc, rcSrc);
				qdrs->SetGraphics(qvg32);
				qrootb->SetSite(qdrs);
				HRESULT hr = qrootb->Layout(qvg32, 300);
				unitpp::assert_true("ObjCharNotification Layout succeeded", hr == S_OK);

				// set up mock notifier.
				MockNotifyObjCharDeletion nocd;
				qrootb->RequestObjCharDeleteNotification(&nocd);

				// Make a selection that spans all four ORCs; delete it.
				VwSelLevInfo vsli;
				vsli.tag = kflidStText_Paragraphs;
				vsli.cpropPrevious = 0; //1st (and only) occurrence of text paragraphs property.
				vsli.ihvo = 1; // 2nd paragraph.
				hr = qrootb->MakeTextSelection(
					0, // First (and only) object in root.
					1, &vsli, // simulate an array of one item for first object to select.
					kflidStTxtPara_Contents, // the only property of that object
					0, // first (and only) occurrence)
					10,// selection starts at position 10 (anywhere after first ORC would do).
					4, // ends at character 4 (just before second ORC)
					0, // not multilingual, don't care about ws
					false, // range, don't care about association direction
					3, // selection ends in 4th paragraph
					NULL, // no special insertion point props
					true, // install it
					NULL); // No need to return it.
				int wsPending = -1;
				hr = qrootb->OnTyping(qvg32, NULL, 1, 0, L'\x0008', &wsPending);

				// verify that expected notifications (and no others) are received.
				int rgExpected[] = {0, 0, 1, 1, 1, 1, 0};
				nocd.VerifyResults(rgExpected);

				// Reset the contents and update the view to match.
				SetParaContents(qtsf, qcda);
				qcda->CacheVecProp(hvoRoot, kflidStText_Paragraphs, rghvo, 5);
				qsda->PropChanged(NULL, kpctNotifyAll, hvoRoot, kflidStText_Paragraphs, 0, 5, 3);
				// This time we'll delete (exactly) the whole of the second paragraph. (This is
				// a special case in the delete code, so needs a separate test.)
				hr = qrootb->MakeTextSelection(
					0, // First (and only) object in root.
					1, &vsli, // simulate an array of one item for first object to select.
					kflidStTxtPara_Contents, // the only property of that object
					0, // first (and only) occurrence)
					0,// selection starts at position 0 (anywhere after first ORC would do).
					0, // ends at character 0 (start of para 3)
					0, // not multilingual, don't care about ws
					false, // range, don't care about association direction
					2, // selection ends in 3rd paragraph
					NULL, // no special insertion point props
					true, // install it
					NULL); // No need to return it.
				nocd.Reset();
				hr = qrootb->OnTyping(qvg32, NULL, 1, 0, L'\x0008', &wsPending);
				int rgExpected2[] = {0, 1, 1, 0, 0, 0, 0}; // Exactly the two in the
				nocd.VerifyResults(rgExpected2);

				// Reset the contents and update the view to match.
				SetParaContents(qtsf, qcda);
				qcda->CacheVecProp(hvoRoot, kflidStText_Paragraphs, rghvo, 5);
				qsda->PropChanged(NULL, kpctNotifyAll, hvoRoot, kflidStText_Paragraphs, 0, 5, 3);

				// Simulate a backspace (at an IP) that clobbers one, the one at position 0 in para 3.
				hr = qrootb->MakeTextSelection(
					0, // First (and only) object in root.
					1, &vsli, // simulate an array of one item for first object to select.
					kflidStTxtPara_Contents, // the only property of that object
					0, // first (and only) occurrence)
					5,// IP after the 5th character.
					5, // ends there also (to make it an IP)
					0, // not multilingual, don't care about ws
					true, // associate with previous char is most typical
					-1, // IP, end paragraph same as start
					NULL, // no special insertion point props
					true, // install it
					NULL); // No need to return it.
				nocd.Reset();
				hr = qrootb->OnTyping(qvg32, NULL, 1, 0, L'\x0008', &wsPending);
				int rgExpected3[] = {0, 1, 0, 0, 0, 0, 0}; // Exactly the first in the 2nd para.
				nocd.VerifyResults(rgExpected3);

				// Simulate a del (at an IP) that clobbers one, the one at position 4 in para 2.
				// (We don't need to regenerate the display, we didn't change it much.)
				vsli.ihvo = 2; // 3rd paragraph.
				hr = qrootb->MakeTextSelection(
					0, // First (and only) object in root.
					1, &vsli, // simulate an array of one item for first object to select.
					kflidStTxtPara_Contents, // the only property of that object
					0, // first (and only) occurrence)
					0,// IP at start.
					0, // ends there also (to make it an IP)
					0, // not multilingual, don't care about ws
					true, // associate with previous char is most typical
					-1, // IP, end paragraph same as start
					NULL, // no special insertion point props
					true, // install it
					NULL); // No need to return it.
				nocd.Reset();
				hr = qrootb->OnTyping(qvg32, NULL, 0, 1, kscDelForward, &wsPending);
				int rgExpected4[] = {0, 0, 0, 1, 0, 0, 0}; // Exactly the first in the 3rd para.
				nocd.VerifyResults(rgExpected4);

				// Todo: verify special case of deleting whole paragraph;
				// Make sure no reports for ORCs in adjacent paragraphs.
				// Verify effects of typing backspace and delete at IP.
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

		// This test could go in VwSelections instead, I suppose.
		void testMakeSelections()
		{
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
			StrUni stuPara1(L"This is the first test paragraph");
			qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoOrigPara1, kflidStTxtPara_Contents, qtss);
			StrUni stuPara2(L"This is the second test paragraph");
			qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoOrigPara2, kflidStTxtPara_Contents, qtss);
			StrUni stuPara3(L"This is the third test paragraph, which is quite long. "
				L"This is to make sure it has more than one string box.");
			qtsf->MakeString(stuPara3.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoOrigPara3, kflidStTxtPara_Contents, qtss);

			// Now make them the paragraphs of an StText.
			HVO rghvo[3] = {khvoOrigPara1, khvoOrigPara2, khvoOrigPara3};
			HVO hvoRoot = 101;
			qcda->CacheVecProp(hvoRoot, kflidStText_Paragraphs, rghvo, 3);

			// Now make the root box and view constructor and Graphics object.
			IVwRootBoxPtr qrootb;
			qrootb.CreateInstance(CLSID_VwRootBox);
			IVwGraphicsWin32Ptr qvg32;
			HDC hdc = 0;
			try
			{
				qvg32.CreateInstance(CLSID_VwGraphicsWin32);
				hdc = ::GetDC(NULL);
				qvg32->Initialize(hdc);

				IVwViewConstructorPtr qvc;
				qvc.Attach(NewObj DummyParaVc());
				qrootb->putref_DataAccess(qsda);
				qrootb->SetRootObject(hvoRoot, qvc, kfragStText, NULL);
				DummyRootSitePtr qdrs;
				qdrs.Attach(NewObj DummyRootSite());
				Rect rcSrc(0, 0, 96, 96);
				qdrs->SetRects(rcSrc, rcSrc);
				qdrs->SetGraphics(qvg32);
				qrootb->SetSite(qdrs);
				HRESULT hr = qrootb->Layout(qvg32, 300);
				unitpp::assert_eq("MakeSelections Layout succeeded", S_OK, hr);

				int cLevelsEnd, cLevelsAnchor;
				int ihvoRoot, cpropPrev, ichAnchor, ichEnd, ws, ihvoEnd;
				VwSelLevInfo vsli;
				PropTag tagTextProp;
				ComBool fAssocPrev;
				ITsTextPropsPtr qttp;
				/*
					HRESULT MakeSimpleSel(
						[in] ComBool fInitial,
						[in] ComBool fEdit,
						[in] ComBool fRange,
						[in] ComBool fInstall,
						[out, retval] IVwSelection ** ppsel);
				*/
				IVwSelectionPtr qvwsel0;
				hr = qrootb->MakeSimpleSel(true, true, false, false, &qvwsel0);
				unitpp::assert_eq("MakeSimpleSel(true,...) HRESULT", S_OK, hr);
				hr = qvwsel0->CLevels(true, &cLevelsEnd);
				unitpp::assert_eq("qvwsel0->CLevels(true) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel0 CLevels value for simple text", 2, cLevelsEnd);
				hr = qvwsel0->CLevels(false, &cLevelsAnchor);
				unitpp::assert_eq("qvwsel0->CLevels(false) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel0 both CLevels are equal for IP",
					cLevelsEnd, cLevelsAnchor);
				hr = qvwsel0->AllTextSelInfo(&ihvoRoot, 1, &vsli, &tagTextProp, &cpropPrev,
					&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, &qttp);
				unitpp::assert_eq("qvwsel0->AllTextInfo() HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel0 tagTextProp value",
					kflidStTxtPara_Contents, tagTextProp);
				unitpp::assert_eq("qvwsel0 ichEnd value", 0, ichEnd);
				unitpp::assert_eq("qvwsel0 ichEnd == ichAnchor for IP", ichAnchor, ichEnd);
				unitpp::assert_true("qvwsel0 fAssocPrev value", !fAssocPrev);
				unitpp::assert_eq("qvwsel0 in StText paras", kflidStText_Paragraphs, vsli.tag);
				unitpp::assert_eq("qvwsel0 in first paragraph", 0, vsli.ihvo);

				IVwSelectionPtr qvwsel1;
				hr = qrootb->MakeSimpleSel(false, true, false, false, &qvwsel1);
				unitpp::assert_eq("MakeSimpleSel(false,...) HRESULT", S_OK, hr);
				hr = qvwsel1->CLevels(true, &cLevelsEnd);
				unitpp::assert_eq("qvwsel1->CLevels(true) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel1 CLevels value for simple text", 2, cLevelsEnd);
				hr = qvwsel1->CLevels(false, &cLevelsAnchor);
				unitpp::assert_eq("qvwsel1->CLevels(false) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel1 both CLevels are equal for IP",
					cLevelsEnd, cLevelsAnchor);
				hr = qvwsel1->AllTextSelInfo(&ihvoRoot, 1, &vsli, &tagTextProp, &cpropPrev,
					&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, &qttp);
				unitpp::assert_eq("qvwsel1->AllTextInfo() HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel1 tagTextProp value",
					kflidStTxtPara_Contents, tagTextProp);
				unitpp::assert_eq("qvwsel1 ichEnd value", stuPara3.Length(), ichEnd);
				unitpp::assert_eq("qvwsel1 ichEnd == ichAnchor for IP", ichAnchor, ichEnd);
				unitpp::assert_true("qvwsel1 fAssocPrev value", fAssocPrev);
				unitpp::assert_eq("qvwsel1 in StText paras", kflidStText_Paragraphs, vsli.tag);
				unitpp::assert_eq("qvwsel1 in third paragraph", 2, vsli.ihvo);
				/*
					HRESULT MakeRangeSelection(
						[in] IVwSelection * pselAnchor,
						[in] IVwSelection * pselEnd,
						[in] ComBool fInstall,
						[out, retval] IVwSelection ** ppsel);
				*/
				IVwSelectionPtr qvwsel2;
				hr = qrootb->MakeRangeSelection(qvwsel0, qvwsel1, false, &qvwsel2);
				unitpp::assert_eq("MakeRangeSelection(qvwsel0,qvwsel1) HRESULT", S_OK, hr);
				hr = qvwsel2->CLevels(true, &cLevelsEnd);
				unitpp::assert_eq("qvwsel2->CLevels(true) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel2 End CLevels value", 2, cLevelsEnd);
				hr = qvwsel2->CLevels(false, &cLevelsAnchor);
				unitpp::assert_eq("qvwsel2->CLevels(false) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel2 both CLevels are equal for IP",
					cLevelsEnd, cLevelsAnchor);
				hr = qvwsel2->AllSelEndInfo(false, &ihvoRoot, 1, &vsli, &tagTextProp,
					&cpropPrev, &ichAnchor, &ws, &fAssocPrev, &qttp);
				unitpp::assert_eq("qvwsel2->AllSelEndInfo(false) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel2 ichAnchor value", 0, ichAnchor);
				unitpp::assert_eq("qvwsel2 Anchor tagTextProp value",
					kflidStTxtPara_Contents, tagTextProp);
				unitpp::assert_eq("qvwsel2 Anchor in StText paras",
					kflidStText_Paragraphs, vsli.tag);
				unitpp::assert_eq("qvwsel2 Anchor in first paragraph", 0, vsli.ihvo);
				hr = qvwsel2->AllSelEndInfo(true, &ihvoRoot, 1, &vsli, &tagTextProp,
					&cpropPrev, &ichAnchor, &ws, &fAssocPrev, &qttp);
				unitpp::assert_eq("qvwsel2->AllSelEndInfo(true) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel2 ichEnd value", stuPara3.Length(), ichEnd);
				unitpp::assert_eq("qvwsel2 End tagTextProp value",
					kflidStTxtPara_Contents, tagTextProp);
				unitpp::assert_eq("qvwsel2 End in StText paras",
					kflidStText_Paragraphs, vsli.tag);
				unitpp::assert_eq("qvwsel2 End in third paragraph", 2, vsli.ihvo);
				/*
					HRESULT MakeSelInBox(
						[in] IVwSelection * pselInit,
						[in] ComBool fEndPoint,
						[in] int iLevel,
						[in] int iBox,
						[in] ComBool fInitial,
						[in] ComBool fRange,
						[in] ComBool fInstall,
						[out, retval] IVwSelection ** ppsel);
				 */
				IVwSelectionPtr qvwsel3;
				hr = qrootb->MakeSelInBox(qvwsel0, true, 1, 0, false, false, false, &qvwsel3);
				unitpp::assert_eq("MakeSelInBox(qvwsel0, ...) HRESULT", S_OK, hr);
				hr = qvwsel3->CLevels(true, &cLevelsEnd);
				unitpp::assert_eq("qvwsel3->CLevels(true) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel3 CLevels value for simple text", 2, cLevelsEnd);
				hr = qvwsel3->CLevels(false, &cLevelsAnchor);
				unitpp::assert_eq("qvwsel3->CLevels(false) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel3 both CLevels are equal for IP",
					cLevelsEnd, cLevelsAnchor);
				hr = qvwsel3->AllTextSelInfo(&ihvoRoot, 1, &vsli, &tagTextProp, &cpropPrev,
					&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, &qttp);
				unitpp::assert_eq("qvwsel3->AllTextInfo() HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel3 tagTextProp value",
					kflidStTxtPara_Contents, tagTextProp);
				unitpp::assert_eq("qvwsel3 ichEnd value", stuPara1.Length(), ichEnd);
				unitpp::assert_eq("qvwsel3 ichEnd == ichAnchor for IP", ichAnchor, ichEnd);
				unitpp::assert_true("qvwsel3 fAssocPrev value", fAssocPrev);
				unitpp::assert_eq("qvwsel3 in StText paras", kflidStText_Paragraphs, vsli.tag);
				unitpp::assert_eq("qvwsel3 in first paragraph", 0, vsli.ihvo);

				IVwSelectionPtr qvwsel4;
				hr = qrootb->MakeSelInBox(qvwsel1, true, 1, 2, true, false, false, &qvwsel4);
				unitpp::assert_eq("MakeSelInBox(qvwsel1, ...) HRESULT", S_OK, hr);
				hr = qvwsel4->CLevels(true, &cLevelsEnd);
				unitpp::assert_eq("qvwsel4->CLevels(true) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel4 CLevels value for simple text", 2, cLevelsEnd);
				hr = qvwsel4->CLevels(false, &cLevelsAnchor);
				unitpp::assert_eq("qvwsel4->CLevels(false) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel4 both CLevels are equal for IP",
					cLevelsEnd, cLevelsAnchor);
				hr = qvwsel4->AllTextSelInfo(&ihvoRoot, 1, &vsli, &tagTextProp, &cpropPrev,
					&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, &qttp);
				unitpp::assert_eq("qvwsel4->AllTextInfo() HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel4 tagTextProp value",
					kflidStTxtPara_Contents, tagTextProp);
				unitpp::assert_eq("qvwsel4 ichEnd value", 0, ichEnd);
				unitpp::assert_eq("qvwsel4 End == Anchor for IP", ichAnchor, ichEnd);
				unitpp::assert_true("qvwsel4 fAssocPrev value", !fAssocPrev);
				unitpp::assert_eq("qvwsel4 in StText paras", kflidStText_Paragraphs, vsli.tag);
				unitpp::assert_eq("qvwsel4 in third paragraph", 2, vsli.ihvo);

				IVwSelectionPtr qvwsel5;
				hr = qrootb->MakeRangeSelection(qvwsel3, qvwsel4, false, &qvwsel5);
				unitpp::assert_eq("MakeRangeSelection(qvwsel3,qvwsel4) HRESULT", S_OK, hr);
				hr = qvwsel5->CLevels(true, &cLevelsEnd);
				unitpp::assert_eq("qvwsel5->CLevels(true) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel5 End CLevels value", 2, cLevelsEnd);
				hr = qvwsel5->CLevels(false, &cLevelsAnchor);
				unitpp::assert_eq("qvwsel5->CLevels(false) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel5 both CLevels are equal for IP",
					cLevelsEnd, cLevelsAnchor);
				hr = qvwsel5->AllSelEndInfo(false, &ihvoRoot, 1, &vsli, &tagTextProp,
					&cpropPrev, &ichAnchor, &ws, &fAssocPrev, &qttp);
				unitpp::assert_eq("qvwsel5->AllSelEndInfo(false) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel5 ichAnchor value", stuPara1.Length(), ichAnchor);
				unitpp::assert_eq("qvwsel5 Anchor tagTextProp value",
					kflidStTxtPara_Contents, tagTextProp);
				unitpp::assert_eq("qvwsel5 Anchor in StText paras",
					kflidStText_Paragraphs, vsli.tag);
				unitpp::assert_eq("qvwsel5 Anchor in first paragraph", 0, vsli.ihvo);
				hr = qvwsel5->AllSelEndInfo(true, &ihvoRoot, 1, &vsli, &tagTextProp,
					&cpropPrev, &ichAnchor, &ws, &fAssocPrev, &qttp);
				unitpp::assert_eq("qvwsel5->AllSelEndInfo(true) HRESULT", S_OK, hr);
				unitpp::assert_eq("qvwsel5 ichEnd value", 0, ichEnd);
				unitpp::assert_eq("qvwsel5 End tagTextProp value",
					kflidStTxtPara_Contents, tagTextProp);
				unitpp::assert_eq("qvwsel5 End in StText paras",
					kflidStText_Paragraphs, vsli.tag);
				unitpp::assert_eq("qvwsel5 End in third paragraph", 2, vsli.ihvo);
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

	public:
		TestVwRootBox();

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

#endif /*TESTVWROOTBOX_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
