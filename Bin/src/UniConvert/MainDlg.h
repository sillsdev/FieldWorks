/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: MainDlg.h
Responsibility: Darrell Zook
Last reviewed:

	Header file for MainDlg.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef _MAINDLG_H_
#define _MAINDLG_H_


typedef enum {
	kfoWorkingDir = 0,
	kfoControlFile,
	kfoInputFile,
	kfoOutputFile,
	kfoErrorLog
} FileOffset;


class MainDlg
{
public:
	MainDlg(char szFiles[NUM_COMBOS][MAX_PATH], bool fSilent, HINSTANCE hInstance);
	~MainDlg();
	void Create();
	void Initialize(HWND hwndDlg);
	void SaveSettings();

	BOOL OnCommand(WPARAM wParam, LPARAM lParam);


protected:
	bool StartConvert();

	char m_szFiles[NUM_COMBOS][MAX_PATH];
	HWND m_hwndCombos[NUM_COMBOS];
	HWND m_hwndDlg;
	HINSTANCE m_hInstance;
	bool m_fSilent;
	HBITMAP m_hbmpOld;
	HDC m_hdcButton;
};

#endif // !_MAINDLG_H_