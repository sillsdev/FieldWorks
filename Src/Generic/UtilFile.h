/*----------------------------------------------------------------------------------------------
Copyright (c) 2002-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: UtilFile.h
Responsibility: Alistair Imrie
Last reviewed: Never

	Header file for the file utilities.
----------------------------------------------------------------------------------------------*/
#ifdef WIN32
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
#endif // WIN32