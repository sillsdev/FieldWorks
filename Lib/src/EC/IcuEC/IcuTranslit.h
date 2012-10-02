// IcuTranslit.h : Declaration of the CIcuTranslit

#pragma once

#include "ECResource.h"     // from EncCnvtrs (because we use their interface)
#include "IcuEC.h"
#include "ECEncConverter.h"
#include "unicode/translit.h"
#include "IcuTransEncConverterConfig.h"

// CIcuTranslit
class CIcuTranslit;  // forward declaration
typedef CComCoClass<CIcuTranslit, &CLSID_IcuECTransliterator> IcuTranslitComCoClass;

class ATL_NO_VTABLE CIcuTranslit :
	public CEncConverter
  , public IcuTranslitComCoClass
{
public:
	CIcuTranslit();

DECLARE_REGISTRY_RESOURCEID(IDR_ICUTRANSLIT)


BEGIN_COM_MAP(CIcuTranslit)
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
		if( m_pTForwards != 0 )
		{
			delete m_pTForwards;
			m_pTForwards = 0;
		}

		if( m_pTBackwards != 0 )
		{
			delete m_pTBackwards;
			m_pTBackwards = 0;
		}
	}

// IEncConverter
public:
	STDMETHOD(Initialize)(BSTR ConverterName, BSTR converterSpec, BSTR* LhsEncodingID, BSTR* RhsEncodingID, ConvType* peConversionType, long* plProcessTypeFlags, long CodePageInput, long CodePageOutput, VARIANT_BOOL bAdding);
	STDMETHOD(get_ConverterNameEnum)(SAFEARRAY* *pVal);
	virtual HRESULT GetConfigurator(IEncConverterConfig* *pConfigurator)
	{
		CComObject<CIcuTransEncConverterConfig>* p = 0;
		HRESULT hr = CComObject<CIcuTransEncConverterConfig>::CreateInstance(&p);
		if( SUCCEEDED(hr) )
		{
			hr = p->QueryInterface(pConfigurator);
		}
		return hr;
	}

	// these don't make sense for some subclasses
	STDMETHOD(ConvertToUnicode)(/*[in]*/ BSTR sInput, /*[out]*/ BSTR* sOutput)
	{ return E_NOTIMPL; };
	STDMETHOD(ConvertFromUnicode)(/*[in]*/ BSTR sInput, /*[out]*/ BSTR* sOutput)
	{ return E_NOTIMPL; };

protected:
	virtual HRESULT Load(const CComBSTR& strConverterSpec);
	virtual HRESULT ReturnError(long status);
	virtual HRESULT Error(UINT nID, const IID& iid, HRESULT hRes);
	virtual HRESULT Error(LPCTSTR lpszDesc, const IID& iid, HRESULT hRes);
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
	virtual BOOL    IsFileLoaded() const    { return (m_pTForwards != 0); };
	CString DisplayErrorMsgBox(UParseError parseError, LPCSTR lpID, const CString& strFunc);

	BOOL			m_bLTR;	// if true, then use m_pTForwards
	Transliterator* m_pTForwards;
	Transliterator* m_pTBackwards;
};

OBJECT_ENTRY_AUTO(__uuidof(IcuECTransliterator), CIcuTranslit)

extern BOOL IsRuleBased(const CComBSTR& strConverterSpec);
