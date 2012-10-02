/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFieldEditor.cpp
Responsibility: Ken Zook
Last reviewed: never

Description:
	This implements AfDeFieldEditor.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDeFieldEditor::AfDeFieldEditor()
{
	m_dypFontHeight = 0;
	m_hbrBkg = NULL;
	m_hfont = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeFieldEditor::~AfDeFieldEditor()
{
	// Clear the brush and font objects, if they exits.
	if (m_hbrBkg)
	{
		AfGdi::DeleteObjectBrush(m_hbrBkg);
		m_hbrBkg = NULL;
	}
	if (m_hfont)
	{
		AfGdi::DeleteObjectFont(m_hfont);
		m_hfont = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	If this record is different than the previous record, update the modification date if
	if something changed.
	Note: This should always be called from overloaded methods.
----------------------------------------------------------------------------------------------*/
bool AfDeFieldEditor::BeginEdit(HWND hwnd, Rect & rc, int dxpCursor, bool fTopCursor)
{
	m_qadsc->BeginEdit(this);
	SaveFullCursorInfo(); // Need this to update header and caption bar.
	return true;
}


/*----------------------------------------------------------------------------------------------
	Close the field editor.
----------------------------------------------------------------------------------------------*/
void AfDeFieldEditor::EndEdit(bool fForce)
{
	m_qadsc->SetLastObj(GetOwner()); // Save the current record id.
}

/*----------------------------------------------------------------------------------------------
	Saves all information to restore the cursor to a given location in a field. It
	doesn't support selection ranges. This information is stored in RecMainWnd m_vhvoPath,
	m_vflidPath, and m_ichCur.
----------------------------------------------------------------------------------------------*/
void AfDeFieldEditor::SaveFullCursorInfo()
{
	// Store the current record/subrecord and field info.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (!prmw)
		return;
	HVO hvoRoot = prmw->GetRootObj();
	Vector<int> & vflid = prmw->GetFlidPath();
	Vector<HVO> & vhvo = prmw->GetHvoPath();
	vhvo.Clear();
	vflid.Clear();
	// Save cursor specifics within a field.
	SaveCursorInfo();
	// Now save information leading up to the field.
	CustViewDaPtr qcvd;
	prmw->GetLpInfo()->GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	HVO hvo = GetOwner();
	int flid = GetOwnerFlid();
	while (hvo && hvo != hvoRoot)
	{
		vhvo.Insert(0, hvo);
		vflid.Insert(0, flid);
		HVO hvoT;
		CheckHr(qcvd->get_ObjOwner(hvo, &hvoT));
		CheckHr(qcvd->get_ObjOwnFlid(hvo, &flid));
		hvo = hvoT;
	}
	if (!hvo)
	{
		// This is to cover flukes in M3ModelEditor.
		vhvo.Clear();
		vflid.Clear();
	}
	// Also update the status bar and caption bar.
	prmw->UpdateStatusBar();
	prmw->UpdateCaptionBar();
}

/*----------------------------------------------------------------------------------------------
	Clear pointers to other windows so its destructor will be called; also to all smart
	pointer objects to prevent Release being called after DLL unloaded at end of program.
----------------------------------------------------------------------------------------------*/
void AfDeFieldEditor::OnReleasePtr()
{
	m_qadsc.Clear();
	m_qfsp.Clear();
	m_qtssLabel.Clear();
	m_qtssHelp.Clear();
}


/*----------------------------------------------------------------------------------------------
	Initialize the common portion of field editors.
	@param hvoObj Id of object we are editing (the object that has m_flid).
	@param flid Id of the field we are editing.
	@param nIndent Level of nesting in the tree. 0 is top.
	@param ptssLabel Pointer to the label to show in the tree for this field.
	@param ptssHelp Pointer to the "What's this" help string associated with this field.
	@param padsc Pointer to the owning AfDeSplitChild window class.
	@param pfsp The FldSpec that defines this field (NULL is acceptable, but not recommended).
----------------------------------------------------------------------------------------------*/
void AfDeFieldEditor::Initialize(int hvoObj, int flid, int nIndent, ITsString * ptssLabel,
	ITsString * ptssHelp, AfDeSplitChild * padsc, FldSpec * pfsp)
{
	AssertPtrN(ptssLabel);
	AssertPtrN(ptssHelp);
	AssertPtrN(pfsp);
	AssertPtr(padsc);
	Assert(hvoObj);
	Assert(flid);

	// Store member variables.
	m_hvoObj = hvoObj;
	m_flid = flid;
	m_nIndent = nIndent;
	m_qtssLabel = ptssLabel;
	m_qtssHelp = ptssHelp;
	m_qadsc = padsc;
	// Make a dummy FldSpec if one wasn't supplied.
	if (pfsp)
		m_qfsp = pfsp;
	else
		m_qfsp.Create();

	// Get writing system.
	AfLpInfoPtr qlpi = padsc->GetLpInfo();
	Assert(qlpi);
	m_wsMagic = (m_qfsp->m_ws != 0) ? m_qfsp->m_ws : kwsAnal;
	m_ws = qlpi->ActualWs(m_wsMagic);

	MakeCharProps(m_ws);
}

/*----------------------------------------------------------------------------------------------
	Get the height of the font + 2 (1 pixel above and below) in pixels.
	@return height of font in pixels + 2 pixels for padding above and below. For
	one line fields this is like GetHeight except GetHeight returns one more pixel
	for the separator line.
----------------------------------------------------------------------------------------------*/
int AfDeFieldEditor::GetFontHeight()
{
	// If the editor's font wasn't created then call it for the sole purpose of setting
	// m_dypFontHeight. If that's the case, delete the font just after calling the function
	// to create it, since it must not otherwise be needed now.
	if (!m_hfont && m_dypFontHeight == 0)
	{
		// Calling CreateFont will set m_dypHeight. Since m_dypHeight may have already had
		// a different value computed for it, save it in order to restore it after calling
		// CreateFont.
		int dypSaveHeight = m_dypHeight;
		CreateFont();
		m_dypHeight = dypSaveHeight;

		if (m_hfont)
		{
			AfGdi::DeleteObjectFont(m_hfont);
			m_hfont = NULL;
		}
	}

	return m_dypFontHeight + 2;  // Add 2 for 1 pixel above and below.
}

/*----------------------------------------------------------------------------------------------
	Set up a Chrp consistent with the current style sheet.
----------------------------------------------------------------------------------------------*/
void AfDeFieldEditor::MakeCharProps(int ws)
{
	AfLpInfoPtr qlpi = GetLpInfo();
	// Make a text property with the named style.
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->SetStrPropValue(kspNamedStyle, m_qfsp->m_stuSty.Bstr()));
	CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, ws));
	ITsTextPropsPtr qttp;
	CheckHr(qtpb->GetTextProps(&qttp));

	// Fill out the LgCharRenderProps.
	ILgWritingSystemFactoryPtr qwsf;
	IVwPropertyStorePtr qvps;
	qvps.CreateInstance(CLSID_VwPropertyStore);
	if (qlpi)
	{
		qvps->putref_Stylesheet(qlpi->GetAfStylesheet());
		AssertPtr(qlpi->GetDbInfo());
		qlpi->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	}
	else
	{
		qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get default registry-based factory.
	}
	AssertPtr(qwsf);
	qvps->putref_WritingSystemFactory(qwsf);
	CheckHr(qvps->get_ChrpFor(qttp, &m_chrp));

	IWritingSystemPtr qLgWritingSystem;
	CheckHr(qwsf->get_EngineOrNull(m_chrp.ws, &qLgWritingSystem));
	AssertPtr(qLgWritingSystem);
	CheckHr(qLgWritingSystem->InterpretChrp(&m_chrp));

	// For our purposes here, we don't want transparent backgrounds.
	if (m_chrp.clrBack == kclrTransparent)
		m_chrp.clrBack = ::GetSysColor(COLOR_WINDOW);

	// Make a brush for the background. If we already have one delete it first.
	if (m_hbrBkg)
	{
		AfGdi::DeleteObjectBrush(m_hbrBkg);
		m_hbrBkg = NULL;
	}
	m_hbrBkg = AfGdi::CreateSolidBrush(m_chrp.clrBack);
}


/*----------------------------------------------------------------------------------------------
	Create the display font and set the edit height based on character property settings.
----------------------------------------------------------------------------------------------*/
void AfDeFieldEditor::CreateFont()
{
	// If we already have one delete it first.
	if (m_hfont)
	{
		AfGdi::DeleteObjectFont(m_hfont);
		m_hfont = NULL;
	}
	HWND hwnd = m_qadsc->Hwnd();
	HDC hdc = ::GetDC(hwnd);

	LOGFONT lf;
	lf.lfItalic = m_chrp.ttvItalic == kttvOff ? false : true;
	lf.lfWeight = m_chrp.ttvBold == kttvOff ? 400 : 700;
	// The minus causes this to be the font height (roughly, from top of ascenders
	// to bottom of descenders). A positive number indicates we want a font with
	// this distance as the total line spacing, which makes them too small.
	lf.lfHeight = -MulDiv(m_chrp.dympHeight, ::GetDeviceCaps(hdc, LOGPIXELSY), kdzmpInch);
	lf.lfUnderline = false;
	lf.lfWidth = 0;	// Default width, based on height.
	lf.lfEscapement = 0; // No rotation of text.
	lf.lfOrientation = 0; // No rotation of character baselines.
	lf.lfStrikeOut = 0; // Not strike-out.
	lf.lfCharSet = DEFAULT_CHARSET; // Let name determine it; WS should specify valid.
	lf.lfOutPrecision = OUT_TT_ONLY_PRECIS; // Only work with TrueType fonts.
	lf.lfClipPrecision = CLIP_DEFAULT_PRECIS; // ??
	lf.lfQuality = DRAFT_QUALITY; // TODO JohnT: Should we control somehow?
	lf.lfPitchAndFamily = 0; // Must be zero for EnumFontFamiliesEx.
#ifdef UNICODE
	// ENHANCE JohnT: Test this path if ever needed.
	wcscpy_s(lf.lfFaceName, m_chrp.szFaceName);
#else // Not Unicode, LOGFONT has 8-bit chars.
	WideCharToMultiByte(
		CP_ACP,	0, // Dumb; we don't expect non-ascii chars.
		m_chrp.szFaceName, // String to convert.
		-1, // Null-terminated.
		lf.lfFaceName, 32,
		NULL, NULL); // Default handling of unconvertibles.
#endif // Not Unicode.

	m_hfont = AfGdi::CreateFontIndirect(&lf);

	// Set the field height.
	TEXTMETRIC tm;
	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, m_hfont);
	::GetTextMetrics(hdc, &tm);
	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	int iSuccess;
	iSuccess = ::ReleaseDC(hwnd, hdc);
	Assert(iSuccess);
	Assert(hwnd == m_qadsc->Hwnd());

	m_dypHeight = tm.tmHeight + 3; // 1 pixel top/bottom margin + divider line.
	m_dypFontHeight = tm.tmHeight;
}

/*----------------------------------------------------------------------------------------------
	Process a stylesheet change. Embedded root boxes have already been notified.

	Note: if m_hfont exists it is updated, along with the field's height. Subclasses which
	user m_hfont but do not use the field height it calculates should override.
----------------------------------------------------------------------------------------------*/
void AfDeFieldEditor::OnStylesheetChange()
{
	MakeCharProps(m_ws);
	if (m_hfont) // If we have a font update it.
		CreateFont();
}


/*----------------------------------------------------------------------------------------------
	Call BeginUndoTask on you DeWnd's data access object with names like
	"Undo changes to <label>".
----------------------------------------------------------------------------------------------*/
void AfDeFieldEditor::BeginChangesToLabel()
{
	// Make up names for the overall Undo/Redo, something like "Undo changes to <label>"
	SmartBstr sbstrLabel;
	CheckHr(m_qtssLabel->get_Text(&sbstrLabel));
	StrUni stuUndoFmt;
	StrUni stuRedoFmt;
	StrUtil::MakeUndoRedoLabels(kstidUndoChangesTo, &stuUndoFmt, &stuRedoFmt);
	StrUni stuUndo;
	StrUni stuRedo;
	stuUndo.Format(stuUndoFmt.Chars(), sbstrLabel.Chars());
	stuRedo.Format(stuRedoFmt.Chars(), sbstrLabel.Chars());

	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	CheckHr(qcvd->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
}


/*----------------------------------------------------------------------------------------------
	Create a new possibility item (in the Poss List cache and database) and return the HVO.
	@param hvoPssl The id for the list in which to add the new item.
	@param psz Pointer to the name/abbr string. pnt and fHier determines whether this string
		is a simple	name, abbreviation, combination, and whether it includes tree hierarchy.
	@param pnt Enumeration indicating name, abbreviation, or "abbr - name"
	@param fHier True if the string includes hierarchy (name:name...). False if not.
	@return HVO of the newly created item; 0 if there was any problem.
----------------------------------------------------------------------------------------------*/
HVO AfDeFieldEditor::CreatePss(HVO hvoPssl, const OLECHAR * psz, PossNameType pnt, bool fHier)
{
	StrUni stu(psz);
	Vector<StrUni> vstu;
	if (fHier)
	{
		// replace each "::" with ":"
		// (probably not needed; but just in case user enters extra ":"s)
		int ich = stu.FindStr(L"::");
		while (ich >= 0)
		{
			stu.Replace(ich, ich + 2, L":");
			ich = stu.FindStr(L"::");
		}

		// remove leading ":" (in case the user wrongly enters leading ":"s)
		// :AA:BB:CC
		ich = stu.FindStr(L":");
		while (ich == 0)
		{
			stu.Assign(stu.Right(stu.Length() - 1));
			ich = stu.FindStr(L":");
		}

		// example:  "AA:BB:CC"
		ich = stu.FindStr(L":"); //1st pass(=2)
		StrUni stuTmp;
		while (ich > 0)
		{
			stuTmp.Assign(stu.Chars(), ich);
			// push "AA"; then push "AA:BB"
			vstu.Push(stuTmp);
			ich = stu.FindStr((wchar *)L":", ich + 1);
		}
	}

	// push "AA:BB:CC"
	vstu.Push(stu);

	PossListInfoPtr qpli;
	GetLpInfo()->LoadPossList(hvoPssl, m_wsMagic, &qpli);
	AssertPtr(qpli);

	StrUni stuName;
	StrUni stuAbbr;
	HVO hvoNew;
	int ipss = -1;
	Locale loc = GetLpInfo()->GetLocale(m_ws);
	for (int ist = 0; ist < vstu.Size(); ist++)
	{
		// Trim leading and trailing space characters.
		StrUtil::TrimWhiteSpace(vstu[ist].Chars(), vstu[ist]);

		StrUni stuString;
		PossItemInfo* ppii;
		ComBool fExactMatch;
		if (fHier)
		{
			ppii = qpli->FindPssHier(vstu[ist].Chars(), loc, pnt, fExactMatch);
			if (!fExactMatch)
			{
				// need to get the current part (name/abbr) of the hierarchy
				// The left-most part (when ist == 0) needs no parsing.
				if (ist > 0)
				{
					stu.Assign(vstu[ist].Chars());
					int ichLeft = stu.FindStr(L":");
					int ich = ichLeft;
					for (int istTemp = 0; istTemp < ist; istTemp++)
					{
						ich = stu.FindStr((wchar *)L":", ich + 1);
						if (ich > 0) ichLeft = ich;
					}
					ichLeft++; // move it past the ":"
					int ichRight = stu.FindStr((wchar *)L":", ichLeft);
					if (ichRight < 0)
						ichRight = stu.Length();
					int iLength = ichRight - ichLeft;
					stuString = stu.Mid(ichLeft, iLength);
				}
				else
				{
					stuString = vstu[ist].Chars();
				}
				ppii = NULL;
			}
			else
			{
				stuString = vstu[ist].Chars();
			}
		}
		else
		{
			ppii = qpli->FindPss(vstu[ist].Chars(), loc, pnt, NULL, true);
		}
		if (ppii)
		{
			ipss = qpli->GetIndexFromId(ppii->GetPssId());
			hvoNew = ppii->GetPssId(); // Return the id of the item we found.
			// TODO TimP:  what about when duplicates are allowed?
			continue;
		}

		// At this point, need to add a new item.
		switch (pnt)
		{
		case kpntName:
		case kpntAbbreviation:
			{
				if (fHier)
					stuName = stuString;
				else
					stuName.Assign(vstu[ist].Chars());
				// replace each " - " with "-"
				int ich = stuName.FindStr(L" - ");
				while (ich > 0)
				{
					stuName.Replace(ich, ich + 3, "-");
					ich = stuName.FindStr(L" - ");
				}
				stuAbbr = stuName;
			}
			break;
		case kpntNameAndAbbrev:
			{
				if (fHier)
					stu = stuString;
				else
					stu.Assign(vstu[ist].Chars());
				int ich = stu.FindStr(L" - ");
				if (ich > 0)
				{
					stuAbbr = stu.Left(ich);
					stuName = stu.Right(stu.Length() - ich - 3);
					// replace each remaining " - " with "-"
					int ich = stuName.FindStr(L" - ");
					while (ich > 0)
					{
						stuName.Replace(ich, ich + 3, "-");
						ich = stuName.FindStr(L" - ");
					}
				}
				else
				{
					if (fHier)
					{
						stuAbbr = stuString;
						stuName = stuString;
					}
					else
					{
						stuAbbr.Assign(vstu[ist].Chars());
						stuName.Assign(vstu[ist].Chars());
					}
				}
			}
			break;
		}

		int iInsert;
		PossItemLocation nPos;
		// When sorting is working, this needs to go in the correct location.
		if (fHier)
		{
			if (ist)
			{
				// It is a sub item
				iInsert = ipss;
				nPos = kpilUnder;
			}
			else
			{
				// It goes on the top level
				iInsert = qpli->GetCount() - 1;
				nPos = kpilTop;
			}
		}
		else
		{
			iInsert = qpli->GetCount() - 1;
			nPos = kpilTop;
		}

		if (!qpli->InsertPss(iInsert, stuAbbr.Chars(), stuName.Chars(), nPos, &ipss))
		{
			Assert(false);
			return 0; // Something went wrong.
		}
		// Update all interested parties of change.
		ppii = qpli->GetPssFromIndex(ipss);
		AssertPtr(ppii);
		hvoNew = ppii->GetPssId();
		SyncInfo sync(ksyncAddPss, hvoPssl, hvoNew);
		// We could try to get by without syncing the current application since this reloads
		// the list unnecessarily. The only place we could get into trouble is using this to add
		// an item in a ListEditor which caused the current list to be updated. But, since this
		// could happen in the People list editing researcher, we'd better take the hit for
		// syncing the current application.
		GetLpInfo()->StoreAndSync(sync);
	}
	Assert(ipss != -1); // in case nothing was pushed into the vector.
	return hvoNew;
}


/*----------------------------------------------------------------------------------------------
	Get the main window from m_qadsc, since this object can't get it directly.

	@return Pointer to the main window.
----------------------------------------------------------------------------------------------*/
AfMainWnd * AfDeFieldEditor::MainWindow()
{
	AssertPtr(m_qadsc);
	return m_qadsc->MainWindow();
}


/*------------------------------------------------------------------------------------------
	Get the data access cache for this editor.

	@param ppcvd Address of a pointer for returning the data access cache.
------------------------------------------------------------------------------------------*/
void AfDeFieldEditor::GetDataAccess(CustViewDa ** ppcvd)
{
	GetLpInfo()->GetDataAccess(ppcvd);
}


/*------------------------------------------------------------------------------------------
	Get the language project information for this editor.

	@return Pointer to the language project information object.
------------------------------------------------------------------------------------------*/
AfLpInfo * AfDeFieldEditor::GetLpInfo()
{
	AssertPtr(m_qadsc);
	return m_qadsc->GetLpInfo();
}
