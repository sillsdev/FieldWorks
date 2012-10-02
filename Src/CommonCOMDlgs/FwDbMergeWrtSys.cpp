/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwDbMergeWrtSys.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Implementation of FwDbMergeWrtSys.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma hdrstop
#include "main.h"
#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	IMPLEMENTATION OF FwDbMergeWrtSys.
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.FW.FwDbMergeWrtSys"),
	&CLSID_FwDbMergeWrtSys,
	_T("SIL FieldWorks FwDbMergeWrtSys"),
	_T("Apartment"),
	&FwDbMergeWrtSys::CreateCom);


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwDbMergeWrtSys::FwDbMergeWrtSys()
	: DbStringCrawler(false, true, true, false)
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwDbMergeWrtSys::~FwDbMergeWrtSys()
{
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Static method to create an instance of FwDbMergeWrtSys.
----------------------------------------------------------------------------------------------*/
void FwDbMergeWrtSys::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<FwDbMergeWrtSys> qzfwst;
	qzfwst.Attach(NewObj FwDbMergeWrtSys());	// ref count initially 1
	CheckHr(qzfwst->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:> DbStringCrawler virtual methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Scan the serialized string format (vbFmt) for runs in the writing system m_wsOld.  If any
	are found, change the run to use m_wsNew instead, and return true.  Otherwise, return false.

	@param vbFmt Reference to a byte vector containing the formatting information for the
					string.

	@return True if one or more runs had their writing system changed, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FwDbMergeWrtSys::ProcessBytes(Vector<byte> & vbFmt)
{
	Assert(sizeof(int) == 4);
	int * pn = reinterpret_cast<int *>(vbFmt.Begin());
	int crun = *pn++;
	int crunChg = 0;
	// 2 for char-min followed by prop offset
	int cbOffsets = isizeof(int) + (2 * crun * isizeof(int));
	int itip;
	int ctip;
	int ctsp;
	int irun;
	int ws;
	int scp;

	const byte * pbProp;
	const byte * pb;
	const byte * pbNext;
	Vector<int> vibProp;
	vibProp.Resize(crun);

	for (irun = 0; irun < crun; ++irun)
		vibProp[irun] = pn[2 * irun + 1];
	for (irun = 0; irun < crun; ++irun)
	{
		pbProp = vbFmt.Begin() + cbOffsets + vibProp[irun];
		pb = pbProp;
		ctip = *pb++;
		ctsp = *pb++;
		for (itip = 0; itip < ctip; ++itip)
		{
			scp = TextProps::DecodeScp(pb, vbFmt.End() - pb, &pbNext);
			if (scp == kscpWs || scp == kscpWsAndOws)
			{
				ws = *(reinterpret_cast<const int *>(pbNext));
				if (ws == m_wsOld)
				{
					byte * pbEnc = const_cast<byte *>(pbNext);
					*(reinterpret_cast<int *>(pbEnc)) = m_wsNew;
					++crunChg;
				}
			}
			switch (scp & 0x3)
			{
			case 0:
				pb = pbNext + 1;
				break;
			case 1:
				pb = pbNext + 2;
				break;
			case 2:
				pb = pbNext + 4;
				break;
			case 3:
				pb = pbNext + 8;
				break;
			}
		}
	}
	return (crunChg > 0);
}

//:>********************************************************************************************
//:>	IUnknown methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown and IFwDbMergeWrtSys are
	supported.

	This is a standard COM IUnknown method.

	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.

	@return SOK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwDbMergeWrtSys::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IFwDbMergeWrtSys *>(this));
	else if (iid == IID_IFwDbMergeWrtSys)
		*ppv = static_cast<IFwDbMergeWrtSys *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IFwDbMergeWrtSys);
		return S_OK;
	}
	else
	{
		return E_NOINTERFACE;
	}
	reinterpret_cast<IUnknown *>(*ppv)->AddRef();

	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Standard COM AddRef method.

	@return The reference count after incrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) FwDbMergeWrtSys::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Standard COM Release method.

	@return The reference count after decrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) FwDbMergeWrtSys::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}

//:>********************************************************************************************
//:>	IFwDbMergeWrtSys methods.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Initialize the string crawler / database fixup process.

	@param pfwt Pointer to the application's IFwTool interface.
	@param bstrServer Name of the database server.
	@param bstrDatabase Name of the database.
	@param pstrmLog Optional output stream for logging (may be NULL).
	@param hvoProj Database id of the FieldWorks project.
	@param hvoRootObj Database id of the program's root object.
	@param wsUser User interface writing system id.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwDbMergeWrtSys::Initialize(IFwTool * pfwt, BSTR bstrServer, BSTR bstrDatabase,
	IStream * pstrmLog, int hvoProj, int hvoRootObj, int wsUser)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pfwt);
	ChkComBstrArg(bstrServer);
	ChkComBstrArg(bstrDatabase);
	ChkComArgPtrN(pstrmLog);

	m_qfwt = pfwt;
	m_hvoProj = hvoProj;
	m_hvoRoot = hvoRootObj;
	m_wsUser = wsUser;
	m_qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stuServer(bstrServer);
	StrUni stuDatabase(bstrDatabase);
	m_qprog.Create();
	IAdvInd3Ptr qadvi3;
	m_qprog->QueryInterface(IID_IAdvInd3, (void **)&qadvi3);
	if (Init(stuServer, stuDatabase, pstrmLog, qadvi3))
	{
		// Close all affected windows and database connections, but leave the program running.
		return m_qfwt->CloseDbAndWindows(bstrServer, bstrDatabase, false);
	}
	else
	{
		Terminate(m_hvoRoot);
		ThrowHr(WarnHr(E_FAIL));
	}
	END_COM_METHOD(g_fact, IID_IFwDbMergeWrtSys);
}


/*----------------------------------------------------------------------------------------------
	Crawl through the database (established by calling Initialize earlier), changing every
	occurrence of wsOld to wsNew.  This updates various writing system lists and sort
	specifications as well as the formatted string binary format fields.

	@param wsOld Obsolete writing system id.
	@param bstrOldName Name of the obsolete writing system.
	@param wsNew Desired writing system id.
	@param bstrNewName Name of the desired writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwDbMergeWrtSys::Process(int wsOld, BSTR bstrOldName, int wsNew, BSTR bstrNewName)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrOldName);
	ChkComBstrArg(bstrNewName);
	if (!wsOld || !wsNew)
		ThrowHr(WarnHr(E_INVALIDARG));

	m_wsOld = wsOld;
	m_wsNew = wsNew;
	if (m_wsUser == wsOld)
		m_wsUser = wsNew;
	StrUni stuFmt;
	StrUni stuMsg;
	m_qprog->DoModeless(NULL);
	m_qprog->SetRange(0, 100);
	// "Merge language writing system %<0>s into %<1>s"
	stuFmt.Load(kstidMergeWrtSys);
	stuMsg.Format(stuFmt.Chars(), bstrOldName, bstrNewName);
	m_qprog->put_Title(stuMsg.Bstr());
	stuMsg.Clear();
	m_qprog->put_Message(stuMsg.Bstr());

	////////////////////////////////////////////////////////////////////////////////////
	// Munge the database as needed to change language encodings.

	ResetConnection();
	if (m_qstrmLog)
	{
		StrAnsi staLog;
		StrAnsi staFmt(kstidLogMergeWrtSys);
		staLog.Format(staFmt.Chars(),
			bstrOldName, bstrNewName, m_stuDatabase.Chars(), m_stuServerName.Chars());
		ULONG cbLog;
		m_qstrmLog->Write(staLog.Chars(), staLog.Length(), &cbLog);
	}
	BeginTrans();
	CreateCommand();
	StrUni stuCmd;
	ComBool fMoreRows;
	ComBool fIsNull;
	UINT cbSpaceTaken;
	// Update the encodings for all forms of multilingual text.
	// Note that this command assumes there is only one language project per database:
	// If there are more than one, this command updates the indicated multilingual
	// strings in all of the language projects.
	stuMsg.Load(kstidLngPrjChgEncPhaseOne);
	m_qprog->put_Message(stuMsg.Bstr());

	MergeWsInMultilingualData(wsOld, wsNew);
	SetPercentComplete(5);

	// Do the standard stuff for monolingual and multilingual strings.
	// don't create a new transaction--this method handles it so DoAll doesn't have to.
	DoAll(kstidLngPrjChgEncPhaseOne, kstidLngPrjChgEncPhaseTwo, false);

	// Update the structured text style rules for StPara and StStyle.
	// First StPara.
	stuMsg.Load(kstidLngPrjChgEncPhaseThree);
	m_qprog->put_Message(stuMsg.Bstr());
	int nPercent = 0;
	SetPercentComplete(nPercent);

	int hobj = 0;
	//int flid = 0;
	//int encStr;
	Vector<byte> vbFmt;
	int cbFmt;

	int ceFix;
	int ieFix;
	Vector<int> vflidFix;
	Vector<int> vhobjFix;
	Vector<int> vwsFix;
	Vector<Vector<byte> > vvbFmtFix;

	// Get the count of rows for the sake of the progress indicator dialog.
	// Review (SharonC): Does this slow things down too much?
	int cRows;
	StrUni stuCmdCnt(
		L"SELECT count (*) FROM StPara WHERE StyleRules IS NOT NULL");
	CheckHr(m_qodc->ExecCommand(stuCmdCnt.Bstr(),
				knSqlStmtSelectWithOneRowset));
	CheckHr(m_qodc->GetRowset(0));
	CheckHr(m_qodc->NextRow(&fMoreRows));
	CheckHr(m_qodc->GetColValue(1, reinterpret_cast <BYTE *>(&cRows),
				isizeof(hobj), &cbSpaceTaken, &fIsNull, 0));

	// Get the items.
	stuCmd.Format(L"SELECT [Id], StyleRules FROM StPara "
		L"WHERE StyleRules IS NOT NULL");
	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(),
				knSqlStmtSelectWithOneRowset));
	CheckHr(m_qodc->GetRowset(0));
	CheckHr(m_qodc->NextRow(&fMoreRows));
	vbFmt.Resize(1024);
	int iRow = 0;
	while (fMoreRows)
	{
		CheckHr(m_qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hobj),
					isizeof(hobj), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(m_qodc->GetColValue(2,
					reinterpret_cast<BYTE *>(vbFmt.Begin()), vbFmt.Size(),
					&cbSpaceTaken, &fIsNull, 0));
		cbFmt = cbSpaceTaken;
		if (cbFmt >= vbFmt.Size())
		{
			vbFmt.Resize(cbFmt + 1);
			CheckHr(m_qodc->GetColValue(2,
						reinterpret_cast<BYTE *>(vbFmt.Begin()), vbFmt.Size(),
						&cbSpaceTaken, &fIsNull, 0));
			cbFmt = cbSpaceTaken;
		}
		vbFmt.Resize(cbFmt);
		if (ChangeStyleWs(vbFmt, wsOld, wsNew))
		{
			// Mark the string for updating.
			vhobjFix.Push(hobj);
			vvbFmtFix.Push(vbFmt);
		}
		CheckHr(m_qodc->NextRow(&fMoreRows));
		iRow++;
		SetPercentComplete((iRow * 30) / cRows);
	}
	SetPercentComplete(30);

	ceFix = vhobjFix.Size();
	for (ieFix = 0; ieFix < ceFix; ++ieFix)
	{
		stuCmd.Format(L"UPDATE StPara SET StyleRules=? WHERE [Id] = %d",
			vhobjFix[ieFix]);
		// Set the parameter and execute the command.
		CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT,
					NULL, DBTYPE_BYTES,
					reinterpret_cast<BYTE *>(vvbFmtFix[ieFix].Begin()),
					vvbFmtFix[ieFix].Size()));
		CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		SetPercentComplete(30 + ((ieFix * 20) / ceFix));
	}
	vhobjFix.Clear();
	vvbFmtFix.Clear();
	SetPercentComplete(50);

	// Now do the same for StStyle.

	// Get the count.
	stuCmdCnt.Format(
		L"SELECT count (*) FROM StStyle WHERE Rules IS NOT NULL");
	CheckHr(m_qodc->ExecCommand(stuCmdCnt.Bstr(),
				knSqlStmtSelectWithOneRowset));
	CheckHr(m_qodc->GetRowset(0));
	CheckHr(m_qodc->NextRow(&fMoreRows));
	CheckHr(m_qodc->GetColValue(1, reinterpret_cast <BYTE *>(&cRows),
		isizeof(hobj), &cbSpaceTaken, &fIsNull, 0));

	// Get the items.
	stuCmd.Format(L"SELECT [Id], Rules FROM StStyle "
		L"WHERE Rules IS NOT NULL");
	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(m_qodc->GetRowset(0));
	CheckHr(m_qodc->NextRow(&fMoreRows));
	vbFmt.Resize(1024);
	iRow = 0;
	while (fMoreRows)
	{
		CheckHr(m_qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hobj),
			isizeof(hobj), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(m_qodc->GetColValue(2, reinterpret_cast<BYTE *>(vbFmt.Begin()), vbFmt.Size(),
			&cbSpaceTaken, &fIsNull, 0));
		cbFmt = cbSpaceTaken;
		if (cbFmt >= vbFmt.Size())
		{
			vbFmt.Resize(cbFmt + 1);
			CheckHr(m_qodc->GetColValue(2, reinterpret_cast<BYTE *>(vbFmt.Begin()),
				vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
			cbFmt = cbSpaceTaken;
		}
		vbFmt.Resize(cbFmt);
		if (ChangeStyleWs(vbFmt, wsOld, wsNew))
		{
			// Mark the string for updating.
			vhobjFix.Push(hobj);
			vvbFmtFix.Push(vbFmt);
		}
		CheckHr(m_qodc->NextRow(&fMoreRows));
		iRow++;
		SetPercentComplete(50 + ((iRow * 30) / cRows));
	}

	SetPercentComplete(80);

	ceFix = vhobjFix.Size();
	for (ieFix = 0; ieFix < ceFix; ++ieFix)
	{
		stuCmd.Format(L"UPDATE StStyle SET Rules=? WHERE [Id] = %d", vhobjFix[ieFix]);
		// Set the parameter and execute the command.
		CheckHr(m_qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
			reinterpret_cast<BYTE *>(vvbFmtFix[ieFix].Begin()), vvbFmtFix[ieFix].Size()));
		CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		SetPercentComplete(80 + ((ieFix * 18) / ceFix));
	}
	vhobjFix.Clear();
	vvbFmtFix.Clear();

	// UPDATE SORT SPECS IN THE DATABASE.
	stuCmd.Format(L"UPDATE CmSortSpec SET PrimaryWs=%d WHERE PrimaryWs=%d", m_wsNew, m_wsOld);
	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
	stuCmd.Format(L"UPDATE CmSortSpec SET SecondaryWs=%d WHERE SecondaryWs=%d",
		m_wsNew, m_wsOld);
	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
	stuCmd.Format(L"UPDATE CmSortSpec SET TertiaryWs=%d WHERE TertiaryWs=%d", m_wsNew, m_wsOld);
	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
	SetPercentComplete(99);

	// UPDATE WS LISTS IN THE DATABASE.
	UpdateWsList(L"LangProject_AnalysisWss");
	UpdateWsList(L"LangProject_CurAnalysisWss");
	UpdateWsList(L"LangProject_VernWss");
	UpdateWsList(L"LangProject_CurVernWss");
	UpdateWsList(L"LangProject_CurPronunWss");

	// Our final step: delete the old writing system.
	stuCmd.Format(L"EXEC DeleteObjects '%d'", m_wsOld);
	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

	CommitTrans();
	SetPercentComplete(100);
	Terminate(m_hvoRoot);


	/*
	 * Allow the caller to determine the right time to try to bring the application window back up.

	// IFwTool::NewMainWnd(
	//	[in] BSTR bstrServerName,
	//	[in] BSTR bstrDbName,
	//	[in] int hvoLangProj,	// which language project within the database
	//	[in] int hvoMainObj,	// the top-level object on which to open the window.
	//	[in] int wsUi,			// the user-interface writing system
	//	[in] int nTool,			// tool-dependent identifier of which tool to use
	//	[in] int nParam,		// another tool-dependend parameter
	//	[out] int * ppidNew,			// process id of the new main window's process
	//	[out, retval] long * phtool);	// handle to the newly created main window
	long htool;
	int pidNew;
	CheckHr(m_qfwt->NewMainWnd(m_stuServerName.Bstr(), m_stuDatabase.Bstr(),
		m_hvoProj, m_hvoRoot, m_wsUser, 0, 0, &pidNew, &htool));
	*/

//?	// Notify the database that we've made a change.
//?	SyncInfo sync(ksyncWs, 0, 0);

//?	// This is needed because the old AfDbInfo object is destroyed by the processing above.
//?	AfDbApp * pdapp = dynamic_cast<AfDbApp *>(AfApp::Papp());
//?	Assert(pdapp);
//?	pdbi = pdapp->GetDbInfo(stuDbName.Chars(), stuServerName.Chars());
//?
//?	// We need to store a synch message on startup.
//?	Vector<AfLpInfoPtr> & vlpi = pdbi->GetLpiVec();
//?
//?	// There should only be a single LpInfo at this point.
//?	Assert(vlpi.Size() == 1);
//?	vlpi[0]->StoreSync(sync);

	if (m_qprog)
		m_qprog->DestroyHwnd();

	END_COM_METHOD(g_fact, IID_IFwDbMergeWrtSys);
}


/*----------------------------------------------------------------------------------------------
	Update one writing system list in the database.  This may involve dropping rows from the
	table, or updating rows in a table, or doing nothing.

	@param pszTable Name of a joiner table in the database that specifies a list of writing
					systems.
----------------------------------------------------------------------------------------------*/
void FwDbMergeWrtSys::UpdateWsList(const OLECHAR * pszTable)
{
	StrUni stuCmd;
	ComBool fMoreRows;
	ComBool fIsNull;
	UINT cbSpaceTaken;

	// First, get all the rows that contain either the old (obsolete) or new (but already
	// existing) writing system as the destination of the joiner table.
	stuCmd.Format(L"SELECT Src,Dst FROM %s WHERE Dst in (%d,%d)", pszTable, m_wsNew, m_wsOld);
	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(m_qodc->GetRowset(0));
	CheckHr(m_qodc->NextRow(&fMoreRows));
	Vector<int> vhvoSrcOld;
	Vector<int> vhvoSrcNew;
	while (fMoreRows)
	{
		int hvoSrc = 0;
		int hvoDst = 0;
		CheckHr(m_qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoSrc), isizeof(hvoSrc),
			&cbSpaceTaken, &fIsNull, 0));
		CheckHr(m_qodc->GetColValue(2, reinterpret_cast <BYTE *>(&hvoDst), isizeof(hvoDst),
			&cbSpaceTaken, &fIsNull, 0));
		Assert(hvoSrc != 0);
		Assert(hvoDst == m_wsNew || hvoDst == m_wsOld);
		if (hvoDst == m_wsNew)
			vhvoSrcNew.Push(hvoSrc);
		else
			vhvoSrcOld.Push(hvoSrc);
		CheckHr(m_qodc->NextRow(&fMoreRows));
	}
	if (vhvoSrcOld.Size())
	{
		if (vhvoSrcNew.Size())
		{
			for (int ihvoNew = 0; ihvoNew < vhvoSrcNew.Size(); ++ihvoNew)
			{
				for (int ihvoOld = 0; ihvoOld < vhvoSrcOld.Size(); ++ihvoOld)
				{
					if (vhvoSrcNew[ihvoNew] == vhvoSrcOld[ihvoOld])
					{
						stuCmd.Format(L"DELETE FROM %s WHERE Src=%d AND Dst=%d",
							pszTable, vhvoSrcOld[ihvoOld], m_wsOld);
						vhvoSrcOld.Delete(ihvoOld);
						CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
						break;
					}
				}
			}
			Assert(vhvoSrcOld.Size() == 0);
			Assert(vhvoSrcNew.Size() == 1);
		}
		else
		{
			stuCmd.Format(L"UPDATE %s SET Dst=%d WHERE Dst=%d", pszTable, m_wsNew, m_wsOld);
			CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Read a persistent text property code for either an integer-valued property or a string-
	valued property.

	@param pb Reference to a pointer to the binary text property information.
----------------------------------------------------------------------------------------------*/
static int GetTextPropCode(const byte *& pb)
{
	byte bT = *pb++;
	int cbScp = TextProps::CbScpCode(bT);
	if (cbScp == 1)
		return bT;

	int scp;
	if (cbScp == 2)
	{
		scp = *pb++;
	}
	else if (cbScp == 5)
	{
		CopyBytes(pb, &scp, 4);
		pb += 4;
	}
	else
	{
		Assert(false);		// THIS SHOULD NEVER HAPPEN!
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	scp <<= 6;
	scp |= bT & 0x3F;
	return scp;
}

/*----------------------------------------------------------------------------------------------
	Read the length of the character string stored for a string-valued text property.

	@param pb Reference to a pointer to the binary text property information.
----------------------------------------------------------------------------------------------*/
static int GetStrPropLength(const byte *& pb)
{
	int cch = 0;
	byte bT = *pb++;
	int cbCch = !(bT & 0x80) ? 1 : (bT & 0x40) ? 4 : 2;
	if (cbCch == 1)
	{
		cch = bT;
	}
	else
	{
		if (cbCch == 2)
		{
			cch = *pb++;
		}
		else if (cbCch == 4)
		{
			CopyBytes(pb, &cch, 3);
			pb += 3;
		}
		else
		{
			Assert(false);		// THIS SHOULD NEVER HAPPEN!
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		cch <<= 6;
		cch |= bT & 0x3F;
	}
	return cch;
}

/*----------------------------------------------------------------------------------------------
	Scan the serialized style rule (vbRule) for the writing system wsOld.  If found, change the
	writing system to wsNew instead, and return true.  Otherwise, return false.

	NOTE: This method must be kept in sync with the FwStyledText functions.

	@param vbRule Reference to a byte vector containing the style rule.
	@param wsOld Language writing system code for runs that need to be changed in the style
					rule.
	@param wsNew New writing system value to use.
	@param pstrm IStream pointer for optional logging.

	@return True if the style rule has its writing system changed, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FwDbMergeWrtSys::ChangeStyleWs(Vector<byte> & vbRule, int wsOld, int wsNew)
{
	Assert(sizeof(int) == 4);
	const byte * pb = vbRule.Begin();
	AssertPtr(pb);
	Assert(vbRule.Size() >= 2);

	int ctip = *pb++;
	int ctsp = *pb++;
	Assert((int)(byte)ctip == ctip);
	Assert((int)(byte)ctsp == ctsp);
	bool fEncChanged = false;
	int ws;
	if (ctip)
	{
		int scp;
		for (int itip = 0; itip < ctip; itip++)
		{
			scp = GetTextPropCode(pb);
			switch (TextProps::CbScpData(scp))
			{
			case 1:
				++pb;
				break;
			case 2:
				pb += 2;
				break;
			case 4:
				if (scp == kscpWs)
				{
					CopyBytes(pb, &ws, 4);
					if (ws == wsOld)
					{
						CopyBytes(&wsNew, const_cast<byte *>(pb), 4);
						fEncChanged = true;
					}
				}
				pb += 4;
				break;
			case 8:
				if (scp == kscpWsAndOws)
				{
					int ws;
					CopyBytes(pb, &ws, 4);
					if (ws == wsOld)
					{
						CopyBytes(&wsNew, const_cast<byte *>(pb), 4);
						fEncChanged = true;
					}
				}
				pb += 8;
				break;
			default:
				Assert(false);		// THIS SHOULD NEVER HAPPEN!
				break;
			}
		}
	}

	if (ctsp)
	{
		int tpt;
		int cch;
		for (int itsp = 0; itsp < ctsp; ++itsp)
		{
			tpt = GetTextPropCode(pb);
			cch = GetStrPropLength(pb);
			if (tpt == ktptWsStyle)
			{
				Vector<unsigned int> vws;
				Vector<int> vibOffsets;
				byte * pbMin = const_cast<byte *>(pb);

				const byte * pbLim = pb + cch * isizeof(wchar);
				while (pb < pbLim)
				{
					vibOffsets.Push(pb - pbMin);

					if (pbLim - pb < 12)
					{
						Assert(false);
						ThrowHr(WarnHr(E_UNEXPECTED));
					}
					// The writing system comes first for this type property.
					CopyBytes(pb, &ws, 4);
					if (ws == wsOld)
					{
						CopyBytes(&wsNew, const_cast<byte *>(pb), 4);
						fEncChanged = true;
						vws.Push(wsNew);
					}
					else
						vws.Push(ws);

					// Move past ws.
					pb += 4;
					// Move past the font family.
					int cchFF = 0;
					CopyBytes(pb, &cchFF, 2);
					pb += 2 + cchFF * isizeof(wchar);
					if (pb >= pbLim)
						ThrowHr(WarnHr(E_FAIL));
					int cprop = 0;
					CopyBytes(pb, &cprop, 2);
					pb += 2;
					if (pbLim - pb < 8 * cprop)
					{
						Assert(false);
						ThrowHr(WarnHr(E_FAIL));
					}
					pb += cprop * 8;
				}

				// Now sort the writing system chunks based on the writing system ID.
				vibOffsets.Push(pb - pbMin);
				Vector<byte> vbEnc1;
				Vector<byte> vbEnc2;

				for (int iws1 = 0; iws1 < vws.Size() - 1; iws1++)
				{
					for (int iws2 = iws1 + 1; iws2 < vws.Size(); iws2++)
					{
						if (vws[iws1] > vws[iws2])
						{
							int cbEnc1 = vibOffsets[iws1 + 1] - vibOffsets[iws1];
							vbEnc1.Resize(cbEnc1);
							memcpy(vbEnc1.Begin(), pbMin + vibOffsets[iws1], cbEnc1);

							int cbEnc2 = vibOffsets[iws2 + 1] - vibOffsets[iws2];
							vbEnc2.Resize(cbEnc2);
							memcpy(vbEnc2.Begin(), pbMin + vibOffsets[iws2], cbEnc2);

							// how much to shift the intervening stuff by:
							int dbShift = cbEnc2 - cbEnc1;
							// how much intervening stuff there is:
							int cbShift = vibOffsets[iws2] - vibOffsets[iws1 + 1];
							byte * pbShiftFrom = pbMin + vibOffsets[iws1 + 1];
							byte * pbShiftTo = pbShiftFrom + dbShift;
							// shift intervening stuff
							memmove(pbShiftTo, pbShiftFrom, cbShift);
							// slide the swapped chunks into the their new slots
							memcpy(pbMin + vibOffsets[iws2] + dbShift, vbEnc1.Begin(), cbEnc1);
							memcpy(pbMin + vibOffsets[iws1], vbEnc2.Begin(), cbEnc2);

							unsigned int encSwap = vws[iws1];
							vws[iws1] = vws[iws2];
							vws[iws2] = encSwap;

							for (int iwsTmp = iws1 + 1; iwsTmp <= iws2; iwsTmp++)
								vibOffsets[iwsTmp] += dbShift;
						}
					}
				}
			}
			else
			{
				pb += cch * isizeof(wchar);
			}
		}
	}
	Assert(pb == vbRule.Begin() + vbRule.Size());

	return fEncChanged;
}

/*----------------------------------------------------------------------------------------------
  Scan the multilingual data in the database, merging text data that occurs in both writing
  systems, or just updating the writing system selector when no conflict occurs.

  @param wsOld Old writing system value to replace
  @param wsNew New writing system value to use.
  ----------------------------------------------------------------------------------------------*/
void FwDbMergeWrtSys::MergeWsInMultilingualData(int wsOld, int wsNew)
{
	Vector<StrUni> vstuTable;
	GetMultiTxtTables(vstuTable);
	for (int i = 0; i < vstuTable.Size(); ++i)
		MergeWsDataInTable(wsOld, wsNew, vstuTable[i].Chars(), false);
	MergeWsDataInTable(wsOld, wsNew, L"MultiBigTxt$", false);
	MergeWsDataInTable(wsOld, wsNew, L"MultiStr$", true);
	MergeWsDataInTable(wsOld, wsNew, L"MultiBigStr$", true);

	StrUni stuCmd;
	stuCmd.Format(L"EXEC MergeWritingSystem %d, %d", wsOld, wsNew);
	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
}

/*----------------------------------------------------------------------------------------------
  Get the list of table names in the database for MultiUnicode type data.

  @param vstuTable Reference to a vector of database table names
  ----------------------------------------------------------------------------------------------*/
void FwDbMergeWrtSys::GetMultiTxtTables(Vector<StrUni> & vstuTable)
{
	StrUni stuCmd;
	ComBool fMoreRows;
	ComBool fIsNull;
	UINT cbSpaceTaken;
	OLECHAR rgch[1024];
	stuCmd.Assign(L"SELECT c.Name+'_'+f.Name"
		L" FROM Field$ f JOIN Class$ c ON c.Id=f.Class WHERE f.Type=16 ORDER BY f.Id");
	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(m_qodc->GetRowset(0));
	CheckHr(m_qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		CheckHr(m_qodc->GetColValue(1, reinterpret_cast <BYTE *>(*&rgch),
					isizeof(rgch), &cbSpaceTaken, &fIsNull, 0));
		Assert(!fIsNull);
		int cchw = cbSpaceTaken / isizeof(OLECHAR);
		StrUni stu(rgch, cchw);
		vstuTable.Push(stu);
		CheckHr(m_qodc->NextRow(&fMoreRows));
	}
}

/*----------------------------------------------------------------------------------------------
  Scan the multilingual data in the database, merging text data that occurs in both writing
  systems, or just updating the writing system selector when no conflict occurs.

  @param wsOld Old writing system value to replace
  @param wsNew New writing system value to use.
  @param pszTable Name of the database table to update.
  @param fHasFmt Whether to get Fmt data as well as Txt data.
  ----------------------------------------------------------------------------------------------*/
void FwDbMergeWrtSys::MergeWsDataInTable(int wsOld, int wsNew, const OLECHAR * pszTable,
	bool fHasFmt)
{
	bool fUseFlid = wcschr(pszTable, '_') == NULL;
	StrUni stuCmd;
	stuCmd.Assign(L"SELECT t.Obj");
	if (fUseFlid)
		stuCmd.Append(L",t.Flid");
	stuCmd.Append(L",t.Txt,t2.Txt");
	if (fHasFmt)
		stuCmd.Append(L",t.Fmt,t2.Fmt");
	stuCmd.FormatAppend(L" FROM %<0>s t JOIN %<0>s t2 ON t2.Obj=t.Obj", pszTable);
	if (fUseFlid)
		stuCmd.Append(L" AND t2.Flid=t.Flid");
	stuCmd.FormatAppend(L" AND t2.Ws=%<0>d WHERE t.Ws=%<1>d", wsOld, wsNew);

	// data from all the rows in the output
	Vector<int> vhvo;
	Vector<int> vflid;
	Vector<SmartBstr> vbstrTxt;
	Vector< Vector<byte> > vvbFmt;

	// data from a single row in the output
	int hvo;
	int flid = 0;
	Vector<wchar> vchTxtOld;
	Vector<byte> vbFmtOld;
	Vector<wchar> vchTxtNew;
	Vector<byte> vbFmtNew;
	vchTxtOld.Resize(4000);
	vchTxtNew.Resize(4000);
	if (fHasFmt)
	{
		vbFmtOld.Resize(4000);
		vbFmtNew.Resize(4000);
	}
	int cchNew;
	int cchOld;
	int cbNew = 0;
	int cbOld = 0;

	CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(m_qodc->GetRowset(0));
	ComBool fMoreRows;
	CheckHr(m_qodc->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		int iCol = 1;
		ReadIntValue(iCol++, hvo);
		if (fUseFlid)
			ReadIntValue(iCol++, flid);
		cchNew = ReadTextValue(iCol++, vchTxtNew);
		cchOld = ReadTextValue(iCol++, vchTxtOld);
		SmartBstr sbstrMerged;
		Vector<byte> vbMerged;
		int cbMerged;
		if (fHasFmt)
		{
			cbNew = ReadFmtValue(iCol++, vbFmtNew);
			cbOld = ReadFmtValue(iCol++, vbFmtOld);
			ITsStringPtr qtssNew;
			ITsStringPtr qtssOld;
			CheckHr(m_qtsf->DeserializeStringRgch(vchTxtNew.Begin(), &cchNew, vbFmtNew.Begin(),
				&cbNew, &qtssNew));
			CheckHr(m_qtsf->DeserializeStringRgch(vchTxtOld.Begin(), &cchOld, vbFmtOld.Begin(),
				&cbOld, &qtssOld));
			ITsStrBldrPtr qtsb;
			CheckHr(qtssNew->GetBldr(&qtsb));
			CheckHr(qtsb->ReplaceRgch(cchNew, cchNew, L"; ", 2, NULL));
			CheckHr(qtsb->ReplaceTsString(cchNew + 2, cchNew + 2, qtssOld));
			CheckHr(qtsb->GetString(&qtssNew));
			CheckHr(qtssNew->get_Text(&sbstrMerged));
			vbMerged.Resize(cbNew + cbOld);
			CheckHr(qtssNew->SerializeFmtRgb(vbMerged.Begin(), vbMerged.Size(), &cbMerged));
			if (cbMerged > vbMerged.Size())
			{
				vbMerged.Resize(cbMerged);
				CheckHr(qtssNew->SerializeFmtRgb(vbMerged.Begin(), vbMerged.Size(), &cbMerged));
				Assert(vbMerged.Size() == cbMerged);
			}
			else if (cbMerged < vbMerged.Size())
			{
				vbMerged.Resize(cbMerged);
			}
		}
		else
		{
			StrUni stuNew(vchTxtNew.Begin(), cchNew);
			StrUni stuOld(vchTxtOld.Begin(), cchOld);
			stuNew.Append(L"; ");
			stuNew.Append(stuOld);
			sbstrMerged.Assign(stuNew.Chars(), stuNew.Length());
		}

		vhvo.Push(hvo);
		vflid.Push(flid);
		vbstrTxt.Push(sbstrMerged);
		vvbFmt.Push(vbMerged);

		CheckHr(m_qodc->NextRow(&fMoreRows));
	}

	if (vhvo.Size() > 0)
	{
		for (int i = 0; i < vhvo.Size(); ++i)
		{
			stuCmd.Format(L"UPDATE %<0>s SET Txt=?", pszTable);
			if (fHasFmt)
				stuCmd.Append(L",Fmt=?");
			stuCmd.FormatAppend(L" WHERE Obj=%<0>d AND Ws=%<1>d",
				vhvo[i], wsNew);
			if (fUseFlid)
				stuCmd.FormatAppend(L" AND Flid=%<0>d", vflid[i]);
			stuCmd.FormatAppend(L"; DELETE FROM %<0>s WHERE Obj=%<1>d AND Ws=%<2>d",
				pszTable, vhvo[i], wsOld);
			if (fUseFlid)
				stuCmd.FormatAppend(L" AND Flid=%<0>d", vflid[i]);
			int iCol = 1;
			CheckHr(m_qodc->SetParameter(iCol++, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(BYTE *)vbstrTxt[i].Chars(), vbstrTxt[i].Length() * 2));
			if (fHasFmt)
				CheckHr(m_qodc->SetParameter(iCol++, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
					reinterpret_cast<BYTE *>(vvbFmt[i].Begin()), vvbFmt[i].Size()));
			CheckHr(m_qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		}
		m_qodc.Clear();
		m_qode->CreateCommand(&m_qodc);
	}
}

/*----------------------------------------------------------------------------------------------
	Scan the multilingual data in the database, merging text data that occurs in both writing
	systems, or just updating the writing system selector when no conflict occurs.

	@param wsOld Old writing system value to replace
	@param wsNew New writing system value to use.

	@return True if the style rule has its writing system changed, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FwDbMergeWrtSys::ReadIntValue(int iCol, int & val)
{
	ComBool fIsNull;
	UINT cbSpaceTaken = 0;
	CheckHr(m_qodc->GetColValue(iCol, reinterpret_cast <BYTE *>(&val),
		sizeof(val), &cbSpaceTaken, &fIsNull, 0));
	return !fIsNull;
}

/*----------------------------------------------------------------------------------------------
	Scan the multilingual data in the database, merging text data that occurs in both writing
	systems, or just updating the writing system selector when no conflict occurs.

	@param wsOld Old writing system value to replace
	@param wsNew New writing system value to use.

	@return True if the style rule has its writing system changed, otherwise false.
----------------------------------------------------------------------------------------------*/
int FwDbMergeWrtSys::ReadTextValue(int iCol, Vector<wchar> & vchTxt)
{
	ComBool fIsNull;
	UINT cbSpaceTaken;

	CheckHr(m_qodc->GetColValue(iCol, reinterpret_cast<BYTE *>(vchTxt.Begin()),
		vchTxt.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
	if (!fIsNull)
	{
		long cch = cbSpaceTaken / sizeof(wchar);
		if (cch > vchTxt.Size())
		{
			vchTxt.Resize(cch + 1000);
			CheckHr(m_qodc->GetColValue(iCol, reinterpret_cast<BYTE *>(vchTxt.Begin()),
				vchTxt.Size() * isizeof(wchar), &cbSpaceTaken, &fIsNull, 0));
			Assert(!fIsNull);
			Assert((int)cch == (int)(cbSpaceTaken / isizeof(wchar)));
		}
		return (int)cch;
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Scan the multilingual data in the database, merging text data that occurs in both writing
	systems, or just updating the writing system selector when no conflict occurs.

	@param wsOld Old writing system value to replace
	@param wsNew New writing system value to use.

	@return True if the style rule has its writing system changed, otherwise false.
----------------------------------------------------------------------------------------------*/
int FwDbMergeWrtSys::ReadFmtValue(int iCol, Vector<byte> & vbFmt)
{
	ComBool fIsNull;
	UINT cbSpaceTaken;

	CheckHr(m_qodc->GetColValue(iCol, reinterpret_cast<BYTE *>(vbFmt.Begin()),
		vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
	if (!fIsNull)
	{
		long cbFmt = cbSpaceTaken;
		if (cbFmt > vbFmt.Size())
		{
			vbFmt.Resize(cbFmt + 1000);
			CheckHr(m_qodc->GetColValue(iCol, reinterpret_cast<BYTE *>(vbFmt.Begin()),
				vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
			Assert(!fIsNull);
			Assert((int)cbFmt == (int)cbSpaceTaken);
		}
		return (int)cbFmt;
	}
	return 0;
}

// Handle implicit instantiation.
#include "Vector_i.cpp"

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)
