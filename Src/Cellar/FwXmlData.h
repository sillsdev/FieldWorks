/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, 2009 SIL International. All rights reserved.

File: FwXmlData.h
Responsibility: Steve McConnel (was Shon Katzenberger)
Last reviewed:

TODO SteveMc: This should change to using our OLEDB wrapper instead of ODBC.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef FwXmlData_H
#define FwXmlData_H 1

/***********************************************************************************************
	ODBC headers.
***********************************************************************************************/
#include <sql.h>
#include <sqlext.h>
#include <sqltypes.h>
#include <odbcss.h>

/***********************************************************************************************
	Our headers.
***********************************************************************************************/
#include "SqlDb.h"

// Forward declarations.
class FwXmlExportData;
class FwXmlImportData;
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Basic information for a conceptual model "module" in a FieldWorks database.

	Hungarian: fdmi
----------------------------------------------------------------------------------------------*/
typedef struct FwDbModuleInfo
{
	int mid;			// unique module id (unique to this database, that is).
	int ver;			// current version number (??)
	int verBack;		// oldest version number still supported (??)
} FwDbModuleInfo;

/*----------------------------------------------------------------------------------------------
	Basic information for a conceptual model "class" in a FieldWorks database.

	Hungarian: fdci
----------------------------------------------------------------------------------------------*/
typedef struct FwDbClassInfo
{
	int cid;			// unique class id (unique to this database, that is).
	int cidBase;		// class id of base (super) class.
	int mid;			// id of the module to which this class belongs
	ComBool fAbstract;	// flag whether this class can exist on its own, or needs a subclass.
} FwDbClassInfo;

/*----------------------------------------------------------------------------------------------
	Basic information for a conceptual model "field" in a FieldWorks database.

	Hungarian: fdfi
----------------------------------------------------------------------------------------------*/
typedef struct FwDbFieldInfo
{
	int fid;			// unique field id (unique to database, that is).
	int cpt;			// type of data held in field (see CellarModuleDefns above).
	int cid;			// class id of "owner".
	int cidDst;			// class id of data, if "object" or "collection/sequence of objects".
	ComBool fCustom;	// flag whether this is a custom field

	bool fNullMin;			// Flag whether the corresponding field actually has data.
	bool fNullMax;
	bool fNullBig;
	_int64 nMin;			// minimum legal value for an integer field (or null)
	_int64 nMax;			// maximum legal value for an integer field (or null)
	ComBool fBig;			// flag whether a binary field if big?? (or null)
	long int nListRootId;	// database id of the target list (0 <=> null)
	long int nWsSelector;	// writing system selector for the field (0 <=> null)
} FwDbFieldInfo;

/*----------------------------------------------------------------------------------------------
		Generic ODBC type names.  They get defined to either Firebird or MSSQL types.
		This eventually needs to be added to the "database object"
----------------------------------------------------------------------------------------------*/
class OdbcType
{
public:
	SQLSMALLINT
		BINARY, BIT, CHAR, DOUBLE, GUID,
		FLOAT, SLONG, NUMERIC, SBIGINT, SHORT, DATE, TIME,
		TIMESTAMP, TINYINT, UBIGINT, UTINYINT, WCHAR;

	//constructor
	OdbcType::OdbcType(){
		switch(CURRENTDB){
			case FB: BIT = SQL_C_CHAR;
					 GUID = SQL_C_CHAR;
					 //fprintf(stdout,"case FB\n");
				break;
			case MSSQL: BIT = SQL_C_BIT;
						GUID = SQL_C_GUID;
						//fprintf(stdout,"case MSSQL\n");
				break;
			default: BIT = SQL_C_DEFAULT;
					 GUID = SQL_C_DEFAULT;
					 //fprintf(stdout,"default\n");
		}
		BINARY = SQL_C_BINARY;
		CHAR = SQL_C_CHAR;
		DOUBLE = SQL_C_DOUBLE;
		FLOAT = SQL_C_FLOAT;
		SLONG = SQL_C_SLONG;
		NUMERIC = SQL_C_NUMERIC;
		SBIGINT = SQL_C_SBIGINT;
		SHORT = SQL_C_SHORT;
		DATE = SQL_C_TYPE_DATE;
		TIME = SQL_C_TYPE_TIME;
		TIMESTAMP = SQL_C_TYPE_TIMESTAMP;
		TINYINT = SQL_C_TINYINT;
		UBIGINT = SQL_C_UBIGINT;
		UTINYINT = SQL_C_UTINYINT;
		WCHAR = SQL_C_WCHAR;
	}
};

/*----------------------------------------------------------------------------------------------
	This is the main Fieldworks Database class for dealing with importing and exporting data.
----------------------------------------------------------------------------------------------*/
class FwXmlData : public IFwXmlData2
{
public:
	//:> Static methods.
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);

	//:> IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)();
	STDMETHOD_(ULONG, Release)();

	//:> IFwXmlData methods.
	STDMETHOD(Open)(BSTR bstrServer, BSTR bstrDatabase);
	STDMETHOD(Close)();
	STDMETHOD(LoadXml)(BSTR bstrFile, IAdvInd * padvi);
	STDMETHOD(SaveXml)(BSTR bstrFile, ILgWritingSystemFactory * pwsf, IAdvInd * padvi);
	//:> IFwXmlData2 methods.
	STDMETHOD(ImportXmlObject)(BSTR bstrFile, int hvoOwner, int flid, IAdvInd * padvi);
	STDMETHOD(ImportMultipleXmlFields)(BSTR bstrFile, int hvoOwner, IAdvInd * padvi);
	STDMETHOD(UpdateListFromXml)(BSTR bstrFile, int hvoOwner, int flid, IAdvInd * padvi);
	STDMETHOD(put_BaseImportDirectory)(BSTR bstrDir);
	STDMETHOD(get_BaseImportDirectory)(BSTR * pbstrDir);

	// Utility access methods.

	int IndexOfCid(int cid)
	{
		int icls;
		if (m_hmcidicls.Retrieve(cid, &icls))
			return icls;
		else
			return -1;
	}

	bool IsSubclass(int cid, int cidPossibleBase)
	{
		int icls;
		if (m_hmcidicls.Retrieve(cid, &icls))
		{
			if (m_vfdci[icls].cidBase == cidPossibleBase)
				return true;
			else if (m_vfdci[icls].cidBase == 0)
				return false;
			else
				return (IsSubclass(m_vfdci[icls].cidBase, cidPossibleBase));
		}
		else
		{
			return false;
		}
	}

	bool MapCidToIndex(int cid, int * picls)
	{
		return m_hmcidicls.Retrieve(cid, picls);
	}

	int ClassCount()
	{
		return m_vfdci.Size();
	}

	const FwDbClassInfo & ClassInfo(int icls)
	{
		return m_vfdci[icls];
	}

	const StrUni & ClassName(int icls)
	{
		return m_vstucls[icls];
	}

	int ClassIndexFromName(StrUni & stuName)
	{
		int icls;
		if (m_hmsuicls.Retrieve(stuName, &icls))
			return icls;
		else
			return -1;
	}

	const Vector<int> & ClassFields(int icls)
	{
		return m_mpclsflds[icls];
	}

	int IndexOfFid(int fid)
	{
		int ifld;
		if (m_hmfidifld.Retrieve(fid, &ifld))
			return ifld;
		else
			return -1;
	}

	bool MapFidToIndex(int fid, int * pifld)
	{
		return m_hmfidifld.Retrieve(fid, pifld);
	}

	int FieldCount()
	{
		return m_vfdfi.Size();
	}

	const FwDbFieldInfo & FieldInfo(int ifld)
	{
		return m_vfdfi[ifld];
	}

	const StrUni & FieldName(int ifld)
	{
		return m_vstufld[ifld];
	}

	int FieldIndexFromNames(StrUni & stuClass, StrUni & stuField)
	{
		int icls = ClassIndexFromName(stuClass);
		if (icls < 0)
			return -1;
		StrUni stuXml(stuField);
		stuXml.FormatAppend(L"%d", m_vfdci[icls].cid);
		int ifld;
		if (m_hmsuXmlifld.Retrieve(stuXml, &ifld))
			return ifld;
		else
			return -1;
	}

	const StrUni & FieldXmlName(int ifld)
	{
		return m_vstufldXml[ifld];
	}

	const StrUni & ServerName()
	{
		return m_stuServer;
	}

	const StrUni & DatabaseName()
	{
		return m_stuDatabase;
	}

	const int DbVersion()
	{
		if (!m_sdb.IsOpen())
			return 0;
		else
			return m_nVersion;
	}

protected:
	int m_cref;							// Reference count maintained by AddRef and Release.
	SqlDb m_sdb;						// The database connection, if one is open.
	int m_nVersion;						// The database version, if the database is open.

	HashMap<int,int> m_hmmidimod;		// Map module id onto module index.
	HashMapStrUni<int> m_hmsuimod;		// Map module name onto module index.
	Vector<FwDbModuleInfo> m_vfdmi;		// Table of basic module information.
	Vector<StrUni> m_vstumod;			// Table of module names, parallel to m_vfdmi.

	HashMap<int,int> m_hmcidicls;		// Map class id onto class index.
	HashMapStrUni<int> m_hmsuicls;		// Map class name onto class index.
	Vector<FwDbClassInfo> m_vfdci;		// Table of basic class information.
	Vector<StrUni> m_vstucls;			// Table of class names, parallel to m_vfdci.

	HashMap<int,int> m_hmfidifld;		// Map field id onto field index.
	MultiMap<StrUni,int> m_mmsuifld;	// Map field name onto field indices.
	HashMapStrUni<int> m_hmsuXmlifld;	// Map XML field name onto field index.
	Vector<FwDbFieldInfo> m_vfdfi;		// Table of basic field information.
	Vector<StrUni> m_vstufld;			// Table of field names, parallel to m_vfdfi.
	Vector<StrUni> m_vstufldXml;		// This is a performance hack, trading space for time.

	Vector<StrUni> m_vstufldUserLabel;	// Table of field User Labels, parallel to m_vfdfi.
	Vector<StrUni> m_vstufldHelpString;	// Table of field Help Strings, parallel to m_vfdfi.
	Vector<StrUni> m_vstufldXmlUI;		// Table of field UI XML strings, parallel to m_vfdfi.

	Vector<Vector<int> > m_mpmodclss;	// Map module index onto a set of class indices.

	// Map class index onto a complete set of field indices.  The set includes the class's
	// fields and its superclass's fields (recursively all the way to CmObject).
	Vector<Vector<int> > m_mpclsflds;
	StrUni m_stuServer;					// The name of the open server.
	StrUni m_stuDatabase;				// The name of the open database.

	StrUni m_stuBaseImportDirectory;

	static GenericFactory s_fact;

	// Constructor.
	FwXmlData()
	{
		ModuleEntry::ModuleAddRef();
		m_cref = 1;
	}

	// Destructor.
	~FwXmlData()
	{
		ModuleEntry::ModuleRelease();
	}

	void LoadMultiUnicodeRows(FwXmlExportData * pxed, SqlDb & sdb);

	void LoadMetaInfo();
	const wchar * WriteFieldStartTag(IStream * pstrm, int ifld);
	void WriteObjectData(IStream * pstrm, int iobj, FwXmlExportData * pxed);
	void WriteLink(IStream * pstrm, int iobj, int hobjSrc, int hobjDst,
		const wchar * pszwXmlName, FwXmlExportData * pxed);

#if 99
	void DumpMetaInfo();				//:> Temporary debugging hack for ODBC.
#endif

	friend class FwXmlImportData;
	friend class FwXmlExportData;
};

/*
inline int CbScpCode(byte bT) { return !(bT & 0x80) ? 1 : (bT & 0x40) ? 5 : 2; }
inline int CbScpData(int scp) { return 1 << (scp & 0x03); }

  04/02/44 00/56 01 00 00 00/5A 01 77 01 00/62 02  ..D.V....Z.w..b.
  71 02 00/97 02 3C C1 A2 25 2C 00 00 00 00 14 00  q..—.<Á¢%,......
  3C 00 64 00 65 00 66 00 61 00 75 00 6C 00 74 00  <.d.e.f.a.u.l.t.
  20 00 73 00 61 00 6E 00 73 00 20 00 73 00 65 00   .s.a.n.s. .s.e.
  72 00 69 00 66 00 3E 00 01 00 06 00 01 00 10 27  r.i.f.>........'
  00 00 E9 BD 8B 37 00 00 00 00 14 00 3C 00 64 00  ..é½‹7......<.d.
  65 00 66 00 61 00 75 00 6C 00 74 00 20 00 73 00  e.f.a.u.l.t. .s.
  61 00 6E 00 73 00 20 00 73 00 65 00 72 00 69 00  a.n.s. .s.e.r.i.
  66 00 3E 00 01 00 06 00 01 00 10 27 00 00/9C 02  f.>........'..œ.
  32 C1 A2 25 2C 00 00 00 00 0F 00 54 00 69 00 6D  2Á¢%,......T.i.m
  00 65 00 73 00 20 00 4E 00 65 00 77 00 20 00 52  .e.s. .N.e.w. .R
  00 6F 00 6D 00 61 00 6E 00 01 00 06 00 01 00 10  .o.m.a.n........
  27 00 00 E9 BD 8B 37 00 00 00 00 0F 00 54 00 69  '..é½‹7......T.i
  00 6D 00 65 00 73 00 20 00 4E 00 65 00 77 00 20  .m.e.s. .N.e.w.
  00 52 00 6F 00 6D 00 61 00 6E 00 01 00 06 00 01  .R.o.m.a.n......
  00 10 27 00 00								   ..'..

ctip = 04
ctsp = 02
scp = 44 (0100 0100)
tpt = 11 (ktptAlign)
nVal = 00 [nVar = ktpvEnum = 3]
scp = 56 (0101 0110)
tpt = 15 (ktptSpaceBefore)
nVal = 0000000 (0), nVar = 1
scp = 5A (0101 1010)
tpt = 16 (ktptSpaceAfter)
nVal = 0001770 (6000), nVar = 1
scp = 62 (0110 0010)
ttp = 18 (ktptLineHeight)
nVal = 0002710 (10000), nVar = 2
ttp = 97 = 151 (????)
cch = 3C (60)
ttp = 9C = 156 (ktptWsStyle)

 */

// If Ling.sqh and LangProj.sqh didn't depend on Cellar.sqh, we could #include those files and
// get these definitions!
// enum
// {
// #define CMCG_SQL_ENUM
// #include "Ling.sqh"
// #include "LangProj.sqh"
// #undef CMCG_SQL_ENUM
// };
// Someday we should fix the build system to produce all the header/tlb files before running the
// C++ and C# compilers over the code base.

enum
{
	kclidMoStemMsa = 5001,
	kflidMoStemMsa_MsFeatures = 5001001,
	kflidMoStemMsa_PartOfSpeech = 5001002,
	kflidMoStemMsa_InflectionClass = 5001003,
	kflidMoStemMsa_ExceptionFeatures = 5001004,
	kflidMoStemMsa_Stratum = 5001005,

	kclidLexEntry = 5002,
	kflidLexEntry_HomographNumber = 5002001,
	kflidLexEntry_CitationForm = 5002003,
	kflidLexEntry_DateCreated = 5002005,
	kflidLexEntry_DateModified = 5002006,
//	kflidLexEntry_Allomorphs = 5002008,
	kflidLexEntry_MorphoSyntaxAnalyses = 5002009,
//	kflidLexEntry_UnderlyingForm = 5002010,
	kflidLexEntry_Senses = 5002011,
	kflidLexEntry_Bibliography = 5002012,
	kflidLexEntry_Etymology = 5002013,
	kflidLexEntry_Restrictions = 5002014,
	kflidLexEntry_Pronunciation = 5002016,
	kflidLexEntry_SummaryDefinition = 5002017,
	kflidLexEntry_LiteralMeaning = 5002018,
	kflidLexEntry_Comment = 5002025,
	kflidLexEntry_DoNotUseForParsing = 5002026,
	kflidLexEntry_ExcludeAsHeadword = 5002027,
	kflidLexEntry_ImportResidue = 5002028,
	kflidLexEntry_LexemeForm = 5002029,
	kflidLexEntry_AlternateForms = 5002030,

	kclidLexDb = 5005,
	kflidLexDb_Entries = 5005001,
	kflidLexDb_Appendixes = 5005002,
	kflidLexDb_SenseTypes = 5005005,
	kflidLexDb_UsageTypes = 5005006,
	kflidLexDb_DomainTypes = 5005007,
	kflidLexDb_MorphTypes = 5005008,
	kflidLexDb_LexicalFormIndex = 5005010,
	kflidLexDb_AllomorphIndex = 5005011,
	kflidLexDb_Introduction = 5005012,
	kflidLexDb_IsHeadwordCitationForm = 5005013,
	kflidLexDb_IsBodyInSeparateSubentry = 5005014,
	kflidLexDb_Status = 5005015,
	kflidLexDb_Styles = 5005016,
	kflidLexDb_ReversalIndexes = 5005017,
	kflidLexDb_References = 5005019,
	kflidLexDb_Resources = 5005021,
	kflidLexDb_VariantEntryTypes = 5005022,
	kflidLexDb_ComplexEntryTypes = 5005023,

	kclidLexPronunciation = 5014,
	kflidLexPronunciation_Form = 5014001,
	kflidLexPronunciation_Location = 5014003,
	kflidLexPronunciation_MediaFiles = 5014004,
	kflidLexPronunciation_CVPattern = 5014005,
	kflidLexPronunciation_Tone = 5014006,

	kclidLexSense = 5016,
	kflidLexSense_MorphoSyntaxAnalysis = 5016001,
	kflidLexSense_AnthroCodes = 5016002,
	kflidLexSense_Senses = 5016003,
	kflidLexSense_Appendixes = 5016004,
	kflidLexSense_Definition = 5016005,
	kflidLexSense_DomainTypes = 5016006,
	kflidLexSense_Examples = 5016007,
	kflidLexSense_Gloss = 5016008,
	kflidLexSense_ReversalEntries = 5016009,
	kflidLexSense_Pictures = 5016010,
	kflidLexSense_ScientificName = 5016011,
	kflidLexSense_SenseType = 5016012,
	kflidLexSense_ThesaurusItems = 5016013,
	kflidLexSense_UsageTypes = 5016014,
	kflidLexSense_AnthroNote = 5016015,
	kflidLexSense_Bibliography = 5016016,
	kflidLexSense_DiscourseNote = 5016017,
	kflidLexSense_EncyclopedicInfo = 5016018,
	kflidLexSense_GeneralNote = 5016019,
	kflidLexSense_GrammarNote = 5016020,
	kflidLexSense_PhonologyNote = 5016021,
	kflidLexSense_Restrictions = 5016022,
	kflidLexSense_SemanticsNote = 5016023,
	kflidLexSense_SocioLinguisticsNote = 5016024,
	kflidLexSense_Source = 5016025,
	kflidLexSense_Status = 5016026,
	kflidLexSense_SemanticDomains = 5016027,
	kflidLexSense_ImportResidue = 5016028,

	kclidMoAffixAllomorph = 5027,
	kflidMoAffixAllomorph_MsEnvFeatures = 5027001,
	kflidMoAffixAllomorph_PhoneEnv = 5027002,
	kflidMoAffixAllomorph_MsEnvPartOfSpeech = 5027004,
	kflidMoAffixAllomorph_Position = 5027005,

	kclidMoAffixForm = 5028,
	kflidMoAffixForm_InflectionClasses = 5028001,

	kclidMoDerivAffMsa = 5031,
	kflidMoDerivAffMsa_FromMsFeatures = 5031001,
	kflidMoDerivAffMsa_ToMsFeatures = 5031002,
	kflidMoDerivAffMsa_FromPartOfSpeech = 5031003,
	kflidMoDerivAffMsa_ToPartOfSpeech = 5031004,
	kflidMoDerivAffMsa_FromInflectionClass = 5031005,
	kflidMoDerivAffMsa_ToInflectionClass = 5031006,
	kflidMoDerivAffMsa_FromExceptionFeatures = 5031007,
	kflidMoDerivAffMsa_AffixCategory = 5031008,
	kflidMoDerivAffMsa_FromStemName = 5031010,
	kflidMoDerivAffMsa_ToExceptionFeatures = 5031011,
	kflidMoDerivAffMsa_Stratum = 5031012,

	kclidMoDerivStepMsa = 5032,
	kflidMoDerivStepMsa_PartOfSpeech = 5032001,
	kflidMoDerivStepMsa_MsFeatures = 5032002,
	kflidMoDerivStepMsa_InflFeats = 5032003,
	kflidMoDerivStepMsa_InflectionClass = 5032004,
	kflidMoDerivStepMsa_ExceptionFeatures = 5032005,

	kclidMoForm = 5035,
	kflidMoForm_Form = 5035001,
	kflidMoForm_MorphType = 5035002,
	kflidMoForm_IsAbstract = 5035003,

	kclidMoInflAffixSlot = 5036,
	kflidMoInflAffixSlot_Name = 5036001,
	kflidMoInflAffixSlot_Description = 5036002,
	kflidMoInflAffixSlot_Optional = 5036004,

	kclidMoInflAffMsa = 5038,
	kflidMoInflAffMsa_InflFeats = 5038001,
	kflidMoInflAffMsa_AffixCategory = 5038002,
	kflidMoInflAffMsa_FromExceptionFeatures = 5038003,
	kflidMoInflAffMsa_PartOfSpeech = 5038005,
	kflidMoInflAffMsa_Slots = 5038007,

	kclidMoInflClass = 5039,
	kflidMoInflClass_Abbreviation = 5039001,
	kflidMoInflClass_Description = 5039002,
	kflidMoInflClass_Name = 5039003,
	kflidMoInflClass_Subclasses = 5039004,
	kflidMoInflClass_RulesOfReferral = 5039005,
	kflidMoInflClass_StemNames = 5039006,
	kflidMoInflClass_ReferenceForms = 5039007,

	kclidMoStemAllomorph = 5045,
	kflidMoStemAllomorph_PhoneEnv = 5045002,
	kflidMoStemAllomorph_StemName = 5045003,

	kclidMoStemName = 5047,
	kflidMoStemName_Abbreviation = 5047001,
	kflidMoStemName_Description = 5047002,
	kflidMoStemName_Name = 5047003,
	kflidMoStemName_Regions = 5047004,
	kflidMoStemName_DefaultAffix = 5047006,
	kflidMoStemName_DefaultStem = 5047007,

	kclidMoStratum = 5048,
	kflidMoStratum_Abbreviation = 5048001,
	kflidMoStratum_Description = 5048002,
	kflidMoStratum_Name = 5048003,
	kflidMoStratum_Phonemes = 5048004,

	kclidPartOfSpeech = 5049,
	kflidPartOfSpeech_InherFeatVal = 5049001,
	kflidPartOfSpeech_EmptyParadigmCells = 5049002,
	kflidPartOfSpeech_RulesOfReferral = 5049003,
	kflidPartOfSpeech_InflectionClasses = 5049004,
	kflidPartOfSpeech_AffixTemplates = 5049005,
	kflidPartOfSpeech_AffixSlots = 5049006,
	kflidPartOfSpeech_StemNames = 5049007,
	kflidPartOfSpeech_BearableFeatures = 5049008,
	kflidPartOfSpeech_InflectableFeats = 5049009,
	kflidPartOfSpeech_ReferenceForms = 5049010,
	kflidPartOfSpeech_DefaultFeatures = 5049011,
	kflidPartOfSpeech_DefaultInflectionClass = 5049012,
	kflidPartOfSpeech_CatalogSourceId = 5049013,

	kclidReversalIndex = 5052,
	kflidReversalIndex_PartsOfSpeech = 5052001,
	kflidReversalIndex_Entries = 5052003,
	kflidReversalIndex_WritingSystem = 5052004,

	kclidReversalIndexEntry = 5053,
	kflidReversalIndexEntry_Subentries = 5053002,
	kflidReversalIndexEntry_PartOfSpeech = 5053003,
	kflidReversalIndexEntry_Form = 5053004,
	kflidReversalIndexEntry_WritingSystem = 5053005,

	kclidWfiAnalysis = 5059,
	kflidWfiAnalysis_Category = 5059003,
	kflidWfiAnalysis_MsFeatures = 5059004,
	kflidWfiAnalysis_Stems = 5059005,
	kflidWfiAnalysis_Derivation = 5059006,
	kflidWfiAnalysis_Meanings = 5059010,
	kflidWfiAnalysis_MorphBundles = 5059011,
	kflidWfiAnalysis_CompoundRuleApps = 5059012,
	kflidWfiAnalysis_InflTemplateApps = 5059013,

	kclidPhNaturalClass = 5093,
	kflidPhNaturalClass_Name = 5093001,
	kflidPhNaturalClass_Description = 5093003,
	kflidPhNaturalClass_Abbreviation = 5093004,

	kclidPhEnvironment = 5097,
	kflidPhEnvironment_Name = 5097001,
	kflidPhEnvironment_Description = 5097002,
	kflidPhEnvironment_LeftContext = 5097004,
	kflidPhEnvironment_RightContext = 5097005,
	kflidPhEnvironment_AMPLEStringSegment = 5097006,
	kflidPhEnvironment_StringRepresentation = 5097007,

	kclidPhPhonData = 5099,
	kflidPhPhonData_PhonemeSets = 5099001,
	kflidPhPhonData_Environments = 5099002,
	kflidPhPhonData_NaturalClasses = 5099003,
	kflidPhPhonData_Contexts = 5099004,

	kclidMoGlossItem = 5109,
	kflidMoGlossItem_Name = 5109001,
	kflidMoGlossItem_Abbreviation = 5109002,
	kflidMoGlossItem_Type = 5109003,
	kflidMoGlossItem_AfterSeparator = 5109004,
	kflidMoGlossItem_ComplexNameSeparator = 5109005,
	kflidMoGlossItem_ComplexNameFirst = 5109006,
	kflidMoGlossItem_Status = 5109007,
	kflidMoGlossItem_FeatStructFrag = 5109008,
	kflidMoGlossItem_GlossItems = 5109009,
	kflidMoGlossItem_Target = 5109010,
	kflidMoGlossItem_EticID = 5109011,

	kclidMoUnclassifiedAffixMsa = 5117,
	kflidMoUnclassifiedAffixMsa_PartOfSpeech = 5117001,

	kclidLexRefType = 5119,
	kflidLexRefType_ReverseAbbreviation = 5119001,
	kflidLexRefType_MappingType = 5119002,
	kflidLexRefType_Members = 5119003,
	kflidLexRefType_ReverseName = 5119004,

	kclidLexReference = 5120,
	kflidLexReference_Comment = 5120001,
	kflidLexReference_Targets = 5120002,
	kflidLexReference_Name = 5120003,

	kclidLexEntryRef = 5127,
	kflidLexEntryRef_VariantEntryTypes = 5127001,
	kflidLexEntryRef_ComplexEntryTypes = 5127002,
	kflidLexEntryRef_PrimaryLexemes = 5127003,
	kflidLexEntryRef_ComponentLexemes = 5127004,
	kflidLexEntryRef_HideMinorEntry = 5127005,
	kflidLexEntryRef_Summary = 5127006,

	kclidLangProject = 6001,
	kflidLangProject_EthnologueCode = 6001001,
	kflidLangProject_WorldRegion = 6001002,
	kflidLangProject_MainCountry = 6001003,
	kflidLangProject_FieldWorkLocation = 6001004,
	kflidLangProject_PartsOfSpeech = 6001005,
	kflidLangProject_Texts = 6001006,
	kflidLangProject_TranslationTags = 6001007,
	kflidLangProject_Thesaurus = 6001009,
	kflidLangProject_WordformLookupLists = 6001011,
	kflidLangProject_AnthroList = 6001012,
	kflidLangProject_WordformInventory = 6001013,
	kflidLangProject_LexDb = 6001014,
	kflidLangProject_ResearchNotebook = 6001015,
	kflidLangProject_AnalysisWss = 6001017,
	kflidLangProject_CurVernWss = 6001018,
	kflidLangProject_CurAnalysisWss = 6001019,
	kflidLangProject_CurPronunWss = 6001020,
	kflidLangProject_MsFeatureSystem = 6001021,
	kflidLangProject_MorphologicalData = 6001022,
	kflidLangProject_Styles = 6001023,
	kflidLangProject_Filters = 6001024,
	kflidLangProject_ConfidenceLevels = 6001025,
	kflidLangProject_Restrictions = 6001026,
	kflidLangProject_WeatherConditions = 6001027,
	kflidLangProject_Roles = 6001028,
	kflidLangProject_AnalysisStatus = 6001029,
	kflidLangProject_Locations = 6001030,
	kflidLangProject_People = 6001031,
	kflidLangProject_Education = 6001032,
	kflidLangProject_TimeOfDay = 6001033,
	kflidLangProject_AffixCategories = 6001034,
	kflidLangProject_PhonologicalData = 6001035,
	kflidLangProject_Positions = 6001036,
	kflidLangProject_Overlays = 6001037,
	kflidLangProject_AnalyzingAgents = 6001038,
	kflidLangProject_TranslatedScripture = 6001040,
	kflidLangProject_VernWss = 6001041,
	kflidLangProject_ExtLinkRootDir = 6001042,
	kflidLangProject_SortSpecs = 6001043,
	kflidLangProject_Annotations = 6001044,
	kflidLangProject_UserAccounts = 6001045,
	kflidLangProject_ActivatedFeatures = 6001046,
	kflidLangProject_AnnotationDefs = 6001047,
	kflidLangProject_Pictures = 6001048,
	kflidLangProject_SemanticDomainList = 6001049,
	kflidLangProject_CheckLists = 6001050,
	kflidLangProject_Media = 6001051,
};

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /E:4096 /C c:\\FW\\Bin\\mkcel.bat"
// End:

#endif // !FwXmlData_H
