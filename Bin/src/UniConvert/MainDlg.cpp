/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: MainDlg.cpp
Responsibility: Darrell Zook
Last reviewed:

	Implementation file for MainDlg.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


#define BUTTON_WIDTH  23
#define BUTTON_HEIGHT 22

const char kpszSubKey[] = "Software\\SIL\\UniConvert";

const char * kpszHistoryValueNames[NUM_COMBOS] = {
	"Working Directory History",
	"Control File History",
	"Input File History",
	"Output File History",
	"Error Log History"
};

const char * kpszComboError[NUM_COMBOS] = {
	"Please enter the working directory.",
	"Please enter the control file.",
	"Please enter the input file.",
	"Please enter the output file.",
	"Please enter the error log file."
};

HDC g_hdcButton = 0;
HWND g_hwndButtonIsDrawn = NULL;
WNDPROC g_lpfnOldButtonProc = NULL;

typedef enum
{
	kdtDrawUp,
	kdtDrawDown,
	kdtNoDraw,
} DrawType;


/*----------------------------------------------------------------------------------------------
	Callback function for the main dialog
----------------------------------------------------------------------------------------------*/

BOOL CALLBACK MainDlgProc(HWND hwndDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	MainDlg * pmd = (MainDlg *)GetWindowLong(hwndDlg, GWL_USERDATA);

	switch (uMsg)
	{
	case WM_INITDIALOG:
		SetWindowLong(hwndDlg, GWL_USERDATA, lParam);
		AssertPtr((MainDlg *)lParam);
		((MainDlg *)lParam)->Initialize(hwndDlg);
		ShowWindow(hwndDlg, SW_SHOW);
		return 1;

	case WM_COMMAND:
		AssertPtr(pmd);
		return pmd->OnCommand(wParam, lParam);

	case WM_MOUSEMOVE:
		if (g_hwndButtonIsDrawn)
		{
			HWND hwndButton = g_hwndButtonIsDrawn;
			g_hwndButtonIsDrawn = NULL;
			InvalidateRect(hwndButton, NULL, FALSE);
			UpdateWindow(hwndButton);
		}
		return 1;

	default:
		break;
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Draw a 3d rectangle on the dialog based on the draw type.
----------------------------------------------------------------------------------------------*/
void Draw3dRect(HDC hdc, DrawType dt)
{
	COLORREF clrTopLeft, clrBottomRight;
	switch (dt)
	{
	case kdtDrawUp:
		clrTopLeft = GetSysColor(COLOR_3DHILIGHT);
		clrBottomRight = GetSysColor(COLOR_3DSHADOW);
		break;

	case kdtDrawDown:
		clrTopLeft = GetSysColor(COLOR_3DSHADOW);
		clrBottomRight = GetSysColor(COLOR_3DHILIGHT);
		break;

	case kdtNoDraw:
		clrTopLeft = clrBottomRight = GetSysColor(COLOR_3DFACE);
		break;

	default:
		Assert(false);
	}

	HPEN hpenOld = (HPEN)SelectObject(hdc, CreatePen(PS_SOLID, 0, clrBottomRight));
	MoveToEx(hdc, 0, BUTTON_HEIGHT - 1, NULL);
	LineTo(hdc, BUTTON_WIDTH - 1, BUTTON_HEIGHT - 1);
	LineTo(hdc, BUTTON_WIDTH - 1, 0);
	DeleteObject(SelectObject(hdc, CreatePen(PS_SOLID, 0, clrTopLeft)));
	LineTo(hdc, 0, 0);
	LineTo(hdc, 0, BUTTON_HEIGHT - 2);
	DeleteObject(SelectObject(hdc, hpenOld));
}

/*----------------------------------------------------------------------------------------------
	Callback function for the buttons on the dialog.
----------------------------------------------------------------------------------------------*/
LRESULT CALLBACK ButtonProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	static DrawType s_dt = kdtNoDraw;

	switch (uMsg)
	{
	case WM_KILLFOCUS:
		InvalidateRect(hwnd, NULL, FALSE);
		UpdateWindow(hwnd);
		g_hwndButtonIsDrawn = NULL;
		break;

	case WM_MOUSEMOVE:
		if (g_hwndButtonIsDrawn)
			break;
		// fall through

	case WM_SETFOCUS:
	case WM_LBUTTONUP:
		s_dt = kdtDrawUp;
		g_hwndButtonIsDrawn = hwnd;
		InvalidateRect(hwnd, NULL, FALSE);
		break;

	case WM_LBUTTONDOWN:
		s_dt = kdtDrawDown;
		g_hwndButtonIsDrawn = hwnd;
		InvalidateRect(hwnd, NULL, FALSE);
		break;

	case WM_PAINT:
		{
			if (!g_hwndButtonIsDrawn)
				s_dt = kdtNoDraw;
			PAINTSTRUCT ps;
			HDC hdc = BeginPaint(hwnd, &ps);
			int nOffset = s_dt == kdtDrawDown ? 4 : 3;
			SetBkColor(hdc, GetSysColor(COLOR_3DFACE));
			RECT rect = {0, 0, BUTTON_WIDTH, BUTTON_HEIGHT};
			ExtTextOut(hdc, 0, 0, ETO_OPAQUE, &rect, NULL, 0, NULL);

			if (GetWindowLong(hwnd, GWL_ID) < IDC_BUTTON_BROWSE1)
				BitBlt(hdc, nOffset, nOffset, 16, 15, g_hdcButton, 16, 0, SRCCOPY);
			else
				BitBlt(hdc, nOffset, nOffset, 16, 15, g_hdcButton, 0, 0, SRCCOPY);
			if (g_hwndButtonIsDrawn == hwnd)
				Draw3dRect(hdc, s_dt);
			else
				Draw3dRect(hdc, kdtNoDraw);
			EndPaint(hwnd, &ps);
			break;
		}
	}
	AssertPfn(g_lpfnOldButtonProc);
	return CallWindowProc(g_lpfnOldButtonProc, hwnd, uMsg, wParam, lParam);
}


/***********************************************************************************************
	MainDlg methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
MainDlg::MainDlg(char szFiles[NUM_COMBOS][MAX_PATH], bool fSilent, HINSTANCE hInstance)
{
	AssertArray((char *)szFiles, NUM_COMBOS * MAX_PATH);

	m_hwndDlg = NULL;
	m_fSilent = fSilent;
	m_hInstance = hInstance;
	memmove(m_szFiles, szFiles, sizeof(m_szFiles));
	memset(m_hwndCombos, 0, sizeof(m_hwndCombos));
	m_hdcButton = 0;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
MainDlg::~MainDlg()
{
	if (m_hdcButton)
	{
		DeleteObject(SelectObject(m_hdcButton, m_hbmpOld));
		DeleteDC(m_hdcButton);
	}
}

/*----------------------------------------------------------------------------------------------
	Method to create the dialog box.
----------------------------------------------------------------------------------------------*/
void MainDlg::Create()
{
	g_hdcButton = m_hdcButton = CreateCompatibleDC(NULL);
	m_hbmpOld = (HBITMAP)SelectObject(m_hdcButton, LoadBitmap(m_hInstance,
		MAKEINTRESOURCE(IDB_BUTTON)));
	DialogBoxParam(m_hInstance, MAKEINTRESOURCE(IDD_MAIN), NULL, MainDlgProc, (LPARAM)this);
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog box by reading settings from the registry. Also, override the
	default button window procedures.
----------------------------------------------------------------------------------------------*/
void MainDlg::Initialize(HWND hwndDlg)
{
	SetClassLong(hwndDlg, GCL_HICON, (long)LoadIcon(m_hInstance, MAKEINTRESOURCE(IDI_MAIN)));
	m_hwndDlg = hwndDlg;
	g_lpfnOldButtonProc = (WNDPROC)GetWindowLong(GetDlgItem(hwndDlg, IDC_BUTTON_BROWSE1),
		GWL_WNDPROC);
	for (int i = 0; i < NUM_COMBOS; i++)
	{
		m_hwndCombos[i] = GetDlgItem(hwndDlg, IDC_COMBO_DIR + i);
		SetWindowLong(GetDlgItem(hwndDlg, IDC_BUTTON_BROWSE1 + i), GWL_WNDPROC,
			(long)ButtonProc);
		if (i < NUM_COMBOS - 1)
		{
			SetWindowLong(GetDlgItem(hwndDlg, IDC_BUTTON_EDIT1 + i), GWL_WNDPROC,
				(long)ButtonProc);
		}
	}

	// Create tool tip for the buttons.
	HWND hwndTool = CreateWindowEx(0, TOOLTIPS_CLASS, NULL, WS_VISIBLE, 0, 0, 0, 0, hwndDlg, NULL, NULL, NULL);
	if (hwndTool)
	{
		TOOLINFO ti = {sizeof(ti), TTF_IDISHWND | TTF_SUBCLASS, hwndDlg};
		ti.lpszText = "Browse";
		for (int i = 0; i < NUM_COMBOS; i++)
		{
			ti.uId = (UINT)GetDlgItem(hwndDlg, IDC_BUTTON_BROWSE1 + i);
			SendMessage(hwndTool, TTM_ADDTOOL, 0, (LPARAM)&ti);
		}
		ti.lpszText = "View File";
		for (i = 0; i < NUM_COMBOS - 1; i++)
		{
			ti.uId = (UINT)GetDlgItem(hwndDlg, IDC_BUTTON_EDIT1 + i);
			SendMessage(hwndTool, TTM_ADDTOOL, 0, (LPARAM)&ti);
		}
	}

	// Read history information from the registry into the combo boxes
	HKEY hKey;
	if (0 != RegCreateKeyEx(HKEY_LOCAL_MACHINE, kpszSubKey, 0, NULL, 0, KEY_READ, NULL, &hKey,
		NULL))
	{
		return;
	}

	char * pszBuffer = new char[MAX_PATH * HISTORY_COUNT];
	if (pszBuffer)
	{
		char * pPos;
		DWORD dwLength;
		for (i = 0; i < NUM_COMBOS; i++)
		{
			dwLength = MAX_PATH * HISTORY_COUNT;
			if (0 == RegQueryValueEx(hKey, kpszHistoryValueNames[i], 0, NULL,
				(BYTE *)pszBuffer, &dwLength))
			{
				pPos = pszBuffer;
				while (*pPos)
				{
					SendMessage(m_hwndCombos[i], CB_ADDSTRING, 0, (LPARAM)pPos);
					pPos += strlen(pPos) + 1;
				}
			}
			SendMessage(m_hwndCombos[i], WM_SETTEXT, 0, (LPARAM)m_szFiles[i]);
			strcpy(pszBuffer, m_szFiles[i]);
			GetFullPathName(pszBuffer, MAX_PATH, m_szFiles[i], NULL);
		}
		delete pszBuffer;
	}
	RegCloseKey(hKey);
}

/*----------------------------------------------------------------------------------------------
	Save program settings to the registry.
----------------------------------------------------------------------------------------------*/
void MainDlg::SaveSettings()
{
	// Write history information to the registry from the combo boxes
	HKEY hKey;
	if (0 != RegCreateKeyEx(HKEY_LOCAL_MACHINE, kpszSubKey, 0, NULL, 0, KEY_WRITE, NULL,
		&hKey, NULL))
	{
		return;
	}

	char * pszBuffer = new char[MAX_PATH * HISTORY_COUNT];
	if (pszBuffer)
	{
		char * pPos;
		int cItems;
		for (int i = 0; i < NUM_COMBOS; i++)
		{
			pPos = pszBuffer;
			cItems = SendMessage(m_hwndCombos[i], CB_GETCOUNT, 0, 0);
			for (int j = 0; j < cItems; j++)
			{
				SendMessage(m_hwndCombos[i], CB_GETLBTEXT, j, (LPARAM) pPos);
				pPos += strlen(pPos) + 1;
			}
			*pPos = 0;
			RegSetValueEx(hKey, kpszHistoryValueNames[i], 0, REG_MULTI_SZ, (BYTE *)pszBuffer,
				pPos - pszBuffer + 1);
		}
	}
	RegCloseKey(hKey);
}

/*----------------------------------------------------------------------------------------------
	Perform operations based on WM_COMMAND messages.
----------------------------------------------------------------------------------------------*/
BOOL MainDlg::OnCommand(WPARAM wParam, LPARAM lParam)
{
	switch (LOWORD(wParam))
	{
	case IDCANCEL:
		SaveSettings();
		return EndDialog(m_hwndDlg, 0);

	case IDC_BUTTON_BROWSE1:
		{
			LPMALLOC pMalloc;
			SHGetMalloc(&pMalloc);
			if (pMalloc)
			{
				BROWSEINFO bi = {m_hwndDlg, NULL, m_szFiles[kfoWorkingDir],
					"Select the folder that should become the new working directory.",
					BIF_RETURNONLYFSDIRS, NULL, 0, 0};
				ITEMIDLIST * pidl = SHBrowseForFolder(&bi);
				if (pidl)
				{
					SHGetPathFromIDList(pidl, m_szFiles[kfoWorkingDir]);
					SendMessage(m_hwndCombos[kfoWorkingDir], WM_SETTEXT, 0,
						(LPARAM)m_szFiles[kfoWorkingDir]);
					SetCurrentDirectory(m_szFiles[kfoWorkingDir]);
					pMalloc->Free(pidl);
				}
				pMalloc->Release();
			}
			break;
		}

	case IDC_BUTTON_BROWSE2:
	case IDC_BUTTON_BROWSE3:
	case IDC_BUTTON_BROWSE4:
	case IDC_BUTTON_BROWSE5:
		{
			int iButton = LOWORD(wParam) - IDC_BUTTON_BROWSE1;
			static OPENFILENAME s_ofn = {sizeof(s_ofn), m_hwndDlg, m_hInstance};
			char szFilename[MAX_PATH];
			char szPath[MAX_PATH];
			s_ofn.lpstrFile = szFilename;
			s_ofn.lpstrInitialDir = szPath;
			s_ofn.nMaxFile = MAX_PATH;
			s_ofn.Flags = OFN_HIDEREADONLY;
			SendMessage(m_hwndCombos[iButton], WM_GETTEXT, MAX_PATH, (LPARAM)szFilename);
			SendMessage(m_hwndCombos[kfoWorkingDir], WM_GETTEXT, MAX_PATH, (LPARAM)szPath);
			GetFullPathName(szFilename, MAX_PATH, szFilename, NULL);
			if (GetOpenFileName(&s_ofn))
				SendMessage(m_hwndCombos[iButton], WM_SETTEXT, 0, (LPARAM)szFilename);
			break;
		}

	case IDC_BUTTON_EDIT1:
	case IDC_BUTTON_EDIT2:
	case IDC_BUTTON_EDIT3:
	case IDC_BUTTON_EDIT4:
		{
			if (!SetCurrentDirectory(m_szFiles[kfoWorkingDir]))
			{
				MessageBox(m_hwndDlg,
					"The working directory does not exist. Please enter a valid directory.",
					"UniConvert", MB_OK | MB_ICONINFORMATION);
				SetFocus(m_hwndCombos[kfoWorkingDir]);
				break;
			}
			char szFilename[MAX_PATH], szMessage[MAX_PATH << 1];
			SendMessage(m_hwndCombos[LOWORD(wParam) - IDC_BUTTON_EDIT1 + 1], WM_GETTEXT,
				MAX_PATH, (LPARAM)szFilename);
			GetFullPathName(szFilename, MAX_PATH, szFilename, NULL);
			int nResult = (int)ShellExecute(m_hwndDlg, "open", szFilename, NULL, NULL, SW_SHOW);
			if (nResult == 1 || nResult == 2)
			{
				sprintf(szMessage, "The file \"%s\" does not exist.", szFilename);
				MessageBox(m_hwndDlg, szMessage, "UniConvert", MB_OK);
			}
			else if (nResult <= 32)
			{
				sprintf(szMessage, "The file \"%s\" could not be opened.", szFilename);
				MessageBox(m_hwndDlg, szMessage, "UniConvert", MB_OK);
			}
			break;
		}

		case IDC_COMBO_DIR:
			if (HIWORD(wParam) == CBN_EDITCHANGE)
			{
				SendMessage(m_hwndCombos[kfoWorkingDir], WM_GETTEXT, MAX_PATH,
					(LPARAM)m_szFiles[kfoWorkingDir]);
			}
			break;

		case IDOK:
			return StartConvert();
	}
	return true;
}

bool MainDlg::StartConvert()
{
	SendMessage(m_hwndCombos[kfoWorkingDir], WM_GETTEXT, MAX_PATH,
		(LPARAM)m_szFiles[kfoWorkingDir]);
	SetCurrentDirectory(m_szFiles[kfoWorkingDir]);

	// Update m_szFiles with the final changes and make sure each combo box has
	// some text in it.
	for (int i = 1; i < NUM_COMBOS; i++)
	{
		SendMessage(m_hwndCombos[i], WM_GETTEXT, MAX_PATH, (LPARAM)m_szFiles[i]);
		if (*m_szFiles[i] == 0)
		{
			SetFocus(m_hwndCombos[i]);
			MessageBox(m_hwndDlg, kpszComboError[i], "UniConvert", MB_OK);
			return false;
		}
		GetFullPathName(m_szFiles[i], MAX_PATH, m_szFiles[i], NULL);
	}

	// Add the combo box text to the drop down list if it doesn't already exist
	char szBuffer[MAX_PATH];
	for (i = 0; i < NUM_COMBOS; i++)
	{
		SendMessage(m_hwndCombos[i], WM_GETTEXT, MAX_PATH, (LPARAM)szBuffer);
		int iFound = SendMessage(m_hwndCombos[i], CB_FINDSTRINGEXACT, -1, (LPARAM)m_szFiles[i]);
		if (iFound == CB_ERR)
			iFound = HISTORY_COUNT - 1;
		SendMessage(m_hwndCombos[i], CB_DELETESTRING, iFound, 0);
		SendMessage(m_hwndCombos[i], CB_INSERTSTRING, 0, (LPARAM)m_szFiles[i]);
		SendMessage(m_hwndCombos[i], WM_SETTEXT, 0, (LPARAM)szBuffer);
	}

	ConvertProcess * pcp = NewObj ConvertProcess(m_hwndDlg, m_hInstance);
	if (!pcp)
		return false;

	SetCursor(LoadCursor(NULL, IDC_WAIT));
	bool fSuccess = pcp->Process(m_szFiles, m_fSilent);
	delete pcp;
	SetCursor(LoadCursor(NULL, IDC_ARROW));
	return fSuccess;
}