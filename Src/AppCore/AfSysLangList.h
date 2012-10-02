/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfSysLangList.h
Responsibility: Steve McConnel
Last reviewed: never

Description:
	Define a class that manages a list of languages (locales) known by the operating system.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef AFSYSLANGLIST_H_INCLUDED
#define AFSYSLANGLIST_H_INCLUDED 1
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Data structure containing the information of interest for one language (locale) known by the
	operating system.

	Hungarian: sli
----------------------------------------------------------------------------------------------*/
struct SysLangInfo
{
	int m_lcid;
	StrApp m_strName;
};

/*----------------------------------------------------------------------------------------------
	Class to provide a list of languages (locales) supported by the operating system.

	Hungarian: sll
----------------------------------------------------------------------------------------------*/
class AfSystemLanguageList
{
public:
	// Constructor.
	AfSystemLanguageList()
	{
		m_hmenuLang = NULL;
	}
	// Destructor.
	~AfSystemLanguageList()
	{
		if (m_hmenuLang)
		{
			::DestroyMenu(m_hmenuLang);
			m_hmenuLang = NULL;
		}
	}

	HMENU GetLanguageMenu();

	//:> Static Methods.

	/*------------------------------------------------------------------------------------------
		Return the vector of language information.
	------------------------------------------------------------------------------------------*/
	static Vector<SysLangInfo> & GetLanguages()
	{
		Initialize();
		return g_vsli;
	}

	/*------------------------------------------------------------------------------------------
		Get the index value for the given language (locale) id.

		@param lcid The locale number for the language of interest.

		@return The appropriate internal index value, or -1 if the language id is not found.
	------------------------------------------------------------------------------------------*/
	static int GetLangInfoIndex(int lcid)
	{
		Initialize();
		int isli;
		if (g_hmlcidisli.Retrieve(lcid, &isli))
			return isli;
		else
			return -1;
	}

	/*------------------------------------------------------------------------------------------
		Return the name of the language for the given locale id, or NULL if the locale id is not
		valid.

		@param lcid The locale number for the language of interest.
	------------------------------------------------------------------------------------------*/
	static const achar * GetLanguageName(int lcid)
	{
		int isli = GetLangInfoIndex(lcid);
		if (isli < 0)
			return NULL;
		else
			return g_vsli[isli].m_strName.Chars();
	}

	/*------------------------------------------------------------------------------------------
		Release all memory used for storing language/locale information.
	------------------------------------------------------------------------------------------*/
	static void Clear()
	{
		g_vsli.Clear();
		g_hmlcidisli.Clear();
	}
protected:
	HMENU m_hmenuLang;

	//:> Static Methods.
	static void Initialize();
	//:> Static data.
	static Vector<SysLangInfo> g_vsli;
	static HashMap <int,int> g_hmlcidisli;
};

// Local Variables:
// mode:C++
// End: (These 3 lines are useful to Steve McConnel.)

#endif /*AFSYSLANGLIST_H_INCLUDED*/
