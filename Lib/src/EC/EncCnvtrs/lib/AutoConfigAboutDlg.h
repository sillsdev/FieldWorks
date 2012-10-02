#pragma once
#include "ECResource.h"
#include "AutoConfigPropertyPage.h"
#include "Web_browser.h"

// CAutoConfigAboutDlg dialog
class CAutoConfigAboutDlg : public CAutoConfigPropertyPage
{
	DECLARE_DYNAMIC(CAutoConfigAboutDlg)

public:
	CAutoConfigAboutDlg() {};   // don't care ctor (for the "DisplayTestPage" method)
	CAutoConfigAboutDlg(const CString& strHtmlFilename);
	virtual ~CAutoConfigAboutDlg()
	{
	};

// Dialog Data
	enum { IDD = IDD_DLG_AUTO_CONFIG_ABOUT };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	virtual BOOL OnInitDialog();
	virtual BOOL PreTranslateMessage(MSG* pMsg);

	// gotta implement the following pure virtual function to work around a compiler bug
	//  (see comment in AutoConfigPropertyPage.cpp), but it doesn't really do
	//  anything (here) except call the base-base class implementation
	virtual BOOL WorkAroundCompilerBug_OnApply()
	{
		return CPropertyPage::OnApply();
	}

	DECLARE_MESSAGE_MAP()
	afx_msg void OnCtrlC();
	HACCEL  m_hAccelTable;       // accelerator table

	CString         m_strHtmlFilename;
	CWeb_browser    m_ctlWebBrowser;
	BOOL            m_bLoaded;
};
