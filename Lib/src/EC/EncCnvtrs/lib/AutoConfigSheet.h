#pragma once

#include "AutoConfigDlg.h"
#include "AutoConfigAboutDlg.h"
#include "AutoConfigTestDlg.h"

// CAutoConfigSheet

class CAutoConfigSheet : public CPropertySheet
{
	DECLARE_DYNAMIC(CAutoConfigSheet)

public:
	CAutoConfigSheet
		(
			CAutoConfigDlg* pPgConfig,
			const CString&  strCaption,
			const CString&  strHtmlFilename,
			const CString&  strProgramID
		);
	CAutoConfigSheet
		(
			CAutoConfigDlg* pPgConfig,
			const CString&  strCaption,
			const CString&  strTestData
		);
	virtual ~CAutoConfigSheet()
		{
		};
	virtual BOOL OnInitDialog();
	virtual BOOL PreTranslateMessage(MSG* pMsg);

	IEncConverter*  InitializeEncConverter()
		{
			return m_pPgConfig->InsureApplyAndInitializeEncConverter();
		};

protected:
	CAutoConfigAboutDlg     m_pgAbout;
	CAutoConfigDlg*         m_pPgConfig;
	CAutoConfigTestDlg      m_pgTest;
	HICON                   m_hIcon;

	DECLARE_MESSAGE_MAP()
	afx_msg HCURSOR OnQueryDragIcon();
};
