/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, SIL International. All rights reserved.

File: AfBrowseSplitChild.cpp
Responsibility: Randy Regnier
Last reviewed: never

Description:
	This file contains AfBrowseSplitChild.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

static DummyFactory g_factbsc(_T("SIL.FieldWorks.AfBrowseSplitChild"));

//:>********************************************************************************************
//:>	Constructor/Destructor
//:>********************************************************************************************
AfBrowseSplitChild::AfBrowseSplitChild(bool fScrollHoriz) : AfVwRecSplitChild(fScrollHoriz)
{
	m_hwndHeader = NULL;
	m_fColumnsModified = false;
}

AfBrowseSplitChild::~AfBrowseSplitChild()
{
}

//:>********************************************************************************************
//:>	AfBrowseSplitChild Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Prepare to hide the window.
----------------------------------------------------------------------------------------------*/
void AfBrowseSplitChild::PrepareToHide()
{
	AfApp::Papp()->RemoveCmdHandler(this, 1);
}


/*----------------------------------------------------------------------------------------------
	Prepare to show the window.
----------------------------------------------------------------------------------------------*/
void AfBrowseSplitChild::PrepareToShow()
{
	AfApp::Papp()->AddCmdHandler(this,1);
}


/*----------------------------------------------------------------------------------------------
	Initialize the browse view window.
----------------------------------------------------------------------------------------------*/
void AfBrowseSplitChild::PostAttach(void)
{
	// 10000 seems to be the limit on W9x machines. The header control didn't show up when
	// I (DarrellZ) used values >= SHRT_MAX.
	Rect rc(0, 0, 10000, 1000);

	if (GetPaneIndex() == 0)
	{
		// Create the header control (but only on the top pane).
		INITCOMMONCONTROLSEX iccex = { sizeof(iccex), ICC_LISTVIEW_CLASSES };
		::InitCommonControlsEx(&iccex);
		m_hwndHeader = ::CreateWindowEx(0, WC_HEADER, NULL, WS_CHILD | /*HDS_BUTTONS | */
			HDS_FULLDRAG | HDS_HORZ, 0, 0, 0, 0, m_hwnd, 0, 0, 0);
		::SendMessage(m_hwndHeader, WM_SETFONT, (WPARAM)::GetStockObject(DEFAULT_GUI_FONT),
			true);

		HDLAYOUT hdl;
		WINDOWPOS wp;
		hdl.prc = &rc;
		hdl.pwpos = &wp;
		::SendMessage(m_hwndHeader, HDM_LAYOUT, 0, (LPARAM)&hdl);

		// Set the size, position, and visibility of the header control.
		::SetWindowPos(m_hwndHeader, wp.hwndInsertAfter, wp.x, wp.y, wp.cx, wp.cy,
			wp.flags | SWP_SHOWWINDOW);
		// Store the height of this header control for use in superclasses.
		m_dyHeader = wp.cy;
	}
	SuperClass::PostAttach();
}


/*----------------------------------------------------------------------------------------------
	Clear out the window contents, and especially any active database connections, when the
	project is (about to be) changed.
----------------------------------------------------------------------------------------------*/
bool AfBrowseSplitChild::CloseProj()
{
	// Conditionally update the modification date on the last record we were in.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	if (prmw)
		prmw->UpdateRecordDate();

	// We can't get rid of the root box here since OnKillFocus gets called after this and uses
	// the root box. The rootbox is cleared in OnReleasePtr.

	if (m_fColumnsModified)
	{
		AssertPtr(prmw);

		// [Note: This used to be in OnReleasePointer. When I (RandyR) found it, it was broken,
		// because of a null pointer access. It seems that the db info was gone by the
		// time the call to OnReleasePtr comes through,
		// even though the lp info was still around.
		m_fColumnsModified = false;
		ISilDataAccessPtr qda;
		qda.CreateInstance(CLSID_VwOleDbDa);
		AssertPtr(qda);
		IOleDbEncapPtr qode;
		IFwMetaDataCachePtr qmdc;
		ILgWritingSystemFactoryPtr qwsf;
		AfLpInfo * plpi = prmw->GetLpInfo();
		AfDbInfo * pdbi = plpi->GetDbInfo();
		pdbi->GetDbAccess(&qode);
		pdbi->GetFwMetaDataCache(&qmdc);
		pdbi->GetLgWritingSystemFactory(&qwsf);
		ISetupVwOleDbDaPtr qsda;
		CheckHr(qda->QueryInterface(IID_ISetupVwOleDbDa, (void**)&qsda));
		qsda->Init(qode, qmdc, qwsf, NULL);

		int ccol = m_qrsp->m_vqbsp.Size();
		for (int icol = 0; icol < ccol; icol++)
			m_qrsp->m_vqbsp[icol]->SaveDetails(qda);
	}

	// Let it know it needs layout.
	m_dxdLayoutWidth = -50000;
	// ENHANCE JohnT: check whether anything needs saving, if we save on closing windows. (Is this the right place?)
	Invalidate();
	return true;
}

/*----------------------------------------------------------------------------------------------
	Make the root box.

	@param pvg (Not used here)
	@param pprootb Out The RootBox to be returned.
----------------------------------------------------------------------------------------------*/
void AfBrowseSplitChild::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
	IVwRootBox ** pprootb, bool fPrintingSel)
{
	AssertPtrN(pwsf);
	*pprootb = NULL;

	// Make sure we are attached. If not, don't make the root box. PostAttach tries again.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	if (!prmw)
		return;
	AfLpInfo * plpi = prmw->GetLpInfo();
	AssertPtr(plpi);

	// Check the database connection.
	IOleDbEncapPtr qode;
	plpi->GetDbInfo()->GetDbAccess(&qode);
	if (!qode)
		// Review JohnT: should we return E_FAIL or S_FALSE?
		return;	// No current session, can't make root box.

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this));
	// Set hvo to the ID of the dummy vector that stores the root objects that have been
	// filtered and sorted.
	HVO hvo = (fPrintingSel) ? prmw->GetPrintSelId() : prmw->GetFilterId();
	int frag = kfrcdRoot;
	AfSplitterClientWnd * pafscw = GetSplitterClientWnd();
	int wid = pafscw->GetWindowId();
	AfMdiClientWnd * pmdic = dynamic_cast<AfMdiClientWnd *>(pafscw->Parent());
	AssertPtr(pmdic);
	int iview;
	iview = pmdic->GetChildIndexFromWid(wid);
	UserViewSpecVec & vuvs = plpi->GetDbInfo()->GetUserViewSpecs();
	Assert(vuvs[iview]->m_vwt == kvwtBrowse);
	ClsLevel clevKey(prmw->GetBrowseDummyClass(), 0); // We use the dummy display RecordSpec
	vuvs[iview]->m_hmclevrsp.Retrieve(clevKey, m_qrsp);

	StrApp str;
	HDITEM hdi = { HDI_TEXT | HDI_WIDTH };
	int ccol = m_qrsp->m_vqbsp.Size();
	int ccol2 = Header_GetItemCount(m_hwndHeader);
	Assert(ccol2 == 0 || ccol2 == ccol + 1); // For the icons column

	if (!ccol2)
	{
		// Don't do this more than once...for example, not when making an extra root box
		// for printing.

		// First insert a dummy item for the icons.
		hdi.pszText = _T("");
		hdi.cxy = 21;
		::SendMessage(m_hwndHeader, HDM_INSERTITEM, 0, (LPARAM)&hdi);

		for (int icol = 1; icol < ccol + 1; icol++)
		{
			const OLECHAR * prgwch;
			int cch;
			ITsStringPtr qtss = m_qrsp->m_vqbsp[icol - 1]->m_qtssLabel;
			CheckHr(qtss->LockText(&prgwch, &cch));
			str.Assign(prgwch, cch);
			qtss->UnlockText(prgwch);

			hdi.pszText = const_cast<achar *>(str.Chars());
			hdi.cxy = m_qrsp->m_vqbsp[icol - 1]->m_dxpColumn;
			::SendMessage(m_hwndHeader, HDM_INSERTITEM, icol, (LPARAM)&hdi);
		}
	}

	// Set up a new view constructor with the given record specs.
	Rect rc;
	::GetWindowRect(m_hwndHeader, &rc);
	m_qvcvc.Attach(prmw->CreateCustBrowseVc(vuvs[iview], rc.Height(), vuvs[iview]->m_nMaxLines, prmw->GetRootObj()));

	// Get (or make) the custom view database access cache.
	CustViewDaPtr qcvd = prmw->MainDa();
	AssertPtr(qcvd);
	// In case we reconstruct this root box sometime, the way we know hvo is valid is that SOME
	// classid is cached for it. We don't otherwise use this information, so the value is arbitrary.
	CheckHr(qcvd->CacheIntProp(hvo, kflidCmObject_Class, 1988));

	AfStatusBarPtr qstbr = prmw->GetStatusBarWnd();
	Assert(qstbr);
	qstbr->StepProgressBar();

	// Tell the view constructor where to load them.
	IVwViewConstructor * pvvc = m_qvcvc;
	m_qvcvc->SetDa(qcvd, qstbr, vuvs[iview]);

	if (pwsf)
		CheckHr(qcvd->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qcvd));
	CheckHr(qrootb->SetRootObjects(&hvo, &pvvc, &frag, plpi->GetAfStylesheet(), 1));
	*pprootb = qrootb;
	(*pprootb)->AddRef();

	// This is a kludgy way of making sure that we don't register the printing root box.
	if (!ccol2)
		prmw->RegisterRootBox(qrootb);

	qstbr->StepProgressBar();
}

/*----------------------------------------------------------------------------------------------
	Return the index of this window.

	@return  index of this window (0=Pane(0), 1=Pane(1))
----------------------------------------------------------------------------------------------*/
int AfBrowseSplitChild::GetPaneIndex()
{
	AfSplitterClientWnd * pwnd = GetSplitterClientWnd();
	Assert(this == pwnd->GetPane(0) || this == pwnd->GetPane(1));
	if (this == pwnd->GetPane(1))
		return 1;
	return 0;
}


/*----------------------------------------------------------------------------------------------
	After resizing the window, make the selection visible. Here, we use ScrollSelectionIntoView
	in order to scroll horizontally if necessary.
----------------------------------------------------------------------------------------------*/
void AfBrowseSplitChild::MakeSelVisAfterResize(bool fSelVis)
{
	if (fSelVis)
		ScrollSelectionIntoView(NULL, kssoDefault);
}

/*----------------------------------------------------------------------------------------------
	For the bottom pane of the browse view, adjust the minimum scroll position
	to avoid showing the line of white space that is needed to accomodate the column headers
	(see RnCustBrowseVc::Display).
----------------------------------------------------------------------------------------------*/
int AfBrowseSplitChild::SetRootSiteScrollInfo(int nBar, SCROLLINFO * psi, bool fRedraw)
{
	if (nBar == SB_VERT && GetPaneIndex() == 1)
	{
		AfBrowseSplitChildPtr qbsc = dynamic_cast<AfBrowseSplitChild *>(OtherPane());
		Assert(qbsc);
		Rect rcHeader;
		::GetWindowRect(qbsc->m_hwndHeader, &rcHeader);

		int dy = psi->nMax - psi->nMin;
		psi->nMin = max(psi->nMin, rcHeader.Height());
		// I don't understand why we have to adjust the max too, but Ken thinks it makes
		// sense, and otherwise we can't get all the way to the bottom. -Sharon
		psi->nMax = psi->nMin + dy;
	}

	return SuperClass::SetRootSiteScrollInfo(nBar, psi, fRedraw);
}

/*----------------------------------------------------------------------------------------------
	Overridden to track the current record in order to update the status bar and keep things
	synchronized if the user changes view.

	@param pvwselNew Represents the new selection of something within the view.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfBrowseSplitChild::SelectionChanged(IVwRootBox * prootb, IVwSelection * pvwselNew)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvwselNew);

	if (!pvwselNew)
		return S_OK;
	int cvsli;
	CheckHr(pvwselNew->CLevels(false, &cvsli));
	// CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
	cvsli--;
	if (cvsli == -1)
	{
		// NOTE: This means that clicking on the "date created" field in the browse view
		// does not change the setting of the "current record", which can lead to seemingly
		// unpredictable results in cursor placement by TAB or Shift-TAB.
		// HOW CAN WE DETERMINE THE PROPER VALUE OF ihvoCurr???
		return S_OK;
	}
	if (cvsli <= 0)
	{
		// Some strange selection, perhaps a literal string, can't handle as yet.
		return S_OK;
	}
#ifdef JohnT10_15_02_FixPictureSelectProblem
	VwSelLevInfo * prgvsli;
	prgvsli = NewObj VwSelLevInfo[cvsli];
	int ihvoRoot;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;
	CheckHr(pvwselNew->AllTextSelInfo(&ihvoRoot, cvsli - 1, prgvsli, &tagTextProp,
		&cpropPrevious, &ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
	int ihvoCurr = prgvsli[cvsli].ihvo;
	delete[] prgvsli;
#else
	// We don't want to use AllTextSelInfo because it isn't adequately implemented for
	// picture selection, which can occur if the user clicks on the left hand column icon.
	// Values to receive results from PropInfo; only ihvoCurr is interesting.
	int ihvoCurr;
	HVO hvoRoot;
	PropTag tag;
	int cpropPrevious;
	IVwPropertyStorePtr qvps;
	// Note: it is cvsli rather than cvsli-1 because we decremented above.
	CheckHr(pvwselNew->PropInfo(false, cvsli, &hvoRoot, &tag,
		&ihvoCurr, &cpropPrevious, &qvps));
#endif

	// Store record and cursor information.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	prmw->SetCurRecIndex(ihvoCurr);
	TagFlids tf;
	prmw->GetTagFlids(tf);
	Vector<HVO> & vhvo = prmw->GetHvoPath();
	Vector<int> & vflid = prmw->GetFlidPath();
	AfMdiClientWnd * pmcw = prmw->GetMdiClientWnd();
	AssertPtr(pmcw);
	AfSplitterClientWnd * pscw = dynamic_cast<AfSplitterClientWnd *>(pmcw->GetCurChild());
	AssertPtr(pscw);
	int ich;
	ich = pscw->CurrentPane()->GetLocation(vhvo, vflid);
	prmw->SetCursorIndex(ich);

	// If we moved into a new record, process possible modification date change.
	// First, find the last record/subrecord modified
	HVO hvoCurRec;
	HVO hvoLastRec = pscw->CurrentPane()->GetLastObj();
	// When this was testing for equality, we were getting an assert when
	// we promote an entry to a major entry when a browse window had been open
	// in the same window prior to the promote. vhvo had 1 item and flid had 0.
	Assert(vhvo.Size() >= vflid.Size());
	for (int iflid = 0; iflid < vflid.Size(); ++iflid)
	{
		if (vflid[iflid] == tf.flidSubitems)
			continue;
		hvoCurRec = vhvo[iflid];
		break;
	}
	if (hvoCurRec != hvoLastRec)
	{
		// We've changed records, so update modification date if needed.
		pscw->CurrentPane()->SetLastObj(hvoCurRec);
		if (hvoLastRec)
			prmw->UpdateDateModified(hvoLastRec, tf.flidModified);
	}
	prmw->UpdateCaptionBar();
	prmw->UpdateStatusBar();
#if 1
	// Selection may have changed by now (after calling SetCurRecIndex above).
	// If still valid, perform any needed keyboard switching.
	IVwSelectionPtr qsel;
	CheckHr(m_qrootb->get_Selection(&qsel));
	if (pvwselNew == qsel.Ptr())
		HandleSelectionChange(pvwselNew);
#endif

	return S_OK;

	END_COM_METHOD(g_factbsc, IID_IVwRootSite);
}

/*----------------------------------------------------------------------------------------------
	This should keep the header control from scrolling vertically out of sight when the view
	window needs to be scrolled.

	@param dx Change in horizontal position.
	@param dy Change in vertical position.
	@param rc Reference to a coordinate rectangle that gets updated for the change in position.
----------------------------------------------------------------------------------------------*/
void AfBrowseSplitChild::GetScrollRect(int dx, int dy, Rect & rc)
{
	AfWnd::GetClientRect(rc);
	if (dy)
	{
		// This will get a little more complicated if we are scrolling in both directions.
		// Basically if we are scrolling horizontally, we want the header control to scroll
		// as well. If we are scrolling vertically, we don't want it to scroll.
		Assert(dx == 0);
		Rect rcHeader;
		if (m_hwndHeader)
		{
			// Only do this for the pane that has the header!
			::GetWindowRect(m_hwndHeader, &rcHeader);
			rc.top += rcHeader.Height();
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Resize the column widths of the embedded table.

	@param pnmh Windows command that is being passed.
----------------------------------------------------------------------------------------------*/
void AfBrowseSplitChild::OnHeaderTrack(NMHEADER * pnmh)
{
	AssertPtr(pnmh);
	AssertPtr(pnmh->pitem);
	AssertPtr(m_qrootb);
	AssertPtr(m_qrsp);

	m_fColumnsModified = true;

	HDC hdc = ::GetDC(m_hwnd);
	int ypLogPixels = ::GetDeviceCaps(hdc, LOGPIXELSY);
	::ReleaseDC(m_hwnd, hdc);

	Rect rc;
	int ccol = Header_GetItemCount(m_hwndHeader);
	// It is one more that the bsp size because of the left fixed column
	Assert(ccol == m_qrsp->m_vqbsp.Size() + 1);
	Vector<VwLength> vvlen;
	vvlen.Resize(ccol);
	for (int icol = 0; icol < ccol; icol++)
	{
		Header_GetItemRect(m_hwndHeader, icol, &rc);
		vvlen[icol].unit = kunPoint1000;
		vvlen[icol].nVal = rc.Width() * 72000 / ypLogPixels;
		// The first column is the left fixed column
		if (icol >= 1)
			m_qrsp->m_vqbsp[icol - 1]->m_dxpColumn = rc.Width();
	}

	CheckHr(m_qrootb->SetTableColWidths(vvlen.Begin(), vvlen.Size()));
	::UpdateWindow(m_hwnd);

	AfBrowseSplitChild * pbscOther =
			dynamic_cast<AfBrowseSplitChild*>(GetSplitterClientWnd()->GetPane(1 - GetPaneIndex()));
	if (pbscOther)
		::InvalidateRect(pbscOther->Hwnd(), NULL, true);
}


/*----------------------------------------------------------------------------------------------
	Handle notification messages.

	@param ctid Id of the control that issued the windows command.
	@param pnmh Windows command that is being passed.
	@param lnRet return value to be returned to the windows command.
	@return true if command is handled.
	See ${AfWnd#OnNotifyChild}
----------------------------------------------------------------------------------------------*/
bool AfBrowseSplitChild::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	static bool s_fTracking = false;
	static bool s_fAllowTrack = false;

	const int kdxyMinHeader = 6;

	switch (pnmh->code)
	{
	case HDN_BEGINTRACK:
		s_fTracking = true;
		s_fAllowTrack = true;
		return true;
	case HDN_ENDTRACK:
		s_fTracking = false;
		return true;

	// ENHANCE DarrellZ: For some stupid reason, this doesn't ever seem to get called.
	// We should be trapping HDN_TRACK instead of HDN_ITEMCHANGING, so once Microsoft fixes
	// their bug, make the change.
	/*case HDN_TRACK:
		OnHeaderTrack((NMHEADER *)pnmh);
		return true;*/

	case HDN_ITEMCHANGING:
		if (s_fAllowTrack)
		{
			NMHEADER * pnhdr = (NMHEADER *)pnmh;
			// JohnT: not sure why the first condition, but the second prevents resizing
			// the icon column. Allowing that causes problems because we have nowhere to
			// to communicate the new width to rows later created lazily, so they wind up
			// inconsistent.
			if (pnhdr->pitem->cxy < kdxyMinHeader || pnhdr->iItem == 0)
			{
				lnRet = true;
				s_fAllowTrack = s_fTracking;
			}
		}
		return true;
	case HDN_ITEMCHANGED:
		if (s_fAllowTrack)
			OnHeaderTrack((NMHEADER *)pnmh);
		s_fAllowTrack = s_fTracking;
		return true;
	}

	return SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Handle window messages.

	See ${AfWnd#FWndProcPre}
	@param wm windows message
	@param wp WPARAM
	@param lp LPARAM
	@param lnRet Value to be returned to the windows.
	@return true
----------------------------------------------------------------------------------------------*/
bool AfBrowseSplitChild::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AfClientRecWnd * pafcrw = dynamic_cast<AfClientRecWnd*>(GetSplitterClientWnd());
	AfBrowseSplitChild * pbscZero = dynamic_cast<AfBrowseSplitChild *>(pafcrw->GetPane(0));
	if (wm == WM_DESTROY && this == pbscZero)
	{
		// If we are closing the top pane and we have two panes open, make a new copy of the
		// current header control in the bottom pane so that it doesn't disappear.
		AfBrowseSplitChild * pbscOne = dynamic_cast<AfBrowseSplitChild*>(pafcrw->GetPane(1));
		AssertPtrN(pbscOne);
		if (pbscOne)
		{
			HWND hwndHeader = ::CreateWindowEx(0, WC_HEADER, NULL, WS_CHILD | /*HDS_BUTTONS |*/
				HDS_FULLDRAG | HDS_HORZ, 0, 0, 0, 0, pbscOne->Hwnd(), 0, 0, 0);
			::SendMessage(hwndHeader, WM_SETFONT, (WPARAM)::GetStockObject(DEFAULT_GUI_FONT),
				true);

			HDLAYOUT hdl;
			WINDOWPOS wp;
			// 10000 seems to be the limit on W9x machines. The header control didn't show up
			// when I (DarrellZ) used values >= SHRT_MAX.
			Rect rc(0, 0, 10000, 1000);
			hdl.prc = &rc;
			hdl.pwpos = &wp;
			::SendMessage(hwndHeader, HDM_LAYOUT, 0, (LPARAM)&hdl);

			// Set the size, position, and visibility of the header control.
			::SetWindowPos(hwndHeader, wp.hwndInsertAfter, wp.x, wp.y, wp.cx, wp.cy,
				wp.flags | SWP_SHOWWINDOW);

			Assert(!pbscOne->m_hwndHeader);
			pbscOne->m_hwndHeader = hwndHeader;

			// Copy all the current items to the new header control.
			HDITEM hdi = { HDI_TEXT | HDI_WIDTH };
			achar rgchBuf[MAX_PATH];
			hdi.pszText = rgchBuf;
			hdi.cchTextMax = MAX_PATH;
			int citem = Header_GetItemCount(m_hwndHeader);
			for (int iitem = 0; iitem < citem; iitem++)
			{
				Header_GetItem(m_hwndHeader, iitem, &hdi);
				Header_InsertItem(hwndHeader, iitem, &hdi);
			}
			m_hwndHeader = NULL;
			pbscOne->m_qrsp = m_qrsp;
		}
	}
	else if (wm == WM_KEYDOWN && wp == VK_TAB)
	{
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
		AssertPtr(prmw);
		int ihvoCurr = prmw->CurRecIndex();
		int crec = prmw->Records().Size();
		VwShiftStatus ss = (::GetKeyState(VK_SHIFT) < 0) ? kfssShift : kfssNone;
		if (pbscZero->m_qrootb->OnExtendedKey(VK_TAB, ss, 0) == S_FALSE)
		{
			if (ss == kfssNone && ihvoCurr < crec - 1)
			{
				pafcrw->DispCurRec(NULL, 1);
			}
			else if (ss == kfssShift && ihvoCurr > 0)
			{
				pafcrw->DispCurRec(NULL, -1);
				// Now, move to last editable field of this record.
				pbscZero->m_qrootb->OnExtendedKey(VK_TAB, kfssControl, 0);
			}
		}
		return true;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Process a mouse click. If it is on a ref, jump/launch to that ref. Otherwise use the default
	action.
----------------------------------------------------------------------------------------------*/
void AfBrowseSplitChild::CallMouseDown(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot)
{
	m_wsPending = -1;

	// Get the tentative selection at the mouse location regardless of editability.
	IVwSelectionPtr qsel;
	// For some reason, this can fail to get a selection while changing filters (e.g.,
	// try switching Anthro Category sort from Name to Abbreviation and while it is updating,
	// try clicking around in various fields. There may be a better solution than this, but
	// if we don't get a selection, we might as well pass the click up higher.
	m_qrootb->MakeSelAt(xp, yp, rcSrcRoot, rcDstRoot, false, &qsel);
	if (!qsel)
	{
		SuperClass::CallMouseDown(xp, yp, rcSrcRoot, rcDstRoot);
		return;
	}

	// Get information on the selection.
	int cvsli;
	CheckHr(qsel->CLevels(false, &cvsli));
	if (cvsli < 4)
	{
		// This won't be a valid reference property, so skip further testing.
		SuperClass::CallMouseDown(xp, yp, rcSrcRoot, rcDstRoot);
		return;
	}
	/*
	cvsli--; // CLevels includes the string prop itself, but AllTextSelInfo does not need it.
	VwSelLevInfo * prgvsli;
	prgvsli = NewObj VwSelLevInfo[cvsli];
	// Get selection information to determine where the mouse is located.
	int ihvoObj;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;
	CheckHr(qsel->AllTextSelInfo(&ihvoObj, cvsli, prgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
	delete[] prgvsli;
	*/
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);
	AfLpInfoPtr qlpi = prmw->GetLpInfo();
	AssertPtr(qlpi);
	IFwMetaDataCachePtr qmdc;
	qlpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
	AssertPtr(qmdc);
	CustViewDaPtr qcvd = prmw->MainDa();
	AssertPtr(qcvd);

	// PropInfo index 1 will give us the object we clicked on, assuming index 2 flid is
	// an object reference. Example:
	// isli = 0, hvo = 2348, flid = -2, ihvo = -1
	// isli = 1, hvo = 2348, flid = -1, ihvo = 0
	// isli = 2, hvo = 2394, flid = 4005005, ihvo = 2
	// In this example, 2348 is a reference to a record in the 4005005 reference property.
	HVO hvo;
	int flid;
	int ihvo;
	int cpropPrev;
	IVwPropertyStorePtr qvps;
	CheckHr(qsel->PropInfo(false, 2, &hvo, &flid, &ihvo, &cpropPrev, &qvps));
	int nType;
	CheckHr(qmdc->GetFieldType(flid, &nType));

	if (((1 << nType) & kgrfcptReference) == 0)
	{
		// We aren't dealing with a reference, so exit.
		SuperClass::CallMouseDown(xp, yp, rcSrcRoot, rcDstRoot);
		return;
	}
	// We have a reference property. Get the referenced object and see if it is an event
	// or an analysis.
	CheckHr(qsel->PropInfo(false, 1, &hvo, &flid, &ihvo, &cpropPrev, &qvps));
	int clid;
	CheckHr(qcvd->get_ObjClid(hvo, &clid));
	if (!prmw->IsJumpableClid(clid))
	{
		// We aren't dealing with an object we want to jump to, so exit.
		SuperClass::CallMouseDown(xp, yp, rcSrcRoot, rcDstRoot);
		return;
	}

	// We have a record to jump to.
	prmw->JumpTo(hvo);
}
