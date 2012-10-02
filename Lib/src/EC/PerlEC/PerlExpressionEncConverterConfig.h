// PerlExpressionEncConverterConfig.h : Declaration of the CPerlExpressionEncConverterConfig

#pragma once

#include "Resource.h"     // from EncCnvtrs (because we use their interface)
#include "PerlEC.h"
#include "ECEncConverter.h"
#include "PerlExpressionAutoConfigDlg.h"

extern LPCTSTR clpszPerlExpressionProgID;

// CPerlExpressionEncConverterConfig
class CPerlExpressionEncConverterConfig;  // forward declaration
typedef CComCoClass<CPerlExpressionEncConverterConfig, &CLSID_PerlExpressionEncConverterConfig> PerlExpressionConfigComCoClass;

class ATL_NO_VTABLE CPerlExpressionEncConverterConfig :
	public CEncConverterConfig
  , public PerlExpressionConfigComCoClass
{
public:
	CPerlExpressionEncConverterConfig()
		: CEncConverterConfig
			(
				clpszPerlExpressionProgID,
				_T("Perl Expression"),    // must match with what's in PyExpressionEncConverter.rgs
				_T("Perl Expression Plug-in About box.htm"),
				ProcessTypeFlags_PerlExpression // defining process type
			)
	{
	}


DECLARE_REGISTRY_RESOURCEID(IDR_PERLEXPR_CONFIG)

BEGIN_COM_MAP(CPerlExpressionEncConverterConfig)
	COM_INTERFACE_ENTRY(IEncConverterConfig)
	COM_INTERFACE_ENTRY(IDispatch)
	COM_INTERFACE_ENTRY(ISupportErrorInfo)
END_COM_MAP()

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

// IEncConverter
public:
	STDMETHOD(Configure)(IEncConverters* pECs, BSTR strFriendlyName, ConvType eConversionType, BSTR strLhsEncodingID, BSTR strRhsEncodingID, VARIANT_BOOL *bRet);
	STDMETHOD(DisplayTestPage)(IEncConverters* pECs, BSTR strFriendlyName, BSTR strConverterIdentifier, ConvType eConversionType, BSTR strTestData);

protected:
	virtual HRESULT Error(UINT nID, const IID& iid, HRESULT hRes)
	{
		return PerlExpressionConfigComCoClass::Error(nID, iid, hRes);
	}
	virtual HRESULT Error(LPCTSTR lpszDesc, const IID& iid, HRESULT hRes)
	{
		return PerlExpressionConfigComCoClass::Error(lpszDesc, iid, hRes);
	}
};

OBJECT_ENTRY_AUTO(__uuidof(PerlExpressionEncConverterConfig), CPerlExpressionEncConverterConfig)
