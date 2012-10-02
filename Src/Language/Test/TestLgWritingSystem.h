/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestLgWritingSystem.h
Responsibility:
Last reviewed:

	Unit tests for the WritingSystem class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLGWRITINGSYSTEM_H_INCLUDED
#define TESTLGWRITINGSYSTEM_H_INCLUDED

#pragma once

#include "testLanguage.h"

namespace TestLanguage
{
	/*******************************************************************************************
		Tests for WritingSystem
	 ******************************************************************************************/
	class TestLgWritingSystem : public unitpp::suite
	{
		IWritingSystemPtr m_qws0;
		ILgWritingSystemFactoryPtr m_qwsf;

		// These two vectors are used in testSaveIfDirty to record files that have been copied,
		// and need to be restored in Teardown.
		Vector<StrUni> m_vstuFile;
		Vector<StrUni> m_vstuBackup;

		void testNullArgs()
		{
			unitpp::assert_true("m_qws0", m_qws0.Ptr());
			HRESULT hr;
			try{
				CheckHr(hr = m_qws0->get_WritingSystem(NULL));
				unitpp::assert_eq("get_WritingSystem(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_WritingSystem(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qws0->get_Name(0, NULL));
				unitpp::assert_eq("get_Name(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_Name(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qws0->put_Name(0, NULL));
			unitpp::assert_eq("put_Name(0, NULL) HRESULT", S_OK, hr);
			try{
				CheckHr(hr = m_qws0->get_Locale(NULL));
				unitpp::assert_eq("get_Locale(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_Locale(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qws0->get_ConverterFrom(0, NULL));
				unitpp::assert_eq("get_ConverterFrom(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_ConverterFrom(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qws0->get_NormalizeEngine(NULL));
				unitpp::assert_eq("get_NormalizeEngine(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_NormalizeEngine(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qws0->get_WordBreakEngine(NULL));
				unitpp::assert_eq("get_WordBreakEngine(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_WordBreakEngine(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qws0->get_SearchEngine(NULL));
				unitpp::assert_eq("get_SearchEngine(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_SearchEngine(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qws0->get_NameWsCount(NULL));
				unitpp::assert_eq("get_NameWsCount(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_NameWsCount(NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qws0->get_NameWss(0, NULL));
			unitpp::assert_eq("get_NameWss(0, NULL) HRESULT", S_OK, hr);	// empty array
			try{
				CheckHr(hr = m_qws0->get_Dirty(NULL));
				unitpp::assert_eq("get_Dirty(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_Dirty(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qws0->get_WritingSystemFactory(NULL));
				unitpp::assert_eq("get_WritingSystemFactory(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_WritingSystemFactory(NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qws0->putref_WritingSystemFactory(NULL)); // allowed!
			unitpp::assert_eq("putref_WritingSystemFactory(NULL) HRESULT", S_OK, hr);

			// Put it back for other tests! this is a suite variable.
			CheckHr(m_qws0->putref_WritingSystemFactory(m_qwsf));

			try{
				CheckHr(hr = m_qws0->WriteAsXml(NULL, 0));
				unitpp::assert_eq("WriteAsXml(NULL, 0) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("WriteAsXml(NULL, 0) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qws0->Serialize(NULL));
				unitpp::assert_eq("Serialize(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Serialize(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qws0->Deserialize(NULL));
				unitpp::assert_eq("Deserialize(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Deserialize(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qws0->get_RightToLeft(NULL));
				unitpp::assert_eq("get_RightToLeft(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_RightToLeft(NULL) HRESULT", E_POINTER, thr.Result());
			}
		}

		void testRightToLeft()
		{
			unitpp::assert_true("m_qws0", m_qws0.Ptr());
			HRESULT hr;
			ComBool fRtoL;
			hr = m_qws0->get_RightToLeft(&fRtoL);
			unitpp::assert_eq("get_RightToLeft(default)", false, fRtoL);
			hr = m_qws0->put_RightToLeft(true);
			unitpp::assert_eq("put_RightToLeft(true) HRESULT", S_OK, hr);
			hr = m_qws0->get_RightToLeft(&fRtoL);
			unitpp::assert_eq("get_RightToLeft(default)", true, fRtoL);
			hr = m_qws0->put_RightToLeft(false);
			unitpp::assert_eq("put_RightToLeft(false) HRESULT", S_OK, hr);
			hr = m_qws0->get_RightToLeft(&fRtoL);
			unitpp::assert_eq("get_RightToLeft(default)", false, fRtoL);
		}

		// THIS IS CRASHING IN GrEngine.cpp line 1136 due to -1 returned as size of
		// some font table, and trying to allocate -1 bytes.
		void Failing_testRenderer()
		{
			HRESULT hr;
			WritingSystemPtr qzwsTest;
			qzwsTest.Attach(NewObj WritingSystem);
			qzwsTest->SetHvo(kwsEng);
			SmartBstr sbstrWs(kszEng);
			hr = qzwsTest->put_IcuLocale(sbstrWs);
			hr = qzwsTest->putref_WritingSystemFactory(m_qwsf);
			IWritingSystemPtr qwsTest;
			hr = qzwsTest->QueryInterface(IID_IWritingSystem, (void **)&qwsTest);

			IRenderEnginePtr qreGraphite;
			IRenderEnginePtr qreGraphiteBold;
			IRenderEnginePtr qreUniscribe;
			IVwGraphicsWin32Ptr qvgW;
			qvgW.CreateInstance(CLSID_VwGraphicsWin32);
			HDC hdc = ::CreateDC(TEXT("DISPLAY"),NULL,NULL,NULL);
			qvgW->Initialize(hdc);

			IVwGraphicsPtr qvg;
			hr = qvgW->QueryInterface(IID_IVwGraphics, (void **) &qvg);
			LgCharRenderProps chrp;
			wcscpy_s(chrp.szFaceName, L"SILDoulos PigLatinDemo");
			chrp.ttvBold = kttvForceOn;
			chrp.ttvItalic = kttvOff;
			qvg->SetupGraphics(&chrp);
			hr = qwsTest->get_Renderer(qvg, & qreGraphite);
			unitpp::assert_true("get renderer, graphite, not null", qreGraphite.Ptr()!= NULL);
			GUID guid;
			hr = qreGraphite->get_ClassId(&guid);
			unitpp::assert_true("get renderer, graphite", guid == CLSID_FwGrEngine);

			// initial test of setting tracing goes here before we make the second engine,
			// so we can both see it set an existing engine and a new one.
			ITraceControlPtr qtc;
			CheckHr(qreGraphite->QueryInterface(IID_ITraceControl, (void **)&qtc));
			int n;
			hr = qtc->GetTracing(&n);
			unitpp::assert_eq("tracing off, default", 0, n);

			hr = qwsTest->SetTracing(1);
			hr = qtc->GetTracing(&n);
			unitpp::assert_eq("tracing on, existing", 1, n);

			ILgWritingSystemFactoryPtr qwsf;
			hr = qreGraphite->get_WritingSystemFactory(&qwsf);
			unitpp::assert_true("graphite renderer has wsf", qwsf.Ptr() != NULL);

			IRenderEnginePtr qre2;
			hr = qwsTest->get_Renderer(qvg, & qre2);
			unitpp::assert_true("same props produce same graphite engine",
				qreGraphite.Ptr()== qre2.Ptr());

			chrp.ttvBold = kttvOff;
			qvg->SetupGraphics(&chrp);
			hr = qwsTest->get_Renderer(qvg, & qreGraphiteBold);
			unitpp::assert_true("bold produces different instance of graphite engine",
				qreGraphite.Ptr()!= qreGraphiteBold.Ptr());
			hr = qreGraphiteBold->get_ClassId(&guid);
			unitpp::assert_true("type of 2nd graphite renderer", guid == CLSID_FwGrEngine);

			CheckHr(qreGraphiteBold->QueryInterface(IID_ITraceControl, (void **)&qtc));
			hr = qtc->GetTracing(&n);
			unitpp::assert_eq("tracing on, new", 1, n);

			wcscpy_s(chrp.szFaceName, L"Times New Roman");
			chrp.ttvBold = kttvOff;
			chrp.ttvItalic = kttvForceOn;
			qvg->SetupGraphics(&chrp);
			hr = qwsTest->get_Renderer(qvg, & qreUniscribe);
			unitpp::assert_true("get renderer, not graphite, not null",
				qreUniscribe.Ptr()!= NULL);
			hr = qreUniscribe->get_ClassId(&guid);
			unitpp::assert_true("get renderer, not graphite", guid == CLSID_UniscribeEngine);

			hr = qreUniscribe->get_WritingSystemFactory(&qwsf);
			unitpp::assert_true("Uniscribe renderer has wsf", qwsf.Ptr() != NULL);

			wcscpy_s(chrp.szFaceName, L"Arial");
			qvg->SetupGraphics(&chrp);
			hr = qwsTest->get_Renderer(qvg, & qre2);
			unitpp::assert_true("Different font produces same Uniscribe renderer",
				guid == CLSID_UniscribeEngine);

			// Test the method that changes the writing system; should affect all renderers
			LgWritingSystemFactoryPtr qzwsf;
			qzwsf.Attach(NewObj LgWritingSystemFactory);
			ILgWritingSystemFactoryPtr qwsfRep;
			qzwsf->QueryInterface(IID_ILgWritingSystemFactory, (void **)&qwsfRep);
			hr = qwsTest->putref_WritingSystemFactory(qwsfRep);
			ILgWritingSystemFactoryPtr qwsf2;
			hr = qwsTest->get_WritingSystemFactory(&qwsf2);
			unitpp::assert_true("Change writing system", qwsfRep.Ptr() == qwsf2.Ptr());

			hr = qreGraphite->get_WritingSystemFactory(&qwsf2);
			unitpp::assert_true("1st Graphite renderer fact modified",
				qwsfRep.Ptr() == qwsf2.Ptr());
			hr = qreGraphiteBold->get_WritingSystemFactory(&qwsf2);
			unitpp::assert_true("2nd Graphite renderer fact modified",
				qwsfRep.Ptr() == qwsf2.Ptr());
			hr = qreUniscribe->get_WritingSystemFactory(&qwsf2);
			unitpp::assert_true("Uniscribe renderer fact modified",
				qwsfRep.Ptr() == qwsf2.Ptr());

			qwsfRep->Shutdown();
			qwsf->Shutdown();
			qwsf2->Shutdown();
			qvgW.Clear();
			qvg.Clear();

			::DeleteDC(hdc);
		}

		void testFontVar()
		{
			unitpp::assert_true("m_qws0", m_qws0.Ptr());
			HRESULT hr;
			SmartBstr sbstr;
			hr = m_qws0->get_FontVariation(&sbstr);
			unitpp::assert_true("get font var, not set", sbstr.Length() == 0);
			sbstr = L"Dummy";
			hr = m_qws0->put_FontVariation(sbstr.Bstr());
			unitpp::assert_eq("put_FontVariation('Dummy') HRESULT", S_OK, hr);
			SmartBstr sbstr2;
			hr = m_qws0->get_FontVariation(&sbstr2);
			unitpp::assert_true("set and get font var", wcscmp(sbstr2.Chars(), L"Dummy") == 0);
			hr = m_qws0->get_SansFontVariation(&sbstr);
			unitpp::assert_true("get sans font var, not set", sbstr.Length() == 0);
			sbstr = L"Dummy";
			hr = m_qws0->put_SansFontVariation(sbstr.Bstr());
			unitpp::assert_eq("put_SansFontVariation('Dummy') HRESULT", S_OK, hr);
			hr = m_qws0->get_SansFontVariation(&sbstr2);
			unitpp::assert_true("set and get sans font var",
				wcscmp(sbstr2.Chars(), L"Dummy") == 0);
		}

		void testDefaultSerif()
		{
			unitpp::assert_true("m_qws0", m_qws0.Ptr());
			HRESULT hr;
			SmartBstr sbstr;
			hr = m_qws0->get_DefaultSerif(&sbstr);
			unitpp::assert_true("get def serif, not set",
				wcscmp(sbstr.Chars(), L"Times New Roman") == 0);
			sbstr = L"TIMES NEW ROMAN";
			hr = m_qws0->put_DefaultSerif(sbstr.Bstr());
			unitpp::assert_eq("put_DefaultSerif('TIMES NEW ROMAN') HRESULT", S_OK, hr);
			SmartBstr sbstr2;
			hr = m_qws0->get_DefaultSerif(&sbstr2);
			unitpp::assert_true("set and get def serif",
				wcscmp(sbstr2.Chars(), L"TIMES NEW ROMAN") == 0);
		}

		void testDefaultSansSerif()
		{
			unitpp::assert_true("m_qws0", m_qws0.Ptr());
			HRESULT hr;
			SmartBstr sbstr;
			hr = m_qws0->get_DefaultSansSerif(&sbstr);
			unitpp::assert_true("get def sans serif, not set",
				wcscmp(sbstr.Chars(), L"Arial") == 0);
			sbstr = L"ARIAL";
			hr = m_qws0->put_DefaultSansSerif(sbstr.Bstr());
			unitpp::assert_eq("put_DefaultSansSerif('ARIAL') HRESULT", S_OK, hr);
			SmartBstr sbstr2;
			hr = m_qws0->get_DefaultSansSerif(&sbstr2);
			unitpp::assert_true("set and get def sans serif",
				wcscmp(sbstr2.Chars(), L"ARIAL") == 0);
		}

		void testDefaultBodyFont()
		{
			unitpp::assert_true("m_qws0", m_qws0.Ptr());
			HRESULT hr;
			SmartBstr sbstr;
			hr = m_qws0->get_DefaultBodyFont(&sbstr);
			unitpp::assert_true("get def body font, not set",
				wcscmp(sbstr.Chars(), L"Charis SIL") == 0);
			sbstr = L"CHARIS";
			hr = m_qws0->put_DefaultBodyFont(sbstr.Bstr());
			unitpp::assert_eq("put_DefaultBodyFont('CHARIS') HRESULT", S_OK, hr);
			SmartBstr sbstr2;
			hr = m_qws0->get_DefaultBodyFont(&sbstr2);
			unitpp::assert_true("set and get body font",
				wcscmp(sbstr2.Chars(), L"CHARIS") == 0);
		}

		void testDefaultMonospace()
		{
			unitpp::assert_true("m_qws0", m_qws0.Ptr());
			HRESULT hr;
			SmartBstr sbstr;
			hr = m_qws0->get_DefaultMonospace(&sbstr);
			unitpp::assert_true("get def Monospace, not set",
				wcscmp(sbstr.Chars(), L"Courier") == 0);
			sbstr = L"COURIER";
			hr = m_qws0->put_DefaultMonospace(sbstr.Bstr());
			unitpp::assert_eq("put_DefaultMonospace('COURIER') HRESULT", S_OK, hr);
			SmartBstr sbstr2;
			hr = m_qws0->get_DefaultMonospace(&sbstr2);
			unitpp::assert_true("set and get def COURIER",
				wcscmp(sbstr2.Chars(), L"COURIER") == 0);
		}

		void testKeyMan()
		{
			unitpp::assert_true("m_qws0", m_qws0.Ptr());
			HRESULT hr;
			ComBool fKeyMan;
			hr = m_qws0->get_KeyMan(&fKeyMan);
			unitpp::assert_eq("get KeyMan not set", false, fKeyMan);

			hr = m_qws0->put_KeyMan(true);
			unitpp::assert_eq("put_KeyMan(true) HRESULT", S_OK, hr);
			ComBool fResult;
			hr = m_qws0->get_KeyMan(&fResult);
			unitpp::assert_eq("put and get KeyMan", true, fResult);
		}

		void testUiName()
		{
			unitpp::assert_true("m_qws0", m_qws0.Ptr());
			HRESULT hr;
			SmartBstr sbstr;
			SmartBstr sbstrIcu;
			SmartBstr sbstrName;
			hr = m_qws0->get_IcuLocale(&sbstrIcu);
			hr = m_qws0->get_UiName(kwsEng, &sbstrName);
			unitpp::assert_true("get_UiName, return English",
				!wcscmp(sbstrName.Chars(), L"English"));
			sbstr = L"MyWs";
			hr = m_qws0->put_IcuLocale(sbstr); // set to illegal locale
			hr = m_qws0->get_UiName(kwsEng, &sbstr);
			unitpp::assert_true("get_UiName", wcscmp(sbstr.Chars(), L"MyWs") == 0);
			hr = m_qws0->put_IcuLocale(sbstrIcu); // restore locale
			hr = m_qws0->put_Name(kwsEng, sbstrName.Bstr()); // Store English name
			unitpp::assert_eq("put_Name(kwsTest, 'MyWs') HRESULT", S_OK, hr);
		}

		// Test the serialization of the member variables that used to be in OldWritingSystem
		// and those added since.
		// Legacy data from WS original is not (yet) tested.
		void testSerialize()
		{
			unitpp::assert_true("m_qws0", m_qws0.Ptr());
			CheckHr(CreateTestWritingSystem(m_qwsf, kwsTest, kszTest));

			CheckHr(m_qws0->put_RightToLeft(true));

			SmartBstr sbstr = L"Dummy";
			CheckHr(m_qws0->put_FontVariation(sbstr.Bstr()));
			CheckHr(m_qws0->put_SansFontVariation(sbstr.Bstr()));

			sbstr = L"Times New Roman";
			CheckHr(m_qws0->put_DefaultSerif(sbstr.Bstr()));

			sbstr = L"Arial";
			CheckHr(m_qws0->put_DefaultSansSerif(sbstr.Bstr()));

			sbstr = L"Charis SIL";
			CheckHr(m_qws0->put_DefaultBodyFont(sbstr.Bstr()));

			sbstr = L"Courier";
			CheckHr(m_qws0->put_DefaultMonospace(sbstr.Bstr()));

			CheckHr(m_qws0->put_KeyMan(true));

			sbstr = L"MyKeyboard";
			CheckHr(m_qws0->put_KeymanKbdName(sbstr.Bstr()));

			sbstr = L"MyWs";
			CheckHr(m_qws0->put_Name(kwsTest, sbstr.Bstr()));

			sbstr = L"TST";
			CheckHr(m_qws0->put_Abbr(kwsEng, sbstr.Bstr()));

			sbstr = L"Some Description";
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			ITsStringPtr qtss;
			CheckHr(qtsf->MakeStringRgch(sbstr.Chars(), sbstr.Length(), kwsEng, &qtss));
			CheckHr(m_qws0->put_Description(kwsEng, qtss));

			int ccoll;
			CollationPtr qzcoll;
			qzcoll.Attach(NewObj Collation);
//BAD		qcoll.CreateInstance(CLSID_Collation);
			sbstr = L"My Collation";
			CheckHr(qzcoll->put_Name(kwsEng, sbstr));
			int nCollLCID = 567;
			CheckHr(qzcoll->put_WinLCID(nCollLCID));
			sbstr = L"English_Collation_String";
			CheckHr(qzcoll->put_WinCollation(sbstr));
			CheckHr(m_qws0->get_CollationCount(&ccoll));
			unitpp::assert_eq("One default collation", 1, ccoll);
			HRESULT hr;
			CheckHr(hr = m_qws0->putref_Collation(0, qzcoll));
			unitpp::assert_eq("Replace default collation HRESULT", S_OK, hr);
			CheckHr(m_qws0->get_CollationCount(&ccoll));
			unitpp::assert_eq("Still exactly one collation", 1, ccoll);

			// Serialize m_qws0. First we need an implementation of IStorage to write it to.
			IStoragePtr qstg;
			CheckHr(::StgCreateStorageEx(
				NULL, // null file name causes allocation of temporary file.
				STGM_CREATE|STGM_SHARE_EXCLUSIVE|STGM_READWRITE,
				STGFMT_STORAGE, // ordinary doc file
				0, // required value
				NULL, // default options
				0, // reserved value must be 0
				IID_IStorage, // desired interface type returned.
				(void **)&qstg
			));
			CheckHr(hr = m_qws0->Serialize(qstg));
			unitpp::assert_eq("Serialize HRESULT", S_OK, hr);

			// Deserialize qwsOut.
			IWritingSystemPtr qwsOut;
			WritingSystem::CreateCom(NULL, IID_IWritingSystem, (void **)&qwsOut);
			CheckHr(hr = qwsOut->Deserialize(qstg));
			unitpp::assert_eq("Deserialize HRESULT", S_OK, hr);
			ILgWritingSystemFactoryPtr qwsf;
			CheckHr(qwsOut->putref_WritingSystemFactory(m_qwsf));

			// Check that we can retrieve from the output everything we wrote to the input.
			ComBool fRtoL;
			CheckHr(qwsOut->get_RightToLeft(&fRtoL));
			unitpp::assert_eq("get_RightToLeft(default)", true, fRtoL);

			SmartBstr sbstr2;
			CheckHr(qwsOut->get_FontVariation(&sbstr2));
			unitpp::assert_true("set and get font var", wcscmp(sbstr2.Chars(), L"Dummy") == 0);
			CheckHr(qwsOut->get_SansFontVariation(&sbstr2));
			unitpp::assert_true("set and get sans font var",
				wcscmp(sbstr2.Chars(), L"Dummy") == 0);

			CheckHr(qwsOut->get_DefaultSerif(&sbstr2));
			unitpp::assert_true("set and get def serif",
				wcscmp(sbstr2.Chars(), L"Times New Roman") == 0);

			CheckHr(qwsOut->get_DefaultSansSerif(&sbstr2));
			unitpp::assert_true("set and get def sans serif",
				wcscmp(sbstr2.Chars(), L"Arial") == 0);

			CheckHr(qwsOut->get_DefaultBodyFont(&sbstr2));
			unitpp::assert_true("set and get def body font",
				wcscmp(sbstr2.Chars(), L"Charis SIL") == 0);

			CheckHr(qwsOut->get_DefaultMonospace(&sbstr2));
			unitpp::assert_true("set and get def courier",
				wcscmp(sbstr2.Chars(), L"Courier") == 0);

			ComBool fResult;
			CheckHr(qwsOut->get_KeyMan(&fResult));
			unitpp::assert_eq("put and get KeyMan", true, fResult);

			CheckHr(qwsOut->get_KeymanKbdName(&sbstr2));
			unitpp::assert_true("get_KeymanKbdName",
				wcscmp(sbstr2.Chars(), L"MyKeyboard") == 0);

			CheckHr(qwsOut->get_UiName(kwsEng, &sbstr2));
			unitpp::assert_true("get_UiName", wcscmp(sbstr2.Chars(), L"English") == 0);

			CheckHr(qwsOut->get_Abbr(kwsEng, &sbstr2));
			unitpp::assert_true("deserialize Abbr", wcscmp(sbstr2.Chars(), L"TST") == 0);

			ITsStringPtr qtss2;
			CheckHr(hr = qwsOut->get_Description(kwsEng, &qtss2));
			unitpp::assert_eq("retrieve Desc from deserialize: hr", S_OK, hr);
			unitpp::assert_true("retrieve Desc from deserialize: qtss", qtss2.Ptr());
			qtss2->get_Text(&sbstr2);
			unitpp::assert_true("retrieve Desc from deserialize",
				wcscmp(sbstr2.Chars(), L"Some Description") == 0);

			ICollationPtr qcoll2;
			CheckHr(qwsOut->get_CollationCount(&ccoll));
			unitpp::assert_eq("retrieve collation count from deserialize", 1, ccoll);
			CheckHr(qwsOut->get_Collation(0, &qcoll2));
			unitpp::assert_true("get non-null collation from deserialize",
				qcoll2.Ptr() != NULL);
			CheckHr(qcoll2->get_Name(1, &sbstr2));
			unitpp::assert_eq("empty collation name for bogus ws from deserialize",
				0, sbstr2.Length());
			CheckHr(qcoll2->get_Name(kwsEng, &sbstr2));
			unitpp::assert_true("deserialize collation name",
				wcscmp(sbstr2.Chars(), L"My Collation") == 0);
			int cwsColl;
			CheckHr(qcoll2->get_NameWsCount(&cwsColl));
			unitpp::assert_eq("collation name ws count from deserialize", 1, cwsColl);
			int rgws[3];
			CheckHr(qcoll2->get_NameWss(2, rgws));
			unitpp::assert_eq("collation name wses from deserialize", kwsEng, rgws[0]);
			unitpp::assert_eq("collation name wses from deserialize", 0, rgws[1]);
			int nCollLCID2;
			CheckHr(qcoll2->get_WinLCID(&nCollLCID2));
			unitpp::assert_eq("collation WinLCID from deserialize", nCollLCID, nCollLCID2);
			CheckHr(qcoll2->get_WinCollation(&sbstr2));
			unitpp::assert_true("retrieve WinCollation from deserialize",
				wcscmp(sbstr2.Chars(), L"English_Collation_String") == 0);
		}

		void testIcuLocale()
		{
			HRESULT hr;
			const int kwsX = 77;
			const wchar kszX[] = L"en_Latn_GB_EURO";
			const wchar kszX_Lang[] = L"en";
			const wchar kszX_Script[] = L"Latn";
			const wchar kszX_Country[] = L"GB";
			const wchar kszX_Var[] = L"EURO";
			const wchar kszX_FullName[] = L"English (Latin, United Kingdom, EURO)";
			const wchar kszX_LangName[] = L"English";
			const wchar kszX_ScriptName[] = L"Latin";
			const wchar kszX_CountryName[] = L"United Kingdom";
			const wchar kszX_VarName[] = L"EURO";
			const int kwsY = 777;
			const wchar kszY[] = L"fr__EURO";
			const wchar kszY_Lang[] = L"fr";
			const wchar kszY_Script[] = L"";
			const wchar kszY_Country[] = L"";
			const wchar kszY_Var[] = L"EURO";

			ILgWritingSystemFactoryPtr qwsf;
			CreateTestWritingSystemFactory(&qwsf);
			hr = CreateTestWritingSystem(qwsf, kwsTest, kszTest);
			hr = CreateTestWritingSystem(qwsf, kwsTest2, kszTest2);
			hr = CreateTestWritingSystem(qwsf, kwsX, kszX);
			hr = CreateTestWritingSystem(qwsf, kwsY, kszY);

			IWritingSystemPtr qws;
			SmartBstr sbstrLoc;
			SmartBstr sbstrLang;
			SmartBstr sbstrScript;
			SmartBstr sbstrCountry;
			SmartBstr sbstrVar;

			hr = qwsf->get_EngineOrNull(kwsEng, &qws);
			unitpp::assert_eq("get_EngineOrNull(kwsEng) HRESULT", S_OK, hr);
			unitpp::assert_true("get_EngineOrNull(kwsEng)", qws.Ptr());
			hr = qws->get_IcuLocale(&sbstrLoc);
			unitpp::assert_eq("get ICU Locale of English HRESULT", S_OK, hr);
			unitpp::assert_true("get ICU Locale of English",
				wcscmp(sbstrLoc.Chars(), kszEng) == 0);
			hr = qws->GetIcuLocaleParts(&sbstrLang, &sbstrScript, &sbstrCountry, &sbstrVar);
			unitpp::assert_eq("GetIcuLocaleParts of English: HRESULT", S_OK, hr);
			unitpp::assert_true("GetIcuLocaleParts of English: Lang",
				wcscmp(sbstrLang.Chars(), kszEng) == 0);
			unitpp::assert_true("GetIcuLocaleParts of English: Script",
				wcscmp(sbstrScript.Chars(), L"") == 0);
			unitpp::assert_true("GetIcuLocaleParts of English: Country",
				wcscmp(sbstrCountry.Chars(), L"") == 0);
			unitpp::assert_true("GetIcuLocaleParts of English: Var",
				wcscmp(sbstrVar.Chars(), L"") == 0);

			hr = qwsf->get_EngineOrNull(kwsX, &qws);
			unitpp::assert_eq("get_EngineOrNull(kwsX) HRESULT", S_OK, hr);
			unitpp::assert_true("get_EngineOrNull(kwsX)", qws.Ptr());
			hr = qws->get_IcuLocale(&sbstrLoc);
			unitpp::assert_eq("get ICU Locale of X HRESULT", S_OK, hr);
			unitpp::assert_true("get ICU Locale of X",
				wcscmp(sbstrLoc.Chars(), kszX) == 0);
			hr = qws->GetIcuLocaleParts(&sbstrLang, &sbstrScript, &sbstrCountry, &sbstrVar);
			unitpp::assert_eq("GetIcuLocaleParts of X: HRESULT", S_OK, hr);
			unitpp::assert_true("GetIcuLocaleParts of X: Lang",
				wcscmp(sbstrLang.Chars(), kszX_Lang) == 0);
			unitpp::assert_true("GetIcuLocaleParts of X: Script",
				wcscmp(sbstrScript.Chars(), kszX_Script) == 0);
			unitpp::assert_true("GetIcuLocaleParts of X: Country",
				wcscmp(sbstrCountry.Chars(), kszX_Country) == 0);
			unitpp::assert_true("GetIcuLocaleParts of X: Var",
				wcscmp(sbstrVar.Chars(), kszX_Var) == 0);
			SmartBstr sbstr;
			hr = qws->get_UiName(kwsEng, &sbstr);
			unitpp::assert_eq("get UiName of X HRESULT", S_OK, hr);
			unitpp::assert_true("get UiName of X",
				wcscmp(sbstr.Chars(), kszX_FullName) == 0);
			hr = qws->get_LanguageName(&sbstr);
			unitpp::assert_eq("get LanguageName of X HRESULT", S_OK, hr);
			unitpp::assert_true("get LanguageName of X",
				wcscmp(sbstr.Chars(), kszX_LangName) == 0);
			hr = qws->get_ScriptName(&sbstr);
			unitpp::assert_eq("get ScriptName of X HRESULT", S_OK, hr);
			unitpp::assert_true("get ScriptName of X",
				wcscmp(sbstr.Chars(), kszX_ScriptName) == 0);
			hr = qws->get_CountryName(&sbstr);
			unitpp::assert_eq("get CountryName of X HRESULT", S_OK, hr);
			unitpp::assert_true("get CountryName of X",
				wcscmp(sbstr.Chars(), kszX_CountryName) == 0);
			hr = qws->get_VariantName(&sbstr);
			unitpp::assert_eq("get VariantName of X HRESULT", S_OK, hr);
			unitpp::assert_true("get VariantName of X",
				wcscmp(sbstr.Chars(), kszX_VarName) == 0);
			hr = qws->get_LanguageAbbr(&sbstr);
			unitpp::assert_eq("get LanguageAbbr of X HRESULT", S_OK, hr);
			unitpp::assert_true("get LanguageAbbr of X",
				wcscmp(sbstr.Chars(), kszX_Lang) == 0);
			hr = qws->get_ScriptAbbr(&sbstr);
			unitpp::assert_eq("get ScriptAbbr of X HRESULT", S_OK, hr);
			unitpp::assert_true("get ScriptAbbr of X",
				wcscmp(sbstr.Chars(), kszX_Script) == 0);
			hr = qws->get_CountryAbbr(&sbstr);
			unitpp::assert_eq("get CountryAbbr of X HRESULT", S_OK, hr);
			unitpp::assert_true("get CountryAbbr of X",
				wcscmp(sbstr.Chars(), kszX_Country) == 0);
			hr = qws->get_VariantAbbr(&sbstr);
			unitpp::assert_eq("get VariantAbbr of X HRESULT", S_OK, hr);
			unitpp::assert_true("get VariantAbbr of X",
				wcscmp(sbstr.Chars(), kszX_Var) == 0);
			hr = qwsf->get_EngineOrNull(kwsY, &qws);
			unitpp::assert_eq("get_EngineOrNull(kwsY) HRESULT", S_OK, hr);
			unitpp::assert_true("get_EngineOrNull(kwsY)", qws.Ptr());
			hr = qws->get_IcuLocale(&sbstrLoc);
			unitpp::assert_eq("get ICU Locale of Y HRESULT", S_OK, hr);
			unitpp::assert_true("get ICU Locale of Y",
				wcscmp(sbstrLoc.Chars(), kszY) == 0);
			hr = qws->GetIcuLocaleParts(&sbstrLang, &sbstrScript, &sbstrCountry, &sbstrVar);
			unitpp::assert_eq("GetIcuLocaleParts of Y: HRESULT", S_OK, hr);
			unitpp::assert_true("GetIcuLocaleParts of Y: Lang",
				wcscmp(sbstrLang.Chars(), kszY_Lang) == 0);
			unitpp::assert_true("GetIcuLocaleParts of Y: Script",
				wcscmp(sbstrScript.Chars(), kszY_Script) == 0);
			unitpp::assert_true("GetIcuLocaleParts of Y: Country",
				wcscmp(sbstrCountry.Chars(), kszY_Country) == 0);
			unitpp::assert_true("GetIcuLocaleParts of Y: Var",
				wcscmp(sbstrVar.Chars(), kszY_Var) == 0);

			SmartBstr sbstrNew(L"fr_GB");
			hr = qws->put_IcuLocale(sbstrNew);
			unitpp::assert_eq("put ICU Locale HRESULT", S_OK, hr);
			hr = qws->GetIcuLocaleParts(&sbstrLang, &sbstrScript, &sbstrCountry, &sbstrVar);
			unitpp::assert_eq("GetIcuLocaleParts of changed Y: HRESULT", S_OK, hr);
			unitpp::assert_true("GetIcuLocaleParts of changed Y: Lang",
				wcscmp(sbstrLang.Chars(), L"fr") == 0);
			unitpp::assert_true("GetIcuLocaleParts of changed Y: Script",
				wcscmp(sbstrScript.Chars(), L"") == 0);
			unitpp::assert_true("GetIcuLocaleParts of changed Y: Country",
				wcscmp(sbstrCountry.Chars(), L"GB") == 0);
			unitpp::assert_true("GetIcuLocaleParts of Y: Var",
				wcscmp(sbstrVar.Chars(), L"") == 0);
			hr = qws->get_IcuLocale(&sbstrLoc);
			unitpp::assert_eq("get ICU Locale of changed Y HRESULT", S_OK, hr);
			unitpp::assert_true("get ICU Locale of changed Y",
				wcscmp(sbstrLoc.Chars(), sbstrNew.Chars()) == 0);

			qwsf->Shutdown();
			qwsf.Clear();
		}

		// Todo JohnT: The enhanced WriteAsXml is not yet tested. When we do the WorldPad XML
		// rework, we should (a) move the XML reading code here; (b) deal with backward
		// compatibility; and (c) test the new writer and both kinds of reader.

		void testSaveIfDirty()
		{
			// Since the writing system factory is not connected to a database, the only effect
			// of saving the writing systems is to create any missing Language Definition Files
			// in the {FW}/Languages subdirectory and installing those languages.

			// 1. Make backup copies of {FW}/icu/icudt26l_root.res,
			// {FW}/icu/icudt26l_res_index.res, {FW}/icu/data/locales/root.txt, and
			// {FW}/icu/data/locales/res_index.txt.

			StrUni stuLangDir(DirectoryFinder::FwRootDataDir());
			stuLangDir.Append(L"\\Languages");
			StrUni stuIcuDir(DirectoryFinder::IcuDir());
			StrUni stuIcuDataDir(DirectoryFinder::IcuDataDir());
			StrUni stuDataDir(stuIcuDir);
			stuDataDir.Append(L"\\data\\locales");

			HRESULT hr;
			StrAnsi sta;
			StrUni stuFile;
			StrUni stuBak;
			BOOL fOk;

			// Don't fail if backup already there as this might be a left-over from
			// a previous failure.
			stuFile.Format(L"%s\\root.res", stuIcuDataDir.Chars());
			stuBak.Format(L"%s\\BACKUP-root.res-BACKUP", stuIcuDataDir.Chars());
			fOk = ::CopyFileW(stuFile.Chars(), stuBak.Chars(), false);
			StrAnsi staMsg;
			staMsg.Format("backed up %S", stuFile.Chars());
			unitpp::assert_true(staMsg.Chars(), fOk);
			m_vstuFile.Push(stuFile);
			m_vstuBackup.Push(stuBak);

			stuFile.Format(L"%s\\res_index.res", stuIcuDataDir.Chars());
			stuBak.Format(L"%s\\BACKUP-res_index.res-BACKUP", stuIcuDataDir.Chars());
			fOk = ::CopyFileW(stuFile.Chars(), stuBak.Chars(), false);
			staMsg.Format("backed up %S", stuFile.Chars());
			unitpp::assert_true(staMsg.Chars(), fOk);
			m_vstuFile.Push(stuFile);
			m_vstuBackup.Push(stuBak);

			stuFile.Format(L"%s\\root.txt", stuDataDir.Chars());
			stuBak.Format(L"%s\\BACKUP-root.txt-BACKUP", stuDataDir.Chars());
			fOk = ::CopyFileW(stuFile.Chars(), stuBak.Chars(), false);
			staMsg.Format("backed up %S", stuFile.Chars());
			unitpp::assert_true(staMsg.Chars(), fOk);
			m_vstuFile.Push(stuFile);
			m_vstuBackup.Push(stuBak);

			stuFile.Format(L"%s\\res_index.txt", stuDataDir.Chars());
			stuBak.Format(L"%s\\BACKUP-res_index.txt-BACKUP", stuDataDir.Chars());
			fOk = ::CopyFileW(stuFile.Chars(), stuBak.Chars(), false);
			staMsg.Format("backed up %S", stuFile.Chars());
			unitpp::assert_true(staMsg.Chars(), fOk);
			m_vstuFile.Push(stuFile);
			m_vstuBackup.Push(stuBak);

#ifdef HAVE_FULL_SET_OF_LDF_FILES
			// 2. enumerate the writing systems.

			int cws;
			Vector<int> vws;
			hr = m_qwsf->get_NumberOfWs(&cws);
			vws.Resize(cws);
			hr = m_qwsf->GetWritingSystems(vws.Begin(), cws);

			// 3. check which (if any) writing systems already have an LDF.

			Vector<StrUni> vstuWs;
			Vector<StrUni> vstuLDF;
			Vector<bool> vfOldLDF;
			StrUni stu;
			int iws;
			for (iws = 0; iws < cws; ++iws)
			{
				SmartBstr sbstr;
				hr = m_qwsf->GetStrFromWs(vws[iws], &sbstr);
				stu.Assign(sbstr.Chars(), sbstr.Length());
				vstuWs.Push(stu);
				stu.Format(L"%s\\%s.xml", stuLangDir.Chars(), sbstr);
				bool fLDFExists = false;
				WIN32_FIND_DATA wfd;
				HANDLE hFind = ::FindFirstFileW(stu.Chars(), &wfd);
				if (hFind != INVALID_HANDLE_VALUE)
				{
					::FindClose(hFind);
					fLDFExists = true;
				}
				vstuLDF.Push(stu);
				vfOldLDF.Push(fLDFExists);
			}

			// 4. save all the writing systems.  This calls SaveIfDirty internally.

			hr = m_qwsf->SaveWritingSystems();
			unitpp::assert_eq("m_qwsf->SaveWritingSystems() HRESULT", S_OK, hr);

			// 5. check which writing systems now have an LDF: all of them should.

			for (iws = 0; iws < cws; ++iws)
			{
				bool fLDFExists = false;
				WIN32_FIND_DATA wfd;
				HANDLE hFind = ::FindFirstFileW(vstuLDF[iws].Chars(), &wfd);
				if (hFind != INVALID_HANDLE_VALUE)
				{
					::FindClose(hFind);
					fLDFExists = true;
				}
				sta.Format("LDF File %S exists", vstuLDF[iws].Chars());
				unitpp::assert_true(sta.Chars(), fLDFExists);
			}

			// 6. Delete any newly created Language Definition to clean up for another test
			//    later.

			for (iws = 0; iws < cws; ++iws)
			{
				if (!vfOldLDF[iws])
				{
					// Delete newly created Languages/xx.xml (LDF) file.
					fOk = ::DeleteFileW(vstuLDF[iws].Chars());
					sta.Format("deleted LDF file %S okay", vstuLDF[iws].Chars());
					unitpp::assert_true(sta.Chars(), fOk);
					// Delete newly created Icu/icudt26l_xx.res file.
					stuFile.Format(L"%s\\%s.res", stuIcuDataDir.Chars(), vstuWs[iws].Chars());
					fOk = ::DeleteFileW(stuFile.Chars());
					sta.Format("deleted ICU resource file %S okay", stuFile.Chars());
					unitpp::assert_true(sta.Chars(), fOk);
					// Delete newly created Icu/data/locales/xx.txt file.
					stuFile.Format(L"%s\\%s.txt", stuDataDir.Chars(), vstuWs[iws].Chars());
					fOk = ::DeleteFileW(stuFile.Chars());
					sta.Format("deleted ICU locale file %S okay", stuFile.Chars());
					unitpp::assert_true(sta.Chars(), fOk);
				}
			}
#else
			// 2. save the dummy writing system.

			IWritingSystemPtr qws;
			SmartBstr sbstrWs(L"test");
			hr = m_qwsf->get_Engine(sbstrWs, &qws);
			unitpp::assert_eq("m_qwsf->get_Engine(\"test\",...) HRESULT", S_OK, hr);
			hr = qws->SaveIfDirty(NULL);
			unitpp::assert_eq("[\"test\"] qws->SaveIfDirty() HRESULT", S_OK, hr);

			// 3. check that the test writing system now has an LDF.

			bool fLDFExists = false;
			WIN32_FIND_DATA wfd;
			stuFile.Format(L"%s\\test.xml", stuLangDir.Chars());
			HANDLE hFind = ::FindFirstFileW(stuFile.Chars(), &wfd);
			if (hFind != INVALID_HANDLE_VALUE)
			{
				::FindClose(hFind);
				fLDFExists = true;
			}
			sta.Format("LDF File %S exists", stuFile.Chars());
			unitpp::assert_true(sta.Chars(), fLDFExists);

			// 4. delete the newly created Language Definition to clean up for another test
			//    later.

			// Delete newly created Languages/test.xml (LDF) file.
			stuFile.Format(L"%s\\test.xml", stuLangDir.Chars());
			fOk = ::DeleteFileW(stuFile.Chars());
			sta.Format("deleted LDF file %S okay", stuFile.Chars());
			unitpp::assert_true(sta.Chars(), fOk);

			// Delete newly created Icu/data/locales/test.txt file.
			stuFile.Format(L"%s\\test.txt", stuDataDir.Chars());
			fOk = ::DeleteFileW(stuFile.Chars());
			sta.Format("deleted ICU locale file %S okay", stuFile.Chars());
			unitpp::assert_true(sta.Chars(), fOk);

			// Delete newly created Icu/icudt26l_test.res file.
			stuFile.Format(L"%s\\test.res", stuIcuDataDir.Chars());
			fOk = ::DeleteFileW(stuFile.Chars());
			sta.Format("deleted ICU resource file %S okay", stuFile.Chars());
			unitpp::assert_true(sta.Chars(), fOk);
#endif
		}

	public:
		TestLgWritingSystem();

		virtual void Teardown()
		{
			// Restore the original files, replacing them with the backups.
			if (m_vstuFile.Size())
			{
				for (int i = 0; i < m_vstuFile.Size(); ++i)
					::MoveFileExW(m_vstuBackup[i].Chars(), m_vstuFile[i].Chars(),
						MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH);
				m_vstuBackup.Clear();
				m_vstuFile.Clear();
			}
		}

		virtual void SuiteSetup()
		{
			CreateTestWritingSystemFactory(&m_qwsf);
			if (m_qwsf)
				m_qwsf->get_EngineOrNull(kwsEng, &m_qws0);
		}
		virtual void SuiteTeardown()
		{
			m_qwsf->Shutdown();
			m_qws0.Clear();
			m_qwsf.Clear();
		}

	};
}

#endif /*TESTLGWRITINGSYSTEM_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
