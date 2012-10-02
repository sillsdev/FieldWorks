/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Compiler.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Methods to implement the compiler, which generates the final tables and writes them to
	the output file.
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
	Methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Do the pre-compilation tasks for each of the main chunks of data. Return false if
	compilation cannot continue due to an unrecoverable error.
----------------------------------------------------------------------------------------------*/
bool GrcManager::PreCompile(GrcFont * pfont)
{
	if (!PreCompileFeatures(pfont))
		return false;

	if (!PreCompileLanguages(pfont)) // do after features
		return false;

	if (!PreCompileClassesAndGlyphs(pfont))
		return false;

	//	Unscale the extra ascent and extra descent.
	GdlExpression * pexp = m_prndr->ExtraAscent();
	if (pexp)
		pexp->SimplifyAndUnscale(0xFFFF, pfont);
	pexp = m_prndr->ExtraDescent();
	if (pexp)
		pexp->SimplifyAndUnscale(0xFFFF, pfont);

	if (!PreCompileRules(pfont))
		return false;

	return true;
}


bool GrcManager::Compile(GrcFont * pfont)
{
	GenerateFsms();
	CalculateContextOffsets(); // after max-rule-context has been set
	return false;
}


/*----------------------------------------------------------------------------------------------
	Generate the engine code for the constraints and actions of a rule.
----------------------------------------------------------------------------------------------*/
void GdlRule::GenerateEngineCode(GrcManager * pcman, int fxdRuleVersion,
	Vector<byte> & vbActions, Vector<byte> & vbConstraints)
{
	GenerateConstraintEngineCode(pcman, fxdRuleVersion, vbConstraints);
	//	Save the size of the rule constraints from the -if- statements.
	int cbGenConstraint = vbConstraints.Size();

	//	Count the number of unmodified items at the end of the rule; these do not need to
	//	be processed as far as actions go, and the default scan advance position is just
	//	before these.
	int iritLimMod = m_vprit.Size();
	while (iritLimMod > 0 && !dynamic_cast<GdlSetAttrItem *>(m_vprit[iritLimMod - 1]))
		iritLimMod--;

	//	Now iritLimMod is the first item that will not be modified.

	//	Also note the the first item that needs to be processed as far as constraints go is
	//	m_critPrependedAnys.
	//	The first item to be processed as far as actions go is
	//	m_critPrependedAnys + m_critPreModContext.

	bool fSetInsertToFalse = false;
	bool fBackUpOneMore = false;
	int iritFirstModItem = m_critPrependedAnys + m_critPreModContext;
	int irit;
	for (irit = m_critPrependedAnys; irit < m_vprit.Size(); irit++)
	{
		if (iritFirstModItem <= irit && irit < iritLimMod)
		{
			m_vprit[irit]->GenerateActionEngineCode(pcman, fxdRuleVersion, vbActions, this, irit,
				&fSetInsertToFalse);
		}

		m_vprit[irit]->GenerateConstraintEngineCode(pcman, fxdRuleVersion, vbConstraints,
			irit, m_viritInput, iritFirstModItem);
	}
	if (fSetInsertToFalse)
	{
		//	Have to modify the first item beyond the scan advance position, in order to
		//	set insert = false (due to some attachment). So here we create the code to
		//	set the attribute, and then back up one to get the scan advance position back
		//	to where it should be.
		m_vprit[iritLimMod]->GenerateActionEngineCode(pcman, fxdRuleVersion, vbActions, this, irit,
			&fSetInsertToFalse);
		Assert(!fSetInsertToFalse);
		m_vprit[iritLimMod]->GenerateConstraintEngineCode(pcman, fxdRuleVersion, vbConstraints,
			irit, m_viritInput, iritFirstModItem);
		fBackUpOneMore = true;
	}

	if (vbConstraints.Size() == 0)
	{ }	// vbConstraints.Push(kopRetTrue); -- no, leave empty
	else
		vbConstraints.Push(kopPopRet);

	if (m_nOutputAdvance == -1)
	{
		if (fBackUpOneMore)
		{
			//	Return -1.
			vbActions.Push(kopPushByte);
			vbActions.Push(0xFF);
			vbActions.Push(kopPopRet);
		}
		else
			//	Push return-zero, meaning don't adjust the scan position.
			vbActions.Push(kopRetZero);
	}
	else
	{
		//	Push a command to return the amount to adjust the scan position--
		//	forward or backward.

		Assert(m_nDefaultAdvance != -1);	// calculated set in GdlRule::HasReprocessing()

		int nAdvanceOffset = m_nOutputAdvance - m_nDefaultAdvance;
		Assert((abs(nAdvanceOffset) & 0xFFFFFF00) == 0);	// check for trucation error

		if (fBackUpOneMore)
			nAdvanceOffset--;

		vbActions.Push(kopPushByte);
		vbActions.Push((char)nAdvanceOffset);
		vbActions.Push(kopPopRet);
	}
}

/*----------------------------------------------------------------------------------------------
	Generate engine code for the constraints of a given rule that were in -if- statements,
	minus the final pop-and-return command.
----------------------------------------------------------------------------------------------*/
void GdlRule::GenerateConstraintEngineCode(GrcManager *pcman, int fxdRuleVersion,
	Vector<byte> & vbOutput)
{
	if (m_vpexpConstraints.Size() == 0)
	{
		return;
	}

	//	'and' all the constraints together; the separate constraints come from separate
	//	-if- or -elseif- statements.
	m_vpexpConstraints[0]->GenerateEngineCode(fxdRuleVersion, vbOutput,
		-1, NULL, -1, false, -1, false);
	for (int ipexp = 1; ipexp < m_vpexpConstraints.Size(); ipexp++)
	{
		m_vpexpConstraints[ipexp]->GenerateEngineCode(fxdRuleVersion, vbOutput,
			-1, NULL, -1, false, -1, false);
		vbOutput.Push(kopAnd);
	}
}


/*----------------------------------------------------------------------------------------------
	Generate engine code for the constraints of a given rule item.
	Arguments:
		vbOutput			- buffer containing engine code already generated from -if-
								statements, minus the final pop-and-return command.
		viritInput			- input indices for items of this rule
		irit				- index of item
----------------------------------------------------------------------------------------------*/
void GdlRuleItem::GenerateConstraintEngineCode(GrcManager * pcman, int fxdRuleVersion,
	Vector<byte> & vbOutput,
	int irit, Vector<int> & viritInput, int iritFirstModItem)
{
	if (!m_pexpConstraint)
	{
		return;
	}

	bool fNeedAnd = vbOutput.Size() > 0;	// need to 'and' rule item constraints with
											// -if- condition(s)

	bool fInserting = (m_psymInput->FitsSymbolType(ksymtSpecialUnderscore));
	Assert(!fInserting || dynamic_cast<GdlSubstitutionItem *>(this));

	char iritByte = viritInput[irit];
	Assert((int)iritByte == viritInput[irit]);	// no truncation error
	Assert(viritInput[irit] >= 0);	// not an inserted item

	vbOutput.Push(kopCntxtItem);
	vbOutput.Push(iritByte - iritFirstModItem);
	vbOutput.Push(0); // place holder
	int ibSkipLoc = vbOutput.Size();
	m_pexpConstraint->GenerateEngineCode(fxdRuleVersion, vbOutput, irit, &viritInput, irit,
		fInserting, -1, false);

	//	Go back and fill in number of bytes to skip if we are not at the
	//	appropriate context item.
	vbOutput[ibSkipLoc - 1] = vbOutput.Size() - ibSkipLoc;

	if (fNeedAnd)
		vbOutput.Push(kopAnd);
}

/*----------------------------------------------------------------------------------------------
	Generate the engine code for the constraints of a pass.
----------------------------------------------------------------------------------------------*/
void GdlPass::GenerateEngineCode(GrcManager * pcman, int fxdRuleVersion, Vector<byte> & vbOutput)
{
	if (m_vpexpConstraints.Size() == 0)
	{
		return;
	}

	//	'and' all the constraints together; multiple constraints result from an -else if-
	//	structure.
	m_vpexpConstraints[0]->GenerateEngineCode(fxdRuleVersion, vbOutput,
		-1, NULL, -1, false, -1, false);
	for (int ipexp = 1; ipexp < m_vpexpConstraints.Size(); ipexp++)
	{
		m_vpexpConstraints[ipexp]->GenerateEngineCode(fxdRuleVersion, vbOutput,
			-1, NULL, -1, false, -1, false);
		vbOutput.Push(kopAnd);
	}
	vbOutput.Push(kopPopRet);
}

/*----------------------------------------------------------------------------------------------
	Generate engine code to perform the actions for a given item.
----------------------------------------------------------------------------------------------*/
void GdlRuleItem::GenerateActionEngineCode(GrcManager * pcman, int fxdRuleVersion,
	Vector<byte> & vbOutput,
	GdlRule * prule, int irit, bool * pfSetInsertToFalse)
{
	if (*pfSetInsertToFalse)
	{
		vbOutput.Push(kopPutCopy);
		vbOutput.Push(0);
		GenerateInsertEqualsFalse(vbOutput);
		*pfSetInsertToFalse = false;
		vbOutput.Push(kopNext);
	}
	else
		//	Nothing special is happening; just pass the item through unchanged.
		vbOutput.Push(kopCopyNext);
}

/*--------------------------------------------------------------------------------------------*/
void GdlSetAttrItem::GenerateActionEngineCode(GrcManager * pcman, int fxdRuleVersion,
	Vector<byte> & vbOutput,
	GdlRule * prule, int irit, bool * pfSetInsertToFalse)
{
	if (m_vpavs.Size() == 0 && !*pfSetInsertToFalse)
		vbOutput.Push(kopCopyNext);
	else
	{
		int nIIndex = m_nInputIndex;
		nIIndex = (nIIndex < 0) ? (nIIndex + 1) * -1 : nIIndex;

		vbOutput.Push(kopPutCopy);
		vbOutput.Push(0);
		if (*pfSetInsertToFalse)
			GenerateInsertEqualsFalse(vbOutput);
		*pfSetInsertToFalse = GenerateAttrSettingCode(pcman, fxdRuleVersion, vbOutput,
			irit, nIIndex);
		vbOutput.Push(kopNext);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlSubstitutionItem::GenerateActionEngineCode(GrcManager * pcman, int fxdRuleVersion,
	Vector<byte> & vbOutput,
	GdlRule * prule, int irit, bool * pfSetInsertToFalse)
{
	bool fInserting = (m_psymInput->FitsSymbolType(ksymtSpecialUnderscore));
	bool fDeleting = (m_psymOutput->FitsSymbolType(ksymtSpecialUnderscore));
	Assert(!fInserting || !fDeleting);

	int nIIndex = m_nInputIndex;
	nIIndex = (nIIndex < 0) ? (nIIndex + 1) * -1 : nIIndex;
	if (fInserting)
		//	Because we haven't "got" the current slot yet (when inserting we do a peek,
		//	not a "get").
		nIIndex--;

	//	Generate the code to insert, delete, replace, etc.
	if (fDeleting)
		//	Note that it's kind of strange to be setting attributes or associations for
		//	deleted objects, but it's not an error and we've already given a warning.
		vbOutput.Push(kopDelete);
	else
	{
		if (fInserting)
			vbOutput.Push(kopInsert);

		if (m_psymOutput->FitsSymbolType(ksymtSpecialAt))
		{
			//	Direct copy.
			int bOffset = (m_nSelector == -1) ? 0 : m_nSelector - nIIndex;
			Assert((abs(bOffset) & 0xFFFFFF00) == 0);	// check for truncation error
			vbOutput.Push(kopPutCopy);
			vbOutput.Push((char)bOffset);
		}
		else
		{
			Assert(m_psymOutput->FitsSymbolType(ksymtClass));
			GdlGlyphClassDefn * pglfcOutput = m_psymOutput->GlyphClassDefnData();
			Assert(pglfcOutput);

			int op;
			int nSel = (m_pexpSelector) ? m_pexpSelector->SlotNumber() - 1 : irit;
			GdlRuleItem * pritSel = prule->Item(nSel);
			Symbol psymSel = pritSel->m_psymInput;
			GdlGlyphClassDefn * pglfcSel = psymSel->GlyphClassDefnData();
			//	We're not doing a substitution based on correspondences within classes, but
			//	rather a simple replacement, under the following circumstances:
			if (psymSel->FitsSymbolType(ksymtSpecialUnderscore))
				//	(a) there is no selector class
				op = kopPutGlyph;
			else if (pglfcSel && pglfcSel->GlyphIDCount() == 0)
				//	(b) the selector class has no glyphs
				op = kopPutGlyph;
			else if (pglfcOutput && pglfcOutput->GlyphIDCount() <= 1)
				//	(c) there is only one glyph in the output class
				op = kopPutGlyph;
			else
			{
				//	Otherwise we're doing a replacement of a glyph from the selector class
				//	with the corresponding glyph from the output class.
				Assert(pglfcSel);
				op = kopPutSubs;
			}
			if (fxdRuleVersion <= 0x00020000)
			{
				// Use old 8-bit versions of these commands.
				switch (op)
				{
				case kopPutGlyph:	op = kopPutGlyphV1_2;	break;
				case kopPutSubs:	op = kopPutSubsV1_2;	break;
				default: break;
				}
			}
			vbOutput.Push(op);

			int nOutputID = pglfcOutput->ReplcmtOutputID();
			Assert(nOutputID >= 0);

			switch (op)
			{
			case kopPutGlyph:
				vbOutput.Push(nOutputID >> 8);
				vbOutput.Push(nOutputID & 0x000000FF);
				break;
			case kopPutGlyphV1_2:
				vbOutput.Push(nOutputID);
				break;
			case kopPutSubs:
			case kopPutSubsV1_2:
				{
					int nSelIO = (m_nSelector == -1) ? nIIndex : m_nSelector;
					int bSelOffset = nSelIO - nIIndex;
					Assert((abs(bSelOffset) & 0xFFFF0000) == 0);	// check for truncation error

					Assert(pglfcSel->ReplcmtInputID() >= 0);

					vbOutput.Push((char)bSelOffset);

					int nInputID = pglfcSel->ReplcmtInputID();
					if (op == kopPutSubsV1_2)
					{
						vbOutput.Push(nInputID);
						vbOutput.Push(nOutputID);
					}
					else
					{
						vbOutput.Push(nInputID >> 8);
						vbOutput.Push(nInputID & 0x000000FF);
						vbOutput.Push(nOutputID >> 8);
						vbOutput.Push(nOutputID & 0x000000FF);
					}
					break;
				}
			default:
				Assert(false);
				break;
			}
		}
	}

	//	Generate the code to set the associations.
	if (m_vnAssocs.Size() > 0)
	{
		vbOutput.Push(kopAssoc);
		vbOutput.Push(m_vnAssocs.Size());
		for (int in = 0; in < m_vnAssocs.Size(); in++)
		{
			Assert(m_vnAssocs[in] >= 0);	// can't associate with an inserted item
			int bAssocOffset = m_vnAssocs[in] - nIIndex;
			Assert((abs(bAssocOffset) & 0xFFFFFF00) == 0);	// check for truncation error

			vbOutput.Push((char)bAssocOffset);
		}
	}

	//	Generate the code to set the attributes.
	if (*pfSetInsertToFalse)
		GenerateInsertEqualsFalse(vbOutput);

	*pfSetInsertToFalse = GenerateAttrSettingCode(pcman, fxdRuleVersion, vbOutput, irit, nIIndex);

	//	Go on to the next slot.
	vbOutput.Push(kopNext);
}


/*----------------------------------------------------------------------------------------------
	Generate engine code to set slot attributes. Return true if we need to set
	insert = false on the following item (because this item makes a forward attachment).
----------------------------------------------------------------------------------------------*/
bool GdlSetAttrItem::GenerateAttrSettingCode(GrcManager * pcman, int fxdRuleVersion,
	Vector<byte> & vbOutput,
	int irit, int nIIndex)
{
	bool fAttachForward = false;
	for (int ipavs = 0; ipavs < m_vpavs.Size(); ipavs++)
	{
		if (m_vpavs[ipavs]->GenerateAttrSettingCode(pcman, fxdRuleVersion, vbOutput,
			irit, nIIndex, AttachTo()))
		{
			fAttachForward = true;
		}
	}
	return fAttachForward;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlAttrValueSpec::GenerateAttrSettingCode(GrcManager * pcman, int fxdRuleVersion,
	Vector<byte> & vbOutput,
	int irit, int nIIndex, int iritAttachTo)
{
	bool fAttachForward = false;

	int nBogus;

	Assert(m_psymName->FitsSymbolType(ksymtSlotAttr));
	Assert(m_pexpValue);
	ExpressionType expt = m_psymName->ExpType();
	Assert(expt == kexptSlotRef || expt == kexptNumber ||
		expt == kexptMeas || expt == kexptBoolean);
	StrAnsi staOp = m_psymOperator->FullName();
	int slat = m_psymName->SlotAttrEngineCodeOp();

	if (m_psymName->IsIndexedSlotAttr())	// eg, component.XXX.ref, user1
	{
		m_pexpValue->GenerateEngineCode(fxdRuleVersion, vbOutput,
			irit, NULL, nIIndex, false, -1, &nBogus);

		if (m_psymName->IsComponentRef() || pcman->VersionForTable(ktiSilf) < 0x00020000)
		{
			Assert(staOp == "=");
			vbOutput.Push(kopIAttrSetSlot);
		}
		else if (m_psymName->IsUserDefinableSlotAttr())
		{
			if (staOp == "=")
				vbOutput.Push(kopIAttrSet);
			else if (staOp == "+=")
				vbOutput.Push(kopIAttrAdd);
			else if (staOp == "-=")
				vbOutput.Push(kopIAttrSub);
			else
				Assert(false);
		}
		vbOutput.Push(slat);
		vbOutput.Push(pcman->SlotAttributeIndex(m_psymName));
	}
	else if (expt == kexptSlotRef)
	{
		Assert(staOp == "=");
		int nValue;
		if (slat == kslatAttTo && iritAttachTo == -1)
		{
			// attach.to = @0 means no attachment
		}
		else
		{
			m_pexpValue->GenerateEngineCode(fxdRuleVersion, vbOutput, irit, NULL, nIIndex,
				false, iritAttachTo, &nValue);

			vbOutput.Push(kopAttrSetSlot);
			vbOutput.Push(slat);

			if (slat == kslatAttTo)
			{
				if (nValue < 0)
					GdlRuleItem::GenerateInsertEqualsFalse(vbOutput);	// for this slot
				else if (nValue > 0)
					fAttachForward = true;	// generate insert = false for next slot
			}
		}
	}
	else
	{
		bool fAttachAt =
			(slat == kslatAttAtX || slat == kslatAttAtY || slat == kslatAttAtGpt ||
				slat == kslatAttAtXoff || slat == kslatAttAtYoff);

		int op;
		if (staOp == "=")
			op = kopAttrSet;
		else if (staOp == "+=")
			op = kopAttrAdd;
		else if (staOp == "-=")
			op = kopAttrSub;
		else
			Assert(false);

		m_pexpValue->GenerateEngineCode(fxdRuleVersion, vbOutput, irit, NULL, nIIndex,
			fAttachAt, iritAttachTo, &nBogus);

		vbOutput.Push(op);
		vbOutput.Push(slat);

	}

	return fAttachForward;
}

/*----------------------------------------------------------------------------------------------
	Generate the extra "insert = false" for attachments.
----------------------------------------------------------------------------------------------*/
void GdlRuleItem::GenerateInsertEqualsFalse(Vector<byte> & vbOutput)
{
	vbOutput.Push(kopPushByte);
	vbOutput.Push(0);	// false;
	vbOutput.Push(kopAttrSetSlot);
	vbOutput.Push(kslatInsert);
}


/*----------------------------------------------------------------------------------------------
	Return the component ID for a given ligature component symbol, or the number cooresponding
	to the index of the user-definable slot attribute.
----------------------------------------------------------------------------------------------*/
int GrcManager::SlotAttributeIndex(Symbol psym)
{
	if (psym->IsComponentRef())
	{
		Symbol psymBase = psym->BaseLigComponent();
		if (!psymBase->IsGeneric())
			psymBase = psymBase->Generic();
		Assert(psymBase->IsGeneric());

		int nIDRet = psymBase->InternalID();
		Assert(nIDRet > -1);
		return nIDRet;
	}
	else if (psym->IsUserDefinableSlotAttr())
	{
		return psym->UserDefinableSlotAttrIndex();
	}
	else
		//	No other kinds of indexed attributes so far.
		Assert(false);
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Analyze the passes to calculate the maximum number of characters before and after
	the official range of a segment that could cause the segment to become invalid.
	Also set a simple flag if any line-break items occur in any rule.
----------------------------------------------------------------------------------------------*/
void GrcManager::CalculateContextOffsets()
{
	m_prndr->CalculateContextOffsets();
}

/*--------------------------------------------------------------------------------------------*/
void GdlRenderer::CalculateContextOffsets()
{
	m_fLineBreak = false;
	m_critPreXlbContext = 0;
	m_critPostXlbContext = 0;

	GdlRuleTable * prultblSub = FindRuleTable("substitution");
	GdlRuleTable * prultblJust= FindRuleTable("justification");
	GdlRuleTable * prultblPos = FindRuleTable("positioning");

	//	Don't need to do this for the linebreak table, since conceptually it occurs
	//	before the linebreaks have been made.

	if (prultblSub)
		prultblSub->CalculateContextOffsets(&m_critPreXlbContext, &m_critPostXlbContext,
			&m_fLineBreak, false, NULL, NULL);

	if (prultblJust)
		prultblJust->CalculateContextOffsets(&m_critPreXlbContext, &m_critPostXlbContext,
			&m_fLineBreak, false, prultblSub, NULL);

	if (prultblPos)
		prultblPos->CalculateContextOffsets(&m_critPreXlbContext, &m_critPostXlbContext,
			&m_fLineBreak, true, prultblSub, prultblJust);
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::CalculateContextOffsets(int * pcPreXlbContext, int * pcPostXlbContext,
	bool * pfLineBreak, bool fPos, GdlRuleTable * prultbl1, GdlRuleTable * prultbl2)
{
	if (*pcPreXlbContext == kInfiniteXlbContext && *pcPostXlbContext == kInfiniteXlbContext)
	{
		Assert(*pfLineBreak);
		*pfLineBreak = true;
		return;
	}

	for (int ipass = 0; ipass < m_vppass.Size(); ipass++)
	{
		GdlPass * ppass = m_vppass[ipass];

		if (ppass->HasLineBreaks())
			*pfLineBreak = true;

		//	If no cross-line-boundary rules in this pass, ignore it.
		if (!ppass->HasCrossLineContext())
			continue;

		if (ppass->HasReprocessing())
		{
			//	This pass has reprocessing occurring: return values indicating that we
			//	can't determine a context limit.
			*pcPreXlbContext = kInfiniteXlbContext;
			*pcPostXlbContext = kInfiniteXlbContext;
			return;
		}

		int cPreTmp = ppass->MaxPreLBSlots();
		int cPostTmp = ppass->MaxPostLBSlots();

		//	Loop backwards through all the passes in this table, calculating the ranges.
		for (int ipassPrev = ipass; ipassPrev-- > 0; )
		{
			GdlPass * ppassPrev = m_vppass[ipassPrev];
			if (fPos)
			{
				cPreTmp = std::max(cPreTmp, ppassPrev->MaxPreLBSlots());
				cPostTmp = std::max(cPostTmp, ppassPrev->MaxPostLBSlots());
			}
			else
			{
				if (ppassPrev->HasReprocessing())
				{
					//	Previous pass has reprocessing occurring: return values indicating that
					//	we can't determine a context limit.
					*pcPreXlbContext = kInfiniteXlbContext;
					*pcPostXlbContext = kInfiniteXlbContext;
					return;
				}
				//	For the substitution table, multiply the range by the max number of context
				//	items in the pass.
				cPreTmp = cPreTmp * ppassPrev->MaxRuleContext();
				cPostTmp = cPostTmp * ppassPrev->MaxRuleContext();
			}
		}

		//	Loop backwards through the previous table(s) also.
		for (int itbl = 2; itbl > 0; itbl--)
		{
			GdlRuleTable * prultblPrev = ((itbl == 2) ? prultbl2 : prultbl1);
			if (prultblPrev)
			{
				for (int ipassPrev = prultblPrev->NumberOfPasses(); ipassPrev-- > 0; )
				{
					GdlPass * ppassPrev = prultblPrev->m_vppass[ipassPrev];

					if (ppassPrev->HasReprocessing())
					{
						//	Previous pass has reprocessing occurring:
						//	return values indicating that
						//	we can't determine a context limit.
						*pcPreXlbContext = kInfiniteXlbContext;
						*pcPostXlbContext = kInfiniteXlbContext;
						return;
					}
					cPreTmp = cPreTmp * ppassPrev->MaxRuleContext();
					cPostTmp = cPostTmp * ppassPrev->MaxRuleContext();
				}
			}
		}

		*pcPreXlbContext = max(*pcPreXlbContext, cPreTmp);
		*pcPostXlbContext = max(*pcPostXlbContext, cPostTmp);
	}
}


/***********************************************************************************************
	Debuggers
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Output a list of rules ordered by precedence.
----------------------------------------------------------------------------------------------*/
void GrcManager::DebugRulePrecedence()
{
	std::ofstream strmOut;
	strmOut.open("dbg_ruleprec.txt");
	if (strmOut.fail())
	{
		g_errorList.AddError(6101, NULL,
			"Error in writing to file ",
			"dbg_ruleprec.txt");
		return;
	}

	if (g_errorList.AnyFatalErrors())
		strmOut << "Fatal errors--compilation aborted";
	else
	{
		strmOut << "RULE PRECEDENCE\n\n";
		m_prndr->DebugRulePrecedence(this, strmOut);
	}

	strmOut.close();
}
/*--------------------------------------------------------------------------------------------*/
void GdlRenderer::DebugRulePrecedence(GrcManager * pcman, std::ostream & strmOut)
{
	GdlRuleTable * prultbl;

	if ((prultbl = FindRuleTable("linebreak")) != NULL)
		prultbl->DebugRulePrecedence(pcman, strmOut);

	if ((prultbl = FindRuleTable("substitution")) != NULL)
		prultbl->DebugRulePrecedence(pcman, strmOut);

	if (m_iPassBidi > -1)
		strmOut << "\nPASS " << m_iPassBidi + 1 << ": bidi\n";

	if ((prultbl = FindRuleTable("justification")) != NULL)
		prultbl->DebugRulePrecedence(pcman, strmOut);

	if ((prultbl = FindRuleTable("positioning")) != NULL)
		prultbl->DebugRulePrecedence(pcman, strmOut);
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::DebugRulePrecedence(GrcManager * pcman, std::ostream & strmOut)
{
	strmOut << "\nTABLE: " << m_psymName->FullName().Chars() << "\n";
	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->DebugRulePrecedence(pcman, strmOut);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::DebugRulePrecedence(GrcManager * pcman, std::ostream & strmOut)
{
	if (m_vprule.Size() == 0)
		return;

	Assert(PassDebuggerNumber() != 0);

	strmOut << "\nPASS: " << PassDebuggerNumber() << "\n";

	// Sort rules by their precedence: primarily by the number of items matched (largest first),
	// and secondarily by their location in the file (rule number--smallest first).
	Vector<int> viruleSorted;
	Vector<int> vnKeys;
	for (int irule1 = 0; irule1 < m_vprule.Size(); irule1++)
	{
		int nSortKey1 = m_vprule[irule1]->SortKey();
		int iirule2;
		for (iirule2 = 0; iirule2 < viruleSorted.Size(); iirule2++)
		{
			int nSortKey2 = vnKeys[iirule2];
			if (nSortKey1 > nSortKey2 ||
				(nSortKey1 == nSortKey2 && irule1 < viruleSorted[iirule2]))
			{
				// Insert it.
				viruleSorted.Insert(iirule2, irule1);
				vnKeys.Insert(iirule2, nSortKey1);
				break;
			}
		}
		if (iirule2 >= viruleSorted.Size())
		{
			viruleSorted.Push(irule1);
			vnKeys.Push(nSortKey1);
		}

		Assert(viruleSorted.Size() == irule1 + 1);
	}

	int nPassNum = PassDebuggerNumber();
	for (int iirule = 0; iirule < m_vprule.Size(); iirule++)
	{
		strmOut << "\n" << iirule << " - RULE " << nPassNum << "." << viruleSorted[iirule] << ", ";
		m_vprule[viruleSorted[iirule]]->LineAndFile().WriteToStream(strmOut, true);
		strmOut << ":  ";

		m_vprule[viruleSorted[iirule]]->RulePrettyPrint(pcman, strmOut);
		strmOut << "\n\n";
	}
}

/*----------------------------------------------------------------------------------------------
	Output a text version of the engine code to the stream.
----------------------------------------------------------------------------------------------*/
void GrcManager::DebugEngineCode()
{
	std::ofstream strmOut;
	strmOut.open("dbg_enginecode.txt");
	if (strmOut.fail())
	{
		g_errorList.AddError(6102, NULL,
			"Error in writing to file ",
			"dbg_enginecode.txt");
		return;
	}

	if (g_errorList.AnyFatalErrors())
		strmOut << "Fatal errors--compilation aborted";
	else
	{
		strmOut << "ENGINE CODE FOR RULES\n\n";
		m_prndr->DebugEngineCode(this, strmOut);
	}

	strmOut.close();
}

/*--------------------------------------------------------------------------------------------*/
void GdlRenderer::DebugEngineCode(GrcManager * pcman, std::ostream & strmOut)
{
	GdlRuleTable * prultbl;

	int fxdRuleVersion = pcman->VersionForRules();

	if ((prultbl = FindRuleTable("linebreak")) != NULL)
		prultbl->DebugEngineCode(pcman, fxdRuleVersion, strmOut);

	if ((prultbl = FindRuleTable("substitution")) != NULL)
		prultbl->DebugEngineCode(pcman, fxdRuleVersion, strmOut);

	if (m_iPassBidi > -1)
		strmOut << "\nPASS " << m_iPassBidi + 1 << ": bidi\n";

	if ((prultbl = FindRuleTable("justification")) != NULL)
		prultbl->DebugEngineCode(pcman, fxdRuleVersion, strmOut);

	if ((prultbl = FindRuleTable("positioning")) != NULL)
		prultbl->DebugEngineCode(pcman, fxdRuleVersion, strmOut);
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::DebugEngineCode(GrcManager * pcman, int fxdRuleVersion, std::ostream & strmOut)
{
	strmOut << "\nTABLE: " << m_psymName->FullName().Chars() << "\n";
	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->DebugEngineCode(pcman, fxdRuleVersion, strmOut);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::DebugEngineCode(GrcManager * pcman, int fxdRuleVersion, std::ostream & strmOut)
{
	int nPassNum = PassDebuggerNumber();
	strmOut << "\nPASS: " << nPassNum << "\n";

	Vector<byte> vbPassConstraints;
	GenerateEngineCode(pcman, fxdRuleVersion, vbPassConstraints);
	if (vbPassConstraints.Size() == 0)
	{
		strmOut << "\nPASS CONSTRAINTS: none\n";
	}
	else
	{
		strmOut << "\nPASS CONSTRAINTS:\n";
		GdlRule::DebugEngineCode(vbPassConstraints, fxdRuleVersion, strmOut);
	}

	for (int iprul = 0; iprul < m_vprule.Size(); iprul++)
	{
		strmOut << "\nRULE " << nPassNum << "." << iprul << ", ";
		m_vprule[iprul]->LineAndFile().WriteToStream(strmOut, true);
		strmOut << ":  ";

		m_vprule[iprul]->RulePrettyPrint(pcman, strmOut);
		strmOut << "\n";
		m_vprule[iprul]->DebugEngineCode(pcman, fxdRuleVersion, strmOut);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRule::DebugEngineCode(GrcManager * pcman, int fxdRuleVersion, std::ostream & strmOut)
{
	Vector<byte> vbActions;
	Vector<byte> vbConstraints;

	GenerateEngineCode(pcman, fxdRuleVersion, vbActions, vbConstraints);

	strmOut << "\nACTIONS:\n";
	DebugEngineCode(vbActions, fxdRuleVersion, strmOut);

	if (vbConstraints.Size() == 0)
	{
		strmOut << "\nCONSTRAINTS: none\n";
	}
	else
	{
		strmOut << "\nCONSTRAINTS:\n";
		DebugEngineCode(vbConstraints, fxdRuleVersion, strmOut);
	}
}

void GdlRule::DebugEngineCode(Vector<byte> & vb, int fxdRuleVersion, std::ostream & strmOut)
{
	int ib = 0;
	while (ib < vb.Size())
	{
		int op = vb[ib++];
		strmOut << EngineCodeDebugString(op).Chars();

		int cbArgs = 0;
		int slat;
		unsigned int nUnsigned;
		int nSigned;
		signed short int nSignedShort;
		int gmet;
		int pstat;
		switch (op)
		{
		case kopNop:				cbArgs = 0;		break;
		case kopPushByte:			cbArgs = 1;		break;
		case kopPushByteU:
			nUnsigned = (unsigned int)vb[ib++];
			strmOut << " " << nUnsigned;
			break;
		case kopPushShort:
			nSignedShort = (signed short int)vb[ib++];
			nSignedShort = (nSignedShort << 8) + vb[ib++];
			nSigned = (signed int)nSignedShort;
			strmOut << " " << nSigned;
			break;
		case kopPushShortU:
			nUnsigned = (unsigned int)vb[ib++];
			nUnsigned = (nUnsigned << 8) + vb[ib++];
			strmOut << " " << nUnsigned;
			break;
		case kopPushLong:
			nUnsigned = (int)vb[ib++];
			nUnsigned = (nUnsigned << 8) + vb[ib++];
			nUnsigned = (nUnsigned << 8) + vb[ib++];
			nUnsigned = (nUnsigned << 8) + vb[ib++];
			nSigned = (signed int)nUnsigned;
			strmOut << " " << nSigned;
			break;
		case kopAdd:				cbArgs = 0;		break;
		case kopSub:				cbArgs = 0;		break;
		case kopMul:				cbArgs = 0;		break;
		case kopDiv:				cbArgs = 0;		break;
		case kopMin:				cbArgs = 0;		break;
		case kopMax:				cbArgs = 0;		break;
		case kopNeg:				cbArgs = 0;		break;
		case kopTrunc8:				cbArgs = 0;		break;
		case kopTrunc16:			cbArgs = 0;		break;
		case kopCond:				cbArgs = 0;		break;
		case kopAnd:				cbArgs = 0;		break;
		case kopOr:					cbArgs = 0;		break;
		case kopNot:				cbArgs = 0;		break;
		case kopEqual:				cbArgs = 0;		break;
		case kopNotEq:				cbArgs = 0;		break;
		case kopLess:				cbArgs = 0;		break;
		case kopGtr:				cbArgs = 0;		break;
		case kopLessEq:				cbArgs = 0;		break;
		case kopGtrEq:				cbArgs = 0;		break;
		case kopNext:				cbArgs = 0;		break;
		case kopNextN:				cbArgs = 1;		break;	// N
		case kopCopyNext:			cbArgs = 0;		break;
		case kopPutGlyphV1_2:
			nUnsigned = (unsigned int)vb[ib++];	// output class
			strmOut << " " << nUnsigned;
			cbArgs = 0;
			break;
		case kopPutSubsV1_2:
			nSigned = (signed int)vb[ib++];		// selector
			strmOut << " " << nSigned;
			nUnsigned = (unsigned int)vb[ib++];	// input class
			strmOut << " " << nUnsigned;
			nUnsigned = (unsigned int)vb[ib++];	// output class
			strmOut << " " << nUnsigned;
			cbArgs = 0;
			break;
		case kopPutCopy:			cbArgs = 1;		break;	// selector
		case kopInsert:				cbArgs = 0;		break;
		case kopDelete:				cbArgs = 0;		break;

		case kopPutGlyph:
			nSignedShort = (signed short int)vb[ib++];
			nSignedShort = (nSignedShort << 8) + vb[ib++];
			nSigned = (signed int)nSignedShort;
			strmOut << " " << nSigned;	// output class
			break;

		case kopPutSubs3:
			nSigned = (int)vb[ib++];
			strmOut << " " << nSigned;	// slot offset
			nSignedShort = (signed short int)vb[ib++];
			nSignedShort = (nSignedShort << 8) + vb[ib++];
			nSigned = (signed int)nSignedShort;
			strmOut << " " << nSigned;	// input class
			// fall through
		case kopPutSubs2:
			nSigned = (int)vb[ib++];
			strmOut << " " << nSigned;	// slot offset
			nSignedShort = (signed short int)vb[ib++];
			nSignedShort = (nSignedShort << 8) + vb[ib++];
			nSigned = (signed int)nSignedShort;
			strmOut << " " << nSigned;	// input class
			// fall through
		case kopPutSubs:
			nSigned = (int)vb[ib++];
			strmOut << " " << nSigned;	// slot offset
			nSignedShort = (signed short int)vb[ib++];
			nSignedShort = (nSignedShort << 8) + vb[ib++];
			nSigned = (signed int)nSignedShort;
			strmOut << " " << nSigned;	// input class
			nSignedShort = (signed short int)vb[ib++];
			nSignedShort = (nSignedShort << 8) + vb[ib++];
			nSigned = (signed int)nSignedShort;
			strmOut << " " << nSigned;	// output class
			break;

		case kopAssoc:
			cbArgs = vb[ib++];
			strmOut << " " << cbArgs;
			break;
		case kopCntxtItem:			cbArgs = 2;		break;

		case kopAttrSet:
		case kopAttrAdd:
		case kopAttrSub:
		case kopAttrSetSlot:
			slat = vb[ib++];
			strmOut << " " << SlotAttributeDebugString(slat).Chars();
			cbArgs = 0;
			break;
		case kopIAttrSet:
		case kopIAttrAdd:
		case kopIAttrSub:
		case kopIAttrSetSlot:
			slat = vb[ib++];
			strmOut << " " << SlotAttributeDebugString(slat).Chars();
			cbArgs = 1;
			break;
		case kopPushSlotAttr:
			slat = vb[ib++];
			strmOut << " " << SlotAttributeDebugString(slat).Chars();
			cbArgs = 1;	// selector
			break;
		case kopPushISlotAttr:
			slat = vb[ib++];
			strmOut << " " << SlotAttributeDebugString(slat).Chars();
			cbArgs = 2;	// selector, index
			break;
		case kopPushGlyphAttr:
		case kopPushAttToGlyphAttr:
			nSignedShort = (signed short int)vb[ib++];
			nSignedShort = (nSignedShort << 8) + vb[ib++];
			nSigned = (signed int)nSignedShort;
			strmOut << " " << nSigned;	// glyph attribute
			cbArgs = 1;					// selector
			break;
		case kopPushGlyphAttrV1_2:
		case kopPushAttToGAttrV1_2:
			nUnsigned = (unsigned int)vb[ib++]; // glyph attribute
			strmOut << " " << nUnsigned;
			cbArgs = 1;							// selector
			break;
		case kopPushGlyphMetric:
		case kopPushAttToGlyphMetric:
			gmet = vb[ib++];
			strmOut << " " << GlyphMetricDebugString(gmet).Chars();
			cbArgs = 2;	// selector, cluster
			break;
		case kopPushFeat:			cbArgs = 2;		break;	// feature, selector
		//case kopPushIGlyphAttr:	cbArgs = 2;		break;	// glyph attr, index
		case kopPushProcState:
			pstat = vb[ib++];
			strmOut << " " << ProcessStateDebugString(pstat).Chars();
			cbArgs = 0;
			break;
		case kopPushVersion:		cbArgs = 0;		break;
		case kopPopRet:				cbArgs = 0;		break;
		case kopRetZero:			cbArgs = 0;		break;
		case kopRetTrue:			cbArgs = 0;		break;
		default:
			Assert(false);
			cbArgs = 0;
		}

		// This loop handles only 8-bit signed values.
		for (int iTmp = 0; iTmp < cbArgs; iTmp++)
		{
			int n = (char)vb[ib++];
			strmOut << " " << n;
		}
		strmOut << "\n";
	}
}


/*----------------------------------------------------------------------------------------------
	Return the text equivalent of the given slot attribute.
----------------------------------------------------------------------------------------------*/
StrAnsi GdlRule::SlotAttributeDebugString(int slat)
{
	StrAnsi sta("bad-slot-attr-");
	switch (slat)
	{
	case kslatAdvX:				return "advance_x";
	case kslatAdvY:				return "advance_y";
	case kslatAttTo:			return "attach_to";
	case kslatAttAtX:			return "attach_at_x";
	case kslatAttAtY:			return "attach_at_y";
	case kslatAttAtGpt:			return "attach_at_gpoint";
	case kslatAttAtXoff:		return "attach_at_xoffset";
	case kslatAttAtYoff:		return "attach_at_yoffset";
	case kslatAttWithX:			return "attach_with_x";
	case kslatAttWithY:			return "attach_with_y";
	case kslatAttWithGpt:		return "attach_with_gpoint";
	case kslatAttWithXoff:		return "attach_with_xoffset";
	case kslatAttWithYoff:		return "attach_with_yoffset";
	case kslatAttLevel:			return "attach_level";
	case kslatBreak:			return "break";
	case kslatCompRef:			return "comp_ref";
	case kslatDir:				return "dir";
	case kslatInsert:			return "insert";
	case kslatPosX:				return "pos_x";
	case kslatPosY:				return "pos_y";
	case kslatShiftX:			return "shift_x";
	case kslatShiftY:			return "shift_y";
	case kslatUserDefnV1:		return "user";
	case kslatUserDefn:			return "user";
	case kslatMeasureSol:		return "measure_startofline";
	case kslatMeasureEol:		return "measure_endofline";
	case kslatJStretch:			return "justify_stretch";
	case kslatJShrink:			return "justify_shrink";
	case kslatJStep:			return "justify_step";
	case kslatJWeight:			return "justify_weight";
	case kslatJWidth:			return "justify_width";
	default:
		Assert(false);
		char rgch[20];
		itoa(slat, rgch, 10);
		sta += rgch;
		return sta;
	}
}


/*----------------------------------------------------------------------------------------------
	Return the text equivalent of the given glyph metric.
----------------------------------------------------------------------------------------------*/
StrAnsi GdlRule::GlyphMetricDebugString(int gmet)
{
	StrAnsi sta("bad-glyph-metric-");
	switch (gmet)
	{
	case kgmetLsb:				return "lsb";
	case kgmetRsb:				return "rsb";
	case kgmetBbTop:			return "bb_top";
	case kgmetBbBottom:			return "bb_bottom";
	case kgmetBbLeft:			return "bb_left";
	case kgmetBbRight:			return "bb_right";
	case kgmetBbHeight:			return "bb_height";
	case kgmetBbWidth:			return "bb_width";
	case kgmetAdvWidth:			return "aw";
	case kgmetAdvHeight:		return "ah";
	case kgmetAscent:			return "ascent";
	case kgmetDescent:			return "descent";
	default:
		Assert(false);
		char rgch[20];
		itoa(gmet, rgch, 10);
		sta += rgch;
		return sta;
	}
}


/*----------------------------------------------------------------------------------------------
	Return the text equivalent of the given engine code operator.
----------------------------------------------------------------------------------------------*/
StrAnsi GdlRule::EngineCodeDebugString(int op)
{
	StrAnsi sta("bad-engine-op-");
	switch (op)
	{
	case kopNop:					return "Nop";
	case kopPushByte:				return "PushByte";
	case kopPushByteU:				return "PushByteU";
	case kopPushShort:				return "PushShort";
	case kopPushShortU:				return "PushShortU";
	case kopPushLong:				return "PushLong";
	case kopAdd:					return "Add";
	case kopSub:					return "Sub";
	case kopMul:					return "Mul";
	case kopDiv:					return "Div";
	case kopMin:					return "Min";
	case kopMax:					return "Max";
	case kopNeg:					return "Neg";
	case kopTrunc8:					return "Trunc8";
	case kopTrunc16:				return "Trunc16";
	case kopCond:					return "Cond";
	case kopAnd:					return "And";
	case kopOr:						return "Or";
	case kopNot:					return "Not";
	case kopEqual:					return "Equal";
	case kopNotEq:					return "NotEq";
	case kopLess:					return "Less";
	case kopGtr:					return "Gtr";
	case kopLessEq:					return "LessEq";
	case kopGtrEq:					return "GtrEq";
	case kopNext:					return "Next";
	case kopNextN:					return "NextN";
	case kopCopyNext:				return "CopyNext";
	case kopPutGlyph:				return "PutGlyph";
	case kopPutGlyphV1_2:			return "PutGlyph(V1&2)";
	case kopPutSubsV1_2:			return "PutSubs(V1&2)";
	case kopPutSubs:				return "PutSubs";
	case kopPutSubs2:				return "PutSubs2";
	case kopPutSubs3:				return "PutSubs3";
	case kopPutCopy:				return "PutCopy";
	case kopInsert:					return "Insert";
	case kopDelete:					return "Delete";
	case kopAssoc:					return "Assoc";
	case kopCntxtItem:				return "CntxtItem";
	case kopAttrSet:				return "AttrSet";
	case kopAttrAdd:				return "AttrAdd";
	case kopAttrSub:				return "AttrSub";
	case kopAttrSetSlot:			return "AttrSetSlot";
	case kopIAttrSetSlot:			return "IAttrSetSlot";
	case kopPushSlotAttr:			return "PushSlotAttr";
	case kopPushISlotAttr:			return "PushISlotAttr";
	case kopPushGlyphAttr:			return "PushGlyphAttr";
	case kopPushGlyphAttrV1_2:		return "PushGlyphAttr(V1&2)";
	case kopPushGlyphMetric:		return "PushGlyphMetric";
	case kopPushFeat:				return "PushFeat";
	case kopPushAttToGlyphAttr:		return "PushAttToGlyphAttr";
	case kopPushAttToGAttrV1_2:		return "PushAttToGlyphAttr(V1&2)";
	case kopPushAttToGlyphMetric:	return "PushAttToGlyphMetric";
	case kopPushIGlyphAttr:			return "PushIGlyphAttr";
	case kopPushVersion:			return "PushVersion";
	case kopPopRet:					return "PopRet";
	case kopRetZero:				return "RetZero";
	case kopRetTrue:				return "RetTrue";
	case kopIAttrSet:				return "IAttrSet";
	case kopIAttrAdd:				return "IAttrAdd";
	case kopIAttrSub:				return "IAttrSub";
	case kopPushProcState:			return "PushProcState";
	default:
		Assert(false);
		char rgch[20];
		itoa(op, rgch, 10);
		sta += rgch;
		return sta;
	}
}

/*----------------------------------------------------------------------------------------------
	Return the text equivalent of the given process-state.
----------------------------------------------------------------------------------------------*/
StrAnsi GdlRule::ProcessStateDebugString(int pstat)
{
	StrAnsi sta("bad-process-state-");
	switch (pstat)
	{
	case kpstatJustifyMode:		return "JustifyMode";
	case kpstatJustifyLevel:	return "JustifyLevel";
	default:
		Assert(false);
		char rgch[20];
		itoa(pstat, rgch, 10);
		sta += rgch;
		return sta;
	}
}

/*----------------------------------------------------------------------------------------------
	Output a list of glyph attributes.
----------------------------------------------------------------------------------------------*/
void GrcManager::DebugGlyphAttributes()
{
	std::ofstream strmOut;
	strmOut.open("dbg_glyphattrs.txt");
	if (strmOut.fail())
	{
		g_errorList.AddError(6103, NULL,
			"Error in Griting to file ",
			"dbg_glyphattrs.txt");
		return;
	}

	Symbol psymBw = m_psymtbl->FindSymbol("breakweight");
	int nAttrIdBw = psymBw->InternalID();
	//Symbol psymJStr = m_psymtbl->FindSymbol(GrcStructName("justify", "0", "stretch"));
	Symbol psymJStr = m_psymtbl->FindSymbol(GrcStructName("justify", "stretch"));
	int nAttrIdJStr = psymJStr->InternalID();

	if (g_errorList.AnyFatalErrors())
		strmOut << "Fatal errors--compilation aborted";
	else
	{
		strmOut << "GLYPH ATTRIBUTE IDS\n\n";
		for (int nAttrID = 0; nAttrID < m_vpsymGlyphAttrs.Size(); nAttrID++)
		{
			strmOut << nAttrID << ": "
				<< m_vpsymGlyphAttrs[nAttrID]->FullName().Chars() << "\n";
		}
		strmOut << "\n\n\nGLYPH ATTRIBUTE VALUES\n\n";

		for (int wGlyphID = 0; wGlyphID < m_cwGlyphIDs; wGlyphID++)
		{
			// Convert breakweight values depending on the table version to output.
			ConvertBwForVersion(wGlyphID, nAttrIdBw);

			//	Split any large stretch values into two 16-bit words.
			SplitLargeStretchValue(wGlyphID, nAttrIdJStr);

			bool fAnyNonZero = false;

			for (int nAttrID = 0; nAttrID < m_vpsymGlyphAttrs.Size(); nAttrID++)
			{
				int nValue = FinalAttrValue(wGlyphID, nAttrID);

				//	Skip undefined and zero-valued attributes.
				if (nValue == 0)
					continue;

				if (fAnyNonZero == false)
				{
					DebugHex(strmOut, wGlyphID);
//					if (wGlyphID < 0x0100)
//						strmOut << " '" << (char)wGlyphID << "'";
					strmOut << "  (" << wGlyphID << ")" << "\n";
				}

				fAnyNonZero = true;

				strmOut << "   " << m_vpsymGlyphAttrs[nAttrID]->FullName().Chars()
					<< " = ";
				if (m_vpsymGlyphAttrs[nAttrID]->LastFieldIs("gpoint") &&
					nValue == kGpointZero)
				{
					strmOut << "zero" << "\n";
				}
				else
				{
					strmOut  << nValue;
					if (nValue > 9 || nValue < 0)
						strmOut << " (0x" << GdlGlyphDefn::GlyphIDString(nValue).Chars() << ")";
					strmOut << "\n";
				}

			}

			if (fAnyNonZero)
				strmOut << "\n\n";
		}
	}

	strmOut.close();
}


/*----------------------------------------------------------------------------------------------
	Generate a list of glyph attributes (whose indices in the vector match their internal IDs).
----------------------------------------------------------------------------------------------*/
void GrcSymbolTable::GlyphAttrList(Vector<Symbol> & vpsym)
{
	for (SymbolTableMap::iterator it = m_hmstasymEntries.Begin();
		it != m_hmstasymEntries.End();
		++it)
	{
		Symbol psym = it.GetValue();

		if (psym->m_psymtblSubTable)
			psym->m_psymtblSubTable->GlyphAttrList(vpsym);

		else if (psym->IsGeneric() && psym->FitsSymbolType(ksymtGlyphAttr))
		{
			if (psym->InternalID() >= 0)
			{
				while (vpsym.Size() <= psym->InternalID())
					vpsym.Push(NULL);

				vpsym[psym->InternalID()] = psym;
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Generate a pretty-print description of the rule (similar to the original syntax).
----------------------------------------------------------------------------------------------*/
void GdlRule::RulePrettyPrint(GrcManager * pcman, std::ostream & strmOut)
{
	//	Loop through all the items to see if we need a LHS or a context.
	bool fLhs = false;
	bool fContext = (m_nScanAdvance != -1);
	int irit;
	for (irit = 0; irit < m_vprit.Size() ; irit++)
	{
		GdlRuleItem * prit = m_vprit[irit];
		GdlSubstitutionItem * pritsub = dynamic_cast<GdlSubstitutionItem *>(prit);
		if (pritsub)
			fLhs = true;

		GdlSetAttrItem * pritset = dynamic_cast<GdlSetAttrItem *>(prit);
		if (!pritset)
			fContext = true;
		else if (prit->m_pexpConstraint)
			fContext = true;
	}

	if (fLhs)
	{
		for (irit = 0; irit < m_vprit.Size() ; irit++)
		{
			m_vprit[irit]->LhsPrettyPrint(pcman, this, irit, strmOut);
		}
		strmOut << ">  ";
	}

	for (irit = 0; irit < m_vprit.Size() ; irit++)
	{
		m_vprit[irit]->RhsPrettyPrint(pcman, this, irit, strmOut);
	}

	if (fContext)
	{
		strmOut << " /  ";
		for (irit = 0; irit < m_vprit.Size() ; irit++)
		{
			if (m_nScanAdvance == irit)
				strmOut << "^  ";
			m_vprit[irit]->ContextPrettyPrint(pcman, this, irit, strmOut);
		}
	}

	strmOut << ";";
}



void GdlRuleItem::LhsPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
	std::ostream & strmOut)
{
	//	Do nothing.
}

void GdlRuleItem::RhsPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
	std::ostream & strmOut)
{
	//	Do nothing.
}

void GdlRuleItem::ContextPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
	std::ostream & strmOut)
{
	strmOut << m_psymInput->FullAbbrev().Chars();
	ConstraintPrettyPrint(pcman, prule, irit, strmOut);
	strmOut << "  ";
}

void GdlLineBreakItem::ContextPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
	std::ostream & strmOut)
{
	strmOut << "#";
	ConstraintPrettyPrint(pcman, prule, irit, strmOut);
	strmOut << "  ";
}

void GdlRuleItem::ConstraintPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
	std::ostream & strmOut)
{
	if (m_pexpConstraint)
	{
		strmOut << " {";
		m_pexpConstraint->PrettyPrint(pcman, strmOut);
		strmOut << "}";
	}
}

void GdlSetAttrItem::LhsPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
	std::ostream & strmOut)
{
	strmOut << m_psymInput->FullAbbrev().Chars();
	strmOut << "  ";
}

void GdlSetAttrItem::RhsPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
	std::ostream & strmOut)
{
	strmOut << m_psymInput->FullAbbrev().Chars();
	AttrSetterPrettyPrint(pcman, prule, irit, strmOut);
	strmOut << "  ";
}

void GdlSetAttrItem::ContextPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
	std::ostream & strmOut)
{
	strmOut << "_";
	ConstraintPrettyPrint(pcman, prule, irit, strmOut);
	strmOut << "  ";
}

void GdlSubstitutionItem::LhsPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
	std::ostream & strmOut)
{
	strmOut << m_psymInput->FullAbbrev().Chars();
	strmOut << "  ";
}

void GdlSubstitutionItem::RhsPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
	std::ostream & strmOut)
{
	strmOut << m_psymOutput->FullAbbrev().Chars();

	if (m_pexpSelector)
	{
		if (m_psymOutput->Data())
			strmOut << "$";
		strmOut << m_pexpSelector->SlotNumber();
	}

	if (m_vpexpAssocs.Size() > 0)
	{
		strmOut << ":";
		if (m_vpexpAssocs.Size() > 1)
			strmOut << "(";
		int iexp;
		for (iexp = 0; iexp < m_vpexpAssocs.Size() - 1; iexp++)
			strmOut << m_vpexpAssocs[iexp]->SlotNumber() << " ";
		strmOut << m_vpexpAssocs[iexp]->SlotNumber();
		if (m_vpexpAssocs.Size() > 1)
			strmOut << ")";
	}
	AttrSetterPrettyPrint(pcman, prule, irit, strmOut);
	strmOut << "  ";
}


void GdlSetAttrItem::AttrSetterPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
	std::ostream & strmOut)
{
	if (m_vpavs.Size() > 0)
	{
		bool fAtt = false;
		bool fAttAt = false;
		bool fAttWith = false;
		strmOut << " { ";
		for (int iavs = 0; iavs < m_vpavs.Size(); iavs++)
		{
			m_vpavs[iavs]->PrettyPrint(pcman, strmOut, &fAtt, &fAttAt, &fAttWith, m_vpavs.Size());
		}
		strmOut << " }";
	}
}

void GdlAttrValueSpec::PrettyPrint(GrcManager * pcman, std::ostream & strmOut,
	bool * pfAtt, bool * pfAttAt, bool * pfAttWith, int cpavs)
{
	if (cpavs > 6 && m_psymName->IsAttachment())
	{
		if (*pfAtt)
			return;
		*pfAtt = true;
		strmOut << "attach {...} ";
		return;
	}

	if (m_psymName->IsAttachAtField() && m_fFlattened)
	{
		if (*pfAttAt)
			return;
		*pfAttAt = true;
		strmOut << "attach.at=...";
		return;
	}
	else if (m_psymName->IsAttachWithField() && m_fFlattened)
	{
		if (*pfAttWith)
			return;
		*pfAttWith = true;
		strmOut << "attach.with=...";
		return;
	}
	else
		strmOut << m_psymName->FullAbbrev().Chars();

	strmOut << m_psymOperator->FullAbbrev().Chars();
	m_pexpValue->PrettyPrint(pcman, strmOut);
	strmOut << "; ";
}


/*----------------------------------------------------------------------------------------------
	Output a list of all the classes and their members.
----------------------------------------------------------------------------------------------*/
void GrcManager::DebugClasses()
{
	std::ofstream strmOut;
	strmOut.open("dbg_classes.txt");
	if (strmOut.fail())
	{
		g_errorList.AddError(6104, NULL,
			"Error in writing to file ",
			"dbg_classes.txt");
		return;
	}

	if (g_errorList.AnyFatalErrors())
		strmOut << "Fatal errors--compilation aborted";
	else
	{
		m_prndr->DebugClasses(strmOut, m_vpglfcReplcmtClasses, m_cpglfcLinear);
	}

	strmOut.close();
}

void GdlRenderer::DebugClasses(std::ostream & strmOut,
	Vector<GdlGlyphClassDefn *> & vpglfcReplcmt, int cpglfcLinear)
{
	strmOut << "LINEAR (OUTPUT) CLASSES";

	//	linear classes (output)
	int cTmp = 0;
	int ipglfc;
	for (ipglfc = 0; ipglfc < cpglfcLinear; ipglfc++)
	{
		GdlGlyphClassDefn * pglfc = vpglfcReplcmt[ipglfc];

		Assert(pglfc->ReplcmtOutputClass() || pglfc->GlyphIDCount() <= 1);
		//Assert(pglfc->ReplcmtOutputID() == cTmp);

		strmOut << "\n\n";
		strmOut << "Class #" << ipglfc << ": ";
		strmOut << pglfc->Name().Chars();

		Vector<utf16> vwGlyphs;
		pglfc->GenerateOutputGlyphList(vwGlyphs);

		//	glyph list
		for (int iw = 0; iw < vwGlyphs.Size(); iw++)
		{
			if (iw % 10 == 0)
			{
				strmOut << "\n" << iw << ":";
			}
			strmOut << "   ";
			GrcManager::DebugHex(strmOut, vwGlyphs[iw]);
		}

		cTmp++;
	}

	strmOut << "\n\n\nINDEXED (INPUT) CLASSES";

	//	indexed classes (input)
	for (ipglfc = cpglfcLinear; ipglfc < vpglfcReplcmt.Size(); ipglfc++)
	{
		GdlGlyphClassDefn * pglfc = vpglfcReplcmt[ipglfc];

		Assert(pglfc->ReplcmtInputClass());
		Assert(pglfc->ReplcmtInputID() == cTmp);

		strmOut << "\n\n";
		strmOut << "Class #" << ipglfc << ": ";
		strmOut << pglfc->Name().Chars();

		Vector<utf16> vwGlyphs;
		Vector<int> vnIndices;
		pglfc->GenerateInputGlyphList(vwGlyphs, vnIndices);
		//	glyph list
		for (int iw = 0; iw < vwGlyphs.Size(); iw++)
		{
			if (iw % 5 == 0)
			{
				strmOut << "\n";
			}
			GrcManager::DebugHex(strmOut, vwGlyphs[iw]);
			strmOut << " :";
			if (vnIndices[iw] < 1000) strmOut << " ";
			if (vnIndices[iw] < 100)  strmOut << " ";
			if (vnIndices[iw] < 10)   strmOut << " ";
			strmOut << vnIndices[iw];
			strmOut << "    ";
		}

		cTmp++;
	}
}

/*----------------------------------------------------------------------------------------------
	Output the contents of the -cmap-, the mapping from unicode-to-glyph ID and vice-versa.
	Also include any pseudo-glyphs.
----------------------------------------------------------------------------------------------*/
void GrcManager::DebugCmap(GrcFont * pfont)
{
	bool fSuppPlaneChars = pfont->AnySupplementaryPlaneChars();

	std::ofstream strmOut;
	strmOut.open("dbg_cmap.txt");
	if (strmOut.fail())
	{
		g_errorList.AddError(6105, NULL,
			"Error in writing to file ",
			"dbg_cmap.txt");
		return;
	}

	int nFirstPseudo = 0x10000;
	for (int iw = 0; iw < m_vwPseudoForUnicode.Size(); iw++)
		nFirstPseudo = min(nFirstPseudo, static_cast<int>(m_vwPseudoForUnicode[iw]));

	if (g_errorList.AnyFatalErrors())
	{
		strmOut << "Fatal errors--compilation aborted";
	}
	else
	{
		int cnUni = pfont->NumUnicode();
		utf16 * rgchwUniToGlyphID = new utf16[cnUni];
		memset(rgchwUniToGlyphID, 0, (cnUni * isizeof(utf16)));

		unsigned int * rgnGlyphIDToUni = new unsigned int[0x10000];
		memset(rgnGlyphIDToUni, 0, (0x10000 * isizeof(int)));

		pfont->GetGlyphsFromCmap(rgchwUniToGlyphID);
		//m_prndr->DebugCmap(pfont, rgchwUniToGlyphID, rgnGlyphIDToUni);

		Vector<unsigned int> vnXUniForPsd;
		Vector<utf16> vwXPsdForUni;

		// Generate the inverse cmap. Also overwrite the glyph IDs for any pseudos.
		int iUni;
		GrcFont::iterator fit(pfont);
		int iUniPsd = 0;
		for (iUni = 0, fit = pfont->Begin();
			fit != pfont->End();
			++fit, ++iUni)
		{
			unsigned int nUni = *fit;

			// Handle pseudos.
			while (iUniPsd < m_vnUnicodeForPseudo.Size() && nUni > m_vnUnicodeForPseudo[iUniPsd])
			{
				// Put any Unicode -> pseudo mappings where the Unicode is not in the cmap into
				// a separate list.
				vnXUniForPsd.Push(m_vnUnicodeForPseudo[iUniPsd]);
				vwXPsdForUni.Push(m_vwPseudoForUnicode[iUniPsd]);
				iUniPsd++;
			}
			if (iUniPsd < m_vnUnicodeForPseudo.Size() && m_vnUnicodeForPseudo[iUniPsd] == nUni)
			{
				// Pseudo: overwrite glyph ID.
				rgchwUniToGlyphID[iUni] = m_vwPseudoForUnicode[iUniPsd];
				iUniPsd++;
			}
			utf16 wGlyph = rgchwUniToGlyphID[iUni];
			rgnGlyphIDToUni[wGlyph] = nUni;
		}
		Assert(iUni == cnUni);

		unsigned int nUni;
		utf16 wGlyphID;

		strmOut << "UNICODE => GLYPH ID MAPPINGS\n\n";

		int iXPsd = 0; // extra pseudos
		for (iUni = 0, fit = pfont->Begin();
			fit != pfont->End();
			++fit, ++iUni)
		{
			nUni = *fit;
			wGlyphID = rgchwUniToGlyphID[iUni];
			Assert(wGlyphID != 0);

			while (iXPsd < vnXUniForPsd.Size() && vnXUniForPsd[iXPsd] < nUni)
			{
				// insert extra pseudos that are not in the cmap
				WriteCmapItem(strmOut, vnXUniForPsd[iXPsd], fSuppPlaneChars, vwXPsdForUni[iXPsd],
					true, true, false);
				iXPsd++;
			}
			WriteCmapItem(strmOut, nUni, fSuppPlaneChars, wGlyphID, true,
				wGlyphID >= nFirstPseudo, true);
		}

		// Sort the extra pseudos by glyph ID.
		for (int i1 = 0; i1 < vwXPsdForUni.Size() - 1; i1++)
			for (int i2 = i1 + 1; i2 < vwXPsdForUni.Size(); i2++)
				if (vwXPsdForUni[i1] > vwXPsdForUni[i2])
				{
					// Swap
					utf16 wTmp = vwXPsdForUni[i1];
					vwXPsdForUni[i1] = vwXPsdForUni[i2];
					vwXPsdForUni[i2] = wTmp;
					unsigned int nTmp = vnXUniForPsd[i1];
					vnXUniForPsd[i1] = vnXUniForPsd[i2];
					vnXUniForPsd[i2] = nTmp;
				}

		strmOut << "\n\n\nGLYPH ID => UNICODE MAPPINGS\n\n";

		iXPsd = 0;
		for (wGlyphID = 0; wGlyphID < 0xFFFF; wGlyphID++)
		{
			if (wGlyphID == m_wLineBreak)
			{
				DebugHex(strmOut, wGlyphID);
				if (fSuppPlaneChars) strmOut << "    ";
				strmOut << "                 [line-break]\n";
			}
			else if (wGlyphID == m_wPhantom)
			{
				DebugHex(strmOut, wGlyphID);
				if (fSuppPlaneChars) strmOut << "    ";
				strmOut << "                 [phantom]\n";
			}
			else if (iXPsd < vwXPsdForUni.Size() && vwXPsdForUni[iXPsd] == wGlyphID)
			{
				// Pseudo-glyph where the Unicode value is not in the cmap.
				if (vnXUniForPsd[iXPsd] != 0)
					WriteCmapItem(strmOut, vnXUniForPsd[iXPsd], fSuppPlaneChars,
						wGlyphID, false, true, false);
				iXPsd++;
			}
			else
			{
				nUni = rgnGlyphIDToUni[wGlyphID];
				if (nUni != 0)
					WriteCmapItem(strmOut, nUni, fSuppPlaneChars,
						wGlyphID, false, wGlyphID >= nFirstPseudo, true);
			}
		}

		delete[] rgchwUniToGlyphID;
		delete[] rgnGlyphIDToUni;
	}

	strmOut.close();
}

void GrcManager::WriteCmapItem(std::ofstream & strmOut,
	unsigned int nUnicode, bool fSuppPlaneChars, utf16 wGlyphID, bool fUnicodeToGlyph,
	bool fPseudo, bool fInCmap)
{
	if (fUnicodeToGlyph)
	{
		DebugUnicode(strmOut, nUnicode, fSuppPlaneChars);
//		if (wGlyphID < 0x0100)
//			strmOut << " '" << (char)wGlyphID << "'";
//		else
//			strmOut << "    ";

		strmOut << " => ";

		DebugHex(strmOut, wGlyphID);
		strmOut << "  (" << wGlyphID << ")";

		if (fPseudo)
		{
			if (fInCmap)
			{
				if (wGlyphID >= m_wFirstAutoPseudo)
					strmOut << "  [auto-pseudo]";
				else
					strmOut << "  [pseudo]";
			}
			else
				strmOut << "  [pseudo; not in cmap]";
		}

		strmOut << "\n";
	}
	else
	{
		DebugHex(strmOut, wGlyphID);

		strmOut << " => ";

		DebugUnicode(strmOut, nUnicode, fSuppPlaneChars);
		if (nUnicode < 0x0100)
			strmOut << "  '" << (char)nUnicode << "'";
		else
			strmOut << "     ";

		if (fPseudo)
		{
			if (fInCmap)
			{
				if (wGlyphID >= m_wFirstAutoPseudo)
					strmOut << "  [auto-pseudo]";
				else
					strmOut << "  [pseudo]";
			}
			else
				strmOut << "  [pseudo; not in cmap]";
		}

		strmOut << "\n";
	}
}


void GdlRenderer::DebugCmap(GrcFont * pfont, utf16 * rgchwUniToGlyphID, unsigned int * rgnGlyphIDToUni)
{
	for (int ipglfc = 0; ipglfc < m_vpglfc.Size(); ipglfc++)
		m_vpglfc[ipglfc]->DebugCmap(pfont, rgchwUniToGlyphID, rgnGlyphIDToUni);
}


void GdlGlyphClassDefn::DebugCmap(GrcFont * pfont,
	utf16 * rgchwUniToGlyphID, unsigned int * rgnGlyphIDToUni)
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
		m_vpglfdMembers[iglfd]->DebugCmapForMember(pfont, rgchwUniToGlyphID, rgnGlyphIDToUni);
}


void GdlGlyphClassDefn::DebugCmapForMember(GrcFont * pfont,
	utf16 * rgchwUniToGlyphID, unsigned int * rgnGlyphIDToUni)
{
	//	Do nothing; this class will be handled separately at the top level.
}

void GdlGlyphDefn::DebugCmapForMember(GrcFont * pfont,
	utf16 * rgchwUniToGlyphID, unsigned int * rgnGlyphIDToUni)
{
	Assert(m_vwGlyphIDs.Size() > 0);

	// TODO: check for kBadGlyph values

	//unsigned int n;
	//unsigned int nUnicode;
	//utf16 w;
	//utf16 wGlyphID;
	//utf16 wFirst; // wLast;

	switch (m_glft)
	{
	case kglftGlyphID:
		break;

	case kglftUnicode:
//		Assert(m_nFirst <= m_nLast);
//		for (n = m_nFirst; n <= m_nLast; ++n)
//		{
//			wGlyphID = pfont->GlyphFromCmap(n, this);
//			if (wGlyphID != 0)
//			{
//				rgchwUniToGlyphID[n] = wGlyphID;
//				rgnGlyphIDToUni[wGlyphID] = n;
//			}
//
//			// Just in case, since incrementing 0xFFFFFFFF will produce zero.
//			if (n == 0xFFFFFFFF)
//				break;
//		}

		break;

	case kglftPostscript:
		break;

	case kglftCodepoint:
//		char rgchCdPg[20];
//		itoa(m_wCodePage, rgchCdPg, 10);
//		wFirst = (utf16)m_nFirst;
//		wLast = (utf16)m_nLast;
//		if (wFirst == 0 && wLast == 0)
//		{
//			for (int ich = 0; ich < m_sta.Length(); ich++)
//			{
//				char rgchCdPt[2];
//				rgchCdPt[0] = m_sta.GetAt(ich);
//				nUnicode = pfont->UnicodeFromCodePage(m_wCodePage, m_sta[ich], this);
//				Assert(nUnicode != 0);
//				if ((wGlyphID = g_cman.PseudoForUnicode(nUnicode)) == 0)
//					wGlyphID = pfont->GlyphFromCmap(nUnicode, this);
//				Assert(wGlyphID != 0);
//				rgchwUniToGlyphID[nUnicode] = wGlyphID;
//				rgnGlyphIDToUni[wGlyphID] = nUnicode;
//			}
//		}
//		else
//		{
//			Assert(wFirst <= wLast);
//			for (w = wFirst; w <= wLast; w++)
//			{
//				nUnicode = pfont->UnicodeFromCodePage(m_wCodePage, w, this);
//				Assert(nUnicode != 0);
//				if ((wGlyphID = g_cman.PseudoForUnicode(nUnicode)) == 0)
//					wGlyphID = pfont->GlyphFromCmap(nUnicode, this);
//				Assert(wGlyphID != 0);
//				rgchwUniToGlyphID[nUnicode] = wGlyphID;
//				rgnGlyphIDToUni[wGlyphID] = nUnicode;
//
//				// Just in case, since incrementing 0xFFFF will produce zero.
//				if (w == 0xFFFF)
//					break;
//			}
//		}
		break;

	case kglftPseudo:
		Assert(m_nFirst == 0);
		Assert(m_nLast == 0);
		Assert(m_pglfOutput);
		//	While we're at it, handle the output glyph ID.
		m_pglfOutput->DebugCmapForMember(pfont, rgchwUniToGlyphID, rgnGlyphIDToUni);

		if (m_nUnicodeInput != 0)
		{
			//	It is the assigned glyph ID which is the 'contents' of this glyph defn.
//			rgchwUniToGlyphID[m_nUnicodeInput - pfont->MinUnicode()] = m_wPseudo;
			//rgnGlyphIDToUni[m_wPseudo] = m_nUnicodeInput;
		}
		break;

	default:
		Assert(false);
	}
}


/*----------------------------------------------------------------------------------------------
	Output a number in hex format.
----------------------------------------------------------------------------------------------*/
void GrcManager::DebugHex(std::ostream & strmOut, utf16 wGlyphID)
{
	char rgch[20];
	itoa(wGlyphID, rgch, 16);
	strmOut << "0x";
	if (wGlyphID <= 0x0fff) strmOut << "0";
	if (wGlyphID <= 0x00ff) strmOut << "0";
	if (wGlyphID <= 0x000f) strmOut << "0";
	strmOut << rgch;
}

/*----------------------------------------------------------------------------------------------
	Output a Unicode codepoint in hex format.
----------------------------------------------------------------------------------------------*/
void GrcManager::DebugUnicode(std::ostream & strmOut, int nUnicode, bool f32bit)
{
	char rgch[20];
	itoa(nUnicode, rgch, 16);
	strmOut << "U+";
	if (f32bit)
	{
		if (nUnicode <= 0x0FFFFFFF) strmOut << "0";
		if (nUnicode <= 0x00FFFFFF) strmOut << "0";
		if (nUnicode <= 0x000FFFFF) strmOut << "0";
		if (nUnicode <= 0x0000FFFF) strmOut << "0";
	}
	if (nUnicode <= 0x0fff) strmOut << "0";
	if (nUnicode <= 0x00ff) strmOut << "0";
	if (nUnicode <= 0x000f) strmOut << "0";
	strmOut << rgch;
}
