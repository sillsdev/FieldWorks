// IcuConvAutoConfigDlg.cpp : implementation file
//

#include "stdafx.h"
#include "IcuConvAutoConfigDlg.h"
#include "Resource.h"
#include "QueryConverterNameDlg.h"

// CIcuConvAutoConfigDlg dialog

extern LPCTSTR clpszIcuConvImplType;
extern LPCTSTR clpszIcuConvProgId;

IMPLEMENT_DYNAMIC(CIcuConvAutoConfigDlg, CAutoConfigDlg)
CIcuConvAutoConfigDlg::CIcuConvAutoConfigDlg
(
	IEncConverters* pECs,
	const CString&  strFriendlyName,
	const CString&  strConverterIdentifier,
	ConvType        eConversionType,
	const CString&  strLhsEncodingId,
	const CString&  strRhsEncodingId,
	long            lProcessTypeFlags,
	BOOL            m_bIsInRepository
)
  : CAutoConfigDlg
	(
		pECs,
		CIcuConvAutoConfigDlg::IDD,
		strFriendlyName,
		strConverterIdentifier,
		// ICU converters are generally Legacy_to_from_Unicode (with the exception of the UTFX<>UTFY flavors)
		// but we don't need to be more specific here, because the converter itself picks this up.
		(eConversionType != ConvType_Unknown) ? eConversionType : ConvType_Legacy_to_from_Unicode,
		strLhsEncodingId,
		strRhsEncodingId,
		lProcessTypeFlags,
		m_bIsInRepository
	)
  , m_bComboBoxInitialized(false)
{
	m_bQueryForConvType = false; // we know what ours our:
}

CString CIcuConvAutoConfigDlg::ImplType()
{
	return clpszIcuConvImplType;
}

CString CIcuConvAutoConfigDlg::ProgramID()
{
	return clpszIcuConvProgId;
}

const CComBSTR strTempName = _T("CIcuConvAutoConfigDlg");

void CIcuConvAutoConfigDlg::DoDataExchange(CDataExchange* pDX)
{
	CAutoConfigDlg::DoDataExchange(pDX);
	DDX_Control(pDX,IDC_COMBO_BUILT_IN_CONV,m_cbBuiltInConverters);

	if( pDX->m_bSaveAndValidate )
	{
		DDX_CBString(pDX,IDC_COMBO_BUILT_IN_CONV,m_strConverterIdentifier);

		// the list the user is choosing from contains all the aliases as well, so just clip
		//  the string at the first "(". If there isn't one, then grab the whole thing.
		int nIndexLeftParan = m_strConverterIdentifier.Find('(');
		if( nIndexLeftParan > 0 )
			m_strConverterIdentifier = m_strConverterIdentifier.Left(--nIndexLeftParan);
	}
	else if( !m_bComboBoxInitialized )
	{
		CComPtr<IEncConverter> pEC;
		pEC.CoCreateInstance(clpszIcuConvProgId);
		if( !pEC )
		{
			MessageBox(_T("Unable to query for the list of ICU converters! Reinstall required."));
			return;
		}

		SAFEARRAY* pSA = 0;
		HRESULT hr = pEC->get_ConverterNameEnum(&pSA);
		if( ProcessHResult(hr, pEC) )
		{
			CComSafeArray<BSTR> saConverterNames(pSA);
			CString str;
			for(UINT i = 0; i < saConverterNames.GetCount(); i++)
			{
				BSTR b = (BSTR)saConverterNames.GetAt(i);
				str = b;
				m_cbBuiltInConverters.AddString(str);
			}

			if( m_strConverterIdentifier.IsEmpty() )
			{
				int nIndexRtParan = str.Find('(');
				if( nIndexRtParan > 0 )
					m_strConverterIdentifier = str.Left(--nIndexRtParan);
			}
			else
			{
				int nIndex = m_cbBuiltInConverters.SelectString(0,m_strConverterIdentifier);
			}
		}

		m_bComboBoxInitialized = true;
	}
}

BEGIN_MESSAGE_MAP(CIcuConvAutoConfigDlg, CAutoConfigDlg)
	ON_CBN_SELCHANGE(IDC_COMBO_BUILT_IN_CONV, OnCbnSelchangeCbExistingConverter)
END_MESSAGE_MAP()

void CIcuConvAutoConfigDlg::OnCbnSelchangeCbExistingConverter()
{
	// When a converter is chosen, then if it's unidirectional, disable the 'Reverse' checkbox
	if( !UpdateData() )
		return;
	SetModified();
}
