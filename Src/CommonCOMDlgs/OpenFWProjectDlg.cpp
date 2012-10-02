/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: OpenFWProjectDlg.cpp
Responsibility: Randy Regnier
Last reviewed: Not yet.

Description:
	Implementation of OpenFWProjectDlg.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma hdrstop
#include "main.h"
#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	IMPLEMENTATION OF OpenFWProjectDlg.
//:>********************************************************************************************


//:>********************************************************************************************
//:>	OpenFWProjectDlg - Generic factory stuff to allow creating an instance
//:>		with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factOPD(
	_T("SIL.FW.OpenFWProjectDlg"),
	&CLSID_OpenFWProjectDlg,
	_T("SIL FieldWorks Open Project Dialog"),
	_T("Apartment"),
	&OpenFWProjectDlg::CreateCom);


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
OpenFWProjectDlg::OpenFWProjectDlg()
{
	m_cref = 1;

	m_fHaveProject = false;
	m_hvoProj = 0;
	m_sbstrProject = L"";
	m_sbstrDatabase = L"";
	m_sbstrMachine = L"";
	m_fHaveSubitem = false;
	m_hvoSubitem = 0;
	m_sbstrSubitemName = L"";

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
OpenFWProjectDlg::~OpenFWProjectDlg()
{
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Static method to create an instance of OpenFWProjectDlg.
----------------------------------------------------------------------------------------------*/
void OpenFWProjectDlg::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
	{
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	}
	ComSmartPtr<OpenFWProjectDlg> qcofwpd;
	qcofwpd.Attach(NewObj OpenFWProjectDlg());	// ref count initially 1
	CheckHr(qcofwpd->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	IUnknown methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown and IOpenFWProjectDlg are
	supported.

	This is a standard COM IUnknown method.

	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.

	@return SOK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OpenFWProjectDlg::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IOpenFWProjectDlg *>(this));
	else if (iid == IID_IOpenFWProjectDlg)
		*ppv = static_cast<IOpenFWProjectDlg *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IOpenFWProjectDlg);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


STDMETHODIMP_(ULONG) OpenFWProjectDlg::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


STDMETHODIMP_(ULONG) OpenFWProjectDlg::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}


//:>********************************************************************************************
//:>	IOpenFWProjectDlg methods.
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Show the dialog.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OpenFWProjectDlg::Show(
	IStream * fist, /* [in] */
	BSTR bstrCurrentServer, /* [in] */
	BSTR bstrLocalServer, /* [in] */
	BSTR bstrUserWs, /* [in] */
	DWORD hwndParent, /* [in] */
	ComBool fAllowMenu, /* [in] */
	int clidSubitem, /* [in] */
	BSTR bstrHelpFullUrl /* [in] */)
{
	BEGIN_COM_METHOD;

	int rid = kridOpenProjDlg;
	OpenProjDlgPtr qopd;
	if (clidSubitem > 0)
	{
		switch (clidSubitem)
		{
		default:	// Class not supported.
			Assert(false);
			return E_FAIL;
			break;
		case kclidCmPossibilityList:
			CleOpenProjDlgPtr qcopd;
			qcopd.Create();
			qopd = qcopd;
			rid = kridOpenProjSubitemDlg;
			break;
		}
	}
	else
		qopd.Create();

	m_fHaveProject = false;
	m_fHaveSubitem = false;
	qopd->Init(fist, bstrCurrentServer, bstrLocalServer, bstrUserWs, fAllowMenu, clidSubitem,
		bstrHelpFullUrl, rid, m_qwsf);

	if (qopd->DoModal((HWND)hwndParent, rid) == kctidOk)
	{
		StrUni stuProject;
		StrUni stuDatabase;
		StrUni stuMachine;
		if (qopd->GetSelectedProject(m_hvoProj, stuProject, stuDatabase, stuMachine, &m_guid))
		{
			m_sbstrProject = stuProject.Bstr();
			m_sbstrDatabase = stuDatabase.Bstr();
			m_sbstrMachine = stuMachine.Bstr();
			StrUni stuName;
			m_fHaveProject = true;
			if (clidSubitem > 0 && qopd->GetSelectedSubitem(m_hvoSubitem, stuName))
			{
				m_sbstrSubitemName = stuName.Bstr();
				m_fHaveSubitem = true;
			}
		}
	}

	return S_OK;

	END_COM_METHOD(g_factOPD, IID_IOpenFWProjectDlg);
}


/*----------------------------------------------------------------------------------------------
	Get the results from the user.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OpenFWProjectDlg::GetResults(
	ComBool * fHaveProject, /* [out] */
	int * hvoProj, /* [out] */
	BSTR * bstrProject, /* [out] */
	BSTR * bstrDatabase, /* [out] */
	BSTR * bstrMachine, /* [out] */
	GUID * guid, /* [out] */
	ComBool * fHaveSubitem, /* [out] */
	int * hvoSubitem, /* [out] */
	BSTR * bstrName /* [out] */)
{
	BEGIN_COM_METHOD;

	*fHaveProject = m_fHaveProject;
	if (m_fHaveProject)
	{
		*hvoProj = m_hvoProj;
		m_sbstrProject.Copy(bstrProject);
		m_sbstrDatabase.Copy(bstrDatabase);
		m_sbstrMachine.Copy(bstrMachine);
		*guid = m_guid;
	}
	*fHaveSubitem = m_fHaveSubitem;
	if (m_fHaveSubitem)
	{
		*hvoSubitem = m_hvoSubitem;
		m_sbstrSubitemName.Copy(bstrName);
	}

	return S_OK;

	END_COM_METHOD(g_factOPD, IID_IOpenFWProjectDlg);
}

/*----------------------------------------------------------------------------------------------
	Store a pointer to the application's current writing system factory.  This is needed
	whenever using this implementation from C# code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP OpenFWProjectDlg::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD;

	m_qwsf = pwsf;

	END_COM_METHOD(g_factOPD, IID_IOpenFWProjectDlg);
}
