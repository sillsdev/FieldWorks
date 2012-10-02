/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilRegistry.h
Responsibility:
Last reviewed:

Description:
	Provides Registry related utilities.
----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef UTILREGISTRY_H
#define UTILREGISTRY_H 1


#ifdef _WIN32
int DeleteSubKey(HKEY hk, const char *psz);


/*************************************************************************************
	A class wrapper for a registry key. The destructor closes the key.
*************************************************************************************/
class RegKey
{
protected:
	HKEY m_hkey;

public:
	RegKey(void) {
		m_hkey = NULL;
	}
	~RegKey(void) {
		Close();
	}

	// Cast operator.
	operator HKEY (void) {
		return m_hkey;
	}

	// Address of operator.
	HKEY * operator&(void) {
		Close();
		return &m_hkey;
	}

	void Close(void) {
		if (m_hkey) {
			RegCloseKey(m_hkey);
			m_hkey = NULL;
		}
	}
};

#endif // _WIN32

#endif //!UTILREGISTRY_H
