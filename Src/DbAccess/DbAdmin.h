/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, SIL International. All rights reserved.

File: DbAdmin.h
Responsibility: John Thomson

Description:
	Header file for the DbAdmin data access module.

	This file contains class declarations for the following classes:
		DbAdmin
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef __DbAdmin_H__
#define __DbAdmin_H__


typedef ComSmartPtr<IDbAdmin> IDbAdminPtr;

/*----------------------------------------------------------------------------------------------
	Standard implementation of the IDbAdmin interface.

	Cross Reference: ${IDbAdmin}
----------------------------------------------------------------------------------------------*/
class DbAdmin : public IDbAdmin
{
protected:
	long m_cref;

private:

public:
	DbAdmin();
	~DbAdmin();

	STDMETHOD_(ULONG, AddRef)(void);
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	// IDbAdmin methods
	STDMETHOD(CopyDatabase)(BSTR bstrSrcPathName, BSTR bstrDstPathName);
	STDMETHOD(AttachDatabase)(BSTR bstrDatabaseName, BSTR bstrPathName);
	STDMETHOD(DetachDatabase)(BSTR bstrDatabaseName);
	STDMETHOD(RenameDatabase)(BSTR bstrDirName, BSTR bstrOldName,
		BSTR bstrNewName, ComBool fDetachBefore, ComBool fAttachAfter);
	STDMETHOD(putref_LogStream)(IStream * pstrm);
	STDMETHOD(get_FwRootDir)(BSTR * pbstr);
	STDMETHOD(get_FwMigrationScriptDir)(BSTR * pbstr);
	STDMETHOD(get_FwDatabaseDir)(BSTR * pbstr);
	STDMETHOD(get_FwTemplateDir)(BSTR * pbstr);
	STDMETHOD(SimplyRenameDatabase)(BSTR bstrOldName, BSTR bstrNewName);

protected:
	void GetMdfFileOfDatabase(IOleDbEncap * pode, const BSTR bstrDatabase, BSTR * pbstrMdfFile);
	bool DetachDatabase(IOleDbEncap * pode, const BSTR bstrDatabase);
	void AttachDatabase(IOleDbEncap * pode, const OLECHAR * pszDatabase,
		const OLECHAR * pszMdfFile, const OLECHAR * pszLdfFile);

	IStreamPtr m_qfist; // log stream pointer.
};

#endif   // __DbAdmin_H__
