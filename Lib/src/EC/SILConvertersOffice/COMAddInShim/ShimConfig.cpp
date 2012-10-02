#include "stdafx.h"
#include "ShimConfig.h"

// Assembly specific values
// TODO: modify the values to reflect those in your project
// assembly strong name
// To Modify: assembly strong name and public key token
static LPCWSTR szAddInAssemblyName = L"SILConvertersOffice, PublicKeyToken=f1447bae1e63f485";
// To Modify: full name of connect class
static LPCWSTR szConnectClassName = L"SILConvertersOffice.Connect";

// Functions that return the above values

LPCWSTR ShimConfig::AssemblyName()
{
	return szAddInAssemblyName;
}

LPCWSTR ShimConfig::ConnectClassName()
{
	return szConnectClassName;
}
