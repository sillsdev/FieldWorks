/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgTsDataObject.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	See LgTsDataObject.h for a description of the LgTsDataObject and LgTsEnumFORMATETC classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE
//:End Ignore

//:>********************************************************************************************
//:>	LgTsDataObject Methods
//:>********************************************************************************************

unsigned int LgTsDataObject::s_cfTsString = 0;

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.FieldWorks.LgTsDataObject"),
	&CLSID_LgTsDataObject,
	_T("SIL LgTsDataObject"),
	_T("Apartment"),
	&LgTsDataObject::CreateCom);


/*----------------------------------------------------------------------------------------------
	This static method creates a new LgTsDataObject object.
----------------------------------------------------------------------------------------------*/
void LgTsDataObject::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgTsDataObject> qtsdo;
	qtsdo.Attach(NewObj LgTsDataObject); 		// ref count initialy 1
	CheckHr(qtsdo->QueryInterface(riid, ppv));
}

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
LgTsDataObject::LgTsDataObject()
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;

	// Make sure that we have registered the type
	UINT uType;
	GetClipboardType(&uType);
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
LgTsDataObject::~LgTsDataObject()
{
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  IUnknown, IDataObject, and ITsString
	are supported.  Note that the ITsString interface refers to an internal variable, and cannot
	be used to get back to the IDataObject or the original IUnknown.

	This is a standard COM IUnknown method.

	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.

	@return SOK, STG_E_INVALIDPOINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsDataObject::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgTsDataObject *>(this));
	else if (riid == IID_IDataObject)
		*ppv = static_cast<IDataObject *>(this);
	else if (riid == IID_ILgTsDataObject)
		*ppv = static_cast<ILgTsDataObject *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(static_cast<IDataObject *>(this), IID_IDataObject);
//		*ppv = NewObj CSupportErrorInfo(this, IID_ITsString);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Increment the reference count.

	This is a standard COM IUnknown method.

	@return The updated reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) LgTsDataObject::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Decrement the reference count.  Delete the object if the count goes to zero (or below).

	This is a standard COM IUnknown method.

	@return The updated reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) LgTsDataObject::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Initialize the LgTsDataObject with its internal ILgTsStringPlusWss value.

	@param ptss Pointer to the ILgTsStringPlusWss COM object which this LgTsDataObject object
					wraps.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsDataObject::Init(ILgTsStringPlusWss * ptssencs)
{
	BEGIN_COM_METHOD;

	ChkComArgPtr(ptssencs);

	m_qtsswss = ptssencs;

	END_COM_METHOD(g_fact, IID_ILgTsDataObject);
}

/*----------------------------------------------------------------------------------------------
	Static method to obtain the clipboard type registered for TsString data.

	@param pType The clipboard type registered for "CF_TsString".
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsDataObject::GetClipboardType(UINT* pType)
{
	BEGIN_COM_METHOD;

	ChkComArgPtr(pType);
	*pType = 0;

	if (!LgTsDataObject::s_cfTsString)
		LgTsDataObject::s_cfTsString = ::RegisterClipboardFormat(_T("CF_TsString"));
	Assert(LgTsDataObject::s_cfTsString);
	*pType = LgTsDataObject::s_cfTsString;

	END_COM_METHOD(g_fact, IID_ILgTsDataObject);
}



/*----------------------------------------------------------------------------------------------
	Renders the data described in a FORMATETC structure and transfers it through the STGMEDIUM
	structure.

	This is a standard COM IDataObject method.

	@param pfmte Pointer to a FORMATETC structure describing the desired data format.
	@param pmedium Pointer to a STGMEDIUM structure that defines the output medium.

	@return S_OK, DV_E_DVASPECT, DV_E_FORMATETC, DV_E_LINDEX, DV_E_TYMED, E_POINTER,
					E_UNEXPECTED, or E_NOTIMPL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsDataObject::GetData(FORMATETC * pfmte, STGMEDIUM * pmedium)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pfmte);
	ChkComArgPtr(pmedium);
	AssertPtr(m_qtsswss.Ptr());
	if (pfmte->lindex != -1 && pfmte->lindex != 1)
		return DV_E_LINDEX;
	if (pfmte->dwAspect && pfmte->dwAspect != DVASPECT_CONTENT)
		return DV_E_DVASPECT;	// Invalid dwAspect value.

	if (pfmte->cfFormat == LgTsDataObject::s_cfTsString)
	{
		if (pfmte->tymed == TYMED_NULL)
		{
			pmedium->tymed = TYMED_NULL;
			pmedium->pstm = NULL;
			pmedium->pUnkForRelease = NULL;
			return S_OK;
		}
		if (pfmte->tymed & TYMED_ISTORAGE)
		{
			// Since we have no idea where to put the IStorage object, this particular request
			// is handled by GetDataHere instead.
			return E_NOTIMPL;
		}
		else
		{
			return DV_E_TYMED;
		}
	}
	else if (pfmte->cfFormat == CF_TEXT)
	{
		if (pfmte->tymed == TYMED_NULL)
		{
			pmedium->tymed = TYMED_NULL;
			pmedium->pstm = NULL;
			pmedium->pUnkForRelease = NULL;
			return S_OK;
		}
		if (pfmte->tymed & TYMED_HGLOBAL)
		{
			SmartBstr sbstr;
			CheckHr(m_qtsswss->get_Text(&sbstr));
			StrUni stu = sbstr.Chars();
			if (!StrUtil::NormalizeStrUni(stu, UNORM_NFC))
				ThrowInternalError(E_FAIL, "Normalize failure in StringDataObject::GetData.");
			StrAnsi sta(stu);
			pmedium->tymed = TYMED_HGLOBAL;
			pmedium->pUnkForRelease = NULL;
			pmedium->hGlobal = ::GlobalAlloc(GHND, (DWORD)(sta.Length() + 1));
			if (pmedium->hGlobal)
			{
				char * pszDst;
				pszDst = (char *)::GlobalLock(pmedium->hGlobal);
				strcpy_s(pszDst, sta.Length() + 1, sta.Chars());
				::GlobalUnlock(pmedium->hGlobal);
			}
			return S_OK;
		}
		if (!(pfmte->tymed & TYMED_HGLOBAL))
			return DV_E_TYMED;
		else
			return E_UNEXPECTED;
	}
	else if (pfmte->cfFormat == CF_OEMTEXT)
	{
		if (pfmte->tymed == TYMED_NULL)
		{
			pmedium->tymed = TYMED_NULL;
			pmedium->pstm = NULL;
			pmedium->pUnkForRelease = NULL;
			return S_OK;
		}
		if (pfmte->tymed & TYMED_HGLOBAL)
		{
			// REVIEW SteveMc: How does CF_OEMTEXT differ from CF_TEXT?
			SmartBstr sbstr;
			CheckHr(m_qtsswss->get_Text(&sbstr));
			StrUni stu = sbstr.Chars();
			if (!StrUtil::NormalizeStrUni(stu, UNORM_NFC))
				ThrowInternalError(E_FAIL, "Normalize failure in StringDataObject::GetData.");
			StrAnsi sta(stu);
			pmedium->tymed = TYMED_HGLOBAL;
			pmedium->pUnkForRelease = NULL;
			pmedium->hGlobal = ::GlobalAlloc(GHND, (DWORD)(sta.Length() + 1));
			if (pmedium->hGlobal)
			{
				char * pszDst;
				pszDst = (char *)::GlobalLock(pmedium->hGlobal);
				strcpy_s(pszDst, sta.Length() + 1, sta.Chars());
				::GlobalUnlock(pmedium->hGlobal);
			}
			return S_OK;
		}
		if (!(pfmte->tymed & TYMED_HGLOBAL))
			return DV_E_TYMED;
	}
	else if (pfmte->cfFormat == CF_UNICODETEXT)
	{
		if (pfmte->tymed == TYMED_NULL)
		{
			pmedium->tymed = TYMED_NULL;
			pmedium->pstm = NULL;
			pmedium->pUnkForRelease = NULL;
			return S_OK;
		}
		if (pfmte->tymed & TYMED_HGLOBAL)
		{
			SmartBstr sbstr;
			CheckHr(m_qtsswss->get_Text(&sbstr));
			StrUni stu = sbstr.Chars();
			if (!StrUtil::NormalizeStrUni(stu, UNORM_NFC))
				ThrowInternalError(E_FAIL, "Normalize failure in StringDataObject::GetData.");
			pmedium->tymed = TYMED_HGLOBAL;
			pmedium->pUnkForRelease = NULL;
			int cb = isizeof(wchar) * (stu.Length() + 1);
			pmedium->hGlobal = ::GlobalAlloc(GHND, (DWORD)cb);
			if (pmedium->hGlobal)
			{
				wchar * pszDst;
				pszDst = (wchar *)::GlobalLock(pmedium->hGlobal);
				memcpy(pszDst, stu.Chars(), cb);
				::GlobalUnlock(pmedium->hGlobal);
			}
			return S_OK;
		}
		if (!(pfmte->tymed & TYMED_HGLOBAL))
			return DV_E_TYMED;
		else
			return E_UNEXPECTED;
	}
	else
	{
		return DV_E_FORMATETC;
	}
	END_COM_METHOD(g_fact, IID_IDataObject);
}

/*----------------------------------------------------------------------------------------------
	Renders the data described in a FORMATETC structure and transfers it through the STGMEDIUM
	structure allocated by the caller.

	This is a standard COM IDataObject method.

	@param pfmte Pointer to a FORMATETC structure describing the desired data format.
	@param pmedium Pointer to a STGMEDIUM structure that defines the output medium.

	@return S_OK, DV_E_DVASPECT, DV_E_FORMATETC, DV_E_LINDEX, DV_E_TYMED, E_FAIL, E_INVALIDARG,
					E_NOTIMPL, E_UNEXPECTED, or possibly some other COM error code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsDataObject::GetDataHere(FORMATETC * pfmte, STGMEDIUM * pmedium)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pfmte);
	ChkComArgPtr(pmedium);
	AssertPtr(m_qtsswss.Ptr());
	if (pfmte->lindex != -1 && pfmte->lindex != 1)
		return DV_E_LINDEX;
	if (pfmte->dwAspect && pfmte->dwAspect != DVASPECT_CONTENT)
		return DV_E_DVASPECT;	// Invalid dwAspect value.

	if (pfmte->cfFormat == LgTsDataObject::s_cfTsString)
	{
		if (pfmte->tymed == TYMED_NULL)
		{
			pmedium->tymed = TYMED_NULL;
			pmedium->pstm = NULL;
			pmedium->pUnkForRelease = NULL;
			return S_OK;
		}
		if (pfmte->tymed & TYMED_ISTORAGE)
		{
			if (pmedium->tymed == TYMED_ISTORAGE && pmedium->pstg)
			{
				CheckHr(m_qtsswss->Serialize(pmedium->pstg));
				CheckHr(pmedium->pstg->Commit(STGC_DEFAULT));
				return S_OK;
			}
			else
			{
				return E_NOTIMPL;
			}
		}
		else
		{
			return DV_E_TYMED;
		}
	}
	else if (pfmte->cfFormat == CF_TEXT || pfmte->cfFormat == CF_OEMTEXT ||
		pfmte->cfFormat == CF_UNICODETEXT)
	{
		// These are better handled by GetData, since the amount of global memory that needs to
		// be allocated is known only internally.
		return E_NOTIMPL;
	}
	else
	{
		return DV_E_FORMATETC;
	}
	END_COM_METHOD(g_fact, IID_IDataObject);
}

/*----------------------------------------------------------------------------------------------
	Determines whether the data object is capable of rendering the data described in the
	FORMATETC structure.

	This is a standard COM IDataObject method.

	@param pfmte Pointer to a FORMATETC structure describing the desired data format.

	@return  S_OK, DV_E_DVASPECT, DV_E_FORMATETC, DV_E_LINDEX, DV_E_TYMED, or E_INVALIDARG.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsDataObject::QueryGetData(FORMATETC * pfmte)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pfmte);
	if (pfmte->cfFormat != CF_TEXT &&
		pfmte->cfFormat != CF_OEMTEXT &&
		pfmte->cfFormat != CF_UNICODETEXT &&
		pfmte->cfFormat != LgTsDataObject::s_cfTsString)
	{
		return DV_E_FORMATETC;
	}
	if (pfmte->lindex != -1 && pfmte->lindex != 1)
	{
		return DV_E_LINDEX;		// Invalid value for lindex; currently, only -1 is supported.
	}
	if (pfmte->dwAspect && pfmte->dwAspect != DVASPECT_CONTENT)
	{
		return DV_E_DVASPECT;	// Invalid dwAspect value.
	}
	if (pfmte->cfFormat == LgTsDataObject::s_cfTsString)
	{
		if (pfmte->tymed != TYMED_ISTORAGE)
			return DV_E_TYMED;	// Invalid tymed value.
	}
	else if (pfmte->tymed != TYMED_NULL && !(pfmte->tymed & TYMED_HGLOBAL))
	{
		return DV_E_TYMED;		// Invalid tymed value.
	}
	END_COM_METHOD(g_fact, IID_IDataObject);
}

/*----------------------------------------------------------------------------------------------
	Provides a potentially different but logically equivalent FORMATETC structure.  This
	implementation merely copies the input format to the output format.

	This is a standard COM IDataObject method.

	According to Inside OLE, chapter 10, this method should return DATA_S_SAMEFORMATETC if,
	as in our case, it uses the simplest implementation (for where the implementor does not
	care about the output medium) and copies the input to the output in this method.

	@param pfmteIn Pointer to a FORMATETC structure describing a data format.
	@param pfmteOut Pointer to a FORMATETC structure describing the canonical form of that data
					format.

	@return S_OK, E_INVALIDARG, or E_POINTER.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsDataObject::GetCanonicalFormatEtc(FORMATETC * pfmteIn, FORMATETC * pfmteOut)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pfmteIn);
	ChkComArgPtr(pfmteOut);

	memcpy(pfmteOut, pfmteIn, isizeof(FORMATETC));

	return DATA_S_SAMEFORMATETC;

	END_COM_METHOD(g_fact, IID_IDataObject);
}

/*----------------------------------------------------------------------------------------------
	Provides the source data object with data described by a FORMATETC structure and an
	STGMEDIUM structure.

	This is a standard COM IDataObject method.    It is not supported by this implementation.

	@param pfmte Not used by this implementation.
	@param pmedium Not used by this implementation.
	@param fRelease Not used by this implementation.

	@return E_NOTIMPL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsDataObject::SetData(FORMATETC * pfmte, STGMEDIUM * pmedium, BOOL fRelease)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Creates and returns a pointer to an object to enumerate the FORMATETC supported by the data
	object.  Only getting data (DATADIR_GET) is supported.

	This is a standard COM IDataObject method.

	@param dwDirection Specify whether setting or getting data (DATADIR_SET or DATADIR_GET).
	@param ppenum Address of a pointer to an IEnumFORMATETC COM interface for returning the
					desired enumeration object.

	@return S_OK, E_POINTER, E_INVALIDARG, E_FAIL, E_NOTIMPL, or possibly another COM error
					code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsDataObject::EnumFormatEtc(DWORD dwDirection, IEnumFORMATETC ** ppenum)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppenum);

	if (dwDirection == DATADIR_SET)
	{
		return E_NOTIMPL;
	}
	else if (dwDirection == DATADIR_GET)
	{
		LgTsEnumFORMATETC::Create(ppenum);
	}
	else
	{
		return E_INVALIDARG;
	}
	END_COM_METHOD(g_fact, IID_IDataObject);
}

/*----------------------------------------------------------------------------------------------
	Creates a connection between a data object and an advise sink so the advise sink can
	receive notifications of changes in the data object.

	This is a standard COM IDataObject method.  It is not supported by this implementation.

	@param pfmte Not used by this implementation.
	@param advf Not used by this implementation.
	@param pAdvSink Not used by this implementation.
	@param pdwConnection Not used by this implementation.

	@return OLE_E_ADVISENOTSUPPORTED.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsDataObject::DAdvise(FORMATETC * pfmte, DWORD advf, IAdviseSink * pAdvSink,
	DWORD * pdwConnection)
{
	// LgTsDataObject supports only static data transfer!
	return OLE_E_ADVISENOTSUPPORTED;
}

/*----------------------------------------------------------------------------------------------
	Destroys a notification previously set up with the DAdvise method.

	This is a standard COM IDataObject method.  It is not supported by this implementation.

	@param dwConnection Not used by this implementation.

	@return OLE_E_ADVISENOTSUPPORTED.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsDataObject::DUnadvise(DWORD dwConnection)
{
	// LgTsDataObject supports only static data transfer!
	return OLE_E_ADVISENOTSUPPORTED;
}

/*----------------------------------------------------------------------------------------------
	Creates and returns a pointer to an object to enumerate the current advisory connections.

	This is a standard COM IDataObject method.  It is not supported by this implementation.

	@param ppenumAdvise Not used by this implementation.

	@return OLE_E_ADVISENOTSUPPORTED.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsDataObject::EnumDAdvise(IEnumSTATDATA ** ppenumAdvise)
{
	// LgTsDataObject supports only static data transfer!
	return OLE_E_ADVISENOTSUPPORTED;
}


//:>********************************************************************************************
//:>	LgTsEnumFORMATETC Methods
//:>********************************************************************************************

FORMATETC LgTsEnumFORMATETC::g_rgfmte[kcfmteLim] =
{
	{	0 /* "CF_TsString" */, NULL, DVASPECT_CONTENT, -1, TYMED_ISTORAGE	},
	{	CF_UNICODETEXT, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL	},
	{	CF_OEMTEXT, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL	},
	{	CF_TEXT, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL	}
};

static DummyFactory g_factEnum(_T("SIL.FieldWorks.LgTsEnumFORMATETC"));

/*----------------------------------------------------------------------------------------------
	Create a LgTsEnumFORMATETC object.

	@param ppenum Address of a pointer for returning the newly created LgTsEnumFORMATETC COM
					object.
----------------------------------------------------------------------------------------------*/
void LgTsEnumFORMATETC::Create(IEnumFORMATETC ** ppenum)
{
	AssertPtr(ppenum);

	ComSmartPtr<LgTsEnumFORMATETC> qtsenum;
	qtsenum.Attach(NewObj LgTsEnumFORMATETC);
	*ppenum = qtsenum.Detach();
}

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
LgTsEnumFORMATETC::LgTsEnumFORMATETC()
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
	m_ifmte = 0;
	// Finish initializing the static array if necessary.
	if (g_rgfmte[0].cfFormat == 0)
	{
		g_rgfmte[0].cfFormat = static_cast<unsigned short>(
			::RegisterClipboardFormat(_T("CF_TsString")));
	}
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
LgTsEnumFORMATETC::~LgTsEnumFORMATETC()
{
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown and IEnumFORMATETC are
	supported.

	This is a standard COM IUnknown method.

	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.

	@return SOK, STG_E_INVALIDPOINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsEnumFORMATETC::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IEnumFORMATETC)
		*ppv = static_cast<IEnumFORMATETC *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IEnumFORMATETC);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Increment the reference count.

	This is a standard COM IUnknown method.

	@return The updated reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) LgTsEnumFORMATETC::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}

/*----------------------------------------------------------------------------------------------
	Decrement the reference count.  Delete the object if the count goes to zero (or below).

	This is a standard COM IUnknown method.

	@return The updated reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) LgTsEnumFORMATETC::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Retrieves a specified number of items in the enumeration sequence.

	Retrieves the next celt items in the enumeration sequence. If there are fewer than the
	requested number of elements left in the sequence, it retrieves the remaining elements.
	The number of elements actually retrieved is returned through pceltFetched (unless the
	caller passed in NULL for that parameter).

	This is a standard COM IEnumFORMATETC method.

	@param celt Desired number of elements to retrieve.
	@param rgelt Pointer to an array for returning the retrieved elements.
	@param pceltFetched Pointer to a count of the number of elements actually retrieved, or
					NULL.

	@return S_OK (if celt items retrieved), S_FALSE (if fewer than celt items retrieved), or
					E_POINTER.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsEnumFORMATETC::Next(ULONG celt, FORMATETC * rgelt, ULONG * pceltFetched)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(rgelt, celt);
	ChkComArgPtrN(pceltFetched);

	if (celt == 0)
	{
		if (pceltFetched)
			*pceltFetched = 0;
		return S_OK;
	}
	int cfmteAvail = kcfmteLim - m_ifmte;
	if (cfmteAvail <= 0)
	{
		if (pceltFetched)
			*pceltFetched = 0;
		return S_FALSE;
	}
	int cfmte;
	if (celt > static_cast<ULONG>(cfmteAvail))
		cfmte = cfmteAvail;
	else
		cfmte = celt;
	if (pceltFetched)
		*pceltFetched = cfmte;
	memcpy(rgelt, &g_rgfmte[m_ifmte], cfmte * isizeof(FORMATETC));
	m_ifmte += cfmte;
	if (m_ifmte > kcfmteLim)
		m_ifmte = kcfmteLim;
	return celt == static_cast<ULONG>(cfmte) ? S_OK : S_FALSE;

	END_COM_METHOD(g_factEnum, IID_IEnumFORMATETC);
}

/*----------------------------------------------------------------------------------------------
	Skip over a specified number of items in the enumeration sequence.

	This is a standard COM IEnumFORMATETC method.

	@param celt Number of elements to skip.

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsEnumFORMATETC::Skip(ULONG celt)
{
	BEGIN_COM_METHOD

	if (m_ifmte < kcfmteLim)
	{
		m_ifmte += celt;
		if (m_ifmte > kcfmteLim)
			m_ifmte = kcfmteLim;
	}
	return S_OK;

	END_COM_METHOD(g_factEnum, IID_IEnumFORMATETC);
}

/*----------------------------------------------------------------------------------------------
	Reset the enumeration sequence to the beginning.

	This is a standard COM IEnumFORMATETC method.

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsEnumFORMATETC::Reset(void)
{
	BEGIN_COM_METHOD

	m_ifmte = 0;
	return S_OK;

	END_COM_METHOD(g_factEnum, IID_IEnumFORMATETC);
}

/*----------------------------------------------------------------------------------------------
	Creates another enumerator that contains the same enumeration state as the current one.

	This is a standard COM IEnumFORMATETC method.

	@param ppenum Address of a pointer for returning the newly created copy of the
					LgTsEnumFORMATETC COM object.

	@return S_OK, E_POINTER, E_INVALIDARG, or possibly another COM error code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsEnumFORMATETC::Clone(IEnumFORMATETC ** ppenum)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppenum);

	LgTsEnumFORMATETC::Create(ppenum);
	LgTsEnumFORMATETC * ptsenum = dynamic_cast<LgTsEnumFORMATETC *>(*ppenum);
	if (!ptsenum)
		ThrowHr(WarnHr(E_INVALIDARG));
	ptsenum->m_ifmte = m_ifmte;

	END_COM_METHOD(g_factEnum, IID_IEnumFORMATETC);
}

// Local Variables:
// compile-command:"cmd.exe /E:4096 /C ..\\..\\Bin\\mkaflib.bat"
// End:
