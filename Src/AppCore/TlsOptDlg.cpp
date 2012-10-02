/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TlsOptDlg.cpp
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Implementation of the Tools Options Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

HHOOK TlsOptDlg::s_hhook = NULL;


//:>********************************************************************************************
//:>Main Dialog Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
TlsOptDlg::TlsOptDlg(void)
{
	m_rid = kridTlsOptDlg;
	m_itabCurrent = -1;
	m_hwndTab = NULL;
	m_dxsClient = 0;
	m_dysClient = 0;
	m_tgv.itabInitial = 0;
	m_tgv.clsid = 0;
	m_tgv.nLevel = 0;
}


/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
TlsOptDlg::~TlsOptDlg()
{
	if (s_hhook)
	{
		::UnhookWindowsHookEx(s_hhook);
		s_hhook = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Save the original UserViewSpecs on a temp vector.  This copy is used during the save after
	OK or Apply has been pressed.
----------------------------------------------------------------------------------------------*/
void TlsOptDlg::SetVuvsCopy()
{
	m_vuvsOld = m_vuvs; // Save the original UserViewSpecs.
}

/*----------------------------------------------------------------------------------------------
	Strips all non alpha-numeric chars from the field name then checks to see if the name
	is used already in the Db.  If it is then a number is addeed to the end of the name.

	@param stu User name of field
	@param stuDbName Out Fixed name to be used for Db Field name
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlg::FixDbName(StrUni stu, StrUni & stuDbName)
{
	// Strip all not alpha numeric chars to form name for Field$ table (stuDbName).
	wchar rgch[500];
	wchar * pch = rgch;
	stuDbName = stu;
	stuDbName.ToLower();
	int cchDbN = stuDbName.Length();
	wchar ch;
	for (int ich = 0; ich < cchDbN; ++ich)
	{
		ch = stuDbName[ich];
		// We assume only ASCII.
		if (ch >= 'a' && ch <= 'z' || ch >= 0 && ch <= 9)
			*pch++ = ch;
	}
	*pch = '\0';
	stuDbName = rgch;

	// Check to make sure stuDbName is not already used, if it is the add a number to it.
	IOleDbCommandPtr qodc;
	ComBool fMoreRows;
	ComBool fIsNull;
	ULONG cbSpaceTaken;
	StrUni stuQuery;
	int ncnt = 0;
	StrUni stuName = stuDbName;
	int nFound;
	do
	{
		Vector<TlsObject> & vcdi = CustDefInVec();
		stuQuery.Format(L"if exists(select * from Field$ where "
			L"Name = '%s' and class in (%d", stuDbName.Chars(),vcdi[0].m_clsid);
		if (vcdi.Size()>1)
		{
			for (int iv = 1; iv < vcdi.Size(); ++iv)
			{
				stuQuery.FormatAppend(L",%d",vcdi[iv].m_clsid);
			}
		}
		stuQuery.Append(L")) (select 1) else (select 0)");

		AfLpInfo * plpi = m_qrmw->GetLpInfo();
		AssertPtr(plpi);
		AfDbInfoPtr qdbi = plpi->GetDbInfo();
		AssertPtr(qdbi);
		IOleDbEncapPtr qode;
		qdbi->GetDbAccess(&qode);

		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));

		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&nFound),
			isizeof(nFound), &cbSpaceTaken, &fIsNull, 0));
		if (nFound)
		{
			// The field name is already in the list: make a name with a number appended.
			ncnt ++;
			stuDbName = stuName;
			stuDbName.FormatAppend(L"%d", ncnt);
		}
	} while (nFound);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Saves any changes to custom fields that to the Db.
	Any new field that was added will have a 0 as a flid, and will have the class id in
----------------------------------------------------------------------------------------------*/
void TlsOptDlg::SaveCustFlds()
{
	AfLpInfo * plpi = m_qrmw->GetLpInfo();
	AssertPtr(plpi);
	AfDbInfoPtr qdbi = plpi->GetDbInfo();
	AssertPtr(qdbi);
	IOleDbEncapPtr qode;
	qdbi->GetDbAccess(&qode);
	bool fDirty = false;
	IOleDbCommandPtr qodc;

	try
	{
		IOleDbCommandPtr qodc;

		// For each deleted custom field we need to delete the data in the Db.
		Set<int>::iterator sit = m_siCustFldDel.Begin();
		Set<int>::iterator sitLim = m_siCustFldDel.End();

		for (; sit != sitLim; ++sit)
		{
			if (*sit == 0)
				continue;		// deleting a custom field that was just created -- nothing to do!
			// We need to get rid of all StText objects owned in the custom field,
			// plus all UserViewField, CmSortSpec, and CmFilter objects that may
			// use this field, then we can delete the field.
			Vector<HVO> vhvoDel; // StText objects to delete.
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			StrUni stuQuery;
			stuQuery.Format(L"select id from CmObject where ownflid$ = %d %n"
				L"union %n"
				L"select id from CmSortSpec where PrimaryField like '%d,%%' or PrimaryField like "
				L"'%%,%d,%%' or PrimaryField like '%%,%d' or PrimaryField like '%d'%n"
				L" or SecondaryField like '%d,%%' or SecondaryField like '%%,%d,%%'"
				L" or SecondaryField like '%%,%d' or SecondaryField like '%d'%n"
				L" or TertiaryField like '%d,%%' or TertiaryField like '%%,%d,%%' or TertiaryField "
				L"like '%%,%d' or TertiaryField like '%d'%n"
				L"union %n"
				L"select id from CmFilter where ColumnInfo like '%d,%%' or"
				L" ColumnInfo like '%%,%d,%%' or ColumnInfo like '%%,%d' or ColumnInfo like '%d'%n"
				L"union %n"
				L"select id from UserViewField where flid = %d", *sit, *sit, *sit, *sit, *sit, *sit,
				*sit, *sit, *sit, *sit, *sit, *sit, *sit, *sit, *sit, *sit, *sit, *sit);
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			while (fMoreRows)
			{
				HVO hvo;
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo),
					isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
				Assert(cbSpaceTaken == isizeof(hvo));
				vhvoDel.Push(hvo);
				CheckHr(qodc->NextRow(&fMoreRows));
			}
			int ihvo = 0;
			int ihvoLim = vhvoDel.Size();
			stuQuery.Format(L"declare @retval int ;%n");
			stuQuery.FormatAppend(L"exec @retval = DeleteObjects ',"); // the SP will put the comma in if you don't
			for (; ihvo < ihvoLim; ++ihvo)
				stuQuery.FormatAppend(L"%d,", vhvoDel[ihvo]);
			stuQuery.FormatAppend(L"';%n");
			stuQuery.FormatAppend(L"delete from Field$ where id = %d", *sit);
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
			fDirty = true;
		}
		if (fDirty)
		{
			m_qrmw->ClearFilterMenuNodes();
			m_qrmw->ClearSortMenuNodes();
			fDirty = false;
		}

		// find the index to a data entry view, because it will have all the fields in it that need
		// to be added.
		int iuvs;
		for (iuvs = 0; iuvs < m_vuvs.Size(); ++iuvs)
		{
			if (m_vuvs[iuvs]->m_vwt == kvwtDE)
				break;
		}

		// go through all fields and add any new fields to the Db, the update all RecordSpecs
		// where that field is used.
		for (int ito = 0; ito < m_vto.Size(); ++ito)
		{
			RecordSpecPtr qrsp;
			GetBlockVec(m_vuvs, iuvs, ito, &qrsp);
			for (int ifld = 0; ifld < qrsp->m_vqbsp.Size(); ++ifld)
			{
				if (qrsp->m_vqbsp[ifld]->m_flid)
					continue;
				// This field needs to be added to the Db
				fDirty = true;
				StrUni stuName = "Custom";

				ITsStringPtr qtssLabel;
				qtssLabel = qrsp->m_vqbsp[ifld]->m_qtssLabel;

				StrUni stuDbName;
				FixDbName(stuName, stuDbName);

				int nFldType;
				int nDst;
				switch (qrsp->m_vqbsp[ifld]->m_ft)
				{
				case kftMsa:
					nFldType = kcptMultiString;
					break;
				case kftString:
					nFldType = kcptString;
					break;
				case kftStText:
					nFldType = kcptOwningAtom;
					nDst = kclidStText;
					break;
				case kftRefSeq:
					nFldType = kcptReferenceSequence;
					nDst = kclidCmPossibility;
					break;
				case kftRefAtomic:
					nFldType = kcptReferenceAtom;
					nDst = kclidCmPossibility;
					break;
				case kftGenDate:
					nFldType = kcptGenDate;
					break;
				case kftInteger:
					nFldType = kcptInteger;
				}

				// We now have a valid Db field name and type, so add the new field.
				StrUni stuQuery;

				// What is stored in m_hvo at this time is the class id of the object, after
				// getting the id out, this must be cleared.
				if (nFldType == kcptOwningAtom || nFldType == kcptReferenceSequence ||
					nFldType == kcptReferenceAtom)
				{
					stuQuery.Format(L"EXEC [AddCustomField$] ? output, '%s',%d,%d,%d",
						stuDbName.Chars(), nFldType, qrsp->m_vqbsp[ifld]->m_hvo, nDst);
				}
				else
				{
					stuQuery.Format(L"EXEC [AddCustomField$] ? output, '%s',%d,%d",
						stuDbName.Chars(), nFldType, qrsp->m_vqbsp[ifld]->m_hvo);
				}

				int flid;
				ComBool fIsNull;
				CheckHr(qode->CreateCommand(&qodc));
				qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *) &flid,
					sizeof(int));
				CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
				qodc->GetParameter(1, reinterpret_cast<BYTE *>(&flid), sizeof(int), &fIsNull);
				Assert(flid);

				// The field is now added to the Db so go through all Views and add the flid
				// to the blockspecs
				for (int iuv = 0; iuv < m_vuvs.Size(); ++iuv)
				{
					for (int ito = 0; ito < m_vto.Size(); ++ito)
					{
						RecordSpecPtr qrsp;
						GetBlockVec(m_vuvs, iuv, ito, &qrsp);
						for (int ifld = 0; ifld < qrsp->m_vqbsp.Size(); ++ifld)
						{
							if (qrsp->m_vqbsp[ifld]->m_flid)
								continue;
							ComBool fFound;
							qtssLabel->Equals(qrsp->m_vqbsp[ifld]->m_qtssLabel, &fFound);
							if (fFound)
							{
								// Found a field that needs the same flid
								qrsp->m_vqbsp[ifld]->m_flid = flid;
								qrsp->m_vqbsp[ifld]->m_hvo  = 0;
							}
						}
					}
				}
			}
		}

		m_sync = m_siCustFldDel.Size() || fDirty ? ksyncCustomField : ksyncNothing;
		if(!fDirty)
			return;
		m_qrmw->ClearFilterMenuNodes();
		m_qrmw->ClearSortMenuNodes();

		IFwMetaDataCachePtr qmdc;
		plpi->GetDbInfo()->GetFwMetaDataCache(&qmdc);
		qmdc->Init(qode);

		for (iuvs = 0; iuvs < m_vuvs.Size(); ++iuvs)
		{
			for (int ito = 0; ito < m_vto.Size(); ++ito)
			{
				RecordSpecPtr qrsp;
				GetBlockVec(m_vuvs, iuvs, ito, &qrsp);
				qrsp->SetMetaNames(qmdc);
				qrsp->m_fDirty = true;
			}
		}

		// Add the window to the delete list.
		for (int i = 0; i < m_vuvs.Size(); ++i)
		{
			int iwndClient = m_vuvs[i]->m_iwndClient;
			if (iwndClient >= 0)
				m_siwndClientDel.Insert(iwndClient);
		}
		ComBool fTrans = FALSE;
		CheckHr(qode->IsTransactionOpen(&fTrans));
		if (fTrans)
			qode->CommitTrans();
	}
	catch(...)
	{
		ComBool fTrans = FALSE;
		qode->IsTransactionOpen(&fTrans);
		if (fTrans)
			qode->RollbackTrans();
		throw;	// For now we have nothing to add, so pass it on up.
	}
	qodc.Clear();
	qode.Clear();
}


/*----------------------------------------------------------------------------------------------
	Process a browse UserViewSpec. They require extra work since we have a dummy RecordSpec
	with the information we need and this needs to be put into appropriate RecordSpecs for
	each object that is being loaded.
----------------------------------------------------------------------------------------------*/
void TlsOptDlg::ProcessBrowseSpec(UserViewSpec * puvs, AfLpInfo * plpi)
{
	ClevRspMap::iterator ithmclevrspLim = puvs->m_hmclevrsp.End();
	for (ClevRspMap::iterator it = puvs->m_hmclevrsp.Begin(); it != ithmclevrspLim; ++it)
	{
		ClsLevel clev = it.GetKey();
		// Delete all RecordSpecs except our dummy.
		if (clev.m_clsid)
			puvs->m_hmclevrsp.Delete(clev);
	}
	// Now create new record specs as needed.
	CompleteBrowseRecordSpec(puvs, plpi);
}


void TlsOptDlg::CompleteBrowseRecordSpec(UserViewSpec * puvs, AfLpInfo * plpi)
{
	plpi->GetDbInfo()->CompleteBrowseRecordSpec(puvs);
}


void TlsOptDlg::SetUserViewSpecs(UserViewSpecVec * pvuvs)
{
	m_qrmw->GetLpInfo()->GetDbInfo()->SetUserViewSpecs(pvuvs);
}


/*----------------------------------------------------------------------------------------------
	Saves user view information changed in the dialog and updates the viewbar and any
	affected windows.  It makes a copy of the vector of UserViewSpecs, makes the
	modifications on the copy, then returns the new vector.

	@param iViewTab Index to which TlsOptDlg tab is to be saved. (Always kidlgViews.)
----------------------------------------------------------------------------------------------*/
void TlsOptDlg::SaveViewValues(int iViewTab)
{
	AfLpInfo * plpi = m_qrmw->GetLpInfo();
	AssertPtr(plpi);
	AssertPtr(m_qrmw);

	// If we have deleted any views, delete them from the database.
	for (int iuvsO = 0; iuvsO < m_vuvsOld.Size(); ++iuvsO)
	{
		int iuvsN;
		for (iuvsN = 0; iuvsN < m_vuvs.Size(); ++iuvsN)
		{
			if (m_vuvsOld[iuvsO] == m_vuvs[iuvsN])
				break;
		}
		// If we deleted a UserViewSpec with an hvo, then we need to delete it from the
		// database.
		if (iuvsN == m_vuvs.Size() && m_vuvsOld[iuvsO]->m_hvo)
			plpi->GetDbInfo()->DeleteObject(m_vuvsOld[iuvsO]->m_hvo);
	}

	// For each Browse UserViewSpec, we need to fill in additional information.
	int cuvs = m_vuvs.Size();
	for (int ivuvs = 0; ivuvs < cuvs; ivuvs++)
	{
		UserViewSpecPtr quvs = m_vuvs[ivuvs];
		if (quvs->m_vwt == kvwtBrowse)
			ProcessBrowseSpec(quvs, plpi);
	}

	SetUserViewSpecs(&m_vuvs);
	AfDbInfo * pdbi = m_qrmw->GetLpInfo()->GetDbInfo();
	AssertPtr(pdbi);

	// Get the number of open frame windows that use the same database.
	// Remember UserViews are only global to projects in a single database.
	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	int iqwnd;
	int cqwnd = vqafw.Size();
	int cwnd = 0;
	RecMainWnd * prmwLoop;
	for (iqwnd = 0; iqwnd < cqwnd; ++iqwnd)
	{
		prmwLoop = dynamic_cast<RecMainWnd *>(vqafw[iqwnd].Ptr());
		AssertObj(prmwLoop);
		if (pdbi == prmwLoop->GetLpInfo()->GetDbInfo())
			++cwnd;
	}

	// Storage for information on each open frame window that uses the same database.
	Vector<AfMdiClientWnd *> vpmdic;
	vpmdic.Resize(cwnd);
	for (int iwnd = 0, iqwnd = 0; iqwnd < cqwnd; ++iqwnd)
	{
		prmwLoop = dynamic_cast<RecMainWnd *>(vqafw[iqwnd].Ptr());
		AssertObj(prmwLoop);
		if (pdbi == prmwLoop->GetLpInfo()->GetDbInfo())
		{
			vpmdic[iwnd] = prmwLoop->GetMdiClientWnd();
		}
	}
	cuvs = m_vuvs.Size();
		// Build up the list of child windows that should appear in this main window.
		//Vector<AfClientWndPtr> vqafcw;
		for (int iuvs = 0; iuvs < cuvs; ++iuvs) //, ++wid)
		{
			// Save the new/modified view to the database.
				UserViewSpecPtr quvs = m_vuvs[iuvs];
				// Now save or update the view in the database.
				// Note: quvs->m_hvo should always be 0 for a new or copied UserViewSpec.
				// In this case, all HVOs in embedded objects will be forced to new HVOs.
				IOleDbEncapPtr qode;
				plpi->GetDbInfo()->GetDbAccess(&qode);
				quvs->Save(qode, !quvs->m_hvo);
		}
}

/*----------------------------------------------------------------------------------------------
	Saves the filter changes.

	@param imagFilterSimple Index to image of simple filter. (Always kimagFilterSimple.)
	@param imagFilterFull Index to image of Advanced filter. (Always kimagFilterFull.)
	@param idlgFilters Index to which TlsOptDlg tab is to be saved. (Always kidlgFilters.)
----------------------------------------------------------------------------------------------*/
void TlsOptDlg::SaveFilterValues(int imagFilterSimple, int imagFilterFull,
	int idlgFilters)
{
	FwFilterDlg * pfltdlg = dynamic_cast<FwFilterDlg *>(m_vdlgv[idlgFilters].Ptr());
	AssertPtr(pfltdlg);

	if (!pfltdlg->WasModified())
		return;

	AssertPtr(m_qrmw);
	int iFilterList = m_qrmw->GetViewbarListIndex(kvbltFilter);

	AfDbInfo * pdbiCur = m_qrmw->GetLpInfo()->GetDbInfo();
	AssertPtr(pdbiCur);
	int cfltOld = pdbiCur->GetFilterCount();

	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	int cwnd = vqafw.Size();

	Vector<AfViewBarShell *> vpvwbrs;
	Vector<HVO> vhvoSel;
	Vector<int> vifltNewSel;
	vpvwbrs.Resize(cwnd);
	vhvoSel.Resize(cwnd);
	vifltNewSel.Resize(cwnd);

	int clidRec = m_qrmw->GetRecordClid();

	// Keep track of the currently selected filter.
	Set<int> sisel;
	for (int iwnd = 0; iwnd < cwnd; iwnd++)
	{
		RecMainWnd * prmwLoop = dynamic_cast<RecMainWnd *>(vqafw[iwnd].Ptr());
		AssertObj(prmwLoop);
		AfDbInfo * pdbi = prmwLoop->GetLpInfo()->GetDbInfo();
		AssertPtr(pdbi);
		if (pdbi != pdbiCur)
		{
			// We don't have to update windows that are not using the same database as the
			// current window.
			continue;
		}
		if (prmwLoop->GetRecordClid() != clidRec)
		{
			// We don't have to update windows that are not using the same basic record class
			// as the current window.
			continue;
		}

		vpvwbrs[iwnd] = prmwLoop->GetViewBarShell();
		AssertObj(vpvwbrs[iwnd]);

		sisel.Clear();
		vpvwbrs[iwnd]->GetSelection(iFilterList, sisel);
		Assert(sisel.Size() == 1);
		int ifltSel = pdbiCur->ComputeFilterIndex(*sisel.Begin(), clidRec);
		if (ifltSel >= 0)
		{
			Assert((uint)ifltSel < (uint)pdbiCur->GetFilterCount());
			AppFilterInfo & afi = pdbiCur->GetFilterInfo(ifltSel);
			vhvoSel[iwnd] = afi.m_hvo;
		}
	}

	// Clear out all the old filters from the relevant view bar(s) and the database info.
	for (int ifltOld = cfltOld; --ifltOld >= 0; )
	{
		AppFilterInfo & afi = pdbiCur->GetFilterInfo(ifltOld);
		if (afi.m_clidRec == clidRec)
		{
			StrApp str = afi.m_stuName;
			for (int iwnd = 0; iwnd < cwnd; iwnd++)
			{
				if (vpvwbrs[iwnd])
					vpvwbrs[iwnd]->DeleteListItem(iFilterList, str.Chars());
			}
			pdbiCur->RemoveFilter(ifltOld);
		}
	}


	// Add all the new filters to the relevant view bar(s) and the database info.
	Vector<FwFilterDlg::FilterInfo> vfi;
	pfltdlg->GetDialogValues(vfi);
	int cflt = vfi.Size();
	for (int iflt = 0; iflt < cflt; iflt++)
	{
		FwFilterDlg::FilterInfo & fi = vfi[iflt];
		Assert(fi.m_fs != FwFilterDlg::kfsDeleted);

		StrApp str = fi.m_stuName;
		for (int iwnd = 0; iwnd < cwnd; iwnd++)
		{
			if (vpvwbrs[iwnd])
			{
				vpvwbrs[iwnd]->AddListItem(iFilterList, str.Chars(),
					fi.m_fSimple ? imagFilterSimple : imagFilterFull);
			}
			if (vhvoSel[iwnd] == fi.m_hvoOld)
				vifltNewSel[iwnd] = iflt + 1;
		}
		pdbiCur->AddFilter(fi.m_stuName, fi.m_fSimple, fi.m_hvoOld, fi.m_stuColInfo,
			fi.m_fShowPrompt, fi.m_stuPrompt, clidRec);
	}

	// Save the filter and window/viewbar information for later processing.  Doing it here
	// allows a massive consumption of "GDI Object" and "User Object" resources.
	m_vifltNew.Clear();
	m_vpvwbrsFlt = vpvwbrs;
	int ifltNew;
	for (int iwnd = 0; iwnd < cwnd; iwnd++)
	{
		if (vpvwbrs[iwnd])
			ifltNew = vifltNewSel[iwnd];
		else
			ifltNew = -1;
		m_vifltNew.Push(ifltNew);
	}
//	// Select the old filter if it still exists, otherwise select no filter.
//	for (int iwnd = 0; iwnd < cwnd; iwnd++)
//	{
//		if (vpvwbrs[iwnd])
//		{
//			sisel.Clear();
//			sisel.Insert(vifltNewSel[iwnd]);
//			vpvwbrs[iwnd]->SetSelection(iFilterList, sisel);
//		}
//	}
}

/*----------------------------------------------------------------------------------------------
	Saves the sort method changes.

	@param imagSort Index to image of SortMethod. (Always kimagSort.)
	@param idlgSort Index to which TlsOptDlg tab is to be saved. (Always kidlgSortMethods.)
----------------------------------------------------------------------------------------------*/
void TlsOptDlg::SaveSortValues(int imagSort, int idlgSort)
{
	TlsOptDlgSort * ptods = dynamic_cast<TlsOptDlgSort *>(m_vdlgv[idlgSort].Ptr());
	AssertPtr(ptods);

	if (!ptods->WasModified())
		return;

	AssertPtr(m_qrmw);
	int iSortList = m_qrmw->GetViewbarListIndex(kvbltSort);

	AfDbInfo * pdbiCur = m_qrmw->GetLpInfo()->GetDbInfo();
	AssertPtr(pdbiCur);
	int csrtOld = pdbiCur->GetSortCount();

	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	int cwnd = vqafw.Size();

	Vector<AfViewBarShell *> vpvwbrs;
	Vector<HVO> vhvoSel;
	Vector<int> visrtNewSel;
	vpvwbrs.Resize(cwnd);
	vhvoSel.Resize(cwnd);
	visrtNewSel.Resize(cwnd);

	int clidRec = m_qrmw->GetRecordClid();

	// Keep track of the currently selected sort method.
	Set<int> sisel;
	for (int iwnd = 0; iwnd < cwnd; iwnd++)
	{
		RecMainWnd * prmwLoop = dynamic_cast<RecMainWnd *>(vqafw[iwnd].Ptr());
		AssertObj(prmwLoop);
		AfDbInfo * pdbi = prmwLoop->GetLpInfo()->GetDbInfo();
		AssertPtr(pdbi);
		if (pdbi != pdbiCur)
		{
			// We don't have to update windows that are not using the same database as the
			// current window.
			continue;
		}
		if (prmwLoop->GetRecordClid() != clidRec)
		{
			// We don't have to update windows that are not using the same basic record class
			// as the current window.
			continue;
		}

		vpvwbrs[iwnd] = prmwLoop->GetViewBarShell();
		AssertObj(vpvwbrs[iwnd]);
		sisel.Clear();
		vpvwbrs[iwnd]->GetSelection(iSortList, sisel);
		Assert(sisel.Size() == 1);
		int isrtSel = pdbiCur->ComputeSortIndex(*sisel.Begin(), clidRec);
		if (isrtSel >= 0)
		{
			Assert((uint)isrtSel < (uint)pdbiCur->GetSortCount());
			AppSortInfo & asi = pdbiCur->GetSortInfo(isrtSel);
			vhvoSel[iwnd] = asi.m_hvo;
		}
	}

	// Clear out all the old sort methods from the relevant view bar(s) and the database info.
	for (int isrtOld = csrtOld; --isrtOld >= 0; )
	{
		AppSortInfo & asi = pdbiCur->GetSortInfo(isrtOld);
		if (asi.m_clidRec == clidRec)
		{
			StrApp str = asi.m_stuName;
			for (int iwnd = 0; iwnd < cwnd; iwnd++)
			{
				if (vpvwbrs[iwnd])
					vpvwbrs[iwnd]->DeleteListItem(iSortList, str.Chars());
			}
			pdbiCur->RemoveSort(isrtOld);
		}
	}

	// Add all the new sort methods to the relevant view bar(s) and the database info.
	Vector<TlsOptDlgSort::SortMethodInfo> vsmi;
	ptods->GetDialogValues(vsmi);
	int csrt = vsmi.Size();
	for (int isrt = 0; isrt < csrt; isrt++)
	{
		TlsOptDlgSort::SortMethodInfo & smi = vsmi[isrt];
		Assert(smi.m_sms != TlsOptDlgSort::ksmsDeleted);

		StrApp str = smi.m_stuName;
		for (int iwnd = 0; iwnd < cwnd; iwnd++)
		{
			if (vpvwbrs[iwnd])
			{
				vpvwbrs[iwnd]->AddListItem(iSortList, str.Chars(), imagSort);
			}
			if (vhvoSel[iwnd] == smi.m_hvoOld)
				visrtNewSel[iwnd] = isrt + 1;
		}
		AppSortInfo asi;
		asi.m_stuName = smi.m_stuName;
		asi.m_fIncludeSubfields = smi.m_fIncludeSubfields;
		asi.m_hvo = smi.m_hvoOld;
		asi.m_clidRec = clidRec;

		SortMethodUtil::CreateFieldPath(smi.m_rgski[TlsOptDlgSort::kiskiPrimary].m_vflid,
			asi.m_stuPrimaryField);
		asi.m_wsPrimary = smi.m_rgski[TlsOptDlgSort::kiskiPrimary].m_ws;
		asi.m_collPrimary = smi.m_rgski[TlsOptDlgSort::kiskiPrimary].m_coll;
		asi.m_fPrimaryReverse = smi.m_rgski[TlsOptDlgSort::kiskiPrimary].m_fReverse;

		SortMethodUtil::CreateFieldPath(smi.m_rgski[TlsOptDlgSort::kiskiSecondary].m_vflid,
			asi.m_stuSecondaryField);
		asi.m_wsSecondary = smi.m_rgski[TlsOptDlgSort::kiskiSecondary].m_ws;
		asi.m_collSecondary = smi.m_rgski[TlsOptDlgSort::kiskiSecondary].m_coll;
		asi.m_fSecondaryReverse = smi.m_rgski[TlsOptDlgSort::kiskiSecondary].m_fReverse;

		SortMethodUtil::CreateFieldPath(smi.m_rgski[TlsOptDlgSort::kiskiTertiary].m_vflid,
			asi.m_stuTertiaryField);
		asi.m_wsTertiary = smi.m_rgski[TlsOptDlgSort::kiskiTertiary].m_ws;
		asi.m_collTertiary = smi.m_rgski[TlsOptDlgSort::kiskiTertiary].m_coll;
		asi.m_fTertiaryReverse = smi.m_rgski[TlsOptDlgSort::kiskiTertiary].m_fReverse;

		SortMethodUtil::CheckMultiOutput(pdbiCur, asi);
		pdbiCur->AddSort(asi);
	}

	// Select the old sort method if it still exists, otherwise select the default sort method.
	for (int iwnd = 0; iwnd < cwnd; iwnd++)
	{
		if (vpvwbrs[iwnd])
		{
			sisel.Clear();
			sisel.Insert(visrtNewSel[iwnd]);
			vpvwbrs[iwnd]->SetSelection(iSortList, sisel);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Saves the overlay changes.

	@param imagOverlay Index to image of Overlay. (Always kimagOverlay.)
	@param idlgOverlays Index to which TlsOptDlg tab is to be saved. (Always kidlgOverlays.)
----------------------------------------------------------------------------------------------*/
void TlsOptDlg::SaveOverlayValues(int imagOverlay, int idlgOverlays)
{
	TlsOptDlgOvr * ptod = dynamic_cast<TlsOptDlgOvr *>(m_vdlgv[idlgOverlays].Ptr());
	AssertPtr(ptod);

	if (!ptod->WasModified())
		return;

	AssertPtr(m_qrmw);
	int iOverlayList = m_qrmw->GetViewbarListIndex(kvbltOverlay);

	AfLpInfo * plpiCur = m_qrmw->GetLpInfo();
	AssertPtr(plpiCur);
	int covrOld = plpiCur->GetOverlayCount();

	m_qrmw->ClearFilterMenuNodes();
	m_qrmw->ClearSortMenuNodes();

	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	int cwnd = vqafw.Size();

	Vector<AfViewBarShell *> vpvwbrs;
	typedef Set<HVO> SetHvo;
	typedef Set<int> SetInt;
	Vector<SetHvo> vshvoSel;
	Vector<SetInt> vsiselNew;
	vpvwbrs.Resize(cwnd);
	vshvoSel.Resize(cwnd);
	vsiselNew.Resize(cwnd);

	// Keep track of the currently selected overlays.
	Set<int> sisel;
	for (int iwnd = 0; iwnd < cwnd; iwnd++)
	{
		RecMainWnd * prmwLoop = dynamic_cast<RecMainWnd *>(vqafw[iwnd].Ptr());
		AssertObj(prmwLoop);
		AfLpInfo * plpi = prmwLoop->GetLpInfo();
		AssertPtr(plpi);
		if (plpi != plpiCur)
		{
			// We don't have to update windows that are not showing the same language project
			// as the current window.
			continue;
		}

		vpvwbrs[iwnd] = prmwLoop->GetViewBarShell();
		AssertObj(vpvwbrs[iwnd]);

		sisel.Clear();
		vpvwbrs[iwnd]->GetSelection(iOverlayList, sisel);
		Set<int>::iterator si = sisel.Begin();
		if (*si != 0)
		{
			for (; si != sisel.End(); ++si)
			{
				Assert((uint)(*si - 1) < (uint)plpiCur->GetOverlayCount());
				AppOverlayInfo & aoi = plpiCur->GetOverlayInfo(*si - 1);
				Assert(aoi.m_hvo);
				vshvoSel[iwnd].Insert(aoi.m_hvo);
			}
		}
	}

	// Clear out all the old overlays from the relevant view bar(s) and the langproj info.
	for (int iovrOld = covrOld; --iovrOld >= 0; )
	{
		AppOverlayInfo & aoi = plpiCur->GetOverlayInfo(iovrOld);
		StrApp str = aoi.m_stuName;
		for (int iwnd = 0; iwnd < cwnd; iwnd++)
		{
			if (vpvwbrs[iwnd])
				vpvwbrs[iwnd]->DeleteListItem(iOverlayList, str.Chars());
		}
		plpiCur->RemoveOverlay(iovrOld);
	}

	// Add all the new overlays to the relevant view bar(s) and the langproj info.
	Vector<TlsOptDlgOvr::OverlayInfo> voi;
	ptod->GetDialogValues(voi);
	int covr = voi.Size();
	for (int iovr = 0; iovr < covr; iovr++)
	{
		TlsOptDlgOvr::OverlayInfo & oi = voi[iovr];
		Assert(oi.m_os != TlsOptDlgOvr::kosDeleted);

		StrApp str = oi.m_stuName;
		for (int iwnd = 0; iwnd < cwnd; iwnd++)
		{
			if (vpvwbrs[iwnd])
			{
				vpvwbrs[iwnd]->AddListItem(iOverlayList, str.Chars(), imagOverlay);
			}
			if (vshvoSel[iwnd].IsMember(oi.m_hvo))
			{
				int iselNew = iovr + 1;
				vsiselNew[iwnd].Insert(iselNew);
			}
		}
		plpiCur->AddOverlay(oi.m_hvo, oi.m_hvoPssl, oi.m_stuName);
	}

	// Select the old overlays if they still exist, otherwise select no overlay.
	for (int iwnd = 0; iwnd < cwnd; iwnd++)
	{
		if (vpvwbrs[iwnd])
		{
			if (vsiselNew[iwnd].Size() == 0)
			{
				int iselNew = 0;
				vsiselNew[iwnd].Insert(iselNew);
			}
			vpvwbrs[iwnd]->SetSelection(iOverlayList, vsiselNew[iwnd]);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	DWORD dwThreadId = GetCurrentThreadId();
	s_hhook = ::SetWindowsHookEx(WH_GETMESSAGE, &GetMsgProc, 0, dwThreadId);

	m_fCustFldDirty = false;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Switch to a different dialog tab.

	@param itab index of the dialog to swich to.
	@return true
----------------------------------------------------------------------------------------------*/
bool TlsOptDlg::ShowChildDlg(int itab)
{
	Assert((uint)itab < (uint)m_cTabs);
	AssertPtr(m_vdlgv[itab]);

	if (m_itabCurrent == itab)
	{
		// We already have the tab selected, so we can return without doing anything.
		return true;
	}

	if (!m_vdlgv[itab]->Hwnd())
	{
		HWND hwndFocus = ::GetFocus();

		// This is the first time this tab has been selected, and the dialog has not
		// been created yet, so create it now.
		m_vdlgv[itab]->DoModeless(m_hwnd);

		// This is needed so the new dialog has the correct z-order in the parent dialog.
		::SetWindowPos(m_vdlgv[itab]->Hwnd(), NULL, m_dxsClient, m_dysClient, 0, 0,
			SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

		// If the focus was on the tab control, Windows moves the focus to the
		// new dialog, so set it back to the tab control.
		if (hwndFocus == m_hwndTab)
			::SetFocus(m_hwndTab);
	}

	bool fRet = m_vdlgv[itab]->SetActive();
	if (fRet)
	{
		// Show the new dialog view and hide the old one.
		::ShowWindow(m_vdlgv[itab]->Hwnd(), SW_SHOW);
		if (m_itabCurrent != -1)
		{
			::ShowWindow(m_vdlgv[m_itabCurrent]->Hwnd(), SW_HIDE);
			// Hide/Show child windows for filters when switching tabs.
			FwFilterDlg * pfltdlg = dynamic_cast<FwFilterDlg *>(m_vdlgv[m_itabCurrent].Ptr());
			if (pfltdlg)
				pfltdlg->ShowChildren(false);
			pfltdlg = dynamic_cast<FwFilterDlg *>(m_vdlgv[itab].Ptr());
			if (pfltdlg)
				pfltdlg->ShowChildren(true);
		}
		m_itabCurrent = itab;
	}

	TabCtrl_SetCurSel(m_hwndTab, m_itabCurrent);
	return fRet;
}


/*----------------------------------------------------------------------------------------------
	Process notifications for this dialog from some event on a control.  This method is called
	by the framework.

	@param ctid Id of the control that issued the windows command.
	@param pnmh Windows command that is being passed.
	@param lnRet return value to be returned to the windows command.
	@return true if command is handled.
	See ${AfWnd#OnNotifyChild}
----------------------------------------------------------------------------------------------*/
bool TlsOptDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	switch(pnmh->code)
	{
	case TCN_SELCHANGE:
		{
			WaitCursor wc;
			// Make sure we can move to the current tab.
			int itab = TabCtrl_GetCurSel(m_hwndTab);
			Assert((uint)itab < (uint)m_cTabs);
			ShowChildDlg(itab);
			return true;
		}

	case TCN_SELCHANGING:
		{
			WaitCursor wc;
			// Make sure that we can move off of the current tab.
			int itab = TabCtrl_GetCurSel(m_hwndTab);
			Assert((uint)itab < (uint)m_cTabs);
			lnRet = !m_vdlgv[itab]->QueryClose(AfDialogView::kqctChange);
			return true;
		}

	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the OK or the Apply Now button.
	When the framework calls this method, changes are made in the dialog are accepted if the
	return value is true.

	@param fClose not used here
	@return true if Successful
----------------------------------------------------------------------------------------------*/
bool TlsOptDlg::OnApply(bool fClose)
{
	for (int idlgv = 0; idlgv < m_cTabs; idlgv++)
	{
		Assert(m_vdlgv[idlgv]);
		if (m_vdlgv[idlgv]->Hwnd() && !m_vdlgv[idlgv]->Apply())
			return false;
	}

	AfApp::Papp()->EnableMainWindows(true);
	return AfDialog::OnApply(fClose);
}


/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the Cancel button.
	When the framework calls this method, changes made in the dialog are not accepted.

	@return true if Successful
----------------------------------------------------------------------------------------------*/
bool TlsOptDlg::OnCancel()
{
	if (m_vdlgv.Size())
	{
		for (int idlgv = 0; idlgv < m_cTabs; idlgv++)
		{
			if (m_vdlgv[idlgv]->Hwnd())
				m_vdlgv[idlgv]->Cancel();
		}
	}

	AfApp::Papp()->EnableMainWindows(true);
	return AfDialog::OnCancel();
}


/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the Help button.

	@return true if Successful
----------------------------------------------------------------------------------------------*/
bool TlsOptDlg::OnHelp()
{
	Assert((uint)m_itabCurrent < (uint)m_cTabs);
	AssertPtr(m_vdlgv[m_itabCurrent]);
	m_vdlgv[m_itabCurrent]->Help();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Returns a RecordSpec containing the BlockSpec pointers for the given
	UserViewSpec (ivuvs) and the given record type (vrt).

	@param vuvs UserViewSpec
	@param ivuvs Index into the UserViewSpec
	@param vrt Record type
	@param pprsp RecordSpec
----------------------------------------------------------------------------------------------*/
void TlsOptDlg::GetBlockVec(UserViewSpecVec & vuvs, int ivuvs, int vrt,
	RecordSpec ** pprsp)
{
	AssertPtr(pprsp);
	Assert(false); // This method needs to be processed in the subclass.
	*pprsp = NULL;
}


/*----------------------------------------------------------------------------------------------
	Set the initial values for upon entering the TlsOptDlg.  In addition to which tab to show,
	there can be up to two additional values that are defined for that tab.

	@param tgv struct to handle several input parameters.
----------------------------------------------------------------------------------------------*/
void TlsOptDlg::SetDialogValues(TlsDlgValue tgv)
{
	Assert((uint)tgv.itabInitial < (uint)m_cTabs);

	m_tgv = tgv;
}


/*----------------------------------------------------------------------------------------------
	This method checks to see if the name (staName) is already used in a listview control.
	If it is then it adds a number until it gets a valid unused name.

	@param staName Name to find or fix.
	@param hwndList Handle of the listview control that has the list of names.
	@param fCopy This is used to tell whether the name is for a new item or for a copy of an
	existing item. It is needed because the naming convention is different for these two cases.
----------------------------------------------------------------------------------------------*/
void TlsOptDlg::FixName(StrApp & strName, HWND hwndList, bool fCopy)
{
	StrApp strOld = strName;
	if (fCopy)
	{
		StrApp str(kstidCopy1);
		strName.Format(str.Chars(), strOld.Chars());
	}
	LVFINDINFO plvfi = { LVFI_STRING };
	plvfi.psz = strName.Chars();

	int iv = 1;
	while (-1 < ListView_FindItem(hwndList, -1, &plvfi))
	{
		if (fCopy)
		{
			StrApp str(kstidCopyN);
			strName.Format(str.Chars(), ++iv, strOld.Chars()); // Start at 2.
		}
		else
			strName.Format(_T("%s (%d)"), strOld.Chars(), iv++); // Start at 1.
		plvfi.psz = strName.Chars();
	}
}

/*----------------------------------------------------------------------------------------------
	Handle window messages, passing unhandled messages on to the superclass's FWndProc method.
	Only WM_ACTIVATE is processed, and even then the message is
	passed on to the superclass's FWndProc method.

	@param wm Window message code.
	@param wp Window message word parameter.
	@param lp Window message long parameter.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True or false: whatever the superclass's FWndProc method returns.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_ACTIVATE)
	{
		if (LOWORD(wp) == WA_INACTIVE)
		{
			// Remove our special accelerator table.
			AfApp::Papp()->RemoveAccelTable(m_atid);
		}
		else
		{
			// We load this basic accelerator table so that these commands can be directed to this
			// window. This allows the embedded Views to see the commands. Otherwise, if
			// they are translated by the main window, the main window is the 'target', and the
			// command handlers on AfVwRootSite don't work, because the root site is not a child
			// window of the main one.
			// I'm creating and destroying in Activate/Deactivate partly because I copied the code
			// from AfFindDialog, but also just to make sure this accel table can't be
			// accidentally used for other windows.
			m_atid = AfApp::Papp()->LoadAccelTable(kridAccelBasic, 0, m_hwnd);
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Catch WM_COMMAND messages from the hook method.
----------------------------------------------------------------------------------------------*/
bool TlsOptDlg::OnCommand(int cid, int nc, HWND hctl)
{
	if (cid == kcidPrevTab || cid == kcidNextTab)
	{
		int itabCur = TabCtrl_GetCurSel(m_hwndTab);
		Assert((uint)itabCur < (uint)m_cTabs);
		int itabNew;
		if (cid == kcidPrevTab)
			itabNew = itabCur - 1;
		else
			itabNew = itabCur + 1;
		if (itabNew >= 0 && itabNew < m_cTabs)
		{
			WaitCursor wc;
			// Make sure that we can move off of the current tab.
			if (m_vdlgv[itabCur]->QueryClose(AfDialogView::kqctChange))
			{
				Assert((uint)itabNew < (uint)m_cTabs);
				ShowChildDlg(itabNew);
			}
		}
		return true;
	}

	return SuperClass::OnCommand(cid, nc, hctl);
}


/*----------------------------------------------------------------------------------------------
	A hook procedure used to capture accelerator keys while the dialog is open.
----------------------------------------------------------------------------------------------*/
LRESULT TlsOptDlg::GetMsgProc(int code, WPARAM wParam, LPARAM lParam)
{
	if (code < 0)
		return ::CallNextHookEx(s_hhook, code, wParam, lParam);

	MSG * pmsg = (MSG *)lParam;
	AssertPtr(pmsg);

	if (pmsg->message == WM_KEYDOWN && pmsg->wParam == VK_TAB)
	{
		if (::GetAsyncKeyState(VK_CONTROL) < 0)
		{
			TlsOptDlg * ptod = NULL;
			HWND hwnd = pmsg->hwnd;
			while (hwnd && !ptod)
			{
				ptod = dynamic_cast<TlsOptDlg *>(AfWnd::GetAfWnd(hwnd));
				hwnd = ::GetParent(hwnd);
			}
			if (ptod && pmsg->message == WM_KEYDOWN)
			{
				if (::GetAsyncKeyState(VK_SHIFT) < 0)
					::SendMessage(ptod->Hwnd(), WM_COMMAND, kcidPrevTab, 0);
				else
					::SendMessage(ptod->Hwnd(), WM_COMMAND, kcidNextTab, 0);
			}
			// Keep the message from being handled normally.
			pmsg->message = 0;
			pmsg->hwnd = 0;
		}
	}
	return ::CallNextHookEx(s_hhook, code, wParam, lParam);
}
