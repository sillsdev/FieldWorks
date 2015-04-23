/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GdlObject.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Contains definitions of simple abstract superclasses needed for parser and compiler.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GDLOBJECT_INCLUDED
#define GDLOBJECT_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: GdlObject
Description: General abstract superclass subsuming classes of objects corresponding to
	structures in the GDL file, specifically those that are created by the parser.
Hungarian: gdl
----------------------------------------------------------------------------------------------*/
class GdlObject
{
public:
	GdlObject()
	{
		m_lnf = GrpLineAndFile(0, 0, "");
	}
	GdlObject(const GdlObject & gdl)
	{
		m_lnf = gdl.m_lnf;
	}

	void SetLineAndFile(GrpLineAndFile const& lnf)
	{
		m_lnf = lnf;
	}
	void CopyLineAndFile(const GdlObject & gdl)
	{
		m_lnf = gdl.m_lnf;
	}

	GrpLineAndFile & LineAndFile()
	{
		return m_lnf;
	}

	bool LineIsZero()
	{
		return m_lnf.PreProcessedLine() == 0;
	}

protected:
	//	instance variables:
	GrpLineAndFile m_lnf;
};


/*----------------------------------------------------------------------------------------------
Class: GdlDefn
Description: Abstract class subsuming GdlGlyphClassDefn and GdlFeatureDefn.
Hungarian: defn
----------------------------------------------------------------------------------------------*/
class GdlDefn : public GdlObject
{
public:
	virtual ~GdlDefn()
	{
	}
};


#endif // !GDLOBJECT_INCLUDED
