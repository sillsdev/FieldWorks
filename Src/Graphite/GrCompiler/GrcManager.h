/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrcManager.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	The main object that manages the compliation process.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef WRC_MANAGER_INCLUDED
#define WRC_MANAGER_INCLUDED

class GdlFeatureDefn;
class GdlGlyphClassDefn;
class GdlNameDefn;

/*----------------------------------------------------------------------------------------------
Class: GrcManager
Description: The object that manages the complication process. There is one global instance.
Hungarian: cman
----------------------------------------------------------------------------------------------*/
class GrcManager
{
public:
	//	Constructor & destructor:
	GrcManager();
	~GrcManager();
protected:
	void Init();
	void Clear();
public:
	void ClearFsmWorkSpace();

public:
	//	General:
	GdlRenderer * Renderer()				{ return m_prndr; }
	GrcSymbolTable * SymbolTable()			{ return m_psymtbl; }
	Vector<Symbol> * GlyphAttrVec()			{ return &m_vpsymGlyphAttrs; }
	GrcGlyphAttrMatrix * GlyphAttrMatrix()	{ return m_pgax; }
	GrcLigComponentList * LigCompList()		{ return m_plclist; }
	int NumGlyphs()							{ return m_cwGlyphIDs; }
	utf16 PhantomGlyph()					{ return m_wPhantom; }

	int FontTableVersion()
	{
		return m_fxdFontTableVersion;
	}
	void SetFontTableVersion(int fxd, bool f)
	{
		m_fxdFontTableVersion = fxd;
		m_fUserSpecifiedVersion = f;
	}
	int MaxFontVersion()
	{
		// Highest version of the font tables this version of the compiler can generate:
		return kfxdCompilerVersion;
	}
	int DefaultFontVersion()
	{
		return 0x00020000; // kfxdCompilerVersion;
	}
	bool UserSpecifiedVersion()
	{
		return m_fUserSpecifiedVersion;
	}

	int VersionForTable(int ti);
	int VersionForTable(int ti, int fxdRequestedVersion);
	int VersionForRules();

	void SetNameTableStart(int n)
	{
		m_nNameTblStart = n;
	}
	int NameTableStart()
	{
		 return m_nNameTblStart;
	}
	int NameTableStartMin()
	{
		return 256;
	}

	int NumJustLevels();

	//	environment getters & setters
	Symbol Table()				{ return m_venv.Top()->Table(); }
	int Pass()					{ return m_venv.Top()->Pass(); }
	int MUnits()				{ return m_venv.Top()->MUnits(); }
	int PointRadius()			{ return m_venv.Top()->PointRadius(); }
	int PointRadiusUnits()		{ return m_venv.Top()->PointRadiusUnits(); }
	int MaxRuleLoop()			{ return m_venv.Top()->MaxRuleLoop(); }
	int MaxBackup()				{ return m_venv.Top()->MaxBackup(); }
	bool AttrOverride()			{ return m_venv.Top()->AttrOverride(); }
	utf16 CodePage()			{ return m_venv.Top()->CodePage(); }

	void SetTable(Symbol psym)			{ m_venv.Top()->SetTable(psym); }
	void SetPass(int n)					{ m_venv.Top()->SetPass(n); }
	void SetMUnits(int m)				{ m_venv.Top()->SetMUnits(m); }
	void SetPointRadius(int n, int m)	{ m_venv.Top()->SetPointRadius(n, m); }
	void SetMaxRuleLoop(int n)			{ m_venv.Top()->SetMaxRuleLoop(n); }
	void SetMaxBackup(int n)			{ m_venv.Top()->SetMaxBackup(n); }
	void SetAttrOverride(bool f)		{ m_venv.Top()->SetAttrOverride(f); }
	void SetCodePage(utf16 w)			{ m_venv.Top()->SetCodePage(w); }

	GdlRuleTable * RuleTable(GrpLineAndFile & lnf)
	{
		return m_prndr->GetRuleTable(lnf, Table()->FieldAt(0));
	}

	bool OutputDebugFiles()				{ return m_fOutputDebugFiles; }
	void SetOutputDebugFiles(bool f)	{ m_fOutputDebugFiles = f; }
	bool IgnoreBadGlyphs()				{ return m_fIgnoreBadGlyphs; }
	void SetIgnoreBadGlyphs(bool f)		{ m_fIgnoreBadGlyphs = f; }
	bool IsVerbose()					{ return m_verbose; }
	void SetVerbose(bool verbose) 		{ m_verbose = verbose; }
	int SeparateControlFile()			{ return m_fSepCtrlFile; }
	void SetSeparateControlFile(bool f)	{ m_fSepCtrlFile = f; }

public:
	//	Parser:
	bool Parse(StrAnsi staFileName);
protected:
	bool RunPreProcessor(StrAnsi staFileName, StrAnsi * staFilePreProc);
	void RecordPreProcessorErrors(FILE * pFilePreProcErr);
	StrAnsi PreProcName(StrAnsi sta);
	bool ParseFile(std::ifstream & strmIn, StrAnsi staFileName);
	void InitPreDefined();
	void WalkParseTree(RefAST ast);
	void WalkTopTree(RefAST ast);
	void WalkEnvTree(RefAST ast, TableType tblt, GdlRuleTable *, GdlPass *);
	void WalkDirectivesTree(RefAST ast);
	void WalkTableTree(RefAST ast);
	void WalkTableElement(RefAST ast, TableType tblt, GdlRuleTable * prultbl, GdlPass * ppass);
	void WalkGlyphTableTree(RefAST ast);
	void WalkGlyphTableElement(RefAST ast);
	void WalkGlyphClassTree(RefAST ast, GdlGlyphClassDefn * pglfc);
	void WalkGlyphAttrTree(RefAST ast, Vector<StrAnsi> & vsta);
	void WalkFeatureTableTree(RefAST ast);
	void WalkFeatureTableElement(RefAST ast);
	void WalkFeatureSettingsTree(RefAST ast, Vector<StrAnsi> & vsta);
	void WalkLanguageTableTree(RefAST ast);
	void WalkLanguageTableElement(RefAST ast);
	void WalkLanguageItem(RefAST ast, GdlLangClass * plcls);
	void WalkLanguageCodeList(RefAST astList, GdlLangClass * plcls);
	void WalkNameTableTree(RefAST ast);
	void WalkNameTableElement(RefAST ast);
	void WalkNameIDTree(RefAST ast, Vector<StrAnsi> & vsta);
	void WalkRuleTableTree(RefAST ast, int nodetyp);
	void WalkPassTree(RefAST ast, GdlRuleTable * prultbl, GdlPass * ppassPrev);
	void WalkIfTree(RefAST astContents, GdlRuleTable *, GdlPass *);
	bool AllContentsArePasses(RefAST ast);
	void WalkRuleTree(RefAST ast, GdlRuleTable * prultbl, GdlPass * ppass);
	void WalkSlotAttrTree(RefAST ast, GdlRuleItem * prit, Vector<StrAnsi> & vsta);
	GdlExpression * WalkExpressionTree(RefAST ast);

	void ProcessGlobalSetting(RefAST);
	void ProcessGlyphClassMember(RefAST ast, GdlGlyphClassDefn * pglfc,
		GdlGlyphDefn ** ppglfRet);
	GdlGlyphDefn * ProcessGlyph(RefAST astGlyph, GlyphType glft, int nCodePage = -1);
	void ProcessFunction(RefAST ast, Vector<StrAnsi> & vsta,
		bool fSlotAttr, GdlRuleItem * prit = NULL, Symbol psymOp = NULL);
	void ProcessFunctionArg(bool fSlotAttr, GrcStructName const& xns,
		int nPR, int mPRUnits, bool fOverride, GrpLineAndFile const& lnf,
		ExpressionType expt, GdlRuleItem * prit, Symbol psymOp, GdlExpression * pexpValue);
	void BadFunctionError(GrpLineAndFile & lnf, StrAnsi staFunction,
		StrAnsi staArgsExpected);
	void ProcessItemRange(RefAST astItem, GdlRuleTable * prultbl, GdlPass * ppass,
		GdlRule * prule, int * pirit, int lrc, bool fHasLhs);
	void ProcessRuleItem(RefAST astItem, GdlRuleTable * prultbl, GdlPass * ppass,
		GdlRule * prule, int * pirit, int lrc, bool fHasLhs);
	StrAnsi ProcessClassList(RefAST ast, RefAST * pastNext);
	StrAnsi ProcessAnonymousClass(RefAST ast, RefAST * pastNext);
	void ProcessSlotIndicator(RefAST ast, GdlAlias * palias);
	void ProcessAssociations(RefAST ast, GdlRuleTable * prultbl, GdlRuleItem * prit, int lrc);

	GrpLineAndFile LineAndFile(RefAST);
	int NumericValue(RefAST);
	int NumericValue(RefAST, bool * pfM);
	Symbol IdentifierSymbol(RefAST ast, Vector<StrAnsi> & vsta);
	bool ClassPredefGlyphAttr(Vector<StrAnsi> & vsta, ExpressionType * pexpt, SymbolType * psymt);
public:	// so they can be called by the test procedures
	GrcEnv * PushTableEnv(GrpLineAndFile &, StrAnsi staTableName);
	GrcEnv * PushPassEnv(GrpLineAndFile &, int nPass);
	GrcEnv * PushGeneralEnv(GrpLineAndFile &);
	GrcEnv * PopEnv(GrpLineAndFile &, StrAnsi staStmt);
protected:
	GrcEnv * PushEnvAux();
public:	// so they can be called by the test procedures
	GdlGlyphClassDefn * AddGlyphClass(GrpLineAndFile const&, StrAnsi staClassName);
	GdlGlyphClassDefn * AddAnonymousClass(GrpLineAndFile const&);
protected:
	void AddGlyphToClass(GdlGlyphClassDefn * pglfc, GdlGlyphClassMember * pglfd);

	//	debuggers:
	void DebugParseTree(RefAST);

public:
	//	Post-parser:
	bool PostParse();
protected:
	void ProcessMasterTables();

public:
	//	Pre-compiler:
	bool PreCompile(GrcFont *);
	bool Compile(GrcFont *);

protected:
	bool PreCompileFeatures(GrcFont *);
	bool PreCompileClassesAndGlyphs(GrcFont *);
	bool PreCompileRules(GrcFont *);
	bool PreCompileLanguages(GrcFont * pfont);

	bool GeneratePseudoGlyphs(GrcFont *);
	utf16 FirstFreeGlyph(GrcFont *);
	void CreateAutoPseudoGlyphDefn(utf16 wAssigned, int nUnicode, utf16 wGlyphID);
	void SortPseudoMappings();

	bool AddAllGlyphsToTheAnyClass(GrcFont * pfont, HashMap<utf16, utf16> & hmActualForPseudo);

	bool MaxJustificationLevel(int * pnJLevel);
	bool CompatibleWithVersion(int fxdVersion, int * pfxdNeeded);

	bool AssignInternalGlyphAttrIDs();

	bool AssignGlyphAttrsToClassMembers(GrcFont * pfont);
	bool ProcessGlyphAttributes(GrcFont * pfont);
	void ConvertBetweenXYAndGpoint(GrcFont * pfont, utf16 wGlyphID);
	bool FinalGlyphAttrResolution(GrcFont * pfont);
	void MinAndMaxGlyphAttrValues(int nAttrID,
		int cJLevels, int nAttrIDJStr, int nAttrIDJShr, int nAttrIDJStep, int nAttrIDJWeight,
		int * pnMin, int * pnMax);
	bool StorePseudoToActualAsGlyphAttr();
public:
	int PseudoForUnicode(int nUnicode);
	int ActualForPseudo(utf16 wPseudo);
	utf16 LbGlyphId()
	{
		return m_wLineBreak;
	}

protected:
	bool AssignClassInternalIDs();
public:
	void AddToFsmClasses(GdlGlyphClassDefn * pglfc, int nPassID);
protected:

public:
	//	Compiler:
	int SlotAttributeIndex(Symbol psym);
	void GenerateFsms();
	////void InitializeFsmArrays();
	Vector<GdlGlyphClassDefn *> * FsmClassesForPass(int nPassID);
	void CalculateContextOffsets();

	//	Output:
	bool AssignFeatTableNameIds(utf16 wFirstNameId, Vector<StrUni> * pvstuExtNames,
		Vector<utf16> * pvwLangIds, Vector<utf16> * pvwNameTblIds);
	int OutputToFont(char * pchSrcFileName, char * pchDstFileName,
		utf16 * pchDstFontFamily, utf16 * pchSrcFontFamily);
	int FinalAttrValue(utf16 wGlyphID, int nAttrID);
	void ConvertBwForVersion(int wGlyphId, int nAttrIdBw);
	void SplitLargeStretchValue(int wGlyphId, int nAttrIdJStr);
protected:
	bool AddFeatsModFamily(uint8 ** ppNameTbl, uint32 * pcbNameTbl, uint16 * pchFamilyName);
	bool FindNameTblEntries(void * pNameTblRecord, int cnNameTblRecords,
		uint16 suPlatformId, uint16 suEncodingId, uint16 suLangId,
		int * piFamily, int * piSubFamily, int * piFullName,
		int * piPlatEncMin, int * piPlatEncLim, int * piMaxNameId, int * pcbNames);
	bool BuildFontNames(uint16 * pchFamilyName, uint8 * pSubFamily, uint16 cbSubFamily,
		uint16 ** ppchwFamilyName, uint16 * pcchwFamilyName,
		uint16 ** ppchwFullName, uint16 * pcchwFullName);
	bool AddFeatsModFamilyAux(uint8 * pTbl, uint32 cbTbl, uint8 * pNewTbl, uint32 cbNewTbl,
		Vector<StrUni> * pvstuExtNames, Vector<uint16> * pvsuLangIds, Vector<uint16> * pvsuNameTblIds,
		int iFamilyRecord, int iFullRecord, int iPlatEncMin, int iPlatEncLim, bool f31Name,
		uint16 * pchwFamilyName, uint16 cchwFamilyName,
		uint16 * pchwFullName, uint16 cchwFullName);
	bool OutputOS2Table(uint8 * pOs2TblSrc, uint32 cbOs2TblSrc,
		uint8 * pOs2TblMin, uint32 chbOs2TblMin, GrcBinaryStream * pbstrm, uint32 * pchSizeRet);
	bool OutputCmapTable(uint8 * pCmapTblSrc, uint32 cbCmapTblSrc,
		GrcBinaryStream * pbstrm, uint32 * pchSizeRet);
	int OutputCmap31Table(void * pCmapSubTblSrc, GrcBinaryStream * pbstrm, bool fFrom310,
		bool * pfNeed310);
	int OutputCmap310Table(void * pCmapSubTblSrc, GrcBinaryStream * pbstrm, bool fFrom31);
	void OutputSileTable(GrcBinaryStream * pbstrm,
		utf16 * pchStrFontFamily, char * pchSrcFileName, long luMasterChecksum,
		unsigned int * pnCreateTime, unsigned int * pnModifyTime,
		int * pibOffset, int * pcbSize);
	void OutputGlatAndGloc(GrcBinaryStream * pbstrm, int * pnGlocOffset, int * pnGlocSize,
		int * pnGlatOffset, int * pnGlatSize);
	void OutputSilfTable(GrcBinaryStream * pbstrm, int * pnSilfOffset, int * pnSilfSize);
	void OutputFeatTable(GrcBinaryStream * pbstrm, int * pnFeatOffset, int * pnFeatSize);
	void OutputSileTable(GrcBinaryStream * pbstrm, utf16 * pchwFontName, long nChecksum);
	void OutputSillTable(GrcBinaryStream * pbstrm, int * pnSillOffset, int * pnSillSize);

public:

	//	debuggers:
	void DebugEngineCode();
	void DebugRulePrecedence();
	void DebugGlyphAttributes();
	void DebugClasses();
	void DebugFsm();
	////void WalkFsmMachineClasses();
	void DebugOutput();
	void DebugCmap(GrcFont * pfont);
	void WriteCmapItem(std::ofstream & strmOut,
		unsigned int nUnicode, bool fSuppPlaneChars, utf16 wGlyphID, bool fUnicodeToGlyph,
		bool fPseudo, bool fInCmap);
	static void DebugHex(std::ostream & strmOut, utf16 wGlyphID);
	static void DebugUnicode(std::ostream & strmOut, int nUnicode, bool f32bit);

protected:
	//	Instance variables:

	//	The version of the font tables to output.
	int m_fxdFontTableVersion;
	//	Did the user include a /v option?
	bool m_fUserSpecifiedVersion;

	//	Are we creating a separate control file?
	bool m_fSepCtrlFile;

	//	Basic justification: true if no justify attributes are present.
	bool m_fBasicJust;
	//	Highest justification level used.
	int m_nMaxJLevel;

	//	Where to start the feature names in the name table.
	int m_nNameTblStart;

	//	Ignore nonexistent glyphs?
	bool m_fIgnoreBadGlyphs;

	//	The top-level object representing the GDL program.
	GdlRenderer * m_prndr;

	GrcSymbolTable * m_psymtbl;

	//	Temporary structures used during parsing--master tables of glyph attribute
	//	and feature settings.
	GrcMasterTable *		m_mtbGlyphAttrs;
	GrcMasterTable *		m_mtbFeatures;
	GrcMasterValueList *	m_mvlNameStrings;
	Vector<Symbol>			m_vpsymStyles;
	// Also language classes:
	Vector<GdlLangClass *>	m_vplcls;

	int m_fxdFeatVersion;	// version of feature table to generate

	Vector<GrcEnv> m_venv;
	HashMap<Symbol, int> m_hmpsymnCurrPass;	// for each table, the current pass
	Vector<GdlExpression *> m_vpexpConditionals;
	Vector<GdlExpression *> m_vpexpPassConstraints;

	bool m_fOutputDebugFiles;

	//	For compiler use:

	int m_wGlyphIDLim;	// lim of range of glyph IDs in the font
	int m_cwGlyphIDs;

	int m_cpsymComponents;	// total number of ligature components encountered

	//	Pseudo-code mappings: the two vectors form pairs of underlying unicode values and
	//	coresponding pseudo-glyph IDs.
	Vector<unsigned int> m_vnUnicodeForPseudo;
	Vector<utf16> m_vwPseudoForUnicode;
	unsigned int m_nMaxPseudoUnicode;
	utf16 m_wFirstAutoPseudo;

	HashMap<utf16, utf16> m_hmActualForPseudo;

	utf16 m_wLineBreak;	// line break pseudo glyph

	//	Used to represent a "phantom glyph"--one that matches a rule's pre-context when
	//	the stream position is near the beginning and therefore there aren't enough slots in
	//	the stream; this glyph is a member of the ANY class and no other.
	utf16 m_wPhantom;

	//	The following vector maps the internal glyph attr ID to the symbol in the symbol table
	//	(which in turn has a record of the internal ID).
	Vector<Symbol> m_vpsymGlyphAttrs;

	//	The following matrix contains the glyph attribute assignments for
	//	all of the glyphs in the system. Used by the parser and post-parser.
	GrcGlyphAttrMatrix * m_pgax;

	//	The following defines an array containing the ligature component mappings for
	//	each glyph. For glyphs that are not ligatures, the array contains NULL.
	//	For ligatures, it contains a pointer to a structure holding a vector something like:
	//		clsABC.component.A
	//		clsABC.component.B
	//		clsABC.component.C
	GrcLigComponentList * m_plclist;

	//	Extra instances of expressions that were simplified or changed in any way
	//	from the originals; pointers are stored here so that they can be properly deleted.
	//	In a sense it is an extension to the master tables; it contains expressions that
	//	would normally be owned there but aren't.
	Vector<GdlExpression *> m_vpexpModified;

	//	The following vector maps the internal replacement-class IDs to the replacement-classes
	//	themselves (which in turn have a record of the ID). (Replacement-classes are classes
	//	that are used to do replacements in substitution rules;
	//	eg, in "clsA > clsB / _ clsC" clsA and clsB are replacement classes.)
	Vector<GdlGlyphClassDefn *> m_vpglfcReplcmtClasses;
	int m_cpglfcLinear;	// number of linear classes

	//	Each vector in the array maps the internal FSM-class IDs to the FSM-classes themselves
	//	(which in turn have a record of the ID). (FSM-classes are classes that are used for
	//	matching input.) There is one vector per pass.
	Vector<GdlGlyphClassDefn *> * m_prgvpglfcFsmClasses;

	int cReplcmntClasses;

	bool m_verbose;

public:
	//	For test procedures:
	void test_Recycle();
};

#endif // WRC_MANAGER_INCLUDED
