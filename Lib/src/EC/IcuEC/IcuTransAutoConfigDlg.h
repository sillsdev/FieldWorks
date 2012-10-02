#pragma once
#include "AutoConfigDlg.h"
#include "Resource.h"
#include "afxtempl.h"

// CIcuTransAutoConfigDlg dialog

class CIcuTransAutoConfigDlg : public CAutoConfigDlg
{
	DECLARE_DYNAMIC(CIcuTransAutoConfigDlg)

public:
	CIcuTransAutoConfigDlg
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
	virtual ~CIcuTransAutoConfigDlg()
	{
	};

// Dialog Data
	enum { IDD = IDD_DLG_TRANS_AUTO_CONFIG };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	virtual BOOL OnApply();

	DECLARE_MESSAGE_MAP()
public:
	afx_msg void OnCbnSelchangeBuiltInTrans();
	afx_msg void OnCbnSelchangeCbFuncName();
	virtual CString DefaultFriendlyName();
	virtual CString ImplType();
	virtual CString ProgramID();
	CString InternalNameFromDisplayName(const CString& strDisplayName);

protected:
	CMap<CString,LPCTSTR,CString,CString&>  m_mapDisplayToInternalNames;
	CComboBox   m_cbBuiltInTransliterators;
	CEdit       m_ctlCustomTransliterator;
	CComboBox   m_ctlRecentlyUsed;

	int         m_nTransliteratorType;
	BOOL        m_bComboBoxInitialized;

public:
	afx_msg void OnBnClickedRbBuiltInTrans();
	afx_msg void OnBnClickedRbCustomTrans();
	afx_msg void OnCbnSelchangeCbRecentlyUsed();
	afx_msg void OnEnChangeEdCustomTrans();
	afx_msg void OnBnClickedButtonDelCur();
};
