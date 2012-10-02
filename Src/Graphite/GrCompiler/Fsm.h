/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Fsm.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Defines the class that are used to generate the finite state machines.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef FSM_INCLUDED
#define FSM_INCLUDED


typedef Set<GdlGlyphClassDefn *> SourceClassSet;		// hungarian: scs
typedef Vector<FsmMachineClass *> * MachineClassList;	// hungarian: mcl

void OutputNumber(std::ostream& strmOut, int nValue, int nSpaces);

/*----------------------------------------------------------------------------------------------
Class: FsmMachineClass
Description: A machine class corresponds to one column in the FSM matrix.
Hungarian: fsmc
----------------------------------------------------------------------------------------------*/
class FsmMachineClass
{
	friend class FsmTable;
	friend class GdlPass;

public:
	FsmMachineClass(int nPass)
	{
		m_nPass = nPass;
	}

	int Column()
	{
		return m_ifsmcColumn;
	}

	void SetColumn(int ifsmc)
	{
		m_ifsmcColumn = ifsmc;
	}

	void SetSourceClasses(SourceClassSet * pscs)
	{
		for (SourceClassSet::iterator itscs = pscs->Begin();
			itscs != pscs->End();
			++itscs)
		{
			m_scs.Insert(*itscs, false);
		}
	}

	void AddGlyph(utf16 w);

	int Key(int ipass);

	bool MatchesSources(SourceClassSet *);
	bool MatchesOneSource(GdlGlyphClassDefn *);

	//	Output:
	int NumberOfRanges();
	utf16 OutputRange(utf16 wGlyphID, GrcBinaryStream * pbstrm);

protected:
	int m_nPass;

	SourceClassSet m_scs;	// source-class-set corresponding to this machine-class
	Vector<utf16> m_wGlyphs;
	int m_ifsmcColumn;	// column this machine class is assigned to
	StrAnsi staDebug;

};

/*----------------------------------------------------------------------------------------------
Class: FsmState
Description: A row in the FSM table.
Hungarian: fstate
----------------------------------------------------------------------------------------------*/
class FsmState
{
	friend class FsmTable;
	friend class GdlPass;

public:
	FsmState(int ccol, int critSlots, int ifsIndex)
	{
		Init(ccol, critSlots, ifsIndex);
	}

	FsmState()
	{
		Assert(false);
		m_cfsmc = 0;
		m_prgiCells = NULL;
		m_pfstateMerged = NULL;
	}

	void Init(int cfsmc, int critSlots, int ifsIndex)
	{
		m_cfsmc = cfsmc;
		m_prgiCells = new int[cfsmc];
		memset(m_prgiCells, 0, cfsmc * sizeof(int));

		m_critSlotsMatched = critSlots;
		m_ifsWorkIndex = ifsIndex;
		m_ifsFinalIndex = -1;
		m_pfstateMerged = NULL;
	}

	~FsmState()
	{
		if (m_prgiCells)
			delete[] m_prgiCells;
	}

	int SlotsMatched()
	{
		return m_critSlotsMatched;
	}

	int WorkIndex()
	{
		return m_ifsWorkIndex;
	}

	int CellValue(int ifsmc)
	{
		Assert(m_prgiCells);
		return m_prgiCells[ifsmc];
	}

	void SetCellValue(int ifsmc, int ifsValue)
	{
		Assert(m_prgiCells);
		m_prgiCells[ifsmc] = ifsValue;
	}

	int NumberOfRulesMatched()
	{
		return m_setiruleMatched.Size();
	}

	bool RuleMatched(int irule)
	{
		return m_setiruleMatched.IsMember(irule);
	}

	void AddRuleToMatchedList(int irule)
	{
		if (!m_setiruleMatched.IsMember(irule))
			m_setiruleMatched.Insert(irule);
	}

	int NumberOfRulesSucceeded()
	{
		return m_setiruleSuccess.Size();
	}

	bool RuleSucceeded(int irule)
	{
		return m_setiruleSuccess.IsMember(irule);
	}

	void AddRuleToSuccessList(int irule)
	{
		if (!m_setiruleSuccess.IsMember(irule))
			m_setiruleSuccess.Insert(irule);
	}

	bool StatesMatch(FsmState * pfState);

	bool HasBeenMerged()
	{
		return (m_pfstateMerged != NULL);
	}

	FsmState * MergedState()
	{
		return m_pfstateMerged;
	}

	void SetMergedState(FsmState * pfsmc)
	{
		m_pfstateMerged = pfsmc;
	}

	void SetFinalIndex(int n)
	{
		m_ifsFinalIndex = n;
	}

	bool AllCellsEmpty()
	{
		for (int ifsmc = 0; ifsmc < m_cfsmc; ifsmc++)
		{
			if (m_prgiCells[ifsmc] != 0)
				return false;
		}
		return true;
	}

protected:
	int m_critSlotsMatched;	// number of slots matched at this state
	int m_cfsmc;			// number of machine classes, ie columns
	int m_ifsWorkIndex;		// working index of this state, as the FSM was originally
							// generated (currently just used for debugging)
	int m_ifsFinalIndex;	// adjusted index for final output form of FSM (-1 for merged
							// states)

	Set<int> m_setiruleMatched;	// indices of rules matched by this state
	Set<int> m_setiruleSuccess;	// indices of rules for which this is a success state

	int * m_prgiCells;	// the cells of the state, holding the index of the state
						// to transition to


	FsmState * m_pfstateMerged;		// pointer to an earlier state that is identical to this
									// one (if any), so this state is considered merged with
									// that one


public:
	//	Debuggers:
	void FsmState::DebugFsmState(std::ostream & strmOut, int ifs);
};



/*----------------------------------------------------------------------------------------------
Class: FsmTable
Description: A single finite state machine.
Hungarian: fsm
----------------------------------------------------------------------------------------------*/
class FsmTable
{
	friend class GdlPass;

public:
	FsmTable(int nPass, int cfsmc)
	{
		m_nPass = nPass;
		m_cfsmc = cfsmc;
	}

	~FsmTable()
	{
		for (int ipfstate = 0; ipfstate < m_vpfstate.Size(); ipfstate++)
			delete m_vpfstate[ipfstate];
	}

	void AddState(int critSlotsMatched)
	{
		FsmState * pfstateNew = new FsmState(m_cfsmc, critSlotsMatched, m_vpfstate.Size());
		m_vpfstate.Push(pfstateNew);
	}

	FsmState * StateAt(int ifs)
	{
		FsmState * pfstateRet = RawStateAt(ifs);
		if (pfstateRet->HasBeenMerged())
			return pfstateRet->MergedState();

		return pfstateRet;
	}

	FsmState * RawStateAt(int ifs)
	{
		return m_vpfstate[ifs];
	}

	int RawNumberOfStates()
	{
		return m_vpfstate.Size();
	}

	int NumberOfColumns()
	{
		return m_cfsmc;
	}

protected:
	int m_nPass;	// the pass this table pertains to
	int m_cfsmc;	// the number of machine classes, ie columns, in the table
	Vector<FsmState *> m_vpfstate;	// the rows
};


#endif // !FSM_INCLUDED
