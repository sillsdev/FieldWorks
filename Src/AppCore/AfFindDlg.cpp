/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfFindDlg.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Implementation of the Find Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

BEGIN_CMD_MAP(AfFindRepDlg)
	ON_CID_ALL(kcidExpFindFmtWs, &AfFindRepDlg::CmdFindFmtWsItems, &AfFindRepDlg::CmsFindFmtWsItems)
	ON_CID_ALL(kcidExpFindFmtStyle, &AfFindRepDlg::CmdFindFmtStyleItems,
		&AfFindRepDlg::CmsFindFmtStyleItems)
	ON_CID_ALL(kcidRestoreFocus, &AfFindRepDlg::CmdRestoreFocus, NULL)
END_CMD_MAP_NIL()


/***********************************************************************************************
	Static variables
***********************************************************************************************/

ComVector<ITsString> AfFindRepDlg::m_vtssFindWhatItems; // Items in the Find What Combo box (max of 12).

/***********************************************************************************************
	FindTssEdit methods
***********************************************************************************************/

void FindTssEdit::SetParent(AfFindRepDlg * pfrdlg)
{
	m_pfrdlg = pfrdlg;
}

void FindTssEdit::AdjustForOverlays(IVwOverlay * pvo)
{
	// When the state of the overlays changes, tell the dialog to resize itself.
	m_pfrdlg->ToggleOverlays(pvo);
	m_pfrdlg->EditBoxChanged(this); // enable/disable the Find button
}

void FindTssEdit::GetOverlay(IVwOverlay ** ppvo)
{
	m_qrootb->get_Overlay(ppvo);
}

bool FindTssEdit::OnChange()
{
	m_pfrdlg->EditBoxChanged(this);
	return false;
}

void FindTssEdit::HandleSelectionChange(IVwSelection * pvwsel)
{
	m_pfrdlg->EditBoxChanged(this); // enable/disable the Find button
	SuperClass::HandleSelectionChange(pvwsel);
}

bool FindTssEdit::OnSetFocus(HWND hwndOld, bool fTbControl)
{
	SuperClass::OnSetFocus(hwndOld, fTbControl);
	m_pfrdlg->EditBoxFocus(this);

	// Make this edit box be considered the current one, so the overlay palettes affect it.
	AfMainWnd * pafw = MainWindow();
	AssertPtrN(pafw);
	if (pafw)
	{
		pafw->SetActiveRootBox(m_qrootb);
		/*
		 * This appears to be called whenever the owning Find dialog gains the focus.  The
		 * owning AfMainWnd is brought to the front of the AfMainWnds (apparently an automatic
		 * operation of Windows TM), so it should be flagged as the "current" AfMainWnd of the
		 * application.
		 */
		AssertPtr(AfApp::Papp());
		AfApp::Papp()->SetCurrentWindow(pafw);
	}
	return false;
}


/***********************************************************************************************
	AfFindRepDlg methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
	(Note that each subclass MUST have its own constructor to independently initialize m_rid
	and m_pszHelpUrl.)

----------------------------------------------------------------------------------------------*/
AfFindRepDlg::AfFindRepDlg()
{
	m_fBusy = false;
	m_fCloseOnFindNow = false; // default behavior (like MSWord).
	SetReplace(false); // default
	m_rid = kridAfFindDlg;
}

/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModeless.
	ENHANCE JohnT: it may need more arguments, for example, the name of the kind of object we
	can restrict the search to, a list of fields.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::SetDialogValues(IVwPattern * pxpat, AfVwRootSite * pvrs, bool fReplace,
	bool fOverlays)
{
	SetReplace(fReplace);
	m_fOverlays = fOverlays;
	// Call overrideable method to give variables their values.
	SetDialogValues1(pxpat, pvrs);
	// Call overrideable method to fill the controls themselves with their values.
	FillCtls();
}

/*----------------------------------------------------------------------------------------------
	Set the flag indicating if the Replace tab is selected. Also set the help link to
	match.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::SetReplace(bool f)
{
	m_fReplaceTab = f;
	m_pszHelpUrl = (m_fReplaceTab) ?
		_T("User_Interface/Menus/Edit/Find_and_Replace/Replace.htm") :
		_T("User_Interface/Menus/Edit/Find_and_Replace/Find.htm");
}

/*----------------------------------------------------------------------------------------------
	Simplify a TsTextProps, retaining only the writing system/old writing system and font info.
----------------------------------------------------------------------------------------------*/
void SimplifyProps(ITsTextPropsPtr & qttp)
{
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	// Copy only writing system/old writing system and font info.
	int ttv, tpt;
	CheckHr(qttp->GetIntPropValues(ktptWs, &ttv, &tpt));
	CheckHr(qtpb->SetIntPropValues(ktptWs, ttv, tpt));
	SmartBstr sbstrFont;
	CheckHr(qttp->GetStrPropValue(ktptFontFamily, &sbstrFont));
	CheckHr(qtpb->SetStrPropValue(ktptFontFamily, sbstrFont));
	// Don't copy the character styles from the original selection. The user has to use the
	// Format button to get styles in the find-what field.
//	SmartBstr sbstrCharStyle;
//	CheckHr(qttp->GetStrPropValue(ktptNamedStyle, &sbstrCharStyle));
//	CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, sbstrCharStyle));

	CheckHr(qtpb->GetTextProps(&qttp));
}

/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog.
	Override this in subclasses with additional variables to initialize.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::SetDialogValues1(IVwPattern * pxpat, AfVwRootSite * pvrs)
{
	m_qxpat = pxpat;
	ComBool fMatch;

	// Extract the info we need for the controls from the pattern into member variables.
	CheckHr(pxpat->get_Pattern(&m_qtssFindWhat));
	CheckHr(pxpat->get_ReplaceWith(&m_qtssReplaceWith));

	CheckHr(pxpat->get_MatchCase(&fMatch));
	m_fMatchCase = (bool)fMatch;

	CheckHr(pxpat->get_MatchDiacritics(&fMatch));
	m_fMatchDiacritics = (bool)fMatch;

	CheckHr(pxpat->get_MatchWholeWord(&fMatch));
	m_fMatchWholeWord = (bool)fMatch;

	CheckHr(pxpat->get_MatchOldWritingSystem(&fMatch));
	m_fMatchOldWritingSystem = (bool)fMatch;

	CheckHr(pxpat->get_ShowMore(&fMatch));
	m_fShowMoreControls = (bool)fMatch | m_fMatchCase | m_fMatchDiacritics | m_fMatchWholeWord
		| m_fMatchOldWritingSystem;

	// Now, we want to be a bit smarter about the initial pattern.
	// -- if the window we are starting from has a range selection use that as the pattern.
	// -- if we have no starting pattern, make an empty string with the same properties
	// as the current selection.
	IVwRootBoxPtr qrootb;
	pvrs->get_RootBox(&qrootb);
	IVwSelectionPtr qvwsel;
	if (qrootb)
		CheckHr(qrootb->get_Selection(&qvwsel));
	ITsStringPtr qtssSel;
	if (qvwsel) // fails also if no root box
	{
		SmartBstr sbstr = L" ";
		ComBool fGotItAll; // dummy
		CheckHr(qvwsel->GetFirstParaString(&qtssSel, sbstr, &fGotItAll));
	}

	int cchSel = 0;
	if (qtssSel)
		CheckHr(qtssSel->get_Length(&cchSel));
	int cchFindWhat = 0;
	if (m_qtssFindWhat)
		CheckHr(m_qtssFindWhat->get_Length(&cchFindWhat));
	// If there is a non-empty selection always use it as the default Find What string.
	// If there is only an insertion point, normally use the previous Find string.
	// But, if there is no previous Find string, use the selection one anyway, as it
	// will at least give us a promising writing system to type in.
	if (cchSel || !cchFindWhat)
	{
		// We'll also use this block to assess the criteria for blanking out the replace string:
		ComBool fEqual = false;
		if (m_qtssFindWhat && qtssSel)
			CheckHr(m_qtssFindWhat->Equals(qtssSel, &fEqual));
		m_qtssFindWhat = qtssSel;

		cchFindWhat = cchSel;
		if (cchSel > 0 && !fEqual)
			m_qtssReplaceWith.Clear();
	}

	// Remove any special formatting. It is especially important to remove big point
	// sizes because they can't be displayed.
	if (cchFindWhat)
	{
		ITsStrBldrPtr qtsb;
		qtsb.CreateInstance(CLSID_TsStrBldr);
		int crun;
		CheckHr(m_qtssFindWhat->get_RunCount(&crun));
		for (int irun = 0; irun < crun; irun++)
		{
			TsRunInfo tri;
			ITsTextPropsPtr qttp;
			CheckHr(m_qtssFindWhat->FetchRunInfo(irun, &tri, &qttp));
			SimplifyProps(qttp);

			// Insert modified run into string builder.
			SmartBstr sbstrText;
			CheckHr(m_qtssFindWhat->get_RunText(irun, &sbstrText));
			CheckHr(qtsb->Replace(tri.ichMin, tri.ichMin, sbstrText, qttp));
		}
		CheckHr(qtsb->GetString(&m_qtssFindWhat));
	}
	else
	{
		// Empty string. Unfortunately the code above does not work because
		// the StringBuilder Replace code ignores the properties given for
		// the replacement if it is an empty string.
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		ITsTextPropsPtr qttp;
		if (m_qtssFindWhat)
		{
			CheckHr(m_qtssFindWhat->get_Properties(0, &qttp));
			SimplifyProps(qttp);
			qtsf->MakeStringWithPropsRgch(NULL, 0, qttp, &m_qtssFindWhat);
		}
		else
		{
			int ws;
			ILgWritingSystemFactoryPtr qwsf;
			CheckHr(m_qsda->get_WritingSystemFactory(&qwsf));
			AssertPtr(qwsf);
			CheckHr(qwsf->get_UserWs(&ws));
			qtsf->MakeStringRgch(L"", 0, ws, &m_qtssFindWhat);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Need to clean up. Make sure no longer registered as command handler, and remove the
	accelerator table.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::OnReleasePtr()
{
	SuperClass::OnReleasePtr();
	AfApp::Papp()->RemoveCmdHandler(this, 1);
}


/***********************************************************************************************
	Protected class methods.
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	m_fBusy = false;

	// Make sure Stop button is not yet visible or enabled:
	::ShowWindow(::GetDlgItem(m_hwnd, kctidFindStop), SW_HIDE);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFindStop), false);

	LoadWindowPosition();

	OnInitDlg1(hwndCtrl, lp);
	FillCtls();
	EnableDynamicControls();

	return true;
}

/*----------------------------------------------------------------------------------------------
	Virtual called by OnInitDlg to initialize the dialog. Override this if you have additional
	controls that need one-time initialization. FillCtls will be called afterwards for you.
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::OnInitDlg1(HWND hwndCtrl, LPARAM lp)
{
	StrAppBuf strb;

	// Arranges for icon on help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	// It needs to be a command handler so dynamic menus (e.g., in the Format popup) work.
	AfApp::Papp()->AddCmdHandler(this,1);

	// Initialize values for the Fields combo box.
	HWND hwndSpec = ::GetDlgItem(m_hwnd, kctidFindFieldCombo);
	// TODO JohnT: do it.

	ILgWritingSystemFactoryPtr qwsf;
	CheckHr(m_qsda->get_WritingSystemFactory(&qwsf));
	AssertPtr(qwsf);

	// Convert the Find What combo box into a TssEdit widget.
	// Todo 1348(JohnT) (maybe version 2): make a workable Combo Box widget instead.
	m_qteFindWhat.Create();
	m_qteFindWhat->SetParent(this);
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));
	m_qteFindWhat->SubclassEdit(m_hwnd, kctidFindWhat, qwsf, wsUser, WS_EX_CLIENTEDGE);
	// TODO JohnT: adjust the two fixed items in this list for the kind of objet we can find.
	// Probably use some %s resource strings, inserting an application-specific string
	// passed when this dialog is invoked.
	hwndSpec = ::GetDlgItem(m_hwnd, kctidFindWhichEntries);

	// Figure out extra space needed when showing overlay tags.
	Rect rcFind;
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidFindWhat), &rcFind);
	m_dypFind = (int)(rcFind.Height() * 1.6);
	m_dypReplace = m_dypFind;

	// Figure how much smaller to make the dialog if we are not showing "more" controls.
	// Its natural size includes the "more" controls; the line across indicates where to
	// cut it off it it isn't.
	Rect rcDlg;
	::GetWindowRect(m_hwnd, &rcDlg);
	POINT pt = {0,0};
	MapWindowPoints(m_hwnd, NULL, &pt, 1); // Gets top left of client area in screen coords.

	::GetWindowRect(::GetDlgItem(m_hwnd, kctidFindMoreLess), &m_rcMoreLess);
	// when the Find tab is selected, the More/Less button is further to the right:
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidFindReplaceAll), &m_rcMoreLessFind);
	m_rcMoreLess.Offset(-pt.x, -pt.y);
	m_rcMoreLessFind.Offset(-pt.x, -pt.y);

	Rect rcSep;
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidSeparator), &rcSep);
	HWND hwndTab = ::GetDlgItem(m_hwnd, kctidFindTab);
	Rect rcTab;
	::GetWindowRect(hwndTab, &rcTab);
	int dypBottoms = rcDlg.bottom - rcTab.bottom;
	// Use a distance that clearly makes the separator invisible. We want the bottom of the
	// tab control just above it.
	m_dypMoreControls = rcDlg.bottom - rcSep.top + 1 - dypBottoms;
	// Make it the appropriate current size.
	if (!m_fShowMoreControls)
	{
		int y = rcDlg.Height() - m_dypMoreControls;
		::SetWindowPos(m_hwnd, NULL, 0, 0, rcDlg.Width(), y,
			SWP_NOZORDER | SWP_NOMOVE);

		y = rcTab.Height() - m_dypMoreControls;
		::SetWindowPos(hwndTab, NULL, 0, 0, rcTab.Width(), y,
			SWP_NOZORDER | SWP_NOMOVE);
	}
	if (m_fOverlays)
	{
		m_fOverlays = false;
		IVwOverlayPtr qvo;
		m_qteFindWhat->GetOverlay(&qvo);
		ToggleOverlays(qvo);
	}

	m_qbtnMoreLess.Create(); // Before EnableMoreControls!
	m_qbtnMoreLess->SubclassButton(m_hwnd, kctidFindMoreLess, kbtMore, NULL, 0);

	EnableMoreControls();
	// Disable this control until we implement it.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidListResults), 0);

	// Make the "Find Format" and "Special" buttons be pop-up menu buttons.
	AfButtonPtr qbtnFf;
	qbtnFf.Create();
	qbtnFf->SubclassButton(m_hwnd, kctidFindFormat, kbtPopMenu, NULL, 0);
	AfButtonPtr qbtnFs;
	qbtnFs.Create();
	qbtnFs->SubclassButton(m_hwnd, kctidFindSpecial, kbtPopMenu, NULL, 0);

	// Initialize values for the Fields combo box.
	hwndSpec = ::GetDlgItem(m_hwnd, kctidFindFieldCombo);
	// Todo JohnT: do it.

	// Todo JohnT: adjust the two fixed items in this list for the kind of object we can find.
	// Probably use some %s resource strings, inserting an application-specific string
	// passed when this dialog is invoked.
	hwndSpec = ::GetDlgItem(m_hwnd, kctidFindWhichEntries);

	// Initialize the tab controls.
	m_hwndTab = ::GetDlgItem(m_hwnd, kctidFindTab);

	// Insert the title of each tab.
	TCITEM tci = { TCIF_TEXT };
	strb.Load(kstidFind);
	tci.pszText = const_cast<achar *>(strb.Chars());
	TabCtrl_InsertItem(m_hwndTab, 0, &tci);
	strb.Load(kstidReplace);
	tci.pszText = const_cast<achar *>(strb.Chars());
	TabCtrl_InsertItem(m_hwndTab, 1, &tci);

	// Convert the Replace with combo box into a TssEdit widget.
	// Todo 1348(JohnT) (maybe version 2): make a workable Combo Box widget instead.
	m_qteReplaceWith.Create();
	m_qteReplaceWith->SetParent(this);
	m_qteReplaceWith->SubclassEdit(m_hwnd, kctidFindReplaceWith, qwsf, wsUser,
		WS_EX_CLIENTEDGE);

	m_qteLastFocus = m_qteFindWhat; // Find what is initially in focus.

	return true;
} // AfFindRepDlg::OnInitDlg.

/*----------------------------------------------------------------------------------------------
	Fill the controls with their values.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::FillCtls(void)
{
	// If called before OnInitDlg, we can safely ignore, since OnInitDlg calls it again.
	// Also, our control windows have not been initialized, and various messages wrongly
	// go to the whole screen, causing flicker.
	if (!m_hwnd)
		return;
	FW_COMBOBOXEXITEM fcbi;
	fcbi.mask = CBEIF_TEXT;

	// Set up the items in the Find What combo box.
#ifdef JohnT_010509_FindWhatIsCombo
	HWND hwndCtrl = ::GetDlgItem(m_hwnd, kctidFindWhat);
	for (int item = 0; item < m_vtssFindWhatItems.Size(); item++)
	{
		fcbi.qtss = m_vtssFindWhatItems[item];
		SendMessage(hwndCtrl, FW_CBEM_INSERTITEM, 0, (LPARAM)&fcbi);
	}
	// Select the first one (if any)
	SendMessage(hwndCtrl, CB_SETCURSEL, (WPARAM)0, 0);
#else // Find what is for now just an Edit
	m_qteFindWhat->SetText(m_qtssFindWhat);
#endif
	IVwRootBoxPtr qrootb;
	m_qteFindWhat->get_RootBox(&qrootb);
	// Make a selection at the start of the box, in an editable location (everywhere is),
	// select the whole property (the whole of the string), do install it as the current
	// selection, and don't bother returning it.
	qrootb->MakeSimpleSel(true, true, true, true, NULL);
	::SetFocus(m_qteFindWhat->Hwnd());
	m_qteLastFocus = m_qteFindWhat;

	// Set the check boxes.
	CheckDlgButton(m_hwnd, kctidListResults, m_fListResults ? BST_CHECKED : BST_UNCHECKED);
	CheckDlgButton(m_hwnd, kctidFindFieldCheck, m_fFieldCheck ? BST_CHECKED : BST_UNCHECKED);
	CheckDlgButton(m_hwnd, kctidFindMatchWholeWord,
		m_fMatchWholeWord ? BST_CHECKED : BST_UNCHECKED);
	CheckDlgButton(m_hwnd, kctidFindMatchCase, m_fMatchCase ? BST_CHECKED : BST_UNCHECKED);
	CheckDlgButton(m_hwnd, kctidFindMatchDiacritics,
		m_fMatchDiacritics ? BST_CHECKED : BST_UNCHECKED);
	if (m_qteReplaceWith && m_qtssReplaceWith)
		m_qteReplaceWith->SetText(m_qtssReplaceWith);
	CheckDlgButton(m_hwnd, kctidFindMatchWrtSys,
		m_fMatchOldWritingSystem ? BST_CHECKED : BST_UNCHECKED);

	TabCtrl_SetCurSel(m_hwndTab, m_fReplaceTab ? 1 : 0);

	// Retrieving values using GetCtls is safe now we've done any copying we need to in the
	// other direction.
	m_fOkToGetCtls = true;

} // AfFindRepDlg::FillCtls.

static const achar * s_kFindRepRegTitle = _T("Find and Replace Dialog");
static const achar * s_kFindRepRegPos = _T("Position");
static const achar * s_kFindRepRegExpand = _T("Expanded");

void AfFindRepDlg::LoadWindowPosition()
{
	Assert(::IsWindow(m_hwnd)); // Make sure the window handle is valid.

	Point pt;
	FwSettings * pfws = AfApp::GetSettings();
	if (pfws->GetBinary(s_kFindRepRegTitle, s_kFindRepRegPos, (BYTE *)&pt, isizeof(pt)))
		::SetWindowPos(m_hwnd, NULL, pt.x, pt.y, 0, 0, SWP_NOZORDER | SWP_NOSIZE);
	pfws->GetBinary(s_kFindRepRegTitle, s_kFindRepRegExpand, (BYTE *)&m_fShowMoreControls,
		isizeof(m_fShowMoreControls));
}

void AfFindRepDlg::SaveWindowPosition()
{
	Assert(::IsWindow(m_hwnd)); // Make sure the window handle is valid.

	Rect rc;
	::GetWindowRect(m_hwnd, &rc);
	Point pt = rc.TopLeft();

	FwSettings * pfws = AfApp::GetSettings();
	pfws->SetBinary(s_kFindRepRegTitle, s_kFindRepRegPos, (BYTE *)&pt, isizeof(pt));
	pfws->SetBinary(s_kFindRepRegTitle, s_kFindRepRegExpand, (BYTE *)&m_fShowMoreControls,
		isizeof(m_fShowMoreControls));
}


/*----------------------------------------------------------------------------------------------
	Handle a change in a combo box made by selecting a list item.
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::OnComboChange(NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	// Get the current index from the combo box.
	int icb = ::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0);

	switch (pnmh->idFrom)
	{
	case kctidFindWhat:
		// Just update the text in the box to the selected item.
		// TODO 1348(JohnT): when this becomes a combo...
		{
			//HWND hwndCtrl = ::GetDlgItem(m_hwnd, kctidFindWhat);


		}
		break;
	case kctidFindWhichEntries:
		// Controls whether to search all records or just the current one.
		m_fAllEntries = (icb != 1);
		break;
	case kctidFindFieldCombo:
		// Choice of which field to search in. If the user does anything in this control.
		// turn on the kctidFindFieldCheck check box.
		::CheckDlgButton (m_hwnd, kctidFindFieldCheck, BST_CHECKED);
		// TODO JohnT: make use of the info concerning which item he chose.
		break;
	case kctidFindAnimation:
		// We get this notification when we start the animation. Ignore it.
		break;
	default:
		Assert(false);	// We shouldn't get here.
	}

	lnRet = 0;
	EnableDynamicControls();
	return true;
} // AfFindRepDlg::OnComboChange.

/*----------------------------------------------------------------------------------------------
	Get the values of all controls into the member variables and the pattern. This is
	suppressed until we are far enough through dialog initialization to have valid information
	in the controls.

	ENHANCE (SharonC): when we have real pattern-matching, the calls to StoreWsAndStyleInString
	might be able to go away.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::GetCtls()
{
	if (!m_fOkToGetCtls)
		return;

	ITsStringPtr qtssFindWhatFixed;
	if (m_qteFindWhat)
	{
		m_qteFindWhat->GetText(&m_qtssFindWhat);
		StoreWsAndStyleInString(m_qteFindWhat, m_qtssFindWhat, &qtssFindWhatFixed);
	}

	// Todo 1348 (JohnT): appropriate Combo box get text, and update the list.

	ITsStringPtr qtssPattern;
	CheckHr(m_qxpat->get_Pattern(&qtssPattern));
	ComBool fEqual = false;
	if (qtssPattern && qtssFindWhatFixed)
		CheckHr(qtssPattern->Equals(qtssFindWhatFixed, &fEqual));
	if (!fEqual)
	{
		CheckHr(m_qxpat->putref_Pattern(qtssFindWhatFixed));
		CheckHr(m_qxpat->putref_Limit(NULL)); // string changed, new search.
	}
	CheckHr(m_qxpat->putref_Overlay(m_qvo));
	CheckHr(m_qxpat->put_MatchCase(m_fMatchCase));
	CheckHr(m_qxpat->put_MatchDiacritics(m_fMatchDiacritics));
	CheckHr(m_qxpat->put_MatchWholeWord(m_fMatchWholeWord));
	CheckHr(m_qxpat->put_MatchOldWritingSystem(m_fMatchOldWritingSystem));
	if (m_qteReplaceWith && m_fReplaceTab) // not when first starting up
	{
		ITsStringPtr qtssReplaceWithFixed;
		m_qteReplaceWith->GetText(&m_qtssReplaceWith);
		StoreWsAndStyleInString(m_qteReplaceWith, m_qtssReplaceWith, &qtssReplaceWithFixed);
		CheckHr(m_qxpat->putref_ReplaceWith(qtssReplaceWithFixed));
	}
	CheckHr(m_qxpat->put_ShowMore(m_fShowMoreControls));
}

/*----------------------------------------------------------------------------------------------
	If you have an empty string with the old writing system or style set, make sure the string
	has those things contained in it explicitly (not just in the selection's text props).

	TODO: This method is probably obsolete.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::StoreWsAndStyleInString(FindTssEdit * pte, ITsString * ptss,
	ITsString ** pptssFixed)
{
	int cch = 0;
	if (ptss)
		CheckHr((ptss)->get_Length(&cch));
	if (cch > 0)
	{
		*pptssFixed = ptss;
		AddRefObj(*pptssFixed);
		return;
	}

	IVwSelectionPtr qvwsel;
	TtpVec vqttp;
	VwPropsVec vqvps;
	if (!pte->GetCharacterProps(&qvwsel, vqttp, vqvps))
		return;
	Assert(vqttp.Size() == 1);
//	int ws;
//	int var;
//	SmartBstr sbstr;
//	CheckHr(vqttp[0]->GetIntPropValues(ktptWs, &var, &ws));
//	CheckHr(vqttp[0]->GetStrPropValue(ktptNamedStyle, &sbstr));
//	CheckHr(vqttp[0]->GetStrPropValue(ktptTags, &sbstr));

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	CheckHr(qtsf->MakeStringWithPropsRgch(NULL, 0, vqttp[0], pptssFixed));
}

/*----------------------------------------------------------------------------------------------
	Implement the actual find operation. (This is also "find next" in the Replace dialog.)
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::OnFindNow()
{
	// Change the buttons and animate the magnifying glass. This will stop when out of scope:
	DisplayBusy DB(this);

	GetCtls();
	// Todo 1348 (JohnT): appropriate Combo box get text, and update the list.

	// Do the actual search.
	// Todo 1733 (JohnT): if m_fShowList is true, do something different...
	// Make sure range selection shows up. If the old selection was an IP, it may have
	// been disabled altogether.
	AfVwRootSite * pvrs = RootSite();
	// NOTE: if you want to set a break point in the find-and-replace code, set it AFTER
	// the line above, because bringing up the debugger gets the whole thing confused about
	// which is the main window we want to search in.

	if (pvrs)
	{
		pvrs->Activate(vssOutOfFocus);

		// Set up mechanism to be able to abort search:
		m_qxserkl.CreateInstance(CLSID_VwSearchKiller);
		m_qxserkl->put_Window((int)m_hwnd);

		// search forward, don't launch dialog again if empty str, focus returns to this window.
		pvrs->NextMatch(true, false, true, m_fReplaceTab, m_hwnd, m_qxserkl);
	}

	if (m_fCloseOnFindNow)
		OnApply(true); // inherited implementation closes the dialog.
}

/*----------------------------------------------------------------------------------------------
	Handle a click on More/Less.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::OnFindMoreLess()
{
	// Change the flag.
	m_fShowMoreControls = !m_fShowMoreControls;

	Rect rc;
	::GetWindowRect(m_hwnd, &rc);
	Rect rcTab;
	HWND hwndTab = ::GetDlgItem(m_hwnd, kctidFindTab);
	::GetWindowRect(hwndTab, &rcTab);

	if (m_fShowMoreControls)
	{
		rc.bottom += m_dypMoreControls;
		rcTab.bottom += m_dypMoreControls;
		AfGfx::EnsureVisibleRect(rc);
		::MoveWindow(m_hwnd, rc.left, rc.top, rc.Width(), rc.Height(), true);
	}
	else
	{
		rcTab.bottom -= m_dypMoreControls;
		::SetWindowPos(m_hwnd, NULL, 0, 0, rc.Width(), rc.Height() - m_dypMoreControls,
			SWP_NOZORDER | SWP_NOMOVE);
	}
	::SetWindowPos(hwndTab, NULL, 0, 0, rcTab.Width(), rcTab.Height(),
		SWP_NOZORDER | SWP_NOMOVE);
	EnableMoreControls();
	EnableDynamicControls();
}

/*----------------------------------------------------------------------------------------------
	Enable or disable the extra controls as appropriate. In particular disable them all when
	they are invisible so the user can't tab to them.
	The Full FieldWorks Replace dialog may override this if the "Search In" controls are not
	optional in that version of the dialog.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::EnableMoreControls()
{
	// Toggle the title of the button. It is the opposite of the current state.
	StrApp str(m_fShowMoreControls ? kstidLessPlain : kstidMorePlain);
	::SetWindowText(::GetDlgItem(m_hwnd, kctidFindMoreLess), str.Chars());
	m_qbtnMoreLess->SetArrowType(m_fShowMoreControls ? kbtLess : kbtMore);

	if (m_fShowMoreControls)
	{
		// These items just barely show even in the smaller window, so we have to hide them
		// when not relevant.
		::ShowWindow(::GetDlgItem(m_hwnd, kctidSearchOptionsLabel), true);
		::ShowWindow(::GetDlgItem(m_hwnd, kctidSeparator), true);
		// These groups of controls are made actually invisible if the current application
		// does not need them.
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindWhichEntries), m_fCanWhichEntries);
		::ShowWindow(::GetDlgItem(m_hwnd, kctidFindWhichEntries), m_fCanWhichEntries);

		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindFieldCheck), m_fCanSelectField);
		::ShowWindow(::GetDlgItem(m_hwnd, kctidFindFieldCheck), m_fCanWhichEntries);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindFieldCombo), m_fCanSelectField);
		::ShowWindow(::GetDlgItem(m_hwnd, kctidFindFieldCombo), m_fCanWhichEntries);

		// If none of the "find which items" controls are visible, hide the border around them.
		// If either is in use show the border.
		ShowWindow(::GetDlgItem(m_hwnd, kctidFindInBorder),
			m_fCanWhichEntries || m_fCanSelectField);

		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindMatchWholeWord), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindMatchCase), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindMatchDiacritics), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindMatchWrtSys), TRUE);
		// This is always disabled for now because not implemented.
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindSpecial), 0);
		// This is always enabled for now.
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindFormat), TRUE);
	}
	else
	{
		::ShowWindow(::GetDlgItem(m_hwnd, kctidSearchOptionsLabel), false);
		::ShowWindow(::GetDlgItem(m_hwnd, kctidSeparator), false);

		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindWhichEntries), 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindFieldCheck), 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindFieldCombo), 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindMatchWholeWord), 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindMatchCase), 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindMatchDiacritics), 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindMatchWrtSys), 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindSpecial), 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFindFormat), 0);
	}

	// Show or hide the Replace controls depending on whether the tab is visible.
	// Disable if not visible for sake of Tabbing.
	::ShowWindow(::GetDlgItem(m_hwnd, kctidFindReplaceWith), m_fReplaceTab);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFindReplaceWith), m_fReplaceTab);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidReplaceLabel), m_fReplaceTab);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidFindReplace), m_fReplaceTab);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFindReplace), m_fReplaceTab);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidFindReplaceAll), m_fReplaceTab);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFindReplaceAll), m_fReplaceTab);
	// Move the More/Less button appropriately.
	Rect rc = m_fReplaceTab ? m_rcMoreLess : m_rcMoreLessFind;
	::MoveWindow(::GetDlgItem(m_hwnd, kctidFindMoreLess), rc.left, rc.top,
		rc.Width(), rc.Height(), true);
}

/*----------------------------------------------------------------------------------------------
	Toggle a check box.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::Toggle(bool & fVal, int cid)
{
	fVal = !fVal;
	::CheckDlgButton (m_hwnd, cid, (fVal) ? BST_CHECKED : BST_UNCHECKED);
	EnableDynamicControls();
}

/*----------------------------------------------------------------------------------------------
	Handles a click on a button.
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::OnButtonClick(NMHDR * pnmh, long & lnRet)
{
	// These static variables are part of a "hack" to get round the fact that when you press
	// the enter key to select a currently highlighted menu item dropped down from the 'Format'
	// button the message is sent to the button again and so this code is re-executed when the
	// first execution is still doing ::TrackPopupMenu. Normally, s_fShowingMenu is false, but
	// it is set true while ::TrackPopupMenu is being executed so that we can take special
	// action if we detect that it is already true in "case kctidFindFormat:" below.
	// Note that this problem exists only in the case of a modeless dialog.
	static bool s_fShowingMenu = false;
	static HMENU hmenuPopup;
	static HMENU hmenuWs;
	static HMENU hmenuStyle;

	AssertPtr(pnmh);

	switch (pnmh->idFrom)
	{
	case kctidFindStop:
		m_qxserkl->put_AbortRequest(ComBool(true));
		break;
	case kctidFindFindNow:
		// Go start the Find, in a separate thread.
		OnFindNow();
		EnableDynamicControls();
		break;
	case kctidFindClose:
		// Close the dialog without doing anything.
		OnCancel();
		break;
	// kctidHelp is handled by framework.
	case kctidFindMoreLess:
		// Toggle between the "more" and "less" versions of the dialog.
		OnFindMoreLess();
		break;
	case kctidListResults:
		// Toggle the check box for whether to show results as a list.
		Toggle(m_fListResults, kctidListResults);
		break;
	case kctidFindFieldCheck:
		// Toggle the check box for whether to restrict the search to a particular field.
		Toggle(m_fFieldCheck, kctidFindFieldCheck);
		break;
	case kctidFindMatchWholeWord:
		// Toggle the check box for whether to search for whole words.
		Toggle(m_fMatchWholeWord, kctidFindMatchWholeWord);
		break;
	case kctidFindMatchCase:
		// Toggle the check box for whether to require an exact case match.
		Toggle(m_fMatchCase, kctidFindMatchCase);
		break;
	case kctidFindMatchDiacritics:
		// Toggle the check box for whether to require exact match on diacritics.
		Toggle(m_fMatchDiacritics, kctidFindMatchDiacritics);
		break;
	case kctidFindMatchWrtSys:
		// Toggle the check box for whether to require exact match on diacritics.
		Toggle(m_fMatchOldWritingSystem, kctidFindMatchWrtSys);
		break;
	case kctidFindSpecial:
		// Pull down a menu showing special (pattern matching) character strings.
		Animate_Stop(::GetDlgItem(m_hwnd, kctidFindAnimation)); // Just for a test...
		break;
	case kctidFindFormat:
		// Create a dynamic popup menu, or take other action if the menu is already being shown.
		{ // Block to prevent spurious warnings about uninitialized variables
			if (s_fShowingMenu)
			{
				HMENU hmenu = NULL;
				// Find out whether the Ws (0) or Style (1) menu is highlighted.
				if ((::GetMenuState(hmenuPopup, 0, MF_BYPOSITION) & MF_HILITE) == MF_HILITE)
					hmenu = hmenuWs;
				else
				{
					if ((::GetMenuState(hmenuPopup, 1, MF_BYPOSITION) & MF_HILITE) == MF_HILITE)
						hmenu = hmenuStyle;
				}
				if (!hmenu)
					return true;	// Should not get here.
				int cItems = GetMenuItemCount(hmenu);
				if (cItems < 0) return true;	// Function failed.
				int iItem;
				for (iItem = 0; iItem < cItems; ++iItem)
				{
					if ((::GetMenuState(hmenu, iItem, MF_BYPOSITION) & MF_HILITE) == MF_HILITE)
						break;
				}
				if (iItem == cItems)
					return true;	// Nothing highlighted.
				uint idMenuItem = ::GetMenuItemID(hmenu, iItem);
				::SendMessage(m_hwnd, WM_COMMAND, idMenuItem, 0); // Action the menu command.
				return true;
			}

			hmenuPopup = ::CreatePopupMenu(); // The whole thing.

			// Old writing system submenu.
			hmenuWs = ::CreatePopupMenu();
			// Insert an "expandable" item. It gets replaced by the items generated by
			// CmdFindFmtWsItems. The actual string inserted is a meaningless dummy.
			::AppendMenu(hmenuWs, MF_STRING, kcidExpFindFmtWs, _T("a"));
			// Now put the items into the main menu.
			StrApp strWs(kstidFindFmtWs);
			::AppendMenu(hmenuPopup, MF_POPUP, (uint)hmenuWs, strWs.Chars());

			// Style submenu--handled in CmdFindFmtStyleItems.
			hmenuStyle = ::CreatePopupMenu();
			::AppendMenu(hmenuStyle, MF_STRING, kcidExpFindFmtStyle, _T("b"));
			StrApp strStyle(kstidFindFmtStyle);
			::AppendMenu(hmenuPopup, MF_POPUP, (uint)hmenuStyle, strStyle.Chars());

			// Now display the popup menu and track it until it goes away.  If the user selects
			// an item, it is processed by CmdFindFmtWsItems or CmdFindFmtStyleItems.
			Rect rc;
			::GetWindowRect(pnmh->hwndFrom, &rc);
			s_fShowingMenu = true;
			::TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_RIGHTBUTTON, rc.left, rc.bottom,
				0, m_hwnd, NULL);
			s_fShowingMenu = false;
			::DestroyMenu(hmenuPopup);
		}
		return true;
	case kctidFindReplace:
		// Go start the Find.
		OnFindReplace();
		EnableDynamicControls();
		break;
	case kctidFindReplaceAll:
		// Go start the Find, in a new thread.
		OnFindReplaceAll();
		EnableDynamicControls();
		break;
	default:
		Assert(false);
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Process draw messages.
	No special ones yet, but keep the method, as we may have to make some use of it
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::OnDrawChildItem(DRAWITEMSTRUCT * pdis)
{
	return AfWnd::OnDrawChildItem(pdis);
}

/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	switch(pnmh->code)
	{
	case CBN_SELENDOK:
		// This is used for color combo box...see FmtParaDlg if needed.
		break;

	case UDN_DELTAPOS: // Spin control is activated.
	case EN_KILLFOCUS: // Edit control modified.
		// return OnDeltaSpin(pnmh, lnRet); (if we have spin controls...none at present)
		break;

	case EN_CHANGE: // Change detected in edittext box.
		// If necessary see FmtParaDlg for an example.
		EnableDynamicControls(); // may need to enable or disable the Find button
		break;

	case CBN_SELCHANGE: // Combo box item changed by choosing in the list.
		return OnComboChange(pnmh, lnRet);

	case BN_CLICKED:
		return OnButtonClick(pnmh, lnRet);

	case TCN_SELCHANGE:
		{
			// Change to requested Tab.
			int itab = TabCtrl_GetCurSel(m_hwndTab);
			Assert((uint)itab < 2);
			GetCtls();
			SetReplace(itab != 0);
			FillCtls();
			EnableMoreControls();
			EnableDynamicControls();
			return true;
		}
	// Default is do nothing.
	}

	return AfWnd::OnNotifyChild(ctidFrom, pnmh, lnRet);
}

/*----------------------------------------------------------------------------------------------
	When closing this dialog, restore the focus to the main window,
	so any selection will show up.
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::OnCancel()
{
	if (m_fBusy)
	{
		m_qxserkl->put_AbortRequest(ComBool(true));	// Stop the current search.
		m_fBusy = false;						// Prevent DisplayBusy destructor from crashing.
		m_fOkToGetCtls = false;					// Prevent GetCtls() from crashing.
	}
	bool f = SuperClass::OnCancel();

	if (f)
		::SetFocus(m_qvrsLast->Window()->Hwnd());

	return f;
}

/*----------------------------------------------------------------------------------------------
	This method implements the expandable items for old writing systems in the popup menu that
	appears when the user clicks the "Format" button in the Find dialog. It has three
	responsibilities.
	1)	If pcmd->m_rgn[0] == AfMenuMgr::kmaExpandItem, it is being called to replace the dummy
		item by adding new items. It generates an item for each currently used old writing system,
		by stealing the items from the Formatting toolbar.
	2)	If pcmd->m_rgn[0] == AfMenuMgr::kmaGetStatusText, it is being called to get the status
		bar string for an expanded item. Nothing is available at present, so return false.
	3)	If pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand, it is being called because the user
		selected an expandable menu item. Figure which old writing system it is and apply it
		to the selection in the view.

	Expanding items:
		pcmd->m_rgn[1] -> Contains the handle to the menu (HMENU) to add items to.
		pcmd->m_rgn[2] -> Contains the index in the menu where you should start inserting items.
		pcmd->m_rgn[3] -> This value must be set to the number of items that you inserted.
	The expanded items will automatically be deleted when the menu is closed. The dummy
	menu item will be deleted for you, so don't do anything with it here.

	Getting the status bar text:
		pcmd->m_rgn[1] -> Contains the index of the expanded/inserted item to get text for.
		pcmd->m_rgn[2] -> Contains a pointer (StrApp *) to the text for the inserted item.
	If the menu item does not have any text to show on the status bar, return false.

	Performing the command:
		pcmd->m_rgn[1] -> Contains the handle of the menu (HMENU) containing the expanded items.
		pcmd->m_rgn[2] -> Contains the index of the expanded/inserted item to execute.

	@param pcmd Ptr to menu command
	@return true, except as noted above.
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::CmdFindFmtWsItems(Cmd * pcmd)
{
	AssertPtr(pcmd);
	Assert(pcmd->m_cid == kcidExpFindFmtWs);
	AfVwRootSite * pvrs = RootSite();
	if (!pvrs)
		return false; // can't do anything useful.

	int ma = pcmd->m_rgn[0];
	if (ma == AfMenuMgr::kmaExpandItem)
	{
		HMENU hmenu = (HMENU)pcmd->m_rgn[1];
		int imni = pcmd->m_rgn[2];
		int & cmniAdded = pcmd->m_rgn[3];
		int cws;
		m_qsda->get_WritingSystemsOfInterest(0, NULL, &cws);
		m_vws.Clear();
		int * prgenc = NewObj int[cws];
		m_qsda->get_WritingSystemsOfInterest(cws, prgenc, &cws);
		// Compare this code with WpMainWnd::OnToolBarButtonAdded, case kcidFmttbWrtgSys.
		// Todo 1350 (JohnT): allow code to be shared, or update both when we support
		// multiple old writing systems.
		Vector<StrApp> vstrbNames;
		for (int iws = 0; iws < cws; iws++)
		{
			StrApp strb;
			pvrs->UiNameOfWs(prgenc[iws], &strb);
			int istrbTmp;
			for (istrbTmp = 0; istrbTmp < vstrbNames.Size(); istrbTmp++)
			{
				if (vstrbNames[istrbTmp] > strb)
					break;
			}
			vstrbNames.Insert(istrbTmp, strb);
			m_vws.Insert(istrbTmp, prgenc[iws]);
		}

		int encSelected;
		StrUni stuSelectedStyle;
		SelectedWsAndStyle(&encSelected, &stuSelectedStyle);

		for (int istrb = 0; istrb < vstrbNames.Size(); istrb++)
		{
			int cid = kcidMenuItemDynMin + istrb;
			::InsertMenu(hmenu, imni + istrb, MF_BYPOSITION, cid, vstrbNames[istrb].Chars());
			if (m_vws[istrb] == encSelected)
				::CheckMenuItem(hmenu, cid, MF_CHECKED);
		}

		// Give the overall number of items.
		Assert(cws == vstrbNames.Size());
		cmniAdded = cws;
		delete[] prgenc;
		return true;
	}
	else if (ma == AfMenuMgr::kmaDoCommand)
	{
		// The user selected an expanded menu item, so perform the command now.
		//    m_rgn[1] holds the menu handle.
		//    m_rgn[2] holds the index of the selected item.

		int iws = pcmd->m_rgn[2];
		int ws = m_vws[iws];

		// Apply the change. Note that we are applying it to the find edit box,
		// NOT to the window in which we are finding.
		IVwSelectionPtr qvwsel;
		TtpVec vqttp;
		VwPropsVec vqvps;
		if (!m_qteLastFocus->GetCharacterProps(&qvwsel, vqttp, vqvps))
			return false;

		// Todo 1350 (JohnT): when we support multiple old writing systems we need
		// to obtain and use the appropriate one here.
		m_qteLastFocus->ApplyWritingSystem(vqttp, ws, qvwsel);
		// This seems as if it should work but it does not. Perhaps the dialog box
		// does something about setting focus back to the Format button after
		// closing the menu, which comes after this code executes? Perhaps something
		// else?
		//::SetFocus(m_qteLastFocus->Hwnd());

		// The PostMessage below does put the focus back where we want, but it makes
		// a selection we don't want.

		// This seems to do it. However, the documentation says that it tries to
		// select all the text in the edit box. This (fortunately for us because
		// it's not what we want) fails, probably because we don't have a standard
		// edit box control. If we ever somehow enhance our edit control so it
		// responds to whatever Windows does to try to select all the text, we'll
		// then have to defeat it here.
		// Or, we could try posting a message which we later process to do the SetFocus
		// call above. (Probably, though, we can't just post WM_SETFOCUS. That would
		// omit the sending of WM_KILLFOCUS to the old focus window.)
		//::PostMessage(m_hwnd, WM_NEXTDLGCTL, (WPARAM)m_qteLastFocus->Hwnd(), 1L);

		::PostMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(kcidRestoreFocus, 0), NULL);

		// Since the user is being specific about old writing systems, force the check box
		// relating to them to be on.
		m_fMatchOldWritingSystem = true;
		CheckDlgButton(m_hwnd, kctidFindMatchWrtSys, BST_CHECKED);

		EnableDynamicControls();
		return true;
	}
	else if (ma == AfMenuMgr::kmaGetStatusText)
		return false; // don't have any useful status text to show.
	Assert(false);
	return false;
}

/*----------------------------------------------------------------------------------------------
	This method implements the expandable items for styles in the popup menu that
	appears when the user clicks the "Format" button in the Find dialog.

	See the comments on CmdFindFmtStyleItems.
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::CmdFindFmtStyleItems(Cmd * pcmd)
{
	AssertPtr(pcmd);
	Assert(pcmd->m_cid == kcidExpFindFmtStyle);
	AfVwRootSite * pvrs = RootSite();
	if (!pvrs)
		return false; // can't do anything useful.

	const int kStyleOffset = 100;

//	StrUni stuDefParaChars;
	StrUni stuNoStyle;

	int ma = pcmd->m_rgn[0];
	if (ma == AfMenuMgr::kmaExpandItem)
	{
		HMENU hmenu = (HMENU)pcmd->m_rgn[1];
		int imni = pcmd->m_rgn[2];
		int & cmniAdded = pcmd->m_rgn[3];

		m_vstuStyles.Clear();
		int cstyles;
		CheckHr(m_qasts->get_CStyles(&cstyles));
		for (int istyle = 0; istyle < cstyles; istyle++)
		{
			HVO hvoStyle;
			SmartBstr sbstrName;
			int nType;
			CheckHr(m_qasts->get_NthStyle(istyle, &hvoStyle));
			CheckHr(m_qsda->get_UnicodeProp(hvoStyle, kflidStStyle_Name,
				&sbstrName));
			CheckHr(m_qsda->get_IntProp(hvoStyle, kflidStStyle_Type, &nType));
			// For now we are only including character styles.
			// ENHANCE (SharonC): allow them to search for paragraph styles.
			if (nType == kstCharacter)
			{
				StrUni stuName(sbstrName.Chars());
				m_vstuStyles.Push(stuName);
			}
		}
		// Add an item for <no style>. Temporarily remove the one for Default Paragraph
		// Characters. The full solution has both, but version 2 (Feb 2004) has
		// only <no Style>, which functions like DefParaChars used to function. Thus, for
		// the moment, the only change is in what text the user sees in the list. JIRA DN-157.
//		stuDefParaChars.Load(kstidDefParaChars);
//		m_vstuStyles.Push(stuDefParaChars);
		stuNoStyle.Load(kstidNoStyle);
		m_vstuStyles.Push(stuNoStyle);

		// Sort them alphabetically.
		for (int istyle1 = 0; istyle1 < m_vstuStyles.Size() - 1; istyle1++)
		{
			for (int istyle2 = istyle1 + 1; istyle2 < m_vstuStyles.Size(); istyle2++)
			{
				if (m_vstuStyles[istyle1] > m_vstuStyles[istyle2])
				{
					StrUni stu; stu = m_vstuStyles[istyle1];
					m_vstuStyles[istyle1] = m_vstuStyles[istyle2];
					m_vstuStyles[istyle2] = stu;
				}
			}
		}

		int ws;
		StrUni stuSelectedStyle;
		SelectedWsAndStyle(&ws, &stuSelectedStyle);

		for (int istyle = 0; istyle < m_vstuStyles.Size(); istyle++)
		{
			StrApp strName(m_vstuStyles[istyle]);
			int cid = kcidMenuItemDynMin + istyle + kStyleOffset;
			::InsertMenu(hmenu, imni + istyle, MF_BYPOSITION, cid, strName.Chars());
			if (stuSelectedStyle == m_vstuStyles[istyle])
				::CheckMenuItem(hmenu, cid, MF_CHECKED);
		}

		cmniAdded = m_vstuStyles.Size();
		return true;
	}
	else if (ma == AfMenuMgr::kmaDoCommand)
	{
		// The user selected an expanded menu item, so perform the command now.
		//    m_rgn[1] holds the menu handle.
		//    m_rgn[2] holds the index of the selected item.

		int istyle = pcmd->m_rgn[2] - kStyleOffset;
		StrUni stuStyle = m_vstuStyles[istyle];
		stuNoStyle.Load(kstidNoStyle);
		if (m_vstuStyles[istyle] == stuNoStyle)
			stuStyle = L"";

		// Apply the change. Note that we are applying it to the find edit box,
		// NOT to the window in which we are finding.
		IVwSelectionPtr qvwsel;
		TtpVec vqttp;
		VwPropsVec vqvps;
		if (!m_qteLastFocus->GetCharacterProps(&qvwsel, vqttp, vqvps))
			return false;
		m_qteLastFocus->RemoveCharFormatting(qvwsel, vqttp, stuStyle.Bstr());

		// This seems as if it should work but it does not. Perhaps the dialog box
		// does something about setting focus back to the Format button after
		// closing the menu, which comes after this code executes? Perhaps something
		// else?
		//::SetFocus(m_qteLastFocus->Hwnd());

		// The PostMessage below does put the focus back where we want, but it makes
		// a selection we don't want.

		// This seems to do it. However, the documentation says that it tries to
		// select all the text in the edit box. This (fortunately for us because
		// it's not what we want) fails, probably because we don't have a standard
		// edit box control. If we ever somehow enhance our edit control so it
		// responds to whatever Windows does to try to select all the text, we'll
		// then have to defeat it here.
		// Or, we could try posting a message which we later process to do the SetFocus
		// call above. (Probably, though, we can't just post WM_SETFOCUS. That would
		// omit the sending of WM_KILLFOCUS to the old focus window.)
		//::PostMessage(m_hwnd, WM_NEXTDLGCTL, (WPARAM)m_qteLastFocus->Hwnd(), 1L);

		::PostMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(kcidRestoreFocus, 0), NULL);

		EnableDynamicControls();
		return true;
	}
	else if (ma == AfMenuMgr::kmaGetStatusText)
		return false; // don't have any useful status text to show.
	Assert(false);
	return false;
}

/*----------------------------------------------------------------------------------------------
	Return the selected ws/ows and style, if there is only one, to be indicated in the Format
	menus.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::SelectedWsAndStyle(int * pws, StrUni * pstuStyle)
{
	IVwSelectionPtr qvwsel;
	TtpVec vqttp;
	VwPropsVec vqvps;
	if (!m_qteLastFocus->GetCharacterProps(&qvwsel, vqttp, vqvps))
		return;
	if (vqttp.Size() == 0)
		return;

	int wsSoFar, nVar;
	SmartBstr sbstrStyle;
	CheckHr(vqttp[0]->GetIntPropValues(ktptWs, &nVar, &wsSoFar));
	CheckHr(vqttp[0]->GetStrPropValue(ktptNamedStyle, &sbstrStyle));
	StrUni stuStyleSoFar(sbstrStyle.Chars());
	for (int ittp = 1; ittp < vqttp.Size(); ittp++)
	{
		int ws;
		if (wsSoFar != 0)
		{
			CheckHr(vqttp[ittp]->GetIntPropValues(ktptWs, &nVar, &ws));
			if (ws != wsSoFar)
			{
				wsSoFar = 0;
			}
		}
		if (stuStyleSoFar != L"Conflict")
		{
			CheckHr(vqttp[ittp]->GetStrPropValue(ktptNamedStyle, &sbstrStyle));
			StrUni stuStyle(sbstrStyle.Chars());
			if (stuStyleSoFar != stuStyle)
				stuStyleSoFar = L"Conflict";
		}
	}
	*pws = wsSoFar;
	if (stuStyleSoFar == L"Conflict")
		*pstuStyle = L"";
	else if (stuStyleSoFar == L"")
		pstuStyle->Load(kstidNoStyle);
	else
		*pstuStyle = stuStyleSoFar;
}

/*----------------------------------------------------------------------------------------------
	Note that the specified edit control has the focus.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::EditBoxFocus(FindTssEdit * pte)
{
	// Remember which one has the focus so we can properly apply commands like Format.
	m_qteLastFocus = pte;

	// We wil make the root box in the other pane disabled, otherwise its selection continues to
	// show up if it is a range.
	FindTssEdit * pteOther;
	if (pte == m_qteFindWhat)
		pteOther = m_qteReplaceWith;
	else
		pteOther = m_qteFindWhat;
	IVwRootBoxPtr qrootb;
	pte->get_RootBox(&qrootb);
	ITsStringPtr qtss;
	CheckHr(pte->GetText(&qtss));
	int cch = 0;
	if (qtss)
		CheckHr(qtss->get_Length(&cch));
	if (!cch)
	{
		// Empty: Standard windows dialog stuff does not guarantee to make a selection.
		// If there is one, it must be an IP in the empty string, so don't mess with it
		// (the user might have set some formatting properties on it!).
		// But if there isn't one, make one.
		IVwSelectionPtr qvwsel;
		CheckHr(qrootb->get_Selection(&qvwsel));
		if (!qvwsel)
			qrootb->MakeSimpleSel(true, true, false, true, NULL);
	}
	if (qrootb)
		CheckHr(qrootb->Activate(vssEnabled));
	if (!pteOther) // Can be NULL, during start-up at least.
		return;
	pteOther->get_RootBox(&qrootb);
	if (!qrootb)
		return; // It certainly can't have a selection, then!
	CheckHr(qrootb->Activate(vssDisabled));
}

/*----------------------------------------------------------------------------------------------
	Set the focus to m_qteLastFocus, without changing its selection. This is a message so
	we can make it the target of a WM_COMMAND and thus do a PostMessage.
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::CmdRestoreFocus(Cmd * pcmd)
{
	::SetFocus(m_qteLastFocus->Hwnd());
	return true;
}

/*----------------------------------------------------------------------------------------------
	Set the state of controls in the dialog whose values depend on the state of other controls.
	This is called whenever text editing occurs, when check boxes change, etc.
	Currently we don't have any dynamic controls.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::EnableDynamicControls()
{
	if (m_fBusy)
		return;

	GetCtls();
	EnableReplaceButton();
	// Can't search or replace unless we have something to match, either actual characters or
	// a old writing system, style, or tag.
	// ENHANCE (SharonC): Rework this when we have real patterns.
	int cch = 0;
	Assert(m_qtssFindWhat.Ptr() != NULL);
	Assert(m_qtssFindWhat);
	CheckHr(m_qtssFindWhat->get_Length(&cch));
	IVwSelectionPtr qvwsel;
	TtpVec vqttp;
	VwPropsVec vqvps;
	bool fMatchStyle = false;
	bool fMatchTags = false;
	if (cch == 0 && m_qteFindWhat->GetCharacterProps(&qvwsel, vqttp, vqvps))
	{
		Assert(vqttp.Size() == 1);
		SmartBstr sbstrStyle;
		CheckHr(vqttp[0]->GetStrPropValue(ktptNamedStyle, &sbstrStyle));
		if (sbstrStyle.Length() > 0)
			fMatchStyle = true;

		SmartBstr sbstrTags;
		CheckHr(vqttp[0]->GetStrPropValue(ktptTags, &sbstrTags));
		int cguid = BstrLen(sbstrTags) / kcchGuidRepLength;
		OLECHAR * pch = sbstrTags;
		for (int iguid = 0; m_qvo && iguid < cguid; iguid++)
		{
			ComBool fHidden;
			COLORREF clrFore, clrBack, clrUnder;
			int unt, cchAbbr, cchName;
			CheckHr(m_qvo->GetDispTagInfo(pch, &fHidden, &clrFore, &clrBack, &clrUnder, &unt,
				NULL, 0, &cchAbbr, NULL, 0, &cchName));
			if (!fHidden)
			{
				fMatchTags = true;
				break;
			}
			pch += kcchGuidRepLength;
		}
	}
	bool fEnable = (cch != 0 || m_fMatchOldWritingSystem || fMatchStyle || fMatchTags);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFindReplaceAll), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFindFindNow), fEnable);
}

/*----------------------------------------------------------------------------------------------
	Store the overlay in the dialog. If necessary, readjust the size of the dialog
	depending on whether we want to leave room for overlay tags.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::ToggleOverlays(IVwOverlay * pvo)
{
	m_qvo = pvo;
	if (m_fOverlays == (m_qvo.Ptr() != NULL))
		return; // nothing to adjust in the dialog layout

	m_fOverlays = (m_qvo.Ptr() != NULL);
	int nDir = (m_fOverlays) ? +1 : -1;
	int dy = (m_dypFind + m_dypReplace) * nDir;

	// Make the dialog and tab larger or smaller.
	Rect rcDlg;
	::GetWindowRect(m_hwnd, &rcDlg);
	POINT pt = {0,0};
	MapWindowPoints(m_hwnd, NULL, &pt, 1); // Gets top left of client area in screen coords.

	int y = rcDlg.Height() + dy;
	::SetWindowPos(m_hwnd, NULL, 0, 0, rcDlg.Width(), y,
		SWP_NOZORDER | SWP_NOMOVE);

	Rect rcTab;
	HWND hwndTab = ::GetDlgItem(m_hwnd, kctidFindTab);
	::GetWindowRect(hwndTab, &rcTab);
	y = rcTab.Height() + dy;
	::SetWindowPos(hwndTab, NULL, 0, 0, rcTab.Width(), y,
		SWP_NOZORDER | SWP_NOMOVE);

	// Make the Find and Replace boxes bigger or smaller.
	POINT ptTL = { 0, 0 };
	::ClientToScreen(m_hwnd, &ptTL);
	int dyTitle = (ptTL.y - rcDlg.top);
	int dxTmp = (ptTL.x - rcDlg.left);

	Rect rcTmp;
	HWND hwndT = ::GetDlgItem(m_hwnd, kctidFindWhat);
	::GetWindowRect(hwndT, &rcTmp);
	int w = rcTmp.Width();
	int h = rcTmp.Height() + (m_dypFind * nDir);
	rcTmp.left -= rcDlg.left;
	rcTmp.left -= dxTmp; // offset of the tab
	rcTmp.top -= rcDlg.top;
	rcTmp.top -= dyTitle;
	::MoveWindow(hwndT, rcTmp.left, rcTmp.top, w, h, true);

	hwndT = ::GetDlgItem(m_hwnd, kctidFindReplaceWith);
	::GetWindowRect(hwndT, &rcTmp);
	w = rcTmp.Width();
	h = rcTmp.Height() + (m_dypReplace * nDir);
	rcTmp.left -= rcDlg.left;
	rcTmp.left -= dxTmp;
	rcTmp.top -= rcDlg.top;
	rcTmp.top -= dyTitle;
	rcTmp.top += (m_dypFind * nDir);
	::MoveWindow(hwndT, rcTmp.left, rcTmp.top, w, h, true);

	// Move the Replace label.
	int nLblTop(rcTmp.top + 3);
	hwndT = ::GetDlgItem(m_hwnd, kctidReplaceLabel);
	::GetWindowRect(hwndT, &rcTmp);
	w = rcTmp.Width();
	h = rcTmp.Height();
	rcTmp.left -= rcDlg.left;
	rcTmp.left -= dxTmp;
	rcTmp.top = nLblTop;
	::MoveWindow(hwndT, rcTmp.left, rcTmp.top, w, h, true);

	// Move everything else up or down.
	m_rcMoreLess.top += dy;
	m_rcMoreLess.bottom += dy;
	m_rcMoreLessFind.top += dy;
	m_rcMoreLessFind.bottom += dy;
	Rect rc = m_fReplaceTab ? m_rcMoreLess : m_rcMoreLessFind;
	::MoveWindow(::GetDlgItem(m_hwnd, kctidFindMoreLess), rc.left, rc.top,
		rc.Width(), rc.Height(), true);

	int rgctid[14] = { kctidFindReplace, kctidFindReplaceAll,
		kctidFindFindNow, kctidFindClose,
		kctidFindStop, kctidHelp, kctidSearchOptionsLabel, kctidSeparator,
		kctidFindFormat, kctidFindMatchCase, kctidFindMatchDiacritics, kctidFindMatchWholeWord,
		kctidFindMatchWrtSys, kctidFindAnimation};
	for (int ictid = 0; ictid < 14; ictid++)
	{
		hwndT = ::GetDlgItem(m_hwnd, rgctid[ictid]);
		::GetWindowRect(hwndT, &rcTmp);
		w = rcTmp.Width();
		h = rcTmp.Height();
		rcTmp.left -= rcDlg.left;
		rcTmp.left -= dxTmp;
		rcTmp.top -= rcDlg.top;
		rcTmp.top -= dyTitle;
		rcTmp.top += dy;
		::MoveWindow(hwndT, rcTmp.left, rcTmp.top, w, h, true);
	}
}

/*----------------------------------------------------------------------------------------------
	Set the state for an expanded menu item in the popup for the Format button. Currently all
	are always enabled.

	cms.GetExpMenuItemIndex() returns the index of the item to set the state for.
	To get the menu handle and the old ID of the dummy item that was replaced, call
	AfMenuMgr::GetLastExpMenuInfo.

	@param cms menu command state
	@return true
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::CmsFindFmtWsItems(CmdState & cms)
{
	cms.Enable(true);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Set the state for an expanded menu item in the popup for the Format button. Currently all
	are always enabled.

	cms.GetExpMenuItemIndex() returns the index of the item to set the state for.
	To get the menu handle and the old ID of the dummy item that was replaced, call
	AfMenuMgr::GetLastExpMenuInfo.

	@param cms menu command state
	@return true
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::CmsFindFmtStyleItems(CmdState & cms)
{
	cms.Enable(true);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Change the "Cancel" button to "Close" when a replacement has occurred. (I guess this is
	to help the user infer that clicking will not cancel previous replacements.)
	NOTE: this method is no longer needed because the button now starts out with the label
	"Close."
----------------------------------------------------------------------------------------------*/
void ChangeCancelToClose(HWND hwnd)
{
	StrApp strClose(kstidCloseButton);
	::SendMessage(::GetDlgItem(hwnd, kctidFindClose), WM_SETTEXT, 0, (LPARAM)strClose.Chars());
}

/*----------------------------------------------------------------------------------------------
	Replace the given selection with the given string.

	Use the old writing system information from ptss if fUseWs is true, but all other formatting
	from the first character of the selection (which must not be empty).

	ENHANCE (SharonC): Rework this when we have real pattern-matching. In particular, the
	calls to StoreWsAndStyleInString and the fEmptySearch arg may go away.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::DoReplacement(IVwSelection * psel, ITsString * ptssRepl,
	bool fUseWs, bool fEmptySearch)
{
	int crun;
	CheckHr(ptssRepl->get_RunCount(&crun));
	int cchRepl;
	CheckHr(ptssRepl->get_Length(&cchRepl));

	// Determine whether to replace the style. We do this if any of the runs of
	// the replacement string have the style set (to something other than
	// Default Paragraph Characters).
	bool fUseStyle = false;
	bool fUseTags = false;
	for (int irun = 0; irun < crun; irun++)
	{
		TsRunInfo tri;
		ITsTextPropsPtr qttp;
		CheckHr(ptssRepl->FetchRunInfo(irun, &tri, &qttp));
		SmartBstr sbstrStyle;
		CheckHr(qttp->GetStrPropValue(ktptNamedStyle, &sbstrStyle));
		if (sbstrStyle.Length())
			fUseStyle = true;
		SmartBstr sbstrTags;
		CheckHr(qttp->GetStrPropValue(ktptTags, &sbstrTags));
		if (sbstrTags.Length())
			fUseTags = true;
	}

	// Get the properties we will apply, except for the writing system/ows and/or style.
	ITsStringPtr qtssSel;
	SmartBstr sbstr = L" ";
	ComBool fGotItAll;
	CheckHr(psel->GetFirstParaString(&qtssSel, sbstr, &fGotItAll));
	Assert(fGotItAll);
	if (!fGotItAll)
		return; // desperate defensive programming.
	ITsTextPropsPtr qttpSel;
	CheckHr(qtssSel->get_Properties(0, &qttpSel));
	ITsPropsBldrPtr qtpb;
	CheckHr(qttpSel->GetBldr(&qtpb));

	// Remove all tags that are anywhere in the Find-what string. But also include any
	// other tags that are present in the first run of the found string. So the resulting
	// replacement string will have any tags in the first char of the selection plus
	// any specified replacement tags.
	Vector<StrUni> vstuTagsToRemove;
	GetTagsToRemove(m_qtssFindWhat, &fUseTags, vstuTagsToRemove);
	Vector<StrUni> vstuTagsToInclude;
	GetTagsToInclude(qtssSel, vstuTagsToRemove, vstuTagsToInclude);

	// Make a string builder to accumulate the real replacement string.
	ITsStrBldrPtr qtsb;
	qtsb.CreateInstance(CLSID_TsStrBldr);

	// Copy the runs of ptss, adjusting the properties.
	for (int irun = 0; irun < crun; irun++)
	{
		TsRunInfo tri;
		ITsTextPropsPtr qttp;
		CheckHr(ptssRepl->FetchRunInfo(irun, &tri, &qttp));
		if (fUseWs || fUseStyle || fUseTags)
		{
			// Copy only writing system/old writing system, char style and/or tag info into the builder.
			if (fUseWs)
			{
				int ttv, tpt;
				CheckHr(qttp->GetIntPropValues(ktptWs, &ttv, &tpt));
				CheckHr(qtpb->SetIntPropValues(ktptWs, ttv, tpt));
			}
			if (fUseStyle)
			{
				SmartBstr sbstrStyle;
				CheckHr(qttp->GetStrPropValue(ktptNamedStyle, &sbstrStyle));
				CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, sbstrStyle));
			}
			if (fUseTags)
			{
				SmartBstr sbstrTagsRepl;
				CheckHr(qttp->GetStrPropValue(ktptTags, &sbstrTagsRepl));
				StrUni stuTags = AddReplacementTags(vstuTagsToInclude, sbstrTagsRepl);
				CheckHr(qtpb->SetStrPropValue(ktptTags, stuTags.Bstr()));
			}
			CheckHr(qtpb->GetTextProps(&qttp));
		}
		else
			qttp = qttpSel; // copy all props exactly from matched text.

		// Insert modified run into string builder.
		if (fEmptySearch && cchRepl == 0)
		{
			// We are just replacing an ws/ows/style/tags. The text remains unchanged.
			// ENHANCE (SharonC): Rework this when we get patterns properly implemented.
			SmartBstr sbstrText;
			CheckHr(qtssSel->get_Text(&sbstrText));
			CheckHr(qtsb->Replace(0, 0, sbstrText, qttp));
		}
		else
		{
			SmartBstr sbstrText;
			CheckHr(ptssRepl->get_RunText(irun, &sbstrText));
			CheckHr(qtsb->Replace(tri.ichMin, tri.ichMin, sbstrText, qttp));
		}
	}
	ITsStringPtr qtssRep;
	CheckHr(qtsb->GetString(&qtssRep));
	CheckHr(psel->ReplaceWithTsString(qtssRep));
}

/*----------------------------------------------------------------------------------------------
	Get a list of tags that are in the found string. These are the ones that are going to
	be replaced by the strings in the replacement.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::GetTagsToRemove(ITsString * ptssFindWhat, bool * pfUseTags,
	Vector<StrUni> & vstuTagsToRemove)
{
	int crun;
	CheckHr(ptssFindWhat->get_RunCount(&crun));
	for (int irun = 0; irun < crun; irun++)
	{
		SmartBstr sbstrTags;
		TsRunInfo tri;
		ITsTextPropsPtr qttp;
		CheckHr(ptssFindWhat->FetchRunInfo(irun, &tri, &qttp));
		CheckHr(qttp->GetStrPropValue(ktptTags, &sbstrTags));
		int cguid = BstrLen(sbstrTags) / kcchGuidRepLength;
		OLECHAR * pch = sbstrTags;
		for (int iguid = 0; iguid < cguid; iguid++)
		{
			StrUni stu(pch, kcchGuidRepLength);
			vstuTagsToRemove.Push(stu);
			pch += kcchGuidRepLength;
		}
	}
	if (vstuTagsToRemove.Size())
		*pfUseTags = true;
}

/*----------------------------------------------------------------------------------------------
	Return a list of all tags that are present in the first run of the given string, minus
	the ones that need to be removed (because they were specified in the find-what box).
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::GetTagsToInclude(ITsString * ptssFound, Vector<StrUni> & vstuTagsToRemove,
	Vector<StrUni> & vstuTagsToInclude)
{
	SmartBstr sbstrTags;
	TsRunInfo tri;
	ITsTextPropsPtr qttp;
	CheckHr(ptssFound->FetchRunInfo(0, &tri, &qttp));
	CheckHr(qttp->GetStrPropValue(ktptTags, &sbstrTags));
	int cguid = BstrLen(sbstrTags) / kcchGuidRepLength;
	OLECHAR * pch = sbstrTags;
	for (int iguid = 0; iguid < cguid; iguid++)
	{
		StrUni stu(pch, kcchGuidRepLength);
		int istu;
		for (istu = 0; istu < vstuTagsToRemove.Size(); istu++)
		{
			if (vstuTagsToRemove[istu] == stu)
				break;
		}
		if (istu >= vstuTagsToRemove.Size())
			// Not found in the remove-list: include it.
			vstuTagsToInclude.Push(stu);

		pch += kcchGuidRepLength;
	}
}

/*----------------------------------------------------------------------------------------------
	Return a merged list of all the original tags (minus the ones to remove) plus the
	replacement tags. It is assumed both lists are appropriately sorted. Note that the
	list must be sorted in REVERSE order.
----------------------------------------------------------------------------------------------*/
StrUni AfFindRepDlg::AddReplacementTags(Vector<StrUni> & vstuOrig, SmartBstr sbstrRepl)
{
	SmartBstr sbstrRet;
	int istuOrig = 0;
	int cguid = BstrLen(sbstrRepl) / kcchGuidRepLength;
	int iguidRepl = 0;
	OLECHAR * pchRepl = sbstrRepl;
	while (istuOrig < vstuOrig.Size() || iguidRepl < cguid)
	{
		if (iguidRepl >= cguid)
		{
			// Copy the rest of the original tags.
			for (; istuOrig < vstuOrig.Size(); istuOrig++)
				sbstrRet.Append(vstuOrig[istuOrig].Chars(), kcchGuidRepLength);
		}
		else if (istuOrig >= vstuOrig.Size())
		{
			// Copy the rest of the replacement tags.
			sbstrRet.Append(pchRepl, ((cguid - iguidRepl) * kcchGuidRepLength));
			iguidRepl = cguid;
		}
		else
		{
			// Copy the first.
			int nRes = CompareGuids((OLECHAR *)vstuOrig[istuOrig].Chars(), pchRepl);
			if (nRes >= 0)
			{
				sbstrRet.Append(vstuOrig[istuOrig].Chars(), kcchGuidRepLength);
				istuOrig++;
				if (nRes == 0)
				{
					pchRepl += kcchGuidRepLength;
					iguidRepl++;
				}
			}
			else
			{
				sbstrRet.Append(pchRepl, kcchGuidRepLength);
				pchRepl += kcchGuidRepLength;
				iguidRepl++;
			}
		}
	}
	StrUni stuRet(sbstrRet.Chars(), BstrLen(sbstrRet));
	return stuRet;
}

/*----------------------------------------------------------------------------------------------
	Implement the "Replace" button.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::OnFindReplace()
{
	IVwRootBoxPtr qrootb;
	AfVwRootSite * pvrs = RootSite();
	if (!pvrs)
		return;
	pvrs->get_RootBox(&qrootb);
	if (!qrootb)
		return; // Can't do it; weird.
	IVwSelectionPtr qvwsel;
	CheckHr(qrootb->get_Selection(&qvwsel));
	if (!qvwsel)
		return; // Or Assert false?
	GetCtls(); // Retrieve everything.
	if (IsReplacePossible())
	{
		// May not be, if the user first found something, thus enabling the button,
		// then switched to the data window and changed the selection, then switched
		// focus back and clicked the button. In this case, we interpret the button
		// as "Find Next", which means we skip replacing the current selection and
		// everything related to it.

		// Are we searching for an ws/ows and/or style?
		int cch;
		CheckHr(m_qtssFindWhat->get_Length(&cch));
		bool fEmptySearch = (cch == 0);
		ISilDataAccessPtr qsda;
		CheckHr(qrootb->get_DataAccess(&qsda));
		BeginUndoTask(qsda.Ptr(), kstidReplace);
		ITsStringPtr qtssReplaceWithFixed;
		StoreWsAndStyleInString(m_qteReplaceWith, m_qtssReplaceWith, &qtssReplaceWithFixed);
		DoReplacement(qvwsel, qtssReplaceWithFixed, m_fMatchOldWritingSystem, fEmptySearch);
		CheckHr(qsda->EndUndoTask());
		ChangeCancelToClose(m_hwnd);
	}
	OnFindNow(); // Try another find, to move to the next thing to replace.
}

void SendPropChanged(VecCi & vci, ISilDataAccess * psda)
{
	for (int i = 0; i < vci.Size(); i++)
	{
		VwChangeInfo & ci = vci[i];
		psda->PropChanged(NULL, kpctNotifyAll, ci.hvo, ci.tag, ci.ivIns, ci.cvIns, ci.cvDel);
	}
}

/*----------------------------------------------------------------------------------------------
	Implement the "Replace All" button.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::OnFindReplaceAll()
{
	bool fAlwaysSave = false;
	int cactRep = 0;
	int cactTotal = 0;
	ISilDataAccessPtr qsda;
	// This vector is used to keep track of needed PropChanged messages.
	// They must not be sent at once because, through NoteDependency, they could just possibly
	// cause the selections that are keeping track of our current position to become
	// invalid! For example, a cross-reference in DN has a NoteDependency on the title of
	// the record it refers to; if the replace changes the title, the whole destination
	// record will be replaced. If by some chance a record contains a cross-reference to
	// itself, the replacement will invalidate the current selection.
	VecCi vci;

	int cchRep;
	CheckHr(m_qtssReplaceWith->get_Length(&cchRep));
	// Are we searching for an ws/ows, style, and/or overlay tag?
	int cch;
	CheckHr(m_qtssFindWhat->get_Length(&cch));
	bool fEmptySearch = (cch == 0);
	if (fEmptySearch)
	{
		if (cchRep > 0)
		{
			StrApp str(kstidReplaceErr);
			StrApp staCaption(kstidReplace);
			::MessageBox(m_hwnd, str.Chars(), staCaption.Chars(), MB_OK | MB_ICONEXCLAMATION);
			return;
		}

		// Make sure they really want to replace an empty string.
		ConfirmEmptyReplaceDlgPtr qerd;
		qerd.Create();
		if (qerd->DoModal(m_hwnd) != kctidOk)
			return;
	}

	{
		// Block to contain the DisplayBusy object, so it will be deleted (and the
		// animation turned off) before we give the final message.

		// Change the buttons and animate the magnifying glass.
		DisplayBusy DB(this);
		m_fBusy = true;

		GetCtls();
		AfVwRootSite * pvrs = RootSite();
		if (!pvrs)
			return;
		// Make sure range selection shows up. If the old selection was an IP, it may have
		// been disabled altogether.
		pvrs->Activate(vssOutOfFocus);
		// Todo 1348 (JohnT): appropriate Combo box get text, and update the list.

		// Todo 1733 (JohnT): if m_fShowList is true, do something different...

		// Start searching at the beginning of the window, get first match if any.
		// Todo JohnT: make it the beginning of whatever we are searching
		if (!m_qtssFindWhat)
			return;
		IVwRootBoxPtr qrootb;
		pvrs->get_RootBox(&qrootb);
		if (!qrootb)
			return;
		CheckHr(m_qxpat->putref_Limit(NULL));

		// Set up mechanism to be able to abort search:
		m_qxserkl.CreateInstance(CLSID_VwSearchKiller);
		m_qxserkl->put_Window((int)m_hwnd);

		// Use nextMatch instead of Find to make sure end-of-search limit is set up -SJC.
		// JT: I don't understand what problem Sharon was trying to avoid.
		// But using NextMatch like this causes ReplaceAll to miss a match that is
		// selected at when ReplaceAll is clicked.
//		pvrs->NextMatch(true, false, false, true, m_hwnd, m_qxserkl);
		CheckHr(m_qxpat->Find(qrootb, true, m_qxserkl));

		ComBool fAborted;
		CheckHr(m_qxserkl->get_AbortRequest(&fAborted));
		if (fAborted == ComBool(true))
			return;
		ITsStringPtr qtssReplaceWithFixed;
		StoreWsAndStyleInString(m_qteReplaceWith, m_qtssReplaceWith, &qtssReplaceWithFixed);
		bool fStoppedByLogFileFull = false;
		// While we have a match, do a replacement.
		for ( ; ; )
		{
			CheckHr(m_qxserkl->get_AbortRequest(&fAborted));
			if (fAborted == ComBool(true))
				break;

			// See if we got a match, and stop if we didn't
			ComBool fFound;
			CheckHr(m_qxpat->get_Found(&fFound));
			if (!fFound)
				break;
			// Retrieve the selection and do the replacement.
			IVwSelectionPtr qvwsel;
			CheckHr(m_qxpat->GetSelection(false, &qvwsel));
			Assert(qvwsel);
			ComBool fCanEdit;
			CheckHr(qvwsel->get_CanFormatChar(&fCanEdit));
			if (fCanEdit)
			{
				// Check we aren't replacing a single object character, which might not
				// be an exact match.
				// Review EdK: should we confirm the visible form and if so replace?
				ITsStringPtr qtssSel;
				SmartBstr sbstr = L" ";
				ComBool fGotItAll;
				CheckHr(qvwsel->GetFirstParaString(&qtssSel, sbstr, &fGotItAll));
				int cchSel;
				CheckHr(qtssSel->get_Length(&cchSel));
				if (cchSel == 1)
				{
					OLECHAR ch;
					CheckHr(qtssSel->FetchChars(0, 1, &ch));
					if (ch == 0xfffc) // object replacement character
						fCanEdit = false; // skip this one.
				}
			}
			if (!fCanEdit)
			{
				// Install the selection we just found.
				CheckHr(m_qxpat->Install());
				// Search again;
				CheckHr(m_qxpat->FindFrom(qvwsel, true, m_qxserkl));
				continue;
			}
			AfVwSelInfo avsi;
			avsi.Load(qrootb, qvwsel);
			if (cactRep && (cactRep % 50) == 0)
			{
				RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
				if (prmw)
				{
					if (prmw->ProcessExcessiveChgs(prmw->kSaveWarnReplace, fAlwaysSave,
							&fStoppedByLogFileFull, qsda))
					{
						fAlwaysSave = true;
						cactRep = 0;
					}
				}
			}
			if (!cactRep)
			{
				CheckHr(qrootb->get_DataAccess(&qsda));
				// Wind up any Undo activity related to typing.
				ComBool fOk;
				VwChangeInfo ci;
				CheckHr(qvwsel->CompleteEdits(&ci, &fOk));
				if (ci.hvo != 0)
					vci.Push(ci);
				if (!fOk)
				{
					SendPropChanged(vci, qsda);
					return;
				}
				BeginUndoTask(qsda.Ptr(), kstidReplaceAll);
				ChangeCancelToClose(m_hwnd);
			}
			cactRep++;
			cactTotal++;
			DoReplacement(qvwsel, qtssReplaceWithFixed, m_fMatchOldWritingSystem, fEmptySearch);
			// Commit that change!
			ComBool fOk;
			VwChangeInfo ci;
			CheckHr(qvwsel->CompleteEdits(&ci, &fOk));
			if (ci.hvo != 0)
				vci.Push(ci);
			if (!fOk)
			{
				SendPropChanged(vci, qsda);
				return;
			}
			// Make a new selection just after the replacement.
			avsi.m_ichAnchor += cchRep;
			avsi.m_ichEnd = avsi.m_ichAnchor;
			avsi.Set(qrootb, false, &qvwsel);

			if(fStoppedByLogFileFull)
				break;

// Continue searching from there.
//			pvrs->NextMatch(true, false,  false, true, m_hwnd, m_qxserkl);
			CheckHr(m_qxpat->FindFrom(qvwsel, true, m_qxserkl));
		}
	}
	// Now it is safe to broadcast all the changes.
	SendPropChanged(vci, qsda);

	// turn off animation

	StrApp staCaption(kstidReplace);
	if (cactTotal)
	{
		CheckHr(qsda->EndUndoTask());
		StrApp staFmt(m_stidReplaceN);
		StrApp staMsg;
		staMsg.Format(staFmt.Chars(), cactTotal);
		::MessageBox(Hwnd(), staMsg.Chars(), staCaption.Chars(), MB_OK | MB_ICONINFORMATION);
	}
	else
	{
		StrApp staMsg(m_stidNoMatches);
		::MessageBox(Hwnd(), staMsg.Chars(), staCaption.Chars(), MB_OK | MB_ICONINFORMATION);
	}
}

/*----------------------------------------------------------------------------------------------
	Start a task for undoing, with the label indicated by the string resource ID.
----------------------------------------------------------------------------------------------*/
void AfFindRepDlg::BeginUndoTask(ISilDataAccess * psda, int stid)
{
	StrUni stuUndo, stuRedo;
	StrUtil::MakeUndoRedoLabels(stid, &stuUndo, &stuRedo);
	CheckHr(psda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
}


/*----------------------------------------------------------------------------------------------
	Return a pointer to the main window (if we can find it).
	If it can't be found, return NULL.
----------------------------------------------------------------------------------------------*/
AfMainWnd * AfFindRepDlg::MainWindow()
{
	// See if there is an active window (the front window belonging to this thread).
	HWND hwndActive = ::GetActiveWindow();
	// If there was no active window, it may be because we're in a separate thread for a search:
	if (!hwndActive)
		hwndActive = m_hwnd;
	// Make sure it's one of ours. If not (most likely it's the Find/Replace dialog itself),
	// find one that is.
	AfMainWnd * pafw;
	for ( ; ; )
	{
		if (!hwndActive)
			return NULL;
		pafw = dynamic_cast<AfMainWnd *>(AfWnd::GetAfWnd(hwndActive));
		if (pafw)
			return pafw;
		hwndActive = GetNextWindow(hwndActive, GW_HWNDNEXT);
	}
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Figure which root site to search. If there isn't one return NULL.
----------------------------------------------------------------------------------------------*/
AfVwRootSite * AfFindRepDlg::RootSite()
{
	if (!m_qvrsLast.Ptr())
	{
		// See if there is an active window (the front window belonging to this thread).
		AfMainWnd * pafw = MainWindow();
		// If so, see if it has an active root site. Return whatever this retrieves.
		if (pafw)
			pafw->GetActiveViewWindow(&m_qvrsLast, NULL, false);
	}
	return m_qvrsLast.Ptr();
}

/*----------------------------------------------------------------------------------------------
	Figure whether the "Replace" button should be enabled, and make it so.
	It is enabled when
	(0) It is visible
	(1) we have a replace with string;
	(2) we have a match string;
	(3) there is a current selection, not an IP;
	(4) the selected text is a match.
	(5) editing is allowed at the selection.
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::IsReplacePossible()
{
	bool fEnable = false;
	// caller must do this
	// GetCtls();
	IVwRootBoxPtr qrootb;
	AfVwRootSite * pvrs = RootSite();
	if (pvrs)
		pvrs->get_RootBox(&qrootb);
	if (qrootb)
	{
		IVwSelectionPtr qvwsel;
		CheckHr(qrootb->get_Selection(&qvwsel));
		if (qvwsel)
		{
			ComBool fMatch;
			CheckHr(m_qxpat->MatchWhole(qvwsel, &fMatch));
			fEnable = (bool)fMatch;
			if (fEnable)
			{
				// now check editability
				ComBool fCanEdit;
				CheckHr(qvwsel->get_CanFormatChar(&fCanEdit));
				fEnable = (bool) fCanEdit;
			}
		}
	}
	return fEnable;
}
void AfFindRepDlg::EnableReplaceButton()
{
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFindReplace), IsReplacePossible());
}

void AfFindRepDlg::EditBoxChanged(FindTssEdit * pfte)
{
	EnableDynamicControls();
}

/*----------------------------------------------------------------------------------------------
	Handle getting focus by passing it to the appropriate edit box.
----------------------------------------------------------------------------------------------*/
bool AfFindRepDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_SETFOCUS:
		if (m_qteLastFocus)
		{
			::SetFocus(m_qteLastFocus->Hwnd());
			return true;
		}
		break;
	case WM_ACTIVATE:
		if (LOWORD(wp) == WA_INACTIVE)
		{
			// Deactivating. Remove this from list of command handlers.
			AfApp::Papp()->RemoveCmdHandler(this, 1);
			// Also remove our special accelerator table.
			AfApp::Papp()->RemoveAccelTable(m_atid);
			// Ideally, we should remove the reference to the last root box, but we keep it
			// around so that bringing up the debugger doesn't confuse things.
			//m_qvrsLast.Clear();
		}
		else
		{
			// Activating. Keep track of the last root site for when we actually do the search.
			// This needs to be done before the focus is set to an edit box, because setting
			// the focus will update the root site.
			AfMainWnd * pafw = MainWindow();
			AssertPtrN(pafw);
			AfVwRootSitePtr qvrs;
			if (pafw)
				pafw->GetActiveViewWindow(&qvrs, NULL, false);
			if (!dynamic_cast<FindTssEdit *>(qvrs.Ptr()))
				m_qvrsLast = qvrs;
			// else: leave as is. This is a safety net for when reactivating the Find
			// dialog after bringing up the debugger.

			// If we previously had an edit box in focus, restore it.
			if (m_qteLastFocus)
				::SetFocus(m_qteLastFocus->Hwnd());
			// Make ourself an active command handler.
			AfApp::Papp()->AddCmdHandler(this, 1, kgrfcmmAll);
			// We load the basic accelerator table so that these commands can be directed to this
			// window. This allows the embedded TssEdit controls to see the commands. Otherwise, if
			// they are translated by the main window, the main window is the 'target', and the
			// command handlers on AfVwRootSite don't work, because the root site is not a child
			// window of the main one.
			// Note that we don't just create it once and use SetAccelHwnd, because the active
			// main window can change while the find dialog is open, and we need to install
			// into the active menu manager.
			if (AfApp::Papp())
				m_atid = AfApp::Papp()->LoadAccelTable(kridAccelBasic, 0, m_hwnd);
		}
		break;
	case WM_DESTROY:
		SaveWindowPosition();
		break;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}



/*----------------------------------------------------------------------------------------------
	Disable all buttons, and change the Close button to a Stop button. Start the moving
	magnifying glass animation.
	@param frdlg Find and replace dialog.
----------------------------------------------------------------------------------------------*/
AfFindRepDlg::DisplayBusy::DisplayBusy(AfFindRepDlg * frdlg)
{
	AssertPtr(frdlg);
	m_frdlg = frdlg;

	HWND hwnd = frdlg->Hwnd();
	Assert(hwnd);

	frdlg->m_fBusy = true;

	// Disable all buttons, and make the Close button change to an (enabled) Stop button:
	::EnableWindow(::GetDlgItem(hwnd, kctidFindReplace), false);
	::EnableWindow(::GetDlgItem(hwnd, kctidFindFindNow), false);
	::EnableWindow(::GetDlgItem(hwnd, kctidFindReplaceAll), false);
	::EnableWindow(::GetDlgItem(hwnd, kctidFindClose), false);
	::ShowWindow(::GetDlgItem(hwnd, kctidFindClose), SW_HIDE);
	::ShowWindow(::GetDlgItem(hwnd, kctidFindStop), SW_SHOW);
	::EnableWindow(::GetDlgItem(hwnd, kctidFindStop), true);

	HWND hwndAnim = ::GetDlgItem(hwnd, kctidFindAnimation);

	// Open the AVI clip, and show the animation control.
	Animate_Open(hwndAnim, MAKEINTRESOURCE(kridFindAnimation));
	::ShowWindow(hwndAnim, SW_SHOW);
	// Run the animation from the start (zero) to the end (first -1) and repeat indefinitely
	// (last -1).
	Animate_Play(hwndAnim, 0, -1, -1);
}

/*----------------------------------------------------------------------------------------------
	Enable all buttons, and change the Stop button to a Close button. Stop the moving
	magnifying glass animation.
----------------------------------------------------------------------------------------------*/
AfFindRepDlg::DisplayBusy::~DisplayBusy()
{
	AssertPtr(m_frdlg);
	if (m_frdlg->m_fBusy)
	{
		HWND hwnd = m_frdlg->Hwnd();
		Assert(hwnd);

		// Enable all buttons, and make the Stop button change to an Close button:
		::EnableWindow(::GetDlgItem(hwnd, kctidFindReplace), true);
		::EnableWindow(::GetDlgItem(hwnd, kctidFindFindNow), true);
		::EnableWindow(::GetDlgItem(hwnd, kctidFindReplaceAll), true);
		::EnableWindow(::GetDlgItem(hwnd, kctidFindClose), true);
		::ShowWindow(::GetDlgItem(hwnd, kctidFindClose), SW_SHOW);
		::ShowWindow(::GetDlgItem(hwnd, kctidFindStop), SW_HIDE);
		::EnableWindow(::GetDlgItem(hwnd, kctidFindStop), false);

		HWND hwndAnim = ::GetDlgItem(hwnd, kctidFindAnimation);
		Animate_Stop(hwndAnim);
		::ShowWindow(hwndAnim, SW_HIDE);
		m_frdlg->m_fBusy = false;
	}
}

//:>********************************************************************************************
//:>	ConfirmEmptyReplaceDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
ConfirmEmptyReplaceDlg::ConfirmEmptyReplaceDlg(void)
{
	m_rid = kridEmptyReplaceDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/Edit/Find_and_Replace/Replace.htm");
}

/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool ConfirmEmptyReplaceDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

//	return SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet);
//	SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet);
	return false;
}

/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool ConfirmEmptyReplaceDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	StrApp strFmt;
	StrApp strText;
	HWND hwnd;

	HICON hicon = ::LoadIcon(NULL, IDI_WARNING);
	if (hicon)
	{
		hwnd = ::GetDlgItem(m_hwnd, kridConfirmReplaceIcon);
		::SendMessage(hwnd, STM_SETICON, (WPARAM)hicon, (LPARAM)0);
	}

	// ENHANCE (SharonC): Maybe we want to give a special message depending on whether
	// they are changing the old writing system, style, or overlay tag.

	::SetFocus(::GetDlgItem(m_hwnd, kctidCancel));

///////////////	SuperClass::OnInitDlg(hwndCtrl, lp);
	return false;
}


bool ConfirmEmptyReplaceDlg::OnActivate(bool fActivating, LPARAM lp)
{
	AfDialog * pdlg;

	pdlg = dynamic_cast<AfDialog *>(AfWnd::GetAfWnd(::GetParent(m_hwnd)));
	if (pdlg)
		::SendMessage(pdlg->Hwnd(), DM_SETDEFID, ::GetDlgCtrlID(m_hwnd), 0);

	return false;
}

// Explicit instantiation
#include "vector_i.cpp"
template Vector<VwChangeInfo>; // VecCi;
