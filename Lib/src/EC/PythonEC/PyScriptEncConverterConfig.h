// PyScriptEncConverterConfig.h : Declaration of the CPyScriptEncConverterConfig

#pragma once

#include "ECResource.h"     // from EncCnvtrs (because we use their interface)
#include "PythonEC.h"
#include "ECEncConverter.h"
#include "PyScriptAutoConfigDlg.h"

#ifdef _DEBUG
#undef _DEBUG
#include <Python.h>
#define _DEBUG
#else
#include <Python.h>
#endif

extern LPCTSTR clpszPyScriptProgID;

// CPyScriptEncConverterConfig
class CPyScriptEncConverterConfig;  // forward declaration
typedef CComCoClass<CPyScriptEncConverterConfig, &CLSID_PyScriptEncConverterConfig> PyScriptConfigComCoClass;

class ATL_NO_VTABLE CPyScriptEncConverterConfig :
	public CEncConverterConfig
  , public PyScriptConfigComCoClass
{
public:
	CPyScriptEncConverterConfig()
		: CEncConverterConfig
			(
				clpszPyScriptProgID,
				_T("Python Script"),    // must match with what's in PyScriptEncConverter.rgs
				_T("Python Script Plug-in About box.htm"),
				ProcessTypeFlags_PythonScript   // defining process type
			)
	{
	}


DECLARE_REGISTRY_RESOURCEID(IDR_PYSCRIPT_CONFIG)

BEGIN_COM_MAP(CPyScriptEncConverterConfig)
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
	STDMETHOD(Configure)(IEncConverters* pECs, BSTR strFriendlyName, ConvType eConversionType, BSTR strLhsEncodingID, BSTR strRhsEncodingID, VARIANT_BOOL *bRet)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		*bRet = 0;   // false (pessimistic)

		// create *our* config (i.e. 'Setup') dialog, which get's passed to the base class implementation
		//  (which passes it to the common PropertySheet class).
		CPyScriptAutoConfigDlg dlgConfig
			(
				pECs,
				strFriendlyName,
				m_strConverterID,   // may be available if editing an existing configurator
				eConversionType,
				strLhsEncodingID,
				strRhsEncodingID,
				m_lProcessType,
				m_bIsInRepository
			);

		// call base class implementation to do all the work (since it's the same for everyone)
		if( CEncConverterConfig::Configure(&dlgConfig) )
			*bRet = -1; // TRUE

		return S_OK;
	};

	STDMETHOD(DisplayTestPage)(IEncConverters* pECs, BSTR strFriendlyName, BSTR strConverterIdentifier, ConvType eConversionType, BSTR strTestData)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());

		// most of the input parameters are optional, so initialize from This if so.
		InitializeFromThis(&strFriendlyName,&strConverterIdentifier,eConversionType,&strTestData);

		// create *our* config (i.e. 'Setup') dialog, which get's passed to the common PropertySheet class.
		CPyScriptAutoConfigDlg dlgConfig
			(
				pECs,
				strFriendlyName,
				strConverterIdentifier,
				eConversionType
			);

		// call base class implementation to do all the work (since it's the same for everyone)
		CEncConverterConfig::DisplayTestPageEx(&dlgConfig, strTestData);

		return S_OK;
	}

protected:
	virtual HRESULT Error(UINT nID, const IID& iid, HRESULT hRes)
	{
		return PyScriptConfigComCoClass::Error(nID, iid, hRes);
	}
	virtual HRESULT Error(LPCTSTR lpszDesc, const IID& iid, HRESULT hRes)
	{
		return PyScriptConfigComCoClass::Error(lpszDesc, iid, hRes);
	}
};

OBJECT_ENTRY_AUTO(__uuidof(PyScriptEncConverterConfig), CPyScriptEncConverterConfig)
