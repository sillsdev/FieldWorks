/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: CmDataObject.h
Responsibility: Ken Zook
Last reviewed:

	class CmDataObject : public IDataObject
	class CmEnumFORMATETC : public IEnumFORMATETC
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef CmDataObject_H
#define CmDataObject_H 1


/*----------------------------------------------------------------------------------------------
	This class is used to hold an object being transferred via the clipboard or drag and drop.
	Hungarian: cdo
----------------------------------------------------------------------------------------------*/
class CmDataObject : public IDataObject
{
public:

	static void Create(const OLECHAR * pszSvrName, const OLECHAR * pszDbName, HVO hvo, int clid,
		ITsString * ptss, int pid, IDataObject ** ppdobj);
	static unsigned int GetClipboardType();

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// IDataObject methods.
	STDMETHOD(GetData)(FORMATETC * pformatetcIn, STGMEDIUM * pmedium);
	STDMETHOD(GetDataHere)(FORMATETC * pformatetc, STGMEDIUM * pmedium);
	STDMETHOD(QueryGetData)(FORMATETC * pformatetc);
	STDMETHOD(GetCanonicalFormatEtc)(FORMATETC * pformatectIn, FORMATETC * pformatetcOut);
	STDMETHOD(SetData)(FORMATETC * pformatetc, STGMEDIUM * pmedium, BOOL fRelease);
	STDMETHOD(EnumFormatEtc)(DWORD dwDirection, IEnumFORMATETC ** ppenumFormatEtc);
	STDMETHOD(DAdvise)(FORMATETC * pformatetc, DWORD advf, IAdviseSink * pAdvSink,
		DWORD * pdwConnection);
	STDMETHOD(DUnadvise)(DWORD dwConnection);
	STDMETHOD(EnumDAdvise)(IEnumSTATDATA ** ppenumAdvise);

protected:

	// The first time CmDataObject::GetClipboardType is called, this is set to whatever
	// RegisterClipboardFormat("CF_CmObject") returns.
	static unsigned int cfCmObject;

	CmDataObject(void);
	~CmDataObject(void);

	void Init(const OLECHAR * pszSvrName, const OLECHAR * pszDbName, HVO hvo, int clid,
		ITsString * ptss, int pid);

	long m_cref; // Standard COM reference count.
	StrUni m_stuSvrName; // Server name for the source server.
	StrUni m_stuDbName; // Database name for the source database.
	HVO m_hvo; // Object id of the source item.
	int m_clid; // Class id of the source item.
	// A string representing the source item used for dragging or inserting when text is desired.
	ITsStringPtr m_qtss;
	// This is the process id of the source process. It is not possible to reliably move a
	// CmObject between processes because they are using different caches, undo stacks, they may
	// have multiple windows open, and there is no way to get information back to the source of
	// where the new destination. Thus the only way to get windows in the source process
	// reliably updated would be to reload all of the data after the move. This is too costly,
	// so we will simply disable moves between processes. However, a copy or link operation can
	// go cross-process without a problem.
	int m_pid;
};

DEFINE_COM_PTR(CmDataObject);

/*----------------------------------------------------------------------------------------------
	This class provides an IEnumFORMATETC COM object which supports cfCmObject, CF_UNICODETEXT,
	CF_OEMTEXT, and CF_TEXT formatted clipboard formats.  The first is what we define for the
	CmDataObject class.

	Hungarian: cenum
----------------------------------------------------------------------------------------------*/
class CmEnumFORMATETC : IEnumFORMATETC
{
public:
	//:> Static methods
	static void Create(IEnumFORMATETC ** ppenum);

	//:> IUnknown methods
	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	//:> IEnumFORMATETC methods
	STDMETHOD(Next)(ULONG celt, FORMATETC * rgelt, ULONG * pceltFetched);
	STDMETHOD(Skip)(ULONG celt);
	STDMETHOD(Reset)(void);
	STDMETHOD(Clone)(IEnumFORMATETC ** ppenum);

protected:
	//:> Member variables.
	int m_cref;	// Standard reference count variable.
	int m_ifmte; // Current index into CmEnumFORMATETC::g_rgfmte for this enumerator.

	//:> Static member variables.
	enum {kcfmteLim = 4};
	// Global array of FORMATETC data structures containing all the supported formats.
	static FORMATETC g_rgfmte[kcfmteLim];

	//:> Constructors and destructors.
	CmEnumFORMATETC();
	~CmEnumFORMATETC();
};

DEFINE_COM_PTR(CmEnumFORMATETC);

#endif // !CmDataObject_H