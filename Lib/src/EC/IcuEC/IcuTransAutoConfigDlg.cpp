// IcuTransAutoConfigDlg.cpp : implementation file
//

#include "stdafx.h"
#include "IcuTransAutoConfigDlg.h"
#include "Resource.h"
#include "QueryConverterNameDlg.h"
#include "IcuTranslit.h"    // for IsRuleBased

// CIcuTransAutoConfigDlg dialog

extern LPCTSTR clpszIcuTransImplType;
extern LPCTSTR clpszIcuTransProgId;

#define TransliteratorTypeBuiltIn   0
#define TransliteratorTypeCustom    1

IMPLEMENT_DYNAMIC(CIcuTransAutoConfigDlg, CAutoConfigDlg)
CIcuTransAutoConfigDlg::CIcuTransAutoConfigDlg
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
		CIcuTransAutoConfigDlg::IDD,
		strFriendlyName,
		strConverterIdentifier,
		ConvType_Unicode_to_from_Unicode,   // these are all U t/f U
		strLhsEncodingId,
		strRhsEncodingId,
		lProcessTypeFlags,
		m_bIsInRepository
	)
{
	m_bQueryForConvType = false; // we know what ours is (i.e. Unicode_to_from_Unicode)

	// this radio button int is (re-)initialized during DoDataExchange now (if
	//  this is 'edit mode'
	m_nTransliteratorType = TransliteratorTypeBuiltIn;

	m_bComboBoxInitialized = false;
}

CString CIcuTransAutoConfigDlg::ImplType()
{
	return clpszIcuTransImplType;
}

CString CIcuTransAutoConfigDlg::ProgramID()
{
	return clpszIcuTransProgId;
}

CString DisplayNameFromInternalName(const CString& strInternalName)
{
	UnicodeString ustrDisplayName, ustrID;
	ustrID.setTo((LPCWSTR)strInternalName,strInternalName.GetLength());
	Transliterator::getDisplayName(ustrID, ustrDisplayName);
	return CString(ustrDisplayName.getTerminatedBuffer());
}

CString CIcuTransAutoConfigDlg::InternalNameFromDisplayName(const CString& strDisplayName)
{
	CString strInternalName;
	VERIFY(m_mapDisplayToInternalNames.Lookup(strDisplayName,strInternalName));
	return strInternalName;
}

CString CIcuTransAutoConfigDlg::DefaultFriendlyName()
{
	return DisplayNameFromInternalName(m_strConverterIdentifier);
}

void CIcuTransAutoConfigDlg::DoDataExchange(CDataExchange* pDX)
{
	CAutoConfigDlg::DoDataExchange(pDX);
	DDX_Control(pDX,IDC_COMBO_BUILT_IN_TRANS,m_cbBuiltInTransliterators);
	DDX_Control(pDX,IDC_ED_CUSTOM_TRANS,m_ctlCustomTransliterator);
	DDX_Control(pDX,IDC_CB_RECENTLY_USED,m_ctlRecentlyUsed);

	// during first pass, we have to use the presence of the ConverterID in the 'built-in' list
	//  to determine what the type is (i.e. in built or rule-based).
	if( !pDX->m_bSaveAndValidate && !m_bComboBoxInitialized )
	{
		CComPtr<IEncConverter> pEC;
		pEC.CoCreateInstance(clpszIcuTransProgId);
		if( !pEC )
		{
			MessageBox(_T("Unable to query for the list of available ICU transliterators! Reinstall required."));
			return;
		}

		SAFEARRAY* pSA = 0;
		HRESULT hr = pEC->get_ConverterNameEnum(&pSA);
		if( ProcessHResult(hr, pEC) )
		{
			CComSafeArray<BSTR> saConverterNames(pSA);
			for(UINT i = 0; i < saConverterNames.GetCount(); i++)
			{
				BSTR b = (BSTR)saConverterNames.GetAt(i);
				CString strInternalName = b;

				// according to the ICU webpage, we should be displaynig a 'display name' rather than the
				//  'internal name'.
				CString strDisplayName = DisplayNameFromInternalName(strInternalName);

				m_cbBuiltInTransliterators.AddString(strDisplayName);
				m_mapDisplayToInternalNames.SetAt(strDisplayName,strInternalName);
			}

			if( m_strConverterIdentifier.IsEmpty() )
			{
				// set select of the 'any to latin' transliterator (common one)
				CString strDisplayName = DisplayNameFromInternalName(_T("Any-Latin"));
				int nIndex = m_cbBuiltInTransliterators.FindStringExact(0,strDisplayName);
				if( nIndex == -1 )
					nIndex = 0;
				m_cbBuiltInTransliterators.SetCurSel(nIndex);
				m_cbBuiltInTransliterators.GetWindowText(strDisplayName);
				m_strConverterIdentifier = InternalNameFromDisplayName(strDisplayName);
			}
			else
			{
				// get the display name from the given internal name (i.e. m_strConverterIdentifier)
				CString strDisplayName = DisplayNameFromInternalName(m_strConverterIdentifier);
				if( m_cbBuiltInTransliterators.FindStringExact(0,strDisplayName) == -1 )
				{
					m_nTransliteratorType = TransliteratorTypeCustom;
				}
				else
				{
					m_nTransliteratorType = TransliteratorTypeBuiltIn;
					m_cbBuiltInTransliterators.SelectString(0,strDisplayName);
				}
			}
		}

		if( m_nTransliteratorType == TransliteratorTypeBuiltIn )
		{
			m_ctlCustomTransliterator.EnableWindow(false);
			m_ctlRecentlyUsed.EnableWindow(false);
			m_cbBuiltInTransliterators.SetFocus();
		}
		else
		{
			m_cbBuiltInTransliterators.EnableWindow(false);
			m_ctlCustomTransliterator.SetFocus();

			// populate the recently used combo box.
			EnumRecentlyUsed(m_ctlRecentlyUsed);
		}

		SetModified();
		m_bComboBoxInitialized = true;
	}

	DDX_Radio(pDX,IDC_RB_BUILT_IN_TRANS,m_nTransliteratorType);

	if( m_nTransliteratorType == TransliteratorTypeBuiltIn )
	{
		CString strDisplayName;
		if( pDX->m_bSaveAndValidate )
		{
			DDX_CBString(pDX,IDC_COMBO_BUILT_IN_TRANS,strDisplayName);
			m_strConverterIdentifier = InternalNameFromDisplayName(strDisplayName);
		}
		else
		{
			strDisplayName = DisplayNameFromInternalName(m_strConverterIdentifier);
			DDX_CBString(pDX,IDC_COMBO_BUILT_IN_TRANS,strDisplayName);
		}
	}
	else if( m_nTransliteratorType == TransliteratorTypeCustom )
	{
		DDX_Text(pDX,IDC_ED_CUSTOM_TRANS,m_strConverterIdentifier);
	}
}

BEGIN_MESSAGE_MAP(CIcuTransAutoConfigDlg, CAutoConfigDlg)
	ON_BN_CLICKED(IDC_RB_BUILT_IN_TRANS, OnBnClickedRbBuiltInTrans)
	ON_BN_CLICKED(IDC_RB_CUSTOM_TRANS, OnBnClickedRbCustomTrans)
	ON_CBN_SELCHANGE(IDC_CB_RECENTLY_USED, OnCbnSelchangeCbRecentlyUsed)
	ON_EN_CHANGE(IDC_ED_CUSTOM_TRANS, OnEnChangeEdCustomTrans)
	ON_BN_CLICKED(IDC_BUTTON_DEL_CUR, OnBnClickedButtonDelCur)
	ON_CBN_SELCHANGE(IDC_COMBO_BUILT_IN_TRANS, OnCbnSelchangeBuiltInTrans)
END_MESSAGE_MAP()

void CIcuTransAutoConfigDlg::OnCbnSelchangeBuiltInTrans()
{
	// When a converter is chosen, then if it's unidirectional, disable the 'Reverse' checkbox
	if( !UpdateData() )
		return;
	SetModified();
}

void CIcuTransAutoConfigDlg::OnBnClickedRbBuiltInTrans()
{
	m_strConverterIdentifier.Empty();
	m_nTransliteratorType = TransliteratorTypeBuiltIn;
	UpdateData(false);
	SetModified();
	m_cbBuiltInTransliterators.EnableWindow(true);
	m_ctlCustomTransliterator.EnableWindow(false);
	m_ctlRecentlyUsed.Clear();
	m_ctlRecentlyUsed.EnableWindow(false);
}

void CIcuTransAutoConfigDlg::OnBnClickedRbCustomTrans()
{
	m_strConverterIdentifier.Empty();
	m_nTransliteratorType = TransliteratorTypeCustom;
	UpdateData(false);
	SetModified();
	m_cbBuiltInTransliterators.EnableWindow(false);
	m_ctlCustomTransliterator.EnableWindow(true);
	m_ctlRecentlyUsed.EnableWindow(true);

	// populate the recently used combo box.
	EnumRecentlyUsed(m_ctlRecentlyUsed);
}

BOOL CIcuTransAutoConfigDlg::OnApply()
{
	BOOL bRet = CAutoConfigDlg::OnApply();

	// if doing 'custom', then add it to the recently used list
	if( bRet && (m_nTransliteratorType == TransliteratorTypeCustom) )
	{
		AddToRecentlyUsed(m_strConverterIdentifier);
		EnumRecentlyUsed(m_ctlRecentlyUsed);
	}

	return bRet;
}

void CIcuTransAutoConfigDlg::OnCbnSelchangeCbRecentlyUsed()
{
	// if the user selects one from the recently used combo box, then fill in the Find and Replace edit controls
	int nIndex = m_ctlRecentlyUsed.GetCurSel();
	if( nIndex < 0 )
		return;

	m_ctlRecentlyUsed.GetLBText(nIndex,m_strConverterIdentifier);

	UpdateData(false);
}

void CIcuTransAutoConfigDlg::OnBnClickedButtonDelCur()
{
	// if the user selects one from the recently used combo box, then fill in the Find and Replace edit controls
	int nIndex = m_ctlRecentlyUsed.GetCurSel();
	if( nIndex < 0 )
		return;

	CString strDisplayName;
	m_ctlRecentlyUsed.GetLBText(nIndex,strDisplayName);

	CString strDeleteQuery;
	strDeleteQuery.Format(_T("Are you sure you want to delete the following transliterator from the recently used list?\n\n%s"), strDisplayName );
	if( MessageBox(strDeleteQuery, MB_YESNOCANCEL) == IDYES )
	{
		RemFromRecentlyUsed(strDisplayName);
		m_ctlRecentlyUsed.DeleteString(nIndex);
		m_ctlRecentlyUsed.SetCurSel(--nIndex);
	}
}

void CIcuTransAutoConfigDlg::OnEnChangeEdCustomTrans()
{
	SetModified();
}
