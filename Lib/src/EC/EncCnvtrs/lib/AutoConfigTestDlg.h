#pragma once
#include "afxwin.h"
#include "ECResource.h"
#include "AutoConfigPropertyPage.h"

// CAutoConfigTestDlg dialog

class CAutoConfigTestDlg : public CAutoConfigPropertyPage
{
	DECLARE_DYNAMIC(CAutoConfigTestDlg)

public:
	CAutoConfigTestDlg(const CString& strTestData = _T("Test Data"));
	virtual ~CAutoConfigTestDlg()
	{
	};

// Dialog Data
	enum { IDD = IDD_DLG_AUTO_CONFIG_TEST };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	virtual BOOL OnSetActive();
	virtual BOOL OnKillActive();

	// gotta implement the following pure virtual function to work around a compiler bug
	//  (see comment in AutoConfigPropertyPage.cpp), but it doesn't really do
	//  anything (here) except call the base-base class implementation.
	virtual BOOL WorkAroundCompilerBug_OnApply()
	{
		return CPropertyPage::OnApply();
	}

	void DisplayNoConfigError();

	DECLARE_MESSAGE_MAP()
	afx_msg void OnBnClickedBtnTest();
	afx_msg void OnEnChangeEdInput();

protected:
	CString                 m_strInput;
	CComPtr<IEncConverter>  m_pEC;
	ConvType                m_eConvType;

	CEdit m_ctlOutput;
	CEdit m_ctlInput;
	CEdit m_ctlInputHex;
	CEdit m_ctlOutputHex;
	CButton m_ctlReverseDir;
};
