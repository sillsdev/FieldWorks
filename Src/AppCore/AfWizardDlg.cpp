/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfWizardDlg.cpp
Responsibility: John Wimbish
Last reviewed: never

Description:
	Implementation of the generic wizard dialog classes, as described in the header file.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

#define kMax 512   // Size for work/temprary string buffers


/***********************************************************************************************
	AfWizardPage Implementation
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructs a AfWizardPage object. The page is displayed after (1) the page has been added to
	an AfWizardDlg, (2) the DoModal function has been called, and (3) the user has used the
	Next button to move to the page.

	Paramemters:
		nIdTemplate - the id of the template resource for t his page.
----------------------------------------------------------------------------------------------*/
AfWizardPage::AfWizardPage(uint nIdTemplate)
{
	m_rid = nIdTemplate;
	m_pwiz = NULL;
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
AfWizardPage::~AfWizardPage()
{
}

/*----------------------------------------------------------------------------------------------
	This member function is called by the framework when the Cancel button is selected. Override
	this member function to perform Cancel button actions. You should negate any changes that
	have been made.

	Returns: true (always), in keeping with the AfDialog superclass, which for some reason
	does this.
----------------------------------------------------------------------------------------------*/
bool AfWizardPage::OnCancel()
{
	return true;
}


/*----------------------------------------------------------------------------------------------
	This member function is called by the framework when the page is no longer the active page.
	Override this member function to perform special data validation tasks. You should copy
	settings from controls to the member variables.

	Returns true if the data was updated successfully, otherwise zero. Where zero was returned,
	the page retains focus.
----------------------------------------------------------------------------------------------*/
bool AfWizardPage::OnKillActive()
{
	return true;
}


/*----------------------------------------------------------------------------------------------
	This member function is called by AfWizardDlg when the page is chosen by the user and
	becomes the active page. Override this member function to perform tasks when a page is
	activated. Your override of this member function should call the default version after any
	other processing is done.

	This AfWizardPage superclass method sets the focus of the dialog to the first control
	in the page. This includes a command to select the text in the first control (provided
	it is an edit box.) Subclasse methods should call this method if they desire this behavior.

	Returns: true if the page was successfully set active; otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfWizardPage::OnSetActive()
{
	HWND hwnd = GetNextDlgTabItem(Hwnd(), NULL, FALSE);
	Assert(NULL != hwnd);
	::SetFocus(hwnd);
	SendMessage(hwnd, EM_SETSEL, (WPARAM) 0,  (LPARAM) -1);
	return true;
}


/*----------------------------------------------------------------------------------------------
	This member function is called by the framework when the user clicks the Cancel button and
	before the cancel action has taken place. Override this member function to specify an
	action the program takes when the user clicks the Cancel button. For example, you can
	put up a message box asking the user if he really does indeed want to cancel.

	Returns: false to prevent the cancel operation, true to allow it. The default implementation
	returns true.
----------------------------------------------------------------------------------------------*/
bool AfWizardPage::OnQueryCancel()
{
	return true;
}


/*----------------------------------------------------------------------------------------------
	This member function is called by AfWizardDlg when the user clicks on the Back button in
	a wizard. Override this member function to perform some action when the Back button
	is pressed.

	Returns: NULL to automatically advance to the previous page;  –1 to prevent the page from
	changing. To jump to a page other than the previous one, return the handle of the page
	to display.
----------------------------------------------------------------------------------------------*/
HWND AfWizardPage::OnWizardBack()
{
	return NULL;
}


/*----------------------------------------------------------------------------------------------
	This member function is called by AfWizardDlg when the user clicks on the Next button in
	a wizard. Override this member function to perform some action when the Next button
	is pressed.

	Returns: NULL to automatically advance to the next page;  –1 to prevent the page from
	changing. To jump to a page other than the next one, return the handle of the page
	to display.
----------------------------------------------------------------------------------------------*/
HWND AfWizardPage::OnWizardNext()
{
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	This member function is called by AfWizardDlg when the user clicks on the Finish button.
	When the framework calls this function, the property sheet is destroyed when the Finish
	button is pressed and OnWizardFinish returns true. The method gives the opportunity to
	handle finish work on the page. (There is also a OnWizardFinish() defined on the
	AfWizardDlg where finish work can be done as an alternative.)

	Override this member function to specify some action to take when the Finish button is
	pressed. When overriding this function, return false to prevent the property sheet from
	being destroyed.

	Returns: true if the wizard is destroyed when the wizard finishes; otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfWizardPage::OnWizardFinish()
{
	return true;
}


/*----------------------------------------------------------------------------------------------
	Convenience method that sets the text of a control to a string from the resource file.

	Note: The amount of text copied into the control is kMax (as declared in the szBuffer
	array.) If the resource string is longer, LoadString will automatically truncate it
	(and nul-terminate it).
----------------------------------------------------------------------------------------------*/
void AfWizardPage::SetWindowText(uint ridCtrl, uint ridResourceString)
{
	HWND hwnd = GetDlgItem(Hwnd(), ridCtrl);
	Assert(NULL != hwnd);
	achar szBuffer[kMax];
	LoadString(ModuleEntry::GetModuleHandle(), ridResourceString, szBuffer, sizeof(szBuffer));
	::SetWindowText(hwnd, szBuffer);
}

/*----------------------------------------------------------------------------------------------
	Show the help page for the dialog if there is one.
----------------------------------------------------------------------------------------------*/
bool AfWizardPage::OnHelp()
{
	if (m_pszHelpUrl)
		return AfApp::Papp()->ShowHelpFile(m_pszHelpUrl);
	else
		return false;
}



/***********************************************************************************************
	AfWizardDlg Implementation
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Use this member function to construct a AfWizardDlg object. To display the wizard, call
	DoModal.
----------------------------------------------------------------------------------------------*/
AfWizardDlg::AfWizardDlg()
{
	m_rid = kridDlgWizard;                  // Standard wizard dialog resource
	m_cPages = 0;                           // No pages in the dlg yet
	m_iCurrentPage = -1;                    // No page being displayed yet.
	m_ridFinishText = kridWizFinish;        // Default text for the finish button.
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
AfWizardDlg::~AfWizardDlg()
{
	for (int i = 0; i < m_cPages; i++)
	{
		if (m_rgwizp[i])
		{
			::DestroyWindow(m_rgwizp[i]->Hwnd());
		}
		m_rgwizp[i].Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	Set the text for the "Finish" button
----------------------------------------------------------------------------------------------*/
void AfWizardDlg::SetFinishText(uint rid)
{
	Assert(rid > 0);
	m_ridFinishText = rid;
}


/*----------------------------------------------------------------------------------------------
	This member function is called by AfWizardDlg when the user clicks on the Finish button.
	When the framework calls this function, the property sheet is destroyed when the Finish
	button is pressed and OnWizardFinish returns true. The method gives the opportunity to
	handle finish work on the entire wizard. (There is also a OnWizardFinish() defined on each
	page, where finish work can be done as an alternative.)

	Override this member function to specify some action to take when the Finish button is
	pressed. When overriding this function, return false to prevent the property sheet from
	being destroyed.

	Returns: true if the property sheet is destroyed when the wizard finishes; otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfWizardDlg::OnWizardFinish()
{
	return true;
}


/*----------------------------------------------------------------------------------------------
	This is called when the user presses the Next button. Your subclass can do additional
	processing here. Return false if you wish to cancel the Next button action.
----------------------------------------------------------------------------------------------*/
bool AfWizardDlg::OnWizardNext()
{
	return true;
}

/*----------------------------------------------------------------------------------------------
	This is called when the user presses the Next button. Your subclass can do additional
	processing here. Return false if you wish to cancel the Back button action.
----------------------------------------------------------------------------------------------*/
bool AfWizardDlg::OnWizardBack()
{
	return true;
}


/*----------------------------------------------------------------------------------------------
	This member function appends the supplied page to the wizard. Add pages to the wizard in
	the order you want them to appear. AddPage adds the AfWizardPage object to the
	AfWizardDlg’s list of pages but does not actually create the window for the page. The
	wizard postpones creation of the window for the page until DoModal is called.

	When you add a page using AddPage, the AfWizardDlg is the parent of the AfWizardPage.
	You should call AddPage for each page, prior to calling DoModal.
----------------------------------------------------------------------------------------------*/
void AfWizardDlg::AddPage(AfWizardPage * pwizp)
{
	// Make certain there is room in the array for the page
	Assert(m_cPages < kcdwizp - 1);

	// Make sure that we've passed in a valid page
	Assert(NULL != pwizp);

	// Insert the page into the array
	m_rgwizp[m_cPages].Attach(pwizp);
	++m_cPages;

	// The page will want to know about its owning dialog.
	pwizp->m_pwiz = this;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog. Called from DoModal. A subclass could call AddPage from its OnInitDlg
	method, and then call AfWizardDlg::OnInitDlg as the final thing. The method performs the
	following:
	1. Sizes the wizard to just contain the largest page,
	2. Places the row of buttons underneath the pages,
	3. Sizes the wizard to just contain everything.
	4. Creates all of the pages.
----------------------------------------------------------------------------------------------*/
bool AfWizardDlg::OnInitDlg(HWND hwndParent, LPARAM lp)
{
	// Make certain we have at least one page, otherwise we don't have anything to display!
	Assert(m_cPages > 0);

	// Calculate the rectangle of the largest page
	RECT rc;
	int nHorzMargin = 5;                    // Left, right margin for pages within parent dlg
	int nVertMargin = 5;                    // Top, bottom margin for pages within parent dlg
	int nHeight = 0;
	int nWidth = 0;
	for (int i = 0; i < m_cPages; i++)
	{
		// Create the window if it hasn't been done already
		if (!m_rgwizp[i]->Hwnd())
		{
			m_rgwizp[i]->DoModeless(m_hwnd);
			::SetWindowPos(m_rgwizp[i]->Hwnd(), NULL, nHorzMargin, nVertMargin, 0, 0,
				SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
		}
		Assert(NULL != m_rgwizp[i]->Hwnd());

		// Retrieve its coordinates
		::GetWindowRect(m_rgwizp[i]->Hwnd(), &rc);
		nHeight = max(nHeight, rc.bottom - rc.top);
		nWidth  = max(nWidth,  rc.right  - rc.left);
	}

	// Calculate the horizontal line position to be just beneath the pages; then reposition
	// the line.
	int yLine = nVertMargin + nHeight + nVertMargin;
	HWND hwndLine = GetDlgItem(m_hwnd, kridWizLine);
	::SetWindowPos(hwndLine, NULL, nHorzMargin, yLine, nWidth, 2,
		SWP_NOZORDER | SWP_NOACTIVATE);

	// Calculate the button position to be just beneath the horizontal line
	POINT ptBtn;
	ptBtn.x = nHorzMargin + nWidth - 1;
	ptBtn.y = yLine + 10;

	// Move the buttons
	HWND hwnd;
	RECT rcBtn;
	int nBtnHeightMax = 0;
	static uint rgidButtons[] = { kctidHelp, kctidCancel, kctidWizNext, kctidWizBack };
	for (int i = 0; i < sizeof(rgidButtons)/sizeof(uint); i++)
	{
		hwnd = GetDlgItem(m_hwnd, rgidButtons[i]);
		::GetWindowRect(hwnd, &rcBtn);
		nBtnHeightMax = max(nBtnHeightMax, rcBtn.bottom - rcBtn.top);
		::SetWindowPos(hwnd, NULL,
			ptBtn.x - (rcBtn.right - rcBtn.left), ptBtn.y,
			rcBtn.right - rcBtn.left, rcBtn.bottom - rcBtn.top,
			SWP_NOZORDER | SWP_NOACTIVATE);
		ptBtn.x -= ((rcBtn.right - rcBtn.left) + 5);
	}

	// Figure out how much non-client space we need for the window.
	RECT rcDlg;
	RECT rcDlgClient;
	::GetWindowRect(Hwnd(), &rcDlg);
	::GetClientRect(Hwnd(), &rcDlgClient);
	int nPadVert = (rcDlg.bottom - rcDlg.top)  - (rcDlgClient.bottom - rcDlgClient.top);
	int nPadHorz = (rcDlg.right  - rcDlg.left) - (rcDlgClient.right  - rcDlgClient.left);

	// Size the dialog to contain the pages plus room for the buttons
	::SetWindowPos(Hwnd(), NULL, 0, 0,
		nPadHorz + (2 * nHorzMargin) + nWidth,
		nPadVert + (2 * nVertMargin) + ptBtn.y + nBtnHeightMax,
		SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);

	// Show the first page
	CenterInWindow(hwndParent);
	SetActivePage(0);

	// Do any superclass initializations
	SuperClass::OnInitDlg(hwndParent, lp);
	return false;
}


/*----------------------------------------------------------------------------------------------
	Handles the case where the user presses the Back button. This calls various
	methods on the Pages as appropriate (refer to the internal comments for details.)
----------------------------------------------------------------------------------------------*/
void AfWizardDlg::OnBackButtonPressed()
{
	// If we're on the first page, then nothing should happen. (Actually, the button should
	// be disabled and a Back button action not be possible.)
	if (0 == m_iCurrentPage)
		return;

	// Give the wizard's subclass an opportunity to perform some action in response to
	// the message. If OnWizardBack returns false, then we cancel the Next operation.
	if (false == OnWizardBack())
		return;

	// Give the page the opportunity to tell us which page they want us to go to.
	HWND hwnd = m_rgwizp[m_iCurrentPage]->OnWizardBack();

	// The default case is just to go back to the previous page.
	if (NULL == hwnd)
		SetActivePage(m_iCurrentPage - 1);

	// For a non-Null value, it means the page wants us to advance to a specific page.
	// We therefore look for that page. (If we don't find it, then we don't change
	// to any page. Thus the user can return -1 from OnWizardBack and the page will
	// not change.
	else
	{
		for (int i = 0; i < m_cPages; i++)
		{
			if (hwnd == m_rgwizp[i]->Hwnd())
				SetActivePage(i);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Handles the case where the user presses the Next (or Finish) button. This calls various
	methods on the Pages as appropriate (refer to the internal comments for details.)
----------------------------------------------------------------------------------------------*/
void AfWizardDlg::OnNextButtonPressed()
{
	// If we are not on the final page, then the user pressed the Next button. Otherwise he
	// actually pressed the Finish button (the labels change automatically.)
	if (m_iCurrentPage + 1 < m_cPages)
	{
		// Give the wizard's subclass an opportunity to perform some action in response to
		// the message. If OnWizardNext returns false, then we cancel the Next operation.
		if (false == OnWizardNext())
			return;

		// Give the page the opportunity to tell us which page they want us to go to.
		HWND hwndNext = m_rgwizp[m_iCurrentPage]->OnWizardNext();

		// The default case is just to advance to the next page.
		if (NULL == hwndNext)
			SetActivePage(m_iCurrentPage + 1);

		// For a non-Null value, it means the page wants us to advance to a specific page.
		// We therefore look for that page. (If we don't find it, then we don't change
		// to any page.
		else
		{
			for (int i = 0; i < m_cPages; i++)
			{
				if (hwndNext == m_rgwizp[i]->Hwnd())
					SetActivePage(i);
			}
		}
	}

	// The other possibility is that the user hit the Finish button, therefore, we want to
	// perform all of the Finish work.
	else
	{
		bool fOkToLeaveLastPage = m_rgwizp[m_iCurrentPage]->OnKillActive();
		if (fOkToLeaveLastPage)
		{
			// First call OnWizardFinish for each page
			int i;
			for (i = 0; i < m_cPages; i++)
			{
				if (false == m_rgwizp[i]->OnWizardFinish())
					break;
			}
			// Then call it for the entire wizard.
			if (i == m_cPages && OnWizardFinish())
				::EndDialog(m_hwnd, kctidOk);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	OnNotifyChild. Handle messages from the child windows.
----------------------------------------------------------------------------------------------*/
bool AfWizardDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	// Next button was clicked.
	if (BN_CLICKED == pnmh->code && kctidWizNext == pnmh->idFrom)
	{
		OnNextButtonPressed();
		return true;
	}

	// Back button was clicked.
	if (BN_CLICKED == pnmh->code && kctidWizBack == pnmh->idFrom)
	{
		OnBackButtonPressed();
		return true;
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	The Cancel button was clicked. Call OnQueryCancel for all of the pages. If any of them
	return a false value, then we do not perform the cancel operation for the wizard.
----------------------------------------------------------------------------------------------*/
bool AfWizardDlg::OnCancel()
{
	// Call OnQueryCancel for each page, until any one of the pages returns false, indicating
	// that it doesn't want the Cancel to occur. By returning true, we prevent the cancel
	// operation from happening.
	for (int i = 0; i < m_cPages; i++)
	{
		if (false == m_rgwizp[i]->OnQueryCancel())
			return true;
	}

	// Now go through the same process again, only this time performing the OnCancel
	// operation for each page.
	for (int i = 0; i < m_cPages; i++)
	{
		if (false == m_rgwizp[i]->OnCancel())
			return true;
	}

	// All clear to cancel. (The superclass will call the EndDialog method.)
	return SuperClass::OnCancel();
}

/*----------------------------------------------------------------------------------------------
	Returns the number of pages currently in the wizard.
----------------------------------------------------------------------------------------------*/
int AfWizardDlg::GetPageCount()
{
	return m_cPages;
}


/*----------------------------------------------------------------------------------------------
	Returns a pointer to the desired page
----------------------------------------------------------------------------------------------*/
AfWizardPage * AfWizardDlg::GetPage(int iPageNo)
{
	Assert(iPageNo >= 0 && iPageNo < m_cPages);
	return m_rgwizp[iPageNo];;
}


/*----------------------------------------------------------------------------------------------
	Returns a pointer to the active page, or NULL if the dialog has not been invoked yet.
----------------------------------------------------------------------------------------------*/
AfWizardPage * AfWizardDlg::GetActivePage()
{
	if (-1 == m_iCurrentPage)
		return NULL;
	return m_rgwizp[m_iCurrentPage];
}


/*----------------------------------------------------------------------------------------------
	Enable or disable the "Next" button.
----------------------------------------------------------------------------------------------*/
bool AfWizardDlg::EnableNextButton(bool fEnable)
{
	HWND hwndNextBtn = GetDlgItem(m_hwnd, kctidWizNext);
	Assert(hwndNextBtn);
	return ::EnableWindow(hwndNextBtn, fEnable);
}

/*----------------------------------------------------------------------------------------------
	Returns the index of the active page, or -1 if the dialog has not been invoked yet.
----------------------------------------------------------------------------------------------*/
int AfWizardDlg::GetActiveIndex()
{
	if (-1 == m_iCurrentPage)
		return -1;
	return m_iCurrentPage;
}

/*----------------------------------------------------------------------------------------------
	Returns the index of the specified page, or -1 if not found.
----------------------------------------------------------------------------------------------*/
int AfWizardDlg::GetPageIndex(AfWizardPage * pwizp)
{
	Assert(NULL != pwizp);

	for (int i = 0; i < m_cPages; i++)
	{
		if (pwizp == m_rgwizp[i])
			return i;
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Makes the requested page active, hiding the previous page and showing the new one;
	provided that the individual pages are happy with the idea, as evidenced by their
	response to OnKillActive and OnSetActive messages.

	Parameters:
		iPageNo - the desired page which will be made active.

	Returns: true if the new page is made active, false otherwise.
----------------------------------------------------------------------------------------------*/
bool AfWizardDlg::SetActivePage(int iPage)
{
	// Make certain the dialog has been invoked via DoModal
	Assert(NULL != Hwnd());

	// Make sure the requested page is within range
	Assert(iPage >= 0 && iPage < m_cPages);

	// Make sure the requested page is not NULL.
	AssertPtr(m_rgwizp[iPage]);

	// If we are already showing the desired page, then there isn't anything here to do.
	if (m_iCurrentPage == iPage)
		return true;

	// If this assert fires, the page was not created prior to this command to show it.
	Assert(NULL != m_rgwizp[iPage]->Hwnd());

	// Find out if we can leave the current page. If OnKillActive returns false, then
	// we do not activate the new page.
	if (m_iCurrentPage != -1)
	{
		bool fOkToLeaveOldPage = m_rgwizp[m_iCurrentPage]->OnKillActive();
		if (false == fOkToLeaveOldPage)
			return false;
	}

	// Now find out if we can enter the new page. If OnSetActive returns false, then
	// we do not activate the new page.
	if (!m_rgwizp[iPage]->OnSetActive())
		return false;

	// At this point, then, we are cleared to hide the old page and show the new one.
	if (m_iCurrentPage != -1)
		::ShowWindow(m_rgwizp[m_iCurrentPage]->Hwnd(), SW_HIDE);
	m_iCurrentPage = iPage;
	::ShowWindow(m_rgwizp[iPage]->Hwnd(), SW_SHOW);

	// Handle the various button states, depending upon what our current window is.
	HWND hwndBackBtn = GetDlgItem(Hwnd(), kctidWizBack);
	HWND hwndNextBtn = GetDlgItem(Hwnd(), kctidWizNext);
	Assert(hwndBackBtn);
	Assert(hwndNextBtn);
	::EnableWindow(hwndBackBtn, 0 != m_iCurrentPage);
	achar szBuffer[kMax];
	uint ridResourceString = (m_iCurrentPage == m_cPages -1) ? m_ridFinishText : kridWizNext;
	LoadString(ModuleEntry::GetModuleHandle(), ridResourceString, szBuffer, sizeof(szBuffer));
	::SetWindowText(hwndNextBtn, szBuffer);

	// We were successful
	return true;
}

/*----------------------------------------------------------------------------------------------
	Makes the specified page the active page (the one that is currently displayed in the
	wizard.)

	Parameters:
		pwizp - the desired page which will be made active.

	Returns: true if the new page is made active, false otherwise.
----------------------------------------------------------------------------------------------*/
bool AfWizardDlg::SetActivePage(AfWizardPage * pwizp)
{
	return SetActivePage(GetPageIndex(pwizp));
}

/*----------------------------------------------------------------------------------------------
	Show the help page for the active page if there is one, otherwise revert to the default
	behavior.
----------------------------------------------------------------------------------------------*/
bool AfWizardDlg::OnHelp()
{
	if (m_rgwizp[m_iCurrentPage]->OnHelp())
		return true;
	else
		return SuperClass::OnHelp();
}
