/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfVwWnd.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	A superclass for client windows that consist entirely of a single view.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE

DEFINE_COM_PTR(ITfInputProcessorProfiles);
DEFINE_COM_PTR(IEnumTfLanguageProfiles);

DEFINE_THIS_FILE

	// Caution: Using ON_CID_ALL instead of ON_CID_CHILD will likely cause undesirable
	// effects when multiple windows are open. For example, if you open a second window, then
	// go back to the first window and change the font size using the dropdown menu and the
	// mouse, ON_CID_ALL results in the cursor jumping to the second window instead of
	// returning to the active window.
BEGIN_CMD_MAP(AfVwSplitChild)
	ON_CID_CHILD(kcidEditCut, &AfVwSplitChild::CmdEditCut, &AfVwSplitChild::CmsEditCut)
	ON_CID_CHILD(kcidEditCopy, &AfVwSplitChild::CmdEditCopy, &AfVwSplitChild::CmsEditCopy)
	ON_CID_CHILD(kcidEditPaste, &AfVwSplitChild::CmdEditPaste, &AfVwSplitChild::CmsEditPaste)
	// JohnT: The Delete Key is handled otherwise. The menu command as currently specified does
	// not refer to deleting text but to deleting records, at least in data entry mode.
	// If we put this here, it masks the delete record command, and we can't delete a record
	// unless we get the IP in a non-view field, of which there are fewer and fewer.
	// Review PM: should we have a distinct Delete Record command and CID?
	//ON_CID_CHILD(kcidEditDel, &AfVwSplitChild::CmdEditDel, &AfVwSplitChild::CmsEditDel)
	ON_CID_CHILD(kcidEditSelAll, &AfVwSplitChild::CmdEditSelAll, &AfVwSplitChild::CmsEditSelAll)
	ON_CID_CHILD(kcidInsPic, &AfVwSplitChild::CmdInsertPic, NULL)
	ON_CID_CHILD(kcidFmtFnt, &AfVwSplitChild::CmdFmtFnt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmtWrtgSys, &AfVwSplitChild::CmdFmtWrtSys, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmtPara, &AfVwSplitChild::CmdFmtPara, &AfVwSplitChild::CmsFmtPara)
	ON_CID_CHILD(kcidFmtBulNum, &AfVwSplitChild::CmdFmtBulNum, &AfVwSplitChild::CmsFmtBulNum)
	ON_CID_CHILD(kcidFmtBdr, &AfVwSplitChild::CmdFmtBdr, &AfVwSplitChild::CmsFmtBdr)
	ON_CID_CHILD(kcidFmtStyles, &AfVwSplitChild::CmdFmtStyles, &AfVwSplitChild::CmsFmtStyles)
	ON_CID_CHILD(kcidFmttbStyle, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidApplyNormalStyle, &AfVwSplitChild::CmdApplyNormalStyle, NULL)
	ON_CID_CHILD(kcidFmttbWrtgSys, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFnt, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFntSize, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbBold, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbItal, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbAlignLeft, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbAlignCntr, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbAlignRight, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbLstNum, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbLstBullet, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyBdr, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbUnind, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbInd, &AfVwSplitChild::CmdCharFmt, &AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyBgrndColor, &AfVwSplitChild::CmdCharFmt,
		&AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyFgrndColor, &AfVwSplitChild::CmdCharFmt,
		&AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFocusStyleBox, &AfVwSplitChild::CmdCharFmt,
		&AfVwSplitChild::CmsCharFmt)
	ON_CID_CHILD(kcidFilePrint, &AfVwSplitChild::CmdFilePrint, NULL)
	ON_CID_CHILD(kcidEditSrch, &AfVwSplitChild::CmdEditFind, NULL)
	// The execution of the Quick Search control is handled by intercepting the Enter key:
	ON_CID_CHILD(kcidEditSrchQuick, NULL, &AfVwSplitChild::CmsEditSrchQuick)
	ON_CID_CHILD(kcidEditNextMatch, &AfVwSplitChild::CmdEditNextMatch, NULL)
	ON_CID_CHILD(kcidEditPrevMatch, &AfVwSplitChild::CmdEditNextMatch, NULL)
	ON_CID_CHILD(kcidEditRepl, &AfVwSplitChild::CmdEditRepl, NULL)
	ON_CID_CHILD(kcidViewRefresh, &AfVwSplitChild::CmdViewRefresh, NULL)
END_CMD_MAP_NIL()

BEGIN_CMD_MAP(AfVwScrollWnd)
	ON_CID_CHILD(kcidEditCut, &AfVwScrollWnd::CmdEditCut, &AfVwScrollWnd::CmsEditCut)
	ON_CID_CHILD(kcidEditCopy, &AfVwScrollWnd::CmdEditCopy, &AfVwScrollWnd::CmsEditCopy)
	ON_CID_CHILD(kcidEditPaste, &AfVwScrollWnd::CmdEditPaste, &AfVwScrollWnd::CmsEditPaste)
	//ON_CID_CHILD(kcidEditDel, &AfVwScrollWnd::CmdEditDel, &AfVwScrollWnd::CmsEditDel)
	ON_CID_CHILD(kcidEditSelAll, &AfVwScrollWnd::CmdEditSelAll, &AfVwScrollWnd::CmsEditSelAll)
	ON_CID_CHILD(kcidInsPic, &AfVwScrollWnd::CmdInsertPic, NULL)
	ON_CID_CHILD(kcidFmtFnt, &AfVwScrollWnd::CmdFmtFnt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmtWrtgSys, &AfVwScrollWnd::CmdFmtWrtSys, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmtPara, &AfVwScrollWnd::CmdFmtPara, &AfVwScrollWnd::CmsFmtPara)
	ON_CID_CHILD(kcidFmtBulNum, &AfVwScrollWnd::CmdFmtBulNum, &AfVwScrollWnd::CmsFmtBulNum)
	ON_CID_CHILD(kcidFmtBdr, &AfVwScrollWnd::CmdFmtBdr, &AfVwScrollWnd::CmsFmtBdr)
	ON_CID_CHILD(kcidFmtStyles, &AfVwScrollWnd::CmdFmtStyles, &AfVwScrollWnd::CmsFmtStyles)
	ON_CID_CHILD(kcidFmttbStyle, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidApplyNormalStyle, &AfVwScrollWnd::CmdApplyNormalStyle, NULL)
	ON_CID_CHILD(kcidFmttbWrtgSys, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFnt, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFntSize, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbBold, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbItal, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbAlignLeft, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbAlignCntr, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbAlignRight, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbLstNum, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbLstBullet, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyBdr, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbUnind, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbInd, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyBgrndColor, &AfVwScrollWnd::CmdCharFmt,
		&AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyFgrndColor, &AfVwScrollWnd::CmdCharFmt,
		&AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFocusStyleBox, &AfVwScrollWnd::CmdCharFmt, &AfVwScrollWnd::CmsCharFmt)
	ON_CID_CHILD(kcidEditSrch, &AfVwScrollWnd::CmdEditFind, NULL)
	// The execution of the Quick Search control is handled by intercepting the Enter key:
	ON_CID_CHILD(kcidEditSrchQuick, NULL, &AfVwScrollWnd::CmsEditSrchQuick)
	ON_CID_CHILD(kcidEditNextMatch, &AfVwScrollWnd::CmdEditNextMatch, NULL)
	ON_CID_CHILD(kcidEditPrevMatch, &AfVwScrollWnd::CmdEditNextMatch, NULL)
	ON_CID_CHILD(kcidEditRepl, &AfVwScrollWnd::CmdEditRepl, NULL)
	ON_CID_CHILD(kcidViewRefresh, &AfVwScrollWnd::CmdViewRefresh, NULL)
END_CMD_MAP_NIL()

BEGIN_CMD_MAP(AfVwWnd)
	ON_CID_CHILD(kcidInsPic, &AfVwWnd::CmdInsertPic, NULL)
	ON_CID_CHILD(kcidFmtFnt, &AfVwWnd::CmdFmtFnt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmtWrtgSys, &AfVwWnd::CmdFmtWrtSys, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmtPara, &AfVwWnd::CmdFmtPara, &AfVwWnd::CmsFmtPara)
	ON_CID_CHILD(kcidFmtBulNum, &AfVwWnd::CmdFmtBulNum, &AfVwWnd::CmsFmtBulNum)
	ON_CID_CHILD(kcidFmtBdr, &AfVwWnd::CmdFmtBdr, &AfVwWnd::CmsFmtBdr)
	ON_CID_CHILD(kcidFmtStyles, &AfVwWnd::CmdFmtStyles, &AfVwWnd::CmsFmtStyles)
	ON_CID_CHILD(kcidEditCut, &AfVwWnd::CmdEditCut, &AfVwWnd::CmsEditCut)
	ON_CID_CHILD(kcidEditCopy, &AfVwWnd::CmdEditCopy, &AfVwWnd::CmsEditCopy)
	ON_CID_CHILD(kcidEditPaste, &AfVwWnd::CmdEditPaste, &AfVwWnd::CmsEditPaste)
	//ON_CID_CHILD(kcidEditDel, &AfVwWnd::CmdEditDel, &AfVwWnd::CmsEditDel)
	ON_CID_CHILD(kcidEditSelAll, &AfVwWnd::CmdEditSelAll, &AfVwWnd::CmsEditSelAll)
	ON_CID_CHILD(kcidFmttbStyle, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidApplyNormalStyle, &AfVwWnd::CmdApplyNormalStyle, NULL)
	ON_CID_CHILD(kcidFmttbWrtgSys, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFnt, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFntSize, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbBold, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbItal, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbAlignLeft, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbAlignCntr, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbAlignRight, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbLstNum, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbLstBullet, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyBdr, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbUnind, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbInd, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyBgrndColor, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyFgrndColor, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFocusStyleBox, &AfVwWnd::CmdCharFmt, &AfVwWnd::CmsCharFmt)
	ON_CID_CHILD(kcidEditSrch, &AfVwWnd::CmdEditFind, NULL)
	// The execution of the Quick Search control is handled by intercepting the Enter key:
	ON_CID_CHILD(kcidEditSrchQuick, NULL, &AfVwWnd::CmsEditSrchQuick)
	ON_CID_CHILD(kcidEditNextMatch, &AfVwWnd::CmdEditNextMatch, NULL)
	ON_CID_CHILD(kcidEditPrevMatch, &AfVwWnd::CmdEditNextMatch, NULL)
	ON_CID_CHILD(kcidEditRepl, &AfVwWnd::CmdEditRepl, NULL)
	ON_CID_CHILD(kcidViewRefresh, &AfVwWnd::CmdViewRefresh, NULL)
END_CMD_MAP_NIL()

const int knTimerFlashIP = 57;

Vector<StrUni> AfVwRootSite::s_vstuDrawErrMsgs;
bool AfVwRootSite::s_fBusyPrinting = false;
Vector<AfVwRootSite *> AfVwRootSite::s_vrsDraw;

// Keyman windows messages.
extern uint s_wm_kmselectlang;
extern uint s_wm_kmkbchange;
uint s_wmPendingFixKeyboard = ::RegisterWindowMessage(_T("PendingFixKeyboard"));
uint s_tickFixKeyboard = 0;
bool s_fInNotifyFixKeyboard = false;


// If you define this, pressing F2 dumps a lot of information about time spent in CmsCharFmt,
// the function that enables buttons during idle time. Dump is through OutputDebugStringA.
//#define TRACING_IDLE_TIME
#ifdef TRACING_IDLE_TIME
HashMap<int, int> s_hmcidms; // map from cid to milliseconds
int s_cidle;
#endif

//#define TRACING_KEYMAN

//:>********************************************************************************************
//:>	HoldGraphics methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor. If passed an HDC, makes sure that is the DC of the root site's m_qvg (and
	will restore to previous state in destructor). If hdc is 0 (the default), just makes sure
	the graphics object is initialized (and uninits in destructor, if needed).
----------------------------------------------------------------------------------------------*/
HoldGraphics::HoldGraphics(AfInactiveRootSite * pvrs, HDC hdc)
{
	m_hdc = hdc;
	m_pvrs = pvrs;
	if (!m_hdc)
		m_pvrs->GetGraphics(NULL, &m_qvg, &m_rcSrcRoot, &m_rcDstRoot);
	else
		InitWithOrigHdc();
}

void HoldGraphicsBase::InitWithOrigHdc()
{
	m_hdcOld = 0;
	if (!m_pvrs->m_qvg)
	{
		m_pvrs->m_qvg.CreateInstance(CLSID_VwGraphicsWin32);
	}
	else
	{
		CheckHr(m_pvrs->m_qvg->GetDeviceContext(&m_hdcOld));
		// ENHANCE JohnT: This is getting pretty messy. Maybe we should make a new
		// VwGraphics if the old one is in use?
		if (m_hdcOld)
			m_pvrs->m_qvg->ReleaseDC();
	}
	m_pvrs->m_qvg->Initialize(m_hdc); // Set up the graphics object to draw on the right DC
							// so any call to InitGraphics knows there is a DC already.
	::SetMapMode(m_hdc, MM_TEXT);
	m_pvrs->m_cactInitGraphics++;

	m_qvg = m_pvrs->m_qvg;

	m_pvrs->GetCoordRects(m_qvg, &m_rcSrcRoot, &m_rcDstRoot);
}

HoldGraphics::~HoldGraphics()
{
	if (!m_hdc)
	{
		m_pvrs->ReleaseGraphics(NULL, m_qvg); // Nothing in AppCore currently uses this argument.
	}
	else
	{
		m_pvrs->m_qvg->ReleaseDC();
		m_pvrs->m_cactInitGraphics--;
		if (m_hdcOld)
			m_pvrs->m_qvg->Initialize(m_hdcOld);
	}
}

HoldGraphicsRaw::HoldGraphicsRaw(AfVwRootSite * pvrs, HDC hdc)
{
	m_hdc = hdc;
	m_pvrs = pvrs;
	if (!m_hdc)
	{
		// Note that although we get a ref count, we don't want to release it normally, but
		// by calling ReleaseGraphics. Hence, we don't use a smart pointer.
		m_pvrs->GetGraphicsRaw(&m_qvg, &m_rcSrcRoot, &m_rcDstRoot);
	}
	else
		InitWithOrigHdc();
}

HoldGraphicsRaw::~HoldGraphicsRaw()
{
	if (!m_hdc)
	{
		m_pvrs->ReleaseGraphicsRaw(m_qvg);
	}
	else
	{
		m_pvrs->m_qvg->ReleaseDC();
		m_pvrs->m_cactInitGraphics--;
		if (m_hdcOld)
			m_pvrs->m_qvg->Initialize(m_hdcOld);
	}
}


//:>********************************************************************************************
//:>	AfVwRootSite methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfVwRootSite::AfVwRootSite(AfWnd * pwnd, bool fScrollVert, bool fScrollHoriz)
{
	ModuleEntry::ModuleAddRef();

	m_dxdLayoutWidth = -50000; // Unlikely to be real current window width!
	m_cactInitGraphics = 0;
	m_fVScrollEnabled = fScrollVert;
	m_fHScrollEnabled = fScrollHoriz;
	m_pwndSubclass = pwnd;
	Assert(!m_qrootb);

	m_wsPending = -1;
	m_nCodePage = CP_ACP;

	m_fCanDoRtl = true;			// was false for version 1 of Data Notebook
	m_f1DefaultFont = false;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfVwRootSite::~AfVwRootSite()
{
	if (m_hwndExtLinkTool)
	{
		::DestroyWindow(m_hwndExtLinkTool);
		m_hwndExtLinkTool = NULL;
	}
	if (m_hwndOverlayTool)
	{
		::DestroyWindow(m_hwndOverlayTool);
		m_hwndOverlayTool = NULL;
	}
	ModuleEntry::ModuleRelease();
}

static DummyFactory g_fact1(_T("SIL.AppCore.AfVwRootSite"));
static DummyFactory g_fact2(_T("SIL.AppCore.AfVwSplitChild"));
static DummyFactory g_fact3(_T("SIL.AppCore.AfVwScrollWndBase"));
static DummyFactory g_fact4(_T("SIL.AppCore.AfPrintRootSite"));
static DummyFactory g_fact5(_T("SIL.AppCore.PrintProgressReceiver"));

/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwRootSite)
		*ppv = static_cast<IVwRootSite *>(this);
	else if (&riid == &CLSID_AfVwRootSite)	// trick one to find own impl
		*ppv = static_cast<AfVwRootSite *>(this);
	else
		return E_NOINTERFACE;

	AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Some situations lead to invalidating very large rectangles. Something seems to go wrong
	if they are way bigger than the client rectangle. Finding the intersection makes it more
	reliable.
----------------------------------------------------------------------------------------------*/
void CallInvalidateRect(HWND hwnd, Rect rect, bool fErase)
{
	Rect rcClient;
	::GetClientRect(hwnd, &rcClient);
	rect.Intersect(rcClient);
	::InvalidateRect(hwnd, &rect, fErase);
}

/*----------------------------------------------------------------------------------------------
	Only does anything interesting in subclass that has orientation manager.
----------------------------------------------------------------------------------------------*/
RECT AfVwRootSite::RotateRectDstToPaint(RECT rect)
{
	return rect;
}

RECT AfVwScrollWndBase::RotateRectDstToPaint(RECT rect)
{
	return m_pomgr->RotateRectDstToPaint(rect);
}
/*----------------------------------------------------------------------------------------------
	@param pRoot The sender.
	@param twLeft Relative to top left of root box.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::InvalidateRect(IVwRootBox * pRoot, int xsLeft, int ysTop,
	int xsWidth, int ysHeight)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pRoot);

	// Convert it to our drawing coordinates and do the invalidate.
	HoldGraphics hg(this);
	Rect rect;
	rect.left = hg.m_rcSrcRoot.MapXTo(xsLeft, hg.m_rcDstRoot);
	rect.top = hg.m_rcSrcRoot.MapYTo(ysTop, hg.m_rcDstRoot);
	rect.right = hg.m_rcSrcRoot.MapXTo(xsLeft + xsWidth, hg.m_rcDstRoot);;
	rect.bottom = hg.m_rcSrcRoot.MapYTo(ysTop + ysHeight, hg.m_rcDstRoot);;
	CallInvalidateRect(m_pwndSubclass->Hwnd(), 	RotateRectDstToPaint(rect), true);

	AfVwRootSite * pvsrOther = OtherPane();
	if (pvsrOther)
	{
		// Convert it to the other root site's coordinates and invalidate that also.
		HoldGraphics hg2(pvsrOther);
		Rect rect2;
		rect2.left = hg2.m_rcSrcRoot.MapXTo(xsLeft, hg2.m_rcDstRoot);
		rect2.top = hg2.m_rcSrcRoot.MapYTo(ysTop, hg2.m_rcDstRoot);
		rect2.right = hg2.m_rcSrcRoot.MapXTo(xsLeft + xsWidth, hg2.m_rcDstRoot);;
		rect2.bottom = hg2.m_rcSrcRoot.MapYTo(ysTop + ysHeight, hg2.m_rcDstRoot);;
		CallInvalidateRect(pvsrOther->Window()->Hwnd(), RotateRectDstToPaint(rect2), true);
	}
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Get a graphics object in an appropriate state for drawing and measuring in the view.
	The calling method should pass the IVwGraphics back to ReleaseGraphics() before
	it returns. In particular, problems will arise if OnPaint() gets called before the
	ReleaseGraphics() method.
	REVIEW JohnT(?): We probably need a better way to handle this. Most likely: make the
	VwGraphics object we cache a true COM object so its reference count is meaningful; have this
	method create a new one. Problem: a useable VwGraphics object has a device context that is
	linked to a particular window; if the window closes, the VwGraphics is not useable, whatever
	its reference count says. It may therefore be that we just need to allocate a copy in this
	method, leaving the member variable alone. Or, the current strategy may prove adequate.
----------------------------------------------------------------------------------------------*/
void AfInactiveRootSite::GetGraphicsRaw(IVwGraphics ** ppvg, RECT * prcSrcRoot,
	RECT * prcDstRoot)
{
	AssertPtr(ppvg);
	InitGraphics();
	*ppvg = m_qvg;
	m_qvg.Ptr()->AddRef();
	if (prcSrcRoot)
		GetCoordRects(m_qvg, prcSrcRoot, prcDstRoot);
}

/*----------------------------------------------------------------------------------------------
	Get a graphics object in an appropriate state for drawing and measuring in the view.
	The calling method should pass the IVwGraphics back to ReleaseGraphics() before
	it returns. In particular, problems will arise if OnPaint() gets called before the
	ReleaseGraphics() method.
	REVIEW JohnT(?): We probably need a better way to handle this. Most likely: make the
	VwGraphics object we cache a true COM object so its reference count is meaningful; have this
	method create a new one. Problem: a useable VwGraphics object has a device context that is
	linked to a particular window; if the window closes, the VwGraphics is not useable, whatever
	its reference count says. It may therefore be that we just need to allocate a copy in this
	method, leaving the member variable alone. Or, the current strategy may prove adequate.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::GetGraphics(IVwRootBox * prootb, IVwGraphics ** ppvg,
	RECT * prcSrcRoot, RECT * prcDstRoot)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppvg);
	ChkComArgPtr(prcSrcRoot);
	ChkComArgPtr(prcDstRoot);

	GetGraphicsRaw(ppvg, prcSrcRoot, prcDstRoot);
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}
/*----------------------------------------------------------------------------------------------
	Get a graphics object in an appropriate state for drawing and measuring in the view.
	The calling method should pass the IVwGraphics back to ReleaseGraphics() before
	it returns. In particular, problems will arise if OnPaint() gets called before the
	ReleaseGraphics() method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::get_LayoutGraphics(IVwRootBox * prootb, IVwGraphics ** ppvg)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppvg);

	GetGraphicsRaw(ppvg);
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}
/*----------------------------------------------------------------------------------------------
	Screen version is the same except for print layout views.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::get_ScreenGraphics(IVwRootBox * prootb, IVwGraphics ** ppvg)
{
	return get_LayoutGraphics(prootb, ppvg);
}

/*----------------------------------------------------------------------------------------------
	Screen version is just like the relevant part of GetGraphics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::GetTransformAtDst(IVwRootBox * pRoot,  POINT pt,
	RECT * prcSrcRoot, RECT * prcDstRoot)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(prcSrcRoot);
	ChkComArgPtrN(prcDstRoot);
	GetCoordRects(m_qvg, prcSrcRoot, prcDstRoot);
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}
/*----------------------------------------------------------------------------------------------
	Screen version is just like the relevant part of GetGraphics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::GetTransformAtSrc(IVwRootBox * pRoot,  POINT pt,
	RECT * prcSrcRoot, RECT * prcDstRoot)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(prcSrcRoot);
	ChkComArgPtrN(prcDstRoot);
	GetCoordRects(m_qvg, prcSrcRoot, prcDstRoot);
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

STDMETHODIMP AfVwSplitChild::GetGraphics(IVwRootBox * prootb, IVwGraphics ** ppvg,
	RECT * prcSrcRoot, RECT * prcDstRoot)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppvg);
	ChkComArgPtr(prcSrcRoot);
	ChkComArgPtr(prcDstRoot);

	AfVwRootSite * pvrsOther = OtherPane();
	if (pvrsOther && m_qsplf->CurrentPane() != this)
	{
		// The other pane is the active one in which to draw selections; let it handle
		// the request.
		return pvrsOther->GetGraphics(prootb, ppvg, prcSrcRoot, prcDstRoot);
	}

	InitGraphics();
	*ppvg = m_qvg;
	m_qvg.Ptr()->AddRef();
	GetCoordRects(m_qvg, prcSrcRoot, prcDstRoot);
	return S_OK;

	END_COM_METHOD(g_fact2, IID_IVwRootSite);
}

STDMETHODIMP AfVwSplitChild::get_LayoutGraphics(IVwRootBox * prootb, IVwGraphics ** ppvg)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppvg);

	AfVwRootSite * pvrsOther = OtherPane();
	if (pvrsOther && m_qsplf->CurrentPane() != this)
	{
		// The other pane is the active one in which to draw selections; let it handle
		// the request.
		return pvrsOther->get_LayoutGraphics(prootb, ppvg);
	}

	InitGraphics();
	*ppvg = m_qvg;
	m_qvg.Ptr()->AddRef();
	return S_OK;

	END_COM_METHOD(g_fact2, IID_IVwRootSite);
}
/*----------------------------------------------------------------------------------------------
	Screen version is the same except for print layout views.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwSplitChild::get_ScreenGraphics(IVwRootBox * prootb, IVwGraphics ** ppvg)
{
	return get_LayoutGraphics(prootb, ppvg);
}

/*----------------------------------------------------------------------------------------------
	Screen version is just like the relevant part of GetGraphics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwSplitChild::GetTransformAtDst(IVwRootBox * prootb,  POINT pt,
	RECT * prcSrcRoot, RECT * prcDstRoot)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(prcSrcRoot);
	ChkComArgPtrN(prcDstRoot);
	AfVwRootSite * pvrsOther = OtherPane();
	if (pvrsOther && m_qsplf->CurrentPane() != this)
	{
		// The other pane is the active one in which to draw selections; let it handle
		// the request.
		return pvrsOther->GetTransformAtDst(prootb, pt, prcSrcRoot, prcDstRoot);
	}
	GetCoordRects(m_qvg, prcSrcRoot, prcDstRoot);
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Screen version is just like the relevant part of GetGraphics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwSplitChild::GetTransformAtSrc(IVwRootBox * prootb,  POINT pt,
	RECT * prcSrcRoot, RECT * prcDstRoot)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(prcSrcRoot);
	ChkComArgPtrN(prcDstRoot);
	AfVwRootSite * pvrsOther = OtherPane();
	if (pvrsOther && m_qsplf->CurrentPane() != this)
	{
		// The other pane is the active one in which to draw selections; let it handle
		// the request.
		return pvrsOther->GetTransformAtSrc(prootb, pt, prcSrcRoot, prcDstRoot);
	}
	GetCoordRects(m_qvg, prcSrcRoot, prcDstRoot);
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}
/*----------------------------------------------------------------------------------------------
	Scroll by the specified amount (positive is down, that is, added to the scroll offset).
	If this would exceed the scroll range, move as far as possible. Update both actual display
	and scroll bar position. (Can also scroll up, if dy is negative. Name is just to indicate
	positive direction.)
----------------------------------------------------------------------------------------------*/
void AfVwScrollWndBase::ScrollDown(int dy)
{
	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_ALL, 0, 0, 0, 0 };
	m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);
	int ydNew = sinfo.nPos + dy;
	if (ydNew < 0) ydNew = 0;
	if (ydNew > sinfo.nMax)
		ydNew = sinfo.nMax;
	if (ydNew != sinfo.nPos)
	{
		int dyd = ydNew - sinfo.nPos;
		sinfo.nPos = ydNew;
		sinfo.fMask = SIF_POS;
		SetRootSiteScrollInfo(SB_VERT, &sinfo, true);
		ScrollBy(0, dyd);
	}
}

/*----------------------------------------------------------------------------------------------
	Get your scroll offsets.
----------------------------------------------------------------------------------------------*/
void AfVwScrollWndBase::GetScrollOffsets(int * pdxd, int * pdyd)
{
	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_POS, 0, 0, 0, 0 };
	m_pwndSubclass->GetScrollInfo(SB_HORZ, &sinfo);
	*pdxd = sinfo.nPos;
	m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);
	*pdyd = sinfo.nPos;
}

/*----------------------------------------------------------------------------------------------
	Inform the container when done with the graphics object.
	REVIEW JohnT(?): could we somehow have this handled by the Release method of the
	IVwGraphics?
	But that method does not know anything about the status or source of its hdc.
	This version is not overridden by AfVwSplitChild, and should be used when releasing a
	graphics object that is definitely for this site, not the current one of a split pair.
----------------------------------------------------------------------------------------------*/
void AfInactiveRootSite::ReleaseGraphicsRaw(IVwGraphics * pvg)
{
	Assert(pvg == m_qvg.Ptr());
	UninitGraphics();
}

/*----------------------------------------------------------------------------------------------
	Inform the container when done with the graphics object.
	REVIEW JohnT(?): could we somehow have this handled by the Release method of the
	IVwGraphics?
	But that method does not know anything about the status or source of its hdc.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::ReleaseGraphics(IVwRootBox * prootb, IVwGraphics * pvg)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);

	ReleaseGraphicsRaw(pvg);
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}


STDMETHODIMP AfVwSplitChild::ReleaseGraphics(IVwRootBox * prootb, IVwGraphics * pvg)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);

	AfVwRootSite * pvrsOther = OtherPane();
	// Keep this test exactly synchronized with the one in GetGraphics.
	if (pvrsOther && m_qsplf->CurrentPane() != this)
	{
		// The other pane is the active one in which to draw selections; let it handle
		// the request.
		return pvrsOther->ReleaseGraphics(prootb, pvg);
	}
	Assert(pvg == m_qvg.Ptr());
	UninitGraphics();
	return S_OK;

	END_COM_METHOD(g_fact2, IID_IVwRootSite);
}


/*----------------------------------------------------------------------------------------------
	Get the width available for laying things out in the view.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::GetAvailWidth(IVwRootBox * prootb, int * ptwWidth)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ptwWidth);

	*ptwWidth = LayoutWidth();
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	If there is a pending writing system that should be applied to typing,
	return it; also clear the state so that subsequent typing will not
	have a pending writing system until something sets it again.
	(This is mainly used so that keyboard-change commands can be applied
	while the selection is a range.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::GetAndClearPendingWs(IVwRootBox * prootb, int * pws)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pws);
	*pws = m_wsPending;
	m_wsPending = -1;
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Answer whether boxes in the specified range of destination coordinates
	may usefully be converted to lazy boxes. Should at least answer false
	if any part of the range is visible. The default implementation avoids
	converting stuff within about a screen's height of the visible part(s).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::IsOkToMakeLazy(IVwRootBox * prootb, int ydTop, int ydBottom,
	ComBool * pfOK)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfOK); // sets it false.
	if (OkToConvert(ydTop, ydBottom))
	{
		AfVwRootSite * pvsrOther = OtherPane();
		if (pvsrOther)
		{
			int dxdOffset;
			int dydOffsetThis;
			int dydOffsetOther;
			GetScrollOffsets(&dxdOffset, &dydOffsetThis);
			pvsrOther->GetScrollOffsets(&dxdOffset, &dydOffsetOther);
			int ydTopOther = ydTop + dydOffsetThis - dydOffsetOther;
			int ydBottomOther = ydBottom + dydOffsetThis - dydOffsetOther;
			*pfOK = pvsrOther->OkToConvert(ydTopOther, ydBottomOther);
		}
		else
			*pfOK = true; // no other pane, and this one said OK already.
	}
	// Enhance JohnT: needs to give a useful answer.
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}


/*----------------------------------------------------------------------------------------------
	The public version sets up and clears the graphics object.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::ScrollSelectionIntoView(IVwSelection * psel, VwScrollSelOpts ssoFlag)
{
	BEGIN_COM_METHOD
	ChkComArgPtrN(psel);

	InitGraphics();
	switch (ssoFlag)
	{
	case kssoDefault:
		MakeSelectionVisible1(psel);
		break;
	case kssoNearTop:
		ScrollSelectionNearTop1(psel);
		break;
	default:
		UninitGraphics();
		return E_FAIL;
	}

	UninitGraphics();
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	 The user has attempted to delete something which the system does not
	 inherently know how to delete. The dpt argument indicates the type of
	 problem.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::OnProblemDeletion(IVwSelection * psel, VwDelProbType dpt,
	VwDelProbResponse * pdpr)
{
	BEGIN_COM_METHOD
	return E_NOTIMPL;
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Notifies the site that the size of the root box changed; scroll ranges and/or
	window size may need to be updated. The standard response is to update the scroll range.
	Review JohnT: might this also be the place to make sure the selection is still visible?
	Should we try to preserve the scroll position (at least the top left corner, say) even
	if the selection is not visible? Which should take priority?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::RootBoxSizeChanged(IVwRootBox * prootb)
{
	BEGIN_COM_METHOD

	UpdateScrollRange();
	AfVwRootSite * pvsrOther = OtherPane();
	if (pvsrOther)
		pvsrOther->UpdateScrollRange();

	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Adjust the scroll range when some lazy box got expanded. Needs to be done for both panes
	if we have more than one.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::AdjustScrollRange(IVwRootBox * prootb, int dxdSize, int dxdPosition,
	int dydSize, int dydPosition, ComBool * pfForcedScroll)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfForcedScroll);

	CheckHr(AdjustScrollRange1(dxdSize, dxdPosition, dydSize, dydPosition, pfForcedScroll));
	AfVwRootSite * pvsrOther = OtherPane();
	if (pvsrOther)
	{
		ComBool fForcedScroll;
		CheckHr(pvsrOther->AdjustScrollRange1(dxdSize, dxdPosition, dydSize, dydPosition,
			&fForcedScroll));
		if (fForcedScroll)
			*pfForcedScroll = true;
	}

	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Adjust the scroll range when some lazy box got expanded. Currently this is implemented
	only for scrolling views.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::AdjustScrollRange1(int dxdSize, int dxdPosition,
	int dydSize, int dydPosition, ComBool * pfForcedScroll)
{
	Assert(false); return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Adjust the scroll range when some lazy box got expanded.
	This is rather similar to SizeChanged, but is used when the size changed
	as a result of recomputing something that is invisible (typically about to become
	visible, but not currently on screen). Thus, the scroll bar range and possibly
	position need adjusting, but it isn't necessary to actually redraw anything except
	the scroll bar--unless the scroll position is forced to change, because we were
	in the process of scrolling to somewhere very close to the end, and the expansion
	was smaller than predicted, and the total range is now less than the current
	position.
	@param dzdHeight The change (positive means larger) in the overall size of the
	root box
	@param dzdPos The position where the change happened. In general it may be
	assumed that if this change is above the thumb position, everything that changed
	is above it, and it needs to be increased by dydSize; otherwise, everything is below
	the screen, and no change to the thumb position is needed.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwScrollWndBase::AdjustScrollRange1(int dxdSize, int dxdPosition,
	int dydSize, int dydPosition, ComBool * pfForcedScroll)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfForcedScroll);

	Rect rc;
	// Review 1730: may need to adjust this if we are zooming.
	::GetClientRect(m_pwndSubclass->Hwnd(), &rc);
	int dydWindHeight = rc.Height();
	int dxdWindWidth = rc.Width();

	int dxdOffset;
	int dydOffset;
	GetScrollOffsets(&dxdOffset, &dydOffset);

	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_ALL, 0, 0, 0, 0 };
	m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);
	int dydRangeNew = sinfo.nMax + dydSize;

	// If the current position is after where the change occurred, it needs to
	// be adjusted by the same amount.
	if (sinfo.nPos > dydPosition)
		sinfo.nPos += dydSize;

	// If it is now too big, adjust it. Also, this means we must be in the
	// middle of a draw that is failing, so invalidate and set the return flag.
	if (sinfo.nPos > max(dydRangeNew - dydWindHeight + 1, 0))
	{
		sinfo.nPos = max(dydRangeNew - dydWindHeight + 1, 0);
		Invalidate();
		*pfForcedScroll = true;
	}
	// It is also possible that we've made it too small. This can happen if
	// expanding a lazy box reduces the real scroll range to zero.
	if (sinfo.nPos < 0)
	{
		sinfo.nPos = 0;
		Invalidate();
		*pfForcedScroll = true;
	}
	// Make the actual adjustment. Note we need to reset the page, because
	// Windows does not allow nPage to be more than the scroll range, so if we
	// ever (even temporarily) compute a smaller scroll range, something has to
	// set the page size back to its proper value of the window height.
	sinfo.nMax = dydRangeNew;
	sinfo.nPage = dydWindHeight;
	dydOffset = sinfo.nPos; // May also be used below in fixing horizontal.
	SetRootSiteScrollInfo(SB_VERT, &sinfo, true);

	if (m_fHScrollEnabled)
	{
		// Similarly for horizontal scroll bar.
		// Note: this has probably not been tested.
		sinfo.fMask = SIF_ALL;
		m_pwndSubclass->GetScrollInfo(SB_HORZ, &sinfo);
		sinfo.nMax += dxdSize;
		sinfo.nPage = dxdWindWidth;
		if (sinfo.nPos > dxdPosition)
			sinfo.nPos += dxdSize;
		if (sinfo.nPos > sinfo.nMax - dxdWindWidth + 1)
		{
			sinfo.nPos = max(sinfo.nMax - dxdWindWidth + 1, 0);
			Invalidate();
			*pfForcedScroll = true;
		}
		// Make the actual adjustment
		SetRootSiteScrollInfo(SB_HORZ, &sinfo, true);
	}
	return S_OK;

	END_COM_METHOD(g_fact3, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Cause the display of the root to update.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::DoUpdates(IVwRootBox * prootb)
{
	BEGIN_COM_METHOD

	MSG msg;
	if (::PeekMessage(&msg, m_pwndSubclass->Hwnd() , WM_PAINT, WM_PAINT, PM_REMOVE))
		::DispatchMessage(&msg);
	AfVwRootSite * pvsrOther = OtherPane();
	if (pvsrOther)
	{
		if (::PeekMessage(&msg, pvsrOther->Window()->Hwnd() , WM_PAINT, WM_PAINT, PM_REMOVE))
			::DispatchMessage(&msg);
	}
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	When the selection is changed, it propagates this to its site.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::SelectionChanged(IVwRootBox * prootb, IVwSelection * pvwselNew)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwselNew);

	HandleSelectionChange(pvwselNew);
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	When the state of the overlays changes, it propagates this to its site.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::OverlayChanged(IVwRootBox * prootb, IVwOverlay * pvo)
{
	BEGIN_COM_METHOD
	ChkComArgPtrN(pvo);

	AdjustForOverlays(pvo);
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Return true if this kind of window uses semi-tagging.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::get_SemiTagging(IVwRootBox * prootb, ComBool * pf)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pf);

	*pf = (bool)DoesSemiTagging();
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Return the rootbox embedded in this view site.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::get_RootBox(IVwRootBox ** pprootb)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pprootb);
	*pprootb = m_qrootb;
	AddRefObj(*pprootb);
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Return the HWND for this view site.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::get_Hwnd(DWORD * phwnd)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(phwnd);
	if (m_pwndSubclass)
		*phwnd = (DWORD)m_pwndSubclass->Hwnd();
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Default handling of inserted paragraphs with different properties.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::OnInsertDiffParas(IVwRootBox * prootb, ITsTextProps * pttpDest,
	int cPara, ITsTextProps ** prgpttpSrc, ITsString ** prgptssSrc,  ITsString * ptssTrailing,
	VwInsertDiffParaResponse * pidpr)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pidpr);
	*pidpr = kidprDefault;
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}
/*----------------------------------------------------------------------------------------------
	Converts view output coords to absolute screen coordinates.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::ScreenToClient(IVwRootBox * prootb, POINT * ppnt)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(ppnt);
	::ScreenToClient(Window()->Hwnd(), ppnt);

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Converts absolute screen coordinates to view output coords.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfVwRootSite::ClientToScreen(IVwRootBox * prootb, POINT * ppnt)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(ppnt);
	::ClientToScreen(Window()->Hwnd(), ppnt);

	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Make sure the graphics object has a DC. If it already has, increment a count,
	so we know when to really free the DC.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::InitGraphics()
{
	if (m_cactInitGraphics == 0)
	{
		// We are asking for a VwGraphics but haven't been given a DC. Make one.
		HDC hdc = ::GetDC(m_pwndSubclass->Hwnd());
		if (!m_qvg)
		{
			m_qvg.CreateInstance(CLSID_VwGraphicsWin32);
		}
		m_qvg->Initialize(hdc); // Puts the DC in the right state.
		::SetMapMode(hdc, MM_TEXT);

	}
	m_cactInitGraphics++;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void AfVwRootSite::UninitGraphics()
{
	m_cactInitGraphics--;
	if (m_cactInitGraphics == 0)
	{
		// We have released as often as we init'd. The device context must have been
		// made in InitGraphics. Release it.
		HDC hdc;
		CheckHr(m_qvg->GetDeviceContext(&hdc));
		m_qvg->ReleaseDC();
		int iSuccess;
		iSuccess = ::ReleaseDC(m_pwndSubclass->Hwnd(), hdc);
		Assert(iSuccess);
	}
}


/*----------------------------------------------------------------------------------------------
	Return the layout width for the window, depending on whether or not there is a
	scroll bar. If there is no scroll bar, we pretend that there is, so we don't have
	to keep adjusting the width back and forth based on the toggling on and off of
	vertical and horizontal scroll bars and their interaction.
	The return result is in pixels.
	NOTE: you must save a valid DC into m_vg before calling this routine.
----------------------------------------------------------------------------------------------*/
int AfVwRootSite::LayoutWidth()
{
#if 0
	// Indicates whether we need to adjust width. If the window does not even have the potential
	// for a vertical scroll bar, we don't.
	bool fVScroll = m_fVScrollEnabled;
	if (fVScroll)
	{
		SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_PAGE + SIF_RANGE, 0, 0, 0, 0 };
		m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);

		// JohnT: currently we force the scroll bar to show always, so we don't need
		// this fix.
		if (sinfo.nMax > (int)(sinfo.nPage))
		{
			// The scroll bar is visible, so the regular client rectangle gives the right
			// result.
			fVScroll = false;
			// Otherwise leave it true; we need to correct for its width.
		}
	}
#endif



	// Can't find any doc that says so clearly, but I assume the client rect is measured
	// in pixels.  WHY DON'T THEY SAY SO CLEARLY???!!!
	RECT rcClient;
	::GetClientRect(m_pwndSubclass->Hwnd(), &rcClient);

	// The default -4 allows two pixels right and left to keep data clear of the margins.
	int nWidthPixels = rcClient.right - rcClient.left - (GetHorizMargin() * 2);
#if 0
	// JohnT: we never need this correction at present, because if we enable a
	// scroll bar at all, it is always visible.
	int nVScrollWidthPixels = ::GetSystemMetrics(SM_CXVSCROLL);
	if (fVScroll)
		nWidthPixels -= nVScrollWidthPixels;
#endif

	return nWidthPixels;
}

/*----------------------------------------------------------------------------------------------
	Return the layout width for the window, depending on whether or not there is a
	scroll bar. In a vertical window, this is actually the height.
	The return result is in pixels.
	NOTE: you must save a valid DC into m_vg before calling this routine.
----------------------------------------------------------------------------------------------*/
int AfVwScrollWndBase::LayoutWidth()
{
	return m_pomgr->LayoutWidth() - (GetHorizMargin() * 2);
}

/*----------------------------------------------------------------------------------------------
	Construct cood transformation rectangles. Height and width are dots per inch.
	src origin is 0, dest origin is controlled by scrolling.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::GetCoordRects(IVwGraphics * pvg, RECT * prcSrcRoot, RECT * prcDstRoot)
{
	AssertPtr(pvg);
	AssertPtr(prcSrcRoot);
	AssertPtr(prcDstRoot);

	prcSrcRoot->left = prcSrcRoot->top = 0;
	int dxInch;
	int dyInch;
	pvg->get_XUnitsPerInch(&dxInch);
	pvg->get_YUnitsPerInch(&dyInch);
	prcSrcRoot->right = dxInch;
	prcSrcRoot->bottom = dyInch;
	int dxdScrollOffset, dydScrollOffset;
	GetScrollOffsets(&dxdScrollOffset, &dydScrollOffset);
	prcDstRoot->left = (-dxdScrollOffset) + GetHorizMargin();
	prcDstRoot->top = (-dydScrollOffset);
	prcDstRoot->right = prcDstRoot->left + prcSrcRoot->right;
	prcDstRoot->bottom = prcDstRoot->top + prcSrcRoot->bottom;
}
void AfVwScrollWndBase::GetCoordRects(IVwGraphics * pvg, RECT * prcSrcRoot, RECT * prcDstRoot)
{
	m_pomgr->GetCoordRects(pvg, prcSrcRoot, prcDstRoot);
}

bool AfVwRootSite::OkToConvert(int ydTop, int ydBottom)
{
	Rect rc;
	::GetClientRect(m_pwndSubclass->Hwnd(), &rc);
	// If the box is more than one screen above the top of the window, go ahead.
	if (ydBottom < -rc.Height())
		return true;
	// Likewise if more than one screen below the bottom of the window.
	if (ydTop > rc.Height() + rc.Height())
		return true;
	return false;
}

bool AfVwRootSite::IsVertical()
{
	return false;
}

bool AfVwScrollWndBase::IsVertical()
{
	return m_pomgr->IsVertical();
}

/*----------------------------------------------------------------------------------------------
	Update your scroll range to reflect current conditions.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::UpdateScrollRange()
{
	if ((!m_fVScrollEnabled && !m_fHScrollEnabled) || !m_qrootb)
		return;

	AfVwScrollWndBase * pvswb = dynamic_cast<AfVwScrollWndBase *>(this);
	AssertPtr(pvswb);

	try
	{
		int dysHeight;
		int dxsWidth;
		HRESULT hr;
		IgnoreHr(hr = m_qrootb->get_Height(&dysHeight));
		if (FAILED(hr))
		{
			Warn("Root box height failed");
			m_dxdLayoutWidth = -50000; // No drawing until we get successful layout.
			return;
		}
		IgnoreHr(hr = m_qrootb->get_Width(&dxsWidth));
		if (FAILED(hr))
		{
			Warn("Root box width failed");
			m_dxdLayoutWidth = -50000; // No drawing until we get successful layout.
			return;
		}
		if (IsVertical())
		{
			//swap and add margins to height
			int temp = dysHeight;
			dysHeight = dxsWidth;
			dxsWidth = temp;
		}
		Rect rcSrcRoot;
		Rect rcDstRoot;
		GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);

		int dydHeight = MulDiv(dysHeight, rcDstRoot.Height(), rcSrcRoot.Height());
		int dxdWidth = MulDiv(dxsWidth, rcDstRoot.Width(), rcSrcRoot.Width());

		// to get well clear of the descenders of the last line;
		// about 4 is needed but we add a few more for good measure
		if (IsVertical())
			dxdWidth += 8;
		else
			dydHeight += 8;

		Rect rc;
		// Review 1730: may need to adjust this if we are zooming.
		::GetClientRect(m_pwndSubclass->Hwnd(), &rc);
		int dydWindHeight = rc.Height();
		int dxdWindWidth = rc.Width();

		int dxdOffset;
		int dydOffset;
		GetScrollOffsets(&dxdOffset, &dydOffset);
		int dxdNewXOffset = dxdOffset; // Will be the same unless now out of range
		int dydNewYOffset = dydOffset;
		if (dydOffset > max(dydHeight - dydWindHeight, 0))
			dydNewYOffset = max(dydHeight - dydWindHeight, 0);
		SCROLLINFO sinfo2 = { isizeof(SCROLLINFO), SIF_ALL, 0, 0, 0, 0 };
		m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo2);


		SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_ALL, 0, dydHeight, dydWindHeight,
			dydNewYOffset };
		// Adjust the bounds and redraw.
		SetRootSiteScrollInfo(SB_VERT, &sinfo, true);

		if (m_fHScrollEnabled)
		{
			// Similarly for horizontal scroll bar.
			sinfo.fMask = SIF_ALL;
			sinfo.nMin = 0;
			sinfo.nMax = dxdWidth;
			sinfo.nPage = dxdWindWidth;
			if (dxdOffset > max(dxdWidth - dxdWindWidth, 0))
				dxdNewXOffset = max(dxdWidth - dxdWindWidth, 0);
			sinfo.nPos = dxdNewXOffset;
			// Adjust the bounds and redraw the scroll bar.
			SetRootSiteScrollInfo(SB_HORZ, &sinfo, true);
		}

		// Actually move the window contents if we must.
		pvswb->ScrollBy(dxdNewXOffset - dxdOffset, dydNewYOffset - dydOffset);
	}
	catch (...)
	{
		// Nothing we can usefully do, just don't propagate the error.
	}
}


/*----------------------------------------------------------------------------------------------
	Lay out your root box. If nothing significant has changed since the last layout, answer
	false; if it has, return true. Assumes that m_qvg is in a valid state (having a DC).
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::Layout()
{
	if (!m_qrootb)
	{
		// Make sure we don't think we have a scroll range if we have no data!
		UpdateScrollRange();
		return false; // Nothing to do.
	}

	int dxdAvailWidth = LayoutWidth();
	if (dxdAvailWidth != m_dxdLayoutWidth)
	{
		m_dxdLayoutWidth = dxdAvailWidth;
		// If we have less than 1 point, probably the window has not received its initial
		// OnSize message yet, and we can't do a meaningful layout.
		if (m_dxdLayoutWidth < 2)
		{
			m_dxdLayoutWidth = -50000; // No drawing until we get reasonable size.
			return true;
		}
		HRESULT hr;
		IgnoreHr(hr = m_qrootb->Layout(m_qvg, dxdAvailWidth));
		if (FAILED(hr))
		{
			Warn("Root box layout failed");
			m_dxdLayoutWidth = -50000; // No drawing until we get successful layout.
			return true;
		}
	}

	// Update the scroll range anyway, because height may have changed.xxxxx.
	UpdateScrollRange();

	return true;
}

/*----------------------------------------------------------------------------------------------
	Answer true if this is the active pane in which the selection should be drawn
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::IsCurrentPane()
{
	return true;
}

bool AfVwSplitChild::IsCurrentPane()
{
	return this == m_qsplf->CurrentPane();
}

/*----------------------------------------------------------------------------------------------
	Enable various menu items.
----------------------------------------------------------------------------------------------*/
bool AfVwSplitChild::CmsHaveRecord1(CmdState & cms)
{
	RecMainWnd * prmw = dynamic_cast<RecMainWnd*>(MainWindow());
	Assert(prmw);
	return prmw->CmsHaveRecord(cms);
}

/*----------------------------------------------------------------------------------------------
	Set the focus to the root site, either this one or the other pane.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::SetFocusToRootSite()
{
	::SetFocus(Window()->Hwnd());
}

void AfVwSplitChild::SetFocusToRootSite()
{
	if (IsCurrentPane())
		::SetFocus(Window()->Hwnd());
	else
		OtherPane()->SetFocusToRootSite();
}

/*----------------------------------------------------------------------------------------------
	Draw to the given clip rectangle.
	OPTIMIZE JohnT: pass clip rect to VwGraphics and make use of it.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::Draw(HDC hdc, const Rect & rcpClip)
{
	Assert(hdc);
	if (s_fBusyPrinting)
	{
		// We can't draw while printing (due to re-entrancy issues in UniscribeSegment), so
		// force another draw later.  See DN-841.
		s_vrsDraw.Push(this);
		return;
	}
	try
	{
		if (m_qrootb.Ptr() && (m_dxdLayoutWidth > 0))
		{
			VwPrepDrawResult xpdr = kxpdrAdjust;
			while (xpdr == kxpdrAdjust)
			{
				// Block, to force destruction of HoldGraphics
				HoldGraphics hg(this, hdc);
				CheckHr(m_qrootb->PrepareToDraw(m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot,
					&xpdr));
			}
			// kxpdrInvalidate true means that expanding lazy boxes at the position we planned
			// to draw caused a nasty change in the scroll position, typically because
			// we were near the bottom, and expanding the lazy stuff at the bottom
			// did not yield a screen-ful of information. The entire window has
			// been invalidated, which will cause a new Paint, so do nothing
			// here. Otherwise, we can go ahead and draw.
			if (xpdr != kxpdrInvalidate)
			{
				// Note that we need a distinct HoldGraphics object at this point,
				// because PrepareToDraw may have made changes that alter the transformation
				// rectangles.
				HoldGraphics hg(this, hdc);
				CheckHr(m_qrootb->DrawRoot(m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot,
					IsCurrentPane()));

				HRESULT hr;
				IgnoreHr(hr = m_qrootb->DrawingErrors());
				if (FAILED(hr))
					GiveDrawErrMsg(false);
			}
		}
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

AfInactiveRootSite::AfInactiveRootSite()
{
}

/*----------------------------------------------------------------------------------------------
	This function returns a selection that includes the entire external link at the current
	insertion point. It doesn't actually make the selection active.

	If the return value is false, none of the paramters should be looked at.
	If pfFoundLinkStyle is true, ppvwsel will contain the entire External Link string and
	pbstrFile will contain the filename the external link is pointing to.
	If pfFoundLinkStyle is false, the selection will still be valid, but it couldn't find
	any external link at the current insertion point.
	If ppt is not NULL, it will look for an External Link at that point. Otherwise,
	the current insertion point will be used.
----------------------------------------------------------------------------------------------*/
bool AfInactiveRootSite::GetExternalLinkSel(bool & fFoundLinkStyle, IVwSelection ** ppvwsel,
	StrAppBuf & strbFile, POINT * ppt)
{
	AssertPtr(ppvwsel);
	AssertPtrN(ppt);

	fFoundLinkStyle = false;
	*ppvwsel = NULL;
	strbFile.Clear();

	if (!m_qrootb)
		return false;
	IVwSelectionPtr qvwsel;
	if (ppt)
	{
		InitGraphics();
		RECT rcSrcRoot;
		RECT rcDstRoot;
		GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
		UninitGraphics();
		m_qrootb->MakeSelAt(ppt->x, ppt->y, rcSrcRoot, rcDstRoot, false, &qvwsel);
		if (!qvwsel)
			ThrowHr(WarnHr(E_FAIL));
	}
	else
	{
		CheckHr(m_qrootb->get_Selection(&qvwsel));
	}
	if (!qvwsel)
		return false;

	ITsStringPtr qtss;
	int ichAnchor;
	int ichEnd;
	ComBool fAssocPrev;
	HVO hvoObj;
	PropTag tag;
	int ws;
	int irun;
	int crun;
	int irunMin;
	int irunLim;
	TsRunInfo tri;
	ITsTextPropsPtr qttp;
	SmartBstr sbstr;
	SmartBstr sbstrMain;
	HVO hvoObjEnd;
	CheckHr(qvwsel->TextSelInfo(false, &qtss, &ichAnchor, &fAssocPrev, &hvoObj, &tag, &ws));
	if (!qtss)
		return false; // No string to check.
	CheckHr(qvwsel->TextSelInfo(true, &qtss, &ichEnd, &fAssocPrev, &hvoObjEnd, &tag, &ws));
	if (!qtss)
		return false; // No string to check.

	if (hvoObj != hvoObjEnd)
	{
		// At this point we do not support external links across paragraphs. If we did, the
		// following code would need to be enhanced considerably to handle any number of
		// strings.
		return false;
	}

	// The following test is covering a bug in TextSelInfo until JohnT fixes it.  If you
	// right+click to the right of a tags field (with one tag), TextSelInfo returns a qtss with
	// a null string and returns ichAnchor = length of the entire string.  As a result get_RunAt
	// fails below because it is asking for a run at a non-existent location.
	int cch;
	CheckHr(qtss->get_Length(&cch));
	if (ichAnchor > cch || ichEnd > cch)
		return false; // No string to check.

	CheckHr(qtss->get_RunCount(&crun));
	CheckHr(qtss->get_RunAt(ichAnchor, &irun));
	for (irunMin = irun; irunMin >= 0; irunMin--)
	{
		CheckHr(qtss->FetchRunInfo(irunMin, &tri, &qttp));
		CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstr));
		if (sbstr.Length() == 0 || *sbstr.Chars() != kodtExternalPathName)
			break;
		if (!fFoundLinkStyle)
		{
			fFoundLinkStyle = true;
			sbstrMain = sbstr;
		}
		else if (!sbstr.Equals(sbstrMain))
		{
			// This External Link is different from the other one, so
			// we've found the beginning.
			break;
		}
	}
	irunMin++;
	// If fFoundLinkStyle is true, irunMin now points to the first run
	// that has the External Link style.
	// If fFoundLinkStyle is false, there's no point in looking at
	// following runs.
	if (!fFoundLinkStyle)
		return true;

	for (irunLim = irun + 1; irunLim < crun; irunLim++)
	{
		CheckHr(qtss->FetchRunInfo(irunLim, &tri, &qttp));
		CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstr));
		if (sbstr.Length() == 0 || *sbstr.Chars() != kodtExternalPathName)
			break;
		if (!sbstr.Equals(sbstrMain))
		{
			// This External Link is different from the other one, so
			// we've found the ending.
			break;
		}
	}

	// We can now calculate the character range of this TsString that has
	// the External Link style applied to it.
	int ichMin;
	int ichLim;
	CheckHr(qtss->get_MinOfRun(irunMin, &ichMin));
	CheckHr(qtss->get_LimOfRun(irunLim - 1, &ichLim));

	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
	VwSelLevInfo * prgvsli;
	prgvsli = NewObj VwSelLevInfo[cvsli];
	try
	{
		int ihvoRoot;
		PropTag tagTextProp;
		int cpropPrevious;
		int ichAnchor;
		int ichEnd;
		int ws;
		ComBool fAssocPrev;
		int ihvoEnd;
		ITsTextPropsPtr qttp;
		CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, cvsli, prgvsli, &tagTextProp, &cpropPrevious,
			&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, &qttp));
		// This does not actually make the selection active.
		CheckHr(m_qrootb->MakeTextSelection(ihvoRoot, cvsli, prgvsli, tagTextProp,
			cpropPrevious, ichMin, ichLim, ws, fAssocPrev, ihvoEnd, qttp, false,
			ppvwsel));
		// And clean up.
		if (prgvsli)
			delete prgvsli;
	}
	catch (...)
	{
		// Properly delete the array of VwSelLevInfo.
		if (prgvsli)
			delete prgvsli;
		throw;
	}
	strbFile = sbstrMain.Chars() + 1;
	return true;
}

/*----------------------------------------------------------------------------------------------
	This function returns a selection that includes the entire overlay tag at the current
	insertion point. It doesn't actually make the selection active.

	If the return value is false, none of the paramters should be looked at.
	If the return value is true, ppvwsel will contain the entire Overlay Tag selection.
	If ppt is not NULL, it will look for an Overlay Tag at that point. Otherwise,
	the current insertion point will be used.

	TODO: This function shares a lot of code with GetExternalLinkSel. It would be nice if they
	could somehow be combined into one function.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::GetOverlayTagSel(OLECHAR * prgchGuid, IVwSelection ** ppvwsel, POINT * ppt)
{
	AssertPtr(ppvwsel);
	AssertPtrN(ppt);

	*ppvwsel = NULL;

	if (!m_qrootb)
		return false;
	IVwSelectionPtr qvwsel;
	if (ppt)
	{
		InitGraphics();
		RECT rcSrcRoot;
		RECT rcDstRoot;
		GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
		UninitGraphics();
		m_qrootb->MakeSelAt(ppt->x, ppt->y, rcSrcRoot, rcDstRoot, false, &qvwsel);
		if (!qvwsel)
			ThrowHr(WarnHr(E_FAIL));
	}
	else
	{
		CheckHr(m_qrootb->get_Selection(&qvwsel));
	}
	if (!qvwsel)
		return false;

	ITsStringPtr qtss;
	int ichAnchor;
	int ichEnd;
	ComBool fAssocPrev;
	HVO hvoObj;
	PropTag tag;
	int ws;
	int irun;
	int crun;
	int irunMin;
	int irunLim;
	TsRunInfo tri;
	ITsTextPropsPtr qttp;
	SmartBstr sbstr;
	SmartBstr sbstrMain;
	CheckHr(qvwsel->TextSelInfo(false, &qtss, &ichAnchor, &fAssocPrev, &hvoObj, &tag, &ws));
	if (!qtss)
		return false; // No string to check.
	CheckHr(qvwsel->TextSelInfo(true, &qtss, &ichEnd, &fAssocPrev, &hvoObj, &tag, &ws));
	if (!qtss)
		return false; // No string to check.
	// The following test is covering a bug in TextSelInfo until JohnT fixes it. If you
	// right+click to the right of a tags field (with one tag), TextSelInfo returns a qtss with
	// a null string and returns ichAnchor = length of the entire string. As a result get_RunAt
	// fails below because it is asking for a run at a non-existent location.
	int cch;
	CheckHr(qtss->get_Length(&cch));
	if (ichAnchor >= cch)
	{
		if (ichEnd >= cch)
			return false; // No string to check.
	}
	CheckHr(qtss->get_RunCount(&crun));

	// Check to see if the character associated with the selection is the one preceding the
	// insertion point. If so, call get_RunAt with that character instead of the one after
	// the insertion point.
	if (fAssocPrev)
		CheckHr(qtss->get_RunAt(ichAnchor - 1, &irun));
	else
		CheckHr(qtss->get_RunAt(ichAnchor, &irun));

	// We use the lower left corner of the allTags rectangle when clicking above the line.
	// I (DarrellZ) tried using the top right corner of the rectangle when clicking in a
	// closing tag, but for some reason it seems to be selecting a character or two to
	// the right of the tag. This makes me think that something's off by a pixel or two,
	// but I couldn't find anything obvious. So then I modified this method to first look
	// to the right to find a run that contains the tag string. Which means I pass the
	// top left corner of the rectangle to the method. As far as I can tell, everything
	// is working the way it should.
	// Move to the run that contains the specified tag.
	for (; irun < crun; irun++)
	{
		CheckHr(qtss->FetchRunInfo(irun, &tri, &qttp));
		CheckHr(qttp->GetStrPropValue(ktptTags, &sbstr));
		if (sbstr.Length() > 0)
		{
			int cguid = sbstr.Length() / kcchGuidRepLength;
			OLECHAR * prgchGuidSel = (OLECHAR *)sbstr.Chars();
			int iguid;
			for (iguid = 0; iguid < cguid; iguid++)
			{
				if (memcmp(prgchGuidSel, prgchGuid, kcchGuidRepLength * isizeof(OLECHAR)) == 0)
					break;
				prgchGuidSel += kcchGuidRepLength;
			}
			if (iguid != cguid)
			{
				// This run has the overlay applied to it, so
				// we can stop looking.
				break;
			}
		}
	}

	// Find the first run that contains the specified tag.
	for (irunMin = irun; irunMin >= 0; irunMin--)
	{
		CheckHr(qtss->FetchRunInfo(irunMin, &tri, &qttp));
		CheckHr(qttp->GetStrPropValue(ktptTags, &sbstr));
		if (sbstr.Length() == 0)
			break;
		int cguid = sbstr.Length() / kcchGuidRepLength;
		OLECHAR * prgchGuidSel = (OLECHAR *)sbstr.Chars();
		int iguid;
		for (iguid = 0; iguid < cguid; iguid++)
		{
			if (memcmp(prgchGuidSel, prgchGuid, kcchGuidRepLength * isizeof(OLECHAR)) == 0)
				break;
			prgchGuidSel += kcchGuidRepLength;
		}
		if (iguid == cguid)
		{
			// This run does not have the overlay applied to it, so
			// we've found the beginning.
			break;
		}
	}
	irunMin++;
	if (irunMin >= crun)
	{
		// This shouldn't ever happen, but if we run off the end of the string looking
		// for the tag string, return without doing anything else.
		return false;
	}

	// Find the last run that contains the specified tag.
	for (irunLim = irun + 1; irunLim < crun; irunLim++)
	{
		CheckHr(qtss->FetchRunInfo(irunLim, &tri, &qttp));
		CheckHr(qttp->GetStrPropValue(ktptTags, &sbstr));
		if (sbstr.Length() == 0)
			break;
		int cguid = sbstr.Length() / kcchGuidRepLength;
		OLECHAR * prgchGuidSel = (OLECHAR *)sbstr.Chars();
		int iguid;
		for (iguid = 0; iguid < cguid; iguid++)
		{
			if (memcmp(prgchGuidSel, prgchGuid, kcchGuidRepLength * isizeof(OLECHAR)) == 0)
				break;
			prgchGuidSel += kcchGuidRepLength;
		}
		if (iguid == cguid)
		{
			// This run does not have the overlay applied to it, so
			// we've found the ending.
			break;
		}
	}

	// We can now calculate the character range of this TsString that has
	// the Overlay Tag applied to it.
	int ichMin;
	int ichLim;
	CheckHr(qtss->get_MinOfRun(irunMin, &ichMin));
	CheckHr(qtss->get_LimOfRun(irunLim - 1, &ichLim));

	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
	VwSelLevInfo * prgvsli;
	prgvsli = NewObj VwSelLevInfo[cvsli];
	try
	{
		int ihvoRoot;
		PropTag tagTextProp;
		int cpropPrevious;
		int ichAnchor;
		int ichEnd;
		int ws;
		ComBool fAssocPrev;
		int ihvoEnd;
		ITsTextPropsPtr qttp;
		CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, cvsli, prgvsli, &tagTextProp, &cpropPrevious,
			&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, &qttp));
		// This does not actually make the selection active.
		CheckHr(m_qrootb->MakeTextSelection(ihvoRoot, cvsli, prgvsli, tagTextProp,
			cpropPrevious, ichMin, ichLim, ws, fAssocPrev, ihvoEnd, qttp, false,
			ppvwsel));
		// And clean up.
		if (prgvsli)
			delete prgvsli;
	}
	catch (...)
	{
		// Properly delete the array of VwSelLevInfo.
		if (prgvsli)
			delete prgvsli;
		throw;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	An error has occurred during drawing, and the component in which it occurred should have
	recorded a system error information object describing the problem

	TODO 1734 (JohnT): put strings in a resource.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::GiveDrawErrMsg(bool fFatal)
{
	IErrorInfoPtr qerrinfo;
	HRESULT hr = GetErrorInfo(0, &qerrinfo);
	if (FAILED(hr) || !qerrinfo) // eg, for message boxes qerrinfo == NULL
		// No error object after all.
		return;

	SmartBstr sbstr;
	hr = qerrinfo->GetDescription(&sbstr);
	if (FAILED(hr))
		return;
	StrUni stuDescr;
	stuDescr.Assign(sbstr.Chars());
	StrApp strDescr(stuDescr);

	if (!fFatal)
	{
		// Look through the list to see if we've already given this message before.
		for (int i = 0; i < s_vstuDrawErrMsgs.Size(); i++)
		{
			if (s_vstuDrawErrMsgs[i] == stuDescr)
				return;
		}
	}

	s_vstuDrawErrMsgs.Push(stuDescr);

	StrApp stuTmp(kstidDrawError);
	StrApp strMessage;
	strMessage.Format(stuTmp, strDescr.Chars());

	StrApp strAppName(kstidAppName);

#ifdef DEBUG
	// We probably want to change all occurrences of GetCurMainWnd() to MainWindow()
	// in this file, but we didn't take the time to verify this would always work. These DEBUG
	// asserts in this file are a temporary test to see if there is ever any difference between
	// the two. If one is hit, it's likely that GetCurMainWnd is returning the wrong value. If
	// this doesn't cause any problem after some time, we can go ahead and switch them all
	// to MainWindow.
	AfMainWnd * pafw1 = AfApp::Papp()->GetCurMainWnd();
	AfMainWnd * pafw2 = m_pwndSubclass->MainWindow();
	Assert(pafw1 == pafw2);
#endif
	HWND hwnd = AfApp::Papp()->GetCurMainWnd()->Hwnd();
	::MessageBox(hwnd, strMessage.Chars(), strAppName.Chars(),
		MB_OK | MB_ICONEXCLAMATION);
}


/*----------------------------------------------------------------------------------------------
	Collect whatever keyboard input is available--whatever the user has typed ahead. Includes
	backspaces and delete forwards, but not any more special keys like arrow keys.
	Arguments:
		chsFirst -- the first character the user typed, which started the whole process.
		prgchsBuffer, cchmax -- buffer in which to put data characters.
		pcchBackSpace -- number of backspaces the user typed, in addition to any that just
			cancel stuff from the input buffer.
		pcchDelForward -- any del forward keys the user typed.
	Review JohnT: probably we should not accumulate both typed characters and sequences
		of extra Dels and Bs's, as it leads to ambiguity in character properties.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::CollectTypedInput(wchar chwFirst, wchar * prgchwBuffer, int cchMax,
	int * pcchBackspace, int * pcchDelForward)
{
	wchar * pchwCur = prgchwBuffer;
	int cchBackspace = 0;
	int cchDelForward = 0;
	// The first character goes into the buffer, unless it is a backspace or delete,
	// in which case it affects the counts.
	switch (chwFirst)
	{
	case kscBackspace:
		cchBackspace++;
		break;
	case kscDelForward:
		cchDelForward++;
		break;
	default:
		*pchwCur++ = chwFirst;
	}

	MSG msg;

	while (pchwCur - prgchwBuffer < cchMax)
	{
		if (::PeekMessage(&msg, m_pwndSubclass->Hwnd(), WM_KEYDOWN, WM_KEYDOWN, PM_REMOVE))
		{
			::TranslateMessage(&msg);
		}
		else if (::PeekMessage(&msg, m_pwndSubclass->Hwnd(), WM_CHAR, WM_CHAR, PM_REMOVE))
		{
			wchar chw = (wchar)msg.wParam;
			switch (chw)
			{
			case kscBackspace:
				if (pchwCur > prgchwBuffer)
					pchwCur--;
				else
					cchBackspace++;
				break;
			case kscDelForward:
				cchDelForward++;
				break;
			default:
				*pchwCur++ = chw;
			}
		}
		else
			break;
	}

	*pchwCur = 0;
	*pcchBackspace = cchBackspace;
	*pcchDelForward = cchDelForward;
}


/*----------------------------------------------------------------------------------------------
	Adjust a point to view coords from device coords. This is the translation from a point
	obtained from a windows message like WM_LBUTTONDOWN to a point that can be passed to the
	root box. Currently it does nothing, as any conversion is handled by the source and
	destination rectangles passed to the mouse routines. It is retained for possible future use.
----------------------------------------------------------------------------------------------*/
void AfInactiveRootSite::PixelToView(int & xp, int & yp)
{
}

void AfVwScrollWndBase::PixelToView(int & xp, int & yp)
{
	POINT pt;
	pt.x = xp;
	pt.y = yp;
	POINT transform = m_pomgr->RotatePointPaintToDst(pt);
	xp = transform.x;
	yp = transform.y;
}

/*----------------------------------------------------------------------------------------------
	The window is first being created.
----------------------------------------------------------------------------------------------*/
int AfVwRootSite::OnCreate(CREATESTRUCT * pcs)
{
	AssertPtr(pcs);

	if (!EnsureRootBox())
		return -1; // Failure, don't create.

	// Start a timer used to flash the insertion point if any.
	// The ID is just an arbitrary non-zero integer; we flash every half second
	// (500 ms as arg 2); last arg says to notify using windows message (OnTimer).
	::SetTimer(m_pwndSubclass->Hwnd(), knTimerFlashIP, 500, 0);

	return 0; // Continue creation of window.
}

/*----------------------------------------------------------------------------------------------
	Make sure the root box exists.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::EnsureRootBox()
{
	bool fRetVal = true;
	try
	{
		if (!m_qrootb)
		{
			ILgWritingSystemFactoryPtr qwsf;
			GetLgWritingSystemFactory(&qwsf);
			InitGraphics();
			MakeRoot(m_qvg, qwsf, &m_qrootb);
			m_dxdLayoutWidth = -50000; // Don't try to draw until we get OnSize and do layout.
			UninitGraphics();
		}
	}
	catch (...)
	{
		// TODO JohnT: some message to the user?
		// Or go ahead with create, but set up Paint to display some message?
		UninitGraphics();
		fRetVal = false;
	}

	return fRetVal;
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
#if 99-99
	// Trace WM_IME_* messages to the debug output window.
	if (wm >= WM_IME_STARTCOMPOSITION && wm <= WM_IME_KEYUP)
	{
		const char * pszMsg = NULL;
		const char * pszCmd = NULL;
		StrAnsi staParam;
		switch (wm)
		{
		case WM_IME_STARTCOMPOSITION:	// 0x010D
			pszMsg = "WM_IME_STARTCOMPOSITION";
			break;
		case WM_IME_ENDCOMPOSITION:		// 0x010E
			pszMsg = "WM_IME_ENDCOMPOSITION";
			break;
		case WM_IME_COMPOSITION:		// 0x010F == WM_IME_KEYLAST
			pszMsg = "WM_IME_COMPOSITION/KEYLAST";
			break;
		case WM_IME_SETCONTEXT:			// 0x0281
			pszMsg = "WM_IME_SETCONTEXT";
			if (lp == 0xC000000F)
				staParam = "ISC_SHOWUIALL";
			else if (lp == 0x0000000F)
				staParam = "ISC_SHOWUIALLCANDIDATEWINDOW";
			else
			{
				if (lp & 0x00000001)
					staParam = "ISC_SHOWUICANDIDATEWINDOW";
				if (lp & 0x80000000)
				{
					if (staParam.Length() == 0)
						staParam = "ISC_SHOWUICOMPOSITIONWINDOW";
					else
						staParam.Append("|ISC_SHOWUICOMPOSITIONWINDOW");
				}
				if (lp & 0x40000000)
				{
					if (staParam.Length() == 0)
						staParam = "ISC_SHOWUIGUIDELINE";
					else
						staParam.Append("|ISC_SHOWUIGUIDELINE");
				}
			}
			break;
		case WM_IME_NOTIFY:				// 0x0282
			pszMsg = "WM_IME_NOTIFY";
			switch (wp)
			{
			case 0x0001:	pszCmd = "IMN_CLOSESTATUSWINDOW";		break;
			case 0x0002:	pszCmd = "IMN_OPENSTATUSWINDOW";		break;
			case 0x0003:	pszCmd = "IMN_CHANGECANDIDATE";			break;
			case 0x0004:	pszCmd = "IMN_CLOSECANDIDATE";			break;
			case 0x0005:	pszCmd = "IMN_OPENCANDIDATE";			break;
			case 0x0006:	pszCmd = "IMN_SETCONVERSIONMODE";		break;
			case 0x0007:	pszCmd = "IMN_SETSENTENCEMODE";			break;
			case 0x0008:	pszCmd = "IMN_SETOPENSTATUS";			break;
			case 0x0009:	pszCmd = "IMN_SETCANDIDATEPOS";			break;
			case 0x000A:	pszCmd = "IMN_SETCOMPOSITIONFONT";		break;
			case 0x000B:	pszCmd = "IMN_SETCOMPOSITIONWINDOW";	break;
			case 0x000C:	pszCmd = "IMN_SETSTATUSWINDOWPOS";		break;
			case 0x000D:	pszCmd = "IMN_GUIDELINE";				break;
			case 0x000E:	pszCmd = "IMN_PRIVATE";					break;
			}
			break;
		case WM_IME_CONTROL:			// 0x0283
			pszMsg = "WM_IME_CONTROL";
			break;
		case WM_IME_COMPOSITIONFULL:	// 0x0284
			pszMsg = "WM_IME_COMPOSITIONFULL";
			break;
		case WM_IME_SELECT:				// 0x0285
			pszMsg = "WM_IME_SELECT";
			break;
		case WM_IME_CHAR:				// 0x0286
			pszMsg = "WM_IME_CHAR";
			break;
		case WM_IME_REQUEST:			// 0x0288
			pszMsg = "WM_IME_REQUEST";
			switch (wp)
			{
			case 0x0001:	pszCmd = "IMR_COMPOSITIONWINDOW";		break;
			case 0x0002:	pszCmd = "IMR_CANDIDATEWINDOW";			break;
			case 0x0003:	pszCmd = "IMR_COMPOSITIONFONT";			break;
			case 0x0004:	pszCmd = "IMR_RECONVERTSTRING";			break;
			case 0x0005:	pszCmd = "IMR_CONFIRMRECONVERTSTRING";	break;
			case 0x0006:	pszCmd = "IMR_QUERYCHARPOSITION";		break;
			case 0x0007:	pszCmd = "IMR_DOCUMENTFEED";			break;
			}
			break;
		case WM_IME_KEYDOWN:			// 0x0290
			pszMsg = "WM_IME_KEYDOWN";
			break;
		case WM_IME_KEYUP:				// 0x0291
			pszMsg = "WM_IME_KEYUP";
			break;
		}
		if (pszMsg != NULL)
		{
			StrAnsi sta;
			sta.Format("AfVwRootSite::FWndProc(%s, wparam = ", pszMsg);
			if (pszCmd == NULL)
				sta.FormatAppend("%d", wp);
			else
				sta.Append(pszCmd);
			if (staParam.Length() == 0)
				sta.FormatAppend(", lparam = %d)\n", lp);
			else
				sta.FormatAppend(", lparam = %s)\n", staParam.Chars());
			::OutputDebugStringA(sta.Chars());
		}
	}
	// End of tracing WM_IME_* messages to the debug output window.
#endif
	static OLECHAR chHighSurrogate = 0; // Temporary store for first of surrogate pair.
	switch (wm)
	{
	case WM_CREATE:
		return OnCreate((CREATESTRUCT *)lp);

	case WM_CHAR:
//		OutputDebugString("WM_CHAR\n");
		Assert(wp < 0x10000);
		if (wp >= 0xd800 && wp < 0xdc00)
		{
			chHighSurrogate = LOWORD(wp); // Store a high surrogate; discard any already stored.
			return true;
		}
		if (wp >= 0xdc00 && wp < 0xe000)
		{
			if (chHighSurrogate)
			{
				// Low surrogate found with high surrogate stored; convert to UTF32...
				UINT nchar = ((chHighSurrogate - 0xD800) << 10) + wp + 0x2400;
				chHighSurrogate = 0;	// ...clear the store...
				OnUniChar(nchar, LOWORD(lp), HIWORD(lp)); // ...and call OnUniChar.
				lnRet = 1;
			}
			return true;	// Discard a low surrogate if there was no stored high surrogate.
		}
		// Not a surrogate.
		OnChar(wp, LOWORD(lp), HIWORD(lp));
		return true;

	case WM_UNICHAR:
//		OutputDebugString("WM_UNICHAR\n");
		OnUniChar(wp, LOWORD(lp), HIWORD(lp));
		lnRet = 1; // true
		return true;

	case WM_DESTROY:
		{
			AssertPtr(m_pwndSubclass);
			AfMainWnd * pafw = m_pwndSubclass->MainWindow();
			// This can happen for activeX controls (e.g., in web pages)
			if (!pafw)
				return false;
			//AssertPtr(pafw);
			if (pafw->GetActiveRootBox() == m_qrootb)
				pafw->SetActiveRootBox(NULL);
		}
		return false;

	case WM_LBUTTONDOWN:
		{
			//StrAppBuf strb;
			//strb.Format("WM_LBUTTONDOWN:%d\n", m_pwndSubclass->Hwnd());
			//OutputDebugString(strb.Chars());
			return OnLButtonDown(wp, (int)(short)LOWORD(lp), (int)(short)HIWORD(lp));
		}

	case WM_RBUTTONDOWN:
		return OnRButtonDown(wp, (int)(short)LOWORD(lp), (int)(short)HIWORD(lp));

	case WM_LBUTTONDBLCLK:
		return OnLButtonDblClk(wp, (int)(short)LOWORD(lp), (int)(short)HIWORD(lp));

	case WM_LBUTTONUP:
		return OnLButtonUp(wp, (int)(short)LOWORD(lp), (int)(short)HIWORD(lp));

	case WM_CONTEXTMENU:
		return OnContextMenu((HWND)wp, MakePoint(lp));

	case WM_MOUSEMOVE:
		{
			//StrAppBuf strb;
			//strb.Format("WM_MOUSEMOVE grfmk:%d, xp:%d, yp:%d\n", wp, (int)(short)LOWORD(lp),
			//	(int)(short)HIWORD(lp));
			//OutputDebugString(strb.Chars());
			return OnMouseMove(wp, (int)(short)LOWORD(lp), (int)(short)HIWORD(lp));
		}

	case WM_ERASEBKGND:
		// Since we use double buffering, we don't want to erase the window; it causes
		// flicker. Instead, we erase the background of the buffer before we start drawing.
		return true;

	case WM_SETFOCUS:
		{
#if 99-99
			StrAppBuf strb;
			strb.Format(_T("AfVwRootSite::FWndProc(WM_SETFOCUS, %x, %x, ...); hwnd = %x\n"),
				wp, lp, m_pwndSubclass->Hwnd());
			::OutputDebugString(strb.Chars());
#endif
			return OnSetFocus((HWND)wp);
		}

	case WM_SYSCHAR:
		OnSysChar(wp, LOWORD(lp), HIWORD(lp));
		break;

	case WM_KEYDOWN:
		{
			// To get accelerators (Cut, Copy, Paste, etc.) to work in dialogs, we have to trap
			// key down messages and ask the menu manager if it is an accelerator. If it is,
			// we have to remove the WM_CHAR message that is already in the queue and return
			// without any further processing.
			MSG msg;
			msg.hwnd = m_pwndSubclass->Hwnd();
			msg.message = wm;
			msg.wParam = wp;
			msg.lParam = lp;

			// Do this only if running an application...in an ActiveX view, hopefully it
			// gets handled some other way...Review JohnT: is it?
			if (AfApp::Papp() && !AfApp::GetMenuMgr()->FTransAccel(&msg))
			{
				MSG msg;
				::PeekMessage(&msg, m_pwndSubclass->Hwnd(), WM_CHAR, WM_CHAR, PM_REMOVE);
				return true;
			}
		}
#ifdef TRACING_IDLE_TIME
if (wp == VK_F2)
{
	StrAnsi sta;
	sta.Format("CmsCharFmt called %d times\n", s_cidle);
	OutputDebugStringA(sta.Chars());

	int cidle = s_cidle / max(s_hmcidms.Size(), 1);
	sta.Format("Idle task called %d times\n", cidle);
	OutputDebugStringA(sta.Chars());

	HashMap<int, int>::iterator it = s_hmcidms.Begin();
	HashMap<int, int>::iterator itLim = s_hmcidms.End();
	int msTotal = 0;
	for ( ; it != itLim; ++it)
	{
		int cid = it.GetKey();
		int ms = it.GetValue();
		msTotal += ms;

		sta.Format("    Command %d took %d ms (%d ms per call)\n", cid, ms, ms/max(cidle, 1));
		OutputDebugStringA(sta.Chars());
	}
	sta.Format("  Total of %d ms (%d ms per call)\n", msTotal, msTotal/max(cidle, 1));
	OutputDebugStringA(sta.Chars());
	s_cidle = 0;
	s_hmcidms.Clear();
	break;
}
#endif

		// The Microsoft world apparently doesn't know that <DEL> is an ASCII character just as
		// much as <BS>, so TranslateMessage generates a WM_CHAR message for <BS>, but not for
		// <DEL>!  Is there a better way to overcome this braindeadness?
		if (wp == VK_DELETE)
		{
			OnChar(kscDelForward, LOWORD(lp), HIWORD(lp));
		}
		else
		{
			OnKeyDown(wp, LOWORD(lp), HIWORD(lp));
		}
		break;

	case WM_KILLFOCUS:
		{
#if 99-99
			StrAppBuf strb;
			strb.Format(_T("AfVwRootSite::FWndProc(WM_KILLFOCUS, %x, %x, ...); hwnd = %x\n"),
				wp, lp, m_pwndSubclass->Hwnd());
			::OutputDebugString(strb.Chars());
#endif
			return OnKillFocus((HWND)wp);
		}

	case WM_TIMER:
		OnTimer(wp);
		break;

	case WM_INPUTLANGCHANGEREQUEST:
		//	A request has been made to change the system keyboard.
		// As far as I (JT) can tell, in defiance of all documentation, we never receive
		// this message, no matter how the input language gets changed. So no point having it.
		//return OnInputLangChangeRequest(wp, lp);
		break;

	case WM_INPUTLANGCHANGE:
		//	The system keyboard has changed.
		OnInputLangChange(wp, lp);
		break;

	default:
		if (wm == s_wm_kmselectlang)
		{
#ifdef TRACING_KEYMAN
			StrAnsi sta;
			sta.Format("Got wm_kmselectlang with wp = %d, lp = %d\n", wp, lp);
			OutputDebugStringA(sta.Chars());
#endif
			if (wp == 4)
			{
				OnKeymanKeyboardChange(wp, lp);
			}
			else if (wp == 1)
			{
				// We get these both as a result of our own changes, and changes
				// resulting from control keys. If we just initiated a change ourselves,
				// ignore it.
				if (m_fSelectLangPending)
					m_fSelectLangPending = false;
				else
					OnKeymanKeyboardChange(wp, lp);
			}
		}
		else if (wm == s_wmPendingFixKeyboard)
		{
			StrApp strb(kstidInvalKybd);
			StrApp strbCap(kstidInvalKybdCaption);
			s_fInNotifyFixKeyboard = true;
			::MessageBox(NULL, strb.Chars(), strbCap.Chars(), MB_OK | MB_ICONERROR);
			s_fInNotifyFixKeyboard = false;
			s_tickFixKeyboard = ::GetTickCount();
				StrApp staMsg;
				staMsg.Format(L"Returned from MessageBox at %d\n", s_tickFixKeyboard);
				::OutputDebugStr(staMsg.Chars());
			return true;
		}

		break; // do nothing
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	// Find your parent MainWindow (but NOT if the chain of parents goes through a dialog...
	// especially for the Find/Replace dialog, it is crucial NOT to change the ActiveRootBox
	// while editing in the dialog's controls, as the information is used to determine where
	// to search. Also for an ActiveX control (e.g., in a Word document) it is crucial to
	// stop the loop when we find a window that isn't one of ours at all.
----------------------------------------------------------------------------------------------*/
AfMainWnd * MainWindowForSetActive(HWND hwnd)
{
	AfMainWnd * pafw = NULL;
	AfWnd * pwnd = NULL;
	// Loop until we find a window that (a) isn't one of ours...e.g., a Word window if
	// we're an ActiveX control; (b) is a frame window, the one we want; or (c) is a dialog,
	// which means we don't want any higher one.
	while (hwnd && (pwnd = AfWnd::GetAfWnd(hwnd)) != NULL
		&& (pafw = dynamic_cast<AfMainWnd *>(pwnd)) == NULL
		&& dynamic_cast<AfDialog *>(AfWnd::GetAfWnd(hwnd)) == NULL)
	{
		hwnd = ::GetParent(hwnd);
	}
	return pafw; // could be null still.
}

/*----------------------------------------------------------------------------------------------
	Handle getting focus: put yourself in the queue to handle command messages.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::OnSetFocus(HWND hwndOld, bool fTbControl)
{
#ifdef Tracing
	StrAnsi sta;
	sta.Format("AfVwRootSite::OnSetFocus(%x from %x)%n",
		this->Window()->Hwnd(), hwndOld);
	::OutputDebugStringA(sta.Chars());
#endif
	// BEFORE we start switching keyboard etc, because we DO want it to take effect on this
	// window.
	m_fIAmFocussed = true;
	AssertPtr(m_qrootb);
	AssertPtr(m_pwndSubclass);
	AfMainWnd * pafw = MainWindowForSetActive(m_pwndSubclass->Hwnd());
	// Not guaranteed for controls, and prevented within dialogs: AssertPtr(pwnd);
	if (pafw)
	{
		// If the last view window is our own pair, we need to hide its selection, because we
		// can only reliably draw it in one pane at a time (and that is what Word does, anyway).
		AfVwRootSite * pvrs = OtherPane();
		if (pvrs)
		{
			AfVwRootSitePtr qvwnd;
			if (pafw->GetActiveViewWindow(&qvwnd) && pvrs == qvwnd)
				m_qrootb->Activate(vssDisabled); // happens in the OTHER pane, it is still
												// active.
		}

		// Let the main frame window know that this is the most recent view window/rootbox.
		pafw->SetActiveRootBox(m_qrootb, fTbControl);
	}
	else
	{
		// Even if we can't be the active root box, we need to be an active command handler
		// so we can receive things like copy and paste commands.
		if (AfApp::Papp())
			AfApp::Papp()->AddCmdHandler(Window(), 1, kgrfcmmAll);
	}

	m_qrootb->Activate(vssEnabled); // Enable selection display (now in THIS pane).

	// A tricky problem is that the old hwnd may be the Keyman tray icon, in which the
	// user has just selected a keyboard. If we SetKeyboardForSelection in that case,
	// we change the current keyboard before we find out what the user selected.
	// In most other cases, we'd like to make sure the right keyboard is selected.
	// If the user switches back to this application from some other without changing the
	// focus in this application, we should already have the right keyboard, since the OS
	// maintains a keyboard selection for each application. If he clicks in a view to
	// switch back, he will make a selection, which results in a SetKeyboardForSelection call.
	// If he clicks in a non-view, eventually the focus will move back to the view, and
	// the previous window will belong to our process, so the following code will fire.
	// So, unless there's a case I (JohnT) haven't thought of, the following should set
	// the keyboard at least as often as we need to, and not in the one crucial case where
	// we must not.
	DWORD procIdThis, procIdOld, threadIdThis, threadIdOld;
	threadIdOld = ::GetWindowThreadProcessId(hwndOld, &procIdOld);
	threadIdThis = ::GetWindowThreadProcessId(this->Window()->Hwnd(), &procIdThis);
	if (procIdThis == procIdOld)
	{
#ifdef TRACING_KEYMAN
		StrAnsi sta;
		sta.Format("Setting keyboard from OnSetFocus proc(%d, %d) thread(%d, %d) hwnd(%d, %d)%n",
			procIdOld, procIdThis, threadIdOld, threadIdThis, hwndOld, this->Window()->Hwnd());
		OutputDebugStringA(sta.Chars());
#endif
		IVwSelectionPtr qsel;
		m_qrootb->get_Selection(&qsel);
		SetKeyboardForSelection(qsel);
	}

	return true;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void AfVwRootSite::Activate(VwSelectionState vss)
{
	m_qrootb->Activate(vss);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void AfVwRootSite::SwitchFocusHere()
{
	::SetFocus(Window()->Hwnd());
}

void AfVwSplitChild::SwitchFocusHere()
{
	AfVwRootSite::SwitchFocusHere();
	if (m_qsplf->CurrentPane() != this)
	{
		// If the last view window is our own pair, we need to hide its selection, because we
		// can only reliably draw it in one pane at a time (and that is what Word does, anyway).
		// Doing so in OnSetFocus will be too late, because by then, we will typically have made
		// a new selection (why this routine was called), and the old one will get hidden in the
		// wrong pane.
		AfVwRootSite * pvrs = OtherPane();
		if (pvrs)
		{
			pvrs->Activate(vssDisabled); // happens in the OTHER pane, it is still active.
			m_qsplf->SetCurrentPane(this);
			m_qrootb->Activate(vssEnabled); // and start drawing in this pane.
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Handle loss of focus. Remove yourself from the message handling queue. Switch to a keyboard
	suitable for non-view widgets (use the UI language).
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::OnKillFocus(HWND hwndNew)
{
#ifdef Tracing
	StrAnsi sta;
	sta.Format("AfVwRootSite::OnKillFocus(%x from %x)%n",
		this->Window()->Hwnd(), hwndNew);
	::OutputDebugStringA(sta.Chars());
#endif
	// NOTE: Do not call RemoveCmdHandler or SetActiveRootBox(NULL) here. There are many
	// cases where the view window loses focus, but we still want to keep track of the last
	// view window. If it is necessary to forget about the view window, do it somewhere else.
	// If nothing else sets the last view window to NULL, it will be cleared when the view
	// window gets destroyed.

	// BEFORE we start switching keyboard etc, because we DO NOT want it to affect this window.
	m_fIAmFocussed = false;

	ComBool fOk;
	CheckHr(m_qrootb->LoseFocus(&fOk));

	// If the selection is an IP, deactivate the root box. Otherwise, it happens only when
	// some other root box is activated, or the main window is deactivated.
	IVwSelectionPtr qsel;
	CheckHr(m_qrootb->get_Selection(&qsel));
	if (qsel)
	{
		ComBool fRange;
		CheckHr(qsel->get_IsRange(&fRange));
		if (!fRange)
			m_qrootb->Activate(vssDisabled); // Disable IP display.
	}
	// The next window may be one of our dialogs that isn't using view code. If it isn't using
	// view code, it shouldn't contain vernacular data, so the UI language is a good guess
	// for the right keyboard, and we certainly don't want to leave some vernacular keyboard
	// set.
	SetKeyboardForUI();
	// This window is losing control of the keyboard, so make sure when we get the focus again
	// we reset it to what we want.
	m_hklCurr = 0;
	m_stuActiveKeymanKbd.Clear();
	// Find our parent MainWindow (but NOT if the chain of parents goes through a dialog...
	// especially for the Find/Replace dialog, it is crucial NOT to change the ActiveRootBox
	// while editing in the dialog's controls, as the information is used to determine where
	// to search.
	AfMainWnd * pafw = MainWindowForSetActive(m_pwndSubclass->Hwnd());

	// See if we are the registered active root box of our containing frame window. If not,
	// remove this as an active command handler.
	AfVwRootSitePtr qvrs;
	if (pafw)
		pafw->GetActiveViewWindow(&qvrs);
	if (qvrs != this && AfApp::Papp())
		AfApp::Papp()->RemoveCmdHandler(Window(), 1);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle a WM_CHAR message.
	@param nChar Character code of the key pressed.
	@param nRepCnt The auto-repeat count for this key.
	@param nFlags Scan code, extended key flag, etc. described in MSDE under platform SDK.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::OnChar(UINT nChar, UINT nRepCnt, UINT nFlags)
{
#if 99-99
	StrAnsi sta;
	sta.Format("AfVwRootSite::OnChar(%x ('%c'), %u, %x)%n",
		nChar, nChar, nRepCnt, nFlags);
	::OutputDebugStringA(sta.Chars());
#endif

	if (m_qrootb)
	{
		wchar rgchwUnicode[100];
		int cchBackspace;
		int cchDelForward;
		// Test whether the Ctrl and/or Shift keys are also being pressed.
		SHORT nShift;
		SHORT nControl;
		nShift = ::GetKeyState(VK_SHIFT);
		nControl = ::GetKeyState(VK_CONTROL);
		VwShiftStatus ss = kfssNone;
		if (nShift < 0)
		{
			if (nControl < 0)
				ss = kgrfssShiftControl;
			else
				ss = kfssShift;
		}
		else if (nControl < 0)
		{
			ss = kfssControl;
		}
		CollectTypedInput((wchar)nChar, rgchwUnicode, 99, &cchBackspace, &cchDelForward);
//		char rgchsBuffer[100];
//		::MultiByteToWideChar(m_nCodePage, 0, rgchsBuffer, 99, rgchwUnicode, 99);
		StrUni stuInput(rgchwUnicode);

		OnCharAux(nChar, nFlags, stuInput, cchBackspace, cchDelForward, ss);
	}
}

void AfVwRootSite::OnCharAux(UINT nChar, UINT nFlags, StrUni stuInput,
	int cchBackspace, int cchDelForward, VwShiftStatus ss)
{
	if (nChar == kscDelForward && !cchBackspace && cchDelForward == 1 && !stuInput.Length())
	{
		// This may be a Ctrl-Backspace or Ctrl-Delete instead of a plain Delete.
		if (ss == kfssControl)
		{
			if (nFlags == 0x0e)
			{
				// We actually have a Ctrl-Backspace that's been converted earlier to look
				// like a Delete.
				nChar = kscBackspace;
				cchBackspace = -1;		// Signal delete back one word.
				cchDelForward = 0;
			}
			else
			{
				// We actually have a Ctrl-Delete, not a plain Delete.
				cchDelForward = -1;	// Signal delete forward one word.
			}
		}
		else if (ss != kfssNone)
		{
			// REVIEW JohnT(SteveMc):Ignore Shift-Delete, Ctrl-Shift-Delete, etc.
			// What do they mean, anyway?
			// No, don't return here; we can get a combination of shift and backspace
			// from KeyMan.
///			return;
		}
	}
	else if (nChar == kscBackspace && cchBackspace == 1 && !cchDelForward &&
		!stuInput.Length())
	{
		if (ss == kfssControl)
		{
			// I don't think we can get here, but just in case...
			cchBackspace = -1;
		}
		else if (ss != kfssNone)
		{
			// I don't know if we can get here, but just in case...
			// REVIEW JohnT(SteveMc): Ignore Shift-Backspace, Ctrl-Shift-Backspace, etc.
			// What do they mean, anyway?
			// No, don't return here; we can get a combination of shift and backspace
			// from KeyMan.
///			return;
		}
	}
	InitGraphics();
	RECT rcSrcRoot;
	RECT rcDstRoot;
	GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);

	ISilDataAccessPtr qsda;
	BeginUndoTask(((cchDelForward > 0 && cchBackspace <= 0 && stuInput.Length() == 0) ?
			kcidDeleteKey :	kcidTyping),
		&qsda);
	// We will make a selection near the top left of the other pane if there is one,
	// and try to keep its position fixed.
	SelPositionInfo spi(OtherPane());

	// Make the OnTyping event a virtual method for testing purposes.
	CallOnTyping(m_qvg, stuInput.Bstr(),
		cchBackspace, cchDelForward, (wchar)nChar, rcSrcRoot, rcDstRoot);

	spi.Restore();

	EndUndoTask(qsda);

	// REVIEW JohnT(?): this and similar methods return an HRESULT; what can we reasonably
	// do with it?
	MakeSelectionVisible1();
	UninitGraphics();
}

#ifdef BASELINE
void AfVwRootSite::CallOnTyping(VwGraphicsPtr qvg, BSTR bstr, int cchBackspace,
	int cchDelForward, OLECHAR chFirst, RECT rcSrcRoot, RECT rcDstRoot)
{
	m_qrootb->OnTyping(m_qvg, bstr.Bstr(), cchBackspace, cchDelForward, chFirst,
		&m_wsPending);
}
#else
void AfVwRootSite::CallOnTyping(IVwGraphicsWin32Ptr qvg, BSTR bstr, int cchBackspace,
	int cchDelForward, OLECHAR chFirst, RECT rcSrcRoot, RECT rcDstRoot)
{
	// The user has pressed Ctrl-Space - do not generate a character.
	if (::GetKeyState(VK_CONTROL) < 0 && !wcscmp(bstr, L" "))
	{
		IVwSelectionPtr qvwsel;
		TtpVec vqttp;
		VwPropsVec vqvps;

		if (!SelectionInOneField())
			return;
		if (!GetCharacterProps(&qvwsel, vqttp, vqvps))
			return;

		RemoveCharFormatting(qvwsel, vqttp);
	}
	else
	{
		CheckHr(m_qrootb->OnTyping(m_qvg, bstr, cchBackspace, cchDelForward, chFirst,
			&m_wsPending));
	}
}
#endif

/*----------------------------------------------------------------------------------------------
	Remove character formatting, as when the user types ctrl-space, or chooses
	a named style. Assumes an Undo action is active if wanted. Clears all formatting
	controlled by the Format/Font dialog, and sets the specified named style, or clears that
	too if it is null or empty. (Pass null to choose "default paragraph style".)
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::RemoveCharFormatting(IVwSelection * pvwsel, TtpVec & vqttp, BSTR bstrStyle)
{
	int cttp = vqttp.Size();
	ComBool fRange;
	CheckHr(pvwsel->get_IsRange(&fRange));
	for (int ittp = 0; ittp < cttp; ittp++)
	{
		if (fRange)
		{
			// We don't want to clear most object data, because it has the effect of making
			// ORCs unuseable. A special case is external links, which are applied to regular
			// characters, and annoying not to be able to remove.
			SmartBstr sbstrObjData;
			CheckHr(vqttp[ittp]->GetStrPropValue(ktptObjData, &sbstrObjData));
			if (sbstrObjData.Length() && sbstrObjData.Chars()[0] != kodtExternalPathName)
			{
				continue;
			}
		}
		// Create an empty builder.
		ITsPropsBldrPtr qtpb;

		CheckHr(vqttp[ittp]->GetBldr(&qtpb));
		CheckHr(qtpb->SetStrPropValue(ktptFontFamily, NULL));
		CheckHr(qtpb->SetStrPropValue(kspNamedStyle, bstrStyle));
		CheckHr(qtpb->SetStrPropValue(ktptObjData, NULL));
		CheckHr(qtpb->SetIntPropValues(ktptItalic, -1, -1));
		CheckHr(qtpb->SetIntPropValues(ktptBold, -1, -1));
		CheckHr(qtpb->SetIntPropValues(ktptSuperscript, -1, -1));
		CheckHr(qtpb->SetIntPropValues(ktptUnderline, -1, -1));
		CheckHr(qtpb->SetIntPropValues(ktptFontSize, -1, -1));
		CheckHr(qtpb->SetIntPropValues(ktptOffset, -1, -1));
		CheckHr(qtpb->SetIntPropValues(ktptForeColor, -1, -1));
		CheckHr(qtpb->SetIntPropValues(ktptBackColor, -1, -1));
		CheckHr(qtpb->SetIntPropValues(ktptUnderColor, -1, -1));
		CheckHr(qtpb->SetStrPropValue(ktptFontVariations, NULL));

		// Update the selection.
		ITsTextPropsPtr qttp;
		CheckHr(qtpb->GetTextProps(&qttp));
		vqttp[ittp] = qttp;
	}

	// Assume some change was made.
	CheckHr(pvwsel->SetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin()));

	m_wsPending = -1;
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_UNICHAR message.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::OnUniChar(UINT nChar, UINT nReserved, UINT nFlags)
{
	if (nChar == UNICODE_NOCHAR)
	{
		return;
	}

	StrUni stuInput;
	if ((nChar & 0xFFFF0000) != 0)
	{	//Character is a surrogate pair.
		wchar chwSurrogate[2];
		if (!ToSurrogate(nChar, &chwSurrogate[0], &chwSurrogate[1]))
			return;
		stuInput.Assign(chwSurrogate, 2);
		OnCharAux(nChar, nFlags, stuInput, 0, 0, kfssNone);
	}
	else if (m_qrootb)
	{
		// Test whether the Ctrl and/or Shift keys are also being pressed.
		SHORT nShift;
		SHORT nControl;
		nShift = ::GetKeyState(VK_SHIFT);
		nControl = ::GetKeyState(VK_CONTROL);
		VwShiftStatus ss = kfssNone;
		if (nShift < 0)
		{
			if (nControl < 0)
				ss = kgrfssShiftControl;
			else
				ss = kfssShift;
		}
		else if (nControl < 0)
		{
			ss = kfssControl;
		}

		//	OPTIMIZE JohnT(?): do we need to process a sequence of characters at once?
		//	For now, just handle one character at a time.
		// --or maybe, we need some other optimization, such as just redisplaying the
		// current line. Marc Durdin has indicated that KeyMan may have problems with
		// the look-ahead approach we use for WM_CHAR, I think because it expects us to have
		// updated the context information after each character.

		wchar chw = LOWORD(nChar);
		int cchBackspace = 0;
		int cchDelForward = 0;
		if (chw == kscBackspace)
			cchBackspace++;
		else if (chw == kscDelForward)
			cchDelForward++;
		else
			stuInput.Assign(&chw, 1);

		OnCharAux(nChar, nFlags, stuInput, cchBackspace, cchDelForward, ss);
	}
}

/*----------------------------------------------------------------------------------------------
	Process left button down (WM_LBUTTONDOWN). xp/yp are current coordinates. grfmk identifies
	button/keys pressed.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::OnLButtonDown(uint grfmk, int xp, int yp)
{
	//StrAppBuf strb;
	//strb.Format("Into AfVwRootSite::OnLButtonDown:%d\n", m_pwndSubclass->Hwnd());
	//OutputDebugString(strb.Chars());

	AssertPtr(Window());
	::SetCapture(Window()->Hwnd());

	// Convert to box coords and pass to root box (if any).
	SwitchFocusHere();
	if (m_qrootb)
	{
		InitGraphics();

		PixelToView(xp, yp);

		RECT rcSrcRoot;
		RECT rcDstRoot;
		GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
		// Note we need to UninitGraphics before processing CallMouseDown since that call
		// may jump to another record which invalidates this graphics object.
		UninitGraphics();

		// If we clicked on an overlay tag, highlight all the text with that tag applied.
		ComBool fInOverlayTag;
		int iGuid;
		SmartBstr sbstrGuids;
		RECT rcTag;
		RECT rcAllTags;
		ComBool fOpeningTag;
		CheckHr(m_qrootb->get_IsClickInOverlayTag(xp, yp, rcSrcRoot, rcDstRoot, &iGuid,
			&sbstrGuids, &rcTag, &rcAllTags, &fOpeningTag, &fInOverlayTag));
		if (fInOverlayTag && iGuid != -1)
		{
			OLECHAR * prgchGuid = (OLECHAR *)sbstrGuids.Chars() + (kcchGuidRepLength * iGuid);
			IVwSelectionPtr qvwsel;
			Point pt;
			if (fOpeningTag)
				pt = Point(rcAllTags.left, rcAllTags.bottom);
			else
				pt = Point(rcAllTags.left, rcAllTags.top);
			if (GetOverlayTagSel(prgchGuid, &qvwsel, &pt) && qvwsel)
			{
				qvwsel->Install();
				return true;
			}
		}

		if (grfmk & MK_SHIFT)
		{
//			m_qrootb->MouseDownExtended(xp, yp, rcSrcRoot, rcDstRoot);
			CallMouseDownExtended(xp, yp, rcSrcRoot, rcDstRoot);
		}
		else
		{
//			m_qrootb->MouseDown(xp, yp, rcSrcRoot, rcDstRoot);
			//OutputDebugString("Into AfVwRootSite::CallMouseDown\n");
			CallMouseDown(xp, yp, rcSrcRoot, rcDstRoot);
			//OutputDebugString("Out of AfVwRootSite::CallMouseDown\n");
		}
	}
	//OutputDebugString("Out of AfVwRootSite::OnLButtonDown\n");
	return true;
}

/*----------------------------------------------------------------------------------------------
	A request has been made to change the system keyboard. Return true if it is NOT okay to
	change it. Ideally we would do this if we don't have any writing system which corresponds
	to that input language.
	(Actually, experiment indicates that this message is never received, so trying to handle
	it is useless. The method is currently not called at all.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::OnInputLangChangeRequest(UINT wpFlags, UINT lpHKL)
{
	//if (!m_qrootb)
	//	return false; // For paranoia.

	////	Get the language identifier.
	//int nLangId = (int)LOWORD(lpHKL);

	//ISilDataAccessPtr qsdaT;
	//CheckHr(m_qrootb->get_DataAccess(&qsdaT));
	//ILgWritingSystemFactoryPtr qwsf;
	//CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));

	//int cws;
	//CheckHr(qwsf->get_NumberOfWs(&cws));
	//Vector<int> vws;
	//vws.Resize(cws);
	//CheckHr(qwsf->GetWritingSystems(vws.Begin(), vws.Size()));
	//for (int iws = 0; iws < cws; iws++)
	//{
	//	IWritingSystemPtr qws;
	//	CheckHr(qwsf->get_EngineOrNull(vws[iws], &qws));
	//	if (!qws)
	//		continue;
	//	int nLocale;
	//	CheckHr(qws->get_Locale(&nLocale));
	//	int nLangIdWs = LANGIDFROMLCID(nLocale);

	//	if (nLangIdWs && nLangIdWs == nLangId)
	//		return false; // OK, there's a matching ws.
	//}
	//HKL hklDefault = reinterpret_cast<HKL>(LANGIDFROMLCID(AfApp::GetDefaultKeyboard()));
	//HKL hklSet = reinterpret_cast<HKL>(nLangId);

	//// Nothing matched.
	//if (hklDefault == hklSet)
	//{
	//	// The default keyboard sets set for odd reasons. Just ignore it.
	//	// Review: what if the HKL's are different versions of the same language,
	//	// eg UK and US English?
	//	return false;
	//}
	//else
	//{
	//	StrApp strb(kstidInvalKybd);
	//	::MessageBox(NULL, strb.Chars(), _T("DEBUG"), MB_OK | MB_ICONERROR);
	//}
	return true; // disallow the change.
}

/*----------------------------------------------------------------------------------------------
	Handle default context menu for the view window.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::OnContextMenu(HWND hwnd, Point pt)
{
	if (m_qrootb)
	{
		// See if we are over an External Link.
		bool fFoundLinkStyle;
		IVwSelectionPtr qvwsel;
		StrAppBuf strbFile;
		if (GetExternalLinkSel(fFoundLinkStyle, &qvwsel, strbFile) && fFoundLinkStyle)
		{
			CheckHr(qvwsel->Install());

			HMENU hmenuPopup = ::CreatePopupMenu();
			StrApp str;
			str.Load(kstidExtLinkOpen);
			::AppendMenu(hmenuPopup, MF_STRING, kcidExtLinkOpen, str.Chars());
			str.Load(kstidExtLinkOpenWith);
			::AppendMenu(hmenuPopup, MF_STRING, kcidExtLinkOpenWith, str.Chars());
			str.Load(kstidExtLink);
			::AppendMenu(hmenuPopup, MF_STRING, kcidExtLink, str.Chars());
			str.Load(kstidExtLinkRemove);
			::AppendMenu(hmenuPopup, MF_STRING, kcidExtLinkRemove, str.Chars());
			AddExtraContextMenuItems(hmenuPopup);

			AfMainWnd * pafw = m_pwndSubclass->MainWindow();
			Point ptClient(pt);
			MapWindowPoints(NULL, hwnd, (POINT *)&ptClient, 1);
			TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_LEFTBUTTON | TPM_RIGHTBUTTON,
				pt.x, pt.y, 0, (pafw ? pafw->Hwnd() : hwnd), NULL);
			::DestroyMenu(hmenuPopup);
			return true;
		}
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Get in the vector the list of writing system identifiers currently installed in the
	writing system factory for the current root box. The current writing system for the
	selection is duplicated as the first item in the array (this causes it to be found
	first in searches). Return true if there is at least one valid writing system.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::GetWsListCurrentFirst(IVwSelection * pvwsel, Vector<int> & vws,
	ILgWritingSystemFactory ** ppwsf)
{
	// Get the writing system factory associated with the root box.
	ISilDataAccessPtr qsdaT;
	CheckHr(m_qrootb->get_DataAccess(&qsdaT));
	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));

	int cws;
	CheckHr(qwsf->get_NumberOfWs(&cws));
	vws.Resize(cws + 1);
	CheckHr(qwsf->GetWritingSystems(vws.Begin() + 1, cws));
	if (cws == 0 || (cws == 1 && vws[1] == 0))
		return false;	// no writing systems to work with

	// Put the writing system of the selection first in the list, which gives it priority--
	// we'll find it first if it matches.
	int wsSel;
	if (pvwsel && GetWsOfSelection(pvwsel, &wsSel))
	{
		vws[0] = wsSel;
	}
	else
	{
		vws[0] = vws[1];
	}
	*ppwsf = qwsf.Detach();
	return true;
}

/*----------------------------------------------------------------------------------------------
	The Keyman keyboard has changed. Determine the writing system that is probably implied,
	and apply it to the current range and/or future typing.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::OnKeymanKeyboardChange(UINT wpFlags, UINT lpHKL)
{
	OutputDebugStr(_T("AfVwRootSite::OnKeymanKeyboardChange\n"));
	if (!m_qrootb)
		return; // For paranoia.
	IVwSelectionPtr qvwsel = NULL;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
		return; // can't do anything useful.
	ILgWritingSystemFactoryPtr qwsf;
	Vector<int> vws;
	if (!GetWsListCurrentFirst(qvwsel, vws, &qwsf))
		return; // not enough valid writing systems to make it worth considering a change.

	SmartBstr sbstrKeymanKbd;
	ILgKeymanHandlerPtr qkh;
	qkh.CreateInstance(CLSID_LgKeymanHandler);
	CheckHr(qkh->get_ActiveKeyboardName(&sbstrKeymanKbd));


	int wsMatch = -1;
	const OLECHAR * pchKeymanKbd = L"";
	if (sbstrKeymanKbd.Length() != 0 && wcscmp(sbstrKeymanKbd.Chars(), L"(None)") != 0)
		pchKeymanKbd = sbstrKeymanKbd.Chars();
	for (int iws = 0; iws < vws.Size(); iws++)
	{
		// Don't consider switching to the default, dummy writing system.
		if (vws[iws] == 0)
			continue;

		IWritingSystemPtr qws;
		CheckHr(qwsf->get_EngineOrNull(vws[iws], &qws));
		if (!qws)
			continue;

		SmartBstr sbstrWsKbd;
		CheckHr(qws->get_KeymanKbdName(&sbstrWsKbd));

		if (wcscmp(pchKeymanKbd, sbstrWsKbd.Chars()) == 0)
		{
			wsMatch = vws[iws];
			if (wsMatch == vws[0])
				return; // no change from current.
			break;
		}
	}

	if (wsMatch == -1) // no known writing system uses this keyboard
		return;

	m_wsPending = -1;

	ComBool fRange;
	CheckHr(qvwsel->get_IsRange(&fRange));
	if (fRange)
	{
		// Delay handling it until we get an insertion point.
		m_wsPending = wsMatch;
		return;
	}

	ITsTextPropsPtr qttp; // props of current selection, an IP (therefore only 1 lot of props).
	IVwPropertyStorePtr qvps;
	int cttp;
	CheckHr(qvwsel->GetSelectionProps(1, &qttp, &qvps, &cttp));

	ITsPropsBldrPtr qtpb;
	ITsTextPropsPtr qttpNew;
	CheckHr(qttp->GetBldr(&qtpb));
	CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, wsMatch));
	CheckHr(qtpb->GetTextProps(&qttpNew));
	ITsTextProps * pttpNew = qttpNew; // want to simulate an array, can't use &qttpNew.
	CheckHr(qvwsel->SetSelectionProps(1, &pttpNew));
	// Better not to do this, we have the right keyboard already set.
	//CheckHr(SelectionChanged(qvwsel));
}

/*----------------------------------------------------------------------------------------------
	The system keyboard has changed. Determine the corresponding codepage to use when
	processing characters.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::OnInputLangChange(UINT wpFlags, UINT lpHKL)
{
	//	Get the language identifier.
	int nLangID = (int)LOWORD(lpHKL);

//	OutputDebugString(_T("OnInputLangChange: x"));
//	achar rgch[20];
//	_itot(nLangID, rgch, 16);
//	OutputDebugString(rgch);
//	OutputDebugString(_T("\n"));

	SetCodePageForLangId(nLangID);

	//	If possible, adjust the language of the selection to be one that matches the
	//	keyboard just selected.
	IVwSelectionPtr qvwsel = NULL;
	if (m_qrootb)
		CheckHr(m_qrootb->get_Selection(&qvwsel));
	HandleKeyboardChange(qvwsel, nLangID);
}


/*----------------------------------------------------------------------------------------------
	Get the code page based on the lang id.
----------------------------------------------------------------------------------------------*/
int GetCodePageForLangId(int nLangID)
{
	Assert((nLangID & 0x0000FFFF) == nLangID);	// really only 16-bits

	//	Construct a locale identifer.
	int nLocaleID = MAKELCID(nLangID, SORT_DEFAULT);

	achar rgch[32];
	memset(rgch, 0, 32 * isizeof(achar));
	::GetLocaleInfo(nLocaleID, LOCALE_IDEFAULTANSICODEPAGE, rgch, 32);

	return _tstoi(rgch);
}


/*----------------------------------------------------------------------------------------------
	Set the code page based on the lang id.
	Enhance JohnT: if we actually USE the code page for something, OnKeymanKeyboardChange
	might also need to do something about setting it.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::SetCodePageForLangId(int nLangID)
{
	m_nCodePage = GetCodePageForLangId(nLangID);
}

/*----------------------------------------------------------------------------------------------
	Process left button down (WM_LBUTTONDOWN). xp/yp are current coordinates. grfmk identifies
	button/keys pressed.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::OnLButtonDblClk(uint grfmk, int xp, int yp)
{
	AssertPtr(Window());
	::SetCapture(Window()->Hwnd());

	// Convert to box coords and pass to root box (if any).
	if (m_qrootb)
	{
		SwitchFocusHere();
		InitGraphics();

		PixelToView(xp, yp);

		RECT rcSrcRoot;
		RECT rcDstRoot;
		GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);

		m_qrootb->MouseDblClk(xp, yp, rcSrcRoot, rcDstRoot);

		UninitGraphics();
	}
	return true;
}

void AfVwRootSite::CallMouseDown(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot)
{
	if (m_qrootb)
	{
		m_wsPending = -1;

		m_qrootb->MouseDown(xp, yp, rcSrcRoot, rcDstRoot);
	}
}
void AfVwRootSite::CallMouseDownExtended(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot)
{
	if (m_qrootb)
	{
		m_wsPending = -1;

		m_qrootb->MouseDownExtended(xp, yp, rcSrcRoot, rcDstRoot);
	}
}

/*----------------------------------------------------------------------------------------------
	Process left button up (WM_LBUTTONUP). xp/yp are current coordinates. grfmk identifies
	button/keys pressed.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::OnLButtonUp(uint grfmk, int xp, int yp)
{
	::ReleaseCapture();

	// Convert to box coords and pass to root box (if any).
	if (m_qrootb)
	{
		InitGraphics();

		PixelToView(xp, yp);

		RECT rcSrcRoot;
		RECT rcDstRoot;
		GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
//		m_qrootb->MouseUp(xp, yp, rcSrcRoot, rcDstRoot);
		CallMouseUp(xp, yp, rcSrcRoot, rcDstRoot);
		UninitGraphics();
	}
	return true;
}
void AfVwRootSite::CallMouseUp(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot)
{
	m_qrootb->MouseUp(xp, yp, rcSrcRoot, rcDstRoot);
}


/*----------------------------------------------------------------------------------------------
	Process right button down (WM_RBUTTONDOWN). xp/yp are current coordinates. grfmk identifies
	button/keys pressed.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::OnRButtonDown(uint grfmk, int xp, int yp)
{
	AssertPtr(Window());

	if (m_qrootb)
	{
		SwitchFocusHere();
		InitGraphics();

		PixelToView(xp, yp);

		RECT rcSrcRoot;
		RECT rcDstRoot;
		GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);

		IVwSelectionPtr qsel;
		m_qrootb->get_Selection(&qsel);
		if (qsel)
		{
			// If we don't have a selection, we're certainly not in a range!
			ComBool fRange;
			CheckHr(qsel->get_IsRange(&fRange));
			if (fRange)
			{
				// TODO KenZ: We need a better way to determine if the cursor is in a selection
				// when the selection spans partial lines.
				// We don't want to destroy a range selection if we are within the range, since
				// it is quite likely the user will want to do a right+click cut or paste.
				Rect rdPrimary;
				Rect rdSecondary;
				ComBool fSplit;
				ComBool fEndBeforeAnchor;
				Rect rdIdeal;
				CheckHr(qsel->Location(m_qvg, rcSrcRoot, rcDstRoot, &rdPrimary, &rdSecondary,
							&fSplit, &fEndBeforeAnchor));
				if (xp >= rdPrimary.left && xp < rdPrimary.right && yp >= rdPrimary.top &&
					yp < rdPrimary.bottom)
				{
					UninitGraphics();
					return true;
				}
			}
		}
		// Make an invisible selection to see if we are in editable text.
		bool fEditable = true;
		m_qrootb->MakeSelAt(xp, yp, rcSrcRoot, rcDstRoot, false, &qsel);
		if (!qsel)
			return false;		// This can legitimately fail in FwFilterWnd.
		int cttp = 0;
		ComVector<ITsTextProps> vqttp;
		ComVector<IVwPropertyStore> vqvps;
		CheckHr(qsel->GetSelectionProps(0, NULL, NULL, &cttp));
		vqttp.Resize(cttp);
		vqvps.Resize(cttp);
		CheckHr(qsel->GetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin(),
			(IVwPropertyStore **)vqvps.Begin(), &cttp));
		for (int ittp = 0; ittp < vqttp.Size(); ++ittp)
		{
			ITsTextPropsPtr qttp = vqttp[ittp];
			int nVar;
			int nVal;
			HRESULT hr;
			if (qttp)
				CheckHr(hr = qttp->GetIntPropValues(ktptEditable, &nVar, &nVal));
			if (hr == S_FALSE)
				CheckHr(vqvps[ittp]->get_IntProperty(ktptEditable, &nVal));
			if (nVal == ktptNotEditable)
			{
				fEditable = false;
				break;
			}
		}

		// Note we need to UninitGraphics before processing CallMouseDown since that call
		// may jump to another record which invalidates this graphics object.
		UninitGraphics();

		if (fEditable)
		{
			// Make a simple text selection without executing a mouse click. This is
			// needed in order to not launch a hot link when we right+click.
			HRESULT hr;
			IgnoreHr(hr = m_qrootb->MakeSelAt(xp, yp, rcSrcRoot, rcDstRoot, true, NULL));
			if (FAILED(hr) || hr == S_FALSE)
				ThrowHr(WarnHr(E_FAIL));
		}
		else
		{
			// Use standard mouse click so that it will find an appropriate text box.
			if (grfmk & MK_SHIFT)
				CallMouseDownExtended(xp, yp, rcSrcRoot, rcDstRoot);
			else
				CallMouseDown(xp, yp, rcSrcRoot, rcDstRoot);

			// This is a kludge to handle the fact that the system seems to get in a funny
			// state after right-clicks, even though the effect of our code is exactly the
			// same as a left-click. This seems to help us at least avoid a crash.
			SetFocusToRootSite();
		}
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Process mouse move (WM_MOUSEMOVE). xp/yp are current coordinates. grfmk identifies
	button/keys pressed.
----------------------------------------------------------------------------------------------*/
bool AfInactiveRootSite::OnMouseMove(uint grfmk, int xp, int yp)
{
	// Convert to box coords and pass to root box (if any).
	ComBool fInObject(false);
	ComBool fInOverlayTag(false);
	int odt = 0;
	if (m_qrootb)
	{
		InitGraphics();

		PixelToView(xp, yp);

		RECT rcSrcRoot;
		RECT rcDstRoot;
		GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);

		// For now at least we care only if the mouse is down.
		if (grfmk & MK_LBUTTON)
			CallMouseMoveDrag(xp, yp, rcSrcRoot, rcDstRoot);
		UninitGraphics();
		// Check whether we need a hand or I-beam cursor.
		CheckHr(m_qrootb->get_IsClickInObject(xp, yp, rcSrcRoot, rcDstRoot, &odt, &fInObject));

		int iGuid;
		SmartBstr sbstrGuids;
		RECT rcTag;
		RECT rcAllTags;
		ComBool fOpeningTag;
		CheckHr(m_qrootb->get_IsClickInOverlayTag(xp, yp, rcSrcRoot, rcDstRoot, &iGuid,
			&sbstrGuids, &rcTag, &rcAllTags, &fOpeningTag, &fInOverlayTag));
		if (fInOverlayTag)
		{
			IVwOverlayPtr qvo;
			CheckHr(m_qrootb->get_Overlay(&qvo));
			if (qvo)
			{
				ComBool fHidden;
				COLORREF clrFore;
				COLORREF clrBack;
				COLORREF clrUnder;
				int unt;
				OLECHAR rgchAbbr[256];
				int cchAbbr;
				OLECHAR rgchName[256];
				int cchName;
				StrAppBuf strbAbbr;
				StrAppBuf strbName;
				StrAppBuf strbTag;
				OLECHAR * prgchGuid = (OLECHAR *)sbstrGuids.Chars();
				int cguid = sbstrGuids.Length() / (kcchGuidRepLength * isizeof(OLECHAR));

				if (iGuid == -1)
				{
					for (int iguidT = 0; iguidT < cguid; iguidT++)
					{
						CheckHr(qvo->GetDispTagInfo(prgchGuid, &fHidden, &clrFore, &clrBack,
							&clrUnder, &unt, rgchAbbr, isizeof(rgchAbbr) / isizeof(OLECHAR),
							&cchAbbr, rgchName, isizeof(rgchName) / isizeof(OLECHAR),
							&cchName));
						strbAbbr.Assign(rgchAbbr, cchAbbr);
						strbName.Assign(rgchName, cchName);
						if (iguidT == 0)
							strbTag.Format(_T("%s - %s"), strbAbbr.Chars(), strbName.Chars());
						else
							strbTag.FormatAppend(_T("%n%s - %s"), strbAbbr.Chars(),
								strbName.Chars());
						prgchGuid += kcchGuidRepLength;
					}
				}
				else
				{
					prgchGuid += kcchGuidRepLength * iGuid;
					CheckHr(qvo->GetDispTagInfo(prgchGuid, &fHidden, &clrFore, &clrBack,
						&clrUnder, &unt, rgchAbbr, isizeof(rgchAbbr) / isizeof(OLECHAR),
						&cchAbbr, rgchName, isizeof(rgchName) / isizeof(OLECHAR), &cchName));
					strbAbbr.Assign(rgchAbbr, cchAbbr);
					strbName.Assign(rgchName, cchName);
					strbTag.Format(_T("%s - %s"), strbAbbr.Chars(), strbName.Chars());
				}

				if (!m_hwndOverlayTool)
				{
					m_hwndOverlayTool = ::CreateWindow(TOOLTIPS_CLASS, NULL, TTS_ALWAYSTIP,
						0, 0, 0, 0, Window()->Hwnd(), 0, ModuleEntry::GetModuleHandle(), NULL);
					::SendMessage(m_hwndOverlayTool, TTM_SETMAXTIPWIDTH, 0, 65000);
				}

				// See if this Overlay Tag has already been added to the tooltip control.
				// We use the top left corner of the Overlay Tag as its ID.
				TOOLINFO ti = { TTTOOLINFOA_V2_SIZE, TTF_TRANSPARENT | TTF_SUBCLASS};
				ti.hwnd = Window()->Hwnd();
				ti.uId = MAKELPARAM(rcTag.left, rcTag.top);
				ti.rect = rcTag;
				ti.hinst = ModuleEntry::GetModuleHandle();
				ti.lpszText = (LPTSTR)strbTag.Chars();
				if (!::SendMessage(m_hwndOverlayTool, TTM_GETTOOLINFO, 0, (LPARAM)&ti))
				{
					// Add this Overlay Tag rectangle to the tooltip.
					::SendMessage(m_hwndOverlayTool, TTM_ADDTOOL, 0, (LPARAM)&ti);

					// Remove any other tools in the tooltip.
					if (::SendMessage(m_hwndOverlayTool, TTM_GETTOOLCOUNT, 0, 0) > 1)
					{
						::SendMessage(m_hwndOverlayTool, TTM_ENUMTOOLS, 0, (LPARAM)&ti);
						::SendMessage(m_hwndOverlayTool, TTM_DELTOOL, 0, (LPARAM)&ti);
					}
				}
			}
			else
			{
				fInOverlayTag = false;
			}
		}
		if (!fInOverlayTag && m_hwndOverlayTool)
		{
			// Remove all tools in the tooltip.
			if (::SendMessage(m_hwndOverlayTool, TTM_GETTOOLCOUNT, 0, 0) == 1)
			{
				TOOLINFO ti = { TTTOOLINFOA_V2_SIZE };
				::SendMessage(m_hwndOverlayTool, TTM_ENUMTOOLS, 0, (LPARAM)&ti);
				::SendMessage(m_hwndOverlayTool, TTM_DELTOOL, 0, (LPARAM)&ti);
			}
		}

		bool fFoundLinkStyle = false;
		if (fInObject && odt == kodtExternalPathName)
		{
			IVwSelectionPtr qvwsel;
			StrAppBuf strbFile;
			Point pt(xp, yp);
			if (GetExternalLinkSel(fFoundLinkStyle, &qvwsel, strbFile, &pt) && fFoundLinkStyle)
			{
				if (!m_hwndExtLinkTool)
				{
					m_hwndExtLinkTool = ::CreateWindow(TOOLTIPS_CLASS, NULL, TTS_ALWAYSTIP,
						0, 0, 0, 0, Window()->Hwnd(), 0, ModuleEntry::GetModuleHandle(), NULL);
				}

				// Adjust the filename if needed.
				if (AfApp::Papp())
				{
#ifdef DEBUG
	// We probably want to change all occurrences of GetCurMainWnd() to MainWindow()
	// in this file, but we didn't take the time to verify this would always work. These DEBUG
	// asserts in this file are a temporary test to see if there is ever any difference between
	// the two. If one is hit, it's likely that GetCurMainWnd is returning the wrong value. If
	// this doesn't cause any problem after some time, we can go ahead and switch them all
	// to MainWindow.
	AfMainWnd * pafw1 = AfApp::Papp()->GetCurMainWnd();
	AfMainWnd * pafw2 = Window()->MainWindow();
	Assert(pafw1 == pafw2);
#endif
					AfMainWnd * pafw = AfApp::Papp()->GetCurMainWnd();
					if (pafw)
					{
						AfLpInfo * plpi = pafw->GetLpInfo();
						if (plpi)	// WorldPad doesn't have an LpInfo.
							plpi->MapExternalLink(strbFile);
					}
				}

				HoldGraphics hg(this);
				RECT rcPrimary;
				RECT rcSecondary;
				ComBool fSplit;
				ComBool fEndBeforeAnchor;
				CheckHr(qvwsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rcPrimary,
					&rcSecondary, &fSplit, &fEndBeforeAnchor));

				// See if this External Link has already been added to the tooltip control.
				// We use the top left corner of the External Link as its ID.
				TOOLINFO ti = { TTTOOLINFOA_V2_SIZE, TTF_TRANSPARENT | TTF_SUBCLASS};
				ti.hwnd = Window()->Hwnd();
				ti.uId = MAKELPARAM(rcPrimary.left, rcPrimary.top);
				ti.rect = rcPrimary;
				ti.hinst = ModuleEntry::GetModuleHandle();
				ti.lpszText = (LPTSTR)strbFile.Chars();
				if (!::SendMessage(m_hwndExtLinkTool, TTM_GETTOOLINFO, 0, (LPARAM)&ti))
				{
					// Add this External Link rectangle to the tooltip.
					::SendMessage(m_hwndExtLinkTool, TTM_ADDTOOL, 0, (LPARAM)&ti);

					// Remove any other tools in the tooltip.
					if (::SendMessage(m_hwndExtLinkTool, TTM_GETTOOLCOUNT, 0, 0) > 1)
					{
						::SendMessage(m_hwndExtLinkTool, TTM_ENUMTOOLS, 0, (LPARAM)&ti);
						::SendMessage(m_hwndExtLinkTool, TTM_DELTOOL, 0, (LPARAM)&ti);
					}
				}
			}
		}
		if (!fFoundLinkStyle && m_hwndExtLinkTool)
		{
			// Remove all tools in the tooltip.
			if (::SendMessage(m_hwndExtLinkTool, TTM_GETTOOLCOUNT, 0, 0) == 1)
			{
				TOOLINFO ti = { TTTOOLINFOA_V2_SIZE };
				::SendMessage(m_hwndExtLinkTool, TTM_ENUMTOOLS, 0, (LPARAM)&ti);
				::SendMessage(m_hwndExtLinkTool, TTM_DELTOOL, 0, (LPARAM)&ti);
			}
		}
	}
	if (fInOverlayTag ||
		(fInObject && (odt == kodtNameGuidHot || odt == kodtExternalPathName
			|| odt == kodtOwnNameGuidHot)))
	{
		::SetCursor(::LoadCursor(NULL, MAKEINTRESOURCE(IDC_HAND)));
	}
	else
	{
		::SetCursor(::LoadCursor(NULL, MAKEINTRESOURCE(IDC_IBEAM)));
	}
	return true;
}


void AfVwRootSite::CallMouseMoveDrag(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot)
{
	m_qrootb->MouseMoveDrag(xp, yp, rcSrcRoot, rcDstRoot);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void AfVwRootSite::OnSysChar(UINT nChar, UINT nRepCnt, UINT nFlags)
{
	if (m_qrootb)
		CallOnSysChar((int) nChar);
}
void AfVwRootSite::CallOnSysChar(int chw)
{
	m_qrootb->OnSysChar(chw);
}


/*----------------------------------------------------------------------------------------------
	Handle keyboard message. Returns false if an arrow key caused a movement beyond text.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
{
	if (m_qrootb)
	{
		SHORT nShift;
		SHORT nControl;
		nShift = ::GetKeyState(VK_SHIFT);
		nControl = ::GetKeyState(VK_CONTROL);
		VwShiftStatus ss = kfssNone;
		if (nShift < 0)
		{
			if (nControl < 0)
				ss = kgrfssShiftControl;
			else
				ss = kfssShift;
		}
		else if (nControl < 0)
		{
			ss = kfssControl;
		}
		if ((nChar >= VK_END && nChar <= VK_DOWN) || nChar >= VK_F7 && nChar <= VK_F8)
			// && (nFlags & (KF_EXTENDED | 0x40))) -- doesn't work with KeyMan
		{
			if (!CallOnExtendedKey(static_cast<int>(nChar), ss))
			{
				AfVwScrollWndBase * pavswbInner = dynamic_cast<AfVwScrollWndBase *>(Window());
				if (!pavswbInner)
					return false; // Return for handling arrows going to next field.

				// This is a document or browse window, and we need to make sure that if
				// arrow up or arrow down failed then we make the current selection visible,
				// which it may not otherwise be if CTRL+A has just been used. May as well
				// use on all nChar between VK_END and VK_DOWN as it probably does no harm.
				ScrollSelectionIntoView(NULL, kssoDefault);
				return true;	// On the basis that we have handled the key, really.
			}
			AfVwScrollWndBase * pavswb = dynamic_cast<AfVwScrollWndBase *>(Window());
			if (pavswb && nChar == VK_END && (ss & kfssControl))
			{
				// Control end is supposed to scroll all the way to the end.
				// We only know how to do this reliably for one class at present; otherwise,
				// settle for making visible the selection we made at the end.
				pavswb->ScrollToEnd();
			}
			else if (pavswb && nChar == VK_HOME && (ss & kfssControl))
			{
				// Control home is supposed to scroll all the way to the top
				pavswb->ScrollToTop();
			}
			else
			{
				InitGraphics();
				MakeSelectionVisible1();
				UninitGraphics();
			}
		}
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle extended keys. Returns false if it wasn't handled (e.g., arrow key beyond valid
	characters.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CallOnExtendedKey(int chw, VwShiftStatus ss)
{
	m_wsPending = -1;

	int nFlags = ComplexKeyBehavior(chw, ss);
	return m_qrootb->OnExtendedKey(chw, ss, nFlags) == S_FALSE ? false : true;
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void AfVwRootSite::OnTimer(UINT nIDEvent)
{
	switch (nIDEvent)
	{
	case knTimerFlashIP:
		if (m_qrootb && IsCurrentPane())
			m_qrootb->FlashInsertionPoint(); // Ignore any error code.
		// We do not need to redo menu item enabling if the only thing that happened was to
		// flash the IP.
		AfApp::Papp()->SuppressIdle();
		break;
	}
}


/*----------------------------------------------------------------------------------------------
	Add or remove the tag in the given overlay of the current string.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::ModifyOverlay(bool fApplyTag, IVwOverlay * pvo, int itag)
{
	AssertPtr(pvo);

	if (!m_qrootb)
		return;

	HVO hvo;
	COLORREF clrFore;
	COLORREF clrBack;
	COLORREF clrUnder;
	int unt;
	ComBool fHidden;
	GUID uid;
	CheckHr(WarnHr(pvo->GetDbTagInfo(itag, &hvo, &clrFore, &clrBack, &clrUnder, &unt, &fHidden,
		(OLECHAR *)&uid)));

	IVwSelectionPtr qvwsel;
	TtpVec vqttp;
	VwPropsVec vqvps;
	if (GetCharacterProps(&qvwsel, vqttp, vqvps))
	{
		int cttp = vqttp.Size();
		SmartBstr sbstr;
		for (int ittp = 0; ittp < cttp; ittp++)
		{
			CheckHr(WarnHr(vqttp[ittp]->GetStrPropValue(ktptTags, &sbstr)));
			int cuid = sbstr.Length() * isizeof(wchar) / isizeof(GUID);
			GUID * puid = (GUID *)sbstr.Chars();
			SmartBstr sbstrT;
			int nRes = -1;
			int iuid;
			for (iuid = -1; nRes < 0 && ++iuid < cuid; )
				nRes = CompareGuids((OLECHAR *) &uid, (OLECHAR *)(puid + iuid));

			if (fApplyTag)
			{
				// Add the tag if it does not exist.
				if (nRes == 0)
				{
					// The tag has already been applied to the textprop, so it doesn't
					// need to be modified.
					vqttp[ittp] = NULL;
					continue;
				}
				else
				{
					// We need to add the tag to the textprop.
					int cchBefore = isizeof(GUID) * iuid / isizeof(wchar);
					sbstrT.Append(sbstr.Chars(), cchBefore);
					sbstrT.Append((OLECHAR *)&uid, isizeof(GUID) / isizeof(wchar));
					sbstrT.Append(sbstr.Chars() + cchBefore,
						sbstr.Length() - cchBefore);
				}
			}
			else
			{
				// Remove the tag from the textprop.
				Assert(nRes == 0 && iuid < cuid);
				int cchBefore = isizeof(GUID) * iuid / isizeof(wchar);
				sbstrT.Append(sbstr.Chars(), cchBefore);
				sbstrT.Append(sbstr.Chars() + cchBefore + isizeof(GUID) / isizeof(OLECHAR),
					sbstr.Length() - cchBefore - isizeof(GUID) / isizeof(OLECHAR));
			}

			ITsPropsBldrPtr qtpb;
			CheckHr(WarnHr(vqttp[ittp]->GetBldr(&qtpb)));
			CheckHr((qtpb->SetStrPropValue(ktptTags, sbstrT.Bstr())));
			// JT: Do NOT use WarnHr here. It causes GetTextProps to be called twice,
			// which produces a memory leak.
			ITsTextPropsPtr qttp;
			CheckHr(qtpb->GetTextProps(&qttp));
			vqttp[ittp] = qttp;
		}
		CheckHr(qvwsel->SetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin()));

		// Update the RnGenericRec_PhraseTags table as necessary.
		// (Yes, this is special case code!)

		AfDbInfo * pdbi = NULL;
		AfMainWnd * pafw = m_pwndSubclass->MainWindow();
		if (pafw)
		{
			AfLpInfo * plpi = pafw->GetLpInfo();
			if (plpi)
				pdbi = plpi->GetDbInfo();
		}
		if (pdbi)
		{
			int clevEnd;
			int clevAnchor;
			HVO hvoEnd;
			HVO hvoAnchor;
			PropTag tagEnd;
			PropTag tagAnchor;
			int ihvo;
			int cpropPrev;
			IVwPropertyStorePtr qvps;
			CheckHr(qvwsel->CLevels(true, &clevEnd));
			CheckHr(qvwsel->CLevels(false, &clevAnchor));
			int ilev;
			for (ilev = 0; ilev < clevEnd; ++ilev)
			{
				CheckHr(qvwsel->PropInfo(true, ilev, &hvoEnd, &tagEnd, &ihvo,
					&cpropPrev, &qvps));
				if (tagEnd == kflidStText_Paragraphs)
					break;
			}
			// The Advanced button in the overlay configuration dialog uses a simple view
			// where ilev == clevEnd. We do nothing in this case.
			if (ilev != clevEnd)
			{
				Assert(ilev < clevEnd);
				for (ilev = 0; ilev < clevAnchor; ++ilev)
				{
					CheckHr(qvwsel->PropInfo(true, ilev, &hvoAnchor, &tagAnchor, &ihvo,
						&cpropPrev, &qvps));
					if (tagAnchor == kflidStText_Paragraphs)
						break;
				}
				Assert(ilev < clevAnchor);
				IOleDbEncapPtr qode;
				pdbi->GetDbAccess(&qode);
				DbStringCrawler::UpdatePhraseTagsTable(kflidRnGenericRec_PhraseTags,
					fApplyTag, qode, hvo, hvoEnd, hvoAnchor);
			}
		}
	}

	SetFocusToRootSite();
}

/*----------------------------------------------------------------------------------------------
	Subclasses should override CloseRootBox if something else is still using it
	after the window closes. In that case the subclass should do nothing.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::CloseRootBox()
{
	// Close the root box only if not being shared by another pane.
	if (OtherPane())
	{
		// Make sure that we are not its root site; make the other pane its root site
		m_qrootb->SetSite(OtherPane());
	}
	else if (m_qrootb)	// Splitting an empty window is possible, at least in WorldPad.
	{
		m_qrootb->Close();
	}
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void AfVwRootSite::OnReleasePtr()
{
	// We need this test for the benefit of the ActiveX control subclass.
	if (AfApp::Papp())
	{
		RemoveRootRegistration(); // Before clearing root!
		// We need to clear the frame window's active root box pointer if it is set to our
		// m_qrootb.
		AssertPtr(m_pwndSubclass);
		AfMainWnd * pafw = m_pwndSubclass->MainWindow();

		// Previously, there was an assert test on pafw, but in some rare circumstances, it is
		// possible to have no MainWindow. For example, during a Restore from backup operation,
		// the progress dialog runs with no parent, and can produce child dialogs with "what's
		// this?" helps. Closing those helps used to assert here, as there was no MainWindow.
		if (pafw)
		{
			if (pafw->GetActiveRootBox() == m_qrootb)
				pafw->SetActiveRootBox(NULL);
		}
	}

	// We generally need to close the rootbox here or we get memory leaks all over. But if we
	// always do it, we break switching fields in DE views, and anywhere else a root box is
	// shared.
	// Subclasses should override CloseRootBox if something else is still using it
	// after the window closes.
	CloseRootBox();
	m_qrootb.Clear();
	m_qvg.Clear();

	// Make absolutely sure not to leave a command handler around. This stops cleanup of the
	// window, because there is still a reference, and also interferes with proper toolbar
	// enabling.
	if (AfApp::Papp())
		AfApp::Papp()->RemoveCmdHandler(Window(), 1);
}

/*----------------------------------------------------------------------------------------------
	Remove your root registration. This is a separate (virtual) method because some VwWnds,
	such as AfDeVwWnd, which share their root box, may not want to unregister it.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::RemoveRootRegistration()
{
	AssertPtr(m_pwndSubclass);
	AfMainWnd * pafw = m_pwndSubclass->MainWindow();

	// Unregister the root box with the main window. Only if we are not the second pane of
	// two that are sharing it.
	if (m_qrootb && pafw && !OtherPane())
		pafw->UnregisterRootBox(m_qrootb);
}

/*----------------------------------------------------------------------------------------------
	Make a selection that includes all the text.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::SelectAll()
{
	if (!m_qrootb)
		return;

	WaitCursor wc; // creates a wait cursor and makes it active until the end of the method.

	CheckHr(m_qrootb->MakeSimpleSel(true, false, false, true, NULL));
	// Simulate a Ctrl-Shift-End keypress:
	CheckHr(m_qrootb->OnExtendedKey(VK_END, kgrfssShiftControl,
		1)); // logical arrow key behavior
}

/*----------------------------------------------------------------------------------------------
	Insert a picture at the current selection.

	@param pcmd Pointer to the command information (ignored by this method).

	@return True if a selection exists (even if a picture is not inserted), otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdInsertPic1(Cmd * pcmd)
{
	// Get selection. Can't do command unless we have one.
	if (!m_qrootb)
		return false;
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
		return false;
	// Vars to call TextSelInfo and find out whether it is a structured text field.
	ITsStringPtr qtssDummy;
	int ich;
	ComBool fAssocPrev;
	HVO hvoObj;
	PropTag tag;
	int ws;
	CheckHr(qvwsel->TextSelInfo(false, &qtssDummy, &ich, &fAssocPrev, &hvoObj, &tag, &ws));
	if (tag != kflidStTxtPara_Contents)
	{
		::MessageBox(Window()->Hwnd(), _T("Sorry, for now pictures can only be inserted\nin ")
			_T("multiple-paragraph fields like 'Description'"), NULL, MB_OK);
		return true;
	}

	// Open file dialog.
	achar szFile[MAX_PATH];
	::ZeroMemory(szFile, MAX_PATH);
	OPENFILENAME ofn;
	::ZeroMemory(&ofn, sizeof(OPENFILENAME));
	// the constant below is required for compatibility with Windows 95/98 (and maybe NT4)
	ofn.lStructSize = OPENFILENAME_SIZE_VERSION_400;
	ofn.Flags		= OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_HIDEREADONLY;
	ofn.hwndOwner	= Window()->Hwnd();
	ofn.lpstrFilter	= _T("Supported Files Types(*.bmp;*.gif;*.jpg;*.ico;*.emf;*.wmf)\0")
		_T("*.bmp;*.gif;*.jpg;*.ico;*.emf;*.wmf\0")
		_T("Bitmaps (*.bmp)\0*.bmp\0")
		_T("GIF Files (*.gif)\0*.gif\0")
		_T("JPEG Files (*.jpg)\0*.jpg\0")
		_T("Icons (*.ico)\0*.ico\0")
		_T("Enhanced Metafiles (*.emf)\0*.emf\0")
		_T("Windows Metafiles (*.wmf)\0*.wmf\0\0");
	ofn.lpstrTitle	= _T("Open Picture File");
	ofn.lpstrFile	= szFile;
	ofn.nMaxFile	= MAX_PATH;
	if (IDOK != ::GetOpenFileName(&ofn))
		return true; // We handled the command anyway.

	HANDLE hFile = ::CreateFile(szFile, GENERIC_READ, 0, NULL, OPEN_EXISTING, 0, NULL);
	if (INVALID_HANDLE_VALUE == hFile)
		ThrowHr(WarnHr(E_FAIL));
	StrUni stuData;
	int cchData;
	OLECHAR * pchData;
	DWORD dwBytesRead = 0;
	try
	{
		// Anywhere in here we need to close the file if something goes wrong.

		DWORD dwFileSize = GetFileSize(hFile, NULL);
		if (-1 == dwFileSize)
			ThrowHr(WarnHr(E_OUTOFMEMORY));

		// The number of characters need to hold the bytes, plus 1 for the kodtPict constant.
		cchData = (dwFileSize + isizeof(OLECHAR) - 1) / isizeof(OLECHAR) + 1;
		stuData.SetSize(cchData, &pchData);

		LPVOID pvData = pchData + 1;

		// Read file and store in StrUni memory.
		// Note: for Mac (and other BigEndians) need to flip bytes after reading.
		BOOL bRead = ::ReadFile(hFile, pvData, dwFileSize, &dwBytesRead, NULL);
		if (FALSE == bRead)
		{
			ThrowHr(WarnHr(E_OUTOFMEMORY));
		}
	}
	catch (...)
	{
		::CloseHandle(hFile);
		throw;
	}
	::CloseHandle(hFile);

	// OK, we got the binary file data in the string. Now make a TsTextProps where
	// that data is the value of ktptObjData.
	ITsTextPropsPtr qttp;
	ITsPropsBldrPtr qtpb;
	ITsStringPtr qtssEnd;
	int ichEnd;
	ComBool fAssocPrevEnd;
	HVO hvoObjEnd;
	int tagEnd;
	int encEnd;
	CheckHr(qvwsel->TextSelInfo(true, &qtssEnd, &ichEnd, &fAssocPrevEnd, &hvoObjEnd, &tagEnd,
		&encEnd));
	ComBool fIsRange;
	CheckHr(qvwsel->get_IsRange(&fIsRange));
	if (fIsRange)
	{
		// Use the property of the character at the beginning of the range.
		if (fAssocPrevEnd)
		{
			// The range starts with the anchor point.
			ITsStringPtr qtssAnchor;
			int ichAnchor;
			ComBool fAssocPrevAnchor;
			HVO hvoObjAnchor;
			int tagAnchor;
			int encAnchor;
			CheckHr(qvwsel->TextSelInfo(false, &qtssAnchor, &ichAnchor, &fAssocPrevAnchor,
				&hvoObjAnchor, &tagAnchor, &encAnchor));
			CheckHr(qtssAnchor->get_PropertiesAt(ichAnchor, &qttp));
		}
		else
		{
			CheckHr(qtssEnd->get_PropertiesAt(ichEnd, &qttp));
		}
	}
	else
	{
		if (fAssocPrevEnd && ichEnd)
			CheckHr(qtssEnd->get_PropertiesAt(ichEnd-1, &qttp));
		else
			CheckHr(qtssEnd->get_PropertiesAt(ichEnd, &qttp));
	}
	CheckHr(qttp->GetBldr(&qtpb));
	if (dwBytesRead & 1)
		*pchData = kodtPictOdd;
	else
		*pchData = kodtPictEven;
	CheckHr(qtpb->SetStrPropValue(ktptObjData, pchData));
	CheckHr(qtpb->GetTextProps(&qttp));
	// And now a TsString with a single object character and the props we just made.
	ITsStrBldrPtr qtsb;
	qtsb.CreateInstance(CLSID_TsStrBldr);
	OLECHAR chObj = 0xfffc;
	CheckHr(qtsb->ReplaceRgch(0, 0, &chObj, 1, qttp));
	ITsStringPtr qtss;
	CheckHr(qtsb->GetString(&qtss));
	SelPositionInfo spi(OtherPane());
	CheckHr(qvwsel->ReplaceWithTsString(qtss));
	spi.Restore();

	return true;
}

/*----------------------------------------------------------------------------------------------
	Bring up the format font dialog.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdFmtFnt1(Cmd * pcmd)
{
	IVwSelectionPtr qvwsel;
	TtpVec vqttpSel;
	VwPropsVec vqvpsSel;
	VwPropsVec vqvpsSoft;
	if (!SelectionInOneField())
		return false;
	if (!GetCharacterProps(&qvwsel, vqttpSel, vqvpsSel))
		return false;

	int cttp = vqttpSel.Size();

	vqttpSel.Resize(cttp);
	vqvpsSoft.Resize(cttp);

	CheckHr(qvwsel->GetHardAndSoftCharProps(cttp, (ITsTextProps **)vqttpSel.Begin(),
		(IVwPropertyStore **)vqvpsSoft.Begin(), &cttp));

	ISilDataAccessPtr qsda;
	BeginUndoTask(pcmd->m_cid, &qsda);

	ILgWritingSystemFactoryPtr qwsf;
	GetLgWritingSystemFactory(&qwsf);

	const achar * pszHelpFile = NULL;
	if (AfApp::Papp())
		pszHelpFile = AfApp::Papp()->GetHelpFile();

	if (FmtFntDlg::AdjustTsTextProps(Window()->Hwnd(), vqttpSel, vqvpsSoft, qwsf, pszHelpFile,
		false, false, AllowFontFeatures(), OneDefaultFont()))
	{
		// Some change was made.
		for (int ittp = 0; ittp < vqttpSel.Size(); ittp++)
		{
			ITsTextPropsPtr qttpRet;
			if (RemoveRedundantHardFormatting(qsda, vqvpsSoft[ittp], vqttpSel[ittp],
				false, 0, &qttpRet))
			{
				vqttpSel[ittp] = qttpRet;
			}
		}

		CheckHr(qvwsel->SetSelectionProps(cttp, (ITsTextProps **)vqttpSel.Begin()));
	}

	EndUndoTask(qsda);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Bring up the Format/Writing System dialog.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdFmtWrtSys1(Cmd * pcmd)
{
	// Get the writing system from the selection and initialize the dialog with it.
	int ws = -1;
	IVwSelectionPtr qvwsel;
	TtpVec vqttp;
	VwPropsVec vqvps;
	if (!SelectionInOneField())
		return false;
	if (!GetCharacterProps(&qvwsel, vqttp, vqvps))
		return false;
	int ittp;
	ITsTextProps * pttpTmp;
	HRESULT hr;
	for (ittp = 0; ittp < vqttp.Size(); ++ittp)
	{
		pttpTmp = vqttp[ittp];
		int wsTmp, nVar;

		CheckHr(hr = pttpTmp->GetIntPropValues(ktptWs, &nVar, &wsTmp));
		if (SUCCEEDED(hr) && ws == -1)
		{
			ws = wsTmp;
		}
		else if (wsTmp != ws)
		{
			ws = -1;
			break;
		}
	}

	FmtWrtSysDlgPtr qdlg;
	qdlg.Create();
	qdlg->SetInitEnc(ws);

	if (qdlg->DoModal(Window()->Hwnd()) == kctidOk)
	{
		int wsNew = qdlg->SelectedWritingSystem();
		if (wsNew != -1)
		{
			ISilDataAccessPtr qsda;
			BeginUndoTask(pcmd->m_cid, &qsda);

			// Set the writing system in the string
			ApplyWritingSystem(vqttp, wsNew, qvwsel);

			EndUndoTask(qsda);
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Checks to see what state the border toolbar button should be.
	vqttp is the TsTextProps of the current	selection.
	It returns true if the first paragraph has the current border turned on.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CheckBorderButton(TtpVec &vqttp)
{
	AssertPtr(m_pwndSubclass);
	AfMainWnd * pafw = m_pwndSubclass->MainWindow();
	if (!pafw)
		ThrowHr(WarnHr(E_UNEXPECTED));
	int bp = pafw->GetBdrBtnPos();
	// Establish the desired values of the relevant properties.
	// By default all are zero. They are in the order top, bottom, left, right
	int sidesLim = 4;
	static int rgtpt[4] = {ktptBorderTop, ktptBorderBottom,
		ktptBorderLeading, ktptBorderTrailing};
	bool fButtonOn = false;
	ITsTextProps * pttp = NULL;
	if (vqttp.Size() > 0)
		pttp = vqttp[0];

	if(bp == kbpNone || !pttp)
		return false;

	ITsTextPropsPtr qttpNew;
	int var, val;
	switch (bp)
	{
	case kbpAll:
		for (int itpt = 0; itpt < sidesLim; itpt++)
		{
			var = val = -1;
			CheckHr(pttp->GetIntPropValues(rgtpt[itpt], &var, &val));
			if (val < 1)
			{
				fButtonOn = false;
				break;
			}
				fButtonOn = true;
		}
		break;
	case kbpTop:
	case kbpBottom:
	case kbpLeft:
	case kbpRight:
		var = val = -1;
		CheckHr(pttp->GetIntPropValues(rgtpt[bp - 1], &var, &val));
		if (var == ktpvMilliPoint && val > 0)
			fButtonOn = true;
		break;
	case kbpNone:
		for (int itpt = 0; itpt < sidesLim; itpt++)
		{
			var = val = -1;
			CheckHr(pttp->GetIntPropValues(rgtpt[itpt], &var, &val));
			if (val > 0)
			{
				fButtonOn = false;
				break;
			}
				fButtonOn = true;
		}
		break;
	default: // Allow anything else, including kbpNone, to mean no borders
		break;
	}
	return fButtonOn;
}

/*----------------------------------------------------------------------------------------------
	Handles a click related to the format border button. vqttp is the TsTextProps of the current
	selection.
	Puts a new TsTextProps in vqttp for every selected paragraph.  Always return true.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::HandleBorderButton(TtpVec &vqttp)
{
	AssertPtr(m_pwndSubclass);
	AfMainWnd * pafw = m_pwndSubclass->MainWindow();
	if (!pafw)
		ThrowHr(WarnHr(E_UNEXPECTED));
	int bp = pafw->GetBdrBtnPos();
	// Establish the desired values of the relevant properties.
	// By default all are zero. They are in the order top, bottom, left, right
	int sidesLim = 4;
	static int rgtpt[4] = {ktptBorderTop, ktptBorderBottom,
		ktptBorderLeading, ktptBorderTrailing};
	bool fButtonOn = false;
	ITsTextProps * pttp = vqttp[0];

	ITsTextPropsPtr qttpNew;
	int var, val;
	int itptMin = 0; // Lower range of border properties to test
	int itptLim = 4; // Upper limit of border properties to test

	switch (bp)
	{
	case kbpAll:
		for (int itpt = 0; itpt < sidesLim; itpt++)
		{
			var = val = -1;
			if (pttp)
				CheckHr(pttp->GetIntPropValues(rgtpt[itpt], &var, &val));
			if (val < 1)
			{
				fButtonOn = false;
				break;
			}
				fButtonOn = true;
		}
		break;
	case kbpTop:
	case kbpBottom:
	case kbpLeft:
	case kbpRight:
		itptMin = bp - 1; //Only modify one side of border
		itptLim = bp;
		var = val = -1;
		if (pttp)
			CheckHr(pttp->GetIntPropValues(rgtpt[bp - 1], &var, &val));
		if (var == ktpvMilliPoint && val > 0)
			fButtonOn = true;
		break;
	case kbpNone:
		for (int itpt = 0; itpt < sidesLim; itpt++)
		{
			var = val = -1;
			if (pttp)
				CheckHr(pttp->GetIntPropValues(rgtpt[itpt], &var, &val));
			if (val > 0)
			{
				fButtonOn = false;
				break;
			}
				fButtonOn = true;
		}
		break;
	default: // Allow anything else, including kbpNone, to mean no borders
		break;
	}

	int dmp[4];
	for (int idmp = 0; idmp < sidesLim; idmp++)
		dmp[idmp] = 0;
	int dmpThick;
	int nBorderColor;
	// get the width and color value by checking all sides for exixting borders
	int ittp;
	int cttp = vqttp.Size();
	for (ittp = 0; ittp < cttp; ++ittp)
	{
		dmpThick = 0;
		ITsTextProps * pttp = vqttp[ittp];
		int var, val;
		nBorderColor = kclrBlack; // borders are black by default
		if (pttp)
		{
			for (int i = 0; i < sidesLim; ++i)
			{
				CheckHr(pttp->GetIntPropValues(rgtpt[i], &var, &val));
				if (dmpThick < val)
					dmpThick = val;
			}
			var = val = -1;
			CheckHr(pttp->GetIntPropValues(ktptBorderColor, &var, &val));
			if (val != kclrBlack)
				nBorderColor = val;
		}

		// If there are no existing borders than set width about a pixel thick.
		if (dmpThick == 0)
			dmpThick = kdzmpInch / 96;

		switch (bp)
		{
		case kbpAll:
			if (!fButtonOn)
			{
				dmp[0] = dmpThick;
				dmp[1] = dmpThick;
				dmp[2] = dmpThick;
				dmp[3] = dmpThick;
			}
			break;
		case kbpTop:
		case kbpBottom:
		case kbpLeft:
		case kbpRight:
			if (!fButtonOn)
				dmp[bp - 1] = dmpThick;
			break;
		case kbpNone:
		default: // Allow anything else, including kbpNone, to mean no borders
			break;
		}

		// Update the properties of the paragraph(s).
		ITsTextPropsPtr qttpNew;
		ITsPropsBldrPtr qtpb;
		// Get the current actual values of the properties and compare
		for (int itpt = itptMin; itpt < itptLim; itpt++)
		{
			var = val = -1;
			if (pttp)
				CheckHr(pttp->GetIntPropValues(rgtpt[itpt], &var, &val));
			// If the property has the right value all is well
			if (var == ktpvMilliPoint && val == dmp[itpt])
				continue;
			// If it has no value at all and we wanted zero, that's OK too
			if (var == -1 && dmp[itpt] == 0)
				continue;
			// Update it..and also the corresponding pad amount
			int tptPad = 0; // corresponding pad direction
			int dzmpPad = 0; // corresponding pad amount
			FmtBdrDlg::GetPadInfo(rgtpt[itpt], tptPad, dzmpPad);
			if (!qtpb)
			{
				if (pttp)
					CheckHr(pttp->GetBldr(&qtpb));
				else
					qtpb.CreateInstance(CLSID_TsPropsBldr);
			}
			if (dmp[itpt])
			{
				var = ktpvMilliPoint;
				val = dmp[itpt];
			}
			else
			{
				var = val = -1;
				dzmpPad = -1; // clear that too.
			}
			CheckHr(qtpb->SetIntPropValues(rgtpt[itpt], var, val));
			CheckHr(qtpb->SetIntPropValues(tptPad, var, dzmpPad));
		}

		if (qtpb)
			CheckHr(qtpb->GetTextProps(&qttpNew));
		// Update the vector either to the new TsTextProps, or to NULL
		vqttp[ittp] = qttpNew;
	}
	return true;
}

static const wchar * pwszBullList = L"Bulleted List";
static const wchar * pwszNumList = L"Numbered List";

/*----------------------------------------------------------------------------------------------
	FormatParas is used to update paragraph properties, that is, to modify the TsTextProps
	object that is the style of each of the selected (or partly selected) paragraphs.

	This method is invoked from the toolbar by the paragraph style combobox, and the left,
	center, and right justification buttons, and the increase and decrease indentation buttons.
	It may also be invoked from the menu items "Format-Paragraph...", "Format-Border...", and
	"Format-Bullets & Numbering...". See the switch statement.

	FormatParas begins by getting the paragraph properties from the selection. This is done by
	the	call, GetParagraphProps(&qvwsel, hvoText, tagText, vqvps, ihvoFirst, ihvoLast, &qsda,
	vqttp);. If paragraph properties cannot be retrieved through a selection, FormatParas
	returns false. If no text properties are retrieved in vqttp, FormatParas returns true since
	there is nothing to do.

	Next, in response to menu item choices, AdjustTsTextProps is called for the appropriate
	class: FmtParaDlg, FmtBdrDlg, or FmtBulNumDlg. AdjustTsTextProps returns the updated
	paragraph properties through the variable vqttp. If AdjustTsTextProps returns false,
	FormatParas returns true since no properties were changed. Alternatively, in response to
	a toolbar control selection, immediate changes are made to paragraph properties retrieved
	from the selection, and the variable vqttp is updated.

	If FormatParas has not returned as described above, it narrows the range of TsTextProps to
	those that are not NULL. Then, it saves the view selection level information by calling
	AllTextSelInfo on the selection. To "fake" a property change, PropChanged is called on the
	SilDataAccess pointer. Finally, the selection is restored by a call to MakeTextSelection on
	the RootBox pointer.

	Arguments:
		format - knFormatStyle, knFormatNormal, knFormatBulNum, knFormatBorder, knFormatJustify,
					or knFormatIndent.
		fCanDoRtl - true if we want the bidi version of the paragraph fmt dlg
		fOuterRtl - true if the outer context (eg, overall document direction) is RTL
		var1 - the new value for the ktptAlign property, set for the case knFormatJustify.
		bstrNewVal - the name of a new style, set for the case knFormatStyle.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::FormatParas(int format, bool fCanDoRtl, bool fOuterRtl,
	int var1, BSTR bstrNewVal)
{
	IVwSelectionPtr qvwsel;
	HVO hvoText;
	int tagText;
	VwPropsVec vqvps;
	int ihvoFirst, ihvoLast;
	ISilDataAccessPtr qsda;
	TtpVec vqttp;

	// Get the paragraph properties from the selection. If there is neither a selection nor a
	// paragraph property, return false.
	if (!GetParagraphProps(&qvwsel, hvoText, tagText, vqvps, ihvoFirst, ihvoLast, &qsda, vqttp))
		return false;
	// If there are no TsTextProps for the paragraph(s), return true. There is nothing to
	// format.
	if (0 == vqttp.Size())
		return true;

	int cttp = vqttp.Size();
	TtpVec vqttpHard;
	VwPropsVec vqvpsSoft;
	vqttpHard.Resize(cttp);
	vqvpsSoft.Resize(cttp);
	CheckHr(qvwsel->GetHardAndSoftParaProps(cttp, (ITsTextProps **)vqttp.Begin(),
		(ITsTextProps **)vqttpHard.Begin(),
		(IVwPropertyStore **)vqvpsSoft.Begin(), &cttp));

	ILgWritingSystemFactoryPtr qwsf;
	GetLgWritingSystemFactory(&qwsf);

	StrUni stuNewStyle; // used by bulleted and numbered lists.
#ifdef JT_7_17_01_ButtonsAreStyles
	// We goto here from the bulleted list and numbered list styles, to actually set the
	// style...if we are interpreting them as styles, which is currently disabled.
LRepeat:
#endif
	// Now respond to the user's menu selection to format paragraph properties, or to the
	// user's interaction with toolbar controls that affect paragraph properties. If nothing
	// has changed by the user's interaction with a formatting dialog, return true.
	switch (format)
	{
	case knFormatStyle: // Paragraph style combobox on the toolbar.
		// Set kspStyleName to bstrNewVal, and clear all explicit paragraph formatting.
		{ // BLOCK.
			ITsPropsBldrPtr qtpb;
			int cttp = vqttp.Size();
			for (int ittp = 0; ittp < cttp; ++ittp)
			{
				// ENHANCE JohnT: it would be slightly nicer to detect the case where
				// there is nothing at all to change (the old TsTextProps has only the
				// named style, and it is correct) so we don't have a fake change to undo.

				// ENHANCE JohnT: it would be nicer still to detect we are applying the
				// same style, and if there is explicit formatting put up the dialog
				// asking whether to change the style defn.

				// Make a new TsTextProps object, and set its NamedStyle.
				qtpb.CreateInstance(CLSID_TsPropsBldr);
				CheckHr(qtpb->SetStrPropValue(kspNamedStyle, bstrNewVal));
				CheckHr(qtpb->GetTextProps(&vqttp[ittp]));
			}
		} // End of block.
		break;

	case knFormatNormal: // Format-Paragraph... dialog.
		AssertPtr(AfApp::Papp());
		if (!FmtParaDlg::AdjustTsTextProps(Window()->Hwnd(), fCanDoRtl, fOuterRtl,
			vqttp, vqttpHard, vqvpsSoft, AfApp::Papp()->GetMsrSys()))
		{
			return true;
		}
		break;

	case knFormatBulNum: // Format-Bullets & Numbering... dialog.
		AssertPtr(AfApp::Papp());
		if (!FmtBulNumDlg::AdjustTsTextProps(Window()->Hwnd(), fCanDoRtl, fOuterRtl,
			vqttp, vqttpHard, vqvpsSoft, qwsf, AfApp::Papp()->GetHelpFile()))
		{
			return true;
		}
		break;

	case knFormatBorder: // Format-Border... dialog.
		if (!FmtBdrDlg::AdjustTsTextProps(Window()->Hwnd(), fCanDoRtl, fOuterRtl,
			vqttp, vqttpHard, vqvpsSoft))
		{
			return true;
		}
		break;

	case knFormatJustify: // Left, center, or right justification button on the toolbar.
		// Set ktptAlign to var1.
		{ // BLOCK.
			ITsPropsBldrPtr qtpb;
			int cttp = vqttp.Size();
			for (int ittp = 0; ittp < cttp; ++ittp)
			{
				qtpb = NULL;
				if (vqttp[ittp])
					CheckHr(vqttp[ittp]->GetBldr(&qtpb));
				else
					qtpb.CreateInstance(CLSID_TsPropsBldr);

				// Set the Align property to var1.
				CheckHr(qtpb->SetIntPropValues(ktptAlign, ktpvEnum, var1));
				// If anything changed, we now have a props builder that is the new value for
				// this run. Update the vector, vqttp.
				if (qtpb)
					CheckHr(qtpb->GetTextProps(&vqttp[ittp]));
			}
			break;
		} // End of block.

	case knFormatIndent: // Increase or decrease indentation button on the boolbar.
		// Set ktptLeadingIndent to the calculated value.
		{ // BLOCK.
			ITsPropsBldrPtr qtpb;
			int cttp = vqttp.Size();
			for (int ittp = 0; ittp < cttp; ++ittp)
			{
				int nVar, nOld;
				HRESULT hr = S_FALSE;
				// Get the current value for ktptLeadingIndent, through the PropertyStore if
				// necessary.
				if (vqttp[ittp])
				{
					CheckHr(hr = vqttp[ittp]->GetIntPropValues(ktptLeadingIndent, &nVar,
						&nOld));
				}
				if (hr == S_FALSE)
					CheckHr(vqvps[ittp]->get_IntProperty(ktptLeadingIndent, &nOld));

				qtpb = NULL;
				if (vqttp[ittp])
					CheckHr(vqttp[ittp]->GetBldr(&qtpb));
				else
					qtpb.CreateInstance(CLSID_TsPropsBldr);

				// Force the new value for indentation to be in the range
				// [FmtParaDlg::kthinIndMin, FmtParaDlg::kthinIndMax].
				int nNew = nOld + var1;
				if (nNew < FmtParaDlg::kthinIndMin)
					nNew = FmtParaDlg::kthinIndMin;
				else if (nNew > FmtParaDlg::kthinIndMax)
					nNew = FmtParaDlg::kthinIndMax;

				// Set ktptLeadingIndent to the calculated indentation.
				CheckHr(qtpb->SetIntPropValues(ktptLeadingIndent, ktpvMilliPoint, nNew));
				// If any change, props builder has new values. Update the vector, vqttp.
				if (qtpb)
					CheckHr(qtpb->GetTextProps(&vqttp[ittp]));
			}
			break;
		} // End of block.
#ifdef JT_7_17_01_ButtonsAreStyles
	// Reinstate this (or make it conditional on some flag) to interpret buttons
	// as named styles.
	case knFormatBulletList: // Bullet toolbar button
		{
			StrApp staEmpty;
			bool fOn = GetParaFormatting(kcidFmttbLstBullet, staEmpty);
			if (fOn)
				stuNewStyle = g_pszwStyleNormal; // turning off
			else
				stuNewStyle = pwszBullList; // turning on
			bstrNewVal = stuNewStyle.Bstr();
			format = knFormatStyle;
			goto LRepeat;
		}

	case knFormatNumListButton: // Numbered list button
		{
			StrApp staEmpty;
			bool fOn = GetParaFormatting(kcidFmttbLstNum, staEmpty);
			if (fOn)
				stuNewStyle = g_pszwStyleNormal; // turning off
			else
				stuNewStyle = pwszNumList; // turning on
			bstrNewVal = stuNewStyle.Bstr();
			format = knFormatStyle;
			goto LRepeat;
		}
#endif
	case knFormatBulletList: // Bullet toolbar button
	case knFormatNumListButton: // Numbered list button
		// Set ktptLeadingIndent to 0.25 in more, and ktptFirstLineIndent to 0.25 in less,
		// and ktptBulNumScheme to kvbnBulletBase + 1 (the default bullet in the dialog)
		// if bulleted, or kvbnArabic.
		{ // BLOCK.
			int cttp = vqttp.Size();
			if (!cttp)
				break; // nothing to change.
			ITsTextProps * pttpFirst = vqttp[0];
			// Read current values into these, then adjust
			int dxmpFirstIndent = 0;
			int dxmpStdHanging = - kdzmpInch / 4;

			int nVar = 0;
			int nValBn = 0; // Anything outside range below OK.
			if (pttpFirst)
				CheckHr(pttpFirst->GetIntPropValues(ktptBulNumScheme, &nVar, &nValBn));
			if (format == knFormatBulletList && nValBn < kvbnBulletBase || nValBn >
				kvbnBulletMax)
			{
				// We want bullets and don't have them.
				nValBn = kvbnBulletBase + 1; //(the default bullet in the dialog)
				dxmpFirstIndent = dxmpStdHanging;
			}
			else if (format == knFormatNumListButton && nValBn != kvbnArabic)
			{
				// Want numbered and don't have it
				nValBn = kvbnArabic;
				dxmpFirstIndent = dxmpStdHanging;
			}
			else
			{
				// Have one of them already, turn off
				nValBn = -1;
				dxmpFirstIndent = -1;
			}
			// Now work through our ttps and fix any of the three props that are wrong.
			for (int ittp = 0; ittp < cttp; ++ittp)
			{
				int nValBnOld = -1;
				int dxmpFirstIndOld = -1;
				HRESULT hr = S_FALSE;
				// Get the current value for ktptLeadingIndent.
				bool fWrongVar = false;
				if (vqttp[ittp])
				{
					CheckHr(hr = vqttp[ittp]->GetIntPropValues(ktptBulNumScheme,
						&nVar, &nValBnOld));
					fWrongVar = nValBn == -1 ? (nVar == -1) : (nVar == ktpvEnum);
					CheckHr(hr = vqttp[ittp]->GetIntPropValues(ktptFirstIndent,
						&nVar, &dxmpFirstIndOld));
					//fWrongVar |= (dxmpFirstIndent == -1) ? (nVar == -1) :
					//	(nVar == ktpvMilliPoint);
				}
				// old logic:
				//if (fWrongVar || nValBnOld != nValBn || dxmpFirstIndOld != dxmpFirstIndent)

				ITsPropsBldrPtr qtpb;
				if (vqttp[ittp])
					CheckHr(vqttp[ittp]->GetBldr(&qtpb));
				else
					qtpb.CreateInstance(CLSID_TsPropsBldr);
				if (nValBnOld != nValBn || fWrongVar)
				{
					CheckHr(qtpb->SetIntPropValues(ktptBulNumScheme,
						(nValBn == -1 ? -1 : ktpvEnum),
						nValBn));
				}
				// If we're adding a bullet or number, and there is no first-line indent
				// specified, add the standard hanging indent. If we're taking the bullet/number
				// away, and we have the standard hanging indent, clear it. If the indent is
				// non-standard, leave it.
				if ((nValBn == -1 && dxmpFirstIndOld == dxmpStdHanging) ||
					(nValBn != -1 && (dxmpFirstIndOld == 0 || dxmpFirstIndOld == -1)))
				{
					CheckHr(qtpb->SetIntPropValues(ktptFirstIndent,
						(dxmpFirstIndent == -1 ? -1 : ktpvMilliPoint),
						dxmpFirstIndent));
				}
				CheckHr(qtpb->GetTextProps(&vqttp[ittp]));

			}
			break;
		} // End of block.

	case knFormatBorderButton:// Border button
		{
			HandleBorderButton(vqttp);
		}
		break;

	default:
		Assert(false);	// We should never reach this.
	}

	// Narrow the range of TsTextProps to only include those that are not NULL.
	int ihvoFirstMod = -1;
	int ihvoLastMod = -1;
	for (int ihvo = ihvoFirst; ihvo <= ihvoLast; ihvo++)
	{
		ITsTextPropsPtr qttp;
		qttp = vqttp[ihvo - ihvoFirst];
		IVwPropertyStorePtr qvpsSoft = vqvpsSoft[ihvo - ihvoFirst];
		if (qttp)
		{
			// If we set a style for a paragraph at all, it must specify a named style.
			// The "default Normal" mechanism (see StVc.cpp) only works for paragraphs
			// which lack a style altogether. Any actual style must specify "Normal" unless
			// it specifies something else.
			SmartBstr sbstrNamedStyle;
			CheckHr(qttp->GetStrPropValue(kspNamedStyle, &sbstrNamedStyle));
			if (!sbstrNamedStyle.Length())
			{
				ITsPropsBldrPtr qtpb;
				CheckHr(qttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetStrPropValue(kspNamedStyle, SmartBstr(g_pszwStyleNormal)));
				CheckHr(qtpb->GetTextProps(&qttp));
			}

			ihvoLastMod = ihvo;
			if (ihvoFirstMod < 0)
				ihvoFirstMod = ihvo;
			HVO hvoPara;
			CheckHr(qsda->get_VecItem(hvoText, tagText, ihvo, &hvoPara));

			ITsTextPropsPtr qttpRet;
			if (RemoveRedundantHardFormatting(qsda, qvpsSoft, qttp,
				(format == knFormatStyle), hvoPara, &qttpRet))
			{
				qttp = qttpRet;
			}
			qsda->SetUnknown(hvoPara, kflidStPara_StyleRules, qttp);
		}
	}

	if (ihvoFirstMod < 0)
		return true; // There are no paragraph properties changes.

	// If we modified anything, force redraw by faking a property change.
	// This will destroy the selection, so first, save it.
	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
	VwSelLevInfo * prgvsli;

	try
	{
		prgvsli = NewObj VwSelLevInfo[cvsli];
		int ihvoRoot;
		PropTag tagTextProp;
		int cpropPrevious;
		int ichAnchor;
		int ichEnd;
		int ws;
		ComBool fAssocPrev;
		int ihvoEnd;

		// Save the view selection level information by calling AllTextSelInfo on the
		// selection.
		CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, cvsli, prgvsli, &tagTextProp, &cpropPrevious,
			&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

		// Broadcast the fake property change to all roots, by calling PropChanged on the
		// SilDataAccess pointer. Pretend we deleted and re-inserted the changed items.
		int chvoChanged = ihvoLastMod - ihvoFirstMod + 1;
		qsda->PropChanged(m_qrootb, kpctNotifyMeThenAll, hvoText,
			tagText, ihvoFirstMod, chvoChanged, chvoChanged);

		// Now restore the selection by a call to MakeTextSelection on the RootBox pointer.
		// DO NOT CheckHr. This may legitimately fail, e.g., if there is no editable field.
		// REVIEW JohnT: Should we try again, e.g., to make a non-editable one?
		m_qrootb->MakeTextSelection(ihvoRoot, cvsli, prgvsli, tagTextProp, cpropPrevious,
			ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, NULL, true, NULL);

		// And clean up.
		if (prgvsli)
			delete[] prgvsli;
	}
	catch (...)
	{
		// Properly delete the array of VwSelLevInfo.
		if (prgvsli)
			delete prgvsli; // REVIEW (BobA/EberhardB): Shouldn't this be delete[]?
		throw;
	}
	return true;
} // AfVwRootSite::FormatParas.


/*----------------------------------------------------------------------------------------------
	Bring up the format styles dialog.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdFmtStyles1(Cmd * pcmd)
{
	AssertObj(pcmd);

	IVwSelectionPtr qvwsel;
	ISilDataAccessPtr qsda;
	VwPropsVec vqvpsPara;
	TtpVec vqttpPara;
	HVO hvoText;
	int tagText;
	int ihvoFirst;
	int ihvoLast;

	GetParagraphProps(&qvwsel, hvoText, tagText, vqvpsPara, ihvoFirst, ihvoLast, &qsda,
			vqttpPara);

	TtpVec vqttpChar;
	VwPropsVec vqvpsChar;
	GetCharacterProps(&qvwsel, vqttpChar, vqvpsChar);

	IVwStylesheetPtr qasts;
	CheckHr(m_qrootb->get_Stylesheet(&qasts));
	if (!qasts)
	{
		// Create a style sheet somehow!  But how is one ever created?
		AfStylesheet * pasts = NULL;
		if (Window() && Window()->MainWindow())
		{
			pasts = Window()->MainWindow()->GetStylesheet();
			if (pasts)
				pasts->QueryInterface(IID_IVwStylesheet, (void **)&qasts);
		}
	}
	BeginUndoTask(pcmd->m_cid, &qsda);

	// Variables set by AdjustTsTextProps.
	StrUni stuStyleToApply = L""; // If AdjustTsTextProps sets this, apply it to the selection.
	bool fDefnChanged = false;	// If AdjustTsTextProps makes this true, ask the main windows
								// to redraw.
	ComBool fCanFormatChar = false;
	if (SelectionInOneField() && qvwsel)
		qvwsel->get_CanFormatChar(&fCanFormatChar);
	bool fApply;
	bool fReloadDb;
	if (!OpenFormatStylesDialog(Window()->Hwnd(), m_fCanDoRtl, OuterRightToLeft(), qasts,
			vqttpPara, vqttpChar, fCanFormatChar,
			&stuStyleToApply, fDefnChanged, fApply, fReloadDb))
	{
		EndUndoTask(qsda);
		return true; // If OpenFormatStylesDialog returns false, nothing changed.
	}

	if (fReloadDb)
	{
		// Styles were renamed or deleted. Reload data from the database, if this is the
		// kind of app that needs to do that.
		if (AfApp::Papp() && AfApp::Papp()->OnStyleNameChange(qasts, qsda))
		{
			if (fApply)
			{
				// TODO (SharonC): Remove this message when the reload routine can
				// successfully replace the selection.
				::MessageBox(0,
					_T("Sorry, your selection has been lost, due to the complications of ")
					_T("renaming your styles.\nPlease reapply your style change."),
					_T(""),	MB_OK | MB_ICONEXCLAMATION);
			}
			return true;
		}
		// else call OnStylesheetChange below
	}

	// Has the user picked a different style for the selection?
	if (fApply)
	{
		// Create a new undo-task corresponding to the applying of the style.
		if (qsda)
		{
			int stid = UndoRedoLabelRes(kcidFmttbStyle);
			StrUni stuUndo, stuRedo;
			StrUtil::MakeUndoRedoLabels(stid, &stuUndo, &stuRedo);
			CheckHr(qsda->BreakUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
		}

		int stType;
		if (stuStyleToApply.Length() == 0)
			stType = kstCharacter;
		else
			CheckHr(qasts->GetType(stuStyleToApply.Bstr(), &stType));

		switch (stType)
		{
		default:
			Assert(false); // This should not happen.
			break;

		case kstParagraph:
			// Set the style name of the paragraph(s) to stuStyleName and redraw.
			FormatParas(knFormatStyle, m_fCanDoRtl, OuterRightToLeft(), 0,
				stuStyleToApply.Bstr());
			break;

		case kstCharacter:
			RemoveCharFormatting(qvwsel, vqttpChar, stuStyleToApply.Bstr());
			break;
		}
	}

	// Has the user modified any of the styles?
	if (fDefnChanged || fReloadDb)
	{
		AfDbApp * papp = dynamic_cast<AfDbApp *>(AfApp::Papp());
		if (papp)
		{
			// Update all interested parties using database synchronization.
			AssertPtr(papp);
#ifdef DEBUG
	// We probably want to change all occurrences of GetCurMainWnd() to MainWindow()
	// in this file, but we didn't take the time to verify this would always work. These DEBUG
	// asserts in this file are a temporary test to see if there is ever any difference between
	// the two. If one is hit, it's likely that GetCurMainWnd is returning the wrong value. If
	// this doesn't cause any problem after some time, we can go ahead and switch them all
	// to MainWindow.
	AfMainWnd * pafw1 = AfApp::Papp()->GetCurMainWnd();
	AfMainWnd * pafw2 = m_pwndSubclass->MainWindow();
	Assert(pafw1 == pafw2);
#endif
			RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(papp->GetCurMainWnd());
			AssertPtr(pwnd);
			SyncInfo sync(ksyncStyle, 0, 0);
			pwnd->GetLpInfo()->StoreAndSync(sync);
		}
		else
		{
			// Ask all the views to redraw based on the modified stylesheet (non-db apps)
			Vector<AfMainWndPtr> &vqafw = AfApp::Papp()->GetMainWindows();
			for (int i = 0; i < vqafw.Size(); i++)
				vqafw[i]->OnStylesheetChange();
		}
	}

	EndUndoTask(qsda);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Apply the normal style to the selection.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdApplyNormalStyle1(Cmd * pcmd)
{
	ISilDataAccessPtr qsda;
	BeginUndoTask(pcmd->m_cid, &qsda);

	SmartBstr sbstrNormal = g_pszwStyleNormal;
	FormatParas(AfVwRootSite::knFormatStyle, m_fCanDoRtl, OuterRightToLeft(), 0, sbstrNormal);

	EndUndoTask(qsda);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Check for presence of proper paragraph properties. Return false if neither
	selection nor paragraph property. Otherwise return true.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite:: IsParagraphProps(IVwSelection ** ppvwsel, HVO & hvoText, int & tagText,
	VwPropsVec &vqvps, int & ihvoAnchor, int & ihvoEnd)
{
	AssertPtr(ppvwsel);

	ComBool fOk;
	int clev; // Count of levels of view objects.
	int cpropPrevious;
	HVO hvoEnd;
	int tagEnd;
	int cpropPrevEnd;

	// Get the selection. Can't do command unless we have one.
	if (!m_qrootb)
		return false;
	CheckHr(m_qrootb->get_Selection(ppvwsel));
	if (!(*ppvwsel))
		return false;

	// Commit any outstanding edits.
	CheckHr((*ppvwsel)->Commit(&fOk));
	if (!fOk)
		return false;

	// Get selection info. We need a two-level or more selection.
	CheckHr((*ppvwsel)->CLevels(false, &clev)); // Anchor.
	if (clev < 2)
		return false;
	CheckHr((*ppvwsel)->CLevels(true, &clev)); // Endpoint.
	if (clev < 2)
		return false;

	IVwPropertyStorePtr qvps;
	// At this point, we know how to do this command only for structured text paragraphs.
	CheckHr((*ppvwsel)->PropInfo(false, 1, &hvoText, &tagText, &ihvoAnchor, &cpropPrevious,
		&qvps));
	// Make sure it's the right property.
	if (tagText != kflidStText_Paragraphs)
		return false;

	// And nothing bizarre about other values...
	if (cpropPrevious)
		return false;
	CheckHr((*ppvwsel)->PropInfo(true, 1, &hvoEnd, &tagEnd, &ihvoEnd, &cpropPrevEnd, &qvps));
	// Make sure it's the same property.
	if (tagEnd != tagText || hvoText != hvoEnd || cpropPrevious != cpropPrevEnd)
		return false;

	int cvps;
	CheckHr((*ppvwsel)->GetParaProps(0, NULL, &cvps));
	vqvps.Resize(cvps);
	CheckHr((*ppvwsel)->GetParaProps(cvps, (IVwPropertyStore **)vqvps.Begin(), &cvps));

	return true;
}
/*----------------------------------------------------------------------------------------------
	Get the view selection and paragraph properties. Return false if there is neither a
	selection nor a paragraph property. Otherwise return true.

	@param vqvps - cumulative effect of styles and formatting for each paragraph
	@param vqttp - paragraph styles for each paragraph
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::GetParagraphProps(IVwSelection ** ppvwsel, HVO & hvoText, int & tagText,
	VwPropsVec &vqvps, int & ihvoFirst, int & ihvoLast, ISilDataAccess ** ppsda,
	TtpVec & vqttp)
{
	AssertPtr(ppsda);

	int ihvoAnchor, ihvoEnd;

	if (!IsParagraphProps(ppvwsel, hvoText, tagText, vqvps, ihvoAnchor, ihvoEnd))
		return false;

	// OK, we're going to do it!
	ihvoFirst = ihvoAnchor;
	ihvoLast = ihvoEnd;
	if (ihvoFirst > ihvoLast)
	{
		ihvoFirst = ihvoLast;
		ihvoLast = ihvoAnchor;
	}
	CheckHr(m_qrootb->get_DataAccess(ppsda));
	if (!(*ppsda)) // Very unlikely, but it's a COM interface...
		return true;  // Finished handling the command, anyway.
	for (int ihvo = ihvoFirst; ihvo <= ihvoLast; ihvo++)
	{
		ITsTextPropsPtr qttp;
		HVO hvoPara;
		CheckHr((*ppsda)->get_VecItem(hvoText, tagText, ihvo, &hvoPara));
		IUnknownPtr qunkTtp;
		CheckHr((*ppsda)->get_UnknownProp(hvoPara, kflidStPara_StyleRules, &qunkTtp));
		if (qunkTtp)
		{
			CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttp));
			vqttp.Push(qttp);
		}
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the selection and character properties for a particular root box (this is a static
	method). Return false if there is no root box, or if it has no selection. Otherwise return
	true.

	Note that this method is only guaranteed to work for selections within a single field.
	Selections that span multiple fields will most likely ignore all but the first field.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::GetCharacterProps(IVwRootBox * prootb, IVwSelection ** ppvwsel,
	TtpVec & vqttp, VwPropsVec & vqvps)
{
	AssertPtrN(prootb);
	AssertPtr(ppvwsel);
	Assert(*ppvwsel == NULL);

	// Get the selection. Can't do command unless we have one.
	if (!prootb)
		return false;
	CheckHr(prootb->get_Selection(ppvwsel));
	if (!(*ppvwsel))
		return false;
	int cttp;
	CheckHr((*ppvwsel)->GetSelectionProps(0, NULL, NULL, &cttp));
	if (!cttp)
		// No text selected.
		return false;
	vqttp.Resize(cttp);
	vqvps.Resize(cttp);
	CheckHr((*ppvwsel)->GetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin(),
		(IVwPropertyStore **)vqvps.Begin(), &cttp));

	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the view selection and character properties. Return false if there is neither a
	selection nor any text selected. Otherwise return true.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::GetCharacterProps(IVwSelection ** ppvwsel, TtpVec & vqttp,
	VwPropsVec & vqvps)
{
	return GetCharacterProps(m_qrootb, ppvwsel, vqttp, vqvps);
}

/*----------------------------------------------------------------------------------------------
	Handle the items in the Font/Format toolbar.

	Note (JohnT): I don't know who wrote the following note:
		NOTE: It is important that this method return false even if it handles the message so
		the behavior of the combo boxes on the toolbar is correct.
	But, it is essential that the method DOES return true if it handled the command. Otherwise,
	there can easily be two command handlers in the chain that both know how to do the command,
	and then it gets done twice...which makes two entries in the Undo stack. As far as I can
	tell, it doesn't cause a problem for the combo boxes if this method returns true, so I'm
	guessing the comment above is obsolete; but I'll leave it in in case it some day provides
	a clue to some other problem.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdCharFmt1(Cmd * pcmd)
{
	AssertObj(pcmd);

	IVwSelectionPtr qvwsel;
	TtpVec vqttp;
	VwPropsVec vqvps;

	if (!SelectionInOneField())
		return false;
	if (!GetCharacterProps(&qvwsel, vqttp, vqvps))
		return false;

	// If the command originated from a keyboard accelerator, then no check will have yet been
	// done to determine if the text is editable:
	if (pcmd->m_rgn[0] == 1)
	{
		// Command did originate from an accelerator, so see if text is fully editable:
		ComBool fCanFormat;
		CheckHr(qvwsel->get_CanFormatChar(&fCanFormat));
		if (!(bool)fCanFormat)
			return true;
	}

	// Note: These must be on separate lines or remove this test. Otherwise release builds fail.
	int cttp;
	cttp = vqttp.Size();
	Assert(cttp == vqvps.Size());

	HWND hwnd = (HWND)pcmd->m_rgn[1];
	switch (pcmd->m_cid)
	{
	case kcidFmttbFocusStyleBox:
		{
			// We're about to give the focus to the style combo box.
			// To locate this box, first find the frame window:
			AfMainWnd * pafw = m_pwndSubclass->MainWindow();
			if (pafw)
			{
				// Now find the formatting toolbar:
				AfToolBar * ptlbr = pafw->GetToolBar(kridTBarFmtg);
				if (ptlbr && ::IsWindowVisible(ptlbr->Hwnd()))
				{
					// Now get the handle to the style combo box:
					HWND hwndStyleComboBox = ::GetDlgItem(ptlbr->Hwnd(), kcidFmttbStyle);
					if (hwndStyleComboBox)
					{
						// Prepare for it by updating the item list.
						pafw->OnStyleDropDown(hwndStyleComboBox);
						::SetFocus(hwndStyleComboBox);
					}
				}
				else
				{
					// If we don't have this toolbar or aren't using it launch the style dialog.
					return CmdFmtStyles1(pcmd);
				}
			}
		}
		return true;
	case kcidFmttbStyle:
	case kcidFmttbWrtgSys:
	case kcidFmttbFnt:
	case kcidFmttbFntSize:
		if (pcmd->m_rgn[0] != CBN_SELENDOK)
			return false;
		// Fall through.

	case kcidFmttbBold:
	case kcidFmttbItal:
	case kcidFmttbApplyBgrndColor:
	case kcidFmttbApplyFgrndColor:
	case kcidFmttbAlignLeft:
	case kcidFmttbAlignCntr:
	case kcidFmttbAlignRight:
	case kcidFmttbLstNum:
	case kcidFmttbLstBullet:
	case kcidFmttbUnind:
	case kcidFmttbInd:
	case kcidFmttbApplyBdr:
		{
			TssComboEx * ptce = dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwnd));
			AssertPtrN(ptce);
			if (ptce)
			{
				if (::GetFocus() == ptce->GetEditControl())
				{
					// This only happens when the combo box is not open and the user hits an
					// arrow key, causing the selection to change. Since we don't want
					// to actually perfom the change, return here.
					return false;
				}

				ITsStringPtr qtss;
				int isel = ptce->GetCurSel();
				ptce->GetLBText(isel, &qtss);
				AssertPtr(qtss);
				ptce->GetLBText(ptce->GetCurSel(), &qtss);

				AssertPtr(qtss);

				const OLECHAR * pwrgch = NULL;
				int cch;
				StrApp str;
				HRESULT hr;
				IgnoreHr(hr = qtss->LockText(&pwrgch, &cch));
				if (FAILED(hr))
					return false;
				str.Assign(pwrgch, cch);
				qtss->UnlockText(pwrgch);

				::SetWindowText(ptce->Hwnd(), str.Chars());
				// This next line forces the dropdown list to close. Without it, sometimes the
				// focus is not set properly to the view window.
				// One way I was able to consistently see the problem was to hit the Alt+Down
				// keys in a combobox to show the list, hit the Alt+Up keys to close the list,
				// hit the Alt+Down keys again to reopen it, then hit the Enter key to select
				// the current item in the list. Without the next line, the combobox retains
				// the focus.
				::SendMessage(hwnd, CB_SHOWDROPDOWN, false, 0);
				SetFocusToRootSite();
			}

			ApplyFormatting(pcmd->m_cid, hwnd);
			return true; // We did it, don't look further for a command handler.
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle the Edit / Cut menu commmand.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdEditCut1(Cmd * pcmd)
{
	AssertObj(pcmd);
	// Do nothing if command is not enabled. Needed for Ctrl-X keypress.
	if (!CanCut())
		return false;

	WaitCursor wc; // creates a wait cursor and makes it active until the end of the method.

	// First, try to copy the range of text to the clipboard.
	// Get selection. Can't do command unless we have one.
	if (!m_qrootb)
		return false;
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
		return false;
	int cttp;
	CheckHr(qvwsel->GetSelectionProps(0, NULL, NULL, &cttp));
	// No text selected.
	if (!cttp)
		return false;
	ComBool fRange;
	CheckHr(qvwsel->get_IsRange(&fRange));
	if (!fRange)
		return false;

	// Get a copy of the selection as a TsString, and store it in the clipboard, together with
	// the writing system factory.
	ITsStringPtr qtss;
	SmartBstr sbstr = L"; ";
	CheckHr(qvwsel->GetSelectionString(&qtss, sbstr));
	ISilDataAccessPtr qsdaT;
	CheckHr(m_qrootb->get_DataAccess(&qsdaT));
	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));
	ILgTsStringPlusWssPtr qtsswss;
	qtsswss.CreateInstance(CLSID_LgTsStringPlusWss);
	CheckHr(qtsswss->putref_String(qwsf, qtss));
	ILgTsDataObjectPtr qtsdo;
	qtsdo.CreateInstance(CLSID_LgTsDataObject);
	CheckHr(qtsdo->Init(qtsswss));
	IDataObjectPtr qdobj;
	CheckHr(qtsdo->QueryInterface(IID_IDataObject, (void **)&qdobj));
	if (::OleSetClipboard(qdobj) == S_OK)
	{
		ModuleEntry::SetClipboard(qdobj);
		// The copy succeeded, now delete the range of text that has been copied to the
		// clipboard.
		// REVIEW JohnT(SteveMc): is this deletion undoable?  if not, what else needs to be
		// done?
		ISilDataAccessPtr qsda;
		BeginUndoTask(pcmd->m_cid, &qsda);
		ITsStrFactoryPtr qtsf;
		ITsStringPtr qtss;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		int wsUser;
		CheckHr(qwsf->get_UserWs(&wsUser));
		// Replace with empty string == delete.
		CheckHr(qtsf->MakeString(NULL, wsUser, &qtss));
		SelPositionInfo spi(OtherPane());
		CheckHr(qvwsel->ReplaceWithTsString(qtss));
		spi.Restore();
		EndUndoTask(qsda);
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the Edit / Copy menu commmand.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdEditCopy1(Cmd * pcmd)
{
	AssertObj(pcmd);

	WaitCursor wc; // creates a wait cursor and makes it active until the end of the method.

	// Get selection. Can't do command unless we have one.
	if (!m_qrootb)
		return false;
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
		return false;
	int cttp;
	CheckHr(qvwsel->GetSelectionProps(0, NULL, NULL, &cttp));
	// No text selected.
	if (!cttp)
		return false;
	ComBool fRange;
	CheckHr(qvwsel->get_IsRange(&fRange));
	if (!fRange)
		return false;

	// Get a copy of the selection as a TsString, and store it in the clipboard.
	ITsStringPtr qtss;
	SmartBstr sbstr = L"; ";
	CheckHr(qvwsel->GetSelectionString(&qtss, sbstr));
	ISilDataAccessPtr qsdaT;
	CheckHr(m_qrootb->get_DataAccess(&qsdaT));
	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));
	ILgTsStringPlusWssPtr qtsswss;
	qtsswss.CreateInstance(CLSID_LgTsStringPlusWss);
	CheckHr(qtsswss->putref_String(qwsf, qtss));
	ILgTsDataObjectPtr qtsdo;
	qtsdo.CreateInstance(CLSID_LgTsDataObject);
	CheckHr(qtsdo->Init(qtsswss));
	IDataObjectPtr qdobj;
	CheckHr(qtsdo->QueryInterface(IID_IDataObject, (void **)&qdobj));
	if (::OleSetClipboard(qdobj) == S_OK)
		ModuleEntry::SetClipboard(qdobj);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the Edit / Paste menu commmand.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdEditPaste1(Cmd * pcmd)
{
	AssertObj(pcmd);

	// Do nothing if command is not enabled. Needed for Ctrl-V keypress.
	if (!CanPaste())
		return false;

	WaitCursor wc; // creates a wait cursor and makes it active until the end of the method.

	// Get selection. Can't do command unless we have one.
	if (!m_qrootb)
		return false;
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
		return false;
	int cttp;
	CheckHr(qvwsel->GetSelectionProps(0, NULL, NULL, &cttp));
	// No editable text selection.
	if (!cttp)
		return false;
	TtpVec vqttp;
	VwPropsVec vqvps;
	// OPTIMIZE: we want only the first property, if only there was a way to ask for just that.
	vqttp.Resize(cttp);
	vqvps.Resize(cttp);
	CheckHr(qvwsel->GetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin(),
		(IVwPropertyStore **)vqvps.Begin(), &cttp));

	ComBool fCanFormat;
	CheckHr(qvwsel->get_CanFormatChar(&fCanFormat));

	// Get the code page associated with the writing system of the selection.
	ITsTextProps * pttpSel = vqttp[0];
	int ws, nVar;
	pttpSel->GetIntPropValues(ktptWs, &nVar, &ws);
	int nCodePage = CP_ACP;

	// Get the writing system factory associated with the root box.
	ISilDataAccessPtr qsdaT;
	CheckHr(m_qrootb->get_DataAccess(&qsdaT));
	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));

	IWritingSystemPtr qws;
	CheckHr(qwsf->get_EngineOrNull(ws, &qws));
	if (qws)
	{
		int nLocaleID;
		CheckHr(qws->get_Locale(&nLocaleID));
		achar rgch[32];
		memset(rgch, 0, 32 * isizeof(achar));
		::GetLocaleInfo(nLocaleID, LOCALE_IDEFAULTANSICODEPAGE, rgch, 32);
		nCodePage = _tstoi(rgch);
	}

	// Get the data currently stored on the clipboard.

	ITsStringPtr qtss;
	IDataObjectPtr qdobj;
	CheckHr(::OleGetClipboard(&qdobj));

	// Begin a Paste task for undoing. First, commit any other changes.
	ComBool fOk;
	CheckHr(qvwsel->Commit(&fOk));
	if (!fOk)
		return false;
	ISilDataAccessPtr qsda;
	BeginUndoTask(pcmd->m_cid, &qsda);

	// If the clipboard is storing a serialized TsString, deserialize it and paste it in.
	HRESULT hr;
	FORMATETC format;
	STGMEDIUM medium;
	uint uFormat;
	{
		ILgTsDataObjectPtr qtsdo;
		qtsdo.CreateInstance(CLSID_LgTsDataObject);
		qtsdo->GetClipboardType(&uFormat);
	}
	format.cfFormat = static_cast<unsigned short>(uFormat);
	format.ptd = NULL;
	format.dwAspect = DVASPECT_CONTENT;
	format.lindex = -1;
	format.tymed = TYMED_ISTORAGE;
	hr = qdobj->GetData(&format, &medium);		// (split from if () to ease debugging.)
	if (hr == S_OK)
	{
		if (medium.tymed == TYMED_ISTORAGE && medium.pstg)
		{
			ILgTsStringPlusWssPtr qtssencs;
			qtssencs.CreateInstance(CLSID_LgTsStringPlusWss);
			CheckHr(qtssencs->Deserialize(medium.pstg));
			// Normalize the string to NFD for our use.
			ITsStringPtr qtssOrig;
			CheckHr(qtssencs->get_String(qwsf, &qtssOrig));
			CheckHr(qtssOrig->get_NormalizedForm(knmNFD, &qtss));
			//+ Begin fix for Raid bug 897B
			// Check for an embedded picture.
			int crun;
			CheckHr(qtss->get_RunCount(&crun));
			bool fHasPicture = false;
			int irun;
			ITsTextPropsPtr qttp;
			SmartBstr sbstr;
			HRESULT hr;
			for (irun = 0; irun < crun; ++irun)
			{
				CheckHr(qtss->get_Properties(irun, &qttp));
				CheckHr(hr = qttp->GetStrPropValue(kstpObjData, &sbstr));
				if (hr == S_OK)
				{
					// We have an embedded object.  Is it a picture or a link?
					wchar chType = *sbstr;
					if (chType == kodtPictOdd || chType == kodtPictEven)
					{
						fHasPicture = true;
						break;
					}
				}
			}
			if (fHasPicture)
			{
				// Vars to call TextSelInfo and find out whether it is a structured text field.
				ITsStringPtr qtssDummy;
				int ich;
				ComBool fAssocPrev;
				HVO hvoObj;
				PropTag tag;
				int ws;
				CheckHr(qvwsel->TextSelInfo(false, &qtssDummy, &ich, &fAssocPrev, &hvoObj, &tag,
					&ws));
				if (tag != kflidStTxtPara_Contents)
				{
					::MessageBox(Window()->Hwnd(),
						_T("Sorry, for now strings containing pictures can be pasted\n")
						_T("only in multiple-paragraph fields like 'Description'"),
						NULL, MB_OK);
					goto LDone;
				}
			}
			//- End fix for Raid bug 897B
			if (!fCanFormat)
			{
				// remove formatting from the TsString, except for writing system.
				SmartBstr sbstr;
				CheckHr(qtss->get_Text(&sbstr));
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				int wsT;
				int nVar;
				ITsTextPropsPtr qttp;
				CheckHr(qtss->get_PropertiesAt(0, &qttp));
				CheckHr(qttp->GetIntPropValues(ktptWs, &nVar, &wsT));
				if (!wsT || wsT == -1)
				{
					ISilDataAccessPtr qsdaT;
					CheckHr(m_qrootb->get_DataAccess(&qsdaT));
					ILgWritingSystemFactoryPtr qwsf;
					CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));
					CheckHr(qwsf->get_UserWs(&wsT));
				}
				qtsf->MakeStringRgch(sbstr.Chars(), sbstr.Length(), wsT, &qtss);
			}

			// We will make a selection near the top left of the other pane if there is one,
			// and try to keep its position fixed.
			SelPositionInfo spi(OtherPane());
			CheckHr(qvwsel->ReplaceWithTsString(qtss));
			spi.Restore();
			// Update the formatting toolbar's writing system combobox control just in case
			// we've added new writing systems in the paste.
			AfMainWnd * pafw = m_pwndSubclass->MainWindow();
			if (pafw)
				pafw->UpdateToolBarWrtSysControl();
		}
		ReleaseStgMedium(&medium);
		goto LDone;
	}

	// otherwise, if there is a UNICODE string stored in the clipboard, paste it in.
	format.cfFormat = CF_UNICODETEXT;
	format.ptd = NULL;
	format.dwAspect = DVASPECT_CONTENT;
	format.lindex = -1;
	format.tymed = TYMED_HGLOBAL;
	hr = qdobj->GetData(&format, &medium);
	if (hr == S_OK)
	{
		if (medium.tymed == TYMED_HGLOBAL && medium.hGlobal)
		{
			// Convert the global memory string to a TsString without any formatting.
			const wchar * pwszClip = (const wchar *)::GlobalLock(medium.hGlobal);
			StrUni stu(pwszClip);
			::GlobalUnlock(medium.hGlobal);
			// Normalize the string to NFD for our use.
			StrUtil::NormalizeStrUni(stu, UNORM_NFD);
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			CheckHr(qtsf->MakeStringWithPropsRgch(const_cast<wchar *>(stu.Chars()),
				stu.Length(), pttpSel, &qtss));
			SelPositionInfo spi(OtherPane());
			CheckHr(qvwsel->ReplaceWithTsString(qtss));
			spi.Restore();
		}
		ReleaseStgMedium(&medium);
		goto LDone;
	}

	// otherwise, if there is a text string stored in the clipboard, paste it in.
	format.cfFormat = CF_OEMTEXT;
	format.ptd = NULL;
	format.dwAspect = DVASPECT_CONTENT;
	format.lindex = -1;
	format.tymed = TYMED_HGLOBAL;
	hr = qdobj->GetData(&format, &medium);
	if (hr == S_OK)
	{
		if (medium.tymed == TYMED_HGLOBAL && medium.hGlobal)
		{
			// Convert the global memory string to a TsString without any formatting.
			// REVIEW SteveMc: How does CF_OEMTEXT differ from CF_TEXT?
			const char * pszClip = (const char *)::GlobalLock(medium.hGlobal);
			StrAnsi sta(pszClip);
			::GlobalUnlock(medium.hGlobal);
			StrUni stuToPaste;
			StrUni::AssignViaCodePage(sta, stuToPaste, nCodePage);
			// Normalize the string to NFD for our use.
			StrUtil::NormalizeStrUni(stuToPaste, UNORM_NFD);
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			CheckHr(qtsf->MakeStringWithPropsRgch(const_cast<wchar *>(stuToPaste.Chars()),
				stuToPaste.Length(), pttpSel, &qtss));
			SelPositionInfo spi(OtherPane());
			CheckHr(qvwsel->ReplaceWithTsString(qtss));
			spi.Restore();
		}
		ReleaseStgMedium(&medium);
		goto LDone;
	}
	format.cfFormat = CF_TEXT;
	format.ptd = NULL;
	format.dwAspect = DVASPECT_CONTENT;
	format.lindex = -1;
	format.tymed = TYMED_HGLOBAL;
	hr = qdobj->GetData(&format, &medium);
	if (hr == S_OK)
	{
		if (medium.tymed == TYMED_HGLOBAL && medium.hGlobal)
		{
			// Convert the global memory string to a TsString without any formatting.
			const char * pszClip = (const char *)::GlobalLock(medium.hGlobal);
			StrAnsi sta(pszClip);
			::GlobalUnlock(medium.hGlobal);
			StrUni stuToPaste;
			StrUni::AssignViaCodePage(sta, stuToPaste, nCodePage);
			// Normalize the string to NFD for our use.
			StrUtil::NormalizeStrUni(stuToPaste, UNORM_NFD);
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			CheckHr(qtsf->MakeStringWithPropsRgch(const_cast<wchar *>(stuToPaste.Chars()),
				stuToPaste.Length(), pttpSel, &qtss));
			SelPositionInfo spi(OtherPane());
			CheckHr(qvwsel->ReplaceWithTsString(qtss));
			spi.Restore();
		}
		ReleaseStgMedium(&medium);
		goto LDone;
	}

	// Nothing we can paste.  Oh well...

LDone:
	CheckHr(qvwsel->Commit(&fOk)); // Nothing sensible to do if not Ok...
	EndUndoTask(qsda);
	ScrollSelectionIntoView(NULL, kssoDefault);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the Edit / Delete menu commmand.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdEditDel1(Cmd * pcmd)
{
	AssertObj(pcmd);

	WaitCursor wc; // creates a wait cursor and makes it active until the end of the method.

	// Get selection. Can't do command unless we have one.
	if (!m_qrootb)
		return false;
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
		return false;
	int cttp;
	CheckHr(qvwsel->GetSelectionProps(0, NULL, NULL, &cttp));
	// No text selected.
	if (!cttp)
		return false;

	// REVIEW JohnT(SteveMc): is this deletion undoable?  if not, what else needs to be done?

	// Begin a Delete task for undoing.
	ISilDataAccessPtr qsda;
	BeginUndoTask(pcmd->m_cid, &qsda);

	ITsStrFactoryPtr qtsf;
	ITsStringPtr qtss;
	qtsf.CreateInstance(CLSID_TsStrFactory);

	// Get a writing system to use for the empty string.
	int wsT = GetSelectionWs();
	CheckHr(qtsf->MakeString(NULL, wsT, &qtss));	// Replace with empty string == delete.
	SelPositionInfo spi(OtherPane());
	CheckHr(qvwsel->ReplaceWithTsString(qtss));
	spi.Restore();

	EndUndoTask(qsda);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the Edit / Select All menu commmand.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdEditSelAll1(Cmd * pcmd)
{
	SelectAll();
	return true;
}

/*----------------------------------------------------------------------------------------------
	Enable/Disable Edit / Cut menu Command.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmsEditCut1(CmdState & cms)
{
	Assert(cms.Cid() == kcidEditCut);
	cms.Enable(CanCut());
	return true;		// Indicates we have handled it.
}

bool AfVwRootSite::CanCut()
{
	bool fEnable = false;
	if (m_qrootb)
	{
		IVwSelectionPtr qvwsel;
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (qvwsel)
		{
			ComBool fRange;
			CheckHr(qvwsel->get_IsRange(&fRange));
			fEnable = fRange ? true : false;
		}
	}
	return fEnable;
}

/*----------------------------------------------------------------------------------------------
	Enable/Disable Edit / Copy menu Command.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmsEditCopy1(CmdState & cms)
{
	Assert(cms.Cid() == kcidEditCopy);
	// Get selection. Can't do command unless we have a selected range.
	bool fEnable = false;
	if (m_qrootb)
	{
		IVwSelectionPtr qvwsel;
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (qvwsel)
		{
			ComBool fRange;
			CheckHr(qvwsel->get_IsRange(&fRange));
			fEnable = fRange ? true : false;
		}
	}
	cms.Enable(fEnable);
	return true;		// Indicates we have handled it.
}

/*----------------------------------------------------------------------------------------------
	Enable/Disable Edit / Paste menu Command.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmsEditPaste1(CmdState & cms)
{
	Assert(cms.Cid() == kcidEditPaste);
	// Get selection. Can't do command unless we have one, either a range or an insertion point.
	cms.Enable(CanPaste());
	return true; // we handled it
}

bool AfVwRootSite::CanPaste()
{
	bool fEnable = false;
	if (m_qrootb)
	{
		IVwSelectionPtr qvwsel;
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (qvwsel)
		{
			// Get the type of object on the clipboard, and check whether it is compatible
			// with being pasted into text.
			IDataObjectPtr qdobj;
			if (::OleGetClipboard(&qdobj) == S_OK)
			{
				FORMATETC format = { CF_TEXT, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };
				if (qdobj->QueryGetData(&format) == S_OK)
					fEnable = true;
			}
		}
	}
	return fEnable;
}

/*----------------------------------------------------------------------------------------------
	Enable/Disable Edit / Delete menu Command.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmsEditDel1(CmdState & cms)
{
	Assert(cms.Cid() == kcidEditDel);
	// Get selection. Can't do command unless we have a selected range.
	bool fEnable = false;
	if (m_qrootb)
	{
		IVwSelectionPtr qvwsel;
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (qvwsel)
		{
			ComBool fRange;
			CheckHr(qvwsel->get_IsRange(&fRange));
			fEnable = fRange ? true : false;
		}
	}
	cms.Enable(fEnable);
	return true;		// Indicates we have handled it.
}

/*----------------------------------------------------------------------------------------------
	Enable/Disable Edit / Select All menu Command.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmsEditSelAll1(CmdState & cms)
{
	if (!m_qrootb)
	{
		cms.Enable(false);
	}
	else
	{
		cms.Enable(true);
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Make a best guess at the writing system used by the selection.  This defaults to the user
	interface ws if nothing better exists.
----------------------------------------------------------------------------------------------*/
int AfVwRootSite::GetSelectionWs()
{
	int wsT = 0;
	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (qvwsel)
	{
		int cttp;
		CheckHr(qvwsel->GetSelectionProps(0, NULL, NULL, &cttp));
		if (cttp)
		{
			TtpVec vqttp;
			VwPropsVec vqvps;
			vqttp.Resize(cttp);
			vqvps.Resize(cttp);
			CheckHr(qvwsel->GetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin(),
						(IVwPropertyStore **)vqvps.Begin(), &cttp));
			int ittp;
			ITsTextProps * pttp;
			HRESULT hr = E_FAIL;
			for (ittp = 0; ittp < cttp; ++ittp)
			{
				pttp = vqttp[ittp];
				int var;
				hr = pttp->GetIntPropValues(ktptWs, &var, &wsT);
				if (hr == S_OK)
					break;
			}
		}
	}
	if (!wsT || wsT == -1)
	{
		ISilDataAccessPtr qsdaT;
		CheckHr(m_qrootb->get_DataAccess(&qsdaT));
		ILgWritingSystemFactoryPtr qwsf;
		CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));
		CheckHr(qwsf->get_UserWs(&wsT));
	}
	return wsT;
}


/*----------------------------------------------------------------------------------------------
	Enable/Disable formatting toolbar buttons and comboboxes (and the format font command).
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmsCharFmt1(CmdState & cms)
{
#ifdef TRACING_IDLE_TIME
s_cidle ++;
int ms = 0;
{
MeasureDuration(ms);
#endif
	// Get selection. Can't do command unless we have one.
	bool fEnable = false;
	ComBool fCanFormat = false;
	IVwSelectionPtr qvwsel;
	if (m_qrootb)
		CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel || !SelectionInOneField())
	{
		cms.Enable(false);
		goto LDone;
	}

	int cid = cms.Cid();
	switch (cid)
	{
	case kcidFmtFnt:
	case kcidFmtWrtgSys:
		CheckHr(qvwsel->get_CanFormatChar(&fCanFormat));
		fEnable = (bool)fCanFormat;
		break;
	case kcidFmttbWrtgSys:
	case kcidFmttbStyle:
	case kcidFmttbFnt:
	case kcidFmttbFntSize:
		Assert(cms.IsFromToolbar());
		CheckHr(qvwsel->get_CanFormatChar(&fCanFormat));
		if (fCanFormat)
		{
			// Get the handle to the given ComboBox on this toolbar.
			HWND hwndTool = cms.GetToolbar();
			Assert(hwndTool);
			HWND hwndCombo = ::GetDlgItem(hwndTool, cid);
			Assert(hwndCombo);

			// Update the combobox setting to the value of the current
			// selection (or insertion point).
			HWND hwnd = ::GetFocus();
			if (!hwnd || !::IsChild(hwndCombo, hwnd))
			{
				achar szBuffer[64] = { 0 };
				int cch = isizeof(szBuffer) / isizeof(achar);
				cch = ::GetWindowText(hwndCombo, szBuffer, cch);
				szBuffer[cch] = 0;
				StrApp strOld(szBuffer);
				StrApp str(strOld);
				if (GetFormatting(cid, str) && str != strOld)
				{
					if (cid == kcidFmttbFnt || cid == kcidFmttbWrtgSys)
					{
						HWND hwndFont = ::GetDlgItem(hwndTool, kcidFmttbFnt);
						TssComboEx * ptceFont =
							dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwndFont));
						UpdateDefaultFontNames((void *)ptceFont);
					}

					TssComboEx * ptce = dynamic_cast<TssComboEx *>(
						AfWnd::GetAfWnd(hwndCombo));
					AssertPtr(ptce);

					ITsStrFactoryPtr qtsf;
					ITsStringPtr qtss;
					qtsf.CreateInstance(CLSID_TsStrFactory);
					StrUni stu(str);
					int wsT = GetSelectionWs();
					qtsf->MakeStringRgch(stu.Chars(), stu.Length(), wsT, &qtss);
					int iitem = ptce->FindStringExact(-1, qtss);
					ptce->SetCurSel(iitem);
					if (iitem == -1)
					{
						// There wasn't an exact match, so set the window text to the right
						// string. NOTE: We still need to call SetCurSel even if iitem = -1
						// so that the current selection will be removed.
						::SetWindowText(ptce->Hwnd(), str.Chars());
					}
				}
			}
			fEnable = true; // Now renderer selection is automatic always enable the font combo.
		}
		break;
	case kcidFmttbBold:
	case kcidFmttbItal:
		CheckHr(qvwsel->get_CanFormatChar(&fCanFormat));
		if (fCanFormat)
		{
			// Update the CHECK state to the value of the current selection
			// (or insertion point).
			StrApp str;
			cms.SetCheck(GetFormatting(cid, str));
			cms.Enable(true);
		}
		fEnable = (bool)fCanFormat;
		break;
	case kcidFmttbAlignLeft:
	case kcidFmttbAlignCntr:
	case kcidFmttbAlignRight:
		CheckHr(qvwsel->get_CanFormatPara(&fCanFormat));
		if (fCanFormat)
		{
			// Update the CHECK state to the value of the current selection
			// (or insertion point).
			StrApp str;
			cms.SetCheck(GetFormatting(cid, str));
		}
		fEnable = (bool)fCanFormat;
		break;
	case kcidFmttbLstNum:
	case kcidFmttbLstBullet:
	case kcidFmttbApplyBdr:
		{
			CheckHr(qvwsel->get_CanFormatPara(&fCanFormat));
			if (fCanFormat)
			{
				// Update the CHECK state to the value of the current selection
				// (or insertion point).
				StrApp str;
				cms.SetCheck(GetParaFormatting(cid, str));
			}
			fEnable = (bool)fCanFormat;
		}
		break;

	case kcidFmttbUnind:
	case kcidFmttbInd:
		CheckHr(qvwsel->get_CanFormatPara(&fCanFormat));
		fEnable = (bool)fCanFormat;
		break;
	case kcidFmttbApplyBgrndColor:
	case kcidFmttbApplyFgrndColor:
		CheckHr(qvwsel->get_CanFormatChar(&fCanFormat));
		fEnable = (bool)fCanFormat;
		break;
	default:
		Assert(false);
		fEnable = false;
		break;
	}
	cms.Enable(fEnable);
LDone: ;
#ifdef TRACING_IDLE_TIME
} // ends MeasureDuration block, ms has time
int msOld;
int cid = cms.Cid();
if (s_hmcidms.Retrieve(cid, &msOld))
	ms += msOld;
s_hmcidms.Insert(cid, ms, true);
#endif
	return true; // Indicates we have handled it.
}

/*----------------------------------------------------------------------------------------------
	Bring up the format paragraph dialog.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdFmtPara1(Cmd * pcmd)
{
	ISilDataAccessPtr qsda;
	BeginUndoTask(pcmd->m_cid, &qsda);

	bool f = FormatParas(knFormatNormal, m_fCanDoRtl, OuterRightToLeft(), 0);

	EndUndoTask(qsda);

	return f;
}

/*----------------------------------------------------------------------------------------------
	Format/Borders commmand.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdFmtBdr1(Cmd * pcmd)
{
	ISilDataAccessPtr qsda;
	BeginUndoTask(pcmd->m_cid, &qsda);

	bool f = FormatParas(knFormatBorder, m_fCanDoRtl, OuterRightToLeft(), 0);

	EndUndoTask(qsda);

	return f;
}

/*----------------------------------------------------------------------------------------------
	Format Bullets/Numbers commmand.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdFmtBulNum1(Cmd * pcmd)
{
	ISilDataAccessPtr qsda;
	BeginUndoTask(pcmd->m_cid, &qsda);

	bool f = FormatParas(knFormatBulNum, m_fCanDoRtl, OuterRightToLeft(), 0);

	EndUndoTask(qsda);

	return f;
}

/*----------------------------------------------------------------------------------------------
	Enable Paragraph menu item or not
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmsFmtPara1(CmdState & cms)
{
	Assert(cms.Cid() == kcidFmtPara);
	return CmsFmtX(cms);
}

/*----------------------------------------------------------------------------------------------
	Enable Style menu item or not.

	To start with, Format Style should be enabled only when paragraph formatting is permitted,
	since that is currently the only time it works.  Eventually, we want to allow it all the
	time, but it needs to know what kinds of formatting are possible: if only character
	formatting is possible, only character styles may be applied, if no formatting is possible,
	Apply can't be done at all, but the style definitions can still be changed.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmsFmtStyles1(CmdState & cms)
{
	Assert(cms.Cid() == kcidFmtStyles);
	cms.Enable(true);
	return true;		// Indicates we have handled it.
}

/*----------------------------------------------------------------------------------------------
	Enable Border menu item or not
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmsFmtBdr1(CmdState & cms)
{
	Assert(cms.Cid() == kcidFmtBdr);
	return CmsFmtX(cms);
}

/*----------------------------------------------------------------------------------------------
	Enable Bullet/Numbers menu item or not
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmsFmtBulNum1(CmdState & cms)
{
	Assert(cms.Cid() == kcidFmtBulNum);
	return CmsFmtX(cms);
}

/*----------------------------------------------------------------------------------------------
	Enable menu item if we are in a paragraph
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmsFmtX(CmdState & cms)
{
	IVwSelectionPtr qvwsel;
	HVO hvoText;
	int tagText;
	VwPropsVec vqvps;
	int ihvoFirst, ihvoLast;
	ISilDataAccessPtr qsda;
	TtpVec vqttp;
	bool ans;

	// If neither a selection nor a paragraph property, don't enable menu item.
	ans = GetParagraphProps(
		&qvwsel, hvoText, tagText, vqvps, ihvoFirst, ihvoLast, &qsda, vqttp);
	ans = ans && (0 != vqttp.Size());

	cms.Enable(ans);
	return true; // we handled it.
}

/*----------------------------------------------------------------------------------------------
	Run the Find command.
	(The pcmd argument is unused and therefore has a default, but it is present because this
	method much match the pattern for command implementations.)
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdEditFind1(Cmd * pcmd)
{
	return DoFindReplace(false);
}

/*----------------------------------------------------------------------------------------------
	Run the Replace command.
	(The pcmd argument is unused and therefore has a default, but it is present because this
	method much match the pattern for command implementations.)
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdEditRepl1(Cmd * pcmd)
{
	return DoFindReplace(true);
}

/*----------------------------------------------------------------------------------------------
	Run the Refresh command.
	(The pcmd argument is unused and therefore has a default, but it is present because this
	method much match the pattern for command implementations.)
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdViewRefresh1(Cmd * pcmd)
{
	WaitCursor wc;
	// Don't test the results here. Under some circumstances CleDeFeString::IsOkToClose might
	// return false, because the cache wasn't loaded yet when it was checking things out.
	// We don't want to return false or we get make extra unnecessary calls when multiple
	// windows are open.
	DoViewRefresh();
	return true; // We processed the command.
}

/*----------------------------------------------------------------------------------------------
	Remove any hard-formatting that duplicates formatting in the styles.
	Return true if any change was made to pttpHard.

	Review (SharonC): this also removes any hard-formatting that is redundant with what is
	imposed by the view constructor. The rationale is that since they are working within that
	visual context, the difference between, say, hard-formatted size-22 text and size-22 text
	generated by the view constructor will not be obvious, so there should not be any
	internal difference. Also it is more straightforward to implement that way, and they
	really shouldn't be using view constructor formatting for structured text anyway.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::RemoveRedundantHardFormatting(ISilDataAccess * psda,
	IVwPropertyStore * pvpsSoft, ITsTextProps * pttpHard, bool fParaStyle, HVO hvoPara,
	ITsTextProps ** ppttpRet)
{
	IVwPropertyStorePtr qvpsSoft = pvpsSoft;
	ITsPropsBldrPtr qtpb;
	if (fParaStyle)
	{
		// Setting a paragraph style automatically removes any paragraph hard formatting.
		// But what we need to fix is the character hard formatting for each run in the
		// paragraph.

		// First, apply the new style to the "soft" property store.
		SmartBstr sbstrStyle;
		CheckHr(pttpHard->GetStrPropValue(ktptNamedStyle, &sbstrStyle));
		ITsPropsBldrPtr qtpbStyle;
		qtpbStyle.CreateInstance(CLSID_TsPropsBldr);
		CheckHr(qtpbStyle->SetStrPropValue(ktptNamedStyle, sbstrStyle));
		ITsTextPropsPtr qttpStyle;
		CheckHr(qtpbStyle->GetTextProps(&qttpStyle));
		IVwPropertyStorePtr qvpsSoftPlusStyle;
		CheckHr(qvpsSoft->get_DerivedPropertiesForTtp(qttpStyle, &qvpsSoftPlusStyle));

		ITsPropsBldrPtr qtpbEnc;
		qtpbEnc.CreateInstance(CLSID_TsPropsBldr);

		ITsStringPtr qtss;
		CheckHr(psda->get_StringProp(hvoPara, kflidStTxtPara_Contents, &qtss));

		ITsStrBldrPtr qtsb;
		CheckHr(qtss->GetBldr(&qtsb));

		int crun;
		CheckHr(qtss->get_RunCount(&crun));
		bool fChanged = false;
		for (int irun = 0; irun < crun; irun++)
		{
			// Get the run's properties.
			TsRunInfo tri;
			ITsTextPropsPtr qttpRun;
			CheckHr(qtss->FetchRunInfo(irun, &tri, &qttpRun));

			// Create a property store based on the soft properties but with the writing system
			// specified. This has the effect of applying any character properties for
			// for the given writing system that are already in the property store.
			IVwPropertyStorePtr qvpsForRun;
			int ws, nVar;
			CheckHr(qttpRun->GetIntPropValues(ktptWs, &nVar, &ws));
			CheckHr(qtpbEnc->SetIntPropValues(ktptWs, nVar, ws));
			ITsTextPropsPtr qttpEnc;
			CheckHr(qtpbEnc->GetTextProps(&qttpEnc));
			CheckHr(qvpsSoftPlusStyle->get_DerivedPropertiesForTtp(qttpEnc, &qvpsForRun));

			// Compare the adjusted property store to the run's properties.
			ITsTextPropsPtr qttpFixed;
			if (RemoveRedundantHardFormatting(psda, qvpsForRun, qttpRun, false, 0, &qttpFixed))
			{
				// Make the change in the string builder.
				CheckHr(qtsb->SetProperties(tri.ichMin, tri.ichLim, qttpFixed));
				fChanged = true;
			}
		}

		if (fChanged)
		{
			// Update the string in the data cache.
			ITsStringPtr qtssFixed;
			CheckHr(qtsb->GetString(&qtssFixed));
			CheckHr(psda->SetString(hvoPara, kflidStTxtPara_Contents, qtssFixed));
		}

		return false;
	}

	int ctpt;
	CheckHr(pttpHard->get_IntPropCount(&ctpt));
	for (int itpt = 0; itpt < ctpt; itpt++)
	{
		int tpt;
		int nVarHard, nValHard;
		CheckHr(pttpHard->GetIntProp(itpt, &tpt, &nVarHard, &nValHard));

		int nValSoft, nVarSoft;
		int nWeight, nRelHeight;
		switch (tpt)
		{
		case ktptLineHeight:
			CheckHr(pvpsSoft->get_IntProperty(tpt, &nValSoft));
			CheckHr(pvpsSoft->get_IntProperty(kspRelLineHeight, &nRelHeight));
			if (nRelHeight)
			{
				nVarSoft = ktpvRelative;
				nValSoft = nRelHeight;
			}
			// By default, we have no min spacing; interpret this as single-space.
			else if (!nValSoft)
			{
				nVarSoft = ktpvRelative;
				nValSoft = kdenTextPropRel;
			}
			// Otherwise interpret as absolute. Use the value we already.
			else
				nVarSoft = ktpvMilliPoint;
			break;
		case ktptBold:
			// For an inverting property, a value of invert is never redundant.
			if (nValHard == kttvInvert)
				continue;
			CheckHr(pvpsSoft->get_IntProperty(tpt, &nWeight));
			nValSoft = (nWeight > 550) ? kttvInvert : kttvOff;
			nVarSoft = ktpvEnum;
			break;
		case ktptItalic:
			// For an inverting property, a value of invert is never redundant.
			if (nValHard == kttvInvert)
				continue;
		case ktptUnderline:
		case ktptSuperscript:
		case ktptRightToLeft:
		case ktptKeepTogether:
		case ktptKeepWithNext:
		case ktptWidowOrphanControl:
		case ktptAlign:
		case ktptBulNumScheme:
			CheckHr(pvpsSoft->get_IntProperty(tpt, &nValSoft));
			nVarSoft = ktpvEnum;
			break;
		case ktptFontSize:
		case ktptOffset:
		case ktptLeadingIndent:		// == ktptMarginLeading
		case ktptTrailingIndent:	// == ktptMarginTrailing
		case ktptFirstIndent:
		case ktptSpaceBefore:		// == ktptMswMarginTop
		case ktptSpaceAfter:		// == ktptMarginBottom
		case ktptBorderTop:
		case ktptBorderBottom:
		case ktptBorderLeading:
		case ktptBorderTrailing:
		case ktptMarginTop:
		case ktptPadTop:
		case ktptPadBottom:
		case ktptPadLeading:
		case ktptPadTrailing:
			CheckHr(pvpsSoft->get_IntProperty(tpt, &nValSoft));
			nVarSoft = ktpvMilliPoint;
			break;
		case ktptForeColor:
		case ktptBackColor:
		case ktptUnderColor:
		case ktptBorderColor:
		case ktptBulNumStartAt:
			CheckHr(pvpsSoft->get_IntProperty(tpt, &nValSoft));
			nVarSoft = ktpvDefault;
			break;
		default:
			// Ignore.
			continue;
		};

		if (nValHard == nValSoft && nVarHard == nVarSoft)
		{
			// Clear.
			if (!qtpb)
				CheckHr(pttpHard->GetBldr(&qtpb));
			CheckHr(qtpb->SetIntPropValues(tpt, -1, -1));
		}
	}

	// String properties.

	CheckHr(pttpHard->get_StrPropCount(&ctpt));
	for (int itpt = 0; itpt < ctpt; itpt++)
	{
		int tpt;
		SmartBstr sbstrHard;
		CheckHr(pttpHard->GetStrProp(itpt, &tpt, &sbstrHard));

		switch (tpt)
		{
		case ktptFontFamily:
		case ktptWsStyle:
		case ktptFontVariations:
		case ktptBulNumTxtBef:
		case ktptBulNumTxtAft:
		case ktptBulNumFontInfo:
			break; // Process.
		default:
			// Ignore.
			continue;
		}

		SmartBstr sbstrSoft;
		CheckHr(pvpsSoft->get_StringProperty(tpt, &sbstrSoft));
		if (sbstrHard == sbstrSoft)
		{
			// Clear.
			if (!qtpb)
				CheckHr(pttpHard->GetBldr(&qtpb));
			sbstrHard.Clear();
			CheckHr(qtpb->SetStrPropValue(tpt, sbstrHard));
		}
	}

	if (qtpb)
	{
		// Something changed.
		CheckHr(qtpb->GetTextProps(ppttpRet));
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Run the Find or Replace command.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::DoFindReplace(bool fReplace)
{
	if (!AfApp::Papp())
	{
		// For now give up. Maybe later we will have our own member variable and fall back on
		// that? This only happens when used as an ActiveX control. Or maybe we will implement
		// a COM interface for the ActiveX control to still access an Application interface?
		return true;
	}

	// Make sure the find is happening in the real active window, not something like the
	// Quick Find edit box itself, which could be the active root site if we just clicked in it.
	ISilDataAccessPtr qsda;
	IVwStylesheetPtr qasts;
#ifdef DEBUG
	// We probably want to change all occurrences of GetCurMainWnd() to MainWindow()
	// in this file, but we didn't take the time to verify this would always work. These DEBUG
	// asserts in this file are a temporary test to see if there is ever any difference between
	// the two. If one is hit, it's likely that GetCurMainWnd is returning the wrong value. If
	// this doesn't cause any problem after some time, we can go ahead and switch them all
	// to MainWindow.
	AfMainWnd * pafw1 = AfApp::Papp()->GetCurMainWnd();
	AfMainWnd * pafw2 = m_pwndSubclass->MainWindow();
	Assert(pafw1 == pafw2);
#endif
	AfMainWndPtr qafw = AfApp::Papp()->GetCurMainWnd();
	Assert(qafw);
	IVwRootBoxPtr qrootbCur = qafw->GetActiveRootBox(false);
	if (!qrootbCur)
		return false;		// Shouldn't happen, but if it does ...

	CheckHr(qrootbCur->get_DataAccess(&qsda));
	CheckHr(qrootbCur->get_Stylesheet(&qasts));

	IVwRootSitePtr qvrs;
	CheckHr(qrootbCur->get_Site(&qvrs));
	AfVwRootSitePtr qavrs = dynamic_cast<AfVwRootSite *>(qvrs.Ptr());
	Assert(qavrs);

	IVwOverlayPtr qvo;
	CheckHr(qrootbCur->get_Overlay(&qvo));
	bool fOverlays = (qvo.Ptr() != NULL);

	AfFindRepDlgPtr qfnddlg;
	qfnddlg.Attach(NewObj AfFindRepDlg());
	IVwPattern * pxpat = qafw->GetFindPattern();
	CheckHr(pxpat->putref_Limit(NULL)); // new search, forget limit.
	qfnddlg->SetFormatSources(qsda, qasts);
	qfnddlg->SetDialogValues(pxpat, qavrs, fReplace, fOverlays);
	int stidNoMatches, stidReplaceN;
	GetFindReplMsgs(&stidNoMatches, &stidReplaceN);
	qfnddlg->SetMessageIDs(stidNoMatches, stidReplaceN);
	qfnddlg->DoModeless(Window()->Hwnd());
	::ShowWindow(qfnddlg->Hwnd(), SW_SHOW);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Run the View Refresh command.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::DoViewRefresh()
{
	if (!AfApp::Papp())
	{
		// For now give up. Maybe later we will have our own member variable and fall back on
		// that? This only happens when used as an ActiveX control. Or maybe we will implement
		// a COM interface for the ActiveX control to still access an Application interface?
		return true;
	}

	AfDbApp * papp = dynamic_cast<AfDbApp *>(AfApp::Papp());
	if (papp)
	{
#ifdef DEBUG
	// We probably want to change all occurrences of GetCurMainWnd() to MainWindow()
	// in this file, but we didn't take the time to verify this would always work. These DEBUG
	// asserts in this file are a temporary test to see if there is ever any difference between
	// the two. If one is hit, it's likely that GetCurMainWnd is returning the wrong value. If
	// this doesn't cause any problem after some time, we can go ahead and switch them all
	// to MainWindow.
	AfMainWnd * pafw1 = AfApp::Papp()->GetCurMainWnd();
	AfMainWnd * pafw2 = m_pwndSubclass->MainWindow();
	Assert(pafw1 == pafw2);
#endif
		RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(papp->GetCurMainWnd());
		if (pwnd && !papp->FullRefresh(pwnd->GetLpInfo()))
			return false;
	}
	else
		return false;
	return true; // Processed command.
}

/*----------------------------------------------------------------------------------------------
	Return the messages that should be used for the Find/Replace dialog, depending
	on whether we are in a document or data entry view.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::GetFindReplMsgs(int * pstidNoMatches, int * pstidReplaceN)
{
	*pstidNoMatches = kstidNoMatchesDoc;
	*pstidReplaceN = kstidReplaceNDoc;
}

/*----------------------------------------------------------------------------------------------
	Run the Quick Search command.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdEditSrchQuick1(Cmd * pcmd)
{
	AfMainWnd * pafw = m_pwndSubclass->MainWindow();
	AssertPtr(pafw);
	IVwPattern * pxpat = pafw->GetFindPattern();

	ITsStringPtr qtssFindWhat;
	CheckHr(pxpat->get_Pattern(&qtssFindWhat));
	int cch;
	CheckHr(qtssFindWhat->get_Length(&cch));
	if (cch == 0)
		return true;

	return NextMatch(true, false, true, false);
}

/*----------------------------------------------------------------------------------------------
	The Quick Search control is never disabled, but we regularly update the edit box to
	match the current search pattern.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmsEditSrchQuick1(CmdState & cms)
{
	AfMainWnd * pafw = m_pwndSubclass->MainWindow();
	AssertPtr(pafw);
	IVwPattern * pxpat = pafw->GetFindPattern();

	ITsStringPtr qtssFindWhat;
	CheckHr(pxpat->get_Pattern(&qtssFindWhat));

	HWND hwndSQ = ::GetDlgItem(cms.GetToolbar(), cms.Cid());
	ITsStringPtr qtssCurr;
	::SendMessage(hwndSQ, FW_EM_GETTEXT, 0, (LPARAM)&qtssCurr);

	ComBool fEq = false;
	if (qtssFindWhat)
		CheckHr(qtssFindWhat->Equals(qtssCurr, &fEq));
	if (!fEq)
		::SendMessage(hwndSQ, FW_EM_SETTEXT, 0, (LPARAM)qtssFindWhat.Ptr());
	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the next (or previous, if that cid) match.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdEditNextMatch1(Cmd * pcmd)
{
	if (!AfApp::Papp())
		return true;
	// Make sure the find is happening in the real active window, not something like the
	// Quick Find edit box itself, which could be the active root site if we just clicked in it.
#ifdef DEBUG
	// We probably want to change all occurrences of GetCurMainWnd() to MainWindow()
	// in this file, but we didn't take the time to verify this would always work. These DEBUG
	// asserts in this file are a temporary test to see if there is ever any difference between
	// the two. If one is hit, it's likely that GetCurMainWnd is returning the wrong value. If
	// this doesn't cause any problem after some time, we can go ahead and switch them all
	// to MainWindow.
	AfMainWnd * pafw1 = AfApp::Papp()->GetCurMainWnd();
	AfMainWnd * pafw2 = m_pwndSubclass->MainWindow();
	Assert(pafw1 == pafw2);
#endif
	AfMainWndPtr qafw = AfApp::Papp()->GetCurMainWnd();
	Assert(qafw);
	AfVwRootSitePtr qavrs;
	qafw->GetActiveViewWindow(&qavrs, NULL, false);
	if (!qavrs)
		return false;

	if (qavrs != this)
	{
		// From the Quick Search box.
		IVwPattern * pxpat = qafw->GetFindPattern();

		ITsStringPtr qtssFindWhat;
		CheckHr(pxpat->get_Pattern(&qtssFindWhat));
		int cch;
		CheckHr(qtssFindWhat->get_Length(&cch));
	}

	return qavrs->NextMatch(pcmd->m_cid != kcidEditPrevMatch);
}

/*----------------------------------------------------------------------------------------------
	Get the next (or previous, if fForward is false) match.

	Launch the Find dialog if the search string is empty, but only if fDialogIfEmpty is false.

	Return focus to hwndFrom when complete, if reporting an error. (If null, return focus to
	your own window.)
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::NextMatch(bool fForward, bool fDialogIfEmpty, bool fScrollAndMsg,
	bool fReplace, HWND hwndFrom, IVwSearchKiller * pxserkl)
{
	// Set the focus on the main window (away from the Quick Find box if that's where it
	// happens to be) so the selection will show up.
#ifdef DEBUG
	// We probably want to change all occurrences of GetCurMainWnd() to MainWindow()
	// in this file, but we didn't take the time to verify this would always work. These DEBUG
	// asserts in this file are a temporary test to see if there is ever any difference between
	// the two. If one is hit, it's likely that GetCurMainWnd is returning the wrong value. If
	// this doesn't cause any problem after some time, we can go ahead and switch them all
	// to MainWindow.
	AfMainWnd * pafw1 = AfApp::Papp()->GetCurMainWnd();
	AfMainWnd * pafw2 = m_pwndSubclass->MainWindow();
	Assert(pafw1 == pafw2);
#endif
	AfMainWndPtr qafw = AfApp::Papp()->GetCurMainWnd();
	Assert(qafw);
	AfVwRootSitePtr qvrs;
	qafw->GetActiveViewWindow(&qvrs, NULL, false);
	::SetFocus(qvrs->Window()->Hwnd());

	ComBool fAbort;
	if (pxserkl)
	{
		CheckHr(pxserkl->get_AbortRequest(&fAbort));
		if (fAbort == ComBool(true))
			return false;
	}

	StrApp strMsgTitle;
	strMsgTitle.Load(fReplace ? kstidReplace : kstidFind);

	int stidNoMatches;
	int stidReplaceN;
	GetFindReplMsgs(&stidNoMatches, &stidReplaceN);

	if (!hwndFrom)
		hwndFrom = Window()->Hwnd();
	IVwPattern * pxpat = qafw->GetFindPattern();

	ITsStringPtr qtssPattern;
	CheckHr(pxpat->get_Pattern(&qtssPattern));
	int cch = 0;
	if (qtssPattern)
		CheckHr(qtssPattern->get_Length(&cch));
	// If we don't have a current pattern launch the full Find dialog, if permitted.
	if (!cch)
	{
		if (fDialogIfEmpty)
			if (fForward)
				return CmdEditFind1();
			else
			{
				StrApp strMsg(kstidNoFindMatch);
				::MessageBox(hwndFrom, strMsg.Chars(), strMsgTitle.Chars(),
					MB_OK | MB_ICONINFORMATION);
				return true;
			}
		// We can now search for empty strings, if there is a style or old writing system
		// selected.
		// ENHANCE (SharonC): possibly rework this when we use real patterns.
//		else
//			return true; // Caller will handle, maybe in ReplaceAll.
	}
	IVwSelectionPtr qsel;
	if (!m_qrootb)
		return false;
	CheckHr(m_qrootb->get_Selection(&qsel));
	// If we introduce a new limit, save it here until after the first search; otherwise,
	// we will stop at once because of starting at the limit.
	IVwSelectionPtr qselNewLimit;
	bool fFirstTry = false;

	if (qsel)
	{
		IVwSelectionPtr qselLimit;
		CheckHr(pxpat->get_Limit(&qselLimit));
		IVwSelectionPtr qselStartingPoint;
		CheckHr(pxpat->get_StartingPoint(&qselStartingPoint));
		DWORD hwndOld;
		CheckHr(pxpat->get_SearchWindow(&hwndOld));
		ComBool fForwardLast;
		CheckHr(pxpat->get_LastDirection(&fForwardLast));
		if (fForward != fForwardLast || hwndOld != (DWORD) Window()->Hwnd() ||
			!SameObject(qselStartingPoint, qsel) || !qselLimit)
		{
			// User has done something else since last find next or is searching in the
			// opposite direction or we discarded the limit for some reason; make a new limit.
			qselNewLimit = qsel;
			// No limit for this search.
			CheckHr(pxpat->putref_Limit(NULL));
			// But we can go ahead and remember the window we're searching in.
			CheckHr(pxpat->put_SearchWindow((DWORD)(Window()->Hwnd())));
			fFirstTry = true;
		}
		CheckHr(pxpat->FindFrom(qsel, fForward, pxserkl));
	}
	else
	{
		// No current selection, do a find starting from the boundary. Also note no
		// Limit.
		CheckHr(pxpat->putref_Limit(NULL));
		CheckHr(pxpat->Find(m_qrootb, fForward, pxserkl));
		fFirstTry = true;
	}
	if (qselNewLimit)
		CheckHr(pxpat->putref_Limit(qselNewLimit));
	for (int cact = 0; cact < 2; cact++) // for safety no more than two iterations.
	{
		ComBool fFound;
		CheckHr(pxpat->get_Found(&fFound));
		if (fFound)
		{
			IVwSelectionPtr qselFound;
			CheckHr(pxpat->GetSelection(true, &qselFound));
			CheckHr(pxpat->putref_StartingPoint(qselFound));
			//MakeSelectionVisible1();
			if (fScrollAndMsg)
				ScrollSelectionNearTop1();
			return true;
		}
		else
		{
			// Is it because we hit the limit? If not we can try wrapping around...
			ComBool fStoppedAtLimit;
			CheckHr(pxpat->get_StoppedAtLimit(&fStoppedAtLimit));
			if (fStoppedAtLimit)
			{
				// There may be an animated icon. Stop it if so:
				if (hwndFrom)
				{
					HWND hwndAnim = ::GetDlgItem(hwndFrom, kctidFindAnimation);
					if (hwndAnim)
					{
						Animate_Stop(hwndAnim);
						::ShowWindow(hwndAnim, SW_HIDE);
					}
				}
				if (fScrollAndMsg)
				{
					StrApp staMsg(fFirstTry ? stidNoMatches : kstidNoMoreMatches);
					::MessageBox(hwndFrom, staMsg.Chars(), strMsgTitle.Chars(),
						MB_OK | MB_ICONINFORMATION);
				}
				CheckHr(pxpat->putref_Limit(NULL)); // allow the user to go around again if he
													// wishes.
				return true;
			}
			// Wrap around.
			CheckHr(pxpat->Find(m_qrootb, fForward, pxserkl));
		}
	}
	if (pxserkl)
	{
		CheckHr(pxserkl->get_AbortRequest(&fAbort));
		if (fAbort == ComBool(true))
			return false;
	}

	Assert(false); // we should have stopped at limit the second time around.
	// There may be an animated icon. Stop it if so:
	if (hwndFrom)
	{
		HWND hwndAnim = ::GetDlgItem(hwndFrom, kctidFindAnimation);
		if (hwndAnim)
		{
			Animate_Stop(hwndAnim);
			::ShowWindow(hwndAnim, SW_HIDE);
		}
	}
	if (fScrollAndMsg)
	{
		// Just in case we'll report not found anyway.
		StrApp staMsg(fFirstTry ? stidNoMatches : kstidNoMoreMatches);
		::MessageBox(hwndFrom, staMsg.Chars(), strMsgTitle.Chars(),
			MB_OK | MB_ICONINFORMATION);
	}
	CheckHr(pxpat->putref_Limit(NULL)); // allow the user to go around again if he wishes.

	// TODO JohnT: implement differently for data entry views. Maybe this can be done by
	// overriding on AfDeFeVw?
	return true;
}

/*----------------------------------------------------------------------------------------------
	Begin a sequence of tasks that can be undone as a unit. Give them an appropriate label
	based on what menu item or button was used.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::BeginUndoTask(int cid, ISilDataAccess ** ppsda)
{
	CheckHr(m_qrootb->get_DataAccess(ppsda));
	if (*ppsda)
	{
		int stid = UndoRedoLabelRes(cid);
		StrUni stuUndo, stuRedo;
		StrUtil::MakeUndoRedoLabels(stid, &stuUndo, &stuRedo);
		CheckHr((*ppsda)->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));

		//	Record an action that will handle replacing the selection on undo.
		SetupUndoSelection(*ppsda, true);
	}
}

/*----------------------------------------------------------------------------------------------
	End a sequence of tasks that can be undone as a unit.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::EndUndoTask(ISilDataAccess * psda)
{
	if (psda)
	{
		//	Record an action that will handle replacing the selection on redo.
		SetupUndoSelection(psda, false);

		CheckHr(psda->EndUndoTask());
	}
}

/*----------------------------------------------------------------------------------------------
	Set up an undo-action to replace the selection
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::SetupUndoSelection(ISilDataAccess * psda, bool fForUndo)
{
	IActionHandlerPtr qacth;
	CheckHr(psda->GetActionHandler(&qacth));
	if (!qacth)
		return;

	VwUndoDaPtr quda = dynamic_cast<VwUndoDa *>(psda);
	if (!quda)
		return;

	IVwSelectionPtr qsel;
	m_qrootb->get_Selection(&qsel);

	VwUndoSelectionActionPtr quact;
	quact.Attach(NewObj VwUndoSelectionAction(quda, this, qsel, fForUndo));

	CheckHr(qacth->AddAction(quact));
}

/*----------------------------------------------------------------------------------------------
	Return resources IDs of strings that can be used as labels for the Undo and Redo
	commands in the menu.
----------------------------------------------------------------------------------------------*/
int AfVwRootSite::UndoRedoLabelRes(int cid)
{
	switch (cid)
	{
	case kcidTyping:
		return kstidUndoTyping;
	case kcidDeleteKey:
		return kstidUndoDelete;
	case kcidFmtFnt:
		return kstidUndoFontFormatting;
	case kcidFmtPara:
		return kstidUndoParaFormatting;
	case kcidFmtBulNum:
		return kstidUndoBulAndNum;
	case kcidFmtBdr:
		return kstidUndoBorder;
	case kcidFmtStyles:
		return kstidUndoStyleChanges;
	case kcidFmttbStyle:
	case kcidApplyNormalStyle:
		return kstidUndoApplyStyle;
	case kcidFmttbWrtgSys:
	case kcidFmtWrtgSys:
		return kstidUndoWritingSys;
	case kcidFmttbFnt:
		return kstidUndoFont;
	case kcidFmttbFntSize:
		return kstidUndoFontSize;
	case kcidFmttbBold:
		return kstidUndoBold;
	case kcidFmttbItal:
		return kstidUndoItalic;
	case kcidFmttbApplyBgrndColor:
		return kstidUndoBackColor;
	case kcidFmttbApplyFgrndColor:
		return kstidUndoForeColor;
	case kcidFmttbAlignLeft:
	case kcidFmttbAlignCntr:
	case kcidFmttbAlignRight:
		return kstidUndoParaAlign;
	case kcidFmttbLstNum:
		return kstidUndoNumber;
	case kcidFmttbLstBullet:
		return kstidUndoBullet;
	case kcidFmttbUnind:
		return kstidUndoDecIndent;
	case kcidFmttbInd:
		return kstidUndoIncIndent;
	case kcidFmttbApplyBdr:
		return kstidUndoBorder;
	case kcidEditCut:
		return kstidUndoCut;
	case kcidEditPaste:
		return kstidUndoPaste;
	case kcidEditDel:
		return kstidUndoDelete;
	default:
		return kstidUndoUnknown;
	}

	Assert(false);
	return kstidUndoUnknown;
}

/*----------------------------------------------------------------------------------------------
	Open Styles Dialog. Put in a separate function so that a derived class can provide
	its own slightly different dialog (e.g. for TE)

	@h3{Parameters}
	@code{
		hwnd -- window handle
		past -- pointer to the IVwStylesheet for a particular language project.
		vqttpPara -- vector of TsTextProps for paragraph properties.
		cttpChar -- count of TsTextProps for character properties.
		vqttpChar -- pointer to range of TsTextProps for character properties.
		pstuStyleName -- name of selected style, when AdjustTsTextProps returns.
		fStylesChanged -- true if any of the styles have been changed, when AdjustTsTextProps
			returns.
		fApply -- true if the Apply button was clicked
		fReloadDb -- true if the views data needs to be reloaded from the DB; this is needed
			when styles were renamed.
	}
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::OpenFormatStylesDialog(HWND hwnd, bool fCanDoRtl, bool fOuterRtl,
	IVwStylesheet * past, TtpVec & vqttpPara, TtpVec & vqttpChar, bool fCanFormatChar,
	StrUni * pstuStyleName, bool & fStylesChanged, bool & fApply, bool & fReloadDb)
{
	Assert(sizeof(ITsTextPropsPtr) == sizeof(ITsTextProps *));
	IFwCppStylesDlgPtr qfwst;
	qfwst.CreateInstance(CLSID_FwCppStylesDlg);
	StrUni stuHelpFile(AfApp::Papp()->GetHelpFile());
	ILgWritingSystemFactoryPtr qwsf;
	GetLgWritingSystemFactory(&qwsf);
	int nResult;
	ComBool fStylesChangedT;
	ComBool fApplyT;
	ComBool fReloadDbT;
	ComBool fResultT;
	SmartBstr sbstrStyleName;
	int hvoRoot = 0;
	// Get the writing systems "of interest" to the user.
	Vector<int> vws;
	AfMainWnd * pafw = m_pwndSubclass->MainWindow();
	AssertPtr(pafw);
	if (pafw)
	{
		AfLpInfo * plpi = pafw->GetLpInfo();
		AssertPtr(plpi);
		Vector<int> & vwsAnal = plpi->AllAnalWss();
		Vector<int> & vwsVern = plpi->AllVernWss();
		Set<int> setws;
		int iws;
		for (iws = 0; iws < vwsAnal.Size(); ++iws)
			setws.Insert(vwsAnal[iws]);
		for (iws = 0; iws < vwsVern.Size(); ++iws)
			setws.Insert(vwsVern[iws]);
		Set<int>::iterator it;
		for (it = setws.Begin(), iws = 0; it != setws.End(); ++it, iws++)
			vws.Push(it->GetValue());

		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(pafw);
		AssertPtr(prmw);
		hvoRoot = prmw->GetRootObj();
	}
	// get the log file stream
	IStreamPtr qstrm;
	CheckHr(AfApp::Papp()->GetLogPointer(&qstrm));
	CheckHr(qfwst->put_DlgType(ksdtStandard));
	CheckHr(qfwst->put_ShowAll(false));
	CheckHr(qfwst->put_SysMsrUnit(AfApp::Papp()->GetMsrSys()));
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));
	CheckHr(qfwst->put_UserWs(wsUser));
	CheckHr(qfwst->put_HelpFile(stuHelpFile.Bstr()));
	CheckHr(qfwst->putref_WritingSystemFactory(qwsf));
	CheckHr(qfwst->put_ParentHwnd(reinterpret_cast<DWORD>(hwnd)));
	CheckHr(qfwst->put_CanDoRtl(fCanDoRtl));
	CheckHr(qfwst->put_OuterRtl(fOuterRtl));
	CheckHr(qfwst->put_FontFeatures(true));
	CheckHr(qfwst->putref_Stylesheet(past));
	CheckHr(qfwst->put_CanFormatChar(fCanFormatChar));
	CheckHr(qfwst->put_StyleName(pstuStyleName->Bstr()));
	CheckHr(qfwst->SetTextProps(
		reinterpret_cast<ITsTextProps **>(vqttpPara.Begin()), vqttpPara.Size(),
		reinterpret_cast<ITsTextProps **>(vqttpChar.Begin()), vqttpChar.Size()));
	CheckHr(qfwst->put_RootObjectId(hvoRoot));
	CheckHr(qfwst->SetWritingSystemsOfInterest(vws.Begin(), vws.Size()));
	CheckHr(qfwst->putref_LogFile(qstrm));
	GUID clsidApp = *AfApp::Papp()->GetAppClsid();
	CheckHr(qfwst->put_AppClsid(clsidApp));
	IHelpTopicProviderPtr qhtprov = new HelpTopicProvider(AfApp::Papp()->GetHelpBaseName());
	CheckHr(qfwst->putref_HelpTopicProvider(qhtprov));

	CheckHr(qfwst->ShowModal(&nResult));
	CheckHr(qfwst->GetResults(&sbstrStyleName, &fStylesChangedT, &fApplyT,
		&fReloadDbT, &fResultT));

	pstuStyleName->Assign(sbstrStyleName.Chars());
	fStylesChanged = bool(fStylesChangedT);
	fApply = bool(fApplyT);
	fReloadDb = bool(fReloadDbT);
	return bool(fResultT);
}


//:>********************************************************************************************
//:>	AfVwScrollWndBase methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfVwScrollWndBase::AfVwScrollWndBase(AfWnd * pwnd, bool fScrollHoriz, bool fVerticalOrientation) :
	AfVwRootSite(pwnd, true, fScrollHoriz)
{
	// Default height of the (optional) header.
	m_dyHeader = 0;
	m_pomgr = CreateOrientationManager(fVerticalOrientation);
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfVwScrollWndBase::~AfVwScrollWndBase()
{
	delete(m_pomgr);
	m_pomgr = NULL;
}
/*----------------------------------------------------------------------------------------------
	Make a default orientation manager. Subclasses may override and make a different one.
----------------------------------------------------------------------------------------------*/
OrientationManager *  AfVwScrollWndBase::CreateOrientationManager(bool fVerticalOrientation)
{
	if (fVerticalOrientation)
		m_pomgr = NewObj VerticalOrientationManager(this);
	else
		m_pomgr = NewObj OrientationManager(this);
	return m_pomgr;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool AfVwScrollWndBase::OnVScroll(int nSBCode, int nPos, HWND hwndSbar)
{
	// NB - DON'T use nPos; it has only a 16-bit range.
	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_ALL, 0, 0, 0, 0 };
	m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);
	int dydPos = sinfo.nPos; // Where the window thinks it is now.
	int dydTrackPos = sinfo.nTrackPos; // Where the user dragged to.
	int dydMax = sinfo.nMax - sinfo.nMin; // Min is always 0 in VwWindow.
	int dydPage = sinfo.nPage;
	// ENHANCE JohnT: use resolution to figure dydLine accurately.
	int dydLine = 14 * 96 / 72; // 14 points is typically about a line.
	if (dydPage > dydLine * 2)
		dydPage -= dydLine;  // Page scroll by a line less than a complete page.
	switch (nSBCode)
	{
	case SB_BOTTOM:
		dydPos = dydMax; // Too large, but corrected like any out of range below.
		break;
	case SB_ENDSCROLL: // User released after holding in button; do nothing.
		break;
	case SB_LINEDOWN:
		dydPos += dydLine;
		break;
	case SB_LINEUP:
		dydPos -= dydLine;
		break;
	case SB_PAGEDOWN:
		dydPos += dydPage;
		break;
	case SB_PAGEUP:
		dydPos -= dydPage;
		break;
	case SB_THUMBTRACK:   // REVIEW JohnT(?): are we generally able to draw fast enough for
							// this?
	case SB_THUMBPOSITION:
		dydPos = dydTrackPos;
		break; // dydPos is already correct (but display may need adjusting).
	case SB_TOP:
		dydPos = 0;
		break;
	}
	// Max legal value (see MSDN\Platform SDK\User interface services\Controls\
	// Scroll bars\About scroll bars\Scroll box position and scrolling range).
	if (dydPos > dydMax - (int)(sinfo.nPage) - 1)
		dydPos = dydMax - (int)(sinfo.nPage) - 1;
	// Check this AFTER checking max, it is possible that dydMax < page size and we
	// just made it negative.
	if (dydPos < sinfo.nMin)
		dydPos = sinfo.nMin;

	int dydScrollBy = dydPos - sinfo.nPos;

	// Update the scroll bar.
	sinfo.fMask = SIF_POS;
	sinfo.nPos = dydPos;
	SetRootSiteScrollInfo(SB_VERT, &sinfo, true);

	ScrollBy(0, dydScrollBy);

#ifdef JT_2_24_04_WantSelMoveOnPageScroll
// According to DN-110 and by comparison with MS-Word, page up and down should not move
// the selection, even if it becomes invisible.
	// If the selection has disappeared off the screen due to vertical scrolling via PGUP or
	// PGDOWN, move it to where it's visible, either at the top or the bottom of the window,
	// depending on the direction of the scrolling.  Try to maintain the same relative
	// horizontal location.
	if ((nSBCode == SB_PAGEUP || nSBCode == SB_PAGEDOWN) && m_qrootb && !IsSelectionVisible())
	{
		HoldGraphics hg(this);
		Rect rcSrc;
		Rect rcDst;
		IVwSelectionPtr qvwsel;
		RECT rdIP;
		RECT rdSecondary;
		ComBool fSplit;
		ComBool fEndBeforeAnchor;
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (qvwsel)
		{
			bool fShifted = (nPos == kfssShift || nPos == kgrfssShiftControl);
			CheckHr(qvwsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rdIP,
				&rdSecondary, &fSplit, &fEndBeforeAnchor));
			int xd = (rdIP.left + rdIP.right) / 2;
			int yd = dydLine;
			if (nSBCode == SB_PAGEUP && dydPage > dydLine)
				yd = dydPage - dydLine;
			if (fShifted)
				m_qrootb->MouseDownExtended(xd, yd, hg.m_rcSrcRoot, hg.m_rcDstRoot);
			else
				m_qrootb->MouseDown(xd, yd, hg.m_rcSrcRoot, hg.m_rcDstRoot);
			if (!IsSelectionVisible())
			{
				// If we can't make a selection on the top or bottom line, try further in toward
				// the middle.
				if (nSBCode == SB_PAGEUP)
				{
					while (yd > 0)
					{
						yd -= dydLine;
						if (fShifted)
							m_qrootb->MouseDownExtended(xd, yd, hg.m_rcSrcRoot, hg.m_rcDstRoot);
						else
							m_qrootb->MouseDown(xd, yd, hg.m_rcSrcRoot, hg.m_rcDstRoot);
						if (IsSelectionVisible())
							break;
					}
				}
				else
				{
					while (yd < dydPage)
					{
						yd += dydLine;
						if (fShifted)
							m_qrootb->MouseDownExtended(xd, yd, hg.m_rcSrcRoot, hg.m_rcDstRoot);
						else
							m_qrootb->MouseDown(xd, yd, hg.m_rcSrcRoot, hg.m_rcDstRoot);
						if (IsSelectionVisible())
							break;
					}
				}
			}
		}
	}
#endif // want selection movement on page scroll.

	return true;
}


/*----------------------------------------------------------------------------------------------
	Process sizing (WM_SIZE). wst is the type of sizing requested. dxp is the new width, dyp
	is the new height.
----------------------------------------------------------------------------------------------*/
bool AfVwScrollWndBase::OnSize(int wst, int dxp, int dyp)
{
	bool fWindowVisible = ::IsWindowVisible(m_pwndSubclass->Hwnd());
	// Recompute your layout and redraw completely, unless the width has not actually changed.
	bool fSelVis = IsSelectionVisible();

	InitGraphics();
	if (Layout())
		Invalidate();
	UninitGraphics();

	if (fWindowVisible && fSelVis)
		MakeSelVisAfterResize(fSelVis);

	return true;
}

/*----------------------------------------------------------------------------------------------
	After resizing the window, make the selection visible.
----------------------------------------------------------------------------------------------*/
void AfVwScrollWndBase::MakeSelVisAfterResize(bool fSelVis)
{
	if (fSelVis)
		ScrollSelectionIntoView(NULL, kssoNearTop);
}


/*----------------------------------------------------------------------------------------------
	Process mouse move (WM_MOUSEMOVE). xp/yp are current coordinates. grfmk identifies
	button/keys pressed.
----------------------------------------------------------------------------------------------*/
bool AfVwScrollWndBase::OnMouseMove(uint grfmk, int xp, int yp)
{
	if (grfmk & MK_LBUTTON)
	{
		Rect rc;
		m_pwndSubclass->GetClientRect(rc);

		// If the mouse position is outside of the client area, send scroll messages.
		if (yp < rc.top)
			OnVScroll(SB_LINEUP, 0, NULL);
		else if (yp > rc.bottom)
			OnVScroll(SB_LINEDOWN, 0, NULL);
		if (xp < rc.left)
			OnHScroll(SB_LINELEFT, 0, NULL);
		else if (xp > rc.right)
			OnHScroll(SB_LINERIGHT, 0, NULL);
	}

	return AfVwRootSite::OnMouseMove(grfmk, xp, yp);
}

/*----------------------------------------------------------------------------------------------
	The public version sets up and clears the graphics object.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::IsSelectionVisible(IVwSelection * psel)
{
	InitGraphics();
	bool fRet = IsSelectionVisible1(psel);
	UninitGraphics();
	return fRet;
}

/*----------------------------------------------------------------------------------------------
	Activate the given keyboard. On Windows 98, sending this message unnecessarily destroys
	the current keystroke context, so only do it when we're actually switching.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::ActivateKeyboard(HKL hkl, UINT nFlags)
{
	if (hkl != m_hklCurr)
	{
#if 99
		StrAnsi sta;
		sta.Format("AfVwRootSite::ActivateKeyboard(%x, %x)\n", hkl, nFlags);
		::OutputDebugStringA(sta.Chars());
#endif
		::ActivateKeyboardLayout(hkl, nFlags);
		m_hklCurr = hkl;
	}
}

/*----------------------------------------------------------------------------------------------
	Retrieve the first language writing system and old writing system used by the given
	selection.
	ENHANCE JohnT(?): Should this be a COM method for IVwSelection?
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::GetWsOfSelection(IVwSelection * pvwsel, int * pwsSel)
{
	AssertPtr(pvwsel);
	AssertPtr(pwsSel);

	int cttp;
	CheckHr(pvwsel->GetSelectionProps(0, NULL, NULL, &cttp));
	if (!cttp)
		return false;
	TtpVec vqttp;
	VwPropsVec vqvps;
	vqttp.Resize(cttp);
	vqvps.Resize(cttp);
	CheckHr(pvwsel->GetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin(),
		(IVwPropertyStore **)vqvps.Begin(), &cttp));
	*pwsSel = 0;
	int ittp;
	ITsTextProps * pttp;
	HRESULT hr = E_FAIL;
	for (ittp = 0; ittp < cttp; ++ittp)
	{
		pttp = vqttp[ittp];
		int var;
		CheckHr(hr = pttp->GetIntPropValues(ktptWs, &var, pwsSel));
		if (hr == S_OK)
			return true;
	}
	// On the offchance this fails, just go with zero. It should be safe because we
	// can't have an editable selection where we would really use the information.
	*pwsSel = 0;
	return false;
}


/*----------------------------------------------------------------------------------------------
	Change the system keyboard when the selection changes.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::HandleSelectionChange(IVwSelection * pvwselNew)
{
#ifdef TRACING_KEYMAN
	OutputDebugStringA("Setting keyboard from AfVwRootSite::HandleSelectionChange\n");
#endif
	SetKeyboardForSelection(pvwselNew);
}

/*----------------------------------------------------------------------------------------------
	Get the writing system factory.
	@param ppwsf pointer to receive writing system factory.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::GetLgWritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	AssertPtr(ppwsf);
	*ppwsf = NULL;
	AfMainWnd * pafw = m_pwndSubclass->MainWindow();
	if (pafw)
		pafw->GetLgWritingSystemFactory(ppwsf);
	if (!*ppwsf)
	{
		AfVwWnd * pavw = dynamic_cast<AfVwWnd *>(m_pwndSubclass);
		if (pavw)
			pavw->GetLgWritingSystemFactory(ppwsf);
	}
	AssertPtr(*ppwsf);
}

/*----------------------------------------------------------------------------------------------
	Set the keyboard to match what is needed for the selection
	Todo JohnT: try some of the following to try to get it working right:
	(1) ignore wm_kmselectlang if wparam != 1. Don't even count it for resetting insetkeyboard.
	(2) Always activate keyman TS, even if ws doesn't use it. (Things seem to work better if it
		is always active...if this works we could do it only if at least one current language
		uses keyman.)
	(3) Use SendMessage instead of PostMessage to switch keyboards, and ignore wm_selectlang
		received during the SendMessage.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::SetKeyboardForSelection(IVwSelection * pvwsel)
{
#ifdef TRACING_KEYMAN
	OutputDebugStringA("AfVwRootSite::SetKeyboardForSelection\n");
#endif
	if (!pvwsel)
		return;			// Can't do anything useful.

	// If there is a pending message indicating a change of keyboard originating from Keyman,
	// don't set the keyboard, as that would destroy the information we want to get from
	// Keyman about the user's selection!
	// ...but this way of detecting the problem didn't help. Presumably Keyman switches the
	// focus back before posting the message.
	//MSG msg;
	//if (PeekMessage(&msg, NULL, s_wm_kmkbchange, s_wm_kmkbchange, PM_NOREMOVE))
	//	return;

	int ws;
	if (!GetWsOfSelection(pvwsel, &ws))
		return;
	SetKeyboardForWs(ws);
}

void AfVwRootSite::SetKeyboardForUI()
{
	// Get the writing system factory associated with the root box.
	if (!m_qrootb)
		return; // For paranoia.
	ISilDataAccessPtr qsdaT;
	CheckHr(m_qrootb->get_DataAccess(&qsdaT));
	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));
	if (qwsf)	// May be NULL if we are quitting the application.
	{
		int ws;
		CheckHr(qwsf->get_UserWs(&ws));
		SetKeyboardForWs(ws);
	}
}

#define Tracing

void AfVwRootSite::SetKeyboardForWs(int ws)
{
	// Get the writing system factory associated with the root box.
	if (!m_qrootb)
		return; // For paranoia.
	ISilDataAccessPtr qsdaT;
	CheckHr(m_qrootb->get_DataAccess(&qsdaT));
	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));
	IWritingSystemPtr qws;

	HKL hkl = reinterpret_cast<HKL>(LANGIDFROMLCID(AfApp::GetDefaultKeyboard()));

	CheckHr(qwsf->get_EngineOrNull(ws, &qws));
	if (!qws)
	{
		ActivateKeyboard(hkl, 0);
		return;
	}

	HRESULT hr;
	BSTR bstrActiveKeymanKbd = ::SysAllocStringLen(
		m_stuActiveKeymanKbd.Chars(), m_stuActiveKeymanKbd.Length());
	int nActiveLangId = m_langIDCurrent;
	int nhklActive = (int)m_hklCurr;
	ComBool fSelectLangPending = m_fSelectLangPending;
	try {
		CheckHr(hr = m_qrootb->SetKeyboardForWs(qws, &bstrActiveKeymanKbd, &nActiveLangId, &nhklActive,
			&fSelectLangPending));
		if (hr == S_OK)
			m_stuActiveKeymanKbd = bstrActiveKeymanKbd;
	}
	catch(Throwable & thr) {
		// Ensure that we release the allocated BSTR before throwing any error!
		::OutputDebugString(thr.Message());
		if (bstrActiveKeymanKbd)
			::SysFreeString(bstrActiveKeymanKbd);
	}
	if (bstrActiveKeymanKbd)
		::SysFreeString(bstrActiveKeymanKbd);

	m_langIDCurrent = (LANGID)nActiveLangId;
	m_hklCurr = (HKL)nhklActive;
	m_fSelectLangPending = fSelectLangPending;
	//	We shouldn't have to do this, but it seems like the WM_INPUTLANGCHANGE message
	//	isn't being send quite soon enough, so we end up with one character using the
	//	wrong code page. So immediately fix the codepage based on the new lang id.
	int nLangId;
	CheckHr(qws->get_CurrentInputLanguage(&nLangId));
	SetCodePageForLangId(nLangId);
}

/*----------------------------------------------------------------------------------------------
	When the user has selected a keyboard from the system tray, adjust the language of the
	selection to something that matches, if possible.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::HandleKeyboardChange(IVwSelection * pvwsel, int nLangId)
{
#ifdef Tracing
	StrAnsi sta;
	sta.Format("AfVwRootSite::HandleKeyboardChange(window %x to %d)%n",
		this->Window()->Hwnd(), nLangId);
	::OutputDebugStringA(sta.Chars());
#endif
	// Don't do any of this if not the focus window (including during OnKillFocus).
	if (!m_fIAmFocussed)
		return;
	// Get the writing system factory associated with the root box.
	if (!m_qrootb)
		return; // For paranoia.
	ISilDataAccessPtr qsdaT;
	CheckHr(m_qrootb->get_DataAccess(&qsdaT));
	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));

	int cws;
	CheckHr(qwsf->get_NumberOfWs(&cws));
	Vector<int> vws;
	vws.Resize(cws + 1);
	CheckHr(qwsf->GetWritingSystems(vws.Begin() + 1, vws.Size() - 1));
	if (cws == 0 || (cws == 1 && vws[1] == 0))
		return;	// no writing systems to work with

	HKL hklDefault = reinterpret_cast<HKL>(LANGIDFROMLCID(AfApp::GetDefaultKeyboard()));
	HKL hklSet = reinterpret_cast<HKL>(nLangId);

	// Put the writing system of the selection first in the list, which gives it priority--
	// we'll find it first if it matches.
	int wsSel;
	if (pvwsel && GetWsOfSelection(pvwsel, &wsSel))
	{
		vws[0] = wsSel;
	}
	else
	{
		vws[0] = vws[1];
	}

	int wsMatch = -1;
	int wsDefault = -1;
	int wsCurrentLang = -1; // used to note first ws whose CurrentInputLanguage matches.
	for (int iws = 0; iws < cws + 1; iws++)
	{
		if (vws[iws] == 0)
			continue;

		IWritingSystemPtr qws;
		CheckHr(qwsf->get_EngineOrNull(vws[iws], &qws));
		if (!qws)
			continue;
		int nLocale;
		// REVIEW SteveMc, SharonC, KenZ, JohnT: nail down where the locale/langid belongs, in
		// the writing system or in the old writing system.
		CheckHr(qws->get_Locale(&nLocale));
		int nLangIdWs = LANGIDFROMLCID(nLocale);

		if (nLangIdWs && nLangIdWs == nLangId)
		{
			wsMatch = vws[iws];
			break;
		}
		if (iws == 0 && !nLangIdWs && nLangId == (int)hklDefault)
		{
			// The writing system of the current selection doesn't have any keyboard specified,
			// and we've set the keyboard to the default. This is acceptable; leave as is.
			wsMatch = vws[iws];
			break;
		}
		if (!nLangIdWs && nLangId == (int)hklDefault && wsDefault == -1)
		{
			// Use this writing system as the default.
			wsDefault = vws[iws];
		}
		if (wsCurrentLang == -1)
		{
			int nLangIdCurrent;
			CheckHr(qws->get_CurrentInputLanguage(&nLangIdCurrent));
			if (nLangId == nLangIdCurrent)
				wsCurrentLang = vws[iws];
		}
	}

	if (wsMatch == -1)
	{
		wsMatch = wsDefault;
	}

	m_wsPending = -1;
	// Next, see if it is the current langid of any ws. This will leave it -1 if we didn't find
	// such a match.
	if (wsMatch == -1)
		wsMatch = wsCurrentLang;
	if (wsMatch == -1)
	{
		// Nothing matched.
		if (hklDefault == hklSet)
		{
			// The default keyboard sets set for odd reasons. Just ignore it.
			// Review: what if the HKL's are different versions of the same language,
			// eg UK and US English?
		}
		else
		{
			// We will make this the current input language for the current writing system for
			// the current session.
			IWritingSystemPtr qwsSel;
			CheckHr(qwsf->get_EngineOrNull(wsSel, &qwsSel));
			if (qwsSel)
				CheckHr(qwsSel->put_CurrentInputLanguage(nLangId));
		}
		return;
	}

	// We are going to make wsMatch the current writing system.
	// Make sure it is set to use the langid that the user just selected.
	// (This cleans up any earlier overrides).
	IWritingSystemPtr qwsMatch;
	CheckHr(qwsf->get_EngineOrNull(wsMatch, &qwsMatch));
	if (qwsMatch)
		CheckHr(qwsMatch->put_CurrentInputLanguage(nLangId));


	if (!pvwsel)
	{
		// Delay handling it until we get a selection.
		m_wsPending = wsMatch;
		return;
	}
	ComBool fRange;
	CheckHr(pvwsel->get_IsRange(&fRange));
	if (fRange)
	{
		// Delay handling it until we get an insertion point.
		m_wsPending = wsMatch;
		return;
	}

	TtpVec vqttp;
	VwPropsVec vqvps;
	int cttp;
	CheckHr(pvwsel->GetSelectionProps(0, NULL, NULL, &cttp));
	if (!cttp)
		return;
	vqttp.Resize(cttp);
	vqvps.Resize(cttp);
	CheckHr(pvwsel->GetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin(),
		(IVwPropertyStore **)vqvps.Begin(), &cttp));

	// If nothing changed, avoid the infinite loop that happens when we change the selection
	// and update the system keyboard, which in turn tells the program to change its writing
	// system.
	// (This is a problem on Windows 98.)
	bool fChanged = false;
	int wsTmp;
	int var;
	for (int ittp = 0; ittp < cttp; ittp++)
	{
		CheckHr(vqttp[ittp]->GetIntPropValues(ktptWs, &var, &wsTmp));
		if (wsTmp != wsMatch)
			fChanged = true;
	}
	if (!fChanged)
		return;

	Assert(cttp == 1);
	ITsPropsBldrPtr qtpb;
	ITsTextPropsPtr qttp;
	CheckHr(vqttp[0]->GetBldr(&qtpb));
	CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, wsMatch));
	CheckHr(qtpb->GetTextProps(&vqttp[0]));
	CheckHr(pvwsel->SetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin()));
	CheckHr(SelectionChanged(m_qrootb, pvwsel));
}

/*----------------------------------------------------------------------------------------------
	Scroll to make the selection visible.
	In general, scroll the minimum distance to make it entirely visible.
	If the selection is higher than the window, scroll the minimum distance to make it
	fill the window.
	If the window is too small to show both primary and secondary, show primary.
	Note: subclasses for which scrolling is disabled should override.
	If psel is null, make the current selection visible.
----------------------------------------------------------------------------------------------*/
void AfVwScrollWndBase::MakeSelectionVisible1(IVwSelection * psel)
{
	//Assert(m_fVScrollEnabled);
	IVwSelectionPtr qvwsel;
	if (!m_qrootb)
		return; // For paranoia.
	if (psel)
		qvwsel = psel;
	else
	{
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
		{
			return; // Nothing we can do.
		}
	}
	Rect rdPrimary;
	Rect rdSecondary;
	ComBool fSplit;
	ComBool fEndBeforeAnchor;
	Rect rcSrcRoot;
	Rect rcDstRoot;
	Rect rdIdeal;
	HoldGraphics hg(this);
	GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
	CheckHr(qvwsel->Location(m_qvg, rcSrcRoot, rcDstRoot, &rdPrimary, &rdSecondary, &fSplit,
		&fEndBeforeAnchor));
	rdIdeal = rdPrimary;

	Rect rcClient;
	m_pwndSubclass->GetClientRect(rcClient);
	if (fSplit)
	{
		rdIdeal.Sum(rdSecondary);
		if (m_fVScrollEnabled && rdIdeal.Height() > rcClient.Height())
			rdIdeal = rdPrimary; // Revert to just showing main IP.
		if (m_fHScrollEnabled && rdIdeal.Width() > rcClient.Width())
			rdIdeal = rdPrimary;
	}
	rdIdeal.top = rdIdeal.top - 1; // for good measure
	rdIdeal.bottom = rdIdeal.bottom + 1;
	// OK, we want rdIdeal to be visible.
	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_ALL, 0, 0, 0, 0 };

	// dy gets added to the scroll offset. This means a positive dy causes there to be more
	// of the view hidden above the top of the screen. This is the same effect as clicking a
	// down arrow, which paradoxically causes the window contents to move up.
	int dy = 0;
	m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);
	int ydTop = sinfo.nPos; // Where the window thinks it is now.
	if (m_fVScrollEnabled)
	{
		// This loop repeats until we have figured out a scroll distance AND confirmed
		// that we can draw that location without messing things up.
		for (;;)
		{
			rdIdeal.Offset(0, ydTop); // Was in drawing coords, adjusted by top.
			// Adjust for height of the (optional) header.
			rdIdeal.top -= m_dyHeader;
			rdIdeal.bottom -= m_dyHeader;
			int ydBottom = ydTop + rcClient.Height() - m_dyHeader;

			// Is the selection partly off the top of the screen?
			if (rdIdeal.top < ydTop)
			{
				// Is it bigger than the screen?
				if (rdIdeal.Height() > rcClient.Height() && !fEndBeforeAnchor)
				{
					// Top is off, and though it is too big to show entirely, we can show
					// more. Move the window contents down (negative dy).
					dy = rdIdeal.bottom - ydBottom;
				}
				else
				{
					// Partly off top, and fits: move window contents down (less is hidden, neg
					// dy).
					dy = rdIdeal.top - ydTop;
				}
			}
			else
			{
				// Top of selection is below (or at) top of screen.
				// Is bottom of selection below bottom of screen?
				if (rdIdeal.bottom > ydBottom)
				{
					if (rdIdeal.Height() > rcClient.Height() && fEndBeforeAnchor)
					{
						// Top is visible, bottom isn't: move until tops coincide to show as
						// much as possible. This is hiding more text above the top of the
						// window: positive dy.
						dy = rdIdeal.top - ydTop;
					}
					else
					{
						// Fits entirely: scroll up minimum to make bottom visible. This
						// involves hiding more text at the top: positive dy.
						dy = rdIdeal.bottom - ydBottom;
					}
				}
				// Else it is already entirely visible, do nothing. (But still make sure we can
				// draw the requisite screen full without messing stuff up. Just in case a
				// previous lazy box expansion looked like making it visible, but another makes
				// it invisible again...I'm not sure this can happen, but play safe.)
			}
			// OK, we need to move by dy. But, if that puts the selection near the bottom of the
			// screen, we may have to expand a lazy box above it in order to display a whole
			// screen full. If the size estimate is off (which it usually is), that would affect
			// the position' where the selection gets moved to. To avoid this, we make the same
			// PrepareToDraw call that the rendering code will make before drawing after the
			// scroll.
			HoldGraphics hg(this);
			hg.m_rcDstRoot.Offset(0, -dy); // Want to draw at the position we plan to move to.

			int dyRange = sinfo.nMax;

			if (m_qrootb.Ptr() && (m_dxdLayoutWidth > 0))
			{
				VwPrepDrawResult xpdr = kxpdrAdjust;
				CheckHr(m_qrootb->PrepareToDraw(m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &xpdr));
				// We can ignore the result because the code below checks whether the scroll
				// range was affected and loops if not.
			}
			m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);
			ydTop = sinfo.nPos; // Where the window thinks it is now.
			// If PrepareToDraw didn't change the scroll range, it didn't mess anything up and
			// we can use the dy we figured. Otherwise, loop and figure it again with more
			// complete information, because something at a relevant point has been expanded to
			// real boxes.
			if (sinfo.nMax == dyRange)
				break;
			// Otherwise we need another iteration, we need to recompute the selection location
			// in view of the changes to layout.
			GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
			CheckHr(qvwsel->Location(m_qvg, rcSrcRoot, rcDstRoot, &rdPrimary, &rdSecondary,
				&fSplit, &fEndBeforeAnchor));
			rdIdeal = rdPrimary;
			dy = 0; // Back to initial state.

			if (fSplit)
			{
				rdIdeal.Sum(rdSecondary);
				if (m_fVScrollEnabled && rdIdeal.Height() > rcClient.Height())
					rdIdeal = rdPrimary; // Revert to just showing main IP.
				if (m_fHScrollEnabled && rdIdeal.Width() > rcClient.Width())
					rdIdeal = rdPrimary;
			}
		}
		rdIdeal.top = rdIdeal.top - 1;
		rdIdeal.bottom = rdIdeal.bottom + 1;
		if (dy > sinfo.nMax - (int)(sinfo.nPage) - 1 - sinfo.nPos)
		{
			// This value makes it the maximum it can be, except this may make it negative
			dy = sinfo.nMax - (int)(sinfo.nPage) - 1 - sinfo.nPos;
		}
		if (dy + sinfo.nPos < 0)
			dy = -sinfo.nPos; // make offset 0 if it would have been less than that

		if (dy)
		{
			// Update the scroll bar.
			sinfo.fMask = SIF_POS;
			sinfo.nPos = ydTop + dy;
			SetRootSiteScrollInfo(SB_VERT, &sinfo, true);
		}
	}

	// dx gets added to the scroll offset. This means a positive dx causes there to be more
	// of the view hidden left of the screen. This is the same effect as clicking a
	// right arrow, which paradoxically causes the window contents to move left.
	int dx = 0;
	m_pwndSubclass->GetScrollInfo(SB_HORZ, &sinfo);
	int xdLeft = sinfo.nPos; // Where the window thinks it is now.
	if (m_fHScrollEnabled)
	{
		if (m_pomgr->IsVertical())
		{
			// In all current vertical views we have no vertical scrolling, so only need
			// to consider horizontal. Also we have no laziness, so no need to mess with
			// possible effects of expanding lazy boxes that become visible.
			// In this case, rcPrimary's top is the distance from the right of the ClientRect to the
			// right of the selection, and the height of rcPrimary is a distance further left.
			int right = rdIdeal.top; // distance to left of right edge of window
			int left = right + rdIdeal.Height();
			if (right < 0)
			{
				// selection is partly off the right of the window
				dx = -right; // positive dx to move window contents left.
			}
			else if (left > rcClient.Width())
			{
				dx = rcClient.Width() - left; // negative to move window contents right
			}
		}
		else // normal view
		{
			rdIdeal.Offset(xdLeft, 0); // Was in drawing coords, adjusted by left.
			int xdRight = xdLeft + rcClient.Width();

			// Is the selection partly off the left of the screen?
			if (rdIdeal.left < xdLeft)
			{
				// Is it bigger than the screen?
				if (rdIdeal.Width() > rcClient.Width() && !fEndBeforeAnchor)
				{
					// Left is off, and though it is too big to show entirely, we can show
					// more. Move the window contents right (negative dx).
					dx = rdIdeal.right - xdRight;
				}
				else
				{
					// Partly off left, and fits: move window contents right (less is hidden,
					// neg dx).
					dx = rdIdeal.left - xdLeft;
				}
			}
			else
			{
				// Left of selection is right of (or at) the left side of the screen.
				// Is right of selection right of the right side of the screen?
				if (rdIdeal.right > xdRight)
				{
					if (rdIdeal.Width() > rcClient.Width() && fEndBeforeAnchor)
					{
						// Left is visible, right isn't: move until lefts coincide to show as much
						// as possible. This is hiding more text left of the window: positive dx.
						dx = rdIdeal.left - xdLeft;
					}
					else
					{
						// Fits entirely: scroll left minimum to make right visible. This involves
						// hiding more text at the left: positive dx.
						dx = rdIdeal.right - xdRight;
					}
				}
				// Else it is already entirely visible, do nothing.
			}
		}
		if (dx > sinfo.nMax - (int)(sinfo.nPage) - 1 - sinfo.nPos)
		{
			// This value makes it the maximum it can be, except this may make it negative
			dx = sinfo.nMax - (int)(sinfo.nPage) - 1 - sinfo.nPos;
		}
		if (dx + sinfo.nPos < 0)
			dx = -sinfo.nPos; // make offset 0 if it would have been less than that
		if (dx)
		{
			// Update the scroll bar.
			sinfo.fMask = SIF_POS;
			sinfo.nPos = xdLeft + dx;
			SetRootSiteScrollInfo(SB_HORZ, &sinfo, true);
		}
	}
	ScrollBy(dx, dy);
}

/*----------------------------------------------------------------------------------------------
	Put the top of the selection at about 1/4 down the window, and all the way to the left
	(undo any horizontal scrolling).
	Modeled after MakeSelectionVisible1
----------------------------------------------------------------------------------------------*/
void AfVwScrollWndBase::ScrollSelectionNearTop1(IVwSelection * psel)
{
	//Assert(m_fVScrollEnabled);
	IVwSelectionPtr qvwsel;
	if (!m_qrootb)
	{
		return; // For paranoia.
	}
	if (psel)
		qvwsel = psel;
	else
	{
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
		{
			return; // Nothing we can do.
		}
	}
	Rect rdPrimary;
	Rect rdSecondary;
	ComBool fSplit;
	ComBool fEndBeforeAnchor;
	Rect rcSrcRoot;
	Rect rcDstRoot;
	Rect rdIdeal;
	HoldGraphics hg(this);
	GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
	CheckHr(qvwsel->Location(m_qvg, rcSrcRoot, rcDstRoot, &rdPrimary, &rdSecondary, &fSplit,
		&fEndBeforeAnchor));
	rdIdeal = rdPrimary;

	Rect rcClient;
	m_pwndSubclass->GetClientRect(rcClient);
	if (fSplit)
	{
		rdIdeal.Sum(rdSecondary);
		if (m_fVScrollEnabled && rdIdeal.Height() > rcClient.Height())
			rdIdeal = rdPrimary; // Revert to just showing main IP.
		if (m_fHScrollEnabled && rdIdeal.Width() > rcClient.Width())
			rdIdeal = rdPrimary;
	}
	rdIdeal.top = rdIdeal.top - 1; // for good measure
	rdIdeal.bottom = rdIdeal.bottom + 1;
	// OK, we want rdIdeal to be visible.
	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_ALL, 0, 0, 0, 0 };

	// dy gets added to the scroll offset. This means a positive dy causes there to be more
	// of the view hidden above the top of the screen. This is the same effect as clicking a
	// down arrow, which paradoxically causes the window contents to move up.
	int dy = 0;
	m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);
	int ydTop = sinfo.nPos; // Where the window thinks it is now.
	if (m_fVScrollEnabled)
	{
		// This loop repeats until we have figured out a scroll distance AND confirmed
		// that we can draw that location without messing things up.
		for (;;)
		{
			rdIdeal.Offset(0, ydTop); // Was in drawing coords, adjusted by top.
			// Adjust for height of the (optional) header.
			rdIdeal.top -= m_dyHeader;
			rdIdeal.bottom -= m_dyHeader;

			// We want to make an adjustment if the selection is more than 3/4 of
			// the way down, or above the top of the window.
			if ((rdIdeal.top - ydTop) > ((rcClient.Height() * 3) / 4) ||
				rdIdeal.top < ydTop)
			{
				int nPosWanted = rdIdeal.top - (rcClient.Height() / 4);
				dy = nPosWanted - ydTop;
			}
			else
				dy = 0;

			// OK, we need to move by dy. But, if that puts the selection near the bottom of the
			// screen, we may have to expand a lazy box above it in order to display a whole
			// screen full. If the size estimate is off (which it usually is), that would affect
			// the position' where the selection gets moved to. To avoid this, we make the same
			// PrepareToDraw call that the rendering code will make before drawing after the
			// scroll.
			HoldGraphics hg(this);
			hg.m_rcDstRoot.Offset(0, -dy); // Want to draw at the position we plan to move to.

			int dyRange = sinfo.nMax;

			if (m_qrootb.Ptr() && (m_dxdLayoutWidth > 0))
			{
				VwPrepDrawResult xpdr = kxpdrAdjust;
				CheckHr(m_qrootb->PrepareToDraw(m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &xpdr));
				// We can ignore the result because the code below checks whether the scroll
				// range was affected and loops if not.
			}
			m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);
			ydTop = sinfo.nPos; // Where the window thinks it is now.
			// If PrepareToDraw didn't change the scroll range, it didn't mess anything up and
			// we can use the dy we figured. Otherwise, loop and figure it again with more
			// complete information, because something at a relevant point has been expanded to
			// real boxes.
			if (sinfo.nMax == dyRange)
				break;
			// Otherwise we need another iteration, we need to recompute the selection location
			// in view of the changes to layout.
			GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
			CheckHr(qvwsel->Location(m_qvg, rcSrcRoot, rcDstRoot, &rdPrimary, &rdSecondary,
				&fSplit, &fEndBeforeAnchor));
			rdIdeal = rdPrimary;
			dy = 0; // Back to initial state.

			if (fSplit)
			{
				rdIdeal.Sum(rdSecondary);
				if (m_fVScrollEnabled && rdIdeal.Height() > rcClient.Height())
					rdIdeal = rdPrimary; // Revert to just showing main IP.
				if (m_fHScrollEnabled && rdIdeal.Width() > rcClient.Width())
					rdIdeal = rdPrimary;
			}
		}
		rdIdeal.top = rdIdeal.top - 1;
		rdIdeal.bottom = rdIdeal.bottom + 1;
		if (dy > sinfo.nMax - (int)(sinfo.nPage) - 1 - sinfo.nPos)
		{
			// This value makes it the maximum it can be, except this may make it negative
			dy = sinfo.nMax - (int)(sinfo.nPage) - 1 - sinfo.nPos;
		}
		if (dy + sinfo.nPos < 0)
			dy = -sinfo.nPos; // make offset 0 if it would have been less than that

		if (dy)
		{
			// Update the scroll bar.
			sinfo.fMask = SIF_POS;
			sinfo.nPos = ydTop + dy;
			SetRootSiteScrollInfo(SB_VERT, &sinfo, true);
		}
	}

#ifdef JohnT10_16_2002RestoreHScrollOnFind
	// The following was present and causes there to be no h scrolling at all, contradicting
	// the method comment.

	// dx gets added to the scroll offset. This means a positive dx causes there to be more
	// of the view hidden left of the screen. This is the same effect as clicking a
	// right arrow, which paradoxically causes the window contents to move left.
	int dx = 0;
	m_pwndSubclass->GetScrollInfo(SB_HORZ, &sinfo);
//tccp	dx = -sinfo.nPos;
#else
	// This is what Find wants: make sure it can be seen, even by h scrolling.

	// dx gets added to the scroll offset. This means a positive dx causes there to be more
	// of the view hidden left of the screen. This is the same effect as clicking a
	// right arrow, which paradoxically causes the window contents to move left.
	int dx = 0;
	m_pwndSubclass->GetScrollInfo(SB_HORZ, &sinfo);
	int xdLeft = sinfo.nPos; // Where the window thinks it is now.
	if (m_fHScrollEnabled)
	{
		rdIdeal.Offset(xdLeft, 0); // Was in drawing coords, adjusted by left.
		int xdRight = xdLeft + rcClient.Width();

		// Is the selection partly off the left of the screen?
		if (rdIdeal.left < xdLeft)
		{
			// Is it bigger than the screen?
			if (rdIdeal.Width() > rcClient.Width() && !fEndBeforeAnchor)
			{
				// Left is off, and though it is too big to show entirely, we can show
				// more. Move the window contents right (negative dx).
				dx = rdIdeal.right - xdRight;
			}
			else
			{
				// Partly off left, and fits: move window contents right (less is hidden,
				// neg dx).
				dx = rdIdeal.left - xdLeft;
			}
		}
		else
		{
			// Left of selection is right of (or at) the left side of the screen.
			// Is right of selection right of the right side of the screen?
			if (rdIdeal.right > xdRight)
			{
				if (rdIdeal.Width() > rcClient.Width() && fEndBeforeAnchor)
				{
					// Left is visible, right isn't: move until lefts coincide to show as much
					// as possible. This is hiding more text left of the window: positive dx.
					dx = rdIdeal.left - xdLeft;
				}
				else
				{
					// Fits entirely: scroll left minimum to make right visible. This involves
					// hiding more text at the left: positive dx.
					dx = rdIdeal.right - xdRight;
				}
			}
			// Else it is already entirely visible, do nothing.
		}
		if (dx > sinfo.nMax - (int)(sinfo.nPage) - 1 - sinfo.nPos)
		{
			// This value makes it the maximum it can be, except this may make it negative
			dx = sinfo.nMax - (int)(sinfo.nPage) - 1 - sinfo.nPos;
		}
		if (dx + sinfo.nPos < 0)
			dx = -sinfo.nPos; // make position 0 if it would have been less than that
		if (dx)
		{
			// Update the scroll bar.
			sinfo.fMask = SIF_POS;
			sinfo.nPos = xdLeft + dx;
			SetRootSiteScrollInfo(SB_HORZ, &sinfo, true);
		}
	}
#endif
	ScrollBy(dx, dy);
}

/*----------------------------------------------------------------------------------------------
	Scroll to the top.
----------------------------------------------------------------------------------------------*/
void AfVwScrollWndBase::ScrollToTop()
{
	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_ALL, 0, 0, 0, 0 };

	m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);
	int ydTop = sinfo.nPos; // Where the window thinks it is now.
	if (m_fVScrollEnabled)
	{
		sinfo.fMask = SIF_POS;
		sinfo.nPos = 0;
		SetRootSiteScrollInfo(SB_VERT, &sinfo, true);
		ScrollBy(0, - ydTop);
	}
}

/*----------------------------------------------------------------------------------------------
	Scroll to the bottom. This is somewhat tricky because after scrolling to the bottom of
	the range as we currently estimate it, expanding a closure may change things.
----------------------------------------------------------------------------------------------*/
void AfVwScrollWndBase::ScrollToEnd()
{
	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_ALL, 0, 0, 0, 0 };

	// dy gets added to the scroll offset. This means a positive dy causes there to be more
	// of the view hidden above the top of the screen. This is the same effect as clicking a
	// down arrow, which paradoxically causes the window contents to move up.
	int dy = 0;
	m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);
	int ydCurr = sinfo.nPos; // Where the window thinks it is now.
	if (m_fVScrollEnabled)
	{
		// This loop repeats until we have figured out a scroll distance AND confirmed
		// that we can draw that location without messing things up.
		for (;;)
		{
			// The maximum possible scroll bar setting is the total document length minus one
			// pixel less than a page...or zero, if that calculation produces a negative.
			int ydMax = max(sinfo.nMax - (sinfo.nPage - 1), 0);
			dy = ydMax - ydCurr;
			// OK, we need to move by dy. But, we may have to expand a lazy box there in order
			// to display a whole screen full. If the size estimate is off (which it usually
			// is), that would affect the scroll position we need to be at the very bottom.
			// To avoid this, we make the same PrepareToDraw call
			// that the rendering code will make before drawing after the scroll.
			HoldGraphics hg(this);
			hg.m_rcDstRoot.Offset(0, -dy); // Want to draw at the position we plan to move to.

			int dyRange = sinfo.nMax;

			if (m_qrootb.Ptr() && (m_dxdLayoutWidth > 0))
			{
				VwPrepDrawResult xpdr = kxpdrAdjust;
				CheckHr(m_qrootb->PrepareToDraw(m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &xpdr));
				// We can ignore the result because the code below checks whether the scroll
				// range was affected and loops if not.
			}
			m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);
			ydCurr = sinfo.nPos; // Where the window thinks it is now. (May have changed
								// expanding.)
			// If PrepareToDraw didn't change the scroll range, it didn't mess anything up and
			// we can use the dy we figured. Otherwise, loop and figure it again with more
			// complete information, because something at a relevant point has been expanded to
			// real boxes.
			if (sinfo.nMax == dyRange)
				break;
			dy = 0; // Back to initial state.
		}
#ifdef JohnT_10Dec02_3638Fix
		// A previous attempt to fix scrolling above the top position if the view is smaller
		// than the window. A better fix it to compute ydMax correctly, as is now done above, so
		// never negative.
		if ((int64)sinfo.nPage > (int64)sinfo.nMax)
		{
			// The whole page is visible already, and there is white space at the bottom.
			// RAID #3638 demonstrated that Windows will attempt to move this space up the
			// window and obliterate other data, if we continue now:
			return;
		}
#endif
		if (dy)
		{
			// Update the scroll bar.
			sinfo.fMask = SIF_POS;
			sinfo.nPos = ydCurr + dy;
			SetRootSiteScrollInfo(SB_VERT, &sinfo, true);
		}
		ScrollBy(0, dy);
	}
}


/*----------------------------------------------------------------------------------------------
	Test whether the (primary part of the selection is visible. If the argument is null
	test the current selection.
----------------------------------------------------------------------------------------------*/
bool AfVwScrollWndBase::IsSelectionVisible1(IVwSelection * psel)
{
	IVwSelectionPtr qvwsel;
	if (!m_qrootb)
	{
		return false; // For paranoia.
	}
	if (psel)
		qvwsel = psel;
	else
	{
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
		{
			return false; // Nothing we can test.
		}
	}
	Rect rdPrimary;
	Rect rdSecondary;
	ComBool fSplit;
	ComBool fEndBeforeAnchor;
	Rect rcSrcRoot;
	Rect rcDstRoot;
	Rect rdIdeal;
	GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
	CheckHr(qvwsel->Location(m_qvg, rcSrcRoot, rcDstRoot, &rdPrimary, &rdSecondary, &fSplit,
		&fEndBeforeAnchor));
	rdIdeal = rdPrimary;

	Rect rcClient;
	m_pwndSubclass->GetClientRect(rcClient);
	// OK, we want rdIdeal to be visible.
	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_ALL, 0, 0, 0, 0 };

	m_pwndSubclass->GetScrollInfo(SB_VERT, &sinfo);
	int ydTop = sinfo.nPos; // Where the window thinks it is now.
	rdIdeal.Offset(0, ydTop); // Was in drawing coords, adjusted by top.
	// Adjust for height of the (optional) header.
	rdIdeal.top -= m_dyHeader;
	rdIdeal.bottom -= m_dyHeader;
	int ydBottom = ydTop + rcClient.Height() - m_dyHeader;

	// Does the selection rectangle overlap the screen one?
	// Note that if we support horizontal scrolling we will need to enhance this.
	if (rdIdeal.top < ydTop)
	{
		return rdIdeal.bottom > ydTop;
	}
	return rdIdeal.top < ydBottom;
}

/*----------------------------------------------------------------------------------------------
	Update the paragraph direction based on the first character in the paragraph, or the
	old writing system of the selection.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::UpdateParaDirection(IVwSelection * pvwselNew)
{
	return false;
}

/*----------------------------------------------------------------------------------------------
	ENHANCE JohnT(?): maybe factor out common code?
	Test this when we have a display that needs horizontal scrolling.
----------------------------------------------------------------------------------------------*/
bool AfVwScrollWndBase::OnHScroll(int nSBCode, int nPos, HWND hwndSbar)
{
	// NB - DON'T use nPos; it has only a 16-bit range.
	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_ALL, 0, 0, 0, 0};
	m_pwndSubclass->GetScrollInfo(SB_HORZ, &sinfo);
	int dxdPos = sinfo.nPos; // Where the window thinks it is now.
	int dxdTrackPos = sinfo.nTrackPos; // Where the user dragged to.
	int dxdMax = sinfo.nMax; // Min is always 0 in VwWindow.
	int dxdPage = sinfo.nPage;
	// ENHANCE JohnT: use actual resolution.
	int dxdLine = 30 * 96/72; // 30 points seems a useful size increment.

	if (dxdPage > dxdLine * 2)
		dxdPage -= dxdLine;  // Page scroll by a line less than a complete page.

	switch (nSBCode)
	{
	case SB_LEFT:
		dxdPos = dxdMax; // Too large, but corrected like any out of range below.
		break;
	case SB_ENDSCROLL: // User released after holding in button; do nothing.
		break;
	case SB_LINELEFT:
		dxdPos -= dxdLine;
		break;
	case SB_LINERIGHT:
		dxdPos += dxdLine;
		break;
	case SB_PAGELEFT:
		dxdPos -= dxdPage;
		break;
	case SB_PAGERIGHT:
		dxdPos += dxdPage;
		break;
	case SB_THUMBTRACK: // REVIEW JohnT(?): are we generally able to draw fast enough for this?
	case SB_THUMBPOSITION:
		dxdPos = dxdTrackPos;
		break; // dxdPos is already correct (but display may need adjusting).
	case SB_RIGHT:
		dxdPos = 0;
		break;
	}
	// Max legal value (see MSDN\Platform SDK\User interface services\Controls\
	// Scroll bars\About scroll bars\Scroll box position and scrolling range).
	if (dxdPos > dxdMax - (int)(sinfo.nPage) - 1)
		dxdPos = dxdMax - (int)(sinfo.nPage) - 1;
	// Check this AFTER checking max, it is possible that dydMax < page size and we
	// just made it negative.
	if (dxdPos < 0)
		dxdPos = 0;

	int dxdScrollBy = dxdPos - sinfo.nPos;

	// Update the scroll bar.
	sinfo.fMask = SIF_POS;
	sinfo.nPos = dxdPos;
	SetRootSiteScrollInfo(SB_HORZ, &sinfo, true);

	ScrollBy(dxdScrollBy, 0);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Remove yourself from the message handling queue.
----------------------------------------------------------------------------------------------*/
bool AfVwScrollWndBase::OnKillFocus(HWND hwndNew)
{
	return AfVwRootSite::OnKillFocus(hwndNew);
}

/*----------------------------------------------------------------------------------------------
	Handle window painting (WM_PAINT).
----------------------------------------------------------------------------------------------*/
bool AfVwScrollWndBase::OnPaint(HDC hdcDef)
{
	HDC hdc;
	HDC hdcMem;
	HBITMAP hbmpOld;
	PAINTSTRUCT ps;
	Rect rcp;

	m_pwndSubclass->GetClientRect(rcp);

	if (hdcDef)
	{
		Draw(hdcDef, rcp);
	}
	else
	{
		hdc = ::BeginPaint(m_pwndSubclass->Hwnd(), &ps);
		// rcp.Intersect(ps.rcPaint);
		hdcMem = AfGdi::CreateCompatibleDC(hdc);
		Rect rcpBitmap;
		rcpBitmap.top = rcpBitmap.left = 0;
		HBITMAP hbmp;
		if (IsVertical())
		{
			rcpBitmap.bottom = rcp.Width();
			rcpBitmap.right = rcp.Height();
		}
		else
		{
			rcpBitmap.right = rcp.Width();
			rcpBitmap.bottom = rcp.Height();
		}
			hbmp = AfGdi::CreateCompatibleBitmap(hdc, rcpBitmap.Width(), rcpBitmap.Height());
		Assert(hbmp);
		hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
		Assert(hbmpOld && hbmpOld != HGDI_ERROR);
		AfGfx::FillSolidRect(hdcMem, rcpBitmap, GetWindowColor());
		if (IsVertical())
		{
			// We need to transform rcp to the bit we want to draw in drawing coords, draw it,
			// Then blit it onto the destination rotated.
			Rect rcpRotated = RotateRectDstToPaint(rcp);
			Draw(hdcMem, rcpRotated);

			POINT rgptTransform[3];
			rgptTransform[0].x = rcp.right;
			rgptTransform[0].y = rcp.top;	// upper left of actual drawing maps to top right of rotated drawing
			rgptTransform[1].x = rcp.right;
			rgptTransform[1].y = rcp.bottom; // upper right of actual drawing maps to bottom right of rotated drawing.
			rgptTransform[2].x = rcp.left;
			rgptTransform[2].y = rcp.top; // bottom left of actual drawing maps to top left of rotated drawing.
				// We drew something...now blast it onto the screen.
			::PlgBlt(hdc, rgptTransform, hdcMem, 0, 0, rcp.Height(), rcp.Width(), 0, 0, 0);
		}
		else
		{
			Draw(hdcMem, rcp);

			// Splat the memory DC onto the screen.
			::BitBlt(hdc, rcp.left, rcp.top, rcp.Width(), rcp.Height(), hdcMem, 0, 0, SRCCOPY);
		}
		// Clean up memory DC.
		HBITMAP hbmpDebug;
		hbmpDebug = AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);
		Assert(hbmpDebug && hbmpDebug != HGDI_ERROR);
		Assert(hbmpDebug == hbmp);

		BOOL fSuccess;
		fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
		Assert(fSuccess);

		fSuccess = AfGdi::DeleteDC(hdcMem);
		Assert(fSuccess);

		::EndPaint(m_pwndSubclass->Hwnd(), &ps);
	}

	return true; // Pass the message on.
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfVwScrollWndBase::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	Assert(!lnRet);

	if (wm == WM_KEYDOWN)
	{
		if (wp == VK_PRIOR)
		{
			SHORT nShift = ::GetKeyState(VK_SHIFT);
			SHORT nControl = ::GetKeyState(VK_CONTROL);
			VwShiftStatus ss = kfssNone;
			if (nShift < 0)
			{
				if (nControl < 0)
					ss = kgrfssShiftControl;
				else
					ss = kfssShift;
			}
			else if (nControl < 0)
			{
				ss = kfssControl;
			}
			OnVScroll(SB_PAGEUP, ss, m_pwndSubclass->Hwnd());
			return true;
		}
		else if (wp == VK_NEXT)
		{
			SHORT nShift = ::GetKeyState(VK_SHIFT);
			SHORT nControl = ::GetKeyState(VK_CONTROL);
			VwShiftStatus ss = kfssNone;
			if (nShift < 0)
			{
				if (nControl < 0)
					ss = kgrfssShiftControl;
				else
					ss = kfssShift;
			}
			else if (nControl < 0)
			{
				ss = kfssControl;
			}
			OnVScroll(SB_PAGEDOWN, ss, m_pwndSubclass->Hwnd());
			return true;
		}
	}
	if (AfVwRootSite::FWndProc(wm, wp, lp, lnRet))
		return true;

	switch (wm)
	{
	case WM_VSCROLL:
		return OnVScroll(LOWORD(wp), HIWORD(wp), (HWND)lp);

	case WM_HSCROLL:
		return OnHScroll(LOWORD(wp), HIWORD(wp), (HWND)lp);

	case WM_PAINT:
		return OnPaint((HDC)wp);
	}

	return false;
}

//:>********************************************************************************************
//:>	AfVwSplitChild methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfVwSplitChild::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	Assert(!lnRet);

	if (AfVwScrollWndBase::FWndProc(wm, wp, lp, lnRet))
		return true;

	return SuperClass::FWndProc(wm, wp, lp, lnRet); // Pass message on.
}


/*----------------------------------------------------------------------------------------------
	Scrolls down by dxdOffset, right by dydOffset (or reverse if negative).
	That is, they are the amounts being added to the scroll bar position.
----------------------------------------------------------------------------------------------*/
void AfVwSplitChild::ScrollBy(int dxdOffset, int dydOffset)
{
	Rect rcScroll;
	if (dxdOffset && dydOffset)
	{
		// With two offsets we don't need the rectangle (and some subclasses can't compute it)
		SuperClass::ScrollBy(dxdOffset, dydOffset, NULL);
	}
	else
	{
		GetScrollRect(-dxdOffset, -dydOffset, rcScroll);
		SuperClass::ScrollBy(dxdOffset, dydOffset, &rcScroll);
	}
}


/*----------------------------------------------------------------------------------------------
	Process sizing (WM_SIZE). wst is the type of sizing requested. dxp is the new width, dyp
	is the new height.
----------------------------------------------------------------------------------------------*/
bool AfVwSplitChild::OnSize(int wst, int dxp, int dyp)
{
	AfVwScrollWndBase::OnSize(wst, dxp, dyp);
	bool result = SuperClass::OnSize(wst, dxp, dyp);
	AfVwSplitChild * vswOther = dynamic_cast<AfVwSplitChild *>(OtherPane());
	if (vswOther)
	{
		// its root is already resized. Adjust the available width field so it does not do
		// any substantial work when it gets the resize message.
		vswOther->m_dxdLayoutWidth = m_dxdLayoutWidth;
	}
	return result;
}

/*----------------------------------------------------------------------------------------------
	If this pane is one half of a split that is sharing the same root box, return the other one.
	Otherwise return NULL.
----------------------------------------------------------------------------------------------*/
AfVwRootSite * AfVwSplitChild::OtherPane()
{
	// No m_qspfl is anomolous, but it can happen if the timer for flash insertion point
	// happens while we are in the process of shutting down.
	if (!m_qsplf)
		return NULL;
	AssertObj(m_qsplf);
	AssertObj(this);
	AfSplitChild * pane0 = m_qsplf->GetPane(0);
	AfVwSplitChild * paneOther = dynamic_cast<AfVwSplitChild *>(pane0);
	if (paneOther == this)
	{
		paneOther = dynamic_cast<AfVwSplitChild *>(m_qsplf->GetPane(1));
	}
	else
		Assert(m_qsplf->GetPane(1) == this);

	if (paneOther && paneOther->m_qrootb == m_qrootb)
		return paneOther;
	else
		return NULL; // no other pane, or not sharing the same root.
}

/*----------------------------------------------------------------------------------------------
	It is the selection of the current pane that should be automatically scrolled into view.
----------------------------------------------------------------------------------------------*/
void AfVwSplitChild::MakeSelectionVisible1(IVwSelection * psel)
{
	if (IsCurrentPane())
		ScrollSuperClass::MakeSelectionVisible1(psel);
	else
		OtherPane()->MakeSelectionVisible1(psel);
}

/*----------------------------------------------------------------------------------------------
	Move the selection near the top of the window.
	It is the selection of the current pane that should be scrolled.
----------------------------------------------------------------------------------------------*/
void AfVwSplitChild::ScrollSelectionNearTop1(IVwSelection * psel)
{
	if (IsCurrentPane())
		ScrollSuperClass::ScrollSelectionNearTop1(psel);
	else
		OtherPane()->ScrollSelectionNearTop1(psel);
}

/*----------------------------------------------------------------------------------------------
	Make the second child (passed as argument) a clone of this one by copying the root box.
----------------------------------------------------------------------------------------------*/
void AfVwSplitChild::CopyRootTo(AfVwSplitChild * pvswOther)
{
	if (pvswOther->m_qrootb != m_qrootb)
		pvswOther->CloseRootBox();
	pvswOther->m_qrootb = m_qrootb;
	// The other pane should also always have the same notion as this of how its root was
	// laid out.
	pvswOther->m_dxdLayoutWidth = m_dxdLayoutWidth;
}

/*----------------------------------------------------------------------------------------------
	Handle getting focus: Note self as current pane, then regular
----------------------------------------------------------------------------------------------*/
bool AfVwSplitChild::OnSetFocus(HWND hwndOld, bool fTbControl)
{
	if (this != m_qsplf->CurrentPane())
	{
		// Hide the selection if it is visible; we are about to switch active panes, and it
		// is the active pane that shows the selection
		CheckHr(m_qrootb->Activate(vssDisabled));
		m_qsplf->SetCurrentPane(this); // cf AfSplitChild FWndProc
	}
	// This will re-activate the selection (in the now-proper pane) if needed.
	return AfVwScrollWndBase::OnSetFocus(hwndOld, fTbControl);
}

/*----------------------------------------------------------------------------------------------
	Get a vector with a list of HVOs corresponding to the objects that the selection
	touches, and cache it in a special dummy object used for printing the selection.
----------------------------------------------------------------------------------------------*/
void AfVwSplitChild::CacheSelectedObjects()
{
	if (!CanPrintOnlySelection())
	{
		Assert(false);
		return;
	}

	Vector<HVO> vhvo;
	GetSelectedObjects(vhvo);

	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	if (!prmw)
		return;
	AfLpInfo * plpi = prmw->GetLpInfo();
	AssertPtr(plpi);
	CustViewDaPtr qcvd;
	plpi->GetDataAccess(&qcvd);
	if (!qcvd)
		return;

	HVO hvoSelected = prmw->GetPrintSelId();
	int flid = prmw->GetFilterFlid();
	// Store the list in a separate dummy object in the cache.
	CheckHr(qcvd->CacheVecProp(hvoSelected, flid, vhvo.Begin(), vhvo.Size()));
}

/*----------------------------------------------------------------------------------------------
	Return a vector of HVOs corresponding to the objects that the selection touches.
----------------------------------------------------------------------------------------------*/
void AfVwSplitChild::GetSelectedObjects(Vector<HVO> & vhvo)
{
	IVwSelectionPtr qsel;
	CheckHr(m_qrootb->get_Selection(&qsel));
	if (!qsel)
		return;

	int clevAnchor, clevEnd;
	CheckHr(qsel->CLevels(false, &clevAnchor));
	CheckHr(qsel->CLevels(true, &clevEnd));

	HVO hvoAnchor, hvoEnd;
	PropTag tagAnchor, tagEnd;
	int ihvo, cpropPrev;
	IVwPropertyStorePtr qvps;

	CheckHr(qsel->PropInfo(false, clevAnchor - 2, &hvoAnchor, &tagAnchor,
		&ihvo, &cpropPrev, &qvps));
	CheckHr(qsel->PropInfo(true, clevEnd - 2, &hvoEnd, &tagEnd,
		&ihvo, &cpropPrev, &qvps));

	if (hvoAnchor == 0 && hvoEnd == 0)
		return;
	if (hvoEnd == 0)
		// Probably a bug, but just use the anchor.
		hvoEnd = hvoAnchor;
	if (hvoAnchor == 0)
		hvoAnchor = hvoEnd;

	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	if (!prmw)
		return;
	AfLpInfo * plpi = prmw->GetLpInfo();
	AssertPtr(plpi);
	CustViewDaPtr qcvd;
	plpi->GetDataAccess(&qcvd);
	if (!qcvd)
		return;

	HVO hvoMainFilteredList = prmw->GetFilterId();
	int flid = prmw->GetFilterFlid();
	int chvoVal;
	CheckHr(qcvd->get_VecSize(hvoMainFilteredList, flid, &chvoVal));
	bool fInclude = false;
	for (int ihvo = 0; ihvo < chvoVal; ihvo++)
	{
		HVO hvoTmp;
		bool fMatchedAnchor;
		CheckHr(qcvd->get_VecItem(hvoMainFilteredList, flid, ihvo, &hvoTmp));
		if (!fInclude && (hvoTmp == hvoAnchor || hvoTmp == hvoEnd))
		{
			fInclude = true;
			fMatchedAnchor = (hvoTmp == hvoAnchor);
		}
		if (fInclude)
			vhvo.Push(hvoTmp);
		if (fMatchedAnchor && hvoTmp == hvoEnd)
			break;
		else if (!fMatchedAnchor && hvoTmp == hvoAnchor)
			break;
	}
}


/*----------------------------------------------------------------------------------------------
	Return vectors of HVOs and flids to identify a path from the root to the current field,
	and an index into the characters in that field.
	For example, if the cursor is on the 5th character of the title of subentry 1582, this
	would return:
	vhvoPath: 1579, 1581, 1582
	vflidPath: 4004009, 4004009, 4004001
	return = 4
	1579 is the main record. 1581 is a subentry in the 4004009 flid of 1579. 1582 is a
	subentry in the 4004009 flid of 1581. 4004001 is the Title field of 1582, and 4 indicates
	an index into the title of 4 (before the 5th character).
	@param vhvo Vector of object ids
	@param vflid Vector of field ids
	@return cursor index in the final field
----------------------------------------------------------------------------------------------*/
int AfVwSplitChild::GetLocation(Vector<HVO> & vhvo, Vector<int> & vflid)
{
	vhvo.Clear();
	vflid.Clear();
	VwSelLevInfo * prgvsli = NULL;
	try
	{
		// Get the selection information.
		IVwSelectionPtr qvwsel;
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
			return 0; // No selection.
		int csli;
		CheckHr(qvwsel->CLevels(false, &csli));
		if (!csli)
			return 0; // Some strange selection, perhaps a literal string, can't handle as yet.
		prgvsli = NewObj VwSelLevInfo[csli];
		int ihvoRoot;
		PropTag tagTextProp;
		int cpropPrevious;
		int ichAnchor;
		int ichEnd;
		int ws;
		ComBool fAssocPrev;
		int ihvoEnd;
		// JohnT: we apparently only call this to get ichAnchor and ichEnd!
		// Talk about overkill...
		// but I want to make minimal changes at this point. We can get E_NOTIMPL, from
		// picture selections...
		try {
			CheckHr(qvwsel->AllTextSelInfo(&ihvoRoot, csli, prgvsli, &tagTextProp, &cpropPrevious,
				&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
		}
		catch (Throwable& thr) {
			if (thr.Result() == E_NOTIMPL)
			{
				ichAnchor = ichEnd = 0;
			}
			else {
				throw(thr);
			}
		}

		// Get the information about each level.
		// The highest level is kflidStartDummyFlids, which represents the root object.
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
		return Min(ichAnchor, ichEnd);
	}
	catch (...)
	{
		if (prgvsli)
			delete[] prgvsli;
		return 0;
	}
}


//:>********************************************************************************************
//:>	AfVwScrollWnd methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfVwScrollWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	Assert(!lnRet);

	if (AfVwScrollWndBase::FWndProc(wm, wp, lp, lnRet))
		return true;

	return SuperClass::FWndProc(wm, wp, lp, lnRet); // Pass message on.
}

/*----------------------------------------------------------------------------------------------
	Scroll to the specified offsets.
	Does NOT update the scroll bars.
	Scrolls down by dxdOffset, right by dydOffset (or reverse if negative).
	That is, they are the amounts being added to the scroll bar position.
----------------------------------------------------------------------------------------------*/
void AfVwScrollWnd::ScrollBy(int dxdOffset, int dydOffset)
{
	try
	{
		int dx = - dxdOffset;
		int dy = - dydOffset;
		if (dx == 0 && dy == 0)
			return; // no change.

		if (dx != 0 && dy != 0)
		{
			// GetScrollRect and ScrollWindow don't handle both directions at once; do a
			// complete redraw.
			::InvalidateRect(m_hwnd, NULL, false);
		}
		else
		{
			Rect rc;
			GetScrollRect(dx, dy, rc);
			// Smoother effect with ScrollWindow.
			::ScrollWindowEx(m_hwnd, dx, dy,
				&rc,  // Whole client rectangle
				&rc,  // Clip just to client rectangle--can this be null??
				NULL, // Don't care what region is invalidated
				NULL, // Also don't care what rectangle is invalidated
				SW_ERASE + SW_INVALIDATE + SW_SCROLLCHILDREN); // Move child windows
																// automatically.
		}
		::UpdateWindow(m_hwnd);
	}
	catch (...)
	{
		// Nothing we can usefully do, just don't propagate the error.
	}
}


/*----------------------------------------------------------------------------------------------
	Process sizing (WM_SIZE). wst is the type of sizing requested. dxp is the new width, dyp
	is the new height. Both superclasses need to process the message.
----------------------------------------------------------------------------------------------*/
bool AfVwScrollWnd::OnSize(int wst, int dxp, int dyp)
{
	AfVwScrollWndBase::OnSize(wst, dxp, dyp);
	return SuperClass::OnSize(wst, dxp, dyp);
}


//:>********************************************************************************************
//:>	AfVwWnd methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfVwWnd::AfVwWnd() : AfVwRootSite(this)
{
	m_fResizeMakeSelVis = false;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfVwWnd::~AfVwWnd()
{
}


//:>********************************************************************************************
//:>	Windows message handling
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Process sizing (WM_SIZE). wst is the type of sizing requested. dxp is the new width, dyp
	is the new height.
----------------------------------------------------------------------------------------------*/
bool AfVwWnd::OnSize(int wst, int dxp, int dyp)
{
	// To make this work really right, we have to save this flag all during the process of
	// dragging the edge of the window to the final size, because at intermediate stges
	// it seems to get confused about whether or not the selection is really visible.
	// This means that the very last time the ScrollSelectionIntoView call has its intended
	// effect. We clear the flag on any subsequent mouse moves, because (I figure) they're
	// going to have to do at least one mouse move to start the proces of resizing over again.
	if (!m_fResizeMakeSelVis)
		m_fResizeMakeSelVis = IsSelectionVisible();

	// Recompute your layout and redraw completely, unless the width has not actually changed.
	InitGraphics();
	if (Layout())
		Invalidate();
	UninitGraphics();

	if (m_fResizeMakeSelVis)
		ScrollSelectionIntoView(NULL, kssoDefault);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Process mouse move (WM_MOUSEMOVE). xp/yp are current coordinates. grfmk identifies
	button/keys pressed.
----------------------------------------------------------------------------------------------*/
bool AfVwWnd::OnMouseMove(uint grfmk, int xp, int yp)
{
	// This seems like a re
	m_fResizeMakeSelVis = false;

	if (grfmk & MK_LBUTTON)
	{
		HWND hwndSplitChild = ::GetParent(m_hwnd);
		AfSplitChild * psplc = dynamic_cast<AfSplitChild *>(AfWnd::GetAfWnd(hwndSplitChild));
		// NOTE: Right now we are assuming that the parent of this window will always be an
		// AfSplitChild window. If this assert fails, the assert can be removed as long as
		// additional code is provided to handle the new case where the parent of this window
		// is not an AfSplitChild.
		AssertPtr(psplc);

		Rect rcParent;
		::GetClientRect(hwndSplitChild, &rcParent);
		::MapWindowPoints(hwndSplitChild, m_hwnd, (POINT *)&rcParent, 2);
		Rect rc;
		::GetClientRect(m_hwnd, &rc);

		// Figure out if we need to scroll up or down or not at all. We only need to scroll up
		// if the cursor is above the split child window and the top of the view window is
		// above the top of the split child window.
		bool fScrollUp;
		if (yp < rcParent.top && rc.top < rcParent.top)
			fScrollUp = true;
		else if (yp > rcParent.bottom && rc.bottom > rcParent.bottom)
			fScrollUp = false;
		else
			return AfVwRootSite::OnMouseMove(grfmk, xp, yp);

		HWND hwndSplitFrame = ::GetParent(hwndSplitChild);
		AfSplitFrame * psplf = dynamic_cast<AfSplitFrame *>(AfWnd::GetAfWnd(hwndSplitFrame));
		AssertPtr(psplf);
		HWND hwndScrollBar = psplf->GetScrollBarFromPane(psplc);
		if (fScrollUp)
			::SendMessage(hwndSplitFrame, WM_VSCROLL, SB_LINEUP, (LPARAM)hwndScrollBar);
		else
			::SendMessage(hwndSplitFrame, WM_VSCROLL, SB_LINEDOWN, (LPARAM)hwndScrollBar);
		::UpdateWindow(m_hwnd);
	}

	return AfVwRootSite::OnMouseMove(grfmk, xp, yp);
}


/*----------------------------------------------------------------------------------------------
	Scroll to make the selection visible.
	In general, scroll the minimum distance to make it entirely visible.
	If the selection is higher than the window, scroll the minimum distance to make it
	fill the window.
	If the window is too small to show both primary and secondary, show primary.
	This subclass, which does not implement its own scrolling, can do this only if its
	containing window can scroll. Currently we detect this by checking that it is an
	AfSplitChild.
----------------------------------------------------------------------------------------------*/
void AfVwWnd::MakeSelectionVisible1(IVwSelection * psel)
{
	// Get the parent window that we can really scroll. Currently the only type supported
	// is AfSplitChild (or a subclass).
	AfSplitChild * pascParent = dynamic_cast<AfSplitChild *>(Parent());
	if (!pascParent)
		return;

	IVwSelectionPtr qvwsel;
	if (!m_qrootb)
		return; // For paranoia.
	if (psel)
		qvwsel = psel;
	else
	{
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
			return; // Nothing we need to do.
	}

	Rect rdPrimary;
	Rect rdSecondary;
	ComBool fSplit;
	ComBool fEndBeforeAnchor;
	Rect rcSrcRoot;
	Rect rcDstRoot;
	Rect rdIdeal;
	// dy gets added to the scroll offset. This means a positive dy causes there to be more
	// of the view hidden above the top of the screen. This is the same effect as clicking a
	// down arrow, which paradoxically causes the window contents to move up.
	int dy = 0;
	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_ALL, 0, 0, 0, 0 };
	pascParent->GetScrollInfo(SB_VERT, &sinfo);
	// This loop repeats until we have figured out a scroll distance AND confirmed
	// that we can draw that location without messing things up.
	for (;;)
	{
		{ // BLOCK, to govern scope of HoldGraphics
			HoldGraphics hg(this);
			CheckHr(qvwsel->Location(m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot,
				&rdPrimary, &rdSecondary, &fSplit, &fEndBeforeAnchor));
		}
		rdIdeal = rdPrimary;
		Rect rcClient;
		::GetClientRect(m_hwnd, &rcClient);
		if (fSplit)
		{
			rdIdeal.Sum(rdSecondary);
			if (rdIdeal.Height() > rcClient.Height())
				rdIdeal = rdPrimary; // Revert to just showing main IP.
		}
		// OK, we want rdIdeal to be visible. RdIdeal is relative to our own client rectangle.
		Rect rdThis;
		Rect rdParent;
		::GetWindowRect(m_hwnd, &rdThis);
		::GetWindowRect(pascParent->Hwnd(), &rdParent);
		rcClient.Intersect(rdParent);
		// Note: if the parent could have a border, we would need to allow for that somehow.

		// OK, we want rdIdeal (relative to our own window) to be visible. Adjust it to be
		// relative to the parent window. Then we want rdIdeal to fit between zero and ydBottom.
		rdIdeal.Offset(0, (rdThis.top - rdParent.top));
		int ydTop = 0;
		int ydBottom = ydTop + rdParent.Height();

		// Is the selection partly off the top of the screen?
		if (rdIdeal.top < ydTop)
		{
			// Is it bigger than the screen?
			if (rdIdeal.Height() > rcClient.Height() && !fEndBeforeAnchor)
			{
				// Top is off, and though it is too big to show entirely, we can show
				// more. Move the window contents down (negative dy).
				dy = rdIdeal.bottom - ydBottom;
			}
			else
			{
				// Partly off top, and fits: move window contents down (less is hidden, neg dy).
				dy = rdIdeal.top - ydTop;
			}
		}
		else
		{
			// Top of selection is below (or at) top of screen.
			// Is bottom of selection below bottom of screen?
			if (rdIdeal.bottom > ydBottom)
			{
				if (rdIdeal.Height() > rcClient.Height() && fEndBeforeAnchor)
				{
					// Top is visible, bottom isn't: move until tops coincide to show as much as
					// possible. This is hiding more text above the top of the window: positive
					// dy.
					dy = rdIdeal.top - ydTop;
				}
				else
				{
					// fits entirely: scroll up minimum to make bottom visible. This involves
					// hiding more text at the top: positive dy.
					dy = rdIdeal.bottom - ydBottom;
				}
			}
			// Else it is already entirely visible, do nothing.
		}
		// OK, we need to move by dy. But, if that puts the selection near the bottom of the
		// screen, we may have to expand a lazy box above it in order to display a whole screen
		// full. If the size estimate is off (which it usually is), that would affect the
		// position' where the selection gets moved to. To avoid this, we make the same
		// PrepareToDraw call that the rendering code will make before drawing after the scroll.
		HoldGraphics hg(this);

		hg.m_rcDstRoot.Offset(0, -dy); // Want to draw at the position we plan to move to.

		int dyRange = sinfo.nMax;

		if (m_qrootb.Ptr() && (m_dxdLayoutWidth > 0))
		{
			VwPrepDrawResult xpdr = kxpdrAdjust;
			CheckHr(m_qrootb->PrepareToDraw(m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &xpdr));
			// OK to ignore result as we check independently for changed scroll range.
		}
		pascParent->GetScrollInfo(SB_VERT, &sinfo);
		// If PrepareToDraw didn't change the scroll range, it didn't mess anything up and we
		// can use the dy we figured. Otherwise, loop and figure it again with more complete
		// information, because something at a relevant point has been expanded to real boxes.
		if (sinfo.nMax == dyRange)
			break;
	}
	if (dy > sinfo.nMax - (int)(sinfo.nPage) - 1 - sinfo.nPos)
	{
		// This value makes it the maximum it can be, except this may make it too negative
		dy = sinfo.nMax - (int)(sinfo.nPage) - 1 - sinfo.nPos;
	}
	if (dy + sinfo.nPos < 0)
		dy = -sinfo.nPos; // make scroll pos 0 if it would have been less than that

	if (!dy)
		return; // Already as good as we can get.

	// Update the scroll position.
	sinfo.fMask = SIF_POS;
	sinfo.nPos += dy;
	pascParent->SetScrollInfo(SB_VERT, &sinfo, true);
	pascParent->ScrollBy(0, dy, NULL);
}

/*----------------------------------------------------------------------------------------------
	Test whether the (primary part of the) selection is visible.
	This subclass, which does not implement its own scrolling, can do this only if its
	containing window can scroll. Currently we detect this by checking that it is an
	AfSplitChild. If not, we assume the selection is visible
----------------------------------------------------------------------------------------------*/
bool AfVwWnd::IsSelectionVisible1(IVwSelection * psel)
{
	// Get the parent window that we can really scroll. Currently the only type supported
	// is AfSplitChild (or a subclass).
	AfSplitChild * pascParent = dynamic_cast<AfSplitChild *>(Parent());
	if (!pascParent)
		return true;

	IVwSelectionPtr qvwsel;
	if (!m_qrootb)
		return false; // For paranoia.
	if (psel)
		qvwsel = psel;
	else
	{
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
			return false; // Nothing we need to do.
	}

	Rect rdPrimary;
	Rect rdSecondary;
	ComBool fSplit;
	ComBool fEndBeforeAnchor;
	Rect rcSrcRoot;
	Rect rcDstRoot;
	Rect rdIdeal;
	GetCoordRects(m_qvg, &rcSrcRoot, &rcDstRoot);
	CheckHr(qvwsel->Location(m_qvg, rcSrcRoot, rcDstRoot, &rdPrimary, &rdSecondary, &fSplit,
		&fEndBeforeAnchor));
	rdIdeal = rdPrimary;
	Rect rcClient;
	::GetClientRect(m_hwnd, &rcClient);
	// OK, we want rdIdeal to be visible. RdIdeal is relative to our own client rectangle.
	Rect rdThis;
	Rect rdParent;
	::GetWindowRect(m_hwnd, &rdThis);
	::GetWindowRect(pascParent->Hwnd(), &rdParent);
	rcClient.Intersect(rdParent);
	// Note: if the parent could have a border, we would need to allow for that somehow.

	// OK, we want rdIdeal (relative to our own window) to be visible. Adjust it to be relative
	// to the parent window. Then we want rdIdeal to fit between zero and ydBottom.
	rdIdeal.Offset(0, (rdThis.top - rdParent.top));
	int ydTop = 0;
	int ydBottom = ydTop + rdParent.Height();

	// Does the selection rectangle overlap the screen one?
	// Note that if we support horizontal scrolling we will need to enhance this.
	if (rdIdeal.top < ydTop)
		return rdIdeal.bottom > ydTop;
	else
		return rdIdeal.top < ydBottom;
}

/*----------------------------------------------------------------------------------------------
	Move the selection near the top of the window.
	TODO: Implement properly if needed.
----------------------------------------------------------------------------------------------*/
void AfVwWnd::ScrollSelectionNearTop1(IVwSelection * psel)
{
	MakeSelectionVisible1(psel);
}

/*----------------------------------------------------------------------------------------------
	Remove yourself from the message handling queue.
----------------------------------------------------------------------------------------------*/
bool AfVwWnd::OnKillFocus(HWND hwndNew)
{
	return AfVwRootSite::OnKillFocus(hwndNew);

	m_fResizeMakeSelVis = false;
}


/*----------------------------------------------------------------------------------------------
	Handle window painting (WM_PAINT).
----------------------------------------------------------------------------------------------*/
bool AfVwWnd::OnPaint(HDC hdcDef)
{
	AssertObj(this);

	HDC hdc;
	HDC hdcMem;
	HBITMAP hbmpOld;
	PAINTSTRUCT ps;
	Rect rcp;

	GetClientRect(rcp);

	if (hdcDef)
	{
		Draw(hdcDef, rcp);
	}
	else
	{
		hdc = ::BeginPaint(m_hwnd, &ps);
		// rcp.Intersect(ps.rcPaint);
		hdcMem = AfGdi::CreateCompatibleDC(hdc);
		HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdc, rcp.Width(), rcp.Height());
		Assert(hbmp);
		hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
		Assert(hbmpOld && hbmpOld != HGDI_ERROR);
		AfGfx::FillSolidRect(hdcMem, rcp, ::GetSysColor(COLOR_WINDOW));
		Draw(hdcMem, rcp);

		// Splat the memory DC onto the screen.
		::BitBlt(hdc, rcp.left, rcp.top, rcp.Width(), rcp.Height(), hdcMem, 0, 0, SRCCOPY);

		// Clean up memory DC.
		HBITMAP hbmpDebug;
		hbmpDebug = AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);
		Assert(hbmpDebug && hbmpDebug != HGDI_ERROR);
		Assert(hbmpDebug == hbmp);

		BOOL fSuccess;
		fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
		Assert(fSuccess);

		fSuccess = AfGdi::DeleteDC(hdcMem);
		Assert(fSuccess);

		::EndPaint(m_hwnd, &ps);
	}

	return true; // Pass the message on.
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfVwWnd::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(!lnRet);

	if (AfVwRootSite::FWndProc(wm, wp, lp, lnRet))
		return true;

	return SuperClass::FWndProc(wm, wp, lp, lnRet); // Pass message on.
}

/*----------------------------------------------------------------------------------------------
	Apply a toggle property (bold or italic) to the specified range.
	fWantOn indicates whether we want the property on or off.
	tpt indicates which property.
----------------------------------------------------------------------------------------------*/
void ApplyInvertingProperty(TtpVec & vqttp, VwPropsVec & vqvps,
	int tpt, bool fWantOn, IVwSelection * pvwsel)
{
	int nVar, nOld, nNew;
	int cttp = vqttp.Size();
	// Now apply new state to all characters in selection:
	for (int ittp = 0; ittp < cttp; ++ittp)
	{
		ITsTextProps * pttp = vqttp[ittp];
		IVwPropertyStore * pvps = vqvps[ittp];
		int nValShow;
		bool fIsOn;
		CheckHr(pvps->get_IntProperty(tpt, &nValShow));
		if (tpt == ktptBold)
		{
			// The VwPropertyStore uses a special enumeration for bold.
			// Note that this produces the value we want, the opposite of
			// what is showing.
			fIsOn = (nValShow >= 550);
		}
		else
			fIsOn = nValShow != kttvOff;
		if (fIsOn != fWantOn)
		{
			ITsPropsBldrPtr qtpb;
			CheckHr(pttp->GetBldr(&qtpb));
			CheckHr(pttp->GetIntPropValues(tpt, &nVar, &nOld));
			if (nVar == ktpvEnum && nOld == kttvInvert)
				nNew = nVar = -1;
			else
			{
				nVar = ktpvEnum;
				nNew = kttvInvert;
			}
			CheckHr(qtpb->SetIntPropValues(tpt, nVar, nNew));
			ITsTextPropsPtr qttp;
			CheckHr(qtpb->GetTextProps(&qttp));
			vqttp[ittp] = qttp;
		}
		else
		{
			vqttp[ittp] = NULL;
		}
	}
	CheckHr(pvwsel->SetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin()));
}

/*----------------------------------------------------------------------------------------------
	Apply the formatting information from the toolbar to the current selection.  hwnd is the
	handle the button on the toolbar.
	ENHANCE JohnT: should we call SelectionChanged() for any kind of change, not just the
	writing system/old writing system? We end up with the same text selected, but its properties
	have changed, so it might be a good idea.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::ApplyFormatting(int cid, HWND hwnd)
{
	ISilDataAccessPtr qsda;
	BeginUndoTask(cid, &qsda);

	bool f = ApplyFormattingAux(cid, hwnd);

	EndUndoTask(qsda);

	return f;
}

bool AfVwRootSite::ApplyFormattingAux(int cid, HWND hwnd)
{
	IVwSelectionPtr qvwsel;
	// Avoid double message boxes. See comment below.
	static bool s_fRecurse;
	if (s_fRecurse)
		return false;
	TtpVec vqttp;
	VwPropsVec vqvps;

	if (!SelectionInOneField())
		return false;
	if (!GetCharacterProps(&qvwsel, vqttp, vqvps))
		return false;
	int cttp = vqttp.Size();

	int ittp;
	StrUni stu;
	ITsTextProps * pttp;
	IVwPropertyStore * pvps;
	StrUni stuNewVal;
	StrUni stuOldVal;
	int nNew;
	int nOld;
	int nVar;
	int wsNew;
	HRESULT hr;
	SmartBstr sbstr;
	ITsPropsBldrPtr qtpb;
	bool fChanged;
	COLORREF clr;
	COLORREF clrNew;
	int tpt = ktptForeColor;
	UiColorPopupPtr qcop;
	WndCreateStruct wcs;
	Point pt;
	Rect rc;
	AfMainWnd * pafw;

	// It seems in some situations we get a command message from the embedded real combo box,
	// and sometimes we may get it from the TssComboEx itself. Either way, navigate to the
	// TssComboEx. Do this in all the cases that involve a TssComboEx.
	SmartBstr sbstrCombo;
	switch (cid)
	{
	case kcidFmttbStyle:
	case kcidFmttbWrtgSys:
	case kcidFmttbFnt:
	case kcidFmttbFntSize:
		{
			TssComboEx * ptce = dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwnd));
			if (!ptce)
			{
				HWND hwndParent = ::GetParent(hwnd);
				ptce = dynamic_cast<TssComboEx *>(AfWnd::GetAfWnd(hwndParent));
				Assert(ptce);
			}
			int isel = ptce->GetCurSel();
			if (isel != -1)
			{
				ITsStringPtr qtss;
				ptce->GetLBText(isel, &qtss);
				CheckHr(qtss->get_Text(&sbstrCombo));
			}
			else
			{
				// We get the window text here instead of the current combo selection because
				// the user might have typed some text into the combo box that doesn't match any
				// selection.
				achar rgch[MAX_PATH];
				::GetWindowText(ptce->Hwnd(), rgch, isizeof(rgch) / isizeof(achar));
				StrUni stu(rgch);
				sbstrCombo = stu.Chars();
			}
			// REVIEW JohnT(DarrellZ): What should we do here if the combo box is empty?  Should
			// we return false here? That would mean if someone tries to tab off an empty combo
			// box, the focus would continue to stay in the combo box until they type something.
			if (sbstrCombo.Length() == 0)
				return true;
		}
		break;
	default:
		break;
	}

	switch (cid)
	{
	case kcidFmttbStyle:
		{
			StrUni stutyp(sbstrCombo.Chars(),2);
			if ((stutyp == L"\xb6 ") || (stutyp == L"\xaa "))
			{
				// Remove the first two chars which marks it's style type in the combo box.
				StrUni stu(sbstrCombo.Chars() + 2, sbstrCombo.Length() - 2);
				sbstrCombo = stu.Chars();
			}

			// Apply the style (named in sbstrCombo).
			// Get the stylesheet object (currently from the main window's AfLpInfo).
			AfStylesheet * pasts = Window()->MainWindow()->GetStylesheet();
			ITsTextPropsPtr qttpBogus;
			int stType = kstCharacter; // default for def para chars
			StrUni stuDefParaChars = L""; // Dummy style name for "no character style at all"
			stuDefParaChars.Load(kstidDefParaChars);
			bool fDefChars = !wcscmp(sbstrCombo, stuDefParaChars.Chars());
			if (!fDefChars)
			{
				if (pasts->GetStyleRgch(sbstrCombo.Length(), sbstrCombo, &qttpBogus) == S_FALSE)
				{
					::MessageBeep(MB_ICONEXCLAMATION);  // no such style
					return false;
				}
				CheckHr(pasts->GetType(sbstrCombo, &stType));
			}
			// Is it a paragraph or character style?
			switch (stType)
			{
			default:
				Assert(false);
				return true;
			case kstParagraph:
				FormatParas(knFormatStyle, m_fCanDoRtl, OuterRightToLeft(), 0, sbstrCombo);
				break;
			case kstCharacter:
				RemoveCharFormatting(qvwsel, vqttp, fDefChars ? NULL : sbstrCombo);
				break;
			}
		}
		return true;

	case kcidFmttbWrtgSys:
		// Apply the new writing system (name in sbstrCombo).
		if (!WsWithUiName(sbstrCombo.Chars(), &wsNew))
		{
			// No writing system with that name; see if it is an ICU Locale name.
			ISilDataAccessPtr qsdaT;
			CheckHr(m_qrootb->get_DataAccess(&qsdaT));
			ILgWritingSystemFactoryPtr qwsf;
			CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));
			AssertPtr(qwsf.Ptr());
			CheckHr(qwsf->GetWsFromStr(sbstrCombo, &wsNew));
			if (wsNew == 0)
			{
				::MessageBeep(MB_ICONEXCLAMATION);  // no such old writing system
				return false;
			}
		}
		// Now make the changes, if any.
		ApplyWritingSystem(vqttp, wsNew, qvwsel);
		return true;

	case kcidFmttbFnt:
		// Apply the new font and shift focus back to the target window.
		stuNewVal.Assign(sbstrCombo.Chars(), sbstrCombo.Length());
		// TODO: when the UI strings can be localized, the following method must be made
		// smarter.
		StripNameFromDefaultFont(stuNewVal);
		stuNewVal = FwStyledText::FontStringUiToMarkup(stuNewVal);
		// Now see what changes we have to deal with.
		fChanged = false;
		for (ittp = 0; ittp < cttp; ++ittp)
		{
			pttp = vqttp[ittp];
			pvps = vqvps[ittp];
			// If this property has not changed, do nothing.
			CheckHr(hr = pttp->GetStrPropValue(ktptFontFamily, &sbstr));
			if (hr == S_FALSE)
				CheckHr(pvps->get_StringProperty(ktptFontFamily, &sbstr));
			stuOldVal = sbstr.Chars();
			if (wcscmp(stuNewVal.Chars(), stuOldVal.Chars()))
			{
				CheckHr(pttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetStrPropValue(ktptFontFamily, stuNewVal.Bstr()));
				ITsTextPropsPtr qttp;
				CheckHr(qtpb->GetTextProps(&qttp));
				vqttp[ittp] = qttp;
				fChanged = true;
			}
			else
			{
				vqttp[ittp] = NULL;
			}
		}
		if (fChanged)
		{
			// Some change was made.
			CheckHr(qvwsel->SetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin()));
		}
		return true;

	case kcidFmttbFntSize:
		{
			// Apply the new font size and shift focus back to the target window.
			if (sbstrCombo.Length() == 0)
			{
				::SetFocus(::GetDlgItem(hwnd, kcidFmttbFntSize));
				return true;
			}
			nNew = wcstol(sbstrCombo.Chars(), NULL, 10) * 1000;
			// Verify the font size entered.
			if (!IsValidFontSize(nNew / 1000))
			{
				StrAppBuf strb;
				StrApp strMessage(kstidFfdRange);
				// This message box causes the toolbar combo box to lose focus. That results
				// in WM_KILLFOCUS calling ApplyFormatting a second time, but since the values
				// in the combobox have not yet been changed (since we want the user to see
				// the bad value) we end up displaying the error message twice. To avoid this
				// double call, we set the s_fRecurse flag to true which causes the second
				// call to be ignored.
				s_fRecurse = true;
				::MessageBox(hwnd, strMessage.Chars(), NULL, MB_OK | MB_ICONEXCLAMATION);
				s_fRecurse = false;

				// If nNew < kdyptMinSize, set it to kdyptMinSize; otherwise set it to
				// kdyptMaxSize.
				if (nNew < kdyptMinSize * 1000)
					strb.Format(_T("%d"), kdyptMinSize);
				else
					strb.Format(_T("%d"), kdyptMaxSize);
				// Put the appropriate number in the font size combobox.
				::SetWindowText(::GetWindow(::GetWindow(hwnd, GW_CHILD), GW_CHILD),
					strb.Chars());

				// Set focus back to font size combobox.
				::SetFocus(::GetDlgItem(hwnd, kcidFmttbFntSize));
				return false;
			}

//			else
//				nNew = knConflicting;

			// Now see what changes we have to deal with.
			fChanged = false;
			for (ittp = 0; ittp < cttp; ++ittp)
			{
				pttp = vqttp[ittp];
				pvps = vqvps[ittp];
				// If this property has not changed, do nothing.
				nVar = ktpvMilliPoint;
				CheckHr(hr = pttp->GetIntPropValues(ktptFontSize, &nVar, &nOld));
				if (hr == S_FALSE)
				{
					nVar = ktpvMilliPoint;
					CheckHr(pvps->get_IntProperty(ktptFontSize, &nOld));
				}
				if (nVar != ktpvMilliPoint || nNew != nOld)
				{
					CheckHr(pttp->GetBldr(&qtpb));
					if (sbstrCombo[0] && nNew > 0)
						CheckHr(qtpb->SetIntPropValues(ktptFontSize, ktpvMilliPoint, nNew));
					else
						CheckHr(qtpb->SetIntPropValues(ktptFontSize, -1, -1));
					ITsTextPropsPtr qttp;
					CheckHr(qtpb->GetTextProps(&qttp));
					vqttp[ittp] = qttp;
					fChanged = true;
				}
				else
				{
					vqttp[ittp] = NULL;
				}
			}
			if (fChanged)
			{
				// Some change was made.
				CheckHr(qvwsel->SetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin()));
			}
			return true;
		}

	case kcidFmttbBold:
		// Check if any runs are selected:
		if (cttp > 0)
		{
			// We have at least one selected run. If all of them are bold,
			// we want to turn off; if any one is not, we turn on.
			bool fWantOn = false; // if all are on now...
			for (ittp = 0; ittp < cttp; ++ittp)
			{
				int nNew;
				CheckHr(vqvps[ittp]->get_IntProperty(ktptBold, &nNew));
				if (nNew < 550)
				{
					// One run isn't bold: the check box is initially off:
					// we want to make everything bold.
					fWantOn = true;
					break;
				}
			}
			ApplyInvertingProperty(vqttp, vqvps, ktptBold, fWantOn, qvwsel);
		}
		return true;

	case kcidFmttbItal:
		// Check if any runs are selected:
		if (cttp > 0)
		{
			// We have at least one selected run. If all of them are italic,
			// we want to turn off; if any one is not, we turn on.
			bool fWantOn = false; // if all are on now...
			for (ittp = 0; ittp < cttp; ++ittp)
			{
				int nNew;
				CheckHr(vqvps[ittp]->get_IntProperty(ktptItalic, &nNew));
				if (nNew != kttvForceOn)
				{
					// One run isn't italic: the check box is initially off:
					// we want to make everything italic.
					fWantOn = true;
					break;
				}
			}
			ApplyInvertingProperty(vqttp, vqvps, ktptItalic, fWantOn, qvwsel);
		}
		return true;

	case kcidFmttbAlignLeft:
		return FormatParas(knFormatJustify, m_fCanDoRtl, OuterRightToLeft(), ktalLeft);

	case kcidFmttbAlignCntr:
		return FormatParas(knFormatJustify, m_fCanDoRtl, OuterRightToLeft(), ktalCenter);

	case kcidFmttbAlignRight:
		return FormatParas(knFormatJustify, m_fCanDoRtl, OuterRightToLeft(), ktalRight);

	case kcidFmttbLstNum:
		return FormatParas(knFormatNumListButton, m_fCanDoRtl, OuterRightToLeft(), ktalLeft);

	case kcidFmttbLstBullet:
		return FormatParas(knFormatBulletList, m_fCanDoRtl, OuterRightToLeft(), ktalLeft);

	case kcidFmttbApplyBdr:
		return FormatParas(knFormatBorderButton, m_fCanDoRtl, OuterRightToLeft(), ktalLeft);

//	case kcidFmttbLstNum:
//	case kcidFmttbLstBullet:

	// ENHANCE JohnT (LukeU): The number 7200 (now 18000) represents kSpnStpIn
	// from FmtParaDlg.h and is the step that the indent is increased
	// by when the user clicks the spin button. If ever the step gets changed
	// in FmtParaDlg.h it will need to be changed here as well or adjust the
	// include directories to include FmtParaDlg.h and replace 7200 with kSpnStpIn.
	// SC 3Jul2001 - the specs say the indentation should be 0.25 inch.
	case kcidFmttbUnind:
		return FormatParas(knFormatIndent, m_fCanDoRtl, OuterRightToLeft(), -18000);
	case kcidFmttbInd:
		return FormatParas(knFormatIndent, m_fCanDoRtl, OuterRightToLeft(), 18000);
//	case kcidFmttbApplyBdr:

	case kcidFmttbApplyBgrndColor:
		tpt = ktptBackColor;
		// Fall through.
	case kcidFmttbApplyFgrndColor:
		nOld = knNinch;
		for (ittp = 0; ittp < cttp; ++ittp)
		{
			pttp = vqttp[ittp];
			pvps = vqvps[ittp];
			if (!ittp)
			{
				CheckHr(hr = pttp->GetIntPropValues(tpt, &nVar, &nOld));
				if (hr == S_FALSE)
					CheckHr(pvps->get_IntProperty(tpt, &nOld));
			}
			else
			{
				CheckHr(hr = pttp->GetIntPropValues(tpt, &nVar, &nNew));
				if (hr == S_FALSE)
					CheckHr(pvps->get_IntProperty(tpt, &nNew));
				if (nNew != nOld)
					nOld = knConflicting;
			}
		}
		clr = (nOld == knConflicting ? knNinch : nOld);
		AssertPtr(m_pwndSubclass);
		pafw = m_pwndSubclass->MainWindow();
		if (pafw)
		{
			if (tpt != ktptBackColor)
				pafw->GetColors(&clrNew, NULL);
			else
				pafw->GetColors(NULL, &clrNew);
			int nVar = 0;
			// Turn the color off if it matches the color that is already on the selection.
			if (clrNew == clr)
			{
				nVar = -1;
				clrNew = (COLORREF)-1;
			}

			if (clrNew != clr)
			{
				for (ittp = 0; ittp < cttp; ++ittp)
				{
					CheckHr(vqttp[ittp]->GetBldr(&qtpb));
					CheckHr(qtpb->SetIntPropValues(tpt, nVar, clrNew));
					ITsTextPropsPtr qttp;
					CheckHr(qtpb->GetTextProps(&qttp));
					vqttp[ittp] = qttp;
				}
				CheckHr(qvwsel->SetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin()));
			}
		}
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	If the font name from the  toolbar combo-box is something like
	"Times New Roman <default font>", strip off the name of the font to leave something
	like "<default font>".

	TODO: make this method smarter to handle potentially localized strings. To do this, call
	FwStyledText::FontUiStrings and search for each string.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::StripNameFromDefaultFont(StrUni & stuDefault)
{
	int ich = stuDefault.FindStr("<default");
	if (ich > 0)
		stuDefault = stuDefault.Right(stuDefault.Length() - ich);
}

/*----------------------------------------------------------------------------------------------
	Set the old writing system of the selected text in the selection pvwsel. vqttp should
	have been obtained from the selection as the current values using GetCharacterProps or
	similar.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::ApplyWritingSystem(TtpVec & vqttp, int wsNew,
	IVwSelection * pvwsel)
{
#if 99-99
	StrAnsi sta;
	sta.Format("AfVwRootSite::ApplyWritingSystem(..., %d, ...)%n", wsNew);
	::OutputDebugStringA(sta.Chars());
#endif
	bool fChanged = false;
	int cttp = vqttp.Size();
	for (int ittp = 0; ittp < cttp; ++ittp)
	{
		HRESULT hr;
		int wsOld, var;
		ITsTextProps * pttp = vqttp[ittp];
		ITsPropsBldrPtr qtpb;
		// If this property has not changed, do nothing.
		CheckHr(hr = pttp->GetIntPropValues(ktptWs, &var, &wsOld));
		if (hr == S_FALSE || wsOld != wsNew)
		{
			CheckHr(pttp->GetBldr(&qtpb));
			CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, wsNew));
			ITsTextPropsPtr qttp;
			CheckHr(qtpb->GetTextProps(&qttp));
			vqttp[ittp] = qttp;
			fChanged = true;
		}
		else
		{
			vqttp[ittp] = NULL;
		}
	}
	if (fChanged)
	{
		// Some change was made.
		CheckHr(pvwsel->SetSelectionProps(cttp, (ITsTextProps **)vqttp.Begin()));
		CheckHr(SelectionChanged(m_qrootb, pvwsel));
	}
}

/*----------------------------------------------------------------------------------------------
	Get the indicated formatting value for the current selection (paragraph level props).
----------------------------------------------------------------------------------------------*/
int AfVwRootSite::GetParaFormatting(int cid, StrApp & strValue)
{
	IVwSelectionPtr qvwsel;
	HVO hvoText;
	int tagText;
	VwPropsVec vqvps;
	int ihvoFirst, ihvoLast;
	ISilDataAccessPtr qsda;
	TtpVec vqttp;

	// Get the paragraph properties from the selection. If there is neither a selection nor a
	// paragraph property, return false to turn the button off.
	if (!GetParagraphProps(&qvwsel, hvoText, tagText, vqvps, ihvoFirst, ihvoLast, &qsda, vqttp))
		return false;
	// For each command we handle, if we find one paragraph that doesn't have the property,
	// we return false.
	int cttp = vqttp.Size();
	if (cid == kcidFmttbApplyBdr)
		return CheckBorderButton(vqttp);
	for (int ittp = 0; ittp < cttp; ++ittp)
	{
		ITsTextProps * pttp = vqttp[ittp];
		IVwPropertyStore * pvps = vqvps[ittp];
		if (!pttp)
			return false;
		if (!pvps)
			return false;
		switch (cid)
		{
#ifdef JT_7_17_01_ButtonsAreStyles
		case kcidFmttbLstNum:
			{
				// On if the paragraph has the named style "Numbered List"
				SmartBstr sbstrStyle;
				CheckHr(pttp->GetStrPropValue(ktptNamedStyle, &sbstrStyle));

				if (wcscmp(pwszNumList, sbstrStyle.Chars()))
				{
					int nVar, nVal;
					if (pttp)
						CheckHr(pttp->GetIntPropValues(ktptBulNumScheme, &nVar, &nVal));
					if (nVal < kvbnNumberBase || nVal > kvbnNumberMax)
						return false;
				}
				else
					return false;
			}
			break;
		case kcidFmttbLstBullet:
			{
				// On if the paragraph has the named style "Bulleted List"
				SmartBstr sbstrStyle;
				CheckHr(pttp->GetStrPropValue(ktptNamedStyle, &sbstrStyle));
				if (wcscmp(pwszBullList, sbstrStyle.Chars()))
				{
					int nVar, nVal;
					if (pttp)
						CheckHr(pttp->GetIntPropValues(ktptBulNumScheme, &nVar, &nVal));
					if (nVal < kvbnBulletBase || nVal > kvbnBulletMax)
						return false;
				}
				else
					return false;
			}
			break;
#endif
		case kcidFmttbLstNum:
		case kcidFmttbLstBullet:
			{ // BLOCK
//				int nVar = 0;
				int nValBn = 0; // Anything outside range below OK.
//				CheckHr(pttp->GetIntPropValues(ktptBulNumScheme, &nVar, &nValBn));
				// Use the property store, not the ttp, to get the cumulative effect of all
				// the styles.
				CheckHr(pvps->get_IntProperty(ktptBulNumScheme, &nValBn));
				if (cid == kcidFmttbLstBullet)
					return nValBn >= kvbnBulletBase && nValBn <= kvbnBulletMax;
				else
					return nValBn == kvbnArabic;
			}
		default :
			Assert(false);
			return false;
		}
	}
	return true; // If none of them is different the option is on.
}

/*----------------------------------------------------------------------------------------------
	Get the format border combo buttons that should be pressed.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::GetFmtBdrPressed(bool * pavrs)
{
	// Border constants are in the same order as the bp enum.
	static int rgtpt[4] = {ktptBorderTop, ktptBorderBottom,
		ktptBorderLeading, ktptBorderTrailing};

	IVwSelectionPtr qvwsel;
	HVO hvoText;
	int tagText;
	VwPropsVec vqvps;
	int ihvoFirst, ihvoLast;
	ISilDataAccessPtr qsda;
	TtpVec vqttp;

	// Get the paragraph properties from the selection. If there is neither a selection nor a
	// paragraph property, return false.
	if (!GetParagraphProps(&qvwsel, hvoText, tagText, vqvps, ihvoFirst, ihvoLast, &qsda, vqttp))
		return false;

	// Set all the buttons except None to on/pressed.
	for (int i = 0; i < kbpLim; ++i)
		pavrs[i] = true;
	pavrs[kbpNone] = false;  // Ensure that the button is false after the loop

	int cttp = vqttp.Size();
	for (int ittp = 0; ittp < cttp; ++ittp)
	{
		ITsTextProps * pttp = vqttp[ittp];
		// Get the current actual values of the properties and compare
		int var, val;
		for (int itpt = 0; itpt < 4; itpt++)
		{
			var = val = -1;  // Treat as undefined if no pttp.
			if (pttp)
				CheckHr(pttp->GetIntPropValues(rgtpt[itpt], &var, &val));
			// If there is no border on the side we're testing
			if (var == -1 || val <= 0)
			{
				pavrs[itpt + kbpSingles] = false;  // button for this side is off
				pavrs[kbpAll] = false; // button for all 4 sides is also off.
			}
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the indicated formatting value for the current selection.
----------------------------------------------------------------------------------------------*/
int AfVwRootSite::GetFormatting(int cid, StrApp & strValue)
{
	IVwSelectionPtr qvwsel;
	TtpVec vqttp;
	VwPropsVec vqvps;

	if (!GetCharacterProps(&qvwsel, vqttp, vqvps))
		return 0;
	int cttp = vqttp.Size();

	strValue = "";

	int ittp;
	ITsTextProps * pttp;
	VwPropsVec vqvpsPara;
	StrUni stuNewVal;
	StrUni stuOldVal;
	int nNew = 0;
	int nOld = 0;
	int nVar = 0;
	int nRtl = 0;
	HRESULT hr;
	SmartBstr sbstr;
	bool fMixed;
	COLORREF clr;
	int tpt = ktptForeColor;
	StrUni stuDefSerif;
	StrUni stuDefSans;
	StrUni stuDefMono;
	StrUni stuDefBodyFont;

	switch (cid)
	{
	case kcidFmttbStyle:
		{ // BLOCK
			int ittp;
			// If no style is specified, use 'Normal'. If there is any kind of conflict,
			// leave the box empty.
			strValue.Assign(g_pszwStyleNormal);

			if (cttp)
			{
				SmartBstr sbstrFirst;
				CheckHr(vqttp[0]->GetStrPropValue(kspNamedStyle, &sbstrFirst));
				for (ittp = 1; ittp < cttp; ittp++)
				{
					SmartBstr sbstrThis;
					CheckHr(vqttp[ittp]->GetStrPropValue(kspNamedStyle, &sbstrThis));
					if (!sbstrThis.Equals(sbstrFirst))
					{
						// Conflict: show nothing
						strValue.Clear();
						return 1;
					}
				}
				if (BstrLen(sbstrFirst))
				{
					// We got a character style, uniform across all runs: return that.
					strValue = sbstrFirst.Chars();
					return 1;
				}
			}
			// No character style (and no conflicts): look for para style.
			IVwSelectionPtr qvwsel;
			HVO hvoText;
			int tagText;
			IVwPropertyStorePtr qvps;
			int ihvoFirst, ihvoLast;
			ISilDataAccessPtr qsda;
			TtpVec vqttp;

			// If there is neither a selection nor a paragraph property, set the combo text
			// to "Normal".
			if (!GetParagraphProps(&qvwsel, hvoText, tagText, vqvpsPara, ihvoFirst, ihvoLast,
				&qsda, vqttp))
			{
				return 1;
			}
			int cttpPara = vqttp.Size();
			if (!cttpPara)
				return 1; // no paragraph info, leave blank
			// The first style determines the result, unless there is a conflict.
			SmartBstr sbstrParaStyle;
			ITsTextProps * pttp = vqttp[0];
			if (pttp)
				CheckHr(hr = pttp->GetStrPropValue(kspNamedStyle, &sbstrParaStyle));
			if (BstrLen(sbstrParaStyle) == 0)
				sbstrParaStyle = g_pszwStyleNormal;

			// Look for conflicts
			for (ittp = 1; ittp < cttpPara; ittp++)
			{
				SmartBstr sbstrParaStyleThis;
				ITsTextProps * pttp = vqttp[ittp];

				// In some cases pttp may be null, i.e., no paragraph properties were set.
				if (pttp)
				{
					CheckHr(pttp->GetStrPropValue(kspNamedStyle, &sbstrParaStyleThis));
				}
				if (BstrLen(sbstrParaStyleThis) == 0)
					sbstrParaStyleThis = g_pszwStyleNormal;

				if (!sbstrParaStyle.Equals(sbstrParaStyleThis))
				{
					// conflict: leave blank
					strValue.Clear();
					return 1;
				}
			}
			// No conflict: use the paragraph style name.
			if (sbstrParaStyle.Length() > 0)
				strValue = sbstrParaStyle.Chars();
		}
		return 1;

	case kcidFmttbWrtgSys:
		fMixed = false;
		for (ittp = 0; ittp < cttp; ++ittp)
		{
			pttp = vqttp[ittp];
			IVwPropertyStore * pvps = vqvps[ittp];
			// If this property is not set on the ttp, or we got no ttp for this range.
			hr = S_FALSE;
			if (pttp)
				CheckHr(hr = pttp->GetIntPropValues(ktptWs, &nVar, &nNew));
			if (hr == S_FALSE || nNew == 0)
				CheckHr(pvps->get_IntProperty(ktptBaseWs, &nNew));
			if (ittp > 0 && nNew != nOld)
			{
				fMixed = true;
				break;
			}
			nOld = nNew;
		}

		if (fMixed || nNew == -1)
			strValue = "";
		else
			UiNameOfWs(nNew, &strValue);
		return 1;

	case kcidFmttbFnt:
		fMixed = false;
		GetDefaultFontNames(vqttp, vqvps, stuDefSerif, stuDefSans, stuDefMono, stuDefBodyFont);
		for (ittp = 0; ittp < cttp; ++ittp)
		{
			pttp = vqttp[ittp];
			IVwPropertyStore * pvps = vqvps[ittp];
			// If this property is not set on the ttp, or we got no ttp for this range.
			hr = S_FALSE;
			if (pttp)
				CheckHr(hr = pttp->GetStrPropValue(ktptFontFamily, &sbstr));
			if (hr == S_FALSE)
				CheckHr(pvps->get_StringProperty(ktptFontFamily, &sbstr));
			stuNewVal = sbstr.Chars();
			if (FwStyledText::MatchesDefaultSerifMarkup(stuNewVal))
				stuNewVal = stuDefSerif;
			else if (FwStyledText::MatchesDefaultSansMarkup(stuNewVal))
				stuNewVal = stuDefSans;
			else if (FwStyledText::MatchesDefaultBodyFontMarkup(stuNewVal))
				stuNewVal = stuDefBodyFont;
			else if (FwStyledText::MatchesDefaultMonoMarkup(stuNewVal))
				stuNewVal = stuDefMono;
			if (ittp > 0 && stuNewVal != stuOldVal)
				fMixed = true;
			stuOldVal = stuNewVal;
		}
		if (!fMixed)
			strValue = stuOldVal;
		return 1;

	case kcidFmttbFntSize:
		fMixed = false;
		nOld = 0;
		for (ittp = 0; ittp < cttp; ++ittp)
		{
			pttp = vqttp[ittp];
			IVwPropertyStore * pvps = vqvps[ittp];
			nVar = ktpvMilliPoint;
			hr = S_FALSE;
			if (pttp)
				CheckHr(hr = pttp->GetIntPropValues(ktptFontSize, &nVar, &nNew));
			if (hr == S_FALSE)
			{
				nVar = ktpvMilliPoint;
				CheckHr(pvps->get_IntProperty(ktptFontSize, &nNew));
			}
			if (ittp > 0 && nNew != nOld)
				fMixed = true;
			nOld = nNew;
		}
		if (!fMixed && nOld)
		{
			if (nVar == ktpvMilliPoint)
				strValue.Format(_T("%d"), nOld / 1000);
			else
				strValue.Format(_T("%d"), nOld);
		}
		return 1;

	case kcidFmttbBold:
		if (cttp == 0)
			return false; // treat as not bold
		for (ittp = 0; ittp < cttp; ++ittp)
		{
			CheckHr(vqvps[ittp]->get_IntProperty(ktptBold, &nNew));
			if (nNew < 550)
				return false;
		}
		return true;

	case kcidFmttbItal:
		if (cttp == 0)
			return false; // treat as not italic
		for (ittp = 0; ittp < cttp; ++ittp)
		{
			CheckHr(vqvps[ittp]->get_IntProperty(ktptItalic, &nNew));
			if (nNew != kttvForceOn)
				return false;
		}
		return true;

	case kcidFmttbAlignLeft:
		nOld = ktalLeft;
		for (ittp = 0; ittp < cttp; ++ittp)
		{
			IVwPropertyStore * pvps = vqvps[ittp];
			CheckHr(pvps->get_IntProperty(ktptRightToLeft, &nRtl));
			nVar = ktpvEnum;
			CheckHr(pvps->get_IntProperty(ktptAlign, &nNew));
			if (nNew == ktalLeading)
				nNew = (nRtl) ? ktalRight : ktalLeft;
			else if (nNew == ktalTrailing)
				nNew = (nRtl) ? ktalLeft : ktalRight;
			if (nNew != ktalLeft)
				nOld = nNew;
		}
		return nOld == ktalLeft;

	case kcidFmttbAlignRight:
		nOld = ktalRight;
		for (ittp = 0; ittp < cttp; ++ittp)
		{
			IVwPropertyStore * pvps = vqvps[ittp];
			CheckHr(pvps->get_IntProperty(ktptRightToLeft, &nRtl));
			nVar = ktpvEnum;
			CheckHr(pvps->get_IntProperty(ktptAlign, &nNew));
			if (nNew == ktalLeading)
				nNew = (nRtl) ? ktalRight : ktalLeft;
			else if (nNew == ktalTrailing)
				nNew = (nRtl) ? ktalLeft : ktalRight;
			if (nNew != ktalRight)
				nOld = nNew;
		}
		return nOld == ktalRight;

	case kcidFmttbAlignCntr:
		nOld = ktalCenter;
		for (ittp = 0; ittp < cttp; ++ittp)
		{
			IVwPropertyStore * pvps = vqvps[ittp];
			// Note: the directionality doesn't affect centering.
			nVar = ktpvEnum;
			CheckHr(pvps->get_IntProperty(ktptAlign, &nNew));
			if (nNew != ktalCenter)
				nOld = nNew;
		}
		return nOld == ktalCenter;

	case kcidFmttbLstNum:
	case kcidFmttbLstBullet:
	case kcidFmttbApplyBdr:
		Assert(false); // these cases should not come here
		return false;  // a desperate default.
		// No need to disable/enable the indent/unindent buttons.
	case kcidFmttbUnind:
	case kcidFmttbInd:
		return true;

	case kcidFmttbApplyBgrndColor:
		tpt = ktptBackColor;
		// Fall through.
	case kcidFmttbApplyFgrndColor:
		nOld = knNinch;
		for (ittp = 0; ittp < cttp; ++ittp)
		{
			pttp = vqttp[ittp];
			IVwPropertyStore * pvps = vqvps[ittp];
			if (!ittp)
			{
				hr = S_FALSE;
				if (pttp)
					CheckHr(hr = pttp->GetIntPropValues(tpt, &nVar, &nOld));
				if (hr == S_FALSE)
					CheckHr(pvps->get_IntProperty(tpt, &nOld));
			}
			else
			{
				hr = S_FALSE;
				if (pttp)
					CheckHr(hr = pttp->GetIntPropValues(tpt, &nVar, &nNew));
				if (hr == S_FALSE)
					CheckHr(pvps->get_IntProperty(tpt, &nNew));
				if (nNew != nOld)
					nOld = knConflicting;
			}
		}
		clr = nOld == knConflicting ? knNinch : nOld;
		return clr;
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Update the font combo box with more informative names for the default font and default
	heading font.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::UpdateDefaultFontNames(void * pv)
{
	TssComboEx * ptceFont = reinterpret_cast<TssComboEx *>(pv);

	IVwSelectionPtr qvwsel;
	TtpVec vqttp;
	VwPropsVec vqvps;

	GetCharacterProps(&qvwsel, vqttp, vqvps);

	StrUni stuSerif;
	StrUni stuSans;
	StrUni stuMono;
	StrUni stuBodyFont;
	GetDefaultFontNames(vqttp, vqvps, stuSerif, stuSans, stuMono, stuBodyFont);

	// Update the font combobox.  <default font> is the first item and <default heading font>
	// is the second item--see RecMainWnd::OnToolBarButtonAdded.

	FW_COMBOBOXEXITEM fcbi;
	memset(&fcbi, 0, isizeof(fcbi));
	fcbi.mask = CBEIF_TEXT;
	fcbi.iIndent = 0;

	int wsUser;
	ILgWritingSystemFactoryPtr qwsf;
	GetLgWritingSystemFactory(&qwsf);
	CheckHr(qwsf->get_UserWs(&wsUser));

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);

	qtsf->MakeStringRgch(stuSerif.Chars(), stuSerif.Length(), wsUser, &fcbi.qtss);
	fcbi.iItem = 0;
	ptceFont->SetItem(&fcbi);

	if (!m_f1DefaultFont)
	{
		qtsf->MakeStringRgch(stuSans.Chars(), stuSans.Length(), wsUser, &fcbi.qtss);
		fcbi.iItem = 1;
		ptceFont->SetItem(&fcbi);

		qtsf->MakeStringRgch(stuBodyFont.Chars(), stuBodyFont.Length(), wsUser, &fcbi.qtss);
		fcbi.iItem = 2;
		ptceFont->SetItem(&fcbi);

		//qtsf->MakeStringRgch(stuMono.Chars(), stuMono.Length(), wsUser, &fcbi.qtss);
		//fcbi.iItem = 3;
		//ptceFont->SetItem(&fcbi);
	}
}

/*----------------------------------------------------------------------------------------------
	Get the labels for the default fonts that should go in the toolbar combo-box.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::GetDefaultFontNames(TtpVec & vqttp, VwPropsVec & vqvps,
	StrUni & stuDefSerif, StrUni & stuDefSans, StrUni & stuDefMono, StrUni & stuDefBodyFont)
{
	HRESULT hr;

	int cttp = vqttp.Size();
	bool fMixed = false;
	int wsNew = 0;
	int nVar = 0;
	int wsOld = 0;
	for (int ittp = 0; ittp < cttp; ++ittp)
	{
		ITsTextProps * pttp = vqttp[ittp];
		IVwPropertyStore * pvps = vqvps[ittp];
		// If this property is not set on the ttp, or we got no ttp for this range.
		hr = S_FALSE;
		if (pttp)
			CheckHr(hr = pttp->GetIntPropValues(ktptWs, &nVar, &wsNew));
		if (hr == S_FALSE || wsNew == 0)
			CheckHr(pvps->get_IntProperty(ktptBaseWs, &wsNew));
		if (ittp && wsNew != wsOld) // TODO: test ows as well
		{
			fMixed = true;
			break;
		}
		wsOld = wsNew;
	}

	Vector<StrUni> vstu;
	FwStyledText::FontUiStrings(m_f1DefaultFont, vstu);
	stuDefSerif = (vstu.Size() > 0) ? vstu[0] : L"";
	stuDefSans = (vstu.Size() > 1) ? vstu[1] : L"";
	stuDefMono = (vstu.Size() > 2) ? vstu[2] : L"";
	stuDefBodyFont = (vstu.Size() > 3) ? vstu[3] : L"";

	if (fMixed || wsNew == 0 || wsNew == -1)
	{
		// don't append name of font
	}
	else
	{
		ISilDataAccessPtr qsdaT;
		CheckHr(m_qrootb->get_DataAccess(&qsdaT));
		ILgWritingSystemFactoryPtr qwsf;
		IWritingSystemPtr qws;
		CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));
		if (qwsf)
			CheckHr(qwsf->get_EngineOrNull(wsNew, &qws));
		if (qws)
		{
			StrUni stuFSerif;
			StrUni stuFSans;
			StrUni stuFMono;
			StrUni stuFBodyFont;
			AfApp::DefaultFontsForWs(qws, stuFSerif, stuFSans, stuFMono, stuFBodyFont);
			if (stuFSerif.Length())
			{
				if (stuDefSerif.Length())
				{
					stuFSerif.Append(L" ");
					stuFSerif.Append(stuDefSerif);
				}
				stuDefSerif = stuFSerif;
			}
			if (stuFSans.Length())
			{
				if (stuDefSans.Length())
				{
					stuFSans.Append(L" ");
					stuFSans.Append(stuDefSans);
				}
				stuDefSans = stuFSans;
			}
			if (stuFBodyFont.Length())
			{
				if (stuDefBodyFont.Length())
				{
					stuFBodyFont.Append(L" ");
					stuFBodyFont.Append(stuDefBodyFont);
				}
				stuDefBodyFont = stuFBodyFont;
			}
			if (stuFMono.Length())
			{
				if (stuDefMono.Length())
				{
					stuFMono.Append(L" ");
					stuFMono.Append(stuDefMono);
				}
				stuDefMono = stuFMono;
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Return the writing system and old writing system that has the given UI name (ie, what is
	shown in the old writing system combo-box).
	TODO 1350 (JohnT): handle multiple old writing systems per encodings.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::WsWithUiName(StrApp strUiName, int * pws)
{
	// Get the writing system factory associated with the root box.
	if (!m_qrootb)
		return false; // For paranoia.
	ISilDataAccessPtr qsdaT;
	CheckHr(m_qrootb->get_DataAccess(&qsdaT));
	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));

	int cws;
	CheckHr(qwsf->get_NumberOfWs(&cws));
	Vector<int> vws;
	vws.Resize(cws);
	CheckHr(qwsf->GetWritingSystems(vws.Begin(), cws));
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));
	for (int iws = 0; iws < cws; iws++)
	{
		IWritingSystemPtr qws;
		CheckHr(qwsf->get_EngineOrNull(vws[iws], &qws));
		if (!qws)
			continue;

		SmartBstr sbstr;
		CheckHr(qws->get_UiName(wsUser, &sbstr));
		if (!sbstr)
			continue;
		StrUni stu(sbstr.Chars());
		if (strUiName == stu)
		{
			*pws = vws[iws];
			return true;
		}
	}
	*pws = 0;
	return false;
}

/*----------------------------------------------------------------------------------------------
	Return the UI name for the given writing system and old writing system, eg, for display in a
	combo-box.
----------------------------------------------------------------------------------------------*/
void AfVwRootSite::UiNameOfWs(int ws, StrApp * pstr)
{
	*pstr = L"";

	// Get the writing system factory associated with the root box.
	if (!m_qrootb)
		return; // For paranoia.
	ISilDataAccessPtr qsdaT;
	CheckHr(m_qrootb->get_DataAccess(&qsdaT));
	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));
	AssertPtr(qwsf);

	IWritingSystemPtr qws;
	CheckHr(qwsf->get_EngineOrNull(ws, &qws));
	if (!qws)
		return;
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));
	SmartBstr sbstr;
	CheckHr(qws->get_UiName(wsUser, &sbstr));
	if (!sbstr)
		return;
	StrUni stu(sbstr.Chars());
	*pstr = stu;
}


//:>********************************************************************************************
//:>	Printing-related methods.
//:>	These are defined on AfVwRootSite so that subclasses of AfVwWnd can enable them,
//:>	though currently they are only enabled for full AfVwSplitChild.
//:>********************************************************************************************

// These are static variables so the AbortProc can access them.
HWND s_hwndCancel = 0;
bool s_fContinuePrint;
IVwPrintContext * s_pvpc = NULL;

// callback called during printing if the user aborts or something goes wrong.
// Note: it is possible to receive the error SP_OUTOFDISK and wait for disk space
// to become available. I don't think it is worth doing, though.
BOOL CALLBACK AbortProc(
	HDC hdc,     // handle to DC
	int iError   // error value
	)
{
	if (iError)
		return false; // abort print job

	MSG msg;

	// Retrieve and remove messages from the thread's message queue.

	while (PeekMessage((LPMSG) &msg, NULL, 0, 0, PM_REMOVE))
	{
		// Process any messages for the Cancel dialog box.
#if 0
		// The following was an unsuccessful attempt at a bug fix. It seems our
		// code IS sufficiently re-entrant for this situation. And it looks
		// little nicer to keep the window painted. However, the explanation
		// following may be helpful if some similar problem occurs later.

		// But, do NOT process WM_PAINT messages for the main window.
		// Somehow, this interferes with printing, perhaps because this function
		// somehow (asynchronously?) gets called during VwGraphics::DrawText calls
		// in the printing process, and our drawing code is not particularly
		// designed to be reentrant, though I (JohnT) don't see exactly where it
		// is going wrong.
		// Note that we won't lose the WM_PAINT, because it is not removed from
		// the queue until processed.

		if (msg.message == WM_PAINT && !::IsChild(s_hwndCancel, msg.hwnd))
			return s_fContinuePrint;
#endif
		if (!IsDialogMessage(s_hwndCancel, (LPMSG) &msg))
		{
			TranslateMessage((LPMSG) &msg);
			DispatchMessage((LPMSG) &msg);
		}
	}

	// Return the global print flag, which is initially set to TRUE.
	// The dialog box procedure for the Cancel dialog box
	// sets bPrint to FALSE if the user presses the Cancel button.

	return s_fContinuePrint;
}

// Window procedure for the cancel dialog.
LRESULT CALLBACK AbortPrintJob(
		HWND hwndDlg,     // handle to dialog box
		UINT message,     // type of message
		WPARAM wParam,    // message-specific information
		LPARAM lParam)    // message-specific information
{
	switch (message)
	{
		case WM_INITDIALOG:  // message: initialize dialog box

			// Initialize the static text control.
			// ENHANCE JohnT (maybe V2?): init with some sort of document name.

			//SetDlgItemText(hwndDlg, IDD_FILE, "Some view");

			return TRUE;

		case WM_COMMAND:     // the only possible command is the cancel button

			s_fContinuePrint = FALSE;
			if (s_pvpc)
				s_pvpc->RequestAbort();

			return TRUE;

		default:
			return FALSE;
	}
	UNREFERENCED_PARAMETER(lParam);
	UNREFERENCED_PARAMETER(wParam);
}

void ProcessHeader(ITsString * ptss, int vhpTopBottom, bool fShowOnFirstPage,
	ITsStrFactory * ptsf, IVwPrintContext * pvpc)
{
	if (!ptss)
		return;

	const OLECHAR * pszHeader;
	int cchTotal;
	CheckHr(ptss->LockText(&pszHeader, &cchTotal));

	int cchLeft = cchTotal;
	int cchCenter = 0;
	int cchRight = 0;
	if (!cchLeft)
	{
		ptss->UnlockText(pszHeader);
		return;
	}
	// Look for one or two commas in header string.
	OLECHAR * pchFirstComma = (OLECHAR*)wcschr(pszHeader, ',');
	OLECHAR * pchSecondComma = NULL;
	if (pchFirstComma)
	{
		pchSecondComma = wcschr(pchFirstComma + 1, ',');
		cchLeft = pchFirstComma - pszHeader;
		cchRight = cchTotal - cchLeft - 1;
	}
	if (pchSecondComma)
	{
		cchRight = cchTotal - (pchSecondComma - pszHeader) - 1;
		cchCenter = pchSecondComma - pchFirstComma - 1;
	}

	// qtssLeft ------------------------------------------
	ITsStringPtr qtssLeft;
	qtssLeft = ptss;
	ITsStrBldrPtr qtsb;
	// Get a builder to work on the string.
	CheckHr(qtssLeft->GetBldr(&qtsb));
	// Replace the entire string to the right of (and including) the left most comma with ""
	CheckHr(qtsb->ReplaceRgch(cchLeft, cchTotal, L"", 0, NULL));
	// Now replace the string in qtss with the modified string.
	CheckHr(qtsb->GetString(&qtssLeft));

	// qtssCenter ------------------------------------------
	ITsStringPtr qtssCenter;
	if (pchSecondComma)
	{
		qtssCenter = ptss;
		// Get a builder to work on the string.
		CheckHr(qtssCenter->GetBldr(&qtsb));
		// "Erase" the entire string to the right of (and including) the second comma
		if (cchRight)
			CheckHr(qtsb->ReplaceRgch((cchTotal - cchRight)-1, cchTotal, L"", 0, NULL));
		else
			CheckHr(qtsb->ReplaceRgch(cchTotal - 1, cchTotal, L"", 0, NULL));
		// "Erase" the entire string to the left of (and including) the first comma
		CheckHr(qtsb->ReplaceRgch(0, cchLeft+1, L"", 0, NULL));
		// Now replace the string in qtss with the modified string.
		CheckHr(qtsb->GetString(&qtssCenter));
	}

	// qtssRight ------------------------------------------
	ITsStringPtr qtssRight;
	if (pchFirstComma)
	{
		qtssRight = ptss;
		// Get a builder to work on the string.
		CheckHr(qtssRight->GetBldr(&qtsb));
		// "Erase" the entire string to the left of (and including) the first comma
		if (cchRight)
			CheckHr(qtsb->ReplaceRgch(0, cchTotal - cchRight, L"", 0, NULL));
		else
			if (pchSecondComma)
				CheckHr(qtsb->ReplaceRgch(0, cchTotal, L"", 0, NULL));
			else
				CheckHr(qtsb->ReplaceRgch(0, cchLeft+1, L"", 0, NULL));
		// Now replace the string in qtss with the modified string.
		CheckHr(qtsb->GetString(&qtssRight));
	}

	ITsStringPtr qtss;
	qtss = qtssLeft;
	int vhp = (vhpTopBottom | kvhpLeft);
	if (pchFirstComma == 0)
	{
		// No commas--the first string is centered
		vhp = (vhpTopBottom | kvhpCenter);
	}
	CheckHr(pvpc->SetHeaderString((VwHeaderPositions)vhp, qtss));
	if (fShowOnFirstPage)
	{
		vhp |= kvhpFirst;
		CheckHr(pvpc->SetHeaderString((VwHeaderPositions)vhp, qtss));
	}
	if (cchCenter)
	{
		vhp = (vhpTopBottom | kvhpCenter);
		if (pchSecondComma == 0)
		{
			// One comma--the second string is on the right
			vhp = (vhpTopBottom | kvhpRight);
		}
		qtss = qtssCenter;
		CheckHr(pvpc->SetHeaderString((VwHeaderPositions)vhp, qtss));
		if (fShowOnFirstPage)
		{
			vhp |= kvhpFirst;
			CheckHr(pvpc->SetHeaderString((VwHeaderPositions)vhp, qtss));
		}
	}
	if (cchRight)
	{
		vhp = (vhpTopBottom | kvhpRight);
		qtss = qtssRight;
		CheckHr(pvpc->SetHeaderString((VwHeaderPositions)vhp, qtss));
		if (fShowOnFirstPage)
		{
			vhp |= kvhpFirst;
			CheckHr(pvpc->SetHeaderString((VwHeaderPositions)vhp, qtss));
		}
	}
	ptss->UnlockText(pszHeader);
}


//:>********************************************************************************************
//:>	PrintProgressReceiver methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
PrintProgressReceiver::PrintProgressReceiver(HWND hwndDlg)
{
	m_hwndDlg = hwndDlg;
	m_nProgress = 0;
	m_nRangeMax = 0;
	m_cref = 1;
}

	// IUnknown methods
STDMETHODIMP PrintProgressReceiver::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IAdvInd3)
		*ppv = static_cast<IAdvInd3 *>(this);
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


STDMETHODIMP_(ULONG) PrintProgressReceiver::AddRef()
{
	return InterlockedIncrement(&m_cref);
}

STDMETHODIMP_(ULONG) PrintProgressReceiver::Release()
{
	long cref = InterlockedDecrement(&m_cref);
	if (cref == 0)
	{
		m_cref = 1;
		delete this;
	}
	return cref;
}

STDMETHODIMP PrintProgressReceiver::Step(int nStepAmt)
{
	BEGIN_COM_METHOD;

	m_nProgress += nStepAmt;

	if (!m_nRangeMax)
		return E_UNEXPECTED;

	int nPercent = (int)(0.5 + 100.0 * m_nProgress / m_nRangeMax);
	StrApp strOut;
	StrApp strFmt(m_stuFmt);
	strOut.Format(strFmt.Chars(), nPercent);
	::SetDlgItemText(m_hwndDlg, kctidPrintProgress, strOut.Chars());
	return S_OK;

	END_COM_METHOD(g_fact5, IID_IAdvInd);
}

// This is called when one complete stage of the process being monitored is complete.
STDMETHODIMP PrintProgressReceiver::NextStage()
{
	BEGIN_COM_METHOD;

	m_nProgress = 0;
	Step(0);
	return S_OK;

	END_COM_METHOD(g_fact5, IID_IAdvInd2);
}

STDMETHODIMP PrintProgressReceiver::put_Message(BSTR bstrMessage)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrMessage);

	m_stuFmt = bstrMessage;
	StrApp strOut(m_stuFmt);
	::SetDlgItemText(m_hwndDlg, kctidPrintProgress, strOut.Chars());
	return S_OK;

	END_COM_METHOD(g_fact5, IID_IAdvInd3);
}

STDMETHODIMP PrintProgressReceiver::put_Position(int nPos)
{
	BEGIN_COM_METHOD;
	if (nPos < 0 || nPos >= m_nRangeMax)
	{
		Assert(false);
		return E_INVALIDARG;
	}
	m_nProgress = nPos;
	Step(0);
	return S_OK;

	END_COM_METHOD(g_fact5, IID_IAdvInd3);
}

STDMETHODIMP PrintProgressReceiver::put_StepSize(int nStepInc)
{
	BEGIN_COM_METHOD;

	Assert(false);
	return E_NOTIMPL;

	END_COM_METHOD(g_fact5, IID_IAdvInd3);
}

STDMETHODIMP PrintProgressReceiver::put_Title(BSTR bstrTitle)
{
	BEGIN_COM_METHOD;

	Assert(false);
	return E_NOTIMPL;

	END_COM_METHOD(g_fact5, IID_IAdvInd3);
}

STDMETHODIMP PrintProgressReceiver::SetRange(int nMin, int nMax)
{
	BEGIN_COM_METHOD;
	if (nMin != 0)
	{
		Assert(false);
		return E_INVALIDARG;
	}
	m_nRangeMax = nMax;

	return S_OK;

	END_COM_METHOD(g_fact5, IID_IAdvInd3);
}


/*----------------------------------------------------------------------------------------------
	Run the print dialog, and print.
----------------------------------------------------------------------------------------------*/
bool AfVwRootSite::CmdFilePrint1(Cmd * pcmd)
{
	AssertObj(pcmd);
	if (!m_qrootb)
		return true; // trivially handled (report error?)

	AfMainWnd * pafw = Window()->MainWindow();
	AssertPtr(pafw);

	POrientType potPageOrient;
	PgSizeType pstPageSize;
	int dxmpLeftMargin;
	int dxmpRightMargin;
	int dympTopMargin;
	int dympBottomMargin;
	int dympHeaderMargin;
	int dympFooterMargin;
	int dxmpPageWidth;
	int dympPageHeight;
	ITsStringPtr qtssHeader;
	ITsStringPtr qtssFooter;
	bool fHeaderOnFirstPage;

	AfWnd * pwndMain;
	// Find the main window
	for (pwndMain = Window();
		!dynamic_cast<AfMainWnd *>(pwndMain) && pwndMain->Parent();
		pwndMain = pwndMain->Parent())
	{
	}

	pafw->GetPageSetupInfo(&potPageOrient, &pstPageSize,
		&dxmpLeftMargin, &dxmpRightMargin,
		&dympTopMargin, &dympBottomMargin,
		&dympHeaderMargin, &dympFooterMargin,
		&dxmpPageWidth, &dympPageHeight,
		&qtssHeader, &qtssFooter, &fHeaderOnFirstPage);

	IVwSelectionPtr qsel;
	CheckHr(m_qrootb->get_Selection(&qsel));
	bool fCanPrintOnlySel = CanPrintOnlySelection() && (qsel.Ptr() != NULL);

	// -- run the print dialog --

	// Initialize PRINTDLG
	// Review JohnT: I found something in MSDN that suggests pd.hDevMode and hDevNames
	// need to be freed somehow; but can't find anything that tells how to free them.
	// Also, Petzold's examples ignore them altogether. For now I'm assuming I can do
	// the same.
	// AlistairI says (11/7/00): I found some useful info in the MSDN library. There
	// now follows my interpretation of what needs to be done. This will feed in the
	// settings from our page setup dialog:
	PRINTDLG pd;
	ZeroMemory(&pd, isizeof(PRINTDLG));
	pd.lStructSize = isizeof(PRINTDLG);
	pd.hwndOwner = Window()->Hwnd(); // Review JohnT: should this be the main window?
	pd.hDevMode = NULL; // We'll let Windows fill this in for us.
	pd.hDevNames = NULL; // Ditto
	pd.Flags = (fCanPrintOnlySel) ?
		PD_DISABLEPRINTTOFILE :
		PD_NOSELECTION | PD_DISABLEPRINTTOFILE;
	pd.nCopies = 1;
	pd.nFromPage = 0;
	pd.nToPage = 0xFFFF;
	pd.nMinPage = 1;
	pd.nMaxPage = 0xFFFF;

	// Show the Print dialog.
	// At this point, we're really just letting the user confirm which printer to use.
	// Important note: It is possible to navigate from the print dialog to show paper size and
	// orientation settings. These are NOT the settings for the current print out.
	// Compare with Microsoft word, using this test:
	// Set up some text on a page, then select File | Page setup. On the Paper Size tab, select
	// Landscape. Click OK. Now select File | Print. Click on the Properties button. On the
	// Setup tab, note that the orientation says Portrait. Click OK. Now which way round do you
	// think the page will print? It is Landscape!

	if (::PrintDlg(&pd) == 0)
		return true; // User pressed Cancel.

	bool fPrintOnlySel = pd.Flags & PD_SELECTION;

	// We now have access to all the settings:
	DEVMODE * pdevmDevModeDataReal = (DEVMODE *)::GlobalLock(pd.hDevMode);
	// Make a vector of bytes to ensure it gets freed eventually.
	// To allow for later versions of the OS, we need to allocate a size obtained from
	// the object we're copying, not the size of DEVMODE at the time we build.
	Vector<byte> buffer;
	buffer.Resize(pdevmDevModeDataReal->dmSize);
	DEVMODE * pdevmDevModeData = (DEVMODE *)buffer.Begin();
	CopyBytes(pdevmDevModeDataReal, pdevmDevModeData, pdevmDevModeDataReal->dmSize);

	// Get names of printer device, port etc. We need these so we can access the driver, find
	// out the location of its ExtDeviceMode function, and write our values to the printer.
	DEVNAMES * pdevnDevNamesData = (DEVNAMES *)::GlobalLock(pd.hDevNames);
	if (pdevmDevModeData && pdevnDevNamesData)
	{
		// Access printer settings
		const achar * pszDeviceName = reinterpret_cast<const achar *>(pdevnDevNamesData) +
			pdevnDevNamesData->wDeviceOffset;
		const achar * pszDriverNameTemplate =
			reinterpret_cast<const achar *>(pdevnDevNamesData) +
			pdevnDevNamesData->wDriverOffset;
		const achar * pszDriverNameExtension = _T("drv");
		StrApp strDriverName(pszDriverNameTemplate);
		strDriverName.Append(_T("."));
		strDriverName.Append(pszDriverNameExtension);
		const achar * pszPortName = reinterpret_cast<const achar *>(pdevnDevNamesData) +
			pdevnDevNamesData->wOutputOffset;

#ifdef JOHNT_1_2_20002_WIN98FIX
		// Get printer driver:
		HINSTANCE hDriver = LoadLibrary(strDriverName.Chars());
		// The following code works fine on Windows 2000, but the alternative should
		// work on all 32-bit Windows systems.
		if (hDriver)
		{
			// Access printer driver
			LPFNDEVMODE pfnExtDeviceMode;

			// Get address of function we need to write our values to printer driver:
			pfnExtDeviceMode = (LPFNDEVMODE)GetProcAddress(hDriver, (LPSTR)"EXTDEVICEMODE");

			if (pfnExtDeviceMode)
			{
				// Make a note of which data fields are supported:
				DWORD grfdmSavedFields = pdevmDevModeData->dmFields;
				// Initialize flag saying which fields we've changed:
				pdevmDevModeData->dmFields = 0;

				if (grfdmSavedFields & DM_ORIENTATION)
				{
					// Check what orientation setting the user requested:
					switch (potPageOrient)
					{
					case kPort:
						pdevmDevModeData->dmOrientation = DMORIENT_PORTRAIT;
						pdevmDevModeData->dmFields |= DM_ORIENTATION;
						break;
					case kLands:
						pdevmDevModeData->dmOrientation = DMORIENT_LANDSCAPE;
						pdevmDevModeData->dmFields |= DM_ORIENTATION;
						break;
					default:
						// Unknown orientation, so let's just leave the printer's default:
						break;
					}
				}

				if (grfdmSavedFields & DM_PAPERSIZE)
				{
					// Check what page size setting the user requested:
					switch (pstPageSize)
					{
					case kSzLtr:
						pdevmDevModeData->dmPaperSize = DMPAPER_LETTER;
						pdevmDevModeData->dmFields |= DM_PAPERSIZE;
						break;
					case kSzLgl:
						pdevmDevModeData->dmPaperSize = DMPAPER_LEGAL;
						pdevmDevModeData->dmFields |= DM_PAPERSIZE;
						break;
					case kSzA4:
						pdevmDevModeData->dmPaperSize = DMPAPER_A4;
						pdevmDevModeData->dmFields |= DM_PAPERSIZE;
						break;
					case kSzCust:
					default:
						pdevmDevModeData->dmPaperSize = 0;
						pdevmDevModeData->dmFields |= DM_PAPERSIZE;
						// Note that the page height and width are currently in inches/72000.
						// The DEVMODE structure needs mm/10. There are 25.4mm in an inch:
						pdevmDevModeData->dmPaperLength =
							(short)(MulDiv (dympPageHeight, 254, 72000));
						pdevmDevModeData->dmFields |= DM_PAPERLENGTH;
						pdevmDevModeData->dmPaperWidth =
							(short)(MulDiv (dxmpPageWidth, 254, 72000));
						pdevmDevModeData->dmFields |= DM_PAPERWIDTH;
						break;
					}
				}

				// Write new printer settings. Use our modified copy as input,
				// write back to the full version we will use to make the DC:
				pfnExtDeviceMode(NULL, hDriver, pdevmDevModeDataReal, pszDeviceName,
					pszPortName, pdevmDevModeData, NULL, DM_OUT_BUFFER | DM_IN_BUFFER);

			}
			// Now, at last, we can create our printer device context.
			// I'll put it into the pd structure, for convenience:
			// Note that we must create the DC, even if (as for example on W98)
			// we can't get a pfnExtDeviceMode.
			pd.hDC = CreateDC(strDriverName.Chars(), pszDeviceName, pszPortName,
				pdevmDevModeDataReal);
			// Done accessing printer driver
		}
#else
		// Make a note of which data fields are supported:
		DWORD grfdmSavedFields = pdevmDevModeData->dmFields;
		// Initialize flag saying which fields we've changed:
		pdevmDevModeData->dmFields = 0;

		if (grfdmSavedFields & DM_ORIENTATION)
		{
			// Check what orientation setting the user requested:
			switch (potPageOrient)
			{
			case kPort:
				pdevmDevModeData->dmOrientation = DMORIENT_PORTRAIT;
				pdevmDevModeData->dmFields |= DM_ORIENTATION;
				break;
			case kLands:
				pdevmDevModeData->dmOrientation = DMORIENT_LANDSCAPE;
				pdevmDevModeData->dmFields |= DM_ORIENTATION;
				break;
			default:
				// Unknown orientation, so let's just leave the printer's default:
				break;
			}
		}

		if (grfdmSavedFields & DM_PAPERSIZE)
		{
			// Check what page size setting the user requested:
			switch (pstPageSize)
			{
			case kSzLtr:
				pdevmDevModeData->dmPaperSize = DMPAPER_LETTER;
				pdevmDevModeData->dmFields |= DM_PAPERSIZE;
				break;
			case kSzLgl:
				pdevmDevModeData->dmPaperSize = DMPAPER_LEGAL;
				pdevmDevModeData->dmFields |= DM_PAPERSIZE;
				break;
			case kSzA4:
				pdevmDevModeData->dmPaperSize = DMPAPER_A4;
				pdevmDevModeData->dmFields |= DM_PAPERSIZE;
				break;
			case kSzCust:
			default:
				pdevmDevModeData->dmPaperSize = 0;
				pdevmDevModeData->dmFields |= DM_PAPERSIZE;
				// Note that the page height and width are currently in inches/72000.
				// The DEVMODE structure needs mm/10. There are 25.4mm in an inch:
				pdevmDevModeData->dmPaperLength =
					(short)(MulDiv (dympPageHeight, 254, 72000));
				pdevmDevModeData->dmFields |= DM_PAPERLENGTH;
				pdevmDevModeData->dmPaperWidth =
					(short)(MulDiv (dxmpPageWidth, 254, 72000));
				pdevmDevModeData->dmFields |= DM_PAPERWIDTH;
				break;
			}

		}
		// Write new printer settings:
		// Note (JohnT): seems somewhere we should already have the printer "open" from
		// calling the print dialog, but I don't know how to get a printer handle from it.
		// It doesn't seem to hurt to have this extra open/close pair before we actually
		// print.
		HANDLE hPrinter;
		StrApp strDevice(pszDeviceName);
		if (::OpenPrinter(const_cast<achar *>(strDevice.Chars()), &hPrinter, NULL))
		{
			// We can set its modes. (Ignore any error here.)
			// But, even though we didn't specify PD_USEDEVMODECOPIES, if the printer can
			// collate or do multiple copies, W2000 leaves it up to the printer.
			// Unfortunately, this call wipes out the critical values in those fields,
			// so if we don't preserve them, we don't get the multiple copies etc.
			short sCopies = pdevmDevModeData->dmCopies;
			short fCollate = pdevmDevModeData->dmCollate;
			::DocumentProperties(pwndMain->Hwnd(), hPrinter,
				const_cast<achar *>(strDevice.Chars()),
				pdevmDevModeDataReal, pdevmDevModeData, DM_IN_BUFFER | DM_OUT_BUFFER);
			pdevmDevModeDataReal->dmCollate = fCollate;
			pdevmDevModeDataReal->dmCopies = sCopies;
			// It also seems to ignore what we tell it about paper size.
			pdevmDevModeDataReal->dmPaperLength = pdevmDevModeData->dmPaperLength;
			pdevmDevModeDataReal->dmPaperWidth = pdevmDevModeData->dmPaperWidth;
			::ClosePrinter(hPrinter);
		}
		// Now, at last, we can create our printer device context.
		// I'll put it into the pd structure, for convenience:
		StrApp strPort(pszPortName);
		pd.hDC = ::CreateDC(strDriverName.Chars(), strDevice.Chars(), strPort.Chars(),
			pdevmDevModeDataReal);

#endif
		// Done accessing printer settings
	}

	// It is our resposibility to free up the data structures that Windows allocated:
	if (pd.hDevMode)
	{
		::GlobalUnlock(pd.hDevMode);
		::GlobalFree(pd.hDevMode);
		pd.hDevMode = NULL;
	}
	if (pd.hDevNames)
	{
		::GlobalUnlock(pd.hDevNames);
		::GlobalFree(pd.hDevNames);
		pd.hDevNames = NULL;
	}

	if (!pd.hDC)
		// Couldn't make device context, for some reason.
		return true;

	// Test that the current printer can stretch dibs:
	if (!(GetDeviceCaps(pd.hDC, RASTERCAPS) & RC_STRETCHDIB))
	{
		DeleteDC(pd.hDC);
		StrApp staMsg(kstidCannotPrint);
		MessageBox(Window()->Hwnd(), staMsg.Chars(), 0, MB_OK);
		return true;
	}

	// OK, we will try to print.
	// From here on we have a device context to free if something goes wrong,
	// so use a try...catch
	try
	{
		// Register the application's AbortProc function with GDI.
		SetAbortProc(pd.hDC, AbortProc);

		// Display the modeless Cancel dialog box.
		if (s_hwndCancel)
			DestroyWindow(s_hwndCancel);
		s_hwndCancel = CreateDialog(ModuleEntry::GetModuleHandle(),
			MAKEINTRESOURCE(kridPrintCancelDlg),
			pwndMain->Hwnd(), (DLGPROC) AbortPrintJob);

		PrintProgressReceiverPtr qppr;
		qppr.Attach(NewObj PrintProgressReceiver(s_hwndCancel));
		qppr->SetRange(0, pd.nToPage + 1);

		// Disable the main window during printing.
		::EnableWindow(pwndMain->Hwnd(), false);

		// Set stuff up for the cancel dialog
		s_fContinuePrint = true;

		// Create and initialize the print context
		IVwPrintContextPtr qvpc;
		qvpc.CreateInstance(CLSID_VwPrintContextWin32);
		IVwGraphicsWin32Ptr qvg32;
		qvg32.CreateInstance(CLSID_VwGraphicsWin32);
		CheckHr(qvg32->Initialize(pd.hDC));
		CheckHr(qvpc->SetGraphics(qvg32));
		s_pvpc = qvpc;

		// Header and footer strings. We are not doing mirroring or left/right pages.
		qvpc->put_HeaderMask(kgrfvhpNormal);
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		ProcessHeader(qtssHeader, kvhpTop, fHeaderOnFirstPage, qtsf, qvpc);
		ProcessHeader(qtssFooter, kvhpBottom, true, qtsf, qvpc);

		// Margins and page counts
		int dxpInch = ::GetDeviceCaps(pd.hDC, LOGPIXELSX);
		int dypInch = ::GetDeviceCaps(pd.hDC, LOGPIXELSY);

		// Unprintable distance on each margin. This needs to be subtracted from the
		// actual page margins, since the print process measures margins from the
		// device clip rectangle, which reflects the printable part of the page.
		int dxpLeftUnPrint = ::GetDeviceCaps(pd.hDC, PHYSICALOFFSETX);
		int dypTopUnPrint = ::GetDeviceCaps(pd.hDC, PHYSICALOFFSETY);

		// Page size
//		int dxpPageWidth = ::GetDeviceCaps(pd.hDC, PHYSICALWIDTH);
//		int dypPageHeight = ::GetDeviceCaps(pd.hDC, PHYSICALHEIGHT);
#ifdef JohnT_2004_6_11_NoGoodOnSomePrinters
		// It seems some printers always fill in these fields, but some fill them
		// in only if the user chooses a non-standard paper size. Otherwise,
		// all that is set is an enumeration in pdevmDevModeData->dmPaperSize,
		// which there is no obvious way to interpret.
		int dxpPageWidth = pdevmDevModeData->dmPaperWidth * dxpInch / 254;
		int dypPageHeight = pdevmDevModeData->dmPaperLength * dypInch / 254;
//#else This always gives the maximum size regardless of custom sizes.
		// Review KenZ (JohnT) (see above comment): is this reliable? Or will it totally defeat custom
		// page sizes and always use the maximum paper size the device is capable of?
		// Even that might be better than crashing when we can't get page size info...
		// It seems to be really hard to get the actual page size the user has requested.
		// This MIGHT work, since it is asking for the size of the paper in the actual
		// DC that has been initialized for this job...MAYBE it already allows for
		// what paper size the user said to use?
		int dxpPageWidth = ::GetDeviceCaps(pd.hDC, PHYSICALWIDTH);
		int dypPageHeight = ::GetDeviceCaps(pd.hDC, PHYSICALHEIGHT);
#else
		// If we have a valid width/height override, use it regardless of anything else.
		int dxpPageWidth = pdevmDevModeData->dmFields & DM_PAPERWIDTH ?
			pdevmDevModeData->dmPaperWidth * dxpInch / 254 : 0;
		int dypPageHeight = pdevmDevModeData->dmFields & DM_PAPERLENGTH ?
			pdevmDevModeData->dmPaperLength * dxpInch / 254 : 0;

		DWORD dwPaperSize = pdevmDevModeData->dmFields & DM_PAPERSIZE ?
			pdevmDevModeData->dmPaperSize : 0;
		switch (dwPaperSize)
		{
		case 0:
			// Custom size or dmPaperSize not set. If not set, we just go for
			// the physical paper size from the printer.
			if (!dxpPageWidth)
				dxpPageWidth = ::GetDeviceCaps(pd.hDC, PHYSICALWIDTH);
			if (!dypPageHeight)
				dypPageHeight = ::GetDeviceCaps(pd.hDC, PHYSICALHEIGHT);
			break;
		case DMPAPER_LEGAL:
			if (!dxpPageWidth)
				dxpPageWidth = 2159 * dxpInch / 254; // 2159 DEVMODE units for 8.5"
			if (!dypPageHeight)
				dypPageHeight = 3556 * dypInch / 254;
			break;
		case DMPAPER_A4:
			if (!dxpPageWidth)
				dxpPageWidth = 2100 * dxpInch / 254;
			if (!dypPageHeight)
				dypPageHeight = 2970 * dypInch / 254;
			break;
		case DMPAPER_LETTER:
		default: // Use LETTER if we don't interpret the actual size.
			if (!dxpPageWidth)
				dxpPageWidth = 2159 * dxpInch / 254;
			if (!dypPageHeight)
				dypPageHeight = 2794 * dypInch / 254;
			break;
		}
		// Switch width/height if landscape orientation and not custom size.
		if (pdevmDevModeData->dmFields & DM_ORIENTATION &&
			pdevmDevModeData->dmOrientation == DMORIENT_LANDSCAPE && dwPaperSize)
		{
			int nTemp = dxpPageWidth;
			dxpPageWidth = dypPageHeight;
			dypPageHeight = nTemp;
		}

#endif
		Rect rcPrint;
		::GetClipBox(pd.hDC, &rcPrint); // Get the printable area.
//StrUni stu;
//stu.Format(L"dmPaperSize = %d, dxpPageWidth = %d, dypPageHeight = %d, dxpInch = %d, dypInch = %d", pdevmDevModeData->dmPaperSize, dxpPageWidth, dypPageHeight, dxpInch, dypInch);
//::MessageBox(NULL, stu.Chars(), L"CmdFilePrint1", MB_OK);

		int dxpRightUnPrint = dxpPageWidth - rcPrint.Width() - dxpLeftUnPrint;
		int dypBottomUnPrint = dypPageHeight - rcPrint.Height() - dypTopUnPrint;

		// TODO JohnT: check each of these margins comes out positive, if not, put up the
		// "Part of the document is printing outside the printable area" dialog, offer to fix,
		// if so change the problem margins to be the minimum for the printer.

		int iLeftMargin = MulDiv(dxmpLeftMargin, dxpInch, kdzmpInch) - dxpLeftUnPrint;
		int iRightMargin = MulDiv(dxmpRightMargin, dxpInch, kdzmpInch) - dxpRightUnPrint;
		int iHeaderMargin = MulDiv(dympHeaderMargin, dypInch, kdzmpInch) - dypTopUnPrint;
		int iTopMargin = MulDiv(dympTopMargin, dypInch, kdzmpInch) - dypTopUnPrint;
		int iBottomMargin = MulDiv(dympBottomMargin, dypInch, kdzmpInch) - dypBottomUnPrint;
		int iFooterMargin = MulDiv(dympFooterMargin, dypInch, kdzmpInch) - dypBottomUnPrint;


		StrApp strHelpUrl(AfApp::Papp()->GetHelpFile());
		strHelpUrl.Append(_T("::/"));
		StrApp strPath2 = _T("User_Interface/Menus/File/Page_Setup.htm");
		strHelpUrl.Append(strPath2);
#ifdef DEBUG
	// We probably want to change all occurrences of GetCurMainWnd() to MainWindow()
	// in this file, but we didn't take the time to verify this would always work. These DEBUG
	// asserts in this file are a temporary test to see if there is ever any difference between
	// the two. If one is hit, it's likely that GetCurMainWnd is returning the wrong value. If
	// this doesn't cause any problem after some time, we can go ahead and switch them all
	// to MainWindow.
	AfMainWnd * pafw1 = AfApp::Papp()->GetCurMainWnd();
	AfMainWnd * pafw2 = m_pwndSubclass->MainWindow();
	Assert(pafw1 == pafw2);
#endif
		AfMainWndPtr qafwTop = AfApp::Papp()->GetCurMainWnd();
		qafwTop->SetFullHelpUrl(strHelpUrl.Chars());

		int nRes = IDYES;
		StrApp strTitle;
		StrApp strMessage;

		HWND hwnd = qafwTop->Hwnd();

		if (iLeftMargin < 0)
		{
			strMessage.Load(kstidLeftMarginError);
			StrApp strTitle(kstidUnprintableText);
			nRes = ::MessageBox(hwnd, strMessage.Chars(), strTitle.Chars(),
				MB_ICONQUESTION | MB_YESNO | MB_DEFBUTTON2 | MB_HELP);
		}
		if (nRes == IDYES && iRightMargin < 0)
		{
			strMessage.Load(kstidRightMarginError);
			StrApp strTitle(kstidUnprintableText);
			nRes = ::MessageBox(hwnd, strMessage.Chars(), strTitle.Chars(),
				MB_ICONQUESTION | MB_YESNO | MB_DEFBUTTON2 | MB_HELP);
		}
		if (nRes == IDYES && iHeaderMargin < 0)
		{
			strMessage.Load(kstidHeaderMarginError);
			StrApp strTitle(kstidUnprintableHeader);
			nRes = ::MessageBox(hwnd, strMessage.Chars(), strTitle.Chars(),
				MB_ICONQUESTION | MB_YESNO | MB_DEFBUTTON2 | MB_HELP);
		}
		if (nRes == IDYES && iTopMargin < 0)
		{
			strMessage.Load(kstidTopMarginError);
			StrApp strTitle(kstidUnprintableText);
			nRes = ::MessageBox(hwnd, strMessage.Chars(), strTitle.Chars(),
				MB_ICONQUESTION | MB_YESNO | MB_DEFBUTTON2 | MB_HELP);
		}
		if (nRes == IDYES && iBottomMargin < 0)
		{
			strMessage.Load(kstidBottomMarginError);
			StrApp strTitle(kstidUnprintableText);
			nRes = ::MessageBox(hwnd, strMessage.Chars(), strTitle.Chars(),
				MB_ICONQUESTION | MB_YESNO | MB_DEFBUTTON2 | MB_HELP);
		}
		if (nRes == IDYES && iFooterMargin < 0)
		{
			strMessage.Load(kstidFooterMarginError);
			StrApp strTitle(kstidUnprintableFooter);
			nRes = ::MessageBox(hwnd, strMessage.Chars(), strTitle.Chars(),
				MB_ICONQUESTION | MB_YESNO | MB_DEFBUTTON2 | MB_HELP);
		}
		qafwTop->ClearFullHelpUrl();

		if (nRes == IDNO)
		{
			::EnableWindow(pwndMain->Hwnd(), true);
			// Remove the AbortPrintJob dialog box.
			DestroyWindow(s_hwndCancel);
			BOOL fSuccess;
			fSuccess = ::DeleteDC(pd.hDC);
			Assert(fSuccess);
			return true;
		}
		s_fBusyPrinting = true;

		CheckHr(qvpc->SetMargins(iLeftMargin, iRightMargin, iHeaderMargin, iTopMargin,
			iBottomMargin, iFooterMargin));

		CheckHr(qvpc->SetPagePrintInfo(1, pd.nFromPage, pd.nToPage, pd.nCopies,
			pd.Flags & PD_COLLATE));

		if (fPrintOnlySel)
			// Store a list of selected objects under a dummy HVO in the cache.
			CacheSelectedObjects();

		// Make an extra root box and tell it to print...
		ISilDataAccessPtr qsdaT;
		CheckHr(m_qrootb->get_DataAccess(&qsdaT));
		ILgWritingSystemFactoryPtr qwsf;
		CheckHr(qsdaT->get_WritingSystemFactory(&qwsf));
		IVwRootBoxPtr qrootbPrint;
		MakeRoot(qvg32, qwsf, &qrootbPrint, fPrintOnlySel);
		Rect rcTrans(0, 0, dxpInch, dypInch);
		AfPrintRootSitePtr qprs;

		// This mirrors as closely as possible the algorithm used by VwRootBox::Print
		// to compute the available width.
		int dxpLeft, dxpRight, dypHeader, dypTop, dypBottom, dypFooter;
		CheckHr(qvpc->GetMargins(&dxpLeft, &dxpRight, &dypHeader, &dypTop,
			&dypBottom, &dypFooter));
		int left, top, right, bottom;
		CheckHr(qvg32->GetClipRect(&left, &top, &right, &bottom));
		Rect rcDoc;
		rcDoc.left = left;
		rcDoc.right = right;
		rcDoc.left += dxpLeft;
		rcDoc.right -= dxpRight;
		int dxpAvailWidth = rcDoc.Width();


		qprs.Attach(NewObj AfPrintRootSite(qvg32, rcTrans, rcTrans, dxpAvailWidth));
		CheckHr(qrootbPrint->SetSite(qprs));
		CheckHr(qrootbPrint->Print(qvpc, qppr));
		CheckHr(qrootbPrint->Close());
		// In case MakeRoot registered the root box, as most do, make sure it doesn't
		// stay registered.
		if (dynamic_cast<AfMainWnd *>(pwndMain))
			dynamic_cast<AfMainWnd *>(pwndMain)->UnregisterRootBox(qrootbPrint);
		s_pvpc = NULL;
		s_fBusyPrinting = false;
		for (int i = 0; i < s_vrsDraw.Size(); ++i)
			s_vrsDraw[i]->Invalidate();
		s_vrsDraw.Clear();
	}
	catch (...)
	{
		s_fBusyPrinting = false;
		for (int i = 0; i < s_vrsDraw.Size(); ++i)
			s_vrsDraw[i]->Invalidate();
		s_vrsDraw.Clear();
		::EnableWindow(pwndMain->Hwnd(), true);
		BOOL fSuccess;
		fSuccess = ::DeleteDC(pd.hDC);
		Assert(fSuccess);

		// Remove the AbortPrintJob dialog box.
		DestroyWindow(s_hwndCancel);
		s_pvpc = NULL;
		throw;
	}
	::EnableWindow(pwndMain->Hwnd(), true);
	// Remove the AbortPrintJob dialog box.
	DestroyWindow(s_hwndCancel);
	BOOL fSuccess;
	fSuccess = ::DeleteDC(pd.hDC);
	Assert(fSuccess);

	return true;
}

//:>********************************************************************************************
//:>	AfVwSelInfo methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Load into your member variables all the information about the current selection (if psel
	is null) or the given selection.
----------------------------------------------------------------------------------------------*/
void AfVwSelInfo::Load(IVwRootBox * prootb, IVwSelection * psel)
{
	IVwSelectionPtr qvwsel;
	if (psel)
		qvwsel = psel;
	else
		CheckHr(prootb->get_Selection(&qvwsel));

	int cvsliAnchor;
	CheckHr(qvwsel->CLevels(false, &cvsliAnchor));
	cvsliAnchor--; // CLevels includes the string property itself, but AllSelInfo does not need
					// it
	if (cvsliAnchor < 0)
		return;
	m_vvsli.Resize(cvsliAnchor);

//	CheckHr(qvwsel->AllTextSelInfo(&m_ihvoRoot, cvsliAnchor, m_vvsli.Begin(),
//		&m_tagTextProp, &m_cpropPrevious, &m_ichAnchor, &m_ichEnd,
//		&m_ws, &m_fAssocPrev, &m_ihvoEnd, &m_qttp));

	int cvsliEnd;
	CheckHr(qvwsel->CLevels(true, &cvsliEnd));
	cvsliEnd--;

	try {
		CheckHr(qvwsel->AllSelEndInfo(false, // asking about anchor
				&m_ihvoRoot, cvsliAnchor, m_vvsli.Begin(), &m_tagTextProp, &m_cpropPrevious,
				&m_ichAnchor, &m_ws, &m_fAssocPrev, &m_qttp));
	}
	catch(Throwable & thr) {
		if (thr.Result() == E_NOTIMPL)
		{
			// picture selection or the like. Can't restore.
			m_vvsli.Resize(0);
			m_cpropPrevious = -1; // flag warns Set method
			return;
		}
		else
			throw(thr);
	}

	ComBool fAssocPrevBogus;
	if (cvsliEnd >= 0)
	{
		// The end is in a reasonable place to get information.
		m_vvsliEnd.Resize(cvsliEnd);
		CheckHr(qvwsel->AllSelEndInfo(true, // asking about end
			&m_ihvoEnd, cvsliEnd, m_vvsliEnd.Begin(), &m_tagTextPropEnd, &m_cpropPreviousEnd,
			&m_ichEnd, &m_ws, &fAssocPrevBogus, NULL));
	}
	else
	{
		// Somewhat pathological; the endpoint is probably in a non-editable location.
		// Treat the selection as an insertion point.
		m_vvsliEnd.Resize(cvsliAnchor);
		CheckHr(qvwsel->AllSelEndInfo(false, // asking about anchor
			&m_ihvoEnd, cvsliAnchor, m_vvsliEnd.Begin(), &m_tagTextPropEnd, &m_cpropPreviousEnd,
			&m_ichEnd, &m_ws, &fAssocPrevBogus, NULL));
	}
}

/*----------------------------------------------------------------------------------------------
	Load into your member variables information about a selection useful for scrolling the
	root box back to a logically similar position after some change such as turning overlays
	on or off. The rule is that if the selection is currently visible, we plan to make the
	current selection visible again. If the current selection is not visible, we get info
	about a selection at the top left of the window.
----------------------------------------------------------------------------------------------*/
bool AfVwSelInfo::LoadVisible(IVwRootBox * prootb)
{
	IVwRootSitePtr qvrs;
	CheckHr(prootb->get_Site(&qvrs));
	AfVwRootSitePtr qavrs;
	qvrs->QueryInterface(CLSID_AfVwRootSite, (void **) &qavrs); // OK if this fails
	if (!qavrs)
		return false;

	// First try the active selection if any
	IVwSelectionPtr qsel;
	CheckHr(prootb->get_Selection(&qsel));
	if (qsel && qavrs->IsSelectionVisible(qsel))
	{
		Load(prootb, qsel);
		return true;
	}
	{
		int xdLeft, ydTop, xdRight, ydBottom;
		HoldGraphics hg(qavrs);
		hg.m_qvg->GetClipRect(&xdLeft, &ydTop, &xdRight, &ydBottom);
		// Deliberately don't CheckHr. This can fail for various reasons...for example, if
		// called after making some changes and before redrawing, there may be a lazy box
		// at the top left of the window.
		prootb->MakeSelAt(xdLeft + 1,ydTop + 1, hg.m_rcSrcRoot, hg.m_rcDstRoot, false, &qsel);
	}
	if (qsel)
	{
		Load(prootb, qsel);
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Load into your member variables information about a selection useful for scrolling the
	argument root box back to the same position (in terms of its top left) as it now occupies.
	This is used for example to preserve the position of the other pane in a split window.
----------------------------------------------------------------------------------------------*/
bool AfVwSelInfo::LoadTopLeft(AfVwRootSite * pavrs)
{
	IVwRootBoxPtr qrootb;
	pavrs->get_RootBox(&qrootb);
	IVwSelectionPtr qsel;
	HoldGraphicsRaw hg(pavrs);
	int xdLeft, ydTop, xdRight, ydBottom;
	hg.m_qvg->GetClipRect(&xdLeft, &ydTop, &xdRight, &ydBottom);
	// Deliberately don't CheckHr. This can fail for various reasons...for example, if
	// called after making some changes and before redrawing, there may be a lazy box
	// at the top left of the window.
	qrootb->MakeSelAt(xdLeft + 1,ydTop + 1,hg.m_rcSrcRoot, hg.m_rcDstRoot, false, &qsel);
	if (qsel)
	{
		Load(qrootb, qsel);
		Rect rdSec;
		ComBool fSplit, fEndBeforeAnchor;
		CheckHr(qsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &m_rdPrimary, &rdSec,
			&fSplit, &fEndBeforeAnchor));
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Make a selection equivalent to your saved information, and adjust scroll positions to
	make it as close as possible to the position saved with LoadTopLeft.
	Currently only attempts vertical scrolling.
----------------------------------------------------------------------------------------------*/
void AfVwSelInfo::RestorePosition(AfVwRootSite * pavrs)
{
	IVwSelectionPtr qsel;
	IVwRootBoxPtr qrootb;
	pavrs->get_RootBox(&qrootb);
	Set(qrootb, false, &qsel);
	HoldGraphicsRaw hg(pavrs);
	Rect rdPrimary, rdSec;
	ComBool fSplit, fEndBeforeAnchor;
	CheckHr(qsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rdPrimary, &rdSec,
		&fSplit, &fEndBeforeAnchor));
	if (rdPrimary.top != m_rdPrimary.top)
		pavrs->ScrollDown(m_rdPrimary.top - rdPrimary.top);
}


/*----------------------------------------------------------------------------------------------
	Create a selection based on all your member variables. If fInstall is true, install it
	into the root box. Return true if successful.

	(Note: depending on how drastially the view has changed since the AfVwSelInfo was loaded,
	it may be quite legitimate for this routine to fail.)
----------------------------------------------------------------------------------------------*/
bool AfVwSelInfo::Set(IVwRootBox * prootb, bool fInstall, IVwSelection ** ppsel)
{
	IVwSelectionPtr qselAnchor;
	IVwSelectionPtr qselEnd;

	if (m_cpropPrevious < 0)
		return false; // failed to load, can't reset.
	// JohnT: there are various reasons this could fail, such as Undo removing the
	// object previously selected. Ignore all problems.
	HRESULT hr;
	IgnoreHr(hr = prootb->MakeTextSelection(m_ihvoRoot, m_vvsli.Size(), m_vvsli.Begin(),
		m_tagTextProp, m_cpropPrevious,
		m_ichAnchor, m_ichAnchor, m_ws, m_fAssocPrev, -1, m_qttp, false, &qselAnchor));
	if (FAILED(hr))
	{
		return false;
	}
	IgnoreHr(hr = prootb->MakeTextSelection(m_ihvoEnd, m_vvsliEnd.Size(), m_vvsliEnd.Begin(),
		m_tagTextPropEnd, m_cpropPreviousEnd,
		m_ichEnd, m_ichEnd, m_ws, m_fAssocPrev, -1, m_qttp, false, &qselEnd));
	if (FAILED(hr))
	{
		return false;
	}

	IgnoreHr(hr = prootb->MakeRangeSelection(qselAnchor, qselEnd, fInstall, ppsel));
	if (FAILED(hr))
		return false;
	else
		return true;
}

/*----------------------------------------------------------------------------------------------
	Create a selection equivalent to your member variables and make it visible.
	Also, install it is fInstall is true, and return it if ppsel is non-NULL.
	If making the selection fails, just answer false.
----------------------------------------------------------------------------------------------*/
bool AfVwSelInfo::MakeVisible(IVwRootBox * prootb, bool fInstall, IVwSelection ** ppsel)
{
	IVwRootSitePtr qvrs;
	CheckHr(prootb->get_Site(&qvrs));
	AfVwRootSitePtr qavrs;
	qvrs->QueryInterface(CLSID_AfVwRootSite, (void **)&qavrs); // OK if this fails
	if (!qavrs)
		return false;
	IVwSelectionPtr qsel;
	if (ppsel)
		*ppsel = NULL;

//	IVwSelectionPtr qselTmp;
//	HRESULT hr; hr = prootb->MakeTextSelection(m_ihvoRoot, m_vvsli.Size(), m_vvsli.Begin(),
//		m_tagTextProp, m_cpropPrevious,
//		m_ichAnchor, m_ichEnd, m_ws, m_fAssocPrev, m_ihvoEnd, m_qttp, fInstall, &qselTmp);

	if (!Set(prootb, fInstall, &qsel))
		return false;

	if (ppsel)
		*ppsel = qsel.Detach();
	qavrs->ScrollSelectionIntoView(qsel, kssoDefault);
	return true;
}

/*----------------------------------------------------------------------------------------------
	The requested anchor and endpoint may be beyond the end of the string. Try to make
	a selection as near the end of the string as possible.
----------------------------------------------------------------------------------------------*/
bool AfVwSelInfo::MakeBest(IVwRootBox * prootb, bool fInstall, IVwSelection ** ppsel)
{
	int ichAnchorOrig = m_ichAnchor;
	int ichEndOrig = m_ichEnd;

	if (MakeVisible(prootb, fInstall, ppsel))
		return true;

	m_ichAnchor = min(ichAnchorOrig, ichEndOrig);
	m_ichEnd = m_ichAnchor;
	if (m_ichAnchor != ichAnchorOrig || m_ichEnd != ichEndOrig)
	{
		if (MakeVisible(prootb, fInstall, ppsel))
			return true;
	}
	m_ichAnchor--;
	m_ichEnd--;
	while (m_ichAnchor >= 0)
	{
		if (MakeVisible(prootb, fInstall, ppsel))
			return true;
		m_ichAnchor--;
		m_ichEnd--;
	}
	return false;
}

//:>********************************************************************************************
//:>	AfPrintRootSite methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor, destructor.
----------------------------------------------------------------------------------------------*/
AfPrintRootSite::AfPrintRootSite(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int dxpAvailWidth)
{
	m_cref = 1;
	m_qvg = pvg;
	m_rcSrc = rcSrc;
	m_rcDst = rcDst;
	m_dxpAvailWidth = dxpAvailWidth;
}

AfPrintRootSite::~AfPrintRootSite()
{
}


/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwRootSite)
		*ppv = static_cast<IVwRootSite *>(this);
	else
		return E_NOINTERFACE;

	AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	@param pRoot The sender.
	@param twLeft Relative to top left of root box.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::InvalidateRect(IVwRootBox * pRoot, int xdLeft, int ydTop,
	int xdWidth, int ydHeight)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pRoot);

	return S_OK;

	END_COM_METHOD(g_fact4, IID_IVwRootSite);
}


/*----------------------------------------------------------------------------------------------
	Get a graphics object in an appropriate state for drawing and measuring in the view.
	The calling method should pass the IVwGraphics back to ReleaseGraphics() before
	it returns. In particular, problems will arise if OnPaint() gets called before the
	ReleaseGraphics() method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::GetGraphics(IVwRootBox * pRoot, IVwGraphics ** ppvg,
	RECT * prcSrcRoot, RECT * prcDstRoot)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppvg);
	ChkComArgPtr(prcSrcRoot);
	ChkComArgPtr(prcDstRoot);

	*ppvg = m_qvg;
	m_qvg.Ptr()->AddRef();
	*prcSrcRoot = m_rcSrc;
	*prcDstRoot = m_rcDst;
	return S_OK;

	END_COM_METHOD(g_fact4, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Get a graphics object in an appropriate state for drawing and measuring in the view.
	The calling method should pass the IVwGraphics back to ReleaseGraphics() before
	it returns. In particular, problems will arise if OnPaint() gets called before the
	ReleaseGraphics() method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::get_LayoutGraphics(IVwRootBox * pRoot, IVwGraphics ** ppvg)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppvg);

	*ppvg = m_qvg;
	m_qvg.Ptr()->AddRef();
	return S_OK;

	END_COM_METHOD(g_fact4, IID_IVwRootSite);
}
/*----------------------------------------------------------------------------------------------
	Screen version is the same except for print layout views.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::get_ScreenGraphics(IVwRootBox * prootb, IVwGraphics ** ppvg)
{
	return get_LayoutGraphics(prootb, ppvg);
}

/*----------------------------------------------------------------------------------------------
	Screen version is just like the relevant part of GetGraphics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::GetTransformAtDst(IVwRootBox * pRoot,  POINT pt,
	RECT * prcSrcRoot, RECT * prcDstRoot)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(prcSrcRoot);
	ChkComArgPtrN(prcDstRoot);
	*prcSrcRoot = m_rcSrc;
	*prcDstRoot = m_rcDst;
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}
/*----------------------------------------------------------------------------------------------
	Screen version is just like the relevant part of GetGraphics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::GetTransformAtSrc(IVwRootBox * pRoot,  POINT pt,
	RECT * prcSrcRoot, RECT * prcDstRoot)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(prcSrcRoot);
	ChkComArgPtrN(prcDstRoot);
	*prcSrcRoot = m_rcSrc;
	*prcDstRoot = m_rcDst;
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}
/*----------------------------------------------------------------------------------------------
	Inform the container when done with the graphics object.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::ReleaseGraphics(IVwRootBox * prootb, IVwGraphics * pvg)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);

	Assert(pvg == m_qvg.Ptr());
	return S_OK;

	END_COM_METHOD(g_fact4, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Get the width available for laying things out in the view.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::GetAvailWidth(IVwRootBox * prootb, int * ptwWidth)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ptwWidth);

	*ptwWidth = m_dxpAvailWidth;
	return S_OK;

	END_COM_METHOD(g_fact4, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Pending writing system not applicable to printing. -1 is a safe default if by some bizarre
	chance it gets called.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::GetAndClearPendingWs(IVwRootBox * prootb, int * pws)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pws);
	*pws = -1;
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Answer whether boxes in the specified range of destination coordinates
	may usefully be converted to lazy boxes. Should at least answer false
	if any part of the range is visible. The default implementation avoids
	converting stuff within about a screen's height of the visible part(s).

	Print root sites already have a mechanism for discarding boxes they no
	longer need, so just answer false.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::IsOkToMakeLazy(IVwRootBox * prootb, int ydTop, int ydBottom,
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
STDMETHODIMP AfPrintRootSite::OnProblemDeletion(IVwSelection * psel, VwDelProbType dpt,
	VwDelProbResponse * pdpr)
{
	BEGIN_COM_METHOD
	return E_NOTIMPL;
	END_COM_METHOD(g_fact1, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Notifies the site that the size of the root box changed; scroll ranges and/or
	window size may need to be updated. The standard response is to update the scroll range.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::RootBoxSizeChanged(IVwRootBox * prootb)
{
	BEGIN_COM_METHOD

	return S_OK;

	END_COM_METHOD(g_fact4, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Adjust the scroll range when some lazy box got expanded. Needs to be done for both panes
	if we have more than one.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::AdjustScrollRange(IVwRootBox * prootb, int dxdSize, int dxdPosition,
	int dydSize, int dydPosition, ComBool * pfForcedScroll)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfForcedScroll);

	*pfForcedScroll = false;
	return S_OK;

	END_COM_METHOD(g_fact4, IID_IVwRootSite);
}


/*----------------------------------------------------------------------------------------------
	Cause the display of the root to update.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::DoUpdates(IVwRootBox * prootb)
{
	BEGIN_COM_METHOD

	return S_OK;

	END_COM_METHOD(g_fact4, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	When the selection is changed, it propagates this to its site.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::SelectionChanged(IVwRootBox * prootb, IVwSelection * pvwselNew)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwselNew);

	return S_OK;

	END_COM_METHOD(g_fact4, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	When the state of the overlays changes, it propagates this to its site.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::OverlayChanged(IVwRootBox * prootb, IVwOverlay * pvo)
{
	BEGIN_COM_METHOD

	return S_OK;

	END_COM_METHOD(g_fact4, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Return true if this kind of window uses semi-tagging.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfPrintRootSite::get_SemiTagging(IVwRootBox * prootb, ComBool * pf)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pf);

	*pf = FALSE;
	return S_OK;

	END_COM_METHOD(g_fact4, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	Construct and save info about selection at top left of pavrs.
----------------------------------------------------------------------------------------------*/
SelPositionInfo::SelPositionInfo(AfVwRootSite * pavrs)
{
	m_pavrs = pavrs;
	if (!pavrs)
		return; // no other pane to save position for.
	HoldGraphicsRaw hg(pavrs);
	Rect rdSec;
	ComBool fSplit, fEndBeforeAnchor;
	int xdLeft, ydTop, xdRight, ydBottom;
	hg.m_qvg->GetClipRect(&xdLeft, &ydTop, &xdRight, &ydBottom);
	// Deliberately don't CheckHr. This can fail for various reasons...for example, if
	// called after making some changes and before redrawing, there may be a lazy box
	// at the top left of the window.
	IVwRootBoxPtr qrootb;
	pavrs->get_RootBox(&qrootb);
	qrootb->MakeSelAt(xdLeft + 1,ydTop + 1,hg.m_rcSrcRoot, hg.m_rcDstRoot, false,
		&m_qsel);
	if (m_qsel)
	{
		CheckHr(m_qsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot,
			&m_rdPrimaryOld, &rdSec, &fSplit, &fEndBeforeAnchor));
	}
}

/*----------------------------------------------------------------------------------------------
	Scroll pavrs so the selection we made in the constructor is at the same position it was
	when we were constructed, if possible.
----------------------------------------------------------------------------------------------*/
void SelPositionInfo::Restore()
{
	if (m_qsel)
	{
		// We were able to get a selection...before we use it, make sure it wasn't
		// spoiled by the changes we made.
		ComBool fValid;
		CheckHr(m_qsel->get_IsValid(&fValid));
		if (fValid)
		{
			HoldGraphicsRaw hg(m_pavrs);
			Rect rdPrimary, rdSec;
			ComBool fSplit, fEndBeforeAnchor;
			CheckHr(m_qsel->Location(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot,
				&rdPrimary, &rdSec, &fSplit, &fEndBeforeAnchor));
			if (rdPrimary.top != m_rdPrimaryOld.top)
				m_pavrs->ScrollDown(rdPrimary.top - m_rdPrimaryOld.top);
		}
	}
}

//:>********************************************************************************************
//:>	AfVwRecSplitChild methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfVwRecSplitChild::AfVwRecSplitChild(bool fScrollHoriz) : AfVwSplitChild(fScrollHoriz)
{
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfVwRecSplitChild::~AfVwRecSplitChild()
{
}


/*----------------------------------------------------------------------------------------------
	Refresh the window.
----------------------------------------------------------------------------------------------*/
bool AfVwRecSplitChild::FullRefresh(void)
{
	if (m_qrootb)
	{
		// We need to clear lazy load stuff and encodings.
		if (!m_qvcvc->FullRefresh())
			return false;
		// We need to restore the notifier for this window.
		ISilDataAccessPtr qsda;
		CheckHr(m_qrootb->get_DataAccess(&qsda));
		CheckHr(m_qrootb->putref_DataAccess(qsda));
		m_qrootb->Reconstruct();
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Clear lazyload stuff.
----------------------------------------------------------------------------------------------*/
bool AfVwRecSplitChild::PreSynchronize(SyncInfo & sync)
{
	if (m_qrootb)
		return m_qvcvc->FullRefresh();
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return a flag indicating whether the anchor and the endpoint of the selection are in
	the same field in the same entry.
----------------------------------------------------------------------------------------------*/
bool AfVwRecSplitChild::SelectionInOneField()
{
	IVwSelectionPtr qsel;
	CheckHr(m_qrootb->get_Selection(&qsel));
	if (!qsel)
		return false;

	int clevAnchor, clevEnd;
	CheckHr(qsel->CLevels(false, &clevAnchor));
	CheckHr(qsel->CLevels(true, &clevEnd));

	// REVIEW KenZ(RandyR): Why does this check the same thing twice?
	if (clevAnchor < 2 || clevAnchor < 2)
		return false;

	HVO hvoAnchor, hvoEnd;
	PropTag tagAnchor, tagEnd;
	int ihvo, cpropPrev;
	IVwPropertyStorePtr qvps;

	CheckHr(qsel->PropInfo(false, clevAnchor - 2, &hvoAnchor, &tagAnchor,
		&ihvo, &cpropPrev, &qvps));
	CheckHr(qsel->PropInfo(true, clevEnd - 2, &hvoEnd, &tagEnd,
		&ihvo, &cpropPrev, &qvps));
	if (hvoAnchor != hvoEnd)
		return false;

	CheckHr(qsel->PropInfo(false, 0, &hvoAnchor, &tagAnchor,
		&ihvo, &cpropPrev, &qvps));
	CheckHr(qsel->PropInfo(true, 0, &hvoEnd, &tagEnd,
		&ihvo, &cpropPrev, &qvps));
	if (hvoAnchor == hvoEnd && tagAnchor == tagEnd)
	{
		// Same paragraph.
		return true;
	}
	else if (tagAnchor == kflidStTxtPara_Contents && tagEnd == kflidStTxtPara_Contents)
	{
		Assert(clevAnchor > 1);
		Assert(clevEnd > 1);
		CheckHr(qsel->PropInfo(false, 1, &hvoAnchor, &tagAnchor,
			&ihvo, &cpropPrev, &qvps));
		CheckHr(qsel->PropInfo(true, 1, &hvoEnd, &tagEnd,
			&ihvo, &cpropPrev, &qvps));
		if (hvoAnchor == hvoEnd && tagAnchor == tagEnd)
			// Two different paragraphs in the same StText.
			return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Set clsid to the class of the entry or subentry in which the cursor is located and
	sets nLevel to 0 since all levels are now treated identically. Both are set to zero
	if we can't determine a valid entry.

	@param pclsid Class ID
	@param pnLevel 0 = main item in window.
----------------------------------------------------------------------------------------------*/
void AfVwRecSplitChild::GetCurClsLevel(int * pclsid, int * pnLevel)
{
	AssertPtr(pclsid);
	AssertPtr(pnLevel);

	*pclsid = 0;
	*pnLevel = 0;
	VwSelLevInfo * prgvsli = NULL;

	try
	{
		// Get the selection information.
		IVwSelectionPtr qvwsel;
		CheckHr(m_qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
			return; // No selection.
		int csli;
		CheckHr(qvwsel->CLevels(false, &csli));
		--csli; // CLevels includes the string prop itself, but AllTextSelInfo doesn't use it.
		if (csli <= 0)
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
		int isli = 0;
		int flid;
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
		AssertPtr(prmw);
		// We may fail here if the user has clicked on the icon at the left (browse view).
		// That's fine since the code below will still work.
		if (SUCCEEDED(qvwsel->AllTextSelInfo(&ihvoRoot, csli, prgvsli, &tagTextProp,
			&cpropPrevious, &ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL)))
		{
			// Find the level closest to the cursor that holds a record or subrecord.
			for ( ; isli < csli; ++isli)
			{
				// The flid at this level is deceptive. If we have an active filter, the flid
				// may show kflidStartDummyFlids even though it is a subentry.
				TagFlids tf;
				prmw->GetTagFlids(tf);
				flid = prgvsli[isli].tag;
				if (flid == tf.flidSubitems ||
					flid == kflidStartDummyFlids)
				{
					break;
				}
			}
		}
		delete[] prgvsli;
		if (isli == csli)
			return; // Something was odd.

		// Get the information about this record.
		HVO hvo;
		int ihvo;
		int cpropPrev;
		IVwPropertyStorePtr qvps;
		// PropInfo indexes based on 0, but our isli index is one less than this due to the
		// decrement above. isli + 1 will return the flid we got above, and isli returns
		// the object owned in that flid. We want the object.
		CheckHr(qvwsel->PropInfo(false, isli, &hvo, &flid, &ihvo, &cpropPrev, &qvps));
		AfLpInfo * plpi = prmw->GetLpInfo();
		AssertPtr(plpi);
		// Get the clsid and owner from the cache to determine needed information.
		CustViewDaPtr qcvd;
		plpi->GetDataAccess(&qcvd);
		AssertPtr(qcvd);
		CheckHr(qcvd->get_IntProp(hvo, kflidCmObject_Class, pclsid));
		*pnLevel = 0;
	}
	catch (...)
	{
		if (prgvsli)
			delete[] prgvsli;
	}
}

/*----------------------------------------------------------------------------------------------
	Enable/Disable Edit / Delete menu Command.
----------------------------------------------------------------------------------------------*/
bool AfVwRecSplitChild::CmsEditDel1(CmdState &cms)
{
	if (!SelectionInOneField())
	{
		cms.Enable(false);
		return true; // we handled it
	}
	return SuperClass::CmsEditDel1(cms);
}

/*----------------------------------------------------------------------------------------------
	Enable/Disable Edit / Cut menu Command.
----------------------------------------------------------------------------------------------*/
bool AfVwRecSplitChild::CanCut()
{
	if (!SelectionInOneField())
		return false;
	return SuperClass::CanCut();
}

/*----------------------------------------------------------------------------------------------
	Enable/Disable Edit / Paste menu Command.
----------------------------------------------------------------------------------------------*/
bool AfVwRecSplitChild::CanPaste()
{
	if (!SelectionInOneField())
		return false;

	return SuperClass::CanPaste();
}

/*----------------------------------------------------------------------------------------------
	When creating selections, if the selection is not contained within a
	single text field, expand the selection to include the entire entry or entries.
----------------------------------------------------------------------------------------------*/
void AfVwRecSplitChild::CallMouseUp(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot)
{
	SuperClass::CallMouseUp(xp, yp, rcSrcRoot, rcDstRoot);
	AfClientRecVwWnd * pcrvw = dynamic_cast<AfClientRecVwWnd*>(GetSplitterClientWnd());
	pcrvw->SelectWholeObjects(m_qrootb);
}


/*----------------------------------------------------------------------------------------------
	Only allow typing if the selection is appropriate, ie, doesn't span fields.
----------------------------------------------------------------------------------------------*/
#ifdef BASELINE
void AfVwRecSplitChild::CallOnTyping(VwGraphicsPtr qvg, BSTR bstr, int cchBackspace,
	int cchDelForward, OLECHAR chFirst, RECT rcSrcRoot, RECT rcDstRoot)
#else
void AfVwRecSplitChild::CallOnTyping(IVwGraphicsWin32Ptr qvg, BSTR bstr, int cchBackspace,
	int cchDelForward, OLECHAR chFirst, RECT rcSrcRoot, RECT rcDstRoot)
#endif
{
	if (!SelectionInOneField())
		return;

	SuperClass::CallOnTyping(qvg, bstr, cchBackspace, cchDelForward, chFirst,
		rcSrcRoot, rcDstRoot);
}

// Semi-Explicit instantiation.
#include "Vector_i.cpp"
