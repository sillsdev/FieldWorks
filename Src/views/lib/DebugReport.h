/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: DebugReport.h
Responsibility: TE Team

	Declarations for all of DebugReport.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef DebugReport_H
#define DebugReport_H 1

#ifdef DEBUG

#include "DebugProcs.h"

/*----------------------------------------------------------------------------------------------
	Cross-Reference: ${IDebugReport}

	@h3{Hungarian: dbr}
----------------------------------------------------------------------------------------------*/
class DebugReport: public IDebugReport
{
public:
	DebugReport();
	~DebugReport();

	STDMETHOD_(UCOMINT32, AddRef)(void);
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, Release)(void);

	STDMETHOD(SetSink)(IDebugReportSink * pSink);
	STDMETHOD(ClearSink)(void);

protected:
	int m_cref;
	_DBG_REPORT_HOOK m_oldReportHook;
	Pfn_Assert m_oldAssertProc;
	static void __stdcall ReportHandler(int reportType, char * szMsg);
	static void __stdcall AssertProcWrapper(const char * pszExp, const char * pszFile, int nLine,
		HMODULE hmod);

};
DEFINE_COM_PTR(DebugReport);


#endif
#endif // !DebugReport_H
