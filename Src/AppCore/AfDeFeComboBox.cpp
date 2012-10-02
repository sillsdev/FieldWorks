/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeComboBox.cpp
Responsibility: Ken Zook
Last reviewed: never

Description:
	This is a data entry field editor for combo-box fields.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

const int kwidEditChild = 1; // An identifier for an edit box.
const int kdxpButtonMargin = 18;
const int kdxpButtonWid = 16;
const int kdxpButtonHeight = 15;


BEGIN_CMD_MAP(AfDeFeComboBox::DecbEdit)
	ON_CID_CHILD(kcidEditCut, &AfDeFeComboBox::DecbEdit::CmdEdit,
		&AfDeFeComboBox::DecbEdit::CmsEditUpdate)
	ON_CID_CHILD(kcidEditCopy, &AfDeFeComboBox::DecbEdit::CmdEdit,
		&AfDeFeComboBox::DecbEdit::CmsEditUpdate)
	ON_CID_CHILD(kcidEditPaste, &AfDeFeComboBox::DecbEdit::CmdEdit,
		&AfDeFeComboBox::DecbEdit::CmsEditUpdate)
//	ON_CID_CHILD(kcidEditDel, &AfDeFeComboBox::DecbEdit::CmdEdit,
//		&AfDeFeComboBox::DecbEdit::CmsEditUpdate)
	ON_CID_CHILD(kcidEditSelAll, &AfDeFeComboBox::DecbEdit::CmdEdit,
		&AfDeFeComboBox::DecbEdit::CmsEditUpdate)

	// Since we want to disable formatting commands, we need to have our own command map in
	// order to override and disable the buttons/comboboxes.
	ON_CID_CHILD(kcidFmtFnt, &AfVwWnd::CmdFmtFnt, &AfDeFeComboBox::DecbEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmtStyles, &AfVwWnd::CmdFmtStyles, &AfDeFeComboBox::DecbEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbStyle, &AfVwWnd::CmdCharFmt, &AfDeFeComboBox::DecbEdit::CmsCharFmt)
	ON_CID_CHILD(kcidApplyNormalStyle, &AfVwWnd::CmdApplyNormalStyle, NULL)
	ON_CID_CHILD(kcidFmttbWrtgSys, &AfVwWnd::CmdCharFmt, &AfDeFeComboBox::DecbEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFnt, &AfVwWnd::CmdCharFmt, &AfDeFeComboBox::DecbEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbFntSize, &AfVwWnd::CmdCharFmt, &AfDeFeComboBox::DecbEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbBold, &AfVwWnd::CmdCharFmt, &AfDeFeComboBox::DecbEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbItal, &AfVwWnd::CmdCharFmt, &AfDeFeComboBox::DecbEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyBgrndColor, &AfVwWnd::CmdCharFmt,
		&AfDeFeComboBox::DecbEdit::CmsCharFmt)
	ON_CID_CHILD(kcidFmttbApplyFgrndColor, &AfVwWnd::CmdCharFmt,
		&AfDeFeComboBox::DecbEdit::CmsCharFmt)
	ON_CID_CHILD(kcidEditUndo, &AfDeFeComboBox::DecbEdit::CmdEditUndo,
		&AfDeFeComboBox::DecbEdit::CmsEditUndo)
	ON_CID_CHILD(kcidEditRedo, &AfDeFeComboBox::DecbEdit::CmdEditRedo,
		&AfDeFeComboBox::DecbEdit::CmsEditRedo)
END_CMD_MAP_NIL()


//:>********************************************************************************************
//:>	AfDeFeComboBox methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDeFeComboBox::AfDeFeComboBox()
	: AfDeFieldEditor()
{
	m_itss = kitssEmpty;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeFeComboBox::~AfDeFeComboBox()
{
}

/*----------------------------------------------------------------------------------------------
	Relase all smart pointers.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::OnReleasePtr()
{
	if (m_hvoPssl)
	{
		PossListInfoPtr qpli;
		if (GetLpInfo()->GetPossList(m_hvoPssl, m_wsMagic, &qpli))
		{
			AssertPtr(qpli);
			qpli->RemoveNotify(this);
		}
	}
	m_vtss.Clear();
	m_qde.Clear();
	SuperClass::OnReleasePtr();
}


/*----------------------------------------------------------------------------------------------
	Finish initialization.
	@param pnt
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::Init(PossNameType pnt)
{
	Assert(m_hvoObj); // Initialize should have been called first.
	Assert((uint)pnt < (uint)kpntLim);
	m_pnt = pnt;
	CreateFont();
}


/*----------------------------------------------------------------------------------------------
	Finish setting for a possibility list.
	@param hvoPssl The id of the list.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::SetPssl(HVO hvoPssl)
{
	Assert(hvoPssl);
	m_hvoPssl = hvoPssl;
	PossListInfoPtr qpli;
	GetLpInfo()->LoadPossList(m_hvoPssl, m_wsMagic, &qpli);
	qpli->AddNotify(this);
}


/*----------------------------------------------------------------------------------------------
Deletes the selected text in this this control.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::DeleteSelectedText()
{
}


/*----------------------------------------------------------------------------------------------
Returns true if there is selected text in this this control.
@return True if there is text selected.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::IsTextSelected()
{
	int ichAnchor = 0;
	int ichEnd = 0;
	::SendMessage(m_hwnd, EM_GETSEL, (WPARAM)&ichAnchor, (LPARAM)&ichEnd);
	if (ichEnd - ichAnchor)
		return true;
	return false;
}


/*----------------------------------------------------------------------------------------------
	Draw to the given clip rectangle.
	@param hdc
	@param rcpClip
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::Draw(HDC hdc, const Rect & rcpClip)
{
	Assert(hdc);

	StrUni stu;

	int ws;
	if (m_itss != kitssEmpty)
	{
		if (m_hvoPssl)
		{
			// Process a possibility list item.
			PossListInfoPtr qpli;
			GetLpInfo()->LoadPossList(m_hvoPssl, m_wsMagic, &qpli);
			PossItemInfo * pii = qpli->GetPssFromIndex(m_itss);
			pii->GetName(stu, m_pnt);
			ws = pii->GetWs();
		}
		else
		{
			// Process a string from a vector.
			int nVar;
			ITsTextPropsPtr qttp;
			CheckHr(m_vtss[m_itss]->get_PropertiesAt(0, &qttp));
			CheckHr(qttp->GetIntPropValues(ktptWs, &nVar, &ws));

			SmartBstr sbstr;
			CheckHr(m_vtss[m_itss]->get_Text(&sbstr));
			stu = sbstr.Chars();
		}

		MakeCharProps(ws);
		CreateFont();
	}

	AfGfx::FillSolidRect(hdc, rcpClip, m_chrp.clrBack);
	COLORREF clrBgOld = AfGfx::SetBkColor(hdc, m_chrp.clrBack);
	COLORREF clrFgOld = AfGfx::SetTextColor(hdc, m_chrp.clrFore);

	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, m_hfont);
	::TextOutW(hdc, rcpClip.left + 2, rcpClip.top + 1, stu.Chars(), stu.Length());
	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	AfGfx::SetBkColor(hdc, clrBgOld);
	AfGfx::SetTextColor(hdc, clrFgOld);
}


/*----------------------------------------------------------------------------------------------
	Make a combo box to allow editing. hwnd is the parent hwnd. rc is the size of the child
	window. Store the hwnd and return true.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::BeginEdit(HWND hwnd, Rect &rc, int dxpCursor, bool fTopCursor,
	TptEditable tpte)
{
	// This can happen because the edit box doesn't quite fill the FieldEditor, which means
	// that it is possible to click in the field editor while the editor is active. If this
	// happens, ignore the click.
	if (m_qde)
		return true;
	if (!SuperClass::BeginEdit(hwnd, rc, dxpCursor, fTopCursor))
		return false;
	m_qde.Create();
	m_qde->SetEditable(tpte);
	m_qde->m_qdecb = this;
	IActionHandler * pacth = BeginTempEdit();
	ILgWritingSystemFactoryPtr qwsf;
	GetLpInfo()->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);
	m_qde->Create(hwnd, kwidEditChild, WS_CHILD | ES_LEFT | ES_AUTOHSCROLL | ES_NOHIDESEL,
		NULL, (ITsString *)NULL, qwsf, m_ws, pacth);
	m_hwnd = m_qde->Hwnd();
	Rect rcT(rc.left + 2, rc.top + 1, rc.right, rc.bottom);
	::MoveWindow(m_hwnd, rcT.left, rcT.top, rcT.Width(), rcT.Height(), true);

	// Add text to the window.
	::SendMessage(m_hwnd, FW_EM_SETSTYLE, m_chrp.clrBack, (LPARAM)&m_qfsp->m_stuSty);
	::SendMessage(m_hwnd, EM_SETMARGINS, EC_RIGHTMARGIN | EC_LEFTMARGIN,
		MAKELPARAM(0, kdxpButtonMargin));
	SetItem(m_itss);
	::ShowWindow(m_hwnd, SW_SHOW);
	// When we tab into the field, we want everything to higlight to make it easy to change.
	::SendMessage(m_hwnd, EM_SETSEL, 0, -1);
	//::SendMessage(m_hwnd, WM_SETFONT, (WPARAM)::GetStockObject(DEFAULT_GUI_FONT), 0);
	// Foreground/background colors are set via WM_CTLCOLOREDIT in AfDeFeWnd.

	WndCreateStruct wcsButton;
	wcsButton.InitChild(_T("BUTTON"), m_hwnd, kwidEditChild);
	wcsButton.style |= WS_VISIBLE | BS_OWNERDRAW;
	Rect rcTb;
	::GetClientRect(m_hwnd, &rcTb);
	rcTb.left = rcTb.right - kdxpButtonWid;
	rcTb.bottom = rcTb.top + Min((int)rcTb.bottom - (int)rcTb.top, (int)kdxpButtonHeight);

	wcsButton.SetRect(rcTb);

	DecbButtonPtr qdb;
	qdb.Create();
	qdb->CreateAndSubclassHwnd(wcsButton);
	qdb->m_qdecb = this;
	m_hwndButton = qdb->Hwnd();
	::SendMessage(m_hwndButton, WM_SETFONT, (WPARAM)::GetStockObject(DEFAULT_GUI_FONT), 0);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Move the edit and button windows.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::MoveWnd(const Rect & rcClip)
{
	::MoveWindow(m_hwnd, rcClip.left + 2, rcClip.top + 1, rcClip.Width() - 2,
		rcClip.Height() - 1, true);
	Rect rc;
	::GetClientRect(m_hwnd, &rc);
	rc.left = rc.right - kdxpButtonWid;
	rc.bottom = rc.top + Min((int)rc.bottom - (int)rc.top, (int)kdxpButtonHeight);

	if (m_hwndButton)
	{
		::MoveWindow(m_hwndButton, rc.left, rc.top, rc.Width(), rc.Height(), true);
	}
	else
	{
		// Create the button if it isn't present.
		WndCreateStruct wcsButton;
		wcsButton.InitChild(_T("BUTTON"), m_hwnd, kwidEditChild);
		wcsButton.style |= WS_VISIBLE | BS_OWNERDRAW;
		Rect rcTb;
		::GetClientRect(m_hwnd, &rcTb);
		rcTb.left = rcTb.right - kdxpButtonWid;
		rcTb.bottom = rcTb.top + Min((int)rcTb.bottom - (int)rcTb.top, (int)kdxpButtonHeight);
		wcsButton.SetRect(rcTb);

		DecbButtonPtr qdb;
		qdb.Create();
		qdb->CreateAndSubclassHwnd(wcsButton);
		qdb->m_qdecb = this;
		m_hwndButton = qdb->Hwnd();
	}
}


/*----------------------------------------------------------------------------------------------
	Return true if any changes have been made.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::IsDirty()
{
	if (!m_hwnd)
		return false; // Editor not active.
	HVO hvoOrig;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	CheckHr(qcvd->get_ObjectProp(m_hvoObj, m_flid, &hvoOrig));

	HVO hvoNew;
	if (m_itss == kitssEmpty)
	{
		hvoNew = 0;
	}
	else if (m_hvoPssl)
	{
		PossListInfoPtr qpli;
		GetLpInfo()->LoadPossList(m_hvoPssl, m_wsMagic, &qpli);
		PossItemInfo * pii = qpli->GetPssFromIndex(m_itss);
		hvoNew = pii->GetPssId();
	}
	else
	{
		hvoNew = m_itss;
	}
	return hvoOrig != hvoNew;
}


/*----------------------------------------------------------------------------------------------
	Save changes that have been made to the current editor.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::SaveEdit()
{
	// If the item has changed, save it to the data cache.
	EndTempEdit();

	HVO hvoOrig;
	int nOrig;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	if (m_hvoPssl)
		CheckHr(qcvd->get_ObjectProp(m_hvoObj, m_flid, &hvoOrig));
	else
	{
		CheckHr(qcvd->get_IntProp(m_hvoObj, m_flid, &nOrig));
		hvoOrig = (HVO)nOrig;
	}

	HVO hvoNew;
	if (m_itss == kitssEmpty)
	{
		hvoNew = 0;
	}
	else if (m_hvoPssl)
	{
		PossListInfoPtr qpli;
		GetLpInfo()->LoadPossList(m_hvoPssl, m_wsMagic, &qpli);
		PossItemInfo * pii = qpli->GetPssFromIndex(m_itss);
		hvoNew = pii->GetPssId();
	}
	else
	{
		hvoNew = m_itss;
	}
	if (hvoOrig != hvoNew)
	{
		// Check if the record has been edited by someone else since we first loaded the data.
		HRESULT hrTemp;
		if ((hrTemp = qcvd->CheckTimeStamp(m_hvoObj)) != S_OK)
		{
			// If it was changed and the user does not want to overwrite it, perform a refresh
			// so the displayed field will revert to its original value.
			m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
			qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1);
			goto LFinish;
		}

		// Update the value in the cache and refresh views.
		BeginChangesToLabel();
		if (m_hvoPssl)
			qcvd->SetObjProp(m_hvoObj, m_flid, hvoNew);
		else
			qcvd->SetInt(m_hvoObj, m_flid, hvoNew);
		m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
		qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1);
		CheckHr(qcvd->EndUndoTask());
	}
LFinish:
	// We need to leave in a state that cancels undo actions since this may be called
	// without actually closing the edit box.
	SaveFullCursorInfo();
	BeginTempEdit();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Close the current editor, saving changes that were made. hwnd is the editor handle.
	@param fForce True if we want to force the editor closed without making any
		validity checks or saving any changes.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::EndEdit(bool fForce)
{
	SuperClass::EndEdit(fForce);
	if (!fForce)
	{
		// See if the user entered a new item.
		if (m_itss == kitssEmpty)
		{
			// Get the string entered by the user.
			ITsStringPtr qtss;
			StrUni stu; // Name/Abbr string entered by user.
			int cch;
			OLECHAR * pchBuf;
			// Get the characters from the edit box.
			::SendMessage(m_hwnd, FW_EM_GETTEXT, 0, (LPARAM)&qtss);
			qtss->get_Length(&cch);
			stu.SetSize(cch, &pchBuf);
			qtss->FetchChars(0, cch, pchBuf);

			if (cch)
			{
				// The user created a new item, so add it to the possibility list.
				HVO hvo;
				PossListInfoPtr qpli;
				GetLpInfo()->LoadPossList(m_hvoPssl, m_wsMagic, &qpli);
				m_fSaving = true; // Disable updates while adding a new item.
				hvo = CreatePss(qpli->GetPsslId(), stu.Chars(), m_pnt, false);
				m_fSaving = false;
				m_itss = qpli->GetIndexFromId(hvo);
				Assert(m_itss >= 0); // What should we do if we have an error?
			}
		}
		if (!SaveEdit())
		{
			Assert(false); // Should have called IsOkToClose() first.
		}
	}

	EndTempEdit();
	// If a listbox is open, get rid of it.
	DecbEditPtr qde = dynamic_cast<DecbEdit *>(AfWnd::GetAfWnd(m_hwnd));
	if (qde && qde->m_qdef)
		::DestroyWindow(qde->m_qdef->Hwnd());
	// Get rid of the edit box and button.
	::DestroyWindow(m_hwnd);
	m_hwnd = 0;
	m_hwndButton = 0;
	m_qde.Clear();
}


/*----------------------------------------------------------------------------------------------
	Set the height for the specified width, and return it.
----------------------------------------------------------------------------------------------*/
int AfDeFeComboBox::SetHeightAt(int dxpWidth)
{
	Assert(dxpWidth > 0);
	if (dxpWidth != m_dxpWidth)
	{
		m_dxpWidth = dxpWidth;
		// The height is set when the field is initialized and doesn't change here.
	}
	return m_dypHeight;
}

/*----------------------------------------------------------------------------------------------
	This is called when the user clicks inside a field editor that is active but which
	has not filled its entire area with a child window (if it does fill its entire
	area, this can't happen). The point passed is in SCREEN coordinates. Should return
	true if it handled the click, false for default click handling.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::ActiveClick(POINT pt)
{
	// Pretend the click happened to the edit box.
	HWND hwndEdit = m_qde->Hwnd();
	::ScreenToClient(hwndEdit, &pt);
	::SendMessage(hwndEdit, WM_LBUTTONDOWN, MK_LBUTTON, MAKELPARAM((WORD)(pt.x), (WORD)(pt.y)));

	return true;
}


/*----------------------------------------------------------------------------------------------
	This method saves the current cursor information in RecMainWnd. Normally it just
	stores the cursor index in RecMainWnd::m_ichCur. For structured texts, however,
	it also inserts the appropriate hvos and flids for the StText classes in
	m_vhvoPath and m_vflidPath. Other editors may need to do other things.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::SaveCursorInfo()
{
	// Store the current record/subrecord and field info.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (!prmw)
		return;
	// On BeginEdit we come in here before we have an edit box, so return 0.
	int ichAnchor = 0;
	int ichEnd = 0;
	if (m_hwnd)
		::SendMessage(m_hwnd, EM_GETSEL, (WPARAM)&ichAnchor, (LPARAM)&ichEnd);
	prmw->SetCursorIndex(Min(ichAnchor, ichEnd));
}


/*----------------------------------------------------------------------------------------------
	This attempts to place the cursor as defined in RecMainWnd m_vhvoPath, m_vflidPath,
	and m_ichCur.
	@param vhvo Vector of ids inside the field.
	@param vflid Vector of flids inside the field.
	@param ichCur Character offset in the final field for the cursor.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur)
{
	// Store the current record/subrecord and field info.
	::SendMessage(m_hwnd, EM_SETSEL, ichCur, ichCur);
}


/*----------------------------------------------------------------------------------------------
	Set the edit box to the specified item. It returns the result from FW_EM_SETTEXT.
----------------------------------------------------------------------------------------------*/
int AfDeFeComboBox::SetItem(int itss)
{
#ifdef DEBUG
	bool fOk = (itss == kitssEmpty);
	if (m_hvoPssl)
	{
		PossListInfoPtr qpli;
		GetLpInfo()->LoadPossList(m_hvoPssl, m_wsMagic, &qpli);
		fOk |= (uint)itss < (uint)qpli->GetCount();
	}
	else
		fOk |= (uint)itss < (uint)m_vtss.Size();
	Assert(fOk);
#endif
	ITsStringPtr qtss;
	m_itss = itss;
	DecbEditPtr qde = dynamic_cast<DecbEdit *>(AfWnd::GetAfWnd(m_hwnd));
	AssertPtr(qde);
	qde->m_fRecurse = true; // Keep the cursor at the beginning of the item.
	if (itss == kitssEmpty)
		return ::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)qtss.Ptr());
	if (m_hvoPssl)
	{
		// Process a possibility list item.
		StrUni stu;
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		PossListInfoPtr qpli;
		GetLpInfo()->LoadPossList(m_hvoPssl, m_wsMagic, &qpli);
		PossItemInfo * pii = qpli->GetPssFromIndex(itss);
		pii->GetName(stu, m_pnt);
		int ws;
		ws = pii->GetWs();
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, &qtss);
		return ::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)qtss.Ptr());
	}
	else
	{
		// Process a string from a vector.
		int ws = m_ws;
		int nVar;
		ITsTextPropsPtr qttp;
		CheckHr(m_vtss[m_itss]->get_PropertiesAt(0, &qttp));
		CheckHr(qttp->GetIntPropValues(ktptWs, &nVar, &ws));

		return ::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)m_vtss[itss].Ptr());
	}
}

/*----------------------------------------------------------------------------------------------
	Refresh the field from the data cache.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::UpdateField()
{
	// Get the item info from the cache.
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	if (m_hvoPssl)
	{
		HVO hvoPss;
		CheckHr(qcvd->get_ObjectProp(m_hvoObj, m_flid, &hvoPss));
		if (hvoPss)
		{
			PossListInfoPtr qpli;
			GetLpInfo()->LoadPossList(m_hvoPssl, m_wsMagic, &qpli);
			PossListInfo * ppli = NULL;
			m_itss = qpli->GetIndexFromId(hvoPss, &ppli);
			if (ppli)
				m_hvoPssl = ppli->GetPsslId();
		}
		else
		{
			m_itss = -1;
		}
	}
	else
		CheckHr(qcvd->get_IntProp(m_hvoObj, m_flid, &m_itss));

	// If we have an edit box, update the contents.
	if (m_hwnd)
		SetItem(m_itss);
}


/*----------------------------------------------------------------------------------------------
	Check the requirements of the FldSpec, and verify that data in the field meets the
	requirement. It returns:
		kFTReqNotReq if the all requirements are met.
		kFTReqWs if data is missing, but it is encouraged.
		kFTReqReq if data is missing, but it is required.
----------------------------------------------------------------------------------------------*/
FldReq AfDeFeComboBox::HasRequiredData()
{
	if (m_itss == kitssEmpty)
		return m_qfsp->m_fRequired;
	else
		return kFTReqNotReq;
}


/*----------------------------------------------------------------------------------------------
	Set things up for editing with a temporary data cache by marking the point to return to
	in the undo stack. Also returns the action handler which should be installed in the
	temporary cache. This can be called multiple times. If the mark is already set, it does
	nothing.
----------------------------------------------------------------------------------------------*/
IActionHandler * AfDeFeComboBox::BeginTempEdit()
{
	// Get your action handler.
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	IActionHandlerPtr qacth;
	CheckHr(qcvd->GetActionHandler(&qacth));
	if (!m_hMark)
		CheckHr(qacth->Mark(&m_hMark));
	return qacth;
}


/*----------------------------------------------------------------------------------------------
	End editing with a temporary data cache by clearing stuff out down to the mark created by
	BeginTempEdit. This can be called any number of times. If a mark is not in progress, it
	does nothing.
----------------------------------------------------------------------------------------------*/
IActionHandler * AfDeFeComboBox::EndTempEdit()
{
	// Get your action handler.
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	IActionHandlerPtr qacth;
	CheckHr(qcvd->GetActionHandler(&qacth));
	// Clear out any temporary Undo items relating to this window.
	if (m_hMark)
	{
		// AfDeFeComboBox::DecbEdit::OnKillFocus() calls this method *after*
		// AfVwRootSite::CmdFmtStyles1() calls BeginUndoTask(), which sets the depth to 1.
		// Calling DiscardToMark() when the depth is greater than zero crashes.  (See CLE-64.)
		int nDepth;
		CheckHr(qacth->get_CurrentDepth(&nDepth));
		if (nDepth == 0)
		{
			CheckHr(qacth->DiscardToMark(0));
			m_hMark = 0;
		}
	}
	return qacth;
}


/*----------------------------------------------------------------------------------------------
	Something has changed in the possibility list.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::ListChanged(int nAction, HVO hvoPssl, HVO hvoSrc, HVO hvoDst, int ipssSrc,
	int ipssDst)
{
	// We don't want to update the field while we are in the middle of saving.
	if (!m_fSaving)
		UpdateField();
}


//:>********************************************************************************************
//:>	AfDeFeComboBox::DecbEdit methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Clean up smart pointers.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::DecbEdit::OnReleasePtr(void)
{
	m_qdecb.Clear();
	m_qdef.Clear();
	SuperClass::OnReleasePtr();
}

/*----------------------------------------------------------------------------------------------
	Process window messages for edit box. Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_SYSKEYDOWN:
		// Alt+Up and Alt+Down toggles the drop down list.
		if (wp == VK_DOWN || wp == VK_UP)
			{
				// If a listbox is open, get rid of it.
				DecbEditPtr qde = dynamic_cast<DecbEdit *>(AfWnd::GetAfWnd(m_hwnd));
				if (qde && qde->m_qdef)
					::DestroyWindow(qde->m_qdef->Hwnd());
				else
					OnButtonClk();
				return true;
			}
		break;

	case WM_KEYDOWN:
		{
			m_ch = wp;
			switch (wp)
			{
			case VK_TAB:
				if (::GetKeyState(VK_SHIFT) < 0)
					// Shift Tab to previous editor.
					m_qdecb->GetDeWnd()->OpenPreviousEditor(DxpCursorOffset(), true);
				else
					// Tab to next editor.
					m_qdecb->GetDeWnd()->OpenNextEditor(DxpCursorOffset());
				return true;

			case VK_LEFT:
			case VK_RIGHT:
				{
					if (OnKeyDown(wp, LOWORD(lp), HIWORD(lp)))
					{
						int ichAnchor;
						int ichEnd;
						GetSel(&ichAnchor, &ichEnd);
						m_cchMatched = Min(ichAnchor, ichEnd);
					}
					else
					{
						if (wp == VK_LEFT)
							m_qdecb->GetDeWnd()->OpenPreviousEditor(9999, false);
						else
							m_qdecb->GetDeWnd()->OpenNextEditor();
					}
					return true;
				}

			case VK_UP:
				// Up arrow to previous editor.
				m_qdecb->GetDeWnd()->OpenPreviousEditor(DxpCursorOffset(), false);
				return true;

			case VK_DOWN:
				// Down arrow to next editor.
				m_qdecb->GetDeWnd()->OpenNextEditor(DxpCursorOffset());
				return true;

			case VK_END:
				SuperClass::FWndProc(wm, wp, lp, lnRet);
				m_cchMatched = GetTextLength();
				return true;

			case VK_HOME:
				SuperClass::FWndProc(wm, wp, lp, lnRet);
				m_cchMatched = 0;
				return true;

			case VK_PRIOR:
			case VK_NEXT:
				// Scroll the entire data entry window up or down one page.
				return m_qdecb->GetDeWnd()->ScrollKey(wp, lp);

			default:
				break;
			}
			break;
		}

	case WM_LBUTTONDOWN:
		{
			SuperClass::FWndProc(wm, wp, lp, lnRet);
			int ichAnchor;
			int ichEnd;
			GetSel(&ichAnchor, &ichEnd);
			m_cchMatched = Min(ichAnchor, ichEnd);
			return true;
		}

	default:
		break;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Enable/Disable Format toolbar buttons (and the format font command).
	All these are disabled in a tags field; there is nothing here we should be editing, except
	pseudo-editing to select something.
	@param cms The command state object.
	@return True indicating it was handled.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::CmsCharFmt(CmdState & cms)
{
	if (cms.Cid() == kcidFmtStyles)
		cms.Enable(true);
	else
		cms.Enable(false);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return the horizontal pixel offset of the cursor.
----------------------------------------------------------------------------------------------*/
int AfDeFeComboBox::DecbEdit::DxpCursorOffset()
{
	Assert(m_qrootb);

	int dxp;
	CheckHr(m_qrootb->get_XdPos(&dxp));
	return dxp;
}


/*----------------------------------------------------------------------------------------------
	Called immediately after it is created before other events happen.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::DecbEdit::PostAttach(void)
{
}


/*----------------------------------------------------------------------------------------------
	The edit box changed. We need to validate what was done.
	@return true
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::OnChange()
{
	if (m_fRecurse)
	{
		m_fRecurse = false;
		return true;
	}
	if (!m_hwnd)
		return true; // We aren't completely set up yet, so ignore this.

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);

	// Get the characters from the edit box.
	ITsStringPtr qtss;
	GetText(&qtss);

	int ws;
	int nVar;
	ITsTextPropsPtr qttp;
	CheckHr(qtss->get_PropertiesAt(0, &qttp));
	CheckHr(qttp->GetIntPropValues(ktptWs, &nVar, &ws));

	int ichMin;
	int ichLim;
	GetSel(&ichMin, &ichLim);

	int cchTyped;
	// JohnT: use a StrUni rather than an StrUniBuf, because some user sometime will accidentally
	// paste something long here, and performance here is not critical.
	StrUni stuTyped;
	OLECHAR * pchBuf;
	qtss->get_Length(&cchTyped);

	if (cchTyped > kcchPossNameAbbrMax)
	{
		if (ichMin == cchTyped)
			ichMin = kcchPossNameAbbrMax;
		if (ichLim == cchTyped)
			ichLim = kcchPossNameAbbrMax;
		cchTyped = kcchPossNameAbbrMax;
		m_fRecurse = true; // Stop the recursion caused by the next instruction.
		SetText(qtss); // Note: This recursively calls this procedure.
		SetSel(ichMin, ichMin);
	}

	stuTyped.SetSize(cchTyped, &pchBuf);
	qtss->FetchChars(0, cchTyped, pchBuf);

	bool fTypeAhead = false; // allow type ahead only when adding characters at end of current item
							// or backspacing at end of current item.
	bool fNeedCompare;
	if (m_ch == 0) // (see kcidEditPaste special code)
	{
		// If we pasted something, force a compare.
		fNeedCompare = true;
	}
	else if (ichMin == m_cchMatched + 1 && m_ch != VK_BACK && m_ch != VK_DELETE)
	{
		// Need to compare if we typed a character and we are one greater than last match.
		fNeedCompare = true;
		if (cchTyped == ichMin)
		{
			fTypeAhead = true;
		}
	}
	else if (ichMin > m_cchMatched)
	{
		// Don't compare any other time we are past the last match.
		fNeedCompare = false;
	}
	else if ((cchTyped == ichMin) && (ichMin == ichLim) && (m_ch != 46))
	{
		// Need to compare if we typed a character and we are at the end of the item
		// Need to compare if we delete the last character in the non-type-ahead string
		fNeedCompare = true;
		fTypeAhead = true;
	}
	else
	{
		// Always compare if we are not past the last match.
		fNeedCompare = true;
	}

	int cch;

	// Since the edit box deletes the selection on backspace, we need to use
	// some extra logic to make backspace actually move the selection back.
	if (m_cchMatched && m_ch == VK_BACK && m_qdecb->m_itss != kitssEmpty)
	{
		// If we had a previous match and we got a backspace, we always decrement the matched
		// characters and start looking at that point.
		cch = --m_cchMatched;
	}
	else
	{
		// Otherwise we start looking at the cursor location.
		cch = ichMin;
	}

	int itss;
	bool fFound = false;;
	StrUni stuFound;
	fNeedCompare = true;

	if (cchTyped == 0)
	{
		// If nothing to match.
		m_cchMatched = 0;
		if (m_qdecb->m_hvoPssl)
		{
			// For a possibility list.
			// If nothing to match, get the first item in the possibility list. If we are
			// already at that item, remove the item. If we are already cleared, do nothing.
			if (m_qdecb->m_itss == kitssEmpty)
				return true;
			// If everything is highlighted, we want to clear the item with Del or Bsp.
			// But when we are backspacing, if there is only one character left and we backspace
			// over that, we want to switch to the first item in the list.
			itss = m_qdecb->GetIndex();
			if (ichMin != 1)
			{
				m_qdecb->SetIndex(kitssEmpty);
				qtsf->MakeStringRgch(L"", 0, m_qdecb->m_ws, &qtss);
				m_cchMatched = 0; // Keep cursor at beginning of item.
				m_fRecurse = true; // Stop the recursion caused by the next instruction.
				SetText(qtss); // Note: This recursively calls this procedure.
				return true;
			}
			else
			{
				itss = 0;
				StrUni stu;
				PossListInfoPtr qpli;
				m_qdecb->GetDeWnd()->GetLpInfo()->LoadPossList(m_qdecb->m_hvoPssl,
					m_qdecb->m_wsMagic, &qpli);
				PossItemInfo * pii = qpli->GetPssFromIndex(0);
				pii->GetName(stu, m_qdecb->m_pnt);
				stuFound = stu;
				fFound = true;
				ws = pii->GetWs();
			}
		}
		else
		{
			// For a list, set it to the default item.
			m_qdecb->SetIndex(0);
			m_fRecurse = true; // Keep cursor at beginning of item.
			SetText(m_qdecb->m_vtss[0].Ptr());
			SetSel(0, -1);
			return true;
		}
	}
	else if (fNeedCompare)
	{
		// Try to find an item that matches what the user typed in the possibility list.
		StrUni stuMatch(stuTyped);
/////		stuMatch.Replace(cch, stuMatch.Length(), L"");// Delete chars to right of cursor.
		int wsTemp;
		if (m_qdecb->m_hvoPssl)
		{
			if (fTypeAhead)
				fFound = FindPliItem(stuMatch, stuFound, &itss, &wsTemp);
			else
				fFound = FindPliItem(stuMatch, stuFound, &itss, &wsTemp, true);
		}
		else
		{
			stuMatch.Replace(cch, stuMatch.Length(), L"");// Delete chars to right of cursor.
			fFound = FindVecItem(stuMatch, stuFound, &itss);
			wsTemp = m_qdecb->m_ws;
		}
		if (fFound)
			ws = wsTemp;
	}

	if (fFound)
	{
		// If found, process the new item.
		m_qdecb->SetIndex(itss);
		m_cchMatched = cch;
	}
	else
	{
		// Something illegal was typed. Assume they are adding a new item.
		if (m_cchMatched + 1 == cch && m_ch != VK_BACK)
			::MessageBeep(MB_ICONEXCLAMATION); // Beep on the first unmatched character.
		if (m_qdecb->m_hvoPssl)
		{
			// Underline the string with a red squiggly.
			ITsIncStrBldrPtr qtisb;
			qtisb.CreateInstance(CLSID_TsIncStrBldr);
			qtisb->SetIntPropValues(ktptWs, ktpvDefault, m_qdecb->m_ws);
			CheckHr(qtisb->SetIntPropValues(ktptUnderColor, ktpvDefault, kclrRed));
			CheckHr(qtisb->SetIntPropValues(ktptUnderline, ktpvEnum, kuntSquiggle));
			qtisb->AppendRgch(stuTyped.Chars(), stuTyped.Length());
			qtisb->GetString(&qtss);
			m_fRecurse = true; // Stop the recursion caused by the next instruction.
			// Note: This recursively calls this procedure.
			SetText(qtss);
			m_qdecb->SetIndex(kitssEmpty); // We no longer have a matched HVO.
			return true;
		}
		else
		{
			// Can't create new items for enums.
			itss = m_qdecb->GetIndex();
			SmartBstr sbstr;
			CheckHr(m_qdecb->m_vtss[itss]->get_Text(&sbstr));
			stuFound = sbstr.Chars();
		}
	}

	// Update the edit box text and selection.
	qtsf->MakeStringRgch(stuFound.Chars(), stuFound.Length(), ws, &qtss);
	m_fRecurse = true; // Stop the recursion caused by the next instruction.
	if (fTypeAhead || !m_qdecb->m_hvoPssl)
	{
		SetText(qtss); // Note: This recursively calls this procedure.
		SetSel(m_cchMatched, stuFound.Length());
	}
	else
	{
		SetText(qtss); // Note: This recursively calls this procedure.
		SetSel(ichMin, ichMin);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Find a string in the poss list that matches stuTyped. If found, set stuFound to the found
	string, and if non-NULL, set pitss to the correct index, and set pwspss to the item ws,
	then return true. Return false if it wasn't found.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::FindPliItem(StrUni & stuTyped, StrUni & stuFound,
	int * pipss, int * pwspss, ComBool fExactMatch)
{
	Assert(m_qdecb->GetPssl());
	// Try to find a corresponding item in the list.
	PossItemInfo * ppii = NULL;
	PossListInfoPtr qpli;
	m_qdecb->GetDeWnd()->GetLpInfo()->LoadPossList(m_qdecb->GetPssl(), m_qdecb->m_wsMagic,
		&qpli);
	AssertPtr(qpli);
//	ppii = qpli->FindPss(stuTyped.Chars(), m_qdecb->m_pnt, pipss);
	Locale loc = m_qdecb->GetLpInfo()->GetLocale(m_qdecb->m_ws);
	ppii = qpli->FindPss(stuTyped.Chars(), loc, m_qdecb->m_pnt, pipss, fExactMatch);
	if (ppii)
	{
		ppii->GetName(stuFound, m_qdecb->m_pnt);
		*pwspss = ppii->GetWs();
		return true;
	}
	stuFound = "";
	*pipss = kitssEmpty;
	return false;
}


/*----------------------------------------------------------------------------------------------
	Find a string in the vector that matches stuTyped. If found, set stuFound to the found
	string, and if non-NULL, set pitss to the correct index, then return true. Return false
	if it wasn't found.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::FindVecItem(StrUni & stuTyped, StrUni & stuFound,
	int * pitss)
{
	int itss;
	SmartBstr sbstr;
	ITsStringPtr qtss;
	int ctss = m_qdecb->m_vtss.Size();
	int cch = stuTyped.Length();
	for (itss = 0; itss < ctss; ++itss)
	{
		CheckHr(m_qdecb->m_vtss[itss]->get_Text(&sbstr));
		// TODO KenZ: This needs to use our smart encoding-aware comparison.
		if (_wcsnicmp(stuTyped.Chars(), sbstr.Chars(), cch) == 0)
		{
			stuFound = sbstr.Chars();
			if (pitss)
				*pitss = itss;
			return true;
		}
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Setting focus on the editor.
	@param hwndOld The handle of the window losing focus.
	@param fTbControl ?
	@return superclass' OnSetFocus success state
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::OnSetFocus(HWND hwndOld, bool fTbControl)
{
	AfApp::Papp()->AddCmdHandler(this, 1, kgrfcmmAll);
	m_qdecb->BeginTempEdit();
	// Using SuperClass below is ambiguous.
	return ScrollSuperClass::OnSetFocus(hwndOld, fTbControl);
}


/*----------------------------------------------------------------------------------------------
	Clearing focus on the editor
	@param hwndNew The handle of the window gaining focus.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::OnKillFocus(HWND hwndNew)
{
	AfApp::Papp()->RemoveCmdHandler(this, 1);
	if (m_qdecb->IsOkToClose(false)) // Don't raise an error message here.
		m_qdecb->SaveEdit();
	m_qdecb->EndTempEdit();
	return SuperClass::OnKillFocus(hwndNew);
}


/*----------------------------------------------------------------------------------------------
	Enable/Disable Edit buttons. Copy, Paste, Select All always enabled. Cut enabled whenever
	there is a selection.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::CmsEditUpdate(CmdState & cms)
{
	switch (cms.Cid())
	{
	case kcidEditCut:
	case kcidEditCopy:
		{
			int ichStart = 0;
			int ichStop = 0;
			// When we click on another editor, the outgoing editor gets closed before the
			// command handler empties its queue (apparently). In any case, we come in here
			// with m_hwnd == NULL. So we need to catch this here, unless there is a better way.
			if (m_hwnd)
				GetSel(&ichStart, &ichStop);
			cms.Enable(ichStart != ichStop);
		}
		break;

	case kcidEditPaste:
	case kcidEditDel:
	case kcidEditSelAll:
		cms.Enable(true);
		break;

	default:
		Assert(false);
		cms.Enable(false);
		break;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle Edit commands
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::CmdEdit(Cmd * pcmd)
{
	AssertObj(pcmd);

	switch (pcmd->m_cid)
	{
	case kcidEditCut:
		m_ch = VK_DELETE;
		Cut();
		break;
	case kcidEditCopy:
		Copy();
		break;
	case kcidEditPaste:
		m_ch = 0; // Special code to indicate paste.
		Paste();
		break;
	case kcidEditDel:
		::SendMessage(m_hwnd, WM_CLEAR, 0, 0);
		break;
	case kcidEditSelAll:
		SetSel(0, -1);
		m_cchMatched = 0;
		break;
	default:
		Assert(false); // We shouldn't get here.
		return false;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Process commands. Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::OnButtonClk()
{
	// Process a button click in the edit box if we don't already have a combo box open..
	if (!m_qdef)
	{
		// Ignore the button click if the combo-box was just closed.
		// See AfDeFeComboBox::DecbFrame::OnCommand comment for more details.
		if (m_fSkipBnClicked)
		{
			m_fSkipBnClicked = false;
			::InvalidateRect(m_qdecb->m_hwndButton, NULL, false);
			return true;
		}

		::SendMessage(m_qdecb->m_hwndButton, BM_SETSTATE, TRUE, 0);
		::SendMessage(m_hwnd, EM_SETSEL, 0, -1);
		int ctss;
		PossListInfoPtr qpli;
		if (m_qdecb->m_hvoPssl)
		{
			m_qdecb->GetDeWnd()->GetLpInfo()->LoadPossList(m_qdecb->m_hvoPssl,
				m_qdecb->m_wsMagic, &qpli);
			ctss = qpli->GetCount();
		}
		else
			ctss = m_qdecb->m_vtss.Size();
		// Get the number of lines to show in the listbox.
		int nLines = min(15, ctss);
		// The final 2 is for the frame border.
		int dypList = nLines * m_qdecb->m_qadsc->GetTreeFontHeight() + 2;

		// Get the edit box rect in screen coordinates.
		Rect rc;
		GetClientRect(rc);
		Point pt(rc.left, rc.top); // Get a point
		::ClientToScreen(m_hwnd, &pt); // Translate point to screen points
		rc += pt; // Translate original rect to Screen coordinates.

		// Check to see whether listbox should go above or below edit box.
		Rect rcT;
		::GetClientRect(::GetDesktopWindow(), &rcT);
		if (rc.bottom + dypList < rcT.bottom)
		{
			// Listbox goes below edit box.
			rc.top = rc.bottom;
			rc.bottom = rc.top + dypList;
		}
		else
		{
			// Listbox goes above edit box.
			rc.bottom = rc.top - 1;
			rc.top = rc.bottom - dypList;
		}

		// Create the popup window. (Its PostAttach() will create the listbox.)
		WndCreateStruct wcs;
		wcs.lpszClass = _T("STATIC");
		wcs.style = WS_VISIBLE | WS_POPUP;
		wcs.hwndParent = m_hwnd;
		wcs.SetRect(rc);

		m_qdef.Create();
		m_qdef->m_qdecb = m_qdecb;
		m_qdef->CreateAndSubclassHwnd(wcs);
		m_qdef->m_qcbed = this;

		// Fill the list box.
		HWND hwndList = ::GetWindow(m_qdef->Hwnd(), GW_CHILD);
		::SendMessage(hwndList, WM_SETFONT, (WPARAM)::GetStockObject(DEFAULT_GUI_FONT), 0);
		StrUni stu;
		ITsStringPtr qtss;
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		for (int itss = 0; itss < ctss; ++itss)
			if (m_qdecb->m_hvoPssl)
			{
				// Process a possibility list item.
				PossItemInfo * pii = qpli->GetPssFromIndex(itss);
				pii->GetName(stu, m_qdecb->m_pnt);

				int ws;
				ws = pii->GetWs();
				qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, &qtss);
				::SendMessage(hwndList, FW_LB_ADDSTRING, 0, (LPARAM)qtss.Ptr());
			}
			else
			{
				// Process an item from the list of strings.
				::SendMessage(hwndList, FW_LB_ADDSTRING, 0,
					(LPARAM)m_qdecb->m_vtss[itss].Ptr());
			}
		// Select the text in the edit box and in the list box.
		::SendMessage(hwndList, LB_SETCURSEL, m_qdecb->m_itss, 0);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Enable/disable the undo menu item. Because of complexities in undo/redoing these changes,
	for now we disable undo/redo if any typing has been done in this field.
	@param cms menu command state
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::CmsEditUndo(CmdState & cms)
{
	// At this point we've sent a Mark to the action handler and nasty things happen if we try
	// to undo/redo things. So until this problem can be resolved, we will not allow any
	// undo/redo actions while in this field.
//	if (m_qdecb->IsDirty())
//	{
		StrApp staLabel(kstidRedoFieldDisabled);
		cms.SetText(staLabel, staLabel.Length());
		cms.Enable(false);
		return true;
//	}
//	else
//	{
//		RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(MainWindow());
//		Assert(pwnd);
//		return pwnd->CmsEditUndo(cms);
//	}
}


/*----------------------------------------------------------------------------------------------
	Enable/disable the redo menu item. Because of complexities in undo/redoing these changes,
	for now we disable undo/redo if any typing has been done in this field.
	@param cms menu command state
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::CmsEditRedo(CmdState & cms)
{
	// At this point we've sent a Mark to the action handler and nasty things happen if we try
	// to undo/redo things. So until this problem can be resolved, we will not allow any
	// undo/redo actions while in this field.
//	if (m_qdecb->IsDirty())
//	{
		StrApp staLabel(kstidRedoFieldDisabled);
		cms.SetText(staLabel, staLabel.Length());
		cms.Enable(false);
		return true;
//	}
//	else
//	{
//		RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(MainWindow());
//		Assert(pwnd);
//		return pwnd->CmsEditRedo(cms);
//	}
}


/*----------------------------------------------------------------------------------------------
	Handle the undo command by passing it on to the main window.
	@param pcmd menu command
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::CmdEditUndo(Cmd * pcmd)
{
	m_qdecb->EndTempEdit();
	RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(pwnd);
	pwnd->CmdEditUndo(pcmd);
	// Note, due to FullRefresh, this field editor is deleted at this point.
	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle the redo command by passing it on to the main window.
	@param pcmd menu command
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbEdit::CmdEditRedo(Cmd * pcmd)
{
	m_qdecb->EndTempEdit();
	RecMainWnd * pwnd = dynamic_cast<RecMainWnd *>(MainWindow());
	Assert(pwnd);
	pwnd->CmdEditRedo(pcmd);
	// Note, due to FullRefresh, this field editor is deleted at this point.
	return true;
}


//:>********************************************************************************************
//:>	AfDeFeComboBox::DecbButton methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Clean up smart pointers.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::DecbButton::OnReleasePtr(void)
{
	m_qdecb.Clear();
	SuperClass::OnReleasePtr();
}
/*----------------------------------------------------------------------------------------------
	Handle window painting (WM_PAINT). Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbButton::OnDrawThisItem(DRAWITEMSTRUCT * pdis)
{
	AssertObj(this);
	AssertPtr(pdis);
	if (pdis->itemState & ODS_SELECTED)
		::DrawFrameControl(pdis->hDC, &pdis->rcItem, DFC_SCROLL, DFCS_SCROLLDOWN | DFCS_FLAT);
	else
		::DrawFrameControl(pdis->hDC, &pdis->rcItem, DFC_SCROLL, DFCS_SCROLLDOWN);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Return the what's-this help string for the Drop-down list down-arrow button.

	@param pt not used.
	@param pptss Address of a pointer to an ITsString COM object for returning the help string.

	@return true.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbButton::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	StrApp str;
	str.Load(kstidDownarrowButtonWhatsThisHelp); // No context help available
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(str);
	CheckHr(qtsf->MakeString(stu.Bstr(), m_qdecb->UserWs(), pptss));
	return true;
}


/*----------------------------------------------------------------------------------------------
	Process window messages for button. Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbButton::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	// We will probably need this to get tab and arrows to move to next field.
	/*if (wm == WM_KEYDOWN)
	{
		m_qdecb->SetHeight(m_qdecb->GetHeight() + 15);
		m_qdecb->m_qadsc->EditorResizing();
		return false;
	}*/

	if (wm == WM_LBUTTONDOWN)
	{
		DecbEditPtr qdb = dynamic_cast<DecbEdit *>(AfWnd::GetAfWnd(m_qdecb->Hwnd()));
		Assert(qdb);
		qdb->OnButtonClk();
		return true;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Called immediately after it is created before other events happen.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::DecbButton::PostAttach(void)
{
}

/*----------------------------------------------------------------------------------------------
	Process commands. Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbButton::OnCommand(int cid, int nc, HWND hctl)
{
	if (nc == BN_CLICKED)
	{
		int x;
		x = 2;
	}

	return SuperClass::OnCommand(cid, nc, hctl);
}


//:>********************************************************************************************
//:>	AfDeFeComboBox::DecbListBox methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Process window messages for button. Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbListBox::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case WM_SYSKEYDOWN:
		if (wp == VK_UP || wp == VK_DOWN) // Alt+Up or Alt+Down closes list box.
		{
			DecbFramePtr qdf = dynamic_cast<DecbFrame *>(AfWnd::GetAfWnd(::GetParent(m_hwnd)));
			AssertPtr(qdf);
			qdf->m_qcbed->m_qdef.Clear();
			qdf->m_qcbed.Clear();
			::SendMessage(m_qdecb->m_hwndButton, BM_SETSTATE, FALSE, 0);
			::PostMessage(qdf->m_hwnd, WM_CLOSE, 0, 0);
			return true;
		}
		break;

	case WM_KEYDOWN:
		// Handle keyboard input
		{
			DecbFramePtr qdf = dynamic_cast<DecbFrame *>(AfWnd::GetAfWnd(::GetParent(m_hwnd)));
			AssertPtr(qdf);
/*StrAppBuf strb;
strb.Format("Key wp=%d lp=%d\n", wp, lp);
OutputDebugString(strb.Chars());*/

			switch (wp)
			{
			case VK_ESCAPE:
				// Restore the original item.
				m_qdecb->SetItem(m_itssOrg);
				// Select the text in the edit box.
				::SendMessage(m_qdecb->m_hwnd, EM_SETSEL, 0, -1);
				// Close down the combo list box.
				qdf->m_qcbed->m_qdef.Clear();
				qdf->m_qcbed.Clear();
				::SendMessage(m_qdecb->m_hwndButton, BM_SETSTATE, FALSE, 0);
				::PostMessage(qdf->m_hwnd, WM_CLOSE, 0, 0);
				return true;

			// Return closes drop down box.
			case VK_RETURN:
				{
					DecbFramePtr qdf = dynamic_cast<DecbFrame *>(
						AfWnd::GetAfWnd(::GetParent(m_hwnd)));
					AssertPtr(qdf);
					qdf->m_qcbed->m_qdef.Clear();
					qdf->m_qcbed.Clear();
					::SendMessage(m_qdecb->m_hwndButton, BM_SETSTATE, FALSE, 0);
					::PostMessage(qdf->m_hwnd, WM_CLOSE, 0, 0);
					return true;
				}

			case VK_TAB:
				if (::GetKeyState(VK_SHIFT) < 0)
					m_qdecb->GetDeWnd()->OpenPreviousEditor(); // Shift Tab to previous editor.
				else
					m_qdecb->GetDeWnd()->OpenNextEditor(); // Tab to next editor.
				return true;

			default:
				qdf->m_fKeyPressed = true;
				// We are using list-box logic here to move the selection based
				// on the first letter typed.
				break;
			}
		}
		break;

	case WM_MOUSEMOVE:
		// This is where we get mouse moves for the list box.
		{
			POINT pt = {(SHORT)LOWORD(lp), (SHORT)HIWORD(lp)};
			Rect rc;
			GetClientRect(rc);
/*StrAppBuf strb;
strb.Format("Mouse x=%d y=%d; Rect left=%d top=%d rt=%d bt=%d\n", pt.x, pt.y,
	rc.left, rc.top, rc.right, rc.bottom);
OutputDebugString(strb.Chars());*/

			if (::PtInRect(&rc, pt))
			{
				// ItemFromPoint is supposed to return whether a point is in the rect,
				// but for some reason it does not work in this context, so we use above test.
				int iItem = LOWORD(ItemFromPoint(pt));

				GetItemRect(iItem, &rc);
				if (::PtInRect(&rc, pt))
				{
					if (iItem != GetCurSel())
						SetCurSel(iItem);
				}
			}
		}
		break;

	case WM_LBUTTONUP:
		{
			DecbFramePtr qdf = dynamic_cast<DecbFrame *>(AfWnd::GetAfWnd(::GetParent(m_hwnd)));
			AssertPtr(qdf);
			POINT pt = {(SHORT)LOWORD(lp), (SHORT)HIWORD(lp)};
			Rect rc;
			GetClientRect(rc);

			if (::PtInRect(&rc, pt))
			{
				// ItemFromPoint is supposed to return whether a point is in the rect,
				// but for some reason it does not work in this context, so we use above test.
				int iItem = LOWORD(ItemFromPoint(pt));

				GetItemRect(iItem, &rc);
				if (::PtInRect(&rc, pt))
				{
					m_qdecb->SetItem(iItem);
					// Select the text in the edit box.
					::SendMessage(m_qdecb->m_hwnd, EM_SETSEL, 0, -1);
					// Close down the combo list box.
					qdf->m_qcbed->m_qdef.Clear();
					qdf->m_qcbed.Clear();
					::SendMessage(m_qdecb->m_hwndButton, BM_SETSTATE, FALSE, 0);
					::PostMessage(qdf->m_hwnd, WM_CLOSE, 0, 0);
					return true;
				}
			}
		}
		break;

	default:
		break;
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Clean up smart pointers.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::DecbListBox::OnReleasePtr(void)
{
	m_qdecb.Clear();
	SuperClass::OnReleasePtr();
}

/*----------------------------------------------------------------------------------------------
	Called immediately after it is created before other events happen.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::DecbListBox::PostAttach(void)
{
	// This allows us to catch LBN_KILLFOCUS when clicking elsewhere to close the box.
	// SetCapture caused problems with the scroll bar in the list box.
	::SetFocus(m_hwnd);
}

/*----------------------------------------------------------------------------------------------
	Process commands. Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbListBox::OnCommand(int cid, int nc, HWND hctl)
{
	return SuperClass::OnCommand(cid, nc, hctl);
}


/*----------------------------------------------------------------------------------------------
	This isn't being called at the moment (commented out of AfWnd.cpp), so I'm using FWndProc
	instead.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::DecbListBox::OnMouseMove(UINT nFlags, POINT point)
{
	int x;
	x = 2;
}

//:>********************************************************************************************
//:>	AfDeFeComboBox::DecbFrame methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Clean up smart pointers.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::DecbFrame::OnReleasePtr(void)
{
	m_qcbed.Clear();
	SuperClass::OnReleasePtr();
}
/*----------------------------------------------------------------------------------------------
	Finish setting up the frame by embedding a listbox.
----------------------------------------------------------------------------------------------*/
void AfDeFeComboBox::DecbFrame::PostAttach(void)
{
	Rect rc;
	GetClientRect(rc);

	WndCreateStruct wcs;
	wcs.InitChild(_T("LISTBOX"), m_hwnd, 1);
	wcs.style |= WS_VISIBLE | WS_BORDER;
	//wcs.dwExStyle |= WS_EX_TOPMOST;
	wcs.SetRect(rc);

	DecbListBoxPtr qdelb;
	qdelb.Create();
	qdelb->CreateAndSubclassHwnd(wcs);
	qdelb->m_qdecb = m_qdecb;
	qdelb->m_itssOrg = m_qdecb->m_itss; // Save original value for Escape.
}


/*----------------------------------------------------------------------------------------------
	Process commands. Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbFrame::OnCommand(int cid, int nc, HWND hctl)
{
	// Catch a click in some window other than the list box. This also happens following
	// the LBN_SELCHANGE.
	if (nc == LBN_KILLFOCUS)
	{
		if (m_qcbed) // Skip if following LBN_SELCHANGE
		{
			// If the user clicks on the down arrow, we need to keep the edit box from
			// processing the next BN_CLICKED message. Trying to remove messages from the
			// message queue for the edit box and/or the button did not work. So we
			// peek at the next edit box message, which is normally a WM_LBUTTONDBLCLK.
			// If the position is over the button, then we set a flag to skip the
			// BN_CLICKED message in AfDeFeComboBox::DecbEdit::OnCommand.
			MSG msg;
			//bool f = true;
			//while (f)
			//	f = ::PeekMessage(&msg, m_qcbed->Hwnd(), 0, 0, PM_REMOVE);
			if (::PeekMessage(&msg, m_qcbed->Hwnd(), 0, 0, PM_NOREMOVE))
			{
				HWND hwnd = ::WindowFromPoint(msg.pt);
				DecbButtonPtr qdb = dynamic_cast<DecbButton *>(AfWnd::GetAfWnd(hwnd));
				if (qdb)
					m_qcbed->m_fSkipBnClicked = true;
			}
			// Close down the combo list box.
			m_qcbed->m_qdef.Clear();
			m_qcbed.Clear();
			::SendMessage(m_qdecb->m_hwndButton, BM_SETSTATE, FALSE, 0);
			::PostMessage(m_hwnd, WM_CLOSE, 0, 0);
			return true;
		}
	}

	// Catches a new list item being selected by keyboard.
	// This is also called after a mouse up has already made the selection, so we want to
	// ignore this case. In this case, m_qcbed is already cleared.
	if (nc == LBN_SELCHANGE && m_qcbed)
	{
		// Set the edit box to the latest item from the listbox.
		m_qdecb->SetItem(::SendMessage(hctl, LB_GETCURSEL, 0, 0));
		// Select the text in the edit box.
		::SendMessage(m_qcbed->m_hwnd, EM_SETSEL, 0, -1);
		// Close down the combo list box unless the keyboard moved the selection.
		if (m_fKeyPressed)
			m_fKeyPressed = false;
		else
		{
			m_qcbed->m_qdef.Clear();
			m_qcbed.Clear();
			::SendMessage(m_qdecb->m_hwndButton, BM_SETSTATE, FALSE, 0);
			::PostMessage(m_hwnd, WM_CLOSE, 0, 0);
		}
		return true;
	}

	return SuperClass::OnCommand(cid, nc, hctl);
}


/*----------------------------------------------------------------------------------------------
	Process window messages for button. Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeComboBox::DecbFrame::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}
