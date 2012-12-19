/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

	Allows to redirect HKCU in unit tests when building on Jenkins. This happens only when the
	environment variable BUILDAGENT_SUBKEY is set.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef REDIRECTHKCU_H
#define REDIRECTHKCU_H 1

void RedirectRegistry()
{
	// nothing to do on Linux
#if WIN32
	wchar_t* subkey;
	size_t requiredSize;

	_wgetenv_s(&requiredSize, NULL, 0, L"BUILDAGENT_SUBKEY");
	if (requiredSize == 0)
	{
		// environment variable doesn't exist - nothing to do
		return;
	}

	subkey = (wchar_t*)malloc(requiredSize * sizeof(wchar_t));
	if (!subkey)
	{
		throw "out of memory";
		return;
	}

	if (_wgetenv_s(&requiredSize, subkey, requiredSize, L"BUILDAGENT_SUBKEY") != 0)
	{
		free(subkey);
		return;
	}

	// keep in sync with BasicUtilsTests/Attributes/RedirectHKCU.cs and SetupInclude.targets
	std::wstring fullkey(L"Software\\SIL\\BuildAgents\\");
	fullkey.append(subkey);
	fullkey.append(L"\\HKCU");
	free(subkey);

	HKEY hKey;
	RegCreateKey(HKEY_CURRENT_USER, fullkey.c_str(), &hKey);
	RegOverridePredefKey(HKEY_CURRENT_USER, hKey);
	RegCloseKey(hKey);
#endif
}


#endif
