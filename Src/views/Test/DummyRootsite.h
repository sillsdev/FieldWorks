/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: DummyRootSite.h
Responsibility:
Last reviewed:

	For now this is just enough of a root site to allow views code to GetGraphics and perform other functions necessary for unit tests.
	Other methods trivially succeed, but don't return any useful information.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef DummyRootSite_H_INCLUDED
#define DummyRootSite_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{

	// {59CE17FA-1E0B-4656-A97B-3893AF240E01}
	DEFINE_GUID(g_GuidForTextRepOfObj1,
		0x59ce17fa, 0x1e0b, 0x4656, 0xa9, 0x7b, 0x38, 0x93, 0xaf, 0x24, 0xe, 0x1);
	// {43FB762B-D14A-4dde-B80D-C347C0F0D268}
	DEFINE_GUID(g_GuidForMakeObjFromText1,
		0x43fb762b, 0xd14a, 0x4dde, 0xb8, 0xd, 0xc3, 0x47, 0xc0, 0xf0, 0xd2, 0x68);

	// {F00B63E6-3D56-48c3-9CF3-9BFDD4C287FA}
	DEFINE_GUID(g_GuidForTextRepOfObj2,
		0xf00b63e6, 0x3d56, 0x48c3, 0x9c, 0xf3, 0x9b, 0xfd, 0xd4, 0xc2, 0x87, 0xfa);
	// {4AFB3D04-81B9-4960-88E1-739CD508941E}
	DEFINE_GUID(g_GuidForMakeObjFromText2,
		0x4afb3d04, 0x81b9, 0x4960, 0x88, 0xe1, 0x73, 0x9c, 0xd5, 0x8, 0x94, 0x1e);

	// Returned by TextRepOfObj, recognized by MakeObjFromText.
	static OleStringLiteral g_pszFakeObjTextRep1(L"<some silly element data/>");
	static OleStringLiteral g_pszFakeObjTextRep2(L"<some other silly element data/>");

	class DummyAction : public IUndoAction
	{
	public:
		DummyAction()
		{
			m_cref = 1; // initial ref count assumed on creation.
		}
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
			return ++m_cref;
	}
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv)
	{
		AssertPtr(ppv);
		if (!ppv)
			return WarnHr(E_POINTER);
		*ppv = NULL;

		if (iid == IID_IUnknown)
			*ppv = static_cast<IUnknown *>(static_cast<IUndoAction *>(this));
		else if (iid == IID_IUndoAction)
			*ppv = static_cast<IUndoAction *>(this);
		else if (iid == IID_ISupportErrorInfo)
		{
			*ppv = NewObj CSupportErrorInfo(this, IID_IUndoAction);
			return NOERROR;
		}
		else
			return E_NOINTERFACE;

		reinterpret_cast<IUnknown *>(*ppv)->AddRef();
		return S_OK;
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		if (--m_cref > 0)
			return m_cref;

		m_cref = 1;
		delete this;
		return 0;
	}

	STDMETHOD(Undo)(ComBool * pfSuccess) {return S_OK;}
	STDMETHOD(Redo)(ComBool * pfSuccess) {return S_OK;}
	STDMETHOD(Commit)() {return S_OK;}
	STDMETHOD(get_IsDataChange)(ComBool * pfRet) { *pfRet = true; return S_OK;}
	STDMETHOD(get_IsRedoable)(ComBool * pfRet) {return S_OK;}
	STDMETHOD(put_SuppressNotification)(ComBool fSuppress) {return S_OK;}

	protected:
		int m_cref;	// Standard reference count variable.
	};

	class DummyCache : public VwCacheDa
	{
		// For a few tests, notably those involving deleting complex selections, it is important
		// that something gets put in the action handler.
		// VwCacheDa, however, has no Undo support, so override this method to put something minimal there.
		STDMETHOD(SetString)(HVO hvo, PropTag tag, ITsString * ptss)
		{
			HRESULT hr = VwCacheDa::SetString(hvo, tag, ptss);
			IActionHandlerPtr qah;
			CheckHr(GetActionHandler(&qah));
			if (qah)
			{
				IUndoActionPtr qua;
				qua.Attach(NewObj DummyAction());
				CheckHr(qah->AddAction(qua));
			}
			return hr;
		}
	};

	class DummyRootSite : public IVwRootSite
	{
	public:
		DummyRootSite()
		{
			m_cref = 1;
			m_ydTopFirstVis = m_ydBottomFirstVis = m_ydTopSecondVis
				= m_ydBottomSecondVis = INT_MAX;
			m_fTestingVisRanges = false;
			m_iProbDeleteAction = 0;
			m_dptProblemType = -1;
		}
		~DummyRootSite()
		{
		}

		// IUnknown methods.
		STDMETHOD(QueryInterface)(REFIID riid, void ** ppv)
		{
			AssertPtr(ppv);
			if (!ppv)
				return WarnHr(E_POINTER);
			*ppv = NULL;

			if (riid == IID_IUnknown)
				*ppv = static_cast<IUnknown *>(static_cast<IVwRootSite *>(this));
			else if (riid == IID_IVwRootSite)
				*ppv = static_cast<IVwRootSite *>(this);
			else
				return E_NOINTERFACE;

			AddRef();
			return NOERROR;
		}
		STDMETHOD_(UCOMINT32, AddRef)(void)
		{
			return InterlockedIncrement(&m_cref);
		}
		STDMETHOD_(UCOMINT32, Release)(void)
		{
			long cref = InterlockedDecrement(&m_cref);
			if (cref == 0) {
				m_cref = 1;
				delete this;
			}
			return cref;
		}

		// IVwRootSite methods.
		STDMETHOD(InvalidateRect)(IVwRootBox * pRoot, int twLeft, int twTop, int twWidth,
			int twHeight)
		{
			return S_OK;
		}

		STDMETHOD(GetGraphics)(IVwRootBox * pRoot, IVwGraphics ** ppvg, RECT * prcSrcRoot, RECT * prcDstRoot)
		{
			*ppvg = m_qvg32.Ptr();
			(*ppvg)->AddRef();
			*prcSrcRoot = m_srcRect;
			*prcDstRoot = m_dstRect;
			return S_OK;
		}

		STDMETHOD(get_LayoutGraphics)(IVwRootBox * pRoot, IVwGraphics ** ppvg)
		{
			*ppvg = m_qvg32.Ptr();
			(*ppvg)->AddRef();
			return S_OK;
		}
		STDMETHOD(get_ScreenGraphics)(IVwRootBox * prootb, IVwGraphics ** ppvg)
		{
			return get_LayoutGraphics(prootb, ppvg);
		}
		STDMETHOD(GetTransformAtDst)(IVwRootBox * pRoot,  POINT pt,
			RECT * prcSrcRoot, RECT * prcDstRoot)
		{
			*prcSrcRoot = m_srcRect;
			*prcDstRoot = m_dstRect;
			return S_OK;
		}
		STDMETHOD(GetTransformAtSrc)(IVwRootBox * pRoot,  POINT pt,
			RECT * prcSrcRoot, RECT * prcDstRoot)
		{
			*prcSrcRoot = m_srcRect;
			*prcDstRoot = m_dstRect;
			return S_OK;
		}
		STDMETHOD(ReleaseGraphics)(IVwRootBox * prootb, IVwGraphics * pvg)
		{
			return S_OK;
		}
		STDMETHOD(GetAvailWidth)(IVwRootBox * prootb, int * ptwWidth)
		{
			*ptwWidth = 300;
			return S_OK;
		}
		STDMETHOD(RootBoxSizeChanged)(IVwRootBox * prootb)
		{
			return S_OK;
		}
		STDMETHOD(AdjustScrollRange)(IVwRootBox * prootb, int dxdSize, int dxdPosition,
			int dydSize, int dydPosition, ComBool * pfForcedScroll)
		{
			*pfForcedScroll = false;
			Adjust(m_ydBottomFirstVis, dydSize, dydPosition);
			Adjust(m_ydTopFirstVis, dydSize, dydPosition);
			Adjust(m_ydBottomSecondVis, dydSize, dydPosition);
			Adjust(m_ydTopSecondVis, dydSize, dydPosition);
			return S_OK;
		}
		// Adjust a saved Y position by dydSize if it is not INT_MAX and is greater than
		// dydPosition.
		void Adjust(int & yd, int dydSize, int dydPosition)
		{
			if (yd == INT_MAX)
				return;
			if (yd > dydPosition)
				yd += dydSize;
		}
		STDMETHOD(AdjustScrollRange1)(int dxdSize, int dxdPosition, int dydSize,
			int dydPosition, ComBool * pfForcedScroll)
		{
			*pfForcedScroll = false;
			Adjust(m_ydBottomFirstVis, dydSize, dydPosition);
			Adjust(m_ydTopFirstVis, dydSize, dydPosition);
			Adjust(m_ydBottomSecondVis, dydSize, dydPosition);
			Adjust(m_ydTopSecondVis, dydSize, dydPosition);

			return S_OK;
		}
		STDMETHOD(DoUpdates)(IVwRootBox * prootb)
		{
			return S_OK;
		}
		STDMETHOD(PropChanged)(HVO hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			return S_OK;
		}
		STDMETHOD(SelectionChanged)(IVwRootBox * prootb, IVwSelection * pvwselNew)
		{
			IVwSelection * pvwselFromRootb;
			CheckHr(prootb->get_Selection(&pvwselFromRootb));
			unitpp::assert_eq("pvwselNew == pvwselFromRootb", pvwselNew, pvwselFromRootb); // copied from SimpleRootSite.SelectionChanged
			// These calls to TextSelInfo are needed for more-complete coverage in TestVwTextStore.testOnSelectionChange:
			// They will cause a crash if the Selection is out of bounds.
			ITsString * ptss; int ich; ComBool fAssocPrev; HVO hvoObj; PropTag tag; int ws; // dummy output vars
			pvwselNew->TextSelInfo(true, &ptss, &ich, &fAssocPrev, &hvoObj, &tag, &ws);
			pvwselNew->TextSelInfo(false, &ptss, &ich, &fAssocPrev, &hvoObj, &tag, &ws);
			return S_OK;
		}
		STDMETHOD(OverlayChanged)(IVwRootBox * prootb, IVwOverlay * pvo)
		{
			return S_OK;
		}
		STDMETHOD(get_SemiTagging)(IVwRootBox * prootb, ComBool * pf)
		{
			*pf = false;
			return S_OK;
		}
		STDMETHOD(ScreenToClient)(IVwRootBox * prootb, POINT * ppnt)
		{
			ppnt->x += 50;
			ppnt->y += 100;
			return S_OK;
		}
		STDMETHOD(GetAndClearPendingWs)(IVwRootBox * prootb, int * pws)
		{
			*pws = -1;
			return S_OK;
		}
		STDMETHOD(IsOkToMakeLazy)(IVwRootBox * prootb, int ydTop, int ydBottom, ComBool * pfOK)
		{
			Assert(ydTop <= ydBottom);
			if (!m_fTestingVisRanges)
			{
				*pfOK = false;
				return S_OK;
			}
			*pfOK = !((ydBottom >= m_ydTopFirstVis && ydTop <= m_ydBottomFirstVis)
				|| (ydBottom >= m_ydTopSecondVis && ydTop <= m_ydBottomSecondVis));
			return S_OK;
		}
		STDMETHOD(ScrollSelectionIntoView)(IVwSelection * psel, VwScrollSelOpts ssoFlag,
			ComBool * pfRetVal)
		{
			return S_OK;
		}
		STDMETHOD(get_RootBox)(IVwRootBox ** pprootb)
		{
			*pprootb = 0;
			return S_OK;
		}
		STDMETHOD(get_Hwnd)(DWORD * phwnd)
		{
			*phwnd = 0;
			return S_OK;
		}
		STDMETHOD(ClientToScreen)(IVwRootBox * prootb, POINT * ppnt)
		{
			ppnt->x -= 50;
			ppnt->y -= 100;
			return S_OK;
		}
		STDMETHOD(get_BaseWs)(int * pws)
		{
			*pws = 0;
			return S_OK;
		}
		STDMETHOD(put_BaseWs)(int ws)
		{
			return S_OK;
		}
		STDMETHOD(OnProblemDeletion)(IVwSelection * psel, VwDelProbType dpt,
			VwDelProbResponse * pdpr)
		{
			m_dptProblemType = dpt;
			switch(m_iProbDeleteAction)
			{
			default: // simulate not being implemented.
				return E_NOTIMPL;
			case 1: // abort the edit
				*pdpr = kdprAbort;
				return S_OK;
			case 4: // change the selection to end of doc and claim to have done it
				*pdpr = kdprDone;
				m_qrootb->MakeSimpleSel(false, true, false, true, NULL);
				return S_OK;
			case 5: // Change sel to start of doc and claim to have done it.
				*pdpr = kdprDone;
				m_qrootb->MakeSimpleSel(true, true, false, true, NULL);
				return S_OK;
			case 6: // simulate having failed to make the change.
				*pdpr = kdprFail;
				return S_OK;
			}
		}

		IVwRootBoxPtr m_qrootbRsaeou;
		int m_ihvoRootRsaeou;
		Vector<VwSelLevInfo> m_vsliRsaeou;
		int m_tagTextPropRsaeou;
		int m_cpropPreviousRsaeou;
		int m_ichRsaeou;
		bool m_fAssocPrevRsaeou;

		STDMETHOD(RequestSelectionAtEndOfUow)(IVwRootBox *prootb, int ihvoRoot, int cvsli,
			VwSelLevInfo *prgvsli, int tagTextProp, int cpropPrevious, int ich, int wsAlt,
			ComBool fAssocPrev, ITsTextProps *tsProps)
		{
			m_qrootbRsaeou = prootb;
			m_ihvoRootRsaeou = ihvoRoot;
			m_vsliRsaeou.Clear();
			for(int i = 0; i < cvsli; i++)
				m_vsliRsaeou.Push(prgvsli[i]);
			m_tagTextPropRsaeou = tagTextProp;
			m_cpropPreviousRsaeou = cpropPrevious;
			m_ichRsaeou = ich;
			m_fAssocPrevRsaeou = fAssocPrev;

			return S_OK;
		}

		void SimulateBeginUnitOfWork()
		{
			ISilDataAccessPtr qsda;
			CheckHr(m_qrootb->get_DataAccess(&qsda));
			SmartBstr sbstrUndo = L"Undo Test Action";
			SmartBstr sbstrRedo = L"Redo Test Action";
			CheckHr(qsda->BeginUndoTask(sbstrUndo, sbstrRedo));
			IActionHandlerPtr qah;
			CheckHr(qsda->GetActionHandler(&qah));
			if (qah)
			{
				// For a few tests, notably those involving deleting complex selections, it is important
				// that something gets put in the action handler.
				// Normally (in real code) this would be done by qsda->BeginUndoTask, but we're using a simple-minded
				// subclass of ISilDataAccess here, with no undo support.
				CheckHr(qah->BeginUndoTask(sbstrUndo, sbstrRedo));
			}
		}

		void SimulateEndUnitOfWork()
		{
			ISilDataAccessPtr qsda;
			CheckHr(m_qrootb->get_DataAccess(&qsda));
			CheckHr(qsda->EndUndoTask());
			IActionHandlerPtr qah;
			CheckHr(qsda->GetActionHandler(&qah));
			if (qah)
			{
				// For a few tests, notably those involving deleting complex selections, it is important
				// that something gets put in the action handler.
				// Normally (in real code) this would be done by qsda->BeginUndoTask, but we're using a simple-minded
				// subclass of ISilDataAccess here, with no undo support.
				CheckHr(qah->EndUndoTask());
			}

			if (m_qrootbRsaeou)
			{
				IgnoreHr(m_qrootbRsaeou->MakeTextSelection(m_ihvoRootRsaeou, m_vsliRsaeou.Size(), m_vsliRsaeou.Begin(),
					m_tagTextPropRsaeou, m_cpropPreviousRsaeou, m_ichRsaeou, m_ichRsaeou, 0, m_fAssocPrevRsaeou,
					-1, NULL, true, NULL));
				m_qrootbRsaeou.Clear();
				m_vsliRsaeou.Clear();
			}
		}

		STDMETHOD(OnInsertDiffParas)(IVwRootBox * prootb, ITsTextProps * pttpDest, int cPara,
			ITsTextProps ** prgpttpSrc, ITsString ** prgptssSrc,  ITsString * ptssTrailing,
			VwInsertDiffParaResponse * pidpr)
		{
			*pidpr = kidprDefault;
			return S_OK;
		}

		STDMETHOD(OnInsertDiffPara)(IVwRootBox * prootb, ITsTextProps * pttpDest,
			ITsTextProps * pttpSrc, ITsString * ptssSrc,  ITsString * ptssTrailing,
			VwInsertDiffParaResponse * pidpr)
		{
			*pidpr = kidprDefault;
			return S_OK;
		}

		STDMETHOD(get_TextRepOfObj)(GUID * pguid, BSTR * pbstrRep)
		{
			if (*pguid == g_GuidForTextRepOfObj1)
			{
				*pbstrRep = ::SysAllocString(g_pszFakeObjTextRep1);
				return S_OK;
			}
			else if (*pguid == g_GuidForTextRepOfObj2)
			{
				*pbstrRep = ::SysAllocString(g_pszFakeObjTextRep2);
				return S_OK;
			}
			*pbstrRep = NULL;
			return S_OK;
		}
		STDMETHOD(get_MakeObjFromText)(BSTR bstrText, IVwSelection * pselDst, int * podt, GUID * pGuid)
		{
			if (wcscmp(bstrText, g_pszFakeObjTextRep1) == 0)
			{
				*pGuid = g_GuidForMakeObjFromText1;
				*podt = kodtOwnNameGuidHot;
				return S_OK;
			}
			else if (wcscmp(bstrText, g_pszFakeObjTextRep2) == 0)
			{
				*pGuid = g_GuidForMakeObjFromText2;
				*podt = kodtOwnNameGuidHot;
				return S_OK;
			}
			*pGuid = GUID_NULL;
			return S_OK;
		}

		void SetRects(RECT srcRect, RECT dstRect)
		{
			m_srcRect = srcRect;
			m_dstRect = dstRect;
		}
		void SetGraphics(IVwGraphicsWin32 * pvg32)
		{
			m_qvg32 = pvg32;
		}

		void SetVisRanges(int ydTopFirstVis, int ydBottomFirstVis, int ydTopSecondVis,
			int ydBottomSecondVis)
		{
			m_ydTopFirstVis = ydTopFirstVis;
			m_ydBottomFirstVis = ydBottomFirstVis;
			m_ydTopSecondVis = ydTopSecondVis;
			m_ydBottomSecondVis = ydBottomSecondVis;
			m_fTestingVisRanges = true;
		}

		void SetRootBox(IVwRootBox * prootb)
		{
			m_qrootb = prootb;
		}

		void SetProbDeleteAction(int iaction)
		{
			m_iProbDeleteAction = iaction;
		}

		int GetAndResetProblemType()
		{
			int dpt = m_dptProblemType;
			m_dptProblemType = -1;
			return dpt;
		}

		bool m_fSupportAboutToDelete;
		IVwRootBox * m_prootbAA;
		HVO m_hvoObject;
		HVO m_hvoOwner;
		PropTag m_tag;
		int m_ihvo;
		bool m_fMergeNext;
	protected:
		long m_cref;
		RECT m_srcRect;
		RECT m_dstRect;
		IVwGraphicsWin32Ptr m_qvg32;
		// Coordinate ranges where boxes may not be replaced by lazy ones.
		int m_ydTopFirstVis, m_ydBottomFirstVis, m_ydTopSecondVis, m_ydBottomSecondVis;
		bool m_fTestingVisRanges;
		// Tells it what action to take on receiving OnProblemDeletion; see there.
		int m_iProbDeleteAction;
		IVwRootBoxPtr m_qrootb; // note: not all clients set this; use SetRootBox if needed.
		int m_dptProblemType;
	};
	DEFINE_COM_PTR(DummyRootSite);
}

#endif /*DummyRootSite_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
