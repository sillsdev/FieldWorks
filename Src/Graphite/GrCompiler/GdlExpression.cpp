/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GdlExpression.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implement the various kinds of arithmetic and logical expressions that can appear in an
	GDL file.
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
	Methods: Parser
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Propagate the line number down to the sub-items.
----------------------------------------------------------------------------------------------*/
void GdlUnaryExpression::PropagateLineAndFile(GrpLineAndFile & lnf)
{
	if (LineIsZero())
	{
		SetLineAndFile(lnf);
		m_pexpOperand->PropagateLineAndFile(lnf);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlBinaryExpression::PropagateLineAndFile(GrpLineAndFile & lnf)
{
	if (LineIsZero())
	{
		SetLineAndFile(lnf);
		m_pexpOperand1->PropagateLineAndFile(lnf);
		m_pexpOperand2->PropagateLineAndFile(lnf);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlCondExpression::PropagateLineAndFile(GrpLineAndFile & lnf)
{
	if (LineIsZero())
	{
		SetLineAndFile(lnf);
		m_pexpTest->PropagateLineAndFile(lnf);
		m_pexpTrue->PropagateLineAndFile(lnf);
		if (m_pexpFalse)
			m_pexpFalse->PropagateLineAndFile(lnf);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlLookupExpression::PropagateLineAndFile(GrpLineAndFile & lnf)
{
	if (LineIsZero())
	{
		SetLineAndFile(lnf);
		if (m_pexpSelector)
			m_pexpSelector->PropagateLineAndFile(lnf);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlNumericExpression::PropagateLineAndFile(GrpLineAndFile & lnf)
{
	if (LineIsZero())
		SetLineAndFile(lnf);
}

/*--------------------------------------------------------------------------------------------*/
void GdlSlotRefExpression::PropagateLineAndFile(GrpLineAndFile & lnf)
{
	if (LineIsZero())
		SetLineAndFile(lnf);
}

/*--------------------------------------------------------------------------------------------*/
void GdlStringExpression::PropagateLineAndFile(GrpLineAndFile & lnf)
{
	if (LineIsZero())
		SetLineAndFile(lnf);
}


/***********************************************************************************************
	Methods: Post-parser
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Return true if it make sense to add, subtract, or compare two expressions with the given
	types.
----------------------------------------------------------------------------------------------*/
bool EquivalentTypes(ExpressionType expt1, ExpressionType expt2)
{
	if (expt1 == expt2)
		return true;

	if (expt1 == kexptZero || expt1 == kexptOne)
	{
		switch (expt2)
		{
		case kexptNumber:
		case kexptMeas:
		case kexptBoolean:
		case kexptZero:
		case kexptOne:
			return true;
		default:
			break;
		}
	}
	if (expt2 == kexptZero || expt2 == kexptOne)
	{
		switch (expt1)
		{
		case kexptNumber:
		case kexptMeas:
		case kexptBoolean:
		case kexptZero:
		case kexptOne:
			return true;
		default:
			break;
		}
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Replace any slot aliases with the corresponding (1-based) index.
	Arguments:
		prule			- the rule that contains the list of slot-alias mappings
----------------------------------------------------------------------------------------------*/
bool GdlUnaryExpression::ReplaceAliases(GdlRule * prule)
{
	return m_pexpOperand->ReplaceAliases(prule);
}

/*--------------------------------------------------------------------------------------------*/
bool GdlBinaryExpression::ReplaceAliases(GdlRule * prule)
{
	if (!m_pexpOperand1->ReplaceAliases(prule))
		return false;

	return m_pexpOperand2->ReplaceAliases(prule);
}

/*--------------------------------------------------------------------------------------------*/
bool GdlCondExpression::ReplaceAliases(GdlRule * prule)
{
	if (!m_pexpTest->ReplaceAliases(prule))
		return false;

	if (!m_pexpTrue->ReplaceAliases(prule))
		return false;

	if (m_pexpFalse)
		return m_pexpFalse->ReplaceAliases(prule);

	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlLookupExpression::ReplaceAliases(GdlRule * prule)
{
	if (m_pexpSelector)
		return m_pexpSelector->ReplaceAliases(prule);

	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlNumericExpression::ReplaceAliases(GdlRule * prule)
{
	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlSlotRefExpression::ReplaceAliases(GdlRule * prule)
{
	if (m_srNumber == -1)
	{
		m_srNumber = prule->LookupAliasIndex(m_staName);
		if (m_srNumber < 1)
		{
			g_errorList.AddError(1101, this,
				"Undefined slot alias: ",
				m_staName);
			m_srNumber = 0;
			return false;
		}
	}
	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlStringExpression::ReplaceAliases(GdlRule * prule)
{
	return true;
}


/*----------------------------------------------------------------------------------------------
	Adjust the slot references based on what optional slots were omitted.
	Return false if there was a reference to an omitted item.
	Arguments:
		vfOmit			- for each item, was it omitted?
		vnNewIndices	- for the items that were not omitted, the adjusted index
		prule			- the rule that contains the list of slot-alias mappings, for
							interpreting slot-ref expressions that use names
----------------------------------------------------------------------------------------------*/
bool GdlUnaryExpression::AdjustSlotRefs(Vector<bool>& vfOmit, Vector<int>& vnNewIndices,
	GdlRule * prule)
{
	return m_pexpOperand->AdjustSlotRefs(vfOmit, vnNewIndices, prule);
}

/*--------------------------------------------------------------------------------------------*/
bool GdlBinaryExpression::AdjustSlotRefs(Vector<bool>& vfOmit, Vector<int>& vnNewIndices,
	GdlRule * prule)
{
	if (!m_pexpOperand1->AdjustSlotRefs(vfOmit, vnNewIndices, prule))
		return false;

	return m_pexpOperand2->AdjustSlotRefs(vfOmit, vnNewIndices, prule);
}

/*--------------------------------------------------------------------------------------------*/
bool GdlCondExpression::AdjustSlotRefs(Vector<bool>& vfOmit, Vector<int>& vnNewIndices,
	GdlRule * prule)
{

	if (!m_pexpTest->AdjustSlotRefs(vfOmit, vnNewIndices, prule))
		return false;

	if (!m_pexpTrue->AdjustSlotRefs(vfOmit, vnNewIndices, prule))
		return false;

	if (m_pexpFalse)
		return m_pexpFalse->AdjustSlotRefs(vfOmit, vnNewIndices, prule);

	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlLookupExpression::AdjustSlotRefs(Vector<bool>& vfOmit, Vector<int>& vnNewIndices,
	GdlRule * prule)
{
	if (m_pexpSelector)
		return m_pexpSelector->AdjustSlotRefs(vfOmit, vnNewIndices, prule);

	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlNumericExpression::AdjustSlotRefs(Vector<bool>& vfOmit, Vector<int>& vnNewIndices,
	GdlRule * prule)
{
	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlSlotRefExpression::AdjustSlotRefs(Vector<bool>& vfOmit, Vector<int>& vnNewIndices,
	GdlRule * prule)
{
	int sr = m_srNumber;
	if (m_srNumber == -1)
	{
		sr = prule->LookupAliasIndex(m_staName);
		if (sr < 1)
		{
			g_errorList.AddError(1102, this,
				"Undefined slot alias: ",
				m_staName);
			return false;
		}
	}

	if (vfOmit[sr-1])
	{
		if (m_staName == "")
		{
			char rgch[20];
			itoa(m_srNumber, rgch, 10);
			g_errorList.AddError(1103, this,
				"Optional item referenced: ",
				rgch);
		}
		else
			g_errorList.AddError(1103, this,
				"Optional item referenced: ",
				m_staName);
		return false;
	}

	m_srNumber = vnNewIndices[sr-1];

	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlStringExpression::AdjustSlotRefs(Vector<bool>& vfOmit, Vector<int>& vnNewIndices,
	GdlRule * prule)
{
	return true;
}


/*----------------------------------------------------------------------------------------------
	Set the return argument to the value of the expression as an integer if it can be
	calculated without any context; otherwise return false.
----------------------------------------------------------------------------------------------*/
bool GdlUnaryExpression::ResolveToInteger(int * pnRet, bool fSlotRef)
{

	int nTmp;
	if (!m_pexpOperand->ResolveToInteger(&nTmp, fSlotRef))
		return false;

	if (m_psymOperator->MatchesOp("!"))
		*pnRet = (nTmp == 0)? 1: 0;
	else if (m_psymOperator->MatchesOp("-"))
		*pnRet = nTmp * -1;
	else
		return false;

	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlBinaryExpression::ResolveToInteger(int * pnRet, bool fSlotRef)
{
	int nTmp1, nTmp2;
	if (!m_pexpOperand1->ResolveToInteger(&nTmp1, fSlotRef))
		return false;
	if (!m_pexpOperand2->ResolveToInteger(&nTmp2, fSlotRef))
		return false;

	if (m_psymOperator->MatchesOp("+"))
		*pnRet = nTmp1 + nTmp2;
	else if (m_psymOperator->MatchesOp("-"))
		*pnRet = nTmp1 - nTmp2;
	else if (m_psymOperator->MatchesOp("*"))
		*pnRet = nTmp1 * nTmp2;
	else if (m_psymOperator->MatchesOp("/"))
	{
		if (nTmp2 == 0)
		{
			g_errorList.AddError(1104, this, "Divide by zero.");
			return false;
		}
		*pnRet = nTmp1 / nTmp2;
	}
	else if (m_psymOperator->MatchesOp("&&"))
		*pnRet = (nTmp1 != 0 && nTmp2 != 0)? 1: 0;
	else if (m_psymOperator->MatchesOp("||"))
		*pnRet = (nTmp1 != 0 || nTmp2 != 0)? 1: 0;

	else if (m_psymOperator->MatchesOp("=="))
		*pnRet = (nTmp1 == nTmp2)? 1: 0;
	else if (m_psymOperator->MatchesOp("!="))
		*pnRet = (nTmp1 != nTmp2)? 1: 0;
	else if (m_psymOperator->MatchesOp("<"))
		*pnRet = (nTmp1 < nTmp2)? 1: 0;
	else if (m_psymOperator->MatchesOp("<="))
		*pnRet = (nTmp1 <= nTmp2)? 1: 0;
	else if (m_psymOperator->MatchesOp(">"))
		*pnRet = (nTmp1 > nTmp2)? 1: 0;
	else if (m_psymOperator->MatchesOp(">="))
		*pnRet = (nTmp1 >= nTmp2)? 1: 0;

	else if (m_psymOperator->MatchesOp("max"))
		*pnRet = max(nTmp1, nTmp2);
	else if (m_psymOperator->MatchesOp("min"))
		*pnRet = min(nTmp1, nTmp2);

	else
		return false;

	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlCondExpression::ResolveToInteger(int * pnRet, bool fSlotRef)
{
	int nTmp;
	if (m_pexpTest->ResolveToInteger(&nTmp, fSlotRef))
	{
		if (nTmp == 0)
			return m_pexpFalse->ResolveToInteger(pnRet, fSlotRef);
		else
			return m_pexpTrue->ResolveToInteger(pnRet, fSlotRef);
	}
	else
		return false;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlLookupExpression::ResolveToInteger(int * pnRet, bool fSlotRef)
{
	if (m_pexpSimplified)
		return m_pexpSimplified->ResolveToInteger(pnRet, fSlotRef);

	return false;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlNumericExpression::ResolveToInteger(int * pnRet, bool fSlotRef)
{
	*pnRet = m_nValue;
	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlSlotRefExpression::ResolveToInteger(int * pnRet, bool fSlotRef)
{
	if (fSlotRef)
	{
		*pnRet = m_srNumber;
		return true;
	}
	else
		return false;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlStringExpression::ResolveToInteger(int * pnRet, bool fSlotRef)
{
	return false;
}

/*----------------------------------------------------------------------------------------------
	Return the corresponding feature ID. This is the same as ResolveToInteger except that
	strings of <= 4 characters can be treated as feature IDs.
----------------------------------------------------------------------------------------------*/
bool GdlExpression::ResolveToFeatureID(unsigned int * pnRet)
{
	int nRet;
	bool fRet = ResolveToInteger(&nRet, false);
	*pnRet = (unsigned int)nRet;
	return fRet;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlStringExpression::ResolveToFeatureID(unsigned int * pnRet)
{
	if (m_staValue.Length() > 4)
		return false;

	union {
		char rgch[4];
		unsigned int n;
	} featid;
	// The way we do the assignments ensures the characters are left-aligned
	// in the 4-byte integer (ie, occupying the most significant bytes).
	for (int ich = 0; ich < 4; ich++)
		featid.rgch[3-ich] = (ich < m_staValue.Length()) ? m_staValue[ich] : 0;
	*pnRet = featid.n;
	return true;
}

/***********************************************************************************************
	Methods: Pre-compiler
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Return the type of the expression. If there is inconsistency, return our best guess.
	This is used to deduce the type of a glyph attribute from the value put in it.
----------------------------------------------------------------------------------------------*/
ExpressionType GdlUnaryExpression::ExpType()
{
	if (m_psymOperator->MatchesOp("!"))
		return kexptBoolean;

	else if (m_psymOperator->MatchesOp("-"))
		return m_pexpOperand->ExpType();

	else
		return kexptUnknown;
}

/*--------------------------------------------------------------------------------------------*/
ExpressionType GdlBinaryExpression::ExpType()
{
	ExpressionType expt1 = m_pexpOperand1->ExpType();

	if (m_psymOperator->MatchesOp("+") || m_psymOperator->MatchesOp("-"))
	{
		return expt1;
	}
	else if (m_psymOperator->MatchesOp("*"))
	{
		ExpressionType expt2 = m_pexpOperand2->ExpType();
		if (expt1 == kexptMeas || expt2 == kexptMeas)
			return kexptMeas;
		else
			return kexptNumber;
	}
	else if (m_psymOperator->MatchesOp("/"))
	{
		return expt1;
	}
	else if (m_psymOperator->MatchesOp("&&") || m_psymOperator->MatchesOp("||"))
	{
		return kexptBoolean;
	}
	else if (m_psymOperator->MatchesOp("==") || m_psymOperator->MatchesOp("!=") ||
		m_psymOperator->MatchesOp("<") || m_psymOperator->MatchesOp("<=") ||
		m_psymOperator->MatchesOp(">") || m_psymOperator->MatchesOp(">="))
	{
		return kexptBoolean;
	}
	else if (m_psymOperator->MatchesOp("max") || m_psymOperator->MatchesOp("min"))
	{
		return expt1;
	}
	else
	{
		return expt1;
	}
}

/*--------------------------------------------------------------------------------------------*/
ExpressionType GdlCondExpression::ExpType()
{
	return m_pexpTrue->ExpType();
}

/*--------------------------------------------------------------------------------------------*/
ExpressionType GdlLookupExpression::ExpType()
{
	return m_psymName->ExpType();
}

/*--------------------------------------------------------------------------------------------*/
ExpressionType GdlNumericExpression::ExpType()
{
	if (m_munits == kmunitNone)
	{
		if (m_nValue == 0)
			return kexptZero;
		else if (m_nValue == 1)
			return kexptOne;
		else
			return kexptNumber;
	}
	else
		return kexptMeas;
}

/*--------------------------------------------------------------------------------------------*/
ExpressionType GdlSlotRefExpression::ExpType()
{
	return kexptSlotRef;
}

/*--------------------------------------------------------------------------------------------*/
ExpressionType GdlStringExpression::ExpType()
{
	return kexptString;
}


/*----------------------------------------------------------------------------------------------
	Check for matching types and the appropriate presence and absence of scaled numbers.
	Record an error if there is a problem. Return true if everything is okay.
----------------------------------------------------------------------------------------------*/
bool GdlExpression::TypeCheck(ExpressionType exptExpected)
{
	Vector<ExpressionType> vnTmp;
	vnTmp.Push(exptExpected);
	return TypeCheck(vnTmp);
}

/*--------------------------------------------------------------------------------------------*/
bool GdlExpression::TypeCheck(ExpressionType expt1, ExpressionType expt2, ExpressionType expt3)
{
	Vector<ExpressionType> vnTmp;
	vnTmp.Push(expt1);
	vnTmp.Push(expt2);
	vnTmp.Push(expt3);
	return TypeCheck(vnTmp);
}

/*--------------------------------------------------------------------------------------------*/
bool GdlExpression::TypeCheck(Vector<ExpressionType>& vexptExpected)
{
	ExpressionType exptFound;
	if (!CheckTypeAndUnits(&exptFound))
		return false;

	for (int i = 0; i < vexptExpected.Size(); ++i)
	{
		ExpressionType exptOkay = vexptExpected[i];
		if (exptOkay == exptFound)
			return true;
		if (exptOkay == kexptUnknown)
			return true;
		if ((exptFound == kexptZero || exptFound == kexptOne) &&
			(exptOkay == kexptMeas || exptOkay == kexptNumber || exptOkay == kexptBoolean ||
				exptOkay == kexptZero || exptOkay == kexptOne))
		{
			return true;
		}
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Check for matching types in sub-expressions and the appropriate presence and absence of
	scaled numbers. Return the expression type in the argument. Return true if the expression
	is well-formed enough that it is worth continuing processing with it, false if a fatal
	error was recorded.
----------------------------------------------------------------------------------------------*/
bool GdlUnaryExpression::CheckTypeAndUnits(ExpressionType * pexptRet)
{
	ExpressionType expt;
	if (!m_pexpOperand->CheckTypeAndUnits(&expt))
		return false;

	if (expt == kexptSlotRef)
	{
		g_errorList.AddError(2101, this,
			"Cannot use '",
			m_psymOperator->FullName(),
			"' operator with slot index");
		*pexptRet = expt;
		return false;
	}
	if (m_psymOperator->MatchesOp("!"))
	{
		if (expt != kexptBoolean && expt != kexptZero && expt != kexptOne)
			g_errorList.AddWarning(2501, this,
				"Boolean expression expected as target of '!' operator.");
		*pexptRet = kexptBoolean;
		return true;
	}
	else if (m_psymOperator->MatchesOp("-"))
	{
		if (expt != kexptNumber && expt != kexptMeas && expt != kexptZero && expt != kexptOne)
			g_errorList.AddWarning(2502, this,
				"Numeric expression expected as target of '-' operator.");
		*pexptRet = expt;
		return true;
	}
	else
	{
		g_errorList.AddError(2102, this,
			"Invalid unary operator: ",
			m_psymOperator->FieldAt(1));
	}

	return expt;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlBinaryExpression::CheckTypeAndUnits(ExpressionType * pexptRet)
{
	ExpressionType expt1;
	if (!m_pexpOperand1->CheckTypeAndUnits(&expt1))
		return false;
	ExpressionType expt2;
	if (!m_pexpOperand2->CheckTypeAndUnits(&expt2))
		return false;

	*pexptRet = expt1;	// unless changed below

	if (m_psymOperator->MatchesOp("+") || m_psymOperator->MatchesOp("-") ||
		m_psymOperator->MatchesOp("*") || m_psymOperator->MatchesOp("/"))
	{
		//	Additive, multiplicative
		if (expt1 == kexptSlotRef || expt2 == kexptSlotRef)
		{
			g_errorList.AddError(2103, this,
				"Using '",
				m_psymOperator->FullName(),
				"' operator with slot indices");
			return false;
		}
		if ((expt1 != kexptNumber && expt1 != kexptMeas &&
				expt1 != kexptZero && expt1 != kexptOne) ||
			(expt2 != kexptNumber && expt2 != kexptMeas &&
				expt2 != kexptZero && expt2 != kexptOne))
		{
			g_errorList.AddWarning(2503, this,
				"Numeric expression expected as target of ",
				m_psymOperator->FullName(),
				" operator.");
		}

		if (m_psymOperator->MatchesOp("+") || m_psymOperator->MatchesOp("-"))
		{
			if (!EquivalentTypes(expt1, expt2) && expt1 != kexptUnknown && expt2 != kexptUnknown)
				g_errorList.AddWarning(2504, this,
					"Adding measurement to non-measurement");
		}
		else if (m_psymOperator->MatchesOp("*"))
		{
			if (expt1 == kexptMeas && expt2 == kexptMeas)
				g_errorList.AddWarning(2505, this,
					"Multiplying two measurements");

			if (expt1 == kexptMeas || expt2 == kexptMeas)
				*pexptRet = kexptMeas;
			else
				*pexptRet = kexptNumber;
		}
		else if (m_psymOperator->MatchesOp("/"))
		{
			if (expt2 == kexptMeas)
				g_errorList.AddWarning(2506, this, "Divisor is a measurement");
			else if (expt2 == kexptZero)
				g_errorList.AddError(2104, this, "Dividing by zero");
		}
	}
	else if (m_psymOperator->MatchesOp("&&") || m_psymOperator->MatchesOp("||"))
	{
		//	Logical
		if ((expt1 != kexptBoolean && expt1 != kexptZero && expt1 != kexptOne) ||
			(expt2 != kexptBoolean && expt2 != kexptZero && expt2 != kexptOne))
		{
			g_errorList.AddWarning(2507, this,
				"Boolean expression expected as target of ",
				m_psymOperator->FullName(),
				" operator");
		}
		if (!EquivalentTypes(expt1, expt2) && expt1 != kexptUnknown && expt2 != kexptUnknown)
			g_errorList.AddWarning(2508, this,
				"Logically combining expressions of different types");

		*pexptRet = kexptBoolean;
	}
	else if (m_psymOperator->MatchesOp("==") || m_psymOperator->MatchesOp("!=") ||
		m_psymOperator->MatchesOp("<") || m_psymOperator->MatchesOp("<=") ||
		m_psymOperator->MatchesOp(">") || m_psymOperator->MatchesOp(">="))
	{
		//	Comparative
		if (!EquivalentTypes(expt1, expt2) && expt1 != kexptUnknown && expt2 != kexptUnknown)
			g_errorList.AddWarning(2509, this,
				"Comparing expressions of different types");

		*pexptRet = kexptBoolean;
	}
	else if (m_psymOperator->MatchesOp("max") || m_psymOperator->MatchesOp("min"))
	{
		//	Functional
		if (expt1 != kexptNumber && expt1 != kexptMeas &&
			expt1 != kexptSlotRef && expt1 != kexptZero && expt1 != kexptOne &&
			expt2 != kexptNumber && expt2 != kexptMeas &&
			expt2 != kexptSlotRef && expt2 != kexptZero && expt2 != kexptOne)
		{
			g_errorList.AddWarning(2510, this,
				"Numeric expression expected as target of ",
				m_psymOperator->FullName(),
				" function");
		}
		if (!EquivalentTypes(expt1, expt2) && expt1 != kexptUnknown && expt2 != kexptUnknown)
			g_errorList.AddWarning(2511, this,
				"Calculating ",
				m_psymOperator->FullName(),
				" of different expression types");
	}
	else if (m_psymOperator->MatchesOp("=") ||
		m_psymOperator->MatchesOp("+=") || m_psymOperator->MatchesOp("-=") ||
		m_psymOperator->MatchesOp("*=") || m_psymOperator->MatchesOp("/="))
	{
		//	Assignment
		g_errorList.AddError(2105, this,
			m_psymOperator->FullName(),
			" assignment operator not permitted in expression");
		return false;
	}
	else
	{
		g_errorList.AddError(2106, this,
			"Invalid binary operator: ",
			m_psymOperator->FullName());
		return false;
	}

	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlCondExpression::CheckTypeAndUnits(ExpressionType * pexptRet)
{
	ExpressionType exptTest;
	if (!m_pexpTest->CheckTypeAndUnits(&exptTest))
		return false;

	if (exptTest != kexptBoolean)
		g_errorList.AddWarning(2512, this, "Boolean expression expected as condition");

	ExpressionType expt1;
	if (!m_pexpTrue->CheckTypeAndUnits(&expt1))
		return false;
	ExpressionType expt2;
	if (!m_pexpFalse->CheckTypeAndUnits(&expt2))
		return false;

	if (!EquivalentTypes(expt1, expt2))
	{
		if (expt1 == kexptSlotRef || expt2 == kexptSlotRef)
			//	One or the other is sure to be wrong.
			g_errorList.AddError(2107, this,
				"Inconsistent types in conditional branches");
		else
			g_errorList.AddWarning(2513, this,
				"Non-matching types in conditional branches");
	}

	if (expt1 == kexptZero || expt1 == kexptOne)
		*pexptRet = expt2;	// this is more useful than kexptZero
	else
		*pexptRet = expt1;

	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlLookupExpression::CheckTypeAndUnits(ExpressionType * pexptRet)
{
	*pexptRet = m_psymName->ExpType();
	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlNumericExpression::CheckTypeAndUnits(ExpressionType * pexptRet)
{
	if (m_nValue == 0 && m_munits == kmunitNone)
		//	so that 0 can be treated as equivalent to 0m
		*pexptRet = kexptZero;
	else if (m_nValue == 1 && m_munits == kmunitNone)
		*pexptRet = kexptOne;
	else if (m_munits == kmunitNone)
		*pexptRet = kexptNumber;
	else
		*pexptRet = kexptMeas;

	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlSlotRefExpression::CheckTypeAndUnits(ExpressionType * pexptRet)
{
	*pexptRet = kexptSlotRef;
	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlStringExpression::CheckTypeAndUnits(ExpressionType * pexptRet)
{
	*pexptRet = kexptString;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Check that the expression, which is the value of a glyph attribute, does not make use
	of slot attributes, features, or slot index references.
----------------------------------------------------------------------------------------------*/
void GdlUnaryExpression::GlyphAttrCheck()
{
	m_pexpOperand->GlyphAttrCheck();
}

/*--------------------------------------------------------------------------------------------*/
void GdlBinaryExpression::GlyphAttrCheck()
{
	m_pexpOperand1->GlyphAttrCheck();
	m_pexpOperand2->GlyphAttrCheck();
}

/*--------------------------------------------------------------------------------------------*/
void GdlCondExpression::GlyphAttrCheck()
{
	m_pexpTest->GlyphAttrCheck();
	m_pexpTrue->GlyphAttrCheck();
	m_pexpFalse->GlyphAttrCheck();
}

/*--------------------------------------------------------------------------------------------*/
void GdlLookupExpression::GlyphAttrCheck()
{
	if (m_psymName->FitsSymbolType(ksymtGlyph))
	{
		//	okay
	}
	else if (m_psymName->FitsSymbolType(ksymtSlotAttr))
	{
		g_errorList.AddError(2108, this,
			"Slot attribute references are not permitted in glyph attribute values");
	}
	else if (m_psymName->FitsSymbolType(ksymtFeature))
	{
		g_errorList.AddError(2109, this,
			"Feature references are not permitted in glyph attribute values");
	}
	else if (m_psymName->FitsSymbolType(ksymtProcState))
	{
		g_errorList.AddError(2110, this,
			"Processing-state references are not permitted in glyph attribute values");
	}
	else
		g_errorList.AddError(2111, this,
			"Unknown attribute: ",
			m_psymName->FullName());
}

/*--------------------------------------------------------------------------------------------*/
void GdlNumericExpression::GlyphAttrCheck()
{
}

/*--------------------------------------------------------------------------------------------*/
void GdlSlotRefExpression::GlyphAttrCheck()
{
	g_errorList.AddError(2112, this,
		StrAnsi("Slot references are not permitted in glyph attribute values"));
}

/*--------------------------------------------------------------------------------------------*/
void GdlStringExpression::GlyphAttrCheck()
{
}


/*----------------------------------------------------------------------------------------------
	Test that any feature settings tests are valid within the context; replace settings
	with the corresponding numeric expression.
	For instance, suppose we have

		ligatures.settings {
			none.value = 0;
			some.value = 1;
			all.value  = 2;
		}
		swashes.settings {
			none.value  = 0;
			basic.value = 1;
			most.value = 2;
			all.value = 3;
		}
	Then an expression like "ligatures == all" gets converted to "ligatures == 2";
	"swashes <= basic" becomes "swashes <= 1'; "ligatures > basic" is an error.

	The special "lang" feature converts its language ID string to an integer.
----------------------------------------------------------------------------------------------*/
void GdlUnaryExpression::FixFeatureTestsInRules(GrcFont *pfont)
{
	m_pexpOperand->FixFeatureTestsInRules(pfont);
}

/*--------------------------------------------------------------------------------------------*/
void GdlBinaryExpression::FixFeatureTestsInRules(GrcFont *pfont)
{

	GdlLookupExpression * pexplookFeature =
		dynamic_cast<GdlLookupExpression *>(m_pexpOperand1);

	if (m_psymOperator->IsComparativeOp() && pexplookFeature &&
		pexplookFeature->NameFitsSymbolType(ksymtFeature))
	{
		GdlFeatureDefn * pfeat = pexplookFeature->Name()->FeatureDefnData();
		Assert(pfeat);
		GdlExpression * pexpNew = m_pexpOperand2->ConvertFeatureSettingValue(pfeat);
		if (pexpNew != m_pexpOperand2)
		{
			delete m_pexpOperand2;
			m_pexpOperand2 = pexpNew;
		}

		pexpNew = m_pexpOperand2->SimplifyAndUnscale(0xFFFF, pfont);
		Assert(pexpNew);
		if (pexpNew && pexpNew != m_pexpOperand2)
		{
			delete m_pexpOperand2;
			m_pexpOperand2 = pexpNew;
		}

		GdlNumericExpression * pexpnum = dynamic_cast<GdlNumericExpression *>(m_pexpOperand2);
		if (pexpnum)
		{
			if (!pfeat->IsLanguageFeature())
			{
				GdlFeatureSetting * pfset = pfeat->FindSettingWithValue(pexpnum->Value());
				if (!pfset)
				{
					char rgch[20];
					itoa(pexpnum->Value(), rgch, 10);
					g_errorList.AddWarning(2514, this,
						"Feature '",
						pfeat->Name(),
						"' has no setting with value ",
						rgch,
						((pexpnum->m_munits >= kmunitDefault) ? "m" : ""));
				}
			}
		}
	}
	else
	{
		m_pexpOperand1->FixFeatureTestsInRules(pfont);
		m_pexpOperand2->FixFeatureTestsInRules(pfont);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlCondExpression::FixFeatureTestsInRules(GrcFont *pfont)
{
	m_pexpTest->FixFeatureTestsInRules(pfont);
	m_pexpTrue->FixFeatureTestsInRules(pfont);
	m_pexpFalse->FixFeatureTestsInRules(pfont);
}

/*--------------------------------------------------------------------------------------------*/
void GdlLookupExpression::FixFeatureTestsInRules(GrcFont *pfont)
{
}

/*--------------------------------------------------------------------------------------------*/
void GdlNumericExpression::FixFeatureTestsInRules(GrcFont *pfont)
{
}

/*--------------------------------------------------------------------------------------------*/
void GdlSlotRefExpression::FixFeatureTestsInRules(GrcFont *pfont)
{
}

/*--------------------------------------------------------------------------------------------*/
void GdlStringExpression::FixFeatureTestsInRules(GrcFont *pfont)
{
}


/*----------------------------------------------------------------------------------------------
	The recipient is the right-hand side of a feature comparison expresion
	(eg, ligatures == all).
	Return the equivalent expression, with any feature setting value converted to a
	numeric expression.
----------------------------------------------------------------------------------------------*/
GdlExpression * GdlUnaryExpression::ConvertFeatureSettingValue(GdlFeatureDefn * pfeat)
{
	if (pfeat->IsLanguageFeature())
		g_errorList.AddWarning(2515, this,
			"Arithmetic calculation of language ID value");

	GdlExpression * pexpNew = m_pexpOperand->ConvertFeatureSettingValue(pfeat);
	if (pexpNew != m_pexpOperand)
	{
		delete m_pexpOperand;
		m_pexpOperand = pexpNew;
	}
	return this;
}

/*--------------------------------------------------------------------------------------------*/
GdlExpression * GdlBinaryExpression::ConvertFeatureSettingValue(GdlFeatureDefn * pfeat)
{
	if (pfeat->IsLanguageFeature())
		g_errorList.AddWarning(2516, this,
			"Arithmetic calculation of language ID value");

	GdlExpression * pexpNew;
	pexpNew = m_pexpOperand1->ConvertFeatureSettingValue(pfeat);
	if (pexpNew != m_pexpOperand1)
	{
		delete m_pexpOperand1;
		m_pexpOperand1 = pexpNew;
	}
	pexpNew = m_pexpOperand2->ConvertFeatureSettingValue(pfeat);
	if (pexpNew != m_pexpOperand2)
	{
		delete m_pexpOperand2;
		m_pexpOperand2 = pexpNew;
	}
	return this;
}

/*--------------------------------------------------------------------------------------------*/
GdlExpression * GdlCondExpression::ConvertFeatureSettingValue(GdlFeatureDefn * pfeat)
{
	GdlExpression * pexpNew;
	pexpNew = m_pexpTest->ConvertFeatureSettingValue(pfeat);
	if (pexpNew != m_pexpTest)
	{
		delete m_pexpTest;
		m_pexpTest = pexpNew;
	}
	pexpNew = m_pexpTrue->ConvertFeatureSettingValue(pfeat);
	if (pexpNew != m_pexpTrue)
	{
		delete m_pexpTrue;
		m_pexpTrue = pexpNew;
	}
	pexpNew = m_pexpFalse->ConvertFeatureSettingValue(pfeat);
	if (pexpNew != m_pexpFalse)
	{
		delete m_pexpFalse;
		m_pexpFalse = pexpNew;
	}
	return this;
}

/*--------------------------------------------------------------------------------------------*/
GdlExpression * GdlLookupExpression::ConvertFeatureSettingValue(GdlFeatureDefn * pfeat)
{
	if (pfeat->IsLanguageFeature())
		g_errorList.AddWarning(2517, this,
			"Arithmetic calculation of language ID value");

	//	Note: normally the symbol type will be ksymtInvalid.
	if (m_psymName->FieldCount() > 1)
	{
		g_errorList.AddError(2113, this,
			"Invalid feature setting: ",
			m_psymName->FullName());
		return this;
	}

	GdlFeatureSetting * pfset = pfeat->FindSetting(m_psymName->LastField());
	if (!pfset)
	{
		g_errorList.AddError(2114, this,
			"Feature '",
			pfeat->Name(),
			"' has no setting '",
			m_psymName->FullName(),
			"'");
		return this;
	}

	//	Caller will replace setting expression with numeric value.
	GdlNumericExpression * pexpValue = new GdlNumericExpression(pfset->Value());
	return pexpValue;
}

/*--------------------------------------------------------------------------------------------*/
GdlExpression * GdlNumericExpression::ConvertFeatureSettingValue(GdlFeatureDefn * pfeat)
{
	if (pfeat->IsLanguageFeature())
		g_errorList.AddWarning(2518, this,
			"Numeric value where language ID string expected");

	return this;
}

/*--------------------------------------------------------------------------------------------*/
GdlExpression * GdlSlotRefExpression::ConvertFeatureSettingValue(GdlFeatureDefn * pfeat)
{
	//	Caller will replace slot-ref expression with numeric expression.
	char rgch[20];
	itoa(m_srNumber, rgch, 10);
	g_errorList.AddWarning(2519, this,
		"Inappropriate value of feature setting: @",
		rgch);
	GdlNumericExpression * pexpValue = new GdlNumericExpression(m_srNumber);
	return pexpValue;
}

/*--------------------------------------------------------------------------------------------*/
GdlExpression * GdlStringExpression::ConvertFeatureSettingValue(GdlFeatureDefn * pfeat)
{
	if (pfeat->IsLanguageFeature())
	{
		int nValue = 0;
		int cb = m_staValue.Length();
		if (m_staValue.Length() > 4)
		{
			g_errorList.AddError(2115, this,
				"Invalid language ID--must be a 4-byte string");
		}
		else if (m_staValue.Length() < 4)
		{
			g_errorList.AddWarning(2520, this,
				"Possibly invalid language ID--4-byte string expected");
		}
		byte b1, b2, b3, b4;
		b1 = (cb > 0) ? m_staValue[0] : 0;
		b2 = (cb > 1) ? m_staValue[1] : 0;
		b3 = (cb > 2) ? m_staValue[2] : 0;
		b4 = (cb > 3) ? m_staValue[3] : 0;
		nValue = (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;

		//	Caller will replace original expression with numeric value.
		GdlNumericExpression * pexpValue = new GdlNumericExpression(nValue);
		return pexpValue;
	}
	else
	{
		g_errorList.AddError(2116, this,
			"Inappropriate value of feature setting: ",
			m_staValue);
	}
	return this;
}


/*----------------------------------------------------------------------------------------------
	Do a final check to make sure that all look-up expressions within a rule are meaningful.
	Argument:
		fInIf			- true if the statement was inside an -if- statement, rather than
							inside a rule's context.
----------------------------------------------------------------------------------------------*/
void GdlUnaryExpression::LookupExpCheck(bool fInIf)
{
	m_pexpOperand->LookupExpCheck(fInIf);
}

/*--------------------------------------------------------------------------------------------*/
void GdlBinaryExpression::LookupExpCheck(bool fInIf)
{
	m_pexpOperand1->LookupExpCheck(fInIf);
	m_pexpOperand2->LookupExpCheck(fInIf);
}

/*--------------------------------------------------------------------------------------------*/
void GdlCondExpression::LookupExpCheck(bool fInIf)
{
	m_pexpTest->LookupExpCheck(fInIf);
	m_pexpTrue->LookupExpCheck(fInIf);
	m_pexpFalse->LookupExpCheck(fInIf);
}

/*--------------------------------------------------------------------------------------------*/
void GdlLookupExpression::LookupExpCheck(bool fInIf)
{
	if (!m_psymName)
	{
		g_errorList.AddError(2117, this,
			"Undefined attribute");
		return;
	}
	else if (m_psymName->FitsSymbolType(ksymtGlyphMetric))
	{
		//	Okay
	}
	else if (m_psymName->FitsSymbolType(ksymtGlyphAttr) ||
		m_psymName->FitsSymbolType(ksymtSlotAttr) ||
		m_psymName->FitsSymbolType(ksymtFeature) ||
		m_psymName->FitsSymbolType(ksymtProcState))
	{
		if (m_nClusterLevel != 0)
			g_errorList.AddError(2118, this,
				"Composite metric indicator is incompatible with ",
				m_psymName->TypeDescriptorString());
	}
	else if (m_psymName->FitsSymbolType(ksymtInvalidGlyphAttr))
	{
		g_errorList.AddError(2119, this,
			"Incomplete glyph attribute: ",
			m_psymName->FullName());
	}
	else
	{
		g_errorList.AddError(2120, this,
			"Undefined attribute: ",
			m_psymName->FullName());
		return;
	}

	if (fInIf)
	{
		if (!m_psymName->FitsSymbolType(ksymtFeature) &&
			!m_psymName->FitsSymbolType(ksymtProcState))
		{
			g_errorList.AddError(2121, this,
				"Only features and the processing state may be tested within 'if' statements; ",
				m_psymName->TypeDescriptorString(),
				"s not permitted");
		}

		if (m_pexpSelector)
		{
			char rgch[20];
			itoa(m_pexpSelector->SlotNumber(), rgch, 10);
			g_errorList.AddError(2122, this,
				"Slot selectors are not permitted in 'if' statements: @",
				rgch);
		}
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlNumericExpression::LookupExpCheck(bool fInIf)
{
}

/*--------------------------------------------------------------------------------------------*/
void GdlSlotRefExpression::LookupExpCheck(bool fInIf)
{
}

/*--------------------------------------------------------------------------------------------*/
void GdlStringExpression::LookupExpCheck(bool fInIf)
{
}


/*----------------------------------------------------------------------------------------------
	Simplify any expressions down to integers that can be, returning a new expression
	equivalent to the recipient. Convert scaled numbers to absolute (in place).
	Return NULL if there was an error in the expression.
	Caller is responsible to delete the returned expression.
	Arguments:
		pgax			- glyph attribute maxtrix, for resolving glyph attributes; or NULL
		wGlyphID		- glyph ID to use in resolving glyph attributes and metrics;
							0xFFFF means that glyph attributes are not permitted in
							the current context
		setpsym			- set of glyph attribute symbols encountered; for catching infinite
							loops in definitions of glyph attributes
		pfont			- font, for reading glyph metrics
		fGAttrDefChk	- check that glyph attributes are defined for the given glyph ID
							(this is false within the context of rules, which are checked
							elsewhere)
		pfCanSub		- true means replacement expression is irrespective of the glyph ID,
							and therefore can be replaced directly in the enclosing expression;
							if false, make a new expression to return, because another glyph ID
							may be using the original expression, so it needs to hang around
							unmodified.
----------------------------------------------------------------------------------------------*/
GdlExpression * GdlUnaryExpression::SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
	utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont, bool fGAttrDefChk,
	bool * pfCanSub)
{
	bool fCanSubOperand;
	GdlExpression * pexpOperandNew =
		m_pexpOperand->SimplifyAndUnscale(pgax, wGlyphID, setpsym, pfont,
			fGAttrDefChk, &fCanSubOperand);

	if (!pexpOperandNew)
		return NULL;

	GdlUnaryExpression * pexpunRet = this;

	if (pexpOperandNew != m_pexpOperand)
	{
		if (!fCanSubOperand)
		{
			pexpunRet = dynamic_cast<GdlUnaryExpression *>(this->Clone());
			Assert(pexpunRet);
		}
		delete pexpunRet->m_pexpOperand;
		pexpunRet->m_pexpOperand = pexpOperandNew;
	}
	*pfCanSub = (pexpunRet == this);

	int nValue;
	if (this->ResolveToInteger(&nValue, false))
	{
		GdlExpression * pexpRet = new GdlNumericExpression(nValue);
		pexpRet->CopyLineAndFile(*this);
		return pexpRet;
	}

	return pexpunRet;
}

/*--------------------------------------------------------------------------------------------*/
GdlExpression * GdlBinaryExpression::SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
	utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont, bool fGAttrDefChk,
	bool * pfCanSub)
{
	bool fCanSubOperand1, fCanSubOperand2;
	GdlExpression * pexpOperand1New =
		m_pexpOperand1->SimplifyAndUnscale(pgax, wGlyphID, setpsym, pfont,
			fGAttrDefChk, &fCanSubOperand1);
	GdlExpression * pexpOperand2New =
		m_pexpOperand2->SimplifyAndUnscale(pgax, wGlyphID, setpsym, pfont,
			fGAttrDefChk, &fCanSubOperand2);

	if (!pexpOperand1New || !pexpOperand2New)
		return NULL;

	GdlBinaryExpression * pexpbinRet = this;

	if (pexpOperand1New && pexpOperand1New != m_pexpOperand1)
	{
		if (!fCanSubOperand1)
		{
			pexpbinRet = dynamic_cast<GdlBinaryExpression *>(this->Clone());
			Assert(pexpbinRet);
		}
		delete pexpbinRet->m_pexpOperand1;
		pexpbinRet->m_pexpOperand1 = pexpOperand1New;
	}
	if (pexpOperand2New && pexpOperand2New != m_pexpOperand2)
	{
		if (!fCanSubOperand2 && pexpbinRet == this)
		{
			pexpbinRet = dynamic_cast<GdlBinaryExpression *>(this->Clone());
			Assert(pexpbinRet);
		}
		delete pexpbinRet->m_pexpOperand2;
		pexpbinRet->m_pexpOperand2 = pexpOperand2New;
	}
	*pfCanSub = (pexpbinRet == this);

	int nValue;
	if (this->ResolveToInteger(&nValue, false))
	{
		GdlExpression * pexpRet = new GdlNumericExpression(nValue);
		pexpRet->CopyLineAndFile(*this);
		return pexpRet;
	}

	return pexpbinRet;
}

/*--------------------------------------------------------------------------------------------*/
GdlExpression * GdlCondExpression::SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
	utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont, bool fGAttrDefChk,
	bool * pfCanSub)
{
	bool fCanSubTest, fCanSubTrue, fCanSubFalse;
	GdlExpression * pexpTestNew =
		m_pexpTest->SimplifyAndUnscale(pgax, wGlyphID, setpsym, pfont, fGAttrDefChk,
			&fCanSubTest);
	GdlExpression * pexpTrueNew =
		m_pexpTrue->SimplifyAndUnscale(pgax, wGlyphID, setpsym, pfont, fGAttrDefChk,
			&fCanSubTrue);
	GdlExpression * pexpFalseNew =
		m_pexpFalse->SimplifyAndUnscale(pgax, wGlyphID, setpsym, pfont, fGAttrDefChk,
			&fCanSubFalse);

	if (!pexpTestNew || !pexpTrueNew || !pexpFalseNew)
		return NULL;

	GdlCondExpression * pexpcondRet = this;

	if (pexpTestNew && pexpTestNew != m_pexpTest)
	{
		if (!fCanSubTest)
		{
			pexpcondRet = dynamic_cast<GdlCondExpression *>(this->Clone());
			Assert(pexpcondRet);
		}
		delete pexpcondRet->m_pexpTest;
		pexpcondRet->m_pexpTest = pexpTestNew;
	}
	if (pexpTrueNew && pexpTrueNew != m_pexpTrue)
	{
		if (!fCanSubTrue && pexpcondRet == this)
		{
			pexpcondRet = dynamic_cast<GdlCondExpression *>(this->Clone());
			Assert(pexpcondRet);
		}
		delete pexpcondRet->m_pexpTrue;
		pexpcondRet->m_pexpTrue = pexpTrueNew;
	}
	if (pexpFalseNew && pexpFalseNew != m_pexpFalse)
	{
		if (!fCanSubFalse && pexpcondRet == this)
		{
			pexpcondRet = dynamic_cast<GdlCondExpression *>(this->Clone());
			Assert(pexpcondRet);
		}
		delete pexpcondRet->m_pexpFalse;
		pexpcondRet->m_pexpFalse = pexpFalseNew;
	}
	*pfCanSub = (pexpcondRet == this);

	int nValue;
	if (this->ResolveToInteger(&nValue, false))
	{
		GdlExpression * pexpRet = new GdlNumericExpression(nValue);
		pexpRet->CopyLineAndFile(*this);
		return pexpRet;
	}

	return pexpcondRet;
}

/*--------------------------------------------------------------------------------------------*/
GdlExpression * GdlLookupExpression::SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
	utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont, bool fGAttrDefChk,
	bool * pfCanSub)
{
	int nAttrID;

	if (!m_psymName->IsGeneric() && pgax
		&& (m_psymName->FitsSymbolType(ksymtGlyphAttr)
			|| m_psymName->FitsSymbolType(ksymtGlyphMetric)))
	{
		//	A non-generic attribute or metric means the value for the
		//	first element of the given class.
		Symbol psymTmp = m_psymName;
		while (psymTmp->ParentSymbol())
			psymTmp = psymTmp->ParentSymbol();
		GdlGlyphClassDefn * pglfc = psymTmp->GlyphClassDefnData();
		if (!pglfc)
		{
			g_errorList.AddError(2123, this,
				"Undefined glyph class: ", psymTmp->FullName());
			return NULL;
		}
		bool fMoreThanOne = false;
		unsigned int nGlyphIDFirst = pglfc->FirstGlyphInClass(&fMoreThanOne);
		bool fDefined = (nGlyphIDFirst != 0);
		if (m_psymName->FitsSymbolType(ksymtGlyphAttr))
		{
			nAttrID = m_psymName->InternalID();
			if (fDefined)
			{
				Symbol psymGeneric = m_psymName->Generic();
				//	A predefined glyph attribute such as directionality does not need an
				//	explicit assignment to be defined.
				fDefined = (psymGeneric && !psymGeneric->IsUserDefined());
			}
			fDefined = (fDefined || pgax->Defined(nGlyphIDFirst, nAttrID));
			if (!fDefined)
			{
				g_errorList.AddError(2124, this,
					"Undefined glyph attribute: ", m_psymName->FullName());
				return NULL;
			}
		}
		else if (!fDefined)
		{
			g_errorList.AddError(2125, this,
				"Undefined identifier: ", m_psymName->FullName());
		}

		if (fMoreThanOne)
		{
			g_errorList.AddWarning(2521, this,
				"Class '",
				pglfc->Name(),
				"' has size > 1; first glyph will be used to evaluate ",
				m_psymName->FullName());
		}

		GdlExpression * pexpRet;
		if (m_psymName->FitsSymbolType(ksymtGlyphAttr))
		{
			setpsym.Insert(m_psymName);
			GdlExpression * pexp;
			pexp = pgax->GetExpression(nGlyphIDFirst, nAttrID);
			if (pexp)
			{
				pexpRet = pexp->SimplifyAndUnscale(pgax, nGlyphIDFirst, setpsym, pfont,
					true, pfCanSub);
				if (pexpRet)
					pexpRet = pexpRet->Clone();
			}
			else
				//	Predefined attribute with no explicit value.
				pexpRet = new GdlNumericExpression(0);
			setpsym.Delete(m_psymName);	// in case we look up this attribute again in the
										// same outer expression
		}
		else if (m_psymName->FitsSymbolType(ksymtGlyphMetric))
		{
			int gmet = m_psymName->GlyphMetricEngineCodeOp();
			int nValue;
			if (gmet == -1)
			{
				g_errorList.AddError(2126, this,
					"Invalid glyph metric: ",
					m_psymName->FullName());
				nValue = 0;
			}
			else
			{
				int nActual = g_cman.ActualForPseudo(nGlyphIDFirst);
				if (nActual == 0)
					nActual = nGlyphIDFirst;
				nValue = pfont->GetGlyphMetric(nActual, GlyphMetric(gmet), this);
			}

			pexpRet = new GdlNumericExpression(nValue);
			pexpRet->CopyLineAndFile(*this);
		}

		m_pexpSimplified = dynamic_cast<GdlNumericExpression *>(pexpRet);

		*pfCanSub = true;
		if (m_pexpSimplified)
			return this;
		else
			return pexpRet;
	}

	// A generic glyph attribute or metric refers to the current item.

	if (m_psymName->FitsSymbolType(ksymtGlyphMetric))
	{
		if (wGlyphID == 0xFFFF)
		{
			g_errorList.AddError(2127, this,
				"Illegal use of glyph metric: ",
				m_psymName->FullName());
			return this;
		}

		int gmet = m_psymName->GlyphMetricEngineCodeOp();
		int nValue;
		if (gmet == -1)
		{
			g_errorList.AddError(2128, this,
				"Invalid glyph metric: ",
				m_psymName->FullName());
			nValue = 0;
		}
		else
		{
			int nActual = g_cman.ActualForPseudo(wGlyphID);
			if (nActual == 0)
				nActual = wGlyphID;
			nValue = pfont->GetGlyphMetric(nActual, GlyphMetric(gmet), this);
		}

		if (m_pexpSelector)
		{
			// Can't resolve.
			*pfCanSub = false;
			return NULL;
		}

		GdlNumericExpression * pexpRet = new GdlNumericExpression(nValue);
		pexpRet->CopyLineAndFile(*this);
		*pfCanSub = false;
		return pexpRet;
	}
	else if (m_psymName->FitsSymbolType(ksymtGlyphAttr) && pgax)
	{
		nAttrID = m_psymName->InternalID();

		if (!fGAttrDefChk)
		{
			*pfCanSub = false;
			return this;
		}

		if (!pgax->Defined(wGlyphID, nAttrID))
		{
			g_errorList.AddError(2129, this,
				"The glyph attribute ",
				m_psymName->FullName(),
				" is not defined for glyph 0x",
				GdlGlyphDefn::GlyphIDString(wGlyphID));
			return NULL;
		}
		else if (setpsym.IsMember(m_psymName))
		{
			g_errorList.AddError(2130, this,
				"Circular definition of glyph attribute ",
				m_psymName->FullName(),
				" for glyph 0x",
				GdlGlyphDefn::GlyphIDString(wGlyphID));
			return NULL;
		}
		else
		{
			setpsym.Insert(m_psymName);
			GdlExpression * pexp;
			GdlExpression * pexpRet;
			pexp = pgax->GetExpression(wGlyphID, nAttrID);
			pexpRet = pexp->SimplifyAndUnscale(pgax, wGlyphID, setpsym, pfont,
				fGAttrDefChk, pfCanSub);
			if (pexpRet)
				pexpRet = pexpRet->Clone();
			setpsym.Delete(m_psymName);	// in case we look up this attribute again in the
										// same outer expression
			*pfCanSub = false;
			return pexpRet;
		}
	}

	*pfCanSub = true;
	return this;
}

/*--------------------------------------------------------------------------------------------*/
GdlExpression * GdlNumericExpression::SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
	utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont, bool fGAttrDefChk,
	bool * pfCanSub)
{
	m_nValue = pfont->ScaledToAbsolute(m_nValue, m_munits);
	m_munits = (m_munits == kmunitNone) ? kmunitNone : kmunitUnscaled;
	*pfCanSub = true;
	return this;
}

/*--------------------------------------------------------------------------------------------*/
GdlExpression * GdlSlotRefExpression::SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
	utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont, bool fGAttrDefChk,
	bool * pfCanSub)
{
	*pfCanSub = true;
	return this;
}

/*--------------------------------------------------------------------------------------------*/
GdlExpression * GdlStringExpression::SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
	utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont, bool fGAttrDefChk,
	bool * pfCanSub)
{
	*pfCanSub = true;
	return this;
}


/*----------------------------------------------------------------------------------------------
	Change the value 0 in the expression to a special value. This is needed for the value
	of gpoint attributes, because we consistently use 0 to mean "no legitimate value".
----------------------------------------------------------------------------------------------*/
void GdlNumericExpression::SetSpecialZero()
{
	Assert(m_nValue == 0);
	m_nValue = kGpointZero;
}


/*----------------------------------------------------------------------------------------------
	The expression is the value of an attribute setting statement or rule item constraint.
	Check that any glyph attributes accessed are defined for the relevant glyph classes
	(all of the glyph IDs for the input class).
----------------------------------------------------------------------------------------------*/
void GdlUnaryExpression::CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit)
{
	m_pexpOperand->CheckAndFixGlyphAttrsInRules(pcman, vpglfcInClasses, irit);
}

/*--------------------------------------------------------------------------------------------*/
void GdlBinaryExpression::CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit)
{
	m_pexpOperand1->CheckAndFixGlyphAttrsInRules(pcman, vpglfcInClasses, irit);
	m_pexpOperand2->CheckAndFixGlyphAttrsInRules(pcman, vpglfcInClasses, irit);
}

/*--------------------------------------------------------------------------------------------*/
void GdlCondExpression::CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit)
{
	m_pexpTest->CheckAndFixGlyphAttrsInRules(pcman, vpglfcInClasses, irit);
	m_pexpTrue->CheckAndFixGlyphAttrsInRules(pcman, vpglfcInClasses, irit);
	m_pexpFalse->CheckAndFixGlyphAttrsInRules(pcman, vpglfcInClasses, irit);
}

/*--------------------------------------------------------------------------------------------*/
void GdlLookupExpression::CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit)
{
	char rgchItem[20];
	itoa(irit+1, rgchItem, 10);
	if (m_psymName->FitsSymbolType(ksymtGlyphAttr)
		&& !m_psymName->FitsSymbolType(ksymtSlotAttr))	// treat it like a slot attribute
	{													// if it could be either
		int nSel;
		if (m_pexpSelector)
			nSel = m_pexpSelector->m_srNumber - 1;	// selectors are 1-based
		else
			nSel = irit;
		if (nSel < 0 || nSel >= vpglfcInClasses.Size())
		{
			g_errorList.AddError(2131, this,
				"Item ", rgchItem,
				": glyph attribute selector out of range");
			return;
		}
		else if (vpglfcInClasses[nSel] == NULL)
		{
			g_errorList.AddError(2132, this,
				"Item ", rgchItem,
				": no input class for selector");
			return;
		}

		m_nInternalID = m_psymName->InternalID();
		m_nSubIndex = -1;	// never needed for glyph attributes

		//	Check that the class associated with the glyph attribute matches
		//	the class in the rule.
		//Symbol psymBaseClass = m_psymName->BaseClassDefn();
		//if (psymBaseClass)
		//{
		//	GdlGlyphClassDefn * pglfc = psymBaseClass->GlyphClassDefnData();
		//	if (pglfc != vpglfcInClasses[nSel])
		//	{
		//		g_errorList.AddWarning(2522, this,
		//			"Item ", rgchItem,
		//			": Invalid glyph attribute: ",
		//			m_psymName->FullName());
		//	}
		//}

		if (m_psymName->IsGeneric())
			// Then it must apply to all the glyphs in the class.
			vpglfcInClasses[nSel]->CheckExistenceOfGlyphAttr(this,
				pcman->SymbolTable(), pcman->GlyphAttrMatrix(), m_psymName);
	}
}

/*------------------------------------------------------s--------------------------------------*/
void GdlNumericExpression::CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit)
{
}

/*--------------------------------------------------------------------------------------------*/
void GdlSlotRefExpression::CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit)
{
}

/*--------------------------------------------------------------------------------------------*/
void GdlStringExpression::CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit)
{
}


/*----------------------------------------------------------------------------------------------
	The expression is the value of an attribute setting statement, specifically setting
	an attachment point.
	Check that the attachment points are completely defined for the relevant glyph classes
	(all of the glyph IDs for the input class).
	Arguments:
		irit			- for an attach.at statement, the value of the attach.to setting;
							for an attach.with, the current item; in other words, the item
							for which the glyph attribute needs to be defined
		pfXY			- set to true if there is a glyph that needs to use x / y
		pfGpoint		- set to true if there is a glyph that needs to use gpoint
							note that both *pfXY and *pfGpoint could be true
----------------------------------------------------------------------------------------------*/
void GdlUnaryExpression::CheckCompleteAttachmentPoint(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
	bool * pfXY, bool * pfGpoint)
{
	m_pexpOperand->CheckCompleteAttachmentPoint(pcman, vpglfcInClasses, irit, pfXY, pfGpoint);
}

/*--------------------------------------------------------------------------------------------*/
void GdlBinaryExpression::CheckCompleteAttachmentPoint(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
	bool * pfXY, bool * pfGpoint)
{
	m_pexpOperand1->CheckCompleteAttachmentPoint(pcman, vpglfcInClasses, irit, pfXY, pfGpoint);
	m_pexpOperand2->CheckCompleteAttachmentPoint(pcman, vpglfcInClasses, irit, pfXY, pfGpoint);
}

/*--------------------------------------------------------------------------------------------*/
void GdlCondExpression::CheckCompleteAttachmentPoint(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
	bool * pfXY, bool * pfGpoint)
{
	m_pexpTest->CheckCompleteAttachmentPoint(pcman, vpglfcInClasses, irit, pfXY, pfGpoint);
	m_pexpTrue->CheckCompleteAttachmentPoint(pcman, vpglfcInClasses, irit, pfXY, pfGpoint);
	m_pexpFalse->CheckCompleteAttachmentPoint(pcman, vpglfcInClasses, irit, pfXY, pfGpoint);
}

/*--------------------------------------------------------------------------------------------*/
void GdlLookupExpression::CheckCompleteAttachmentPoint(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
	bool * pfXY, bool * pfGpoint)
{
	if (m_psymName->FitsSymbolType(ksymtGlyphAttr))
	{
		int nSel;
		if (m_pexpSelector)
			nSel = m_pexpSelector->m_srNumber - 1;	// selectors are 1-based
		else
			nSel = irit;
		if (nSel < 0 || nSel >= vpglfcInClasses.Size())
		{
			char rgch[20];
			itoa(irit+1, rgch, 10);
			g_errorList.AddError(2133, this,
				"Item ", rgch,
				"slot selector on glyph attribute ",
				m_psymName->FullName(),
				" out of range");
			return;
		}
		else if (vpglfcInClasses[nSel] == NULL)
		{
			char rgch[20];
			itoa(irit+1, rgch, 10);
			g_errorList.AddError(2134, this,
				"Item ", rgch,
				": no input class for selector on glyph attribute ",
				m_psymName->FullName());
			return;
		}

		vpglfcInClasses[nSel]->CheckCompleteAttachmentPoint(this,
			pcman->SymbolTable(), pcman->GlyphAttrMatrix(), m_psymName->BasePoint(),
			pfXY, pfGpoint);
	}
	else
		LookupExpCheck(false);
}

/*--------------------------------------------------------------------------------------------*/
void GdlNumericExpression::CheckCompleteAttachmentPoint(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
	bool * pfXY, bool * pfGpoint)
{
}

/*--------------------------------------------------------------------------------------------*/
void GdlSlotRefExpression::CheckCompleteAttachmentPoint(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
	bool * pfXY, bool * pfGpoint)
{
}

/*--------------------------------------------------------------------------------------------*/
void GdlStringExpression::CheckCompleteAttachmentPoint(GrcManager * pcman,
	Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
	bool * pfXY, bool * pfGpoint)
{
}


/*----------------------------------------------------------------------------------------------
	Create expressions corresponding to the fields of a point. For instance, if the recipient
	is "@2.somePoint", return "@2.somePoint.x", "@2.somePoint.y", etc. Any of the arguments
	(but most usefully the gpoint, xoffset,	and yoffset fields) may be NULL.

	Return false if this is not the kind of expression that has obvious equivalents.
	Specifically, only glyph attribute lookups in fact do.

	Note that if the symbol for one of the fields does not exist in the symbol table,
	then there is no slot attribute setting making use of it, so we can afford to just
	ignore it at this point. Eventually	we may detect an error due to the omission.
----------------------------------------------------------------------------------------------*/
bool GdlExpression::PointFieldEquivalents(GrcManager * pcman,
	GdlExpression ** ppexpX, GdlExpression ** ppexpY,
	GdlExpression ** ppexpGpoint, GdlExpression ** ppexpXoffset, GdlExpression ** ppexpYoffset)
{
	//	Only glyph attribute lookups can handle this.
	return false;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlLookupExpression::PointFieldEquivalents(GrcManager * pcman,
	GdlExpression ** ppexpX, GdlExpression ** ppexpY,
	GdlExpression ** ppexpGpoint, GdlExpression ** ppexpXoffset, GdlExpression ** ppexpYoffset)
{
	Assert(m_psymName);

	if (m_psymName->FitsSymbolType(ksymtSlotAttr))
	{
		return false;
	}
	if (m_psymName->FitsSymbolType(ksymtFeature))
	{
		return false;
	}

	if (!m_psymName->HasSubFields())
	{
		if (ppexpX) *ppexpX = NULL;
		if (ppexpY) *ppexpY = NULL;
		if (ppexpGpoint) *ppexpGpoint = NULL;
		if (ppexpXoffset) *ppexpXoffset = NULL;
		if (ppexpYoffset) *ppexpYoffset = NULL;
		return true;
	}

	//	Generally the symbol type will be ksymtInvalidGlyphAttr, because the name itself is
	//	undefined, only the subfields are defined.

	if (ppexpX)
	{
		Symbol psymX = m_psymName->SubField("x");
		if (psymX && psymX->FitsSymbolType(ksymtGlyphAttr))
		{
			GdlLookupExpression * pexpX = new GdlLookupExpression(*this);
			pexpX->m_psymName = psymX;
			pexpX->m_nInternalID = psymX->InternalID();
			*ppexpX = pexpX;
		}
		else
			*ppexpX = NULL;
	}

	if (ppexpY)
	{
		Symbol psymY = m_psymName->SubField("y");
		if (psymY && psymY->FitsSymbolType(ksymtGlyphAttr))
		{
			GdlLookupExpression * pexpY = new GdlLookupExpression(*this);
			pexpY->m_psymName = psymY;
			pexpY->m_nInternalID = psymY->InternalID();
			*ppexpY = pexpY;
		}
		else
			*ppexpY = NULL;
	}

	if (ppexpGpoint)
	{
		Symbol psymGpoint = m_psymName->SubField("gpoint");
		if (psymGpoint && psymGpoint->FitsSymbolType(ksymtGlyphAttr))
		{
			GdlLookupExpression * pexpGpoint = new GdlLookupExpression(*this);
			pexpGpoint->m_psymName = psymGpoint;
			pexpGpoint->m_nInternalID = psymGpoint->InternalID();
			*ppexpGpoint = pexpGpoint;
		}
		else
			*ppexpGpoint = NULL;
	}

	if (ppexpXoffset)
	{
		Symbol psymXoffset = m_psymName->SubField("xoffset");
		if (psymXoffset && psymXoffset->FitsSymbolType(ksymtGlyphAttr))
		{
			GdlLookupExpression * pexpXoffset = new GdlLookupExpression(*this);
			pexpXoffset->m_psymName = psymXoffset;
			pexpXoffset->m_nInternalID = psymXoffset->InternalID();
			*ppexpXoffset = pexpXoffset;
		}
		else
			*ppexpXoffset = NULL;
	}

	if (ppexpYoffset)
	{
		Symbol psymYoffset = m_psymName->SubField("yoffset");
		if (psymYoffset && psymYoffset->FitsSymbolType(ksymtGlyphAttr))
		{
			GdlLookupExpression * pexpYoffset = new GdlLookupExpression(*this);
			pexpYoffset->m_psymName = psymYoffset;
			pexpYoffset->m_nInternalID = psymYoffset->InternalID();
			*ppexpYoffset = pexpYoffset;
		}
		else
			*ppexpYoffset = NULL;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Do various checks on the expression that is the value of a slot attribute or a constraint:
	* legal use of slot references
	* not reading a write-only attribute (eg kern)
	* not reading the composite value of anything but a glyph metric
	* valid user-definable slot attribute

	For slot references, check that the expression does not make references to slots that
	are out of range, or that represent line-breaks or inserted items.

	Assumes that the expression has been type-checked, so we do not need to worry about the
	situation of inappropriately performing operations on slot references (eg, adding them
	together), or assigning numerical value where a slot reference is expected.

	Arguments:
		vfLb			- vector of flags for each item in rule, true if it is a line-break
		vfIns			- vector of flags for each item, true if item is an insertion
		fValue			- true if the expression represents a value of an attribute that is
							being set, in which case LB slots are illegal, and we need to
							consider fValueIsInputSlot;
						  false if the expression is simply looking up information from
							the input stream (ie in a constraint), so references to inserted
							slots are illegal.
		fValueIsInputSlot
						- true if the expression is the new value of an attribute and any
							slot references refer to a slot in the input stream (eg,
							comp.X.ref) rather than a slot in the output stream (eg,
							attach.to). In the former case, references to deleted slots
							are legal but refs to inserted slots are not; in the latter case,
							the opposite is true.
----------------------------------------------------------------------------------------------*/
bool GdlUnaryExpression::CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
	Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
	bool fValue, bool fValueIsInputSlot)
{
	return m_pexpOperand->CheckRuleExpression(pfont, prndr, vfLb, vfIns, vfDel,
		fValue, fValueIsInputSlot);
}

/*--------------------------------------------------------------------------------------------*/
bool GdlBinaryExpression::CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
	Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
	bool fValue, bool fValueIsInputSlot)
{
	bool fOkay = m_pexpOperand1->CheckRuleExpression(pfont, prndr, vfLb, vfIns, vfDel,
		fValue, fValueIsInputSlot);

	if (!m_pexpOperand2->CheckRuleExpression(pfont, prndr, vfLb, vfIns, vfDel,
		fValue, fValueIsInputSlot))
	{
		fOkay = false;
	}

	return fOkay;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlCondExpression::CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
	Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
	bool fValue, bool fValueIsInputSlot)
{
	bool fOkay = m_pexpTest->CheckRuleExpression(pfont, prndr, vfLb, vfIns, vfDel,
		false, false);
	if (!m_pexpTrue->CheckRuleExpression(pfont, prndr, vfLb, vfIns, vfDel,
		fValue, fValueIsInputSlot))
	{
		fOkay = false;
	}
	if (!m_pexpFalse->CheckRuleExpression(pfont, prndr, vfLb, vfIns, vfDel,
		fValue, fValueIsInputSlot))
	{
		fOkay = false;
	}
	return fOkay;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlLookupExpression::CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
	Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
	bool fValue, bool fValueIsInputSlot)
{
	Assert(m_psymName->FitsSymbolType(ksymtGlyph) ||
		m_psymName->FitsSymbolType(ksymtSlotAttr) ||
		m_psymName->FitsSymbolType(ksymtFeature) ||
		m_psymName->FitsSymbolType(ksymtInvalid));

	if (m_nClusterLevel != 0 && !m_psymName->FitsSymbolType(ksymtGlyphMetric))
		g_errorList.AddError(2135, this,
			"Composite metrics are only available for glyph metrics");

	if (m_psymName->FitsSymbolType(ksymtSlotAttr))
	{
		if (m_psymName->IsWriteOnlySlotAttr())
			g_errorList.AddError(2136, this,
				"The '",
				m_psymName->FullName(),
				"' attribute is write-only");

		if (m_psymName->IsIndexedSlotAttr())
		{
			if (m_psymName->IsUserDefinableSlotAttr())
			{
				int nIndex = m_psymName->UserDefinableSlotAttrIndex();
				if (nIndex < 0)
					g_errorList.AddError(2137, this,
						"Invalid slot attribute: ", m_psymName->FullName());
				else if (nIndex >= kMaxUserDefinableSlotAttrs)
				{
					char rgch[20];
					itoa(kMaxUserDefinableSlotAttrs, rgch, 10);
					g_errorList.AddError(2138, this,
						"Invalid slot attribute: ", m_psymName->FullName(),
						"; maximum is ", rgch);
				}
				else
				{
					prndr->SetNumUserDefn(nIndex);
				}
			}
			else
			{
				g_errorList.AddError(2139, this,
					"Not permitted to read the '",
					m_psymName->FullName(), "' attribute");
			}
		}
	}

	//	Ignore fValue and fValueIsInputSlot; because this is a lookup expression,
	//	act as if fValue = false.

	int crit = vfLb.Size();

	if (m_pexpSelector)
	{
		int sr = m_pexpSelector->SlotNumber();

		char rgchSlotNumber[20];
		itoa(sr, rgchSlotNumber, 10);

		if (sr < 1 || sr > crit)
		{
			g_errorList.AddError(2140, this,
				"Slot selector out of range: @",
				rgchSlotNumber,
				".",
				m_psymName->FullName());
			return false;
		}

		//	Always okay to read the attribute of a line-break item or a deleted item.
		//	Never okay to read the attribute of an inserted item.
		else if (vfIns[sr - 1])
		{
			g_errorList.AddError(2141, this,
				"Slot selector indicates an inserted item: @",
				rgchSlotNumber,
				".",
				m_psymName->FullName());
			return false;
		}
	}

	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlNumericExpression::CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
	Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
	bool fValue, bool fValueIsInputSlot)
{
	m_nValue = pfont->ScaledToAbsolute(m_nValue, m_munits);
	m_munits = (m_munits == kmunitNone) ? kmunitNone : kmunitUnscaled;
	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlSlotRefExpression::CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
	Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
	bool fValue, bool fValueIsInputSlot)
{
	char rgchSlotNumber[20];
	itoa(m_srNumber, rgchSlotNumber, 10);

	if (m_srNumber < 1 || m_srNumber > vfLb.Size())
	{
		g_errorList.AddError(2142, this,
			"Slot reference out of range: @",
			rgchSlotNumber);
		return false;
	}

	else if (fValue && vfLb[m_srNumber - 1])
	{
		//	Eg, attach.to = @2 or comp.X.ref = @2, where @2 is a LB slot
		g_errorList.AddError(2143, this,
			"Illegal reference to line-break slot: @",
			rgchSlotNumber);
		return false;
	}

	else if ((!fValue || fValueIsInputSlot) && vfIns[m_srNumber - 1])
	{
		//	Eg, @1.bb.width or comp.X.ref = @1, where @1 is being inserted
		g_errorList.AddError(2144, this,
			"Illegal reference to inserted slot: @",
			rgchSlotNumber);
		return false;
	}

	else if (fValue && !fValueIsInputSlot && vfDel[m_srNumber - 1])
	{
		//	Eg, attach.to = @3, where @3 is being deleted
		g_errorList.AddError(2145, this,
			"Illegal reference to deleted slot: @",
			rgchSlotNumber);
		return false;
	}

	else
		return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlStringExpression::CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
	Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
	bool fValue, bool fValueIsInputSlot)
{
	//	By this point any value string values (eg, values of the "lang" feature) should
	//	have been converted to integers.
	g_errorList.AddError(2146, this,
		"Illegal expression: ",
		m_staValue);

	return false;
}


/*----------------------------------------------------------------------------------------------
	Adjust slot references based on the fact that we have prepended ANYs to the
	beginning of the rule.
----------------------------------------------------------------------------------------------*/
void GdlUnaryExpression::AdjustSlotRefsForPreAnys(int critPrependedAnys)
{
	m_pexpOperand->AdjustSlotRefsForPreAnys(critPrependedAnys);
}

/*--------------------------------------------------------------------------------------------*/
void GdlBinaryExpression::AdjustSlotRefsForPreAnys(int critPrependedAnys)
{
	m_pexpOperand1->AdjustSlotRefsForPreAnys(critPrependedAnys);
	m_pexpOperand2->AdjustSlotRefsForPreAnys(critPrependedAnys);
}

/*--------------------------------------------------------------------------------------------*/
void GdlCondExpression::AdjustSlotRefsForPreAnys(int critPrependedAnys)
{
	m_pexpTest->AdjustSlotRefsForPreAnys(critPrependedAnys);
	m_pexpTrue->AdjustSlotRefsForPreAnys(critPrependedAnys);
	m_pexpFalse->AdjustSlotRefsForPreAnys(critPrependedAnys);

}

/*--------------------------------------------------------------------------------------------*/
void GdlLookupExpression::AdjustSlotRefsForPreAnys(int critPrependedAnys)
{
	if (m_pexpSelector)
		m_pexpSelector->AdjustSlotRefsForPreAnys(critPrependedAnys);
}

/*--------------------------------------------------------------------------------------------*/
void GdlNumericExpression::AdjustSlotRefsForPreAnys(int critPrependedAnys)
{
}

/*--------------------------------------------------------------------------------------------*/
void GdlSlotRefExpression::AdjustSlotRefsForPreAnys(int critPrependedAnys)
{
	m_srNumber += critPrependedAnys;
}

/*--------------------------------------------------------------------------------------------*/
void GdlStringExpression::AdjustSlotRefsForPreAnys(int critPrependedAnys)
{
}


/*----------------------------------------------------------------------------------------------
	Adjust any slot references to use the corresponding value of the vector, which is
	either an input index or an output index.
	Arguments:
		prit			- pointer to owning rule item, when expression is an attach.to command;
							we store the adjusted index of the target of the attachment in
							the rule item for future reference.
----------------------------------------------------------------------------------------------*/
void GdlUnaryExpression::AdjustToIOIndices(Vector<int> & virit, GdlRuleItem * prit)
{
	Assert(prit == NULL);
	m_pexpOperand->AdjustToIOIndices(virit, prit);
}

/*--------------------------------------------------------------------------------------------*/
void GdlBinaryExpression::AdjustToIOIndices(Vector<int> & virit, GdlRuleItem * prit)
{
	Assert(prit == NULL);
	m_pexpOperand1->AdjustToIOIndices(virit, prit);
	m_pexpOperand2->AdjustToIOIndices(virit, prit);
}

/*--------------------------------------------------------------------------------------------*/
void GdlCondExpression::AdjustToIOIndices(Vector<int> & virit, GdlRuleItem * prit)
{
	Assert(prit == NULL);
	m_pexpTest->AdjustToIOIndices(virit, prit);
	m_pexpTrue->AdjustToIOIndices(virit, prit);
	m_pexpFalse->AdjustToIOIndices(virit, prit);

}

/*--------------------------------------------------------------------------------------------*/
void GdlLookupExpression::AdjustToIOIndices(Vector<int> & virit, GdlRuleItem * prit)
{
	Assert(prit == NULL);
	if (m_pexpSelector)
		m_pexpSelector->AdjustToIOIndices(virit, prit);
}

/*--------------------------------------------------------------------------------------------*/
void GdlNumericExpression::AdjustToIOIndices(Vector<int> & virit, GdlRuleItem * prit)
{
	Assert(prit == NULL);
}

/*--------------------------------------------------------------------------------------------*/
void GdlSlotRefExpression::AdjustToIOIndices(Vector<int> & virit, GdlRuleItem * prit)
{
	if (m_srNumber == 0)
		m_nIOIndex = -1;
	else
		m_nIOIndex = virit[m_srNumber - 1];
	if (prit)
		prit->SetAttachTo(m_nIOIndex);
}

/*--------------------------------------------------------------------------------------------*/
void GdlStringExpression::AdjustToIOIndices(Vector<int> & virit, GdlRuleItem * prit)
{
	Assert(prit == NULL);
}


/*----------------------------------------------------------------------------------------------
	Convert a string-plus-codepage into a Unicode string.
----------------------------------------------------------------------------------------------*/
StrUni GdlStringExpression::ConvertToUnicode()
{
	int cch = m_staValue.Length();
	const schar * pchs = m_staValue.Chars();
	utf16 * pchw = new utf16[cch];
	Platform_8bitToUnicode(m_nCodepage, pchs, cch, pchw, cch);
#ifdef GR_FW
	StrUni stuRet((wchar_t*)pchw, cch); // something about the new VS compiler needs this :-(
#else
	StrUni stuRet(pchw, cch);
#endif // GR_FW
	delete[] pchw;
	return stuRet;
}


/*----------------------------------------------------------------------------------------------
	Return true if the expression is testing the justification status (JustifyLevel or
	JustifyMode).
----------------------------------------------------------------------------------------------*/
void GdlUnaryExpression::MaxJustificationLevel(int * pnLevel)
{
	m_pexpOperand->MaxJustificationLevel(pnLevel);
}

/*--------------------------------------------------------------------------------------------*/
void GdlBinaryExpression::MaxJustificationLevel(int * pnLevel)
{
	m_pexpOperand1->MaxJustificationLevel(pnLevel);
	m_pexpOperand2->MaxJustificationLevel(pnLevel);
}

/*--------------------------------------------------------------------------------------------*/
void GdlCondExpression::MaxJustificationLevel(int * pnLevel)
{
	m_pexpTest->MaxJustificationLevel(pnLevel);
	m_pexpTrue->MaxJustificationLevel(pnLevel);
	m_pexpFalse->MaxJustificationLevel(pnLevel);
}

/*--------------------------------------------------------------------------------------------*/
void GdlLookupExpression::MaxJustificationLevel(int * pnLevel)
{
	int n = m_psymName->JustificationLevel();
	*pnLevel = max(*pnLevel, n);
}

/*--------------------------------------------------------------------------------------------*/
void GdlNumericExpression::MaxJustificationLevel(int * pnLevel)
{
}

/*--------------------------------------------------------------------------------------------*/
void GdlSlotRefExpression::MaxJustificationLevel(int * pnLevel)
{
}

/*--------------------------------------------------------------------------------------------*/
void GdlStringExpression::MaxJustificationLevel(int * pnLevel)
{
}


/*----------------------------------------------------------------------------------------------
	Return true if the expression is testing the justification status (JustifyLevel or
	JustifyMode).
----------------------------------------------------------------------------------------------*/
bool GdlUnaryExpression::TestsJustification()
{
	return m_pexpOperand->TestsJustification();
}

/*--------------------------------------------------------------------------------------------*/
bool GdlBinaryExpression::TestsJustification()
{
	if (m_pexpOperand1->TestsJustification())
		return true;

	return m_pexpOperand2->TestsJustification();
}

/*--------------------------------------------------------------------------------------------*/
bool GdlCondExpression::TestsJustification()
{
	if (m_pexpTest->TestsJustification())
		return true;

	if (m_pexpTrue->TestsJustification())
		return true;

	if (m_pexpFalse)
		return m_pexpFalse->TestsJustification();

	return false;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlLookupExpression::TestsJustification()
{
	if (!m_psymName->FitsSymbolType(ksymtProcState))
		return false;

	if (m_psymName->FullName() == "JustifyMode")
		return true;
	//if (m_psymName->FullName() == "JustifyLevel")
	//	return true;

	return false;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlNumericExpression::TestsJustification()
{
	return false;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlSlotRefExpression::TestsJustification()
{
	return false;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlStringExpression::TestsJustification()
{
	return false;
}

/*----------------------------------------------------------------------------------------------
	Check that the expression is compatible with the requested version.
	If not, return the version required.
----------------------------------------------------------------------------------------------*/
bool GdlUnaryExpression::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	return m_pexpOperand->CompatibleWithVersion(fxdVersion, pfxdNeeded);
}

/*--------------------------------------------------------------------------------------------*/
bool GdlBinaryExpression::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	bool f1 = m_pexpOperand1->CompatibleWithVersion(fxdVersion, pfxdNeeded);
	bool f2 = m_pexpOperand2->CompatibleWithVersion(fxdVersion, pfxdNeeded);
	return (f1 && f2);
}

/*--------------------------------------------------------------------------------------------*/
bool GdlCondExpression::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	bool fTest = m_pexpTest->CompatibleWithVersion(fxdVersion, pfxdNeeded);
	bool fTrue = m_pexpTrue->CompatibleWithVersion(fxdVersion, pfxdNeeded);
	bool fFalse = m_pexpFalse->CompatibleWithVersion(fxdVersion, pfxdNeeded);
	return (fTest && fTrue && fFalse);
}

/*--------------------------------------------------------------------------------------------*/
bool GdlLookupExpression::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	bool fRet = true;
	if (TestsJustification())
	{
		*pfxdNeeded = max(*pfxdNeeded, 0x00020000);
		fRet = false;
	}

	if (m_psymName->IsMeasureAttr())
	{
		*pfxdNeeded = max(*pfxdNeeded, 0x00020000);
		fRet = false;
	}

	if (m_psymName->FitsSymbolType(ksymtGlyphAttr))
	{
		int nID = m_psymName->InternalID();
		if (nID >= 0xFF)
		{
			*pfxdNeeded = max(*pfxdNeeded, 0x00030000);
			fRet = false;
		}
	}

	return fRet;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlNumericExpression::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlSlotRefExpression::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlStringExpression::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	return true;
}


/***********************************************************************************************
	Methods: Compiler
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Translate the expression into engine code and append it to the code block.
	Arguments:
		fxdRuleVersion	- version of rule code to generate
		vbOutput		- vector of bytes, engine code being generated
		iritCurrent		- current rule item index (0-based); -1 if we are in an -if- statement
		pviritInput		- vector of input indices for this rule; only relevant when
							generating constraints; NULL if we are generating actions
		nIIndex			- input index of current slot
		fAttachAt		- true if this the value of an attach.at attribute, in which case
							output a special command that will read the value of the attached
							slot
		iritAttachTo	- value of attach.to attribute for current item (0-based, relative
							to start of rule)
		pnValue			- return the value of the slot ref expression for the benefit of
							calling code that, if it happens to be an attach.to setter,
							needs to decide how to handle the accompanying insert = false;
							only slot references need to worry about it
----------------------------------------------------------------------------------------------*/
void GdlUnaryExpression::GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
	int iritCurrent, Vector<int> * pviritInput, int nIIndex,
	bool fAttachAt, int iritAttachTo, int * pnValue)
{
	m_pexpOperand->GenerateEngineCode(fxdRuleVersion, vbOutput, iritCurrent, pviritInput, nIIndex,
		fAttachAt, iritAttachTo, pnValue);

	StrAnsi staOp = m_psymOperator->FullName();

	if (staOp == "!")
		vbOutput.Push(kopNot);
	else if (staOp == "-")
		vbOutput.Push(kopNeg);
	// eventually, perhaps add kopTrunc8 and kopTrunc16
	else
		Assert(false);
}

/*--------------------------------------------------------------------------------------------*/
void GdlBinaryExpression::GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
	int iritCurrent, Vector<int> * pviritInput, int nIIndex,
	bool fAttachAt, int iritAttachTo, int * pnValue)
{
	int nBogus;
	m_pexpOperand1->GenerateEngineCode(fxdRuleVersion, vbOutput,
		iritCurrent, pviritInput, nIIndex,
		fAttachAt, iritAttachTo, &nBogus);
	m_pexpOperand2->GenerateEngineCode(fxdRuleVersion, vbOutput,
		iritCurrent, pviritInput, nIIndex,
		fAttachAt, iritAttachTo, &nBogus);

	StrAnsi staOp = m_psymOperator->FullName();

	if (staOp == "+")
		vbOutput.Push(kopAdd);
	else if (staOp == "-")
		vbOutput.Push(kopSub);
	else if (staOp == "*")
		vbOutput.Push(kopMul);
	else if (staOp == "/")
		vbOutput.Push(kopDiv);
	else if (staOp == "max")
		vbOutput.Push(kopMax);
	else if (staOp == "min")
		vbOutput.Push(kopMin);
	else if (staOp == "&&")
		vbOutput.Push(kopAnd);
	else if (staOp == "||")
		vbOutput.Push(kopOr);
	else if (staOp == "==")
		vbOutput.Push(kopEqual);
	else if (staOp == "!=")
		vbOutput.Push(kopNotEq);
	else if (staOp == "<")
		vbOutput.Push(kopLess);
	else if (staOp == ">")
		vbOutput.Push(kopGtr);
	else if (staOp == "<=")
		vbOutput.Push(kopLessEq);
	else if (staOp == ">=")
		vbOutput.Push(kopGtrEq);
	else
		Assert(false);
}

/*--------------------------------------------------------------------------------------------*/
void GdlCondExpression::GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
	int iritCurrent, Vector<int> * pviritInput, int nIIndex,
	bool fAttachAt, int iritAttachTo, int * pnValue)
{
	int nBogus;
	m_pexpTest->GenerateEngineCode(fxdRuleVersion, vbOutput, iritCurrent, pviritInput, nIIndex,
		fAttachAt, iritAttachTo, &nBogus);
	m_pexpTrue->GenerateEngineCode(fxdRuleVersion, vbOutput, iritCurrent, pviritInput, nIIndex,
		fAttachAt, iritAttachTo, pnValue);
	m_pexpFalse->GenerateEngineCode(fxdRuleVersion, vbOutput, iritCurrent, pviritInput, nIIndex,
		fAttachAt, iritAttachTo, pnValue);
	vbOutput.Push(kopCond);
}

/*--------------------------------------------------------------------------------------------*/
void GdlLookupExpression::GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
	int iritCurrent, Vector<int> * pviritInput, int nIIndex,
	bool fAttachAt, int iritAttachTo, int * pnValue)
{
	if (m_pexpSimplified)
	{
		m_pexpSimplified->GenerateEngineCode(fxdRuleVersion, vbOutput,
			iritCurrent, pviritInput, nIIndex,
			fAttachAt, iritAttachTo, pnValue);
		return;
	}

	int nSelOffset;
	if (iritCurrent == -1)
	{
		//	In an -if- statement.
		Assert(!m_pexpSelector);
		nSelOffset = 0;	// whatever the current slot of interest is
	}
	else
	{
		int nSel;
		if (m_pexpSelector)
		{
			nSel = m_pexpSelector->m_nIOIndex;
			if (nSel < 0)
				nSel = (nSel + 1) * -1;		// inverse of calculation in
											// GdlSubstitutionItem::AssignIOIndices()
		}
		else
			nSel = iritCurrent;

		// Adjust for constraints when there may be insertions.
		int nIIndex2 = nIIndex;
		if (pviritInput)
		{
			nIIndex2 = (*pviritInput)[nIIndex];
			nSel = (*pviritInput)[nSel];
		}

		nSelOffset = nSel - nIIndex2;
	}

	//	Several slot attributes and glyph attributes have the same name; treat these as
	//	slot attributes, since their values will default to the glyph attributes.
	if (m_psymName->FitsSymbolType(ksymtSlotAttr))
	{
		if (m_psymName->IsIndexedSlotAttr())
		{
			if (m_psymName->IsUserDefinableSlotAttr())
			{
				vbOutput.Push(kopPushISlotAttr);
				vbOutput.Push(m_psymName->SlotAttrEngineCodeOp());
				vbOutput.Push(nSelOffset);
				vbOutput.Push(m_psymName->UserDefinableSlotAttrIndex());
			}
			else
				Assert(false);	// currently no way to look up the value of a
								// component.XXX.ref attr
		}
		else
		{
			vbOutput.Push(kopPushSlotAttr);
			vbOutput.Push(m_psymName->SlotAttrEngineCodeOp());
			vbOutput.Push(nSelOffset);
		}
	}
	else if (m_psymName->FitsSymbolType(ksymtGlyphAttr))
	{
		if (m_psymName->IsIndexedGlyphAttr())
		{
			Assert(false);	// currently no way to look up the value of a component attr;
							// eventually use kopPushIGlyphAttr.
		}
		else
		{
			int nID = m_psymName->InternalID();
			if (fAttachAt)
			{
				Assert(iritAttachTo != -1);
				int nSel = (m_pexpSelector) ? m_pexpSelector->m_nIOIndex : iritAttachTo;

				if (fxdRuleVersion <= 0x00020000)
				{
					// Use old 8-bit version of this command.
					vbOutput.Push(kopPushAttToGAttrV1_2);
					vbOutput.Push(nID);
				}
				else
				{
					vbOutput.Push(kopPushAttToGlyphAttr);
					vbOutput.Push(nID >> 8);
					vbOutput.Push(nID & 0x000000FF);
				}

				vbOutput.Push(nSel - iritAttachTo);	// relative to attach.to target
			}
			else
			{
				if (fxdRuleVersion <= 0x00020000)
				{
					// Use old 8-bit version of this command.
					vbOutput.Push(kopPushGlyphAttrV1_2);
					vbOutput.Push(nID);
				}
				else
				{
					vbOutput.Push(kopPushGlyphAttr);
					vbOutput.Push(nID >> 8);
					vbOutput.Push(nID & 0x000000FF);
				}

				vbOutput.Push(nSelOffset);
			}
		}
	}
	else if (m_psymName->FitsSymbolType(ksymtGlyphMetric))
	{
		if (fAttachAt)
		{
			Assert(iritAttachTo != -1);
			int nSel = (m_pexpSelector) ? m_pexpSelector->m_nIOIndex : iritAttachTo;
			vbOutput.Push(kopPushAttToGlyphMetric);
			vbOutput.Push(m_psymName->GlyphMetricEngineCodeOp());
			vbOutput.Push(nSel - iritAttachTo);	// relative to attach.to target
		}
		else
		{
			vbOutput.Push(kopPushGlyphMetric);
			vbOutput.Push(m_psymName->GlyphMetricEngineCodeOp());
			vbOutput.Push(nSelOffset);
		}
		vbOutput.Push(m_nClusterLevel);
	}
	else if (m_psymName->FitsSymbolType(ksymtFeature))
	{
		Assert(!m_pexpSelector);
		vbOutput.Push(kopPushFeat);
		GdlFeatureDefn * pfeat = m_psymName->FeatureDefnData();
		Assert(pfeat);
		vbOutput.Push(pfeat->InternalID());
		vbOutput.Push(nSelOffset);
	}
	else if (m_psymName->FitsSymbolType(ksymtProcState))
	{
		Assert(!m_pexpSelector);
		vbOutput.Push(kopPushProcState);
		if (m_psymName->FullName() == "JustifyMode")
			vbOutput.Push(kpstatJustifyMode);
		else if (m_psymName->FullName() == "JustifyLevel")
			vbOutput.Push(kpstatJustifyLevel);
		else
			Assert(false);
	}
	else
		Assert(false);
}

/*--------------------------------------------------------------------------------------------*/
void GdlNumericExpression::GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
	int iritCurrent, Vector<int> * pviritInput, int nIIndex,
	bool fAttachAt, int iritAttachTo, int * pnValue)
{
	//	Output most-significant byte first.

	byte b4 = m_nValue & 0x000000FF;
	if ((m_nValue & 0xFFFFFF80) == 0 || (m_nValue & 0xFFFFFF80) == 0xFFFFFF80)
	{
		vbOutput.Push(kopPushByte);
		vbOutput.Push(b4);
	}
	else
	{
		byte b3 = (m_nValue & 0x0000FF00) >> 8;
		if ((m_nValue & 0xFFFF8000) == 0 || (m_nValue & 0xFFFF8000) == 0xFFFF8000)
		{
			vbOutput.Push(kopPushShort);
			vbOutput.Push(b3);
			vbOutput.Push(b4);
		}
		else
		{
			byte b1 = (m_nValue & 0xFF000000) >> 24;
			byte b2 = (m_nValue & 0x00FF0000) >> 16;

			vbOutput.Push(kopPushLong);
			vbOutput.Push(b1);
			vbOutput.Push(b2);
			vbOutput.Push(b3);
			vbOutput.Push(b4);
		}
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlSlotRefExpression::GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
	int iritCurrent, Vector<int> * pviritInput, int nIIndex,
	bool fAttachAt, int iritAttachTo, int * pnValue)
{
	int nOffset = m_nIOIndex - iritCurrent;

	Assert(!pviritInput);	// should not be used for constraints
	if (pviritInput)
		nOffset = (*pviritInput)[m_nIOIndex] - iritCurrent;

	char bOffset = (char)nOffset;
	Assert((int)bOffset == nOffset);	// no truncation error

	//	If this happens to be the value of an attach.to attribute, current or following
	//	slot needs to have insert = false set.
	*pnValue = nOffset;

	vbOutput.Push(kopPushByte);
	vbOutput.Push(bOffset);
}

/*--------------------------------------------------------------------------------------------*/
void GdlStringExpression::GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
	int iritCurrent, Vector<int> * pviritInput, int nIIndex,
	bool fAttachAt, int iritAttachTo, int * pnValue)
{
	//	Should never have string expressions in engine code.
	Assert(false);
}

/*----------------------------------------------------------------------------------------------
	Translate the expression into engine code and append it to the code block.
	Arguments:
		vbOutput		- vector of bytes, engine code being generated
		iritCurrent		- current rule item index (0-based); -1 if we are in an -if- statement
		fAttachAt		- true if this the value of an attach.at attribute, in which case
							output a special command that will read the value of the attached
							slot
		pnValue			- return the value of the slot ref expression for the benefit of
							calling code that, if it happens to be an attach.to setter,
							needs to decide how to handle the accompanying insert = false;
							only slot references need to worry about it
----------------------------------------------------------------------------------------------*/
void GdlUnaryExpression::PrettyPrint(GrcManager * pcman, std::ostream & strmOut)
{
	strmOut << "...";
}

/*--------------------------------------------------------------------------------------------*/
void GdlBinaryExpression::PrettyPrint(GrcManager * pcman, std::ostream & strmOut)
{
	strmOut << "...";
}

/*--------------------------------------------------------------------------------------------*/
void GdlCondExpression::PrettyPrint(GrcManager * pcman, std::ostream & strmOut)
{
	strmOut << "...";
}

/*--------------------------------------------------------------------------------------------*/
void GdlLookupExpression::PrettyPrint(GrcManager * pcman, std::ostream & strmOut)
{
	strmOut << "...";
}

/*--------------------------------------------------------------------------------------------*/
void GdlNumericExpression::PrettyPrint(GrcManager * pcman, std::ostream & strmOut)
{
	strmOut << m_nValue;
}

/*--------------------------------------------------------------------------------------------*/
void GdlSlotRefExpression::PrettyPrint(GrcManager * pcman, std::ostream & strmOut)
{
	strmOut << "@" << m_srNumber;
}

/*--------------------------------------------------------------------------------------------*/
void GdlStringExpression::PrettyPrint(GrcManager * pcman, std::ostream & strmOut)
{
	strmOut << m_staValue.Chars();
}
