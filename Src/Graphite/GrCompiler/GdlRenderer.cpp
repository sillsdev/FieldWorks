/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GdlRenderer.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implementation of the top-level GDL thing.
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


/***********************************************************************************************
	Methods: General
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
GdlRenderer::GdlRenderer()
{
	m_fAutoPseudo = true;
	m_fBidi = true;
	m_pexpXAscent = NULL;
	m_pexpXDescent = NULL;
	m_grfsdc = kfsdcNone;
	m_cnUserDefn = 0;
	m_cnComponents = 0;
}


/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
GdlRenderer::~GdlRenderer()
{
	//	Delete all the GdlGlyphDefns (simple glyphs) stored in the classes,
	//	then delete all the classes at the top level.
	int i;
	for (i = 0; i < m_vpglfc.Size(); ++i)
		m_vpglfc[i]->DeleteGlyphDefns();
	for (i = 0; i < m_vpglfc.Size(); ++i)
		delete m_vpglfc[i];

	for (i = 0; i < m_vprultbl.Size(); ++i)
		delete m_vprultbl[i];

	for (i = 0; i < m_vpfeat.Size(); ++i)
		delete m_vpfeat[i];

	for (i = 0; i < m_vplang.Size(); ++i)
		delete m_vplang[i];

	for (NameDefnMap::iterator itmap = m_hmNameDefns.Begin();
		itmap != m_hmNameDefns.End();
		++itmap)
	{
		delete itmap->GetValue();
	}

	if (m_pexpXAscent)
		delete m_pexpXAscent;
	if (m_pexpXDescent)
		delete m_pexpXDescent;
}

/*----------------------------------------------------------------------------------------------
	Add a language to the list. Keep them in sorted order. Return false if the language
	was already present.
----------------------------------------------------------------------------------------------*/
bool GdlRenderer::AddLanguage(GdlLanguageDefn * plang)
{
	int iplangLo = 0;
	int iplangHi = m_vplang.Size();
	while (true)
	{
		int iplangMid = (iplangLo + iplangHi) >> 1; // div by 2
		if (iplangMid >= m_vplang.Size())
		{
			m_vplang.Push(plang);
			return true;
		}

		unsigned int nCodeThis = plang->Code();
		unsigned int nCodeThat = m_vplang[iplangMid]->Code();
		int cmp = strcmp((char*)&nCodeThis, (char*)&nCodeThat);

		if (cmp == 0)
			return false; // already present
		if (iplangHi - iplangLo == 1)
		{
			m_vplang.Insert(iplangLo + ((cmp<0) ? 0 : 1), plang);
			return true;
		}
		if (cmp < 0)
			iplangHi = iplangMid;
		else
			iplangLo = iplangMid;
	}
}


/***********************************************************************************************
	Methods: Parser
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Return the rule table with the given name; create it if it does not exist. Assumes that
	the name is a valid one for tables, but not necessarily for rule tables.
----------------------------------------------------------------------------------------------*/
GdlRuleTable * GdlRenderer::GetRuleTable(GrpLineAndFile & lnf, StrAnsi staTableName)
{
	GdlRuleTable * prultbl = FindRuleTable(staTableName);

	if (prultbl)
		return prultbl;

	//	Create a new one, if permissible.

	Symbol psymTableName = g_cman.SymbolTable()->FindSymbol(staTableName);
	Assert(psymTableName);
	if (!psymTableName || !psymTableName->FitsSymbolType(ksymtTableRule))
	{
		g_errorList.AddError(2162, NULL,
			"The ",
			staTableName,
			" table cannot hold rules",
			lnf);
		return NULL;
	}

	prultbl = new GdlRuleTable(psymTableName);
	prultbl->SetLineAndFile(lnf);
	m_vprultbl.Push(prultbl);

	return prultbl;
}


/*----------------------------------------------------------------------------------------------
	Return the table with the given name, or NULL if it does not exist.
----------------------------------------------------------------------------------------------*/
GdlRuleTable * GdlRenderer::FindRuleTable(StrAnsi staTableName)
{
	Symbol psymTableName = g_cman.SymbolTable()->FindSymbol(staTableName);
	if (!psymTableName)
		return NULL;

	return FindRuleTable(psymTableName);
}

GdlRuleTable * GdlRenderer::FindRuleTable(Symbol psymTableName)
{
	for (int iprultbl = 0; iprultbl < m_vprultbl.Size(); iprultbl++)
	{
		if (m_vprultbl[iprultbl]->NameSymbol() == psymTableName)
			return m_vprultbl[iprultbl];
	}

	return NULL;
}

/***********************************************************************************************
	Methods: Pre-compiler
***********************************************************************************************/

void GdlRenderer::SetNumUserDefn(int c)
{
	m_cnUserDefn = max(m_cnUserDefn, c+1);
}

void GdlRenderer::SetNumLigComponents(int c)
{
	m_cnComponents = max(m_cnComponents, c);
}
