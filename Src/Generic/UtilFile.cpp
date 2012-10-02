/*----------------------------------------------------------------------------------------------
Copyright 2002, SIL International. All rights reserved.

File: UtilFile.cpp
Responsibility: Alistair Imrie
Last reviewed: Never

	File utilities implementation.
----------------------------------------------------------------------------------------------*/

#ifdef WIN32
#include "UtilFile.h"
#include <direct.h>

//:>********************************************************************************************
//:>	FolderSelectDlg Methods.
//:>*******************************************************************************************/

const achar * FolderSelectDlg::krgchDummyName = _T("FieldWorks_Dummy_File");

/*----------------------------------------------------------------------------------------------
	Called from outside the class to instantiate and run the folder chooser dialog.
	@param hwndParent [in] Handle of window to be used as dialog parent.
	@param strbFolder [in, out] Default folder. User's choice is returned in this parameter.
	@param ridTitle Resource id of string for dialog title.
	@param pszFilter [in] File filter to determine which files may be shown to user.
	&return True if user picked anything, False if they canceled.
----------------------------------------------------------------------------------------------*/
bool FolderSelectDlg::ChooseFolder(HWND hwndParent, StrAppBuf & strbPath, int ridTitle,
								   const achar * pszFilter)
{
	StrAppBuf strbDummy;
	return ChooseFolder(hwndParent, strbPath, strbDummy, ridTitle, pszFilter);
}

/*----------------------------------------------------------------------------------------------
	Called from outside the class to instantiate and run the folder chooser dialog.
	@param hwndParent [in] Handle of window to be used as dialog parent.
	@param strbPath [in, out] The starting path, and eventually the user's selected path.
	@param strbFile [out] The actual file that the user selected, if any.
	@param ridTitle Resource id of string for dialog title.
	@param pszFilter [in] File filter to determine which files may be shown to user.
	&return true if a folder (file) selected, false if selection canceled
----------------------------------------------------------------------------------------------*/
bool FolderSelectDlg::ChooseFolder(HWND hwndParent, StrAppBuf & strbPath, StrAppBuf & strbFile,
								   int ridTitle, const achar * pszFilter)
{
	bool fResult = false;
	// Get the current directory in case we need to restore it.  GetOpenFileName() can and will
	// change the current directory.  The OFN_NOCHANGEDIR flag (for ofn.Flags) is explicitly
	// listed as "ineffective" for "Windows NT 4.0/2000/XP"!
	achar rgchCurrentDir[MAX_PATH + 1];
	::GetCurrentDirectory(MAX_PATH + 1, rgchCurrentDir);
	try
	{
		// Set up open-file dialog:
		achar rgchFile[MAX_PATH + 1];
		::ZeroMemory(rgchFile, sizeof(rgchFile));
		OPENFILENAME ofn;
		::ZeroMemory(&ofn, sizeof(OPENFILENAME));
		// The constant below is required for compatibility with Windows 95/98 (and maybe NT4)
		ofn.lStructSize = OPENFILENAME_SIZE_VERSION_400;
		ofn.Flags = OFN_PATHMUSTEXIST | OFN_HIDEREADONLY | OFN_ENABLESIZING | OFN_ENABLEHOOK |
			OFN_EXPLORER;
		ofn.hwndOwner = hwndParent;
		// Set a file filter most likely just to show FieldWorks backup files:
		ofn.lpstrFilter = pszFilter;
		StrApp strTitle;
		if (ridTitle)
		{
			strTitle.Load(ridTitle);
			ofn.lpstrTitle = strTitle.Chars();
		}
		ofn.lpstrInitialDir = strbPath.Chars();
		ofn.lpstrFile = rgchFile;
		ofn.nMaxFile = MAX_PATH + 1;	// number of chars.
		// Register our customized hook function:
		ofn.lpfnHook = BrowseFolderHookProc;
		if (IDOK == ::GetOpenFileName(&ofn))
		{
			fResult = true;
			// Make sure any file name at the end of path is removed, just leaving a folder
			// path:
			strbPath.Assign(rgchFile);
			DWORD nFlags = GetFileAttributes(strbPath.Chars());
			if (nFlags == -1 || !(nFlags & FILE_ATTRIBUTE_DIRECTORY))
			{
				StrAppBuf strbSlash("\\");
				int ichLastSlash = strbPath.ReverseFindCh(strbSlash[0]);
				if (ichLastSlash >= 0)
				{
					strbFile.Assign(
						strbPath.Right(strbPath.Length() - ichLastSlash - 1).Chars());
					strbPath.Assign(strbPath.Left(ichLastSlash).Chars());
				}
			}
		}
	}
	catch (...)
	{
		fResult = false;
	}
	// Get the (probably new) current directory.  If it differs from what we had earlier,
	// set the current directory to the old value.
	achar rgchCurrentDir2[MAX_PATH + 1];
	::GetCurrentDirectory(MAX_PATH + 1, rgchCurrentDir2);
	if (_tcscmp(rgchCurrentDir, rgchCurrentDir2) != 0)
		::SetCurrentDirectory(rgchCurrentDir);
	return fResult;
}


/*----------------------------------------------------------------------------------------------
	A callback function, called when there is something to notify us about in the
	folder-browsing dialog. We use it to hide controls that deal specifically with files, and to
	artificially insert a dummy file name, so that dialog can be closed with only a directory
	chosen by user.
	@param hwnd Handle to child dialog of main File-Open dialog (our extra contols dialog).
	@param uiMsg Identifies the message being received.
	@param wParam message parameter
	@param lParam message parameter
----------------------------------------------------------------------------------------------*/
UINT CALLBACK FolderSelectDlg::BrowseFolderHookProc(HWND hwnd, UINT uiMsg, WPARAM wParam,
													LPARAM lParam)
{
	if (uiMsg == WM_NOTIFY)
	{
		// Retrieve notification structure:
		NMHDR * pnmh = (LPNMHDR)lParam;
		switch (pnmh->code)
		{
		case CDN_INITDONE:
			// Dialog is initialized, so hide controls we don't want. These are the file-name
			// text and editbox, and the file-types text and combobox.
			CommDlg_OpenSave_HideControl(pnmh->hwndFrom, 0x0442);
			CommDlg_OpenSave_HideControl(pnmh->hwndFrom, 0x0480);
			CommDlg_OpenSave_HideControl(pnmh->hwndFrom, 0x0441);
			CommDlg_OpenSave_HideControl(pnmh->hwndFrom, 0x0470);
			// Make sure there is at least some 'name' in the selected file box:
			CommDlg_OpenSave_SetControlText(pnmh->hwndFrom, 0x0480, krgchDummyName);
			return 0;
		case CDN_SELCHANGE:
			// Fall through:
		case CDN_FOLDERCHANGE:
			{ // Begin Block
				// See if we need to put the word "Open" or "OK" in the button. If the current
				// selection is a directory, then show "Open".
				// First, see how long the current selection is:
				int nLenPath = CommDlg_OpenSave_GetFilePath(pnmh->hwndFrom, NULL, 0);
				bool fSetOpen = false;
				if (nLenPath > 0)
				{
					// Create string space:
					achar * pszFilePath = NewObj achar [nLenPath];
					CommDlg_OpenSave_GetFilePath(pnmh->hwndFrom, pszFilePath, nLenPath);
					// See if current selection is a directory:
					DWORD nFlags = GetFileAttributes(pszFilePath);
					delete[] pszFilePath;
					if (nFlags != -1 && (nFlags & FILE_ATTRIBUTE_DIRECTORY))
						fSetOpen = true;
				}
				// Set text in button:
				StrApp strButton;
				if (fSetOpen)
					strButton.Load(kstidBrowseOpen);
				else
					strButton.Load(kstidBrowseOK);
				CommDlg_OpenSave_SetControlText(pnmh->hwndFrom, 0x0001, strButton.Chars());
			} // End Block
			// Make sure there is at least some 'name' in the selected file box:
			CommDlg_OpenSave_SetControlText(pnmh->hwndFrom, 0x0480, krgchDummyName);
			return 0;
		case CDN_SHAREVIOLATION:
			// Fall through:
		case CDN_HELP:
			// Fall through:
		case CDN_FILEOK:
			// Fall through:
		case CDN_TYPECHANGE:
			return 0;
		default:
			break;
		}
	}
	return 0;
}
#endif // WIN32