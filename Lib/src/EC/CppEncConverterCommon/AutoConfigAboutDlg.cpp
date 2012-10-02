// AutoConfigAboutDlg.cpp : implementation file
//

#include "stdafx.h"
#include "AutoConfigAboutDlg.h"
#include "ECResource.h"

// CAutoConfigAboutDlg dialog

IMPLEMENT_DYNAMIC(CAutoConfigAboutDlg, CAutoConfigPropertyPage)
CAutoConfigAboutDlg::CAutoConfigAboutDlg(const CString& strHtmlFilename)
  : CAutoConfigPropertyPage(CAutoConfigAboutDlg::IDD)
  , m_strHtmlFilename(strHtmlFilename)
{
}

void CAutoConfigAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CAutoConfigPropertyPage::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_WEB_BROWSER, m_ctlWebBrowser);
}

BEGIN_MESSAGE_MAP(CAutoConfigAboutDlg, CAutoConfigPropertyPage)
	ON_BN_CLICKED(ID_COPY, OnCtrlC)
END_MESSAGE_MAP()

extern CString GetDir(const CString& strFSpec);

// CAutoConfigSheet message handlers
BOOL CAutoConfigAboutDlg::OnInitDialog()
{
	BOOL bRet = CAutoConfigPropertyPage::OnInitDialog();

	LPCTSTR lpszResourceStr = MAKEINTRESOURCE(IDR_ACCELERATOR);
	HINSTANCE hInst = AfxFindResourceHandle(lpszResourceStr, RT_ACCELERATOR);
	m_hAccelTable = ::LoadAccelerators(hInst, lpszResourceStr);

	CString m_strHtmlAbout;
#ifndef UseModulePath
	// use the 'RootDir' registry key to get the base path to the help files (since we
	//	now put the DLLs that include this code potentially in different places--e.g.
	//	SilEcConfig22.dll goes in the \PF\CF\SIL\2.6.0.0 folder (i.e. 2.6.0.0 sub-folder)
	CRegKey keyRootPath;
	if (keyRootPath.Open(HKEY_LOCAL_MACHINE, CNVTRS_ROOT, KEY_READ) == ERROR_SUCCESS)
	{
		ULONG nChars = _MAX_PATH;
		TCHAR lpszRootDir[_MAX_PATH + 1];
		lpszRootDir[0] = 0;
		if( keyRootPath.QueryStringValue(_T("RootDir"), lpszRootDir, &nChars) == ERROR_SUCCESS )
		{
			CString strFileSpec = lpszRootDir;
			if (strFileSpec[strFileSpec.GetLength() - 1] != '\\')
				strFileSpec += '\\';

			_tcscpy_s(lpszRootDir, (LPCTSTR)strFileSpec);
			strFileSpec.Format(_T("%sHelp\\%s"), lpszRootDir, m_strHtmlFilename);

			// but... on development machines, the old approach should be used (so if the
			//	file doesn't exist where we think it should be, try based on where this DLL
			//	is located)
			CFileStatus fstat;
			if( CFile::GetStatus(strFileSpec,fstat) )
			{
				m_strHtmlAbout.Format(_T("file:///%sHelp/%s"), lpszRootDir, m_strHtmlFilename);
			}
			else
			{
				TCHAR lpszModule[_MAX_PATH + 1];
				if (GetModuleFileName(AfxGetResourceHandle(), lpszModule, _MAX_PATH))
				{
					// by default, help/about html documents are put into %Common Files%\SIL\Help folder
					m_strHtmlAbout.Format(_T("file:///%sHelp/%s"), GetDir(lpszModule), m_strHtmlFilename);
				}
			}
		}
	}
#else
	TCHAR lpszModule[_MAX_PATH];
	if (GetModuleFileName(AfxGetResourceHandle(), lpszModule, _MAX_PATH))
	{
		// by default, help/about html documents are put into %Common Files%\SIL\Help folder
		m_strHtmlAbout.Format(_T("file:///%sHelp/%s"), GetDir(lpszModule), m_strHtmlFilename);
	}
#endif

	CComVariant vEmpty;
	m_ctlWebBrowser.Navigate(m_strHtmlAbout, &vEmpty, &vEmpty, &vEmpty, &vEmpty);

	return bRet;
}

void CAutoConfigAboutDlg::OnCtrlC()
{
	m_ctlWebBrowser.ExecWB(OLECMDID_COPY,0,0,0);
}

BOOL CAutoConfigAboutDlg::PreTranslateMessage(MSG* pMsg)
{
	if(
			(pMsg->message >= WM_KEYFIRST)
		&&  (pMsg->message <= WM_KEYLAST)
	)
	{
		// if it's a key, see if it translates to an accelerator.
		return m_hAccelTable != NULL && ::TranslateAccelerator(m_hWnd, m_hAccelTable, pMsg);
	}
	return CAutoConfigPropertyPage::PreTranslateMessage(pMsg);
}