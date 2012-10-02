/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2005 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwDbMergeStyles.cpp
Responsibility: FW Team (Written by TE Team, but probably more understandable by Steve Mc)
Last reviewed: Not yet.

Description:
	Implementation of FwDbMergeStyles.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma hdrstop
#include "main.h"
#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	IMPLEMENTATION OF FwDbMergeStyles.
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.FW.FwDbMergeStyles"),
	&CLSID_FwDbMergeStyles,
	_T("SIL FieldWorks FwDbMergeStyles"),
	_T("Apartment"),
	&FwDbMergeStyles::CreateCom);


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwDbMergeStyles::FwDbMergeStyles()
	: DbStringCrawler(false, true, false, false)
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_fDontCloseMainWnd = true;
	m_pclsidApp = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwDbMergeStyles::~FwDbMergeStyles()
{
	ModuleEntry::ModuleRelease();
}


//:>********************************************************************************************
//:> DbStringCrawler virtual methods.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Change all the occurrences of the old styles names to the new names, and delete any
	obsolete names.
----------------------------------------------------------------------------------------------*/
bool FwDbMergeStyles::ProcessFormatting(ComVector<ITsTextProps> & vqttp)
{
	return ProcessFormatting(vqttp, L"");
}


/*----------------------------------------------------------------------------------------------
	Static method to create an instance of FwDbMergeStyles.
----------------------------------------------------------------------------------------------*/
void FwDbMergeStyles::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<FwDbMergeStyles> qzfwst;
	qzfwst.Attach(NewObj FwDbMergeStyles());	// ref count initially 1
	CheckHr(qzfwst->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	IUnknown methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown and IFwDbMergeStyles are
	supported.

	This is a standard COM IUnknown method.

	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.

	@return SOK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwDbMergeStyles::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IFwDbMergeStyles *>(this));
	else if (iid == IID_IFwDbMergeStyles)
		*ppv = static_cast<IFwDbMergeStyles *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IFwDbMergeStyles);
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
STDMETHODIMP_(ULONG) FwDbMergeStyles::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Standard COM Release method.

	@return The reference count after decrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) FwDbMergeStyles::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}

//:>********************************************************************************************
//:>	IFwDbMergeStyles methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize the string crawler / database fixup process.

	@param bstrServer Name of the database server.
	@param bstrDatabase Name of the database.
	@param pstrmLog Optional output stream for logging (may be NULL).
	@param hvoRootObj Database id of the program's root object.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwDbMergeStyles::Initialize(BSTR bstrServer, BSTR bstrDatabase, IStream * pstrmLog,
	int hvoRootObj, const GUID * pclsidApp)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrServer);
	ChkComBstrArg(bstrDatabase);
	ChkComArgPtrN(pstrmLog);
	ChkComArgPtr(pclsidApp);

	m_hvoRoot = hvoRootObj;
	m_pclsidApp = pclsidApp;
	StrUni stuServer(bstrServer);
	StrUni stuDatabase(bstrDatabase);
	m_qprog.Create();
	IAdvInd3Ptr qadvi3;
	m_qprog->QueryInterface(IID_IAdvInd3, (void **)&qadvi3);
	// Note that the set of styles is different for each program.
	if (!Init(stuServer, stuDatabase, pstrmLog, qadvi3))
	{
		Terminate(m_hvoRoot);
		ThrowHr(WarnHr(E_FAIL));
	}
	END_COM_METHOD(g_fact, IID_IFwDbMergeStyles);
}


STDMETHODIMP FwDbMergeStyles::InitializeEx(IOleDbEncap * pode, IStream * pstrmLog,
	int hvoRootObj, const GUID * pclsidApp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pode);
	ChkComArgPtrN(pstrmLog);
	ChkComArgPtr(pclsidApp);

	m_hvoRoot = hvoRootObj;
	m_pclsidApp = pclsidApp;
	m_qprog.Create();
	IAdvInd3Ptr qadvi3;
	m_qprog->QueryInterface(IID_IAdvInd3, (void **)&qadvi3);
	// Note that the set of styles is different for each program.
	if (!Init(pode, pstrmLog, qadvi3))
	{
		Terminate(m_hvoRoot);
		ThrowHr(WarnHr(E_FAIL));
	}
	END_COM_METHOD(g_fact, IID_IFwDbMergeStyles);
}


STDMETHODIMP FwDbMergeStyles::AddStyleReplacement(BSTR bstrOldStyleName, BSTR bstrNewStyleName)
{
	BEGIN_COM_METHOD;
	m_vstuOldNames.Insert(0, 1, bstrOldStyleName);
	m_vstuNewNames.Insert(0, 1, bstrNewStyleName);
	END_COM_METHOD(g_fact, IID_IFwDbMergeStyles);
}

STDMETHODIMP FwDbMergeStyles::AddStyleDeletion(BSTR bstrDeleteStyleName)
{
	BEGIN_COM_METHOD;
	m_vstuDelNames.Insert(0, 1, bstrDeleteStyleName);
	END_COM_METHOD(g_fact, IID_IFwDbMergeStyles);
}

/*----------------------------------------------------------------------------------------------
	Crawl through the database and rename/delete the given styles.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwDbMergeStyles::Process(DWORD hWnd)
{
	BEGIN_COM_METHOD;
	Assert(m_vstuOldNames.Size() == m_vstuNewNames.Size());

	if (!m_hvoRoot)
		return E_UNEXPECTED;
	if (!m_pclsidApp)
		return E_UNEXPECTED;

	if (LogFile())
	{
		ULONG cbLog;
		StrAnsi staLog;
		staLog.Format("Changing style names in %S (%S)%n",
			DbName().Chars(), ServerName().Chars());
		LogFile()->Write(staLog.Chars(), staLog.Length(), &cbLog);
	}

	m_qprog->DoModeless((HWND)hWnd);
	StrUni stuMsg(kstidChgStyleLabel);
	m_qprog->put_Title(stuMsg.Bstr());

	ResetConnection();
	BeginTrans();
	CreateCommand();

	SetPercentComplete(0);

	// We want to affect only a subset of the formatting and style information in the database,
	// based on which program is currently running, since different programs may be using
	// different sets of styles.  This information is derived from m_hvoRoot and m_pclsidApp.
	// (Unfortunately, there's no way to derive one of these values from the other one.)  The
	// current scheme works for Data Notebook, List Editor, TE, and LexText.  Additional
	// programs may introduce even more complexities.
	// 1. Find all the owners of StStyle objects.  This should match up to LangProject (Data
	//    Notebook and List Editor), Scripture (TE), or LexDb (LexText).
	// 2. If an owner equals m_hvoRoot, then only those objects owned by m_hvoRoot have the
	//    string and paragraph formatting fixed.  Except that LexText also wants to fix all the
	//    Text objects owned by the LangProject in addition to all the objects owned by the
	//    LexDb, so those have to be added to the collection of objects.
	// 3. If none of the owners equals m_hvoRoot, we must be dealing with Data Notebook or List
	//    Editor, which share a common set of styles owned by the LangProject itself.  In
	//    this case, we want all the objects owned by the LangProject except those owned by
	//    another owner of styles (or the LangProject Text objects, since those use the
	//    LexText styles even though they're not owned by the root object).  (This isn't quite
	//    right if either TE or LexText does not actually own any styles yet, but we won't worry
	//    about this nicety.)
	// 4. After creating a temp table containing just the ids of those objects of interest, we
	//    can then process all the relevant string formatting (via a string crawler) and
	//    paragraph StyleRules (later in this method)
	// 5. Finally, we may need to fix the Style fields of the relevant UserViewField objects.
	//    These are limited by the psclsidApp argument, since the UserView objects are specific
	//    to specific applications.  If pssclidApp indicates either Data Notebook or List
	//    Editor, then UserViewField objects belonging to both of those programs are fixed.
	//    Otherwise, only those UserViewField objects belonging to the specific program
	//    identified by psclsidApp are fixed.

	ComBool fMoreRows;
	unsigned long cbSpaceTaken;
	ComBool fIsNull;
	int hobj = 0;
	int clid = 0;
	bool fFixOwned = false;
	int clidOwned = 0;
	int hvoLangProj = 0;
	Vector<int> vhvoOwners;
	Vector<int> vclidOwners;
	Vector<int> vhvoTexts;
	StrUni stuCmd;
	stuCmd.Format(L"SELECT DISTINCT c.Owner$, co.Class$ "
		L"FROM CmObject c "
		L"JOIN CmObject co on co.Id = c.Owner$ "
		L"WHERE c.Class$ = %d",
		kclidStStyle);
	CheckHr(GetOleDbCommand()->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(GetOleDbCommand()->GetRowset(0));
	CheckHr(GetOleDbCommand()->NextRow(&fMoreRows));
	while (fMoreRows)
	{
		CheckHr(GetOleDbCommand()->GetColValue(1, reinterpret_cast <BYTE *>(&hobj),
			isizeof(hobj), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(GetOleDbCommand()->GetColValue(2, reinterpret_cast <BYTE *>(&clid),
			isizeof(clid), &cbSpaceTaken, &fIsNull, 0));
		if (hobj == m_hvoRoot)
		{
			fFixOwned = true;
			clidOwned = clid;
		}
		else if (clid == kclidLangProject)
		{
			hvoLangProj = hobj;
		}
		else
		{
			vhvoOwners.Push(hobj);
			vclidOwners.Push(clid);
		}
		CheckHr(GetOleDbCommand()->NextRow(&fMoreRows));
	}
	Assert(hvoLangProj != 0);
// This may not be defined in any of our header files.
#ifndef kclidLexDb
#define kclidLexDb 5005
#endif
	if (!fFixOwned || clidOwned == kclidLexDb)
	{
		// We need the set of LangProject_Texts objects to include or exclude.
		stuCmd.Format(L"SELECT Dst FROM LangProject_Texts WHERE Src = %d", hvoLangProj);
		CheckHr(GetOleDbCommand()->ExecCommand(stuCmd.Bstr(),
			knSqlStmtSelectWithOneRowset));
		CheckHr(GetOleDbCommand()->GetRowset(0));
		CheckHr(GetOleDbCommand()->NextRow(&fMoreRows));
		while (fMoreRows)
		{
			CheckHr(GetOleDbCommand()->GetColValue(1, reinterpret_cast <BYTE *>(&hobj),
				isizeof(hobj), &cbSpaceTaken, &fIsNull, 0));
			vhvoTexts.Push(hobj);
			CheckHr(GetOleDbCommand()->NextRow(&fMoreRows));
		}
	}
	// Note that dbo.fnGetOwnedObjects$() returns the root object as the first row.
	stuCmd.Format(L"CREATE TABLE #OwnedObjIdsTbl%n"
		L"(%n"
		L"    ObjId int not null%n"
		L")%n"
		L"CREATE CLUSTERED INDEX #OwnedObjIdsTblObjId ON #OwnedObjIdsTbl ([ObjId])%n");
	CheckHr(GetOleDbCommand()->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

	const OLECHAR * kpszDefTableFmt =
		L"INSERT INTO #OwnedObjIdsTbl%n"
		L"SELECT ObjId%n"
		L"FROM dbo.fnGetOwnedObjects$('%d', null, 0, 0, 1, null, 0)";

	if (fFixOwned)
	{
		stuCmd.Format(kpszDefTableFmt, m_hvoRoot);
		stuCmd.FormatAppend(L";%n");
		if (vhvoTexts.Size())
		{
			stuCmd.FormatAppend(
				L"INSERT INTO #OwnedObjIdsTbl%n"
				L"SELECT ObjId%n"
				L"FROM dbo.fnGetOwnedObjects$('");
			for (int ihvo = 0; ihvo < vhvoTexts.Size(); ++ihvo)
				if (ihvo == 0)
					stuCmd.FormatAppend(L"%d", vhvoTexts[ihvo]);
				else
					stuCmd.FormatAppend(L",%d", vhvoTexts[ihvo]);
			stuCmd.FormatAppend(L"', null, 0, 0, 1, null, 0);");
		}
	}
	else
	{
	/*
	POSSIBLE SPEED ENHANCEMENT
	--------------------------
	SELECT co.Id
	FROM CmObject co
	WHERE co.Owner$=1 AND co.OwnFlid$ NOT IN (6001006, 6001014, 6001040)

	gives a list of ids which own what we want to look through.  This could then be
	handled by the following query, which runs in one-half to one-third of the time
	required by the current code.

	SELECT 1 UNION SELECT ObjId FROM dbo.fnGetOwnedObjects$('2,54728', null, 0, 0, 1, null, 0)

	whether the C++ code or the SQL code should put together the XML string is a good
	question.
	*/
		// REVIEW (SteveMiller): The temp tables below just slow things down.

		stuCmd.Clear();
		if (vhvoOwners.Size() || vhvoTexts.Size())
		{
			stuCmd.FormatAppend(L"CREATE TABLE #tblUnwanted ( ObjId int not null );%n");
			stuCmd.FormatAppend(L"INSERT INTO #tblUnwanted%n");
			stuCmd.FormatAppend(L"SELECT ObjId%n");
			stuCmd.FormatAppend(L"FROM dbo.fnGetOwnedObjects$('");
			for (int ihvo = 0; ihvo < vhvoOwners.Size(); ++ihvo)
				if (ihvo == 0)
					stuCmd.FormatAppend(L"%d", vhvoOwners[ihvo]);
				else
					stuCmd.FormatAppend(L",%d", vhvoOwners[ihvo]);
			for (int ihvo = 0; ihvo < vhvoTexts.Size(); ++ihvo)
				if (vhvoOwners.Size() < 0)
					// I don't think we can have an ownerless text, but hey...
					stuCmd.FormatAppend(L"%d", vhvoTexts[ihvo]);
				else
					stuCmd.FormatAppend(L",%d", vhvoTexts[ihvo]);
			stuCmd.FormatAppend(L"',null,0,0,1,null,0);%n");
		}
		stuCmd.FormatAppend(kpszDefTableFmt, hvoLangProj);
		if (vhvoOwners.Size() || vhvoTexts.Size())
		{
			stuCmd.FormatAppend(L"WHERE ObjId NOT IN (SELECT ObjId FROM #tblUnwanted);%n");
			stuCmd.FormatAppend(L"DROP TABLE #tblUnwanted");
		}
		stuCmd.FormatAppend(L";%n");	// terminate for Firebird
	}
	CheckHr(GetOleDbCommand()->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

	// Do the standard stuff for monolingual and multilingual strings.
	DoAll(kstidChgStylePhaseOne, kstidChgStylePhaseTwo,
		false,	// don't create a new transaction--this method handles it
		L"#OwnedObjIdsTbl");

	// Fix all the StyleRules of instances of StPara.
	stuMsg.Load(kstidChgStylePhaseThree);
	m_qprog->put_Message(stuMsg.Bstr());

	int nPercent = 0;
	SetPercentComplete(nPercent);

	Vector<int> vhobjFix;
	Vector<Vector<byte> > vvbFmtFix;
	vhobjFix.Clear();
	vvbFmtFix.Clear();

	Vector<byte> vbFmt;
	int cbFmt;

	ITsPropsFactoryPtr qtpf;
	qtpf.CreateInstance(CLSID_TsPropsFactory);

	int cRows;
	StrUni stuCmdCnt;
	stuCmdCnt.Format(L"SELECT COUNT(*) FROM StPara a%n"
		L"JOIN #OwnedObjIdsTbl b on b.ObjId = a.Id%n"
		L"WHERE a.StyleRules IS NOT NULL",
		m_hvoRoot);
	CheckHr(GetOleDbCommand()->ExecCommand(stuCmdCnt.Bstr(),
		knSqlStmtSelectWithOneRowset));
	CheckHr(GetOleDbCommand()->GetRowset(0));
	CheckHr(GetOleDbCommand()->NextRow(&fMoreRows));
	CheckHr(GetOleDbCommand()->GetColValue(1, reinterpret_cast <BYTE *>(&cRows),
		isizeof(hobj), &cbSpaceTaken, &fIsNull, 0));

	stuCmd.Format(L"SELECT a.Id, a.StyleRules FROM StPara a%n"
		L"JOIN #OwnedObjIdsTbl b on b.ObjId = a.Id%n"
		L"WHERE a.StyleRules IS NOT NULL",
		m_hvoRoot);
	CheckHr(GetOleDbCommand()->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(GetOleDbCommand()->GetRowset(0));
	CheckHr(GetOleDbCommand()->NextRow(&fMoreRows));
	int iRow = 0;
	ComVector<ITsTextProps> vqttp;
	vqttp.Resize(1);
	while (fMoreRows)
	{
		CheckHr(GetOleDbCommand()->GetColValue(1, reinterpret_cast <BYTE *>(&hobj),
			isizeof(hobj), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(GetOleDbCommand()->GetColValue(2,
			reinterpret_cast <BYTE *>(vbFmt.Begin()), vbFmt.Size(), &cbSpaceTaken, &fIsNull,
			0));
		cbFmt = cbSpaceTaken;
		if (cbFmt >= vbFmt.Size())
		{
			vbFmt.Resize(cbFmt + 1);
			CheckHr(GetOleDbCommand()->GetColValue(2,
				reinterpret_cast <BYTE *>(vbFmt.Begin()),
				vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0));
			cbFmt = cbSpaceTaken;
		}
		vbFmt.Resize(cbFmt);

		ITsTextPropsPtr qttp;
		bool fModify = false;
		int cb = vbFmt.Size();
		CheckHr(qtpf->DeserializePropsRgb(vbFmt.Begin(), &cb, (ITsTextProps **)&qttp));

		// use a vector with exactly 1 item
		vqttp[0] = qttp;
		// Note: using "Normal" doesn't work all the time. For more complex scenarios where the
		// default style depends on the context of the deleted style, a replace should be done
		// instead of a delete. See FwStylesDlg.DeleteAndRenameStylesInDB() in FwStylesDlg.cs.
		fModify = ProcessFormatting(vqttp, g_pszwStyleNormal);
		if (fModify)
		{
			vhobjFix.Push(hobj);
			int cbNeeded;
			HRESULT hr;
			CheckHr(hr = vqttp[0]->SerializeRgb(vbFmt.Begin(), vbFmt.Size(), &cbNeeded));
			if (hr == S_FALSE)
			{
				vbFmt.Resize(cbNeeded);
				hr = vqttp[0]->SerializeRgb(vbFmt.Begin(), vbFmt.Size(), &cbNeeded);
			}
			vbFmt.Resize(cbNeeded);
			vvbFmtFix.Push(vbFmt);
		}
		CheckHr(GetOleDbCommand()->NextRow(&fMoreRows));

		iRow++;
		SetPercentComplete((iRow * 50) / cRows);
	}
	int ceFix = vhobjFix.Size();
	SetPercentComplete(50);
	for (int ieFix = 0; ieFix < ceFix; ++ieFix)
	{
		stuCmd.Format(L"UPDATE StPara SET StyleRules=? WHERE [Id] = %d", vhobjFix[ieFix]);
		// Set the parameter and execute the command.
		CheckHr(GetOleDbCommand()->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL,
			DBTYPE_BYTES, reinterpret_cast<ULONG *>(vvbFmtFix[ieFix].Begin()),
			vvbFmtFix[ieFix].Size()));
		CheckHr(GetOleDbCommand()->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

		SetPercentComplete(50 + ((ieFix * 50) / ceFix));
	}
	SetPercentComplete(100);

	stuCmd.Assign(L"DROP TABLE #OwnedObjIdsTbl");
	CheckHr(GetOleDbCommand()->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

	// Fix style names in the view specs.
	// SQL gives us the power to just rename all items in one query. But that
	// won't correctly handle the situation of, for instance, renaming A to B, B to C, and
	// C to A. So we fix one item at a time and then update the database.

	static const GUID clsidNotebook =	// {39886581-4DD5-11D4-8078-0000C0FB81B5}
		{ 0x39886581, 0x4DD5, 0x11D4, { 0x80, 0x78, 0x00, 0x00, 0xC0, 0xFB, 0x81, 0xB5 } };
	static GUID clsidListEditor =	// {5EA62D01-7A78-11D4-8078-0000C0FB81B5}
		{ 0x5EA62D01, 0x7A78, 0x11D4, { 0x80, 0x78, 0x00, 0x00, 0xC0, 0xFB, 0x81, 0xB5 } };

	StrUni stuBaseCmd;
	stuBaseCmd.Format(L"%nFROM UserViewField a%n"
		L"JOIN UserViewRec_Fields uf on uf.Dst = a.Id%n"
		L"JOIN UserView_Records ur on ur.Dst = uf.Src%n"
		L"JOIN UserView uv on uv.Id = ur.Src%n"
		L"WHERE a.Style IS NOT NULL AND");
	if (*m_pclsidApp == clsidNotebook || *m_pclsidApp == clsidListEditor)
	{
		Assert(!fFixOwned);
		stuBaseCmd.FormatAppend(L"%n\tuv.App in ('%g','%g')", &clsidNotebook, &clsidListEditor);
	}
	else
	{
		Assert(fFixOwned);
		stuBaseCmd.FormatAppend(L" uv.App = '%g'", m_pclsidApp);
	}
	stuMsg.Load(kstidChgStylePhaseFour);
	m_qprog->put_Message(stuMsg.Bstr());
	SetPercentComplete(0);

	OLECHAR rgch[1024];
	vhobjFix.Clear();
	Vector<StrUni> vstuFix;

	stuCmdCnt.Format(L"SELECT COUNT(*)%s", stuBaseCmd.Chars());
	CheckHr(GetOleDbCommand()->ExecCommand(stuCmdCnt.Bstr(),
		knSqlStmtSelectWithOneRowset));
	CheckHr(GetOleDbCommand()->GetRowset(0));
	CheckHr(GetOleDbCommand()->NextRow(&fMoreRows));
	CheckHr(GetOleDbCommand()->GetColValue(1, reinterpret_cast <BYTE *>(&cRows),
		isizeof(hobj), &cbSpaceTaken, &fIsNull, 0));

	stuCmd.Format(L"SELECT a.Id, a.Style%s", stuBaseCmd.Chars());

	CheckHr(GetOleDbCommand()->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
	CheckHr(GetOleDbCommand()->GetRowset(0));
	CheckHr(GetOleDbCommand()->NextRow(&fMoreRows));
	iRow = 0;
	while (fMoreRows)
	{
		CheckHr(GetOleDbCommand()->GetColValue(1, reinterpret_cast <BYTE *>(&hobj),
			isizeof(hobj), &cbSpaceTaken, &fIsNull, 0));
		CheckHr(GetOleDbCommand()->GetColValue(2, reinterpret_cast <BYTE *>(*&rgch),
			isizeof(rgch), &cbSpaceTaken, &fIsNull, 0));
		int cchw = cbSpaceTaken / isizeof(OLECHAR);

		StrUni stuOld(rgch, cchw);
		StrUni stuNew;
		if (Delete(stuOld))
		{
			vstuFix.Push(L"");
			vhobjFix.Push(hobj);
		}
		else if (Rename(stuOld, stuNew))
		{
			vstuFix.Push(stuNew);
			vhobjFix.Push(hobj);
		}

		CheckHr(GetOleDbCommand()->NextRow(&fMoreRows));

		iRow++;
		SetPercentComplete((iRow * 50) / cRows);
	}
	SetPercentComplete(50);
	Assert(vhobjFix.Size() == vstuFix.Size());

	ceFix = vstuFix.Size();
	for (int ieFix = 0; ieFix < ceFix; ieFix++)
	{
		if (vstuFix[ieFix] == L"")
		{
			stuCmd.Format(L"UPDATE UserViewField SET Style = NULL where [Id] = '%d'",
				vhobjFix[ieFix]);
		}
		else
		{
			StrUtil::NormalizeStrUni(vstuFix[ieFix], UNORM_NFD);
			stuCmd.Format(L"UPDATE UserViewField SET Style = '%s' where [Id] = '%d'",
				vstuFix[ieFix].Chars(), vhobjFix[ieFix]);
		}
		CheckHr(GetOleDbCommand()->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

		SetPercentComplete(50 + ((ieFix * 50) / ceFix));
	}

	SetPercentComplete(100);
	CommitTrans();
	Terminate(m_hvoRoot);

	if (m_qprog)
		m_qprog->DestroyHwnd();
	END_COM_METHOD(g_fact, IID_IFwDbMergeStyles);
}

//:>********************************************************************************************
//:>	FwDbMergeStyles protected methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	@return true if the style name is on the list to delete.
----------------------------------------------------------------------------------------------*/
bool FwDbMergeStyles::Delete(StrUni & stu)
{
	for (int istu = 0; istu < m_vstuDelNames.Size(); istu++)
	{
		if (stu == (m_vstuDelNames)[istu])
			return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	@return true if the style name is on the list to rename.
----------------------------------------------------------------------------------------------*/
bool FwDbMergeStyles::Rename(StrUni & stuOld, StrUni & stuNew)
{
	Assert(m_vstuOldNames.Size() == m_vstuNewNames.Size());
	for (int istu = 0; istu < m_vstuOldNames.Size(); istu++)
	{
		if (stuOld == m_vstuOldNames[istu])
		{
			stuNew = m_vstuNewNames[istu];
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Change all the occurrences of the old styles names to the new names, and delete any
	obsolete names.
----------------------------------------------------------------------------------------------*/
bool FwDbMergeStyles::ProcessFormatting(ComVector<ITsTextProps> & vqttp,
	StrUni stuDelete)
{
	bool fAnyChanged = false;
	for (int ittp = 0; ittp < vqttp.Size(); ittp++)
	{
		SmartBstr sbstr;
		HRESULT hr;
		CheckHr(hr = vqttp[ittp]->GetStrPropValue(ktptNamedStyle, &sbstr));
		if (hr == S_OK && sbstr.Length() > 0)
		{
			ITsPropsBldrPtr qtpb = NULL;
			StrUni stuOld(sbstr.Chars());
			StrUni stuNew;
			if (Delete(stuOld))
			{
				CheckHr(vqttp[ittp]->GetBldr(&qtpb));
				if (stuDelete.Length() == 0)
				{
					// If the style name to delete is empty, we want to pass null
					// so that the named style string property is removed.
					CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, NULL));
				}
				else
					CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, stuDelete.Bstr()));

			}
			else if (Rename(stuOld, stuNew))
			{
				CheckHr(vqttp[ittp]->GetBldr(&qtpb));
				CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, stuNew.Bstr()));
			}

			if (qtpb)
			{
				ITsTextPropsPtr qttpNew;
				CheckHr(qtpb->GetTextProps(&qttpNew));
				vqttp[ittp] = qttpNew;
				fAnyChanged = true;
			}
		}
	}

	return fAnyChanged;
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)
