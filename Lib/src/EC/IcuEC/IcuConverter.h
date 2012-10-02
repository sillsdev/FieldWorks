// IcuConverter.h : Declaration of the CIcuConverter

#pragma once

#include "ECResource.h"     // from EncCnvtrs (because we use their interface)
#include "IcuEC.h"
#include "ECEncConverter.h"
#include "unicode/ucnv.h"   /* C++ Converter API    */
#include "IcuConvEncConverterConfig.h"

// CIcuConverter
class CIcuConverter;  // forward declaration
typedef CComCoClass<CIcuConverter, &CLSID_IcuECConverter> IcuCnvtrComCoClass;

class ATL_NO_VTABLE CIcuConverter :
	public CEncConverter
  , public IcuCnvtrComCoClass
{
public:
	CIcuConverter();

DECLARE_REGISTRY_RESOURCEID(IDR_ICUCONVERTER)


BEGIN_COM_MAP(CIcuConverter)
	COM_INTERFACE_ENTRY(IEncConverter)
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
		if( m_pConverter != 0 )
		{
			ucnv_close(m_pConverter);
			m_pConverter = 0;
		}
	}

// IEncConverter
public:
	STDMETHOD(Initialize)(BSTR ConverterName, BSTR converterSpec, BSTR* LhsEncodingID, BSTR* RhsEncodingID, ConvType* peConversionType, long* plProcessTypeFlags, long CodePageInput, long CodePageOutput, VARIANT_BOOL bAdding);
	STDMETHOD(get_ConverterNameEnum)(SAFEARRAY* *pVal);
	virtual HRESULT GetConfigurator(IEncConverterConfig* *pConfigurator)
	{
		CComObject<CIcuConvEncConverterConfig>* p = 0;
		HRESULT hr = CComObject<CIcuConvEncConverterConfig>::CreateInstance(&p);
		if( SUCCEEDED(hr) )
		{
			hr = p->QueryInterface(pConfigurator);
		}
		return hr;
	}

protected:
	virtual HRESULT ReturnError(long status);
	virtual HRESULT Error(UINT nID, const IID& iid, HRESULT hRes);
	virtual HRESULT Error(LPCTSTR lpszDesc, const IID& iid, HRESULT hRes);
	virtual BOOL    IsFileLoaded() const    { return (m_pConverter != 0); };
	virtual HRESULT PreConvert
						(
							EncodingForm    eInEncodingForm,
							EncodingForm&	eInFormEngine,
							EncodingForm    eOutEncodingForm,
							EncodingForm&	eOutFormEngine,
							NormalizeFlags& eNormalizeOutput,
							BOOL            bForward,
							UINT            nInactivityWarningTimeOut
						);
	virtual HRESULT DoConvert
						(
							LPBYTE  lpInBuffer,
							UINT    nInLen,
							LPBYTE  lpOutBuffer,
							UINT&   rnOutLen
						);
	virtual void InactivityWarning();
	virtual HRESULT GetAttributeKeys(CComSafeArray<BSTR>& rSa);
	virtual HRESULT Load(const CComBSTR& strTablePath);
	virtual EncodingForm    DefaultUnicodeEncForm(BOOL bForward, BOOL bLHS);

	UConverter* m_pConverter;
	BOOL        m_bToWide;
	CComBSTR    m_strStandardName;
};

OBJECT_ENTRY_AUTO(__uuidof(IcuECConverter), CIcuConverter)
