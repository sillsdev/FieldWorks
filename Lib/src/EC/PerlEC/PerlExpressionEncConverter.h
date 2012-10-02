// PerlExpressionEncConverter.h : Declaration of the CPerlExpressionEncConverter

#pragma once

#include "ECResource.h"     // from EncCnvtrs (because we use their interface)
#include "PerlEC.h"
#include "ECEncConverter.h"
#include "PerlExpressionEncConverterConfig.h"

class CPerlExpressionEncConverter;  // forward declaration

// CPerlExpressionEncConverter
typedef CComCoClass<CPerlExpressionEncConverter, &CLSID_PerlExpressionEncConverter> PerlExpressionComCoClass;

class ATL_NO_VTABLE CPerlExpressionEncConverter :
	public CEncConverter
  , public PerlExpressionComCoClass
{
public:
	CPerlExpressionEncConverter();

DECLARE_REGISTRY_RESOURCEID(IDR_PERLEXPR)


BEGIN_COM_MAP(CPerlExpressionEncConverter)
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
		UnLoadPerl();
		delete m_pScript;
		m_pScript = 0;
	}

// IEncConverter
public:
	STDMETHOD(Initialize)(BSTR ConverterName, BSTR converterSpec, BSTR* LhsEncodingID, BSTR* RhsEncodingID, ConvType* peConversionType, long* plProcessTypeFlags, long CodePageInput, long CodePageOutput, VARIANT_BOOL bAdding);
	virtual HRESULT GetConfigurator(IEncConverterConfig* *pConfigurator)
	{
		CComObject<CPerlExpressionEncConverterConfig>* p = 0;
		HRESULT hr = CComObject<CPerlExpressionEncConverterConfig>::CreateInstance(&p);
		if( SUCCEEDED(hr) )
		{
			hr = p->QueryInterface(pConfigurator);
		}

		return hr;
	}

protected:
	HRESULT Compile(const CString& strPackageNameT, const CString& strExpressionT);
	HRESULT CheckForStdError();
	CString CheckForStdOutput();

	// virtual HRESULT ReturnError(long status);
	virtual HRESULT Error(UINT nID, const IID& iid, HRESULT hRes)
			{
				return PerlExpressionComCoClass::Error(nID, iid, hRes);
			};
	virtual HRESULT Error(LPCTSTR lpszDesc, const IID& iid, HRESULT hRes)
			{
				return PerlExpressionComCoClass::Error(lpszDesc, iid, hRes);
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
	virtual HRESULT GetAttributeKeys(CComSafeArray<BSTR>& rSa);
	virtual HRESULT Load(const CString& strExpressionPathAndArgs);
	virtual EncodingForm    DefaultUnicodeEncForm(BOOL bForward, BOOL bLHS)
			{
				return EncodingForm_UTF16;
			};

	BOOL        m_bUseInOut;
	CScript*    m_pScript;
	CPerlScalar m_sInput;
	CPerlScalar m_sOutput;

	UTF8Mode    m_eStringDataTypeIn;
	UTF8Mode    m_eStringDataTypeOut;
};

OBJECT_ENTRY_AUTO(__uuidof(PerlExpressionEncConverter), CPerlExpressionEncConverter)
