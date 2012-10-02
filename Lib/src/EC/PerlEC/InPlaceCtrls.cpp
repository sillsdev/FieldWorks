//*-
//
// MODULE NAME:   InPlaceCtrls.cpp
//
// DESCRIPTION:   Contains class definitions for in-place controls
//
// AUTHOR     :   Copyright (C) Stefano Passiglia, December 1999
//                passig@geocities.com
//                You can reuse and redistribute this code, provided this header is
//                kept as is.
//+*/

//
// Include Files
//
#include "stdafx.h"
#include <afxpriv.h> // WM_IDLEUPDATECMDUI definition

#include "InPlaceCtrls.h"

//
// Global variables.
//


// Registered messages

static unsigned int const g_IPCMsg = RegisterWindowMessage( IPCMSGSTRING );


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
// None.


//
// Local defines
//


//
// Local function declarations.
//
// None.


//
// Local class declarations.
//
// None.



//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//                         CInPlaceEdit class                               //
//                                                                          //
//////////////////////////////////////////////////////////////////////////////



BEGIN_MESSAGE_MAP( CInPlaceEdit, CEdit )
   //{{AFX_MSG_MAP(CInPlaceEdit)
   ON_WM_CREATE()
   ON_WM_KILLFOCUS()
   //}}AFX_MSG_MAP
END_MESSAGE_MAP()

IMPLEMENT_DYNCREATE( CInPlaceEdit, CEdit )



			   ///////////////////////////////////////////////
			   //           Constructor/Destructor          //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CInPlaceEdit::CInPlaceEdit()
//
// DESCRIPTION:   CInPlaceEdit class constructor
//
// PARAMETER(S):  None.
//
// RETURN:        None
//
// NOTES:         None
//+*/
CInPlaceEdit::CInPlaceEdit()
{
   m_hParent = NULL;
} // CInPlaceEdit::CInPlaceEdit


///*-
// FUNCTION NAME: CInPlaceEdit::~CInPlaceEdit()
//
// DESCRIPTION:   CInPlaceEdit class destructor
//
// PARAMETER(S):  None.
//
// RETURN:        None
//
// NOTES:         None
//+*/
CInPlaceEdit::~CInPlaceEdit()
{
} // CInPlaceEdit::~CInPlaceEdit


///*-
// FUNCTION NAME: CInPlaceEdit::PreCreateWindow
//
// DESCRIPTION:   Called before the window handle is created
//
// PARAMETER(S):
//                cs:
//                   TYPE:          CREATESTRUCT structure
//                   MODE:          In
//                   MECHANISM:     By reference
//                   DESCRIPTION:   Allows to change window features
//
// RETURN:        Inherited value.
//
// NOTES:         None
//+*/
BOOL CInPlaceEdit::PreCreateWindow( CREATESTRUCT & cs )
{
   // Add also the ES_MULTILINE style so that
   // we can intercept the <Return> keys
   cs.style |= ( WS_CHILD | ES_MULTILINE | ES_WANTRETURN | ES_AUTOHSCROLL | ES_NOHIDESEL );
   return CEdit::PreCreateWindow( cs );
} // CInPlaceEdit::PreCreateWindow


///*-
// FUNCTION NAME: CInPlaceEdit::OnCreate
//
// DESCRIPTION:   Called whren the window handle is created
//
// PARAMETER(S):
//                lpCreateStruct:
//                   TYPE:          CREATESTRUCT structure
//                   MODE:          In
//                   MECHANISM:     By reference
//                   DESCRIPTION:   Allows to change window features
//
// RETURN:        -1 to avoid windiw creation. 0 to continue normally.
//
// NOTES:         None
//+*/
int CInPlaceEdit::OnCreate( LPCREATESTRUCT lpCreateStruct )
{
   if ( CEdit::OnCreate(lpCreateStruct) == -1 )
   {
	  return -1;
   }

   m_hParent = lpCreateStruct->hwndParent;

   return 0;
} // CInPlaceEdit::OnCreate



			   ///////////////////////////////////////////////
			   //              Message Handling             //
			   ///////////////////////////////////////////////



// FUNCTION NAME: CInPlaceEdit::PreTranslateMessage
//
// DESCRIPTION:   Called before the keyboard message is dispatched through the
//                MFC hierarchy
//
// PARAMETER(S):
//                pMsg:
//                   TYPE:          MSG structure
//                   MODE:          In
//                   MECHANISM:     By variable
//                   DESCRIPTION:   Message to be processed
//
// RETURN:        TRUE to block message routing. FALSE otherwise.
//
// NOTES:         None.
//+*/
BOOL CInPlaceEdit::PreTranslateMessage( MSG *pMsg )
{
   IPCENDEDITINFO   ipeEditInfo;

   switch ( pMsg->message )
   {
	  case WM_KEYDOWN:

		 if ( (int)pMsg->wParam == VK_ESCAPE ||
			  (int)pMsg->wParam == VK_RETURN )
		 {
			ZeroMemory( &ipeEditInfo, sizeof( ipeEditInfo ) );
			ipeEditInfo.hwndSrc = m_hWnd;
			ipeEditInfo.uKind = IPEK_KEY;
			ipeEditInfo.nVirtKey = (int)pMsg->wParam;
			ipeEditInfo.pt = pMsg->pt;
			if ( ::SendMessage( m_hParent, g_IPCMsg, 0, (LPARAM)&ipeEditInfo ) )
			{
			   Hide();
			}
			return TRUE;
		 }
		 break;
   }

   return CEdit::PreTranslateMessage(pMsg);
} // CInPlaceEdit::PreTranslateMessage


// FUNCTION NAME: CInPlaceEdit::OnKillFocus
//
// DESCRIPTION:   Called immedately before the window loses the keyboard focus
//
// PARAMETER(S):
//                pNewWnd:
//                   TYPE:          CWnd class
//                   MODE:          In
//                   MECHANISM:     By variable
//                   DESCRIPTION:   The new window that will gain the input focus.
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CInPlaceEdit::OnKillFocus( CWnd *pNewWnd )
{
   // If the window is still visible, signal the parent
   if ( IsWindowVisible() )
   {
	  IPCENDEDITINFO   ipeEditInfo;
	  const MSG *pMsg = GetCurrentMessage();

	  TRACE( "IPE: WM_KILLFOCUS\n" );
	  ZeroMemory( &ipeEditInfo, sizeof( ipeEditInfo ) );
	  ipeEditInfo.uKind = IPEK_FOCUS;
	  ipeEditInfo.hwndSrc = m_hWnd;
	  ipeEditInfo.hNewWnd = pNewWnd ? pNewWnd->m_hWnd : NULL;
	  ipeEditInfo.pt      = pMsg->pt;
	  if ( ::SendMessage( m_hParent, g_IPCMsg, 0, (LPARAM)&ipeEditInfo ) )
	  {
		 Hide();
	  }
   }
   CEdit::OnKillFocus( pNewWnd );
} // CInPlaceEdit::OnKillFocus


// FUNCTION NAME: CInPlaceEdit::Hide
//
// DESCRIPTION:   Called to hide the control
//
// PARAMETER(S):  None.
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CInPlaceEdit::Hide()
{
   if ( IsWindowVisible() )
   {
	  ::SetWindowPos( m_hWnd, 0, 0, 0, 0, 0,
					  SWP_HIDEWINDOW | SWP_NOZORDER | SWP_NOREDRAW | SWP_NOSIZE | SWP_NOMOVE );
	  ::SetFocus( m_hParent );
   }
} // CInPlaceEdit::Hide



// FUNCTION NAME: CInPlaceEdit::Show
//
// DESCRIPTION:   Called to show the control
//
// PARAMETER(S):
//                rcEdit:
//                   TYPE:          CRect class
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Edit rectangle, in parent's client coordinates
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CInPlaceEdit::Show( CRect rcEdit )
{
   if ( !IsWindowVisible() && !rcEdit.IsRectEmpty() )
   {
	  // Show the edit box
	  ::SetWindowPos( m_hWnd,
					  HWND_TOP,
					  rcEdit.left, rcEdit.top, rcEdit.Width(), rcEdit.Height(),
					  SWP_SHOWWINDOW | SWP_NOREDRAW );

	  // Set a bit of margin on the left
	  SendMessage( EM_SETMARGINS, EC_LEFTMARGIN, 2 );

	  // Select all the text
	  SetSel( 0, -1 );

	  // Invalidate
	  Invalidate();

	  // Give keyboard focus
	  ::SetFocus( m_hWnd );

   }
} // CInPlaceEdit::Show()






//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//                       CInPlaceButton class                               //
//                                                                          //
//////////////////////////////////////////////////////////////////////////////



BEGIN_MESSAGE_MAP( CInPlaceButton, CButton )
   //{{AFX_MSG_MAP(CInPlaceButton)
   ON_WM_CREATE()
   ON_WM_KILLFOCUS()
   ON_CONTROL_REFLECT(BN_CLICKED, OnClick)
   //}}AFX_MSG_MAP
END_MESSAGE_MAP()

IMPLEMENT_DYNCREATE( CInPlaceButton, CButton )



			   ///////////////////////////////////////////////
			   //           Constructor/Destructor          //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CInPlaceButton::CInPlaceButton()
//
// DESCRIPTION:   CInPlaceButton class constructor
//
// PARAMETER(S):  None.
//
// RETURN:        None
//
// NOTES:         None
//+*/
CInPlaceButton::CInPlaceButton()
{
   m_hParent = NULL;
   m_bInAction = FALSE;
} // CInPlaceButton::CInPlaceButton


///*-
// FUNCTION NAME: CInPlaceButton::~CInPlaceButton()
//
// DESCRIPTION:   CInPlaceButton class destructor
//
// PARAMETER(S):  None.
//
// RETURN:        None
//
// NOTES:         None
//+*/
CInPlaceButton::~CInPlaceButton()
{
} // CInPlaceButton::~CInPlaceButton


///*-
// FUNCTION NAME: CInPlaceButton::PreCreateWindow
//
// DESCRIPTION:   Called before the window handle is created
//
// PARAMETER(S):
//                cs:
//                   TYPE:          CREATESTRUCT structure
//                   MODE:          In
//                   MECHANISM:     By reference
//                   DESCRIPTION:   Allows to change window features
//
// RETURN:        Inherited value.
//
// NOTES:         None
//+*/
BOOL CInPlaceButton::PreCreateWindow( CREATESTRUCT & cs )
{
   cs.style |= ( WS_CHILD | BS_PUSHBUTTON );
   return CButton::PreCreateWindow( cs );
} // CInPlaceButton::PreCreateWindow


///*-
// FUNCTION NAME: CInPlaceButton::OnCreate
//
// DESCRIPTION:   Called whren the window handle is created
//
// PARAMETER(S):
//                lpCreateStruct:
//                   TYPE:          CREATESTRUCT structure
//                   MODE:          In
//                   MECHANISM:     By reference
//                   DESCRIPTION:   Allows to change window features
//
// RETURN:        -1 to avoid windiw creation. 0 to continue normally.
//
// NOTES:         None
//+*/
int CInPlaceButton::OnCreate( LPCREATESTRUCT lpCreateStruct )
{
   if ( CButton::OnCreate(lpCreateStruct) == -1 )
   {
	  return -1;
   }

   m_hParent = lpCreateStruct->hwndParent;

   return 0;
} // CInPlaceButton::OnCreate



			   ///////////////////////////////////////////////
			   //              Message Handling             //
			   ///////////////////////////////////////////////



// FUNCTION NAME: CInPlaceEdit::PreTranslateMessage
//
// DESCRIPTION:   Called before the keyboard message is dispatched through the
//                MFC hierarchy
//
// PARAMETER(S):
//                pMsg:
//                   TYPE:          MSG structure
//                   MODE:          In
//                   MECHANISM:     By variable
//                   DESCRIPTION:   Message to be processed
//
// RETURN:        TRUE to block message routing. FALSE otherwise.
//
// NOTES:         None.
//+*/
BOOL CInPlaceButton::PreTranslateMessage( MSG *pMsg )
{
   IPCENDEDITINFO   ipcInfo;

   switch ( pMsg->message )
   {
	  case WM_KEYDOWN:

		 if ( (int)pMsg->wParam == VK_ESCAPE )
		 {
			ZeroMemory( &ipcInfo, sizeof( ipcInfo ) );
			ipcInfo.hwndSrc = m_hWnd;
			ipcInfo.uKind = IPEK_KEY;
			ipcInfo.nVirtKey = (int)pMsg->wParam;
			ipcInfo.pt = pMsg->pt;
			::SendMessage( m_hParent, g_IPCMsg, 0, (LPARAM)&ipcInfo );
			return TRUE;
		 }
		 break;
   }

   return CButton::PreTranslateMessage(pMsg);
} // CInPlaceButton::PreTranslateMessage


// FUNCTION NAME: CInPlaceButton::OnKillFocus
//
// DESCRIPTION:   Called immedately before the window loses the keyboard focus
//
// PARAMETER(S):
//                pNewWnd:
//                   TYPE:          CWnd class
//                   MODE:          In
//                   MECHANISM:     By variable
//                   DESCRIPTION:   The new window that will gain the input focus.
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CInPlaceButton::OnKillFocus( CWnd *pNewWnd )
{
   // If the window is still visible, signal the parent
   if ( IsWindowVisible() && !m_bInAction )
   {
	  IPCENDEDITINFO   ipcEventInfo;
	  const MSG *pMsg = GetCurrentMessage();

	  TRACE( "IPB: WM_KILLFOCUS\n" );
	  ZeroMemory( &ipcEventInfo, sizeof( ipcEventInfo ) );
	  ipcEventInfo.uKind      = IPEK_FOCUS;
	  ipcEventInfo.hNewWnd    = pNewWnd->m_hWnd;
	  ipcEventInfo.pt         = pMsg->pt;
	  if ( ::SendMessage( m_hParent, g_IPCMsg, 0, (LPARAM)&ipcEventInfo ) )
	  {
		 Hide();
	  }
   }
   CButton::OnKillFocus( pNewWnd );
} // CInPlaceButton::OnKillFocus


///*-
// FUNCTION NAME: CInPlaceButton::OnClick
//
// DESCRIPTION:   Called when a click occurs.
//
// PARAMETER(S):  None.
//
// RETURN:        None
//
// NOTES:         None
//+*/
void CInPlaceButton::OnClick()
{
   IPCENDEDITINFO   ipcEventInfo;

   TRACE( "IPB: BN_CLICK\n" );
   ZeroMemory( &ipcEventInfo, sizeof( ipcEventInfo ) );
   ipcEventInfo.uKind   = IPEK_ACTION;
   ipcEventInfo.uID     = GetWindowLong( m_hWnd, GWL_ID );
   ipcEventInfo.pt.x    = LOWORD( GetMessagePos() );
   ipcEventInfo.pt.y    = HIWORD( GetMessagePos() );

   m_bInAction = TRUE;
   ::SendMessage( m_hParent, g_IPCMsg, 0, (LPARAM)&ipcEventInfo );
   m_bInAction = FALSE;
} // CInPlaceButton::OnClick


// FUNCTION NAME: CInPlaceButton::Hide
//
// DESCRIPTION:   Called to hide the control
//
// PARAMETER(S):  None.
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CInPlaceButton::Hide()
{
   if ( IsWindowVisible() )
   {
	  TRACE( "Hide() called\n" );
	  ::SetWindowPos( m_hWnd, 0, 0, 0, 0, 0,
					  SWP_HIDEWINDOW | SWP_NOZORDER | SWP_NOREDRAW | SWP_NOSIZE | SWP_NOMOVE );
	  ::SetFocus( m_hParent );
   }
} // CInPlaceButton::Hide



// FUNCTION NAME: CInPlaceButton::Show
//
// DESCRIPTION:   Called to show the control
//
// PARAMETER(S):
//                rcEdit:
//                   TYPE:          CRect class
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Button rectangle, in parent's client coordinates
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CInPlaceButton::Show( CRect rcButton )
{
   if ( !IsWindowVisible() && !rcButton.IsRectEmpty() )
   {
	  ::SetWindowPos( m_hWnd,
					  HWND_TOP,
					  rcButton.left, rcButton.top, rcButton.Width(), rcButton.Height(),
					  SWP_SHOWWINDOW | SWP_NOREDRAW );
	  Invalidate();
   }
} // CInPlaceButton::Show()
