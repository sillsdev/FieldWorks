//*-
//
// MODULE NAME:   ListBoxEx.cpp
//
// DESCRIPTION:   CListBoxEx class declaration
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

#include <stdio.h>

#include "InPlaceCtrls.h"
#include "ListboxEx.h"

#include "BmpData.h" // for bitmap definition

//
// Global variables.
//
static unsigned int const g_DragListMsg = RegisterWindowMessage( DRAGLISTMSGSTRING );
static unsigned int const g_IPCEndEditMsg = RegisterWindowMessage( IPCMSGSTRING );


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
#define ALT_KEY_PRESSED( uFlag )  ( ((uFlag) & (1 << 3)) != 0 )

#define LBEX_ID_EDITCONTROL     1
#define LBEX_ID_BUTTONCONTROL   2

#define LBEX_LASTITEM_MAGIC     0x45424558   // 'LBEX'


//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//                         CListBoxEx class                                 //
//                                                                          //
//////////////////////////////////////////////////////////////////////////////



			   ///////////////////////////////////////////////
			   //               Message map                 //
			   ///////////////////////////////////////////////



BEGIN_MESSAGE_MAP( CListBoxEx, CDragListBox )
   //{{AFX_MSG_MAP(CListBoxEx)
   ON_WM_CREATE()
   ON_WM_LBUTTONDOWN()
   ON_WM_LBUTTONDBLCLK()
   ON_WM_LBUTTONUP()
   ON_WM_SYSKEYDOWN()
   ON_WM_KEYDOWN()
   //}}AFX_MSG_MAP
   ON_REGISTERED_MESSAGE( g_IPCEndEditMsg, OnEndEditMessage )
END_MESSAGE_MAP()

IMPLEMENT_DYNCREATE( CListBoxEx, CDragListBox )



			   ///////////////////////////////////////////////
			   //           Constructor/Destructor          //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CListBoxEx::CListBoxEx()
//
// DESCRIPTION:   CListBoxEx class constructor
//
// PARAMETER(S):  None.
//
// RETURN:        None
//
// NOTES:         None
//+*/
CListBoxEx::CListBoxEx()
{
   m_pBuddy = NULL;

   m_dwEditStyle = LBEX_EDITBUTTON;

   m_pEdit = NULL;

   m_iSelected = -1;
   m_iEdited = -1;

   m_bAllowEditing = TRUE;
   m_bAllowDrag = TRUE;
} // CListBoxEx::CListBoxEx



///*-
// FUNCTION NAME: CListBoxEx::~CListBoxEx()
//
// DESCRIPTION:   CListBoxEx class destructor
//
// PARAMETER(S):  None.
//
// RETURN:        None
//
// NOTES:         None
//+*/
CListBoxEx::~CListBoxEx()
{
   delete m_pEdit;
   delete m_pBrowseButton;
} // CListBoxEx::~CListBoxEx



			   ///////////////////////////////////////////////
			   //             Initialization                //
			   ///////////////////////////////////////////////




///*-
// FUNCTION NAME: CListBoxEx::PreCreateWindow
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
BOOL CListBoxEx::PreCreateWindow( CREATESTRUCT & cs )
{
   // Make sure it has the correct styles
   cs.style |= LBEX_STYLE;
   cs.dwExStyle |= LBEX_EXSTYLE;
   return CWnd::PreCreateWindow(cs);
} // CListBoxEx::PreCreateWindow


///*-
// FUNCTION NAME: CListBoxEx::PreSubclassWindow
//
// DESCRIPTION:   Called before the window handle is attached to a dialog item.
//
// PARAMETER(S):  None.
//
// RETURN:        Inherited value.
//
// NOTES:         None
//+*/
void CListBoxEx::PreSubclassWindow()
{
   // Make sure it has the correct styles
   ModifyStyle( 0, LBEX_STYLE, SWP_SHOWWINDOW );
   ModifyStyleEx( 0, LBEX_EXSTYLE, SWP_SHOWWINDOW );

   CDragListBox::PreSubclassWindow();

   CreateEdit();
} // CListBoxEx::PreSubclassWindow


///*-
// FUNCTION NAME: CListBoxEx::OnCreate
//
// DESCRIPTION:   Called when the window handle is created
//
// PARAMETER(S):
//                lpCreateStruct:
//                   TYPE:          CREATESTRUCT structure
//                   MODE:          In
//                   MECHANISM:     By reference
//                   DESCRIPTION:   Allows to change window features
//
// RETURN:        Inherited value.
//
// NOTES:         None
//+*/
int CListBoxEx::OnCreate( LPCREATESTRUCT lpCreateStruct )
{
   if ( CDragListBox::OnCreate(lpCreateStruct) == -1 )
   {
	  return -1;
   }

   CreateEdit();

   return 0;
} // CListBoxEx::OnCreate


///*-
// FUNCTION NAME: CListBoxEx::CreateEdit
//
// DESCRIPTION:   Creates the edit box
//
// PARAMETER(S):  None.
//
// RETURN:        Inherited value.
//
// NOTES:         None
//+*/
void CListBoxEx::CreateEdit(BOOL bWithBrowseButton)
{
   // Create an in-place edit box
   if ( m_pEdit == NULL )
   {
	  m_pEdit = new CInPlaceEdit;
	  m_pEdit->Create( WS_CHILD | WS_BORDER | m_dwEditStyle,
					   CRect( 0, 0, 0, 0 ),
					   this,
					   LBEX_ID_EDITCONTROL );
	  m_pEdit->SetFont( GetFont() );

	  if( bWithBrowseButton )
	  {
		// Create the browse button
		m_pBrowseButton = new CInPlaceButton;
		m_pBrowseButton->Create( _T("..."),
								WS_CHILD | BS_PUSHBUTTON,
								CRect( 0, 0, 0, 0 ),
								this,
								LBEX_ID_BUTTONCONTROL );
	  }
   }
} // CListBoxEx::CreateEdit()



			   ///////////////////////////////////////////////
			   //             Draw functions                //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CListBoxEx::MeasureItem
//
// DESCRIPTION:   Called by the framework when a list box
//                with an owner-draw style is created.
//
// PARAMETER(S):
//                lpMeasureItemStruct:
//                   TYPE:          MEASUREITEMSTRUCT structure.
//                   MODE:          In
//                   MECHANISM:     By reference
//                   DESCRIPTION:   Used to inform Windows of the list-box dimensions.
//
// RETURN:        None.
//
// NOTES:         None
//+*/
void CListBoxEx::MeasureItem( LPMEASUREITEMSTRUCT lpMeasureItemStruct )
{
   // Get current font metrics
   TEXTMETRIC metrics;
   HDC dc = ::GetDC( m_hWnd );
   GetTextMetrics( dc, &metrics );
   ::ReleaseDC( m_hWnd, dc );

   // Set the height
   lpMeasureItemStruct->itemHeight = metrics.tmHeight + metrics.tmExternalLeading;
} // CListBoxEx::MeasureItem


///*-
// FUNCTION NAME: CListBoxEx::DrawItem
//
// DESCRIPTION:   Called by the framework when a
//                visual aspect of an owner-draw list box changes.
//
// PARAMETER(S):
//                lpDrawItemStruct:
//                   TYPE:          DRAWITEMSTRUCT structure.
//                   MODE:          In
//                   MECHANISM:     By reference
//                   DESCRIPTION:   Contains information about the type of drawing required.
//
// RETURN:        None.
//
// NOTES:         None
//+*/
void CListBoxEx::DrawItem( LPDRAWITEMSTRUCT lpDrawItemStruct )
{
   // If there are no list box items, skip this message.
   if ( lpDrawItemStruct->itemID == -1 )
   {
	  return;
   }

   CString  strItemText;
   CRect    rcText( lpDrawItemStruct->rcItem );

   COLORREF clrItemText,
			clrOldTextColor;

   // Put a bit of room to the left of the text
   rcText.left += 8;

   // Act upon the item state
   switch ( lpDrawItemStruct->itemAction )
   {
	  case ODA_SELECT:
	  case ODA_DRAWENTIRE:

		 // Is the item selected?
		 if ( lpDrawItemStruct->itemState & ODS_SELECTED )
		 {
			clrItemText = GetSysColor( COLOR_HIGHLIGHTTEXT );
			// Clear the rectangle
			FillRect(  lpDrawItemStruct->hDC,
					  &lpDrawItemStruct->rcItem,
					   (HBRUSH)(COLOR_ACTIVECAPTION+1) );
		 }
		 else
		 {
			clrItemText = GetSysColor( COLOR_WINDOWTEXT );
			// Clear the rectangle
			FillRect(  lpDrawItemStruct->hDC,
					  &lpDrawItemStruct->rcItem,
					   (HBRUSH)(COLOR_WINDOW+1) );
		 }

		 clrOldTextColor = SetTextColor( lpDrawItemStruct->hDC,
										 clrItemText );
		 SetBkMode( lpDrawItemStruct->hDC,
					TRANSPARENT );

		 // Display the text associated with the item.
		 if ( lpDrawItemStruct->itemData != LBEX_LASTITEM_MAGIC )
		 {
			GetText( lpDrawItemStruct->itemID,
					 strItemText );
			DrawText(  lpDrawItemStruct->hDC,
					   LPCTSTR( strItemText ),
					   strItemText.GetLength(),
					  &rcText,
					   DT_SINGLELINE | DT_VCENTER );
		 }
		 else
		 {
			DrawText(  lpDrawItemStruct->hDC,
					   _T("--- Last Item"),
					   strItemText.GetLength(),
					  &rcText,
					   DT_SINGLELINE | DT_VCENTER );
		 }
		 // Is the item selected?
		 if ( lpDrawItemStruct->itemState & ODS_SELECTED )
		 {
			SetTextColor( lpDrawItemStruct->hDC,
						  clrOldTextColor );
			DrawFocusRect(  lpDrawItemStruct->hDC,
						   &lpDrawItemStruct->rcItem );
		 }

		 break;
   }
} // CListBoxEx::DrawItem


///*-
// FUNCTION NAME: CListBoxEx::DrawSeparator
//
// DESCRIPTION:   Draws the insertion guide before the
//                item with the indicated index.
//
// PARAMETER(S):
//                nIndex:
//                   TYPE:          int.
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Zero-based index of the insertion point.
//
// RETURN:        None.
//
// NOTES:         Copied from the MFC implementation, with some changes.
//+*/
void CListBoxEx::DrawSeparator( int nIndex )
{
   if ( nIndex == -1 )
   {
	  return;
   }

   CBrush* pBrush = CDC::GetHalftoneBrush();

   CRect rect;
   GetClientRect(&rect);

   CRgn rgn;
   rgn.CreateRectRgnIndirect( &rect );

   CDC* pDC = GetDC();

   // Prevent drawing outside of listbox
   // This can happen at the top of the listbox since the listbox's DC is the
   // parent's DC
   pDC->SelectClipRgn( &rgn );

   GetItemRect( nIndex, &rect );
   rect.bottom = rect.top+2;
   rect.top -= 2;
   rect.left += 5;
   rect.right -= 5;
   CBrush* pBrushOld = pDC->SelectObject(pBrush);

   // Draw main line
   pDC->PatBlt( rect.left, rect.top, rect.Width(), rect.Height(), PATINVERT );

   // Draw vertical lines
   pDC->PatBlt( rect.left-3, rect.top-4, 3, rect.Height()+8, PATINVERT );
   pDC->PatBlt( rect.right, rect.top-4, 3, rect.Height()+8, PATINVERT );

   pDC->SelectObject( pBrushOld );
   ReleaseDC( pDC );
} // CListBoxEx::DrawSeparator


///*-
// FUNCTION NAME: CListBoxEx::DrawInsert
//
// DESCRIPTION:   Called to draw the insertion guide before the
//                item with the indicated index.
//
// PARAMETER(S):
//                nIndex:
//                   TYPE:          int.
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Zero-based index of the insertion point.
//
// RETURN:        None.
//
// NOTES:         Copied from the MFC implementation.
//+*/
void CListBoxEx::DrawInsert( int nIndex )
{
   if ( m_nLast != nIndex )
   {
	  DrawSeparator( m_nLast );
	  DrawSeparator( nIndex );
   }
   // Set last selected
   m_nLast = nIndex;
} // CDragListBox::DrawInsert



			   ///////////////////////////////////////////////
			   //                 Messages                  //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CListBoxEx::OnChildNotify
//
// DESCRIPTION:   Called by this window’s parent window when it receives a
//                notification message that applies to this window.
//
// PARAMETER(S):
//                nMessage:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Message identifier
//
//                wParam:
//                   TYPE:          WPARAM
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Message wParam
//
//                lParam:
//                   TYPE:          LPARAM
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Message lParam
//
//                pLResult:
//                   TYPE:          LRESULT
//                   MODE:          In
//                   MECHANISM:     By variable
//                   DESCRIPTION:   A pointer to a value to be returned from
//                                  the parent’s window procedure.
//
// RETURN:        None.
//
// NOTES:         Copied from the MFC implementation, with some changes regarding the
//                editing improvement.
//+*/
BOOL CListBoxEx::OnChildNotify( UINT     nMessage,
								WPARAM   wParam,
								LPARAM   lParam,
								LRESULT *pLResult )
{
   if ( nMessage == g_DragListMsg )
   {
	  ASSERT( pLResult != NULL );
	  if ( m_iEdited == -1 && m_bAllowDrag )
	  {
		 LPDRAGLISTINFO pInfo = (LPDRAGLISTINFO)lParam;
		 ASSERT( pInfo != NULL );
		 switch ( pInfo->uNotification )
		 {
			case DL_BEGINDRAG:
			   TRACE( "Begin Dragging\n" );
			   // Removed from the MFC implementation
			   //*pLResult = BeginDrag(pInfo->ptCursor);
			   *pLResult = TRUE;
			   break;

			case DL_CANCELDRAG:
			   TRACE( "Cancel Drag\n" );
			   CancelDrag( pInfo->ptCursor );
			   break;

			case DL_DRAGGING:
			   TRACE( "Dragging\n" );
			   *pLResult = Dragging( pInfo->ptCursor );
			   break;

			case DL_DROPPED:
			   TRACE( "Dropped\n" );
			   Dropped( GetCurSel(), pInfo->ptCursor );
			   break;
		 }
	  }
	  else
	  {
		 *pLResult = FALSE;
	  }

	  return TRUE;  // Message handled
   }

   return CListBox::OnChildNotify( nMessage, wParam, lParam, pLResult );

} // CListBoxEx::OnChildNotify


///*-
// FUNCTION NAME: CListBoxEx::OnKeyDown
//
// DESCRIPTION:   Called when it receives a WM_KEYDOWN message.
//
// PARAMETER(S):
//                nChar:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Character identifier
//
//                nRepCnt:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Repetition count
//
//                nFlags:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Flags
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxEx::OnKeyDown( UINT nChar,
							UINT nRepCnt,
							UINT nFlags )
{
   CDragListBox::OnKeyDown( nChar, nRepCnt, nFlags );

   if ( (nChar == VK_F2)    &&
		(m_iSelected != -1) &&
		m_bAllowEditing     &&
		(OnBeginEditing( m_iSelected ) != FALSE) )
   {
	  // Begin Editing
	  BeginEditing( m_iSelected );
   }
   else
   if ( (nChar == VK_DELETE)    &&
		(m_iSelected != -1) )
   {
	  DeleteString( m_iSelected );
   }

} // CListBoxEx::OnKeyDown


///*-
// FUNCTION NAME: CListBoxEx::OnSysKeyDown
//
// DESCRIPTION:   Called when it receives a WM_SYSKEYDOWN message.
//
// PARAMETER(S):
//                nChar:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Character identifier
//
//                nRepCnt:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Repetition count
//
//                nFlags:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Flags
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxEx::OnSysKeyDown( UINT nChar,
							   UINT nRepCnt,
							   UINT nFlags )
{
   if ( ALT_KEY_PRESSED( nFlags ) &&
		m_bAllowDrag )
   {
	  switch ( nChar )
	  {
		 case VK_UP:
		 case VK_LEFT:

			MoveItemUp( GetCurSel() );
			break;

		 case VK_DOWN:
		 case VK_RIGHT:

			MoveItemDown( GetCurSel() );
			break;
	  }
   }

   CDragListBox::OnSysKeyDown( nChar, nRepCnt, nFlags );
} // CListBoxEx::OnSysKeyDown


///*-
// FUNCTION NAME: CListBoxEx::OnLButtonDown
//
// DESCRIPTION:   Called when it receives a WM_LBUTTONDOWN message.
//
// PARAMETER(S):
//                nFlags:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Flags
//
//                point:
//                   TYPE:          CPoint class
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Point of action
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxEx::OnLButtonDown( UINT   nFlags,
								CPoint point )
{
   int     iItem;

   ClientToScreen( &point );
   iItem = ItemFromPt( point, FALSE );
   TRACE1( "LButtonDown: %d\n", iItem );

   if ( iItem != -1 )
   {
	  if ( m_iSelected != iItem )
	  {
		 // Update info
		 m_iSelected = iItem;
	  }
   }

   CDragListBox::OnLButtonDown( nFlags, point );
} // CListBoxEx::OnLButtonDown


///*-
// FUNCTION NAME: CListBoxEx::OnLButtonDblClk
//
// DESCRIPTION:   Called when it receives a WM_LBUTTONDBLCLICK message.
//
// PARAMETER(S):
//                nFlags:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Flags
//
//                point:
//                   TYPE:          CPoint class
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Point of action
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxEx::OnLButtonDblClk( UINT   nFlags,
								  CPoint point )
{
   CDragListBox::OnLButtonDblClk( nFlags, point );

   if ( m_bAllowEditing )
   {
	  int     iItem;

	  ClientToScreen( &point );
	  iItem = ItemFromPt( point, FALSE );
	  TRACE1( "LButtonDblClk: %d\n", iItem );

	  if ( (iItem != -1)      &&
		   m_bAllowEditing    &&
		   (OnBeginEditing( iItem ) != FALSE) )
	  {
		 // Begin Editing
		 BeginEditing( iItem );
	  }
   }
} // CListBoxEx::OnLButtonDblClk


///*-
// FUNCTION NAME: CListBoxEx::OnLButtonUp
//
// DESCRIPTION:   Called when it receives a WM_LBUTTONUP message.
//
// PARAMETER(S):
//                nFlags:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Flags
//
//                point:
//                   TYPE:          CPoint class
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Point of action
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxEx::OnLButtonUp( UINT   nFlags,
							  CPoint point )
{
   CDragListBox::OnLButtonUp( nFlags, point );
} // CListBoxEx::OnLButtonUp



			   ///////////////////////////////////////////////
			   //               Overridables                //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CListBoxEx::OnBeginEditing
//
// DESCRIPTION:   Called when the item editing phase has been started.
//
// PARAMETER(S):
//                iItem:
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Zero-based index of the item.
//
// RETURN:        TRUE if the editing action can be performed. FALSE otherwise.
//
// NOTES:         None.
//+*/
BOOL CListBoxEx::OnBeginEditing( int iItem )
{
   UNUSED_ALWAYS( iItem );
   return TRUE;
} // CListBoxEx::OnBeginEditing


///*-
// FUNCTION NAME: CListBoxEx::OnEndEditing
//
// DESCRIPTION:   Called when the item editing phase has ended.
//
// PARAMETER(S):
//                iItem:
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Zero-based index of the item.
//
//                fCancel:
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   If the editing was canceled, this parameter is
//                                  TRUE. Otherwise, it is FALSE.
//
// RETURN:        TRUE to set the item's label to the edited text.
//                Return FALSE to reject the edited text and revert to
//                the original label.
//
// NOTES:         None.
//+*/
BOOL CListBoxEx::OnEndEditing( int  iItem,
							   BOOL fCanceled )
{
   UNUSED_ALWAYS( iItem );
   UNUSED_ALWAYS( fCanceled );

   return TRUE;
} // CListBoxEx::OnEndEditing


///*-
// FUNCTION NAME: CListBoxEx::OnBrowseButton
//
// DESCRIPTION:   Called when the browse button on the item is pressed.
//
// PARAMETER(S):
//                iItem:
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Zero-based index of the item.
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxEx::OnBrowseButton( int iItem )
{
   UNUSED_ALWAYS( iItem );
} // CListBoxEx::OnMoreButtonPressed



			   ///////////////////////////////////////////////
			   //             Public functions              //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CListBoxEx::BeginEditing
//
// DESCRIPTION:   Set the edit style
//
// PARAMETER(S):
//                iItem:
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Item index. If -1, editing is for the currently
//                                  selected item.
//
// RETURN:        None.
//
// NOTES:         This method does not call the OnBeginEditing function.
//+*/
void CListBoxEx::BeginEditing( int iItem )
{
   if ( m_bAllowEditing &&
	   (m_iEdited == -1) )
   {
	  if ( iItem == -1 )
	  {
		 iItem = m_iSelected;
	  }
	  else
	  if ( iItem > GetCount()-1 )
	  {
		 iItem = GetCount();
		 AddString( _T("") );
	  }

	  // Make it the current selection
	  SetCurSel( iItem );
	  m_iSelected = iItem;

	  // Save item index
	  m_iEdited = iItem;

	  // Retrieve item text
	  CString strItemText;
	  GetText( iItem, strItemText );

	  // Cache edit/button rectangles
	  GetItemRect( iItem, m_rcEdit );
	  m_rcButton.CopyRect( m_rcEdit );

	  // Adjust for the edit and the button
	  m_rcEdit.left += 5;
	  if ( m_dwEditStyle & LBEX_EDITBUTTON )
	  {
		 m_rcEdit.right -= 30;
	  }
	  else
	  {
		 m_rcEdit.right -= 5;
	  }
	  m_rcEdit.InflateRect( 0, 2 );

	  // Initialize the edit control with the item text
	  m_pEdit->SetWindowText( strItemText );

	  // Show the edit control
	  m_pEdit->Show( m_rcEdit );

	  if ( m_dwEditStyle & LBEX_EDITBUTTON )
	  {
		 m_rcButton.left = m_rcEdit.right;
		 m_rcButton.right -= 5;
		 m_rcButton.InflateRect( 0, 2 );
		 m_pBrowseButton->Show( m_rcButton );
	  }
   }
} // CListBoxEx::BeginEditing()s


///*-
// FUNCTION NAME: CListBoxEx::EditNew
//
// DESCRIPTION:   Adds a new item and begin editing
//
// PARAMETER(S):  None.
//
// RETURN:        None.
//
// NOTES:         This method does not call the OnBeginEditing function.
//+*/
void CListBoxEx::EditNew()
{
   BeginEditing( GetCount() );
} // CListBoxEx::EditNew()


///*-
// FUNCTION NAME: CListBoxEx::EndEditing
//
// DESCRIPTION:   Ends the editing of an item.
//
// PARAMETER(S):
//                fCancel:
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   If this parameter is TRUE, the control cancels
//                                  editing without saving the changes.
//                                  Otherwise, the control saves the changes to the
//                                  label.
//
// RETURN:        None.
//
// NOTES:         This method does not call the OnEndEditing function.
//+*/
void CListBoxEx::EndEditing( BOOL fCancel )
{
   TRACE( "EndEditing\n" );

   // Hide the edit box
   m_pEdit->Hide();

   m_pBrowseButton->Hide();

   // Update item text
   CString strNewItemText;
   m_pEdit->GetWindowText( strNewItemText );
   if ( strNewItemText.IsEmpty() )
   {
	  DeleteString( m_iEdited );
   }
   else
   if ( !fCancel )
   {
	  // Replace the text
	  SetItemText( m_iEdited, LPCTSTR(strNewItemText) );
	  // Select the edited item
	  SetCurSel( m_iEdited );
   }

   m_iEdited = -1;

   Invalidate();
} // CListBoxEx::EndEditing


///*-
// FUNCTION NAME: CListBoxEx::SetEditStyle
//
// DESCRIPTION:   Set the edit style
//
// PARAMETER(S):
//                dwEditStyle:
//                   TYPE:          DWORD
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Edit style.
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxEx::SetEditStyle( DWORD dwEditStyle, BOOL bWithBrowseButton )
{
   if ( m_dwEditStyle != dwEditStyle )
   {
	  m_dwEditStyle = dwEditStyle;
	  delete m_pEdit;
	  m_pEdit = 0;
	  CreateEdit(bWithBrowseButton);
   }
} // CListBoxEx::SetEditStyle


///*-
// FUNCTION NAME: CListBoxEx::SetEditText
//
// DESCRIPTION:   Sets the new edit text
//
// PARAMETER(S):
//                strNewText:
//                   TYPE:          CString class
//                   MODE:          In
//                   MECHANISM:     By const reference
//                   DESCRIPTION:   New edit text.
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxEx::SetEditText( const CString & strNewText ) const
{
   if ( m_pEdit && m_pEdit->IsWindowVisible() )
   {
	  m_pEdit->SetWindowText( LPCTSTR(strNewText) );
	  m_pEdit->SetFocus();
   }
} // CListBoxEx::SetEditText


///*-
// FUNCTION NAME: CListBoxEx::GetEditHandle
//
// DESCRIPTION:   Returns the edit handle
//
// PARAMETER(S):  None.
//
// RETURN:        The handle of the edit tool.
//                If no editing operation has never been performed, the function
//                returns NULL.
//                You cannot destroy the edit control, but you can subclass it.
//
// NOTES:         None.
//+*/
HWND CListBoxEx::GetEditHandle() const
{
   return m_pEdit ? m_pEdit->m_hWnd : NULL;
} // CListBoxEx::GetEditHandle


///*-
// FUNCTION NAME: CListBoxEx::SetItem
//
// DESCRIPTION:   Set item text and data
//
// PARAMETER(S):
//                iItem:
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Zero-based index of the item.
//
//                szItemText:
//                   TYPE:          char
//                   MODE:          In
//                   MECHANISM:     By address
//                   DESCRIPTION:   Addres of the new item text.
//
//                dwItemData:
//                   TYPE:          DWORD
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Custom item data.
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxEx::SetItem( int     iItem,
						  LPCTSTR szItemText,
						  DWORD   dwItemData )
{
   ASSERT( iItem < GetCount() );

   SendMessage( WM_SETREDRAW, FALSE, 0 );

   DeleteString( iItem );
   InsertString( iItem, szItemText );
   SetItemData( iItem, dwItemData );

   SendMessage( WM_SETREDRAW, TRUE, 0 );
} // CListBoxEx::SetItem


///*-
// FUNCTION NAME: CListBoxEx::SetItemText
//
// DESCRIPTION:   Set item text
//
// PARAMETER(S):
//                iItem:
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Zero-based index of the item.
//
//                szItemText:
//                   TYPE:          char
//                   MODE:          In
//                   MECHANISM:     By const address
//                   DESCRIPTION:   Addres of the new item text.
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxEx::SetItemText( int     iItem,
							  LPCTSTR szItemText )
{
   ASSERT( iItem < GetCount() );

   DWORD dwItemData;
   dwItemData = (DWORD)GetItemData( iItem );
   SetItem( iItem, szItemText, dwItemData );
} // CListBoxEx::SetItemText


///*-
// FUNCTION NAME: CListBoxEx::SwapItems
//
// DESCRIPTION:   Called to swap the two items
//
// PARAMETER(S):
//                iFirstItem:
//                   TYPE:          int.
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Zero-based index of the first item to be swapped.
//
//                iSecondItem:
//                   TYPE:          int.
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Zero-based index of the second item to be swapped.
//
// RETURN:        The new index of the item.
//
// NOTES:         None.
//+*/
void CListBoxEx::SwapItems( int iFirstItem,
							int iSecondItem )
{
   ASSERT( iFirstItem < GetCount() );
   ASSERT( iSecondItem < GetCount() );

   if ( iFirstItem != iSecondItem )
   {
	  // Cache the first item data
	  CString strFirstItem;
	  DWORD dwFirstItemData;

	  GetText( iFirstItem, strFirstItem );
	  dwFirstItemData = (DWORD)GetItemData( iFirstItem );

	  // Cache the second item data
	  CString strSecondItem;
	  DWORD dwSecondItemData;

	  GetText( iSecondItem, strSecondItem );
	  dwSecondItemData = (DWORD)GetItemData( iSecondItem );

	  // Insert the items in reverse order
	  if ( iFirstItem < iSecondItem )
	  {
		 SetItem( iFirstItem, strSecondItem, dwSecondItemData );
		 SetItem( iSecondItem, strFirstItem, dwFirstItemData );
	  }
	  else
	  {
		 SetItem( iSecondItem, strFirstItem, dwFirstItemData );
		 SetItem( iFirstItem, strSecondItem, dwSecondItemData );
	  }
   }
} // CListBoxEx::SwapItems


///*-
// FUNCTION NAME: CListBoxEx::MoveItemUp
//
// DESCRIPTION:   Called to move the item up.
//
// PARAMETER(S):
//                iItem:
//                   TYPE:          int.
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Zero-based index of the item to be moved.
//
// RETURN:        The new index of the item.
//
// NOTES:         None.
//+*/
int CListBoxEx::MoveItemUp( int iItem )
{
   ASSERT( iItem > 0 );

   if ( iItem > 0 )
   {
	  SwapItems( iItem, iItem-1 );
	  SetCurSel( iItem - 1 );
   }
   return iItem;
} // CListBoxEx::MoveItemUp()


///*-
// FUNCTION NAME: CListBoxEx::MoveItemDown
//
// DESCRIPTION:   Called to move the item down.
//
// PARAMETER(S):
//                iItem:
//                   TYPE:          int.
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Zero-based index of the item to be moved.
//
// RETURN:        The new index of the item.
//
// NOTES:         None.
//+*/
int CListBoxEx::MoveItemDown( int iItem )
{
   ASSERT( iItem >= 0 );

   if ( iItem != GetCount()-1 )
   {
	  SwapItems( iItem, iItem+1 );
	  SetCurSel( iItem + 1 );
   }
   return iItem;
} // CListBoxEx::MoveItemDown


///*-
// FUNCTION NAME: CListBoxEx::OnEndEditMessage
//
// DESCRIPTION:   Ends the editing of an item.
//
// PARAMETER(S):
//                wParam:
//                   TYPE:          WPARAM
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Unused.
//
//                lParam:
//                   TYPE:          LPARAM
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Pointer to a IPEENDEDITINFO structure.
//
// RETURN:        TRUE to end editing process, FALSE to continue anyway.
//
// NOTES:         This message handler calls OnEndEditing callback, if necessary.
//+*/
LRESULT CListBoxEx::OnEndEditMessage( WPARAM wParam,
									  LPARAM lParam )
{
   UNUSED_ALWAYS( wParam );

   BOOL bEndEdit;

   LPIPCENDEDITINFO lpEndEditInfo = (LPIPCENDEDITINFO)lParam;
   switch ( lpEndEditInfo->uKind )
   {
	  case IPEK_KEY:

		 if ( lpEndEditInfo->nVirtKey == VK_ESCAPE )
		 {
			bEndEdit = TRUE;
			OnEndEditing( m_iEdited, TRUE );
			EndEditing( TRUE );
		 }
		 else
		 {
			bEndEdit = TRUE;
			EndEditing( !OnEndEditing( m_iEdited, FALSE ) );
		 }
		 break;

	  case IPEK_ACTION:

		 bEndEdit = FALSE; // Superfluous
		 OnBrowseButton( m_iEdited );
		 break;

	  case IPEK_FOCUS:

		 if ( (lpEndEditInfo->hNewWnd == m_pEdit->m_hWnd) ||
			  (lpEndEditInfo->hNewWnd == m_pBrowseButton->m_hWnd) )
		 {
			bEndEdit = FALSE;
		 }
		 else
		 {
			bEndEdit = TRUE;
			EndEditing( !OnEndEditing( m_iEdited, FALSE ) );
		 }
		 break;

	  default:

		 bEndEdit = TRUE;
   }

   return bEndEdit;
} // CListBoxEx::OnEndEditMessage




//////////////////////////////////////////////////////////////////////////////
//                                                                          //
//                       CListBoxExBuddy class                              //
//                                                                          //
//////////////////////////////////////////////////////////////////////////////


			   ///////////////////////////////////////////////
			   //               Message map                 //
			   ///////////////////////////////////////////////



BEGIN_MESSAGE_MAP(CListBoxExBuddy, CWnd)
   //{{AFX_MSG_MAP(CListBoxExBuddy)
   ON_WM_PAINT()
   ON_WM_MOUSEMOVE()
   ON_WM_LBUTTONDOWN()
   ON_WM_LBUTTONUP()
   ON_WM_SIZE()
   ON_WM_NCMOUSEMOVE()
	ON_WM_CREATE()
	//}}AFX_MSG_MAP
   //ON_NOTIFY( TTN_NEEDTEXT, LBB_TTIP_ID, OnTooltipNeedText )
END_MESSAGE_MAP()



			   ///////////////////////////////////////////////
			   //           Constructor/Destructor          //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CListBoxExBuddy::CListBoxExBuddy()
//
// DESCRIPTION:   CListBoxExBuddy class constructor
//
// PARAMETER(S):  None.
//
// RETURN:        None
//
// NOTES:         None
//+*/
CListBoxExBuddy::CListBoxExBuddy()
{
   // Load the bitmap by using a temp file
   TCHAR szFileName[_MAX_PATH + 1];
   _ttmpnam_s( szFileName, _MAX_PATH );

   FILE *pfBmpFile = 0;
   _tfopen_s(&pfBmpFile, szFileName, _T("wb") );
   fwrite( lbbuddy_data, sizeof(unsigned char), __bmp_size, pfBmpFile );
   fclose( pfBmpFile );
   m_ButtonBitmap.Attach( (HBITMAP)LoadImage( NULL,
											  szFileName,
											  IMAGE_BITMAP,
											  0,
											  0,
											  LR_LOADFROMFILE | LR_LOADMAP3DCOLORS ) );
   _tunlink( szFileName );

   // Init other data
   m_bButtonPressed = FALSE;
   m_iButton = __BMP_NUMBTN;
   m_pListBoxEx = NULL;

   m_arcButtons = new CRect[ __BMP_NUMBTN ];

#ifdef _DEBUG
   // Verify the dimensions
   BITMAP BmpInfo;
   m_ButtonBitmap.GetObject( sizeof(BITMAP), &BmpInfo );
   ASSERT( BmpInfo.bmWidth == __BMP_WIDTH );
   ASSERT( BmpInfo.bmHeight == __BMP_HEIGHT );
#endif
} // CListBoxEx::CListBoxEx



///*-
// FUNCTION NAME: CListBoxExBuddy::~CListBoxExBuddy()
//
// DESCRIPTION:   CListBoxExBuddy class destructor
//
// PARAMETER(S):  None.
//
// RETURN:        None
//
// NOTES:         None
//+*/
CListBoxExBuddy::~CListBoxExBuddy()
{
   delete[] m_arcButtons;
} // CListBoxExBuddy::~CListBoxExBuddy



			   ///////////////////////////////////////////////
			   //             Initialization                //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CListBoxExBuddy::OnCreate
//
// DESCRIPTION:   Called when the window handle is created
//
// PARAMETER(S):
//                lpCreateStruct:
//                   TYPE:          CREATESTRUCT structure
//                   MODE:          In
//                   MECHANISM:     By reference
//                   DESCRIPTION:   Allows to change window features
//
// RETURN:        Inherited value.
//
// NOTES:         None
//+*/
int CListBoxExBuddy::OnCreate( LPCREATESTRUCT lpCreateStruct )
{
   if ( CWnd::OnCreate(lpCreateStruct) == -1 )
   {
	  return -1;
   }

   // Enable Tooltips
   CreateTooltips();

   return 0;
} // CListBoxExBuddy::OnCreate()


///*-
// FUNCTION NAME: CListBoxEx::PreSubclassWindow
//
// DESCRIPTION:   Called before the window handle is attached to a dialog item.
//
// PARAMETER(S):  None.
//
// RETURN:        Inherited value.
//
// NOTES:         None
//+*/
void CListBoxExBuddy::PreSubclassWindow()
{
   // Enable Tooltips
   CreateTooltips();

   // Send a WM_SIZE message, as WM_CREATE would do
   CRect rcClient;
   GetClientRect( &rcClient );
   OnSize( 0, rcClient.Width(), rcClient.Height() );

   // Call default
   CWnd::PreSubclassWindow();
} // CListBoxExBuddy::PreSubclassWindow()



			   ///////////////////////////////////////////////
			   //                 Paint                     //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: DrawBitmap
//
// DESCRIPTION:   Draw bitmaps helper
//
// PARAMETER(S):
//                dc
//                   TYPE:          CDC
//                   MODE:          In
//                   MECHANISM:     By reference
//                   DESCRIPTION:   Target dc
//                bmp
//                   TYPE:          CBitmap
//                   MODE:          In
//                   MECHANISM:     By reference
//                   DESCRIPTION:   bitmap object
//                x
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   x-pos
//                y
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   y-pos
//
//                cx
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   width
//                cy
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   height
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
static
void DrawBitmap( CDC     & dc,
				 CBitmap & bmp,
				 int       x,
				 int       y,
				 int       cx,
				 int       cy )
{
   CDC memDC;
   memDC.CreateCompatibleDC( &dc );

   CBitmap *poldbmp = memDC.SelectObject( &bmp );

   dc.BitBlt( x, y, cx, cy, &memDC, 0, 0, SRCCOPY );

   memDC.SelectObject( poldbmp );
   memDC.DeleteDC();
} // DrawBitmap()


///*-
// FUNCTION NAME: CListBoxExBuddy::OnPaint
//
// DESCRIPTION:   Called in response to a WM_PAINT message.
//
// PARAMETER(S):  None.
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxExBuddy::OnPaint()
{
   CPaintDC dc( this ); // device context for painting

   // Create a compatible memory DC
   CDC memDC;
   memDC.CreateCompatibleDC( &dc );

   // Get aware of the size of the client area
   CRect rcClient;
   GetClientRect( &rcClient );

   // This is used to center the button bitmap
   int nBmpTopY = (rcClient.Height() - __BMP_HEIGHT) / 2;

   // To store old selected objects
   CBitmap *pOldBmp;
   CFont   *pOldFont;

   // Select the font
   CFont font;
   font.Attach( (HFONT)GetStockObject( DEFAULT_GUI_FONT ) );
   pOldFont = memDC.SelectObject( &font );

   // Select the out-of-screen bitmap
   CBitmap memBmp;
   memBmp.CreateCompatibleBitmap( &dc,
								   rcClient.Width(),
								   rcClient.Height() );
   pOldBmp = memDC.SelectObject( &memBmp );

   // Erase the background
   CBrush brush;
   brush.CreateSolidBrush( ::GetSysColor(COLOR_3DFACE) );
   memDC.FillRect( &rcClient, &brush );
   brush.DeleteObject();

   //
   // Window Text
   //

   // Prepare to draw the text transparently
   memDC.SetBkMode( TRANSPARENT );
   memDC.SetTextColor( ::GetSysColor(COLOR_WINDOWTEXT) );

   // Draw the text
   CString strWindowText;
   GetWindowText( strWindowText );
   memDC.DrawText( strWindowText, rcClient, DT_SINGLELINE | DT_VCENTER );

   //
   // Buttons
   //

   // Draw the button bitmap
   DrawBitmap( memDC,
			   m_ButtonBitmap,
			   rcClient.right - __BMP_WIDTH - 2,
			   nBmpTopY,
			   __BMP_WIDTH,
			   __BMP_HEIGHT );

   // Draw the button edge
   if ( m_iButton != __BMP_NUMBTN )
   {
	  CRect rcButtonEdge( rcClient.right - (__BMP_NUMBTN - m_iButton)*__BMP_BTNWID - 2,
						  nBmpTopY,
						  rcClient.right - (__BMP_NUMBTN - m_iButton - 1)*__BMP_BTNWID - 2,
						  __BMP_HEIGHT + nBmpTopY );
	  memDC.DrawEdge( &rcButtonEdge,
					   m_bButtonPressed ? BDR_SUNKENOUTER : BDR_RAISEDINNER,
					   BF_RECT );
   }

   //
   // Bit copy
   //

   dc.BitBlt(  2,
			   0,
			   rcClient.Width()-2,
			   rcClient.Height(),
			  &memDC,
			   0,
			   0,
			   SRCCOPY );

   //
   // Tidy up
   //
   // Select the bitmap out of the device context
   memDC.SelectObject( pOldBmp );

   // Select the font out of the device context
   memDC.SelectObject( pOldFont );

} // CListBoxExBuddy::OnPaint()



			   ///////////////////////////////////////////////
			   //             Mouse Management              //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CListBoxExBuddy::FindButton
//
// DESCRIPTION:   Finds a button given a point.
//
// PARAMETER(S):
//                point:
//                   TYPE:          CPoint class
//                   MODE:          In
//                   MECHANISM:     By const reference
//                   DESCRIPTION:   Point of action
//
// RETURN:        The index of the button, between 0 and 4 (bounds included).
//
// NOTES:         None.
//+*/
int CListBoxExBuddy::FindButton( const CPoint & point )
{
   // Find the button
   UINT iIndex = 0;
   for ( ; iIndex < __BMP_NUMBTN; iIndex++ )
   {
	  if ( m_arcButtons[iIndex].PtInRect( point ) )
	  {
		 break;
	  }
   }

   return iIndex;
} // CListBoxExBuddy::FindButton()


///*-
// FUNCTION NAME: CListBoxExBuddy::InvalidateButton
//
// DESCRIPTION:   Called to redraw a button.
//
// PARAMETER(S):
//                iIndex:
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Button index
//
//                bUpdateWindow:
//                   TYPE:          BOOL
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   If TRUE, calls UpdateWindow()
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxExBuddy::InvalidateButton( int  iIndex,
										BOOL bUpdateWindow /*= TRUE */ )
{
   if ( iIndex < __BMP_NUMBTN )
   {
	  InvalidateRect( &m_arcButtons[ iIndex ], FALSE );
   }
   if ( bUpdateWindow ) UpdateWindow();
} // CListBoxExBuddy::InvalidateButton


///*-
// FUNCTION NAME: CListBoxExBuddy::OnMouseMove
//
// DESCRIPTION:   Called when it receives a WM_MOUSEMOVE message.
//
// PARAMETER(S):
//                nFlags:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Flags
//
//                point:
//                   TYPE:          CPoint class
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Point of action
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxExBuddy::OnMouseMove( UINT   nFlags,
								   CPoint point )
{
   if ( !m_bButtonPressed )
   {
	  UINT iIndex = FindButton( point );

	  // If found a button, update info
	  if ( iIndex != m_iButton )
	  {
		 InvalidateButton( m_iButton, FALSE );
		 m_iButton = iIndex;
		 InvalidateButton( m_iButton, TRUE );
	  }

   }

   // Releay tooltip events
   //m_ToolTip.RelayEvent( const_cast<MSG *>( GetCurrentMessage() ) );
   //m_ToolTip.Activate( TRUE );

   CWnd::OnMouseMove(nFlags, point);
} // CListBoxExBuddy::OnMouseMove


///*-
// FUNCTION NAME: CListBoxExBuddy::OnLButtonDown
//
// DESCRIPTION:   Called when it receives a WM_LBUTTONDOWN message.
//
// PARAMETER(S):
//                nFlags:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Flags
//
//                point:
//                   TYPE:          CPoint class
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Point of action
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxExBuddy::OnLButtonDown( UINT   nFlags,
									 CPoint point )
{
   // Capture the mouse
   SetCapture();

   // Find the button
   m_iButton = FindButton( point );

   // Redraw the button
   if ( m_iButton != __BMP_NUMBTN )
   {
	  m_bButtonPressed = TRUE;

	  // Redraw only the affected button
	  InvalidateRect( &m_arcButtons[ m_iButton ], FALSE );
	  UpdateWindow();
   }

   CWnd::OnLButtonDown(nFlags, point);
} // CListBoxExBuddy::OnLButtonDown


///*-
// FUNCTION NAME: CListBoxExBuddy::OnLButtonUp
//
// DESCRIPTION:   Called when it receives a WM_LBUTTONUP message.
//
// PARAMETER(S):
//                nFlags:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Flags
//
//                point:
//                   TYPE:          CPoint class
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Point of action
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxExBuddy::OnLButtonUp( UINT   nFlags,
								   CPoint point )
{
   // Find the button
   UINT iButton = FindButton( point );

   // Accept only clicks that occur on the same button where the mouse was pressed
   if ( iButton == m_iButton )
   {
	  // Take action, if necessary
	  if ( m_iButton != __BMP_NUMBTN )
	  {
		 DoClick( m_iButton );
	  }

   }

   // Set default conditions
   m_bButtonPressed = FALSE;

   // Redraw
   Invalidate( FALSE );

   // Memorize last
   m_iButton = iButton;

   // Release mouse capture
   ReleaseCapture();

   // Call base
   CWnd::OnLButtonUp(nFlags, point);
} // CListBoxExBuddy::OnLButtonUp


// FUNCTION NAME: CListBoxExBuddy::DoClick
//
// DESCRIPTION:   Called when a click occurs on one of the action button.
//
// PARAMETER(S):
//                iIndex:
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Zero-based index of the button
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxExBuddy::DoClick( int iIndex )
{
   int iSelected = m_pListBoxEx->GetCurSel();

   switch ( iIndex )
   {
	  case __BTN_NEW:

		 m_pListBoxEx->EditNew();
		 break;

	  case __BTN_DEL:

		 if ( iSelected != -1 ) m_pListBoxEx->DeleteString( iSelected );
		 break;

	  case __BTN_UP:

		 if ( iSelected != -1 ) m_pListBoxEx->MoveItemUp( iSelected );
		 break;

	  case __BTN_DOWN:

		 if ( iSelected != -1 ) m_pListBoxEx->MoveItemDown( m_pListBoxEx->GetCurSel() );
		 break;
   }
} // CListBoxExBuddy::DoClick


///*-
// FUNCTION NAME: CListBoxExBuddy::OnNcMouseMove
//
// DESCRIPTION:   Called when it receives a WM_NCMOUSEMOVE message.
//
// PARAMETER(S):
//                nHitTest:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Hit test
//
//                point:
//                   TYPE:          CPoint class
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Point of action
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxExBuddy::OnNcMouseMove( UINT   nHitTest,
									 CPoint point )
{
   // Redraw the affected button
   InvalidateButton( m_iButton, FALSE );
   m_iButton = FindButton( point );
   InvalidateButton( m_iButton, TRUE );

   // Call base
   CWnd::OnNcMouseMove(nHitTest, point);
} // CListBoxExBuddy::OnNcMouseMove



			   ///////////////////////////////////////////////
			   //            Tooltip Management             //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CListBoxExBuddy::CreateTooltips
//
// DESCRIPTION:   Creates the tooltip control and assigns the ttols to it.
//
// PARAMETER(S):  None.
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxExBuddy::CreateTooltips()
{
   // Create the tooltip
   m_ToolTip.Create( this );

   // Set tip common data
   TOOLINFO ttInfo;
   ttInfo.cbSize   = sizeof( TOOLINFO );
   ttInfo.uFlags   = TTF_SUBCLASS;
   ttInfo.hwnd     = m_hWnd;
   ttInfo.rect     = CRect( 0, 0, 0, 0 ); // OnSize will resize it
   ttInfo.hinst    = NULL;
   ttInfo.lpszText = LPSTR_TEXTCALLBACK;
   ttInfo.lParam   = 0;

   // Add tooltips for each button
   for ( int iTip = 0; iTip < __BMP_NUMBTN; iTip++ )
   {
	  ttInfo.uId = iTip+1;
	  m_ToolTip.SendMessage( TTM_ADDTOOL, 0, (LPARAM)&ttInfo );
	  m_ToolTip.Activate( TRUE );
   }
} // CListBoxExBuddy::CreateTooltips()


///*-
// FUNCTION NAME: CListBoxExBuddy::SetTipText
//
// DESCRIPTION:   Sett the appropriate tip text for the button
//
// PARAMETER(S):
//                nID:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Button identifier (index)
//
//                szTipText:
//                   TYPE:          TCHAR
//                   MODE:          In
//                   MECHANISM:     By pointer
//                   DESCRIPTION:   Pointer to a preallocated buffer to be filled
//                                  with the tip text
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxExBuddy::SetTipText( UINT   nID,
								  LPTSTR szTipText,
								  rsize_t len)
{
   TCHAR *aszTips[] = { _T("New"),
						_T("Delete"),
						_T("Move Up"),
						_T("Move Down") };

   // Set tooltip text
   if ( nID < __BMP_NUMBTN )
   {
	  _tcscpy_s( szTipText, len, aszTips[ nID ] );
   }
} // CListBoxExBuddy::SetTipText()



			   ///////////////////////////////////////////////
			   //             Other Messages                //
			   ///////////////////////////////////////////////



///*-
// FUNCTION NAME: CListBoxExBuddy::OnSize
//
// DESCRIPTION:   Called when it receives a WM_SIZE message.
//
// PARAMETER(S):
//                nType:
//                   TYPE:          unsigned int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Specifies the type of resizing requested.
//
//                cx:
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   New width of the client area
//
//                cy:
//                   TYPE:          int
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   New height of the client area
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
void CListBoxExBuddy::OnSize( UINT nType,
							  int  cx,
							  int  cy )
{
   // Get aware of the size of the client area
   CRect rcClient;
   GetClientRect( &rcClient );

   // This is used to center the button bitmap
   int nBmpTopY = (rcClient.Height() - __BMP_HEIGHT) / 2;

   // Update buttons positions
   TOOLINFO ttInfo;
   for ( int iIndex = 0; iIndex < __BMP_NUMBTN; iIndex++ )
   {
	  m_arcButtons[ iIndex ].top     = nBmpTopY;
	  m_arcButtons[ iIndex ].left    = cx - (__BMP_NUMBTN-iIndex)*__BMP_BTNWID;
	  m_arcButtons[ iIndex ].bottom  = __BMP_HEIGHT + nBmpTopY;
	  m_arcButtons[ iIndex ].right   = cx - (__BMP_NUMBTN-iIndex-1)*__BMP_BTNWID;

	  // Resize tooltip area
	  ttInfo.cbSize   = sizeof( TOOLINFO );
	  ttInfo.hwnd     = m_hWnd;
	  ttInfo.uId      = iIndex+1;
	  ttInfo.rect     = m_arcButtons[ iIndex ];
	  m_ToolTip.SendMessage( TTM_NEWTOOLRECT, 0, (LPARAM)&ttInfo );
   }


   // Call base
   CWnd::OnSize( nType, cx, cy );
} // CListBoxExBuddy::OnSize


///*-
// FUNCTION NAME: CListBoxExBuddy::OnNotify
//
// DESCRIPTION:   Called when it receives a WM_NOTIFY message.
//
// PARAMETER(S):
//                wParam:
//                   TYPE:          WPARAM
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Identifier of the common control sending the message.
//
//                lParam:
//                   TYPE:          LPARAM
//                   MODE:          In
//                   MECHANISM:     By value
//                   DESCRIPTION:   Address of an NMHDR structure that contains
//                                  the notification code and additional information.
//
//                pResult:
//                   TYPE:          LRESULT
//                   MODE:          In
//                   MECHANISM:     By address
//                   DESCRIPTION:   Message result.
//
// RETURN:        None.
//
// NOTES:         None.
//+*/
BOOL CListBoxExBuddy::OnNotify( WPARAM   wParam,
								LPARAM   lParam,
								LRESULT *pResult )
{
   UINT nCode = ((NMHDR *)lParam)->code;

   // Get tooltip notification
   if ( nCode == TTN_GETDISPINFO )
   {
	  UINT nID = (UINT)((NMHDR *)lParam)->idFrom - 1;
	  SetTipText( nID,
				  ((NMTTDISPINFO *)lParam)->szText,
				  sizeof(((NMTTDISPINFO *)lParam)->szText) );
	  return TRUE;
   }

   // Call base
   return CWnd::OnNotify( wParam, lParam, pResult );
} // CListBoxExBuddy::OnNotify
