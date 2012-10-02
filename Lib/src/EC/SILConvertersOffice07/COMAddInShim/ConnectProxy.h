// ConnectProxy.h : Declaration of the CConnectProxy

#pragma once
#include "resource.h"       // main symbols

#include "COMAddInShim.h"

using namespace Office;

// CConnectProxy

class ATL_NO_VTABLE CConnectProxy :
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CConnectProxy, &CLSID_ConnectProxy>,
	public IDispatchImpl<AddInDesignerObjects::_IDTExtensibility2, &AddInDesignerObjects::IID__IDTExtensibility2, &AddInDesignerObjects::LIBID_AddInDesignerObjects, /* wMajor = */ 1, /* wMinor = */ 0>
{
public:
	CConnectProxy()
		: m_pConnect(NULL)
	{
	}

	DECLARE_REGISTRY_RESOURCEID(IDR_CONNECTPROXY)

	BEGIN_COM_MAP(CConnectProxy)
		COM_INTERFACE_ENTRY2(IDispatch, AddInDesignerObjects::IDTExtensibility2)
		COM_INTERFACE_ENTRY(AddInDesignerObjects::IDTExtensibility2)
		COM_INTERFACE_ENTRY_AGGREGATE(IID_IRibbonExtensibility, m_pConnect)
	END_COM_MAP()


	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct();
	void FinalRelease();

public:
	//IDTExtensibility2 implementation:
	STDMETHOD(OnConnection)(IDispatch * Application, AddInDesignerObjects::ext_ConnectMode ConnectMode, IDispatch *AddInInst, SAFEARRAY **custom)
	{
		return m_pConnect->OnConnection(Application, ConnectMode, AddInInst, custom);
	}
	STDMETHOD(OnDisconnection)(AddInDesignerObjects::ext_DisconnectMode RemoveMode, SAFEARRAY **custom )
	{
		return m_pConnect->OnDisconnection(RemoveMode, custom);
	}
	STDMETHOD(OnAddInsUpdate)(SAFEARRAY **custom )
	{
		return m_pConnect->OnAddInsUpdate(custom);
	}
	STDMETHOD(OnStartupComplete)(SAFEARRAY **custom )
	{
		return m_pConnect->OnStartupComplete(custom);
	}
	STDMETHOD(OnBeginShutdown)(SAFEARRAY **custom )
	{
		return m_pConnect->OnBeginShutdown(custom);
	}

protected:
	// caches pointer to managed add-in
	AddInDesignerObjects::IDTExtensibility2 *m_pConnect;
};

OBJECT_ENTRY_AUTO(__uuidof(ConnectProxy), CConnectProxy)
