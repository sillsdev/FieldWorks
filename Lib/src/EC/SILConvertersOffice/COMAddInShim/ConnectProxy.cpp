// ConnectProxy.cpp : Implementation of CConnectProxy

#include "stdafx.h"
#include "ConnectProxy.h"
#include "CLRLoader.h"
#include "ShimConfig.h"

using namespace ShimConfig;
using namespace AddInDesignerObjects;

// CConnectProxy

HRESULT CConnectProxy::FinalConstruct()
{
	HRESULT hr = S_OK;

	// Create an instance of a managed addin
	// and fetch the interface pointer
	CCLRLoader *pCLRLoader = CCLRLoader::TheInstance();
	IfNullGo(pCLRLoader);
	IfFailGo(pCLRLoader->CreateInstance(AssemblyName(), ConnectClassName(),
										__uuidof(IDTExtensibility2),
										(void **)&m_pConnect));

Error:
	return hr;
}

void CConnectProxy::FinalRelease()
{
	if (m_pConnect)
		m_pConnect->Release();
}