/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfProgressDlg.cpp
Responsibility: Steve McConnel
Last reviewed: never.

Description:
	Implementation of AfProgressDlg, a simple modeless dialog class for supporting progress
	messages and a progress bar.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	AfProgressDlg Implementation.
//:>********************************************************************************************

static DummyFactory g_fact(_T("SIL.AppCore.AfProgressDlg"));

/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  IUnknown, IAdvInd, IAdvInd3, and
	ISupportErrorInfo are supported.

	@param riid Reference to the desired interface GUID.
	@param ppv Address of a pointer for returning the desired interface pointer.

	@return S_OK, E_POINTER, or E_NOINTERFACE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfProgressDlg::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IAdvInd4 *>(this));
	else if (riid == IID_IAdvInd)
		*ppv = static_cast<IAdvInd4 *>(this);
	else if (riid == IID_IAdvInd3)
		*ppv = static_cast<IAdvInd3 *>(this);
	else if (riid == IID_IAdvInd4)
		*ppv = static_cast<IAdvInd4 *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(
			static_cast<IUnknown *>(static_cast<IAdvInd4 *>(this)), IID_IAdvInd);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Increment the reference count.

	@return The updated reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) AfProgressDlg::AddRef()
{
	return SuperClass::AddRef();
}

/*----------------------------------------------------------------------------------------------
	Decrement the reference count.

	@return The updated reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(ULONG) AfProgressDlg::Release()
{
	return SuperClass::Release();
}

/*----------------------------------------------------------------------------------------------
	Advance the progress bar indicator by calling StepProgressBar with the given amount.

	@param nStepAmt Amount by which to advance the progress bar display, relative to the defined
					end points of the progress bar.

	@return S_OK.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfProgressDlg::Step(int nStepAmt)
{
	BEGIN_COM_METHOD;

	StepIt(nStepAmt);

	END_COM_METHOD(g_fact, IID_IAdvInd);
}

/*----------------------------------------------------------------------------------------------
	Set the window title for the progress dialog.

	@param bstrTitle Contains the title string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfProgressDlg::put_Title(BSTR bstrTitle)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrTitle);

	StrApp str(bstrTitle);
	::SetWindowText(m_hwnd, str.Chars());
	CheckCancel();

	END_COM_METHOD(g_fact, IID_IAdvInd);
}

/*----------------------------------------------------------------------------------------------
	Set the message for the progress dialog.

	@param bstrMessage Contains the message string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfProgressDlg::put_Message(BSTR bstrMessage)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrMessage);

	StrApp str(bstrMessage);
	::SetWindowText(::GetDlgItem(m_hwnd, kctidProgressMessage), str.Chars());
	CheckCancel();

	END_COM_METHOD(g_fact, IID_IAdvInd);
}

/*----------------------------------------------------------------------------------------------
	Send the PBM_SETPOS message to the progress bar window.

	@param nNewPos New position for the progress bar.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfProgressDlg::put_Position(int nPos)
{
	BEGIN_COM_METHOD;

	if (!m_hwndProgress)
		ThrowHr(E_UNEXPECTED);
	if (nPos < m_nLowLim)
		nPos = m_nLowLim;
	if (nPos > m_nHighLim)
		nPos = m_nHighLim;
	::SendMessage(m_hwndProgress, PBM_SETPOS, nPos, 0);
	m_nCurrent = nPos;
	CheckCancel();

	END_COM_METHOD(g_fact, IID_IAdvInd);
}

/*----------------------------------------------------------------------------------------------
	Send the PBM_SETSTEP message to the progress bar window.

	@param nStepInc Default amount by which to advance the progress bar.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfProgressDlg::put_StepSize(int nStepInc)
{
	BEGIN_COM_METHOD;

	if (nStepInc > m_nHighLim - m_nLowLim)
		ThrowHr(E_INVALIDARG);
	if (!m_hwndProgress)
		ThrowHr(E_UNEXPECTED);
	::SendMessage(m_hwndProgress, PBM_SETSTEP, nStepInc, 0);
	m_nStep = nStepInc;
	CheckCancel();

	END_COM_METHOD(g_fact, IID_IAdvInd);
}

/*----------------------------------------------------------------------------------------------
	Send the PBM_SETRANGE32 message to the progress bar window.

	@param nMin Lower limit of possible values for the progress bar (typically 0).
	@param nMax Upper limit of possible values for the progress bar.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfProgressDlg::SetRange(int nMin, int nMax)
{
	BEGIN_COM_METHOD;

	if (nMin >= nMax)
		ThrowHr(E_INVALIDARG);
	if (m_hwndProgress)
		::SendMessage(m_hwndProgress, PBM_SETRANGE32, nMin, nMax);
	m_nLowLim = nMin;
	m_nHighLim = nMax;
	m_nCurrent = nMin;
	CheckCancel();

	END_COM_METHOD(g_fact, IID_IAdvInd);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the title of the progress display window.

	@param pbstrTitle Pointer to the title for output.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfProgressDlg::get_Title(BSTR * pbstrTitle)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrTitle);

	achar rgch[1000];
	::GetWindowText(m_hwnd, rgch, sizeof(rgch));
	SmartBstr sbstr(rgch);
	*pbstrTitle = sbstr.Detach();
	CheckCancel();

	END_COM_METHOD(g_fact, IID_IAdvInd);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the message within the progress display window.

	@param pbstrMessage Pointer to the message for output.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfProgressDlg::get_Message(BSTR * pbstrMessage)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrMessage);

	achar rgch[1000];
	::GetWindowText(::GetDlgItem(m_hwnd, kctidProgressMessage), rgch, sizeof(rgch));
	SmartBstr sbstr(rgch);
	*pbstrMessage = sbstr.Detach();
	CheckCancel();

	END_COM_METHOD(g_fact, IID_IAdvInd);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the current position of the progress bar.

	@param pnPos Pointer to the message for output.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfProgressDlg::get_Position(int * pnPos)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnPos);

	*pnPos = m_nCurrent;
	CheckCancel();

	END_COM_METHOD(g_fact, IID_IAdvInd);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the size of the step increment used by Step().

	@param pnStepInc Pointer to lower limit of possible values for the progress bar.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfProgressDlg::get_StepSize(int * pnStepInc)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnStepInc);

	*pnStepInc = m_nStep;
	CheckCancel();

	END_COM_METHOD(g_fact, IID_IAdvInd);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the minimum and maximum values of the progress bar.

	@param pnMin Pointer to lower limit of possible values for the progress bar.
	@param pnMax Pointer to upper limit of possible values for the progress bar.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP AfProgressDlg::GetRange(int * pnMin, int * pnMax)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnMin);
	ChkComOutPtr(pnMax);

	*pnMin = m_nLowLim;
	*pnMax = m_nHighLim;
	CheckCancel();

	END_COM_METHOD(g_fact, IID_IAdvInd);
}


/*----------------------------------------------------------------------------------------------
	Set the window title for the progress dialog.

	@param pszTitle Points to the title string.
----------------------------------------------------------------------------------------------*/
void AfProgressDlg::SetTitle(const achar * pszTitle)
{
	::SetWindowText(m_hwnd, pszTitle);
	CheckCancel();
}

/*----------------------------------------------------------------------------------------------
	Set the message for the progress dialog.

	@param pszMsg Points to the message string.
----------------------------------------------------------------------------------------------*/
void AfProgressDlg::SetMessage(const achar * pszMsg)
{
	::SetWindowText(::GetDlgItem(m_hwnd, kctidProgressMessage), pszMsg);
	CheckCancel();
}

/*----------------------------------------------------------------------------------------------
	Send the PBM_SETBARCOLOR and PBM_SETBKCOLOR messages to the progress bar window.

	@param clrBar Color of the progress bar.
	@param clrBk Background color of the pane in which the progress bar is displayed.
----------------------------------------------------------------------------------------------*/
void AfProgressDlg::SetColors(COLORREF clrBar, COLORREF clrBk)
{
	Assert(m_hwndProgress);
	::SendMessage(m_hwndProgress, PBM_SETBARCOLOR, 0, (LPARAM)clrBar);
	::SendMessage(m_hwndProgress, PBM_SETBKCOLOR, 0, (LPARAM)clrBk);
	CheckCancel();
}

/*----------------------------------------------------------------------------------------------
//-	Send the PBM_SETRANGE32 message to the progress bar window.
//-
//-	@param nLowLim Lower limit of possible values for the progress bar (typically 0).
//-	@param nHighLim Upper limit of possible values for the progress bar.
----------------------------------------------------------------------------------------------*/
//-void AfProgressDlg::SetRange(int nLowLim, int nHighLim)
//-{
//-	Assert(nLowLim < nHighLim);
//-	Assert(m_hwndProgress);
//-	::SendMessage(m_hwndProgress, PBM_SETRANGE32, nLowLim, nHighLim);
//-	m_nLowLim = nLowLim;
//-	m_nHighLim = nHighLim;
//-	m_nCurrent = nLowLim;
//-}

/*----------------------------------------------------------------------------------------------
	Send either the PBM_STEPIT or the PBM_DELTAPOS message to the progress bar control.

	@param nIncrement Amount by which to advance the progress bar.
----------------------------------------------------------------------------------------------*/
void AfProgressDlg::StepIt(int nIncrement)
{
	if (nIncrement)
		m_nCurrent += nIncrement;
	else
		m_nCurrent += m_nStep;
	if (m_nCurrent > m_nHighLim)
		m_nCurrent = m_nLowLim + m_nCurrent % (m_nHighLim - m_nLowLim);
	Assert(m_hwndProgress);
	::SendMessage(m_hwndProgress, PBM_SETPOS, m_nCurrent, 0);
	CheckCancel();
}

/*----------------------------------------------------------------------------------------------
	Send the PBM_SETSTEP message to the progress bar window.

	@param nStepInc Default amount by which to advance the progress bar.
----------------------------------------------------------------------------------------------*/
void AfProgressDlg::SetStep(int nStepInc)
{
	Assert(m_hwndProgress);
	::SendMessage(m_hwndProgress, PBM_SETSTEP, nStepInc, 0);
	m_nStep = nStepInc;
	CheckCancel();
}

/*----------------------------------------------------------------------------------------------
	Send the PBM_SETPOS message to the progress bar window.

	@param nNewPos New position for the progress bar.
----------------------------------------------------------------------------------------------*/
void AfProgressDlg::SetPos(int nNewPos)
{
	Assert(m_hwndProgress);
	::SendMessage(m_hwndProgress, PBM_SETPOS, nNewPos, 0);
	m_nCurrent = nNewPos;
	CheckCancel();
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool AfProgressDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	m_hwndProgress = ::GetDlgItem(m_hwnd, kctidProgressBar);
	Assert(m_hwndProgress);
	SetRange(m_nLowLim, m_nHighLim);
	SetPos(0);
	m_nStep = 1;
	if (m_hcur)
	{
		m_hcurOld = (HCURSOR)::SetClassLongPtr(m_hwnd, GCLP_HCURSOR, (LONG_PTR)m_hcur);
		m_hcurOldProg = (HCURSOR)::SetClassLongPtr(m_hwndProgress, GCLP_HCURSOR,
			(LONG_PTR)m_hcur);
	}
	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Set a flag provided by the caller.  It is up to the caller to check this flag periodically.
	It disables the Cancel button as a visual clue to the user that something has happened, even
	if the program doesn't seem to know it yet.

	@return True if flag set, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfProgressDlg::OnCancel()
{
	if (m_pfCancelled)
	{
		*m_pfCancelled = true;
		::EnableWindow(::GetDlgItem(m_hwnd, kctidCancel), false);
		return true;
	}
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Check whether any messages are pending for the Cancel button, and send them on if they are.
	This allows the Cancel button to be clicked by the user during compute bound processing, and
	have the program handle the user action at intervals determined by calls to the public
	methods of this dialog.

	@return True if the canceled flag has been set, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfProgressDlg::CheckCancel()
{
	if (m_pfCancelled)
	{
		HWND hwndCancel = ::GetDlgItem(m_hwnd, kctidCancel);
		MSG msg;
		// Pick up all queued asynchronous mouse and keyboard messages on the Cancel button,
		// and handle them synchronously.
		while (::PeekMessage(&msg, hwndCancel, WM_MOUSEFIRST, WM_MOUSELAST, PM_REMOVE))
			::SendMessage(hwndCancel, msg.message, msg.wParam, msg.lParam);
		while (::PeekMessage(&msg, hwndCancel, WM_KEYFIRST, WM_KEYLAST, PM_REMOVE))
			::SendMessage(hwndCancel, msg.message, msg.wParam, msg.lParam);

		// Pick up all queued asynchronous mouse messages on the progress bar control, and
		// handle them synchronously.
		while (::PeekMessage(&msg, m_hwndProgress, WM_MOUSEFIRST, WM_MOUSELAST, PM_REMOVE))
			::SendMessage(m_hwnd, msg.message, msg.wParam, msg.lParam);
		// Pick up all queued asynchronous mouse messages on the dialog proper, and handle them
		// synchronously.
		while (::PeekMessage(&msg, m_hwnd, WM_MOUSEFIRST, WM_MOUSELAST, PM_REMOVE))
			::SendMessage(m_hwnd, msg.message, msg.wParam, msg.lParam);

		return *m_pfCancelled;
	}
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle final cleanup as the window is being destroyed.  This primarily involves restoring
	the cursors from one assigned earlier.
----------------------------------------------------------------------------------------------*/
void AfProgressDlg::DestroyHwnd()
{
	if (m_hwnd && m_hcurOld)
	{
		HWND hwndProgress = ::GetDlgItem(m_hwnd, kctidProgressBar);
		if (hwndProgress && m_hcurOldProg)
		{
			::SetClassLongPtr(hwndProgress, GCLP_HCURSOR, (LONG_PTR)m_hcurOldProg);
			m_hcurOldProg = 0;
		}
		::SetClassLongPtr(m_hwnd, GCLP_HCURSOR, (LONG_PTR)m_hcurOld);
		m_hcurOld = 0;
	}
	SuperClass::SuperClass::DestroyHwnd();	// AfWnd defines this, AfDialog does not.
}


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkaflib.bat"
// End: (These 4 lines are useful to Steve McConnel.)
