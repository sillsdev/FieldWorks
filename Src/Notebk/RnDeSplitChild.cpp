/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, SIL International. All rights reserved.

File: RnDeSplitChild.cpp
Responsibility: Ken Zook
Last reviewed: never

Description:
	This class provides the data entry Research Notebook Analysis functions.
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
BEGIN_CMD_MAP(RnDeSplitChild)
// RnMainWnd::OnIdle creates a CmdState with RnMainWnd as the qcmh. Since we are in a different
// class here, we use ON_CID_CHILD so that these will be executed in the same frame window.
// Note: We can't call RecMainWnd::CmsHaveRecord from here, no matter which ON_CID_xx
// we try. So kludge it bu passing through our own, which calls it.
	ON_CID_CHILD(kcidInsSubentEvent, &RnDeSplitChild::CmdInsertSubentry,
		&RnDeSplitChild::CmsInsertSubentry)
	ON_CID_CHILD(kcidInsSubentAnal, &RnDeSplitChild::CmdInsertSubentry,
		&RnDeSplitChild::CmsInsertSubentry)
	ON_CID_CHILD(kcidExpParticipants, &RnDeSplitChild::CmdExpContextMenu, NULL)
	ON_CID_CHILD(kcidEditDel, &RnDeSplitChild::CmdDelete, NULL)
	//ON_CID_CHILD(kcidParticipants, &RnDeSplitChild::CmdShowParticipant, NULL)
	ON_CID_CHILD(kcidEditRoles, &RnDeSplitChild::CmdEditRoles, NULL)
	ON_CID_CHILD(kcidFindInDictionary, &RnDeSplitChild::CmdFindInDictionary, &RnDeSplitChild::CmsFindInDictionary)
END_CMD_MAP_NIL()


//:>********************************************************************************************
//:>    RnDeSplitChild Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnDeSplitChild::RnDeSplitChild()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
RnDeSplitChild::~RnDeSplitChild()
{
}

/*----------------------------------------------------------------------------------------------
	Open up the List Editor on the current list and scroll down to the selected item.
----------------------------------------------------------------------------------------------*/
bool RnDeSplitChild::CmdEditRoles(Cmd * pcmd)
{
	IFwToolPtr qft;
	try
	{
		MSG message;
		if (::PeekMessage(&message, NULL, WM_PAINT, WM_PAINT, PM_REMOVE))
			::DispatchMessage(&message);
		WaitCursor wc;
		CLSID clsid;
		StrUni stu(kpszCleProgId);

		CheckHr(::CLSIDFromProgID(stu.Chars(), &clsid));
		// See if already running.
		IRunningObjectTablePtr qrot;
		CheckHr(::GetRunningObjectTable(0, &qrot));
		IMonikerPtr qmnk;
		CheckHr(::CreateClassMoniker(clsid, &qmnk));
		IUnknownPtr qunk;
		if (SUCCEEDED(qrot->GetObject(qmnk, &qunk)))
			qunk->QueryInterface(IID_IFwTool, (void **)&qft);
		// If not start it up.
		if (!qft)
			qft.CreateInstance(clsid);
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
		AssertPtr(prmw);
		AfLpInfo * plpi = prmw->GetLpInfo();
		AssertPtr(plpi);
		AfDbInfo * pdbi = plpi->GetDbInfo();
		AssertPtr(pdbi);

		// Always save the database prior to opening the list editor to avoid locks. Locks can
		// happen even when the user doesn't intentionally modify the database (e.g., UserViews
		// are written the first time a ListEditor is opened on a newly created database.)
		prmw->SaveData();

		long htool;
		int nPid;
		Vector<HVO> vhvo;
		// We don't want to pass in a flid array, but if we try to pass in a null
		// vector, the marshalling process complains. So we need to use this kludge
		// to get it to work.
		int flidKludge;
		int nView = -1; // Default to data entry.

		int hvopssl = plpi->GetPsslIds()[RnLpInfo::kpidPsslRol];
		vhvo.Push(hvopssl);
		PossListInfoPtr qpli;
		plpi->LoadPossList(hvopssl, plpi->AnalWs(), &qpli);
		Assert(qpli);

		CheckHr(qft->NewMainWndWithSel((wchar *)pdbi->ServerName(), (wchar *)pdbi->DbName(),
			plpi->GetLpId(), qpli->GetPsslId(), qpli->GetWs(), 0, 0,
			vhvo.Begin(), vhvo.Size(), &flidKludge, 0, 0, nView,
			&nPid,
			&htool)); // value you can pass to CloseMainWnd if you want.
		// Note that on Windows 2000, the list editor CANNOT do this for itself. If it isn't
		// already open, it comes to the front automatically, but if it is already running,
		// NOTHING it can do will fix things except for the current foreground application
		// to do this.
		::SetForegroundWindow((HWND)htool);
	}
	catch (...)
	{
		StrApp str(kstidCannotLaunchListEditor);
		::MessageBox(m_hwnd, str.Chars(), NULL, MB_OK | MB_ICONSTOP);
		return true;
	}
	return true;
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
	@param fAlwaysVisible If true, show field regardless of pfsp->m_eVisibility
----------------------------------------------------------------------------------------------*/
void RnDeSplitChild::AddField(HVO hvoRoot, int clid, int nLev, FldSpec * pfsp,
	CustViewDa * pcvd, int & idfe, int nInd, bool fAlwaysVisible)
{
	AssertPtr(pcvd);
	AssertPtr(pfsp);

	bool fCheckMissingData = fAlwaysVisible ? false : pfsp->m_eVisibility == kFTVisIfData;

	AfStatusBarPtr qstbr = MainWindow()->GetStatusBarWnd();
	Assert(qstbr);

	if (pfsp->m_eVisibility == kFTVisNever)
		return;
	Assert(m_wsUser);

	switch(pfsp->m_ft)
	{
	case kftExpandable:
	case kftSubItems:
		{
			int chvoSub;
			AfDeFeTreeNode * pdetn;
			ITsStringPtr qtss;
			CheckHr(pcvd->get_VecSize(hvoRoot, pfsp->m_flid, &chvoSub));
			if (pfsp->m_flid == kflidRnEvent_Participants)
			{
				// Special processing for RnRoledPartic fields.
				if (!chvoSub && fCheckMissingData)
					return; // Nothing to show.

				RnMainWnd * prnmw = dynamic_cast<RnMainWnd *>(MainWindow());
				RnLpInfo * plpi = dynamic_cast<RnLpInfo *>(prnmw->GetLpInfo());
				Vector<HVO> vpsslIds = plpi->GetPsslIds();
				HVO vpsslPeople = vpsslIds[RnLpInfo::kpidPsslPeo];
				Vector<HVO> vhvo;
				HVO pssRole;
				bool fMissing = true;
				int ihvo;
				// Get a list of RnRoledPartic, with the top one being the one with
				// no role. If there is none with no role, add a dummy id to fake it out.
				for (ihvo = 0; ihvo < chvoSub; ++ihvo)
				{
					HVO hvo;
					CheckHr(pcvd->get_VecItem(hvoRoot, kflidRnEvent_Participants, ihvo, &hvo));
					CheckHr(pcvd->get_ObjectProp(hvo, kflidRnRoledPartic_Role, &pssRole));
					if (pssRole)
						vhvo.Push(hvo);
					else
					{
						vhvo.Insert(0, hvo);
						fMissing = false;
					}
				}
				if (fMissing)
					vhvo.Insert(0, khvoDummyRnRoledPartic); // Put in a dummy unspecified RnRoledPartic.

				// Use Participants for the unspecified editor label.
				AfUtil::GetResourceTss(kstidTlsOptParticipants, m_wsUser, &qtss);
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				// Add fields for each item, dependent on expansion options.
				for (ihvo = 0; ihvo < vhvo.Size(); ++ihvo)
				{
					if (ihvo > 0 && !pfsp->m_fExpand && !fAlwaysVisible)
						break; // Only show the first RnRoledPartic.
					HVO hvoPart = vhvo[ihvo];
					CheckHr(pcvd->get_ObjectProp(hvoPart, kflidRnRoledPartic_Role, &pssRole));
					int nIndT = nInd;
					RnDeFeRoleParts * pderp = NewObj RnDeFeRoleParts;
					// Use the name of the role item as the label for the field.
					// The unspecified label defaults to 'Participants'.
					if (pssRole)
					{
						PossItemInfo * ppii;
						plpi->GetPossListAndItem(pssRole, m_wsUser, &ppii, NULL);
						StrUni stuName;  // the Role's name
						ppii->GetName(stuName, kpntName);
						ITsStringPtr qtssName;  // the Role's name
						qtsf->MakeStringRgch(stuName.Chars(), stuName.Length(), m_wsUser,
							&qtssName);

						ITsIncStrBldrPtr qtisb;
						qtisb.CreateInstance(CLSID_TsIncStrBldr);

						// Add the first substring.
						qtisb->AppendTsString(pfsp->m_qtssHelp);

						// Add the second substring.
						StrUni stuTemp;
						stuTemp.Load(kstidRnRoledPartic_HelpA);
						qtisb->AppendRgch(stuTemp.Chars(), stuTemp.Length());

						// Turn bold on.
						qtisb->SetIntPropValues(ktptBold, ktpvEnum, kttvForceOn);
						// Add the Role's name
						qtisb->AppendRgch(stuName.Chars(), stuName.Length());
						// Turn bold off.
						qtisb->SetIntPropValues(ktptBold, ktpvEnum, kttvOff);

						// Add the third substring.
						stuTemp.Load(kstidRnRoledPartic_HelpB);
						qtisb->AppendRgch(stuTemp.Chars(), stuTemp.Length());

						// Get the completed TsString.
						ITsStringPtr qtssHelp;
						qtisb->GetString(&qtssHelp);

						++nIndT;
						pderp->Initialize(hvoPart, kflidRnRoledPartic_Participants, nIndT,
							qtssName, qtssHelp, this, pfsp);
					}
					else
					{
						pderp->Initialize(hvoPart, kflidRnRoledPartic_Participants, nIndT,
							qtss, pfsp->m_qtssHelp, this, pfsp);
					}
					pderp->Init(vpsslPeople, hvoRoot, kflidRnEvent_Participants, pssRole,
						pfsp->m_fHier, pfsp->m_pnt);
					// The tree state may be one of three values.
					DeTreeState dts = kdtsFixed;
					if (!ihvo)
					{
						if ((pfsp->m_fExpand || fAlwaysVisible) && vhvo.Size() > 1)
							dts = kdtsExpanded;
						else if (vhvo.Size() > 1)
							dts = kdtsCollapsed;
					}
					pderp->SetExpansion(dts);
					m_vdfe.Insert(idfe++, pderp);
				}
			}
			else
			{
				// Add a line for each subrecord.
				// This is automatically invisible if there are none.
				for (int ihvoSub = 0; ihvoSub < chvoSub; ++ihvoSub)
				{
					HVO hvoSub;
					int clsid;
					CheckHr(pcvd->get_VecItem(hvoRoot, pfsp->m_flid, ihvoSub, &hvoSub));
					CheckHr(pcvd->get_IntProp(hvoSub, kflidCmObject_Class, &clsid));
					int stid;
					switch (clsid)
					{
					case kclidRnEvent:
						stid = kstidEvent;
						break;
					case kclidRnAnalysis:
						stid = kstidAnalysis;
						break;
					default:
						Assert(false);
						break;
					}
					AfUtil::GetResourceTss(stid, m_wsUser, &qtss);
					pdetn = NewObj AfDeFeTreeNode;
					pdetn->Initialize(hvoRoot, pfsp->m_flid, nInd, qtss,
						pfsp->m_qtssHelp, this, pfsp);
					pdetn->Init();
					pdetn->SetClsid(clsid);
					pdetn->SetTreeObj(hvoSub);
					pdetn->SetExpansion(pfsp->m_fExpand ? kdtsExpanded : kdtsCollapsed);
					SetTreeHeader(pdetn);
					m_vdfe.Insert(idfe++, pdetn);
					// Automatically expand the subrecord if the option is set.
					if (pfsp->m_fExpand)
					{
						ClsLevel clev(clsid, 0);
						idfe = AddFields(hvoSub, clev, pcvd, idfe, nInd + 1);
					}
					qstbr->StepProgressBar(10);
				}
			}
		}
		return;

	case kftObjRefAtomic:
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
			AssertPtr(m_qlpi);
			AfDbInfo * pdbi = m_qlpi->GetDbInfo();
			AssertPtr(pdbi);
			RnRecVc * prrvc = NewObj RnRecVc;
			prrvc->SetDbInfo(pdbi);
			AfDeFeRefs * pdfr = NewObj AfDeFeRefs;
			pdfr->Initialize(hvoRoot, pfsp->m_flid, nInd, pfsp->m_qtssLabel,
				pfsp->m_qtssHelp, this, pfsp);
			pdfr->Init(prrvc, fMultiRefs);
			prrvc->Release(); // The AfDeFeRefs class holds the single reference count.
			m_vdfe.Insert(idfe++, pdfr);
		}
		return;
	}

	// Standard fields are all handled in the superclass.
	SuperClass::AddField(hvoRoot, clid, nLev, pfsp, pcvd, idfe, nInd, fAlwaysVisible);
	return;
}


/*----------------------------------------------------------------------------------------------
	Set the title of the tree node.

	@param pden Node of the tree to be whose header is to be set.
----------------------------------------------------------------------------------------------*/
void RnDeSplitChild::SetTreeHeader(AfDeFeNode * pden)
{
	AssertPtr(pden);

	ITsStringPtr qtss;
	StrUni stu;
	HVO hvo = pden->GetTreeObj();
	Assert(hvo); // This needs to be set prior to calling this.
	CustViewDaPtr qcvd;
	m_qlpi->GetDataAccess(&qcvd);

	switch (pden->GetOwnerFlid())
	{
	case kflidRnGenericRec_SubRecords:
		CheckHr(qcvd->get_StringProp(hvo, kflidRnGenericRec_Title, &qtss));
		AssertPtr(qtss);
		if (pden->GetOutlineSty() != konsNone)
		{
			bool fFinalDot = pden->GetOutlineSty() == konsNumDot;
			HRESULT hr;
			SmartBstr sbstr;
			IgnoreHr(hr = qcvd->GetOutlineNumber(hvo, kflidRnGenericRec_SubRecords, fFinalDot, &sbstr));
			stu = sbstr.Chars();
			if (!FAILED(hr))
			{
				// Insert the number in front of the title.
				stu.Append(L"  ");
				ITsStrBldrPtr qtsb;
				qtss->GetBldr(&qtsb);
				Assert(qtsb);
				qtsb->ReplaceRgch(0, 0, stu.Chars(), stu.Length(), NULL);
				qtsb->GetString(&qtss);
			}
		}
		break;
	default:
		Assert(false);
		break;
	}
	pden->SetContents(qtss);
}


/*----------------------------------------------------------------------------------------------
	Process the right+click delete.
	@param pcmd Menu command
	@return true if sucessful
----------------------------------------------------------------------------------------------*/
bool RnDeSplitChild::CmdDelete(Cmd * pcmd)
{
	AssertObj(pcmd);
	HVO hvo = 0;
	// If we are coming from a right-click menu, we get the field info for the field
	// over which the user right-clicked. If it is a subentry node, assume we delete the
	// subentry.
	if (pcmd->m_rgn[0] == kPopupMenu)
	{
		AfDeFieldEditor * pdfe = m_vdfe[m_idfe];
		AfDeFeTreeNode * pdetn = dynamic_cast<AfDeFeTreeNode *>(pdfe);
		if (pdetn && pdetn->GetOwnerFlid() == kflidRnGenericRec_SubRecords)
			hvo = pdetn->GetTreeObj();
		else if (pdfe)
			hvo = pdfe->GetOwner();
	}
	else if (m_pdfe)
		hvo = m_pdfe->GetOwner();

	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(prmw);

	if (!hvo)
	{
		// Get the current record we are working with.
		HvoClsidVec & vhcRecords = prmw->Records();
		int ihvoCurr = prmw->CurRecIndex();
		hvo = vhcRecords[ihvoCurr].hvo;
	}

	RnMainWnd * prnmw = dynamic_cast<RnMainWnd *>(MainWindow());
	AssertPtr(prnmw);
	return prnmw->CmdDelete1(pcmd, hvo);
}


/*----------------------------------------------------------------------------------------------
	Respond to the Insert subentry menu item.
	Inserts the appropriate subentry at the end of the current entry or subentry. The current
	entry or subentry is determined by the active field editor or the location of the mouse
	for a right-click menu. The new subentry is expanded and the cursor is inserted in the
	first editable field, which is scrolled into view.
	@param pcmd Menu command
	@return true if sucessful
----------------------------------------------------------------------------------------------*/
bool RnDeSplitChild::CmdInsertSubentry(Cmd * pcmd)
{
	AssertObj(pcmd);

	bool fPopupMenu = pcmd->m_rgn[0] == kPopupMenu;
	int nInd = 0; // Indent for the current editor.
	int idfe = 0; // Index for the current editor.

	// If we have a current editor
	if (m_pdfe)
	{
		if (!fPopupMenu)
		{
			// Save index and indent for the current editor before closing.
			nInd = m_pdfe->GetIndent();
			for (idfe = 0; idfe < m_vdfe.Size(); ++idfe)
				if (m_vdfe[idfe] == m_pdfe)
					break;
		}
		// Now close the current editor or abort if we can't.
		if (!CloseEditor())
			return false;
	}

	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(prmw);
	AfViewBarShellPtr qvwbrs = prmw->GetViewBarShell();
	AssertPtr(qvwbrs);
	AfLpInfo * plpi = prmw->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	Set<int> sisel;
	qvwbrs->GetSelection(prmw->GetViewbarListIndex(kvbltSort), sisel);
	int isort = -1;
	if (sisel.Size())
	{
		Assert(sisel.Size() == 1);
		isort = pdbi->ComputeSortIndex(*sisel.Begin(), prmw->GetRecordClid());
	}
	if (isort >= 0)
	{
		AppSortInfo & asi = pdbi->GetSortInfo(isort);
		if (asi.m_fIncludeSubfields || asi.m_fMultiOutput)
		{
			if (!prmw->EnsureSafeSort())
				return false;
		}
	}

	// If we are coming from a right-click menu, we get the field info for the field
	// over which the user right-clicked. If it is a subentry node, assume we add to the
	// subentry.
	if (fPopupMenu)
	{
		AfDeFeTreeNode * pdetn = dynamic_cast<AfDeFeTreeNode *>(m_vdfe[m_idfe]);
		if (pdetn && pdetn->GetOwnerFlid() == kflidRnGenericRec_SubRecords)
		{
			if (pdetn->GetExpansion() == kdtsCollapsed)
				ToggleExpansion(m_idfe);
			idfe = m_idfe + 1;
			nInd = pdetn->GetExpansion() + 1;
		}
		else
		{
			idfe = m_idfe;
			nInd = m_vdfe[idfe]->GetIndent();
		}
	}

	int iclid;		// Class id of new object
	int iflid = kflidRnGenericRec_SubRecords;		// Owning flid of new object
	int itype = kcptOwningSequence;		// attr type.
	int ikstidUR;	// string res id for undo/redo ("Insert xxx")
	int ikstid;		// string res ID for new tree label.
	HVO hvoOwner;	// HVO of owner
	int iOwnerClid;	// clsid of owner.

	if (pcmd->m_cid == kcidInsSubentEvent)
	{
		iclid = kclidRnEvent;
		ikstidUR = kstidEventSubentry;
		ikstid = kstidEvent;
	}
	else
	{
		iclid = kclidRnAnalysis;
		ikstidUR = kstidAnalSubentry;
		ikstid = kstidAnalysis;
	}

	// Set hvoOwner to the major entry or subentry into which we are inserting.
	if (nInd)
		hvoOwner = m_vdfe[idfe]->GetOwner();
	else
	{
		// Get the current record we are working with.
		HvoClsidVec & vhcRecords = prmw->Records();
		int ihvoCurr = prmw->CurRecIndex();
		hvoOwner = vhcRecords[ihvoCurr].hvo;
	}

	// Get class of owner.
	CustViewDaPtr qcvd = prmw->MainDa();
	Assert(qcvd);
	qcvd->get_IntProp(hvoOwner, kflidCmObject_Class, &iOwnerClid);

	return InsertSubItem(iOwnerClid, iflid, kflidRnGenericRec_DateModified,
			itype, hvoOwner, iclid, ikstidUR, ikstid, nInd);
}

/*----------------------------------------------------------------------------------------------
	Enable/disable the Insert Entry menu item.

	@param cms menu command state
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool RnDeSplitChild::CmsInsertSubentry(CmdState & cms)
{
	RnMainWnd * prnmw = dynamic_cast<RnMainWnd *>(MainWindow());
	AssertPtr(prnmw);
	AfMdiClientWndPtr pmdic = prnmw->GetMdiClientWnd();
	AssertPtr(pmdic);
	AfLpInfo * plpi = prnmw->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	AfClientWnd * pafcw = dynamic_cast<AfClientWnd *>(pmdic->GetCurChild());
	AssertPtr(pafcw);
	int iview = pmdic->GetChildIndexFromWid(pafcw->GetWindowId());
	UserViewSpecVec & vuvs = pdbi->GetUserViewSpecs();
	if ((kvwtDoc == vuvs[iview]->m_vwt) || (kvwtBrowse == vuvs[iview]->m_vwt))
	{
		cms.Enable(false);
		return true;
	}
	return prnmw->CmsHaveRecord(cms);
}


/*----------------------------------------------------------------------------------------------
	Returns the RecordSpec and optional FldSpec for the given field.
	We need to override this from AfDeSplitChild to handle RnRoledPartic which don't follow
	standard field editors.
	@param idfe Index of field of interest.
	@param ppfsp Pointer to receive FldSpec associated with the field of interest.
		This is ignored if it is NULL.
	@return The RecordSpec holding the FldSpec associated with the field of interest.
----------------------------------------------------------------------------------------------*/
RecordSpec * RnDeSplitChild::GetRecordSpec(int idfe, FldSpec ** ppfsp)
{
	Assert(idfe < m_vdfe.Size());
	RnDeFeRoleParts * pderp = dynamic_cast<RnDeFeRoleParts *>(m_vdfe[idfe]);
	if (!pderp)
		return SuperClass::GetRecordSpec(idfe, ppfsp);

	// For participants, we need to take a different strategy.
	CustViewDaPtr qcvd;
	m_qlpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	ClsLevel clevOwn;
	clevOwn.m_clsid = kclidRnEvent;
	clevOwn.m_nLevel = 0;
	RecordSpecPtr qrsp;
	m_quvs->m_hmclevrsp.Retrieve(clevOwn, qrsp);
	AssertPtr(qrsp);
	if (ppfsp)
	{
		FldSpec * pfsp = pderp->GetFldSpec();
#if DEBUG
		// Make sure we have the right RecordSpec
		int ibsp;
		for (ibsp = qrsp->m_vqbsp.Size(); --ibsp >= 0; )
		{
			if (qrsp->m_vqbsp[ibsp] == pfsp)
				break;
		}
		Assert(ibsp >= 0);
#endif
		*ppfsp = pfsp;
	}
	return qrsp;
}


/*----------------------------------------------------------------------------------------------
	Toggle the expansion state of a tree node.
	@param idfe Index of the field editor node we are toggling.
----------------------------------------------------------------------------------------------*/
void RnDeSplitChild::ToggleExpansion(int idfe)
{
	AfDeFeTreeNode * pdetn = dynamic_cast<AfDeFeTreeNode *>(FieldAt(idfe));
	if (pdetn)
	{
		// Handle tree nodes as usual.
		SuperClass::ToggleExpansion(idfe);
		return;
	}

	// We have a participants field.
	RnDeFeRoleParts * pderp = dynamic_cast<RnDeFeRoleParts *>(FieldAt(idfe));
	Assert(pderp);
	DeTreeState dts = pderp->GetExpansion();
	pderp->SetExpansion(dts == kdtsExpanded ? kdtsCollapsed : kdtsExpanded);

	if (dts == kdtsCollapsed)
	{
		// Get the main custom view DA shared by all windows
		CustViewDaPtr qcvd;
		m_qlpi->GetDataAccess(&qcvd);
		HVO hvo = GetDragObject(FieldAt(idfe)); // Get the owner.
		int clid;
		CheckHr(qcvd->get_ObjClid(hvo, &clid));
		int nInd = pderp->GetIndent();
		int idfeT = idfe;
		FldSpecPtr qfsp = pderp->GetFldSpec(); // Save this before we delete it.

		// Delete the collapsed node.
		m_vdfe[idfe]->OnReleasePtr();
		m_vdfe[idfe]->Release();
		m_vdfe.Delete(idfe);

		// Force all nodes to show.
		AddField(hvo, clid, 0, qfsp, qcvd, idfeT, nInd, true);
	}
	else
	{
		// Release all indented field editors, if there are any.
		if (idfe < m_vdfe.Size() - 1)
		{
			int idfeLast = LastFieldAtSameIndent(idfe + 1);
			for (; idfeLast > idfe; --idfeLast)
			{
				if (m_vdfe[idfe])
				{
					m_vdfe[idfeLast]->OnReleasePtr();
					m_vdfe[idfeLast]->Release();
				}
				m_vdfe.Delete(idfeLast);
			}
		}
		// Set the expansion to fixed or contracted, depending on whether we have any
		// remaining roles.
		CustViewDaPtr qcvd;
		m_qlpi->GetDataAccess(&qcvd);
		int chvo;
		HVO hvoEvent = pderp->GetOwner();
		dts = kdtsFixed;
		CheckHr(qcvd->get_VecSize(hvoEvent, kflidRnEvent_Participants, &chvo));
		for (int ihvo = 0; ihvo < chvo; ++ihvo)
		{
			HVO hvoPart;
			CheckHr(qcvd->get_VecItem(hvoEvent, kflidRnEvent_Participants, ihvo, &hvoPart));
			HVO hvoPss;
			CheckHr(qcvd->get_ObjectProp(hvoPart, kflidRnRoledPartic_Role, &hvoPss));
			if (!hvoPss)
				continue; // Skip the unspecified item.
			dts = kdtsCollapsed;
			break;
		}
		pderp->SetExpansion(dts);
	}
}


/*----------------------------------------------------------------------------------------------
	This needs to return a valid flid where we can place an object of the specified
	class in the object with the specified id.
	@param hvoDstOwner The object id of the object holding flid.
	@param clid The class id of the object we want to insert into flid.
	@param pptss Pointer to receive the string label name for this field.
	@return The flid in hvoDstOwner where we are inserting the new object.
----------------------------------------------------------------------------------------------*/
int RnDeSplitChild::GetDstFlidAndLabel(HVO hvoDstOwner, int clid, ITsString ** pptss)
{
	AssertPtr(pptss);
	// For now we only insert subrecords.
	Assert(clid == kclidRnAnalysis || clid == kclidRnEvent);
	Assert(m_wsUser);
	AfUtil::GetResourceTss(clid == kclidRnEvent ? kstidEvent : kstidAnalysis, m_wsUser, pptss);
	return kflidRnGenericRec_SubRecords;
}


/*----------------------------------------------------------------------------------------------
	Override to provide modified flid.

	@param clid Class id that may have a modified date.
	@return Modified flid for clid, or zero.
----------------------------------------------------------------------------------------------*/
int RnDeSplitChild::GetOwnerModifiedFlid(int clid)
{
	switch (clid)
	{
	default:
		return 0;
		break;
	case kclidRnEvent:	// Fall through.
	case kclidRnAnalysis:
		return kflidRnGenericRec_DateModified;
		break;
	}
}


/*----------------------------------------------------------------------------------------------
	Find a field editor for the given owner and flid (if it exists) and update that field, plus
	dependent fields.

	@param hvoOwn Owner of object
	@param flid Field ID
	@param hvoNode tree node
----------------------------------------------------------------------------------------------*/
void RnDeSplitChild::UpdateField(HVO hvoOwn, int flid, HVO hvoNode)
{
	Assert(hvoOwn);
	Assert(flid);

	SuperClass::UpdateField(hvoOwn, flid, hvoNode);

	// The title affects a subentry node title, so update these.
	// Note, the node title may be visible even though the title isn't (e.g., contracted).
	if (flid != kflidRnGenericRec_Title)
		return;
	for (int idfe = m_vdfe.Size(); --idfe >= 0; )
	{
		AfDeFeTreeNode * pdetn = dynamic_cast<AfDeFeTreeNode *>(m_vdfe[idfe]);
		if (!pdetn)
			continue;
		if (pdetn->GetTreeObj() == hvoOwn)
		{
			SetTreeHeader(pdetn);
			::InvalidateRect(m_hwnd, NULL, false);
			break;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Replace the current record in the window with hvo, as long as it is in the current filter.
	Otherwise open a new window to show the record.
	@param Non-null id of the object we want to make the main item in the current window.
----------------------------------------------------------------------------------------------*/
void RnDeSplitChild::JumpItem(HVO hvo)
{
	Assert(hvo);
	// At some point we want to enable the jump. But before doing that
	// we need to come up with an easy way to get back to the location from which
	// we jumped. Since that presents some challenges, for now we are just opening
	// a new window
	/*
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	HvoClsidVec & vhcRecords = prmw->Records();
	for (int ihc = vhcRecords.Size(); --ihc >= 0; )
	{
		if (vhcRecords[ihc].hvo == hvo)
		{
			prmw->SetCurRecIndex(ihc);
			GetSplitterClientWnd()->DispCurRec(NULL, 0); // Switch to the new record.
			return;
		}
	}
	// The indicated record isn't in the current filter, so open a new window.
	*/
	LaunchItem(hvo);
}


/*----------------------------------------------------------------------------------------------
	Open a new window with hvo as the current record.
	@param Non-null id of the object we want to display in a new window.
----------------------------------------------------------------------------------------------*/
void RnDeSplitChild::LaunchItem(HVO hvo)
{
	Assert(hvo);
	// At the moment we are assuming hvo is an RnGenericRec. At some point this will
	// probably change, which will require additional code.

	Vector<HVO> vhvo;
	Vector<int> vflid;
	int nView;

	// Set up the path to hvo.
	vhvo.Push(hvo);
	// Add owning records, if there are any, until we reach the main record.
	CustViewDaPtr qcvd;
	m_qlpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	RnLpInfoPtr qrlpi = dynamic_cast<RnLpInfo *>(m_qlpi.Ptr());
	AssertPtr(qrlpi);
	HVO hvoOwn;
	while (hvo)
	{
		CheckHr(qcvd->get_ObjOwner(hvo, &hvoOwn));
		hvo = hvoOwn;
		if (hvo == qrlpi->GetRnId() || !hvo)
			break;
		vhvo.Insert(0, hvo);
		vflid.Insert(0, kflidRnGenericRec_SubRecords);
	}

	// Get the current view.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);
	AfViewBarShell * pvwbrs = prmw->GetViewBarShell();
	AssertPtr(pvwbrs);
	Set<int> sisel;
	pvwbrs->GetSelection(prmw->GetViewbarListIndex(kvbltView), sisel);
	Assert(sisel.Size() == 1);
	nView = *sisel.Begin();
	//nView = 2; // JohnT: Enable this for selection test.

	WndCreateStruct wcs;
	wcs.InitMain(_T("RnMainWnd"));

	prmw->PrepareNewWindowLocation();

	RnMainWndPtr qrnmw;
	qrnmw.Create();
	qrnmw->Init(m_qlpi);
	qrnmw->SetStartupInfo(vhvo.Begin(), vhvo.Size(), vflid.Begin(), vflid.Size(), 0, nView);
	qrnmw->CreateHwnd(wcs);
}


/*----------------------------------------------------------------------------------------------
	Add menu items for the right-click context Insert menu over tree pane.
----------------------------------------------------------------------------------------------*/
void RnDeSplitChild::AddContextInsertItems(HMENU & hmenu)
{
	StrApp str;
	str.Load(kstidContextEventSub);
	::AppendMenu(hmenu, MF_STRING, kcidInsSubentEvent, str.Chars());
	str.Load(kstidContextAnalSub);
	::AppendMenu(hmenu, MF_STRING, kcidInsSubentAnal, str.Chars());
}


/*----------------------------------------------------------------------------------------------
	Add menu items for the right-click context Show menu over tree pane.
	@param hmenu Handle to the menu in which to add the items.
----------------------------------------------------------------------------------------------*/
void RnDeSplitChild::AddContextShowItems(HMENU & hmenu)
{
	CustViewDaPtr qcvd;
	m_qlpi->GetDataAccess(&qcvd);
	HVO hvo = FieldAt(m_idfe)->GetOwner();
	int clid;
	CheckHr(qcvd->get_ObjClid(hvo, &clid));
	if (clid != kclidRnEvent)
		return; // Nothing to add.

	// See if we have any existing participants being shown. If so, and if expanded,
	// we need to go through these to build our vector of roles. Otherwise we go through
	// the cache to build our vector of roles
	int idfe = FirstFieldOfTreeNode(m_idfe);
	// We need to use the indent from the first field, not the current field, since the
	// current field (e.g., RnEvent_Participants) may have a phony indent.
	int nInd = m_vdfe[idfe]->GetIndent();
	int cdfe = m_vdfe.Size();
	DeTreeState dts = kdtsFixed;
	bool fFound = false;
	for (; idfe < cdfe; ++idfe)
	{
		AfDeFieldEditor * pdfe = m_vdfe[idfe];
		int nIndT = pdfe->GetIndent();
		if (nIndT > nInd)
			continue; // Skip nested fields.
		if (nIndT < nInd)
			break; // We are out of this subentry.
		RnDeFeRoleParts * pderp = dynamic_cast<RnDeFeRoleParts *>(m_vdfe[idfe]);
		if (pderp)
		{
			dts = pderp->GetExpansion();
			fFound = true;
			break; // We've found one in this subentry.
		}
	}

	// Store vector of participants labels (roles) currently assigned.
	m_vstuParticipants.Clear();
	if (dts == kdtsExpanded)
	{
		// Get all of the following participants. (The first is unspecified.)
		for (idfe = idfe + 1; idfe < cdfe; ++idfe)
		{
			RnDeFeRoleParts * pderp = dynamic_cast<RnDeFeRoleParts *>(m_vdfe[idfe]);
			if (pderp)
			{
				ITsStringPtr qtss;
				pderp->GetLabel(&qtss);
				const wchar * prgch;
				int cch;
				StrUni stu;
				if (SUCCEEDED(qtss->LockText(&prgch, &cch)))
				{
					stu.Assign(prgch, cch);
					CheckHr(qtss->UnlockText(prgch));
				}
				m_vstuParticipants.Push(stu);
			}
		}
	}
	else
	{
		// Participants tree is collapsed, or there is none, so we need to use the
		// cache to build our list of assigned roles.
		int chvo;
		CheckHr(qcvd->get_VecSize(hvo, kflidRnEvent_Participants, &chvo));
		for (int ihvo = 0; ihvo < chvo; ++ihvo)
		{
			HVO hvoPart;
			CheckHr(qcvd->get_VecItem(hvo, kflidRnEvent_Participants, ihvo, &hvoPart));
			HVO hvoPss;
			CheckHr(qcvd->get_ObjectProp(hvoPart, kflidRnRoledPartic_Role, &hvoPss));
			if (!hvoPss)
				continue; // Skip the unspecified item.
			PossItemInfo * ppii;
			Assert(m_wsUser);
			m_qlpi->GetPossListAndItem(hvoPss, m_wsUser, &ppii);
			AssertPtr(ppii);
			StrUni stu;
			ppii->GetName(stu, kpntName);
			m_vstuParticipants.Push(stu);
		}
	}

	StrApp str;
	str.Load(kstidTlsOptParticipants);
	if (fFound)
	{
		// We have at least one item, so show popup menu. At this point we no longer have
		// a Participants menu item since at least one field already exists.
/* For some reason this will not work. It comes up with the right position, but InsertMenu
// is still appending it to the bottom of the list, and it is also disabled and not a popup.

		// We have a block speck to add. First find the appropriate insertion
		// spot so we keep the list sorted.
		int ibsp;
		StrUni stuNew;
		for (ibsp = 0; ibsp < m_vbsp.Size(); ++ibsp)
		{
			ITsStringPtr qtss;
			const wchar * prgch;
			int cch;
			StrApp strT;
			qtss = m_vbsp[ibsp]->m_qtssLabel;
			if (SUCCEEDED(qtss->LockText(&prgch, &cch)))
			{
				strT.Assign(prgch, cch);
				CheckHr(qtss->UnlockText(prgch));
			}
			if (str.Compare(strT) < 0)
				break;
		}

		int cmnu;
		cmnu = GetMenuItemCount(hmenu);
		HMENU hmenuParticipants = ::CreatePopupMenu();
		// The +1 gets us past the dummy item.
		::InsertMenu(hmenu, ibsp + 1, MF_BYPOSITION || MF_POPUP, (uint)hmenuParticipants,
			str.Chars());
		::AppendMenu(hmenuParticipants, MF_STRING, kcidExpParticipants, NULL);
		char rgch[200];
		cmnu = GetMenuItemCount(hmenu);
		for (int i = 0; i < cmnu; ++i)
		{
			::GetMenuString(hmenu, i, rgch, 200, MF_BYPOSITION);
		}
		// May also need to update m_vbsp.
*/
		HMENU hmenuParticipants = ::CreatePopupMenu();
		::AppendMenu(hmenu, MF_POPUP, (uint)hmenuParticipants, str.Chars());
		::AppendMenu(hmenuParticipants, MF_STRING, kcidExpParticipants, NULL);
	}
}


/*----------------------------------------------------------------------------------------------
	This method is used for three purposes.
	1)	If pcmd->m_rgn[0] == AfMenuMgr::kmaExpandItem, it is being called to expand the dummy
		item by adding new items.
	2)	If pcmd->m_rgn[0] == AfMenuMgr::kmaGetStatusText, it is being called to get the status
		bar string for an expanded item.
	3)	If pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand, it is being called because the user
		selected an expandable menu item.

	Expanding items:
		pcmd->m_rgn[1] -> Contains the handle to the menu (HMENU) to add items to.
		pcmd->m_rgn[2] -> Contains the index in the menu where you should start inserting items.
		pcmd->m_rgn[3] -> This value must be set to the number of items that you inserted.
	The expanded items will automatically be deleted when the menu is closed. The dummy
	menu item will be deleted for you, so don't do anything with it here.

	Getting the status bar text:
		pcmd->m_rgn[1] -> Contains the index of the expanded/inserted item to get text for.
		pcmd->m_rgn[2] -> Contains a pointer (StrApp *) to the text for the inserted item.
	If the menu item does not have any text to show on the status bar, return false.

	Performing the command:
		pcmd->m_rgn[1] -> Contains the handle of the menu (HMENU) containing the expanded items.
		pcmd->m_rgn[2] -> Contains the index of the expanded/inserted item to get text for.

	@param pcmd Ptr to menu command

	@return true if we have processed this command.
----------------------------------------------------------------------------------------------*/
bool RnDeSplitChild::CmdExpContextMenu(Cmd * pcmd)
{
	Assert(m_wsUser);

	StrApp strx;
	strx.Format(_T("RnDeSplitChild::CmdExpContextMenu:  rgn0=%d, rgn1=%d, rgn2=%d, rgn3=%d, cid=%d.\n"),
		pcmd->m_rgn[0], pcmd->m_rgn[1], pcmd->m_rgn[2], pcmd->m_rgn[3], pcmd->m_cid);
	OutputDebugString(strx.Chars());

	AssertPtr(pcmd);
	if (pcmd->m_cid != kcidExpParticipants)
		return SuperClass::CmdExpContextMenu(pcmd);

	// We are processing a participants expanded menu.

	// Don't allow menus if we can't close the current editor.
	if (!IsOkToChange())
		return false;

	StrUni stu;
	StrApp str;
	int ma = pcmd->m_rgn[0];
	if (ma == AfMenuMgr::kmaExpandItem)
	{
		// We need to expand the dummy menu item.
		HMENU hmenu = (HMENU)pcmd->m_rgn[1];
		int imni = pcmd->m_rgn[2];
		int & cmniAdded = pcmd->m_rgn[3];

		// Add menu items for all roles items not currently represented by a participants field.
		RnLpInfo * plpi = dynamic_cast<RnLpInfo *>(m_qlpi.Ptr());
		PossListInfoPtr qpli;
		m_hvoMenuRoles.Clear();
		plpi->LoadPossList(plpi->GetPsslIds()[RnLpInfo::kpidPsslRol], m_wsUser, &qpli);
		int cpss = qpli->GetCount();
		Vector<StrUni> vstu; // Used for sorting menu items.
		// Process all items in the roles possibility list.
		for (int ipss = 0; ipss < cpss; ++ipss)
		{
			int cstu = m_vstuParticipants.Size();
			PossItemInfo * ppii = qpli->GetPssFromIndex(ipss);
			ppii->GetName(stu, kpntName);
			int istu;
			// See if this item already has a field.
			for (istu = 0; istu < cstu; ++istu)
			{
				if (stu.Equals(m_vstuParticipants[istu]))
					break; // This item already has a field.
			}
			if (istu < cstu)
				continue; // This one is already assigned.

			// We have an unassigned item, so we need to add a menu item for this one.
			// First, we need to figure out where to insert the item.
			cstu = vstu.Size();
			for (istu = 0; istu < cstu; ++istu)
			{
				if (stu.Compare(vstu[istu]) < 0)
					break; // We have our insertion point.
			}
			m_hvoMenuRoles.Insert(istu, ppii->GetPssId());
			vstu.Insert(istu, stu);
		}

		cmniAdded = vstu.Size();
		// Now that we have a sorted list, add corresponding menu items.
		int istu;
		for (istu = 0; istu < cmniAdded; istu++)
		{
			str.Assign(vstu[istu]);
			::InsertMenu(hmenu, imni + istu, MF_BYPOSITION, kcidMenuItemDynMin + istu,
				str.Chars());
		}
		// Add last menu item ... "Edit Roles List..."
		::AppendMenu(hmenu, MF_SEPARATOR, 0, NULL);
		cmniAdded = istu;
		str.Load(kstidEditRoles);
		::AppendMenu(hmenu, MF_BYCOMMAND, kcidEditRoles, str.Chars());
		return true;
	}
	else if (ma == AfMenuMgr::kmaGetStatusText)
	{
		// We need to return the text for the expanded menu item.
		//    m_rgn[1] holds the index of the selected item.
		//    m_rgn[2] holds a pointer to the string to set

		int imni = pcmd->m_rgn[1];
		StrApp * pstr = (StrApp *)pcmd->m_rgn[2];
		AssertPtr(pstr);

		// Append the name of the role to the standard string, and return the result.
		if (!m_hvoMenuRoles.Size())
			return false; // Mouse is outside of the menu.
		str.Load(kstidShowBody);
		PossItemInfo * ppii;
		m_qlpi->GetPossListAndItem(m_hvoMenuRoles[imni], m_wsUser, &ppii);
		AssertPtr(ppii);
		ppii->GetName(stu, kpntName);
		str.Append(stu);
		pstr->Assign(str.Chars());
		return true;
	}
	else if (ma == AfMenuMgr::kmaDoCommand)
	{
		// The user selected an expanded menu item, so perform the command now.
		//    m_rgn[1] holds the menu handle.
		//    m_rgn[2] holds the index of the selected item.

		int iitem;
		iitem = pcmd->m_rgn[2];
		// Close active editor before showing a new field.
		if (!CloseEditor())
			return false;

		// Get the name for the selected role.
		PossItemInfo * ppii;
		m_qlpi->GetPossListAndItem(m_hvoMenuRoles[iitem], m_wsUser, &ppii);
		AssertPtr(ppii);
		ppii->GetName(stu, kpntName);
		ITsStringPtr qtss;
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &qtss);

		// Get the record spec for this class.
		CustViewDaPtr qcvd;
		m_qlpi->GetDataAccess(&qcvd);
		HVO hvo = FieldAt(m_idfe)->GetOwner();
		int clid;
		CheckHr(qcvd->get_ObjClid(hvo, &clid));
		ClsLevel clev;
		clev.m_clsid = clid;
		clev.m_nLevel = 0;
		RecordSpecPtr qrsp;
		m_quvs->m_hmclevrsp.Retrieve(clev, qrsp);
		AssertPtr(qrsp);

		// Find the block spec for participants.
		BlockSpec * pbsp;
		for (int ibsp = qrsp->m_vqbsp.Size(); --ibsp >= 0; )
		{
			pbsp = qrsp->m_vqbsp[ibsp];
			if (pbsp->m_flid == kflidRnEvent_Participants)
				break;
		}
		Assert(pbsp);

		// Get the index following the final participants field in the current object.
		// We'll always add the new field to the end of the list.
		int idfe = FirstFieldOfTreeNode(m_idfe);
		// We need to use the indent from the first field, not the current field, since the
		// current field (e.g., RnEvent_Participants) may have a phony indent.
		int nInd = m_vdfe[idfe]->GetIndent();
		int cdfe = m_vdfe.Size();
		RnDeFeRoleParts * pderp;
		for (; idfe < cdfe; ++idfe)
		{
			AfDeFieldEditor * pdfe = m_vdfe[idfe];
			if (pdfe->GetIndent() > nInd)
				continue;
			pderp = dynamic_cast<RnDeFeRoleParts *>(pdfe);
			if (pderp)
				break;
		}
		Assert(idfe < cdfe);
		// If we aren't expanded, we need to expand the fields first.
		if (pderp->GetExpansion() == kdtsCollapsed)
			ToggleExpansion(idfe);
		while (++idfe < cdfe)
		{
			pderp = dynamic_cast<RnDeFeRoleParts *>(m_vdfe[idfe]);
			if (!pderp)
				break;
		}

		// Create and insert the new blank field.
		pderp = NewObj RnDeFeRoleParts;
		Vector<HVO> vpsslIds = m_qlpi->GetPsslIds();
		HVO vpsslPeople = vpsslIds[RnLpInfo::kpidPsslPeo];
		pderp->Initialize(khvoDummyRnRoledPartic, kflidRnRoledPartic_Participants,
			nInd + 1, qtss, pbsp->m_qtssHelp, this, pbsp);
		pderp->Init(vpsslPeople, hvo, kflidRnEvent_Participants, ppii->GetPssId(),
			pbsp->m_fHier, pbsp->m_pnt);
		pderp->SetExpansion(kdtsFixed);
		m_vdfe.Insert(idfe, pderp);
		// Make sure the top RnDeFeRoleParts field is now set to expanded. There should always
		// be at least one participants field above the one we just inserted.
		for (int idfeT = idfe; --idfeT >= 0; )
		{
			pderp = dynamic_cast<RnDeFeRoleParts *>(m_vdfe[idfeT]);
			if (!pderp)
			{
				pderp = dynamic_cast<RnDeFeRoleParts *>(m_vdfe[idfeT + 1]);
				pderp->SetExpansion(kdtsExpanded);
				break;
			}
		}
		ActivateField(idfe);
		return true;
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Check to see if we are editing a new record, if so, update the date modified on the old
	record if changes were made.
----------------------------------------------------------------------------------------------*/
void RnDeSplitChild::BeginEdit(AfDeFieldEditor * pdfe)
{
	HVO hvoNew = pdfe->GetOwner();
	if (m_hvoLastObj && m_hvoLastObj != hvoNew)
	{
		// We've opened an editor on a new object.
		CustViewDaPtr qcvd;
		m_qlpi->GetDataAccess(&qcvd);
		AssertPtr(qcvd);
		int clid;
		// Find out if this is a kind of RnGenericRec.
		CheckHr(qcvd->get_ObjClid(m_hvoLastObj, &clid));
		IFwMetaDataCachePtr qmdc;
		m_qlpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
		AssertPtr(qmdc);
		do
		{
			if (clid == kclidRnGenericRec)
			{
				// The object has a DateModified property, so see that it is current.
				RnMainWnd * prnmw = dynamic_cast<RnMainWnd *>(MainWindow());
				Assert(prnmw);
				prnmw->UpdateDateModified(m_hvoLastObj, kflidRnGenericRec_DateModified);
				break;
			}
			ulong uclid;
			qmdc->GetBaseClsId(clid, &uclid);
			clid = (int)uclid;
		} while (clid != 0);
	}
}


/*----------------------------------------------------------------------------------------------
	Set the startup information this window to the promoted entry.
	@param hvo The id of the entry or subentry that has been promoted.
----------------------------------------------------------------------------------------------*/
void RnDeSplitChild::PromoteSetup(HVO hvo)
{
	Assert(hvo);
	// At the moment we are assuming hvo is an RnGenericRec. At some point this will
	// probably change, which will require additional code.

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
		vflid.Insert(0, kflidRnGenericRec_SubRecords);
	}
	prmw->SetStartupInfo(vhvo.Begin(), vhvo.Size(), vflid.Begin(), vflid.Size(), 0, 0);
}

// Implements the Find In Dictionary command.
bool RnDeSplitChild::CmdFindInDictionary(Cmd * pcmd)
{
	if (m_pdfe)		// without this check, notebook is crashing all over.
		return m_pdfe->CmdFindInDictionary(pcmd);
	else
		return false;
}

// Handle enabling the Find In Dictionary command. For now it is always enabled.
bool RnDeSplitChild::CmsFindInDictionary(CmdState & cms)
{
	if (m_pdfe)		// without this check, notebook is crashing all over.
		return m_pdfe->CmsFindInDictionary(cms);
	else
		return false;
}

void RnDeSplitChild::AddExtraContextMenuItems(HMENU hmenuPopup)
{
	StrApp strLabel(kstidFindInDictionary);
	::AppendMenu(hmenuPopup, MF_STRING, kcidFindInDictionary, strLabel.Chars());
}


//:>********************************************************************************************
//:> RnRecVc methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
	@param fLoadData True if the VC needs to load any data it uses.
----------------------------------------------------------------------------------------------*/
RnRecVc::RnRecVc(bool fLoadData) : SuperClass(fLoadData)
{ }


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
RnRecVc::~RnRecVc()
{
}

static DummyFactory g_fact(_T("SIL.Notebook.RnRecVc"));

/*----------------------------------------------------------------------------------------------
	Load the data needed to display this view. In this case, we need to load the class, owner
	(so we can tell whether it is a subitem), the title, and create date. If all of these are
	already in the cache, don't reload it.
	@param pvwenv Pointer to the view environment.
	@param hvo The id of the object we are displaying.
	@param frag Identifies the part of the view we are currently displaying.
	@return HRESULT indicating success (S_OK), or failure (E_FAIL).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnRecVc::LoadDataFor(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);
	if (!hvo)
		ThrowHr(WarnHr(E_INVALIDARG));

	ISilDataAccessPtr qsda;
	CheckHr(pvwenv->get_DataAccess(&qsda));
	bool fLoaded = false;
	int clid;
	CheckHr(qsda->get_IntProp(hvo, kflidCmObject_Class, &clid));
	if (clid)
	{
		HVO hvoOwn;
		CheckHr(qsda->get_ObjectProp(hvo, kflidCmObject_Owner, &hvoOwn));
		if (hvoOwn)
		{
			int64 tim;
			CheckHr(qsda->get_TimeProp(hvo, kflidRnGenericRec_DateCreated, &tim));
			if (tim)
			{
				ITsStringPtr qtss;
				CheckHr(qsda->get_StringProp(hvo, kflidRnGenericRec_Title, &qtss));
				if (qtss)
				{
					int cch;
					CheckHr(qtss->get_Length(&cch));
					if (cch)
						fLoaded = true;
				}
			}
		}
	}

	if (!fLoaded)
	{
		// If any field is missing from the cache, load everything.
		StrUni stuSql;
		IDbColSpecPtr qdcs;
		IVwOleDbDaPtr qda;
		CheckHr(qsda->QueryInterface(IID_IVwOleDbDa, (void**)&qda));
		stuSql.Format(L"select id, Class$, Owner$, DateCreated, Title, Title_Fmt "
			L"from RnGenericRec_ "
			L"where id = %d", hvo);
		qdcs.CreateInstance(CLSID_DbColSpec);
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctInt, 1, kflidCmObject_Class, 0));
		CheckHr(qdcs->Push(koctObj, 1, kflidCmObject_Owner, 0));
		CheckHr(qdcs->Push(koctTime, 1, kflidRnGenericRec_DateCreated, 0));
		CheckHr(qdcs->Push(koctString, 1, kflidRnGenericRec_Title, 0));
		CheckHr(qdcs->Push(koctFmt, 1, kflidRnGenericRec_Title, 0));

		AfMainWnd * pafw = AfApp::Papp()->GetCurMainWnd();
		AssertPtr(pafw);
		AfStatusBar * pstbr = pafw->GetStatusBarWnd();
		AssertPtr(pstbr);
		bool fProgBar = pstbr->IsProgressBarActive();
		if (!fProgBar)
		{
			StrApp strMsg(kstidStBar_LoadingData);
			pstbr->StartProgressBar(strMsg.Chars(), 0, 70, 1);
		}

		// Execute the query and store results in the cache.
		CheckHr(qda->Load(stuSql.Bstr(), qdcs, hvo, 0, pstbr, FALSE));
		if (!fProgBar)
			pstbr->EndProgressBar();
	}

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	This is the method for displaying the name of a single reference. This view shows the
	name for an RnGenericRec consisting of the type of record, hyphen, title, hyphen,
	creation date. "Subevent - Fishing for pirana - 3/22/2001"
	@param pvwenv Pointer to the view environment.
	@param hvo The id of the object we are displaying.
	@param frag Identifies the part of the view we are currently displaying.
	@return HRESULT indicating success (S_OK), or failure (E_FAIL).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnRecVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	switch (frag)
	{
	case kfrRefName:
	case kfrListName:
		{
			SmartBstr bstrClass = L"UnLoaded";
			ITsStringPtr qtss;
			ITsStringPtr qtssTitle;

			// Make sure data is loaded.
			LoadDataFor(pvwenv, hvo, frag);
			AfMainWnd * pafw = AfApp::Papp()->GetCurMainWnd();
			AssertPtr(pafw);
			RnLpInfo * plpi = dynamic_cast<RnLpInfo *>(pafw->GetLpInfo());
			AssertPtr(plpi);

#define HYPERLINK_CHANGE
#ifdef HYPERLINK_CHANGE
			// Update the string with the new object.
			GUID uid;
			AfDbInfo * pdbi = plpi->GetDbInfo();
			if (!pdbi->GetGuidFromId(hvo, uid))
				ReturnHr(E_FAIL);

			StrUni stuData;
			OLECHAR * prgchData;
			// Make large enough for a guid plus the type character at the start.
			stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
			*prgchData = kodtNameGuidHot;
			memmove(prgchData + 1, &uid, isizeof(uid));

			ITsPropsFactoryPtr qtpf;
			ITsPropsBldrPtr qtpb;
			ITsTextPropsPtr qttp;
			ITsStrFactoryPtr qtsf;

			qtpf.CreateInstance(CLSID_TsPropsFactory);
			CheckHr(qtpf->GetPropsBldr(&qtpb));
			CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, m_qdbi->UserWs()));
			CheckHr(qtpb->SetStrPropValue(ktptObjData, stuData.Bstr()));
			CheckHr(qtpb->GetTextProps(&qttp));
			qtsf.CreateInstance(CLSID_TsStrFactory);
			OLECHAR chObj = kchObject;
			CheckHr(qtsf->MakeStringWithPropsRgch(&chObj, 1, qttp, &qtss));

			CheckHr(pvwenv->OpenSpan());
			int flid = kflidRnGenericRec_Title;
			CheckHr(pvwenv->NoteDependency(&hvo, &flid, 1));
			CheckHr(pvwenv->AddString(qtss)); // The class name.
			CheckHr(pvwenv->CloseSpan());
#else // !HYPERLINK_CHANGE
			int clid;
			HVO hvoOwn;
			int64 ntim;
			int ws = m_qdbi->UserWs();
			ISilDataAccessPtr qsda;
			CheckHr(pvwenv->get_DataAccess(&qsda));
			AssertPtr(qsda);
			CheckHr(qsda->get_IntProp(hvo, kflidCmObject_Class, &clid));
			CheckHr(qsda->get_ObjectProp(hvo, kflidCmObject_Owner, &hvoOwn));
			CheckHr(qsda->get_TimeProp(hvo, kflidRnGenericRec_DateCreated, &ntim));
			CheckHr(qsda->get_StringProp(hvo, kflidRnGenericRec_Title, &qtssTitle));

			int stid;
			if (clid == kclidRnEvent)
			{
				if (plpi->GetRnId() == hvoOwn)
					stid = kstidEvent;
				else
					stid = kstidSubevent;
			}
			else if (clid == kclidRnAnalysis)
			{
				if (plpi->GetRnId() == hvoOwn)
					stid = kstidAnalysis;
				else
					stid = kstidSubanalysis;
			}
			StrUni stu(stid);
			StrUni stuSep(kstidSpHyphenSp);
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, &qtss));

			CheckHr(pvwenv->OpenSpan());
			CheckHr(pvwenv->AddString(qtss)); // The class name.
			CheckHr(qtsf->MakeStringRgch(stuSep.Chars(), stuSep.Length(), ws, &qtss));
			CheckHr(pvwenv->AddString(qtss)); // The separator
			//CheckHr(pvwenv->AddString(qtssTitle)); // The title.
			// The following gives the title of the owning object instead of the ref.
			CheckHr(pvwenv->AddStringProp(kflidRnGenericRec_Title, this)); // The title.
			CheckHr(pvwenv->AddString(qtss)); // The separator
			// Leave the date blank if a date doesn't exist.
			if (ntim)
			{
				// Convert the date to a system date.
				SilTime tim = ntim;
				SYSTEMTIME stim;
				stim.wYear = (unsigned short) tim.Year();
				stim.wMonth = (unsigned short) tim.Month();
				stim.wDay = (unsigned short) tim.Date();

				// Then format it to a time based on the current user locale.
				achar rgchDate[50]; // Tuesday, August 15, 2000		mardi 15 août 2000
				::GetDateFormat(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &stim, NULL, rgchDate, 50);
				stu = rgchDate;
				CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, &qtss));
				CheckHr(pvwenv->AddString(qtss)); // The date.
			}
			CheckHr(pvwenv->CloseSpan());
#endif // HYPERLINK_CHANGE

			break;
		}
	}

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	Return the text string that gets shown to the user when this object needs to be displayed.
	This is the method for displaying the name of a single reference. This view shows the
	name for an RnGenericRec consisting of the type of record, hyphen, title, hyphen,
	creation date. "Subevent - Fishing for pirana - 3/22/2001"

	@param pguid Pointer to a database object's assigned GUID.
	@param pptss Address of a pointer to an ITsString COM object used for returning the text
					string.

	@return S_OK, E_POINTER, or E_FAIL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnRecVc::GetStrForGuid(BSTR bstrGuid, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrGuid);
	ChkComOutPtr(pptss);
	if (BstrLen(bstrGuid) != 8)
		ReturnHr(E_INVALIDARG);

	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(AfApp::Papp()->GetCurMainWnd());
	AssertPtr(prmw);
	AfLpInfo * plpi = prmw->GetLpInfo();
	AssertPtr(plpi);

	HVO hvo = plpi->GetDbInfo()->GetIdFromGuid((GUID *)bstrGuid);

	CustViewDaPtr qcvd;
	plpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	int clid;
	HVO hvoOwn;
	HVO hvoRootObj = prmw->GetRootObj();
	int64 ntim;
	ITsStringPtr qtssTitle;
	CheckHr(qcvd->get_IntProp(hvo, kflidCmObject_Class, &clid));
	CheckHr(qcvd->get_ObjectProp(hvo, kflidCmObject_Owner, &hvoOwn));
	CheckHr(qcvd->get_TimeProp(hvo, kflidRnGenericRec_DateCreated, &ntim));
	CheckHr(qcvd->get_StringProp(hvo, kflidRnGenericRec_Title, &qtssTitle));

	int stid;
	if (clid == kclidRnEvent)
	{
		if (hvoRootObj == hvoOwn)
			stid = kstidEvent;
		else
			stid = kstidSubevent;
	}
	else if (clid == kclidRnAnalysis)
	{
		if (hvoRootObj == hvoOwn)
			stid = kstidAnalysis;
		else
			stid = kstidSubanalysis;
	}
	StrUni stu(stid);
	StrUni stuSep(kstidSpHyphenSp);

	ITsStrFactoryPtr qtsf;
	ITsIncStrBldrPtr qtisb;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	CheckHr(qtsf->GetIncBldr(&qtisb));
	CheckHr(qtisb->Append(stu.Bstr()));

	CheckHr(qtisb->Append(stuSep.Bstr()));
	CheckHr(qtisb->AppendTsString(qtssTitle)); // The title.
	CheckHr(qtisb->Append(stuSep.Bstr()));
	// Leave the date blank if a date doesn't exist.
	if (ntim)
	{
		// Convert the date to a system date.
		SilTime tim = ntim;
		SYSTEMTIME stim;
		stim.wYear = (unsigned short) tim.Year();
		stim.wMonth = (unsigned short) tim.Month();
		stim.wDay = (unsigned short) tim.Date();

		// Then format it to a time based on the current user locale.
		achar rgchDate[50]; // Tuesday, August 15, 2000		mardi 15 août 2000
		::GetDateFormat(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &stim, NULL, rgchDate, 50);
		stu = rgchDate;
		CheckHr(qtisb->Append(stu.Bstr()));
	}
	CheckHr(qtisb->GetString(pptss));

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	The user clicked on the object.

	@param pguid Pointer to a database object's assigned GUID.
	@param hvoOwner The database ID of the object.
	@param tag Identifier used to select one particular property of the object.
	@param ptss Pointer to an ITsString COM object containing a string that embeds a link to the
					object.
	@param ichObj Offset in the string to the pseudo-character that represents the object link.

	@return S_OK, E_POINTER, E_INVALIDARG, or E_FAIL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnRecVc::DoHotLinkAction(BSTR bstrData, HVO hvoOwner, PropTag tag,
	ITsString * ptss, int ichObj)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrData); // REVIEW: Is this allowed to be NULL?
	ChkComArgPtr(ptss);

	// TODO (DarrellZ): This is currently not called because something else is handling the
	// click.
	return SuperClass::DoHotLinkAction(bstrData, hvoOwner, tag, ptss, ichObj);

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}
