/*

File to manage status dialog box of an application that otherwise would not have a window.

Normal usage:
{
	ShowStatusDialog();
	...
	AppendStatusText(_T("Some progress message"));
	...
	AppendStatusText(_T("\r\nAnother message on a new line"));
	...
	HideStatusDialog();
}
The dialog itself is created in a new thread and run modally in that thread. It is possible
for a user to "cancel" the dialog box. To test for this, periodically call IfStopRequested(),
which will return true if the user canceled. The process of canceling will cause a confirmation
message to be displayed, which the user will have to accept or reject. While this is pending,
a call to IfStopRequested() will not return, so the main task gets halted while the user makes
up their mind.

*/

#include <windows.h>

#include "StatusDialog.h"
#include "StringFunctions.h"
#include "resource.h"

static _TCHAR * pszStatus = NULL; // Complete status report so far
static HWND hwndStatusDialog = NULL; // Handle of Status dialog window
static bool fContinueWithoutStatusDialog = false; // Flag for when StatusDialog fails
static HANDLE hCreateStatusDlg = NULL; // Event used to synchronize threads on dialog creation.
static HANDLE hDestroyStatusDlg = NULL; // Event used to sync threads on dialog destruction.
static const _TCHAR * kszStatusDlgMutexName = _T("SIL FW File Decompressor Status Dialog");
static HANDLE hStopRequestInProgress = NULL;
static bool fStopRequested = false;
static bool fSuppressAutoScroll = false;
static int nErrors = 0;

INT_PTR CALLBACK DlgProcStatus(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
// The dialog procedure for displaying current status.
{
	switch(msg)
	{
	case WM_INITDIALOG: // Dialog is being shown.
		{
			hwndStatusDialog = hwnd;

			// Set Icon:
			HICON hIcon = LoadIcon(GetModuleHandle(NULL), (LPCTSTR)IDR_MAIN_ICON);
			if (hIcon)
			{
				SendMessage(hwnd, WM_SETICON, 1, (LPARAM)hIcon);
				SendMessage(hwnd, WM_SETICON, 0, (LPARAM)hIcon);
				DestroyIcon(hIcon);
				hIcon = NULL;
			}

			// Signal that our window is created:
			if (hCreateStatusDlg)
				::SetEvent(hCreateStatusDlg);
		}
		break;

	case WM_COMMAND: // We got a message from a control/menu - in this case, a button.
		switch(LOWORD(wParam))
		{
		case IDCANCEL: // User pressed either Quit, the X on the top right, or used ALT+F4

			// Make sure main thread does not advance far until user has decided whether
			// or not to confirm the stop request:
			hStopRequestInProgress = CreateEvent(NULL, true, false, NULL);

			const int cchMsg = 256;
			_TCHAR pszMsg[cchMsg];
			_TCHAR pszTitle[cchMsg];
			LoadString(GetModuleHandle(NULL), IDS_QUIT_MSG, pszMsg,
				sizeof(pszMsg)/sizeof(TCHAR));
			LoadString(GetModuleHandle(NULL), IDS_QUIT_TITLE, pszTitle,
				sizeof(pszTitle)/sizeof(TCHAR));
			if (MessageBox(hwnd, pszMsg, pszTitle, MB_YESNO | MB_ICONSTOP | MB_DEFBUTTON2)
				== IDYES)
			{
				fStopRequested = true;
				// TODO: Add text to status saying we're quitting
				EnableWindow(GetDlgItem(hwnd, IDCANCEL), false);
			}
			// We're done with the user's confirmation, so release the event handle:
			if (hStopRequestInProgress)
			{
				SetEvent(hStopRequestInProgress);
				CloseHandle(hStopRequestInProgress);
			}
			hStopRequestInProgress = NULL;
			break;
		}
		break;

	case IDM_USERMSG_QUIT: // Sent from another thread to force this dialog to quit.
		EndDialog(hwnd, 0);
		break;

	case WM_DESTROY: // Dialog is off the screen by now.
		// Signal that our window is destroyed:
		::SetEvent(hDestroyStatusDlg);
		break;

	default: // All the messages we don't handle are handled by Windows.
		return 0;
	}
	return 1; // This means we have processed the message.
}

// Deals with the failure to create a status dialog.
void StatusDialogCreateFailed()
{
	fContinueWithoutStatusDialog = true;

	// Signal (falsely) that our window is created, so we can continue:
	if (hCreateStatusDlg)
		::SetEvent(hCreateStatusDlg);
}

// Creates a modal dialog in a new thread.
DWORD WINAPI StatusDlgThreadEntry(LPVOID)
{
	if (DialogBoxParam(GetModuleHandle(NULL), MAKEINTRESOURCE(IDD_DIALOG_STATUS), NULL,
		DlgProcStatus, 0) == -1)
	{
		StatusDialogCreateFailed();
	}
	return 0;
}

void DisplayStatusText()
{
	if (!pszStatus)
	{
		SendDlgItemMessage(hwndStatusDialog, IDC_EDIT_STATUS, WM_SETTEXT, 0,
			(LPARAM)_T(""));
		return;
	}

	// Put the message in the dialog:
	if (hwndStatusDialog)
	{
		SendDlgItemMessage(hwndStatusDialog, IDC_EDIT_STATUS, WM_SETTEXT, 0,
			(LPARAM)pszStatus);
		if (!fSuppressAutoScroll)
			SendDlgItemMessage(hwndStatusDialog, IDC_EDIT_STATUS, WM_VSCROLL, SB_BOTTOM, 0);
	}
}

// Create and show the Status dialog. Returns the HWND of the static text control.
void ShowStatusDialog()
{
	if (hwndStatusDialog || fContinueWithoutStatusDialog)
		return; // Dialog already present, or we can't create one.

	// It is possible for the Status Dialog to be shown and hidden via another thread.
	// We don't want other threads interfering with this creation, so we'll use a mutex:
	HANDLE hMutex = ::CreateMutex(NULL, false, kszStatusDlgMutexName);

	// Check that the mutex handle didn't already exist:
	if (ERROR_ALREADY_EXISTS == ::GetLastError())
	{
		// Mutex does already exist, so we'll stop our creation:
		::CloseHandle(hMutex);
		hMutex = NULL;
		return;
	}

	// Create an Event, initially reset so we wait until the window handle is created:
	hCreateStatusDlg = CreateEvent(NULL, true, false, NULL);

	// Create new thread to process dialog message queue independently:
	DWORD nThreadId; // MSDN says you can pass NULL instead of this, but you can't on Win98.
	HANDLE hThread = CreateThread(NULL, 0, StatusDlgThreadEntry, NULL, 0, &nThreadId);

	// Wait upto 15 seconds or until window has a handle:
	if (WaitForSingleObject(hCreateStatusDlg, 15000/*INFINITE*/) == WAIT_TIMEOUT)
	{
		StatusDialogCreateFailed();
	}
	// Restore status texts:
	DisplayStatusText();

	CloseHandle(hCreateStatusDlg);
	hCreateStatusDlg = NULL;

	// Release mutex:
	::CloseHandle(hMutex);
	hMutex = NULL;
}

// Appends an error message to the status window. Regular C string formatting is allowed.
void LogError(const _TCHAR * pszFormat, ...)
{
	// We will be passing on the variable arguments to the new_vsprintf() function:
	va_list arglist;
	va_start(arglist, pszFormat);

	_TCHAR * pszMsg = new_vsprintf(pszFormat, arglist);

	AppendStatusText(_T("\r\n"));
	AppendStatusText(pszMsg);
	AppendStatusText(_T("\r\n"));

	delete[] pszMsg;
	nErrors++;
}

// Appends an message to the status window. Regular C string formatting is allowed.
void AppendStatusText(const _TCHAR * pszFormat, ...)
{
	// We will be passing on the variable arguments to the new_vsprintf() function:
	va_list arglist;
	va_start(arglist, pszFormat);

	new_vsprintf_concat(pszStatus, 0, pszFormat, arglist);

	DisplayStatusText();
}

void HideStatusDialog()
{
	if (!hwndStatusDialog)
		return; // Dialog already absent.

	// It is possible for the Status Dialog to be shown and hidden via another thread.
	// We don't want other threads interfering with this destruction, so we'll use a mutex:
	HANDLE hMutex = ::CreateMutex(NULL, false, kszStatusDlgMutexName);

	// Check that the mutex handle didn't already exist:
	if (ERROR_ALREADY_EXISTS == ::GetLastError())
	{
		// Mutex does already exist, so we'll stop our destruction:
		::CloseHandle(hMutex);
		hMutex = NULL;
		return;
	}

	// Create an Event, initially reset so we wait until the window handle is destroyed:
	hDestroyStatusDlg = CreateEvent(NULL, true, false, NULL);

	SendMessage(hwndStatusDialog, IDM_USERMSG_QUIT, 0, 0);

	// Wait until window is destroyed:
	WaitForSingleObject(hDestroyStatusDlg, INFINITE);

	CloseHandle(hDestroyStatusDlg);
	hDestroyStatusDlg = NULL;

	hwndStatusDialog = NULL;

	// Release mutex:
	::CloseHandle(hMutex);
	hMutex = NULL;
}

bool IfStopRequested()
{
	if (hStopRequestInProgress)
		WaitForSingleObject(hStopRequestInProgress, INFINITE);

	return fStopRequested;
}

void KillStatusDialog()
{
	HideStatusDialog();
	delete[] pszStatus;
	pszStatus = NULL;
}

// Writes the given text to the Clipboard
bool WriteLogToClipboard()
{
	int nLen = (1 + (int)_tcslen(pszStatus)) * sizeof(_TCHAR);
	int nRet;

	// Open clipboard for our use:
	if (!OpenClipboard(hwndStatusDialog))
		return false;

	if (!EmptyClipboard())
	{
		nRet = GetLastError();
		return false;
	}

	HGLOBAL hglbCopy = GlobalAlloc(GMEM_MOVEABLE, nLen);
	if (hglbCopy == NULL)
	{
		CloseClipboard();
		return false;
	}

	// Lock the handle and copy the text to the buffer.
	LPVOID lptstrCopy = GlobalLock(hglbCopy);
	memcpy(lptstrCopy, pszStatus, nLen);
	GlobalUnlock(hglbCopy);

	// Place the handle on the clipboard.
#ifdef UNICODE
	if (!SetClipboardData(CF_UNICODETEXT, hglbCopy))
#else
	if (!SetClipboardData(CF_TEXT, hglbCopy))
#endif
	{
		nRet = GetLastError();
		CloseClipboard();
		return false;
	}

	if (!CloseClipboard())
	{
		nRet = GetLastError();
		return false;
	}

	return true;
}

void CopyErrorsToClipboard()
{
	if (nErrors > 0)
	{
		if (::MessageBox(NULL,
			_T("There was at least one error. Would you like the whole log to be copied to the clipboard?"),
			_T("Operation was not successful"), MB_ICONQUESTION | MB_YESNO | MB_SYSTEMMODAL)
				== IDYES)
		{
			if (!WriteLogToClipboard())
			{
				::MessageBox(NULL, _T("Error - could not write to clipboard."),
					_T("Clipboard Error"), MB_ICONINFORMATION | MB_OK);
			}
		}
	}
}