/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001, 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDbInfo.cpp
Responsibility: Steve McConnel
Last reviewed:

	This file contains implementations for the following classes:
		AfDbInfo : GenRefObj
		PossItemInfo
		PossListInfo : GenRefObj
		AfLpInfo : GenRefObj
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	AfDbInfo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
AfDbInfo::AfDbInfo()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
AfDbInfo::~AfDbInfo()
{
}

void AfDbInfo::CheckTransactionKludge()
{
	// REVIEW ?? (PaulP): This is a kludge to commit changes made to the various "settings"
	// tables during application startup.  Doing this allows other applications to read those
	// tables and not block.  Generally speaking, one should NOT call the CommitTrans on the
	// OleDbEncap object directly but rather the Commit method on the ActionHandler object
	// should be called. If we don't do this commit, Cle will lock when it calls LoadPossList
	// in CleMainWnd::PostAttach. Something should probably be fixed in the procedure or
	// somewhere else instead of this approach.
	IOleDbEncapPtr qode;
	GetDbAccess(&qode);
	ComBool fOpen;
	qode->IsTransactionOpen(&fOpen);
	if (fOpen)
		qode->CommitTrans();
}

/*----------------------------------------------------------------------------------------------
	Clear out the pointers to the Language Projects used by this database.
----------------------------------------------------------------------------------------------*/
void AfDbInfo::CleanUp()
{
	ComBool f;
	m_qode->IsTransactionOpen(&f);
	if (f)
		// We need this commit to save CmOverlays that may be created on startup.
		m_qode->CommitTrans();
	int clpi = m_vlpi.Size();
	for (int ilpi = 0; ilpi < clpi; ilpi++)
	{
		m_vlpi[ilpi]->CleanUp();
		m_vlpi[ilpi].Clear();
	}
	m_vlpi.Clear();
	m_vuvs.Clear();

	// This is needed here to avoid spurious memory leak.
	m_qwsf->Shutdown();
}


/*----------------------------------------------------------------------------------------------
	Initialize a new database info object.

	@param pszServer
	@param pszDatabase
	@param pfist
----------------------------------------------------------------------------------------------*/
void AfDbInfo::Init(const OLECHAR * pszServer, const OLECHAR * pszDatabase, IStream * pfist)
{
	// Init should only be called once.
	Assert(!m_qode);
	Assert(!m_qmdc);
	Assert(!m_qwsf);

	//  Obtain pointer to view cache, OleDbEncap, and MetaDataCache interface
	m_stuSvrName = pszServer;
	m_stuDbName = pszDatabase;
	m_qode.CreateInstance(CLSID_OleDbEncap);
	AssertPtr(m_qode);
	CheckHr(m_qode->Init(m_stuSvrName.Bstr(), m_stuDbName.Bstr(), pfist, koltMsgBox,
		koltvForever));
	m_qmdc.CreateInstance(CLSID_FwMetaDataCache);
	AssertPtr(m_qmdc);
	CheckHr(m_qmdc->Init(m_qode));

	// Obtain a pointer to the language writing system factory for this database, creating it if
	// necessary, and pass it the IStream pointer for logging.
	ILgWritingSystemFactoryBuilderPtr qwsfb;
	qwsfb.CreateInstance(CLSID_LgWritingSystemFactoryBuilder);
	CheckHr(qwsfb->GetWritingSystemFactory(m_qode, pfist, &m_qwsf));
	AssertPtr(m_qwsf);
}


/*----------------------------------------------------------------------------------------------
	Add a new filter to the vector of stored filters.

	@param pszName
	@param fSimple
	@param hvo
	@param pszColInfo
	@param fShowPrompt
	@param pszPrompt
	@param clidRec
----------------------------------------------------------------------------------------------*/
void AfDbInfo::AddFilter(const wchar * pszName, bool fSimple, HVO hvo, const wchar * pszColInfo,
	bool fShowPrompt, const wchar * pszPrompt, int clidRec)
{
	AssertPsz(pszName);
	AssertPszN(pszColInfo);
	AssertPszN(pszPrompt);

	AppFilterInfo afi;
	afi.m_stuName = pszName;
	afi.m_fSimple = fSimple;
	afi.m_hvo = hvo;
	afi.m_stuColInfo = pszColInfo;
	afi.m_fShowPrompt = fShowPrompt;
	afi.m_stuPrompt = pszPrompt;
	afi.m_clidRec = clidRec;

	m_vafi.Push(afi);
}

/*----------------------------------------------------------------------------------------------
	Compute the actual stored filter index from the view bar index.

	@param isel Index from the view bar (0 = "no filter").
	@param clidRec Basic class id of the target records for this filter.

	@return Index into the vector of filters, or -1 if the combination of isel and clidRec is
					invalid.
----------------------------------------------------------------------------------------------*/
int AfDbInfo::ComputeFilterIndex(int isel, int clidRec)
{
	if (!isel)
		return -1;

	int iflt;
	for (iflt = 0; iflt < m_vafi.Size(); ++iflt)
	{
		if (m_vafi[iflt].m_clidRec == clidRec)
		{
			--isel;
			if (!isel)
				return iflt;
		}
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Compute the actual stored sort method index from the view bar index.

	@param isel Index from the view bar (0 = "default sort").
	@param clidRec Basic class id of the target records for this sort method.

	@return Index into the vector of sort methods, or -1 if the combination of isel and clidRec
					is invalid.
----------------------------------------------------------------------------------------------*/
int AfDbInfo::ComputeSortIndex(int isel, int clidRec)
{
	if (!isel)
		return -1;

	int isrt;
	for (isrt = 0; isrt < m_vasi.Size(); ++isrt)
	{
		if (m_vasi[isrt].m_clidRec == clidRec)
		{
			--isel;
			if (!isel)
				return isrt;
		}
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Delete the object (hvo) from the database along with everything it owns.
	Return true if successful.

	@param hvo

	@return
----------------------------------------------------------------------------------------------*/
bool AfDbInfo::DeleteObject(HVO hvo)
{
	Assert(hvo);

	IOleDbCommandPtr qodc;
	StrUni stuSqlStmt;
	//ComBool fIsNull;
	//ComBool fMoreRows;
	//ULONG cbSpaceTaken;

	// Obtain pointer to IOleDbEncap interface and execute the given SQL select command.
	AssertPtr(m_qode);

	try
	{
		/*
		Query is:
			declare @retval int
			exec @retval = DeleteObjects '3391'
			select @retval
		It returns 1 row with 0 or a failure code.
		*/
		//stuSqlStmt.Format(
		//	L"declare @retval int ;%n"
		//	L"exec @retval = DeleteObjects N'%d', null ;%n"
		//	L"select @retval ;%n", hvo);
		stuSqlStmt.Format(L"exec DeleteObjects N'%d'", hvo);

		CheckHr(m_qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtStoredProcedure));

		//int nErr = -1;
		//CheckHr(qodc->GetRowset(0));
		//CheckHr(qodc->NextRow(&fMoreRows));
		//if (fMoreRows)
		//{
		//	CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&nErr),
		//		isizeof(int), &cbSpaceTaken, &fIsNull, 0));
		//}

		return true; //!nErr; // 0 means success.
	}
	catch (...)
	{
		Assert(false);
		return false;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------

	@param pvuvs

	@return true
----------------------------------------------------------------------------------------------*/
bool AfDbInfo::GetCopyUserViewSpecs(UserViewSpecVec * pvuvs)
{
	UserViewSpecPtr quvs;
	for (int ivw = 0; ivw < m_vuvs.Size(); ++ivw)
	{
		m_vuvs[ivw]->NewCopy(&quvs);
		pvuvs->Push(quvs);
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Find a field spec from the user view specs that matches the desired field id, and possibly
	view type as well.

	@param flid Field id
	@param vwt View Type (defaults to "don't care")

	@return Pointer to the desired field spec object, or NULL if one could not be found.
----------------------------------------------------------------------------------------------*/
FldSpec * AfDbInfo::FindFldSpec(int flid, int vwt)
{
	Assert(m_vuvs.Size());

	int iuvs;
	UserViewSpecPtr quvs;
	ClevRspMap::iterator it;
	RecordSpecPtr qesp;
	int ibsp;
	BlockSpecPtr qbsp;
	int ifsp;
	FldSpecPtr qfsp;

	for (iuvs = 0; iuvs < m_vuvs.Size(); ++iuvs)
	{
		quvs = m_vuvs[iuvs];
		if (vwt != -1 && quvs->m_vwt != vwt)
			continue;
		for (it = quvs->m_hmclevrsp.Begin(); it != quvs->m_hmclevrsp.End(); ++it)
		{
			qesp = it->GetValue();
			for (ibsp = 0; ibsp < qesp->m_vqbsp.Size(); ++ibsp)
			{
				qbsp = qesp->m_vqbsp[ibsp];
				if (qbsp->m_flid == flid)
					return dynamic_cast<FldSpec *>(qbsp.Ptr());
				for (ifsp = 0; ifsp < qbsp->m_vqfsp.Size(); ++ifsp)
				{
					qfsp = qbsp->m_vqfsp[ifsp];
					if (qfsp->m_flid == flid)
						return qfsp.Ptr();
				}
			}
		}
	}
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Find the guid of the CmObject in the database that matches this ID.

	@param pdbi Pointer to the application database information.
	@param hvo Input object id value for the database.
	@param uid Reference to the output GUID.

	@return True if a CmObject has the desired hvo (object id), otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfDbInfo::GetGuidFromId(HVO hvo, GUID & uid)
{
	AssertPtr(m_qode);

	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	IOleDbCommandPtr qodc;

	try
	{
		StrUni stuQuery;
		stuQuery.Format(L"select guid$ from CmObject where id = %d", hvo);

		CheckHr(m_qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (!fMoreRows)
			return false;

		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&uid), isizeof(GUID),
			&cbSpaceTaken, &fIsNull, 0));

		return true;
	}
	catch (...)
	{
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Find the ID of the CmObject in the database that matches this guid.

	@param pdbi Pointer to the application database information.
	@param puid Pointer to a GUID that represents an entry in the database CmObject table (the
					Guid$ field).

	@return The database ID corresponding to the GUID (the Id field), or zero if the GUID is
					invalid.
----------------------------------------------------------------------------------------------*/
HVO AfDbInfo::GetIdFromGuid(GUID * puid)
{
	AssertPtr(puid);
	AssertPtr(m_qode);

	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	IOleDbCommandPtr qodc;

	try
	{
		StrUni stuQuery;
		stuQuery.Format(L"select id from CmObject where guid$ = '%g'", puid);

		CheckHr(m_qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (!fMoreRows)
			return 0;

		HVO hvo;
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), isizeof(HVO),
			&cbSpaceTaken, &fIsNull, 0));

		return hvo;
	}
	catch (...)
	{
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Refresh various parts of AfDbInfo.
	@param grfdbi A bitmap indicating what parts of AfDbInfo to refresh (defined in
		DbiRefreshFlags.
----------------------------------------------------------------------------------------------*/
bool AfDbInfo::FullRefresh(int grfdbi)
{
	if (grfdbi & kfdbiSortSpecs)
	{
		// Reload sort specs from the database.
		m_vasi.Clear();
		LoadSortMethods();
	}

	if (grfdbi & kfdbiFilters)
	{
		// Reload filters from the database.
		m_vafi.Clear();
		LoadFilters();
	}

	if (grfdbi & kfdbiMetadata)
	{
		// Reload metadata from the database.
		m_qmdc->Init(m_qode);
	}

	if (grfdbi & kfdbiUserViews)
	{
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(AfApp::Papp()->GetCurMainWnd());
		if (prmw)
			prmw->LoadUserViews();
	}

	if (grfdbi & kfdbiEncFactories)
	{
		// Clear the writing system cache in the writing system factory.
		CheckHr(m_qwsf->Clear());
	}

	if (grfdbi & kfdbiLpInfo)
	{
		// Refresh all of the language projects for this database.
		for (int ilpi = m_vlpi.Size(); --ilpi >= 0; )
		{
			int grflpi = kflpiCache | kflpiStyles | kflpiWritingSystems | kflpiProjBasics |
				kflpiPossLists | kflpiExtLink;
			// If we are using overlays, reload them as well.
			if (m_vlpi[ilpi]->GetOverlayCount())
				grflpi |= kflpiOverlays;
			// We need to clear the cache so that new data will be loaded and reload the
			// stylesheet as well as reloading other caches.
			if (!m_vlpi[ilpi]->FullRefresh(grflpi))
				return false;
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Load the UserViews, create UserViewSpecs for each one and store in vuvs.
	Load the UserViewRecs, create RecordSpecs and store in appropriate UserViewSpecs.
	Load the UserViewFields, create BlockSpecs/FldSpecs and store in appropriate RecordSpecs.
	Then fill in the names for each RecordSpec and find all Browse UserViews and fill in the
	additional RecordSpecs.

	@param pclsid Pointer to the application's class id GUID.
	@param vwt User view type to load (defaults to -1 for all)

	@return true
----------------------------------------------------------------------------------------------*/
bool AfDbInfo::LoadUserViews(const CLSID * pclsid, int vwt)
{
	m_vuvs.Clear();		// Get rid of any old views.
	int ws;
	CheckHr(m_qwsf->get_UserWs(&ws));
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	IOleDbCommandPtr qodc;
	ITsStringPtr qtss;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	AssertPtr(qtsf);
	Vector<RecordSpec *> vprsp;
	UserViewSpecPtr quvs;
	RecordSpecPtr qrsp;

	// Load the UserViews, create UserViewSpecs for each one and store in m_vuvs.
	try
	{
		// We order by name so that the list will be sorted in the view bar.
		HVO hvo;
		int nType;
		const int kcbBuf = 100;
		BYTE rgb[kcbBuf];
		int fSystem;
		const int kcchBuf = MAX_PATH;
		OLECHAR rgchName[kcchBuf];
		StrUni stuQuery;
		stuQuery.Format(L"select Id, Type, uvn.Txt, Details, System "
			L"from UserView "
			L"left outer join UserView_Name uvn on uvn.obj = id "
			L"where app = '%g' and uvn.Ws = %d ",
			pclsid, ws);
		if ((unsigned)vwt < (unsigned)kvwtLim)
			stuQuery.FormatAppend(L"and Type = %d ", vwt);
		stuQuery.Append(L"order by uvn.Txt");

		CheckHr(m_qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		while (fMoreRows)
		{
			quvs.Create();
			quvs->m_ws = ws;
			quvs->m_guid = *pclsid;

			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo),
				isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
			Assert(hvo);
			quvs->m_hvo = hvo;

			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&nType),
				isizeof(nType), &cbSpaceTaken, &fIsNull, 0));
			Assert(cbSpaceTaken == 1); // Tinyint.
			nType = *(signed char *)(&nType); // Sign extend to int.
			Assert((uint)nType < kvwtLim);
			quvs->m_vwt = (UserViewType)nType;

			CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&rgchName),
				isizeof(rgchName), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qtsf->MakeStringRgch(rgchName, cbSpaceTaken / 2, ws, &qtss));
			quvs->m_qtssName = qtss;

			CheckHr(qodc->GetColValue(4, reinterpret_cast <BYTE *>(rgb),
				isizeof(rgb), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull && cbSpaceTaken)
			{
				quvs->m_nMaxLines = (int &)rgb[0] & 0x7FFFFFFF;
				quvs->m_fIgnorHier = (bool)((int &)rgb[0] & 0x80000000);
			}
			CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(&fSystem),
				isizeof(fSystem), &cbSpaceTaken, &fIsNull, 0));
			Assert(cbSpaceTaken == 2); // Bool (? Not sure why this isn't 1).
			fSystem = *(short *)(&fSystem); // Sign extend to int.
			quvs->m_fv = fSystem;

			m_vuvs.Push(quvs); // Save the list of UserViewSpecs.

			CheckHr(qodc->NextRow(&fMoreRows));
		}
	}
	catch (...)
	{
		return false;
	}
	if (!m_vuvs.Size())
		return false; // Fail if we can't find a user view.

	// Load the UserViewRecs, create RecordSpecs and store in appropriate UserViewSpecs.
	try
	{
		/* Example query
			select Src, Id, Clsid, [Level] from UserView_Records uv
			left outer join UserViewRec uvr on uvr.id = uv.dst
			where Src in (3468,3493,3564)
			order by src
		*/
		HVO hvo;
		int iuvs;
		int cuvs = m_vuvs.Size();
		ClsLevel clev;
		HVO hvoLast = -1; // We are ordered by UserViewSpec, so keep the last id.

		StrUni stuQuery;
		stuQuery.Format(L"select Src, Id, Clsid, [Level] from UserView_Records uv "
			L"left outer join UserViewRec uvr on uvr.id = uv.dst "
			L"where Src in (%d", m_vuvs[0]->m_hvo);
		for (iuvs = 1; iuvs < cuvs; ++iuvs)
			stuQuery.FormatAppend(L",%d", m_vuvs[iuvs]->m_hvo);
		stuQuery.Append(L") order by src");
		CheckHr(m_qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		while (fMoreRows)
		{
			qrsp.Create();

			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo),
				isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
			Assert(hvo);
			if (hvo != hvoLast)
			{
				// Find the UserViewSpec we are loading.
				for (iuvs = 0; iuvs < cuvs; ++iuvs)
				{
					quvs = m_vuvs[iuvs];
					if (quvs->m_hvo == hvo)
						break;
				}
				Assert(iuvs < cuvs);
				hvoLast = hvo;
			}

			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hvo),
				isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
			Assert(hvo);
			qrsp->m_hvo = hvo;

			CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&clev.m_clsid),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			qrsp->m_clsid = clev.m_clsid;

			CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&clev.m_nLevel),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			Assert(cbSpaceTaken == 1); // Tinyint.
			clev.m_nLevel = *(signed char *)(&clev.m_nLevel); // Sign extend to int.
			qrsp->m_nLevel = clev.m_nLevel;
			qrsp->m_vwt = quvs->m_vwt;

			// Add the new RecordSpec to the UserViewSpec.
			quvs->m_hmclevrsp.Insert(clev, qrsp, true);

			vprsp.Push(qrsp); // Save a list of RecordSpecs.

			CheckHr(qodc->NextRow(&fMoreRows));
		}
	}
	catch (...)
	{
		m_vuvs.Clear(); // Get rid of the partial user view.
		return false;
	}

	// Load the UserViewFields, create BlockSpecs/FldSpecs and store in appropriate RecordSpecs.
	try
	{
		/* Example query
			select Src, Id, SubfieldOf, Type, uvf.Flid, Visibility, Required, Style, WritingSystem,
				IsCustomField, PossList, isnull(uvfl.txt,""), isnull(uvfh.txt,""), Details
			from UserViewRec_Fields uvrf
			left outer join UserViewField uvf on uvf.id = uvrf.dst
			left outer join UserViewField_Label uvfl on uvfl.obj = uvf.id
			left outer join UserViewField_HelpString uvfh on uvfh.obj = uvf.id
			where src in (3469,3476,3481,3488,3494,3513,3529,3548,3565,3582,3595,3612)
				and (uvfl.txt is not null or uvfl.ws = 740664001
				and uvfh.txt is not null or uvfh.ws = 740664001)
			order by src, uvrf.ord
		*/
		HVO hvo;
		int irsp;
		int crsp = vprsp.Size();
		HVO hvoLast = -1; // We are ordered by RecordSpec, so keep the last id.
		BlockSpecPtr qbsp;
		FldSpecPtr qfsp;
		HVO hvoSub;
		int nType;
		int nVisibility;
		int nRequired;
		int flid;
		int wsReal;
		int wsMagic;
		ComBool fCustFld;
		HVO hvoPssl;
		const int kcchBuf = MAX_PATH;
		OLECHAR rgch[kcchBuf];
		const int kcbBuf = 100;
		BYTE rgb[kcbBuf];

		StrUni stuQuery;
		// Note: We include uvrf.ord in the sort sequence to make sure that the BlockSpec
		// that owns the FldSpecs is defined just prior to the FldSpecs.
		stuQuery.Format(L"SELECT uvrf.Src, uvf.Id, uvf.SubfieldOf, uvf.Type, uvf.Flid,"
			L" uvf.Visibility, uvf.Required, uvf.Style, uvf.WritingSystem, uvf.IsCustomField,"
			L" uvf.PossList, isnull(uvfl.txt,''), isnull(uvfh.txt,''), uvf.Details,"
			L" uvf.WsSelector"
			L" FROM UserViewRec_Fields uvrf"
			L" LEFT OUTER JOIN UserViewField uvf ON uvf.id = uvrf.dst"
			L" LEFT OUTER JOIN UserViewField_Label uvfl ON uvfl.obj = uvf.id"
		L" LEFT OUTER JOIN UserViewField_HelpString uvfh ON uvfh.obj = uvf.id"
			L" where src in (%d", vprsp[0]->m_hvo);
		for (irsp = 1; irsp < crsp; ++irsp)
			stuQuery.FormatAppend(L",%d", vprsp[irsp]->m_hvo);
		stuQuery.FormatAppend(L") and ((uvfl.txt is null or uvfl.ws = %d) "
			L"and (uvfh.txt is null or uvfh.ws = %d)) ", ws, ws);
		stuQuery.Append(L"order by uvrf.Src, uvrf.Ord");

		CheckHr(m_qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo),
				isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
			Assert(hvo);
			if (hvo != hvoLast)
			{
				// Find the RecordSpec we are loading.
				for (irsp = 0; irsp < crsp; ++irsp)
				{
					qrsp = vprsp[irsp];
					if (qrsp->m_hvo == hvo)
						break;
				}
				Assert(irsp < crsp);
				hvoLast = hvo;
			}

			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&hvo),
				isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
			Assert(hvo);

			CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&hvoSub),
				isizeof(hvoSub), &cbSpaceTaken, &fIsNull, 0));
			Assert(fIsNull || hvo);
			if (fIsNull)
			{
				qbsp.Create(); // Create a BlockSpec.
				qfsp = dynamic_cast<FldSpec *>(qbsp.Ptr());
				hvoSub = 0;
			}
			else
			{
				qfsp.Create(); // Create a FldSpec.
			}
			qfsp->m_hvo = hvo;
			qfsp->m_ws = 0;

			CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(&nType),
				isizeof(nType), &cbSpaceTaken, &fIsNull, 0));
			Assert(cbSpaceTaken == 1); // Tinyint.
			nType = *(signed char *)(&nType); // Sign extend to int.
			Assert((uint)nType < kftLim);
			qfsp->m_ft = (FldType)nType;

			CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(&flid),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			qfsp->m_flid = flid;

			CheckHr(qodc->GetColValue(6, reinterpret_cast<BYTE *>(&nVisibility),
				isizeof(nVisibility), &cbSpaceTaken, &fIsNull, 0));
			Assert(cbSpaceTaken == 1); // Tinyint.
			nVisibility = *(signed char *)(&nVisibility); // Sign extend to int.
			Assert((uint)nVisibility < kFTVisLim);
			qfsp->m_eVisibility = (FldVis)nVisibility;

			CheckHr(qodc->GetColValue(7, reinterpret_cast<BYTE *>(&nRequired),
				isizeof(nRequired), &cbSpaceTaken, &fIsNull, 0));
			Assert(cbSpaceTaken == 1); // Tinyint.
			nRequired = *(signed char *)(&nRequired); // Sign extend to int.
			Assert((uint)nRequired < kFTReqLim);
			qfsp->m_fRequired = (FldReq)nRequired;

			CheckHr(qodc->GetColValue(8, reinterpret_cast<BYTE *>(&rgch),
				isizeof(rgch), &cbSpaceTaken, &fIsNull, 2));
			qfsp->m_stuSty = rgch;

			CheckHr(qodc->GetColValue(9, reinterpret_cast<BYTE *>(&wsReal),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
				qfsp->m_ws = wsReal;

			CheckHr(qodc->GetColValue(10, reinterpret_cast<BYTE *>(&fCustFld),
				isizeof(ComBool), &cbSpaceTaken, &fIsNull, 0));
			qfsp->m_fCustFld = (bool)fCustFld;

			CheckHr(qodc->GetColValue(11, reinterpret_cast<BYTE *>(&hvoPssl),
				isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
			qfsp->m_hvoPssl = hvoPssl;

			// fields 12 and 13 are the name (aka label) and
			// description (aka Help) for "What's This?".
			CheckHr(qodc->GetColValue(12, reinterpret_cast<BYTE *>(&rgch),
				isizeof(rgch), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qtsf->MakeStringRgch(rgch, cbSpaceTaken / 2, ws, &qtss));
			qfsp->m_qtssLabel = qtss;

			CheckHr(qodc->GetColValue(13, reinterpret_cast<BYTE *>(&rgch),
				isizeof(rgch), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qtsf->MakeStringRgch(rgch, cbSpaceTaken / 2, ws, &qtss));
			qfsp->m_qtssHelp = qtss;

			CheckHr(qodc->GetColValue(14, reinterpret_cast <BYTE *>(rgb),
				isizeof(rgb), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull && cbSpaceTaken)
			{
				int n;
				// The first 4 bytes store m_fHideLabel as bit 31 and m_dxpColumn as low bits.
				qfsp->m_fHideLabel = (int &)rgb[0] & (1 << 31);
				qfsp->m_dxpColumn = (int &)rgb[0] & ~(1 << 31);

				switch (nType)
				{
				case kftRefAtomic:
				case kftRefCombo:
				case kftRefSeq:
					{
						// It is a Choices List field type.
						// The second int stores m_fVert as bit 31, m_fHier as bit 30, and
						// m_pnt as low bits.
						n = (int &)rgb[4];
						qfsp->m_fVert = n & 1 << 31;
						qfsp->m_fHier = n & 1 << 30;
						qfsp->m_pnt = PossNameType(n & 0xff);
						break;
					}
				case kftExpandable:
					{
						// It is an Expandable field type.
						// The second int stores m_fExpand as bit 31, m_fHier as bit 30, and
						// m_pnt as low bits.
						n = (int &)rgb[4];
						qfsp->m_fExpand = n & 1 << 31;

						qfsp->m_fHier = n & 1 << 30;
						qfsp->m_pnt = PossNameType(n & 0xff);
						break;
					}
				case kftSubItems:
					{
						// It is a Hierarchical field type.
						// The second int stores m_fExpand as bit 31 and m_ons as low bits.
						n = (int &)rgb[4];
						qfsp->m_fExpand = n & 1 << 31;
						qfsp->m_ons = OutlineNumSty(n & 0xff);
						break;
					}
				default:
					// Other types.
					// The second int is not stored.
					break;
				}
			}

			if (hvoSub)
			{
				// Add the new FldSpec to the last BlockSpec.
				qbsp->m_vqfsp.Push(qfsp);
			}
			else
			{
				// Add the new BlockSpec to the RecordSpec.
				qrsp->m_vqbsp.Push(qbsp);
			}
			CheckHr(qodc->GetColValue(15, reinterpret_cast<BYTE *>(&wsMagic),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
				qfsp->m_ws = wsMagic;

			CheckHr(qodc->NextRow(&fMoreRows));
		}
	}
	catch (...)
	{
		m_vuvs.Clear(); // Get rid of the partial user view.
		return false;
	}

	// Now fill in the names for each RecordSpec.
	for (int irsp = 0; irsp < vprsp.Size(); ++irsp)
		vprsp[irsp]->SetMetaNames(m_qmdc);

	// Find all Browse UserViews and fill in the additional RecordSpecs.
	for (int iuvs = 0; iuvs < m_vuvs.Size(); ++iuvs)
	{
		UserViewSpecPtr quvs = m_vuvs[iuvs];
		AssertPtr(quvs);
		if (quvs->m_vwt != kvwtBrowse)
			continue;
		CompleteBrowseRecordSpec(quvs);
	}

	return true;
}


//:>********************************************************************************************
//:>	PossItemInfo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
PossItemInfo::PossItemInfo()
{
	m_hvoPss = 0;
	m_nHier = 0;
}


/*----------------------------------------------------------------------------------------------
	Set the requested part of the name of the possibility item.

	@param stu
	@param pnt
	@param ws
----------------------------------------------------------------------------------------------*/
void PossItemInfo::SetName(StrUni stu, PossNameType pnt, int ws)
{
	StrUni stuTemp;
	if (!m_stu.Length() || pnt == kpntNameAndAbbrev)
	{
		m_stu = stu;
	}
	else
	{
		int ich = m_stu.FindStr(L" - ");
		Assert(ich > 0);
		if (pnt == kpntName)
		{
			m_stu = m_stu.Left(ich + 3);
			m_stu.Append(stu.Chars(),stu.Length());
		}
		else
		{
			stuTemp = stu;
			stuTemp.Append(m_stu.Chars() + ich);
			m_stu = stuTemp;
		}
	}
	m_ws = ws;
}

/*----------------------------------------------------------------------------------------------
	Get the requested part of the name and writing system of the possibility item.

	@param stu
	@param pnt
	@return Writing system of the text.
----------------------------------------------------------------------------------------------*/
int PossItemInfo::GetName(StrUni & stu, PossNameType pnt)
{
	if (!m_stu.Length() || pnt == kpntNameAndAbbrev)
		stu = m_stu;
	else
	{
		int ich = m_stu.FindStr(L" - ");
		if (ich > 0)
		{
			if (pnt == kpntName)
				stu = m_stu.Chars() + ich + 3;
			else
				stu = m_stu.Left(ich);
		}
		else
		stu = m_stu;
	}
	return m_ws;
}

/*----------------------------------------------------------------------------------------------
	Get the requested part of the name and writing system of the possibility item displaying the
	full hierarchy. The current item must be part of the pli possibility list.

	@param ppli
	@param stu
	@param pnt
	@return Writing system of the text.
----------------------------------------------------------------------------------------------*/
int PossItemInfo::GetHierName(PossListInfo * ppli, StrUni & stu, PossNameType pnt)
{
	AssertPtr(ppli);
	Assert((uint)pnt < (uint)kpntLim);
	// These constants come from the OrdKey column of the GetPossibilities MSDE stored
	// procedure. GetPossibilities returns the list depth-first.

	stu.Clear(); // Get rid of anything passed in.
	StrUni stuT;
	PossItemInfo * ppii = this;
	int nHier;
	int ipii = ppli->GetIndexFromId(m_hvoPss);
	Assert(ipii >= 0);

	for (;;)
	{
		// Insert the name for the current item.
		ppii->GetName(stuT, pnt);
		stu.Replace(0, 0, stuT);

		// Check for higher level names.
		nHier = ppii->GetHierLevel();
		if (nHier == 1)
			return m_ws; // We are done.
		// Back up to the next higher possibility node.
		while (--ipii >= 0)
		{
			ppii = ppli->GetPssFromIndex(ipii);
			Assert(ppii);
			if (ppii->GetHierLevel() < nHier)
			{
				// Add a higher level name.
				stu.Replace(0, 0, &kchHierDelim, 1);
				break;
			}
		}
		Assert(ipii >= 0); // We should never get in an endless loop.
	}
	return m_ws;
}


/*----------------------------------------------------------------------------------------------
	Return the level of the item. The possibilities list info is passed in order to determine
	a baseline for the hierarchy lengths for this list.
	@param ppli
	@return hierarchical level of the current item
----------------------------------------------------------------------------------------------*/
int PossItemInfo::GetLevel(PossListInfo * ppli)
{
	int ilevel = (m_nHier - ppli->TopHierLevel());
	return ilevel;
}


//:>********************************************************************************************
//:>	PossListInfo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
PossListInfo::PossListInfo()
{
	Assert(m_hvoPssl == NULL);
	Assert(m_wsTitle == 0);
	m_pnt = kpntName;
}


/*----------------------------------------------------------------------------------------------
	Change the type of name information displayed for the possibility list items.
	@param pnt
----------------------------------------------------------------------------------------------*/
void PossListInfo::SetDisplayOption(PossNameType pnt)
{
	Assert((uint)pnt < (uint)kpntLim);
	if (pnt != m_pnt)
	{
		m_pnt = pnt;
		//  Obtain pointer to IOleDbEncap interface and execute the appropriate SQL update
		// command to save the new DisplayOption value to the database.
		try
		{
			AfDbInfo * pdbi = m_qlpi->GetDbInfo();
			AssertPtr(pdbi);
			IOleDbEncapPtr qode;
			pdbi->GetDbAccess(&qode);
			StrUni stuSqlStmt;
			stuSqlStmt.Format(
				L"UPDATE CmPossibilityList SET DisplayOption=%d WHERE [Id]=%d",
					m_pnt, m_hvoPssl);
			IOleDbCommandPtr qodc;
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtNoResults));
		}
		catch (...)	// Was empty.
		{
			throw;	// For now we have nothing to add, so pass it on up.
		}

		// Update all lists now that we've changed.
		SyncInfo sync(ksyncPossList, m_hvoPssl, 0);
		m_qlpi->StoreAndSync(sync);
		// Notify all the PossListNotify objects for this list of the change.
		//DoNotify(kplnaDisplayOption, 0, 0, 0, 0);
	}
}

/*----------------------------------------------------------------------------------------------
 * This struct is used in loading the possibility list in order to build up the tree structure
 * of a hierarchical list.
 ---------------------------------------------------------------------------------------------*/
struct PossItemInfoPlus
{
	PossItemInfo m_pii;
	int m_iFirstChild;
	int m_iNextSibling;
};

/*----------------------------------------------------------------------------------------------
 *	Store the possibility items in the vector in correct tree order ("preorder" in this case).
 *  I.e., 1) visit the root.  2) visit the subtrees of the first tree (in preorder).  3) visit
 *  the remaining trees at the same level (in preorder).  See Knuth, I.334.
 ---------------------------------------------------------------------------------------------*/
static void StorePossibilityVector(const Vector<PossItemInfoPlus> & vpiix, int ix,
	Vector<PossItemInfo> & vpii)
{
	do
	{
		vpii.Push(vpiix[ix].m_pii);
		if (vpiix[ix].m_iFirstChild > 0)
			StorePossibilityVector(vpiix, vpiix[ix].m_iFirstChild, vpii);
		ix = vpiix[ix].m_iNextSibling;	// think of this loop as tail recursion if you like...
	} while (ix > 0);
}

/*----------------------------------------------------------------------------------------------
	Load the possibility list for the given writing system.
	This should only be called by AfLpInfo::LoadPossList.
	@param plpi Pointer to the AfLpInfo for the language project
	@param hvoPssl Id of the possibility list
	@param wsMagic Writing system we want to return. It has the following possibilities:
		kwsAnal = first writing system in CurAnalysisWss -> Use the first writing system if found,
			or substitute any other writing system that is available for the item.
		kwsVern = first writing system in CurVernWss -> Use the first writing system if found,
			or substitute any other writing system that is available for the item.
		kwsAnalVerns = first writing system in CurAnalysisWss if there is one else the first
			CurVernWss, or substitute any other writing system for the item.
		kwsVernAnals = first writing system in CurVernWss if there is one else the first
			CurAnalysisWss, or substitute any other writing system for the item.
		for use the specified encoding or *** if missing.
	@param ipli Index to the possibility list in the vector of lists.
	@return True if successful, false if something went wrong.
----------------------------------------------------------------------------------------------*/
bool PossListInfo::LoadPossList(AfLpInfo * plpi, HVO hvoPssl, int wsMagic, int ipli)
{
/*
Initially the item name and abbreviation was brought in from the DB where they could have
different encodings, but that was a problem since we store it in memory as a single stu
string.  To solve this problem and problems with type-ahead it was decided that the name and
abbr. should come in together as one writing system.  If your encodings are ENG, TGL, FRE then when
the list is loaded it looks for item name in ENG if not found it looks at TGL then FRE if nothing
was found returns "***".  Whatever Name writing system is found, the abbr of that same writing system will
be returned, if it does not exist then "***" will be loaded as the abbr.  Rand
*/


	AssertPtr(plpi);
	m_vpii.Clear(); // Remove any old items from the list.

	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	wsMagic = plpi->NormalizeWs(wsMagic);

	// Obtain pointer to IOleDbEncap interface and execute the given SQL select command.
	m_qlpi = plpi;
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetDbAccess(&qode);

	const int kcchBuffer = MAX_PATH;
	StrUni stuSqlStmt;
	OLECHAR rgchNameAbbr[kcchBuffer];
	OLECHAR rgchAbbr[kcchBuffer];
	OLECHAR rgchName[kcchBuffer];
	OLECHAR rgchDesc[kcchBuffer] = L"";
	try
	{
		// Load information about the possibility list from the database.
		OLECHAR rgchHelp[kcchBuffer];
		byte pnt;
		byte nDepth;
		ComBool fIsSorted;
		ComBool fIsClosed;
		ComBool fPreventDup;
		ComBool fUseExtendedFlds;
		int nItemClsid;
		int wsTitle;

		bool fWsAnal = true; // Default to analaysis list title unless vernacular.
		stuSqlStmt.Format(L"exec GetOrderedMultiTxt '%d', %d, %d",
			hvoPssl, kflidCmMajorObject_Name, fWsAnal);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtStoredProcedure));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		Assert(fMoreRows); // This proc should always return something.
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchName),
			kcchBuffer * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
		CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&wsTitle), isizeof(wsTitle),
			&cbSpaceTaken, &fIsNull, 0));
		m_wsTitle = wsTitle;
		m_stuName = rgchName;

		stuSqlStmt.Format(L"select txt from CmPossibilityList_Abbreviation"
			L" where obj = %d and ws = %d",
			hvoPssl, wsTitle);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchAbbr),
				kcchBuffer * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
			m_stuAbbr = rgchAbbr;
		}
		else
			m_stuAbbr.Assign("***");

		stuSqlStmt.Format(L"select txt from MultiBigStr$"
			L" where obj = %d and flid = %d and ws = %d",
			hvoPssl, kflidCmMajorObject_Description, wsTitle);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		cbSpaceTaken = 0;
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchDesc),
				kcchBuffer * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));

			int cchData = cbSpaceTaken / isizeof(OLECHAR);
			//  If buffer was too small, reallocate and try again.
			if ((cchData > kcchBuffer) && (!fIsNull))
			{
				OLECHAR * pchBuffer = NewObj wchar[cchData];
				CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(pchBuffer),
					cchData * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
				m_stuDesc = pchBuffer;
				delete[] pchBuffer;
			}
			else
			{
				m_stuDesc = rgchDesc;
			}
		}
		else
		{
			m_stuDesc = L"";
		}

		stuSqlStmt.Format(
			L"SELECT cp.HelpFile, cp.DisplayOption, cp.Depth, cp.IsSorted, "
			L"cp.IsClosed, cp.PreventDuplicates, cp.UseExtendedFields, cp.ItemClsid "
			L"FROM CmPossibilityList cp "
			L"WHERE cp.id = %d", hvoPssl);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchHelp),
				kcchBuffer * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
			CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(&pnt),
				isizeof(byte), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(3, reinterpret_cast <BYTE *>(&nDepth),
				isizeof(byte), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(4, reinterpret_cast <BYTE *>(&fIsSorted),
				isizeof(ComBool), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(5, reinterpret_cast <BYTE *>(&fIsClosed),
				isizeof(ComBool), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(6, reinterpret_cast <BYTE *>(&fPreventDup),
				isizeof(ComBool), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(7, reinterpret_cast <BYTE *>(&fUseExtendedFlds),
				isizeof(ComBool), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(8, reinterpret_cast <BYTE *>(&nItemClsid),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			// Update the title bar (window text) with the name.
			m_nDepth = nDepth;
			m_fIsSorted = (bool)fIsSorted;
			m_fIsClosed = (bool)fIsClosed;
			m_fAllowDup = !(bool)fPreventDup;
			m_fUseExtendedFlds = (bool)fUseExtendedFlds;
			m_nItemClsid = nItemClsid;
			m_stuHelp = rgchHelp;
			m_pnt = (PossNameType)(min((uint)pnt, kpntLim - 1));
			// We don't expect the database to have -1/-2, but for older databases
			// it's possible they will. So we'll force them to -3/-4 here just in case.
			m_wsMagic = wsMagic;
		}

		// Load information about the possibilities in this list from the database.
		int cpss;
		HVO hvoPss;
		int nHier;
		COLORREF clrFore;
		COLORREF clrBack;
		COLORREF clrUnder;
		int unt;
		int wsNameAbbr = 0;
		HVO hvoOwner = 0;

		// Execute the stored procedure to get all the Possibilities given the
		// PossibilityList and the writing system.
		stuSqlStmt.Format(L"exec GetPossibilities %d, %d", hvoPssl, wsMagic);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuSqlStmt.Bstr(), knSqlStmtStoredProcedure));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&cpss),
				sizeof(int), &cbSpaceTaken, &fIsNull, 0));

			// Allocate memory for all the rows.
			Vector<PossItemInfoPlus> vpiix;
			vpiix.Resize(cpss);
			HashMap<HVO, int> hmhvoidx;

			// The second rowset gives the actual possibility items.
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			int ipss = 0;
			while (fMoreRows && ipss < cpss)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoPss),
					sizeof(hvoPss), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(rgchNameAbbr),
					sizeof(OLECHAR) * kcchBuffer, &cbSpaceTaken, &fIsNull, 2));
				CheckHr(qodc->GetColValue(3, reinterpret_cast <BYTE *>(&wsNameAbbr),
					sizeof(wsNameAbbr), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(4, reinterpret_cast <BYTE *>(&nHier),
					sizeof(nHier), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(&clrFore),
					sizeof(COLORREF), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(6, reinterpret_cast<BYTE *>(&clrBack),
					sizeof(COLORREF), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(7, reinterpret_cast<BYTE *>(&clrUnder),
					sizeof(COLORREF), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(8, reinterpret_cast<BYTE *>(&unt),
					sizeof(int), &cbSpaceTaken, &fIsNull, 0));
				unt &= 0xFF;
				CheckHr(qodc->GetColValue(9, reinterpret_cast <BYTE *>(&hvoOwner),
					sizeof(hvoOwner), &cbSpaceTaken, &fIsNull, 0));

				CheckHr(qodc->NextRow(&fMoreRows));

				//  Set the item information in the possibility item vector.
				// The return values are ordered by OwnerDepth, RelOrder
				PossItemInfo & pii = vpiix[ipss].m_pii;
				pii.m_hvoPss = hvoPss;
				pii.m_clrFore = clrFore;
				pii.m_clrBack = clrBack;
				pii.m_clrUnder = clrUnder;
				pii.m_unt = unt;
				pii.m_ws = wsNameAbbr;
				pii.m_stu = rgchNameAbbr;
				pii.m_nHier = nHier;
				vpiix[ipss].m_iFirstChild = 0;
				vpiix[ipss].m_iNextSibling = 0;
				hmhvoidx.Insert(hvoPss, ipss);
				if (nHier > 1)
				{
					int idx;
					if (hmhvoidx.Retrieve(hvoOwner, &idx))
					{
						if (vpiix[idx].m_iFirstChild == 0)
						{
							vpiix[idx].m_iFirstChild = ipss;
						}
						else
						{
							idx = vpiix[idx].m_iFirstChild;
							while (vpiix[idx].m_iNextSibling != 0)
								idx = vpiix[idx].m_iNextSibling;
							vpiix[idx].m_iNextSibling = ipss;
						}
					}
					else
					{
						// WARN THE PROGRAMMER!
						Assert(hmhvoidx.Retrieve(hvoOwner, &idx));
					}
				}
				else
				{
					if (ipss > 0)
						vpiix[ipss - 1].m_iNextSibling = ipss;
				}
				ipss++;
			}
			// The number returned above (cpss) is actually >= the actual number of rows
			// returned. So we resize the vector to the actual number of rows that were
			// returned.
			m_vpii.EnsureSpace(ipss, true);
			vpiix.Resize(ipss);
			hmhvoidx.Clear();
			/*
			 * The data is read in ordered by OwnerDepth, RelOrder, like this:
			 *
			 *		hvoPss  Possibility Item       OwnerDepth   Owner    Child    Next
			 *		------  ----------------           ------   -----    -----    ----
			 *	[ 0]  7423   "VIL - village"                1    7422        8       1
			 *	[ 1]  7426   "RIV - river"                  1    7422        9       2
			 *	[ 2]  7429   "SHR - mountain shrine"        1    7422        0       3
			 *	[ 3]  7432   "WIT - witch doctor's house"   1    7422        0       4
			 *	[ 4]  7435   "RIC - rice field"             1    7422        0       5
			 *	[ 5]  7438   "JGL - jungle"                 1    7422        0       6
			 *	[ 6]  7441   "PLN - plain"                  1    7422        0       7
			 *	[ 7]  7444   "OCN - ocean"                  1    7422       10       0
			 *	[ 8] 41275   "1st - First Level"            2    7423       16       0
			 *	[ 9] 41266   "Hud - Hudson"                 2    7426        0      12
			 *	[10] 41272   "Atl - Atlantic"               2    7444        0      11
			 *	[11] 41269   "Pac - Pacific"                2    7444        0      13
			 *	[12] 41263   "Miss - Mississippi"           2    7426       15       0
			 *	[13] 41317   "Ind - Indian"                 2    7444        0      14
			 *	[14] 41320   "Arc - Arctic"                 2    7444        0       0
			 *	[15] 41314   "MO - Missouri"                3   41263        0       0
			 *	[16] 41278   "2nd - Second Level"           3   41275       17       0
			 *	[17] 41290   "3rd - Third Level"            4   41278        0       0
			 *
			 * It needs to be reordered so that subitems immediately follow their owners,
			 * keeping subitems at the same level in order, like this:
			 *
			 *		hvoPss  Possibility Item       OwnerDepth   Owner    Child    Next
			 *		------  ----------------           ------   -----    -----    ----
			 *	[ 0]  7423   "VIL - village"                1    7422    8       1
			 *	[ 8] 41275   "1st - First Level"            2    7423   16       0
			 *	[16] 41278   "2nd - Second Level"           3   41275   17       0
			 *	[17] 41290   "3rd - Third Level"            4   41278    0       0
			 *	[ 1]  7426   "RIV - river"                  1    7422    9       2
			 *	[ 9] 41266   "Hud - Hudson"                 2    7426    0      12
			 *	[12] 41263   "Miss - Mississippi"           2    7426   15       0
			 *	[15] 41314   "MO - Missouri"                3   41263    0       0
			 *	[ 2]  7429   "SHR - mountain shrine"        1    7422    0       3
			 *	[ 3]  7432   "WIT - witch doctor's house"   1    7422    0       4
			 *	[ 4]  7435   "RIC - rice field"             1    7422    0       5
			 *	[ 5]  7438   "JGL - jungle"                 1    7422    0       6
			 *	[ 6]  7441   "PLN - plain"                  1    7422    0       7
			 *	[ 7]  7444   "OCN - ocean"                  1    7422   10       0
			 *	[10] 41272   "Atl - Atlantic"               2    7444    0      11
			 *	[11] 41269   "Pac - Pacific"                2    7444    0      13
			 *	[13] 41317   "Ind - Indian"                 2    7444    0      14
			 *	[14] 41320   "Arc - Arctic"                 2    7444    0       0
			 */
			if (ipss > 0)
				StorePossibilityVector(vpiix, 0, m_vpii);
			/*
			 *  Now we need to fill in the hashmap of all possibility items in the list.
			 */
			for (int i = 0; i < ipss; ++i)
			{
				// Add this item to the hashmap of all possibility items.
				int nT = MAKELONG(i, ipli);
				HvoWs hvows(m_vpii[i].m_hvoPss, m_qlpi->NormalizeWs(wsMagic));
				m_qlpi->m_hmPssWs.Insert(hvows, nT);
			}
		}
	}
	catch (...)
	{
		m_vpii.Clear();
		return false;
	}
	m_hvoPssl = hvoPssl;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Create and return a new overlay created from the possibility list.

	@param ppvo

	@return
----------------------------------------------------------------------------------------------*/
bool PossListInfo::CreateNewOverlay(IVwOverlay ** ppvo)
{
	AssertPtr(ppvo);
	Assert(!*ppvo);

	IVwOverlayPtr qvo;

	try
	{
		ComBool fIsNull;
		ComBool fMoreRows;
		HVO hvo;
		ULONG luSpaceTaken;
		IOleDbEncapPtr qode;
		IOleDbCommandPtr qodc;
		StrUni stu;
		StrUni stuAbbrev;
		StrUni stuName;
		GUID uid;

		//  Obtain a pointer to the OleDbEncap interface.
		AfDbInfo * pdbi = m_qlpi->GetDbInfo();
		pdbi->GetDbAccess(&qode);
		AssertPtr(qode);

		//  Create an instance of a VwOverlay and initialize.
		qvo.CreateInstance(CLSID_VwOverlay);
		CheckHr(qvo->put_Name(m_stuName.Bstr()));
		CheckHr(qvo->put_PossListId(m_hvoPssl));

		//  Form an SQL statement to retrieve the GUID's of the PossibilityItemIds.
		if (m_vpii.Size())
		{
			stu.Format(L"SELECT id, guid$ from CmObject where id in (%d",
				m_vpii[0].GetPssId());
			int cpii = m_vpii.Size();
			for (int ipii = 1; ipii < cpii; ipii++)
				stu.FormatAppend(L", %d", m_vpii[ipii].GetPssId());
			stu.FormatAppend(L")");
			//  Create and execute the command.
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		else
		{
			fMoreRows = FALSE;
		}

		while (fMoreRows)
		{
			// Get the id and match it up with one in the list.
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), sizeof(HVO),
				&luSpaceTaken, &fIsNull, 0));

			int ipii = GetIndexFromId(hvo);
			Assert((uint)ipii < (uint)m_vpii.Size());
			PossItemInfo & pii = m_vpii[ipii];

			//  Get the GUID data.
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&uid), sizeof(GUID),
				&luSpaceTaken, &fIsNull, 0));
			pii.GetName(stuAbbrev, kpntAbbreviation);
			pii.GetName(stuName, kpntName);
			CheckHr(qvo->SetTagInfo((OLECHAR *)&uid, hvo, kosmAll, stuAbbrev.Bstr(),
				stuName.Bstr(), pii.m_clrFore, pii.m_clrBack, pii.m_clrUnder, pii.m_unt,
				false));
			CheckHr(qodc->NextRow(&fMoreRows));
		}
	}
	catch (...)
	{
		qvo.Clear();
		return false;
	}
	*ppvo = qvo;
	AddRefObj(*ppvo);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Insert a new item in the possibility list at the indicated position.
	ipss is the index into the vector of all items in the list.

	@param ipss
	@param pszAbbr
	@param pszName
	@param pil
	@param pipssNew

	@return
----------------------------------------------------------------------------------------------*/
bool PossListInfo::InsertPss(int ipss, const OLECHAR * pszAbbr, const OLECHAR * pszName,
	PossItemLocation pil, int * pipssNew)
{
	AssertPszN(pszAbbr);
	AssertPszN(pszName);
	AssertPtrN(pipssNew);
	AssertObj(m_qlpi);
	if (!pszAbbr)
		pszAbbr = L"";
	if (!pszName)
		pszName = L"";

	int ipli;
	int cpli = m_qlpi->m_vqpli.Size();
	for (ipli = 0; ipli < cpli; ipli++)
	{
		if (m_qlpi->m_vqpli[ipli] == this)
			break;
	}
	Assert(ipli < cpli);

	int cpii = m_vpii.Size();
	int nHierLevel;
	HVO hvoOwner = m_hvoPssl;
	int tag = kflidCmPossibilityList_Possibilities;
	int ipiiIns;
	int ipii;
	HVO hvoBefore = 0;
	if (cpii == 0)
	{
		Assert(ipss <= 0);
		ipiiIns = 0;
		nHierLevel = 1;
	}
	else
	{
		Assert(ipss >= 0);
		Assert(ipss < cpii);
		nHierLevel = m_vpii[ipss].GetHierLevel();
		int nHier;

		// CreateObject2 does an insert before. The interface
		// is currently written to do an insert before or an
		// insert after, depending on the value of pil.
		if (pil == kpilBefore)
		{
			hvoBefore = m_vpii[ipss].GetPssId();
			ipiiIns = ipss;
			for (ipii = 0; ipii < ipss; ++ipii)
			{
				nHier = m_vpii[ipii].GetHierLevel();
				if (nHier < nHierLevel)
				{
					hvoOwner = m_vpii[ipii].GetPssId();
					tag = kflidCmPossibility_SubPossibilities;
				}
			}
		}
		else if (pil == kpilAfter)
		{
			hvoBefore = 0;
			ipiiIns = -1;
			for (ipii = 0; ipii < ipss; ++ipii)
			{
				nHier = m_vpii[ipii].GetHierLevel();
				if (nHier < nHierLevel)
				{
					hvoOwner = m_vpii[ipii].GetPssId();
					tag = kflidCmPossibility_SubPossibilities;
				}
			}

			// This loop in for the vector of possibility items
			for (ipii = ipss + 1; ipii < cpii; ++ipii)
			{
				nHier = m_vpii[ipii].GetHierLevel();
				if (nHier <= nHierLevel)
				{
					ipiiIns = ipii;
					// If we're on the same hierarchy level, send it the item
					// clicked. Otherwise send the SQL function a 0 so that
					// it adds the item to the end of the list for the
					// hierarchy level.
					hvoBefore = nHier == nHierLevel ? m_vpii[ipii].GetPssId(): 0;
					break;
				}
			}
			if (ipiiIns == -1)
				ipiiIns = cpii;
		}
		else if (pil == kpilUnder)
		{
			Assert(ipss < cpii);
			hvoOwner = m_vpii[ipss].GetPssId();
			tag = kflidCmPossibility_SubPossibilities;
			hvoBefore = 0;
			++nHierLevel;
			ipiiIns = ipss + 1;

			for (ipii = ipss + 1; ipii < cpii; ++ipii)
			{
				nHier = m_vpii[ipii].GetHierLevel();
				if (nHier < nHierLevel)
					break;
				ipiiIns = ipii + 1;
			}
		}
		else // pil == kpilTop
		{
			nHierLevel = 1;
			HVO hvoBefore = 0;
			ipiiIns = -1;

			// This loop is for the vector of possibility items
			for (ipii = ipss + 1; ipii < cpii; ++ipii)
			{
				nHier = m_vpii[ipii].GetHierLevel();
				if (nHier <= nHierLevel)
				{
					ipiiIns = ipii;
					// If we're on the same hierarchy level, send it the item
					// clicked. Otherwise send the SQL function a 0 so that
					// it adds the item to the end of the list for the
					// hierarchy level.
					hvoBefore = nHier == nHierLevel ? m_vpii[ipii].GetPssId(): 0;
					break;
				}
			}
			if (ipiiIns == -1)
				ipiiIns = cpii;
		}
	}
	AssertObj(m_qlpi);
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);

	// Add the new item to the database.
	// Execute the stored procedure CreateOwnedObject$(clid, hvo, guid, hvoOwner, tag, ord, 1)
	// to create a new object (with no values).
	ComBool fIsNull;
	HVO hvoPss = 0;
	StrUni stuQuery;
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	pdbi->GetDbAccess(&qode);
	try
	{
		// Start a transcation if there isn't already one open. If there is, this change is part of some
		// larger change. It would be better, probably, if undoing that would also Undo this, but we
		// haven't found time to rewrite this to go through the cache or make a custom Undo action.
		ComBool fWasTransOpen;
		CheckHr(qode->IsTransactionOpen(&fWasTransOpen));
		if (!fWasTransOpen)
			qode->BeginTrans();
		CheckHr(qode->CreateCommand(&qodc));
		qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *)&hvoPss,
			sizeof(HVO));

		stuQuery.Format(L"exec CreateOwnedObject$ %d, ? output, null, %d, %d, %d, ",
			m_nItemClsid, hvoOwner, tag, kcptOwningSequence);
		if (hvoBefore)
			stuQuery.FormatAppend(L"%d", hvoBefore);
		else
			stuQuery.FormatAppend(L"null");
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
		qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvoPss), sizeof(HVO), &fIsNull);
		qodc.Clear(); // Clear before ValidPossName.
		if (!hvoPss)
		{
			if (!fWasTransOpen)
				qode->CommitTrans();
			return false;
		}

		// Create the MultiTxt$ strings for Name and Abbreviation.
		StrUni stuName(pszName);
		StrUni stuAbbr(pszAbbr);
		StrUtil::NormalizeStrUni(stuName, UNORM_NFD);
		StrUtil::NormalizeStrUni(stuAbbr, UNORM_NFD);
		ValidPossName(-1, stuAbbr, stuName); // Gets the next valid Poss Name

		CheckHr(qode->CreateCommand(&qodc));
		stuQuery.Format(L"exec SetMultiTxt$ %d, %d, %d, ?",
			kflidCmPossibility_Name, hvoPss, m_qlpi->ActualWs(m_wsMagic));
		StrUtil::NormalizeStrUni(stuName, UNORM_NFD);
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)stuName.Chars(), stuName.Length() * sizeof(OLECHAR)));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
		stuQuery.Format(L"exec SetMultiTxt$ %d, %d, %d, ?",
			kflidCmPossibility_Abbreviation, hvoPss, m_qlpi->ActualWs(m_wsMagic));
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)stuAbbr.Chars(), stuAbbr.Length() * sizeof(OLECHAR)));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));

		// Set create/modify times and default colors.
		stuQuery.Format(L"update CmPossibility set DateCreated = getdate(), "
			L"DateModified = getdate(), ForeColor = %d, BackColor = %d, UnderColor = %d "
			L"where id = %d", kclrTransparent, kclrTransparent, kclrTransparent, hvoPss);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));

		// Add empty StTexts for all StText fields. To do this, we need to go through the
		// MetaData cache and find all flids for the CmPossibility we are creating that
		// accept StTexts.
		IFwMetaDataCachePtr qmdc;
		pdbi->GetFwMetaDataCache(&qmdc);
		AssertPtr(qmdc);
		int cflid;
		CheckHr(qmdc->GetFields(m_nItemClsid, true, kfcptOwningAtom, 0, NULL, &cflid));
		if (cflid)
		{
			ulong * prgflid = NewObj ulong[cflid];
			CheckHr(qmdc->GetFields(m_nItemClsid, true, kfcptOwningAtom, cflid, prgflid, &cflid));
			for (int iflid = 0; iflid < cflid; ++iflid)
			{
				ulong clid;
				ulong flid = prgflid[iflid];
				CheckHr(qmdc->GetDstClsId(flid, &clid));
				if (clid == kclidStText)
				{
					// We have a field that takes an StText, so create an empty text.
					HVO hvoTxt;
					CheckHr(qode->CreateCommand(&qodc));
					qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *)&hvoTxt,
						sizeof(HVO));
					stuQuery.Format(L"exec CreateOwnedObject$ %d, ? output, null, %d, %d, %d",
						kclidStText, hvoPss, flid, kcptOwningAtom);
					CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
					qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvoTxt), sizeof(HVO), &fIsNull);
					Assert(hvoTxt);
					// Add an empty paragraph.
					HVO hvoPara;
					CheckHr(qode->CreateCommand(&qodc));
					qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *)&hvoPara,
						sizeof(HVO));
					stuQuery.Format(L"exec CreateOwnedObject$ %d, ? output, null, %d, %d, %d",
						kclidStTxtPara, hvoTxt, kflidStText_Paragraphs, kcptOwningSequence);
					CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
					qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvoPara), sizeof(HVO), &fIsNull);
					Assert(hvoPara);
				}
			}
			delete[] prgflid;
		}

		// Add the new item to our internal vector of possibility items.
		PossItemInfo pii;
		pii.m_nHier = nHierLevel;
		pii.m_hvoPss = hvoPss;
		pii.m_stu.Format(L"%s - %s", stuAbbr.Chars(), stuName.Chars());
		pii.m_ws = m_qlpi->ActualWs(m_wsMagic);

		m_vpii.Insert(ipiiIns, pii);

		// Add the new item to the hashmap of all possibility items.
		int nT = MAKELONG(ipiiIns, ipli);
		HvoWs hvows(hvoPss, m_qlpi->NormalizeWs(m_wsMagic));
		m_qlpi->m_hmPssWs.Insert(hvows, nT);

		// Update the index stored in the hashmap for all items following the one we just inserted.
		for (ipii = ipiiIns + 1; ipii < cpii; ++ipii)
		{
			nT = MAKELONG(ipii, ipli);
			HvoWs hvowsT(m_vpii[ipii].m_hvoPss, m_qlpi->NormalizeWs(m_wsMagic));
			m_qlpi->m_hmPssWs.Insert(hvowsT, nT, true);
		}

		qodc.Clear(); // In case DoNotify reads the database.
		// Notify all the PossListNotify objects for this list of the insertion.
		DoNotify(kplnaInsert, hvoPss, hvoPss, ipiiIns, ipiiIns);

		if (!fWasTransOpen)
			qode->CommitTrans();
	}
	catch(...)
	{
		qodc.Clear();
		qode->RollbackTrans();
		throw;	// For now we have nothing to add, so pass it on up.
	}
	qode.Clear();

	if (pipssNew)
		*pipssNew = ipiiIns;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Create a new unique possibility list item name and abbreviation.
	Adds a (1), (2),(3) ,etc if	needed. The current poss item is excluded from this check.
	If you do not want to exclude the current item then pass a -1 in for ipss.

	@param ipss index of the current poss item
	@param pszAbbr Original Abbrevation
	@param pszName Original Name
	@return True if original names were already unique.
----------------------------------------------------------------------------------------------*/
bool PossListInfo::ValidPossName(int ipss, StrUni & stuAbbr, StrUni &  stuName)
{
	StrUni stuNewName = stuName;
	StrUni stuNewAbbr = stuAbbr;
	// Taken out by JT, 3 Mar 04, on decision (See CLE-16) that we don't want to provide an
	// initial value for new items.
	//if (stuName.Length())
	//{
	//	stuNewName = stuName;
	//}
	//else
	//{
	//	stuNewName.Load(kstidNewItem);
	//	stuName = stuNewName;
	//}
	//if (stuAbbr.Length())
	//{
	//	stuNewAbbr = stuAbbr;
	//}
	//else
	//{
	//	stuNewAbbr.Load(kstidNew);
	//	stuAbbr = stuNewAbbr;
	//}

	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stuSql;
	ComBool fIsNull;
	ComBool fMoreRows;
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetDbAccess(&qode);
	AssertPtr(qode);

	StrUni stu;
	bool fDup;
	bool fReturn = true;
	int cNewItems = 1;
	do
	{
		fDup = false;
		for (int i = 0; i < m_vpii.Size(); ++i)
		{
			if (i == ipss)
				continue;
			m_vpii[i].GetName(stu, kpntName);
			if (stu == stuName)
			{
				stuSql.Format(L"select ws from CmPossibility_Name"
					L" where obj = %d and ws = %d",
					m_vpii[i].GetPssId(), m_qlpi->ActualWs(m_wsMagic));
				CheckHr(qode->CreateCommand(&qodc));
				CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));
				if (fMoreRows)
				{
					fDup = true;
					++cNewItems;
					fReturn = false;
					break;
				}
			}
			m_vpii[i].GetName(stu, kpntAbbreviation);
			if (stu == stuAbbr)
			{
				stuSql.Format(
					L"select ws from CmPossibility_Abbreviation where obj = %d and ws = %d",
					m_vpii[i].GetPssId(), m_qlpi->ActualWs(m_wsMagic));
				CheckHr(qode->CreateCommand(&qodc));
				CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));
				if (fMoreRows)
				{
					fDup = true;
					++cNewItems;
					fReturn = false;
					break;
				}
			}
		}
		if (fDup)
		{
			stuName.Format(L"%s (%d)", stuNewName.Chars(), cNewItems);
			stuAbbr.Format(L"%s (%d)", stuNewAbbr.Chars(), cNewItems);
		}
	} while (fDup);

	return fReturn;
}

/*----------------------------------------------------------------------------------------------
	Check to see if a name or abbreviation is a unique name (not yet in list).
	The current poss item is excluded from this check, if its index is passed in via the ipss
	argument.

	@param ipss index of the current poss item (or -1 if noitem to be excluded).
	@param stuString Original input string
	@param pnt	kpntName = check name field,
				kpntAbbreviation = check abbreviation field,
				kpntNameAndAbbrev = check name and abbreviation fields.
	@param iMatch output index of item with the same name; -1 if no match.
	@return True if name is unique.
----------------------------------------------------------------------------------------------*/
bool PossListInfo::PossUniqueName(int ipss, StrUni &  stuString, PossNameType pnt, int & iMatch)
{
	StrUni stu;
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stuSql;
	ComBool fIsNull;
	ComBool fMoreRows;
	StrUni stuName;
	ComBool fFoundName = false;
	int iFoundName = -1;
	StrUni stuAbbr;
	ComBool fFoundAbbr = false;
	int iFoundAbbr = -1;

	int ich;

	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetDbAccess(&qode);
	AssertPtr(qode);

	// new code TimP
	switch (pnt)
	{
		case kpntName:
			stuName = stuString;
			break;
		case kpntAbbreviation:
			stuAbbr = stuString;
			break;
		case kpntNameAndAbbrev:
			ich = stuString.FindStr(L" - ");
			if (ich > 0)
			{
				stuAbbr = stuString.Left(ich);
				stuName = stuString.Right(stuString.Length() - ich - 3);
			}
			else
			{
				stuAbbr = stuString;
				pnt = kpntAbbreviation; // !!
			}
			break;
	}
	// Just in case the user had multiple " - " in their string ...
	ich = stuName.FindStr(L" - ");
	while (ich > 0)
	{
		stuName.Replace(ich,ich + 3,"-");
		ich = stuName.FindStr(L" - ");
	}
	ich = stuAbbr.FindStr(L" - ");
	while (ich > 0)
	{
		stuAbbr.Replace(ich,ich + 3,"-");
		ich = stuAbbr.FindStr(L" - ");
	}
	// end new code TimP

	for (iMatch = 0; iMatch < m_vpii.Size(); ++iMatch)
	{
		if (iMatch == ipss)
			continue;
		if ((pnt == kpntName) || (pnt == kpntNameAndAbbrev))
		{
			// Check Name field
			m_vpii[iMatch].GetName(stu, kpntName);
			if (stu == stuName)
			{
				stuSql.Format(L"select ws from CmPossibility_Name"
					L" where obj = %d and ws = %d",
					m_vpii[iMatch].GetPssId(), m_vpii[iMatch].GetWs());
				CheckHr(qode->CreateCommand(&qodc));
				CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));
				if (fMoreRows)
				{
					fFoundName = true;
					iFoundName = iMatch;
				}
			}
		}
		if ((pnt == kpntAbbreviation) || (pnt == kpntNameAndAbbrev))
		{
			// Check Abbr field
			m_vpii[iMatch].GetName(stu, kpntAbbreviation);
			if (stu == stuAbbr)
			{
				stuSql.Format(
					L"select ws from CmPossibility_Abbreviation where obj = %d and ws = %d",
					m_vpii[iMatch].GetPssId(), m_qlpi->ActualWs(m_wsMagic));
				CheckHr(qode->CreateCommand(&qodc));
				CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));
				if (fMoreRows)
				{
					fFoundAbbr = true;
					iFoundAbbr = iMatch;
				}
			}
		}
	}
	switch (pnt)
	{
		case kpntName:
			if (fFoundName)
			{
				iMatch = iFoundName;
				return false;
			}
			break;
		case kpntAbbreviation:
			if (fFoundAbbr)
			{
				iMatch = iFoundAbbr;
				return false;
			}
			break;
		case kpntNameAndAbbrev:
			if (fFoundName && fFoundAbbr && (iFoundName == iFoundAbbr))
			{
				iMatch = iFoundAbbr;
				return false;
			}
			break;
	}
	iMatch = -1;
	return true;
}


/*----------------------------------------------------------------------------------------------
	This merges all reference and ownings from the hvoSrc item to hvoDst item.  Then hvoSrc is
	deleted.

	@param hvoSrc Poss item that is to be merged
	@param hvoDst Poss item to merge into
	@return true.
----------------------------------------------------------------------------------------------*/
bool PossListInfo::MergeItem(HVO hvoSrc, HVO hvoDst)
{
	HashMap<GUID,GUID> hmguidTagguidPss;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	CustViewDaPtr qcvd;
	m_qlpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	pdbi->GetDbAccess(&qode);
	StrUni stuSql;

	try
	{
		qode->BeginTrans();

		int flidSrc;
		int flidDst;
		int clidSrc;
		int clidDst;
		CheckHr(qcvd->get_ObjClid(hvoSrc, &clidSrc));
		CheckHr(qcvd->get_ObjClid(hvoDst, &clidDst));
		CheckHr(qcvd->get_ObjOwnFlid(hvoSrc, &flidSrc));
		CheckHr(qcvd->get_ObjOwnFlid(hvoDst, &flidDst));
		Assert(clidSrc == clidDst);

		// Get all linked REFERENCES for merge.
		stuSql.Assign(
			L"create table [#ObjInfoTbl$]("
			L"[ObjId] int not null,"
			L"[ObjClass] int null,"
			L"[InheritDepth] int null default(0),"
			L"[OwnerDepth] int null default(0),"
			L"[RelObjId] int null,"
			L"[RelObjClass] int null,"
			L"[RelObjField] int null,"
			L"[RelOrder] int null,"
			L"[RelType] int null,"
			L"[OrdKey] varbinary(250) null default(0))");

		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

		stuSql.Format(L"GetLinkedObjs$ '%d', %d, 1, 0, 0, 0", hvoSrc, kgrfcptReference);
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

		stuSql.Assign(L"select * from [#ObjInfoTbl$] where RelObjId is not null");
		Vector<HVO> vhvoObj;
		Vector<HVO> vhvoObjR;
		Vector<int> viclidR;
		Vector<int> vflidR;
		Vector<HVO> vntypeR;
		HVO hvoObj;
		HVO hvoObjR;
		int iclidR;
		int flidR;
		int ntypeR;
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoObj),
				isizeof(hvoObj), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(&hvoObjR),
				isizeof(hvoObjR), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(6, reinterpret_cast<BYTE *>(&iclidR),
				isizeof(iclidR), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(7, reinterpret_cast<BYTE *>(&flidR),
				isizeof(flidR), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(9, reinterpret_cast<BYTE *>(&ntypeR),
				isizeof(ntypeR), &cbSpaceTaken, &fIsNull, 0));
			vhvoObj.Push (hvoObj);
			vhvoObjR.Push (hvoObjR);
			viclidR.Push (iclidR);
			vflidR.Push (flidR);
			vntypeR.Push (ntypeR);
			CheckHr(qodc->NextRow(&fMoreRows));
		}

		stuSql.Format(L"drop table [#ObjInfoTbl$]");
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

		HashMap<GUID,GUID> hmguidGuid;
		GUID uidSrc;
		GUID uidDst;
		StrUni stuCmd;
		stuCmd.Format(L"select [Guid$] from cmobject where id = %d", hvoSrc);
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		fIsNull = TRUE;
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&uidSrc), sizeof(uidSrc),
				&cbSpaceTaken, &fIsNull, 0));
		}

		if (!fIsNull)
		{
			stuCmd.Format(L"select [Guid$] from cmobject where id = %d", hvoDst);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			fIsNull = TRUE;
			if (fMoreRows)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&uidDst), sizeof(uidDst),
					&cbSpaceTaken, &fIsNull, 0));
			}
		}

		if (!fIsNull)
		{
			hmguidGuid.Insert(uidSrc, uidDst);
			AfProgressDlgPtr qprog;
			try
			{
				// Save data before calling crawler since it uses a different connection.
				pdbi->SaveAllData();	// Will most likely commit any open transaction...
				ComBool fOpen;
				CheckHr(qode->IsTransactionOpen(&fOpen));
				if (!fOpen)
					qode->BeginTrans();
				StrUni stuServer = pdbi->ServerName();
				StrUni stuDB = pdbi->DbName();

				FwDbChangeOverlayTags dsc(hmguidGuid);
				qprog.Create();
				qprog->DoModeless(NULL);
				qprog->SetRange(0, 100);
				IAdvInd3Ptr qadvi3;
				qprog->QueryInterface(IID_IAdvInd3, (void **)&qadvi3);

				IStreamPtr qfist;
				CheckHr(pdbi->GetLogPointer(&qfist));
				if (dsc.Init(stuServer, stuDB, qfist, qadvi3))
				{
					dsc.ResetConnection();
					dsc.CreateCommand();
					dsc.DoAll(kstidMergeItemPhaseOne, kstidMergeItemPhaseOne, false);
				}
				dsc.ResetConnection();
				dsc.Terminate(1);		// value doesn't matter with no relaunch!
				if (qprog)
					qprog->DestroyHwnd();
			}
			catch (...)
			{
				if (qprog)
					qprog->DestroyHwnd();
				return false;
			}
		}

		// Do the Merge on all linked REFERENCES.
		IFwMetaDataCachePtr qmdc;
		pdbi->GetFwMetaDataCache(&qmdc);
		AssertPtr(qmdc);

		for (int ipii = 0; ipii < vflidR.Size(); ++ipii)
		{
			SmartBstr sbstrName;
			SmartBstr sbstrProp;
			CheckHr(qmdc->GetOwnClsName(vflidR[ipii], &sbstrName));
			CheckHr(qmdc->GetFieldName(vflidR[ipii], &sbstrProp));
			StrUni stuSql;
			if (viclidR[ipii]== kclidCmOverlay)
			{
				// Take care if the overlay pallet
				stuSql.Format(L"if exists (select * from CmOverlay_PossItems "
					L"where src = %d) select cast(1 as tinyint) fDataExists "
					L"else select cast(0 as tinyint) fDataExists", vhvoObjR[ipii]);

				CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(qodc->GetRowset(0));
				CheckHr(qodc->NextRow(&fMoreRows));

				bool fFound;
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&fFound),
					isizeof(fFound), &cbSpaceTaken, &fIsNull, 0));
				Assert(cbSpaceTaken == isizeof(fFound));
				if (fFound)
				{
					// There is already a new item in this Overlay pallet, so delete this item from
					// the pallet.
					stuSql.Format(L"DELETE FROM %s_%s where Src = %d and Dst = %d",
						sbstrName.Chars(), sbstrProp.Chars(), vhvoObjR[ipii], hvoSrc);
					CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
					continue;
				}
			}

			if (vhvoObjR[ipii]== hvoSrc)
			{
				// Handle all FORWARD references.
				switch (vntypeR[ipii])
				{
					case kcptReferenceAtom:
					{
						stuSql.Format(L"select [%s] from [%s] where id = %d",
							sbstrProp.Chars(), sbstrName.Chars(), hvoDst);
						CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
						CheckHr(qodc->GetRowset(0));
						CheckHr(qodc->NextRow(&fMoreRows));
						int nData;
						CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&nData),
						isizeof(nData), &cbSpaceTaken, &fIsNull, 0));
						if (!nData)
						{
							// The other Poss item(hvoDst) has NO data in this field, so put this
							// data in its field.
							stuSql.Format(L"update [%s] set [%s] = %d where id = %d",
								sbstrName.Chars(), sbstrProp.Chars(), vhvoObj[ipii], hvoDst);
							CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
						}
						continue;
					}
					case kcptReferenceCollection:
					case kcptReferenceSequence:
					{
						// see if the hvoDst already has a ref from here.
						// If so the we will delete this one, else we will change this one to hvoDst.
						stuSql.Format(L"if exists (select * from %s_%s "
							L"where src = %d and dst = %d) select cast(1 as tinyint) fDataExists "
							L"else select cast(0 as tinyint) fDataExists", sbstrName.Chars(),
								sbstrProp.Chars(), hvoDst, vhvoObj[ipii]);

						CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
						CheckHr(qodc->GetRowset(0));
						CheckHr(qodc->NextRow(&fMoreRows));

						bool fFound;
						CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&fFound),
							isizeof(fFound), &cbSpaceTaken, &fIsNull, 0));
						Assert(cbSpaceTaken == isizeof(fFound));
						if (fFound)
						{
							// There is already one, so delete this one.
							stuSql.Format(L"DELETE FROM %s_%s where Src = %d and Dst = %d",
								sbstrName.Chars(), sbstrProp.Chars(), hvoSrc , vhvoObj[ipii]);
							CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
							continue;
						}
						else
						{
							// There is NOT one, so move this one to hvoDst.
							stuSql.Format(L"update %s_%s set src = %d where src = %d and dst = %d",
								sbstrName.Chars(), sbstrProp.Chars(), hvoDst, hvoSrc, vhvoObj[ipii]);
							CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
							continue;
						}
					}
				}
			}

			// Handle all BACK references.
			switch (vntypeR[ipii])
			{
			case kcptReferenceCollection:
			case kcptReferenceSequence:
				{
					// see if the hvoDst already has a ref from here.
					// If so the we will delete this one, else we will change this one to hvoDst.
					stuSql.Format(L"if exists (select * from %s_%s "
						L"where src = %d and dst = %d) select cast(1 as tinyint) fDataExists "
						L"else select cast(0 as tinyint) fDataExists", sbstrName.Chars(),
							sbstrProp.Chars(), vhvoObjR[ipii], hvoDst);

					CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
					CheckHr(qodc->GetRowset(0));
					CheckHr(qodc->NextRow(&fMoreRows));

					bool fFound;
					CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&fFound),
						isizeof(fFound), &cbSpaceTaken, &fIsNull, 0));
					Assert(cbSpaceTaken == isizeof(fFound));
					if (fFound)
					{
						// There is already one, so delete this one.
						stuSql.Format(L"DELETE FROM %s_%s where Src = %d and Dst = %d",
							sbstrName.Chars(), sbstrProp.Chars(), vhvoObjR[ipii], hvoSrc);
						CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
						continue;
					}
					else
					{
						stuSql.Format(L"update %s_%s set dst = %d where src = %d and dst = %d",
							sbstrName.Chars(), sbstrProp.Chars(), hvoDst, vhvoObjR[ipii], hvoSrc);
						CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
						continue;
					}
				}
			case kcptReferenceAtom:
				{
					stuSql.Format(L"update %s set %s = %d where %s = %d", sbstrName.Chars(),
						sbstrProp.Chars(), hvoDst, sbstrProp.Chars(), hvoSrc);
					CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
					break;
				}
			}
		}

		// Get all OWNING links for merge.
		stuSql.Assign(
			L"create table [#ObjInfoTbl$]("
			L"[ObjId] int not null,"
			L"[ObjClass] int null,"
			L"[InheritDepth] int null default(0),"
			L"[OwnerDepth] int null default(0),"
			L"[RelObjId] int null,"
			L"[RelObjClass] int null,"
			L"[RelObjField] int null,"
			L"[RelOrder] int null,"
			L"[RelType] int null,"
			L"[OrdKey] varbinary(250) null default(0))");

		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

		stuSql.Format(L"GetLinkedObjs$ '%d', %d, 0, 0, 0", hvoSrc, kgrfcptOwning);
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

		stuSql.Assign(L"select * from [#ObjInfoTbl$] where RelObjId is not null"); //order by ord???
		int iclid;
		Vector<int> viclid;
		vhvoObj.Clear();
		vhvoObjR.Clear();
		viclidR.Clear();
		vflidR.Clear();
		vntypeR.Clear();
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoObj),
				isizeof(hvoObj), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&iclid),
				isizeof(iclid), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(&hvoObjR),
				isizeof(hvoObjR), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(6, reinterpret_cast<BYTE *>(&iclidR),
				isizeof(iclidR), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(7, reinterpret_cast<BYTE *>(&flidR),
				isizeof(flidR), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(9, reinterpret_cast<BYTE *>(&ntypeR),
				isizeof(ntypeR), &cbSpaceTaken, &fIsNull, 0));
			viclid.Push (iclid);
			vhvoObj.Push (hvoObj);
			vhvoObjR.Push (hvoObjR);
			viclidR.Push (iclidR);
			vflidR.Push (flidR);
			vntypeR.Push (ntypeR);
			CheckHr(qodc->NextRow(&fMoreRows));
		}

		stuSql.Format(L"drop table [#ObjInfoTbl$]");
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

		// Do the Merge of all OWNED Objects.
		for (int ipii = 0; ipii < vflidR.Size(); ++ipii)
		{
			switch (vntypeR[ipii])
			{
			case kcptOwningCollection:
			case kcptOwningSequence:
				{
					stuSql.Format(L"exec MoveOwnedObject$ %d, %d, null, %d, %d, %d, %d",
						hvoSrc, vflidR[ipii], vhvoObj[ipii], vhvoObj[ipii],
						hvoDst,	vflidR[ipii]);
					CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
					continue;
				}
			case kcptOwningAtom:
				{
					stuSql.Format(L"select Class$ from CmObject where id = %d ",
						hvoObj);
					CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
					CheckHr(qodc->GetRowset(0));
					CheckHr(qodc->NextRow(&fMoreRows));
					int clsid;
					CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&clsid),
						isizeof(clsid), &cbSpaceTaken, &fIsNull, 0));
					Assert(cbSpaceTaken == isizeof(clsid));
					if (clsid == kclidStText)
					{
						stuSql.Format(L"select Dst from StText_Paragraphs where "
							L"src = %d order by Ord",
							hvoObj);
						CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
						CheckHr(qodc->GetRowset(0));
						CheckHr(qodc->NextRow(&fMoreRows));
						if (!fMoreRows)
							continue;
						HVO hvoSrcStPara;
						Vector<int> vHvoSrcStPara;
						while (fMoreRows)
						{
							CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoSrcStPara),
								isizeof(hvoSrcStPara), &cbSpaceTaken, &fIsNull, 0));
							vHvoSrcStPara.Push(hvoSrcStPara);
							CheckHr(qodc->NextRow(&fMoreRows));
						}

						SmartBstr sbstrName;
						SmartBstr sbstrProp;
						CheckHr(qmdc->GetOwnClsName(vflidR[ipii], &sbstrName));
						CheckHr(qmdc->GetFieldName(vflidR[ipii], &sbstrProp));

						// see if the hvoDst already has a owning in here.
						stuSql.Format(L"select Dst from %s_%s where src = %d",
							sbstrName.Chars(), sbstrProp.Chars(), hvoDst);
						CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
						CheckHr(qodc->GetRowset(0));
						CheckHr(qodc->NextRow(&fMoreRows));
						if (!fMoreRows)
						{
							// The Dst has no StText for this flid so we will move the Src StText
							// to the Dst
							stuSql.Format(L"exec MoveOwnedObject$ %d, %d, null, %d, %d, %d, %d",
								hvoSrc, vflidR[ipii], vhvoObj[ipii], vhvoObj[ipii], hvoDst,
								vflidR[ipii], NULL);

							CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
							continue;
						}
						HVO hvoDstFld;
						CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoDstFld),
							isizeof(hvoDstFld), &cbSpaceTaken, &fIsNull, 0));

						for (int ihssp = 0; ihssp < vHvoSrcStPara.Size(); ++ihssp)
						{
							stuSql.Format(L"exec MoveOwnedObject$ %d, %d, null, %d, %d, %d, %d",
								hvoObj, kflidStText_Paragraphs, vHvoSrcStPara[ihssp],
								vHvoSrcStPara[ihssp], hvoDstFld, kflidStText_Paragraphs, NULL);

							CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
						}
						continue;
					}

					SmartBstr sbstrName;
					SmartBstr sbstrProp;
					CheckHr(qmdc->GetOwnClsName(vflidR[ipii], &sbstrName));
					CheckHr(qmdc->GetFieldName(vflidR[ipii], &sbstrProp));

					// see if the hvoDst already has a owning in here.
					stuSql.Format(L"if exists (select * from %s_%s "
						L"where src = %d) select cast(1 as tinyint) fDataExists "
						L"else select cast(0 as tinyint) fDataExists",
						sbstrName.Chars(), sbstrProp.Chars(), hvoDst);

					CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
					CheckHr(qodc->GetRowset(0));
					CheckHr(qodc->NextRow(&fMoreRows));
					bool fFound;
					CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&fFound),
						isizeof(fFound), &cbSpaceTaken, &fIsNull, 0));
					Assert(cbSpaceTaken == isizeof(fFound));
					// if the Dsthvo already owns something in this field then fFound = true.

					if (fFound)
					{
						// if the Dsthvo already owns something in this field then do nothing,
						// and it will be deleted when the Obj is deleted.
						continue;
					}
					// hvoDst does not have one so move the hvoSrc to the hvoDst
					stuSql.Format(L"exec MoveOwnedObject$ %d, %d, null, %d, %d, %d, %d",
						hvoSrc, vflidR[ipii], vhvoObj[ipii], vhvoObj[ipii],
						hvoDst,	vflidR[ipii]);
					CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
					continue;
				}
			}
		}
		qode->CommitTrans();
	}
	catch(...)
	{
		qode->RollbackTrans();
		throw;	// For now we have nothing to add, so pass it on up.
	}
	qodc.Clear();
	qode.Clear();

	// Notify all the PossListNotify objects for this list of the merge
	// before the source item gets deleted.
	int nT;
	HvoWs hvows(hvoSrc, m_qlpi->NormalizeWs(m_wsMagic));
	if (m_qlpi->m_hmPssWs.Retrieve(hvows, &nT))
	{
		int ipssSrc = LOWORD(nT);
		Assert((uint)ipssSrc < (uint)m_vpii.Size());
		HvoWs hvowsT(hvoDst, m_qlpi->NormalizeWs(m_wsMagic));
		if (m_qlpi->m_hmPssWs.Retrieve(hvowsT, &nT))
		{
			int ipssDst = LOWORD(nT);
			Assert((uint)ipssDst < (uint)m_vpii.Size());
			DoNotify(kplnaMerged, hvoSrc, hvoDst, ipssSrc, ipssDst);
		}
	}

	DeletePss(hvoSrc, false);	// Database strings have already been fixed.

	return true;
}


/*----------------------------------------------------------------------------------------------
	Add the notifier to the vector of notify objects for this possibility list.
	@param ppln
----------------------------------------------------------------------------------------------*/
void PossListInfo::AddNotify(PossListNotify * ppln)
{
	AssertPtr(ppln);
	// Don't add it again if it's already there.
	for (int ipln = m_vppln.Size(); --ipln >= 0; )
	{
		if (ppln == m_vppln[ipln])
			return;
	}
	// We didn't find it, so it must be new.
	m_vppln.Push(ppln);
}


/*----------------------------------------------------------------------------------------------
	Add the notifier to the vector of notify objects for this possibility list.
	@param ppln
	@eturn true if it found the notifier in the vector.
----------------------------------------------------------------------------------------------*/
bool PossListInfo::RemoveNotify(PossListNotify * ppln)
{
	AssertPtr(ppln);
	for (int ipln = m_vppln.Size(); --ipln >= 0; )
	{
		if (ppln == m_vppln[ipln])
		{
			m_vppln.Delete(ipln);
			return true;
		}
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Call the ListChanged method on all the PossListNotify objects for this list.
	@param nAction
	@param hvoSrc
	@param hvoDst
	@param ipssSrc
	@param ipssDst
----------------------------------------------------------------------------------------------*/
void PossListInfo::DoNotify(int nAction, HVO hvoSrc, HVO hvoDst, int ipssSrc, int ipssDst)
{
	for (int ipln = m_vppln.Size(); --ipln >= 0; )
	{
		AssertPtr(m_vppln[ipln]);
		PossListNotify * ppln;
		ppln = m_vppln[ipln];
		ppln->ListChanged(nAction, m_hvoPssl, hvoSrc, hvoDst, ipssSrc, ipssDst);
	}
}


/*----------------------------------------------------------------------------------------------
	Delete the given possibility from the possibility list and update all windows.

	@param hvoPss

	@return
----------------------------------------------------------------------------------------------*/
bool PossListInfo::DeletePss(HVO hvoPss, bool fFixDbStrings)
{
	Assert(hvoPss);
#if 1
	// In order to get sync code working quicker, I'm reloading lists after a deletion is
	// made, thus eliminating this code. For now we should keep it in case we want to optimize
	// this in the future by doing a simple delete instead of reload.
	int nT;
	HvoWs hvows(hvoPss, m_qlpi->NormalizeWs(m_wsMagic));
	if (!m_qlpi->m_hmPssWs.Retrieve(hvows, &nT))
		return false;

	int ipss = LOWORD(nT);
	Assert((uint)ipss < (uint)m_vpii.Size());

	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	if (fFixDbStrings)
	{
		IOleDbEncapPtr qode;
		IOleDbCommandPtr qodc;
		pdbi->GetDbAccess(&qode);
		CheckHr(qode->CreateCommand(&qodc));
		StrUni stuCmd;
		ComBool fMoreRows;
		stuCmd.Format(L"select [Guid$] from CmObject where Id = %d", hvoPss);
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			GUID guid;
			ComBool fIsNull;
			ULONG cbSpaceTaken;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&guid), sizeof(guid),
				&cbSpaceTaken, &fIsNull, 0));
			AfProgressDlgPtr qprog;
			try
			{
				// Save data before calling crawler since it uses a different connection.
				pdbi->SaveAllData();
				StrUni stuServer = pdbi->ServerName();
				StrUni stuDB = pdbi->DbName();

				FwDbDeleteOverlayTags dsc(&guid);
				qprog.Create();
				qprog->DoModeless(NULL);
				qprog->SetRange(0, 100);
				IAdvInd3Ptr qadvi3;
				qprog->QueryInterface(IID_IAdvInd3, (void **)&qadvi3);
				IStreamPtr qfist;
				CheckHr(pdbi->GetLogPointer(&qfist));
				if (dsc.Init(stuServer, stuDB, qfist, qadvi3))
				{
					dsc.ResetConnection();
					dsc.CreateCommand();
					dsc.DoAll(kstidDeleteItemPhaseOne, kstidDeleteItemPhaseOne, false);
				}
				dsc.ResetConnection();
				dsc.Terminate(1);		// value doesn't matter with no relaunch!
				if (qprog)
					qprog->DestroyHwnd();
			}
			catch (...)
			{
				if (qprog)
					qprog->DestroyHwnd();
				return false;
			}
		}
	}

	// Delete it from the database.
	pdbi->DeleteObject(hvoPss);

	// Update all interested parties of change.
	SyncInfo sync(ksyncDelPss, m_hvoPssl, hvoPss);
	m_qlpi->StoreAndSync(sync);

	// Notify all the PossListNotify objects for this list of the deletion.
	DoNotify(kplnaDelete, hvoPss, hvoPss, ipss, ipss);

	return true;
#else
//	// Delete the item from the database.
//	m_qlpi->GetDbInfo()->DeleteObject(hvoPss);
//	// Update all lists now that we've changed.
//	SyncInfo sync(ksyncDelPss, m_hvoPssl, 0);
//	return m_qlpi->StoreAndSync(sync);
#endif
}


/*----------------------------------------------------------------------------------------------
	Synchronize all windows in this application with any changes made in the database.
	@param sync The Sync information describing a given change.
----------------------------------------------------------------------------------------------*/
bool PossListInfo::Synchronize(SyncInfo & sync)
{
	switch (sync.msg)
	{
	case ksyncDelPss:
		{
			// sync.flid is actually the hvo of the item being deleted.
			HVO hvoPss = sync.flid;
			Assert(hvoPss);
			int cpii = m_vpii.Size();
			int ipii;
			int nT;
			HvoWs hvows(hvoPss, m_qlpi->NormalizeWs(m_wsMagic));
			if (!m_qlpi->m_hmPssWs.Retrieve(hvows, &nT))
				return true; // The item isn't present, so we don't need to do anything.

			int ipli = HIWORD(nT);
			Assert((uint)ipli < (uint)m_qlpi->m_vqpli.Size() && m_qlpi->m_vqpli[ipli] == this);
			int ipss = LOWORD(nT);
			Assert((uint)ipss < (uint)m_vpii.Size());

			// Delete it from the hashmap of all possibility items.
			m_qlpi->m_hmPssWs.Delete(hvows);

			int nHier = m_vpii[ipss].GetHierLevel();

			// Delete all subitems owned by the item from the hashmap.
			for (ipii = ipss; ++ipii < cpii; )
			{
				if (m_vpii[ipii].GetHierLevel() <= nHier)
					break;
				HVO hvoT = m_vpii[ipii].GetPssId(); // Keep the compiler happy!
				HvoWs hvowsT(hvoT, m_qlpi->NormalizeWs(m_wsMagic));
				m_qlpi->m_hmPssWs.Delete(hvowsT);
			}

			// Update the index stored in the hashmap for all items following the ones
			// we just deleted.
			int ipiiT;
			int cpiiDel = ipii - ipss;
			for (ipiiT = ipii; ipiiT < cpii; ++ipiiT)
			{
				HVO hvoPssT = m_vpii[ipiiT].GetPssId();
				int nT = MAKELONG(ipiiT - cpiiDel, ipli);
				HvoWs hvowsT(hvoPssT, m_qlpi->NormalizeWs(m_wsMagic));
				m_qlpi->m_hmPssWs.Insert(hvowsT, nT, true);
			}

			// Delete the item and subitems from the internal vector of possibility items.
			while (--ipii >= ipss)
				m_vpii.Delete(ipii);

			// Notify all the PossListNotify objects for this list of the deletion.
			DoNotify(kplnaDelete, hvoPss, hvoPss, ipss, ipss);

			return true;
		}

	default:
		break;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Checks to see if the first HVO is an ancestor of the second HVO, no matter how many
	generations are skipped.

	@param hvoFirst The suspected ancestor
	@param hvoSecond The suspected descendent
	@return True if hvoSecond is descended from hvoFirst, otherwise false.
----------------------------------------------------------------------------------------------*/
bool PossListInfo::IsFirstHvoAncestor(HVO hvoFirst, HVO hvoSecond)
{
	// Check simple case
	if (hvoSecond == hvoFirst)
	{
		// We'll say one is descended from the other, even though this is not strictly true.
		return true;
	}

	// In a loop, get successive ancestors of hvoSecond:
	HVO hvoParent;
	HVO hvoChild = hvoSecond;
	bool fContinue = true;

	while (fContinue)
	{
		hvoParent = GetOwnerIdFromId(hvoChild);

		if (hvoParent <= 0)
			fContinue = false;

		if (hvoParent == hvoFirst)
		{
			// Found ancestor:
			return true;
		}
		// Prepare for next iteration:
		hvoChild = hvoParent;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	If the list is sorted, then alter the Target and PossItemLocation so that the Source will
	move into a place which preserves the sorted order.

	@param hvoSource [in] Item to move
	@param hvoTarget [in, out] Item nearest destination
	@param pil [in, out] Where moved item is to go relative to target.
	@param fHaltAtSource [in] true if search is to go only as far in the list as the source.
	@return True if all is OK, false if adjustment was not possible.

	Important note: this method assumes that if the list is supposed to be sorted, then it is
	correctly sorted. If items in the list are not in the correct order, the results may appear
	random. However, this method can be used to sort a random list. See ${PossListInfo#Sort}
	fHaltAtSource can be set to true if it is known that the source definitely does not need to
	move down the list. In that case, the target may be a higher location, or stay put.
----------------------------------------------------------------------------------------------*/
bool PossListInfo::CorrectMoveWhenSorted(HVO hvoSource, HVO & hvoTarget, PossItemLocation & pil,
	bool fHaltAtSource)
{
	if (!GetIsSorted())
		return true;

	// Get the current target:
	int iTarget = GetIndexFromId(hvoTarget);
	PossItemInfo * ppiiTarget = GetPssFromIndex(iTarget);
	AssertPtr(ppiiTarget);

	// Get the hierarchy level of the current target:
	int nTargetLevel = ppiiTarget->GetLevel(this);

	// See if the intention was to move down a level in hierarchy:
	if (pil == kpilUnder)
	{
		// See if the next item in the list is a child of the current target:
		PossItemInfo * ppiiNewTarget = GetPssFromIndex(iTarget + 1);
		if (ppiiNewTarget->GetLevel(this) > nTargetLevel)
		{
			// It is a child, so set up for inserting into child list:
			hvoTarget = ppiiNewTarget->GetPssId();
			pil = kpilAfter;
			return CorrectMoveWhenSorted(hvoSource, hvoTarget, pil, fHaltAtSource);
		}
		// Otherwise, just return with current settings:
		return true;
	}

	// Get a collater, which will be able to compare names for us, even in foreign encodings:
	ILgWritingSystemFactoryPtr qwsf;
	ILgCollatingEnginePtr qcoleng;
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	pdbi->GetLgWritingSystemFactory(&qwsf);
	CheckHr(qwsf->get_DefaultCollater(m_qlpi->ActualWs(m_wsMagic), &qcoleng));

	// Get the name of the source item:
	int iSource = GetIndexFromId(hvoSource);
	PossItemInfo * ppiiSource = GetPssFromIndex(iSource);
	AssertPtr(ppiiSource);
	StrUni stuSourceName;
	ppiiSource->GetName(stuSourceName, m_pnt);
	SmartBstr sbstrSourceName;
	stuSourceName.GetBstr(&sbstrSourceName);

	// Now find the first item in the target's sub-list:
	while (ppiiTarget->GetLevel(this) >= nTargetLevel)
	{
		iTarget--;
		if (iTarget < 0)
			break;
		ppiiTarget = GetPssFromIndex(iTarget);
		AssertPtr(ppiiTarget);
	}
	iTarget++;
	ppiiTarget = GetPssFromIndex(iTarget);
	AssertPtr(ppiiTarget);

	// Work down from top of current sub-list:
	HVO hvoLastCandidate = -1;
	bool fStillGoing = true;
	do
	{
		hvoTarget = ppiiTarget->GetPssId();
		if (fHaltAtSource && hvoTarget == hvoSource)
			return true;

		StrUni stuTargetName;
		ppiiTarget->GetName(stuTargetName, m_pnt);
		SmartBstr sbstrTargetName;
		stuTargetName.GetBstr(&sbstrTargetName);

		int nVal;
		CheckHr(qcoleng->Compare(sbstrSourceName, sbstrTargetName, fcoDefault, &nVal));

		if (nVal < 0)
		{
			// See if the previous candidate was in fact the source item:
			if (hvoLastCandidate == hvoSource)
			{
				// Target is effectively unmoved, so make sure no misleading result is returned:
				hvoTarget = hvoSource;
			}
			else
			{
				// Source must go before current target:
				pil = kpilBefore;
			}
			return true;
		}
		else if (nVal > 0 || (!fHaltAtSource && nVal == 0))
		{
			pil = kpilAfter;
			do
			{
				iTarget++;
				if (iTarget >= GetCount())
					return true;
				ppiiTarget = GetPssFromIndex(iTarget);
				if (!ppiiTarget)
					return true;
			} while (ppiiTarget->GetLevel(this) > nTargetLevel);
		}
		else // (nVal == 0) the strings were the same
			return true;

		// Get next target candidate:
		ppiiTarget = GetPssFromIndex(iTarget);
		if (!ppiiTarget)
			return true;
		if (ppiiTarget->GetLevel(this) < nTargetLevel)
			return true;
		hvoLastCandidate = hvoTarget;
	} while (fStillGoing);

	Assert(false);
	return false;
}


/*----------------------------------------------------------------------------------------------
	This method will move an item up the list if there are items before it that should be after
	it. Thus, a sorted list with a single item item in the wrong place can use this method to
	put that item in the right place.
	@param hvoItem Item to be checked/moved
	@param fNoDownMoves True if item is known not to need to move down the list.
	@return True if item was moved, false if it was in the correct place.
----------------------------------------------------------------------------------------------*/
bool PossListInfo::PutInSortedPosition(HVO hvoItem, bool fNoDownMoves)
{
	Assert(GetIsSorted());

	// See what would happen if we tried to move item to just in front of itself:
	HVO hvoNewTarget = hvoItem;
	PossItemLocation pil = kpilBefore;
	if (!CorrectMoveWhenSorted(hvoItem, hvoNewTarget, pil, fNoDownMoves))
	{
		Assert(false);
		return false;
	}
	if (hvoNewTarget != hvoItem)
	{
		// Item should be moved (we don't need to check sort order again, hence false argument):
		if (!MoveItem(hvoItem, hvoNewTarget, pil, false))
		{
			Assert(false);
			return false;
		}
		return true;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Moves the Source item from its current location to the location next to Target, depending
	on the location flag: before Target, after Target, or as a child of Target. Children of the
	item move with it. If the list is sorted, then the moved item will likely need
	reposisitioning, and this method takes care of that.

	@param hvoSource Item to move
	@param hvoTarget Item nearest destination
	@param pil Where moved item is to go relative to target.
	@param fSort Set to false if sorting is active but must be ignored here. Default = true.
	@return True if all is OK, false if move was not possible.

	One possible reason for failure is if the Target is a descendent of the Source.
	This routine works by calling stored procedure MoveOwnedObject$. Most of the rest of this
	method is to do with getting all the arguments ready for the call.
----------------------------------------------------------------------------------------------*/
bool PossListInfo::MoveItem(HVO hvoSource, HVO hvoTarget, PossItemLocation pil, bool fSort)
{
	if (IsFirstHvoAncestor(hvoSource, hvoTarget))
		return false;

	Assert (pil == kpilBefore || pil == kpilAfter || pil == kpilUnder);

	//  Obtain pointer to IOleDbEncap interface:
	AfDbInfo * pdbi = m_qlpi->GetDbInfo();
	AssertPtr(pdbi);
	IOleDbEncapPtr qode; // Declare before qodc.
	IOleDbCommandPtr qodc;
	pdbi->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));

	try
	{
		qode->BeginTrans();

		if (fSort)
			if (!CorrectMoveWhenSorted(hvoSource, hvoTarget, pil, false))
				return false;


		HVO hvoSourceOwner;
		int flidSourceOwner;
		HVO hvoTargetOwner;
		int flidTargetOwner;
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG luSpaceTaken;
		StrUni stuSql;

		// Get Owner$ and OwnFlid$ of Source and Target:
		StrUni stuFmt = L"select [Owner$], [OwnFlid$] from CmObject "
			L"where [Id] = %d";

		stuSql.Format(stuFmt.Chars(), hvoSource);
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (!fMoreRows)
			return false;

		// Fetch hvoSourceOwner:
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoSourceOwner),
			isizeof(hvoSourceOwner), &luSpaceTaken, &fIsNull, 0));
		if (fIsNull)
			return false;

		// Fetch flidSourceOwner:
		CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(&flidSourceOwner),
			isizeof(flidSourceOwner), &luSpaceTaken, &fIsNull, 0));
		if (fIsNull)
			return false;

		stuSql.Format(stuFmt.Chars(), hvoTarget);
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (!fMoreRows)
			return false;

		// Fetch hvoTargetOwner:
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoTargetOwner),
			isizeof(hvoTargetOwner), &luSpaceTaken, &fIsNull, 0));
		if (fIsNull)
			return false;

		// Fetch flidTargetOwner:
		CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(&flidTargetOwner),
			isizeof(flidTargetOwner), &luSpaceTaken, &fIsNull, 0));
		if (fIsNull)
			return false;

		bool fAppendAtEnd = false;

		if (pil == kpilAfter)
		{
			// Because stored procedure MoveOwnedObject$ will only allow positioning before the
			// specified target (or append to end of list), we have to manually change our
			// target to be the current target's next sibling.
			// Get the current target's OwnOrd$:
			int nTargetOrd;
			stuSql.Format(L"select [OwnOrd$] from CmObject where [Id] = %d",
				hvoTarget);

			CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (!fMoreRows)
				return false;

			// Fetch OwnOrd$:
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&nTargetOrd),
				isizeof(nTargetOrd), &luSpaceTaken, &fIsNull, 0));
			if (fIsNull)
				return false;

			// Get Id of item with next Ord in list:
			stuSql.Format(L"select [Id] from CmObject where [Owner$] = %d "
				L"and [OwnOrd$] = (select MIN([OwnOrd$]) from CmObject "
				L"where [Owner$] = %d and [OwnOrd$] > %d)", hvoTargetOwner, hvoTargetOwner,
				nTargetOrd);

			CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (!fMoreRows)
				fAppendAtEnd = true;
			else
			{
				// Fetch new hvoTarget:
				CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoTarget),
					isizeof(hvoTarget), &luSpaceTaken, &fIsNull, 0));
				if (fIsNull)
					return false;
			}
		}

		// Check if we're to make the source become a child of the target:
		if (pil == kpilUnder)
		{
			stuSql.Format(L"exec MoveOwnedObject$ %d, %d, null, %d, %d, %d, %d, null",
				hvoSourceOwner, flidSourceOwner, hvoSource, hvoSource, hvoTarget,
				kflidCmPossibility_SubPossibilities);
		}
		else if (fAppendAtEnd)
		{
			// Configure the procedure to append the soruce to the end of the target's list:
			stuSql.Format(L"exec MoveOwnedObject$ %d, %d, null, %d, %d, %d, %d, null",
				hvoSourceOwner, flidSourceOwner, hvoSource, hvoSource, hvoTargetOwner,
				flidTargetOwner);
		}
		else
		{
			// Configure the procedure to make the move:
			stuSql.Format(L"exec MoveOwnedObject$ %d, %d, null, %d, %d, %d, %d, %d",
				hvoSourceOwner, flidSourceOwner, hvoSource, hvoSource, hvoTargetOwner,
				flidTargetOwner, hvoTarget);
		}

		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));

		qode->CommitTrans();
	}
	catch (...)
	{
		qode->RollbackTrans();
		return false;
	}
	return FullRefresh();
}


/*----------------------------------------------------------------------------------------------
	This method is highly inefficient, but will sort a list. A hierarchical list will be sorted
	so that each sublist is sorted as a separate entity.
----------------------------------------------------------------------------------------------*/
void PossListInfo::Sort()
{
	int nLevelCount;
	int nMaxLevel = 0; // This will be adjusted inside the loop.
	// Sort items within the same level, first:
	for (nLevelCount = 0; nLevelCount <= nMaxLevel; nLevelCount++)
	{
		for (int i = 0; i < GetCount(); i++)
		{
			PossItemInfo * ppii = GetPssFromIndex(i);
			AssertPtr(ppii);
			int nLevel = ppii->GetLevel(this);
			if (nLevel > nMaxLevel)
				nMaxLevel = nLevel;

			if (nLevel == nLevelCount)
			{
				HVO hvo = ppii->GetPssId();

				// This method only need move an item up the list. It will do so if there are
				// items before it that should be after it:
				if (PutInSortedPosition(hvo, true))
				{
					// Item did move up. Advance i by the number of descendents the item has
					// (including itself):
					int iNew = GetIndexFromId(hvo);
					do
					{
						iNew++;
						i++;
						ppii = GetPssFromIndex(iNew);
						AssertPtr(ppii);
					} while (nLevel != ppii->GetLevel(this));
					i--; // Adjust down because for loop will increment
				} // End if item was moved
			} // End if current item is correct level
		} // Next index
	} // Next level
}

/*----------------------------------------------------------------------------------------------
	@param hvoPss

	@return The index of a possibility item given its id; -1 if the item is not in the list.
----------------------------------------------------------------------------------------------*/
int PossListInfo::GetIndexFromId(HVO hvoPss, PossListInfo ** pppli)
{
	int ipss;
	PossListInfoPtr qpli;
	if (!m_qlpi->GetPossListAndItem(hvoPss, m_wsMagic, &ipss, &qpli))
		return -1;
	Assert(m_hvoPssl == qpli->m_hvoPssl);
	// Note that does not increment the reference count!
	if (pppli != NULL)
		*pppli = qpli;
	return ipss;
}

/*----------------------------------------------------------------------------------------------
	Returns the id of the owner of the id passed in.
	Return 0 if it is a top level id that was passed in.
	Return -1 if the item is not in the list.

	@param hvoPss

	@return HVO of owner
----------------------------------------------------------------------------------------------*/
HVO PossListInfo::GetOwnerIdFromId(HVO hvoPss)
{
	int ipss;
	PossListInfoPtr qpli;
	if (!m_qlpi->GetPossListAndItem(hvoPss, m_wsMagic, &ipss, &qpli))
		return -1;
	//Assert(m_hvoPssl == qpli->m_hvoPssl);
	if(m_hvoPssl != qpli->m_hvoPssl)
		return -1;

	int ipii = GetIndexFromId(hvoPss);
	Assert(ipii >= 0);
	PossItemInfo * ppii = GetPssFromIndex(ipii);

	int nHier = ppii->GetHierLevel();

	if (nHier == 1)
		return 0;

	// Back up to the next higher possibility node.
	while (--ipii >= 0)
	{
		ppii = GetPssFromIndex(ipii);
		Assert(ppii);
		if (ppii->GetHierLevel() < nHier)
			return ppii->GetPssId();
	}
	Assert(ipii >= 0); // We should never get in an endless loop.
	return 0;
}

/*----------------------------------------------------------------------------------------------
	If fExactMatch is false, return the first choice that starts with the given string.
	If fExactMatch is true, return first choice that matches the entire given string.
	pnt indicates whether stu is the abbreviation, name, or combination.
	If pipii is non-NULL on entry, *pipii is set to the index of the returned item.

	@param psz The characters we are looking for at the beginning of items.
	@param loc ICU Locale of the string in psz
	@param pnt Flag to indicate whether we are looking for name, abbr, or both.
	@param fExactMatch input variable

	@return NULL if an item is not found. pii of matched item.

----------------------------------------------------------------------------------------------*/
PossItemInfo * PossListInfo::FindPss(const OLECHAR * psz, Locale & loc, PossNameType pnt,
	int * pipii, ComBool fExactMatch)
{
	AssertPsz(psz);
	AssertPtrN(pipii);
	Assert((uint)pnt < (uint)kpntLim);

#if 0 // new unfinished work.
	StrUni stuT;
	int cch = StrLen(psz);
	if (cch == 0)
	{
		// We can't successfully init a string searcher with length zero, so give up.
		// Presumably there really are no matches!
		return NULL;
	}
	UErrorCode uerr = U_ZERO_ERROR;
	UnicodeString ust(psz, cch);
	UnicodeString ustPattern;
	Normalizer::normalize(ust, UNORM_NFD, 0, ustPattern, uerr);
	Assert(U_SUCCESS(uerr));
	UCharCharacterIterator itch(psz, cch); // Temporarily set for next step.
	StringSearch * pss = new StringSearch(ustPattern, itch, loc, NULL, uerr);
	Assert(U_SUCCESS(uerr));
	RuleBasedCollator * rbc = pss->getCollator();
	rbc->setStrength(Collator::SECONDARY); // We want a caseless search.
	pss->setCollator(rbc, uerr);
	Assert(U_SUCCESS(uerr));

	int cpii = m_vpii.Size();
	for (int ipii = 0; ipii < cpii; ++ipii)
	{
		m_vpii[ipii].GetName(stuT, pnt);
		if (fExactMatch)
			if (stuT.Length() != cch)
				continue;

		itch.setText(stuT.Chars(), stuT.Length());
		pss->setText(itch, uerr);
		int ichMatch = pss->first(uerr);
		if (ichMatch == 0)
		{
			if (pipii)
				*pipii = ipii;
			delete pss;
			return &m_vpii[ipii];
		}
	}
	delete pss;
	return NULL;
#else
	StrUni stuT;
	int cch = StrLen(psz);
	if (cch == 0)
	{
		// We can't successfully init a string searcher with length zero, so give up.
		// Presumably there really are no matches!
		return NULL;
	}

	int cpii = m_vpii.Size();
	for (int ipii = 0; ipii < cpii; ++ipii)
	{
		m_vpii[ipii].GetName(stuT, pnt);
		if (fExactMatch)
			if (stuT.Length() != cch)
				continue;

		UErrorCode uerr = U_ZERO_ERROR;
		UnicodeString ustTarget(stuT.Chars());
		UnicodeString ust(psz, cch);
		UnicodeString ustPattern;
		Normalizer::normalize(ust, UNORM_NFD, 0, ustPattern, uerr);
		Assert(U_SUCCESS(uerr));
		StringSearch * pss = new StringSearch(ustPattern, ustTarget, loc, NULL, uerr);
		Assert(U_SUCCESS(uerr));
		RuleBasedCollator * rbc = pss->getCollator();
		rbc->setStrength(Collator::SECONDARY); // We want a caseless search.
		pss->setCollator(rbc, uerr);
		Assert(U_SUCCESS(uerr));
		int ichMatch = pss->first(uerr);
		delete pss;
		if (ichMatch == 0)
		{
			if (pipii)
				*pipii = ipii;
			return &m_vpii[ipii];
		}
	}
	return NULL;
#endif
}


/*----------------------------------------------------------------------------------------------
	Return the first choice that starts with the given string. pnt indicates whether stu is
	the abbreviation, name, or combination. Returns NULL if an item is not found. if prgch
	includes colons, the colons are assumed to be hierarchy separators.
	(e.g., n:p may return noun:proper)

	@param prgch The characters we are looking for at the beginning of items.
	@param loc ICU Locale of the string in prgch
	@param pnt Flag to indicate whether we are looking for name, abbr, or both.
	@param fExactMatch output variable True if match was exact.

	@return
----------------------------------------------------------------------------------------------*/
PossItemInfo * PossListInfo::FindPssHier(const OLECHAR * prgch, Locale & loc, PossNameType pnt,
	ComBool & fExactMatch)
{
	AssertPsz(prgch);
	Assert((uint)pnt < (uint)kpntLim);

	// These numbers come from the OrdKey column of the GetPossibilities MSDE stored procedure.
	// GetPossibilities returns the list depth-first.
#ifdef DEBUG
	if (m_vpii.Size())
		Assert(m_vpii[0].GetHierLevel() == 1);
#endif

	StrUni stuT;
	int nHier = 1;
	OLECHAR * pchHierBeg = const_cast<OLECHAR *>(prgch);
	OLECHAR * pchHier = pchHierBeg;
	OLECHAR * pchLim = pchHierBeg + StrLen(pchHierBeg);
	int ipii = 0;
	int cpii = m_vpii.Size();

	// following variable needed for Bob:bobby => delete "b" => Bo:booby case
	// "levels" tested from left to right
	bool fExactMatchFailedAtPreviousLevel = false;
	fExactMatch = false;
	for (;;)
	{
		// Find the end of the next substring to match.
		if (*pchHier != kchHierDelim && pchHier < pchLim)
		{
			++pchHier;
			continue;
		}

		// Try to match a string at the current level.
		for (; ipii < cpii; ++ipii)
		{
			int nCur = m_vpii[ipii].GetHierLevel();
			if (nCur > nHier)
				continue; // Skip lower items in tree.
			else if (nCur < nHier)
				return NULL; // We failed to find a match.
			// Check this item for a match.
			m_vpii[ipii].GetName(stuT, pnt);
			UErrorCode uerr = U_ZERO_ERROR;
			UnicodeString ustTarget(stuT.Chars());
			UnicodeString ust(pchHierBeg, pchHier - pchHierBeg);
			UnicodeString ustPattern;
			Normalizer::normalize(ust, UNORM_NFD, 0, ustPattern, uerr);
			Assert(U_SUCCESS(uerr));
			StringSearch * pss = new StringSearch(ustPattern, ustTarget, loc, NULL, uerr);
			Assert(U_SUCCESS(uerr));
			RuleBasedCollator * rbc = pss->getCollator();
			rbc->setStrength(Collator::SECONDARY); // We want a caseless search.
			pss->setCollator(rbc, uerr);
			Assert(U_SUCCESS(uerr));
			int ichMatch = pss->first(uerr);
			delete pss;
			if (ichMatch != 0)
				continue; // Keep looking at this level.

			// We've found a match at this level.
			if (stuT.Length() != (pchHier - pchHierBeg))
				fExactMatchFailedAtPreviousLevel = true;
			if (pchHier == pchLim)
			{
				// last level as counted from left to right
				int a = stuT.Length();
				a = StrLen(pchHier);
				if ((stuT.Length() == StrLen(pchHierBeg)) &&
					!fExactMatchFailedAtPreviousLevel)
					fExactMatch = true;
				return &m_vpii[ipii]; // We found the desired item.
			}
			// Look for match at next level of the hierarchy.
			pchHierBeg = ++pchHier; // Moves past the delimiter.
			++nHier;
			if (++ipii == cpii)
				return NULL; // There are no further poss items to match.
			if (pchHierBeg < pchLim)
				break; // Process the next substring.
			if (m_vpii[ipii].GetHierLevel() == nHier)
				return &m_vpii[ipii]; // Return the first item at the new level.
			return NULL; // No further substrings and no subitems, so fail.
		}
		if (ipii == cpii)
			return NULL; // Failed to find a match.
	}
	return NULL; // Keep the compiler happy.
}


/*----------------------------------------------------------------------------------------------
	Return the ID of the first choice that has the given helpid.
	Returns NULL if an item is not found.

	@param psz

	@return
----------------------------------------------------------------------------------------------*/
HVO PossListInfo::GetIdFromHelpId(const OLECHAR * psz)
{
	AssertPsz(psz);
	HVO hvoFound = NULL;

	try
	{
		IOleDbEncapPtr qode;
		IOleDbCommandPtr qodc;
		StrUni stuQuery;
		ComBool fIsNull;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;

		//  Obtain pointer to IOleDbEncap interface.
		m_qlpi->GetDbInfo()->GetDbAccess(&qode);
		AssertPtr(qode);
		CheckHr(qode->CreateCommand(&qodc));
		StrUni stu(psz);
		StrUtil::NormalizeStrUni(stu, UNORM_NFD);
		stuQuery.Format(L"select id from CmPossibility where helpid = '%s'",
			stu.Chars());
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoFound),
				isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
		}
	}
	catch (...)
	{
		// Do nothing here so we return NULL.
	}
	return hvoFound;
}


/*----------------------------------------------------------------------------------------------
	Reloads the possibility list.
	@return True if successful, false otherwise.
----------------------------------------------------------------------------------------------*/
bool PossListInfo::FullRefresh()
{
	// Clear out the old list without deleting it.
	PossListInfoPtr qpli;
	if (!m_qlpi->LoadPossList(m_hvoPssl, m_wsMagic, &qpli, true))
		return false;
	Assert(qpli == this); // We should be reloading the same item.
	return true;
}


//:>********************************************************************************************
//:>	AfLpInfo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfLpInfo::AfLpInfo()
{
	m_stuPrjName = L"";
	m_hvoLp = 0;
}


/*----------------------------------------------------------------------------------------------
	Initializes the item. This should be called prior to setting the item.

	@param pdbi
	@param hvoLp
----------------------------------------------------------------------------------------------*/
void AfLpInfo::Init(AfDbInfo * pdbi, HVO hvoLp)
{
	Assert(hvoLp); // Must give a valid project id.
	AssertPtr(pdbi);
	Assert(!m_qdbi); // This should only be called once.
	m_qdbi = pdbi;
	m_hvoLp = hvoLp;

	m_qacth.CreateInstance(CLSID_ActionHandler);

	m_qcvd.Attach(NewObj CustViewDa);
	AssertPtr(m_qcvd);

	// Handle synchronization initialization.
	CheckHr(CoCreateGuid(&m_guidSync)); // Initialize a new GUID for this application.
	IOleDbEncapPtr qode; // Declare before qodc.
	IOleDbCommandPtr qodc;
	unsigned long cbSpaceTaken;
	ComBool fMoreRows;
	ComBool fIsNull = true;
	int nSync;
	pdbi->GetDbAccess(&qode);
	IUndoGrouperPtr qundg;
	CheckHr(qode->QueryInterface(IID_IUndoGrouper, (void **)&qundg));
	CheckHr(m_qacth->put_UndoGrouper(qundg));
	CheckHr(qode->CreateCommand(&qodc));
	StrUni stuDbName = pdbi->DbName();
	// If we only have one connection to the database, clear the sync$ table and
	// return NULL. Otherwise store the latest id from the sync$ table in m_nLastSync.
	StrUni stuQuery = L"exec ClearSyncTable$ ?";
	CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
		(ULONG *)stuDbName.Chars(), stuDbName.Length() * 2));
	CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&nSync),
			isizeof(int), &cbSpaceTaken, &fIsNull, 0));
	}
	// Store the latest sync$ id, or 0 if none.
	m_nLastSync = fIsNull ? 0 : nSync;

	IFwMetaDataCachePtr qmdc;
	pdbi->GetFwMetaDataCache(&qmdc);

	ILgWritingSystemFactoryPtr qwsf;
	pdbi->GetLgWritingSystemFactory(&qwsf);

	m_qcvd->Init(this, qode, qmdc, qwsf, m_qacth);

}

/*----------------------------------------------------------------------------------------------
	Clear your overlay cache; they will be re-loaded on demand. This is used when the active
	languages are changed.

	@param pszName Pointer to the project name.
----------------------------------------------------------------------------------------------*/
void AfLpInfo::ClearOverlays()
{
	for (int iaoi = 0; iaoi < m_vaoi.Size(); iaoi++)
	{
		AppOverlayInfo & aoi = m_vaoi[iaoi];
		if (aoi.m_qtot)
		{
			if (aoi.m_qtot->Hwnd())
				::DestroyWindow(aoi.m_qtot->Hwnd());
			aoi.m_qtot.Clear();
		}
		aoi.m_qvo.Clear();
		// Should always be called when the tool is closed. It will be deleted and re-created
		// when next used, so we can leave it alone.
	}
}


/*----------------------------------------------------------------------------------------------
	Returns the ICU Locale object for the supplied Writing System.

	@param ws Writing system
----------------------------------------------------------------------------------------------*/
Locale AfLpInfo::GetLocale(int ws)
{
	ILgWritingSystemFactoryPtr qwsf;
	m_qdbi->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);
	IWritingSystemPtr qws;
	CheckHr(qwsf->get_EngineOrNull(ws, &qws));
	if (qws)
	{
		SmartBstr sbstrLoc;
		CheckHr(qws->get_IcuLocale(&sbstrLoc));
		StrAnsi staLoc(sbstrLoc.Chars());
		return Locale(staLoc.Chars());
	}
	else
	{
		AssertPtr(qws);
		return Locale("en");
	}
}


/*----------------------------------------------------------------------------------------------
	Set the project name, also updating the database.

	@param pszName Pointer to the project name.
----------------------------------------------------------------------------------------------*/
void AfLpInfo::SetPrjName(const OLECHAR * pszName)
{
	AssertPsz(pszName);

	// First, check whether this is the same name as before.
	StrUni stuNew(pszName);
	if (stuNew == m_stuPrjName)
		return;

	// Set the new name in memory.
	m_stuPrjName = stuNew;
	// Set the new name in the database.
	try
	{
		StrUni stuCmd;
		stuCmd.Format(L"exec SetMultiTxt$ %d, %d, %d, ?",
			kflidCmProject_Name, m_hvoLp, AnalWs());
		IOleDbEncapPtr qode;
		IOleDbCommandPtr qodc;
		AssertPtr(m_qdbi);
		m_qdbi->GetDbAccess(&qode);
		CheckHr(qode->CreateCommand(&qodc));
		// Normalizing the project name may be paranoid overkill.
		StrUtil::NormalizeStrUni(m_stuPrjName, UNORM_NFD);
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)m_stuPrjName.Chars(), m_stuPrjName.Length() * sizeof(OLECHAR)));
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
	}
	catch (...)	// Was empty.
	{
		throw;	// For now we have nothing to add, so pass it on up.
	}
}

/*----------------------------------------------------------------------------------------------
	Get the current project name from the database.

	@param pbstrProjName pointer to the output.
----------------------------------------------------------------------------------------------*/
void AfLpInfo::GetCurrentProjectName(BSTR * pbstrProjName)
{
	AssertPtr(pbstrProjName);

	*pbstrProjName = NULL;

	StrUni stuSql;
	stuSql.Format(L"SELECT Txt FROM CmProject_Name WHERE Obj=%<0>d AND Ws=%<1>d",
		m_hvoLp, m_qdbi->UserWs());		// FwProjPropertiesDlg.cs stores as user ws: see CLE-90.
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	OLECHAR rgch[MAX_PATH];
	SmartBstr sbstrProject;
	AssertPtr(m_qdbi);
	m_qdbi->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	if (!fMoreRows)
	{
		// not found for user ws, try any ws!
		stuSql.Format(L"SELECT Txt FROM CmProject_Name WHERE Obj=%<0>d", m_hvoLp);
		CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	Assert(fMoreRows); // This should always return something.
	CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgch), isizeof(rgch), &cbSpaceTaken,
		&fIsNull, 2));
	Assert(!fIsNull);	// This should always return something.
	if (cbSpaceTaken > isizeof(rgch))
	{
		Vector<OLECHAR> vch;
		vch.Resize(cbSpaceTaken / isizeof(OLECHAR));
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(vch.Begin()),
			vch.Size() * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
		sbstrProject.Assign(vch.Begin());
	}
	else
	{
		sbstrProject.Assign(rgch);
	}
	qodc.Clear();
	*pbstrProjName = sbstrProject.Detach();
}

/*----------------------------------------------------------------------------------------------
	Clear out the pointers to the Language Projects used by this database.
----------------------------------------------------------------------------------------------*/
void AfLpInfo::CleanUp()
{
	if (m_qacth)
	{
		m_qacth->Close();
		m_qacth.Clear();
	}
	m_qdsts.Clear();
	m_qdbi.Clear();
	if (m_qcvd)
	{
		m_qcvd->Close();
		m_qcvd.Clear();
	}

	int caoi = m_vaoi.Size();
	for (int iaoi = 0; iaoi < caoi; iaoi++)
	{
		m_vaoi[iaoi].m_qvo.Clear();
		m_vaoi[iaoi].m_qtot.Clear();
	}
	m_vaoi.Clear();

	// Release the reference to all the possibility lists that are cached.
	while (m_vqpli.Size())
	{
		AssertPtr(m_vqpli[0]);
		ReleasePossList(m_vqpli[0]->GetPsslId(), m_vqpli[0]->GetWs());
	}
}


/*----------------------------------------------------------------------------------------------
	@return pointer to the stylesheet.
----------------------------------------------------------------------------------------------*/
AfStylesheet * AfLpInfo::GetAfStylesheet()
{
	return m_qdsts.Ptr();
//	AfStylesheet * pasts = dynamic_cast<AfStylesheet *>(m_qdsts.Ptr());
//	Assert(pasts);
//	return pasts;
}


/*----------------------------------------------------------------------------------------------
	Load the possibility list if it is not already loaded. If the possibility list is already
	loaded, but with a different writing system, release it and load the possibility list with the
	new writing system. Can also be used to force a refresh e.g. after a drag and drop operation
	@param hvoPssl Id of the possibility list
	@param ws Writing system we want to return. It has the following possibilities:
		kwsAnal or first writing system in CurAnalysisWss -> Use the first writing system if found,
			or substitute any other writing system that is available for the item.
		kwsVern or first writing system in CurVernWss -> Use the first writing system if found,
			or substitute any other writing system that is available for the item.
		anything else -> Use the specified writing system or *** if missing.
	@param pppli Address to receive the PossListInfo for the list.
	@param fRefresh If true then force a refresh of the list from database. Default = false.
	@return True if successful. False if something failed.
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::LoadPossList(HVO hvoPssl, int wsMagic, PossListInfo ** pppli, bool fRefresh)
{
	AssertPtr(pppli);
	Assert(!*pppli);
	PossListInfoPtr qpli;
	bool fListChanged = false;

/*	int cpli = m_vqpli.Size();
	int ipli;
	for (ipli = 0; ipli < cpli; ++ipli)
	{
		if (m_vqpli[ipli]->GetPsslId() == hvoPssl)
		{
			bool fLoaded = false;
			// The desired list is already present. We need to check for a valid writing
			// system. Since the load produces identical results for single vs multiple
			// encodings for analysis and vernacular, we won't hold separate lists for
			// these differences. Anything else must match exactly, for now.
			int wsLoaded = m_vqpli[ipli]->GetWs(); // This is typically a magic value.
			switch (wsMagic)
			{
			case kwsAnal:
			case kwsAnals:
				if (kwsAnal == wsLoaded || kwsAnals == wsLoaded)
					fLoaded = true; // These lists load the same thing into the cache.
				break;
			case kwsVern:
			case kwsVerns:
				if (kwsVern == wsLoaded || kwsVerns == wsLoaded)
					fLoaded = true; // These lists load the same thing into the cache.
				break;
			default:
				fLoaded = wsMagic == wsLoaded;
				break;
			}
			if (fLoaded && !fRefresh)
			{
				// We have the correct writing system and don't need to refresh,
				// so return the list.
				*pppli = m_vqpli[ipli];
				AddRefObj(*pppli);
				return true; // List already loaded with correct writing system.
			}

			// We either have the wrong writing system or we want to refresh the current
			// writing system. If we have the wrong writing system, we don't want to do
			// anything with the existing lists, but we need to load another copy of the
			// list with the correct writing sytem. If we want to refresh the list, we
			// need to discard the old list contents, and then reload the same PossListInfo
			// with the new information.
			if (fRefresh)
			{
				qpli = m_vqpli[ipli]; // Hang on to the pli so we can reuse it.
				if (!ReleasePossList(hvoPssl, wsLoaded))
					return false;
				ipli = m_vqpli.Size();
				fListChanged = true;
				goto LLoad;
			}
		}
	}

	// We didn't have the desired list, so create a new one.
	qpli.Create();

LLoad:
	// Load/reload the list and return it.
	if (!qpli->LoadPossList(this, hvoPssl, wsMagic, ipli))
		return false;
	m_vqpli.Insert(ipli, qpli);
	if (fListChanged)
		qpli->DoNotify(kplnaReload, 0, 0, 0, 0);
	*pppli = qpli.Detach();
	return true;*/


	int cpli = m_vqpli.Size();
	int ipli;
	int wsNorm = NormalizeWs(wsMagic);
	for (ipli = 0; ipli < cpli; ++ipli)
	{
		if (m_vqpli[ipli]->GetPsslId() == hvoPssl)
		{
			// The desired list is already present. We need to check for a valid writing
			// system. Since the load produces identical results for single vs multiple
			// encodings for analysis and vernacular, we won't hold separate lists for
			// these differences. Anything else must match exactly, for now.
			int wsLoaded = m_vqpli[ipli]->GetWs(); // This is typically a magic value.
			if (wsNorm == wsLoaded && !fRefresh)
			{
				// We have the correct writing system and don't need to refresh,
				// so return the list.
				*pppli = m_vqpli[ipli];
				AddRefObj(*pppli);
				return true; // List already loaded with correct writing system.
			}
		}
	}

	// Either the list isn't present, or we want to force it to be refreshed.
	for (ipli = 0; ipli < cpli; ++ipli)
	{
		if (m_vqpli[ipli]->GetPsslId() == hvoPssl)
		{
			// The desired list is already present. We need to check for a valid writing
			// system. Since the load produces identical results for single vs multiple
			// encodings for analysis and vernacular, we won't hold separate lists for
			// these differences. Anything else must match exactly, for now.
			int wsLoaded = m_vqpli[ipli]->GetWs(); // This is typically a magic value.
			if (wsNorm == wsLoaded)
			{
				qpli = m_vqpli[ipli]; // Hang on to the pli so we can reuse it.
				if (!ReleasePossList(hvoPssl, wsLoaded))
					return false;
				ipli = m_vqpli.Size();
				fListChanged = true;
				goto LLoad;
			}
		}
	}

	// We didn't have the desired list, so create a new one.
	qpli.Create();

LLoad:
	// Load/reload the list and return it.
	if (!qpli->LoadPossList(this, hvoPssl, wsNorm, ipli))
		return false;
	m_vqpli.Insert(ipli, qpli);
	if (fListChanged)
		qpli->DoNotify(kplnaReload, 0, 0, 0, 0);
	*pppli = qpli.Detach();
	return true;
}

/*----------------------------------------------------------------------------------------------
	CleanUp the application.
	@return true if load succeeds
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::LoadCustomLists()
{
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;

	AssertPtr(m_qdbi);
	m_qdbi->GetDbAccess(&qode);
	// Load any custom lists that there are.
	StrUni stu(L"select cpl.ID from CmPossibilityList cpl "
		L"join CmObject co on cpl.id = co.id "
		L"where co.Owner$ is NULL");
	CheckHr(qode->CreateCommand(&qodc));
	CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));

	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		HVO hvo;
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo),
			isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
		m_vhvoPsslIds.Push(hvo);
		CheckHr(qodc->NextRow(&fMoreRows));
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Load the possibility list for a given item. If the list is already loaded, this reloads
	the list. Normally GetPossListAndItem should be called instead of this since it will
	first check to see if the list is already loaded.

	@param hvoPss
	@param ws
	@param pppli

	@return
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::LoadPossListForItem(HVO hvoPss, int wsMagic, PossListInfo ** pppli)
{
	AssertPtr(pppli);

	HVO hvoPssl = 0;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stu;

	try
	{
		/*
			declare @uid uniqueidentifier
			exec GetObjInOwnershipPathWithId$ @uid output, 1747, 8
			select RelObjId pssl from ObjInfoTbl$
			where uid = @uid
			exec CleanObjInfoTbl$ @uid
		*/
		stu.Format(L"declare @uid uniqueidentifier "
			L"exec GetObjInOwnershipPathWithId$ @uid output, ?, %d; "
			L"select RelObjId pssl from ObjInfoTbl$ where uid = @uid; "
			L"exec CleanObjInfoTbl$ @uid", kclidCmPossibilityList);

		//  Create and execute command.
		m_qdbi->GetDbAccess(&qode);
		AssertPtr(qode);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
			reinterpret_cast<ULONG *>(&hvoPss), sizeof(HVO)));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (!fMoreRows)
			return false;
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvoPssl),
			isizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
		if (fIsNull)
			return false;
		Assert(hvoPssl);
		CheckHr(qodc->GetRowset(0));
		return LoadPossList(hvoPssl, wsMagic, pppli);
	}
	catch (...)
	{
		Assert(false);
		return false;
	}
}


/*----------------------------------------------------------------------------------------------
	Retrieve the pointer to a possibility item and the possibility list it is a part of
	based on the possibility ID and writing system. pppli can be NULL if it is not needed.

	@param hvoPss
	@param ws
	@param pppii
	@param pppli

	@return
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::GetPossListAndItem(HVO hvoPss, int ws, PossItemInfo ** pppii,
	PossListInfo ** pppli)
{
	AssertPtr(pppii);
	AssertPtrN(pppli);

	PossListInfoPtr qpli;
	int ipss;
	if (!GetPossListAndItem(hvoPss, ws, &ipss, &qpli))
		return false;
	AssertPtr(qpli);
	*pppii = (qpli)->GetPssFromIndex(ipss);

	if (pppli)
	{
		*pppli = qpli;
		AddRefObj(*pppli);
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Retrieve the pointer to a possibility item index and the possibility list it is a part of
	based on the possibility ID and writing system. pppli can be NULL if it is not needed.

	@param hvoPss The id of the possibility item we are looking for.
	@param wsMagic This is the writing system for the list containing hvoPss.
	@param pipss Index of the item in the list.
	@param pppli Pointer to the list containing the item.

	@return
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::GetPossListAndItem(HVO hvoPss, int wsMagic, int * pipss, PossListInfo ** pppli)
{
	AssertPtr(pipss);
	AssertPtrN(pppli);
	Assert(hvoPss > 0);

	int nT;
	HvoWs hvows(hvoPss, NormalizeWs(wsMagic));
	if (!m_hmPssWs.Retrieve(hvows, &nT))
	{
		// If the item isn't found, try loading the list that owns the item.
		PossListInfoPtr qpli;
		LoadPossListForItem(hvoPss, wsMagic, &qpli);
		if (!m_hmPssWs.Retrieve(hvows, &nT))
			return false; // hvoPss must not be a valid CmPossibility.
	}

	int ipli = HIWORD(nT);
	Assert((uint)ipli < (uint)m_vqpli.Size());
	if (pppli)
	{
		*pppli = m_vqpli[ipli];
		AddRefObj(*pppli);
	}

	*pipss = LOWORD(nT);
	Assert((uint)*pipss < (uint)m_vqpli[ipli]->GetCount());

	return true;
}


/*----------------------------------------------------------------------------------------------
	Retrieve the pointer to a possibility list if it exists. Unlike the other methods, this
	will not load the list if it doesn't exist, it will just return NULL.
	@param hvoPssl Id for the desired possibility list.
	@param ws Magic Writing system of the desired possibility list, or 0 if encoding doesn't matter.
	@param pppli Address to receive the resulting list, or NULL if result not wanted.
	@return true if found, false if not available.
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::GetPossList(HVO hvoPssl, int wsMagic, PossListInfo ** pppli)
{
	AssertPtrN(pppli);
	Assert(hvoPssl);

	int ipli;
	for (ipli = m_vqpli.Size(); --ipli >= 0; )
	{
		if (m_vqpli[ipli]->GetPsslId() == hvoPssl)
		{
			bool fLoaded = false;
			// The desired list is already present. We need to check for a valid writing
			// system. Since the load produces identical results for single vs multiple
			// encodings for analysis and vernacular, we won't hold separate lists for
			// these differences. Anything else must match exactly, for now.
			int wsLoaded = m_vqpli[ipli]->GetWs(); // This is typically a magic value.
			switch (wsMagic)
			{
			case kwsAnal:
			case kwsAnals:
				if (kwsAnal == wsLoaded || kwsAnals == wsLoaded)
					fLoaded = true; // These lists load the same thing into the cache.
				break;
			case kwsVern:
			case kwsVerns:
				if (kwsVern == wsLoaded || kwsVerns == wsLoaded)
					fLoaded = true; // These lists load the same thing into the cache.
				break;
			default:
				fLoaded = wsMagic == wsLoaded;
				break;
			}
			if (wsMagic == 0 || fLoaded)
				break;
		}
	}

	if (ipli < 0)
	{
		if (pppli)
			*pppli = NULL;
		return false;
	}

	if (pppli)
	{
		*pppli = m_vqpli[ipli];
		AddRefObj(*pppli);
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	If the given possibility list is loaded, this sets the dirty flag in the possibility list
	info and removes the possibility list from the vector.

	@param hvoPssl The id of the list.
	@param wsMagic The writing system associated with this list.

	@return
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::ReleasePossList(HVO hvoPssl, int wsMagic)
{
	int cpli = m_vqpli.Size();
	for (int ipli = 0; ipli < cpli; ipli++)
	{
		if (m_vqpli[ipli]->GetPsslId() == hvoPssl)
		{
			bool fLoaded = false;
			// The desired list is already present. We need to check for a valid writing
			// system. Since the load produces identical results for single vs multiple
			// encodings for analysis and vernacular, we won't hold separate lists for
			// these differences. Anything else must match exactly, for now.
			int wsLoaded = m_vqpli[ipli]->GetWs(); // This is typically a magic value.
			switch (wsMagic)
			{
			case kwsAnal:
			case kwsAnals:
				if (kwsAnal == wsLoaded || kwsAnals == wsLoaded)
					fLoaded = true; // These lists load the same thing into the cache.
				break;
			case kwsVern:
			case kwsVerns:
				if (kwsVern == wsLoaded || kwsVerns == wsLoaded)
					fLoaded = true; // These lists load the same thing into the cache.
				break;
			default:
				fLoaded = wsMagic == wsLoaded;
				break;
			}
			if (!fLoaded)
				continue; // We don't match this one.
			// Remove the possibilities in this list from the hashmap.
			PossListInfo * ppli = m_vqpli[ipli];
			AssertPtr(ppli);
			int cpss = ppli->GetCount();
			int ipss;
			PossItemInfo * ppii;
			HVO hvoPss;
			for (ipss = 0; ipss < cpss; ipss++)
			{
				ppii = ppli->GetPssFromIndex(ipss);
				AssertPtr(ppii);
				hvoPss = ppii->GetPssId();
				HvoWs hvows(hvoPss, NormalizeWs(wsLoaded));
				m_hmPssWs.Delete(hvows);
			}
			m_vqpli[ipli].Clear();
			m_vqpli.Delete(ipli);
			// Update the hashmap for all the items in all the following possibility lists.
			--cpli;
			for (; ipli < cpli; ++ipli)
			{
				ppli = m_vqpli[ipli];
				cpss = ppli->GetCount();
				int nT;
				int ws = ppli->GetWs();
				for (ipss = 0; ipss < cpss; ++ipss)
				{
					ppii = ppli->GetPssFromIndex(ipss);
					AssertPtr(ppii);
					hvoPss = ppii->GetPssId();
					nT = MAKELONG(ipss, ipli);
					HvoWs hvows(hvoPss, ws);
					m_hmPssWs.Insert(hvows, nT, true);
				}
			}
			return true;
		}
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Return the cached overlay, or, if it has not been loaded yet, load it now.

	@param ivo
	@param ppvo

	@return
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::GetOverlay(int ivo, IVwOverlay ** ppvo)
{
	AssertPtr(ppvo);
	Assert((uint)ivo < (uint)m_vaoi.Size());
	if (!m_vaoi[ivo].m_qvo)
	{
		// The overlay has not been loaded yet, so load it now.
		try
		{
			int wsMagic = GetPsslWsFromDb(m_vaoi[ivo].m_hvoPssl);
			PossListInfoPtr qpli;
			// I'm (KenZ) not sure why this is here. Apparently just to check for
			// a programming error, since it isn't being used below.
			if (!LoadPossList(m_vaoi[ivo].m_hvoPssl, wsMagic, &qpli))
				ThrowHr(WarnHr(E_FAIL));

			// Set the overlay information.
			IVwOverlayPtr qvo;
			qvo.CreateInstance(CLSID_VwOverlay);
			CheckHr(qvo->put_Name(m_vaoi[ivo].m_stuName.Bstr()));
			CheckHr(qvo->put_PossListId(m_vaoi[ivo].m_hvoPssl));
			int fof;
			StrUni stuFont;
			int dympFont;
			int ctagMax;
			g_tog.GetGlobalOverlayValues(fof, stuFont, dympFont, ctagMax);
			CheckHr(qvo->put_Flags((VwOverlayFlags)fof));
			CheckHr(qvo->put_FontName(stuFont.Bstr()));
			CheckHr(qvo->put_FontSize(dympFont));
			CheckHr(qvo->put_MaxShowTags(ctagMax));

			IOleDbEncapPtr qode;
			IOleDbCommandPtr qodc;
			StrUni stuQuery;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;

			// Obtain pointer to IOleDbEncap interface.
			AssertPtr(m_qdbi);
			m_qdbi->GetDbAccess(&qode);

			// Load the tag information for this overlay from the database.
			GUID uid;
			int pssId;
			wchar rgchBuffer[MAX_PATH];
			StrUni stuAbbrev;
			StrUni stuName;
			COLORREF clrFore;
			COLORREF clrBack;
			COLORREF clrUnder;
			int unt;
			bool fHidden;
			CheckHr(qode->CreateCommand(&qodc));
			stuQuery.Format(L"exec GetTagInfo$ %d, %d", m_vaoi[ivo].m_hvo, wsMagic);
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			while (fMoreRows)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&uid),
					isizeof(uid), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&pssId),
					isizeof(pssId), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(rgchBuffer),
					isizeof(rgchBuffer), &cbSpaceTaken, &fIsNull, 2));
				stuAbbrev.Assign(rgchBuffer);
				CheckHr(qodc->GetColValue(4, reinterpret_cast<BYTE *>(rgchBuffer),
					isizeof(rgchBuffer), &cbSpaceTaken, &fIsNull, 2));
				stuName.Assign(rgchBuffer);
				CheckHr(qodc->GetColValue(5, reinterpret_cast<BYTE *>(&clrFore),
					isizeof(clrFore), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(6, reinterpret_cast<BYTE *>(&clrBack),
					isizeof(clrBack), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(7, reinterpret_cast<BYTE *>(&clrUnder),
					isizeof(clrUnder), &cbSpaceTaken, &fIsNull, 0));
				int nT;
				CheckHr(qodc->GetColValue(8, reinterpret_cast<BYTE *>(&nT),
					isizeof(nT), &cbSpaceTaken, &fIsNull, 0));
				unt = nT & ((1 << (cbSpaceTaken * 8)) - 1);
				CheckHr(qodc->GetColValue(9, reinterpret_cast<BYTE *>(&nT),
					isizeof(nT), &cbSpaceTaken, &fIsNull, 0));
				fHidden = (nT & ((1 << (cbSpaceTaken * 8)) - 1)) != 0;

				CheckHr(qvo->SetTagInfo((OLECHAR *)&uid, pssId, kosmAll,
					stuAbbrev.Bstr(), stuName.Bstr(), clrFore, clrBack, clrUnder,
					unt, fHidden));

				CheckHr(qodc->NextRow(&fMoreRows));
			}

			m_vaoi[ivo].m_qvo = qvo;
		}
		catch (...)
		{
			return false;
		}
	}

	*ppvo = m_vaoi[ivo].m_qvo;
	AddRefObj(*ppvo);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Retrieve the overlay index from a overlay toolwindow.

	@param ptot

	@return
----------------------------------------------------------------------------------------------*/
int AfLpInfo::GetOverlayIndex(AfTagOverlayTool * ptot)
{
	AssertPtr(ptot);

	int ctot = m_vaoi.Size();
	for (int itot = 0; itot < ctot; itot++)
	{
		if (m_vaoi[itot].m_qtot == ptot)
			return itot;
	}

	Assert(false); // This should never happen.
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Remove an overlay from the vector of overlays.

	@param ivo
----------------------------------------------------------------------------------------------*/
void AfLpInfo::RemoveOverlay(int ivo)
{
	Assert((uint)ivo < (uint)m_vaoi.Size());
	if (m_vaoi[ivo].m_qtot)
	{
		::DestroyWindow(m_vaoi[ivo].m_qtot->Hwnd());
		m_vaoi[ivo].m_qtot.Clear();
	}
	m_vaoi.Delete(ivo);
}


/*----------------------------------------------------------------------------------------------
	Show/hide an overlay.

	@param ivo
	@param polb
	@param hwndOwner
	@param fShow
	@param prc
----------------------------------------------------------------------------------------------*/
void AfLpInfo::ShowOverlay(int ivo, AfOverlayListBar * polb, HWND hwndOwner, bool fShow,
	RECT * prc)
{
	Assert((uint)ivo < (uint)m_vaoi.Size());
	AssertPtrN(prc);

	AppOverlayInfo & aoi = m_vaoi[ivo];

	if (fShow)
	{
		AssertPtr(polb);
		// If the window has already been created, check to see if it was created with
		// another owner window. Since we can't transfer its ownership to the new
		// window, we need to destroy it here and create it again with the new owner.
		if (aoi.m_qtot)
		{
			if (! aoi.m_qtot->Hwnd() ||
				(::GetWindow(aoi.m_qtot->Hwnd(), GW_OWNER) != hwndOwner))
			{
				if (aoi.m_qtot->Hwnd())
					::DestroyWindow(aoi.m_qtot->Hwnd());
				aoi.m_qtot.Clear();
			}
		}
		if (!aoi.m_qtot)
		{
			// Now create a new overlay window.
			IVwOverlayPtr qvo;
			GetOverlay(ivo, &qvo);
			polb->CreateOverlayTool(&aoi.m_qtot);
			aoi.m_qtot->Create(polb, hwndOwner, this, qvo);
		}

		HWND hwndT = aoi.m_qtot->Hwnd();
		if (prc)
		{
			Rect rc = *prc;
			::MoveWindow(hwndT, rc.left, rc.top, rc.Width(), rc.Height(), true);
		}

		::ShowWindow(hwndT, SW_SHOW);
	}
	else
	{
		// We want to hide the window instead of destroying it so it will load quicker
		// next time the window gains focus again. It will get destroyed if needed
		// when it gets shown again (see comment above when fShow is true).
		// OLD COMMENT: We destroy the window here instead of just hiding it because the next time the
		// window needs to be shown, it might need to be owned by a new top level window,
		// and there is no way to change this once the tool window has been created.
		if (aoi.m_qtot && aoi.m_qtot->Hwnd())
		{
			::ShowWindow(aoi.m_qtot->Hwnd(), SW_HIDE);
			/*::DestroyWindow(aoi.m_qtot->Hwnd());
			aoi.m_qtot.Clear();*/
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Fills a vector with a unique list of encodings from the set of possible analysis and
	possible vernacular encodings.
	@param vws Reference to a vector of integers to receive the list of encodings.
----------------------------------------------------------------------------------------------*/
void AfLpInfo::ProjectWritingSystems(Vector<int> & vws)
{
	vws.Clear();
	int iws;
	int cws = m_vwsAllAnal.Size();
	// Start out with the list of possible analysis encodings.
	// We assume here that there are no duplicates in m_vwsAllAnal.
	for (iws = 0; iws < cws; ++iws)
		vws.Push(m_vwsAllAnal[iws]);
	// Now add possible vernacular encodings, as long as they aren't already in the vector.
	for (iws = 0; iws < m_vwsAllVern.Size(); ++iws)
	{
		int ws = m_vwsAllVern[iws];
		int iwsT;
		for (iwsT = 0; iwsT < cws; ++iwsT)
			if (m_vwsAllAnal[iwsT] == ws)
				break;
		if (iwsT < cws)
			break; // Skip this one since it is already present.
		vws.Push(ws); // Otherwise, if not present add it to the list.
	}
}


/*----------------------------------------------------------------------------------------------
	Load the four vectors with current analysis and vernacular encodings and possible analysis
	and vernacular encodings.
	@return true if successful, false if not.
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::LoadWritingSystems()
{
	// Set up the PrjIds array of ids for this project.
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stu;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;

	//  Obtain pointer to IOleDbEncap interface and execute the given SQL select command.
	AssertPtr(m_qdbi);
	m_qdbi->GetDbAccess(&qode);

	// Get the encodings for the language project.
	try
	{
		ILgWritingSystemFactoryPtr qwsf;
		m_qdbi->GetLgWritingSystemFactory(&qwsf);
		AssertPtr(qwsf);
		int wsUser;
		CheckHr(qwsf->get_UserWs(&wsUser));

		// Get the current vernacular encodings for the language project.
		stu.Format(L"select le.Id"
			L" from LangProject_CurVernWss lpc"
			L" join LgWritingSystem le on le.id = lpc.dst"
			L" where lpc.src = %d order by lpc.ord", m_hvoLp);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		m_vwsVern.Clear(); // Clear values from old project.
		while (fMoreRows)
		{
			int ws;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&ws),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			m_vwsVern.Push(ws);
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		if (!m_vwsVern.Size())
			m_vwsVern.Push(wsUser); // Use User writing system as a default

		// Get the current analysis encodings for the language project.
		stu.Format(L"select le.Id"
			L" from LangProject_CurAnalysisWss lpc"
			L" join LgWritingSystem le on le.id = lpc.dst"
			L" where lpc.src = %d order by lpc.ord",
			m_hvoLp);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		m_vwsAnal.Clear(); // Clear values from old project.
		while (fMoreRows)
		{
			int ws;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&ws),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			m_vwsAnal.Push(ws);
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		if (!m_vwsAnal.Size())
			m_vwsAnal.Push(wsUser); // Use User encodings as a default.

		// Get the current AnalVern encodings for the language project.
		m_vwsAnalVern.Clear(); // Clear values
		int iWsA;
		int iWsV;
		for (iWsA = 0; iWsA < m_vwsAnal.Size(); iWsA++)
		{
			m_vwsAnalVern.Push(m_vwsAnal[iWsA]);
		}
		for (iWsV = 0; iWsV < m_vwsVern.Size(); iWsV++)
		{
			for (iWsA = 0; iWsA < m_vwsAnal.Size(); iWsA++)
			{
				if (m_vwsAnal[iWsA] == m_vwsVern[iWsV])
					break;
			}
			if(iWsA == m_vwsAnal.Size())
				m_vwsAnalVern.Push(m_vwsVern[iWsV]);
		}

		// Get the current VernAnal writing systems for the language project.
		m_vwsVernAnal.Clear(); // Clear values from old project.
		for (iWsV = 0; iWsV < m_vwsVern.Size(); iWsV++)
		{
			m_vwsVernAnal.Push(m_vwsVern[iWsV]);
		}
		for (iWsA = 0; iWsA < m_vwsAnal.Size(); iWsA++)
		{
			for (iWsV = 0; iWsV < m_vwsVern.Size(); iWsV++)
			{
				if (m_vwsVern[iWsV] == m_vwsAnal[iWsA])
					break;
			}
			if(iWsV == m_vwsVern.Size())
				m_vwsVernAnal.Push(m_vwsAnal[iWsA]);
		}

		// Get the possible vernacular encodings for the language project.
		stu.Format(L"select le.Id"
			L" from LangProject_VernWss lpc"
			L" join LgWritingSystem le on le.id = lpc.dst"
			L" where lpc.src = %d ", m_hvoLp);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		m_vwsAllVern.Clear(); // Clear values from old project.
		while (fMoreRows)
		{
			int ws;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&ws),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			m_vwsAllVern.Push(ws);
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		if (!m_vwsAllVern.Size())
			m_vwsAllVern.Push(wsUser); // Use User encodings as a default.

		// Get the possible analysis encodings for the language project.
		stu.Format(L"select le.Id"
			L" from LangProject_AnalysisWss lpc"
			L" join LgWritingSystem le on le.id = lpc.dst"
			L" where lpc.src = %d", m_hvoLp);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		m_vwsAllAnal.Clear(); // Clear values from old project.
		while (fMoreRows)
		{
			int ws;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&ws),
				isizeof(int), &cbSpaceTaken, &fIsNull, 0));
			m_vwsAllAnal.Push(ws);
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		if (!m_vwsAllAnal.Size())
			m_vwsAllAnal.Push(wsUser); // Use User encodings as a default.
		return true;
	}
	catch (...)
	{
		return false;
	}
}


/*----------------------------------------------------------------------------------------------
	Convert an writing system (could be magic) to a real writing system.
	@param ws
	@return a real writing system
----------------------------------------------------------------------------------------------*/
int AfLpInfo::ActualWs(int ws)
{
	Assert(ws); // An writing system should always be present.
	switch (ws)
	{
	case kwsAnals:
	case kwsAnal:
	case kwsAnalVerns:
		return AnalWs();
	case kwsVerns:
	case kwsVern:
	case kwsVernAnals:
		return VernWs();
	default:
		return ws;
	}
}


/*----------------------------------------------------------------------------------------------
	Return the External Link root directory.
	@param fRefresh True if current value is presumed to be out of date.
----------------------------------------------------------------------------------------------*/
const OLECHAR * AfLpInfo::GetExtLinkRoot(bool fRefresh)
{
	if (m_stuExtLinkRoot.Length() == 0 || fRefresh)
	{
		try
		{
			// Load the External Link root directory from the database.
			IOleDbEncapPtr qode;
			IOleDbCommandPtr qodc;
			StrUni stuQuery;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			wchar rgchRoot[MAX_PATH] = { 0 };

			// Obtain pointer to IOleDbEncap interface.
			stuQuery.Format(L"select ExtLinkRootDir from LangProject lp "
				L"where lp.id = %d", GetLpId());
			m_qdbi->GetDbAccess(&qode);
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (fMoreRows)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(rgchRoot),
					isizeof(rgchRoot), &cbSpaceTaken, &fIsNull, 2));
				if (*rgchRoot)
				{
					// Make sure the root path ends with a \ character.
					int cchRoot = StrLen(rgchRoot);
					if (rgchRoot[cchRoot - 1] != '\\')
					{
						rgchRoot[cchRoot++] = '\\';
						rgchRoot[cchRoot] = 0;
					}
					m_stuExtLinkRoot = rgchRoot;
				}
			}
		}
		catch (...)
		{
			// Nothing to do here.
		}
	}
	return m_stuExtLinkRoot.Chars();
}


/*----------------------------------------------------------------------------------------------
	Convert a filename to the corresponding remote file if the user is looking at a database
	on a remote machine.
	@param strbFile
	@return true if the filename was changed to a network filename.
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::MapExternalLink(StrAppBuf & strbFile)
{
	// We're done if we're looking at a local database.
	StrUni stuLocalServer;
	achar rgch[MAX_COMPUTERNAME_LENGTH + 1];
	ulong cch = isizeof(rgch);
	::GetComputerName(rgch, &cch);
	stuLocalServer.Format(L"%s\\SILFW", rgch);
	StrUni stuServerName(m_qdbi->ServerName());
	if (stuServerName.EqualsCI(stuLocalServer))
		return false;

	// If we can't get the external link root, we can't do anything.
	if (*GetExtLinkRoot() == NULL)
		return false;

	// Check to see if the filename is within the External Link root directory.
	StrUniBuf stubFilePart(strbFile);
	if (stubFilePart.FindStrCI(m_stuExtLinkRoot) != 0)
		return false;

	// Get the actual remote machine name.
	int ich = stuServerName.FindCh('\\');
	if (ich >= 0)
	{
		wchar * pszDummy;
		stuServerName.SetSize(ich, &pszDummy);
	}

	// Find the part of the local filename that needs to be added to the remote machine name.
	ich = stubFilePart.ReverseFindCh('\\', m_stuExtLinkRoot.Length() - 2);
	if (ich < 0)
		return false;

	// Compute the corresponding path name on the remote machine.
	strbFile.Format(_T("\\\\%S%S"), stuServerName.Chars(), stubFilePart.Chars() + ich);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Convert a remote filename to the corresponding "local" file if the file and the database
	are on the same remote machine.
	@param strbFile
	@return true if the filename was changed to a "local" filename.
	example:  \\Ls-zook\Dropbox\Prototype\Documentation\Help\LLLibraryAbout.html becomes
			c:\Dropbox\Prototype\Documentation\Help\LLLibraryAbout.html in the Ls-zook database.
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::UnmapExternalLink(StrAppBuf & strbFile)
{
	// Do nothing if the filename is not a remote file spec
	int ich = strbFile.FindCh('\\');
	if (ich != 0)
		return false;

	// Do nothing if the External Link root directory IS a remote directory
	ich = m_stuExtLinkRoot.FindCh('\\');
	if (ich == 0)
		return false;

	// Get the actual remote machine name.
	StrUni stuServerName(m_qdbi->ServerName());
	int ichB = stuServerName.FindCh('\\');
	if (ichB >= 0)
	{
		wchar * pszDummy;
		stuServerName.SetSize(ichB, &pszDummy);
	}

	// file must be on same server as the database
	StrUniBuf stubFilePart(strbFile);
	ich = stubFilePart.FindStrCI(stuServerName);
	if (ich != 2)
		return false;

	// If we can't get the external link root, we can't do anything.
	if (*GetExtLinkRoot() == NULL)
		return false;

	// The filespec to the right of the remote shared directory name
	StrUniBuf stubFilePartB(strbFile.Chars() + ich + stuServerName.Length() + 1);
	ich = stubFilePartB.FindCh('\\');

	// Compose the new local machine file specification.
	strbFile.Format(_T("%S%S"), m_stuExtLinkRoot.Chars(), stubFilePartB.Chars() + ich + 1);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Synchronize all windows in this application with any changes made in the database.
	@param sync The Sync information describing a given change.
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::Synchronize(SyncInfo & sync)
{
	switch (sync.msg)
	{
	case ksyncWs:
		{
			// An writing system change basically affects everything.
			int grflpi = kflpiCache | kflpiStyles | kflpiWritingSystems | kflpiPossLists |
				kflpiOverlays;
			return FullRefresh(grflpi);
		}

	case ksyncMoveEntry:
	case ksyncPromoteEntry:
		{
			// We also need to clear the cache.
			return FullRefresh(kflpiCache | kflpiStyles | kflpiPossLists);
		}

	case ksyncMergePss:
		{
			// We need to update the possibility list, then redraw all windows. It is too
			// difficult to track changes in the view cache resulting from a merge, so we'll
			// just redraw everything.

			// Find out if any loaded overlays are using this list.
			// Do this before loading PossList.
			int iovr;
			for (iovr = m_vaoi.Size(); --iovr >= 0; )
			{
				AppOverlayInfo & aoi = m_vaoi[iovr];
				if (aoi.m_qvo && aoi.m_hvoPssl == sync.hvo)
				{
					// Clear the overlay cache so it will be reloaded from the database
					// next time it is referenced.
					aoi.m_qvo = NULL;
					break;
				}
			}
			// First see if we are using this list.
			int ipli;
			for (ipli = m_vqpli.Size(); --ipli >= 0; )
			{
				if (m_vqpli[ipli]->GetPsslId() == sync.hvo)
				{
					// We are using the list, so we need to reload.
					if (!m_vqpli[ipli]->FullRefresh())
						return false;
				}
			}
			// We also need to clear the cache.
			// We could get by without clearing the cache if the list wasn't used, but
			// at this point it is too complex to get this information to other parts
			// of this process. So we'll be consistent.
			return FullRefresh(kflpiCache | kflpiStyles);
		}

	case ksyncUndoRedo:
		{
			int grflpi = kflpiCache | kflpiStyles | kflpiOverlays | kflpiPossLists;
			return FullRefresh(grflpi);
		}

	case ksyncDelPss:
		{
			// sync.flid is actually the hvo of the item being deleted.
			Assert(sync.flid);
			// Find out if any loaded overlays are using this list.
			int iovr;
			for (iovr = m_vaoi.Size(); --iovr >= 0; )
			{
				AppOverlayInfo & aoi = m_vaoi[iovr];
				if (aoi.m_qvo && aoi.m_hvoPssl == sync.hvo)
				{
					// Clear the overlay cache so it will be reloaded from the database
					// next time it is referenced.
					aoi.m_qvo = NULL;
					break;
				}
			}
			// Find out if we have this list loaded.
			int ipli;
			for (ipli = m_vqpli.Size(); --ipli >= 0; )
			{
				if (m_vqpli[ipli]->GetPsslId() == sync.hvo)
				{
					// We are using the list.
					// First, clear the view cache of this object. This will update views.
					// Do this before updating the list
					CheckHr(m_qcvd->RemoveObjRefs(sync.hvo));
					// Now delete the item from the PossList cache. This will update interested parties.
					if (!m_vqpli[ipli]->Synchronize(sync))
						return false;
				}
			}
			return true;
		}

	case ksyncAddPss:
	case ksyncPossList:
		{
			// Find out if any loaded overlays are using this list.
			// Do this before reloading PossList.
			int iovr;
			for (iovr = m_vaoi.Size(); --iovr >= 0; )
			{
				AppOverlayInfo & aoi = m_vaoi[iovr];
				if (aoi.m_qvo && aoi.m_hvoPssl == sync.hvo)
				{
					// Clear the overlay cache so it will be reloaded from the database
					// next time it is referenced.
					aoi.m_qvo = NULL;
					break;
				}
			}
			// Find out if we have this list loaded, and if so, reload the list.
			int ipli;
			for (ipli = m_vqpli.Size(); --ipli >= 0; )
			{
				if (m_vqpli[ipli]->GetPsslId() == sync.hvo)
				{
					if (!m_vqpli[ipli]->FullRefresh())
						return false;
				}
			}
			return true;
		}

	case ksyncStyle:
		{
			return FullRefresh(kflpiStyles);
		}

	case ksyncHeadingChg:
		{
			int grflpi = kflpiProjBasics;
			return FullRefresh(grflpi);
		}

	default:
		break;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Refresh various parts of AfLpInfo.
	@param grflpi A bitmap indicating what parts of AfLpInfo to refresh (defined in
		LpiRefreshFlags.
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::FullRefresh(int grflpi)
{
	// Clear the data cache.
	if (grflpi & kflpiCache)
	{
		m_qcvd->ClearAllData();
		// If we clear the cache, we'd better reload the stylsheet.
		Assert(grflpi & kflpiStyles);
	}

	// Load the stylesheet into the data cache.
	if (grflpi & kflpiStyles)
	{
		m_qdsts->LoadStyles(m_qcvd, this);
	}

	// Load the writing system ids.
	if (grflpi & kflpiWritingSystems)
	{
		if (!LoadWritingSystems())
			return false;
		// Make sure the writing system factory has all these writing systems loaded.
		Vector<int> vws;
		ProjectWritingSystems(vws);
		IWritingSystemPtr qws;
		ILgWritingSystemFactoryPtr qwsf;
		m_qdbi->GetLgWritingSystemFactory(&qwsf);
		for (int iws = vws.Size(); --iws >= 0; )
		{
			CheckHr(qwsf->get_EngineOrNull(vws[iws], &qws));
			AssertPtr(qws.Ptr());
		}
	}

	// Load the ids for major object, standard PossLists, and CustomPossLists.
	if (grflpi & kflpiProjBasics)
	{
		if (!LoadProjBasics())
			return false;
	}

	if (grflpi & kflpiExtLink)
		GetExtLinkRoot(true); // Reload the external root directory.


	// Reload the overlay list. Do this before reloading PossLists.
	if (grflpi & kflpiOverlays)
	{
		if (!LoadOverlays())
			return false;
	}

	// Load Possibility Lists (perhaps clear, since they will load automatically? But
	// maybe we need to do something special to maintain notifications.
	if (grflpi & kflpiPossLists)
	{
		// Refresh from back to front since items are moved back in the process.
		for (int ipssl = m_vqpli.Size(); --ipssl >= 0; )
			if (!m_vqpli[ipssl]->FullRefresh())
				return false;
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Load overlays from the database. This generates dummy overlays based on
	possibility lists and saves them in the database if it can't find any to begin with.
----------------------------------------------------------------------------------------------*/
bool AfLpInfo::LoadOverlays()
{
	// If we've already loaded the overlays from the database, clear them so we can reload.
	if (m_vaoi.Size() != 0)
	{
		ClearOverlays();
		m_vaoi.Clear();
	}

	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stuQuery;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;

	//	Obtain pointer to IOleDbEncap interface.
	AssertPtr(m_qdbi);
	m_qdbi->GetDbAccess(&qode);

	try
	{
		CheckHr(qode->CreateCommand(&qodc));

		stuQuery = L"select * from CmOverlay order by name";
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));

		//--------------------------------------------------------------------------------------
		// ENHANCE JohnT (DarrellZ): Remove this once we have the initial overlays stored in the
		// database.
		//--------------------------------------------------------------------------------------
		if (!fMoreRows)
		{
			qodc.Clear();	// Prevent deadlock writing to database in g_tog.SaveOverlay().

			// There are no overlays stored in the database, so generate default ones from
			// the following possibility lists.
			int cpssl = m_vhvoPsslIds.Size();
			for (int ipssl = 0; ipssl < cpssl; ipssl++)
			{
				PossListInfoPtr qpli;
				IVwOverlayPtr qvo;
				if (!LoadPossList(m_vhvoPsslIds[ipssl], m_qdbi->UserWs(), &qpli))
					ThrowHr(WarnHr(E_FAIL));
				if (!qpli->CreateNewOverlay(&qvo))
					ThrowHr(WarnHr(E_FAIL));
				g_tog.SaveOverlay(this, qvo);
			}

			// Redo the query to catch the newly inserted overlays.
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		//--------------------------------------------------------------------------------------

		// Read the overlay information from the overlays stored in the database.
		while (fMoreRows)
		{
			int ovrId;
			int psslId;
			wchar rgchName[MAX_PATH];
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&ovrId),
				isizeof(ovrId), &cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(rgchName),
				isizeof(rgchName), &cbSpaceTaken, &fIsNull, 2));
			CheckHr(qodc->GetColValue(3, reinterpret_cast<BYTE *>(&psslId),
				isizeof(psslId), &cbSpaceTaken, &fIsNull, 0));
			StrUni stuName(rgchName);
			AddOverlay(ovrId, psslId, stuName);

			CheckHr(qodc->NextRow(&fMoreRows));
		}
	}
	catch (...)
	{
		return false;
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Store the given sync info in the database if there is more than one connection.
	@param sync The Sync information to store describing a given change.
----------------------------------------------------------------------------------------------*/
void AfLpInfo::StoreSync(SyncInfo & sync)
{
	IOleDbEncapPtr qode; // Declare before qodc.
	IOleDbCommandPtr qodc;
	m_qdbi->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));
	StrUni stuDbName = m_qdbi->DbName();
	// If we have more than one connection to the database, then add a sync record.
	StrUni stuQuery = L"StoreSyncRec$ ?, ?, ?, ?, ?";
	CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
		(ULONG *)(stuDbName.Chars()), stuDbName.Length() * 2));
	CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_GUID,
		(ULONG *)&m_guidSync, isizeof(GUID)));
	CheckHr(qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
		(ULONG *) &sync.msg, sizeof(int)));
	CheckHr(qodc->SetParameter(4, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
		(ULONG *) &sync.hvo, sizeof(HVO)));
	CheckHr(qodc->SetParameter(5, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
		(ULONG *) &sync.flid, sizeof(int)));
	CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
}


/*----------------------------------------------------------------------------------------------
	Retrieve the writing system from the db for this Possibility List.

	@param hvopssl HVO of the Possibility List that you want the writing systm for.
	@return Magic writing system for this list.
----------------------------------------------------------------------------------------------*/
int AfLpInfo::GetPsslWsFromDb(HVO hvoPssl)
{
	Assert(hvoPssl);
	StrUni stuCmd;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	AssertPtr(m_qdbi.Ptr());
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	m_qdbi.Ptr()->GetDbAccess(&qode);
	CheckHr(qode->CreateCommand(&qodc));
	stuCmd.Format(L"SELECT WritingSystem, WsSelector FROM CmPossibilityList"
		L" WHERE id = %d", hvoPssl);
	CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(qodc->GetRowset(0));
	CheckHr(qodc->NextRow(&fMoreRows));
	ComBool fRealIsNull = TRUE;
	ComBool fMagicIsNull = TRUE;
	int wsReal = 0;
	int wsMagic = 0;
	if (fMoreRows)
	{
		CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&wsReal), sizeof(wsReal),
			&cbSpaceTaken, &fRealIsNull, 0));
		CheckHr(qodc->GetColValue(2, reinterpret_cast<BYTE *>(&wsMagic), sizeof(wsMagic),
			&cbSpaceTaken, &fMagicIsNull, 0));
	}
	if (!fMagicIsNull && wsMagic != 0)
	{
		return wsMagic;
	}
	else if (wsReal != 0)
	{
		return wsReal;
	}
	else
	{
		// Both are zero. This is a bad state of the data, but rather than crash
		// we'll make a reasonable guess.
		return kwsAnals;
	}
}

// Semi-Explicit instantiation.
#include "Vector_i.cpp"
#include "HashMap_i.cpp"
#include "Set_i.cpp"
