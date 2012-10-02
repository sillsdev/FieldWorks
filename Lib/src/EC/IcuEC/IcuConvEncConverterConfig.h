// IcuConvEncConverterConfig.h : Declaration of the CIcuConvEncConverterConfig

#pragma once

#include "Resource.h"
#include "ECResource.h"     // from EncCnvtrs (because we use their interface)
#include "ECEncConverter.h"
#include "IcuEC.h"

extern LPCTSTR clpszIcuConvProgId;

// CIcuConvEncConverterConfig
class CIcuConvEncConverterConfig;  // forward declaration
typedef CComCoClass<CIcuConvEncConverterConfig, &CLSID_IcuECConvConfig> IcuConvConfigComCoClass;

class ATL_NO_VTABLE CIcuConvEncConverterConfig :
	public CEncConverterConfig
  , public IcuConvConfigComCoClass
{
public:
	CIcuConvEncConverterConfig()
		: CEncConverterConfig
			(
				clpszIcuConvProgId,
				_T("ICU Converter"),    // must match with what's in IcuConverter.rgs
				_T("ICU Converters Plug-in About box.htm"),
				ProcessTypeFlags_ICUConverter   // defining process type
			)
	{
	}

DECLARE_REGISTRY_RESOURCEID(IDR_ICUCONV_CONFIG)

BEGIN_COM_MAP(CIcuConvEncConverterConfig)
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
		return IcuConvConfigComCoClass::Error(nID, iid, hRes);
	}
	virtual HRESULT Error(LPCTSTR lpszDesc, const IID& iid, HRESULT hRes)
	{
		return IcuConvConfigComCoClass::Error(lpszDesc, iid, hRes);
	}
};

OBJECT_ENTRY_AUTO(__uuidof(IcuECConvConfig), CIcuConvEncConverterConfig)
