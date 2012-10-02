/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TsMultiStr.cpp
Responsibility: Jeff Gayle.
Last reviewed: Not yet.

-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"

#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Forward Declarations
***********************************************************************************************/
class TsMultiString;

/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/

/***********************************************************************************************
	Methods
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
TsMultiString::TsMultiString(void)
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;

	// ActivateEmptyString makes sure there is a reference count on the global empty string.
	TextServGlobals::ActivateEmptyString(true);
}


/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
TsMultiString::~TsMultiString(void)
{
	ModuleEntry::ModuleRelease();

	// Passing false to ActivateEmptyString make sure the reference count is decremented on the
	// global empty string.
	TextServGlobals::ActivateEmptyString(false);
}


/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsMultiString::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_ITsMultiString)
		*ppv = static_cast<ITsMultiString *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ITsMultiString);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	AddRef.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) TsMultiString::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Release.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) TsMultiString::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;

	m_cref = 1;
	delete this;
	return 0;
}



// The class factory for TsIncStrBldr.
static GenericFactory g_factMultiString(
	_T("FieldWorks.TsMultiString"),
	&CLSID_TsMultiString,
	_T("FieldWorks MultiString"),
	_T("Apartment"),
	&TsMultiString::CreateCom);


/*----------------------------------------------------------------------------------------------
	Static method called by the class factory to create a MultiString.
----------------------------------------------------------------------------------------------*/
void TsMultiString::CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	if (punkOuter)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<TsMultiString> qtms;

	qtms.Attach(NewObj TsMultiString);
	CheckHr(qtms->QueryInterface(iid, ppv));
}


/*----------------------------------------------------------------------------------------------
	Interface Metods.
----------------------------------------------------------------------------------------------*/

/*----------------------------------------------------------------------------------------------
	Returns the number of alternate strings.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsMultiString::get_StringCount(int * pctss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pctss);

	*pctss = m_vtse.Size();

	END_COM_METHOD(g_factMultiString, IID_ITsMultiString);
}


/*----------------------------------------------------------------------------------------------
	Set *pptss to the string, and pws to the writing system of the alternative at index iws. If the
	MultiString is empty and iws is 0, pass an empty string and a zero	writing system. Thus iws == 0
	will always return the first string or an empty string.	Other than the above exception, if
	TsStringMulti does not hold a string at the requested index, it returns E_INVALIDARG.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsMultiString::GetStringFromIndex(int iws, int * pws, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);
	ChkComOutPtr(pws);

	if ((uint)iws >= (uint)m_vtse.Size())
	{
		if (iws > 0 || m_vtse.Size() > 0)
			ReturnHr(E_INVALIDARG);

		// Contains no strings, pass back the empty string.
		*pws = 0;
		TextServGlobals::GetEmptyString(pptss);
	}
	else
	{
		*pptss = m_vtse[iws].m_qtss;
		AddRefObj(*pptss);
		*pws = m_vtse[iws].m_ws;
	}

	END_COM_METHOD(g_factMultiString, IID_ITsMultiString);
}


/*----------------------------------------------------------------------------------------------
	Set *pptss to the string for alternative writing system ws. If the string does not already exist,
	an empty string is returned.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsMultiString::get_String(int ws, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);
	if ((uint)ws > kwsLim)
		ThrowInternalError(E_INVALIDARG, "Magic writing system invalid in string");

	int iws;
	if (FindStrEntry(ws, &iws))
	{
		*pptss = m_vtse[iws].m_qtss;
		AddRefObj(*pptss);
		return S_OK;
	}

	// No string exists, pass back the empty string.
	TextServGlobals::GetEmptyString(pptss);

	END_COM_METHOD(g_factMultiString, IID_ITsMultiString);
}


/*----------------------------------------------------------------------------------------------
	Store string ptss in the ws alternative. If a string already exists for ws, the original
	string will be replaced. If ptss is NULL, the alternative is deleted. The list must maintain
	sorted order.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsMultiString::putref_String(int ws, ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ptss);
	if ((uint)ws > kwsLim)
		ThrowInternalError(E_INVALIDARG, "Magic writing system invalid in string");

	int iws;
	if (FindStrEntry(ws, &iws))
	{
		if (!ptss)
			m_vtse.Delete(iws);
		else
			m_vtse[iws].m_qtss = ptss;
	}
	else if (ptss)
	{
		// Otherwise, add the new entry at this location.
		TsStrEntry tseT;

		tseT.m_ws = ws;
		tseT.m_qtss = ptss;
		m_vtse.Insert(iws, tseT);
	}
	END_COM_METHOD(g_factMultiString, IID_ITsMultiString);
}


#ifdef POSSIBLE_FUTURE_ENHANCEMENTS
/*----------------------------------------------------------------------------------------------
	Serialize the text and formatting information of the multistring to the given stream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsMultiString::SerializeFmt(IStream * pstrm)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstrm);

#ifdef TODO2
	DataWriterStrm dws(pstrm);

	// Write the text data first

	// Determine the total size of the strings concatinated together.
	for (iws = 0; iws < m_vtse.Size(); iws++)
	{
		cch += m_vtse[i].m_qtss.Length();
	}

	// Write the total size
	IgnoreHr(hr = dws->WriteInt(cch));	// REVIEW JeffG: - should this be in bytes or wchars
	if (FAILED(hr))
		return WarnHr(hr);

	// Write the text data
	for (iws = 0; iws < m_vtse.Size() ; iws++)
	{
		BSTR bstr;
		int cch;

		m_vtse.m_qtss.get_Text(bstr);
		m_vtse.m_qtss.get_Length(&cch);
		IgnoreHr(hr = dws->WriteBuf(&bstr, cch));
		if (FAILED(hr))
			return WarnHr(hr);
	}

	// Now write the format data

	// Write the number of encodings
	IgnoreHr(hr = dws->WriteInt(m_vtse.Size()));
	if (FAILED(hr))
		return WarnHr(hr);

	for (iws = 0; iws < m_vtse.Size() ; iws++)
	{
		// Write the writing system value.
		IgnoreHr(hr = dws->WriteInt(m_vtse[iws].m_ws));
		if (FAILED(hr))
			return WarnHr(hr);

		// Write the Fmt values
		IgnoreHr(hr = m_vtse[iws].m_qtss.SerializeFmt(pstrm));
		if (FAILED(hr))
			return WarnHr(hr);
	}

	return S_OK;
#else
	return WarnHr(E_NOTIMPL);
#endif
	END_COM_METHOD(g_factMultiString, IID_ITsMultiString);
}


/*----------------------------------------------------------------------------------------------
	Serialize the formatting information of the string to the given byte array.  If the cbMax is
	too small this sets *pcb to the required size and returns S_FALSE. The text data for each
	string is concatinated together in the bstr parameter.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsMultiString::SerializeFmtRgb(BYTE * prgb, int cbMax, int * pcb, BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	CheckComArrayArg(prgb, cbMax);
	ChkComArgPtr(pcb);
	ChkComBstrArg(pbstr);

	return WarnHr(E_NOTIMPL);

	END_COM_METHOD(g_factMultiString, IID_ITsMultiString);
}


STDMETHODIMP TsMultiString::Deserialize(IStream * pstrm)
{
	BEGIN_COM_METHOD;

	return E_NOTIMPL;

	END_COM_METHOD(g_factMultiString, IID_ITsMultiString);
}


/*----------------------------------------------------------------------------------------------
	Deserialize the TsMultiString from the given data.

----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsMultiString::DeserializeRgb(const OLECHAR * prgchTxt, int * pcchTxt,
										   const BYTE * prgbFmt, int * pcbFmt)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pcchTxt);
	ChkComArgPtr(pcbFmt);
	CheckComArrayArg(prgchTxt, *pcchTxt);
	CheckComArrayArg(prgbFmt, *pcbFmt);

#ifdef TODO2
	HRESULT hr;
	int cch = *pcchTxt;
	int cb = *pcbFmt;
	DataReaderRgb drrTxt(prgchTxt, cch);
	DataReaderRgb drrFmt(prgbFmt, cb);

	m_vtse.Clear();

	IgnoreHr(hr = DeserializeCore(pool, &drrTxt, &drrFmt));
	if (FAILED(hr))
	{
		m_vtse.Clear();
		return WarnHr(hr);
	}

	*pcchTxt = drrTxt.IbCur() / isizeof(OLECHAR);
	*pcbFmt = drrTxt.IbCur();
	Assert(*pcchTxt <= cch && *pcbFmt <= cb);

	return *pcchTxt < cch || *pcbFmt < cb ? S_FALSE : S_OK;
#else
	return WarnHr(E_NOTIMPL);
#endif
	END_COM_METHOD(g_factMultiString, IID_ITsMultiString);
}


/*----------------------------------------------------------------------------------------------
	Read the (writing system, string) pairs from the given data readers.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TsMultiString::DeserializeCore(DataReader * pdrdrTxt, DataReader * pdrdrFmt)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pdrdrTxt);
	ChkComArgPtr(pdrdrFmt);
	Assert(!m_vtse.Size());

#ifdef TODO2
	int ctss;
	int ws;

	IgnoreHr(hr = pdrdrFmt->ReadInt(&ctss));
	if (FAILED(hr))
		return WarnHr(hr);
	if (ctss < 0)
		return WarnHr(E_UNEXPECTED);

	if (ctss == 0)
		return S_OK;

	for (itss = 0; itss < ctss; itss++)
	{
		IgnoreHr(hr = pdrdrFmt->ReadInt(&ws));
		if (FAILED(hr))
			return WarnHr(hr);
		IgnoreHr(hr = TsStrFact::DeserializeStringCore(pool, pdrdrTxt, pdrdrFmt, &qtss));
		if (FAILED(hr))
			return WarnHr(hr);

		IgnoreHr(hr = putref_String(ws, qtss));
		if (FAILED(hr))
			return WarnHr(hr);
	}

	return S_OK;
#else
	return WarnHr(E_NOTIMPL);
#endif
	END_COM_METHOD(g_factMultiString, IID_ITsMultiString);
}
#endif /*POSSIBLE_FUTURE_ENHANCEMENTS*/


/***********************************************************************************************
	Non-Interface methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Look in the array of TsStrEntries for the given writing system. Set *pienc to the index where it
	was found and return true or set *pienc to where it would be inserted and return false.
----------------------------------------------------------------------------------------------*/
bool TsMultiString::FindStrEntry(int ws, int * pienc)
{
	AssertPtrN(pienc);
	if ((uint)ws > kwsLim)
		ThrowInternalError(E_INVALIDARG, "Magic writing system invalid in string");
	int itseMin = 0;
	int itseLim = m_vtse.Size();

	// Perform a binary search
	while (itseMin < itseLim)
	{
		int itseT = (itseMin + itseLim) >> 1;
		if (m_vtse[itseT].m_ws < ws)
			itseMin = itseT + 1;
		else
			itseLim = itseT;
	}
	if (pienc)
		*pienc = itseMin;

	return itseMin < m_vtse.Size() && m_vtse[itseMin].m_ws == ws;
}
