/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeUni.cpp
Responsibility: Ken Zook
Last reviewed: never

Description:
	This class provides the base for all data entry field editors.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

const int kwidEditChild = 1; // An identifier for an edit box.


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDeFeUni::AfDeFeUni()
	: AfDeFieldEditor()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeFeUni::~AfDeFeUni()
{
}


/*----------------------------------------------------------------------------------------------
	Initialize font after superclass initialization is done.
----------------------------------------------------------------------------------------------*/
void AfDeFeUni::Init()
{
	Assert(m_hvoObj); // Initialize should have been called first.
	UpdateField(); // Get the initial value.
	CreateFont();
}


/*----------------------------------------------------------------------------------------------
Deletes the selected text in this this control.
----------------------------------------------------------------------------------------------*/
void AfDeFeUni::DeleteSelectedText()
{
}


/*----------------------------------------------------------------------------------------------
Returns true if there is selected text in this this control.
@return True if there is text selected.
----------------------------------------------------------------------------------------------*/
bool AfDeFeUni::IsTextSelected()
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
----------------------------------------------------------------------------------------------*/
void AfDeFeUni::Draw(HDC hdc, const Rect & rcpClip)
{
	Assert(hdc);

	SmartBstr sbstr;
	CheckHr(m_qtss->get_Text(&sbstr));

	AfGfx::FillSolidRect(hdc, rcpClip, m_chrp.clrBack);
	COLORREF clrBgOld = AfGfx::SetBkColor(hdc, m_chrp.clrBack);
	COLORREF clrFgOld = AfGfx::SetTextColor(hdc, m_chrp.clrFore);
	HFONT hfontOld = AfGdi::SelectObjectFont(hdc, m_hfont);
	::TextOutW(hdc, rcpClip.left + 2, rcpClip.top + 1, sbstr.Chars(), sbstr.Length());

	AfGdi::SelectObjectFont(hdc, hfontOld, AfGdi::OLD);
	AfGfx::SetBkColor(hdc, clrBgOld);
	AfGfx::SetTextColor(hdc, clrFgOld);
}


/*----------------------------------------------------------------------------------------------
	Check whether the content of this edit field has changed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeUni::IsDirty()
{
	ITsStringPtr qtssNew;
	::SendMessage(m_hwnd, FW_EM_GETTEXT, 0, (LPARAM)&qtssNew);

	// If the item has changed, save it to the data cache.
	SmartBstr sbstrOrig;
	SmartBstr sbstrNew;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	CheckHr(qcvd->get_UnicodeProp(m_hvoObj, m_flid, &sbstrOrig));
	CheckHr(qtssNew->get_Text(&sbstrNew));
	return !sbstrNew.Equals(sbstrOrig);
}


/*----------------------------------------------------------------------------------------------
	Make an edit box to allow editing. hwnd is the parent hwnd. rc is the size of the child
	window. Store the hwnd and return true.
----------------------------------------------------------------------------------------------*/
bool AfDeFeUni::BeginEdit(HWND hwnd, Rect &rc, int dxpCursor, bool fTopCursor,
	TptEditable tpte)
{
	if (!SuperClass::BeginEdit(hwnd, rc, dxpCursor, fTopCursor))
		return false;
	DeuEditPtr qme;
	qme.Create();
	qme->SetEditable(tpte);
	qme->m_pdeu = this;
	IActionHandler * pacth = BeginTempEdit();
	ILgWritingSystemFactoryPtr qwsf;
	GetLpInfo()->GetDbInfo()->GetLgWritingSystemFactory(&qwsf);
	AssertPtr(qwsf);
	qme->Create(hwnd, kwidEditChild, WS_CHILD | ES_LEFT | ES_AUTOHSCROLL, NULL, m_qtss,
		qwsf, m_ws, pacth);
	m_hwnd = qme->Hwnd();
	Rect rcT(rc.left + 2, rc.top + 1, rc.right, rc.bottom);
	::MoveWindow(m_hwnd, rcT.left, rcT.top, rcT.Width(), rcT.Height(), true);

	// Add text to the window.
	::SendMessage(m_hwnd, FW_EM_SETSTYLE, m_chrp.clrBack, (LPARAM)&m_qfsp->m_stuSty);
	::SendMessage(m_hwnd, EM_SETMARGINS, EC_RIGHTMARGIN | EC_LEFTMARGIN, MAKELPARAM(0, 0));
	::ShowWindow(m_hwnd, SW_SHOW);
	::SendMessage(m_hwnd, EM_SETSEL, 0, 0);
	// Foreground/background colors are set via WM_CTLCOLOREDIT in AfDeFeWnd.
	return true;
}


/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/
void AfDeFeUni::MoveWnd(const Rect & rcClip)
{
	::MoveWindow(m_hwnd, rcClip.left + 2, rcClip.top + 1, rcClip.Width(),
		rcClip.Height() - 1, true);
}

/*----------------------------------------------------------------------------------------------
	Save changes that have been made to the current editor.
----------------------------------------------------------------------------------------------*/
bool AfDeFeUni::SaveEdit()
{
	SmartBstr sbstrNew;
	CustViewDaPtr qcvd;
	ITsStringPtr qtssNew;

	EndTempEdit();
	if (!IsDirty())
		goto LFinish;

	// Save the changed value.
	::SendMessage(m_hwnd, FW_EM_GETTEXT, 0, (LPARAM)&qtssNew);
	m_qtss = qtssNew;

	// If the item has changed, save it to the data cache.
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	CheckHr(qtssNew->get_Text(&sbstrNew));
	// Check if the record has been edited by someone else since we first loaded the data.
	HRESULT hr;
	CheckHr(hr = qcvd->CheckTimeStamp(m_hvoObj));
	if (hr != S_OK)
	{
		// If it was changed and the user does not want to overwrite it, perform a refresh
		// so the displayed field will revert to it's original value.
		m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
		CheckHr(qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1));
		goto LFinish;
	}

	// Update the value in the cache and refresh views.
	BeginChangesToLabel();
	CheckHr(qcvd->SetUnicode(m_hvoObj, m_flid, const_cast<wchar *>(sbstrNew.Chars()),
		sbstrNew.Length()));
	CheckHr(qcvd->EndUndoTask());
	// Notify all windows of the change.
	m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
	CheckHr(qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1));
LFinish:
	// We need to leave in a state that cancels undo actions since this may be called
	// without actually closing the edit box.
	SaveFullCursorInfo();
	BeginTempEdit();
	return true;
}


/*----------------------------------------------------------------------------------------------
	Close the current editor, saving changes that were made. hwnd is the editor hwnd.
	@param fForce True if we want to force the editor closed without making any
		validity checks or saving any changes.
----------------------------------------------------------------------------------------------*/
void AfDeFeUni::EndEdit(bool fForce)
{
	SuperClass::EndEdit(fForce);
	if (!fForce)
	{
		if (!SaveEdit())
		{
			Assert(false); // Should have called IsOkToClose() first.
		}
	}
	EndTempEdit();
	// Get rid of the edit box.
	::DestroyWindow(m_hwnd);
	m_hwnd = 0;
}


/*----------------------------------------------------------------------------------------------
	Set the height for the specified width, and return it.
----------------------------------------------------------------------------------------------*/
int AfDeFeUni::SetHeightAt(int dxpWidth)
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
	This method saves the current cursor information in RecMainWnd. Normally it just
	stores the cursor index in RecMainWnd::m_ichCur. For structured texts, however,
	it also inserts the appropriate hvos and flids for the StText classes in
	m_vhvoPath and m_vflidPath. Other editors may need to do other things.
----------------------------------------------------------------------------------------------*/
void AfDeFeUni::SaveCursorInfo()
{
	// Store the current record/subrecord and field info.
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(m_qadsc->MainWindow());
	if (!prmw)
		return;
	// On BeginEdit we come in here before we have an edit box, so return 0.
	int ichAnchor = 0;
	int ichEnd = 0;
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
void AfDeFeUni::RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur)
{
	// Store the current record/subrecord and field info.
	::SendMessage(m_hwnd, EM_SETSEL, ichCur, ichCur);
}


/*----------------------------------------------------------------------------------------------
	Process window messages for edit box. Return true if processed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeUni::DeuEdit::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	// Catch keys to move to next/previous fields.
	if (wm == WM_CHAR)
	{
		bool fValid = m_pdeu->ValidKeyUp(wp);
		if (!fValid)
		{
			::PostMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)m_qtssOld.Ptr());
		}
	}

	if (wm == WM_KEYDOWN)
	{
		// If the key just went down then save the old value incase it is not a valid key.
		if (!(lp & 0x40000000))
			::SendMessage(m_hwnd, FW_EM_GETTEXT, 0, (LPARAM)&m_qtssOld);

		switch (wp)
		{
		case VK_TAB:
			if (::GetKeyState(VK_SHIFT) < 0)
				// Shift Tab to previous editor.
				m_pdeu->GetDeWnd()->OpenPreviousEditor(DxpCursorOffset(), true);
			else
				// Tab to next editor.
				m_pdeu->GetDeWnd()->OpenNextEditor(DxpCursorOffset());
			return true;

		case VK_LEFT:
		case VK_RIGHT:
			if (!OnKeyDown(wp, LOWORD(lp), HIWORD(lp)))
			{
				if (wp == VK_LEFT)
					m_pdeu->GetDeWnd()->OpenPreviousEditor(9999, false);
				else
					m_pdeu->GetDeWnd()->OpenNextEditor();
			}
			return true;

		case VK_UP:
			// Up arrow to previous editor.
			m_pdeu->GetDeWnd()->OpenPreviousEditor(DxpCursorOffset(), false);
			return true;

		case VK_DOWN:
			// Down arrow to next editor.
			m_pdeu->GetDeWnd()->OpenNextEditor(DxpCursorOffset());
			return true;

		case VK_PRIOR:
		case VK_NEXT:
			// Scroll the entire data entry window up or down one page.
			return m_pdeu->GetDeWnd()->ScrollKey(wp, lp);

		default:
			break;
		}
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Return the horizontal pixel offset of the cursor.
----------------------------------------------------------------------------------------------*/
int AfDeFeUni::DeuEdit::DxpCursorOffset()
{
	Assert(m_qrootb);

	int dxp;
	CheckHr(m_qrootb->get_XdPos(&dxp));
	return dxp;
}


/*----------------------------------------------------------------------------------------------
	Called immediately after it is created before other events happen.
----------------------------------------------------------------------------------------------*/
void AfDeFeUni::DeuEdit::PostAttach(void)
{
	//::mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
	//::SetFocus(m_hwnd);
	//::SetCapture(m_hwnd);
}


/*----------------------------------------------------------------------------------------------
	The field has changed, so make sure it is updated.
----------------------------------------------------------------------------------------------*/
void AfDeFeUni::UpdateField()
{
	// Get the data from the cache and update the string.
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	SmartBstr sbstr;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	CheckHr(qcvd->get_UnicodeProp(m_hvoObj, m_flid, &sbstr));
	qtsf->MakeStringRgch(sbstr.Chars(), sbstr.Length(), m_ws, &m_qtss);

	// If we have an edit box, update the contents.
	if (m_hwnd)
		::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)m_qtss.Ptr());
}


/*----------------------------------------------------------------------------------------------
	Check the requirments of the FldSpec, and verify that data in the field meets the
	requirement. It returns:
		kFTReqNotReq if the all requirements are met.
		kFTReqWs if data is missing, but it is encouraged.
		kFTReqReq if data is missing, but it is required.
----------------------------------------------------------------------------------------------*/
FldReq AfDeFeUni::HasRequiredData()
{
	if (m_qfsp->m_fRequired == kFTReqNotReq)
		return kFTReqNotReq;
	int cch = 0;
	if (m_qtss)
		m_qtss->get_Length(&cch);
	if (!cch)
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
IActionHandler * AfDeFeUni::BeginTempEdit()
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
IActionHandler * AfDeFeUni::EndTempEdit()
{
	// Get your action handler.
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	IActionHandlerPtr qacth;
	CheckHr(qcvd->GetActionHandler(&qacth));
	// Clear out any temporary Undo items relating to this window.
	if (m_hMark)
	{
		CheckHr(qacth->DiscardToMark(0));
		m_hMark = 0;
	}
	return qacth;
}
