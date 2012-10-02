// IcuTrans.cpp : Implementation of CIcuTransEncConverterConfig

#include "stdafx.h"
#include "IcuTransEncConverterConfig.h"

/////////////////////////////////////////////////////////////////////////////
// CIcuTransEncConverterConfig
#include "IcuTransAutoConfigDlg.h"

STDMETHODIMP CIcuTransEncConverterConfig::Configure
(
	IEncConverters* pECs,
	BSTR            strFriendlyName,
	ConvType        eConversionType,
	BSTR            strLhsEncodingID,
	BSTR            strRhsEncodingID,
	VARIANT_BOOL*   bRet
)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	*bRet = 0;   // false (pessimistic)

	// create *our* config (i.e. 'Setup') dialog, which get's passed to the common PropertySheet class.
	CIcuTransAutoConfigDlg dlgConfig
		(
			pECs,
			strFriendlyName,
			m_strConverterID,   // may be available if editing an existing configurator
			eConversionType,
			strLhsEncodingID,
			strRhsEncodingID,
			m_lProcessType,
			m_bIsInRepository
		);

	// call base class implementation to do all the work (since it's the same for everyone)
	if( CEncConverterConfig::Configure(&dlgConfig) )
		*bRet = -1; // TRUE

	return S_OK;
}

STDMETHODIMP CIcuTransEncConverterConfig::DisplayTestPage
(
	IEncConverters*     pECs,
	BSTR                strFriendlyName,
	BSTR                strConverterIdentifier,
	ConvType            eConversionType,
	BSTR                strTestData
)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());

	// most of the input parameters are optional, so initialize from This if so.
	InitializeFromThis(&strFriendlyName,&strConverterIdentifier,eConversionType,&strTestData);

	// create *our* config (i.e. 'Setup') dialog, which get's passed to the common PropertySheet class.
	CIcuTransAutoConfigDlg dlgConfig
		(
			pECs,
			strFriendlyName,
			strConverterIdentifier,
			eConversionType
		);

	// call base class implementation to do all the work (since it's the same for everyone)
	CEncConverterConfig::DisplayTestPageEx(&dlgConfig, strTestData);

	return S_OK;
}
