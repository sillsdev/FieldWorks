/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeInt.cpp
Responsibility: Ken Zook
Last reviewed: never

Description:
	This defines a field editor for integers.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

const int kwidEditChild = 1; // An identifier for an edit box.


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfDeFeInt::AfDeFeInt()
	: AfDeFeUni()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfDeFeInt::~AfDeFeInt()
{
}


/*----------------------------------------------------------------------------------------------
	Check whether the content of this edit field has changed.
----------------------------------------------------------------------------------------------*/
bool AfDeFeInt::IsDirty()
{
	if (!m_hwnd)
		return false; // Editor not active.
	ITsStringPtr qtss;
	::SendMessage(m_hwnd, FW_EM_GETTEXT, 0, (LPARAM)&qtss);
	m_qtss = qtss;

	int nOrig;
	int nNew;
	StrUni stu;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	CheckHr(qcvd->get_IntProp(m_hvoObj, m_flid, &nOrig));
	const wchar * pwrgch;
	int cch;
	if (SUCCEEDED(qtss->LockText(&pwrgch, &cch)))
	{
		stu.Assign(pwrgch, cch);
		qtss->UnlockText(pwrgch);
	}
	nNew = StrUtil::ParseInt(stu.Chars());

	return nNew != nOrig;
}


/*----------------------------------------------------------------------------------------------
	Save changes that have been made to the current editor.
----------------------------------------------------------------------------------------------*/
bool AfDeFeInt::SaveEdit()
{
	EndTempEdit();

	// Save the changed value.
	ITsStringPtr qtss;
	::SendMessage(m_hwnd, FW_EM_GETTEXT, 0, (LPARAM)&qtss);
	m_qtss = qtss;

	// If the item has changed, save it to the data cache.
	int nOrig;
	int nNew;
	StrUni stu;
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	CheckHr(qcvd->get_IntProp(m_hvoObj, m_flid, &nOrig));
	const wchar * pwrgch;
	int cch;
	StrAnsi sta;
	if (SUCCEEDED(qtss->LockText(&pwrgch, &cch)))
	{
		stu.Assign(pwrgch, cch);
		sta.Assign(pwrgch, cch);
		qtss->UnlockText(pwrgch);
	}
	char   *pstopstring;
	nNew = strtol(sta.Chars(), &pstopstring, 10);
	Assert(isizeof(int) == isizeof(long));
	if ((nNew == INT_MAX) || (nNew == INT_MIN))
	{
		StrApp strM(kstidMaxIntMsg);
		StrApp strT(kstidMaxIntTitle);
		::MessageBox(m_hwnd, strM.Chars(), strT.Chars(), MB_OK | MB_ICONWARNING);
	}

	// if the value changed or the lenght of the sting changed then ...
	if (nNew != nOrig || sta.Chars() + sta.Length() != pstopstring)
	{
		// Check if the record has been edited by someone else since we first loaded the data.
		HRESULT hrTemp;
		if ((hrTemp = qcvd->CheckTimeStamp(m_hvoObj)) != S_OK)
		{
			// If it was changed and the user does not want to overwrite it, perform a refresh
			// so the displayed field will revert to it's original value.
			m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
			qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1);
			goto LFinish;
		}

		// Update the value in the cache and refresh views.
		BeginChangesToLabel();
		qcvd->SetInt(m_hvoObj, m_flid, nNew);
		// Notify all windows of the change.
		m_qadsc->UpdateAllDEWindows(m_hvoObj, m_flid);
		qcvd->PropChanged(NULL, kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 1);
		CheckHr(qcvd->EndUndoTask());
	}
LFinish:
	// We need to leave in a state that cancels undo actions since this may be called
	// without actually closing the edit box.
	BeginTempEdit();
	return true;
}


/*----------------------------------------------------------------------------------------------
	The field has changed, so make sure it is updated.
----------------------------------------------------------------------------------------------*/
void AfDeFeInt::UpdateField()
{
	// Get the integer from the cache and update the string.
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	CustViewDaPtr qcvd;
	GetDataAccess(&qcvd);
	AssertPtr(qcvd);
	int n;
	CheckHr(qcvd->get_IntProp(m_hvoObj, m_flid, &n));
	StrUni stu;
	stu.Format(L"%d", n);
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_ws, &m_qtss);

	// If we have an edit box, update the contents.
	if (m_hwnd)
		::SendMessage(m_hwnd, FW_EM_SETTEXT, 0, (LPARAM)m_qtss.Ptr());
}


/*----------------------------------------------------------------------------------------------
	Checks for valid keys that are pressed within the integer field.

----------------------------------------------------------------------------------------------*/
bool AfDeFeInt::ValidKeyUp(UINT wp)
{
	// If keyup is 0-9 or backspace or tab or '-' then it is valid.
	if (((wp > 0x2F) && (wp < 0x3A)) || wp == 0x08 || wp == 0x09 || wp == 0x2D)
		return true;
	// Otherwise it is NOT valid.
	return false;
}
