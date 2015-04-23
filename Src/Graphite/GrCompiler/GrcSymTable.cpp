/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrcSymTable.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implementation of the parser/compiler symbol table.
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
	Destructors
----------------------------------------------------------------------------------------------*/
GrcSymbolTable::~GrcSymbolTable()
{
	m_psymParent = NULL;

	for (SymbolTableMap::iterator it = m_hmstasymEntries.Begin();
		it != m_hmstasymEntries.End();
		++it)
	{
		delete it.GetValue();
	}
}


GrcSymbolTableEntry::~GrcSymbolTableEntry()
{
	delete m_psymtblSubTable;
}


/*----------------------------------------------------------------------------------------------
	Add a symbol to the main symbol table in the normal way, assuming there is nothing
	special to do.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTable::AddSymbol(const GrcStructName & xns, SymbolType symt,
	GrpLineAndFile const& lnf)
{
	Assert(m_cLevel == 0);
	return AddSymbolAux(xns, symt, ksymtInvalid, lnf);
}


/*----------------------------------------------------------------------------------------------
	Add a symbol that is the name of a class to the main symbol table (if it is not already
	there). Also, ensure that it has an GdlGlyphClassDefn as its data.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTable::AddClassSymbol(const GrcStructName & xns, GrpLineAndFile const& lnf)
{
	Assert(m_cLevel == 0);
	Assert(xns.NumFields() == 1);

	Symbol psymAdded = AddSymbolAux(xns, ksymtClass, ksymtInvalid, lnf);
	if (!psymAdded->HasData())
	{
		GdlGlyphClassDefn * pglfc = new GdlGlyphClassDefn();
		pglfc->SetLineAndFile(lnf);
		psymAdded->SetData(pglfc);
	}

	return psymAdded;
}


/*----------------------------------------------------------------------------------------------
	Add a symbol that is the name of a feature to the main symbol table (if it is not already
	there). Also, ensure that it has an GdlFeatureDefn as its data.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTable::AddFeatureSymbol(const GrcStructName & xns, GrpLineAndFile const& lnf)
{
	Assert(m_cLevel == 0);
	Assert(xns.NumFields() == 1);

	Symbol psymAdded = AddSymbolAux(xns, ksymtFeature, ksymtInvalid, lnf);
	if (!psymAdded->HasData())
	{
		GdlFeatureDefn * pfeat = new GdlFeatureDefn();
		pfeat->SetLineAndFile(lnf);
		psymAdded->SetData(pfeat);
	}
	psymAdded->SetExpType(kexptNumber);

	return psymAdded;
}


/*----------------------------------------------------------------------------------------------
	Add a symbol that is the name of a feature to the main symbol table (if it is not already
	there). Also, ensure that it has an GdlFeatureDefn as its data.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTable::AddLanguageSymbol(const GrcStructName & xns, GrpLineAndFile const& lnf)
{
	Assert(m_cLevel == 0);
	Assert(xns.NumFields() == 1);

	Symbol psymAdded = AddSymbolAux(xns, ksymtLanguage, ksymtInvalid, lnf);
	if (!psymAdded->HasData())
	{
		GdlLanguageDefn * plang = new GdlLanguageDefn();
		plang->SetLineAndFile(lnf);
		psymAdded->SetData(plang);
	}
	psymAdded->SetExpType(kexptNumber);

	return psymAdded;
}


/*----------------------------------------------------------------------------------------------
	Add a symbol that is the name of a class's glyph attribute to the main symbol table
	(if it is not already there).
	Ensure that there is a generic version of the glyph attribute in the symbol table
	(minus the class name),	and set a pointer to it.
	If it is one of the standard attributes, set its expression type.
	If is a component glyph attribute, create a corresponding ".ref" slot attribute.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTable::AddGlyphAttrSymbol(const GrcStructName & xns, GrpLineAndFile const& lnf,
	ExpressionType expt, bool fMetric)
{
	Assert(m_cLevel == 0);
	SymbolType symt = (fMetric) ? ksymtGlyphMetric : ksymtGlyphAttr;
	SymbolType symtOther = (fMetric) ? ksymtInvalid : ksymtInvalidGlyphAttr;
	Symbol psymAdded = AddSymbolAux(xns, symt, symtOther, lnf);
	psymAdded->AdjustExpTypeIfPossible(expt);

	//	Find or add the generic version of the glyph attribute (without the class name).
	GrcStructName xnsGeneric;
	xns.CopyMinusFirstField(xnsGeneric);	// take off the class name
	Symbol psymGeneric = FindSymbol(xnsGeneric);
	if (psymGeneric)
	{
		if (psymGeneric->m_symt == ksymtGlyphMetric && !fMetric)
		{
			StrAnsi staMsg("Cannot set the value of a glyph metric: ");
			StrAnsi staName = psymGeneric->FullName();
			staMsg.Append(staName.Chars());
			g_errorList.AddItem(true, 1184, NULL, &lnf, staMsg); // fatal error
			return NULL;
		}
		if (psymGeneric->m_symt == ksymtInvalid)
		{
			//	previously undefined symbol, now we know it is a glyph attribute or metric
			psymGeneric->m_fGeneric = true;
			psymGeneric->m_symt = symt;
		}
		Assert(psymGeneric->m_fGeneric);
		if (!psymGeneric->FitsSymbolType(symt))
		{
			//	Symbol being used as a glyph attribute was previously used as something else.
			Assert(false);
			return psymAdded;
		}

		psymGeneric->AdjustExpTypeIfPossible(expt);
		psymAdded->SetExpType(psymGeneric->ExpType());
	}
	else
	{
		Assert(!fMetric); // all metrics should be defined from the outset

		psymGeneric = AddSymbolAux(xnsGeneric, symt, symtOther, lnf);
		psymGeneric->m_fGeneric = true;
		psymGeneric->AdjustExpTypeIfPossible(expt);
	}

	psymAdded->SetGeneric(psymGeneric);

	//	For component attributes, define the corresponding component.?.ref attribute, and
	//	link up the generic version of the base ligature symbol.
	if (psymGeneric->IsComponentBoxField())
	{
		GrcStructName xnsCompRef(xnsGeneric);
		xnsCompRef.DeleteField(xnsCompRef.NumFields() - 1);
		xnsCompRef.InsertField(xnsCompRef.NumFields(), "reference");
		Symbol psymCompRef = AddSymbolAux(xnsCompRef, ksymtSlotAttr, ksymtSlotAttr, lnf);
		psymCompRef->m_expt = kexptSlotRef;

		Symbol psymBaseLig = psymAdded->BaseLigComponent();
		Assert(psymBaseLig);
		Symbol psymGenericBaseLig = psymGeneric->BaseLigComponent();
		psymGenericBaseLig->m_fGeneric = true;
		psymBaseLig->SetGeneric(psymGenericBaseLig);
	}

	return psymAdded;
}


/*----------------------------------------------------------------------------------------------
	Add a symbol that is of the form <class>.component.<X>.... to the main symbol table
	(if it is not already there). CURRENTLY NOT USED
----------------------------------------------------------------------------------------------*/
#if 0
Symbol GrcSymbolTable::AddComponentField(const GrcStructName & xns, GrpLineAndFile const& lnf)
{
	Assert(m_cLevel == 0);
	Assert(xns.FieldEquals(1, StrAnsi("component")));

	SymbolType symt;
	if (xns.FieldEquals(3, StrAnsi("top")) ||
			xns.FieldEquals(3, StrAnsi("bottom")) ||
			xns.FieldEquals(3, StrAnsi("left")) ||
			xns.FieldEquals(3, StrAnsi("right")))
		symt = ksymtGlyphAttr;	// valid component glyph attribute

	else if (xns.FieldEquals(3, StrAnsi("reference")))
		symt = ksymtSlotAttr;	// valid component slot attribute
	else

	{
		//	Invalid use of component attribute.
		Assert(false);
		return NULL;
	}

	Symbol psymAdded = AddGlyphAttrSymbol(xns, lnf, kexptMeas);
	Symbol psymGeneric = psymAdded->Generic();
	Assert(psymGeneric->m_fGeneric);
	ExpressionType exptGeneric = psymGeneric->ExpType();

	if (symt == ksymtGlyphAttr)
	{
		Assert(exptGeneric == kexptUnknown || exptGeneric == kexptMeas);
		psymGeneric->SetExpType(kexptMeas);
		psymAdded->SetExpType(kexptMeas);
	}
	else // symt == ksymtSlotAttr
	{
		Assert(exptGeneric == kexptUnknown || exptGeneric == kexptSlotRef);
		psymGeneric->SetExpType(kexptSlotRef);
		psymAdded->SetExpType(kexptSlotRef);
	}

	return psymAdded;
}
#endif // 0

/*----------------------------------------------------------------------------------------------
	Add a new unique class-name symbol to the table. Return the new symbol.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTable::AddAnonymousClassSymbol(GrpLineAndFile const& lnf)
{
	char rgch[20];
	itoa(m_csymAnonClass, rgch, 10);
	m_csymAnonClass++;

	StrAnsi sta = "*GC";
	sta += rgch;
	sta += "*";
	Assert(!FindField(sta));

	GrcStructName xns(sta);
	return AddClassSymbol(xns, lnf);
}

/*----------------------------------------------------------------------------------------------
	Add a symbol to the table if it is not already there. Return an error code if the
	symbol is already there but is of a different type. (Nodes along the way may
	be of different types, but the final node may not.)

	Caller should ensure that if the symbol is already present it has the expected type,
	or raise an error.

	Arguments:
		xns				- structured name of symbol
		symtLeaf		- symbol type to use for leaf node
		symtOther		- symbol type to use for nodes along the way (generally ksymtInvalid
							or ksymtInvalidGlyphAttr)
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTable::AddSymbolAux(const GrcStructName & xns,
	SymbolType symtLeaf, SymbolType symtOther, GrpLineAndFile const& lnf)
{
	Symbol psym = NULL;
	GrcSymbolTable * psymtbl = this;

	for (int i = 0; i < xns.NumFields(); ++i)
	{
		StrAnsi staField = xns.FieldAt(i);

		if (psymtbl == NULL)	// never true first time through
		{
			psymtbl = new GrcSymbolTable(false);
			psymtbl->MakeSubTableOf(psym);
		}

		psym = psymtbl->FindField(staField);
		if (psym == NULL)
		{
			psym = new GrcSymbolTableEntry(staField,
				((i == xns.NumFields() - 1) ? symtLeaf : symtOther),
				psymtbl);
			psymtbl->m_hmstasymEntries.Insert(staField, psym);
		}
		psymtbl = psym->m_psymtblSubTable;
	}

	// TODO: if the symbol is of the form <class>.<predefined-glyph-attr>, define it
	// as a glyph attribute.

	if (psym->m_lnf.NotSet())
		psym->m_lnf = lnf;

	if (psym->m_symt == ksymtInvalid)
		psym->SetSymbolType(symtLeaf);

	Assert(psym->m_symt == symtLeaf);

	return psym;
}


/*----------------------------------------------------------------------------------------------
	Answer true if the symbol fits the given type.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::FitsSymbolType(SymbolType symt)
{
	if (m_symt == symt || m_symt2 == symt)
		return true;

	//	Handle subtypes
	switch (symt)
	{
	case ksymtTable:
		if (FitsSymbolType(ksymtTableRule))
			return true;
		break;

	case ksymtGlyph:
		if (FitsSymbolType(ksymtGlyphAttr))
			return true;
		if (FitsSymbolType(ksymtGlyphMetric))
			return true;
		break;

	case ksymtGlyphAttr:
		if (FitsSymbolType(ksymtGlyphAttrComp))
			return true;
		break;

	case ksymtInvalid:
		if (FitsSymbolType(ksymtInvalidGlyphAttr))
			return true;
		break;

	case ksymtSlotAttr:
		if (FitsSymbolType(ksymtSlotAttrPt))
			return true;
		if (FitsSymbolType(ksymtSlotAttrCompRef))
			return true;
		break;

	case ksymtSpecial:
		if (FitsSymbolType(ksymtSpecialAt))
			return true;
		if (FitsSymbolType(ksymtSpecialCaret))
			return true;
		if (FitsSymbolType(ksymtSpecialLb))
			return true;
		if (FitsSymbolType(ksymtSpecialUnderscore))
			return true;
		break;

	case ksymtOperator:
		if (FitsSymbolType(ksymtOpAssign))
			return true;
		break;

	default:
		;	// no subtypes
	}

	return false;
}


/*----------------------------------------------------------------------------------------------
	Return a pointer to the symbol indicated by the field, or NULL if it is not present in
	the table.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTable::FindField(StrAnsi staField)
{
	Symbol psymRet;
	if (m_hmstasymEntries.Retrieve(staField, &psymRet))
		return psymRet;
	else
		return NULL;
}

/*----------------------------------------------------------------------------------------------
	Return a pointer to the symbol indicated by the structured name, or NULL if the symbol
	is not present.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTable::FindSymbol(const GrcStructName & xns)
{
	GrcSymbolTable * psymtbl = this;
	Symbol psym = NULL;

	for (int i = 0; i < xns.NumFields(); ++i)
	{
		psym = psymtbl->FindField(xns.FieldAt(i));
		if (psym == NULL)
			return NULL;
		psymtbl = psym->m_psymtblSubTable;
		if (i < xns.NumFields() - 1 && psymtbl == NULL)
			return NULL;
	}
	return psym;
}

//	one-field version:
Symbol GrcSymbolTable::FindSymbol(const StrAnsi staName)
{
	return FindField(staName);
}


/*----------------------------------------------------------------------------------------------
	Return the symbol corresponding to the slot attribute name. Return NULL if it is not
	a legal slot attribute. If it is of the form component.???.reference, add it to the
	symbol table.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTable::FindSlotAttr(const GrcStructName & xns, GrpLineAndFile const& lnf)
{
	Symbol psym = FindSymbol(xns);

	if (!psym && xns.NumFields() == 3 &&
		xns.FieldEquals(0, "component") && xns.FieldEquals(2, "reference"))
	{
		psym = AddSymbolAux(xns, ksymtSlotAttrCompRef, ksymtInvalidGlyphAttr, lnf);
		psym->SetExpType(kexptSlotRef);
	}

	if (!psym || !psym->FitsSymbolType(ksymtSlotAttr))
		return NULL;
	else
		return psym;
}


/*----------------------------------------------------------------------------------------------
	Answer true if the field at the given index is the given string.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::FieldIs(int i, StrAnsi staField)
{
	Assert(i >= 0);

	if (i > Level())
		return false;

	Symbol psymCurr = this;
	while (psymCurr->Level() > i)
		psymCurr = psymCurr->m_psymtbl->m_psymParent;
	return (psymCurr->m_staFieldName == staField);
}

/*----------------------------------------------------------------------------------------------
	Answer the field at the given index, or an empty string if there are not
	that many fields.
----------------------------------------------------------------------------------------------*/
StrAnsi GrcSymbolTableEntry::FieldAt(int i)
{
	Assert(i >= 0);

	if (i > Level())
		return StrAnsi("");

	Symbol psymCurr = this;
	while (psymCurr->Level() > i)
		psymCurr = psymCurr->m_psymtbl->m_psymParent;
	return psymCurr->m_staFieldName;
}

/*----------------------------------------------------------------------------------------------
	Answer the index  of the (first) field containing the given string, or -1 if it is not
	present.
----------------------------------------------------------------------------------------------*/
int GrcSymbolTableEntry::FieldIndex(StrAnsi sta)
{
	int cRet = -1;
	Symbol psymCurr = this;
	while (psymCurr)
	{
		if (psymCurr->m_staFieldName == sta)
			cRet = psymCurr->Level();
		psymCurr = psymCurr->m_psymtbl->m_psymParent;
	}
	return cRet;
}

/*----------------------------------------------------------------------------------------------
	Answer the number of fields in the symbol.
----------------------------------------------------------------------------------------------*/
int	GrcSymbolTableEntry::FieldCount()
{
	return Level() + 1;
}


/*----------------------------------------------------------------------------------------------
	Answer true if the symbol is the given operator.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::MatchesOp(StrAnsi sta)
{
	if (!FitsSymbolType(ksymtOperator))
		return false;
	return (m_staFieldName == sta);
}


/*----------------------------------------------------------------------------------------------
	Answer true if the symbol is a comparative operator.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsComparativeOp()
{
	if (!FitsSymbolType(ksymtOperator))
		return false;
	return (m_prec == kprecComparative);
}


/*----------------------------------------------------------------------------------------------
	Answer true if the symbol is a bogus slot attribute that is only present in symbol table
	to assist in error checking: advance/shift/kern.gpoint/gpath/xoffset/yoffset.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsBogusSlotAttr()
{
	Assert(FitsSymbolType(ksymtSlotAttr));

	if (FieldCount() != 2)
		return false;

	if (m_staFieldName == "gpoint" || m_staFieldName == "gpath" ||
		m_staFieldName == "xoffset" || m_staFieldName == "yoffset")
	{
		StrAnsi sta = FieldAt(0);
		if (sta == "shift" || sta == "advance" || sta == "kern")
			return true;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Answer true if the symbol is a read-only slot attribute, ie, position, position.x, or
	position.y.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsReadOnlySlotAttr()
{
	Assert(FitsSymbolType(ksymtSlotAttr));

	return (FieldIs(0, "position"));
}


/*----------------------------------------------------------------------------------------------
	Answer true if the symbol is a write-only slot attribute, ie, kern, kern.x, or
	kern.y.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsWriteOnlySlotAttr()
{
	Assert(FitsSymbolType(ksymtSlotAttr));

	return (FieldIs(0, "kern"));
}


/*----------------------------------------------------------------------------------------------
	Answer true if the symbol is an indexed slot attribute, ie, component.XXX.reference or
	a user-definable slot attribute.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsIndexedSlotAttr()
{
	Assert(FitsSymbolType(ksymtSlotAttr));

	if (FieldIs(0, "component") && LastFieldIs("reference"))
		return true;
	else if (IsUserDefinableSlotAttr())
		return true;
	else
		return false;
}


/*----------------------------------------------------------------------------------------------
	Answer true if the symbol is an indexed glyph attribute, ie, component.XXX....
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsIndexedGlyphAttr()
{
	Assert(FitsSymbolType(ksymtGlyphAttr));

	if (IsGeneric())
		return (FieldIs(0, "component"));
	else
		return Generic()->IsIndexedGlyphAttr();
}


/*----------------------------------------------------------------------------------------------
	Return the full dotted name of the symbol.
----------------------------------------------------------------------------------------------*/
StrAnsi GrcSymbolTableEntry::FullName()
{
	StrAnsi staRet = m_staFieldName;
	GrcSymbolTableEntry * psymCurr = m_psymtbl->m_psymParent;
	while (psymCurr)
	{
		StrAnsi staTmp = psymCurr->m_staFieldName;
		staTmp += ".";
		staTmp += staRet;
		staRet = staTmp;
		psymCurr = psymCurr->m_psymtbl->m_psymParent;
	}
	return staRet;
}


/*----------------------------------------------------------------------------------------------
	Return the full dotted name of the symbol, using abbreviations.
----------------------------------------------------------------------------------------------*/
StrAnsi GrcSymbolTableEntry::FullAbbrev()
{
	StrAnsi staRet = Abbreviation(m_staFieldName);
	GrcSymbolTableEntry * psymCurr = m_psymtbl->m_psymParent;
	while (psymCurr)
	{
		StrAnsi staTmp = Abbreviation(psymCurr->m_staFieldName);
		staTmp += ".";
		staTmp += staRet;
		staRet = staTmp;
		psymCurr = psymCurr->m_psymtbl->m_psymParent;
	}
	return staRet;
}


/*----------------------------------------------------------------------------------------------
	Return the standard abbreviation for the keyword.
----------------------------------------------------------------------------------------------*/
StrAnsi GrcSymbolTableEntry::Abbreviation(StrAnsi staFieldName)
{
	if (staFieldName == "reference")
		return "ref";
	else if (staFieldName == "boundingbox")
		return "bb";
	else if (staFieldName == "advancewidth")
		return "aw";
	else if (staFieldName == "advanceheight")
		return "ah";
	else if (staFieldName == "leftsidebearing")
		return "lsb";
	else if (staFieldName == "rightsidebearing")
		return "rsb";
	else if (staFieldName == "directionality")
		return "dir";
	else if (staFieldName == "component")
		return "comp";
	else if (staFieldName == "breakweight")
		return "break";
	else
		return staFieldName;
}

/*----------------------------------------------------------------------------------------------
	Fill in the structured name with the name of the symbol.
----------------------------------------------------------------------------------------------*/
void GrcSymbolTableEntry::GetStructuredName(GrcStructName * pxns)
{
	GrcSymbolTableEntry * psymCurr = this;
	while (psymCurr)
	{
		pxns->InsertField(0, psymCurr->m_staFieldName);
		psymCurr = psymCurr->ParentSymbol();
	}
}

/*----------------------------------------------------------------------------------------------
	This symbol is a non-generic glyph attribute or feature setting. Return the symbol
	for the defining class or feature.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTableEntry::BaseDefnForNonGeneric()
{
	Assert(FitsSymbolType(ksymtGlyphAttr) ||
		FitsSymbolType(ksymtFeatSetting) ||
		FitsSymbolType(ksymtInvalid));

	Symbol psymParent = ParentSymbol();
	Assert(psymParent);
	if (psymParent->FitsSymbolType(ksymtClass))
		return psymParent;
	else if (psymParent->FitsSymbolType(ksymtFeature))
		return psymParent;
	else
		return psymParent->BaseDefnForNonGeneric();
}

/*----------------------------------------------------------------------------------------------
	Return the symbol for the defining class, or NULL if this symbol is something other
	than a non-generic glyph attribute.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTableEntry::BaseClassDefn()
{
	if (!FitsSymbolType(ksymtGlyphAttr))
		return NULL;

	Symbol psymParent = ParentSymbol();
	if (!psymParent)
		return NULL;

	if (psymParent->FitsSymbolType(ksymtClass))
		return psymParent;
	else
		return psymParent->BaseClassDefn();
}

/*----------------------------------------------------------------------------------------------
	This symbol is a feature setting value, eg someFeat.settings.opt1.id.
	Return the symbol for the feature setting someFeat.settings.opt1.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTableEntry::BaseFeatSetting()
{
	Assert(FitsSymbolType(ksymtFeatSetting));

	Symbol psymParent = ParentSymbol();
	Assert(psymParent);
	if (psymParent->m_staFieldName == StrAnsi("setting"))
		return this;
	else
		return psymParent->BaseFeatSetting();
}

/*----------------------------------------------------------------------------------------------
	This symbol is something like clsABC.component.A.top or component.A.ref.
	Return the symbol corresponding to the component itself: clsABC.component.A.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTableEntry::BaseLigComponent()
{
	Assert(FitsSymbolType(ksymtGlyphAttr) ||
		FitsSymbolType(ksymtSlotAttr) ||
		FitsSymbolType(ksymtInvalidGlyphAttr));

	Symbol psymParent = ParentSymbol();
	Assert(psymParent);
	if (psymParent->m_staFieldName == StrAnsi("component"))
		return this;
	else
		return psymParent->BaseLigComponent();
}

/*----------------------------------------------------------------------------------------------
	This symbol is something like clsABC.somePoint.x. Return the symbol corresponding to the
	point itself: clsABC.somePoint.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTableEntry::BasePoint()
{
	Assert(FitsSymbolType(ksymtGlyphAttr));
	Assert(m_staFieldName == "x" || m_staFieldName == "y" ||
		m_staFieldName == "gpoint" || m_staFieldName == "gpath" ||
		m_staFieldName == "xoffset" || m_staFieldName == "yoffset");

	return ParentSymbol();
}

/*----------------------------------------------------------------------------------------------
	This symbol is something like clsABC.somePoint.x. Return the symbol corresponding to a
	sister field, ie, clsABC.somePoint.y. If such a symbol has not been defined, return NULL.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTableEntry::PointSisterField(StrAnsi staField)
{
	Assert(m_staFieldName == "x" || m_staFieldName == "y" ||
		m_staFieldName == "gpoint" || m_staFieldName == "gpath" ||
		m_staFieldName == "xoffset" || m_staFieldName == "yoffset");

	Symbol psymBase = BasePoint();
	Assert(psymBase);
	Symbol psymRet = psymBase->m_psymtblSubTable->FindField(staField);
	return psymRet;
}

/*----------------------------------------------------------------------------------------------
	Return true if the symbol is of the form ...component.???.reference.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsComponentRef()
{
	Assert(FitsSymbolType(ksymtSlotAttr));
	Symbol psymParent = ParentSymbol();
	return (m_staFieldName == "reference" &&
		psymParent && psymParent->ParentSymbol() &&
		psymParent->ParentSymbol()->m_staFieldName == "component");
}

/*----------------------------------------------------------------------------------------------
	Return true if the symbol is of the form ...component.???.top/bottom/left/right
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsComponentBoxField()
{
	Symbol psymParent = ParentSymbol();
	return (psymParent && psymParent->ParentSymbol() &&
		psymParent->ParentSymbol()->m_staFieldName == "component" &&
		(m_staFieldName == "top" || m_staFieldName == "bottom" ||
			m_staFieldName == "left" || m_staFieldName == "right"));
}

/*----------------------------------------------------------------------------------------------
	Return true if the symbol is of the form ...component.???.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsComponentBase()
{
	return (ParentSymbol() && ParentSymbol()->m_staFieldName == "component");
}

/*----------------------------------------------------------------------------------------------
	Return true if the symbol is of the form "attach.to"
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsAttachTo()
{
	Assert(FitsSymbolType(ksymtSlotAttr));
	Symbol psymParent = ParentSymbol();
	return (psymParent &&
		psymParent->m_staFieldName == "attach" && m_staFieldName == "to");
}

/*----------------------------------------------------------------------------------------------
	Return true if the symbol is of the form "attach.at.???"
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsAttachAtField()
{
	Assert(FitsSymbolType(ksymtSlotAttr));
	Symbol psymParent = ParentSymbol();
	return (psymParent && psymParent->ParentSymbol() &&
		psymParent->ParentSymbol()->m_staFieldName == "attach" &&
		psymParent->m_staFieldName == "at");
}

/*----------------------------------------------------------------------------------------------
	Return true if the symbol is of the form "attach.with.???"
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsAttachWithField()
{
	Assert(FitsSymbolType(ksymtSlotAttr));
	Symbol psymParent = ParentSymbol();
	return (psymParent && psymParent->ParentSymbol() &&
		psymParent->ParentSymbol()->m_staFieldName == "attach" &&
		psymParent->m_staFieldName == "with");
}

/*----------------------------------------------------------------------------------------------
	Return true if the symbol is an attachment attribute.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsAttachment()
{
	if (m_staFieldName == "attach")
		return true;
	Symbol psymParent = ParentSymbol();
	if (!psymParent)
		return false;
	return (psymParent->IsAttachment());
}

/*----------------------------------------------------------------------------------------------
	Return true if the symbol is an movement attribute: shift, advance, kern.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsMovement()
{
	if (m_staFieldName == "shift")
		return true;
	if (m_staFieldName == "kern")
		return true;
	if (m_staFieldName == "advance")
		return true;
	Symbol psymParent = ParentSymbol();
	if (!psymParent)
		return false;
	return (psymParent->IsMovement());
}

/*----------------------------------------------------------------------------------------------
	Return true if the symbol is a justification-related attribute.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::DoesJustification()
{
	if (m_staFieldName == "justify")
		return true;
	Symbol psymParent = ParentSymbol();
	if (!psymParent)
		return false;
	return (psymParent->DoesJustification());
}

/*----------------------------------------------------------------------------------------------
	Return true if the symbol is a justification-related attribute.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsMeasureAttr()
{
	if (m_staFieldName == "measure")
		return true;
	Symbol psymParent = ParentSymbol();
	if (!psymParent)
		return false;
	return (psymParent->IsMeasureAttr());
}

/*----------------------------------------------------------------------------------------------
	Return true if the symbol is a user-definable slot attribute.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::IsUserDefinableSlotAttr()
{
	if (m_staFieldName[0] == 'u' && m_staFieldName[1] == 's' &&
		m_staFieldName[2] == 'e' && m_staFieldName[3] == 'r')
	{
		return true;
	}
	else
		return false;
}

/*----------------------------------------------------------------------------------------------
	Return the number corresponding to the index of the user-definable slot attribute,
	or -1 if it is something invalid (does not parse to a number).
----------------------------------------------------------------------------------------------*/
int GrcSymbolTableEntry::UserDefinableSlotAttrIndex()
{
	Assert(IsUserDefinableSlotAttr());
	int nRet = 0;
	for (int ich = 4; ich < m_staFieldName.Length(); ich++)
	{
		char ch = m_staFieldName[ich];
		if (ch < '0')
			return -1;
		if (ch > '9')
			return -1;
		nRet = (nRet * 10) + (ch - '0');
	}
	return nRet - 1;	// 0-based
}

/*----------------------------------------------------------------------------------------------
	For slot attributes that use points (eg, attach.at, shift), or for glyph attributes
	that define a point (eg, udap) return the sub-symbol that appends the given point field,
	or NULL if such a field does not exist.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTableEntry::SubField(StrAnsi sta)
{
	Assert(FitsSymbolType(ksymtSlotAttrPt) || FitsSymbolType(ksymtGlyphAttr) ||
		FitsSymbolType(ksymtInvalidGlyphAttr)) ;
	Assert(m_psymtblSubTable);
	return m_psymtblSubTable->FindField(sta);
}

/*----------------------------------------------------------------------------------------------
	Return the class that is stored as data, or NULL.
----------------------------------------------------------------------------------------------*/
GdlGlyphClassDefn * GrcSymbolTableEntry::GlyphClassDefnData()
{
	if (!HasData())
		return NULL;

	GdlGlyphClassDefn * pglfc = dynamic_cast<GdlGlyphClassDefn *>(Data());
	return pglfc;
}

/*----------------------------------------------------------------------------------------------
	Return the feature that is stored as data, or NULL.
----------------------------------------------------------------------------------------------*/
GdlFeatureDefn * GrcSymbolTableEntry::FeatureDefnData()
{
	if (!HasData())
		return NULL;

	GdlFeatureDefn * pfeat = dynamic_cast<GdlFeatureDefn *>(Data());
	return pfeat;
}

/*----------------------------------------------------------------------------------------------
	Return the language-map that is stored as data, or NULL.
----------------------------------------------------------------------------------------------*/
GdlLanguageDefn * GrcSymbolTableEntry::LanguageDefnData()
{
	if (!HasData())
		return NULL;

	GdlLanguageDefn * plang = dynamic_cast<GdlLanguageDefn *>(Data());
	return plang;
}

/*----------------------------------------------------------------------------------------------
	Return the level of the justification symbol. Return -2 if this is not such a symbol;
	-1 if this has no level associated (eg, justify.stretch).
----------------------------------------------------------------------------------------------*/
int GrcSymbolTableEntry::JustificationLevel()
{
	if (!DoesJustification())
		return -2;

	StrAnsi sta;
	int nLevel = 0;
	do {
		sta = FieldAt(nLevel);
		nLevel++;
	} while (sta != "justify");
	sta = FieldAt(nLevel);

	if (sta == "0")
		return 0;
	else if (sta == "1")
		return 1;
	else if (sta == "2")
		return 2;
	else if (sta == "3")
		return 3;
	else if (sta == "stretch" || sta == "stretchHW" || sta == "shrink" || sta == "step"
		|| sta == "weight" || sta == "width")
	{
		return -1; // no level specified
	}
	else
		return 4; // some invalid level
}

/*----------------------------------------------------------------------------------------------
	Adjust the expression type to be something more specific, if possible. If the new
	expression type does not fit within the constraints of the old, return false.
----------------------------------------------------------------------------------------------*/
bool GrcSymbolTableEntry::AdjustExpTypeIfPossible(ExpressionType expt)
{
	if (m_expt == kexptUnknown)
	{
		m_expt = expt;
		return true;
	}
	if (m_expt == kexptZero && (expt == kexptNumber || expt == kexptMeas || expt == kexptBoolean))
	{
		m_expt = expt;
		return true;
	}
	if (m_expt == kexptOne && (expt == kexptNumber || expt == kexptBoolean))
	{
		m_expt = expt;
		return true;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	For slot attributes, return the corresponding engine code operator.
----------------------------------------------------------------------------------------------*/
int GrcSymbolTableEntry::SlotAttrEngineCodeOp()
{
	Assert(FitsSymbolType(ksymtSlotAttr)) ;

	StrAnsi staField0 = FieldAt(0);
	StrAnsi staField1 = FieldAt(1);
	StrAnsi staField2 = FieldAt(2);

	if (staField0 == "advance")
	{
		if (staField1 == "x")
			return kslatAdvX;
		else if (staField1 == "y")
			return kslatAdvY;
		else
			Assert(false);
	}
	else if (staField0 == "attach")
	{
		if (staField1 == "to")
			return kslatAttTo;

		else if (staField1 == "at")
		{
			if (staField2 == "x")
				return kslatAttAtX;
			else if (staField2 == "y")
				return kslatAttAtY;
			else if (staField2 == "gpoint")
				return kslatAttAtGpt;
			else if (staField2 == "xoffset")
				return kslatAttAtXoff;
			else if (staField2 == "yoffset")
				return kslatAttAtYoff;
			else
				Assert(false);
		}
		else if (staField1 == "with")
		{
			if (staField2 == "x")
				return kslatAttWithX;
			else if (staField2 == "y")
				return kslatAttWithY;
			else if (staField2 == "gpoint")
				return kslatAttWithGpt;
			else if (staField2 == "xoffset")
				return kslatAttWithXoff;
			else if (staField2 == "yoffset")
				return kslatAttWithYoff;
			else
				Assert(false);
		}

		else if (staField1 == "level")
			return kslatAttLevel;

		else
			Assert(false);
	}
	else if (staField0 == "breakweight")
		return kslatBreak;

	else if (staField0 == "component")
	{
		Assert(staField2 == "reference");
		return kslatCompRef;
	}

	else if (staField0 == "directionality")
		return kslatDir;

	else if (staField0 == "insert")
		return kslatInsert;

	else if (staField0 == "position")
	{
		if (staField1 == "x")
			return kslatPosX;
		else if (staField1 == "y")
			return kslatPosY;
		else
			Assert(false);
	}

	else if (staField0 == "shift")
	{
		if (staField1 == "x")
			return kslatShiftX;
		else if (staField1 == "y")
			return kslatShiftY;
		else
			Assert(false);
	}

	else if (staField0 == "measure")
	{
		if (staField1 == "startofline")
			return kslatMeasureSol;
		else if (staField1 == "endofline")
			return kslatMeasureEol;
		else
			Assert(false);
	}

	else if (staField0 == "justify")	// TODO: handle all the levels
	{
		if (staField1 == "stretch")
			return kslatJStretch;
		else if (staField1 == "shrink")
			return kslatJShrink;
		else if (staField1 == "step")
			return kslatJStep;
		else if (staField1 == "weight")
			return kslatJWeight;
		else if (staField1 == "width")
			return kslatJWidth;
		else
			Assert(false);
	}

	else if (IsUserDefinableSlotAttr())
	{
		return kslatUserDefn;
	}

	else if (staField0[0] == 'u' && staField0[1] == 's' &&
		staField0[2] == 'e' && staField0[3] == 'r')
	{
		Assert(false);
		return kslatUserDefn;
	}

	else
		Assert(false);

	return -1;
}


/*----------------------------------------------------------------------------------------------
	For glyph metrics, return the corresponding engine code operator.
----------------------------------------------------------------------------------------------*/
int GrcSymbolTableEntry::GlyphMetricEngineCodeOp()
{
	if (!m_fGeneric && m_psymGeneric)
		return m_psymGeneric->GlyphMetricEngineCodeOp();

	Assert(FitsSymbolType(ksymtGlyphMetric)) ;

	StrAnsi staField0 = FieldAt(0);
	StrAnsi staField1 = FieldAt(1);
	StrAnsi staField2 = FieldAt(2);

	if (staField0 == "leftsidebearing")
	{
		return kgmetLsb;
	}
	else if (staField0 == "rightsidebearing")
	{
		return kgmetRsb;
	}
	else if (staField0 == "boundingbox")
	{
		if (staField1 == "top")
			return kgmetBbTop;
		else if (staField1 == "bottom")
			return kgmetBbBottom;
		else if (staField1 == "left")
			return kgmetBbLeft;
		else if (staField1 == "right")
			return kgmetBbRight;
		else if (staField1 == "width")
			return kgmetBbWidth;
		else if (staField1 == "height")
			return kgmetBbHeight;
		else
			Assert(false);
	}
	else if (staField0 == "advancewidth")
	{
		return kgmetAdvWidth;
	}
	else if (staField0 == "advanceheight")
	{
		return kgmetAdvHeight;
	}
//	else if (staField0 == "advance")
//	{
//		if (staField1 == "width")
//			return kgmetAdvWidth;
//		else if (staField1 == "height")
//			return kgmetAdvHeight;
//		else
//			Assert(false);
//	}
	else if (staField0 == "ascent")
	{
		return kgmetAscent;
	}
	else if (staField0 == "descent")
	{
		return kgmetDescent;
	}
	else
		Assert(false);

	return -1;
}


/*----------------------------------------------------------------------------------------------
	Pre-define a system-defined symbol.
----------------------------------------------------------------------------------------------*/
Symbol GrcSymbolTable::PreDefineSymbol(const GrcStructName& xns, SymbolType symt,
	ExpressionType expt, OpPrec prec)
{
	Symbol psym = AddSymbolAux(xns, symt, ksymtInvalid, GrpLineAndFile());

	psym->m_expt = expt;
	psym->m_fUserDefined = false;
	psym->m_prec = prec;

	return psym;
}


/*----------------------------------------------------------------------------------------------
	Symbol table initialization
----------------------------------------------------------------------------------------------*/
void GrcSymbolTable::InitWithPreDefined()
{
	InitGlobals();
	InitDirectives();
	InitFeatureSettings();
	InitGlyphMetrics();
	InitOperators();
	InitSlotAttrs();
	InitGlyphAttrs();
	InitSpecial();
	InitTableTypes();
	InitUnits();
	InitProcStates();

	PreDefineSymbol(GrcStructName(GdlGlyphClassDefn::Undefined()), ksymtClass);
}

/*--------------------------------------------------------------------------------------------*/
void GrcSymbolTable::InitGlobals()
{
	SymbolType kst = ksymtGlobal;

	PreDefineSymbol(GrcStructName("AutoPseudo"),			kst, kexptBoolean);
	PreDefineSymbol(GrcStructName("Bidi"),					kst, kexptBoolean);
	PreDefineSymbol(GrcStructName("ExtraAscent"),			kst, kexptMeas);
	PreDefineSymbol(GrcStructName("ExtraDescent"),			kst, kexptMeas);
	PreDefineSymbol(GrcStructName("ScriptDirection"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("ScriptDirections"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("ScriptTags"),			kst, kexptString);
	PreDefineSymbol(GrcStructName("ScriptTag"),				kst, kexptString);
}

/*--------------------------------------------------------------------------------------------*/
void GrcSymbolTable::InitDirectives()
{
	SymbolType kst = ksymtDirective;

	PreDefineSymbol(GrcStructName("AttributeOverride"),		kst, kexptBoolean);
	PreDefineSymbol(GrcStructName("CodePage"),				kst, kexptNumber);
	PreDefineSymbol(GrcStructName("MaxBackup"),				kst, kexptNumber);
	PreDefineSymbol(GrcStructName("MaxRuleLoop"),			kst, kexptNumber);
	PreDefineSymbol(GrcStructName("MUnits"),				kst, kexptMeas);
	PreDefineSymbol(GrcStructName("PointRadius"),			kst, kexptMeas);
}

/*--------------------------------------------------------------------------------------------*/
void GrcSymbolTable::InitFeatureSettings()
{
	SymbolType kst = ksymtFeatSetting;

	PreDefineSymbol(GrcStructName("id"),				kst, kexptNumber);
	PreDefineSymbol(GrcStructName("default"),			kst, kexptNumber);
	PreDefineSymbol(GrcStructName("settings"),			kst, kexptUnknown);
}

/*--------------------------------------------------------------------------------------------*/
void GrcSymbolTable::InitGlyphMetrics()
{
	SymbolType kst = ksymtGlyphMetric;

	Symbol psym;
	psym = PreDefineSymbol(GrcStructName("boundingbox", "top"),	kst, kexptMeas);
	psym->m_fGeneric = true;
	psym = PreDefineSymbol(GrcStructName("boundingbox", "bottom"),	kst, kexptMeas);
	psym->m_fGeneric = true;
	psym = PreDefineSymbol(GrcStructName("boundingbox", "left"),	kst, kexptMeas);
	psym->m_fGeneric = true;
	psym = PreDefineSymbol(GrcStructName("boundingbox", "right"),	kst, kexptMeas);
	psym->m_fGeneric = true;
	psym = PreDefineSymbol(GrcStructName("boundingbox", "height"),	kst, kexptMeas);
	psym->m_fGeneric = true;
	psym = PreDefineSymbol(GrcStructName("boundingbox", "width"),	kst, kexptMeas);
	psym->m_fGeneric = true;

	psym = PreDefineSymbol(GrcStructName("advanceheight"),		kst, kexptMeas);
	psym->m_fGeneric = true;
	psym = PreDefineSymbol(GrcStructName("advancewidth"),		kst, kexptMeas);
	psym->m_fGeneric = true;

	psym = PreDefineSymbol(GrcStructName("leftsidebearing"),	kst, kexptMeas);
	psym->m_fGeneric = true;
	psym = PreDefineSymbol(GrcStructName("rightsidebearing"),	kst, kexptMeas);	// = munits - lsb
	psym->m_fGeneric = true;

	psym = PreDefineSymbol(GrcStructName("ascent"),	kst, kexptMeas);
	psym->m_fGeneric = true;
	psym = PreDefineSymbol(GrcStructName("descent"),	kst, kexptMeas);	// = font height - ascent
	psym->m_fGeneric = true;
	psym = PreDefineSymbol(GrcStructName("munits"),	kst, kexptNumber);
	psym->m_fGeneric = true;
}

/*--------------------------------------------------------------------------------------------*/
void GrcSymbolTable::InitOperators()
{
	SymbolType kst = ksymtOperator;
	ExpressionType kexpt = kexptUnknown;

	OpPrec kprec = GrcSymbolTableEntry::kprecFunctional;
	PreDefineSymbol(GrcStructName("min"),	kst, kexpt, kprec);
	PreDefineSymbol(GrcStructName("max"),	kst, kexpt, kprec);

	kprec = GrcSymbolTableEntry::kprecAssignment;
	PreDefineSymbol(GrcStructName("="),		ksymtOpAssign, kexpt, kprec);
	PreDefineSymbol(GrcStructName("+="),	ksymtOpAssign, kexpt, kprec);
	PreDefineSymbol(GrcStructName("-="),	ksymtOpAssign, kexpt, kprec);
	PreDefineSymbol(GrcStructName("*="),	ksymtOpAssign, kexpt, kprec);
	PreDefineSymbol(GrcStructName("/="),	ksymtOpAssign, kexpt, kprec);

	kprec = GrcSymbolTableEntry::kprecConditional;
	PreDefineSymbol(GrcStructName("?"),		kst, kexpt, kprec);

	kprec = GrcSymbolTableEntry::kprecLogical;
	PreDefineSymbol(GrcStructName("||"),	kst, kexpt, kprec);
	PreDefineSymbol(GrcStructName("&&"),	kst, kexpt, kprec);
	PreDefineSymbol(GrcStructName("!"),		kst, kexpt, kprec);

	kprec = GrcSymbolTableEntry::kprecComparative;
	PreDefineSymbol(GrcStructName("=="),	kst, kexpt, kprec);
	PreDefineSymbol(GrcStructName("!="),	kst, kexpt, kprec);
	PreDefineSymbol(GrcStructName("<"),		kst, kexpt, kprec);
	PreDefineSymbol(GrcStructName("<="),	kst, kexpt, kprec);
	PreDefineSymbol(GrcStructName(">"),		kst, kexpt, kprec);
	PreDefineSymbol(GrcStructName(">="),	kst, kexpt, kprec);

	kprec = GrcSymbolTableEntry::kprecAdditive;
	PreDefineSymbol(GrcStructName("+"),		kst, kexpt, kprec);
	PreDefineSymbol(GrcStructName("-"),		kst, kexpt, kprec);

	kprec = GrcSymbolTableEntry::kprecMultiplicative;
	PreDefineSymbol(GrcStructName("*"),		kst, kexpt, kprec);
	PreDefineSymbol(GrcStructName("/"),		kst, kexpt, kprec);
}

/*--------------------------------------------------------------------------------------------*/
void GrcSymbolTable::InitSlotAttrs()
{
	SymbolType kst = ksymtSlotAttr;
	SymbolType kstPt = ksymtSlotAttrPt;

	PreDefineSymbol(GrcStructName("attach", "to"),				kst, kexptSlotRef);
	PreDefineSymbol(GrcStructName("attach", "level"),			kst, kexptNumber);

	PreDefineSymbol(GrcStructName("attach", "at"),				kstPt, kexptPoint);
	PreDefineSymbol(GrcStructName("attach", "at", "x"),			kst, kexptMeas);
	PreDefineSymbol(GrcStructName("attach", "at", "y"),			kst, kexptMeas);
	PreDefineSymbol(GrcStructName("attach", "at", "gpath"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("attach", "at", "gpoint"),	kst, kexptNumber);
	PreDefineSymbol(GrcStructName("attach", "at", "xoffset"),	kst, kexptMeas);
	PreDefineSymbol(GrcStructName("attach", "at", "yoffset"),	kst, kexptMeas);

	PreDefineSymbol(GrcStructName("attach", "with"),			kstPt, kexptPoint);
	PreDefineSymbol(GrcStructName("attach", "with", "x"),		kst, kexptMeas);
	PreDefineSymbol(GrcStructName("attach", "with", "y"),		kst, kexptMeas);
	PreDefineSymbol(GrcStructName("attach", "with", "gpath"),	kst, kexptNumber);
	PreDefineSymbol(GrcStructName("attach", "with", "gpoint"),	kst, kexptNumber);
	PreDefineSymbol(GrcStructName("attach", "with", "xoffset"),	kst, kexptMeas);
	PreDefineSymbol(GrcStructName("attach", "with", "yoffset"),	kst, kexptMeas);

	PreDefineSymbol(GrcStructName("breakweight"),	kst, kexptNumber);

	PreDefineSymbol(GrcStructName("component"),		kst);
	//	Specific component.?.reference attributes are added as component glyph attributes
	//	are defined, or as encountered.

	PreDefineSymbol(GrcStructName("directionality"),kst, kexptNumber);

	PreDefineSymbol(GrcStructName("user1"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user2"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user3"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user4"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user5"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user6"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user7"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user8"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user9"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user10"),	kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user11"),	kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user12"),	kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user13"),	kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user14"),	kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user15"),	kst, kexptNumber);
	PreDefineSymbol(GrcStructName("user16"),	kst, kexptNumber);

	PreDefineSymbol(GrcStructName("insert"),		kst, kexptBoolean);

	PreDefineSymbol(GrcStructName("advance"),		kstPt,	kexptPoint);
	PreDefineSymbol(GrcStructName("advance", "x"),	kst,	kexptMeas);
	PreDefineSymbol(GrcStructName("advance", "y"),	kst,	kexptMeas);
	//	bogus attributes for error detection:
	PreDefineSymbol(GrcStructName("advance", "gpath"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("advance", "gpoint"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("advance", "xoffset"),	kst, kexptMeas);
	PreDefineSymbol(GrcStructName("advance", "yoffset"),	kst, kexptMeas);

	PreDefineSymbol(GrcStructName("kern"),				kstPt,	kexptPoint);
	PreDefineSymbol(GrcStructName("kern", "x"),			kst,	kexptMeas);
	PreDefineSymbol(GrcStructName("kern", "y"),			kst,	kexptMeas);
	//	bogus attributes for error detection:
	PreDefineSymbol(GrcStructName("kern", "gpath"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("kern", "gpoint"),	kst, kexptNumber);
	PreDefineSymbol(GrcStructName("kern", "xoffset"),	kst, kexptMeas);
	PreDefineSymbol(GrcStructName("kern", "yoffset"),	kst, kexptMeas);

	PreDefineSymbol(GrcStructName("position"),			kstPt,	kexptPoint);
	PreDefineSymbol(GrcStructName("position", "x"),		kst,	kexptMeas);
	PreDefineSymbol(GrcStructName("position", "y"),		kst,	kexptMeas);

	PreDefineSymbol(GrcStructName("shift"),				kstPt,	kexptPoint);
	PreDefineSymbol(GrcStructName("shift", "x"),		kst,	kexptMeas);
	PreDefineSymbol(GrcStructName("shift", "y"),		kst,	kexptMeas);
	//	bogus attributes for error detection:
	PreDefineSymbol(GrcStructName("shift", "gpath"),	kst, kexptNumber);
	PreDefineSymbol(GrcStructName("shift", "gpoint"),	kst, kexptNumber);
	PreDefineSymbol(GrcStructName("shift", "xoffset"),	kst, kexptMeas);
	PreDefineSymbol(GrcStructName("shift", "yoffset"),	kst, kexptMeas);

	PreDefineSymbol(GrcStructName("measure", "startofline"),kst, kexptMeas);
	PreDefineSymbol(GrcStructName("measure", "endofline"),	kst, kexptMeas);

	//	TODO: handle all the levels.
	PreDefineSymbol(GrcStructName("justify", "stretch"),	kst, kexptMeas);
	PreDefineSymbol(GrcStructName("justify", "stretchHW"),	kst, kexptMeas);
	PreDefineSymbol(GrcStructName("justify", "shrink"),		kst, kexptMeas);
	PreDefineSymbol(GrcStructName("justify", "step"),		kst, kexptMeas);
	PreDefineSymbol(GrcStructName("justify", "weight"),		kst, kexptNumber);
	PreDefineSymbol(GrcStructName("justify", "width"),		kst, kexptMeas);
}

/*--------------------------------------------------------------------------------------------*/
void GrcSymbolTable::InitGlyphAttrs()
{
	SymbolType kst = ksymtGlyphAttr;

	//	These were first recorded as slot attributes:

	Symbol psym;
	psym = AddType2(GrcStructName("component"), kst);
	psym->m_fGeneric = true;
	psym = AddType2(GrcStructName("directionality"), kst);
	psym->m_fGeneric = true;
	psym = AddType2(GrcStructName("breakweight"), kst);
	psym->m_fGeneric = true;

	//	TODO: handle all the levels
	psym = AddType2(GrcStructName("justify", "stretch"), kst);
	psym->m_fGeneric = true;
	psym = AddType2(GrcStructName("justify", "shrink"), kst);
	psym->m_fGeneric = true;
	psym = AddType2(GrcStructName("justify", "step"), kst);
	psym->m_fGeneric = true;
	psym = AddType2(GrcStructName("justify", "weight"), kst);
	psym->m_fGeneric = true;

	//	A fake glyph attribute that is used to store the actual glyph ID for pseudo-glyphs.
	psym = PreDefineSymbol(GrcStructName("*actualForPseudo*"), kst, kexptNumber);
	psym->m_fGeneric = true;
}

/*--------------------------------------------------------------------------------------------*/
void GrcSymbolTable::InitSpecial()
{
	SymbolType kst = ksymtSpecial;

	PreDefineSymbol(GrcStructName("@"),	ksymtSpecialAt);
	PreDefineSymbol(GrcStructName("^"),	ksymtSpecialCaret);
	PreDefineSymbol(GrcStructName("#"),	ksymtSpecialLb);
	PreDefineSymbol(GrcStructName("_"), ksymtSpecialUnderscore);

	AddType2(GrcStructName("?"), kst);
}

/*--------------------------------------------------------------------------------------------*/
void GrcSymbolTable::InitTableTypes()
{
	SymbolType kst = ksymtTableRule;

	PreDefineSymbol(GrcStructName("linebreak"),			kst);
	PreDefineSymbol(GrcStructName("substitution"),		kst);
	PreDefineSymbol(GrcStructName("justification"),		kst);
	PreDefineSymbol(GrcStructName("positioning"),		kst);

	kst = ksymtTable;

	PreDefineSymbol(GrcStructName("feature"),			kst);
	PreDefineSymbol(GrcStructName("glyph"),				kst);
	PreDefineSymbol(GrcStructName("name"),				kst);
}

/*--------------------------------------------------------------------------------------------*/
void GrcSymbolTable::InitUnits()
{
	PreDefineSymbol(GrcStructName("m"),	ksymtUnit);
}

/*--------------------------------------------------------------------------------------------*/
void GrcSymbolTable::InitProcStates()
{
	SymbolType kst = ksymtProcState;

	PreDefineSymbol(GrcStructName("JustifyMode"),	kst, kexptNumber);
	//PreDefineSymbol(GrcStructName("JustifyLevel"),	kst, kexptNumber);
}
