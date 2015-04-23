/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: PostParser.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Methods to implement the post-parser, which does adjustments independent of the font file.
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

bool GrcManager::PostParse()
{
	//	Add the system-defined "lang" feature whose values are 4-byte language ID codes.
	//	This feature always has an ID = 1 (kfidStdLang).
	Symbol psymFeat = SymbolTable()->AddFeatureSymbol(GrcStructName("lang"),
		GrpLineAndFile(0, 0, ""));
	GdlFeatureDefn * pfeat = psymFeat->FeatureDefnData();
	Assert(pfeat);
	pfeat->SetName("lang");
	pfeat->MarkAsLanguageFeature();
	m_prndr->AddFeature(pfeat);

	g_cman.ProcessMasterTables();

	if (!m_prndr->ReplaceAliases())
		return false;

	if (!m_prndr->HandleOptionalItems())
		return false;

	if (!m_prndr->CheckSelectors())
		return false;

	return true;
}

/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Convert slot aliases to (1-based) indices.
----------------------------------------------------------------------------------------------*/
bool GdlRenderer::ReplaceAliases()
{
	for (int iprultbl = 0; iprultbl < m_vprultbl.Size(); iprultbl++)
	{
		m_vprultbl[iprultbl]->ReplaceAliases();
	}

	return true;
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::ReplaceAliases()
{
	int ippass;
	for (ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		if (m_vppass[ippass] == NULL)
			m_vppass[ippass] = new GdlPass(ippass, 1, 0);	// bogus
	}

	for (ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->ReplaceAliases();
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::ReplaceAliases()
{
	for (int iprule = 0; iprule < m_vprule.Size(); iprule++)
	{
		m_vprule[iprule]->ReplaceAliases();
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRule::ReplaceAliases()
{
	//	Look for slots with more than one name, or names assigned to more than one slot.
	int ipalias;
	for (ipalias = 0; ipalias < m_vpalias.Size() - 1; ipalias++)
	{
		for (int ipalias2 = ipalias + 1; ipalias2 < m_vpalias.Size(); ipalias2++)
		{
			if (m_vpalias[ipalias]->m_srIndex == m_vpalias[ipalias2]->m_srIndex)
			{
				if (m_vpalias[ipalias]->m_staName != m_vpalias[ipalias2]->m_staName)
				{
					char rgch[20];
					itoa(m_vpalias[ipalias]->m_srIndex, rgch, 10);
					g_errorList.AddWarning(1510, this,
						"Item ",
						StrAnsi(rgch),
						" was assigned more than one slot alias");
				}
			}
			else if (m_vpalias[ipalias]->m_staName ==
						m_vpalias[ipalias2]->m_staName)
			{
				g_errorList.AddError(1173, this,
					"Slot alias '",
					m_vpalias[ipalias]->m_staName,
					"' was assigned to more than one item");
			}
		}
	}

	//	Replace the aliases with the indices in any attribute-setters or constraints.
	for (int irit = 0; irit < m_vprit.Size(); irit++)
	{
		m_vprit[irit]->ReplaceAliases(this);
	}

	for (ipalias = 0; ipalias < m_vpalias.Size(); ipalias++)
		delete m_vpalias[ipalias];

	m_vpalias.Clear();
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleItem::ReplaceAliases(GdlRule * prule)
{
	if (m_pexpConstraint)
		m_pexpConstraint->ReplaceAliases(prule);
}

/*--------------------------------------------------------------------------------------------*/
void GdlLineBreakItem::ReplaceAliases(GdlRule * prule)
{
	//	method on superclass: check constraints.
	GdlRuleItem::ReplaceAliases(prule);
}

/*--------------------------------------------------------------------------------------------*/
void GdlSetAttrItem::ReplaceAliases(GdlRule * prule)
{
	for (int ipavs = 0; ipavs < m_vpavs.Size(); ipavs++)
	{
		m_vpavs[ipavs]->ReplaceAliases(prule);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlSubstitutionItem::ReplaceAliases(GdlRule * prule)
{
	GdlSetAttrItem::ReplaceAliases(prule);

	if (m_pexpSelector)
		m_pexpSelector->ReplaceAliases(prule);

	for (int ipexp = 0; ipexp < m_vpexpAssocs.Size(); ipexp++)
	{
		m_vpexpAssocs[ipexp]->ReplaceAliases(prule);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlAttrValueSpec::ReplaceAliases(GdlRule * prule)
{
	m_pexpValue->ReplaceAliases(prule);
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Generate the various versions of the rules depending on the	optional items.
----------------------------------------------------------------------------------------------*/
bool GdlRenderer::HandleOptionalItems()
{
	for (int iprultbl = 0; iprultbl < m_vprultbl.Size(); iprultbl++)
	{
		m_vprultbl[iprultbl]->HandleOptionalItems();
	}

	return true;
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::HandleOptionalItems()
{
	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->HandleOptionalItems();
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::HandleOptionalItems()
{
	Vector<GdlRule*> vpruleNewList;

	for (int irule = 0; irule < m_vprule.Size(); ++irule)
	{
		GdlRule* prule = m_vprule[irule];
		if (prule->HasNoItems())
		{
			Assert(false);
			delete prule;
		}

		else if (prule->HandleOptionalItems(vpruleNewList))
			delete prule;
	}

	m_vprule.Clear();
	vpruleNewList.CopyTo(m_vprule);
}


/*----------------------------------------------------------------------------------------------
	If there are optional items in this rule, replace this rule with versions corresponding
	to the combinations of the presence and absence of the optional items. Return true if
	this rule has been replaced and should be deleted, false if no changes were necessary
	(ie, there were no optional items).
	Argument:
		vpruleNewList	- new list of rules under construction
----------------------------------------------------------------------------------------------*/
bool GdlRule::HandleOptionalItems(Vector<GdlRule*> & vpruleNewList)
{
	Assert(m_viritOptRangeStart.Size() == m_viritOptRangeEnd.Size());

	if (m_viritOptRangeStart.Size() == 0)
	{
		vpruleNewList.Push(this);
		return false;	// don't delete this rule
	}

	if (!AdjustOptRanges())
	{
		return false;	// retain the rule as is for further processing
	}

	Vector<bool> vfOmit;
	for (int irange = 0; irange < m_viritOptRangeStart.Size(); ++irange)
		vfOmit.Push(false);
	Assert(vfOmit.Size() == m_viritOptRangeStart.Size());

	GenerateOptRanges(vpruleNewList, vfOmit, 0);

	return true;	// delete this rule; it has been replaced
}


/*----------------------------------------------------------------------------------------------
	Adjust the list of optional ranges in this rule for ease of processing,
	and do error checking. Return false if we found an error.
----------------------------------------------------------------------------------------------*/
bool GdlRule::AdjustOptRanges()
{
	if (m_viritOptRangeStart.Size() > 1)
	{
		//	Sort the ranges primarily by start of range, then with longest ranges first.
		int i1;
		for (i1 = 0 ; i1 < m_viritOptRangeStart.Size() - 1; ++i1)
		{
			for (int i2 = i1 + 1; i2 < m_viritOptRangeStart.Size(); ++i2)
			{
				int start1 = m_viritOptRangeStart[i1];
				int start2 = m_viritOptRangeStart[i2];
				int len1 = m_viritOptRangeEnd[i1] - start1;
				int len2 = m_viritOptRangeEnd[i2] - start2;
				if ((start1 > start2) ||
						(start1 == start2 && len1 < len2))
				{
					//	swap
					int t = m_viritOptRangeStart[i1];
					m_viritOptRangeStart[i1] = m_viritOptRangeStart[i2];
					m_viritOptRangeStart[i2] = t;

					t = m_viritOptRangeEnd[i1];
					m_viritOptRangeEnd[i1] = m_viritOptRangeEnd[i2];
					m_viritOptRangeEnd[i2] = t;
				}
			}
		}

		//	Remove duplicate ranges.
		for (int i = 0; i < m_viritOptRangeStart.Size() - 1; ++i)
		{
			if (m_viritOptRangeStart[i] == m_viritOptRangeStart[i + 1] &&
					m_viritOptRangeEnd[i] == m_viritOptRangeEnd[i + 1])
			{
				m_viritOptRangeStart.Delete(i+1);
				m_viritOptRangeEnd.Delete(i+1);
			}
		}

		//	Check for overlapping ranges.
		for (i1 = 0 ; i1 < m_viritOptRangeStart.Size() - 1; ++i1)
		{
			for (int i2 = i1 + 1; i2 < m_viritOptRangeStart.Size(); ++i2)
			{
				if (m_viritOptRangeStart[i2] <= m_viritOptRangeEnd[i1] &&
						m_viritOptRangeEnd[i2] > m_viritOptRangeEnd[i1])
					//	Overlapping ranges, eg, 3-6 and 4-7
				{
					g_errorList.AddError(1174, this,
						"Invalid optional ranges.");
					return false;
				}
			}
		}
	}

	//	Check for an error of an optional range consisting only of inserted items.
	for (int iirit = 0; iirit < m_viritOptRangeStart.Size(); ++iirit)
	{
		bool fFoundNonInsertion = false;
		for (int irit = m_viritOptRangeStart[iirit]; irit <= m_viritOptRangeEnd[iirit]; irit++)
		{
			if (!m_vprit[irit]->m_psymInput->FitsSymbolType(ksymtSpecialUnderscore))
				fFoundNonInsertion = true;
		}
		if (!fFoundNonInsertion)
		{
			if (m_viritOptRangeStart[iirit] == m_viritOptRangeEnd[iirit])
				g_errorList.AddError(1175, this, "Optional item is an insertion.");
			else
				g_errorList.AddError(1176, this, "Optional range includes only inserted items.");
			return false;
		}
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Generate versions of this rule for all the various combinations of optional items.
	Add the rules to the new list of rules.
	We call this function recursively, working our way down through the list of ranges
	and toggling the omit flags for each.

	For instance, if our rule is

		A [ B? [ C D? ]? ]? E

	the (0-based) omit ranges are:
		1 - 3
		1 - 1
		2 - 3
		3 - 3
	and we generate the rules in the following order:
		A  B  C  D  E		(nothing omitted)
		A  B  C  E			(3-3 omitted)
		A  B  E				(2-3 omitted)
		A  C  D  E			(1-1 omitted)
		A  C  E				(1-1, 3-3 omitted)
		A  E				(1-1, 2-3 omitted)

	In the case that all the items in the rule happen to be optional, we avoid
	generating a version with no items.

	Arguments:
		vpruleNewList	- list of rules being generated
		vfOmitRange		- flags indicating which ranges should be omitted
		irangeCurr		- current range in list being processed (toggled)
----------------------------------------------------------------------------------------------*/
void GdlRule::GenerateOptRanges(Vector<GdlRule*> & vpruleNewList, Vector<bool> & vfOmitRange,
	int irangeCurr)
{
	if (irangeCurr >= vfOmitRange.Size())
		//	We've got a complete set of omit flags for each of the optional ranges--
		//	generate the corresponding rule.
		GenerateOneRuleVersion(vpruleNewList, vfOmitRange);

	else
	{
		int irangeSubsuming = PrevRangeSubsumes(irangeCurr);
		if (irangeSubsuming > -1 && vfOmitRange[irangeSubsuming])
		{}	//	This range is subsumed by a another range that is already being omitted.
		else
			//	Generate version(s) of this rule containing this range.
			GenerateOptRanges(vpruleNewList, vfOmitRange, irangeCurr + 1);

		//	Now generate version(s) of this rule omitting this range.
		vfOmitRange[irangeCurr] = true;
		GenerateOptRanges(vpruleNewList, vfOmitRange, irangeCurr + 1);
		vfOmitRange[irangeCurr] = false;
	}
}


/*----------------------------------------------------------------------------------------------
	Generate a version of this rule for the given combination of optional items.
	Add it to the new list of rules.
----------------------------------------------------------------------------------------------*/
void GdlRule::GenerateOneRuleVersion(Vector<GdlRule*> & vpruleNewList,
	Vector<bool> & vfOmitRange)
{
	//	Make a vector indicating items to omit, and a list of mappings of old->new
	//	indices. For instance, if we have 5 items and we are omitting items 2 & 3, we'll
	//	have vfOmit = [F T T F F] and viNewSlots [1 ? ? 2 3].
	Vector<bool> vfOmit;
	Vector<int> viNewSlots;
	int irit;
	for (irit = 0; irit < m_vprit.Size(); ++irit)
	{
		vfOmit.Push(false);
		viNewSlots.Push(irit + 1);	// +1: currently slot refs are 1-based
	}
	for (int irange = 0; irange < vfOmitRange.Size(); ++irange)
	{
		if (vfOmitRange[irange])
		{
			for (int irit = m_viritOptRangeStart[irange];
				irit <= m_viritOptRangeEnd[irange];
				++irit)
			{
				if (vfOmit[irit] == false)
				{
					vfOmit[irit] = true;	// so we won't consider it again if there is
											// another range that includes it
					for (int irit2 = irit + 1; irit2 < m_vprit.Size(); ++irit2)
						viNewSlots[irit2] -= 1;
				}
			}
		}
	}

	//	Calculate the new scan advance.
	int nNewScan = m_nScanAdvance;
	if (m_nScanAdvance > -1)
	{
		while (nNewScan < vfOmit.Size() && vfOmit[nNewScan])
			nNewScan++;
		if (nNewScan >= vfOmit.Size())
			nNewScan = kMaxSlotsPerRule + 1; // put it after the last item
		else
			nNewScan = viNewSlots[nNewScan] - 1;	// 0-based
	}

	//	Check to make sure there is a least one item to be included.
	bool fNonEmpty = false;
	for (irit = 0; irit < m_vprit.Size(); ++irit)
	{
		if (!vfOmit[irit])
		{
			fNonEmpty = true;
			break;
		}
	}
	if (!fNonEmpty)
	{
		g_errorList.AddWarning(1511, this,
			"All items are optional");
		return;
	}

	//	Generate a version of the rule with the appropriate optional items omitted.
	//	First, copy the items to include.
	GdlRule * pruleNew = new GdlRule();
	pruleNew->CopyLineAndFile(*this);
	int critNew = 0;
	for (irit = 0; irit < m_vprit.Size(); ++irit)
	{
		if (!vfOmit[irit])
		{
			GdlRuleItem * pritNew = m_vprit[irit]->Clone();
			pritNew->m_iritContextPos = critNew++;
			pruleNew->m_vprit.Push(pritNew);
		}
	}
	if (nNewScan > kMaxSlotsPerRule)
		nNewScan = pruleNew->NumberOfSlots();
	pruleNew->SetScanAdvance(nNewScan);

	//	Copy all the constraints and the list of aliases.
	int iexp;
	for (iexp = 0; iexp < m_vpexpConstraints.Size(); ++iexp)
		pruleNew->m_vpexpConstraints.Push(m_vpexpConstraints[iexp]->Clone());
	for (int ialias = 0; ialias < m_vpalias.Size(); ++ialias)
		pruleNew->m_vpalias.Push(new GdlAlias(*m_vpalias[ialias]));

	bool fError = false;
	//	Adjust all the slot references based on what slots were omitted.
	for (irit = 0; irit < pruleNew->m_vprit.Size(); ++irit)
	{
		if (!pruleNew->m_vprit[irit]->AdjustSlotRefs(vfOmit, viNewSlots, this))
			// error (message was generated by AdjustSlotRefs)
			fError = true;
	}

	for (iexp = 0; iexp < pruleNew->m_vpexpConstraints.Size(); ++iexp)
	{
		if (!pruleNew->m_vpexpConstraints[iexp]->AdjustSlotRefs(vfOmit, viNewSlots, this))
			// error (message was generated by AdjustSlotRefs)
			fError = true;
	}

	if (fError)
	{
		delete pruleNew;
		return;
	}

	//	Delete any irrelevant slot-aliases from the mapping; shouln't be necessary because
	//	we've gotten rid of the aliases by this point.
	Assert(m_vpalias.Size() == 0);
//	for (ialias = pruleNew->m_vpalias.Size() - 1; ialias >= 0; --ialias)
//	{
//		if (!pruleNew->m_vpalias[ialias]->AdjustSlotRefs(vfOmit, viNewSlots))
//			//	This entry represents a slot that was omitted.
//			pruleNew->m_vpalias.Delete(ialias);
//	}

	vpruleNewList.Push(pruleNew);
}


/*----------------------------------------------------------------------------------------------
	If there is a previous range in the list of ranges that subsumes the current range,
	return its index, or -1 if there is none.
	Return the most immediate, because that is the one we want to test for being omitted
	or not.
----------------------------------------------------------------------------------------------*/
int GdlRule::PrevRangeSubsumes(int irangeCurr)
{
	//	Loop backwards, in order to return the most immediate.
	for (int irange = irangeCurr; --irange >= 0; )
	{
		if (m_viritOptRangeStart[irange] <= m_viritOptRangeStart[irangeCurr] &&
				m_viritOptRangeEnd[irangeCurr] <= m_viritOptRangeEnd[irange])
			return irange;
	}
	return -1;
}


/*----------------------------------------------------------------------------------------------
	Adjust the slot references based on what optional slots were omitted.
	Return false if there was a reference to an omitted item; lower-level function is
	responsible to create an error message.
	Arguments:
		vfOmit			- for each item, was it omitted?
		vnNewIndices	- for the items that were not omitted, the adjusted index
		prule			- the 'owning' rule
----------------------------------------------------------------------------------------------*/
bool GdlRuleItem::AdjustSlotRefs(Vector<bool> & vfOmit, Vector<int> & vnNewIndices,
	GdlRule * prule)
{
	if (m_pexpConstraint)
		return m_pexpConstraint->AdjustSlotRefs(vfOmit, vnNewIndices, prule);
	else
		return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlLineBreakItem::AdjustSlotRefs(Vector<bool> & vfOmit, Vector<int> & vnNewIndices,
	GdlRule * prule)
{
	return GdlRuleItem::AdjustSlotRefs(vfOmit, vnNewIndices, prule);
}

/*--------------------------------------------------------------------------------------------*/
bool GdlSetAttrItem::AdjustSlotRefs(Vector<bool> & vfOmit, Vector<int> & vnNewIndices,
	GdlRule * prule)
{
	if (!GdlRuleItem::AdjustSlotRefs(vfOmit, vnNewIndices, prule))
		return false;

	for (int i = 0; i < m_vpavs.Size(); ++i)
	{
		if (!m_vpavs[i]->AdjustSlotRefs(vfOmit, vnNewIndices, prule))
			return false;
	}
	return true;
}

/*--------------------------------------------------------------------------------------------*/
bool GdlSubstitutionItem::AdjustSlotRefs(Vector<bool> & vfOmit, Vector<int> & vnNewIndices,
	GdlRule * prule)
{
	if (!GdlSetAttrItem::AdjustSlotRefs(vfOmit, vnNewIndices, prule))
		return false;

	for (int i = 0; i < m_vpexpAssocs.Size(); ++i)
	{
		if (!m_vpexpAssocs[i]->AdjustSlotRefs(vfOmit, vnNewIndices, prule))
			return false;
	}

	if (m_pexpSelector)
	{
		if (!m_pexpSelector->AdjustSlotRefs(vfOmit, vnNewIndices, prule))
			return false;
	}

	return true;
}
/*--------------------------------------------------------------------------------------------*/
bool GdlAttrValueSpec::AdjustSlotRefs(Vector<bool> & vfOmit, Vector<int> & vnNewIndices,
	GdlRule * prule)
{
	if (m_pexpValue)
		return m_pexpValue->AdjustSlotRefs(vfOmit, vnNewIndices, prule);
	else
		return true;
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	For GdlSubstitutionItems, if there is a selector (eg, @2 or clsX$2), make sure it is valid.
----------------------------------------------------------------------------------------------*/
bool GdlRenderer::CheckSelectors()
{
	for (int iprultbl = 0; iprultbl < m_vprultbl.Size(); iprultbl++)
	{
		m_vprultbl[iprultbl]->CheckSelectors();
	}

	return true;
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::CheckSelectors()
{
	for (int ippass = 0; ippass < m_vppass.Size(); ippass++)
	{
		m_vppass[ippass]->CheckSelectors();
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlPass::CheckSelectors()
{
	for (int iprule = 0; iprule < m_vprule.Size(); iprule++)
	{
		m_vprule[iprule]->CheckSelectors();
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRule::CheckSelectors()
{
	for (int irit = 0; irit < m_vprit.Size(); irit++)
	{
		m_vprit[irit]->CheckSelectors(this, irit, m_vprit.Size());
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlRuleItem::CheckSelectors(GdlRule * prule, int irit, int crit)
{
	//	No selector--do nothing.
}

/*--------------------------------------------------------------------------------------------*/
void GdlSubstitutionItem::CheckSelectors(GdlRule * prule, int irit, int crit)
{
	if (m_psymOutput->FitsSymbolType(ksymtClass) ||
		m_psymOutput->FitsSymbolType(ksymtSpecialAt))
	{
		if (m_pexpSelector)
		{

			if (m_pexpSelector->SlotNumber() <= 0)
			{
				g_errorList.AddError(1177, this,
					"Item ",
					PosString(),
					": invalid selector following '@'");
				m_pritSelInput = NULL;
				return;
			}
			else if (m_pexpSelector->SlotNumber() > crit)
			{
				g_errorList.AddError(1178, this,
					"Item ",
					PosString(),
					": selector out of range");
				m_pritSelInput = NULL;
				return;
			}
			else
			{
				m_pritSelInput = prule->Item(m_pexpSelector->SlotNumber() - 1);
			}

			if (!m_pritSelInput->m_psymInput->FitsSymbolType(ksymtClass))
			{
				if (m_pritSelInput->m_psymInput->FitsSymbolType(ksymtSpecialLb))
				{	// Error handled elsewhere
				}
				else
					g_errorList.AddError(1179, this,
						"Item ",
						PosString(),
						": input not specified for slot indicated by selector");
			}
		}
		else
		{
			m_pritSelInput = this;
		}

	}
	else
	{
		if (m_pexpSelector)
		{
			g_errorList.AddError(1180, this,
				"Item ",
				PosString(),
				": selectors can only be used with class names and '@'");
			delete m_pexpSelector;
			m_pexpSelector = NULL;
		}

		m_pritSelInput = NULL;
	}

}



/**********************************************************************************************/
