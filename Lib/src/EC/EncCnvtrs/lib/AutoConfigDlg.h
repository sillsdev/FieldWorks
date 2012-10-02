#pragma once
#include "afxwin.h"
#include "afxdlgs.h"
#include "ECEncConverter.h"
#include "AutoConfigPropertyPage.h"

// CAutoConfigDlg dialog

class CAutoConfigDlg : public CAutoConfigPropertyPage
{
	DECLARE_DYNAMIC(CAutoConfigDlg)

public:
	CAutoConfigDlg
	(
		IEncConverters* pECs,
		UINT            nID,
		const CString&  strFriendlyName,
		const CString&  strConverterIdentifier,
		ConvType        eConversionType,
		const CString&  strLhsEncodingId,
		const CString&  strRhsEncodingId,
		long            lProcessTypeFlags,
		BOOL            bIsInRepository
	);
	virtual ~CAutoConfigDlg() {};

// Dialog Data
	CString     m_strFriendlyName;
	CString     m_strConverterIdentifier;
	CString     m_strLhsEncodingId;
	CString     m_strRhsEncodingId;
	ConvType    m_eConversionType;
	long        m_lProcessTypeFlags;
	BOOL        m_bIsInRepository;
	BOOL        m_bQueryToUseTempConverter;

protected:
	PtrIEncConverters   m_pECs;
	BOOL                m_bQueryForConvType;
	BOOL                m_bEditMode;            // for 'Edit' (c.f. 'Add New')--in this case, prompt for Update rather than Save
	int                 m_nLhsExpects;          // Left-hand side expects (either 'Unicode' or 'non-Unicode')
	int                 m_nRhsReturns;
	CButton             m_ctlLhsExpects;
	CButton             m_ctlRhsReturns;
	ConvType            m_eOrigConvType;        // keep track of an original version from the client (we change it sometimes and want to be able to go back)
	CString             m_strOriginalFriendlyName;

	DECLARE_MESSAGE_MAP()
public:
	void    SetVisibility(UINT nID, BOOL bVisible);
	void    ConvTypeVisibility(BOOL bVisible);
	void    SetRbIntValuesFromConvType();
	void    SetConvTypeFromIntValues();
	void    EnumRecentlyUsed(CComboBox& cbRecentlyUsed);
	void    AddToRecentlyUsed(const CString& strRecentlyUsed);
	void    RemFromRecentlyUsed(const CString& strRecentlyUsed);
	CString GetRegKey();

	virtual HRESULT AddConverterMapping();
	virtual HRESULT AddConverterMappingSub();
	virtual HRESULT BnClickedBtnAddToRepositoryEx();
	virtual BOOL CallInitializeEncConverter();  // so it can be overridden
	virtual BOOL OnKillActive();
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	virtual BOOL OnInitDialog();
	virtual CString DefaultFriendlyName() = 0;
	virtual CString ImplType() = 0;
	virtual CString ProgramID() = 0;

	// see comment in AutoConfigPropertyPage.h
	virtual BOOL WorkAroundCompilerBug_OnApply();

	IEncConverter*  InsureApplyAndInitializeEncConverter(); // called by the Test tab to make sure "Apply" was done
	virtual IEncConverter*  InitializeEncConverter();
	virtual void    ResetFields();
	BOOL UpdateData(BOOL bSaveAndValidate = TRUE);
	void SetDlgItemText(int nID, LPCTSTR str);

	afx_msg void OnBnClickedBtnAddToRepository();
	afx_msg void OnBnClickedBytesUnicodeOptions();
};

extern BOOL ProcessHResult(HRESULT hr, IUnknown* pEC);
extern CString GetDirNoSlash(const CString& strFSpec);
extern CString GetDir(const CString& strFSpec);
extern const CString& strEmpty;
