/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

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

	STDMETHOD_(ULONG, AddRef)(void);
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	STDMETHOD(ShowAssertMessageBox)(ComBool fShowMessageBox);
	STDMETHOD(SetSink)(IDebugReportSink * pSink);
	STDMETHOD(ClearSink)(void);

protected:
	int m_cref;
	_DBG_REPORT_HOOK m_oldHook;
	static IDebugReportSinkPtr s_qReportSink;
	static void __stdcall ReportHandler(int reportType, char * szMsg);

};
DEFINE_COM_PTR(DebugReport);


#endif
#endif // !DebugReport_H
