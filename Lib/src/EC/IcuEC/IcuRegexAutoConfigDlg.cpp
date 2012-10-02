// IcuRegexAutoConfigDlg.cpp : implementation file
//

#include "stdafx.h"
#include "IcuRegexAutoConfigDlg.h"
#include "Resource.h"
#include "QueryConverterNameDlg.h"
#include "IcuRegexEncConverter.h"

// CIcuRegexAutoConfigDlg dialog

extern LPCTSTR clpszIcuRegexImplType;
extern LPCTSTR clpszIcuRegexProgId;

IMPLEMENT_DYNAMIC(CIcuRegexAutoConfigDlg, CAutoConfigDlg)
CIcuRegexAutoConfigDlg::CIcuRegexAutoConfigDlg
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
		CIcuRegexAutoConfigDlg::IDD,
		strFriendlyName,
		strConverterIdentifier,
		ConvType_Unicode_to_Unicode,   // these are all U > U
		strLhsEncodingId,
		strRhsEncodingId,
		lProcessTypeFlags,
		m_bIsInRepository
	)
  , m_bComboBoxInitialized(false)
{
	m_bQueryForConvType = false; // we know what ours is (i.e. Unicode_to_Unicode)
	m_bIgnoreCase = false;

	// if we're in 'edit mode'...
	if( !strConverterIdentifier.IsEmpty() )
	{
		// split apart the converter identifier into Find, Replace, and case flag
		DeconstructConverterSpec(strConverterIdentifier,m_strFind,m_strReplace,m_bIgnoreCase);
	}
}

CString CIcuRegexAutoConfigDlg::ImplType()
{
	return clpszIcuRegexImplType;
}

CString CIcuRegexAutoConfigDlg::ProgramID()
{
	return clpszIcuRegexProgId;
}

CString CIcuRegexAutoConfigDlg::ConstructConverterSpec
(
	const CString&  strFind,
	const CString&  strReplace,
	LPCTSTR         szFlags /* = 0 */
)
{
	CString str = strFind + clpszFindReplaceDelimiter;
	str += strReplace;
	if( szFlags != 0 )
		str += szFlags;
	return str;
}

CString CIcuRegexAutoConfigDlg::DefaultFriendlyName()
{
	return ConstructConverterSpec(m_strFind, m_strReplace);
}

void CIcuRegexAutoConfigDlg::DoDataExchange(CDataExchange* pDX)
{
	CAutoConfigDlg::DoDataExchange(pDX);
	DDX_Control(pDX,IDC_ED_FIND,m_ctlFind);
	DDX_Text(pDX,IDC_ED_FIND,m_strFind);
	DDX_Text(pDX,IDC_ED_REPLACE,m_strReplace);
	DDX_Check(pDX,IDC_CHK_IGNORE_CASE,m_bIgnoreCase);
	DDX_Control(pDX,IDC_CB_RECENTLY_USED,m_ctlRecentlyUsed);

	if( pDX->m_bSaveAndValidate )
	{
		// for the find, there are two requirements: 1) it can't be empty, and 2) it can't have the
		//  delimiter in it (or I don't know how to unpack it)
		if( m_strFind.IsEmpty() )
		{
			MessageBox(_T("Enter a regular expression to search for (can't be empty)!"));
			pDX->m_idLastControl = IDC_ED_FIND;
			pDX->Fail();
		}
		else if( m_strFind.Find(clpszFindReplaceDelimiter) != -1 )
		{
			MessageBox(_T("The 'Search For' string can't contain the delimiter, '->'. Use the sequence '\\u002d\\u003e' instead for the same result."));
			pDX->m_idLastControl = IDC_ED_FIND;
			pDX->Fail();
		}
		// oops, replacement *can* be empty, no?
		/*
		else if( m_strReplace.IsEmpty() )
		{
			MessageBox(_T("Enter the replacement string!"));
			pDX->m_idLastControl = IDC_ED_REPLACE;
			pDX->Fail();
		}
		*/
		else
		{
			m_strConverterIdentifier = ConstructConverterSpec(m_strFind, m_strReplace, (m_bIgnoreCase) ? clpszCaseInsensitiveFlag : 0);
		}
	}
	else if( !m_bComboBoxInitialized )
	{
		m_bComboBoxInitialized = true;
		EnumRecentlyUsed(m_ctlRecentlyUsed);
	}
}

BEGIN_MESSAGE_MAP(CIcuRegexAutoConfigDlg, CAutoConfigDlg)
	ON_EN_CHANGE(IDC_ED_FIND, OnEnChange)
	ON_EN_CHANGE(IDC_ED_REPLACE, OnEnChange)
	ON_BN_CLICKED(IDC_CHK_IGNORE_CASE, OnEnChange)
	ON_CBN_SELCHANGE(IDC_CB_RECENTLY_USED, OnCbnSelchangeCbRecentlyUsed)
	ON_COMMAND_RANGE(ID__32768,ID__32788,OnMenuItems)
	ON_BN_CLICKED(IDC_BTN_POPUP_HELP, OnBnClickedBtnPopupHelp)
	ON_BN_CLICKED(IDC_BUTTON_DEL_CUR, OnBnClickedButtonDelCur)
END_MESSAGE_MAP()

void CIcuRegexAutoConfigDlg::OnMenuItems(UINT nID)
{
	CMenu menu;
	if (menu.LoadMenu(IDR_MENU_REGEX_HELPER))
	{
		CMenu* pPopup = menu.GetSubMenu(0);
		ASSERT(pPopup != NULL);
		CString strMenuItem;
		pPopup->GetMenuString(nID,strMenuItem,MF_BYCOMMAND);
		int nIndexTab = strMenuItem.Find('\t');
		m_ctlFind.ReplaceSel(strMenuItem.Left(nIndexTab),true);
		m_ctlFind.SetFocus();
	}
}

void CIcuRegexAutoConfigDlg::OnBnClickedBtnPopupHelp()
{
	CMenu menu;
	if (menu.LoadMenu(IDR_MENU_REGEX_HELPER))
	{
		CMenu* pPopup = menu.GetSubMenu(0);
		ASSERT(pPopup != NULL);
		CWnd* pWnd = GetDlgItem(IDC_ED_FIND);
		CPoint curPoint;
		GetCursorPos(&curPoint);
		pPopup->TrackPopupMenu(TPM_LEFTALIGN | TPM_RIGHTBUTTON, curPoint.x, curPoint.y, this);
	}
}

void CIcuRegexAutoConfigDlg::OnEnChange()
{
	// indicate it's modified
	SetModified();
}

BOOL CIcuRegexAutoConfigDlg::OnApply()
{
	BOOL bRet = CAutoConfigDlg::OnApply();

	if( bRet )
	{
		AddToRecentlyUsed(m_strConverterIdentifier);
		EnumRecentlyUsed(m_ctlRecentlyUsed);
	}

	return bRet;
}

void CIcuRegexAutoConfigDlg::OnBnClickedButtonDelCur()
{
	// if the user selects one from the recently used combo box, then fill in the Find and Replace edit controls
	int nIndex = m_ctlRecentlyUsed.GetCurSel();
	if( nIndex < 0 )
		return;

	CString strExpression;
	m_ctlRecentlyUsed.GetLBText(nIndex,strExpression);

	CString strDeleteQuery;
	strDeleteQuery.Format(_T("Are you sure you want to delete the following expression?\n\n%s"), strExpression );
	if( MessageBox(strDeleteQuery, MB_YESNOCANCEL) == IDYES )
	{
		RemFromRecentlyUsed(strExpression);
		m_ctlRecentlyUsed.DeleteString(nIndex);
		m_ctlRecentlyUsed.SetCurSel(--nIndex);
	}
}

#include <unicode/regex.h>
void CIcuRegexAutoConfigDlg::OnCbnSelchangeCbRecentlyUsed()
{
	// if the user selects one from the recently used combo box, then fill in the Find and Replace edit controls
	int nIndex = m_ctlRecentlyUsed.GetCurSel();
	if( nIndex < 0 )
		return;

	m_ctlRecentlyUsed.GetLBText(nIndex,m_strConverterIdentifier);

	if( m_strConverterIdentifier.IsEmpty() )
		return;

	m_bIgnoreCase = false;
	DeconstructConverterSpec(m_strConverterIdentifier,m_strFind,m_strReplace,m_bIgnoreCase);

	UpdateData(false);

	SetModified();
}
