#pragma once
#include "afxwin.h"

#include "ECResource.h"

// CECAdvConfigDlg dialog

class CECAdvConfigDlg : public CDialog
{
	DECLARE_DYNAMIC(CECAdvConfigDlg)

public:
	CECAdvConfigDlg
		(
		PtrIEncConverters   pECs,
		CWnd*               pParent,
		const CString&      strLhsEncodingID,
		const CString&      strRhsEncodingID,
		long                lProcessType
		);

	virtual ~CECAdvConfigDlg()
		{
		};

	CString     LhsEncodingID;
	CString     RhsEncodingID;
	long        ProcessType;

// Dialog Data
	enum { IDD = IDD_DLG_ADV_EC_CONFIG };

protected:
	PtrIEncConverters   m_pECs;

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	void AddAndOrSelect(CComboBox& ctrl, const CString& str);
	void SetProcessTypeBitField(UINT nID, ProcessTypeFlags nBitMask);
	void GetProcessTypeBitField(UINT nID, ProcessTypeFlags nBitMask);

	DECLARE_MESSAGE_MAP()
	CComboBox m_ctlCbLhsEncodingID;
	CComboBox m_ctlCbRhsEncodingID;
	BOOL m_bComboBoxInitialized;
};
