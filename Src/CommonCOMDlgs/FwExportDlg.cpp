/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwExportDlg.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Implementation of FwExportDlg.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma hdrstop
#include "main.h"
#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	IMPLEMENTATION OF FwExportDlg.
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factFED(
	_T("SIL.FW.FwExportDlg"),
	&CLSID_FwExportDlg,
	_T("SIL FieldWorks Export Dialog"),
	_T("Apartment"),
	&FwExportDlg::CreateCom);


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwExportDlg::FwExportDlg()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();

	if (::InterlockedIncrement(&FwCppStylesDlg::s_cFwStylesDlg) == 1L)
	{
		// We need to register the class in this module, but only once.
		AfWnd::RegisterClass(_T("AfVwWnd"), kgrfwcsDef, NULL, 0, COLOR_WINDOW, 0);
	}
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwExportDlg::~FwExportDlg()
{
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Static method to create an instance of FwExportDlg.
----------------------------------------------------------------------------------------------*/
void FwExportDlg::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<FwExportDlg> qzfwst;
	qzfwst.Attach(NewObj FwExportDlg());	// ref count initially 1
	CheckHr(qzfwst->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	IUnknown methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown and IFwExportDlg are
	supported.

	This is a standard COM IUnknown method.

	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.

	@return SOK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwExportDlg::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IFwExportDlg *>(this));
	else if (iid == IID_IFwExportDlg)
		*ppv = static_cast<IFwExportDlg *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IFwExportDlg);
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
STDMETHODIMP_(ULONG) FwExportDlg::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Standard COM Release method.

	@return The reference count after decrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) FwExportDlg::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}


//:>********************************************************************************************
//:>	IFwExportDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize the FieldWorks File / Export dialog.

	@param hwndParent HWND of the parent window (may be zero)
	@param pvss Pointer to the style sheet (must contain the database connection).
	@param pfcex Pointer to application's export customization interface.
	@param pclsidApp The application CLSID.
	@param bstrRegProgName The application name used in Registry keys.
	@param bstrProgHelpFile The application's help file.
	@param bstrHelpTopic the help topic for this dialog box
	@param hvoLp The project's root database id.
	@param hvoObj The program's root object database id.
	@param flidSubitems The field id of subfields, if any.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwExportDlg::Initialize(DWORD hwndParent, IVwStylesheet * pvss,
	IFwCustomExport * pfcex, GUID * pclsidApp, BSTR bstrRegProgName, BSTR bstrProgHelpFile, BSTR bstrHelpTopic,
	int hvoLp, int hvoObj, int flidSubitems)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvss);
	ChkComArgPtrN(pfcex);		// Allow this to be NULL -- we check it everywhere.
	ChkComArgPtr(pclsidApp);
	ChkComBstrArg(bstrRegProgName);
	ChkComBstrArgN(bstrProgHelpFile);

	m_hwndParent = reinterpret_cast<HWND>(hwndParent);
	m_qvss = pvss;
	m_qfcex = pfcex;
	m_pclsidApp = pclsidApp;
	m_strRegProgName.Assign(bstrRegProgName);
	m_strProgHelpFile.Assign(bstrProgHelpFile);
	if (bstrHelpTopic)
		m_strHelpTopic.Assign(bstrHelpTopic);
	m_hvoLp = hvoLp;
	m_hvoObj = hvoObj;
	m_flidSubitems = flidSubitems;

	END_COM_METHOD(g_factFED, IID_IFwExportDlg);
}


/*----------------------------------------------------------------------------------------------
	Run the FieldWorks File / Export dialog.  The dialog appears, the user does his thing, and
	then the program exports as desired.

	@param vwt The current user view type
	@param crec The number of records to export.
	@param rghvoRec Array of crec record database ids.
	@param rgclidRec Parallel array of crec record class ids.
	@param pnRet Pointer to dialog return value (generally returns kctidOk or kctidCancel, but
				any exporting has already been done)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwExportDlg::DoDialog(int vwt, int crec, int * rghvoRec, int * rgclidRec,
	int * pnRet)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(rghvoRec, crec);
	ChkComArrayArg(rgclidRec, crec);
	ChkComOutPtr(pnRet);
	if (vwt < 0 || vwt >= kvwtLim)
		ThrowHr(WarnHr(E_INVALIDARG));

	if (!crec)
		return S_OK;		// Nothing to export!

	// Extract the database connection from the stylesheet.  This better work!
	IOleDbEncapPtr qode;
	ISilDataAccessPtr qsda;
	CheckHr(m_qvss->get_DataAccess(&qsda));
	ISetupVwOleDbDaPtr qods;
	CheckHr(qsda->QueryInterface(IID_ISetupVwOleDbDa, (void **)&qods));
	IUnknownPtr qunk;
	CheckHr(qods->GetOleDbEncap(&qunk));
	CheckHr(qunk->QueryInterface(IID_IOleDbEncap, (void **)&qode));
	AssertPtr(qode.Ptr());
	SmartBstr sbstrServer;
	SmartBstr sbstrDatabase;
	CheckHr(qode->get_Server(&sbstrServer));
	CheckHr(qode->get_Database(&sbstrDatabase));

	// Create our specialized DbInfo/LpInfo objects.
	ExpDbInfoPtr qxdbi;
	ExpLpInfoPtr qxlpi;
	qxdbi.Create();
	qxdbi->Init(sbstrServer.Chars(), sbstrDatabase.Chars(), NULL);
	qxdbi->SetObjId(m_hvoObj);
	qxdbi->LoadUserViews(m_pclsidApp, vwt);
	qxlpi = dynamic_cast<ExpLpInfo *>(qxdbi->GetLpInfo(m_hvoLp));

	AfExportDlgPtr qexd;
	qexd.Create();
	qexd->Initialize(qxlpi, m_qvss, m_qfcex, m_strRegProgName.Chars(),
		m_strProgHelpFile.Chars(), m_strHelpTopic.Chars(), vwt, m_flidSubitems, crec, rghvoRec, rgclidRec);

	int nRet = qexd->DoModal(m_hwndParent);
	*pnRet = nRet;
	if (nRet == kctidOk)
	{
		// Restore the main window before getting compute bound.
		if (m_hwndParent)
			::UpdateWindow(m_hwndParent);
		qexd->ExportData(m_hwndParent);
	}
	else if (nRet == -1)
	{
		DWORD dwError = ::GetLastError();
		achar rgchMsg[MAX_PATH+1];
		DWORD cch = ::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, NULL, dwError, 0,
			rgchMsg, MAX_PATH, NULL);
		rgchMsg[cch] = 0;
		StrApp strTitle(kstidExportMsgCaption);
		::MessageBox(m_hwndParent, rgchMsg, strTitle.Chars(), MB_OK | MB_ICONWARNING);
	}
	qexd.Clear();

	qxlpi->CleanUp();
	qxdbi->CleanUp();
	qxlpi.Clear();
	qxdbi.Clear();
	qode.Clear();
	qunk.Clear();
	qods.Clear();
	qsda.Clear();

	END_COM_METHOD(g_factFED, IID_IFwExportDlg);
}


//:>********************************************************************************************
//:>	ExpDbInfo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	constructor.
----------------------------------------------------------------------------------------------*/
ExpDbInfo::ExpDbInfo()
{
	m_hvoObj = 0;
}

/*----------------------------------------------------------------------------------------------
	Clear out the pointers to the Language Projects used by this database.  This is like the
	default AfDbInfo method except that it does not shut down the writing system factory.
----------------------------------------------------------------------------------------------*/
void ExpDbInfo::CleanUp()
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
}

/*----------------------------------------------------------------------------------------------
	Retrieve our langauge project info object, creating it if necessary.
----------------------------------------------------------------------------------------------*/
AfLpInfo * ExpDbInfo::GetLpInfo(HVO hvoLp)
{
	int clpi = m_vlpi.Size();
	Assert((unsigned)clpi <= 1);
	for (int ilpi = 0; ilpi < clpi; ilpi++)
	{
		if (hvoLp == m_vlpi[ilpi]->GetLpId())
			return m_vlpi[ilpi];
	}

	// We didn't find it in the cache, so create it now.
	ExpLpInfoPtr qxlpi;
	qxlpi.Create();
	qxlpi->Init(dynamic_cast<AfDbInfo *>(this), hvoLp);
	qxlpi->SetObjId(m_hvoObj);
	qxlpi->OpenProject();
	m_vlpi.Push(dynamic_cast<AfLpInfo *>(qxlpi.Ptr()));
	return qxlpi;
}


//:>********************************************************************************************
//:>	ExpLpInfo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	constructor.
----------------------------------------------------------------------------------------------*/
ExpLpInfo::ExpLpInfo()
{
	m_hvoObj = 0;
}

/*----------------------------------------------------------------------------------------------
	Open a language project.

	@return true if successful
----------------------------------------------------------------------------------------------*/
bool ExpLpInfo::OpenProject()
{
	if (!LoadWritingSystems())
		return false;

	// Load the project ids and names.
	if (!LoadProjBasics())
		return false;

	// Do we need to fake out having an AfDbStylesheet?

	return true;
}

/*----------------------------------------------------------------------------------------------
	Load basic information for the project (ids, names).

	@return true if successful
----------------------------------------------------------------------------------------------*/
bool ExpLpInfo::LoadProjBasics()
{
	// Set up the PrjIds array of ids for this project.
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	StrUni stu;
	ComBool fIsNull;
	ComBool fMoreRows;
	ULONG cbSpaceTaken;
	OLECHAR rgchProjName[MAX_PATH];
	OLECHAR rgchRNName[MAX_PATH];
	m_vhvoPsslIds.Clear(); // Clear any old values.

	// Obtain pointer to IOleDbEncap interface and execute the given SQL select command.
	AssertPtr(m_qdbi);
	m_qdbi->GetDbAccess(&qode);

	try
	{
		stu.Format(L"select cpl.id from CmPossibilityList cpl"
			L" left outer join CmObject cmo on cmo.Id = cpl.Id"
			L" where cmo.Owner$ in (%d, %d)"
			L" order by cmo.Owner$, cmo.OwnFlid$",
			m_hvoLp, m_hvoObj);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		while (fMoreRows)
		{
			int hvo;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo),
				isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
				m_vhvoPsslIds.Push(hvo);
			CheckHr(qodc->NextRow(&fMoreRows));
		}

		// Load any custom lists that there are.
		if (!LoadCustomLists())
			return false;

		stu.Format(L"exec GetOrderedMultiTxt '%d', %d",
			m_hvoLp, kflidCmProject_Name);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		Assert(fMoreRows); // This proc should always return something.
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchProjName),
			MAX_PATH * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));
		m_stuPrjName = rgchProjName;

		stu.Format(L"exec GetOrderedMultiTxt '%d', %d",
			m_hvoObj, kflidCmMajorObject_Name);
		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stu.Bstr(), knSqlStmtStoredProcedure));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		Assert(fMoreRows); // This proc should always return something.
		CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchRNName),
			MAX_PATH * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2));

		m_stuObjName = rgchRNName;
	}
	catch (...)
	{
		return false;
	}
	return true;
}

//:>********************************************************************************************
//:>	FwFldSpec implementation.
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factFFS(
	_T("SIL.FW.FwFldSpec"),
	&CLSID_FwFldSpec,
	_T("SIL FieldWorks Field Spec"),
	_T("Apartment"),
	&FwFldSpec::CreateCom);

/*----------------------------------------------------------------------------------------------
	Static method to create an instance of FwFldSpec.
----------------------------------------------------------------------------------------------*/
void FwFldSpec::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<FwFldSpec> qzfwst;
	qzfwst.Attach(NewObj FwFldSpec());	// ref count initially 1
	CheckHr(qzfwst->QueryInterface(riid, ppv));
}

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwFldSpec::FwFldSpec()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwFldSpec::~FwFldSpec()
{
	ModuleEntry::ModuleRelease();
}


//:>********************************************************************************************
//:>	IUnknown methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown and IFwExportDlg are
	supported.

	This is a standard COM IUnknown method.

	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.

	@return SOK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IFwFldSpec *>(this));
	else if (iid == IID_IFwFldSpec)
		*ppv = static_cast<IFwFldSpec *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IFwExportDlg);
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
STDMETHODIMP_(ULONG) FwFldSpec::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}

/*----------------------------------------------------------------------------------------------
	Standard COM Release method.

	@return The reference count after decrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) FwFldSpec::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}

//:>********************************************************************************************
//:>	IFwFldSpec methods.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Store the visibility flag.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::put_Visibility(int nVis)
{
	BEGIN_COM_METHOD;

	m_nVis = nVis;

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the visibility flag.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::get_Visibility(int * pnVis)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnVis);

	*pnVis = m_nVis;

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

/*----------------------------------------------------------------------------------------------
	Store the "Hide Label" flag.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::put_HideLabel(ComBool fHide)
{
	BEGIN_COM_METHOD;

	m_fHide = fHide;

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the "Hide Label" flag.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::get_HideLabel(ComBool * pfHide)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfHide);

	*pfHide = m_fHide;

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

/*----------------------------------------------------------------------------------------------
	Store the label string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::put_Label(ITsString * ptssLabel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ptssLabel);

	m_qtssLabel = ptssLabel;

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the label string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::get_Label(ITsString ** pptssLabel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptssLabel);

	*pptssLabel = m_qtssLabel;
	AddRefObj(*pptssLabel);

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

/*----------------------------------------------------------------------------------------------
	Store the field id.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::put_FieldId(int flid)
{
	BEGIN_COM_METHOD;

	m_flid = flid;

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the field id.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::get_FieldId(int * pflid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pflid);

	*pflid = m_flid;

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

/*----------------------------------------------------------------------------------------------
	Store the class name.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::put_ClassName(BSTR bstrClsName)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrClsName);

	m_sbstrClsName = bstrClsName;

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the class name.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::get_ClassName(BSTR * pbstrClsName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrClsName);

	m_sbstrClsName.Copy(pbstrClsName);

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

/*----------------------------------------------------------------------------------------------
	Store the field name.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::put_FieldName(BSTR bstrFieldName)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrFieldName);

	m_sbstrFieldName = bstrFieldName;

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the field name.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::get_FieldName(BSTR * pbstrFieldName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrFieldName);

	m_sbstrFieldName.Copy(pbstrFieldName);

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::put_Style(BSTR bstrStyle)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrStyle);

	m_sbstrStyle = bstrStyle;

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwFldSpec::get_Style(BSTR * pbstrStyle)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrStyle);

	m_sbstrStyle.Copy(pbstrStyle);

	END_COM_METHOD(g_factFFS, IID_IFwFldSpec);
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)
