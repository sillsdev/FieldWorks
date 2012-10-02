/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GdlExpression.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Arithmetic and logical expressions that can appear in an GDL file.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GDL_EXP_INCLUDED
#define GDL_EXP_INCLUDED

class GdlRenderer;
class GdlRule;
class GrcGlyphAttrMatrix;
class GrcLigComponentList;
class GdlGlyphClassDefn;
class GdlAttrValueSpec;


/*----------------------------------------------------------------------------------------------
Class: GdlExpression
Description: Abstract superclass representing the various kinds of expressions that can serve
	as the values of glyph attributes, slot attributes, etc.
Hungarian: exp
----------------------------------------------------------------------------------------------*/

class GdlExpression : public GdlObject
{
	friend class GdlUnaryExpression;
	friend class GdlBinaryExpression;
	friend class GdlCondExpression;
	friend class GdlLookupExpression;

public:
	//	Constructors:
	GdlExpression() : m_exptResult(kexptUnknown)
	{
	}

	//	copy constructor
	GdlExpression(const GdlExpression & exp)
		:	GdlObject(exp),
			m_exptResult(exp.m_exptResult)
	{
	}

	virtual GdlExpression * Clone() = 0;

	virtual ~GdlExpression()
	{
	}

protected:
	//	Initialization:
	virtual void SetType(ExpressionType exptResult)
	{
		if (m_exptResult == kexptBoolean && exptResult == kexptNumber)
			return;

		Assert(
			exptResult == m_exptResult ||
			m_exptResult == kexptUnknown ||
			(m_exptResult == kexptNumber &&
				exptResult == kexptBoolean));

		m_exptResult = exptResult;
	}

public:
	//	Parser:
	virtual void PropagateLineAndFile(GrpLineAndFile &) = 0;

public:
	//	Post-parser:
	virtual bool ReplaceAliases(GdlRule *) = 0;
	virtual bool AdjustSlotRefs(Vector<bool>&, Vector<int>&, GdlRule *) = 0;
	virtual bool ResolveToInteger(int * pnRet, bool fSlotRef) = 0;
	virtual bool ResolveToFeatureID(unsigned int * pnRet);

public:
	//	Pre-compiler:
	virtual ExpressionType ExpType() = 0;
	bool TypeCheck(ExpressionType nExpectedType);
	bool TypeCheck(ExpressionType, ExpressionType, ExpressionType);
	bool TypeCheck(Vector<ExpressionType>& vnExpectedTypes);
	virtual bool CheckTypeAndUnits(ExpressionType * pexpt) = 0;
	virtual void GlyphAttrCheck() = 0;
	virtual void FixFeatureTestsInRules(GrcFont *) = 0;
	virtual GdlExpression * ConvertFeatureSettingValue(GdlFeatureDefn * pfeat) = 0;
	virtual void LookupExpCheck(bool fInIf) = 0;
	virtual GdlExpression * SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
		utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont,
		bool fGAttrDefChk, bool * pfCanSub) = 0;
	virtual GdlExpression * SimplifyAndUnscale(utf16 wGlyphID, GrcFont * pfont)
	{
		Set<Symbol> setpsym;
		bool fCanSub;
		return SimplifyAndUnscale(NULL, wGlyphID, setpsym, pfont, true, &fCanSub);
	}
	virtual void SetSpecialZero()
	{
	}
	virtual void CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit) = 0;
	virtual void CheckCompleteAttachmentPoint(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
		bool * pfXY, bool * pfGpoint) = 0;
	virtual bool GdlExpression::PointFieldEquivalents(GrcManager * pcman,
		GdlExpression ** ppexpX, GdlExpression ** ppexpY,
		GdlExpression ** ppexpGpoint,
		GdlExpression ** ppexpXoffset, GdlExpression ** ppexpYoffset);
	virtual bool CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
		bool fValue, bool fValueIsInputSlot) = 0;
	virtual void AdjustSlotRefsForPreAnys(int critPrependedAnys) = 0;
	virtual void AdjustToIOIndices(Vector<int> & virit, GdlRuleItem *) = 0;
	virtual void MaxJustificationLevel(int * pnLevel) = 0;
	virtual bool TestsJustification() = 0;
	virtual bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded) = 0;

	//	Compiler:
	virtual void GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
		int irit, Vector<int> * pviritInput, int nIIndex,
		bool fAttachAt, int iritAttachTo, int * pnValue) = 0;

	//	debuggers:
	virtual void PrettyPrint(GrcManager * pcman, std::ostream & strmOut) = 0;

private:
	//void operator=(GdlExpression);	// don't call the assignment operator

protected:
	//	Instance variables:
	ExpressionType m_exptResult;
};


/*----------------------------------------------------------------------------------------------
Class: GdlSimpleExpression
Description: Abstract superclass for expressions that have no expressions embedded, in case
	it becomes handy.
Hungarian:
----------------------------------------------------------------------------------------------*/

class GdlSimpleExpression : public GdlExpression
{
public:
	//	Constructors & destructors:
	GdlSimpleExpression()
		:	GdlExpression()
	{}

///	virtual GdlExpression * Clone() { return new GdlSimpleExpression(); }

	virtual ~GdlSimpleExpression()
	{
	}

	//	copy constructor
	GdlSimpleExpression(const GdlSimpleExpression & exp)
		:	GdlExpression(exp)
	{
	}


public:
	//	Parser:
	virtual void PropagateLineAndFile(GrpLineAndFile &)
	{
	}

public:
	//	Post-parser:
	virtual bool ReplaceAliases(GdlRule *)
	{
		return true;
	}
	virtual bool AdjustSlotRefs(Vector<bool>&, Vector<int>&, GdlRule *)
	{
		return true;
	}
	virtual bool ResolveToInteger(int * pnRet, bool fSlotRef)
	{
		return false;
	}

public:
	//	Pre-compiler:
	virtual ExpressionType ExpType()
	{
		return kexptUnknown;
	}

	virtual bool CheckTypeAndUnits(ExpressionType * pexpt)
	{
		*pexpt = kexptUnknown;
		return true;
	}

	virtual void GlyphAttrCheck() { }
	virtual void FixFeatureTestsInRules(GrcFont *) { }

	virtual GdlExpression * ConvertFeatureSettingValue(GdlFeatureDefn * pfeat)
		{ return this; }

	virtual void LookupExpCheck(bool fInIf) { }

	virtual GdlExpression * SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
		utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont,
		bool fGAttrDefChk, bool * pfCanSub)
	{
		return this;
	}

	virtual void CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit)
	{ }

	virtual void CheckCompleteAttachmentPoint(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
		bool * pfXY, bool * pfGpoint)
	{ }

	virtual bool PointFieldEquivalents(GrcManager * pcman,
		GdlExpression ** ppexpX, GdlExpression ** ppexpY,
		GdlExpression ** ppexpGpoint,
		GdlExpression ** ppexpXoffset, GdlExpression ** ppexpYoffset)
	{
		return false;
	}

	virtual bool CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
		bool fValue, bool fValueIsInputSlot)
	{
		return true;
	}

	virtual void AdjustSlotRefsForPreAnys(int critPrependedAnys)
	{
	}

	virtual void AdjustToIOIndices(Vector<int> & virit, GdlRuleItem *)
	{
	}

	virtual void MaxJustificationLevel(int * pnLevel)
	{
	}

	virtual bool TestsJustification()
	{
		return false;
	}

	virtual bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded)
	{
		return true;
	}

	//	Compiler:
	virtual void GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
		int irit, Vector<int> * pviritInput, int nIIndex,
		bool fAttachAt, int iritAttachTo, int * pnValue)
	{
	}

	//	debuggers:
	virtual void PrettyPrint(GrcManager * pcman, std::ostream & strmOut)
	{
		strmOut << "???";
	};
};


/*----------------------------------------------------------------------------------------------
Class: GdlNumericExpression
Description: Scaled or unscaled number
Hungarian:
----------------------------------------------------------------------------------------------*/

class GdlNumericExpression : public GdlSimpleExpression
{
	friend class GdlUnaryExpression;
	friend class GdlBinaryExpression;
	friend class GdlCondExpression;
	friend class GdlLookupExpression;

public:
	//	Constructors & destructors:
	GdlNumericExpression(int nValue)
		:	GdlSimpleExpression(),
			m_nValue(nValue),
			m_munits(kmunitNone)
	{
		SetType(kexptNumber);
	}
	GdlNumericExpression(int nValue, int munits)
		:	GdlSimpleExpression(),
			m_nValue(nValue),
			m_munits(munits)
	{
		if (m_munits == kmunitNone)
			SetType(kexptNumber);
		else
			SetType(kexptMeas);
	}

	//	copy constructor
	GdlNumericExpression(const GdlNumericExpression & exp)
		:	GdlSimpleExpression(exp),
			m_nValue(exp.m_nValue),
			m_munits(exp.m_munits)
	{}

	virtual GdlExpression * Clone()
	{
		return new GdlNumericExpression(*this);
	}

	//	Getters:
	int Value()	{ return m_nValue; }
	int	Units()	{ return m_munits; }

public:
	//	Parser:
	virtual void PropagateLineAndFile(GrpLineAndFile &);

public:
	//	Post-parser:
	virtual bool ReplaceAliases(GdlRule *);
	virtual bool AdjustSlotRefs(Vector<bool>&, Vector<int>&, GdlRule *);
	virtual bool ResolveToInteger(int * pnRet, bool fSlotRef);

public:
	//	Pre-compiler:
	virtual ExpressionType ExpType();
	virtual bool CheckTypeAndUnits(ExpressionType * pexpt);
	virtual void GlyphAttrCheck();
	virtual void FixFeatureTestsInRules(GrcFont *);
	virtual GdlExpression * ConvertFeatureSettingValue(GdlFeatureDefn * pfeat);
	virtual void LookupExpCheck(bool fInIf);
	virtual GdlExpression * SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
		utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont,
		bool fGAttrDefChk, bool * pfCanSub);
	virtual void SetSpecialZero();
	virtual void CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit);
	virtual void CheckCompleteAttachmentPoint(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
		bool * pfXY, bool * pfGpoint);
	virtual bool CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
		bool fValue, bool fValueIsInputSlot);
	virtual void AdjustSlotRefsForPreAnys(int critPrependedAnys);
	virtual void AdjustToIOIndices(Vector<int> & virit, GdlRuleItem *);
	virtual void MaxJustificationLevel(int * pnLevel);
	virtual bool TestsJustification();
	virtual bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	//	Compiler:
	virtual void GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
		int irit, Vector<int> * pviritInput, int nIIndex,
		bool fAttachAt, int iritAttachTo, int * pnValue);

	//	debuggers:
	virtual void PrettyPrint(GrcManager * pcman, std::ostream & strmOut);

protected:
	//	Instance variables:
	int m_nValue;
	int m_munits;	// Scaling in effect when expression was encountered, or kmunitNone
};


/*----------------------------------------------------------------------------------------------
Class: GdlSlotRefExpression
Description: Number or alias that should be interpreted as a slot in the rule;
	eg, @2, @vowel
Hungarian:
----------------------------------------------------------------------------------------------*/

class GdlSlotRefExpression : public GdlSimpleExpression
{
	friend class GdlUnaryExpression;
	friend class GdlBinaryExpression;
	friend class GdlCondExpression;
	friend class GdlLookupExpression;

public:
	//	Constructors & destructors:
	GdlSlotRefExpression(int sr)
		:	GdlSimpleExpression(),
			m_srNumber(sr)
	{
		SetType(kexptSlotRef);
	}
	GdlSlotRefExpression(StrAnsi sta)
		:	GdlSimpleExpression(),
			m_srNumber(-1),
			m_staName(sta)
	{
		SetType(kexptSlotRef);
	}

	//	copy constructor
	GdlSlotRefExpression(const GdlSlotRefExpression & exp)
		:	GdlSimpleExpression(exp),
			m_srNumber(exp.m_srNumber),
			m_staName(exp.m_staName),
			m_nIOIndex(exp.m_nIOIndex)
	{
	}

	virtual GdlExpression * Clone()
	{
		return new GdlSlotRefExpression(*this);
	}

	virtual ~GdlSlotRefExpression()
	{
	}

public:
	//	General:
	int SlotNumber()
	{
		return m_srNumber;
	}
	StrAnsi Alias()
	{
		return m_staName;
	}

public:
	//	Parser:
	virtual void PropagateLineAndFile(GrpLineAndFile &);

public:
	//	Post-parser:
	virtual bool ReplaceAliases(GdlRule *);
	virtual bool AdjustSlotRefs(Vector<bool>&, Vector<int>&, GdlRule *);
	virtual bool ResolveToInteger(int * pnRet, bool fSlotRef);

public:
	//	Pre-compiler:
	virtual ExpressionType ExpType();
	virtual bool CheckTypeAndUnits(ExpressionType * pexpt);
	virtual void GlyphAttrCheck();
	virtual void FixFeatureTestsInRules(GrcFont *);
	virtual GdlExpression * ConvertFeatureSettingValue(GdlFeatureDefn * pfeat);
	virtual void LookupExpCheck(bool fInIf);
	virtual GdlExpression * SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
		utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont,
		bool fGAttrDefChk, bool * pfCanSub);
	virtual void CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit);
	virtual void CheckCompleteAttachmentPoint(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
		bool * pfXY, bool * pfGpoint);
	virtual bool CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
		bool fValue, bool fValueIsInputSlot);
	virtual void AdjustSlotRefsForPreAnys(int critPrependedAnys);
	virtual void AdjustToIOIndices(Vector<int> & virit, GdlRuleItem *);
	virtual void MaxJustificationLevel(int * pnLevel);
	virtual bool TestsJustification();
	virtual bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	//	Compiler:
	virtual void GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
		int irit, Vector<int> * pviritInput, int nIIndex,
		bool fAttachAt, int iritAttachTo, int * pnValue);

	//	debuggers:
	virtual void PrettyPrint(GrcManager * pcman, std::ostream & strmOut);

protected:
	//	Instance variables:
	//	either the number or the name is used, not both
	int		m_srNumber;	// 1-based
	StrAnsi	m_staName;

	//	for compiler use:
	int m_nIOIndex;		// adjusted input index or output index (which ever is relevant
						// for the context)
};


/*----------------------------------------------------------------------------------------------
Class: GdlStringExpression
Description: An GDL string function.
Hungarian:
----------------------------------------------------------------------------------------------*/

class GdlStringExpression : public GdlSimpleExpression
{
	friend class GdlUnaryExpression;
	friend class GdlBinaryExpression;
	friend class GdlCondExpression;
	friend class GdlLookupExpression;

public:
	//	Constructors & destructors:
	GdlStringExpression(StrAnsi sta, int nCodepage)
		:	GdlSimpleExpression(),
			m_staValue(sta),
			m_nCodepage(nCodepage)
	{}

	//	copy constructor
	GdlStringExpression(const GdlStringExpression & exp)
		:	GdlSimpleExpression(exp),
			m_staValue(exp.m_staValue),
			m_nCodepage(exp.m_nCodepage)
	{}

	virtual GdlExpression * Clone()
	{
		return new GdlStringExpression(*this);
	}

public:
	//	Parser:
	virtual void PropagateLineAndFile(GrpLineAndFile &);

public:
	//	Post-parser:
	virtual bool ReplaceAliases(GdlRule *);
	virtual bool AdjustSlotRefs(Vector<bool>&, Vector<int>&, GdlRule *);
	virtual bool ResolveToInteger(int * pnRet, bool fSlotRef);
	virtual bool ResolveToFeatureID(unsigned int *pnRet);

	StrUni ConvertToUnicode();

public:
	//	Pre-compiler:
	virtual ExpressionType ExpType();
	virtual bool CheckTypeAndUnits(ExpressionType * pexpt);
	virtual void GlyphAttrCheck();
	virtual void FixFeatureTestsInRules(GrcFont *);
	virtual GdlExpression * ConvertFeatureSettingValue(GdlFeatureDefn * pfeat);
	virtual void LookupExpCheck(bool fInIf);
	virtual GdlExpression * SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
		utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont,
		bool fGAttrDefChk, bool * pfCanSub);
	virtual void CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit);
	virtual void CheckCompleteAttachmentPoint(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
		bool * pfXY, bool * pfGpoint);
	virtual bool CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
		bool fValue, bool fValueIsInputSlot);
	virtual void AdjustSlotRefsForPreAnys(int critPrependedAnys);
	virtual void AdjustToIOIndices(Vector<int> & virit, GdlRuleItem *);
	virtual void MaxJustificationLevel(int * pnLevel);
	virtual bool TestsJustification();
	virtual bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	//	Compiler:
	virtual void GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
		int irit, Vector<int> * pviritInput, int nIIndex,
		bool fAttachAt, int iritAttachTo, int * pnValue);

	//	debuggers:
	virtual void PrettyPrint(GrcManager * pcman, std::ostream & strmOut);

protected:
	//	Instance variables:
	StrAnsi	m_staValue;
	int		m_nCodepage;
};

/*----------------------------------------------------------------------------------------------
Class: GdlUnaryExpression
Description: Unary expression, for example, -(a + b), !boolean.
Hungarian:
----------------------------------------------------------------------------------------------*/

class GdlUnaryExpression : public GdlExpression
{
	friend class GdlBinaryExpression;
	friend class GdlCondExpression;
	friend class GdlLookupExpression;

public:
	//	Constructors & destructors:
	GdlUnaryExpression(Symbol psymOperator, GdlExpression * pexpOperand)
		:	GdlExpression(),
			m_psymOperator(psymOperator),
			m_pexpOperand(pexpOperand)
	{
		if (m_pexpOperand && m_pexpOperand->LineIsZero())
			m_pexpOperand->PropagateLineAndFile(m_lnf);
	}

	//	copy constructor
	GdlUnaryExpression(const GdlUnaryExpression & exp)
		:	GdlExpression(exp),
			m_psymOperator(exp.m_psymOperator),
			m_pexpOperand(exp.m_pexpOperand->Clone())
	{
	}

	virtual GdlExpression * Clone()
	{
		return new GdlUnaryExpression(*this);
	}

	virtual ~GdlUnaryExpression()
	{
		delete m_pexpOperand;
	}

	//	Getters:
	Symbol Operator()			{ return m_psymOperator; }
	GdlExpression* Operand()	{ return m_pexpOperand; }

public:
	//	Parser:
	virtual void PropagateLineAndFile(GrpLineAndFile &);

public:
	//	Post-parser:
	virtual bool ReplaceAliases(GdlRule *);
	virtual bool AdjustSlotRefs(Vector<bool>&, Vector<int>&, GdlRule *);
	virtual bool ResolveToInteger(int * pnRet, bool fSlotRef);

public:
	//	Pre-compiler:
	virtual ExpressionType ExpType();
	virtual bool CheckTypeAndUnits(ExpressionType * pexpt);
	virtual void GlyphAttrCheck();
	virtual void FixFeatureTestsInRules(GrcFont *);
	virtual GdlExpression * ConvertFeatureSettingValue(GdlFeatureDefn * pfeat);
	virtual void LookupExpCheck(bool fInIf);
	virtual GdlExpression * SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
		utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont,
		bool fGAttrDefChk, bool * pfCanSub);
	virtual void CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit);
	virtual void CheckCompleteAttachmentPoint(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
		bool * pfXY, bool * pfGpoint);
	virtual bool CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
		bool fValue, bool fValueIsInputSlot);
	virtual void AdjustSlotRefsForPreAnys(int critPrependedAnys);
	virtual void AdjustToIOIndices(Vector<int> & virit, GdlRuleItem *);
	virtual void MaxJustificationLevel(int * pnLevel);
	virtual bool TestsJustification();
	virtual bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	//	Compiler:
	virtual void GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
		int irit, Vector<int> * pviritInput, int nIIndex,
		bool fAttachAt, int iritAttachTo, int * pnValue);

	//	debuggers:
	virtual void PrettyPrint(GrcManager * pcman, std::ostream & strmOut);

protected:
	//	Instance variables:
	Symbol			m_psymOperator;
	GdlExpression *	m_pexpOperand;
};


/*----------------------------------------------------------------------------------------------
Class: GdlBinaryExpression
Description: Binary expression or function, for example, a + b, min(x,y).
Hungarian:
----------------------------------------------------------------------------------------------*/

class GdlBinaryExpression : public GdlExpression
{
	friend class GdlUnaryExpression;
	friend class GdlCondExpression;
	friend class GdlLookupExpression;

public:
	//	Constructors & destructors:
	GdlBinaryExpression(Symbol psymOperator, GdlExpression * pexpOp1, GdlExpression * pexpOp2)
		:	GdlExpression(),
			m_psymOperator(psymOperator),
			m_pexpOperand1(pexpOp1),
			m_pexpOperand2(pexpOp2)
	{
		if (m_pexpOperand1 && m_pexpOperand1->LineIsZero())
			m_pexpOperand1->PropagateLineAndFile(m_lnf);
		if (m_pexpOperand2 && m_pexpOperand2->LineIsZero())
			m_pexpOperand2->PropagateLineAndFile(m_lnf);
	}

	//	copy constructor
	GdlBinaryExpression(const GdlBinaryExpression & exp)
		:	GdlExpression(exp),
			m_psymOperator(exp.m_psymOperator),
			m_pexpOperand1(exp.m_pexpOperand1->Clone()),
			m_pexpOperand2(exp.m_pexpOperand2->Clone())
	{
	}

	virtual GdlExpression * Clone()
	{
		return new GdlBinaryExpression(*this);
	}

	virtual ~GdlBinaryExpression()
	{
		delete m_pexpOperand1;
		delete m_pexpOperand2;
	}

	//	Getters:
	Symbol	Operator()			{ return m_psymOperator; }
	GdlExpression*	Operand1()	{ return m_pexpOperand1; }
	GdlExpression*	Operand2()	{ return m_pexpOperand2; }

public:
	//	Parser:
	virtual void PropagateLineAndFile(GrpLineAndFile &);

public:
	//	Post-parser:
	virtual bool ReplaceAliases(GdlRule *);
	virtual bool AdjustSlotRefs(Vector<bool>&, Vector<int>&, GdlRule *);
	virtual bool ResolveToInteger(int * pnRet, bool fSlotRef);

public:
	//	Pre-compiler:
	virtual ExpressionType ExpType();
	virtual bool CheckTypeAndUnits(ExpressionType * pexpt);
	virtual void GlyphAttrCheck();
	virtual void FixFeatureTestsInRules(GrcFont *);
	virtual GdlExpression * ConvertFeatureSettingValue(GdlFeatureDefn * pfeat);
	virtual void LookupExpCheck(bool fInIf);
	virtual GdlExpression * SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
		utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont,
		bool fGAttrDefChk, bool * pfCanSub);
	virtual void CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit);
	virtual void CheckCompleteAttachmentPoint(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
		bool * pfXY, bool * pfGpoint);
	virtual bool CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
		bool fValue, bool fValueIsInputSlot);
	virtual void AdjustSlotRefsForPreAnys(int critPrependedAnys);
	virtual void AdjustToIOIndices(Vector<int> & virit, GdlRuleItem *);
	virtual void MaxJustificationLevel(int * pnLevel);
	virtual bool TestsJustification();
	virtual bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	//	Compiler:
	virtual void GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
		int irit, Vector<int> * pviritInput, int nIIndex,
		bool fAttachAt, int iritAttachTo, int * pnValue);

	//	debuggers:
	virtual void PrettyPrint(GrcManager * pcman, std::ostream & strmOut);

protected:
	//	Instance variables:
	Symbol			m_psymOperator;
	GdlExpression*	m_pexpOperand1;
	GdlExpression*	m_pexpOperand2;
};


/*----------------------------------------------------------------------------------------------
Class: GdlCondExpression
Description: Conditional expression, eg, (test)? true_value: false_value
Hungarian:
----------------------------------------------------------------------------------------------*/

class GdlCondExpression : public GdlExpression
{
	friend class GdlUnaryExpression;
	friend class GdlBinaryExpression;
	friend class GdlLookupExpression;

public:
	//	Constructors & destructors:
	GdlCondExpression(
		GdlExpression* pexpTest,
		GdlExpression * pexpTrue,
		GdlExpression * pexpFalse)
		:	GdlExpression(),
			m_pexpTest(pexpTest),
			m_pexpTrue(pexpTrue),
			m_pexpFalse(pexpFalse)
	{
//		m_pexpTest->SetType(kexptBoolean);
		if (m_pexpTest && m_pexpTest->LineIsZero())
			m_pexpTest->PropagateLineAndFile(m_lnf);
		if (m_pexpTrue && m_pexpTrue->LineIsZero())
			m_pexpTrue->PropagateLineAndFile(m_lnf);
		if (m_pexpFalse && m_pexpFalse->LineIsZero())
			m_pexpFalse->PropagateLineAndFile(m_lnf);
	}

	//	copy constructor
	GdlCondExpression(const GdlCondExpression & exp)
		:	GdlExpression(exp),
			m_pexpTest(exp.m_pexpTest->Clone()),
			m_pexpTrue(exp.m_pexpTrue->Clone()),
			m_pexpFalse(exp.m_pexpFalse->Clone())
	{
	}

	virtual GdlExpression * Clone()
	{
		return new GdlCondExpression(*this);
	}

	virtual ~GdlCondExpression()
	{
		delete m_pexpTest;
		delete m_pexpTrue;
		delete m_pexpFalse;
	}

	//	Getters:
	GdlExpression*	Test()		{ return m_pexpTest; }
	GdlExpression*	TrueExp()	{ return m_pexpTrue; }
	GdlExpression*	FalseExp()	{ return m_pexpFalse; }

public:
	//	Parser:
	virtual void PropagateLineAndFile(GrpLineAndFile &);

public:
	//	Post-parser:
	virtual bool ReplaceAliases(GdlRule *);
	virtual bool AdjustSlotRefs(Vector<bool>&, Vector<int>&, GdlRule *);
	virtual bool ResolveToInteger(int * pnRet, bool fSlotRef);

public:
	//	Pre-compiler:
	virtual ExpressionType ExpType();
	virtual bool CheckTypeAndUnits(ExpressionType * pexpt);
	virtual void GlyphAttrCheck();
	virtual void FixFeatureTestsInRules(GrcFont *);
	virtual GdlExpression * ConvertFeatureSettingValue(GdlFeatureDefn * pfeat);
	virtual void LookupExpCheck(bool fInIf);
	virtual GdlExpression * SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
		utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont,
		bool fGAttrDefChk, bool * pfCanSub);
	virtual void CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit);
	virtual void CheckCompleteAttachmentPoint(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
		bool * pfXY, bool * pfGpoint);
	virtual bool CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
		bool fValue, bool fValueIsInputSlot);
	virtual void AdjustSlotRefsForPreAnys(int critPrependedAnys);
	virtual void AdjustToIOIndices(Vector<int> & virit, GdlRuleItem *);
	virtual void MaxJustificationLevel(int * pnLevel);
	virtual bool TestsJustification();
	virtual bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	//	Compiler:
	virtual void GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
		int irit, Vector<int> * pviritInput, int nIIndex,
		bool fAttachAt, int iritAttachTo, int * pnValue);

	//	debuggers:
	virtual void PrettyPrint(GrcManager * pcman, std::ostream & strmOut);

protected:
	//	Instance variables:
	GdlExpression*	m_pexpTest;
	GdlExpression*	m_pexpTrue;
	GdlExpression*	m_pexpFalse;
};


/*----------------------------------------------------------------------------------------------
Class: GdlLookupExpression
Description: Expression to look up the value of a slot or glyph attribute, eg, linebreak,
	BoundingBox.Left.2, @3.Advance.Width
Hungarian:
----------------------------------------------------------------------------------------------*/

class GdlLookupExpression : public GdlExpression
{
	friend class GdlUnaryExpression;
	friend class GdlBinaryExpression;
	friend class GdlCondExpression;

public:
	//	name space in which to do look-up:
//	enum LookupType {
//		klookUnknown,
//		klookGlyph,
//		klookSlot,
//		klookFeature
//	};

	//	Constructors & destructors:
	GdlLookupExpression(Symbol psymName, int nSel, int nClus)
		:	GdlExpression(),
			m_psymName(psymName),
			m_nClusterLevel(nClus),
			m_pexpSimplified(NULL)
	{
		if (nSel > -1)
		{
			m_pexpSelector = new GdlSlotRefExpression(nSel);
			m_pexpSelector->PropagateLineAndFile(m_lnf);
		}
		else
			m_pexpSelector = NULL;
	}

	GdlLookupExpression(Symbol psymName, StrAnsi staSel, int nClus)
		:	GdlExpression(),
			m_psymName(psymName),
			m_nClusterLevel(nClus),
			m_pexpSimplified(NULL)
	{
		m_pexpSelector = new GdlSlotRefExpression(staSel);
		m_pexpSelector->PropagateLineAndFile(m_lnf);
	}

	GdlLookupExpression(Symbol psymName, GdlSlotRefExpression * pexpSel, int nClus)
		:	GdlExpression(),
			m_psymName(psymName),
			m_pexpSelector(pexpSel),
			m_nClusterLevel(nClus),
			m_pexpSimplified(NULL)
	{
	}

	GdlLookupExpression(Symbol psymName, int nSel)
		:	GdlExpression(),
			m_psymName(psymName),
			m_nClusterLevel(0),
			m_pexpSimplified(NULL)
	{
		m_pexpSelector = new GdlSlotRefExpression(nSel);
		m_pexpSelector->PropagateLineAndFile(m_lnf);
	}

	GdlLookupExpression(Symbol psymName, StrAnsi staSel)
		:	GdlExpression(),
			m_psymName(psymName),
			m_nClusterLevel(0),
			m_pexpSimplified(NULL)
	{
		m_pexpSelector = new GdlSlotRefExpression(staSel);
		m_pexpSelector->PropagateLineAndFile(m_lnf);
	}

	GdlLookupExpression(Symbol psymName)
		:	GdlExpression(),
			m_psymName(psymName),
			m_pexpSelector(NULL),
			m_nClusterLevel(0),
			m_pexpSimplified(NULL)
	{
	}

	//	copy constructor
	GdlLookupExpression(const GdlLookupExpression& exp)
		:	GdlExpression(exp),
			m_psymName(exp.m_psymName),
			m_nClusterLevel(exp.m_nClusterLevel),
			m_nInternalID(exp.m_nInternalID),
			m_nSubIndex(exp.m_nSubIndex)
	{
		m_pexpSelector =
			(exp.m_pexpSelector) ?
				new GdlSlotRefExpression(*exp.m_pexpSelector) :
				NULL;
		m_pexpSimplified =
			(exp.m_pexpSimplified) ?
				new GdlNumericExpression(*exp.m_pexpSimplified) :
				NULL;
	}

	virtual GdlExpression * Clone()
	{
		return new GdlLookupExpression(*this);
	}

	virtual ~GdlLookupExpression()
	{
		if (m_pexpSelector)
			delete m_pexpSelector;
		if (m_pexpSimplified)
			delete m_pexpSimplified;
	}

	//	Getters:
	Symbol Name()				{ return m_psymName; }

	GdlSlotRefExpression* Selector()
	{
		return m_pexpSelector;
	}

	bool NameFitsSymbolType(SymbolType symt)
	{
		return m_psymName->FitsSymbolType(symt);
	}

public:
	//	Parser:
	virtual void PropagateLineAndFile(GrpLineAndFile &);

public:
	//	Post-parser:
	virtual bool ReplaceAliases(GdlRule *);
	virtual bool AdjustSlotRefs(Vector<bool>&, Vector<int>&, GdlRule *);
	virtual bool ResolveToInteger(int * pnRet, bool fSlotRef);

public:
	//	Pre-compiler:
	virtual ExpressionType ExpType();
	virtual bool CheckTypeAndUnits(ExpressionType * pexpt);
	virtual void GlyphAttrCheck();
	virtual void FixFeatureTestsInRules(GrcFont *);
	virtual GdlExpression * ConvertFeatureSettingValue(GdlFeatureDefn * pfeat);
	virtual void LookupExpCheck(bool fInIf);
	virtual GdlExpression * SimplifyAndUnscale(GrcGlyphAttrMatrix * pgax,
		utf16 wGlyphID, Set<Symbol> & setpsym, GrcFont * pfont,
		bool fGAttrDefChk, bool * pfCanSub);
	virtual void CheckAndFixGlyphAttrsInRules(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit);
	virtual void CheckCompleteAttachmentPoint(GrcManager * pcman,
		Vector<GdlGlyphClassDefn *> & vpglfcInClasses, int irit,
		bool * pfXY, bool * pfGpoint);
	virtual bool PointFieldEquivalents(GrcManager * pcman,
		GdlExpression ** ppexpX, GdlExpression ** ppexpY,
		GdlExpression ** ppexpGpoint,
		GdlExpression ** ppexpXoffset, GdlExpression ** ppexpYoffset);
	virtual bool CheckRuleExpression(GrcFont * pfont, GdlRenderer * prndr,
		Vector<bool> & vfLb, Vector<bool> & vfIns, Vector<bool> & vfDel,
		bool fValue, bool fValueIsInputSlot);
	virtual void AdjustSlotRefsForPreAnys(int critPrependedAnys);
	virtual void AdjustToIOIndices(Vector<int> & virit, GdlRuleItem *);
	virtual void MaxJustificationLevel(int * pnLevel);
	virtual bool TestsJustification();
	virtual bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	//	Compiler:
	virtual void GenerateEngineCode(int fxdRuleVersion, Vector<byte> & vbOutput,
		int irit, Vector<int> * pviritInput, int nIIndex,
		bool fAttachAt, int iritAttachTo, int * pnValue);

	//	debuggers:
	virtual void PrettyPrint(GrcManager * pcman, std::ostream & strmOut);

protected:
	//	Instance variables:
	Symbol					m_psymName;
	GdlSlotRefExpression *	m_pexpSelector;	// 1-based
	int						m_nClusterLevel;

//	LookupType				m_lookType;	// glyph attr, slot attr, feature

	GdlNumericExpression *	m_pexpSimplified;

	//	for compiler use:
	int m_nInternalID;
	int m_nSubIndex;	//	for indexed attributes (component.X.ref)
};


#endif // !GDL_EXP_INCLUDED
