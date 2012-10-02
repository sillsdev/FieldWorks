/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfFwTool.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a default implementation for the IFwTool interface.
	It assumes that the application overrides the NewMainWindow method of AfApp.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Constructor/Destructor
***********************************************************************************************/

AfFwTool::AfFwTool()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	AfApp::Papp()->IncExportedObjects();
}

AfFwTool::~AfFwTool()
{
	ModuleEntry::ModuleRelease();
	AfApp::Papp()->DecExportedObjects();
}

static DummyFactory g_fact(_T("SIL.AppCore.AfFwTool"));

/***********************************************************************************************
	IUnknown Methods
***********************************************************************************************/
STDMETHODIMP AfFwTool::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IFwTool)
		*ppv = static_cast<IFwTool *>(this);
	else
		// Note: When we use the ROT, we get numerous calls here, including IID_IProxyManager,
		// IID_IMarshal, IID_IStdMarshalInfo, IID_IExternalConnection, etc. We should not be
		// using a WarnHr.
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


/***********************************************************************************************
	Generic factory stuff to allow creating an instance with CoCreateInstance.
	(A class factory with appropriate CLSID should be included in each particular application
	which uses this mechanism, e.g.,

static GenericFactory g_fact(
	"SIL.RN.AfFwTool",
	&CLSID_ResearchNotebook,
	"SIL Research Notebook",
	"Apartment",
	&AfFwTool::CreateCom);
***********************************************************************************************/

void AfFwTool::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<AfFwTool> qaft;
	qaft.Attach(NewObj AfFwTool());		// ref count initialy 1
	CheckHr(qaft->QueryInterface(riid, ppv));
}

/***********************************************************************************************
	IFwTool implementation
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Open a main window on a particular object in a particular database.
	Will fail if the specified tool cannot handle the specified top-level object.
	Returns a value which can be used to identify the particular window in subsequent calls.
	@param bstrServerName Name of the MSDE/SQLServer computer.
	@param bstrDbName Name of the database.
	@param hvoLangProj Which languate project within the database.
	@param hvoMainObj The top-level object on which to open the window.
	@param encUi The user-interface writing system.
	@param nTool A tool-dependent identifier of which tool to use.
	@param nParam Another tool-dependent parameter.
	@param ppidNew Process id of the new main window's process.
	@param phtool Handle to the newly created window.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfFwTool::NewMainWnd(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
	int hvoMainObj, int encUi, int nTool, int nParam, int * ppidNew, long * phtool)
{
	BEGIN_COM_METHOD
	ChkComBstrArg(bstrServerName);
	ChkComBstrArg(bstrDbName);
	ChkComOutPtr(ppidNew);
	ChkComOutPtr(phtool);

	const CLSID * pclsid = AfApp::Papp()->GetAppClsid();
	DWORD dwRegister = 0;
	if (pclsid)
	{
		// Check to see if the application is already running.
		IRunningObjectTablePtr qrot;
		CheckHr(::GetRunningObjectTable(0, &qrot));
		IMonikerPtr qmnk;
		CheckHr(::CreateClassMoniker(*pclsid, &qmnk));
		IUnknownPtr qunk;
		IFwToolPtr qtool;
		if (SUCCEEDED(qrot->GetObject(qmnk, &qunk)))
		{
			if (SUCCEEDED(qunk->QueryInterface(IID_IFwTool, (void **)&qtool)) &&
				qtool.Ptr() != this)
			{
				// The document is already open in another process, so create a new
				// window in that process.
				qtool->NewMainWnd(bstrServerName, bstrDbName, hvoLangProj, hvoMainObj,
					encUi, nTool, nParam, ppidNew, phtool);
				// After we create the new window in the other process, we exit here.
				// When the IFwTool pointer pointing to 'this' goes out of scope, the
				// second process (if it was launched from Windows Explorer or the
				// command line) will shut down automatically.
				return S_OK;
			}
		}
		else
		{
			// Note: ROTFLAGS_ALLOWANYCLIENT causes an error on Win2K (The class is
			// configured to run as a security ID different from the caller).
			CheckHr(qrot->Register(ROTFLAGS_REGISTRATIONKEEPSALIVE, this, qmnk,
				&dwRegister));
		}
	}
	AfApp::Papp()->NewMainWnd(bstrServerName, bstrDbName, hvoLangProj, hvoMainObj,
		encUi, nTool, nParam, dwRegister);
	// The handle we return is actually the hwnd of the top-level window.
	// ENHANCE JohnT: we should probably make htool a long...
	*phtool = (long)(AfApp::Papp()->GetCurMainWnd()->Hwnd());
	if (ppidNew)
		*ppidNew = (int)::GetCurrentProcessId();

	return S_OK;

	END_COM_METHOD(g_fact, IID_IFwTool);
}


/*----------------------------------------------------------------------------------------------
	Open a main window on a particular object in a particular database on a particular field
	in a particular object using a particular view.
	Will fail if the specified tool cannot handle the specified top-level object.
	Returns a value which can be used to identify the particular window in subsequent calls.
	@param bstrServerName Name of the MSDE/SQLServer computer.
	@param bstrDbName Name of the database.
	@param hvoLangProj Which languate project within the database.
	@param hvoMainObj The top-level object on which to open the window.
	@param encUi The user-interface writing system.
	@param nTool A tool-dependent identifier of which tool to use.
	@param nParam Another tool-dependent parameter.
	@param prghvo Pointer to an array of object ids.
	@param chvo Number of object ids in prghvo.
	@param prgflid Pointer to an array of flids.
	@param cflid Number of flids in prgflid.
	@param ichCur Cursor offset from beginning of field.
	@param nView The view to display when showing the first object. Use -1 to use the first
		data entry view.
	@param ppidNew Process id of the new main window's process.
	@param phtool Handle to the newly created window.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfFwTool::NewMainWndWithSel(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
	int hvoMainObj, int encUi, int nTool, int nParam, const HVO * prghvo, int chvo,
		const int * prgflid, int cflid, int ichCur, int nView, int * ppidNew, long * phtool)
{
	BEGIN_COM_METHOD
	ChkComBstrArg(bstrServerName);
	ChkComBstrArg(bstrDbName);
	ChkComArrayArg(prghvo, chvo);
	ChkComArrayArg(prgflid, cflid);
	ChkComOutPtr(ppidNew);
	ChkComOutPtr(phtool);

	const CLSID * pclsid = AfApp::Papp()->GetAppClsid();
	DWORD dwRegister = 0;
	if (pclsid)
	{
		// Check to see if the application is already running.
		IRunningObjectTablePtr qrot;
		CheckHr(::GetRunningObjectTable(0, &qrot));
		IMonikerPtr qmnk;
		CheckHr(::CreateClassMoniker(*pclsid, &qmnk));
		IUnknownPtr qunk;
		IFwToolPtr qtool;
		if (SUCCEEDED(qrot->GetObject(qmnk, &qunk)))
		{
			if (SUCCEEDED(qunk->QueryInterface(IID_IFwTool, (void **)&qtool)) &&
				qtool.Ptr() != this)
			{
				// The document is already open in another process, so create a new
				// window in that process.
				qtool->NewMainWndWithSel(bstrServerName, bstrDbName, hvoLangProj, hvoMainObj,
					encUi, nTool, nParam, prghvo, chvo, prgflid, cflid, ichCur, nView,
					ppidNew, phtool);
				// After we create the new window in the other process, we exit here.
				// When the IFwTool pointer pointing to 'this' goes out of scope, the
				// second process (if it was launched from Windows Explorer or the
				// command line) will shut down automatically.
				return S_OK;
			}
		}
		else
		{
			// Note: ROTFLAGS_ALLOWANYCLIENT causes an error on Win2K (The class is
			// configured to run as a security ID different from the caller).
			CheckHr(qrot->Register(ROTFLAGS_REGISTRATIONKEEPSALIVE, this, qmnk,
				&dwRegister));
		}
	}
	AfApp::Papp()->NewMainWndWithSel(bstrServerName, bstrDbName, hvoLangProj, hvoMainObj,
		encUi, nTool, nParam, prghvo, chvo, prgflid, cflid, ichCur, nView, dwRegister);
	// The handle we return is actually the hwnd of the top-level window.
	// ENHANCE JohnT: we should probably make htool a long...
	*phtool = (long)(AfApp::Papp()->GetCurMainWnd()->Hwnd());
	if (ppidNew)
		*ppidNew = (int)::GetCurrentProcessId();

	return S_OK;

	END_COM_METHOD(g_fact, IID_IFwTool);
}


/*----------------------------------------------------------------------------------------------
	Ask a main window to close. May return *pfCancelled true if closing the window requires
	the user to confirm a save, and the user says cancel. In this case the caller should
	normally abort whatever required the window to close.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfFwTool::CloseMainWnd(long htool, ComBool *pfCancelled)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfCancelled);

	Vector<AfMainWndPtr> &vqafw = AfApp::Papp()->GetMainWindows();
	for (int i = 0; i < vqafw.Size(); i++)
	{
		if (((long)(vqafw[i]->Hwnd())) == htool)
		{
			// close the window
			::SendMessage((HWND)htool, WM_CLOSE, 0, 0);
			return S_OK;
		}
	}
	return S_FALSE;

	END_COM_METHOD(g_fact, IID_IFwTool);
}


/*----------------------------------------------------------------------------------------------
	Close any windows associated with a database, save the database, clear all caches, and
	shutdown the connection to the database.
	@param bstrSvrName Name of the server hosting the database.
	@param bstrDbName Name of the database to close.
	@param fOkToClose True to close the application if there are no further connections after
		the requested connection is closed. False leaves the application open.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfFwTool::CloseDbAndWindows(BSTR bstrSvrName, BSTR bstrDbName, ComBool fOkToClose)
{
	BEGIN_COM_METHOD
	ChkComBstrArg(bstrSvrName);
	ChkComBstrArg(bstrDbName);

	OLECHAR * pszDb = bstrDbName ? bstrDbName : L"";
	OLECHAR * pszSvr = bstrSvrName ? bstrSvrName : L"";
	AfApp::Papp()->CloseDbAndWindows(pszDb, pszSvr, (bool)fOkToClose);

	return S_OK;

	END_COM_METHOD(g_fact, IID_IFwTool);
}

/*----------------------------------------------------------------------------------------------
	Prepare application to enter or leave a state in which an app-modal process can be
	performed by disabling/enabling all main windows associated with this tool.
	NOTE: This has never been tested, so we have no idea if it works. :~)
	@param fModalState If true, this will enter the modal state. Otherwise it will leave modal
		state.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfFwTool::SetAppModalState(ComBool fModalState)
{
	BEGIN_COM_METHOD

	Vector<AfMainWndPtr> &vqafw = AfApp::Papp()->GetMainWindows();
	for (int i = 0; i < vqafw.Size(); i++)
	{
		// enable/disable the window
		::SendMessage(vqafw[i]->Hwnd(), WM_ENABLE, fModalState, 0);
	}

	return S_OK;

	END_COM_METHOD(g_fact, IID_IFwTool);
}
