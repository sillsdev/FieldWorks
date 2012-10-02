/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, SIL International. All rights reserved.

File: AfDocSplitChild.cpp
Responsibility: Randy Regnier
Last reviewed: never

Description:
	This file contains AfDocSplitChild.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

static DummyFactory g_fact(_T("SIL.AppCore.AfDocSplitChild"));

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	AfDocSplitChild Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDocSplitChild::AfDocSplitChild(bool fScrollHoriz) : AfVwRecSplitChild(fScrollHoriz)
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDocSplitChild::~AfDocSplitChild()
{
}


/*----------------------------------------------------------------------------------------------
	Prepare to hide the window.
----------------------------------------------------------------------------------------------*/
void AfDocSplitChild::PrepareToHide()
{
	AfApp::Papp()->RemoveCmdHandler(this, 1);
}


/*----------------------------------------------------------------------------------------------
	Prepare to show the window.
----------------------------------------------------------------------------------------------*/
void AfDocSplitChild::PrepareToShow()
{
	AfApp::Papp()->AddCmdHandler(this,1);
}


/*----------------------------------------------------------------------------------------------
	Clear out the window contents, and especially any active database connections, when the
	project is (about to be) changed.
----------------------------------------------------------------------------------------------*/
bool AfDocSplitChild::CloseProj()
{
	// Conditionally update the modification date on the last record we were in.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	if (prmw)
		prmw->UpdateRecordDate();

	// We can't get rid of the root box here since OnKillFocus gets called after this and uses
	// the root box. The rootbox is cleared in OnReleasePtr.

	// Let it know it needs layout.
	m_dxdLayoutWidth = -50000;
	// TODO JohnT: check whether anything needs saving. (Is this the right place?)
	Invalidate();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Close things down.
----------------------------------------------------------------------------------------------*/
void AfDocSplitChild::OnReleasePtr()
{
	SuperClass::OnReleasePtr();
	AfApp::Papp()->RemoveCmdHandler(this, 1);
}

/*----------------------------------------------------------------------------------------------
	Overridden to track the current record in order to update the status bar and keep things
	synchronized if the user changes view.

	@param pvwselNew Represents the new selection of something within the view.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfDocSplitChild::SelectionChanged(IVwRootBox * prootb, IVwSelection * pvwselNew)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvwselNew);

	if (!pvwselNew)
		return S_OK;
	int cvsli;
	CheckHr(pvwselNew->CLevels(false, &cvsli));
	// CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
	cvsli--;
	if (cvsli <= 0)
	{
		// Some strange selection, perhaps a literal string, can't handle as yet.
		return S_OK;
	}
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
	CheckHr(pvwselNew->AllTextSelInfo(&ihvoRoot, cvsli, prgvsli, &tagTextProp,
		&cpropPrevious, &ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));
	int ihvoCurr = prgvsli[cvsli - 1].ihvo;
	delete[] prgvsli;

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
	Assert(vhvo.Size() == vflid.Size());
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

	END_COM_METHOD(g_fact, IID_IVwRootSite)
}

/*----------------------------------------------------------------------------------------------
	Make the root box.

	@param pvg (Not used here)
	@param pprootb Out The RootBox to be returned.
	@param fPrintingSel True if we are printing only the selected entries.
----------------------------------------------------------------------------------------------*/
void AfDocSplitChild::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
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
		return; // No current session, can't make root box.

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
	Assert(vuvs[iview]->m_vwt == kvwtDoc);
	RecordSpecPtr qrsp;
	ClsLevel clevKey(prmw->GetDocumentClsid(), 0);
	vuvs[iview]->m_hmclevrsp.Retrieve(clevKey, qrsp);
	m_qvcvc.Attach(prmw->CreateCustDocVc(vuvs[iview]));

	// Get (or make) the custom view database access cache.
	CustViewDaPtr qcvd = prmw->MainDa();
	AssertPtr(qcvd);

	AfStatusBarPtr qstbr = prmw->GetStatusBarWnd();
	Assert(qstbr);
	qstbr->StepProgressBar();

	// Tell the view constructor where to load data.
	m_qvcvc->SetDa(qcvd, qstbr, vuvs[iview]);
	if (pwsf)
		CheckHr(qcvd->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qcvd));
	IVwViewConstructor * pvvc = m_qvcvc;
	CheckHr(qrootb->SetRootObjects(&hvo, &pvvc, &frag, plpi->GetAfStylesheet(), 1));
	*pprootb = qrootb;
	(*pprootb)->AddRef();

	prmw->RegisterRootBox(qrootb);

	qstbr->StepProgressBar();
}

/*----------------------------------------------------------------------------------------------
	Process a mouse click. If it is on a ref, jump/launch to that ref. Otherwise use the default
	action.
----------------------------------------------------------------------------------------------*/
void AfDocSplitChild::CallMouseDown(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot)
{
	m_wsPending = -1;

	// Get the tentative selection at the mouse location regardless of editability.
	IVwSelectionPtr qsel;
	// For some reason, this can fail to get a selection while changing filters. There
	// may be a better solution than this, but if we don't get a selection, we might as
	// well pass the click up higher.
	m_qrootb->MakeSelAt(xp, yp, rcSrcRoot, rcDstRoot, false, &qsel);
	if (!qsel)
	{
		SuperClass::CallMouseDown(xp, yp, rcSrcRoot, rcDstRoot);
		return;
	}

	// Get information on the selection.
	int cvsli;
	CheckHr(qsel->CLevels(false, &cvsli));
	if (cvsli <= 1)
	{
		// This won't be a valid reference property, so skip further testing.
		SuperClass::CallMouseDown(xp, yp, rcSrcRoot, rcDstRoot);
		return;
	}

	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);
	TagFlids tf;
	prmw->GetTagFlids(tf);
	AfLpInfoPtr qlpi = prmw->GetLpInfo();
	AssertPtr(qlpi);
	IFwMetaDataCachePtr qmdc;
	qlpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
	AssertPtr(qmdc);
	CustViewDaPtr qcvd = prmw->MainDa();
	AssertPtr(qcvd);

	// PropInfo index 0 will give us the object we clicked on, assuming index 1 flid is
	// an object reference. Example:
	// isli = 0, hvo = 2348, flid = 4004001, ihvo = -1
	// isli = 1, hvo = 2352, flid = 4005005, ihvo = 2
	// isli = 2, hvo = 2339, flid = 4004009, ihvo = 3
	// isli = 3, hvo = 2337, flid = 4004009, ihvo = 1
	// In this example, 2348 is a reference to a record in the 4005005 reference property.
	HVO hvo;
	int flid;
	int ihvo;
	int cpropPrev;
	IVwPropertyStorePtr qvps;
	CheckHr(qsel->PropInfo(false, 1, &hvo, &flid, &ihvo, &cpropPrev, &qvps));
	int nType;
	CheckHr(qmdc->GetFieldType(flid, &nType));

	if (((1 << nType) & kgrfcptReference) == 0)
	{
		// We aren't dealing with a reference, so exit.
		SuperClass::CallMouseDown(xp, yp, rcSrcRoot, rcDstRoot);
		return;
	}
	// We have a reference property. Get the referenced object
	// and see if it is an object we can deal with.
	CheckHr(qsel->PropInfo(false, 0, &hvo, &flid, &ihvo, &cpropPrev, &qvps));
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

/*----------------------------------------------------------------------------------------------
	Handle window messages.

	See ${AfWnd#FWndProcPre}
	@param wm windows message
	@param wp WPARAM
	@param lp LPARAM
	@param lnRet Value to be returned to the windows.
	@return true
----------------------------------------------------------------------------------------------*/
bool AfDocSplitChild::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_KEYDOWN && wp == VK_TAB)
	{
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
		AssertPtr(prmw);
		int ihvoCurr = prmw->CurRecIndex();
		int crec = prmw->Records().Size();
		VwShiftStatus ss = (::GetKeyState(VK_SHIFT) < 0) ? kfssShift : kfssNone;
		if (m_qrootb->OnExtendedKey(VK_TAB, ss, 0) == S_FALSE)
		{
			AfClientRecWnd * pafcrw = dynamic_cast<AfClientRecWnd *>(GetSplitterClientWnd());
			AssertPtr(pafcrw);
			if (ss == kfssNone && ihvoCurr < crec - 1)
			{
				pafcrw->DispCurRec(NULL, 1);
			}
			else if (ss == kfssShift && ihvoCurr > 0)
			{
				pafcrw->DispCurRec(NULL, -1);
				// Now, move to last editable field of this record.
				m_qrootb->OnExtendedKey(VK_TAB, kfssControl, 0);
			}
		}
		return true;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}
