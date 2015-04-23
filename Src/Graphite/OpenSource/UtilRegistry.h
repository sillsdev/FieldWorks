/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

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
