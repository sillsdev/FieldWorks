/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfAtlVwWnd.h
Responsibility: John Thomson
Last reviewed: never

Description:
-------------------------------------------------------------------------------*//*:End Ignore*/

/*----------------------------------------------------------------------------------------------
	A view window that scrolls and is the main window of an ATL ActiveX control.
	Hungarian: avsw.
----------------------------------------------------------------------------------------------*/
class AfAtlVwScrollWnd : public AfAxWnd, public AfVwScrollWndBase
{
	typedef AfAxWnd SuperClass;

public:
	AfAtlVwScrollWnd();

	// We have to be tricky here. There is an inherited reference count from
	// AfClientWnd. We want to go on existing as long as there are pointers
	// to either interface.
	STDMETHOD_(ULONG, AddRef)(void)
	{
		AfWnd::AddRef();
		return m_cref;
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		// AfDeFieldEditor::Release might delete this object, so we need to get the reference
		// count before calling it and subtract one.
		long cref = m_cref;
		AfWnd::Release();
		return ::InterlockedDecrement(&cref);
	}

	virtual void PreCreateHwnd(CREATESTRUCT & cs)
	{
		SuperClass::PreCreateHwnd(cs);
		cs.style |= WS_CHILD | WS_CLIPCHILDREN;
	}

	// This method should be overrided to clear all smart pointer member variables
	// in the subclass. It gets called during the WM_NCDESTROY message.
	virtual void OnReleasePtr()
	{
		AfVwRootSite::OnReleasePtr();
		SuperClass::OnReleasePtr();
	}

	virtual void ScrollBy(int dxdOffset, int dydOffset);

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnSize(int nId, int dxp, int dyp);

	virtual void GetScrollOffsets(int * pdxd, int * pdyd);

	/*******************************************************************************************
		Command handlers.

		The functionality is all implemented on the mixin baseclass AfVwRootSite, but it doesn't
		work to put pointers to those functions directly in the command map, so we need
		functions that are directly implemented on this class.
		It also doesn't work (this appears to be a compiler bug) if the baseclass methods are
		virtual.
	*******************************************************************************************/
	virtual bool CmdFmtFnt(Cmd * pcmd)
		{ return CmdFmtFnt1(pcmd); }
	virtual bool CmdFmtPara(Cmd * pcmd)
		{ return CmdFmtPara1(pcmd); }
	virtual bool CmsFmtPara(CmdState & cms)
		{ return CmsFmtPara1(cms); }
	virtual bool CmdFmtWrtSys(Cmd * pcmd)
		{ return CmdFmtWrtSys1(pcmd); }
	virtual bool CmdFmtStyles(Cmd * pcmd)
		{ return CmdFmtStyles1(pcmd); }
	virtual bool CmdApplyNormalStyle(Cmd * pcmd)
		{ return CmdApplyNormalStyle1(pcmd); }
	virtual bool CmdFmtBulNum(Cmd * pcmd)
		{ return CmdFmtBulNum1(pcmd); }
	virtual bool CmsFmtBulNum(CmdState & cms)
		{ return CmsFmtBulNum1(cms); }
	virtual bool CmdFmtBdr(Cmd * pcmd)
		{ return CmdFmtBdr1(pcmd); }
	virtual bool CmsFmtBdr(CmdState & cms)
		{ return CmsFmtBdr1(cms); }
	virtual bool CmdCharFmt(Cmd * pcmd)
		{ return CmdCharFmt1(pcmd); }
	virtual bool CmsCharFmt(CmdState & cms)
		{ return CmsCharFmt1(cms); }
	virtual bool CmdInsertPic(Cmd * pcmd)
		{ return CmdInsertPic1(pcmd); }

	virtual bool CmdEditCut(Cmd * pcmd)
		{ return CmdEditCut1(pcmd); }
	virtual bool CmdEditCopy(Cmd * pcmd)
		{ return CmdEditCopy1(pcmd); }
	virtual bool CmdEditPaste(Cmd * pcmd)
		{ return CmdEditPaste1(pcmd); }
	virtual bool CmdEditDel(Cmd * pcmd)
		{ return CmdEditDel1(pcmd); }
	virtual bool CmdEditSelAll(Cmd * pcmd)
		{ return CmdEditSelAll1(pcmd); }
	virtual bool CmsEditCut(CmdState & cms)
		{ return CmsEditCut1(cms); }
	virtual bool CmsEditCopy(CmdState & cms)
		{ return CmsEditCopy1(cms); }
	virtual bool CmsEditPaste(CmdState & cms)
		{ return CmsEditPaste1(cms); }
	virtual bool CmsEditDel(CmdState & cms)
		{ return CmsEditDel1(cms); }
	virtual bool CmsEditSelAll(CmdState & cms)
		{ return CmsEditSelAll1(cms); }

	CMD_MAP_DEC(AfAtlVwScrollWnd);
};

template <class TBase>
class AfVwAtlControl : public CComControl<TBase, CWindowImpl<TBase, AfAtlVwScrollWnd> >
{
protected:
	typedef CComControl<TBase, CWindowImpl<TBase, AfAtlVwScrollWnd> > SuperClass;
	Rect m_rcBounds; // remember bounds from one call to the next
public:
	HRESULT OnDraw(ATL_DRAWINFO & di)
	{
		Rect & rc = *(Rect*)di.prcBounds;
		Rectangle(di.hdcDraw, rc.left, rc.top, rc.right, rc.bottom);
		if (rc.Width() != m_rcBounds.Width())
		{
			// Size has changed. I can't figure how to trap OnSize, so do layout here.
			m_rcBounds = rc;
			InitGraphics();
			Layout();
			UninitGraphics();
		}
		try
		{
			AfVwRootSite::Draw(di.hdcDraw, rc);
		}
		catch(...)
		{
			return E_FAIL;
		}

		return S_OK;
	}

	BEGIN_MSG_MAP(AfVwAtlControl<TBase>)
		// Allow the AfWnd message framework to handle any messages it wants to.
		if (DoAfWndMessageProc(hWnd, uMsg, wParam, lParam, lResult))
			return TRUE;
		CHAIN_MSG_MAP(SuperClass)
		DEFAULT_REFLECTION_HANDLER()
	END_MSG_MAP()

	LRESULT OnLButtonDown(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
	{
		// TODO JohnT(?): Add Code for message handler. Call DefWindowProc if necessary.
		return 0;
	}
	// This does not get called unless we have our own window.
	LRESULT OnSize(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL& bHandled)
	{
		InitGraphics();
		if (Layout())
			AfVwRootSite::Invalidate();
		UninitGraphics();
		return 0;
	}
};
