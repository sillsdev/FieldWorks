//*-
//
// MODULE NAME:   InPlaceCtrls.h
//
// DESCRIPTION:   Contains class declarations for in-place controls
//
// AUTHOR     :   Copyright (C) Stefano Passiglia, December 1999
//                passig@geocities.com
//                You can reuse and redistribute this code, provided this header is
//                kept as is.
//+*/

#ifndef _INPLACECTRLS_H
#define _INPLACECTRLS_H

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000


//
// Include Files
//
#include "stdafx.h"


//
// An "In-Place control is a standard Windows control that is used to
// help editing values.
// These controls send an end-editing message to the parent whenever they
// think that an action should terminate the editing process.
//
// Currently signaled events are:
//   - VK_RETURN, VK_ESCAPE key press, for edit controls (IPEK_KEY)
//   - CLICK for button (IPEK_ACTION)
//   - WM_KILLFOCUS, for any control (IPEK_FOCUS)
//

// This registered message is used to communicate to the user
// that an event occured so that the editing process should stop.
// User must call RegisterWindowMessage to register this message
// with the provided string.
//
// wParam: Unused
// lParam: Pointer to a IPEENDEDITINFO structure
// Return Value: TRUE to end editing process, FALSE to continue anyway
//
#define IPCMSGSTRING    TEXT("ipc_CtrlMsg")

// In-place event kind
#define IPEK_KEY     1
#define IPEK_ACTION  2
#define IPEK_FOCUS   3


#pragma pack( push, 1 )
typedef struct tagIPCENDEDITINFO
{
   HWND  hwndSrc;    // Event source
   UINT  uKind;      // Event (key press, focus change, etc )
   union
   {
	  UINT  uID;        // Control ID (for actions)
	  UINT  nVirtKey;   // Virtual key
	  HWND  hNewWnd;    // New window with focus
   };
   POINT pt;         // Event point, in screen coordinates
} IPCENDEDITINFO, FAR *LPIPCENDEDITINFO;
#pragma pack( pop )


//
// Function declarations.
//
// None.


//
// Class declarations.
//

//
// CInPlaceEdit class
//

class CInPlaceEdit: public CEdit
{
   DECLARE_DYNCREATE( CInPlaceEdit )

public:

   // Constructor/Destructor
   CInPlaceEdit();
   virtual ~CInPlaceEdit();

public:

   void Show( CRect rcEdit );
   void Hide();

   //{{AFX_VIRTUAL(CInPlaceEdit)
public:
   virtual BOOL PreTranslateMessage(MSG* pMsg);
protected:
   virtual BOOL PreCreateWindow(CREATESTRUCT& cs);
   //}}AFX_VIRTUAL

protected:


private:

   HWND m_hParent;

   //{{AFX_MSG(CInPlaceEdit)
   afx_msg int OnCreate( LPCREATESTRUCT lpCreateStruct );
   afx_msg void OnKillFocus( CWnd *pNewWnd );
   //}}AFX_MSG
   DECLARE_MESSAGE_MAP()

}; // class CInPlaceEdit



//
// CInPlaceButton class
//

class CInPlaceButton: public CButton
{
   DECLARE_DYNCREATE( CInPlaceButton )

public:

   // Constructor/Destructor
   CInPlaceButton();
   virtual ~CInPlaceButton();

public:

   void Show( CRect rcButton );
   void Hide();

   //{{AFX_VIRTUAL(CInPlaceButton)
public:
   virtual BOOL PreTranslateMessage(MSG* pMsg);
protected:
   virtual BOOL PreCreateWindow(CREATESTRUCT& cs);
   //}}AFX_VIRTUAL

protected:


private:

   HWND m_hParent;

   BOOL m_bInAction;

   //{{AFX_MSG(CInPlaceButton)
   afx_msg int OnCreate( LPCREATESTRUCT lpCreateStruct );
   afx_msg void OnKillFocus( CWnd *pNewWnd );
   afx_msg void OnClick();
   //}}AFX_MSG
   DECLARE_MESSAGE_MAP()

}; // class CInPlaceButton


#endif // _INPLACECTRLS_H