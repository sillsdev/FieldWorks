// BrowseForDistros.cpp : implementation file
//

#include "stdafx.h"
#include "BrowseForStrings.h"
#include "Resource.h"

// CBrowseForStrings dialog

IMPLEMENT_DYNAMIC(CBrowseForStrings, CDialog)
CBrowseForStrings::CBrowseForStrings(LPCTSTR lpszCaption, CStringArray& astrStrings, BOOL bBrowseButton, CWnd* pParent /*=NULL*/)
  : CDialog(CBrowseForStrings::IDD, pParent)
  , m_strCaption(lpszCaption)
  , m_astrStrings(astrStrings)
  , m_bBrowseButton(bBrowseButton)
{
}

CBrowseForStrings::~CBrowseForStrings()
{
}

void CBrowseForStrings::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_LISTBOX, m_ctlEditList);

	if( pDX->m_bSaveAndValidate )
	{
		CString str;
		int nCount = m_ctlEditList.GetCount();
		m_astrStrings.RemoveAll();
		for(int i = 0; i < nCount; i++)
		{
			m_ctlEditList.GetText(i,str);
			m_astrStrings.Add(str);
		}
	}
}

BEGIN_MESSAGE_MAP(CBrowseForStrings, CDialog)
	ON_BN_CLICKED(IDC_EDITBUTTON, OnEditbutton)
END_MESSAGE_MAP()

BOOL CBrowseForStrings::OnInitDialog()
{
	BOOL bRet = CDialog::OnInitDialog();

	SetWindowText(m_strCaption);

	if( !m_bBrowseButton )    // the browse button is *on* by default, so turn it *off* if not wanted
		m_ctlEditList.SetEditStyle( 0, false );

	for(int i = 0; i < m_astrStrings.GetCount(); i++)
	{
		m_ctlEditList.InsertString(-1,m_astrStrings[i]);
	}

	// Add the listbox buddy
	m_ListBoxExBuddy.SubclassDlgItem( IDC_LISTBUDDY, this );
	m_ListBoxExBuddy.SetListbox( &m_ctlEditList );

	return bRet;
}

// CBrowseForStrings message handlers
void CBrowseForStrings::OnEditbutton()
{
   m_ctlEditList.EditNew();
}
