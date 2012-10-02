/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgTsDataObject.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

	class LgTsDataObject : public IDataObject
	class LgTsEnumFORMATETC : public IEnumFORMATETC
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LgTsDataObject_H_INCLUDED
#define LgTsDataObject_H_INCLUDED

/*----------------------------------------------------------------------------------------------
	This class provides an IDataObject wrapper around a TsString object.  This facilitates
	passing TsString data via the clipboard or "drag and drop".

	Hungarian: tsdo
----------------------------------------------------------------------------------------------*/
class LgTsDataObject : public IDataObject, ILgTsDataObject
{
public:
	//:> Static methods
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	//:> IUnknown methods

	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	//:> ILgTsDataObject methods
	STDMETHOD(Init)(ILgTsStringPlusWss * ptssencs);
	STDMETHOD(GetClipboardType)(UINT* pType);

	//:> IDataObject methods

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
	//:> Static member variables.
	// The first time LgTsDataObject::GetClipboardType is called, this is set to whatever
	// RegisterClipboardFormat("CF_TsString") returns.
	static unsigned int s_cfTsString;

	//:> Member variables.
	long m_cref;						// Standard reference count variable.
	ILgTsStringPlusWssPtr m_qtsswss;	// ILgTsStringPlusWss COM object this LgTsDataObject wraps.

	//:> Constructors and destructors.
	LgTsDataObject();
	~LgTsDataObject();

};
DEFINE_COM_PTR(LgTsDataObject);

/*----------------------------------------------------------------------------------------------
	This class provides an IEnumFORMATETC COM object which supports s_cfTsString,
	CF_UNICODETEXT, CF_OEMTEXT, and CF_TEXT formatted clipboard formats.  The first is what we
	define for the LgTsDataObject class.

	Hungarian: tsenum
----------------------------------------------------------------------------------------------*/
class LgTsEnumFORMATETC : IEnumFORMATETC
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
	long m_cref;		// Standard reference count variable.
	int m_ifmte;		// Current index into LgTsEnumFORMATETC::g_rgfmte for this enumerator.

	//:> Static member variables.
	enum { kcfmteLim = 4 };
	// Global array of FORMATETC data structures containing all the supported formats.
	static FORMATETC g_rgfmte[kcfmteLim];

	//:> Constructors and destructors.
	LgTsEnumFORMATETC();
	~LgTsEnumFORMATETC();
};

// Local Variables:
// mode:C++
// End: (These 3 lines are useful to Steve McConnel.)

#endif  /*LgTsDataObject_H_INCLUDED*/
