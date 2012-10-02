/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ClientWindows.cpp
Responsibility: Ken Zook
Last reviewed:

Description:
	This file contains class definitions for the following classes:
		AfClientRecWnd : AfSplitterClientWnd
			AfClientRecDeWnd : AfClientRecWnd
			AfClientRecVwWnd : AfClientRecWnd
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE
//:End Ignore

//:>********************************************************************************************
//:>	AfClientRecWnd window code.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Synchronize all panes in this client with any changes made in the database.
	@param sync -> The information describing a given change.
----------------------------------------------------------------------------------------------*/
bool AfClientRecWnd::Synchronize(SyncInfo & sync)
{
	// I (KenZ) tried getting this to work in conjunction with CleMainWnd::Synchronize,
	// but I couldn't get it to work. No matter what I try, it fails to work correctly.
	// Either not scrolling at all, not placing the cursor in the result, or not
	// positioning records correctly after using File...Properties to turn the sort flag on
	// and off in the People list. For example, if the cursor is in the Tiga entry when it is
	// unsorted, after switching to sorted, it is ending up in the Sembilan entry, and Tiga
	// remains in its original position even though the cache is properly updated.

	// Display record to make sure data is loaded and it is scrolled into view.
//	AfVwSplitChild * pavsc = dynamic_cast<AfVwSplitChild *>(CurrentPane());
//	pavsc->ScrollSelectionNearTop();
//	IVwRootBoxPtr qrootb;
//	pavsc->get_RootBox(&qrootb);
//	qrootb->DestroySelection();
//	ShowCurrentRec();
	return SuperClass::Synchronize(sync);
//	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
//	AssertPtr(prmw);
//	int ihvoCur = prmw->CurRecIndex();
//	prmw->SetCurRecIndex(-1);
//	DispCurRec(0, ihvoCur + 1);
//	return true;
}

/*----------------------------------------------------------------------------------------------
	Clear out the window contents, and especially any active database connections,
	when the project is (about to be) changed.
	NOTE JohnT: we don't need to invalidate the window, because we will shortly destroy it.
----------------------------------------------------------------------------------------------*/
bool AfClientRecWnd::CloseProj()
{
	if (!m_hwnd)
		return true;

	bool fT = GetPane(0)->CloseProj();
	if ((GetPane(1) && !GetPane(1)->CloseProj()) || !fT)
		return false;

	return true;
}


/*----------------------------------------------------------------------------------------------
	Prepare to hide the window.
----------------------------------------------------------------------------------------------*/
void AfClientRecWnd::PrepareToHide()
{
	GetPane(0)->PrepareToHide();
	if (GetPane(1))
		GetPane(1)->PrepareToHide();
}


/*----------------------------------------------------------------------------------------------
	Prepare to show the window.
----------------------------------------------------------------------------------------------*/
void AfClientRecWnd::PrepareToShow()
{
	GetPane(0)->PrepareToShow();
	if (GetPane(1))
		GetPane(1)->PrepareToShow();
}


/*----------------------------------------------------------------------------------------------
	Check if it is OK to close window.
----------------------------------------------------------------------------------------------*/
bool AfClientRecWnd::IsOkToChange(bool fChkReq)
{
	if (!GetPane(0)->IsOkToChange(fChkReq))
		return false;
	if (GetPane(1))
		// No need to check requirements second time.
		return GetPane(1)->IsOkToChange();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Create a xxxSplitChild window for the entire client area.

	@param psplcCopy If NULL we load settings from registry and then display the first record.
					If valid pointer then this new window's settings are copied from this one.
	@param psplcNew Out The new window to be created.
----------------------------------------------------------------------------------------------*/
void AfClientRecWnd::CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew)
{
	AssertPtr(psplcNew);
	AssertPtrN(psplcCopy);

	RecMainWnd * qrmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(qrmw);
	AfSplitChildPtr qsplc;
	qsplc.Attach(qrmw->CreateNewSplitChild());
	*psplcNew = qsplc;

	WndCreateStruct wcs;
	wcs.InitChild(_T("AfVwWnd"), SplitterHwnd(), 0);
	wcs.style |=  WS_VISIBLE;

	// Add information to the new child.
	if (psplcCopy)
	{
		AfVwRecSplitChild * pavrscFirst = dynamic_cast<AfVwRecSplitChild *>(psplcCopy);
		if (pavrscFirst)
		{
			// The cast worked, so it must not be a DE child.
			AfVwRecSplitChild * pavrsc = dynamic_cast<AfVwRecSplitChild*>(qsplc.Ptr());
			AssertPtr(pavrsc);
			// We have to call this before CreateHwnd or we'll get a bogus RootBox hanging
			// around that will crash later on whenever we cycle through all RootBoxes in the
			// main wnd, as when Overlays are opened.
			pavrscFirst->CopyRootTo(pavrsc);
			qsplc->CreateHwnd(wcs);
		}
		else
		{
			qsplc->CreateHwnd(wcs);
			AfDeRecSplitChild * parsc = dynamic_cast<AfDeRecSplitChild *>(qsplc.Ptr());
			AssertPtr(parsc);
			// Make sure the child is resized once it is created before setting the root object.
			Rect rc;
			GetClientRect(rc);
			OnSize(kwstRestored, rc.Width(), rc.Height());
			// We are making a child window of an existing window.
			parsc->SetRootObj(qrmw->Records()[qrmw->CurRecIndex()], false);
		}
	}
	else
	{
		// This is the first child window we are making. We need to load settings and
		// then display the first record.
		qsplc->CreateHwnd(wcs);
		LoadSettings(NULL);
	}
	AddRefObj(*psplcNew);
}


/*----------------------------------------------------------------------------------------------
	The user has selected another filter or sort method, which means that the root objects
	shown by the client window need to be refreshed. Call RnMainWnd::Records() to get the new
	list of root objects to show. The current record should be kept in view as long as it is
	listed in Records(). Otherwise, scroll to the top (or show the first record).
----------------------------------------------------------------------------------------------*/
void AfClientRecWnd::ReloadRootObjects()
{
	if (!m_hwnd)
		return;

	// See if we can arrange to show the same record.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);
	HvoClsidVec & vhcRecords = prmw->Records();
	int ivhc;
	for (ivhc = 0; ivhc < vhcRecords.Size(); ivhc++)
	{
		if (m_hvoCurrent == vhcRecords[ivhc].hvo)
			break;
	}
	if (ivhc >= vhcRecords.Size())
		ivhc = 0; // goto first record if not found
	// Force DispCurRec to do something
	prmw->SetCurRecIndex(-1);
	m_hvoCurrent = 0;

	// Call DispCurRec to scroll to the proper record.
	DispCurRec(0, ivhc + 1); // +1 because it is relative and we just made origin -1
}


//:>********************************************************************************************
//:>	AfClientRecDeWnd window code.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Display the indicated record. If bmk is DBBMK_FIRST or DDBMK_LAST, then the parameter drec
	(record offset) is ignored. If bmk is NULL, drec (which	can be negative) is	added to the
	index of the current record. Note that we actually don't use bookmarks in finding the
	record, since the bookmark system doesn't (unfortunately) appear to be related to
	GetNextRows. Also, when you change directions the system gives you the same record again,
	initially, as the last one in the previous direction. We thus take steps to get round this.
	The default does nothing.

	@param bmk Values are DBBMK_FIRST, DDBMK_LAST, or NULL
	@param drec Record offset

	@return  S_OK if sucessful or E_FAIL if not.
----------------------------------------------------------------------------------------------*/
HRESULT AfClientRecDeWnd::DispCurRec(BYTE bmk, int drec)
{
	// When called to reinitialize a view for a different project, we don't need to do anything
	// yet as PostAttach will do what we want when the user clicks on the DataEntry view button.
	if (!m_hwnd)
		return S_OK;

	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(prmw);
	AfDeRecSplitChild * parscOne = dynamic_cast<AfDeRecSplitChild*>(GetPane(1));

	HvoClsidVec & vhcRecords = prmw->Records();
	if (!vhcRecords.Size())
	{
		// No records to display.
		prmw->SetCurRecIndex(-1);	// Make sure it is legal.
		// If we are deleting the last record, we need to be sure to clear up the display.
		dynamic_cast<AfDeSplitChild*>(GetPane(0))->CloseAllEditors();
		if (parscOne)
			parscOne->CloseAllEditors();
		::InvalidateRect(m_hwnd, NULL, false);
		return S_OK;
	}

	int ihvoCurr = prmw->CurRecIndex();
	switch(bmk)
	{
	case DBBMK_FIRST:
		ihvoCurr = 0;
		break;
	case DBBMK_LAST:
		ihvoCurr = max(vhcRecords.Size() - 1, 0);
		break;
	case 0:
		ihvoCurr += drec;
		break;
	}
	Assert(ihvoCurr >= 0);
	HVO hvoCurrent = vhcRecords[ihvoCurr].hvo;
	if (hvoCurrent == m_hvoCurrent)
	{
		// We're on the same physical record.
		bool fRefresh = false;
		if (ihvoCurr != prmw->CurRecIndex())
		{
			// We've changed to a different logical record.  (Filtering and sorting can multiply
			// the number of logical records.)
			prmw->SetCurRecIndex(ihvoCurr);
			fRefresh = true;
		}
		// If we don't have an active editor open, open the first one.
		AfDeSplitChild * padsc = dynamic_cast<AfDeSplitChild *>(CurrentPane());
		if (!padsc->GetActiveFieldEditor())
			padsc->OpenPath(prmw->GetHvoPath(), prmw->GetFlidPath(), prmw->GetCursorIndex());
		if (fRefresh)
			prmw->UpdateStatusBar();

		return S_OK;
	}
	m_hvoCurrent = hvoCurrent; // ready for next time

	prmw->SetCurRecIndex(ihvoCurr);
	dynamic_cast<AfDeRecSplitChild*>(GetPane(0))->SetRootObj(vhcRecords[ihvoCurr], true);
	if (parscOne)
		parscOne->SetRootObj(vhcRecords[ihvoCurr], false);
	::InvalidateRect(m_hwnd, NULL, false);

	// Update the caption and status bars.
	prmw->UpdateCaptionBar();
	prmw->UpdateStatusBar();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Load settings specific to this window.

	@param pszRoot The string that is used for this app as the root for all registry entries.
	@param fRecursive If true, then every child window will be asked to save their settings.
----------------------------------------------------------------------------------------------*/
void AfClientRecDeWnd::LoadSettings(const achar * pszRoot, bool fRecursive)
{
	FwSettings * pfws = AfApp::GetSettings();
	AssertPtr(pfws);

	DWORD dwT;
	if (pfws->GetDword(pszRoot, _T("DeTreeWidth"), &dwT))
	{
		AfDeSplitChildPtr qadsc = dynamic_cast<AfDeSplitChild *>(GetPane(0));
		Assert(qadsc);

		qadsc->SetTreeWidth(dwT);
		qadsc = dynamic_cast<AfDeSplitChild *>(GetPane(1));
		if (qadsc)
			qadsc->SetTreeWidth(dwT);

		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
		Assert(prmw);
		prmw->SetTreeWidth(dwT);
	}
}


/*----------------------------------------------------------------------------------------------
	Save settings specific to this window.

	@param pszRoot The string that is used for this app as the root for all registry entries.
	@param fRecursive If true, then every child window will be asked to save their settings.
----------------------------------------------------------------------------------------------*/
void AfClientRecDeWnd::SaveSettings(const achar * pszRoot, bool fRecursive)
{
	FwSettings * pfws = AfApp::GetSettings();
	AssertPtr(pfws);

	AfDeSplitChildPtr qadsc = dynamic_cast<AfDeSplitChild *>(GetPane(0));
	Assert(qadsc);

	pfws->SetDword(pszRoot, _T("DeTreeWidth"), qadsc->GetTreeWidth());
}


/*----------------------------------------------------------------------------------------------
	If two panes are showing, change the tree width of the other pane.

	@param dxpTreeWidth The new tree width value.
	@param padsc Ptr to window that has the tree.
----------------------------------------------------------------------------------------------*/
void AfClientRecDeWnd::OnTreeWidthChanged(int dxpTreeWidth, AfDeSplitChild * padsc)
{
	AssertPtr(padsc);
	AfDeSplitChildPtr qadscZero = dynamic_cast<AfDeSplitChild*>(GetPane(0));
	Assert(qadscZero);

	AfDeSplitChildPtr qadscOne = dynamic_cast<AfDeSplitChild*>(GetPane(1));
	if (qadscOne)
	{
		Assert(padsc == qadscZero || padsc == qadscOne);
		AfDeSplitChildPtr qadscOther = dynamic_cast<AfDeSplitChild*>(GetPane(padsc == qadscZero));
		qadscOther->SetTreeWidth(dxpTreeWidth);
		qadscOther->SetHeight();
		::InvalidateRect(qadscOther->Hwnd(), NULL, true);
		::UpdateWindow(qadscOther->Hwnd());
	}

	RecMainWndPtr qrmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(qrmw);
	qrmw->SetTreeWidth(dxpTreeWidth);
}


/*----------------------------------------------------------------------------------------------
	Save away the split and scroll info for the window so it can be restored after returning
	from TlsOptDlg.  This saves the info then RestoreVwInfo restores it.

	@param pwndSet Out Structure that holds the window settings.
----------------------------------------------------------------------------------------------*/
void AfClientRecDeWnd::GetVwSpInfo(WndSettings * pwndSet)
{
	AfDeSplitChildPtr qadsc = dynamic_cast<AfDeSplitChild *>(GetPane(0));
	Assert(qadsc);

	pwndSet->viTreeW = qadsc->GetTreeWidth();
	SuperClass::GetVwSpInfo(pwndSet);
}


/*----------------------------------------------------------------------------------------------
	Reload and redraw window and child dialogs.
----------------------------------------------------------------------------------------------*/
bool AfClientRecDeWnd::FullRefresh()
{
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	if (!prmw)
		return true; // Window isn't there yet.

	if (!IsOkToChange())
		return false; // Don't do anything if there are problems.

	int ihvoCur = prmw->CurRecIndex();
	prmw->SetCurRecIndex(-1);
	dynamic_cast<AfDeSplitChild *>(GetPane(0))->CloseAllEditors(true);
	if (GetPane(1))
		dynamic_cast<AfDeSplitChild *>(GetPane(1))->CloseAllEditors(true);
	m_hvoCurrent = -1;
	DispCurRec(0, ihvoCur + 1);
	return true;
}


//:>********************************************************************************************
//:>	AfClientRecVwWnd window code.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	If two panes are showing, change the tree width of the other pane. If we need to move
	to a specific spot in a record, try to get there.
----------------------------------------------------------------------------------------------*/
void AfClientRecVwWnd::ShowCurrentRec()
{
	// Move the selection and scroll to show it
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AfVwSplitChild * pavsc = dynamic_cast<AfVwSplitChild *>(CurrentPane());
	AssertPtr(pavsc);
	CustViewDaPtr qcvd = prmw->MainDa();
	AssertPtr(qcvd);

	int ihvoCurr = prmw->CurRecIndex();
	VwSelLevInfo * prgvsli;

	Vector<int> & vflid = prmw->GetFlidPath();
	int csli = Max(1, vflid.Size());
	prgvsli = NewObj VwSelLevInfo[csli];
	IVwRootBoxPtr qrootb;
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
		pavsc->get_RootBox(&qrootb);
		if (qrootb)
		{
			// We need to eliminate the old selection to avoid problems
			// with ObjectIsSelected below.
			qrootb->DestroySelection();
		}
		Vector<HVO> & vhvo = prmw->GetHvoPath();
		int iflid = 0;
		// Build up a selection that attempts to get us to the desired spot in the record.
		// We need to build the SelLevInfo backwards with the root on the bottom.
		int isli = vflid.Size() - 1;
		prgvsli[isli].tag = kflidStartDummyFlids;
		prgvsli[isli].cpropPrevious = 0;
		prgvsli[isli].ihvo = ihvoCurr;
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
					AfMdiClientWnd * pmdic = dynamic_cast<AfMdiClientWnd *>(Parent());
					AssertPtr(pmdic);
					int iview;
					iview = pmdic->GetChildIndexFromWid(GetWindowId());
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
	else
	{
		// We simply want to display the current main record.
		prgvsli[0].tag = kflidStartDummyFlids;
		prgvsli[0].cpropPrevious = 0;
		prgvsli[0].ihvo = ihvoCurr;
	}

	// DO NOT CheckHr. This could legitimately fail e.g., if no editable fields.
	pavsc->get_RootBox(&qrootb);
	if (qrootb)
	{
		if (ObjectIsSelected(m_hvoCurrent))
		{
			// Don't move the selection if it is already in an appropriate place, for instance,
			// somewhere inside the entry in the document view.
			// This test was added by Sharon to avoid problems of needing to click twice in
			// the document view of the list editor.
		}
		else
		{
			IVwSelectionPtr qsel;
			if (vflid.Size())
			{
				qrootb->MakeTextSelection(
					0, // int ihvoRoot
					csli, // int cvlsi,
					prgvsli, // Skip the first one -- VwSelLevInfo * prgvsli
					vflid[vflid.Size() - 1], // int tagTextProp,
					0, // int cpropPrevious,
					prmw->GetCursorIndex(), // int ichAnchor,
					prmw->GetCursorIndex(), // int ichEnd,
					0, // int ws,
					true, // ComBool fAssocPrev,
					-1, // int ihvoEnd,
					NULL, // ITsTextProps * pttpIns,
					true, // ComBool fInstall,
					&qsel); // IVwSelection ** ppsel
			}
			// If we didn't get a text selection, try getting an editable selection somewhere close.
			VwSelLevInfo * pvsli = prgvsli;
			int csliTry;
			for (csliTry = csli; csliTry > 0 && !qsel; --csliTry, ++pvsli)
			{
				qrootb->MakeTextSelInObj(
					0,  // index of the one and only root object in this view
					csliTry, // See if we can find what we want to this many levels.
					pvsli, // And here's the level info.
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
			// If that fails, we'll settle for a non-editable selection.
			pvsli = prgvsli;
			for (csliTry = csli; csliTry > 0 && !qsel; --csliTry, ++pvsli)
			{
				qrootb->MakeTextSelInObj(
					0,  // index of the one and only root object in this view
					csliTry, // See if we can find what we want to this many levels.
					pvsli, // And here's the level info.
					0,
					NULL, // don't worry about the endpoint
					true, // select at the start of it
					false, // Find an editable field
					false, // and don't select a range.
					// Making this true, allows the whole record to scroll into view when we launch
					// a new window by clicking on a reference to an entry, but we don't get an insertion
					// point. Using false gives an insertion point, but the top of the record is typically
					// at the bottom of the screen, which isn't good.
					false, // don't select the whole object
					true, // but do install it as the current selection
					NULL); // and don't bother returning it to here. */
			}
			pavsc->ScrollSelectionIntoView(NULL, kssoNearTop);
		}
	}
	delete[] prgvsli;

	// Update the caption bar.
	prmw->UpdateCaptionBar();

	// Update the status bar.
	prmw->UpdateStatusBar();
}


/*----------------------------------------------------------------------------------------------
	Display the indicated item. If bmk is DBBMK_FIRST or DDBMK_LAST,
	the parameter drec (record offset) is ignored. If bmk is NULL, drec (which
	can be negative) is	added to the index of the current record. Note that we
	actually don't use bookmarks in finding the record, since the bookmark system
	doesn't	(unfortunately) appear to be related to GetNextRows.
	Also, when you change directions the system gives you the same record again, initially, as
	the last one in the previous direction. We thus take steps to get round this.
	The default does nothing.

	@param bmk Values are DBBMK_FIRST, DDBMK_LAST, or NULL
	@param drec Record offset
	@return  S_OK if sucessfull or E_FAIL if not.
----------------------------------------------------------------------------------------------*/
HRESULT AfClientRecVwWnd::DispCurRec(BYTE bmk, int drec)
{
	// When called to reinitialize a view for a different project, we may need to create a new
	// RootBox. If there isn't an m_hwnd, we don't need to do anything yet as PostAttach will
	// do what we want when the user clicks on the Document view button.
	if (!m_hwnd)
		return S_OK;

	// Initialize a rootbox for the first pane if we don't already have one (e.g., when
	// switching projects after a document window was already opened.)
	EnsureRootBoxes();
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);
	int ihvoCurr = prmw->CurRecIndex();
	HvoClsidVec & vhcRecords = prmw->Records();
	if (!vhcRecords.Size())
	{
		// No records to display.
		prmw->SetCurRecIndex(0); // Make sure it is legal.
		return S_OK;
	}

	// Conditionally update the modification date of the last record.
	prmw->UpdateRecordDate();

	switch(bmk)
	{
	case DBBMK_FIRST:
		ihvoCurr = 0;
		break;
	case DBBMK_LAST:
		ihvoCurr = max(vhcRecords.Size() - 1, 0);
		break;
	case 0:
		ihvoCurr += drec;
		break;
	}
	Assert(ihvoCurr >= 0);
	prmw->SetCurRecIndex(ihvoCurr);
	m_hvoCurrent = vhcRecords[ihvoCurr].hvo;

	ShowCurrentRec();

	AfStatusBarPtr qstbr = prmw->GetStatusBarWnd();
	Assert(qstbr);
	qstbr->StepProgressBar();

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Display the item with the given HVO.

	@param hvo	The item to show
	@return		S_OK if sucessfull or E_FAIL if not.
----------------------------------------------------------------------------------------------*/
HRESULT AfClientRecVwWnd::DispRec(HVO hvoNew)
{
	// Initialize a rootbox for the first pane if we don't already have one (e.g., when
	// switching projects after a document window was already opened.)
	// (I don't know if this is needed; I just copied it from DispCurRec. --Sharon)
	EnsureRootBoxes();
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);
	HvoClsidVec & vhc = prmw->Records();
	int ihc;
	for (ihc = 0; ihc < vhc.Size(); ihc++)
	{
		if (vhc[ihc].hvo == hvoNew)
		{
			// Conditionally update the modification date of the last record.
			prmw->UpdateRecordDate();
			prmw->SetCurRecIndex(ihc);
			break;
		}
	}
	if (ihc >= vhc.Size())
		return E_FAIL; // couldn't find the requested record

	m_hvoCurrent = hvoNew;
	ShowCurrentRec();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Make sure the root box exists.
----------------------------------------------------------------------------------------------*/
void AfClientRecVwWnd::EnsureRootBoxes()
{
	AfVwRecSplitChildPtr qavrsc = dynamic_cast<AfVwRecSplitChild*>(GetPane(0));
	qavrsc->EnsureRootBox();
	AfVwRecSplitChildPtr qavrsc1 = dynamic_cast<AfVwRecSplitChild*>(GetPane(1));
	if (qavrsc1)
		qavrsc->CopyRootTo(qavrsc1);
}

/*----------------------------------------------------------------------------------------------
	If the current selection (eg, created by shift-clicking or dragging) is not contained
	within a single text field, expand the selection to include the entire entry or entries.
----------------------------------------------------------------------------------------------*/
void AfClientRecVwWnd::SelectWholeObjects(IVwRootBox * prootb)
{
	bool fOneEntry = false;

	IVwSelectionPtr qsel;
	CheckHr(prootb->get_Selection(&qsel));
	if (!qsel)
	{
		return;
	}

	int clevAnchor, clevEnd;
	HRESULT hr;
	CheckHr(hr = qsel->CLevels(false, &clevAnchor));
	Assert(hr == S_OK);
	CheckHr(hr = qsel->CLevels(true, &clevEnd));
	if (hr == S_FALSE || clevAnchor < 2 || clevEnd < 2)
	{
		Warn("Couldn't expand selection");
		return;
	}

	HVO hvoAnchor, hvoEnd;
	PropTag tagAnchor, tagEnd;
	int ihvoAnchor, ihvoEnd, cpropPrev;
	IVwPropertyStorePtr qvps;

	CheckHr(qsel->PropInfo(false, clevAnchor - 2, &hvoAnchor, &tagAnchor,
		&ihvoAnchor, &cpropPrev, &qvps));
	CheckHr(qsel->PropInfo(true, clevEnd - 2, &hvoEnd, &tagEnd,
		&ihvoEnd, &cpropPrev, &qvps));
	fOneEntry = (hvoAnchor == hvoEnd);
	if (fOneEntry)
	{
		// See if it is within a single field.
		int ihvoTmp;
		CheckHr(qsel->PropInfo(false, 0, &hvoAnchor, &tagAnchor,
			&ihvoTmp, &cpropPrev, &qvps));
		CheckHr(qsel->PropInfo(true, 0, &hvoEnd, &tagEnd,
			&ihvoTmp, &cpropPrev, &qvps));
		if (hvoAnchor == hvoEnd && tagAnchor == tagEnd)
		{
			// Same paragraph.
			return;
		}
		else if (tagAnchor == kflidStTxtPara_Contents && tagEnd == kflidStTxtPara_Contents)
		{
			Assert(clevAnchor > 1);
			Assert(clevEnd > 1);
			CheckHr(qsel->PropInfo(false, 1, &hvoAnchor, &tagAnchor,
				&ihvoTmp, &cpropPrev, &qvps));
			CheckHr(qsel->PropInfo(true, 1, &hvoEnd, &tagEnd,
				&ihvoTmp, &cpropPrev, &qvps));
			if (hvoAnchor == hvoEnd && tagAnchor == tagEnd)
				// Two different paragraphs in the same StText.
				return;
		}
	}

	if (clevAnchor < 2)
		return;  // can't figure out how to do it
	if (clevEnd < 2)
	{
		ihvoEnd = ihvoAnchor; // kludge to handle current bug
	}

	// Need to expand.

	HVO hvoTop;
	int cpropPrevTop;
	PropTag tagTop;
	CheckHr(qsel->PropInfo(false, clevAnchor - 1, &hvoTop, &tagTop,
		&ihvoAnchor, &cpropPrevTop, &qvps));
	VwSelLevInfo rgvsliAnchor[1];
	rgvsliAnchor[0].cpropPrevious = cpropPrevTop;
	rgvsliAnchor[0].ihvo = ihvoAnchor;
	rgvsliAnchor[0].tag = tagTop;

	CheckHr(qsel->PropInfo(true, clevEnd - 1, &hvoTop, &tagTop,
		&ihvoEnd, &cpropPrevTop, &qvps));
	VwSelLevInfo rgvsliEnd[1];
	rgvsliEnd[0].cpropPrevious = cpropPrevTop;
	rgvsliEnd[0].ihvo = ihvoEnd;
	rgvsliEnd[0].tag = tagTop;

	CheckHr(prootb->MakeTextSelInObj(0, 1, rgvsliAnchor, 1, rgvsliEnd,
		true, false, true, // these are ignored
		true, // select the entire object (or objects)
		true, // install the new selection
		NULL)); // don't bother returning the new selection
}


/*----------------------------------------------------------------------------------------------
	Reload and redraw window and child dialogs.
	This only reloads data if the cache has been cleared out first.
----------------------------------------------------------------------------------------------*/
bool AfClientRecVwWnd::FullRefresh()
{
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	if (!prmw)
		return true; // Window isn't there yet.

	if (!IsOkToChange())
		return false; // Don't do anything if there are problems.

	int ihvoCur = prmw->CurRecIndex();
	prmw->SetCurRecIndex(-1);
	m_hvoCurrent = -1;
	AfVwSplitChild * pavsc = dynamic_cast<AfVwSplitChild *>(CurrentPane());
	AssertPtr(pavsc);
	IVwRootBoxPtr qrootb;
	pavsc->get_RootBox(&qrootb);
	AfVwSplitChild *pavscT = dynamic_cast<AfVwSplitChild *>(GetPane(0));
	if (pavscT && qrootb)
	{
		if (!pavscT->FullRefresh())
			return false;
		::InvalidateRect(pavscT->Hwnd(), NULL, true);
	}
	pavscT = dynamic_cast<AfVwSplitChild *>(GetPane(1));
	if (pavscT)
	{
		// Note: we do NOT need to reconstruct its root box. The two panes share one.
		::InvalidateRect(pavscT->Hwnd(), NULL, true);
	}
	DispCurRec(0, ihvoCur + 1);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Get the view ready for synchronization/refresh
	@param sync -> The information describing a given change.
----------------------------------------------------------------------------------------------*/
bool AfClientRecVwWnd::PreSynchronize(SyncInfo & sync)
{
	if (!m_qsplf)
		return true; // Nothing present yet.
	IVwRootBoxPtr qrootb;
	AfVwSplitChild *pavsc = dynamic_cast<AfVwSplitChild *>(GetPane(0));
	pavsc->get_RootBox(&qrootb);
	if (pavsc && qrootb && !pavsc->PreSynchronize(sync))
		return false;
	return true;
}


/*----------------------------------------------------------------------------------------------
	The user has selected another filter or sort method, which means that the root objects
	shown by the client window need to be refreshed. Call RnMainWnd::Records() to get the new
	list of root objects to show. The current record should be kept in view as long as it is
	listed in Records(). Otherwise, scroll to the top (or show the first record).
----------------------------------------------------------------------------------------------*/
void AfClientRecVwWnd::ReloadRootObjects()
{
	if (!m_hwnd)
		return;

	AfMdiClientWnd * pmdic = dynamic_cast<AfMdiClientWnd *>(Parent());
	AssertPtr(pmdic);
	int iview;
	iview = pmdic->GetChildIndexFromWid(GetWindowId());
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);
	UserViewSpecVec & vuvs = prmw->GetLpInfo()->GetDbInfo()->GetUserViewSpecs();
	//Assert(vuvs[iview]->m_vwt == kvwtBrowse);
	CustViewDaPtr qcvd = prmw->MainDa();
	AssertPtr(qcvd);

	AfStatusBarPtr qstbr = prmw->GetStatusBarWnd();
	Assert(qstbr);
	qstbr->StepProgressBar();

	// Load all data needed for browse view into the data access object.
	TagFlids tf;
	prmw->GetTagFlids(tf);
	qcvd->SetTags(tf.flidRoot, tf.flidCreated);
	qcvd->LoadData(prmw->Records(), vuvs[iview], qstbr, false);

	for (int iPane = 0; iPane < 2; ++iPane)
	{
		AfVwRecSplitChild * pavrsc = dynamic_cast<AfVwRecSplitChild*>(GetPane(iPane));
		if (pavrsc)
		{
			if (iPane == 0)
			{
				IVwRootBoxPtr qrootb;
				pavrsc->get_RootBox(&qrootb);
				qrootb->Reconstruct();
			}
			::InvalidateRect(pavrsc->Hwnd(), NULL, true);
		}
	}

	SuperClass::ReloadRootObjects();
}


/*----------------------------------------------------------------------------------------------
	Return the what's-this help string in the Browse or Document Window for the area
	at the given point.

	@param pt Screen location of a mouse click.
	@param pptss Address of a pointer to an ITsString COM object for returning the help string.

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfClientRecVwWnd::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	StrApp str;
	Point ptClient(pt);
	::ScreenToClient(m_hwnd, &ptClient);

	Rect rc;
	AfWnd::GetClientRect(rc);

	AfSplitChild * pAfSC = CurrentPane();
	SCROLLINFO sinfo = { isizeof(SCROLLINFO), SIF_POS, 0, 0, 0, 0 };
	bool fWorked = AfSplitterClientWnd::GetScrollInfo(pAfSC, SB_HORZ, &sinfo);

	if (fWorked && (ptClient.x < (21 - sinfo.nPos)))
		str = "Displays icons ...";
	else
		return false;

	RecMainWnd * prmw = dynamic_cast<RecMainWnd*>(MainWindow());
	AssertPtr(prmw);
	if (!AfUtil::GetResourceStr(krstWhatsThisEnabled, ::GetDlgCtrlID(m_hwnd), str))
		str.Load(prmw->GetWhatsThisStid()); // No context help available
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(str);
	CheckHr(qtsf->MakeString(stu.Bstr(), prmw->UserWs(), pptss));
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return true if the given object is selected, specifically if it is the anchor of the
	selection. (If the selection includes the object but the anchor is within a different
	object, return false.)
----------------------------------------------------------------------------------------------*/
bool AfClientRecVwWnd::ObjectIsSelected(HVO hvo)
{
	AfSplitChildPtr	qsplc = CurrentPane();
	AfVwSplitChildPtr qavsc = dynamic_cast<AfVwSplitChild *>(qsplc.Ptr());
	if (!qavsc)
		return false;
	IVwRootBoxPtr qrootb;
	qavsc->get_RootBox(&qrootb);
	IVwSelectionPtr qsel;
	CheckHr(qrootb->get_Selection(&qsel));
	if (!qsel)
		return false;

	int clevAnchor;
	CheckHr(qsel->CLevels(false, &clevAnchor));

	HVO hvoAnchor;
	PropTag tagAnchor;
	int ihvoAnchor, cpropPrev;
	IVwPropertyStorePtr qvps;

	CheckHr(qsel->PropInfo(false, clevAnchor - 2, &hvoAnchor, &tagAnchor,
		&ihvoAnchor, &cpropPrev, &qvps));

	return (hvo == hvoAnchor);
}
