/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwStylesDlg.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Implementation of FwCppStylesDlg.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma hdrstop
#include "main.h"
#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	IMPLEMENTATION OF FwCppStylesDlg.
//:>********************************************************************************************


//:>********************************************************************************************
//:>	FwCppStylesDlg - Generic factory stuff to allow creating an instance
//:>		with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factOPD(
	_T("SIL.FW.FwStylesDlg"),
	&CLSID_FwCppStylesDlg,
	_T("SIL FieldWorks Styles Dialog"),
	_T("Apartment"),
	&FwCppStylesDlg::CreateCom);


long FwCppStylesDlg::s_cFwStylesDlg = 0;

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwCppStylesDlg::FwCppStylesDlg()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();

	if (::InterlockedIncrement(&s_cFwStylesDlg) == 1)
	{
		// We need to register the class in this module, but only once.
		AfWnd::RegisterClass(_T("AfVwWnd"), kgrfwcsDef, NULL, 0, COLOR_WINDOW, 0);
	}

	m_sdt = ksdtStandard;
	m_fShowAll = false;

	m_hwndParent = 0;
	m_fCanDoRtl = false;
	m_fOuterRtl = false;
	m_fFontFeatures = false;
	m_f1DefaultFont = false;
	m_fCanFormatChar = false;
	m_fStylesChanged = false;
	m_fApply = false;
	m_fReloadDb = false;
	m_nCustomStyleLevel = 9999;
	m_hvoRootObj = 0;
	m_clsidApp = GUID();

	m_fResult = false;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwCppStylesDlg::~FwCppStylesDlg()
{
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Static method to create an instance of FwCppStylesDlg.
----------------------------------------------------------------------------------------------*/
void FwCppStylesDlg::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<FwCppStylesDlg> qzfwst;
	qzfwst.Attach(NewObj FwCppStylesDlg());	// ref count initially 1
	CheckHr(qzfwst->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	IUnknown methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown and IFwCppStylesDlg are
	supported.

	This is a standard COM IUnknown method.

	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.

	@return SOK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IFwCppStylesDlg *>(this));
	else if (iid == IID_IFwCppStylesDlg)
		*ppv = static_cast<IFwCppStylesDlg *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IFwCppStylesDlg);
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
STDMETHODIMP_(ULONG) FwCppStylesDlg::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Standard COM Release method.

	@return The reference count after decrementing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) FwCppStylesDlg::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}


//:>********************************************************************************************
//:>	IFwCppStylesDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Select the style of the dialog.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_DlgType(StylesDlgType sdt)
{
	BEGIN_COM_METHOD;

	m_sdt = sdt;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the ShowAll value for the TE version of the dialog.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_ShowAll(ComBool fShowAll)
{
	BEGIN_COM_METHOD;

	m_fShowAll = bool(fShowAll);

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the measurement unit.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_SysMsrUnit(int nMsrSys)
{
	BEGIN_COM_METHOD;

	m_nMsrSys = (MsrSysType)nMsrSys;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the user interface writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_UserWs(int wsUser)
{
	BEGIN_COM_METHOD;

	m_wsUser = wsUser;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the help file name.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_HelpFile(BSTR bstrHelpFile)
{
	BEGIN_COM_METHOD;

	m_strHelpFile = bstrHelpFile;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}

/*----------------------------------------------------------------------------------------------
	Set the help file url for the given tab.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_TabHelpFileUrl(int tabNum, BSTR bstrHelpFile)
{
	BEGIN_COM_METHOD;

	if (tabNum < 0 || tabNum >= AfStylesDlg::kcdlgv)
		return E_INVALIDARG;
	m_strTabHelpFileUrl[tabNum] = bstrHelpFile;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}

/*----------------------------------------------------------------------------------------------
	Set the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pwsf);

	m_qwsf = pwsf;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the HWND of the parent window.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_ParentHwnd(DWORD hwndParent)
{
	BEGIN_COM_METHOD;

	m_hwndParent = reinterpret_cast<HWND>(hwndParent);

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the flag for being able to handle RTL scripts.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_CanDoRtl(ComBool fCanDoRtl)
{
	BEGIN_COM_METHOD;

	m_fCanDoRtl = bool(fCanDoRtl);

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the flag for the outer script directionality being RTL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_OuterRtl(ComBool fOuterRtl)
{
	BEGIN_COM_METHOD;

	m_fOuterRtl = bool(fOuterRtl);

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the flag for displaying the font features button on the Font tab.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_FontFeatures(ComBool fFontFeatures)
{
	BEGIN_COM_METHOD;

	m_fFontFeatures = bool(fFontFeatures);

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the style sheet used to initialize the dialog.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::putref_Stylesheet(IVwStylesheet * pvss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvss);

	m_qvss = pvss;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}

/*----------------------------------------------------------------------------------------------
	Specifies a set of style contexts that should be used to determine which styles can be
	applied. Selecting any style having a context not in this array will cause the Apply
	button to be grayed out.

	@param rgnRoles Array of integers that represent style contexts.
	@param cpnRoles Number of contexts in rgnContexts.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::SetApplicableStyleContexts(int * rgnContexts, int cpnContexts)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(rgnContexts, cpnContexts);

	m_vApplicableContexts.Clear();
	m_vApplicableContexts.InsertMulti(0, cpnContexts, rgnContexts);

	// In the test code, it is possible (and VERY likely) that this method could be called
	// after we simulate the showing of the modal dialog. If so, we need to pass this vector
	// along immediately.
	if (m_qafsd)
		m_qafsd->SetApplicableStyleContexts(m_vApplicableContexts);

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}

/*----------------------------------------------------------------------------------------------
	Set the flag for allowing character styles in the absence of any paragraph styles.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_CanFormatChar(ComBool fCanFormatChar)
{
	BEGIN_COM_METHOD;

	m_fCanFormatChar = bool(fCanFormatChar);

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the flag whether paragrah styles are to used in the dialog, or only character styles
	are used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_OnlyCharStyles(ComBool fOnlyCharStyles)
{
	BEGIN_COM_METHOD;

	m_fOnlyCharStyles = bool(fOnlyCharStyles);

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the name of the selected style on entry (possibly ignored).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_StyleName(BSTR bstrStyleName)
{
	BEGIN_COM_METHOD;

	m_stuStyleName = bstrStyleName;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the custom style level
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_CustomStyleLevel(int level)
{
	BEGIN_COM_METHOD;

	m_nCustomStyleLevel = level;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the text properties for paragraph and character styles.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::SetTextProps(ITsTextProps ** rgpttpPara, int cttpPara,
	ITsTextProps ** rgpttpChar, int cttpChar)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(rgpttpPara, cttpPara);
	ChkComArrayArg(rgpttpChar, cttpChar);

	// Note: need to increment reference counts on these COM objects.
	int ittp;
	m_vqttpPara.Resize(cttpPara);
	for (ittp = 0; ittp < cttpPara; ++ittp)
		m_vqttpPara[ittp] = rgpttpPara[ittp];

	m_vqttpChar.Resize(cttpChar);
	for (ittp = 0; ittp < cttpChar; ++ittp)
		m_vqttpChar[ittp] = rgpttpChar[ittp];

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Set the database root object id for the current program.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_RootObjectId(int hvoRootObj)
{
	BEGIN_COM_METHOD;
	if (hvoRootObj == 0)
		ThrowInternalError(E_INVALIDARG);

	m_hvoRootObj = hvoRootObj;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Store the writing system codes for the available writing systems.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::SetWritingSystemsOfInterest(int * rgws, int cws)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(rgws, cws);

	m_vwsAvailable.InsertMulti(0, cws, rgws);

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Store the stream pointer to the log file, if any.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::putref_LogFile(IStream * pstrmLog)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pstrmLog);

	m_qstrmLog = pstrmLog;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Store the pointer to the help topic provider, if any.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::putref_HelpTopicProvider(IHelpTopicProvider * phtprov)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(phtprov);

	m_qhtprov = phtprov;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Store the application's clsid GUID.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::put_AppClsid(GUID clsidApp)
{
	BEGIN_COM_METHOD;

	m_clsidApp = clsidApp;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	First step of displaying the modal dialog: create the dialog object and initialize it.
----------------------------------------------------------------------------------------------*/
void FwCppStylesDlg::SetupForDoModal()
{
	if (m_sdt == ksdtStandard)
		m_qafsd.Attach(NewObj AfStylesDlg);
	else if (m_sdt == ksdtTransEditor)
		m_qafsd.Attach(NewObj TeStylesDlg(m_fShowAll));
	else
		ThrowInternalError(E_UNEXPECTED);

	m_qafsd->SetMsrSys(m_nMsrSys);
	m_qafsd->SetUserWs(m_wsUser);
	m_qafsd->SetHelpFile(m_strHelpFile.Chars());
	m_qafsd->SetCustomUserLevel(m_nCustomStyleLevel);
	m_qafsd->SetAppClsid(&m_clsidApp);

	for (int i = 0; i < AfStylesDlg::kcdlgv; i++)
	{
		if (m_strTabHelpFileUrl[i].Length() > 0)
			m_qafsd->SetTabHelpFileUrl(i, m_strTabHelpFileUrl[i].Chars());
	}
	m_qafsd->SetLgWritingSystemFactory(m_qwsf);
	m_qafsd->SetApplicableStyleContexts(m_vApplicableContexts);
	m_qafsd->SetHelpTopicProvider(m_qhtprov);
	m_qafsd->SetupForAdjustTsTextProps(m_fCanDoRtl, m_fOuterRtl, m_fFontFeatures,
		m_f1DefaultFont, m_qvss, m_vqttpPara, m_vqttpChar, m_fCanFormatChar, m_fReloadDb,
		m_vwsAvailable, m_hvoRootObj, m_qstrmLog, m_stuStyleName, m_fOnlyCharStyles);
}


/*----------------------------------------------------------------------------------------------
	Second step of displaying the modal dialog: do it.
----------------------------------------------------------------------------------------------*/
void FwCppStylesDlg::DoModalDialog(int * pncid)
{
	AssertPtr(m_qafsd);

	int ncid = m_qafsd->DoModalForAdjustTsTextProps(m_hwndParent);
	if (pncid)
		*pncid = ncid;
}


/*----------------------------------------------------------------------------------------------
	Final step of displaying the modal dialog: retrieve the results and delete the dialog
	object.
----------------------------------------------------------------------------------------------*/
void FwCppStylesDlg::GetModalResults(int ncid)
{
	AssertPtr(m_qafsd);

	m_fResult = m_qafsd->ResultsForAdjustTsTextProps(ncid, &m_stuStyleName,
		m_fStylesChanged, m_fApply);

	m_qafsd.Clear();
}


/*----------------------------------------------------------------------------------------------
	Display the styles dialog modally and let it do its thing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::ShowModal(int * pncid)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pncid);	// since ChkComOutPtrN doesn't exist.
	if (pncid)
		*pncid = 0;

	SetupForDoModal();
	DoModalDialog(pncid);
	GetModalResults(*pncid);

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


/*----------------------------------------------------------------------------------------------
	Get the results of calling ShowModal.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwCppStylesDlg::GetResults(BSTR * pbstrStyleName, ComBool * pfStylesChanged,
	ComBool * pfApply, ComBool * pfReloadDb, ComBool * pfResult)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrStyleName);
	ChkComOutPtr(pfStylesChanged);
	ChkComOutPtr(pfApply);
	ChkComOutPtr(pfReloadDb);
	ChkComOutPtr(pfResult);

	m_stuStyleName.GetBstr(pbstrStyleName);
	*pfStylesChanged = m_fStylesChanged;
	*pfApply = m_fApply;
	*pfReloadDb = m_fReloadDb;
	*pfResult = m_fResult;

	END_COM_METHOD(g_factOPD, IID_IFwCppStylesDlg);
}


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)
