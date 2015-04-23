/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrcMasterTable.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Classes to implement master tables used by the post-parser.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef MASTERTBL_INCLUDED
#define MASTERTBL_INCLUDED

class GdlFeatureDefn;
class GdlGlyphClassDefn;
class GdlNameDefn;

/*----------------------------------------------------------------------------------------------
Class: GdlAssignment
Description: An expression, the statement number in which that expression was assigned,
	and the current values of relevant directives.
	This class, used by the master tables, is responsible for deleting the expression.
Hungarian: asgn
----------------------------------------------------------------------------------------------*/
class GdlAssignment : public GrcAssignment
{
	friend class GrcMasterValueList;
	friend class GrcMasterTable;

public:
	//	Constructors & destructor:
	GdlAssignment()
		:	GrcAssignment()
	{
	}

	GdlAssignment(GdlExpression * pexp, int nPR, int munitPR, bool fOverride, GrpLineAndFile const& lnf)
		:	GrcAssignment(pexp, nPR, munitPR, fOverride, lnf)
	{
	}

	~GdlAssignment()
	{
		if (m_pexp)
			delete m_pexp;
	}
	virtual void Set(GdlExpression * pexp, int nPR, int mPrUnits, bool fOverride,
		GrpLineAndFile const& lnf)
	{
		if (m_pexp)
			delete m_pexp;
		GrcAssignment::Set(pexp, nPR, mPrUnits, fOverride, lnf);
	}
};


/*----------------------------------------------------------------------------------------------
Class: GrcMasterValueList
Description: A list of values for a single entry in the master table; for instance, a list of
	glyph attributes for a class, or a list of feature information for a feature. The keys
	are Symbols of glyph attributes or feature fields; the values are assignment statements.
	Used only by the post-parser.
Hungarian: mvl
----------------------------------------------------------------------------------------------*/

class GrcMasterValueList	// hungarian: mvl
{
	friend class GrcMasterTable;

	typedef HashMap<Symbol, GdlAssignment*> ValueMap; // hungarian: valmap

public:
	//	Constructor
	GrcMasterValueList()
	{
	}

	//	Destructor:
	~GrcMasterValueList()
	{
		//	Once the list has been processed, the assignment items might be deleted
		//	as part of the class definition, or whatever, so don't do it here.
		for (ValueMap::iterator it = m_valmapEntries.Begin();
			it != m_valmapEntries.End();
			++it)
		{
			delete it.GetValue();
		}
	}

	void AddItem(Symbol psym, GdlExpression * pexpValue,
		int nPR, int munitPR, bool fOverride, GrpLineAndFile const& lnf,
		StrAnsi staDescription);

	//	Iterators:
	ValueMap::iterator EntriesBegin()
	{
		return m_valmapEntries.Begin();
	}
	ValueMap::iterator EntriesEnd()
	{
		return m_valmapEntries.End();
	}

	//	Post-parser:
protected:
	void SetupFeatures(GdlFeatureDefn * pfeat);
	void SetupGlyphAttrs(GdlGlyphClassDefn * pglfc);
public:
	void GrcMasterValueList::SetupNameDefns(NameDefnMap & hmNameMap);

protected:
	//	instance variables:
//	SymbolType	m_symt	// ?????
	ValueMap	m_valmapEntries;
};


/*----------------------------------------------------------------------------------------------
Class: GrcMasterTable
Description: There will be two instances of this table: one to hold all the glyph attribute
	settings for all the classes, and one to hold all the feature definitions.

	For the glyph attribute table, the keys are class name Symbols; the values are
	GrMasterValueLists containing the attribute-setting statements for the class.

	For the feature table, the keys are feature name Symbols; the values are GrMasterValueLists
	containing the assignments for the feature.
Hungarian: mtb
----------------------------------------------------------------------------------------------*/

class GrcMasterTable
{
	friend class GrcMasterValueList;

	typedef HashMap<Symbol, GrcMasterValueList*> ValueListMap;	// hungarian: vlistmap

public:
	//	Destructor:
	~GrcMasterTable()
	{
		for (ValueListMap::iterator it = m_vlistmapEntries.Begin();
			it != m_vlistmapEntries.End();
			++it)
		{
			delete it.GetValue();
		}
	}

	void AddItem(Symbol psym, GdlExpression * pexpValue,
		int nPR, int munitPR, bool fOverride, GrpLineAndFile const& lnf,
		StrAnsi staDescription);

	GrcMasterValueList * ValueListFor(Symbol psym);
	GdlExpression * ItemValue(GrcStructName* psymClassOrFeat, GrcStructName* psymField);

	//	Find the feature corresponding to the standard styles. If it is there,
	//	store its settings in the master style list (clearing anything that does
	//	not belong).
	bool RecordStdStyles(GrcStructName * pxnsFeat);

	//	Iterators:
	ValueListMap::iterator EntriesBegin()
	{
		return m_vlistmapEntries.Begin();
	}
	ValueListMap::iterator EntriesEnd()
	{
		return m_vlistmapEntries.End();
	}

public:
	//	Post-parser:
	void SetupFeatures();
	void SetupGlyphAttrs();

	//	Pre-compiler:

protected:
	//	instance variables:
	ValueListMap m_vlistmapEntries;
};


#endif // MASTERTBL_INCLUDED
