/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: RenderEngineTestBase.h
Responsibility:
Last reviewed:

	Unit tests for the Rendering Engine classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef RenderEngineTestBase_H_INCLUDED
#define RenderEngineTestBase_H_INCLUDED

#pragma once

#include "testFwKernel.h"

#ifndef WIN32 // on Linux - symbols for for methods of Vector<int> - This include adds them into testLanguage
#include "Vector_i.cpp"
#endif

namespace TestFwKernel
{
	// For error reporting:
	static DummyFactory g_fact(_T("SIL.TestFwKernel.TxtSrc"));

	/*******************************************************************************************
		Mock object class for TestUniscribeEngine::testBreakPointing().
	 ******************************************************************************************/
	class TxtSrc : public IVwTextSource
	{
	public:
		TxtSrc(int n, ILgWritingSystemFactory * pwsf);

		// IUnknown methods.
		STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
		STDMETHOD_(UCOMINT32, AddRef)(void)
		{
			return InterlockedIncrement(&m_cref);
		}
		STDMETHOD_(UCOMINT32, Release)(void)
		{
			long cref = InterlockedDecrement(&m_cref);
			if (cref == 0)
			{
				m_cref = 1;
				delete this;
			}
			return cref;
		}

		// IVwTextSource methods
		STDMETHOD(Fetch)(int ichMin, int ichLim, OLECHAR * prgchBuf);
		STDMETHOD(get_Length)(int * pcch);
		STDMETHODIMP FetchSearch(int ichMin, int ichLim, OLECHAR * prgchBuf) {return E_NOTIMPL;}
		STDMETHODIMP get_LengthSearch(int * pcch) {return E_NOTIMPL;}
		STDMETHOD(GetCharProps)(int ich, LgCharRenderProps * pchrp, int * pichMin,
			int * pichLim);
		STDMETHOD(GetParaProps)(int ich, LgParaRenderProps * pchrp, int * pichMin,
			int * pichLim);
		STDMETHOD(GetCharStringProp)(int ich, int id, BSTR * pbstr, int * pichMin,
			int * pichLim);
		STDMETHOD(GetParaStringProp)(int ich, int id, BSTR * pbstr, int * pichMin,
			int * pichLim);
		STDMETHOD(GetSubString)(int ichMin, int ichLim, ITsString ** pptss);
		STDMETHOD(GetWsFactory)(ILgWritingSystemFactory ** ppwsf);
		STDMETHOD(LogToSearch)(int ichlog, int * pichSearch)
		{
			*pichSearch = ichlog;
			return S_OK;
		}
		STDMETHOD(SearchToLog)(int ichSearch, ComBool fAssocPrev, int * pichLog)
		{
			*pichLog = ichSearch;
			return S_OK;
		}
		STDMETHOD(LogToRen)(int ichlog, int * pichRen)
		{
			*pichRen = ichlog;
			return S_OK;
		}
		STDMETHOD(RenToLog)(int ichRen, int * pichLog)
		{
			*pichLog = ichRen;
			return S_OK;
		}
		STDMETHOD(RenToSearch)(int ichRen, int * pichSearch)
		{
			*pichSearch = ichRen;
			return S_OK;
		}
		STDMETHOD(SearchToRen)(int ichSearch, ComBool fAssocPrev, int * pichRen)
		{
			*pichRen = ichSearch;
			return S_OK;
		}

		// Accessor method to get text character at given index.
		int CharAt(int ich)
		{
			return (uint)ich < (uint)m_stu.Length() ? m_stu.GetAt(ich) : 0;
		}

	protected:
		long m_cref;
		StrUni m_stu;
		Vector<int> m_vws;
	};

	TxtSrc::TxtSrc(int n, ILgWritingSystemFactory * pwsf)
	{
		AssertPtr(pwsf);
		m_cref = 1;

		switch(n)
		{
		case 1:
			m_stu.Assign(L"This is a relatively short input text.");
			break;
		case 2:
			m_stu.Assign(
				L"This is an input text 1 that goes on and on and on.  "
				L"This is an input text 2 that goes on and on and on.  "
				L"This is an input text 3 that goes on and on and on.  "
				L"This is an input text 4 that goes on and on and on.  "
				L"This is an input text 5 that goes on and on and on.  "
				L"This is an input text 6 that goes on and on and on.  "
				L"This is an input text 7 that goes on and on and on.  "
				L"This is an input text 8 that goes on and on and on.  "
				L"This is an input text 9 that goes on and on and on.  "
				L"This is an input text 10 that goes on and on and on.  "
				L"This is an input text 11 that goes on and on and on.  "
				L"This is an input text 12 that goes on and on and on.  "
				L"This is an input text 13 that goes on and on and on.  "
				L"This is an input text 14 that goes on and on and on.  "
				L"This is an input text 15 that goes on and on and on.  "
				L"This is an input text 16 that goes on and on and on.  "
				L"This is an input text 17 that goes on and on and on.  "
				L"This is an input text 18 that goes on and on and on.  "
				L"This is an input text 19 that goes on and on and on.  "
				L"This is an input text 20 that goes on and on and on.  "
				L"This is an input text 21 that goes on and on and on.  "
				L"This is an input text 22 that goes on and on and on.  "
				L"This is an input text 23 that goes on and on and on.  "
				L"This is an input text 24 that goes on and on and on.  "
				L"This is an input text 25 that goes on and on and on.  "
				L"This is an input text 26 that goes on and on and on.  "
				L"This is an input text 27 that goes on and on and on.  "
				L"This is an input text 28 that goes on and on and on.  "
				L"This is an input text 29 that goes on and on and on.  "
				L"This is an input text 30 that goes on and on and on.  "
				L"This is an input text 31 that goes on and on and on.  "
				L"This is an input text 32 that goes on and on and on.  "
				L"This is an input text 33 that goes on and on and on.  "
				L"This is an input text 34 that goes on and on and on.  "
				L"This is an input text 35 that goes on and on and on.  "
				L"This is an input text 36 that goes on and on and on.  "
				L"This is an input text 37 that goes on and on and on.  "
				L"This is an input text 38 that goes on and on and on.  "
				L"This is an input text 39 that goes on and on and on.  "
				L"This is an input text 40 that goes on and on and on.  "
				L"This is an input text 41 that goes on and on and on.  "
				L"This is an input text 42 that goes on and on and on.  "
				L"This is an input text 43 that goes on and on and on.  "
				L"This is an input text 44 that goes on and on and on.  "
				L"This is an input text 45 that goes on and on and on.  "
				L"This is an input text 46 that goes on and on and on.  "
				L"This is an input text 47 that goes on and on and on.  "
				L"This is an input text 48 that goes on and on and on.  "
				L"This is an input text 49 that goes on and on and on.  "
				L"This is an input text 50 that goes on and on and on.  "
				);
			break;
		case 3:
			m_stu.Assign(
				L"This is an input text with hard breaks.\tThis was a tab.\rThis was a return. "
				L"\x2028 This is a hard line break.\nThis was a new line."
				L"\xfffc This was a object."
				);
			break;
		case 4:
			m_stu.Assign(L"\rShort text with hard break at beginning.");
			break;
		case 5:
			m_stu.Assign(L"\x2028\x2028Two hard breaks at beginning.");
			break;
		}
		int cws = 0;
		pwsf->get_NumberOfWs(&cws);
		m_vws.Resize(cws);
		pwsf->GetWritingSystems(m_vws.Begin(), m_vws.Size());
	};


	STDMETHODIMP TxtSrc::QueryInterface(REFIID riid, void ** ppv)
	{
		AssertPtr(ppv);
		if (!ppv)
			return WarnHr(E_POINTER);
		*ppv = NULL;

		if (riid == IID_IUnknown)
			*ppv = static_cast<IUnknown *>(this);
		else if (riid == IID_IVwTextSource)
			*ppv = static_cast<IVwTextSource *>(this);
		else
			return E_NOINTERFACE;

		AddRef();
		return S_OK;
	}

	STDMETHODIMP TxtSrc::Fetch(int ichMin, int ichLim, OLECHAR * prgchBuf)
	{
		BEGIN_COM_METHOD;
		ChkComArrayArg(prgchBuf, ichLim-ichMin);

		if (ichMin < 0 || ichLim < ichMin || ichLim > m_stu.Length())
			return E_INVALIDARG;
		if (ichMin == ichLim)
			return S_OK;

#ifdef WIN32
		wmemcpy(prgchBuf, m_stu.Chars() + ichMin, ichLim - ichMin);
#else
		wmemcpy((wchar_t*)prgchBuf, (wchar_t*)m_stu.Chars() + ichMin, ichLim - ichMin);
#endif
		return S_OK;

		END_COM_METHOD(g_fact, IID_IVwTextSource);
	}

	STDMETHODIMP TxtSrc::get_Length(int * pcch)
	{
		BEGIN_COM_METHOD;
		ChkComOutPtr(pcch);

		*pcch = m_stu.Length();
		return S_OK;

		END_COM_METHOD(g_fact, IID_IVwTextSource);
	}

	STDMETHODIMP TxtSrc::GetCharProps(int ich, LgCharRenderProps * pchrp, int * pichMin,
		int * pichLim)
	{
		BEGIN_COM_METHOD;
		ChkComArgPtr(pchrp);
		ChkComOutPtr(pichMin);
		ChkComOutPtr(pichLim);

		if (ich < 0 || ich > m_stu.Length())
			return E_INVALIDARG;

		pchrp->clrFore = kclrBlack;
		pchrp->clrBack = kclrWhite;
		pchrp->clrUnder = kclrRed;
		pchrp->dympOffset = 0;
		pchrp->fWsRtl = FALSE;
		pchrp->nDirDepth = 0;
		pchrp->ssv = kssvOff;
		pchrp->unt = kuntNone;
		pchrp->ttvBold = kttvOff;
		pchrp->ttvItalic = kttvOff;
		pchrp->dympHeight = 14000;		// 14pt.
		wcscpy_s(pchrp->szFontVar, 32, StrUni(L"").Chars());

		if (ich < 1000)
		{
			if (m_vws.Size())
				pchrp->ws = m_vws[0];
			else
				pchrp->ws = 0;
			wcscpy_s(pchrp->szFaceName, 32, StrUni(L"Lucida Console").Chars());
			*pichMin = 0;
			*pichLim = m_stu.Length() < 1000 ? m_stu.Length() : 1000;
		}
		else if (ich < 2000)
		{
			if (m_vws.Size() > 1)
				pchrp->ws = m_vws[1];
			else
				pchrp->ws = g_wsTest;
			wcscpy_s(pchrp->szFaceName, 32, StrUni(L"Lucida Sans Unicode").Chars());
			*pichMin = 1000;
			*pichLim = m_stu.Length() < 2000 ? m_stu.Length() : 2000;
		}
		else
		{
			if (m_vws.Size() > 2)
				pchrp->ws = m_vws[2];
			else
				pchrp->ws = g_wsTest2;
			wcscpy_s(pchrp->szFaceName, 32, StrUni(L"Times New Roman").Chars());
			*pichMin = 2000;
			*pichLim = m_stu.Length();
		}

		return S_OK;

		END_COM_METHOD(g_fact, IID_IVwTextSource);
	}

	STDMETHODIMP TxtSrc::GetParaProps(int ich, LgParaRenderProps * pchrp, int * pichMin,
		int * pichLim)
	{
		BEGIN_COM_METHOD;
		return E_NOTIMPL;
		END_COM_METHOD(g_fact, IID_IVwTextSource);
	}

	STDMETHODIMP TxtSrc::GetCharStringProp(int ich, int id, BSTR * pbstr, int * pichMin,
		int * pichLim)
	{
		BEGIN_COM_METHOD;
		return E_NOTIMPL;
		END_COM_METHOD(g_fact, IID_IVwTextSource);
	}

	STDMETHODIMP TxtSrc::GetParaStringProp(int ich, int id, BSTR * pbstr, int * pichMin,
		int * pichLim)
	{
		BEGIN_COM_METHOD;
		return E_NOTIMPL;
		END_COM_METHOD(g_fact, IID_IVwTextSource);
	}
	STDMETHODIMP TxtSrc::GetSubString(int ichMin, int ichLim, ITsString ** pptss)
	{
		BEGIN_COM_METHOD;
		return E_NOTIMPL;
		END_COM_METHOD(g_fact, IID_IVwTextSource);
	}
	STDMETHODIMP TxtSrc::GetWsFactory(ILgWritingSystemFactory ** ppwsf)
	{
		BEGIN_COM_METHOD;
		return E_NOTIMPL;
		END_COM_METHOD(g_fact, IID_IVwTextSource);
	}

	/*******************************************************************************************
		Base class for testing RenderEngines
	 ******************************************************************************************/
	class RenderEngineTestBase
	{
	protected:
		IRenderEnginePtr m_qre;
		ILgWritingSystemFactoryPtr m_qwsf;
		int m_wsEng;
		int m_wsTest;
		int m_wsTest2;

		void VerifyNullArgs()
		{
			HRESULT hr;
//			ISimpleInit* pre = dynamic_cast<ISimpleInit*>(m_qre.Ptr());
//			hr = pre->get_InitializationData(NULL);
//			unitpp::assert_eq("get_InitializationData(NULL); HRESULT", E_POINTER, hr);
			try{
				CheckHr(hr = m_qre->get_SegDatMaxLength(NULL));
				unitpp::assert_eq("get_SegDatMaxLength(NULL); HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_SegDatMaxLength(NULL); HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qre->FindBreakPoint(NULL, NULL, NULL, 0, 0, 0, FALSE, FALSE, 0, (LgLineBreak)0,
					(LgLineBreak)0, (LgTrailingWsHandling)0, FALSE, NULL, NULL, NULL, NULL,
					//0, NULL, 0, NULL, NULL, NULL,
					NULL));
				unitpp::assert_eq("FindBreakPoint(NULL, NULL, ...) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("FindBreakPoint(NULL, NULL, ...) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qre->get_ScriptDirection(NULL));
				unitpp::assert_eq("get_ScriptDirection(NULL); HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_ScriptDirection(NULL); HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qre->get_ClassId(NULL));
				unitpp::assert_eq("get_ClassId(NULL); HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_ClassId(NULL); HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qre->get_WritingSystemFactory(NULL));
				unitpp::assert_eq("get_WritingSystemFactory(NULL); HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_WritingSystemFactory(NULL); HRESULT", E_POINTER, thr.Result());
			}
		}

		void VerifyBreakPointing()
		{
#ifdef WIN32
			// Create an IVwGraphics object and initialize it.
			int dxMax = 600;
			HDC hdc = ::CreateCompatibleDC(::GetDC(::GetDesktopWindow()));
			HBITMAP hbm = ::CreateCompatibleBitmap(hdc, dxMax, dxMax);
			::SelectObject(hdc, hbm);
			::SetMapMode(hdc, MM_TEXT);
			IVwGraphicsWin32Ptr qvg;
			qvg.CreateInstance(CLSID_VwGraphicsWin32);
			qvg->Initialize(hdc);
			ILgWritingSystemFactoryPtr qwsf;	// needed as data for m_qre.

			try
			{
				HRESULT hr;
				IVwTextSourcePtr qts;
				int cch;
				ILgSegmentPtr qseg;
				int dichLimSeg;
				int dxWidth;
				LgEndSegmentType est;
				//int cb0, dich0;			// output values always set to zero.

				m_qre->get_WritingSystemFactory(&qwsf);

				// Create a simple IVwTextSource string.
				TxtSrc ts1(1, qwsf);
				ts1.QueryInterface(IID_IVwTextSource, (void **)&qts);
				hr = qts->get_Length(&cch);
				hr = m_qre->FindBreakPoint(qvg, qts, NULL, 0, cch, cch, TRUE, TRUE, dxMax,
					klbWordBreak, klbLetterBreak, ktwshAll, FALSE,
					&qseg, &dichLimSeg, &dxWidth, &est,
					//0, NULL, 0, NULL, &cb0, &dich0,
					NULL);
				unitpp::assert_eq("FindBreakPoint(Short string) HRESULT", S_OK, hr);
				unitpp::assert_eq("Short string fits in one segment", cch, dichLimSeg);
				unitpp::assert_eq("Short string fits in one segment", kestNoMore, est);
				unitpp::assert_true("output segment no wider than maximum",
					(uint)dxWidth <= (uint)dxMax);
				qts.Clear();

				// Do another test with a much longer IVwTextSource string.
				TxtSrc ts2(2, qwsf);
				int cSeg = VerifyBreakPoints(&ts2, dxMax, dichLimSeg, (IVwGraphics*)qvg);

				// Do another test with a IVwTextSource string with hard breaks in it.
				TxtSrc ts3(3, qwsf);
				cSeg = VerifyBreakPoints(&ts3, dxMax, dichLimSeg, (IVwGraphics*)qvg);
				unitpp::assert_eq("Unexpected number of segments found (ts3)", 6, cSeg);

				// Do another test with a IVwTextSource string with hard breaks at beginning.
				TxtSrc ts4(4, qwsf);
				cSeg = VerifyBreakPoints(&ts4, dxMax, dichLimSeg, (IVwGraphics*)qvg);
				unitpp::assert_eq("Unexpected number of segments found (ts4)", 2, cSeg);

				// Do another test with a IVwTextSource string with hard breaks at beginning.
				TxtSrc ts5(5, qwsf);
				cSeg = VerifyBreakPoints(&ts5, dxMax, dichLimSeg, (IVwGraphics*)qvg);
				unitpp::assert_eq("Unexpected number of segments found (ts5)", 3, cSeg);
			}
			catch(...)
			{
				// Cleanup the system objects that were created.
				qvg.Clear();
				::DeleteObject(hbm);
				::DeleteDC(hdc);
				throw;
			}

			// Cleanup the system objects that were created.
			qvg.Clear();
			::DeleteObject(hbm);
			::DeleteDC(hdc);
#endif
		}

		virtual IRenderEnginePtr GetRenderer(IVwGraphics * pvg, LgCharRenderProps* pchrp)
		{
			IRenderEnginePtr qreneng;
			m_qwsf->get_RendererFromChrp(pvg, pchrp, &qreneng);
			return qreneng;
		}

		int VerifyBreakPoints(TxtSrc* ts, uint dxMax, int dichLimSeg, IVwGraphics* pvg)
		{
			IVwTextSourcePtr qts;
			ts->QueryInterface(IID_IVwTextSource, (void **)&qts);
			ILgSegmentPtr qseg;
			int cch;
			HRESULT hr = qts->get_Length(&cch);
			int ichMin;
			int dxAvail = dxMax;
			ComBool fStart = TRUE;
			int dxWidth;
			LgEndSegmentType est;
			//int cb0, dich0;			// output values always set to zero.
			int cSegs = 0; // number of segments we identify
			for (ichMin = 0; ichMin < cch; ichMin += dichLimSeg)
			{
				// Must use what the factory says is the right renderer for the properties.
				// Otherwise it just keeps saying it is the wrong renderer, and we make no
				// progress.
				LgCharRenderProps chrp;
				int ichMinTmp, ichLimTmp;
				ts->GetCharProps(ichMin, &chrp, &ichMinTmp, &ichLimTmp);
				IRenderEnginePtr qreneng = GetRenderer(pvg, &chrp);
				if (!qreneng)
				{
					unitpp::assert_true("VerifyBreakPoints - GetRenderer", chrp.ws != g_wsEng);
					dichLimSeg = ichLimTmp - ichMin;
					if (!dichLimSeg)
						dichLimSeg = 1;
					continue;
				}
				hr = qreneng->FindBreakPoint(pvg, qts, NULL, ichMin, cch, cch, FALSE, fStart,
					dxAvail, klbWordBreak, klbWordBreak, ktwshAll, FALSE,
					&qseg, &dichLimSeg, &dxWidth, &est,
					//0, NULL, 0, NULL, &cb0, &dich0,
					NULL);
				fStart = TRUE;
				unitpp::assert_eq("FindBreakPoint(long string) HRESULT", S_OK, hr);
				if (dxWidth > dxAvail)
				{
					unitpp::assert_eq("OverLong segment ends with a space",
						L' ', ts->CharAt(ichMin + dichLimSeg - 1));
				}
				switch (est)
				{
				case kestNoMore:
					// No more segments are needed, everything in [ichMin, ichLim) fit.
					unitpp::assert_true("Long string requires multiple segments", ichMin > 0);
//					printf("DEBUG: est = NoMore, ichMin = %d, dichLim = %d, dxWidth = %d\n",
//						ichMin, dichLimSeg, dxWidth);
					break;
				case kestMoreLines:
					// We filled the line and need to put the rest of the text on another.
//					printf("DEBUG: est = MoreLines, ichMin = %d, dichLim = %d, dxWidth = %d\n",
//						ichMin, dichLimSeg, dxWidth);
					unitpp::assert_eq("est = MoreLines: segment ends with a space",
						L' ', ts->CharAt(ichMin + dichLimSeg - 1));
					break;
				case kestHardBreak:
				{
					// We found a hard break, e.g., a tab or return. Depending on which
					// character it is, something more may or may not go on this line.
					// pdichLimSeg indicates the location of the hard break character.
					int breakChar = ts->CharAt(ichMin + dichLimSeg);
					unitpp::assert_true("Long string breaks at a tab or return",
						breakChar == '\t' || breakChar == '\n' || breakChar == '\r'
						|| breakChar == 0xfffc || breakChar == 0x2028);
					if (!dichLimSeg)
					{
						unitpp::assert_true("Segment is NULL instead of empty", qseg);
					}
					dichLimSeg++; // skip hard break character
//					printf("DEBUG: est = HardBreak, ichMin = %d, dichLim = %d, dxWidth = %d\n",
//						ichMin, dichLimSeg, dxWidth);
					break;
				}
				case kestBadBreak:
					// We ended this segment for some reason (e.g., change of chrp) at a point
					// that is not a valid line break. Client must either fit another segment
					// on this line, or replace this segment with one that breaks sooner.
//					printf("DEBUG: est = BadBreak, ichMin = %d, dichLim = %d, dxWidth = %d\n",
//						ichMin, dichLimSeg, dxWidth);
					break;
				case kestOkayBreak:
					// We ended this segment for some reason (e.g., change of chrp) at a point
					// that is a valid line break; however, there may be room for more stuff
					// on the line.
//					printf("DEBUG: est = OkayBreak, ichMin = %d, dichLim = %d, dxWidth = %d\n",
//						ichMin, dichLimSeg, dxWidth);
					break;
				case kestWsBreak:
					// We ended the segment because we hit the end of the old writing system;
					// the caller is responsible to determine if this is in fact a valid break,
					// and whether there is room to put more stuff on the line.
//					printf("DEBUG: est = WsBreak, ichMin = %d, dichLim = %d, dxWidth = %d\n",
//						ichMin, dichLimSeg, dxWidth);
					unitpp::assert_true("Writing system boundary ends segment",
						((ichMin + dichLimSeg) % 1000) == 0);
					dxAvail = dxMax - dxWidth;
					if (dxAvail > 10)
						fStart = FALSE;
					break;
				case kestMoreWhtsp:
					// We filled the line and need to put the rest of the text on another,
					// but we didn't include the trailing white-space in this segment; it needs
					// its own segment that will go on the same line.
//					printf("DEBUG: est = MoreWhtsp, ichMin = %d, dichLim = %d, dxWidth = %d\n",
//						ichMin, dichLimSeg, dxWidth);
					break;
				default:
					unitpp::assert_true("invalid LgEndSegmentType value", false);
					break;
				}
				if (fStart)
					dxAvail = dxMax;
				cSegs++;
			}
			qts.Clear();
			return cSegs;
		}

	public:
		virtual void Setup()
		{
			m_qwsf.Attach(NewObj MockLgWritingSystemFactory);
			ILgWritingSystemPtr qws;
			SmartBstr sbstr;

			sbstr.Assign(kszEng);
			m_qwsf->get_Engine(sbstr, &qws);
			qws->get_Handle(&g_wsEng);

			sbstr.Assign(kszTest);
			m_qwsf->get_Engine(sbstr, &qws);
			qws->get_Handle(&g_wsTest);

			sbstr.Assign(kszTest2);
			m_qwsf->get_Engine(sbstr, &qws);
			qws->get_Handle(&g_wsTest);
		}
		virtual void Teardown()
		{
			m_qwsf.Clear();
			m_qre.Clear();
		}
	};
}

#endif /*RenderEngineTestBase_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
