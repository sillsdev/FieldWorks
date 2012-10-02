// IcuRegexEncConverter.h : Declaration of the CIcuRegexEncConverter

#pragma once

#include "ECResource.h"     // from EncCnvtrs (because we use their interface)
#include "IcuEC.h"
#include "ECEncConverter.h"
#include <unicode/regex.h>
#include "IcuRegexEncConverterConfig.h"

// CIcuRegexEncConverter
class CIcuRegexEncConverter;  // forward declaration
typedef CComCoClass<CIcuRegexEncConverter, &CLSID_IcuECRegex> IcuRegexComCoClass;

class ATL_NO_VTABLE CIcuRegexEncConverter :
	public CEncConverter
  , public IcuRegexComCoClass
{
public:
	CIcuRegexEncConverter();

DECLARE_REGISTRY_RESOURCEID(IDR_ICU_REGEX)


BEGIN_COM_MAP(CIcuRegexEncConverter)
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
		delete  m_pMatcher;
		m_pMatcher = 0;
	}

// IEncConverter
public:
	STDMETHOD(Initialize)(BSTR ConverterName, BSTR converterSpec, BSTR* LhsEncodingID, BSTR* RhsEncodingID, ConvType* peConversionType, long* plProcessTypeFlags, long CodePageInput, long CodePageOutput, VARIANT_BOOL bAdding);
	virtual HRESULT GetConfigurator(IEncConverterConfig* *pConfigurator)
	{
		CComObject<CIcuRegexEncConverterConfig>* p = 0;
		HRESULT hr = CComObject<CIcuRegexEncConverterConfig>::CreateInstance(&p);
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
	virtual HRESULT Error(UINT nID, const IID& iid, HRESULT hRes)
					{
						return IcuRegexComCoClass::Error(nID, iid, hRes);
					};
	virtual HRESULT Error(LPCTSTR lpszDesc, const IID& iid, HRESULT hRes)
					{
						return IcuRegexComCoClass::Error(lpszDesc, iid, hRes);
					};
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
	virtual BOOL    IsMatcherLoaded() const { return (m_pMatcher != 0); };

	UnicodeString   m_strFind;
	UnicodeString   m_strReplace;
	RegexMatcher*   m_pMatcher;
};

OBJECT_ENTRY_AUTO(__uuidof(IcuECRegex), CIcuRegexEncConverter)

// the format of the converter spec for ICU regular expressions is:
//  {Find string}->{Replace} ({flags})
// e.g.
//  "[aeiou]->V /i"
extern LPCTSTR  clpszFindReplaceDelimiter;  /* = _T("->") */
extern LPCTSTR  clpszCaseInsensitiveFlag;   /* = _T(" /i"); */
extern BOOL     DeconstructConverterSpec
				(
					const CString&  strConverterSpec,
					CString&        strFind,
					CString&        strReplace,
					BOOL&           bIgnoreCase
				);
