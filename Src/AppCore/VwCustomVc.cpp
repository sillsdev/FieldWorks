/*----------------------------------------------------------------------------------------------
Copyright 2000, 2002, SIL International. All rights reserved.

File: VwCustomVc.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a customiseable view constructor for browse views.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include "Vector_i.cpp"
#include "GpHashMap_i.cpp"
#include "Set_i.cpp"
template Set<HVO>; // HvoSet;

#undef THIS_FILE
DEFINE_THIS_FILE

static DummyFactory g_factCust(_T("Sil.VwCustomVc"));
static DummyFactory g_factCustDoc(_T("Sil.VwCustDocVc"));
static DummyFactory g_factCustBrowse(_T("Sil.VwCustBrowseVc"));


//:>********************************************************************************************
//:>	VwCustomVc Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
VwCustomVc::VwCustomVc(UserViewSpec * puvs, AfLpInfo * plpi, int tagTitle, int tagSubItems,
	int tagRootItems)
{
	AssertPtr(puvs);
	AssertPtr(plpi);

	m_quvs = puvs;
	m_qlpi = plpi;
	m_tagTitle = tagTitle;
	m_tagSubItems = tagSubItems;
	m_tagRootItems = tagRootItems;
	m_flidTemp = 0;
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	m_wsUser = pdbi->UserWs();
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
VwCustomVc::~VwCustomVc()
{
}

/*----------------------------------------------------------------------------------------------
	Subclasses handle the ones they want, and call this method for any others.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCustomVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	int cch;

	// Constant fragments
	switch (frag)
	{
	default:
		ThrowHr(WarnHr(E_UNEXPECTED));
	case kfrcdMlaVern:
		{
			ITsStringPtr qtss;
			CheckHr(m_qcda->get_MultiStringAlt(hvo, m_flidTemp,
				m_qlpi->VernWs(), &qtss));
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
			qtss->get_Length(&cch);
			if (cch > 0)
				CheckHr(pvwenv->AddString(qtss));
			else
				CheckHr(pvwenv->AddString(m_qtssMissing));
			m_flidTemp = 0;	// Clear out temp flid.
			break;
		}
	case kfrcdMlaAnal:
		{
			ITsStringPtr qtss;
			CheckHr(m_qcda->get_MultiStringAlt(hvo, m_flidTemp,
				m_qlpi->AnalWs(), &qtss));
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
			qtss->get_Length(&cch);
			if (cch > 0)
				CheckHr(pvwenv->AddString(qtss));
			else
				CheckHr(pvwenv->AddString(m_qtssMissing));
			m_flidTemp = 0;	// Clear out temp flid.
			break;
		}
	}

	return S_OK;

	END_COM_METHOD(g_factCust, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCustomVc::DisplayVec(IVwEnv * pvwenv, HVO hvo, int tag, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	switch (frag)
	{
	case kfrcdTagSeq:
		{
			// We handle this one as a sequence so we can insert separators
			// REVIEW JohnT: do we do something here about sorting by topic list?
			ISilDataAccessPtr qsda;
			CheckHr(pvwenv->get_DataAccess(&qsda));
			int chvo;
			CheckHr(qsda->get_VecSize(hvo, tag, &chvo));
			if (!chvo)
			{
				CheckHr(pvwenv->AddString(m_qtssMissing));
				return S_OK;
			}
			for (int ihvo = 0; ihvo < chvo; ihvo++)
			{
				if (ihvo != 0)
					CheckHr(pvwenv->AddString(m_qtssListSep));
				HVO hvoPli;
				CheckHr(qsda->get_VecItem(hvo, tag, ihvo, &hvoPli));
				CheckHr(pvwenv->AddObj(hvoPli, this, kfrcdPliName));
			}
			break;
		}
	case kfrcdObjRefSeq:
		{
			// We handle this one as a sequence so we can insert separators.
			ISilDataAccessPtr qsda;
			CheckHr(pvwenv->get_DataAccess(&qsda));
			int chvo;
			CheckHr(qsda->get_VecSize(hvo, tag, &chvo));
			if (!chvo)
			{
				CheckHr(pvwenv->AddString(m_qtssMissing));
				return S_OK;
			}
			for (int ihvo = 0; ihvo < chvo; ihvo++)
			{
				if (ihvo != 0)
					CheckHr(pvwenv->AddString(m_qtssListSep));
				HVO hvoObj;
				CheckHr(qsda->get_VecItem(hvo, tag, ihvo, &hvoObj));
				CheckHr(pvwenv->AddObj(hvoObj, m_qRecVc, kfrRefName));
			}
		}
		break;
	default:
		Assert(false); // Subclasses should handle any others.
	}

	return S_OK;

	END_COM_METHOD(g_factCust, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	Load data needed to display the specified objects using the specified fragment.
	This is called before attempting to Display an item that has been listed for lazy display
	using AddLazyItems. It may be used to load the necessary data into the DataAccess object.
	If you are not using AddLazyItems this method may be left unimplemented.
	If you pre-load all the data, it should trivially succeed (i.e., without doing anything).
	(That is the case here, for the moment.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCustomVc::LoadDataFor(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
	int tag, int frag, int ihvoMin)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);
	ChkComArrayArg(prghvo, chvo);

	HvoVec vhvoNeedLoad; // vector of objects we need to load data about.

	for (HVO * phvo = prghvo; phvo < prghvo + chvo; phvo++)
	{
		if (!m_shvoLoaded.IsMember(*phvo))
			vhvoNeedLoad.Push(*phvo);
	}
	// If none of the immediately wanted objects need loading we are done.
	if (vhvoNeedLoad.Size() == 0)
		return S_OK;

	// Otherwise broaden the list a bit. This is to reduce the number of load calls.
	// We arbitrarily load up to five entries before and after the ones we know we want.
	int ihvoMinLoad = max(0, ihvoMin - 5);
	int chvoProp;
	CheckHr(m_qcda->get_VecSize(hvoParent, tag, &chvoProp));
	int ihvoMaxLoad = min(chvoProp, ihvoMin + chvo + 5);
	int ihvo;
	for (ihvo = ihvoMinLoad; ihvo < ihvoMaxLoad; ihvo++)
	{
		if (ihvo == ihvoMin)
		{
			// skip the ones we already got from prghvo
			ihvo += chvo - 1;
			continue;
		}
		HVO hvoItem;
		CheckHr(m_qcda->get_VecItem(hvoParent, tag, ihvo, &hvoItem));
		if (!m_shvoLoaded.IsMember(hvoItem))
			vhvoNeedLoad.Push(hvoItem);
	}

	// If we have accesss to a status bar but nothing is in progress, start up a
	// progress indicator, using arbitrary range just so something is seen to happen.
	bool fStartProgressBar = m_qstbr && !m_qstbr->IsProgressBarActive();
	if (fStartProgressBar)
	{
		StrApp strMsg(kstidStBar_LoadingData);
		m_qstbr->StartProgressBar(strMsg.Chars(), 0, 1000, 50);
	}
	// At this point vhvoNeedLoad is a list of the items we want to load.
	// The load process also needs to know their classes.
	HvoClsidVec vhcNeedLoad;
	for (ihvo = 0; ihvo < vhvoNeedLoad.Size(); ihvo++)
	{
		HvoClsid hc;
		hc.hvo = vhvoNeedLoad[ihvo];
		CheckHr(m_qcda->get_ObjClid(hc.hvo, &hc.clsid));
		vhcNeedLoad.Push(hc);
	}
	m_qcda->LoadData(vhcNeedLoad, m_quvs, m_qstbr, GetLoadRecursively());
	for (ihvo = 0; ihvo < vhvoNeedLoad.Size(); ihvo++)
	{
		m_shvoLoaded.Insert(vhvoNeedLoad[ihvo]);
	}
	// If we had to start a progress bar, return things to normal.
	if (fStartProgressBar)
	{
		m_qstbr->EndProgressBar();
	}

	END_COM_METHOD(g_factCust, IID_IVwViewConstructor);
}

/*----------------------------------------------------------------------------------------------
	For each object in prghvo, look for a (non-multilingual) string property identified by
	tagStrProp. If it is an empty string and its writing system is not wsExpected, fix it in
	the cache.
----------------------------------------------------------------------------------------------*/

void VwCustomVc::FixEmptyStrings(HVO * prghvo, int chvo, PropTag tagStrProp, int wsExpected)
{
	for (int i = 0; i < chvo; ++i)
	{
		int hvo = prghvo[i];
		ITsStringPtr qtssCurrent;
		CheckHr(m_qcda->get_StringProp(hvo, tagStrProp, &qtssCurrent));
		int cch;
		CheckHr(qtssCurrent->get_Length(&cch));
		if (cch == 0)
		{
			ITsTextPropsPtr qttp;
			CheckHr(qtssCurrent->get_Properties(0, &qttp));
			int ws, var;
			CheckHr(qttp->GetIntPropValues(ktptWs, &var, &ws));
			if (ws != wsExpected)
			{
				// Fix!
				ITsPropsBldrPtr qtpb;
				CheckHr(qttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetIntPropValues(ktptWs, var, wsExpected));
				ITsTextPropsPtr qttpNew;
				CheckHr(qtpb->GetTextProps(&qttpNew));
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				ITsStringPtr qtssNew;
				CheckHr(qtsf->MakeStringWithPropsRgch(NULL, 0, qttpNew, &qtssNew));
				// It's enough just to override what's in the cache. If the user actually types
				// something, the modified string in the proper ws will get saved.
				CheckHr(m_qcda->CacheStringProp(hvo, tagStrProp, qtssNew));
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Return the text string that gets shown to the user when this object needs to be displayed.

	@param pguid Pointer to a database object's assigned GUID.
	@param pptss Address of a pointer to an ITsString COM object used for returning the text
					string.

	@return S_OK, E_POINTER, or E_FAIL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCustomVc::GetStrForGuid(BSTR bstrGuid, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrGuid); // REVIEW: Is this allowed to be NULL?
	ChkComOutPtr(pptss);

	AssertPtr(m_qRecVc);
	return m_qRecVc->GetStrForGuid(bstrGuid, pptss);

	END_COM_METHOD(g_factCust, IID_IVwViewConstructor);
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
STDMETHODIMP VwCustomVc::DoHotLinkAction(BSTR bstrData, HVO hvoOwner, PropTag tag,
	ITsString * ptss, int ichObj)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrData); // REVIEW: Is this allowed to be NULL?
	ChkComArgPtr(ptss);

	// TODO (DarrellZ): This is currently not called because something else is handling the
	// click.
	return m_qRecVc->DoHotLinkAction(bstrData, hvoOwner, tag, ptss, ichObj);

	END_COM_METHOD(g_factCust, IID_IVwViewConstructor);
}

/*----------------------------------------------------------------------------------------------
	Set the data accessor cache.

	@param pcda Pointer to the cache.
	@param pstbr Pointer to the status bar.
	@param puvs Pointer to the user views. (May be NULL.)
----------------------------------------------------------------------------------------------*/
void VwCustomVc::SetDa(CustViewDa * pcda, AfStatusBar * pstbr, UserViewSpec * puvs)
{
	AssertPtr(pcda);
	AssertPtr(pstbr);

	m_qcda = pcda;
	m_qstbr = pstbr;
	if (puvs)
		m_quvs = puvs;
}


/*----------------------------------------------------------------------------------------------
	The bulk of kfrcdMainItem and kfrcdSubItem.
----------------------------------------------------------------------------------------------*/
void VwCustomVc::BodyOfRecord(IVwEnv * pvwenv, HVO hvo, ITsTextProps * pttpDiv, int nLevel)
{
	// TODO: Move any common code up here from subclasses.
}

/*----------------------------------------------------------------------------------------------
	Prepare for full refresh.
----------------------------------------------------------------------------------------------*/
bool VwCustomVc::FullRefresh()
{
	m_shvoLoaded.Clear();
	return true;
}


//:>********************************************************************************************
//:>	VwCustDocVc Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
VwCustDocVc::VwCustDocVc(UserViewSpec * puvs, AfLpInfo * plpi, int tagTitle, int tagSubItems,
	int tagRootItems)
: VwCustomVc(puvs, plpi, tagTitle, tagSubItems, tagRootItems)
{
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu;

	stu.Load(kstidSpaces3);
	CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &m_qtssFldSep));
	stu.Load(kstidListSeparator);
	CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &m_qtssListSep));
	stu.Load(kstidBlockEnd);
	CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &m_qtssBlockEnd));
	stu.Load(kstidColon);
	CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &m_qtssColon));
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
VwCustDocVc::~VwCustDocVc()
{
}

/*----------------------------------------------------------------------------------------------
	Subclasses handle the ones they want, and call this method for any others.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCustDocVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	// Constant fragments
	switch (frag)
	{
	default:
		return SuperClass::Display(pvwenv, hvo, frag);
		break;
	case kfrcdRoot:
		// This sets up a default, so we can only edit where we explicitly permit it.
		CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
		// And this makes our current ananlysis writing system the default for all empty,
		// unencoded strings unless overridden.
		CheckHr(pvwenv->put_IntProperty(ktptBaseWs, ktpvDefault, m_qlpi->AnalWs()));
		CheckHr(pvwenv->OpenDiv());
		CheckHr(pvwenv->AddLazyVecItems(m_tagRootItems, this, kfrcdMainItem));
		CheckHr(pvwenv->CloseDiv());
		break;
	case kfrcdSubItem:
		BodyOfRecord(pvwenv, hvo, m_qttpSub);
		break;
	case kfrcdMainItem:
		CheckHr(pvwenv->put_Props(m_qttpMain));
		BodyOfRecord(pvwenv, hvo, m_qttpMain);
		break;
	case kfrcdPliName:
		{
			if (!hvo)
				return S_OK; // Nothing to display.
			PossListInfoPtr qpli;
			PossItemInfo * ppii = NULL;
			m_qlpi->GetPossListAndItem(hvo, m_qfsp->m_ws, &ppii, &qpli);
			AssertPtr(ppii);
			StrUni stu;
			if (m_qfsp->m_fHier)
				ppii->GetHierName(qpli, stu, m_qfsp->m_pnt);
			else
				ppii->GetName(stu, m_qfsp->m_pnt);
			ITsStringPtr qtss;
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			AssertPtr(qtsf);
			int ws;
			ws = ppii->GetWs();
			qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws,	&qtss);
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
			CheckHr(pvwenv->AddString(qtss));
			break;
		}
	}
	return S_OK;

	END_COM_METHOD(g_factCustDoc, IID_IVwViewConstructor)
}

/*----------------------------------------------------------------------------------------------
	DisplayVariant is used for times.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCustDocVc::DisplayVariant(IVwEnv * pvwenv, int tag, VARIANT v, int frag,
	ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);
	ChkComOutPtr(pptss);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);

	StrUniBuf stub;
	switch (v.vt)
	{
	default:
		Assert(false);
		break;
	case VT_I4:
		{ // BLOCK
			int gdat = v.lVal;
			stub.Format(L"%D", gdat);
		}
		break;
	case VT_I8: // availabe now, fits our use better then VT_CY
	case VT_CY:
		{ // BLOCK
			// Get the time information from the variant..
			SilTime tim = v.cyVal.int64;

			// Leave the field blank if a date doesn't exist.
			if (v.cyVal.int64)
			{
				// Convert the date to a system date.
				SYSTEMTIME stim;
				stim.wYear = (unsigned short) tim.Year();
				stim.wMonth = (unsigned short) tim.Month();
				stim.wDayOfWeek = (unsigned short) tim.WeekDay();
				stim.wDay = (unsigned short) tim.Date();
				stim.wHour = (unsigned short) tim.Hour();
				stim.wMinute = (unsigned short) tim.Minute();
				stim.wSecond = (unsigned short) tim.Second();
				stim.wMilliseconds = (unsigned short)(tim.MilliSecond());

				// Then format it to a time based on the current user locale.
				wchar rgchDate[100]; // Tuesday, August 15, 2000	mardi 15 août 2000
				wchar rgchTime[100]; // 10:17:09 PM					22:20:08
				::GetDateFormatW(LOCALE_USER_DEFAULT, DATE_SHORTDATE, &stim, NULL, rgchDate,
					100);
				::GetTimeFormatW(LOCALE_USER_DEFAULT, NULL, &stim, NULL, rgchTime, 100);
				stub.Format(L"%s %s", rgchDate, rgchTime);
			}
		}
		break;
	}
	return qtsf->MakeStringRgch(stub.Chars(), stub.Length(), m_wsUser, pptss);

	return S_OK;

	END_COM_METHOD(g_factCustDoc, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	This routine is used to estimate the height of an item. The item will be one of
	those you have added to the environment using AddLazyItems. Note that the calling code
	does NOT ensure that data for displaying the item in question has been loaded.
	The first three arguments are as for Display, that is, you are being asked to estimate
	how much vertical space is needed to display this item in the available width.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCustDocVc::EstimateHeight(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pdyHeight);

	*pdyHeight = 72 * 5 / 2; // Guess an entry is about 2.5 inches.

	return S_OK;

	END_COM_METHOD(g_factCustDoc, IID_IVwViewConstructor)
}

/*----------------------------------------------------------------------------------------------
	Add a paragraph consisting of a list of fields.
	If all the fields are empty, add nothing at all.
	If fTest is true, just test whether anything will be displayed
----------------------------------------------------------------------------------------------*/
bool VwCustDocVc::DisplayFields(FldSpecPtr * prgqfsp, int cfsp, IVwEnv * pvwenv, HVO hvo,
	bool fOnePerLine, HvoVec & vhvoNoData, IntVec & vtagNoData, bool fTest)
{
	ISilDataAccessPtr qsda;
	CheckHr(pvwenv->get_DataAccess(&qsda));
	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(qsda->get_WritingSystemFactory(&qwsf));
	AssertPtr(qwsf);

	bool fParaOpen = false;
	bool fGeneratedAField = false;
	for (int ifsp = 0; ifsp < cfsp; ifsp++)
	{
		FldSpecPtr qfsp = prgqfsp[ifsp];
		// Does the user want this field at all?
		if (qfsp->m_eVisibility == kFTVisNever)
			continue;

		bool fGotData = false;
		ITsStringPtr qtss;
		int cch;
		HVO hvoVal;
		int chvo;
		// Default values to note if no data.
		HVO hvoNoData = hvo;
		PropTag tagNoData = qfsp->m_flid;
		switch (qfsp->m_ft)
		{
		case kftString:
			CheckHr(qsda->get_StringProp(hvo, qfsp->m_flid, &qtss));
			CheckHr(qtss->get_Length(&cch));
			fGotData = (cch != 0 || qfsp->m_eVisibility == kFTVisAlways);
			break;
		case kftMta:
		case kftMsa:
			{
				int cch = 0; // Total number of characters in all strings.
				Vector<int> vws;
				switch (qfsp->m_ws)
				{
				case kwsAnals:
				case kwsAnal:
					vws = m_qlpi->AnalWss();
					break;
				case kwsVerns:
				case kwsVern:
					vws = m_qlpi->VernWss();
					break;
				case kwsAnalVerns:
					vws = m_qlpi->AnalVernWss();
					break;
				case kwsVernAnals:
					vws = m_qlpi->VernAnalWss();
					break;
				default:
					CheckHr(qsda->get_MultiStringAlt(hvo, qfsp->m_flid, qfsp->m_ws, &qtss));
					CheckHr(qtss->get_Length(&cch));
					break;
				}
				for (int i = vws.Size(); --i >= 0; )
				{
					int cchT;
					CheckHr(qsda->get_MultiStringAlt(hvo, qfsp->m_flid, vws[i], &qtss));
					CheckHr(qtss->get_Length(&cchT));
					cch += cchT;
				}
				fGotData = (cch != 0 || qfsp->m_eVisibility == kFTVisAlways);
				break;
			}
		case kftRefAtomic:	// Fall through.
		case kftRefCombo:
		case kftObjRefAtomic:
			CheckHr(qsda->get_ObjectProp(hvo, qfsp->m_flid, &hvoVal));
			fGotData = (hvoVal != 0 || qfsp->m_eVisibility == kFTVisAlways);
			break;
		case kftEnum:
			// An enumeration is considered to always have a value.
			fGotData = true;
			break;
		case kftExpandable:	// Fall through.
		case kftSubItems:
		case kftObjRefSeq:
		case kftRefSeq:
			CheckHr(qsda->get_VecSize(hvo, qfsp->m_flid, &chvo));
			fGotData = (chvo != 0 || qfsp->m_eVisibility == kFTVisAlways);
			break;
		case kftDateRO:
			int64 nTime;
			CheckHr(qsda->get_TimeProp(hvo, qfsp->m_flid, &nTime));
			fGotData = (nTime != 0 || qfsp->m_eVisibility == kFTVisAlways);
			break;
		case kftGenDate:
			int gdat;
			CheckHr(qsda->get_IntProp(hvo, qfsp->m_flid, &gdat));
			fGotData = (gdat != 0 || qfsp->m_eVisibility == kFTVisAlways);
			break;
		case kftStText:
			// We should never have a structured text in a block.
			Assert(false);
			/*{ // BLOCK
				// Structured text block.
				Assert(fOnePerLine); // Can't put whole text embedded in para.
				HVO hvoText;
				int chvoPara;
				CheckHr(qsda->get_ObjectProp(hvo, qfsp->m_flid, &hvoText));
				if (!hvoText)
					break; // Show nothing for dataless fields in Doc view
				CheckHr(qsda->get_VecSize(hvoText, kflidStText_Paragraphs, &chvoPara));
				if (chvoPara == 0)
				{
					hvoNoData = hvoText;
					tagNoData = kflidStText_Paragraphs;
					break;
				}
				if (chvoPara == 1)
				{
					// If exactly one paragraph check whether empty.
					HVO hvoPara1;
					CheckHr(qsda->get_VecItem(hvoText, kflidStText_Paragraphs, 0, &hvoPara1));
					ITsStringPtr qtss;
					CheckHr(qsda->get_StringProp(hvoPara1, kflidStTxtPara_Contents, &qtss));
					int cch;
					CheckHr(qtss->get_Length(&cch));
					if (!cch)
					{
						hvoNoData = hvoPara1;
						tagNoData = kflidStTxtPara_Contents;
						Vector<HVO> vhvo;
						Vector<int> vflid;
						vhvo.Push(hvoPara1);
						vflid.Push(kflidStTxtPara_Contents);
						CheckHr(pvwenv->NoteDependency(vhvo.Begin(), vflid.Begin(), vhvo.Size()));
						break;
					}
				}
				fGotData = true;
			}*/
			break;
		case kftInteger:
			// Allow integer fields to be edited.
			int ndat;
			CheckHr(qsda->get_IntProp(hvo, qfsp->m_flid, &ndat));
			fGotData = true;
			break;
		}
		if (!fGotData)
		{
			// This is tricky. If no fields are visible, this routine does not get called again.
			// But if even one is visible, the loop stops. So neither the testing nor the not-
			// testing pass is a reliable time to note missing fields.
			// What we do is note ones before the first visible one during the test pass (so all
			// get noted then if nothing is visible) and any others in the data pass.
			if (fTest || fGeneratedAField)
				NoteFldNoData(hvoNoData, tagNoData, vhvoNoData, vtagNoData);

			if (qfsp->m_eVisibility == kFTVisIfData)
				continue;
		}
		// If we are just testing, we now know we will display something.
		if (fTest)
			return true;
		fGeneratedAField = true; // from here on, if we hit any empty fields, call NoteFldNoData

		// OK, the field has some data, or at least we want to show the label.
		// Display it.
		if (fParaOpen)
		{
			// We added at least one thing already, and are doing only one paragraph,
			// add a separator before the next.
			CheckHr(pvwenv->AddString(m_qtssFldSep));
		}
		else
		{
			// The first time we find a non-empty field we open a paragraph...
			// Or, if fOnePerLine is true, we open one for every non-empty field...
			// Except structured texts, where we need a Div to embed several paras.
			if (qfsp->m_ft != kftStText)
			{
#ifdef JohnT_10_4_2001_UseNormalAutomatically
				StrUni stuNormal = g_pszwStyleNormal;
				CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, stuNormal.Bstr()));
#endif
				if (fOnePerLine) // Need extra indent
					CheckHr(pvwenv->put_IntProperty(ktptMarginLeading, ktpvMilliPoint,
						kdzmpInch * 3 / 10));
				CheckHr(pvwenv->OpenMappedPara());
			}
			if (!fOnePerLine)
				fParaOpen = true; // Set the flag so we don't open another
		}

		if (!qfsp->m_fHideLabel && qfsp->m_ft != kftStText)
		{
			// Add a label. StText does something special to embed it in the first para
			CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
			CheckHr(pvwenv->AddString(qfsp->m_qtssLabel));
			CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
			CheckHr(pvwenv->AddString(m_qtssColon));
		}
		switch (qfsp->m_ft)
		{
		// ENHANCE JohnT: the string cases are not tested, as no examples occur in RN.
		case kftString:
			// Allow string fields to be edited.
			CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qfsp->m_stuSty.Bstr()));
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptIsEditable));
			CheckHr(pvwenv->AddStringProp(qfsp->m_flid, this));
			break;
		case kftInteger:
			// Allow integer fields to be edited.
			CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qfsp->m_stuSty.Bstr()));
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptSemiEditable));
			CheckHr(pvwenv->AddIntProp(qfsp->m_flid));
			break;
		case kftMta:
		case kftMsa:
			{
				// Allow MultiString text to be edited.
				Vector<int> vws;
				switch (qfsp->m_ws)
				{
				case kwsAnals:
				case kwsAnal:
					vws = m_qlpi->AnalWss();
					break;
				case kwsVerns:
				case kwsVern:
					vws = m_qlpi->VernWss();
					break;
				case kwsAnalVerns:
					vws = m_qlpi->AnalVernWss();
					break;
				case kwsVernAnals:
					vws = m_qlpi->VernAnalWss();
					break;
				default:
					CheckHr(qsda->get_MultiStringAlt(hvo, qfsp->m_flid, qfsp->m_ws, &qtss));
					CheckHr(qtss->get_Length(&cch));
					break;
				}
				StrUni stuLangCodeStyle(L"Language Code");
				ITsPropsFactoryPtr qtpf;
				qtpf.CreateInstance(CLSID_TsPropsFactory);
				ITsTextPropsPtr qttp;
				SmartBstr sbstrWs;
				ITsStringPtr qtss;
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				// Get the properties of the "Language Code" style for the writing system
				// which corresponds to the user's environment.
				qtpf->MakeProps(stuLangCodeStyle.Bstr(), m_wsUser, 0, &qttp);
				int cnt = vws.Size();
				for (int i = 0; i < cnt; ++i)
				{
					if (cnt > 1)
					{
						if (cnt > 1)
						{
							CheckHr(qtsf->MakeStringRgch(L" ", 1, m_wsUser, &qtss));
							CheckHr(pvwenv->AddString(qtss));
						}

						// Set qtss to a string representing the writing sys; abbr or ICULocale
						sbstrWs.Clear();
						IWritingSystemPtr qws;
						CheckHr(qwsf->get_EngineOrNull(vws[i], &qws));
						if (qws)
							CheckHr(qws->get_Abbr(m_wsUser, &sbstrWs));
						if (!sbstrWs)
							CheckHr(qwsf->GetStrFromWs(vws[i], &sbstrWs));
						CheckHr(qtsf->MakeStringRgch(sbstrWs.Chars(), sbstrWs.Length(),
							m_wsUser, &qtss));
						CheckHr(pvwenv->put_StringProperty(kspNamedStyle,
							stuLangCodeStyle.Bstr()));
						CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum,
							ktptNotEditable));
						CheckHr(pvwenv->AddString(qtss));
						CheckHr(qtsf->MakeStringRgch(L" ", 1, m_wsUser, &qtss));
						CheckHr(pvwenv->AddString(qtss));
					}
					CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qfsp->m_stuSty.Bstr()));
					CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptIsEditable));
					CheckHr(pvwenv->AddStringAltMember(qfsp->m_flid, vws[i], this));
				}
				break;
			}
		case kftExpandable:	// Fall through.
		case kftSubItems:
			// We are only prepared to handle participants here at this point.
			// Subentries are handled elsewhere, so should never come here.
			CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qfsp->m_stuSty.Bstr()));
			m_qfsp = qfsp;
			// Need the span here so the above properties apply to all items in list.
			CheckHr(pvwenv->OpenSpan());
			SetSubItems(pvwenv, qfsp->m_flid);	// Call subclass specific handler.
			CheckHr(pvwenv->CloseSpan());
			break;
		case kftRefAtomic:
		case kftRefCombo:
			CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qfsp->m_stuSty.Bstr()));
			m_qfsp = qfsp;
			CheckHr(pvwenv->AddObjProp(qfsp->m_flid, this, kfrcdPliName));
			break;
		case kftEnum:
			// ENHANCE JohnT(KenZ): This probably needs to come up with a string instead of
			// just returning an int. (JohnT: don't think we currently display any of these
			// props in doc view.)
			CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qfsp->m_stuSty.Bstr()));
			CheckHr(pvwenv->AddProp(qfsp->m_flid, this, 0));
			break;
		case kftRefSeq:
			CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qfsp->m_stuSty.Bstr()));
			m_qfsp = qfsp;
			// Need the span here so the above properties apply to all items in list.
			CheckHr(pvwenv->OpenSpan());
			CheckHr(pvwenv->AddObjVec(qfsp->m_flid, this, kfrcdTagSeq));
			CheckHr(pvwenv->CloseSpan());
			break;
		case kftObjRefAtomic:
			{
				CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qfsp->m_stuSty.Bstr()));
				CheckHr(pvwenv->AddObj(hvo, m_qRecVc, kfrRefName));
			}
			break;
		case kftObjRefSeq:
			CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qfsp->m_stuSty.Bstr()));
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
			m_qfsp = qfsp;
			// Need the span here so the above properties apply to all items in list.
			CheckHr(pvwenv->OpenSpan());
			CheckHr(pvwenv->AddObjVec(qfsp->m_flid, this, kfrcdObjRefSeq));
			CheckHr(pvwenv->CloseSpan());
			break;
		case kftDateRO:
		case kftGenDate:
			// The main work for these cases is done by DisplayVariant below, which
			// distinguishes them based on the type of value AddProp puts into the variant.
			// Hence the final (fragment) argument can just be zero.
			CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qfsp->m_stuSty.Bstr()));
			CheckHr(pvwenv->AddProp(qfsp->m_flid, this, 0));
			break;
		case kftStText:
			// We should never have a structured text in a block.
			Assert(false);
			/*{ // BLOCK
				// Structured text block.
				StVcPtr qstvc;
				qstvc.Attach(NewObj StVc(m_wsUser));
				if (!qfsp->m_fHideLabel)
				{
					ITsStrBldrPtr qtsb;
					CheckHr(qfsp->m_qtssLabel->GetBldr(&qtsb));
					int cch;
					CheckHr(qtsb->get_Length(&cch));
					CheckHr(qtsb->ReplaceTsString(cch, cch, m_qtssColon));
					int cch2;
					CheckHr(m_qtssColon->get_Length(&cch2));
					// Make whole label bold
					CheckHr(qtsb->SetIntPropValues(0, cch + cch2, ktptBold, ktpvEnum, kttvForceOn));
					ITsStringPtr qtssLabel;
					CheckHr(qtsb->GetString(&qtssLabel));
					qstvc->SetLabel(qtssLabel);
				}
				CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qfsp->m_stuSty.Bstr()));
				qstvc->SetStyle(qfsp->m_stuSty.Chars());
				// TODO KenZ: We need to compute clrBkg from qbsp->m_stuSty efficiently.
				// Since it will be different for different fields, perhaps the best approach
				// is to create a map of flid and clrBkg for each StText in the constructor.
				// See AfDeFieldEditor::Initialize for sample code.
				qstvc->SetClrBkg(::GetSysColor(COLOR_WINDOW));
//				qstvc->SetClrBkg(kclrWhite);

				// Embed it in a div to get extra indent for everything.
				CheckHr(pvwenv->put_IntProperty(kspMarginLeading, ktpvMilliPoint,
						kdzmpInch * 3 / 10));
				CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptIsEditable));
				CheckHr(pvwenv->OpenDiv());
				CheckHr(pvwenv->AddObjProp(qfsp->m_flid, qstvc, kfrText));
				CheckHr(pvwenv->CloseDiv());
			}*/
			break;
		}
		if (fOnePerLine && qfsp->m_ft != kftStText)
			pvwenv->CloseParagraph();
	}
	if (fParaOpen)
	{
		// There was some data, add a terminator and close the paragraph.
		// Commented out; for now we don't want terminators.
/*
		if (cfsp > 1 || !prgqfsp[0]->m_fHideLabel)
			// If there is no label, closing punctuation looks weird.
			// Review (SharonC): Do we need a separate parameter to control this?
		{
			CheckHr(pvwenv->AddString(m_qtssBlockEnd));
		}
*/
		CheckHr(pvwenv->CloseParagraph());
	}
	return fGeneratedAField;
}


/*----------------------------------------------------------------------------------------------
	Special stuff that should be done before each visible field.
	If cVisFld is already positive, there is nothing special to do.
	If cVisFld is zero, we need to
		(1) Open a Div, setting the requested left indent if any
		(2) If a subrecord, open a 2-column table so as to put a number in the
			left margin aligned with the first field. Set everything up so the field
			contents will be the second table.
----------------------------------------------------------------------------------------------*/
void VwCustDocVc::FirstFldCheckPre(IVwEnv * pvwenv, int cVisFld, ITsTextProps * pttpDiv)
{
	if (cVisFld)
		return;
	// OK, this is the first visible field.
	pvwenv->put_Props(pttpDiv);
	pvwenv->OpenDiv();
	if (pttpDiv == m_qttpSub)
	{
		// Enclose the first visible field in a table, as the second cell, where the first cell
		// contains the record number.
		// First compute the string we need to display.
		ITsStringPtr qtssOutline;
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		StrUni stu;
		if (m_ons != konsNone)
		{
			// Use this for subentry numbers. It gets an outline number from the cache.
			HVO hvo;
			CheckHr(pvwenv->CurrentObject(&hvo));
			CustViewDaPtr qcvd;
			m_qlpi->GetDataAccess(&qcvd);
			AssertPtr(qcvd);
			bool fFinalDot = m_ons == konsNumDot;
			SmartBstr sbstr = stu.Bstr();
			qcvd->GetOutlineNumber(hvo, GetSubItemFlid(), fFinalDot, &sbstr);
			stu = sbstr.Chars();
		}
		CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &qtssOutline));

		// Now make the table
		VwLength vlTab = {10000, kunPercent100}; // table uses 100% of available width
		// Extra tag column for subitem uses up extra indent
		VwLength vlCol1 = {kdzmpInch * 3 / 10, kunPoint1000};
		VwLength vlCol2 = {1, kunRelative}; // only relative col, uses rest of space

		// Table has negative indent relative to the division, putting the label number
		// in the left margin.
		CheckHr(pvwenv->put_IntProperty(ktptLeadingIndent, ktpvMilliPoint,
			-kdzmpInch * 3 / 10));

		CheckHr(pvwenv->OpenTable(2, // columns
			vlTab,
			0, // border thickness
			kvaLeft, // default alignment
			kvfpVoid, // no border
			kvrlNone, // no rules between cells
			0, // no forced space between cells
			0, // no padding inside cells
			false));
		// Specify column widths. The first argument is #cols, not col index.
		CheckHr(pvwenv->MakeColumns(1, vlCol1));
		CheckHr(pvwenv->MakeColumns(1, vlCol2));

		CheckHr(pvwenv->OpenTableBody());
		CheckHr(pvwenv->OpenTableRow());

		CheckHr(pvwenv->OpenTableCell(1,1));
		CheckHr(pvwenv->AddString(qtssOutline));
		CheckHr(pvwenv->CloseTableCell());
		CheckHr(pvwenv->OpenTableCell(1,1)); // Filled by body of field.
	}
}

/*----------------------------------------------------------------------------------------------
	Special stuff that should be done after each visible field.
	If cVisFld is already positive, there is nothing special to do.
	If cVisFld is zero, we need to Close the table cell, row, and table that was opened in
	step (2) above.
	In either case we need to increment the count.
----------------------------------------------------------------------------------------------*/
void VwCustDocVc::FirstFldCheckPost(IVwEnv * pvwenv, int & cVisFld, ITsTextProps * pttpDiv)
{
	if (pttpDiv == m_qttpSub && !cVisFld)
	{
		pvwenv->CloseTableCell();
		pvwenv->CloseTableRow();
		pvwenv->CloseTableBody();
		pvwenv->CloseTable();
	}
	cVisFld++;
}


/*----------------------------------------------------------------------------------------------
	The bulk of kfrcdMainItem and kfrcdSubItem.
----------------------------------------------------------------------------------------------*/
void VwCustDocVc::BodyOfRecord(IVwEnv * pvwenv, HVO hvo, ITsTextProps * pttpDiv, int nLevel)
{
	int ibspMin = 0; // first block to process normally
	int clsid;
	ISilDataAccessPtr qsda;
	CheckHr(pvwenv->get_DataAccess(&qsda));
	CheckHr(qsda->get_IntProp(hvo, kflidCmObject_Class, &clsid));
	RecordSpecPtr qrsp;
	ClsLevel clev(clsid, nLevel);
	m_quvs->m_hmclevrsp.Retrieve(clev, qrsp);
	if (!qrsp || qrsp->m_vqbsp.Size() == 0)
		return; // Nothing requested to show.

	// Count of visible fields we have shown. We do special things when we encounter the
	// first.
	int cVisFld = 0;
	// Vectors used to maintain lists of tags of properties that were not displayed because
	// no data. Hence, we need to set up special monitoring on them.
	IntVec vtagNoData;
	HvoVec vhvoNoData;

	int cbsp = qrsp->m_vqbsp.Size();
	for (int ibsp = ibspMin; ibsp < cbsp; ibsp++)
	{
		BlockSpecPtr qbsp = qrsp->m_vqbsp[ibsp];
		// PM doesn't provide a way to set/clear visibility on groups, so
		// we don't want to check the status of that flag here.
		if (qbsp->m_eVisibility == kFTVisNever && qbsp->m_ft != kftGroup
			&& qbsp->m_ft != kftGroupOnePerLine)
		{
			continue;
		}
		switch (qbsp->m_ft)
		{
		case kftTitleGroup:
			{ // BLOCK
				Assert(qbsp->m_vqfsp.Size() == 1);
				FldSpecPtr qfspType = qbsp->m_vqfsp[0];
				if (qfspType->m_eVisibility == kFTVisNever)
				{
					// Need to check visibility of the whole group.
					ITsStringPtr qtss;
					CheckHr(qsda->get_StringProp(hvo, qbsp->m_flid, &qtss));
					int cch;
					CheckHr(qtss->get_Length(&cch));
					if (!cch)
					{
						NoteFldNoData(hvo, qbsp->m_flid, vhvoNoData, vtagNoData);
						break;
					}
					if (!qbsp->m_fHideLabel)
					{
						// Add a label.
						int cch = 0;
						if (qbsp->m_qtssLabel)
							CheckHr(qbsp->m_qtssLabel->get_Length(&cch));
						if (cch)
						{
							ITsStrBldrPtr qtsb;
							ITsStringPtr qtss;
							CheckHr(qbsp->m_qtssLabel->GetBldr(&qtsb));
							CheckHr(qtsb->ReplaceTsString(cch, cch, m_qtssColon));
							CheckHr(qtsb->GetString(&qtss));
							CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
							CheckHr(pvwenv->AddString(qtss));
						}
					}
					// Showing title, but not type; nothing fancy.
					FirstFldCheckPre(pvwenv, cVisFld, pttpDiv);
					// Allow string fields to be edited.
					CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qbsp->m_stuSty.Bstr()));
					CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptIsEditable));
					CheckHr(pvwenv->AddStringProp(qbsp->m_flid, this));
					FirstFldCheckPost(pvwenv, cVisFld, pttpDiv);
					break;
				}
				else
				{
					FirstFldCheckPre(pvwenv, cVisFld, pttpDiv);
					// Figure the width of the type string.
					ITsStringPtr qtssType = GetTypeForClsid(clsid);
					AssertPtr(qtssType);
					int dxmpType;
					int dymp; // dummy, not used
					CheckHr(pvwenv->get_StringWidth(qtssType, NULL, &dxmpType, &dymp));

					VwLength vlTab = {10000, kunPercent100}; // table uses 100% of available width
					VwLength vlCol1 = {1, kunRelative}; // only relative col, uses rest of space
					// Last column is big enough for type label plus 1/4 inch indent
					// (+ 3 for rounding, to be sure it fits)
					VwLength vlCol2 = {dxmpType + kdzmpInch / 4 + 3, kunPoint1000};

					CheckHr(pvwenv->OpenTable(2, // columns
						vlTab,
						0, // border thickness
						kvaLeft, // default alignment
						kvfpVoid, // no border
						kvrlNone, // no rules between cells
						0, // no forced space between cells
						0, // no padding inside cells
						false));
					// Specify column widths. The first argument is #cols, not col index.
					CheckHr(pvwenv->MakeColumns(1, vlCol1));
					CheckHr(pvwenv->MakeColumns(1, vlCol2));

					CheckHr(pvwenv->OpenTableBody());
					CheckHr(pvwenv->OpenTableRow());

					CheckHr(pvwenv->OpenTableCell(1,1));
					if (!qbsp->m_fHideLabel)
					{
						// Add a label.
						int cch = 0;
						if (qbsp->m_qtssLabel)
							CheckHr(qbsp->m_qtssLabel->get_Length(&cch));
						if (cch)
						{
							ITsStrBldrPtr qtsb;
							ITsStringPtr qtss;
							CheckHr(qbsp->m_qtssLabel->GetBldr(&qtsb));
							CheckHr(qtsb->ReplaceTsString(cch, cch, m_qtssColon));
							CheckHr(qtsb->GetString(&qtss));
							CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
							CheckHr(pvwenv->AddString(qtss));
						}
					}
					//CheckHr(pvwenv->put_IntProperty(ktptFontSize, ktpvRelative, 12000));
					//CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
					// Allow titles to be edited.
					CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qbsp->m_stuSty.Bstr()));
					CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptIsEditable));
					CheckHr(pvwenv->AddStringProp(qbsp->m_flid, this));
					CheckHr(pvwenv->CloseTableCell());

					CheckHr(pvwenv->OpenTableCell(1,1));
					// Indent the string by just the amount we allowed in figuring the
					// column width, so it ends up right justified and there is some gap
					// between it and the title.
					CheckHr(pvwenv->put_IntProperty(ktptLeadingIndent, ktpvMilliPoint,
						kdzmpInch / 4));
					CheckHr(pvwenv->AddString(qtssType));
					CheckHr(pvwenv->CloseTableCell());

					CheckHr(pvwenv->CloseTableRow());
					CheckHr(pvwenv->CloseTableBody());
					CheckHr(pvwenv->CloseTable());

					FirstFldCheckPost(pvwenv, cVisFld, pttpDiv);
				}
			}
			break;
		case kftGroup:
		case kftGroupOnePerLine:
			{ // BLOCK for classifications and history
				bool fOnePerLine = qbsp->m_ft == kftGroupOnePerLine;
				if (!DisplayFields(qbsp->m_vqfsp.Begin(), qbsp->m_vqfsp.Size(), pvwenv, hvo,
					fOnePerLine, vhvoNoData, vtagNoData, true))
					break;
				FirstFldCheckPre(pvwenv, cVisFld, pttpDiv);
				if (fOnePerLine && !qbsp->m_fHideLabel)
				{
					CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
					CheckHr(pvwenv->OpenParagraph());
					CheckHr(pvwenv->AddString(qbsp->m_qtssLabel));
					CheckHr(pvwenv->CloseParagraph());
				}
				DisplayFields(qbsp->m_vqfsp.Begin(), qbsp->m_vqfsp.Size(), pvwenv, hvo,
					fOnePerLine, vhvoNoData, vtagNoData);
				FirstFldCheckPost(pvwenv, cVisFld, pttpDiv);
			}
			break;
		case kftStText:
			{ // BLOCK
				// Structured text block. Use standard view constructor, if non-empty.
				HVO hvoText;
				int chvoPara;
				CheckHr(qsda->get_ObjectProp(hvo, qbsp->m_flid, &hvoText));
				if (!hvoText)
				{
					// Show nothing for dataless fields in Doc view. This shouldn't really
					// happen, because we supposedly initialize all entries with whatever
					// StText fields are needed.
					NoteFldNoData(hvo, qbsp->m_flid, vhvoNoData, vtagNoData);
					break;
				}
				CheckHr(qsda->get_VecSize(hvoText, kflidStText_Paragraphs, &chvoPara));
				if (chvoPara == 0)
				{
					NoteFldNoData(hvoText, kflidStText_Paragraphs, vhvoNoData, vtagNoData);
					if (qbsp->m_eVisibility == kFTVisIfData)
						break;
				}
				else if (chvoPara == 1)
				{
					// If exactly one paragraph check whether empty.
					HVO hvoPara1;
					CheckHr(qsda->get_VecItem(hvoText, kflidStText_Paragraphs, 0, &hvoPara1));
					ITsStringPtr qtss;
					CheckHr(qsda->get_StringProp(hvoPara1, kflidStTxtPara_Contents, &qtss));
					int cch;
					CheckHr(qtss->get_Length(&cch));
					if (!cch)
					{
						NoteFldNoData(hvoPara1, kflidStTxtPara_Contents, vhvoNoData, vtagNoData);
						if (qbsp->m_eVisibility == kFTVisIfData)
							break;
					}
				}
				// OK, if we get this far the field is considered non-empty; go ahead and show it.
				FirstFldCheckPre(pvwenv, cVisFld, pttpDiv);

				// If its label is to be shown we need a special StVc to show the heading.
				StVcPtr qstvc;
				qstvc.Attach(NewObj StVc(m_wsUser));
				if (!qbsp->m_fHideLabel)
				{
#ifdef JohnT_10_25_01_EmbedLabel
					ITsStrBldrPtr qtsb;
					CheckHr(qbsp->m_qtssLabel->GetBldr(&qtsb));
					int cch;
					CheckHr(qtsb->get_Length(&cch));
					CheckHr(qtsb->ReplaceTsString(cch, cch, m_qtssColon));
					int cch2;
					CheckHr(m_qtssColon->get_Length(&cch2));
					// Make whole label bold
					CheckHr(qtsb->SetIntPropValues(0, cch + cch2, ktptBold, ktpvEnum, kttvForceOn));
					ITsStringPtr qtssLabel;
					CheckHr(qtsb->GetString(&qtssLabel));
					qstvc->SetLabel(qtssLabel);
#else
					int cch;
					CheckHr(qbsp->m_qtssLabel->get_Length(&cch));
					if (cch)
					{
						CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
						CheckHr(pvwenv->OpenParagraph());
						CheckHr(pvwenv->AddString(qbsp->m_qtssLabel));
						CheckHr(pvwenv->AddString(m_qtssColon));
						CheckHr(pvwenv->CloseParagraph());
					}
#endif
				}

				// Don't do this here! It would only affect the first paragraph...
				CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptIsEditable));
				// Ediable needs to apply to the whole StText, not just the first paragaph.
				CheckHr(pvwenv->OpenDiv());
				qstvc->SetStyle(qbsp->m_stuSty.Chars());
				// TODO KenZ: We need to compute clrBkg from qbsp->m_stuSty efficiently.
				// Since it will be different for different fields, perhaps the best approach
				// is to create a map of flid and clrBkg for each StText in the constructor.
				// See AfDeFieldEditor::Initialize for sample code.
				//qstvc->SetClrBkg(kclrWhite);
				qstvc->SetClrBkg(::GetSysColor(COLOR_WINDOW));
				CheckHr(pvwenv->AddObjProp(qbsp->m_flid, qstvc, kfrText));
				CheckHr(pvwenv->CloseDiv());
				FirstFldCheckPost(pvwenv, cVisFld, pttpDiv);
			}
			break;
		default:
			{
				if (qbsp->m_flid == m_tagSubItems)
				{
					// Show subentries, if any.
					int chvo;
					CheckHr(qsda->get_VecSize(hvo, m_tagSubItems, &chvo));
					if (chvo)
					{
						m_ons = qbsp->m_ons; // Save the flag for the first field.
						FirstFldCheckPre(pvwenv, cVisFld, pttpDiv);
						// Add the heading string, with an extra 0.1 inch indent.
#ifdef JohnT_10_4_2001_UseNormalAutomatically
						StrUni stuNormal = g_pszwStyleNormal;
						CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, stuNormal.Bstr()));
#endif
						if (!qbsp->m_fHideLabel)
						{
							CheckHr(pvwenv->put_IntProperty(ktptLeadingIndent, ktpvMilliPoint,
								kdzmpInch / 10));
							CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
							CheckHr(pvwenv->AddString(qbsp->m_qtssLabel));
						}
						// Make a Div for the sub-items, with  0.2 inch extra indent
						// TODO JohnT: set some limit to prevent more than half the window
						// width going to indentation. (Maybe maxIndentPercent is another
						// property?)
						CheckHr(pvwenv->put_IntProperty(ktptLeadingIndent, ktpvMilliPoint,
							kdzmpInch / 5));
						CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
						CheckHr(pvwenv->OpenDiv());
						// This sets up a default, so we can only edit where we explicitly
						// permit it.
						CheckHr(pvwenv->AddObjVecItems(m_tagSubItems, this, kfrcdSubItem));
						CheckHr(pvwenv->CloseDiv());
						FirstFldCheckPost(pvwenv, cVisFld, pttpDiv);
					}
					else
					{
						NoteFldNoData(hvo, qbsp->m_flid, vhvoNoData, vtagNoData);
						break;
					}
					break;
				}
				// Treat like a list with just one field
				// Tricky way to get a pointer to the smart pointer.
				// ENHANCE JohnT: We should probably clean this up some way. Most likely, we will
				// want to combine FldSpec and BlockSpec, thus simplying this interface.
				if (!DisplayFields((FldSpecPtr *)(qrsp->m_vqbsp.Begin() + ibsp), 1, pvwenv, hvo,
					false, vhvoNoData, vtagNoData, true))
				{
					break;
				}
				FirstFldCheckPre(pvwenv, cVisFld, pttpDiv);
				DisplayFields((FldSpecPtr *)(qrsp->m_vqbsp.Begin() + ibsp), 1, pvwenv, hvo,
					false, vhvoNoData, vtagNoData);
				FirstFldCheckPost(pvwenv, cVisFld, pttpDiv);
				break;
			}
		}
	}
	// If there were any invisible fields we need to ensure regeneration if the data changes.
	if (vhvoNoData.Size() > 0)
	{
		CheckHr(pvwenv->NoteDependency(vhvoNoData.Begin(), vtagNoData.Begin(), vhvoNoData.Size()));
	}

	if (cVisFld)
	{
		// Close the div that we opened when we found the first visible field.
		CheckHr(pvwenv->CloseDiv());
	}
}


//:>********************************************************************************************
//:>	VwCustBrowseVc Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
VwCustBrowseVc::VwCustBrowseVc(UserViewSpec * puvs, AfLpInfo * plpi,
	int dypHeader, int nMaxLines, HVO hvoRootObjId,
	int tagTitle, int tagSubItems, int tagRootItems)
	: VwCustomVc(puvs, plpi, tagTitle, tagSubItems, tagRootItems)
{
	m_dypHeader = dypHeader;
	m_nMaxLines = nMaxLines;
	m_fIgnoreHier = m_quvs->m_fIgnorHier;
	m_hvoRootObjId = hvoRootObjId;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
VwCustBrowseVc::~VwCustBrowseVc()
{
}

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.
	Subclasses should override this to handle interesting things for that class.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCustBrowseVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	// Constant fragments
	switch (frag)
	{
	case kfrcdRoot:
		{
			// Two reasons: this sets up a default, so we can only edit where we explicitly
			// permit it. Also, it is needed to prevent the system trying to insert paragraphs
			// when someone hits return in fields like title.
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
			// And this makes our current analysis writing system the default for all empty,
			// unencoded strings unless overridden.
			CheckHr(pvwenv->put_IntProperty(ktptBaseWs, ktpvDefault, m_qlpi->AnalWs()));
			// If this is the top pane, insert an extra space at the top for the
			// embedded header control.
			HWND hwndDesk = ::GetDesktopWindow();
			HDC hdc = ::GetDC(hwndDesk);
			int ypLogPixels = ::GetDeviceCaps(hdc, LOGPIXELSY);
			int iSuccess;
			iSuccess = ::ReleaseDC(hwndDesk, hdc);
			Assert(iSuccess);
			int mp = m_dypHeader * 72000 / ypLogPixels;
			CheckHr(pvwenv->put_IntProperty(ktptMarginTop, ktpvMilliPoint, mp));
			CheckHr(pvwenv->OpenDiv()); // above properties should apply to all, not just first
			CheckHr(pvwenv->AddLazyVecItems(m_tagRootItems, this, kfrcdMainItem));
			CheckHr(pvwenv->CloseDiv());
		}
		break;
	case kfrcdSubItem:
		BodyOfRecord(pvwenv, hvo, m_qttpSub);
		break;
	case kfrcdMainItem:
		BodyOfRecord(pvwenv, hvo, m_qttpMain);
		break;
	default:
		return SuperClass::Display(pvwenv, hvo, frag);
		break;
	}

	return S_OK;

	END_COM_METHOD(g_factCustBrowse, IID_IVwViewConstructor);
}

/*----------------------------------------------------------------------------------------------
	This routine is used to estimate the height of an item. The item will be one of
	those you have added to the environment using AddLazyItems. Note that the calling code
	does NOT ensure that data for displaying the item in question has been loaded.
	The first three arguments are as for Display, that is, you are being asked to estimate
	how much vertical space is needed to display this item in the available width.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwCustBrowseVc::EstimateHeight(HVO hvo, int frag, int dxAvailWidth, int * pdyHeight)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pdyHeight);
	// Guess an entry is 16 pixels per line. Zero means all lines, so in that case, guess a
	// largish number.
	*pdyHeight = 16 * (m_nMaxLines ? m_nMaxLines : 20);

	END_COM_METHOD(g_factCustBrowse, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	This routine is used to create the bitmaps used in the left column of the browse view.
----------------------------------------------------------------------------------------------*/
void VwCustBrowseVc::CreateBitmap(HIMAGELIST himl, int iimage, COLORREF clrBkg, IPicture ** ppict)
{
	int kdxpImage;
	int kdypImage;

	// First, get the width and height of the bitmap image so a memory bitmap
	// can be created.
	::ImageList_GetIconSize(himl, &kdxpImage, &kdypImage);

	HDC hdc = ::GetDC(0);
	HDC hdcMem = AfGdi::CreateCompatibleDC(hdc);
	HBITMAP hbmpImage = AfGdi::CreateCompatibleBitmap(hdc, kdxpImage, kdypImage);
	::ReleaseDC(0, hdc);

	{ // Block: spal must go out of scope before ::DeleteDc()
		SmartPalette spal(hdcMem);

		HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmpImage);

		// If clrBkg is non negative, it means the caller wants to use it as the background
		// color and draw the bitmap transparently. (It's up to the caller to have given himl
		// a mask color for drawing transparently.) If clrImgBkg is negative, it means the
		// caller wants to draw the bitmap normally (i.e. without any transparent areas).
		if (clrBkg >= 0)
		{
			Rect rc(0, 0, kdxpImage, kdypImage);
			AfGfx::FillSolidRect(hdcMem, rc, clrBkg);
			::ImageList_Draw(himl, iimage, hdcMem, 0, 0, ILD_TRANSPARENT);
		}
		else
			::ImageList_Draw(himl, iimage, hdcMem, 0, 0, ILD_NORMAL);

		AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);
	}

	BOOL fSuccess;
	fSuccess = AfGdi::DeleteDC(hdcMem);
	Assert(fSuccess);

	PICTDESC pictd = {sizeof(PICTDESC), PICTYPE_BITMAP};
	pictd.bmp.hbitmap = hbmpImage;
	pictd.bmp.hpal = 0;
	CheckHr(OleCreatePictureIndirect(&pictd, IID_IPicture, true, (void **) ppict));
}
