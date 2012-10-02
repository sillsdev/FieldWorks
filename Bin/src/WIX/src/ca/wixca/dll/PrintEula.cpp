//-------------------------------------------------------------------------------------------------
// <copyright file="PrintEula.cpp" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
//    Functionality to print Eula
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// Constants
LPCWSTR vcsEulaQuery = L"SELECT `Text` FROM `Control` WHERE `Control`='LicenseText'";


enum eEulaQuery { eqText = 1};
const int IDM_POPULATE = 100;
const int IDM_PRINT = 101;
const int CONTROL_X_COORDINATE = 0;
const int CONTROL_Y_COORDINATE = 0;
const int CONTROL_WIDTH = 500;
const int CONTROL_HEIGHT = 500;
const int ONE_INCH = 1440; // 1440 TWIPS = 1 inch.
const int TEXT_RECORD_POS = 1;
const int STRING_CAPACITY = 512;
const int NO_OF_COPIES = 1;

//Forward declarations of functions, check the function definitions for the comments
static LRESULT CALLBACK WndProc(__in HWND hWnd, __in UINT message, __in WPARAM wParam, __in LPARAM lParam);
static HRESULT DisplayPrintDialog(__inout PRINTDLGEXW* pPdlgex);
static HRESULT ReadEulaText(__in MSIHANDLE hInstall, __out LPSTR* ppszEulaText);
static DWORD CALLBACK ReadStreamCallback(__in DWORD Cookie, __out LPBYTE pbBuff, __in LONG cb, __out LONG FAR *pcb);
static HRESULT CreateRichTextWindow(__out HWND* phWndMain);
static HRESULT PrintRichText(__in HWND hWndMain);
static void Print(__in_opt HWND hWnd);
static void LoadEulaText(__in_opt HWND hWnd);
static void ShowErrorMessage(__in HRESULT hr);

//Global variables
PRINTDLGEXW vPrintDlg; //Parameters for print (needed on both sides of WndProc callbacks)
LPSTR vpszEulaText = NULL;
HRESULT vhr = S_OK; //Global hr, used by the functions called from WndProc to set errorcode


/********************************************************************
 PrintEula - Custom Action entry point

********************************************************************/
extern "C" UINT __stdcall PrintEula(MSIHANDLE hInstall)
{
	//AssertSz(FALSE, "Debug PrintEula");

	HRESULT hr = S_OK;
	HWND hWndMain = NULL;
	HMODULE hRichEdit = NULL;

	hr = WcaInitialize(hInstall, "PrintEula");
	ExitOnFailure(hr, "failed to initialize");

	// Display the print dialog
	hr = DisplayPrintDialog(&vPrintDlg);
	ExitOnFailure(hr, "Failed to show print dialog");

	// If they said they want to print
	if (vPrintDlg.dwResultAction == PD_RESULT_PRINT )
	{
		// Get the stream for Eula
		hr = ReadEulaText(hInstall, &vpszEulaText);
		ExitOnFailure(hr, "failed to read Eula text from MSI database");

		// Have to load Rich Edit since we'll be creating a Rich Edit control in the window
		hRichEdit = ::LoadLibraryW(L"Riched20.dll");
		if (NULL == hRichEdit)
			ExitOnLastError(hr, "failed to load rich edit 2.0 library");

		hr = CreateRichTextWindow(&hWndMain);
		ExitOnFailure(hr, "failed to create rich text window for printing");

		hr = PrintRichText(hWndMain);
		if (FAILED(hr)) // Since we've already shown the print dialog, we better show them a dialog explaining why it didn't print
			ShowErrorMessage(hr);
	}

LExit:
	if (NULL != hRichEdit)
		::FreeLibrary(hRichEdit);

	ReleaseStr(vpszEulaText);

	// Always return success since we dont want to stop the
	// installation even if the Eula printing fails.
	// TODO: can't we make this a type 'continue' action?
	return WcaFinalize(ERROR_SUCCESS);
}



/********************************************************************
CreateRichTextWindow - Creates Window and Child RichText control.

********************************************************************/
HRESULT CreateRichTextWindow(
	__out HWND* phWndMain
	)
{
	HRESULT hr = S_OK;
	HWND hWndMain = NULL;
	WNDCLASSEXW wcex;

	//
	// Register the window class
	//
	wcex.cbSize = sizeof(WNDCLASSEXW);
	wcex.style = CS_HREDRAW | CS_VREDRAW;
	wcex.lpfnWndProc = (WNDPROC)WndProc;
	wcex.cbClsExtra = 0;
	wcex.cbWndExtra = 0;
	wcex.hInstance = NULL;
	wcex.hIcon = NULL;
	wcex.hCursor = LoadCursor(NULL, IDC_ARROW);
	wcex.hbrBackground = (HBRUSH)(COLOR_BACKGROUND+1);
	wcex.lpszMenuName = NULL;
	wcex.lpszClassName = L"PrintEulaRichText";
	wcex.hIconSm = NULL;

	if (0 == ::RegisterClassExW(&wcex))
	{
		DWORD  dwResult = ::GetLastError();
		// If we get "Class already exists" error ignore it.
		// We might encounter this when the user tries to print more than
		// once in the same setup instance
		if (dwResult != ERROR_CLASS_ALREADY_EXISTS)
		{
			ExitOnFailure(hr = HRESULT_FROM_WIN32(dwResult), "failed to register window class");
		}
	}

	// Perform application initialization:
	hWndMain = ::CreateWindowW(L"PrintEulaRichText", NULL, WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, 0, CW_USEDEFAULT, 0, NULL, NULL, NULL, NULL);
	ExitOnNullWithLastError(hWndMain, hr, "failed to create window for printing");

	::ShowWindow(hWndMain, SW_HIDE);
	if (!::UpdateWindow(hWndMain))
		ExitOnLastError(hr, "failed to update window");

	*phWndMain = hWndMain;

LExit:
	return hr;
}


/********************************************************************
 PrintRichText - Sends messages to load the Eula text, print it, and
 close the window.

 NOTE: Returns errors that have occured while attempting to print,
 which were saved in vhr by the print callbacks.
********************************************************************/
HRESULT PrintRichText(
	__in HWND hWndMain
	)
{
	MSG msg;

	// Populate the RichEdit control
	::SendMessageW(hWndMain, WM_COMMAND, IDM_POPULATE, 0);

	// Print Eula
	::SendMessageW(hWndMain, WM_COMMAND, IDM_PRINT, 0);

	// Done! Lets close the Window
	::SendMessage(hWndMain, WM_CLOSE, 0, 0);
	// Main message loop:
	while (::GetMessageW(&msg, NULL, 0, 0))
	{
//		if (!::TranslateAcceleratorW(msg.hwnd, NULL, &msg))
//		{
//			::TranslateMessage(&msg);
//			::DispatchMessageW(&msg);
//		}
	}


	// return any errors encountered in the print callbacks
	return vhr;
}


/********************************************************************
 WndProc - Windows callback procedure

********************************************************************/
LRESULT CALLBACK WndProc(
	__in HWND hWnd,
	__in UINT message,
	__in WPARAM wParam,
	__in LPARAM lParam
	)
{
	static HWND hWndRichEdit = NULL;
	int wmId, wmEvent;
	PAINTSTRUCT ps;
	HDC hdc;

	switch (message)
	{
	case WM_CREATE:
		hWndRichEdit = ::CreateWindowExW(WS_EX_CLIENTEDGE, RICHEDIT_CLASSW, L"", ES_MULTILINE | WS_CHILD | WS_VISIBLE | WS_VSCROLL, CONTROL_X_COORDINATE, CONTROL_Y_COORDINATE, CONTROL_WIDTH, CONTROL_HEIGHT, hWnd, NULL, NULL, NULL);
		break;
	case WM_COMMAND:
		wmId = LOWORD(wParam);
		wmEvent = HIWORD(wParam);
		switch (wmId)
		{
		case IDM_POPULATE:
			LoadEulaText(hWndRichEdit);
			break;
		case IDM_PRINT:
			Print(hWndRichEdit);
			break;
		default:
			return ::DefWindowProcW(hWnd, message, wParam, lParam);
			break;
		}
	case WM_PAINT:
		hdc = ::BeginPaint(hWnd, &ps);
		::EndPaint(hWnd, &ps);
		break;
	case WM_DESTROY:
		::PostQuitMessage(0);
		break;
	default:
		return ::DefWindowProcW(hWnd, message, wParam, lParam);
	}

	return 0;
}


/********************************************************************
 ReadStreamCallback - Callback function to read data to the RichText control

 NOTE: Richtext control uses this function to read data from the buffer
********************************************************************/
DWORD CALLBACK ReadStreamCallback(
	__in DWORD Cookie,
	__out LPBYTE pbBuff,
	__in LONG cb,
	__out LONG FAR *pcb
	)
{
	static LPCSTR pszTextBuf = NULL;
	DWORD er = 0;

	// If it's null set it to the beginning of the EULA buffer
	if (pszTextBuf == NULL)
	{
		pszTextBuf = vpszEulaText;
	}

	LONG lTextLength = (LONG)lstrlen(pszTextBuf);

	// If the size to be written is less than then length of the buffer, write the rest
	if (lTextLength < cb )
	{
		*pcb = lTextLength;
		memcpy(pbBuff, pszTextBuf, *pcb);
		pszTextBuf = NULL;
	}
	else // Only write the amount being asked for and move the pointer along
	{
		*pcb = cb;
		memcpy(pbBuff, pszTextBuf, *pcb);
		pszTextBuf = pszTextBuf +  cb;
	}

	return er;
}


/********************************************************************
 LoadEulaText - Reads data for Richedit control

********************************************************************/
void LoadEulaText(
	__in HWND hWnd
	)
{
	DWORD dwError = ERROR_SUCCESS;

	ExitOnNull(hWnd, dwError, ERROR_INVALID_HANDLE, "Invalid Handle passed to LoadEulaText");
	EDITSTREAM es;
	::ZeroMemory(&es, sizeof(es));
	es.pfnCallback = (EDITSTREAMCALLBACK)ReadStreamCallback;
	es.dwCookie = (DWORD)0;
	::SendMessageW(hWnd, EM_STREAMIN, SF_RTF, (LPARAM)&es);

	if (es.dwError != 0)
	{
		ExitOnLastError(es.dwError, "failed to load the EULA into the control");
	}

LExit:
	vhr = es.dwError;
}


/********************************************************************
 DisplayPrintDialog - Display the printer selection dialog

 NOTE: pPdlgex.dwResultAction should be checked for dialog result
********************************************************************/
HRESULT DisplayPrintDialog(
	__inout PRINTDLGEXW* pPrintDlg
	)
{
	HWND hWnd = ::GetForegroundWindow();

	// Initialize the PRINTDLGEX structure.
	::ZeroMemory(pPrintDlg, sizeof(*pPrintDlg));

	pPrintDlg->lStructSize = sizeof(PRINTDLGEX);
	pPrintDlg->hwndOwner = hWnd;
	pPrintDlg->Flags = PD_RETURNDC | PD_COLLATE | PD_NOCURRENTPAGE | PD_ALLPAGES | PD_NOPAGENUMS | PD_NOSELECTION;
	pPrintDlg->lpPageRanges  = NULL;
	pPrintDlg->nCopies = NO_OF_COPIES;
	pPrintDlg->nStartPage = START_PAGE_GENERAL;

	// Invoke the Print property sheet.
	return ::PrintDlgExW(pPrintDlg);
}


/********************************************************************
 ReadEulaText - Reads Eula text from the MSI

********************************************************************/
HRESULT ReadEulaText(
	__in MSIHANDLE hInstall,
	__out LPSTR* ppszEulaText
	)
{
	HRESULT hr = S_OK;
	PMSIHANDLE hDB;
	PMSIHANDLE hView;
	PMSIHANDLE hRec;
	LPWSTR pwzEula = NULL;
	DWORD cchEula = 0;

	char szTemp[1] = {0};
	DWORD len = 1;

	hr = WcaOpenExecuteView(vcsEulaQuery, &hView);
	ExitOnFailure(hr, "failed to open and execute view for PrintEula query");

	hr = WcaFetchSingleRecord(hView, &hRec);
	ExitOnFailure(hr, "failed to fetch the row containing the LicenseText");

	hr = WcaGetRecordString(hRec, 1, &pwzEula);
	ExitOnFailure(hr, "failed to get LicenseText in PrintEula");

	hr = StrAnsiAllocString(ppszEulaText, pwzEula, 0, CP_ACP);
	ExitOnFailure(hr, "failed to convert LicenseText to ANSI code page");

LExit:
	return hr;
}


/********************************************************************
 Print - Function that sends the data from richedit control to the printer

 NOTE: Any errors encountered are saved to the vhr variable
********************************************************************/
void Print(
	__in_opt HWND hRtfWnd
	)
{

	HRESULT hr = S_OK;
	FORMATRANGE fRange;
	GETTEXTLENGTHEX gTxex;
	HDC hPrinterDC = vPrintDlg.hDC;
	int nHorizRes = ::GetDeviceCaps(hPrinterDC, HORZRES);
	int nVertRes = ::GetDeviceCaps(hPrinterDC, VERTRES);
	int nLogPixelsX = ::GetDeviceCaps(hPrinterDC, LOGPIXELSX);
	int nLogPixelsY = ::GetDeviceCaps(hPrinterDC, LOGPIXELSY);
	LONG_PTR lTextLength; // Length of document.
	LONG_PTR lTextPrinted; // Amount of document printed.
	DOCINFOW dInfo;
	LPDEVNAMES pDevnames;

	// Ensure the printer DC is in MM_TEXT mode.
	if (0 == ::SetMapMode ( hPrinterDC, MM_TEXT ))
		ExitOnLastError(hr, "failed to set map mode");

	// Rendering to the same DC we are measuring.
	::ZeroMemory(&fRange, sizeof(fRange));
	fRange.hdc = fRange.hdcTarget = hPrinterDC;

	// Set up the page.
	fRange.rcPage.left = fRange.rcPage.top = 0;
	fRange.rcPage.right = (nHorizRes/nLogPixelsX) * ONE_INCH;
	fRange.rcPage.bottom = (nVertRes/nLogPixelsY) * ONE_INCH;

	// Set up 1" margins all around.
	fRange.rc.left = fRange.rcPage.left + ONE_INCH;
	fRange.rc.top = fRange.rcPage.top + ONE_INCH;
	fRange.rc.right = fRange.rcPage.right - ONE_INCH;
	fRange.rc.bottom = fRange.rcPage.bottom - ONE_INCH;

	// Default the range of text to print as the entire document.
	fRange.chrg.cpMin = 0;
	fRange.chrg.cpMax = -1;

	// Set up the print job (standard printing stuff here).
	::ZeroMemory(&dInfo, sizeof(dInfo));
	dInfo.cbSize = sizeof(DOCINFO);
	dInfo.lpszDocName = L"";

	pDevnames = (LPDEVNAMES)::GlobalLock(vPrintDlg.hDevNames);
	ExitOnNullWithLastError(pDevnames, hr, "failed to get global lock");

	dInfo.lpszOutput  = (LPWSTR)pDevnames + pDevnames->wOutputOffset;

	if (0 == ::GlobalUnlock(pDevnames))
		ExitOnLastError(hr, "failed to release global lock");

	// Start the document.
	if (0 >= ::StartDocW(hPrinterDC, &dInfo))
		ExitOnLastError(hr, "failed to start print document");

	::ZeroMemory(&gTxex, sizeof(gTxex));
	gTxex.flags = GTL_NUMCHARS;
	lTextLength = ::SendMessageW(hRtfWnd, EM_GETTEXTLENGTHEX , (LONG_PTR)&gTxex, 0);

	do
	{
		// Start the page.
		if (0 >= ::StartPage(hPrinterDC))
			ExitOnLastError(hr, "failed to start print page");

		// Print as much text as can fit on a page. The return value is
		// the index of the first character on the next page. Using TRUE
		// for the wParam parameter causes the text to be printed.
		lTextPrinted = ::SendMessageW(hRtfWnd, EM_FORMATRANGE, TRUE, (LPARAM)&fRange);

		// Print last page.
		if (0 >= ::EndPage(hPrinterDC))
			ExitOnLastError(hr, "failed to end print page");

		// If there is more text to print, adjust the range of characters
		// to start printing at the first character of the next page.
		if (lTextPrinted < lTextLength)
		{
			fRange.chrg.cpMin = (LONG)lTextPrinted;
			fRange.chrg.cpMax = -1;
		}
	}
	while (lTextPrinted < lTextLength);

	// Tell the control to release cached information.
	::SendMessageW(hRtfWnd, EM_FORMATRANGE, 0, (LPARAM)NULL);
	if (0 >= ::EndDoc(hPrinterDC))
		ExitOnLastError(hr, "failed to end print document");

LExit:
	vhr = hr;
}


/********************************************************************
 ShowErrorMessage - Display MessageBox showing the message for hr.

********************************************************************/
void ShowErrorMessage(
	__in HRESULT hr
	)
{
	WCHAR wzMsg[STRING_CAPACITY];
	if (0 != ::FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM, 0, hr, 0, wzMsg, countof(wzMsg), 0))
	{
		HWND hWnd = ::GetForegroundWindow();
		::MessageBoxW(hWnd, wzMsg, L"PrintEULA", MB_OK | MB_ICONWARNING);
	}
}
