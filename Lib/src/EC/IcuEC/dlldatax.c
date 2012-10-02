// wrapper for dlldata.c

#ifdef _MERGE_PROXYSTUB // merge proxy stub DLL

#define REGISTER_PROXY_DLL //DllRegisterServer, etc.

// For Visual Studio .NET 2003, this must be set to 0x0500 for this to compile.
#define _WIN32_WINNT 0x0500	//for Win2000 or greater
#define USE_STUBLESS_PROXY	//defined only with MIDL switch /Oicf

#pragma comment(lib, "rpcndr.lib")
#pragma comment(lib, "rpcns4.lib")
#pragma comment(lib, "rpcrt4.lib")

#define ENTRY_PREFIX	Prx

// #include "dlldata.c"
#include "IcuEC_p.c"

#endif //_MERGE_PROXYSTUB
