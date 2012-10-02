/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwTextStore.cpp
Responsibility:
Last reviewed: Not yet.

Description:
	Contains the class VwTextStore which implements the MS Text Services Framework interface
	ITextStoreACP.

	We simulate a document by pretending that it consists only of the Anchor and End paragraphs
	(or just the one paragraph if Anchor and End are in the same paragraph).  To some extent, we
	also pretend that a nonexistent selection (or a nontext selection) means an empty document
	regardless of how much text actually exists in the rootbox.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "main.h"
#pragma hdrstop
// any other headers (not precompiled)

#include "IcuCommon.h"

#undef THIS_FILE
DEFINE_THIS_FILE

#undef ENABLE_TSF
#define ENABLE_TSF

#undef TRACING_TSF
//#define TRACING_TSF

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

static DummyFactory g_factDummy(
	_T("SIL.Views.VwTextStore"));

// Application level values from the Text Services Framework.
ITfThreadMgrPtr VwTextStore::s_qttmThreadMgr;
TfClientId VwTextStore::s_tfClientID = 0;
ITfCategoryMgrPtr VwTextStore::s_qtfCategoryMgr;
ITfDisplayAttributeMgrPtr VwTextStore::s_qtfDisplayAttributeMgr;

// Global count of instances of VwTextStore objects.
static long g_ctxs = 0;
static StrUni s_stuParaBreak;
static int s_cchParaBreak;

//:>********************************************************************************************
//:>	Methods
//:>********************************************************************************************
#ifdef TRACING_TSF
FILE * fpTracing = NULL;

static void TraceTSF(const char * pszMsg)
{
	::OutputDebugStringA(pszMsg);
	if (fpTracing)
		fputs(pszMsg, fpTracing);
}

static void TraceTSF(const wchar * pszMsg)
{
	::OutputDebugStringW(pszMsg);
	if (fpTracing)
		fputws(pszMsg, fpTracing);
}
#endif

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
VwTextStore::VwTextStore(VwRootBox * prootb)
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	AssertPtr(prootb);
	m_qrootb = prootb;
	m_fNotify = true;
	InterlockedIncrement(&g_ctxs);

#ifdef ENABLE_TSF
	// Create the primary Text Services Framework interface the first time we need it.
	// If we fail, which happens if no text services are installed, don't try again,
	// at least untill all windows have closed (the test on g_ctxs achieves this).
	if (!s_qttmThreadMgr && g_ctxs == 1)
	{
		//Assert(g_ctxs == 1);

		// We don't sourround this COM call with CheckHr because we expect it to fail
		// sometimes. If we surround it with CheckHr it will print an unnecessary stack dump
		// which makes things take longer.
		HRESULT hr;
		IgnoreHr(hr = ::CoCreateInstance(CLSID_TF_ThreadMgr, NULL, CLSCTX_INPROC_SERVER,
			IID_ITfThreadMgr, (void **)&s_qttmThreadMgr));
		if (hr == E_FAIL)
		{
			// According to personal correspondence with MS and our experience, this returns
			// E_FAIL if (roughly) there are no text services installed. For example, Keyman
			// isn't installed, nor any Far-East IMs, nor voice recognition...
			// In this case we do nothing and all methods return E_UNEXPECTED.
			return;
		}
		else if (FAILED(hr))
			CheckHrCore(hr);

		CheckHr(s_qttmThreadMgr->Activate(&s_tfClientID));

		//Create the category manager.
		try
		{
			CheckHr(::CoCreateInstance(CLSID_TF_CategoryMgr, NULL, CLSCTX_INPROC_SERVER,
				IID_ITfCategoryMgr, (void **)&s_qtfCategoryMgr));
		}
		catch(Throwable& thr)
		{
			// Currently we don't check for errors here...if we can't get a category manager
			// we don't attempt to display 'display attributes'.
			WarnHr(thr.Result()); // But just for debugging note it failed.
		}

		//Create the display attribute manager.
		try
		{
			CheckHr(::CoCreateInstance(CLSID_TF_DisplayAttributeMgr, NULL, CLSCTX_INPROC_SERVER,
				IID_ITfDisplayAttributeMgr, (void **)&s_qtfDisplayAttributeMgr));
		}
		catch(Throwable& thr)
		{
			WarnHr(thr.Result()); // But just for debugging note it failed.
		}
	}
	if (s_stuParaBreak.Length() == 0)
	{
		s_stuParaBreak.Format(L"%n");
		s_cchParaBreak = s_stuParaBreak.Length();
	}
#endif /*ENABLE_TSF*/
#ifdef TRACING_TSF
	if (!fpTracing)
		fopen_s(&fpTracing, "C:\\FW\\TraceTSF.debug", "a");
#endif
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
VwTextStore::~VwTextStore()
{
#ifdef TRACING_TSF
	if (fpTracing)
	{
		fclose(fpTracing);
		fpTracing = NULL;
	}
#endif
	// Take care of our global COM pointer.
	long ctxs = InterlockedDecrement(&g_ctxs);
	Assert(g_ctxs >= 0);
	if (s_qttmThreadMgr)
	{
		if (ctxs == 0)
		{
			CheckHr(s_qttmThreadMgr->Deactivate());
			s_qttmThreadMgr.Clear();
			s_tfClientID = 0;
			s_stuParaBreak.Clear();	// Prevent bogus memory leak reports.
			s_qtfCategoryMgr.Clear();
			s_qtfDisplayAttributeMgr.Clear();
		}
	}
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	IUnknown Methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Standard COM function.

	@param riid - reference to the desired interface GUID.
	@param ppv - address that receives the interface pointer.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

#ifdef ENABLE_TSF
	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ITextStoreACP *>(this));
	else if (riid == IID_ITextStoreACP)
		*ppv = static_cast<ITextStoreACP *>(this);
	else if (riid == IID_ITfMouseTrackerACP)
		*ppv = static_cast<ITfMouseTrackerACP *>(this);
	else if (riid == IID_ITfContextOwnerCompositionSink)
		*ppv = static_cast<ITfContextOwnerCompositionSink *>(this);
//	else if (&riid == &CLSID_VwTextStore)
//		*ppv = static_cast<VwTextStore *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(static_cast<ITextStoreACP *>(this),
			IID_ITextStoreACP);
		return S_OK;
	}
	else
#endif /*ENABLE_TSF*/
	{
		StrAnsi staError;
		staError.Format(
			"VwTextStore::QueryInterface could not provide interface %g; compare %g",
			&riid, &IID_IServiceProvider);
		// We might want this when doing further TSF testing, but otherwise
		// it causes unnecessary concerns to those watching warnings.
#ifdef TRACING_TSF
		TraceTSF(staError.Chars());
#endif
		Warn(staError.Chars());
		return E_NOINTERFACE;
	}

	AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	ITextStoreACP methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Install an advise sink (ITextStoreACPSink interface), or modify the current advise sink.
	The advise sink is specified by the punk parameter (riid must be IID_ITextStoreACPSink).
	See MSDN for details.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::AdviseSink(REFIID riid, IUnknown * punk, DWORD dwMask)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(punk);

#ifdef TRACING_TSF
	StrAnsi sta;
	const GUID * pguid = &riid;
	sta.Format("VwTextStore::AdviseSink(%g, ..., %x)%n", pguid, dwMask);
	TraceTSF(sta.Chars());
#endif
	if (!s_qttmThreadMgr)
		ThrowHr(WarnHr(E_UNEXPECTED));
	// We handle only one type of sink.
	if (riid != IID_ITextStoreACPSink)
		return E_INVALIDARG;

	// Get the "real" IUnknown pointer, and check whether this sink has already been set.
	IUnknownPtr qunkID;
	CheckHr(punk->QueryInterface(IID_IUnknown, (void **)&qunkID));
	if (qunkID == m_AdviseSinkInfo.m_qunkID)
	{
		// This is the same sink, so just update the mask.
		m_AdviseSinkInfo.m_dwMask = dwMask;
		return S_OK;
	}
	else if (m_AdviseSinkInfo.m_qunkID)
	{
		// We can't just overwrite an existing sink!
		return CONNECT_E_ADVISELIMIT;
	}
	else
	{
		// Set the mask, the IUnknown pointer, the ITextStoreACPSink pointer, and (if
		// available) the ITextStoreACPServices pointer.
		m_AdviseSinkInfo.m_dwMask = dwMask;
		m_AdviseSinkInfo.m_qunkID = qunkID;
		CheckHr(punk->QueryInterface(IID_ITextStoreACPSink,
			(void **)&m_AdviseSinkInfo.m_qTextStoreACPSink));
		punk->QueryInterface(IID_ITextStoreACPServices, (void **)&m_qServices);
	}

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Release any installed advise sink.
	See MSDN for details.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::UnadviseSink(IUnknown * punk)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(punk);

#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("VwTextStore::UnadviseSink(...)%n");
	TraceTSF(sta.Chars());
#endif
	if (!s_qttmThreadMgr)
		ThrowHr(WarnHr(E_UNEXPECTED));
	// Get the "real" IUnknown pointer, and check whether this is the sink we know about.
	IUnknownPtr qunkID;
	CheckHr(punk->QueryInterface(IID_IUnknown, (void **)&qunkID));
	if (qunkID == m_AdviseSinkInfo.m_qunkID)
	{
		// Remove the advise sink.
		m_AdviseSinkInfo.m_qunkID.Clear();
		m_AdviseSinkInfo.m_qTextStoreACPSink.Clear();
		m_AdviseSinkInfo.m_dwMask = 0;
		m_qServices.Clear();
		return S_OK;
	}
	else
	{
		return CONNECT_E_NOCONNECTION;
	}

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Before starting to test, if a real advise sink has been installed we need to remove it.
	We also return it (and the mask) so we can reinstate it before winding everything down.
	If a sink is not already installed, returns zero and null
----------------------------------------------------------------------------------------------*/
DWORD VwTextStore::SuspendAdvise(IUnknown ** ppunk)
{
	*ppunk = m_AdviseSinkInfo.m_qunkID;
	AddRefObj(*ppunk);
	DWORD result = m_AdviseSinkInfo.m_dwMask;
	if (*ppunk)
		CheckHr(UnadviseSink(*ppunk));
	return result;
}

/*----------------------------------------------------------------------------------------------
	"Lock" the document so that the TSF manager may access it reliably.  This calls the
	ITextStoreACPSink::OnLockGranted method to create the document lock.
	See MSDN for details.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::RequestLock(DWORD dwLockFlags, HRESULT * phrSession)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phrSession);

#ifdef TRACING_TSF
	StrAnsi sta;
	StrAnsi staFlags;
	if (dwLockFlags & TS_LF_SYNC)
		staFlags.Append("SYNC");
	if ((dwLockFlags & TS_LF_READWRITE) == TS_LF_READWRITE)
	{
		if (staFlags.Length())
			staFlags.Append("|");
		staFlags.Append("READWRITE");
	}
	else if ((dwLockFlags & TS_LF_READWRITE) == TS_LF_READ)
	{
		if (staFlags.Length())
			staFlags.Append("|");
		staFlags.Append("READ");
	}
	sta.Format("VwTextStore::RequestLock(%s (%x), ...)%n", staFlags.Chars(), dwLockFlags);
	TraceTSF(sta.Chars());
#endif
	// Don't need to check thread manager...we can't have an advise sink if no TM.
	if (!m_AdviseSinkInfo.m_qTextStoreACPSink)
		ThrowHr(WarnHr(E_UNEXPECTED));

	*phrSession = E_FAIL;
	if (m_fLocked)
	{
		// The document is locked already.
		if (dwLockFlags & TS_LF_SYNC)
		{
			// The caller wants an immediate lock, but this cannot be granted because
			// the document is already locked.
			*phrSession = TS_E_SYNCHRONOUS;
#ifdef TRACING_TSF
			sta.Format("    VwTextStore::RequestLock() - TS_E_SYNCHRONOUS%n");
			TraceTSF(sta.Chars());
#endif
			return S_OK;
		}
		else
		{
			// the request is asynchronous
			// The only type of asynchronous lock request this application
			// supports while the document is locked is to upgrade from a read
			// lock to a read/write lock. This scenario is referred to as a lock
			// upgrade request.
			if (((m_dwLockType & TS_LF_READWRITE) == TS_LF_READ) &&
				((dwLockFlags & TS_LF_READWRITE) == TS_LF_READWRITE))
			{
				m_fPendingLockUpgrade = TRUE;
				*phrSession = TS_S_ASYNC;
#ifdef TRACING_TSF
				sta.Format("    VwTextStore::RequestLock() - TS_S_ASYNCH%n");
				TraceTSF(sta.Chars());
#endif
				return S_OK;
			}
		}
		ThrowHr(WarnHr(E_FAIL));
	}

#ifdef TRACING_TSF
	TraceTSF("    VwTextStore::RequestLock() - locking document\r\n");
#endif
	// lock the document
	_LockDocument(dwLockFlags);

	// Have the sink to do its thing.
	*phrSession = m_AdviseSinkInfo.m_qTextStoreACPSink->OnLockGranted(dwLockFlags);

	// unlock the document
	_UnlockDocument();
#ifdef TRACING_TSF
	TraceTSF("    VwTextStore::RequestLock() - unlocked document\r\n");
#endif

	// Todo JohnT: send layout change notification if m_fLayoutChanged.
	if (m_fLayoutChanged)
		OnLayoutChange();

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	This returns the document status through the TS_STATUS structure pointed to by pdcs.
	See MSDN for details (ITextStoreACP::GetStatus).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::GetStatus(TS_STATUS * pdcs)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pdcs);

#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("VwTextStore::GetStatus(...)%n");
	TraceTSF(sta.Chars());
#endif

	// Can be zero or:
	// TS_SD_READONLY - the document is read only; writes will fail
	// TS_SD_LOADING  - the document is loading, expect additional inserts
	pdcs->dwDynamicFlags = 0;

	// Can be zero or:
	// TS_SS_DISJOINTSEL  - the document supports multiple selections
	// TS_SS_REGIONS	  - the document can contain multiple regions
	// TS_SS_TRANSITORY	  - the document is expected to have a short lifespan
	// TS_SS_NOHIDDENTEXT - the document will never contain hidden text
	pdcs->dwStaticFlags = TS_SS_REGIONS | TS_SS_NOHIDDENTEXT;

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Determine whether the document can accept text at the selection or insertion point.
	See MSDN for details.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::QueryInsert(LONG acpTestStart, LONG acpTestEnd, ULONG cch,
	LONG * pacpResultStart, LONG * pacpResultEnd)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pacpResultStart);
	ChkComArgPtr(pacpResultEnd);

	LONG lTextLength = TextLength();

#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("VwTextStore::QueryInsert(%d, %d, %d, ...); TextLength = %d%n",
		acpTestStart, acpTestEnd, cch, lTextLength);
	TraceTSF(sta.Chars());
#endif
	if (!s_qttmThreadMgr)
		ThrowHr(WarnHr(E_UNEXPECTED));
	//make sure the parameters are within range of the document
	if ((acpTestStart > acpTestEnd) ||
		(AcpToLog(acpTestEnd) > lTextLength))
	{
		ThrowHr(WarnHr(E_INVALIDARG));
	}

	//set the start point to the given start point
	*pacpResultStart = acpTestStart;

	//set the end point to the given end point
	*pacpResultEnd = acpTestEnd;

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Get the character position of a text selection in a document.
	See MSDN for details.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::GetSelection(ULONG ulIndex, ULONG ulCount,
	TS_SELECTION_ACP * pSelection, ULONG * pcFetched)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(pSelection, ulCount);
	ChkComOutPtr(pcFetched);

#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("VwTextStore::GetSelection(%d, %d, ...)%n",
		ulIndex, ulCount);
	TraceTSF(sta.Chars());
#endif

	// Caller must have a lock (and can't get one unless we have thread manager).
	if (!m_fLocked)
		return TS_E_NOLOCK;

	//check the requested index
	if (TF_DEFAULT_SELECTION == ulIndex)
		ulIndex = 0;
	else if (ulIndex >= 1)
	{
		//The index is too high. This app only supports one selection.
		ThrowHr(WarnHr(E_INVALIDARG));
	}

	VwParagraphBox * pvpboxFirst;
	VwParagraphBox * pvpboxLast;
	bool fEndBeforeAnchor;
	VwTextSelection * psel = GetStartAndEndBoxes(&pvpboxFirst, &pvpboxLast, &fEndBeforeAnchor);
	pSelection[0].style.fInterimChar = m_fInterimChar;
	if (!psel)
	{
		// No selection yet exists.  Pretend an empty document.
		pSelection[0].acpStart = 0;
		pSelection[0].acpEnd = 0;
		pSelection[0].style.ase = TS_AE_NONE;
#ifdef TRACING_TSF
		sta.Format("    VwTextStore::GetSelection() => %d, %d, %s%n",
			pSelection[0].acpStart, pSelection[0].acpEnd,
			pSelection[0].style.fInterimChar ? "true" : "false");
		TraceTSF(sta.Chars());
#endif
		return S_OK;
	}

	int ichAnchor = psel->AnchorOffset();
	int ichEnd = psel->EndOffset();

	if (pvpboxFirst == pvpboxLast)
	{
		// Single paragraph selection.
		pSelection[0].acpStart = LogToAcp(min(ichAnchor, ichEnd));
		pSelection[0].acpEnd = LogToAcp(max(ichAnchor, ichEnd));
	}
	else
	{
		// multi-paragraph selection.
		int cchFirst = pvpboxFirst->Source()->Cch() + s_cchParaBreak;
		if (fEndBeforeAnchor)
		{
			Assert(ichEnd <= cchFirst - s_cchParaBreak);
			pSelection[0].acpStart = LogToAcp(ichEnd);
			pSelection[0].acpEnd = LogToAcp(cchFirst + ichAnchor);
		}
		else
		{
			Assert(ichAnchor <= cchFirst - s_cchParaBreak);
			pSelection[0].acpStart = LogToAcp(ichAnchor);
			pSelection[0].acpEnd = LogToAcp(cchFirst + ichEnd);
		}
	}
	if (m_fInterimChar)
	{
		//fInterimChar will be set when an intermediate character has been
		//set. One example of when this will happen is when an IME is being
		//used to enter characters and a character has been set, but the IME
		//is still active.
		pSelection[0].style.ase = TS_AE_NONE;
	}
	else
	{
		// The 'active end' (non-anchor, the end that moves for shift-arrow keys)
		// is the start if it is less than the anchor.
		// Review JohnT: does it matter what we answer for an IP?
		pSelection[0].style.ase = fEndBeforeAnchor ? TS_AE_START : TS_AE_END;
	}
	*pcFetched = 1;
#ifdef TRACING_TSF
	sta.Format("    VwTextStore::GetSelection() => %d, %d, %s%n",
		pSelection[0].acpStart, pSelection[0].acpEnd,
		pSelection[0].style.fInterimChar ? "true" : "false");
	TraceTSF(sta.Chars());
#endif

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Select text within the document.
	See MSDN for details.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::SetSelection(ULONG ulCount, const TS_SELECTION_ACP * pSelection)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(pSelection, ulCount);

#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("VwTextStore::SetSelection(%d, [%d, %d, ...])%n",
		ulCount, pSelection[0].acpStart, pSelection[0].acpEnd);
	TraceTSF(sta.Chars());
#endif
	// We support only a single selection.
	if (ulCount > 1)
		ThrowHr(WarnHr(E_INVALIDARG));

	// Check for a proper lock.
	if (!m_fLocked || (m_dwLockType & TS_LF_READWRITE) != TS_LF_READWRITE)
		ThrowHr(WarnHr(TS_E_NOLOCK));

	// Make sure we have a root box.
	if (!m_qrootb)
		ThrowHr(WarnHr(E_UNEXPECTED));

	// if the requested selection is the same as the current selection, do not create a whole new
	// selection, this can cause the loss of text props that might have been set, such as writing system
	TS_SELECTION_ACP curTsa;
	ULONG fetched;
	CheckHr(GetSelection(TF_DEFAULT_SELECTION, 1, &curTsa, &fetched));
	if (fetched > 0 && curTsa.acpStart == pSelection[0].acpStart && curTsa.acpEnd == pSelection[0].acpEnd
		&& (curTsa.style.ase == pSelection[0].style.ase || curTsa.acpStart == curTsa.acpEnd)
		&& curTsa.style.fInterimChar == pSelection[0].style.fInterimChar)
	{
		return S_OK;
	}

	VwTextSelectionPtr qtsel;
	CreateNewSelection(AcpToLog(pSelection[0].acpStart),
		pSelection[0].acpEnd == -1 ? -1 : AcpToLog(pSelection[0].acpEnd),
		pSelection[0].style.ase == TS_AE_START, &qtsel);

	if ((!qtsel) && pSelection[0].acpEnd == 0)
	{
		// Don't tell the service it failed, just don't do it.
		// If we keep telling the service it failed, it eventually goes into a
		// fallback mode that is much less pleasant.
		// Generally this only happens if there's no current selection, which
		// means we're closing down the view and it doesn't much matter what the
		// text service does here next.
		return S_OK;
	}
	m_fInterimChar = pSelection[0].style.fInterimChar;

	m_fNotify = false;
	m_qrootb->SetSelection(qtsel);
	m_fNotify = true;

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Return information about the text at a specified character position.
	See MSDN for details (ITextStoreACP::GetText).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::GetText(LONG acpFirst, LONG acpLast, WCHAR * pchPlain,
	ULONG cchPlainReq, ULONG * pcchPlainOut, TS_RUNINFO * prgRunInfo, ULONG ulRunInfoReq,
	ULONG * pulRunInfoOut, LONG * pacpNext)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(pchPlain, cchPlainReq);
	ChkComOutPtr(pcchPlainOut);
	ChkComArrayArg((char *)prgRunInfo, ulRunInfoReq);	// ???
	ChkComOutPtr(pulRunInfoOut);
	ChkComOutPtr(pacpNext);

#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("VwTextStore::GetText(%d, %d, %d, ..., %d, ...)%n",
		acpFirst, acpLast, cchPlainReq, ulRunInfoReq);
	TraceTSF(sta.Chars());
#endif
	// Caller must have a lock.
	if (!m_fLocked)
		ThrowHr(WarnHr(TS_E_NOLOCK));

	bool fDoText = cchPlainReq > 0;
	bool fDoRunInfo = ulRunInfoReq > 0;

	int cchTotalNfd = TextLength();
	int ichFirst = AcpToLog(acpFirst);
	int ichLast;
	if (acpLast == -1)
	{
		acpLast = LogToAcp(cchTotalNfd);
		ichLast = min(ichFirst + (int)cchPlainReq, cchTotalNfd);
	}
	else if (acpLast - acpFirst > (LONG)cchPlainReq)
	{
		acpLast = acpFirst + cchPlainReq;
		ichLast = AcpToLog(acpFirst + cchPlainReq);
	}
	else
		ichLast = AcpToLog(acpLast);

	// validate the start and end positions.
	if (ichFirst < 0 || ichFirst > cchTotalNfd)
		ThrowHr(WarnHr(TS_E_INVALIDPOS));
	if (ichLast < ichFirst || ichLast > cchTotalNfd)
		ThrowHr(WarnHr(TS_E_INVALIDPOS));

	// are we at the end of the document?
	if (ichFirst == cchTotalNfd && cchTotalNfd > 0)
	{
		// *pcchPlainOut and *pulRunInfoOut are already set to 0
		*pacpNext = LogToAcp(cchTotalNfd);
		return S_OK;
	}

	ULONG cchPlainNfc = acpLast - acpFirst;
	int cchReq = ichLast - ichFirst;
	if (fDoText && cchReq)
	{
		// determine if the current IME requires NFD or NFC and return the text in the
		// appropriate form
		if (IsNfdIMEActive())
		{
			cchReq = RetrieveText(ichFirst, ichLast, cchPlainReq, pchPlain);
			if (ulRunInfoReq > 0)
				*pulRunInfoOut = SetOrAppendRunInfo(prgRunInfo, ulRunInfoReq, 0, TS_RT_PLAIN, cchReq);
		}
		else
		{
			// Retrieve the text and convert to composed form (NFC). If we return the
			// decomposed form (NFD), Korean IME doesn't work properly (LT-8829).
			wchar* pchPlainNfd;
			StrUni stuPlain;
			int cchPlainNfdReq = ichLast - ichFirst;
			// We need a buffer large enough for cchPlainNfdReq characters plus NULL
			stuPlain.SetSize(cchPlainNfdReq + 1, &pchPlainNfd);
			cchReq = RetrieveText(ichFirst, ichLast, cchPlainNfdReq + 1, pchPlainNfd);
			// If we leave the buffer size, stuPlain.Length() reports a wrong length
			stuPlain.SetSize(cchReq, &pchPlainNfd);
			NormalizeText(stuPlain, pchPlain, cchPlainReq, &cchPlainNfc, prgRunInfo,
				ulRunInfoReq, pulRunInfoOut);
		}
	}
	else // empty text or we're not interested in the text
	{
		if (!m_qrootb->Selection())
			ThrowHr(WarnHr(E_FAIL));

		// Set the run info for these characters.
		// TODO JohnT: handle object replacement chars as TS_RT_OPAQUE?
		//		If we do, they must be omitted from the text returned.  (From sample program:
		//			TS_RT_OPAQUE is used to indicate characters or character sequences
		//			that are in the document, but are used privately by the application
		//			and do not map to text.	 Runs of text tagged with TS_RT_OPAQUE should
		//			NOT be included in the pchPlain or cchPlainOut [out] parameters.
		if (fDoRunInfo)
		{
			*pulRunInfoOut = 1;
			prgRunInfo[0].type = TS_RT_PLAIN;
			prgRunInfo[0].uCount = cchPlainNfc;
		}
	}
	// Set the number of characters returned.
	if (pcchPlainOut)
		*pcchPlainOut = cchPlainNfc;
	// Set the index of the next character to fetch.
	if (pacpNext)
		*pacpNext = acpFirst + cchPlainNfc;

#ifdef TRACING_TSF
	StrUni stu;
	stu.Assign("    VwTextStore::GetText() => \"");
	if (cchPlainNfc)
		stu.Append(pchPlain, cchPlainNfc);
	stu.FormatAppend(L"\" (%d)%n", cchPlainNfc);
	TraceTSF(stu.Chars());
	stu.Clear();
	for (unsigned ich = 0; ich < cchPlainNfc; ++ich)
	{
		if (ich % 16 == 0)
		{
			if (ich)
				stu.FormatAppend(L"%n");
			stu.Append(L"       ");
		}
		stu.FormatAppend(L" %04x", pchPlain[ich]);
	}
	stu.FormatAppend(L"%n");
	TraceTSF(stu.Chars());
#endif

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the text from the text source
	Returns length of text.
----------------------------------------------------------------------------------------------*/
int VwTextStore::RetrieveText(int ichFirst, int ichLast, int cbufPlainReq,
	wchar* pchPlainNfd)
{
	VwParagraphBox * pvpboxFirst;
	VwParagraphBox * pvpboxLast;
	VwTextSelection * psel = GetStartAndEndBoxes(&pvpboxFirst, &pvpboxLast);
	if (!psel)
		ThrowHr(WarnHr(E_FAIL));

	VwParagraphBox * pvpboxStart;
	VwParagraphBox * pvpboxEnd;
	int ichStart = ComputeBoxAndOffset(ichFirst, pvpboxFirst, pvpboxLast, &pvpboxStart);
	/*int ichEnd =*/ ComputeBoxAndOffset(ichLast, pvpboxFirst, pvpboxLast, &pvpboxEnd);

	int cchReq = ichLast - ichFirst;

	if (cchReq >= cbufPlainReq)
		cchReq = cbufPlainReq - 1;

	if (pvpboxStart == pvpboxEnd)
	{
		// Single paragraph text.
		// Get the characters from ichStart to ichStart + cchReq.
		pvpboxStart->Source()->FetchLog(ichStart, ichStart + cchReq, pchPlainNfd);
	}
	else
	{
		// Multi-paragraph text.
		int cchFirst = min(pvpboxStart->Source()->Cch() - ichStart, cchReq);
		int cchRemaining = cchReq - cchFirst;
		pvpboxStart->Source()->FetchLog(ichStart, ichStart + cchFirst, pchPlainNfd);
		if (cchRemaining > 0)
		{
			LONG cchParaBreak = s_cchParaBreak;
			if (cchParaBreak > cchRemaining)
				cchParaBreak = cchRemaining;
			memcpy(pchPlainNfd + cchFirst, s_stuParaBreak.Chars(),
				cchParaBreak * isizeof(wchar));
			cchRemaining -= cchParaBreak;
			if (cchRemaining > 0)
			{
				pvpboxEnd->Source()->FetchLog(0, cchRemaining,
					pchPlainNfd + cchFirst + cchParaBreak);
			}
		}
	}
	*(pchPlainNfd + cchReq) = NULL;
	return cchReq;
}

/*----------------------------------------------------------------------------------------------
	Normalize the text to NFC. For parameters see VwTextStore::GetText.
	@param pchTextNfd - The text in NFD
----------------------------------------------------------------------------------------------*/
void VwTextStore::NormalizeText(StrUni & stuText, WCHAR* pchPlain, ULONG cchPlainReq,
	ULONG * pcchPlainOut, TS_RUNINFO * prgRunInfo, ULONG ulRunInfoReq, ULONG * pulRunInfoOut)
{
	StrUtil::NormalizeStrUni(stuText, UNORM_NFC);
	*pcchPlainOut = min(cchPlainReq, (ULONG)stuText.Length());
	Assert(cchPlainReq >= (ULONG)stuText.Length());
	wcscpy(pchPlain, stuText.Chars());

	if (ulRunInfoReq > 0)
		*pulRunInfoOut = SetOrAppendRunInfo(prgRunInfo, ulRunInfoReq, 0, TS_RT_PLAIN, *pcchPlainOut);
}

/*----------------------------------------------------------------------------------------------
	Set or append the run info.
	@param prgRunInfo - Array of RunInfo
	@param ulRunInfoReq - Length of array
	@param iRunInfo - Index into RunInfo array
	@param runType - type of the run that will be set in the run info
	@param lengthNfc - length of the run
	@returns new run info index
----------------------------------------------------------------------------------------------*/
int VwTextStore::SetOrAppendRunInfo(TS_RUNINFO * prgRunInfo, ULONG ulRunInfoReq, int iRunInfo,
	TsRunType runType, int lengthNfc)
{
	// If possible, append to previous run
	if (iRunInfo > 0 && prgRunInfo[iRunInfo - 1].type == runType && (ULONG)iRunInfo <= ulRunInfoReq)
		prgRunInfo[iRunInfo - 1].uCount += lengthNfc;
	else if ((ULONG)iRunInfo < ulRunInfoReq)
	{
		prgRunInfo[iRunInfo].type = runType;
		prgRunInfo[iRunInfo].uCount = lengthNfc;
		iRunInfo++;
	}
	else
		iRunInfo++;

	return iRunInfo;
}

/*----------------------------------------------------------------------------------------------
	Replace the text selection with the supplied characters.
	See MSDN for details.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::SetText(DWORD dwFlags, LONG acpStart, LONG acpEnd,
	const WCHAR * pchText, ULONG cch, TS_TEXTCHANGE * pChange)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(pchText, cch);
	ChkComArgPtr(pChange);

#ifdef TRACING_TSF
	StrUni stu;
	stu.Format(L"VwTextStore::SetText(%x, %d, %d, \"", dwFlags, acpStart, acpEnd);
	if (pchText)
		stu.Append(pchText, cch);
	stu.FormatAppend(L"\", %d, ...)%n", cch);
	TraceTSF(stu.Chars());
	stu.Clear();
	for (unsigned ich = 0; ich < cch; ++ich)
	{
		if (ich % 16 == 0)
		{
			if (ich)
				stu.FormatAppend(L"%n");
			stu.Append(L"       ");
		}
		stu.FormatAppend(L" %04x", pchText[ich]);
	}
	stu.FormatAppend(L"%n");
	TraceTSF(stu.Chars());
#endif

	if (dwFlags == TS_ST_CORRECTION)
	{
		// REVIEW: should we pay attention to this argument?
	}

	// Change the selection.

	TS_SELECTION_ACP tsa;
	tsa.acpStart = acpStart;
	tsa.acpEnd = acpEnd;
	tsa.style.ase = TS_AE_END;
	tsa.style.fInterimChar = FALSE;
	CheckHr(SetSelection(1, &tsa));

	// Replace the new selection with the given text.

	CheckHr(InsertTextAtSelection(TS_IAS_NOQUERY, pchText, cch, NULL, NULL, pChange));

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Return formatted text data about a specified text string as an IDataObject.
	(This implementation provides the same formats as FieldWork's clipboard implementation:
	UNICODE, and OEM are the only formats provided at present. TsString almost works.)
	See MSDN for details.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::GetFormattedText(LONG acpStart, LONG acpEnd,
	IDataObject ** ppDataObject)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppDataObject);

#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("VwTextStore::GetFormattedText(%d, %d, ...)%n", acpStart, acpEnd);
	TraceTSF(sta.Chars());
#endif
	if (!s_qttmThreadMgr)
		ThrowHr(WarnHr(E_UNEXPECTED));
	if (!m_qrootb)
		ThrowHr(WarnHr(E_UNEXPECTED));

	// Make a new selection that has the given end points.
	VwTextSelectionPtr qtsel;
	CreateNewSelection(AcpToLog(acpStart), AcpToLog(acpEnd), false, &qtsel);

	ISilDataAccessPtr qsdaT;
	CheckHr(m_qrootb->get_DataAccess(&qsdaT));
	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));
	// Get a copy of the selection as a TsString, and store it in a "data object".
	ITsStringPtr qtss;
	SmartBstr sbstrNonText = L"; ";
	if (qtsel)
		CheckHr(qtsel->GetSelectionString(&qtss, sbstrNonText));
	else
	{
		// Return an empty string in the default user ws, for lack of anything better.
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		int ws;
		CheckHr(qwsf->get_UserWs(&ws));
		CheckHr(qtsf->MakeStringRgch(L"", 0, ws, &qtss));
	}
	ILgTsStringPlusWssPtr qtsswss;
	qtsswss.CreateInstance(CLSID_LgTsStringPlusWss);
	CheckHr(qtsswss->putref_String(qwsf, qtss));
	ILgTsDataObjectPtr qtsdo;
	qtsdo.CreateInstance(CLSID_LgTsDataObject);
	CheckHr(qtsdo->Init(qtsswss));
	CheckHr(qtsdo->QueryInterface(IID_IDataObject, (void **)ppDataObject));

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Leave unimplemented...we don't support embedded objects.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::GetEmbedded(LONG acpPos, REFGUID rguidService, REFIID riid,
	IUnknown ** ppunk)
{
	BEGIN_COM_METHOD;
#ifdef TRACING_TSF
	TraceTSF("GetEmbedded\n");
#endif

	return E_NOTIMPL;

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	We can't insert embedded objects at present.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::QueryInsertEmbedded(const GUID * pguidService,
	const FORMATETC * pFormatEtc, BOOL * pfInsertable)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfInsertable);
#ifdef TRACING_TSF
	TraceTSF("QueryInsertEmbedded\n");
#endif

	*pfInsertable = false;

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Leave unimplemented...we don't support embedded objects.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::InsertEmbedded(DWORD dwFlags, LONG acpStart, LONG acpEnd,
	IDataObject * pDataObject, TS_TEXTCHANGE * pChange)
{
	BEGIN_COM_METHOD;
#ifdef TRACING_TSF
	TraceTSF("InsertEmbedded\n");
#endif

	return E_NOTIMPL;

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Leave unimplemented until we discover we need them, and figure out what these "attributes"
	actually are.
	See MSDN for details (ITextStoreACP::RequestSupportedAttrs).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::RequestSupportedAttrs(DWORD dwFlags, ULONG cFilterAttrs,
	const TS_ATTRID * paFilterAttrs)
{
	BEGIN_COM_METHOD;
#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("VwTextStore::RequestSupportedAttrs(%x, %d, %g)%n",
		dwFlags, cFilterAttrs, paFilterAttrs);
	TraceTSF(sta.Chars());
#endif

	// This check is a workaround for a bug in the Japanese IME. Apparently it breaks if
	// we return S_OK for this GUID, but don't actually return any properties from
	// RetrieveRequestedAttrs. The symptom is that after starting the program and clicking in
	// some Japanese text, then clicking in text in another language (or another window
	// or application) and back in the Japanese, the Language bar is in Japanese characters
	// and does not work; nor does the IME.
	static const GUID Guid_Japanese_Bug =
		{ 0x372e0716, 0x974f, 0x40ac, { 0xa0, 0x88, 0x08, 0xcd, 0xc9, 0x2e, 0xbf, 0xbc } };
	//372e0716-974f-40ac-a088-08cdc92ebfbc

	if (*paFilterAttrs == Guid_Japanese_Bug)
		return E_NOTIMPL;
	return S_OK; // We don't support any attributes, but we can allow the request!

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Leave unimplemented until we discover we need them, and figure out what these "attributes"
	actually are.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::RequestAttrsAtPosition(LONG acpPos, ULONG cFilterAttrs,
	const TS_ATTRID * paFilterAttrs, DWORD dwFlags)
{
	BEGIN_COM_METHOD;
#ifdef TRACING_TSF
	TraceTSF("RequestAttrsAtPosition\n");
#endif

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Leave unimplemented until we discover we need them, and figure out what these "attributes"
	actually are.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::RequestAttrsTransitioningAtPosition(LONG acpPos, ULONG cFilterAttrs,
	const TS_ATTRID * paFilterAttrs, DWORD dwFlags)
{
	BEGIN_COM_METHOD;
#ifdef TRACING_TSF
	TraceTSF("RequestAttrsTransitioningAtPosition\n");
#endif

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Leave unimplemented until we discover we need them, and figure out what these "attributes"
	actually are.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::FindNextAttrTransition(LONG acpStart, LONG acpHalt,
	ULONG cFilterAttrs, const TS_ATTRID * paFilterAttrs, DWORD dwFlags, LONG * pacpNext,
	BOOL * pfFound, LONG * plFoundOffset)
{
	BEGIN_COM_METHOD;
#ifdef TRACING_TSF
	TraceTSF("FindNextAttrTransition\n");
#endif

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	We don't support attributes, so none are returned, whatever was requested.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::RetrieveRequestedAttrs(ULONG ulCount, TS_ATTRVAL * paAttrVals,
	ULONG * pcFetched)
{
	BEGIN_COM_METHOD;
#ifdef TRACING_TSF
	TraceTSF("RetrieveRequestedAttrs\n");
#endif

	*pcFetched = 0; // No attrs were retrieved, whatever was requested.

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Get the number of characters in a document.
	See MSDN for details.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::GetEndACP(LONG * pacp)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pacp);

	// Caller must have a lock.
	if (!m_fLocked)
		ThrowHr(WarnHr(TS_E_NOLOCK));

	int cchTextNfd = TextLength();
	*pacp = cchTextNfd ? LogToAcp(cchTextNfd) : 0;
#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("GetEndACP returned %d%n", (int) *pacp);
	TraceTSF(sta.Chars());
#endif

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Return a TsViewCookie data type that specifies the current active "view".
	See MSDN for details.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::GetActiveView(TsViewCookie * pvcView)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pvcView);

	if (!s_qttmThreadMgr)
		ThrowHr(WarnHr(E_UNEXPECTED));
	Assert(sizeof(TsViewCookie) >= sizeof(VwTextStore *));
	*pvcView = reinterpret_cast<TsViewCookie>(this);

#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("GetActiveView returned %d%n", (int) *pvcView);
	TraceTSF(sta.Chars());
#endif

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Convert a point in screen coordinates to an application character position.
	See MSDN for details.
	NOT IMPLEMENTED - HANDLING TEXT WITH MORE THAN ONE PARAGRAPH IS FULL OF PITFALLS.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::GetACPFromPoint(TsViewCookie vcView, const POINT * pt, DWORD dwFlags,
	LONG * pacp)
{
	BEGIN_COM_METHOD;
#ifdef TRACING_TSF
	TraceTSF("GetACPFromPoint\n");
#endif

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Get the screen extent of a bounding rectangle for the given selection.
	See MSDN for more details (ITextStoreACP::GetTextExt).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::GetTextExt(TsViewCookie vcView, LONG acpStart, LONG acpEnd,
	RECT * prc, BOOL * pfClipped)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prc);
	ChkComOutPtr(pfClipped);// value will be false, we aren't being lazy about text in store.

	// Caller must have a lock.
	if (!m_fLocked)
		return TS_E_NOLOCK;
	// Doc says this is not allowed, but TSF does it all the same, e.g., for simplified
	// Chinese. We can easily support it, so we do so.
	//// Validate the arguments according to the specs.
	//if (acpStart == acpEnd)
	//	ThrowHr(WarnHr(E_INVALIDARG));

	VwTextSelectionPtr qsel;
	CreateNewSelection(AcpToLog(acpStart), AcpToLog(acpEnd), false, &qsel);

	if (!qsel)
	{
		*prc = Rect(0, 0, 0, 0);
		return S_OK;
	}

	VwParagraphBox * pvpboxStart;
	ComBool fEndBeforeAnchor;
	CheckHr(qsel->get_EndBeforeAnchor(&fEndBeforeAnchor));

	if (fEndBeforeAnchor)
	{
		pvpboxStart = qsel->EndBox();
		if (!pvpboxStart) // Single-paragraph selection.
			pvpboxStart = qsel->AnchorBox();
	}
	else
	{
		pvpboxStart = qsel->AnchorBox();
	}

	Point pt(pvpboxStart->LeftToLeftOfDocument(), pvpboxStart->TopToTopOfDocument());
	HoldGraphicsAtSrc hg(pvpboxStart->Root(), pt);

	IVwGraphicsWin32Ptr qvg32;
	CheckHr(hg.m_qvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	HDC hdc;
	CheckHr(qvg32->GetDeviceContext(&hdc));
	HWND hwnd = ::WindowFromDC(hdc);
	if (reinterpret_cast<TsViewCookie>(this) != vcView)
		ThrowHr(WarnHr(E_NOTIMPL)); // Probably another view, but we only support the current.

	Rect rcSel(0,0,0,0); // default if no selection: top left of window.
	if (qsel)
	{
		Rect rcSecondary;
		ComBool fSplit;
		ComBool fEndBeforeAnchor;
		CheckHr(qsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rcSel,
			&rcSecondary, &fSplit, &fEndBeforeAnchor));
	}
	rcSel.ClientToScreen(hwnd);
	*prc = rcSel;
#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("GetTextExt returned %d, %d, %d, %d%n", rcSel.left, rcSel.top, rcSel.right, rcSel.bottom);
	TraceTSF(sta.Chars());
#endif

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Return the bounding box, in screen coordinates, of the display surface where the text
	stream is rendered.
	See MSDN for details.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::GetScreenExt(TsViewCookie vcView, RECT * prc)
{
	BEGIN_COM_METHOD;

	BOOL fDummy;
	HRESULT hr = GetTextExt(vcView, 0, TextLength(), prc, &fDummy);
#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("GetScreenExt returned %d, %d, %d, %d%n", prc->left, prc->top, prc->right, prc->bottom);
	TraceTSF(sta.Chars());
#endif
	return hr;

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	Convert the TsViewCookie into the corresponding HWND.
	See MSDN for details.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::GetWnd(TsViewCookie vcView, HWND * phwnd)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phwnd);

	if (!s_qttmThreadMgr)
		ThrowHr(WarnHr(E_UNEXPECTED));

	Assert(sizeof(TsViewCookie) >= sizeof(VwTextStore *));
	if (reinterpret_cast<TsViewCookie>(this) != vcView)
		ThrowHr(WarnHr(E_INVALIDARG));
	HoldScreenGraphics hg(m_qrootb);
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(hg.m_qvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	HDC hdc;
	CheckHr(qvg32->GetDeviceContext(&hdc));
	*phwnd = ::WindowFromDC(hdc);

#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("GetWnd returned %d%n", (int) *phwnd);
	TraceTSF(sta.Chars());
#endif

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

#ifdef TRACING_TSF
void DumpChars(uint cch, const OLECHAR * pchText)
{
	StrUni stu;
	for (unsigned ich = 0; ich < cch; ++ich)
	{
		if (ich % 16 == 0)
		{
			if (ich)
				stu.FormatAppend(L"%n");
			stu.Append(L"       ");
		}
		stu.FormatAppend(L" %04x", pchText[ich]);
	}
	stu.FormatAppend(L"%n");
	TraceTSF(stu.Chars());
}
#endif

/*----------------------------------------------------------------------------------------------
	Insert text at the insertion point (or selection).  This requires a Read/Write lock.
	See MSDN for details (ITextStoreACP::InsertTextAtSelection).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::InsertTextAtSelection(DWORD dwFlags, const WCHAR * pchText, ULONG cch,
	LONG * pacpStart, LONG * pacpEnd, TS_TEXTCHANGE * pChange)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(pchText, cch);
	ChkComArgPtrN(pacpStart);
	ChkComArgPtrN(pacpEnd);
	ChkComArgPtrN(pChange);

	if (!m_fLocked || (m_dwLockType & TS_LF_READWRITE) != TS_LF_READWRITE)
		ThrowHr(WarnHr(TS_E_NOLOCK));

	LONG lTemp; // dummy for start and end pointers if initially null
	//fake pacpStart and End if not provided.
	if (!pacpStart)
		pacpStart = &lTemp;
	if (!pacpEnd)
		pacpEnd = &lTemp;

	VwParagraphBox * pvpboxStart;
	VwParagraphBox * pvpboxEnd;
	bool fEndBeforeAnchor;
	VwTextSelection * psel = GetStartAndEndBoxes(&pvpboxStart, &pvpboxEnd, &fEndBeforeAnchor);
	if (!psel)
		ThrowHr(WarnHr(E_FAIL));
#ifdef TRACING_TSF
	StrUni stu;
	stu.Format(L"VwTextStore::InsertTextAtSelection(flags %x, \"", dwFlags);
	if (pchText)
		stu.Append(pchText, cch);
	stu.FormatAppend(L"\", first char %x, cch %d, ...); text to insert is:%n",
		pchText && cch ? pchText[0] : 0, cch);
	TraceTSF(stu.Chars());
	StrAnsi sta;
#endif

	int ichFirst = fEndBeforeAnchor ? psel->EndOffset() : psel->AnchorOffset();
	int ichLast = fEndBeforeAnchor ? psel->AnchorOffset() : psel->EndOffset();
#ifdef TRACING_TSF
	sta.Format("  Replacing from %d to %d%n", ichFirst, ichLast);
	TraceTSF(sta.Chars());
#endif
	int ichOldEnd = ichLast;
	if (pvpboxStart != pvpboxEnd)
	{
		// Multiple paragraph selection.
		ichOldEnd += pvpboxStart->Source()->Cch() + s_cchParaBreak;
	}

	int acpStart = LogToAcp(ichFirst);
	int acpOldEnd = LogToAcp(ichOldEnd);
	if (dwFlags & TS_IAS_QUERYONLY)
	{
		// This isn't really correct, but is the best approximation readily available.
		// If the current paragraph changes, then this won't be correct.  :-(
		// This version is for the natural behavior of leaving an IP after the inserted text.
		//*pacpStart = ichFirst + cch;
		//*pacpEnd = *pacpStart;
		// This version mimics TSFAPP and selects the inserted text.
		*pacpStart = acpStart;
		*pacpEnd = acpOldEnd; // ichFirst + cch;

#ifdef TRACING_TSF
		sta.Format("  query only returned *papcStart is %d, *pacpEnd is %d%n",
			*pacpStart, *pacpEnd);
		TraceTSF(sta.Chars());
#endif
		return S_OK;
	}
	// don't notify TSF of text and selection changes when in response to a TSF action
	m_fNotify = FALSE;
	HoldScreenGraphics hg(m_qrootb);
	// Make the change.
	// If there is a range and nothing to insert, we simulate a single delete key
	// to force the range to be deleted.
	bool fSimulateDelete = (cch == 0) && (ichFirst != ichOldEnd);
	OLECHAR chFirst = cch ? *pchText : '\0';
	if (fSimulateDelete)
		chFirst = 0x7F;
	int ws;
	CheckHr(m_qrootb->Site()->GetAndClearPendingWs(m_qrootb, &ws));
#ifdef TRACING_TSF
	stu.Format(L"  calling OnTyping, initial para contents is%n");
	TraceTSF(stu.Chars());

	ITsStringPtr qtssFirst;
	pvpboxStart->Source()->StringAtIndex(0, &qtssFirst);
	const OLECHAR * pchOld;
	int cchOld;
	CheckHr(qtssFirst->LockText(&pchOld, &cchOld));
	stu.Format(L"      sel from %d to %d; first paragraph string is \"%s\"%n", psel->AnchorOffset(), psel->EndOffset(), pchOld);
	TraceTSF(stu.Chars());
	DumpChars(cchOld, pchOld);
	CheckHr(qtssFirst->UnlockText(pchOld));
#endif
	psel->OnTyping(hg.m_qvg, const_cast<OLECHAR *>(pchText), cch,
		0, // no backspaces
		(fSimulateDelete ? 1 : 0),
		chFirst, // supposedly the first char typed, only bs and del matter
		&ws); // Enhance JohnT: need a new way to pass this info when using TSF.

	VwParagraphBox * pvpboxStartNew;
	VwParagraphBox * pvpboxEndNew;
	psel = GetStartAndEndBoxes(&pvpboxStartNew, &pvpboxEndNew);
#ifdef TRACING_TSF
	stu.Format(L"  after OnTyping%n");
	TraceTSF(stu.Chars());

	pvpboxStartNew->Source()->StringAtIndex(0, &qtssFirst);
	const OLECHAR * pchNew;
	int cchNew;
	CheckHr(qtssFirst->LockText(&pchNew, &cchNew));
	stu.Format(L"      sel from %d to %d; first paragraph string is \"%s\"%n", psel->AnchorOffset(), psel->EndOffset(), pchNew);
	TraceTSF(stu.Chars());
	DumpChars(cchNew, pchNew);
	CheckHr(qtssFirst->UnlockText(pchNew));
#endif
	// if the paragraph box is different, make sure that ichFirst is still valid and within the limits
	// of the new box
	if (pvpboxStart != pvpboxStartNew)
	{
		ichFirst = min(ichFirst, pvpboxStartNew->Source()->Cch());
		if (psel->m_qtsbProp)
		{
			ichFirst = max(ichFirst, psel->m_ichMinEditProp);
			ichFirst = min(ichFirst, psel->m_ichLimEditProp);
		}
	}
	// To mimic TSFAPP, we need to select the inserted text. Assume for now it didn't
	// include any newlines.
	// This gets a bit tricky because normalization may cause more characters than we inserted to get added.
	int ichAnchor = ichFirst; // Characters before the range inserted should not have been affected.
	psel->Hide();
	psel->m_ichAnchor = ichAnchor;
	psel->Show();
	m_qrootb->NotifySelChange(1);
	m_fNotify = TRUE;

	int acpNewEnd = LogToAcp(psel->EndOffset());
	if (!(dwFlags & TS_IAS_NOQUERY))
	{
		*pacpStart = LogToAcp(psel->AnchorOffset());
		*pacpEnd = acpNewEnd;
#ifdef TRACING_TSF
		sta.Format("  end of InsertTextAtSelection: *papcStart is %d, *pacpEnd is %d%n", *pacpStart, *pacpEnd);
	TraceTSF(sta.Chars());
#endif
	}

	// set the TS_TEXTCHANGE members
	pChange->acpStart = acpStart;
	pChange->acpOldEnd = acpOldEnd;
	pChange->acpNewEnd = acpNewEnd;
#ifdef TRACING_TSF
	sta.Format("  acpStart is %d, acpOldEnd is %d, acpNewEnd is %d%n", pChange->acpStart, pChange->acpOldEnd, pChange->acpNewEnd);
	TraceTSF(sta.Chars());
#endif

	// defer the layout change notification until the document is unlocked
	m_fLayoutChanged = TRUE;

	DoDisplayAttrs();

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

/*----------------------------------------------------------------------------------------------
	// Can't implement at present.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::InsertEmbeddedAtSelection(DWORD dwFlags, IDataObject * pDataObject,
	LONG * pacpStart, LONG * pacpEnd, TS_TEXTCHANGE * pChange)
{
	BEGIN_COM_METHOD;
#ifdef TRACING_TSF
	TraceTSF("InsertEmbeddedAtSelection\n");
#endif

	ThrowHr(WarnHr(E_NOTIMPL));

	END_COM_METHOD(g_factDummy, IID_ITextStoreACP);
}

//:>********************************************************************************************
//:>	Other Methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Retrieve the start and end boxes of the selection. Return true if there is a text selection.
	Returns a value for both boxes, even if they are the same.
----------------------------------------------------------------------------------------------*/
VwTextSelection * VwTextStore::GetStartAndEndBoxes(VwParagraphBox ** ppvpboxStart,
	VwParagraphBox ** ppvpboxEnd, bool * pfEndBeforeAnchor)
{
	VwTextSelection * psel = dynamic_cast<VwTextSelection *>(m_qrootb->Selection());
	VwParagraphBox * pvpboxStart = NULL;
	VwParagraphBox * pvpboxEnd = NULL;
	if (psel)
	{
		ComBool fEndBeforeAnchor;
		CheckHr(psel->get_EndBeforeAnchor(&fEndBeforeAnchor));
		if (pfEndBeforeAnchor)
			*pfEndBeforeAnchor = static_cast<bool>(fEndBeforeAnchor);

		if (fEndBeforeAnchor)
		{
			pvpboxStart = psel->EndBox();
			pvpboxEnd = psel->AnchorBox();
			if (!pvpboxStart) // Single-paragraph selection.
				pvpboxStart = pvpboxEnd;
		}
		else
		{
			pvpboxStart = psel->AnchorBox();
			pvpboxEnd = psel->EndBox();
			if (!pvpboxEnd) // Single-paragraph selection.
				pvpboxEnd = pvpboxStart;
		}
		if (pvpboxStart)
		{
			m_cchLastPara = pvpboxStart->Source()->Cch();
			if (pvpboxStart != pvpboxEnd)
				m_cchLastPara += s_cchParaBreak + pvpboxEnd->Source()->Cch();
		}
		else
		{
			// probably in the middle of setting up the selection. Not much we can do...
			psel = NULL;
		}
	}
	// If no selection, let the caller worry about it.
	*ppvpboxStart = pvpboxStart;
	*ppvpboxEnd = pvpboxEnd;
	return psel;
}

/*----------------------------------------------------------------------------------------------
	Compute the length of the current text (in decomposed characters NFD).
----------------------------------------------------------------------------------------------*/
int VwTextStore::TextLength()
{
	VwParagraphBox * pvpboxFirst;
	VwParagraphBox * pvpboxLast;
	VwTextSelection * psel = GetStartAndEndBoxes(&pvpboxFirst, &pvpboxLast);
	if (psel)
	{
		if (pvpboxFirst != pvpboxLast)
			return pvpboxFirst->Source()->Cch() + s_cchParaBreak + pvpboxLast->Source()->Cch();
		else
			return pvpboxFirst->Source()->Cch();
	}
	else
	{
		// Handle no selection by pretending we have an empty document.
		return 0;
	}
}

/*----------------------------------------------------------------------------------------------
	The document changed. (Ideally we'd like to know where, but at least let the service know.)
----------------------------------------------------------------------------------------------*/
void VwTextStore::OnDocChange()
{
	if (m_fNotify)
	{
		if (m_AdviseSinkInfo.m_dwMask & TS_AS_TEXT_CHANGE &&
			m_AdviseSinkInfo.m_qTextStoreACPSink)
		{
			// issue a document changed notification. The OldEnd may not be exactly right.
			TS_TEXTCHANGE ttc;
			ttc.acpStart = 0;
			ttc.acpOldEnd = m_pvpboxCurrent ? LogToAcp(m_pvpboxCurrent->Source()->Cch()) : 0;
			ttc.acpNewEnd = ttc.acpOldEnd;
#ifdef TRACING_TSF
			StrAnsi sta;
			sta.Format("VwTextStore::OnDocChange() calling AdviseSink->OnTextChange()%n");
			TraceTSF(sta.Chars());
#endif
			m_AdviseSinkInfo.m_qTextStoreACPSink->OnTextChange(0, &ttc);
		}
	}
	DoDisplayAttrs();
}

/*----------------------------------------------------------------------------------------------
	The selection changed.

	@param nHow Flag how the selection changed: ksctSamePara, ksctDiffPara, etc.
----------------------------------------------------------------------------------------------*/
void VwTextStore::OnSelChange(int nHow)
{
#ifdef TRACING_TSF
	StrAnsi sta;
	sta.Format("VwTextStore::OnSelChange(%d), m_fNotify = %s%n",
		nHow, m_fNotify ? "true" : "false");
	TraceTSF(sta.Chars());
#endif
	// since the selection has changed, we must retrieve the current writing system, so that we
	// can use it to determine whether to return NFD or NFC to TSF

	GetCurrentWritingSystem();
	// Brute force...if this works we should probably at least check that our window has focus.
	//CheckHr(s_qttmThreadMgr->SetFocus(m_qtdmDocMgr));
	if (m_fNotify)
	{
		if (nHow != ksctSamePara)
		{
			// Working on a different paragraph means we pretend the whole document changed.
			// Todo: the first box of the selection.
			VwTextSelection * psel = dynamic_cast<VwTextSelection *>(m_qrootb->Selection());
			// If it's not a text selection don't send the notification. It seems to be a bad
			// idea to tell TSF about a changed document or selection at a time when there
			// is not actually any text data it can get. Note that this may depend on what
			// scripts are installed. For example, we've had crashes here when selecting an
			// icon in interlinear text using Chinese data.
			if (!psel)
				return;
			VwParagraphBox * pvpboxNew = psel ? psel->AnchorBox() : NULL;
			if (m_AdviseSinkInfo.m_dwMask & TS_AS_TEXT_CHANGE &&
				m_AdviseSinkInfo.m_qTextStoreACPSink)
			{
				// issue a document changed notification.
				TS_TEXTCHANGE ttc;
				ttc.acpStart = 0;
				// REVIEW (DamienD): I don't think this will give an accurate acpOldEnd, since
				// the LogToAcp() method calculates the acp offset based off of the current
				// paragraphs in the selection
				ttc.acpOldEnd = LogToAcp(m_pvpboxCurrent ? m_pvpboxCurrent->Source()->Cch()
					: m_cchLastPara);
				ttc.acpNewEnd = pvpboxNew ? LogToAcp(pvpboxNew->Source()->Cch()) : 0;
#ifdef TRACING_TSF
				StrAnsi sta;
				sta.Format("VwTextStore::OnSelChange(%d) calling AdviseSink->OnTextChange()%n",
					nHow);
				TraceTSF(sta.Chars());
#endif
				if (nHow != ksctDeleted)
				{
					// When we delete the selection it seems to be disatrous to tell
					// at least the Chinese IME that the doc changed. I (JT) am not sure
					// whether this is because we're changing focus pane, or something even
					// more obscure. If you consider putting this back in, check that
					// you don't get an exception deep in TSF while switching from a
					// selection in some Chinese text in one DN field to another field.
					m_AdviseSinkInfo.m_qTextStoreACPSink->OnTextChange(0, &ttc);
				}
			}
			m_pvpboxCurrent = pvpboxNew;
		}
		if (m_AdviseSinkInfo.m_dwMask & TS_AS_SEL_CHANGE &&
				m_AdviseSinkInfo.m_qTextStoreACPSink && nHow != ksctDeleted)
		{
			// issue a selection change notification.
#ifdef TRACING_TSF
			StrAnsi sta;
			sta.Format("VwTextStore::OnSelChange(%d) calling AdviseSink->OnSelectionChange()%n",
				nHow);
			TraceTSF(sta.Chars());
#endif
			m_AdviseSinkInfo.m_qTextStoreACPSink->OnSelectionChange();
		}
	}
	// Clear any error info resulting from advising the ACP.
	// This prevents AssertNoErrorInfo from firing in debug builds.
	// In a release, we just hope anything that goes wrong in TSF won't hurt us too badly.
	IErrorInfo * pIErrorInfo = NULL;
	HRESULT hr;
	hr = GetErrorInfo(0, &pIErrorInfo);
	if(pIErrorInfo != NULL) {
#ifdef DEBUG // may as well output the info.
		BSTR bstr;
		hr = pIErrorInfo->GetDescription(&bstr);
		Assert(SUCCEEDED(hr));
		::OutputDebugString(bstr);
		::SysFreeString(bstr);
		hr = pIErrorInfo->GetSource(&bstr);
		Assert(SUCCEEDED(hr));
		::OutputDebugString(bstr);
		::SysFreeString(bstr);
#endif
		pIErrorInfo->Release();
	}

}

void VwTextStore::OnLayoutChange()
{
	m_fLayoutChanged = false;
	if (m_fNotify)
	{
		if (m_AdviseSinkInfo.m_dwMask & TS_AS_LAYOUT_CHANGE &&
				m_AdviseSinkInfo.m_qTextStoreACPSink)
		{
			// issue a layout change notification.
			TsViewCookie vcView;
			GetActiveView(&vcView);
#ifdef TRACING_TSF
			StrAnsi sta;
			sta.Format(
	"VwTextStore::OnLayoutChange() calling AdviseSink->OnLayoutChange(TS_LC_CHANGE, ...)%n");
			TraceTSF(sta.Chars());
#endif
			m_AdviseSinkInfo.m_qTextStoreACPSink->OnLayoutChange(TS_LC_CHANGE, vcView);
		}
	}
	DoDisplayAttrs();
}

/*----------------------------------------------------------------------------------------------
	Set the Text Service focus to our root box.
----------------------------------------------------------------------------------------------*/
void VwTextStore::SetFocus()
{
#ifdef TRACING_TSF
	TraceTSF("VwTextStore::SetFocus()\r\n");
#endif
	if (!s_qttmThreadMgr)
		return;

	// retrieve the current writing system, so that we can use it to determine whether to
	// return NFD or NFC to TSF
	GetCurrentWritingSystem();

	// This try/catch is a patch to try to minimize the problems of a bug which appears
	// to be in Microft's code...at least, they are throwing an exception across a COM
	// interface, which is a no-no. See LT-8483. We may want to take it out if we can
	// get a fix or work-around from Microsoft.
	try
	{
		CheckHr(s_qttmThreadMgr->SetFocus(m_qtdmDocMgr));
	}
	catch (...)
	{
		Assert(false); //, "Microsoft's thread manager threw an exception from SetFocus!");
	}
}

/*----------------------------------------------------------------------------------------------
	Create and initialize the document manager.
	This can be called more than once. It releases the old document manager if one
	already exists.
----------------------------------------------------------------------------------------------*/
void VwTextStore::Init()
{
	if (!s_qttmThreadMgr)
		return;
	// If we already have a DocMgr, we don't want to create a new one, especially without
	// properly closing off the old one with a pop to clear the reference count on this.
	// Otherwise we end up with leaking memory on this.
	if (m_qtdmDocMgr)
		return;
	// Create the Text Services Framework document manager for this "document" (root box).
	CheckHr(s_qttmThreadMgr->CreateDocumentMgr(&m_qtdmDocMgr));

	// Create and install the Text Services Framework "context".
	CheckHr(m_qtdmDocMgr->CreateContext(s_tfClientID, 0, dynamic_cast<ITextStoreACP *>(this),
		&m_qtcContext, &m_tfEditCookie));
	CheckHr(m_qtdmDocMgr->Push(m_qtcContext));

//	HRESULT hr;
//	hr = s_qttmThreadMgr->AssociateFocus(
//		HWND hwnd,
//		ITfDocumentMgr* pdimNew,
//		ITfDocumentMgr** ppdimPrev);

}

/*----------------------------------------------------------------------------------------------
	Release the interfaces installed by the constructor or by Init.
----------------------------------------------------------------------------------------------*/
void VwTextStore::Close()
{
	if (!m_qtdmDocMgr)
		return;
	AssertPtr(s_qttmThreadMgr.Ptr());
	CheckHr(m_qtdmDocMgr->Pop(TF_POPF_ALL));
	m_qtdmDocMgr.Clear();
	m_qtcContext.Clear();
	m_qrootb.Clear();
	m_qws.Clear();
}

/*----------------------------------------------------------------------------------------------
	Convert the acp (TSF offset, but already in NFD) value to an ich (Views code paragraph
	offset), and select the corresponding paragraph box that goes with it.
----------------------------------------------------------------------------------------------*/
int VwTextStore::ComputeBoxAndOffset(int acpNfd, VwParagraphBox * pvpboxFirst,
	VwParagraphBox * pvpboxLast, VwParagraphBox ** ppvpboxOut)
{
	int cchFirst = pvpboxFirst->Source()->Cch();
	if (acpNfd <= cchFirst)
	{
		*ppvpboxOut = pvpboxFirst;
		return acpNfd;
	}
	else
	{
		Assert(pvpboxFirst != pvpboxLast);
		int ich = acpNfd - cchFirst - s_cchParaBreak;
		if (ich < 0)
		{
#ifdef TRACING_TSF
			TraceTSF("VwTextStore fed acp between CR and LF in para break");
#endif
			ich = 0;
		}
		Assert(ich <= pvpboxLast->Source()->Cch());
		*ppvpboxOut = pvpboxLast;
		return ich;
	}
}

/*----------------------------------------------------------------------------------------------
	Create a new text selection based on the input document character offsets.
	If there's no current selection we can't do it; return a null pointer.
----------------------------------------------------------------------------------------------*/
void VwTextStore::CreateNewSelection(int ichFirst, int ichLast, bool fEndBeforeAnchor,
	VwTextSelection ** pptsel)
{
	AssertPtr(pptsel);
	int cch = TextLength();
	// This is required for the correct behavior of SetText, which calls SetSelection
	// internally.  It says that ichLast = -1 means to ignore ichLast.
	if (ichLast == -1)
		ichLast = ichFirst;

	if (ichFirst > cch || ichLast > cch)
	{
		// don't throw an exception here. Some IMEs (e.g. Chinese QuanPin on Vista) don't put
		// any characters in the view until later. Throwing an exception here messes up the TSF
		// so that the IME doesn't work right. (TE-6420/LT-7487). If this happens, the user typed
		// a character that shows up in the IME. Just make a selection at the end of the text.
		ichFirst = cch;
		ichLast = cch;
	}
	if (ichFirst > ichLast)
		ThrowHr(WarnHr(E_INVALIDARG));

	VwParagraphBox * pvpboxFirst;
	VwParagraphBox * pvpboxLast;
	VwTextSelection * psel; // Two lines to keep release build working.
	bool fEndBeforeStart;
	psel = GetStartAndEndBoxes(&pvpboxFirst, &pvpboxLast, &fEndBeforeStart);
	if (!psel || !pvpboxFirst)
		return;

	VwParagraphBox * pvpboxAnchor;
	VwParagraphBox * pvpboxEnd;
	int ichAnchor = ComputeBoxAndOffset(ichFirst, pvpboxFirst, pvpboxLast, &pvpboxAnchor);
	int ichEnd = ComputeBoxAndOffset(ichLast, pvpboxFirst, pvpboxLast, &pvpboxEnd);
	if (fEndBeforeAnchor)
	{
		// Swap end and anchor values.
		int ichT = ichAnchor;
		ichAnchor = ichEnd;
		ichEnd = ichT;
		VwParagraphBox * pvpboxT = pvpboxAnchor;
		pvpboxAnchor = pvpboxEnd;
		pvpboxEnd = pvpboxT;
	}
	VwTextSelectionPtr qtsel;
	if (pvpboxAnchor == pvpboxEnd)
	{
		// single-paragraph case.
		// If we're making an insertion point, typically style.ase == TS_AE_NONE.
		// In any case, neither end of the selection is 'active' in any meaningful
		// sense; the ends are the same. However, in this case it can be important
		// which way we set fAssocPrev. When the Chinese IME, for example, inserts
		// its 'placeholder' Chinese space, it next makes a selection right before it.
		// If the preceding character is in a different writing system and fAssocPrev
		// makes the selection take its properties from that preceding character,
		// the Chinese IME can get turned off before it has a chance to work.
		// On the other hand, when it's just inserted a character, it's likely to make
		// an IP right after it. Attempt to set it so that if possible, the ws is the
		// same as the current selection.
		bool fAssocPrev = true; // default.
		if (ichAnchor == ichEnd)
		{
			VwTextSelection * pselCurrent =
				dynamic_cast<VwTextSelection *> (m_qrootb->Selection());
			if (pselCurrent && pselCurrent->AnchorBox() == pvpboxAnchor
				&& pselCurrent->EndBox() == NULL)
			{
				// One case is that the current selection is a range and the new
				// one is at the start.
				if (pselCurrent->AnchorOffset() == ichAnchor && pselCurrent->EndOffset() > ichAnchor
					|| pselCurrent->EndOffset() == ichAnchor && pselCurrent->AnchorOffset() > ichAnchor)
				{
					fAssocPrev = false;
				}
				// Another one is that the current selection is an IP at the same position
				// and associated with the following character.
				else if (pselCurrent->AnchorOffset() == ichAnchor && pselCurrent->EndOffset() == ichAnchor
					&& !pselCurrent->AssocPrevious())
				{
					fAssocPrev = false;
				}
			}
		}

		qtsel.Attach(NewObj VwTextSelection(pvpboxAnchor, ichAnchor, ichEnd, fAssocPrev));
	}
	else
	{
		// multi-paragraph selection.
		qtsel.Attach(NewObj VwTextSelection(pvpboxAnchor, ichAnchor, ichEnd, true,
			pvpboxEnd));
	}
	*pptsel = qtsel.Detach();
}

void VwTextStore::AddToKeepList(LazinessIncreaser *pli)
{
	if (m_pvpboxCurrent)
		pli->KeepSequence(m_pvpboxCurrent, m_pvpboxCurrent->NextOrLazy());
}

// The specified box is being deleted. If somehow we are stil pointing at it
// (this can happen, for one example, during a replace all where NoteDependencies
// cause large-scale regeneration), clear the pointers to a safe, neutral state.
void VwTextStore::ClearPointersTo(VwParagraphBox * pvpbox)
{
	if (m_pvpboxCurrent == pvpbox)
		m_pvpboxCurrent = NULL;
}

COLORREF InterpretTfDaColor(TF_DA_COLOR tdc, COLORREF current)
{
	if (tdc.type == TF_CT_SYSCOLOR)
		return GetSysColor(tdc.nIndex);
	else if (tdc.type == TF_CT_COLORREF)
		return tdc.cr;
	else return current;
}

void VwTextStore::DoDisplayAttrs()
{
	HRESULT hr;
#ifdef TRACING_TSF
	TraceTSF("VwTextStore::DoDisplayAttrs\n");
#endif
	// May need to do this for both selected paragraphs...for now, give up unless
	// there is only one selected paragraph. Also, give up if we were not able to get
	// a category manager and display attribute manager.
	VwParagraphBox * pvpboxFirst;
	VwParagraphBox * pvpboxLast;
	bool fEndBeforeAnchor;
	VwTextSelection * psel = GetStartAndEndBoxes(&pvpboxFirst, &pvpboxLast, &fEndBeforeAnchor);
	// These tests fail in various situations, such as where TSF is not installed, or where
	// we're running automated unit tests and so don't have a real context manager.
	if (!psel || pvpboxFirst != pvpboxLast || !s_qtfCategoryMgr || !s_qtfDisplayAttributeMgr ||
		!m_qtcContext)
	{
		return;
	}

	// Get an ITfReadOnlyProperty.  Can get an ITfProperty from context...this inherits from
	// ReadOnlyProperty.  If we can't get one for some reason give up.
	ITfPropertyPtr qProp;
	IgnoreHr(hr = m_qtcContext->GetProperty(GUID_PROP_ATTRIBUTE, &qProp));
	if (FAILED(WarnHr(hr)) || !qProp)
		return;

	// This gives a minimal set of ranges for the part of the context where the property has a
	// non-null value.  Some of the intermediate ranges, however, may have null values for the
	// property.  Again, if we don't get anything useful give up.
	IEnumTfRangesPtr qEnumRanges;
	IgnoreHr(hr = qProp->EnumRanges(m_tfEditCookie, &qEnumRanges, NULL));
	if (FAILED(WarnHr(hr)) || !qEnumRanges)
		return;
	PropOverrideVec vdp;
	VwTxtSrc * pts = pvpboxFirst->Source();
	VwOverrideTxtSrcPtr qots = dynamic_cast<VwOverrideTxtSrc *>(pts);
	if (qots)
		pts = qots->EmbeddedSrc();
#ifdef TRACING_TSF
	TraceTSF("  Got some display attrs\n");
#endif

	// Loop over the ranges, if any.
	for ( ; ; )
	{
		ITfRangePtr qRange;
		ULONG crange;
		IgnoreHr(hr = qEnumRanges->Next(1, &qRange, &crange));
#ifdef TRACING_TSF
		StrAnsi sta;
		sta.Format("    HR = %x, crange is %d%n", hr, crange);
		TraceTSF(sta.Chars());
#endif
		if (FAILED(hr) || crange != 1)
			break;

		SmartVariant svar;
		IgnoreHr(hr = qProp->GetValue(m_tfEditCookie, qRange, &svar));
		if (FAILED(hr) || svar.vt != VT_I4)
			continue;
		// Doc says it may return S_FALSE if the property isn't uniform over the range.
		// But I (JohnT) don't think that should happen when the range came from an enumeration of the property.
		Assert(hr == S_OK);
		//The property is a guidatom. Convert it into a GUID. If unsuccessful give up on this range.
		GUID guid;
		IgnoreHr(hr = s_qtfCategoryMgr->GetGUID((TfGuidAtom)svar.lVal, &guid));
		if (FAILED(hr))
			continue;
		// From the guid we can get the display attribute info object for this attribute (at last!!!).
		ITfDisplayAttributeInfoPtr qDispInfo;
		IgnoreHr(hr = s_qtfDisplayAttributeMgr->GetDisplayAttributeInfo(guid, &qDispInfo, NULL));
		if (FAILED(hr))
			continue;
		TF_DISPLAYATTRIBUTE tfda;
		IgnoreHr(hr = qDispInfo->GetAttributeInfo(&tfda));
		if (FAILED(hr))
			continue;
		// Since we're an ACP document, we should be able to get an ACP range.
		ITfRangeACPPtr qRangeAcp;
		IgnoreHr(hr = qRange->QueryInterface(IID_ITfRangeACP, (void **) &qRangeAcp));
		if (FAILED(WarnHr(hr)))
			continue;
		int ichMin;
		LONG acpAnchor, cchNfc;
		// Get the range of characters that have these display attributes.
		IgnoreHr(hr = qRangeAcp->GetExtent(&acpAnchor, &cchNfc));
		int ichMinUnderlying, ichLimUnderlying;
		DispPropOverride dpo;
		ichMin = AcpToLog(acpAnchor);
		int ichLimNfc = acpAnchor + cchNfc;
		int ichLim = AcpToLog(ichLimNfc);
		// JohnT: not sure how it can get out of range, but see LT-9637; best to make sure.
		int cchRen = pts->CchRen();
		if (ichLim > cchRen)
			ichLim = cchRen;
		// Each iteration (usually only one) makes one entry in the vector, for as many
		// characters as have sufficiently uniform properties out of the range that TSF
		// wants to modify.
#ifdef TRACING_TSF
		sta.Format("     Got some display props: ichMin = %d, ichLim = %d, cchNfc = %d\n",
			ichMin, ichLim, cchNfc);
		TraceTSF(sta.Chars());
#endif
		while (ichMin < ichLim)
		{
			int isbt = 0;
			int irun = 0;
			ITsTextPropsPtr qttp;
			VwPropertyStorePtr qzvps;
			dpo.chrp = *pts->GetCharPropInfo(ichMin, &ichMinUnderlying, &ichLimUnderlying, &isbt, &irun, &qttp, &qzvps);
			dpo.ichMin = ichMin;
			dpo.ichLim = min(ichLim, ichLimUnderlying);
			if (dpo.ichMin == dpo.ichLim)
				break;

			COLORREF clrFore = dpo.chrp.clrFore;
			COLORREF clrBack = dpo.chrp.clrBack;
			COLORREF clrUnder = dpo.chrp.clrUnder;
			int unt = dpo.chrp.unt;

			// Figure the significance of the supplied display properties.
			dpo.chrp.clrFore = InterpretTfDaColor(tfda.crText, dpo.chrp.clrFore);
			dpo.chrp.clrBack = InterpretTfDaColor(tfda.crBk, dpo.chrp.clrBack);
			// These two values are redundant...eventually we will clean one out.
			dpo.chrp.clrUnder = InterpretTfDaColor(tfda.crLine, dpo.chrp.clrUnder);
			switch(tfda.lsStyle)
			{
			case TF_LS_NONE:
				dpo.chrp.unt = kuntNone;
				break;
			case TF_LS_SOLID:
				// use double to simulate a bold single line, since we don't have bold underlines.
				dpo.chrp.unt = tfda.fBoldLine ? kuntDouble : kuntSingle;
				break;
			case TF_LS_DOT:
				dpo.chrp.unt = kuntDotted;
				break;
			case TF_LS_DASH:
				dpo.chrp.unt = kuntDashed;
				break;
			case TF_LS_SQUIGGLE:
				dpo.chrp.unt = kuntSquiggle;
				break;
			}

			if (dpo.chrp.clrFore == clrFore && dpo.chrp.clrBack == clrBack && dpo.chrp.clrUnder == clrUnder && dpo.chrp.unt == unt)
			{
				// No indication of composition...somehow this is typically flashing (e.g., for Korean) on Vista.
				// We can at least simulate the XP behavior by swapping foreground and background.
				dpo.chrp.clrBack = clrFore;
				dpo.chrp.clrFore = (clrBack == kclrTransparent ? kclrWhite : clrBack);
				//dpo.chrp.clrBack = kclrRed;
				//dpo.chrp.clrFore = kclrWhite;
			}
#ifdef TRACING_TSF
		sta.Format("       PropsAre %x, %x, %x, %x from %d to %d\n",
			dpo.chrp.clrFore, dpo.chrp.clrBack, dpo.chrp.clrUnder, dpo.chrp.unt, dpo.ichMin, dpo.ichLim);
		TraceTSF(sta.Chars());
#endif
			vdp.Push(dpo);
			ichMin = dpo.ichLim;
		}
	}
	VwTxtSrc * ptxsOld = pvpboxFirst->Source();
	if (vdp.Size() == 0)
	{
		if (!qots || dynamic_cast<VwImeDisplayAttrsOverrideTxtSrc *>(qots.Ptr()) == NULL)
			return; // don't want IME override, and have either no override or some other kind.
		pvpboxFirst->SetSource(qots->EmbeddedSrc());
		OutputDebugStringA("Restoring normal source\n");
	}
	else
	{
		if (qots && dynamic_cast<VwImeDisplayAttrsOverrideTxtSrc *>(qots.Ptr()) == NULL)
		{
			// remove the other kind of override so we can display the IME one.
			// Note: this branch is currently untestable, since we don't have a workable
			// way to spell-check Chinese, nor do we do editing in the merge tools which
			// display overrides.
			pvpboxFirst->SetSource(qots->EmbeddedSrc());
			pts = pvpboxFirst->Source();
			qots.Clear(); // forces us to make a new one.
		}
		if (!qots)
		{
			qots.Attach(NewObj VwImeDisplayAttrsOverrideTxtSrc(pts));
			pvpboxFirst->SetSource(qots);
			OutputDebugStringA("Creating an override source\n");
		}
		qots->SetOverrides(vdp);
	}

	if (pvpboxFirst->Source() != ptxsOld)
	{
		// It takes a DoLayout call to force the new text source to take effect in all the string boxes.
		HoldGraphics hg(m_qrootb);
		pvpboxFirst->DoLayout(hg.m_qvg, pvpboxFirst->ComputeOuterWidth());
	}
	pvpboxFirst->Invalidate();
}

/*----------------------------------------------------------------------------------------------
	Called when a composition is started. See MSDN for details
	(ITfContextOwnerCompositionSink::OnStartComposition).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::OnStartComposition(ITfCompositionView * pComposition, BOOL * pfOk)
{
	BEGIN_COM_METHOD;
#ifdef TRACING_TSF
	TraceTSF("VwTextStore::OnStartComposition\n");
#endif
	*pfOk = TRUE;
	m_compositions.Push(pComposition);
	END_COM_METHOD(g_factDummy, IID_ITfContextOwnerCompositionSink);
}


/*----------------------------------------------------------------------------------------------
	Called when the text within a composition changes or the range of a composition changes.
	See MSDN for details (ITfContextOwnerCompositionSink::OnUpdateComposition).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::OnUpdateComposition(ITfCompositionView * pComposition,
	ITfRange * pRangeNew)
{
	BEGIN_COM_METHOD;

#ifdef TRACING_TSF
	TraceTSF("VwTextStore::OnUpdateComposition\n");
#endif

	END_COM_METHOD(g_factDummy, IID_ITfContextOwnerCompositionSink);
}


/*----------------------------------------------------------------------------------------------
	Terminate all compositions, and refresh the display attributes.
----------------------------------------------------------------------------------------------*/
void VwTextStore::TerminateAllCompositions(void)
{
#ifdef TRACING_TSF
	TraceTSF("VwTextStore::TerminateAllCompositions\n");
#endif
	// Can't have (or terminate!) compositions without a real context.
	if (!m_qtcContext)
		return;
	HRESULT hr;
	ITfContextOwnerCompositionServices * pCompServices;
	//get the ITfContextOwnerCompositionServices interface pointer
	hr = m_qtcContext->QueryInterface(IID_ITfContextOwnerCompositionServices,
		(void **)&pCompServices);
	if (SUCCEEDED(hr))
	{
		// passing NULL terminates all compositions. We should get OnEndComposition notifications.
		hr = pCompServices->TerminateComposition(NULL);
		pCompServices->Release();
#ifdef TRACING_TSF
		TraceTSF("          all compositions terminated!\n");
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Called when a composition is terminated. See MSDN for details
	(ITfContextOwnerCompositionSink::OnEndComposition).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::OnEndComposition(ITfCompositionView * pComposition)
{
	BEGIN_COM_METHOD;
#ifdef TRACING_TSF
	TraceTSF("VwTextStore::OnEndComposition\n");
#endif
	for (int i = 0; i < m_compositions.Size(); i++)
	{
		if (m_compositions[i] == pComposition)
		{
			m_compositions.Delete(i);
			if (m_compositions.Size() == 0 && m_fCommitDuringComposition)
			{
				m_fCommitDuringComposition = false;
				VwTextSelection * psel = dynamic_cast<VwTextSelection *>(m_qrootb->Selection());
				ComBool fOk;
				m_fDoingRecommit = true;
				try
				{
					CheckHr(psel->Commit(&fOk));
				}
				catch(...)
				{
					m_fDoingRecommit = false;
					throw;
				}
				m_fDoingRecommit = false;
			}

			DoDisplayAttrs();
			return S_OK;
		}
	}
	Warn("EndComposition did not find composition\n");
	END_COM_METHOD(g_factDummy, IID_ITfContextOwnerCompositionSink);
}

/*----------------------------------------------------------------------------------------------
	The caller asks to receive notifications of mouse events affecting the specified range.
	We generate an identifier that can be used to cancel the request.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::AdviseMouseSink(ITfRangeACP * range, ITfMouseSink * pSink,
	DWORD* pdwCookie)
{
	BEGIN_COM_METHOD;
#ifdef TRACING_TSF
	TraceTSF("VwTextStore::AdviseMouseSink\n");
#endif
	if (m_qMouseSink)
	{
		Warn("Multiple mouse sink requests not handled\n");
		ThrowHr(WarnHr(E_FAIL));
	}
	VwParagraphBox * pvpboxFirst;
	VwParagraphBox * pvpboxLast;
	bool fEndBeforeAnchor;
	GetStartAndEndBoxes(&pvpboxFirst, &pvpboxLast, &fEndBeforeAnchor);
	if (pvpboxFirst != pvpboxLast || !pvpboxFirst)
	{
		Warn("Mouse sink on multiple-paragraph selection not handled\n");
		ThrowHr(WarnHr(E_FAIL));
	}
	m_pvpboxMouseSink = pvpboxFirst;

	LONG acpAnchor, cch;
	range->GetExtent(&acpAnchor, &cch);
	m_ichMinMouseSink = AcpToLog(acpAnchor);
	m_ichLimMouseSink = AcpToLog(acpAnchor + cch);
	m_qMouseSink = pSink;
	*pdwCookie = 1234567; // arbitrary since we only support one.
	END_COM_METHOD(g_factDummy, IID_ITfMouseTrackerACP);
}

/*----------------------------------------------------------------------------------------------
	Cancel a previous request for mouse event notifications.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwTextStore::UnadviseMouseSink(DWORD dwCookie)
{
	BEGIN_COM_METHOD;
#ifdef TRACING_TSF
	TraceTSF("VwTextStore::UnadviseMouseSink\n");
#endif
	if (dwCookie != 1234567)
		ThrowHr(WarnHr(E_UNEXPECTED));
	m_qMouseSink.Clear();
	END_COM_METHOD(g_factDummy, IID_ITfMouseTrackerACP);
}

/*----------------------------------------------------------------------------------------------
	Send appropriate mouse event notifications, if they have been requested.  A "Mouse Down"
	event terminates all open compositions if it is not handled by the sink (unless, of course,
	the sink does not exist).
----------------------------------------------------------------------------------------------*/
bool VwTextStore::MouseEvent(int xd, int yd, RECT rcSrc1, RECT rcDst1, VwMouseEvent me)
{
	if (!m_qMouseSink)
		return false;
	// Determine whether it intersects the range that the mouse sink is interested in.
	HoldGraphicsAtDst hg(m_qrootb, Point(xd, yd));
	// Find the most local box where the user clicked, and where he clicked relative to it.
	Rect rcSrcBox;
	Rect rcDstBox;
	VwBox * pboxClick = m_qrootb->FindBoxClicked(hg.m_qvg, xd, yd, hg.m_rcSrcRoot, hg.m_rcDstRoot,
		&rcSrcBox, &rcDstBox);
	if (!pboxClick)
	{
		// The mouse event is nowhere of interest to text services.
		return EndAllCompositions(me == kmeDown);
	}

	VwSelectionPtr qvwsel;
	pboxClick->GetSelection(hg.m_qvg, m_qrootb, xd, yd, hg.m_rcSrcRoot, hg.m_rcDstRoot, rcSrcBox,
		rcDstBox, &qvwsel);

	VwTextSelection * psel = dynamic_cast<VwTextSelection *>(qvwsel.Ptr());
	if (!psel)
	{
		// It must be a text selection to be relevant for text services.
		return EndAllCompositions(me == kmeDown);
	}

	VwParagraphBox * pvpboxClick = psel->AnchorBox();
	if (pvpboxClick != m_pvpboxMouseSink)
	{
		// The mouse event is not near the box of interest.
		return EndAllCompositions(me == kmeDown);
	}

	int ichClick = psel->AnchorOffset();
	if (ichClick < m_ichMinMouseSink || ichClick > m_ichLimMouseSink)
	{
		// The mouse event is not (even close to) the range of interest.
		return EndAllCompositions(me == kmeDown);
	}
	RECT rdPrimary, rdSecondary;
	ComBool fSplit, fEndBeforeAnchor;
	Point pt(xd,yd);
	bool fFoundChar = false;
	Rect rdChar;
	if (ichClick > 0 && ichClick > m_ichMinMouseSink)
	{
		// See if the click is in the character before the position. Selection was made from a single
		// click, so it is an IP. Extend it to cover the previous character.
		psel->m_ichEnd--;
		psel->m_fEndBeforeAnchor = true;
		psel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rdPrimary,
			&rdSecondary, &fSplit, &fEndBeforeAnchor);
		rdChar = rdPrimary;
		if (rdChar.Contains(pt))
		{
			fFoundChar = true;
		}
		else
		{
			psel->m_ichEnd++;
		}
	}
	if (ichClick < pvpboxClick->Source()->Cch()  && ichClick < m_ichLimMouseSink && !fFoundChar)
	{
		// See if the click is in the character after the position. Selection was made from a single
		// click, so it is an IP. Extend it to cover the following character.
		psel->m_ichEnd++;
		psel->m_fEndBeforeAnchor = false;
		psel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rdPrimary,
			&rdSecondary, &fSplit, &fEndBeforeAnchor);
		rdChar = rdPrimary;
		if (rdChar.Contains(pt))
		{
			fFoundChar = true;
		}
	}
	if (!fFoundChar)
	{
		// The mouse event wasn't inside a character in the range, maybe just before or
		// after (or empty string).
		return EndAllCompositions(me == kmeDown);
	}

	ULONG edge = LogToAcp(ichClick) - LogToAcp(m_ichMinMouseSink);
	int section = ((rdChar.right - rdChar.left) > 0) ?
		min((xd - rdChar.left) * 4 / (rdChar.right - rdChar.left), 3) : 1;
	ULONG quadrant = (section + 2) % 4; // want 2, 3, 0, 1 for the respective sections.
	DWORD dwBtnStatus = 0;

	// Figure a set of flags that indicates approximately the state of the mouse.
	switch(me)
	{
	case kmeDown: // no shift, main button
	case kmeDblClick: // assume no shift, main button
	case kmeMoveDrag: // mouse move, main button down, assume no modifiers
		// All of these cases assume the main button is down, nothing else.
		dwBtnStatus = MK_LBUTTON;
		break;
	case kmeExtend: // main click, shift down
		dwBtnStatus = MK_LBUTTON | MK_SHIFT;
		break;
	case kmeUp: // main button up.
		// Assume nothing is down
		break;
	}

	// Send the notification.
	BOOL fEaten;
	m_qMouseSink->OnMouseEvent(edge, quadrant, dwBtnStatus, &fEaten);
#ifdef TRACING_TSF
	if (me == kmeDown)
	{
		StrAnsi sta;
		sta.Format(
"VwTextStore::MouseEvent, xd = %d (%d), width = %d, edge = %d, quadrant = %d - fEaten = %s%n",
			xd, xd - rdChar.left, rdChar.Width(), (int) edge, (int) quadrant,
			fEaten ? "true" : "false");
		TraceTSF(sta.Chars());
	}
#endif
	if (!fEaten)
	{
		// End all current compositions on mouse down.
		return EndAllCompositions(me == kmeDown);
	}
	return fEaten;
}

void VwTextStore::OnLoseFocus()
{
}

/*----------------------------------------------------------------------------------------------
	Convert the ACP character index (used by TSF manager) to the decomposed NFD
	character index used internally by views code.
----------------------------------------------------------------------------------------------*/
int VwTextStore::AcpToLog(int acpReq)
{
	if (IsNfdIMEActive())
	{
		// the ACP offset is a NFD offset
		return acpReq;
	}
	else
	{
		// convert NFC offsets to internal NFD offsets
		VwTextSelection * psel = dynamic_cast<VwTextSelection *>(m_qrootb->Selection());
		if (!psel)
			ThrowHr(WarnHr(E_FAIL));

		if (acpReq == 0)
			return 0;

		int cch = TextLength();

		if (acpReq > cch)
			// acpReq points beyond the available text, i.e. is invalid.
			return cch + 10; // arbitrary number that is bigger than NFD text

		StrUni stuIn;
		wchar* pchIn;
		stuIn.SetSize(cch + 1, &pchIn);
		RetrieveText(0, cch, cch + 1, pchIn);

		wchar szOut[kNFDBufferSize];
		UCharIterator iter;
		uiter_setString(&iter, pchIn, -1);
		int acpIch = 0;
		while (iter.hasNext(&iter))
		{
			if (acpIch >= acpReq)
				return iter.getIndex(&iter, UITER_CURRENT);
			UBool neededToNormalize;
			UErrorCode uerr = U_ZERO_ERROR;
			unorm_next(&iter, szOut, kNFDBufferSize, UNORM_NFC, 0, FALSE, &neededToNormalize, &uerr);
			Assert(U_SUCCESS(uerr));
			acpIch++;
		}
		return iter.getIndex(&iter, UITER_CURRENT);
	}
}

/*----------------------------------------------------------------------------------------------
	Convert the decomposed NFD character index (used by views code) to the ACP
	character index used used by TSF manager.
	The NFD character index is the same as the logical character index.
----------------------------------------------------------------------------------------------*/
int VwTextStore::LogToAcp(int ichReq)
{
	if (IsNfdIMEActive())
	{
		// the NFD offset is an ACP offset
		return ichReq;
	}
	else
	{
		// convert internal NFD offsets to NFC offsets
		VwTextSelection * psel = dynamic_cast<VwTextSelection *>(m_qrootb->Selection());
		if (ichReq == 0 || !psel)
			return 0;

		int cch = TextLength();
		if (ichReq > cch)
			return ichReq;

		StrUni stuIn;
		wchar* pchIn;
		stuIn.SetSize(cch + 1, &pchIn);
		RetrieveText(0, cch, cch + 1, pchIn);

		wchar szOut[kNFDBufferSize];
		UCharIterator iter;
		uiter_setString(&iter, pchIn, -1);
		int acpIch = 0;
		while (iter.hasNext(&iter))
		{
			UBool neededToNormalize;
			UErrorCode uerr = U_ZERO_ERROR;
			unorm_next(&iter, szOut, kNFDBufferSize, UNORM_NFC, 0, FALSE, &neededToNormalize, &uerr);
			Assert(U_SUCCESS(uerr));
			int index = iter.getIndex(&iter, UITER_CURRENT);
			if (index > ichReq)
				return acpIch;
			acpIch++;
		}
		return acpIch;
	}
}

/*----------------------------------------------------------------------------------------------
	Determines if the current IME requires NFD or NFC.
----------------------------------------------------------------------------------------------*/
bool VwTextStore::IsNfdIMEActive()
{
	if (!m_qws)
		return false;

	// at this point, we assume that all Keyman keyboards require NFD and all other IMEs require
	// NFC.
	SmartBstr sbstrKeymanKbd;
	CheckHr(m_qws->get_KeymanKbdName(&sbstrKeymanKbd));
	return BstrLen(sbstrKeymanKbd) > 0;
}

void VwTextStore::GetCurrentWritingSystem()
{
	m_qws.Clear();
	VwTextSelection * psel = dynamic_cast<VwTextSelection *>(m_qrootb->Selection());
	if (psel && psel->IsValid())
	{
		ITsStringPtr qtss;
		SmartBstr sbstr = L"";
		CheckHr(psel->GetSelectionString(&qtss, sbstr));

		ITsTextPropsPtr qttp;
		CheckHr(qtss->get_PropertiesAt(0, &qttp));
		int nVar, wsT;
		CheckHr(qttp->GetIntPropValues(ktptWs, &nVar, &wsT));

		if (!wsT)
		{
			// If this wasn't an editable selection (might have been a rectangle or something),
			// we don't really care about the current writing system, so just ignore it.
			ComBool fEditable;
			CheckHr(psel->get_IsEditable(&fEditable));
			if (fEditable)
				ThrowHr(E_UNEXPECTED);
			return;
		}

		ISilDataAccessPtr qsdaT;
		CheckHr(m_qrootb->get_DataAccess(&qsdaT));
		ILgWritingSystemFactoryPtr qwsf;
		CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));
		CheckHr(qwsf->get_EngineOrNull(wsT, &m_qws));
	}
}


// Explicit instantiation
#include "Vector_i.cpp"

template ComVector<ITfCompositionView>;
