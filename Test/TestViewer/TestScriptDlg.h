/*----------------------------------------------------------------------------------------------
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TlsOptDlg.h
Responsibility: Luke Ulrich
Last reviewed: Not yet.

Description:
	Non-modal dialog box that visually displays and allows editing of interactive tests
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef TSTSCPTDLG_H_INCLUDED
#define TSTSCPTDLG_H_INCLUDED
//---------------------------------------------------------------------------------------------
// Special constants that refer to bitmap handles and button window handles simultaneously
// Except for idPAUSE - it only has a bitmap handle
#define			idNEW		0
#define			idOPEN		1
#define			idSAVE		2
#define			idSAVEAS	3
#define			idEXECUTE	4
#define			idDELETE	5
#define			idRECORD	6
#define			idSTEP		7
#define			idPAUSE		8

/*----------------------------------------------------------------------------------------------
	This is the complete dialog class
	Hungarian: brdp.

----------------------------------------------------------------------------------------------*/

class TstScript : public AfDialog
{
typedef AfDialog SuperClass;
public:
	friend class WpChildWnd;
	TstScript();
	HWND Hwnd();

	virtual void OnReleasePtr();
	void SetTestObj(TestVwRoot * tvr);
	void SetDialogValues(WpChildWnd * pwcw, VwGraphicsPtr qvg);
	void GetDialogValues(int temp);

	bool OpenTestDialog();
	void LoadTest();
	void SaveTest(char *testname);
	bool SaveTestAs();

	void RecordString(string str);
protected:
	// Member variables.
	WpChildWnd * m_pwcw;		// Pointer to child window
	TestVwRoot * m_tvr;			// Pointer to viewlcass object
	VwGraphicsPtr m_qvg;		// VwGraphics pointer for Initializing/Uninitializing
	HWND listhwnd;				// Handle to listbox

	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
private:
	HBITMAP m_hbm[9];	// To hold the bitmaps for the buttons
	HWND btnHwnd[9];	// The corresponding button window handles
	bool m_fte;			// Test exists?
	bool m_fmodified;	// Test modified?
	char filename[256];	// Test filename
	bool Recording;		// Actively logging graphical input?

	void SetTestName(char *testname);
	char * GetTestName();
	bool TestExists();
	void SetTestExists(bool fte);
	bool IsRecording();
};

typedef GenSmartPtr<TstScript> TstScriptPtr;

#endif  // !TSTSCPTDLG_H