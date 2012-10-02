#include <windows.h>
#include <stdio.h>
#include <time.h>
#include <CommCtrl.h>

#include "Dialogs.h"
#include "Globals.h"
#include "resource.h"


void CentralizeWindow(HWND hwnd)
// Move specified window to center of desktop.
{
	HWND desktop = GetDesktopWindow();
	RECT screenRect, dialogRect;
	GetClientRect(desktop, &screenRect);
	GetClientRect(hwnd, &dialogRect);
	int nScreenW = screenRect.right - screenRect.left;
	int nScreenH = screenRect.bottom - screenRect.top;
	int nDialogW = dialogRect.right - dialogRect.left;
	int nDialogH = dialogRect.bottom - dialogRect.top;
	SetWindowPos(hwnd, HWND_TOP, (nScreenW - nDialogW)/2, (nScreenH - nDialogH)/2,
		nDialogW, nDialogH, SWP_NOSIZE);
}

int GetCurrentChangelistId(HWND hwndParent)
{
	HWND hwndCtrl = GetDlgItem(hwndParent, IDC_CBB_CHANGELIST);
	int cch = SendMessage(hwndCtrl, WM_GETTEXTLENGTH, 0, 0);
	char * pszChangeList = new char [cch + 1];
	SendMessage(hwndCtrl, WM_GETTEXT, cch + 1, (LPARAM)(pszChangeList));
	int nResult = atoi(pszChangeList);
	delete[] pszChangeList;
	return nResult;
}

INT_PTR CALLBACK DlgProcUserInfo(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
// The dialog procedure for collecting user info.
{
	switch(msg)
	{
	case WM_INITDIALOG: // Dialog is being shown.
		{
			CentralizeWindow(hwnd);
			SendMessage(hwnd, WM_SETTEXT, 0, (LPARAM)"Check-in details");

			// Fill in controls with default values:
			::SendDlgItemMessage(hwnd, IDC_EDIT_NAME, WM_SETTEXT, 0, (LPARAM)gpszUser);
			::SendDlgItemMessage(hwnd, IDC_EDIT_DATE, WM_SETTEXT, 0, (LPARAM)gpszCheckinDate);
			::SendDlgItemMessage(hwnd, IDC_EDIT_ROOT, WM_SETTEXT, 0, (LPARAM)gpszRoot);
			::SendDlgItemMessage(hwnd, IDC_CB_AUTORESOLVE, BM_SETCHECK, gfAutoResolve? BST_CHECKED : BST_UNCHECKED, 0);
			// Add changelist numbers to combo box:
			::SendDlgItemMessage(hwnd, IDC_CBB_CHANGELIST, CB_ADDSTRING, 0, (LPARAM)"Default");
			::SendDlgItemMessage(hwnd, IDC_CBB_CHANGELIST, CB_SETCURSEL, 0, 0);
			for (int i = 0; i < gcclChangeLists; i++)
			{
				char szBuf[20];
				sprintf(szBuf, "%d", gpnChangeLists[i]);
				::SendDlgItemMessage(hwnd, IDC_CBB_CHANGELIST, CB_ADDSTRING, 0, (LPARAM)szBuf);
			}
		}
		break;

	case WM_COMMAND: // We got a message from a control.
		switch(LOWORD(wParam))
		{
		case IDOK:
			{ // New scope
				HWND hwndCtrl = GetDlgItem(hwnd, IDC_EDIT_COMMENT);
				int cch = SendMessage(hwndCtrl, WM_GETTEXTLENGTH, 0, 0);
				delete[] gpszCheckinComment;
				gpszCheckinComment = new char [cch + 1];
				SendMessage(hwndCtrl, WM_GETTEXT, cch + 1, (LPARAM)(gpszCheckinComment));

				hwndCtrl = GetDlgItem(hwnd, IDC_EDIT_NAME);
				cch = SendMessage(hwndCtrl, WM_GETTEXTLENGTH, 0, 0);
				delete[] gpszCheckinUser;
				gpszCheckinUser = new char [cch + 1];
				SendMessage(hwndCtrl, WM_GETTEXT, cch + 1, (LPARAM)(gpszCheckinUser));

				hwndCtrl = GetDlgItem(hwnd, IDC_EDIT_DATE);
				cch = SendMessage(hwndCtrl, WM_GETTEXTLENGTH, 0, 0);
				delete[] gpszCheckinDate;
				gpszCheckinDate = new char [cch + 1];
				SendMessage(hwndCtrl, WM_GETTEXT, cch + 1, (LPARAM)(gpszCheckinDate));

				hwndCtrl = GetDlgItem(hwnd, IDC_EDIT_ROOT);
				cch = SendMessage(hwndCtrl, WM_GETTEXTLENGTH, 0, 0);
				delete[] gpszRoot;
				gpszRoot = new char [cch + 1];
				SendMessage(hwndCtrl, WM_GETTEXT, cch + 1, (LPARAM)(gpszRoot));

				hwndCtrl = GetDlgItem(hwnd, IDC_CB_AUTORESOLVE);
				gfAutoResolve = (SendMessage(hwndCtrl, BM_GETCHECK, 0, 0) == BST_CHECKED);

				gnCheckinChangeList = GetCurrentChangelistId(hwnd);
			}
			EndDialog(hwnd, 1);
			break;

		case IDCANCEL:
			// If there is no cancel button, user may have pressed the X on the top right, or
			// used ALT+F4:
			EndDialog(hwnd, 0);
			break;

		case IDC_CBB_CHANGELIST:
			switch (HIWORD(wParam))
			{
			case CBN_SELCHANGE:
				{ // New Scope
					int iSel = ::SendDlgItemMessage(hwnd, IDC_CBB_CHANGELIST, CB_GETCURSEL, 0, 0);
					// Skip "default" text:
					iSel--;
					if (iSel >= 0 && iSel < gcclChangeLists)
					{
						::SendDlgItemMessage(hwnd, IDC_EDIT_COMMENT, WM_SETTEXT, 0,
							(LPARAM)(gppszChangeListsComments[iSel]));
					}
					else
						::SendDlgItemMessage(hwnd, IDC_EDIT_COMMENT, WM_SETTEXT, 0, (LPARAM)"");
					break;
				}
			case CBN_EDITCHANGE:
				{ // New Scope
					// User has selected different changelist, so update comment, if one exists:
					int nChangelist = GetCurrentChangelistId(hwnd);
					bool fAlteredComment = false;
					if (nChangelist)
					{
						for (int i = 0; i < gcclChangeLists; i++)
						{
							if (nChangelist == gpnChangeLists[i])
							{
								if ((gppszChangeListsComments[i]))
								{
									::SendDlgItemMessage(hwnd, IDC_EDIT_COMMENT, WM_SETTEXT, 0,
										(LPARAM)(gppszChangeListsComments[i]));
									fAlteredComment = true;
									break;
								}
							}
						}
					}
					if (!fAlteredComment)
						::SendDlgItemMessage(hwnd, IDC_EDIT_COMMENT, WM_SETTEXT, 0, (LPARAM)"");
					break;
				}
			}
		}
		break;

	default: // All the messages we don't handle are handled by Windows.
		return 0;
	}
	return 1; // This means we have processed the message.
}
