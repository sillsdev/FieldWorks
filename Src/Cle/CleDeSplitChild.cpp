/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, SIL International. All rights reserved.

File: CleDeSplitChild.cpp
Responsibility: Rand Burgett
Last reviewed: never

Description:
	This class provides the data entry Choices List Editor functions.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"
#include "GpHashMap_i.cpp" // Needed for release version.


#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	The command map for popup menus.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(CleDeSplitChild)
// CleMainWnd::OnIdle creates a CmdState with CleMainWnd as the qcmh. Since we are in a different
// class here, we use ON_CID_CHILD so that these will be executed in the same frame window.
	ON_CID_CHILD(kcidInsItem, &CleDeSplitChild::InsertEntry, NULL)
	ON_CID_CHILD(kcidInsItemBef, &CleDeSplitChild::InsertEntry, NULL)
	ON_CID_CHILD(kcidInsItemAft, &CleDeSplitChild::InsertEntry, NULL)
	ON_CID_CHILD(kcidInsSubItem, &CleDeSplitChild::InsertEntry, NULL)
	ON_CID_CHILD(kcidEditDel, &CleDeSplitChild::CmdDelete, NULL)
END_CMD_MAP_NIL()


//:>********************************************************************************************
//:>    CleDeSplitChild Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CleDeSplitChild::CleDeSplitChild()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CleDeSplitChild::~CleDeSplitChild()
{
}

void CleDeSplitChild::LoadOtherData(IOleDbEncap * pode, CustViewDa * pcvd, HvoClsid & hcRoot)
{
	Assert(pode);
	Assert(pcvd);
}

/*----------------------------------------------------------------------------------------------
	Add menu items for the right-click context Insert menu over tree pane.
----------------------------------------------------------------------------------------------*/
void CleDeSplitChild::AddContextInsertItems(HMENU & hmenu)
{
	StrApp str;

	CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
	Assert(pcmw);

	PossListInfo * ppli = pcmw->GetPossListInfoPtr();
	AssertPtr(ppli);

	if (!ppli->GetIsSorted())
	{
		str.Assign(_T("List Item &Above"));
		::AppendMenu(hmenu, MF_STRING, kcidInsItemBef, str.Chars());
		str.Assign(_T("List Item &Below"));
		::AppendMenu(hmenu, MF_STRING, kcidInsItemAft, str.Chars());
	}
	else
	{
		str.Assign(_T("List &Item"));
		::AppendMenu(hmenu, MF_STRING, kcidInsItem, str.Chars());
	}
	if (ppli->GetDepth() != 1)
	{
		str.Assign(_T("List &Subitem"));
		::AppendMenu(hmenu, MF_STRING, kcidInsSubItem, str.Chars());
	}
}

/*----------------------------------------------------------------------------------------------
	This launches a dialog to choose an item for a reference field.

	This method should be overwritten to do something more useful.

	@param pdfr The reference field editor making this request
	@return An object id to add to the reference field. 0 for none.
----------------------------------------------------------------------------------------------*/
HVO CleDeSplitChild::LaunchRefChooser(AfDeFeRefs * pdfr)
{
	Assert(pdfr);

	return 0;
}


/*----------------------------------------------------------------------------------------------
	Add a field editor for a given field at the location and indent specified.

	@param hvoRoot Id of the root object that holds the fields we want to display.
	@param clid Class of the root object.
	@param nLev Level (main/sub) of the root object in the window.
	@param pfsp The FldSpec that defines this field.
	@param pcvd Pointer to the CustViewDa specifying what fields to display.
	@param idfe Index where the new fields are to be inserted. On return it contains
		an index to the field following any inserted fields.
	@param nInd Indent of the fields to be added.
	@param fAlwaysVisible If true, show field regardless of pbsp->m_eVisibility
----------------------------------------------------------------------------------------------*/
void CleDeSplitChild::AddField(HVO hvoRoot, int clid, int nLev, FldSpec * pfsp,
	CustViewDa * pcvd, int & idfe, int nInd, bool fAlwaysVisible)
{
	AssertPtr(pcvd);
	AssertPtr(pfsp);

	bool fCheckMissingData = fAlwaysVisible ? false : pfsp->m_eVisibility == kFTVisIfData;

	// If the user never wants to see this field, skip it.
	if (pfsp->m_eVisibility == kFTVisNever)
		return;

	ITsStringPtr qtss;

	AfLpInfo * plpi = pcvd->GetLpInfo();
	AssertPtr(plpi);

	AfStatusBarPtr qstbr = MainWindow()->GetStatusBarWnd();
	Assert(qstbr);

	switch(pfsp->m_ft)
	{
	case kftEnum:
		{
			AfDeFeComboBox * pdecb = NewObj AfDeFeComboBox();
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			int itss;
			CheckHr(pcvd->get_IntProp(hvoRoot, pfsp->m_flid, &itss));
			pdecb->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel,
				pfsp->m_qtssHelp, this, pfsp);
			pdecb->Init(pfsp->m_pnt);
			ComVector<ITsString> * pvtss;
			pvtss = pdecb->GetVec();
			int stid;
			switch (pfsp->m_flid)
			{
			case kflidCmPerson_Gender:
				stid = kstidEnumGender;
				break;
#ifdef ADD_LEXTEXT_LISTS
			case kflidCmAnnotationDefn_AllowsComment: // Fall through.
			case kflidCmAnnotationDefn_AllowsFeatureStructure: // Fall through.
			case kflidCmAnnotationDefn_AllowsInstanceOf: // Fall through.
			case kflidCmAnnotationDefn_UserCanCreate: // Fall through.
			case kflidCmAnnotationDefn_CanCreateOrphan: // Fall through.
			case kflidCmAnnotationDefn_PromptUser: // Fall through.
			case kflidCmAnnotationDefn_CopyCutPastable: // Fall through.
			case kflidCmAnnotationDefn_ZeroWidth: // Fall through.
			case kflidCmAnnotationDefn_Multi: // Fall through.
#endif
			case kflidMoInflAffixSlot_Optional: // Fall through.
			case kflidCmPerson_IsResearcher:
				stid = kstidEnumNoYes;
				if (itss)
					itss = 1;
				break;
			default:
				Assert(false); // A list must be provided above.
				break;
			}
			StrUni stuEnum(stid);
			const wchar * pszEnum = stuEnum.Chars();
			const wchar * pszEnumLim = stuEnum.Chars() + stuEnum.Length();
			while (pszEnum < pszEnumLim)
			{
				const wchar * pszEnumNl = wcschr(pszEnum, '\n');
				if (!pszEnumNl)
					pszEnumNl = pszEnumLim;

				AfDbInfo * pdbi = plpi->GetDbInfo();
				AssertPtr(pdbi);
				int wsUser = pdbi->UserWs();

				qtsf->MakeStringRgch(pszEnum, pszEnumNl - pszEnum, wsUser,
					&qtss);
				pvtss->Push(qtss);
				pszEnum = pszEnumNl + 1;
			}
			pdecb->SetIndex(itss);
			m_vdfe.Insert(idfe++, pdecb);
		}
		return;

	case kftMsa:	// Fall through.
	case kftMta:
		{
			Vector<int> vwsT;
			Vector<int> & vws = vwsT;

			// We need a special editor for name and address to catch changes to update the
			// tree views.
			if ((pfsp->m_flid == kflidCmPossibility_Name) ||
				(pfsp->m_flid == kflidCmPossibility_Abbreviation))
			{
				CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
				Assert(pcmw);
		//		PossListInfoPtr qpli = pcmw->GetPossListInfoPtr();
				int wsPL= pcmw->GetWsPssl();
				switch (wsPL)
				{
				case kwsAnals:
					vws = plpi->AnalWss();
					break;
				case kwsVerns:
					vws = plpi->VernWss();
					break;
				case kwsAnal:
					vwsT.Push(plpi->AnalWs());
					break;
				case kwsVern:
					vwsT.Push(plpi->VernWs());
					break;
				case kwsAnalVerns:
					vws = plpi->AnalVernWss();
					break;
				case kwsVernAnals:
					vws = plpi->VernAnalWss();
					break;
				default:
					vwsT.Push(wsPL);
					break;
				}

				if (fCheckMissingData)
				{
					ITsStringPtr qtss;
					int iws;
					for (iws = vws.Size(); --iws >= 0; )
					{
						CheckHr(pcvd->get_MultiStringAlt(hvoRoot, pfsp->m_flid, vws[iws],
							&qtss));
						int cch;
						CheckHr(qtss->get_Length(&cch));
						if (cch)
							break;
					}
					if (iws < 0)
						return; // All writing systems are empty.
				}
				// An extra ref cnt is created here which is eventually assigned to the vector.
				CleDeFeStringPtr qdfs = NewObj CleDeFeString;
				qdfs->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel,
					pfsp->m_qtssHelp, this, pfsp);

				if (pfsp->m_ft == kftMta)
					qdfs->Init(&vws, ktptSemiEditable);
				else
					qdfs->Init(&vws, ktptIsEditable);

				m_vdfe.Insert(idfe++, qdfs);
			}
			else
			{
				switch (pfsp->m_ws)
				{
				case kwsAnals:
					vws = plpi->AnalWss();
					break;
				case kwsVerns:
					vws = plpi->VernWss();
					break;
				case kwsAnal:
					vwsT.Push(plpi->AnalWs());
					break;
				case kwsVern:
					vwsT.Push(plpi->VernWs());
					break;
				case kwsAnalVerns:
					vwsT = plpi->AnalVernWss();
					break;
				case kwsVernAnals:
					vwsT = plpi->VernAnalWss();
					break;
				default:
					vwsT.Push(pfsp->m_ws);
					Assert(pfsp->m_ws);
					break;
				}

				if (fCheckMissingData)
				{
					ITsStringPtr qtss;
					int iws;
					for (iws = vws.Size(); --iws >= 0; )
					{
						CheckHr(pcvd->get_MultiStringAlt(hvoRoot, pfsp->m_flid, vws[iws],
							&qtss));
						int cch;
						CheckHr(qtss->get_Length(&cch));
						if (cch)
							break;
					}
					if (iws < 0)
						return; // All writing systems are empty.
				}
				// An extra ref cnt is created here which is eventually assigned to the vector.
				AfDeFeStringPtr qdfs = NewObj AfDeFeString;
				qdfs->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel,
					pfsp->m_qtssHelp, this, pfsp);

				if (pfsp->m_ft == kftMta)
					qdfs->Init(&vws, ktptSemiEditable);
				else
					qdfs->Init(&vws, ktptIsEditable);

				m_vdfe.Insert(idfe++, qdfs);
			}

		}
		return;
	case kftExpandable:	// Fall through.
	case kftSubItems:
		return;

	case kftObjRefAtomic:	// Fall through.
	case kftObjRefSeq:
		{
			bool fMultiRefs;
			if (pfsp->m_ft == kftObjRefAtomic)
			{
				HVO hvoRef;
				CheckHr(pcvd->get_ObjectProp(hvoRoot, pfsp->m_flid, &hvoRef));
				fMultiRefs = false;
				if (fCheckMissingData && !hvoRef)
					return;
			}
			else
			{
				fMultiRefs = true;
				if (fCheckMissingData) // otherwise no need to check
				{
					int chvo;
					CheckHr(pcvd->get_VecSize(hvoRoot, pfsp->m_flid, &chvo));
					if (!chvo)
						return;
				}
			}
			AfDeFeRefs * pdfr = NewObj AfDeFeRefs;
			pdfr->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel,
				pfsp->m_qtssHelp, this, pfsp);
			switch (pfsp->m_flid)
			{
			default:
				// Standard kftObjRefAtomic & kftObjRefSeq fields are all handled in the superclass.
				SuperClass::AddField(hvoRoot, clid, nLev, pfsp, pcvd, idfe, nInd, fAlwaysVisible);
				return;
			}
			m_vdfe.Insert(idfe++, pdfr);
		}
		return;
	}
	qstbr->StepProgressBar(10);

	// Standard fields are all handled in the superclass.
	SuperClass::AddField(hvoRoot, clid, nLev, pfsp, pcvd, idfe, nInd, fAlwaysVisible);
	return;
}


/*----------------------------------------------------------------------------------------------
	Handle context menu ().
	TODO KenZ: Make this work. We just have a skeleton so far.

	@param hwnd Current window
	@param pt Mouse location
----------------------------------------------------------------------------------------------*/
void CleDeSplitChild::OnContextMenu(HWND hwnd, POINT pt)
{
	// Show a popup menu
	HMENU hmenuPopup = ::CreatePopupMenu();
	Point ptT = pt;
	::ScreenToClient(m_hwnd, &ptT);
	if (ptT.x > m_dxpTreeWidth)
		return; // The following menu only applies to the tree view.
	HMENU hmenuInsert = ::CreatePopupMenu();
	::AppendMenu(hmenuPopup, MF_POPUP | MF_ENABLED, (uint)hmenuInsert, _T("Insert"));
	::AppendMenu(hmenuPopup, MF_STRING, 2, _T("Show"));
	::AppendMenu(hmenuPopup, MF_STRING | MF_ENABLED | MF_ENABLED, 3, _T("Delete"));
	::AppendMenu(hmenuPopup, MF_STRING | MF_ENABLED, 4, _T("Help"));
	::AppendMenu(hmenuInsert, MF_STRING | MF_ENABLED, 5, _T("List Item"));
	::DrawMenuBar(hwnd);
	int nItem;
	// The next function returns when the menu is closed.
	nItem = ::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON | TPM_RETURNCMD,
		pt.x, pt.y, 0, hwnd, NULL);
	::DestroyMenu(hmenuInsert);
	::DestroyMenu(hmenuPopup);
}


/*----------------------------------------------------------------------------------------------
	Set the title of the tree node.

	@param pden Node of the tree to be whose header is to be set.
----------------------------------------------------------------------------------------------*/
void CleDeSplitChild::SetTreeHeader(AfDeFeNode * pden)
{
}


/*----------------------------------------------------------------------------------------------
	Process left button down (WM_LBUTTONDOWN). Starts a tree drag, a label drag, outline toggle,
	and activating an editor window.
	@param grfmk Indicates whether various virtual keys are down.
	@param xp The x-coord of the mouse relative to the upper-left corner of the client.
	@param yp The y-coord of the mouse relative to the upper-left corner of the client.
	@return Return true if processed.
----------------------------------------------------------------------------------------------*/
bool CleDeSplitChild::OnLButtonDown(uint grfmk, int xp, int yp)
{
	m_fNeedSync = false; // Clear from previous EndEdits.
	bool f = SuperClass::OnLButtonDown(grfmk, xp, yp);
	if (m_fNeedSync)
	{
		// Update all lists now that we've changed.
		CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
		Assert(pcmw);
		SyncInfo sync(ksyncPossList, pcmw->GetRootObj(), 0);
		m_qlpi->StoreAndSync(sync);
		m_fNeedSync = false;
	}
	return f;
}


/*----------------------------------------------------------------------------------------------
	Process right button down (WM_LBUTTONDOWN). Activates an editor window if needed.
	@param grfmk Indicates whether various virtual keys are down.
	@param xp The x-coord of the mouse relative to the upper-left corner of the client.
	@param yp The y-coord of the mouse relative to the upper-left corner of the client.
	@return Return true if processed.
----------------------------------------------------------------------------------------------*/
bool CleDeSplitChild::OnRButtonDown(uint grfmk, int xp, int yp)
{
	m_fNeedSync = false; // Clear from previous EndEdits.
	bool f = SuperClass::OnRButtonDown(grfmk, xp, yp);
	if (m_fNeedSync)
	{
		// Update all lists now that we've changed.
		CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
		Assert(pcmw);
		SyncInfo sync(ksyncPossList, pcmw->GetRootObj(), 0);
		m_qlpi->StoreAndSync(sync);
		m_fNeedSync = false;
	}
	return f;
}


/*----------------------------------------------------------------------------------------------
	If there is another editable field below the currently open field, this closes the current
	field and opens the next editable field. dxpCursor allows the insertion point in the new
	field to be located at some point other than the left margin. The insertion point is
	always placed on the top line.
	@param dxpCursor Horiz. pixels from the left field margin for the insertion point.
	@return True if it succeeded in opening a new field. False if the current field editor
		can't be closed or if there are no editable fields below the current field.
----------------------------------------------------------------------------------------------*/
bool CleDeSplitChild::OpenNextEditor(int dxpCursor)
{
	m_fNeedSync = false; // Clear from previous EndEdits.
	bool f = SuperClass::OpenNextEditor(dxpCursor);
	if (m_fNeedSync)
	{
		// Update all lists now that we've changed.
		CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
		Assert(pcmw);
		SyncInfo sync(ksyncPossList, pcmw->GetRootObj(), 0);
		m_qlpi->StoreAndSync(sync);
		m_fNeedSync = false;
	}
	return f;
}


/*----------------------------------------------------------------------------------------------
	If there is another editable field above the currently open field, this closes the current
	field and opens the previous editable field. dxpCursor allows the insertion point in
	the new field to be located at some point other than the left margin. fTopCursor determines
	whether the insertion point is placed on the top or bottom line.
	@param dxpCursor Horiz. pixels from the left field margin for the insertion point.
	@param fTopCursor True if the insertion point is to be placed on the top field line.
	@return True if it succeeded in opening a new field. False if the current field editor
		can't be closed or if there are no editable fields above the current field.
----------------------------------------------------------------------------------------------*/
bool CleDeSplitChild::OpenPreviousEditor(int dxpCursor, bool fTopCursor)
{
	m_fNeedSync = false; // Clear from previous EndEdits.
	bool f = SuperClass::OpenPreviousEditor(dxpCursor, fTopCursor);
	if (m_fNeedSync)
	{
		// Update all lists now that we've changed.
		CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
		Assert(pcmw);
		SyncInfo sync(ksyncPossList, pcmw->GetRootObj(), 0);
		m_qlpi->StoreAndSync(sync);
		m_fNeedSync = false;
	}
	return f;
}


/*----------------------------------------------------------------------------------------------
	Set the startup information this window to the promoted entry.
	@param hvo The id of the entry or subentry that has been promoted.
----------------------------------------------------------------------------------------------*/
void CleDeSplitChild::PromoteSetup(HVO hvo)
{
	Assert(hvo);
	// At the moment we are assuming hvo is a CmPossibility (or subclass). At some point this
	// will probably change, which will require additional code.
	Vector<HVO> vhvo;
	Vector<int> vflid;
	// Set up the path to hvo.
	vhvo.Push(hvo);
	// Add owning records, if there are any, until we reach the main record.
	CustViewDaPtr qcvd;
	m_qlpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);
	HVO hvoRoot = prmw->GetRootObj();
	HVO hvoOwn;
	while (hvo)
	{
		CheckHr(qcvd->get_ObjOwner(hvo, &hvoOwn));
		hvo = hvoOwn;
		if (hvo == hvoRoot || !hvo)
			break;
		vhvo.Insert(0, hvo);
		vflid.Insert(0, kflidCmPossibility_SubPossibilities);
	}
	prmw->SetStartupInfo(vhvo.Begin(), vhvo.Size(), vflid.Begin(), vflid.Size(), 0, 0);
}


//:>********************************************************************************************
//:>    CleDeFeString Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Check to see if the edit box has valid data.  if so return true.  If not then put up a
	message to the user, then return false.
----------------------------------------------------------------------------------------------*/
bool CleDeFeString::BeginEdit(HWND hwnd, Rect & rc, int dxpCursor, bool fTopCursor)
{
	SuperClass::BeginEdit(hwnd, rc, dxpCursor, fTopCursor);

	CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(m_qadsc->MainWindow());
	Assert(pcmw);
	PossListInfoPtr qpli = pcmw->GetPossListInfoPtr();
	int ipss = qpli->GetIndexFromId(m_hvoObj);
	PossItemInfo * ppii = qpli->GetPssFromIndex(ipss);
	AssertPtr(ppii);

	// Save the primary string for comparison.
	AfLpInfo * plpi = pcmw->GetLpInfo();
	AssertPtr(plpi);
	CustViewDaPtr qcvd = pcmw->MainDa();
	AssertPtr(qcvd);
	CheckHr(qcvd->get_MultiStringAlt(m_hvoObj, m_flid, plpi->ActualWs(qpli->GetWs()), &m_qtssOld));

	switch(m_flid)
	{
		case kflidCmPossibility_Name:
		{
			StrUni stu;
			ppii->GetName(stu, kpntName);
			StrUni stuNewItem(kstidNewItem);
			if (stu.Left(stuNewItem.Length()) == stuNewItem)
				m_qrootb->MakeSimpleSel(true, true, true, true, NULL);
			break;
		}
		case kflidCmPossibility_Abbreviation:
		{
			StrUni stu;
			ppii->GetName(stu, kpntAbbreviation);
			StrUni stuNew(kstidNew);
			if (stu.Left(stuNew.Length()) == stuNew)
				m_qrootb->MakeSimpleSel(true, true, true, true, NULL);
			break;
		}
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Check to see if the edit box has valid data.  if so return true.  If not then put up a
	message to the user, then return false.
----------------------------------------------------------------------------------------------*/
bool CleDeFeString::IsOkToClose(bool fWarn)
{
	CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(m_qadsc->MainWindow());
	Assert(pcmw);

	IVwSelectionPtr qvwsel;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (qvwsel)
	{
		ComBool fOk;
		CheckHr(qvwsel->Commit(&fOk));
	}

	PossListInfoPtr qpli = pcmw->GetPossListInfoPtr();
	int ipss = qpli->GetIndexFromId(m_hvoObj);
	StrUni stuNew;

	const OLECHAR * prgwch;
	int cch;
	ITsStringPtr qtss;
	CustViewDaPtr qcvd;

	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	int ws = m_qsvc->WritingSystems()[0];

	CheckHr(qcvd->get_MultiStringAlt(m_hvoObj, m_flid, ws, &qtss));
	Assert(qtss);
	qtss->LockText(&prgwch, &cch);
	qtss->UnlockText(prgwch);

	// Trim leading and trailing space characters.
	UnicodeString ust(prgwch, cch);
	ust.trim();
	stuNew.Assign(ust.getBuffer(), ust.length());

	//  Obtain pointer to IOleDbEncap interface.
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stuSql;
	ComBool fIsNull;
	ComBool fMoreRows;
	AssertPtr(m_qadsc->MainWindow());
	AfLpInfo * plpi = m_qadsc->MainWindow()->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetDbAccess(&qode);
	AssertPtr(qode);
	CheckHr(qode->CreateCommand(&qodc));
	int cpii = qpli->GetCount();

	if ((m_flid == kflidCmPossibility_Name) || (m_flid == kflidCmPossibility_Abbreviation))
	{
		// Make sure it does not have a ":" or a " - " in the string
		int ich = stuNew.FindStr(L":");
		StrUni stuTmp;
		bool fFixed = false;
		while (ich > 0)
		{
			stuNew.Replace(ich,ich + 1,"-");
			fFixed = true;
			ich = stuNew.FindStr(L":");
		}
		ich = stuNew.FindStr(L" - ");
		while (ich > 0)
		{
			stuNew.Replace(ich,ich + 3,"-");
			fFixed = true;
			ich = stuNew.FindStr(L" - ");
		}
		if (fFixed)
		{
			if (fWarn)
			{
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				qtsf->MakeStringRgch(stuNew.Chars(), stuNew.Length(), pcmw->UserWs(), &qtss);
				CheckHr(qcvd->SetMultiStringAlt(m_hvoObj, m_flid, ws, qtss));
				CheckHr(qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1));
				StrApp strMsg(kstidFixedStr);
				StrApp strTitle(kstidFixedStrTitle);
				::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(),
					MB_OK | MB_ICONINFORMATION);
			}
			return false;
		}
	}

	if (qpli->GetAllowDup())
		return true;

	ILgWritingSystemFactoryPtr qwsf;
	pdbi->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);
	switch (m_flid)
	{
		case kflidCmPossibility_Name:
		{
			for (int ipii = 0; ipii < cpii; ipii++)
			{
				if (ipii == ipss)
					continue;
				PossItemInfo * ppii = qpli->GetPssFromIndex(ipii);
				AssertPtr(ppii);
				StrUni stu;
				ppii->GetName(stu, kpntName);
				if (stu == stuNew)
				{
					stuSql.Format(L"select ws from CmPossibility_Name "
						L"where obj = %d and ws = %d",
						ppii->GetPssId(), ws);
					CheckHr(qode->CreateCommand(&qodc));
					CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
					CheckHr(qodc->GetRowset(0));
					CheckHr(qodc->NextRow(&fMoreRows));

					if (fMoreRows)
					{
						if (fWarn)
						{
							// this name already exists
							IWritingSystemPtr qws;
							CheckHr(qwsf->get_EngineOrNull(ws, &qws));
							AssertPtr(qws);
							SmartBstr sbstr;
							qws->get_Name(ws, &sbstr);

							StrUni stu(kstidDupItemName);
							StrUni stuMsg;
							stuMsg.Format(stu,sbstr.Chars());
							StrApp str(stuMsg);
							StrApp strTitle(kstidDupItemTitle);
							::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(),
								MB_OK | MB_ICONINFORMATION);
						}
						return false;
					}
				}
			}
			break;
		}
		case kflidCmPossibility_Abbreviation:
		{
			for (int ipii = 0; ipii < cpii; ipii++)
			{
				if (ipii == ipss)
					continue;
				PossItemInfo * ppii = qpli->GetPssFromIndex(ipii);
				AssertPtr(ppii);
				StrUni stu;
				ppii->GetName(stu, kpntAbbreviation);
				if (stu == stuNew)
				{
					stuSql.Format(L"select ws from CmPossibility_Abbreviation "
						L"where obj = %d and ws = %d",
						ppii->GetPssId(), ws);
					CheckHr(qode->CreateCommand(&qodc));
					CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
					CheckHr(qodc->GetRowset(0));
					CheckHr(qodc->NextRow(&fMoreRows));

					if (fMoreRows)
					{
						if (fWarn)
						{
							// this abbreviation already exists
							IWritingSystemPtr qws;
							CheckHr(qwsf->get_EngineOrNull(ws, &qws));
							AssertPtr(qws);
							SmartBstr sbstr;
							qws->get_Name(ws, &sbstr);

							StrUni stu(kstidDupItemAbbr);
							StrUni stuMsg;
							stuMsg.Format(stu,sbstr.Chars());
							StrApp str(stuMsg);
							StrApp strTitle(kstidDupItemTitle);
							::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(),
								MB_OK | MB_ICONINFORMATION);
						}
						return false;
					}
				}
			}
			break;
		}
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Close the current editor, saving changes that were made.  This also updates the the cache
	if the name or abbr fields were edited, this then causing the treebar to be updated.
	@param fForce True if we want to force the editor closed without making any
		validity checks or saving any changes.
----------------------------------------------------------------------------------------------*/
void CleDeFeString::EndEdit(bool fForce)
{
	if (fForce)
	{
		SuperClass::EndEdit(fForce);
		return;
	}

	CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
	Assert(pcmw);

	PossListInfoPtr qpli = pcmw->GetPossListInfoPtr();
	int ipss = qpli->GetIndexFromId(m_hvoObj);
	if (ipss < 0)
	{
		SuperClass::EndEdit(fForce); // Closing an old editor after an item was removed.
		return;
	}
	PossItemInfo * ppii = qpli->GetPssFromIndex(ipss);
	AssertPtr(ppii);
	StrUni stu;

	const OLECHAR * prgwch;
	int cch;
	ITsStringPtr qtss;
	AssertPtr(m_qadsc->MainWindow());
	AfLpInfo * plpi = m_qadsc->MainWindow()->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi;
	pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	int wsMagic = qpli->GetWs();
	int ws = plpi->ActualWs(wsMagic);

	// At this point, all strings in a PossList cache are assumed to have the same writing system.
	// However, if that writing system is missing, a non-null string from a different writing system is
	// loaded. Thus if FRN is the writing system for the PossList cache, we can't tell from the
	// cache whether the string we are getting is FRN or ENG, or some other writing system. When
	// we display strings in this editor, we show actual strings for each writing system shown.
	// Suppose we are showing FRN and ENG, but FRN is missing. If a person edits the ENG
	// string that is showing, the user would expect the tree to reflect this change, since
	// FRN is still missing. However we can't do this at this point because we don't know
	// which writing system is actually being substituted in the tree. It could actually be a GER
	// string because there also wasn't an ENG string. So until we cache encodings with each
	// string, the best we can do is only modify the PossList cache if the string for the
	// primary writing system changed. In that case, we now have a FRN string, so it should show
	// in the tree as well.
	CustViewDaPtr qcvd = pcmw->MainDa();
	AssertPtr(qcvd);

	// Get the current primary string.
	CheckHr(qcvd->get_MultiStringAlt(m_hvoObj, m_flid, ws, &qtss));
	if (qtss)
	{
		qtss->LockText(&prgwch, &cch);
		qtss->UnlockText(prgwch);
	}

	// Trim leading and trailing space characters.
	UnicodeString ust(prgwch, cch);
	ust.trim();
	stu.Assign(ust.getBuffer(), ust.length());

	// We don't expect to use this for anything except name or abbr.
	Assert(m_flid == kflidCmPossibility_Name || m_flid == kflidCmPossibility_Abbreviation);
	// We don't allow long strings.
	if (stu.Length() > kcchPossNameAbbrMax) // Need constant here and the line below.
	{
		::MessageBeep(MB_ICONEXCLAMATION); // Beep if we truncated the length.
		stu.Replace(kcchPossNameAbbrMax, stu.Length(), L"");
	}

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, &qtss);

	// If we changed the length, store the trimmed string.
	if (cch != stu.Length())
	{
		// Check if the record has been edited by someone else since we first loaded the data.
		HRESULT hrTemp;
		if ((hrTemp = qcvd->CheckTimeStamp(m_hvoObj)) != S_OK)
		{
			// If it was changed and the user does not want to overwrite it, perform a refresh
			// so the displayed field will revert to its original value.
			CheckHr(qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1));
			return;
		}

		// Update the value in the cache and refresh views.
		CheckHr(qcvd->SetMultiStringAlt(m_hvoObj, m_flid, ws, qtss));
		CheckHr(qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1));
	}

	SuperClass::EndEdit(fForce);

	// See if the primary string has changed.
	ComBool fEqual;
	CheckHr(qtss->Equals(m_qtssOld, &fEqual));
	if (fEqual)
		return; // No change was made, so we don't need to do anything else.

	// A change was made.
	if (m_flid != kflidCmPossibility_Name && m_flid != kflidCmPossibility_Abbreviation)
		return; // Nothing more to do if it isn't a name or abbreviation.

	// We've changed the primary name or abbreviation, so update the PossList cache.
	ppii->SetName(stu, m_flid == kflidCmPossibility_Name ? kpntName : kpntAbbreviation);

	if (qpli->GetIsSorted())
	{
		// The list is sorted, so we need to move the item to its new location.
		if (qpli->PutInSortedPosition(m_hvoObj, false))
		{
			int ihvo;
			pcmw->LoadData();
			HvoClsidVec & vhc = pcmw->Records();
			for (ihvo = vhc.Size(); --ihvo >= 0; )
			{
				if (vhc[ihvo].hvo == m_hvoObj)
					break;
			}
			Assert(ihvo >= 0); // Should have found item.
			pcmw->SetCurRecIndex(ihvo);
		}
	}
	// We can't do a sync in this method that opens new editors, so just flag
	// that it needs to be done.
	// We need to sync if either name or abbreviation has changed. Even though we may only
	// be showing one or the other in our lists, there may be overlays or something else
	// showing the other, so to be safe, we need to do a sync if either changed.
	dynamic_cast<CleDeSplitChild *>(m_qadsc.Ptr())->SetNeedSync(true);
}


/*----------------------------------------------------------------------------------------------
	Process the right+click delete.
	@param pcmd Menu command
	@return true if sucessful
----------------------------------------------------------------------------------------------*/
bool CleDeSplitChild::CmdDelete(Cmd * pcmd)
{
	if ((dynamic_cast<RecMainWnd *>(MainWindow())->GetRecordClid() == kclidPartOfSpeech)
		&& pcmd->m_rgn[0] == kPopupMenu
		&& m_vdfe[m_idfe]->GetIndent() > 0)
			return CmdDeleteObject(pcmd);

	CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
	AssertPtr(pcmw);
	return pcmw->CmdEditDelete(pcmd);
}


/*----------------------------------------------------------------------------------------------
	Respond to the Edit...Delete menu item.
	@param pcmd Menu command info.
	@return true to stop further processing.
----------------------------------------------------------------------------------------------*/
bool CleDeSplitChild::ConfirmDeletion(int flid, bool & fAtomic,
									   bool & fTopLevelObj, int & kstid, bool & fHasRefs)
{
	return false;
}


/*----------------------------------------------------------------------------------------------
	Respond to any of the items in the Insert submenu.
	@param pcmd Menu command info.
	@return true to stop further processing.
----------------------------------------------------------------------------------------------*/
bool CleDeSplitChild::InsertEntry(Cmd * pcmd)
{
	AssertPtr(pcmd);

	CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
	if (pcmw)
	{
		switch (pcmd->m_cid)
		{
		case kcidInsItem:
			::SendMessage(pcmw->Hwnd(), WM_COMMAND, kcidInsListItem, 0);
			return true;
		case kcidInsItemBef:
			::SendMessage(pcmw->Hwnd(), WM_COMMAND, kcidInsListItemBef, 0);
			return true;
		case kcidInsItemAft:
			::SendMessage(pcmw->Hwnd(), WM_COMMAND, kcidInsListItemAft, 0);
			return true;
		case kcidInsSubItem:
			::SendMessage(pcmw->Hwnd(), WM_COMMAND, kcidInsListSubItem, 0);
			return true;
		default:
			return false;
		}
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Check to see if we are editing a new record, if so, update the date modified on the old
	record if changes were made.
----------------------------------------------------------------------------------------------*/
void CleDeSplitChild::BeginEdit(AfDeFieldEditor * pdfe)
{
	HVO hvoNew = pdfe->GetObj();
	if (m_hvoLastObj && m_hvoLastObj != hvoNew)
	{
		// We've opened an editor on a new object.
		CustViewDaPtr qcvd;
		m_qlpi->GetDataAccess(&qcvd);
		AssertPtr(qcvd);
		int clid;
		// Find out if this is a kind of CmPossibility.
		CheckHr(qcvd->get_ObjClid(m_hvoLastObj, &clid));
		IFwMetaDataCachePtr qmdc;
		m_qlpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
		AssertPtr(qmdc);
		do
		{
			if (clid == kclidCmPossibility)
			{
				// The object has a DateModified property, so see that it is current.
				RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
				Assert(prmw);
				prmw->UpdateDateModified(m_hvoLastObj, kflidCmPossibility_DateModified);
				break;
			}
			ulong uclid;
			qmdc->GetBaseClsId(clid, &uclid);
			clid = (int)uclid;
		} while (clid != 0);
	}
}


/*----------------------------------------------------------------------------------------------
	Synchronize all windows in this application with any changes made in the database.
	@param sync -> The information describing a given change.
----------------------------------------------------------------------------------------------*/
bool CleDeSplitChild::Synchronize(SyncInfo & sync)
{
	SuperClass::Synchronize(sync);

	switch(sync.msg)
	{
	case ksyncAddPss:
	case ksyncDelPss:
	case ksyncPossList:
		{
			RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
			Assert(prmw);
			int chvo = prmw->Records().Size(); // Need to handle deleting last item.
			if (m_vdfe.Size())
			{
				if (chvo && prmw->Records()[prmw->CurRecIndex()].hvo == m_vdfe[0]->GetOwner())
					return true; // No change.
				// Otherwise our record has changed, so we need to redisplay the record.
				CloseAllEditors();
			}
			if (chvo)
				SetRootObj(prmw->Records()[prmw->CurRecIndex()], true);
			::InvalidateRect(m_hwnd, NULL, false);
		}
		return true;

	default:
		break;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Clear out the window contents, and especially any active database connections, when the
	project is (about to be) changed. Return true if the close is OK.
	@return true if the close is OK.
----------------------------------------------------------------------------------------------*/
bool CleDeSplitChild::CloseProj()
{
	// Close current editor if there is one.
	if (!CloseEditor())
		return false;
	if (m_fNeedSync)
	{
		// Notify other apps of change.
		CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
		Assert(pcmw);
		SyncInfo sync(ksyncPossList, pcmw->GetRootObj(), 0);
		m_qlpi->StoreSync(sync);
	}
	return SuperClass::CloseProj();
}
