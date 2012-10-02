//*-
//
// MODULE NAME:   SHBrowseDlg.h
//
// DESCRIPTION:   CSHBrowseDlg class declaration
//
// AUTHOR     :   Stefano Passiglia, March 1998
//                passig@geocities.com
//                You can reuse and redistribute this code, provided this header is
//                kept as is.
//+*/

#ifndef __SHBROWSEDLG_H
#define __SHBROWSEDLG_H

//
// Include Files
//
#include "stdafx.h"

#include <winnetwk.h>
#include <shlobj.h>


//
// Constant definitions.
//


//
// Type definitions.
//
// None.


//
// Function declarations.
//
// None.


//
// Class declarations.
//

class CSHBrowseDlg
{
private:

   TCHAR m_szHint[MAX_PATH];
   TCHAR m_szDisplayName[MAX_PATH];
   TCHAR m_szDirFullPath[MAX_PATH];

   HWND m_hWndOwner;
   HWND m_hWndTreeView;

   // Shell's OLE interfaces
   IShellFolder *m_pSHFolder;
   IMalloc      *m_pMalloc;

private:

   void GetNames( LPCITEMIDLIST pidl );

   static int WINAPI SHBrowseCallBack( HWND hWnd, UINT uMsg, LPARAM lParam, LPARAM lpData );

protected:


public:

   // Constructor
   CSHBrowseDlg( HWND   hWndOwner = NULL,
				 LPCTSTR  szHint    = _T("\nPlease Select Folder:") );
   // Destructor
   ~CSHBrowseDlg();

public:

   // Shows the dialog
   BOOL DoModal( LPTSTR lpszInitialDir = NULL,
				 UINT  uRoot          = CSIDL_DESKTOP,
				 UINT  uFlags         = BIF_STATUSTEXT );

   // Path selection helpers
   LPTSTR GetFullPath();
   LPTSTR GetFolderName();

};


#endif // __SHBROWSEDLG_H