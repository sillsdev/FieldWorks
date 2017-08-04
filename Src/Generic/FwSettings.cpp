/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 2006-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: FwSettings.cpp
Responsibility: Steve McConnel
Last reviewed:

	This file contains Utility methods. FwSettings used to be in AppCore.
	It contains class declarations for the following classes and structs:
		class FwSettings
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop

//:>********************************************************************************************
//:>	FwSettings methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Set the root of the subkey. This must be called before any of the other methods.

	@param pszRoot
----------------------------------------------------------------------------------------------*/
void FwSettings::SetRoot(const achar * pszRoot)
{
	AssertPsz(pszRoot);
	m_strRoot.Format(_T("Software\\SIL\\Fieldworks\\%s"), pszRoot);
}

/*----------------------------------------------------------------------------------------------
	Get the root of the subkey.

	@return the root of the subkey
----------------------------------------------------------------------------------------------*/
StrApp FwSettings::GetRoot()
{
	return m_strRoot;
}


/*----------------------------------------------------------------------------------------------
	Remove all the registry settings stored for this key.
----------------------------------------------------------------------------------------------*/
void FwSettings::RemoveAll()
{
	Assert(m_strRoot.Length()); // Make sure SetRoot was called at one point.
#ifdef WIN32
	DeleteSubKey(HKEY_CURRENT_USER, m_strRoot.Chars());
#else
	// TODO-Linux: port
#endif
}

/*----------------------------------------------------------------------------------------------
	Open a key and return a handle to it. pszSubKey can be NULL if we want a handle to the
	main key.

	@param pszSubKey
	@param at
	@param phkey

	@return true or false
----------------------------------------------------------------------------------------------*/
bool FwSettings::OpenKey(const achar * pszSubKey, int at, HKEY * phkey)
{
#ifdef WIN32
	AssertPszN(pszSubKey);
	AssertPtr(phkey);
	Assert(m_strRoot.Length()); // Make sure SetRoot was called at one point.

	return OpenKey(m_strRoot, pszSubKey, at, phkey);
#else
	// TODO-Linux: port
	return false;
#endif
}

/*----------------------------------------------------------------------------------------------
	Retrieve a nul-terminated string.

	@param pszSubKey Name of the Subkey (or NULL)
	@param pszValue Name of string the Value wanted
	@param str output string value

	@return true if successful, false otherwise
----------------------------------------------------------------------------------------------*/
bool FwSettings::GetString(const achar * pszSubKey, const achar * pszValue, StrApp & str)
{
#ifdef WIN32
	AssertPszN(pszSubKey);
	AssertPtr(pszValue);
	Assert(m_strRoot.Length()); // Make sure SetRoot was called at one point.

	return GetString(m_strRoot, pszSubKey, pszValue, str);
#else
	// TODO-Linux: port
	return false;
#endif
}

/*----------------------------------------------------------------------------------------------
	Store a null-terminated string.

	@param pszSubKey
	@param pszValue
	@param str

	@return
----------------------------------------------------------------------------------------------*/
bool FwSettings::SetString(const achar * pszSubKey, const achar * pszValue, StrApp & str)
{
#ifdef WIN32
	AssertPszN(pszSubKey);
	AssertPsz(pszValue);
	Assert(m_strRoot.Length()); // Make sure SetRoot was called at one point.

	return SetString(m_strRoot, pszSubKey, pszValue, str);
#else
	// TODO-Linux: port
	return false;
#endif
}

/*----------------------------------------------------------------------------------------------
	Retrieve a dword value.

	@param pszSubKey
	@param pszValue
	@param pdwT

	@return
----------------------------------------------------------------------------------------------*/
bool FwSettings::GetDword(const achar * pszSubKey, const achar * pszValue, DWORD * pdwT)
{
	AssertPszN(pszSubKey);
	AssertPsz(pszValue);
	AssertPtr(pdwT);

#ifdef WIN32
	RegKey hkey;
	if (OpenKey(pszSubKey, katRead, &hkey))
	{
		DWORD cb = isizeof(DWORD);
		DWORD dwT;
		if (::RegQueryValueEx(hkey, pszValue, NULL, &dwT, (BYTE *)pdwT, &cb) == ERROR_SUCCESS)
		{
			Assert(dwT == REG_DWORD);
			return true;
		}
	}
#else
	// TODO-Linux: port
#endif
	return false;
}

/*----------------------------------------------------------------------------------------------
	Store a dword value.

	@param pszSubKey
	@param pszValue
	@param dwT

	@return
----------------------------------------------------------------------------------------------*/
bool FwSettings::SetDword(const achar * pszSubKey, const achar * pszValue, DWORD dwT)
{
	AssertPszN(pszSubKey);
	AssertPsz(pszValue);

#ifdef WIN32
	RegKey hkey;
	if (OpenKey(pszSubKey, katWrite, &hkey))
	{
		return ::RegSetValueEx(hkey, pszValue, 0, REG_DWORD, (BYTE *)&dwT,
			sizeof(DWORD)) == ERROR_SUCCESS;
	}
#else
	// TODO-Linux: port
#endif
	return false;
}

/*----------------------------------------------------------------------------------------------
	Retrieve a binary value.

	@param pszSubKey
	@param pszValue
	@param pv
	@param cb

	@return
----------------------------------------------------------------------------------------------*/
bool FwSettings::GetBinary(const achar * pszSubKey, const achar * pszValue, BYTE * pv, DWORD cb)
{
	AssertPszN(pszSubKey);
	AssertPsz(pszValue);
	AssertArray(pv, cb);

#ifdef WIN32
	RegKey hkey;
	if (OpenKey(pszSubKey, katRead, &hkey))
	{
		DWORD dwT;
		DWORD cbT = cb;
		if (::RegQueryValueEx(hkey, pszValue, NULL, &dwT, pv, &cbT) == ERROR_SUCCESS)
		{
			Assert(cbT <= cb);
			Assert(dwT == REG_BINARY);
			return true;
		}
	}
#else
	// TODO-Linux: port
#endif
	return false;
}


/*----------------------------------------------------------------------------------------------
	Store a binary value (i.e. a structure of some sort).
	WARNING: The structure must not contain any pointers because the memory address will only
		be written to the registry, not the actual memory contents.
	Only use this if you are sure of what you are doing.

	@param pszSubKey
	@param pszValue
	@param pv
	@param cb

	@return
----------------------------------------------------------------------------------------------*/
bool FwSettings::SetBinary(const achar * pszSubKey, const achar * pszValue, BYTE * pv, DWORD cb)
{
#ifdef WIN32
	AssertPszN(pszSubKey);
	AssertPsz(pszValue);
	AssertArray(pv, cb);

	RegKey hkey;
	if (OpenKey(pszSubKey, katWrite, &hkey))
		return ::RegSetValueEx(hkey, pszValue, 0, REG_BINARY, pv, cb) == ERROR_SUCCESS;
#else
	// TODO-Linux: port
#endif
	return false;
}

/***********************************************************************************************
	Static methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Open a key under HKCU and return a handle to it. pszSubKey can be NULL if we want a handle
	to the main key.

	@param pszRootKey The RootKey of the key (i.e. "software\Microsoft\Windows")
	@param pszSubKey  The subkey
	@param accessType Desired access (read/write)
	@param phkey	  On return handle to the registry key

	@return true or false
----------------------------------------------------------------------------------------------*/
bool FwSettings::OpenKey(const achar * pszRootKey, const achar * pszSubKey,
						 int accessType, HKEY * phkey)
{
	AssertPszN(pszSubKey);
	AssertPtr(phkey);
	AssertPtr(pszRootKey);
#ifdef WIN32
	StrApp str(pszRootKey);
	if (pszSubKey)
		str.FormatAppend(_T("\\%s"), pszSubKey);
	if (accessType == katRead)
		return ::RegOpenKeyEx(HKEY_CURRENT_USER, str.Chars(), 0, accessType, phkey) == ERROR_SUCCESS;
	return ::RegCreateKeyEx(HKEY_CURRENT_USER, str.Chars(), 0, NULL, 0, accessType, NULL, phkey,
		NULL) == ERROR_SUCCESS;
#else
	// TODO-Linux: port
	return false;
#endif
}

/*----------------------------------------------------------------------------------------------
	Store a null-terminated string.

	@param pszRootKey The RootKey of the key (i.e. "\software\Microsoft\Windows") NOTE: this
					  needs the slash in front.
	@param pszSubKey  Name of the Subkey (or NULL)
	@param pszValue   Name of the Value (or NULL)
	@param str        String value

	@return true if success
----------------------------------------------------------------------------------------------*/
bool FwSettings::SetString(const achar * pszRootKey, const achar * pszSubKey,
						   const achar * pszValue, StrApp & str)
{
	AssertPsz(pszRootKey);
	AssertPszN(pszSubKey);
	AssertPszN(pszValue);
#ifdef WIN32
	RegKey hkey;
	if (OpenKey(pszRootKey, pszSubKey, katWrite, &hkey))
	{
		return ::RegSetValueEx(hkey, pszValue, 0, REG_SZ, (BYTE *)str.Chars(),
			(str.Length() + 1) * isizeof(achar)) == ERROR_SUCCESS;
	}
#else
	// TODO-Linux: port
#endif
	return false;
}

/*----------------------------------------------------------------------------------------------
	Retrieve a nul-terminated string.

	@param pszRootKey The RootKey of the key (i.e. "\software\Microsoft\Windows") NOTE: this
					  needs the slash in front.
	@param pszSubKey Name of the Subkey (or NULL)
	@param pszValue Name of string the Value wanted (or NULL)
	@param str output string value

	@return true if successful, false otherwise
----------------------------------------------------------------------------------------------*/
bool FwSettings::GetString(const achar * pszRootKey, const achar * pszSubKey,
						   const achar * pszValue, StrApp & str)
{
	AssertPsz(pszRootKey);
	AssertPszN(pszSubKey);
	AssertPszN(pszValue);
#ifdef WIN32
	RegKey hkey;
	if (OpenKey(pszRootKey, pszSubKey, katRead, &hkey))
	{
		achar rgch[MAX_PATH];
		DWORD cb = isizeof(rgch);
		DWORD dwT = 0;
		LONG nRet = ::RegQueryValueEx(hkey, pszValue, NULL, &dwT, (BYTE *)rgch, &cb);
		if (nRet == ERROR_SUCCESS)
		{
			Assert(dwT == REG_SZ || dwT == REG_EXPAND_SZ);
			str = rgch;
			return true;
		}
		else if (nRet == ERROR_MORE_DATA)
		{
			Vector<achar> vch;
			vch.Resize((cb / isizeof(achar)) + 1);
			cb = vch.Size() * isizeof(achar);
			nRet = ::RegQueryValueEx(hkey, pszValue, NULL, &dwT, (BYTE *)vch.Begin(), &cb);
			if (nRet == ERROR_SUCCESS)
			{
				Assert(dwT == REG_SZ || dwT == REG_EXPAND_SZ);
				str = vch.Begin();
				return true;
			}
		}
	}
#else
	// TODO-Linux: port
#endif
	return false;
}

/*----------------------------------------------------------------------------------------------
	Store a dword value in the specified root and sub key.

	@param pszRootKey
	@param pszSubKey
	@param pszValue
	@param dwT

	@return
----------------------------------------------------------------------------------------------*/
bool FwSettings::SetDword(const achar * pszRootKey, const achar * pszSubKey, const achar * pszValue,
						  DWORD dwT)
{
	AssertPsz(pszRootKey);
	AssertPszN(pszSubKey);
	AssertPsz(pszValue);
#ifdef WIN32
	RegKey hkey;
	if (OpenKey(pszRootKey, pszSubKey, katWrite, &hkey))
	{
		return ::RegSetValueEx(hkey, pszValue, 0, REG_DWORD, (BYTE *)&dwT,
			sizeof(DWORD)) == ERROR_SUCCESS;
	}
#else
	// TODO-Linux: port
#endif
	return false;
}

/*----------------------------------------------------------------------------------------------
	Retrieve a dword value from the specified root and sub key.

	@param pszRootKey
	@param pszSubKey
	@param pszValue
	@param pdwT

	@return
----------------------------------------------------------------------------------------------*/
bool FwSettings::GetDword(const achar * pszRootKey, const achar * pszSubKey, const achar * pszValue,
						  DWORD * pdwT)
{
	AssertPsz(pszRootKey);
	AssertPszN(pszSubKey);
	AssertPsz(pszValue);
	AssertPtr(pdwT);
#ifdef WIN32
	// NOTE: RegKey calls ::RegCloseKey in the d'tor
	RegKey hkey;
	if (OpenKey(pszRootKey, pszSubKey, katRead, &hkey))
	{
		DWORD cb = isizeof(DWORD);
		DWORD dwT;
		if (::RegQueryValueEx(hkey, pszValue, NULL, &dwT, (BYTE *)pdwT, &cb) == ERROR_SUCCESS)
		{
			Assert(dwT == REG_DWORD);
			return true;
		}
	}
#else
	// TODO-Linux: port
#endif
	return false;
}

/*----------------------------------------------------------------------------------------------
	Store a bool value in the specified root and sub key.

	@param pszRootKey
	@param pszSubKey
	@param pszValue
	@param fValue

	@return true if successful
----------------------------------------------------------------------------------------------*/
bool FwSettings::SetBool(const achar * pszRootKey, const achar * pszSubKey, const achar * pszValue,
						  bool fValue)
{
#ifdef WIN32
	AssertPsz(pszRootKey);
	AssertPszN(pszSubKey);
	AssertPsz(pszValue);

	StrApp value(fValue ? "true" : "false");
	return FwSettings::SetString(pszRootKey, pszSubKey, pszValue, value);
#else
	// TODO-Linux: port
	return false;
#endif
}

/*----------------------------------------------------------------------------------------------
	Retrieve a bool value from the specified root and sub key.

	@param pszRootKey
	@param pszSubKey
	@param pszValue
	@param pfValue

	@return true if successful
----------------------------------------------------------------------------------------------*/
bool FwSettings::GetBool(const achar * pszRootKey, const achar * pszSubKey, const achar * pszValue,
						  bool * pfValue)
{
	AssertPsz(pszRootKey);
	AssertPszN(pszSubKey);
	AssertPsz(pszValue);
	AssertPtr(pfValue);

	StrApp str;
	if (!FwSettings::GetString(pszRootKey, pszSubKey, pszValue, str))
		return false;

	str.ToLower();
	if (str == _T("true") || str == _T("on") || str == _T("1") )
		*pfValue = true;
	else if (str == _T("false") || str == _T("off") || str == _T("0"))
		*pfValue = false;
	return true;
}
