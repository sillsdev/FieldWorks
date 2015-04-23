/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GdlGlyphClass.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Definitions of classes of glyphs.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef CLASSES_INCLUDED
#define CLASSES_INCLUDED

class GdlGlyphDefn;

/*----------------------------------------------------------------------------------------------
Class: GdlGlyphAttrSetting
Description: The setting of a glyph attribute--attribute name and expression indicating
	the value.
Hungarian: glfa
----------------------------------------------------------------------------------------------*/

class GdlGlyphAttrSetting : public GdlObject
{
	friend class GdlGlyphClassDefn;

public:
	//	Constructors & destructors:
	GdlGlyphAttrSetting(Symbol psym, GdlAssignment * pasgn)
		:	m_psym(psym),
			m_pasgn(pasgn)
	{
		SetLineAndFile(pasgn->LineAndFile());
	}

	~GdlGlyphAttrSetting()
	{
		Assert(m_pasgn);
		delete m_pasgn;
	}

	Symbol GlyphSymbol()				{ return m_psym; }
	GdlAssignment * Assignment()		{ return m_pasgn; }
	GdlExpression * Expression()		{ return m_pasgn->Expression(); }

protected:
	//	Instance variables:
	Symbol			m_psym;
	GdlAssignment * m_pasgn;
};


/*----------------------------------------------------------------------------------------------
Class: GdlGlyphClassMember
Description: Abstract class subsuming GdlGlyphClassDefn and GdlGlyphDefn, ie, an element
	of a class.
Hungarian: glfd
----------------------------------------------------------------------------------------------*/
class GdlGlyphClassMember : public GdlDefn
{
public:
	virtual ~GdlGlyphClassMember()
	{
	};

	//	Pre-compiler:
	virtual void ExplicitPseudos(Set<GdlGlyphDefn *> & setpglf) = 0;
	virtual int ActualForPseudo(utf16 wPseudo) = 0;
	virtual int GlyphIDCount() = 0;
	virtual unsigned int FirstGlyphInClass(bool * pfMoreThanOne) = 0;
	virtual void AssignGlyphIDsToClassMember(GrcFont *, utf16 wGlyphIDLim,
		HashMap<utf16, utf16> & hmActualForPseudo,
		bool fLookUpPseudo = true) = 0;
	virtual void AssignGlyphAttrsToClassMembers(GrcGlyphAttrMatrix * pgax,
		GdlRenderer * prndr, GrcLigComponentList * plclist,
		Vector<GdlGlyphAttrSetting *> & vpglfaAttrs) = 0;
	virtual void CheckExistenceOfGlyphAttr(GdlObject * pgdlAvsOrExp,
		GrcSymbolTable * psymtbl, GrcGlyphAttrMatrix * pgax, Symbol psymGlyphAttr) = 0;
	virtual void CheckCompleteAttachmentPoint(GdlObject * pgdlAvsOrExp,
		GrcSymbolTable * psymtbl, GrcGlyphAttrMatrix * pgax, Symbol psymGlyphAttr,
		bool * pfXY, bool * pfGpoint) = 0;
	virtual void CheckCompBox(GdlObject * pritset,
		GrcSymbolTable * psymtbl, GrcGlyphAttrMatrix * pgax, Symbol psymCompRef) = 0;
	virtual void StorePseudoToActualAsGlyphAttr(GrcGlyphAttrMatrix * pgax, int nAttrID,
		Vector<GdlExpression *> & vpexpExtra) = 0;
	virtual bool IncludesGlyph(utf16) = 0;
	virtual bool HasOverlapWith(GdlGlyphClassMember * glfd, GrcFont * pfont) = 0;
	virtual bool HasBadGlyph() = 0;
	virtual bool WarnAboutBadGlyphs(bool fTop) = 0;
	virtual bool DeleteBadGlyphs() = 0;

	//	Compiler:
	virtual void RecordInclusionInClass(GdlPass * ppass, GdlGlyphClassDefn * pglfc) = 0;
	virtual void GetMachineClasses(FsmMachineClass ** ppfsmcAssignments,
		Set<FsmMachineClass *> & setpfsmc) = 0;

	//	Output:
	virtual void AddGlyphsToUnsortedList(Vector<utf16> & vwGlyphs) = 0;
	virtual void AddGlyphsToSortedList(Vector<utf16> & vwGlyphs, Vector<int> & vnIndices) = 0;

	//	debuggers
	virtual void DebugCmapForMember(GrcFont * pfont,
		utf16 * rgchwUniToGlyphID, unsigned int * rgnGlyphIDToUni) = 0;
};


/*----------------------------------------------------------------------------------------------
Class: GdlGlyphClassDefn
Description: A class of glyphs and glyph attribute settings.
Hungarian: glfc
----------------------------------------------------------------------------------------------*/
class GdlGlyphClassDefn : public GdlGlyphClassMember
{
	friend class GdlGlyphDefn;

public:
	//	Constructors & destructors:
	GdlGlyphClassDefn()
	{
		m_fReplcmtIn = false;
		m_fReplcmtOut = false;
		m_nReplcmtInID = -1;
		m_nReplcmtOutID = -1;
	}

	~GdlGlyphClassDefn();

	void DeleteGlyphDefns();

	//	Getters:
	StrAnsi Name()		{ return m_staName; }

	//	Setters:
	void SetName(StrAnsi sta)		{ m_staName = sta; }

	void AddMember(GdlGlyphClassMember * pglfd);

	void AddGlyphAttr(Symbol, GdlAssignment * pasgn);
	void AddComponent(Symbol, GdlAssignment * pasgn);

	static StrAnsi Undefined()
	{
		return "*GCUndefined*";
	}

	//	Parser:
	GdlGlyphClassMember * AddGlyphToClass(GrpLineAndFile const& lnf,
		GlyphType glft, int nFirst);
	GdlGlyphClassMember * AddGlyphToClass(GrpLineAndFile const& lnf,
		GlyphType glft, int nFirst, int nLast);
	GdlGlyphClassMember * AddGlyphToClass(GrpLineAndFile const& lnf,
		GlyphType glft, int nFirst, int nLast, utf16 wCodePage);
	GdlGlyphClassMember * AddGlyphToClass(GrpLineAndFile const& lnf,
		GlyphType glft, StrAnsi staPostscript);
	GdlGlyphClassMember * AddGlyphToClass(GrpLineAndFile const& lnf,
		GlyphType glft, StrAnsi staCodepoints, utf16 wCodePage);
	GdlGlyphClassMember * AddGlyphToClass(GrpLineAndFile const& lnf,
		GlyphType glft, GdlGlyphDefn * pglfOutput, utf16 nPseudoInput);
	GdlGlyphClassMember * AddGlyphToClass(GrpLineAndFile const& lnf,
		GlyphType glft, GdlGlyphDefn * pglfOutput);
	GdlGlyphClassMember * AddClassToClass(GrpLineAndFile const& lnf,
		GdlGlyphClassDefn * pglfcMember);

	//	Pre-compiler:
	virtual void ExplicitPseudos(Set<GdlGlyphDefn *> & setpglf);
	virtual int ActualForPseudo(utf16 wPseudo);
	void AssignGlyphIDs(GrcFont *, utf16 wGlyphIDLim,
		HashMap<utf16, utf16> & hmActualForPseudos);
	virtual void AssignGlyphIDsToClassMember(GrcFont *, utf16 wGlyphIDLim,
		HashMap<utf16, utf16> & hmActualForPseudo,
		bool fLookUpPseudo = true);
	virtual int GlyphIDCount();
	void MaxJustificationLevel(int * pnJLevel);
	virtual unsigned int FirstGlyphInClass(bool * pfMoreThanOne);
	void AssignGlyphAttrsToClassMembers(GrcGlyphAttrMatrix * pgax,
		GdlRenderer * prndr, GrcLigComponentList * plclist);
	virtual void AssignGlyphAttrsToClassMembers(GrcGlyphAttrMatrix * pgax,
		GdlRenderer * prndr, GrcLigComponentList * plclist,
		Vector<GdlGlyphAttrSetting *> & vpglfaAttrs);
	virtual void CheckExistenceOfGlyphAttr(GdlObject * pgdlAvsOrExp,
		GrcSymbolTable * psymtbl, GrcGlyphAttrMatrix * pgax, Symbol psymGlyphAttr);
	virtual void CheckCompleteAttachmentPoint(GdlObject * pgdlAvsOrExp,
		GrcSymbolTable * psymtbl, GrcGlyphAttrMatrix * pgax, Symbol psymGlyphAttr,
		bool * pfXY, bool * pfGpoint);
	virtual void CheckCompBox(GdlObject * pritset,
		GrcSymbolTable * psymtbl, GrcGlyphAttrMatrix * pgax, Symbol psymCompRef);
	virtual void StorePseudoToActualAsGlyphAttr(GrcGlyphAttrMatrix * pgax, int nAttrID,
		Vector<GdlExpression *> & vpexpExtra);
	void MarkFsmClass(int nPassID, int nClassID);

	bool IsFsmClass(int ipass)
	{
		if (ipass >= m_vfFsm.Size())
			return false;
		return m_vfFsm[ipass];
	}
	int FsmID(int ipass)
	{
		return m_vnFsmID[ipass];
	}

	virtual bool IncludesGlyph(utf16);

	void MarkReplcmtInputClass()			{ m_fReplcmtIn = true; }
	void MarkReplcmtOutputClass()			{ m_fReplcmtOut = true; }
	void SetReplcmtInputID(int nID)			{ m_nReplcmtInID = nID; }
	void SetReplcmtOutputID(int nID)		{ m_nReplcmtOutID = nID; }

	bool ReplcmtInputClass()				{ return m_fReplcmtIn; }
	bool ReplcmtOutputClass()				{ return m_fReplcmtOut; }
	int ReplcmtInputID()					{ return m_nReplcmtInID; }
	int ReplcmtOutputID()					{ return m_nReplcmtOutID; }

	bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	virtual bool HasOverlapWith(GdlGlyphClassMember * glfd, GrcFont * pfont);
	virtual bool HasBadGlyph();
	virtual bool WarnAboutBadGlyphs(bool fTop);
	virtual bool DeleteBadGlyphs();

public:
	//	Compiler:
	void RecordInclusionInClass(GdlPass * ppass);
	virtual void RecordInclusionInClass(GdlPass * ppass, GdlGlyphClassDefn * pglfc);
	virtual void GetMachineClasses(FsmMachineClass ** ppfsmcAssignments,
		Set<FsmMachineClass *> & setpfsmc);

	//	Output
	void GenerateOutputGlyphList(Vector<utf16> & vwGlyphs);
	void GenerateInputGlyphList(Vector<utf16> & vwGlyphs, Vector<int> & vnIndices);
	void AddGlyphsToUnsortedList(Vector<utf16> & vwGlyphs);
	void AddGlyphsToSortedList(Vector<utf16> & vwGlyphs, Vector<int> & vnIndices);

	//	debuggers
	void DebugCmap(GrcFont * pfont,
		utf16 * rgchwUniToGlyphID, unsigned int * rgnGlyphIDToUni);
	virtual void DebugCmapForMember(GrcFont * pfont,
		utf16 * rgchwUniToGlyphID, unsigned int * rgnGlyphIDToUni);

protected:
	//	Instance variables:
	StrAnsi							m_staName;
	Vector<GdlGlyphClassMember*>	m_vpglfdMembers;

	Vector<GdlGlyphAttrSetting*>	m_vpglfaAttrs;

//	Vector<GdlGlyphAttrSetting*>	m_vpglfaComponents;
//	Vector<StrAnsi>					m_vstaComponentNames;	// redundant with what is in components
															// list, but more accessible
//	GdlExpression *		m_pexpDirection;
//	int					m_nDirStmtNo;
//	GdlExpression *		m_pexpBreakweight;
//	int					m_nBwStmtNo;

	//	for compiler use:
	Vector<bool>	m_vfFsm;		// needs to be matched by the FSM, one flag for each pass
	Vector<int>		m_vnFsmID;		// FSM class ID, one for each pass

	bool	m_fReplcmtIn;		// serves as an input class for replacement
	bool	m_fReplcmtOut;		// serves as an output class for replacement
	int		m_nReplcmtInID;		// internal ID when serving as replacement input class
	int		m_nReplcmtOutID;	// internal ID when serving as replacement output class

};	// end of class GdlGlyphClassDefn


#endif // CLASSES_INCLUDED
