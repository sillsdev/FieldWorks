/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2005 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwCheckAnthroList.h
Responsibility:
Last reviewed: never

Description:
	The standard definition for the IFwCheckAnthroList interface.
-------------------------------------------------------------------------------*//*:End Ignore*/

#pragma once
#ifndef FWCHECKANTHROLIST_H_INCLUDED
#define FWCHECKANTHROLIST_H_INCLUDED


/*----------------------------------------------------------------------------------------------
	This class implements the IFwCheckAnthroList interface which supports bringing up a dialog
	to choose which anthropology list the user wants to install into a language project.
	Hungarian: fcal
----------------------------------------------------------------------------------------------*/
class FwCheckAnthroList : public IFwCheckAnthroList
{
public:
	//:> Standard COM creation method.
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods.
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	// IFwCheckAnthroList methods.
	STDMETHOD(CheckAnthroList)(IOleDbEncap * pode, DWORD hwndParent, BSTR bstrProjName,
		int wsDefault);
	STDMETHOD(put_Description)(BSTR bstrDescription);
	STDMETHOD(put_HelpFilename)(BSTR bstrHelpFilename);

protected:
	// Construction / Destruction
	FwCheckAnthroList();
	~FwCheckAnthroList();

	int m_cref;
	SmartBstr m_sbstrDescription;
	StrApp m_strHelpFilename;
};

#endif /* FWCHECKANTHROLIST_H_INCLUDED */
