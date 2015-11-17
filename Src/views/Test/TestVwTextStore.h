/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestVwTextStore.h
Responsibility:
Last reviewed:

	Unit tests for the VwTextStore class.

	TODO JohnT: not yet testing unimplemented functionality for multi-paragraph selections
		or selections not entirely editable or selections where the end-point paragraph(s)
		are embedded in tables, interlinear texts, etc.

	TODO JohnT: Test that all applicable functions return something sensible (or at least a
		clean E_FAIL, rather than asserting or worse) if the view has no selection.  Consider
		not telling TSF we have focus in such a case.  (Can we get rid of focus by switching to
		the desktop window and back?)
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestVwTextStore_H_INCLUDED
#define TestVwTextStore_H_INCLUDED

#pragma once

#include "testViews.h"

#ifdef WIN32
#undef ENABLE_TSF
#define ENABLE_TSF

namespace TestViews
{

#define assert_exception(msg, hrValue, func) \
		{ \
			HRESULT hr = S_OK; \
			try \
			{ \
				(func); \
			} \
			catch (Throwable& thr) \
			{ \
				hr = thr.Result(); \
			} \
			unitpp::assert_eq((msg), (hrValue), hr); \
		}

#define assert_exceptionHr(msg, hrValue, func) \
		{ \
			HRESULT hr = S_OK; \
			try \
			{ \
				CheckHr(func); \
			} \
			catch (Throwable& thr) \
			{ \
				hr = thr.Result(); \
			} \
			unitpp::assert_eq((msg), (hrValue), hr); \
		}

	//******************************************************************************************
	// Global variables.
	//******************************************************************************************

#define khvoParaBase 1000
#define khvoPara1 1
#define khvoPara2 2
#define khvoPara3 3
#define khvoPara4 4
#define khvoRootText 101
#define kflidFake 999
#define kfragRoot 1001
#define kfragPara 1002
#define kfragFake 1003
#define kfragRootLazy 1004

	static const OLECHAR * s_rgpsz1[] = {
		L"This is the first test paragraph",
		L"This is the second test paragraph",
		NULL
	};

	static const OLECHAR * s_rgpsz2[] = {
		L"This is the first test paragraph",
		L"This is the second test paragraph",
		L"This is the para 3",
		L"",
		L"This is the para 5 (para 4 is empty)",
		NULL
	};

	static const OLECHAR * s_rgpsz3[] = {
		L"\x1112\x1161\x11ab \x1112\x1161\x11ab",
		NULL
	};
	static const OLECHAR s_rgpszExpected3[] = L"\xd55c \xd55c";

	static TS_RUNINFO s_triExpected3[] = {
		{ 3, TS_RT_PLAIN },
	};

	static StrUni s_stuParaBreak;
	static long s_cchParaBreak;
	static long s_cchPara1;
	static long s_cchPara2;
	static long s_cchPara3;
	static long s_cchPara4;
	static long s_cchPara5;
	const OLECHAR chBound = (OLECHAR)-1;

	//******************************************************************************************
	// Mock Object Classes to support testing.
	//******************************************************************************************

	/*------------------------------------------------------------------------------------------
		Mock (simple, incomplete) implementation of the ITextStoreACPSink interface for use in
		testing.
	------------------------------------------------------------------------------------------*/
	class MockTextStoreACPSink : public ITextStoreACPSink
	{
	public:
		MockTextStoreACPSink(ITextStoreACP * ptsa, IUnknown * punkOldSink)
		{
			m_qtsa = ptsa;
			if (punkOldSink)
				CheckHr(ptsa->UnadviseSink(punkOldSink));
			CheckHr(ptsa->AdviseSink(IID_ITextStoreACPSink, this, TS_AS_ALL_SINKS));
			m_fLockGranted = false;
			m_dwLockFlags = 0;
		}
		MockTextStoreACPSink()
		{
			m_fLockGranted = false;
			m_dwLockFlags = 0;
		}
		virtual ~MockTextStoreACPSink()
		{
		}
		STDMETHOD(QueryInterface)(REFIID riid, void ** ppv)
		{
			AssertPtr(ppv);
			if (!ppv)
				return WarnHr(E_POINTER);
			*ppv = NULL;

			if (riid == IID_IUnknown)
				*ppv = static_cast<IUnknown *>(this);
			else if (riid == IID_ITextStoreACPSink)
				*ppv = static_cast<ITextStoreACPSink *>(this);
			else
				return E_NOINTERFACE;

			AddRef();
			return NOERROR;
		}
		STDMETHOD_(ULONG, AddRef)(void)
		{
			return 1;
		}
		STDMETHOD_(ULONG, Release)(void)
		{
			return 1;
		}
		STDMETHOD(OnTextChange)(DWORD dwFlags, const TS_TEXTCHANGE *pChange)
		{
			return S_OK;
		}
		STDMETHOD(OnSelectionChange)(void)
		{
			return S_OK;
		}
		STDMETHOD(OnLayoutChange)(TsLayoutCode lcode, TsViewCookie vcView)
		{
			return S_OK;
		}
		STDMETHOD(OnStatusChange)(DWORD dwFlags)
		{
			return S_OK;
		}
		STDMETHOD(OnAttrsChange)(LONG acpStart, LONG acpEnd, ULONG cAttrs,
			const TS_ATTRID *paAttrs)
		{
			return S_OK;
		}

		STDMETHOD(OnLockGranted)(DWORD dwLockFlags)
		{
			if (dwLockFlags & ~(TS_LF_READWRITE | TS_LF_SYNC))
				return E_INVALIDARG;
			m_fLockGranted = true;
			m_dwLockFlags = dwLockFlags;
			return S_OK;
		}

		STDMETHOD(OnStartEditTransaction)(void)
		{
			return E_NOTIMPL;
		}
		STDMETHOD(OnEndEditTransaction)(void)
		{
			return E_NOTIMPL;
		}

		bool GetLockGranted()
		{
			return m_fLockGranted;
		}
		DWORD GetLockFlags()
		{
			return m_dwLockFlags;
		}
		void ClearGrantedLock()
		{
			m_fLockGranted = false;
			m_dwLockFlags = 0;
		}
	protected:
		ITextStoreACPPtr m_qtsa;
		bool m_fLockGranted;
		DWORD m_dwLockFlags;
	};

	/*------------------------------------------------------------------------------------------
		This class exists to test error reporting on getting locks.
		The only way to request a lock when one is unavailable is to do it during another
		OnLockGranted. So this one does so and confirms that it gets an error message.
	------------------------------------------------------------------------------------------*/
	class MockTSDoubleLocker : public MockTextStoreACPSink
	{
	public:
		MockTSDoubleLocker(ITextStoreACP * ptsa)
		{
			m_qtsa = ptsa;
			m_hrSession = S_OK;
			m_hrCall = S_OK;
			m_nTestCase = 0;
		}

		STDMETHOD(OnLockGranted)(DWORD dwLockFlags)
		{
			switch(m_nTestCase)
			{
			case 1:
				try{
					CheckHr(m_hrCall = m_qtsa->RequestLock(TS_LF_READ | TS_LF_SYNC, &m_hrSession));
				}
				catch(Throwable& thr){
					m_hrCall = thr.Result();
				}
				break;

			case 2:
				try{
					CheckHr(m_hrCall = m_qtsa->RequestLock(TS_LF_READ, &m_hrSession));
				}
				catch(Throwable& thr){
					m_hrCall = thr.Result();
				}
				break;

			// Read-write async call will fail if already read-write locked, but
			// succeed if current lock is read-only.
			case 3:
				try{
					CheckHr(m_hrCall = m_qtsa->RequestLock(TS_LF_READWRITE, &m_hrSession));
				}
				catch(Throwable& thr){
					m_hrCall = thr.Result();
				}
				m_nTestCase = 4;
				break;
			case 4: // upgrade lock call automatically invoked after case 3 returns.
				MockTextStoreACPSink::OnLockGranted(dwLockFlags);
				m_nTestCase = 3;
				break;
			}

			return S_OK;
		}

	public:
		HRESULT m_hrSession;
		HRESULT m_hrCall;
		int m_nTestCase;
	};

	/*------------------------------------------------------------------------------------------
		Provide an ITextStoreACPSink class for testing VwTextStore::GetSelection().
	------------------------------------------------------------------------------------------*/
	class LockGetSelection : public MockTextStoreACPSink
	{
	public:
		LockGetSelection(ITextStoreACP * ptsa, TS_SELECTION_ACP * pSelection, ULONG * pcFetched)
			: MockTextStoreACPSink(ptsa, NULL)
		{
			m_pSelection = pSelection;
			m_pcFetched = pcFetched;
			CheckHr(ptsa->RequestLock(TS_LF_READ | TS_LF_SYNC, &m_hrLock));
			CheckHr(ptsa->UnadviseSink(this));
		}

		STDMETHOD(OnLockGranted)(DWORD dwLockFlags)
		{
			HRESULT hr;
			IgnoreHr(hr = m_qtsa->GetSelection(0, 1, m_pSelection, m_pcFetched));
			return hr;
		}

		TS_SELECTION_ACP * m_pSelection;
		ULONG * m_pcFetched;
		HRESULT m_hrLock;
	};

	/*------------------------------------------------------------------------------------------
		Provide an ITextStoreACPSink class for testing VwTextStore::SetSelection().
	------------------------------------------------------------------------------------------*/
	class LockSetSelection : public MockTextStoreACPSink
	{
	public:
		LockSetSelection(ITextStoreACP * ptsa, TS_SELECTION_ACP * pSelection)
			: MockTextStoreACPSink(ptsa, NULL)
		{
			m_pSelection = pSelection;
			CheckHr(ptsa->RequestLock(TS_LF_READWRITE | TS_LF_SYNC, &m_hrLock));
			CheckHr(ptsa->UnadviseSink(this));
		}

		STDMETHOD(OnLockGranted)(DWORD dwLockFlags)
		{
			HRESULT hr;
			IgnoreHr(hr = m_qtsa->SetSelection(1, m_pSelection));
			return hr;
		}

		TS_SELECTION_ACP * m_pSelection;
		HRESULT m_hrLock;
	};

	/*------------------------------------------------------------------------------------------
		Provide an ITextStoreACPSink class for testing VwTextStore::GetText().
	------------------------------------------------------------------------------------------*/
	class LockGetText : public MockTextStoreACPSink
	{
	public:
		LockGetText(ITextStoreACP * ptsa, int ichStart, int ichEnd,
			OLECHAR * prgchBuf, int cchBuf, TS_RUNINFO * ptri, int ctri)
			: MockTextStoreACPSink(ptsa, NULL)
		{
			m_ichStart = ichStart;
			m_ichEnd = ichEnd;
			m_ptri = ptri;
			m_ctri = ctri;
			m_prgchBuf = prgchBuf;
			m_cchBuf = cchBuf;
			m_cchOut = (ULONG)-1;
			m_ctriOut = (ULONG)-1;
			m_ichNext = (ULONG)-1;
			m_hrLock = S_OK;

			CheckHr(ptsa->RequestLock(TS_LF_READ | TS_LF_SYNC, &m_hrLock));
			CheckHr(ptsa->UnadviseSink(this));
		}

		STDMETHOD(OnLockGranted)(DWORD dwLockFlags)
		{
			HRESULT hr;
			IgnoreHr(hr = m_qtsa->GetText(m_ichStart, m_ichEnd, m_prgchBuf, m_cchBuf, &m_cchOut,
				m_ptri, m_ctri, &m_ctriOut, &m_ichNext));
			return hr;
		}

		LONG m_ichStart;
		LONG m_ichEnd;
		TS_RUNINFO * m_ptri;
		ULONG m_ctri;
		OLECHAR * m_prgchBuf;
		ULONG m_cchBuf;
		ULONG m_cchOut;
		ULONG m_ctriOut;
		LONG m_ichNext;
		HRESULT m_hrLock;
	};

	/*------------------------------------------------------------------------------------------
		Provide an ITextStoreACPSink class for testing VwTextStore::SetText().
	------------------------------------------------------------------------------------------*/
	class LockSetText : public MockTextStoreACPSink
	{
	public:
		LockSetText(ITextStoreACP * ptsa, int ichStart, int ichEnd, const OLECHAR * pszText,
			TS_TEXTCHANGE * pttc)
			: MockTextStoreACPSink(ptsa, NULL)
		{
			m_ichStart = ichStart;
			m_ichEnd = ichEnd;
			m_pszText = pszText;
			m_pttc = pttc;

			CheckHr(ptsa->RequestLock(TS_LF_READWRITE | TS_LF_SYNC, &m_hrLock));
			CheckHr(ptsa->UnadviseSink(this));
		}

		STDMETHOD(OnLockGranted)(DWORD dwLockFlags)
		{
			HRESULT hr;
			IgnoreHr(hr = m_qtsa->SetText(0, m_ichStart, m_ichEnd, m_pszText, wcslen(m_pszText),
				m_pttc));
			return hr;
		}

		int m_ichStart;
		int m_ichEnd;
		const OLECHAR * m_pszText;
		TS_TEXTCHANGE * m_pttc;
		HRESULT m_hrLock;
	};

	/*------------------------------------------------------------------------------------------
		Provide an ITextStoreACPSink class for testing VwTextStore::InsertTextAtSelection().
	------------------------------------------------------------------------------------------*/
	class LockInsertTextAtSelection : public MockTextStoreACPSink
	{
	public:
		LockInsertTextAtSelection(ITextStoreACP * ptsa, DWORD dwFlags,
			const OLECHAR * pszText, LONG * pichStart, LONG * pichEnd, TS_TEXTCHANGE * pChange)
			: MockTextStoreACPSink(ptsa, NULL)
		{
			m_dwFlags = dwFlags;
			m_pszText = pszText;
			m_pichStart = pichStart;
			m_pichEnd = pichEnd;
			m_pChange = pChange;

			CheckHr(ptsa->RequestLock(TS_LF_READWRITE | TS_LF_SYNC, &m_hrLock));
			CheckHr(ptsa->UnadviseSink(this));
		}

		STDMETHOD(OnLockGranted)(DWORD dwLockFlags)
		{
			HRESULT hr;
			IgnoreHr(hr = m_qtsa->InsertTextAtSelection(m_dwFlags, m_pszText, wcslen(m_pszText),
				m_pichStart, m_pichEnd, m_pChange));
			return hr;
		}

		DWORD m_dwFlags;
		const OLECHAR * m_pszText;
		LONG * m_pichStart;
		LONG * m_pichEnd;
		TS_TEXTCHANGE * m_pChange;
		HRESULT m_hrLock;
	};

	/*------------------------------------------------------------------------------------------
		Provide an ITextStoreACPSink class for testing VwTextStore::GetFormattedText().
	------------------------------------------------------------------------------------------*/
	class LockGetFormattedText : public MockTextStoreACPSink
	{
	public:
		LockGetFormattedText(ITextStoreACP * ptsa, int ichStart, int ichEnd,
			IDataObject ** ppdo)
			: MockTextStoreACPSink(ptsa, NULL)
		{
			m_ichStart = ichStart;
			m_ichEnd = ichEnd;
			m_ppdo = ppdo;
			CheckHr(ptsa->RequestLock(TS_LF_READWRITE | TS_LF_SYNC, &m_hrLock));
			CheckHr(ptsa->UnadviseSink(this));
		}

		STDMETHOD(OnLockGranted)(DWORD dwLockFlags)
		{
			HRESULT hr;
			IgnoreHr(hr = m_qtsa->GetFormattedText(m_ichStart, m_ichEnd, m_ppdo));
			return hr;
		}
		int m_ichStart;
		int m_ichEnd;
		IDataObject ** m_ppdo;
		HRESULT m_hrLock;
	};

	/*------------------------------------------------------------------------------------------
		Provide an ITextStoreACPSink class for testing VwTextStore::GetEndACP().
	------------------------------------------------------------------------------------------*/
	class LockGetEndACP : public MockTextStoreACPSink
	{
	public:
		LockGetEndACP(ITextStoreACP * ptsa, LONG * pcch)
			: MockTextStoreACPSink(ptsa, NULL)
		{
			m_pcch = pcch;

			CheckHr(ptsa->RequestLock(TS_LF_READ | TS_LF_SYNC, &m_hrLock));
			CheckHr(ptsa->UnadviseSink(this));
		}

		STDMETHOD(OnLockGranted)(DWORD dwLockFlags)
		{
			HRESULT hr;
			IgnoreHr(hr = m_qtsa->GetEndACP(m_pcch));
			return hr;
		}

		LONG * m_pcch;
		HRESULT m_hrLock;
	};

	/*------------------------------------------------------------------------------------------
		Provide an ITextStoreACPSink class for testing VwTextStore::GetTextExt().
	------------------------------------------------------------------------------------------*/
	class LockGetTextExt : public MockTextStoreACPSink
	{
	public:
		LockGetTextExt(ITextStoreACP * ptsa, TsViewCookie vcView, LONG acpStart, LONG acpEnd,
			RECT * prc, BOOL * pfClipped)
			: MockTextStoreACPSink(ptsa, NULL)
		{
			m_vcView = vcView;
			m_acpStart = acpStart;
			m_acpEnd = acpEnd;
			m_prc = prc;
			m_pfClipped = pfClipped;

			CheckHr(ptsa->RequestLock(TS_LF_READ | TS_LF_SYNC, &m_hrLock));
			CheckHr(ptsa->UnadviseSink(this));
		}

		STDMETHOD(OnLockGranted)(DWORD dwLockFlags)
		{
			HRESULT hr;
			IgnoreHr(hr = m_qtsa->GetTextExt(m_vcView, m_acpStart, m_acpEnd, m_prc, m_pfClipped));
			return hr;
		}

		TsViewCookie m_vcView;
		LONG m_acpStart;
		LONG m_acpEnd;
		RECT * m_prc;
		BOOL * m_pfClipped;
		HRESULT m_hrLock;
	};

	/*------------------------------------------------------------------------------------------
		Provide an ITextStoreACPSink class for testing VwTextStore::GetScreenExt().
	------------------------------------------------------------------------------------------*/
	class LockGetScreenExt : public MockTextStoreACPSink
	{
	public:
		LockGetScreenExt(ITextStoreACP * ptsa, TsViewCookie vcView, RECT * prc)
			: MockTextStoreACPSink(ptsa, NULL)
		{
			m_vcView = vcView;
			m_prc = prc;

			CheckHr(ptsa->RequestLock(TS_LF_READ | TS_LF_SYNC, &m_hrLock));
			CheckHr(ptsa->UnadviseSink(this));
		}

		STDMETHOD(OnLockGranted)(DWORD dwLockFlags)
		{
			HRESULT hr;
			IgnoreHr(hr = m_qtsa->GetScreenExt(m_vcView, m_prc));
			return hr;
		}

		TsViewCookie m_vcView;
		RECT * m_prc;
		HRESULT m_hrLock;
	};


	/*------------------------------------------------------------------------------------------
		Expose protected methods of VwTextStore for testing purposes
	------------------------------------------------------------------------------------------*/
	class VwTextStoreTestSub: public VwTextStore
	{
	public:
		int m_cCalledGetCurrentWS = 0;

		VwTextStoreTestSub(VwRootBox * prootb): VwTextStore(prootb)
		{
			m_cCalledGetCurrentWS = 0;
		}

		void GetCurrentWritingSystem()
		{
			m_cCalledGetCurrentWS++;
			VwTextStore::GetCurrentWritingSystem();
		}

		int CallAcpToLog(int cchAcp)
		{
			return AcpToLog(cchAcp);
		}
		int CallLogToAcp(int cchLog)
		{
			return LogToAcp(cchLog);
		}
	};

	//******************************************************************************************
	// Test Suite Classes.
	//******************************************************************************************

	/*------------------------------------------------------------------------------------------
		Test suite class for testing VwTextStore.
	------------------------------------------------------------------------------------------*/
	class TestVwTextStore : public unitpp::suite
	{
		//**************************************************************************************
		// Data used by the tests.
		//**************************************************************************************

		bool m_fTestable; // true if we can initialize TSF thread manager.
		// Created by fixture setup
		ISilDataAccessPtr m_qsda;
		VwCacheDaPtr m_qcda;
		ITsStrFactoryPtr m_qtsf;
		IVwGraphicsWin32Ptr m_qvg32;
		HDC m_hdc;
		IVwViewConstructorPtr m_qvc;
		HVO m_hvoRoot;
		DummyRootSitePtr m_qdrs;
		// Created for each test
		VwRootBoxPtr m_qrootb;
		VwTextStorePtr m_qtxs;
		HWND m_hwnd; // the dummy window we make to host the view
		Vector<HVO> m_vhvo; // of paragraphs
		IUnknownPtr m_qunkSuspendedAdviseSink;
		DWORD m_dwSuspendedAdviseMask;

	public:
		TestVwTextStore();

#ifdef ENABLE_TSF
		//**************************************************************************************
		// Test methods.
		//**************************************************************************************

		/*--------------------------------------------------------------------------------------
			Test both VwTextStore::AdviseSink() and VwTextStore::UnadviseSink().
		--------------------------------------------------------------------------------------*/
		void testAdviseSink()
		{
			if (!m_fTestable)
				return;
			HRESULT hr = S_OK;
			MockTextStoreACPSink txsas1;
			DWORD dwMask1 = TS_AS_ALL_SINKS;
			MockTextStoreACPSink txsas2;
			DWORD dwMask2 = TS_AS_TEXT_CHANGE;

			CheckHr(hr = m_qtxs->AdviseSink(IID_ITextStoreACPSink, &txsas1, dwMask1));
			unitpp::assert_eq("First AdviseSink", S_OK, hr);
			IgnoreHr(hr = m_qtxs->AdviseSink(IID_ITextStoreACPSink, &txsas2, dwMask2));
			unitpp::assert_eq("Second AdviseSink", CONNECT_E_ADVISELIMIT, hr);
			CheckHr(hr = m_qtxs->UnadviseSink(&txsas1));
			unitpp::assert_eq("First UnadviseSink", S_OK, hr);
			CheckHr(hr = m_qtxs->AdviseSink(IID_ITextStoreACPSink, &txsas2, dwMask2));
			unitpp::assert_eq("Third AdviseSink", S_OK, hr);
			CheckHr(hr = m_qtxs->AdviseSink(IID_ITextStoreACPSink, &txsas2, dwMask1));
			unitpp::assert_eq("Fourth AdviseSink", S_OK, hr);

			assert_exceptionHr("Fifth AdviseSink", E_INVALIDARG,
				m_qtxs->AdviseSink(IID_ITextStoreACPServices, &txsas2, dwMask2));

			CheckHr(hr = m_qtxs->UnadviseSink(&txsas2));
			unitpp::assert_eq("Second UnadviseSink", S_OK, hr);

			// By this point, we've tested basic setting and unsetting of advise sinks.
			// The remaining tests verify that a sink actually gets called when appropriate.
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::RequestLock().
		--------------------------------------------------------------------------------------*/
		void testRequestLock()
		{
			if (!m_fTestable)
				return;
			HRESULT hr = S_OK;
			MockTextStoreACPSink txsas1;
			DWORD dwMask1 = TS_AS_ALL_SINKS;
			CheckHr(hr = m_qtxs->AdviseSink(IID_ITextStoreACPSink, &txsas1, dwMask1));
			unitpp::assert_eq("AdviseSink(&txsas1)", S_OK, hr);

			HRESULT hrLock = S_OK;
			DWORD dwLockFlags = TS_LF_READ | TS_LF_SYNC;
			CheckHr(hr = m_qtxs->RequestLock(dwLockFlags, &hrLock));
			unitpp::assert_eq("First RequestLock hr", S_OK, hr);
			unitpp::assert_eq("First RequestLock hrLock", S_OK, hrLock);
			DWORD dwLock = txsas1.GetLockFlags();
			unitpp::assert_eq("Sink's Lock Flags", dwLockFlags, dwLock);

			CheckHr(hr = m_qtxs->RequestLock(0xFFFFFFFF, &hrLock));
			unitpp::assert_eq("Second RequestLock hr", S_OK, hr);
			unitpp::assert_eq("Second RequestLock hrLock", E_INVALIDARG, hrLock);

			// Try double-locking.
			MockTSDoubleLocker tsdl(m_qtxs);
			CheckHr(hr = m_qtxs->UnadviseSink(&txsas1));
			unitpp::assert_eq("UnadviseSink(&txsas1)", S_OK, hr);
			CheckHr(hr = m_qtxs->AdviseSink(IID_ITextStoreACPSink, &tsdl, dwMask1));
			unitpp::assert_eq("AdviseSink(&tsdl)", S_OK, hr);

			// Trying to get a synchronous lock from within another lock should fail
			tsdl.m_nTestCase = 1;
			CheckHr(hr = m_qtxs->RequestLock(dwLockFlags, &hrLock));
			unitpp::assert_eq("Double lock call OK", S_OK, tsdl.m_hrCall);
			unitpp::assert_eq("Double lock failed", TS_E_SYNCHRONOUS, tsdl.m_hrSession);
			unitpp::assert_eq("Double lock outer call OK", S_OK, hr);

			// Trying to get an async read lock from within another lock should fail.
			tsdl.m_nTestCase = 2;
			CheckHr(hr = m_qtxs->RequestLock(dwLockFlags, &hrLock));
			unitpp::assert_eq("Double async Read lock call OK", E_FAIL, tsdl.m_hrCall);
			unitpp::assert_eq("Double async lock outer call OK", S_OK, hr);

			// Trying to get an async RW lock from within another RW lock should fail.
			CheckHr(hr = m_qtxs->RequestLock(TS_LF_READWRITE | TS_LF_SYNC, &hrLock));
			unitpp::assert_eq("Double async read-write lock fails", E_FAIL, tsdl.m_hrCall);
			unitpp::assert_eq("double locks so far didn't work",
				false, tsdl.GetLockGranted());

			// Trying to get an asychronous RW lock from within a read lock should succeed.
			tsdl.m_nTestCase = 3;
			CheckHr(hr = m_qtxs->RequestLock(dwLockFlags, &hrLock));
			unitpp::assert_eq("Upgrade lock OK", S_OK, tsdl.m_hrCall);
			unitpp::assert_eq("Upgrade lock information message",
				TS_S_ASYNC, tsdl.m_hrSession);
			unitpp::assert_eq("Double async lock outer call OK", S_OK, hr);
			unitpp::assert_true("lock upgrade worked", tsdl.GetLockGranted());

			CheckHr(hr = m_qtxs->UnadviseSink(&tsdl));
			unitpp::assert_eq("UnadviseSink(&tsdl)", S_OK, hr);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::QueryInsert().
		--------------------------------------------------------------------------------------*/
		void testQueryInsert()
		{
			if (!m_fTestable)
				return;
			MakeStringList1();
			IVwSelectionPtr qselTemp;
			// Put insertion point at the beginning of the view
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

			HRESULT hr = S_OK;
			LONG ichStart;
			LONG ichEnd;
			CheckHr(hr = m_qtxs->QueryInsert(0, 0, 99, &ichStart, &ichEnd));
			unitpp::assert_eq("Simplest QueryInsert(0,0,99) hr", S_OK, hr);
			unitpp::assert_eq("Simplest QueryInsert(0,0,99) ichStart", 0, ichStart);
			unitpp::assert_eq("Simplest QueryInsert(0,0,99) ichEnd", 0, ichEnd);

			CheckHr(hr = m_qtxs->QueryInsert(s_cchPara1, s_cchPara1, 99, &ichStart, &ichEnd));
			unitpp::assert_eq("Simplest QueryInsert(s_cchPara1,s_cchPara1,99) hr", S_OK, hr);
			unitpp::assert_eq("Simplest QueryInsert(s_cchPara1,s_cchPara1,99) ichStart",
				s_cchPara1, ichStart);
			unitpp::assert_eq("Simplest QueryInsert(s_cchPara1,s_cchPara1,99) ichEnd",
				s_cchPara1, ichEnd);

			CheckHr(hr = m_qtxs->QueryInsert(0, s_cchPara1, 99, &ichStart, &ichEnd));
			unitpp::assert_eq("Simplest QueryInsert(0,s_cchPara1,99) hr", S_OK, hr);
			unitpp::assert_eq("Simplest QueryInsert(0,s_cchPara1,99) ichStart",
				0, ichStart);
			unitpp::assert_eq("Simplest QueryInsert(0,s_cchPara1,99) ichEnd",
				s_cchPara1, ichEnd);

			assert_exceptionHr("QueryInsert(999,1999,99) hr", E_INVALIDARG,
				m_qtxs->QueryInsert(999, 1999, 99, &ichStart, &ichEnd));

			Make2ParaSel(0, 0, 1, 1);
			CheckHr(hr = m_qtxs->QueryInsert(s_cchPara1 / 2, s_cchPara1 + s_cchPara2 / 2, 99,
				&ichStart, &ichEnd));
			unitpp::assert_eq("2 para QueryInsert(mid1st,mid2nd,99) hr", S_OK, hr);
			unitpp::assert_eq("2 para QueryInsert(mid1st,mid2nd,99) ichStart",
				s_cchPara1 / 2, ichStart);
			unitpp::assert_eq("2 para QueryInsert(mid1st,mid2nd,99) ichEnd",
				s_cchPara1 + s_cchPara2 / 2, ichEnd);

			// TODO JohnT: test cases (and implementation!) selections that can't be entirely
			// replaced, and (if in any way different) selections where the end point is a
			// paragraph embedded in another paragraph or in a table.
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetSelection() when we don't have a selection.
		--------------------------------------------------------------------------------------*/
		void testGetSelection_NoSelection()
		{
			if (!m_fTestable)
				return;
			MakeStringList1();

			TS_SELECTION_ACP tsa;
			ulong ctsa;
			IVwSelectionPtr qselTemp;

			// Verify that not having a selection doesn't cause a crash.
			LockGetSelection lgs0(m_qtxs, &tsa, &ctsa);
			unitpp::assert_eq("Premature GetSelection() hrLock", S_OK, lgs0.m_hrLock);
			unitpp::assert_eq("Premature GetSelection() start", 0, tsa.acpStart);
			unitpp::assert_eq("Premature GetSelection() end", 0, tsa.acpEnd);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetSelection() when the IP is at the beginning of the view.
		--------------------------------------------------------------------------------------*/
		void testGetSelection_IpBeginning()
		{
			if (!m_fTestable)
				return;
			MakeStringList1();

			TS_SELECTION_ACP tsa;
			ulong ctsa;
			IVwSelectionPtr qselTemp;

			// IP at beginning of view.
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));
			LockGetSelection lgs1(m_qtxs, &tsa, &ctsa);
			unitpp::assert_eq("GetSelection(0,0) hr", S_OK, lgs1.m_hrLock);
			unitpp::assert_eq("GetSelection(0,0) start", 0, tsa.acpStart);
			unitpp::assert_eq("GetSelection(0,0) end", 0, tsa.acpEnd);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetSelection() when the IP is at the end of the view.
		--------------------------------------------------------------------------------------*/
		void testGetSelection_IpAtEnd()
		{
			if (!m_fTestable)
				return;
			MakeStringList1();

			TS_SELECTION_ACP tsa;
			ulong ctsa;
			IVwSelectionPtr qselTemp;

			// IP at end of view.
			CheckHr(m_qrootb->MakeSimpleSel(false, true, false, true, &qselTemp));
			//MakeSelection(1, s_cchPara2, s_cchPara2);
			LockGetSelection lgs2(m_qtxs, &tsa, &ctsa);
			unitpp::assert_eq("GetSelection(end,end) hr", S_OK, lgs2.m_hrLock);
			unitpp::assert_eq("GetSelection(end,end) start", s_cchPara2, tsa.acpStart);
			unitpp::assert_eq("GetSelection(end,end) end", s_cchPara2, tsa.acpEnd);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetSelection() when all of the first paragraph is selected
		--------------------------------------------------------------------------------------*/
		void testGetSelection_FirstParagraph()
		{
			if (!m_fTestable)
				return;
			MakeStringList1();

			TS_SELECTION_ACP tsa;
			ulong ctsa;
			IVwSelectionPtr qselTemp;

			// Select all of first paragraph
			CheckHr(m_qrootb->MakeSimpleSel(true, true, true, true, &qselTemp));
			LockGetSelection lgs3(m_qtxs, &tsa, &ctsa);
			unitpp::assert_eq("GetSelection(para1) hr", S_OK, lgs3.m_hrLock);
			unitpp::assert_eq("GetSelection(para1) start", 0, tsa.acpStart);
			unitpp::assert_eq("GetSelection(para1) end", s_cchPara1, tsa.acpEnd);
			unitpp::assert_eq("GetSelection(para1) active end", TS_AE_END, tsa.style.ase);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetSelection() with a selection in first para with end before
			anchor.
		--------------------------------------------------------------------------------------*/
		void testGetSelection_EndBeforeAnchor()
		{
			if (!m_fTestable)
				return;
			MakeStringList1();

			TS_SELECTION_ACP tsa;
			ulong ctsa;
			IVwSelectionPtr qselTemp;

			// Make an arbitrary selection in first para with end before anchor
			MakeSelection(0, 15, 10);
			LockGetSelection lgs4(m_qtxs, &tsa, &ctsa);
			unitpp::assert_eq("GetSelection(reverse) hr", S_OK, lgs4.m_hrLock);
			unitpp::assert_eq("GetSelection(reverse) start", 10, tsa.acpStart);
			unitpp::assert_eq("GetSelection(reverse) end", 15, tsa.acpEnd);
			unitpp::assert_eq("GetSelection(reverse) active end", TS_AE_START, tsa.style.ase);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetSelection() for multi-paragraph selections starting and
			ending in the middle of a paragraph.
		--------------------------------------------------------------------------------------*/
		void testGetSelection_Sel2Para()
		{
			if (!m_fTestable)
				return;

			MakeStringList2();
			MakeSel2Para();	// 1, 4; 2, 10

			TS_SELECTION_ACP tsa;
			ulong ctsa;
			LockGetSelection lgs1(m_qtxs, &tsa, &ctsa);
			unitpp::assert_eq("GetSelection(Test2Para) hr", S_OK, lgs1.m_hrLock);
			unitpp::assert_eq("GetSelection(Test2Para) start", 4, tsa.acpStart);
			unitpp::assert_eq("GetSelection(Test2Para) end", s_cchPara2 + s_cchParaBreak + 10,
				tsa.acpEnd);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetSelection() for multi-paragraph selections where the end is
			before the anchor.
		--------------------------------------------------------------------------------------*/
		void testGetSelection_Sel2ParaRev()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();

			TS_SELECTION_ACP tsa;
			ulong ctsa;

			MakeSel2ParaRev();
			LockGetSelection lgs2(m_qtxs, &tsa, &ctsa);
			unitpp::assert_eq("GetSelection(Test2Para) hr", S_OK, lgs2.m_hrLock);
			unitpp::assert_eq("GetSelection(Test2Para) start", 4, tsa.acpStart);
			unitpp::assert_eq("GetSelection(Test2Para) end", s_cchPara2 + s_cchParaBreak + 10,
				tsa.acpEnd);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetSelection() for multi-paragraph selections where the selection
			ends in an empty paragraph.
		--------------------------------------------------------------------------------------*/
		void testGetSelection_SelToEmpty()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();

			TS_SELECTION_ACP tsa;
			ulong ctsa;

			MakeSelToEmpty();
			LockGetSelection lgs3(m_qtxs, &tsa, &ctsa);
			unitpp::assert_eq("GetSelection(Test2Para) hr", S_OK, lgs3.m_hrLock);
			unitpp::assert_eq("GetSelection(Test2Para) start", 5, tsa.acpStart);
			unitpp::assert_eq("GetSelection(Test2Para) end",
				s_cchPara2 + s_cchParaBreak, // impl leaves out contents of para 3
				tsa.acpEnd);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetSelection() for multi-paragraph selections where the selection
			starts in an empty paragraph.
		--------------------------------------------------------------------------------------*/
		void testGetSelection_SelFromEmpty()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();

			TS_SELECTION_ACP tsa;
			ulong ctsa;

			MakeSelFromEmpty();
			LockGetSelection lgs4(m_qtxs, &tsa, &ctsa);
			unitpp::assert_eq("GetSelection(Test2Para) hr", S_OK, lgs4.m_hrLock);
			unitpp::assert_eq("GetSelection(Test2Para) start", 0, tsa.acpStart);
			unitpp::assert_eq("GetSelection(Test2Para) end", s_cchPara4 + s_cchParaBreak + 6,
				tsa.acpEnd);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetSelection() for multi-paragraph selections where the selection
			spans multiple paragraphs.
		--------------------------------------------------------------------------------------*/
		void testGetSelection_SelLong()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();

			TS_SELECTION_ACP tsa;
			ulong ctsa;

			MakeSelLong();
			LockGetSelection lgs5(m_qtxs, &tsa, &ctsa);
			unitpp::assert_eq("GetSelection(Test2Para) hr", S_OK, lgs5.m_hrLock);
			unitpp::assert_eq("GetSelection(Test2Para) start", 1, tsa.acpStart);
			unitpp::assert_eq("GetSelection(Test2Para) end", s_cchPara1 + s_cchParaBreak + 7,
				tsa.acpEnd);
		}

		///*--------------------------------------------------------------------------------------
		//	Test VwTextStore::SetSelection().
		//--------------------------------------------------------------------------------------*/
		//void testSetSelection()
		//{
		//	if (!m_fTestable)
		//		return;
		//	MakeStringList2();
		//	TS_SELECTION_ACP tsa;

		//	// Verify what happens before we have a real selection.
		//	// This test is currently disabled because of TE-6420/LT-7487: this condition
		//	// can happen in real life and messes up if we throw an exception (see comment
		//	// in VwTextStore::CreateNewSelection)
		//	//tsa.acpStart = 0;
		//	//tsa.acpEnd = 1;
		//	//tsa.style.ase = TS_AE_END;
		//	//LockSetSelection lss0(m_qtxs, &tsa);
		//	//unitpp::assert_eq("Premature SetSelection(0,1) hrLock",
		//	//	TS_E_INVALIDPOS, lss0.m_hrLock);

		//	// This test is currently disabled because I (JT) decided to allow making a (0,0)
		//	// selection even when there is no current selection, since that is the state
		//	// that other methods simulate when there is no selection.
		//	//tsa.acpStart = 0;
		//	//tsa.acpEnd = 0;
		//	//tsa.style.ase = TS_AE_END;
		//	//LockSetSelection lss0a(m_qtxs, &tsa);
		//	//unitpp::assert_eq("Premature SetSelection(0,0) hrLock", E_FAIL, lss0a.m_hrLock);
		//}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::SetSelection() when we have a simple range selection inside of
			a paragraph.
		--------------------------------------------------------------------------------------*/
		void testSetSelection_SimpleRange()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_SELECTION_ACP tsa;

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;

			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));
			tsa.acpStart = 7;
			tsa.acpEnd = 9;
			tsa.style.ase = TS_AE_END;
			LockSetSelection lss1(m_qtxs, &tsa);
			VerifySelection(0, 7, 9, "First SetSelection");
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::SetSelection() when we have a simple range selection inside of
			a paragraph that has then end before the anchor.
		--------------------------------------------------------------------------------------*/
		void testSetSelection_ReverseRange()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_SELECTION_ACP tsa;

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

			tsa.acpStart = 6;
			tsa.acpEnd = 10;
			tsa.style.ase = TS_AE_START;
			LockSetSelection lss2(m_qtxs, &tsa);
			VerifySelection(0, 10, 6, "Second SetSelection");
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::SetSelection() if we have a selection between two paragraphs.
		--------------------------------------------------------------------------------------*/
		void testSetSelection_Sel2Para()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_SELECTION_ACP tsa;

			MakeSel2Para();	// 1, 4; 2, 10
			tsa.acpStart = 3;
			tsa.acpEnd = s_cchPara2 + s_cchParaBreak + 11;
			tsa.style.ase = TS_AE_END;
			LockSetSelection lss3(m_qtxs, &tsa);
			unitpp::assert_eq("Third SetSelection hrLock", S_OK, lss3.m_hrLock);
			VerifySelection(1, 3, 2, 11, "Third SetSelection");
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::SetSelection() if we have a selection between two paragraphs with
			the end before the anchor.
		--------------------------------------------------------------------------------------*/
		void testSetSelection_Sel2ParaRev()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_SELECTION_ACP tsa;

			MakeSel2Para();	// 1, 4; 2, 10
			tsa.acpStart = 3;
			tsa.acpEnd = s_cchPara2 + s_cchParaBreak + 11;
			tsa.style.ase = TS_AE_START;
			LockSetSelection lss4(m_qtxs, &tsa);
			unitpp::assert_eq("hrLock", S_OK, lss4.m_hrLock);
			VerifySelection(2, 11, 1, 3, "Fourth SetSelection");
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::SetSelection() if we make a selection that ends in an empty
			paragraph.
		--------------------------------------------------------------------------------------*/
		void testSetSelection_SelToEmpty()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_SELECTION_ACP tsa;

			MakeSelToEmpty();
			tsa.acpStart = s_cchPara2 + s_cchParaBreak;
			tsa.acpEnd = s_cchPara2 + s_cchParaBreak;
			tsa.style.ase = TS_AE_START;
			LockSetSelection lss5(m_qtxs, &tsa);
			unitpp::assert_eq("hrLock", S_OK, lss5.m_hrLock);
			VerifySelection(3, 0, 3, 0, "Fifth SetSelection");
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText(): Verify what happens before we have a real selection
		--------------------------------------------------------------------------------------*/
		void testGetText_NoSelection()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_RUNINFO tri;
			const ULONG kcch1 = 30;
			OLECHAR rgch1[kcch1 + 1];

			// Verify what happens before we have a real selection.
			LockGetText lgt0(m_qtxs, 0, -1, rgch1, kcch1, &tri, 1);
			unitpp::assert_eq("Should return E_FAIL if we don't have a seletion",
				E_FAIL, lgt0.m_hrLock);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText() when we try to get the entire text
		--------------------------------------------------------------------------------------*/
		void testGetText_AllAutoEnd_SmallBuffer()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_RUNINFO tri;
			const ULONG kcch1 = 30;
			OLECHAR rgch1[kcch1 + 1];
			ULONG cch;

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));
			cch = min( kcch1, (ULONG)wcslen(s_rgpsz1[0]));
			LockGetText lgt1(m_qtxs, 0, -1, rgch1, kcch1 + 1, &tri, 1);
			unitpp::assert_eq("Should succeed", S_OK, lgt1.m_hrLock);
			unitpp::assert_eq("GetText(0,-1) lim ichNext", (LONG)cch, lgt1.m_ichNext);
			unitpp::assert_true("GetText(0,-1) lim rgch1",
				wcsncmp(rgch1, s_rgpsz1[0], cch) == 0);
			unitpp::assert_eq("GetText(0,-1) lim cchOut", cch, lgt1.m_cchOut);
			unitpp::assert_eq("GetText(0,-1) lim ctriOut", (ULONG)1, lgt1.m_ctriOut);
			unitpp::assert_eq("GetText(0,-1) lim  run count", cch, tri.uCount);
			unitpp::assert_eq("GetText(0,-1) lim run type", TS_RT_PLAIN, tri.type);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText() when we try to get the entire text.
		--------------------------------------------------------------------------------------*/
		void testGetText_AllAutoEnd_LargeBuffer()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_RUNINFO tri;
			ULONG cch;
			const int kcch2 = 1000;
			OLECHAR rgch2[kcch2];

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

			cch = wcslen(s_rgpsz1[0]);
			LockGetText lgt2(m_qtxs, 0, -1, rgch2, kcch2, &tri, 1);
			unitpp::assert_eq("Should succeed", S_OK, lgt2.m_hrLock);
			unitpp::assert_eq("GetText(0,-1) ichNext", (LONG)cch, lgt2.m_ichNext);
			unitpp::assert_true("GetText(0,-1) rgch2",
				wcsncmp(rgch2, s_rgpsz1[0], cch) == 0);
			unitpp::assert_eq("GetText(0,-1) cchOut", cch, lgt2.m_cchOut);
			unitpp::assert_eq("GetText(0,-1) ctriOut", (ULONG)1, lgt2.m_ctriOut);
			unitpp::assert_eq("GetText(0,-1) run count", cch, tri.uCount);
			unitpp::assert_eq("GetText(0,-1) run type", TS_RT_PLAIN, tri.type);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText() when we try to get only part of the text from the middle
			of the text.
		--------------------------------------------------------------------------------------*/
		void testGetText_PartOfText()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_RUNINFO tri;
			ULONG cch;
			const int kcch2 = 1000;
			OLECHAR rgch2[kcch2];

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

			cch = 10 - 2;
			LockGetText lgt3(m_qtxs, 2, 10, rgch2, kcch2, &tri, 1);
			unitpp::assert_eq("Should succeed", S_OK, lgt3.m_hrLock);
			unitpp::assert_eq("GetText(2,10) ichNext", (LONG)10, lgt3.m_ichNext);
			unitpp::assert_true("GetText(2,10) rgch2",
				wcsncmp(rgch2, s_rgpsz1[0] + 2, cch) == 0);
			unitpp::assert_eq("GetText(2,10) cchOut", cch, lgt3.m_cchOut);
			unitpp::assert_eq("GetText(2,10) ctriOut", (ULONG)1, lgt3.m_ctriOut);
			unitpp::assert_eq("GetText(2,10) run count", cch, tri.uCount);
			unitpp::assert_eq("GetText(2,10) run type", TS_RT_PLAIN, tri.type);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText() when we try to get text where the start position is
			equal to the end position so that we get an empty string.
		--------------------------------------------------------------------------------------*/
		void testGetText_StartEqualsEnd()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_RUNINFO tri;
			ULONG cch;
			const int kcch2 = 1000;
			OLECHAR rgch2[kcch2];

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));
			cch = 0;
			LockGetText lgt4(m_qtxs, 3, 3, rgch2, kcch2, &tri, 1);
			unitpp::assert_eq("Should succeed", S_OK, lgt4.m_hrLock);
			unitpp::assert_eq("GetText(3,3) ichNext", (LONG)3, lgt4.m_ichNext);
			unitpp::assert_eq("GetText(3,3) cchOut", cch, lgt4.m_cchOut);
			unitpp::assert_eq("GetText(3,3) ctriOut", (ULONG)1, lgt4.m_ctriOut);
			unitpp::assert_eq("GetText(3,3) run count", cch, tri.uCount);
			unitpp::assert_eq("GetText(3,3) run type", TS_RT_PLAIN, tri.type);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText() when we try to get text from the end.
		--------------------------------------------------------------------------------------*/
		void testGetText_AtEnd()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_RUNINFO tri;
			ULONG cch;
			const int kcch2 = 1000;
			OLECHAR rgch2[kcch2];

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));
			ULONG cchDocLen =  wcslen(s_rgpsz1[0]);
			cch = 0;
			LockGetText lgt5(m_qtxs, cchDocLen, cchDocLen, rgch2, kcch2, &tri, 1);
			unitpp::assert_eq("Should succeed", S_OK, lgt5.m_hrLock);
			unitpp::assert_eq("GetText(lim,lim) ichNext", (LONG)cchDocLen, lgt5.m_ichNext);
			unitpp::assert_eq("GetText(lim,lim) cchOut", cch, lgt5.m_cchOut);
			unitpp::assert_eq("GetText(lim,lim) ctriOut", (ULONG)0, lgt5.m_ctriOut);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText() when the selection spans two paragraphs.
		--------------------------------------------------------------------------------------*/
		void testGetText_SelectionAcross2Paras()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			const int kcch2 = 1000;
			OLECHAR rgch2[kcch2];

			StrUni stu;
			MakeSel2Para();
			verifyGetText2Para(kcch2 - 1, rgch2, "MakeSel2Para");
			verifyGetText2Para(100, rgch2, "MakeSel2Para");
			verifyGetText2Para(70, rgch2, "MakeSel2Para");
			verifyGetText2Para(60, rgch2, "MakeSel2Para");
			verifyGetText2Para(50, rgch2, "MakeSel2Para");
			verifyGetText2Para(40, rgch2, "MakeSel2Para");
			verifyGetText2Para(30, rgch2, "MakeSel2Para");
			verifyGetText2Para(20, rgch2, "MakeSel2Para");
			verifyGetText2Para(10, rgch2, "MakeSel2Para");
			verifyGetText2Para(8, rgch2, "MakeSel2Para");
			verifyGetText2Para(4, rgch2, "MakeSel2Para");
			verifyGetText2Para(2, rgch2, "MakeSel2Para");
			verifyGetText2Para(1, rgch2, "MakeSel2Para");
			// Test before, after, and in the middle of the paragraph break.
			verifyGetText2Para(s_cchPara2 - 7, rgch2, "MakeSel2Para");
			verifyGetText2Para(s_cchPara2 - 6, rgch2, "MakeSel2Para");
			verifyGetText2Para(s_cchPara2 - 5, rgch2, "MakeSel2Para");
			verifyGetText2Para(s_cchPara2 - 4, rgch2, "MakeSel2Para");
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText() when we have a reverse (end before anchor) selection
			that spans two paragraphs.
		--------------------------------------------------------------------------------------*/
		void testGetText_SelectionSpan2ParasRev()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			const int kcch2 = 1000;
			OLECHAR rgch2[kcch2];

			MakeSel2ParaRev();
			verifyGetText2Para(kcch2 - 1, rgch2, "MakeSel2ParaRev");
			verifyGetText2Para(10, rgch2, "MakeSel2ParaRev");
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText() when we have a long selection.
		--------------------------------------------------------------------------------------*/
		void testGetText_MakeSelLong()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_RUNINFO tri;
			ULONG cch;
			const int kcch2 = 1000;
			OLECHAR rgch2[kcch2];

			StrUni stu;
			MakeSelLong();
			stu.Format(L"%s%n", s_rgpsz2[0]);
			stu.Append(s_rgpsz2[4], 10);
			cch = stu.Length();
			LockGetText lgt8(m_qtxs, 0, s_cchPara1 + s_cchParaBreak + 10, rgch2, kcch2, &tri,
				1);
			unitpp::assert_eq("Should succeed", S_OK, lgt8.m_hrLock);
			unitpp::assert_eq("GetText(MakeSelLong) ichNext", (LONG)cch, lgt8.m_ichNext);
			unitpp::assert_true("GetText(MakeSelLong) text",
				wcsncmp(rgch2, stu.Chars(), cch) == 0);
			unitpp::assert_eq("GetText(MakeSelLong) cchOut", cch, lgt8.m_cchOut);
			unitpp::assert_eq("GetText(MakeSelLong) ctriOut", (ULONG)1, lgt8.m_ctriOut);
			unitpp::assert_eq("GetText(MakeSelLong) run count", cch, tri.uCount);
			unitpp::assert_eq("GetText(MakeSelLong) run type", TS_RT_PLAIN, tri.type);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText().
		--------------------------------------------------------------------------------------*/
		void testGetText_MakeSelLong_Short()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_RUNINFO tri;
			ULONG cch;
			const int kcch2 = 1000;
			OLECHAR rgch2[kcch2];

			StrUni stu;
			MakeSelLong();
			stu.Assign(s_rgpsz2[0]);
			cch = stu.Length();
			LockGetText lgt9(m_qtxs, 0, s_cchPara1 + s_cchParaBreak + 10, rgch2, cch + 1, &tri, 1);
			unitpp::assert_eq("Should succeed", S_OK, lgt9.m_hrLock);
			unitpp::assert_eq("GetText(MakeSelLong) short ichNext", (LONG)cch, lgt9.m_ichNext);
			unitpp::assert_true("GetText(MakeSelLong) short text",
				wcsncmp(rgch2, stu.Chars(), cch) == 0);
			unitpp::assert_eq("GetText(MakeSelLong) short cchOut", cch, lgt9.m_cchOut);
			unitpp::assert_eq("GetText(MakeSelLong) short ctriOut", (ULONG)1, lgt9.m_ctriOut);
			unitpp::assert_eq("GetText(MakeSelLong) short run count", cch, tri.uCount);
			unitpp::assert_eq("GetText(MakeSelLong) short run type", TS_RT_PLAIN, tri.type);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText().
		--------------------------------------------------------------------------------------*/
		void testGetText_MakeSelLong_WithCrLf()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_RUNINFO tri;
			const int kcch2 = 1000;
			OLECHAR rgch2[kcch2];

			StrUni stu;
			MakeSelLong();
			LockGetText lgt10(m_qtxs, s_cchPara1, s_cchPara1 + s_cchParaBreak + 10, rgch2,
				s_cchParaBreak + 1, &tri, 1);
			unitpp::assert_eq("Should succeed", S_OK, lgt10.m_hrLock);
			unitpp::assert_eq("GetText(MakeSelLong) crlf ichNext",
				(LONG)s_cchPara1 + s_cchParaBreak, lgt10.m_ichNext);
			unitpp::assert_true("GetText(MakeSelLong) crlf text",
				wcsncmp(rgch2, s_stuParaBreak.Chars(), s_cchParaBreak) == 0);
			unitpp::assert_eq("GetText(MakeSelLong) crlf cchOut",
				(ULONG)s_cchParaBreak, lgt10.m_cchOut);
			unitpp::assert_eq("GetText(MakeSelLong) crlf ctriOut", (ULONG)1, lgt10.m_ctriOut);
			unitpp::assert_eq("GetText(MakeSelLong) crlf run count",
				(ULONG)s_cchParaBreak, tri.uCount);
			unitpp::assert_eq("GetText(MakeSelLong) crlf run type", TS_RT_PLAIN, tri.type);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText().
		--------------------------------------------------------------------------------------*/
		void testGetText_MakeSelLong_Range()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			TS_RUNINFO tri;
			ULONG cch;
			const int kcch2 = 1000;
			OLECHAR rgch2[kcch2];

			StrUni stu;
			MakeSelLong();
			stu.Assign(s_rgpsz2[4] + 1, 9);
			cch = stu.Length();
			LockGetText lgt11(m_qtxs,
				s_cchPara1 + s_cchParaBreak + 1, s_cchPara1 + s_cchParaBreak + 10,
				rgch2, kcch2, &tri, 1);
			unitpp::assert_eq("Should succeed", S_OK, lgt11.m_hrLock);
			unitpp::assert_eq("GetText(MakeSelLong) ichNext",
				(LONG)(s_cchPara1 + s_cchParaBreak + 10), lgt11.m_ichNext);
			unitpp::assert_true("GetText(MakeSelLong) text",
				wcsncmp(rgch2, stu.Chars(), cch) == 0);
			unitpp::assert_eq("GetText(MakeSelLong) cchOut", cch, lgt11.m_cchOut);
			unitpp::assert_eq("GetText(MakeSelLong) ctriOut", (ULONG)1, lgt11.m_ctriOut);
			unitpp::assert_eq("GetText(MakeSelLong) run count", cch, tri.uCount);
			unitpp::assert_eq("GetText(MakeSelLong) run type", TS_RT_PLAIN, tri.type);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText(): should return NFC with proper run info.
		--------------------------------------------------------------------------------------*/
		void testGetText_ReturnsNfc()
		{
			if (!m_fTestable)
				return;
			// Test data:
			// index:	 0      1      2      3      4      5      6
			// NFD:      \U1112 \U1161 \U11ab \U0020 \U1112 \U1161 \U11ab
			// expected:
			// NFC:      \Ud55c               \U0020 \Ud55c
			// Run: len: 1      2             2             2
			//      typ: plain  hidden        plain         hidden

			MakeStringList(s_rgpsz3);
			TS_RUNINFO tri[10];
			ULONG cchNfd;
			const int kcch2 = 1000;
			OLECHAR rgch2[kcch2];

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

			cchNfd = wcslen(s_rgpsz3[0]);
			ULONG cchNfc = wcslen(s_rgpszExpected3);
			LockGetText lgt(m_qtxs, 0, -1, rgch2, kcch2, tri, 10);
			unitpp::assert_eq("Should succeed", S_OK, lgt.m_hrLock);
			unitpp::assert_eq("GetText(0,-1) ichNext", (LONG)cchNfc, lgt.m_ichNext);
			unitpp::assert_eq("GetText(0,-1) cchOut", cchNfc, lgt.m_cchOut);
			unitpp::assert_true("GetText(0,-1) rgch2",
				wcsncmp(rgch2, s_rgpszExpected3, cchNfc) == 0);
			unitpp::assert_eq("GetText(0,-1) ctriOut", (ULONG)1, lgt.m_ctriOut);
			unitpp::assert_eq("GetText(0,-1) run 0 count",
				s_triExpected3[0].uCount, tri[0].uCount);
			unitpp::assert_eq("GetText(0,-1) run 0 type", s_triExpected3[0].type, tri[0].type);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetText(): should return NFC with proper run info.
			This tests the case where we don't want to get any run info.
		--------------------------------------------------------------------------------------*/
		void testGetText_ReturnsNfc_NoRunInfo()
		{
			if (!m_fTestable)
				return;

			MakeStringList(s_rgpsz3);
			ULONG cchNfd;
			const int kcch2 = 1000;
			OLECHAR rgch2[kcch2];

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

			cchNfd = wcslen(s_rgpsz3[0]);
			ULONG cchNfc = wcslen(s_rgpszExpected3);
			LockGetText lgt(m_qtxs, 0, -1, rgch2, kcch2, NULL, 0);
			unitpp::assert_eq("Should succeed", S_OK, lgt.m_hrLock);
			unitpp::assert_eq("GetText(0,-1) ichNext", (LONG)cchNfc, lgt.m_ichNext);
			unitpp::assert_eq("GetText(0,-1) cchOut", cchNfc, lgt.m_cchOut);
			unitpp::assert_true("GetText(0,-1) rgch2",
				wcsncmp(rgch2, s_rgpszExpected3, cchNfc) == 0);
			unitpp::assert_eq("GetText(0,-1) ctriOut", (ULONG)0, lgt.m_ctriOut);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::SetText() with single paragraph selections.
		--------------------------------------------------------------------------------------*/
		void testSetText()
		{
			if (!m_fTestable)
				return;
			MakeStringList1();

			TS_TEXTCHANGE ttc1 = { 0 };

			// Verify that we don't have a document before we make a selection, and that it
			// doesn't cause a worse crash.
			const OLECHAR * pszText0 = L"Hello";
			LockSetText xlst0(m_qtxs, 0, 0, pszText0, &ttc1);
			unitpp::assert_eq("Premature SetText(0,0) hrLock", E_FAIL, xlst0.m_hrLock);

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

			const OLECHAR * pszText1 = L"This is a test.";
			int cchText1 = wcslen(pszText1);
			int s_cchPara1 = wcslen(s_rgpsz1[0]);
			LockSetText xlst1(m_qtxs, 0, s_cchPara1, pszText1, &ttc1);
			unitpp::assert_eq("SetText(0, lim) acpStart", 0, ttc1.acpStart);
			unitpp::assert_eq("SetText(0, lim) acpOldEnd", s_cchPara1, ttc1.acpOldEnd);
			unitpp::assert_eq("SetText(0, lim) acpNewEnd", cchText1, ttc1.acpNewEnd);
			VerifySelection(0, 0, cchText1, "SetText(0, lim)");
			VerifyParaContents(0, pszText1, "SetText(0, lim)");

			const OLECHAR * pszText2 = L"abcde";
			int cchText2 = wcslen(pszText2);
			StrUni stuContents(pszText2);
			stuContents.Append(pszText1);
			LockSetText xlst2(m_qtxs, 0, 0, pszText2, &ttc1);
			unitpp::assert_eq("SetText(0, 0) acpStart", 0, ttc1.acpStart);
			unitpp::assert_eq("SetText(0, 0) acpOldEnd", 0, ttc1.acpOldEnd);
			unitpp::assert_eq("SetText(0, 0) acpNewEnd", cchText2, ttc1.acpNewEnd);
			VerifySelection(0, 0, cchText2, "SetText(0, 0)");
			VerifyParaContents(0, stuContents.Chars(), "SetText(0, 0)");

			const OLECHAR * pszText3 = L" xyz ";
			int cchText3 = wcslen(pszText3);
			stuContents.Replace(cchText2, cchText2, pszText3, cchText3);
			LockSetText xlst3(m_qtxs, cchText2, -1, pszText3, &ttc1);
			unitpp::assert_eq("SetText(5, -1) acpStart", cchText2, ttc1.acpStart);
			unitpp::assert_eq("SetText(5,-1) acpOldEnd", cchText2, ttc1.acpOldEnd);
			unitpp::assert_eq("SetText(5,-1) acpNewEnd", cchText2 + cchText3, ttc1.acpNewEnd);
			VerifySelection(0, cchText2, cchText2 + cchText3, "SetText(5,-1)");
			VerifyParaContents(0, stuContents.Chars(), "SetText(5,-1)");

			stuContents.Replace(cchText2, cchText2 + cchText3, L"", 0);
			LockSetText xlst4(m_qtxs, cchText2, cchText2 + cchText3, L"", &ttc1);
			unitpp::assert_eq("SetText(5, 10) acpStart", cchText2, ttc1.acpStart);
			unitpp::assert_eq("SetText(5,10) acpOldEnd", cchText2 + cchText3, ttc1.acpOldEnd);
			unitpp::assert_eq("SetText(5,10) acpNewEnd", cchText2, ttc1.acpNewEnd);
			VerifySelection(0, cchText2, cchText2, "SetText(5,10)");
			VerifyParaContents(0, stuContents.Chars(), "SetText(5,10)");

			const OLECHAR * pszText5 = L"Hello World";
			int cchText5 = wcslen(pszText5);
			int cchPara = stuContents.Length();
			stuContents.Replace(cchPara, cchPara, pszText5, cchText5);
			LockSetText xlst5(m_qtxs, cchPara, cchPara, pszText5, &ttc1);
			unitpp::assert_eq("SetText(lim,lim) acpStart", cchPara, ttc1.acpStart);
			unitpp::assert_eq("SetText(lim,lim) acpOldEnd", cchPara, ttc1.acpOldEnd);
			unitpp::assert_eq("SetText(lim,lim) acpNewEnd", cchPara + cchText5, ttc1.acpNewEnd);
			VerifySelection(0, cchPara, cchPara + cchText5, "SetText(lim,lim)");
			VerifyParaContents(0, stuContents.Chars(), "SetText(lim,lim)");
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::SetText() with multiparagraph selections.
		--------------------------------------------------------------------------------------*/
		void testSetTextMulti1()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();
			const OLECHAR * pszText5 = L"Hello World";
			TS_TEXTCHANGE ttc1 = { 0 };
			MakeSel2Para();
			StrUni stuContents(s_rgpsz2[1]);
			stuContents.Append(pszText5);
			LockSetText xlst(m_qtxs, s_cchPara2, s_cchPara2, pszText5, &ttc1);
			VerifyParaContents(1, stuContents.Chars(), "SetText(insert end para 2)");
			VerifyParaContents(2, s_rgpsz2[2], "SetText(insert end para 2)");
			int chvo;
			CheckHr(m_qsda->get_VecSize(m_hvoRoot, kflidStText_Paragraphs, &chvo));
			unitpp::assert_eq("SetText(insert end para 2) para list", m_vhvo.Size(), chvo);
		}

		// This tests using InsertTextAtSelection to insert multiple lines. Since it is implemented using OnTyping,
		// that isn't currently supported. In practice I don't believe TSF ever inserts newlines this way.
		//void testSetTextMulti2()
		//{
		//	if (!m_fTestable)
		//		return;
		//	MakeStringList2();
		//	TS_TEXTCHANGE ttc1 = { 0 };
		//	MakeSelLong();
		//	const OLECHAR * psz6 = L"Goodbye paras";
		//	const OLECHAR * psz7 = L"Here's a new one";
		//	const OLECHAR * psz8 = L"Start of new last para";
		//	StrUni stuInsert6;
		//	stuInsert6.Format(L"%s%n%s%n%s", psz6, psz7, psz8);
		//	int cchFrom = 5;
		//	int cchTo = s_cchPara1 + s_cchParaBreak + 4;
		//	//int cch7 = wcslen(psz7);
		//	int cch8 = wcslen(psz8);
		//	LockSetText xlst2(m_qtxs, cchFrom, cchTo, stuInsert6.Chars(), &ttc1);
		//	StrUni stuContents;
		//	stuContents.Append(s_rgpsz2[0], cchFrom); // Keep first 5 chars of p1
		//	stuContents.Append(psz6);
		//	VerifyParaContents(0, stuContents.Chars(), "SetText(mangle para 1)");
		//	stuContents.Clear();
		//	stuContents.Append(psz8);
		//	stuContents.Append(s_rgpsz2[4] + 4); // Keep all but first 4 chars of p4
		//	VerifyParaContents(2, stuContents.Chars(), "SetText(mangle para 3)");
		//	VerifyParaContents(1, psz7, "SetText(add para 2)");
		//	int chvo;
		//	CheckHr(m_qsda->get_VecSize(m_hvoRoot, kflidStText_Paragraphs, &chvo));
		//	unitpp::assert_eq("SetText(mangle para list)", 3, chvo);
		//	// Note that only the last para counts for the revised document.
		//	unitpp::assert_eq("SetText(mangle) acpStart", cchFrom, ttc1.acpStart);
		//	unitpp::assert_eq("SetText(mangle) acpOldEnd", cchTo, ttc1.acpOldEnd);
		//	unitpp::assert_eq("SetText(mangle) acpNewEnd", cch8, ttc1.acpNewEnd);
		//}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::SetText() replacing a paragraph break with nothing to join two
			paragraphs.
		--------------------------------------------------------------------------------------*/
		void testSetTextEmpty()
		{
			//_CrtSetDbgFlag(_CRTDBG_CHECK_ALWAYS_DF | _CRTDBG_DELAY_FREE_MEM_DF );
			if (!m_fTestable)
				return;
			MakeStringList2();
			Make2ParaSel(1, s_cchPara2, 2, 0);
			TS_TEXTCHANGE ttc1 = { 0 };
			LockSetText xlst1(m_qtxs, s_cchPara2, s_cchPara2 + s_cchParaBreak, L"", &ttc1);
			_CrtCheckMemory();
			StrUni stuNew(s_rgpsz2[1]);
			stuNew.Append(s_rgpsz2[2]);	// no newline between them!
			VerifyParaContents(1, stuNew.Chars(), "SetText(join paras)");
			VerifyParaContents(0, s_rgpsz2[0], "SetText(join paras) prev para");
			VerifyParaContents(2, s_rgpsz2[3], "SetText(join paras) next para");
		}

		// We no longer implement this method in C++
		///*--------------------------------------------------------------------------------------
		//	Test VwTextStore::GetFormattedText().
		//	Enhance: test with multi-paragraph selections.  We're not sure it's ever being used
		//	in the real world (Keyman and other IMEs).
		//--------------------------------------------------------------------------------------*/
		//void testGetFormattedText()
		//{
		//	if (!m_fTestable)
		//		return;
		//	MakeStringList1();
		//	// Need some selection to start with as it determines which paragraph.
		//	IVwSelectionPtr qselTemp;
		//	CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

		//	IDataObjectPtr qdobj;
		//	int s_cchPara1 = wcslen(s_rgpsz1[0]);
		//	LockGetFormattedText lgft1(m_qtxs, 0, s_cchPara1, &qdobj);
		//	unitpp::assert_eq("GetFormattedText hr", S_OK, lgft1.m_hrLock);

		//	HRESULT hr = S_OK;
		//	FORMATETC format;
		//	STGMEDIUM medium = { TYMED_ISTORAGE, NULL, NULL };

		//	// GetData() does not work directly for TsString type data, and GetDataHere()
		//	// works only if medium.pstg is already allocated.
		//	CheckHr(hr = ::StgCreateStorageEx(NULL, // create a temporary storage
		//		STGM_CREATE | STGM_READWRITE | STGM_SHARE_EXCLUSIVE,
		//		STGFMT_STORAGE, 0, NULL, NULL, IID_IStorage, (void **)&medium.pstg));
		//	unitpp::assert_true("IStorage created ok", medium.pstg != NULL);
		//	uint uFormat;
		//	{
		//		ILgTsDataObjectPtr qtsdo;
		//		qtsdo.CreateInstance(CLSID_LgTsDataObject);
		//		CheckHr(qtsdo->GetClipboardType(&uFormat));
		//	}
		//	format.cfFormat = static_cast<unsigned short>(uFormat);
		//	format.ptd = NULL;
		//	format.dwAspect = DVASPECT_CONTENT;
		//	format.lindex = -1;
		//	format.tymed = TYMED_ISTORAGE;
		//	CheckHr(hr = qdobj->GetDataHere(&format, &medium));
		//	unitpp::assert_eq("Data Obj has tss data", S_OK, hr);

		//	ILgTsStringPlusWssPtr qtssencs;
		//	qtssencs.CreateInstance(CLSID_LgTsStringPlusWss);
		//	CheckHr(qtssencs->Deserialize(medium.pstg));
		//	ITsStringPtr qtssDo;
		//	CheckHr(qtssencs->get_String(g_qwsf, &qtssDo));
		//	// There's an extra newline added to end of qtssDo because we're getting the entire
		//	// paragraph.
		//	VerifyParaContents(0, qtssDo, "data obj contains first para tss",
		//		s_stuParaBreak.Bstr());
		//	::ReleaseStgMedium(&medium);

		//	format.cfFormat = CF_UNICODETEXT;
		//	format.ptd = NULL;
		//	format.dwAspect = DVASPECT_CONTENT;
		//	format.lindex = -1;
		//	format.tymed = TYMED_HGLOBAL;
		//	CheckHr(hr = qdobj->GetData(&format, &medium));
		//	unitpp::assert_eq("Data Obj has uni data", S_OK, hr);
		//	unitpp::assert_eq("Data Obj has uni data2", (DWORD)TYMED_HGLOBAL, medium.tymed);
		//	unitpp::assert_true("Data Obj has uni data3", medium.hGlobal != NULL);

		//	const wchar * pwszDo = (const wchar *)::GlobalLock(medium.hGlobal);
		//	StrUni stu(pwszDo);
		//	::GlobalUnlock(medium.hGlobal);
		//	::ReleaseStgMedium(&medium);
		//	// Strip off the CRLF that our code adds.
		//	// Enhance JohnT: if we go multi-paragraph, or anything really uses this function,
		//	// we may have to consider more carefully whether including the crlf in this
		//	// case is right.
		//	stu.Replace(stu.Length() - 2, stu.Length(), L"", 0);
		//	VerifyParaContents(0, stu.Chars(), "data obj contains first para uni");
		//}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::InsertTextAtSelection().	This is largely tested by testing
			SetText(), which calls it internally.
		--------------------------------------------------------------------------------------*/
		void testInsertTextAtSelection()
		{
			if (!m_fTestable)
				return;
			MakeStringList1();

			LONG ichStart = -1;
			LONG ichEnd = -1;
			TS_TEXTCHANGE ttc = { (LONG)-1, (LONG)-1, (LONG)-1 };

			// Verify that we don't have a document before we make a selection, and that it
			// doesn't cause a worse crash.
			const OLECHAR * pszText0 = L"Hello";
			LockInsertTextAtSelection litas0(m_qtxs, 0, pszText0, &ichStart, &ichEnd, &ttc);
			unitpp::assert_eq("Premature InsertTextAtSelection() hrLock",
				E_FAIL, litas0.m_hrLock);

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

			OLECHAR * pszText1 = L"This is another test.";
			LockInsertTextAtSelection litas1(m_qtxs, TS_IAS_QUERYONLY, pszText1,
				&ichStart, &ichEnd, &ttc);
			VerifyParaContents(0, s_rgpsz1[0], "InsertTextAtSelection(TS_IAS_QUERYONLY) text");
			unitpp::assert_eq("InsertTextAtSelection(TS_IAS_QUERYONLY) ichStart",
				0, ichStart);
			unitpp::assert_eq("InsertTextAtSelection(TS_IAS_QUERYONLY) ichEnd",
				0, ichEnd);
			unitpp::assert_eq("InsertTextAtSelection(TS_IAS_QUERYONLY) acpStart",
				(LONG)-1, ttc.acpStart);
			unitpp::assert_eq("InsertTextAtSelection(TS_IAS_QUERYONLY) acpOldEnd",
				(LONG)-1, ttc.acpOldEnd);
			unitpp::assert_eq("InsertTextAtSelection(TS_IAS_QUERYONLY) acpNewEnd",
				(LONG)-1, ttc.acpNewEnd);

			OLECHAR * pszText2 = L"This is a test.";
			LONG cchText2 = wcslen(pszText2);
			ichStart = -1;
			ichEnd = -1;
			LockInsertTextAtSelection litas2(m_qtxs, TS_IAS_NOQUERY, pszText2,
				&ichStart, &ichEnd, &ttc);
			unitpp::assert_eq("InsertTextAtSelection(TS_IAS_NOQUERY) ichStart",
				(LONG)-1, ichStart);
			unitpp::assert_eq("InsertTextAtSelection(TS_IAS_NOQUERY) ichEnd",
				(LONG)-1, ichEnd);
			unitpp::assert_eq("InsertTextAtSelection(TS_IAS_NOQUERY) acpStart",
				0, ttc.acpStart);
			unitpp::assert_eq("InsertTextAtSelection(TS_IAS_NOQUERY) acpOldEnd",
				0, ttc.acpOldEnd);
			unitpp::assert_eq("InsertTextAtSelection(TS_IAS_NOQUERY) acpNewEnd",
				cchText2, ttc.acpNewEnd);
			StrUni stu(pszText2);
			stu.Append(s_rgpsz1[0]);
			VerifyParaContents(0, stu.Chars(), "InsertTextAtSelection(TS_IAS_NOQUERY) text");

			OLECHAR * pszText3 = L"This is still a test.";
			LONG cchText3 = wcslen(pszText3);
			ichStart = -1;
			ichEnd = -1;
			ttc.acpStart = -1;
			ttc.acpOldEnd = -1;
			ttc.acpNewEnd = -1;
			LockInsertTextAtSelection litas3(m_qtxs, 0, pszText3, &ichStart, &ichEnd, &ttc);
			unitpp::assert_eq("InsertTextAtSelection(0) ichStart", 0, ichStart);
			unitpp::assert_eq("InsertTextAtSelection(0) ichEnd", cchText3, ichEnd);
			unitpp::assert_eq("InsertTextAtSelection(0) acpStart", 0, ttc.acpStart);
			unitpp::assert_eq("InsertTextAtSelection(0) acpOldEnd", cchText2, ttc.acpOldEnd);
			unitpp::assert_eq("InsertTextAtSelection(0) acpNewEnd", cchText3, ttc.acpNewEnd);
			stu.Replace(0, cchText2, pszText3, cchText3);
			VerifyParaContents(0, stu.Chars(), "InsertTextAtSelection(0) text");
		}

		/*--------------------------------------------------------------------------------------
		Test VwRootBox::PropChanged(), that it calls NotifySelChange and resets the NormalizationCommitInProgress state
		iff m_fNormalizationCommitInProgress and HVO and PropTag both match what was those for the commit in progress.
		This is a test of VwRootBox, but it needs VwTextStoreTestSub, so we put it here.
		--------------------------------------------------------------------------------------*/
		void testVwRootBox_PropChanged()
		{
			HVO hvo = 1;
			PropTag tag = 2;
			VwRootBox* prootb = dynamic_cast<VwRootBox*>(m_qrootb.Ptr());
			VwTextStoreTestSub* ptxs = new VwTextStoreTestSub(prootb);
			prootb->m_qvim.Attach(ptxs);
			prootb->BeginNormalizationCommit(hvo, tag);
			ptxs->m_cCalledGetCurrentWS = 0;

			// NormalizationCommitInProgress and same HVO but different PropTag
			prootb->PropChanged(hvo, 10);
			unitpp::assert_eq("PropChanged(Different PropTag) ptxs->m_cCalledGetCurrentWS", 0, ptxs->m_cCalledGetCurrentWS);
			unitpp::assert_true("PropChanged(Different PropTag) prootb->m_fNormalizationCommitInProgress", prootb->m_fNormalizationCommitInProgress);

			// NormalizationCommitInProgress and same PropTag but different HVO
			prootb->PropChanged(10, tag);
			unitpp::assert_eq("PropChanged(Different HVO) ptxs->m_cCalledGetCurrentWS", 0, ptxs->m_cCalledGetCurrentWS);
			unitpp::assert_true("PropChanged(Different HVO) prootb->m_fNormalizationCommitInProgress", prootb->m_fNormalizationCommitInProgress);

			// test same HVO and PropTag, but no NormalizationCommitInProgress
			prootb->m_fNormalizationCommitInProgress = false;
			prootb->PropChanged(hvo, tag);
			unitpp::assert_eq("PropChanged(No NormalizationCommitInProgress) ptxs->m_cCalledGetCurrentWS",
				0, ptxs->m_cCalledGetCurrentWS);
			unitpp::assert_eq("PropChanged(No NormalizationCommitInProgress) prootb->m_hvoNormalizationCommitInProgress",
				hvo, prootb->m_hvoNormalizationCommitInProgress);
			unitpp::assert_eq("PropChanged(No NormalizationCommitInProgress) prootb->m_tagNormalizationCommitInProgress",
				tag, prootb->m_tagNormalizationCommitInProgress);
			prootb->m_fNormalizationCommitInProgress = true;

			// NormalizationCommitInProgress and same HVO and PropTag
			prootb->PropChanged(hvo, tag);
			unitpp::assert_eq("NormalizationCommitInProgress(correct) ptxs->m_cCalledGetCurrentWS",
				1, ptxs->m_cCalledGetCurrentWS);
			unitpp::assert_false("NormalizationCommitInProgress(correct) prootb->m_fNormalizationCommitInProgress",
				prootb->m_fNormalizationCommitInProgress);
		}

		/*--------------------------------------------------------------------------------------
		Test VwTextStore::OnSelectionChange() when the selection is near the end of a string
		that gets longer in normalization during commit.  This is really a test of VwRootBox,
		but it requires that we set up and monitor a Selection, which is more easily done here.
		--------------------------------------------------------------------------------------*/
		void testOnSelectionChange()
		{
			if (!m_fTestable)
				return;
			const OLECHAR * rgchKoreanTestData[] = {
				// These are Korean characters:
				L"\x110b\x1163" // the first two codepoints are one decomposed character
				L"\xbb44" // the third codepoint is one composed character; it will be decomposed into three codepoints
				// We will place the insertion point (IP) here (Anchor=End=3)
				L"\x110c", // the fourth codepoint is one atomic character.
				// This setup is important: We start with Anchor=End=3 (IP), Limit=4 (length).  When we NfdAndFixOffsets, the third codepoint is
				// decomposed into three, giving us       Anchor=End=5,      Limit=6.  This way, we can catch the following potential problems:
				// - Anchor is never adjusted and remains 3
				// - Anchor is adjusted twice, to 7, which will crash because 7 > Limit=6
				// - Anchor is bumped to the end of the string (possibly adjusted twice, but capped at Limit=6)
				NULL
			};
			MakeStringList(rgchKoreanTestData);

			// Select at the end of the string
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(false, true, false, true, &qselTemp));
			VwTextSelection *pselTemp = dynamic_cast<VwTextSelection *>(qselTemp.Ptr());

			int* pichLimEditProp = &(pselTemp->m_ichLimEditProp); // limit (end of the string)
			int* pichAnchor = &(pselTemp->m_ichAnchor); // Anchor ("selection start" or IP)
			int* pichEnd = &(pselTemp->m_ichEnd); // [selction] end (IP: same as Anchor for this test)
			*pichAnchor = *pichEnd = 3; // set the IP (hack)

			// Use the VwTextStoreTestSub to track whether we call GetWritingSystem (called by NotifySelChange) when we ResumePropChanges.
			VwTextStoreTestSub* ptxs = dynamic_cast<VwTextStoreTestSub*>(m_qtxs.Detach());
			m_qrootb->m_qvim.Attach(ptxs);

			pselTemp->StartEditing();
			ptxs->m_cCalledGetCurrentWS = 0;
			// suppressing immediate propChanged notifications makes the CachedDataAccess class behave
			// like the real FdoCache in a way that is crucial for the function we are testing here.
			m_qcda->SuppressPropChanges();
			CheckHr(pselTemp->CommitAndNotify(ksctSamePara, m_qrootb));
			m_qcda->ResumePropChanges(); // Resume retriggers NotifySelChange after it was postponed for a normalization commit.
			unitpp::assert_eq("OnSelectionChange(1) ptxs->m_cCalledGetCurrentWS", 1, ptxs->m_cCalledGetCurrentWS);
			unitpp::assert_false("OnSelectionChange(false) m_qrootb->m_fNormalizationCommitInProgress", m_qrootb->m_fNormalizationCommitInProgress);
			unitpp::assert_eq("OnSelectionChange(5) pichAnchor", 5, *pichAnchor);
			unitpp::assert_eq("OnSelectionChange(5) pichEnd", 5, *pichEnd);
			unitpp::assert_eq("OnSelectionChange(6) pichLimEditProp", 6, *pichLimEditProp);
		}

		/*--------------------------------------------------------------------------------------
		Test VwTextStore::GetEndACP() for single paragraph selections.
		--------------------------------------------------------------------------------------*/
		void testGetEndACP()
		{
			if (!m_fTestable)
				return;
			MakeStringList1();
			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

			LONG cch1 = -1;
			LockGetEndACP lgea1(m_qtxs, &cch1);
			unitpp::assert_eq("GetEndACP first para", (LONG)wcslen(s_rgpsz1[0]), cch1);

			CheckHr(m_qrootb->MakeSimpleSel(false, true, false, true, &qselTemp));
			LONG cch2 = -1;
			LockGetEndACP lgea2(m_qtxs, &cch2);
			unitpp::assert_eq("GetEndACP second para", (LONG)wcslen(s_rgpsz1[1]), cch2);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetEndACP() for multiple paragraph selections.
		--------------------------------------------------------------------------------------*/
		void testGetEndACP2()
		{
			if (!m_fTestable)
				return;
			MakeStringList2();

			LONG cch0 = -1;
			LockGetEndACP lgea0(m_qtxs, &cch0);
			unitpp::assert_eq("Premature GetEndACP()", (LONG)0, cch0);

			MakeSel2Para();
			LONG cch1 = -1;
			LockGetEndACP lgea1(m_qtxs, &cch1);
			unitpp::assert_eq("GetEndACP first 2-para",
				(LONG)(s_cchPara2 + s_cchParaBreak + s_cchPara3), cch1);

			MakeSel2ParaRev();
			LONG cch2 = -1;
			LockGetEndACP lgea2(m_qtxs, &cch2);
			unitpp::assert_eq("GetEndACP second 2-para",
				(LONG)(s_cchPara2 + s_cchParaBreak + s_cchPara3), cch2);

			MakeSelLong();
			LONG cch3 = -1;
			LockGetEndACP lgea3(m_qtxs, &cch3);
			unitpp::assert_eq("GetEndACP second 2-para",
				(LONG)(s_cchPara1 + s_cchParaBreak + s_cchPara5), cch3);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetActiveView() and VwTextStore::GetWnd()..
		--------------------------------------------------------------------------------------*/
		void testGetActiveView_GetWnd()
		{
			if (!m_fTestable)
				return;
			HRESULT hr = S_OK;
			TsViewCookie tvc;
			CheckHr(hr = m_qtxs->GetActiveView(&tvc));
			unitpp::assert_eq("GetActiveView hr", S_OK, hr);

			HWND hwnd;
			CheckHr(hr = m_qtxs->GetWnd(tvc, &hwnd));
			unitpp::assert_eq("GetWnd hr", S_OK, hr);
			unitpp::assert_eq("GetWnd hwnd", ::WindowFromDC(m_hdc), hwnd);
		}

	// If GetACPFromPoint is ever implemented, the following should be a good start for
	// testing it.
	//	/*--------------------------------------------------------------------------------------
	//		Test VwTextStore::GetACPFromPoint().
	//	--------------------------------------------------------------------------------------*/
		//void testGetACPFromPoint()
		//{
			//if (!m_fTestable)
			//	return;
		//	HRESULT hr = S_OK;
		//	MakeStringList1();
		//	VwTextSelectionPtr qsel;
		//	MakeSelection(0, 5, 6, false, true, &qsel);
		//	HoldGraphics hg(m_qrootb);
		//	RECT rcSel;
		//	RECT rcSecondary;
		//	ComBool fSplit;
		//	ComBool fEndBeforeAnchor;
		//	qsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rcSel,
		//		&rcSecondary, &fSplit, &fEndBeforeAnchor);
		//	ClientRectToScreen(rcSel);
		//	POINT insideTopLeft = {rcSel.left + 1, rcSel.top + 1};
		//	// Now we have the boundary of this character in screen coordinates.
		//	int ich;
		//	TsViewCookie tvc;
		//	m_qtxs->GetActiveView(&tvc);
		//
		//	// With a point near the top left of the character, all variations find that
		//	// character.
		//	hr = m_qtxs->GetACPFromPoint(tvc, &insideTopLeft, 0, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", 5, ich);
		//
		//	hr = m_qtxs->GetACPFromPoint(tvc, &insideTopLeft, GXFPF_ROUND_NEAREST, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", 5, ich);
		//
		//	hr = m_qtxs->GetACPFromPoint(tvc, &insideTopLeft, GXFPF_NEAREST, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", 5, ich);
		//
		//	POINT insideBottomRight = {rcSel.right - 1, rcSel.bottom - 1};
		//
		//	// With a point near the bottom right of the character, default and nearest find that
		//	// character, but Round-nearest is based on the nearest char boundary, so it finds
		//	// the next.
		//	hr = m_qtxs->GetACPFromPoint(tvc, &insideBottomRight, 0, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", 5, ich);
		//
		//	hr = m_qtxs->GetACPFromPoint(tvc, &insideBottomRight, GXFPF_ROUND_NEAREST, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", 6, ich);
		//
		//	hr = m_qtxs->GetACPFromPoint(tvc, &insideBottomRight, GXFPF_NEAREST, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", 5, ich);
		//
		//	// With a point outside the bounding box, default and round-nearest fail, but nearest
		//	// still finds it.
		//	POINT aboveChar = {rcSel.left + 1, rcSel.top - 1};
		//	hr = m_qtxs->GetACPFromPoint(tvc, &aboveChar, 0, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", TF_E_INVALIDPOINT, hr);
		//
		//	hr = m_qtxs->GetACPFromPoint(tvc, &aboveChar, GXFPF_ROUND_NEAREST, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", TF_E_INVALIDPOINT, hr);
		//
		//	hr = m_qtxs->GetACPFromPoint(tvc, &aboveChar, GXFPF_NEAREST, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", 5, ich);
		//
		//	// Now try beyond last character
		//	int s_cchPara1 = wcslen(s_rgpsz1[0]);
		//	MakeSelection(0, s_cchPara1 - 1, s_cchPara1, false, false, &qsel);
		//	RECT rcSel2;
		//	qsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rcSel2,
		//		&rcSecondary, &fSplit, &fEndBeforeAnchor);
		//	ClientRectToScreen(rcSel2);
		//	POINT beyondRight = {rcSel2.right + 1, rcSel2.top + 1};
		//
		//	hr = m_qtxs->GetACPFromPoint(tvc, &beyondRight, 0, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", TF_E_INVALIDPOINT, hr);
		//
		//	hr = m_qtxs->GetACPFromPoint(tvc, &beyondRight, GXFPF_ROUND_NEAREST, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", TF_E_INVALIDPOINT, hr);
		//
		//	hr = m_qtxs->GetACPFromPoint(tvc, &beyondRight, GXFPF_NEAREST, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", s_cchPara1, ich);
		//
		//	// Now try before first character
		//	int s_cchPara1 = wcslen(s_rgpsz1[0]);
		//	MakeSelection(0, 0, 1, false, false, &qsel);
		//	RECT rcSel3;
		//	qsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rcSel3,
		//		&rcSecondary, &fSplit, &fEndBeforeAnchor);
		//	ClientRectToScreen(rcSel3);
		//	POINT beforeLeft = {rcSel3.left - 1, rcSel3.top + 1};
		//
		//	hr = m_qtxs->GetACPFromPoint(tvc, &beforeLeft, 0, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", TF_E_INVALIDPOINT, hr);
		//
		//	hr = m_qtxs->GetACPFromPoint(tvc, &beforeLeft, GXFPF_ROUND_NEAREST, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", TF_E_INVALIDPOINT, hr);
		//
		//	hr = m_qtxs->GetACPFromPoint(tvc, &beforeLeft, GXFPF_NEAREST, & ich);
		//	unitpp::assert_eq("GetACPFromPoint TL default", 0, ich);
		//}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetTextExt() for single paragraph selections.
		--------------------------------------------------------------------------------------*/
		void testGetTextExt()
		{
			if (!m_fTestable)
				return;
			HRESULT hr = S_OK;
			MakeStringList1();
			TsViewCookie tvc;
			CheckHr(hr = m_qtxs->GetActiveView(&tvc));

			VwTextSelectionPtr qsel;
			MakeSelection(0, 5, 6, false, true, &qsel);
			HoldGraphics hg(m_qrootb);
			RECT rcSel;
			RECT rcSecondary;
			ComBool fSplit;
			ComBool fEndBeforeAnchor;
			CheckHr(qsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rcSel,
				&rcSecondary, &fSplit, &fEndBeforeAnchor));
			ClientRectToScreen(rcSel);

			RECT rc;
			BOOL fClipped;
			LockGetTextExt lgte1(m_qtxs, tvc, 5, 6, &rc, &fClipped);
			VerifyEqualRects(rcSel, rc, "GetTextExt 5, 6");

			int s_cchPara1 = wcslen(s_rgpsz1[0]);
			LockGetTextExt lgte2(m_qtxs, tvc, 0, s_cchPara1, &rc, &fClipped);
			MakeSelection(0, 0, s_cchPara1, false, false, &qsel);
			CheckHr(qsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rcSel,
				&rcSecondary, &fSplit, &fEndBeforeAnchor));
			ClientRectToScreen(rcSel);
			VerifyEqualRects(rcSel, rc, "GetTextExt all");

			LockGetTextExt lgte3(m_qtxs, tvc, 2, 2, &rc, &fClipped);
			// TSF spec says that GetTextExt should fail when the selection is an IP.
			//unitpp::assert_eq("GetTextExt on IP fails", E_INVALIDARG, lgte3.m_hrLock);
			// However the Chinese IME expects it to succeed.
			unitpp::assert_eq("GetTextExt on IP fails", S_OK, lgte3.m_hrLock);
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetTextExt() for multi-paragraph selection.
		--------------------------------------------------------------------------------------*/
		void testGetTextExt2()
		{
			if (!m_fTestable)
				return;
			HRESULT hr = S_OK;
			MakeStringList2();
			TsViewCookie tvc;
			CheckHr(hr = m_qtxs->GetActiveView(&tvc));

			HoldGraphics hg(m_qrootb);
			RECT rcSel;
			RECT rcSecondary;
			ComBool fSplit;
			ComBool fEndBeforeAnchor;
			RECT rc;
			BOOL fClipped;

			MakeSel2Para();	// 1, 4; 2, 10
			CheckHr(m_qrootb->Selection()->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot,
				&rcSel, &rcSecondary, &fSplit, &fEndBeforeAnchor));
			ClientRectToScreen(rcSel);
			LockGetTextExt lgte1(m_qtxs, tvc, 4, s_cchPara2 + s_cchParaBreak + 10,
				&rc, &fClipped);
			VerifyEqualRects(rcSel, rc, "GetTextExt(MakeSel2Para)");
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetScreenExt() for single paragraph selection.
		--------------------------------------------------------------------------------------*/
		void testGetScreenExt()
		{
			if (!m_fTestable)
				return;
			HRESULT hr = S_OK;
			MakeStringList1();
			TsViewCookie tvc;
			CheckHr(hr = m_qtxs->GetActiveView(&tvc));

			VwTextSelectionPtr qsel;
			int s_cchPara1 = wcslen(s_rgpsz1[0]);
			// First select all the first paragraph, then get its bounds.
			MakeSelection(0, 0, s_cchPara1, false, true, &qsel);
			HoldGraphics hg(m_qrootb);
			RECT rcSel;
			RECT rcSecondary;
			ComBool fSplit;
			ComBool fEndBeforeAnchor;
			CheckHr(qsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rcSel,
				&rcSecondary, &fSplit, &fEndBeforeAnchor));
			ClientRectToScreen(rcSel);
			MakeSelection(0, 2, 2, false, true, &qsel);	// Change selection to IP.

			RECT rc;
			LockGetScreenExt lgse1(m_qtxs, tvc, &rc);
			VerifyEqualRects(rcSel, rc, "GetScreenExt first para");
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::GetScreenExt() for multi-paragraph selection.
		--------------------------------------------------------------------------------------*/
		void testGetScreenExt2()
		{
			if (!m_fTestable)
				return;
			HRESULT hr = S_OK;
			MakeStringList2();
			TsViewCookie tvc;
			CheckHr(hr = m_qtxs->GetActiveView(&tvc));
			HoldGraphics hg(m_qrootb);
			RECT rcSel;
			RECT rcSecondary;
			ComBool fSplit;
			ComBool fEndBeforeAnchor;
			RECT rc;

			// Make a selection that covers the entire text.
			Make2ParaSel(0, 0, 4, wcslen(s_rgpsz2[4]));
			CheckHr(m_qrootb->Selection()->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot,
				&rcSel, &rcSecondary, &fSplit, &fEndBeforeAnchor));
			ClientRectToScreen(rcSel);
			LockGetScreenExt lgse1(m_qtxs, tvc, &rc);
			VerifyEqualRects(rcSel, rc, "GetScreenExt(MakeSel2Para)");
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::AcpToLog()
		--------------------------------------------------------------------------------------*/
		void testAcpToLog()
		{
			if (!m_fTestable)
				return;

			VwTextStoreTestSub* ptxs = dynamic_cast<VwTextStoreTestSub*>(m_qtxs.Ptr());
			MakeStringList(s_rgpsz3);

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

			unitpp::assert_eq("Wrong index 0", 0, ptxs->CallAcpToLog(0));
			unitpp::assert_eq("Wrong index 1", 3, ptxs->CallAcpToLog(1));
			unitpp::assert_eq("Wrong index 2", 4, ptxs->CallAcpToLog(2));
			unitpp::assert_eq("Wrong index 3", 7, ptxs->CallAcpToLog(3));
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::AcpToLog() when we try to convert the index that points beyond
			the available text
		--------------------------------------------------------------------------------------*/
		void testAcpToLog_OutOfBounds()
		{
			if (!m_fTestable)
				return;

			VwTextStoreTestSub* ptxs = dynamic_cast<VwTextStoreTestSub*>(m_qtxs.Ptr());
			MakeStringList(s_rgpsz3);

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

			unitpp::assert_true("Got wrong index for out-of-bounds condition",
				(unsigned)ptxs->CallAcpToLog(99) > wcslen(s_rgpsz3[0]));
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::AcpToLog() when we don't have a selection
		--------------------------------------------------------------------------------------*/
		void testAcpToLog_NoSelection()
		{
			if (!m_fTestable)
				return;

			VwTextStoreTestSub* ptxs = dynamic_cast<VwTextStoreTestSub*>(m_qtxs.Ptr());
			MakeStringList(s_rgpsz3);
			unitpp::assert_eq("AcpToLog should convert 0 even with no selection", 0, ptxs->CallAcpToLog(0));

			assert_exception("Got wrong exception on non-zero AcpToLog with no selection", E_FAIL, ptxs->CallAcpToLog(1));
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::LogToAcp()
		--------------------------------------------------------------------------------------*/
		void testLogToAcp()
		{
			if (!m_fTestable)
				return;

			VwTextStoreTestSub* ptxs = dynamic_cast<VwTextStoreTestSub*>(m_qtxs.Ptr());
			MakeStringList(s_rgpsz3);

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

			unitpp::assert_eq("Wrong index 0", 0, ptxs->CallLogToAcp(0));
			unitpp::assert_eq("Wrong index 1", 0, ptxs->CallLogToAcp(1));
			unitpp::assert_eq("Wrong index 2", 0, ptxs->CallLogToAcp(2));
			unitpp::assert_eq("Wrong index 3", 1, ptxs->CallLogToAcp(3));
			unitpp::assert_eq("Wrong index 4", 2, ptxs->CallLogToAcp(4));
			unitpp::assert_eq("Wrong index 5", 2, ptxs->CallLogToAcp(5));
			unitpp::assert_eq("Wrong index 6", 2, ptxs->CallLogToAcp(6));
			unitpp::assert_eq("Wrong index 7", 3, ptxs->CallLogToAcp(7));
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::LogToAcp() when we try to convert the index that points beyond
			the available text
		--------------------------------------------------------------------------------------*/
		void testLogToAcp_OutOfBounds()
		{
			if (!m_fTestable)
				return;

			VwTextStoreTestSub* ptxs = dynamic_cast<VwTextStoreTestSub*>(m_qtxs.Ptr());
			MakeStringList(s_rgpsz3);

			// Need some selection to start with as it determines which paragraph.
			IVwSelectionPtr qselTemp;
			CheckHr(m_qrootb->MakeSimpleSel(true, true, false, true, &qselTemp));

			unitpp::assert_true("Got wrong index for out-of-bounds condition",
				(unsigned)ptxs->CallLogToAcp(99) > wcslen(s_rgpszExpected3));
		}

		/*--------------------------------------------------------------------------------------
			Test VwTextStore::LogToAcp() when we don't have a selection
		--------------------------------------------------------------------------------------*/
		void testLogToAcp_NoSelection()
		{
			if (!m_fTestable)
				return;

			VwTextStoreTestSub* ptxs = dynamic_cast<VwTextStoreTestSub*>(m_qtxs.Ptr());
			MakeStringList(s_rgpsz3);

			unitpp::assert_eq("Unexpected result when there is no selection", 0,
				ptxs->CallLogToAcp(0));
		}

		//**************************************************************************************
		// Various utility methods used by the tests.
		//**************************************************************************************

		/*--------------------------------------------------------------------------------------
			Utility function for testing VwTextStore::GetText() with a limited buffer size.
			This assumes that MakeSel2Para() or MakeSel2ParaRev() has been called.

			@param cchBuf Supposed length of the buffer (actual must be at least one greater).
			@param prgch Pointer to the output text buffer.
			@param pszMsg First part of the unitpp assert message
		--------------------------------------------------------------------------------------*/
		void verifyGetText2Para(int cchBuf, OLECHAR * prgch, const char * pszMsg)
		{
			StrUni stu;
			stu.Format(L"%s%n%s", s_rgpsz2[1] + 7, s_rgpsz2[2]);
			int cch = min(stu.Length(), cchBuf);
			//prgch[cch] = chBound;
			TS_RUNINFO tri;
			LockGetText lgt(m_qtxs, 7, -1, prgch, cchBuf + 1, &tri, 1);
			StrAnsi staMsg;
			staMsg.Format("GetText(%s) [bufsize=%d] ichNext", pszMsg, cchBuf);
			unitpp::assert_eq(staMsg.Chars(), (LONG)cch + 7, lgt.m_ichNext);
			staMsg.Format("GetText(%s) [bufsize=%d] text", pszMsg, cchBuf);
			unitpp::assert_true(staMsg.Chars(), wcsncmp(prgch, stu.Chars(), cch) == 0);
			staMsg.Format("GetText(%s) [bufsize=%d] cchOut", pszMsg, cchBuf);
			unitpp::assert_eq(staMsg.Chars(), (ULONG)cch, lgt.m_cchOut);
			// Disabled because there's no reason why GetText shouldn't zero out the available
			// buffer
			//staMsg.Format("GetText(%s) [bufsize=%d] output boundcheck", pszMsg, cchBuf);
			//unitpp::assert_eq(staMsg.Chars(), chBound, prgch[cch]);
			staMsg.Format("GetText(%s) [bufsize=%d] ctriOut", pszMsg, cchBuf);
			unitpp::assert_eq(staMsg.Chars(), (ULONG)1, lgt.m_ctriOut);
			staMsg.Format("GetText(%s) [bufsize=%d] run count", pszMsg, cchBuf);
			unitpp::assert_eq(staMsg.Chars(), (ULONG)cch, tri.uCount);
			staMsg.Format("GetText(%s) [bufsize=%d] run type", pszMsg, cchBuf);
			unitpp::assert_eq(staMsg.Chars(), TS_RT_PLAIN, tri.type);
		}

		/*--------------------------------------------------------------------------------------
			Verify that the two RECT objects contain the same coordinates.
		--------------------------------------------------------------------------------------*/
		void VerifyEqualRects(RECT & rcCorrect, RECT & rcTest, const char * pszMsg)
		{
			StrAnsi staMsg;
			staMsg.Format("%s left", pszMsg);
			unitpp::assert_eq(staMsg.Chars(), rcCorrect.left, rcTest.left);
			staMsg.Format("%s top", pszMsg);
			unitpp::assert_eq(staMsg.Chars(), rcCorrect.top, rcTest.top);
			staMsg.Format("%s right", pszMsg);
			unitpp::assert_eq(staMsg.Chars(), rcCorrect.right, rcTest.right);
			staMsg.Format("%s bottom", pszMsg);
			unitpp::assert_eq(staMsg.Chars(), rcCorrect.bottom, rcTest.bottom);
		}

		/*--------------------------------------------------------------------------------------
			Convert the RECT values from client coordinates to screen coordinates.
		--------------------------------------------------------------------------------------*/
		void ClientRectToScreen(RECT & rc)
		{
			POINT topLeft = {rc.left, rc.top};
			POINT bottomRight = {rc.right, rc.bottom};
			::ClientToScreen(m_hwnd, &topLeft);
			::ClientToScreen(m_hwnd, &bottomRight);
			rc.left = topLeft.x;
			rc.top = topLeft.y;
			rc.right = bottomRight.x;
			rc.bottom = bottomRight.y;
		}

		/*--------------------------------------------------------------------------------------
			Verify that the text string is the same as the indicated paragraph contents.
		--------------------------------------------------------------------------------------*/
		void VerifyParaContents(int ihvoPara, const OLECHAR * pszText, const char * pszMsg)
		{
			StrUni stu = pszText;
			ITsStringPtr qtss;
			CheckHr(m_qtsf->MakeString(stu.Bstr(), g_wsEng, &qtss));
			VerifyParaContents(ihvoPara, qtss, pszMsg);
		}

		/*--------------------------------------------------------------------------------------
			Verify that the TsString is the same as the indicated paragraph contents.
		--------------------------------------------------------------------------------------*/
		void VerifyParaContents(int ihvoPara, ITsString * ptss, const char * pszMsg,
			BSTR bstrEnd = NULL)
		{
			HRESULT hr = S_OK;
			VwTextSelectionPtr qsel;
			MakeSelection(ihvoPara, 0, 0, false, false, &qsel);
			ComBool fEndPoint;
			ITsStringPtr qtss;
			int ich;
			ComBool fAssocPrev;
			HVO hvoObj;
			PropTag tag;
			int ws;
			CheckHr(qsel->TextSelInfo(false, // Get anchor info
				&qtss, &ich, &fAssocPrev, &hvoObj, &tag, &ws));
			if (bstrEnd)
			{
				// Append these chars to the string for the test.
				int cch;
				ITsStrBldrPtr qtsb;
				CheckHr(hr = qtss->get_Length(&cch));
				CheckHr(hr = qtss->GetBldr(&qtsb));
				CheckHr(hr = qtsb->Replace(cch, cch, bstrEnd, NULL));
				CheckHr(qtsb->GetString(&qtss));
			}
			ComBool fEqual;
			StrAnsi sta;
			sta.Format("%s: strings equal", pszMsg);
			CheckHr(hr = ptss->Equals(qtss, &fEqual));
			unitpp::assert_eq(sta.Chars(), S_OK, hr);
			sta.Format("%s: para contents", pszMsg);
			unitpp::assert_true(sta.Chars(), fEqual);
		}

		/*--------------------------------------------------------------------------------------
			Verify that the given parameters match the current rootbox selection for a single
			paragraph selection.
		--------------------------------------------------------------------------------------*/
		void VerifySelection(int ihvoPara, int ichAnchor, int ichEnd, const char * pszMsg)
		{
			VerifySelection(ihvoPara, ichAnchor, ihvoPara,	ichEnd, pszMsg);
		}

		/*--------------------------------------------------------------------------------------
			Verify that the given parameters match the current rootbox selection.  This handles
			either single or multiple paragraph selections.
		--------------------------------------------------------------------------------------*/
		void VerifySelection(int ihvoPara, int ichAnchor, int ihvoEnd, int ichEnd,
			const char * pszMsg)
		{
			ComBool fEndPoint;
			ITsStringPtr qtss;
			int ich;
			ComBool fAssocPrev;
			HVO hvoObj;
			PropTag tag;
			int ws;
			StrAnsi sta;
			CheckHr(m_qrootb->Selection()->TextSelInfo(false, // Get anchor info
				&qtss, &ich, &fAssocPrev, &hvoObj, &tag, &ws));
			sta.Format("%s: Anchor", pszMsg);
			unitpp::assert_eq(sta.Chars(), ichAnchor, ich);
			sta.Format("%s: Anchor para", pszMsg);
			unitpp::assert_eq(sta.Chars(), m_vhvo[ihvoPara], hvoObj);
			CheckHr(m_qrootb->Selection()->TextSelInfo(true, // Get end info
				&qtss, &ich, &fAssocPrev, &hvoObj, &tag, &ws));
			sta.Format("%s: End", pszMsg);
			unitpp::assert_eq(sta.Chars(), ichEnd, ich);
			sta.Format("%s: End para", pszMsg);
			unitpp::assert_eq(sta.Chars(), m_vhvo[ihvoEnd], hvoObj);
		}

		/*--------------------------------------------------------------------------------------
			Make a selection from the 5th char of the second para to the 11th char of the third
			para.  (This range was chosen arbitrarily.)
		--------------------------------------------------------------------------------------*/
		void MakeSel2Para()
		{
			Make2ParaSel(1, 4, 2, 10);
		}

		/*--------------------------------------------------------------------------------------
			Make a selection from the 11th char of the third para to the 5th char of the second
			para.  (This range swaps the anchor and end point from the previous method.)
		--------------------------------------------------------------------------------------*/
		void MakeSel2ParaRev()
		{
			Make2ParaSel(2, 10, 1, 4);
		}

		/*--------------------------------------------------------------------------------------
			Make a selection from the 6th char of the second para to the 1st char of the fourth
			para.  This range ends on an empty paragraph.
		--------------------------------------------------------------------------------------*/
		void MakeSelToEmpty()
		{
			Make2ParaSel(1, 5, 3, 0);
		}

		/*--------------------------------------------------------------------------------------
			Make a selection from the 1st char of the fourth para to the 7th char of the fifth
			para.  This range starts on an empty paragraph.
		--------------------------------------------------------------------------------------*/
		void MakeSelFromEmpty()
		{
			Make2ParaSel(3, 0, 4, 6);
		}

		/*--------------------------------------------------------------------------------------
			Make a selection from the 2nd char of the first para to the 8th char of the fifth
			para.  This range spans several paragraphs.
		--------------------------------------------------------------------------------------*/
		void MakeSelLong()
		{
			Make2ParaSel(0, 1, 4, 7);
		}

		/*--------------------------------------------------------------------------------------
			Make a selection with the anchor and end in (possibly) different paragraphs.
		--------------------------------------------------------------------------------------*/
		void Make2ParaSel(int ihvoAnchor, int ichAnchor, int ihvoEnd, int ichEnd)
		{
			MakeSelection(ihvoAnchor, ichAnchor, ichEnd, true, true, NULL, ihvoEnd);
		}

		/*--------------------------------------------------------------------------------------
			Make a selection with the anchor and end in the same paragraph.
		--------------------------------------------------------------------------------------*/
		void MakeSelection(int ihvoPara, int ichAnchor, int ichEnd, bool fAssocPrev = true)
		{
			MakeSelection(ihvoPara, ichAnchor, ichEnd, fAssocPrev, true, NULL);
		}

		/*--------------------------------------------------------------------------------------
			Make the specified selection. Return it if ppsel is non-NULL.
		--------------------------------------------------------------------------------------*/
		void MakeSelection(int ihvoAnchor, int ichAnchor, int ichEnd, bool fAssocPrev,
			bool fInstall, VwTextSelection ** ppsel, int ihvoEnd = -1)
		{
			if (ihvoEnd == ihvoAnchor)
				ihvoEnd = -1;
			VwSelLevInfo vsli;
			vsli.tag = kflidStText_Paragraphs;
			vsli.cpropPrevious = 0; // first occurrence of that property
			vsli.ihvo = ihvoAnchor;
			IVwSelectionPtr qsel;
			CheckHr(m_qrootb->MakeTextSelection(0, // first top-level target
				1, // VwSelLevInfo object
				&vsli,
				kflidStTxtPara_Contents,
				0, // first occurrence of contents.
				ichAnchor, ichEnd,
				0, // ws doesn't matter, not multilingual.
				fAssocPrev,
				ihvoEnd, // end in same paragraph.
				NULL, // not overriding default props to type.
				fInstall, // go ahead and install it if true.
				ppsel ? &qsel : NULL));
			if (ppsel)
			{
				// convert to VwTextSelection
				*ppsel = dynamic_cast<VwTextSelection *>(qsel.Detach());
			}
		}

		/*--------------------------------------------------------------------------------------
			Make a list of paragraphs in the cache from the first array of strings.
		--------------------------------------------------------------------------------------*/
		void MakeStringList1()
		{
			MakeStringList(s_rgpsz1);
		}

		/*--------------------------------------------------------------------------------------
			Make a list of paragraphs in the cache from the second array of strings.
		--------------------------------------------------------------------------------------*/
		void MakeStringList2()
		{
			MakeStringList(s_rgpsz2);
		}

		/*--------------------------------------------------------------------------------------
			Make the list of strings passed be the contents of the document (each is a
			paragraph, which HVO's incrementing from khvoParaBase, in the kflidStText_Paragraphs
			property of m_hvoRoot).
		--------------------------------------------------------------------------------------*/
		void MakeStringList(const OLECHAR ** prgsz)
		{
			int isz;
			m_vhvo.Clear();
			for (isz = 0; prgsz[isz]; ++isz)
			{
				StrUni stu = prgsz[isz];
				ITsStringPtr qtss;
				CheckHr(m_qtsf->MakeString(stu.Bstr(), g_wsEng, &qtss));
				CheckHr(m_qcda->CacheStringProp(khvoParaBase + isz, kflidStTxtPara_Contents, qtss));
				m_vhvo.Push(khvoParaBase + isz);
			}
			CheckHr(m_qcda->CacheVecProp(m_hvoRoot, kflidStText_Paragraphs, m_vhvo.Begin(),
				m_vhvo.Size()));

			CheckHr(m_qrootb->Layout(m_qvg32, 300));
		}

		/*--------------------------------------------------------------------------------------
			Called before any of the test methods in the suite class.
		--------------------------------------------------------------------------------------*/
		virtual void SuiteSetup()
		{
			// Determine whether we can test. Testing is possible only on a computer that has
			// text services installed.
			ITfThreadMgrPtr qttmThreadMgr;
			HRESULT hr = S_OK;
			try{
				CheckHr(hr = ::CoCreateInstance(CLSID_TF_ThreadMgr, NULL, CLSCTX_INPROC_SERVER,
					IID_ITfThreadMgr, (void **)&qttmThreadMgr));
			}
			catch(Throwable& thr){
				hr = thr.Result();
			}
			m_fTestable = hr != E_FAIL;
			if (!m_fTestable)
			{
				printf("Text Services tests not really run...no text services installed\n");
				return;
			}
			CreateTestWritingSystemFactory();
			m_qcda.Attach(NewObj VwCacheDa());
			CheckHr(m_qcda->QueryInterface(IID_ISilDataAccess, (void **)&m_qsda));
			CheckHr(m_qsda->putref_WritingSystemFactory(g_qwsf));

			m_qtsf.CreateInstance(CLSID_TsStrFactory);
			m_qvg32.CreateInstance(CLSID_VwGraphicsWin32);
			// Create a dummy background window (never visible) to host the view.
			// This is mainly important for the functions that use screen coordinates,
			// to make sure that the conversions from screen to and from client coords
			// are being performed correctly.
			m_hwnd = ::CreateWindowW(L"STATIC", L"DUMMY",
				WS_OVERLAPPED , // nb NOT WS_VISIBLE! don't interfere with running programs
				20,
				40,
				300,
				200,
				NULL, // parent
				NULL, // default menu or none
				NULL, // hInstance ignored since NT
				NULL); // no extra data to WM_CREATE

			m_hdc = ::GetDC(m_hwnd);
			CheckHr(m_qvg32->Initialize(m_hdc));
			m_qvc.Attach(NewObj DummyParaVc());
			m_hvoRoot = 101;
			m_qdrs.Attach(NewObj DummyRootSite());
			Rect rcSrc(0, 0, 96, 96);
			m_qdrs->SetRects(rcSrc, rcSrc);
			m_qdrs->SetGraphics(m_qvg32);
			s_stuParaBreak.Format(L"%n");
			s_cchParaBreak = s_stuParaBreak.Length();
			s_cchPara1 = wcslen(s_rgpsz1[0]);
			s_cchPara2 = wcslen(s_rgpsz1[1]);
			s_cchPara3 = wcslen(s_rgpsz2[2]);
			s_cchPara4 = wcslen(s_rgpsz2[3]);
			s_cchPara5 = wcslen(s_rgpsz2[4]);
		}

		/*--------------------------------------------------------------------------------------
			Called after all the test methods in the suite class.
		--------------------------------------------------------------------------------------*/
		virtual void SuiteTeardown()
		{
			if (!m_fTestable)
				return;
			m_qtsf.Clear();
			m_qsda.Clear();
			m_qcda.Clear();
			if (m_qvg32)
				m_qvg32->ReleaseDC();
			if (m_hdc != 0)
				::ReleaseDC(NULL, m_hdc);
			m_qvc.Clear();
			m_qdrs.Clear();
			m_qvg32.Clear();
			::DestroyWindow(m_hwnd);
			CloseTestWritingSystemFactory();
		}

		/*--------------------------------------------------------------------------------------
			Called before each test method.
		--------------------------------------------------------------------------------------*/
		virtual void Setup()
		{
			if (!m_fTestable)
				return;
			// Make the root box and initialize it.
			VwRootBox::CreateCom(NULL, IID_IVwRootBox, (void **) &m_qrootb);
			CheckHr(m_qrootb->putref_DataAccess(m_qsda));
			CheckHr(m_qrootb->SetRootObject(m_hvoRoot, m_qvc, kfragStText, NULL));
			CheckHr(m_qrootb->SetSite(m_qdrs));
			m_qdrs->SetRootBox(m_qrootb);

			m_qtxs.Attach(NewObj VwTextStoreTestSub(m_qrootb));
			// Creating it calls Init(), which installs a context, which installs an AdviseSink.
			// This messes up our tests, so suspend the real sink for the duration of the test.
			m_dwSuspendedAdviseMask = m_qtxs->SuspendAdvise(&m_qunkSuspendedAdviseSink);
		}

		/*--------------------------------------------------------------------------------------
			Called after each test method.
		--------------------------------------------------------------------------------------*/
		virtual void Teardown()
		{
			if (!m_fTestable)
				return;
			if (m_qunkSuspendedAdviseSink)
			{
				m_qtxs->AdviseSink(IID_ITextStoreACPSink, m_qunkSuspendedAdviseSink,
					m_dwSuspendedAdviseMask);
				m_qunkSuspendedAdviseSink.Clear();
			}
			m_qtxs.Clear();
			m_qrootb->Close();
			m_qrootb.Clear();
		}
#endif /*ENABLE_TSF*/
	};
}
#endif /*WIN32*/

#endif /*TestVwTextStore_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
