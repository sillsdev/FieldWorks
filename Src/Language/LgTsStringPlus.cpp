/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgTsStringPlus.cpp
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

// The class factory for LgTsStringPlusWss.
static GenericFactory g_fact(
	_T("FieldWorks.LgTsStringPlusWss"),
	&CLSID_LgTsStringPlusWss,
	_T("FieldWorks TsString Plus Writing Systems"),
	_T("Apartment"),
	&LgTsStringPlusWss::CreateCom);

/***********************************************************************************************
	Methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Static method called by the class factory to create a MultiString.
----------------------------------------------------------------------------------------------*/
void LgTsStringPlusWss::CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	if (punkOuter)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgTsStringPlusWss> qtms;

	qtms.Attach(NewObj LgTsStringPlusWss);
	CheckHr(qtms->QueryInterface(iid, ppv));
}

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
LgTsStringPlusWss::LgTsStringPlusWss(void)
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
}


/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
LgTsStringPlusWss::~LgTsStringPlusWss(void)
{
	if (m_qwsfSrc)
	{
		m_qwsfSrc->Shutdown();
		m_qwsfSrc.Clear();
	}
	ModuleEntry::ModuleRelease();
}

/***********************************************************************************************
	IUnknown interface methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown & ILgTsStringPlusWss are
	supported.

	This is a standard COM IUnknown method.

	@param riid Reference to the GUID of the desired COM interface.
	@param ppv Address of a pointer for returning the desired COM interface.

	@return SOK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsStringPlusWss::QueryInterface(REFIID iid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_ILgTsStringPlusWss)
		*ppv = static_cast<ILgTsStringPlusWss *>(this);
	else if (iid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ILgTsStringPlusWss);
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
STDMETHODIMP_(ULONG) LgTsStringPlusWss::AddRef(void)
{
	Assert(m_cref > 0);
	::InterlockedIncrement(&m_cref);
	return m_cref;
}


/*----------------------------------------------------------------------------------------------
	Release.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) LgTsStringPlusWss::Release(void)
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
	ILgTsStringPlusWss interface methods.
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Set *pptss to the string stored in the object formatting it with the specified writing
	system.

	@param newWs	Id of the writing system to use for all runs in the string.
	@param pptss Address of a pointer for returning the string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsStringPlusWss::get_StringUsingWs(int newWs, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pptss);

	*pptss = NULL;
	if (!m_qtss)
		return S_OK;

	HRESULT hr;
	ITsStrBldrPtr qtsb;
	CheckHr(m_qtss->GetBldr(&qtsb));
	int irun;
	int cch;
	CheckHr(qtsb->get_Length(&cch));
	int ichMin = 0;
	// Note: Can't just iterate through the runs because changing the props of the runs can
	// cause runs to coalesce and this change the number of runs
	while (ichMin < cch)
	{
		ITsTextPropsPtr qttp;
		int nVar;
		int oldWs;
		CheckHr(qtsb->get_RunAt(ichMin, &irun));
		CheckHr(qtsb->get_Properties(irun, &qttp));
		int ichLim;
		CheckHr(qtsb->GetBoundsOfRun(irun, &ichMin, &ichLim));
		CheckHr(hr = qttp->GetIntPropValues(ktptWs, &nVar, &oldWs));
		if (hr == S_OK && oldWs != newWs)
			CheckHr(qtsb->SetIntPropValues(ichMin, ichLim, ktptWs, nVar, newWs));
		ichMin = ichLim;
	}
	CheckHr(qtsb->GetString(pptss));

	END_COM_METHOD(g_fact, IID_ILgTsStringPlusWss);
}

/*----------------------------------------------------------------------------------------------
	Set *pptss to the string stored in the object.  The writing system factory is updated as
	needed to ensure that it contains all of the writing systems used in the string.

	@param pwsf Pointer to the writing system factory which may need to be updated with missing
					writing systems found in the string.
	@param pptss Address of a pointer for returning the string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsStringPlusWss::get_String(ILgWritingSystemFactory * pwsf, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pwsf);
	ChkComOutPtr(pptss);

	*pptss = NULL;
	if (!m_qtss)
		return S_OK;

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	AssertPtr(qtsf);
	ITsIncStrBldrPtr qtisb;

	if (m_vqws.Size())
	{
		if (!m_hmwsOldwsNew.Size())
		{
			SmartBstr sbstr;
			int iws;
			ComVector<IWritingSystem> vqwsNew;
			IWritingSystemPtr qws;
			// Remove all known writing systems from the vector of source writing systems.
			// Also, build map from old ws hvos to new.
			int wsNew;
			int wsOld;
			m_fNeedToMap = false;
			for (iws = 0; iws < m_vqws.Size(); ++iws)
			{
				m_vqws[iws]->get_WritingSystem(&wsOld);
				CheckHr(m_vqws[iws]->get_IcuLocale(&sbstr));
				CheckHr(pwsf->GetWsFromStr(sbstr, &wsNew));
				if (wsNew)
				{
					CheckHr(pwsf->get_EngineOrNull(wsNew, &qws));
					m_vqws[iws].Clear();
				}
				else
				{
					CheckHr(pwsf->get_Engine(sbstr, &qws));
				}
				AssertPtr(qws);
				CheckHr(qws->get_WritingSystem(&wsNew));
				m_hmwsOldwsNew.Insert(wsOld, wsNew);
				if (wsOld != wsNew)
					m_fNeedToMap = true;
				vqwsNew.Push(qws);
			}
			// finish creating the missing writing systems by copying information from the
			// deserialized writing systems.  Note that any "known" writing systems have been
			// released (cleared) from the vector of deserialized writing systems.
			IWritingSystemPtr qwsSrc;
			int nLocale;
			ComBool fT;
			ITsStringPtr qtssDesc;
			IRenderEnginePtr qrenengSrc;
			IRenderEnginePtr qreneng;
			ISimpleInitPtr qsimi;
			Vector<int> vwsT;
			int iwsT;
			int cwsT;
			int ws;
			LgWritingSystemFactory * pzwsf = dynamic_cast<LgWritingSystemFactory *>(pwsf);
			AssertPtr(pzwsf);
			if (!pzwsf)
				ThrowHr(WarnHr(E_UNEXPECTED));
			for (iws = 0; iws < m_vqws.Size(); ++iws)
			{
				qwsSrc = m_vqws[iws];
				if (!qwsSrc)
					continue;
				qws = vqwsNew[iws];
				int icol;
				int ccol;
				CheckHr(qws->get_CollationCount(&ccol));
				Assert(ccol == 1);		// newly created ws with single dummy collation.
				for (icol = 0; icol < ccol; ++icol)
					CheckHr(qws->RemoveCollation(0));

				// Finish cloning the source writing system.
				CheckHr(qwsSrc->get_NameWsCount(&cwsT));		// Multilingual name.
				if (cwsT)
				{
					vwsT.Resize(cwsT);
					CheckHr(qwsSrc->get_NameWss(cwsT, vwsT.Begin()));
					for (iwsT = 0; iwsT < cwsT; ++ iwsT)
					{
						CheckHr(qwsSrc->get_Name(vwsT[iwsT], &sbstr));
						if (m_fNeedToMap)
						{
							// Map from the old ws to the new.
							if (m_hmwsOldwsNew.Retrieve(vwsT[iwsT], &ws))
								CheckHr(qws->put_Name(ws, sbstr));
						}
						else
						{
							CheckHr(qws->put_Name(vwsT[iwsT], sbstr));
						}
					}
				}
				CheckHr(qwsSrc->get_AbbrWsCount(&cwsT));		// Multilingual abbreviation.
				if (cwsT)
				{
					vwsT.Resize(cwsT);
					CheckHr(qwsSrc->get_AbbrWss(cwsT, vwsT.Begin()));
					for (iwsT = 0; iwsT < cwsT; ++ iwsT)
					{
						CheckHr(qwsSrc->get_Abbr(vwsT[iwsT], &sbstr));
						if (m_fNeedToMap)
						{
							// Map from the old ws to the new.
							if (m_hmwsOldwsNew.Retrieve(vwsT[iwsT], &ws))
								CheckHr(qws->put_Abbr(ws, sbstr));
						}
						else
						{
							CheckHr(qws->put_Abbr(vwsT[iwsT], sbstr));
						}
					}
				}
				CheckHr(qwsSrc->get_DescriptionWsCount(&cwsT));	// Multilingual description.
				if (cwsT)
				{
					vwsT.Resize(cwsT);
					CheckHr(qwsSrc->get_DescriptionWss(cwsT, vwsT.Begin()));
					for (iwsT = 0; iwsT < cwsT; ++ iwsT)
					{
						ITsStringPtr qtssDesc;
						CheckHr(qwsSrc->get_Description(vwsT[iwsT], &qtssDesc));
						if (m_fNeedToMap)
						{
							// Map from the old ws to the new.
							if (m_hmwsOldwsNew.Retrieve(vwsT[iwsT], &ws))
							{
								ITsStringPtr qtssNew;
								MapInternalWss(qtssDesc, &qtssNew);
								CheckHr(qws->put_Description(ws, qtssNew));
							}
						}
						else
						{
							CheckHr(qws->put_Description(vwsT[iwsT], qtssDesc));
						}
					}
				}
				CheckHr(qwsSrc->get_Locale(&nLocale));					// Locale.
				CheckHr(qws->put_Locale(nLocale));
				CheckHr(qwsSrc->get_RightToLeft(&fT));			// Primary direction.
				CheckHr(qws->put_RightToLeft(fT));
				// WS can now create rendering engine on demand, so no need to clone them
				CheckHr(qwsSrc->get_FontVariation(&sbstr));		// "Font Variation" string.
				CheckHr(qws->put_FontVariation(sbstr));
				CheckHr(qwsSrc->get_SansFontVariation(&sbstr));		// "Sans Serif Font Variation" string.
				CheckHr(qws->put_SansFontVariation(sbstr));
				CheckHr(qwsSrc->get_BodyFontFeatures(&sbstr));		// "Body Font Features" string.
				CheckHr(qws->put_BodyFontFeatures(sbstr));
				CheckHr(qwsSrc->get_DefaultSerif(&sbstr));		// default font.
				CheckHr(qws->put_DefaultSerif(sbstr));
				CheckHr(qwsSrc->get_DefaultSansSerif(&sbstr));	// default heading font.
				CheckHr(qws->put_DefaultSansSerif(sbstr));
				CheckHr(qwsSrc->get_DefaultBodyFont(&sbstr));	// default body font.
				CheckHr(qws->put_DefaultBodyFont(sbstr));
				CheckHr(qwsSrc->get_DefaultMonospace(&sbstr));	// default fixed width font.
				CheckHr(qws->put_DefaultMonospace(sbstr));
				CheckHr(qwsSrc->get_KeyMan(&fT));				// Using Keyman flag.
				CheckHr(qws->put_KeyMan(fT));
				CheckHr(qwsSrc->get_IcuLocale(&sbstr));			// ICU locale identifier.
				CheckHr(qws->put_IcuLocale(sbstr));
				// REVIEW (SteveMc): Are the next two lines needed?
				CheckHr(qwsSrc->get_LegacyMapping(&sbstr));
				CheckHr(qws->put_LegacyMapping(sbstr));
				ICollationPtr qcoll;
				CheckHr(qwsSrc->get_CollationCount(&ccol));
				for (icol = 0; icol < ccol; ++icol)
				{
					CheckHr(qwsSrc->get_Collation(icol, &qcoll));
					AssertPtr(qcoll);
					CollationPtr qzcoll;
					pzwsf->CreateLgCollation(&qzcoll, qws);
					// Get and Put all the internal values of the collation.
					int lcid;
					SmartBstr sbstr;
					CheckHr(qcoll->get_WinLCID(&lcid));
					CheckHr(qcoll->get_WinCollation(&sbstr));
					if (sbstr.Length())
					{
						Assert(lcid);
						// These have a side-effect of clearing IcuResourceName/Text.
						CheckHr(qzcoll->put_WinLCID(lcid));
						CheckHr(qzcoll->put_WinCollation(sbstr));
					}
					CheckHr(qcoll->get_IcuResourceName(&sbstr));
					if (sbstr.Length())
					{
						// These puts have a side-effect of clearing WinLCID and WinCollation.
						CheckHr(qzcoll->put_IcuResourceName(sbstr));
						CheckHr(qcoll->get_IcuResourceText(&sbstr));
						Assert(sbstr.Length());
						CheckHr(qzcoll->put_IcuResourceText(sbstr));
					}
					CheckHr(qcoll->get_IcuRules(&sbstr));
					CheckHr(qzcoll->put_IcuRules(sbstr));
					CheckHr(qcoll->get_NameWsCount(&cwsT));
					vwsT.Resize(cwsT);
					CheckHr(qcoll->get_NameWss(cwsT, vwsT.Begin()));
					for (iwsT = 0; iwsT < cwsT; ++iwsT)
					{
						CheckHr(qcoll->get_Name(vwsT[iwsT], &sbstr));
						if (m_fNeedToMap)
						{
							// Map from the old ws to the new.
							if (m_hmwsOldwsNew.Retrieve(vwsT[iwsT], &ws))
								CheckHr(qzcoll->put_Name(ws, sbstr));
						}
						else
						{
							CheckHr(qzcoll->put_Name(vwsT[iwsT], sbstr));
						}
					}
					CheckHr(qws->putref_Collation(icol, qzcoll));
				}
				// Ensure this gets persisted.
				CheckHr(qws->put_Dirty(TRUE));
			}

			// Keep the DB in snyc with the data in the WS
			CheckHr(pwsf->SaveWritingSystems());
		}
		if (m_fNeedToMap)
		{
			// Convert the internal WS codes (HVOs) from wsOld -> wsNew.
			MapInternalWss(m_qtss, pptss);
		}
		else
		{
			// Must be the same database!  (or 2 that are more identical than is likely)
			*pptss = m_qtss;
			(*pptss)->AddRef();
		}
	}
	else
	{
		// We should never have a string without a set of associated writing systems!
		*pptss = m_qtss;
		(*pptss)->AddRef();
	}

	END_COM_METHOD(g_fact, IID_ILgTsStringPlusWss);
}

/*----------------------------------------------------------------------------------------------
	Convert the internal WS codes (HVOs) from wsOld -> wsNew.

	@param ptss Pointer to the string to convert.
	@param pptssNew Address of pointer to the converted string produced.
----------------------------------------------------------------------------------------------*/
void LgTsStringPlusWss::MapInternalWss(ITsString * ptss, ITsString ** pptssNew)
{
	HRESULT hr;
	ITsStrBldrPtr qtsb;
	CheckHr(ptss->GetBldr(&qtsb));
	int irun;
	int crun;
	CheckHr(qtsb->get_RunCount(&crun));
	for (irun = 0; irun < crun; ++irun)
	{
		ITsTextPropsPtr qttp;
		int nVar;
		int ws;
		CheckHr(qtsb->get_Properties(irun, &qttp));
		CheckHr(hr = qttp->GetIntPropValues(ktptWs, &nVar, &ws));
		if (hr == S_OK)
		{
			int wsNew;
			if (m_hmwsOldwsNew.Retrieve(ws, &wsNew))
			{
				int ichMin;
				int ichLim;
				CheckHr(qtsb->GetBoundsOfRun(irun, &ichMin, &ichLim));
				CheckHr(qtsb->SetIntPropValues(ichMin, ichLim, ktptWs, nVar, wsNew));
			}
			else
			{
				Assert(m_hmwsOldwsNew.Retrieve(ws, &wsNew));
				ThrowHr(WarnHr(E_FAIL));
			}
		}
	}
	CheckHr(qtsb->GetString(pptssNew));
}


/*----------------------------------------------------------------------------------------------
	Store the string ptss and the needed writing systems from the writing system factory pwsf.

	@param pwsf Pointer to the writing system factory which contains all the writing systems
				used in the string.
	@param ptss Pointer to the string to store.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsStringPlusWss::putref_String(ILgWritingSystemFactory * pwsf, ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pwsf);
	ChkComArgPtr(ptss);

	m_qtss = ptss;

	// Save pointers to the language writing system objects that are referenced by the string.
	// Merely saving a pointer to the factory complicates matters when the program terminates
	// with a TsString in the clipboard.  All the factories are shutdown before the system gets
	// around to asking for the TsString to be serialized.
	Set<int> setws;
	AddStringWritingSystems(ptss, setws, pwsf);

	// Now for the fun part: each writing system contains zero or more multilingual names,
	// zero or more multilingual abbreviations, and zero or more multilingual description
	// strings that themselves reference writing systems.  This gets rather involved figuring
	// out the minimal set of writing systems that span everything.  Note that m_vqws may get
	// longer during the loop, but this is self-limiting and handled automatically by the loop
	// as written.
	int iws;
	for (iws = 0; iws < m_vqws.Size(); ++iws)
	{
		int iws2;
		int cws;
		Vector<int> vws;

		// Add any writing systems of the multilingual Name.
		CheckHr(m_vqws[iws]->get_NameWsCount(&cws));
		vws.Resize(cws);
		CheckHr(m_vqws[iws]->get_NameWss(cws, vws.Begin()));
		for (iws2 = 0; iws2 < cws; ++iws2)
			AddWritingSystemIfMissing(vws[iws2], setws, pwsf);

		// Add any writing systems of the multilingual Abbreviation.
		CheckHr(m_vqws[iws]->get_AbbrWsCount(&cws));
		vws.Resize(cws);
		CheckHr(m_vqws[iws]->get_AbbrWss(cws, vws.Begin()));
		for (iws2 = 0; iws2 < cws; ++iws2)
			AddWritingSystemIfMissing(vws[iws2], setws, pwsf);

		// Add any writing systems of the multilingual Description, including those inside the
		// description strings.
		CheckHr(m_vqws[iws]->get_DescriptionWsCount(&cws));
		vws.Resize(cws);
		CheckHr(m_vqws[iws]->get_DescriptionWss(cws, vws.Begin()));
		for (iws2 = 0; iws2 < cws; ++iws2)
		{
			AddWritingSystemIfMissing(vws[iws2], setws, pwsf);
			ITsStringPtr qtss;
			CheckHr(m_vqws[iws]->get_Description(vws[iws2], &qtss));
			AddStringWritingSystems(qtss, setws, pwsf);
		}
	}

	END_COM_METHOD(g_fact, IID_ILgTsStringPlusWss);
}


/*----------------------------------------------------------------------------------------------
	Retrieve the text of the string stored in this object, without worrying about writing systems
	or other properties.

	@param pbstr Pointer to the BSTR for returning the text of the string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsStringPlusWss::get_Text(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (m_qtss)
		CheckHr(m_qtss->get_Text(pbstr));
	else
		*pbstr = NULL;

	END_COM_METHOD(g_fact, IID_ILgTsStringPlusWss);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the writing system factory containing writing systems stored for this string.

	@param pwsf Pointer to the writing system factory which contains all the writing systems
				used in the string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsStringPlusWss::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppwsf);
	AssertPtr(m_qwsfSrc);

	*ppwsf = m_qwsfSrc;
	if (*ppwsf)
		(*ppwsf)->AddRef();

	END_COM_METHOD(g_fact, IID_ICollation);
}

/*----------------------------------------------------------------------------------------------
	Store the string and writing systems factory in serialized form.

	@param pstg Pointer to the IStorage object for storing the serialized data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsStringPlusWss::Serialize(IStorage * pstg)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstg);

	// Serialize the string in two streams, "Text" and "Fmt".
	IStreamPtr qstrmText;
	ULONG cb;
	ULONG cbWritten;
	CheckHr(pstg->CreateStream(L"Text", STGM_READWRITE | STGM_SHARE_EXCLUSIVE, 0, 0,
		&qstrmText));
	SmartBstr sbstr;
	CheckHr(m_qtss->get_Text(&sbstr));
	cb = BstrSize(sbstr);
	CheckHr(qstrmText->Write(sbstr.Chars(), cb, &cbWritten));
	if (cb != cbWritten)
		ThrowHr(WarnHr(E_UNEXPECTED));
	IStreamPtr qstrmFmt;
	CheckHr(pstg->CreateStream(L"Fmt", STGM_READWRITE | STGM_SHARE_EXCLUSIVE, 0, 0,
		&qstrmFmt));
	CheckHr(m_qtss->SerializeFmt(qstrmFmt));

	// Serialize the vector of writing systems as an writing system factory.
	LgWritingSystemFactory::SerializeVector(pstg, m_vqws);
	CheckHr(pstg->Commit(STGC_DEFAULT));

	END_COM_METHOD(g_fact, IID_ILgTsStringPlusWss);
}


/*----------------------------------------------------------------------------------------------
	Initialize the string and writing systems factory from the serialized data.

	@param pstg Pointer to the IStorage object which contains the serialized data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgTsStringPlusWss::Deserialize(IStorage * pstg)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstg);

	// Read the "Text" and "Fmt" streams, and combine them to form a TsString.
	IStreamPtr qstrmText;
	IStreamPtr qstrmFmt;
	CheckHr(pstg->OpenStream(L"Text", NULL, STGM_READ | STGM_SHARE_EXCLUSIVE, 0,
		&qstrmText));
	CheckHr(pstg->OpenStream(L"Fmt", NULL, STGM_READ | STGM_SHARE_EXCLUSIVE, 0,
		&qstrmFmt));
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	CheckHr(qtsf->DeserializeStringStreams(qstrmText, qstrmFmt, &m_qtss));

	// Deserialize the writing system factory.
	if (m_qwsfSrc)
	{
		m_qwsfSrc->Shutdown();
		m_qwsfSrc.Clear();
	}
	ILgWritingSystemFactoryBuilderPtr qwsfb;
	LgWritingSystemFactoryBuilder::CreateCom(NULL, IID_ILgWritingSystemFactoryBuilder,
		(void **)&qwsfb);
	if (!qwsfb.Ptr())
		ThrowHr(WarnHr(E_UNEXPECTED));
	CheckHr(qwsfb->Deserialize(pstg, &m_qwsfSrc));
	// Build the vector of writing systems from the factory.
	m_vqws.Clear();
	int cws;
	int iws;
	Vector<int> vws;
	IWritingSystemPtr qws;
	CheckHr(m_qwsfSrc->get_NumberOfWs(&cws));
	vws.Resize(cws);
	CheckHr(m_qwsfSrc->GetWritingSystems(vws.Begin(), vws.Size()));
	for (iws = 0; iws < cws; ++iws)
	{
		CheckHr(m_qwsfSrc->get_EngineOrNull(vws[iws], &qws));
		if (qws)
			m_vqws.Push(qws);
	}

	END_COM_METHOD(g_fact, IID_ILgTsStringPlusWss);
}


/*----------------------------------------------------------------------------------------------
	Scan the string for any language writing system properties that are not already accounted
	for by m_vqws and setws.  This updates both m_vqws and setws.

	@param ptss Pointer to the string to scan.
	@param setws Reference to the current set of writing systems contained in m_vqws.
	@param pwsf Pointer to the relevant writing system factory.
----------------------------------------------------------------------------------------------*/
void LgTsStringPlusWss::AddStringWritingSystems(ITsString * ptss, Set<int> & setws,
	ILgWritingSystemFactory * pwsf)
{
	AssertPtr(ptss);
	AssertPtr(pwsf);

	int crun;
	int irun;
	int ws;
	int nVar;
	TsRunInfo tri;
	ITsTextPropsPtr qttp;
	HRESULT hr;
	IWritingSystemPtr qws;
	CheckHr(ptss->get_RunCount(&crun));
	for (irun = 0; irun < crun; ++irun)
	{
		CheckHr(ptss->FetchRunInfo(irun, &tri, &qttp));
		CheckHr(hr = qttp->GetIntPropValues(ktptWs, &nVar, &ws));
		if (hr != S_FALSE && ws != -1 && !setws.IsMember(ws))
		{
			CheckHr(pwsf->get_EngineOrNull(ws, &qws));
			if (qws)
				m_vqws.Push(qws);
			setws.Insert(ws);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Add the indicated writing system to both m_vqws and setws if it is not already a member of
	setws.

	@param ws Writing system id.
	@param setws Reference to the current set of writing systems contained in m_vqws.
	@param pwsf Pointer to the relevant writing system factory.
----------------------------------------------------------------------------------------------*/
void LgTsStringPlusWss::AddWritingSystemIfMissing(int ws, Set<int> & setws,
	ILgWritingSystemFactory * pwsf)
{
	Assert(ws);
	AssertPtr(pwsf);

	if (!setws.IsMember(ws))
	{
		IWritingSystemPtr qws;
		CheckHr(pwsf->get_EngineOrNull(ws, &qws));
		AssertPtr(qws);
		if (qws)
		{
			setws.Insert(ws);
			m_vqws.Push(qws);
		}
	}
}

#include "Hashmap_i.cpp"
#include "Set_i.cpp"
