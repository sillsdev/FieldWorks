/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgCollation.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"

#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

// The class factory for Collation.
static GenericFactory g_fact(
	_T("FieldWorks.Collation"),
	&CLSID_Collation,
	_T("FieldWorks Collation"),
	_T("Apartment"),
	&Collation::CreateCom);

/***********************************************************************************************
	Methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Static method called by the class factory to create a MultiString.
----------------------------------------------------------------------------------------------*/
void Collation::CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	if (punkOuter)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<Collation> qcoll;

	qcoll.Attach(NewObj Collation);
	CheckHr(qcoll->QueryInterface(iid, ppv));
}

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
Collation::Collation(void)
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
}


/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
Collation::~Collation(void)
{
	ModuleEntry::ModuleRelease();
}

/***********************************************************************************************
	IUnknown interface methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown and ICollation are
	supported.

	This is a standard COM IUnknown method.

	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.

	@return SOK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_ICollation)
		*ppv = static_cast<ICollation *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ICollation);
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
STDMETHODIMP_(ULONG) Collation::AddRef(void)
{
	Assert(m_cref > 0);
	::InterlockedIncrement(&m_cref);
	return m_cref;
}


/*----------------------------------------------------------------------------------------------
	Release.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) Collation::Release(void)
{
	ulong cref = ::InterlockedDecrement(&m_cref);
	if (!cref)
	{
		m_cref = 1;
		delete this;
	}
	return cref;
}

/***********************************************************************************************
	ICollation interface methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Get the name in the given writing system.  Return S_FALSE if the given writing system does not
	have a name assigned.

	@param ws
	@param pbstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::get_Name(int ws, BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstr);

	StrUni stu;
	if (m_hmencstuName.Retrieve(ws, &stu) && stu.Length())
		stu.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Set the name in the given writing system.

	@param ws
	@param bstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::put_Name(int ws, BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stuOld;
	bool fHaveOld = m_hmencstuName.Retrieve(ws, &stuOld);
	if (bstr && BstrLen(bstr))
	{
		StrUni stu(bstr, BstrLen(bstr));
		if (stu != stuOld)
		{
			m_hmencstuName.Insert(ws, stu, true);
			m_fDirty = true;
		}
	}
	else if (fHaveOld)
	{
		m_hmencstuName.Delete(ws);
		m_fDirty = true;
	}

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Get the number of encodings in which the name of this collation is stored.

	@param pcws Pointer to an integer for returning the number of encodings.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::get_NameWsCount(int * pcws)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcws);

	*pcws = m_hmencstuName.Size();

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Get the (first cws) encodings in which the name of this old writing system is stored.  If cws
	is larger than the number of encodings, the excess entries in prgenc are set to zero.

	@param cws Number of entries available in prgenc.
	@param prgenc Pointer to an array for returning the encodings.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::get_NameWss(int cws, int * prgenc)
{
	BEGIN_COM_METHOD;
	if (cws < 0)
		ThrowHr(WarnHr(E_INVALIDARG));
	ChkComArrayArg(prgenc, cws);

	int iws;
	HashMap<int, StrUni>::iterator it;
	for (iws = 0, it = m_hmencstuName.Begin(); it != m_hmencstuName.End(); ++it, ++iws)
	{
		if (iws < cws)
			prgenc[iws] = it.GetKey();
		else
			break;
	}
	for ( ; iws < cws; ++iws)
		prgenc[iws] = 0;

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Get the collation's database id.

	@param phvo Pointer to an integer for returning the database id.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::get_Hvo(int * phvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvo);

	if (!m_hvo)
	{
	}
	*phvo = m_hvo;

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Set the collation database id.  THIS IS DANGEROUS AND SHOULD BE DONE EXACTLY ONCE!

	@param hvo Database id value.
----------------------------------------------------------------------------------------------*/
void Collation::SetHvo(int hvo)
{
	if (!hvo)
		ThrowHr(WarnHr(E_INVALIDARG));
	if (m_hvo && hvo != m_hvo)
		ThrowHr(WarnHr(E_FAIL));

	m_hvo = hvo;
}

/*----------------------------------------------------------------------------------------------
	Get the Windows LCID (locale identifier).

	@param plcid
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::get_WinLCID(int * plcid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(plcid);

	*plcid = m_lcid;

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Set the Windows LCID (locale identifier).  This clears any IcuResourceName or
	IcuResourceText.

	@param lcid
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::put_WinLCID(int lcid)
{
	BEGIN_COM_METHOD;

	if (m_lcid != lcid)
	{
		m_lcid = lcid;
		m_fDirty = true;
	}

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Get the Windows collation designator.

	@param pbstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::get_WinCollation(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstr);

	if (m_stuWinCollation.Length())
		m_stuWinCollation.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Set the Windows collation designator.  This clears any IcuResourceName or IcuResourceText.

	@param bstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::put_WinCollation(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuWinCollation != stu)
	{
		m_stuWinCollation.Assign(stu);
		m_fDirty = true;
	}
	if (m_stuIcuResourceName.Length())
	{
		m_stuIcuResourceName.Clear();
		m_fDirty = true;
	}
	if (m_stuIcuResourceText.Length())
	{
		m_stuIcuResourceText.Clear();
		m_fDirty = true;
	}

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Get the ICU resource name.

	@param pbstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::get_IcuResourceName(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstr);

	if (m_stuIcuResourceName.Length())
		m_stuIcuResourceName.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Set the ICU resource name.  This clears any WinLCID or WinCollation value.

	@param bstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::put_IcuResourceName(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuIcuResourceName != stu)
	{
		m_stuIcuResourceName.Assign(stu);
		m_fDirty = true;
	}
	if (m_lcid)
	{
		m_lcid = 0;
		m_fDirty = true;
	}
	if (m_stuWinCollation.Length())
	{
		m_stuWinCollation.Clear();
		m_fDirty = true;
	}

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Get the ICU resource text.

	@param pbstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::get_IcuResourceText(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstr);

	if (m_stuIcuResourceText.Length())
		m_stuIcuResourceText.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Set the ICU resource text.  This clears any WinLCID or WinCollation value.

	@param bstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::put_IcuResourceText(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuIcuResourceText != stu)
	{
		m_stuIcuResourceText.Assign(stu);
		m_fDirty = true;
	}
	if (m_lcid)
	{
		m_lcid = 0;
		m_fDirty = true;
	}
	if (m_stuWinCollation.Length())
	{
		m_stuWinCollation.Clear();
		m_fDirty = true;
	}

	END_COM_METHOD(g_fact, IID_ICollation);
}


/*----------------------------------------------------------------------------------------------
	Get the ICU rules.

	@param pbstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::get_IcuRules(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstr);

	if (m_stuIcuRules.Length())
		m_stuIcuRules.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Set the ICU rules.

	@param bstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::put_IcuRules(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuIcuRules != stu)
	{
		m_stuIcuRules.Assign(stu);
		m_fDirty = true;
	}

	END_COM_METHOD(g_fact, IID_ICollation);
}


/*----------------------------------------------------------------------------------------------
	Check whether this collation needs to be saved to persistent storage.

	@param pf Pointer to the ComBool for returning whether this old writing system is "dirty".
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::get_Dirty(ComBool * pf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pf);

	*pf = m_fDirty;

	END_COM_METHOD(g_fact, IID_ICollation);
}


/*----------------------------------------------------------------------------------------------
	Indicate whether this collation needs to be saved to persistent storage.

	@param pf Pointer to the ComBool for returning whether this old writing system is "dirty".
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::put_Dirty(ComBool fDirty)
{
	BEGIN_COM_METHOD;

	m_fDirty = (bool)fDirty;

	END_COM_METHOD(g_fact, IID_ICollation);
}


/*----------------------------------------------------------------------------------------------
	Write the collation to the stream in standard FieldWorks XML format.  Indent the number of
	spaces given by cchIndent, which may be zero.

	@param pstrm
	@param cchIndent
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::WriteAsXml(IStream * pstrm, int cchIndent)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstrm);

	if (cchIndent < 0)
		cchIndent = 0;		// Ignore negative numbers.
	Vector<char> vchIndent;
	if (cchIndent)
	{
		vchIndent.Resize(cchIndent + 5);
		memset(vchIndent.Begin(), ' ', cchIndent + 4);
	}
	const char * pszIndent = cchIndent ? vchIndent.Begin() + 4 : "";
	const char * pszIndent2 = cchIndent ? vchIndent.Begin() + 2 : "";
	const char * pszIndent4 = cchIndent ? vchIndent.Begin() : "";
	FormatToStream(pstrm, "%s<LgCollation>%n", pszIndent);
	if (m_hmencstuName.Size())
	{
		FormatToStream(pstrm, "%s<Name30>%n", pszIndent2);
		SmartBstr sbstr;
		HashMap<int, StrUni>::iterator it;
		for (it = m_hmencstuName.Begin(); it != m_hmencstuName.End(); ++it)
		{
			FormatToStream(pstrm, "%s<AUni ws=\"", pszIndent4);
			int ws = it.GetKey();
			if (ws)
			{
				CheckHr(m_qwsf->GetStrFromWs(ws, &sbstr));
				WriteXmlUnicode(pstrm, sbstr.Chars(), sbstr.Length());
			}
			else
			{
				StrUni stuUserWs(kstidUserWs);
				WriteXmlUnicode(pstrm, stuUserWs.Chars(), stuUserWs.Length());
			}
			FormatToStream(pstrm, "\">");
			WriteXmlUnicode(pstrm, it.GetValue().Chars(), it.GetValue().Length());
			FormatToStream(pstrm, "</AUni>%n");
		}
		FormatToStream(pstrm, "%s</Name30>%n", pszIndent2);
	}
	if (m_lcid)
	{
		FormatToStream(pstrm, "%s<WinLCID30><Integer val=\"%d\"/></WinLCID30>%n",
			pszIndent2, m_lcid);
	}
	if (m_stuWinCollation.Length())
	{
		FormatToStream(pstrm, "%s<WinCollation30><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuWinCollation.Chars(), m_stuWinCollation.Length());
		FormatToStream(pstrm, "</Uni></WinCollation30>%n");
	}
	if (m_stuIcuResourceName.Length())
	{
		FormatToStream(pstrm, "%s<IcuResourceName30><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuIcuResourceName.Chars(),
			m_stuIcuResourceName.Length());
		FormatToStream(pstrm, "</Uni></IcuResourceName30>%n");
	}
	if (m_stuIcuResourceText.Length())
	{
		// REVIEW: Should this be a binary hex dump instead?
		FormatToStream(pstrm, "%s<IcuResourceText30><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuIcuResourceText.Chars(),
			m_stuIcuResourceText.Length());
		FormatToStream(pstrm, "</Uni></IcuResourceText30>%n");
	}
	if (m_stuIcuRules.Length())
	{
		FormatToStream(pstrm, "%s<ICURules30><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuIcuRules.Chars(), m_stuIcuRules.Length());
		FormatToStream(pstrm, "</Uni></ICURules30>%n");
	}
	FormatToStream(pstrm, "%s</LgCollation>%n", pszIndent);

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Get the factory for this writing system.

	@param ppwsf Address where to store a pointer to the writing system factory that
					stores/produces this writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppwsf);
	AssertPtr(m_qwsf);

	*ppwsf = m_qwsf;
	if (*ppwsf)
		(*ppwsf)->AddRef();

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Set the factory for this writing system.  This also sets the factory for the
	writing system's renderers and collaters.

	@param pwsf Pointer to the writing system factory that stores/produces this writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pwsf);

	m_qwsf = pwsf;

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Write the pertinent data to the IStorage object to allow a read-only copy of this collation
	to be reconstituted later.

	@param pstg Pointer to the IStorage object used to store the collation's data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::Serialize(IStorage * pstg)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstg);

	IStreamPtr qstrm;
	CheckHr(pstg->CreateStream(L"Data", STGM_READWRITE | STGM_SHARE_EXCLUSIVE, 0, 0, &qstrm));
	ULONG cb;
	ULONG cbWritten;
	cb = isizeof(m_lcid);
	CheckHr(qstrm->Write(&m_lcid, cb, &cbWritten));
	int cch = m_stuWinCollation.Length();
	cb = isizeof(cch);
	CheckHr(qstrm->Write(&cch, cb, &cbWritten));
	cb = cch * isizeof(OLECHAR);
	CheckHr(qstrm->Write(m_stuWinCollation.Chars(), cb, &cbWritten));
	cch = m_stuIcuResourceName.Length();
	cb = isizeof(cch);
	CheckHr(qstrm->Write(&cch, cb, &cbWritten));
	cb = cch * isizeof(OLECHAR);
	CheckHr(qstrm->Write(m_stuIcuResourceName.Chars(), cb, &cbWritten));
	cch = m_stuIcuResourceText.Length();
	cb = isizeof(cch);
	CheckHr(qstrm->Write(&cch, cb, &cbWritten));
	cb = cch * isizeof(OLECHAR);
	CheckHr(qstrm->Write(m_stuIcuResourceText.Chars(), cb, &cbWritten));
	cch = m_stuIcuRules.Length();
	cb = isizeof(cch);
	CheckHr(qstrm->Write(&cch, cb, &cbWritten));
	cb = cch * isizeof(OLECHAR);
	CheckHr(qstrm->Write(m_stuIcuRules.Chars(), cb, &cbWritten));
	// Multilingual name.
	int cName = m_hmencstuName.Size();
	cb = isizeof(cName);
	CheckHr(qstrm->Write(&cName, cb, &cbWritten));
	HashMap<int, StrUni>::iterator ithm;
	for (ithm = m_hmencstuName.Begin(); ithm != m_hmencstuName.End(); ++ithm)
	{
		int ws = ithm.GetKey();
		StrUni & stuName = ithm.GetValue();
		cb = isizeof(ws);
		CheckHr(qstrm->Write(&ws, cb, &cbWritten));
		cch = stuName.Length();
		cb = isizeof(cch);
		CheckHr(qstrm->Write(&cch, cb, &cbWritten));
		cb = cch * isizeof(OLECHAR);
		CheckHr(qstrm->Write(stuName.Chars(), cb, &cbWritten));
	}
	CheckHr(qstrm->Commit(STGC_DEFAULT));

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Read the pertinent data from the IStorage object to initialize a read-only copy of the
	collation serialized earlier.

	@param pstg Pointer to the IStorage object used to store the collation's data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::Deserialize(IStorage * pstg)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstg);

	IStreamPtr qstrm;
	CheckHr(pstg->OpenStream(L"Data", NULL, STGM_READ | STGM_SHARE_EXCLUSIVE, 0, &qstrm));
	ULONG cb;
	ULONG cbRead;
	cb = isizeof(m_lcid);
	CheckHr(qstrm->Read(&m_lcid, cb, &cbRead));
	int cch;
	Vector<OLECHAR> vch;
	cb = isizeof(cch);
	CheckHr(qstrm->Read(&cch, cb, &cbRead));
	vch.Resize(cch);
	cb = cch * isizeof(OLECHAR);
	CheckHr(qstrm->Read(vch.Begin(), cb, &cbRead));
	m_stuWinCollation.Assign(vch.Begin(), cch);
	cb = isizeof(cch);
	CheckHr(qstrm->Read(&cch, cb, &cbRead));
	vch.Resize(cch);
	cb = cch * isizeof(OLECHAR);
	CheckHr(qstrm->Read(vch.Begin(), cb, &cbRead));
	m_stuIcuResourceName.Assign(vch.Begin(), cch);
	cb = isizeof(cch);
	CheckHr(qstrm->Read(&cch, cb, &cbRead));
	vch.Resize(cch);
	cb = cch * isizeof(OLECHAR);
	CheckHr(qstrm->Read(vch.Begin(), cb, &cbRead));
	m_stuIcuResourceText.Assign(vch.Begin(), cch);
	cb = isizeof(cch);
	CheckHr(qstrm->Read(&cch, cb, &cbRead));
	vch.Resize(cch);
	cb = cch * isizeof(OLECHAR);
	CheckHr(qstrm->Read(vch.Begin(), cb, &cbRead));
	m_stuIcuRules.Assign(vch.Begin(), cch);
	// Multilingual name.
	int cName;
	int ws;
	int i;
	cb = isizeof(cName);
	CheckHr(qstrm->Read(&cName, cb, &cbRead));
	for (i = 0; i < cName; ++i)
	{
		cb = isizeof(ws);
		CheckHr(qstrm->Read(&ws, cb, &cbRead));
		cb = isizeof(cch);
		CheckHr(qstrm->Read(&cch, cb, &cbRead));
		vch.Resize(cch);
		cb = cch * isizeof(OLECHAR);
		CheckHr(qstrm->Read(vch.Begin(), cb, &cbRead));
		StrUni stuName(vch.Begin(), cch);
		m_hmencstuName.Insert(ws, stuName, true);
	}

	END_COM_METHOD(g_fact, IID_ICollation);
}


/*----------------------------------------------------------------------------------------------
	Initialize the ICU rules from the given ICU locale collation data.

	@param bstrBaseLocale
----------------------------------------------------------------------------------------------*/
STDMETHODIMP Collation::LoadIcuRules(BSTR bstrBaseLocale)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrBaseLocale);

	StrUni stu;
	StrUtil::InitIcuDataDir();
	StrAnsi staBaseLocale(bstrBaseLocale);
	UErrorCode uerr = U_ZERO_ERROR;
	UCollator * pcoll = ucol_open(staBaseLocale.Chars(), &uerr);
	if (U_SUCCESS(uerr))
	{
		Vector<UChar> vchRules;
		vchRules.Resize(1000);
		int cchRules = ucol_getRulesEx(pcoll, UCOL_TAILORING_ONLY, vchRules.Begin(),
			vchRules.Size());
		if (cchRules > vchRules.Size())
		{
			vchRules.Resize(cchRules);
			cchRules = ucol_getRulesEx(pcoll, UCOL_TAILORING_ONLY, vchRules.Begin(),
				vchRules.Size());
			Assert(cchRules == vchRules.Size());
		}
		else
		{
			vchRules.Resize(cchRules);
		}
		stu.Assign(vchRules.Begin(), vchRules.Size());
		// Make the result more readable for the user.
		int ich = stu.FindCh('&');
		while (ich >= 0)
		{
			ich = stu.FindCh('&', ich+1);
			if (ich > 0)
			{
				stu.Replace(ich, ich, L"\r\n");
				ich += 2;
			}
		}
		ucol_close(pcoll);
	}
	if (stu != m_stuIcuRules)
		return put_IcuRules(stu.Bstr());

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*
Windows Collation Designators

Use this table to synchronize collation settings with another Windows locale.

Windows locale							LCID		Collation designator		Code page
--------------							----		--------------------		---------
Afrikaans								0x436		Latin1_General				1252
Albanian								0x41C		Albanian					1250
Arabic (Saudi Arabia)					0x401		Arabic						1256
Arabic (Iraq)							0x801		Arabic						1256
Arabic (Egypt)							0xC01		Arabic						1256
Arabic (Libya)							0x1001		Arabic						1256
Arabic (Algeria)						0x1401		Arabic						1256
Arabic (Morocco)						0x1801		Arabic						1256
Arabic (Tunisia)						0x1C01		Arabic						1256
Arabic (Oman)							0x2001		Arabic						1256
Arabic (Yemen)							0x2401		Arabic						1256
Arabic (Syria)							0x2801		Arabic						1256
Arabic (Jordan)							0x2C01		Arabic						1256
Arabic (Lebanon)						0x3001		Arabic						1256
Arabic (Kuwait)							0x3401		Arabic						1256
Arabic (United Arab Emirates)			0x3801		Arabic						1256
Arabic (Bahrain)						0x3C01		Arabic						1256
Arabic (Qatar)							0x4001		Arabic						1256
Basque									0x42D		Latin1_General				1252
Byelorussian							0x423		Cyrillic_General			1251
Bulgarian								0x402		Cyrillic_General			1251
Catalan									0x403		Latin1_General				1252
Chinese (Taiwan)						0x30404		Chinese_Taiwan_Bopomofo		950
Chinese (Taiwan)						0x404		Chinese_Taiwan_Stroke		950
Chinese (People's Republic of China)	0x804		Chinese_PRC					936
Chinese (People's Republic of China)	0x20804		Chinese_PRC_Stroke			936
Chinese (Singapore)						0x1004		Chinese_PRC					936
Croatia									0x41a		Croatian					1250
Czech									0x405		Czech						1250
Danish									0x406		Danish_Norwegian			1252
Dutch (Standard)						0x413		Latin1_General				1252
Dutch (Belgium)							0x813		Latin1_General				1252
English (United States)					0x409		Latin1_General				1252
English (Britain)						0x809		Latin1_General				1252
English (Canada)						0x1009		Latin1_General				1252
English (New Zealand)					0x1409		Latin1_General				1252
English (Australia)						0xC09		Latin1_General				1252
English (Ireland)						0x1809		Latin1_General				1252
English (South Africa)					0x1C09		Latin1_General				1252
English (Carribean)						0x2409		Latin1_General				1252
English (Jamaican)						0x2009		Latin1_General				1252
Estonian								0x425		Estonian					1257
Faeroese								0x0438		Latin1_General				1252
Farsi									0x429		Arabic						1256
Finnish									0x40B		Finnish_Swedish				1252
French (Standard)						0x40C		French						1252
French (Belgium)						0x80C		French						1252
French (Switzerland)					0x100C		French						1252
French (Canada)							0xC0C		French						1252
French (Luxembourg)						0x140C		French						1252
Georgian (Modern Sort)					0x10437		Georgian_Modern_Sort		1252
German (PhoneBook Sort)					0x10407		German_PhoneBook			1252
German (Standard)						0x407		Latin1_General				1252
German (Switzerland)					0x807		Latin1_General				1252
German (Austria)						0xC07		Latin1_General				1252
German (Luxembourg)						0x1007		Latin1_General				1252
German (Liechtenstein)					0x1407		Latin1_General				1252
Greek									0x408		Greek						1253
Hebrew									0x40D		Hebrew						1255
Hindi									0x439		Hindi						Unicode only
Hungarian								0x40E		Hungarian					1250
Hungarian								0x104E		Hungarian_Technical			1250
Icelandic								0x40F		Icelandic					1252
Indonesian								0x421		Latin1_General				1252
Italian									0x410		Latin1_General				1252
Italian (Switzerland)					0x810		Latin1_General				1252
Japanese								0x411		Japanese					932
Japanese (Unicode)						0x10411		Japanese_Unicode			932
Korean (Extended Wansung)				0x412		Korean_Wansung				949
Korean									0x412		Korean_Wansung_Unicode		949
Latvian									0x426		Latvian						1257
Lithuanian								0x427		Lithuanian					1257
Lithuanian								0x827		Lithuanian_Classic			1257
Macedonian (Former Yugoslav Rep of M)	0x41C		Cyrillic_General			1251
Norwegian (BokmÅÂl)						0x414		Danish_Norwegian			1252
Norwegian (Nynorsk)						0x814		Danish_Norwegian			1252
Polish									0x415		Polish						1250
Portuguese (Standard)					0x816		Latin1_General				1252
Portuguese (Brazil)						0x416		Latin1_General				1252
Romanian								0x418		Romanian					1250
Russian									0x419		Cyrillic_General			1251
Serbian (Latin)							0x81A		Cyrillic_General			1251
Serbian (Cyrillic)						0xC1A		Cyrillic_General			1251
Slovak									0x41B		Slovak						1250
Slovenian								0x424		Slovenian					1250
Spanish (Mexico)						0x80A		Traditional_Spanish			1252
Spanish (Traditional Sort)				0x40A		Traditional_Spanish			1252
Spanish (Modern Sort)					0xC0A		Modern_Spanish				1252
Spanish (Guatemala)						0x100A		Modern_Spanish				1252
Spanish (Costa Rica)					0x140A		Modern_Spanish				1252
Spanish (Panama)						0x180A		Modern_Spanish				1252
Spanish (Dominican Republic)			0x1C0A		Modern_Spanish				1252
Spanish (Venezuela)						0x200A		Modern_Spanish				1252
Spanish (Colombia)						0x240A		Modern_Spanish				1252
Spanish (Peru)							0x280A		Modern_Spanish				1252
Spanish (Argentina)						0x2C0A		Modern_Spanish				1252
Spanish (Ecuador)						0x300A		Modern_Spanish				1252
Spanish (Chile)							0x340A		Modern_Spanish				1252
Spanish (Uruguay)						0x380A		Modern_Spanish				1252
Spanish (Paraguay)						0x3C0A		Modern_Spanish				1252
Spanish (Bolivia)						0x400A		Modern_Spanish				1252
Swedish									0x41D		Finnish_Swedish				1252
Thai									0x41E		Thai						874
Turkish									0x41F		Turkish						1254
Ukrainian								0x422		Ukrainian					1251
Urdu									0x420		Arabic						1256
Vietnamese								0x42A		Vietnamese					1258

©1988-2001 Microsoft Corporation. All Rights Reserved.
*/
struct CollationMap
{
	int lcid;
	const wchar * pszColl;
};
static CollationMap cmWin[] =
{
	{	0x00401,	L"Arabic"					},
	{	0x00402,	L"Cyrillic_General"			},
	{	0x00403,	L"Latin1_General"			},
	{	0x00404,	L"Chinese_Taiwan_Stroke"	},
	{	0x00405,	L"Czech"					},
	{	0x00406,	L"Danish_Norwegian"			},
	{	0x00407,	L"Latin1_General"			},
	{	0x00408,	L"Greek"					},
	{	0x00409,	L"Latin1_General"			},
	{	0x0040A,	L"Traditional_Spanish"		},
	{	0x0040B,	L"Finnish_Swedish"			},
	{	0x0040C,	L"French"					},
	{	0x0040D,	L"Hebrew"					},
	{	0x0040E,	L"Hungarian"				},
	{	0x0040F,	L"Icelandic"				},
	{	0x00410,	L"Latin1_General"			},
	{	0x00411,	L"Japanese"					},
//	{	0x00412,	L"Korean_Wansung"			},
	{	0x00412,	L"Korean_Wansung_Unicode"	},
	{	0x00413,	L"Latin1_General"			},
	{	0x00414,	L"Danish_Norwegian"			},
	{	0x00415,	L"Polish"					},
	{	0x00416,	L"Latin1_General"			},
	{	0x00418,	L"Romanian"					},
	{	0x00419,	L"Cyrillic_General"			},
	{	0x0041B,	L"Slovak"					},
	{	0x0041C,	L"Albanian"					},
//	{	0x0041C,	L"Cyrillic_General"			},
	{	0x0041D,	L"Finnish_Swedish"			},
	{	0x0041E,	L"Thai"						},
	{	0x0041F,	L"Turkish"					},
	{	0x0041a,	L"Croatian"					},
	{	0x00420,	L"Arabic"					},
	{	0x00421,	L"Latin1_General"			},
	{	0x00422,	L"Ukrainian"				},
	{	0x00423,	L"Cyrillic_General"			},
	{	0x00424,	L"Slovenian"				},
	{	0x00425,	L"Estonian"					},
	{	0x00426,	L"Latvian"					},
	{	0x00427,	L"Lithuanian"				},
	{	0x00429,	L"Arabic"					},
	{	0x0042A,	L"Vietnamese"				},
	{	0x0042D,	L"Latin1_General"			},
	{	0x00436,	L"Latin1_General"			},
	{	0x00438,	L"Latin1_General"			},
	{	0x00439,	L"Hindi"					},
	{	0x00801,	L"Arabic"					},
	{	0x00804,	L"Chinese_PRC"				},
	{	0x00807,	L"Latin1_General"			},
	{	0x00809,	L"Latin1_General"			},
	{	0x0080A,	L"Traditional_Spanish"		},
	{	0x0080C,	L"French"					},
	{	0x00810,	L"Latin1_General"			},
	{	0x00813,	L"Latin1_General"			},
	{	0x00814,	L"Danish_Norwegian"			},
	{	0x00816,	L"Latin1_General"			},
	{	0x0081A,	L"Cyrillic_General"			},
	{	0x00827,	L"Lithuanian_Classic"		},
	{	0x00C01,	L"Arabic"					},
	{	0x00C07,	L"Latin1_General"			},
	{	0x00C09,	L"Latin1_General"			},
	{	0x00C0A,	L"Modern_Spanish"			},
	{	0x00C0C,	L"French"					},
	{	0x00C1A,	L"Cyrillic_General"			},
	{	0x01001,	L"Arabic"					},
	{	0x01004,	L"Chinese_PRC"				},
	{	0x01007,	L"Latin1_General"			},
	{	0x01009,	L"Latin1_General"			},
	{	0x0100A,	L"Modern_Spanish"			},
	{	0x0100C,	L"French"					},
	{	0x0104E,	L"Hungarian_Technical"		},
	{	0x01401,	L"Arabic"					},
	{	0x01407,	L"Latin1_General"			},
	{	0x01409,	L"Latin1_General"			},
	{	0x0140A,	L"Modern_Spanish"			},
	{	0x0140C,	L"French"					},
	{	0x01801,	L"Arabic"					},
	{	0x01809,	L"Latin1_General"			},
	{	0x0180A,	L"Modern_Spanish"			},
	{	0x01C01,	L"Arabic"					},
	{	0x01C09,	L"Latin1_General"			},
	{	0x01C0A,	L"Modern_Spanish"			},
	{	0x02001,	L"Arabic"					},
	{	0x02009,	L"Latin1_General"			},
	{	0x0200A,	L"Modern_Spanish"			},
	{	0x02401,	L"Arabic"					},
	{	0x02409,	L"Latin1_General"			},
	{	0x0240A,	L"Modern_Spanish"			},
	{	0x02801,	L"Arabic"					},
	{	0x0280A,	L"Modern_Spanish"			},
	{	0x02C01,	L"Arabic"					},
	{	0x02C0A,	L"Modern_Spanish"			},
	{	0x03001,	L"Arabic"					},
	{	0x0300A,	L"Modern_Spanish"			},
	{	0x03401,	L"Arabic"					},
	{	0x0340A,	L"Modern_Spanish"			},
	{	0x03801,	L"Arabic"					},
	{	0x0380A,	L"Modern_Spanish"			},
	{	0x03C01,	L"Arabic"					},
	{	0x03C0A,	L"Modern_Spanish"			},
	{	0x04001,	L"Arabic"					},
	{	0x0400A,	L"Modern_Spanish"			},
	{	0x10407,	L"German_PhoneBook"			},
	{	0x10411,	L"Japanese_Unicode"			},
	{	0x10437,	L"Georgian_Modern_Sort"		},
	{	0x20804,	L"Chinese_PRC_Stroke"		},
	{	0x30404,	L"Chinese_Taiwan_Bopomofo"	}
};
static const int ccmWin = isizeof(cmWin) / isizeof(CollationMap);


/*----------------------------------------------------------------------------------------------
	Return the collation designator name associated with the given locale id, or NULL if none is
	known.

	@param lcid locale id
----------------------------------------------------------------------------------------------*/
const wchar * Collation::GetCollationName(int lcid)
{
	int iv;
	int ivLim;
	for (iv = 0, ivLim = ccmWin; iv < ivLim; )
	{
		int ivMid = (iv + ivLim) / 2;
		if (cmWin[ivMid].lcid < lcid)
			iv = ivMid + 1;
		else
			ivLim = ivMid;
	}
	if (iv < ccmWin && cmWin[iv].lcid == lcid)
	{
		return cmWin[iv].pszColl;
	}
	else
	{
		return NULL;
	}
}

Vector<StrUni> Collation::s_vstuColl;

/*----------------------------------------------------------------------------------------------
	Check whether the given collation designator is actually supported by the current database.

	@param stuColl collation designator name.
	@param pdbi Pointer to the current database info.
----------------------------------------------------------------------------------------------*/
bool Collation::_IsValidCollation(StrUni & stuColl, IOleDbEncap * pode)
{
	if (!s_vstuColl.Size())
	{
		try
		{
			IOleDbCommandPtr qodc;
			CheckHr(pode->CreateCommand(&qodc));
			StrUni stuQuery;
			stuQuery.Format(L"SELECT name FROM ::fn_helpcollations()%n"
				L"WHERE name NOT LIKE 'SQL_%%' AND name LIKE '%%_CI_AI'%n"
				L"ORDER BY NAME");
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			ComBool fMoreRows;
			CheckHr(qodc->NextRow(&fMoreRows));
			ComBool fIsNull;
			ULONG cbSpaceTaken;
			OLECHAR rgchName[MAX_PATH];
			StrUni stu;
			int ich;
			while (fMoreRows)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(rgchName),
					isizeof(rgchName), &cbSpaceTaken, &fIsNull, 2));
				stu.Assign(rgchName);
				ich = stu.FindStr(L"_CI_AI");
				Assert(ich > 0);
				stu.Replace(ich, stu.Length(), L"");
				s_vstuColl.Push(stu);
				CheckHr(qodc->NextRow(&fMoreRows));
			}
		}
		catch (...)
		{
			s_vstuColl.Clear();
		}
	}
	int iv;
	int ivLim;
	for (iv = 0, ivLim = s_vstuColl.Size(); iv < ivLim; )
	{
		int ivMid = (iv + ivLim) / 2;
		if (s_vstuColl[ivMid] < stuColl)
			iv = ivMid + 1;
		else
			ivLim = ivMid;
	}
	if (iv < s_vstuColl.Size() && s_vstuColl[iv] == stuColl)
		return true;
	else
		return false;
}

/*
Case-sensitive

Specifies that SQL Server distinguish between uppercase and lowercase letters.
If not selected, SQL Server considers the uppercase and lowercase versions of letters to be
equal. SQL Server does not define whether lowercase letters sort lower or higher in relation to
uppercase letters when Case-sensitive is not selected.

Accent-sensitive

Specifies that SQL Server distinguish between accented and unaccented characters. For example,
'a' is not equal to 'Å·'.
If not selected, SQL Server considers the accented and unaccented versions of letters to be
equal.
*/
/*
Transact-SQL Reference


Windows Collation Name

Specifies the Windows collation name in the COLLATE clause. The Windows collations name is composed of the collation designator and the comparison styles.

Syntax
< Windows_collation_name > :: =

	CollationDesignator_<ComparisonStyle>

	< ComparisonStyle > :: =
		CaseSensitivity_AccentSensitivity
		[_KanatypeSensitive [_WidthSensitive ] ]
		| _BIN

Arguments
CollationDesignator

Specifies the base collation rules used by the Windows collation. The base collation rules
cover:

* The alphabet or language whose sorting rules are applied when dictionary sorting is specified
* The code page used to store non-Unicode character data.

Examples are Latin1_General or French, both of which use code page 1252, or Turkish, which uses
code page 1254.

CaseSensitivity:	CI specifies case-insensitive, CS specifies case-sensitive.

AccentSensitivity:	AI specifies accent-insensitive, AS specifies accent-sensitive.
KanatypeSensitive:	Omitted specifies kanatype-insensitive, KS specifies kanatype-sensitive.
WidthSensitivity:	Omitted specifies width-insensitive, WS specifies width-sensitive.
BIN					Specifies the binary sort order is to be used.


Examples
These are some examples of Windows collation names:

Latin1_General_CI_AS
Collation uses the Latin1 General dictionary sorting rules, code page 1252. Is case-insensitive
and accent-sensitive.

Estonian_CS_AS
Collation uses the Estonian dictionary sorting rules, code page 1257. Is case-sensitive and
accent-sensitive.

Latin1_General_BIN
Collation uses code page 1252 and binary sorting rules. The Latin1 General dictionary sorting
rules are ignored.


See Also

ALTER TABLE

Collation Settings in Setup

Constants

CREATE DATABASE

CREATE TABLE

DECLARE @local_variable

table

Windows Collation Names Table

©1988-2001 Microsoft Corporation. All Rights Reserved.
 */
