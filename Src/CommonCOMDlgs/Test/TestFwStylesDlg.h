/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestFwXmlData.h
Responsibility:
Last reviewed:

	Unit tests for the FwXmlData class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTFWSTYLESDLG_H_INCLUDED
#define TESTFWSTYLESDLG_H_INCLUDED

#pragma once

#include "testCmnFwDlgs.h"

namespace TestCmnFwDlgs
{
	class MockCacheDa : public VwCacheDa
	{
		typedef VwCacheDa SuperClass;
	public:
		MockCacheDa()
		{
		}
	};
	typedef ComSmartPtr<MockCacheDa> MockCacheDaPtr;

	class AfMockStylesheet : public AfStylesheet
	{
	public:
		typedef AfStylesheet SuperClass;
		AfMockStylesheet()
		{
		}

		// Initialize the class and load the owner's collection of styles from the db
		void Init()
		{
			const int nFakeObjId = 573;
			const int nFakeFlid = 1001001;
			MockCacheDaPtr qmcd;
			qmcd.Attach(NewObj MockCacheDa);
			//ISilDataAccessPtr qsda;
			//hr = pmcd->QueryInterface(IID_ISilDataAccess, (void **)&qsda);
			SuperClass::Init(qmcd, nFakeObjId, nFakeFlid);
		}

		// Get dummy role (in pnRole) based on the style named bstrName.
		STDMETHOD(GetRole)(BSTR bstrName, int * pnRole)
		{
			SmartBstr name(bstrName);
			if (name.Equals(L"Paragraph"))
			{
				*pnRole = 1;
			}
			else if (name.Equals(L"Section"))
			{
				*pnRole = 2;
			}
			else
			{
				*pnRole = 0;
			}

			return S_OK;
		}


	};
	typedef ComSmartPtr<AfMockStylesheet> AfMockStylesheetPtr;

	class TestFwStylesDlg : public unitpp::suite
	{
		IFwCppStylesDlgPtr m_qfwst;

		void testNullArgs()
		{
			unitpp::assert_true("Non-null m_qfwst after setup", m_qfwst.Ptr() != 0);
			HRESULT hr;
			try{
				CheckHr(hr = m_qfwst->putref_WritingSystemFactory(NULL));
				unitpp::assert_eq("putref_WritingSystemFactory(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("putref_WritingSystemFactory(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qfwst->putref_Stylesheet(NULL));
				unitpp::assert_eq("putref_Stylesheet(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("putref_Stylesheet(NULL) HRESULT", E_POINTER, thr.Result());
			}
			BSTR bstrT;
			ComBool fT;
			try{
				CheckHr(hr = m_qfwst->GetResults(NULL, &fT, &fT, &fT, &fT));
				unitpp::assert_eq("GetResults(NULL, &fT, &fT, &fT, &fT) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetResults(NULL, &fT, &fT, &fT, &fT) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qfwst->GetResults(&bstrT, NULL, &fT, &fT, &fT));
				unitpp::assert_eq("GetResults(&bstrT, NULL, &fT, &fT, &fT) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetResults(&bstrT, NULL, &fT, &fT, &fT) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qfwst->GetResults(&bstrT, &fT, NULL, &fT, &fT));
				unitpp::assert_eq("GetResults(&bstrT, &fT, NULL, &fT, &fT) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetResults(&bstrT, &fT, NULL, &fT, &fT) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qfwst->GetResults(&bstrT, &fT, &fT, NULL, &fT));
				unitpp::assert_eq("GetResults(&bstrT, &fT, &fT, NULL, &fT) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetResults(&bstrT, &fT, &fT, NULL, &fT) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qfwst->GetResults(&bstrT, &fT, &fT, &fT, NULL));
				unitpp::assert_eq("GetResults(&bstrT, &fT, &fT, &fT, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetResults(&bstrT, &fT, &fT, &fT, NULL) HRESULT", E_POINTER, thr.Result());
			}
		}

		void testStylesDlgSetup()
		{
			FwCppStylesDlg * pzfwst = dynamic_cast<FwCppStylesDlg *>(m_qfwst.Ptr());
			unitpp::assert_true("typecast of interface to class", pzfwst);
			int ws = 25;	// Dummy value -- we think anything (but zero) will work okay.
			HRESULT hr;
			hr = m_qfwst->put_DlgType(ksdtStandard);
			unitpp::assert_eq("put_DlgType(ksdtStandard)", ksdtStandard, pzfwst->m_sdt);
		//	hr = m_qfwst->put_ShowAll(ComBool fShowAll);	// ksdtTransEditor only
			hr = m_qfwst->put_SysMsrUnit(knmm);
			unitpp::assert_eq("put_SysMsrUnit(knmm)", knmm, pzfwst->m_nMsrSys);
			hr = m_qfwst->put_UserWs(ws);
			unitpp::assert_eq("put_UserWs(ws)", ws, pzfwst->m_wsUser);
			hr = m_qfwst->put_HelpFile(NULL);				// no help file, alas.
			ILgWritingSystemFactoryPtr qwsf;				// get the registry based factory
			qwsf.CreateInstance(CLSID_LgWritingSystemFactory);
			hr = m_qfwst->putref_WritingSystemFactory(qwsf);
			unitpp::assert_eq("putref_WritingSystemFactory(qwsf)",
				qwsf.Ptr(), pzfwst->m_qwsf.Ptr());
			hr = m_qfwst->put_ParentHwnd(NULL);
			hr = m_qfwst->put_CanDoRtl(FALSE);
			unitpp::assert_eq("put_CanDoRtl(FALSE)", false, pzfwst->m_fCanDoRtl);
			hr = m_qfwst->put_OuterRtl(FALSE);
			unitpp::assert_eq("put_OuterRtl(FALSE)", false, pzfwst->m_fOuterRtl);
			hr = m_qfwst->put_FontFeatures(FALSE);
			unitpp::assert_eq("put_FontFeatures(FALSE)", false, pzfwst->m_fFontFeatures);

			const int nFakeObjId = 573;
			SmartBstr sbstrStyle(L"Normal");
			SmartBstr sbstrUsage(L"Usage");

			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			hr = qtpb->SetIntPropValues(ktptWs, ktpvDefault, ws);
			hr = qtpb->SetStrPropValue(ktptNamedStyle, sbstrStyle);
			ITsTextPropsPtr qttpNormal;
			hr = qtpb->GetTextProps(&qttpNormal);
			AfMockStylesheetPtr qams;
			qams.Attach(NewObj AfMockStylesheet);
			qams->Init();
			IVwStylesheetPtr qvss;
			hr = qams->PutStyle(sbstrStyle, sbstrUsage,
				nFakeObjId, 0, nFakeObjId, kstParagraph, TRUE, FALSE, qttpNormal);
			hr = qams->QueryInterface(IID_IVwStylesheet, (void **)&qvss);
			unitpp::assert_eq("AfStyleSheet::QueryInterface for IVwStylesheet", S_OK, hr);
			hr = m_qfwst->putref_Stylesheet(qvss);
			unitpp::assert_eq("putref_Stylesheet(qvss)", qvss.Ptr(), pzfwst->m_qvss.Ptr());

			hr = m_qfwst->put_CanFormatChar(TRUE);
			unitpp::assert_eq("put_CanFormatChar(TRUE)", true, pzfwst->m_fCanFormatChar);
			hr = m_qfwst->put_OnlyCharStyles(TRUE);
			unitpp::assert_eq("put_OnlyCharStyles(TRUE)", true, pzfwst->m_fOnlyCharStyles);
			hr = m_qfwst->put_StyleName(sbstrStyle);
			unitpp::assert_true("put_StyleName(sbstrStyle)",
				!wcscmp(sbstrStyle.Chars(), pzfwst->m_stuStyleName.Chars()));
			ITsTextProps * pttpNormal = qttpNormal.Ptr();
			hr = m_qfwst->SetTextProps(&pttpNormal, 1, NULL, 0);
			hr = m_qfwst->put_RootObjectId(nFakeObjId);
			unitpp::assert_eq("put_RootObjectId(nFakeObjId)", nFakeObjId, pzfwst->m_hvoRootObj);
			hr = m_qfwst->SetWritingSystemsOfInterest(&ws, 1);
		//	hr = m_qfwst->putref_LogFile(NULL);
			unitpp::assert_eq("put_RootObjectId(nFakeObjId)", 1, pzfwst->m_vwsAvailable.Size());
			unitpp::assert_eq("put_RootObjectId(nFakeObjId)", ws, pzfwst->m_vwsAvailable[0]);

			pzfwst->SetupForDoModal();
			AfStylesDlg * pasd = pzfwst->m_qafsd;
			unitpp::assert_true("SetupForDoModal creates AfStylesDlg object", pasd);
			TeStylesDlg * ptesd = dynamic_cast<TeStylesDlg *>(pasd);
			unitpp::assert_true("SetupForDoModal does not create TeStylesDlg object", !ptesd);

			bool fResult, fStylesChanged, fApply;
			StrUni stuStyleName;
			// This is really the Apply button
			fResult = pasd->ResultsForAdjustTsTextProps(kctidOk, &stuStyleName, fStylesChanged,
				fApply);
			unitpp::assert_true("GetModalResults(Apply) sets fApply", fApply);
			unitpp::assert_true("GetModalResults(Apply) sets fResult", fResult);
			unitpp::assert_true("GetModalResults(Apply): nothing changed [1]",
				!fStylesChanged);
			unitpp::assert_true("GetModalResults(Apply): nothing changed [2]",
				!pzfwst->m_fReloadDb);

			// Now test that the Apply button is disabled if the role of the selected style
			// is not in the array of applicable styles passed in by the caller.
			pzfwst->SetupForDoModal();
			pasd = pzfwst->m_qafsd;

			// initialize styles from stylesheet
			pasd->CopyToLocal();

			int contexts1[1] = { 0 };
			pzfwst->SetApplicableStyleContexts(contexts1, 1);
			unitpp::assert_true("Normal style (context 0) should be able to applied",
				pasd->CanApplyStyle(0));

			int contexts2[2] = {1, 2};
			pzfwst->SetApplicableStyleContexts(contexts2, 2);
			unitpp::assert_true("Normal style (context 0) should not be able to applied",
				!pasd->CanApplyStyle(0));

			// This is really the Apply button
			pzfwst->GetModalResults(kctidOk);
			unitpp::assert_true("GetModalResults(Apply) sets fApply", pzfwst->m_fApply);
			unitpp::assert_true("GetModalResults(Apply) sets fResult", pzfwst->m_fResult);
			unitpp::assert_true("GetModalResults(Apply): nothing changed [1]",
				!pzfwst->m_fStylesChanged);
			unitpp::assert_true("GetModalResults(Apply): nothing changed [2]",
				!pzfwst->m_fReloadDb);

			qwsf->Shutdown();
		}

	public:
		TestFwStylesDlg();

		virtual void Setup()
		{
			FwCppStylesDlg::CreateCom(NULL, IID_IFwCppStylesDlg, (void **)&m_qfwst);
		}
		virtual void Teardown()
		{
			m_qfwst.Clear();
		}
	};
}

#endif /*TESTFWSTYLESDLG_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
