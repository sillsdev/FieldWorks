/*----------------------------------------------------------------------------------------------
Copyright 2000. 2002, SIL International. All rights reserved.

File: RnCustBrowseVc.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a customiseable view constructor for browse views.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#include "Vector_i.cpp"
#include "GpHashMap_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

static DummyFactory g_fact(_T("Sil.Notebk.RnCustBrowseVc"));

/*----------------------------------------------------------------------------------------------
	Construct the view constructor. Pass the UserViewSpec of the view it is for, which allows it
	to find its block specs.
	ENHANCE JohnT: This is a rather low level object to know about the application and its list
	of views. Should we just pass in the list of block specs?
----------------------------------------------------------------------------------------------*/
RnCustBrowseVc::RnCustBrowseVc(UserViewSpec * puvs, AfLpInfo * plpi,
	int dypHeader, int nMaxLines, HVO hvoRootObjId)
	: VwCustBrowseVc(puvs, plpi, dypHeader, nMaxLines, hvoRootObjId,
		kflidRnGenericRec_Title, kflidRnGenericRec_SubRecords)
{
	// Get the dummy display RecordSpec that combines both entries.
	ClsLevel clevKey(0, 0);
	m_quvs->m_hmclevrsp.Retrieve(clevKey, m_qrsp);

	HBITMAP hbmp = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(),
		MAKEINTRESOURCE(kridRnEntryType));
	if (!hbmp)
		ThrowHr(WarnHr(E_FAIL));
	// Load an image list from a resource
	HIMAGELIST himl = AfGdi::ImageList_Create(16, 15, ILC_COLORDDB | ILC_MASK, 0, 0);
	if (!himl)
		ThrowHr(WarnHr(E_FAIL));
	// Add the bitmap with a mask so it can be drawn transparently.
	::ImageList_AddMasked(himl, hbmp, kclrPink);
	AfGdi::DeleteObjectBitmap(hbmp);
	for (int i = 0; i < kentypLim; i++)
		CreateBitmap(himl, i, ::GetSysColor(COLOR_3DFACE), &m_qpicRnEntryType[i]);
	AfGdi::ImageList_Destroy(himl);

	RnRecVc * prrvc = NewObj RnRecVc;
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	prrvc->SetDbInfo(pdbi);
	m_qRecVc.Attach(prrvc);
}


RnCustBrowseVc::~RnCustBrowseVc()
{
}

/*----------------------------------------------------------------------------------------------
	In a couple of cases we're displaying something...typically a possibility name...and
	exactly how to display it depends on the FldSpec for the next OUTER property (say, the
	anthropology codes property). This method tracks down the appropriate FldSpec.

	In an earlier version of the code, this was saved in a member variable by the code that
	displays the outer property and calls AddObjVec or similar. This works when the view first
	comes up, but when regenerating just one or two items that changed, the execution path
	does not go through the case for the containing property; the AddObjVec call is made
	independently by the regenerate code. Therefore we have to be able to figure it out
	within the particular case.

	The code assumes we are doing exactly what I described: that we are one level down from
	the property whose fldspec we need.
----------------------------------------------------------------------------------------------*/
BlockSpec * RnCustBrowseVc::GetFldSpecOutOne(IVwEnv * pvwenv)
{
	int clev;
	CheckHr(pvwenv->get_EmbeddingLevel(&clev));
	Assert(clev >= 1);
	HVO hvoDum;
	PropTag tag;
	int ihvoDum;
	CheckHr(pvwenv->GetOuterObject(clev - 1, &hvoDum, &tag, &ihvoDum));
	// Now find the fld spec for that tag.
	// When we are showing fields on RnRoledPartic, we would normally use a different
	// RecordSpec to find the FldSpec, but in the case of the Browse view, we don't allow
	// the user to actually change properties on those FieldSpecs, so instead, we use the
	// FldSpec on the RnEvent, which is what the user is actually modifying.
	if (tag == kflidRnRoledPartic_Participants || tag == kflidRnRoledPartic_Role)
		tag = kflidRnEvent_Participants;
	int ccols = m_qrsp->m_vqbsp.Size();
	for (int icol = 0; icol < ccols; icol++)
	{
		BlockSpec * pbsp = m_qrsp->m_vqbsp[icol];
		if (pbsp->m_flid == tag)
			return pbsp;
	}
	Assert(false);
	return m_qrsp->m_vqbsp[0]; // probably not too destructive if it happens in a release build.
}

/*----------------------------------------------------------------------------------------------
	Load data needed to display the specified objects using the specified fragment.
	This is called before attempting to Display an item that has been listed for lazy display
	using AddLazyItems. It may be used to load the necessary data into the DataAccess object.
	If you are not using AddLazyItems this method may be left unimplemented.
	If you pre-load all the data, it should trivially succeed (i.e., without doing anything).
	(That is the case here, for the moment.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnCustBrowseVc::LoadDataFor(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
	int tag, int frag, int ihvoMin)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);
	ChkComArrayArg(prghvo, chvo);
	CheckHr(SuperClass::LoadDataFor(pvwenv, prghvo, chvo, hvoParent, tag, frag, ihvoMin));
	FixEmptyStrings(prghvo, chvo, kflidRnGenericRec_Title, m_qlpi->AnalWs());
	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.
	Here an RnResearchNbk is displayed by displaying its Records.
	So far we only handle records that are RnEvents.
	An RnEvent is displayed according to the stored specifications.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnCustBrowseVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	bool fRole = false;
	HVO hvoOuter;
	int ihvoOuter, tagOuter, chvoOuter;
	ISilDataAccessPtr qsda;
	// Constant fragments
	switch (frag)
	{
	case kfrcdPliRole:
		fRole = true;
	case kfrcdPliNameOnePrimary:
		// Get the index of the record in the top-level list.
		CheckHr(pvwenv->GetOuterObject(0, &hvoOuter, &tagOuter, &ihvoOuter));
		CheckHr(pvwenv->get_DataAccess(&qsda));
		CheckHr(qsda->get_VecSize(hvoOuter, tagOuter, &chvoOuter));
		// If we have a sort key array, and it is the right size, then we can get the
		// current primary key from it. If the current hvo is NOT the primary key,
		// make it grey.
		if (m_pvskhSortKeys != NULL && chvoOuter == m_pvskhSortKeys->Size()
			&& hvo != (*m_pvskhSortKeys)[ihvoOuter].m_hvoPrimary)
		{
			CheckHr(pvwenv->put_IntProperty(ktptForeColor, ktpvDefault, kclrGray50));
		}
		// FALL THROUGH
	case kfrcdPliNameOne:
		// Display a choice name in at most one line.
		CheckHr(pvwenv->put_IntProperty(kspMaxLines, ktpvDefault, 1));
		// FALL THROUGH
	case kfrcdPliName:
		{
			// Display a choice name (may wrap unless falling through from kfrdcCliNameOne)
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));

			if (!hvo)
				return S_OK; // Nothing to display.
			FldSpec * pfsp = GetFldSpecOutOne(pvwenv);
			PossListInfoPtr qpli;
			PossItemInfo * ppii = NULL;
			m_qlpi->GetPossListAndItem(hvo, pfsp->m_ws, &ppii, &qpli);
			AssertPtr(ppii);
			StrUni stu;
			PossNameType pnt = fRole ? kpntName : pfsp->m_pnt;
			if (pfsp->m_fHier)
				ppii->GetHierName(qpli, stu, pnt);
			else
				ppii->GetName(stu, pnt);
			ITsStringPtr qtss;
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			AssertPtr(qtsf);
			int ws;
			ws = ppii->GetWs();
			qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws,	&qtss);
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
			CheckHr(pvwenv->put_StringProperty(kspNamedStyle, pfsp->m_stuSty.Bstr()));
			CheckHr(pvwenv->AddString(qtss));
			break;
		}
	case kfrcdObjNameOne:
		// Display an object reference name in at most one line.
		CheckHr(pvwenv->put_IntProperty(kspMaxLines, ktpvDefault, 1));
		// FALL THROUGH
	case kfrcdObjName:
		{
			// Display a an object reference name (may wrap unless falling through)
			if (!hvo)
				return S_OK; // Nothing to display.
			FldSpec * pfsp = GetFldSpecOutOne(pvwenv);
			CheckHr(pvwenv->put_StringProperty(kspNamedStyle, pfsp->m_stuSty.Bstr()));
			CheckHr(pvwenv->OpenMappedPara());
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
			CheckHr(pvwenv->AddObj(hvo, m_qRecVc, kfrRefName));
			CheckHr(pvwenv->CloseParagraph());
			break;
		}
	case kfrcdRpsParticipants:
		CheckHr(pvwenv->AddObjVecItems(kflidRnRoledPartic_Participants, this,
			kfrcdPliNameOne));
		break;
	default:
		return SuperClass::Display(pvwenv, hvo, frag);
	}

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}

/*----------------------------------------------------------------------------------------------
	Process the vector of RnRoledPartic.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnCustBrowseVc::DisplayVec(IVwEnv * pvwenv, HVO hvo, int tag, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	switch (frag)
	{
	case kfrcdEvnParticipants:
		{
			ISilDataAccessPtr qsda;
			CheckHr(pvwenv->get_DataAccess(&qsda));
			int chvo;
			CheckHr(qsda->get_VecSize(hvo, tag, &chvo));
			// Make sure the unspecified item is first.
			Vector<HVO> vhvo;
			int ihvo;
			HVO hvoRpar;
			for (ihvo = 0; ihvo < chvo; ++ihvo)
			{
				CheckHr(qsda->get_VecItem(hvo, tag, ihvo, &hvoRpar));
				int chvoT;
				CheckHr(qsda->get_VecSize(hvoRpar, kflidRnRoledPartic_Participants,
					&chvoT));
				if (!chvoT)
					continue; // Skip roles with no participants.
				HVO hvoRole;
				CheckHr(qsda->get_ObjectProp(hvoRpar, kflidRnRoledPartic_Role,
					&hvoRole));
				if (hvoRole)
					vhvo.Push(hvoRpar);
				else
					vhvo.Insert(0, hvoRpar);
			}
			// Now add information from each roled participant.
			chvo = vhvo.Size();
			for (ihvo = 0; ihvo < chvo; ihvo++)
			{
				HVO hvoRole;
				CheckHr(qsda->get_ObjectProp(vhvo[ihvo], kflidRnRoledPartic_Role,
					&hvoRole));
				if (hvoRole)
				{
					CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
					CheckHr(pvwenv->put_IntProperty(ktptItalic, ktpvEnum, kttvForceOn));
					CheckHr(pvwenv->AddObj(hvoRole, this, kfrcdPliRole));
				}
				// Output the people for this role. We verified above that there are some.
				CheckHr(pvwenv->AddObj(vhvo[ihvo], this, kfrcdRpsParticipants));
			}
			break;
		}
	default:
		// Superclass to handle the others.
		return SuperClass::DisplayVec(pvwenv, hvo, tag, frag);
		break;
	}

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------
	The bulk of kfrcdMainItem
----------------------------------------------------------------------------------------------*/
void RnCustBrowseVc::BodyOfRecord(IVwEnv * pvwenv, HVO hvo, ITsTextProps * pttpDiv, int nLevel)
{
	ISilDataAccessPtr qsda;
	CheckHr(pvwenv->get_DataAccess(&qsda));
	AssertPtr(qsda);
	ILgWritingSystemFactoryPtr qwsf;
	m_qlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);

	HWND hwndDesk = ::GetDesktopWindow();
	HDC hdc = ::GetDC(hwndDesk);
	int ypLogPixels = ::GetDeviceCaps(hdc, LOGPIXELSY);
	int iSuccess;
	iSuccess = ::ReleaseDC(hwndDesk, hdc);
	Assert(iSuccess);
	ITsStringPtr qtss;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUniBuf stub;

	// We start at a width of 21 which is the width of the left fixed column
	int dxpTable = 21;
	Rect rc;
	int ccols = m_qrsp->m_vqbsp.Size();
	for (int icol = 0; icol < ccols; icol++)
		dxpTable += m_qrsp->m_vqbsp[icol]->m_dxpColumn;

	VwLength vlTab = { dxpTable * 72000 / ypLogPixels, kunPoint1000 };

	// We add one extra column for the left fixed column
	CheckHr(pvwenv->OpenTable(ccols + 1,
		vlTab,
		72000/96, // border thickness about a pixel
		kvaLeft, // default alignment
		kvfpBelow, // border below table only
		kvrlRows, // rules between rows only (to be used when subentry expanded)
		0, // no forced space between cells
		72000 * 2 / 96, // 2 pixels padding inside cells
		false));
	// Specify column widths. The first argument is #cols, not col index.
	// The tag column only occurs at all if its width is non-zero.

	VwLength vl = {15000, kunPoint1000};
	CheckHr(pvwenv->MakeColumns(1, vl));

	for (int icol = 0; icol < ccols; icol++)
	{
		VwLength vl = { m_qrsp->m_vqbsp[icol]->m_dxpColumn * 72000 / ypLogPixels,
			kunPoint1000 };
		CheckHr(pvwenv->MakeColumns(1, vl));
	}

	CheckHr(pvwenv->OpenTableBody());
	CheckHr(pvwenv->OpenTableRow());

	CheckHr(pvwenv->put_IntProperty(kspMaxLines, ktpvDefault, m_nMaxLines));

	CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
	CheckHr(pvwenv->put_IntProperty(ktptBackColor, ktpvDefault,
		::GetSysColor(COLOR_3DFACE)));
	CheckHr(pvwenv->OpenTableCell(1,1));
	// Add Entry Type column
	CustViewDaPtr qcvd = dynamic_cast<CustViewDa*>(qsda.Ptr());
	int clsid;
	qcvd->get_ObjClid(hvo, &clsid);
	HVO hvoOwn;
	qcvd->get_ObjOwner(hvo, &hvoOwn);
	if (hvoOwn == m_hvoRootObjId)
	{
		if (clsid == kclidRnEvent)
			// Event entry
			CheckHr(pvwenv->AddPicture(m_qpicRnEntryType[kentypEvent], ktagNotAnAttr, 0, 0));
		else if (clsid == kclidRnAnalysis)
			// Event analysis
			CheckHr(pvwenv->AddPicture(m_qpicRnEntryType[kentypAnal], ktagNotAnAttr, 0, 0));
	}
	else
	{
		if (clsid == kclidRnEvent)
			// Event subentry
			CheckHr(pvwenv->AddPicture(m_qpicRnEntryType[kentypSubEvent], ktagNotAnAttr, 0, 0));
		else if (clsid == kclidRnAnalysis)
			// Event subanalysis
			CheckHr(pvwenv->AddPicture(m_qpicRnEntryType[kentypSubAnal], ktagNotAnAttr, 0, 0));
	}
	CheckHr(pvwenv->CloseTableCell());

	for (int icol = 0; icol < ccols; icol++)
	{
		BlockSpecPtr qbsp = m_qrsp->m_vqbsp[icol];
		CheckHr(pvwenv->put_IntProperty(kspMaxLines, ktpvDefault, m_nMaxLines));
		CheckHr(pvwenv->OpenTableCell(1,1));
		switch (qbsp->m_ft)
		{
		case kftInteger:
			// Allow integer fields to be edited.
			CheckHr(pvwenv->put_StringProperty(kspNamedStyle, qbsp->m_stuSty.Bstr()));
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptSemiEditable));
			CheckHr(pvwenv->AddIntProp(qbsp->m_flid));
			break;
		case kftStText:
			{ // BLOCK
				// Structured text block. Use standard view constructor. We need a unique one
				// because it has to actually store the possible special character style.
				// Allow the structured text to be edited.
				// It is Ok to set the default analysis writing system (never vernacular) since this
				// only applies (JohnL thinks) to imported data which has no XML for this field,
				// and that doesn't include any vernacular fields at the moment.
				StVcPtr qstvc;
				qstvc.Attach(NewObj StVc(L"", m_qlpi->ActualWs(qbsp->m_ws), (COLORREF)-1,
					qwsf));
				HVO hvoText;
				CheckHr(qsda->get_ObjectProp(hvo, qbsp->m_flid, &hvoText));
				if (hvoText)
				{
					int cpara;
					CheckHr(qsda->get_VecSize(hvoText, kflidStText_Paragraphs, &cpara));
					// no paragraphs: if we get one, regenerate
					if (!cpara)
					{
						int flid = kflidStText_Paragraphs;
						CheckHr(pvwenv->NoteDependency(&hvoText, &flid, 1));
					}
				}
				else
				{
					// no text even: if we get one, regenerate
					CheckHr(pvwenv->NoteDependency(&hvo, &qbsp->m_flid, 1));
				}
				// Set a character style to override styles actually on paragraphs.
				qstvc->SetStyle(qbsp->m_stuSty.Chars());
				CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptIsEditable));
				CheckHr(pvwenv->OpenDiv());
				CheckHr(pvwenv->AddObjProp(qbsp->m_flid, qstvc, kfrText));
				CheckHr(pvwenv->CloseDiv());
			}
			break;
		case kftString:
			// Allow string fields to be edited.
			CheckHr(pvwenv->put_StringProperty(kspNamedStyle, qbsp->m_stuSty.Bstr()));
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptIsEditable));
			CheckHr(pvwenv->AddStringProp(qbsp->m_flid, this));
			break;
		case kftMsa:
			{
				// Allow MultiString text to be edited.
				Vector<int> vws;
				switch (qbsp->m_ws)
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
					CheckHr(qsda->get_MultiStringAlt(hvo, qbsp->m_flid, qbsp->m_ws, &qtss));
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
					CheckHr(pvwenv->OpenParagraph());
					if (cnt > 1)
					{
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
					CheckHr(pvwenv->put_StringProperty(ktptNamedStyle, qbsp->m_stuSty.Bstr()));
					CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptIsEditable));
					CheckHr(pvwenv->AddStringAltMember(qbsp->m_flid, vws[i], this));
					CheckHr(pvwenv->CloseParagraph());
				}
				break;
			}
		case kftExpandable:
		case kftSubItems:
			// We are only prepared to handle participants here at this point.
			// Subentries are handled elsewhere, so should never come here.
			Assert(qbsp->m_flid == kflidRnEvent_Participants);
			CheckHr(pvwenv->put_StringProperty(kspNamedStyle, qbsp->m_stuSty.Bstr()));
			// Need the span here so the above properties apply to all items in list.
			//CheckHr(pvwenv->OpenSpan());
			CheckHr(pvwenv->AddObjVec(qbsp->m_flid, this, kfrcdEvnParticipants));
			//CheckHr(pvwenv->CloseSpan());
			break;
		case kftRefAtomic:
		case kftRefCombo:
			CheckHr(pvwenv->put_StringProperty(kspNamedStyle, qbsp->m_stuSty.Bstr()));
			CheckHr(pvwenv->AddObjProp(qbsp->m_flid, this, kfrcdPliName));
			break;
		case kftEnum:
			// ENHANCE JohnT(KenZ): This probably needs to come up with a string instead of
			// just returning an int. (Note JohnT: as far as I can tell none of this
			// type of property is ever shown in browse view...yet.)
			CheckHr(pvwenv->put_StringProperty(kspNamedStyle, qbsp->m_stuSty.Bstr()));
			CheckHr(pvwenv->AddIntProp(qbsp->m_flid));
			break;
		case kftRefSeq:
			{
				// Add each item on a line by itself (as many as will fit altogether)
				// ENHANCE JohnT: there should be an option to add them as a bar-separated list.
				int flid = qbsp->m_flid;

				CheckHr(pvwenv->AddObjVecItems(flid, this,
					(flid == m_flidPrimarySort ? kfrcdPliNameOnePrimary : kfrcdPliNameOne)));
			}
			break;
		case kftObjRefAtomic:
			{
				CheckHr(pvwenv->put_StringProperty(kspNamedStyle, qbsp->m_stuSty.Bstr()));
				CheckHr(pvwenv->AddObj(hvo, m_qRecVc, kfrRefName));
			}
			break;
		case kftObjRefSeq:
			CheckHr(pvwenv->AddObjVecItems(qbsp->m_flid, this, kfrcdObjNameOne));
			break;
		case kftDateRO:
			CheckHr(pvwenv->put_StringProperty(kspNamedStyle, qbsp->m_stuSty.Bstr()));
			CheckHr(pvwenv->AddTimeProp(qbsp->m_flid, DATE_SHORTDATE));
			break;
		case kftGenDate:
			CheckHr(pvwenv->put_StringProperty(kspNamedStyle, qbsp->m_stuSty.Bstr()));
			CheckHr(pvwenv->AddGenDateProp(qbsp->m_flid));
			break;
		default:
			ThrowHr(WarnHr(E_FAIL));
			break;
		}
		CheckHr(pvwenv->CloseTableCell());
	}
	CheckHr(pvwenv->CloseTableRow());
	CheckHr(pvwenv->CloseTableBody());
	CheckHr(pvwenv->CloseTable());

	if (m_fIgnoreHier)
	{
		// Show subentries, if any.
		int chvo;
		CheckHr(qsda->get_VecSize(hvo, m_tagSubItems, &chvo));
		if (chvo)
		{
			CheckHr(pvwenv->put_IntProperty(ktptEditable, ktpvEnum, ktptNotEditable));
			CheckHr(pvwenv->OpenDiv()); // not editable applies to all, not just first.
			CheckHr(pvwenv->AddObjVecItems(m_tagSubItems, this, kfrcdSubItem));
			CheckHr(pvwenv->CloseDiv());
		}
	}
}
