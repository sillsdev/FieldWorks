#pragma once
#include "AutoConfigDlg.h"
#include "Resource.h"

// CIcuRegexAutoConfigDlg dialog

class CIcuRegexAutoConfigDlg : public CAutoConfigDlg
{
	DECLARE_DYNAMIC(CIcuRegexAutoConfigDlg)

public:
	CIcuRegexAutoConfigDlg
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
	virtual ~CIcuRegexAutoConfigDlg()
	{
	};

	CString ConstructConverterSpec
	(
		const CString&  strFind,
		const CString&  strReplace,
		LPCTSTR         szFlags = 0
	);

// Dialog Data
	enum { IDD = IDD_DLG_REGEX_AUTO_CONFIG };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	virtual BOOL OnApply();

	DECLARE_MESSAGE_MAP()
public:
	virtual CString DefaultFriendlyName();
	virtual CString ImplType();
	virtual CString ProgramID();

protected:
	CEdit       m_ctlFind;
	CString     m_strFind;
	CString     m_strReplace;
	BOOL        m_bIgnoreCase;
	CComboBox   m_ctlRecentlyUsed;
	BOOL        m_bComboBoxInitialized;

public:
	afx_msg void OnEnChange();
	afx_msg void OnCbnSelchangeCbRecentlyUsed();
	afx_msg void OnMenuItems(UINT nID);
	afx_msg void OnBnClickedBtnPopupHelp();
	afx_msg void OnBnClickedButtonDelCur();
};
