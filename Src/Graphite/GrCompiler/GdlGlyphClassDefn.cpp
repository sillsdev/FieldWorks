/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GdlGlyphClass.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implements definitions of classes of glyphs..
-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Forward declarations
***********************************************************************************************/

/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/

//	None


/***********************************************************************************************
	Methods: Destruction
***********************************************************************************************/

GdlGlyphClassDefn::~GdlGlyphClassDefn()
{
	for (int i = 0; i < m_vpglfaAttrs.Size(); ++i)
		delete m_vpglfaAttrs[i];
}


void GdlGlyphClassDefn::DeleteGlyphDefns()
{
	for (int i = 0; i < m_vpglfdMembers.Size(); ++i)
	{
		if (dynamic_cast<GdlGlyphDefn * >(m_vpglfdMembers[i]))
			delete m_vpglfdMembers[i];
	}

}


/***********************************************************************************************
	Methods: Post-parser
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Add a simple glyph to the class.
----------------------------------------------------------------------------------------------*/
GdlGlyphClassMember * GdlGlyphClassDefn::AddGlyphToClass(GrpLineAndFile const& lnf,
	GlyphType glft, int nFirst)
{
	GdlGlyphDefn * pglf = new GdlGlyphDefn(glft, nFirst);
	pglf->SetLineAndFile(lnf);
	AddMember(pglf);
	return pglf;
}

GdlGlyphClassMember * GdlGlyphClassDefn::AddGlyphToClass(GrpLineAndFile const& lnf,
	GlyphType glft, int nFirst, int nLast)
{
	GdlGlyphDefn * pglf = new GdlGlyphDefn(glft, nFirst, nLast);
	pglf->SetLineAndFile(lnf);
	AddMember(pglf);
	return pglf;
}

GdlGlyphClassMember * GdlGlyphClassDefn::AddGlyphToClass(GrpLineAndFile const& lnf,
	GlyphType glft, int nFirst, int nLast, utf16 wCodePage)
{
	GdlGlyphDefn * pglf = new GdlGlyphDefn(glft, nFirst, nLast, wCodePage);
	pglf->SetLineAndFile(lnf);
	AddMember(pglf);
	return pglf;
}

GdlGlyphClassMember * GdlGlyphClassDefn::AddGlyphToClass(GrpLineAndFile const& lnf,
	GlyphType glft, StrAnsi staPostscript)
{
	GdlGlyphDefn * pglf = new GdlGlyphDefn(glft, staPostscript);
	pglf->SetLineAndFile(lnf);
	AddMember(pglf);
	return pglf;
}

GdlGlyphClassMember * GdlGlyphClassDefn::AddGlyphToClass(GrpLineAndFile const& lnf,
	GlyphType glft, StrAnsi staCodepoints, utf16 wCodePage)
{
	GdlGlyphDefn * pglf = new GdlGlyphDefn(glft, staCodepoints, wCodePage);
	pglf->SetLineAndFile(lnf);
	AddMember(pglf);
	return pglf;
}

GdlGlyphClassMember * GdlGlyphClassDefn::AddGlyphToClass(GrpLineAndFile const& lnf,
	GlyphType glft, GdlGlyphDefn * pglfOutput, utf16 wPseudoInput)
{
	GdlGlyphDefn * pglf = new GdlGlyphDefn(glft, pglfOutput, (int)wPseudoInput);
	pglf->SetLineAndFile(lnf);
	AddMember(pglf);
	return pglf;
}

GdlGlyphClassMember * GdlGlyphClassDefn::AddGlyphToClass(GrpLineAndFile const& lnf,
	GlyphType glft, GdlGlyphDefn * pglfOutput)
{
	GdlGlyphDefn * pglf = new GdlGlyphDefn(glft, pglfOutput);
	pglf->SetLineAndFile(lnf);
	AddMember(pglf);
	return pglf;
}

GdlGlyphClassMember * GdlGlyphClassDefn::AddClassToClass(GrpLineAndFile const& lnf,
	GdlGlyphClassDefn * pglfcMember)
{
	AddMember(pglfcMember);
	return pglfcMember;
}


/*----------------------------------------------------------------------------------------------
	Add a member to the class.
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::AddMember(GdlGlyphClassMember * pglfd)
{
	m_vpglfdMembers.Push(pglfd);
}


/*----------------------------------------------------------------------------------------------
	Add a user-defined glyph attribute to the list. Note that we shouldn't have to check
	for duplicates, due to the way the master glyph attribute list is managed (see
	GrcMasterTable::AddItem(...)).
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::AddGlyphAttr(Symbol psym, GdlAssignment * pasgn)
{
	GdlGlyphAttrSetting * pglfa = new GdlGlyphAttrSetting(psym, pasgn);
	pglfa->SetLineAndFile(pasgn->LineAndFile());
	m_vpglfaAttrs.Push(pglfa);
}


/*----------------------------------------------------------------------------------------------
	Add an attribute of the form component.<compname>.<corner>.  Note that we shouldn't have
	to check for duplicates, due to the way the master glyph attribute list is managed
	(see GrcMasterTable::AddItem(...)).
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::AddComponent(Symbol psym, GdlAssignment * pasgn)
{
	AddGlyphAttr(psym, pasgn);

//	Add this component to the list.
//	StrAnsi staCompName = psym->FieldAt(2);
//	m_vstaComponentNames.Push(staCompName);

//	GdlGlyphAttrSetting * pglfa = new RldGlyphAttrSetting(psym, pasgn);
//	m_vglfaComponents.Push(pglfa);
}


/*----------------------------------------------------------------------------------------------
	Return true if the given glyph is a member of the class.
----------------------------------------------------------------------------------------------*/
bool GdlGlyphClassDefn::IncludesGlyph(utf16 w)
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
	{
		if (m_vpglfdMembers[iglfd]->IncludesGlyph(w))
			return true;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Return true if the class include a bad glyph definition
----------------------------------------------------------------------------------------------*/
bool GdlGlyphClassDefn::HasBadGlyph()
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
	{
		if (m_vpglfdMembers[iglfd]->HasBadGlyph())
			return true;
	}
	return false;
}


#if 0

/*----------------------------------------------------------------------------------------------
	Set the directionality attribute.
----------------------------------------------------------------------------------------------*/

void GdlGlyphClassDefn::SetDirection(GdlExpression * pglfaDir, int nStmtNo, bool fAttrOverride)
{
	if (m_pglfaDirection == NULL ||
		(fAttrOverride && nStmtNo > m_pglfaDirection->LineNumber()))
	{
		m_pexpDirection = pglfaDir;
	}
}


/*----------------------------------------------------------------------------------------------
	Set the breakweight attribute.
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::SetBreakweight(GdlExpression * pexpBw, int nStmtNo, bool fAttrOverride)
{
	if (m_pexpBreakweight == NULL || (fAttrOverride && nStmtNo > m_nBwStmtNo))
		m_pexpBreakweight = pexpBw;
}

#endif // 0
