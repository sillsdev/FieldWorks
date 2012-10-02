#pragma once
#include "AutoConfigDlg.h"
#include "Resource.h"

// CIcuConvAutoConfigDlg dialog

class CIcuConvAutoConfigDlg : public CAutoConfigDlg
{
	DECLARE_DYNAMIC(CIcuConvAutoConfigDlg)

public:
	CIcuConvAutoConfigDlg
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
	virtual ~CIcuConvAutoConfigDlg()
	{
	};

// Dialog Data
	enum { IDD = IDD_DLG_CONV_AUTO_CONFIG };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	afx_msg void OnCbnSelchangeCbExistingConverter();

	DECLARE_MESSAGE_MAP()
public:
	virtual CString DefaultFriendlyName()   { return m_strConverterIdentifier; };
	virtual CString ImplType();
	virtual CString ProgramID();

protected:
	BOOL        m_bComboBoxInitialized;
	CComboBox   m_cbBuiltInConverters;
};
