// IcuTransEncConverterConfig.h : Declaration of the CIcuTransEncConverterConfig

#pragma once

#include "Resource.h"
#include "ECResource.h"     // from EncCnvtrs (because we use their interface)
#include "ECEncConverter.h"
#include "IcuEC.h"

extern LPCTSTR clpszIcuTransProgId;

// CIcuTransEncConverterConfig
class CIcuTransEncConverterConfig;  // forward declaration
typedef CComCoClass<CIcuTransEncConverterConfig, &CLSID_IcuECTransConfig> IcuTransConfigComCoClass;

class ATL_NO_VTABLE CIcuTransEncConverterConfig :
	public CEncConverterConfig
  , public IcuTransConfigComCoClass
{
public:
	CIcuTransEncConverterConfig()
		: CEncConverterConfig
			(
				clpszIcuTransProgId,
				_T("ICU Transliterator"),   // must match with what's in IcuTranslit.rgs
				_T("ICU Transliterators Plug-in About box.htm"),
				ProcessTypeFlags_ICUTransliteration // defining process type
			)
	{
	}

DECLARE_REGISTRY_RESOURCEID(IDR_ICUTRANS_CONFIG)

BEGIN_COM_MAP(CIcuTransEncConverterConfig)
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
		return IcuTransConfigComCoClass::Error(nID, iid, hRes);
	}
	virtual HRESULT Error(LPCTSTR lpszDesc, const IID& iid, HRESULT hRes)
	{
		return IcuTransConfigComCoClass::Error(lpszDesc, iid, hRes);
	}
};

OBJECT_ENTRY_AUTO(__uuidof(IcuECTransConfig), CIcuTransEncConverterConfig)
