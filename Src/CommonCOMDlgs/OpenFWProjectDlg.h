/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: OpenFWProjectDlg.h
Responsibility: Randy Regnier
Last reviewed: Not yet.

Description:
	Header file for the IOpenFWProjectDlg interface implementation.

	OpenFWProjectDlg : IOpenFWProjectDlg
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef OPENFWPROJECTDLG_H_INCLUDED
#define OPENFWPROJECTDLG_H_INCLUDED

/*----------------------------------------------------------------------------------------------
	Implementation of interface for choosing a project and optionally a subitem
	stored in a database.
	Cross-Reference: ${IOpenFWProjectDlg}

	@h3{Hungarian: ofwpd)
----------------------------------------------------------------------------------------------*/
class OpenFWProjectDlg : public IOpenFWProjectDlg
{
public:
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods.
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	// IOpenFWProjectDlg methods.
	STDMETHOD(Show)(
		IStream * fist, /* [in] */
		BSTR bstrCurrentServer, /* [in] */
		BSTR bstrLocalServer, /* [in] */
		BSTR bstrUserWs, /* [in] */
		DWORD hwndParent, /* [in] */
		ComBool fAllowMenu, /* [in] */
		int clidSubitem, /* [in] */
		BSTR bstrHelpFullUrl /* [in] */);
	STDMETHOD(GetResults)(ComBool * fHaveProject, /* [out] */
		int * hvoProj, /* [out] */
		BSTR * bstrProject, /* [out] */
		BSTR * bstrDatabase, /* [out] */
		BSTR * bstrMachine, /* [out] */
		GUID * guid, /* [out] */
		ComBool * fHaveSubitem, /* [out] */
		int * hvoSubitem, /* [out] */
		BSTR * bstrName /* [out] */);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);

protected:
	OpenFWProjectDlg();
	~OpenFWProjectDlg();

	int m_cref;

	ComBool m_fHaveProject;
	int m_hvoProj;
	SmartBstr m_sbstrProject;
	SmartBstr m_sbstrDatabase;
	SmartBstr m_sbstrMachine;
	GUID m_guid;
	ComBool m_fHaveSubitem;
	HVO m_hvoSubitem;
	SmartBstr m_sbstrSubitemName;

	StrUni m_stuHelpFileName;
	ILgWritingSystemFactoryPtr m_qwsf;
};
DEFINE_COM_PTR(OpenFWProjectDlg);

#endif /*OPENFWPROJECTDLG_H_INCLUDED*/
