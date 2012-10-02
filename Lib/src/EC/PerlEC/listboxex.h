//*-
//
// MODULE NAME:   ListBoxEx.h
//
// DESCRIPTION:   CListBoxEx class declaration
//
// AUTHOR     :   Copyright (C) Stefano Passiglia, December 1999
//                passig@geocities.com
//                You can reuse and redistribute this code, provided this header is
//                kept as is.
//
//+*/

#ifndef _LISTBOXEX_H
#define _LISTBOXEX_H

#if _MSC_VER > 1000
#   pragma once
#endif // _MSC_VER > 1000


//
// Include Files
//
#include "stdafx.h"
#include "afxcmn.h" // for CDragListBox


//
// Constant definitions.
//

// Listbox styles
#define LBEX_STYLE\
   WS_CHILD | WS_VISIBLE | WS_VSCROLL|\
   LBS_EXTENDEDSEL | LBS_NOINTEGRALHEIGHT | LBS_HASSTRINGS |\
   LBS_NOTIFY | LBS_WANTKEYBOARDINPUT | LBS_DISABLENOSCROLL |\
   LBS_OWNERDRAWFIXED

// Listbox extended styles
#define LBEX_EXSTYLE\
   WS_EX_CLIENTEDGE


// Listbox editing mode
#define LBEX_EDITBUTTON    0x4000L     // If set, shows a "..." button near the edit box


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

class CListBoxExBuddy;
class CInPlaceEdit;
class CInPlaceButton;

class CListBoxEx : public CDragListBox
{
   DECLARE_DYNCREATE( CListBoxEx )

public:

   // Constructor/Destructor
   CListBoxEx();
   virtual ~CListBoxEx();

public:

   // "Allow" methods.
   // Editing & drag are anabled by default
   void AllowEditing( BOOL bAllowEditing = TRUE )
   {
	  m_bAllowEditing = bAllowEditing;
   };

   void AllowDrag( BOOL bAllowDrag = TRUE )
   {
	  m_bAllowDrag = bAllowDrag;
   };

   // Editing methods
   void BeginEditing( int iItem );
   void EndEditing( BOOL fCancel );

   // Add a new empty string and begin editing
   void EditNew();

   void SetEditStyle( DWORD dwEditStyle, BOOL bWithBrowseButton = true );

   HWND GetEditHandle() const;

   void SetEditText( const CString & strNewText ) const;

   // Item methods
   int MoveItemUp( int iItem );
   int MoveItemDown( int iItem );
   void SwapItems( int iFirstItem, int iSecondItem );

   void SetItem( int iItem, LPCTSTR szItemText, DWORD dwItemData );
   void SetItemText( int iItem, LPCTSTR szItemText );

public:

   // Virtual (overridables) events
   virtual BOOL OnBeginEditing( int iItem );
   virtual BOOL OnEndEditing( int iItem, BOOL fCanceled );

   virtual void OnBrowseButton( int iItem );

public:

   // Data members

   // Overrides
   // ClassWizard generated virtual function overrides
   //{{AFX_VIRTUAL(CListBoxEx)
public:
   virtual BOOL OnChildNotify(UINT message, WPARAM wParam, LPARAM lParam, LRESULT* pLResult);
protected:
   virtual BOOL PreCreateWindow(CREATESTRUCT& cs);
   virtual void PreSubclassWindow();
   virtual void MeasureItem( LPMEASUREITEMSTRUCT lpMeasureItemStruct );
   virtual void DrawItem( LPDRAWITEMSTRUCT lpDrawItemStruct );
   virtual void DrawInsert( int nIndex );
   //}}AFX_VIRTUAL

   void DrawSeparator( int nIndex );

   // Generated message map functions
protected:
   //{{AFX_MSG(CListBoxEx)
   afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
   afx_msg void OnLButtonDown(UINT nFlags, CPoint point);
   afx_msg void OnLButtonDblClk(UINT nFlags, CPoint point);
   afx_msg void OnLButtonUp(UINT nFlags, CPoint point);
   afx_msg void OnSysKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags);
   afx_msg void OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags);
	//}}AFX_MSG
   afx_msg LRESULT OnEndEditMessage(WPARAM wParam, LPARAM lParam);

   DECLARE_MESSAGE_MAP()

private:

   void CreateEdit(BOOL bWithBrowseButton = true);

private:

   CListBoxExBuddy *m_pBuddy;

   DWORD            m_dwEditStyle;

   CInPlaceEdit    *m_pEdit;
   CRect            m_rcEdit;

   CInPlaceButton  *m_pBrowseButton;
   CRect            m_rcButton;

   int              m_iSelected;
   int              m_iEdited;

   BOOL             m_bAllowEditing;
   BOOL             m_bAllowDrag;
}; // class CListBoxEx



class CListBoxExBuddy: public CWnd
{
public:

   CListBoxExBuddy();
   virtual ~CListBoxExBuddy();

public:

   void SetListbox( CListBoxEx *pListBox )
   {
	  m_pListBoxEx = pListBox;
   };

   // Overrides
   // ClassWizard generated virtual function overrides
   //{{AFX_VIRTUAL(CListBoxExBuddy)
protected:
   virtual void PreSubclassWindow();
   virtual BOOL OnNotify(WPARAM wParam, LPARAM lParam, LRESULT* pResult);
   //}}AFX_VIRTUAL

   // Generated message map functions
protected:
   //{{AFX_MSG(CListBoxExBuddy)
   afx_msg void OnPaint();
   afx_msg void OnMouseMove(UINT nFlags, CPoint point);
   afx_msg void OnLButtonDown(UINT nFlags, CPoint point);
   afx_msg void OnLButtonUp(UINT nFlags, CPoint point);
   afx_msg void OnSize(UINT nType, int cx, int cy);
   afx_msg void OnNcMouseMove(UINT nHitTest, CPoint point);
	afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
	//}}AFX_MSG
   DECLARE_MESSAGE_MAP()

private:

   void CreateTooltips();
   void SetTipText( UINT nID, LPTSTR szTipText, rsize_t len );

   int FindButton( const CPoint & point );
   void InvalidateButton( int iIndex, BOOL bUpdateWindow = TRUE );
   void DoClick( int iIndex );

private:

   CListBoxEx   *m_pListBoxEx;

   CBitmap       m_ButtonBitmap;

   UINT          m_iButton;
   BOOL          m_bButtonPressed;
   CRect        *m_arcButtons;

   CToolTipCtrl  m_ToolTip;

}; // class CListBoxExBuddy



/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(_LISTBOXEX_H)
