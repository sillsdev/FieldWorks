/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeCliRef.cpp
Responsibility: Ken Zook
Last reviewed: never

Description:
	This class provides the base for all data entry field editors.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

#undef DEBUG_THIS_FILE
//#define DEBUG_THIS_FILE 1

//:>********************************************************************************************
//:>	AfDeFeCliRef methods.
//:>********************************************************************************************


AfDeFeCliRef::AfDeFeCliRef()
{
}


AfDeFeCliRef::~AfDeFeCliRef()
{
}


/*----------------------------------------------------------------------------------------------
	Relase all smart pointers.
----------------------------------------------------------------------------------------------*/
void AfDeFeCliRef::OnReleasePtr()
{
	PossListInfoPtr qpli;
	if (GetLpInfo()->GetPossList(m_hvoPssl, m_wsMagic, &qpli))
	{
		AssertPtr(qpli);
		qpli->RemoveNotify(this);
	}
	SuperClass::OnReleasePtr();
}


/*----------------------------------------------------------------------------------------------
	Make an edit box to allow editing. hwnd is the parent hwnd. rc is the size of the child
	window. Store the hwnd and return true.
	@param hwnd
	@param rc
	@param dxpCursor
	@param fTopCursor
	@param tpte
	@return true if successful
----------------------------------------------------------------------------------------------*/
bool AfDeFeCliRef::BeginEdit(HWND hwnd, Rect &rc, int dxpCursor, bool fTopCursor,
	TptEditable tpte)
{
//	m_fRecurse = true;
	if (!SuperClass::BeginEdit(hwnd, rc, dxpCursor, fTopCursor, ktptSemiEditable))
		return false;
	// When we tab into the field, we want everything to highlight to make it easy to change.
	::SendMessage(m_hwnd, EM_SETSEL, 0, -1);
	return true;
}


/*----------------------------------------------------------------------------------------------
	@return true if any changes have been made.
----------------------------------------------------------------------------------------------*/
bool AfDeFeCliRef::IsDirty()
{
	if (!m_hwnd)
		return false; // Editor not active.
	HVO hvoOrig;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	CheckHr(qcvd->get_ObjectProp(m_hvoObj, m_flid, &hvoOrig));
	return hvoOrig != m_pss;
}


/*----------------------------------------------------------------------------------------------
	Save changes that have been made to the current editor.
	@return true if edit saved successfully
----------------------------------------------------------------------------------------------*/
bool AfDeFeCliRef::SaveEdit()
{
	if (m_fDelFromDialog)
	{
		m_fDelFromDialog = false;
		return true;
	}
	EndTempEdit();
	if (IsDirty())
	{
		CustViewDaPtr qcvd;
		GetDataAccess(&qcvd);
		AssertPtr(qcvd);
		// Check if the record has been edited by someone else since we first loaded the data.
		HRESULT hr;
		CheckHr(hr = qcvd->CheckTimeStamp(m_hvoObj));
		if (hr != S_OK)
		{
			// If it was changed and the user does not want to overwrite it, perform a refresh
			// so the displayed field will revert to it's original value.
			m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
			qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1);
		}
		else
		{
			// Update the value in the cache and refresh views.
			BeginChangesToLabel();
			qcvd->SetObjProp(m_hvoObj, m_flid, m_pss);
			m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
			qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1);
			CheckHr(qcvd->EndUndoTask());
		}
	}
	// We need to leave in a state that cancels undo actions since this may be called
	// without actually closing the edit box.
	BeginTempEdit();
	return true;
}


/*----------------------------------------------------------------------------------------------
	This initializes the string based after the pssl and pss have been set. It takes into
	account whether the view is hierarchical or not.
	@param fHier
	@param pnt
----------------------------------------------------------------------------------------------*/
void AfDeFeCliRef::InitContents(bool fHier, PossNameType pnt)
{
	Assert(m_hvoPssl); // This must be set prior to calling this method.
	Assert(pnt < kpntLim);

	SuperClass::Init(); // Initialize the superclass.

	m_fHier = fHier;
	m_pnt = pnt;

	if (!m_pss)
		return; // If the reference isn't set, we don't have anything to display.

	ITsStringPtr qtss;
	ITsStrFactoryPtr qtsf;
	StrUni stu;
	PossListInfoPtr qpli;
	PossItemInfo * ppii;
	int ipss;

	qtsf.CreateInstance(CLSID_TsStrFactory);
	GetLpInfo()->LoadPossList(m_hvoPssl, m_wsMagic, &qpli);
	AssertPtr(qpli);
	ipss = qpli->GetIndexFromId(m_pss);
	ppii = qpli->GetPssFromIndex(ipss);
	AssertPtr(ppii);
	if (m_fHier)
		ppii->GetHierName(qpli, stu, m_pnt);
	else
		ppii->GetName(stu, m_pnt);
	int ws = ppii->GetWs();
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, &qtss);
	m_qtss = qtss;
	qpli->AddNotify(this);
}


/*----------------------------------------------------------------------------------------------
	Refresh the field from the data cache.
----------------------------------------------------------------------------------------------*/
void AfDeFeCliRef::UpdateField()
{
	// Get the item info from the cache.
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	HVO hvoPss;
	CheckHr(qcvd->get_ObjectProp(m_hvoObj, m_flid, &hvoPss));
	m_pss = hvoPss;

	// Get the string from the poss cache.
	ITsStringPtr qtss;
	ITsStrFactoryPtr qtsf;
	StrUni stu;
	PossListInfoPtr qpli;
	PossItemInfo * ppii;
	int ipss;

	qtsf.CreateInstance(CLSID_TsStrFactory);
	int ws = m_ws;
	if (m_pss)
	{
		GetLpInfo()->LoadPossList(m_hvoPssl, m_wsMagic, &qpli);
		AssertPtr(qpli);
		ipss = qpli->GetIndexFromId(m_pss);
		if (ipss >= 0)
		{
			ppii = qpli->GetPssFromIndex(ipss);
			AssertPtr(ppii);
			if (m_fHier)
				ppii->GetHierName(qpli, stu, m_pnt);
			else
				ppii->GetName(stu, m_pnt);
			ws = ppii->GetWs();
		}
	}
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, &qtss);
	m_qtss = qtss;

	// If we have an edit box, update the contents.
	if (m_hwnd)
	{
		// Setting the property causes a recursive call to OnChange, so we want to block it
		// from making a further change. (e.g., when you backspace to set the string to null
		// then tab to the next field, we want it to stay null, not go to the first item in
		// the list.)
		m_fRecurse = true;
		::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)m_qtss.Ptr());
	}
}


/*----------------------------------------------------------------------------------------------
	Process commands.
----------------------------------------------------------------------------------------------*/
void AfDeFeCliRef::ProcessChooser()
{
	// The button was clicked. We need to open the possibility chooser and
	// get input from the user.
	PossChsrDlgPtr qplc;
	qplc.Create();
	qplc->SetDialogValues(m_hvoPssl, m_wsMagic, m_pss);
	// Since this field editor may be deleted while the list chooser is up (e.g., sync), we
	// launch the chooser here and request it to call us back with ChooserApplied to finish
	// the processing. Nothing of any significance should be done in this method after
	// launching the dialog.
	qplc->SetFromEditor(true);
	qplc->DoModal(m_hwnd);
}


/*----------------------------------------------------------------------------------------------
	The Ok button in the list chooser has been hit. Process the results from the list chooser.
	@param pplc Pointer to the dialog box being closed.
	----------------------------------------------------------------------------------------------*/
void AfDeFeCliRef::ChooserApplied(PossChsrDlg * pplc)
{
	// Get the output values.
	StrUni stu;
	ITsStringPtr qtss;
	ITsStrFactoryPtr qtsf;
	HVO pssId;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	pplc->GetDialogValues(pssId);
	m_pss = pssId;
	m_fRecurse = true;
	if (pssId)
	{
		PossListInfoPtr qpli;
		AfLpInfo * plpi = GetLpInfo();
		AssertPtr(plpi);
		plpi->LoadPossList(m_hvoPssl, m_wsMagic, &qpli);
		AssertPtr(qpli);
		int ipss = qpli->GetIndexFromId(pssId);
		PossItemInfo * ppii = qpli->GetPssFromIndex(ipss);
		if (m_fHier)
			ppii->GetHierName(qpli, stu, m_pnt);
		else
			ppii->GetName(stu, m_pnt);
		int ws = ppii->GetWs();
		qtsf->MakeStringRgch(stu.Chars(), stu.Length(), ws, &qtss);
		m_qtss = qtss;
		::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)qtss.Ptr());
	}
	else
	{
		m_qtss = NULL;
		::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, NULL);
	}
	// I (KenZ) don't fully understand this. But if a user enters the chooser, and opens a
	// list editor from there and adds a new item, closes the list editor, checks the new
	// item in the chooser, selects OK, then moves to the next record without moving from
	// the field, the added item is lost. We get ksyncPossList and ksyncAddPss sync messages
	// from the list editor, but for some reason we are getting an extra ksyncPossList
	// message after this method completes. That extra message is calling ListChanged which
	// calls UpdateField, which reloads our temporary cache from the main cache and wipes out
	// the change we just made. So until we can do something better, we'll save the changes
	// here to make sure the UpdateField doesn't wipe out our change.
	SaveEdit();
	::InvalidateRect(m_hwnd, NULL, true);
}


/*----------------------------------------------------------------------------------------------
	The edit box changed. We need to validate what was done.
	@param pedit
	@return
----------------------------------------------------------------------------------------------*/
bool AfDeFeCliRef::OnChange(AfDeFeEdBoxBut::DeEdit * pedit)
{
	if (m_fRecurse)
	{
		m_fRecurse = false;
		return true;
	}
	if (!m_hwnd)
		return true; // We aren't completely set up yet, so ignore this.

	// Get the characters from the edit box.
	ITsStringPtr qtss;
	::SendMessage(m_hwnd, FW_EM_GETTEXT, 0, (LPARAM)&qtss);

	int ichMin;
	int ichLim;
	::SendMessage(m_hwnd, EM_GETSEL, (WPARAM)&ichMin, (LPARAM)&ichLim);

	int cchTyped; // number of characters in the typed string
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
		// Note: This recursively calls this procedure.
		::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)qtss.Ptr());
		::SendMessage(m_hwnd, EM_SETSEL, ichMin, ichMin);
	}

	stuTyped.SetSize(cchTyped, &pchBuf);
	qtss->FetchChars(0, cchTyped, pchBuf);

#ifdef DEBUG_THIS_FILE
	StrAnsi sta;
	sta.Format("AfDeFeCliRef::OnChange:  pedit->m_ch=%d; ichMin=%d; ichLim=%d; pedit->m_cchMatched=%d; cchTyped=%d.\n",
										 pedit->m_ch,    ichMin,    ichLim,    pedit->m_cchMatched,    cchTyped);
	OutputDebugString(sta.Chars());
#endif

	bool fTypeAhead = false; // allow type ahead only when adding characters at end of current item
							// or backspacing at end of current item.
	bool fNeedCompare;
	if (pedit->m_ch == 0) // (see kcidEditPaste special code)
	{
		// If we pasted something, force a compare.
		fNeedCompare = true;
	}
	else if (ichMin == pedit->m_cchMatched + 1 && pedit->m_ch != VK_BACK && pedit->m_ch != VK_DELETE)
	{
		// Need to compare if we typed a character and we are one greater than last match.
		fNeedCompare = true;
		if (cchTyped == ichMin)
		{
			fTypeAhead = true;
#ifdef DEBUG_THIS_FILE
			sta.Format("OnChange: 1 - setting fTypeAhead to true.\n");
			OutputDebugString(sta.Chars());
#endif
		}
	}
	else if (ichMin > pedit->m_cchMatched)
	{
		// Don't compare any other time we are past the last match.
		fNeedCompare = false;
	}
//	else if ((cchTyped == ichMin) && (ichMin == ichLim) && (pedit->m_ch != kscDelForward))
	else if ((cchTyped == ichMin) && (ichMin == ichLim) && (pedit->m_ch != 46))
	{
		// Need to compare if we typed a character and we are at the end of the item
		// Need to compare if we delete the last character in the non-type-ahead string
		fNeedCompare = true;
		fTypeAhead = true;
#ifdef DEBUG_THIS_FILE
		sta.Format("OnChange:  kscDelForward=%d.\n");
		OutputDebugString(sta.Chars());
		sta.Format("OnChange: 2 - setting fTypeAhead to true.\n");
		OutputDebugString(sta.Chars());
#endif
	}
	else
	{
		// Always compare if we are not past the last match.
		fNeedCompare = true;
	}

	int cch;

	// Since the edit box deletes the selection on backspace, we need to use
	// some extra logic to make backspace actually move the selection back.
	if (pedit->m_cchMatched && pedit->m_ch == VK_BACK && m_pss)
	{
		// If we had a previous match and we got a backspace, we always decrement the matched
		// characters and start looking at that point.
		cch = --pedit->m_cchMatched;
	}
	else
	{
		// Otherwise we start looking at the cursor location.
		cch = ichMin;
	}

	AfLpInfo * plpi = GetLpInfo();
	AssertPtr(plpi);

	PossListInfoPtr qpli;
	plpi->LoadPossList(m_hvoPssl, m_wsMagic, &qpli);
	AssertPtr(qpli);

	PossItemInfo * ppii = NULL;
	ComBool fExactMatch = false;

	fNeedCompare = true;

	if (cchTyped == 0)
	{
		// If nothing to match, get the first item in the possibility list. If we are
		// already at that item, remove the item. If we are already cleared, do nothing.
		if (!m_pss)
			return true;
		// If everything is highlighted, we want to clear the item with Del or Bsp.
		// But when we are backspacing, if there is only one character left and we backspace
		// over that, we want to switch to the first item in the list.
		ppii = qpli->GetPssFromIndex(0);
		if (ichMin != 1)
		{
			m_pss = 0;
			m_qtss = NULL;
			pedit->m_cchMatched = 0; // Keep cursor at beginning of item.
			m_fRecurse = true;
			::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)m_qtss.Ptr());
			return true;
		}
	}
	else if (fNeedCompare)
	{
		// Try to find an item that matches what the user typed in the possibility list.
		StrUni stuMatch(stuTyped);
/////		stuMatch.Replace(cch, stuMatch.Length(), L"");// Delete chars to right of cursor.
		Locale loc = GetLpInfo()->GetLocale(m_ws);
		if (m_fHier)
		{
			ppii = qpli->FindPssHier(stuMatch.Chars(), loc, m_pnt, fExactMatch);
		}
		else
		{
			ppii = qpli->FindPss(stuMatch.Chars(), loc, m_pnt);
		}

		if (ppii)
		{
			// found a match that starts with stuTyped
			int ipssTemp;

			// TODO TimP:  check for hierarchy.  If stuTyped contains hierarchy,

			// Was the match exact (rather than just starting with stuTyped) ?
			if (fExactMatch) // May have matched in the FindPssHier() call above.
			{
#ifdef DEBUG_THIS_FILE
				sta.Format("OnChange:  Exact match (hier).\n");
				OutputDebugString(sta.Chars());
#endif
			}
			else
			{
				fExactMatch = ! qpli->PossUniqueName(-1, stuTyped, m_pnt, ipssTemp);
				if (fExactMatch)
				{
#ifdef DEBUG_THIS_FILE
					sta.Format("OnChange:  Exact match.\n");
					OutputDebugString(sta.Chars());
#endif
					// in case FindPss() above matches "ABC" but "AB" is also in list.
					ppii = qpli->GetPssFromIndex(ipssTemp);
				}
				else
				{
#ifdef DEBUG_THIS_FILE
					sta.Format("OnChange:  Not exact match.\n");
					OutputDebugString(sta.Chars());
#endif
				}
			}
		}
	}
	else
		ppii = NULL;

	StrUni stuFound;
	int ws = m_ws;
	if (ppii && (fTypeAhead || fExactMatch))
	{
		// If found, process the new item.
		int pss = ppii->GetPssId();
		m_pss = pss;
		if (m_fHier)
			ppii->GetHierName(qpli, stuFound, m_pnt);
		else
			ppii->GetName(stuFound, m_pnt);
		ws = ppii->GetWs();

		// If the last character was a delimiter, we need to set cch accordingly.
		if (m_fHier && (stuTyped.Length() > 0) && (pedit->m_ch != VK_BACK) &&
			(stuTyped.GetAt(stuTyped.Length() - 1) == kchHierDelim))
		{
			// Need to set cch to the position of the last delimiter.
			cch = stuFound.FindCh(kchHierDelim, cch - 1) + 1;
		}

		pedit->m_cchMatched = cch;
	}
	else
	{
		// Something illegal was typed. Assume they are adding a new item.
		if (pedit->m_cchMatched + 1 == cch && pedit->m_ch != VK_BACK)
			::MessageBeep(MB_ICONEXCLAMATION); // Beep on the first unmatched character.
		// Underline the string with a red squiggly.
		ITsIncStrBldrPtr qtisb;
		qtisb.CreateInstance(CLSID_TsIncStrBldr);
		qtisb->SetIntPropValues(ktptWs, ktpvDefault, m_ws);
		CheckHr(qtisb->SetIntPropValues(ktptUnderColor, ktpvDefault, kclrRed));
		CheckHr(qtisb->SetIntPropValues(ktptUnderline, ktpvEnum, kuntSquiggle));
		qtisb->AppendRgch(stuTyped.Chars(), stuTyped.Length());
		qtisb->GetString(&m_qtss);
		m_fRecurse = true; // Stop the recursion caused by the next instruction.
		// Note: This recursively calls this procedure.
		::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)m_qtss.Ptr());
		::SendMessage(m_hwnd, EM_SETSEL, ichMin, ichMin);
		m_pss = 0; // We no longer have a matched HVO.
		return true;
	}

	// Update the edit box text and selection.
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	qtsf->MakeStringRgch(stuFound.Chars(), stuFound.Length(), ws, &qtss);
	m_qtss = qtss;

#ifdef DEBUG_THIS_FILE
	sta.Format("AfDeFeCliRef::OnChange:  pedit->m_cchMatched=%d; stuFound.Length()=%d; ichMin=%d; ichLim=%d.\n",
										 pedit->m_cchMatched,    stuFound.Length(),    ichMin,    ichLim);
	OutputDebugString(sta.Chars());
#endif

	m_fRecurse = true; // Shortcut the recursion caused by the next instruction.
	if (fTypeAhead)
	{
		// Note: This recursively calls this procedure.
		::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)qtss.Ptr());
		::SendMessage(m_hwnd, EM_SETSEL, pedit->m_cchMatched, stuFound.Length());
#ifdef DEBUG_THIS_FILE
	sta.Format("AfDeFeCliRef::OnChange:  type ahead.\n");
	OutputDebugString(sta.Chars());
#endif
	}
	else
	{
		// Note: This recursively calls this procedure.
		::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)qtss.Ptr());
		::SendMessage(m_hwnd, EM_SETSEL, ichMin, ichMin);
#ifdef DEBUG_THIS_FILE
	sta.Format("AfDeFeCliRef::OnChange:  NOT type ahead.\n");
	OutputDebugString(sta.Chars());
#endif
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Close the current editor, saving changes that were made. hwnd is the editor hwnd.
	@param fForce True if we want to force the editor closed without making any
		validity checks or saving any changes.
----------------------------------------------------------------------------------------------*/
void AfDeFeCliRef::EndEdit(bool fForce)
{
	if (!fForce)
	{
		if (!m_pss)
		{
			// The user added a new item.
			ITsStringPtr qtss;
			int cch;
			StrUni stu; // Name/Abbr string entered by user.
			OLECHAR * pchBuf;

			// Get the characters from the edit box.
			::SendMessage(m_hwnd, FW_EM_GETTEXT, 0, (LPARAM)&qtss);
			qtss->get_Length(&cch);
			stu.SetSize(cch, &pchBuf);
			qtss->FetchChars(0, cch, pchBuf);

			if (cch)
			{
				// If a non-null string is present, create a new item.
				m_fSaving = true; // Disable updates while adding a new item.
				// The user created a new item, so add it to the possibility list.
				m_pss = CreatePss(m_hvoPssl, stu.Chars(), m_pnt, m_fHier);
				Assert(m_pss); // What should we do if we have an error?
				m_fSaving = false;
			}
		}
	}
	// When we close the edit window, the squiggley will automatically be removed.
	SuperClass::EndEdit(fForce);
}

/*----------------------------------------------------------------------------------------------
	Something has changed in the possibility list.
----------------------------------------------------------------------------------------------*/
void AfDeFeCliRef::ListChanged(int nAction, HVO hvoPssl, HVO hvoSrc, HVO hvoDst, int ipssSrc,
	int ipssDst)
{
	// We don't want to update the field while we are in the middle of saving.
	if (!m_fSaving)
		UpdateField();
}