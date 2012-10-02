#pragma once

#include "afxcmn.h"
#include "ListBoxEx.h"
#include "SHBrowseDlg.h"

class CMyListBoxEx: public CListBoxEx
{
public:

	CMyListBoxEx() {};

	virtual void OnBrowseButton( int iItem )
	{
		iItem;
		CSHBrowseDlg dlgBrowse;
		if ( dlgBrowse.DoModal() )
			SetEditText( dlgBrowse.GetFullPath() );
	};
};

// CBrowseForStrings dialog
class CBrowseForStrings : public CDialog
{
	DECLARE_DYNAMIC(CBrowseForStrings)

public:
	CBrowseForStrings(LPCTSTR lpszCaption, CStringArray& astrStrings, BOOL bBrowseButton, CWnd* pParent = NULL);   // standard constructor
	virtual ~CBrowseForStrings();

// Dialog Data
	enum { IDD = IDD_DLG_BROWSE_DISTROS };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	virtual BOOL OnInitDialog();
	afx_msg void OnEditbutton();

	DECLARE_MESSAGE_MAP()
public:
	CString         m_strCaption;
	CStringArray&   m_astrStrings;
	CMyListBoxEx    m_ctlEditList;
	CListBoxExBuddy m_ListBoxExBuddy;
	BOOL            m_bBrowseButton;
};
