// AutoConfigSheet.cpp : implementation file
//

#include "stdafx.h"
#include "AutoConfigSheet.h"


// CAutoConfigSheet

IMPLEMENT_DYNAMIC(CAutoConfigSheet, CPropertySheet)

CAutoConfigSheet::CAutoConfigSheet
(
	CAutoConfigDlg* pPgConfig,
	const CString&  strCaption,
	const CString&  strHtmlFilename,
	const CString&  strProgramID
)
  : CPropertySheet(strCaption, 0, 0)
  , m_pPgConfig(pPgConfig)
  , m_pgAbout(strHtmlFilename)
  , m_pgTest()
{
	// the about box has an OLE control (CWebBrowser to display the HTML), so we need to enable the control container
	AfxEnableControlContainer();

	// all config sheets have three pages: an About HTML display, a configuration dialog
	//  (which is passed in--different for each sub-class) and a test page (which is the
	//  same for all.
	AddPage(&m_pgAbout);
	AddPage(m_pPgConfig);
	AddPage(&m_pgTest);
}

CAutoConfigSheet::CAutoConfigSheet
(
	CAutoConfigDlg* pPgConfig,
	const CString&  strCaption,
	const CString&  strTestData
)
  : CPropertySheet(strCaption, 0, 0)
  , m_pPgConfig(pPgConfig)
  , m_pgTest(strTestData)
{
	// special constructor for just testing
	// We need the config page (since it knows how to create its own
	//  kind of IEncConverter instance), but we don't show it.
	AddPage(&m_pgTest);
}

BEGIN_MESSAGE_MAP(CAutoConfigSheet, CPropertySheet)
	ON_WM_QUERYDRAGICON()
END_MESSAGE_MAP()

// The system calls this to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CAutoConfigSheet::OnQueryDragIcon()
{
	return (HCURSOR) m_hIcon;
}

// CAutoConfigSheet message handlers
BOOL CAutoConfigSheet::OnInitDialog()
{
	BOOL b = CPropertySheet::OnInitDialog();

	// initialize a nicer icon
	m_hIcon = AfxGetApp()->LoadIcon(IDI_ICON);
	if( m_hIcon != 0 )
	{
		this->SetIcon(m_hIcon,true);
		this->SetIcon(m_hIcon,false);
	}

	// if the converter id is already configured (i.e. we're in 'edit' mode), then
	//  activate the Setup tab directly.
	if( !m_pPgConfig->m_strConverterIdentifier.IsEmpty() )
		SetActivePage(m_pPgConfig);

	return b;
}

BOOL CAutoConfigSheet::PreTranslateMessage(MSG* pMsg)
{
	// first give the active propertypage a chance to pretranslate
	if( ((CDialog*)GetActivePage( ))->PreTranslateMessage(pMsg) )
		return  true;

	return CPropertySheet::PreTranslateMessage(pMsg);
}
