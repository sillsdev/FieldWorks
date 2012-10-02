/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GdlTablePass.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implements the classes corresponding to the tables of rules and their passes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef TABLEPASS_INCLUDED
#define TABLEPASS_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: GdlPass
Description: corresponds to a pass within the substitution, linebreak, or positioning table.
Hungarian: pass
----------------------------------------------------------------------------------------------*/

class GdlPass : public GdlObject
{
public:
	//	Constructors & destructors:
	GdlPass(int nNum, int nMaxRuleLoop, int nMaxBackup)
		:	m_nNumber(nNum),
			m_nMaxRuleLoop(nMaxRuleLoop),
			m_nMaxBackup(nMaxBackup),
			m_fBidi(false),
			m_nGlobalID(-1),
			m_nPreBidiPass(0),
			m_pfsm(NULL)
	{
	}

	~GdlPass();
	void ClearFsmWorkSpace();

	//	Getters:
	int Number()					{ return m_nNumber; }

	bool HasLineBreaks()			{ return m_fLB; }
	bool HasCrossLineContext()		{ return m_fCrossLB; }
	int MaxPreLBSlots()				{ return m_critPreLB; }
	int MaxPostLBSlots()			{ return m_critPostLB; }
	bool HasReprocessing()			{ return m_fReproc; }
	int MaxRuleContext()			{ return m_nMaxRuleContext; }

	int MaxRuleLoop()				{ return m_nMaxRuleLoop; }
	int MaxBackup()					{ return m_nMaxBackup; }

	//	Setters:
	void SetBidi(bool f)			{ m_fBidi = f; }
	void AddRule(GdlRule* prule)	{ m_vprule.Push(prule); }

	void SetMaxRuleLoop(int n)		{ m_nMaxRuleLoop = n; }
	void SetMaxBackup(int n)		{ m_nMaxBackup = n; }

public:
	//	Parser:
	GdlRule * NewRule(GrpLineAndFile & lnf)
	{
		GdlRule * pruleNew = new GdlRule();
		pruleNew->SetLineAndFile(lnf);
		m_vprule.Push(pruleNew);
		return pruleNew;
	}

	void AddConstraint(GdlExpression * pexp)
	{
		m_vpexpConstraints.Push(pexp);
	}

	//	Post-parser:
	void ReplaceAliases();
	void HandleOptionalItems();
	void CheckSelectors();

	//	Pre-compiler:
	void FixRulePreContexts(Symbol psymAnyClass);
	void FixGlyphAttrsInRules(GrcManager * pcman, GrcFont * pfont);
	void FixFeatureTestsInPass(GrcFont * pfont);
	void MarkReplacementClasses(GrcManager * pcman,
		Set<GdlGlyphClassDefn *> & setpglfc);
	void CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
		GdlRenderer * prndr, Symbol psymTable, int grfrco);
	void CheckLBsInRules(Symbol psymTable);
	void ReplaceKern(GrcManager * pcman);
	void MaxJustificationLevel(int * pnJLevel);
	bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	void AssignGlobalID(int nID)
	{
		m_nGlobalID = nID;
	}
	bool HasRules()
	{
		return (m_vprule.Size() > 0);
	}
	void SetPreBidiPass(int n)
	{
		Assert(n == 0 || n == 1);
		m_nPreBidiPass = n;
	}

	//	Compiler:
	int GlobalID()
	{
		return m_nGlobalID;
	}
	int PassDebuggerNumber()
	{
		return m_nGlobalID + m_nPreBidiPass + 1;
	}
	void GenerateEngineCode(GrcManager *, int fxdRuleVersion, Vector<byte> & vbConstraints);
	void GenerateFsm(GrcManager * pcman);
	void GenerateFsmMachineClasses(GrcManager * pcman);
	void GenerateFsmTable(GrcManager * pcman);
	int AssignGlyphIDToMachineClasses(utf16 wGlyphID, int nPassID);
	int MachineClassKey(utf16 wGlyphID, int nPassID);
	void RecordInclusionInClass(utf16 wGlyphID, GdlGlyphClassDefn * pglfc);
	FsmMachineClass * MachineClassMatching(Vector<FsmMachineClass *> & vpfsmc,
		utf16 wGlyphID);
	void InitializeFsmArrays();
	void MergeIdenticalStates(int ifsFixMin, int ifsCheckMin, int ifsCheckLim);
	int NumberOfFsmMachineClasses();
	void GetMachineClassesForRuleItem(GdlRule  * prule, int irit,
		Set<FsmMachineClass *> & setpfsmc);
	int FindIdenticalState(int ifsToMatch, int ifsMin);
	void ReorderFsmStates();
	int NumStates();
	int NumAcceptingStates();
	int NumTransitionalStates();
	int NumSuccessStates();
	int NumFinalStates();
	void GenerateStartStates(GrcManager * pcman);

	//	Output:
	int TotalNumGlyphSubRanges();
	void OutputPass(GrcManager * pcman, GrcBinaryStream * pbstrm, int lTableStart);
	void GenerateRuleMaps(Vector<int> & vnOffsets, Vector<int> & vnRuleList);
	void OutputFsmTable(GrcBinaryStream * pbstrm);

	//	debuggers:
	void DebugEngineCode(GrcManager * pcman, int fxdRuleVersion, std::ostream & strmOut);
	void DebugRulePrecedence(GrcManager * pcman, std::ostream & strmOut);
	void DebugFsm(GrcManager * pcman, std::ostream & strmOut);
	void DebugFsmTable(GrcManager * pcman, std::ostream & strmOut, bool fWorking);
	void WalkFsmMachineClasses();

protected:
	//	Instance variables:
	int m_nNumber;
	int m_nMaxRuleLoop;
	int m_nMaxBackup;
	bool m_fBidi;
	Vector<GdlRule*> m_vprule;
	Vector<GdlExpression *>	m_vpexpConstraints; // multiple constraints result from -else if-

	int m_critMinPreContext;
	int m_critMaxPreContext;

	//	for compiler use:
//	int m_nNumber2;		// with respect to all the passes in all tables
	int m_nGlobalID;	// -1 if invalid--no rules
	int m_nPreBidiPass;	// 1 if there is a previous bidi pass, 0 otherwise
	int m_nMaxRuleContext;	// number of slots of input needed

	bool m_fLB;			// true if there is a rule containing line-break items
	bool m_fCrossLB;	// true if there are are cross-line-boundary contextualization rules
	int m_critPreLB;	// max number of slots before a LB slot
	int m_critPostLB;	// max number of slots after a LB slot
	bool m_fReproc;		// true if this pass has reprocessing happening in any of its rules


	//	Finite State Machine construction:

	FsmTable * m_pfsm;

	//	Mapping from glyph ID to column in the FSM
	HashMap<utf16, int> m_hmGlyphToColumn;

	//	Master list of machine classes:
	Vector<FsmMachineClass *> m_vpfsmc;

	//	For each glyph ID, its source-class-set (the set of source-classes it belongs to):
	SourceClassSet m_rgscsInclusions[kMaxTotalGlyphs];

	//	For each glyph ID, the machine class it is assigned to:
	FsmMachineClass * m_rgpfsmcAssignments[kMaxTotalGlyphs];

	//	Map enabling us to find the machine class for a given glyph ID's source-class-set.
	//	Each unique combination of source-classes corresponds to a machine class.
	//	The key into the map is the sum of the source-class IDs (for all the
	//	source classes a given glyph is a member of); the value is a list of MachineClasses
	//	whose source-class-IDs add up to that key. For instance, for the key 8 you might
	//	have a value which is a vector of three MachineClasses: the first containing
	//	SourceClasses 2 & 6, the second containing SourceClasses 1, 3, & 4, and the
	//	third containing SourceClass 8.
	HashMap<int, MachineClassList> m_hmMachineClassMap;

	Vector<int> m_vifsWorkToFinal;	// final indices of states, causing them to be ordered
									// as expected by the font/engine data structures:
									//		transitional, non-success
									//		transitional, success
									//		non-transitional, success
	Vector<int> m_vifsFinalToWork;	// inverse mapping from above vector, mapping the final
									// state indices back to the working indices

	Vector<int> m_vrowStartStates;

public:
	//	For test procedures:
	int test_NumberOfRules()
	{
		return m_vprule.Size();
	}
};


/*----------------------------------------------------------------------------------------------
Class: GdlRuleTable
Description: corresponds to the substitution, linebreak, or positioning table.
Hungarian: rultbl
----------------------------------------------------------------------------------------------*/

class GdlRuleTable : public GdlObject
{
public:
	//	Constructors & destructors:
	GdlRuleTable(Symbol psym)
		:	m_psymName(psym),
			m_fSubstitution(false)
	{
	}

	~GdlRuleTable()
	{
		for (int i = 0; i < m_vppass.Size(); ++i)
			delete m_vppass[i];
	}

	//	Getters:
	Symbol NameSymbol()		{ return m_psymName; }
	bool Substitution()		{ return m_fSubstitution; }

	//	Setters:
	void SetSubstitution(bool f)	{ m_fSubstitution = f; }

	int NumberOfPasses()
	{
		return m_vppass.Size();
	}

public:
	//	Post-parser:
	GdlPass * GetPass(GrpLineAndFile &, int nNumber, int nMaxRuleLoop, int nMaxBackup);
	void ReplaceAliases();
	void HandleOptionalItems();
	void CheckSelectors();

	//	Pre-compiler:
	void FixRulePreContexts(Symbol psymAnyClass);
	void FixGlyphAttrsInRules(GrcManager * pcman, GrcFont * pfont);
	void CheckTablesAndPasses(GrcManager * pcman, int *pnPassNum);
	void MarkReplacementClasses(GrcManager * pcman,
		Set<GdlGlyphClassDefn *> & setpglfc);
	void CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont, GdlRenderer * prndr);
	void CheckLBsInRules();
	void ReplaceKern(GrcManager * pcman);
	void MaxJustificationLevel(int * pnJLevel);
	bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	//	Compiler:
	void GenerateFsms(GrcManager * pcman);
	void CalculateContextOffsets(int * pcPrevious, int * pcFollowing, bool * pfLineBreak,
		bool fPos, GdlRuleTable * prultbl1, GdlRuleTable * prultbl2);

	enum { kInfiniteXlbContext = 255 };

	//	Output
	int CountPasses();
	void OutputPasses(GrcManager * pcman, GrcBinaryStream * pbstrm, long lTableStart,
		Vector<int> & vnOffsets);

	//	debuggers:
	void DebugEngineCode(GrcManager * pcman, int fxdRuleVersion, std::ostream & strmOut);
	void DebugRulePrecedence(GrcManager * pcman, std::ostream & strmOut);
	void DebugFsm(GrcManager * pcman, std::ostream & strmOut);


protected:
	//	Instance variables:
	Symbol				m_psymName;
	bool				m_fSubstitution;	// are substitutions (& associations) allowed in
											// this table?
	Vector<GdlPass*>	m_vppass;
};


#endif // TABLEPASS_INCLUDED
