/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwTextStore.h
Responsibility:
Last reviewed: Not yet.

Description:
	Defines the class VwTextStore which implements the MS Text Services Framework interface
	ITextSourceACP.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VwTextStore_INCLUDED
#define VwTextStore_INCLUDED

namespace TestViews
{
	class TestVwTextStore;
};

typedef int HVO;
typedef int PropTag;

DEFINE_COM_PTR(ITextStoreACP);
DEFINE_COM_PTR(ITextStoreACPSink);
DEFINE_COM_PTR(ITextStoreACPServices);
DEFINE_COM_PTR(ITfThreadMgr);
DEFINE_COM_PTR(ITfDocumentMgr);
DEFINE_COM_PTR(ITfContext);
DEFINE_COM_PTR(ITfCategoryMgr);
DEFINE_COM_PTR(ITfDisplayAttributeMgr);
DEFINE_COM_PTR(ITfProperty);
DEFINE_COM_PTR(ITfReadOnlyProperty);
DEFINE_COM_PTR(IEnumTfRanges);
DEFINE_COM_PTR(ITfRange);
DEFINE_COM_PTR(ITfRangeACP);
DEFINE_COM_PTR(ITfDisplayAttributeInfo);
DEFINE_COM_PTR(ITfMouseSink);

/*----------------------------------------------------------------------------------------------
	Class: VwTextStore
	Description: implement the ITextStoreACP interface as a wrapper around a VwRootBox.
	Hungarian: txs
----------------------------------------------------------------------------------------------*/
class VwTextStore : public ITextStoreACP, public ITfContextOwnerCompositionSink,
	public ITfMouseTrackerACP, public IViewInputMgr
{
	friend class TestViews::TestVwTextStore;

public:
	VwTextStore(VwRootBox * prootb);
	~VwTextStore();

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID, LPVOID*);
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

	// ITextStoreACP methods.
	STDMETHOD(AdviseSink)(REFIID riid, IUnknown * punk, DWORD dwMask);
	STDMETHOD(UnadviseSink)(IUnknown * punk);
	STDMETHOD(RequestLock)(DWORD dwLockFlags, HRESULT * phrSession);
	STDMETHOD(GetStatus)(TS_STATUS * pdcs);
	STDMETHOD(QueryInsert)(COMINT32 acpTestStart, COMINT32 acpTestEnd, UCOMINT32 cch,
		COMINT32 * pacpResultStart, COMINT32 * pacpResultEnd);
	STDMETHOD(GetSelection)(UCOMINT32 ulIndex, UCOMINT32 ulCount, TS_SELECTION_ACP * pSelection,
		UCOMINT32 * pcFetched);
	STDMETHOD(SetSelection)(UCOMINT32 ulCount, const TS_SELECTION_ACP * pSelection);
	STDMETHOD(GetText)(COMINT32 acpStart, COMINT32 acpEnd, WCHAR * pchPlain, UCOMINT32 cchPlainReq,
		UCOMINT32 * pcchPlainOut, TS_RUNINFO * prgRunInfo, UCOMINT32 ulRunInfoReq,
		UCOMINT32 * pulRunInfoOut, COMINT32 * pacpNext);
	STDMETHOD(SetText)(DWORD dwFlags, COMINT32 acpStart, COMINT32 acpEnd, const WCHAR * pchText,
		UCOMINT32 cch, TS_TEXTCHANGE * pChange);
	STDMETHOD(GetFormattedText)(COMINT32 acpStart, COMINT32 acpEnd, IDataObject ** ppDataObject);
	STDMETHOD(GetEmbedded)(COMINT32 acpPos, REFGUID rguidService, REFIID riid, IUnknown ** ppunk);
	STDMETHOD(QueryInsertEmbedded)(const GUID * pguidService, const FORMATETC * pFormatEtc,
		BOOL * pfInsertable);
	STDMETHOD(InsertEmbedded)(DWORD dwFlags, COMINT32 acpStart, COMINT32 acpEnd,
		IDataObject * pDataObject, TS_TEXTCHANGE * pChange);
	STDMETHOD(RequestSupportedAttrs)(DWORD dwFlags, UCOMINT32 cFilterAttrs,
		const TS_ATTRID * paFilterAttrs);
	STDMETHOD(RequestAttrsAtPosition)(COMINT32 acpPos, UCOMINT32 cFilterAttrs,
		const TS_ATTRID * paFilterAttrs, DWORD dwFlags);
	STDMETHOD(RequestAttrsTransitioningAtPosition)(COMINT32 acpPos, UCOMINT32 cFilterAttrs,
		const TS_ATTRID * paFilterAttrs, DWORD dwFlags);
	STDMETHOD(FindNextAttrTransition)(COMINT32 acpStart, COMINT32 acpHalt, UCOMINT32 cFilterAttrs,
		const TS_ATTRID * paFilterAttrs, DWORD dwFlags, COMINT32 * pacpNext, BOOL * pfFound,
		COMINT32 * plFoundOffset);
	STDMETHOD(RetrieveRequestedAttrs)(UCOMINT32 ulCount, TS_ATTRVAL * paAttrVals,
		UCOMINT32 * pcFetched);
	STDMETHOD(GetEndACP)(COMINT32 * pacp);
	STDMETHOD(GetActiveView)(TsViewCookie * pvcView);
	STDMETHOD(GetACPFromPoint)(TsViewCookie vcView, const POINT * pt, DWORD dwFlags,
		COMINT32 * pacp);
	STDMETHOD(GetTextExt)(TsViewCookie vcView, COMINT32 acpStart, COMINT32 acpEnd, RECT * prc,
		BOOL * pfClipped);
	STDMETHOD(GetScreenExt)(TsViewCookie vcView, RECT * prc);
	STDMETHOD(GetWnd)(TsViewCookie vcView, HWND * phwnd);
	STDMETHOD(InsertTextAtSelection)(DWORD dwFlags, const WCHAR * pchText, UCOMINT32 cch,
		COMINT32 * pacpStart, COMINT32 * pacpEnd, TS_TEXTCHANGE * pChange);
	STDMETHOD(InsertEmbeddedAtSelection)(DWORD dwFlags, IDataObject * pDataObject,
		COMINT32 * pacpStart, COMINT32 * pacpEnd, TS_TEXTCHANGE * pChange);

	//ITfContextOwnerCompositionSink methods
	STDMETHOD(OnStartComposition)(ITfCompositionView *pComposition, BOOL *pfOk);
	STDMETHOD(OnUpdateComposition)(ITfCompositionView *pComposition, ITfRange *pRangeNew);
	STDMETHOD(OnEndComposition)(ITfCompositionView *pComposition);

	// ITfMouseTrackerACP
	STDMETHOD(AdviseMouseSink)(ITfRangeACP * range, ITfMouseSink* pSink,
		DWORD* pdwCookie);
	STDMETHOD(UnadviseMouseSink)(DWORD dwCookie);

	// IViewInputMgr
	STDMETHOD(Init)(IVwRootBox * prootb);
	STDMETHOD(Close)();
	STDMETHOD(OnTextChange)();
	STDMETHOD(OnSelectionChange)(int nHow);
	STDMETHOD(OnLayoutChange)();
	STDMETHOD(SetFocus)();
	STDMETHOD(OnMouseEvent)(int xd, int yd, RECT rcSrc1, RECT rcDst1, VwMouseEvent me, ComBool * pfProcessed);
	STDMETHOD(KillFocus)();
	STDMETHOD(OnUpdateProp)(ComBool * pSuppressNormalization);
	STDMETHOD(get_IsCompositionActive)(ComBool * pfCompositionActive);
	STDMETHOD(get_IsEndingComposition)(ComBool * pfDoingRecommit);
	STDMETHOD(TerminateAllCompositions)();


protected:
	// Member variables
	long m_cref;
	// We keep references to compositions as a desperate attempt to make them work.
	ComVector<ITfCompositionView> m_compositions;

	struct AdviseSinkInfo
	{
		IUnknownPtr  m_qunkID;
		ITextStoreACPSinkPtr m_qTextStoreACPSink;
		DWORD m_dwMask;
	} m_AdviseSinkInfo;
	ITextStoreACPServicesPtr m_qServices;

	// Set this to false to suppress all notifications, typically while we do something
	// through the interface that would produce a notification if done any other way.
	bool m_fNotify;
	bool m_fLocked;
	DWORD m_dwLockType;
	bool m_fPendingLockUpgrade;
	bool m_fLayoutChanged;	// Set when TSF changes layout but can't notify until lock released.
	bool m_fInterimChar;	// Used in intermediate states of far-east IMEs.

	VwRootBoxPtr m_qrootb;

	// if non-null, receives mouse notifications, if mouse over range of characters specified
	// by following variables.
	ITfMouseSinkPtr m_qMouseSink;
	VwParagraphBox * m_pvpboxMouseSink;
	int m_ichMinMouseSink; // in NFD form
	int m_ichLimMouseSink; // in NFD form
	// This keeps track of the length of the paragraph we last worked on.
	// It is used to give some sort of reasonable DocChanged notification
	// when the selection is destroyed.
	int m_cchLastPara; // in NFD form
	// This is set true if a property is updated without normalization (and therefore not
	// in the database) in order to avoid messing up compositions.
	bool m_fCommitDuringComposition;
	bool m_fDoingRecommit;

	static const int kNFDBufferSize = 64;
	ILgWritingSystemPtr m_qws;
	int AcpToLog(int acpReq);
	int LogToAcp(int ichReq);
	bool IsNfdIMEActive();
	virtual void GetCurrentWritingSystem();

	VwRootBox * m_prootb;

public:
	static ITfThreadMgrPtr s_qttmThreadMgr;
	static TfClientId s_tfClientID;
	static ITfCategoryMgrPtr s_qtfCategoryMgr;
	static ITfDisplayAttributeMgrPtr s_qtfDisplayAttributeMgr;
	ITfDocumentMgrPtr m_qtdmDocMgr;
	ITfContextPtr m_qtcContext;
	TfEditCookie m_tfEditCookie;

private:
	// Internal methods.
	bool _LockDocument(DWORD dwLockFlags)
	{
		if (m_fLocked)
			return false;
		m_fLocked = true;
		m_dwLockType = dwLockFlags;
		return true;
	}

	void _UnlockDocument()
	{
		m_fLocked = false;
		m_dwLockType = 0;

		// if there is a pending lock upgrade, grant it
		if (m_fPendingLockUpgrade)
		{
			m_fPendingLockUpgrade = false;
			HRESULT hr;
			RequestLock(TS_LF_READWRITE, &hr);
		}

		// if any layout changes occurred during the lock, notify the manager
		if (m_fLayoutChanged)
		{
			m_fLayoutChanged = false;
			TsViewCookie tvc;
			GetActiveView(&tvc);
			m_AdviseSinkInfo.m_qTextStoreACPSink->OnLayoutChange(TS_LC_CHANGE, tvc);
		}
	}

	VwTextSelection * GetStartAndEndBoxes(VwParagraphBox ** ppvpboxStart,
		VwParagraphBox ** ppvpboxEnd, bool * pfEndBeforeAnchor = NULL);
	int TextLength();
	int ComputeBoxAndOffset(int acpNfd, VwParagraphBox * pvpboxFirst, VwParagraphBox * pvpboxLast,
		VwParagraphBox ** ppvpboxOut);
	void CreateNewSelection(int ichFirst, int ichLast, bool fEndBeforeAnchor,
		VwTextSelection ** pptsel);
	void ClearPointersTo(VwParagraphBox * pvpbox);
	void DoDisplayAttrs();
	// Conditionally terminate all compositions and return false.  Used mostly in MouseEvent().
	bool EndAllCompositions(bool fStop)
	{
		if (fStop)
			TerminateAllCompositions();
		return false;
	}
	DWORD SuspendAdvise(IUnknown ** ppunk);

	void NoteCommitDuringComposition()
	{
		m_fCommitDuringComposition = true;
	}

private:
	int RetrieveText(int ichFirst, int ichLast, int cchPlainReqNfd, wchar* pchPlainNfd);
	void NormalizeText(StrUni & stuText, WCHAR* pchPlain, UCOMINT32 cchPlainReq,
		UCOMINT32 * pcchPlainOut, TS_RUNINFO * prgRunInfo, UCOMINT32 ulRunInfoReq,
		UCOMINT32 * pulRunInfoOut);
	int SetOrAppendRunInfo(TS_RUNINFO * prgRunInfo, UCOMINT32 ulRunInfoReq, int iRunInfo,
		TsRunType runType, int length);
};

DEFINE_COM_PTR(VwTextStore);

#endif  //VwTextStore_INCLUDED
