/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: PreCompiler.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Methods to implement the pre-compiler, which does error checking and adjustments.
-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Tables, Passes, and Rules
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Do the pre-compilation tasks for the tables, passes, and rules. Return false if
	compilation cannot continue due to an unrecoverable error.
----------------------------------------------------------------------------------------------*/
bool GrcManager::PreCompileRules(GrcFont * pfont)
{
	int cpassValid;	// number of valid passes

	if (!m_prndr->CheckTablesAndPasses(this, &cpassValid))
		return false;

	//	Fix up the rules that have items in the context before the first modified item.
	Symbol psymAnyClass = m_psymtbl->FindSymbol("ANY");
	if (!m_prndr->FixRulePreContexts(psymAnyClass))
		return false;

	//	In preparation for the next step, create the fsm class vectors for each pass.
	m_prgvpglfcFsmClasses = new Vector<GdlGlyphClassDefn *>[cpassValid];

	if (!AssignClassInternalIDs())
		return false;

	if (!m_prndr->CheckRulesForErrors(m_pgax, pfont))
		return false;

	if (!m_prndr->CheckLBsInRules())
		return false;

	m_prndr->ReplaceKern(this);

	int fxdVersionNeeded;
	if (!CompatibleWithVersion(FontTableVersion(), &fxdVersionNeeded))
	{
		if (UserSpecifiedVersion())
			g_errorList.AddWarning(3501, NULL,
				"Version ",
				VersionString(FontTableVersion()),
				" of the font tables is inadequate for your specfication; version ",
				VersionString(fxdVersionNeeded),
				" will be generated instead.");
		SetFontTableVersion(fxdVersionNeeded, false);
	}

	return true;
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Assign each pass a global ID number. Record a warning if the pass numbers are not
	sequential for a given table, or if there are rules in an unspecified pass. Return
	the number of valid passes (those with rules in them).
----------------------------------------------------------------------------------------------*/
bool GdlRenderer::CheckTablesAndPasses(GrcManager * pcman, int * pcpassValid)
{
	int nPassNum = 0;

	GdlRuleTable * prultbl;

	if ((prultbl = FindRuleTable("linebreak")) != NULL)
		prultbl->CheckTablesAndPasses(pcman, &nPassNum);

	if ((prultbl = FindRuleTable("substitution")) != NULL)
		prultbl->CheckTablesAndPasses(pcman, &nPassNum);

	if (m_fBidi)
		m_iPassBidi = nPassNum;
	else
		m_iPassBidi = -1;

	if ((prultbl = FindRuleTable("justification")) != NULL)
		prultbl->CheckTablesAndPasses(pcman, &nPassNum);

	if ((prultbl = FindRuleTable("positioning")) != NULL)
		prultbl->CheckTablesAndPasses(pcman, &nPassNum);

	//	At this point nPassNum = the number of valid passes.
	*pcpassValid = nPassNum;

	if (nPassNum >= kMaxPasses)
	{
		char rgch1[20];
		char rgch2[20];
		itoa(nPassNum, rgch1, 10);
		itoa(kMaxPasses - 1, rgch2, 10);
		g_errorList.AddError(3101, NULL,
			"Number of passes (",
			rgch1,
			") exceeds maximum of ",
			rgch2);
	}
	else if (nPassNum == 0)
	{
		g_errorList.AddWarning(3502, NULL,
			"No valid passes");
	}

	return true;
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::CheckTablesAndPasses(GrcManager * pcman, int *pnPassNum)
{
	if (m_vppass.Size() > 1 && m_vppass[0]->HasRules())
	{
		g_errorList.AddError(3102, this,
			m_psymName->FullName(),
			" table is multi-pass, so all rules must be explicitly placed in a pass");

		// but go ahead and treat it as the zeroth pass for now
	}

	for (int ipass = 0; ipass < m_vppass.Size(); ++ipass)
	{
		if (m_vppass[ipass]->HasRules())
		{
			m_vppass[ipass]->AssignGlobalID(*pnPassNum);
			(*pnPassNum)++;
			if (pcman->Renderer()->Bidi()
				&& (m_psymName->LastFieldIs("positioning") || m_psymName->LastFieldIs("justification")))
			{
				m_vppass[ipass]->SetPreBidiPass(1);
			}
		}
		else if (ipass == 0)
		{
			// okay--zeroth pass *should* be empty if there are other passes
			m_vppass[ipass]->AssignGlobalID(-1);
		}
		else
		{
			char rgch[20];
			itoa(ipass, rgch, 10);
			g_errorList.AddWarning(3503, this,
				"Pass ",
				StrAnsi(rgch),
				" of ",
				m_psymName->FullName(),
				" table contains no rules");
			m_vppass[ipass]->AssignGlobalID(-1);
		}
	}
}

/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Add ANY classes to the beginning of rules to ensure that all rules in a pass have
	the same number of items before the first modified item.
----------------------------------------------------------------------------------------------*/
bool GdlRenderer::FixRulePreContexts(Symbol psymAnyClass)
{
	for (int iprultbl = 0; iprultbl < m_vprultbl.Size(); iprultbl++)
	{
		m_vprultbl[iprultbl]->FixRulePreContexts(psymAnyClass);
	}
	return true;
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::FixRulePreContexts(Symbol psymAnyClass)
{
	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->FixRulePreContexts(psymAnyClass);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::FixRulePreContexts(Symbol psymAnyClass)
{
	m_critMinPreContext = kMaxSlotsPerRule;
	m_critMaxPreContext = 0;

	//	First, calculate the maximum and minimum pre-contexts lengths for all the rules
	//	in this pass. Also record the original rule length to use as a sort key.
	int iprule;
	for (iprule = 0; iprule < m_vprule.Size(); iprule++)
	{
		int crit = m_vprule[iprule]->CountRulePreContexts();
		m_critMinPreContext = min(m_critMinPreContext, crit);
		m_critMaxPreContext = max(m_critMaxPreContext, crit);
	}

	//	Now add "ANY" class slots to the beginning of each rule to make every rule have
	//	the same number of pre-context items.
	for (iprule = 0; iprule < m_vprule.Size(); iprule++)
	{
		m_vprule[iprule]->FixRulePreContexts(psymAnyClass, m_critMaxPreContext);
	}
}

/*----------------------------------------------------------------------------------------------
	Return the number of items at the beginning of the context that are not modified.
	Also record the original rule length to use as a sort key (before it is modified
	by adding ANY classes to the beginning of the rule).
----------------------------------------------------------------------------------------------*/
int GdlRule::CountRulePreContexts()
{
	m_critOriginal = m_vprit.Size();

	m_critPreModContext = 0;
	for (int irit = 0; irit < m_vprit.Size(); irit++)
	{
		if (dynamic_cast<GdlSetAttrItem *>(m_vprit[irit]))
			return m_critPreModContext;
		m_critPreModContext++;
	}

	//	Should have hit at least on modified item.
	Assert(false);
	return m_critPreModContext;
}

/*----------------------------------------------------------------------------------------------
	Add instances of ANY slots to the beginning of the rule until the rule has the given
	number of items before the first modified item.
----------------------------------------------------------------------------------------------*/
void GdlRule::FixRulePreContexts(Symbol psymAnyClass, int critNeeded)
{
	m_critPrependedAnys = critNeeded - m_critPreModContext;
	if (m_critPrependedAnys == 0)
		return;

	for (int iritToAdd = 0; iritToAdd < m_critPrependedAnys; iritToAdd++)
	{
		GdlRuleItem * prit = new GdlRuleItem(psymAnyClass);
		prit->SetLineAndFile(LineAndFile());
		prit->m_iritContextPos = iritToAdd;
		m_vprit.Insert(iritToAdd, prit);
	}

	//	Increment the item positions following the inserted items, and adjust any slot
	//	references.
	for (int irit = m_critPrependedAnys; irit < m_vprit.Size(); irit++)
	{
		m_vprit[irit]->IncContextPosition(m_critPrependedAnys);
		m_vprit[irit]->AdjustSlotRefsForPreAnys(m_critPrependedAnys);
	}

	//	Increment the scan advance position, if any.
	if (m_nScanAdvance > -1)
		m_nScanAdvance += m_critPrependedAnys;
}

/*----------------------------------------------------------------------------------------------
	Adjust slot references to take into account the fact that ANYS have been prepended
	to the beginning of the rule.
----------------------------------------------------------------------------------------------*/
void GdlRuleItem::AdjustSlotRefsForPreAnys(int critPrependedAnys)
{
	if (m_pexpConstraint)
		m_pexpConstraint->AdjustSlotRefsForPreAnys(critPrependedAnys);
}

/*--------------------------------------------------------------------------------------------*/
void GdlLineBreakItem::AdjustSlotRefsForPreAnys(int critPrependedAnys)
{
	GdlRuleItem::AdjustSlotRefsForPreAnys(critPrependedAnys);
}

/*--------------------------------------------------------------------------------------------*/
void GdlSetAttrItem::AdjustSlotRefsForPreAnys(int critPrependedAnys)
{
	GdlRuleItem::AdjustSlotRefsForPreAnys(critPrependedAnys);

	for (int i = 0; i < m_vpavs.Size(); i++)
		m_vpavs[i]->AdjustSlotRefsForPreAnys(critPrependedAnys, this);
}

/*--------------------------------------------------------------------------------------------*/
void GdlSubstitutionItem::AdjustSlotRefsForPreAnys(int critPrependedAnys)
{
	GdlSetAttrItem::AdjustSlotRefsForPreAnys(critPrependedAnys);

	if (m_pexpSelector)
		m_pexpSelector->AdjustSlotRefsForPreAnys(critPrependedAnys);

	for (int ipexp = 0; ipexp < m_vpexpAssocs.Size(); ipexp++)
		m_vpexpAssocs[ipexp]->AdjustSlotRefsForPreAnys(critPrependedAnys);
}

/*--------------------------------------------------------------------------------------------*/
void GdlAttrValueSpec::AdjustSlotRefsForPreAnys(int critPrependedAnys, GdlRuleItem * prit)
{
	Assert(m_psymName->FitsSymbolType(ksymtSlotAttr));

	m_pexpValue->AdjustSlotRefsForPreAnys(critPrependedAnys);
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Mark classes that are used for substitution and FSM matching, and assign them
	internal IDs.
----------------------------------------------------------------------------------------------*/
bool GrcManager::AssignClassInternalIDs()
{
	Set<GdlGlyphClassDefn *> setpglfc;
	m_prndr->MarkReplacementClasses(this, setpglfc);

	//	Now that we've given warnings about invalid glyphs, delete them from the classes.
	m_prndr->DeleteAllBadGlyphs();

	//	Now actually assign the IDs for all substitution classes in the resulting set.
	//	The first batch of sub-IDs are assigned to the output classes, the second
	//	batch to input classes. Note that some classes may have both an input
	//	and an output ID.
	int nSubID = 0;
	Set<GdlGlyphClassDefn *>::iterator itset;
	for (itset = setpglfc.Begin();
		itset != setpglfc.End();
		++itset)
	{
		if ((*itset)->ReplcmtOutputClass())
		{
			(*itset)->SetReplcmtOutputID(nSubID);
			m_vpglfcReplcmtClasses.Push(*itset);
			nSubID++;
		}
	}

	//	Next do the input classes that have only one glyph; they can also be in linear format.
	for (itset = setpglfc.Begin();
		itset != setpglfc.End();
		++itset)
	{
		GdlGlyphClassDefn * pglfc = *itset;
		if (pglfc->ReplcmtInputClass() && pglfc->GlyphIDCount() <= 1)
		{
			if (pglfc->ReplcmtOutputClass())
				//	Already an output class; don't need to include it again.
				pglfc->SetReplcmtInputID(pglfc->ReplcmtOutputID());
			else
			{
				pglfc->SetReplcmtInputID(nSubID);
				m_vpglfcReplcmtClasses.Push(*itset);
				nSubID++;
			}
		}
	}

	m_cpglfcLinear = nSubID;

	//	Finally do the input classes that have multiple glyphs. These are the classes that
	//	cannot be in linear format; they must be in indexed format (glyph ID / index pair,
	//	ordered by glyph ID).

	for (itset = setpglfc.Begin();
		itset != setpglfc.End();
		++itset)
	{
		GdlGlyphClassDefn * pglfc = *itset;
		if (pglfc->ReplcmtInputClass() && pglfc->GlyphIDCount() > 1)
		{
			pglfc->SetReplcmtInputID(nSubID);
			m_vpglfcReplcmtClasses.Push(*itset);
			nSubID++;
		}
	}

	Assert(nSubID == m_vpglfcReplcmtClasses.Size());

	if (nSubID >= kMaxReplcmtClasses)
	{
		char rgch1[20];
		char rgch2[20];
		itoa(nSubID, rgch1, 10);
		itoa(kMaxReplcmtClasses - 1, rgch2, 10);
		g_errorList.AddError(3103, NULL,
			"Number of classes used in glyph substitution (",
			rgch1,
			") exceeds maximum of ",
			rgch2);
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Mark classes that are used for substitution and FSM matching. Also assign internal IDs
	for use in the FSMs.
----------------------------------------------------------------------------------------------*/
void GdlRenderer::MarkReplacementClasses(GrcManager * pcman,
	Set<GdlGlyphClassDefn *> & setpglfc)
{
	for (int iprultbl = 0; iprultbl < m_vprultbl.Size(); iprultbl++)
	{
		m_vprultbl[iprultbl]->MarkReplacementClasses(pcman, setpglfc);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::MarkReplacementClasses(GrcManager * pcman,
	Set<GdlGlyphClassDefn *> & setpglfc)
{
	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->MarkReplacementClasses(pcman, setpglfc);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::MarkReplacementClasses(GrcManager * pcman,
	Set<GdlGlyphClassDefn *> & setpglfc)
{
	for (int iprule = 0; iprule < m_vprule.Size(); iprule++)
	{
		m_vprule[iprule]->MarkReplacementClasses(pcman, m_nGlobalID, setpglfc);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRule::MarkReplacementClasses(GrcManager * pcman, int nPassID,
	Set<GdlGlyphClassDefn *> & setpglfcReplace)
{
	//	Make lists of flags indicating whether each slot serves as an input replacement slot
	//	and/or an output replacement slot.
	Vector<bool> vfInput;
	Vector<bool> vfOutput;
	vfInput.Resize(m_vprit.Size(), false);
	vfOutput.Resize(m_vprit.Size(), false);
	int irit;

	for (irit = 0; irit < m_vprit.Size(); irit++)
	{
		m_vprit[irit]->AssignFsmInternalID(pcman, nPassID);
		m_vprit[irit]->FindSubstitutionSlots(irit, vfInput, vfOutput);
	}

	for (irit = 0; irit < m_vprit.Size(); irit++)
	{
		if (vfInput[irit])
			m_vprit[irit]->MarkClassAsReplacementClass(pcman, setpglfcReplace, true);
		if (vfOutput[irit])
			m_vprit[irit]->MarkClassAsReplacementClass(pcman, setpglfcReplace, false);
	}
}

/*----------------------------------------------------------------------------------------------
	Assign an internal ID to the input class, if any, to be used by the pass's FSM.
----------------------------------------------------------------------------------------------*/
void GdlRuleItem::AssignFsmInternalID(GrcManager * pcman, int nPassID)
{
	if (!m_psymInput)
		//	Already recorded an error--undefined class.
		return;

	if (m_psymInput->Data())
	{
		GdlGlyphClassDefn * pglfcIn = m_psymInput->GlyphClassDefnData();
		Assert(pglfcIn);
		pcman->AddToFsmClasses(pglfcIn, nPassID);
	}
	//	else insertion
}

/*----------------------------------------------------------------------------------------------
	Mark the class as being needed for the FSM for the given pass.
----------------------------------------------------------------------------------------------*/
void GrcManager::AddToFsmClasses(GdlGlyphClassDefn * pglfc, int nPassID)
{
	Vector<GdlGlyphClassDefn *> * pvpglfcThisPass = m_prgvpglfcFsmClasses + nPassID;
	if (pglfc->IsFsmClass(nPassID))
	{
		//	Already assigned an ID for this pass.
		Assert((*pvpglfcThisPass)[pglfc->FsmID(nPassID)] == pglfc);
		return;
	}
	pglfc->MarkFsmClass(nPassID, pvpglfcThisPass->Size());
	pvpglfcThisPass->Push(pglfc);
}

/*----------------------------------------------------------------------------------------------
	Record the FSM ID for the given pass in the class.
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::MarkFsmClass(int nPassID, int nClassID)
{
	m_vfFsm.Resize(nPassID + 1, false);
	m_vnFsmID.Resize(nPassID + 1, -1);

	m_vfFsm[nPassID] = true;
	m_vnFsmID[nPassID] = nClassID;
}

/*----------------------------------------------------------------------------------------------
	If this item is performing an substitution, set the flags for input and output slots.
----------------------------------------------------------------------------------------------*/
void GdlRuleItem::FindSubstitutionSlots(int irit,
	Vector<bool> & vfInput, Vector<bool> & vfOutput)
{
	//	Do nothing.
}

/*--------------------------------------------------------------------------------------------*/
void GdlSubstitutionItem::FindSubstitutionSlots(int irit,
	Vector<bool> & vfInput, Vector<bool> & vfOutput)
{
	if (!m_psymOutput)
	{
		return;	// no output, therefore no substitution (if another item does a substitution
				// based on the input, that item will set the flag)
	}

	if (m_psymOutput->FitsSymbolType(ksymtClass))
	{
		if (m_pexpSelector)
		{
			//	The selector indicates the input slot.
			int nValue = m_pexpSelector->SlotNumber();
			if (nValue >= 1 && nValue <= vfInput.Size())
				vfInput[nValue - 1] = true;	// selectors are 1-based
			else
				//	error condition--already handled
				return;
		}
		else if (m_psymInput->FitsSymbolType(ksymtSpecialUnderscore))
		{	// no input class, but the output class still needs an output ID.
		}
		else
			vfInput[irit] = true;

		vfOutput[irit] = true;
	}
}

/*----------------------------------------------------------------------------------------------
	Mark this item's class as a replacement class, and add it to the list.
----------------------------------------------------------------------------------------------*/
void GdlRuleItem::MarkClassAsReplacementClass(GrcManager * pcman,
	Set<GdlGlyphClassDefn *> & setpglfcReplace, bool fInput)
{
	GdlDefn * pdefn;
	if (fInput)
	{
		if (m_psymInput)
			pdefn = m_psymInput->Data();
		else
		{
			g_errorList.AddError(3104, this,
				"Item ",
				PosString(),
				" used as selector has no class specified");
			return;
		}
	}
	else // output
	{
		if (OutputSymbol())
		{
			//	This item must be a substitution item itself.
			Assert(dynamic_cast<GdlSubstitutionItem*>(this));
			Assert(OutputSymbol()->FitsSymbolType(ksymtClass));
			pdefn = OutputSymbol()->Data();
		}
	}

	if (!pdefn)
		return;	// error--undefined class

	GdlGlyphClassDefn * pglfc = dynamic_cast<GdlGlyphClassDefn *>(pdefn);
	Assert(pglfc);

	if (pcman->IgnoreBadGlyphs())
		pglfc->WarnAboutBadGlyphs(true);

	(fInput) ? pglfc->MarkReplcmtInputClass() : pglfc->MarkReplcmtOutputClass();

	if (fInput)
	{
		int cw = pglfc->GlyphIDCount();
		if (cw > kMaxGlyphsPerInputClass)
		{
			char rgchMax[20];
			itoa(kMaxGlyphsPerInputClass, rgchMax, 10);
			char rgchCount[20];
			itoa(cw, rgchCount, 10);
			g_errorList.AddError(3105, this,
				"Number of glyphs (",
				rgchCount,
				") in class ",
				pglfc->Name(),
				" exceeds maximum of ",
				rgchMax,
				" allowed for input side of substitution");
		}
	}

	setpglfcReplace.Insert(pglfc);
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Do general error checking for all rules:
	* inappropriate inclusion of LHS (substitution items)
	* LB symbol in line-break pass
	* non-integer selector or association
	* inserted item used as selector/association
	* in-rule constraint on inserted item
	* LB used as selector/association
	* attribute assignment inappropriate for table type
	* inappropriate presence or absence of unit specifier
	* inappropriate operator type (eg, assignment rather than logical)
	* setting the attribute or association of a deleted item
	* mismatch between associations component.X.ref settings
	* reference to slot beyond length of rule
	* setting something other than a slot attribute
	* too many slots in the rule
	* invalid glyphs in replacement class
	* mismatch between size of class in left- and right-hand-sides
	* invalid user-definable slot attribute
----------------------------------------------------------------------------------------------*/
bool GdlRenderer::CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont)
{
	for (int iprultbl = 0; iprultbl < m_vprultbl.Size(); iprultbl++)
	{
		m_vprultbl[iprultbl]->CheckRulesForErrors(pgax, pfont, this);
	}

	return true;
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
	GdlRenderer * prndr)
{
	int grfrco = kfrcoNone;

	if (m_psymName->LastFieldIs("linebreak"))
		grfrco = kfrcoSetBreak | kfrcoSetDir | kfrcoPreBidi;
	else if (m_psymName->LastFieldIs("substitution"))
		grfrco = kfrcoLb | kfrcoSubst | kfrcoSetCompRef | kfrcoSetDir |
					kfrcoSetInsert | kfrcoPreBidi;
	else if (m_psymName->LastFieldIs("justification"))
		grfrco = kfrcoNeedJust | kfrcoLb | kfrcoSubst | kfrcoSetCompRef |
					kfrcoSetInsert;
	else if (m_psymName->LastFieldIs("positioning"))
		grfrco = kfrcoLb | kfrcoSetInsert | kfrcoSetPos;
	else
		Assert(false);

	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->CheckRulesForErrors(pgax, pfont, prndr, m_psymName, grfrco);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
	GdlRenderer * prndr, Symbol psymTable, int grfrco)
{
	for (int iprule = 0; iprule < m_vprule.Size(); iprule++)
	{
		m_vprule[iprule]->CheckRulesForErrors(pgax, pfont, prndr, psymTable, grfrco);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRule::CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
	GdlRenderer * prndr, Symbol psymTable, int grfrco)
{
	if (m_vprit.Size() > kMaxSlotsPerRule)
	{
		char rgchMax[20];
		itoa(kMaxSlotsPerRule, rgchMax, 10);
		char rgchCount[20];
		itoa(m_vprit.Size(), rgchCount, 10);
		g_errorList.AddError(3106, this,
			"Number of slots (",
			rgchCount,
			") exceeds maximum of ",
			rgchMax);
	}

	//	Create lists of flags indicating which items are line-break items, insertions,
	//	or deletions. Also create a vector giving the size of classes in the left-hand-side.
	Vector<bool> vfLb;
	Vector<bool> vfInsertion;
	Vector<bool> vfDeletion;
	int crit = m_vprit.Size();
	vfLb.Resize(crit, false);
	vfInsertion.Resize(crit, false);
	vfDeletion.Resize(crit, false);
	Vector<int> vcwClassSizes;
	vcwClassSizes.Resize(crit, false);
	int irit;
	for (irit = 0; irit < crit; irit++)
	{
		GdlRuleItem * prit = m_vprit[irit];
		GdlLineBreakItem * pritlb = dynamic_cast<GdlLineBreakItem *>(prit);
		if (pritlb)
			vfLb[irit] = true;
		if (!prit->m_psymInput)
		{	//	Invalid class
			vcwClassSizes[irit] = 0;
		}
		else
		{
			if (prit->OutputSymbol()->FitsSymbolType(ksymtSpecialUnderscore))
				vfDeletion[irit] = true;
			if (prit->m_psymInput->FitsSymbolType(ksymtSpecialUnderscore))
				vfInsertion[irit] = true;
			if (prit->m_psymInput->FitsSymbolType(ksymtClass))
			{
				GdlGlyphClassDefn * pglfc = prit->m_psymInput->GlyphClassDefnData();
				if (pglfc)
					vcwClassSizes[irit] = pglfc->GlyphIDCount();
				else
					vcwClassSizes[irit] = 0;
			}
			else vcwClassSizes[irit] = 0;
		}
	}

	bool fOkay = true;

	if (grfrco & kfrcoNeedJust)
	{
		// Check that there is a constraint on the rule that tests for justification status.
		CheckForJustificationConstraint();
	}

	//	Do the checks for each item.
	for (irit = 0; irit < crit; irit++)
	{
//		if (m_nScanAdvance > -1 && irit >= m_nScanAdvance &&
//			(vfInsertion[irit] || vfDeletion[irit]))
//		{
//			g_errorList.AddError(3107, m_vprit[irit],
//				"Cannot place the scan advance position (^) before insertions or deletions");
//			fOkay = false;
//		}

		if (!m_vprit[irit]->CheckRulesForErrors(pgax, pfont, prndr, psymTable,
				grfrco, irit, vfLb,
				vfInsertion, vfDeletion, vcwClassSizes))
		{
			fOkay = false;
		}
	}

	//	If all items were okay, calculate the input and ouput indices and make the
	//	adjustments.
	if (fOkay)
	{
		CalculateIOIndices();
		if (grfrco & kfrcoSetPos)	// positioning pass
			GiveOverlapWarnings(pfont, prndr->ScriptDirections());
	}
	else
	{
		m_fBadRule = true;
	}
}

/*--------------------------------------------------------------------------------------------*/
bool GdlRuleItem::CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
	GdlRenderer * prndr, Symbol psymTable, int grfrco, int irit,
	Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
	Vector<int> & vcwClassSizes)
{
	bool fOkay = true;

	//	Check constraint.
	if (m_pexpConstraint)
	{
		ExpressionType expt;
		bool fKeepChecking = m_pexpConstraint->CheckTypeAndUnits(&expt);
		if (expt != kexptBoolean && expt != kexptOne && expt != kexptZero)
		{
			g_errorList.AddWarning(3504, m_pexpConstraint,
				"Boolean value expected as result of constraint");
		}
		if (fKeepChecking)
		{
			if (!m_pexpConstraint->CheckRuleExpression(pfont, prndr, vfLb, vfIns, vfDel,
				false, false))
			{
				fOkay = false;
			}
		}
		if (fOkay)
		{
			bool fCanSub;
			Set<Symbol> setBogus;
			GdlExpression * pexpNew =
				m_pexpConstraint->SimplifyAndUnscale(pgax, 0, setBogus, pfont, false, &fCanSub);
			if (pexpNew && pexpNew != m_pexpConstraint)
			{
				if (fCanSub)
				{
					delete m_pexpConstraint;
					m_pexpConstraint = pexpNew;
				}
				else
					delete pexpNew;
			}
		}
	}

	return fOkay;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlLineBreakItem::CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
	GdlRenderer * prndr, Symbol psymTable, int grfrco, int irit,
	Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
	Vector<int> & vcwSizes)
{
	bool fOkay = true;

	if ((grfrco & kfrcoLb) == 0)
	{
		g_errorList.AddError(3108, this,
			"Line-break inappropriate in ",
			psymTable->FullName(),
			" table");
		fOkay = false;
	}

	//	method on superclass: check constraints.
	if(!GdlRuleItem::CheckRulesForErrors(pgax, pfont, prndr, psymTable, grfrco, irit,
		vfLb, vfIns, vfDel, vcwSizes))
	{
		fOkay = false;
	}

	return fOkay;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlSetAttrItem::CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
	GdlRenderer * prndr, Symbol psymTable, int grfrco,
	int irit, Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
	Vector<int> & vcwSizes)
{
	bool fOkay = GdlRuleItem::CheckRulesForErrors(pgax, pfont, prndr, psymTable, grfrco, irit,
			vfLb, vfIns, vfDel, vcwSizes);

	for (int ipavs = 0; ipavs < m_vpavs.Size(); ipavs++)
	{
		if (!m_vpavs[ipavs]->CheckRulesForErrors(pgax, pfont, prndr, psymTable,
			grfrco, this, irit, vfLb, vfIns, vfDel))
		{
			fOkay = false;
		}
	}

	return fOkay;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlSubstitutionItem::CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
	GdlRenderer * prndr, Symbol psymTable, int grfrco,
	int irit, Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
	Vector<int> & vcwClassSizes)
{
	int crit = vfLb.Size();

	bool fOkay = true;
	if ((grfrco & kfrcoSubst) == 0)
	{
		g_errorList.AddError(3109, this,
			"Substitution (left-hand-side) not permitted in ",
			psymTable->FullName(),
			" table");
		fOkay = false;

	}

	if (OutputSymbol()->FitsSymbolType(ksymtSpecialUnderscore))
	{
		//	Deletion
		if (m_vpavs.Size())
		{
			g_errorList.AddWarning(3505, this,
				"Item ", PosString(),
				": setting attributes of a deleted item");
			for (int ipavs = 0; ipavs < m_vpavs.Size(); ipavs++)
				delete m_vpavs[ipavs];
			m_vpavs.Clear();
			fOkay = false;
		}

		if (m_vpexpAssocs.Size())
		{
			g_errorList.AddWarning(3506, this,
				"Item ", PosString(),
				": setting associations of a deleted item");
			for (int ipexp = 0; ipexp < m_vpexpAssocs.Size(); ipexp++)
				delete m_vpexpAssocs[ipexp];
			m_vpexpAssocs.Clear();
			fOkay = false;
		}
	}
	if (m_psymInput->FitsSymbolType(ksymtSpecialUnderscore))
	{
		//	Insertion
		if (m_vpexpAssocs.Size() == 0)
		{
			g_errorList.AddWarning(3507, this,
				"Item ", PosString(),
				": inserted item was not given association");
		}
		if (m_pexpConstraint)
		{
			g_errorList.AddError(3110, this,
				"Item ", PosString(),
				": cannot include constraint on inserted item");
		}
	}

	//	If there are any component.X.ref settings, give a warning if they are not equal
	//	to the associations.
	Set<int> setsrCompRef;
	for (int ipavs = 0; ipavs < m_vpavs.Size(); ipavs++)
	{
		if (m_vpavs[ipavs]->m_psymName->IsComponentRef())
		{
			GdlSlotRefExpression * pexpsr =
				dynamic_cast<GdlSlotRefExpression *>(m_vpavs[ipavs]->m_pexpValue);
			if (pexpsr)
			{
				int sr = pexpsr->SlotNumber();
				setsrCompRef.Insert(sr);
			}
		}
	}
	if (setsrCompRef.Size() > 0)
	{
		Set<int> setsrAssocs;
		for (int ipexp = 0; ipexp < m_vpexpAssocs.Size() && fOkay; ipexp++)
		{
			int sr = m_vpexpAssocs[ipexp]->SlotNumber();
			setsrAssocs.Insert(sr);
		}

		bool fOkay = (setsrCompRef.Size() == setsrAssocs.Size());
		for (Set<int>::iterator it = setsrCompRef.Begin(); fOkay && it != setsrCompRef.End(); ++it)
		{
			if (!setsrAssocs.IsMember(*it))
				fOkay = false;
		}

		if (!fOkay)
		{
			g_errorList.AddWarning(3508, this,
				"Item ", PosString(),
				": mismatch between associations and component references");
		}
	}

	//	Mismatched class sizes.
	if (OutputSymbol()->FitsSymbolType(ksymtClass))
	{
		int nSel;
		if (m_pexpSelector)
			nSel = m_pexpSelector->SlotNumber() - 1;
		else
			nSel = irit;
		int cwInput = vcwClassSizes[nSel];
		GdlGlyphClassDefn * pglfc = OutputSymbol()->GlyphClassDefnData();
		if (pglfc) // otherwise, undefined class
		{
			int cwOutput = OutputSymbol()->GlyphClassDefnData()->GlyphIDCount();
			if (cwOutput == 0)
				g_errorList.AddWarning(3509, this,
					"Item ", PosString(),
					": empty class '",
					OutputSymbol()->FullName(),
					"' in right-hand-side");
			else if (cwInput == 0 && cwOutput > 1)
				g_errorList.AddWarning(3510, this,
					"Item ", PosString(),
					": class '",
					OutputSymbol()->FullName(),
					"' in rhs has multiple glyphs but selector class is empty");
			else if (cwInput == 0 && cwOutput == 1)
			{	// okay
			}
			else if (cwInput > cwOutput && cwOutput > 1)
				g_errorList.AddWarning(3511, this,
					"Item ", PosString(),
					": class '",
					OutputSymbol()->FullName(),
					"' in rhs is smaller than selector class");
			else if (cwInput < cwOutput)
				g_errorList.AddWarning(3512, this,
					"Item ", PosString(),
					": mismatched class sizes");
			else
				Assert(cwOutput <= 1 || cwInput == cwOutput);
		}
	}

	if (!GdlSetAttrItem::CheckRulesForErrors(pgax, pfont, prndr, psymTable, grfrco, irit,
		vfLb, vfIns, vfDel, vcwClassSizes))
	{
		fOkay = false;
	}

	if (m_pexpSelector) // the lhs item to match in the substitution
	{
		int srSel = m_pexpSelector->SlotNumber();
		if (srSel < 1 || srSel > crit)
		{
			//	error condition--already handled
			fOkay = false;
		}
		else if (vfLb[srSel - 1])
		{
			g_errorList.AddError(3111, this,
				"Item ", PosString(),
				": line-break item (#) cannot serve as glyph selector");
			fOkay = false;
		}

		else if (vfIns[srSel - 1])
		{
			g_errorList.AddError(3112, this,
				"Item ", PosString(),
				": glyph selector cannot indicate an inserted item");
			fOkay = false;
		}
	}

	for (int ipexp = 0; ipexp < m_vpexpAssocs.Size(); ipexp++)
	{
		int srAssoc = m_vpexpAssocs[ipexp]->SlotNumber(); // 1-based
		if (srAssoc < 1 || srAssoc > crit)
		{
			g_errorList.AddError(3113, this,
				"Item ", PosString(),
				": association out of range");
			fOkay = false;
		}

		else if (vfLb[srAssoc - 1])
		{
			g_errorList.AddError(3114, this,
				"Item ", PosString(),
				": association cannot be made with line-break item (#)");
			fOkay = false;
		}

		else if (vfIns[srAssoc - 1])
		{
			g_errorList.AddError(3115, this,
				"Item ", PosString(),
				": association with an inserted item");
			fOkay = false;
		}
	}

	return fOkay;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlAttrValueSpec::CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
	GdlRenderer * prndr, Symbol psymTable, int grfrco,
	GdlRuleItem * prit, int irit,
	Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel)
{
	if (!m_psymOperator->FitsSymbolType(ksymtOpAssign))
		g_errorList.AddError(3116, this,
			"Attribute assignment must use an assignment operator");

	bool fValueIsInputSlot = false;	// true if the value of an attribute indicates a slot
									// in the input stream (ie, comp.X.ref) as opposed to
									// an output slot (ie, attach.to)

	if (!m_psymName->FitsSymbolType(ksymtSlotAttr))
	{
		if (m_psymName->FitsSymbolType(ksymtGlyphAttr))
			g_errorList.AddError(3117, this,
				"Cannot set glyph attributes in rules");
		else if (m_psymName->FitsSymbolType(ksymtGlyphMetric))
			g_errorList.AddError(3118, this,
				"Cannot set glyph metrics");
		else
			g_errorList.AddError(3119, this,
				"Cannot set anything but slot attributes in rules");
		return false;
	}

	bool fOkay = true;

	if (m_psymName->IsReadOnlySlotAttr())
	{
		g_errorList.AddError(3120, this,
			"The '",
			m_psymName->FullName(),
			"' attribute is read-only");
		fOkay = false;
	}

	else if (m_psymName->IsMovement())
	{
		if ((grfrco & kfrcoSetPos) == 0)
		{
			g_errorList.AddError(3121, this,
				"Cannot set the ",
				m_psymName->FullName(),
				" attribute in the ",
				psymTable->FullName(),
				" table");
			fOkay = false;
		}
		GdlGlyphClassDefn * pglfc = prit->OutputSymbol()->GlyphClassDefnData();
		if (pglfc && pglfc->IncludesGlyph(g_cman.LbGlyphId()))
		{
			g_errorList.AddWarning(3513, this,
				"Moving a line-break glyph will have no effect");
		}
		// else undefined class
	}

	else if (m_psymName->IsAttachment())
	{
		if ((grfrco & kfrcoSetPos) == 0)
		{
			g_errorList.AddError(3122, this,
				"Cannot set the ",
				m_psymName->FullName(),
				" attribute in the ",
				psymTable->FullName(),
				" table");
			fOkay = false;
		}
		GdlGlyphClassDefn * pglfcTmp = prit->OutputSymbol()->GlyphClassDefnData();
		if (pglfcTmp && pglfcTmp->IncludesGlyph(g_cman.LbGlyphId()))
		{
			g_errorList.AddWarning(3514, this,
				"Attaching a line-break glyph will have no effect");
		}
	}

	else if (m_psymName->LastFieldIs("breakweight"))
	{
		if ((grfrco & kfrcoSetBreak) == 0)
		{
			g_errorList.AddError(3123, this,
				"Cannot set the breakweight attribute in the ",
				psymTable->FullName(),
				" table");
			fOkay = false;
		}
	}

	else if (m_psymName->IsComponentRef())
	{
		if ((grfrco & kfrcoSetCompRef) == 0)
		{
			g_errorList.AddError(3124, this,
				"Cannot set the ",
				m_psymName->FullName(),
				" attribute in the ",
				psymTable->FullName(),
				" table");
			fOkay = false;
		}
		fValueIsInputSlot = true;
	}

	else if (m_psymName->LastFieldIs("directionality"))
	{
		if ((grfrco & kfrcoSetDir) == 0)
		{
			g_errorList.AddError(3125, this,
				"Cannot set the directionality attribute in the ",
				psymTable->FullName(),
				" table");
			fOkay = false;
		}
	}

	else if (m_psymName->LastFieldIs("insert"))
	{
		if ((grfrco & kfrcoSetInsert) == 0)
		{
			g_errorList.AddError(3126, this,
				"Cannot set the insert attribute in the ",
				psymTable->FullName(),
				" table");
			fOkay = false;
		}
	}

	else if (m_psymName->DoesJustification())
	{
		if (grfrco & kfrcoPreBidi)
		{
			if (m_psymName->LastFieldIs("width"))
			{
				g_errorList.AddWarning(3515, this,
					"Setting ",
					m_psymName->FullName(),
					" too early in the process (should be before the bidi pass)");
			}
		}
		else
		{
			if (m_psymName->LastFieldIs("stretch") || m_psymName->LastFieldIs("shrink") ||
				m_psymName->LastFieldIs("step") || m_psymName->LastFieldIs("weight"))
			{
				g_errorList.AddWarning(3516, this,
					"Setting ",
					m_psymName->FullName(),
					" too late in the process (should be before the bidi pass)");
			}
		}
	}

	else if (m_psymName->IsMeasureAttr())
	{
		// These can pretty much go anywhere, as far as I can see.
	}

	else if (m_psymName->IsUserDefinableSlotAttr())
	{
		int nIndex = m_psymName->UserDefinableSlotAttrIndex();
		if (nIndex < 0)
		{
			g_errorList.AddError(3127, this,
				"Invalid slot attribute: ", m_psymName->FullName());
			fOkay = false;
		}
		else if (nIndex >= kMaxUserDefinableSlotAttrs)
		{
			char rgch[20];
			itoa(kMaxUserDefinableSlotAttrs, rgch, 10);
			g_errorList.AddError(3128, this,
				"Invalid slot attribute: ", m_psymName->FullName(),
				"; maximum is ", rgch);
			fOkay = false;
		}
		else
		{
			prndr->SetNumUserDefn(nIndex);
		}
	}

	else
	{
		Assert(false);
		return false;
	}

	ExpressionType exptExpected = m_psymName->ExpType();
	Assert(exptExpected != kexptUnknown);
	ExpressionType exptFound;
	bool fKeepChecking = m_pexpValue->CheckTypeAndUnits(&exptFound);
	if (!EquivalentTypes(exptExpected, exptFound))
	{
		if (exptExpected == kexptSlotRef)
		{
			//	Make it an error, not a warning, because we can't do adequate checking below.
			g_errorList.AddError(3129, this,
				"Value for ",
				m_psymName->FullName(),
				" attribute must be a slot reference");
			fKeepChecking = false;
			fOkay = false;
		}
		else if (exptExpected == kexptMeas && exptFound == kexptNumber)
		{
			g_errorList.AddWarning(3517, this,
				"Slot attribute ",
				m_psymName->FullName(),
				" expects a scaled number");
		}
		else if (exptFound == kexptUnknown)
		{
			g_errorList.AddError(3130, this,
				"Invalid value for ",
				m_psymName->FullName());
			fOkay = false;
			fKeepChecking = false;
		}
		else if (m_psymName->IsUserDefinableSlotAttr())
		{
			// any value is appropriate
		}
		else
			g_errorList.AddWarning(3518, this,
				"Assigned value has inappropriate type for '",
				m_psymName->FullName(),
				"' attribute");
	}

	if (fKeepChecking)
	{
		int nTmp;
		if (m_psymName->IsAttachTo() && m_pexpValue->ResolveToInteger(&nTmp, true) && nTmp == 0)
		{
			// okay - attach.to = @0 has a special meaning
		}
		else
		{
			if (!m_pexpValue->CheckRuleExpression(pfont, prndr, vfLb, vfIns, vfDel,
				true, fValueIsInputSlot))
			{
				fOkay = false;
			}
			if (m_psymName->IsAttachTo())
			{
				GdlSlotRefExpression * pexpsr = dynamic_cast<GdlSlotRefExpression *>(m_pexpValue);
				if (pexpsr)
				{
					if (pexpsr->SlotNumber() == irit + 1)
						g_errorList.AddWarning(3519, this,
							"Item ", prit->PosString(),
							": slot is being attached to itself--no attachment will result");
				}
			}
		}

		bool fCanSub;
		Set<Symbol> setBogus;
		GdlExpression * pexpNewValue =
			m_pexpValue->SimplifyAndUnscale(pgax, 0, setBogus, pfont, false, &fCanSub);
		if (pexpNewValue && pexpNewValue != m_pexpValue)
		{
			if (fCanSub)
			{
				delete m_pexpValue;
				m_pexpValue = pexpNewValue;
			}
			else
				delete pexpNewValue;
		}

		//	Use a special zero value for attach.at.gpoint and attach.with.gpoint, to
		//	distinguish from the unspecified case.
		if ((m_psymName->IsAttachAtField() || m_psymName->IsAttachWithField()) &&
			m_psymName->LastFieldIs("gpoint"))
		{
			int n;
			m_pexpValue->ResolveToInteger(&n, false);
			if (n == 0)
				m_pexpValue->SetSpecialZero();
		}
	}

	return fOkay;
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Check that there is a constraint on the rule, or on at least one of its items, that
	tests justification status. This is expected in the justification table. If not,
	give a warning.
	TODO: if we implement pass constraints, these tests should be in the pass constraints.
----------------------------------------------------------------------------------------------*/
bool GdlRule::CheckForJustificationConstraint()
{
	bool fFound = false;
	int ipexp;
	for (ipexp = 0; ipexp < m_vpexpConstraints.Size(); ipexp++)
	{
		GdlExpression * pexp = m_vpexpConstraints[ipexp];
		if (pexp->TestsJustification())
			return true;
	}

	//	No such constraint on the rule itself. Look at the items.
	int irit;
	for (irit = 0; irit < m_vprit.Size(); irit++)
	{
		GdlRuleItem * prit = m_vprit[irit];
		if (prit->CheckForJustificationConstraint())
			return true;
	}

	//	No such constraint on the items. Record a warning.
	g_errorList.AddWarning(3520, this,
		"Rules in justification table should test justification status.");

	return false;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlRuleItem::CheckForJustificationConstraint()
{
	if (m_pexpConstraint)
		return m_pexpConstraint->TestsJustification();
	else
		return false;
}

/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Calculate input and output indices for each rule item, that is, indices relative to the
	input and output streams. Specifically, the input index is the rule index
	ignoring inserted items, and the output index is the rule index ignoring deleted items.
	Make the associations and slot references use the appropriate ones: component.X.ref and
	associations use input indices, and attach.to uses output indices.
	Also calculate the scan-advance value for both input and output.

	Inserted items have a negative input-index, equal to (-1 * the next item's input-index);
	deleted items have a negative output-index, equal to (-1 * the next item's output-index).
----------------------------------------------------------------------------------------------*/

void GdlRule::CalculateIOIndices()
{
	int critInput = 0;
	int critOutput = 0;

	//	Assign the indices for each item. Generate a map of indices to use in later steps.
	int irit;
	for (irit = 0; irit < m_vprit.Size(); irit++)
		m_vprit[irit]->AssignIOIndices(&critInput, &critOutput, m_viritInput, m_viritOutput);

	//	Adjust the slot references to use the new indices.
	for (irit = 0; irit < m_vprit.Size(); irit++)
		m_vprit[irit]->AdjustToIOIndices(m_viritInput, m_viritOutput);


	//	Adjust the scan-advance indicator to take the deleted items into account.
	if (m_nScanAdvance == -1)
	{
		m_nOutputAdvance = -1;
	}
	else
	{
		if (m_nScanAdvance >= m_vprit.Size())
		{
			Assert(m_nScanAdvance == m_vprit.Size());
			m_nOutputAdvance = m_viritOutput[m_vprit.Size() - 1];
			m_nOutputAdvance = (m_nOutputAdvance < 0) ?
				m_nOutputAdvance * -1 :
				m_nOutputAdvance + 1;
		}
		else
		{
			m_nOutputAdvance = m_viritOutput[m_nScanAdvance];
			if (m_nOutputAdvance < 0) // just before a deleted item
				m_nOutputAdvance = (m_nOutputAdvance * -1) - 1;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Assign each rule item an input and an output index.
----------------------------------------------------------------------------------------------*/
void GdlRuleItem::AssignIOIndices(int * pcritInput, int * pcritOutput,
	Vector<int> & viritInput, Vector<int> & viritOutput)
{
	m_nInputIndex = (*pcritInput)++;
	m_nOutputIndex = (*pcritOutput)++;

	viritInput.Push(m_nInputIndex);
	viritOutput.Push(m_nOutputIndex);
}

/*--------------------------------------------------------------------------------------------*/
void GdlSubstitutionItem::AssignIOIndices(int * pcritInput, int * pcritOutput,
	Vector<int> & viritInput, Vector<int> & viritOutput)
{
	if (m_psymInput && m_psymInput->FitsSymbolType(ksymtSpecialUnderscore))
	{
		//	insertion
		m_nInputIndex = (*pcritInput * -1) - 1;
		viritInput.Push(m_nInputIndex);
	}
	else
	{
		m_nInputIndex = (*pcritInput)++;
		viritInput.Push(m_nInputIndex);
	}
	if (m_psymOutput && m_psymOutput->FitsSymbolType(ksymtSpecialUnderscore))
	{
		//	deletion
		m_nOutputIndex = (*pcritOutput * -1) - 1;
		viritOutput.Push(m_nOutputIndex);
	}
	else
	{
		m_nOutputIndex = (*pcritOutput)++;
		viritOutput.Push(m_nOutputIndex);
	}
}


/*----------------------------------------------------------------------------------------------
	Modify the rule items to use either input or output indices.
----------------------------------------------------------------------------------------------*/
void GdlRuleItem::AdjustToIOIndices(Vector<int> & viritInput, Vector<int> & viritOutput)
{
	//	Constraints are read from the input stream.
	if (m_pexpConstraint)
		m_pexpConstraint->AdjustToIOIndices(viritInput, NULL);
}

/*--------------------------------------------------------------------------------------------*/
void GdlLineBreakItem::AdjustToIOIndices(Vector<int> & viritInput, Vector<int> & viritOutput)
{
	GdlRuleItem::AdjustToIOIndices(viritInput, viritOutput);
}

/*--------------------------------------------------------------------------------------------*/
void GdlSetAttrItem::AdjustToIOIndices(Vector<int> & viritInput, Vector<int> & viritOutput)
{
	GdlRuleItem::AdjustToIOIndices(viritInput, viritOutput);

	for (int i = 0; i < m_vpavs.Size(); i++)
		m_vpavs[i]->AdjustToIOIndices(this, viritInput, viritOutput);
}

/*--------------------------------------------------------------------------------------------*/
void GdlSubstitutionItem::AdjustToIOIndices(Vector<int> & viritInput, Vector<int> & viritOutput)
{
	GdlSetAttrItem::AdjustToIOIndices(viritInput, viritOutput);

	//	for selectors: use input indices
	if (m_pexpSelector)
	{
		int sr = m_pexpSelector->SlotNumber();	// 1-based;
		m_nSelector = viritInput[sr - 1];
		Assert(m_nSelector >= 0);	// otherwise, error of using an inserted item as a selector
	}
	else
		m_nSelector = -1; // default, same item

	//	for associations: use input indices
	for (int ipexp = 0; ipexp < m_vpexpAssocs.Size(); ipexp++)
	{
		int srAssoc = m_vpexpAssocs[ipexp]->SlotNumber(); // 1-based
		Assert(srAssoc >= 1 && srAssoc <= viritInput.Size());
		m_vnAssocs.Push(viritInput[srAssoc - 1]);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlAttrValueSpec::AdjustToIOIndices(GdlRuleItem * prit,
	Vector<int> & viritInput, Vector<int> & viritOutput)
{
	Assert(m_psymName->FitsSymbolType(ksymtSlotAttr));

	if (m_psymName->ExpType() == kexptSlotRef)
	{
		if (m_psymName->IsComponentRef())
			m_pexpValue->AdjustToIOIndices(viritInput, NULL);
		else if (m_psymName->IsAttachment())
		{
			Assert(m_psymName->LastFieldIs("to"));	// must be attach.to
			m_pexpValue->AdjustToIOIndices(viritOutput, prit);
		}
		else
			Assert(false);
	}
	else
		m_pexpValue->AdjustToIOIndices(viritInput, NULL);
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Generate warning messages if there are any contiguous glyphs whose bounding boxes
	overlap (in the vertical dimension) but are not attached.
	Note that these checks are only run for positioning rules.
----------------------------------------------------------------------------------------------*/

void GdlRule::GiveOverlapWarnings(GrcFont * pfont, int grfsdc)
{
	int irit;
	for (irit = 0; irit < m_vprit.Size() - 1; irit++)
	{
		GdlRuleItem * prit1 = m_vprit[irit];
		GdlRuleItem * prit2 = m_vprit[irit + 1];

		if (prit1->m_iritContextPosOrig < 0 || prit1->m_iritContextPosOrig >= m_vprit.Size()) // eg, ANY
			continue;
		if (prit2->m_iritContextPosOrig < 0 || prit2->m_iritContextPosOrig >= m_vprit.Size()) // eg, ANY
			continue;

		// Figure out what these items are attached to. A value of zero means it is
		// deliberately attached to nothing.
		int nAtt1 = prit1->AttachedToSlot();
		int nAtt2 = prit2->AttachedToSlot();
		if (nAtt1 == 0 || nAtt2 == 0)
			continue;
		if (nAtt1 == prit2->m_iritContextPos + 1) // m_iritContextPos is 0-based
			continue;
		if (nAtt2 == prit1->m_iritContextPos + 1)
			continue;

		if (prit1->OverlapsWith(prit2, pfont, grfsdc))
		{
			// Give warning.
			g_errorList.AddWarning(3521, this,
				"Vertical overlap between glyphs in items ", prit1->PosString(), " and ",
				prit2->PosString(), "; attachment may be needed");
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Return the slot the item is attached to, or -1 if none.
----------------------------------------------------------------------------------------------*/
int GdlRuleItem::AttachedToSlot()
{
	return -1;
}

int GdlSetAttrItem::AttachedToSlot()
{
	Assert(m_nInputIndex == m_nOutputIndex); // because this is a positioning rule
	for (int ipavs = 0; ipavs < m_vpavs.Size(); ipavs++)
	{
		if (m_vpavs[ipavs]->m_psymName->IsAttachTo())
		{
			int nSlot;
			m_vpavs[ipavs]->m_pexpValue->ResolveToInteger(&nSlot, true);
			return nSlot;
		}
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Return true if any glyph in the recipient item's glyph class overlaps along the
	vertical axis with any glyph in the argument item's glyph class.
----------------------------------------------------------------------------------------------*/
bool GdlRuleItem::OverlapsWith(GdlRuleItem * prit, GrcFont * pfont, int grfsdc)
{
	Symbol psymGlyphs1 = this->m_psymInput;
	Symbol psymGlyphs2 = prit->m_psymInput;

	GdlGlyphClassDefn * glfd1 = psymGlyphs1->GlyphClassDefnData();
	GdlGlyphClassDefn * glfd2 = psymGlyphs2->GlyphClassDefnData();

	if (!glfd1 || !glfd2)
		return false;

	if ((grfsdc & kfsdcHorizLtr || grfsdc == 0) && glfd1->HasOverlapWith(glfd2, pfont))
		return true;
	if (grfsdc & kfsdcHorizRtl && glfd2->HasOverlapWith(glfd1, pfont))
		return true;
	return false;
}

/*----------------------------------------------------------------------------------------------
	Return true if there are any overlaps along the x-axis between any glyphs in the two
	classes.
----------------------------------------------------------------------------------------------*/
bool GdlGlyphClassDefn::HasOverlapWith(GdlGlyphClassMember * pglfdLeft, GrcFont * pfont)
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
	{
		if (m_vpglfdMembers[iglfd]->HasOverlapWith(pglfdLeft, pfont))
			return true;
	}
	return false;
}

bool GdlGlyphDefn::HasOverlapWith(GdlGlyphClassMember * pglfdLeft, GrcFont * pfont)
{
	GdlGlyphDefn * pglfLeft = dynamic_cast<GdlGlyphDefn*>(pglfdLeft);
	if (m_glft == kglftPseudo)
	{
		return m_pglfOutput->HasOverlapWith(pglfdLeft, pfont);
	}
	else if (pglfLeft)
	{
		if (pglfLeft->m_glft == kglftPseudo)
		{
			return HasOverlapWith(pglfLeft->m_pglfOutput, pfont);
		}
		else
		{
			for (int iw1 = 0; iw1 < this->m_vwGlyphIDs.Size(); iw1++)
			{
				utf16 w1 = this->m_vwGlyphIDs[iw1];
				if (w1 == kBadGlyph)
					continue;
				int nLsb = pfont->GetGlyphMetric(w1, kgmetLsb, this);
				for (int iw2 = 0; iw2 < pglfLeft->m_vwGlyphIDs.Size(); iw2++)
				{
					utf16 w2 = pglfLeft->m_vwGlyphIDs[iw2];
					if (w2 == kBadGlyph)
						continue;
					int nRsb = pfont->GetGlyphMetric(w2, kgmetRsb, pglfLeft);
					if (nLsb + nRsb < 0)
						return true;
				}
			}
		}
	}
	else
	{
		GdlGlyphClassDefn * pglfcLeft = dynamic_cast<GdlGlyphClassDefn*>(pglfdLeft);
		Assert(pglfcLeft);
		for (int iglfd = 0; iglfd < pglfcLeft->m_vpglfdMembers.Size(); iglfd++)
		{
			if (HasOverlapWith(pglfcLeft->m_vpglfdMembers[iglfd], pfont))
				return true;
		}
	}
	return false;
}


/**********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Delete all invalid glyphs.
----------------------------------------------------------------------------------------------*/
void GdlRenderer::DeleteAllBadGlyphs()
{
	for (int iglfc = 0; iglfc < m_vpglfc.Size(); iglfc++)
		m_vpglfc[iglfc]->DeleteBadGlyphs();
}

/*----------------------------------------------------------------------------------------------
	Delete invalid glyphs from the class
----------------------------------------------------------------------------------------------*/
bool GdlGlyphClassDefn::DeleteBadGlyphs()
{
	bool fRet = false;
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
	{
		fRet = (fRet || m_vpglfdMembers[iglfd]->DeleteBadGlyphs());
	}
	return fRet;
}

bool GdlGlyphDefn::DeleteBadGlyphs()
{
	bool fRet = false;
	for (int i = m_vwGlyphIDs.Size() - 1; i >=0; i--)
	{
		if (m_vwGlyphIDs[i] == kBadGlyph)
		{
			m_vwGlyphIDs.Delete(i);
			fRet = true;
		}
	}
	return fRet;
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Give a warning about any invalid glyphs in the class.
----------------------------------------------------------------------------------------------*/
bool GdlGlyphClassDefn::WarnAboutBadGlyphs(bool fTop)
{
	bool fRet = false;
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
	{
		fRet = (fRet || m_vpglfdMembers[iglfd]->WarnAboutBadGlyphs(false));
	}
	if (fRet && fTop)
		g_errorList.AddWarning(3522, this,
			"Class '", m_staName, "' is used in substitution but has invalid glyphs");
	return fRet;
}

bool GdlGlyphDefn::WarnAboutBadGlyphs(bool fTop)
{
	Assert(!fTop);
	bool fRet = false;
	for (int i = m_vwGlyphIDs.Size() - 1; i >=0; i--)
	{
		fRet = (fRet || m_vwGlyphIDs[i] == kBadGlyph);
	}
	return fRet;
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Check for the error of cross-line contextualization across more than two lines.
	Record the maximum number of slots occurring in a cross-line contextualization rule.
----------------------------------------------------------------------------------------------*/
bool GdlRenderer::CheckLBsInRules()
{
	for (int iprultbl = 0; iprultbl < m_vprultbl.Size(); iprultbl++)
	{
		m_vprultbl[iprultbl]->CheckLBsInRules();
	}

	return true;
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::CheckLBsInRules()
{
	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->CheckLBsInRules(m_psymName);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::CheckLBsInRules(Symbol psymTable)
{
	m_fLB = false;
	m_fCrossLB = false;
	m_critPreLB = 0;
	m_critPostLB = 0;
	m_fReproc = false;

	int critPreLB = 0;
	int critPostLB = 0;
	bool fReproc = false;

	for (int iprule = 0; iprule < m_vprule.Size(); iprule++)
	{
		if (m_vprule[iprule]->IsBadRule())
			continue;	// don't process if we've discovered errors

		bool fAnyLB = m_vprule[iprule]->CheckLBsInRules(psymTable, &critPreLB, &critPostLB);
		m_fLB = (m_fLB || fAnyLB);
		if (critPreLB > 0 && critPostLB > 0)
		{
			m_fCrossLB = true;
			m_critPreLB = max(m_critPreLB, critPreLB);
			m_critPostLB = max(m_critPostLB, critPostLB);
		}

		if (m_vprule[iprule]->HasReprocessing())
			m_fReproc = true;
	}
	Assert(!m_fCrossLB || m_fLB);
	Assert(!m_fCrossLB || m_critPreLB > 0 || m_critPostLB > 0);
}

/*--------------------------------------------------------------------------------------------*/
bool GdlRule::CheckLBsInRules(Symbol psymTable, int * pcritPreLB, int * pcritPostLB)
{
	//	Check to make sure that there are at most two line-break slots in the rule,
	//	and if there are two, they are the first and last. While we're at it, count the
	//	items before and after the line-break.

	int critLB = 0;
	int critPreTmp = 0;
	int critPostTmp = 0;
	int critPost2Tmp = 0;
	for (int iprit = 0; iprit < m_vprit.Size(); iprit++)
	{
		if (dynamic_cast<GdlSetAttrItem *>(m_vprit[iprit]) == NULL &&
			m_vprit[iprit]->OutputSymbol()->LastFieldIs("#"))
		{
			critLB++;
		}
		else
		{
			if (iprit < m_critPrependedAnys)
			{
				// prepended ANY doesn't count
			}
			else if (critLB == 0)
				critPreTmp++;
			else if (critLB == 1)
				critPostTmp++;
			else
				critPost2Tmp++;
		}
	}

	if (critLB == 0)
	{
		//	No line-breaks in this rule.
		*pcritPreLB = 0;
		*pcritPostLB = 0;
		return false;
	}

	if (critLB > 2 || (critLB == 2 && (critPreTmp > 0 || critPost2Tmp > 0)))
	{
		g_errorList.AddError(3131, this,
			"Cross-line contextualization involving more than two lines.");
		return true;
	}

	Assert(critPost2Tmp == 0);

	*pcritPreLB = critPreTmp;
	*pcritPostLB = critPostTmp;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Return true if the rule has its scan position set so that reprocessing will occur.
	As a side effect, if there was a caret in the rule, record the default scan position,
	which will be used later in outputting the rule commands.
----------------------------------------------------------------------------------------------*/
bool GdlRule::HasReprocessing()
{
	if (m_nOutputAdvance == -1)
		return false;

	//	Count the number of unmodified items at the end of the rule; these do not need to
	//	be processed, and the default scan advance position is just before these.
	int iritLim = m_vprit.Size();
	while (iritLim > 0 && !dynamic_cast<GdlSetAttrItem *>(m_vprit[iritLim - 1]))
		iritLim--;

	if (iritLim < m_vprit.Size())
	{
		GdlRuleItem * pritLim = m_vprit[iritLim];
		Assert(pritLim->m_nOutputIndex >= 0);
		m_nDefaultAdvance = pritLim->m_nOutputIndex;
	}
	else
	{
		Assert(iritLim == m_vprit.Size());
		m_nDefaultAdvance = (*m_vprit.Top())->m_nOutputIndex;
		m_nDefaultAdvance++;
		if (m_nDefaultAdvance < 0)
			// Note that when the last slot is a deletion, the ++ above in effect subtracted
			// 1, reflecting the fact that their is no output for the deletion.
			m_nDefaultAdvance = m_nDefaultAdvance * -1;
	}

	return (m_nOutputAdvance < m_nDefaultAdvance);
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Replace any kern assigments with the equivalent shift and advance.
----------------------------------------------------------------------------------------------*/
void GdlRenderer::ReplaceKern(GrcManager * pcman)
{
	for (int iprultbl = 0; iprultbl < m_vprultbl.Size(); iprultbl++)
	{
		m_vprultbl[iprultbl]->ReplaceKern(pcman);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::ReplaceKern(GrcManager * pcman)
{
	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->ReplaceKern(pcman);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::ReplaceKern(GrcManager * pcman)
{
	for (int iprule = 0; iprule < m_vprule.Size(); iprule++)
	{
		m_vprule[iprule]->ReplaceKern(pcman);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRule::ReplaceKern(GrcManager * pcman)
{
	for (int iprit = 0; iprit < m_vprit.Size(); iprit++)
	{
		m_vprit[iprit]->ReplaceKern(pcman);
	}
}


/*--------------------------------------------------------------------------------------------*/
void GdlRuleItem::ReplaceKern(GrcManager * pcman)
{
	//	Do nothing.
}

/*--------------------------------------------------------------------------------------------*/
void GdlSetAttrItem::ReplaceKern(GrcManager * pcman)
{
	for (int ipavs = 0; ipavs < m_vpavs.Size(); ipavs++)
	{
		GdlAttrValueSpec * pavsShift;
		GdlAttrValueSpec * pavsAdvance;
		bool fKern = m_vpavs[ipavs]->ReplaceKern(pcman, &pavsShift, &pavsAdvance);
		if (fKern)
		{
			delete m_vpavs[ipavs];
			m_vpavs[ipavs] = pavsShift;
			m_vpavs.Insert(ipavs + 1, pavsAdvance);
			ipavs++;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	If this a statement setting the kern attribute, generate the corresponding shift and
	advance statements, and return true. Return false otherwise.
	Specifically,
		kern.x = -10m		becomes		shift.x = -10m; adv.x = advancewidth - 10m

		kern.x += 10m		becomes		shift.x += 10m; adv.x += 10m
----------------------------------------------------------------------------------------------*/
bool GdlAttrValueSpec::ReplaceKern(GrcManager * pcman,
	GdlAttrValueSpec ** ppavsShift, GdlAttrValueSpec ** ppavsAdvance)
{
	if (!m_psymName->FieldIs(0, "kern"))
		return false;

	GrcStructName xns;
	m_psymName->GetStructuredName(&xns);
	xns.DeleteField(0);
	xns.InsertField(0, "advance");
	Symbol psymNameAdvance = pcman->SymbolTable()->FindSymbol(xns);
	if (!psymNameAdvance)
	{
		g_errorList.AddError(3132, this,
			"Invalid kern assignment");
		return false;
	}
	xns.DeleteField(0);
	xns.InsertField(0, "shift");
	Symbol psymNameShift = pcman->SymbolTable()->FindSymbol(xns);
	if (!psymNameShift)
	{
		g_errorList.AddError(3133, this,
			"Invalid kern assignment");
		return false;
	}

	GdlExpression * pexpValueShift = m_pexpValue->Clone();
	*ppavsShift = new GdlAttrValueSpec(psymNameShift, m_psymOperator, pexpValueShift);

	GdlExpression * pexpValueAdvance = m_pexpValue->Clone();

	if (m_psymOperator->FieldIs(0, "="))
	{
		//	Base 'advance' off of advancewidth (or advanceheight).
		Symbol psymAdvMetric =
			pcman->SymbolTable()->FindSymbol("advancewidth");
		if (xns.FieldEquals(1, "y"))
			psymAdvMetric = pcman->SymbolTable()->FindSymbol("advanceheight");
		Assert(psymAdvMetric);
		GdlLookupExpression * pexplook = new GdlLookupExpression(psymAdvMetric);
		Symbol psymPlus = pcman->SymbolTable()->FindSymbol("+");
		Assert(psymPlus);
		pexpValueAdvance = new GdlBinaryExpression(psymPlus, pexplook, pexpValueAdvance);
	}

	*ppavsAdvance = new GdlAttrValueSpec(psymNameAdvance, m_psymOperator, pexpValueAdvance);
	return true;
}

/**********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Check that the rules and glyph attributes are compatible with the requested version.
	If not, return the version required.

	This routine assumes that we can always sucessfully use a later version.
----------------------------------------------------------------------------------------------*/
bool GrcManager::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	*pfxdNeeded = fxdVersion;

	if (fxdVersion >= kfxdCompilerVersion)
		return true;

	if (!m_fBasicJust)
	{
		*pfxdNeeded = max(0x00020000, *pfxdNeeded);
	}

	if (m_vpglfcReplcmtClasses.Size() >= kMaxReplcmtClassesV1_2)
	{
		*pfxdNeeded = max(0x00030000, *pfxdNeeded);
	}

	bool fRet = (*pfxdNeeded <= fxdVersion);

	fRet = (m_prndr->CompatibleWithVersion(fxdVersion, pfxdNeeded) && fRet);

	return fRet;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlRenderer::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	bool fRet = true;

	//	Glyph atrributes:
	for (int ipglfc = 0; ipglfc < m_vpglfc.Size(); ipglfc++)
	{
		fRet = m_vpglfc[ipglfc]->CompatibleWithVersion(fxdVersion, pfxdNeeded) && fRet;
	}
	//	Rules:
	for (int iprultbl = 0; iprultbl < m_vprultbl.Size(); iprultbl++)
	{
		fRet = m_vprultbl[iprultbl]->CompatibleWithVersion(fxdVersion, pfxdNeeded) && fRet;
	}
	return fRet;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlGlyphClassDefn::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	bool fRet = true;

	//	For each attribute assignment in the value list:
	for (int ipglfa = 0; ipglfa < m_vpglfaAttrs.Size(); ipglfa++)
	{
		Symbol psym = m_vpglfaAttrs[ipglfa]->GlyphSymbol();
		if (psym->IsMeasureAttr() || psym->DoesJustification())
		{
			*pfxdNeeded = max(*pfxdNeeded, 0x00020000);
			fRet = false;
		}
	}
	return fRet;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlRuleTable::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	bool fRet = true;
	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		fRet = m_vppass[ippass]->CompatibleWithVersion(fxdVersion, pfxdNeeded) && fRet;
	}
	return fRet;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlPass::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	bool fRet = true;
	for (int iprule = 0; iprule < m_vprule.Size(); iprule++)
	{
		fRet = m_vprule[iprule]->CompatibleWithVersion(fxdVersion, pfxdNeeded) && fRet;
	}
	return fRet;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlRule::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	bool fRet = true;
	for (int iprit = 0; iprit < m_vprit.Size(); iprit++)
	{
		fRet = m_vprit[iprit]->CompatibleWithVersion(fxdVersion, pfxdNeeded) && fRet;
	}
	return fRet;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlRuleItem::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	if (m_pexpConstraint)
	{
		return m_pexpConstraint->CompatibleWithVersion(fxdVersion, pfxdNeeded);
	}
	else
		return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlSetAttrItem::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	bool fRet = GdlRuleItem::CompatibleWithVersion(fxdVersion, pfxdNeeded);

	for (int ipavs = 0; ipavs < m_vpavs.Size(); ipavs++)
	{
		fRet = m_vpavs[ipavs]->CompatibleWithVersion(fxdVersion, pfxdNeeded) && fRet;
	}
	return fRet;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlAttrValueSpec::CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
{
	if (m_psymName->IsMeasureAttr() || m_psymName->DoesJustification())
	{
		*pfxdNeeded = max(*pfxdNeeded, 0x00020000);
		return false;
	}
	else
		return true;
}
