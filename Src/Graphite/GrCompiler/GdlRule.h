/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GdlRule.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Classes to implement rules and rule items.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GDL_RULE_INCLUDED
#define GDL_RULE_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: GdlAttrValueSpec
Description: The action setting the value for a single attribute.
Hungarian: avs
----------------------------------------------------------------------------------------------*/

class GdlAttrValueSpec : public GdlObject
{
	friend class GdlRule;
	friend class GdlRuleItem;
	friend class GdlSetAttrItem;
	friend class GdlSubstitutionItem;

public:
	//	Constructors & destructors:
	GdlAttrValueSpec(Symbol psymName, Symbol psymOp, GdlExpression * pexpValue)
		:	m_psymName(psymName),
			m_psymOperator(psymOp),
			m_pexpValue(pexpValue),
			m_fFlattened(false)
	{
		m_pexpValue->PropagateLineAndFile(pexpValue->LineAndFile());
	}

	//	copy constructor
	GdlAttrValueSpec(const GdlAttrValueSpec & avs)
		:	GdlObject(avs),
			m_psymName(avs.m_psymName),
			m_psymOperator(avs.m_psymOperator),
			m_pexpValue(avs.m_pexpValue->Clone()),
			m_fFlattened(avs.m_fFlattened)
	{
	}

	~GdlAttrValueSpec()
	{
		delete m_pexpValue;
	}

	//	General:
	bool Flattened()
	{
		return m_fFlattened;
	}
	void SetFlattened(bool f)
	{
		m_fFlattened = f;
	}

protected:
	//	Parser:
	void PropagateLineAndFile(GrpLineAndFile & lnf)
	{
		if (LineIsZero())
		{
			SetLineAndFile(lnf);
			m_pexpValue->PropagateLineAndFile(lnf);
		}
	}

	//	Post-parser:
	void ReplaceAliases(GdlRule *);
	bool AdjustSlotRefs(Vector<bool>&, Vector<int>&, GdlRule *);

	//	Pre-compiler:
	void FixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit, Symbol psymOutClass);
	void FlattenPointSlotAttrs(GrcManager * pcman, Vector<GdlAttrValueSpec *> & vpavsNew);
	void CheckAttachAtPoint(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
		bool * pfXY, bool *pfGpoint);
	void CheckAttachWithPoint(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
		bool * pfXY, bool *pfGpoint);
	void FixFeatureTestsInRules(GrcFont * pfont);
	bool CheckRulesForErrors(GrcGlyphAttrMatrix * pgax,  GrcFont * pfont,
		GdlRenderer * prndr, Symbol psymTable, int rco,
		GdlRuleItem * prit, int irit,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel);
	void AdjustSlotRefsForPreAnys(int critPrependedAnys, GdlRuleItem * prit);
	void AdjustToIOIndices(GdlRuleItem * prit,
		Vector<int> & viritInput, Vector<int> & viritOutput);
	bool ReplaceKern(GrcManager * pcman,
		GdlAttrValueSpec ** ppavsShift, GdlAttrValueSpec ** ppavsAdvance);
	void MaxJustificationLevel(int * pnJLevel);
	bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	//	Compiler:
	bool GenerateAttrSettingCode(GrcManager *, int fxdRuleVersion, Vector<byte> & vbOutput,
		int irit, int nIIndex, int iritAttachTo);

private:
	void operator=(GdlAttrValueSpec);	// don't define the assignment operator

public:
	//	debuggers:
	void PrettyPrint(GrcManager * pcman, std::ostream & strmOut,
		bool * pfAtt, bool * pfAttAt, bool * pfAttWith, int cpavs);

protected:
	//	Instance variables:
	Symbol			m_psymName;
	Symbol			m_psymOperator;
	GdlExpression *	m_pexpValue;

	//	for compiler use:
	int m_nInternalID;	// internal ID for slot attribute
	bool m_fFlattened;	// an expression that was created from a more general expression being
						// "flattened;" ie, "attach.with = ptA" => "attach.with { x = ptA.x;
						// y = ptA.y; xoffset = ptA.xoffset; yoffset = ptA.yoffset }"

public:
	void test_SetLineAndFile(GrpLineAndFile & lnf)
	{
		SetLineAndFile(lnf);
		m_pexpValue->PropagateLineAndFile(lnf);
	}

};	//	end of GdlAttrValueSpec


/*----------------------------------------------------------------------------------------------
Class: GdlRuleItem
Description:
Hungarian: rit
----------------------------------------------------------------------------------------------*/

class GdlRuleItem : public GdlObject
{
	friend class GdlRule;
	friend class GdlLineBreakItem;
	friend class GdlSetAttrItem;
	friend class GdlSubstitutionItem;

public:
	//	Constructors & destructors:
	GdlRuleItem()
		:	m_psymInput(NULL),
			m_pexpConstraint(NULL)
	{
	}
	GdlRuleItem(Symbol psym)
		:	m_psymInput(psym),
			m_pexpConstraint(NULL)
	{
	}

	//	copy constructor
	GdlRuleItem(const GdlRuleItem & rit)
		:	GdlObject(rit),
			m_iritContextPos(rit.m_iritContextPos),
			m_iritContextPosOrig(rit.m_iritContextPosOrig),
			m_psymInput(rit.m_psymInput),
			m_nInputFsmID(rit.m_nInputFsmID),
			m_staAlias(rit.m_staAlias)
	{
		if (rit.m_pexpConstraint)
			m_pexpConstraint = rit.m_pexpConstraint->Clone();
		else
			m_pexpConstraint = NULL;
	}

	virtual GdlRuleItem * Clone()
	{
		return new GdlRuleItem(*this);
	}

	virtual ~GdlRuleItem()
	{
		if (m_pexpConstraint)
			delete m_pexpConstraint;
	}

	//	Alpha version of original item number (1-based), for error messages
	StrAnsi PosString()
	{
		char rgchItem[20];
		itoa(m_iritContextPosOrig + 1, rgchItem, 10);
		return StrAnsi(rgchItem);
	}

	//	Increment the context position by the given number
	void IncContextPosition(int dirit)
	{
		m_iritContextPos += dirit;
	}

public:
	//	For classes that don't do substitutions, the output is the input
	virtual Symbol OutputSymbol()
	{
		return m_psymInput;
	}

	//	Parser:
	virtual void AddAssociation(GrpLineAndFile &, int n);
	virtual void AddAssociation(GrpLineAndFile &, StrAnsi sta);
	virtual void AddAttrValueSpec(GdlAttrValueSpec * pavs)
	{
		Assert(false);	// should have been converted to an GdlSetAttrItem
	}

	void SetConstraint(GdlExpression * pexp)
	{
		Assert(!m_pexpConstraint);
		m_pexpConstraint = pexp;
		pexp->PropagateLineAndFile(LineAndFile());
	}

	void SetSlotName(StrAnsi sta)
	{
		m_staAlias = sta;
	}

	//	Post-parser:
	virtual void ReplaceAliases(GdlRule *);
	virtual bool AdjustSlotRefs(Vector<bool>& vfOmit, Vector<int>& vnNewIndices,
		GdlRule * prule);
	virtual void CheckSelectors(GdlRule * prule, int irit, int crit);

	//	Pre-compiler:
	virtual void FixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, GdlRule * prule, int irit);
	virtual void FlattenPointSlotAttrs(GrcManager * pcman);
	void AssignFsmInternalID(GrcManager * pcman, int nPassID);
	virtual void FindSubstitutionSlots(int irit,
		Vector<bool> & vfInput, Vector<bool> & vfOutput);
	void MarkClassAsReplacementClass(GrcManager * pcman,
		Set<GdlGlyphClassDefn *> & setpglfcReplace, bool fInput);
	virtual void FixFeatureTestsInRules(GrcFont *);
	virtual bool CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
		GdlRenderer * prndr, Symbol psymTable,
		int grfrco, int irit,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
		Vector<int> & vcwClassSizes);
	bool CheckForJustificationConstraint();
	virtual void AdjustSlotRefsForPreAnys(int critPrependedAnys);
	virtual void AssignIOIndices(int * pcritInput, int * pcritOutput,
		Vector<int> & viritInput, Vector<int> & viritOutput);
	virtual void AdjustToIOIndices(Vector<int> & viritInput, Vector<int> & viritOutput);
	virtual void SetAttachTo(int n)
	{
		Assert(false);	// only useful for GdlSetAttrItem
	}
	virtual int AttachTo()
	{
		return -1;
	}
	virtual int AttachedToSlot();
	bool OverlapsWith(GdlRuleItem * prit, GrcFont * pfont, int grfsdc);
///	void CheckLBsInRules(Symbol psymTable, int * pcritPreLB, int * pcritPostLB);
	virtual void ReplaceKern(GrcManager * pcman);
	virtual void MaxJustificationLevel(int * pnJLevel);
	virtual bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	//	Compiler:
	void GenerateConstraintEngineCode(GrcManager *, int fxdRuleVersion, Vector<byte> & vbOutput,
		int irit, Vector<int> & viritInput, int iritFirstModItem);
	virtual void GenerateActionEngineCode(GrcManager *, int fxdRuleVersion,
		Vector<byte> & vbOutput,
		GdlRule * prule, int irit, bool * pfSetInsertToFalse);
	static void GenerateInsertEqualsFalse(Vector<byte> & vbOutput);
	void GetMachineClasses(FsmMachineClass ** ppfsmcAssignments,
		Set<FsmMachineClass *> & setpfsmc);

private:
	void operator=(GdlRuleItem);	// don't call the assignment operator--compile error

public:
	//	debuggers:
	virtual void LhsPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
		std::ostream & strmOut);
	virtual void RhsPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
		std::ostream & strmOut);
	virtual void ContextPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
		std::ostream & strmOut);
	virtual void ConstraintPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
		std::ostream & strmOut);

protected:
	//	Instance variables:
	int m_iritContextPos;		// position within context (0-based)
	Symbol m_psymInput;
	GdlExpression * m_pexpConstraint;

	//	for parser use:
	StrAnsi m_staAlias;
	int m_iritContextPosOrig;	// original--not adjusted for optional items (0-based) or
								// inserted ANY's--for error messages

	//	for compiler use:
	int m_nInputFsmID;

	int m_nInputIndex;	// index of item relative to input stream (ignoring inserted items)
	int m_nOutputIndex;	// index of item relative to output stream (ignoring deleted items)

};	//	end of GdlRuleItem


/*----------------------------------------------------------------------------------------------
Class: GdlLineBreakItem
Description: A line-break item in the context of a rule
Hungarian: ritlb
----------------------------------------------------------------------------------------------*/

class GdlLineBreakItem : public GdlRuleItem
{
	friend class GdlRule;

public:
	//	Constructors & destructors:
	GdlLineBreakItem(Symbol psym)
		:	GdlRuleItem(psym)
	{
	}

	GdlLineBreakItem(const GdlRuleItem & rit)
		:	GdlRuleItem(rit)
	{
	}
	GdlLineBreakItem(const GdlLineBreakItem & rit)
		:	GdlRuleItem(rit)
	{
	}

	virtual GdlRuleItem * Clone()
	{
		return new GdlLineBreakItem(*this);
	}

protected:
	//	Parser:

	//	Post-parser:
	virtual void ReplaceAliases(GdlRule *);
	virtual bool AdjustSlotRefs(Vector<bool>&, Vector<int>&, GdlRule*);

	//	Pre-compiler:
	virtual void FixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, GdlRule * prule, int irit);
	virtual bool CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
		GdlRenderer * prndr, Symbol psymTable,
		int grfrco, int irit,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
		Vector<int> & vcwClassSizes);
	virtual void AdjustSlotRefsForPreAnys(int critPrependedAnys);
	virtual void AdjustToIOIndices(Vector<int> & viritInput, Vector<int> & viritOutput);

public:
	//	debuggers:
	virtual void ContextPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
		std::ostream & strmOut);

};	//	end of GdlLineBreakItem


/*----------------------------------------------------------------------------------------------
Class: GdlSetAttrItem
Description: An item in a positioning rule, having the effect of setting slot attributes.
Hungarian: ritset
----------------------------------------------------------------------------------------------*/

class GdlSetAttrItem : public GdlRuleItem
{
	friend class GdlRule;

public:
	//	Constructors & destructors:
	GdlSetAttrItem()
		:	GdlRuleItem()
	{
		m_nAttachTo = -1;
	}

	GdlSetAttrItem(Symbol psym)
		:	GdlRuleItem(psym)
	{
		m_nAttachTo = -1;
	}

	//	copy constructors
	GdlSetAttrItem(const GdlRuleItem & rit)
		:	GdlRuleItem(rit)
	{
		m_nAttachTo = -1;
	}

	GdlSetAttrItem(const GdlSetAttrItem & rit)
		:	GdlRuleItem(rit)
	{
		m_nAttachTo = -1;
		Assert(m_vpavs.Size() == 0);
		for (int i = 0; i < rit.m_vpavs.Size(); ++i)
			m_vpavs.Push(new GdlAttrValueSpec(*rit.m_vpavs[i]));
	}

	virtual ~GdlSetAttrItem()
	{
		for (int i = 0; i < m_vpavs.Size(); ++i)
			delete m_vpavs[i];
	}

	virtual GdlRuleItem * Clone()
	{
		return new GdlSetAttrItem(*this);
	}

protected:
	//	Parser:
	virtual void AddAttrValueSpec(GdlAttrValueSpec * pavs)
	{
		m_vpavs.Push(pavs);
		pavs->PropagateLineAndFile(LineAndFile());
	}

	//	Post-parser:
	virtual void ReplaceAliases(GdlRule *);
	virtual bool AdjustSlotRefs(Vector<bool>&, Vector<int>&, GdlRule *);

	//	Pre-compiler:
	virtual void FixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, GdlRule * prule, int irit);
	virtual void FlattenPointSlotAttrs(GrcManager * pcman);
	virtual Symbol OutputClassSymbol();
	void CheckCompBox(GrcManager * pcman, Symbol psymCompRef);
	virtual void FixFeatureTestsInRules(GrcFont *);
	virtual bool CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
		GdlRenderer * prndr, Symbol psymTable,
		int grfrco, int irit,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
		Vector<int> & vcwClassSizes);
	virtual void AdjustSlotRefsForPreAnys(int critPrependedAnys);
	virtual void AdjustToIOIndices(Vector<int> & viritInput, Vector<int> & viritOutput);
	virtual void ReplaceKern(GrcManager * pcman);
	virtual void MaxJustificationLevel(int * pnJLevel);
	virtual bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);
	virtual void SetAttachTo(int n)
	{
		m_nAttachTo = n;
	}
	virtual int AttachTo()
	{
		return m_nAttachTo;
	}
	virtual int AttachedToSlot();

protected:
	int AttachToSettingValue();

public:
	//	Compiler:
	virtual void GenerateActionEngineCode(GrcManager *, int fxdRuleVersion,
		Vector<byte> & vbOutput,
		GdlRule * prule, int irit, bool * pfSetInsertToFalse);
	bool GenerateAttrSettingCode(GrcManager *, int fxdRuleVersion, Vector<byte> & vbOutput,
		int irit, int nIIndex);

	//	debuggers:
	virtual void LhsPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
		std::ostream & strmOut);
	virtual void RhsPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
		std::ostream & strmOut);
	virtual void ContextPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
		std::ostream & strmOut);
	virtual void AttrSetterPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
		std::ostream & strmOut);

protected:
	//	Instance variables:
	Vector<GdlAttrValueSpec *> m_vpavs;

	int m_nAttachTo;	// index of attach.to slot attr (0-based); -1 if none

};	//	end of GdlSetAttrItem


/*----------------------------------------------------------------------------------------------
Class: GdlSubstitutionItem
Description: An item in a substitution rule.
Hungarian: ritsub
----------------------------------------------------------------------------------------------*/

class GdlSubstitutionItem : public GdlSetAttrItem
{
	friend class GdlRule;

public:
	//	Constructors & destructors:
	GdlSubstitutionItem(Symbol psymInput, Symbol psymOutput)
		:	GdlSetAttrItem(psymInput),
			m_psymOutput(psymOutput),
			m_pexpSelector(NULL),
			m_nSelector(-1)
	{
	}

	GdlSubstitutionItem(const GdlSetAttrItem & rit)
		:	GdlSetAttrItem(rit),
			m_psymOutput(rit.m_psymInput),
			m_pexpSelector(NULL),
			m_nSelector(-1)
	{
	}

	//	copy constructor
	GdlSubstitutionItem(const GdlSubstitutionItem & rit)
		:	GdlSetAttrItem(rit),
			m_psymOutput(rit.m_psymOutput),
//			m_vpexpAssocs(rit.m_vpexpAssocs),
			m_nSelector(rit.m_nSelector),
			m_nOutputFsmID(rit.m_nOutputFsmID),
			m_nInputSubsID(rit.m_nInputSubsID),
			m_nOutputSubsID(rit.m_nOutputSubsID)
	{
		for (int i = 0; i < rit.m_vpexpAssocs.Size(); ++i)
		{
			m_vpexpAssocs.Push(
				dynamic_cast<GdlSlotRefExpression*>(rit.m_vpexpAssocs[i]->Clone()));
		}

		m_pexpSelector =
			(rit.m_pexpSelector)?
				dynamic_cast<GdlSlotRefExpression*>(rit.m_pexpSelector->Clone()):
				NULL;
	}

	virtual GdlRuleItem * Clone()
	{
		return new GdlSubstitutionItem(*this);
	}

	virtual ~GdlSubstitutionItem()
	{
		for (int i = 0; i < m_vpexpAssocs.Size(); ++i)
			delete m_vpexpAssocs[i];

		if (m_pexpSelector)
			delete m_pexpSelector;
	}

protected:
	virtual Symbol OutputSymbol()
	{
		return m_psymOutput;
	}

	//	Parser:
public:
	virtual void AddAssociation(GrpLineAndFile &, int n);
	virtual void AddAssociation(GrpLineAndFile &, StrAnsi sta);

	//	Post-parser:
protected:
	virtual void ReplaceAliases(GdlRule *);
	virtual bool AdjustSlotRefs(Vector<bool>&, Vector<int>&, GdlRule *);
	virtual void CheckSelectors(GdlRule * prule, int irit, int crit);

	//	Pre-compiler:
	virtual void FixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, GdlRule * prule, int irit);
	virtual Symbol OutputClassSymbol();
	virtual void FindSubstitutionSlots(int irit,
		Vector<bool> & vfInput, Vector<bool> & vfOutput);
	virtual bool CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
		GdlRenderer * prndr, Symbol psymTable,
		int grfrco, int irit,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
		Vector<int> & vcwClassSizes);
	virtual void AdjustSlotRefsForPreAnys(int critPrependedAnys);
	virtual void AssignIOIndices(int * pcritInput, int * pcritOutput,
		Vector<int> & viritInput, Vector<int> & viritOutput);
	virtual void AdjustToIOIndices(Vector<int> & viritInput, Vector<int> & viritOutput);

	//	Compiler:
	virtual void GenerateActionEngineCode(GrcManager *, int fxdRuleVersion, Vector<byte> & vbOutput,
		GdlRule * prule, int irit, bool * pfSetInsertToFalse);
public:
	//	debuggers:
	virtual void LhsPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
		std::ostream & strmOut);
	virtual void RhsPrettyPrint(GrcManager * pcman, GdlRule * prule, int irit,
		std::ostream & strmOut);

protected:
	//	Instance variables:
	Symbol							m_psymOutput;	// possibly '_' or '@'
	GdlSlotRefExpression*			m_pexpSelector;	// original, 1-based
	Vector<GdlSlotRefExpression*>	m_vpexpAssocs;	// original, 1-based

	GdlRuleItem * m_pritSelInput;	// slot indicated by selector

	//	for pre-compiler use:
	Vector<int> m_vnAssocs;		// m_vpexpAssocs converted to input indices
	int			m_nSelector;	// m_pexpSelector converted to input index,
								//     or -1 if default (same item)

	//	for compiler use: -- not yet implemented
	int m_nOutputFsmID;
	int m_nInputSubsID;
	int m_nOutputSubsID;

};	//	end of GdlSubstitutionItem


/*----------------------------------------------------------------------------------------------
Class: GdlAlias
Description: A mapping of a slot alias to a slot index. Can also be used as an item that can
	hold either a slot alias or an index.
Hungarian: alias
----------------------------------------------------------------------------------------------*/

class GdlAlias
{
	friend class GdlRule;

public:
	//	Constructors:
	GdlAlias()
		:	m_srIndex(-1)
	{
	}

	GdlAlias(StrAnsi sta, int sr)
		:	m_staName(sta),
			m_srIndex(sr)
	{
	}

	GdlAlias(const GdlAlias & alias)
		:	m_staName(alias.m_staName),
			m_srIndex(alias.m_srIndex)
	{
	}

public:
	//	Parser:
	int Index()		{ return m_srIndex; }
	StrAnsi Name()	{ return m_staName; }

	void SetIndex(int sr)
	{
		m_srIndex = sr;
	}
	void SetName(StrAnsi sta)
	{
		m_staName = sta;
	}

	//	Post-parser:

	//	Adjust all the slot references based on the slots that were omitted because
	//	they were optional. Return false if there is a reference to an omitted slot.
	bool AdjustSlotRefs(Vector<bool>& vfOmit, Vector<int>& vnNewIndices)
	{
		//	Should no longer be necessary because we replace all the aliases
		//	before we do this.
		Assert(false);

		if (vfOmit[m_srIndex])
			return false;
		m_srIndex = vnNewIndices[m_srIndex];
		return true;
	}

protected:
	StrAnsi		m_staName;
	int			m_srIndex;	// 1-based

};	//	end of GdlAlias


/*----------------------------------------------------------------------------------------------
Class: GdlRule
Description:
Hungarian: rule
----------------------------------------------------------------------------------------------*/
class GdlRule : public GdlObject
{
	friend class GdlRuleItem;

public:
	//	Constructors & destructors:
	GdlRule()
	{
		m_nScanAdvance = -1;
		m_nDefaultAdvance = -1;
		m_fBadRule = false;
	}

	~GdlRule()
	{
		int i;
		for (i = 0; i < m_vprit.Size(); ++i)
			delete m_vprit[i];
		for (i = 0; i < m_vpexpConstraints.Size(); ++i)
			delete m_vpexpConstraints[i];
		for (i = 0; i < m_vpalias.Size(); ++i)
			delete m_vpalias[i];
	}

public:
	//	General:
	GdlRuleItem * Item(int irit)
	{
		Assert((unsigned int)irit < (unsigned int)m_vprit.Size());
		return m_vprit[irit];
	}
	int LookupAliasIndex(StrAnsi sta);

	int NumberOfSlots()
	{
		return m_vprit.Size();
	}

	bool IsBadRule()
	{
		return m_fBadRule;
	}

	int SortKey();

public:
	//	Parser:
	GdlRuleItem * ContextItemAt(GrpLineAndFile &, int irit,
		StrAnsi staInput, StrAnsi staAlias = "");
	GdlRuleItem * RhsItemAt(GrpLineAndFile &, int irit,
		StrAnsi staOutput, StrAnsi staAlias = "", bool fSubItem = false);
	GdlRuleItem * LhsItemAt(GrpLineAndFile &, int irit,
		StrAnsi staInput, StrAnsi staAlias = "");

	GdlRuleItem * ContextSelectorItemAt(GrpLineAndFile &, int irit,
		StrAnsi staClassOrAt, int nSel, StrAnsi staAlias = "");
	GdlRuleItem * ContextSelectorItemAt(GrpLineAndFile &, int irit,
		StrAnsi staClassOrAt, StrAnsi staSel, StrAnsi staAlias = "");

	GdlRuleItem * RhsSelectorItemAt(GrpLineAndFile &, int irit,
		StrAnsi staClassOrAt,  int nSel,StrAnsi staAlias = "");
	GdlRuleItem * RhsSelectorItemAt(GrpLineAndFile &, int irit,
		StrAnsi staClassOrAt, StrAnsi staSel, StrAnsi staAlias = "");

	GdlRuleItem * LhsSelectorItemAt(GrpLineAndFile &, int irit,
		StrAnsi staClassOrAt,  int nSel, StrAnsi staAlias = "");
	GdlRuleItem * LhsSelectorItemAt(GrpLineAndFile &, int irit,
		StrAnsi staClassOrAt, StrAnsi staSel, StrAnsi staAlias = "");

	void AddOptionalRange(int iritStart, int crit, bool fContext);
	int ScanAdvance()
	{
		return m_nScanAdvance;
	}
	void SetScanAdvance(int n)
	{
		Assert(n >= -1 && n <= m_vprit.Size());
		m_nScanAdvance = n;
	}
	void AddConstraint(GdlExpression * pexp)
	{
		m_vpexpConstraints.Push(pexp);
	}

	void InitialChecks();
protected:
	void CheckInputClass();
	void ConvertLhsOptRangesToContext();

public:
	//	Post-parser:
	void ReplaceAliases();
	bool HandleOptionalItems(Vector<GdlRule*> & vpruleNewList);
	void CheckSelectors();
	bool HasNoItems()
	{
		return m_vprit.Size() == 0;
	}
protected:
	bool AdjustOptRanges();
	void GenerateOptRanges(
			Vector<GdlRule*>&	vpruleNewList,
			Vector<bool>	&	vfOmitRange,
			int					irangeCurr);
	void GenerateOneRuleVersion(
			Vector<GdlRule*>&	vpruleNewList,
			Vector<bool>	&	vfOmitRange);
	int PrevRangeSubsumes(int irangeCurr);

public:
	//	Pre-compiler:
	int CountRulePreContexts();
	void FixRulePreContexts(Symbol psymAnyClass, int critNeeded);

	void FixGlyphAttrsInRules(GrcManager * pcman, GrcFont * pfont);
	void AssignClassInternalIDs(GrcManager * pcman, int nPassID,
		Set<GdlGlyphClassDefn *> & setpglfc);
	void FixFeatureTestsInRules(GrcFont *);
	void MarkReplacementClasses(GrcManager * pcman, int nPassID,
		Set<GdlGlyphClassDefn *> & setpglfcReplace);
	void MarkClassAsReplacementClass(GrcManager * pcman,
		Set<GdlGlyphClassDefn *> & setpglfcReplace, bool fInput);
	void CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont,
		GdlRenderer * prndr, Symbol psymTable, int grfrco);
	bool CheckForJustificationConstraint();
	void CalculateIOIndices();
	void GiveOverlapWarnings(GrcFont * pfont, int grfsdc);
	bool CheckLBsInRules(Symbol psymTable, int * pcritPreLB, int * pcritPostLB);
	bool HasReprocessing();
	void ReplaceKern(GrcManager * pcman);
	void MaxJustificationLevel(int * pnJLevel);
	bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	//	Compiler:
	void GenerateEngineCode(GrcManager *, int fxdRuleVersion,
		Vector<byte> & vbActions, Vector<byte> & vbConstraints);
	void GenerateConstraintEngineCode(GrcManager *, int fxdRuleVersion, Vector<byte> & vbOutput);
	GdlRuleItem * InputItem(int n);
	int NumberOfInputItems();
	int NumberOfPreModContextItems()
	{
		return m_critPreModContext;
	}

	//	debuggers:
	void DebugEngineCode(GrcManager * pcman, int fxdRuleVersion, std::ostream & strmOut);
	static void DebugEngineCode(Vector<byte> & vb, int fxdRuleVersion, std::ostream & strmOut);
	void RulePrettyPrint(GrcManager * pcman, std::ostream & strmOut);
	static StrAnsi SlotAttributeDebugString(int slat);
	static StrAnsi GlyphMetricDebugString(int gmet);
	static StrAnsi EngineCodeDebugString(int op);
	static StrAnsi ProcessStateDebugString(int pstat);

protected:
	//	Instance variables:
	int m_nScanAdvance;	// 0-based; item before which the ^ is placed, or -1 if no ^
	Vector<GdlRuleItem *>	m_vprit;
	Vector<GdlExpression *>	m_vpexpConstraints;	// multiple constraints come from multiple -if-s
	Vector<GdlAlias *>		m_vpalias;

	//	for post-parser use:
	Vector<bool>		m_vfOptRangeContext;  // are opt ranges below relative to context?
	Vector<int>			m_viritOptRangeStart;
	Vector<int>			m_viritOptRangeEnd;

	//	for pre-compiler use:
	//	input- and output indices for each item:
	Vector<int> m_viritInput;
	Vector<int> m_viritOutput;

	//	number of items in the context before the first modified item (original, before adding
	//	ANY items)
	int m_critPreModContext;

	//	number of ANY items that were prepended to the front of the rule
	int m_critPrependedAnys;

	//	original context length
	int m_critOriginal;

	//	scan-advance, adjusted
	int m_nOutputAdvance;

	//	where the scan advance would be if there were no caret (only calculated if the caret
	//	is present)
	int m_nDefaultAdvance;

	//	true if errors have been detected in this rule so don't keep processing
	bool m_fBadRule;

};	// end of GdlRule



#endif // !GDL_RULE_INCLUDED
