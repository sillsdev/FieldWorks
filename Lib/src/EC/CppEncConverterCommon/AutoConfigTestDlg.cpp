// AutoConfigTestDlg.cpp : implementation file
//

#include "stdafx.h"
#include "AutoConfigTestDlg.h"


// CAutoConfigTestDlg dialog

IMPLEMENT_DYNAMIC(CAutoConfigTestDlg, CAutoConfigPropertyPage)
CAutoConfigTestDlg::CAutoConfigTestDlg(const CString& strTestData)
  : CAutoConfigPropertyPage(CAutoConfigTestDlg::IDD)
  , m_strInput(strTestData)
  , m_pEC(0)
  , m_eConvType(ConvType_Unknown)
{
}

void CAutoConfigTestDlg::DoDataExchange(CDataExchange* pDX)
{
	CAutoConfigPropertyPage::DoDataExchange(pDX);
	DDX_Control(pDX,IDC_ED_INPUT,m_ctlInput);
	DDX_Text(pDX,IDC_ED_INPUT,m_strInput);
	DDX_Control(pDX,IDC_ED_OUTPUT,m_ctlOutput);
	DDX_Control(pDX,IDC_ED_INPUT_HEX,m_ctlInputHex);
	DDX_Control(pDX,IDC_ED_OUTPUT_HEX,m_ctlOutputHex);
	DDX_Control(pDX,IDC_CHK_REVERSE,m_ctlReverseDir);
}

BEGIN_MESSAGE_MAP(CAutoConfigTestDlg, CAutoConfigPropertyPage)
	ON_BN_CLICKED(IDC_BTN_TEST, OnBnClickedBtnTest)
	ON_EN_CHANGE(IDC_ED_INPUT, OnEnChangeEdInput)
	ON_BN_CLICKED(IDC_CHK_REVERSE, OnEnChangeEdInput)
END_MESSAGE_MAP()

void CAutoConfigTestDlg::DisplayNoConfigError()
{
	MessageBox(_T("You must first configure the conversion process on the Setup tab"));
}

#include "AutoConfigSheet.h"

BOOL CAutoConfigTestDlg::OnSetActive( )
{
	// go ahead and setup the converter now (assuming that we're going to
	//  eventually do the test), because the initialization process will fix
	//  certain properties (e.g. the ICU Converter plug-in will set the eConvType).
	// In fact, make it easy, just ask the sheet (which asks the "Setup" dlg) to
	//  give us a IEncConverter (already initialized) for the appropriate configuration.
	CAutoConfigSheet* pSheet = DYNAMIC_DOWNCAST(CAutoConfigSheet, GetParent());
	m_pEC = pSheet->InitializeEncConverter();

	if(     !m_pEC
		||  FAILED(m_pEC->get_ConversionType(&m_eConvType))
		||  (m_eConvType == ConvType_Unknown)
	)
	{
		DisplayNoConfigError();
		return false;
	}

	// don't allow the 'test' button to be clicked unless there's something there.
	GetDlgItem(IDC_BTN_TEST)->EnableWindow(!m_strInput.IsEmpty());

	// finally, if we now see that it's bi-directional, add a reverse checkbox as well.
	m_ctlReverseDir.ShowWindow( (IsUnidirectional(m_eConvType)) ? SW_HIDE : SW_SHOW);

	BOOL b = CAutoConfigPropertyPage::OnSetActive();
	OnEnChangeEdInput();
	return b;
}

BOOL CAutoConfigTestDlg::OnKillActive()
{
	// don't keep it around if the user switches to another tab (they might reconfigure it and we'd
	//  have to re-acquire it anyway).
	m_pEC.Detach();
	ASSERT(!m_pEC);
	return true;
}

// give a hex representation of the data (for bit-heads :-)
CString GetInHex(NormConversionType eType, const CString& str)
{
	CString strInHex;
	if( eType == NormConversionType_eLegacy )
	{
		CStringA strInputString = CT2A(str);
		int nLen = strInputString.GetLength();
		CString strValue;
		for( int i = 0; i < nLen; i++ )
		{
			TCHAR ch = (TCHAR)(strInputString[i] & 0xFF);
			strValue.Format(_T("%c (d%d) "), ch, ch );
			strInHex += strValue;
		}
	}
	else    // eUnicode
	{
		int nLen = str.GetLength();
		CString strValue, strValues;
		for( int i = 0; i < nLen; i++ )
		{
			if( str[i] == 0 )
				strValue = _T("nul (u0000)  ");
			else
				strValue.Format(_T("%c (u%04x) "), str[i], str[i]);
			strInHex += strValue;
		}
	}

	return strInHex;
}

// CAutoConfigTestDlg message handlers
void CAutoConfigTestDlg::OnBnClickedBtnTest()
{
	CWaitCursor x;  // Perl takes a *long* time!
	if( !!m_pEC )
	{
		BOOL bDirectionForward = (m_ctlReverseDir.GetCheck() == 0);
		m_pEC->put_DirectionForward( (bDirectionForward) ? -1 : 0 );

		CComBSTR strOutput;
		HRESULT hr = m_pEC->Convert(m_strInput.AllocSysString(),&strOutput);

		if( ProcessHResult(hr, m_pEC) )
		{
			m_ctlOutput.SetWindowText(strOutput);
			NormConversionType eType = (bDirectionForward)
				? NormalizeRhsConversionType(m_eConvType) : NormalizeLhsConversionType(m_eConvType);
			CString strOutputInHex = GetInHex(eType,CString(strOutput));
			m_ctlOutputHex.SetWindowText(strOutputInHex);
		}
	}
}

void CAutoConfigTestDlg::OnEnChangeEdInput()
{
	m_ctlInput.GetWindowText(m_strInput);

	// don't allow the 'test' button to be clicked unless there's something there.
	GetDlgItem(IDC_BTN_TEST)->EnableWindow(!m_strInput.IsEmpty());

	if( !m_strInput.IsEmpty() )
	{
		BOOL bDirectionForward = (m_ctlReverseDir.GetCheck() == 0);
		NormConversionType eType = (bDirectionForward)
			? NormalizeLhsConversionType(m_eConvType) : NormalizeRhsConversionType(m_eConvType);
		CString strInputInHex = GetInHex(eType,m_strInput);
		m_ctlInputHex.SetWindowText(strInputInHex);
	}

	// whenever the input changes, clear out the output
	m_ctlOutput.SetWindowText(_T(""));
	m_ctlOutputHex.SetWindowText(_T(""));
}
