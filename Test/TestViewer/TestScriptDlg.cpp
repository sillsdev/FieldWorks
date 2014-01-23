/*----------------------------------------------------------------------------------------------
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TlsOptDlg.cpp
Responsibility: Luke Ulrich
Last reviewed: Not yet.

Description:
	Non-modal dialog box that visually displays and allows editing of interactive tests.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

/***********************************************************************************************
	Modify Field Text Dialog Methods
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
TstScript::TstScript(void)
{
	m_rid = kridTstScrDlg;
}

HWND TstScript::Hwnd()
{
	return m_hwnd;
}
//---------------------------------------------------------------------------------------------
void TstScript::OnReleasePtr()
{
	// Cycle through each bitmap handle and delete the associated memory
	for (int i = 0; i < 9; i++)
		if (m_hbm[i])
			DeleteObject(m_hbm[i]);
	// Call parent class pointer release
	SuperClass::OnReleasePtr();
}
//---------------------------------------------------------------------------------------------
void TstScript::SetTestObj(TestVwRoot * tvr)
{
	m_tvr = tvr;
}
//---------------------------------------------------------------------------------------------
/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.
	A value that is out of range will be brought in range without complaint.
----------------------------------------------------------------------------------------------*/
void TstScript::SetDialogValues(WpChildWnd *pwcw, VwGraphicsPtr qvg)
{
	m_qvg = qvg;
	m_pwcw = pwcw;
}

/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.
	A value that is out of range will be brought in range without complaint.
----------------------------------------------------------------------------------------------*/
void TstScript::GetDialogValues(int temp)
{
}
//---------------------------------------------------------------------------------------------
bool TstScript::OpenTestDialog()
{
	// Get the current directory
	static char szFile[260] = {0};		// buffer for filename
	static char szFileTitle[64] = {0};	// buffer for filename + ext without path info
	static char szDir[256] = {0};
	GetCurrentDirectory(256, szDir);

	OPENFILENAME ofn;		// common dialog box structure

	// Initialize OPENFILENAME
	ZeroMemory(&ofn, sizeof(OPENFILENAME));
	ofn.lStructSize = sizeof(OPENFILENAME);
	ofn.hwndOwner = m_pwcw->Hwnd();
	ofn.nMaxFile = sizeof(szFile);
	ofn.lpstrFile = szFile;
	ofn.lpstrTitle = "Open Test...";				// dialog title
	ofn.lpstrFilter = "All\0*.*\0Test\0*.TST\0";
	ofn.nFilterIndex = 2;
	ofn.lpstrFileTitle = szFileTitle;
	ofn.nMaxFileTitle = 64;
	ofn.lpstrInitialDir = szDir;
	ofn.Flags = OFN_PATHMUSTEXIST | OFN_HIDEREADONLY;

	// Display the Save dialog box.
	if (!GetOpenFileName(&ofn))
		return false;

	// Store the test name for future usage
	SetTestName(szFile);
	// Set the dialog box title to the current test name
	::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)szFileTitle);

	return true;
}
//---------------------------------------------------------------------------------------------
void TstScript::LoadTest()
{
	char szBuf[256];

	// Clear the listbox
	::SendMessage(listhwnd, LB_RESETCONTENT, 0, 0);
	// Load the existing test contents into the listbox
	m_tvr->SetMacroIn(GetTestName());
	while (m_tvr->GetLine(szBuf))
		::SendMessage(listhwnd, LB_ADDSTRING, 0, (LPARAM)(LPCTSTR)szBuf);
	m_tvr->CloseMacroIn();

	// Adjust data members to reflect current file status
	m_fmodified = false;
	SetTestExists(true);
	EnableWindow(btnHwnd[idEXECUTE], true);
}
//---------------------------------------------------------------------------------------------
void TstScript::SaveTest(char *testname)
{
	char szBuf[256];
	m_tvr->SetMacroOut(testname);

	int iCount = ::SendMessage(listhwnd, LB_GETCOUNT, 0, 0);
	for (int i = 0; i < iCount; i++)
	{
		::SendMessage(listhwnd, LB_GETTEXT, i, (LPARAM)(LPCTSTR)szBuf);
		m_tvr->WriteLine(szBuf);
	}
	m_fmodified = false;
	SetTestExists(true);
	m_tvr->CloseMacroOut();
}
//---------------------------------------------------------------------------------------------
bool TstScript::SaveTestAs()
{
	// Get the current directory
	static char szFile[260] = {0};	// buffer for filename
	static char szFileTitle[64] = {0};	// buffer for filename + ext without path info
	static char szDir[256] = {0};
	GetCurrentDirectory(256, szDir);

	OPENFILENAME ofn;		// common dialog box structure

	// Initialize OPENFILENAME
	ZeroMemory(&ofn, sizeof(OPENFILENAME));
	ofn.lStructSize = sizeof(OPENFILENAME);
	ofn.hwndOwner = m_pwcw->Hwnd();
	ofn.nMaxFile = sizeof(szFile);
	ofn.lpstrFile = szFile;
	ofn.lpstrTitle = "Save Test As...";				// dialog title
	ofn.lpstrFilter = "All\0*.*\0Test\0*.TST\0";
	ofn.nFilterIndex = 2;
	ofn.lpstrFileTitle = szFileTitle;
	ofn.nMaxFileTitle = 64;
	ofn.lpstrInitialDir = szDir;
	ofn.Flags = OFN_PATHMUSTEXIST | OFN_HIDEREADONLY;

	// Display the Save dialog box.
	if (!GetSaveFileName(&ofn))
		return false;

	// Store the test name for future usage
	SetTestName(szFile);
	SaveTest(GetTestName());

	// Determine whether we need to add a ".tst" extension or not
	string title = szFileTitle;
	int ires = title.find(".tst", 0);
	if(ires < 0)
		title.append(".tst");
	// Adjust the dialog box title to current test name
	::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)title.c_str());

	return true;
}
//---------------------------------------------------------------------------------------------
// This function most likely will be called from other classes. For example, from the WpMainWnd
// class, each of the CallOn... functions use this function to log the AfVwRootSite call as a
// string to the listbox
void TstScript::RecordString(string str)
{
	// Add the string
	int index = ::SendMessage(listhwnd, LB_ADDSTRING, 0, (LPARAM)(LPCTSTR)str.c_str());
	// Update the caret index for just added string
	::SendMessage(listhwnd, LB_SETCARETINDEX, index, MAKELPARAM(true, 0));
	// set the test status
	m_fmodified = true;
}
//---------------------------------------------------------------------------------------------
//---------------------------------------------------------------------------------------------
//---------------------------------------------------------------------------------------------
/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)  This is also called to update the spin controls in the dialog.
----------------------------------------------------------------------------------------------*/
bool TstScript::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Initialize specific data members accordingly
	::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)"No Test Loaded");
	listhwnd = ::GetDlgItem(m_hwnd, kcidTstScrList);
	m_fmodified = false;
	m_fte = false;
	Recording = false;

	// Need an instance of the dialog box for use in the LoadBitmap function
	HINSTANCE dlghandle = (HINSTANCE)GetWindowLong(m_hwnd, GWL_HINSTANCE);

	// New button bitmap
	m_hbm[idNEW] = ::LoadBitmap(dlghandle, "NEWBITMAP");
	btnHwnd[idNEW] = ::GetDlgItem(m_hwnd, kcidNewBtn);
	// Open button bitmap
	m_hbm[idOPEN] = ::LoadBitmap(dlghandle, "OPENBITMAP");
	btnHwnd[idOPEN] = ::GetDlgItem(m_hwnd, kcidOpenBtn);
	// Save button bitmap
	m_hbm[idSAVE] = ::LoadBitmap(dlghandle, "SAVEBITMAP");
	btnHwnd[idSAVE] = ::GetDlgItem(m_hwnd, kcidSaveBtn);
	// SaveAs button bitmap
	m_hbm[idSAVEAS] = ::LoadBitmap(dlghandle, "SAVEASBITMAP");
	btnHwnd[idSAVEAS] = ::GetDlgItem(m_hwnd, kcidSaveAsBtn);
	// Execute button bitmap
	m_hbm[idEXECUTE] = ::LoadBitmap(dlghandle, "EXECUTEBITMAP");
	btnHwnd[idEXECUTE] = ::GetDlgItem(m_hwnd, kcidExecuteBtn);
	// Delete button bitmap
	m_hbm[idDELETE] = ::LoadBitmap(dlghandle, "DELETEBITMAP");
	btnHwnd[idDELETE] = ::GetDlgItem(m_hwnd, kcidDeleteBtn);
	// Pause button bitmap
	m_hbm[idRECORD] = ::LoadBitmap(dlghandle, "WRITEBITMAP");
	btnHwnd[idRECORD] = ::GetDlgItem(m_hwnd, kcidPauseTstBtn);
	// Step button bitmap
	m_hbm[idSTEP] = ::LoadBitmap(dlghandle, "STEPBITMAP");
	btnHwnd[idSTEP] = ::GetDlgItem(m_hwnd, kcidRunLineBtn);
	// PauseExec bitmap bitmap
	m_hbm[idPAUSE] = ::LoadBitmap(dlghandle, "PAUSEBITMAP");

	// Attach bitmaps to their respective buttons
	for (int i = 0; i < 8; i++)
		::SendMessage(btnHwnd[i], BM_SETIMAGE, IMAGE_BITMAP, (LPARAM)m_hbm[i]);

	return AfDialog::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Process notifications for main dialog from user.
----------------------------------------------------------------------------------------------*/
bool TstScript::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	// Decipher which control is sending the message
	switch(ctid)	{
	case kcidNewBtn:		// New Button
		{
			if (m_fmodified)
			{
				switch (MessageBox(m_hwnd, "Do you wish to save the current test", "New Test",
					MB_YESNOCANCEL))
				{
				case IDYES:
					// Does test already have a name? If so, perform regular save
					if (strcmp(GetTestName(), ""))
						SaveTest(GetTestName());
					else				// Otherwise ask user for a name
						SaveTestAs();
				case IDNO:		// fall through
					// Clear the listbox
					::SendMessage(listhwnd, LB_RESETCONTENT, 0, 0);
					break;
				case IDCANCEL:
					return 0;
				}
			}
			else
				::SendMessage(listhwnd, LB_RESETCONTENT, 0, 0);
			// Adjust the test file status and button availability
			SetTestExists(false);
			m_fmodified = false;
			SetTestName("");
			// Not recording info as default
			Recording = false;
			::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)"No Test Loaded");
			::SendMessage(btnHwnd[idRECORD], BM_SETIMAGE, IMAGE_BITMAP, (LPARAM)m_hbm[idRECORD]);
			EnableWindow(btnHwnd[idEXECUTE], true);
			EnableWindow(btnHwnd[idNEW], true);
		}
		break;
	case kcidRecordTstBtn:			// Record Button
		Recording = !Recording;
		if (Recording)
		{
			::SendMessage(btnHwnd[idRECORD], BM_SETIMAGE, IMAGE_BITMAP, (LPARAM)m_hbm[idPAUSE]);
			EnableWindow(btnHwnd[idEXECUTE], false);
			EnableWindow(btnHwnd[idOPEN], false);
		}
		else
		{
			::SendMessage(btnHwnd[idRECORD], BM_SETIMAGE, IMAGE_BITMAP, (LPARAM)m_hbm[idRECORD]);
			EnableWindow(btnHwnd[idEXECUTE], true);
			EnableWindow(btnHwnd[idOPEN], true);
		}
		break;
	case kcidOpenBtn:	// Open button
		{
			if (!TestExists())	// A test does not exists
			{
				if (OpenTestDialog())	// If user selected a file
					LoadTest();			// Load it into the listbox control
			}
			else	// test exists
			{
				if (!m_fmodified)	// Unchanged test, so...
				{
					if (OpenTestDialog())	// If user selected a file
						LoadTest();	// Load it into the listbox control
				}
				else	// Test has been modified
				{
					switch( MessageBox(m_hwnd, "Do you wish to save the current test?",
						"Modified Test", MB_YESNOCANCEL))
					{
					case IDYES:
						if (strcmp(GetTestName(), ""))	// If test has a name
						{
							SaveTest(GetTestName());
							if (OpenTestDialog())	// If user selected a file
								LoadTest();
						}
						else	// otherwise
							if (SaveTestAs() && OpenTestDialog())
								LoadTest();
						break;
					case IDNO:
						if (OpenTestDialog())
							LoadTest();
					}
				}
			}
		}
		break;
	case kcidSaveBtn:		// SaveTest Button
		{
			// Is the listbox not modified
			if (!m_fmodified)
				break;
			if (strcmp(GetTestName(), ""))
				SaveTest(GetTestName());
			else
				SaveTestAs();
		}
		break;
	case kcidSaveAsBtn:			// SaveTestAs button
		SaveTestAs();
		break;
	case kcidExecuteBtn:	// Execute Button
		{
			// Write listbox contents to disk before executing
			// Auto-save if test name already given
			if (strcmp(filename, ""))
				SaveTest(GetTestName());
			else	// Otherwise ask for a name, if fails return
				if (!SaveTestAs())
					break;

			// NOTE: There are a couple of ways to execute a macro/test script. First,
			// Could call the RunMacro function with a baseline name and test name (this
			// would be the file just written to disk - usually)
			// OR can cycle through number of items in the listbox and call runline
			// for that many times - again this is reading from the file
			// OR call RunString(listbox string) for each listbox item
			int icount = ::SendMessage(listhwnd, LB_GETCOUNT, 0, 0);
			m_tvr->SetBaselineFile(GetTestName());
			m_tvr->SetMacroIn(GetTestName());
			// Before doing any graphical operation, must call InitGraphics and provide
			// a valid VwGraphics pointer - otherwise - dies
			m_pwcw->InitGraphics();
			m_tvr->SetVwGraphicsPtr(m_qvg);
			for (int i = 0; i < icount; i++)
			{
				// Move through each item and select it as called to provide visual cue
				// as to progess of test
				::SendMessage(listhwnd, LB_SETSEL, true, i);
				::SendMessage(listhwnd, LB_SETCARETINDEX, i, MAKELPARAM(false, 0));
				UpdateWindow(listhwnd);
				m_tvr->RunLine();
			}
			// Uninitialize graphics
			m_pwcw->UninitGraphics();
			m_tvr->CloseMacroIn();
		}
		break;
	case kcidDeleteBtn:	// Delete button
		{
			// Get total number of strings in the listbox
			int icount = ::SendMessage(listhwnd, LB_GETCOUNT, 0, 0);
			// Cycle through each one
			for (int i = icount; i >= 0; i--)
			{
				// If the item is selected, delete it
				if (::SendMessage(listhwnd, LB_GETSEL, i, 0))
					::SendMessage(listhwnd, LB_DELETESTRING, i, 0);
			}
			m_fmodified = true;
		}
		break;
	case kcidRunLineBtn:		// RunLine Button
		{
			// Run the "careted" line
			int i = ::SendMessage(listhwnd, LB_GETCARETINDEX, 0, 0);
			int icount = ::SendMessage(listhwnd, LB_GETCOUNT, 0, 0);
			// Are we at the end and is it selected (use the selected property to determine
			// if line has been executed or not
			if (i+1 >= icount && ::SendMessage(listhwnd, LB_GETSEL, i, 0))
				break;
			char szBuf[256];
			// See kcidExecuteBtn case for info on executing test scripts in various ways
			::SendMessage(listhwnd, LB_SETSEL, true, i);
			::SendMessage(listhwnd, LB_SETCARETINDEX, i+1, MAKELPARAM(false, 0));
			m_pwcw->InitGraphics();
			m_tvr->SetVwGraphicsPtr(m_qvg);
			::SendMessage(listhwnd, LB_GETTEXT, i, (LPARAM)(LPCTSTR)szBuf);
			m_tvr->RunString(szBuf);
			m_pwcw->UninitGraphics();
		}
		break;
	}

	return SuperClass::OnNotifyChild(ctid, pnmh, lnRet);
}
//---------------------------------------------------------------------------------------------
// Below are utility functions that control changing of test filename and test existence
// function calls to call in place of referring to m_fte
//---------------------------------------------------------------------------------------------
char * TstScript::GetTestName()
{
	return filename;
}
//---------------------------------------------------------------------------------------------
void TstScript::SetTestName(char * testname)
{
	strcpy(filename, testname);
	string temp = filename;
	if (temp.length() == 0)
		return;
	int ires = temp.find(".tst", 0);
	if(ires < 0)
		strcat(filename, ".tst");
}
//---------------------------------------------------------------------------------------------
bool TstScript::TestExists()
{
	return m_fte;
}
//---------------------------------------------------------------------------------------------
void TstScript::SetTestExists(bool fte)
{
	m_fte = fte;
}
//---------------------------------------------------------------------------------------------
bool TstScript::IsRecording()
{
	return Recording;
}