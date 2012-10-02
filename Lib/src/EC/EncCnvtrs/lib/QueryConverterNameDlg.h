#pragma once

#include "ECResource.h"

// CQueryConverterNameDlg dialog

class CQueryConverterNameDlg : public CDialog
{
	DECLARE_DYNAMIC(CQueryConverterNameDlg)

public:
	CQueryConverterNameDlg
		(
		PtrIEncConverters pECs,
		CWnd* pParent = NULL,
		const CString& strFriendlyName = _T(""),
		const CString& strLhsEncodingID = _T(""),
		const CString& strRhsEncodingID = _T(""),
		long  eProcessTypeFlags = ProcessTypeFlags_DontKnow
		);

	virtual ~CQueryConverterNameDlg()
		{
		};

	CString     FriendlyName;
	CString     LhsEncodingID;
	CString     RhsEncodingID;
	long        ProcessType;

// Dialog Data
	enum { IDD = IDD_DLG_CONVERTER_NAME };

protected:
	PtrIEncConverters m_pECs;

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	DECLARE_MESSAGE_MAP()
public:
	afx_msg void OnBnClickedBtnAdvanced();
};
