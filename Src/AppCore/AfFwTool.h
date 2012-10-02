/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfFwTool.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a default implementation for the IFwTool interface.
	It assumes that the application overrides the NewMainWindow method of AfApp.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AfFwTool_H
#define AfFwTool_H 1

class AfFwTool : public IFwTool
{
public:
	// Constructors/destructors/etc.
	AfFwTool();
	virtual ~AfFwTool();
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// FwTool methods
	STDMETHOD(NewMainWnd)(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
		int hvoMainObj, int encUi, int nTool, int nParam, int * ppidNew, long * phtool);
	STDMETHOD(NewMainWndWithSel)(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
		int hvoMainObj, int encUi, int nTool, int nParam, const HVO * prghvo, int chvo,
		const int * prgflid, int cflid, int ichCur, int nView, int * ppidNew, long * phtool);
	STDMETHOD(CloseMainWnd)(long htool, ComBool *pfCancelled);
	STDMETHOD(CloseDbAndWindows)(BSTR bstrSvrName, BSTR bstrDbName, ComBool fOkToClose);
	STDMETHOD(SetAppModalState)(ComBool fModalState);

protected:
	long m_cref;
};
#endif // !AfFwTool_H
