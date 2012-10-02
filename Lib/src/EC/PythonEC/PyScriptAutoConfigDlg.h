#pragma once
#include "AutoConfigDlg.h"

#ifdef _DEBUG
#undef _DEBUG
#include <Python.h>
#define _DEBUG
#else
#include <Python.h>
#endif

// CPyScriptAutoConfigDlg dialog

class CPyScriptAutoConfigDlg : public CAutoConfigDlg
{
	DECLARE_DYNAMIC(CPyScriptAutoConfigDlg)

public:
	CPyScriptAutoConfigDlg
	(
		IEncConverters* pECs,
		const CString&  strFriendlyName,
		const CString&  strConverterIdentifier,
		ConvType        eConversionType,
		const CString&  strLhsEncodingId = strEmpty,
		const CString&  strRhsEncodingId = strEmpty,
		long            lProcessTypeFlags = 0,
		BOOL            m_bIsInRepository = false   // don't care if not specified
	);
	virtual ~CPyScriptAutoConfigDlg();

// Dialog Data
	enum { IDD = IDD_DLG_AUTO_CONFIG };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	virtual BOOL OnKillActive( );
	virtual BOOL OnInitDialog();

	void    ResetPython();
	BOOL    InitPython();
	void    DeconstructConverterSpec(const CString& strScriptPathAndArgs, CString& strScriptSpec, CString& strFuncName, CString& strAddlParams);
	void    InitFuncNameComboBox();

	DECLARE_MESSAGE_MAP()
public:
	afx_msg void OnBnClickedBtnBrowse();
	afx_msg void OnCbnSelchangeCbFuncName();
	virtual CString DefaultFriendlyName()   { return m_strFuncName; };
	virtual CString ImplType();
	virtual CString ProgramID();
	virtual void    ResetFields();

protected:

	// the full file spec for the Python script
	CEdit m_ctlScriptFilespec;
	CEdit m_ctlAddlParams;
	CString m_strConverterFilespec;
	CString m_strFuncName;
	CString m_strAddlParams;
	CComboBox m_cbFunctionNames;
	CStatic m_ctlFuncPrototype;
	PyObject* m_pModule;
	PyObject* m_pDictionary;
public:
	afx_msg void OnEnChangeEditAddlParams();
};
