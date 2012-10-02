// SILGraphiteControl.cpp : Implementation of CSILGraphiteControlApp and DLL registration.

#include "stdafx.h"
#include "SILGraphiteControl.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


CSILGraphiteControlApp NEAR theApp;
#ifdef _DEBUG
static CMemoryState msOld,msNew,diffMemState;
#endif
const GUID CDECL BASED_CODE _tlid =
		{ 0xDBA69D4C, 0xA68F, 0x4156, { 0xA4, 0x11, 0x47, 0x91, 0x1C, 0xEA, 0x62, 0x75 } };
const WORD _wVerMajor = 1;
const WORD _wVerMinor = 0;



// CSILGraphiteControlApp::InitInstance - DLL initialization

BOOL CSILGraphiteControlApp::InitInstance()
{
	BOOL bInit = COleControlModule::InitInstance();

	if (bInit)
	{
		// TODO: Add your own module initialization code here.
		#ifdef _DEBUG
		afxMemDF = allocMemDF | checkAlwaysMemDF;
		AfxEnableMemoryTracking(TRUE);
		msOld.Checkpoint();
		//_CrtSetBreakAlloc(68);
		#endif
	}

	return bInit;
}



// CSILGraphiteControlApp::ExitInstance - DLL termination

int CSILGraphiteControlApp::ExitInstance()
{
	// TODO: Add your own module termination code here.
#ifdef _DEBUG
	msNew.Checkpoint();
  if(diffMemState.Difference(msOld,msNew))
  {
	TRACE("Memory difference between 2 check points \n");
	diffMemState.DumpStatistics();
	//diffMemState.DumpAllObjectsSince();
  }
#endif

	return COleControlModule::ExitInstance();
}



// DllRegisterServer - Adds entries to the system registry

STDAPI DllRegisterServer(void)
{
	AFX_MANAGE_STATE(_afxModuleAddrThis);

	if (!AfxOleRegisterTypeLib(AfxGetInstanceHandle(), _tlid))
		return ResultFromScode(SELFREG_E_TYPELIB);

	if (!COleObjectFactoryEx::UpdateRegistryAll(TRUE))
		return ResultFromScode(SELFREG_E_CLASS);

	return NOERROR;
}



// DllUnregisterServer - Removes entries from the system registry

STDAPI DllUnregisterServer(void)
{
	AFX_MANAGE_STATE(_afxModuleAddrThis);

	if (!AfxOleUnregisterTypeLib(_tlid, _wVerMajor, _wVerMinor))
		return ResultFromScode(SELFREG_E_TYPELIB);

	if (!COleObjectFactoryEx::UpdateRegistryAll(FALSE))
		return ResultFromScode(SELFREG_E_CLASS);

	return NOERROR;
}
