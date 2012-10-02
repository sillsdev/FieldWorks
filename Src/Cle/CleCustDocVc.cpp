/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: CleCustDocVc.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a customiseable view constructor for document views.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#include "Vector_i.cpp"
#include "GpHashMap_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

static DummyFactory g_fact1(_T("SIL.Tle.CleCustDocVc"));

/*----------------------------------------------------------------------------------------------
	Construct the view constructor. Pass the RecordSpec of the view it is for, which allows it
	to find its block specs.
	ENHANCE JohnT: This is a rather low level object to know about the application and its list
	of views. Should we just pass in the list of block specs?
----------------------------------------------------------------------------------------------*/
CleCustDocVc::CleCustDocVc(UserViewSpec * puvs, AfLpInfo * plpi, CleMainWnd * pcmw)
	: VwCustDocVc(puvs, plpi, 0, kflidCmPossibilityList_Possibilities)
{
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu;

	stu.Load(kstidSpaces0);
	CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_qlpi->GetDbInfo()->UserWs(),
		&m_qtssMissing));

	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	// Border thickness below about 1/96 inch, a single pixel on a typical display.
	CheckHr(qtpb->SetIntPropValues(ktptBorderTop, ktpvMilliPoint, kdzmpInch / 96));
	// About a line (say 12 point) of white space above and below the border.
	CheckHr(qtpb->SetIntPropValues(ktptPadTop, ktpvMilliPoint, 12000));
	CheckHr(qtpb->SetIntPropValues(ktptMarginTop, ktpvMilliPoint, 12000));
	CheckHr(qtpb->GetTextProps(&m_qttpMain));

	CheckHr(qtpb->SetIntPropValues(ktptBorderBottom, ktpvMilliPoint, kdzmpInch / 96));
	CheckHr(qtpb->SetIntPropValues(ktptPadBottom, ktpvMilliPoint, 12000));
	CheckHr(qtpb->GetTextProps(&m_qttpMainLast));

	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->SetIntPropValues(ktptMarginTop, ktpvMilliPoint, 12000));
	CheckHr(qtpb->GetTextProps(&m_qttpMainFirst));

	qtpb.CreateInstance(CLSID_TsPropsBldr);
	// Border thickness below about 1/96 inch, a single pixel on a typical display.
	CheckHr(qtpb->SetIntPropValues(ktptBorderBottom, ktpvMilliPoint, kdzmpInch / 96));
	CheckHr(qtpb->SetIntPropValues(ktptPadBottom, ktpvMilliPoint, 12000));
	CheckHr(qtpb->SetIntPropValues(ktptMarginBottom, ktpvMilliPoint, 12000));
	CheckHr(qtpb->GetTextProps(&m_qttpMainFlat));

	// And make the one for subentries.
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->SetIntPropValues(ktptPadTop, ktpvMilliPoint, 12000));
	CheckHr(qtpb->SetIntPropValues(ktptBorderTop, ktpvMilliPoint, 0));
	CheckHr(qtpb->SetIntPropValues(ktptMarginBottom, ktpvMilliPoint, 0));
	// The value below is sort of a default for one level of indentation; it will be
	// adjusted for lower levels.
	CheckHr(qtpb->SetIntPropValues(ktptPadLeading, ktpvMilliPoint, kdzmpInch * 3 / 10));
	CheckHr(qtpb->GetTextProps(&m_qttpSub));

	CheckHr(qtpb->SetIntPropValues(ktptBorderBottom, ktpvMilliPoint, kdzmpInch / 96));
	CheckHr(qtpb->SetIntPropValues(ktptPadBottom, ktpvMilliPoint, 12000));
	CheckHr(qtpb->GetTextProps(&m_qttpSubLast));

	m_qRecVc.Attach(NewObj CleRecVc);
	m_qcmw.Attach(pcmw);
	AddRefObj(pcmw);
	PossListInfoPtr qpli;
	m_qlpi->LoadPossList(pcmw->GetHvoPssl(), pcmw->AnalysisEnc(), &qpli);
	m_hvoPssl = qpli->GetPsslId();
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CleCustDocVc::~CleCustDocVc()
{
}

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.
	Here a CmPossibilityList is displayed by displaying its Records.
	So far we only handle records that are objects of type CmPossibility (or a subclass).
	A CmPossibility (or derived object) is displayed according to the stored specifications.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CleCustDocVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	bool fFlat = m_qcmw->IsFilterActive() || m_qcmw->IsSortMethodActive();

	// Constant fragments
	switch(frag)
	{
	case kfrcdSubItem:	// Override for special treatment.
		Assert(false); // sub-items are handled as main items; see below.
		break;
	case kfrcdMainItem:	// Override for special treatment.
		{
			// Get the complete list of the items in the order in which they are displayed.
			ISilDataAccessPtr qsda;
			CheckHr(pvwenv->get_DataAccess(&qsda));
			RecMainWndPtr qrmw = dynamic_cast<RecMainWnd *>(m_qcmw.Ptr());
			HVO hvoMain = qrmw->GetFilterId();
			int flidMain = qrmw->GetFilterFlid();
			Assert(qrmw);
			int chvoMain;
			Vector<HVO> vhvoMain;
			CheckHr(qsda->get_VecSize(hvoMain, flidMain, &chvoMain));
			for (int ihvo = 0; ihvo < chvoMain; ihvo++)
			{
				HVO hvoTmp;
				CheckHr(qsda->get_VecItem(hvoMain, flidMain, ihvo, &hvoTmp));
				vhvoMain.Push(hvoTmp);
			}

			ITsTextPropsPtr qttp;
			PossItemInfo * ppii;
			PossListInfoPtr qpli;
			m_qlpi->GetPossListAndItem(hvo, m_qlpi->AnalWs(), &ppii, &qpli);
			int ilevel; ilevel = ppii->GetLevel(qpli);
			int ipss;
			for (ipss = 0; ipss < vhvoMain.Size(); ipss++)
			{
				if (hvo == vhvoMain[ipss])
					break;
			}
			Assert(ipss < vhvoMain.Size());

			if (fFlat)
				qttp = m_qttpMainFlat;
			else if (ilevel > 0)
			{
				if (ipss == qpli->GetCount() - 1)
					qttp = m_qttpSubLast;
				else
					qttp = m_qttpSub;
				// Adjust the indentation to match the current level.
				ITsPropsBldrPtr qtpb;
				CheckHr(qttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetIntPropValues(ktptPadLeading, ktpvMilliPoint,
					(ilevel * kdzmpInch / 5)));  // indent 1/5" per level
				CheckHr(qtpb->GetTextProps(&qttp));
			}
			else if (ipss == 0) // first item
				qttp = m_qttpMainFirst;
			else if (ipss == qpli->GetCount() - 1)
				qttp = m_qttpMainLast;
			else
				qttp = m_qttpMain;
			CheckHr(pvwenv->put_Props(qttp));
			BodyOfRecord(pvwenv, hvo, qttp);
			break;
		}
	default:
		return SuperClass::Display(pvwenv, hvo, frag);
	}
	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwViewConstructor)
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP CleCustDocVc::DisplayVec(IVwEnv * pvwenv, HVO hvo, int tag, int frag)
{
	BEGIN_COM_METHOD;

	switch(frag)
	{
	default:
		// Superclass to handle the others.
		return SuperClass::DisplayVec(pvwenv, hvo, tag, frag);
		break;
	}

	return S_OK;

	END_COM_METHOD(g_fact1, IID_IVwViewConstructor)
}

/*----------------------------------------------------------------------------------------------
	DisplayVariant is used for times and enumerations.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CleCustDocVc::DisplayVariant(IVwEnv * pvwenv, int tag, VARIANT v, int frag,
	ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);
	ChkComOutPtr(pptss);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	int ws = m_qlpi->GetDbInfo()->UserWs();

	if (tag == kflidCmPerson_Gender || tag == kflidCmPerson_IsResearcher)
	{
		int itss = v.intVal;
		int stid;
		switch (tag)
		{
		case kflidCmPerson_Gender:
			stid = kstidEnumGender;
			break;
		case kflidCmPerson_IsResearcher:
			stid = kstidEnumNoYes;
			if (itss)
				itss = 1;
			break;
		default:
			Assert(false);
			break;
		}
		StrUni stuEnum(stid);
		const wchar * pszEnum = stuEnum.Chars();
		const wchar * pszEnumLim = stuEnum.Chars() + stuEnum.Length();
		ITsStringPtr qtss;
		//ITsStrFactoryPtr qtsf;
		//qtsf.CreateInstance(CLSID_TsStrFactory);
		int itssTry = 0;
		while (pszEnum < pszEnumLim && itssTry <= itss)
		{
			const wchar * pszEnumNl = wcschr(pszEnum, '\n');
			if (!pszEnumNl)
				pszEnumNl = pszEnumLim;
			if (itss == itssTry)
			{
				return qtsf->MakeStringRgch(pszEnum, (pszEnumNl - pszEnum), ws, pptss);
			}
			itssTry++;
			pszEnum = pszEnumNl + 1;
		}
		// Fall-back behavior if we couldn't find a string: use the integer value.
		char rgch[20];
		_itoa_s(itss, rgch, 10);
		StrUni stu(rgch);
		return qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, pptss);
	}

	return SuperClass::DisplayVariant(pvwenv, tag, v, frag, pptss);

	END_COM_METHOD(g_fact1, IID_IVwViewConstructor)
}


void CleCustDocVc::SetSubItems(IVwEnv * pvwenv, int flid)
{
	switch (flid)
	{
	default:
		Assert(false);
		break;
	}
}

//:>********************************************************************************************
//:>	CleRecVc methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
	@param fLoadData True if the VC needs to load any data it uses.
----------------------------------------------------------------------------------------------*/
CleRecVc::CleRecVc(bool fLoadData) : ObjVc(fLoadData)
{ }


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CleRecVc::~CleRecVc()
{
}

static DummyFactory g_fact2(_T("SIL.Tle.CleRecVc"));


/*----------------------------------------------------------------------------------------------
	Load the data needed to display this view. In this case, we need to load the class, owner
	(so we can tell whether it is a subitem), the title, and create date. If all of these are
	already in the cache, don't reload it.
	@param pvwenv Pointer to the view environment.
	@param hvo The id of the object we are displaying.
	@param frag Identifies the part of the view we are currently displaying.
	@return HRESULT indicating success (S_OK), or failure (E_FAIL).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CleRecVc::LoadDataFor(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	Assert(false);  // TODO: rework

	StrUni stuSql;
	ISilDataAccessPtr qsda;
	CheckHr(pvwenv->get_DataAccess(&qsda));
	bool fLoaded = false;
	int clid;
	CheckHr(qsda->get_IntProp(hvo, kflidCmObject_Class, &clid));
	if (clid)
	{
		HVO hvoOwn;
		CheckHr(qsda->get_ObjectProp(hvo, kflidCmObject_Owner, &hvoOwn));
		if (hvoOwn)
		{
			int64 tim;
			// REVIEW KenZ(RandyR) Whey are DN flids in this app?
			CheckHr(qsda->get_TimeProp(hvo, kflidRnGenericRec_DateCreated, &tim));
			if (tim)
			{
				ITsStringPtr qtss;
				CheckHr(qsda->get_StringProp(hvo, kflidRnGenericRec_Title, &qtss));
				if (qtss)
				{
					int cch;
					CheckHr(qtss->get_Length(&cch));
					if (cch)
						fLoaded = true;
				}
			}
		}
	}

	if (!fLoaded)
	{
		// If any field is missing from the cache, load everything.
		IDbColSpecPtr qdcs;
		IVwOleDbDaPtr qda;
		CheckHr(qsda->QueryInterface(IID_IVwOleDbDa, (void**)&qda));
		stuSql.Format(L"select id, Class$, Owner$, DateCreated, Title, Title_Fmt "
			L"from RnGenericRec_ "
			L"where id = %d", hvo);
		qdcs.CreateInstance(CLSID_DbColSpec);
		CheckHr(qdcs->Push(koctBaseId, 0, 0, 0));
		CheckHr(qdcs->Push(koctInt, 1, kflidCmObject_Class, 0));
		CheckHr(qdcs->Push(koctObj, 1, kflidCmObject_Owner, 0));
			// REVIEW KenZ(RandyR) Whey are DN flids in this app?
		CheckHr(qdcs->Push(koctTime, 1, kflidRnGenericRec_DateCreated, 0));
		CheckHr(qdcs->Push(koctString, 1, kflidRnGenericRec_Title, 0));
		CheckHr(qdcs->Push(koctFmt, 1, kflidRnGenericRec_Title, 0));

		AfMainWnd * pafw = AfApp::Papp()->GetCurMainWnd();
		AssertPtr(pafw);
		AfStatusBar * pstbr = pafw->GetStatusBarWnd();
		AssertPtr(pstbr);
		bool fProgBar = pstbr->IsProgressBarActive();
		if (!fProgBar)
		{
			StrApp strMsg(kstidStBar_LoadingData);
			pstbr->StartProgressBar(strMsg.Chars(), 0, 70, 1);
		}

		// Execute the query and store results in the cache.
		CheckHr(qda->Load(stuSql.Bstr(), qdcs, hvo, 0, pstbr, NULL));
		if (!fProgBar)
			pstbr->EndProgressBar();
	}

	return S_OK;

	END_COM_METHOD(g_fact2, IID_IVwViewConstructor)
}


/*----------------------------------------------------------------------------------------------
	This is the method for displaying the name of a single reference. This view shows the
	name for an RnGenericRec consisting of the type of record, hyphen, title, hyphen,
	creation date. "Subevent - Fishing for pirana - 3/22/2001"
	@param pvwenv Pointer to the view environment.
	@param hvo The id of the object we are displaying.
	@param frag Identifies the part of the view we are currently displaying.
	@return HRESULT indicating success (S_OK), or failure (E_FAIL).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CleRecVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	Assert(false);  // Is this needed for poss lists?

	switch (frag)
	{
	case kfrRefName:
	case kfrListName:
		{
			SmartBstr bstrClass = L"UnLoaded";
			ITsStringPtr qtss;
			ITsStringPtr qtssTitle;

			// Make sure data is loaded.
			LoadDataFor(pvwenv, hvo, frag);
			AfMainWnd * pafw = AfApp::Papp()->GetCurMainWnd();
			AssertPtr(pafw);
			AfLpInfo * plpi = pafw->GetLpInfo();
			AssertPtr(plpi);
			AfDbInfo * pdbi = plpi->GetDbInfo();
			AssertPtr(pdbi);

#define HYPERLINK_CHANGE
#ifdef HYPERLINK_CHANGE
			// Update the string with the new object.
			GUID uid;
			if (!pdbi->GetGuidFromId(hvo, uid))
				ReturnHr(E_FAIL);

			StrUni stuData;
			OLECHAR * prgchData;
			// Make large enough for a guid plus the type character at the start.
			stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
			*prgchData = kodtNameGuidHot;
			memmove(prgchData + 1, &uid, isizeof(uid));

			ITsPropsFactoryPtr qtpf;
			ITsPropsBldrPtr qtpb;
			ITsTextPropsPtr qttp;
			ITsStrFactoryPtr qtsf;

			qtpf.CreateInstance(CLSID_TsPropsFactory);
			CheckHr(qtpf->GetPropsBldr(&qtpb));
			CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, pdbi->UserWs()));
			CheckHr(qtpb->SetStrPropValue(ktptObjData, stuData.Bstr()));
			CheckHr(qtpb->GetTextProps(&qttp));
			qtsf.CreateInstance(CLSID_TsStrFactory);
			OLECHAR chObj = kchObject;
			CheckHr(qtsf->MakeStringWithPropsRgch(&chObj, 1, qttp, &qtss));

			CheckHr(pvwenv->OpenSpan());
			// REVIEW KenZ(RandyR) Whey are DN flids in this app?
			int flid = kflidRnGenericRec_Title;
			CheckHr(pvwenv->NoteDependency(&hvo, &flid, 1));
			CheckHr(pvwenv->AddString(qtss)); // The class name.
			CheckHr(pvwenv->CloseSpan());
#else // !HYPERLINK_CHANGE
			int clid;
			HVO hvoOwn;
			int64 ntim;
			int ws = pdbi->UserWs();
			ISilDataAccessPtr qsda;
			CheckHr(pvwenv->get_DataAccess(&qsda));
			AssertPtr(qsda);
			CheckHr(qsda->get_IntProp(hvo, kflidCmObject_Class, &clid));
			CheckHr(qsda->get_ObjectProp(hvo, kflidCmObject_Owner, &hvoOwn));
			// REVIEW KenZ(RandyR) Whey are DN flids in this app?
			CheckHr(qsda->get_TimeProp(hvo, kflidRnGenericRec_DateCreated, &ntim));
			CheckHr(qsda->get_StringProp(hvo, kflidRnGenericRec_Title, &qtssTitle));

			int stid;
			// Sharon! Not needed?
//			if (clid == kclidRnEvent)
//			{
//				if (plpi->GetRnId() == hvoOwn)
//					stid = kstidEvent;
//				else
//					stid = kstidSubevent;
//			}
//			else if (clid == kclidRnAnalysis)
//			{
//				if (plpi->GetRnId() == hvoOwn)
//					stid = kstidAnalysis;
//				else
//					stid = kstidSubanalysis;
//			}
			StrUni stu(stid);
			StrUni stuSep(kstidSpHyphenSp);
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, &qtss));

			CheckHr(pvwenv->OpenSpan());
			CheckHr(pvwenv->AddString(qtss)); // The class name.
			CheckHr(qtsf->MakeStringRgch(stuSep.Chars(), stuSep.Length(), ws, &qtss));
			CheckHr(pvwenv->AddString(qtss)); // The separator
			//CheckHr(pvwenv->AddString(qtssTitle)); // The title.
			// The following gives the title of the owning object instead of the ref.
			CheckHr(pvwenv->AddStringProp(kflidRnGenericRec_Title, this)); // The title.
			CheckHr(pvwenv->AddString(qtss)); // The separator
			// Leave the date blank if a date doesn't exist.
			if (ntim)
			{
				// Convert the date to a system date.
				SilTime tim = ntim;
				SYSTEMTIME stim;
				stim.wYear = (unsigned short) tim.Year();
				stim.wMonth = (unsigned short) tim.Month();
				stim.wDay = (unsigned short) tim.Date();

				// Then format it to a time based on the current user locale.
				char rgchDate[50]; // Tuesday, August 15, 2000		mardi 15 août 2000
				::GetDateFormat(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &stim, NULL, rgchDate, 50);
				stu = rgchDate;
				CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, &qtss));
				CheckHr(pvwenv->AddString(qtss)); // The date.
			}
			CheckHr(pvwenv->CloseSpan());
#endif // HYPERLINK_CHANGE

			break;
		}
	}

	return S_OK;

	END_COM_METHOD(g_fact2, IID_IVwViewConstructor)
}


/*----------------------------------------------------------------------------------------------
	Return the text string that gets shown to the user when this object needs to be displayed.
	This is the method for displaying the name of a single reference. This view shows the
	name for an RnGenericRec consisting of the type of record, hyphen, title, hyphen,
	creation date. "Subevent - Fishing for pirana - 3/22/2001"

	@param pguid Pointer to a database object's assigned GUID.
	@param pptss Address of a pointer to an ITsString COM object used for returning the text
					string.

	@return S_OK, E_POINTER, or E_FAIL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CleRecVc::GetStrForGuid(BSTR bstrGuid, ITsString ** pptss)
{
	Assert(false);  // rework

	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrGuid);
	ChkComOutPtr(pptss);

	if (BstrLen(bstrGuid) != 8)
		ReturnHr(E_INVALIDARG);

	CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(AfApp::Papp()->GetCurMainWnd());
	AssertPtr(pcmw);
	CleLpInfo * plpi = dynamic_cast<CleLpInfo *>(pcmw->GetLpInfo());
	AssertPtr(plpi);

	HVO hvo = plpi->GetDbInfo()->GetIdFromGuid((GUID *)bstrGuid);

	CustViewDaPtr qcvd;
	plpi->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	int clid;
	HVO hvoOwn;
	int64 ntim;
	ITsStringPtr qtssTitle;
	CheckHr(qcvd->get_IntProp(hvo, kflidCmObject_Class, &clid));
	CheckHr(qcvd->get_ObjectProp(hvo, kflidCmObject_Owner, &hvoOwn));
			// REVIEW KenZ(RandyR) Whey are DN flids in this app?
	CheckHr(qcvd->get_TimeProp(hvo, kflidRnGenericRec_DateCreated, &ntim));
	CheckHr(qcvd->get_StringProp(hvo, kflidRnGenericRec_Title, &qtssTitle));

	int stid;
			// REVIEW KenZ(RandyR) Whey are DN flids in this app?
	if (clid == kclidRnEvent)
	{
		if (pcmw->GetRootObj() == hvoOwn)
			stid = kstidEvent;
		else
			stid = kstidSubevent;
	}
	else if (clid == kclidRnAnalysis)
	{
		if (pcmw->GetRootObj() == hvoOwn)
			stid = kstidAnalysis;
		else
			stid = kstidSubanalysis;
	}
	StrUni stu(stid);
	StrUni stuSep(kstidSpHyphenSp);

	ITsStrFactoryPtr qtsf;
	ITsIncStrBldrPtr qtisb;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	CheckHr(qtsf->GetIncBldr(&qtisb));
	CheckHr(qtisb->Append(stu.Bstr()));

	CheckHr(qtisb->Append(stuSep.Bstr()));
	CheckHr(qtisb->AppendTsString(qtssTitle)); // The title.
	CheckHr(qtisb->Append(stuSep.Bstr()));
	// Leave the date blank if a date doesn't exist.
	if (ntim)
	{
		// Convert the date to a system date.
		SilTime tim = ntim;
		SYSTEMTIME stim;
		stim.wYear = (unsigned short) tim.Year();
		stim.wMonth = (unsigned short) tim.Month();
		stim.wDay = (unsigned short) tim.Date();

		// Then format it to a time based on the current user locale.
		achar rgchDate[50]; // Tuesday, August 15, 2000		mardi 15 août 2000
		::GetDateFormat(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &stim, NULL, rgchDate, 50);
		stu = rgchDate;
		CheckHr(qtisb->Append(stu.Bstr()));
	}
	CheckHr(qtisb->GetString(pptss));

	return S_OK;

	END_COM_METHOD(g_fact2, IID_IVwViewConstructor)
}


/*----------------------------------------------------------------------------------------------
	The user clicked on the object.

	@param pguid Pointer to a database object's assigned GUID.
	@param hvoOwner The database ID of the object.
	@param tag Identifier used to select one particular property of the object.
	@param ptss Pointer to an ITsString COM object containing a string that embeds a link to the
					object.
	@param ichObj Offset in the string to the pseudo-character that represents the object link.

	@return S_OK, E_POINTER, E_INVALIDARG, or E_FAIL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CleRecVc::DoHotLinkAction(BSTR bstrData, HVO hvoOwner, PropTag tag,
	ITsString * ptss, int ichObj)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrData);
	ChkComArgPtr(ptss);

	// TODO (DarrellZ): This is currently not called because something else is handling the
	// click.
	return SuperClass::DoHotLinkAction(bstrData, hvoOwner, tag, ptss, ichObj);

	END_COM_METHOD(g_fact2, IID_IVwViewConstructor)
}

#include "Vector_i.cpp"
template Vector<int>; // IntVec;

#include "Set_i.cpp"
template Set<HVO>; // HvoSet;
