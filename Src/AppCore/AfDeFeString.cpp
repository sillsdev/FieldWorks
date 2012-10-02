/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeString.cpp
Responsibility: Ken Zook
Last reviewed: never

Implements:
	AfDeFeString: A field editor to display and edit TsStrings.
	StringVc: A view constructor used by this editor.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	AfDeFeString methods.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDeFeString::AfDeFeString()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeFeString::~AfDeFeString()
{
}


/*----------------------------------------------------------------------------------------------
	Finish initialization of the field editor by creating and initializing the view constructor.
	@param pvws Pointer to a vector of encodings to display (for multistrings) or NULL
		for a single string.
----------------------------------------------------------------------------------------------*/
void AfDeFeString::Init(Vector<int> * pvws, TptEditable nEditable)
{
	AssertPtrN(pvws);
	Assert(m_flid); // Initialize should have been called prior to this.

	// Make a text property with the named style.
	SmartBstr sbstr = m_qfsp->m_stuSty.Chars();
	ITsTextPropsPtr qttp;
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->SetStrPropValue(kspNamedStyle, sbstr));
	CheckHr(qtpb->SetIntPropValues(ktptEditable, ktpvEnum, nEditable));
	CheckHr(qtpb->GetTextProps(&qttp));

	GetLpInfo()->GetDbInfo()->GetLgWritingSystemFactory(&m_qwsf);

	// Now create and setup the view constructor.
	m_qsvc.Attach(NewObj StringVc(m_flid, pvws, qttp, m_qwsf, m_chrp.clrBack));
	if (!pvws)
	{
		// set ws for monolingual field.
		m_qsvc->SetWritingSystem(m_ws);
		// If it is an empty string in the wrong ws fix it.
		AfMainWnd * pafw = m_qadsc->MainWindow();
		RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(pafw);
		Assert(prmw);
		// Get the main custom view DA shared by the project.
		CustViewDaPtr qcvd = prmw->MainDa();
		AssertPtr(qcvd);
		ITsStringPtr qtssCurrent;
		CheckHr(qcvd->get_StringProp(m_hvoObj, m_flid, &qtssCurrent));
		int cch;
		CheckHr(qtssCurrent->get_Length(&cch));
		if (cch == 0)
		{
			ITsTextPropsPtr qttp;
			CheckHr(qtssCurrent->get_Properties(0, &qttp));
			int ws, var;
			CheckHr(qttp->GetIntPropValues(ktptWs, &var, &ws));
			if (ws != m_ws)
			{
				// Fix!
				ITsPropsBldrPtr qtpb;
				CheckHr(qttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetIntPropValues(ktptWs, var, m_ws));
				ITsTextPropsPtr qttpNew;
				CheckHr(qtpb->GetTextProps(&qttpNew));
				ITsStrFactoryPtr qtsf;
				qtsf.CreateInstance(CLSID_TsStrFactory);
				ITsStringPtr qtssNew;
				CheckHr(qtsf->MakeStringWithPropsRgch(NULL, 0, qttpNew, &qtssNew));
				// It's enough just to override what's in the cache. If the user actually types
				// something, the modified string in the proper ws will get saved.
				CheckHr(qcvd->CacheStringProp(m_hvoObj, m_flid, qtssNew));
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Gets the ITsString and writing system of the currently selected field in the current Window.
	If there is	no selection, then NULL is returned for the string.
	@param pptss Out string returned.
	@param ws Out encodeing of string returned.
----------------------------------------------------------------------------------------------*/
void AfDeFeString::GetCurString(ITsString ** pptss, int * pws)
{
	AssertPtr(pptss);
	*pptss = NULL;
	ITsStringPtr qtss;

	AfMainWnd * pafw = m_qadsc->MainWindow();
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(pafw);
	Assert(prmw);
	// Get the main custom view DA shared by the project.
	CustViewDaPtr qcvd = prmw->MainDa();
	AssertPtr(qcvd);


	// Get the selection from the current view window.
	IVwSelectionPtr qvwsel;
	ComBool fOk;
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	// Commit the selection so that we can access the data from the cache.
	CheckHr(qvwsel->Commit(&fOk));
	CheckHr(m_qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
	{
		*pptss = NULL;
		return;
	}
	int cvsli;
	CheckHr(qvwsel->CLevels(false, &cvsli));
	cvsli--; // CLevels includes the string property itself, but AllTextSelInfo does not need it
	Assert((uint)cvsli < 3);
	VwSelLevInfo rgvsli[2];
	// Get selection information to determine where the user is typing.
	int ihvoObj;
	PropTag tagTextProp;
	int cpropPrevious;
	int ichAnchor;
	int ichEnd;
	int ws;
	ComBool fAssocPrev;
	int ihvoEnd;
	// ichAnchor (which should = ichEnd) gives an index to the cursor from the beginning of
	// the paragraph.
	CheckHr(qvwsel->AllTextSelInfo(&ihvoObj, cvsli, rgvsli, &tagTextProp, &cpropPrevious,
		&ichAnchor, &ichEnd, &ws, &fAssocPrev, &ihvoEnd, NULL));

	*pws = ws;
	CheckHr(qcvd->get_MultiStringAlt(m_hvoObj, m_flid, ws, &qtss));
	*pptss = qtss.Detach();
}


/*----------------------------------------------------------------------------------------------
	Make a rootbox, initialize it, and return a pointer to it.
	@param pvg Graphics information (unused by this method).
	@param pprootb Pointer that receives the newly created RootBox.
----------------------------------------------------------------------------------------------*/
void AfDeFeString::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
	IVwRootBox ** pprootb)
{
	AssertPtrN(pwsf);
	AssertPtr(pprootb);
	AssertPtr(m_qsvc); // Init should have been called before calling this.

	*pprootb = NULL;

	// Create the RootBox.
	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this)); // The field editor is the root site.

	// Set the RootBox data cache.
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	if (pwsf)
		CheckHr(qcvd->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qcvd));

	// Finish initializing the RootBox.
	IVwViewConstructor * pvvc = m_qsvc;
	int frag = kfrAll;
	CheckHr(qrootb->SetRootObjects(&m_hvoObj, &pvvc, &frag,
		GetLpInfo()->GetAfStylesheet(), 1));

	// Return the RootBox pointer.
	*pprootb = qrootb;
	AddRefObj(*pprootb);

	// Register the RootBox if we have a main window.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (prmw)
		prmw->RegisterRootBox(qrootb);
}


/*----------------------------------------------------------------------------------------------
	Check the requirments of the FldSpec, and verify that data in the field meets the
	requirement. It returns:
		kFTReqNotReq if the all requirements are met.
		kFTReqWs if data is missing, but it is encouraged.
		kFTReqReq if data is missing, but it is required.
----------------------------------------------------------------------------------------------*/
FldReq AfDeFeString::HasRequiredData()
{
	if (m_qfsp->m_fRequired == kFTReqNotReq)
		return kFTReqNotReq;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	bool fMulti = m_qsvc->IsMulti();
	Vector<int> vws = m_qsvc->WritingSystems();
	bool fEmpty;
	ITsStringPtr qtss;
	int cch = 0;

	if (fMulti)
	{
		// Any multistring with data makes it acceptable.
		int iws;
		for (iws = vws.Size(); --iws >= 0; )
		{
			CheckHr(qcvd->get_MultiStringAlt(m_hvoObj, m_flid, vws[iws], &qtss));
			if (qtss)
				CheckHr(qtss->get_Length(&cch));
			if (cch)
				break; // Have a valid string
		}
		fEmpty = iws < 0;
	}
	else
	{
		CheckHr(qcvd->get_StringProp(m_hvoObj, m_flid, &qtss));
		if (qtss)
			CheckHr(qtss->get_Length(&cch));
		fEmpty = !cch;
	}

	if (fEmpty)
	{
		// Check whether we actually have data that is "hidden" because it belongs to a writing
		// system that is no longer displayed for this field.
		int cwsTxt = 0;
		IFwMetaDataCachePtr qmdc;
		GetLpInfo()->GetDbInfo()->GetFwMetaDataCache(&qmdc);
		AssertPtr(qmdc.Ptr());
		int nType;
		CheckHr(qmdc->GetFieldType(m_flid, &nType));
		if (fMulti)
		{
			Assert(nType == kcptMultiString || nType == kcptMultiUnicode ||
				nType == kcptMultiBigString || nType == kcptMultiBigUnicode);
			SmartBstr sbstrClass;
			SmartBstr sbstrProp;
			CheckHr(qmdc->GetOwnClsName(m_flid, &sbstrClass));
			CheckHr(qmdc->GetFieldName(m_flid, &sbstrProp));
			StrUni stuQuery;
			stuQuery.Format(L"select Ws,Txt from %s_%s"
				L" where [Obj] = %d", sbstrClass.Chars(), sbstrProp.Chars(), m_hvoObj);
			IOleDbEncapPtr qode;
			GetLpInfo()->GetDbInfo()->GetDbAccess(&qode);
			AssertPtr(qode.Ptr());
			IOleDbCommandPtr qodc;
			CheckHr(qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			unsigned long cbSpaceTaken;
			ComBool fMoreRows;
			ComBool fIsNull = true;
			CheckHr(qodc->NextRow(&fMoreRows));
			int ws;
			wchar rgch[8192];
			while (fMoreRows)
			{
				ws = 0;
				CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&ws), isizeof(ws),
					&cbSpaceTaken, &fIsNull, 0));
				if (!fIsNull)
				{
					rgch[0] = 0;
					CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(rgch),
						isizeof(rgch), &cbSpaceTaken, &fIsNull, 2));
					if (!fIsNull && cbSpaceTaken && ws && rgch[0])
						++cwsTxt;
				}
				CheckHr(qodc->NextRow(&fMoreRows));
			}
		}
		if (cwsTxt)
		{
			if (m_qfsp->m_fRequired == kFTReqWs)
			{
				return (FldReq)kFTReqWsHidden;
			}
			else if (m_qfsp->m_fRequired == kFTReqReq)
			{
				return (FldReq)kFTReqReqHidden;
			}
			return m_qfsp->m_fRequired;
		}
		return m_qfsp->m_fRequired;
	}
	else
	{
		return kFTReqNotReq;
	}
}


/*----------------------------------------------------------------------------------------------
	This method saves the current cursor information in RecMainWnd. Normally it just
	stores the cursor index in RecMainWnd::m_ichCur. For structured texts, however,
	it also inserts the appropriate hvos and flids for the StText classes in
	m_vhvoPath and m_vflidPath. Other editors may need to do other things.
----------------------------------------------------------------------------------------------*/
void AfDeFeString::SaveCursorInfo()
{
	SuperClass::SaveCursorInfo();
}


/*----------------------------------------------------------------------------------------------
	This attempts to place the cursor as defined in RecMainWnd m_vhvoPath, m_vflidPath,
	and m_ichCur.
	@param vhvo Vector of ids inside the field.
	@param vflid Vector of flids inside the field.
	@param ichCur Character offset in the final field for the cursor.
----------------------------------------------------------------------------------------------*/
void AfDeFeString::RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur)
{
	if (m_qrootb)
	{
		IVwSelectionPtr qsel;
		if (vflid.Size())
		{
			m_qrootb->MakeTextSelection(
				0, // int ihvoRoot
				0, // int cvlsi,
				NULL, // VwSelLevInfo * prgvsli
				vflid[vflid.Size() - 1], // int tagTextProp,
				0, // int cpropPrevious,
				ichCur, // int ichAnchor,
				ichCur, // int ichEnd,
				0, // int ws,
				true, // ComBool fAssocPrev,
				-1, // int ihvoEnd,
				NULL, // ITsTextProps * pttpIns,
				true, // ComBool fInstall,
				&qsel); // IVwSelection ** ppsel
		}
		// If we didn't get a text selection, try getting a selection somewhere close.
		if (!qsel)
		{
			m_qrootb->MakeTextSelInObj(
				0,  // index of the one and only root object in this view
				0, // the object we want is one level down
				NULL, // and here's how to find it there
				0,
				NULL, // don't worry about the endpoint
				true, // select at the start of it
				true, // Find an editable field
				false, // and don't select a range.
				// Making this true, allows the whole record to scroll into view when we launch
				// a new window by clicking on a reference to an entry, but we don't get an insertion
				// point. Using false gives an insertion point, but the top of the record is typically
				// at the bottom of the screen, which isn't good.
				false, // don't select the whole object
				true, // but do install it as the current selection
				NULL); // and don't bother returning it to here. */
		}
	}
}


//:>********************************************************************************************
//:>	StringVc methods
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Constructor. Initiatlize member variables.
	@param flid The field id for the field we are displaying.
	@param pvws Pointer to a vector of encodings to show for MultiStrings, or NULL for
		a single string.
	@param pttp Pointer to text properties to use for displaying the contents.
	@param clrBkg Color of the field background.
----------------------------------------------------------------------------------------------*/
StringVc::StringVc(int flid, Vector<int> * pvws, ITsTextProps * pttp,
	ILgWritingSystemFactory * pwsf, COLORREF clrBkg)
{
	Assert(flid);
	AssertPtrN(pvws);
	AssertPtr(pttp);
	AssertPtr(pwsf);

	m_qttp = pttp;
	m_flid = flid;
	m_clrBkg = (clrBkg == -1 ? ::GetSysColor(COLOR_WINDOW) : clrBkg);
	// If we are displaying MultiStrings, set these variables.
	if (pvws)
	{
		m_fMulti = true;
		m_vws = *pvws;
		Assert(m_vws.Size()); // We should have at least one writing system.
	}
	m_qwsf = pwsf;
	CheckHr(pwsf->get_UserWs(&m_wsUser));

	// Fill vector with writing system abbreviations.
	SmartBstr sbstr;
	ITsStrFactoryPtr qtsf;
	ITsStringPtr qtss;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	AssertPtr(qtsf);
	for (int iws = 0; iws < m_vws.Size(); ++iws)
	{
		sbstr.Clear();
		IWritingSystemPtr qws;
		CheckHr(pwsf->get_EngineOrNull(m_vws[iws], &qws));
		if (qws)
			CheckHr(qws->get_Abbr(m_wsUser, &sbstr));
		if (!sbstr)
			CheckHr(pwsf->GetStrFromWs(m_vws[iws], &sbstr));
		CheckHr(qtsf->MakeStringRgch(sbstr.Chars(), sbstr.Length(), m_wsUser, &qtss));
		m_vqtss.Push(qtss);
	}
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
StringVc::~StringVc()
{
	m_vqtss.Clear();
}

static DummyFactory g_fact(_T("SIL.AppCore.StringVc"));

/*----------------------------------------------------------------------------------------------
	This is the main method for displaying string(s), either in a table, or without a table.
	@param pvwenv Pointer to the view environment.
	@param hvo The id of the object we are displaying.
	@param frag Identifies the part of the view we are currently displaying.
	@return HRESULT indicating success (S_OK), or failure (E_FAIL).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP StringVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwenv);

	switch (frag)
	{
		case kfrAll: // Display the entire field.
		if (m_fMulti)
		{
			IWritingSystemPtr qws;
			ComBool fRTL;
			if (m_vws.Size() <= 1)
			{
				// For MultiStrings with a single writing system, we display the string without
				// a table.
				pvwenv->put_Props(m_qttp);	// Apply the field style properties.
				// Set writing system direction based on m_vws[0]
				fRTL = FALSE;
				if (m_vws.Size() == 1)
				{
					CheckHr(m_qwsf->get_EngineOrNull(m_vws[0], &qws));
					if (qws)
						CheckHr(qws->get_RightToLeft(&fRTL));
				}
				CheckHr(pvwenv->put_IntProperty(ktptRightToLeft, ktpvEnum, fRTL));
				CheckHr(pvwenv->AddStringAltMember(m_flid, m_vws[0], this));
			}
			else
			{
				// For MultiStrings with more than one writing system we use a table to display
				// encodings in column one and the strings in column two.
				// The table uses 100% of the available width.
				VwLength vlTab = {10000, kunPercent100};

				// The width of the writing system column is determined from the width of the
				// longest one which will be displayed.
				StrUni stuLangCodeStyle(L"Language Code");
				ITsPropsFactoryPtr qtpf;
				qtpf.CreateInstance(CLSID_TsPropsFactory);
				ITsTextPropsPtr qttp;
				SmartBstr sbstrWs;
				ITsStringPtr qtss;
				// Get the properties of the "Language Code" style for the writing system
				// which corresponds to the user's environment.
				qtpf->MakeProps(stuLangCodeStyle.Bstr(), m_wsUser, 0, &qttp);
				int dxs;	// Width of displayed string.
				int dys;	// Height of displayed string (not used here).
				int dxsMax = 0;	// Max width required.
				int i;
				CheckHr(m_qwsf->get_EngineOrNull(m_wsUser, &qws));
				if (qws)
				{
					for (i = 0; i < m_vqtss.Size(); ++i)
					{
						pvwenv->get_StringWidth(m_vqtss[i], qttp, &dxs, &dys);
						dxsMax = (dxsMax < dxs) ? dxs : dxsMax;
					}
				}
				Assert(dxsMax);
				VwLength vlColEnc = {dxsMax + 5000, kunPoint1000};	// Nominal 5-pt space.

				// Get the direction of the User Interface writing system
				// (this is almost certainly left-to-right).
				ComBool fUserRTL = FALSE;
				if (qws)
					CheckHr(qws->get_RightToLeft(&fUserRTL));

				// The Main column is relative and uses the rest of the space.
				VwLength vlColMain = {1, kunRelative};

				CheckHr(pvwenv->OpenTable(2, // Two columns.
					vlTab,
					0, // Border thickness.
					kvaLeft, // Default alignment.
					kvfpVoid, // No border.
					kvrlNone, // No rules between cells.
					0, // No forced space between cells.
					0, // No padding inside cells.
					false));
				// Specify column widths. The first argument is the number of columns,
				// not a column index. The writing system column only occurs at all if its
				// width is non-zero.
				CheckHr(pvwenv->MakeColumns(1, vlColEnc));
				CheckHr(pvwenv->MakeColumns(1, vlColMain));

				CheckHr(pvwenv->OpenTableBody());

				// OPTIMIZE JohnT(KenZ): Is there a way to break this up to use kfrRow?
				// I assume this would make edit updates more efficient since it would
				// only need to update the one row. I'm not sure how to do it so that
				// the correct writing system will be used in AddStringAltMember.
				// The other question: is the view code smart enough to know when a
				// string can be formatted and when it can't? This code is used for
				// both MultiString and MultiText.

				// Add a row for each writing system.
				for (i = 0; i < m_vws.Size(); ++i)
				{
					CheckHr(pvwenv->OpenTableRow());

					// First cell has writing system abbreviation in "Language Code" style.
					CheckHr(pvwenv->put_StringProperty(kspNamedStyle,
														stuLangCodeStyle.Bstr()));
					CheckHr(pvwenv->put_IntProperty(ktptEditable,
													ktpvEnum, ktptNotEditable));
					// Set direction based on m_qwsf->get_UserWs()
					CheckHr(pvwenv->put_IntProperty(ktptRightToLeft, ktpvEnum, fUserRTL));
					CheckHr(pvwenv->OpenTableCell(1,1));
					CheckHr(pvwenv->AddString(m_vqtss[i]));
					CheckHr(pvwenv->CloseTableCell());

					// Second cell has the string contents for the alternative.
//					CheckHr(pvwenv->put_IntProperty(ktptBackColor, ktpvDefault, m_clrBkg));
					pvwenv->put_Props(m_qttp);	// Apply the field style properties.
					CheckHr(pvwenv->OpenTableCell(1,1));
					CheckHr(pvwenv->put_IntProperty(ktptTrailingIndent, ktpvMilliPoint,
						kdzmpInch / 8));
					// Set direction based on m_vws[i]
					fRTL = FALSE;
					CheckHr(m_qwsf->get_EngineOrNull(m_vws[i], &qws));
					if (qws)
						CheckHr(qws->get_RightToLeft(&fRTL));
					CheckHr(pvwenv->put_IntProperty(ktptRightToLeft, ktpvEnum, fRTL));
					CheckHr(pvwenv->OpenParagraph());
					CheckHr(pvwenv->AddStringAltMember(m_flid, m_vws[i], this));
					CheckHr(pvwenv->CloseParagraph());
					CheckHr(pvwenv->CloseTableCell());

					CheckHr(pvwenv->CloseTableRow());
				}
				CheckHr(pvwenv->CloseTableBody());

				CheckHr(pvwenv->CloseTable());
			}
		}
		else
		{
			// For single strings, we display the string without a table.
			pvwenv->put_Props(m_qttp);	// Apply the field style properties.
			// Set direction based on m_vws[0].
			IWritingSystemPtr qws;
			ComBool fRTL;
			fRTL = FALSE;
			if (m_vws.Size() == 1)
			{
				CheckHr(m_qwsf->get_EngineOrNull(m_vws[0], &qws));
				if (qws)
					CheckHr(qws->get_RightToLeft(&fRTL));
			}
			CheckHr(pvwenv->put_IntProperty(ktptRightToLeft, ktpvEnum, fRTL));
			CheckHr(pvwenv->AddStringProp(m_flid, this));
		}
		break;

	case kfrRow:
		{
			// OPTIMIZE KenZ: Is this useful? See the previous OPTIMIZE.
		}
		break;
	}
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}

/*----------------------------------------------------------------------------------------------
	Set the single writing system for a monolingual string field.
----------------------------------------------------------------------------------------------*/
void StringVc::SetWritingSystem(int ws)
{
	Assert(!m_vws.Size());
	Assert(!m_fMulti);
	m_fMulti = false;	// Let's be very paranoid.
	m_vws.Clear();		// ditto.
	m_vws.Push(ws);
}
