/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GdlNameDefn.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	GdlNameDefn contains collections of informative strings.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef NAMEDEFN_INCLUDED
#define NAMEDEFN_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: GdlNameDefn
Description: Each GdlNameDefn contains a string (or a collection strings in multiple languages)
	giving a piece of information about the renderer (version, author, comment, etc.).
	These are stored in the GdlRenderer, keyed under the numeric identifier for the name.
Hungarian: ndefn
----------------------------------------------------------------------------------------------*/

class GdlNameDefn : public GdlObject
{
public:
	GdlExtName * ExtName(int i)
	{
		return &m_vextname[i];
	}

	void AddExtName(utf16 wLangID, StrUni stu)
	{
		m_vextname.Push(GdlExtName(stu, wLangID));
	}
	void AddExtName(utf16 wLangID, GdlExpression * pexp)
	{
		GdlStringExpression * pexpString = dynamic_cast<GdlStringExpression*>(pexp);
		Assert(pexpString);

		StrUni stu = pexpString->ConvertToUnicode();
		m_vextname.Push(GdlExtName(stu, wLangID));
	}

	void DeleteExtName(int i)
	{
		Assert(i < m_vextname.Size());
		m_vextname.Delete(i);
	}

	int NameCount()
	{
		 return m_vextname.Size();
	}

protected:
	Vector<GdlExtName>	m_vextname;
};


typedef HashMap<int, GdlNameDefn *> NameDefnMap;



#endif // NAMEDEFN_INCLUDED
