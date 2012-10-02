/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: DummyRootSite.h
Responsibility:
Last reviewed:

	For now this is just enough of a root site to allow views code to GetGraphics.
	Other methods trivially succeed (but don't return any useful information if
	they are supposed to).
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
	static OLECHAR * g_pszFakeObjTextRep1(L"<some silly element data/>");
	static OLECHAR * g_pszFakeObjTextRep2(L"<some other silly element data/>");

	class DummyRootSite : public IVwRootSite, public IVwObjDelNotification
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
			else if (riid == IID_IVwObjDelNotification && m_fSupportAboutToDelete)
				*ppv = static_cast<IVwObjDelNotification *>(this);
			else
				return E_NOINTERFACE;

			AddRef();
			return NOERROR;
		}
		STDMETHOD_(ULONG, AddRef)(void)
		{
			return InterlockedIncrement(&m_cref);
		}
		STDMETHOD_(ULONG, Release)(void)
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
		STDMETHOD(ScrollSelectionIntoView)(IVwSelection * psel, VwScrollSelOpts ssoFlag)
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
			case 2: // change the selection to end of doc and retry
				*pdpr = kdprRetry;
				m_qrootb->MakeSimpleSel(false, true, false, true, NULL);
				return S_OK;
			case 3: // Change sel to start of doc and retry.
				*pdpr = kdprRetry;
				m_qrootb->MakeSimpleSel(true, true, false, true, NULL);
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
		STDMETHOD(OnInsertDiffParas)(IVwRootBox * prootb, ITsTextProps * pttpDest, int cPara,
			ITsTextProps ** prgpttpSrc, ITsString ** prgptssSrc,  ITsString * ptssTrailing,
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

		STDMETHOD(AboutToDelete)(IVwSelection *psel, IVwRootBox * pRoot, HVO hvoObject, HVO hvoOwner, PropTag tag, int ihvo, ComBool fMergeNext)
		{
			m_prootbAA = pRoot;
			m_hvoObject = hvoObject;
			m_hvoOwner = hvoOwner;
			m_ihvo = ihvo;
			m_tag = tag;
			m_fMergeNext = fMergeNext;
			m_vhvoDeletedItems.Push(hvoObject);
			return S_OK;
		}

		void VerifyAboutToDelete(IVwRootBox * pRoot, HVO hvoObject, HVO hvoOwner, PropTag tag, int ihvo, bool fMergeNext)
		{
			unitpp::assert_eq("AboutToDelete wrong root box", pRoot, m_prootbAA);
			unitpp::assert_eq("AboutToDelete wrong obj", hvoObject, m_hvoObject);
			unitpp::assert_eq("AboutToDelete wrong owner", hvoOwner, m_hvoOwner);
			unitpp::assert_eq("AboutToDelete wrong tag", tag, m_tag);
			unitpp::assert_eq("AboutToDelete wrong index", ihvo, m_ihvo);
			unitpp::assert_eq("AboutToDelete wrong fMerge", fMergeNext, m_fMergeNext);
		}


		bool m_fSupportAboutToDelete;
		IVwRootBox * m_prootbAA;
		HVO m_hvoObject;
		HVO m_hvoOwner;
		PropTag m_tag;
		int m_ihvo;
		bool m_fMergeNext;
		Vector<HVO> m_vhvoDeletedItems;
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
