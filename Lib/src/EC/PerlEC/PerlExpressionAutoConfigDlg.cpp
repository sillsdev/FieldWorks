 PerlExpressionAutoConfigDlg.cpp : implementation file
//

#include "stdafx.h"
#include "PerlEC.h"
#include "PerlExpressionAutoConfigDlg.h"
#include "Resource.h"

// CPerlExpressionAutoConfigDlg dialog

extern LPCTSTR clpszPerlExpressionImplType;
extern LPCTSTR clpszPerlExpressionProgID;

IMPLEMENT_DYNAMIC(CPerlExpressionAutoConfigDlg, CAutoConfigDlg)
CPerlExpressionAutoConfigDlg::CPerlExpressionAutoConfigDlg
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
		CPerlExpressionAutoConfigDlg::IDD,
		strFriendlyName,
		strConverterIdentifier,
		eConversionType,
		strLhsEncodingId,
		strRhsEncodingId,
		lProcessTypeFlags,
		m_bIsInRepository
	)
{
	// Perl expressions are probably mostly for Legacy
	if( m_eConversionType == ConvType_Unknown )
	{
		m_nLhsExpects = 1;  // bytes
		m_nRhsReturns = 1;
	}
}

BOOL CPerlExpressionAutoConfigDlg::OnInitDialog()
{
	BOOL bRet = CAutoConfigDlg::OnInitDialog();

	m_ctlExpression.SetParent(this); // so we get the reflected messages
	m_ctlExpression.SetEventMask(m_ctlExpression.GetEventMask() | ENM_SELCHANGE);
	m_ctlExpression.SetOptions(ECOOP_OR, ECO_AUTOWORDSELECTION);

	// see if we can make it word wrap
	m_ctlExpression.SetTargetDevice(NULL, 0);

	CHARFORMAT cf;
	memset(&cf,0,sizeof(CHARFORMAT) );
	cf.cbSize = sizeof(CHARFORMAT);

	// set it to what the user wants as the system font.
	cf.dwMask = CFM_FACE | CFM_FACE;
	cf.yHeight = 10;
	_tcscpy_s(cf.szFaceName, _T("Courier New") ); // something fixed-width
	m_ctlExpression.SendMessage(EM_SETCHARFORMAT, SCF_SELECTION, (LPARAM) &cf);

	return bRet;
}

void CPerlExpressionAutoConfigDlg::DoDataExchange(CDataExchange* pDX)
{
	CAutoConfigDlg::DoDataExchange(pDX);
	DDX_Control(pDX,IDC_RICHED_EXPRESSION,m_ctlExpression);
	DDX_Text(pDX,IDC_RICHED_EXPRESSION,m_strConverterIdentifier);
	DDX_Control(pDX,IDC_CB_RECENTLY_USED,m_ctlRecentlyUsed);

	if( pDX->m_bSaveAndValidate )
	{
		m_strConverterIdentifier.Trim();
		m_ctlExpression.SetWindowText(m_strConverterIdentifier); // reset it (to normalize the font/style, trimmed, etc).
		m_ctlExpression.SetSel(0,0);
	}
	else
	{
		EnumRecentlyUsed(m_ctlRecentlyUsed);
	}
}

CString CPerlExpressionAutoConfigDlg::ImplType()
{
	return clpszPerlExpressionImplType;
}

CString CPerlExpressionAutoConfigDlg::ProgramID()
{
	return clpszPerlExpressionProgID;
}

CString CPerlExpressionAutoConfigDlg::DefaultFriendlyName()
{
	CString strDefaultFriendlyName = m_strConverterIdentifier;
	int nIndex;
	if ((nIndex = strDefaultFriendlyName.Find(_T("\n"))) != -1)
		strDefaultFriendlyName = strDefaultFriendlyName.Left(nIndex);
	strDefaultFriendlyName.Remove(';'); // remove semi-colons since those aren't allowed in friendly names
	return strDefaultFriendlyName;
}

BOOL CPerlExpressionAutoConfigDlg::OnApply()
{
	BOOL bRet = CAutoConfigDlg::OnApply();
	if( bRet )
		AddToRecentlyUsed(m_strConverterIdentifier);
	return bRet;
}

BEGIN_MESSAGE_MAP(CPerlExpressionAutoConfigDlg, CAutoConfigDlg)
	ON_NOTIFY(EN_SELCHANGE, IDC_RICHED_EXPRESSION, OnSelChange)
	ON_BN_CLICKED(IDC_BUTTON_DEL_CUR, OnBnClickedButtonDelCur)
	ON_CBN_SELCHANGE(IDC_CB_RECENTLY_USED, OnCbnSelchangeCbRecentlyUsed)
	ON_BN_CLICKED(IDC_BTN_DISTRO_CONFIG, OnBnDistroConfig)
END_MESSAGE_MAP()

void CPerlExpressionAutoConfigDlg::OnCbnSelchangeCbRecentlyUsed()
{
	// if the user selects one from the recently used combo box, then fill in the Find and Replace edit controls
	int nIndex = m_ctlRecentlyUsed.GetCurSel();
	if( nIndex < 0 )
		return;

	m_ctlRecentlyUsed.GetLBText(nIndex,m_strConverterIdentifier);
	m_ctlExpression.SetWindowText(m_strConverterIdentifier);
	SetModified();
}

void CPerlExpressionAutoConfigDlg::OnBnClickedButtonDelCur()
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

void CPerlExpressionAutoConfigDlg::OnBnDistroConfig()
{
	// get the path to the Perl installation and the default modules to load from the registry
	CStringArray astrPaths;
	EnumRegKeys(PERLEXPR_PATHS_KEY, _T(""), astrPaths);
	if( astrPaths.GetCount() == 0 )
	{
		astrPaths.Add(_T("C:\\Perl"));
		astrPaths.Add(_T("C:\\Perl\\lib"));
		astrPaths.Add(_T("C:\\Perl\\site\\lib"));
	}

	WritePerlDistroPaths(astrPaths);

	CStringArray astrModules;
	EnumRegKeys(PERLEXPR_MODULES_KEY, _T(""), astrModules);
	if( astrModules.GetCount() == 0 )
		astrModules.Add(_T("Win32"));

	WritePerlModulePaths(astrModules);
}

void CPerlExpressionAutoConfigDlg::OnSelChange(NMHDR* pNMHDR, LRESULT* pResult)
{
	ASSERT(pNMHDR->code == EN_SELCHANGE);
	CString strExpression;
	m_ctlExpression.GetWindowText(strExpression);
	if( strExpression != m_strConverterIdentifier )
		SetModified();  // always enable Apply button

	*pResult = 0;
}

BOOL CPerlExpressionAutoConfigDlg::PreTranslateMessage(MSG* pMsg)
{
	if(     (pMsg->message >= WM_KEYFIRST)
		&&  (pMsg->message <= WM_KEYLAST) )
	{
		// eat the keydown's that are tabs (put two spaces in the expression control)
		if(     (GetFocus() == &m_ctlExpression)
			&&  (pMsg->message == WM_KEYDOWN)
			&&  (pMsg->wParam == VK_TAB) )
		{
			// send it to the control (rather than the dialog box, which'll just jump to the next control
			m_ctlExpression.ReplaceSel(_T("  "), true);
			return true;
		}
	}

	return CAutoConfigDlg::PreTranslateMessage(pMsg);
}
