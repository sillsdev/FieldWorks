/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WritingSystem.cpp
Responsibility:
Last reviewed:

	A representation of one way of writing system data in a language.  Mainly it consists of
	engines which can perform particular manipulations of data in the language.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)
#include <oledb.h>

#undef THIS_FILE
DEFINE_THIS_FILE

// Magic font strings that are used in markup (TomB: I think these are somewhat obsolete. Current ones are defined in FwKernelLib.idh):
static const wchar * g_pszDefaultFixed = L"<default fixed>";
static const wchar * g_pszDefaultFont = L"<default font>";
static const wchar * g_pszDefaultHeadingFont = L"<default heading font>";

// Make an writing system factory
static GenericFactory g_fact_pseudonym(
	_T("SIL.WritingSystem"),
	&CLSID_WritingSystem,
	_T("WritingSystem"),
	_T("Apartment"),
	&WritingSystem::CreateCom);

void WritingSystem::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<WritingSystem> qwsm;
	qwsm.Attach(NewObj WritingSystem());		// ref count initially 1
	CheckHr(qwsm->QueryInterface(riid, ppv));
}

void WritingSystem::Create(ILgWritingSystemFactory * pwsf, WritingSystem ** ppws)
{
	AssertPtr(pwsf);
	AssertPtr(ppws);
	Assert(!*ppws);
	*ppws = NewObj WritingSystem();		// ref count initially 1
	AssertPtr(*ppws);
	(*ppws)->putref_WritingSystemFactory(pwsf);
}


//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

inline void InitializeDWORDMetrics(DWORD* pVariable, const achar * pszKey, const DWORD defaultvalue)
{
	*pVariable = defaultvalue;
	FwSettings::GetDword(_T("Software\\SIL\\Fieldworks\\Metrics"), NULL, pszKey, pVariable);
}

inline void InitializeBoolMetrics(bool* pVariable, const achar * pszKey, const bool defaultvalue)
{
	*pVariable = defaultvalue;
	FwSettings::GetBool(_T("Software\\SIL\\Fieldworks\\Metrics"), NULL, pszKey, pVariable);
}

WritingSystem::WritingSystem()
{
	m_cref = 1;
	// Assign our designated super default.
	m_stuDefSerif.Assign(L"Times New Roman");
	m_stuDefSans.Assign(L"Arial");
	m_stuDefBodyFont.Assign(L"Charis SIL");
	m_stuDefMono.Assign(L"Courier");

	ModuleEntry::ModuleAddRef();

	InitializeBoolMetrics(&m_fUseMetricsFromFont, _T("UseMetricsFromFont"), false);
	InitializeDWORDMetrics((DWORD*)&m_dSuperscriptYOffsetNumerator, _T("SuperscriptYOffsetNumerator"), 1);
	InitializeDWORDMetrics((DWORD*)&m_dSuperscriptYOffsetDenominator, _T("SuperscriptYOffsetDenominator"), 3);
	InitializeDWORDMetrics((DWORD*)&m_dSuperscriptSizeNumerator, _T("SuperscriptSizeNumerator"),  2);
	InitializeDWORDMetrics((DWORD*)&m_dSuperscriptSizeDenominator, _T("SuperscriptSizeDenominator"), 3);
	InitializeDWORDMetrics((DWORD*)&m_dSubscriptYOffsetNumerator, _T("SubscriptYOffsetNumerator"), 1);
	InitializeDWORDMetrics((DWORD*)&m_dSubscriptYOffsetDenominator, _T("SubscriptYOffsetDenominator"), 5);
	InitializeDWORDMetrics((DWORD*)&m_dSubscriptSizeNumerator, _T("SubscriptSizeNumerator"), 2);
	InitializeDWORDMetrics((DWORD*)&m_dSubscriptSizeDenominator, _T("SubscriptSizeDenominator"), 3);
}

WritingSystem::~WritingSystem()
{
	ModuleEntry::ModuleRelease();
}


//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP WritingSystem::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IWritingSystem)
		*ppv = static_cast<IWritingSystem *>(this);
	else if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (&riid == &CLSID_WritingSystem)
		*ppv = static_cast<WritingSystem *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IWritingSystem);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   IWritingSystem Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get the writing system integer that identifies this writing system to classes which
	don't actually need to use its methods

	@param pws
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_WritingSystem(int * pws)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pws);

	*pws = m_hvo;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the writing system integer that identifies this writing system to classes which
	don't actually need to use its methods. Don't even think about doing this after
	we've put the writing system in the writing system factory.

	@param ws
----------------------------------------------------------------------------------------------*/
void WritingSystem::SetHvo(int ws)
{
	if (!ws || (unsigned long)ws > (unsigned long)kwsLim)
		ThrowHr(WarnHr(E_INVALIDARG));
	if (m_hvo && m_hvo != ws)
		ThrowHr(WarnHr(E_INVALIDARG));

	if (m_hvo != ws)
	{
		m_hvo = ws;
		m_fDirty = true;
	}
}

/*----------------------------------------------------------------------------------------------
	Get the name in the given writing system.  Return S_FALSE if the given writing system does not
	have a name assigned.

	@param ws
	@param pbstrName
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_Name(int ws, BSTR * pbstrName)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstrName);

	StrUni stu;
	if (m_hmwsstuName.Retrieve(ws, &stu))
	{
		*pbstrName = SysAllocString(stu.Chars());
		return S_OK;
	}
	else
	{
		*pbstrName = NULL;
		return S_FALSE;
	}
	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the name in the given writing system.

	@param ws
	@param bstrName
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_Name(int ws, BSTR bstrName)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);

	StrUni stuOld;
	bool fHaveOld = m_hmwsstuName.Retrieve(ws, &stuOld);
	if (bstrName && BstrLen(bstrName))
	{
		StrUni stu(bstrName, BstrLen(bstrName));
		if (stu != stuOld)
		{
			m_hmwsstuName.Insert(ws, stu, true);
			m_fDirty = true;
		}
	}
	else if (fHaveOld)
	{
		m_hmwsstuName.Delete(ws);
		m_fDirty = true;
	}
	// Clear the UiName cache so that it reloads. This is debatable, but it keeps current
	// tests working. We'll probably be doing away with the name property one of these days.
	m_stuUiName.Clear();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Get the locale assigned to this writing system.

	@param pnLocale
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_Locale(int * pnLocale)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnLocale);

	*pnLocale = m_nLocale;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the locale for this writing system.

	@param nLocale
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_Locale(int nLocale)
{
	BEGIN_COM_METHOD;

	if (m_nLocale != nLocale)
	{
		m_nLocale = nLocale;
		m_fDirty = true;
	}

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Get the ICU Locale.

	@param pbstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_IcuLocale(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstr);

	if (m_stuIcuLocale.Length())
		m_stuIcuLocale.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Put the ICU Locale.

	@param pbstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_IcuLocale(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuIcuLocale != stu)
	{
		// We need to notify the writing system factory that we are changing the ICULocale so
		// it can update its internal hashmaps.
		LgWritingSystemFactory * pwsf = dynamic_cast<LgWritingSystemFactory *>(m_qwsf.Ptr());
		// We don't need to update the hashmaps if we are setting this for the first time.
		// Also skip if hvo is 0. This currently happens for each character in WsPropsDlg.
		if (m_hvo && m_stuIcuLocale.Length())
			pwsf->ChangingIcuLocale(m_hvo, m_stuIcuLocale.Chars(), stu.Chars());
		m_stuIcuLocale.Assign(stu);
		m_stuUiName.Clear(); // Clear name cache since the name will likely change.
		m_fDirty = true;
	}
	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Get the ICU Locale.

	@param pbstrLanguage
	@param pbstrScript
	@param pbstrCountry
	@param pbstrVariant
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::GetIcuLocaleParts(BSTR * pbstrLanguage,
	BSTR * pbstrScript, BSTR * pbstrCountry, BSTR * pbstrVariant)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstrLanguage);
	ChkComArgPtr(pbstrScript);
	ChkComArgPtr(pbstrCountry);
	ChkComArgPtr(pbstrVariant);

	UErrorCode uec = U_ZERO_ERROR;
	StrUtil::InitIcuDataDir();
	StrAnsi sta = m_stuIcuLocale.Chars();
	StrAnsi staLang;
	StrAnsi staScript;
	StrAnsi staCountry;
	StrAnsi staVar;
	char rgch[MAX_PATH];
	int cch;
	cch = uloc_getLanguage(sta.Chars(), rgch, ULOC_LANG_CAPACITY, &uec);
	staLang.Assign(rgch, cch);
	cch = uloc_getScript(sta.Chars(), rgch, ULOC_SCRIPT_CAPACITY, &uec);
	staScript.Assign(rgch, cch);
	cch = uloc_getCountry(sta.Chars(), rgch, ULOC_COUNTRY_CAPACITY, &uec);
	staCountry.Assign(rgch, cch);
	cch = uloc_getVariant(sta.Chars(), rgch, ULOC_FULLNAME_CAPACITY, &uec);
	staVar.Assign(rgch, cch);
	if (U_FAILURE(uec))
		ThrowHr(E_UNEXPECTED);
	staLang.GetBstr(pbstrLanguage);
	staScript.GetBstr(pbstrScript);
	staCountry.GetBstr(pbstrCountry);
	staVar.GetBstr(pbstrVariant);

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Get the Legacy Mapping name.

	@param pbstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_LegacyMapping(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstr);

	if (m_stuLegacyMapping.Length())
		m_stuLegacyMapping.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Put the Legacy Mapping name.

	@param pbstr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_LegacyMapping(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuLegacyMapping != stu)
	{
		m_stuLegacyMapping.Assign(stu);
		m_fDirty = true;
	}
	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Answer a string converter that can convert data in the other writing system into yours.
	At present no converter classes exist, so we always return null indicating no special
	conversion is required.
	ENHANCE: when we have some real converter classes, do appropriate lookup to find one.

	@param ws
	@param ppstrconv
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_ConverterFrom(int ws, ILgStringConverter ** ppstrconv)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppstrconv);

	*ppstrconv = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Retrieve a converter that can convert data in this writing system into a
	'normalized' form.
	ENHANCE: Decide what operations may assume data is normalized, and how to make sure
	they always in fact get normalized data. Is there actually a need to normalize?

	@param ppstrconv
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_NormalizeEngine(ILgStringConverter ** ppstrconv)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppstrconv);

	Assert(false);
	return E_NOTIMPL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Retrieve a tokenizer that can find words in a string.

	@param pptoker
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_WordBreakEngine(ILgTokenizer ** pptoker)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptoker);

	Assert(false);
	return E_NOTIMPL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Retrieve a spelling dictionary name; if it is empty default to the IcuLocale.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_SpellCheckDictionary(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (m_stuSpellCheckDictionary.Length())
		m_stuSpellCheckDictionary.GetBstr(pbstr);
	else if (m_stuIcuLocale.Length())
		m_stuIcuLocale.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set a spelling dictionary name; not yet persisted.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_SpellCheckDictionary(BSTR bstr)
{
	BEGIN_COM_METHOD;

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuSpellCheckDictionary != stu)
	{
		m_stuSpellCheckDictionary.Assign(stu);
		m_fDirty = true;
	}

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	ENHANCE: is the search engine sufficiently writing-system dependent to belong
	in the WS interface instead?

	@param ppsrcheng
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_SearchEngine(ILgSearchEngine ** ppsrcheng)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsrcheng);

	Assert(false);
	return E_NOTIMPL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	This finds all of the subclasses of LgSpec in the writing system model and executes the
	Compile() method on each one, then stores the resulting Moniker in the Engine
	property of the corresponding LgComponent.
	When some specifications are changed from an Object Viewer, or some other way,
	the writing system can be loaded, this method can be run, and the writing system can be saved.
	This will result in an writing system with valid engines.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::CompileEngines(void)
{
	BEGIN_COM_METHOD;

	Assert(false);
	return E_NOTIMPL;
#if 0
	IClassInitMonikerPtr qcim;
	IBindCtxPtr qbc;
	ICellarUtilitiesPtr qclr;
	IUnknownVectorPtr quvec;
	ILgComponentPtr qcomp;
	IMonikerPtr qmnk;
	ILgSpecPtr qspec;
	int cspec;

	if (FAILED(hr = qclr.CreateInstance(CLSID_CellarUtilities)))
		return WarnHr(hr);
	if (FAILED(hr = quvec.CreateInstance(CLSID_UnknownVector)))
		return WarnHr(hr);

	// ********** Get all subclasses of LgSpec.
	if (FAILED(hr = qclr->GetLinkedObjects(GetInterface(), kgrfcptOwning, true,
		IID_ILgSpec, quvec)))
	{
		return WarnHr(hr);
	}
	if (FAILED(hr = quvec->get_Size(&cspec)))
		return WarnHr(hr);
	// For each spec, compile it and store it in the owning LgComponent.
	for (int ispec = cspec; --ispec >= 0; )
	{
		if (FAILED(hr = quvec->GetItemInterface(ispec, IID_ILgSpec, (void **)&qspec)))
			return WarnHr(hr);
		if (FAILED(hr = qspec->Compile(&qmnk)))
			return WarnHr(hr);
		if (FAILED(hr = qclr->GetObjInOwnershipPathWithIid(qspec, IID_ILgComponent,
			(void**)&qcomp)))
		{
			return WarnHr(hr);
		}
		if (FAILED(hr = qcomp->putref_Engine(qmnk)))
			return WarnHr(hr);
	}

	return S_OK;
#endif

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Get the number of writing systems in which the name of this writing system is stored.

	@param pcws Pointer to an integer for returning the number of writing systems.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_NameWsCount(int * pcws)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcws);

	*pcws = m_hmwsstuName.Size();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Get the (first cws) writing systems in which the name of this writing system is stored.  If
	cws is larger than the number of writing systems, the excess entries in prgws are set to
	zero.

	@param cws Number of entries available in prgws.
	@param prgws Pointer to an array for returning the writing systems.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_NameWss(int cws, int * prgws)
{
	BEGIN_COM_METHOD;
	if (cws < 0)
		ThrowHr(WarnHr(E_INVALIDARG));
	ChkComArrayArg(prgws, cws);

	int iws;
	HashMap<int, StrUni>::iterator it;
	for (iws = 0, it = m_hmwsstuName.Begin(); it != m_hmwsstuName.End(); ++it, ++iws)
	{
		if (iws < cws)
			prgws[iws] = it.GetKey();
		else
			break;
	}
	for ( ; iws < cws; ++iws)
		prgws[iws] = 0;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Check whether this writing system needs to be saved to persistent storage.

	@param pf Pointer to the ComBool for returning whether this writing system is "dirty".
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_Dirty(ComBool * pf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pf);

	if (m_fDirty)
	{
		*pf = TRUE;
		return S_OK;
	}

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the "dirty" bit.

	@param fDirty Flag whether this writing system has changed since being loaded from persistent
		storage, or is newly created.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_Dirty(ComBool fDirty)
{
	BEGIN_COM_METHOD;

	m_fDirty = (bool)fDirty;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Get the factory for this writing system.

	@param ppwsf Address where to store a pointer to the writing system factory that
					stores/produces this writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppwsf);
	AssertPtr(m_qwsf);

	*ppwsf = m_qwsf;
	if (*ppwsf)
		(*ppwsf)->AddRef();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the factory for this writing system.  This also sets the factory for the
	writing system's renderers and collaters.

	@param pwsf Pointer to the writing system factory that stores/produces this writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pwsf);
	// JohnT: The following code defeats the code in LgWritingSystemFactory::Shutdown
	// which passes null to this method in order to help break circular references.
	//if (!pwsf)
	//	return E_INVALIDARG;

	m_qwsf = pwsf;

	if (m_qrenengUni)
		m_qrenengUni->putref_WritingSystemFactory(pwsf);

	ComHashMapStrUni<IRenderEngine>::iterator it;
	for (it = m_hmstureEngines.Begin(); it != m_hmstureEngines.End(); ++it)
	{
		IRenderEnginePtr qre = it.GetValue();
		if (qre.Ptr() != m_qrenengUni.Ptr())
			CheckHr(qre->putref_WritingSystemFactory(pwsf));
	}

	ComVector<ICollation>::iterator itc;
	for (itc = m_vqcoll.Begin(); itc != m_vqcoll.End(); ++itc)
	{
		CheckHr((*itc)->putref_WritingSystemFactory(pwsf));
	}

	if (m_qcoleng)
		m_qcoleng->putref_WritingSystemFactory(pwsf);

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Write this writing system to the stream in standard FieldWorks XML format.

	@param pstrm Pointer to the output stream.
	@param cchIndent Number of spaces to start the indentation.  Zero means no indentation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::WriteAsXml(IStream * pstrm, int cchIndent)
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
	SmartBstr sbstrWs;

	// Write out the ICU locale (language{_region{_variant}}) as the writing system ID
	FormatToStream(pstrm, "%s<LgWritingSystem id=\"", pszIndent);
	CheckHr(m_qwsf->GetStrFromWs(m_hvo, &sbstrWs));
	WriteXmlUnicode(pstrm, sbstrWs.Chars(), sbstrWs.Length());
	// Write out just the language part of the ICU locale
	StrUni stuLang = m_stuIcuLocale.Chars();
	int ich = stuLang.FindStr("_");
	if (ich >= 0)
	{
		stuLang.Replace(ich, stuLang.Length(), "", 0);
	}
	// Write out the "type" (i.e., standard) by which the language is identified.
	StrUni stuLangType;
	switch (stuLang.Length())
	{
	case 2: // 2-letter ICU locale code
		stuLangType = "ISO-639-1";
		break;
	case 3: // 3-letter ICU locale code
		stuLangType = "ISO-639-2";
		break;
	case 4:
		if (stuLang[0] == 'e')  // Old-style Ethnologue code (with e- prefix)
			stuLangType = "SIL";
		else // User couldn't identify this as a known language
		{
			stuLangType = "OTHER";
			SmartBstr sbstrName;
			int wsUI;
			m_qwsf->get_UserWs(&wsUI);
			if (wsUI > 0)
			{
				get_Name(wsUI, &sbstrName);
				if (sbstrName.Length() > 0)
				{
					stuLang.Append(" - ", 3);
					stuLang.Append(sbstrName.Chars(), sbstrName.Length());
				}
			}
		}
		stuLang.Replace(0, 1, "", 0); //remove the "e" or "x" prefix
		break;
	default:
		stuLangType = "OTHER";
		break;
	}
	FormatToStream(pstrm, "\" language=\"");
	WriteXmlUnicode(pstrm, stuLang, stuLang.Length());
	FormatToStream(pstrm, "\" type=\"");
	WriteXmlUnicode(pstrm, stuLangType, stuLangType.Length());
	FormatToStream(pstrm, "\">%n");
	if (m_hmwsstuName.Size())
	{
		FormatToStream(pstrm, "%s<Name24>%n", pszIndent2);
		HashMap<int, StrUni>::iterator it;
		for (it = m_hmwsstuName.Begin(); it != m_hmwsstuName.End(); ++it)
		{
			FormatToStream(pstrm, "%s<AUni ws=\"", pszIndent4);
			CheckHr(m_qwsf->GetStrFromWs(it.GetKey(), &sbstrWs));
			WriteXmlUnicode(pstrm, sbstrWs.Chars(), sbstrWs.Length());
			FormatToStream(pstrm, "\">");
			WriteXmlUnicode(pstrm, it.GetValue().Chars(), it.GetValue().Length());
			FormatToStream(pstrm, "</AUni>%n");
		}
		FormatToStream(pstrm, "%s</Name24>%n", pszIndent2);
	}
	if (m_hmwsstuAbbr.Size())
	{
		FormatToStream(pstrm, "%s<Abbr24>%n", pszIndent2);
		HashMap<int, StrUni>::iterator it;
		for (it = m_hmwsstuAbbr.Begin(); it != m_hmwsstuAbbr.End(); ++it)
		{
			FormatToStream(pstrm, "%s<AUni ws=\"", pszIndent4);
			CheckHr(m_qwsf->GetStrFromWs(it.GetKey(), &sbstrWs));
			WriteXmlUnicode(pstrm, sbstrWs.Chars(), sbstrWs.Length());
			FormatToStream(pstrm, "\">");
			WriteXmlUnicode(pstrm, it.GetValue().Chars(), it.GetValue().Length());
			FormatToStream(pstrm, "</AUni>%n");
		}
		FormatToStream(pstrm, "%s</Abbr24>%n", pszIndent2);
	}
	if (m_hmwsqtssDescr.Size())
	{
		FormatToStream(pstrm, "%s<Description24>%n", pszIndent2);
		ComHashMap<int, ITsString>::iterator it;
		for (it = m_hmwsqtssDescr.Begin(); it != m_hmwsqtssDescr.End(); ++it)
		{
			it.GetValue()->WriteAsXml(pstrm, m_qwsf, cchIndent ? cchIndent + 4 : 0, it.GetKey(),
				FALSE);
		}
		FormatToStream(pstrm, "%s</Description24>%n", pszIndent2);
	}

	if (m_nLocale != 0)
	{
		FormatToStream(pstrm, "%s<Locale24><Integer val=\"%d\"/></Locale24>%n",
			pszIndent2, m_nLocale);
	}
	FormatToStream(pstrm, "%s<RightToLeft24><Boolean val=\"%s\"/></RightToLeft24>%n",
		pszIndent2, m_fRightToLeft ? "true" : "false");

	if (m_stuFontVar.Length())
	{
		FormatToStream(pstrm, "%s<FontVariation24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuFontVar.Chars(), m_stuFontVar.Length());
		FormatToStream(pstrm, "</Uni></FontVariation24>%n");
	}
	if (m_stuSansFontVar.Length())
	{
		FormatToStream(pstrm, "%s<SansFontVariation24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuSansFontVar.Chars(), m_stuSansFontVar.Length());
		FormatToStream(pstrm, "</Uni></SansFontVariation24>%n");
	}
	if (m_stuBodyFontFeatures.Length())
	{
		FormatToStream(pstrm, "%s<BodyFontFeatures24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuBodyFontFeatures.Chars(), m_stuBodyFontFeatures.Length());
		FormatToStream(pstrm, "</Uni></BodyFontFeatures24>%n");
	}
	if (m_stuDefSerif.Length())
	{
		FormatToStream(pstrm, "%s<DefaultSerif24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuDefSerif.Chars(), m_stuDefSerif.Length());
		FormatToStream(pstrm, "</Uni></DefaultSerif24>%n");
	}
	if (m_stuDefSans.Length())
	{
		FormatToStream(pstrm, "%s<DefaultSansSerif24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuDefSans.Chars(), m_stuDefSans.Length());
		FormatToStream(pstrm, "</Uni></DefaultSansSerif24>%n");
	}
	if (m_stuDefBodyFont.Length())
	{
		FormatToStream(pstrm, "%s<DefaultBodyFont24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuDefBodyFont.Chars(), m_stuDefBodyFont.Length());
		FormatToStream(pstrm, "</Uni></DefaultBodyFont24>%n");
	}
	if (m_stuDefMono.Length())
	{
		FormatToStream(pstrm, "%s<DefaultMonospace24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuDefMono.Chars(), m_stuDefMono.Length());
		FormatToStream(pstrm, "</Uni></DefaultMonospace24>%n");
	}
	if (m_stuValidChars.Length())
	{
		FormatToStream(pstrm, "%s<ValidChars24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuValidChars.Chars(), m_stuValidChars.Length());
		FormatToStream(pstrm, "</Uni></ValidChars24>%n");
	}
	if (m_stuMatchedPairs.Length())
	{
		FormatToStream(pstrm, "%s<MatchedPairs24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuMatchedPairs.Chars(), m_stuMatchedPairs.Length());
		FormatToStream(pstrm, "</Uni></MatchedPairs24>%n");
	}
	if (m_stuPunctuationPatterns.Length())
	{
		FormatToStream(pstrm, "%s<PunctuationPatterns24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuPunctuationPatterns.Chars(), m_stuPunctuationPatterns.Length());
		FormatToStream(pstrm, "</Uni></PunctuationPatterns24>%n");
	}
	if (m_stuCapitalizationInfo.Length())
	{
		FormatToStream(pstrm, "%s<CapitalizationInfo24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuCapitalizationInfo.Chars(), m_stuCapitalizationInfo.Length());
		FormatToStream(pstrm, "</Uni></CapitalizationInfo24>%n");
	}
	if (m_stuQuotationMarks.Length())
	{
		FormatToStream(pstrm, "%s<QuotationMarks24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuQuotationMarks.Chars(), m_stuQuotationMarks.Length());
		FormatToStream(pstrm, "</Uni></QuotationMarks24>%n");
	}
	if (m_stuIcuLocale.Length())
	{
		FormatToStream(pstrm, "%s<ICULocale24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuIcuLocale.Chars(), m_stuIcuLocale.Length());
		FormatToStream(pstrm, "</Uni></ICULocale24>%n");
	}
	if (m_stuSpellCheckDictionary.Length())
	{
		FormatToStream(pstrm, "%s<SpellCheckDictionary24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuSpellCheckDictionary.Chars(), m_stuSpellCheckDictionary.Length());
		FormatToStream(pstrm, "</Uni></SpellCheckDictionary24>%n");
	}

	FormatToStream(pstrm, "%s<KeyboardType24><Uni>%s</Uni></KeyboardType24>%n",
		pszIndent2, m_fKeyMan ? "keyman" : "standard");

	if (m_stuKeymanKbdName.Length())
	{
		FormatToStream(pstrm, "%s<KeymanKeyboard24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuKeymanKbdName.Chars(), m_stuKeymanKbdName.Length());
		FormatToStream(pstrm, "</Uni></KeymanKeyboard24>%n");
	}

	if (m_stuLegacyMapping.Length())
	{
		FormatToStream(pstrm, "%s<LegacyMapping24><Uni>", pszIndent2);
		WriteXmlUnicode(pstrm, m_stuLegacyMapping.Chars(), m_stuLegacyMapping.Length());
		FormatToStream(pstrm, "</Uni></LegacyMapping24>%n");
	}

	if (m_vqcoll.Size())
	{
		FormatToStream(pstrm, "%s<Collations24>%n", pszIndent2);
		for (int icoll = 0; icoll < m_vqcoll.Size(); ++icoll)
		{
			CheckHr(m_vqcoll[icoll]->WriteAsXml(pstrm, cchIndent ? cchIndent + 4 : 0));
		}
		FormatToStream(pstrm, "%s</Collations24>%n", pszIndent2);
	}

	FormatToStream(pstrm, "%s</LgWritingSystem>%n", pszIndent);

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

void WriteStringToStream(IStream * pstrm, StrUni & stu)
{
	int cch = stu.Length();
	ULONG cb = isizeof(cch);
	ULONG cbWritten;
	CheckHr(pstrm->Write(&cch, cb, &cbWritten));
	cb = cch * isizeof(OLECHAR);
	CheckHr(pstrm->Write(stu.Chars(), cb, &cbWritten));
}
/*----------------------------------------------------------------------------------------------
	Write this writing system to the IStorage object to allow a read-only copy to be
	reconstituted later.

	@param pstg Pointer to the IStorage object used to store the writing system's data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::Serialize(IStorage * pstg)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstg);

	IStreamPtr qstrm;
	CheckHr(pstg->CreateStream(L"Data", STGM_READWRITE | STGM_SHARE_EXCLUSIVE, 0, 0, &qstrm));

	//	  Flid	Type			DstCls			Name
	//	  -----	----			------			----
	//  1 		Integer							Id
	//  2 24003	Integer							Locale
	//  3 24001	MultiUnicode					Name
	//  4 24006	MultiUnicode					Abbr
	//  5 24015	Boolean							RightToLeft
	//  6 24012	Unicode							FontVariation
	//  7 24024	Unicode							SansFontVariation
	//  8 24027	Unicode							BodyFontFeatures
	//  9 24011	Unicode							DefaultSerif
	// 10 24010	Unicode							DefaultSansSerif
	// 11 24026	Unicode							DefaultBodyFont
	// 12 24009	Unicode							DefaultMonospace
	// 13 24028	BigUnicode						ValidChars
	// 14 24030	BigUnicode						MatchedPairs
	// 15 24031	BigUnicode						PunctuationPatterns
	// 16 24032	BigUnicode						CapitalizationInfo
	// 17 24033	BigUnicode						QuotationMarks
	// 18 24021	Unicode							ICULocale
	// 19 24029	Unicode							Spelling dictionary
	// 20 24023	Unicode							LegacyMapping
	// 21 24013	Unicode							KeyboardType
	// 22 24022	Unicode							KeymanKeyboard
	// 23 24020	MultiString						Description
	// 24 24018	OwningSequence	LgCollation		Collations

	ULONG cb;
	ULONG cbWritten;

	// Id and Windows Locale (LCID?)
	cb = isizeof(m_hvo);
	CheckHr(qstrm->Write(&m_hvo, cb, &cbWritten));
	cb = isizeof(m_nLocale);
	CheckHr(qstrm->Write(&m_nLocale, cb, &cbWritten));

	// Multilingual Name
	int cName = m_hmwsstuName.Size();
	cb = isizeof(cName);
	CheckHr(qstrm->Write(&cName, cb, &cbWritten));
	HashMap<int, StrUni>::iterator ithm;
	for (ithm = m_hmwsstuName.Begin(); ithm != m_hmwsstuName.End(); ++ithm)
	{
		int ws = ithm.GetKey();
		StrUni & stuName = ithm.GetValue();
		cb = isizeof(ws);
		CheckHr(qstrm->Write(&ws, cb, &cbWritten));
		WriteStringToStream(qstrm, stuName);
	}

	// Multilingual Abbreviation
	int cAbbr = m_hmwsstuAbbr.Size();
	cb = isizeof(cAbbr);
	CheckHr(qstrm->Write(&cAbbr, cb, &cbWritten));
	for (ithm = m_hmwsstuAbbr.Begin(); ithm != m_hmwsstuAbbr.End(); ++ithm)
	{
		int ws = ithm.GetKey();
		StrUni & stuAbbr = ithm.GetValue();
		cb = isizeof(ws);
		CheckHr(qstrm->Write(&ws, cb, &cbWritten));
		WriteStringToStream(qstrm, stuAbbr);
	}

	// Serializing data members transferred to this class from OldWritingSystem
	// Right to left flag.
	int fRTL = m_fRightToLeft;
	cb = isizeof(fRTL);
	CheckHr(qstrm->Write(&fRTL, cb, &cbWritten));

	// font stuff
	WriteStringToStream(qstrm, m_stuFontVar);
	WriteStringToStream(qstrm, m_stuSansFontVar);
	WriteStringToStream(qstrm, m_stuBodyFontFeatures);
	WriteStringToStream(qstrm, m_stuDefSerif);
	WriteStringToStream(qstrm, m_stuDefSans);
	WriteStringToStream(qstrm, m_stuDefBodyFont);
	WriteStringToStream(qstrm, m_stuDefMono);
	WriteStringToStream(qstrm, m_stuValidChars);
	WriteStringToStream(qstrm, m_stuMatchedPairs);
	WriteStringToStream(qstrm, m_stuPunctuationPatterns);
	WriteStringToStream(qstrm, m_stuCapitalizationInfo);
	WriteStringToStream(qstrm, m_stuQuotationMarks);

	// ICU Locale.
	WriteStringToStream(qstrm, m_stuIcuLocale);

	// Spelling dict
	WriteStringToStream(qstrm, m_stuSpellCheckDictionary);

	// Legacy Mapping name.
	WriteStringToStream(qstrm, m_stuLegacyMapping);

	// Keyman stuff.
	int fKM = m_fKeyMan;
	cb = isizeof(fKM);
	CheckHr(qstrm->Write(&fKM, cb, &cbWritten));

	WriteStringToStream(qstrm, m_stuKeymanKbdName);

	// Multilingual description strings.
	ComHashMap<int, ITsString>::iterator itD;
	IStoragePtr qstgDesc;
	for (itD =  m_hmwsqtssDescr.Begin(); itD != m_hmwsqtssDescr.End(); ++itD)
	{
		int ws = itD.GetKey();
		StrUni stu;
		stu.Format(L"ws.%d", ws);
		CheckHr(pstg->CreateStorage(stu.Chars(), STGM_READWRITE | STGM_SHARE_EXCLUSIVE, 0, 0,
			&qstgDesc));
		ITsStringPtr qtss = itD.GetValue();
		// Serialize the string in two streams, "Text" and "Fmt".
		IStreamPtr qstrmText;
		CheckHr(qstgDesc->CreateStream(L"Text", STGM_READWRITE | STGM_SHARE_EXCLUSIVE, 0, 0,
			&qstrmText));
		SmartBstr sbstr;
		CheckHr(qtss->get_Text(&sbstr));
		ULONG cb = BstrSize(sbstr);
		ULONG cbWritten;
		CheckHr(qstrmText->Write(sbstr.Chars(), cb, &cbWritten));
		if (cb != cbWritten)
			ThrowHr(WarnHr(E_UNEXPECTED));
		IStreamPtr qstrmFmt;
		CheckHr(qstgDesc->CreateStream(L"Fmt", STGM_READWRITE | STGM_SHARE_EXCLUSIVE, 0, 0,
			&qstrmFmt));
		CheckHr(qtss->SerializeFmt(qstrmFmt));
	}
	// Collations.
	IStoragePtr qstgColl;
	for (int icoll = 0; icoll < m_vqcoll.Size(); ++icoll)
	{
		StrUni stu;
		stu.Format(L"Collation-%02d", icoll);
		CheckHr(pstg->CreateStorage(stu.Chars(), STGM_READWRITE | STGM_SHARE_EXCLUSIVE, 0, 0,
			&qstgColl));
		CheckHr(m_vqcoll[icoll]->Serialize(qstgColl));
	}

	CheckHr(qstrm->Commit(STGC_DEFAULT));

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

void ReadStringFromStorage(IStream * pstrm, StrUni & stu)
{
	int cch;
	ULONG cb = isizeof(cch);
	ULONG cbRead;
	CheckHr(pstrm->Read(&cch, cb, &cbRead));
	if (cch)
	{
		OLECHAR * pch;
		stu.SetSize(cch, &pch);
		cb = cch * isizeof(OLECHAR);
		CheckHr(pstrm->Read(pch, cb, &cbRead));
		if (cb != cbRead)
			ThrowHr(E_UNEXPECTED);
	}
	else
	{
		stu.Clear();		// may be unnecessary
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize this writing system from the IStorage object which contains data from an writing
	system that had been initialized earlier.

	@param pstg Pointer to the IStorage object used to store the writing system's data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::Deserialize(IStorage * pstg)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstg);

	IStreamPtr qstrm;
	CheckHr(pstg->OpenStream(L"Data", NULL, STGM_READ | STGM_SHARE_EXCLUSIVE, 0, &qstrm));

	//	  Flid	Type			DstCls			Name
	//	  -----	----			------			----
	//  1 		Integer							Id
	//  2 24003	Integer							Locale
	//  3 24001	MultiUnicode					Name
	//  4 24006	MultiUnicode					Abbr
	//  5 24015	Boolean							RightToLeft
	//  6 24012	Unicode							FontVariation
	//  7 24024	Unicode							SansFontVariation
	//  8 24027	Unicode							BodyFontFeatures
	//  9 24011	Unicode							DefaultSerif
	// 10 24010	Unicode							DefaultSansSerif
	// 11 24026	Unicode							DefaultBodyFont
	// 12 24009	Unicode							DefaultMonospace
	// 13 24028	BigUnicode						ValidChars
	// 14 24030	BigUnicode						MatchedPairs
	// 15 24031	BigUnicode						PunctuationPatterns
	// 16 24032	BigUnicode						CapitalizationInfo
	// 17 24033	BigUnicode						QuotationMarks
	// 18 24021	Unicode							ICULocale
	// 19 24029	Unicode							Spelling dictionary
	// 20 24023	Unicode							LegacyMapping
	// 21 24013	Unicode							KeyboardType
	// 22 24022	Unicode							KeymanKeyboard
	// 23 24020	MultiString						Description
	// 24 24018	OwningSequence	LgCollation		Collations

	ULONG cb;
	ULONG cbRead;

	// Id and Windows Locale (LCID?)
	cb = isizeof(m_hvo);
	CheckHr(qstrm->Read(&m_hvo, cb, &cbRead));
	if (cb != cbRead)
		ThrowHr(E_UNEXPECTED);
	cb = isizeof(m_nLocale);
	CheckHr(qstrm->Read(&m_nLocale, cb, &cbRead));
	if (cb != cbRead)
		ThrowHr(E_UNEXPECTED);

	// Multilingual name.
	int cName;
	cb = isizeof(cName);
	CheckHr(qstrm->Read(&cName, cb, &cbRead));
	if (cb != cbRead)
		ThrowHr(E_UNEXPECTED);
	int i;
	int ws;
	int cch;
	Vector<OLECHAR> vch;
	StrUni stuName;
	for (i = 0; i < cName; ++i)
	{
		cb = isizeof(ws);
		CheckHr(qstrm->Read(&ws, cb, &cbRead));
		if (cb != cbRead)
			ThrowHr(E_UNEXPECTED);
		cb = isizeof(cch);
		CheckHr(qstrm->Read(&cch, cb, &cbRead));
		if (cb != cbRead)
			ThrowHr(E_UNEXPECTED);
		vch.Resize(cch);
		cb = cch * isizeof(OLECHAR);
		CheckHr(qstrm->Read(vch.Begin(), cb, &cbRead));
		if (cb != cbRead)
			ThrowHr(E_UNEXPECTED);
		stuName.Assign(vch.Begin(), cch);
		m_hmwsstuName.Insert(ws, stuName, true);
	}
	// Multilingual abbreviation.
	int cAbbr;
	cb = isizeof(cAbbr);
	CheckHr(qstrm->Read(&cAbbr, cb, &cbRead));
	if (cb != cbRead)
		ThrowHr(E_UNEXPECTED);
	StrUni stuAbbr;
	for (i = 0; i < cAbbr; ++i)
	{
		cb = isizeof(ws);
		CheckHr(qstrm->Read(&ws, cb, &cbRead));
		if (cb != cbRead)
			ThrowHr(E_UNEXPECTED);
		ReadStringFromStorage(qstrm, stuAbbr);
		m_hmwsstuAbbr.Insert(ws, stuAbbr, true);
	}

	// Deserializing data members transferred to this class from OldWritingSystem
	// Right to left flag
	int fRTL;
	cb = isizeof(fRTL);
	CheckHr(qstrm->Read(&fRTL, cb, &cbRead));
	m_fRightToLeft = fRTL;

	// font stuff
	ReadStringFromStorage(qstrm, m_stuFontVar);
	ReadStringFromStorage(qstrm, m_stuSansFontVar);
	ReadStringFromStorage(qstrm, m_stuBodyFontFeatures);
	ReadStringFromStorage(qstrm, m_stuDefSerif);
	ReadStringFromStorage(qstrm, m_stuDefSans);
	ReadStringFromStorage(qstrm, m_stuDefBodyFont);
	ReadStringFromStorage(qstrm, m_stuDefMono);

	// Valid Characters
	ReadStringFromStorage(qstrm, m_stuValidChars);

	// Matched pairs
	ReadStringFromStorage(qstrm, m_stuMatchedPairs);

	// Punctuation patterns
	ReadStringFromStorage(qstrm, m_stuPunctuationPatterns);

	// Capitalization Information
	ReadStringFromStorage(qstrm, m_stuCapitalizationInfo);

	// Quotation Marks
	ReadStringFromStorage(qstrm, m_stuQuotationMarks);

	// ICU Locale.
	ReadStringFromStorage(qstrm, m_stuIcuLocale);

	// Spelling dictionary.
	ReadStringFromStorage(qstrm, m_stuSpellCheckDictionary);

	// Legacy Mapping name.
	ReadStringFromStorage(qstrm, m_stuLegacyMapping);

	// Keyman stuff.
	int fKM;
	cb = isizeof(fKM);
	CheckHr(qstrm->Read(&fKM, cb, &cbRead));
	m_fKeyMan = fKM;

	ReadStringFromStorage(qstrm, m_stuKeymanKbdName);

	// Multilingual description strings and Collations.
	ULONG celt;
	IEnumSTATSTGPtr qenum;
	STATSTG statstg;
	IStoragePtr qstgDesc;
	IStoragePtr qstgColl;
	IStreamPtr qstrmText;
	IStreamPtr qstrmFmt;
	CheckHr(pstg->EnumElements(0, NULL, 0, &qenum));
	memset(&statstg, 0, sizeof(statstg));
	CheckHr(qenum->Next(1, &statstg, &celt));
	ITsStrFactoryPtr qtsf;
	ITsStringPtr qtss;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	while (celt == 1)
	{
		StrUni stu(statstg.pwcsName);
		::CoTaskMemFree(statstg.pwcsName);
		statstg.pwcsName = NULL;
		if (statstg.type == STGTY_STORAGE)
		{
			if (wcsncmp(stu.Chars(), L"Collation-", 10) == 0)
			{
				CheckHr(pstg->OpenStorage(stu.Chars(), NULL, STGM_READ | STGM_SHARE_EXCLUSIVE,
					NULL, 0, &qstgColl));
				if (!qstgColl)
					ThrowHr(E_UNEXPECTED);
				wchar * pszT;
				int icoll = wcstol(stu.Chars() + 10, &pszT, 10);
				Assert(icoll >= 0);
				if (icoll >= m_vqcoll.Size())
					m_vqcoll.Resize(icoll + 1);
				if (m_vqcoll[icoll])
					ThrowHr(E_UNEXPECTED);
				Collation::CreateCom(NULL, IID_ICollation, (void **)&m_vqcoll[icoll]);
				if (!m_vqcoll[icoll].Ptr())
					ThrowHr(WarnHr(E_UNEXPECTED));
				CheckHr(m_vqcoll[icoll]->Deserialize(qstgColl));
			}
			else if (wcsncmp(stu.Chars(), L"ws.", 3) == 0)
			{
				CheckHr(pstg->OpenStorage(stu.Chars(), NULL, STGM_READ | STGM_SHARE_EXCLUSIVE,
					NULL, 0, &qstgDesc));
				if (!qstgDesc)
					ThrowHr(E_UNEXPECTED);
				wchar * pszT;
				ws = wcstol(stu.Chars() + 3, &pszT, 10);
				Assert(pszT == stu.Chars() + stu.Length());
				Assert(!*pszT);
				// Read the "Text" and "Fmt" streams, and combine them to form a TsString.
				CheckHr(qstgDesc->OpenStream(L"Text", NULL, STGM_READ | STGM_SHARE_EXCLUSIVE, 0,
					&qstrmText));
				CheckHr(qstgDesc->OpenStream(L"Fmt", NULL, STGM_READ | STGM_SHARE_EXCLUSIVE, 0,
					&qstrmFmt));
				CheckHr(qtsf->DeserializeStringStreams(qstrmText, qstrmFmt, &qtss));
				m_hmwsqtssDescr.Insert(ws, qtss, true);
			}
		}
		memset(&statstg, 0, sizeof(statstg));
		CheckHr(qenum->Next(1, &statstg, &celt));
	}
	// Ensure that there are no empty slots in the vector of collations.
	for (int icoll = 0; icoll < m_vqcoll.Size(); ++icoll)
	{
		if (!m_vqcoll[icoll])
		{
			m_vqcoll.Delete(icoll);
			--icoll;
		}
	}

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Primary direction, used for complex embedding; may have fragments like numbers
	that go the other way internally.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_RightToLeft(ComBool * pfRightToLeft)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRightToLeft);

	*pfRightToLeft = m_fRightToLeft;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the primary direction.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_RightToLeft(ComBool fRightToLeft)
{
	BEGIN_COM_METHOD;

	bool fRTL = (bool)fRightToLeft;
	if (m_fRightToLeft != fRTL)
	{
		m_fRightToLeft = fRTL;
		m_fDirty = true;
	}
	ClearRenderers();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	See if a font name is one of our magic ones and if so replace with the corresponding real
	one.
----------------------------------------------------------------------------------------------*/
bool WritingSystem::ReplaceChrpFontName(LgCharRenderProps * pchrp)
{
	pchrp->szFaceName[31] = 0;	// ensure NUL termination.
	if (wcscmp(pchrp->szFaceName, DefaultSerif) == 0 ||
		wcscmp(pchrp->szFaceName, g_pszDefaultFont) == 0)
	{
		wcsncpy_s(pchrp->szFaceName, m_stuDefSerif, 31);
		return true;
	}
	else if (wcscmp(pchrp->szFaceName, DefaultSans) == 0 ||
			 wcscmp(pchrp->szFaceName, g_pszDefaultHeadingFont) == 0)
	{
		wcsncpy_s(pchrp->szFaceName, m_stuDefSans, 31);
		return true;
	}
	else if (wcscmp(pchrp->szFaceName, DefaultMono) == 0 ||
			 wcscmp(pchrp->szFaceName, g_pszDefaultFixed) == 0)
	{
		wcsncpy_s(pchrp->szFaceName, m_stuDefMono, 31);
		return true;
	}
	else if (wcscmp(pchrp->szFaceName, DefaultBodyFont) == 0)
	{
		wcsncpy_s(pchrp->szFaceName, m_stuDefBodyFont, 31);
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Get the engine used to render text with the specified properties. At present only
	font, bold, and italic properties are significant.
	Font name may be '<default>' which produces a renderer suitable for the default font.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_Renderer(IVwGraphics * pvg, IRenderEngine ** ppreneng)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppreneng);
	ChkComArgPtr(pvg);
	LgCharRenderProps chrp;
	pvg->get_FontCharProperties(&chrp);

	IRenderEnginePtr qreneng;

	// See if we already have an engine for it.
	// Make a key out of the face name, plus one character which we set to
	// a representation of the bold and italic flags.
	int cchName = wcslen(chrp.szFaceName);
	StrUni stuKey(chrp.szFaceName, cchName + 1);
	OLECHAR chBI = 0x20; // arbitrary, ensures not null, make sure low two bits o.
	if (chrp.ttvBold == kttvForceOn)
		chBI |= 1;
	if (chrp.ttvItalic == kttvForceOn)
		chBI |= 2;
	stuKey.SetAt(cchName, chBI);
	if (m_hmstureEngines.Retrieve(stuKey, qreneng))
	{
		// Got it!
		*ppreneng = qreneng.Detach();
		return S_OK;
	}
	StrUni stuKey2;
	if (ReplaceChrpFontName(&chrp))
	{
		pvg->SetupGraphics(&chrp);
		int cchName2 = wcslen(chrp.szFaceName);
		stuKey2.Append(chrp.szFaceName, cchName2 + 1);
		stuKey2.SetAt(cchName2, chBI);
		if (m_hmstureEngines.Retrieve(stuKey2, qreneng))
		{
			// We already have one for the substitute font. Note it for future reference...
			m_hmstureEngines.Insert(stuKey, qreneng);
			// ...and return it.
			*ppreneng = qreneng.Detach();
			return S_OK;
		}
	}

	// OK, we need a new engine (or possibly not so new, if Uniscribe...)
	// See whether we have Graphite info in the font.
	// Enhance JohnT: this would be the place to add an option to rule out Graphite...
	bool fGraphite = false;
	if (gr::GrUtil::FontHasGraphiteTables(pvg))
	{
		// Good candidate for a Graphite font.
		qreneng.CreateInstance(CLSID_FwGrEngine);
		CheckHr(qreneng->putref_WritingSystemFactory(m_qwsf));

		// Initialize the engine.
		// If it is the default font, initialize the engine's features.
		HRESULT hr;
		if (wcscmp(m_stuDefSerif.Chars(), chrp.szFaceName) == 0)
		{
			IgnoreHr(hr = qreneng->InitRenderer(pvg, m_stuFontVar.Bstr()));
		}
		else if (wcscmp(m_stuDefSans.Chars(), chrp.szFaceName) == 0)
		{
			IgnoreHr(hr = qreneng->InitRenderer(pvg, m_stuSansFontVar.Bstr()));
		}
		else if (wcscmp(m_stuDefBodyFont.Chars(), chrp.szFaceName) == 0)
		{
			IgnoreHr(hr = qreneng->InitRenderer(pvg, m_stuBodyFontFeatures.Bstr()));
		}
		else
		{
			// Otherwise, we don't know any default features. They can still be
			// controlled by style or direct formatting.
			IgnoreHr(hr = qreneng->InitRenderer(pvg, NULL));
		}
		if (FAILED(hr))
		{
			// Don't use Graphite, use Uniscribe.
			Warn("Could not load Graphite font");
		}
		else
		{
			fGraphite = true;
			ITraceControlPtr qtc;
			CheckHr(qreneng->QueryInterface(IID_ITraceControl, (void **)&qtc));
			Assert(qtc);
			CheckHr(qtc->SetTracing(m_nTraceSetting));
		}
	}

	if (!fGraphite)
	{
		// Not a Graphite font, or there was some error in loading it.
		if (!m_qrenengUni)
		{
			UniscribeEngine::CreateCom(NULL, IID_IRenderEngine, (void **)&m_qrenengUni);
			if (!m_qrenengUni)
				ThrowHr(WarnHr(E_UNEXPECTED));
			CheckHr(m_qrenengUni->putref_WritingSystemFactory(m_qwsf));
		}
		qreneng = m_qrenengUni;
	}
	// By this point we should have a renderer, somehow.
	Assert(qreneng.Ptr());
	m_hmstureEngines.Insert(stuKey, qreneng);
	if (stuKey2.Length() > 0)
		m_hmstureEngines.Insert(stuKey2, qreneng);

	*ppreneng = qreneng.Detach();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Return the default font variation string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_FontVariation(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (m_stuFontVar.Length())
		m_stuFontVar.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Set the default font variation string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_FontVariation(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuFontVar != stu)
	{
		m_stuFontVar.Assign(stu);
		m_fDirty = true;
	}
	ClearRenderers();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Return the heading font variation string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_SansFontVariation(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (m_stuSansFontVar.Length())
		m_stuSansFontVar.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Set the heading font variation string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_SansFontVariation(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuSansFontVar != stu)
	{
		m_stuSansFontVar.Assign(stu);
		m_fDirty = true;
	}
	ClearRenderers();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Return the body font variation string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_BodyFontFeatures(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (m_stuBodyFontFeatures.Length())
		m_stuBodyFontFeatures.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Set the body font variation string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_BodyFontFeatures(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuBodyFontFeatures != stu)
	{
		m_stuBodyFontFeatures.Assign(stu);
		m_fDirty = true;
	}
	ClearRenderers();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Return the default font
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_DefaultSerif(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	m_stuDefSerif.GetBstr(pbstr);

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the default font.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_DefaultSerif(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuDefSerif != stu)
	{
		m_stuDefSerif.Assign(stu);
		m_fDirty = true;
	}
	ClearRenderers();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Return the default heading font
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_DefaultSansSerif(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	m_stuDefSans.GetBstr(pbstr);

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the default heading font.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_DefaultSansSerif(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuDefSans != stu)
	{
		m_stuDefSans.Assign(stu);
		m_fDirty = true;
	}
	ClearRenderers();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Return the default body font
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_DefaultBodyFont(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	m_stuDefBodyFont.GetBstr(pbstr);

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the default body font.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_DefaultBodyFont(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuDefBodyFont != stu)
	{
		m_stuDefBodyFont.Assign(stu);
		m_fDirty = true;
	}
	ClearRenderers();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Return the default fixed width font.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_DefaultMonospace(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (m_stuDefMono.Length())
		m_stuDefMono.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the default fixed width font.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_DefaultMonospace(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuDefMono != stu)
	{
		m_stuDefMono.Assign(stu);
		m_fDirty = true;
	}
	ClearRenderers();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Return a string containing the Valid Characters for the writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_ValidChars(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (m_stuValidChars.Length())
		m_stuValidChars.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the string containing the Valid Characters for the writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_ValidChars(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuValidChars != stu)
	{
		m_stuValidChars.Assign(stu);
		m_fDirty = true;
		m_qcpe.Clear(); // get another one when next needed, based on new list.
	}
	ClearRenderers();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Return a string containing the matched pairs for the writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_MatchedPairs(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (m_stuMatchedPairs.Length())
		m_stuMatchedPairs.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the string containing the matched pairs for the writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_MatchedPairs(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuMatchedPairs != stu)
	{
		m_stuMatchedPairs.Assign(stu);
		m_fDirty = true;
	}
	ClearRenderers();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Return a string containing the punctuation patterns for the writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_PunctuationPatterns(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (m_stuPunctuationPatterns.Length())
		m_stuPunctuationPatterns.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the string containing the punctuation patterns for the writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_PunctuationPatterns(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuPunctuationPatterns != stu)
	{
		m_stuPunctuationPatterns.Assign(stu);
		m_fDirty = true;
	}
	ClearRenderers();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Return a string containing the capitalization information for the writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_CapitalizationInfo(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (m_stuCapitalizationInfo.Length())
		m_stuCapitalizationInfo.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the string containing the capitalization information for the writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_CapitalizationInfo(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuCapitalizationInfo != stu)
	{
		m_stuCapitalizationInfo.Assign(stu);
		m_fDirty = true;
	}
	ClearRenderers();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Return a string containing the quotation marks for the writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_QuotationMarks(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (m_stuQuotationMarks.Length())
		m_stuQuotationMarks.GetBstr(pbstr);
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the string containing the quotation marks for the writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_QuotationMarks(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuQuotationMarks != stu)
	{
		m_stuQuotationMarks.Assign(stu);
		m_fDirty = true;
	}
	ClearRenderers();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Return a flag indicating whether this writing system uses KeyMan for its input.
	TODO 1437 (SharonC): Remove when we make the keyboard engine into a separate class.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_KeyMan(ComBool * pfKeyMan)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfKeyMan);

	*pfKeyMan = m_fKeyMan;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set a flag indicating whether this writing system uses KeyMan for its input.
	TODO 1437 (SharonC): Remove when we make the keyboard engine into a separate class.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_KeyMan(ComBool fKeyMan)
{
	BEGIN_COM_METHOD;

	bool fT = (bool)fKeyMan;
	if (m_fKeyMan != fT)
	{
		m_fKeyMan = fT;
		m_fDirty = true;
	}

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Return the string that is appropriate to use in UI widgets. Since the ICU name is often
	different than the Ethnologue name, and the user may choose their own name to override
	the Ethnologue name, we'll try to show the database name first, and failing that, we'll
	try to get a name from ICU. Otherwise we'll use the database abbreviation or the actual
	IcuLocale abbreviation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_UiName(int ws, BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	// Try to show the name from the database.
	StrUni stuRet;
	m_hmwsstuName.Retrieve(ws, &stuRet);
	if (stuRet.Length())
	{
		stuRet.GetBstr(pbstr);
		return S_OK;
	}

	// Try to show the ICU locale display name.
	if (m_stuIcuLocale.Length())
	{
		// Note, the ICU method below memory maps icudt26l_root.res, which means InstallLanguage
		// will fail if called from another process when it tries to update icudt26l_root.res,
		// etc. For example, with Notebook open, if we try to open list editor on another
		// project that has an uninstalled language, it will fail. This really should be rare
		// for end users, so maybe we should leave it this way rather than taking the risk of
		// cleanup possibly wiping out some other ICU function being used elsewhere in the
		// program. Also, a cached name makes it more difficult to update if a person changes
		// the name of a language -- again very rare.
		if (m_stuUiName.Length())
		{
			m_stuUiName.GetBstr(pbstr);
			return S_OK;
		}
		else
		{
			int cch;
			SmartBstr sbstr;
			UChar rgch[MAX_PATH];
			UErrorCode uec = U_ZERO_ERROR;
			StrAnsiBufSmall stabLoc(m_stuIcuLocale.Chars());
			CheckHr(m_qwsf->GetStrFromWs(ws, &sbstr));
			StrAnsiBuf stabUser(sbstr.Chars());
			cch = uloc_getDisplayName(stabLoc.Chars(), stabUser.Chars(), rgch, MAX_PATH, &uec);
			// Get rid of any memory-mapping of files by ICU, and reinitialize ICU.
			IIcuCleanupManagerPtr qicln;
			qicln.CreateInstance(CLSID_IcuCleanupManager);
			CheckHr(qicln->Cleanup());
			// If stabLoc is an illegal locale, uloc_getDisplayName returns the same string
			// forced to lowercase. In that case, we want to try for another name.
			bool fEq = m_stuIcuLocale.EqualsCI(rgch, cch);
			if (!fEq && U_SUCCESS(uec))
			{
				m_stuUiName.Assign(rgch, cch);
				m_stuUiName.GetBstr(pbstr);
				return S_OK;
			}
		}
		// Or show the abbreviation.
		m_hmwsstuAbbr.Retrieve(ws, &stuRet);
		if (stuRet.Length())
		{
			stuRet.GetBstr(pbstr);
		}
		else
		{
			// Or show the Icu Locale abbreviation from the database.
			m_stuIcuLocale.GetBstr(pbstr);
		}
	}

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Get the number of collations defined for this writing system.

	@param pccoll Pointer to the integer for returning the value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_CollationCount(int * pccoll)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pccoll);

	*pccoll = m_vqcoll.Size();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Get the collation at the given (zero-based) index.  If icoll is out of range, the error
	E_INVALIDARG is returned.

	Note that the first collation (index = 0) is the default one, if we need a default
	collation.

	@param icoll Index of the desired collation.
	@param ppcoll Address of the pointer for returning the desired collation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_Collation(int icoll, ICollation ** ppcoll)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppcoll);

	if (icoll < 0 || icoll >= m_vqcoll.Size())
		return E_INVALIDARG;
	*ppcoll = m_vqcoll[icoll];
	AssertPtr(*ppcoll);
	(*ppcoll)->AddRef();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Store the collation at the given (zero-based) index.

	If icoll is out of range, the error E_INVALIDARG is returned.  Note that the range for
	storing is one greater than the range for retrieving a collation: it is permissible to add
	a new collation at the end of the ordered array of collations.

	If a collation with the same code, but a different index, already exists, then the error
	E_INVALIDARG is returned.

	If pcoll is NULL, then the error E_POINTER is returned.

	@param icoll Index of this collation.
	@param pcoll Pointer to the collation to store, or NULL to remove the indexed collation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::putref_Collation(int icoll, ICollation * pcoll)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pcoll);
	if (icoll < 0 || icoll > m_vqcoll.Size())
		return E_INVALIDARG;

	if (icoll < m_vqcoll.Size())
	{
		// ENHANCE: Is it worth checking for equality before setting dirty bit?
		m_vqcoll[icoll] = pcoll;
	}
	else
	{
		m_vqcoll.Push(pcoll);
	}
	CheckHr(pcoll->putref_WritingSystemFactory(m_qwsf));
	m_fDirty = true;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Remove the collation at the given (zero-based) index.

	If icoll is out of range, the error E_INVALIDARG is returned.

	@param icoll Index of this collation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::RemoveCollation(int icoll)
{
	BEGIN_COM_METHOD;
	if (icoll < 0 || icoll >= m_vqcoll.Size())
		return E_INVALIDARG;

	m_vqcoll.Delete(icoll);

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Return the abbreviated name in the given writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_Abbr(int ws, BSTR * pbstrAbbr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrAbbr);

	StrUni stu;
	if (m_hmwsstuAbbr.Retrieve(ws, &stu))
	{
		*pbstrAbbr = SysAllocString(stu.Chars());
		return S_OK;
	}
	else
	{
		*pbstrAbbr = NULL;
		return S_FALSE;
	}

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the abbreviated name in the given writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_Abbr(int ws, BSTR bstrAbbr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrAbbr);

	StrUni stuOld;
	bool fHaveOld = m_hmwsstuAbbr.Retrieve(ws, &stuOld);
	if (bstrAbbr && BstrLen(bstrAbbr))
	{
		StrUni stu(bstrAbbr, BstrLen(bstrAbbr));
		if (stu != stuOld)
		{
			m_hmwsstuAbbr.Insert(ws, stu, true);
			m_fDirty = true;
		}
	}
	else if (fHaveOld)
	{
		m_hmwsstuAbbr.Delete(ws);
		m_fDirty = true;
	}

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Get the number of writing systems in which the abbreviated name of this writing system is
	stored.

	@param pcws Pointer to an integer for returning the number of writing systems.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_AbbrWsCount(int * pcws)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcws);

	*pcws = m_hmwsstuAbbr.Size();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Get the (first cws) writing systems in which the abbreviated name of this writing system is
	stored.  If cws is larger than the number of such writing systems, the excess entries in
	prgws are set to zero.

	@param cws Number of entries available in prgws.
	@param prgws Pointer to an array for returning the writing systems.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_AbbrWss(int cws, int * prgws)
{
	BEGIN_COM_METHOD;
	if (cws < 0)
		ThrowHr(WarnHr(E_INVALIDARG));
	ChkComArrayArg(prgws, cws);

	int iws;
	HashMap<int, StrUni>::iterator it;
	for (iws = 0, it = m_hmwsstuAbbr.Begin(); it != m_hmwsstuAbbr.End(); ++it, ++iws)
	{
		if (iws < cws)
			prgws[iws] = it.GetKey();
		else
			break;
	}
	for ( ; iws < cws; ++iws)
		prgws[iws] = 0;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Return the description.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_Description(int ws, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);

	ITsStringPtr qtss;
	if (m_hmwsqtssDescr.Retrieve(ws, qtss))
		*pptss = qtss.Detach();
	else
		*pptss = NULL;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the description.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_Description(int ws, ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ptss);

	ITsStringPtr qtssOld;
	bool fHaveOld = m_hmwsqtssDescr.Retrieve(ws, qtssOld);
	if (ptss)
	{
		ComBool fEqual = FALSE;
		if (fHaveOld)
			CheckHr(qtssOld->Equals(ptss, &fEqual));
		if (!fEqual)
		{
			m_hmwsqtssDescr.Insert(ws, ptss, true);
			m_fDirty = true;
		}
	}
	else if (fHaveOld)
	{
		m_hmwsqtssDescr.Delete(ws);
		m_fDirty = true;
	}

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Get the number of writing systems in which the description of this writing system is stored.

	@param pcws Pointer to an integer for returning the number of writing systems.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_DescriptionWsCount(int * pcws)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcws);

	*pcws = m_hmwsqtssDescr.Size();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Get the (first cws) writing systems in which the description of this writing system is
	stored.  If cws is larger than the number of writing systems, the excess entries in prgws
	are set to zero.

	@param cws Number of entries available in prgws.
	@param prgws Pointer to an array for returning the writing systems.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_DescriptionWss(int cws, int * prgws)
{
	BEGIN_COM_METHOD;
	if (cws < 0)
		ThrowHr(WarnHr(E_INVALIDARG));
	ChkComArrayArg(prgws, cws);

	int iws;
	ComHashMap<int, ITsString>::iterator it;
	for (iws = 0, it = m_hmwsqtssDescr.Begin(); it != m_hmwsqtssDescr.End(); ++it, ++iws)
	{
		if (iws < cws)
			prgws[iws] = it.GetKey();
		else
			break;
	}
	for ( ; iws < cws; ++iws)
		prgws[iws] = 0;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	The current collating engine.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_CollatingEngine(ILgCollatingEngine ** ppcoleng)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppcoleng);

	if (!m_qcoleng.Ptr())
		ThrowHr(WarnHr(E_UNEXPECTED));
	*ppcoleng = m_qcoleng.Ptr();
	(*ppcoleng)->AddRef();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

// If the given string occurs in the strUni, replace it with the given value.
void ReplaceSubstring(StrUni & stu, OLECHAR * pszTarget, OLECHAR * pszRep)
{
	int ich = stu.FindStr(pszTarget);
	if (ich < 0)
		return;
	stu.Replace(ich, ich + wcslen(pszTarget), pszRep);
}

/*----------------------------------------------------------------------------------------------
	Get your character property engine.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_CharPropEngine(ILgCharacterPropertyEngine ** ppcpe)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppcpe);

	if (!m_qcpe)
	{
		ILgCharacterPropertyEnginePtr qcpe;
		SmartBstr sbstrLanguage;
		SmartBstr sbstrScript;
		SmartBstr sbstrCountry;
		SmartBstr sbstrVariant;
		CheckHr(GetIcuLocaleParts(&sbstrLanguage, &sbstrScript, &sbstrCountry, &sbstrVariant));
		ComSmartPtr<LgIcuCharPropEngine> qzcpe;
		// ref count initially 1
		qzcpe.Attach(NewObj LgIcuCharPropEngine(sbstrLanguage, sbstrScript, sbstrCountry, sbstrVariant));
		CheckHr(qzcpe->QueryInterface(IID_ILgCharacterPropertyEngine, (void **)&m_qcpe));

		StrUni stuWfLabel ="<WordForming>";
		int ichWordForming = m_stuValidChars.FindStr(stuWfLabel.Chars(), stuWfLabel.Length(), 0);
		int ichEndWordForming = -1;
		if (ichWordForming > 0)
		{
			StrUni stuEndWfLabel ="</WordForming>";
			ichWordForming += stuWfLabel.Length();
			ichEndWordForming = m_stuValidChars.FindStr(stuEndWfLabel.Chars(), stuEndWfLabel.Length(), ichWordForming);
		}
		if (ichEndWordForming >= 0)
		{
			StrUni wsCharsList(m_stuValidChars.Chars() + ichWordForming, ichEndWordForming - ichWordForming);
			// Since it's an XML representation, if these characters occur they will be escaped. Reverse that.
			// They should not occur more than once.
			ReplaceSubstring(wsCharsList, L"&amp;", L"&");
			ReplaceSubstring(wsCharsList, L"&lt;", L"<");
			ReplaceSubstring(wsCharsList, L"&gt;", L">");
			qzcpe->InitCharOverrides(wsCharsList);
		}

	}
	*ppcpe = m_qcpe.Ptr();
	if (*ppcpe)
		(*ppcpe)->AddRef();

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set tracing for any Graphite engines around (and any future ones created).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::SetTracing(int n)
{
	BEGIN_COM_METHOD;
	m_nTraceSetting = n;
	ITraceControlPtr qtc;
	ComHashMapStrUni<IRenderEngine>::iterator it;
	for (it = m_hmstureEngines.Begin(); it != m_hmstureEngines.End(); ++it)
	{
		IRenderEnginePtr qre = it.GetValue();
		if (qre.Ptr() != m_qrenengUni.Ptr())
		{
			CheckHr(qre->QueryInterface(IID_ITraceControl, (void **)&qtc));
			if (qtc) // Should always be true if not the Unicode engine? But make sure...
				CheckHr(qtc->SetTracing(n));	// Was FwSetTracing(1) but surely a bug? (JohnL)
		}
	}
	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Make any writing-system dependent changes to the CHRP before it is actually used to render.
	Currently this means:
		- interpret any 'magic' font name
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::InterpretChrp(LgCharRenderProps * pchrp)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pchrp);
	ReplaceChrpFontName(pchrp);

	if(pchrp->ssv != kssvOff)
	{
		int dYOffsetNumerator;
		int dYOffsetDenominator;
		int dSizeNumerator;
		int dSizeDenominator;

		int dBaseFontHeight = pchrp->dympHeight;

		if(m_fUseMetricsFromFont)
		{
			// psuedo device context works since we are getting proportions which are invariant
			HDC hdc = ::CreateDC(TEXT("DISPLAY"),NULL,NULL,NULL);

			IVwGraphicsWin32Ptr qvgW;
			qvgW.CreateInstance(CLSID_VwGraphicsWin32);
			qvgW->Initialize(hdc);

			IVwGraphicsPtr qvg;
			CheckHr(qvgW->QueryInterface(IID_IVwGraphics, (void **) &qvg));
			CheckHr(qvg->SetupGraphics(pchrp));

			if (pchrp->ssv == kssvSuper)
			{
				CheckHr(qvg->GetSuperscriptYOffsetRatio(&dYOffsetNumerator, &dYOffsetDenominator));
				CheckHr(qvg->GetSuperscriptHeightRatio(&dSizeNumerator, &dSizeDenominator));
			}
			else if (pchrp->ssv == kssvSub)
			{
				CheckHr(qvg->GetSubscriptYOffsetRatio(&dYOffsetNumerator, &dYOffsetDenominator));
				CheckHr(qvg->GetSubscriptHeightRatio(&dSizeNumerator, &dSizeDenominator));
			}
			qvgW.Clear();
			qvg.Clear();
			::DeleteDC(hdc);
		}
		else
		{
			if (pchrp->ssv == kssvSuper)
			{
				dYOffsetNumerator = m_dSuperscriptYOffsetNumerator;
				dYOffsetDenominator = m_dSuperscriptYOffsetDenominator;
				dSizeNumerator = m_dSuperscriptSizeNumerator;
				dSizeDenominator = m_dSuperscriptSizeDenominator;
			}
			else if (pchrp->ssv == kssvSub)
			{
				dYOffsetNumerator = m_dSubscriptYOffsetNumerator;
				dYOffsetDenominator = m_dSubscriptYOffsetDenominator;
				dSizeNumerator = m_dSubscriptSizeNumerator;
				dSizeDenominator = m_dSubscriptSizeDenominator;
			}
		}

		if (pchrp->ssv == kssvSuper)
		{
			pchrp->dympOffset += MulDiv(dBaseFontHeight, dYOffsetNumerator, dYOffsetDenominator);
		}
		else if (pchrp->ssv == kssvSub)
		{
			pchrp->dympOffset -= MulDiv(dBaseFontHeight, dYOffsetNumerator, dYOffsetDenominator);
		}
		pchrp->dympHeight = MulDiv(dBaseFontHeight, dSizeNumerator, dSizeDenominator);

		pchrp->ssv = kssvOff; // Make sure no way it can happen twice!
	}

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	On any of the relatively rare events that might make some or all renderers invalid, such
	as changing default font, font variation, or direction, throw them all away; they will
	be recreated when needed.
----------------------------------------------------------------------------------------------*/
void WritingSystem::ClearRenderers()
{
	m_qrenengUni.Clear();
	m_hmstureEngines.Clear();
}

		// A Keyman keyboard name that should be used to invoke the appropriate
		// Keyman keyboard if the writing sytem requires it. Leave as empty string
		// for non-Keyman IMs.
STDMETHODIMP WritingSystem::get_KeymanKbdName(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	if (m_stuKeymanKbdName.Length())
		m_stuKeymanKbdName.GetBstr(pbstr);
	// else leave null, from ChkComOutPtr.

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}
STDMETHODIMP WritingSystem::put_KeymanKbdName(BSTR bstr)
{
	BEGIN_COM_METHOD
	ChkComBstrArgN(bstr);
	StrUni stu(bstr, BstrLen(bstr));
	if (m_stuKeymanKbdName != stu)
	{
		m_stuKeymanKbdName.Assign(stu);
		m_fDirty = true;
	}
	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Get the language name for the locale from ICU using the system writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_LanguageName(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	if (m_stuIcuLocale.Length())
	{
		int wsUser;
		int cch;
		SmartBstr sbstr;
		UChar rgch[MAX_PATH];
		UErrorCode uec = U_ZERO_ERROR;
		StrAnsiBufSmall stabLoc(m_stuIcuLocale.Chars());
		CheckHr(m_qwsf->get_UserWs(&wsUser));
		CheckHr(m_qwsf->GetStrFromWs(wsUser, &sbstr));
		StrAnsiBuf stabUser(sbstr.Chars());
		cch = uloc_getDisplayLanguage(stabLoc.Chars(), stabUser.Chars(), rgch, MAX_PATH, &uec);
		if (U_FAILURE(uec))
			ThrowHr(E_UNEXPECTED);
		*pbstr = ::SysAllocStringLen(rgch, cch);
	}
	// else leave null, from ChkComOutPtr.

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Get the script name for the locale from ICU using the system writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_ScriptName(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	if (m_stuIcuLocale.Length())
	{
		int wsUser;
		int cch;
		SmartBstr sbstr;
		UChar rgch[MAX_PATH];
		UErrorCode uec = U_ZERO_ERROR;
		StrAnsiBufSmall stabLoc(m_stuIcuLocale.Chars());
		CheckHr(m_qwsf->get_UserWs(&wsUser));
		CheckHr(m_qwsf->GetStrFromWs(wsUser, &sbstr));
		StrAnsiBuf stabUser(sbstr.Chars());
		cch = uloc_getDisplayScript(stabLoc.Chars(), stabUser.Chars(), rgch, MAX_PATH, &uec);
		if (U_FAILURE(uec))
			ThrowHr(E_UNEXPECTED);
		*pbstr = ::SysAllocStringLen(rgch, cch);
	}
	// else leave null, from ChkComOutPtr.

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Get the country name for the locale from ICU using the system writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_CountryName(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	if (m_stuIcuLocale.Length())
	{
		int wsUser;
		int cch;
		SmartBstr sbstr;
		UChar rgch[MAX_PATH];
		UErrorCode uec = U_ZERO_ERROR;
		StrAnsiBufSmall stabLoc(m_stuIcuLocale.Chars());
		CheckHr(m_qwsf->get_UserWs(&wsUser));
		CheckHr(m_qwsf->GetStrFromWs(wsUser, &sbstr));
		StrAnsiBuf stabUser(sbstr.Chars());
		cch = uloc_getDisplayCountry(stabLoc.Chars(), stabUser.Chars(), rgch, MAX_PATH, &uec);
		if (U_FAILURE(uec))
			ThrowHr(E_UNEXPECTED);
		*pbstr = ::SysAllocStringLen(rgch, cch);
	}
	// else leave null, from ChkComOutPtr.

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Get the variant name for the locale from ICU using the system writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_VariantName(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	if (m_stuIcuLocale.Length())
	{
		int wsUser;
		int cch;
		SmartBstr sbstr;
		UChar rgch[MAX_PATH];
		UErrorCode uec = U_ZERO_ERROR;
		StrAnsiBufSmall stabLoc(m_stuIcuLocale.Chars());
		CheckHr(m_qwsf->get_UserWs(&wsUser));
		CheckHr(m_qwsf->GetStrFromWs(wsUser, &sbstr));
		StrAnsiBuf stabUser(sbstr.Chars());
		cch = uloc_getDisplayVariant(stabLoc.Chars(), stabUser.Chars(), rgch, MAX_PATH, &uec);
		if (U_FAILURE(uec))
			ThrowHr(E_UNEXPECTED);
		*pbstr = ::SysAllocStringLen(rgch, cch);
	}
	// else leave null, from ChkComOutPtr.

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Get the language abbreviation for the locale from ICU using the system writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_LanguageAbbr(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	if (m_stuIcuLocale.Length())
	{
		int cch;
		SmartBstr sbstr;
		char rgch[ULOC_LANG_CAPACITY];
		UErrorCode uec = U_ZERO_ERROR;
		StrAnsiBufSmall stabLoc(m_stuIcuLocale.Chars());
		cch = uloc_getLanguage(stabLoc.Chars(), rgch, ULOC_LANG_CAPACITY, &uec);
		if (U_FAILURE(uec))
			ThrowHr(E_UNEXPECTED);
		StrUniBufSmall stubs;
		stubs.Assign(rgch, cch);
		stubs.GetBstr(pbstr);
	}
	// else leave null, from ChkComOutPtr.

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Get the script abbreviation for the locale from ICU using the system writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_ScriptAbbr(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	if (m_stuIcuLocale.Length())
	{
		int cch;
		SmartBstr sbstr;
		char rgch[ULOC_SCRIPT_CAPACITY];
		UErrorCode uec = U_ZERO_ERROR;
		StrAnsiBufSmall stabLoc(m_stuIcuLocale.Chars());
		cch = uloc_getScript(stabLoc.Chars(), rgch, ULOC_SCRIPT_CAPACITY, &uec);
		if (U_FAILURE(uec))
			ThrowHr(E_UNEXPECTED);
		StrUniBufSmall stubs;
		stubs.Assign(rgch, cch);
		stubs.GetBstr(pbstr);
	}
	// else leave null, from ChkComOutPtr.

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Get the country abbreviation for the locale from ICU using the system writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_CountryAbbr(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	if (m_stuIcuLocale.Length())
	{
		int cch;
		SmartBstr sbstr;
		char rgch[ULOC_COUNTRY_CAPACITY];
		UErrorCode uec = U_ZERO_ERROR;
		StrAnsiBufSmall stabLoc(m_stuIcuLocale.Chars());
		cch = uloc_getCountry(stabLoc.Chars(), rgch, ULOC_COUNTRY_CAPACITY, &uec);
		if (U_FAILURE(uec))
			ThrowHr(E_UNEXPECTED);
		StrUniBufSmall stubs;
		stubs.Assign(rgch, cch);
		stubs.GetBstr(pbstr);
	}
	// else leave null, from ChkComOutPtr.

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Get the variant abbreviation for the locale from ICU using the system writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_VariantAbbr(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	if (m_stuIcuLocale.Length())
	{
		int cch;
		SmartBstr sbstr;
		char rgch[ULOC_FULLNAME_CAPACITY];
		UErrorCode uec = U_ZERO_ERROR;
		StrAnsiBufSmall stabLoc(m_stuIcuLocale.Chars());
		cch = uloc_getVariant(stabLoc.Chars(), rgch, ULOC_FULLNAME_CAPACITY, &uec);
		if (U_FAILURE(uec))
			ThrowHr(E_UNEXPECTED);
		StrUniBufSmall stubs;
		stubs.Assign(rgch, cch);
		stubs.GetBstr(pbstr);
	}
	// else leave null, from ChkComOutPtr.

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Call this when your program wants to save the (dirty) writing system to persistent
	memory.  If the dirty bit is not set, nothing is done.

	@param pode Pointer to the factory's database connection object, or NULL if a database is
				not used for persistence.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::SaveIfDirty(IOleDbEncap * pode)
{
	BEGIN_COM_METHOD
	ChkComArgPtrN(pode);

	if (!m_fDirty)
		return S_OK;

	// TE-8606: Check remote user, including ugly C++ casts
	LgWritingSystemFactory * pzwsf = dynamic_cast<LgWritingSystemFactory *>(m_qwsf.Ptr());
	if (!pzwsf)
		ThrowHr(WarnHr(E_UNEXPECTED));
	AssertPtr(pzwsf);
	bool fIsRemote = pzwsf->DetermineRemoteUser();

	StrUni stuFile;
	GetLanguageFileName(stuFile);
	HANDLE hFile = ::CreateFileW(stuFile.Chars(), GENERIC_READ, FILE_SHARE_READ, NULL,
		OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hFile == INVALID_HANDLE_VALUE)
	{
		// File does not exist: we'll be creating it shortly!  Get the current time to use as
		// the modification timestamp.
		::GetSystemTime(&m_stModified);
	}
	else
	{
		// Synchronize database and filesystem times.
		ComBool fBypass;
		CheckHr(m_qwsf->get_BypassInstall(&fBypass));

		if (!fBypass && !fIsRemote)
		{
			// We'll be writing a new file since the dirty bit was set.
			::GetSystemTime(&m_stModified);
		}
		else if (!fIsRemote)
		{
			// We'll assume the dirty data came from this file, so use its time.
			FILETIME ftModified;
			BOOL fT;
			fT = ::GetFileTime(hFile, NULL, NULL, &ftModified);
			Assert(fT);

			SYSTEMTIME stFileMod;
			BOOL fTimeConverted;
			fTimeConverted = ::FileTimeToSystemTime(&ftModified, &stFileMod);
			Assert(fTimeConverted);
			stFileMod.wMilliseconds = 0; // to match : wsi.m_stModified.wMilliseconds = 0;
			m_stModified = stFileMod;
		}

		::CloseHandle(hFile);
	}
	if (!fIsRemote)
		SaveToDatabase(pode);

	m_fDirty = false;

	if (m_stuIcuLocale.Length())
	{
		HRESULT hrResult = InstallLanguage(TRUE);

		// TE-8606: Sync file times to match database time so we don't write out to file each startup
		if (fIsRemote)
		{
			HANDLE hFile = ::CreateFileW(stuFile.Chars(), GENERIC_WRITE, FILE_SHARE_WRITE, NULL,
				OPEN_EXISTING, FILE_WRITE_ATTRIBUTES, NULL);
			FILETIME fileTime;
			BOOL fTimeConverted;
			fTimeConverted = ::SystemTimeToFileTime(&m_stModified, &fileTime);
			Assert(fTimeConverted);
			BOOL fWorked;
			fWorked = ::SetFileTime(hFile, &fileTime, &fileTime, &fileTime);
			Assert(fWorked);
			::CloseHandle(hFile);
		}
		return hrResult;
	}

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

void WritingSystem::SaveToDatabase(IOleDbEncap * pode)
{
	if (pode)
	{
		// Write all the information to the database.
		StrUni stuCmd;
		ComBool fMoreRows;
		ULONG cbSpaceTaken;
		ComBool fIsNull;
		IOleDbCommandPtr qodc;
		stuCmd.Format(L"UPDATE LgWritingSystem SET [Locale]=%d, [RightToLeft]=%d,",
			m_nLocale, m_fRightToLeft ? 1 : 0);
		if (m_stuDefMono.Length())
			stuCmd.FormatAppend(L" [DefaultMonospace]=N'%s',", m_stuDefMono.Chars());
		else
			stuCmd.FormatAppend(L" [DefaultMonospace]=null,");
		if (m_stuDefSans.Length())
			stuCmd.FormatAppend(L" [DefaultSansSerif]=N'%s',", m_stuDefSans.Chars());
		else
			stuCmd.FormatAppend(L" [DefaultSansSerif]=null,");
		if (m_stuDefBodyFont.Length())
			stuCmd.FormatAppend(L" [DefaultBodyFont]=N'%s',", m_stuDefBodyFont.Chars());
		else
			stuCmd.FormatAppend(L" [DefaultBodyFont]=null,");
		if (m_stuDefSerif.Length())
			stuCmd.FormatAppend(L" [DefaultSerif]=N'%s',", m_stuDefSerif.Chars());
		else
			stuCmd.FormatAppend(L" [DefaultSerif]=null,");
		if (m_stuFontVar.Length())
			stuCmd.FormatAppend(L" [FontVariation]=N'%s',", m_stuFontVar.Chars());
		else
			stuCmd.FormatAppend(L" [FontVariation]=null,");
		if (m_stuSansFontVar.Length())
			stuCmd.FormatAppend(L" [SansFontVariation]=N'%s',", m_stuSansFontVar.Chars());
		else
			stuCmd.FormatAppend(L" [SansFontVariation]=null,");
		if (m_stuBodyFontFeatures.Length())
			stuCmd.FormatAppend(L" [BodyFontFeatures]=N'%s',", m_stuBodyFontFeatures.Chars());
		else
			stuCmd.FormatAppend(L" [BodyFontFeatures]=null,");
		if (m_stuLegacyMapping.Length())
			stuCmd.FormatAppend(L" [LegacyMapping]=N'%s',", m_stuLegacyMapping.Chars());
		else
			stuCmd.FormatAppend(L" [LegacyMapping]=null,");
		if (m_stuIcuLocale.Length())
			stuCmd.FormatAppend(L" [IcuLocale]=N'%s',", m_stuIcuLocale.Chars());
		else
			stuCmd.FormatAppend(L" [IcuLocale]=null,");
		if (m_stuSpellCheckDictionary.Length())
			stuCmd.FormatAppend(L" [SpellCheckDictionary]=N'%s',", m_stuSpellCheckDictionary.Chars());
		else
			stuCmd.FormatAppend(L" [SpellCheckDictionary]=null,");

		DBTIMESTAMP dbts;
		if (m_stModified.wYear == 0)
			SetLastModifiedTime();
		dbts.year = (short)m_stModified.wYear;
		dbts.month = (unsigned short)m_stModified.wMonth;
		dbts.day = (unsigned short)m_stModified.wDay;
		dbts.hour = (unsigned short)m_stModified.wHour;
		dbts.minute = (unsigned short)m_stModified.wMinute;
		dbts.second = (unsigned short)m_stModified.wSecond;
		dbts.fraction = (unsigned long)0;
		stuCmd.Append(L" [LastModified] = ?,");

		// Need to use a parameter because the valid characters list may have an apostrophe.
		if (m_stuValidChars.Length())
			stuCmd.Append(L" [ValidChars] = ?,");
		else
			stuCmd.FormatAppend(L" [ValidChars]=null,");

		// Need to use a parameter because the matched pairs list may have an apostrophe.
		if (m_stuMatchedPairs.Length())
			stuCmd.Append(L" [MatchedPairs] = ?,");
		else
			stuCmd.FormatAppend(L" [MatchedPairs]=null,");

		// Need to use a parameter because the punctuation patterns list may have an apostrophe.
		if (m_stuPunctuationPatterns.Length())
			stuCmd.Append(L" [PunctuationPatterns] = ?,");
		else
			stuCmd.FormatAppend(L" [PunctuationPatterns]=null,");

		// Need to use a parameter because the capitalization info. may have an apostrophe.
		if (m_stuCapitalizationInfo.Length())
			stuCmd.Append(L" [CapitalizationInfo] = ?,");
		else
			stuCmd.FormatAppend(L" [CapitalizationInfo]=null,");

		// Need to use a parameter because the quotation marks may have an apostrophe.
		if (m_stuQuotationMarks.Length())
			stuCmd.Append(L" [QuotationMarks] = ?,");
		else
			stuCmd.FormatAppend(L" [QuotationMarks]=null,");

		// Need to use parameter here because Keyman keyboard name may have an apostrophe.
		if (m_stuKeymanKbdName.Length())
			stuCmd.Append(L" [KeymanKeyboard] = ?");
		else
			stuCmd.Append(L" [KeymanKeyboard] = null");

		stuCmd.FormatAppend(L" WHERE [Id] = %d", m_hvo);

		// Create the command and set its parameters
		ULONG nextParam = 1;
		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->SetParameter(nextParam, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_DBTIMESTAMP,
			reinterpret_cast<ULONG *>(&dbts), sizeof(dbts)));
		nextParam++;
		// list of valid characters
		if (m_stuValidChars.Length())
		{
			CheckHr(qodc->SetParameter(nextParam, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)m_stuValidChars.Chars(),
				m_stuValidChars.Length() * sizeof(OLECHAR)));
			nextParam++;
		}
		// list of matched pairs
		if (m_stuMatchedPairs.Length())
		{
			CheckHr(qodc->SetParameter(nextParam, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)m_stuMatchedPairs.Chars(),
				m_stuMatchedPairs.Length() * sizeof(OLECHAR)));
			nextParam++;
		}
		// list of punctuation patterns
		if (m_stuPunctuationPatterns.Length())
		{
			CheckHr(qodc->SetParameter(nextParam, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)m_stuPunctuationPatterns.Chars(),
				m_stuPunctuationPatterns.Length() * sizeof(OLECHAR)));
			nextParam++;
		}
		if (m_stuCapitalizationInfo.Length())
		{
			CheckHr(qodc->SetParameter(nextParam, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)m_stuCapitalizationInfo.Chars(),
				m_stuCapitalizationInfo.Length() * sizeof(OLECHAR)));
			nextParam++;
		}
		if (m_stuQuotationMarks.Length())
		{
			CheckHr(qodc->SetParameter(nextParam, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)m_stuQuotationMarks.Chars(),
				m_stuQuotationMarks.Length() * sizeof(OLECHAR)));
			nextParam++;
		}
		// keyboard name
		if (m_stuKeymanKbdName.Length())
		{
			CheckHr(qodc->SetParameter(nextParam, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
				(ULONG *)m_stuKeymanKbdName.Chars(),
				m_stuKeymanKbdName.Length() * sizeof(OLECHAR)));
		}
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

		// Update the multilingual name.
		// Don't check for errors in DELETE FROM statements: there may be nothing to delete!
		stuCmd.Format(L"DELETE FROM LgWritingSystem_Name WHERE Obj = %d", m_hvo);
		qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults);
		if (m_hmwsstuName.Size())
		{
			stuCmd.Format(L"EXEC SetMultiTxt$ %d,%d,?,?", kflidLgWritingSystem_Name, m_hvo);
			HashMap<int, StrUni>::iterator it;
			for (it = m_hmwsstuName.Begin(); it != m_hmwsstuName.End(); ++it)
			{
				int ws = it.GetKey();
				StrUni stuName = it.GetValue();
				if (ws && stuName.Length())
				{
					StrUtil::NormalizeStrUni(stuName, UNORM_NFD);
					CheckHr(pode->CreateCommand(&qodc));
					CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
						reinterpret_cast<ULONG *>(&ws), sizeof(ws)));
					CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
						(ULONG *)stuName.Chars(), stuName.Length() * sizeof(OLECHAR)));
					CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtStoredProcedure));
				}
			}
		}

		// Update the multilingual abbreviation.
		// Don't check for errors in DELETE FROM statements: there may be nothing to delete!
		stuCmd.Format(L"DELETE FROM LgWritingSystem_Abbr WHERE Obj = %d", m_hvo);
		qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults);
		if (m_hmwsstuAbbr.Size())
		{
			stuCmd.Format(L"EXEC SetMultiTxt$ %d,%d,?,?", kflidLgWritingSystem_Abbr, m_hvo);
			HashMap<int, StrUni>::iterator it;
			for (it = m_hmwsstuAbbr.Begin(); it != m_hmwsstuAbbr.End(); ++it)
			{
				int ws = it.GetKey();
				StrUni stuAbbr = it.GetValue();
				if (ws && stuAbbr.Length())
				{
					StrUtil::NormalizeStrUni(stuAbbr, UNORM_NFD);
					CheckHr(pode->CreateCommand(&qodc));
					CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
						reinterpret_cast<ULONG *>(&ws), sizeof(ws)));
					CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
						(ULONG *)stuAbbr.Chars(), stuAbbr.Length() * sizeof(OLECHAR)));
					CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtStoredProcedure));
				}
			}
		}

		// Update the multilingual description.
		// Don't check for errors in DELETE FROM statements: there may be nothing to delete!
		stuCmd.Format(L"DELETE FROM MultiStr$ WHERE Flid = %d AND Obj = %d",
			kflidLgWritingSystem_Description, m_hvo);
		qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults);
		if (m_hmwsqtssDescr.Size())
		{
			stuCmd.Format(L"EXEC SetMultiStr$ %d, %d, ?, ?, ?",
				kflidLgWritingSystem_Description, m_hvo);
			ComHashMap<int, ITsString>::iterator it;
			for (it = m_hmwsqtssDescr.Begin(); it != m_hmwsqtssDescr.End(); ++it)
			{
				int ws = it.GetKey();
				ITsStringPtr qtss = it.GetValue();
				if (ws && qtss)
				{
					ITsStringPtr qtssDesc;
					CheckHr(qtss->get_NormalizedForm(knmNFD, &qtssDesc));
					SmartBstr sbstrDesc;
					CheckHr(qtssDesc->get_Text(&sbstrDesc));
					if (!sbstrDesc.Length())
						continue;
					BYTE rgbFmt[4000];
					Vector<BYTE> vbFmt;
					int cbFmt;
					HRESULT hr;
					BYTE * prgbFmt = rgbFmt;
					CheckHr(hr = qtssDesc->SerializeFmtRgb(rgbFmt, isizeof(rgbFmt), &cbFmt));
					if (hr == S_FALSE)
					{
						vbFmt.Resize(cbFmt);
						prgbFmt = vbFmt.Begin();
						CheckHr(hr = qtssDesc->SerializeFmtRgb(prgbFmt, vbFmt.Size(), &cbFmt));
					}
					CheckHr(pode->CreateCommand(&qodc));
					CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
						reinterpret_cast<ULONG *>(&ws), sizeof(ws)));
					CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
						(ULONG *)sbstrDesc.Chars(), sbstrDesc.Length() * sizeof(OLECHAR)));
					CheckHr(qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
						(ULONG *)prgbFmt, cbFmt));
					CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtStoredProcedure));
				}
			}
		}

		// Update the collations.
		// First, collect all the relevant collations existing in the database.
		int icoll;
		int hvoColl;
		Vector<int> vhvoColl;
		Set<int> sethvoColl;
		stuCmd.Format(L"SELECT Dst "
			L"FROM LgWritingSystem_Collations "
			L"WHERE Src = %d", m_hvo);
		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		while (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoColl),
				isizeof(hvoColl), &cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
				sethvoColl.Insert(hvoColl);
			CheckHr(qodc->NextRow(&fMoreRows));
		}
		// Reduce the set to only those that have been deleted, and then delete every collation
		// in the set.
		for (icoll = 0; icoll < m_vqcoll.Size(); ++icoll)
		{
			CheckHr(m_vqcoll[icoll]->get_Hvo(&hvoColl));
			if (hvoColl && sethvoColl.IsMember(hvoColl))
				sethvoColl.Delete(hvoColl);
		}
		Set<int>::iterator it;
		for (it = sethvoColl.Begin(); it != sethvoColl.End(); ++it)
		{
			hvoColl = it->GetValue();
			stuCmd.Format(L"DELETE FROM LgCollation_Name WHERE Obj = %d", hvoColl);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
			stuCmd.Format(L"DELETE FROM MultiStr$ WHERE Obj = %d", hvoColl);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
			stuCmd.Format(L"DELETE FROM CmSortSpec WHERE"
				L" PrimaryCollation = %d OR SecondaryCollation = %d OR TertiaryCollation = %d",
				hvoColl, hvoColl, hvoColl);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
			stuCmd.Format(L"EXEC DeleteObjects '%d'", hvoColl);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		}
		sethvoColl.Clear();

		for (icoll = 0; icoll < m_vqcoll.Size(); ++icoll)
		{
			CheckHr(m_vqcoll[icoll]->get_Hvo(&hvoColl));
			if (!hvoColl)
			{
				// Add this collation to the database.
				hvoColl = 0;
				stuCmd.Format(L"EXEC CreateOwnedObject$ %d, ? output, null, %d, %d, %d, null",
					kclidLgCollation, m_hvo, kflidLgWritingSystem_Collations,
					kcptOwningSequence);
				CheckHr(pode->CreateCommand(&qodc));
				CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4,
					(ULONG *)&hvoColl, isizeof(hvoColl)));
				CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtStoredProcedure));
				CheckHr(qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvoColl),
					sizeof(hvoColl), &fIsNull));
				if (fIsNull)
					continue;		// Something went wrong.
				Assert(hvoColl);

				// Store the new hvoColl in the collation object.
				Collation * pzcoll = dynamic_cast<Collation *>(m_vqcoll[icoll].Ptr());
				AssertPtr(pzcoll);
				pzcoll->SetHvo(hvoColl);
			}
			vhvoColl.Push(hvoColl);

			// id, WinLCID, WinCollation, IcuResourceName, IcuResourceText, IcuRules
			int nWinLCID;
			SmartBstr sbstrWinCollation;
			SmartBstr sbstrIcuResourceName;
			SmartBstr sbstrIcuResourceText;
			SmartBstr sbstrIcuRules;
			CheckHr(m_vqcoll[icoll]->get_WinLCID(&nWinLCID));
			CheckHr(m_vqcoll[icoll]->get_WinCollation(&sbstrWinCollation));
			CheckHr(m_vqcoll[icoll]->get_IcuResourceName(&sbstrIcuResourceName));
			CheckHr(m_vqcoll[icoll]->get_IcuResourceText(&sbstrIcuResourceText));
			CheckHr(m_vqcoll[icoll]->get_IcuRules(&sbstrIcuRules));
			CheckHr(pode->CreateCommand(&qodc));
			stuCmd.Assign(L"UPDATE LgCollation SET");
			if (sbstrWinCollation.Length())
			{
				Assert(nWinLCID);
				stuCmd.FormatAppend(L" WinLCID=%d, WinCollation=?", nWinLCID);
				// Make sure the WinCollation is normalized.
				StrUni stu(sbstrWinCollation.Chars(), sbstrWinCollation.Length());
				StrUtil::NormalizeStrUni(stu, UNORM_NFD);
				CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
					(ULONG *)stu.Chars(), stu.Length() * sizeof(OLECHAR)));
				if (sbstrIcuRules.Length())
				{
					// Both WinCollation and IcuRules.
					stuCmd.Append(L", IcuRules = ?");
					CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
						(ULONG *)sbstrIcuRules.Chars(),
						sbstrIcuRules.Length() * sizeof(OLECHAR)));
				}
			}
			else
			{
				Assert(sbstrIcuResourceName.Length());
				Assert(sbstrIcuResourceText.Length());
				stuCmd.Append(L" IcuResourceName=?, IcuResourceText=?");
				StrUni stuName(sbstrIcuResourceName.Chars(), sbstrIcuResourceName.Length());
				StrUni stuText(sbstrIcuResourceText.Chars(), sbstrIcuResourceText.Length());
				StrUtil::NormalizeStrUni(stuName, UNORM_NFD);
				StrUtil::NormalizeStrUni(stuText, UNORM_NFD);
				CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
					(ULONG *)stuName.Chars(), stuName.Length() * sizeof(OLECHAR)));
				CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
					(ULONG *)stuText.Chars(), stuText.Length() * sizeof(OLECHAR)));
				if (sbstrIcuRules.Length())
				{
					stuCmd.Append(L", IcuRules = ?");
					CheckHr(qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
						(ULONG *)sbstrIcuRules.Chars(),
						sbstrIcuRules.Length() * sizeof(OLECHAR)));
				}
			}
			stuCmd.FormatAppend(L" WHERE Id = %d", hvoColl);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));

			// Update the multilingual name.
			// Don't check for errors in DELETE FROM statements: there may be nothing to delete!
			stuCmd.Format(L"DELETE FROM LgCollation_Name WHERE Obj = %d", hvoColl);
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
			int cws;
			CheckHr(m_vqcoll[icoll]->get_NameWsCount(&cws));
			if (cws)
			{
				Vector<int> vws;
				vws.Resize(cws);
				CheckHr(m_vqcoll[icoll]->get_NameWss(cws, vws.Begin()));
				int iws;
				stuCmd.Format(L"EXEC SetMultiTxt$ %d,%d,?,?",
					kflidLgCollation_Name, hvoColl);
				for (iws = 0; iws < cws; ++iws)
				{
					SmartBstr sbstrName;
					CheckHr(m_vqcoll[icoll]->get_Name(vws[iws], &sbstrName));
					if (!sbstrName.Length())
						continue;
					StrUni stuName(sbstrName.Chars(), sbstrName.Length());
					StrUtil::NormalizeStrUni(stuName, UNORM_NFD);
					CheckHr(pode->CreateCommand(&qodc));
					CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
						reinterpret_cast<ULONG *>(&vws[iws]), sizeof(int)));
					CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
						(ULONG *)stuName.Chars(), stuName.Length() * sizeof(OLECHAR)));
					CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtStoredProcedure));
				}
			}
		}
		// Establish the proper Ord values for these collations.
		for (icoll = 0; icoll < vhvoColl.Size(); ++icoll)
		{
			stuCmd.Format(L"UPDATE CmObject SET OwnOrd$ = %d WHERE [Id] = %d",
				icoll+1, vhvoColl[icoll]);
			CheckHr(pode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
		}
		qodc.Clear();
	}
}

/*----------------------------------------------------------------------------------------------
	This installs the language for ICU use if it hasn't already been installed.  It uses the ICU
	locale value, which must be set before calling this method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::InstallLanguage(ComBool fForce)
{
	BEGIN_COM_METHOD

	// Especially for testing, we may not want to install any languages. This is especially
	// true for tests that may be run before InstallLanguage.exe is actually built.
	// Tests can avoid this by setting BypassInstall on the writing system after it is created.
	ComBool fBypass;
	CheckHr(m_qwsf->get_BypassInstall(&fBypass));
	if (fBypass)
		return S_OK;

	Assert(m_stuIcuLocale.Length());

	StrUni stuFile;
	GetLanguageFileName(stuFile);
	StrAnsi staLeading;
	StrAnsi staTrailing;
	int cchIndent = 0;
	bool fNewFile = false;
	HANDLE hFile = ::CreateFileW(stuFile.Chars(), GENERIC_READ, FILE_SHARE_READ, NULL,
		OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hFile != INVALID_HANDLE_VALUE)
	{
		if (!fForce)
		{
			// File exists, someone else is responsible for keeping it up to date!
			::CloseHandle(hFile);
			return S_OK;
		}
		// Update just the <LgWritingSystem> portion of the file.
		// First read the entire file.
		DWORD cchFileHigh = 0;
		DWORD cchFile = ::GetFileSize(hFile, &cchFileHigh);
		Assert(!cchFileHigh);
		if (!cchFile)
		{
			::CloseHandle(hFile);
			goto create_new;				// Empty file, just create it anew.
		}
		Vector<char> vch;
		vch.Resize(cchFile + 1);
		DWORD cchRead = 0;
		BOOL fOk = ::ReadFile(hFile, vch.Begin(), cchFile, &cchRead, NULL);
		if (!fOk || cchFile != cchRead)
		{
			::CloseHandle(hFile);
			ThrowHr(WarnHr(E_FAIL));		// We had trouble reading the file.
		}
		fOk = ::CloseHandle(hFile);
		if (!fOk)
			ThrowHr(WarnHr(E_FAIL));		// We had trouble closing the file.

		vch[cchFile] = 0;		// NUL terminate the input buffer.
		const char kszBegin1[] = "<LgWritingSystem ";	// allow attributes
		const char kszBegin2[] = "<LgWritingSystem>";	// allow no attributes
		const char kszEnd[] = "</LgWritingSystem>";
		char * pszBegin = strstr(vch.Begin(), kszBegin1);
		if (!pszBegin)
			pszBegin = strstr(vch.Begin(), kszBegin2);
		char * pszEnd = NULL;
		if (pszBegin)
			pszEnd = strstr(pszBegin, kszEnd);
		if (pszBegin && pszEnd)
		{
			pszEnd += strlen(kszEnd);
			pszEnd += strspn(pszEnd, " \t\r\n");
			staLeading.Assign(vch.Begin(), pszBegin - vch.Begin());
			for (int ichT = staLeading.Length() - 1; ichT >= 0; --ichT)
			{
				wchar ch = staLeading[ichT];
				if (ch == '\n')
				{
					if (cchIndent)
						staLeading.Replace(ichT+1, staLeading.Length(), "");
					break;
				}
				else if (ch == ' ' || ch == '\t')
				{
					++cchIndent;
				}
				else if (ch == '>')
				{
					cchIndent = 0;
					break;
				}
			}
			// Note: C# LanguageDefinition class adds extra attributes to the
			// LanguageDefinition line, so we can't look for the closing >.
			if (staLeading.FindStr("<LanguageDefinition") < 0)
				ThrowHr(WarnHr(E_FAIL));		// Malformed file.
			staTrailing.Assign(pszEnd);
			if (staTrailing.FindStr("</LanguageDefinition>") < 0)
				ThrowHr(WarnHr(E_FAIL));		// Malformed file.
		}
		else
		{
			ThrowHr(WarnHr(E_FAIL));		// Malformed file.
		}
	}
	else
	{
		fNewFile = true;
create_new:
		// Need to generate a new, minimal Language Definition file.
		staLeading.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?>%n"
			"<!DOCTYPE LanguageDefinition SYSTEM \"LanguageDefinition.dtd\">%n"
			"<LanguageDefinition>%n");
		cchIndent = 2;
		staTrailing.Format("</LanguageDefinition>%n");
	}
	// Create or truncate the file.
	IStreamPtr qstrm;
	FileStream::Create(stuFile.Chars(), STGM_WRITE | STGM_CREATE, &qstrm);
	// Write the stuff before <LgWritingSystem> element.
	// write the <LgWritingSystem> element.
	// write the stuff following the <LgWritingSystem> element.
	ULONG cbWritten;
	CheckHr(qstrm->Write(staLeading.Chars(), staLeading.Length(), &cbWritten));
	Assert(cbWritten == (ULONG)staLeading.Length());
	CheckHr(WriteAsXml(qstrm, cchIndent));
	CheckHr(qstrm->Write(staTrailing.Chars(), staTrailing.Length(), &cbWritten));
	Assert(cbWritten == (ULONG)staTrailing.Length());
	qstrm.Clear();

	IcuInstallLanguage(fNewFile);

	// Fix the modification timestamp to remain whatever it was.
	hFile = ::CreateFileW(stuFile.Chars(), GENERIC_READ, FILE_SHARE_READ, NULL,
		OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hFile != INVALID_HANDLE_VALUE)
	{
		FILETIME ftModified;
		if (::SystemTimeToFileTime(&m_stModified, &ftModified) != 0)
		{
			if (fNewFile)
				::SetFileTime(hFile, &ftModified, &ftModified, &ftModified);
			else
				::SetFileTime(hFile, NULL, NULL, &ftModified);
		}
		::CloseHandle(hFile);
	}
	else
	{
		Assert(hFile != INVALID_HANDLE_VALUE);
	}

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

void WritingSystem::IcuInstallLanguage(bool fNewFile)
{
	// Install this new language for ICU use.  Assume the InstallLanguage program is in
	// the same directory as the Language DLL. Assume the file is already up to date.

	StrApp strCmd(ModuleEntry::GetModulePathName());
	int iSlash = strCmd.ReverseFindCh('\\');
	Assert(iSlash > 0);
	strCmd.Replace(iSlash + 1, strCmd.Length(), _T("InstallLanguage.exe"));
	StrUni stuFile;
	GetLanguageFileName(stuFile);
	StrApp strFile(stuFile);
	// Argument needs to be surrounded by double quotes to cover spaces in path.
	strCmd.FormatAppend(_T(" -i \"%s\" -c "), strFile.Chars());

	if (fNewFile)
		strCmd.Append(_T(" -newLang "));

	// Get rid of any memory-mapping of files by ICU, and reinitialize ICU.
	IIcuCleanupManagerPtr qicln;
	qicln.CreateInstance(CLSID_IcuCleanupManager);
	CheckHr(qicln->Cleanup());

	DWORD dwRes;
	bool fOk = SilUtil::ExecCmd(strCmd.Chars(), true, true, &dwRes);

	// -4 occurs when the user cancels at the message that occurs when another program has
	// necessary files locked.
	if (!fOk || (dwRes != 0 && dwRes != (DWORD)-4))
	{
		StrUni stuMsg;
		stuMsg.Format(L"InstallLanguage failed on file %s with code %d", stuFile.Chars(), dwRes);
		ThrowHr(WarnHr(E_FAIL), stuMsg.Chars());
	}
}

/*----------------------------------------------------------------------------------------------
	Get the value of the LastModified timestamp.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_LastModified(DATE * pdate)
{
	BEGIN_COM_METHOD

	m_stModified.wMilliseconds = 0;		// Just in case: keep time only to the closest second.
	if (::SystemTimeToVariantTime(&m_stModified, pdate) == 0)
		return E_FAIL;
	m_fHaveModTime = true;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the value of the LastModified timestamp.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_LastModified(DATE date)
{
	BEGIN_COM_METHOD

	if (::VariantTimeToSystemTime(date, &m_stModified) == 0)
		return E_FAIL;
	m_stModified.wMilliseconds = 0;		// Keep time only to the closest second.

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	The current input language. By default this is derived from Locale, but it can be
	overridden temporarily (for one session). Note that this is not persisted.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::get_CurrentInputLanguage(int * pnLangId)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pnLangId);
	if (m_currentLangId != 0)
		*pnLangId = (int)m_currentLangId;
	else
		*pnLangId = LANGIDFROMLCID(m_nLocale);

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the current (temporary) LangId for this writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP WritingSystem::put_CurrentInputLanguage(int nLangId)
{
	BEGIN_COM_METHOD

	m_currentLangId = (LANGID)nLangId;

	END_COM_METHOD(g_fact_pseudonym, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Use the ICU Locale name to derive the name of the XML file in the Languages directory.
----------------------------------------------------------------------------------------------*/
void WritingSystem::GetLanguageFileName(StrUni & stuFile)
{
	StrUni stuLangDir(DirectoryFinder::FwRootDataDir());
	stuLangDir.Append(L"\\Languages");
	stuFile.Format(L"%s\\%s.xml", stuLangDir.Chars(), m_stuIcuLocale.Chars());
}


/*----------------------------------------------------------------------------------------------
	Set LastModified to the timestamp of the file so it won't be overwritten when we exit.
----------------------------------------------------------------------------------------------*/
void WritingSystem::SetLastModifiedTime()
{
	StrUni stuFile;
	GetLanguageFileName(stuFile);
	HANDLE hFile = ::CreateFileW(stuFile.Chars(), GENERIC_READ, FILE_SHARE_READ, NULL,
		OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hFile != INVALID_HANDLE_VALUE)
	{
		FILETIME ftModified;
		BOOL fT;
		fT = ::GetFileTime(hFile, NULL, NULL, &ftModified);
		Assert(fT);
		::CloseHandle(hFile);
		SYSTEMTIME stFileMod;

		INT fTimeConverted;
		fTimeConverted = ::FileTimeToSystemTime(&ftModified, &stFileMod);
		Assert(fTimeConverted);
		stFileMod.wMilliseconds = 0; // to match : wsi.m_stModified.wMilliseconds = 0;
		m_stModified = stFileMod;
		m_fHaveModTime = true;
		m_fNewFile = false;
	}
}

/// Stuff we should do when wsf shuts down, to clean up outgoing refs.
void WritingSystem::Close()
{
	m_qcpe.Clear();
	m_qrenengUni.Clear();
	m_qwsf.Clear();
	m_vqcoll.Clear();
	m_qcoleng.Clear();
}

#include "HashMap_i.cpp"
#include "ComHashMap_i.cpp"
#include "Vector_i.cpp"
