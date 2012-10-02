// QueryConverterNameDlg.cpp : implementation file
//

#include "stdafx.h"
#include "QueryConverterNameDlg.h"
#include "ECAdvConfigDlg.h"

// CQueryConverterNameDlg dialog

IMPLEMENT_DYNAMIC(CQueryConverterNameDlg, CDialog)
CQueryConverterNameDlg::CQueryConverterNameDlg
(
	PtrIEncConverters pECs,
	CWnd* pParent /*=NULL*/,
	const CString& strFriendlyName /* = _T("") */,
	const CString& strLhsEncodingID /* = _T("") */,
	const CString& strRhsEncodingID /* = _T("") */,
	long eProcessTypeFlags /* = ProcessTypeFlags_DontKnow */
)
	: CDialog(CQueryConverterNameDlg::IDD, pParent)
	, m_pECs(pECs)
	, FriendlyName(strFriendlyName)
	, LhsEncodingID(strLhsEncodingID)
	, RhsEncodingID(strRhsEncodingID)
	, ProcessType(eProcessTypeFlags)
{
}

#define strCaption _T("EncConverter Configurator")

void CQueryConverterNameDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Text(pDX,IDC_ED_CONVERTER_NAME,FriendlyName);

	if( pDX->m_bSaveAndValidate )
	{
		if( FriendlyName.IsEmpty() )
		{
			MessageBox(_T("Converter name cannot be empty"), strCaption);
			pDX->m_idLastControl = IDC_ED_CONVERTER_NAME;
			pDX->Fail();
		}
	}
}

BEGIN_MESSAGE_MAP(CQueryConverterNameDlg, CDialog)
	ON_BN_CLICKED(IDC_BTN_ADVANCED, OnBnClickedBtnAdvanced)
END_MESSAGE_MAP()

// CQueryConverterNameDlg message handlers

void CQueryConverterNameDlg::OnBnClickedBtnAdvanced()
{
	CECAdvConfigDlg dlg(m_pECs, this, LhsEncodingID, RhsEncodingID, ProcessType);
	if( dlg.DoModal() == IDOK )
	{
		LhsEncodingID = dlg.LhsEncodingID;
		RhsEncodingID = dlg.RhsEncodingID;
		ProcessType = dlg.ProcessType;
	}
}
