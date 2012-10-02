/*----------------------------------------------------------------------------------------------
Copyright 2002, SIL International. All rights reserved.

File: UtilFile.h
Responsibility: Alistair Imrie
Last reviewed: Never

	Header file for the file utilities.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef __UTILFILE_H
#define __UTILFILE_H

#include "common.h"

/*----------------------------------------------------------------------------------------------
	This class provides customizes the common file-open dialog, so that the user can only select
	a folder.

	@h3{Hungarian: fdsel}
----------------------------------------------------------------------------------------------*/
class FolderSelectDlg
{
public:
	static bool ChooseFolder(HWND hwndParent, StrAppBuf & strbPath, int ridTitle,
		const achar * pszFilter);
	static bool ChooseFolder(HWND hwndParent, StrAppBuf & strbPath, StrAppBuf & strbFile,
		int ridTitle, const achar * pszFilter);

protected:
	static UINT CALLBACK BrowseFolderHookProc(HWND hdlg, UINT uiMsg, WPARAM wParam,
		LPARAM lParam);
	static const achar * krgchDummyName;
};
typedef GenSmartPtr<FolderSelectDlg> FolderSelectDlgPtr;

#endif // __UTILFILE_H
