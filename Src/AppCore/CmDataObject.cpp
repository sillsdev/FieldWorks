/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: CmDataObject.cpp
Responsibility: Ken Zook
Last reviewed:

Description:
	This file contains class definitions CmDataObject, used in clipboard and drag and drop.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


//:>********************************************************************************************
//:> CmDataObject methods.
//:>********************************************************************************************

unsigned int CmDataObject::cfCmObject = 0;


/*----------------------------------------------------------------------------------------------
	This static method creates a new CmDataObject object, storing the given CmObject pointer
	internally in the newly created object.
	@param pszSvrName Name of the source database server.
	@param pszDbName Name of the source database.
	@param hvo Object id for the source object.
	@param clid Class id of the source object.
	@param ptss TsString representing the source object.
	@param pid Process id of the source process.
	@param ppdobj Address of a pointer for returning the newly created CmDataObject COM object.
----------------------------------------------------------------------------------------------*/
void CmDataObject::Create(const OLECHAR * pszSvrName, const OLECHAR * pszDbName, HVO hvo,
	int clid, ITsString * ptss, int pid, IDataObject ** ppdobj)
{
	Assert(hvo);
	AssertPtr(ptss);
	AssertPtr(ppdobj);
	Assert(!*ppdobj);
	AssertPsz(pszSvrName);
	AssertPsz(pszDbName);
	Assert(pid);

	ComSmartPtr<CmDataObject> qcdo;
	qcdo.Attach(NewObj CmDataObject);
	qcdo->Init(pszSvrName, pszDbName, hvo, clid, ptss, pid);
	*ppdobj = qcdo.Detach();
	CmDataObject::GetClipboardType();
}


/*----------------------------------------------------------------------------------------------
	Static method to obtain the clipboard type registered for CmObject data.
	@return The clipboard type registered for "CF_CmObject".
----------------------------------------------------------------------------------------------*/
unsigned int CmDataObject::GetClipboardType()
{
	if (!CmDataObject::cfCmObject)
		CmDataObject::cfCmObject = ::RegisterClipboardFormat(_T("CF_CmObject"));
	Assert(CmDataObject::cfCmObject);
	return CmDataObject::cfCmObject;
}


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CmDataObject::CmDataObject()
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CmDataObject::~CmDataObject()
{
	ModuleEntry::ModuleRelease();
}

static DummyFactory g_fact1(_T("SIL.AppCore.CmDataObject"));

/*----------------------------------------------------------------------------------------------
	Initialize the CmDataObject.
	@param pszSvrName Name of the source database server.
	@param pszDbName Name of the source database.
	@param hvo Object id for the source object.
	@param clid Class id of the source object.
	@param ptss TsString representing the source object.
	@param pid Process id of the source process.
----------------------------------------------------------------------------------------------*/
void CmDataObject::Init(const OLECHAR * pszSvrName, const OLECHAR * pszDbName, HVO hvo, int clid,
	ITsString * ptss, int pid)
{
	Assert(hvo);
	AssertPtr(ptss);
	m_hvo = hvo;
	m_clid = clid;
	m_qtss = ptss;
	m_stuSvrName = pszSvrName;
	m_stuDbName = pszDbName;
	m_pid = pid;
}


/*----------------------------------------------------------------------------------------------
	Returns a pointer to a specified interface on an object to which a client currently holds
	an interface pointer.
	@param riid Identifier of the requested interface.
	@param ppv Address of output variable that receives the interface pointer requested in riid.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmDataObject::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IDataObject)
		*ppv = static_cast<IDataObject *>(this);
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
STDMETHODIMP_(ULONG) CmDataObject::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Decrement the reference count.  Delete the object if the count goes to zero (or below).
	This is a standard COM IUnknown method.
	@return The updated reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) CmDataObject::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Called by a data consumer to obtain data from a source data object. The GetData method
	renders the data described in the specified FORMATETC structure and transfers it through
	the specified STGMEDIUM structure. The caller then assumes responsibility for releasing
	the STGMEDIUM structure.
	This is a standard COM IDataObject method.
	@param pformatetcIn Pointer to the FORMATETC structure.
	@param pmedium Pointer to the STGMEDIUM structure.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmDataObject::GetData(FORMATETC * pfmte, STGMEDIUM * pmedium)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pfmte);
	ChkComArgPtr(pmedium);

	//StrAnsi sta;
	//sta.Format("GetData\n");
	//OutputDebugString(sta.Chars());
	//AssertPtr(m_qtss.Ptr()); Disable temporarily until we start using the string.
	if (pfmte->lindex != -1 && pfmte->lindex != 1)
	{
		return DV_E_LINDEX;
	}
	if (pfmte->dwAspect && pfmte->dwAspect != DVASPECT_CONTENT)
	{
		return DV_E_DVASPECT;	// Invalid dwAspect value.
	}
	if (pfmte->cfFormat == CmDataObject::cfCmObject)
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
			try
			{
				// The format in memory is 16 bit words:
				// m_hvo (LSW).
				// m_hvo (MSW).
				// m_clid (LSW).
				// m_clid (MSW).
				// m_pid (LSW);
				// m_pid (MSW);
				// Null-terminated server name string.
				// Null-terminated db name string.
				// Null-terminated TsString text.
				// 16 bit count of TsString format bytes.
				// Array of TsString format bytes.
				pmedium->tymed = TYMED_HGLOBAL;
				pmedium->pUnkForRelease = NULL;
				int cbSvr = isizeof(wchar) * (m_stuSvrName.Length() + 1);
				int cbDb = isizeof(wchar) * (m_stuDbName.Length() + 1);
				SmartBstr sbstr;
				const int kcbFmtBufMax = 500; // Should be adequate.
				int cbFmt;
				byte rgbFmt[kcbFmtBufMax];
				// Convert the string formatting into a byte array.
				CheckHr(m_qtss->SerializeFmtRgb(rgbFmt, kcbFmtBufMax, &cbFmt));
				m_qtss->get_Text(&sbstr);
				int cbTxt = isizeof(wchar) * (sbstr.Length() + 1);
				pmedium->hGlobal = ::GlobalAlloc(GHND, (DWORD)(isizeof(int) * 3 + cbSvr +
					cbDb + cbTxt + 2 + cbFmt));
				if (pmedium->hGlobal)
				{
					wchar * prgch;
					prgch = (wchar *)::GlobalLock(pmedium->hGlobal);
					*prgch++ = (wchar)m_hvo;
					*prgch++ = (wchar)(m_hvo >> 16);
					*prgch++ = (wchar)m_clid;
					*prgch++ = (wchar)(m_clid >> 16);
					*prgch++ = (wchar)m_pid;
					*prgch++ = (wchar)(m_pid >> 16);
					memcpy(prgch, m_stuSvrName.Chars(), cbSvr);
					prgch += m_stuSvrName.Length() + 1;
					memcpy(prgch, m_stuDbName.Chars(), cbDb);
					prgch += m_stuDbName.Length() + 1;
					memcpy(prgch, sbstr.Chars(), cbTxt);
					prgch += sbstr.Length() + 1;
					*prgch++ = (wchar)cbFmt;
					memcpy(prgch, rgbFmt, cbFmt);
					::GlobalUnlock(pmedium->hGlobal);
				}
				return S_OK;
			}
			catch (...)
			{
				return E_UNEXPECTED;
			}
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
			try
			{
				SmartBstr sbstr;
				ITsStringPtr qtssNFC;
				CheckHr(m_qtss->get_NormalizedForm(knmNFC, &qtssNFC));
				CheckHr(qtssNFC->get_Text(&sbstr));
				StrAnsi sta(sbstr.Chars(), BstrLen(sbstr));
				pmedium->tymed = TYMED_HGLOBAL;
				pmedium->pUnkForRelease = NULL;
				DWORD nLen = sta.Length() + 1;
				pmedium->hGlobal = ::GlobalAlloc(GHND, nLen);
				if (pmedium->hGlobal)
				{
					char * pszDst;
					pszDst = (char *)::GlobalLock(pmedium->hGlobal);
					strcpy_s(pszDst, nLen, sta.Chars());
					::GlobalUnlock(pmedium->hGlobal);
				}
				return S_OK;
			}
			catch (...)
			{
				return E_UNEXPECTED;
			}
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
			try
			{
				SmartBstr sbstr;
				ITsStringPtr qtssNFC;
				CheckHr(m_qtss->get_NormalizedForm(knmNFC, &qtssNFC));
				CheckHr(qtssNFC->get_Text(&sbstr));
				StrAnsi sta(sbstr.Chars(), BstrLen(sbstr));
				pmedium->tymed = TYMED_HGLOBAL;
				pmedium->pUnkForRelease = NULL;
				DWORD nLen = sta.Length() + 1;
				pmedium->hGlobal = ::GlobalAlloc(GHND, nLen);
				if (pmedium->hGlobal)
				{
					char * pszDst;
					pszDst = (char *)::GlobalLock(pmedium->hGlobal);
					strcpy_s(pszDst, nLen, sta.Chars());
					::GlobalUnlock(pmedium->hGlobal);
				}
				return S_OK;
			}
			catch (...)
			{
				return E_UNEXPECTED;
			}
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
			try
			{
				SmartBstr sbstr;
				ITsStringPtr qtssNFC;
				CheckHr(m_qtss->get_NormalizedForm(knmNFC, &qtssNFC));
				CheckHr(qtssNFC->get_Text(&sbstr));
				pmedium->tymed = TYMED_HGLOBAL;
				pmedium->pUnkForRelease = NULL;
				int cb = isizeof(wchar) * (sbstr.Length() + 1);
				pmedium->hGlobal = ::GlobalAlloc(GHND, (DWORD)cb);
				if (pmedium->hGlobal)
				{
					wchar * pszDst;
					pszDst = (wchar *)::GlobalLock(pmedium->hGlobal);
					memcpy(pszDst, sbstr.Chars(), cb);
					::GlobalUnlock(pmedium->hGlobal);
				}
				return S_OK;
			}
			catch (...)
			{
				return E_UNEXPECTED;
			}
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
	return E_UNEXPECTED;

	END_COM_METHOD(g_fact1, IID_IDataObject)
}


/*----------------------------------------------------------------------------------------------
	Called by a data consumer to obtain data from a source data object. This method differs
	from the GetData method in that the caller must allocate and free the specified storage
	medium.
	This is a standard COM IDataObject method.
	@param pformatetc Pointer to the FORMATETC structure.
	@param pmedium Pointer to the STGMEDIUM structure.
	@return S_OK Operation succeeded. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmDataObject::GetDataHere(FORMATETC * pfmte, STGMEDIUM * pmedium)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pfmte);
	ChkComArgPtr(pmedium);

	//StrAnsi sta;
	//sta.Format("GetDataHere\n");
	//OutputDebugString(sta.Chars());
	AssertPtr(m_qtss.Ptr());
	if (pfmte->lindex != -1 && pfmte->lindex != 1)
	{
		return DV_E_LINDEX;
	}
	if (pfmte->dwAspect && pfmte->dwAspect != DVASPECT_CONTENT)
	{
		return DV_E_DVASPECT;	// Invalid dwAspect value.
	}
	if (pfmte->cfFormat == CmDataObject::cfCmObject)
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
				try
				{
					// We have an IStorage object to work with.
					// Create and write the "Text" and "Fmt" streams.
					IStreamPtr qstrmText;
					CheckHr(pmedium->pstg->CreateStream(L"Text",
						STGM_READWRITE | STGM_SHARE_EXCLUSIVE, 0, 0, &qstrmText));
					SmartBstr sbstr;
					CheckHr(m_qtss->get_Text(&sbstr));
					ULONG cb = BstrSize(sbstr);
					ULONG cbWritten;
					CheckHr(qstrmText->Write(sbstr.Chars(), cb, &cbWritten));
					if (cb != cbWritten)
					{
						ThrowHr(WarnHr(E_UNEXPECTED));
					}
					IStreamPtr qstrmFmt;
					CheckHr(pmedium->pstg->CreateStream(L"Fmt",
						STGM_READWRITE | STGM_SHARE_EXCLUSIVE, 0, 0, &qstrmFmt));
					CheckHr(m_qtss->SerializeFmt(qstrmFmt));
					CheckHr(pmedium->pstg->Commit(STGC_DEFAULT));
				}
				catch (Throwable & thr)
				{
					ReturnHr(thr.Error());
				}
				catch (...)
				{
					ReturnHr(E_FAIL);
				}
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
	return E_UNEXPECTED;

	END_COM_METHOD(g_fact1, IID_IDataObject)
}


/*----------------------------------------------------------------------------------------------
	Determines whether the data object is capable of rendering the data described in the
	FORMATETC structure. Objects attempting a paste or drop operation can call this method
	before calling IDataObject::GetData to get an indication of whether the operation may be
	successful.
	This is a standard COM IDataObject method.
	@param pformatetc Pointer to the FORMATETC structure
	@return S_OK Operation succeeded. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmDataObject::QueryGetData(FORMATETC * pfmte)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pfmte);

	//StrAnsi sta;
	//sta.Format("QueryGetData\n");
	//OutputDebugString(sta.Chars());
	if (pfmte->cfFormat != CF_TEXT &&
		pfmte->cfFormat != CF_OEMTEXT &&
		pfmte->cfFormat != CF_UNICODETEXT &&
		pfmte->cfFormat != CmDataObject::cfCmObject)
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
	if (pfmte->cfFormat == CmDataObject::cfCmObject)
	{
		if (pfmte->tymed != TYMED_HGLOBAL)
			return DV_E_TYMED;	// Invalid tymed value.
	}
	else if (pfmte->tymed != TYMED_NULL && !(pfmte->tymed & TYMED_HGLOBAL))
	{
		return DV_E_TYMED;		// Invalid tymed value.
	}
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IDataObject)
}


/*----------------------------------------------------------------------------------------------
	Provides a standard FORMATETC structure that is logically equivalent to one that is more
	complex. You use this method to determine whether two different FORMATETC structures would
	return the same data, removing the need for duplicate rendering.
	This is a standard COM IDataObject method.

	According to Inside OLE, chapter 10, this method should return DATA_S_SAMEFORMATETC if,
	as in our case, it uses the simplest implementation (for where the implementor does not
	care about the output medium) and copies the input to the output in this method.

	@param pformatectIn Pointer to the FORMATETC structure.
	@param pformatetcOut Pointer to the canonical equivalent FORMATETC structure.
	@return S_OK Operation succeeded. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmDataObject::GetCanonicalFormatEtc(FORMATETC * pfmteIn,
	FORMATETC * pfmteOut)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pfmteIn);
	ChkComArgPtr(pfmteOut);

	//StrAnsi sta;
	//sta.Format("GetCanonicalFormatEtc\n");
	//OutputDebugString(sta.Chars());
	memcpy(pfmteOut, pfmteIn, isizeof(FORMATETC));
	return DATA_S_SAMEFORMATETC;

	END_COM_METHOD(g_fact1, IID_IDataObject)
}


/*----------------------------------------------------------------------------------------------
	Called by an object containing a data source to transfer data to the object that implements
	this method.
	This is a standard COM IDataObject method.
	@param pformatetc Pointer to the FORMATETC structure.
	@param pmedium Pointer to STGMEDIUM structure.
	@param fRelease Indicates which object owns the storage medium after the call is completed.
	@return S_OK Operation succeeded. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmDataObject::SetData(FORMATETC * pformatetc, STGMEDIUM * pmedium, BOOL fRelease)
{
	BEGIN_COM_METHOD
	ReturnHr(E_NOTIMPL);
	END_COM_METHOD(g_fact1, IID_IDataObject)
}


/*----------------------------------------------------------------------------------------------
	Creates an object for enumerating the FORMATETC structures for a data object. These
	structures are used in calls to IDataObject::GetData or IDataObject::SetData.
	This is a standard COM IDataObject method.
	@param dwDirection Specifies a value from the enumeration DATADIR.
	@param ppenumFormatEtc Address of output variable that receives the IEnumFORMATETC
		interface pointer.
	@return S_OK Operation succeeded. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmDataObject::EnumFormatEtc(DWORD dwDirection, IEnumFORMATETC ** ppenum)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppenum);

	//StrAnsi sta;
	//sta.Format("EnumFormatEtc: %x\n", dwDirection);
	//OutputDebugString(sta.Chars());
	if (dwDirection == DATADIR_SET)
	{
		return E_NOTIMPL;
	}
	else if (dwDirection == DATADIR_GET)
	{
		try
		{
			CmEnumFORMATETC::Create(ppenum);
		}
		catch (Throwable & thr)
		{
			ReturnHr(thr.Error());
		}
		catch (...)
		{
			ReturnHr(E_FAIL);
		}
		return S_OK;
	}
	else
	{
		return E_INVALIDARG;
	}

	END_COM_METHOD(g_fact1, IID_IDataObject)
}


/*----------------------------------------------------------------------------------------------
	Called by an object supporting an advise sink to create a connection between a data object
	and the advise sink. This enables the advise sink to be notified of changes in the data
	of the object.
	This is a standard COM IDataObject method.
	@param pformatetc Pointer to data of interest to the advise sink.
	@param advf Flags that specify how the notification takes place.
	@param pAdvSink Pointer to the advise sink.
	@param pdwConnection Pointer to a token that identifies this connection.
	@return S_OK Operation succeeded. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmDataObject::DAdvise(FORMATETC * pformatetc, DWORD advf, IAdviseSink * pAdvSink,
	DWORD * pdwConnection)
{
	// CmDataObject supports only static data transfer!
	return OLE_E_ADVISENOTSUPPORTED;
}


/*----------------------------------------------------------------------------------------------
	Destroys a notification connection that had been previously set up.
	This is a standard COM IDataObject method.
	@param dwConnection Connection to remove.
	@return S_OK Operation succeeded. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmDataObject::DUnadvise(DWORD dwConnection)
{
	// CmDataObject supports only static data transfer!
	return OLE_E_ADVISENOTSUPPORTED;
}


/*----------------------------------------------------------------------------------------------
	Creates an object that can be used to enumerate the current advisory connections.
	This is a standard COM IDataObject method.
	@param ppenumAdvise Address of output variable that receives the IEnumSTATDATA interface
		pointer.
	@return S_OK Operation succeeded. OLE_E_ADVISENOTSUPPORTED Advisory notifications are not
		supported by this object. Other standard errors.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmDataObject::EnumDAdvise(IEnumSTATDATA ** ppenumAdvise)
{
	// CmDataObject supports only static data transfer!
	return OLE_E_ADVISENOTSUPPORTED;
}


//:>********************************************************************************************
//:>	CmEnumFORMATETC Methods
//:>********************************************************************************************

// These are the formats we support.
FORMATETC CmEnumFORMATETC::g_rgfmte[kcfmteLim] =
{
	{0 /* "CF_CmObject" */, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL},
	{CF_UNICODETEXT, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL},
	{CF_OEMTEXT, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL},
	{CF_TEXT, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL}
};


/*----------------------------------------------------------------------------------------------
	Create a CmEnumFORMATETC object.
	@param ppenum Address of a pointer for returning the newly created CmEnumFORMATETC COM
					object.
----------------------------------------------------------------------------------------------*/
void CmEnumFORMATETC::Create(IEnumFORMATETC ** ppenum)
{
	AssertPtr(ppenum);

	ComSmartPtr<CmEnumFORMATETC> qCmEnum;
	qCmEnum.Attach(NewObj CmEnumFORMATETC);
	*ppenum = qCmEnum.Detach();
}


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CmEnumFORMATETC::CmEnumFORMATETC()
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
	m_ifmte = 0;
	// Finish initializing the static array if necessary.
	if (g_rgfmte[0].cfFormat == 0)
	{
		g_rgfmte[0].cfFormat = static_cast<unsigned short>(
			::RegisterClipboardFormat(_T("CF_CmObject")));
	}
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CmEnumFORMATETC::~CmEnumFORMATETC()
{
	ModuleEntry::ModuleRelease();
}

static DummyFactory g_fact2(_T("SIL.AppCore.CmEnumFORMATETC"));

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown and IEnumFORMATETC are
	supported.
	This is a standard COM IUnknown method.
	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.
	@return SOK, STG_E_INVALIDPOINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmEnumFORMATETC::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IEnumFORMATETC)
		*ppv = static_cast<IEnumFORMATETC *>(this);
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
STDMETHODIMP_(ULONG) CmEnumFORMATETC::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Decrement the reference count.  Delete the object if the count goes to zero (or below).
	This is a standard COM IUnknown method.
	@return The updated reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) CmEnumFORMATETC::Release(void)
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
STDMETHODIMP CmEnumFORMATETC::Next(ULONG celt, FORMATETC * rgelt, ULONG * pceltFetched)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(rgelt);
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

	END_COM_METHOD(g_fact2, IID_IEnumFORMATETC)
}


/*----------------------------------------------------------------------------------------------
	Skip over a specified number of items in the enumeration sequence.
	This is a standard COM IEnumFORMATETC method.
	@param celt Number of elements to skip.
	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmEnumFORMATETC::Skip(ULONG celt)
{
	BEGIN_COM_METHOD

	if (m_ifmte < kcfmteLim)
	{
		m_ifmte += celt;
		if (m_ifmte > kcfmteLim)
			m_ifmte = kcfmteLim;
	}
	return S_OK;

	END_COM_METHOD(g_fact2, IID_IEnumFORMATETC)
}


/*----------------------------------------------------------------------------------------------
	Reset the enumeration sequence to the beginning.
	This is a standard COM IEnumFORMATETC method.
	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmEnumFORMATETC::Reset(void)
{
	BEGIN_COM_METHOD

	m_ifmte = 0;
	return S_OK;

	END_COM_METHOD(g_fact2, IID_IEnumFORMATETC)
}


/*----------------------------------------------------------------------------------------------
	Creates another enumerator that contains the same enumeration state as the current one.
	This is a standard COM IEnumFORMATETC method.
	@param ppenum Address of a pointer for returning the newly created copy of the
		CmEnumFORMATETC COM object.
	@return S_OK, E_UNEXPECTED, E_FAIL, or possibly another COM error code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CmEnumFORMATETC::Clone(IEnumFORMATETC ** ppenum)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppenum);

	CmEnumFORMATETC::Create(ppenum);
	CmEnumFORMATETC * pCmEnum = dynamic_cast<CmEnumFORMATETC *>(*ppenum);
	if (!pCmEnum)
		ThrowHr(WarnHr(E_UNEXPECTED));
	pCmEnum->m_ifmte = m_ifmte;

	return S_OK;

	END_COM_METHOD(g_fact2, IID_IEnumFORMATETC)
}
