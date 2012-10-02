// CAutoConfigPropertyPage.cpp : implementation file
//

#include "stdafx.h"
#include "AutoConfigPropertyPage.h"
#include "AutoConfigSheet.h"

// CAutoConfigPropertyPage dialog

IMPLEMENT_DYNAMIC(CAutoConfigPropertyPage, CPropertyPage)

// don't care ctor (for no display--e.g. with the About dialog via the DisplayTestPage method
CAutoConfigPropertyPage::CAutoConfigPropertyPage()
  : CPropertyPage()
  , m_bChanged(false)
{
}

CAutoConfigPropertyPage::CAutoConfigPropertyPage(UINT nID)
  : CPropertyPage(nID)
  , m_bChanged(false)
{
}

BOOL CAutoConfigPropertyPage::OnApply()
{
	// see comment in .h file
	BOOL bRet = WorkAroundCompilerBug_OnApply();
	if( bRet )
		SetModified(false);
	return bRet;
}

int CAutoConfigPropertyPage::MessageBox(LPCTSTR lpszMessage, UINT nType /* = MB_OK */)
{
	CAutoConfigSheet* pSheet = DYNAMIC_DOWNCAST(CAutoConfigSheet, GetParent());
	return CPropertyPage::MessageBox(lpszMessage,pSheet->m_psh.pszCaption, nType);
}
