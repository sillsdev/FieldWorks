/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GdlRule.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implement the rules and their rule items.
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
	Given a slot alias, find the corresponding index in the rule's list. Return -1 if the
	name could not be found.
----------------------------------------------------------------------------------------------*/
int GdlRule::LookupAliasIndex(StrAnsi sta)
{
	for (int i = 0; i < m_vpalias.Size(); ++i)
	{
		if (m_vpalias[i]->m_staName == sta)
			return m_vpalias[i]->m_srIndex;
	}
	return -1;
}

/***********************************************************************************************
	Methods: Parser
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Return the irit'th item in the context; create it if it does not exist.
	If staInput = "_", then the item should be present in the lhs or rhs, so create
	a set-attr item for it.
	Arguments:
		lnf					- line on which the item appears
		irit				- item number (0-based)
		staInput			- input class, "_", or "#"
----------------------------------------------------------------------------------------------*/

GdlRuleItem * GdlRule::ContextItemAt(GrpLineAndFile & lnf, int irit,
	StrAnsi staInput, StrAnsi staAlias)
{
	Assert(irit <= m_vprit.Size());

	if (irit == m_vprit.Size())
	{
		Symbol psymClass = g_cman.SymbolTable()->FindSymbol(staInput);
		GdlRuleItem * prit;
		if (psymClass && psymClass->FitsSymbolType(ksymtSpecialLb))
			prit = new GdlLineBreakItem(psymClass);

		else if (psymClass && psymClass->FitsSymbolType(ksymtSpecialUnderscore))
			// place holder
			prit = new GdlSetAttrItem(psymClass);

		else
		{
			if (!psymClass || !psymClass->FitsSymbolType(ksymtClass))
			{
				g_errorList.AddError(3134, this,
					"Undefined class name: ",
					staInput,
					lnf);
				psymClass = g_cman.SymbolTable()->FindSymbol(GdlGlyphClassDefn::Undefined());
			}
			prit = new GdlRuleItem(psymClass);
		}
		prit->SetLineAndFile(lnf);
		prit->m_iritContextPos = m_vprit.Size();
		prit->m_iritContextPosOrig = prit->m_iritContextPos;
		m_vprit.Push(prit);

		//	record the 1-based slot-alias value, if any
		if (staAlias != "")
			m_vpalias.Push(new GdlAlias(staAlias, prit->m_iritContextPos + 1));
	}

	return m_vprit[irit];
}


/*----------------------------------------------------------------------------------------------
	Handle error sitution where they put a selector in the context (@ or $). nSel == 0 or
	staSel == "" indicates a bare '@'.
----------------------------------------------------------------------------------------------*/
GdlRuleItem * GdlRule::ContextSelectorItemAt(GrpLineAndFile & lnf, int irit,
	StrAnsi staClassOrAt, int nSel, StrAnsi staAlias)
{
	g_errorList.AddError(3135, this,
		"Cannot specify a selector in the context");

	if (staClassOrAt = "@")
		return ContextItemAt(lnf, irit, "_", staAlias);
	else
		return ContextItemAt(lnf, irit, staClassOrAt, staAlias);
}

GdlRuleItem * GdlRule::ContextSelectorItemAt(GrpLineAndFile & lnf, int irit,
	StrAnsi staClassOrAt, StrAnsi staSel, StrAnsi staAlias)
{
	g_errorList.AddError(3136, this,
		"Cannot specify a selector in the context");

	if (staClassOrAt = "@")
		return ContextItemAt(lnf, irit, "_", staAlias);
	else
		return ContextItemAt(lnf, irit, staClassOrAt, staAlias);
}


/*----------------------------------------------------------------------------------------------
	Return the irit'th item in the RHS; create it if it does not exist.
	Assumes that if there was a context, then items have been created for everything in it;
	therefore, needing to create an item when there are context-only items is an error.
	Arguments:
		lnf					- line on which the item appears
		irit				- item number (0-based)
		staInput			- input class name, or "_" or '@'
		staAlias			- string indicating the alias
		fSubItem			- true if we know right off the bat we need a substitition item
----------------------------------------------------------------------------------------------*/
GdlRuleItem * GdlRule::RhsItemAt(GrpLineAndFile & lnf, int irit,
	StrAnsi staInput, StrAnsi staAlias, bool fSubItem)
{
	bool fContext = false;
	int critRhs = 0;

	Symbol psymClassOrPlaceHolder = g_cman.SymbolTable()->FindSymbol(staInput);

	for (int iritT = 0; iritT < m_vprit.Size(); iritT++)
	{
		GdlSetAttrItem * pritset = dynamic_cast<GdlSetAttrItem*>(m_vprit[iritT]);
		if (!pritset)
			fContext = true;
		else
		{
			if (irit == critRhs)
			{
				if (!psymClassOrPlaceHolder ||
					(!psymClassOrPlaceHolder->FitsSymbolType(ksymtClass) &&
						!psymClassOrPlaceHolder->FitsSymbolType(ksymtSpecialUnderscore) &&
						!psymClassOrPlaceHolder->FitsSymbolType(ksymtSpecialLb) &&
						!psymClassOrPlaceHolder->FitsSymbolType(ksymtSpecialAt)))
				{
					g_errorList.AddError(3137, this,
						"Undefined class name: ",
						staInput,
						lnf);
					psymClassOrPlaceHolder =
						g_cman.SymbolTable()->FindSymbol(GdlGlyphClassDefn::Undefined());
				}

				if (psymClassOrPlaceHolder->FitsSymbolType(ksymtSpecialLb))
				{
					goto LLbError;
				}
				else if (fSubItem)
				{
					GdlSubstitutionItem * pritsub = new GdlSubstitutionItem(*pritset);
					pritsub->SetLineAndFile(lnf);
					pritsub->m_psymOutput = psymClassOrPlaceHolder;
					pritsub->m_psymInput = NULL;	// must be set by LHS

					//	record the 1-based slot-alias value, if any
					if (staAlias != "")
						m_vpalias.Push(
							new GdlAlias(staAlias, m_vprit[iritT]->m_iritContextPos + 1));

					delete pritset;
					m_vprit[iritT] = pritsub;
					return pritsub;
				}

				else
				{
					m_vprit[iritT]->SetLineAndFile(lnf);
					m_vprit[iritT]->m_psymInput = psymClassOrPlaceHolder;

					//	record the 1-based slot-alias value, if any
					if (staAlias != "")
						m_vpalias.Push(
							new GdlAlias(staAlias, m_vprit[iritT]->m_iritContextPos + 1));

					return m_vprit[iritT];
				}
			}

			else
				critRhs++;
		}
	}

	//	Need to add an item

LLbError:
	if (staInput == "#")
	{
		g_errorList.AddError(3138, this,
			StrAnsi("Line break indicator # cannot appear in the right-hand-side"));
		return NULL;
	}

	if (!psymClassOrPlaceHolder ||
		(!psymClassOrPlaceHolder->FitsSymbolType(ksymtClass) &&
			!psymClassOrPlaceHolder->FitsSymbolType(ksymtSpecialUnderscore) &&
			!psymClassOrPlaceHolder->FitsSymbolType(ksymtSpecialAt)))
	{
		g_errorList.AddError(3139, this,
			"Undefined class name: ",
			staInput,
			lnf);
		psymClassOrPlaceHolder = g_cman.SymbolTable()->FindSymbol(GdlGlyphClassDefn::Undefined());
	}

	//	Record an error if there was a context for this rule--then pretend that
	//	there was an extra _ on the end of the context.
	if (fContext)
	{
		g_errorList.AddError(3140, this,
			StrAnsi("Context does not account for all items in the right-hand-side"));
	}

	GdlRuleItem * pritNew;
	if (fSubItem)
		pritNew = new GdlSubstitutionItem(
			g_cman.SymbolTable()->FindSymbol("_"),
			psymClassOrPlaceHolder);
	else
		pritNew = new GdlSetAttrItem(psymClassOrPlaceHolder);

	pritNew->SetLineAndFile(lnf);
	pritNew->m_iritContextPos = m_vprit.Size();
	pritNew->m_iritContextPosOrig = pritNew->m_iritContextPos;

	//	record the 1-based slot-alias value, if any
	if (staAlias != "")
		m_vpalias.Push(new GdlAlias(staAlias, pritNew->m_iritContextPos + 1));

	m_vprit.Push(pritNew);
	return pritNew;
}


/*----------------------------------------------------------------------------------------------
	Handle sitution where they put an '@' or '$' in the rhs. This may be followed by either an
	integer or a string as the selector. nSel == 0 or staSel == "" indicates a bare '@'.
----------------------------------------------------------------------------------------------*/
GdlRuleItem * GdlRule::RhsSelectorItemAt(GrpLineAndFile & lnf, int irit,
	StrAnsi staClassOrAt, int nSel, StrAnsi staAlias)
{
	GdlSubstitutionItem * pritsub = dynamic_cast<GdlSubstitutionItem *>(RhsItemAt(lnf, irit,
		staClassOrAt, staAlias, true));
	if (!pritsub)
		return NULL;

	if (nSel == 0)
		pritsub->m_pexpSelector = NULL;
	else
	{
		pritsub->m_pexpSelector = new GdlSlotRefExpression(nSel);
		pritsub->m_pexpSelector->SetLineAndFile(lnf);
	}
	return pritsub;
}


GdlRuleItem * GdlRule::RhsSelectorItemAt(GrpLineAndFile & lnf, int irit,
	StrAnsi staClassOrAt, StrAnsi staSel, StrAnsi staAlias)
{
	GdlSubstitutionItem * pritsub = dynamic_cast<GdlSubstitutionItem *>(RhsItemAt(lnf, irit,
		staClassOrAt, staAlias, true));
	if (!pritsub)
		return NULL;

	if (staSel == "")
		pritsub->m_pexpSelector = NULL;
	else
	{
		pritsub->m_pexpSelector = new GdlSlotRefExpression(staSel);
		pritsub->m_pexpSelector->SetLineAndFile(lnf);
	}
	return pritsub;
}


/*----------------------------------------------------------------------------------------------
	Return the irit'th item in the LHS.
	Assumes that all items have been created for the context and right-hand side, so there
	should be no need to create one here.
	Caller is responsible for checking that there is exactly an equal number of items in the
	left- and right-hand sides.
	Arguments:
		lnf					- line on which the item appears
		irit				- item number (0-based)
		staInput			- input class name, "_", or "#"
----------------------------------------------------------------------------------------------*/
GdlRuleItem * GdlRule::LhsItemAt(GrpLineAndFile & lnf, int irit,
	StrAnsi staInput, StrAnsi staAlias)
{
	bool fContext = false;
	int critLhs = 0;

	Symbol psymClassOrPlaceHolder = g_cman.SymbolTable()->FindSymbol(staInput);

	for (int iritT = 0; iritT < m_vprit.Size(); iritT++)
	{
		GdlSetAttrItem * pritset = dynamic_cast<GdlSetAttrItem*>(m_vprit[iritT]);
		if (!pritset)
			fContext = true;
		else
		{
			if (irit == critLhs)
			{
				if (!psymClassOrPlaceHolder ||
					(!psymClassOrPlaceHolder->FitsSymbolType(ksymtClass) &&
						!psymClassOrPlaceHolder->FitsSymbolType(ksymtSpecialUnderscore) &&
						!psymClassOrPlaceHolder->FitsSymbolType(ksymtSpecialLb)))
				{
					g_errorList.AddError(3141, this,
						"Undefined class name: ",
						staInput,
						lnf);
					psymClassOrPlaceHolder =
						g_cman.SymbolTable()->FindSymbol(GdlGlyphClassDefn::Undefined());
				}

				GdlSubstitutionItem * pritsub = dynamic_cast<GdlSubstitutionItem*>(pritset);
				if (pritsub)
				{
					// for instance, there was a @ in the rhs
				}
				else
				{
					pritsub = new GdlSubstitutionItem(*pritset);
					// output has been set to input
					delete pritset;
				}
				pritsub->SetLineAndFile(lnf);
				m_vprit[iritT] = pritsub;
				pritsub->m_psymInput = psymClassOrPlaceHolder;	// possibly invalid

				//	Record the 1-based slot-alias value, if any
				if (staAlias != "")
					m_vpalias.Push(
						new GdlAlias(staAlias, pritsub->m_iritContextPos + 1));

				if (psymClassOrPlaceHolder &&
					psymClassOrPlaceHolder->FitsSymbolType(ksymtSpecialLb))
				{
					goto LLbError;
				}

				return m_vprit[iritT];
			}

			else
				critLhs++;
		}
	}

	//	Need to add an item

LLbError:
	if (staInput == "#")
	{
		g_errorList.AddError(3142, this,
			StrAnsi("Line break indicator # cannot appear in the left-hand-side"));
		return NULL;
	}

	if (!psymClassOrPlaceHolder ||
		(!psymClassOrPlaceHolder->FitsSymbolType(ksymtClass) &&
			!psymClassOrPlaceHolder->FitsSymbolType(ksymtSpecialUnderscore)))
	{
		g_errorList.AddError(3143, this,
			"Undefined class name: ",
			staInput,
			lnf);
		psymClassOrPlaceHolder = g_cman.SymbolTable()->FindSymbol(GdlGlyphClassDefn::Undefined());
	}

	if (fContext)
		g_errorList.AddError(3144, this,
			StrAnsi("Context does not account for all items in the left-hand-side"));
	else
		g_errorList.AddError(3145, this,
			StrAnsi("Number of items in left- and right-hand-sides do not match"));

	//	Pretend it is a deletion.

	GdlRuleItem * pritNew = new GdlSubstitutionItem(
			psymClassOrPlaceHolder,
			g_cman.SymbolTable()->FindSymbol("_"));

	pritNew->SetLineAndFile(lnf);
	pritNew->m_iritContextPos = m_vprit.Size();
	pritNew->m_iritContextPosOrig = pritNew->m_iritContextPos;

	//	record the 1-based slot-alias value, if any
	if (staAlias != "")
		m_vpalias.Push(new GdlAlias(staAlias, pritNew->m_iritContextPos + 1));

	m_vprit.Push(pritNew);
	return pritNew;
}


/*----------------------------------------------------------------------------------------------
	Handle error sitution where they put a selector in the lhs ('@' or '$'). nSel == 0 or
	staSel == "" indicates a bare '@'.
----------------------------------------------------------------------------------------------*/
GdlRuleItem * GdlRule::LhsSelectorItemAt(GrpLineAndFile & lnf, int irit,
	StrAnsi staClassOrAt, int nSel, StrAnsi staAlias)
{
	g_errorList.AddError(3146, this,
		"Cannot specify a selector in the lhs");

	if (staClassOrAt = "@")
		return LhsItemAt(lnf, irit, "_", staAlias);
	else
		return LhsItemAt(lnf, irit, staClassOrAt, staAlias);
}

GdlRuleItem * GdlRule::LhsSelectorItemAt(GrpLineAndFile & lnf, int irit,
	StrAnsi staClassOrAt, StrAnsi staSel, StrAnsi staAlias)
{
	g_errorList.AddError(3147, this,
		"Cannot specify a selector in the lhs");

	if (staClassOrAt = "@")
		return LhsItemAt(lnf, irit, "_", staAlias);
	else
		return LhsItemAt(lnf, irit, staClassOrAt, staAlias);
}


/*----------------------------------------------------------------------------------------------
	Record the presence of an optional range in the rule.
	Argument:
		iritStart		- index of first item in optional range - 0 based
		crit			- number of items in optional range
		fContext		- true if iritStart is relative to context, false if it is
							relative to lhs or rhs
----------------------------------------------------------------------------------------------*/
void GdlRule::AddOptionalRange(int iritStart, int crit, bool fContext)
{
	if (crit == 0)
		return;

	m_viritOptRangeStart.Push(iritStart);
	m_viritOptRangeEnd.Push(iritStart + crit - 1);
	m_vfOptRangeContext.Push(fContext);
}


/*----------------------------------------------------------------------------------------------
	Add an association to a rule item.
----------------------------------------------------------------------------------------------*/
void GdlRuleItem::AddAssociation(GrpLineAndFile & lnf, int n)
{
	g_errorList.AddError(3148, NULL,
		"Associations are only permitted in the rhs of a substitution rule",
		lnf);
}

void GdlRuleItem::AddAssociation(GrpLineAndFile & lnf, StrAnsi sta)
{
	g_errorList.AddError(3149, NULL,
		"Associations are only permitted in the rhs of a substitution rule",
		lnf);
}

void GdlSubstitutionItem::AddAssociation(GrpLineAndFile & lnf, int n)
{
	GdlSlotRefExpression * pexp = new GdlSlotRefExpression(n);
	pexp->SetLineAndFile(lnf);
	m_vpexpAssocs.Push(pexp);
}

void GdlSubstitutionItem::AddAssociation(GrpLineAndFile & lnf, StrAnsi sta)
{
	GdlSlotRefExpression * pexp = new GdlSlotRefExpression(sta);
	pexp->SetLineAndFile(lnf);
	m_vpexpAssocs.Push(pexp);
}

/***********************************************************************************************
	Methods: Compiler
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	The sort key is the number of items matched in the rule (minus the prepended ANYs).
	So we take the original total number of items and substract the insertions.
----------------------------------------------------------------------------------------------*/
int GdlRule::SortKey()
{
	//	Count insertions.
	int critIns = 0;
	for (int irit = 0; irit < m_vprit.Size(); irit++)
	{
		GdlSubstitutionItem * pritSub = dynamic_cast<GdlSubstitutionItem *>(m_vprit[irit]);
		if (pritSub)
		{
			Symbol psym = m_vprit[irit]->m_psymInput;
			if (psym && psym->FitsSymbolType(ksymtSpecialUnderscore))
				critIns++;
		}
	}
	return m_critOriginal - critIns;	// original length of rule
}
