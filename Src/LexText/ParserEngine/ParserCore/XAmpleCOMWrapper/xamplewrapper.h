// XAmpleWrapper.h : Declaration of the CXAmpleWrapper

#pragma once
#include "resource.h"       // main symbols
#include "XAmpleWrapperCore.h"


// IXAmpleWrapper
[
	object,
	uuid("A5E88985-BCB6-47AB-96AA-20F9592BA2C7"),
	dual,	helpstring("IXAmpleWrapper Interface"),
	pointer_default(unique)
]
__interface IXAmpleWrapper : IDispatch
{
	[id(1), helpstring("method Init")] HRESULT Init( [in] BSTR folderContainingXampleDll);
	[id(2), helpstring("method ParseWord")] HRESULT ParseWord([in] BSTR wordform,[out,retval] BSTR* xmlResult);
	[id(3), helpstring("method TraceWord")] HRESULT TraceWord([in] BSTR wordform, [in] BSTR selectedMorphs, [out,retval] BSTR* xmlResult);
	[id(4), helpstring("method LoadFiles")] HRESULT LoadFiles([in] BSTR fixedFilesDir, [in] BSTR dynamicFilesDir, [in] BSTR databaseName);
	[id(5), helpstring("method SetParameter")] HRESULT SetParameter( [in] BSTR name, [in] BSTR value);
	//[id(5), helpstring("method GetErrorString")] HRESULT GetErrorString([out,retval] BSTR* result);
	[id(6), helpstring("method AmpleThreadId")] HRESULT get_AmpleThreadId([out,retval] int * pid);
};



// CXAmpleWrapper

[
	coclass,
	default(IXAmpleWrapper),
	threading(apartment),
	support_error_info("IXAmpleWrapper"),
	aggregatable(never),
	vi_progid("XAmpleCOMWrapper.XAmpleWrapper"),
	progid("XAmpleCOMWrapper.XAmpleWrapper.1"),
	version(1.0),
	uuid("8476E697-197E-43FA-8C0D-FBD15B59E99C"),
	helpstring("XAmpleWrapper Class")
]
class ATL_NO_VTABLE CXAmpleWrapper :
	public IXAmpleWrapper
{
protected:
	CXAmpleDLLWrapper* m_pXample;

public:
	CXAmpleWrapper()

	{
		m_pXample = NULL;
	}


	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
		if(m_pXample != NULL)
			delete m_pXample;
	}

public:

	STDMETHOD(Init)(BSTR bstrFolderContainingXampleDll);
	STDMETHOD(ParseWord)(BSTR bstrWordform, BSTR* pbstrXmlResult);
	STDMETHOD(TraceWord)(BSTR bstrWordform, BSTR bstrSelectedMorphs, BSTR* pbstrXmlResult);
	STDMETHOD(LoadFiles)(BSTR bstrFixedFilesDir, BSTR bstrDynamicFilesDir, BSTR bstrDatabaseName);
	STDMETHOD(SetParameter)(BSTR name, BSTR value);
	//STDMETHOD(GetErrorString)(BSTR* result);
	STDMETHOD(get_AmpleThreadId)(int * pid);
};
