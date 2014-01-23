/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: DebugReport.cpp
Responsibility: TE Team

	Implementation of the DebugReport class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#include "DebugReport.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

#ifdef DEBUG

//:>********************************************************************************************
//:>	DebugReport methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	C'tor
----------------------------------------------------------------------------------------------*/
DebugReport::DebugReport()
{
	m_cref = 1;
	::DebugProcsInit();
	m_oldReportHook = ::DbgSetReportHook(DebugReport::ReportHandler);
	// Set MsgBoxHook only when we set the sink
	ModuleEntry::ModuleAddRef();
}

/*----------------------------------------------------------------------------------------------
	D'tor
----------------------------------------------------------------------------------------------*/
DebugReport::~DebugReport()
{
	::DbgSetReportHook(m_oldReportHook);
	ClearSink();
	::DebugProcsExit();
	ModuleEntry::ModuleRelease();
}


//:>********************************************************************************************
//:>	DebugReport - Generic factory stuff to allow creating an instance w/ CoCreateInstance.
//:>********************************************************************************************

static GenericFactory g_factDbr(
	_T("SIL.Kernel.DebugReport"),
	&CLSID_DebugReport,
	_T("SIL Debug Report Handler"),
	_T("Apartment"),
	&DebugReport::CreateCom);


void DebugReport::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
	{
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	}
	ComSmartPtr<DebugReport> qdbr;
	// Ref count initially 1
	qdbr.Attach(NewObj DebugReport());
	CheckHr(qdbr->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	DebugReport - IUnknown Methods
//:>********************************************************************************************

STDMETHODIMP DebugReport::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IDebugReport *>(this));
	else if (iid == IID_IDebugReport)
		*ppv = static_cast<IDebugReport *>(this);
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

STDMETHODIMP_(UCOMINT32) DebugReport::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}

STDMETHODIMP_(UCOMINT32) DebugReport::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}

//:>********************************************************************************************
//:>	DebugReport - IDebugReport Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	${IDebugReport#SetSink}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DebugReport::SetSink(IDebugReportSink * pSink)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pSink);

	KernelGlobals::s_qReportSink = pSink;
	m_oldAssertProc = ::SetAssertProc(DebugReport::AssertProcWrapper);

	END_COM_METHOD(g_factDbr, IID_IDebugReport);
}

/*----------------------------------------------------------------------------------------------
	${IDebugReport#ClearSink}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP DebugReport::ClearSink()
{
	BEGIN_COM_METHOD;

	KernelGlobals::s_qReportSink = NULL;
	::SetAssertProc(m_oldAssertProc);
	m_oldAssertProc = NULL;

	END_COM_METHOD(g_factDbr, IID_IDebugReport);
}

/*----------------------------------------------------------------------------------------------
	Callback for DebugProcs
----------------------------------------------------------------------------------------------*/
void __stdcall DebugReport::ReportHandler(int reportType, char * szMsg)
{
	if (KernelGlobals::s_qReportSink)
	{
		StrAnsi sta = szMsg;
		SmartBstr bstr;
		sta.GetBstr(&bstr);
		KernelGlobals::s_qReportSink->Report((CrtReportType)reportType, bstr);
	}
}

void __stdcall DebugReport::AssertProcWrapper(const char * pszExp, const char * pszFile, int nLine,
		HMODULE)
{
	if (KernelGlobals::s_qReportSink)
	{
		StrAnsi sta = pszExp;
		SmartBstr exp;
		sta.GetBstr(&exp);
		SmartBstr file;
		sta = pszFile;
		sta.GetBstr(&file);
		KernelGlobals::s_qReportSink->AssertProc(exp, file, nLine);
	}
}

#endif
