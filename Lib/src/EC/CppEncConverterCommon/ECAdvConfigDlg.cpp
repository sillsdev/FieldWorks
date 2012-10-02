// ECAdvConfigDlg.cpp : implementation file
//

#include "stdafx.h"
#include "ECAdvConfigDlg.h"

// CECAdvConfigDlg dialog

IMPLEMENT_DYNAMIC(CECAdvConfigDlg, CDialog)
CECAdvConfigDlg::CECAdvConfigDlg
(
	PtrIEncConverters   pECs,
	CWnd*               pParent,
	const CString&      strLhsEncodingID,
	const CString&      strRhsEncodingID,
	long                lProcessType
)
	: CDialog(CECAdvConfigDlg::IDD, pParent)
	, m_pECs(pECs)
	, LhsEncodingID(strLhsEncodingID)
	, RhsEncodingID(strRhsEncodingID)
	, ProcessType(lProcessType)
	, m_bComboBoxInitialized(false)
{
}

void CECAdvConfigDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_CB_LHS_ENCODING_ID, m_ctlCbLhsEncodingID);
	DDX_Control(pDX, IDC_CB_RHS_ENCODING_ID, m_ctlCbRhsEncodingID);
	DDX_CBString(pDX, IDC_CB_LHS_ENCODING_ID, LhsEncodingID);
	DDX_CBString(pDX, IDC_CB_RHS_ENCODING_ID, RhsEncodingID);

	if( !pDX->m_bSaveAndValidate && !m_bComboBoxInitialized )
	{
		SAFEARRAY* pSA = 0;
		m_pECs->get_Encodings(&pSA);
		if( pSA != 0 )
		{
			CComSafeArray<BSTR> saEncodingNames(pSA);
			int nCount = (int)saEncodingNames.GetCount();
			if( nCount > 0 )
			{
				for(int i = 0; i < nCount; i++)
				{
					BSTR b = (BSTR)saEncodingNames.GetAt(i);
					m_ctlCbLhsEncodingID.AddString(b);
					m_ctlCbRhsEncodingID.AddString(b);
				}
			}
		}

		// in any case, have at least add "UNICODE" (so they get the right 'all-uppercase' for it).
		if( m_ctlCbLhsEncodingID.FindStringExact(0,_T("UNICODE")) == LB_ERR )
			m_ctlCbLhsEncodingID.AddString(_T("UNICODE"));

		if( m_ctlCbRhsEncodingID.FindStringExact(0,_T("UNICODE")) == LB_ERR )
			m_ctlCbRhsEncodingID.AddString(_T("UNICODE"));

		// in either case, select the one that came in (if one came in)
		// if it isn't there, then add it as well
		AddAndOrSelect(m_ctlCbLhsEncodingID,LhsEncodingID);
		AddAndOrSelect(m_ctlCbRhsEncodingID,RhsEncodingID);

		// set the flags of the process type fields also
		SetProcessTypeBitField(IDC_CHK_PT_UNI_ENC_CONV,ProcessTypeFlags_UnicodeEncodingConversion);
		SetProcessTypeBitField(IDC_CHK_PT_TRANSLITERATION,ProcessTypeFlags_Transliteration);
		SetProcessTypeBitField(IDC_CHK_PT_ICU_TRANSLITERATION2,ProcessTypeFlags_ICUTransliteration);
		SetProcessTypeBitField(IDC_CHK_PT_ICU_CONVERTER,ProcessTypeFlags_ICUConverter);
		SetProcessTypeBitField(IDC_CHK_PT_ICU_REGEX,ProcessTypeFlags_ICURegularExpression);
		SetProcessTypeBitField(IDC_CHK_PT_CODE_PAGE,ProcessTypeFlags_CodePageConversion);
		SetProcessTypeBitField(IDC_CHK_PT_NON_UNI_ENC_CONV,ProcessTypeFlags_NonUnicodeEncodingConversion);
		SetProcessTypeBitField(IDC_CHK_PT_SPELLFIXER,ProcessTypeFlags_SpellingFixerProject);
		SetProcessTypeBitField(IDC_CHK_PT_PYTHON_SCRIPT,ProcessTypeFlags_PythonScript);
		SetProcessTypeBitField(IDC_CHK_PT_PERL_EXPRESSION,ProcessTypeFlags_PerlExpression);
		SetProcessTypeBitField(IDC_CHK_PT_SPARE_1,ProcessTypeFlags_UserDefinedSpare1);
		SetProcessTypeBitField(IDC_CHK_PT_SPARE_2,ProcessTypeFlags_UserDefinedSpare2);

		m_bComboBoxInitialized = true;
	}
	else if( pDX->m_bSaveAndValidate )
	{
		GetProcessTypeBitField(IDC_CHK_PT_UNI_ENC_CONV,ProcessTypeFlags_UnicodeEncodingConversion);
		GetProcessTypeBitField(IDC_CHK_PT_TRANSLITERATION,ProcessTypeFlags_Transliteration);
		GetProcessTypeBitField(IDC_CHK_PT_ICU_TRANSLITERATION2,ProcessTypeFlags_ICUTransliteration);
		GetProcessTypeBitField(IDC_CHK_PT_ICU_CONVERTER,ProcessTypeFlags_ICUConverter);
		GetProcessTypeBitField(IDC_CHK_PT_ICU_REGEX,ProcessTypeFlags_ICURegularExpression);
		GetProcessTypeBitField(IDC_CHK_PT_CODE_PAGE,ProcessTypeFlags_CodePageConversion);
		GetProcessTypeBitField(IDC_CHK_PT_NON_UNI_ENC_CONV,ProcessTypeFlags_NonUnicodeEncodingConversion);
		GetProcessTypeBitField(IDC_CHK_PT_SPELLFIXER,ProcessTypeFlags_SpellingFixerProject);
		GetProcessTypeBitField(IDC_CHK_PT_PYTHON_SCRIPT,ProcessTypeFlags_PythonScript);
		GetProcessTypeBitField(IDC_CHK_PT_PERL_EXPRESSION,ProcessTypeFlags_PerlExpression);
		GetProcessTypeBitField(IDC_CHK_PT_SPARE_1,ProcessTypeFlags_UserDefinedSpare1);
		GetProcessTypeBitField(IDC_CHK_PT_SPARE_2,ProcessTypeFlags_UserDefinedSpare2);
	}
}

void CECAdvConfigDlg::AddAndOrSelect(CComboBox& ctrl, const CString& str)
{
	if( !str.IsEmpty() )
	{
		int nIndex = ctrl.FindStringExact(0,str);
		if( nIndex < 0 )
			nIndex = ctrl.AddString(str);
		ctrl.SetCurSel(nIndex);
	}
}

void CECAdvConfigDlg::SetProcessTypeBitField(UINT nID, ProcessTypeFlags nBitMask)
{
	CButton* pButton = (CButton*)GetDlgItem(nID);
	ASSERT(pButton != 0);
	pButton->SetCheck((ProcessType & nBitMask) ? BST_CHECKED : BST_UNCHECKED);
}

void CECAdvConfigDlg::GetProcessTypeBitField(UINT nID, ProcessTypeFlags nBitMask)
{
	CButton* pButton = (CButton*)GetDlgItem(nID);
	ASSERT(pButton != 0);
	if( pButton->GetCheck() == BST_CHECKED )
		ProcessType |= nBitMask;    // set it or...
	else
		ProcessType &= ~nBitMask;   // clear it (in case it was previously set)
}

BEGIN_MESSAGE_MAP(CECAdvConfigDlg, CDialog)
END_MESSAGE_MAP()


// CECAdvConfigDlg message handlers
