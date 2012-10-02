// IcuRegexEncConverterConfig.h : Declaration of the CIcuRegexEncConverterConfig

#pragma once

#include "Resource.h"
#include "ECResource.h"     // from EncCnvtrs (because we use their interface)
#include "ECEncConverter.h"
#include "IcuEC.h"

extern LPCTSTR clpszIcuRegexProgId;

// CIcuRegexEncConverterConfig
class CIcuRegexEncConverterConfig;  // forward declaration
typedef CComCoClass<CIcuRegexEncConverterConfig, &CLSID_IcuECRegexConfig> IcuRegexConfigComCoClass;

class ATL_NO_VTABLE CIcuRegexEncConverterConfig :
	public CEncConverterConfig
  , public IcuRegexConfigComCoClass
{
public:
	CIcuRegexEncConverterConfig()
		: CEncConverterConfig
			(
				clpszIcuRegexProgId,
				_T("Regular Expression Find and Replace (ICU)"),    // must match with what's in IcuRegex.rgs
				_T("ICU Regular Expression Plug-in About box.htm"),
				ProcessTypeFlags_ICURegularExpression   // defining process type
			)
	{
	}

DECLARE_REGISTRY_RESOURCEID(IDR_ICU_REGEX_CONFIG)

BEGIN_COM_MAP(CIcuRegexEncConverterConfig)
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

// IEncConverterConfig
public:
	STDMETHOD(Configure)(IEncConverters* pECs, BSTR strFriendlyName, ConvType eConversionType, BSTR strLhsEncodingID, BSTR strRhsEncodingID, VARIANT_BOOL *bRet);
	STDMETHOD(DisplayTestPage)(IEncConverters* pECs, BSTR strFriendlyName, BSTR strConverterIdentifier, ConvType eConversionType, BSTR strTestData);

protected:
	virtual HRESULT Error(UINT nID, const IID& iid, HRESULT hRes)
	{
		return IcuRegexConfigComCoClass::Error(nID, iid, hRes);
	}
	virtual HRESULT Error(LPCTSTR lpszDesc, const IID& iid, HRESULT hRes)
	{
		return IcuRegexConfigComCoClass::Error(lpszDesc, iid, hRes);
	}
};

OBJECT_ENTRY_AUTO(__uuidof(IcuECRegexConfig), CIcuRegexEncConverterConfig)
