#pragma once
#include "AutoConfigDlg.h"
#include "afxcmn.h" // for CRichEditCtrl

// CPerlExpressionAutoConfigDlg dialog

class CPerlExpressionAutoConfigDlg : public CAutoConfigDlg
{
	DECLARE_DYNAMIC(CPerlExpressionAutoConfigDlg)

public:
	CPerlExpressionAutoConfigDlg
	(
		IEncConverters* pECs,
		const CString&  strFriendlyName,
		const CString&  strConverterIdentifier,
		ConvType        eConversionType,
		const CString&  strLhsEncodingId = strEmpty,
		const CString&  strRhsEncodingId = strEmpty,
		long            lProcessTypeFlags = 0,
		BOOL            m_bIsInRepository = false   // don't care if not specified
	);
	virtual ~CPerlExpressionAutoConfigDlg() {};

// Dialog Data
	enum { IDD = IDD_DLG_AUTO_CONFIG_PERL_EXPR };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	virtual BOOL OnInitDialog();
	virtual BOOL OnApply();
	virtual BOOL PreTranslateMessage(MSG* pMsg);

	DECLARE_MESSAGE_MAP()
	afx_msg void OnSelChange(NMHDR* pNMHDR, LRESULT* pResult);
	afx_msg void OnCbnSelchangeCbRecentlyUsed();
	afx_msg void OnBnClickedButtonDelCur();
	afx_msg void OnBnDistroConfig();

public:
	virtual CString DefaultFriendlyName();
	virtual CString ImplType();
	virtual CString ProgramID();

protected:
	CRichEditCtrl   m_ctlExpression;
	CComboBox       m_ctlRecentlyUsed;
};
