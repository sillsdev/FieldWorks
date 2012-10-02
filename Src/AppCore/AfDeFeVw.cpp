/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeVw.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	A superclass for client windows that consist entirely of a single view.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE


//:>********************************************************************************************
//:>	Constructor/Destructor methods.
//:>********************************************************************************************

AfDeFeVw::AfDeFeVw()
{
	ModuleEntry::ModuleAddRef();

	m_dxpLayoutWidth = -50000; // unlikely to be real current window width!
	m_cactInitGraphics = 0;
	Assert(!m_qrootb);
}

AfDeFeVw::~AfDeFeVw()
{
	ModuleEntry::ModuleRelease();
}

static DummyFactory g_fact1(_T("SIL.AppCore.AfDeFeVw"));

//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP AfDeFeVw::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwRootSite)
		*ppv = static_cast<IVwRootSite *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwRootSite);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	IRootSite Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Arguments:
		pRoot				the sender
		xsLeft, xsTop, xsWidth, xsHeight			relative to top left of root box
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::InvalidateRect(IVwRootBox * pRoot, int xsLeft, int ysTop,
	int xsWidth, int ysHeight)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pRoot);

	if (m_qdvw)
	{
		// Delegate
		return m_qdvw->InvalidateRect(pRoot, xsLeft, ysTop, xsWidth, ysHeight);
	}
	HoldGraphics hg(this);
	Rect rect;
	rect.left = hg.m_rcSrcRoot.MapXTo(xsLeft, hg.m_rcDstRoot);
	rect.top = hg.m_rcSrcRoot.MapYTo(ysTop, hg.m_rcDstRoot);
	rect.right = hg.m_rcSrcRoot.MapXTo(xsLeft + xsWidth, hg.m_rcDstRoot);;
	rect.bottom = hg.m_rcSrcRoot.MapXTo(ysTop + ysHeight, hg.m_rcDstRoot);;
	::InvalidateRect(m_qadsc->Hwnd(), &rect, true);

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
Deletes the selected text in this this control.
----------------------------------------------------------------------------------------------*/
void AfDeFeVw::DeleteSelectedText()
{
	IVwRootSitePtr qvrs;
	CheckHr(m_qrootb->get_Site(&qvrs));
	AfVwRootSite * pvwnd = dynamic_cast<AfVwRootSite *>(qvrs.Ptr());
	pvwnd->OnChar(127, 1, 83);
}


/*----------------------------------------------------------------------------------------------
Returns true if there is selected text in this this control.
@return True if there is text selected.
----------------------------------------------------------------------------------------------*/
bool AfDeFeVw::IsTextSelected()
{
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (qvwsel)
	{
		ITsStringPtr qtssSel;
		int cchSel = 0;
		SmartBstr sbstr = L"";
		if (qvwsel) // fails also if no root box
			CheckHr(qvwsel->GetSelectionString(&qtssSel, sbstr));
		if (qtssSel)
			CheckHr(qtssSel->get_Length(&cchSel));
		if (cchSel)
		{
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Return the rootbox embedded in this view site.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::get_RootBox(IVwRootBox ** pprootb)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pprootb);
	*pprootb = m_qrootb;
	AddRefObj(*pprootb);
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Return the HWND associated with this view site.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::get_Hwnd(DWORD * phwnd)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(phwnd);
	AfWnd * pwnd = Window();
	if (pwnd)
		*phwnd = (DWORD)pwnd->Hwnd();
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Default handling of inserted paragraphs with different properties.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::OnInsertDiffParas(IVwRootBox * prootb, ITsTextProps * pttpDest,
	int cPara, ITsTextProps ** prgpttpSrc, ITsString ** prgptssSrc,  ITsString * ptssTrailing,
	VwInsertDiffParaResponse * pidpr)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pidpr);
	*pidpr = kidprDefault;
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Get a graphics object in an appropriate state for drawing and measuring in the view.
	The calling method should pass the IVwGraphics back to ReleaseGraphics() before
	it returns. In particular, problems will arise if OnPaint() gets called before the
	ReleaseGraphics() method.
	ENHANCE JohnT(?): we probably need a better way to handle this. Most likely: make the
	VwGraphics object we cache a true COM object so its reference count is meaningful; have this
	method create a new one. Problem: a useable VwGraphics object has a device context that is
	linked to a particular window; if the window closes, the VwGraphics is not useable, whatever
	its reference count says. It may therefore be that we just need to allocate a copy in this
	method, leaving the member variable alone. Or, the current strategy may prove adequate.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::GetGraphics(IVwRootBox * prootb, IVwGraphics ** ppvg, RECT * prcSrcRoot,
	RECT * prcDstRoot)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppvg);
	ChkComArgPtrN(prcSrcRoot);
	ChkComArgPtrN(prcDstRoot);

	if (m_qdvw)
	{
		// Delegate
		return m_qdvw->GetGraphics(prootb, ppvg, prcSrcRoot, prcDstRoot);
	}

	InitGraphics();
	*ppvg = m_qvg;
	m_qvg.Ptr()->AddRef();
	GetCoordRects(m_qvg, prcSrcRoot, prcDstRoot);

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}
/*----------------------------------------------------------------------------------------------
	Get a graphics object in an appropriate state for drawing and measuring in the view.
	The calling method should pass the IVwGraphics back to ReleaseGraphics() before
	it returns. In particular, problems will arise if OnPaint() gets called before the
	ReleaseGraphics() method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::get_LayoutGraphics(IVwRootBox * prootb, IVwGraphics ** ppvg)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppvg);

	if (m_qdvw)
	{
		// Delegate
		return m_qdvw->get_LayoutGraphics(prootb, ppvg);
	}

	InitGraphics();
	*ppvg = m_qvg;
	m_qvg.Ptr()->AddRef();

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}
/*----------------------------------------------------------------------------------------------
	Screen version is the same except for print layout views.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::get_ScreenGraphics(IVwRootBox * prootb, IVwGraphics ** ppvg)
{
	return get_LayoutGraphics(prootb, ppvg);
}

/*----------------------------------------------------------------------------------------------
	Screen version is just like the relevant part of GetGraphics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::GetTransformAtDst(IVwRootBox * prootb,  POINT pt,
	RECT * prcSrcRoot, RECT * prcDstRoot)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(prcSrcRoot);
	ChkComArgPtrN(prcDstRoot);

	if (m_qdvw)
	{
		// Delegate
		return m_qdvw->GetTransformAtDst(prootb, pt, prcSrcRoot, prcDstRoot);
	}
	GetCoordRects(m_qvg, prcSrcRoot, prcDstRoot);
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Screen version is just like the relevant part of GetGraphics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::GetTransformAtSrc(IVwRootBox * prootb,  POINT pt,
	RECT * prcSrcRoot, RECT * prcDstRoot)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(prcSrcRoot);
	ChkComArgPtrN(prcDstRoot);

	if (m_qdvw)
	{
		// Delegate
		return m_qdvw->GetTransformAtSrc(prootb, pt, prcSrcRoot, prcDstRoot);
	}
	GetCoordRects(m_qvg, prcSrcRoot, prcDstRoot);
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Inform the container when done with the graphics object
	ENHANCE JohnT(?): could we somehow have this handled by the Release method of the
	IVwGraphics?
	But that method does not know anything about the status or source of its hdc.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::ReleaseGraphics(IVwRootBox * prootb, IVwGraphics * pvg)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);

	if (m_qdvw)
	{
		// Delegate
		return m_qdvw->ReleaseGraphics(prootb, pvg);
	}
	Assert (pvg == m_qvg.Ptr());
	UninitGraphics();

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Get the width available for laying things out in the view.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::GetAvailWidth(IVwRootBox * prootb, int * ptwWidth)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptwWidth);

	if (m_qdvw)
	{
		// Delegate
		return m_qdvw->GetAvailWidth(prootb, ptwWidth);
	}
	*ptwWidth = LayoutWidth();

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Notifies the site that the size of the root box changed; scroll ranges and/or
	window size may need to be updated. A field editor updates the window size.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::RootBoxSizeChanged(IVwRootBox * prootb)
{
	BEGIN_COM_METHOD;

	// OPTIMIZE JohnT: this overdoes things a bit, it recomputes all sizes.
	m_qadsc->FieldSizeChanged();

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Adjust the scroll range when some lazy box got expanded. Needs to be done for both panes
	if we have more than one.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::AdjustScrollRange(IVwRootBox * prootb, int dxdSize, int dxdPosition,
	int dydSize, int dydPosition, ComBool * pfForcedScroll)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfForcedScroll);

	if (!dxdSize && !dydSize)
		return S_OK;

	// Recompute field positions. Don't redraw until we fix the scroll position if needed.
	m_qadsc->FieldSizeChanged(true);
	SCROLLINFO si = {isizeof(si), SIF_PAGE | SIF_POS | SIF_RANGE};
	::GetScrollInfo(m_qadsc->Hwnd(), SB_VERT, &si);
	// Adjust the scroll position if the changed happened above the current page
	if (si.nPos > dydPosition)
		si.nPos += dydSize;
	// If this makes the scroll position out of range report and fix
	if (si.nPos + (int)(si.nPage) > si.nMax)
	{
		si.nPos = max(si.nMax - si.nPage, 0);
		*pfForcedScroll = true;
	}
	::SetScrollInfo(m_qadsc->Hwnd(), SB_VERT, &si, true);
	// For now always invalidate. It's safe, and it is hard to figure whether
	// the change in size of this field editor affected any other that is visible.
	::InvalidateRect(m_qadsc->Hwnd(), NULL, false);

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Cause the display of the root to update.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::DoUpdates(IVwRootBox * prootb)
{
	BEGIN_COM_METHOD;

	if (m_qdvw)
	{
		// Delegate
		return m_qdvw->DoUpdates(prootb);
	}
	MSG message;
	if (::PeekMessage(&message, m_qadsc->Hwnd(), WM_PAINT, WM_PAINT, PM_REMOVE))
		::DispatchMessage(&message);

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	When the selection is changed, it propagates this to its site.
	Since, any time there is a selection in an AfDeFeVw, we overlay an AfDeVwWnd subclass
	and let it handle things, I don't see any need for this method to do anything. (We have to
	have it because it's in the interface.)
	It it were possible to get a selection and still have this be the active root site, we
	might need to propagate this to m_qdvw, if it exists.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::SelectionChanged(IVwRootBox * prootb, IVwSelection * pvwselNew)
{
	BEGIN_COM_METHOD;
	return S_OK;
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	When the state of the overlays changes, it propagates this to its site.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::OverlayChanged(IVwRootBox * prootb, IVwOverlay * pvo)
{
	BEGIN_COM_METHOD;
	return S_OK;
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Return true if this kind of window uses semi-tagging.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::get_SemiTagging(IVwRootBox * prootb, ComBool * pf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pf);

	*pf = FALSE;

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Converts view output coords to absolute screen coordinates.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::ScreenToClient(IVwRootBox * prootb, POINT * ppnt)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(ppnt);
	::ScreenToClient(m_qadsc->Hwnd(), ppnt);

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Converts absolute screen coordinates to view output coords.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::ClientToScreen(IVwRootBox * prootb, POINT * ppnt)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(ppnt);
	::ClientToScreen(m_qadsc->Hwnd(), ppnt);

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Pending writing system not applicable to printing. -1 is a safe default if by some bizarre
	chance it gets called.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::GetAndClearPendingWs(IVwRootBox * prootb, int * pws)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pws);

	*pws = -1;

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Answer whether boxes in the specified range of destination coordinates
	may usefully be converted to lazy boxes. Should at least answer false
	if any part of the range is visible. The default implementation avoids
	converting stuff within about a screen's height of the visible part(s).

	We don't generally do laziness in DE fields, so this one just says 'no'.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::IsOkToMakeLazy(IVwRootBox * prootb, int ydTop, int ydBottom,
	ComBool * pfOK)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfOK); // sets it false.
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	 The user has attempted to delete something which the system does not
	 inherently know how to delete. The dpt argument indicates the type of
	 problem.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeFeVw::OnProblemDeletion(IVwSelection * psel, VwDelProbType dpt,
	VwDelProbResponse * pdpr)
{
	BEGIN_COM_METHOD
	return E_NOTIMPL;
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

//:>********************************************************************************************
//:>	Utility methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Make sure the graphics object has a DC. If it already has, increment a count,
	so we know when to really free the DC. These are used only when we don't have our own
	window.
----------------------------------------------------------------------------------------------*/
void AfDeFeVw::InitGraphics()
{
	if (m_cactInitGraphics == 0)
	{
		// We are asking for a VwGraphics but haven't been given a DC. Make one.
		HWND hwndTmp = m_qadsc->Hwnd();
		HDC hdc = ::GetDC(hwndTmp);
		if (!m_qvg)
		{
			m_qvg.CreateInstance(CLSID_VwGraphicsWin32);
		}
		m_qvg->Initialize(hdc); // puts the DC in the right state
	}
	m_cactInitGraphics++;
}

void AfDeFeVw::UninitGraphics()
{
	m_cactInitGraphics--;
	if (m_cactInitGraphics == 0)
	{
		// We have released as often as we init'd. The device context must have been
		// made in InitGraphics. Release it.
		HDC hdc;
		CheckHr(m_qvg->GetDeviceContext(&hdc));
		m_qvg->ReleaseDC();
		HWND hwndTmp = m_qadsc->Hwnd();
		int iSuccess;
		iSuccess = ::ReleaseDC(hwndTmp, hdc);
//		iSuccess = ::ReleaseDC(m_hwnd, hdc);
		Assert(iSuccess);
	}
}

/*----------------------------------------------------------------------------------------------
	Return the layout width for the window.
	NOTE: you must save a valid DC into m_vg before calling this routine.
----------------------------------------------------------------------------------------------*/
int AfDeFeVw::LayoutWidth()
{
	return m_dxpLayoutWidth;
}

/*----------------------------------------------------------------------------------------------
	Update the position of the field. It is updated whenever it gets painted, but if you need
	an accurate position of a field that has not been painted, call this. This is also important
	when calling methods of the root box that give results in drawing coords.

	Note that this does not fix the height and width of m_rcClip, since nothing uses them.
----------------------------------------------------------------------------------------------*/
void AfDeFeVw::UpdatePosition()
{
	int ypTopField = GetDeWnd()->TopOfField(this);
	m_rcClip.top = ypTopField;
	m_rcClip.left = GetDeWnd()->GetBranchWidth(this);
}

/*----------------------------------------------------------------------------------------------
	Construct coord transformation rectangles. Height and width are dots per inch.
	src origin is 0, dest origin is controlled by scrolling.
----------------------------------------------------------------------------------------------*/
void AfDeFeVw::GetCoordRects(IVwGraphics * pvg, RECT * prcSrcRoot, RECT * prcDstRoot)
{
	prcSrcRoot->left = prcSrcRoot->top = 0;
	int dxInch;
	int dyInch;
	pvg->get_XUnitsPerInch(&dxInch);
	pvg->get_YUnitsPerInch(&dyInch);
	prcSrcRoot->right = dxInch;
	prcSrcRoot->bottom = dyInch;

	prcDstRoot->left = m_rcClip.left;
	// The + 1 and +2 keep the text clear of the border.
	if (GetDeWnd()->HasVerticalTreeSeparatorLine())
		prcDstRoot->left += 2;
	prcDstRoot->top = (m_rcClip.top + 1);
	prcDstRoot->right = prcDstRoot->left + dxInch;
	prcDstRoot->bottom = prcDstRoot->top + dyInch;
}

/*----------------------------------------------------------------------------------------------
	Lay out your root box and return its height. If necessary, attempt to create the root box.
----------------------------------------------------------------------------------------------*/
int AfDeFeVw::SetHeightAt(int dxpWidth)
{
	dxpWidth -= 4; // Allow two pixel right and left to keep text clear of border
	if (dxpWidth != m_dxpLayoutWidth)
	{
		try
		{
			InitGraphics();
			m_dxpLayoutWidth = dxpWidth;
			if (!m_qrootb)
			{
				ILgWritingSystemFactoryPtr qwsf;
				GetLpInfo()->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
				MakeRoot(m_qvg, qwsf, &m_qrootb);
			}
			// If we have less than 1 point, probably the window has not received its initial
			// OnSize message yet, and we can't do a meaningful layout.
			if (m_dxpLayoutWidth < 2)
			{
				m_dxpLayoutWidth = -50000; // no drawing until we get reasonable size
				UninitGraphics();
				return 0;
			}
			HRESULT hr;
			IgnoreHr(hr = m_qrootb->Layout(m_qvg, dxpWidth));
			if (FAILED(hr))
			{
				Warn("Root box layout failed");
				m_dxpLayoutWidth = -50000; // no drawing until we get successful layout
				UninitGraphics();
				return 0;
			}
			UninitGraphics();
			// The active editing window, if it exists, may get an OnSize message too. If so,
			// it does not need to redo the layout.
			if (m_qdvw)
				m_qdvw->SetLastLayoutWidth(m_dxpLayoutWidth);
		}
		catch (...)
		{
			// TODO JohnT: some message to the user? Oe go ahead with create, but set up Paint
			// to display some message?
			UninitGraphics();
			return 0; // failure, don't create.
		}
	}
	// We need to do this even if the width has not changed. It may be that the root changed
	// internally affecting its height (e.g., because of editing)
	HRESULT hr;
	IgnoreHr(hr = m_qrootb->get_Height(&m_dypHeight));
	if (FAILED(hr))
	{
		Warn("Root box height failed");
		m_dxpLayoutWidth = -50000; // no drawing until we get successful layout
		return 0;
	}
	m_dypHeight += 3; // specs call for this to include dividing line and border.
	// If we have an active editor, it will get resized in the draw method of the containing
	// window. Is this best? What if the active editor gets drawn
	// before it gets moved? Will we get flicker?

	return m_dypHeight;
}

/*----------------------------------------------------------------------------------------------
	Draw to the given clip rectangle.
	(Note: it is not really a clip rectangle, but the actual position to draw.)
----------------------------------------------------------------------------------------------*/
void AfDeFeVw::Draw(HDC hdc, const Rect & rcpClip)
{
	Assert(hdc);
	if (m_qdvw)
	{
		// The edit window will do its own drawing automatically.
		return;
	}
	try
	{
		// This interacts with GetCoordRects to make drawing happen in the right place.
		m_rcClip = rcpClip;
		// If by any chance the VwGraphics already has a DC, save it for later
		HDC hdcOld = 0;
		if (!m_qvg)
		{
			m_qvg.CreateInstance(CLSID_VwGraphicsWin32);
		}
		else
		{
			CheckHr(m_qvg->GetDeviceContext(&hdcOld));
			// ENHANCE JohnT: This is getting pretty messy. Maybe we should make a new
			// VwGraphics if the old one is in use?
			if (hdcOld)
				m_qvg->ReleaseDC();
		}
		m_qvg->Initialize(hdc); // Set up the graphics object to draw on the right DC
		m_cactInitGraphics++; // so any call to InitGraphics knows there is a DC already
		// OPTIMIZE JohnT(?): the paint struct contains info about the rectangle that needs drawing.
		// Does the DC already contain that info, or do we need to provide it?

		if (m_qrootb.Ptr() && (m_dxpLayoutWidth > 0))
		{
			VwPrepDrawResult xpdr = kxpdrAdjust;
			while (xpdr == kxpdrAdjust)
			{
				RECT rcSrcRoot;
				RECT rcDstRoot;
				GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
				CheckHr(m_qrootb->PrepareToDraw(m_qvg, rcSrcRoot, rcDstRoot,
					&xpdr));
			}
			if (xpdr != kxpdrInvalidate)
			{
				// do drawing; otherwise, whole window invalidated for another draw
				// Dest rect may have changed because of PrepareToDraw.
				RECT rcSrcRoot;
				RECT rcDstRoot;
				GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
				m_qrootb->DrawRoot(m_qvg, rcSrcRoot, rcDstRoot, true);
			}
		}
		m_qvg->ReleaseDC();
		m_cactInitGraphics--;
		if (hdcOld)
			m_qvg->Initialize(hdcOld);
	}
	catch (Throwable & thr)
	{
		WarnHr(thr.Error());
	}
	catch (...)
	{
		WarnHr(E_FAIL);
	}
}

//:>********************************************************************************************
//:>	Edit support overrides
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Check whether the content of this edit field has changed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeVw::IsDirty()
{
	if (!m_fDirty)
	{
		ComBool fDirty;
		CheckHr(m_qrootb->IsDirty(&fDirty));
		if (fDirty)
			m_fDirty = true;
	}
	return m_fDirty;
}


/*----------------------------------------------------------------------------------------------
	Create an editing window, set m_hwnd to the hwnd of the editor, and return true.
	Return false if it failed. hwnd is the parent window, and rc is the rect for the
	window based on parent window coordinates.
----------------------------------------------------------------------------------------------*/
bool AfDeFeVw::BeginEdit(HWND hwnd, Rect & rc, int dxpCursor, bool fTopCursor)
{
	if (!SuperClass::BeginEdit(hwnd, rc, dxpCursor, fTopCursor))
		return false;
	// If the field is at the top, we get called here before we have a rootbox, so make one.
	if (!m_qrootb)
	{
		ILgWritingSystemFactoryPtr qwsf;
		GetLpInfo()->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
		InitGraphics();
		MakeRoot(m_qvg, qwsf, &m_qrootb);
		UninitGraphics();
	}
	// It occupies just the part of the screen where we normally draw the box,
	// except that it has its own 2-pixel width adjustment, so we don't have to do that.
	rc.Inflate(0, -1);
	// rc.bottom -= 1; // another 1 pixel for the dividing line.
	m_qdvw.Attach(CreateEditWnd(hwnd, rc));
	m_qdvw->SetRootBox(m_qrootb);
	m_qdvw->SetLastLayoutWidth(m_dxpLayoutWidth); // can take advantage of what we already did
	m_hwnd = m_qdvw->Hwnd();
	// Set up an initial selection (1) near at start of window, (2) where user can edit,
	// (3) not a range.
	m_qrootb->MakeSimpleSel(true, true, false, true, NULL);

	m_qrootb->SetSite(m_qdvw);

	// Move the insertion point to the specified location (MakeSimpleSel always puts it at
	// the beginning of the field).
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	// MakeSimpleSel above could fail, if there is no obviously valid edit position in the
	// field. In such a case, the subclass should usually overide this method, but we make
	// the check just to be safe.
	if (qvwsel)
	{
		CheckHr(m_qrootb->Activate(vssEnabled));
		CheckHr(qvwsel->SetIPLocation(fTopCursor, dxpCursor));
	}
	m_fDirty = false;
	return true;
}

void AfDeFeVw::MoveWnd(const Rect & rcClip)
{
	// Compared to the rectangle passed, we allow one pixel of white space top and bottom.
	// Space left and right is left both by this and the AfVwWnd, so we don't need to allow
	// for it here.
	::MoveWindow(m_hwnd, rcClip.left, rcClip.top + 1, rcClip.Width(),
		rcClip.Height() - 2, true);
}

/*----------------------------------------------------------------------------------------------
	Save changes that have been made to the current editor.
----------------------------------------------------------------------------------------------*/
bool AfDeFeVw::SaveEdit()
{
	ComBool fOk;
	m_qrootb->LoseFocus(&fOk); //  attempts to save changes
	if (!fOk)
		return false; // root box should already have notified user.

	m_fDirty = false;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Close the current editor, saving changes that were made. hwnd is the editor hwnd.
	@param fForce True if we want to force the editor closed without making any
		validity checks or saving any changes.
----------------------------------------------------------------------------------------------*/
void AfDeFeVw::EndEdit(bool fForce)
{
	SuperClass::EndEdit(fForce);
	if (!fForce)
	{
		SaveFullCursorInfo();
		ComBool fOk;
		m_qrootb->LoseFocus(&fOk); //  attempts to save changes
		if (!fOk)
		{
			Assert(false);
			return; // root box should already have notified user.
		}
	}
	// This hides it, and also helps prevent any new selection from being a range.
	// (The initial click in a DE window is lost, because the 'window' does not exist,
	// so we start out with mouse-down-extended calls. Fortunately the first of these
	// is taken as a regular mouse down if there is no existing selection. ENHANCE JohnT:
	// should we instead try to get a real mouse-down message in the root box? Maybe actully
	// convert MouseDownExtended to MouseDown if there is no selection?)
	m_qrootb->DestroySelection();
	// Get rid of the window.
	::DestroyWindow(m_hwnd);
	m_hwnd = 0;
	m_qdvw = NULL;
	m_fDirty = false;

	m_qrootb->SetSite(this);
	if (fForce)
		return;

	// Although the view code automatically updates these fields, we should still notify all
	// windows in case some other editor (such as a tree node) depends on this field.
	// NOTE:  Cannot pass GetOwner(), GetOwnerFlid() to UpdateAllDEWindows() because
	//		AfDeSplitChild::UpdateField() must be passed the roled participant.
	m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
}

void AfDeFeVw::OnReleasePtr()
{
	if (m_qrootb)
	{
		AfMainWnd * pafw = m_qadsc->MainWindow();
		if (pafw) // May not be, e.g., in control.
		{
			// Unregister the root box with the main window.
			pafw->UnregisterRootBox(m_qrootb);
		}

		m_qrootb->Close();
		m_qrootb.Clear();
	}
	m_qvg.Clear();
	SuperClass::OnReleasePtr();
}


/*----------------------------------------------------------------------------------------------
	The field has changed, so make sure it is updated.
----------------------------------------------------------------------------------------------*/
void AfDeFeVw::UpdateField()
{
	// Nothing needed here since the view code handles this.
}


/*----------------------------------------------------------------------------------------------
	This method saves the current cursor information in RecMainWnd. Normally it just
	stores the cursor index in RecMainWnd::m_ichCur. For structured texts, however,
	it also inserts the appropriate hvos and flids for the StText classes in
	m_vhvoPath and m_vflidPath. Other editors may need to do other things.
----------------------------------------------------------------------------------------------*/
void AfDeFeVw::SaveCursorInfo()
{
	// Store the current record/subrecord and field info.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (!prmw)
		return;
	Vector<int> & vflid = prmw->GetFlidPath();
	Vector<HVO> & vhvo = prmw->GetHvoPath();
	VwSelLevInfo * prgvsli = NULL;
	try
	{
		// Get the selection information.
		IVwSelectionPtr qvwsel;
		if (!m_qrootb)
			return; // Happens when splitting window.
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
			return; // No selection.
		int csli;
		CheckHr(qvwsel->CLevels(false, &csli));
		if (!csli)
			return; // Some strange selection, perhaps a literal string, can't handle as yet.
		prgvsli = NewObj VwSelLevInfo[csli];
		int ihvoRoot;
		PropTag tagTextProp;
		int cpropPrevious;
		int ichAnchor;
		int ichEnd;
		int ws;
		ComBool fAssocPrev;
		int ihvoEnd;
		CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, csli, prgvsli, &tagTextProp, &cpropPrevious,
			&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
		// Get the information about each level.
		// The highest level represents the root object.
		// We don't want to include this fake property and hvo.
		for (int isli = csli - 1; --isli >= 0; )
		{
			HVO hvo;
			int ihvo;
			int cpropPrev;
			int flid;
			IVwPropertyStorePtr qvps;
			CheckHr(qvwsel->PropInfo(false, isli, &hvo, &flid, &ihvo, &cpropPrev, &qvps));
			vhvo.Push(hvo);
			vflid.Push(flid);
		}
		delete[] prgvsli;
		prmw->SetCursorIndex(Min(ichAnchor, ichEnd));
	}
	catch (...)
	{
		if (prgvsli)
			delete[] prgvsli;
	}
}


/*----------------------------------------------------------------------------------------------
	This attempts to place the cursor as defined in RecMainWnd m_vhvoPath, m_vflidPath,
	and m_ichCur.
	@param vhvo Vector of ids inside the field.
	@param vflid Vector of flids inside the field.
	@param ichCur Character offset in the final field for the cursor.
----------------------------------------------------------------------------------------------*/
void AfDeFeVw::RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur)
{
	// Store the current record/subrecord and field info.
	// Move the selection and scroll to show it
	// Store the current record/subrecord and field info.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (!prmw)
		return;
	CustViewDaPtr qcvd = prmw->MainDa();
	AssertPtr(qcvd);

	VwSelLevInfo * prgvsli;

	int csli = Max(1, vflid.Size() - 1);
	prgvsli = NewObj VwSelLevInfo[csli];
	if (vflid.Size())
	{
		AfStatusBarPtr qstbr = prmw->GetStatusBarWnd();
		Assert(qstbr);
		bool fProgBar = qstbr->IsProgressBarActive();
		if (!fProgBar)
		{
			StrApp strMsg(kstidStBar_LoadingData);
			qstbr->StartProgressBar(strMsg.Chars(), 0, 70, 1);
		}

		// We have a list of HVO/flids as a path to a selection. Unfortunately, VwSelLevInfo
		// wants an inverse list of flid/index to HVO, so we have to do some work here.
		// Input: (hvo, flid (index must be calculated))
		// hvo1  flid1  (ihvo1 = index of hvo2 in flid1 prop of hvo1)
		// hvo2  flid2  (ihvo2 = index of hvo3 in flid2 prop of hvo2)
		// hvo3  (missing or flid3)
		// Output: (prgsli index, tag, ihvo)
		// prgsli[0], flid3, 0 (or this line is omitted)
		// prgsli[1], flid2, ihvo2
		// prgsli[2], flid1, ihvo1
		// prgsli[3], kflidRnResearchNbk_Records, ihvoCurr
		int iflid = 0;
		// Build up a selection that attempts to get us to the desired spot in the record.
		// We need to build the SelLevInfo backwards with the root on the bottom.
		int isli = vflid.Size() - 1;
		while (--isli >= 0)
		{
			prgvsli[isli].tag = vflid[iflid];
			prgvsli[isli].cpropPrevious = 0;
			int ihvo = 0;
			if (iflid + 1 < vhvo.Size())
			{
				CheckHr(qcvd->GetObjIndex(vhvo[iflid], vflid[iflid], vhvo[iflid + 1], &ihvo));
				if (ihvo < 0)
				{
					// What we are looking for is not in the cache, so go get it.
					AfMdiClientWnd * pmdic = dynamic_cast<AfMdiClientWnd *>(prmw->GetMdiClientWnd());
					AssertPtr(pmdic);
					int iview;
					AfClientRecDeWnd * pcrde = dynamic_cast<AfClientRecDeWnd *>(m_qadsc->Parent());
					iview = pmdic->GetChildIndexFromWid(pcrde->GetWindowId());
					UserViewSpecVec & vuvs = prmw->GetLpInfo()->GetDbInfo()->GetUserViewSpecs();
					HvoClsidVec vhc;
					HvoClsid hc;
					hc.hvo = vhvo[iflid];
					CheckHr(qcvd->get_ObjClid(hc.hvo, &hc.clsid));
					vhc.Push(hc);

					qcvd->LoadData(vhc, vuvs[iview], qstbr, true);
					CheckHr(qcvd->GetObjIndex(vhvo[iflid], vflid[iflid], vhvo[iflid + 1], &ihvo));
					// If still not found, the selected view must not include what we
					// are looking for, so just give up.
					if (ihvo < 0)
						ihvo = 0;
				}
			}
			prgvsli[isli].ihvo = ihvo;
			++iflid;
		}
		if (!fProgBar)
			qstbr->EndProgressBar();
	}

	if (m_qrootb)
	{
		IVwSelectionPtr qsel;
		if (vflid.Size())
		{
			m_qrootb->MakeTextSelection(
				0, // int ihvoRoot
				csli - 1, // int cvlsi,
				prgvsli, // Skip the first one -- VwSelLevInfo * prgvsli
				vflid[vflid.Size() - 1], // int tagTextProp,
				0, // int cpropPrevious,
				ichCur, // int ichAnchor,
				ichCur, // int ichEnd,
				0, // int ws,
				true, // ComBool fAssocPrev,
				-1, // int ihvoEnd,
				NULL, // ITsTextProps * pttpIns,
				true, // ComBool fInstall,
				&qsel); // IVwSelection ** ppsel
		}
		// If we didn't get a text selection, try getting a selection somewhere close.
		if (!qsel)
		{
			m_qrootb->MakeTextSelInObj(
				0,  // index of the one and only root object in this view
				csli, // the object we want is one level down
				prgvsli, // and here's how to find it there
				0,
				NULL, // don't worry about the endpoint
				true, // select at the start of it
				true, // Find an editable field
				false, // and don't select a range.
				// Making this true, allows the whole record to scroll into view when we launch
				// a new window by clicking on a reference to an entry, but we don't get an insertion
				// point. Using false gives an insertion point, but the top of the record is typically
				// at the bottom of the screen, which isn't good.
				false, // don't select the whole object
				true, // but do install it as the current selection
				NULL); // and don't bother returning it to here. */
		}
	}
	delete[] prgvsli;
}


bool AfDeFeVw::CmdFindInDictionary(Cmd * pcmd)
{
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (prmw)
		return prmw->FindInDictionary(m_qrootb);
	else
		return false;
}

bool AfDeFeVw::CmsFindInDictionary(CmdState & cms)
{
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (prmw)
	{
		return prmw->EnableCmdIfVernacularSelection(m_qrootb, cms);
	}
	else
	{
		cms.Enable(false);
		return true;
	}
}


//:>********************************************************************************************
//:>	AfDeVwWnd methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructors
----------------------------------------------------------------------------------------------*/
AfDeVwWnd::AfDeVwWnd()
{
}

AfDeVwWnd::AfDeVwWnd(AfDeFeVw * pdvw)
{
	AssertPtr(pdvw);
	m_pdfv = pdvw;
}

static DummyFactory g_fact2(_T("SIL.AppCore.AfDeFeVw"));

/*----------------------------------------------------------------------------------------------
	Create a new window for editing the contents.
	@param hwndParent The hwnd for the parent window.
	@param rcBounds The position of the new window relative to the parent window.
	@return A pointer to the new AfDeVwWnd window. The caller obtains one (and initially only)
		reference count to the window.
----------------------------------------------------------------------------------------------*/
AfDeVwWnd * AfDeFeVw::CreateEditWnd(HWND hwndParent, Rect & rcBounds)
{
	AfDeVwWndPtr qdvw;
	qdvw.Attach(NewObj AfDeVwWnd(this));

	// ENHANCE JohnT: could some or all of this be moved into BeginEdit so subclasses
	// don't have to mess with it? Maybe we could pass in wcs instead of rcBounds,
	// and have this method just call InitChild with appropriate parameters.
	WndCreateStruct wcs;
	wcs.InitChild(_T("AfVwWnd"), hwndParent, 0);
	wcs.style |=  WS_VISIBLE;
	wcs.SetRect(rcBounds);

	qdvw->CreateHwnd(wcs);
	return qdvw.Detach(); // Give the caller the initial ref count.
}

/*----------------------------------------------------------------------------------------------
	Adjust the scroll range when some lazy box got expanded. Needs to be done for both panes
	if we have more than one. Forwarded to the field editor.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeVwWnd::AdjustScrollRange(IVwRootBox * prootb, int dxdSize, int dxdPosition,
	int dydSize, int dydPosition, ComBool * pfForcedScroll)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfForcedScroll);

	return m_pdfv->AdjustScrollRange(prootb, dxdSize, dxdPosition, dydSize, dydPosition,
		pfForcedScroll);

	END_COM_METHOD(g_fact2, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Handle KeyDown messages.  This is trickier than you think because of the need to move to an
	adjacent field for certain keystrokes, or to scroll the entire window for certain other
	keystrokes.
----------------------------------------------------------------------------------------------*/
bool AfDeVwWnd::OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
{
	AfDeSplitChild * padsc = NULL;
	// Catch keys to move to next/previous fields.
	if (nChar == VK_TAB)
	{
		padsc = dynamic_cast<AfDeSplitChild *>(AfWnd::GetAfWnd(::GetParent(m_hwnd)));
		AssertPtr(padsc);
		if (::GetKeyState(VK_SHIFT) < 0)
			padsc->OpenPreviousEditor(); // Shift Tab to previous editor.
		else
			padsc->OpenNextEditor(); // Tab to next editor.
		return true;;
	}
	else if (nChar == VK_PRIOR || nChar == VK_NEXT)
	{
		// Scroll the entire data entry window up or down one page.
		padsc = dynamic_cast<AfDeSplitChild *>(AfWnd::GetAfWnd(::GetParent(m_hwnd)));
		AssertPtr(padsc);
		padsc->ScrollKey(nChar, 0);
		return true;;
	}


	if (!SuperClass::OnKeyDown(nChar, nRepCnt, nFlags))
	{
		// An arrow key hit the beginning or end of a field: move to the adjacent field
		// editor.
		padsc = dynamic_cast<AfDeSplitChild *>(AfWnd::GetAfWnd(::GetParent(m_hwnd)));
		AssertPtr(padsc);
		int xdPos;
		CheckHr(WarnHr(m_qrootb->get_XdPos(&xdPos)));
		xdPos &= 0xFFFF;
		switch (nChar)
		{
		case VK_UP:
			padsc->OpenPreviousEditor(xdPos, false);
			break;
		case VK_DOWN:
			padsc->OpenNextEditor(xdPos);
			break;
		case VK_LEFT:
			padsc->OpenPreviousEditor(xdPos, false);
			break;
		case VK_RIGHT:
			padsc->OpenNextEditor(xdPos);
			break;
		default:
			break;
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Remove your root registration. This is a separate (virtual) method because some VwWnds,
	such as this, which share their root box, do not want to unregister it.
----------------------------------------------------------------------------------------------*/
void AfDeVwWnd::RemoveRootRegistration()
{
	// We don't want to unregister our root box, because we did not register it: the containing
	// AfDeFeVw did.
}

/*----------------------------------------------------------------------------------------------
	Notifies the site that the size of the root box changed; scroll ranges and/or
	window size may need to be updated. Defer to the underlying field editor.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDeVwWnd::RootBoxSizeChanged(IVwRootBox * prootb)
{
	BEGIN_COM_METHOD

	return m_pdfv->RootBoxSizeChanged(prootb);

	END_COM_METHOD(g_fact2, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Catch a focus change so we can keep current pane updated.
----------------------------------------------------------------------------------------------*/
void AfDeVwWnd::SwitchFocusHere()
{
	AfVwRootSite::SwitchFocusHere();
	m_pdfv->GetDeWnd()->SwitchFocusHere();
}
