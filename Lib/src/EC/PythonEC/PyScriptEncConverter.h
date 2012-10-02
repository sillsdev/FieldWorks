// PyScriptEncConverter.h : Declaration of the CPyScriptEncConverter

#pragma once

#include "ECResource.h"     // from EncCnvtrs (because we use their interface)
#include "PythonEC.h"
#include "ECEncConverter.h"
#include "PyScriptEncConverterConfig.h"

#ifdef _DEBUG
#undef _DEBUG
#include <Python.h>
#define _DEBUG
#else
#include <Python.h>
#endif

// CPyScriptEncConverter
class CPyScriptEncConverter;  // forward declaration
typedef CComCoClass<CPyScriptEncConverter, &CLSID_PyScriptEncConverter> PyScriptComCoClass;

class ATL_NO_VTABLE CPyScriptEncConverter :
	public CEncConverter
  , public PyScriptComCoClass
{
public:
	CPyScriptEncConverter();

DECLARE_REGISTRY_RESOURCEID(IDR_PYSCRIPT)


BEGIN_COM_MAP(CPyScriptEncConverter)
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
		ResetPython();
	}

// IEncConverter
public:
	STDMETHOD(Initialize)(BSTR ConverterName, BSTR converterSpec, BSTR* LhsEncodingID, BSTR* RhsEncodingID, ConvType* peConversionType, long* plProcessTypeFlags, long CodePageInput, long CodePageOutput, VARIANT_BOOL bAdding);
	virtual HRESULT GetConfigurator(IEncConverterConfig* *pConfigurator)
	{
		CComObject<CPyScriptEncConverterConfig>* p = 0;
		HRESULT hr = CComObject<CPyScriptEncConverterConfig>::CreateInstance(&p);
		if( SUCCEEDED(hr) )
		{
			hr = p->QueryInterface(pConfigurator);
		}

		return hr;
	}

protected:
	// virtual HRESULT ReturnError(long status);
	virtual HRESULT Error(UINT nID, const IID& iid, HRESULT hRes)
			{
				return PyScriptComCoClass::Error(nID, iid, hRes);
			};
	virtual HRESULT Error(LPCTSTR lpszDesc, const IID& iid, HRESULT hRes)
			{
				return PyScriptComCoClass::Error(lpszDesc, iid, hRes);
			};
	virtual BOOL    IsModuleLoaded() const    { return (m_pModule != 0); };
	virtual void    ResetPython();
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
	virtual HRESULT Load(const CString& strScriptPathAndArgs);
	virtual EncodingForm    DefaultUnicodeEncForm(BOOL bForward, BOOL bLHS)
			{
				return EncodingForm_UTF16;
			};
	HRESULT         ErrorOccurred();

	PyObject*   m_pFunc;
	PyObject*   m_pModule;
	PyObject*   m_pArgs;
	INT_PTR     m_nArgCount;

	CTime       m_timeLastModified;
	CString     m_strFileSpec;
	CString     m_strScriptName;
	CString     m_strFuncName;

	// keep track of how we're supposed to interpret the data (it comes into "DoConvert"
	//  as a 'byte' pointer, but we need to create it as a Python object which is
	//  different depending on the expected type.
	typedef enum PyStringDataType
	{
		eBytes,
		eUCS2,
		eUCS4       // not supported yet by this code
	};

	PyStringDataType    m_eStringDataTypeIn;
	PyStringDataType    m_eStringDataTypeOut;
};

OBJECT_ENTRY_AUTO(__uuidof(PyScriptEncConverter), CPyScriptEncConverter)

extern LPCTSTR clpszPyScriptDefFuncName;
