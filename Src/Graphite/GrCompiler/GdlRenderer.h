/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GdlRenderer.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	GdlRenderer is the top-level object corresponding to a rendering behavior description in
	a single GDL file.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef RENDERER_INCLUDED
#define RENDERER_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: GdlRenderer
Description: Top-level object; there is only one instance of this class per compile.
Hungarian: rndr
----------------------------------------------------------------------------------------------*/

class GrcTable;

class GdlGlyphClassDefn;
class GdlFeatureDefn;

class GdlRenderer : public GdlObject
{
public:
	//	Constructor & destructor:
	GdlRenderer();
	~GdlRenderer();

	//	General:
	bool AutoPseudo()			{ return m_fAutoPseudo; }
	void SetAutoPseudo(bool f)	{ m_fAutoPseudo = f; }

	bool Bidi()					{ return m_fBidi; }
	void SetBidi(bool f)		{ m_fBidi = f; }

	int ScriptDirections()		{ return m_grfsdc; }
	void AddScriptDirection(int fsdc)
	{
		m_grfsdc |= fsdc;
	}
	bool ClearScriptDirections()
	{
		bool fRet = (m_grfsdc != kfsdcNone);
		m_grfsdc = kfsdcNone;
		return fRet;
	}

	void AddScriptTag(int n)
	{
		for (int i = 0; i < m_vnScriptTags.Size(); i++)
		{
			if (m_vnScriptTags[i] == n)
				return;
		}
		m_vnScriptTags.Push(n);
	}
	bool ClearScriptTags()
	{
		bool fRet = m_vnScriptTags.Size() > 0;
		m_vnScriptTags.Clear();
		return fRet;
	}

	int NumScriptTags()
	{
		return m_vnScriptTags.Size();
	}
	int ScriptTag(int i)
	{
		Assert(i < m_vnScriptTags.Size());
		return m_vnScriptTags[i];
	}

	GdlNumericExpression * ExtraAscent()				{ return m_pexpXAscent; }
	void SetExtraAscent(GdlNumericExpression * pexp)	{ m_pexpXAscent = pexp; }

	GdlNumericExpression * ExtraDescent()				{ return m_pexpXDescent; }
	void SetExtraDescent(GdlNumericExpression * pexp)	{ m_pexpXDescent = pexp; }

	void AddGlyphClass(GdlGlyphClassDefn * pglfc)
	{
		m_vpglfc.Push(pglfc);
	}
	void AddFeature(GdlFeatureDefn * pfeat)
	{
		m_vpfeat.Push(pfeat);
	}
	bool AddLanguage(GdlLanguageDefn * plang);

	NameDefnMap & NameAssignmentsMap()
	{
		return m_hmNameDefns;
	}

	//	Parser:
	GdlRuleTable * GetRuleTable(GrpLineAndFile & lnf, StrAnsi staTableName);
	GdlRuleTable * FindRuleTable(StrAnsi staTableName);
	GdlRuleTable * FindRuleTable(Symbol psymTableName);

	//	Post-parser:
	bool ReplaceAliases();
	bool HandleOptionalItems();
	bool CheckSelectors();

	//	Pre-compiler:
	bool PreCompileFeatures(GrcManager * pcman, GrcFont * pfont, int * pfxdFeatVersion);
	void CheckLanguageFeatureSize();
	int ExplicitPseudos(Set<GdlGlyphDefn *> & setpglf);
	int ActualForPseudo(utf16 wPseudo);
	bool AssignGlyphIDs(GrcFont *, utf16 wGlyphIDLim,
		HashMap<utf16, utf16> & hmActualForPseudos);
	void AssignGlyphAttrsToClassMembers(GrcGlyphAttrMatrix * pgax,
		GrcLigComponentList * plclist);
	void AssignGlyphAttrDefaultValues(GrcFont * pfont,
		GrcGlyphAttrMatrix * pgax, int cwGlyphs,
		Vector<Symbol> & vpsymSysDefined, Vector<int> & vnSysDefValues,
		Vector<GdlExpression *> & vpexpExtra,
		Vector<Symbol> & vpsymGlyphAttrs);
	DirCode ConvertBidiCode(LgBidiCategory bic, utf16 wUnicode);
	void StorePseudoToActualAsGlyphAttr(GrcGlyphAttrMatrix * pgax, int nAttrID,
		Vector<GdlExpression *> & vpexpExtra);

	bool FixRulePreContexts(Symbol psymAnyClass);
	bool FixGlyphAttrsInRules(GrcManager * pcman, GrcFont * pfont);
	bool CheckTablesAndPasses(GrcManager * pcman, int * pcpassValid);
	void MarkReplacementClasses(GrcManager * pcman,
		Set<GdlGlyphClassDefn *> & setpglfc);
	void GdlRenderer::DeleteAllBadGlyphs();
	bool CheckRulesForErrors(GrcGlyphAttrMatrix * pgax, GrcFont * pfont);
	bool CheckLBsInRules();
	void ReplaceKern(GrcManager * pcman);
	void MaxJustificationLevel(int * pnLevel);
	bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	void SetNumUserDefn(int c);
	//{
	//	m_cnUserDefn = max(m_cnUserDefn, c+1);
	//}
	int NumUserDefn()
	{
		return m_cnUserDefn;
	}

	void SetNumLigComponents(int c);
	//{
	//	m_cnComponents = max(m_cnComponents, c);
	//}
	int NumLigComponents()
	{
		return m_cnComponents;
	}

	//	Compiler:
	void GenerateFsms(GrcManager * pcman);
	void CalculateContextOffsets();
	bool LineBreakFlag()
	{
		return m_fLineBreak;
	}
	int PreXlbContext()
	{
		return m_critPreXlbContext;
	}
	int PostXlbContext()
	{
		return m_critPostXlbContext;
	}
	//	debuggers:
	void DebugEngineCode(GrcManager * pcman, std::ostream & strmOut);
	void DebugRulePrecedence(GrcManager * pcman, std::ostream & strmOut);
	void DebugFsm(GrcManager * pcman, std::ostream & strmOut);
	void DebugCmap(GrcFont * pfont, utf16 * rgchwUniToGlyphID, unsigned int * rgnGlyphIDToUni);
	void DebugClasses(std::ostream & strmOut,
		Vector<GdlGlyphClassDefn *> & vpglfcReplcmt, int cpglfcLinear);

	//	Output:
	void OutputReplacementClasses(Vector<GdlGlyphClassDefn *> & vpglfc, int cpglfcLinear,
		GrcBinaryStream * pbstrm);
	void CountPasses(int * pcpass, int * pcpassLB, int * pcpassSub,
		int * pcpassJust, int * pcpassPos, int * pipassBidi);
	void OutputPasses(GrcManager * pcman, GrcBinaryStream * pbstrm, long lTableStart,
		Vector<int> & vnOffsets);
	bool AssignFeatTableNameIds(utf16 wFirstNameId, Vector<StrUni> * pvstuExtNames,
		Vector<utf16> * pvwLangIds, Vector<utf16> * pvwNameTblIds);
	void OutputFeatTable(GrcBinaryStream * pbstrm, long lTableStart, int fxdVersion);
	void OutputSillTable(GrcBinaryStream * pbstrm, long lTableStart);

protected:
	//	Instance variables:

	Vector<GdlRuleTable *>			m_vprultbl;

	Vector<GdlGlyphClassDefn *>		m_vpglfc;
	Vector<GdlFeatureDefn *>		m_vpfeat;
	Vector<GdlLanguageDefn *>		m_vplang;
	NameDefnMap						m_hmNameDefns;
//	GdlStdStyle						m_rgsty[FeatureDefn::kstvLim];

	bool m_fAutoPseudo;
	bool m_fBidi;
	int m_iPassBidi;
	int m_grfsdc;		// supported script directions
	Vector<int>	m_vnScriptTags;
	GdlNumericExpression * m_pexpXAscent;
	GdlNumericExpression * m_pexpXDescent;
	//	true if any line-breaks are relevant to rendering:
	bool m_fLineBreak;
	//	limits on cross-line-boundary contextualization:
	int m_critPreXlbContext;
	int m_critPostXlbContext;

	int m_cnUserDefn;	// number of user-defined slot attributes
	int m_cnComponents;	// max number of components per ligature

	enum { kInfiniteXlbContext = 255 };
};


/*----------------------------------------------------------------------------------------------
Class: GdlStdStyle
Description: Standard style information; each styl in the list corresponds to a separate font
	file; eg, bold, italic, bold-italic.
Hungarian: sty
----------------------------------------------------------------------------------------------*/
class GdlStdStyle : public GdlObject
{
protected:
	//	instance variables:
	int		m_stvSetting;	// feature setting value (which also is this item's index
							// in the m_rgsty array)
	int		m_nInternalID;	// the index into the array of glyph attr values
	StrAnsi	m_staFontName;	// name of the font
};


#endif // RENDERER_INCLUDED
