//*-
//
// MODULE NAME:   SHBrowseDlg.cpp
//
// DESCRIPTION:   CSHBrowseDlg class implementation
//
// AUTHOR     :   Stefano Passiglia, January 1998
//                passig@geocities.com
//                You can reuse and redistribute this code, provided this header is
//                kept as is.
//+*/


//
// Include Files
//
#include "stdafx.h"

#include "SHBrowseDlg.h"


//
// Global variables.
//


//
// Local constant definitions.
//
#ifdef _DEBUG
#  define new DEBUG_NEW
#  undef THIS_FILE
   static char THIS_FILE[] = __FILE__;
#endif

//
// Local type definitions.
//

// Struct used by the callback function
typedef struct _SB_INITDATA
{
   LPTSTR       lpszInitialDir;
   CSHBrowseDlg *pSHBrowseDlg;
} SB_INITDATA, *LPSB_INITDATA;


//
// Local defines
//



//
// Local function declarations.
//

// Dialog callback


//
// Local class declarations.
//
// None.



			   ///////////////////////////////////////////////
			   //          CONSTRUCTORS/DESTRUCTOR          //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CSHBrowseDlg::CSHBrowseDlg
//
// DESCRIPTION:   CSHBrowseDlg class constructor
//
// PARAMETER(S):
//                hwndParent:
//                   TYPE:          HWND
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Dialog parent window handle
//
//                szHint:
//                   TYPE:          char
//                   MODE:          In
//                   MECHANISM:     By reference
//                   DESCRIPTION:   Pointer to the dialog hint
//
// RETURN:        None
//
// NOTES:         None
//+*/
CSHBrowseDlg::CSHBrowseDlg( HWND   hWndOwner,
							LPCTSTR szHint ) :
   m_hWndOwner(hWndOwner)
{
   if ( szHint )
   {
	  lstrcpy( m_szHint, szHint );
   }

   // Initialize path strings
   m_szDirFullPath[0] = m_szDisplayName[0] = 0;

   // Retrieve IShellFolder interface
   SHGetDesktopFolder( &m_pSHFolder );

   // Retrieve IMalloc
   SHGetMalloc( &m_pMalloc );
} // CSHBrowseDlg::CSHBrowseDlg()


///*-
// FUNCTION NAME: CSHBrowseDlg::~CSHBrowseDlg
//
// DESCRIPTION:   CSHBrowseDlg class destructor
//
// PARAMETER(S):  None.
//
// RETURN:        None
//
// NOTES:         None
//+*/
CSHBrowseDlg::~CSHBrowseDlg()
{
   // Release ISHFolder/IMalloc interfaces
   m_pSHFolder->Release();
   m_pMalloc->Release();
} // CSHBrowseDlg::~CSHBrowseDlg()



			   ///////////////////////////////////////////////
			   //             PUBLIC FUNCTIONS              //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CSHBrowseDlg::DoModal
//
// DESCRIPTION:   Shows the dialog
//
// PARAMETER(S):
//                lpszInitialDir:
//                   TYPE:          char
//                   MODE:          In
//                   MECHANISM:     By reference
//                   DESCRIPTION:   Pointer to the string containing
//                                  the initial path
//                uRoot:
//                   TYPE:          UINT
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Root of the browsing tree
//                                  (default: desktop)
//
//                uFlags:
//                   TYPE:          UINT
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Specifies additional flags
//                                  (see SHBrowseForFolder help)
//
// RETURN:        None
//
// NOTES:         None
//+*/
BOOL CSHBrowseDlg::DoModal( LPTSTR lpszInitialDir,
							UINT  uRoot,
							UINT  uFlags
						  )
{
   BOOL bResult = TRUE;

   SB_INITDATA sbInit = { lpszInitialDir, this };

   // Get tree root
   LPITEMIDLIST lpidList;
   SHGetSpecialFolderLocation( m_hWndOwner, uRoot, &lpidList );

   // Initialize BROWSEINFO structure
   BROWSEINFO biInfo;
   biInfo.hwndOwner      = m_hWndOwner;
   biInfo.pidlRoot       = lpidList;
   biInfo.pszDisplayName = m_szDisplayName;
   biInfo.lpszTitle      = m_szHint;
   biInfo.ulFlags        = BIF_STATUSTEXT | uFlags;
   biInfo.lpfn           = BFFCALLBACK( SHBrowseCallBack );
   biInfo.lParam         = LPARAM( &sbInit );
   biInfo.iImage         = 0;

   // Show the dialog
   LPITEMIDLIST pidl = SHBrowseForFolder( &biInfo );

   bResult = (pidl != NULL);
   if ( bResult )
   {
	  // "OK" pressed
	  // Try to retrieve path name
	  if ( !SHGetPathFromIDList(pidl, m_szDirFullPath) && (*m_szDisplayName == 0) )
	  {
		 bResult = FALSE;
	  }

	  // Give memory back to the system using IMalloc interface
	  m_pMalloc->Free( reinterpret_cast< void * >( pidl ) );

   }

   return bResult;
} // CSHBrowseDlg::DoModal()



///*-
// FUNCTION NAME: CSHBrowseDlg::GetFullPath
//
// DESCRIPTION:   Retrieves selected folder full path
//
// PARAMETER(S):  None.
//
// RETURN:        CString object.
//
// NOTES:         None.
//+*/
LPTSTR CSHBrowseDlg::GetFullPath()
{
   return &m_szDirFullPath[0];
} // CSHBrowseDlg::GetFullPath()


///*-
// FUNCTION NAME: CSHBrowseDlg::GetFolderName
//
// DESCRIPTION:   Retrieves selected folder name
//
// PARAMETER(S):  None.
//
// RETURN:        LPTSTR.
//
// NOTES:         None.
//+*/
LPTSTR CSHBrowseDlg::GetFolderName()
{
   return &m_szDisplayName[0];
} // CSHBrowseDlg::GetFolderName()



			   ///////////////////////////////////////////////
			   //             BROWSE CALLBACK               //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CSHBrowseDlg::SHBrowseCallBack
//
// DESCRIPTION:   Browse dialog callback
//
// PARAMETER(S):
//                hWnd:
//                   TYPE:          HWND
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Handle of the dialog
//
//                uMsg:
//                   TYPE:          UIN
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Message identifier
//
//                lParam:
//                   TYPE:          LPARAM
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Message-dependent value
//
//
//                lpData:
//                   TYPE:          LPARAM
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Application-dependent value
//
//
// RETURN:        Zero, always
//
// NOTES:         Used only to set initial dir and initialize subclassing.
//+*/
int WINAPI CSHBrowseDlg::SHBrowseCallBack( HWND   hWnd,
										   UINT   uMsg,
										   LPARAM lParam,
										   LPARAM lpData )
{
   static CSHBrowseDlg *pSBDlg = reinterpret_cast< LPSB_INITDATA >(lpData)->pSHBrowseDlg;

   if ( uMsg == BFFM_INITIALIZED )
   {
	  // Set initial directory
	  if ( lpData && *(LPTSTR)lpData )
	  {
		 ::SendMessage( hWnd,
						BFFM_SETSELECTION,
						TRUE,
						LPARAM(LPSB_INITDATA(lpData)->lpszInitialDir)
					  );
	  }
	  pSBDlg->m_hWndTreeView = FindWindowEx( hWnd, NULL, WC_TREEVIEW, NULL );
   }
   else
   if ( uMsg == BFFM_SELCHANGED )
   {
	  pSBDlg->GetNames( LPCITEMIDLIST(lParam) );

	  // Update status text
	  if ( *pSBDlg->m_szDirFullPath )
	  {
		 ::SendMessage( hWnd,
						BFFM_SETSTATUSTEXT,
						0,
						LPARAM(pSBDlg->m_szDirFullPath) );
	  }
	  else
	  {
		 ::SendMessage( hWnd,
						BFFM_SETSTATUSTEXT,
						0,
						LPARAM(pSBDlg->m_szDisplayName) );
	  }

   }


   return 0;
} // SHBrowseCallBack()



			   ///////////////////////////////////////////////
			   //             PRIVATE FUNCTIONS             //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CSHBrowseDlg::GetNames
//
// DESCRIPTION:   Retrieves full path and display name of the selected treeview item
//
// PARAMETER(S):
//                pidl:
//                   TYPE:          ITEMIDLIST
//                   MODE:          In
//                   MECHANISM:     By reference
//                   DESCRIPTION:   Pointer to the item identifier list
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CSHBrowseDlg::GetNames( LPCITEMIDLIST pidl )
{
   HTREEITEM hSelected;
   TV_ITEM   tvSelected;

   // Get full path
   SHGetPathFromIDList( pidl, m_szDirFullPath );

   // Now retrieve display name.
   hSelected = TreeView_GetSelection( m_hWndTreeView );
   tvSelected.mask       = TVIF_HANDLE | TVIF_TEXT;
   tvSelected.hItem      = hSelected;
   tvSelected.pszText    = m_szDisplayName;
   tvSelected.cchTextMax = MAX_PATH;
   TreeView_GetItem( m_hWndTreeView, &tvSelected );
} // CSHBrowseDlg::GetNames()
