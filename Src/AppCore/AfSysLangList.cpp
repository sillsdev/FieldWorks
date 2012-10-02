/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfSysLangList.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Implementation of the class that manages a list of languages (locales) known by the
	operating system.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE
//:End Ignore

//:>********************************************************************************************
//:>	AfSysLangList static variables, methods, and related functions.
//:>********************************************************************************************

Vector<SysLangInfo> AfSystemLanguageList::g_vsli;
HashMap <int,int> AfSystemLanguageList::g_hmlcidisli;

/*----------------------------------------------------------------------------------------------
	Handle an enumeration callback for a call to ::EnumSystemLocales.

	@param pszLocale Locale Id number encoded as hexadecimal character string.

	@return TRUE (forces complete enumeration)
----------------------------------------------------------------------------------------------*/
static Vector<int> g_vlcid;
static BOOL CALLBACK EnumInstalledLocales(achar * pszLocale)
{
	StrAnsi sta(pszLocale);
	int lcid = (int)strtoul(sta.Chars(), NULL, 16);
	g_vlcid.Push(lcid);
	return TRUE;
}

/*----------------------------------------------------------------------------------------------
	Get the list of installed locales (language ids) from the system.  The results are cached
	in static class variables.
----------------------------------------------------------------------------------------------*/
void AfSystemLanguageList::Initialize()
{
	if (!g_vsli.Size())
	{
		::EnumSystemLocales(EnumInstalledLocales, LCID_INSTALLED);
		achar rgchBuf[kcchMaxBufBig];
		int cch;
		SysLangInfo sli;
		int i;
		for (i = 0; i < g_vlcid.Size(); ++i)
		{
			sli.m_lcid = g_vlcid[i];
			cch = ::GetLocaleInfo(sli.m_lcid, LOCALE_SLANGUAGE, rgchBuf, isizeof(rgchBuf));
			if (cch)
			{
				sli.m_strName.Assign(rgchBuf);	// Note: cch includes the terminating NUL.
				// Get the position of this locale/language in the sorted list.
				int iv, ivLim;
				for (iv = 0, ivLim = g_vsli.Size(); iv < ivLim; )
				{
					int ivMid = (iv + ivLim) / 2;
					if (_tcscoll(g_vsli[ivMid].m_strName.Chars(),
						sli.m_strName.Chars()) < 0)
					{
						iv = ivMid + 1;
					}
					else
					{
						ivLim = ivMid;
					}
				}
				g_vsli.Insert(iv, sli);
			}
		}
		for (i = 0; i < g_vsli.Size(); ++i)
			g_hmlcidisli.Insert(g_vsli[i].m_lcid, i);
	}
}

/*----------------------------------------------------------------------------------------------
	Get the hierarchical popup menu listing the languages (locales) installed on this system.
	The menu is created if necessary.
----------------------------------------------------------------------------------------------*/
HMENU AfSystemLanguageList::GetLanguageMenu()
{
	if (!m_hmenuLang)
	{
		// Create the popup menu that allows a user to choose the language.
		m_hmenuLang = ::CreatePopupMenu();
		if (!m_hmenuLang)
			ThrowHr(WarnHr(E_FAIL));
		Vector<SysLangInfo> & vsli = AfSystemLanguageList::GetLanguages();
		int i;
		for (i = 0; i < vsli.Size(); ++i)
		{
			StrApp & str = vsli[i].m_strName;
			int ichSub = str.FindCh('(');
			if (ichSub >= 0)
			{
				StrApp strLang(str.Chars(), ichSub - 1);
				HMENU hmenuSub = ::CreatePopupMenu();
				::AppendMenu(m_hmenuLang, MF_POPUP, (UINT_PTR)hmenuSub, strLang.Chars());
				for (++ichSub; i < vsli.Size(); ++i)
				{
					StrApp & str2 = vsli[i].m_strName;
					if (str2.Left(ichSub).Equals(str.Chars(), ichSub))
					{
						int ichEnd = str2.FindCh(')', ichSub);
						if (ichEnd < 0)
							ichEnd = str2.Length();
						StrApp strSub(str2.Chars() + ichSub, ichEnd - ichSub);
						::AppendMenu(hmenuSub, MF_STRING, kcidMenuItemDynMin + i,
							strSub.Chars());
					}
					else
					{
						--i;
						break;
					}
				}
			}
			else
			{
				::AppendMenu(m_hmenuLang, MF_STRING, kcidMenuItemDynMin + i, str.Chars());
			}
		}
	}
	return m_hmenuLang;
}
