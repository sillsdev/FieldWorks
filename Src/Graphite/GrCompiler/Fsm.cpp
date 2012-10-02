/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Fsm.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Methods for the classes that used in generating the finite state machines.
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
	Generate the finite state machine for each pass.
----------------------------------------------------------------------------------------------*/
void GrcManager::GenerateFsms()
{
	m_prndr->GenerateFsms(this);
	if (OutputDebugFiles())
		DebugFsm();
}

/*--------------------------------------------------------------------------------------------*/
void GdlRenderer::GenerateFsms(GrcManager * pcman)
{
	for(int iprultbl = 0; iprultbl < m_vprultbl.Size(); iprultbl++)
	{
		m_vprultbl[iprultbl]->GenerateFsms(pcman);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::GenerateFsms(GrcManager * pcman)
{
	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->GenerateFsm(pcman);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::GenerateFsm(GrcManager * pcman)
{
	if (m_nGlobalID == -1)
	{
		m_nMaxRuleContext = 0;
		return;
	}

	GenerateFsmMachineClasses(pcman);
	GenerateFsmTable(pcman);

	//WalkFsmMachineClasses();
	////////pcman->ClearFsmWorkSpace();
}

/*----------------------------------------------------------------------------------------------
	Generate the machine classes for the finite state machine for the pass.
	Process:

 *	By this point, each pass knows which classes need to be included in its FSM, and each
	of those classes has been assigned an FSM-ID (pertinent to the pass). This was done by
	the pre-compiler.

 *	For each glyph we figure out what set of source-classes it is a member of; we record
	the source-class-set (SCS) in a big array indexed by glyph.

	Note that each SCS defines a machine-class--group of glyphs that are considered
	equivalent for the purposes of matching input.

 *	So for each SCS, we create a machine class, which knows that SCS and also which glyphs
	have that set as their SCS. These are the glyphs that are members of that machine class.

	We organize the machine classes in a hash-map. The key for the hash map is based on the
	SCS, specifically we use the sum of the FSM-IDs for the classes in the set. This enables
	us, given a glyph, to take its SCS, find candidate machine-classes in the hash map using
	the key, and then search for the one that has a matching SCS. (This is faster than a
	linear search through the entire list of machine classes.)

	At the end of the process we have a list of machine classes that know what glyphs are
	included, and also each glyph knows which machine class it belongs to.
----------------------------------------------------------------------------------------------*/
void GdlPass::GenerateFsmMachineClasses(GrcManager * pcman)
{
	if (m_nGlobalID == -1)
		return;	// no rules in this pass

	InitializeFsmArrays(); // working arrays

	//	Get a list of all the glyph classes that need to be included in the FSM for this
	//	pass.
	Vector<GdlGlyphClassDefn *> * pvpglfcThisPass = pcman->FsmClassesForPass(m_nGlobalID);

	//	For all the glyphs in the classes, record the fact that the glyph is a member
	//	of the class.
	for (int i = 0; i < pvpglfcThisPass->Size(); i++)
		(*pvpglfcThisPass)[i]->RecordInclusionInClass(this);

//	SortFsmInclusions(); // not needed since we are working with sets

	//	At this point each glyph has a set of source classes it is a member of.
	//	That set serves as a unique identifier indicating the machine class. Ie, for
	//	each combination of source classes, we have a different machine class.

	for (utf16 w = 0; w < kMaxTotalGlyphs; w++)
	{
		int ifsmcColumn = AssignGlyphIDToMachineClasses(w, m_nGlobalID);
		if (ifsmcColumn > -1)
			m_hmGlyphToColumn.Insert(w, ifsmcColumn);
	}

	// TODO SharonC: for each machine class, generate debug string based on source class list.

	if (m_critMaxPreContext > m_critMinPreContext)
	{
		//	The ANY class is (probably) being used in the leading context for some of the rules.
		//	Make sure there is a class that corresponds to exactly the ANY class and no other.
		//	(This is needed to match non-existent glyphs before the beginning of the stream.)
		Symbol psymAnyClass = pcman->SymbolTable()->FindSymbol("ANY");
		GdlGlyphClassDefn * pglfc = psymAnyClass->GlyphClassDefnData();
		Assert(pglfc);
		bool fFound = false;
		for (int ifsmc = 0; ifsmc < m_vpfsmc.Size(); ifsmc++)
		{
			if (m_vpfsmc[ifsmc]->MatchesOneSource(pglfc))
			{
				fFound = true;
				break;
			}
		}
		//	The "non-existent glyph" should be a member of the ANY class and no other, so it
		//	should always be possible to find such a machine-class.
		Assert(fFound);

//		if (!fFound)
//		{
//			//	Add a class that corresponds to exactly the ANY class and no other.
//			FsmMachineClass * pfsmc = new FsmMachineClass(m_nGlobalID);
//			pfsmc->SetColumn(m_vpfsmc.Size());
//			pfsmc->m_scs.Insert(pglfc, false);
//			m_vpfsmc.Push(pfsmc);
//		}

	}
}


/*----------------------------------------------------------------------------------------------
	For the given glyph, use its source-class-set to determine which
	machine class it is a member of. Create a new machine class if there is not
	one corresponding to the SCS. Return the column number of the machine class.
----------------------------------------------------------------------------------------------*/
int GdlPass::AssignGlyphIDToMachineClasses(utf16 wGlyphID, int nPassID)
{
	SourceClassSet * pscsThisGlyph = m_rgscsInclusions + wGlyphID;
	if (pscsThisGlyph->Size() == 0)
		//	This glyph does not need to be included in the FSM, because no source classes of
		//	interest included it.
		return -1;

	//	The key is, somewhat arbitrarily, the sum of the FSM-ID's for all the
	//	source-classes in the SCS.
	int nKey = MachineClassKey(wGlyphID, nPassID);

	Vector<FsmMachineClass *> * pvpfsmc;
	FsmMachineClass * pfsmc;
	if (m_hmMachineClassMap.Retrieve(nKey, &pvpfsmc))
	{
		//	We have a list of machine-classes (and their SCS's) whose source-class FSM-ID's
		//	sum up to the key. Look through the list to see if any of them exactly match this
		//	glyph's SCS.
		pfsmc = MachineClassMatching(*pvpfsmc, wGlyphID);
		if (pfsmc)
		{
			//	This glyph is part of the same machine class as some previous glyph(s).
			pfsmc->AddGlyph(wGlyphID);
		}
		else
		{
			//	Create a new machine class corresponding to the combination of source classes.
			//	Make this glyph ID be a member of that machine class.
			pfsmc = new FsmMachineClass(nPassID);
			pfsmc->SetColumn(m_vpfsmc.Size());
			pfsmc->AddGlyph(wGlyphID);
			pfsmc->SetSourceClasses(pscsThisGlyph);

			//	Add the new machine class to the master list.
			m_vpfsmc.Push(pfsmc);

			//	Add the new machine class to the vector for this key, where it belongs.
			pvpfsmc->Push(pfsmc);
		}
	}
	else
	{
		//	Create a new machine class corresponding to the combination of source classes.
		//	Make this glyph ID be a member of that machine class.
		pfsmc = new FsmMachineClass(nPassID);
		pfsmc->SetColumn(m_vpfsmc.Size());
		pfsmc->AddGlyph(wGlyphID);
		pfsmc->SetSourceClasses(pscsThisGlyph);

		//	Add the new machine class to the master list.
		m_vpfsmc.Push(pfsmc);

		//	Put the new machine class in the map, so another glyph ID that is a member of the
		//	same set of source classes can find it.
		pvpfsmc = new Vector<FsmMachineClass *>;
		pvpfsmc->Push(pfsmc);
		m_hmMachineClassMap.Insert(nKey, pvpfsmc);
	}

	//	Record the fact that this glyph is assigned to the machine class that we found
	//	or created.
	m_rgpfsmcAssignments[wGlyphID] = pfsmc;
	return pfsmc->Column();
}


/*----------------------------------------------------------------------------------------------
	For the given glyph ID, answer the key to use in looking up its machine class in the
	map. The key is the sum of the ID's of the source classes in this glyph's set.
	This key mechanism is just a convenient way of partioning the various vectors.
----------------------------------------------------------------------------------------------*/
int GdlPass::MachineClassKey(utf16 wGlyphID, int nPassID)
{
	SourceClassSet * pscs = m_rgscsInclusions + wGlyphID;
	int nKey = 0;
	for (SourceClassSet::iterator itscs = pscs->Begin();
		itscs != pscs->End();
		++itscs)
	{
		Assert((*itscs)->FsmID(nPassID) > -1);
		nKey += (*itscs)->FsmID(nPassID);
	}
	return nKey;
}


int FsmMachineClass::Key(int ipass)
{
	int nKey = 0;
	for (SourceClassSet::iterator itscs = m_scs.Begin();
		itscs != m_scs.End();
		++itscs)
	{
		Assert((*itscs)->FsmID(ipass) > -1);
		nKey += (*itscs)->FsmID(ipass);
	}
	return nKey;
}


/*----------------------------------------------------------------------------------------------
	Add a glyph ID to a machine class. Do it in a way that ensures the list will be sorted.
----------------------------------------------------------------------------------------------*/
void FsmMachineClass::AddGlyph(utf16 w)
{
	if (m_wGlyphs.Size() == 0)
	{
		m_wGlyphs.Push(w);
		return;
	}

	//	Short-cut for common case:
	if (w > *(m_wGlyphs.Top()))
	{
		m_wGlyphs.Push(w);
		return;
	}

	int iLow = 0;
	int iHigh = m_wGlyphs.Size();

	while (iHigh - iLow > 1)
	{
		int iMid = (iHigh + iLow) >> 1;	// divide by 2
		if (w == m_wGlyphs[iMid])
			return;

		if (w < m_wGlyphs[iMid])
			iHigh = iMid;
		else
			iLow = iMid;
	}

	if (w < m_wGlyphs[iLow])
		m_wGlyphs.Insert(iLow, w);
	else if (w > m_wGlyphs[iLow])
	{
		Assert(iHigh == m_wGlyphs.Size() || w < m_wGlyphs[iHigh]);
		m_wGlyphs.Insert(iLow + 1, w);
	}
}


/*----------------------------------------------------------------------------------------------
	If there is a machine class in the given vector whose source-class-set matches the
	source-class-set for the given glyph, return it, otherwise return NULL.
----------------------------------------------------------------------------------------------*/
FsmMachineClass * GdlPass::MachineClassMatching(Vector<FsmMachineClass *> & vpfsmc,
	utf16 wGlyphID)
{
	SourceClassSet * pscsForGlyph = m_rgscsInclusions + wGlyphID;

	for (int ipfsmc = 0; ipfsmc < vpfsmc.Size(); ipfsmc++)
	{
		FsmMachineClass * pfsmc = vpfsmc[ipfsmc];
		if (pfsmc->MatchesSources(pscsForGlyph))
			return pfsmc;
	}
	return NULL;
}


/*----------------------------------------------------------------------------------------------
	Return true if the given source-class-set matches the recipient's SCS;
	return false if they do not match.
----------------------------------------------------------------------------------------------*/
bool FsmMachineClass::MatchesSources(SourceClassSet * pscs)
{
	if (m_scs.Size() != pscs->Size())
		return false;
	for (SourceClassSet::iterator itscs = m_scs.Begin();
		itscs != m_scs.End();
		++itscs)
	{
		if (!pscs->IsMember(*itscs))
			return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return true if the given source-class-set has exactly one item which is the given
	class.
----------------------------------------------------------------------------------------------*/
bool FsmMachineClass::MatchesOneSource(GdlGlyphClassDefn * pglfc)
{
	if (m_scs.Size() != 1)
		return false;
	return (*(m_scs.Begin()) == pglfc);
}

/*----------------------------------------------------------------------------------------------
	Initialize the working arrays that are used in the FSM generation.
----------------------------------------------------------------------------------------------*/
void GdlPass::InitializeFsmArrays()
{
	for (int w = 0; w < kMaxTotalGlyphs; w++)
	{
		m_rgscsInclusions[w].Clear();
		m_rgpfsmcAssignments[w] = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Return a list of all the classes that need to be included in the FSM for
	the given pass.
----------------------------------------------------------------------------------------------*/
Vector<GdlGlyphClassDefn *> * GrcManager::FsmClassesForPass(int nPassID)
{
	return m_prgvpglfcFsmClasses + nPassID;
}


/*----------------------------------------------------------------------------------------------
	For each glyph ID that is a member of the class, record the fact that it is a member
	of this class.
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::RecordInclusionInClass(GdlPass * ppass)
{
	RecordInclusionInClass(ppass, this);
}


/*--------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::RecordInclusionInClass(GdlPass * ppass, GdlGlyphClassDefn * pglfc)
{
	for (int ipglfd = 0; ipglfd < m_vpglfdMembers.Size(); ipglfd++)
		m_vpglfdMembers[ipglfd]->RecordInclusionInClass(ppass, pglfc);
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphDefn::RecordInclusionInClass(GdlPass * ppass, GdlGlyphClassDefn * pglfc)
{
	for (int iw = 0; iw < m_vwGlyphIDs.Size(); iw++)
	{
		utf16 wGlyphID = m_vwGlyphIDs[iw];
		ppass->RecordInclusionInClass(wGlyphID, pglfc);
	}
}


/*----------------------------------------------------------------------------------------------
	Record the fact that the given glyph ID is a member of the given class.
----------------------------------------------------------------------------------------------*/
void GdlPass::RecordInclusionInClass(utf16 wGlyphID, GdlGlyphClassDefn * pglfc)
{
	SourceClassSet * pscs = m_rgscsInclusions + wGlyphID;
	if (!pscs->IsMember(pglfc))
		pscs->Insert(pglfc);
}


/*----------------------------------------------------------------------------------------------
	Sort the list of source-classes-glyphs-are-included-in by their FSM ID. These
	source-class lists serve as unique identifiers; each combination of source-classes
	corresponds to a different machine class. So sorting them makes it straightforward to
	compare them for equality.
	Not needed since we are using sets.
----------------------------------------------------------------------------------------------*/
//void GdlPass::SortFsmInclusions()
//{
//	for (int w = 0; w < kMaxTotalGlyphs; w++)
//	{
//		Vector<GdlGlyphClassDefn *> vpglfcToSort = m_rgscsInclusions[w];
//		Vector<GdlGlyphClassDefn *> vpglfcSorted;
//
//		//	Sort the list by FSM ID.
//		while (vpglfcToSort.Size())
//		{
//			int iFirst = 0;
//			for (int i = 1; i < vpglfcToSort.Size(); i++)
//			{
//				if (vpglfcToSort[i].FsmID(nPassID) < vpglfcToSort[iFirst].FsmID(nPassID))
//					iFirst = i;
//			}
//			vpglfcSorted.Push(vpglfcToSort[iFirst]);
//			vpglfcToSort.Delete(iFirst);
//		}
//		vpglfcToSort.Clear();
//		vpglfcSorted.CopyTo(vpglfcToSort);
//	}
//}



/*----------------------------------------------------------------------------------------------
	Generate the actual FSM table for the pass.
	Process:

 *	Initialize the table with the start state, corresponding to 0 slots matched.

 *	For each rule in the pass, add a state corresponding to 1 slot matched. Keep track of
	the relevant rule(s) and whether the new state is a terminating state for the rule(s).

 *	When we have created all the 1-slot-matched states, merge any equivalent states....

 *	For each 1-slot-matched, add 2-slots-matched states for any rules with at least 2 slots.
	Keep track of the relevant rule(s) and whether the new state is a terminating state for
	the rule(s).

 *	When we have created all the 2-slots-matched states, merge any equivalent states.

 *	Continue this process until we have created terminating states for every rule
	(ie, N-slots-matched state where the rule has N slots).
----------------------------------------------------------------------------------------------*/
void GdlPass::GenerateFsmTable(GrcManager * pcman)
{
	//	Hungarian: ifs = index of FsmState

	if (m_nGlobalID == -1)
	{
		m_nMaxRuleContext = 0;
		return;
	}

	m_pfsm = new FsmTable(m_nGlobalID, NumberOfFsmMachineClasses());
	Assert(m_pfsm->RawNumberOfStates() == 0);

	//	Create the start state, equivalent to no slots matched.
	m_pfsm->AddState(0);
	int ifsCurrent = 0;

	//	Index of the first state whose slot-count == the slot count of currState, ie, the
	//	beginning of the state range which may need to be fixed up as we merge.
	int ifsCurrSlotCntMin = 0;

	//	Index of first state whose slot-count == slot count of currState + 1, ie, the
	//	beginning of the state range in which to check for duplicates.
	int ifsNextSlotCntMin = 1;

	while (ifsCurrent < m_pfsm->RawNumberOfStates())
	{
		FsmState * pfstateCurr = m_pfsm->RawStateAt(ifsCurrent);
		int critSlotsMatched = pfstateCurr->SlotsMatched();
		if (!pfstateCurr->HasBeenMerged())
		{
			for (int iprule = 0; iprule < m_vprule.Size(); iprule++)
			{
				GdlRule * prule = m_vprule[iprule];

				if (ifsCurrent == 0	||	// for state #0, all rules are consider matched
					pfstateCurr->RuleMatched(iprule))
				{
					if (pfstateCurr->SlotsMatched() == prule->NumberOfInputItems())
					{
						pfstateCurr->AddRuleToSuccessList(iprule);
					}
					else
					{
						Set<FsmMachineClass *> setpfsmc;
						GetMachineClassesForRuleItem(prule, critSlotsMatched, setpfsmc);

						for (Set<FsmMachineClass *>::iterator it = setpfsmc.Begin();
							it != setpfsmc.End();
							++it)
						{
							FsmMachineClass * pfsmc = *it;
							int ifsmcColumn = pfsmc->Column();
							int ifsNextState = pfstateCurr->CellValue(ifsmcColumn);
							if (ifsNextState == 0)
							{
								//	Add a new state.
								ifsNextState = m_pfsm->RawNumberOfStates();
								m_pfsm->AddState(critSlotsMatched + 1);
								pfstateCurr->SetCellValue(ifsmcColumn, ifsNextState);
							}

							//	Store this rule as one matched for this state.
							m_pfsm->RawStateAt(ifsNextState)->AddRuleToMatchedList(iprule);
						}

					}
				}
			}
		}

		if (m_pfsm->RawNumberOfStates() > (ifsCurrent + 1) &&
			m_pfsm->RawStateAt(ifsCurrent + 1)->SlotsMatched() != critSlotsMatched)
		{
			//	We have just finished processing a group of states with critSlotsMatched,
			//	which had the effect of creating a group of states where slots-matched ==
			//	critSlotsMatched + 1. There could be duplicates in the latter group,
			//	which are pointed at by the former group. Mark the duplicates so that they
			//	point to the earlier identical state. (Note that since we are currently
			//	not actually deleting the duplicates, we don't need to fix up the earlier
			//	group.)

			MergeIdenticalStates(ifsCurrSlotCntMin, ifsNextSlotCntMin,
				m_pfsm->RawNumberOfStates());
			ifsCurrSlotCntMin = ifsNextSlotCntMin;
			ifsNextSlotCntMin = m_pfsm->RawNumberOfStates();
		}

		ifsCurrent++;	// go on to next state
	}

	m_nMaxRuleContext = m_pfsm->RawStateAt(ifsCurrent - 1)->m_critSlotsMatched;

	ReorderFsmStates();

	GenerateStartStates(pcman);
}


/*----------------------------------------------------------------------------------------------
	Merge identical states in the FSM.
	Arguments:
		ifsFixMin		- beginning of range that may need to have their cells fixed up;
							currently not used, because we are not fixing up the cells
		ifsCheckMin		- beginning of range that may have duplicates;
							also Lim of range that may need to have cells fixed
		ifsCheckLim		- lim of range that may have duplicates
----------------------------------------------------------------------------------------------*/
void GdlPass::MergeIdenticalStates(int ifsFixMin, int ifsCheckMin, int ifsCheckLim)
{
	//	Work backwards so that we can delete with impunity. However, currently this algorithm
	//	doesn't actually delete, it just sets pointers from the invalid to the valid
	//	states.
	for (int ifsLoop = ifsCheckLim; --ifsLoop >= ifsCheckMin;)
	{
		int ifsIdentical;
		if ((ifsIdentical = FindIdenticalState(ifsLoop, ifsCheckMin)) > -1)
			m_pfsm->RawStateAt(ifsLoop)->SetMergedState(m_pfsm->RawStateAt(ifsIdentical));
	}
}


/*----------------------------------------------------------------------------------------------
	Find a previous state that is identical to the given state. Return its index, or -1 if
	none.
	Arguments:
		ifsToMatch		- index of state to match
		ifsMin			- beginning of range to search
----------------------------------------------------------------------------------------------*/
int GdlPass::FindIdenticalState(int ifsToMatch, int ifsMin)
{
	FsmState * pfstateToMatch = m_pfsm->RawStateAt(ifsToMatch);
	Assert(!pfstateToMatch->HasBeenMerged());

	for (int ifsLoop = ifsMin; ifsLoop < ifsToMatch; ifsLoop++)
	{
		FsmState * pfstateToTry = m_pfsm->RawStateAt(ifsLoop);
		if (pfstateToTry->HasBeenMerged())
			continue;
		if (pfstateToMatch->StatesMatch(pfstateToTry))
			return ifsLoop;
	}
	return -1;
}


/*----------------------------------------------------------------------------------------------
	Return true if the recipient state is identical to the argument state, ie, if they
	have the same rules matched and succeeded.
----------------------------------------------------------------------------------------------*/
bool FsmState::StatesMatch(FsmState * pfstate)
{
	Assert(m_critSlotsMatched == pfstate->m_critSlotsMatched);

	if (NumberOfRulesMatched() != pfstate->NumberOfRulesMatched())
		return false;
	if (NumberOfRulesSucceeded() != pfstate->NumberOfRulesSucceeded())
		return false;

	Set<int>::iterator it;
	for (it = m_setiruleMatched.Begin();
		it != m_setiruleMatched.End();
		++it)
	{
		if (!pfstate->RuleMatched(*it))
			return false;
	}

	for (it = m_setiruleSuccess.Begin();
		it != m_setiruleSuccess.End();
		++it)
	{
		if (!pfstate->RuleSucceeded(*it))
			return false;
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Fill in the set with all the machine classes included in the source class of the
	irit'th input item in the given rule.
----------------------------------------------------------------------------------------------*/
void GdlPass::GetMachineClassesForRuleItem(GdlRule  * prule, int irit,
	Set<FsmMachineClass *> & setpfsmc)
{
	GdlRuleItem * prit = prule->InputItem(irit);
	Assert(prit);
	prit->GetMachineClasses(m_rgpfsmcAssignments, setpfsmc);
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleItem::GetMachineClasses(FsmMachineClass ** ppfsmcAssignments,
	Set<FsmMachineClass *> & setpfsmc)
{
	GdlGlyphClassDefn * pglfc = m_psymInput->GlyphClassDefnData();
	if (!pglfc)
		return;

	pglfc->GetMachineClasses(ppfsmcAssignments, setpfsmc);
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::GetMachineClasses(FsmMachineClass ** ppfsmcAssignments,
	Set<FsmMachineClass *> & setpfsmc)
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
		m_vpglfdMembers[iglfd]->GetMachineClasses(ppfsmcAssignments, setpfsmc);
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphDefn::GetMachineClasses(FsmMachineClass ** ppfsmcAssignments,
	Set<FsmMachineClass *> & setpfsmc)
{
	for (int iw = 0; iw < m_vwGlyphIDs.Size(); iw++)
	{
		utf16 wGlyphID = m_vwGlyphIDs[iw];
		FsmMachineClass * pfsmc = ppfsmcAssignments[wGlyphID];
		if (!setpfsmc.IsMember(pfsmc))
			setpfsmc.Insert(pfsmc);
	}
}

/*----------------------------------------------------------------------------------------------
	Return the item whose input index is the given value.
----------------------------------------------------------------------------------------------*/
GdlRuleItem * GdlRule::InputItem(int n)
{
	for (int i = 0; i < m_vprit.Size(); i++)
	{
		if (m_vprit[i]->m_nInputIndex == n)
			return m_vprit[i];
	}
	return NULL;
}


/*----------------------------------------------------------------------------------------------
	Return the number of input items in the rule, that is, the number of items minus the
	number of insertions.
----------------------------------------------------------------------------------------------*/
int GdlRule::NumberOfInputItems()
{
	int cRet = 0;
	for (int i = 0; i < m_vprit.Size(); i++)
	{
		if (m_vprit[i]->m_nInputIndex >= 0)
			cRet++;
	}
	return cRet;
}


/*----------------------------------------------------------------------------------------------
	Assign adjusted indices to the states in the FSM, causing them to be reordered in the
	following way:
	first:	transitional non-success states
	next:	transitional success states (a rule matches, but a longer one is possible)
	last:	non-transtional success states (a rule matches and no longer one is possible)
	(Non-transitional non-success states are an error!)
	Merged states have the same index as the state they are merged with.
----------------------------------------------------------------------------------------------*/
void GdlPass::ReorderFsmStates()
{
	m_vifsWorkToFinal.Resize(m_pfsm->RawNumberOfStates(), -1);
	Assert(m_vifsFinalToWork.Size() == 0);

	int ifsFinal = 0;

	//	First look for transitional non-success states.
	int ifsWork;
	for (ifsWork = 0; ifsWork < m_pfsm->RawNumberOfStates(); ifsWork++)
	{
		FsmState * pfstate = m_pfsm->RawStateAt(ifsWork);
		Assert(m_vifsWorkToFinal[ifsWork] == -1);
		if (!pfstate->HasBeenMerged())
		{
			if (pfstate->NumberOfRulesSucceeded() == 0)
			{
				Assert(!pfstate->AllCellsEmpty());
				m_vifsWorkToFinal[ifsWork] = ifsFinal;
				pfstate->SetFinalIndex(ifsFinal);
				m_vifsFinalToWork.Push(ifsWork);
				ifsFinal++;
			}
		}
	}

	//	Next look for transitional success states.
	for (ifsWork = 0; ifsWork < m_pfsm->RawNumberOfStates(); ifsWork++)
	{
		FsmState * pfstate = m_pfsm->RawStateAt(ifsWork);
		if (!pfstate->HasBeenMerged() && m_vifsWorkToFinal[ifsWork] == -1)
		{
			if (pfstate->NumberOfRulesSucceeded() > 0 && !pfstate->AllCellsEmpty())
			{
				m_vifsWorkToFinal[ifsWork] = ifsFinal;
				pfstate->SetFinalIndex(ifsFinal);
				m_vifsFinalToWork.Push(ifsWork);
				ifsFinal++;
			}
		}
	}

	//	Last look for non-transitional success states.
	for (ifsWork = 0; ifsWork < m_pfsm->RawNumberOfStates(); ifsWork++)
	{
		FsmState * pfstate = m_pfsm->RawStateAt(ifsWork);
		if (!pfstate->HasBeenMerged() && m_vifsWorkToFinal[ifsWork] == -1)
		{
			if (pfstate->NumberOfRulesSucceeded() > 0 && pfstate->AllCellsEmpty())
			{
				m_vifsWorkToFinal[ifsWork] = ifsFinal;
				pfstate->SetFinalIndex(ifsFinal);
				m_vifsFinalToWork.Push(ifsWork);
				ifsFinal++;
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Return the number of machine classes for this pass's FSM.
----------------------------------------------------------------------------------------------*/
int GdlPass::NumberOfFsmMachineClasses()
{
	return m_vpfsmc.Size();
}


/*----------------------------------------------------------------------------------------------
	Return the total number of glyph sub-ranges for all the columns in the FSM. Glyph sub-ranges
	consist of ranges of contiguous glyph IDs.
----------------------------------------------------------------------------------------------*/
int GdlPass::TotalNumGlyphSubRanges()
{
	int cRanges = 0;
	for (int i = 0; i < m_vpfsmc.Size(); i++)
		cRanges += m_vpfsmc[i]->NumberOfRanges();

	return cRanges;
}


/*----------------------------------------------------------------------------------------------
	Return the number of glyph sub-ranges for the machine class. Glyph sub-ranges consist of
	ranges of contiguous glyph IDs.
----------------------------------------------------------------------------------------------*/
int FsmMachineClass::NumberOfRanges()
{
	Assert(m_wGlyphs.Size() > 0);

	int cRanges = 1;
	for (int i = 1; i < m_wGlyphs.Size(); i++)
	{
		if (m_wGlyphs[i] > m_wGlyphs[i - 1] + 1)
			cRanges++;
	}
	return cRanges;
}


/*----------------------------------------------------------------------------------------------
	Return the total nubmer of states in the FSM.
----------------------------------------------------------------------------------------------*/
int GdlPass::NumStates()
{
	return m_vifsFinalToWork.Size();
}


/*----------------------------------------------------------------------------------------------
	Return the number of accepting (transitional success) states in the FSM.
----------------------------------------------------------------------------------------------*/
int GdlPass::NumAcceptingStates()
{
	int cRet = 0;
	int ifsLim = m_vifsFinalToWork.Size();
	for (int ifs = 0; ifs < ifsLim; ifs++)
	{
		FsmState * pfstate = m_pfsm->StateAt(m_vifsFinalToWork[ifs]);

		Assert(!pfstate->HasBeenMerged());

		if (pfstate->NumberOfRulesSucceeded() > 0 && !pfstate->AllCellsEmpty())
			cRet++;
	}
	return cRet;
}


/*----------------------------------------------------------------------------------------------
	Return the number of success states (where a rule is matched) in the FSM.
----------------------------------------------------------------------------------------------*/
int GdlPass::NumSuccessStates()
{
	int cRet = 0;
	int ifsLim = m_vifsFinalToWork.Size();
	for (int ifs = 0; ifs < ifsLim; ifs++)
	{
		FsmState * pfstate = m_pfsm->StateAt(m_vifsFinalToWork[ifs]);

		Assert(!pfstate->HasBeenMerged());

		if (pfstate->NumberOfRulesSucceeded() > 0)
			cRet++;
	}
	return cRet;
}


/*----------------------------------------------------------------------------------------------
	Return the number of transitional states in the FSM.
----------------------------------------------------------------------------------------------*/
int GdlPass::NumTransitionalStates()
{
	int cRet = 0;
	int ifsLim = m_vifsFinalToWork.Size();
	for (int ifs = 0; ifs < ifsLim; ifs++)
	{
		FsmState * pfstate = m_pfsm->StateAt(m_vifsFinalToWork[ifs]);

		Assert(!pfstate->HasBeenMerged());

		if (!pfstate->AllCellsEmpty())
			cRet++;
	}
	return cRet;
}


/*----------------------------------------------------------------------------------------------
	Return the number of final (non-transitional success) states in the FSM.
----------------------------------------------------------------------------------------------*/
int GdlPass::NumFinalStates()
{
	int cRet = 0;
	int ifsLim = m_vifsFinalToWork.Size();
	for (int ifs = 0; ifs < ifsLim; ifs++)
	{
		FsmState * pfstate = m_pfsm->StateAt(m_vifsFinalToWork[ifs]);

		Assert(!pfstate->HasBeenMerged());

		if (pfstate->NumberOfRulesSucceeded() > 0 && pfstate->AllCellsEmpty())
			cRet++;
	}
	return cRet;
}

/*----------------------------------------------------------------------------------------------
	Generate the start states corresponding to the number of items to skip when we are
	near the beginning of the input.
	Do this by tracing the phantom glyph through the table for as many steps as we need.
----------------------------------------------------------------------------------------------*/
void GdlPass::GenerateStartStates(GrcManager * pcman)
{
	utf16 wPhantomGlyph = pcman->PhantomGlyph();
	int ifsmcPhantom;
	if (!m_hmGlyphToColumn.Retrieve(wPhantomGlyph, &ifsmcPhantom))
		ifsmcPhantom = -1;

	//	The first state is always zero.
	int row = 0;
	for (int i = 0; i < (m_critMaxPreContext - m_critMinPreContext + 1); i++)
	{
		if (ifsmcPhantom == -1)
			m_vrowStartStates.Push(0);
		else
		{
			m_vrowStartStates.Push(row);

			FsmState * pfstate = m_pfsm->StateAt(m_vifsFinalToWork[row]);
			Assert(!pfstate->HasBeenMerged());

			int ifsmcValue = pfstate->CellValue(ifsmcPhantom);
			if (m_pfsm->RawStateAt(ifsmcValue)->HasBeenMerged())
				ifsmcValue = m_pfsm->RawStateAt(ifsmcValue)->MergedState()->WorkIndex();
			ifsmcValue = m_vifsWorkToFinal[ifsmcValue];

			row = ifsmcValue;
		}
	}
}

/***********************************************************************************************
	Debuggers
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Walk the FSM.
----------------------------------------------------------------------------------------------*/
void GdlPass::WalkFsmMachineClasses()
{
	FsmMachineClass * pfsmc;
	for (int i = 0; i < m_vpfsmc.Size(); i++)
	{
		pfsmc = m_vpfsmc[i];
	}

	for (utf16 w = 0; w < kMaxTotalGlyphs; w++)
	{
		pfsmc = m_rgpfsmcAssignments[w];
	}
}


/*----------------------------------------------------------------------------------------------
	Write a text version of the FSMs out to a file.
----------------------------------------------------------------------------------------------*/
void GrcManager::DebugFsm()
{
	std::ofstream strmOut;
	strmOut.open("dbg_fsm.txt");
	if (strmOut.fail())
	{
		g_errorList.AddError(3151, NULL,
			"Error in writing to file ",
			"dbg_fsm.txt");
		return;
	}

	strmOut << "FINITE STATE MACHINES\n\n";

	m_prndr->DebugFsm(this, strmOut);
	strmOut.close();
}

/*--------------------------------------------------------------------------------------------*/
void GdlRenderer::DebugFsm(GrcManager * pcman, std::ostream & strmOut)
{
	GdlRuleTable * prultbl;

	if ((prultbl = FindRuleTable("linebreak")) != NULL)
		prultbl->DebugFsm(pcman, strmOut);

	if ((prultbl = FindRuleTable("substitution")) != NULL)
		prultbl->DebugFsm(pcman, strmOut);

	if (m_iPassBidi > -1)
		strmOut << "\nPASS " << m_iPassBidi + 1 << ": bidi\n";

	if ((prultbl = FindRuleTable("justification")) != NULL)
		prultbl->DebugFsm(pcman, strmOut);

	if ((prultbl = FindRuleTable("positioning")) != NULL)
		prultbl->DebugFsm(pcman, strmOut);
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::DebugFsm(GrcManager * pcman, std::ostream & strmOut)
{
	strmOut << "\nTABLE: " << m_psymName->FullName().Chars() << "\n";
	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->DebugFsm(pcman, strmOut);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::DebugFsm(GrcManager * pcman, std::ostream & strmOut)
{
	int nPassNum = PassDebuggerNumber();

	if (m_nGlobalID == -1)
	{
		strmOut << "\nPASS: " << nPassNum << "--no FSM\n";
		return;
	}

	//	Output glyph -> column assigments
	strmOut << "\nPASS: " << nPassNum << "\n\nGlyph ID => Column:\n";

	int wFirst = 0;
	int wLast = 0;
	int ifsmcColCurr = -1;
	for (int w = 0; w < kMaxTotalGlyphs + 1; w++)
	{
		utf16 wTmp = w;
		char rgch[20];
		int ifsmcColumn;

		if (w == kMaxTotalGlyphs)
			ifsmcColumn = -1;
		else if (!m_hmGlyphToColumn.Retrieve(wTmp, &ifsmcColumn))
			ifsmcColumn = -1;
		if (ifsmcColCurr != ifsmcColumn)
		{
			//	Output previous group of assignments.
			if (ifsmcColCurr != -1)
			{
				wLast = w - 1;
				itoa(wFirst, rgch, 16);
				strmOut << "  0x";
				if (wFirst <= 0x0fff) strmOut << "0";
				if (wFirst <= 0x00ff) strmOut << "0";
				if (wFirst <= 0x000f) strmOut << "0";
				strmOut << rgch;
				if (wFirst < wLast)
				{
					itoa(wLast, rgch, 16);
					strmOut << "..0x";
					if (wLast <= 0x0fff) strmOut << "0";
					if (wLast <= 0x00ff) strmOut << "0";
					if (wLast <= 0x000f) strmOut << "0";
					strmOut << rgch;
				}
				else
					strmOut << "        ";

//				if (w < 0x0100)
//					strmOut << " '" << (char)w << "'";
//				else
//					strmOut << "    ";

				strmOut << " => " << ifsmcColCurr << "\n";
			}
			//	Start a new group.
			ifsmcColCurr = ifsmcColumn;
			wFirst = w;
		}
	}

	//	Output table with working indices.
	/////DebugFsmTable(pcman, strmOut, true);

	strmOut << "\n";

	//	Output table with final indices.
	DebugFsmTable(pcman, strmOut, false);

	//	Output rules.
	for (int irule = 0; irule < m_vprule.Size(); irule++)
	{
		strmOut << "RULE " << nPassNum << "." << irule << ", ";
		m_vprule[irule]->LineAndFile().WriteToStream(strmOut, true);
		strmOut << ":  ";
		m_vprule[irule]->RulePrettyPrint(pcman, strmOut);
		strmOut << "\n\n";
	}
	strmOut << "\n";
}

void GdlPass::DebugFsmTable(GrcManager * pcman, std::ostream & strmOut, bool fWorking)
{
	int cfsmc = m_pfsm->NumberOfColumns();
	if (fWorking)
		strmOut << "\nWorking Table:          ";
	else
		strmOut << "\nFinal Table:            ";
	int ifsmc;
	for (ifsmc = 0; ifsmc < cfsmc; ifsmc++)	// column headers
		OutputNumber(strmOut, ifsmc, 6);
	strmOut << "\n                          ";
	for (ifsmc = 0; ifsmc < cfsmc; ifsmc++)
		strmOut << "- - - ";


	int ifsLim = (fWorking)? m_pfsm->RawNumberOfStates() : m_vifsFinalToWork.Size();
	for (int ifs = 0; ifs < ifsLim; ifs++)
	{
		FsmState * pfstate = (fWorking) ?
			m_pfsm->RawStateAt(ifs) :
			m_pfsm->StateAt(m_vifsFinalToWork[ifs]);

		strmOut << "\n" << ifs << ": " << pfstate->SlotsMatched() << "\n";

		if (pfstate->HasBeenMerged())
		{
			Assert(fWorking);
			strmOut << "  => state #" << pfstate->MergedState()->WorkIndex();
		}
		else
		{
			strmOut << "                        ";

			for (int ifsmc = 0; ifsmc < cfsmc; ifsmc++)
			{
				strmOut << " ";
				int ifsmcValue = pfstate->CellValue(ifsmc);
				if (!fWorking)
				{
					if (m_pfsm->RawStateAt(ifsmcValue)->HasBeenMerged())
						ifsmcValue = m_pfsm->RawStateAt(ifsmcValue)->MergedState()->WorkIndex();
					ifsmcValue = m_vifsWorkToFinal[ifsmcValue];
				}
				OutputNumber(strmOut, ifsmcValue, 5);
			}
		}

		pfstate->DebugFsmState(strmOut, ifs);
	}

	strmOut << "\n                          ";
	for (ifsmc = 0; ifsmc < cfsmc; ifsmc++)
		strmOut << "- - - ";
	strmOut << "\n\n";
}

void FsmState::DebugFsmState(std::ostream & strmOut, int ifs)
{
	strmOut << "\n   Matched=";
	Set<int>::iterator it;
	for (it = m_setiruleMatched.Begin();
		it != m_setiruleMatched.End();
		++it)
	{
		strmOut << *it << ",";
	}
	if (m_setiruleMatched.Size() == 0)
		strmOut << "none";

	strmOut << "\n   Success=";
	for (it = m_setiruleSuccess.Begin();
		it != m_setiruleSuccess.End();
		++it)
	{
		strmOut << *it << ",";
	}
	if (m_setiruleSuccess.Size() == 0)
		strmOut << "none";
	strmOut << "\n";
}


void OutputNumber(std::ostream& strmOut, int nValue, int nSpaces)
{
	if (nSpaces < 1)
		return;

	Assert(nSpaces < 8);

	int cSpaces = 0;
	if (nValue >= 100000000)
		cSpaces--;
	if (nValue < 10000000)
		cSpaces++;
	if (nValue < 1000000)
		cSpaces++;
	if (nValue < 100000)
		cSpaces++;
	if (nValue < 10000)
		cSpaces++;
	if (nValue < 1000)
		cSpaces++;
	if (nValue < 100)
		cSpaces++;
	if (nValue < 10)
		cSpaces++;

	cSpaces = cSpaces - (8 - nSpaces);

	if (cSpaces < 0)
	{
		strmOut << " ";
		for (int i = 1; i < nSpaces; i++) strmOut << "*";
		return;
	}

	for (int i = 0; i < cSpaces; i++) strmOut << " ";
	strmOut << nValue;
}
