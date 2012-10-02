/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, 2009 SIL International. All rights reserved.

File: FwXmlImport.cpp
Responsibility: Steve McConnel
Last reviewed:

	This file contains the XML import methods for the FwXmlData class/interface.

STDMETHODIMP FwXmlData::LoadXml(BSTR bstrFile, IAdvInd * padvi)
STDMETHODIMP FwXmlData::ImportXmlObject(BSTR bstrFile, int hvoOwner, int flid, IAdvInd * padvi)
STDMETHODIMP FwXmlData::ImportMultipleXmlFields(BSTR bstrFile, int hvoOwner, IAdvInd * padvi)
STDMETHODIMP FwXmlData::UpdateListFromXml(BSTR bstrFile, int hvoOwner, int flid, IAdvInd *padvi)
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include "FwXml.h"
#include <io.h>

#undef THIS_FILE
DEFINE_THIS_FILE

//#define LOG_SQL 1
//#define MAXIMUM_BATCHING 1

#define IMPORTCMDSTRING StrAnsiBufHuge
#define READ_SIZE 16384

#ifndef MAKECLIDFROMFLID
#define MAKECLIDFROMFLID(flid) ((flid) / 1000)
#endif
//:End Ignore

// SQL Server allows a maximum of 900 bytes in data that it indexes within a table.
static const int kcchMaxIndexSize = 450;

/*----------------------------------------------------------------------------------------------
	Distinguish among the various fundamental kinds of XML elements.

	Hungarian: elty
----------------------------------------------------------------------------------------------*/
typedef enum ElementType
{
	keltyDatabase = 1,
	keltyAddProps,
	keltyDefineProp,
	keltyObject,
	keltyCustomProp,
	keltyPropName,
	keltyVirtualProp,
	keltyBasicProp,
	keltyUpdateMerge,	// the next two items were added for UpdateListFromXml
	keltyUpdateDelete,
	keltyBad = 0
} ElementType;


/*----------------------------------------------------------------------------------------------
	Distinguish among the various types of LexicalReference objects.

	Hungarian: mt
----------------------------------------------------------------------------------------------*/
typedef enum MappingTypes
{
	kmtSenseCollection = 0,
	kmtSensePair = 1,
	kmtSenseAsymmetricPair = 2,		// Sense Pair with different Forward/Reverse names
	kmtSenseTree = 3,
	kmtSenseSequence = 4,
	kmtEntryCollection = 5,
	kmtEntryPair = 6,
	kmtEntryAsymmetricPair = 7,		// Entry Pair with different Forward/Reverse names
	kmtEntryTree = 8,
	kmtEntrySequence = 9,
	kmtEntryOrSenseCollection = 10,
	kmtEntryOrSensePair = 11,
	kmtEntryOrSenseAsymmetricPair = 12,
	kmtEntryOrSenseTree = 13,
	kmtEntryOrSenseSequence = 14
} MappingTypes;

/*----------------------------------------------------------------------------------------------
	ElemTypeInfo stores the element type and other basic information for a particular XML tag.
	A stack of these is maintained during parsing, one for each open XML element.

	Hungarian: eti
----------------------------------------------------------------------------------------------*/
struct ElemTypeInfo
{
	// Determine what overall type of element this is: Object, Field, or Basic Property.
	ElementType m_elty;
	union
	{
		// For keltyObject, index into the class meta-data table (FwXmlData::m_fdci).
		int m_icls;
		// For keltyPropName, index into the field meta-data table (FwXmlData::m_fdfi).
		int m_ifld;
		// For keltyBasicProp, a more exact (but possibly not totally exact) type.
		int m_cpt;
	};
};

/*----------------------------------------------------------------------------------------------
	SeqPropInfo contains the information needed to handle missing ord attributes for sequence
	properties.  A stack of these is maintained, one for each open property.

	Hungarian: spi
----------------------------------------------------------------------------------------------*/
struct SeqPropInfo
{
	bool m_fSeq;		// Flag whether this is a sequence attribute.
	int m_cobj;			// Number of objects loaded for this sequence.
	int m_cord;			// Number of "ord" attributes loaded for this sequence.
	int m_ord;			// If m_cord = 0, next ord value to use.
};

/*----------------------------------------------------------------------------------------------
	RootObjData contains the basic object information which accumulates during the first pass
	through the XML file.
	The individual elements of this structure are vectors to facilitate using them in storing
	data into the database with column-wise binding.

	Hungarian: rod
----------------------------------------------------------------------------------------------*/
struct RootObjData
{
/*
CreateObject$
	@clid int,
	@id int output,
	@guid uniqueidentifier output
 */
	Vector<int> m_vicls;	// Indexes into the class meta-data table.
	Vector<int> m_vhobj;	// Object database ids ("select Id from CmObject").
	Vector<GUID> m_vguid;	// Object database GUIDs ("select Guid$ from CmObject")
};

/*----------------------------------------------------------------------------------------------
	OwnedObjData contains the object ownership information which accumulates during the first
	pass through the XML file.
	The individual elements of this structure are vectors to facilitate using them in storing
	data into the database with column-wise binding.

	Hungarian: ood
----------------------------------------------------------------------------------------------*/
struct OwnedObjData
{
/*
CreateOwnedObject$
	@clid int,
	@id int output,
	@guid uniqueidentifier output,
	@owner int,
	@ownFlid int,
	@type int,			-- type of field (atomic, collection, or sequence)

	@StartObj int = null,		-- object to insert before - owned sequences
	@fGenerateResults tinyint = 0,	-- default to not generating results
	@nNumObjects int = 1,		-- number of objects to create
	@uid uniqueidentifier = null output -- if nNumObjects != 1
 */
	//:> EXEC CreateOwned_<CLASS> ?,?,?,?,? - id, guid, owner, ownerflid, ownord (=null)
	Vector<int> m_vicls;		// Indexes into the class meta-data table.
	Vector<int> m_vhobj;		// Object database ids (CmObject:  Id).
	Vector<GUID> m_vguid;		// Database object GUIDs (CmObject: Guid$).
	Vector<int> m_vhobjOwner;	// Database ids of the owning object (CmObject: Owner$).
	Vector<int> m_vfidOwner;	// Field codes of the owning object (CmObject: OwnerFlid$).
	Vector<int> m_vordOwner;	// Sorting order codes for the owning object (CmObject: OwnOrd$)
	Vector<int> m_vcpt;			// kcptOwningAtom, kcptOwningCollection, or kcptOwningSequence

	void Clear()
	{
		m_vicls.Clear();
		m_vhobj.Clear();
		m_vguid.Clear();
		m_vhobjOwner.Clear();
		m_vfidOwner.Clear();
		m_vordOwner.Clear();
		m_vcpt.Clear();
	}
};


/*----------------------------------------------------------------------------------------------
	ExistingObjData contains the object ownership information which accumulates during the first
	pass through the XML file.  The objects represented here are always atomic values, ie, owned
	by an OwningAtom type relationship.

	Hungarian: eod
----------------------------------------------------------------------------------------------*/
struct ExistingObjData
{
	int hobj;		// Object database ids (CmObject:  Id).
	int icls;		// Indexes into the class meta-data table.
	int hobjOwner;	// Database ids of the owning object (CmObject: Owner$).
	int fidOwner;	// Field codes of the owning object (CmObject: OwnerFlid$).
};

/*----------------------------------------------------------------------------------------------
	OwnerField contains an object's owner database id and field id.  For atomic owning
	relationships, this uniquely determines the object.

	Hungarian: of
----------------------------------------------------------------------------------------------*/
struct OwnerField
{
	int hobjOwner;
	int fid;
};

/*----------------------------------------------------------------------------------------------
	StoredData contains miscellaneous data collected during the second pass through the XML
	file.  It is used in an array indexed by field type.

	Hungarian: stda
----------------------------------------------------------------------------------------------*/
struct StoredData
{
	Vector<int> m_vhobj;			// Database ids for the object itself.
	Vector<int> m_vhobjDst;			// Database ids for Reference{Atom|Collection|Sequence}.
	Vector<int> m_vord;				// Order codes for Reference{Atom|Collection|Sequence}.
	Vector<StrUni> m_vstu;			// Text for Formatted Strings or Unicode.
	Vector<Vector<byte> > m_vvbFmt;	// Formatting for Formatted Strings.
};

/*----------------------------------------------------------------------------------------------
	MultiTxtData contains MultiUnicode information which accumulates during the second pass
	through the XML file.

	Hungarian: mtd
----------------------------------------------------------------------------------------------*/
struct MultiTxtData
{
	//:> EXEC SetMultiTxt$ ?,?,?,?  -- flid, obj, ws, txt
	Vector<int> m_vfid;		// Field codes of the owning object (MultiTxt$: Flid).
	Vector<int> m_vhobj;	// Database ids of the owning object (MultiTxt$: Obj).
	Vector<int> m_vws;		// Writing Systems for the text data (MultiTxt$: Ws).
	Vector<StrUni> m_vstu;	// Text data (MultiTxt$: Txt).
};

/*----------------------------------------------------------------------------------------------
	MultiStrData contains MultiString information which accumulates during the second pass
	through the XML file.

	Hungarian: msd
----------------------------------------------------------------------------------------------*/
struct MultiStrData
{
	//:> EXEC SetMultiStr$ ?,?,?,?,?  -- flid, obj, ws, txt, fmt
	Vector<int> m_vfid;				// Field codes of the owning object (MultiStr$: Flid).
	Vector<int> m_vhobj;			// Database ids of the owning object (MultiStr$: Obj).
	Vector<int> m_vws;				// Writing Systems for the text data (MultiStr$: Ws).
	Vector<StrUni> m_vstu;			// Text data (MultiStr$: Txt)
	Vector<Vector<byte> > m_vvbFmt;	// Formatting data (MultiStr$: Fmt)
};

const int kcstrMax = 1024;		// Maximum number of strings to store at once.
const int kcstrBigMax = 512;	// Maximum number of "big" strings to store at once.
const int kceSeg = 10000;	// Maximum number of object ownerships to set at once.
// These result in ~5MB maximum temp allocation while storing formatted strings.
const int kcchTxtMaxBundle = 1000;	// was 2047
const int kcbFmtMaxBundle = 1024;

//:> REVIEW SteveMc: is this reasonable, avoiding SQLBindParameter for small numbers of items?
const int kceParamMin = 5;	// Minimum number of items for parametered SQL commands.

/*----------------------------------------------------------------------------------------------
	CustomFieldInfo stores the data for a <CustomField> element until everything is known and
	the row can be added to the Field$ table.  Note that the Field$ table does not allow
	updates, so it's rather painful to create a custom field with partial information and fix it
	later.

	Hungarian: cfi
----------------------------------------------------------------------------------------------*/
struct CustomFieldInfo
{
	int m_fid;
	int m_cpt;
	int m_cid;
	int m_cidDst;				// 0 <=> null
	StrUni m_stuName;
	int m_fCustom;
	StrAnsiBuf m_stabMin;
	StrAnsiBuf m_stabMax;
	StrAnsiBuf m_stabBig;
	StrUni m_stuUserLabel;
	StrUni m_stuHelpString;
	StrAnsiBuf m_stabListRootId;
	StrAnsiBuf m_stabWsSelector;
	StrUni m_stuXmlUI;
};

/*----------------------------------------------------------------------------------------------
	ListIdentity stores the information needed to identify a particular list in a particular
	writing system.

	Hungarian: lid
----------------------------------------------------------------------------------------------*/
struct ListIdentity
{
	int m_hobj;		// The database id of the list.
	int m_ws;		// The writing system id we're interested in.
};


/*----------------------------------------------------------------------------------------------
	ListInfo stores the data for a CmPossibilityList object.  This is used to match against
	<Link> attributes when target is not given during object import.

	Hungarian: li
----------------------------------------------------------------------------------------------*/
struct ListInfo
{
	StrUni m_stuListName;
	bool m_fIsClosed;
	HashMapStrUni<int> * m_phmsuNamehobj;
	HashMapStrUni<int> * m_phmsuAbbrhobj;
	int m_ws;

	ListInfo()
	{
		m_fIsClosed = false;
		m_phmsuNamehobj = NULL;
		m_phmsuAbbrhobj = NULL;
		m_ws = 0;
	}
	~ListInfo()
	{
		if (m_phmsuNamehobj)
		{
			m_phmsuNamehobj->Clear();
			delete m_phmsuNamehobj;
		}
		if (m_phmsuAbbrhobj)
		{
			m_phmsuAbbrhobj->Clear();
			delete m_phmsuAbbrhobj;
		}
	}
	void Init(int ws = 0)
	{
		m_phmsuNamehobj = NewObj HashMapStrUni<int>;
		m_phmsuAbbrhobj = NewObj HashMapStrUni<int>;
		m_ws = ws;
	}
};

/*----------------------------------------------------------------------------------------------
	LexRelationInfo stores the data for one lexical relationship link. This information is
	accumulated during pass 2 and processed at the end of second pass of the xml parse.

	Hungarian: lri
----------------------------------------------------------------------------------------------*/
struct LexRelationInfo
{
	int m_hobj;
	bool m_fSense;
	StrAnsi m_staSense; // or LexEntry
	int m_wsv;
	int m_hvoLexRefType;
	int m_hvoTarget;
	bool m_fReverse;
	int m_ord;
	LexRelationInfo * m_plriNext;	// chain collection together for a single sense/entry.
};

/*----------------------------------------------------------------------------------------------
	HvoVector wraps a Vector<int> inside a struct so that it can be used as the target of a
	HashMap. This ensures that the destructor for the Vector gets called, and that the vector
	can be updated if needed.

	Hungarian: hv
----------------------------------------------------------------------------------------------*/
struct HvoVector
{
	Vector<int> * m_pvhvo;
	HvoVector()
	{
		m_pvhvo = NULL;
	}
	~HvoVector()
	{
		if (m_pvhvo)
			delete m_pvhvo;
		m_pvhvo = NULL;
	}
};

/*----------------------------------------------------------------------------------------------
	LexReference stores the data for one lexical reference object.  m_hvoTop is set only for
	tree type relations.  m_vhvo is used only for sequence type relations.

	Hungarian: lr
----------------------------------------------------------------------------------------------*/
struct LexReference
{
	Set<int> m_setHvo;
	int m_hvoTop;
	Vector<int> m_vhvo;
	LexReference()
	{
		m_hvoTop = 0;
	}
};

/*----------------------------------------------------------------------------------------------
	LexReferenceVec wraps a Vector<LexReference> inside a struct so that it can be used as the
	target of a HashMap. This ensures that the destructor for the Vector gets called, and that
	the vector can be updated if needed.

	Hungarian: lrv
----------------------------------------------------------------------------------------------*/
struct LexReferenceVec
{
	Vector<LexReference> * m_pvlr;
	LexReferenceVec()
	{
		m_pvlr = NULL;
	}
	~LexReferenceVec()
	{
		if (m_pvlr)
			delete m_pvlr;
		m_pvlr = NULL;
	}
};

/*----------------------------------------------------------------------------------------------
	EntryOrSenseLinkInfo stores the data for one EntryOrSense link. This information is
	accumulated during pass 2 and processed at the end of second pass of the xml parse.
	The information stored is subset of that used for LexRelationInfo.

	Hungarian: mesi
----------------------------------------------------------------------------------------------*/
struct EntryOrSenseLinkInfo
{
	int m_hobj;
	int m_ws;
	int m_flid;
	bool m_fSense;
	StrAnsi m_staEntry;
	int m_hvoTarget;
};


/*----------------------------------------------------------------------------------------------
	This class is used to record which objects have already been created in the database.

	Hungarian: cos
----------------------------------------------------------------------------------------------*/
class CreatedObjectSet
{
public:
	/*------------------------------------------------------------------------------------------
		Constructor.

		@param cobj Number of objects for initial internal memory allocation.
	------------------------------------------------------------------------------------------*/
	CreatedObjectSet(int cobj)
	{
		long nZero = 0L;
		m_cobj = cobj;
		m_vgrfCreated.Resize((cobj + 31) / 32, nZero);		// Assume >= 32 bits per long.
	};
	// Destructor.
	~CreatedObjectSet()
	{
		m_vgrfCreated.Clear();
		m_setHobjCreated.Clear();
	}
	/*------------------------------------------------------------------------------------------
		Mark a given object as being created.

		@param hobj Database id for a created object.
	------------------------------------------------------------------------------------------*/
	void AddObject(int hobj)
	{
		if ((unsigned)hobj < (unsigned)m_cobj)
		{
			long grfMask = 1 << (hobj % 32);
			m_vgrfCreated[hobj / 32] |= grfMask;
		}
		else if (!m_setHobjCreated.IsMember(hobj))
		{
			m_setHobjCreated.Insert(hobj);
		}
	}
	/*------------------------------------------------------------------------------------------
		Check whether a given object has been created already.

		@param hobj Database id for an object.

		@return True if the object has been created, otherwise false.
	------------------------------------------------------------------------------------------*/
	bool IsCreated(int hobj)
	{
		if ((unsigned)hobj < (unsigned)m_cobj)
		{
			long grfMask = 1 << (hobj % 32);
			return m_vgrfCreated[hobj / 32] & grfMask ? true : false;
		}
		else
		{
			return m_setHobjCreated.IsMember(hobj);
		}
	}
protected:
	int m_cobj;			// Number of objects allocated in m_vgrfCreated.
	// Flags for each object for whether the object with the given database id has been created.
	// The flags are packed 32 per vector element.
	Vector<long> m_vgrfCreated;
	// For hobj > m_cobj, set contains the database ids (hobj) of created objects.
	Set<int> m_setHobjCreated;
};


/*----------------------------------------------------------------------------------------------
	RawStringData hold the various arrays used to hold data in storing the various forms of
	string data.

	Hungarian: rsd
----------------------------------------------------------------------------------------------*/
struct RawStringData
{
	Vector<int> vhobj;
	Vector<SQLINTEGER> vcbhobj;
	Vector<int> vfid;
	Vector<SQLINTEGER> vcbfid;
	Vector<int> vws;
	Vector<SQLINTEGER> vcbws;
	Vector<wchar> vchTxt;
	Vector<SQLINTEGER> vcbTxt;
	Vector<byte> vbFmt;
	Vector<SQLINTEGER> vcbFmt;
	Vector<SQLUSMALLINT> vnParamStatus;
	SQLUINTEGER cParamsProcessed;
	SQLINTEGER cchTxtLine;
	SQLINTEGER cbTxtLine;

	RawStringData()
	{
		cParamsProcessed = 0;
		cchTxtLine = cbTxtLine = 0;
	}
	RawStringData(int cstr, SQLINTEGER cchTxtMax)
	{
		Initialize(cstr, cchTxtMax);
	}
	RawStringData(int cstr, SQLINTEGER cchTxtMax, SQLINTEGER cbFmtMax)
	{
		Initialize(cstr, cchTxtMax, cbFmtMax);
	}
	RawStringData(int cstr, SQLINTEGER cchTxtMax, SQLINTEGER cbFmtMax, bool fMulti)
	{
		Initialize(cstr, cchTxtMax, cbFmtMax, fMulti);
	}

	void Initialize(int cstr, SQLINTEGER cchTxtMax, SQLINTEGER cbFmtMax = 0,
		bool fMulti = false)
	{
		cchTxtLine = cchTxtMax + 1;
		cbTxtLine = cchTxtLine * isizeof(wchar);

		vhobj.Resize(cstr);
		vcbhobj.Resize(cstr);
		vchTxt.Resize(cstr * (cchTxtMax + 1));
		vcbTxt.Resize(cstr);
		if (cbFmtMax)
		{
			vcbFmt.Resize(cstr);
			vbFmt.Resize(cstr * cbFmtMax);
		}
		else
		{
			vbFmt.Clear();
			vcbFmt.Clear();
		}
		if (fMulti)
		{
			vfid.Resize(cstr);
			vcbfid.Resize(cstr);
			vws.Resize(cstr);
			vcbws.Resize(cstr);
		}
		else
		{
			vfid.Clear();
			vcbfid.Clear();
			vws.Clear();
			vcbws.Clear();
		}
		vnParamStatus.Resize(cstr);
		cParamsProcessed = 0;
	}
};


/*----------------------------------------------------------------------------------------------
	This data structure holds the arrays of data for creating empty structured texts.

	Hungarian: stfi
----------------------------------------------------------------------------------------------*/
struct StTextFieldInfo
{
	Vector<long> vhobjOwner;
	Vector<long> vfid;
	Vector<long> vcid;
	Vector<long> vcpt;
	Vector<long> vhobj;
	Vector<SQLUSMALLINT> vnParamStatus;
};


/*----------------------------------------------------------------------------------------------
	FwXmlImportData serves two purposes: a placeholder for the XML parser callback functions,
	to make it easier for all of them to be "friends" of FwXmlData, and a convenient grouping
	of the various data items used throughout XML import by the various callback functions.

	Note that a pointer to an object of this type is passed as the "User Data" to the callback
	functions.

	Hungarian: xid
----------------------------------------------------------------------------------------------*/
class FwXmlImportData
{
public:
	//:> Constructor and Destructor.

	FwXmlImportData(FwXmlData * pfwxd);
	~FwXmlImportData();

	//:> Other methods.

	void LogMessage(const char * pszMsg);
	void ExecuteSimpleSQL(const char * pszCmd, int cline);
	void ExecuteSimpleUnicodeSQL(const wchar * pwszCmd, int cline);
	void ExecuteParameterizedSQL(const char * pszCmd, int cline, const char * pszParam);
	void ExecuteParameterizedSQL(const char * pszCmd, int cline, const char * pszFirst,
		const char * pszSecond);
	int ReadOneIntFromSQL(const char * pszCmd, int cline, bool & fIsNull);
	int ReadOneIntFromParameterizedSQL(const char * pszCmd, int cline, const char * pszParam,
		bool & fIsNull);
	void VerifySqlRc(RETCODE rc, SQLHANDLE hstmt, int cline, const char * pszCmd = NULL);
protected:
	virtual ElemTypeInfo GetElementType(const char * pszElement);
public:
	void SetFieldOrClassType(const char * pszElement, ElemTypeInfo & eti);
	int GetCustomFieldIndex(const char * pszField, int cid);
	void CreateCustomFields();
	void CreateAddCustomFieldSql(const CustomFieldInfo & cfi);
	void BindAddCustomFieldParameters(SQLHSTMT hstmt, const CustomFieldInfo & cfi,
		SQLINTEGER * rgn);
	void CreateObjects(IAdvInd * padvi);
	void ReleaseExcessSpace();
	void CreateFewRootObjects(IAdvInd * padvi, SQLHSTMT hstmt, int cobj, int & cCreated,
		int & cStep);
	void CreateManyRootObjects(IAdvInd * padvi, SQLHSTMT hstmt, int cobj, int & cCreated,
		int & cStep);
	void CheckRootObjParamsForSuccess(SQLUSMALLINT * rgnParamStatus, int cParamsProcessed,
		int ieSegStart, int cobj);
	void ReportCreateProgress(IAdvInd * padvi, int cobj, int & cCreated, int & cStep);
	void RecordCreatedRootObjects(CreatedObjectSet & cos);
	void CreateOwnedObjects(IAdvInd * padvi, CreatedObjectSet & cos, int & cCreated,
		int & cStep);
	int CollectOwnedObjects(CreatedObjectSet & cos, OwnedObjData & ood, int & iobjMin,
		int & iobjMax, int & cobjAdded, int & cobjRemaining);
	void CreateFewOwnedObjects(IAdvInd * padvi, SQLHSTMT hstmt, int cobjPass,
		OwnedObjData & ood, int & cCreated, int & cStep);
	void CreateManyOwnedObjects(IAdvInd * padvi, SQLHSTMT hstmt, int cobjPass,
		OwnedObjData & ood, int & cCreated, int & cStep);
	void CreateSomeOwnedObjects(SQLHSTMT hstmt, OwnedObjData & ood, int ieSegStart, int cobj,
		SQLUSMALLINT * rgnParamStatus, SQLUINTEGER & cParamsProcessed);
	void CheckOwnedObjParamsForSuccess(OwnedObjData & ood, SQLUSMALLINT * rgnParamStatus,
		int cParamsProcessed, int ieSegStart, int cobj);
	void FixCustomListRefFields();

	void StoreReferenceAtoms(int ifld);
	void StoreFewReferenceAtoms(int ifld, int icls, int crefTotal);
	void StoreManyReferenceAtoms(int ifld, int icls, int crefTotal);
	void CheckReferenceAtomParamsForSuccess(int icls, int ifld, int ieSegStart,
		SQLUINTEGER cParamsProcessed, SQLUSMALLINT * rgnParamStatus);

	void StoreReferenceCollections(int ifld);
	void StoreFewReferenceCollections(int ifld, int icls, int crefTotal);
	void StoreManyReferenceCollections(int ifld, int icls, int crefTotal);
	void CheckReferenceCollectionParamsForSuccess(int icls, int ifld, int ieSegStart,
		SQLUINTEGER cParamsProcessed, SQLUSMALLINT * rgnParamStatus);

	void StoreReferenceSequences(int ifld);
	void StoreFewReferenceSequences(int ifld, int icls, int crefTotal);
	void StoreManyReferenceSequences(int ifld, int icls, int crefTotal);
	void CheckReferenceSequenceParamsForSuccess(int icls, int ifld, int ieSegStart,
		SQLUINTEGER cParamsProcessed, SQLUSMALLINT * rgnParamStatus);

	void StoreUnicodeData(int ifld);
	int NormalizeUnicodeData(int ifld, SQLINTEGER & cchTxtMax);
	void StoreSmallUnicodeData(int ifld, int icls, int cpt, int cstr, int cstrSizeOk,
		SQLINTEGER cchTxtMax);
	void CheckUnicodeParamsForSuccess(const RawStringData & rsd, int icls, int ifld);
	void StoreLargeUnicodeData(int ifld, int icls, int cpt, int cstr);

	void StoreStringData(int ifld);
	int NormalizeStringData(int ifld, SQLINTEGER & cchTxtMax, SQLINTEGER & cbFmtMax);
	void NormalizeOneString(StrUni & stu, Vector<byte> & vbFmt);
	void StoreSmallStringData(int ifld, int icls, int cpt, int cstr, int cstrSizeOk,
		SQLINTEGER cchTxtMax, SQLINTEGER cbFmtMax);
	void StoreLargeStringData(int ifld, int icls, int cpt, int cstr);
	void CheckStringParamsForSuccess(const RawStringData & rsd, int icls, int ifld);

	void StoreMultiUnicode();
	int NormalizeMultiUnicodeData(SQLINTEGER & cchTxtMax);
	void StoreSmallMultiUnicode(int cstr, int cstrSizeOk, SQLINTEGER cchTxtMax);
	void StoreLargeMultiUnicode(int cstr);
	void CheckMultiUnicodeParamsForSuccess(const RawStringData & rsd);

	virtual void StoreMultiBigUnicode();
	int NormalizeMultiBigUnicodeData(SQLINTEGER & cchTxtMax);
	void StoreSmallMultiBigUnicode(int cstr, int cstrSizeOk, SQLINTEGER cchTxtMax);
	void StoreLargeMultiBigUnicode(int cstr);
	void CheckMultiBigUnicodeParamsForSuccess(const RawStringData & rsd);

	void StoreMultiString();
	int NormalizeMultiStringData(SQLINTEGER & cchTxtMax, SQLINTEGER & cbFmtMax);
	void StoreSmallMultiStringData(int cstr, int cstrSizeOk, SQLINTEGER cchTxtMax,
		SQLINTEGER cbFmtMax);
	void StoreLargeMultiStringData(int cstr);
	void CheckMultiStringParamsForSuccess(const RawStringData & rsd);

	virtual void StoreMultiBigString();
	int NormalizeMultiBigStringData(SQLINTEGER & cchTxtMax, SQLINTEGER & cbFmtMax);
	void StoreSmallMultiBigStringData(int cstr, int cstrSizeOk, SQLINTEGER cchTxtMax,
		SQLINTEGER cbFmtMax);
	void StoreLargeMultiBigStringData(int cstr);
	void CheckMultiBigStringParamsForSuccess(const RawStringData & rsd);

	void TimeStampObjects();
	void UpdateDttm();
	void SetCmPossibilityColors();
	void FixStStyle_Rules();
	int CreateMsaForEntry(int cid, int hobjEntry);
	void FixLexSenseMSAs();
	void ProcessPropAttributes(const XML_Char * pszName, const XML_Char ** prgpszAtts,
		Vector<TextProps::TextIntProp> & vtxip, Vector<TextProps::TextStrProp> & vtxsp);
	void SetAlignProperty(const XML_Char * pszName, Vector<TextProps::TextIntProp> & vtxip,
		const char * pszAlign);
	void SetColorProperty(const XML_Char * pszName, Vector<TextProps::TextIntProp> & vtxip,
		int scp, const char * pszColor);
	void SetTextToggleProperty(const XML_Char * pszName, Vector<TextProps::TextIntProp> & vtxip,
		int scp, int stidError, const char * pszToggleVal);
	void SetMilliPointProperty(const XML_Char * pszName, Vector<TextProps::TextIntProp> & vtxip,
		int scp, const char * pszVal);
	void SetEnumeratedProperty(const XML_Char * pszName, Vector<TextProps::TextIntProp> & vtxip,
		int scp, const char * pszEnumVal);
	void SetNumericProperty(const XML_Char * pszName, Vector<TextProps::TextIntProp> & vtxip,
		int scp, const char * pszVal);
	void SetWsProperty(const XML_Char * pszName, Vector<TextProps::TextIntProp> & vtxip,
		int scp, const char * pszWs);
	void SetSizeAndUnitProperties(const XML_Char * pszName,
		Vector<TextProps::TextIntProp> & vtxip, const char * pszSize, const char * pszUnit,
		const char * pszType, int scpSize, int stidBadSize, int stidBadUnit, int stidNoSize);
	void SetSuperscriptProperty(const XML_Char * pszName,
		Vector<TextProps::TextIntProp> & vtxip, const char * pszSuperscript);
	void SetUnderlineProperty(const XML_Char * pszName, Vector<TextProps::TextIntProp> & vtxip,
		const char * pszUnderline);
	void SetSpellCheckProperty(const XML_Char * pszName, Vector<TextProps::TextIntProp> & vtxip,
		const char * pszSpellCheck);


	void CreateEmptyTextFields();
	void GetEmptyStructuredTextFields(StTextFieldInfo & stfi);
	void BindStTextParameters(SQLHSTMT hstmt, StTextFieldInfo & stfi, int iePass,
		int cobjPass);
	void CheckStTextParamsForSuccess(const StTextFieldInfo & stfi,
		SQLUINTEGER cParamsProcessed, int iePass, int cobjPass);
	RETCODE CreateEmptyStTxtParagraphs(SQLHSTMT hstmt, StTextFieldInfo & stfi, int iePass,
		int cobjPass, int cobjTotal);
	void CheckStTxtParagraphParamsForSuccess(const StTextFieldInfo & stfi,
		SQLUINTEGER cParamsProcessed, int iePass, int cobjPass);

	virtual void StoreData(IAdvInd * padvi, int nPercent = 15);
	void StoreLexicalReferences();
	void StoreEntryOrSenseLinks();
	void StoreEntryOrSenseLinks(HashMap<int, HvoVector> & hmhvovhvo, const char * pszTable);
	void DeleteObjects(Vector<int> & vhobj, IAdvInd * padvi = NULL, int nPercent = 0);
	void DeleteObjects(const StrUni & stuHvoList);
	void RemoveRedundantEntries();
	void MergeLexEntries(int hobjOld, int hobjNew);
	void CopyCustomFieldData(int hobjOld, int hobjNew);
	int EntryOrSenseHvo(int hobj, bool fSense, StrAnsi & staEntry, int hvoLexRefType);
	int CreateDummyEntry(const char * pszForm, bool fSense);
	void CreateSimpleFormat(int ws, Vector<byte> & vbFmt);
	void AppendImportResidue(int hobj, bool fSense, StrAnsi & staEntry, int hvoLexRefType);
	void StoreImportResidue(StrUni & stuResidue, Vector<byte> & vbFmt, int hobj,
		const char * pszTableName);
	void LoadExistingImportResidue(int hobj, const char * pszTableName, StrUni & stuResidue,
		Vector<byte> & vbFmt);

	bool InitializeLexReference(int nMappingType, LexReference & lr, LexRelationInfo & lri);
	void CreateDbLexReference(int hvoLexRefType, LexReference & lr);
	bool AddToLexReferenceIfPossible(int nMappingType, LexReference & lr,
		LexRelationInfo & lri);
	bool MergeLexReferenceCollections(LexReference & lr, LexRelationInfo & lri);
	void FillHashMapHvoToSenses(const char * pszSQL, HashMap<int, HvoVector> & hmhvovhvo);
	void FillHashMapHeadwordToHvo(HashMapStrUni<int> & hmsuhvo, int ws);
	void FillHashMapHvoToInt(const char * pszSQL, HashMap<int, int> & hmhvon);
	int TraceSenseNumber(int hvoOwner, const char * pszSenseNum, int depth);
	int GenerateNextNewHvo();

	void StoreWsProps();
	int GetUserWs();
	int GetWsFromIcuLocale(const char * pszWs, int stidErrMsg);
	void EnsureEmptyDatabase();
	void ReportFirstPassTime(long timDelta);
	void ReportCustomFields();
	void ReportCreatingObjects(long timDelta);
	void ReportSecondPassTime(long timDelta);
	void ReportDataStorageStats(long timDelta);
	void ReportTotalTime(long timDelta);
	void EnsureValidImportArguments(int hvoOwner, int flid, int & ifld, int & icls);
	int GetClassIndexOfObject(int hvoOwner);
	void LoadPossibleExistingObjectId(int hvoOwner, int flid, int & hvoObj, GUID & guidObj);
	int GetNextRealObjectId();
	void ReportTime(long timDelta, int stid);

protected:
	virtual void StartObject1(const ElemTypeInfo & eti, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	virtual void StartPropName1(ElemTypeInfo & eti, const XML_Char * pszName,
		const char * pszProp = NULL);
	virtual void StartBasicProp1(const ElemTypeInfo & eti, const XML_Char * pszName);
	const ElemTypeInfo & CheckObjectNesting(const XML_Char * pszName);
	const ElemTypeInfo & CheckPropNameNesting(const XML_Char * pszName);
	const ElemTypeInfo & CheckBasicPropNesting(const XML_Char * pszName);
	int GetOwnerOfCurrentObject(const ElemTypeInfo & etiProp, const XML_Char * pszName);
	int GetObjectIdAndGuid(const XML_Char ** prgpszAtts, GUID & guidObj);
	void LoadIndexedStringFields();

public:
	void PushRootObjectInfo(int hobj, GUID guidObj, int icls);
	void PushOwnedObjectInfo(int hobj, GUID guidObj, int icls, int hobjOwner, int ifld,
		const XML_Char * pszName, const XML_Char ** prgpszAtts);

	void ThrowWithLogMessage(int stid);
	void ThrowWithLogMessage(int stid, const void * psz1);
	void ThrowWithLogMessage(int stid, const void * psz1, const void * psz2);
	void ShowElemTypeStack();

	virtual void StartObject2(ElemTypeInfo & eti, const XML_Char * pszName);
	void StartCustomProp2(ElemTypeInfo & eti, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	void StartPropName2(ElemTypeInfo & eti);
	void StartVirtualProp2(ElemTypeInfo & eti, const XML_Char *pszName);
	void StartBasicProp2(ElemTypeInfo & eti, const XML_Char * pszName,
		const XML_Char ** prgpszAtts, StrAnsiBuf & stabCmd);
	int GetOwnerClassIdForBasicProp(ElemTypeInfo & etiProp, int ifld);
	void SetCommandForBooleanValue(StrAnsiBuf & stabCmd, const XML_Char ** prgpszAtts,
		int hobjOwner, int icls, int ifld);
	void SetCommandForIntegerValue(StrAnsiBuf & stabCmd, const XML_Char ** prgpszAtts,
		int hobjOwner, int icls, int ifld);
	void SetCommandForNumericValue(StrAnsiBuf & stabCmd, const XML_Char ** prgpszAtts,
		int hobjOwner, int icls, int ifld);
	void SetCommandForFloatValue(StrAnsiBuf & stabCmd, const XML_Char ** prgpszAtts,
		int hobjOwner, int icls, int ifld);
	void SetCommandForFloatValueAsBin(StrAnsiBuf & stabCmd, const char * pszVal,
		int hobjOwner, int icls, int ifld);
	void SetCommandForTimeValue(StrAnsiBuf & stabCmd, const XML_Char ** prgpszAtts,
		int hobjOwner, int icls, int ifld);
	void SetCommandForGuidValue(StrAnsiBuf & stabCmd, const XML_Char ** prgpszAtts,
		int hobjOwner, int icls, int ifld);
	void SetCommandForGenDateValue(StrAnsiBuf & stabCmd, const XML_Char ** prgpszAtts,
		int hobjOwner, int icls, int ifld);
	void PrepareForBinaryData();
	void PrepareForStringData();
	void PrepareForMultiStringData(const XML_Char ** prgpszAtts);
	void PrepareForUnicodeData();
	void PrepareForMultiUnicodeData(const XML_Char ** prgpszAtts);
	void HandleReferenceLink(const XML_Char * pszName, const XML_Char ** prgpszAtts,
		int hobjOwner, int icls, int ifld, ElemTypeInfo & etiProp);
	bool HandleImplicitReferenceLink(const XML_Char * pszName, const XML_Char ** prgpszAtts,
		int hobjOwner, int icls, int ifld, ElemTypeInfo & etiProp);

	void PrepareForRuleProp(const XML_Char * pszName, const XML_Char ** prgpszAtts);

	void PushSeqPropInfo(ElemTypeInfo & eti);

	bool StoreCmPossibilityReference(const XML_Char * pszName, const XML_Char ** prgpszAtts,
		int hobjOwner, int ifld, int icls, int ws);
	int GuessFlidOfList(int flidRef);

	bool StoreMoInflAffixSlotReference(const XML_Char * pszName, const XML_Char ** prgpszAtts,
		int hobjOwner, int ifld, int icls, int ws);
	int GetPartOfSpeechForMoInflAffixSlot(const char * pszTargetName,
		const char * pszTargetAbbrPos, const char * pszTargetNamePos, int ws);
	int FindTargetMoInflAffixSlot(int hobjPos, int ws, StrUni & stuNameLow, ListInfo & li);
	int CreateMoInflAffixSlot(int hobjPos, StrUni & stuName, int ws);

	bool StoreMoInflClassReference(const XML_Char * pszName, const XML_Char ** prgpszAtts,
		int hobjOwner, int ifld, int icls, int ws);
	int GetPartOfSpeechForMoInflClass(const char * pszTargetAbbr,
		const char * pszTargetName, const char * pszTargetAbbrPos,
		const char * pszTargetNamePos, int ws);
	int FindTargetMoInflClass(int hobjPos, int ws, StrUni & stuName, StrUni & stuNameLow,
		StrUni & stuAbbr, StrUni & stuAbbrLow, ListInfo & li);
	void EnsureInflClassMapLoaded(int hobjPos, int ws, ListInfo & li);
	int CreateMoInflClass(int hobjPos, StrUni & stuName, StrUni & stuAbbr, int ws);

	bool StoreReversalEntry(const XML_Char * pszName, const XML_Char ** prgpszAtts,
		int hobjOwner, int ifld, int icls, int ws);
	int GetReversalIndex(const char * pszForm, int ws);
	int FindTargetReversalIndexEntry(int hobjIndex, StrUni & stuNameLow, int ws, ListInfo & li);
	void EnsureReversalIndexMapLoaded(int hobjIndex, int ws, ListInfo & li);
	int CreateReversalEntry(int hobjIndex, StrUni & stuName, int ws, ListInfo & li);

	bool StorePhoneEnvReference(const XML_Char * pszName, const XML_Char ** prgpszAtts,
		int hobjOwner, int ifld, int icls, const char * pszForm);
	bool StoreEntryOrSenseLinkInfo(const XML_Char * pszName, const XML_Char ** prgpszAtts,
		int hobjOwner, int ifld, int icls, int ws);
	int FindOrCreatePhEnvironment(int hobjList, int clidItem, const char * pszForm);
	bool StoreLexicalRelationInfo(const XML_Char * pszName, const XML_Char ** prgpszAtts,
		int hobjOwner, int ifld, int icls);
	int DefaultVernacularWritingSystem();
	int DefaultAnalysisWritingSystem();
	int FindOrCreateLexicalReferenceType(const char * pszTypeName, const char * pszTypeAbbr,
		int wsa, const XML_Char * pszName, bool fSense, bool * pfReverse);
	int FindLexicalReferenceType(int wsa, StrUni stuName, StrUni stuAbbr, bool * pfReverse);
	void EnsureLRHashMapsFull(int wsa);
	int CreateLexicalReferenceType(StrUni & stuName, StrUni & stuAbbr, int wsa, bool fSense);

	void FillHashMapNameToHvo(const char * pszSQL, HashMapStrUni<int> & hmsuNameHvo);
	void GenerateNewGuid(GUID * pguid);

	int FindOrCreateReversalIndex(int ws);
	void GetWsNameAndLocale(int ws, StrUni & stuName, StrUni & stuLocale);
	int FindHobjForListFlid(int flidList);

	int FindOrCreateCmPossibility(int hobjList, int ws, int clidItem, const char * pszName,
		const char * pszAbbr,
		int wsVern = 0, const char * pszNameVern = NULL, const char * pszAbbrVern = NULL);
	void AddCmPossibilityVernacularNameAndAbbr(int hobj, int wsVern, const char * pszNameVern,
		const char * pszAbbrVern);
	void EnsurePossibilityListLoaded(int hobjList, int ws, int clidItem, ListInfo & li);
	void LoadPossibilityListProperties(int hobjList, int ws, ListInfo & li);
	int FindCmPossibility(ListInfo & li, StrUni & stuName, StrUni & stuNameLow,
		StrUni & stuAbbr, StrUni & stuAbbrLow);
	int CreateCmPossibility(int hobjList, int ws, int clidItem, StrUni & stuName,
		StrUni & stuAbbr);

	void StoreReference(const XML_Char ** prgpszAtts, int hobjOwner, int ifld, int hobj);

	void SetSingleUserDb(bool fSingle);

	void UpdateDatabaseObjects(SqlDb & sdb, int cline);
	void OpenFiles(BSTR bstrFile, STATSTG * pstatFile);
	void SetOuterHandlers(XML_StartElementHandler startFunc, XML_EndElementHandler endFunc);

	void InitializeTopElementInfo(int hvoOwner, int flid, int icls, const wchar * pszBeginTag,
		int hvoObj, const GUID guidObj, int hvoMin);
	void MapGuidsToHobjs();
	void MapIcuLocalesToWsHobjs();
	void LogRepeatedMessages();
	void InitializeParser(int cFld, int cBlkTotal, int iPass);
	void ParseXmlPhaseOne(int cFld, STATSTG & statFile, IAdvInd * padvi, int nPercent = 2);
	void ParseXmlPhaseTwo(int cFld, STATSTG & statFile, IAdvInd * padvi, int nPercent = 36);

	void ProcessDatabasePass1(ElemTypeInfo & eti, const XML_Char ** prgpszAtts);
	void ProcessAddPropsPass1(ElemTypeInfo & eti);
	void ProcessDefinePropPass1(ElemTypeInfo & eti, const XML_Char ** prgpszAtts);
	int GetCustomFieldFlid(const char * pszFlid);
	int GetCustomFieldClid(const char * pszClass);
	int GetCustomFieldTargetClid(const char * pszTarget);
	int GetCustomFieldType(const char * pszType);
	const char * GetCustomFieldMin(const char * pszMin);
	const char * GetCustomFieldMax(const char * pszMax);
	const char * GetCustomFieldBig(const char * pszBig);
	void SetCustomFieldUserLabel(StrUni & stuUserLabel, const char * pszUserLabel);
	void SetCustomFieldHelpString(StrUni & stuHelp, const char * pszHelpString);
	const char * GetCustomFieldListRootId(const char * pszListRootId);
	const char * GetCustomFieldWsSelector(const char * pszWsSelector);
	void ProcessCustomPropPass1(ElemTypeInfo & eti, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	void SetErrorWithMessage(int stid);
	void BatchSqlCommand(const StrAnsiBuf & stabCmd);
	void ProcessBasicPropPass2(const XML_Char * pszName, ElemTypeInfo & eti);
	void StoreBinaryData(const XML_Char * pszName, ElemTypeInfo & eti, int icls, int ifld,
		int hobjOwner);
	int ConvertHexStringToByteArray(byte * prgbBin, const char * prgchHex, int cch,
		const XML_Char * pszName);
	void StoreStringData(const XML_Char * pszName, int icls, int ifld, int hobjOwner);
	void StoreMultiStringData(const XML_Char * pszName, int icls, int ifld, int hobjOwner);
	void StoreUnicodeData(const XML_Char * pszName, int icls, int ifld, int hobjOwner);
	void StoreMultiUnicodeData(const XML_Char * pszName, int icls, int ifld, int hobjOwner);
	void StoreRulePropData(const XML_Char * pszName, int icls, int ifld, int hobjOwner);
	int GetClassIndexFromFieldIndex(int ifld);

protected:
	void StoreMultiUnicodeData(int hobjOwner, int fid, int ws, StrUni & stuData);
	void StoreMultiStringData(int hobjOwner, int ifld, int ws, StrUni & stuChars,
		Vector<FwXml::BasicRunInfo> & vbri, Vector<FwXml::RunPropInfo> & vrpi);

	virtual int ProcessExternalEntityFile(XML_Parser parser, const XML_Char * pszContext,
		const XML_Char * pszBase, const XML_Char * pszSystemId, const XML_Char * pszPublicId,
		XML_Parser & entParser);
	virtual void ProcessStartTag1(const XML_Char * pszName, const XML_Char ** prgpszAtts);
	virtual void ProcessEndTag1(const XML_Char * pszName);
	void ProcessPropStartTag1(const XML_Char * pszName, const XML_Char ** prgpszAtts);
	void ProcessPropEndTag1(const XML_Char * pszName);
	void ProcessStringStartTag1(const XML_Char * pszName, const XML_Char ** prgpszAtts);
	void ProcessStringEndTag1(const XML_Char * pszName);
	virtual void ProcessCharData1(const XML_Char * prgch, int cch);
	virtual void ProcessStartTag2(const XML_Char * pszName, const XML_Char ** prgpszAtts);
	virtual void ProcessEndTag2(const XML_Char * pszName);
	void ProcessPropStartTag2(const XML_Char * pszName, const XML_Char ** prgpszAtts);
	void ProcessPropEndTag2(const XML_Char * pszName);
	void ProcessWsStylesProp(const XML_Char * pszName);
	void ProcessRuleProp(const XML_Char * pszName);
	void ProcessImportStartTag1(const XML_Char * pszName, const XML_Char ** prgpszAtts);
	void ProcessImportStartTag2(const XML_Char * pszName, const XML_Char ** prgpszAtts);

public:
	//:> Handler (callback) functions for the XML parser.  These must be static methods.

	static int HandleExternalEntityRef(XML_Parser parser, const XML_Char * pszContext,
		const XML_Char * pszBase, const XML_Char * pszSystemId, const XML_Char * pszPublicId);
	static void HandleStartTag1(void * pvUser, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	static void HandleEndTag1(void * pvUser, const XML_Char * pszName);

	static void HandlePropStartTag1(void * pvUser, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	static void HandlePropEndTag1(void * pvUser, const XML_Char * pszName);
	static void HandleStringStartTag1(void * pvUser, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	static void HandleStringEndTag1(void * pvUser, const XML_Char * pszName);

	static void HandleCharData1(void * pvUser, const XML_Char * prgch, int cch);

	static void HandleStartTag2(void * pvUser, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	static void HandleEndTag2(void * pvUser, const XML_Char * pszName);
	static void HandlePropStartTag2(void * pvUser, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	static void HandlePropEndTag2(void * pvUser, const XML_Char * pszName);

	//:> These handlers are specific to importing individual field contents.

	static void ImportStartTag1(void * pvUser, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	static void ImportStartTag2(void * pvUser, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);

	//:> These methods provide access to internal data values.

	int CustomCount() { return m_cCustom; }
	int ObjectCount() { return m_cobj; }
	int ObjectCount_Pass2() { return m_cobj2; }
	int SqlCount() { return m_cSql; }

	//:> These methods are implemented in FwXmlString.cpp.
	void SetIntegerProperty(TextProps::TextIntProp & txip);
	void SetStringProperty(TextProps::TextStrProp & txsp);
	void SetStringProperty(FwXml::TextGuidValuedProp & tgvp);
	void ProcessStringStartTag(const XML_Char * pszName, const XML_Char ** prgpszAtts);
	void ProcessStringEndTag(const XML_Char * pszName);
	void ProcessCharData(const XML_Char * prgch, int cch);
	void SetTextColor(const XML_Char ** prgpszAtts, const char * pszAttName, int scp);
	void SetTextToggle(const XML_Char ** prgpszAtts, const char * pszAttName, int scp);
	void SetTextWs(const XML_Char ** prgpszAtts, const char * pszAttName, int scp);
	void SetTextMetric(const XML_Char ** prgpszAtts, const char * pszAttName,
		const char * pszUnitAttName, int scp);
	void SetTextSuperscript(const XML_Char ** prgpszAtts);
	void SetTextUnderline(const XML_Char ** prgpszAtts);
	void SetStringProperty(const XML_Char ** prgpszAtts, const char * pszAttName, int stp,
		wchar chType = 0);
	void SetTagsAsStringProp(int stp, const char * pszVal);
	void SetObjDataAsStringProp(int stp, const char * pszVal, wchar chType);
	void SaveCharDataInRun();
	void SavePictureDataInRun();
	bool StoreRunInformation();
	bool StoreRawPropertyBytes();
	void ConvertToRawBytes(FwXml::RunPropInfo & rpi);
	int ConvertPictureToBitmap(const char * prgchHex, int cch, byte * prgbBin);
	bool SetWsIfNeeded(const XML_Char * prgch, int cch);
	void AddExtraAnalysisWss();

	//:> This method was added to support ImportMultipleXmlFields().
	void InitializeForMerging(int hvoOwner, int icls, int hvoLim);

	void AllowNewWritingSystems()
	{
		m_fAllowNewWritingSystems = true;
	}

protected:

	//:> These methods were added to support ImportMultipleXmlFields().
	bool CheckForExistingObject(const ElemTypeInfo & eti, int hobjOwner);
	void PushExistingObjectInfo(int hobj, int icls, int hobjOwner, int fid);
	bool GetExistingObject(const ElemTypeInfo & eti, const XML_Char * pszName, int & hobj);
	void RecordExistingObjects(CreatedObjectSet & cos);
	bool StoreWritingSystemReference(const XML_Char * pszName, const XML_Char ** prgpszAtts,
		int hobjOwner, int ifld, int icls, int ws);
	void ChangeOwners(int hobjOld, int hobjNew, int icls);
	void UpdateAtomSubListsOwner(int hobjOld, int hobjNew, int icls, int ifld);
	void UpdateCollectionOwner(int hobjOld, int hobjNew, int icls, int ifld);
	void UpdateSequenceOwner(int hobjOld, int hobjNew, int icls, int ifld);
	bool FileExists(const char * pszPath);
	void EnsureDirectoryExists(const char * pszDir);
	bool StoreFilePathReference(const XML_Char * pszName, const XML_Char ** prgpszAtts,
		int hobjOwner, int ifld, int icls, const char * pszPath);
	void StoreMessageInMultiStringField(int hobjOwner, int flid, StrUni & stuMsg);
	void SetFwDataPath();
	int CreateCmFileForLangProject(int fid, const char * pszPathname);
	int FindOrCreateFolder(int flid, const char * pszFolderName);
	void CreateMissingSensesForComplexForms();
	void CreateEmptySenseForEntry(int hvoRef, HashMap<int, int> & hmhvoEntry);

	//:> Data used by the various methods.

	bool m_fError;			// Flag that an error has occurred: this will terminate the parse.
	HRESULT m_hr;					// COM return code: S_OK or specific error code.
	FwXmlData * m_pfwxd;			// Pointer to the open database object.
	SqlDb & m_sdb;					// The database connection, if one is open.
	IStreamPtr m_qstrm;				// Input IStream pointer.
	// With increased fields in our DB CM, the total query became too large for a
	// StrAnsiBufHuge on export, so it's likely that import may also run into this problem,
	// so it was switched to a plain StrAnsi.
	//IMPORTCMDSTRING m_stabCmd;	// Used for building SQL commands.
	StrAnsi m_staCmd;				// used for building SQL commands.
	StrAnsiBufPath m_stabpFile;		// Name of the input file: used for log file messages.
	StrAnsiBufPath m_stabpLog;		// Name of the log output file.
	FILE * m_pfileLog;				// Old fashioned C FILE pointer for log output file.
	XML_Parser m_parser;			// The open Expat XML parser.
	unsigned m_celemStart;			// Number of start tags encountered in the XML input.
	unsigned m_celemEnd;			// Number of end tags encountered in the XML input.
	XML_Char m_chGuid;				// Letter that starts GUID-based ID strings.
	int m_cchGuid;					// Number of different letters that start GUID-based IDs.
	int m_cobj;						// Object counter for first pass.
	int m_cobj2;					// Object counter for second pass.
	bool m_fInString;				// Flag that we're parsing a <Str> (or <AStr>) element.
	bool m_fInRun;					// Flag that we're parsing a <Run> element.
	bool m_fInUnicode;				// Flag that we're parsing a <Uni> (or <AUni>) element.
	bool m_fInBinary;				// Flag that we're parsing a <Binary> element.
	bool m_fInRuleProp;				// Flag that we're parsing a <Prop> element.
	bool m_fInWsStyles;				// Flag that we're parsing a <WsStyles9999> element.
	bool m_fRunHasChars;			// Flag that this <Run> element contains text data.
	int m_ws;						// Writing system for <AStr> or <AUni>.
	bool m_fIcuLocale;				// Flag that we want to store <Uni> data in pass 1 since
									// we're inside <LgWritingSystem><ICULocale24>.
	StrAnsi m_staChars;				// Character data for <LgWritingSystem><ICULocale24><Uni>.
	StrUni m_stuChars;				// Character data for <Uni>, <AUni>, <Str>, or <AStr>.
	Vector<char> m_vchHex;			// Character data for <Binary> or <Run type="picture">.

	bool m_fCustom;					// flag that we're defining a custom field.
	Vector<CustomFieldInfo> m_vcfi;	// Data for <CustomField> elements.

	// Map from XML tag name to corresponding type information.
	HashMapChars<ElemTypeInfo> m_hmceti;
	// Map XML object ID string to object GUID.  This is empty if all XML IDs are GUID-based.
	HashMapChars<int> m_hmcidhobj;
	HashMap<GUID,int> m_hmguidhobj;		// Map object GUID to object id.
	HashMapChars<int> m_hmcws;			// Map writing system string to writing system integer.
	Set<int> m_setExtraWsUsed;			// Remember writing systems encountered other than
										// default analysis and vernacular.

	Vector<ElemTypeInfo> m_vetiOpen;	// Stack of currently open XML elements.
	Vector<int> m_vhobjOpen;			// Stack of ids for currently open objects.
	Vector<SeqPropInfo> m_vspiOpen;		// Stack of currently open properties.
	Vector<double> m_vdbl;				// Floating point numbers for UPDATE parameters.

	//:> Variables for holding formatted string information.  (also m_stuChars above).
	FwXml::RunDataType m_rdt;				// Type of this run (Characters / Picture)
	FwXml::BasicRunInfo m_bri;				// Offsets of this run into the string data.
	Vector<TextProps::TextIntProp> m_vtxip;	// Integer-valued properties for this run.
	Vector<TextProps::TextStrProp> m_vtxsp;	// String-valued properties for this run
	// Offsets of all the runs into the string data.  A formatted string is completely specified
	// by the contents of m_stuChars, m_vbri, and m_vrpi.
	Vector<FwXml::BasicRunInfo> m_vbri;
	// Formatting data for all the runs.  Multiple runs may share one entry in this vector.
	Vector<FwXml::RunPropInfo> m_vrpi;

	//:> Variables for holding WsStyles properties.
	Vector<TextProps::TextIntProp> m_vtxipWs;	// Scalar-valued properties for this <Prop>.
	Vector<TextProps::TextStrProp> m_vtxspWs;		// Text-valued properties for this <Prop>.
	Vector<WsStyleInfo> m_vesi;				// Vector of WsProp values.

	Vector<byte> m_vbProp;					// Binary value for internal <Prop>s.

	//:> Variables for holding BulNumFontInfo internal properties.
	Vector<TextProps::TextIntProp> m_vtxipBNFI;	// Scalar-valued internal properties.
	Vector<TextProps::TextStrProp> m_vtxspBNFI;	// Text-valued internal properties.

	RootObjData m_rod;			// Object class, id, guid for root objects (first pass).
	OwnedObjData m_ood;			// Object class, id, guid, owner, fid, ord for owned objects.
	Vector<int> m_vcfld;		// number of items present for each field type.

	Vector<StoredData> m_vstda;		// Miscellaneous data (second pass).
	MultiTxtData m_mtd;				// MultiUnicode data (second pass).
	MultiTxtData m_mtdBig;			// MultiBigUnicode data (second pass).
	MultiStrData m_msd;				// MultiString data (second pass).
	MultiStrData m_msdBig;			// MultiBigString data (second pass).

	bool m_fSingle;						// The Database is in single-user mode.
	int m_cCustom;						// The number of custom fields defined.
	int m_cSql;							// Number of SQL commands executed.

	ITsStrFactoryPtr m_qtsf;

	XML_StartElementHandler m_startOuterHandler;	// Outermost Start Element Handler.
	XML_EndElementHandler m_endOuterHandler;		// Outermost End Element Handler.
	int m_hvoOwner;				// Database object id of owning object for importing a field,
								// or -1 if loading an entire database.
	int m_flid;					// Field id for importing a field (0 for loading a database).
	StrAnsi m_staBeginTag;		// Outermost tag of the XML file.
	StrAnsi m_staOwnerBeginTag;	// Possible outermost start tag for importing objects.
	int m_hvoMin;				// One less than the minimum id value for new objects.
	int m_hvoObj;				// Database id/guid of the imported object if it is in an atomic
	GUID m_guidObj;				// field, and the object already exists.  (default = 0, NULL)
	unsigned long m_cBlk;
	unsigned long m_cBlkTotal;
	unsigned long m_cStep;

	// variables used for handling implicit references in Link elements.
	int m_iclsOwner;
	int m_cobjNewTargets;		// Number of target objects created on the fly.
	int m_wsVern;				// Default vernacular writing system
	int m_wsAnal;				// Default analysis writing system

	// Map from PossibilityList/WritingSystem to owned CmPossibility Name/Abbreviation info.
	HashMap<ListIdentity, ListInfo> m_hmlidli;
	// Map from PartOfSpeech/WritingSystem to owned MoInflAffixSlot Name info.
	HashMap<ListIdentity, ListInfo> m_hmlidliSlot;
	// Map from PartOfSpeech/WritingSystem to owned MoInflClass Name/Abbreviation info.
	HashMap<ListIdentity, ListInfo> m_hmlidliInflClass;
	// Map from WritingSystem to corresponding ReversalIndex.
	HashMap<int, int> m_hmwshobjRevIdx;
	// Map from ReversalIndex database id to owned ReversalIndexEntry Form info.
	HashMap<int, ListInfo> m_hmhobjliRevIdx;
	// Map from StringRepresentation to id of PhEnvironment objects.
	HashMapStrUni<int> m_hmsuPhEnvId;
	// Map from WritingSystem id to the name and ICULocale values.
	HashMap<int, StrUni> m_hmwssuName;
	HashMap<int, StrUni> m_hmwssuICULocale;
	// Map from (possibly) repetitive error/warning/info message to a counter.
	HashMapChars<int> m_hmcMsgcMore;

	// Data added for handling LexicalRelation links.
	Vector<LexRelationInfo> m_vlri;
	HashMapStrUni<int> m_hmsuNameHvoLRType;
	HashMapStrUni<int> m_hmsuAbbrHvoLRType;
	HashMapStrUni<int> m_hmsuRevNameHvoLRType;
	HashMapStrUni<int> m_hmsuRevAbbrHvoLRType;
	HashMap<int, HvoVector> m_hmhvoEntryvhvoSenses;
	HashMap<int, HvoVector> m_hmhvoSensevhvoSenses;
	HashMapStrUni<int> m_hmsuHeadwordHvo;
	HashMap<int, int> m_hmhvoTypeMappingType;
	HashMap<int, LexReferenceVec> m_hmhvoTypeLexReferences;
	// Data added for handling EntryOrSense links.
	Vector<EntryOrSenseLinkInfo> m_vmesi;
	HashMap<int, HvoVector> m_hmhvoEntryvhvoComponent;
	HashMap<int, HvoVector> m_hmhvoEntryvhvoPrimary;
	Set<int> m_sethvoLexEntryRef;

	// Set of field ids for text fields that are indexed (hence limited to kcchMaxIndexSize
	// chars in length).
	Set<int> m_setIdxTxtFlids;

	// Data added to support ImportMultipleXmlFields().
	// Flag that we may be merging into existing atomic objects, not just creating objects.
	bool m_fMerge;
	// Object id, class, owner, fid for OwningAtom objects encountered.
	Vector<ExistingObjData> m_veod;
	// map from object owner and fid to index in ExistingObjData vector.
	HashMap<OwnerField, int> m_hmofieod;
	int m_hvoFirst;					// All existing objects have ids < m_hvoFirst.
	StrAppBufPath m_strbpFwDataPath;	// FieldWorks data root directory.
	Vector<int> m_vhobjDel;			// List of objects that we created, but then discovered we
									// don't really want.
	bool m_fAllowNewWritingSystems;	// Flag that we want to create unrecognized ws values.
};

/*----------------------------------------------------------------------------------------------
	This data structure holds the data for one item in a list being updated.

	Hungarian: uoi
----------------------------------------------------------------------------------------------*/
struct UpdateObjInfo
{
	int m_hvoObj;
	GUID m_guidObj;
	int m_cidObj;
	int m_nDepth;
	int m_hvoOwner;
	int m_cidOwner;
	int m_fidOwner;
	int m_ordRel;
	int m_cptRel;

	bool operator==(const UpdateObjInfo & uoi)
	{
		return m_hvoObj == uoi.m_hvoObj &&
			m_guidObj == uoi.m_guidObj &&
			m_cidObj == uoi.m_cidObj &&
			m_nDepth == uoi.m_nDepth &&
			m_hvoOwner == uoi.m_hvoOwner &&
			m_cidOwner == uoi.m_cidOwner &&
			m_fidOwner == uoi.m_fidOwner &&
			m_ordRel == uoi.m_ordRel &&
			m_cptRel == uoi.m_cptRel;
	}
};

/*----------------------------------------------------------------------------------------------
	This data structure holds the data for one reference link to a item in a list being updated.

	Hungarian: uli
----------------------------------------------------------------------------------------------*/
struct UpdateLinkInfo
{
	int m_hvoSrc;
	int m_fidSrc;
	int m_ordSrc;
	int m_cptSrc;
	int m_hvoDst;
};

/*----------------------------------------------------------------------------------------------
	This data structure holds the data for one merge operation in a list being updated.

	Hungarian: umi
----------------------------------------------------------------------------------------------*/
struct UpdateMergeInfo
{
	GUID m_guidFrom;
	int m_hvoFrom;
	GUID m_guidTo;
	int m_hvoTo;	// May be 0 if merging to a new list item.
};

/*----------------------------------------------------------------------------------------------
	This data structure holds the data for one delete operation in a list being updated.

	Hungarian: udi
----------------------------------------------------------------------------------------------*/
struct UpdateDeleteInfo
{
	GUID m_guid;
	int m_hvo;
};

/*----------------------------------------------------------------------------------------------
	This data structure holds the data for one delete operation in a multilingual table
	that needs to be cleared for an update operation.

	Hungarian: mlf
----------------------------------------------------------------------------------------------*/
struct MultilingualField
{
	int m_fid;
	int m_ws;	// used only if we want to preserve existing data not covered in updated list
	Vector<int> m_vhobj;
};

/*----------------------------------------------------------------------------------------------
	FwXmlUpdateData is a specialization of FwXmlImportData used by the
	FwXmlData::UpdateListFromXml() method.

	Note that a pointer to an object of this type is passed as the "User Data" to the callback
	functions.

	Hungarian: xud
----------------------------------------------------------------------------------------------*/
class FwXmlUpdateData : public FwXmlImportData
{
public:
	//:> Constructor and Destructor.

	FwXmlUpdateData(FwXmlData * pfwxd);
	~FwXmlUpdateData();

	//:> Other methods.

	void InitializeForUpdate(int hvoOwner, int flid);
	void MergeDeleteAndCreateObjects(IAdvInd * padvi);
	virtual void StoreData(IAdvInd * padvi, int nPercent = 15);
	void SaveProgressReportStatus(IAdvInd * padvi);
	void RestoreProgressReportStatus(IAdvInd * padvi);
	void ReportInitializationTime(long timDelta);

protected:
	void StoreUpdateObjInfo(SQLHSTMT hstmt);
	void StoreUpdateLinkInfo(SQLHSTMT hstmt);

	virtual void ProcessStartTag1(const XML_Char * pszName, const XML_Char ** prgpszAtts);
	virtual void ProcessEndTag1(const XML_Char * pszName);
	virtual void ProcessCharData1(const XML_Char * prgch, int cch);
	virtual void ProcessStartTag2(const XML_Char * pszName, const XML_Char ** prgpszAtts);
	virtual void ProcessEndTag2(const XML_Char * pszName);

	virtual ElemTypeInfo GetElementType(const char * pszElement);

	virtual void StartObject1(const ElemTypeInfo & eti, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	virtual void StartPropName1(ElemTypeInfo & eti, const XML_Char * pszName,
		const char * pszProp = NULL);
	virtual void StartBasicProp1(const ElemTypeInfo & eti, const XML_Char * pszName);
	void StartMergeElem1(const ElemTypeInfo & eti, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	void StartDeleteElem1(const ElemTypeInfo & eti, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	void CheckUpdatePropNesting(const XML_Char * pszName);
	int GetObjectIdAndGuid(const XML_Char ** prgpszAtts, UpdateObjInfo & uoi);
	void ProcessUpdateObjectInfo();
	int FindValidOwner(int hvoOwner, int * pfidOwner, int * pcidOwner, int * pcptRel);
	void AdjustStandardOrdValues(Vector<int> &viuoiChg);
	void AdjustCustomItemOrdValues(Vector<int> & viuoiCustomChg);
	void FinishCreatingNewListItems();
	void UpdateOwnerAndOrd();
	void CreateNewListObjects();
	void FixLinksToMergedItems();
	void DeleteObsoleteItems(IAdvInd * padvi, int nPercent);
	void CollectDeletedObjects();
	void DumpDebugInfo();
	int FindMatchingObject(UpdateObjInfo & uoi);
	int FindLimitOfOwner(Vector<UpdateObjInfo> & vuoiOrig, int iOldOwner,
		HashMap<int,int> & hmhvoiuoi);
	bool IsOwnedBy(int hvoObj, int hvoOwner, Vector<UpdateObjInfo> & vuoi,
		HashMap<int,int> & hmhvoiuoi, int cidParent);

	virtual void StartObject2(ElemTypeInfo & eti, const XML_Char * pszName);
	void ClearReferenceCollections();
	void ClearReferenceSequences();
	void FixReferenceSequences();
	void ClearReferenceCollection(int hobjSrc, int ifld);
	void ClearReferenceSequence(int hobjSrc, int ifld);
	void FixReferenceSequence(UpdateLinkInfo & uli, const FwDbFieldInfo & fdfi);
	virtual void StoreMultiBigString();
	void ClearMultiBigString();
	virtual void StoreMultiBigUnicode();
	void ClearMultiBigUnicode();
	int GetMultilingualFieldIndex(Vector<MultilingualField> & vmlf, int fid, int ws = 0);
	void ClearMultiTable(Vector<MultilingualField> & vmlf, const char * pszTable);
	void ExpandAndExecuteCommand(const char * pszCmd, Vector<int> & vhobj);
	void CheckDeleteWithParamForSuccess(SQLUINTEGER cParamsProcessed,
		SQLUSMALLINT * rgnParamStatus, const Vector<int> & vhobj, const char * pszCmd);
	void CheckDeleteMultiBigParamsForSuccess(SQLUINTEGER cParamsProcessed,
		SQLUSMALLINT * rgnParamStatus, const MultilingualField & mlf, const char * pszTable);
	void RemoveObsoleteData(int cid, IAdvInd * padvi, int nPercent);
	void ClearObsoleteBasicData(const FwDbFieldInfo & fdfi, const char * pszValue);
	void ClearObsoleteMultilingualData(const FwDbFieldInfo & fdfi);

	//:> Additional data used by the various methods.

	int m_hvoList;
	int m_cidItem;
	int m_cobjNew;

	Vector<UpdateObjInfo> m_vuoiOrig;
	HashMap<GUID,int> m_hmguidiuoiOrig;
	HashMap<int,int> m_hmhvoiuoiOrig;

	Vector<UpdateLinkInfo> m_vuli;

	Vector<UpdateMergeInfo> m_vumi;
	HashMap<GUID,int> m_hmguidiumi;
	HashMap<int, int> m_hmhvoFromhvoTo;

	Vector<UpdateDeleteInfo> m_vudi;
	HashMap<GUID,int> m_hmguidiudi;

	Vector<UpdateObjInfo> m_vuoiRevised;
	HashMap<GUID,int> m_hmguidiuoiRevised;
	HashMap<int,int> m_hmhvoiuoiRevised;

	Vector<UpdateObjInfo> m_vuoiCustom;
	HashMap<GUID,int> m_hmguidiuoiCustom;
	HashMap<int,int> m_hmhvoiuoiCustom;

	Vector<int> m_viuoiItemOrig;
	Vector<int> m_viuoiItemRevised;
#if 99
	Vector<int> m_viuoiItemNew;
#endif
	int m_nPosOrig;
	int m_nMinOrig;
	int m_nMaxOrig;
};

/*----------------------------------------------------------------------------------------------
	Function to turn StrUni's into uppercase.  Used specifically for FireBird column names, e.g.

	stabCmd.Format("UPDATE %S SET \"%S\" = %d WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(),
			UpperName(m_pfwxd->FieldName(ifld)), nVal, hobjOwner);
----------------------------------------------------------------------------------------------*/
const wchar* UpperName(StrUni stu)
{
	ToUpper(stu.Bstr(), stu.Length());
	return stu.Chars();
}


/*----------------------------------------------------------------------------------------------
	If rc indicates an error (or partial success), retrieve and log more specific information.
	(This is identical to FwXmlExportData::VerifySqlRc except for calling a different LogMessage
	method.)

	@param rc ODBC/SQL function return code.
	@param hstmt Allocated handle to an ODBC/SQL statement.
	@param cline Number of the line in the source file where the call to this function occurs.
	@param pszCmd String containing the SQL command that produced rc.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::VerifySqlRc(RETCODE rc, SQLHANDLE hstmt, int cline, const char * pszCmd)
{
	StrAnsi sta;
	StrAnsi staFmt;
	if (pszCmd && *pszCmd)
	{
#ifdef LOG_SQL
		sta.Format("SQL[%d]:(%d) %s%n", cline, strlen(pszCmd), pszCmd);
		LogMessage(sta.Chars());
#endif /*LOG_SQL*/
		++m_cSql;
	}
	if (rc == SQL_ERROR || rc == SQL_SUCCESS_WITH_INFO)
	{
		SQLTCHAR sqst[6];
		SQLINTEGER ntverr;
		SQLTCHAR rgchBuf[512];
		const int cchBuf = (sizeof(rgchBuf) / sizeof(SQLTCHAR)) - 1;
		SQLSMALLINT cb;
		SQLGetDiagRec(SQL_HANDLE_STMT, hstmt, 1, sqst, &ntverr, rgchBuf, cchBuf, &cb);
		sqst[5] = 0;
		rgchBuf[cchBuf] = 0;
		StrAnsi staSqst(sqst);
		if (rc == SQL_ERROR)
		{
			StrAnsi staBuf(rgchBuf);
			if (pszCmd && *pszCmd)
			{
#ifndef LOG_SQL
				sta.Format("SQL[%d]: %s\n", cline, pszCmd);
				LogMessage(sta.Chars());
#endif /*LOG_SQL*/
				// "ERROR %s executing SQL command:\n    %s"
				staFmt.Load(kstidXmlErrorMsg018);
				sta.Format(staFmt.Chars(), staSqst.Chars(), staBuf.Chars());
				LogMessage(sta.Chars());
			}
			else
			{
				// "ERROR %s executing SQL function on line %d of %s:\n    %s"
				staFmt.Load(kstidXmlErrorMsg019);
				sta.Format(staFmt.Chars(), staSqst.Chars(), cline, __FILE__, staBuf.Chars());
				LogMessage(sta.Chars());
			}
		}
		else
		{
			// Remove leading cruft from the information string.
			const achar * psz = reinterpret_cast<achar *>(rgchBuf);
			if (CURRENTDB == FB) {
				if (!_tcsncmp(psz, _T("[Firebird]"), 11))
					psz += 11;
				if (!_tcsncmp(psz, _T("[ODBC Firebird Driver]"), 24))
					psz += 24;
				if (!_tcsncmp(psz, _T("[Firebird]"), 12))
					psz += 12;
			}
			else if (CURRENTDB == MSSQL) {
				if (!_tcsncmp(psz, _T("[Microsoft]"), 11))
					psz += 11;
				if (!_tcsncmp(psz, _T("[ODBC SQL Server Driver]"), 24))
					psz += 24;
				if (!_tcsncmp(psz, _T("[SQL Server]"), 12))
					psz += 12;
			}
			StrAnsi staBuf(psz);
			staFmt.Load(kstidXmlErrorMsg201);		// "    %s - %s\n"
			sta.Format(staFmt.Chars(), staSqst.Chars(), staBuf.Chars());
			LogMessage(sta.Chars());
		}
	}
	CheckSqlRc(rc);
}


/*----------------------------------------------------------------------------------------------
	Execute a simple SQL batch statement, one which does not return any row sets or require any
	input parameters.

	@param pszCmd Pointer to the SQL command string.
	@param cline Number of the line in the source file where the call to this function occurs.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ExecuteSimpleSQL(const char * pszCmd, int cline)
{
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pszCmd)), SQL_NTS);
	//fprintf(stdout,"rc=%d\n",rc);
	VerifySqlRc(rc, sstmt.Hstmt(), cline, pszCmd);
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Execute a simple SQL Unicode batch statement, one which does not return any row sets or require any
	input parameters.
	This should probably be used for everything instead of ExecuteSimpleSQL, but I (Ken)
	didn't want to take the time to convert everything to using Unicode strings, but did need
	one query to be Unicode where we are using a Unicode file name and needed to get
	this through to the server.

	@param pszCmd Pointer to the SQL command string.
	@param cline Number of the line in the source file where the call to this function occurs.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ExecuteSimpleUnicodeSQL(const wchar * pwszCmd, int cline)
{
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLExecDirectW(sstmt.Hstmt(),
		reinterpret_cast<SQLWCHAR *>(const_cast<wchar *>(pwszCmd)), SQL_NTSL);
	//fprintf(stdout,"rc=%d\n",rc);
	StrAnsi sta = pwszCmd;
	VerifySqlRc(rc, sstmt.Hstmt(), cline, sta.Chars());
	sstmt.Clear();
}



/*----------------------------------------------------------------------------------------------
	Execute a single SQL batch statement, one which does not return any row sets but which has
	one input parameter, a Unicode string.

	@param pszCmd Pointer to the SQL command string.
	@param cline Number of the line in the source file where the call to this function occurs.
	@param pszParam the UTF-8 form of the input parameter
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ExecuteParameterizedSQL(const char * pszCmd, int cline,
	const char * pszParam)
{
	// Convert the parameter from UTF-8 to UTF-16, bind the parameter, and execute the SQL.
	StrUni stuParam;
	StrUtil::StoreUtf16FromUtf8(pszParam, strlen(pszParam), stuParam, false);
	SQLINTEGER cchParam = stuParam.Length();
	SQLINTEGER cbParam = cchParam * sizeof(wchar);
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
		cchParam, 0, const_cast<wchar *>(stuParam.Chars()), cbParam, &cbParam);
	VerifySqlRc(rc, sstmt.Hstmt(), cline);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pszCmd)), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), cline, pszCmd);
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Execute a single SQL batch statement, one which does not return any row sets but which has
	two input parameters, both Unicode strings.

	@param pszCmd Pointer to the SQL command string.
	@param cline Number of the line in the source file where the call to this function occurs.
	@param pszFirst the UTF-8 form of the first input parameter
	@param pszSecond the UTF-8 form of the second input parameter
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ExecuteParameterizedSQL(const char * pszCmd, int cline,
	const char * pszFirst, const char * pszSecond)
{
	// Convert the parameters from UTF-8 to UTF-16, bind the parameters, and execute the SQL.
	StrUni stuFirst;
	StrUni stuSecond;
	StrUtil::StoreUtf16FromUtf8(pszFirst, strlen(pszFirst), stuFirst, false);
	StrUtil::StoreUtf16FromUtf8(pszSecond, strlen(pszSecond), stuSecond, false);
	SQLINTEGER cchFirst = stuFirst.Length();
	SQLINTEGER cchSecond = stuSecond.Length();
	SQLINTEGER cbFirst = cchFirst * sizeof(wchar);
	SQLINTEGER cbSecond = cchSecond * sizeof(wchar);
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
		cchFirst, 0, const_cast<wchar *>(stuFirst.Chars()), cbFirst, &cbFirst);
	VerifySqlRc(rc, sstmt.Hstmt(), cline);
	rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
		cchSecond, 0, const_cast<wchar *>(stuSecond.Chars()), cbSecond, &cbSecond);
	VerifySqlRc(rc, sstmt.Hstmt(), cline);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pszCmd)), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), cline, pszCmd);
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Execute a simple SQL batch statement, one which returns one row set containing one integer
	value, and which does not require any input parameters.

	@param pszCmd Pointer to the SQL command string.
	@param cline Number of the line in the source file where the call to this function occurs.
	@param fIsNull reference to a flag whether the data is actually null (output)
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::ReadOneIntFromSQL(const char * pszCmd, int cline, bool & fIsNull)
{
	int nVal = 0;
	SDWORD cbVal;
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pszCmd)), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), cline, pszCmd);
	rc = SQLFetch(sstmt.Hstmt());
	VerifySqlRc(rc, sstmt.Hstmt(), cline);
	if (rc == SQL_SUCCESS || rc == SQL_SUCCESS_WITH_INFO)
	{
		rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_SLONG, &nVal, isizeof(nVal), &cbVal);
		VerifySqlRc(rc, sstmt.Hstmt(), cline);
	}
	else
	{
		// Probably, rc == SQL_NO_DATA.
		cbVal = SQL_NULL_DATA;
	}
	sstmt.Clear();
	fIsNull = (cbVal == SQL_NULL_DATA);
	return nVal;
}


/*----------------------------------------------------------------------------------------------
	Execute a single SQL batch statement, one which returns one row set containing one integer
	value, and which has one input parameter, a Unicode string.

	@param pszCmd Pointer to the SQL command string.
	@param cline Number of the line in the source file where the call to this function occurs.
	@param pszParam the UTF-8 form of the input parameter
	@param fIsNull reference to a flag whether the data is actually null (output)
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::ReadOneIntFromParameterizedSQL(const char * pszCmd, int cline,
	const char * pszParam, bool & fIsNull)
{
	int nVal = 0;
	SDWORD cbVal;
	// Convert the parameter from UTF-8 to UTF-16, bind the parameter, and execute the SQL.
	StrUni stuParam;
	StrUtil::StoreUtf16FromUtf8(pszParam, strlen(pszParam), stuParam, false);
	SQLINTEGER cchParam = stuParam.Length();
	SQLINTEGER cbParam = cchParam * sizeof(wchar);
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
		cchParam, 0, const_cast<wchar *>(stuParam.Chars()), cbParam, &cbParam);
	VerifySqlRc(rc, sstmt.Hstmt(), cline);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pszCmd)), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), cline, pszCmd);
	rc = SQLFetch(sstmt.Hstmt());
	VerifySqlRc(rc, sstmt.Hstmt(), cline);
	if (rc == SQL_SUCCESS || rc == SQL_SUCCESS_WITH_INFO)
	{
		rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_SLONG, &nVal, isizeof(nVal), &cbVal);
		VerifySqlRc(rc, sstmt.Hstmt(), cline);
	}
	else
	{
		// Probably, rc == SQL_NO_DATA.
		cbVal = SQL_NULL_DATA;
	}
	sstmt.Clear();
	fIsNull = (cbVal == SQL_NULL_DATA);
	return nVal;
}


/*----------------------------------------------------------------------------------------------
	Execute an SQL batch statement to add/set values to one or more objects in the database.

	@param pxid Pointer to the XML import data.
	@param sdb Reference to the open SQL database ODBC connection.
	@param cline Number of the line in the source file where the call to this function occurs.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::UpdateDatabaseObjects(SqlDb & sdb, int cline)
{
	SqlStatement sstmt;
	sstmt.Init(sdb);
	RETCODE rc;
	SDWORD cb;
	int iv;
	for (iv = 0; iv < m_vdbl.Size(); ++iv)
	{
		rc = SQLBindParameter(sstmt.Hstmt(), static_cast<unsigned short>(iv + 1),
			SQL_PARAM_INPUT, SQL_C_DOUBLE, SQL_DOUBLE, 0, 0, &m_vdbl[iv], 0, &cb);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	}
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(m_staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), cline, m_staCmd.Chars());
	sstmt.Clear();
	m_staCmd.Clear();
	m_vdbl.Clear();
}

/*----------------------------------------------------------------------------------------------
	Resolve the pszSystemId path.  If pszSystemId contains an absolute path, then use it
	verbatim. Otherwise, append pszSystemId to the directory path part of pszBase.

	@param pszBase Pathname to the XML input file.
	@param pszSystemId System Id string read from the XML file.
	@param stabpFile Reference to the output string object.
----------------------------------------------------------------------------------------------*/
static void ResolvePath(const char * pszBase, const char * pszSystemId,
	StrAnsiBufPath & stabpFile)
{
	if (!pszBase ||
		(*pszSystemId == '\\') ||
		(isascii(pszSystemId[0]) && isalpha(pszSystemId[0]) && (pszSystemId[1] == ':')) ||
		(*pszSystemId == '/'))
	{
		stabpFile.Assign(pszSystemId);
	}
	else
	{
		stabpFile.Assign(pszBase);
		int ich1 = stabpFile.ReverseFindCh('/');
		int ich2 = stabpFile.ReverseFindCh('\\');
		if (ich1 == -1)
		{
			if (ich2 == -1)
			{
				// Neither a / or \ in pszBase: it must be a plain filename.
				stabpFile.Clear();
			}
			else
			{
				// One or more \'s in pszBase: truncate to the last one.
				stabpFile.SetLength(ich2 + 1);
			}
		}
		else if (ich2 == -1)
		{
			// One or more /'s in pszBase: truncate to the last one.
			stabpFile.SetLength(ich1 + 1);
		}
		else
		{
			// Both / and \ in pszBase: truncate to the last one.
			if (ich1 < ich2)
				stabpFile.SetLength(ich2 + 1);
			else
				stabpFile.SetLength(ich1 + 1);
		}
		stabpFile.Append(pszSystemId);
	}
}

/*----------------------------------------------------------------------------------------------
	Constructor.

	@param pfwxd Pointer to an FwXmlData object that encapsulates the database we are wanting
				to fill with imported data.
----------------------------------------------------------------------------------------------*/
FwXmlImportData::FwXmlImportData(FwXmlData * pfwxd)
	: m_pfwxd(pfwxd), m_sdb(pfwxd->m_sdb)
{
	//fprintf(stdout,"inside constructor\n");
	m_pfileLog = NULL;
	m_parser = 0;
	m_celemStart = 0;
	m_celemEnd = 0;
	m_cobj = 1;				// Start at 1 because object id's are based on this, and they must
	m_cobj2 = 1;			// start at one.
	m_fError = false;
	m_hr = S_OK;
	m_chGuid = 0;
	m_cchGuid =0;
	m_fInString = false;
	m_fInRun = false;
	m_fInUnicode = false;
	m_fInBinary = false;
	m_fSingle = false;
	m_cCustom = 0;
	m_cSql = 0;
	m_fIcuLocale = false;
	m_fInRuleProp = false;
	m_fCustom = false;
	m_cobjNewTargets = 0;
	m_wsAnal = 0;
	m_wsVern = 0;
	m_cBlk = 0;
	m_cBlkTotal = 0;
	m_cStep = 0;
	// Set the toplevel element information for the parser to use.
	m_hvoOwner = -1;
	m_iclsOwner = -1;
	m_flid = 0;
	m_staBeginTag.Assign("FwDatabase");
	m_hvoMin = 0;
	m_hvoObj = 0;
	m_guidObj = GUID_NULL;

	m_fMerge = false;
	m_fAllowNewWritingSystems = false;
	m_hvoFirst = 0;

	LoadIndexedStringFields();
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwXmlImportData::~FwXmlImportData()
{
	if (m_pfileLog)
	{
		fclose(m_pfileLog);
		m_pfileLog = NULL;
	}
	if (m_parser)
	{
		XML_ParserFree(m_parser);
		m_parser = 0;
	}
}

/*----------------------------------------------------------------------------------------------
	Write a message to the log file (if one is open).

	@param pszMsg NUL-terminated message string.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::LogMessage(const char * pszMsg)
{
	AssertPsz(pszMsg);
	if (m_pfileLog)
	{
		if (m_parser)
		{
			fprintf(m_pfileLog, "%s:%d: ",
				m_stabpFile.Chars(), XML_GetCurrentLineNumber(m_parser));
		}
		fputs(pszMsg, m_pfileLog);
	}
}

/*----------------------------------------------------------------------------------------------
	Calculate the type of XML element we have here.

	@param pszElement XML element name read from the input file.

	@return Xml element type: basic, custom, object, etc.
----------------------------------------------------------------------------------------------*/
ElemTypeInfo FwXmlImportData::GetElementType(const char * pszElement)
{
	AssertPsz(pszElement);
	ElemTypeInfo eti;
	if (m_hmceti.Retrieve(pszElement, &eti))
	{
		return eti;
	}
	int cpt = FwXml::BasicType(pszElement);
	if (cpt != -1)
	{
		if (cpt == kcptNil)
		{
			eti.m_elty = keltyDatabase;
			eti.m_icls = -1;
		}
		else
		{
			eti.m_elty = keltyBasicProp;
			eti.m_cpt = cpt;
		}
	}
	else if (!strcmp(pszElement, "AdditionalFields"))
	{
		eti.m_elty = keltyAddProps;
		eti.m_icls = -1;
	}
	else if (!strcmp(pszElement, "CustomField"))
	{
		eti.m_elty = keltyDefineProp;
		eti.m_icls = -1;
	}
	else if (!strcmp(pszElement, "Custom") || !strcmp(pszElement, "CustomStr") ||
		!strcmp(pszElement, "CustomLink") || !strcmp(pszElement, "CustomObj"))
	{
		eti.m_elty = keltyCustomProp;
		eti.m_icls = -1;
	}
	else if (!strcmp(pszElement, "LexicalRelations5016"))
	{
		eti.m_elty = keltyVirtualProp;
		eti.m_ifld = 1;
	}
	else if (!strcmp(pszElement, "CrossReferences5002"))
	{
		eti.m_elty = keltyVirtualProp;
		eti.m_ifld = 2;
	}
	else
	{
		SetFieldOrClassType(pszElement, eti);
	}
	m_hmceti.Insert(pszElement, eti);
	return eti;
}

/*----------------------------------------------------------------------------------------------
	Check whether the element name ends with a number.
----------------------------------------------------------------------------------------------*/
static inline bool EndsWithNumber(const char * pszElement)
{
	AssertPsz(pszElement);
	int cch = strlen(pszElement);
	return isdigit(pszElement[cch-1]);
}

/*----------------------------------------------------------------------------------------------
	Calculate the type of XML element we have here, which is either a class or a field.

	@param pszElement XML element name read from the input file.
	@param eti reference to ElemTypeInfo which should be either a keltyPropName or keltyObject
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetFieldOrClassType(const char * pszElement, ElemTypeInfo & eti)
{
	StrAnsi sta;
	StrAnsi staFmt;
	if (EndsWithNumber(pszElement))
	{
		StrUni stu = pszElement;
		int ifld;
		if (!m_pfwxd->m_hmsuXmlifld.Retrieve(stu, &ifld))
		{
			if (m_pfwxd->m_hmsuicls.Retrieve(stu, &ifld))
			{
				// Somebody defined a class whose name ends with a number!
				eti.m_elty = keltyObject;
				eti.m_icls = ifld;
			}
			else
			{
				// "Invalid XML Element: unknown field \"%s\""
				staFmt.Load(kstidXmlErrorMsg054);
				sta.Format(staFmt.Chars(), pszElement);
				LogMessage(sta.Chars());
				eti.m_elty = keltyBad;
				eti.m_icls = -1;
			}
		}
		else
		{
			eti.m_elty = keltyPropName;
			eti.m_ifld = ifld;
		}
	}
	else
	{
		StrUni stu = pszElement;
		int icls;
		if (!m_pfwxd->m_hmsuicls.Retrieve(stu, &icls))
		{
			// "Invalid XML Element: unknown class \"%s\""
			staFmt.Load(kstidXmlErrorMsg053);
			sta.Format(staFmt.Chars(), pszElement);
			LogMessage(sta.Chars());
			eti.m_elty = keltyBad;
			eti.m_icls = -1;
		}
		else
		{
			eti.m_elty = keltyObject;
			eti.m_icls = icls;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Load the default analysis writing system from the language project database.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::DefaultAnalysisWritingSystem()
{
	if (m_wsAnal == 0)
	{
		SqlStatement sstmt;
		RETCODE rc;
		sstmt.Init(m_sdb);

		// Get the first (default) analysis writing system from the database.
		StrAnsi sta;
		const char * pszTable = (m_pfwxd->DbVersion() <= 200202) ?
			"LanguageProject_CurrentAnalysisWritingSystems" : "LangProject_CurAnalysisWss";
		if (CURRENTDB == MSSQL)
		{
			sta.Format("SELECT TOP 1 Dst FROM %s ORDER BY Ord;", pszTable);
		}
		else if (CURRENTDB == FB)
		{
			sta.Format("SELECT FIRST 1 Dst FROM %s ORDER BY Ord;", pszTable);
		}
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(sta.Chars())),
			SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, sta.Chars());
		SDWORD cbT;
		rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &m_wsAnal,
			isizeof(m_wsAnal), &cbT);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		sstmt.Clear();
	}
	return m_wsAnal;
}

/*----------------------------------------------------------------------------------------------
	Load the default vernacular writing system from the language project database.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::DefaultVernacularWritingSystem()
{
	if (m_wsVern == 0)
	{
		SqlStatement sstmt;
		RETCODE rc;
		sstmt.Init(m_sdb);

		// Get the first (default) vernacular writing system from the database.
		StrAnsi sta;
		const char * pszTable = (m_pfwxd->DbVersion() <= 200202) ?
			"LanguageProject_CurrentVernacularWritingSystems" : "LangProject_CurVernWss";
		if(CURRENTDB == MSSQL) {
			sta.Format("SELECT TOP 1 Dst FROM %s ORDER BY Ord;", pszTable);
		}
		else if(CURRENTDB == FB) {
			sta.Format("SELECT FIRST 1 Dst FROM %s ORDER BY Ord;", pszTable);
		}
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(sta.Chars())),
			SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, sta.Chars());
		SDWORD cbT;
		rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &m_wsVern,
			isizeof(m_wsVern), &cbT);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		sstmt.Clear();
	}
	return m_wsVern;
}


/*----------------------------------------------------------------------------------------------
	Create any custom fields defined for this database.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateCustomFields()
{
	if (!m_vcfi.Size())
		return;				// No custom fields to create.

	SqlStatement sstmt;
	RETCODE rc;

	// Put the database into single-user mode while modifying the schema with these custom
	// fields.
	//SetSingleUserDb(true);
	for (int icfi = 0; icfi < m_vcfi.Size(); ++icfi)
	{
		CustomFieldInfo & cfi = m_vcfi[icfi];

		// Update the schema in the database.
		CreateAddCustomFieldSql(cfi);

		sstmt.Init(m_sdb);
		rc = SQLPrepareA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(m_staCmd.Chars())),
			SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

		long nfid = cfi.m_fid;
		SQLINTEGER rgcb[5];	// For input parameters, these values are read at SQLExecute() time.
		rgcb[0] = sizeof(nfid);

		if (CURRENTDB == MSSQL) {
			// Bind the input/output parameter.
			rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT_OUTPUT, SQL_C_SLONG,
				SQL_INTEGER, 0, 0, &nfid, 0, &rgcb[0]);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		}
		else if (CURRENTDB == FB) {
			// Bind the first input parameter
			rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_SLONG,
				SQL_INTEGER, 0, 0, &nfid, 0, &rgcb[0]);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

			// Bind the output parameter.
			// It's the same as the first input parameter,
			// but in firebird you have to specify them separately.
			// See ::CreateAddCustomFieldSql  RETURNING_VALUES (?)
			rc = SQLBindParameter(sstmt.Hstmt(), 6, SQL_PARAM_OUTPUT, SQL_C_SLONG,
				SQL_INTEGER, 0, 0, &nfid, 0, &rgcb[0]);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		}

		// Bind the input parameters.
		BindAddCustomFieldParameters(sstmt.Hstmt(), cfi, rgcb);

		rc = SQLExecute(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, m_staCmd.Chars());

		// NOTE: the output parameters and return values are unavailable until
		// SQLMoreResults returns SQL_NO_DATA.
		do
		{
			rc = SQLMoreResults(sstmt.Hstmt());
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		} while (rc != SQL_NO_DATA);
		sstmt.Clear();
		m_staCmd.Clear();

		// We need to update the appropriate fdfi.m_fid to the value just established (nfid).
		StrUni stuXml;
		stuXml.Format(L"%s%d", cfi.m_stuName.Chars(), cfi.m_cid); // Implied (virtual) XML name.
		cfi.m_fid = (int)nfid;
		int ifld = 0;
		m_pfwxd->m_hmsuXmlifld.Retrieve(stuXml, &ifld);
		Assert(m_pfwxd->FieldInfo(ifld).fCustom);
		m_pfwxd->m_vfdfi[ifld].fid = cfi.m_fid;
		m_pfwxd->m_hmfidifld.Insert(cfi.m_fid, ifld);
	}
	//SetSingleUserDb(false);
}

/*----------------------------------------------------------------------------------------------
	Fill in m_staCmd with the SQL command to create the indicated custom object.

	@param cfi reference to the custom field information object
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateAddCustomFieldSql(const CustomFieldInfo & cfi)
{
	/*
	  proc [AddCustomField$]
		  @flid int output,    - the newly generated field Id (output parameter)
		  @name varchar(100),  - the name of the custom field
		  @type int,           - the type code for the custom field.
		  @clid int,           - the class id of the class to which the field is
								 being added
		  @clidDst int = null, - the class id of the target class for OwningAtom
								 or ReferenceAtom fields (opt)
		  @Min bigint = null,  - the minimum value allowed for Type 2 integer
								 field(opt)
		  @Max bigint = null,  - the maximum value allowed for Type 2 integer
								 field (opt)
		  @Big bit = null,     - flag that determines if a binary datatype
								 should be stored as varbinary (@Big=0) or image
								 (@Big=1) (opt)
		  @nvcUserLabel	NVARCHAR(100) = NULL,
		  @nvcHelpString	NVARCHAR(100) = NULL,
		  @nListRootId	INT  = NULL,
		  @nWsSelector	INT = NULL,
		  @ntXmlUI		NTEXT = NULL

	  The fifth argument has a default value of NULL because it is used for only
	  6 of 23 possible types of custom fields.
	*/
	if (CURRENTDB == FB) {
		if (cfi.m_cidDst)
			m_staCmd.Format("{ EXECUTE PROCEDURE AddCustomField$(?, ?, %d, %d, %d,",
				cfi.m_cpt, cfi.m_cid, cfi.m_cidDst);
		else
			m_staCmd.Format("{ EXECUTE PROCEDURE AddCustomField$(?, ?, %d, %d, null,",
				cfi.m_cpt, cfi.m_cid);
	}
	else if (CURRENTDB == MSSQL) {
		// The CALL syntax below is the only way I've found to get the output
		// parameter value from executing the stored procedure in ODBC.
		if (cfi.m_cidDst)
			m_staCmd.Format("{ CALL AddCustomField$ (?, ?, %d, %d, %d",
				cfi.m_cpt, cfi.m_cid, cfi.m_cidDst);
		else
			m_staCmd.Format("{ CALL AddCustomField$ (?, ?, %d, %d, null",
				cfi.m_cpt, cfi.m_cid);
	}
	if (cfi.m_stabMin.Length())
		m_staCmd.FormatAppend(", %s", cfi.m_stabMin.Chars());
	else
		m_staCmd.Append(", null");
	if (cfi.m_stabMax.Length())
		m_staCmd.FormatAppend(", %s", cfi.m_stabMax.Chars());
	else
		m_staCmd.Append(", null");
	if (cfi.m_stabBig.Length())
		m_staCmd.FormatAppend(", %s", cfi.m_stabBig.Chars());
	else
		m_staCmd.Append(", null");
	if (cfi.m_stuUserLabel.Length())
		m_staCmd.Append(", ?");
	else
		m_staCmd.Append(", null");
	if (cfi.m_stuHelpString.Length())
		m_staCmd.Append(", ?");
	else
		m_staCmd.Append(", null");
	m_staCmd.Append(", null");		// nListRootId -- still unknown
	if (cfi.m_stabWsSelector.Length())
		m_staCmd.FormatAppend(", %s", cfi.m_stabWsSelector.Chars());
	else
		m_staCmd.Append(", null");
	if (cfi.m_stuXmlUI.Length())
		m_staCmd.Append(", ?");
	else
		m_staCmd.Append(", null");
	if (CURRENTDB == FB) {
		//TODO (Steve Miller) firebird output parameter syntax
		m_staCmd.Append(") RETURNING_VALUES (?); }");
	}
	else if (CURRENTDB == MSSQL) {
		m_staCmd.Append(") }");
	}
}


/*----------------------------------------------------------------------------------------------
	Bind the input parameters for the SQL command created by CreateAddCustomFieldSql().

	@param hstmt handle to an ODBC SQL statement object
	@param cfi reference to the custom field information object
	@param rgcb array of byte counts for output from executing SQL statement
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::BindAddCustomFieldParameters(SQLHSTMT hstmt, const CustomFieldInfo & cfi,
	SQLINTEGER * rgcb)
{
	RETCODE rc;
	// Bind field name to parameter 2.
	if (cfi.m_stuName.Length())
	{
		SQLINTEGER cchName = cfi.m_stuName.Length();
		SQLINTEGER cbName = cchName * isizeof(wchar);
		rgcb[1] = cbName;
		rc = SQLBindParameter(hstmt, 2, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
			cchName, 0, const_cast<wchar *>(cfi.m_stuName.Chars()), cbName,
			&rgcb[1]);
		VerifySqlRc(rc, hstmt, __LINE__);
	}
	SQLUSMALLINT nParam = 3;
	// Bind user label to parameter 3 if non-null;
	if (cfi.m_stuUserLabel.Length())
	{
		SQLINTEGER cchUserLabel = cfi.m_stuUserLabel.Length();
		SQLINTEGER cbUserLabel = cchUserLabel * isizeof(wchar);
		rgcb[nParam - 1] = cbUserLabel;
		rc = SQLBindParameter(hstmt, nParam, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
			cchUserLabel, 0, const_cast<wchar *>(cfi.m_stuUserLabel.Chars()), cbUserLabel,
			&rgcb[nParam - 1]);
		VerifySqlRc(rc, hstmt, __LINE__);
		++nParam;
	}
	// Bind help string to parameter 3 or 4 if non-null.
	if (cfi.m_stuHelpString.Length())
	{
		SQLINTEGER cchHelpString = cfi.m_stuHelpString.Length();
		SQLINTEGER cbHelpString = cchHelpString * isizeof(wchar);
		rgcb[nParam - 1] = cbHelpString;
		rc = SQLBindParameter(hstmt, nParam, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
			cchHelpString, 0, const_cast<wchar *>(cfi.m_stuHelpString.Chars()), cbHelpString,
			&rgcb[nParam - 1]);
		VerifySqlRc(rc, hstmt, __LINE__);
		++nParam;
	}
	// Bind XML UI string to parameter 3 or 4 or 5 if non-null.
	if (cfi.m_stuXmlUI.Length())
	{
		SQLINTEGER cchXmlUI = cfi.m_stuXmlUI.Length();
		SQLINTEGER cbXmlUI = cchXmlUI * isizeof(wchar);
		rgcb[nParam - 1] = cbXmlUI;
		rc = SQLBindParameter(hstmt, nParam, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
			cchXmlUI, 0, const_cast<wchar *>(cfi.m_stuXmlUI.Chars()), cbXmlUI,
			&rgcb[nParam - 1]);
		VerifySqlRc(rc, hstmt, __LINE__);
	}
}


/*----------------------------------------------------------------------------------------------
	Fix any custom fields which contain a list reference.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::FixCustomListRefFields()
{
	int cFixesNeeded = 0;
	for (int icfi = 0; icfi < m_vcfi.Size(); ++icfi)
	{
		CustomFieldInfo & cfi = m_vcfi[icfi];
		if (cfi.m_stabListRootId.Length())
			++cFixesNeeded;
	}
	if (!cFixesNeeded == 0)
		return;			// no fixes needed.

	StrAnsi sta;
	StrAnsi staFmt;

	//SetSingleUserDb(true);
	if (CURRENTDB == FB) {
		ExecuteSimpleSQL("ALTER TRIGGER T_BU0_FIELD$ INACTIVE;", __LINE__);
	}
	else if (CURRENTDB == MSSQL) {
		ExecuteSimpleSQL("ALTER TABLE Field$ DISABLE TRIGGER TR_Field$_No_Upd;", __LINE__);
	}
	for (int icfi = 0; icfi < m_vcfi.Size(); ++icfi)
	{
		CustomFieldInfo & cfi = m_vcfi[icfi];
		if (cfi.m_stabListRootId.Length())
		{
			int hobjList = 0;
			if (cfi.m_stabListRootId.Length())
			{
				GUID guidList;
				bool fUpdate;
				if (FwXml::ParseGuid(cfi.m_stabListRootId.Chars() + 1, &guidList))
					fUpdate = m_hmguidhobj.Retrieve(guidList, &hobjList);
				else
					fUpdate = m_hmcidhobj.Retrieve(cfi.m_stabListRootId.Chars(), &hobjList);
				if (!fUpdate)
				{
					// die, or ignore?  for now, we'll ignore.
					// "Invalid list root id '%s' in custom field definition [%S]??\n"
					staFmt.Load(kstidXmlErrorMsg017);
					sta.Format(staFmt.Chars(), cfi.m_stabListRootId.Chars(), cfi.m_stuName);
					LogMessage(sta.Chars());
				}
			}
			if (hobjList)
			{
				if(CURRENTDB == FB || CURRENTDB == MSSQL) {
					m_staCmd.Format("UPDATE Field$ SET ListRootId = %d WHERE Id=%d;",
						hobjList, cfi.m_fid);
				}
				ExecuteSimpleSQL(m_staCmd.Chars(), __LINE__);
			}
		}
	}
	if (CURRENTDB == FB) {
		ExecuteSimpleSQL("ALTER TRIGGER T_BU0_FIELD$ ACTIVE;", __LINE__);
	}
	else if (CURRENTDB == MSSQL) {
		ExecuteSimpleSQL("ALTER TABLE Field$ ENABLE TRIGGER TR_Field$_No_Upd;", __LINE__);
	}
	//SetSingleUserDb(false);
}

/*----------------------------------------------------------------------------------------------
	Create the objects in the database.

	@param padvi pointer to a progress report object
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateObjects(IAdvInd * padvi)
{
	if (m_hvoOwner == -1 && !m_rod.m_vhobj.Size())
	{
		Assert(!m_ood.m_vhobj.Size());
		return;
	}
	if (m_hvoOwner != -1)
	{
		Assert(!m_rod.m_vhobj.Size());
	}
	ReleaseExcessSpace();

	// Create all of the root objects.

	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	int cobj = m_rod.m_vhobj.Size();
	int cStep = 0;
	int cCreated = 0;
	if (cobj < kceParamMin)
	{
		CreateFewRootObjects(padvi, sstmt.Hstmt(), cobj, cCreated, cStep);
	}
	else
	{
		CreateManyRootObjects(padvi, sstmt.Hstmt(), cobj, cCreated, cStep);
	}
	m_staCmd.Clear();
	sstmt.Clear();

	// Record which objects have been created.

	CreatedObjectSet cos(m_rod.m_vhobj.Size() + m_ood.m_vhobj.Size() + m_veod.Size());
	RecordCreatedRootObjects(cos);
	if (m_fMerge)
		RecordExistingObjects(cos);

	// Create all of the owned objects.
	//fprintf(stdout,"before CreateOwnedObjects\n");
	CreateOwnedObjects(padvi, cos, cCreated, cStep);
}


/*----------------------------------------------------------------------------------------------
	Release any excess space consumed by various member vectors.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ReleaseExcessSpace()
{
	Assert(m_rod.m_vhobj.Size() == m_rod.m_vguid.Size());
	Assert(m_rod.m_vhobj.Size() == m_rod.m_vicls.Size());
	Assert(m_ood.m_vhobj.Size() == m_ood.m_vguid.Size());
	Assert(m_ood.m_vhobj.Size() == m_ood.m_vicls.Size());
	Assert(m_ood.m_vhobj.Size() == m_ood.m_vhobjOwner.Size());
	Assert(m_ood.m_vhobj.Size() == m_ood.m_vfidOwner.Size());
	Assert(m_ood.m_vhobj.Size() == m_ood.m_vordOwner.Size());
	Assert(m_ood.m_vhobj.Size() == m_ood.m_vcpt.Size());

	// Release any excess space: we may be needing it!
	m_rod.m_vhobj.EnsureSpace(0, true);
	m_rod.m_vguid.EnsureSpace(0, true);
	m_rod.m_vicls.EnsureSpace(0, true);
	m_ood.m_vhobj.EnsureSpace(0, true);
	m_ood.m_vguid.EnsureSpace(0, true);
	m_ood.m_vicls.EnsureSpace(0, true);
	m_ood.m_vhobjOwner.EnsureSpace(0, true);
	m_ood.m_vfidOwner.EnsureSpace(0, true);
	m_ood.m_vordOwner.EnsureSpace(0, true);
	m_ood.m_vcpt.EnsureSpace(0, true);
}


/*----------------------------------------------------------------------------------------------
	Create a limited number of root objects (cobj < kceParamMin).

	@param padvi pointer to a progress report object
	@param hstmt handle to an ODBC SQL statement object
	@param cobj number of root objects to create
	@param cCreated reference to the number of objects created thus far (initially zero)
	@param cStep reference to the number of progress report steps that have been made
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateFewRootObjects(IAdvInd * padvi, SQLHSTMT hstmt, int cobj,
	int & cCreated, int & cStep)
{
	m_staCmd.Clear();
	if (cobj)
	{
		for (int iobj = 0; iobj < cobj; ++iobj)
		{
			if (iobj)
				m_staCmd.Append("; ");
			if(CURRENTDB == FB) {
				//TODO: not sure about RETURNING_VALUES.  They aren't actually needed by the app here.
				m_staCmd.FormatAppend("EXECUTE PROCEDURE CreateObject$ (%u, %u, %g);",
					m_pfwxd->ClassInfo(m_rod.m_vicls[iobj]).cid,
					m_rod.m_vhobj[iobj],
					&m_rod.m_vguid[iobj]);
			}
			if(CURRENTDB == MSSQL) {
				m_staCmd.FormatAppend("EXEC CreateObject$ %u, %u,'%g';",
					m_pfwxd->ClassInfo(m_rod.m_vicls[iobj]).cid,
					m_rod.m_vhobj[iobj],
					&m_rod.m_vguid[iobj]);
			}
		}
		RETCODE rc = SQLExecDirectA(hstmt,
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(m_staCmd.Chars())), SQL_NTS);
		VerifySqlRc(rc, hstmt, __LINE__, m_staCmd.Chars());
		ReportCreateProgress(padvi, cobj, cCreated, cStep);
	}
}


/*----------------------------------------------------------------------------------------------
	Create a large number of root objects (cobj >= kceParamMin).  This is done in stages, no
	more than kceSeg objects at a time.

	@param padvi pointer to a progress report object
	@param hstmt handle to an ODBC SQL statement object
	@param cobj number of root objects to create
	@param cCreated reference to the number of objects created thus far (initially zero)
	@param cStep reference to the number of progress report steps that have been made
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateManyRootObjects(IAdvInd * padvi, SQLHSTMT hstmt, int cobj,
	int & cCreated, int & cStep)
{
	//fprintf(stdout,"cobj=%d\n",cobj);
	OdbcType ot;
	SQLUINTEGER cParamsProcessed = 0;
	Vector<int> vcid;
	Vector<SQLINTEGER> vcbcid;
	Vector<SQLUSMALLINT> vnParamStatus;
	Vector<SQLINTEGER> vcbhobj;
	Vector<SQLINTEGER> vcbguid;
	int cobjSeg = cobj < kceSeg ? cobj : kceSeg;
	vcid.Resize(cobjSeg);
	vcbcid.Resize(cobjSeg);
	vnParamStatus.Resize(cobjSeg);	// Maximum number of objects to create in one pass.
	vcbhobj.Resize(cobjSeg);
	vcbguid.Resize(cobjSeg);

	if (CURRENTDB == MSSQL) {
		m_staCmd.Format("EXEC CreateObject$ ?,?,?;");
	}
	else if (CURRENTDB == FB) {
		//TODO: not sure about RETURNING_VALUES.  They aren't actually needed by the app here.
		m_staCmd.Format("EXECUTE PROCEDURE CreateObject$ (?,?,?);");
	}

	RETCODE rc = SQLPrepareA(hstmt,
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(m_staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, hstmt, __LINE__);
	// Set the SQL_ATTR_PARAM_BIND_TYPE statement attribute to use column-wise binding.
	// Specify the number of elements in each parameter array.
	rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN,
		0);
	VerifySqlRc(rc, hstmt, __LINE__);
	if(CURRENTDB == FB){
		rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAMSET_SIZE,
			reinterpret_cast<void *>(1), 0);
		VerifySqlRc(rc, hstmt, __LINE__);
	}
	else if(CURRENTDB == MSSQL){
		rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAMSET_SIZE,
			reinterpret_cast<void *>(cobjSeg), 0);
		VerifySqlRc(rc, hstmt, __LINE__);
	}
	// Specify an array in which to return the status of each set of parameters.
	rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAM_STATUS_PTR, vnParamStatus.Begin(),
		0);
	VerifySqlRc(rc, hstmt, __LINE__);
	// Specify an SQLUINTEGER value in which to return the number of sets of parameters
	// processed.
	rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAMS_PROCESSED_PTR, &cParamsProcessed,
		0);
	VerifySqlRc(rc, hstmt, __LINE__);
	if(CURRENTDB == MSSQL){
		for (int ieSegStart = 0; ieSegStart < cobj; ieSegStart += kceSeg)
		{
			cobjSeg = cobj - ieSegStart;
			if (cobjSeg > kceSeg)
				cobjSeg = kceSeg;
			if (ieSegStart && cobjSeg < kceSeg)
			{
				// Specify the number of elements in each parameter array for final, smaller
				// section.
				rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAMSET_SIZE,
					reinterpret_cast<void *>(cobjSeg), 0);
				VerifySqlRc(rc, hstmt, __LINE__);
			}
			// Fill in the values for the first parameter.
			for (int iobj = ieSegStart; iobj < ieSegStart + cobjSeg; ++iobj)
				vcid[iobj - ieSegStart] = m_pfwxd->ClassInfo(m_rod.m_vicls[iobj]).cid;
			// Bind the parameters.
			rc = SQLBindParameter(hstmt, 1, SQL_PARAM_INPUT, ot.SLONG, SQL_INTEGER,
				0, 0, vcid.Begin(), 0, vcbcid.Begin());
			VerifySqlRc(rc, hstmt, __LINE__);
			rc = SQLBindParameter(hstmt, 2, SQL_PARAM_INPUT, ot.SLONG, SQL_INTEGER,
				0, 0, m_rod.m_vhobj.Begin() + ieSegStart, 0, vcbhobj.Begin());
			VerifySqlRc(rc, hstmt, __LINE__);
			rc = SQLBindParameter(hstmt, 3, SQL_PARAM_INPUT, ot.GUID, SQL_GUID,
				0, 0, m_rod.m_vguid.Begin() + ieSegStart, 0, vcbguid.Begin());
			VerifySqlRc(rc, hstmt, __LINE__);

			/*for (int iobj = ieSegStart; iobj < ieSegStart + cobjSeg; ++iobj){
				fprintf(stdout,"1 = %d\n",vcid[iobj]);
				fprintf(stdout,"2 = %d\n",m_rod.m_vhobj[iobj]);
				fprintf(stdout,"3 = %d\n\n",m_rod.m_vguid[iobj]);
			}*/

			rc = SQLExecute(hstmt);

			CheckRootObjParamsForSuccess(vnParamStatus.Begin(), cParamsProcessed, ieSegStart,
				cobjSeg);
			//fprintf(stdout,"I got this far rc=%d\n",rc);
			//system("PAUSE");
			VerifySqlRc(rc, hstmt, __LINE__, m_staCmd.Chars());
			ReportCreateProgress(padvi, cobjSeg, cCreated, cStep);
			//fprintf(stdout,"end of loop iter=%d\n\n",ieSegStart);
		}
	}

	else if(CURRENTDB == FB){
		for (int iobj = 0; iobj < cobj; ++iobj)
				vcid[iobj] = m_pfwxd->ClassInfo(m_rod.m_vicls[iobj]).cid;

		for (int ieSegStart = 0; ieSegStart < cobj; ++ieSegStart){
			// Bind the parameters.
			rc = SQLBindParameter(hstmt, 1, SQL_PARAM_INPUT, ot.SLONG, SQL_INTEGER,
				0, 0, vcid.Begin() + ieSegStart, 0, vcbcid.Begin());
			VerifySqlRc(rc, hstmt, __LINE__);
			rc = SQLBindParameter(hstmt, 2, SQL_PARAM_INPUT, ot.SLONG, SQL_INTEGER,
				0, 0, m_rod.m_vhobj.Begin() + ieSegStart, 0, vcbhobj.Begin());
			VerifySqlRc(rc, hstmt, __LINE__);
			rc = SQLBindParameter(hstmt, 3, SQL_PARAM_INPUT, ot.GUID, SQL_GUID,
				0, 0, m_rod.m_vguid.Begin() + ieSegStart, 0, vcbguid.Begin());
			VerifySqlRc(rc, hstmt, __LINE__);

			//debug print
			//fprintf(stdout,"1 = %d\n",vcid[ieSegStart]);
			//fprintf(stdout,"2 = %d\n",m_rod.m_vhobj[ieSegStart]);
			//fprintf(stdout,"3 = %d\n",m_rod.m_vguid[ieSegStart]);

			rc = SQLExecute(hstmt);

			CheckRootObjParamsForSuccess(vnParamStatus.Begin(), cParamsProcessed, ieSegStart,
				cobjSeg);
			VerifySqlRc(rc, hstmt, __LINE__, m_staCmd.Chars());
			ReportCreateProgress(padvi, cobjSeg, cCreated, cStep);
			//fprintf(stdout,"end of loop iter=%d\n\n",ieSegStart);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing.

	@param rgnParamStatus the array of parameter status codes returned by SQLExecute()
	@param cParamsProcessed the number of parameters processed by SQLExecute()
	@param ieSegStart the starting index for the current set of objects
	@param cobj the number of current objects being created by SQLExecute()
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CheckRootObjParamsForSuccess(SQLUSMALLINT * rgnParamStatus,
	int cParamsProcessed, int ieSegStart, int cobj)
{
	StrAnsi sta;
	StrAnsi staFmt;
	int cSuccess = 0;
	for (int i = 0; i < cParamsProcessed; ++i)
	{
		staFmt.Clear();
		int iobj = ieSegStart + i;
		int icls = m_rod.m_vicls[iobj];
		switch (rgnParamStatus[i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			++cSuccess;
			break;
		case SQL_PARAM_ERROR:
			// "ERROR creating %S (%d, %g)"
			staFmt.Load(kstidXmlErrorMsg020);
			break;
		case SQL_PARAM_UNUSED:
			// "UNUSED creating %S (%d, %g)"
			staFmt.Load(kstidXmlErrorMsg114);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "UNAVAILABLE INFO creating %S (%d, %g)"
			staFmt.Load(kstidXmlErrorMsg110);
			break;
		}
		if (staFmt.Length())
		{
			sta.Format(staFmt.Chars(),
				m_pfwxd->ClassName(icls).Chars(), m_rod.m_vhobj[iobj], &m_rod.m_vguid[iobj]);
			LogMessage(sta.Chars());
		}
	}
#ifdef LOG_SQL
	// "    %d Parameter%s processed, %d successful, %d attempted"
	staFmt.Load(kstidXmlInfoMsg001);
	StrAnsi staParam;
	if (cParamsProcessed == 1)
		staParam.Load(kstidParameter);
	else
		staParam.Load(kstidParameters);
	sta.Format(staFmt.Chars(), cParamsProcessed, staParam.Chars(), cSuccess, cobj);
	LogMessage(sta.Chars());
#endif
}


/*----------------------------------------------------------------------------------------------
	If padvi is not NULL, report the progress being made creating objects.

	@param padvi pointer to a progress report object
	@param cobj the number of current objects being created
	@param cCreated reference to the number of objects created thus far (initially zero)
	@param cStep reference to the number of progress report steps that have been made
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ReportCreateProgress(IAdvInd * padvi, int cobj, int & cCreated,
	int & cStep)
{
	if (padvi)
	{
		// My best estimate is that creating the objects is 46% of the progress to report.
		int cCreatedMax = m_rod.m_vhobj.Size() + m_ood.m_vhobj.Size();
		cCreated += cobj;
		int cStepNew = cCreated * 46 / cCreatedMax;
		if (cStepNew > cStep)
		{
			padvi->Step(cStepNew - cStep);
			cStep = cStepNew;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Record which objects have been created after creating all root objects.

	@param cos reference to the set of created objects
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::RecordCreatedRootObjects(CreatedObjectSet & cos)
{
	if (m_hvoOwner != -1)
	{
		Assert(!m_rod.m_vhobj.Size());
		cos.AddObject(m_hvoOwner);		// not really created, but...
	}
	for (int iobj = 0; iobj < m_rod.m_vhobj.Size(); ++iobj)
		cos.AddObject(m_rod.m_vhobj[iobj]);
	m_rod.m_vhobj.Clear();
	m_rod.m_vguid.Clear();
	m_rod.m_vicls.Clear();
}


/*----------------------------------------------------------------------------------------------
	Create all of the owned objects in several passes, each pass creating those objects owned
	by objects created in the previous pass.

	@param padvi pointer to a progress report object
	@param cos reference to the set of already created objects
	@param cCreated reference to the number of objects created thus far (initially zero)
	@param cStep reference to the number of progress report steps that have been made
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateOwnedObjects(IAdvInd * padvi, CreatedObjectSet & cos,
	int & cCreated, int & cStep)
{
	SqlStatement sstmt;
	int iobjMin = 0;
	int iobjMax = m_ood.m_vhobj.Size() - 1;
	int cobjAdded = 0;
	int cobjRemaining = 0;
	do
	{
		OwnedObjData ood;
		cobjAdded = 0;
		cobjRemaining = 0;
		int cobjPass = CollectOwnedObjects(cos, ood, iobjMin, iobjMax, cobjAdded,
			cobjRemaining);
		if (!cobjPass)
			break;

		// Create owned objects whose owners now exist.

		sstmt.Init(m_sdb);
		// REVIEW SteveMc: is this reasonable, avoiding SQLBindParameter for small numbers
		// of objects of a given class?
		if (cobjPass < kceParamMin)
		{
			CreateFewOwnedObjects(padvi, sstmt.Hstmt(), cobjPass, ood, cCreated, cStep);
		}
		else
		{
			CreateManyOwnedObjects(padvi, sstmt.Hstmt(), cobjPass, ood, cCreated, cStep);
		}
		// Record the objects that were created in this pass.
		for (int iobj = 0; iobj < cobjPass; ++iobj)
			cos.AddObject(ood.m_vhobj[iobj]);
		// Release memory that isn't needed any longer.
		ood.Clear();
		m_staCmd.Clear();
		sstmt.Clear();
		if (cobjRemaining && !cobjAdded)
		{
			StrAnsi sta(kstidXmlErrorMsg132);
			LogMessage(sta.Chars());
		}
	} while (cobjAdded && cobjRemaining);
	// Release all this stuff we don't need any longer.
	m_ood.Clear();
}


/*----------------------------------------------------------------------------------------------
	Collect into cos all objects which need to be created whose owner already exists.

	@param cos reference to the set of already created objects
	@param ood reference to a collection of objects to create (output)
	@param iobjMin starting index into m_ood (all owned objects to create)
	@param iobjMax ending index into m_ood
	@param cobjAdded number of objects added to ood (output)
	@param cobjRemaining number of objects in m_ood which remain to be created later (output)
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::CollectOwnedObjects(CreatedObjectSet & cos, OwnedObjData & ood,
	int & iobjMin, int & iobjMax, int & cobjAdded, int & cobjRemaining)
{
	for (int iobj = iobjMin; iobj <= iobjMax; ++iobj)
	{
		int icls = m_ood.m_vicls[iobj];
		if (icls == -1)
		{
			if (iobj == iobjMin)
				++iobjMin;
			continue;							// Has already been created.
		}
		if (cos.IsCreated(m_ood.m_vhobjOwner[iobj]))
		{
			++cobjAdded;
			ood.m_vicls.Push(m_pfwxd->ClassInfo(icls).cid);	// Store class id, not index.
			ood.m_vhobj.Push(m_ood.m_vhobj[iobj]);
			ood.m_vguid.Push(m_ood.m_vguid[iobj]);
			ood.m_vhobjOwner.Push(m_ood.m_vhobjOwner[iobj]);
			ood.m_vfidOwner.Push(m_ood.m_vfidOwner[iobj]);
			ood.m_vordOwner.Push(m_ood.m_vordOwner[iobj]);
			ood.m_vcpt.Push(m_ood.m_vcpt[iobj]);
			m_ood.m_vicls[iobj] = -1;			// Mark as already created (will be soon).
			if (iobj == iobjMin)
				++iobjMin;
		}
		else
		{
			++cobjRemaining;
		}
	}
	// Remove trailing set of created (or soon to be created) objects from consideration
	// in the next pass.
	while (iobjMax >= iobjMin && m_ood.m_vicls[iobjMax] == -1)
		--iobjMax;

	Assert(ood.m_vhobj.Size() == ood.m_vicls.Size());
	Assert(ood.m_vhobj.Size() == ood.m_vguid.Size());
	Assert(ood.m_vhobj.Size() == ood.m_vguid.Size());
	Assert(ood.m_vhobj.Size() == ood.m_vhobjOwner.Size());
	Assert(ood.m_vhobj.Size() == ood.m_vfidOwner.Size());
	Assert(ood.m_vhobj.Size() == ood.m_vordOwner.Size());
	Assert(ood.m_vhobj.Size() == ood.m_vcpt.Size());

	return ood.m_vhobj.Size();
}


/*----------------------------------------------------------------------------------------------
	Create the limited number of objects given in ood (cobjPass < kceParamMin).

	@param padvi pointer to a progress report object
	@param hstmt handle to an ODBC SQL statement object
	@param cobjPass number of objects in ood to create
	@param ood reference to a collection of objects to create
	@param cCreated reference to the number of objects created thus far (initially zero)
	@param cStep reference to the number of progress report steps that have been made
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateFewOwnedObjects(IAdvInd * padvi, SQLHSTMT hstmt, int cobjPass,
	OwnedObjData & ood, int & cCreated, int & cStep)
{
	// TODO SteveMc: verify that sequenced objects are in proper order already
	// Note that the top object may already exist when importing objects into an
	// existing database.  The next line checks for that condition.
	if (!m_hvoObj || cobjPass > 1 || ood.m_vhobj[0] != m_hvoObj)
	{
		m_staCmd.Clear();
		for (int iobj = 0; iobj < cobjPass; ++iobj)
		{
			if (m_staCmd.Length())
				m_staCmd.Append("; ");
			if(CURRENTDB == FB) {
				m_staCmd.FormatAppend("EXECUTE PROCEDURE CreateOwnedObject$ %u,%u,'%g',%u,%u,%u;",
					ood.m_vicls[iobj], ood.m_vhobj[iobj], &ood.m_vguid[iobj],
					ood.m_vhobjOwner[iobj], ood.m_vfidOwner[iobj], ood.m_vcpt[iobj]);
			}
			if(CURRENTDB == MSSQL) {
				m_staCmd.FormatAppend("EXEC CreateOwnedObject$ %u,%u,'%g',%u,%u,%u;",
					ood.m_vicls[iobj], ood.m_vhobj[iobj], &ood.m_vguid[iobj],
					ood.m_vhobjOwner[iobj], ood.m_vfidOwner[iobj], ood.m_vcpt[iobj]);
			}
		}
		RETCODE rc = SQLExecDirectA(hstmt,
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(m_staCmd.Chars())), SQL_NTS);
		VerifySqlRc(rc, hstmt, __LINE__, m_staCmd.Chars());
		ReportCreateProgress(padvi, cobjPass, cCreated, cStep);
	}
	else
	{
		Assert(m_guidObj != GUID_NULL && ood.m_vguid[0] == m_guidObj);
		Assert(m_hvoOwner && ood.m_vhobjOwner[0] == m_hvoOwner);
		Assert(m_flid && ood.m_vfidOwner[0] == m_flid);
	}
}


/*----------------------------------------------------------------------------------------------
	Create the large number of objects given in ood (cobjPass >= kceParamMin).

	@param padvi pointer to a progress report object
	@param hstmt handle to an ODBC SQL statement object
	@param cobjPass number of objects in ood to create
	@param ood reference to a collection of objects to create
	@param cCreated reference to the number of objects created thus far (initially zero)
	@param cStep reference to the number of progress report steps that have been made
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateManyOwnedObjects(IAdvInd * padvi, SQLHSTMT hstmt, int cobjPass,
	OwnedObjData & ood, int & cCreated, int & cStep)
{
	//fprintf(stdout,"cobjPass=%d\n",cobjPass);
	int ieSegStart;
	Vector<SQLUSMALLINT> vnParamStatus;
	SQLUINTEGER cParamsProcessed = 0;
	RETCODE rc;
	int cobj = (cobjPass < kceSeg) ? cobjPass : kceSeg;
	vnParamStatus.Resize(cobj);		// maximum size we'll need.

	// TODO SteveMc: verify that sequenced objects are in proper order already
	if (CURRENTDB == FB) {
		m_staCmd.Format("EXECUTE PROCEDURE CreateOwnedObject$ ?,?,?,?,?,?;");
	}
	if (CURRENTDB == MSSQL) {
		m_staCmd.Format("EXEC CreateOwnedObject$ ?,?,?,?,?,?;");
	}
	rc = SQLPrepareA(hstmt,
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(m_staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, hstmt, __LINE__);
	// Set the SQL_ATTR_PARAM_BIND_TYPE statement attribute to use column-wise
	// binding.
	rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN, 0);
	VerifySqlRc(rc, hstmt, __LINE__);
	// Specify the number of elements in each parameter array.
	if(CURRENTDB == MSSQL){
		rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAMSET_SIZE, reinterpret_cast<void *>(cobj), 0);
		VerifySqlRc(rc, hstmt, __LINE__);
	}
	else if(CURRENTDB == FB){
		rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAMSET_SIZE, reinterpret_cast<void *>(1), 0);
		VerifySqlRc(rc, hstmt, __LINE__);
	}
	// Specify an array in which to return the status of each set of parameters.
	rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAM_STATUS_PTR, vnParamStatus.Begin(), 0);
	VerifySqlRc(rc, hstmt, __LINE__);
	// Specify an SQLUINTEGER value in which to return the number of sets of
	// parameters processed.
	rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAMS_PROCESSED_PTR, &cParamsProcessed, 0);
	VerifySqlRc(rc, hstmt, __LINE__);
	if(CURRENTDB == MSSQL){
		for (ieSegStart = 0; ieSegStart < cobjPass; ieSegStart += kceSeg)
		{
			cobj = cobjPass - ieSegStart;
			if (cobj > kceSeg)
				cobj = kceSeg;
			CreateSomeOwnedObjects(hstmt, ood, ieSegStart, cobj, vnParamStatus.Begin(),
				cParamsProcessed);
			ReportCreateProgress(padvi, cobj, cCreated, cStep);
		}
	}
	else if(CURRENTDB == FB){
		for (ieSegStart = 0; ieSegStart < cobjPass; ieSegStart += 1)
		{
			CreateSomeOwnedObjects(hstmt, ood, ieSegStart, 1, vnParamStatus.Begin(),
				cParamsProcessed);
			ReportCreateProgress(padvi, cobj, cCreated, cStep);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Execute the prepared SQL statement to create cobj new owned objects.

	@param hstmt handle to an ODBC SQL statement object
	@param ood reference to a collection of objects to create
	@param ieSegStart the starting index into ood for the current set of objects
	@param cobj the number of objects from ood being created (cobj <= kceSeg)
	@param rgnParamStatus the array of parameter status codes returned by SQLExecute()
	@param cParamsProcessed reference to the number of parameters processed by SQLExecute()
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateSomeOwnedObjects(SQLHSTMT hstmt, OwnedObjData & ood, int ieSegStart,
	int cobj, SQLUSMALLINT * rgnParamStatus, SQLUINTEGER & cParamsProcessed)
{
	OdbcType ot;
	RETCODE rc;
	Vector<SQLINTEGER> vcbhobj;
	Vector<SQLINTEGER> vcbguid;
	Vector<SQLINTEGER> vcbhobjOwner;
	Vector<SQLINTEGER> vcbfid;
	Vector<SQLINTEGER> vcbcid;
	Vector<SQLINTEGER> vcbcpt;

	vcbhobj.Resize(cobj);
	vcbguid.Resize(cobj);
	vcbhobjOwner.Resize(cobj);
	vcbfid.Resize(cobj);
	vcbcid.Resize(cobj);
	vcbcpt.Resize(cobj);

	if (ieSegStart && cobj < kceSeg && CURRENTDB==MSSQL)
	{
		// Specify the number of elements in each parameter array for final,
		// smaller section.
		rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAMSET_SIZE, reinterpret_cast<void *>(cobj), 0);
		VerifySqlRc(rc, hstmt, __LINE__);
	}
	rc = SQLBindParameter(hstmt, 1, SQL_PARAM_INPUT, ot.SLONG, SQL_INTEGER, 0, 0,
		ood.m_vicls.Begin() + ieSegStart, 0, vcbcid.Begin());
	VerifySqlRc(rc, hstmt, __LINE__);
	rc = SQLBindParameter(hstmt, 2, SQL_PARAM_INPUT, ot.SLONG, SQL_INTEGER, 0, 0,
		ood.m_vhobj.Begin() + ieSegStart, 0, vcbhobj.Begin());
	VerifySqlRc(rc, hstmt, __LINE__);
	rc = SQLBindParameter(hstmt, 3, SQL_PARAM_INPUT, ot.GUID, SQL_GUID, 0, 0,
		ood.m_vguid.Begin() + ieSegStart, 0, vcbguid.Begin());
	VerifySqlRc(rc, hstmt, __LINE__);
	rc = SQLBindParameter(hstmt, 4, SQL_PARAM_INPUT, ot.SLONG, SQL_INTEGER, 0, 0,
		ood.m_vhobjOwner.Begin() + ieSegStart, 0, vcbhobjOwner.Begin());
	VerifySqlRc(rc, hstmt, __LINE__);
	rc = SQLBindParameter(hstmt, 5, SQL_PARAM_INPUT, ot.SLONG, SQL_INTEGER, 0, 0,
		ood.m_vfidOwner.Begin() + ieSegStart, 0, vcbfid.Begin());
	VerifySqlRc(rc, hstmt, __LINE__);
	rc = SQLBindParameter(hstmt, 6, SQL_PARAM_INPUT, ot.SLONG, SQL_INTEGER, 0, 0,
		ood.m_vcpt.Begin() + ieSegStart, 0, vcbcpt.Begin());
	VerifySqlRc(rc, hstmt, __LINE__);

	rc = SQLExecute(hstmt);
	CheckOwnedObjParamsForSuccess(ood, rgnParamStatus, cParamsProcessed, ieSegStart, cobj);
	VerifySqlRc(rc, hstmt, __LINE__, m_staCmd.Chars());
}

/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing.

	@param ood reference to a collection of objects to create
	@param rgnParamStatus the array of parameter status codes returned by SQLExecute()
	@param cParamsProcessed the number of parameters processed by SQLExecute()
	@param ieSegStart the starting index into ood for the current set of objects
	@param cobj the number of objects from ood being created
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CheckOwnedObjParamsForSuccess(OwnedObjData & ood,
	SQLUSMALLINT * rgnParamStatus, int cParamsProcessed, int ieSegStart, int cobj)
{
	int cSuccess = 0;
	StrAnsi staFmt;
	StrAnsi sta;
	for (int i = 0; i < cParamsProcessed; ++i)
	{
		staFmt.Clear();
		switch (rgnParamStatus[i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			++cSuccess;
#if 99-99
			// "SUCCESSFULLY CREATED %S (%d, %g, %d, %d, %d)"
			staFmt.Load(kstidXmlDebugMsg004);
#endif
			break;
		case SQL_PARAM_ERROR:
			// "SQL_PARAM_ERROR creating %S (%d, %g, %d, %d, %d)"
			staFmt.Load(kstidXmlErrorMsg100);
			break;
		case SQL_PARAM_UNUSED:
			// "SQL_PARAM_UNUSED creating %S (%d, %g, %d, %d, %d)"
			staFmt.Load(kstidXmlErrorMsg101);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "SQL_PARAM_DIAG_UNAVAILABLE INFO creating %S (%d, %g, %d, %d, %d)"
			staFmt.Load(kstidXmlErrorMsg099);
			break;
		}
		if (staFmt.Length())
		{
			int icls;
			m_pfwxd->MapCidToIndex(ood.m_vicls[ieSegStart + i], &icls);
			sta.Format(staFmt.Chars(),
				m_pfwxd->ClassName(icls).Chars(),
				ood.m_vhobj[ieSegStart + i],
				&ood.m_vguid[ieSegStart + i],
				ood.m_vhobjOwner[ieSegStart + i],
				ood.m_vfidOwner[ieSegStart + i],
				ood.m_vcpt[ieSegStart + i]);
			LogMessage(sta.Chars());
		}
	}
#ifdef LOG_SQL
	// "    %d Parameters processed, %d successful, %d attempted"
	staFmt.Load(kstidXmlInfoMsg001);
	StrAnsi staParam;
	if (cParamsProcessed == 1)
		staParam.Load(kstidParameter);
	else
		staParam.Load(kstidParameters);
	sta.Format(staFmt.Chars(), cParamsProcessed, staParam.Chars(), cSuccess, cobj);
	LogMessage(sta.Chars());
#endif
}


/*----------------------------------------------------------------------------------------------
	If fSingle is true, ensure that the database is in single user mode.  Otherwise ensure that
	it is in multi-user mode.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetSingleUserDb(bool fSingle)
{
	if (m_fSingle == fSingle){
		return;			// already in the right mode.
	}
	StrUni stuCmd;
	// TODO (SteveMiller): figure out what to do in Firebird.
	if (CURRENTDB == MSSQL) {
		if (fSingle)
		{
			stuCmd.Format(L"EXEC sp_dboption N'%s', 'single user', 'on'",
				m_pfwxd->DatabaseName().Chars());
		}
		else
		{
			// Put the database back in multiuser mode.
			stuCmd.Format(L"EXEC sp_dboption N'%s', 'single user', 'off'",
				m_pfwxd->DatabaseName().Chars());
		}
		ExecuteSimpleUnicodeSQL(stuCmd.Chars(), __LINE__);
		m_fSingle = fSingle;
	}

}


/*----------------------------------------------------------------------------------------------
	Return the class index for the given field index.

	@param ifld index into the field tables
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetClassIndexFromFieldIndex(int ifld)
{
	int icls = -1;
	int cid = m_pfwxd->FieldInfo(ifld).cid;
	if (!m_pfwxd->MapCidToIndex(cid, &icls))
	{
		// CRASH, BURN, EXPLODE!! -- "INTERNAL DATA CORRUPTION: unable to get class for field!"
		ThrowWithLogMessage(kstidXmlErrorMsg040);
	}
	return icls;
}


/*----------------------------------------------------------------------------------------------
	Write stored Unicode strings to the database for the given field.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreUnicodeData(int ifld)
{
	Assert((unsigned)ifld < (unsigned)m_vstda.Size());
	Assert(!m_vstda[ifld].m_vhobjDst.Size());
	Assert(!m_vstda[ifld].m_vvbFmt.Size());
	Assert(!m_vstda[ifld].m_vord.Size());
	int icls = GetClassIndexFromFieldIndex(ifld);
	int cpt = m_pfwxd->FieldInfo(ifld).cpt;
	int cstr = m_vstda[ifld].m_vhobj.Size();
	Assert(cpt == kcptUnicode || cpt == kcptBigUnicode);
	Assert(cstr == m_vstda[ifld].m_vstu.Size());
	if (!cstr)
		return;

	// Convert from multiple StrUni elements to an array of wchar[cchTxtLine].
	// Check for very large strings, and handle them separately.

	SQLINTEGER cchTxtMax;
	int cstrSizeOk = NormalizeUnicodeData(ifld, cchTxtMax);

	if (cstrSizeOk)
	{
		StoreSmallUnicodeData(ifld, icls, cpt, cstr, cstrSizeOk, cchTxtMax);
	}
	// Now, store the strings whose text was too large, one at a time.
	if (cstrSizeOk < cstr)
	{
		StoreLargeUnicodeData(ifld, icls, cpt, cstr);
	}

	// Clear these values to prepare for future use.

	m_vstda[ifld].m_vhobj.Clear();
	m_vstda[ifld].m_vstu.Clear();
}


/*----------------------------------------------------------------------------------------------
	Normalize the unicode data for the given field, returning the number of strings no longer
	than kcchTxtMaxBundle characters long, and setting the maximum length of those strings.

	@param ifld Index into m_vstda for the unicode data of interest
	@param cchTxtMax maximum length of the strings to store (<= kcchTxtMaxBundle)
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::NormalizeUnicodeData(int ifld, SQLINTEGER & cchTxtMax)
{
	cchTxtMax = 0;
	int cstrSizeOk = 0;
	int cstr = m_vstda[ifld].m_vhobj.Size();
	for (int ie = 0; ie < cstr; ++ie)
	{
		// Normalize each StrUni string to NFD.
		bool fT;
		fT = StrUtil::NormalizeStrUni(m_vstda[ifld].m_vstu[ie], UNORM_NFD);
		Assert(fT);

		SQLINTEGER cchTxt = m_vstda[ifld].m_vstu[ie].Length();
		if (cchTxt <= kcchTxtMaxBundle)
		{
			++cstrSizeOk;
			if (cchTxtMax < cchTxt)
				cchTxtMax = cchTxt;
		}
	}
	return cstrSizeOk;
}


/*----------------------------------------------------------------------------------------------
	Write stored Unicode strings to the database for the given field which are each no longer
	than kcchTxtMaxBundle characters.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param cpt specific data type, either kcptUnicode or kcptBigUnicode
	@param cstr number of strings of the given field type
	@param cstrSizeOk number of strings to store
	@param cchTxtMax maximum length of the strings to store (<= kcchTxtMaxBundle)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreSmallUnicodeData(int ifld, int icls, int cpt, int cstr,
	int cstrSizeOk, SQLINTEGER cchTxtMax)
{
	// Convert from multiple StrUni elements to an array of wchar[cchTxtLine]
	RawStringData rsd(cstrSizeOk, cchTxtMax);

	for (int ie = 0, istr = 0, i = 0; i < cstr; ++i)
	{
		SQLINTEGER cchTxt = m_vstda[ifld].m_vstu[i].Length();
		if (cchTxt <= kcchTxtMaxBundle)
		{
			rsd.vhobj[ie] = m_vstda[ifld].m_vhobj[i];
			rsd.vcbTxt[ie] = cchTxt * isizeof(wchar);
			memcpy(rsd.vchTxt.Begin() + istr, m_vstda[ifld].m_vstu[i].Chars(), rsd.vcbTxt[ie]);
			++ie;
			istr += rsd.cchTxtLine;
		}
	}

	// 1. Set the SQL_ATTR_PARAM_BIND_TYPE statement attribute to use column-wise binding.
	// 2. Specify the number of elements in each parameter array.
	// 3. Specify an array in which to return the status of each set of parameters.
	// 4. Specify an SQLUINTEGER value in which to return the number of sets of parameters
	//    processed.
	// 5. Bind the two input parameters (txt, id) to the appropriate arrays.
	// 6. Update the appropriate table, and check the results.

	SqlStatement sstmt;
	RETCODE rc;
	sstmt.Init(m_sdb);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
		reinterpret_cast<void *>(cstrSizeOk), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_STATUS_PTR, rsd.vnParamStatus.Begin(), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMS_PROCESSED_PTR, &rsd.cParamsProcessed, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	SQLSMALLINT nSqlType = cpt == kcptBigUnicode ? SQL_WLONGVARCHAR : SQL_WVARCHAR;
	rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR, nSqlType,
		rsd.cchTxtLine, 0, rsd.vchTxt.Begin(), rsd.cbTxtLine, rsd.vcbTxt.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		rsd.vhobj.Begin(), 0, rsd.vcbhobj.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

	// NOTE: m_staCmd may already have some accumulated SQL commands waiting to execute.
	StrAnsiBuf stabCmd;
	if(CURRENTDB == MSSQL) {
		stabCmd.Format("UPDATE \"%S\" SET \"%S\" = ? WHERE Id = ?;",
			m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars());
	}
	else if(CURRENTDB == FB) {
		stabCmd.Format("UPDATE %S SET \"%S\" = ? WHERE Id = ?;",
			m_pfwxd->ClassName(icls).Chars(), UpperName(m_pfwxd->FieldName(ifld)));
	}
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
	CheckUnicodeParamsForSuccess(rsd, icls, ifld);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing.

	@param rsd reference to the struct containing the parameter status data
	@param icls Index into m_pfwxd->m_vstucls for the class of the owning object.
	@param ifld Index into m_pfwxd->m_vstufld for the field of the unicode data
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CheckUnicodeParamsForSuccess(const RawStringData & rsd, int icls,
	int ifld)
{
	StrAnsi sta;
	StrAnsi staFmt;
	for (uint i = 0; i < rsd.cParamsProcessed; ++i)
	{
		staFmt.Clear();
		switch (rsd.vnParamStatus[i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			break;
		case SQL_PARAM_ERROR:
			// "ERROR in UPDATE [%S] SET [%S]=? WHERE [Id]=%d"
			staFmt.Load(kstidXmlErrorMsg025);
			break;
		case SQL_PARAM_UNUSED:
			// "UNUSED in UPDATE [%S] SET [%S]=? WHERE [Id]=%d"
			staFmt.Load(kstidXmlErrorMsg122);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "UNAVAIL INFO in UPDATE [%S] SET [%S]=? WHERE [Id]=%d"
			staFmt.Load(kstidXmlErrorMsg108);
			break;
		}
		if (staFmt.Length())
		{
			sta.Format(staFmt.Chars(),
				m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
				rsd.vhobj[i]);
			LogMessage(sta.Chars());
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Write stored Unicode strings to the database for the given field which are each longer
	than kcchTxtMaxBundle characters.  These are handled one unicode string at a time.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param cpt specific data type, either kcptUnicode or kcptBigUnicode
	@param cstr number of strings of the given field type
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreLargeUnicodeData(int ifld, int icls, int cpt, int cstr)
{
	RETCODE rc;
	for (int i = 0; i < cstr; ++i)
	{
		SQLINTEGER cchTxt = m_vstda[ifld].m_vstu[i].Length();
		if (cchTxt <= kcchTxtMaxBundle)
			continue;
		SQLINTEGER cbTxt =  cchTxt * isizeof(wchar);
		SQLINTEGER cbTxtLine = (cchTxt + 1) * isizeof(wchar);

		// 1. Bind the input parameter (txt) to the appropriate data.
		// 2. Update the appropriate table, and check the results.

		SqlStatement sstmt;
		sstmt.Init(m_sdb);
		wchar * pszTxt = const_cast<wchar *>(m_vstda[ifld].m_vstu[i].Chars());
		if (cpt == kcptBigUnicode)
		{
			rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR,
				SQL_WLONGVARCHAR, cchTxt+1, 0, pszTxt, cbTxtLine, &cbTxt);
		}
		else
		{
			// Protect against absurdly long text data.  Even though 4000 chars are
			// theoretically possible for kcptUnicode, the expected upper bound is a
			// couple of hundred chars, and anything approaching the theoretical limit
			// will almost certainly fail.
			// "Warning: Truncating string from %<0>d characters to %<1>d characters"
			// " for the %<2>S field of a %<3>S object.\n"
			StrAnsi staFmt(kstidXmlInfoMsg108);
			StrAnsi staMsg;
			staMsg.Format(staFmt.Chars(),
				cchTxt, kcchTxtMaxBundle, m_pfwxd->FieldName(ifld).Chars(),
				m_pfwxd->ClassName(icls).Chars());
			LogMessage(staMsg.Chars());
			m_vstda[ifld].m_vstu[i].Replace(kcchTxtMaxBundle, cchTxt, L"");
			cchTxt = m_vstda[ifld].m_vstu[i].Length();
			cbTxt =  cchTxt * isizeof(wchar);
			cbTxtLine = (cchTxt + 1) * isizeof(wchar);
			pszTxt = const_cast<wchar *>(m_vstda[ifld].m_vstu[i].Chars());
			rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR,
				SQL_WVARCHAR, cchTxt+1, 0, pszTxt, cbTxtLine, &cbTxt);
		}
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

		// NOTE: m_staCmd may already have some accumulated SQL commands waiting to execute.
		StrAnsiBuf stabCmd;
		if(CURRENTDB == MSSQL) {
			stabCmd.Format("UPDATE \"%S\" SET \"%S\" = ? WHERE Id = %d;",
				m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
				m_vstda[ifld].m_vhobj[i]);
		}
		else if(CURRENTDB == FB) {
			stabCmd.Format("UPDATE %S SET \"%S\" = ? WHERE Id = %d;",
				m_pfwxd->ClassName(icls).Chars(), UpperName(m_pfwxd->FieldName(ifld)),
				m_vstda[ifld].m_vhobj[i]);
		}
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
		sstmt.Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	Write stored String data to the database.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreStringData(int ifld)
{
	Assert((unsigned)ifld < (unsigned)m_vstda.Size());
	int cpt = m_pfwxd->FieldInfo(ifld).cpt;
	int cstr = m_vstda[ifld].m_vhobj.Size();
	Assert(cpt == kcptString || cpt == kcptBigString);
	Assert(cstr <= kcstrMax);
	Assert(cstr == m_vstda[ifld].m_vstu.Size());
	Assert(cstr == m_vstda[ifld].m_vvbFmt.Size());
	Assert(!m_vstda[ifld].m_vhobjDst.Size());
	Assert(!m_vstda[ifld].m_vord.Size());
	if (!cstr)
		return;
	int icls = GetClassIndexFromFieldIndex(ifld);

	// Note that embedding pictures inside the format data can lead to extremely large
	// formatting arrays, and also we don't have any a priori limits on the size or number of
	// strings.  Therefore, we check for very large strings and very large formats, and handle
	// them separately.
	SQLINTEGER cchTxtMax;
	SQLINTEGER cbFmtMax;
	int cstrSizeOk = NormalizeStringData(ifld, cchTxtMax, cbFmtMax);
	if (cstrSizeOk)
	{
		StoreSmallStringData(ifld, icls, cpt, cstr, cstrSizeOk, cchTxtMax, cbFmtMax);
	}
	// Now, store the strings whose text or formatting data were too large, one at a time.
	if (cstrSizeOk < cstr)
	{
		StoreLargeStringData(ifld, icls, cpt, cstr);
	}

	// Clear these values to prepare for future use.

	m_vstda[ifld].m_vhobj.Clear();
	m_vstda[ifld].m_vstu.Clear();
	m_vstda[ifld].m_vvbFmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Normalize the unicode for the string data of the given field, returning the number of
	strings no longer than kcchTxtMaxBundle characters long with formatting no longer than
	kcbFmtMaxBundle bytes long, and setting the maximum length of those strings and maximum
	format size.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param cchTxtMax maximum length of the strings to store (<= kcchTxtMaxBundle)
	@param cbFmtMax maximum length of the formats of the strings to store (<= kcbFmtMaxBundle)
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::NormalizeStringData(int ifld, SQLINTEGER & cchTxtMax,
	SQLINTEGER & cbFmtMax)
{
	cchTxtMax = 0;
	cbFmtMax = 0;
	int cstrSizeOk = 0;
	if (!m_qtsf)
		m_qtsf.CreateInstance(CLSID_TsStrFactory);
	int cstr = m_vstda[ifld].m_vhobj.Size();
	for (int i = 0; i < cstr; ++i)
	{
		// Normalize the string to NFD.
		NormalizeOneString(m_vstda[ifld].m_vstu[i], m_vstda[ifld].m_vvbFmt[i]);

		SQLINTEGER cchTxt = m_vstda[ifld].m_vstu[i].Length();
		SQLINTEGER cbFmt = m_vstda[ifld].m_vvbFmt[i].Size();
		if (cchTxt <= kcchTxtMaxBundle && cbFmt <= kcbFmtMaxBundle)
		{
			++cstrSizeOk;
			if (cchTxtMax < cchTxt)
				cchTxtMax = cchTxt;
			if (cbFmtMax < cbFmt)
				cbFmtMax = cbFmt;
		}
	}
	return cstrSizeOk;
}


/*----------------------------------------------------------------------------------------------
	Normalize the TsString represented by its character and formatting data to NFD

	@param stu Reference to the character data.
	@param vbFmt Reference to the formatting data.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::NormalizeOneString(StrUni & stu, Vector<byte> & vbFmt)
{
	// 1. create the TsString object
	// 2. normalize it
	// 3. reserialize the result to this data.
	ITsStringPtr qtss;
	ITsStringPtr qtss1;
#ifdef DEBUG
	StrUni stuNFD = stu;
	StrUtil::NormalizeStrUni(stuNFD, UNORM_NFD);
#endif
	int cch = stu.Length();
	int cb = vbFmt.Size();
	CheckHr(m_qtsf->DeserializeStringRgch(stu.Chars(), &cch, vbFmt.Begin(), &cb, &qtss));

	CheckHr(qtss->get_NormalizedForm(knmNFD, &qtss1));

	SmartBstr sbstr;
	CheckHr(qtss1->get_Text(&sbstr));
	stu = sbstr.Chars();
#ifdef DEBUG
	Assert(stuNFD == stu);
#endif

	Vector<byte> vbFmt1;
	int cbMax = 2 * vbFmt.Size();
	vbFmt1.Resize(cbMax);
	int cbNeed;
	CheckHr(qtss1->SerializeFmtRgb(vbFmt1.Begin(), cbMax, &cbNeed));
	if (cbNeed > cbMax)
	{
		vbFmt1.Resize(cbNeed);
		CheckHr(qtss1->SerializeFmtRgb(vbFmt1.Begin(), cbNeed, &cbNeed));
	}
	else if (cbNeed < cbMax)
	{
		vbFmt1.Resize(cbNeed);
	}
	vbFmt = vbFmt1;
}

/*----------------------------------------------------------------------------------------------
	Write stored TsStrings to the database for the given field which are each no longer
	than kcchTxtMaxBundle characters with no more than kcbFmtMaxBundle bytes of formatting.
		1. Convert from multiple StrUni elements to an array of wchar[cchTxtLine], and from
		   multiple Vector<byte> elements to an array of byte[cbFmtMax].
		2. Set the SQL_ATTR_PARAM_BIND_TYPE statement attribute to use column-wise binding.
		3. Specify the number of elements in each parameter array.
		4. Specify an array in which to return the status of each set of parameters.
		5. Specify an SQLUINTEGER value in which to return the number of sets of parameters
		   processed.
		6. Bind the three input parameters (txt, fmt, id) to the appropriate arrays.
		7. Update the appropriate table, and check the results.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param cpt specific data type, either kcptString or kcptBigString
	@param cstr number of strings of the given field type
	@param cstrSizeOk number of strings to store
	@param cchTxtMax maximum length of the strings to store (<= kcchTxtMaxBundle)
	@param cbFmtMax maximum length of the formats to store (<= kcbFmtMaxBundle)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreSmallStringData(int ifld, int icls, int cpt, int cstr,
	int cstrSizeOk, SQLINTEGER cchTxtMax, SQLINTEGER cbFmtMax)
{
	RawStringData rsd(cstrSizeOk, cchTxtMax, cbFmtMax);

	for (int ie = 0, ifmt = 0, istr = 0, i = 0; i < cstr; ++i)
	{
		SQLINTEGER cchTxt = m_vstda[ifld].m_vstu[i].Length();
		SQLINTEGER cbFmt = m_vstda[ifld].m_vvbFmt[i].Size();
		if (cchTxt <= kcchTxtMaxBundle && cbFmt <= kcbFmtMaxBundle)
		{
			rsd.vhobj[ie] = m_vstda[ifld].m_vhobj[i];
			rsd.vcbTxt[ie] = cchTxt * isizeof(wchar);
			rsd.vcbFmt[ie] = cbFmt;
			memcpy(rsd.vchTxt.Begin() + istr, m_vstda[ifld].m_vstu[i].Chars(), rsd.vcbTxt[ie]);
			memcpy(rsd.vbFmt.Begin() + ifmt, m_vstda[ifld].m_vvbFmt[i].Begin(), cbFmt);
			++ie;
			istr += rsd.cchTxtLine;
			ifmt += cbFmtMax;
		}
	}
	RETCODE rc;
	SqlStatement sstmt;
	sstmt.Init(m_sdb);

	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
		reinterpret_cast<void *>(cstrSizeOk), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_STATUS_PTR, rsd.vnParamStatus.Begin(), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMS_PROCESSED_PTR, &rsd.cParamsProcessed, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

	SQLSMALLINT nSqlTxtType = cpt == kcptBigString ? SQL_WLONGVARCHAR : SQL_WVARCHAR;
	SQLSMALLINT nSqlFmtType = cpt == kcptBigString ? SQL_LONGVARBINARY : SQL_VARBINARY;
	rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR, nSqlTxtType,
		rsd.cchTxtLine, 0, rsd.vchTxt.Begin(), rsd.cbTxtLine, rsd.vcbTxt.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_BINARY, nSqlFmtType,
		cbFmtMax, 0, rsd.vbFmt.Begin(), cbFmtMax, rsd.vcbFmt.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 3, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		rsd.vhobj.Begin(), 0, rsd.vcbhobj.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

	// NOTE: m_staCmd may already have some accumulated SQL commands waiting to execute.
	StrAnsiBuf stabCmd;
	if(CURRENTDB == MSSQL) {
		stabCmd.Format("UPDATE \"%S\" SET \"%S\" = ?,%S_Fmt=? WHERE Id = ?;",
			m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
			m_pfwxd->FieldName(ifld).Chars());
	}
	else if(CURRENTDB == FB) {
		stabCmd.Format("UPDATE %S SET \"%S\" = ?, \"%S_FMT\" = ? WHERE Id = ?;",
			m_pfwxd->ClassName(icls).Chars(), UpperName(m_pfwxd->FieldName(ifld)),
			UpperName(m_pfwxd->FieldName(ifld)));
	}
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
	CheckStringParamsForSuccess(rsd, icls, ifld);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing.

	@param rsd reference to the struct containing the parameter status data
	@param icls Index into m_pfwxd->m_vstucls for the class of the owning object.
	@param ifld Index into m_pfwxd->m_vstufld for the field of the unicode data
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CheckStringParamsForSuccess(const RawStringData & rsd, int icls, int ifld)
{
	StrAnsi sta;
	StrAnsi staFmt;
	for (uint i = 0; i < rsd.cParamsProcessed; ++i)
	{
		staFmt.Clear();
		switch (rsd.vnParamStatus[i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			break;
		case SQL_PARAM_ERROR:
			// "ERROR in UPDATE [%S] SET [%S]=?,%S_Fmt=? WHERE [Id]=%d"
			staFmt.Load(kstidXmlErrorMsg026);
			break;
		case SQL_PARAM_UNUSED:
			// "UNUSED in UPDATE [%S] SET [%S]=?,%S_Fmt=? WHERE [Id]=%d"
			staFmt.Load(kstidXmlErrorMsg122);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "UNAVAIL INFO in UPDATE [%S] SET [%S]=?,%S_Fmt=? WHERE [Id]=%d"
			staFmt.Load(kstidXmlErrorMsg108);
			break;
		}
		if (staFmt.Length())
		{
			sta.Format(staFmt.Chars(),
				m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
				m_pfwxd->FieldName(ifld).Chars(), rsd.vhobj[i]);
			LogMessage(sta.Chars());
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Write stored TsStrings to the database for the given field which are each longer
	than kcchTxtMaxBundle characters, or have formatting longer than kcbFmtMaxBundle bytes.
	These are handled one TsString at a time.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param cpt specific data type, either kcptUnicode or kcptBigUnicode
	@param cstr number of strings of the given field type
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreLargeStringData(int ifld, int icls, int cpt, int cstr)
{
	RETCODE rc;
	for (int i = 0; i < cstr; ++i)
	{
		SQLINTEGER cchTxt = m_vstda[ifld].m_vstu[i].Length();
		SQLINTEGER cbFmt = m_vstda[ifld].m_vvbFmt[i].Size();
		if (cchTxt <= kcchTxtMaxBundle && cbFmt <= kcbFmtMaxBundle)
			continue;
		SQLINTEGER cbTxt =  cchTxt * isizeof(wchar);
		SQLINTEGER cbTxtLine = (cchTxt + 1) * isizeof(wchar);

		// 1. Bind the two input parameters (txt, fmt) to the appropriate data.
		// 2. Update the appropriate table, and check the results.

		SqlStatement sstmt;
		sstmt.Init(m_sdb);
		wchar * pszTxt = const_cast<wchar *>(m_vstda[ifld].m_vstu[i].Chars());
		SQLSMALLINT nSqlTxtType = cpt == kcptBigString ? SQL_WLONGVARCHAR : SQL_WVARCHAR;
		SQLSMALLINT nSqlFmtType = cpt == kcptBigString ? SQL_LONGVARBINARY : SQL_VARBINARY;
		rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR, nSqlTxtType,
			cchTxt+1, 0, pszTxt, cbTxtLine, &cbTxt);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_BINARY, nSqlFmtType,
			cbFmt, 0, m_vstda[ifld].m_vvbFmt[i].Begin(), cbFmt, &cbFmt);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

		// NOTE: m_staCmd may already have some accumulated SQL commands waiting to execute.
		StrAnsiBuf stabCmd;
		if(CURRENTDB == MSSQL) {
			stabCmd.Format("UPDATE \"%S\" SET \"%S\" = ?,%S_Fmt=? WHERE Id = %d;",
				m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
				m_pfwxd->FieldName(ifld).Chars(), m_vstda[ifld].m_vhobj[i]);
		}
		else if(CURRENTDB == FB) {
			stabCmd.Format("UPDATE %S SET \"%S\" = ?, \"%S_FMT\" = ? WHERE Id = %d;",
				m_pfwxd->ClassName(icls).Chars(), UpperName(m_pfwxd->FieldName(ifld)),
				UpperName(m_pfwxd->FieldName(ifld)), m_vstda[ifld].m_vhobj[i]);
		}
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
		sstmt.Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	Write any stored MultiUnicode data to the database.  This uses the SQL stored procedure
	SetMultiTxt$.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreMultiUnicode()
{
	int cstr = m_mtd.m_vhobj.Size();
	Assert(cstr <= kcstrMax);
	Assert(cstr == m_mtd.m_vfid.Size());
	Assert(cstr == m_mtd.m_vws.Size());
	Assert(cstr == m_mtd.m_vstu.Size());
	if (!cstr)
		return;

	// Check for very large strings, and handle them separately.
	SQLINTEGER cchTxtMax;
	int cstrSizeOk = NormalizeMultiUnicodeData(cchTxtMax);
	if (cstrSizeOk)
	{
		StoreSmallMultiUnicode(cstr, cstrSizeOk, cchTxtMax);
	}
	// Now, store the strings whose text was too large, one at a time.
	if (cstrSizeOk < cstr)
	{
		StoreLargeMultiUnicode(cstr);
	}

	// Clear these values to prepare for future use.

	m_mtd.m_vfid.Clear();
	m_mtd.m_vhobj.Clear();
	m_mtd.m_vws.Clear();
	m_mtd.m_vstu.Clear();
}


/*----------------------------------------------------------------------------------------------
	Normalize the unicode data for all MultiUnicode fields, returning the number of strings no
	longer than kcchTxtMaxBundle characters long, and setting the maximum length of those
	strings.

	@param cchTxtMax maximum length of the strings to store (<= kcchTxtMaxBundle)
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::NormalizeMultiUnicodeData(SQLINTEGER & cchTxtMax)
{
	cchTxtMax = 0;
	int cstrSizeOk = 0;
	int cstr = m_mtd.m_vhobj.Size();
	for (int i = 0; i < cstr; ++i)
	{
		// Normalize each StrUni string to NFD.
		bool fT;
		fT = StrUtil::NormalizeStrUni(m_mtd.m_vstu[i], UNORM_NFD);
		Assert(fT);

		SQLINTEGER cchTxt = m_mtd.m_vstu[i].Length();
		if (cchTxt <= kcchTxtMaxBundle)
		{
			++cstrSizeOk;
			if (cchTxtMax < cchTxt)
				cchTxtMax = cchTxt;
		}
	}
	return cstrSizeOk;
}

/*----------------------------------------------------------------------------------------------
	Write the stored multilingual MultiUnicode strings to the database which are each no
	longer than kcchTxtMaxBundle characters.

	@param cstr number of strings of the given field type
	@param cstrSizeOk number of strings to store
	@param cchTxtMax maximum length of the strings to store (<= kcchTxtMaxBundle)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreSmallMultiUnicode(int cstr, int cstrSizeOk, SQLINTEGER cchTxtMax)
{
	RawStringData rsd(cstrSizeOk, cchTxtMax, 0, true); //poo
	// convert from Vector<StrUni> to an array of wchar[cchTxtLine]
	for (int ie = 0, istr = 0, i = 0; i < cstr; ++i)
	{
		SQLINTEGER cchTxt = m_mtd.m_vstu[i].Length();
		if (cchTxt <= kcchTxtMaxBundle)
		{
			rsd.vfid[ie] = m_mtd.m_vfid[i];
			rsd.vhobj[ie] = m_mtd.m_vhobj[i];
			rsd.vws[ie] = m_mtd.m_vws[i];
			rsd.vcbTxt[ie] = cchTxt * isizeof(wchar);
			memcpy(rsd.vchTxt.Begin() + istr, m_mtd.m_vstu[i].Chars(), rsd.vcbTxt[ie]);
			++ie;
			istr += rsd.cchTxtLine;
		}
	}
	// 1. Set the SQL_ATTR_PARAM_BIND_TYPE statement attribute to use column-wise binding.
	// 2. Specify the number of elements in each parameter array.
	// 3. Specify an array in which to return the status of each set of parameters.
	// 4. Specify an SQLUINTEGER value in which to return the number of sets of parameters
	//    processed.
	// 5. Bind the four input parameters (flid, obj, ws, txt) to the appropriate arrays.
	// 6. Execute the stored procedure SetMultiTxt$, and check the results.
	RETCODE rc;
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	const static SQLCHAR * pszCmd;
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	if(CURRENTDB == MSSQL){
		pszCmd = (const SQLCHAR *) "EXEC SetMultiTxt$ ?,?,?,?;";
	}
	else if(CURRENTDB == FB){
		pszCmd = (const SQLCHAR *) "EXECUTE PROCEDURE SetMultiTxt$ (?,?,?,?);";
	}
		rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
			reinterpret_cast<void *>(cstrSizeOk), 0);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_STATUS_PTR, rsd.vnParamStatus.Begin(), 0);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMS_PROCESSED_PTR, &rsd.cParamsProcessed, 0);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
			rsd.vfid.Begin(), 0, rsd.vcbfid.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
			rsd.vhobj.Begin(), 0, rsd.vcbhobj.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindParameter(sstmt.Hstmt(), 3, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
			rsd.vws.Begin(), 0, rsd.vcbws.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindParameter(sstmt.Hstmt(), 4, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
			rsd.cchTxtLine, 0, rsd.vchTxt.Begin(), rsd.cbTxtLine, rsd.vcbTxt.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

		/*for(int i=0;i<cstrSizeOk;++i){
			fprintf(stdout,"flid=%d\n",rsd.vfid[i]);
			fprintf(stdout,"objid=%d\n",rsd.vhobj[i]);
			fprintf(stdout,"ws=%d\n",rsd.vws[i]);
			fprintf(stdout,"txt=");
			for(int j=0; j<m_mtd.m_vstu[i].Length(); ++j)
				 fprintf(stdout,"%c",m_mtd.m_vstu[i][j]);
			fprintf(stdout,"\n\n");

		}*/

		rc = SQLExecDirectA(sstmt.Hstmt(), const_cast<SQLCHAR *>(pszCmd), SQL_NTS);
		CheckMultiUnicodeParamsForSuccess(rsd);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, reinterpret_cast<const char *>(pszCmd));
		sstmt.Clear();

	/*else if(CURRENTDB == FB){
		pszCmd = (const SQLCHAR *) "EXECUTE PROCEDURE SetMultiTxt$ (?,?,?,?);";
		rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
			reinterpret_cast<void *>(1), 0);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_STATUS_PTR, rsd.vnParamStatus.Begin(), 0);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMS_PROCESSED_PTR, &rsd.cParamsProcessed, 0);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

		for(int i = 0; i < cstrSizeOk; ++i)
		{
			rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
			rsd.vfid.Begin() + i, 0, rsd.vcbfid.Begin());
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
				rsd.vhobj.Begin() + i, 0, rsd.vcbhobj.Begin());
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			rc = SQLBindParameter(sstmt.Hstmt(), 3, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
				rsd.vws.Begin() + i, 0, rsd.vcbws.Begin());
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			rc = SQLBindParameter(sstmt.Hstmt(), 4, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
				rsd.cchTxtLine, 0, rsd.vchTxt.Begin() + i, rsd.cbTxtLine, rsd.vcbTxt.Begin());
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

			  fprintf(stdout,"flid=%d\n",rsd.vfid[i]);
			  fprintf(stdout,"objid=%d\n",rsd.vhobj[i]);
			  fprintf(stdout,"ws=%d\n",rsd.vws[i]);
			  /*for(int i=0;i<rsd.vchTxt.Size();i++)
				 fprintf(stdout,"Txt=%c\n",rsd.vchTxt[i]);

			rc = SQLExecDirectA(sstmt.Hstmt(), const_cast<SQLCHAR *>(pszCmd), SQL_NTS);
			CheckMultiUnicodeParamsForSuccess(rsd);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, reinterpret_cast<const char *>(pszCmd));
			sstmt.Clear();
		}
	}*/
}


/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing.

	@param rsd reference to the struct containing the parameter status data
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CheckMultiUnicodeParamsForSuccess(const RawStringData & rsd)
{
	StrAnsi sta;
	StrAnsi staFmt;
	for (uint i = 0; i < rsd.cParamsProcessed; ++i)
	{
		staFmt.Clear();
		switch (rsd.vnParamStatus[i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			break;
		case SQL_PARAM_ERROR:
			// "ERROR in SetMultiTxt$ %d,%d,%d,'...'"
			staFmt.Load(kstidXmlErrorMsg024);
			break;
		case SQL_PARAM_UNUSED:
			// "UNUSED in SetMultiTxt$ %d,%d,%d,'...'"
			staFmt.Load(kstidXmlErrorMsg120);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "UNAVAIL INFO in SetMultiTxt$ %d,%d,%d,'...'"
			staFmt.Load(kstidXmlErrorMsg106);
			break;
		}
		if (staFmt.Length())
		{
			sta.Format(staFmt.Chars(), rsd.vfid[i], rsd.vhobj[i], rsd.vws[i]);
			LogMessage(sta.Chars());
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Write the stored multilingual Unicode strings to the database which are longer than
	kcchTxtMaxBundle characters.  These are handled one MultiUnicode string at a time.

	@param cstr number of strings of the given field type
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreLargeMultiUnicode(int cstr)
{
	for (int i = 0; i < cstr; ++i)
	{
		SQLINTEGER cchTxt = m_mtd.m_vstu[i].Length();
		if (cchTxt <= kcchTxtMaxBundle)
			continue;
		SQLINTEGER cbTxt = cchTxt * isizeof(wchar);
		SQLINTEGER cbTxtLine = (cchTxt + 1) * isizeof(wchar);

		// 1. Bind the input parameter (txt) to the appropriate data.
		// 2. Execute the stored procedure SetMultiBigStr$, and check the results.

		SqlStatement sstmt;
		sstmt.Init(m_sdb);
		RETCODE rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR,
			SQL_WLONGVARCHAR, cchTxt+1, 0, const_cast<wchar *>(m_mtd.m_vstu[i].Chars()),
			cbTxtLine, &cbTxt);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

		// NOTE: m_staCmd may already have some accumulated SQL commands waiting to execute.
		StrAnsiBuf stabCmd;
		if(CURRENTDB == FB) {
			stabCmd.Format("EXECUTE PROCEDURE SetMultiTxt$ (%d,%d,%d,?);",
				m_mtd.m_vfid[i], m_mtd.m_vhobj[i], m_mtd.m_vws[i]);
		}
		if(CURRENTDB == MSSQL) {
			stabCmd.Format("EXEC SetMultiTxt$ %d,%d,%d,?;",
				m_mtd.m_vfid[i], m_mtd.m_vhobj[i], m_mtd.m_vws[i]);
		}
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
		sstmt.Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	Write any stored MultiBigUnicode data to the database.  This uses the SQL stored procedure
	SetMultiBigTxt$.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreMultiBigUnicode()
{
	int cstr = m_mtdBig.m_vhobj.Size();
	Assert(cstr <= kcstrBigMax);
	Assert(cstr == m_mtdBig.m_vfid.Size());
	Assert(cstr == m_mtdBig.m_vws.Size());
	Assert(cstr == m_mtdBig.m_vstu.Size());
	if (!cstr)
		return;

	// Check for very large strings, and handle them separately.
	SQLINTEGER cchTxtMax;
	int cstrSizeOk = NormalizeMultiBigUnicodeData(cchTxtMax);
	if (cstrSizeOk)
	{
		StoreSmallMultiBigUnicode(cstr, cstrSizeOk, cchTxtMax);
	}

	// Now, store the strings whose text was too large, one at a time.
	if (cstrSizeOk < cstr)
	{
		StoreLargeMultiBigUnicode(cstr);
	}

	// Clear these values to prepare for future use.

	m_mtdBig.m_vfid.Clear();
	m_mtdBig.m_vhobj.Clear();
	m_mtdBig.m_vws.Clear();
	m_mtdBig.m_vstu.Clear();
}


/*----------------------------------------------------------------------------------------------
	Normalize the unicode data for all MultiBigUnicode fields, returning the number of strings
	no longer than kcchTxtMaxBundle characters long, and setting the maximum length of those
	strings.

	@param cchTxtMax maximum length of the strings to store (<= kcchTxtMaxBundle)
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::NormalizeMultiBigUnicodeData(SQLINTEGER & cchTxtMax)
{
	cchTxtMax = 0;
	int cstrSizeOk = 0;
	int cstr = m_mtdBig.m_vhobj.Size();
	for (int i = 0; i < cstr; ++i)
	{
		// Normalize each StrUni string to NFD.
		bool fT;
		fT = StrUtil::NormalizeStrUni(m_mtdBig.m_vstu[i], UNORM_NFD);
		Assert(fT);

		SQLINTEGER cchTxt = m_mtdBig.m_vstu[i].Length();
		if (cchTxt <= kcchTxtMaxBundle)
		{
			++cstrSizeOk;
			if (cchTxtMax < cchTxt)
				cchTxtMax = cchTxt;
		}
	}
	return cstrSizeOk;
}


/*----------------------------------------------------------------------------------------------
	Write the stored multilingual MultiBigUnicode strings to the database which are each no
	longer than kcchTxtMaxBundle characters.

	@param cstr number of strings of the given field type
	@param cstrSizeOk number of strings to store
	@param cchTxtMax maximum length of the strings to store (<= kcchTxtMaxBundle)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreSmallMultiBigUnicode(int cstr, int cstrSizeOk, SQLINTEGER cchTxtMax)
{
	RawStringData rsd(cstrSizeOk, cchTxtMax, 0, true);

	// convert from Vector<StrUni> to an array of wchar[cchTxtLine]
	for (int ie = 0, istr = 0, i = 0; i < cstr; ++i)
	{
		SQLINTEGER cchTxt = m_mtdBig.m_vstu[i].Length();
		if (cchTxt <= kcchTxtMaxBundle)
		{
			rsd.vfid[ie] = m_mtdBig.m_vfid[i];
			rsd.vhobj[ie] = m_mtdBig.m_vhobj[i];
			rsd.vws[ie] = m_mtdBig.m_vws[i];
			rsd.vcbTxt[ie] = cchTxt * isizeof(wchar);
			memcpy(rsd.vchTxt.Begin() + istr, m_mtdBig.m_vstu[i].Chars(), rsd.vcbTxt[ie]);
			++ie;
			istr += rsd.cchTxtLine;
		}
	}
	// 1. Set the SQL_ATTR_PARAM_BIND_TYPE statement attribute to use column-wise binding.
	// 2. Specify the number of elements in each parameter array.
	// 3. Specify an array in which to return the status of each set of parameters.
	// 4. Specify an SQLUINTEGER value in which to return the number of sets of parameters
	//    processed.
	// 5. Bind the four input parameters (flid, obj, ws, txt) to the appropriate arrays.
	// 6. Execute the stored procedure SetMultiBigTxt$, and check the results.
	RETCODE rc;
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
		reinterpret_cast<void *>(cstrSizeOk), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_STATUS_PTR, rsd.vnParamStatus.Begin(), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMS_PROCESSED_PTR, &rsd.cParamsProcessed, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		rsd.vfid.Begin(), 0, rsd.vcbfid.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		rsd.vhobj.Begin(), 0, rsd.vcbhobj.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 3, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		rsd.vws.Begin(), 0, rsd.vcbws.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 4, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WLONGVARCHAR,
		rsd.cchTxtLine, 0, rsd.vchTxt.Begin(), rsd.cbTxtLine, rsd.vcbTxt.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
#undef GOODUNICODEONLY
#ifdef GOODUNICODEONLY
	static SQLCHAR * pszCmd;
	if(CURRENTDB == FB)
		pszCmd = (SQLCHAR *) "EXECUTE PROCEDURE SetMultiBigTxt$ (?,?,?,?);";
	if(CURRENTDB == MSSQL)
		pszCmd = (SQLCHAR *) "EXEC SetMultiBigTxt$ ?,?,?,?";
#else
	// SEE THE COMMENTS IN StoreMultiBigString().
	static SQLCHAR * pszCmd;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		pszCmd = (SQLCHAR *) "INSERT INTO MultiBigTxt$ (Flid,Obj,Ws,Txt) VALUES(?,?,?,?);";
	}
#endif
	rc = SQLExecDirectA(sstmt.Hstmt(), pszCmd, SQL_NTS);
	CheckMultiBigUnicodeParamsForSuccess(rsd);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, reinterpret_cast<const char *>(pszCmd));
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing.

	@param rsd reference to the struct containing the parameter status data
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CheckMultiBigUnicodeParamsForSuccess(const RawStringData & rsd)
{
	StrAnsi sta;
	StrAnsi staFmt;
	for (uint i = 0; i < rsd.cParamsProcessed; ++i)
	{
		staFmt.Clear();
		switch (rsd.vnParamStatus[i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			break;
		case SQL_PARAM_ERROR:
			// "ERROR in SetMultiBigTxt$ %d,%d,%d,'...'"
			staFmt.Load(kstidXmlErrorMsg022);
			break;
		case SQL_PARAM_UNUSED:
			// "UNUSED in SetMultiBigTxt$ %d,%d,%d,'...'"
			staFmt.Load(kstidXmlErrorMsg118);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "UNAVAIL INFO in SetMultiBigTxt$ %d,%d,%d,'...'"
			staFmt.Load(kstidXmlErrorMsg104);
			break;
		}
		if (staFmt.Length())
		{
			sta.Format(staFmt.Chars(), rsd.vfid[i], rsd.vhobj[i], rsd.vws[i]);
			LogMessage(sta.Chars());
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Write the stored multilingual MultiBigUnicode strings to the database which are longer than
	kcchTxtMaxBundle characters.  These are handled one MultiBigUnicode string at a time.

	@param cstr number of strings of the given field type
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreLargeMultiBigUnicode(int cstr)
{
	for (int i = 0; i < cstr; ++i)
	{
		SQLINTEGER cchTxt = m_mtdBig.m_vstu[i].Length();
		if (cchTxt <= kcchTxtMaxBundle)
			continue;
		SQLINTEGER cbTxt = cchTxt * isizeof(wchar);
		SQLINTEGER cbTxtLine = (cchTxt + 1) * isizeof(wchar);

		// 1. Bind the input parameter (txt) to the appropriate data.
		// 2. Execute the stored procedure SetMultiBigStr$, and check the results.

		SqlStatement sstmt;
		sstmt.Init(m_sdb);
		RETCODE rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR,
			SQL_WLONGVARCHAR, cchTxt+1, 0, const_cast<wchar *>(m_mtdBig.m_vstu[i].Chars()),
			cbTxtLine, &cbTxt);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

		// NOTE: m_staCmd may already have some accumulated SQL commands waiting to execute.
		StrAnsiBuf stabCmd;
#ifdef GOODUNICODEONLY
		if(CURRENTDB == FB) {
			stabCmd.Format("EXECUTE PROCEDURE SetMultiBigTxt$ (%d,%d,%d,?);",
				m_mtdBig.m_vfid[i], m_mtdBig.m_vhobj[i], m_mtdBig.m_vws[i]);
		}
		if(CURRENTDB == MSSQL) {
			stabCmd.Format("EXEC SetMultiBigTxt$ %d,%d,%d,?",
				m_mtdBig.m_vfid[i], m_mtdBig.m_vhobj[i], m_mtdBig.m_vws[i]);
		}
#else
		// SEE THE COMMENTS IN StoreMultiBigString().
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stabCmd.Format("INSERT INTO MultiBigTxt$ (Flid,Obj,Ws,Txt) "
				"VALUES(%d,%d,%d,?);",
				m_mtdBig.m_vfid[i], m_mtdBig.m_vhobj[i], m_mtdBig.m_vws[i]);
		}
#endif
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
		sstmt.Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	Write any stored MultiString data to the database.  This uses the SQL stored procedure
	SetMultiStr$.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreMultiString()
{
	int cstr = m_msd.m_vhobj.Size();
	Assert(cstr <= kcstrMax);
	Assert(cstr == m_msd.m_vfid.Size());
	Assert(cstr == m_msd.m_vws.Size());
	Assert(cstr == m_msd.m_vstu.Size());
	Assert(cstr == m_msd.m_vvbFmt.Size());
	if (!cstr)
		return;

	// Convert from Vector<StrUni> to an array of wchar[cchTxtLine], and from
	// Vector<Vector<byte> > to an array of byte[cbFmtMax].
	// Check for very large strings and very large formats, and handle them separately.
	SQLINTEGER cchTxtMax;
	SQLINTEGER cbFmtMax;
	int cstrSizeOk = NormalizeMultiStringData(cchTxtMax, cbFmtMax);

	if (cstrSizeOk)
	{
		StoreSmallMultiStringData(cstr, cstrSizeOk, cchTxtMax, cbFmtMax);
	}
	// Now, store the strings whose text or formatting data were too large, one at a time.
	if (cstrSizeOk < cstr)
	{
		StoreLargeMultiStringData(cstr);
	}

	// Clear these values to prepare for future use.

	m_msd.m_vfid.Clear();
	m_msd.m_vhobj.Clear();
	m_msd.m_vws.Clear();
	m_msd.m_vstu.Clear();
	m_msd.m_vvbFmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Normalize the unicode for the MultiString data, returning the number of MultiStrings no
	longer than kcchTxtMaxBundle characters long with formatting no longer than kcbFmtMaxBundle
	bytes long, and setting the maximum length of those strings and maximum format size.

	@param cchTxtMax maximum length of the strings to store (<= kcchTxtMaxBundle)
	@param cbFmtMax maximum length of the formats of the strings to store (<= kcbFmtMaxBundle)
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::NormalizeMultiStringData(SQLINTEGER & cchTxtMax, SQLINTEGER & cbFmtMax)
{
	cchTxtMax = 0;
	cbFmtMax = 0;
	int cstrSizeOk = 0;
	if (!m_qtsf)
		m_qtsf.CreateInstance(CLSID_TsStrFactory);
	int cstr = m_msd.m_vhobj.Size();
	for (int i = 0; i < cstr; ++i)
	{
		// Normalize the string to NFD.
		NormalizeOneString(m_msd.m_vstu[i], m_msd.m_vvbFmt[i]);

		SQLINTEGER cchTxt = m_msd.m_vstu[i].Length();
		SQLINTEGER cbFmt = m_msd.m_vvbFmt[i].Size();
		if (cchTxt <= kcchTxtMaxBundle && cbFmt <= kcbFmtMaxBundle)
		{
			++cstrSizeOk;
			if (cchTxtMax < cchTxt)
				cchTxtMax = cchTxt;
			if (cbFmtMax < cbFmt)
				cbFmtMax = cbFmt;
		}
	}
	return cstrSizeOk;
}


/*----------------------------------------------------------------------------------------------
	Write the stored multilingual TsStrings to the database which are each no longer than
	kcchTxtMaxBundle characters with no more than kcbFmtMaxBundle bytes of formatting.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param cpt specific data type, either kcptString or kcptBigString
	@param cstr number of strings of the given field type
	@param cstrSizeOk number of strings to store
	@param cchTxtMax maximum length of the strings to store (<= kcchTxtMaxBundle)
	@param cbFmtMax maximum length of the formats to store (<= kcbFmtMaxBundle)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreSmallMultiStringData(int cstr, int cstrSizeOk, SQLINTEGER cchTxtMax,
	SQLINTEGER cbFmtMax)
{
	RawStringData rsd(cstrSizeOk, cchTxtMax, cbFmtMax, true);

	// Convert from multiple StrUni elements to an array of wchar[cchTxtLine], and from

	for (int ie = 0, ifmt = 0, istr = 0, i = 0; i < cstr; ++i)
	{
		SQLINTEGER cchTxt = m_msd.m_vstu[i].Length();
		SQLINTEGER cbFmt = m_msd.m_vvbFmt[i].Size();
		if (cchTxt <= kcchTxtMaxBundle && cbFmt <= kcbFmtMaxBundle)
		{
			rsd.vfid[ie] = m_msd.m_vfid[i];
			rsd.vhobj[ie] = m_msd.m_vhobj[i];
			rsd.vws[ie] = m_msd.m_vws[i];
			rsd.vcbTxt[ie] = cchTxt * isizeof(wchar);
			rsd.vcbFmt[ie] = cbFmt;
			memcpy(rsd.vchTxt.Begin() + istr, m_msd.m_vstu[i].Chars(), rsd.vcbTxt[ie]);
			memcpy(rsd.vbFmt.Begin() + ifmt, m_msd.m_vvbFmt[i].Begin(), cbFmt);
			++ie;
			istr += rsd.cchTxtLine;
			ifmt += cbFmtMax;
		}
	}

	// 1. Set the SQL_ATTR_PARAM_BIND_TYPE statement attribute to use column-wise binding.
	// 2. Specify the number of elements in each parameter array.
	// 3. Specify an array in which to return the status of each set of parameters.
	// 4. Specify an SQLUINTEGER value in which to return the number of sets of parameters
	//    processed.
	// 5. Bind the five input parameters (flid, obj, ws, txt, fmt) to the appropriate
	//    arrays.
	// 6. Execute the stored procedure SetMultiStr$, and check the results.

	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc;
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
		reinterpret_cast<void *>(cstrSizeOk), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_STATUS_PTR, rsd.vnParamStatus.Begin(), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMS_PROCESSED_PTR, &rsd.cParamsProcessed, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		rsd.vfid.Begin(), 0, rsd.vcbfid.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		rsd.vhobj.Begin(), 0, rsd.vcbhobj.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 3, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		rsd.vws.Begin(), 0, rsd.vcbws.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 4, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
		rsd.cchTxtLine, 0, rsd.vchTxt.Begin(), rsd.cbTxtLine, rsd.vcbTxt.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 5, SQL_PARAM_INPUT, SQL_C_BINARY, SQL_VARBINARY,
		cbFmtMax, 0, rsd.vbFmt.Begin(), cbFmtMax, rsd.vcbFmt.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

	static const SQLCHAR * pszCmd;
	if(CURRENTDB == FB) {
		pszCmd = (const SQLCHAR *) "EXECUTE PROCEDURE SetMultiStr$ (?,?,?,?,?);";
	}
	if(CURRENTDB == MSSQL) {
		pszCmd = (const SQLCHAR *) "EXEC SetMultiStr$ ?,?,?,?,?;";
	}
	rc = SQLExecDirectA(sstmt.Hstmt(), const_cast<SQLCHAR *>(pszCmd), SQL_NTS);

	CheckMultiStringParamsForSuccess(rsd);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, reinterpret_cast<const char *>(pszCmd));
	sstmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing.

	@param rsd reference to the struct containing the parameter status data
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CheckMultiStringParamsForSuccess(const RawStringData & rsd)
{
	StrAnsi sta;
	StrAnsi staFmt;
	for (uint i = 0; i < rsd.cParamsProcessed; ++i)
	{
		staFmt.Clear();
		switch (rsd.vnParamStatus[i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			break;
		case SQL_PARAM_ERROR:
			// "ERROR in SetMultiStr$ %d,%d,%d,'...',0x..."
			staFmt.Load(kstidXmlErrorMsg023);
			break;
		case SQL_PARAM_UNUSED:
			// "UNUSED in SetMultiStr$ %d,%d,%d,'...',0x..."
			staFmt.Load(kstidXmlErrorMsg119);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "UNAVAIL INFO in SetMultiStr$ %d,%d,%d,'...',0x..."
			staFmt.Load(kstidXmlErrorMsg105);
			break;
		}
		if (staFmt.Length())
		{
			sta.Format(staFmt.Chars(), rsd.vfid[i], rsd.vhobj[i], rsd.vws[i]);
			LogMessage(sta.Chars());
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Write stored multilingual TsStrings to the database which are each longer than
	kcchTxtMaxBundle characters, or have formatting longer than kcbFmtMaxBundle bytes.
	These are handled one TsString at a time.

	@param cstr total number of multilingual TsStrings
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreLargeMultiStringData(int cstr)
{
	for (int i = 0; i < cstr; ++i)
	{
		SQLINTEGER cchTxt = m_msd.m_vstu[i].Length();
		SQLINTEGER cbFmt = m_msd.m_vvbFmt[i].Size();
		if (cchTxt <= kcchTxtMaxBundle && cbFmt <= kcbFmtMaxBundle)
			continue;
		SQLINTEGER cbTxt = cchTxt * isizeof(wchar);
		SQLINTEGER cbTxtLine = (cchTxt + 1) * isizeof(wchar);

		// 1. Bind the two input parameters (txt, fmt) to the appropriate data.
		// 2. Execute the stored procedure SetMultiBigStr$, and check the results.

		SqlStatement sstmt;
		sstmt.Init(m_sdb);
		RETCODE rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR,
			SQL_WLONGVARCHAR, cchTxt+1, 0, const_cast<wchar *>(m_msd.m_vstu[i].Chars()),
			cbTxtLine, &cbTxt);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_BINARY,
			SQL_LONGVARBINARY, cbFmt, 0, m_msd.m_vvbFmt[i].Begin(), cbFmt, &cbFmt);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

		// NOTE: m_staCmd may already have some accumulated SQL commands waiting to execute.
		StrAnsiBuf stabCmd;
		if(CURRENTDB == FB) {
			stabCmd.Format("EXECUTE PROCEDURE SetMultiStr$ (%d,%d,%d,?,?);",
				m_msd.m_vfid[i], m_msd.m_vhobj[i], m_msd.m_vws[i]);
		}
		if(CURRENTDB == MSSQL) {
			stabCmd.Format("EXEC SetMultiStr$ %d,%d,%d,?,?;",
				m_msd.m_vfid[i], m_msd.m_vhobj[i], m_msd.m_vws[i]);
		}
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
		sstmt.Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	Write any stored MultiBigString data to the database.  This uses the SQL stored procedure
	SetMultiBigStr$.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreMultiBigString()
{
	int cstr = m_msdBig.m_vhobj.Size();
	Assert(cstr <= kcstrBigMax);
	Assert(cstr == m_msdBig.m_vfid.Size());
	Assert(cstr == m_msdBig.m_vws.Size());
	Assert(cstr == m_msdBig.m_vstu.Size());
	Assert(cstr == m_msdBig.m_vvbFmt.Size());
	if (!cstr)
		return;

	// Convert from Vector<StrUni> to an array of wchar[cchTxtLine], and from
	// Vector<Vector<byte> > to an array of byte[cbFmtMax].
	//
	// Note that embedding pictures inside the format data can lead to extremely large
	// formatting arrays, and also we don't have any a priori limits on the size or number of
	// strings.  Therefore, we check for very large strings and very large formats, and handle
	// them separately.

	SQLINTEGER cchTxtMax;
	SQLINTEGER cbFmtMax;
	int cstrSizeOk = NormalizeMultiBigStringData(cchTxtMax, cbFmtMax);
	if (cstrSizeOk)
	{
		StoreSmallMultiBigStringData(cstr, cstrSizeOk, cchTxtMax, cbFmtMax);
	}

	// Now, store the strings whose text or formatting data were too large, one at a time.
	if (cstrSizeOk < cstr)
	{
		StoreLargeMultiBigStringData(cstr);
	}

	// Clear these values to prepare for future use.

	m_msdBig.m_vfid.Clear();
	m_msdBig.m_vhobj.Clear();
	m_msdBig.m_vws.Clear();
	m_msdBig.m_vstu.Clear();
	m_msdBig.m_vvbFmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Normalize the unicode for the MultiBigString data, returning the number of MultiBigStrings
	no longer than kcchTxtMaxBundle characters long with formatting no longer than
	kcbFmtMaxBundle bytes long, and setting the maximum length of those strings and maximum
	format size.

	@param cchTxtMax maximum length of the strings to store (<= kcchTxtMaxBundle)
	@param cbFmtMax maximum length of the formats of the strings to store (<= kcbFmtMaxBundle)
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::NormalizeMultiBigStringData(SQLINTEGER & cchTxtMax, SQLINTEGER & cbFmtMax)
{
	cchTxtMax = 0;
	cbFmtMax = 0;
	int cstrSizeOk = 0;
	if (!m_qtsf)
		m_qtsf.CreateInstance(CLSID_TsStrFactory);
	int cstr = m_msdBig.m_vhobj.Size();
	for (int i = 0; i < cstr; ++i)
	{
		// Normalize the string to NFD.
		NormalizeOneString(m_msdBig.m_vstu[i], m_msdBig.m_vvbFmt[i]);

		SQLINTEGER cchTxt = m_msdBig.m_vstu[i].Length();
		SQLINTEGER cbFmt = m_msdBig.m_vvbFmt[i].Size();
		if (cchTxt <= kcchTxtMaxBundle && cbFmt <= kcbFmtMaxBundle)
		{
			++cstrSizeOk;
			if (cchTxtMax < cchTxt)
				cchTxtMax = cchTxt;
			if (cbFmtMax < cbFmt)
				cbFmtMax = cbFmt;
		}
	}
	return cstrSizeOk;
}


/*----------------------------------------------------------------------------------------------
	Write the stored "big multilingual" TsStrings to the database which are each no longer than
	kcchTxtMaxBundle characters with no more than kcbFmtMaxBundle bytes of formatting.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param cpt specific data type, either kcptString or kcptBigString
	@param cstr number of strings of the given field type
	@param cstrSizeOk number of strings to store
	@param cchTxtMax maximum length of the strings to store (<= kcchTxtMaxBundle)
	@param cbFmtMax maximum length of the formats to store (<= kcbFmtMaxBundle)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreSmallMultiBigStringData(int cstr, int cstrSizeOk,
	SQLINTEGER cchTxtMax, SQLINTEGER cbFmtMax)
{
	RawStringData rsd(cstrSizeOk, cchTxtMax, cbFmtMax, true);

	for (int ie = 0, ifmt = 0, istr = 0, i = 0; i < cstr; ++i)
	{
		SQLINTEGER cchTxt = m_msdBig.m_vstu[i].Length();
		SQLINTEGER cbFmt = m_msdBig.m_vvbFmt[i].Size();
		if (cchTxt <= kcchTxtMaxBundle && cbFmt <= kcbFmtMaxBundle)
		{
			rsd.vfid[ie] = m_msdBig.m_vfid[i];
			rsd.vhobj[ie] = m_msdBig.m_vhobj[i];
			rsd.vws[ie] = m_msdBig.m_vws[i];
			rsd.vcbTxt[ie] = cchTxt * isizeof(wchar);
			rsd.vcbFmt[ie] = cbFmt;
			memcpy(rsd.vchTxt.Begin() + istr, m_msdBig.m_vstu[i].Chars(), rsd.vcbTxt[ie]);
			memcpy(rsd.vbFmt.Begin() + ifmt, m_msdBig.m_vvbFmt[i].Begin(), cbFmt);
			++ie;
			istr += rsd.cchTxtLine;
			ifmt += cbFmtMax;
		}
	}

	// 1. Set the SQL_ATTR_PARAM_BIND_TYPE statement attribute to use column-wise binding.
	// 2. Specify the number of elements in each parameter array.
	// 3. Specify an array in which to return the status of each set of parameters.
	// 4. Specify an SQLUINTEGER value in which to return the number of sets of parameters
	//    processed.
	// 5. Bind the five input parameters (flid, obj, ws, txt, fmt) to the appropriate
	//    arrays.
	// 6. Execute the stored procedure SetMultiBigStr$, and check the results.

	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc;
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
		reinterpret_cast<void *>(cstrSizeOk), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_STATUS_PTR, rsd.vnParamStatus.Begin(), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMS_PROCESSED_PTR, &rsd.cParamsProcessed, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		rsd.vfid.Begin(), 0, rsd.vcbfid.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		rsd.vhobj.Begin(), 0, rsd.vcbhobj.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 3, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		rsd.vws.Begin(), 0, rsd.vcbws.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 4, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WLONGVARCHAR,
		rsd.cchTxtLine, 0, rsd.vchTxt.Begin(), rsd.cbTxtLine, rsd.vcbTxt.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 5, SQL_PARAM_INPUT, SQL_C_BINARY, SQL_LONGVARBINARY,
		cbFmtMax, 0, rsd.vbFmt.Begin(), cbFmtMax, rsd.vcbFmt.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

#undef GOODUNICODEONLY
#ifdef GOODUNICODEONLY
	const static SQLCHAR * pzCmd;
	if(CURRENTDB == MSSQL)
		pzCmd = (const SQLCHAR *) "EXEC SetMultiBigStr$ ?,?,?,?,?";
	else if(CURRENTDB == FB)
		pzCmd = (const SQLCHAR *) "EXECUTE PROCEDURE SetMultiBigStr$ (?,?,?,?,?);";
#else
	// ERROR, TILT, CRASH, BURN!!!  ODBC mishandles those Unicode characters that it does
	// not know about when translating from nvarchar to ntxt for stored procedure
	// arguments.  Thus, we have to insert directly into table.  Since we're creating
	// objects, we don't need to worry about update instead of insert, and we're not going
	// to worry about a transaction because each step reduces to a single insert operation.
	const static SQLCHAR * pszCmd;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		pszCmd = (const SQLCHAR *) "INSERT INTO MultiBigStr$ (Flid,Obj,Ws,Txt,Fmt) "
			"VALUES(?,?,?,?,?);";
	}
#endif
	rc = SQLExecDirectA(sstmt.Hstmt(), const_cast<SQLCHAR *>(pszCmd), SQL_NTS);

	CheckMultiBigStringParamsForSuccess(rsd);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, reinterpret_cast<const char *>(pszCmd));
	sstmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing.

	@param rsd reference to the struct containing the parameter status data
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CheckMultiBigStringParamsForSuccess(const RawStringData & rsd)
{
	StrAnsi sta;
	StrAnsi staFmt;
	for (uint i = 0; i < rsd.cParamsProcessed; ++i)
	{
		staFmt.Clear();
		switch (rsd.vnParamStatus[i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			break;
		case SQL_PARAM_ERROR:
			// "ERROR in SetMultiBigStr$ %d,%d,%d,'...',0x..."
			staFmt.Load(kstidXmlErrorMsg021);
			break;
		case SQL_PARAM_UNUSED:
			// "UNUSED in SetMultiBigStr$ %d,%d,%d,'...',0x..."
			staFmt.Load(kstidXmlErrorMsg118);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "UNAVAIL INFO in SetMultiBigStr$ %d,%d,%d,'...',0x..."
			staFmt.Load(kstidXmlErrorMsg104);
			break;
		}
		if (staFmt.Length())
		{
			sta.Format(staFmt.Chars(), rsd.vfid[i], rsd.vhobj[i], rsd.vws[i]);
			LogMessage(sta.Chars());
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Write stored "big multilingual" TsStrings to the database which are each longer than
	kcchTxtMaxBundle characters, or have formatting longer than kcbFmtMaxBundle bytes.
	These are handled one TsString at a time.

	@param cstr total number of "big multilingual" TsStrings
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreLargeMultiBigStringData(int cstr)
{
	for (int i = 0; i < cstr; ++i)
	{
		SQLINTEGER cchTxt = m_msdBig.m_vstu[i].Length();
		SQLINTEGER cbFmt = m_msdBig.m_vvbFmt[i].Size();
		if (cchTxt <= kcchTxtMaxBundle && cbFmt <= kcbFmtMaxBundle)
			continue;
		SQLINTEGER cbTxt = cchTxt * isizeof(wchar);
		SQLINTEGER cbTxtLine = (cchTxt + 1) * isizeof(wchar);

		// 1. Bind the two input parameters (txt, fmt) to the appropriate data.
		// 2. Execute the stored procedure SetMultiBigStr$, and check the results.

		SqlStatement sstmt;
		sstmt.Init(m_sdb);
		RETCODE rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR,
			SQL_WLONGVARCHAR, cchTxt+1, 0, const_cast<wchar *>(m_msdBig.m_vstu[i].Chars()),
			cbTxtLine, &cbTxt);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_BINARY,
			SQL_LONGVARBINARY, cbFmt, 0, m_msdBig.m_vvbFmt[i].Begin(), cbFmt,
			&cbFmt);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

		// NOTE: m_staCmd may already have some accumulated SQL commands waiting to execute.
		StrAnsiBuf stabCmd;
#ifdef GOODUNICODEONLY
		if(CURRENTDB == FB){
			stabCmd.Format("EXECUTE PROCEDURE SetMultiBigStr$ (%d,%d,%d,?,?);",
				m_msdBig.m_vfid[i], m_msdBig.m_vhobj[i], m_msdBig.m_vws[i]);
		}
		if(CURRENTDB == MSSQL){
			stabCmd.Format("EXEC SetMultiBigStr$ %d,%d,%d,?,?",
				m_msdBig.m_vfid[i], m_msdBig.m_vhobj[i], m_msdBig.m_vws[i]);
		}
#else
		// SEE THE COMMENTS ABOVE.
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stabCmd.Format("INSERT INTO MultiBigStr$ (Flid,Obj,Ws,Txt,Fmt) "
				"VALUES(%d,%d,%d,?,?);",
				m_msdBig.m_vfid[i], m_msdBig.m_vhobj[i], m_msdBig.m_vws[i]);
		}
#endif
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
		sstmt.Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	Set the created and modified timestamps for all major objects that did not have these
	values set by the XML data.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::TimeStampObjects()
{

	if(CURRENTDB == FB) {
		m_staCmd.Assign("");
	}
	else if(CURRENTDB == MSSQL) {
		m_staCmd.Assign("DECLARE @d datetime; SET @d = GETDATE();");
	}
	int ifld;
	int icls;
	for (ifld = 0; ifld < m_pfwxd->FieldCount(); ++ifld)
	{
		int cid = m_pfwxd->FieldInfo(ifld).cid;
		if (m_pfwxd->FieldInfo(ifld).cpt == kcptTime &&
			(m_pfwxd->FieldName(ifld).EqualsCI(L"DateCreated") ||
				m_pfwxd->FieldName(ifld).EqualsCI(L"DateModified")) &&
			(m_pfwxd->MapCidToIndex(cid, &icls)))
		{
			if(CURRENTDB == MSSQL) {
				m_staCmd.FormatAppend(" UPDATE \"%<0>S\" SET \"%<1>S\" = @d WHERE \"%<1>S\" IS NULL;",
					m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars());
			}
			else if(CURRENTDB == FB) {
				m_staCmd.FormatAppend(" UPDATE %<0>S SET \"%<1>S\" = CURRENT_TIMESTAMP WHERE \"%<1>S\" IS NULL;",
					m_pfwxd->ClassName(icls).Chars(), UpperName(m_pfwxd->FieldName(ifld)));
			}
		}
	}
	/*if(CURRENTDB == FB){
		m_staCmd.Append(" END!! SET TERM ; !! COMMIT; DROP PROCEDURE Temp; COMMIT;");
	}*/
	//for(int i=0;i<m_staCmd.Length();i++)
		//fprintf(stdout,"%c",m_staCmd[i]);
	ExecuteSimpleSQL(m_staCmd.Chars(), __LINE__);
	//fprintf(stdout, "END of TimeStampObjects\n");
}


/*----------------------------------------------------------------------------------------------
	Update CmObject::UpdDttm on all objects that have a DateModified, setting UpdDttm and
	DateModified to the value from DateModified (rounded to the minute).
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::UpdateDttm()
{
	m_staCmd.Clear();
	int ifld;
	int icls;
	for (ifld = 0; ifld < m_pfwxd->FieldCount(); ++ifld)
	{
		int cid = m_pfwxd->FieldInfo(ifld).cid;
		if (m_pfwxd->FieldInfo(ifld).cpt == kcptTime &&
			m_pfwxd->FieldName(ifld).EqualsCI(L"DateModified") &&
			(m_pfwxd->MapCidToIndex(cid, &icls)))
		{
			//TODO: this syntax won't work for firebird
			if(CURRENTDB == MSSQL) {
				m_staCmd.FormatAppend(
					" UPDATE CmObject"
					" SET UpdDttm = o.DateModified"
					" FROM %<0>S o, CmObject co"
					" WHERE o.id = co.id;",
					m_pfwxd->ClassName(icls).Chars());
			}
			//TODO: this is the best syntax I (John Scebold) could figure out for FB
			else if(CURRENTDB == FB) {
				m_staCmd.FormatAppend(
					" UPDATE CmObject"
					" SET UpdDttm = (SELECT DateModified FROM %<0>S o JOIN CmObject co ON o.Id = co.Id)"
					" WHERE Id = (SELECT o.Id FROM %<0>S o JOIN CmObject co ON o.Id = co.Id);",
					m_pfwxd->ClassName(icls).Chars(), m_pfwxd->ClassName(icls).Chars());
			}
		}
	}
	ExecuteSimpleSQL(m_staCmd.Chars(), __LINE__);
}


/*----------------------------------------------------------------------------------------------
	Set the colors on CmPossibility items to the default color if they are not explicitly
	set to something else.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetCmPossibilityColors()
{
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		m_staCmd.Format("UPDATE CmPossibility SET ForeColor = %d, BackColor = %d, "
			"UnderColor = %d WHERE ForeColor = 0 AND BackColor = 0;", kclrTransparent,
			kclrTransparent, kclrTransparent);
	}
	ExecuteSimpleSQL(m_staCmd.Chars(), __LINE__);
}


/*----------------------------------------------------------------------------------------------
	For any StStyle objects with an empty Rules field, set a zero count into the field instead
	of the default nothingness.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::FixStStyle_Rules()
{
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		ExecuteSimpleSQL("UPDATE StStyle SET Rules = 0x0000 WHERE Rules IS NULL;", __LINE__);
	}
}


/*
 *  These are fixed guids defined for the root/stem type of morphemes.
 */
static const GUID kguidProclitic =
	{ 0xD7F713E2, 0xE8CF, 0x11D3, { 0x97, 0x64, 0x00, 0xC0, 0x4F, 0x18, 0x69, 0x33 } };
static const GUID kguidEnclitic =
	{ 0xD7F713E1, 0xE8CF, 0x11D3, { 0x97, 0x64, 0x00, 0xC0, 0x4F, 0x18, 0x69, 0x33 } };
static const GUID kguidRoot =
	{ 0xD7F713E5, 0xE8CF, 0x11D3, { 0x97, 0x64, 0x00, 0xC0, 0x4F, 0x18, 0x69, 0x33 } };
static const GUID kguidBoundRoot =
	{ 0xD7F713E4, 0xE8CF, 0x11D3, { 0x97, 0x64, 0x00, 0xC0, 0x4F, 0x18, 0x69, 0x33 } };
static const GUID kguidStem =
	{ 0xD7F713E8, 0xE8CF, 0x11D3, { 0x97, 0x64, 0x00, 0xC0, 0x4F, 0x18, 0x69, 0x33 } };
static const GUID kguidParticle =
	{ 0x56DB04BF, 0x3D58, 0x44CC, { 0xB2, 0x92, 0x4C, 0x8A, 0xA6, 0x85, 0x38, 0xF4 } };
static const GUID kguidPhrase =
	{ 0xA23B6FAA, 0x1052, 0x4F4D, { 0x98, 0x4B, 0x4B, 0x33, 0x8B, 0xDA, 0xF9, 0x5F } };
static const GUID kguidDiscontiguousPhrase =
	{ 0x0CC8C35A, 0xCEE9, 0x434D, { 0xBE, 0x58, 0x5D, 0x29, 0x13, 0x0F, 0xBA, 0x5B } };
static const GUID kguidBoundStem =
	{ 0xD7F713E7, 0xE8CF, 0x11D3, { 0x97, 0x64, 0x00, 0xC0, 0x4F, 0x18, 0x69, 0x33 } };

/*----------------------------------------------------------------------------------------------
	Create an underspecified MSA of the given type, returning its database id.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::CreateMsaForEntry(int cid, int hobjEntry)
{
	SqlStatement sstmt;
	RETCODE rc;
	StrAnsi staClass;
	StrAnsi sta;
	SDWORD cbHobj;
	int hvoMsa = 0;
	if (m_pfwxd->IndexOfCid(cid) >= 0)
	{
		staClass = cid == kclidMoStemMsa ? "MoStemMsa" : "MoUnclassifiedAffixMsa";
		if(CURRENTDB == FB) {
			sta.Format("select top 1 msa.id from LexEntry_MorphoSyntaxAnalyses lmsa%n"
				"  join MoMorphSynAnalysis_ msa on msa.id = lmsa.dst%n"
				"  left outer join %s ms on ms.id = msa.id%n"
				"  where lmsa.src = %<1>u and msa.class$ = %<2>u and ms.PartOfSpeech is null;",
				staClass.Chars(), hobjEntry, cid);
		}
		if(CURRENTDB == MSSQL) {
			sta.Format("select top 1 msa.id from LexEntry_MorphoSyntaxAnalyses lmsa%n"
				"  join MoMorphSynAnalysis_ msa on msa.id = lmsa.dst%n"
				"  left outer join %s ms on ms.id = msa.id%n"
				"  where lmsa.src = %<1>u and msa.class$ = %<2>u and ms.PartOfSpeech is null;",
				staClass.Chars(), hobjEntry, cid);
		}
		sstmt.Init(m_sdb);
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(sta.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, sta.Chars());
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_SUCCESS)
		{
			rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_SLONG, &hvoMsa, isizeof(hvoMsa), &cbHobj);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		}

		if (hvoMsa == 0)
		{
			hvoMsa = GenerateNextNewHvo();
			GUID guidNew;
			GenerateNewGuid(&guidNew);
			if(CURRENTDB == FB) {
				sta.Format("EXECUTE PROCEDURE CreateObject$ %<0>u, %<1>u, '%<2>g';%n"
					"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u WHERE Id=%<1>u;",
					cid, hvoMsa, &guidNew, hobjEntry, kflidLexEntry_MorphoSyntaxAnalyses);
			}
			if(CURRENTDB == MSSQL) {
				sta.Format("EXEC CreateObject$ %<0>u, %<1>u, '%<2>g';%n"
					"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u WHERE Id=%<1>u;",
					cid, hvoMsa, &guidNew, hobjEntry, kflidLexEntry_MorphoSyntaxAnalyses);
			}
			ExecuteSimpleSQL(sta.Chars(), __LINE__);
		}

		return hvoMsa;
	}
	else
	{
		return 0;
	}
}


/*----------------------------------------------------------------------------------------------
	For any LexSense object without an associated MSA object, attach an appropriate MSA object
	(either MoStemMsa or MoUnclassifiedAffixMsa), creating it on the LexEntry if necessary.
	See LT-4900 for the motivation.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::FixLexSenseMSAs()
{
	StrAnsi sta;
	// Using CmObject and OwnFlid$=kflidLexemeEntry_LexemeForm is more robust than using the
	// view LexEntry_LexemeForm, as shows up in data migration from really old databases. And
	// since MoUnclassifiedAffixMsa does not exist in early version 2 databases, we may have
	// to temporarily create the table if it doesn't exist...
	if(CURRENTDB == FB) {
		sta.Format("DECLARE VARIABLE vCreate BIT%n"
			"vCreate=0%n"
			"IF (object_id('MoUnclassifiedAffixMsa') IS NULL) THEN BEGIN%n"
			"	vCreate=1;%n"
			"	CREATE TABLE MoUnclassifiedAffixMsa (Id INT, PartOfSpeech INT);%n"
			"END%n"
			"SELECT le.Id, ls.Id, mt.Guid$, s.Id, u.Id%n"
			"FROM LexSense ls%n"
			"JOIN LexEntry_Senses les on les.Dst=ls.Id%n"
			"JOIN LexEntry le ON le.Id=les.Src%n"
			"JOIN CmObject lelf ON lelf.Owner$=le.Id AND lelf.OwnFlid$=%<0>d%n"
			"JOIN MoForm mf ON mf.Id=lelf.Id%n"
			"LEFT OUTER JOIN VW_MoMorphType_ mt on mt.Id=mf.MorphType%n"
			"LEFT OUTER JOIN LexEntry_MorphoSyntaxAnalyses msa ON msa.Src=le.Id%n"
			"LEFT OUTER JOIN MoStemMsa s on s.Id=msa.Dst AND s.PartOfSpeech IS NULL%n"
			"LEFT OUTER JOIN MoUnclassifiedAffixMsa u on u.Id=msa.Dst AND u.PartOfSpeech IS NULL%n"
			"WHERE ls.MorphoSyntaxAnalysis IS NULL;%n"
			"IF (vCreate<>0) THEN DROP TABLE MoUnclassifiedAffixMsa;", kflidLexEntry_LexemeForm);
	}
	if(CURRENTDB == MSSQL) {
		sta.Format("DECLARE @fCreate BIT%n"
			"SET @fCreate=0%n"
			"if object_id('MoUnclassifiedAffixMsa') IS NULL BEGIN%n"
			"	SET @fCreate=1%n"
			"	CREATE TABLE MoUnclassifiedAffixMsa (Id INT, PartOfSpeech INT)%n"
			"END%n"
			"SELECT le.Id, ls.Id, mt.Guid$, s.Id, u.Id%n"
			"FROM LexSense ls%n"
			"JOIN LexEntry_Senses les on les.Dst=ls.Id%n"
			"JOIN LexEntry le ON le.Id=les.Src%n"
			"JOIN CmObject lelf ON lelf.Owner$=le.Id AND lelf.OwnFlid$=%<0>d%n"
			"JOIN MoForm mf ON mf.Id=lelf.Id%n"
			"LEFT OUTER JOIN MoMorphType_ mt on mt.Id=mf.MorphType%n"
			"LEFT OUTER JOIN LexEntry_MorphoSyntaxAnalyses msa ON msa.Src=le.Id%n"
			"LEFT OUTER JOIN MoStemMsa s on s.Id=msa.Dst AND s.PartOfSpeech IS NULL%n"
			"LEFT OUTER JOIN MoUnclassifiedAffixMsa u on u.Id=msa.Dst AND u.PartOfSpeech IS NULL%n"
			"WHERE ls.MorphoSyntaxAnalysis IS NULL%n"
			"IF @fCreate<>0 DROP TABLE MoUnclassifiedAffixMsa;", kflidLexEntry_LexemeForm);
	}
	SqlStatement sstmt;
	RETCODE rc;
	int hobjSense, hobjEntry, hobjStemMsa, hobjAffixMsa;
	SDWORD cbHobj;
	GUID guid;
	SDWORD cbGuid;
	bool fStem;
	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(sta.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, sta.Chars());
	rc = SQLFetch(sstmt.Hstmt());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	Vector<int> vhobjEntry;
	Vector<int> vhobjSense;
	Vector<int> vhobjStemMsa;
	Vector<int> vhobjAffixMsa;
	Vector<bool> vfStem;
	while (rc == SQL_SUCCESS)
	{
		rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobjEntry, isizeof(int), &cbHobj);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLGetData(sstmt.Hstmt(), 2, SQL_C_SLONG, &hobjSense, isizeof(int), &cbHobj);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLGetData(sstmt.Hstmt(), 3, SQL_C_GUID, &guid, isizeof(guid), &cbGuid);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLGetData(sstmt.Hstmt(), 4, SQL_C_SLONG, &hobjStemMsa, isizeof(int), &cbHobj);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (cbHobj == 0 || cbHobj == SQL_NULL_DATA)
			hobjStemMsa = 0;
		rc = SQLGetData(sstmt.Hstmt(), 5, SQL_C_SLONG, &hobjAffixMsa, isizeof(int), &cbHobj);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (cbHobj == 0 || cbHobj == SQL_NULL_DATA)
			hobjAffixMsa = 0;
		if (guid == kguidProclitic || guid == kguidEnclitic || guid == kguidParticle ||
			guid == kguidPhrase || guid == kguidDiscontiguousPhrase ||
			guid == kguidRoot || guid == kguidBoundRoot ||
			guid == kguidStem || guid == kguidBoundStem)
		{
			fStem = true;
		}
		else
		{
			fStem = false;
		}
		vhobjEntry.Push(hobjEntry);
		vhobjSense.Push(hobjSense);
		vfStem.Push(fStem);
		vhobjStemMsa.Push(hobjStemMsa);
		vhobjAffixMsa.Push(hobjAffixMsa);

		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	}
	sstmt.Clear();

	int hobjMsa;
	int clidMsa;
	for (int i = 0; i < vhobjEntry.Size(); ++i)
	{
		if (vfStem[i])
		{
			clidMsa = kclidMoStemMsa;
			hobjMsa = vhobjStemMsa[i] ? vhobjStemMsa[i] :
				CreateMsaForEntry(kclidMoStemMsa, vhobjEntry[i]);
		}
		else
		{
			clidMsa = kclidMoUnclassifiedAffixMsa;
			hobjMsa = vhobjAffixMsa[i] ? vhobjAffixMsa[i] :
				CreateMsaForEntry(kclidMoUnclassifiedAffixMsa, vhobjEntry[i]);
		}
		if(CURRENTDB == FB) {
			sta.Format("IF ((SELECT COUNT(*) FROM Class$ WHERE Id=%<0>d) <> 0) THEN BEGIN%n"
				"    UPDATE LexSense SET MorphoSyntaxAnalysis=%<1>d WHERE Id=%<2>d;%n"
				"END", clidMsa, hobjMsa, vhobjSense[i]);
		}
		if(CURRENTDB == MSSQL) {
			sta.Format("IF (SELECT COUNT(*) FROM Class$ WHERE Id=%<0>d) <> 0 BEGIN%n"
				"    UPDATE LexSense SET MorphoSyntaxAnalysis=%<1>d WHERE Id=%<2>d;%n"
				"END;", clidMsa, hobjMsa, vhobjSense[i]);
		}
		ExecuteSimpleSQL(sta.Chars(), __LINE__);
	}
}



/*----------------------------------------------------------------------------------------------
	Write any stored ReferenceAtom data to the database.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreReferenceAtoms(int ifld)
{
	Assert((unsigned)ifld < (unsigned)m_vstda.Size());
	Assert(m_pfwxd->FieldInfo(ifld).cpt == kcptReferenceAtom);
	int crefTotal = m_vstda[ifld].m_vhobj.Size();
	Assert(crefTotal == m_vstda[ifld].m_vhobjDst.Size());
	Assert(!m_vstda[ifld].m_vstu.Size());
	Assert(!m_vstda[ifld].m_vvbFmt.Size());
	Assert(!m_vstda[ifld].m_vord.Size());
	if (!crefTotal)
		return;
	int icls = GetClassIndexFromFieldIndex(ifld);
	// REVIEW SteveMc: is this reasonable, avoiding SQLBindParameter for small numbers
	// of references in a given field?
	if (crefTotal < kceParamMin)
	{
		StoreFewReferenceAtoms(ifld, icls, crefTotal);
	}
	else
	{
		StoreManyReferenceAtoms(ifld, icls, crefTotal);
	}
	m_vstda[ifld].m_vhobj.Clear();
	m_vstda[ifld].m_vhobjDst.Clear();
}


/*----------------------------------------------------------------------------------------------
	Write a small number of stored ReferenceAtom data to the database.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param crefTotal number of object references.  (< kceParamMin)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreFewReferenceAtoms(int ifld, int icls, int crefTotal)
{
	StrAnsiBufBig stabCmd;
	if(CURRENTDB == MSSQL) {
		stabCmd.Format("UPDATE \"%S\" SET \"%S\" = %d WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
			m_vstda[ifld].m_vhobjDst[0], m_vstda[ifld].m_vhobj[0]);
	}
	else if(CURRENTDB == FB) {
		stabCmd.Format("UPDATE %S SET \"%S\" = %d WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(), UpperName(m_pfwxd->FieldName(ifld)),
			m_vstda[ifld].m_vhobjDst[0], m_vstda[ifld].m_vhobj[0]);
	}
	for (int i = 1; i < crefTotal; ++i)
	{
		if(CURRENTDB == MSSQL) {
			stabCmd.FormatAppend("UPDATE \"%S\" SET \"%S\" = %d WHERE Id = %d;",
				m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
				m_vstda[ifld].m_vhobjDst[i], m_vstda[ifld].m_vhobj[i]);
		}
		else if(CURRENTDB == FB) {
			stabCmd.FormatAppend("UPDATE %S SET \"%S\" = %d WHERE Id = %d;",
				m_pfwxd->ClassName(icls).Chars(), UpperName(m_pfwxd->FieldName(ifld)),
				m_vstda[ifld].m_vhobjDst[i], m_vstda[ifld].m_vhobj[i]);
		}
	}
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
	sstmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Write a large number of stored ReferenceAtom data to the database.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param crefTotal number of object references.  (>= kceParamMin)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreManyReferenceAtoms(int ifld, int icls, int crefTotal)
{
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	SQLUINTEGER cParamsProcessed = 0;
	Vector<SQLUSMALLINT> vnParamStatus;
	Vector<SQLINTEGER> vcbhobjSrc;
	Vector<SQLINTEGER> vcbhobjDst;
	int cref = (crefTotal < kceSeg) ? crefTotal : kceSeg;
	vnParamStatus.Resize(cref);
	vcbhobjSrc.Resize(cref);
	vcbhobjDst.Resize(cref);
	StrAnsiBuf stabCmd;
	if(CURRENTDB == MSSQL) {
		stabCmd.Format("UPDATE \"%S\" SET \"%S\" = ? WHERE Id = ?;",
			m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars());
	}
	else if(CURRENTDB == FB) {
		stabCmd.Format("UPDATE %S SET \"%S\" = ? WHERE Id = ?;",
			m_pfwxd->ClassName(icls).Chars(), UpperName(m_pfwxd->FieldName(ifld)));
	}
	RETCODE rc = SQLPrepareA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Set the SQL_ATTR_PARAM_BIND_TYPE statement attribute to use column-wise binding.
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN,
		0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Specify the number of elements in each parameter array.
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
		reinterpret_cast<void *>(cref), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Specify an array in which to return the status of each set of parameters.
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_STATUS_PTR, vnParamStatus.Begin(), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Specify an SQLUINTEGER value in which to return the number of sets of parameters
	// processed.
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMS_PROCESSED_PTR, &cParamsProcessed, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	for (int ieSegStart = 0; ieSegStart < crefTotal; ieSegStart += kceSeg)
	{
		cref = crefTotal - ieSegStart;
		if (cref > kceSeg)
			cref = kceSeg;
		if (ieSegStart && cref < kceSeg)
		{
			// Specify the number of elements in each parameter array for final, smaller
			// section.
			rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
				reinterpret_cast<void *>(cref), 0);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		}
		rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_SLONG,
			SQL_INTEGER, 0, 0, m_vstda[ifld].m_vhobjDst.Begin() + ieSegStart, 0,
			vcbhobjDst.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_SLONG,
			SQL_INTEGER, 0, 0, m_vstda[ifld].m_vhobj.Begin() + ieSegStart, 0,
			vcbhobjSrc.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

		rc = SQLExecute(sstmt.Hstmt());

		CheckReferenceAtomParamsForSuccess(icls, ifld, ieSegStart, cParamsProcessed,
			vnParamStatus.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
	}
	sstmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing.

	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param ieSegStart the starting index into m_vstda[ifld].m_vhobj[] for the current set of
					objects
	@param cParamsProcessed the number of parameters processed by SQLExecute()
	@param rgnParamStatus the array of parameter status codes returned by SQLExecute()
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CheckReferenceAtomParamsForSuccess(int icls, int ifld, int ieSegStart,
	SQLUINTEGER cParamsProcessed, SQLUSMALLINT * rgnParamStatus)
{
	StrAnsi sta;
	StrAnsi staFmt;
	for (uint i = 0; i < cParamsProcessed; ++i)
	{
		staFmt.Clear();
		switch (rgnParamStatus[i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			break;
		case SQL_PARAM_ERROR:
			// "ERROR with UPDATE [%S] SET [%S]=%d WHERE [Id]=%d"
			staFmt.Load(kstidXmlErrorMsg029);
			break;
		case SQL_PARAM_UNUSED:
			// "UNUSED data UPDATE [%S] SET [%S]=%d WHERE [Id]=%d"
			staFmt.Load(kstidXmlErrorMsg116);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "UNAVAILABLE INFO for UPDATE [%S] SET [%S]=%d WHERE [Id]=%d"
			staFmt.Load(kstidXmlErrorMsg112);
			break;
		}
		if (staFmt.Length())
		{
			sta.Format(staFmt.Chars(),
				m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
				m_vstda[ifld].m_vhobjDst[ieSegStart + i],
				m_vstda[ifld].m_vhobj[ieSegStart + i]);
			LogMessage(sta.Chars());
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Write any stored ReferenceCollection data to the database.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreReferenceCollections(int ifld)
{
	Assert((unsigned)ifld < (unsigned)m_vstda.Size());
	Assert(m_pfwxd->FieldInfo(ifld).cpt == kcptReferenceCollection);
	int crefTotal = m_vstda[ifld].m_vhobj.Size();
	Assert(crefTotal == m_vstda[ifld].m_vhobjDst.Size());
	Assert(!m_vstda[ifld].m_vstu.Size());
	Assert(!m_vstda[ifld].m_vvbFmt.Size());
	Assert(!m_vstda[ifld].m_vord.Size());
	if (!crefTotal)
		return;
	int icls = GetClassIndexFromFieldIndex(ifld);
	// REVIEW SteveMc: is this reasonable, avoiding SQLBindParameter for small numbers
	// of references in a given field?
	if (crefTotal < kceParamMin)
	{
		StoreFewReferenceCollections(ifld, icls, crefTotal);
	}
	else
	{
		StoreManyReferenceCollections(ifld, icls, crefTotal);
	}
	m_vstda[ifld].m_vhobj.Clear();
	m_vstda[ifld].m_vhobjDst.Clear();
}


/*----------------------------------------------------------------------------------------------
	Write a small number of stored ReferenceCollection data to the database.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param crefTotal number of object references.  (< kceParamMin)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreFewReferenceCollections(int ifld, int icls, int crefTotal)
{

	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	StrAnsiBufBig stabCmd;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		stabCmd.Format("INSERT INTO %S_%S (Src, Dst) VALUES (%d,%d);",
			m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
			m_vstda[ifld].m_vhobj[0], m_vstda[ifld].m_vhobjDst[0]);
	}
	for (int i = 1; i < crefTotal; ++i)
	{
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stabCmd.FormatAppend("INSERT INTO %S_%S (Src, Dst) VALUES (%d,%d);",
				m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
				m_vstda[ifld].m_vhobj[i], m_vstda[ifld].m_vhobjDst[i]);
		}
	}
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
	sstmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Write a large number of stored ReferenceCollection data to the database.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param crefTotal number of object references.  (>= kceParamMin)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreManyReferenceCollections(int ifld, int icls, int crefTotal)
{
	SQLUINTEGER cParamsProcessed = 0;
	Vector<SQLUSMALLINT> vnParamStatus;
	Vector<SQLINTEGER> vcbhobjSrc;
	Vector<SQLINTEGER> vcbhobjDst;
	int ieSegStart;
	int cref = (crefTotal < kceSeg) ? crefTotal : kceSeg;
	vnParamStatus.Resize(cref);
	vcbhobjSrc.Resize(cref);
	vcbhobjDst.Resize(cref);
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	StrAnsiBuf stabCmd;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		stabCmd.Format("INSERT INTO %S_%S (Src, Dst) VALUES (?,?);",
			m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars());
	}
	RETCODE rc = SQLPrepareA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Set the SQL_ATTR_PARAM_BIND_TYPE statement attribute to use column-wise binding.
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN,
		0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Specify the number of elements in each parameter array.
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
		reinterpret_cast<void *>(cref), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Specify an array in which to return the status of each set of parameters.
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_STATUS_PTR, vnParamStatus.Begin(), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Specify an SQLUINTEGER value in which to return the number of sets of parameters
	// processed.
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMS_PROCESSED_PTR, &cParamsProcessed, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	for (ieSegStart = 0; ieSegStart < crefTotal; ieSegStart += kceSeg)
	{
		cref = crefTotal - ieSegStart;
		if (cref > kceSeg)
			cref = kceSeg;
		if (ieSegStart && cref < kceSeg)
		{
			// Specify the number of elements in each parameter array for final, smaller
			// section.
			rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
				reinterpret_cast<void *>(cref), 0);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		}
		rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_SLONG,
			SQL_INTEGER, 0, 0, m_vstda[ifld].m_vhobj.Begin() + ieSegStart, 0,
			vcbhobjSrc.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_SLONG,
			SQL_INTEGER, 0, 0, m_vstda[ifld].m_vhobjDst.Begin() + ieSegStart, 0,
			vcbhobjDst.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLExecute(sstmt.Hstmt());
		CheckReferenceCollectionParamsForSuccess(icls, ifld, ieSegStart, cParamsProcessed,
			vnParamStatus.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
	}
	sstmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing.

	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param ieSegStart the starting index into m_vstda[ifld].m_vhobj[] for the current set of
					objects
	@param cParamsProcessed the number of parameters processed by SQLExecute()
	@param rgnParamStatus the array of parameter status codes returned by SQLExecute()
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CheckReferenceCollectionParamsForSuccess(int icls, int ifld,
	int ieSegStart, SQLUINTEGER cParamsProcessed, SQLUSMALLINT * rgnParamStatus)
{
	StrAnsi sta;
	StrAnsi staFmt;
	for (uint i = 0; i < cParamsProcessed; ++i)
	{
		staFmt.Clear();
		switch (rgnParamStatus[i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			break;
		case SQL_PARAM_ERROR:
			// "ERROR with INSERT %S_%S (Src, Dst) VALUES (%d, %d)"
			staFmt.Load(kstidXmlErrorMsg027);
			break;
		case SQL_PARAM_UNUSED:
			// "UNUSED data INSERT %S_%S (Src, Dst) VALUES (%d, %d)"
			staFmt.Load(kstidXmlErrorMsg114);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "UNAVAILABLE INFO for INSERT %S_%S (Src, Dst) VALUES (%d, %d)"
			staFmt.Load(kstidXmlErrorMsg110);
			break;
		}
		if (staFmt.Length())
		{
			sta.Format(staFmt.Chars(),
				m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
				m_vstda[ifld].m_vhobj[ieSegStart + i],
				m_vstda[ifld].m_vhobjDst[ieSegStart + i]);
			LogMessage(sta.Chars());
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Write any stored ReferenceSequence data to the database.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreReferenceSequences(int ifld)
{
	Assert((unsigned)ifld < (unsigned)m_vstda.Size());
	Assert(m_pfwxd->FieldInfo(ifld).cpt == kcptReferenceSequence);
	int crefTotal = m_vstda[ifld].m_vhobj.Size();
	Assert(crefTotal == m_vstda[ifld].m_vhobjDst.Size());
	Assert(crefTotal == m_vstda[ifld].m_vord.Size());
	Assert(!m_vstda[ifld].m_vstu.Size());
	Assert(!m_vstda[ifld].m_vvbFmt.Size());
	if (!crefTotal)
		return;
	int icls = GetClassIndexFromFieldIndex(ifld);
	// REVIEW SteveMc: is this reasonable, avoiding SQLBindParameter for small numbers
	// of references in a given field?
	if (crefTotal < kceParamMin)
	{
		StoreFewReferenceSequences(ifld, icls, crefTotal);
	}
	else
	{
		StoreManyReferenceSequences(ifld, icls, crefTotal);
	}
	m_vstda[ifld].m_vhobj.Clear();
	m_vstda[ifld].m_vhobjDst.Clear();
	m_vstda[ifld].m_vord.Clear();
}


/*----------------------------------------------------------------------------------------------
	Write a small number of stored ReferenceSequence data to the database.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param crefTotal number of object references.  (< kceParamMin)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreFewReferenceSequences(int ifld, int icls, int crefTotal)
{
	StrAnsiBufBig stabCmd;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		stabCmd.Format("INSERT INTO %S_%S (Src, Dst, Ord) VALUES (%d,%d,%d);",
			m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
			m_vstda[ifld].m_vhobj[0], m_vstda[ifld].m_vhobjDst[0], m_vstda[ifld].m_vord[0]);
	}
	for (int i = 1; i < crefTotal; ++i)
	{
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stabCmd.FormatAppend("; INSERT INTO %S_%S (Src, Dst, Ord) VALUES (%d,%d,%d);",
				m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
				m_vstda[ifld].m_vhobj[i], m_vstda[ifld].m_vhobjDst[i], m_vstda[ifld].m_vord[i]);
		}
	}
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
	sstmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Write a large number of stored ReferenceSequence data to the database.

	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param crefTotal number of object references.  (>= kceParamMin)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreManyReferenceSequences(int ifld, int icls, int crefTotal)
{
	SQLUINTEGER cParamsProcessed = 0;
	Vector<SQLUSMALLINT> vnParamStatus;
	Vector<SQLINTEGER> vcbhobjSrc;
	Vector<SQLINTEGER> vcbhobjDst;
	Vector<SQLINTEGER> vcbord;
	int ieSegStart;
	int cref = (crefTotal < kceSeg) ? crefTotal : kceSeg;
	vnParamStatus.Resize(cref);
	vcbhobjSrc.Resize(cref);
	vcbhobjDst.Resize(cref);
	vcbord.Resize(cref);
	StrAnsiBuf stabCmd;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		stabCmd.Format("INSERT INTO %S_%S (Src, Dst, Ord) VALUES (?,?,?);",
			m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars());
	}
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLPrepareA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Set the SQL_ATTR_PARAM_BIND_TYPE statement attribute to use column-wise binding.
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN,
		0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Specify the number of elements in each parameter array.
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
		reinterpret_cast<void *>(cref), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Specify an array in which to return the status of each set of parameters.
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_STATUS_PTR, vnParamStatus.Begin(), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Specify an SQLUINTEGER value in which to return the number of sets of parameters
	// processed.
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMS_PROCESSED_PTR, &cParamsProcessed, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	for (ieSegStart = 0; ieSegStart < crefTotal; ieSegStart += kceSeg)
	{
		cref = crefTotal - ieSegStart;
		if (cref > kceSeg)
			cref = kceSeg;
		if (ieSegStart && cref < kceSeg)
		{
			// Specify the number of elements in each parameter array for final, smaller
			// section.
			rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
				reinterpret_cast<void *>(cref), 0);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		}
		rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_SLONG,
			SQL_INTEGER, 0, 0, m_vstda[ifld].m_vhobj.Begin() + ieSegStart, 0,
			vcbhobjSrc.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_SLONG,
			SQL_INTEGER, 0, 0, m_vstda[ifld].m_vhobjDst.Begin() + ieSegStart, 0,
			vcbhobjDst.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindParameter(sstmt.Hstmt(), 3, SQL_PARAM_INPUT, SQL_C_SLONG,
			SQL_INTEGER, 0, 0, m_vstda[ifld].m_vord.Begin() + ieSegStart, 0,
			vcbord.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLExecute(sstmt.Hstmt());
		CheckReferenceSequenceParamsForSuccess(icls, ifld, ieSegStart, cParamsProcessed,
			vnParamStatus.Begin());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
	}
	sstmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing.

	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vstda and the field meta-data tables (m_pfwxd->m_vfdfi and
					m_pfwxd->m_vstufld), specifying exactly what type of data to store.
	@param ieSegStart the starting index into m_vstda[ifld].m_vhobj[] for the current set of
					objects
	@param cParamsProcessed the number of parameters processed by SQLExecute()
	@param rgnParamStatus the array of parameter status codes returned by SQLExecute()
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CheckReferenceSequenceParamsForSuccess(int icls, int ifld, int ieSegStart,
	SQLUINTEGER cParamsProcessed, SQLUSMALLINT * rgnParamStatus)
{
	StrAnsi sta;
	StrAnsi staFmt;
	for (uint i = 0; i < cParamsProcessed; ++i)
	{
		staFmt.Clear();
		switch (rgnParamStatus[i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			break;
		case SQL_PARAM_ERROR:
			// "ERROR with INSERT %S_%S (Src, Dst, Ord) VALUES (%d, %d, %d)"
			staFmt.Load(kstidXmlErrorMsg028);
			break;
		case SQL_PARAM_UNUSED:
			// "UNUSED data INSERT %S_%S (Src, Dst, Ord) VALUES (%d, %d, %d)"
			staFmt.Load(kstidXmlErrorMsg115);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "UNAVAILABLE INFO for INSERT (Src, Dst, Ord) %S_%S VALUES (%d, %d, %d)"
			staFmt.Load(kstidXmlErrorMsg111);
			break;
		}
		if (staFmt.Length())
		{
			sta.Format(staFmt.Chars(),
				m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
				m_vstda[ifld].m_vhobj[ieSegStart + i],
				m_vstda[ifld].m_vhobjDst[ieSegStart + i],
				m_vstda[ifld].m_vord[ieSegStart + i]);
			LogMessage(sta.Chars());
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Check for a UTF-8 Byte Order Mark at the beginning of the buffer.  If present, erase it by
	shifting the contents of the buffer and subtracting 3 from cbRead.
----------------------------------------------------------------------------------------------*/
static void CheckForBOM(void * pBuffer, ulong & cbRead)
{
	unsigned char * pch = (unsigned char *)pBuffer;
	if (pch[0] == 0xEF && pch[1] == 0xBB && pch[2] == 0xBF)
	{
		// We have a BOM which is totally unnecessary, since the data is a byte stream.
		// Erase it.  For some reason, expat isn't ignoring the BOM as it should.
		cbRead -= 3;
		memmove(pch, pch + 3, cbRead);
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML external entity references (during any phase).  This creates an external entity
	parser, and parses the external entity file before returning.  The established handlers are
	used by this subsidiary parser.

	This static method is passed to the expat XML parser as a callback function.  See the
	comments in xmlparse.h for the XML_ExternalEntityRefHandler typedef for the documentation
	such as it is.

	@null{	ENHANCE SteveMc: if we implement some sort of progress reporting, we'll need	}
	@null{	separate methods for the first pass and any other pass, since on the first pass	}
	@null{	we may need to add the sizes of multiple XML input files in order to get the	}
	@null{	total size.																		}

	@param parser Expat XML parser parsing the entity containing the reference.
	@param pszContext Parsing context in the format expected by XML_ExternalEntityParserCreate.
	@param pszBase System identifier that should be used as the base for resolving pszSystemId
					if pszSystemId was relative; may be NULL.
	@param pszSystemId System identifier as specified in the entity declaration; never NULL.
	@param pszPublicId Public identifier as specified in the entity declaration, or NULL if
					none was specified.

	@return 0 if an error occurs, otherwise 1.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::HandleExternalEntityRef(XML_Parser parser, const XML_Char * pszContext,
	const XML_Char * pszBase, const XML_Char * pszSystemId, const XML_Char * pszPublicId)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(XML_GetUserData(parser));
	AssertPtr(pxid);
	Assert(pxid->m_parser);
	Assert(pxid->m_parser == parser);

	StrAnsi sta;
	StrAnsi staFmt;
	XML_Parser entParser = 0;
	try
	{
		return pxid->ProcessExternalEntityFile(parser, pszContext, pszBase, pszSystemId,
			pszPublicId, entParser);
	}
	catch (Throwable & thr)
	{
		if (entParser)
		{
			pxid->m_parser = parser;
			XML_ParserFree(entParser);
		}
		pxid->m_fError = true;
		pxid->m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		staFmt.Load(kstidXmlDebugMsg003);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(pxid->m_hr));
		pxid->LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		if (entParser)
		{
			pxid->m_parser = parser;
			XML_ParserFree(entParser);
		}
		pxid->m_fError = true;
		pxid->m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		staFmt.Load(kstidXmlDebugMsg005);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		pxid->LogMessage(sta.Chars());
#endif
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Create an external entity parser, and parse the external entity file before returning.  The
	established handlers are used by this subsidiary parser.

	@param parser Expat XML parser parsing the entity containing the reference.
	@param pszContext Parsing context in the format expected by XML_ExternalEntityParserCreate.
	@param pszBase System identifier that should be used as the base for resolving pszSystemId
					if pszSystemId was relative; may be NULL.
	@param pszSystemId System identifier as specified in the entity declaration; never NULL.
	@param pszPublicId Public identifier as specified in the entity declaration, or NULL if
					none was specified.
	@param entParser reference to the parser created to handle the external entity file

	@return 0 if an error occurs, otherwise 1.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::ProcessExternalEntityFile(XML_Parser parser, const XML_Char * pszContext,
	const XML_Char * pszBase, const XML_Char * pszSystemId, const XML_Char * pszPublicId,
	XML_Parser & entParser)
{
	IStreamPtr qstrmInput;
	STATSTG  statFile;
	StrAnsiBufPath stabpFile;
	ResolvePath(pszBase, pszSystemId, stabpFile);
	// Try to open the external entity file.
	if (stabpFile.Overflow())
		ThrowHr(WarnHr(E_OUTOFMEMORY));
	FileStream::Create(stabpFile.Chars(), STGM_READ, &qstrmInput);
	CheckHr(qstrmInput->Stat(&statFile, STATFLAG_NONAME));
	// Create a subsidiary parser object.
	entParser = XML_ExternalEntityParserCreate(parser, pszContext, 0);
	if (entParser == 0)
		ThrowHr(WarnHr(E_UNEXPECTED));
	LARGE_INTEGER libMove = {0,0};
	ULARGE_INTEGER ulibPos;
	if (!XML_SetBase(entParser, stabpFile.Chars()))
	{
		// "Out of memory!"
		StrAnsi sta(kstidXmlErrorMsg096);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_OUTOFMEMORY));
	}
	m_parser = entParser;
	// Process the external entity file.
	for (int cblk = 0; ; ++cblk)
	{
		ulong cbRead;
		void * pBuffer = XML_GetBuffer(entParser, READ_SIZE);
		if (!pBuffer)
			ThrowHr(WarnHr(E_UNEXPECTED));
		CheckHr(qstrmInput->Read(pBuffer, READ_SIZE, &cbRead));
		if (cblk == 0)
			CheckForBOM(pBuffer, cbRead);
		if (!XML_ParseBuffer(entParser, cbRead, cbRead == 0))
			ThrowHr(WarnHr(E_FAIL));
		CheckHr(qstrmInput->Seek(libMove, STREAM_SEEK_CUR, &ulibPos));
		if (ulibPos.HighPart == statFile.cbSize.HighPart &&
			ulibPos.LowPart == statFile.cbSize.LowPart)
		{
			m_parser = parser;
			XML_ParserFree(entParser);
			// Signal that we processed the entire external entity okay.
			return 1;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Return the index into the database field definition tables for the given custom field, or
	-1 if the custom field cannot be found.

	@param pszField Name of the custom field.
	@param cid Class id number of the class containing the custom field.  This may be a subclass
					of the actual class where the custom field was defined.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetCustomFieldIndex(const char * pszField, int cid)
{
	int ifld;
	int icls;
	StrUni stu;
	while (cid > 0)
	{
		stu.Format(L"%S%d", pszField, cid);
		if (m_pfwxd->m_hmsuXmlifld.Retrieve(stu, &ifld) && m_pfwxd->FieldInfo(ifld).fCustom)
			return ifld;
		if (!m_pfwxd->MapCidToIndex(cid, &icls))
			return -1;
		if (cid == m_pfwxd->ClassInfo(icls).cidBase)
			return -1;
		// Okay, try the superclass.
		cid = m_pfwxd->ClassInfo(icls).cidBase;
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Return the database object id of the user interface writing system.  (Actually, it tries
	for English, and failing that, returns the first ws in the hashmap.)
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetUserWs()
{
	int wsEn;
	if (!m_hmcws.Retrieve("en", &wsEn))
	{
		// use any valid ws if "en" doesn't exist -- this is just a temporary hack that the
		// user will have to fix.
		HashMapChars<int>::iterator it = m_hmcws.Begin();
		wsEn = it->GetValue();
	}
	return wsEn;
}


/*----------------------------------------------------------------------------------------------
	Return the database object id of the writing system identified by its ICU Locale string.

	@param pszWs Writing system short name (ICU Locale string)
	@param stidErrMsg Resource ID of appropriate error message format string if the writing
		system does not exist.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetWsFromIcuLocale(const char * pszWs, int stidErrMsg)
{
	if (pszWs == NULL || *pszWs == '\0')
	{
		StrAnsi sta(kstidXmlErrorMsg174);
		LogMessage(sta.Chars());
		return DefaultAnalysisWritingSystem();
	}
	int ws;
	StrAnsi staWs(pszWs);
	staWs.ToLower();
	if (m_hmcws.Retrieve(staWs.Chars(), &ws))
	{
		if (ws != DefaultAnalysisWritingSystem() && ws != DefaultVernacularWritingSystem())
			m_setExtraWsUsed.Insert(ws);
		return ws;
	}

	StrAnsi staFmt;
	if (m_fAllowNewWritingSystems)
	{
		ILgWritingSystemFactoryPtr qwsf;
		ILgWritingSystemFactoryBuilderPtr qwsfb;
		qwsfb.CreateInstance(CLSID_LgWritingSystemFactoryBuilder);
		CheckHr(qwsfb->GetWritingSystemFactoryNew(m_pfwxd->ServerName().Bstr(),
			m_pfwxd->DatabaseName().Bstr(), NULL, &qwsf));

		// When merging data, let's go ahead and create a new writing system with the minimal
		// information that we have.  (ICULocale string)
		ws = GenerateNextNewHvo();
		int wsUser;
		CheckHr(qwsf->get_UserWs(&wsUser));
		GUID guid;
		GenerateNewGuid(&guid);
		// Get the lcid from ICU if we can.
		StrUtil::InitIcuDataDir();		// just in case...
		Locale loc(pszWs);
		int lcid = loc.getLCID();
		if (!lcid)
		{
			// Ugh.  Use US English and hope for the best.
			lcid = LANG_ENGLISH;
		}
		if (lcid < 0x400)
		{
			// It's a 'primary language ID', a very common case we get from any
			// language name that doesn't have an underscore, like "en"!
			// Windows won't accept this as a langid or lcid, so fix it into a valid one.
			lcid = MAKELCID(MAKELANGID(lcid, SUBLANG_DEFAULT), SORT_DEFAULT);
		}
		IIcuCleanupManagerPtr qicln;
		qicln.CreateInstance(CLSID_IcuCleanupManager);
		CheckHr(qicln->Cleanup());

		StrAnsi staCmd;
		if(CURRENTDB == FB) {
			staCmd.Format("EXECUTE PROCEDURE CreateObject$ @clid=%<0>u, @id=%<1>u, @guid='%<2>g'; "
				"UPDATE LgWritingSystem SET ICULocale='%<3>s', Locale=%<4>d WHERE Id = %<1>d; "
				"INSERT INTO LgWritingSystem_Name (Obj, Ws, Txt) VALUES (%<1>d, %<5>d, '%<3>s'); "
				"INSERT INTO LgWritingSystem_Abbr (Obj, Ws, Txt) VALUES (%<1>d, %<5>d, '%<3>s');",
				kclidLgWritingSystem, ws, &guid, pszWs, lcid, wsUser);
		}
		if(CURRENTDB == MSSQL) {
			staCmd.Format("EXEC CreateObject$ @clid=%<0>u, @id=%<1>u, @guid='%<2>g'; "
				"UPDATE LgWritingSystem SET ICULocale='%<3>s', Locale=%<4>d WHERE Id = %<1>d; "
				"INSERT INTO LgWritingSystem_Name (Obj, Ws, Txt) VALUES (%<1>d, %<5>d, '%<3>s'); "
				"INSERT INTO LgWritingSystem_Abbr (Obj, Ws, Txt) VALUES (%<1>d, %<5>d, '%<3>s');",
				kclidLgWritingSystem, ws, &guid, pszWs, lcid, wsUser);
		}
		SqlStatement sstmt;
		sstmt.Init(m_sdb);
		RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(staCmd.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, staCmd.Chars());
		sstmt.Clear();
		int hvoColl = GenerateNextNewHvo();
		GUID guidColl;
		GenerateNewGuid(&guidColl);
		StrUni stuWinColl(L"Latin1_General_CI_AI");
		StrUni stuCollName(L"default");
		if(CURRENTDB == FB) {
			staCmd.Format("EXECUTE PROCEDURE CreateOwnedObject$ %<0>u, %<1>u, '%<2>g', %<3>u, %<4>u, %<5>u; "
				"UPDATE LgCollation SET WinLCID=%<6>u, WinCollation='%<7>S' WHERE Id = %<1>u; "
				"INSERT INTO LgCollation_Name (Obj, Ws, Txt) VALUES (%<1>u, %<8>d, '%<9>S');",
				kclidLgCollation, hvoColl, &guidColl,
				ws, kflidLgWritingSystem_Collations, kcptOwningSequence,
				lcid, stuWinColl.Chars(), wsUser, stuCollName.Chars());
		}
		if(CURRENTDB == MSSQL) {
			staCmd.Format("EXEC CreateOwnedObject$ %<0>u, %<1>u, '%<2>g', %<3>u, %<4>u, %<5>u; "
				"UPDATE LgCollation SET WinLCID=%<6>u, WinCollation='%<7>S' WHERE Id = %<1>u; "
				"INSERT INTO LgCollation_Name (Obj, Ws, Txt) VALUES (%<1>u, %<8>d, '%<9>S');",
				kclidLgCollation, hvoColl, &guidColl,
				ws, kflidLgWritingSystem_Collations, kcptOwningSequence,
				lcid, stuWinColl.Chars(), wsUser, stuCollName.Chars());
		}
		sstmt.Init(m_sdb);
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(staCmd.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, staCmd.Chars());
		sstmt.Clear();

		// Ensure that the newly created writing system gets installed into the factory.
		StrUni stuWs(pszWs);
		CheckHr(qwsf->AddWritingSystem(ws, stuWs.Bstr()));
		CheckHr(qwsf->Shutdown());

		// Adding the writing system may possibly change the database id for the Collation,
		// or maybe even create multiple Collation objects.  This is because it may be
		// recreating/reloading the writing system from an existing language XML file.
		// Ensure that GenerateNextNewHvo() will do the right thing.
		int hvoNext = GetNextRealObjectId();
		while (hvoNext > hvoColl + 1)
			hvoColl = GenerateNextNewHvo();
		Assert(hvoNext == hvoColl + 1);
		// staWs is already lower-cased.
		m_hmcws.Insert(staWs.Chars(), ws);
		// "Created new writing system for code ""%<0>s""."
		staFmt.Load(kstidXmlInfoMsg223);
		m_setExtraWsUsed.Insert(ws);
	}
	else
	{
		ws = 0;
		staFmt.Load(stidErrMsg);
	}
	StrAnsi sta;
	sta.Format(staFmt.Chars(), pszWs);
	LogMessage(sta.Chars());

	return ws;
}


/*----------------------------------------------------------------------------------------------
	Handle XML start elements for objects during the first pass.

	@param eti Reference to the basic element type information structure.
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StartObject1(const ElemTypeInfo & eti, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	const ElemTypeInfo & etiProp = CheckObjectNesting(pszName);

	int hobjOwner = GetOwnerOfCurrentObject(etiProp, pszName);
	if (m_fMerge && CheckForExistingObject(eti, hobjOwner))
		return;

	// Parse the XML ID string, and use the GUID if it is GUID-based.
	GUID guidObj = GUID_NULL;
	int hobj = GetObjectIdAndGuid(prgpszAtts, guidObj);
	m_vetiOpen.Push(eti);
	m_vhobjOpen.Push(hobj);
	if (hobjOwner < 0)
	{
		PushRootObjectInfo(hobj, guidObj, eti.m_icls);
	}
	else
	{
		PushOwnedObjectInfo(hobj, guidObj, eti.m_icls, hobjOwner, etiProp.m_ifld, pszName,
			prgpszAtts);
	}
	++m_cobj;
}

/*----------------------------------------------------------------------------------------------
	Check the nesting for XML start elements for objects during the first pass.

	@param pszName XML element name read from the input file.

	@return ElemTypeInfo value for the containing property
----------------------------------------------------------------------------------------------*/
const ElemTypeInfo & FwXmlImportData::CheckObjectNesting(const XML_Char * pszName)
{
	StrAnsi sta;
	StrAnsi staFmt;
	if (!m_vetiOpen.Size())
	{
		// "<%<0>s> must be nested inside <%<1>s>...</%<1>s>!"
		ThrowWithLogMessage(kstidXmlErrorMsg004, pszName, m_staBeginTag.Chars());
	}
	return m_vetiOpen[m_vetiOpen.Size() - 1];
}

/*----------------------------------------------------------------------------------------------
	Return the database object id of the owner for the newly opened object element.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetOwnerOfCurrentObject(const ElemTypeInfo & etiProp,
	const XML_Char * pszName)
{
	if (etiProp.m_elty == keltyPropName || etiProp.m_elty == keltyCustomProp ||
		etiProp.m_elty == keltyVirtualProp)
	{
		ElemTypeInfo & etiObject = m_vetiOpen[m_vetiOpen.Size() - 2];
		if (etiObject.m_elty != keltyObject || !m_vhobjOpen.Size())
		{
			// "<%s> is improperly nested!"
			ThrowWithLogMessage(kstidXmlErrorMsg002, pszName);
		}
		return m_vhobjOpen[m_vhobjOpen.Size() - 1];
	}
	else if (etiProp.m_elty == keltyDatabase)
	{
		return m_hvoOwner;
	}
	else
	{
		// "<%s> must be nested inside <%s> or an object attribute element!"
		ThrowWithLogMessage(kstidXmlErrorMsg003, pszName, m_staBeginTag.Chars());
		return -1;
	}
}


/*----------------------------------------------------------------------------------------------
	Load a simple message from the resources, log it, and then throw E_UNEXPECTED.

	@param stid resource id of the message string
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ThrowWithLogMessage(int stid)
{
	StrAnsi sta(stid);
	LogMessage(sta.Chars());
	ThrowHr(WarnHr(E_UNEXPECTED));
}

/*----------------------------------------------------------------------------------------------
	Load a message format from the resources, apply the arguments, log the result, and then
	throw E_UNEXPECTED.

	@param stid resource id of the message string
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ThrowWithLogMessage(int stid, const void * psz1)
{
	StrAnsi staFmt(stid);
	StrAnsi sta;
	sta.Format(staFmt.Chars(), psz1);
	LogMessage(sta.Chars());
	ThrowHr(WarnHr(E_UNEXPECTED));
}

/*----------------------------------------------------------------------------------------------
	Load a message format from the resources, apply the two arguments, log the result, and then
	throw E_UNEXPECTED.

	@param stid resource id of the message string
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ThrowWithLogMessage(int stid, const void * psz1, const void * psz2)
{
	StrAnsi staFmt(stid);
	StrAnsi sta;
	sta.Format(staFmt.Chars(), psz1, psz2);
	LogMessage(sta.Chars());
	ThrowHr(WarnHr(E_UNEXPECTED));
}


/*----------------------------------------------------------------------------------------------
	Get an object's id string, and calculate its guid from that if possible.  Store the mappings
	from guid to hobj, and from the id string to hobj.

	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param guidObj reference to the GUID for the object (set as a side-effect)

	@return database id of the object
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetObjectIdAndGuid(const XML_Char ** prgpszAtts, GUID & guidObj)
{
	int hobj;
	if (m_hvoObj && m_vetiOpen.Size() == 1 &&
		m_pfwxd->FieldInfo(m_vetiOpen[0].m_ifld).fid == m_flid)
	{
		// The object already exists, it just needs to be fleshed out.
		hobj = m_hvoObj;
		guidObj = m_guidObj;
	}
	else
	{
		hobj = m_cobj + m_hvoMin;
	}
	StrAnsiBufSmall stabs;
	const char * pszId = FwXml::GetAttributeValue(prgpszAtts, "id");
	if (!pszId)
	{
		// No ID: manufacture one.
		stabs.Format(":%d", m_celemStart);
		pszId = stabs.Chars();
	}
	if (FwXml::ParseGuid(pszId + 1, &guidObj))
	{
		if (m_hmguidhobj.Retrieve(guidObj, &hobj))
		{
			if (m_hvoObj && hobj != m_hvoObj)
			{
				// "Repeated object GUID"
				ThrowWithLogMessage(kstidXmlErrorMsg097);
			}
			Assert(guidObj == m_guidObj);
		}
		else if (m_hvoObj && hobj == m_hvoObj)
		{
			// "Invalid GUID-based id string for importing object."
			ThrowWithLogMessage(kstidXmlErrorMsg156);
		}
		else
		{
			m_hmguidhobj.Insert(guidObj, hobj);
		}
		if (!m_chGuid)
		{
			m_chGuid = *pszId;
			m_cchGuid = 1;
		}
		else if (m_chGuid != *pszId)
		{
			if (m_cchGuid == 1)
			{
				// "WARNING: GUID-based id strings do not all begin with the same letter!"
				StrAnsi sta(kstidXmlErrorMsg130);
				LogMessage(sta.Chars());
			}
			++m_cchGuid;
		}
	}
	else
	{
		if (guidObj == GUID_NULL)
		{
			GenerateNewGuid(&guidObj);
		}
		if (m_hmcidhobj.Retrieve(pszId, &hobj))
		{
			// "Repeated object ID"
			ThrowWithLogMessage(kstidXmlErrorMsg098);
		}
		m_hmcidhobj.Insert(pszId, hobj);
	}
	return hobj;
}


/*----------------------------------------------------------------------------------------------
	Push the data needed later to create a root (unowned) object.

	@param hobj Database id for an object.
	@param guidObj GUID for the object.
	@param icls index into the class table for the object's class
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::PushRootObjectInfo(int hobj, GUID guidObj, int icls)
{
	Assert(!m_vspiOpen.Size());
	m_rod.m_vhobj.Push(hobj);
	m_rod.m_vguid.Push(guidObj);
	m_rod.m_vicls.Push(icls);
}


/*----------------------------------------------------------------------------------------------
	Push the data needed later to create an owned object.

	@param hobj Database id for an object.
	@param guidObj GUID for the object.
	@param icls index into the class table for the object's class
	@param hobjOwner Database id of the object's owner
	@param ifld index into the field table for the field in the object's owner
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::PushOwnedObjectInfo(int hobj, GUID guidObj, int icls, int hobjOwner,
	int ifld, const XML_Char * pszName, const XML_Char ** prgpszAtts)
{
	Assert(m_vspiOpen.Size());
	m_ood.m_vhobj.Push(hobj);
	m_ood.m_vguid.Push(guidObj);
	m_ood.m_vicls.Push(icls);
	m_ood.m_vhobjOwner.Push(hobjOwner);
	m_ood.m_vfidOwner.Push(m_pfwxd->FieldInfo(ifld).fid);
	int cpt = m_pfwxd->FieldInfo(ifld).cpt;
	m_ood.m_vcpt.Push(cpt);
	int ord;
	const char * pszOrd;
	switch (cpt)
	{
	case kcptOwningAtom:
		ord = 0;
		break;
	case kcptOwningCollection:
		ord = hobj;
		break;
	case kcptOwningSequence:
		m_vspiOpen.Top()->m_cobj++;
		pszOrd = FwXml::GetAttributeValue(prgpszAtts, "ord");
		if (pszOrd)
		{
			char * psz;
			ord = static_cast<int>(strtol(pszOrd, &psz, 10));
			if (*psz)
			{
				// "Invalid ord attribute value: \"%s\""
				ThrowWithLogMessage(kstidXmlErrorMsg062, pszOrd);
			}
			m_vspiOpen.Top()->m_cord++;
			if (m_vspiOpen.Top()->m_cobj != m_vspiOpen.Top()->m_cord)
			{
				// "Cannot have some ord attribute values missing and some present!"
				ThrowWithLogMessage(kstidXmlErrorMsg015);
			}
		}
		else
		{
			if (m_vspiOpen.Top()->m_cord)
			{
				// "Missing %<0>s attribute value in %<1>s element."
				ThrowWithLogMessage(kstidXmlErrorMsg079, "ord", pszName);
			}
			ord = ++m_vspiOpen.Top()->m_ord;
		}
		break;
	default:
		// "Invalid field type containing <%s> element: %d."
		ThrowWithLogMessage(kstidXmlErrorMsg060, pszName,
			(const void *)m_pfwxd->FieldInfo(ifld).cpt);
		break;
	}
	m_ood.m_vordOwner.Push(ord);
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements for object properties during the first pass.

	@param eti Reference to the basic element type information structure.
	@param pszName XML element name read from the input file.
	@param pszProp custom field name (default is NULL)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StartPropName1(ElemTypeInfo & eti, const XML_Char * pszName,
	const char * pszProp)
{
	const ElemTypeInfo & etiObject = CheckPropNameNesting(pszName);
	if (pszProp)
	{
		eti.m_ifld = GetCustomFieldIndex(pszProp, m_pfwxd->ClassInfo(etiObject.m_icls).cid);
		if (eti.m_ifld < 0)
		{
			// "Improperly nested <%s> element!"
			ThrowWithLogMessage(kstidXmlErrorMsg047, pszName);
		}
	}
	m_vetiOpen.Push(eti);
	PushSeqPropInfo(eti);
}

/*----------------------------------------------------------------------------------------------
	Check the nesting for XML start elements for object properties during the first pass.

	@param pszName XML element name read from the input file.

	@return ElemTypeInfo value for the containing object
----------------------------------------------------------------------------------------------*/
const ElemTypeInfo & FwXmlImportData::CheckPropNameNesting(const XML_Char * pszName)
{
	// Check for proper nesting.
	if (!m_vetiOpen.Size())
	{
		// "<%<0>s> must be nested inside <%<1>s>...</%<1>s>!"
		ThrowWithLogMessage(kstidXmlErrorMsg004, pszName, m_staBeginTag.Chars());
	}
	const ElemTypeInfo & etiObject = m_vetiOpen[m_vetiOpen.Size() - 1];
	if (etiObject.m_elty != keltyObject)
	{
#if 99-99
		ShowElemTypeStack();
#endif
		// "<%s> must be nested inside an object element!"
		ThrowWithLogMessage(kstidXmlErrorMsg006, pszName);
	}
	return etiObject;
}

/*----------------------------------------------------------------------------------------------
	Log the element type stack (for debugging purposes).
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ShowElemTypeStack()
{
	const char * pszType;
	StrAnsi staFmt;
	StrAnsi sta;
	for (int i = 0; i < m_vetiOpen.Size(); ++i)
	{
		switch (m_vetiOpen[i].m_elty)
		{
		case keltyBad:			pszType = "Bad";			break;
		case keltyDatabase:		pszType = "Database";		break;
		case keltyAddProps:		pszType = "AddProps";		break;
		case keltyDefineProp:	pszType = "DefineProp";		break;
		case keltyObject:		pszType = "Object";			break;
		case keltyCustomProp:	pszType = "CustomProp";		break;
		case keltyPropName:		pszType = "PropName";		break;
		case keltyVirtualProp:	pszType = "VirtualProp";	break;
		case keltyBasicProp:	pszType = "BasicProp";		break;
		default:				pszType = "??UNKNOWN??";	break;
		}
		// "m_vetiOpen[%d].m_elty = %s, m_icls = %d"
		staFmt.Load(kstidXmlDebugMsg006);
		sta.Format(staFmt.Chars(), i, pszType, m_vetiOpen[i].m_icls);
		LogMessage(sta.Chars());
	}
}


/*----------------------------------------------------------------------------------------------
	Handle XML start elements for basic type properties during the first pass.

	@param eti Reference to the basic element type information structure.
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StartBasicProp1(const ElemTypeInfo & eti, const XML_Char * pszName)
{
	ElemTypeInfo etiProp = CheckBasicPropNesting(pszName);
	m_vetiOpen.Push(eti);
	if (eti.m_cpt == kcptString || eti.m_cpt == kcptMultiString)
	{
		XML_SetElementHandler(m_parser, FwXmlImportData::HandleStringStartTag1,
			FwXmlImportData::HandleStringEndTag1);
	}
	else if (eti.m_cpt == kcptRuleProp)
	{
		m_fInRuleProp = true;
		m_fInWsStyles = false;
		XML_SetElementHandler(m_parser, FwXmlImportData::HandlePropStartTag1,
			FwXmlImportData::HandlePropEndTag1);
	}
	m_vcfld[etiProp.m_ifld]++;
	if (eti.m_cpt == kcptUnicode &&
		m_pfwxd->FieldInfo(etiProp.m_ifld).fid == kflidLgWritingSystem_ICULocale)
	{
		m_staChars.Clear();
		m_fIcuLocale = true;
	}
}

/*----------------------------------------------------------------------------------------------
	Check the nesting for XML start elements for basic type properties during the first pass.

	@param pszName XML element name read from the input file.

	@return ElemTypeInfo value for the containing property
----------------------------------------------------------------------------------------------*/
const ElemTypeInfo & FwXmlImportData::CheckBasicPropNesting(const XML_Char * pszName)
{
	StrAnsi sta;
	StrAnsi staFmt;

	// Check for proper nesting.
	if (m_vetiOpen.Size() < 2)
	{
		// "<%<0>s> must be nested inside <%<1>s>...</%<1>s>!"
		staFmt.Load(kstidXmlErrorMsg004);
		sta.Format(staFmt.Chars(), pszName, m_staBeginTag.Chars());
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	const ElemTypeInfo & etiProp = m_vetiOpen[m_vetiOpen.Size() - 1];
	if (etiProp.m_elty != keltyPropName && etiProp.m_elty != keltyCustomProp &&
		etiProp.m_elty != keltyVirtualProp)
	{
		// "<%s> must be nested inside an object attribute element!"
		staFmt.Load(kstidXmlErrorMsg005);
		sta.Format(staFmt.Chars(), pszName);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	const ElemTypeInfo & etiObject = m_vetiOpen[m_vetiOpen.Size() - 2];
	if (etiObject.m_elty != keltyObject)
	{
		// "<%s> must be nested inside an object element!"
		staFmt.Load(kstidXmlErrorMsg006);
		sta.Format(staFmt.Chars(), pszName);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	return etiProp;
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements for objects during the second pass.

	@param eti Reference to the basic element type information structure.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StartObject2(ElemTypeInfo & eti, const XML_Char * pszName)
{
	// We've already created this object, and stored a cross-reference to its id.
	Assert(m_vetiOpen.Size());
	bool fIncCobj2 = true;
	int hobj;
	if (m_hvoObj && m_vetiOpen.Size() == 1 && m_vhobjOpen.Size() == 0 &&
		m_pfwxd->FieldInfo(m_vetiOpen[0].m_ifld).fid == m_flid)
	{
		// The object already existed, it just needs to be fleshed out.
		hobj = m_hvoObj;
	}
	else if (m_fMerge && GetExistingObject(eti, pszName, hobj))
	{
		fIncCobj2 = false;
	}
	else
	{
		hobj = m_cobj2 + m_hvoMin;
	}
	m_vetiOpen.Push(eti);
	m_vhobjOpen.Push(hobj);
	if (fIncCobj2)
		++m_cobj2;
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements for custom properties during the second pass.

	@param eti Reference to the basic element type information structure.
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StartCustomProp2(ElemTypeInfo & eti, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	StrAnsi sta;
	StrAnsi staFmt;
	const char * pszProp = FwXml::GetAttributeValue(prgpszAtts, "name");
	if (!pszProp)
	{
		// "Missing %<0>s attribute for %<1>s element!"
		staFmt.Load(kstidXmlErrorMsg079);
		sta.Format(staFmt.Chars(), "name", pszName);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	else
	{
		eti.m_ifld = GetCustomFieldIndex(pszProp,
			m_pfwxd->ClassInfo(m_vetiOpen.Top()->m_icls).cid);
		if (eti.m_ifld < 0)
		{
			// "Improperly nested <%s name=\"%s\"> element!"
			staFmt.Load(kstidXmlErrorMsg046);
			sta.Format(staFmt.Chars(), pszName, pszProp);
			LogMessage(sta.Chars());
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
	}
	StartPropName2(eti);
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements for properties during the second pass.

	@param eti Reference to the basic element type information structure.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StartPropName2(ElemTypeInfo & eti)
{
	m_vetiOpen.Push(eti);
	PushSeqPropInfo(eti);
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements for virtual properties during the second pass.

	@param eti Reference to the basic element type information structure.
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StartVirtualProp2(ElemTypeInfo & eti, const XML_Char *pszName)
{
	m_vetiOpen.Push(eti);
	SeqPropInfo spi;
	spi.m_fSeq = false;
	spi.m_cobj = 0;
	spi.m_cord = 0;
	spi.m_ord = 0;
	m_vspiOpen.Push(spi);
}

/*----------------------------------------------------------------------------------------------
	Push the information needed to process a sequence type property.

	@param eti Reference to the basic element type information structure.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::PushSeqPropInfo(ElemTypeInfo & eti)
{
	SeqPropInfo spi;
	switch (m_pfwxd->FieldInfo(eti.m_ifld).cpt)
	{
	case kcptOwningSequence:
	case kcptReferenceSequence:
		spi.m_fSeq = true;
		spi.m_ord = 1;
		break;
	default:
		spi.m_fSeq = false;
		spi.m_ord = 0;
		break;
	}
	spi.m_cobj = 0;
	spi.m_cord = 0;
	m_vspiOpen.Push(spi);
}


/*----------------------------------------------------------------------------------------------
	Skip over a valid number string

	@param pszNum Character string that begins with a number, either an integer or floating
					point.

	@return A pointer to the first character past the end of the number.
----------------------------------------------------------------------------------------------*/
static const char * ScanNumber(const char * pszNum)
{
	const char * pszEnd = pszNum;
	if (*pszEnd == '+' || *pszEnd == '-')
		++pszEnd;
	int cDigits = strspn(pszEnd, "0123456789");
	pszEnd += cDigits;
	if (*pszEnd == '.')
		++pszEnd;
	int cDigitsFrac = strspn(pszEnd, "0123456789");
	pszEnd += cDigitsFrac;
	if (cDigits + cDigitsFrac)		// Must have at least one digit in a number!
		return pszEnd;
	else
		return pszNum;
}

/*----------------------------------------------------------------------------------------------
	Handle a link element which lacks a target attribute, but which has a ws attribute and
	either an abbr attribute or a name attribute (or both).  This set of attributes implies a
	possible CmPossibility object, at least if the information from FieldInfo(ifld) agrees.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param ifld Index into m_vfdfi and m_vstufld for the field of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ws Database id of the desired Writing system.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::StoreCmPossibilityReference(const XML_Char * pszName,
	const XML_Char ** prgpszAtts, int hobjOwner, int ifld, int icls, int ws)
{
	const char * pszTargetAbbr = FwXml::GetAttributeValue(prgpszAtts, "abbr");
	const char * pszTargetName = FwXml::GetAttributeValue(prgpszAtts, "name");
	if (!pszTargetAbbr && !pszTargetName)
		return false;

	// Check for the optional vernacular writing system, name, and abbreviation.
	const char * pszVernWs = FwXml::GetAttributeValue(prgpszAtts, "wsv");
	int wsVern = 0;
	const char * pszVernName = NULL;
	const char * pszVernAbbr = NULL;
	if (pszVernWs != NULL)
	{
		// "Warning: Invalid writing system in <Link wsv=""%<0>s"" .../>.\n"
		wsVern = GetWsFromIcuLocale(pszVernWs, kstidXmlErrorMsg173);
		pszVernName = FwXml::GetAttributeValue(prgpszAtts, "namev");
		if (pszVernName != NULL)
		{
			pszVernAbbr = FwXml::GetAttributeValue(prgpszAtts, "abbrv");
			// Use the standard abbreviation for vernacular abbreviation if none provided.
			if (pszVernAbbr == NULL)
				pszVernAbbr = pszTargetAbbr;
		}
		else
		{
			pszVernWs = NULL;
		}
	}

	// Someday (when?), Field$.ListRootId will give us the list in which to look for the target
	// object for all list reference fields, not just custom fields.  Until then, we have to do
	// the best we can with explicitly coded knowledge here.
	int hobjList = m_pfwxd->FieldInfo(ifld).nListRootId;
	int clidItem = m_pfwxd->FieldInfo(ifld).cidDst;
	int flidList = GuessFlidOfList(m_pfwxd->FieldInfo(ifld).fid);
	if (hobjList == 0)
	{
		// The flidList must be for an atomic valued owning field, probably owned by either
		// LangProject or LexDb.
		if (flidList != 0)
		{
			hobjList = FindHobjForListFlid(flidList);
		}
		if (hobjList == 0)
		{
			// "Invalid field for implicit target in Link element: %<0>S."
			StrAnsi staFmt(kstidXmlErrorMsg158);
			StrAnsi sta;
			sta.Format(staFmt.Chars(), m_pfwxd->FieldXmlName(ifld).Chars());
			LogMessage(sta.Chars());
			return true;
		}
	}

	int hobjTarget = FindOrCreateCmPossibility(hobjList, ws, clidItem, pszTargetName,
		pszTargetAbbr, wsVern, pszVernName, pszVernAbbr);
	StoreReference(prgpszAtts, hobjOwner, ifld, hobjTarget);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Return the list owner's flid based on a list reference's flid.

	@param flid Field id of a list reference field.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GuessFlidOfList(int flidRef)
{
	/*
	  other CmPossibilityList objects owned by LexDb or LangProject:
	  ------------------------------------------------------------------------------------
	  kflidLangProject_AffixCategories
	  kflidLangProject_AnalysisStatus
	  kflidLangProject_CheckLists
	  kflidLangProject_ConfidenceLevels
	  kflidLangProject_Education
	  kflidLangProject_People
	  kflidLangProject_Positions
	  kflidLangProject_Restrictions
	  kflidLangProject_Roles
	  kflidLangProject_Thesaurus
	  kflidLangProject_TimeOfDay
	  kflidLangProject_WeatherConditions

	  kflidLexDb_References (handled differently from other lists)
	  kflidLexDb_SubentryTypes
	*/
	switch (flidRef)
	{
	case kflidMoStemMsa_PartOfSpeech:
	case kflidMoDerivAffMsa_FromPartOfSpeech:
	case kflidMoDerivAffMsa_ToPartOfSpeech:
	case kflidMoUnclassifiedAffixMsa_PartOfSpeech:
	case kflidMoInflAffMsa_PartOfSpeech:
	case kflidWfiAnalysis_Category:
		return kflidLangProject_PartsOfSpeech;

	case kflidLexSense_SenseType:
		return kflidLexDb_SenseTypes;

	case kflidLexSense_Status:
		return kflidLexDb_Status;		// ??

	case kflidMoForm_MorphType:
		return kflidLexDb_MorphTypes;

	case kflidLexSense_AnthroCodes:
		return kflidLangProject_AnthroList;

	case kflidLexSense_DomainTypes:
		return kflidLexDb_DomainTypes;

	case kflidLexSense_UsageTypes:
		return kflidLexDb_UsageTypes;

	case kflidLexSense_SemanticDomains:
		return kflidLangProject_SemanticDomainList;

	case kflidCmTranslation_Type:
		return kflidLangProject_TranslationTags;

	case kflidLexEntryRef_VariantEntryTypes:
		return kflidLexDb_VariantEntryTypes;

	case kflidLexEntryRef_ComplexEntryTypes:
		return kflidLexDb_ComplexEntryTypes;

	case kflidCmAnnotation_AnnotationType:
		return kflidLangProject_AnnotationDefs;

	case kflidLexPronunciation_Location:
		return kflidLangProject_Locations;

	default:
		return 0;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle a link element which lacks a target attribute, but which has a ws attribute and
	either an entry attribute or a sense attribute (but not both).  This set of attributes
	implies a possible EntryOrSense link, at least if the information from FieldInfo(ifld)
	agrees.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param ifld Index into m_vfdfi and m_vstufld for the field of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ws The writing system for interpreting entry or sense.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::StoreEntryOrSenseLinkInfo(const XML_Char * pszName,
	const XML_Char ** prgpszAtts, int hobjOwner, int ifld, int icls, int ws)
{
	const char * pszTargetEntry = FwXml::GetAttributeValue(prgpszAtts, "entry");
	const char * pszTargetSense = FwXml::GetAttributeValue(prgpszAtts, "sense");
	if (!pszTargetEntry && !pszTargetSense)
	{
		return false;
	}
	int clid = m_pfwxd->ClassInfo(icls).cid;
	int flid = m_pfwxd->FieldInfo(ifld).fid;
	if (clid != kclidLexEntryRef)
	{
		// "Invalid class for implicit target in Link element: %<0>S."
		StrAnsi staFmt(kstidXmlErrorMsg172);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), m_pfwxd->ClassName(icls).Chars());
		LogMessage(sta.Chars());
		return true;
	}
	if (flid != kflidLexEntryRef_ComponentLexemes && flid != kflidLexEntryRef_PrimaryLexemes)
	{
		// "Invalid field for implicit target in Link element: %<0>S."
		StrAnsi staFmt(kstidXmlErrorMsg158);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), m_pfwxd->FieldXmlName(ifld).Chars());
		LogMessage(sta.Chars());
		return true;
	}
	EntryOrSenseLinkInfo mesi;
	mesi.m_hobj = hobjOwner;
	mesi.m_ws = ws;
	mesi.m_flid = flid;
	if (pszTargetEntry)
	{
		mesi.m_fSense = false;
		mesi.m_staEntry = pszTargetEntry;
	}
	else
	{
		mesi.m_fSense = true;
		mesi.m_staEntry = pszTargetSense;
	}
	mesi.m_hvoTarget = 0;
	m_vmesi.Push(mesi);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle a link element which lacks a target attribute, but which has a form attribute and not
	a ws attribute.  The form attribute implies a possible MoAffixAllomorph or MoStemAllomorph
	object, at least if the information from FieldInfo(ifld) agrees.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param ifld Index into m_vfdfi and m_vstufld for the field of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param pszForm Possibly the StringRepresentation of a PhEnvironment
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::StorePhoneEnvReference(const XML_Char * pszName,
	const XML_Char ** prgpszAtts, int hobjOwner, int ifld, int icls, const char * pszForm)
{
	int hobjPhonData = m_pfwxd->FieldInfo(ifld).nListRootId;
	int clidItem = m_pfwxd->FieldInfo(ifld).cidDst;
	int flidList = 0;

	switch (m_pfwxd->FieldInfo(ifld).fid)
	{
	case kflidMoAffixAllomorph_PhoneEnv:
	case kflidMoStemAllomorph_PhoneEnv:
		flidList = kflidLangProject_PhonologicalData;
		break;
	}
	if (hobjPhonData == 0)
	{
		if (flidList != 0)
			hobjPhonData = FindHobjForListFlid(flidList);
		if (hobjPhonData == 0)
		{
			// "Invalid field for implicit target in Link element: %<0>S."
			StrAnsi staFmt(kstidXmlErrorMsg158);
			StrAnsi sta;
			sta.Format(staFmt.Chars(), m_pfwxd->FieldXmlName(ifld).Chars());
			LogMessage(sta.Chars());
			return true;
		}
	}
	int hobjTarget = FindOrCreatePhEnvironment(hobjPhonData, clidItem, pszForm);
	StoreReference(prgpszAtts, hobjOwner, ifld, hobjTarget);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Find or create a PhEnvironment object based on the arguments.

	@param hobjList the database object id of the CmPossibilityList to which the desired
					CmPossibility belongs
	@param clidItem specific class id of the target object (may be subclass of CmPossibility)
	@param pszForm StringRepresentation of a PhEnvironment
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::FindOrCreatePhEnvironment(int hobjList, int clidItem, const char * pszForm)
{
	int hobj;
	StrAnsiBuf stab;
	SqlStatement sstmt;
	RETCODE rc;
	wchar rgchName[4001];
	SDWORD cbName;

	// if the HashMap is empty, initialize it from the database.
	if (m_hmsuPhEnvId.Size() == 0)
	{
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stab.Assign("SELECT Id, StringRepresentation FROM PhEnvironment;");
		}
		sstmt.Init(m_sdb);
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		while (rc == SQL_SUCCESS)
		{
			SDWORD cbHobj;
			rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbHobj);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			rc = SQLGetData(sstmt.Hstmt(), 2, SQL_C_WCHAR, rgchName, isizeof(rgchName),
				&cbName);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			if (cbName !=  SQL_NULL_DATA && cbName != 0)
			{
				StrUni stu(rgchName, cbName/sizeof(wchar));
				m_hmsuPhEnvId.Insert(stu, hobj, true);
			}
			rc = SQLFetch(sstmt.Hstmt());
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		}
		sstmt.Clear();
	}
	StrUni stuForm;
	StrUtil::StoreUtf16FromUtf8(pszForm, strlen(pszForm), stuForm, false);
	if (m_hmsuPhEnvId.Retrieve(stuForm, &hobj))
		return hobj;

	// it doesn't exist, we have to create it.
	SQLINTEGER cchTxt;
	SQLINTEGER cbTxt;
	SQLINTEGER cbTxtForm;
	sstmt.Init(m_sdb);
	int hobjTarget = GenerateNextNewHvo();
	if(CURRENTDB == FB) {
		stab.Format("EXECUTE PROCEDURE CreateOwnedObject$ %u,%u,null,%u,%u,%u;",
			clidItem, hobjTarget, hobjList, kflidPhPhonData_Environments,
			kcptOwningSequence);
	}
	if(CURRENTDB == MSSQL) {
		stab.Format("EXEC CreateOwnedObject$ %u,%u,null,%u,%u,%u;",
			clidItem, hobjTarget, hobjList, kflidPhPhonData_Environments,
			kcptOwningSequence);
	}
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
	sstmt.Clear();
	sstmt.Init(m_sdb);
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		stab.Format("UPDATE PhEnvironment SET StringRepresentation=? WHERE id=%u;", hobjTarget);
	}
	cchTxt = stuForm.Length();
	cbTxt = cchTxt * isizeof(wchar);
	cbTxtForm = cbTxt;
	rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
		cchTxt, 0, const_cast<wchar *>(stuForm.Chars()), cbTxt + isizeof(wchar), &cbTxtForm);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
	sstmt.Clear();
	m_hmsuPhEnvId.Insert(stuForm, hobjTarget);
	return hobjTarget;
}

/*----------------------------------------------------------------------------------------------
	Handle a link element which lacks a target attribute, but which has a sense or entry
	attribute and a name or abbr attribute.

	This set of attributes implies either a lexical relation or a cross reference link, which
	is a back reference type property.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param ifld Index into m_vfdfi and m_vstufld for the field of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::StoreLexicalRelationInfo(const XML_Char * pszName,
	const XML_Char ** prgpszAtts, int hobjOwner, int ifld, int icls)
{
	LexRelationInfo lri;
	const char * psz = FwXml::GetAttributeValue(prgpszAtts, "sense");
	if (psz)
	{
		lri.m_fSense = true;
		lri.m_staSense = psz;
	}
	else
	{
		psz = FwXml::GetAttributeValue(prgpszAtts, "entry");
		if (psz)
		{
			lri.m_fSense = false;
			lri.m_staSense = psz;
		}
		else
		{
			//"Implicit %<0>s target in a Link element is missing the sense or entry attribute."
			StrAnsi staFmt(kstidXmlErrorMsg170);
			StrAnsi sta;
			sta.Format(staFmt.Chars(), pszName);
			LogMessage(sta.Chars());
			return true;
		}
	}
	psz = FwXml::GetAttributeValue(prgpszAtts, "wsv");
	if (psz)
		lri.m_wsv = GetWsFromIcuLocale(psz, kstidXmlErrorMsg157);
	else
		lri.m_wsv = DefaultVernacularWritingSystem();

	int wsa;
	psz = FwXml::GetAttributeValue(prgpszAtts, "wsa");
	if (psz)
		wsa = GetWsFromIcuLocale(psz, kstidXmlErrorMsg157);
	else
		wsa = DefaultAnalysisWritingSystem();

	const char * pszTypeName = FwXml::GetAttributeValue(prgpszAtts, "name");
	const char * pszTypeAbbr = FwXml::GetAttributeValue(prgpszAtts, "abbr");

	lri.m_hvoLexRefType = FindOrCreateLexicalReferenceType(pszTypeName, pszTypeAbbr, wsa,
		pszName, lri.m_fSense, &lri.m_fReverse);

	if (lri.m_hvoLexRefType == 0)
	{
			// "Implicit %<0>s target in a Link element is missing the name or abbr attribute."
			StrAnsi staFmt(kstidXmlErrorMsg171);
			StrAnsi sta;
			sta.Format(staFmt.Chars(), pszName);
			LogMessage(sta.Chars());
			return true;
	}

	lri.m_hobj = hobjOwner;

	// Set the ord value (even though it may be meaningless...)
	lri.m_ord = 1;
	if (m_vlri.Size())
	{
		LexRelationInfo & lriTop = m_vlri[m_vlri.Size() - 1];
		if (lriTop.m_hobj == lri.m_hobj &&
			lriTop.m_hvoLexRefType == lri.m_hvoLexRefType &&
			lriTop.m_fReverse == lri.m_fReverse)
		{
			lri.m_ord = lriTop.m_ord + 1;
		}
	}
	lri.m_hvoTarget = 0;
	lri.m_plriNext = NULL;
	m_vlri.Push(lri);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Find the LexicalReferenceType whose name or abbreviation (or reverse name or reverse abbr)
	is given by pszTypeName or pszTypeAbbr. If nothing can be found, then create a new one.

	@param pszTypeName XML attribute value for "name" (may be NULL)
	@param pszTypeAbbr XML attribute value for "abbr" (may be NULL)
	@param wsa The analysis writing system database id
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::FindOrCreateLexicalReferenceType(const char * pszTypeName,
	const char * pszTypeAbbr, int wsa, const XML_Char * pszName, bool fSense, bool * pfReverse)
{
	int cName = pszTypeName == NULL ? 0 : strlen(pszTypeName);
	int cAbbr = pszTypeAbbr == NULL ? 0 : strlen(pszTypeAbbr);

	if (cName == 0 && cAbbr == 0)
		return 0;

	if (wsa == 0)
		return 0;

	*pfReverse = false;

	StrUni stuName;
	if (cName)
	{
		StrUtil::StoreUtf16FromUtf8(pszTypeName, cName, stuName);
//		StrUtil::ToLower(stuName);
	}
	StrUni stuAbbr;
	if (cAbbr)
	{
		StrUtil::StoreUtf16FromUtf8(pszTypeAbbr, cAbbr, stuAbbr);
//		StrUtil::ToLower(stuAbbr);
	}
	int hvoType = FindLexicalReferenceType(wsa, stuName, stuAbbr, pfReverse);
	if (hvoType == 0)
	{
		// we can't find it, so we'll create it.
		hvoType = CreateLexicalReferenceType(stuName, stuAbbr, wsa, fSense);
	}
	return hvoType;
}


/*----------------------------------------------------------------------------------------------
	Use the name and abbreviation to find the database id of the Lexical Reference Type.

	@param wsa The analysis writing system database id.
	@param stuName name of the lexical reference type (may be empty).
	@param stuAbbr abbreviation of the lexical reference type (may be empty).
	@param pfReverse pointer to flag whether this is a reverse lexical reference.

	@return The database id of the Lexical Reference Type found, or 0 if not found.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::FindLexicalReferenceType(int wsa, StrUni stuName, StrUni stuAbbr,
	bool * pfReverse)
{
	EnsureLRHashMapsFull(wsa);
	int hvoType = 0;
	if (stuName.Length())
	{
		StrUtil::ToLower(stuName);

		if (!m_hmsuNameHvoLRType.Retrieve(stuName, &hvoType))
		{
			if (!m_hmsuRevNameHvoLRType.Retrieve(stuName, &hvoType))
				hvoType = 0;
			else
				*pfReverse = true;
		}
	}
	if (hvoType == 0 && stuAbbr.Length())
	{
		StrUtil::ToLower(stuAbbr);

		if (!m_hmsuAbbrHvoLRType.Retrieve(stuAbbr, &hvoType))
		{
			if (!m_hmsuRevAbbrHvoLRType.Retrieve(stuAbbr, &hvoType))
				hvoType = 0;
			else
				*pfReverse = true;
		}
	}
	if (hvoType == 0 && stuName.Length())
	{
		if (!m_hmsuAbbrHvoLRType.Retrieve(stuName, &hvoType))
		{
			if (!m_hmsuRevAbbrHvoLRType.Retrieve(stuName, &hvoType))
				hvoType = 0;
			else
				*pfReverse = true;
		}
	}
	if (hvoType == 0 && stuAbbr.Length())
	{
		if (!m_hmsuNameHvoLRType.Retrieve(stuAbbr, &hvoType))
		{
			if (!m_hmsuRevNameHvoLRType.Retrieve(stuAbbr, &hvoType))
				hvoType = 0;
			else
				*pfReverse = true;
		}
	}
	return hvoType;
}


/*----------------------------------------------------------------------------------------------
	Ensure that all the Lexical Reference Type related hashmaps are loaded from the database.

	@param wsa The analysis writing system database id
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::EnsureLRHashMapsFull(int wsa)
{
	if (m_hmsuNameHvoLRType.Size() == 0)
	{
		const char * pszTable = (m_pfwxd->DbVersion() <= 200202) ?
			"LexReferenceType" : "LexRefType";
		StrAnsiBuf stab;
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stab.Format("SELECT n.Obj, n.Txt "
				"FROM CmPossibility_Name n "
				"JOIN %s t on t.Id = n.Obj "
				"WHERE n.Ws = %d;", pszTable, wsa);
			FillHashMapNameToHvo(stab.Chars(), m_hmsuNameHvoLRType);
			stab.Format("SELECT n.Obj, n.Txt "
				"FROM CmPossibility_Abbreviation n "
				"JOIN %s t on t.Id = n.Obj "
				"WHERE n.Ws = %d;", pszTable, wsa);
			FillHashMapNameToHvo(stab.Chars(), m_hmsuAbbrHvoLRType);
			stab.Format("SELECT Obj, Txt "
				"FROM %s_ReverseName "
				"WHERE Ws = %d;", pszTable, wsa);
			FillHashMapNameToHvo(stab.Chars(), m_hmsuRevNameHvoLRType);
			stab.Format("SELECT Obj, Txt "
				"FROM %s_ReverseAbbreviation "
				"WHERE Ws = %d;", pszTable, wsa);
			FillHashMapNameToHvo(stab.Chars(), m_hmsuRevAbbrHvoLRType);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Create a new Lexical Reference Type object, using the provided information.

	@param stuName Name of the lexical reference type
	@param stuAbbr Abbreviation of the lexical reference type
	@param wsa The analysis writing system database id
	@param fSense Flag whether looking at a Sense or Entry.

	@return The database id of the newly created Lexical Reference Type.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::CreateLexicalReferenceType(StrUni & stuName, StrUni & stuAbbr, int wsa,
	bool fSense)
{
	if (stuName.Length() == 0)
		stuName = stuAbbr;
	else if (stuAbbr.Length() == 0)
		stuAbbr = stuName;
	int hvoType = GenerateNextNewHvo();
	GUID guid;
	GenerateNewGuid(&guid);

	int nType = fSense ? 1 : 6; // 1 = Sense pair, 6 = Entry pair
	int nDbVer = m_pfwxd->DbVersion();
	const char * pszLexDb = (nDbVer <= 200202) ? "LexicalDatabase_References" : "LexDb_References";
	const char * pszLexRef = (nDbVer <= 200202) ? "LexReferenceType" : "LexRefType";
	StrAnsi staCmd;
	//TODO (steve miller): correct assignments to variables for firebird
	if(CURRENTDB == FB) {
		staCmd.Format("DECLARE VARIABLE hvoList INT; DECLARE VARIABLE nextOrd INT; %n"
			"SELECT Dst FROM %<6>s INTO :hvoList; %n"
			"SELECT MAX(OwnOrd$)+1 FROM CmObject WHERE Owner$=hvoList AND Class$=%<0>u AND OwnFlid$=%<3>u INTO :nextOrd; %n"
			"EXECUTE PROCEDURE CreateObject$ %<0>u, %<1>u, '%<2>g'; %n"
			"UPDATE CmObject SET Owner$=hvoList, OwnFlid$=%<3>u, OwnOrd$=nextOrd WHERE Id=%<1>u; %n"
			"UPDATE %<7>s SET MappingType=%<4>u WHERE Id=%<1>u; %n"
			"INSERT INTO CmPossibility_Name (Obj, Ws, Txt) VALUES (%<1>u, %<5>u, ?); %n"
			"INSERT INTO CmPossibility_Abbreviation (Obj, Ws, Txt) VALUES (%<1>u, %<5>u, ?);",
			kclidLexRefType, hvoType, &guid, kflidCmPossibilityList_Possibilities, nType,
			wsa, pszLexDb, pszLexRef);
	}
	else if(CURRENTDB == MSSQL) {
		staCmd.Format("DECLARE @hvoList int, @nextOrd int; %n"
			"SELECT @hvoList=Dst FROM %<6>s; %n"
			"SELECT @nextOrd=MAX(OwnOrd$)+1 FROM CmObject WHERE Owner$=@hvoList AND Class$=%<0>u AND OwnFlid$=%<3>u; %n"
			"EXEC CreateObject$ %<0>u, %<1>u, '%<2>g'; %n"
			"UPDATE CmObject SET Owner$=@hvoList, OwnFlid$=%<3>u, OwnOrd$=@nextOrd WHERE Id=%<1>u; %n"
			"UPDATE %<7>s SET MappingType=%<4>u WHERE Id=%<1>u; %n"
			"INSERT INTO CmPossibility_Name (Obj, Ws, Txt) VALUES (%<1>u, %<5>u, ?); %n"
			"INSERT INTO CmPossibility_Abbreviation (Obj, Ws, Txt) VALUES (%<1>u, %<5>u, ?);",
			kclidLexRefType, hvoType, &guid, kflidCmPossibilityList_Possibilities, nType,
			wsa, pszLexDb, pszLexRef);
	}

	SQLINTEGER cchTxt;
	SQLINTEGER cbTxt;
	SQLINTEGER cbTxtLine;
	SQLINTEGER cbTxtName;
	SQLINTEGER cbTxtAbbr;
	SqlStatement sstmt;
	RETCODE rc;
	sstmt.Init(m_sdb);

	// Set parameters for Name and Abbreviation strings
	cchTxt = stuName.Length();
	cbTxt = cchTxt * isizeof(wchar);
	cbTxtLine = cbTxt + isizeof(wchar);
	cbTxtName = cbTxt;
	rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
		cchTxt, 0, const_cast<wchar *>(stuName.Chars()), cbTxtLine, &cbTxtName);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

	cchTxt = stuAbbr.Length();
	cbTxt = cchTxt * isizeof(wchar);
	cbTxtLine = cbTxt + isizeof(wchar);
	cbTxtAbbr = cbTxt;
	rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
		cchTxt, 0, const_cast<wchar *>(stuAbbr.Chars()), cbTxtLine, &cbTxtAbbr);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

	// Execute SQL
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, staCmd.Chars());
	sstmt.Clear();

	// Report this to the user via the log file.
	StrAnsi staFmt(kstidXmlInfoMsg311);
	StrAnsi sta;
	StrAnsi staType;
	if (fSense)
		staType.Load(kstidSense);
	else
		staType.Load(kstidEntry);
	sta.Format(staFmt.Chars(), staType.Chars(), stuName.Chars(), stuAbbr.Chars());
	LogMessage(sta.Chars());

	StrUtil::ToLower(stuName);
	StrUtil::ToLower(stuAbbr);
	m_hmsuNameHvoLRType.Insert(stuName, hvoType);
	m_hmsuAbbrHvoLRType.Insert(stuAbbr, hvoType);

	return hvoType;
}


/*----------------------------------------------------------------------------------------------
	Generate a new guid for use in the database.

	@param pguid Pointer to a guid that receives the new value.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::GenerateNewGuid(GUID * pguid)
{
	HRESULT hr = ::CoCreateGuid(pguid);
	if (FAILED(hr))
	{
		// "Cannot create GUID for object identifier!"
		StrAnsi sta(kstidXmlErrorMsg012);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(hr));
	}
}

/*----------------------------------------------------------------------------------------------
	Fill a hashmap from the database which maps a name string to the database id.

	@param pszSQL The SQL command to execute.
	@param hmsuNameHvo Reference to the hashmap to fill.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::FillHashMapNameToHvo(const char * pszSQL,
	HashMapStrUni<int> & hmsuNameHvo)
{
	SqlStatement sstmt;
	RETCODE rc;
	int hobj = 0;
	wchar rgch[4001];
	SDWORD cbhobj;
	SDWORD cbName;
	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pszSQL)), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pszSQL);
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbhobj);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_WCHAR, &rgch, isizeof(rgch), &cbName);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbhobj == SQL_NULL_DATA || !cbhobj)
			continue;
		if (cbName && cbName != SQL_NULL_DATA)
		{
			StrUni stu(rgch, cbName/sizeof(wchar));
			StrUtil::ToLower(stu);
			hmsuNameHvo.Insert(stu, hobj, true);
		}
	}
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Handle a link element which lacks a target attribute, but which has a ws attribute and
	a name attribute, and either an abbrOwner attribute or a nameOwner attribute (or both).
	This set of attributes implies a possible MoInflAffixSlot object, at least if the
	information from FieldInfo(ifld) agrees.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param ifld Index into m_vfdfi and m_vstufld for the field of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ws Database id of the desired Writing system.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::StoreMoInflAffixSlotReference(const XML_Char * pszName,
	const XML_Char ** prgpszAtts, int hobjOwner, int ifld, int icls, int ws)
{
	if (m_pfwxd->FieldInfo(ifld).fid != kflidMoInflAffMsa_Slots)
		return false;

	const char * pszTargetName = FwXml::GetAttributeValue(prgpszAtts, "name");
	const char * pszTargetAbbrPos = FwXml::GetAttributeValue(prgpszAtts, "abbrOwner");
	const char * pszTargetNamePos = FwXml::GetAttributeValue(prgpszAtts, "nameOwner");
	int hobjPos = GetPartOfSpeechForMoInflAffixSlot(pszTargetName, pszTargetAbbrPos,
		pszTargetNamePos, ws);
	if (hobjPos == 0)
		return true;

	// Now we have to find (or create) the desired MoInflAffixSlot belonging to that
	// PartOfSpeech given by the name attribute.

	StrUni stuName;
	StrUtil::StoreUtf16FromUtf8(pszTargetName, strlen(pszTargetName), stuName);
	Assert(stuName.Length());
	StrUni stuNameLow(stuName);
	StrUtil::ToLower(stuNameLow);
	ListInfo li;	// overkill without abbreviation or list name, but saves writing more code.
	int hobjTarget = FindTargetMoInflAffixSlot(hobjPos, ws, stuNameLow, li);
	if (hobjTarget == 0)
	{
		// Nothing matched, create a new affix slot item in the database, and add its id to the
		// hashmap.
		hobjTarget = CreateMoInflAffixSlot(hobjPos, stuName, ws);
		li.m_phmsuNamehobj->Insert(stuNameLow, hobjTarget, true);
		// Log creating this item.
		// Info: Creating new inflectional affix slot with ws="%<0>S" and name="%<1>s",
		// for POS with abbr="%<2>s" and name="%<3>s".
		StrUni stuLangName;
		StrUni stuWs;
		GetWsNameAndLocale(ws, stuLangName, stuWs);
		StrAnsi staFmt(kstidXmlInfoMsg102);
		StrAnsi sta;
		sta.Format(staFmt.Chars(),
			stuWs.Chars(), pszTargetName ? pszTargetName : "",
			pszTargetAbbrPos ? pszTargetAbbrPos : "", pszTargetNamePos ? pszTargetNamePos : "");
		LogMessage(sta.Chars());
	}
	Assert(hobjTarget);
	// prevent the hashmap from being deleted by destructor since it is stored in m_hmlidliSlot.
	li.m_phmsuNamehobj = NULL;

	StoreReference(prgpszAtts, hobjOwner, ifld, hobjTarget);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Verify that the target name and Part Of Speech name and abbreviation are valid, then obtain
	the database id for the specified Part Of Speech object.

	@param pszTargetName name of the target slot (used only for validation)
	@param pszTargetAbbrPos name of the Part Of Speech which owns the slot
	@param pszTargetNamePos abbreviation of the Part of Speech which owns the slot
	@param ws Database id of the desired Writing system.

	@return database id of the specified Part Of Speech, or 0 if an error occurs
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetPartOfSpeechForMoInflAffixSlot(const char * pszTargetName,
	const char * pszTargetAbbrPos, const char * pszTargetNamePos, int ws)
{
	StrAnsi staFmt;
	StrAnsi sta;
	if (!pszTargetAbbrPos && !pszTargetNamePos)
	{
	// "Implicit MoInflAffixSlot target in a Link element is missing the nameOwner attribute."
		sta.Load(kstidXmlErrorMsg169);
		LogMessage(sta.Chars());
		return 0;
	}
	if (!pszTargetName)
	{
		// "Implicit MoInflAffixSlot target in a Link element is missing the name attribute."
		sta.Load(kstidXmlErrorMsg159);
		LogMessage(sta.Chars());
		return 0;
	}

	// First we have to find (or create) the desired PartOfSpeech given by the abbrOwner and/or
	// the nameOwner attributes.

	int hobjList = FindHobjForListFlid(kflidLangProject_PartsOfSpeech);
	if (hobjList == 0)
	{
		// "Implicit %<0>s target in a Link element cannot access the PartOfSpeech list."
		staFmt.Load(kstidXmlErrorMsg160);
		sta.Format(staFmt.Chars(), "MoInflAffixSlot");
		LogMessage(sta.Chars());
		return 0;
	}
	int hobjPos = FindOrCreateCmPossibility(hobjList, ws, kclidPartOfSpeech, pszTargetNamePos,
		pszTargetAbbrPos);
	if (hobjPos == 0)
	{
		// "Implicit %<0>s target in Link element cannot find/create a needed PartOfSpeech."
		staFmt.Load(kstidXmlErrorMsg161);
		sta.Format(staFmt.Chars(), "MoInflAffixSlot");
		LogMessage(sta.Chars());
	}
	return hobjPos;
}


/*----------------------------------------------------------------------------------------------
	Find the database id of the specified MoInflAffixSlot owned by the given Part Of Speech.

	@param hobjPos Database id of a Part Of Speech
	@param ws Database id of the desired Writing system.
	@param stuNameLow Lowercased name of the slot
	@param li reference to a ListInfo object containing PartOfSpeech info (output)
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::FindTargetMoInflAffixSlot(int hobjPos, int ws, StrUni & stuNameLow,
	ListInfo & li)
{
	ListIdentity lid = { hobjPos, ws };
	// Find the stored Name hashmap if it exists.
	if (!m_hmlidliSlot.Retrieve(lid, &li))
	{
		li.m_phmsuNamehobj = NewObj HashMapStrUni<int>;	// Instantiate the Name hashmap.
		li.m_phmsuAbbrhobj = NULL;
		// Create the Name hashmap, and fill it from the database.
		StrAnsiBufBig stab;
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stab.Format("SELECT co.Id, n.Txt%n"
				"FROM CmObject co%n"
				"JOIN MoInflAffixSlot_Name n on n.Obj = co.Id AND n.Ws = %<0>d%n"
				"WHERE co.Owner$ = %<1>d;",
				ws, hobjPos);
		}
		FillHashMapNameToHvo(stab.Chars(), *li.m_phmsuNamehobj);
		m_hmlidliSlot.Insert(lid, li);
	}

	// We've obtained the hashmap needed, now try to find the target object id.
	int hobjTarget;
	bool fOk = li.m_phmsuNamehobj->Retrieve(stuNameLow, &hobjTarget);
	if (fOk)
		return hobjTarget;
	else
		return 0;
}


/*----------------------------------------------------------------------------------------------
	Create a new MoInflAffixSlot since none with the given name and owner already exist.

	@param hobjPos Database id of a Part Of Speech (owner of the MoInflAffixSlot)
	@param stuName Name of the MoInflAffixSlot
	@param ws Database id of the desired Writing system.

	@return Database id of the newly created MoInflAffixSlot
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::CreateMoInflAffixSlot(int hobjPos, StrUni & stuName, int ws)
{
	SQLINTEGER cchTxt;
	SQLINTEGER cbTxt;
	SQLINTEGER cbTxtLine;
	SQLINTEGER cbTxtName;
	StrAnsiBuf stab;
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	int hobjTarget = GenerateNextNewHvo();
	if(CURRENTDB == FB) {
		stab.Format("EXECUTE PROCEDURE CreateOwnedObject$ %u,%u,null,%u,%u,%u%n;",
			kclidMoInflAffixSlot, hobjTarget, hobjPos, kflidPartOfSpeech_AffixSlots,
			kcptOwningCollection);
		stab.FormatAppend("INSERT INTO MoInflAffixSlot_Name (Obj,Ws,Txt) VALUES (%u,%u,?);",
			hobjTarget, ws);
	}
	if(CURRENTDB == MSSQL) {
		stab.Format("EXEC CreateOwnedObject$ %u,%u,null,%u,%u,%u%n;",
			kclidMoInflAffixSlot, hobjTarget, hobjPos, kflidPartOfSpeech_AffixSlots,
			kcptOwningCollection);
		stab.FormatAppend("INSERT INTO MoInflAffixSlot_Name (Obj,Ws,Txt) VALUES (%u,%u,?);",
			hobjTarget, ws);
	}
	cchTxt = stuName.Length();
	cbTxt = cchTxt * isizeof(wchar);
	cbTxtLine = cbTxt + isizeof(wchar);
	cbTxtName = cbTxt;
	RETCODE rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
		cchTxt, 0, const_cast<wchar *>(stuName.Chars()), cbTxtLine, &cbTxtName);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
	sstmt.Clear();
	return hobjTarget;
}


/*----------------------------------------------------------------------------------------------
	Handle a link element which lacks a target attribute, but which has a ws attribute and
	either an abbr attribute or a name attribute (or both), and either an abbrOwner attribute
	or a nameOwner attribute (or both). This set of attributes implies a possible
	MoInflClass object, at least if the information from FieldInfo(ifld) agrees.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param ifld Index into m_vfdfi and m_vstufld for the field of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ws Database id of the desired Writing system.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::StoreMoInflClassReference(const XML_Char * pszName,
	const XML_Char ** prgpszAtts, int hobjOwner, int ifld, int icls, int ws)
{
	switch (m_pfwxd->FieldInfo(ifld).fid)
	{
	case kflidMoStemMsa_InflectionClass:
	case kflidMoAffixForm_InflectionClasses:
	case kflidMoDerivAffMsa_ToInflectionClass:
	case kflidMoDerivAffMsa_FromInflectionClass:
	case kflidMoDerivStepMsa_InflectionClass:
		break;
	default:
		return false;
	}
	const char * pszTargetAbbr = FwXml::GetAttributeValue(prgpszAtts, "abbr");
	const char * pszTargetName = FwXml::GetAttributeValue(prgpszAtts, "name");
	const char * pszTargetAbbrPos = FwXml::GetAttributeValue(prgpszAtts, "abbrOwner");
	const char * pszTargetNamePos = FwXml::GetAttributeValue(prgpszAtts, "nameOwner");
	int hobjPos = GetPartOfSpeechForMoInflClass(pszTargetAbbr, pszTargetName,
		pszTargetAbbrPos, pszTargetNamePos, ws);
	if (hobjPos == 0)
		return true;

	StrUni stuName;
	StrUni stuNameLow;
	StrUni stuAbbr;
	StrUni stuAbbrLow;
	if (pszTargetName && *pszTargetName)
	{
		StrUtil::StoreUtf16FromUtf8(pszTargetName, strlen(pszTargetName), stuName);
		stuNameLow.Assign(stuName);
		StrUtil::ToLower(stuNameLow);
	}
	if (pszTargetAbbr && *pszTargetAbbr)
	{
		StrUtil::StoreUtf16FromUtf8(pszTargetAbbr, strlen(pszTargetAbbr), stuAbbr);
		stuAbbrLow.Assign(stuAbbr);
		StrUtil::ToLower(stuAbbrLow);
	}
	ListInfo li;
	int hobjTarget = FindTargetMoInflClass(hobjPos, ws, stuName, stuNameLow, stuAbbr,
		stuAbbrLow, li);
	if (hobjTarget == 0)
	{
		// Nothing matched, create a new inflection class item in the database, and add its id
		// to the hashmap.
		hobjTarget = CreateMoInflClass(hobjPos, stuName, stuAbbr, ws);
		if (stuNameLow.Length())
			li.m_phmsuNamehobj->Insert(stuNameLow, hobjTarget, true);
		if (stuAbbrLow.Length())
			li.m_phmsuAbbrhobj->Insert(stuAbbrLow, hobjTarget, true);

		// Log creating this item.
		// Info: Creating new inflection class with ws="%<0>S", abbr="%<1>s", and name="%<2>s".
		StrUni stuLangName;
		StrUni stuWs;
		GetWsNameAndLocale(ws, stuLangName, stuWs);
		StrAnsi staFmt;
		StrAnsi sta;
		staFmt.Load(kstidXmlInfoMsg101);
		sta.Format(staFmt.Chars(), stuWs.Chars(),
			pszTargetAbbr ? pszTargetAbbr : "", pszTargetName ? pszTargetName : "");
		LogMessage(sta.Chars());
	}
	Assert(hobjTarget);
	li.m_phmsuNamehobj = NULL;		// prevent being deleted by destructor.
	li.m_phmsuAbbrhobj = NULL;		// prevent being deleted by destructor.

	StoreReference(prgpszAtts, hobjOwner, ifld, hobjTarget);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Verify that the target and Part Of Speech name and abbreviation are valid, then obtain
	the database id for the specified Part Of Speech object.

	@param pszTargetAbbr abbreviation of the target class (used only for validation)
	@param pszTargetName name of the target class (used only for validation)
	@param pszTargetAbbrPos name of the Part Of Speech which owns the class
	@param pszTargetNamePos abbreviation of the Part of Speech which owns the class
	@param ws Database id of the desired Writing system.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetPartOfSpeechForMoInflClass(const char * pszTargetAbbr,
	const char * pszTargetName, const char * pszTargetAbbrPos, const char * pszTargetNamePos,
	int ws)
{
	StrAnsi staFmt;
	StrAnsi sta;
	const char * pszTable = (m_pfwxd->DbVersion() <= 200202) ?
		"MoInflectionClass" : "MoInflClass";

	if ((!pszTargetAbbr && !pszTargetName) || (!pszTargetAbbrPos && !pszTargetNamePos))
	{
		// "Implicit %<0>s target in Link element is missing one or more required attributes."
		staFmt.Load(kstidXmlErrorMsg162);
		sta.Format(staFmt.Chars(), pszTable);
		LogMessage(sta.Chars());
		return 0;
	}

	// First we have to find (or create) the desired PartOfSpeech given by the abbrOwner and/or
	// the nameOwner attributes.

	int hobjList = FindHobjForListFlid(kflidLangProject_PartsOfSpeech);
	if (hobjList == 0)
	{
		// "Implicit %<0>s target in a Link element cannot access the PartOfSpeech list."
		staFmt.Load(kstidXmlErrorMsg160);
		sta.Format(staFmt.Chars(), pszTable);
		LogMessage(sta.Chars());
		return 0;
	}
	int hobjPos = FindOrCreateCmPossibility(hobjList, ws, kclidPartOfSpeech, pszTargetNamePos,
		pszTargetAbbrPos);
	if (hobjPos == 0)
	{
		// "Implicit %<0>s target in Link element cannot find/create a needed PartOfSpeech."
		staFmt.Load(kstidXmlErrorMsg161);
		sta.Format(staFmt.Chars(), pszTable);
		LogMessage(sta.Chars());
	}
	return hobjPos;
}


/*----------------------------------------------------------------------------------------------
	Find the database id of the specified MoInflClass owned by the given Part Of Speech.

	@param hobjPos Database id of a Part Of Speech
	@param ws Database id of the desired Writing system.
	@param stuName name of the class
	@param stuNameLow Lowercased name of the class
	@param stuAbbr abbreviation of the class
	@param stuAbbrLow Lowercased abbreviation of the class
	@param li reference to a ListInfo object containing PartOfSpeech info (output)
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::FindTargetMoInflClass(int hobjPos, int ws, StrUni & stuName,
	StrUni & stuNameLow, StrUni & stuAbbr, StrUni & stuAbbrLow, ListInfo & li)
{
	StrAnsi staFmt;
	StrAnsi sta;
	const char * pszTable = (m_pfwxd->DbVersion() <= 200202) ?
		"MoInflectionClass" : "MoInflClass";
	// Find the stored Name hashmap if it exists.
	EnsureInflClassMapLoaded(hobjPos, ws, li);

	// We've obtained the hashmap needed, now try to find the target object id.
	int hobjTarget = 0;
	bool fOk = false;
	if (stuNameLow.Length())
		fOk = li.m_phmsuNamehobj->Retrieve(stuNameLow, &hobjTarget);
	if (!fOk)
		fOk = li.m_phmsuAbbrhobj->Retrieve(stuAbbrLow, &hobjTarget);
	// swap name and abbr and try again just in case.
	if (!fOk && stuAbbrLow.Length())
	{
		fOk = li.m_phmsuNamehobj->Retrieve(stuAbbrLow, &hobjTarget);
		if (fOk)
		{
			staFmt.Load(kstidXmlErrorMsg167);
			sta.Format(staFmt.Chars(), pszTable, stuAbbr.Chars());
			int cMore = 0;
			if (m_hmcMsgcMore.Retrieve(sta.Chars(), &cMore))
			{
				// Just increment the counter.
				++cMore;
				m_hmcMsgcMore.Insert(sta.Chars(), cMore, true);
			}
			else
			{
				m_hmcMsgcMore.Insert(sta.Chars(), cMore);
				LogMessage(sta.Chars());
			}
		}
	}
	if (!fOk && stuNameLow.Length())
	{
		fOk = li.m_phmsuAbbrhobj->Retrieve(stuNameLow, &hobjTarget);
		if (fOk)
		{
			staFmt.Load(kstidXmlErrorMsg168);
			sta.Format(staFmt.Chars(), pszTable, stuName.Chars());
			int cMore = 0;
			if (m_hmcMsgcMore.Retrieve(sta.Chars(), &cMore))
			{
				// Just increment the counter.
				++cMore;
				m_hmcMsgcMore.Insert(sta.Chars(), cMore, true);
			}
			else
			{
				m_hmcMsgcMore.Insert(sta.Chars(), cMore);
				LogMessage(sta.Chars());
			}
		}
	}
	if (fOk)
		return hobjTarget;
	else
		return 0;
	return hobjTarget;
}


/*----------------------------------------------------------------------------------------------
	Ensure that the MoInflClass information for the given Part Of Speech is loaded into
	memory, placing the information into the referenced ListInfo object.

	@param hobjPos Database id of a Part Of Speech
	@param ws Database id of the desired Writing system.
	@param li reference to a ListInfo object containing PartOfSpeech info (output)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::EnsureInflClassMapLoaded(int hobjPos, int ws, ListInfo & li)
{
	ListIdentity lid = { hobjPos, ws };
	if (!m_hmlidliInflClass.Retrieve(lid, &li))
	{
		// Create the Name hashmap, and fill it from the database.
		StrAnsiBufBig stab;
		const char * pszTable = (m_pfwxd->DbVersion() <= 200202) ?
			"MoInflectionClass" : "MoInflClass";
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stab.Format("SELECT co.Id, n.Txt, a.Txt%n"
				"FROM CmObject co%n"
				"LEFT OUTER JOIN %<2>s_Name n ON n.Obj=co.Id AND n.Ws=%<0>d%n"
				"LEFT OUTER JOIN %<2>s_Abbreviation a ON a.Obj=co.Id AND a.ws=%<0>d%n"
				"WHERE (n.Txt IS NOT NULL OR a.Txt IS NOT NULL) AND co.Owner$ = %<1>d%n;",
				ws, hobjPos, pszTable);
		}
		int hobj = 0;
		wchar rgchName[4001];
		wchar rgchAbbr[4001];
		SDWORD cbhobj;
		SDWORD cbName;
		SDWORD cbAbbr;
		SqlStatement sstmt;
		sstmt.Init(m_sdb);
		RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
		rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbhobj);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_WCHAR, &rgchName, isizeof(rgchName), &cbName);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_WCHAR, &rgchAbbr, isizeof(rgchAbbr), &cbAbbr);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		li.Init();	// Instantiate the hashmaps.
		for (;;)
		{
			rc = SQLFetch(sstmt.Hstmt());
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			if (rc == SQL_NO_DATA)
				break;
			if (rc != SQL_SUCCESS)
				ThrowHr(WarnHr(E_UNEXPECTED));
			if (cbhobj == SQL_NULL_DATA || !cbhobj)
				continue;
			if (cbName && cbName != SQL_NULL_DATA)
			{
				StrUni stu(rgchName, cbName/sizeof(wchar));
				StrUtil::ToLower(stu);
				li.m_phmsuNamehobj->Insert(stu, hobj, true);
			}
			if (cbAbbr && cbAbbr != SQL_NULL_DATA)
			{
				StrUni stu(rgchAbbr, cbAbbr/sizeof(wchar));
				StrUtil::ToLower(stu);
				li.m_phmsuAbbrhobj->Insert(stu, hobj, true);
			}
		}
		sstmt.Clear();
		m_hmlidliInflClass.Insert(lid, li);
	}
}


/*----------------------------------------------------------------------------------------------
	Create a new MoInflClass since none with the given name (or abbreviation) and owner
	already exist.

	@param hobjPos Database id of a Part Of Speech (owner of the MoInflClass)
	@param stuName Name of the MoInflClass
	@param stuAbbr Abbreviation of the MoInflClass
	@param ws Database id of the desired Writing system.

	@return Database id of the newly created MoInflClass
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::CreateMoInflClass(int hobjPos, StrUni & stuName, StrUni & stuAbbr,
	int ws)
{
	SQLINTEGER cchTxt;
	SQLINTEGER cbTxt;
	SQLINTEGER cbTxtLine;
	SQLINTEGER cbTxtName;
	SQLINTEGER cbTxtAbbr;
	StrAnsiBuf stab;
	SqlStatement sstmt;
	RETCODE rc;
	sstmt.Init(m_sdb);
	int hobjTarget = GenerateNextNewHvo();
	if(CURRENTDB == FB) {
		stab.Format("EXECUTE PROCEDURE CreateOwnedObject$ %u,%u,null,%u,%u,%u%n;",
			kclidMoInflClass, hobjTarget, hobjPos, kflidPartOfSpeech_InflectionClasses,
			kcptOwningCollection);
	}
	if(CURRENTDB == MSSQL) {
		stab.Format("EXEC CreateOwnedObject$ %u,%u,null,%u,%u,%u%n;",
			kclidMoInflClass, hobjTarget, hobjPos, kflidPartOfSpeech_InflectionClasses,
			kcptOwningCollection);
	}
	SQLUSMALLINT icol = 1;
	const char * pszTable = (m_pfwxd->DbVersion() <= 200202) ? "MoInflectionClass" : "MoInflClass";
	if (stuName.Length())
	{
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stab.FormatAppend(
				"INSERT INTO %s_Name (Obj,Ws,Txt) VALUES (%u,%u,?)%n;",
					pszTable, hobjTarget, ws);
		}
		cchTxt = stuName.Length();
		cbTxt = cchTxt * isizeof(wchar);
		cbTxtLine = cbTxt + isizeof(wchar);
		cbTxtName = cbTxt;
		rc = SQLBindParameter(sstmt.Hstmt(), icol, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
			cchTxt, 0, const_cast<wchar *>(stuName.Chars()), cbTxtLine, &cbTxtName);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		++icol;
	}
	if (stuAbbr.Length())
	{
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stab.FormatAppend(
				"INSERT INTO %s_Abbreviation (Obj,Ws,Txt) VALUES (%u,%u,?);",
					pszTable, hobjTarget, ws);
		}
		cchTxt = stuAbbr.Length();
		cbTxt = cchTxt * isizeof(wchar);
		cbTxtLine = cbTxt + isizeof(wchar);
		cbTxtAbbr = cbTxt;
		rc = SQLBindParameter(sstmt.Hstmt(), icol, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
			cchTxt, 0, const_cast<wchar *>(stuAbbr.Chars()), cbTxtLine, &cbTxtAbbr);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		++icol;
	}
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
	sstmt.Clear();
	return hobjTarget;
}


/*----------------------------------------------------------------------------------------------
	Handle a link element which lacks a target attribute, but which has a ws attribute and
	a name attribute.  This set of attributes implies a possible ReversalIndexEntry object, at
	least if the information from FieldInfo(ifld) agrees.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param ifld Index into m_vfdfi and m_vstufld for the field of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ws Database id of the desired Writing system.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::StoreReversalEntry(const XML_Char * pszName,
	const XML_Char ** prgpszAtts, int hobjOwner, int ifld, int icls, int ws)
{
	if (m_pfwxd->FieldInfo(ifld).fid != kflidLexSense_ReversalEntries)
		return false;

	const char * pszForm = FwXml::GetAttributeValue(prgpszAtts, "form");
	int hobjIndex = GetReversalIndex(pszForm, ws);
	if (hobjIndex == 0)
		return true;

	// Now we have to find (or create) the ReversalIndexEntry given by the form attribute.

	ListInfo li;	// overkill without abbreviation or list name, but saves writing more code.
	StrUni stuName;
	StrUtil::StoreUtf16FromUtf8(pszForm, strlen(pszForm), stuName);
	Assert(stuName.Length());
	Vector<StrUni> vstuForm;
	wchar_t * pszNext;
	const wchar_t * pszSep = NULL;
	if (stuName.FindCh(L':') >= 0)
	{
		pszSep = L":";
	}
	else if (stuName.FindCh(L'|') >= 0)
	{
		pszSep = L"|";
	}
	if (pszSep != NULL)
	{
		wchar_t * pszTok = wcstok_s(const_cast<wchar_t *>(stuName.Chars()), pszSep, &pszNext);
		while (pszTok != NULL)
		{
			StrUni stu;
			StrUtil::TrimWhiteSpace(pszTok, stu);
			if (stu.Length() > 0)
				vstuForm.Push(stu);
			pszTok = wcstok_s(NULL, pszSep, &pszNext);
		}
		if (vstuForm.Size() == 0)
		{
			// "Implicit ReversalIndexEntry target in a Link element is missing the form attribute."
			StrAnsi sta(kstidXmlErrorMsg163);
			LogMessage(sta.Chars());
			return true;
		}
		stuName.Clear();
		for (int i = 0; i < vstuForm.Size(); ++i)
		{
			if (i > 0)
				stuName.Append("|");
			stuName.Append(vstuForm[i]);
		}
	}
	StrUni stuNameLow(stuName);
	StrUtil::ToLower(stuNameLow);
	int hobjTarget = FindTargetReversalIndexEntry(hobjIndex, stuNameLow, ws, li);
	if (hobjTarget == 0)
	{
		// Nothing matched, create a new ReversalIndexEntry in the database, and add its id to
		// the hashmap.
		hobjTarget = CreateReversalEntry(hobjIndex, stuName, ws, li);
	}

	li.m_phmsuNamehobj = NULL;	// prevent being deleted by destructor when this method ends.

	if (hobjTarget == 0)
	{
		// "Implicit ReversalIndexEntry target in a Link element has an invalid form attribute."
		StrAnsi sta(kstidXmlErrorMsg165);
		LogMessage(sta.Chars());
	}
	else
	{
		StoreReference(prgpszAtts, hobjOwner, ifld, hobjTarget);
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Verify that the entry form is valid, and then obtain the database id for the Reversal Index
	specified by the writing system.

	@param pszForm form of the target entry (used only for validation)
	@param ws Database id of the desired Writing system.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetReversalIndex(const char * pszForm, int ws)
{
	if (!pszForm)
	{
		// "Implicit ReversalIndexEntry target in a Link element is missing the form attribute."
		StrAnsi sta(kstidXmlErrorMsg163);
		LogMessage(sta.Chars());
		return 0;
	}

	// First we have to find (or create) the desired ReversalIndex given by the writing system.

	int hobjIndex = FindOrCreateReversalIndex(ws);
	if (hobjIndex == 0)
	{
		// "Implicit ReversalIndexEntry target in a Link element cannot find/create the
		// ReversalIndex."
		StrAnsi sta(kstidXmlErrorMsg164);
		LogMessage(sta.Chars());
	}
	return hobjIndex;
}


/*----------------------------------------------------------------------------------------------
	Find the given entry in the given reversal index.

	@param hobjIndex Database id of a reversal index
	@param stuNameLow Lowercased name of the Reversal Index Entry
	@param ws Writing system id of the the Reversal Index (and Entry)
	@param li reference to a ListInfo object containing ReversalIndex info (output)

	@return Database id of the reversal index entry, or 0 if it doesn't exist
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::FindTargetReversalIndexEntry(int hobjIndex, StrUni & stuNameLow, int ws,
	ListInfo & li)
{
	// Find the stored ReversalIndexEntry Name hashmap if it exists.
	EnsureReversalIndexMapLoaded(hobjIndex, ws, li);
	Assert(li.m_ws == ws);

	// We've obtained the hashmap needed, now try to find the target object id.
	int hobjTarget = 0;
	bool fOk = li.m_phmsuNamehobj->Retrieve(stuNameLow, &hobjTarget);
	if (fOk)
		return hobjTarget;
	else
		return 0;
}


/*----------------------------------------------------------------------------------------------
	Ensure that the ReversalIndexEntry information for the given Reversal Index is loaded into
	memory, placing the information into the referenced ListInfo object.

	@param hobjIndex Database id of a reversal index
	@param ws Writing system id of the reversal index
	@param li reference to a ListInfo object containing ReversalIndex info (output)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::EnsureReversalIndexMapLoaded(int hobjIndex, int ws, ListInfo & li)
{
	if (!m_hmhobjliRevIdx.Retrieve(hobjIndex, &li))
	{
		// Create the hashmap, and fill it in from the database.
		StrAnsiBufBig stab;
		SqlStatement sstmt;
		RETCODE rc;
		int nDbVer = m_pfwxd->DbVersion();
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			if (nDbVer < 200236)
				stab.Format("SELECT co.Id, co.Owner$, rf.Txt%n"
					"FROM CmObject co%n"
					"JOIN ReversalIndexEntry_ReversalForm rf ON rf.Obj=co.Id AND rf.Ws=%<0>d%n"
					"JOIN dbo.fnGetOwnedObjects$(%<1>d,null,%<2>d,0,0,1,null,0) o ON o.ObjId=co.Id%n"
					"ORDER BY o.OwnerDepth, o.ObjId%n;",
					ws, hobjIndex, kgrfcptOwning);
			else
				stab.Format("SELECT co.Id, co.Owner$, rf.Txt%n"
					"FROM CmObject co%n"
					"JOIN ReversalIndexEntry_ReversalForm rf ON rf.Obj=co.Id AND rf.Ws=%<0>d%n"
					"JOIN dbo.fnGetOwnedObjects$('%<1>d',%<2>d,0,0,1,null,0) o ON o.ObjId=co.Id%n"
					"ORDER BY o.OwnerDepth, o.ObjId%n;",
					ws, hobjIndex, kgrfcptOwning);
		}
		int hobj = 0;
		SDWORD cbhobj;
		int hobjOwner = 0;
		SDWORD cbhobj2;
		wchar rgchName[4001];
		SDWORD cbName;
		sstmt.Init(m_sdb);
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
		rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbhobj);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &hobjOwner, isizeof(hobjOwner),
			&cbhobj2);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_WCHAR, &rgchName, isizeof(rgchName), &cbName);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		li.m_phmsuNamehobj = NewObj HashMapStrUni<int>;	// Instantiate the Name hashmap.
		li.m_phmsuAbbrhobj = NULL;
		li.m_ws = ws;
		HashMap<int, StrUni> hmhobjstuName;
		for (;;)
		{
			rc = SQLFetch(sstmt.Hstmt());
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			if (rc == SQL_NO_DATA)
				break;
			if (rc != SQL_SUCCESS)
				ThrowHr(WarnHr(E_UNEXPECTED));
			if (cbhobj == SQL_NULL_DATA || !cbhobj || cbhobj2 == SQL_NULL_DATA || !cbhobj2)
				continue;
			if (cbName && cbName != SQL_NULL_DATA)
			{
				StrUni stu;
				if (hobjOwner == hobjIndex)
				{
					// The top level of the hiearchical list has a simple flat name.
					stu.Assign(rgchName, cbName/sizeof(wchar));
				}
				else
				{
					// This builds up the hierarchical name from the owner, which may have had
					// its hierarchical name built up from its owner, ...
					StrUni stuOwner;
					if (!hmhobjstuName.Retrieve(hobjOwner, &stuOwner))
						ThrowHr(WarnHr(E_UNEXPECTED));
					stu.Format(L"%s|", stuOwner.Chars());
					stu.Append(rgchName, cbName/sizeof(wchar));
				}
				StrUtil::ToLower(stu);
				li.m_phmsuNamehobj->Insert(stu, hobj, true);
				hmhobjstuName.Insert(hobj, stu);
			}
		}
		sstmt.Clear();
		m_hmhobjliRevIdx.Insert(hobjIndex, li);
	}
}


/*----------------------------------------------------------------------------------------------
	Create a new ReversalIndexEntry since none with the given name, writing system, and owner
	already exist.  This is a normal, expected thing to do while loading a dictionary.

	@param hobjIndex Database id of a reversal index
	@param stuName Form of the ReversalIndexEntry.  This is complicated by the possibility of
					embedded hierarchy in the attribute string (indicated by internal |
					characters).
	@param ws Database id of the desired Writing system.
	@param li reference to a ListInfo object containing ReversalIndex info

	@return Database id of the newly created ReversalIndexEntry
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::CreateReversalEntry(int hobjIndex, StrUni & stuName, int ws, ListInfo & li)
{
	int hobjTarget = 0;
	int iBegin = 0;
	int hobjOwner = hobjIndex;
	int flidOwner = kflidReversalIndex_Entries;
	StrUni stuHier;
	StrUni stuNameLow;
	do
	{
		int iBrk = stuName.FindCh('|', iBegin);
		if (iBrk == iBegin)
		{
			++iBegin;	// Handle || same as |.  Ignores leading (or trailing) |.
			continue;
		}
		if (iBrk == -1)
			iBrk = stuName.Length();

		StrUni stuNew(stuName.Chars() + iBegin, iBrk - iBegin);
		if (stuHier.Length())
			stuHier.Append(L"|");
		stuHier.Append(stuNew.Chars());
		int hobj;
		stuNameLow.Assign(stuHier);
		StrUtil::ToLower(stuNameLow);
		bool fAlready = li.m_phmsuNamehobj->Retrieve(stuNameLow, &hobj);
		if (fAlready)
		{
			hobjTarget = hobj;		// no need to create object at this level...
		}
		else
		{
			SQLINTEGER cchTxt;
			SQLINTEGER cbTxt;
			SQLINTEGER cbTxtLine;
			SQLINTEGER cbTxtName;
			StrAnsiBuf stab;
			SqlStatement sstmt;
			RETCODE rc;
			hobjTarget = GenerateNextNewHvo();
			if(CURRENTDB == FB) {
				stab.Format("EXECUTE PROCEDURE CreateOwnedObject$ %<0>u, %<1>u, null, %<2>u, %<3>u, %<4>u;%n"
					"INSERT INTO ReversalIndexEntry_ReversalForm (Obj, Ws, Txt)"
					" VALUES (%<1>u, %<5>u, ?);",
					kclidReversalIndexEntry, hobjTarget, hobjOwner, flidOwner,
					kcptOwningCollection, ws);
			}
			if(CURRENTDB == MSSQL) {
				stab.Format("EXEC CreateOwnedObject$ %<0>u, %<1>u, null, %<2>u, %<3>u, %<4>u;%n"
					"INSERT INTO ReversalIndexEntry_ReversalForm (Obj, Ws, Txt)"
					" VALUES (%<1>u, %<5>u, ?);",
					kclidReversalIndexEntry, hobjTarget, hobjOwner, flidOwner,
					kcptOwningCollection, ws);
			}
			cchTxt = stuNew.Length();
			cbTxt = cchTxt * isizeof(wchar);
			cbTxtLine = cbTxt + isizeof(wchar);
			cbTxtName = cbTxt;

			sstmt.Init(m_sdb);
			rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
				cchTxt, 0, const_cast<wchar *>(stuNew.Chars()), cbTxtLine, &cbTxtName);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

			rc = SQLExecDirectA(sstmt.Hstmt(),
				reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
			sstmt.Clear();
			li.m_phmsuNamehobj->Insert(stuNameLow, hobjTarget);
			// KenZ says that this is not worth logging -- it's a normal, expected thing to
			// do while loading a dictionary.
		}
		iBegin = iBrk + 1;
		hobjOwner = hobjTarget;
		flidOwner = kflidReversalIndexEntry_Subentries;
	} while (iBegin < stuName.Length());
	return hobjTarget;
}


/*----------------------------------------------------------------------------------------------
	Find the database object id for the ReversalIndex indicated by the writing system.

	@param ws
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::FindOrCreateReversalIndex(int ws)
{
	int hobj = 0;
	if (m_hmwshobjRevIdx.Retrieve(ws, &hobj))
		return hobj;

	StrAnsi staFmt;
	StrAnsi sta;
	StrAnsiBuf stab;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		stab.Format("SELECT Id FROM ReversalIndex WHERE WritingSystem = %d;", ws);
	}
	SqlStatement sstmt;
	RETCODE rc;
	bool fExists = true;
	SDWORD cbhobj;
	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbhobj);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLFetch(sstmt.Hstmt());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	sstmt.Clear();
	if (rc == SQL_NO_DATA)
		fExists = false;
	else if (rc != SQL_SUCCESS)
		ThrowHr(WarnHr(E_UNEXPECTED));
	else if (cbhobj == SQL_NULL_DATA || !cbhobj)
		fExists = false;
	if (fExists)
	{
		m_hmwshobjRevIdx.Insert(ws, hobj);
		return hobj;
	}
	// Create the ReversalIndex object, and return its database id.
	// First, we need to get the lexical database id.
	bool fIsNull;
	int hobjLexDb;
	const char * pszTable = (m_pfwxd->DbVersion() <= 200202) ? "LexicalDatabase" : "LexDb";
	if(CURRENTDB == FB)
	{
		sta.Format("SELECT FIRST 1 Id FROM %s;", pszTable);
	}
	else if(CURRENTDB == MSSQL)
	{
		sta.Format("SELECT TOP 1 Id FROM %s;", pszTable);
	}
	hobjLexDb = ReadOneIntFromSQL(sta.Chars(), __LINE__, fIsNull);
	if (fIsNull)
	{
		// "Error obtaining LexDb id from the database!?\n"
		sta.Load(kstidXmlErrorMsg166);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	hobj = GenerateNextNewHvo();
	if(CURRENTDB == FB) {
		stab.Format("EXECUTE PROCEDURE CreateOwnedObject$ %<0>u, %<1>u, null, %<2>u, %<3>u, %<4>u;%n"
			"UPDATE ReversalIndex SET WritingSystem=%<5>u WHERE Id=%<1>u;",
			kclidReversalIndex, hobj, hobjLexDb, kflidLexDb_ReversalIndexes,
			kcptOwningCollection, ws);
	}
	if(CURRENTDB == MSSQL) {
		stab.Format("EXEC CreateOwnedObject$ %<0>u, %<1>u, null, %<2>u, %<3>u, %<4>u;%n"
			"UPDATE ReversalIndex SET WritingSystem=%<5>u WHERE Id=%<1>u;",
			kclidReversalIndex, hobj, hobjLexDb, kflidLexDb_ReversalIndexes,
			kcptOwningCollection, ws);
	}
	ExecuteSimpleSQL(stab.Chars(), __LINE__);
	// We also need to initialize the PartsOfSpeech list owned by the ReversalIndex. (LT-5328)
	int hobjPOSList = GenerateNextNewHvo();
	if(CURRENTDB == FB) {
		stab.Format("EXECUTE PROCEDURE CreateOwnedObject$ %<0>u, %<1>u, null, %<2>u, %<3>u, %<4>u;%n"
			"UPDATE CmPossibilityList SET ItemClsid=%<5>u WHERE Id=%<1>u;",
			kclidCmPossibilityList, hobjPOSList, hobj, kflidReversalIndex_PartsOfSpeech,
			kcptOwningSequence, kclidPartOfSpeech);
	}
	if(CURRENTDB == MSSQL) {
		stab.Format("EXEC CreateOwnedObject$ %<0>u, %<1>u, null, %<2>u, %<3>u, %<4>u;%n"
			"UPDATE CmPossibilityList SET ItemClsid=%<5>u WHERE Id=%<1>u;",
			kclidCmPossibilityList, hobjPOSList, hobj, kflidReversalIndex_PartsOfSpeech,
			kcptOwningSequence, kclidPartOfSpeech);
	}
	ExecuteSimpleSQL(stab.Chars(), __LINE__);

	// Log creating this item.
	// "Creating ReversalIndex for the %<0>S (""%<1>S"") language."
	StrUni stuName;
	StrUni stuLocale;
	GetWsNameAndLocale(ws, stuName, stuLocale);
	staFmt.Load(kstidXmlInfoMsg105);
	sta.Format(staFmt.Chars(), stuName.Chars(), stuLocale.Chars());
	LogMessage(sta.Chars());

	m_hmwshobjRevIdx.Insert(ws, hobj);
	return hobj;
}


/*----------------------------------------------------------------------------------------------
	Get the Name and ICULocale value for the given WritingSystem id, loading these values from
	the database if not already cached.

	@param ws WritingSystem id
	@param stuName reference to the name string
	@param stuLocale reference to the ICU locale string
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::GetWsNameAndLocale(int ws, StrUni & stuName, StrUni & stuLocale)
{
	if (!m_hmwssuName.Retrieve(ws, &stuName))
	{
		StrAnsi staUserWs(kstidXmlUserWs);
		StrAnsiBufBig stab;
		SqlStatement sstmt;
		RETCODE rc;
		wchar rgch[4001];
		SDWORD cbT;

		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stab.Format("SELECT lws.ICULocale, n.Txt%n"
				"FROM LgWritingSystem lws%n"
				"LEFT OUTER JOIN LgWritingSystem lws2 ON lws2.IcuLocale = '%<0>s' --( get name in current locale %n"
				"LEFT OUTER JOIN LgWritingSystem_Name n ON n.Obj = lws.Id AND Ws = lws2.Id %n"
				"WHERE lws.Id = %<1>d;", staUserWs.Chars(), ws);
		}
		sstmt.Init(m_sdb);
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_WCHAR, rgch, isizeof(rgch), &cbT);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_SUCCESS && cbT != SQL_NULL_DATA)
			stuLocale.Assign(rgch, cbT/sizeof(wchar));
		rc = SQLGetData(sstmt.Hstmt(), 2, SQL_C_WCHAR, rgch, isizeof(rgch), &cbT);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_SUCCESS && cbT != SQL_NULL_DATA)
			stuName.Assign(rgch, cbT/sizeof(wchar));
		sstmt.Clear();
		m_hmwssuName.Insert(ws, stuName);
		m_hmwssuICULocale.Insert(ws, stuLocale, true);
	}
	else
	{
		m_hmwssuICULocale.Retrieve(ws, &stuLocale);
	}
}


/*----------------------------------------------------------------------------------------------
	Find the database object id for the list indicated by flidList.  It is assumed that flidList
	is an atomic owning field which points to a CmPossibilityList.

	@param flidList
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::FindHobjForListFlid(int flidList)
{
	if (!flidList)
		return 0;
	int ifld;
	if (!m_pfwxd->MapFidToIndex(flidList, &ifld))
	{
		// "Unknown flid for list: %<0>d"
		ThrowWithLogMessage(kstidXmlErrorMsg307);
	}
	int hobj = 0;
	hobj = m_pfwxd->FieldInfo(ifld).nListRootId;
	if (hobj != 0)
		return hobj;

	StrAnsiBuf stab;
	// Convert the field id into a table name in the query.
	int icls = GetClassIndexFromFieldIndex(ifld);
	if(CURRENTDB == FB) {
		stab.Format("SELECT FIRST 1 Dst FROM %S_%S;",
			m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars());
	}
	else if(CURRENTDB == MSSQL) {
		stab.Format("SELECT TOP 1 Dst FROM %S_%S;",
			m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars());
	}
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbhobj;
	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
	if (rc == SQL_SUCCESS)
		rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbhobj);
	if (rc == SQL_SUCCESS)
		rc = SQLFetch(sstmt.Hstmt());
	sstmt.Clear();
	if (rc == SQL_SUCCESS)
	{
		m_pfwxd->m_vfdfi[ifld].nListRootId = hobj;
		return hobj;
	}
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Find or create the CmPossibility in the given list with the given name and/or abbreviation.
	At least one of pszName and pszAbbr must not be NULL or empty.

	@param hobjList the database object id of the CmPossibilityList to which the desired
					CmPossibility belongs
	@param ws the desired writing system id for the name or abbreviation
	@param clidItem specific class id of the target object (may be subclass of CmPossibility)
	@param pszName pointer to the CmPossibility's name (may be NULL)
	@param pszAbbr pointer to the CmPossibility's abbreviation (may be NULL)
	@param wsVern the desired writing system id for the vernacular name (may be 0)
	@param pszNameVern pointer to the CmPossibility's vernacular name (may be NULL)
	@param pszAbbrVern pointer to the CmPossibility's vernacular abbreviation (may be NULL)

	@returns the database object id of the designated CmPossibility.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::FindOrCreateCmPossibility(int hobjList, int ws, int clidItem,
	const char * pszName, const char * pszAbbr, int wsVern, const char * pszNameVern,
	const char * pszAbbrVern)
{
	// Find the stored hashmaps if they exist.
	ListInfo li;
	EnsurePossibilityListLoaded(hobjList, ws, clidItem, li);

	// We've obtained the hashmaps needed, now try to find the target object id.
	StrUni stuName;
	StrUni stuNameLow;
	StrUni stuAbbr;
	StrUni stuAbbrLow;
	if (pszName && *pszName)
	{
		StrUtil::StoreUtf16FromUtf8(pszName, strlen(pszName), stuName);
		stuNameLow.Assign(stuName);
		StrUtil::ToLower(stuNameLow);
	}
	if (pszAbbr && *pszAbbr)
	{
		StrUtil::StoreUtf16FromUtf8(pszAbbr, strlen(pszAbbr), stuAbbr);
		stuAbbrLow.Assign(stuAbbr);
		StrUtil::ToLower(stuAbbrLow);
	}
	int hobjTarget = FindCmPossibility(li, stuName, stuNameLow, stuAbbr, stuAbbrLow);
	if (hobjTarget == 0)
	{
		// Nothing matched, create a new possibility item in the database, and add its id to the
		// appropriate hashmap(s).
		hobjTarget = CreateCmPossibility(hobjList, ws, clidItem, stuName, stuAbbr);
		if (stuNameLow.Length())
			li.m_phmsuNamehobj->Insert(stuNameLow, hobjTarget, true);
		if (stuAbbrLow.Length())
			li.m_phmsuAbbrhobj->Insert(stuAbbrLow, hobjTarget, true);
		// Log creating this item.
		// Info: Creating new object with ws="%<0>S", abbr="%<1>s", and name="%<2>s"
		// in the %<3>S list.
		StrUni stuLangName;
		StrUni stuWs;
		GetWsNameAndLocale(ws, stuLangName, stuWs);
		StrAnsi staFmt(kstidXmlInfoMsg103);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), stuWs.Chars(),
			pszAbbr ? pszAbbr : "", pszName ? pszName : "", li.m_stuListName.Chars());
		LogMessage(sta.Chars());
		if (li.m_fIsClosed)
		{
			// "Warning: The %<0>S list is not supposed to be extensible!"
			staFmt.Load(kstidXmlInfoMsg104);
			sta.Format(staFmt.Chars(), li.m_stuListName.Chars());
			LogMessage(sta.Chars());
		}
	}
	Assert(hobjTarget);
	if (wsVern != 0 && pszNameVern != NULL)
		AddCmPossibilityVernacularNameAndAbbr(hobjTarget, wsVern, pszNameVern, pszAbbrVern);

	li.m_phmsuNamehobj = NULL;		// prevent being deleted by destructor.
	li.m_phmsuAbbrhobj = NULL;		// prevent being deleted by destructor.
	return hobjTarget;
}

/*----------------------------------------------------------------------------------------------
	If the CmPossibility does not yet have the name and abbreviation in the vernacular writing
	system, add these.  If such data already exists, do nothing.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::AddCmPossibilityVernacularNameAndAbbr(int hobj, int wsVern,
	const char * pszNameVern, const char * pszAbbrVern)
{
	// Check whether the name and abbreviation exist in the vernacular writing system.
	StrAnsiBuf stab;
	// Create the Name and Abbr hashmaps, and fill them from the database.
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		stab.Format("SELECT n.Txt, a.Txt%n"
			"FROM CmPossibility_Name n%n"
			"LEFT OUTER JOIN CmPossibility_Abbreviation a ON a.Obj = n.Obj AND a.Ws = n.Ws%n"
			"WHERE n.Obj = %<0>d AND n.Ws = %<1>d;",
			hobj, wsVern);
	}
	wchar rgchName[4001];
	wchar rgchAbbr[4001];
	SDWORD cbName = 0;
	SDWORD cbAbbr = 0;
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
	rc = SQLFetch(sstmt.Hstmt());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	if (rc != SQL_NO_DATA)
	{
		rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_WCHAR, rgchName, isizeof(rgchName), &cbName);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLGetData(sstmt.Hstmt(), 2, SQL_C_WCHAR, rgchAbbr, isizeof(rgchAbbr), &cbAbbr);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	}
	sstmt.Clear();
	if (rc == SQL_NO_DATA || (cbName == 0 && cbAbbr == 0))
	{
		// Add the vernacular name and abbreviation to the CmPossibility.
		sstmt.Init(m_sdb);

		Assert(pszNameVern != NULL);
		StrUni stuName;
		StrUtil::StoreUtf16FromUtf8(pszNameVern, strlen(pszNameVern), stuName);
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stab.Format("INSERT INTO CmPossibility_Name (Obj,Ws,Txt) VALUES (%<0>u,%<1>u,?);",
				hobj, wsVern);
		}
		SDWORD cchTxt = stuName.Length();
		SDWORD cbTxt = cchTxt * isizeof(wchar);
		SDWORD cbTxtLine = cbTxt + isizeof(wchar);
		cbName = cbTxt;
		rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
			cchTxt, 0, const_cast<wchar *>(stuName.Chars()), cbTxtLine, &cbName);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

		StrUni stuAbbr;
		if (pszAbbrVern != NULL)
		{
			if(CURRENTDB == FB || CURRENTDB == MSSQL) {
				stab.FormatAppend(
					"%nINSERT INTO CmPossibility_Abbreviation (Obj,Ws,Txt) VALUES (%<0>u,%<1>u,?);",
					hobj, wsVern);
			}
			StrUtil::StoreUtf16FromUtf8(pszAbbrVern, strlen(pszAbbrVern), stuAbbr);
			cchTxt = stuAbbr.Length();
			cbTxt = cchTxt * isizeof(wchar);
			cbTxtLine = cbTxt + isizeof(wchar);
			cbAbbr = cbTxt;
			rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
				cchTxt, 0, const_cast<wchar *>(stuAbbr.Chars()), cbTxtLine, &cbAbbr);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		}
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
		sstmt.Clear();
	}
}

/*----------------------------------------------------------------------------------------------
	Ensure that the data for the given possibility list in the given writing system are loaded
	into the ListInfo object.

	@param hobjList the database object id of the CmPossibilityList to which the desired
					CmPossibility belongs
	@param ws the desired writing system id for the name or abbreviation
	@param clidItem specific class id of the target object (may be subclass of CmPossibility)
	@param li reference to a ListInfo object for the possibility list (output)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::EnsurePossibilityListLoaded(int hobjList, int ws, int clidItem,
	ListInfo & li)
{
	ListIdentity lid = { hobjList, ws };
	if (!m_hmlidli.Retrieve(lid, &li))
	{
		// Get the list's name and IsClosed flag.
		LoadPossibilityListProperties(hobjList, ws, li);

		int nDbVer = m_pfwxd->DbVersion();
		StrAnsiBufBig stab;
		// Create the Name and Abbr hashmaps, and fill them from the database.
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			if (nDbVer < 200236)
				stab.Format("SELECT co.Id, n.Txt, a.Txt%n"
					"FROM CmObject co%n"
					"LEFT OUTER JOIN CmPossibility_Name n ON n.Obj = co.Id AND n.ws = %<0>d%n"
					"LEFT OUTER JOIN CmPossibility_Abbreviation a ON a.Obj = co.Id AND a.ws = %<0>d%n"
					"WHERE (n.Txt IS NOT NULL OR a.Txt IS NOT NULL) AND%n"
					"    co.Id IN (SELECT ObjId%n"
					"              FROM dbo.fnGetOwnedObjects$(%<1>d, null, %<2>d, 0, 0, 1, null, 0)%n"
					"              WHERE ObjClass = %<3>d);",
					ws, hobjList, kgrfcptOwning, clidItem);
			else
				stab.Format("SELECT co.Id, n.Txt, a.Txt%n"
					"FROM CmObject co%n"
					"LEFT OUTER JOIN CmPossibility_Name n ON n.Obj = co.Id AND n.ws = %<0>d%n"
					"LEFT OUTER JOIN CmPossibility_Abbreviation a ON a.Obj = co.Id AND a.ws = %<0>d%n"
					"WHERE (n.Txt IS NOT NULL OR a.Txt IS NOT NULL) AND%n"
					"    co.Id IN (SELECT ObjId%n"
					"              FROM dbo.fnGetOwnedObjects$('%<1>d', %<2>d, 0, 0, 1, null, 0)%n"
					"              WHERE ObjClass = %<3>d);",
					ws, hobjList, kgrfcptOwning, clidItem);
		}
		int hobj = 0;
		SDWORD cbhobj;
		wchar rgchName[4001];
		SDWORD cbName;
		wchar rgchAbbr[4001];
		SDWORD cbAbbr;
		SqlStatement sstmt;
		sstmt.Init(m_sdb);
		RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
		rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbhobj);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_WCHAR, &rgchName, isizeof(rgchName), &cbName);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_WCHAR, &rgchAbbr, isizeof(rgchAbbr), &cbAbbr);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		li.Init();	// Instantiate the hashmaps.
		for (;;)
		{
			rc = SQLFetch(sstmt.Hstmt());
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			if (rc == SQL_NO_DATA)
				break;
			if (rc != SQL_SUCCESS)
				ThrowHr(WarnHr(E_UNEXPECTED));
			if (cbhobj == SQL_NULL_DATA || !cbhobj)
				continue;
			if (cbName && cbName != SQL_NULL_DATA)
			{
				StrUni stu(rgchName, cbName/sizeof(wchar));
				StrUtil::ToLower(stu);
				li.m_phmsuNamehobj->Insert(stu, hobj, true);
			}
			if (cbAbbr && cbAbbr != SQL_NULL_DATA)
			{
				StrUni stu(rgchAbbr, cbAbbr/sizeof(wchar));
				StrUtil::ToLower(stu);
				li.m_phmsuAbbrhobj->Insert(stu, hobj, true);
			}
		}
		sstmt.Clear();
		m_hmlidli.Insert(lid, li);
	}
}


/*----------------------------------------------------------------------------------------------
	Load the possibility list's name and IsClosed flag.

	@param hobjList the database object id of the CmPossibilityList to which the desired
					CmPossibility belongs
	@param ws the desired writing system id for the list name
	@param li reference to a ListInfo object for the possibility list
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::LoadPossibilityListProperties(int hobjList, int ws, ListInfo & li)
{
	StrAnsiBufBig stab;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		stab.Format("SELECT cpl.IsClosed, n.Txt, f.Name%n"
			"FROM CmPossibilityList cpl%n"
			"LEFT OUTER JOIN CmMajorObject_Name n ON n.Obj = cpl.id AND n.Ws = %<0>d%n"
			"JOIN CmObject co ON co.Id = cpl.Id%n"
			"JOIN Field$ f ON f.Id = co.OwnFlid$%n"
			"WHERE cpl.Id = %<1>d;",
			ws, hobjList);
	}
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
	rc = SQLFetch(sstmt.Hstmt());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	if (rc == SQL_SUCCESS)
	{
		wchar rgchName[4001];
		SDWORD cbName;
		SDWORD cbBool;
		rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_BIT, &li.m_fIsClosed,
			isizeof(li.m_fIsClosed), &cbBool);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLGetData(sstmt.Hstmt(), 2, SQL_C_WCHAR, rgchName, isizeof(rgchName), &cbName);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (cbName !=  SQL_NULL_DATA && cbName != 0)
		{
			li.m_stuListName.Assign(rgchName, cbName/sizeof(wchar));
		}
		else
		{
			// If the user hasn't given the list a name, use the owner's field name.  (which
			// is not that great, but better than nothing, i suppose.)
			rc = SQLGetData(sstmt.Hstmt(), 3, SQL_C_WCHAR, rgchName, isizeof(rgchName),
				&cbName);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			li.m_stuListName.Assign(rgchName, cbName/sizeof(wchar));
		}
	}
	sstmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Find the database id of the desired CmPossibility in the given list.

	@param li reference to a ListInfo object for a possibility list
	@param stuName Name of the CmPossibility
	@param stuNameLow Lowercased name of the CmPossibility
	@param stuAbbr Abbreviation of the CmPossibility
	@param stuAbbrLow Lowsercased abbreviation of the CmPossibility
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::FindCmPossibility(ListInfo & li, StrUni & stuName, StrUni & stuNameLow,
	StrUni & stuAbbr, StrUni & stuAbbrLow)
{
	int hobjTarget = 0;
	bool fOk = false;
	StrAnsi staFmt;
	StrAnsi sta;
	if (stuNameLow.Length())
	{
		fOk = li.m_phmsuNamehobj->Retrieve(stuNameLow, &hobjTarget);
	}
	if (!fOk && stuAbbrLow.Length())
	{
		fOk = li.m_phmsuAbbrhobj->Retrieve(stuAbbrLow, &hobjTarget);
	}
	// swap name and abbr and try again just in case.
	if (!fOk && stuAbbr.Length())
	{
		fOk = li.m_phmsuNamehobj->Retrieve(stuAbbrLow, &hobjTarget);
		if (fOk)
		{
			staFmt.Load(kstidXmlErrorMsg167);
			sta.Format(staFmt.Chars(), "CmPossibility", stuAbbr.Chars());
			int cMore = 0;
			if (m_hmcMsgcMore.Retrieve(sta.Chars(), &cMore))
			{
				// Just increment the counter.
				++cMore;
				m_hmcMsgcMore.Insert(sta.Chars(), cMore, true);
			}
			else
			{
				m_hmcMsgcMore.Insert(sta.Chars(), cMore);
				LogMessage(sta.Chars());
			}
		}
	}
	if (!fOk && stuName.Length())
	{
		fOk = li.m_phmsuAbbrhobj->Retrieve(stuNameLow, &hobjTarget);
		if (fOk)
		{
			staFmt.Load(kstidXmlErrorMsg168);
			sta.Format(staFmt.Chars(), "CmPossibility", stuName.Chars());
			int cMore = 0;
			if (m_hmcMsgcMore.Retrieve(sta.Chars(), &cMore))
			{
				// Just increment the counter.
				++cMore;
				m_hmcMsgcMore.Insert(sta.Chars(), cMore, true);
			}
			else
			{
				m_hmcMsgcMore.Insert(sta.Chars(), cMore);
				LogMessage(sta.Chars());
			}
		}
	}
	return hobjTarget;
}


/*----------------------------------------------------------------------------------------------
	Create a new CmPossibility since none with the given name and abbreviation in the given
	writing system already exist in the list.

	@param hobjList the database object id of the CmPossibilityList to which the desired
					CmPossibility belongs
	@param ws the desired writing system id for the name or abbreviation
	@param stuName Name of the CmPossibility
	@param stuAbbr Abbreviation of the CmPossibility

	@return Database id of the newly created CmPossibility
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::CreateCmPossibility(int hobjList, int ws, int clidItem, StrUni & stuName,
	StrUni & stuAbbr)
{
	// First ensure that we have both a name and abbreviation (see LT-1692).
	if (stuName.Length() == 0)
		stuName = stuAbbr;
	else if (stuAbbr.Length() == 0)
		stuAbbr = stuName;
	StrAnsiBufBig stab;
	SqlStatement sstmt;
	RETCODE rc;
	SQLINTEGER cchTxt;
	SQLINTEGER cbTxt;
	SQLINTEGER cbTxtLine;
	SQLINTEGER cbTxtName;
	SQLINTEGER cbTxtAbbr;
	sstmt.Init(m_sdb);
	int hobjTarget = GenerateNextNewHvo();
	if(CURRENTDB == FB) {
		stab.Format("EXECUTE PROCEDURE CreateOwnedObject$ %u,%u,null,%u,%u,%u;%n",
			clidItem, hobjTarget, hobjList, kflidCmPossibilityList_Possibilities,
			kcptOwningSequence);
	}
	if(CURRENTDB == MSSQL) {
		stab.Format("EXEC CreateOwnedObject$ %u,%u,null,%u,%u,%u;%n",
			clidItem, hobjTarget, hobjList, kflidCmPossibilityList_Possibilities,
			kcptOwningSequence);
	}
	SQLUSMALLINT icol = 1;
	if (stuName.Length())
	{
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stab.FormatAppend(
				"INSERT INTO CmPossibility_Name (Obj,Ws,Txt) VALUES (%u,%u,?);%n",
					hobjTarget, ws);
		}
		cchTxt = stuName.Length();
		cbTxt = cchTxt * isizeof(wchar);
		cbTxtLine = cbTxt + isizeof(wchar);
		cbTxtName = cbTxt;
		rc = SQLBindParameter(sstmt.Hstmt(), icol++, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
			cchTxt+1, 0, const_cast<wchar *>(stuName.Chars()), cbTxtLine, &cbTxtName);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	}
	if (stuAbbr.Length())
	{
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			stab.FormatAppend(
				"INSERT INTO CmPossibility_Abbreviation (Obj,Ws,Txt) VALUES (%u,%u,?);",
					hobjTarget, ws);
		}
		cchTxt = stuAbbr.Length();
		cbTxt = cchTxt * isizeof(wchar);
		cbTxtLine = cbTxt + isizeof(wchar);
		cbTxtAbbr = cbTxt;
		rc = SQLBindParameter(sstmt.Hstmt(), icol++, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WVARCHAR,
			cchTxt+1, 0, const_cast<wchar *>(stuAbbr.Chars()), cbTxtLine, &cbTxtAbbr);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	}
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
	sstmt.Clear();
	return hobjTarget;
}


/*----------------------------------------------------------------------------------------------
	Store a reference value for a Link element.

	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
	@param hobj Database id of the object being referenced.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreReference(const XML_Char ** prgpszAtts, int hobjOwner, int ifld,
	int hobj)
{
	const char * pszOrd;
	int ord;
	char * psz;
	StrAnsi sta;
	StrAnsi staFmt;

	Assert(m_vspiOpen.Size());
	int flid = m_pfwxd->FieldInfo(ifld).fid;
	if (flid == kflidLexEntryRef_ComponentLexemes || flid == kflidLexEntryRef_PrimaryLexemes)
		m_sethvoLexEntryRef.Insert(hobjOwner);

	// Determine exact type of field.  Get ord if reference sequence.
	switch (m_pfwxd->FieldInfo(ifld).cpt)
	{
	case kcptReferenceAtom:
		m_vstda[ifld].m_vhobj.Push(hobjOwner);
		m_vstda[ifld].m_vhobjDst.Push(hobj);
		if (m_vstda[ifld].m_vhobj.Size() >= kceSeg)
			StoreReferenceAtoms(ifld);
		break;
	case kcptReferenceCollection:
		m_vstda[ifld].m_vhobj.Push(hobjOwner);
		m_vstda[ifld].m_vhobjDst.Push(hobj);
		if (m_vstda[ifld].m_vhobj.Size() >= kceSeg)
			StoreReferenceCollections(ifld);
		break;
	case kcptReferenceSequence:
		Assert(m_vspiOpen.Top()->m_fSeq);
		m_vspiOpen.Top()->m_cobj++;
		pszOrd = FwXml::GetAttributeValue(prgpszAtts, "ord");
		if (pszOrd)
		{
			ord = static_cast<int>(strtol(pszOrd, &psz, 10));
			if (*psz)
			{
				// "Invalid ord attribute in Link element: \"%s\"."
				staFmt.Load(kstidXmlErrorMsg061);
				sta.Format(staFmt.Chars(), pszOrd);
				LogMessage(sta.Chars());
				break;
			}
			m_vspiOpen.Top()->m_cord++;
			if (m_vspiOpen.Top()->m_cobj !=
				m_vspiOpen.Top()->m_cord)
			{
				// "Cannot have some ord attribute values missing and some present!"
				sta.Load(kstidXmlErrorMsg015);
				LogMessage(sta.Chars());
				break;
			}
		}
		else
		{
			if (m_vspiOpen.Top()->m_cord)
			{
				// "Missing %<0>s attribute in %<1>s element."
				staFmt.Load(kstidXmlErrorMsg079);
				sta.Format(staFmt.Chars(), "ord", "Link");
				LogMessage(sta.Chars());
				break;
			}
			ord = m_vspiOpen.Top()->m_ord++;
		}
		m_vstda[ifld].m_vhobj.Push(hobjOwner);
		m_vstda[ifld].m_vhobjDst.Push(hobj);
		m_vstda[ifld].m_vord.Push(ord);
		if (m_vstda[ifld].m_vhobj.Size() >= kceSeg)
			StoreReferenceSequences(ifld);
		break;
	default:
		// "Wrong field type for Link element: %d"
		staFmt.Load(kstidXmlErrorMsg134);
		sta.Format(staFmt.Chars(), m_pfwxd->FieldInfo(ifld).cpt);
		LogMessage(sta.Chars());
		break;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements for basic type properties during the second pass.

	@param eti Reference to the basic element type information structure.
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param stabCmd Reference to a string for outputting an SQL command.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StartBasicProp2(ElemTypeInfo & eti, const XML_Char * pszName,
	const XML_Char ** prgpszAtts, StrAnsiBuf & stabCmd)
{
	Assert(m_vhobjOpen.Size());
	Assert(m_vetiOpen.Size() >= 3);
	Assert(m_vetiOpen[m_vetiOpen.Size() - 2].m_elty == keltyObject);

	int hobjOwner = m_vhobjOpen[m_vhobjOpen.Size() - 1];
	ElemTypeInfo & etiProp = m_vetiOpen[m_vetiOpen.Size() - 1];
	Assert(etiProp.m_elty == keltyPropName || etiProp.m_elty == keltyCustomProp ||
		etiProp.m_elty == keltyVirtualProp);
	m_vetiOpen.Push(eti);
	int ifld = etiProp.m_ifld;
	int icls = GetOwnerClassIdForBasicProp(etiProp, ifld);

	switch (eti.m_cpt)
	{
	case kcptBoolean:
		SetCommandForBooleanValue(stabCmd, prgpszAtts, hobjOwner, icls, ifld);
		break;
	case kcptInteger:
		SetCommandForIntegerValue(stabCmd, prgpszAtts, hobjOwner, icls, ifld);
		break;
	case kcptNumeric:
		SetCommandForNumericValue(stabCmd, prgpszAtts, hobjOwner, icls, ifld);
		break;
	case kcptFloat:
		SetCommandForFloatValue(stabCmd, prgpszAtts, hobjOwner, icls, ifld);
		break;
	case kcptTime:
		SetCommandForTimeValue(stabCmd, prgpszAtts, hobjOwner, icls, ifld);
		break;
	case kcptGuid:
		SetCommandForGuidValue(stabCmd, prgpszAtts, hobjOwner, icls, ifld);
		break;
	case kcptGenDate:
		SetCommandForGenDateValue(stabCmd, prgpszAtts, hobjOwner, icls, ifld);
		break;
	case kcptBinary:
		PrepareForBinaryData();
		break;
	case kcptImage:
		PrepareForBinaryData();
		break;
	case kcptString:				// May actually be kcptBigString.
		PrepareForStringData();
		break;
	case kcptMultiString:			// May actually be kcptMultiBigString.
		PrepareForMultiStringData(prgpszAtts);
		break;
	case kcptUnicode:				// May actually be kcptBigUnicode.
		PrepareForUnicodeData();
		break;
	case kcptMultiUnicode:			// May actually be kcptMultiBigUnicode.
		PrepareForMultiUnicodeData(prgpszAtts);
		break;
	case kcptReferenceAtom:	// May actually be kcptReferenceCollection or kcptReferenceSequence
		HandleReferenceLink(pszName, prgpszAtts, hobjOwner, icls, ifld, etiProp);
		break;
	case kcptRuleProp:
		PrepareForRuleProp(pszName, prgpszAtts);
		break;
	default:
		Assert(eti.m_cpt == kcptString);	// THIS SHOULD NEVER HAPPEN!
		break;
	}
}


/*----------------------------------------------------------------------------------------------
	Get the index into the class table for the owner of the current basic property.

	@param etiProp reference to the Element Type information of the current property
	@param ifld index into the field table for the current property
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetOwnerClassIdForBasicProp(ElemTypeInfo & etiProp, int ifld)
{
	int icls = 0;
	bool fClsOk = false;
	if (etiProp.m_elty == keltyVirtualProp)
	{
		int clid;
		switch (etiProp.m_ifld)
		{
		case 1:
			clid = 5016;
			fClsOk = m_pfwxd->MapCidToIndex(clid, &icls); // LexSense
			break;
		case 2:
			clid = 5002;
			fClsOk = m_pfwxd->MapCidToIndex(clid, &icls); // LexEntry
			break;
		}
	}
	else
	{
		int cid = m_pfwxd->FieldInfo(ifld).cid;
		fClsOk = m_pfwxd->MapCidToIndex(cid, &icls);
	}
	if (!fClsOk)
	{
		// CRASH, BURN, EXPLODE!!
		// "INTERNAL DATA CORRUPTION: unable to get class for field!"
		StrAnsi sta(kstidXmlErrorMsg040);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	return icls;
}


/*----------------------------------------------------------------------------------------------
	Set the SQL command for storing the value of this Boolean property.

	@param stabCmd reference to the string containing the SQL command (output)
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetCommandForBooleanValue(StrAnsiBuf & stabCmd,
	const XML_Char ** prgpszAtts, int hobjOwner, int icls, int ifld)
{
	/*
	  <!ELEMENT Boolean EMPTY >
	  <!ATTLIST Boolean val (true | false) #REQUIRED >
	*/
	StrAnsi sta;
	StrAnsi staFmt;
	const char * pszVal = FwXml::GetAttributeValue(prgpszAtts, "val");
	if (!pszVal)
	{
		// "Missing %<0>s attribute in %<1>s element."
		staFmt.Load(kstidXmlErrorMsg079);
		sta.Format(staFmt.Chars(), "val", "Boolean");
		LogMessage(sta.Chars());
		return;
	}
	int nVal = 0;
	if (strcmp(pszVal, "true") == 0)
	{
		nVal = 1;
	}
	else if (strcmp(pszVal, "false") == 0)
	{
		nVal = 0;
	}
	else
	{
		// "Invalid Boolean value for the %S field of a %S object: \"%s\"."
		staFmt.Load(kstidXmlErrorMsg048);
		sta.Format(staFmt.Chars(),
			m_pfwxd->FieldName(ifld).Chars(), m_pfwxd->ClassName(icls).Chars(), pszVal);
		LogMessage(sta.Chars());
		return;
	}
	if(CURRENTDB == MSSQL) {
		stabCmd.Format("UPDATE \"%S\" SET \"%S\" = %d WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(),
			m_pfwxd->FieldName(ifld).Chars(), nVal, hobjOwner);
	}
	else if(CURRENTDB == FB) {
		stabCmd.Format("UPDATE %S SET \"%S\" = %d WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(),
			UpperName(m_pfwxd->FieldName(ifld)), nVal, hobjOwner);
	}
}

/*----------------------------------------------------------------------------------------------
	Set the SQL command for storing the value of this integer property.

	@param stabCmd reference to the string containing the SQL command (output)
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetCommandForIntegerValue(StrAnsiBuf & stabCmd,
	const XML_Char ** prgpszAtts, int hobjOwner, int icls, int ifld)
{
	/*
	  <!ELEMENT Integer EMPTY >
	  <!ATTLIST Integer val CDATA #REQUIRED >
	*/
	StrAnsi sta;
	StrAnsi staFmt;
	const char * pszVal = FwXml::GetAttributeValue(prgpszAtts, "val");
	if (!pszVal)
	{
		// "Missing val attribute in Integer element."
		sta.Load(kstidXmlErrorMsg091);
		LogMessage(sta.Chars());
		return;
	}
	char * psz;
	int nVal = static_cast<int>(strtol(pszVal, &psz, 10));
	if (*psz || !*pszVal)
	{
		// "Invalid Integer value for the %S field of a %S object: \"%s\"."
		staFmt.Load(kstidXmlErrorMsg051);
		sta.Format(staFmt.Chars(),
			m_pfwxd->FieldName(ifld).Chars(), m_pfwxd->ClassName(icls).Chars(), pszVal);
		LogMessage(sta.Chars());
		return;
	}
	if(CURRENTDB == MSSQL) {
		stabCmd.Format("UPDATE \"%S\" SET \"%S\" = %d WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(),
			m_pfwxd->FieldName(ifld).Chars(), nVal, hobjOwner);
	}
	else if(CURRENTDB == FB) {
		stabCmd.Format("UPDATE %S SET \"%S\" = %d WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(),
			UpperName(m_pfwxd->FieldName(ifld)), nVal, hobjOwner);
	}
}

/*----------------------------------------------------------------------------------------------
	Set the SQL command for storing the value of this numeric property.

	@param stabCmd reference to the string containing the SQL command (output)
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetCommandForNumericValue(StrAnsiBuf & stabCmd,
	const XML_Char ** prgpszAtts, int hobjOwner, int icls, int ifld)
{
	/*
	  <!ELEMENT Numeric EMPTY >
	  <!ATTLIST Numeric val CDATA #REQUIRED >
	*/
	StrAnsi sta;
	StrAnsi staFmt;
	const char * pszVal = FwXml::GetAttributeValue(prgpszAtts, "val");
	if (!pszVal)
	{
		// "Missing val attribute in Numeric element."
		sta.Load(kstidXmlErrorMsg092);
		LogMessage(sta.Chars());
		return;
	}
	const char * psz = ScanNumber(pszVal);
	if (*psz || !*pszVal)
	{
		// "Invalid Numeric value for the %S field of a %S object: \"%s\"."
		staFmt.Load(kstidXmlErrorMsg052);
		sta.Format(staFmt.Chars(),
			m_pfwxd->FieldName(ifld).Chars(), m_pfwxd->ClassName(icls).Chars(), pszVal);
		LogMessage(sta.Chars());
		return;
	}
	if(CURRENTDB == MSSQL) {
		stabCmd.Format("UPDATE \"%S\" SET \"%S\" = %s WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(),
			m_pfwxd->FieldName(ifld).Chars(), pszVal, hobjOwner);
	}
	if(CURRENTDB == FB) {
		stabCmd.Format("UPDATE %S SET \"%S\" = %s WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(),
			UpperName(m_pfwxd->FieldName(ifld)), pszVal, hobjOwner);
	}
}


/*----------------------------------------------------------------------------------------------
	Set the SQL command for storing the value of this floating point property.

	@param stabCmd reference to the string containing the SQL command (output)
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetCommandForFloatValue(StrAnsiBuf & stabCmd,
	const XML_Char ** prgpszAtts, int hobjOwner, int icls, int ifld)
{
	/*
	  <!ELEMENT Float EMPTY >
	  <!ATTLIST Float
	  val CDATA #REQUIRED
	  bin CDATA #IMPLIED >
	*/
	StrAnsi sta;
	StrAnsi staFmt;
	const char * pszVal = FwXml::GetAttributeValue(prgpszAtts, "bin");
	if (pszVal)
	{
		SetCommandForFloatValueAsBin(stabCmd, pszVal, hobjOwner, icls, ifld);
	}
	else
	{
		pszVal = FwXml::GetAttributeValue(prgpszAtts, "val");
		if (!pszVal)
		{
			// "Missing both bin and val attributes in Float element."
			sta.Load(kstidXmlErrorMsg078);
			LogMessage(sta.Chars());
			return;
		}
		const char * psz = ScanNumber(pszVal);
		bool fHaveExp = false;
		int cDigitsExp = 0;
		if (*psz == 'e' || *psz == 'E')
		{
			++psz;
			fHaveExp = true;
			if (*psz == '+' || *psz == '-')
				++psz;
			cDigitsExp = strspn(psz, "0123456789");
			psz += cDigitsExp;
		}
		if (*psz || !*pszVal || (fHaveExp && !cDigitsExp))
		{
			// "Invalid Float value for the %S field of a %S object: \"%s\"."
			staFmt.Load(kstidXmlErrorMsg065);
			sta.Format(staFmt.Chars(),
				m_pfwxd->FieldName(ifld).Chars(), m_pfwxd->ClassName(icls).Chars(), pszVal);
			LogMessage(sta.Chars());
			return;
		}
		if (CURRENTDB == MSSQL) {
			stabCmd.Format("UPDATE \"%S\" SET \"%S\" = %s WHERE Id = %d;",
				m_pfwxd->ClassName(icls).Chars(),
				m_pfwxd->FieldName(ifld).Chars(), pszVal, hobjOwner);
		}
		else if (CURRENTDB == FB) {
			stabCmd.Format("UPDATE %S SET \"%S\" = %d WHERE Id = %d;",
				m_pfwxd->ClassName(icls).Chars(),
				UpperName(m_pfwxd->FieldName(ifld)), pszVal, hobjOwner);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Set the SQL command for storing the value of this floating point property stored as a raw
	hexadecimal value.

	@param stabCmd reference to the string containing the SQL command (output)
	@param pszVal Pointer to NUL-terminated hexadecimal digit string containing the floating
					point value.
	@param hobjOwner Database id of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetCommandForFloatValueAsBin(StrAnsiBuf & stabCmd, const char * pszVal,
	int hobjOwner, int icls, int ifld)
{
	double dbl;
	int cchVal = strlen(pszVal);
	int cchHex = strspn(pszVal, "0123456789ABCDEFabcdef");
	int cchWant = 2 * isizeof(dbl);		// 2 hex chars per byte.
	if (cchVal != cchHex || cchVal != cchWant)
	{
		// "Invalid bin attribute in Float element: \"%s\"."
		StrAnsi staFmt(kstidXmlErrorMsg055);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), pszVal);
		LogMessage(sta.Chars());
		return;
	}
	byte * pb = reinterpret_cast<byte *>(&dbl);
	byte bT;
	char ch;
	for (int ib = 0; ib < isizeof(dbl); ++ib)
	{
		// Read the first Hex digit of the byte.
		ch = pszVal[2 * ib];
		if (isdigit(ch))
			bT = static_cast<byte>((ch & 0xF) << 4);
		else
			bT = static_cast<byte>(((ch & 0xF) + 9) << 4);
		// Read the second Hex digit of the byte.
		ch = pszVal[2 * ib + 1];
		if (isdigit(ch))
			bT |= ch & 0xF;
		else
			bT |= (ch & 0xF) + 9;
		pb[ib] = bT;
	}
	if(CURRENTDB == MSSQL) {
		stabCmd.Format("UPDATE \"%S\" SET \"%S\" = ? WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(),
			m_pfwxd->FieldName(ifld).Chars(), hobjOwner);
	}
	else if(CURRENTDB == FB) {
		stabCmd.Format("UPDATE %S SET \"%S\" = ? WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(),
			UpperName(m_pfwxd->FieldName(ifld)), hobjOwner);
	}
	m_vdbl.Push(dbl);
}


/*----------------------------------------------------------------------------------------------
	Set the SQL command for storing the value of this time property.

	@param stabCmd reference to the string containing the SQL command (output)
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetCommandForTimeValue(StrAnsiBuf & stabCmd,
	const XML_Char ** prgpszAtts, int hobjOwner, int icls, int ifld)
{
	/*
	  <!ELEMENT Time EMPTY >
	  <!ATTLIST Time val CDATA #REQUIRED >
	*/
	const char * pszVal = FwXml::GetAttributeValue(prgpszAtts, "val");
	if (!pszVal)
	{
		// "Missing val attribute in Time element."
		StrAnsi sta(kstidXmlErrorMsg093);
		LogMessage(sta.Chars());
		return;
	}
	bool fOk = false;
	// This array is used for adjusting the fractional part of seconds in time values.
	static const int nFracFix[10] = { 0,	// dummy placeholder
		100000000,	//  1 fractional digit
		10000000,	//  2 fractional digits
		1000000,	//  3 fractional digits
		100000,		//  4 fractional digits
		10000,		//  5 fractional digits
		1000,		//  6 fractional digits
		100,		//  7 fractional digits
		10,			//  8 fractional digits
		1	};		//  9 fractional digits
	SQL_TIMESTAMP_STRUCT tim;
	char * psz;
	tim.year = static_cast<short>(strtol(pszVal, &psz, 10));
	if (*psz == '-')
	{
		tim.month = static_cast<unsigned short>(strtoul(psz+1, &psz, 10));
		if (*psz == '-')
		{
			tim.day = static_cast<unsigned short>(strtoul(psz+1, &psz, 10));
			if (*psz == ' ')
			{
				tim.hour = static_cast<unsigned short>(strtoul(psz+1, &psz, 10));
				if (*psz == ':')
				{
					tim.minute = static_cast<unsigned short>(strtoul(psz+1, &psz, 10));
					if (*psz == ':')
					{
						tim.second = static_cast<unsigned short>(strtoul(psz+1, &psz, 10));
						if (*psz == '.')
						{
							char * pszFrac = psz + 1;
							tim.fraction = strtoul(pszFrac, &psz, 10);
							int cDigits = psz - pszFrac;
							if (cDigits < 10)
								tim.fraction *= nFracFix[cDigits];
							else
								tim.fraction = 0;
						}
					}
					// REVIEW SteveMc: this demands accuracy down to the minute.
					if (!*psz)
						fOk = true;
				}
			}
		}
	}
	if (fOk)
	{
		if(CURRENTDB == MSSQL) {
			stabCmd.Format("UPDATE \"%S\" SET \"%S\" = '%s' WHERE Id=%d;",
					m_pfwxd->ClassName(icls).Chars(),
					m_pfwxd->FieldName(ifld).Chars(), pszVal, hobjOwner);
		}
		else if(CURRENTDB == FB) {
			stabCmd.Format("UPDATE %S SET \"%S\" = '%s' WHERE Id=%d;",
					m_pfwxd->ClassName(icls).Chars(),
					UpperName(m_pfwxd->FieldName(ifld)), pszVal, hobjOwner);
		}
	}
	else
	{
		// "Invalid Time value for the %S field of a %S object: ""%s"".\n"
		StrAnsi staFmt(kstidXmlErrorMsg076);
		StrAnsi sta;
		sta.Format(staFmt.Chars(),
			m_pfwxd->FieldName(ifld).Chars(), m_pfwxd->ClassName(icls).Chars(), pszVal);
		LogMessage(sta.Chars());
	}
}


/*----------------------------------------------------------------------------------------------
	Set the SQL command for storing the value of this GUID property.

	@param stabCmd reference to the string containing the SQL command (output)
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetCommandForGuidValue(StrAnsiBuf & stabCmd,
	const XML_Char ** prgpszAtts, int hobjOwner, int icls, int ifld)
{
	/*
	  <!ELEMENT Guid EMPTY >
	  <!ATTLIST Guid val CDATA #REQUIRED >
	*/
	StrAnsi sta;
	StrAnsi staFmt;
	const char * pszVal = FwXml::GetAttributeValue(prgpszAtts, "val");
	if (!pszVal)
	{
		// "Missing val attribute in Guid element."
		sta.Load(kstidXmlErrorMsg090);
		LogMessage(sta.Chars());
		return;
	}
	GUID guid;
	if (!FwXml::ParseGuid(pszVal, &guid))
	{
		// "Invalid Guid value for the %S field of a %S object: \"%s\"."
		staFmt.Load(kstidXmlErrorMsg075);
		sta.Format(staFmt.Chars(),
			m_pfwxd->FieldName(ifld).Chars(), m_pfwxd->ClassName(icls).Chars(), pszVal);
		LogMessage(sta.Chars());
		return;
	}
	if(CURRENTDB == MSSQL) {
		stabCmd.Format("UPDATE \"%S\" SET \"%S\" = '%g' WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(),
			m_pfwxd->FieldName(ifld).Chars(), &guid, hobjOwner);
	}
	else if(CURRENTDB == FB) {
		stabCmd.Format("UPDATE %S SET \"%S\" = Udf_VarChar_To_Guid('%g') WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(),
			UpperName(m_pfwxd->FieldName(ifld)), &guid, hobjOwner);
	}
}

/*----------------------------------------------------------------------------------------------
	Set the SQL command for storing the value of this GenDate property.

	@param stabCmd reference to the string containing the SQL command (output)
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetCommandForGenDateValue(StrAnsiBuf & stabCmd,
	const XML_Char ** prgpszAtts, int hobjOwner, int icls, int ifld)
{
	/*
	  <!ELEMENT GenDate EMPTY >
	  <!ATTLIST GenDate val CDATA #REQUIRED >
	*/
	StrAnsi sta;
	StrAnsi staFmt;
	const char * pszVal = FwXml::GetAttributeValue(prgpszAtts, "val");
	if (!pszVal)
	{
		// "Missing val attribute in GenDate element."
		sta.Load(kstidXmlErrorMsg089);
		LogMessage(sta.Chars());
		return;
	}
	char * psz;
	int nVal = static_cast<int>(strtol(pszVal, &psz, 10));
	if (*psz || !*pszVal)
	{
		// "Invalid GenDate value for the %S field of a %S object: \"%s\"."
		staFmt.Load(kstidXmlErrorMsg050);
		sta.Format(staFmt.Chars(),
			m_pfwxd->FieldName(ifld).Chars(), m_pfwxd->ClassName(icls).Chars(), pszVal);
		LogMessage(sta.Chars());
		return;
	}
	if(CURRENTDB == MSSQL) {
		stabCmd.Format("UPDATE \"%S\" SET \"%S\" = %d WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(),
			m_pfwxd->FieldName(ifld).Chars(), nVal, hobjOwner);
	}
	else if(CURRENTDB == FB) {
		stabCmd.Format("UPDATE %S SET \"%S\" = %d WHERE Id = %d;",
			m_pfwxd->ClassName(icls).Chars(),
			UpperName(m_pfwxd->FieldName(ifld)), nVal, hobjOwner);
	}
}

/*----------------------------------------------------------------------------------------------
	Prepare for storing the hexadecimary data contained in this Binary or Image property.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::PrepareForBinaryData()
{
	/*
	  <!ELEMENT Binary (#PCDATA) >
	  <!ELEMENT Image (#PCDATA) >
	*/
	m_vchHex.Clear();
	m_fInBinary = true;
}


/*----------------------------------------------------------------------------------------------
	Prepare for storing the data in this string property.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::PrepareForStringData()
{
	/*
	  <!ELEMENT Str (Run)* >
	*/
	XML_SetElementHandler(m_parser, FwXml::HandleStringStartTag, FwXml::HandleStringEndTag);
	m_vbri.Clear();
	m_vrpi.Clear();
	m_vtxip.Clear();
	m_vtxsp.Clear();
	m_stuChars.Clear();
	m_ws = 0;
	m_fInString = true;
}


/*----------------------------------------------------------------------------------------------
	Prepare for storing the data in this multilingual string property.

	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::PrepareForMultiStringData(const XML_Char ** prgpszAtts)
{
	/*
	  <!ELEMENT AStr (Run)* >
	  <!ATTLIST AStr
	  ws CDATA #REQUIRED >
	*/
	m_stuChars.Clear();
	const char * pszWs = FwXml::GetAttributeValue(prgpszAtts, "ws");
	if (!pszWs)
	{
		// "Missing writing system for <AStr> element!"
		StrAnsi sta(kstidXmlErrorMsg080);
		LogMessage(sta.Chars());
		return;
	}
	// "Invalid writing system in <AStr ws=\"%s\">!"
	int ws = GetWsFromIcuLocale(pszWs, kstidXmlErrorMsg058);
//	if (!ws)
//		return;
	XML_SetElementHandler(m_parser, FwXml::HandleStringStartTag,
		FwXml::HandleStringEndTag);
	m_vbri.Clear();
	m_vrpi.Clear();
	m_vtxip.Clear();
	m_vtxsp.Clear();
	m_ws = ws;
	m_fInString = true;
}


/*----------------------------------------------------------------------------------------------
	Prepare for storing the data in this unicode property.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::PrepareForUnicodeData()
{
	/*
	  <!ELEMENT Uni (#PCDATA) >
	*/
	m_stuChars.Clear();
	m_fInUnicode = true;
}


/*----------------------------------------------------------------------------------------------
	Prepare for storing the data in this multilingual unicode property.

	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::PrepareForMultiUnicodeData(const XML_Char ** prgpszAtts)
{
	/*
	  <!ELEMENT AUni (#PCDATA) >
	  <!ATTLIST AUni
	  ws CDATA #REQUIRED >
	*/
	m_stuChars.Clear();
	const char * pszWs = FwXml::GetAttributeValue(prgpszAtts, "ws");
	if (!pszWs)
	{
		// "Missing writing system for <AUni> element!"
		StrAnsi sta(kstidXmlErrorMsg081);
		LogMessage(sta.Chars());
		return;
	}
	// "Invalid writing system in <AUni ws=\"%s\">!"
	int ws = GetWsFromIcuLocale(pszWs, kstidXmlErrorMsg059);
	if (!ws)
		return;
	m_ws = ws;
	m_fInUnicode = true;
}


/*----------------------------------------------------------------------------------------------
	Store the information for this reference property.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
	@param etiProp reference to the Element Type information of the current property
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandleReferenceLink(const XML_Char * pszName,
	const XML_Char ** prgpszAtts, int hobjOwner, int icls, int ifld, ElemTypeInfo & etiProp)
{
	/*
	  <!ELEMENT Link EMPTY >
	  <!ATTLIST Link
	  target IDREF #IMPLIED
	  class  CDATA #IMPLIED
	  ord    CDATA #IMPLIED
	  db     CDATA #IMPLIED
	  ws     CDATA #IMPLIED
	  abbr   CDATA #IMPLIED
	  name   CDATA #IMPLIED
	  wsv    CDATA #IMPLIED
	  wsa    CDATA #IMPLIED
	  sense  CDATA #IMPLIED
	  entry  CDATA #IMPLIED>
	*/
	bool fOk = false;
	const char * pszVal = FwXml::GetAttributeValue(prgpszAtts, "target");
	if (!pszVal)
	{
		fOk = HandleImplicitReferenceLink(pszName, prgpszAtts, hobjOwner, icls, ifld, etiProp);
		if (!fOk)
		{
			// We couldn't find (or create) the reference, and haven't printed any error
			// messages yet.
			// "Missing %<0>s attribute in %<1>s element."
			StrAnsi staFmt(kstidXmlErrorMsg079);
			StrAnsi sta;
			sta.Format(staFmt.Chars(), "target", pszName);
			LogMessage(sta.Chars());
		}
		return;
	}
	const char * pszDb = NULL;
	GUID guid;
	int hobj;
	if (FwXml::ParseGuid(pszVal + 1, &guid))
	{
		fOk = m_hmguidhobj.Retrieve(guid, &hobj);
		if (!fOk)
			pszDb = FwXml::GetAttributeValue(prgpszAtts, "db");
	}
	else
	{
		fOk = m_hmcidhobj.Retrieve(pszVal, &hobj);
	}
	if (fOk)
	{
		StoreReference(prgpszAtts, hobjOwner, ifld, hobj);
	}
	else if (pszDb)
	{
		// TODO SteveMc: evidently a link to an external database -- what gets
		// stored where?
		// "DEBUG: External Link target: db=\"%s\", target=\"%s\""
		StrAnsi staFmt(kstidXmlDebugMsg001);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), pszDb, pszVal);
		LogMessage(sta.Chars());
	}
	else
	{
		// "Missing Link target: \"%s\""
		StrAnsi staFmt(kstidXmlErrorMsg077);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), pszVal);
		LogMessage(sta.Chars());
	}
}


/*----------------------------------------------------------------------------------------------
	Store the information for this reference property, which is implied by various attributes
	since the "target" attribute is not set.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
	@param etiProp reference to the Element Type information of the current property

	@return false if an error should be reported, true if everything is ok
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::HandleImplicitReferenceLink(const XML_Char * pszName,
	const XML_Char ** prgpszAtts, int hobjOwner, int icls, int ifld, ElemTypeInfo & etiProp)
{
	bool fOk = false;
	const char * pszTargetWs = FwXml::GetAttributeValue(prgpszAtts, "ws");
	if (pszTargetWs)
	{
		// "Invalid writing system in <Link ws=""%<0>s"" .../>.\n"
		int ws = GetWsFromIcuLocale(pszTargetWs, kstidXmlErrorMsg157);
		if (!ws)
			return true;	// already produced error message.

		// Try a number of possible implicit targets, starting with the most limited
		// and specific type, and proceeding to the most general type (CmPossibility
		// and its subclasses).  Different types of implicit targets have different,
		// but overlapping sets of attributes.
		fOk = StoreMoInflAffixSlotReference(pszName, prgpszAtts, hobjOwner, ifld, icls, ws);
		if (!fOk)
		{
			fOk = StoreEntryOrSenseLinkInfo(pszName, prgpszAtts, hobjOwner, ifld, icls, ws);
		}
		if (!fOk)
		{
			fOk = StoreReversalEntry(pszName, prgpszAtts, hobjOwner, ifld, icls, ws);
		}
		if (!fOk)
		{
			fOk = StoreMoInflClassReference(pszName, prgpszAtts, hobjOwner, ifld, icls,
				ws);
		}
		if (!fOk)
		{
			fOk = StoreCmPossibilityReference(pszName, prgpszAtts, hobjOwner, ifld, icls, ws);
		}
		if (!fOk)
		{
			fOk = StoreWritingSystemReference(pszName, prgpszAtts, hobjOwner, ifld, icls, ws);
		}
	}
	else
	{
		if (etiProp.m_elty == keltyVirtualProp && etiProp.m_ifld >= 1 && etiProp.m_ifld <= 2)
		{
			fOk = StoreLexicalRelationInfo(pszName, prgpszAtts, hobjOwner, ifld, icls);
		}
		else
		{
			const char * pszForm = FwXml::GetAttributeValue(prgpszAtts, "form");
			if (pszForm)
			{
				fOk = StorePhoneEnvReference(pszName, prgpszAtts, hobjOwner, ifld, icls,
					pszForm);
			}
			else
			{
				const char * pszPath = FwXml::GetAttributeValue(prgpszAtts, "path");
				if (pszPath)
				{
					fOk = StoreFilePathReference(pszName, prgpszAtts, hobjOwner, ifld, icls,
						pszPath);
				}
			}
		}
	}
	return fOk;
}

/*----------------------------------------------------------------------------------------------
	Prepare for storing the data in this styles rule property.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::PrepareForRuleProp(const XML_Char * pszName, const XML_Char ** prgpszAtts)
{
	/*
	  <!ELEMENT Prop (WsStyles9999)? >
	  <!ATTLIST Prop
	  align      CDATA #IMPLIED
	  fontFamily CDATA #IMPLIED
	  fontsize   CDATA #IMPLIED
	  ... >
	  <!ELEMENT WsStyles9999 (Prop)* >
	*/
	XML_SetElementHandler(m_parser, FwXmlImportData::HandlePropStartTag2,
		FwXmlImportData::HandlePropEndTag2);
	m_vtxip.Clear();		// Integer-valued properties for this property.
	m_vtxsp.Clear();		// String-valued properties for this property
	m_fInRuleProp = true;
	m_fInWsStyles = false;
	// Way too many attributes to handle here!
	HandlePropStartTag2(this, pszName, prgpszAtts);
}


/*----------------------------------------------------------------------------------------------
	Handle XML start elements during the first pass.  All objects are created during this pass.

	This static method is passed to the expat XML parser as a callback function.  See the
	comments in xmlparse.h for the XML_StartElementHandler typedef for the documentation
	such as it is.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandleStartTag1(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);

	pxid->ProcessStartTag1(pszName, prgpszAtts);
}

/*----------------------------------------------------------------------------------------------
	Handle XML start tags during the first pass.  All objects are created during this pass.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessStartTag1(const XML_Char * pszName, const XML_Char ** prgpszAtts)
{
	StrAnsi sta;
	StrAnsi staFmt;
	if (m_celemStart == 0)
	{
		if (m_staBeginTag != pszName)
		{
			m_fError = true;
			m_hr = E_INVALIDARG;
			// "Invalid start tag <%<0>s> for XML file: expected <%<1>s>.\n"
			staFmt.Load(kstidXmlErrorMsg311);
			sta.Format(staFmt.Chars(), pszName, m_staBeginTag.Chars());
			LogMessage(sta.Chars());
			ThrowHr(WarnHr(m_hr));
		}
	}
	++m_celemStart;

	m_fCustom = false;		// Assume that we're not defining a custom field.
	try
	{
		ElemTypeInfo eti = GetElementType(pszName);
		switch (eti.m_elty)
		{
		case keltyDatabase:
			ProcessDatabasePass1(eti, prgpszAtts);
			break;
		case keltyAddProps:
			ProcessAddPropsPass1(eti);
			break;
		case keltyDefineProp:
			ProcessDefinePropPass1(eti, prgpszAtts);
			break;
		case keltyObject:
			StartObject1(eti, pszName, prgpszAtts);
			break;
		case keltyCustomProp:
			ProcessCustomPropPass1(eti, pszName, prgpszAtts);
			break;
		case keltyPropName:
			StartPropName1(eti, pszName);
			break;
		case keltyVirtualProp:
			StartPropName1(eti, pszName);
			break;
		case keltyBasicProp:
			StartBasicProp1(eti, pszName);
			break;
		default:
			// "Unknown XML start tag: \"%s\""
			staFmt.Load(kstidXmlErrorMsg128);
			sta.Format(staFmt.Chars(), pszName);
			LogMessage(sta.Chars());
			ThrowHr(WarnHr(E_UNEXPECTED));
			break;
		}
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		staFmt.Load(kstidXmlDebugMsg003);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		staFmt.Load(kstidXmlDebugMsg005);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}


/*----------------------------------------------------------------------------------------------
	Process the start tag for <database> for the first pass.

	@param eti Reference to the basic element type information structure.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessDatabasePass1(ElemTypeInfo & eti, const XML_Char ** prgpszAtts)
{
	StrAnsi sta;
	StrAnsi staFmt;
	if (m_vetiOpen.Size())
	{
		// "<%<0>s> must be the outermost XML element!?"
		staFmt.Load(kstidXmlErrorMsg010);
		sta.Format(staFmt.Chars(), m_staBeginTag.Chars());
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	else
	{
		m_vetiOpen.Push(eti);
		const char * pszVersion = FwXml::GetAttributeValue(prgpszAtts, "version");
		if (pszVersion)
		{
			SqlStatement sstmt;
			RETCODE rc;
			sstmt.Init(m_sdb);

			// Get the version from the database, and make sure it matches.
			if(CURRENTDB == FB || CURRENTDB == MSSQL) {
				sta.Assign("SELECT MAX(DbVer) FROM Version$;");
			}
			rc = SQLExecDirectA(sstmt.Hstmt(),
				reinterpret_cast<SQLCHAR *>(const_cast<char *>(sta.Chars())),
				SQL_NTS);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, sta.Chars());
			long nVerDatabase;
			SDWORD cbT;
			rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &nVerDatabase,
				isizeof(nVerDatabase), &cbT);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			rc = SQLFetch(sstmt.Hstmt());
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			sstmt.Clear();
			long nVerXml = strtol(pszVersion, NULL, 10);
			if (nVerDatabase != nVerXml)
			{
				// "<FwDatabase version=\"%d\"> does not match the database version (%d)."
				staFmt.Load(kstidXmlInfoMsg010);
				sta.Format(staFmt.Chars(), nVerXml, nVerDatabase);
				LogMessage(sta.Chars());
			}
		}
		else
		{
			// "No version number given with <FwDatabase>."
			sta.Load(kstidXmlInfoMsg011);
			LogMessage(sta.Chars());
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Process the start tag <AdditionalFields> for the first pass.

	@param eti Reference to the basic element type information structure.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessAddPropsPass1(ElemTypeInfo & eti)
{
	if (m_vetiOpen.Size() != 1 || m_vetiOpen[0].m_elty != keltyDatabase)
	{
		// "<AdditionalFields> must be a toplevel element inside <FwDatabase>!?"
		StrAnsi sta(kstidXmlErrorMsg008);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	m_vetiOpen.Push(eti);
}

/*----------------------------------------------------------------------------------------------
	Process the start tag for <CustomField> for the first pass.

	@param eti Reference to the basic element type information structure.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessDefinePropPass1(ElemTypeInfo & eti, const XML_Char ** prgpszAtts)
{
	if (m_vetiOpen.Size() != 2 || m_vetiOpen[1].m_elty != keltyAddProps)
	{
		// "<CustomField> must be a toplevel element inside <AdditionalFields>!?"
		ThrowWithLogMessage(kstidXmlErrorMsg009);
	}
	// Add this custom field to the database schema.
	const char * pszProp = FwXml::GetAttributeValue(prgpszAtts, "name");
	const char * pszClass = FwXml::GetAttributeValue(prgpszAtts, "class");
	const char * pszType = FwXml::GetAttributeValue(prgpszAtts, "type");
	if (pszProp && pszClass && pszType)
	{
		// Update the schema in memory.
		CustomFieldInfo cfi;
		cfi.m_fid = GetCustomFieldFlid(FwXml::GetAttributeValue(prgpszAtts, "flid"));
		StrUtil::StoreUtf16FromUtf8(pszProp, strlen(pszProp), cfi.m_stuName);
		cfi.m_cid = GetCustomFieldClid(pszClass);
		cfi.m_fCustom = 1;		// redundant implicit info.
		cfi.m_cidDst = GetCustomFieldTargetClid(FwXml::GetAttributeValue(prgpszAtts, "target"));
		cfi.m_cpt = GetCustomFieldType(pszType);
		cfi.m_stabMin = GetCustomFieldMin(FwXml::GetAttributeValue(prgpszAtts, "min"));
		cfi.m_stabMax = GetCustomFieldMax(FwXml::GetAttributeValue(prgpszAtts, "max"));
		cfi.m_stabBig = GetCustomFieldBig(FwXml::GetAttributeValue(prgpszAtts, "big"));
		SetCustomFieldUserLabel(cfi.m_stuUserLabel,
			FwXml::GetAttributeValue(prgpszAtts, "userLabel"));
		SetCustomFieldHelpString(cfi.m_stuHelpString,
			FwXml::GetAttributeValue(prgpszAtts, "helpString"));
		cfi.m_stabListRootId = GetCustomFieldListRootId(
			FwXml::GetAttributeValue(prgpszAtts, "listRootId"));
		cfi.m_stabWsSelector = GetCustomFieldWsSelector(
			FwXml::GetAttributeValue(prgpszAtts, "wsSelector"));
		m_vcfi.Push(cfi);
		m_fCustom = true;
		++m_cCustom;
		// Store the custom field data in memory, since we need some of this info during the
		// first pass.
		int ifld = m_pfwxd->FieldCount();
		FwDbFieldInfo fdfi;
		fdfi.fid = cfi.m_fid;		// This must be fixed later.
		fdfi.cpt = cfi.m_cpt;
		fdfi.cid = cfi.m_cid;
		fdfi.cidDst = cfi.m_cidDst;
		fdfi.fCustom = TRUE;
		// TODO: store the other fields when needed.
		m_pfwxd->m_vfdfi.Push(fdfi);
		m_pfwxd->m_vstufld.Push(cfi.m_stuName);
		m_pfwxd->m_mmsuifld.Insert(cfi.m_stuName, ifld);
		StrUni stuXml;					// Implied (virtual) XML name.
		stuXml.Format(L"%s%d", cfi.m_stuName.Chars(), cfi.m_cid);
		m_pfwxd->m_vstufldXml.Push(stuXml);
		m_pfwxd->m_hmsuXmlifld.Insert(stuXml, ifld);
		m_vcfld.Resize(m_pfwxd->m_vstufld.Size());
	}
	else
	{
		// "Missing %<0>s attribute for %<1>s element."
		StrAnsi staFmt(kstidXmlErrorMsg079);
		StrAnsi sta;
		if (!pszProp)
		{
			sta.Format(staFmt.Chars(), "name", "CustomField");
			LogMessage(sta.Chars());
		}
		if (!pszType)
		{
			sta.Format(staFmt.Chars(), "type", "CustomField");
			LogMessage(sta.Chars());
		}
		if (!pszClass)
		{
			sta.Format(staFmt.Chars(), "class", "CustomField");
			LogMessage(sta.Chars());
		}
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	m_vetiOpen.Push(eti);
}


/*----------------------------------------------------------------------------------------------
	Convert the attribute value to a database field id for the custom field.

	@param pszFlid value of the "flid" attribute
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetCustomFieldFlid(const char * pszFlid)
{
	if (pszFlid != NULL)
	{
		char * psz;
		return strtol(pszFlid, &psz, 10);
	}
	else
	{
		return 0;
	}
}


/*----------------------------------------------------------------------------------------------
	Convert the attribute value into a database class id for the custom field.

	@param pszClass value of the "class" attribute
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetCustomFieldClid(const char * pszClass)
{
	StrUni stuClass(pszClass);
	int icls;
	if (!m_pfwxd->m_hmsuicls.Retrieve(stuClass, &icls))
	{
		// "Invalid class attribute for CustomField element: %s"
		ThrowWithLogMessage(kstidXmlErrorMsg057, pszClass);
	}
	return m_pfwxd->ClassInfo(icls).cid;
}


/*----------------------------------------------------------------------------------------------
	Convert the attribute value into a database destination class id for the custom field.

	@param pszTarget value of the "target" attribute
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetCustomFieldTargetClid(const char * pszTarget)
{
	if (pszTarget)
	{
		StrUni stuTarget(pszTarget);
		int iclsDst;
		if (!m_pfwxd->m_hmsuicls.Retrieve(stuTarget, &iclsDst))
		{
			// "Invalid target attribute for CustomField element: %s"
			ThrowWithLogMessage(kstidXmlErrorMsg063, pszTarget);
		}
		return m_pfwxd->ClassInfo(iclsDst).cid;
	}
	else
	{
		return 0;
	}
}


/*----------------------------------------------------------------------------------------------
	Convert the attribute value into a data type for the custom field.

	@param pszType value of the "type" attribute
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetCustomFieldType(const char * pszType)
{
	if (!strcmp(pszType, "Boolean"))
		return kcptBoolean;
	else if (!strcmp(pszType, "Integer"))
		return kcptInteger;
	else if (!strcmp(pszType, "Numeric"))
		return kcptNumeric;
	else if (!strcmp(pszType, "Float"))
		return kcptFloat;
	else if (!strcmp(pszType, "Time"))
		return kcptTime;
	else if (!strcmp(pszType, "Guid"))
		return kcptGuid;
	else if (!strcmp(pszType, "Image"))
		return kcptImage;
	else if (!strcmp(pszType, "GenDate"))
		return kcptGenDate;
	else if (!strcmp(pszType, "Binary"))
		return kcptBinary;
	else if (!strcmp(pszType, "String"))
		return kcptString;
	else if (!strcmp(pszType, "MultiString"))
		return kcptMultiString;
	else if (!strcmp(pszType, "Unicode"))
		return kcptUnicode;
	else if (!strcmp(pszType, "MultiUnicode"))
		return kcptMultiUnicode;
	else if (!strcmp(pszType, "BigString"))
		return kcptBigString;
	else if (!strcmp(pszType, "MultiBigString"))
		return kcptMultiBigString;
	else if (!strcmp(pszType, "BigUnicode"))
		return kcptBigUnicode;
	else if (!strcmp(pszType, "MultiBigUnicode"))
		return kcptMultiBigUnicode;
	else if (!strcmp(pszType, "OwningAtom"))
		return kcptOwningAtom;
	else if (!strcmp(pszType, "ReferenceAtom"))
		return kcptReferenceAtom;
	else if (!strcmp(pszType, "OwningCollection"))
		return kcptOwningCollection;
	else if (!strcmp(pszType, "ReferenceCollection"))
		return kcptReferenceCollection;
	else if (!strcmp(pszType, "OwningSequence"))
		return kcptOwningSequence;
	else if (!strcmp(pszType, "ReferenceSequence"))
		return kcptReferenceSequence;
	else
	{
		// "Invalid type attribute for CustomField element: %s"
		ThrowWithLogMessage(kstidXmlErrorMsg064, pszType);
		return 0;
	}
}


/*----------------------------------------------------------------------------------------------
	Convert the attribute value into a minimum value (string) for the custom field.

	@param pszMin value of the "min" attribute
----------------------------------------------------------------------------------------------*/
const char * FwXmlImportData::GetCustomFieldMin(const char * pszMin)
{
	if (pszMin && *pszMin)
	{
		char * psz;
		strtol(pszMin, &psz, 10);
		if (psz && *psz)
		{
			// Throw error?
		}
		return pszMin;
	}
	else
		return "";
}


/*----------------------------------------------------------------------------------------------
	Convert the attribute value into a maximum value (string) for the custom field.

	@param pszMin value of the "max" attribute
----------------------------------------------------------------------------------------------*/
const char * FwXmlImportData::GetCustomFieldMax(const char * pszMax)
{
	if (pszMax && *pszMax)
	{
		char * psz;
		strtol(pszMax, &psz, 10);
		if (psz && *psz)
		{
			// Throw error?
		}
		return pszMax;
	}
	else
	{
		return "";
	}
}


/*----------------------------------------------------------------------------------------------
	Convert the attribute value into a Big flag value (string) for the custom field.

	@param pszMin value of the "big" attribute
----------------------------------------------------------------------------------------------*/
const char * FwXmlImportData::GetCustomFieldBig(const char * pszBig)
{
	if (pszBig && *pszBig)
	{
		char * psz;
		long nBig = strtol(pszBig, &psz, 10);
		if (psz && *psz || nBig < 0 || nBig > 1)
		{
			// Throw error?
		}
		return pszBig;
	}
	else
	{
		return "";
	}
}


/*----------------------------------------------------------------------------------------------
	Set the user label string for the custom field from the attribute value.

	@param stuUserLabel Reference to the custom field user label string.
	@param pszUserLabel value of the "userLabel" attribute
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetCustomFieldUserLabel(StrUni & stuUserLabel, const char * pszUserLabel)
{
	if (pszUserLabel && *pszUserLabel)
		StrUtil::StoreUtf16FromUtf8(pszUserLabel, strlen(pszUserLabel), stuUserLabel);
	else
		stuUserLabel.Clear();
}


/*----------------------------------------------------------------------------------------------
	Set the help string for the custom field from the attribute value.

	@param stuHelp Reference to the custom field help string.
	@param pszHelpString value of the "userLabel" attribute
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetCustomFieldHelpString(StrUni & stuHelp, const char * pszHelpString)
{
	if (pszHelpString && *pszHelpString)
		StrUtil::StoreUtf16FromUtf8(pszHelpString, strlen(pszHelpString), stuHelp);
	else
		stuHelp.Clear();
}


/*----------------------------------------------------------------------------------------------
	Convert the attribute value into a list root object id (string) for the custom field.

	@param pszListRootId value of the "listRootId" attribute
----------------------------------------------------------------------------------------------*/
const char * FwXmlImportData::GetCustomFieldListRootId(const char * pszListRootId)
{
	if (pszListRootId && *pszListRootId)
	{
		GUID guidList;
		bool fOk = FwXml::ParseGuid(pszListRootId + 1, &guidList);
		if (!fOk)
		{
			// Throw error?
		}
		// Is there more error checking we should do for this?
		return pszListRootId;
	}
	else
	{
		return "";
	}
}


/*----------------------------------------------------------------------------------------------
	Convert the attribute value into a writing system selector for the custom field.

	@param pszWsSelector value of the "wsSelector" attribute
----------------------------------------------------------------------------------------------*/
const char * FwXmlImportData::GetCustomFieldWsSelector(const char * pszWsSelector)
{
	if (pszWsSelector && *pszWsSelector)
	{
		char * psz;
		long nWsSel = strtol(pszWsSelector, &psz, 10);
		if (psz && *psz || nWsSel >= 0)
		{
			// Throw error?
		}
		return pszWsSelector;
	}
	else
	{
		return "";
	}
}


/*----------------------------------------------------------------------------------------------
	Process the start tag for <Custom>, <CustomStr>, <CustomLink>, or <CustomObj> for the first
	pass.

	@param eti Reference to the basic element type information structure.
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessCustomPropPass1(ElemTypeInfo & eti, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	StrAnsiBuf stabCustom;
	const char * pszProp = FwXml::GetAttributeValue(prgpszAtts, "name");
	if (!pszProp)
	{
		// "Missing %<0>s attribute for %<1>s element."
		StrAnsi staFmt(kstidXmlErrorMsg079);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), "name", pszName);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	stabCustom.Format("%s name=\"%s\"", pszName, pszProp);
	pszName = stabCustom.Chars();	// For error messages  in StartPropName1().

	StartPropName1(eti, pszName, pszProp);
}

/*----------------------------------------------------------------------------------------------
	Handle XML end elements during the first pass.

	This static method is passed to the expat XML parser as a callback function.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandleEndTag1(void * pvUser, const XML_Char * pszName)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);

	pxid->ProcessEndTag1(pszName);
}

/*----------------------------------------------------------------------------------------------
	Handle XML end elements during the first pass.

	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessEndTag1(const XML_Char * pszName)
{
	++m_celemEnd;
	ElemTypeInfo eti;
	if (!m_vetiOpen.Pop(&eti))
	{
		if (m_iclsOwner != -1 && m_staOwnerBeginTag == pszName)
		{
			// If importing an object, ignore an outermost object element that matches the
			// owner's class.
			return;
		}
		// THIS SHOULD NEVER HAPPEN! -- "Unbalanced XML element stack!?"
		SetErrorWithMessage(kstidXmlErrorMsg123);
	}
	Assert(!m_flid || !m_fSingle);
	Assert(!m_flid || !m_fIcuLocale);
	switch (eti.m_elty)
	{
	case keltyDatabase:
		Assert(!m_vetiOpen.Size());
		break;
	case keltyAddProps:
		break;
	case keltyDefineProp:
		// Store the XmlUI column data, if any.
		if (m_fCustom && m_staChars.Length())
		{
			Assert(m_cCustom);
			Assert(m_vcfi.Size());
			StrUtil::StoreUtf16FromUtf8(m_staChars.Chars(), m_staChars.Length(),
				m_vcfi.Top()->m_stuXmlUI);
		}
		m_staChars.Clear();
		m_fCustom = false;
		break;
	case keltyObject:
		if (!m_vhobjOpen.Pop())
		{
			// THIS SHOULD NEVER HAPPEN! -- "Unbalanced object id stack!?"
			SetErrorWithMessage(kstidXmlErrorMsg125);
		}
		break;
	case keltyCustomProp:
	case keltyPropName:
	case keltyVirtualProp:
		if (!m_vspiOpen.Pop())
		{
			// THIS SHOULD NEVER HAPPEN! -- "Unbalanced property name stack!?"
			SetErrorWithMessage(kstidXmlErrorMsg126);
		}
		break;
	case keltyBasicProp:
		Assert(m_vetiOpen.Size() >= 3);
		if (eti.m_cpt == kcptUnicode && m_fIcuLocale)
		{
			// Handle associating an ICU Locale UTF-8 string to its WritingSystem hobj.
#ifdef DEBUG
			Assert(!strcmp(pszName, "Uni"));
			ElemTypeInfo & etiProp = m_vetiOpen[m_vetiOpen.Size() - 1];
			Assert(etiProp.m_elty == keltyPropName);
			Assert(m_pfwxd->FieldInfo(etiProp.m_ifld).fid == kflidLgWritingSystem_ICULocale);
			ElemTypeInfo & etiObj = m_vetiOpen[m_vetiOpen.Size() - 2];
			Assert(etiObj.m_elty == keltyObject);
			Assert(m_pfwxd->ClassInfo(etiObj.m_icls).cid == kclidLgWritingSystem);
			Assert(m_vhobjOpen.Size());
#endif
			int hobj = *m_vhobjOpen.Top();
			m_staChars.ToLower();
			m_hmcws.Insert(m_staChars.Chars(), hobj);
			m_fIcuLocale = false;
			m_staChars.Clear();
		}
		Assert(!m_fIcuLocale);
		break;
	default:
		// THIS SHOULD NEVER HAPPEN! -- "INTERNAL XML ELEMENT STACK CORRUPTED!?"
		SetErrorWithMessage(kstidXmlErrorMsg041);
		break;
	}
	// If importing objects, pop off the extra element pushed for the start tag.
	if (m_iclsOwner != -1 && m_vetiOpen.Size() == 1 && m_staBeginTag == pszName)
	{
		m_vetiOpen.Pop(&eti);
		Assert(eti.m_elty == keltyDatabase);
	}
}


/*----------------------------------------------------------------------------------------------
	Log the message given by the resource string id, and set the error flags.

	@param stid resource id of a message string
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetErrorWithMessage(int stid)
{
	StrAnsi sta(stid);
	LogMessage(sta.Chars());
	m_fError = true;
	m_hr = E_UNEXPECTED;
}

/*----------------------------------------------------------------------------------------------
	Handle XML start tags inside <Prop> elements during the first pass.

	This static method is passed to the expat XML parser as a callback function.  It is used
	when the start tag for <Prop> or <WsStyles9999> is detected.  See the comments in xmlparse.h
	for the XML_StartElementHandler typedef for the documentation such as it is.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandlePropStartTag1(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);

	pxid->ProcessPropStartTag1(pszName,	prgpszAtts);
}

/*----------------------------------------------------------------------------------------------
	Handle XML start tags inside <Prop> elements during the first pass.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessPropStartTag1(const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	StrAnsi sta;
	StrAnsi staFmt;

	try
	{
		if (m_fInWsStyles)
		{
			if (!strcmp(pszName, "WsProp"))
			{
				// Do nothing on this pass.
			}
			else
			{
				// "<%s> not recognized nested within <WsStyles9999>!"
				ThrowWithLogMessage(kstidXmlErrorMsg137, pszName);
			}
		}
		else if (m_fInRuleProp)
		{
			if (!strcmp(pszName, "WsStyles9999"))
			{
				m_fInWsStyles = true;		// Do nothing else on this pass.
			}
			else if (!strcmp(pszName, "BulNumFontInfo"))
			{
				// Do nothing else on this pass.
			}
			else
			{
				// "<%s> not recognized nested within <Prop>!"
				ThrowWithLogMessage(kstidXmlErrorMsg138, pszName);
			}
		}
		else
		{
			Assert(false);		// THIS SHOULD NEVER HAPPEN!
		}
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		staFmt.Load(kstidXmlDebugMsg003);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		staFmt.Load(kstidXmlDebugMsg005);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML end tags inside <Prop> elements during the first pass.

	This static method is passed to the expat XML parser as a callback function.  It is used
	when the end tag for <Prop> or <WsStyles9999> is detected.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandlePropEndTag1(void * pvUser, const XML_Char * pszName)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);

	pxid->ProcessPropEndTag1(pszName);
}

/*----------------------------------------------------------------------------------------------
	Handle XML end tags inside <Prop> elements during the first pass.

	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessPropEndTag1(const XML_Char * pszName)
{
	if (!strcmp(pszName, "WsStyles9999"))
	{
		m_fInWsStyles = false;	// Do nothing else on this pass.
	}
	else if (!strcmp(pszName, "WsProp"))
	{
		// Do nothing on this pass.
	}
	else if (!strcmp(pszName, "BulNumFontInfo"))
	{
		// Do nothing else on this pass.
	}
	else if (!strcmp(pszName, "Prop"))
	{
		if (!m_fInWsStyles)
		{
			XML_SetElementHandler(m_parser, m_startOuterHandler, m_endOuterHandler);
			m_fInRuleProp = false;
			m_fInWsStyles = false;
			(*m_endOuterHandler)(this, pszName);
			return;
		}
	}
	else
	{
		// We should have already complained about this invalid element.
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML start tags inside <Str> and <AStr> elements during the first pass.

	This static method is passed to the expat XML parser as a callback function.  It is used
	when the start tag for either <Str> or <AStr> is detected.  See the comments in xmlparse.h
	for the XML_StartElementHandler typedef for the documentation such as it is.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandleStringStartTag1(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);

	pxid->ProcessStringStartTag1(pszName, prgpszAtts);
}

/*----------------------------------------------------------------------------------------------
	Handle XML start tags inside <Str> and <AStr> elements during the first pass.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessStringStartTag1(const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	StrAnsi sta;
	StrAnsi staFmt;
	try
	{
		if (!strcmp(pszName, "AStr") || !strcmp(pszName, "Str"))
		{
			// <AStr ws="ENG">...</AStr>
			// <Str>...</Str>
			// "<%s> elements cannot be nested inside either <Str> or <AStr>!"
			ThrowWithLogMessage(kstidXmlErrorMsg001, pszName);
		}
		else if (!strcmp(pszName, "Run"))
		{
			// Do nothing on this pass.
		}
		else
		{
			// "<%s> not recognized nested within either <Str> or <AStr>!"
			ThrowWithLogMessage(kstidXmlErrorMsg007, pszName);
		}
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		staFmt.Load(kstidXmlDebugMsg003);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		staFmt.Load(kstidXmlDebugMsg005);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML end tags inside <Str> and <AStr> elements during the first pass.

	This static method is passed to the expat XML parser as a callback function.  It is used
	when the start tag for either <Str> or <AStr> is detected.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandleStringEndTag1(void * pvUser, const XML_Char * pszName)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);

	pxid->ProcessStringEndTag1(pszName);
}

/*----------------------------------------------------------------------------------------------
	Handle XML end tags inside <Str> and <AStr> elements during the first pass.

	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessStringEndTag1(const XML_Char * pszName)
{
	if (!strcmp(pszName, "Str") || !strcmp(pszName, "AStr"))
	{
		XML_SetElementHandler(m_parser, m_startOuterHandler, m_endOuterHandler);
		(*m_endOuterHandler)(this, pszName);
	}
	else if (!strcmp(pszName, "Run"))
	{
		// Do nothing.
	}
	else
	{
		// We should have already complained about this invalid element.
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML character data during the first pass.

	This static method is passed to the expat XML parser as a callback function.  See the
	comments in xmlparse.h for the XML_CharacterDataHandler typedef for the documentation
	such as it is.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param prgch Pointer to an array of character data; not NUL-terminated.
	@param cch Number of characters (bytes) in prgch.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandleCharData1(void * pvUser, const XML_Char * prgch, int cch)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);

	pxid->ProcessCharData1(prgch, cch);
}

/*----------------------------------------------------------------------------------------------
	Handle XML character data during the first pass.

	@param prgch Pointer to an array of character data; not NUL-terminated.
	@param cch Number of characters (bytes) in prgch.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessCharData1(const XML_Char * prgch, int cch)
{
	if (!prgch || !cch)
		return;

	if (m_fIcuLocale || m_fCustom)
		m_staChars.Append(prgch, cch);
}


/*----------------------------------------------------------------------------------------------
	Handle XML start elements during the second pass.

	This static method is passed to the expat XML parser as a callback function.  See the
	comments in xmlparse.h for the XML_StartElementHandler typedef for the documentation
	such as it is.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandleStartTag2(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);

	pxid->ProcessStartTag2(pszName, prgpszAtts);
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements during the second pass.  All objects must have been created during
	the first pass.  Also, all structural errors must have been detected so that we can assert
	that everything is okay during this pass.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessStartTag2(const XML_Char * pszName, const XML_Char ** prgpszAtts)
{
	++m_celemStart;
	try
	{
		StrAnsiBuf stabCmd;
		ElemTypeInfo eti = GetElementType(pszName);
		switch (eti.m_elty)
		{
		case keltyDatabase:
			Assert(!m_vetiOpen.Size());
			m_vetiOpen.Push(eti);
			break;
		case keltyAddProps:
			Assert(m_vetiOpen.Size() == 1);
			m_vetiOpen.Push(eti);
			break;
		case keltyDefineProp:
			Assert(m_vetiOpen.Size() == 2);
			m_vetiOpen.Push(eti);
			break;
		case keltyObject:
			StartObject2(eti, pszName);
			break;
		case keltyCustomProp:
			StartCustomProp2(eti, pszName, prgpszAtts);
			break;
		case keltyPropName:
			StartPropName2(eti);
			break;
		case keltyVirtualProp:
			StartVirtualProp2(eti, pszName);
			break;
		case keltyBasicProp:
			StartBasicProp2(eti, pszName, prgpszAtts, stabCmd);
			break;
		default:
			// THIS SHOULD NEVER HAPPEN! -- "Unknown XML start tag: \"%s\""
			ThrowWithLogMessage(kstidXmlErrorMsg128, pszName);
			break;
		}
		BatchSqlCommand(stabCmd);
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		StrAnsi staFmt(kstidXmlDebugMsg003);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		StrAnsi staFmt(kstidXmlDebugMsg005);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Possibly execute the SQL commands stored in m_staCmd, and add the command in stabCmd to
	m_staCmd to execute later.

	@param stabCmd Reference to a possibly empty SQL command string
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::BatchSqlCommand(const StrAnsiBuf & stabCmd)
{
	if (stabCmd.Length())
	{
		int cchCmd = m_staCmd.Length();
		if (!cchCmd)
		{
			m_staCmd = stabCmd.Chars();
		}
		else
		{
#ifdef MAXIMUM_BATCHING
			// oPTIMIZE SteveMc: BATCHING THESE UPDATE COMMANDS TOGETHER SUDDENLY QUIT
			// WORKING RELIABLY.  SOME INDETERMINATE NUMBER OF COMMANDS WOULD EXECUTE, BUT
			// NOT ALL OF THE COMMANDS IN THE BATCH.  THIS LEADS TO VARIOUS BIZARRE STATES
			// IN THE LOADED DATABASE!!
			cchCmd += stabCmd.Length() + 7;
//			if (cchCmd < IMPORTCMDSTRING::kcchMaxStr)			// FAILS with " %s"
//			if (cchCmd < IMPORTCMDSTRING::kcchMaxStr / 2)		// FAILS with " %s"
//			if (cchCmd < IMPORTCMDSTRING::kcchMaxStr / 8)		// WORKS with "\n %s"
//			if (cchCmd < IMPORTCMDSTRING::kcchMaxStr / 4)		// FAILS with "\n %s"
			if (cchCmd < IMPORTCMDSTRING::kcchMaxStr / 8)
			{
				m_staCmd.FormatAppend("\n %s", stabCmd.Chars());
			}
			else
#endif
			{
				UpdateDatabaseObjects(m_sdb, __LINE__);
				m_staCmd = stabCmd.Chars();
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Handle XML end elements during the second pass.

	This static method is passed to the expat XML parser as a callback function.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandleEndTag2(void * pvUser, const XML_Char * pszName)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);

	pxid->ProcessEndTag2(pszName);
}

/*----------------------------------------------------------------------------------------------
	Handle XML end elements during the second pass.

	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessEndTag2(const XML_Char * pszName)
{
	try
	{
		ElemTypeInfo eti;
		int hobj;
		if (!m_vetiOpen.Pop(&eti))
		{
			if (m_iclsOwner != -1 && m_staOwnerBeginTag == pszName)
			{
				// If importing an object, ignore an outermost object element that matches the
				// owner's class.
				return;
			}
			// THIS SHOULD NEVER HAPPEN! -- "Unbalanced element stack!?"
			ThrowWithLogMessage(kstidXmlErrorMsg124);
		}
#ifdef DEBUG
		ElemTypeInfo etiEnd = GetElementType(pszName);
		Assert(etiEnd.m_elty == eti.m_elty || (m_flid && eti.m_elty == keltyDatabase));
		Assert(etiEnd.m_icls == eti.m_icls || eti.m_elty == keltyCustomProp);
#endif
		switch (eti.m_elty)
		{
		case keltyDatabase:
			// REVIEW SteveMc: is there anything to do here?
			break;
		case keltyAddProps:
		case keltyDefineProp:
			// Nothing to do for these elements -- they are handled in the first pass.
			break;
		case keltyObject:
			if (!m_vhobjOpen.Pop(&hobj))
			{
				// THIS SHOULD NEVER HAPPEN! -- "Unbalanced object id stack!?"
				ThrowWithLogMessage(kstidXmlErrorMsg125);
			}
			break;
		case keltyCustomProp:
		case keltyPropName:
		case keltyVirtualProp:
			// REVIEW SteveMc: is there anything more to do with any of these?
			break;
		case keltyBasicProp:
			ProcessBasicPropPass2(pszName, eti);
			break;
		default:
			// THIS SHOULD NEVER HAPPEN!
			// "Unknown XML end tag: \"%s\""
			ThrowWithLogMessage(kstidXmlErrorMsg127, pszName);
			break;
		}
		// If importing objects, pop off the extra element pushed for the start tag.
		if (m_iclsOwner != -1 && m_vetiOpen.Size() == 1 && m_staBeginTag == pszName)
		{
			m_vetiOpen.Pop(&eti);
			Assert(eti.m_elty == keltyDatabase);
		}
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		StrAnsi staFmt(kstidXmlDebugMsg003);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		StrAnsi staFmt(kstidXmlDebugMsg005);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}


/*----------------------------------------------------------------------------------------------
	Handle XML end tags for basic properties (</Boolean>, etc) during the second pass.

	@param pszName XML element name read from the input file.
	@param eti reference to the element type information popped off the open element stack
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessBasicPropPass2(const XML_Char * pszName, ElemTypeInfo & eti)
{
	switch (eti.m_cpt)
	{
	case kcptBoolean:
	case kcptInteger:
	case kcptNumeric:
	case kcptFloat:
	case kcptTime:
	case kcptGuid:
	case kcptGenDate:
	case kcptReferenceAtom:
		return;		// Already handled.
	default:
		break;
	}

	Assert(m_vhobjOpen.Size());
	Assert(m_vetiOpen.Size() >= 3);
	Assert(m_vetiOpen[m_vetiOpen.Size() - 2].m_elty == keltyObject);

	int hobjOwner = m_vhobjOpen[m_vhobjOpen.Size() - 1];
	ElemTypeInfo & etiProp = m_vetiOpen[m_vetiOpen.Size() - 1];
	Assert(etiProp.m_elty == keltyPropName || etiProp.m_elty == keltyCustomProp ||
		etiProp.m_elty == keltyVirtualProp);

	int ifld = etiProp.m_ifld;
	int icls = GetClassIndexFromFieldIndex(ifld);

	// Store the attribute value.
	switch (eti.m_cpt)
	{
	case kcptBinary:
	case kcptImage:
		StoreBinaryData(pszName, eti, icls, ifld, hobjOwner);
		break;
	case kcptString:				// May actually be kcptBigString.
		StoreStringData(pszName, icls, ifld, hobjOwner);
		break;
	case kcptMultiString:			// May actually be kcptMultiBigString.
		StoreMultiStringData(pszName, icls, ifld, hobjOwner);
		break;
	case kcptUnicode:
		StoreUnicodeData(pszName, icls, ifld, hobjOwner);
		break;
	case kcptMultiUnicode:			// May actually be kcptMultiBigUnicode.
		StoreMultiUnicodeData(pszName, icls, ifld, hobjOwner);
		break;
	case kcptRuleProp:
		StoreRulePropData(pszName, icls, ifld, hobjOwner);
		break;
	default:
		Assert(false);				// THIS SHOULD NEVER HAPPEN!
		break;
	}
}


/*----------------------------------------------------------------------------------------------
	Store the data accumulated for a <Binary> or <Image> element.

	@param pszName XML element name read from the input file.
	@param eti reference to the element type information popped off the open element stack
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
	@param hobjOwner Database id of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreBinaryData(const XML_Char * pszName, ElemTypeInfo & eti, int icls,
	int ifld, int hobjOwner)
{
	StrAnsi sta;
	StrAnsi staFmt;
	int cch = m_vchHex.Size();
	if (!cch)
	{
		// "Empty <%s> element?"
		staFmt.Load(kstidXmlErrorMsg031);
		sta.Format(staFmt.Chars(), pszName);
		LogMessage(sta.Chars());
	}
	else
	{
#define BIN_SIZE 8000
		byte * prgbBin;
		byte rgbBin[BIN_SIZE];
		Vector<byte> vbBin;
		// Conservative approximation, ignoring possible whitespace.
		int cbBin = cch / 2;
		if (cbBin <= isizeof(rgbBin))
		{
			prgbBin = rgbBin;
		}
		else
		{
			vbBin.Resize(cbBin);
			prgbBin = vbBin.Begin();
		}
		cbBin = ConvertHexStringToByteArray(prgbBin, m_vchHex.Begin(), cch, pszName);
		if (!cbBin)
		{
			// "Empty <%s> element?"
			staFmt.Load(kstidXmlErrorMsg031);
			sta.Format(staFmt.Chars(), pszName);
			LogMessage(sta.Chars());
		}
		else
		{
			long cbBin2 = cbBin;
			StrAnsiBuf stabCmd;
			RETCODE rc;
			SqlStatement sstmt;
			sstmt.Init(m_sdb);
			if(CURRENTDB == MSSQL) {
				stabCmd.Format("UPDATE \"%S\" SET \"%S\" = ? WHERE Id = %d;",
					m_pfwxd->ClassName(icls).Chars(),
					m_pfwxd->FieldName(ifld).Chars(), hobjOwner);
			}
			else if(CURRENTDB == FB) {
				stabCmd.Format("UPDATE %S SET \"%S\" = ? WHERE Id = %d;",
					m_pfwxd->ClassName(icls).Chars(),
					UpperName(m_pfwxd->FieldName(ifld)), hobjOwner);
			}
			if (eti.m_cpt == kcptImage)
			{
				rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_BINARY,
					SQL_LONGVARBINARY, cbBin, 0, prgbBin, cbBin, &cbBin2);
			}
			else
			{
				rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_BINARY,
					SQL_VARBINARY, cbBin, 0, prgbBin, cbBin, &cbBin2);
			}
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			rc = SQLExecDirectA(sstmt.Hstmt(),
				reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())),
				SQL_NTS);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
			sstmt.Clear();
			stabCmd.Clear();
			if (prgbBin != rgbBin)
				vbBin.Clear();
		}
	}
	m_vchHex.Clear();
	m_fInBinary = false;
}


/*----------------------------------------------------------------------------------------------
	Convert the hexadecimal character string (possibly with internal whitespace) into the
	corresponding binary data.

	@param prgbBin pointer to output buffer
	@param prgchHex pointer to the hexadecimal character string
	@param cch length of the hexadecimal character string
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::ConvertHexStringToByteArray(byte * prgbBin, const char * prgchHex, int cch,
	const XML_Char * pszName)
{
	int ib;
	int ich;
	byte ch;
	byte bT;
	for (ib = 0, ich = 0; ich < cch; )
	{
		// Read the first Hex digit of the byte.  It may be preceded by one or
		// more whitespace characters.
		do
		{
			ch = prgchHex[ich];
			++ich;
			if (!isascii(ch))
				ThrowHr(WarnHr(E_UNEXPECTED));
		} while (isspace(ch) && ich < cch);
		if (ich == cch)
		{
			if (!isspace(ch))
			{
				// "Warning: ignoring extra character at the end of %s data."
				StrAnsi staFmt(kstidXmlErrorMsg134);
				StrAnsi sta;
				sta.Format(staFmt.Chars(), pszName);
				LogMessage(sta.Chars());
			}
			break;
		}
		if (!isxdigit(ch))
			ThrowHr(WarnHr(E_UNEXPECTED));

		if (isdigit(ch))
			bT = static_cast<byte>((ch & 0xF) << 4);
		else
			bT = static_cast<byte>(((ch & 0xF) + 9) << 4);

		// Read the second Hex digit of the byte.
		ch = prgchHex[ich];
		++ich;
		if (!isascii(ch) || !isxdigit(ch))
			ThrowHr(WarnHr(E_UNEXPECTED));

		if (isdigit(ch))
			bT |= ch & 0xF;
		else
			bT |= (ch & 0xF) + 9;
		prgbBin[ib] = bT;
		++ib;
	}
	return ib;
}


/*----------------------------------------------------------------------------------------------
	Store the data accumulated for a <Str> element.

	@param pszName XML element name read from the input file.
	@param eti reference to the element type information popped off the open element stack
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
	@param hobjOwner Database id of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreStringData(const XML_Char * pszName, int icls, int ifld,
	int hobjOwner)
{
	long cbtext = BstrSize(m_stuChars.Bstr());
	if (!cbtext)
	{
		// "Empty String element? (cbtext = 0)"
		StrAnsi sta(kstidXmlErrorMsg036);
		LogMessage(sta.Chars());
	}
	else
	{
		// Calculate the number of bytes needed for run information, and assign or
		// allocate the output buffer.
		int crun = m_vbri.Size();
		Assert(crun >= m_vrpi.Size());
		int cbBin = isizeof(crun);
		if (crun)
		{
			cbBin += crun * isizeof(FwXml::BasicRunInfo);
			for (int i = 0; i < m_vrpi.Size(); ++i)
			{
				cbBin += 2;
				cbBin += m_vrpi[i].m_vbRawProps.Size();
			}
		}
		Vector<byte> vbBin;
		vbBin.Resize(cbBin);
		byte * prgbBin = vbBin.Begin();
		// Copy the run information to the output buffer.
		cbBin = isizeof(crun);
		memcpy(prgbBin, &crun, cbBin);
		if (crun)
		{
			memcpy(prgbBin + cbBin, m_vbri.Begin(),
				crun * isizeof(FwXml::BasicRunInfo));
			cbBin += crun * isizeof(FwXml::BasicRunInfo);
			for (int i = 0; i < m_vrpi.Size(); ++i)
			{
				prgbBin[cbBin] = m_vrpi[i].m_ctip;
				++cbBin;
				prgbBin[cbBin] = m_vrpi[i].m_ctsp;
				++cbBin;
				memcpy(prgbBin + cbBin, m_vrpi[i].m_vbRawProps.Begin(),
					m_vrpi[i].m_vbRawProps.Size());
				cbBin += m_vrpi[i].m_vbRawProps.Size();
			}
		}
		m_vstda[ifld].m_vstu.Push(m_stuChars);
		m_vstda[ifld].m_vvbFmt.Push(vbBin);
		vbBin.Clear();
		m_vstda[ifld].m_vhobj.Push(hobjOwner);
		if (m_pfwxd->FieldInfo(ifld).cpt == kcptBigString)
		{
			if (m_vstda[ifld].m_vhobj.Size() >= kcstrBigMax)
				StoreStringData(ifld);
		}
		else
		{
			if (m_vstda[ifld].m_vhobj.Size() >= kcstrMax)
				StoreStringData(ifld);
		}
	}
	m_vbri.Clear();
	m_vtxip.Clear();
	m_vtxsp.Clear();
	m_fInString = false;
}


/*----------------------------------------------------------------------------------------------
	Store the data accumulated for an <AStr> element.

	@param pszName XML element name read from the input file.
	@param eti reference to the element type information popped off the open element stack
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
	@param hobjOwner Database id of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreMultiStringData(const XML_Char * pszName, int icls, int ifld,
	int hobjOwner)
{
	long cbtext = BstrSize(m_stuChars.Bstr());
	if (m_ws == 0)
	{		// Invalid ws value for <AStr ws="x">, so ignore the data altogether.  At
	}		// least one error message has already been logged for the invalid ws value.
	else if (!cbtext)
	{
		// "Empty MultiString element? (cbtext = 0)"
		StrAnsi sta(kstidXmlErrorMsg035);
		LogMessage(sta.Chars());
	}
	else
	{
		StoreMultiStringData(hobjOwner, ifld, m_ws, m_stuChars, m_vbri, m_vrpi);
	}
	m_vbri.Clear();
	m_vtxip.Clear();
	m_vtxsp.Clear();
	m_fInString = false;
}

/*----------------------------------------------------------------------------------------------
	Store the data for an <AStr> element.

	@param hobjOwner Database id of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
	@param ws
	@param stuChars
	@param vbri
	@param vrpi
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreMultiStringData(int hobjOwner, int ifld, int ws, StrUni & stuChars,
	Vector<FwXml::BasicRunInfo> & vbri, Vector<FwXml::RunPropInfo> & vrpi)
{
	// Calculate the number of bytes needed for run information.
	int crun = vbri.Size();
	Assert(crun >= vrpi.Size());
	int cbBin = isizeof(crun);
	if (crun)
	{
		cbBin += crun * isizeof(FwXml::BasicRunInfo);
		for (int i = 0; i < vrpi.Size(); ++i)
			cbBin += 2 + vrpi[i].m_vbRawProps.Size();
	}
	Vector<byte> vbBin;
	vbBin.Resize(cbBin);
	byte * prgbBin = vbBin.Begin();
	// Copy the run information to the output buffer.
	cbBin = isizeof(crun);
	memcpy(prgbBin, &crun, cbBin);
	if (crun)
	{
		memcpy(prgbBin + cbBin, vbri.Begin(),
			crun * isizeof(FwXml::BasicRunInfo));
		cbBin += crun * isizeof(FwXml::BasicRunInfo);
		for (int i = 0; i < vrpi.Size(); ++i)
		{
			prgbBin[cbBin] = vrpi[i].m_ctip;
			++cbBin;
			prgbBin[cbBin] = vrpi[i].m_ctsp;
			++cbBin;
			memcpy(prgbBin + cbBin, vrpi[i].m_vbRawProps.Begin(),
				vrpi[i].m_vbRawProps.Size());
			cbBin += vrpi[i].m_vbRawProps.Size();
		}
	}
	if (m_pfwxd->FieldInfo(ifld).cpt == kcptMultiBigString)
	{
		m_msdBig.m_vfid.Push(m_pfwxd->FieldInfo(ifld).fid);
		m_msdBig.m_vhobj.Push(hobjOwner);
		m_msdBig.m_vws.Push(ws);
		m_msdBig.m_vstu.Push(stuChars);
		m_msdBig.m_vvbFmt.Push(vbBin);
		vbBin.Clear();
		if (m_msdBig.m_vfid.Size() >= kcstrBigMax)
			StoreMultiBigString();
	}
	else
	{
		m_msd.m_vfid.Push(m_pfwxd->FieldInfo(ifld).fid);
		m_msd.m_vhobj.Push(hobjOwner);
		m_msd.m_vws.Push(ws);
		m_msd.m_vstu.Push(stuChars);
		m_msd.m_vvbFmt.Push(vbBin);
		vbBin.Clear();
		if (m_msd.m_vfid.Size() >= kcstrMax)
			StoreMultiString();
	}
}

/*----------------------------------------------------------------------------------------------
	Store the data accumulated for a <Uni> element.

	@param pszName XML element name read from the input file.
	@param eti reference to the element type information popped off the open element stack
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
	@param hobjOwner Database id of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreUnicodeData(const XML_Char * pszName, int icls, int ifld,
	int hobjOwner)
{
	long cbtext = m_stuChars.Length() * isizeof(wchar);
	if (!cbtext)
	{
		// KenZ thinks this is too much information.
		// "Empty <Uni> element? (cbtext = 0)"
		//StrAnsi sta(kstidXmlErrorMsg034);
		//LogMessage(sta.Chars());
	}
	else
	{
		m_vstda[ifld].m_vstu.Push(m_stuChars);
		m_vstda[ifld].m_vhobj.Push(hobjOwner);
		if (m_pfwxd->FieldInfo(ifld).cpt == kcptBigUnicode)
		{
			if (m_vstda[ifld].m_vhobj.Size() >= kcstrBigMax)
				StoreUnicodeData(ifld);
		}
		else
		{
			if (m_vstda[ifld].m_vhobj.Size() >= kcstrMax)
				StoreUnicodeData(ifld);
		}
	}
	m_fInUnicode = false;
	m_stuChars.Clear();
}


/*----------------------------------------------------------------------------------------------
	Load the set of ids for nvarchar(...) fields which are indexed, and hence limited to 450
	characters in length.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::LoadIndexedStringFields()
{
	if (m_setIdxTxtFlids.Size() != 0)
		return;
	//TODO (steve miller): MoForm_Form.Txt and WfiWordform_Form.Txt are too long to indexed in firebird
	// so this query returns nothing
	StrAnsi sta;
	if(CURRENTDB == MSSQL) {
		sta.Format("SELECT ob.name, cl.name%n"
			"FROM dbo.sysindexkeys ik%n"
			"JOIN dbo.sysobjects ob ON ob.id = ik.id AND ob.xtype = 'U' AND ob.name NOT LIKE '%%$'%n"
			"JOIN dbo.syscolumns cl ON cl.colid= ik.colid AND cl.id=ik.id%n"
			"JOIN dbo.systypes ty ON ty.xtype = cl.xtype AND ty.name='nvarchar';");
	}
	else if(CURRENTDB == FB){
		sta.Format("SELECT i.rdb$relation_name, s.rdb$field_name "
			"FROM rdb$indices i, rdb$index_segments s, rdb$relation_fields rf, rdb$fields f "
			"WHERE s.rdb$index_name = i.rdb$index_name%n"
			"AND rf.rdb$relation_Name = i.rdb$relation_Name%n"
			"AND rf.rdb$field_Name = s.rdb$field_name%n"
			"AND rf.rdb$field_source = f.rdb$field_name%n"
			"AND i.rdb$system_flag = 0%n"
			"AND f.rdb$character_set_id = 4 AND i.rdb$relation_name NOT LIKE '%%$%%';");
	}

	SqlStatement sstmt;
	RETCODE rc;
	wchar rgchTable[130];	// sysname == nvarchar(128)
	SDWORD cbTable;
	wchar rgchColumn[130];
	SDWORD cbColumn;
	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(sta.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, sta.Chars());
	rc = SQLFetch(sstmt.Hstmt());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	while (rc == SQL_SUCCESS)
	{
		rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_WCHAR, rgchTable, isizeof(rgchTable),
			&cbTable);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (cbTable !=  SQL_NULL_DATA && cbTable != 0)
		{
			rc = SQLGetData(sstmt.Hstmt(), 2, SQL_C_WCHAR, rgchColumn, isizeof(rgchColumn),
				&cbColumn);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			if (cbColumn !=  SQL_NULL_DATA && cbColumn != 0)
			{
				StrUni stuClass;
				StrUni stuField;
				if (_wcsicmp(rgchColumn, L"Txt") == 0)
				{
					// table name = CLASS_FIELD.
					StrUni stu(rgchTable);
					int ich = stu.FindCh('_');
					stuClass = stu.Left(ich);
					stuField = stu.Chars() + ich + 1;
				}
				else
				{
					stuClass = rgchTable;
					stuField = rgchColumn;
				}
				int ifld = m_pfwxd->FieldIndexFromNames(stuClass, stuField);
				if (ifld >= 0)
				{
					int fid = m_pfwxd->FieldInfo(ifld).fid;
					m_setIdxTxtFlids.Insert(fid);
				}
			}
		}
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	}
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Store the data accumulated for an <AUni> element.

	@param pszName XML element name read from the input file.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
	@param hobjOwner Database id of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreMultiUnicodeData(const XML_Char * pszName, int icls, int ifld,
	int hobjOwner)
{
	long cbtext = m_stuChars.Length() * isizeof(wchar);
	if (!cbtext)
	{
		// KenZ thinks this is too much information.
		// "Empty <AUni> element? (cbtext = 0)"
		//StrAnsi sta(kstidXmlErrorMsg032);
		//LogMessage(sta.Chars());
	}
	else
	{
		if (m_pfwxd->FieldInfo(ifld).cpt == kcptMultiBigUnicode)
		{
			m_mtdBig.m_vfid.Push(m_pfwxd->FieldInfo(ifld).fid);
			m_mtdBig.m_vhobj.Push(hobjOwner);
			m_mtdBig.m_vws.Push(m_ws);
			m_mtdBig.m_vstu.Push(m_stuChars);
			if (m_mtdBig.m_vfid.Size() >= kcstrBigMax)
				StoreMultiBigUnicode();
		}
		else
		{
			// Some short multilingual unicode strings are indexed in the database.
			// SQL Server has a maximum length of 900 bytes for indexed data.  So...
			int fid = m_pfwxd->FieldInfo(ifld).fid;
			StoreMultiUnicodeData(hobjOwner, fid, m_ws, m_stuChars);
		}
	}
	m_fInUnicode = false;
	m_stuChars.Clear();
}

void FwXmlImportData::StoreMultiUnicodeData(int hobjOwner, int fid, int ws, StrUni & stuData)
{
	if (m_setIdxTxtFlids.IsMember(fid) && stuData.Length() >= kcchMaxIndexSize)
	{
		StrAnsi staFmt(kstidXmlErrorMsg323);
		StrAnsi sta;
		int cchUtf8 = CountXmlUtf8FromUtf16(stuData.Chars(), stuData.Length(),
			false);
		Vector<char> vch;
		vch.Resize(cchUtf8 + 1);
		ConvertUtf16ToUtf8(vch.Begin(), vch.Size(),
			stuData.Chars(), stuData.Length());
		sta.Format(staFmt.Chars(),
			kcchMaxIndexSize - 1, stuData.Length(), vch.Begin());
		LogMessage(sta.Chars());
		stuData = stuData.Left(kcchMaxIndexSize - 1);
	}
	m_mtd.m_vfid.Push(fid);
	m_mtd.m_vhobj.Push(hobjOwner);
	m_mtd.m_vws.Push(ws);
	m_mtd.m_vstu.Push(stuData);
	if (m_mtd.m_vfid.Size() >= kcstrMax)
		StoreMultiUnicode();
}

/*----------------------------------------------------------------------------------------------
	Store the data accumulated for a <Prop> element.

	@param pszName XML element name read from the input file.
	@param eti reference to the element type information popped off the open element stack
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ifld Index into m_vfdfi for the field of the owning object.
	@param hobjOwner Database id of the owning object.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreRulePropData(const XML_Char * pszName, int icls, int ifld,
	int hobjOwner)
{
	int cbBin = m_vbProp.Size();
	if (!cbBin)
	{
		// "Empty <%s> element?"
		StrAnsi staFmt(kstidXmlErrorMsg031);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), pszName);
		LogMessage(sta.Chars());
	}
	else
	{
		byte * prgbBin = m_vbProp.Begin();
		long cbBin2 = cbBin;
		SqlStatement sstmt;
		sstmt.Init(m_sdb);
		StrAnsiBuf stabCmd;
		if(CURRENTDB == MSSQL) {
			stabCmd.Format("UPDATE \"%S\" SET \"%S\"=? WHERE Id =%d;",
				m_pfwxd->ClassName(icls).Chars(),
				m_pfwxd->FieldName(ifld).Chars(), hobjOwner);
		}
		else if(CURRENTDB == FB) {
			stabCmd.Format("UPDATE %S SET \"%S\" = ? WHERE Id =%d;",
				m_pfwxd->ClassName(icls).Chars(),
				UpperName(m_pfwxd->FieldName(ifld)), hobjOwner);
		}
		RETCODE rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_BINARY,
			SQL_VARBINARY, cbBin, 0, prgbBin, cbBin, &cbBin2);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())),
			SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
		sstmt.Clear();
		stabCmd.Clear();
		m_vbProp.Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	Add this integer valued property to the collection of properties, or set its value if it is
	already in the collection.

	@param vtxip Reference to a vector of integer valued properties.
	@param txip Reference to a data structure used for storing the integer valued text property.
----------------------------------------------------------------------------------------------*/
static void SetIntProperty(Vector<TextProps::TextIntProp> & vtxip,
	TextProps::TextIntProp & txip)
{
	bool fFound = false;
	for (int i = 0; i < vtxip.Size(); ++i)
	{
		if (vtxip[i].m_scp == txip.m_scp)
		{
			vtxip[i].m_nVal = txip.m_nVal;
			vtxip[i].m_nVar = txip.m_nVar;
			fFound = true;
			break;
		}
	}
	if (!fFound && txip.m_nVal != -1)
		vtxip.Push(txip);
}

/*----------------------------------------------------------------------------------------------
	Set the string valued property for a "BulNumFontInfo" property.

	@param txsp Reference to a string valued property data structure.
	@param pszVal
	@param cbVal
----------------------------------------------------------------------------------------------*/
static void SetBulNumFontInfo(TextProps::TextStrProp & txsp, const char * pszVal, int cbVal)
{
	if (cbVal)
	{
		const wchar * pcchw = reinterpret_cast<const wchar *>(pszVal);
		txsp.m_stuVal.Assign(pcchw, cbVal / isizeof(wchar));
	}
	else
	{
		/* contains a string of hexadecimal characters */
		int cch = strlen(pszVal);
		int ich;
		Vector<byte> vbBin;
		byte rgbBin[1024];
		byte ch;
		byte bT;
		int cbBin = cch / 2;
		byte * prgbBin;
		if (cbBin <= isizeof(rgbBin))
		{
			prgbBin = rgbBin;
		}
		else
		{
			vbBin.Resize(cbBin);
			prgbBin = vbBin.Begin();
		}
		int ib;
		for (ib = 0, ich = 0; ich < cch; )
		{
			// Read the first Hex digit of the byte.  It may be preceded by one or
			// more whitespace characters.
			do
			{
				ch = pszVal[ich];
				++ich;
				if (!isascii(ch))
					ThrowHr(WarnHr(E_UNEXPECTED));
			} while (isspace(ch) && ich < cch);
			if (ich == cch)
				break;
			if (isdigit(ch))
				bT = static_cast<byte>((ch & 0xF) << 4);
			else
				bT = static_cast<byte>(((ch & 0xF) + 9) << 4);

			// Read the second Hex digit of the byte.
			ch = pszVal[ich];
			++ich;
			if (isdigit(ch))
				bT |= ch & 0xF;
			else
				bT |= (ch & 0xF) + 9;
			prgbBin[ib] = bT;
			++ib;
		}
		wchar * pcchw = reinterpret_cast<wchar *>(prgbBin);
		txsp.m_stuVal.Assign(pcchw, (cbBin / isizeof(wchar)));
	}
}


/*----------------------------------------------------------------------------------------------
	Add this string valued property to the collection of properties, or set its value if it is
	already in the collection.

	@param vtxsp Reference to a vector of string valued properties.
	@param stp
	@param pszVal
	@param cbVal
----------------------------------------------------------------------------------------------*/
static void SetStringProp(Vector<TextProps::TextStrProp> & vtxsp, int stp, const char * pszVal,
	int cbVal = 0)
{
	if (pszVal == NULL)
		return;

	TextProps::TextStrProp txsp;
	txsp.m_tpt = stp;

	if (stp == kstpBulNumFontInfo)
	{
		SetBulNumFontInfo(txsp, pszVal, cbVal);
	}
	else if (stp == kstpWsStyle)
	{
		const wchar * pcchw = reinterpret_cast<const wchar *>(pszVal);
		txsp.m_stuVal.Assign(pcchw, cbVal / isizeof(wchar));
	}
	else
	{
		// Convert the string from UTF-8 to UTF-16 and store the Unicode characters.
		StrUtil::StoreUtf16FromUtf8(pszVal, strlen(pszVal), txsp.m_stuVal);
	}
	bool fFound = false;
	for (int i = 0; i < vtxsp.Size(); ++i)
	{
		if (vtxsp[i].m_tpt == txsp.m_tpt)
		{
			vtxsp[i].m_stuVal = txsp.m_stuVal;
			fFound = true;
			break;
		}
	}
	if (!fFound && txsp.m_stuVal.Length())
		vtxsp.Push(txsp);
}


/*----------------------------------------------------------------------------------------------
	Process and temporarily store the attributes of a <Prop> element in the provided vectors.

	If any of these attributes is missing, that implies that it is not set for this style rule.
		align          - contains an unsigned decimal integer.
		backcolor      - contains one of these values:
						 "white" | "black" | "red" | "green" | "blue" | "yellow" |
						 "magenta" | "cyan" | "transparent" | <8 digit hexadecimal number>
		bold           - contains one of these values: "on" | "off" | "invert".
		borderBottom   - contains an unsigned decimal integer.
		borderColor    - contains one of these values:
						 "white" | "black" | "red" | "green" | "blue" | "yellow" |
						 "magenta" | "cyan" | "transparent" | <8 digit hexadecimal number>
		borderLeading  - contains an unsigned decimal integer.
		borderTop      - contains an unsigned decimal integer.
		borderTrailing - contains an unsigned decimal integer.
		bulNumFontInfo - contains a string of encoded information
		bulNumScheme   - contains an unsigned decimal integer.
		bulNumStartAt  - contains an unsigned decimal integer.
		bulNumTxtAft   - contains an arbitrary string which is stored verbatim.
		bulNumTxtBef   - contains an arbitrary string which is stored verbatim.
		charStyle      - contains an arbitrary string which is stored verbatim.
		ws             - contains a valid writing system string as defined elsewhere, or an
						 empty string to indicate that it is not defined.
		wsBase         - contains a valid writing system string as defined elsewhere, or an
						 empty string to indicate that it is not defined.
		firstIndent    - contains a signed decimal integer.
		fontFamily     - contains an arbitrary string which is stored verbatim.
		fontsize       - contains an unsigned decimal integer.
		fontsizeUnit   - is used only if fontsize is set.  It defaults to "mpt".
		forecolor      - contains one of these values:
						 "white" | "black" | "red" | "green" | "blue" | "yellow" |
						 "magenta" | "cyan" | "transparent" | <8 digit hexadecimal number>
		italic         - contains one of these values: "on" | "off" | "invert".
		leadingIndent  - contains an unsigned decimal integer.
		lineHeight     - contains an unsigned decimal integer.
		lineHeightUnit - is used only if lineHeight is set.  It defaults to "mpt".
		lineHeightType - is used only if lineHeightUnit = "mpt".  It defaults to "atLeast".
		marginTop      - contains an unsigned decimal integer.
		namedStyle     - contains an arbitrary string which is stored verbatim.
		offset         - contains an unsigned decimal integer.
		offsetUnit     - is used only if offset is set.  It defaults to "mpt".
		padBottom      - contains an unsigned decimal integer.
		padLeading     - contains an unsigned decimal integer.
		padTop         - contains an unsigned decimal integer.
		padTrailing    - contains an unsigned decimal integer.
		paracolor      - contains one of these values:
						 "white" | "black" | "red" | "green" | "blue" | "yellow" |
						 "magenta" | "cyan" | "transparent" | <8 digit hexadecimal number>
		rightToLeft    - contains an unsigned decimal integer.
		spaceAfter     - contains an unsigned decimal integer.
		spaceBefore    - contains an unsigned decimal integer.
		spellcheck	   - contains one of these values: normal, doNotCheck, forceCheck
		superscript    - contains one of these values: "off", "super", "sub".
		tabDef         - contains an unsigned decimal integer.
		trailingIndent - contains an unsigned decimal integer.
		undercolor     - contains one of these values:
						 "white" | "black" | "red" | "green" | "blue" | "yellow" |
						 "magenta" | "cyan" | "transparent" | <8 digit hexadecimal number>
		underline      - contains one of these values:
						 "none" | "single" | "double" | "dotted" | "dashed" | "strikethrough"
		keepWithNext   - contains one of these values: "true" | "false".
		keepTogether   - contains one of these values: "true" | "false".
		widowOrphan   - contains one of these values: "true" | "false".

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param vtxip Reference to a vector of integer valued properties.
	@param vtxsp Reference to a vector of string valued properties.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessPropAttributes(const XML_Char * pszName,
	const XML_Char ** prgpszAtts, Vector<TextProps::TextIntProp> & vtxip,
	Vector<TextProps::TextStrProp> & vtxsp)
{
	SetAlignProperty(pszName, vtxip, FwXml::GetAttributeValue(prgpszAtts, "align"));
	SetColorProperty(pszName, vtxip, kscpBackColor,
		FwXml::GetAttributeValue(prgpszAtts, "backcolor"));
	SetTextToggleProperty(pszName, vtxip, kscpBold, kstidXmlErrorMsg139,
		FwXml::GetAttributeValue(prgpszAtts, "bold"));
	SetMilliPointProperty(pszName, vtxip, (ktptBorderBottom << 2) | 2,
		FwXml::GetAttributeValue(prgpszAtts, "borderBottom"));
	SetColorProperty(pszName, vtxip, (ktptBorderColor << 2) | 2,
		FwXml::GetAttributeValue(prgpszAtts, "borderColor"));
	SetMilliPointProperty(pszName, vtxip, (ktptBorderLeading << 2) | 2,
		FwXml::GetAttributeValue(prgpszAtts, "borderLeading"));
	SetMilliPointProperty(pszName, vtxip, (ktptBorderTop << 2) | 2,
		FwXml::GetAttributeValue(prgpszAtts, "borderTop"));
	SetMilliPointProperty(pszName, vtxip, (ktptBorderTrailing << 2) | 2,
		FwXml::GetAttributeValue(prgpszAtts, "borderTrailing"));
	SetStringProp(vtxsp, kstpBulNumFontInfo,	/* would be a string of encoded information */
		FwXml::GetAttributeValue(prgpszAtts, "bulNumFontInfo"));
	SetEnumeratedProperty(pszName, vtxip, (ktptBulNumScheme << 2) | 0,
		FwXml::GetAttributeValue(prgpszAtts, "bulNumScheme"));
	SetNumericProperty(pszName, vtxip, (ktptBulNumStartAt << 2) | 2,
		FwXml::GetAttributeValue(prgpszAtts, "bulNumStartAt"));
	SetStringProp(vtxsp, kstpBulNumTxtAft,		/* would be a string which is stored verbatim */
		FwXml::GetAttributeValue(prgpszAtts, "bulNumTxtAft"));
	SetStringProp(vtxsp, kstpBulNumTxtBef,		/* would be a string which is stored verbatim */
		FwXml::GetAttributeValue(prgpszAtts, "bulNumTxtBef"));
	SetStringProp(vtxsp, kstpCharStyle,			/* would be a string which is stored verbatim */
		FwXml::GetAttributeValue(prgpszAtts, "charStyle"));
	SetWsProperty(pszName, vtxip, kscpWs, FwXml::GetAttributeValue(prgpszAtts, "ws"));
	SetWsProperty(pszName, vtxip, kscpBaseWs,
		FwXml::GetAttributeValue(prgpszAtts, "wsBase"));
	SetMilliPointProperty(pszName, vtxip, kscpFirstIndent,
		FwXml::GetAttributeValue(prgpszAtts, "firstIndent"));
	SetStringProp(vtxsp, kstpFontFamily,		/* would be a string which is stored verbatim */
		FwXml::GetAttributeValue(prgpszAtts, "fontFamily"));
	SetSizeAndUnitProperties(pszName, vtxip, FwXml::GetAttributeValue(prgpszAtts, "fontsize"),
		FwXml::GetAttributeValue(prgpszAtts, "fontsizeUnit"), NULL, kscpFontSize,
		kstidXmlErrorMsg141, kstidXmlErrorMsg142, kstidXmlErrorMsg140);
	SetColorProperty(pszName, vtxip, kscpForeColor,
		FwXml::GetAttributeValue(prgpszAtts, "forecolor"));
	SetTextToggleProperty(pszName, vtxip, kscpItalic, kstidXmlErrorMsg146,
		FwXml::GetAttributeValue(prgpszAtts, "italic"));
	SetMilliPointProperty(pszName, vtxip, kscpLeadingIndent,
		FwXml::GetAttributeValue(prgpszAtts, "leadingIndent"));
	SetSizeAndUnitProperties(pszName, vtxip, FwXml::GetAttributeValue(prgpszAtts, "lineHeight"),
		FwXml::GetAttributeValue(prgpszAtts, "lineHeightUnit"),
		FwXml::GetAttributeValue(prgpszAtts, "lineHeightType"), kscpLineHeight,
		kstidXmlErrorMsg147, kstidXmlErrorMsg148, kstidXmlErrorMsg149);
	SetMilliPointProperty(pszName, vtxip, (ktptMarginTop << 2) | 2,
		FwXml::GetAttributeValue(prgpszAtts, "marginTop"));
	SetStringProp(vtxsp, kstpNamedStyle,		/* would be a string which is stored verbatim */
		FwXml::GetAttributeValue(prgpszAtts, "namedStyle"));
	SetSizeAndUnitProperties(pszName, vtxip, FwXml::GetAttributeValue(prgpszAtts, "offset"),
		FwXml::GetAttributeValue(prgpszAtts, "offsetUnit"), NULL, kscpOffset,
		kstidXmlErrorMsg150, kstidXmlErrorMsg151, kstidXmlErrorMsg152);
	SetNumericProperty(pszName, vtxip, (ktptPadBottom << 2) | 2,
		FwXml::GetAttributeValue(prgpszAtts, "padBottom"));
	SetNumericProperty(pszName, vtxip, (ktptPadLeading << 2) | 2,
		FwXml::GetAttributeValue(prgpszAtts, "padLeading"));
	SetNumericProperty(pszName, vtxip, (ktptPadTop << 2) | 2,
		FwXml::GetAttributeValue(prgpszAtts, "padTop"));
	SetNumericProperty(pszName, vtxip, (ktptPadTrailing << 2) | 2,
		FwXml::GetAttributeValue(prgpszAtts, "padTrailing"));
	SetColorProperty(pszName, vtxip, kscpParaColor,
		FwXml::GetAttributeValue(prgpszAtts, "paracolor"));
	SetEnumeratedProperty(pszName, vtxip, (ktptRightToLeft << 2),
		FwXml::GetAttributeValue(prgpszAtts, "rightToLeft"));
	SetMilliPointProperty(pszName, vtxip, kscpSpaceAfter,
		FwXml::GetAttributeValue(prgpszAtts, "spaceAfter"));
	SetMilliPointProperty(pszName, vtxip, kscpSpaceBefore,
		FwXml::GetAttributeValue(prgpszAtts, "spaceBefore"));
	SetSuperscriptProperty(pszName, vtxip, FwXml::GetAttributeValue(prgpszAtts, "superscript"));
	SetMilliPointProperty(pszName, vtxip, kscpTabDef,
		FwXml::GetAttributeValue(prgpszAtts, "tabDef"));
	SetMilliPointProperty(pszName, vtxip, kscpTrailingIndent,
		FwXml::GetAttributeValue(prgpszAtts, "trailingIndent"));
	SetColorProperty(pszName, vtxip, kscpUnderColor,
		FwXml::GetAttributeValue(prgpszAtts, "undercolor"));
	SetUnderlineProperty(pszName, vtxip, FwXml::GetAttributeValue(prgpszAtts, "underline"));
	SetSpellCheckProperty(pszName, vtxip, FwXml::GetAttributeValue(prgpszAtts, "spellcheck"));
	SetEnumeratedProperty(pszName, vtxip, kscpKeepWithNext,
		FwXml::GetAttributeValue(prgpszAtts, "keepWithNext"));
	SetEnumeratedProperty(pszName, vtxip, kscpKeepTogether,
		FwXml::GetAttributeValue(prgpszAtts, "keepTogether"));
	SetEnumeratedProperty(pszName, vtxip, kscpWidowOrphanControl,
		FwXml::GetAttributeValue(prgpszAtts, "widowOrphan"));
}


/*----------------------------------------------------------------------------------------------
	If set, add the Align property to the set of properties.

	@param pszName XML element name read from the input file.
	@param vtxip Reference to a vector of integer valued properties.
	@param pszAlign value of the "align" attribute (may be NULL)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetAlignProperty(const XML_Char * pszName,
	Vector<TextProps::TextIntProp> & vtxip, const char * pszAlign)
{
	if (pszAlign)
	{
		TextProps::TextIntProp txip;
		txip.m_nVal = FwXml::DecodeTextAlign(pszAlign);
		if (txip.m_nVal < ktalMin || txip.m_nVal >= ktalLim)
		{
// "Invalid value in <%s align=\"%s\">: need leading, left, center, right, trailing, or justify"
			StrAnsi staFmt(kstidXmlErrorMsg143);
			StrAnsi sta;
			sta.Format(staFmt.Chars(), pszName, pszAlign);
			LogMessage(sta.Chars());
		}
		else
		{
			txip.m_scp = kscpAlign;		// kscpAlign = SCP1(ktptAlign)
			txip.m_nVar = ktpvEnum;
			SetIntProperty(vtxip, txip);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	If set, add the color property to the set of properties.

	@param pszName XML element name read from the input file.
	@param vtxip Reference to a vector of integer valued properties.
	@param scp property code of the specific color property
	@param pszColor value of the color property attribute (may be NULL)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetColorProperty(const XML_Char * pszName,
	Vector<TextProps::TextIntProp> & vtxip, int scp, const char * pszColor)
{
	if (pszColor)
	{
		TextProps::TextIntProp txip;
		txip.m_scp = scp;
		const char * pszValueLim;
		txip.m_nVal = FwXml::DecodeTextColor(pszColor, &pszValueLim);
		if (*pszValueLim)
		{
			// ERROR
		}
		txip.m_nVar = ktpvDefault;
		SetIntProperty(vtxip, txip);
	}
}


/*----------------------------------------------------------------------------------------------
	If set, add the text toggle (bold/italic) property to the set of properties.

	@param pszName XML element name read from the input file.
	@param vtxip Reference to a vector of integer valued properties.
	@param scp property code of the specific text toggle property
	@param stidError resource id of an error message for an invalid value
	@param pszToggleVal value of the text toggle property attribute (may be NULL)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTextToggleProperty(const XML_Char * pszName,
	Vector<TextProps::TextIntProp> & vtxip, int scp, int stidError, const char * pszToggleVal)
{
	if (pszToggleVal)
	{
		TextProps::TextIntProp txip;
		/* contains one of these values: "on" | "off" | "invert". */
		const char * pszValueLim;
		txip.m_nVal = FwXml::DecodeTextToggleVal(pszToggleVal, &pszValueLim);
		Assert(kttvOff < kttvForceOn && kttvInvert > kttvForceOn);
		if (*pszValueLim || txip.m_nVal < kttvOff || txip.m_nVal > kttvInvert)
		{
			// "Invalid value in <%s bold=\"%s\">: need on, off or invert"
			// "Invalid value in <%s italic=\"%s\">: need on, off or invert"
			StrAnsi staFmt(stidError);
			StrAnsi sta;
			sta.Format(staFmt.Chars(), pszName, pszToggleVal);
			LogMessage(sta.Chars());
		}
		else
		{
			txip.m_scp = scp;		// kscpBold = SCP1(ktptBold)
			txip.m_nVar = ktpvEnum;
			SetIntProperty(vtxip, txip);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	If set, add the size (measured in millipoints) property to the set of properties.

	@param pszName XML element name read from the input file.
	@param vtxip Reference to a vector of integer valued properties.
	@param scp property code of the specific size property
	@param pszVal value of the size property attribute (may be NULL)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetMilliPointProperty(const XML_Char * pszName,
	Vector<TextProps::TextIntProp> & vtxip, int scp, const char * pszVal)
{
	if (pszVal)
	{
		TextProps::TextIntProp txip;
		char * psz;
		txip.m_scp = scp;
		txip.m_nVal = strtoul(pszVal, &psz, 10);
		txip.m_nVar = ktpvMilliPoint;
		SetIntProperty(vtxip, txip);
	}
}


/*----------------------------------------------------------------------------------------------
	If set, add the enumeration (or Boolean) property to the set of properties.

	@param pszName XML element name read from the input file.
	@param vtxip Reference to a vector of integer valued properties.
	@param scp property code of the specific enumeration property
	@param pszEnumVal value of the enumeration property attribute (may be NULL)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetEnumeratedProperty(const XML_Char * pszName,
	Vector<TextProps::TextIntProp> & vtxip, int scp, const char * pszEnumVal)
{
	if (pszEnumVal)
	{
		TextProps::TextIntProp txip;
		char * psz;
		txip.m_scp = scp;
		txip.m_nVal = strtoul(pszEnumVal, &psz, 10);
		Assert((unsigned)txip.m_nVal < 256);
		txip.m_nVar = ktpvEnum;
		SetIntProperty(vtxip, txip);
	}
}


/*----------------------------------------------------------------------------------------------
	If set, add the numeric property to the set of properties.

	@param pszName XML element name read from the input file.
	@param vtxip Reference to a vector of integer valued properties.
	@param scp property code of the specific enumeration property
	@param pszVal value of the numeric property attribute (may be NULL)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetNumericProperty(const XML_Char * pszName,
	Vector<TextProps::TextIntProp> & vtxip, int scp, const char * pszVal)
{
	if (pszVal)
	{
		TextProps::TextIntProp txip;
		char * psz;
		txip.m_scp = scp;
		txip.m_nVal = strtoul(pszVal, &psz, 10);
		txip.m_nVar = ktpvDefault;
		SetIntProperty(vtxip, txip);
	}
}


/*----------------------------------------------------------------------------------------------
	If set, add the writing system property to the set of properties.

	@param pszName XML element name read from the input file.
	@param vtxip Reference to a vector of integer valued properties.
	@param scp property code of the specific writing system property
	@param pszWs value of the writing system property attribute (may be NULL)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetWsProperty(const XML_Char * pszName,
	Vector<TextProps::TextIntProp> & vtxip, int scp, const char * pszWs)
{
	if (pszWs)
	{
		TextProps::TextIntProp txip;
		// "Cannot convert \"%s\" into a Language Writing system code."
		txip.m_nVal = GetWsFromIcuLocale(pszWs, kstidXmlErrorMsg011);
		if (txip.m_nVal)
		{
			txip.m_scp = scp;		// kscpWs = SCP4(ktptWs)
			txip.m_nVar = 0;
			SetIntProperty(vtxip, txip);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	If set, add the size property to the set of properties.

	@param pszName XML element name read from the input file.
	@param vtxip Reference to a vector of integer valued properties.
	@param pszSize value of the size property attribute (may be NULL)
	@param pszUnit value of the corresponding unit attribute (may be NULL)
	@param pszType value of the corresponding type attribute (may be NULL)
	@param scpSize property code of the specific size property
	@param stidBadSize resource id of error message for invalid size attribute value
	@param stidBadUnit resource id of error message for invalid unit attribute value
	@param stidNoSize resource id of error message for unit without size attribute
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetSizeAndUnitProperties(const XML_Char * pszName,
	Vector<TextProps::TextIntProp> & vtxip, const char * pszSize, const char * pszUnit,
	const char * pszType, int scpSize, int stidBadSize, int stidBadUnit, int stidNoSize)
{
	StrAnsi sta;
	if (pszSize)
	{
		TextProps::TextIntProp txip;
		bool fError = false;
		char * psz;
		int nSize = strtol(pszSize, &psz, 10);
		if (*psz)
		{
			if (!pszUnit)
			{
				if (!strcmp(psz, "mpt"))
					pszUnit = psz;
				else
					fError = true;
			}
			else
			{
				fError = true;
			}
		}
		if (psz == pszSize || abs(nSize) > 0x7FFFFF)
		{
			fError = true;
		}
		if (fError)
		{
			// "Invalid value in <%s fontsize=\"%s\">."
			StrAnsi staFmt(stidBadSize);
			sta.Format(staFmt.Chars(), pszName, pszSize);
			LogMessage(sta.Chars());
		}
		int tpv = ktpvDefault;
		if (pszUnit)
		{
			if (!strcmp(pszUnit, "mpt"))
			{
				tpv = ktpvMilliPoint;
			}
			else if (!strcmp(pszUnit, "rel"))
			{
				tpv = ktpvRelative;
			}
			else
			{
				// REVIEW SteveMc: should we default to an unsigned int like this?
				tpv = strtoul(pszUnit, &psz, 10);
				if (*psz || tpv > 0xFF)
				{
					// "Invalid value in <%s fontsizeUnit=\"%s\">."
					StrAnsi staFmt(stidBadUnit);
					sta.Format(staFmt.Chars(), pszName, pszSize);
					LogMessage(sta.Chars());
					fError = true;
				}
			}
		}
		if (pszType)
		{
			if (tpv == ktpvMilliPoint && !strcmp(pszType, "exact"))
				nSize = -nSize;		// negative means "exact" internally.  See FWC-20.
		}
		if (!fError)
		{
			// Variation (4 bits): FwTextPropVar enum { ktpvMilliPoint }
			txip.m_scp = scpSize;			// kscpFontSize = SCP4(ktptFontSize)
			txip.m_nVal = nSize;
			txip.m_nVar = tpv;
			SetIntProperty(vtxip, txip);
		}
	}
	else if (pszUnit)
	{
		/* is used only if fontsize is set.  It defaults to "mpt". */
		// "Ignoring <Prop fontsizeUnit=\"%s\"> in the absence of a fontsize attribute."
		StrAnsi staFmt(stidNoSize);
		sta.Format(staFmt.Chars(), pszName, pszUnit);
		LogMessage(sta.Chars());
	}
}


/*----------------------------------------------------------------------------------------------
	If set, add the Superscript property to the set of properties.

	@param pszName XML element name read from the input file.
	@param vtxip Reference to a vector of integer valued properties.
	@param pszSuperscript value of the "superscript" attribute (may be NULL)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetSuperscriptProperty(const XML_Char * pszName,
	Vector<TextProps::TextIntProp> & vtxip, const char * pszSuperscript)
{
	if (pszSuperscript)
	{
		TextProps::TextIntProp txip;
		const char * pszValueLim;
		/* contains one of these values: "off", "super", "sub". */
		txip.m_nVal = FwXml::DecodeSuperscriptVal(pszSuperscript, &pszValueLim);
		Assert(kssvOff < kssvSuper && kssvSub > kssvSuper);
		if (*pszValueLim || txip.m_nVal < kssvOff || txip.m_nVal > kssvSub)
		{
			// "Invalid value in <%s superscript=\"%s\">: need off, super, or sub"
			StrAnsi staFmt(kstidXmlErrorMsg153);
			StrAnsi sta;
			sta.Format(staFmt.Chars(), pszName, pszSuperscript);
			LogMessage(sta.Chars());
		}
		else
		{
			txip.m_scp = kscpSuperscript;		// kscpSuperscript = SCP1(ktptSuperscript)
			txip.m_nVar = ktpvEnum;
			SetIntProperty(vtxip, txip);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	If set, add the Underline property to the set of properties.

	@param pszName XML element name read from the input file.
	@param vtxip Reference to a vector of integer valued properties.
	@param pszUnderline value of the "underline" attribute (may be NULL)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetUnderlineProperty(const XML_Char * pszName,
	Vector<TextProps::TextIntProp> & vtxip, const char * pszUnderline)
{
	if (pszUnderline)
	{
		TextProps::TextIntProp txip;
		const char * pszValueLim;
		/* contains one of these values: "none" | "single" | "double" | "dotted" | "dashed" |
		   "strikethrough" */
		txip.m_nVal = FwXml::DecodeUnderlineType(pszUnderline, &pszValueLim);
		if (*pszValueLim || txip.m_nVal < kuntMin || txip.m_nVal >= kuntLim)
		{
			// "Invalid value in <%s underline=\"%s\">: need none, single, double, dotted,
			//  dashed, or strikethrough"
			StrAnsi staFmt(kstidXmlErrorMsg154);
			StrAnsi sta;
			sta.Format(staFmt.Chars(), pszName, pszUnderline);
			LogMessage(sta.Chars());
		}
		else
		{
			txip.m_scp = kscpUnderline;		// kscpUnderline = SCP1(ktptUnderline)
			txip.m_nVar = ktpvEnum;
			SetIntProperty(vtxip, txip);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	If set, add the SpellCheck property to the set of properties.

	@param pszName XML element name read from the input file.
	@param vtxip Reference to a vector of integer valued properties.
	@param pszUnderline value of the "underline" attribute (may be NULL)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetSpellCheckProperty(const XML_Char * pszName,
	Vector<TextProps::TextIntProp> & vtxip, const char * pszSpellCheck)
{
	if (pszSpellCheck)
	{
		TextProps::TextIntProp txip;
		const char * pszValueLim;
		/* contains one of these values: "normal" | "doNotCheck" | "forceCheck" */
		txip.m_nVal = FwXml::DecodeSpellingMode(pszSpellCheck, &pszValueLim);
		if (*pszValueLim || txip.m_nVal < ksmMin || txip.m_nVal >= ksmLim)
		{
			// "Invalid value in <%s spellcheck=\"%s\">: need normal, doNotCheck, or forceCheck"
			StrAnsi staFmt(kstidXmlErrorMsg175);
			StrAnsi sta;
			sta.Format(staFmt.Chars(), pszName, pszSpellCheck);
			LogMessage(sta.Chars());
		}
		else
		{
			txip.m_scp = kscpSpellCheck;
			txip.m_nVar = ktpvEnum;
			SetIntProperty(vtxip, txip);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	For all objects that own a structured text field, make sure that one has been created, even
	if it is empty.  This simplifies life elsewhere in the programming of Fieldworks.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateEmptyTextFields()
{
	StTextFieldInfo stfi;
	GetEmptyStructuredTextFields(stfi);
	int cobjTotal = stfi.vfid.Size();
	if (cobjTotal == 0)
		return;

	StrAnsi sta;
	StrAnsi staFmt;
	staFmt.Load(kstidXmlInfoMsg008);
	sta.Format(staFmt.Chars(), cobjTotal);
	LogMessage(sta.Chars());

	// Create an StText object for each owner/field that needs one.

	SQLUINTEGER cParamsProcessed = 0;
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	if(CURRENTDB == FB) {
		//TODO: not sure what to do about the RETURNING_VALUES from CreateOwnedObjects$
		m_staCmd.Assign("EXECUTE PROCEDURE CreateOwnedObject$ (?,?,null,?,?,?);");
	}
	if(CURRENTDB == MSSQL) {
		m_staCmd.Assign("EXEC CreateOwnedObject$ ?,?,null,?,?,?;");
	}
	RETCODE rc = SQLPrepareA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(m_staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Set the SQL_ATTR_PARAM_BIND_TYPE statement attribute to use column-wise binding.
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMS_PROCESSED_PTR, &cParamsProcessed, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// Make sure that we don't try to handle more than kceSeg parameters at a time.
	int cobjRemaining = cobjTotal;
	int iePass = 0;
	do
	{
		int cobjPass = kceSeg;
		if (cobjPass > cobjRemaining)
			cobjPass = cobjRemaining;
		BindStTextParameters(sstmt.Hstmt(), stfi, iePass, cobjPass);

		// Execute the command to create the StText objects.
		rc = SQLExecute(sstmt.Hstmt());
		CheckStTextParamsForSuccess(stfi, cParamsProcessed, iePass, cobjPass);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, m_staCmd.Chars());

		// Create an StTxtParagraph object for each new StText object
		rc = CreateEmptyStTxtParagraphs(sstmt.Hstmt(), stfi, iePass, cobjPass, cobjTotal);
		CheckStTxtParagraphParamsForSuccess(stfi, cParamsProcessed, iePass, cobjPass);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, m_staCmd.Chars());

		cobjRemaining -= cobjPass;
		iePass += cobjPass;

	} while (cobjRemaining);
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Fill the vectors in the StTextFieldInfo with the values for creating the desired empty
	StText objects.

	@param stfi Reference to a StTextFieldInfo data structure.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::GetEmptyStructuredTextFields(StTextFieldInfo & stfi)
{
	// Build a temporary table that contains two columns: a class id for every concrete class
	// that owns a structured text field, and the field id of each owned structured text field.
	// Omit CmAnnotation_Text since we don't want StTexts created for the thousands of
	// CmAnnotations in interlinearized texts.
	//TODO (steve miller): do this temp table in Firebird
	if(CURRENTDB == FB){
		m_staCmd.Format("DECLARE VARIABLE oldsize INT; DECLARE VARIABLE newsize INT;\n"
			"CREATE TABLE Tclidflid (\n"
			" clid INT NOT NULL,\n"
			" flid INT NOT NULL);\n"
			"CREATE UNIQUE INDEX Tclidflid_idx ON Tclidflid (clid,flid);\n"
			"INSERT INTO Tclidflid SELECT c.Id, f.Id FROM Class$ c\n"
			"JOIN Field$ f ON f.Class = c.Id AND f.DstCls = %<0>d AND f.Type IN (%<1>d,%<2>d,%<3>d)\n"
			"WHERE (c.Id IN (SELECT Class FROM Field$ WHERE DstCls = %<0>d));\n"
			"SELECT COUNT(*) FROM Tclidflid INTO :oldsize;\n"
			"WHILE (oldsize > 0)\n"
			"BEGIN\n"
			"  INSERT INTO Tclidflid SELECT c.Id,f.flid FROM Tclidflid f\n"
			"  JOIN Class$ c ON c.Base = f.clid;\n"
			"  SELECT COUNT(*) FROM Tclidflid INTO :newsize;\n"
			"  IF (newsize = oldsize) THEN BREAK;\n"
			"  oldsize = newsize;\n"
			"END\n"
			"DELETE FROM Tclidflid WHERE flid = %<4>d;\n"
			"DELETE FROM Tclidflid WHERE clid IN (SELECT Id FROM Class$ WHERE Abstract = 1);",
			kclidStText,
			kcptOwningAtom, kcptOwningCollection, kcptOwningSequence,
			kflidCmAnnotation_Text);
	}
	if(CURRENTDB == MSSQL){
		m_staCmd.Format("DECLARE @oldsize INT, @newsize INT;\n"
			"CREATE TABLE #clidflid (\n"
			" clid INT NOT NULL,\n"
			" flid INT NOT NULL);\n"
			"CREATE UNIQUE INDEX #clidflid_idx ON #clidflid (clid,flid) WITH IGNORE_DUP_KEY;\n"
			"INSERT INTO #clidflid SELECT c.Id, f.Id FROM Class$ c\n"
			"JOIN Field$ f ON f.Class = c.Id AND f.DstCls = %<0>d AND f.Type IN (%<1>d,%<2>d,%<3>d)\n"
			"WHERE (c.Id IN (SELECT Class FROM Field$ WHERE DstCls = %<0>d));\n"
			"SELECT @oldsize = COUNT(*) FROM #clidflid;\n"
			"WHILE (@oldsize > 0)\n"
			"BEGIN\n"
			"  INSERT INTO #clidflid SELECT c.Id,f.flid FROM #clidflid f\n"
			"  JOIN Class$ c ON c.Base = f.clid;\n"
			"  SELECT @newsize = COUNT(*) FROM #clidflid\n"
			"  IF (@newsize = @oldsize) BREAK\n"
			"  SELECT @oldsize = @newsize\n"
			"END;\n"
			"DELETE FROM #clidflid WHERE flid = %<4>d;\n"
			"DELETE FROM #clidflid WHERE clid IN (SELECT id FROM Class$ WHERE abstract = 1);",
			kclidStText,
			kcptOwningAtom, kcptOwningCollection, kcptOwningSequence,
			kflidCmAnnotation_Text);
	}
	ExecuteSimpleSQL(m_staCmd.Chars(), __LINE__);
	long hobj;
	SDWORD cbhobj;
	long fid;
	SDWORD cbfid;
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	if(CURRENTDB == FB) {
		m_staCmd.Assign("SELECT c.Id, cf.flid FROM CmObject c\n"
			"JOIN Tclidflid cf ON cf.clid = c.Class$\n"
			"LEFT OUTER JOIN CmObject cmo ON cmo.Owner$ = c.Id AND cmo.OwnFlid$ = cf.flid\n"
			"WHERE c.Class$ IN (SELECT clid FROM Tclidflid) AND cmo.Id IS NULL;");
	}
	if(CURRENTDB == MSSQL) {
		m_staCmd.Assign("SELECT c.Id, cf.flid FROM CmObject c\n"
			"JOIN #clidflid cf ON cf.clid = c.Class$\n"
			"LEFT OUTER JOIN CmObject cmo ON cmo.Owner$ = c.Id AND cmo.OwnFlid$ = cf.flid\n"
			"WHERE c.Class$ IN (SELECT clid FROM #clidflid) AND cmo.Id IS NULL;");
	}
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(m_staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbhobj);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &fid, isizeof(fid), &cbfid);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		stfi.vhobjOwner.Push(hobj);
		stfi.vfid.Push(fid);
	}
	sstmt.Clear();
	if(CURRENTDB == FB) {
		ExecuteSimpleSQL("DROP TABLE Tclidflid;", __LINE__);
	}
	if(CURRENTDB == MSSQL) {
		ExecuteSimpleSQL("DROP TABLE #clidflid;", __LINE__);
	}
	stfi.vhobjOwner.EnsureSpace(0, true);
	stfi.vfid.EnsureSpace(0, true);
	int cobjTotal = stfi.vfid.Size();
	if (cobjTotal == 0)
		return;
	stfi.vcid.Resize(cobjTotal);
	stfi.vcpt.Resize(cobjTotal);
	stfi.vhobj.Resize(cobjTotal);
	int i;
	for (i = 0; i < cobjTotal; ++i)
	{
		// REVIEW: Why not use GenerateNextNewHvo()?
		stfi.vhobj[i] = m_cobj2 + m_hvoMin + m_cobjNewTargets + i + 1;
		stfi.vcid[i] = kclidStText;
		stfi.vcpt[i] = kcptOwningAtom;
	}
	stfi.vnParamStatus.Resize(cobjTotal);
}


/*----------------------------------------------------------------------------------------------
	Bind the parameters for one pass through creating empty StText objects.

	@param hstmt handle to an ODBC SQL statement object
	@param stfi Reference to a StTextFieldInfo data structure.
	@param iePass starting index into the data vectors for this pass
	@param cobjPass number of objects to create in this pass
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::BindStTextParameters(SQLHSTMT hstmt, StTextFieldInfo & stfi,
	int iePass, int cobjPass)
{
	// Specify the number of elements in each parameter array.
	// Specify an array in which to return the status of each set of parameters.
	// Specify an SQLUINTEGER value in which to return the number of sets of parameters
	// processed.
	RETCODE rc;
	rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAMSET_SIZE, reinterpret_cast<void *>(cobjPass), 0);
	VerifySqlRc(rc, hstmt, __LINE__);
	rc = SQLSetStmtAttr(hstmt, SQL_ATTR_PARAM_STATUS_PTR, stfi.vnParamStatus.Begin() + iePass,
		0);
	VerifySqlRc(rc, hstmt, __LINE__);
	// Bind the parameters.
	rc = SQLBindParameter(hstmt, 1, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		stfi.vcid.Begin() + iePass, 0, NULL);
	VerifySqlRc(rc, hstmt, __LINE__);
	rc = SQLBindParameter(hstmt, 2, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		stfi.vhobj.Begin() + iePass, 0, NULL);
	VerifySqlRc(rc, hstmt, __LINE__);
	rc = SQLBindParameter(hstmt, 3, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		stfi.vhobjOwner.Begin() + iePass, 0, NULL);
	VerifySqlRc(rc, hstmt, __LINE__);
	rc = SQLBindParameter(hstmt, 4, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		stfi.vfid.Begin() + iePass, 0, NULL);
	VerifySqlRc(rc, hstmt, __LINE__);
	rc = SQLBindParameter(hstmt, 5, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		stfi.vcpt.Begin() + iePass, 0, NULL);
	VerifySqlRc(rc, hstmt, __LINE__);
}


/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing.

	@param stfi Reference to a StTextFieldInfo data structure.
	@param cParamsProcessed the number of parameters processed by SQLExecute()
	@param iePass starting index into the data vectors for this pass
	@param cobjPass number of objects to create in this pass
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CheckStTextParamsForSuccess(const StTextFieldInfo & stfi,
	SQLUINTEGER cParamsProcessed, int iePass, int cobjPass)
{
	int icls;
	int cid = kclidStText;
	m_pfwxd->MapCidToIndex(cid, &icls);
	int cSuccess = 0;
	StrAnsi staFmt;
	for (uint i = 0; i < cParamsProcessed; ++i)
	{
		staFmt.Clear();
		switch (stfi.vnParamStatus[iePass + i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			++cSuccess;
			break;
		case SQL_PARAM_ERROR:
			// "SQL_PARAM_ERROR creating %S (%d, %g, %d, %d, %d)"
			staFmt.Load(kstidXmlErrorMsg100);
			break;
		case SQL_PARAM_UNUSED:
			// "SQL_PARAM_UNUSED creating %S (%d, %g, %d, %d, %d)"
			staFmt.Load(kstidXmlErrorMsg101);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "SQL_PARAM_DIAG_UNAVAILABLE INFO creating %S (%d, %g, %d, %d, %d)"
			staFmt.Load(kstidXmlErrorMsg099);
			break;
		}
		if (staFmt.Length())
		{
			GUID guid;
			memset(&guid, 0, isizeof(guid));
			StrAnsi sta;
			sta.Format(staFmt.Chars(),
				m_pfwxd->ClassName(icls).Chars(),
				stfi.vhobj[iePass + i],
				&guid,
				stfi.vhobjOwner[iePass + i],
				stfi.vfid[iePass + i],
				stfi.vcpt[iePass + i]);
			LogMessage(sta.Chars());
		}
	}
#ifdef LOG_SQL
	// "    %d Parameter%s processed, %d successful, %d attempted"
	staFmt.Load(kstidXmlInfoMsg001);
	StrAnsi staParam;
	if (cParamsProcessed == 1)
		staParam.Load(kstidParameter);
	else
		staParam.Load(kstidParameters);
	sta.Format(staFmt.Chars(), cParamsProcessed, staParam.Chars(), cSuccess, cobjPass);
	LogMessage(sta.Chars());
#endif
}


/*----------------------------------------------------------------------------------------------
	Adjust the parameter values and create an empty StTxtParagraph for each empty StText that
	has just been created.

	@param hstmt handle to an ODBC SQL statement object
	@param stfi Reference to a StTextFieldInfo data structure.
	@param iePass starting index into the data vectors for this pass
	@param cobjPass number of objects to create in this pass
	@param cobjTotal total number of empty StText objects to create
----------------------------------------------------------------------------------------------*/
RETCODE FwXmlImportData::CreateEmptyStTxtParagraphs(SQLHSTMT hstmt,
	StTextFieldInfo & stfi, int iePass, int cobjPass, int cobjTotal)
{
	for (int i = 0; i < cobjPass; ++i)
	{
		stfi.vhobjOwner[iePass + i] = stfi.vhobj[iePass + i];
		// REVIEW: Why not use GenerateNextNewHvo()?
		stfi.vhobj[iePass + i] = m_cobj2 + m_hvoMin + m_cobjNewTargets + cobjTotal + iePass +
			i + 1;
		stfi.vcid[iePass + i] = kclidStTxtPara;
		stfi.vfid[iePass + i] = kflidStText_Paragraphs;
		stfi.vcpt[iePass + i] = kcptOwningSequence;
	}
	// Execute the command with the new parameters for StTxtPara objects.
	return SQLExecute(hstmt);
}


/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing.

	@param stfi Reference to a StTextFieldInfo data structure.
	@param cParamsProcessed the number of parameters processed by SQLExecute()
	@param iePass starting index into the data vectors for this pass
	@param cobjPass number of objects to create in this pass
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CheckStTxtParagraphParamsForSuccess(const StTextFieldInfo & stfi,
	SQLUINTEGER cParamsProcessed, int iePass, int cobjPass)
{
	int icls;
	int cid = kclidStTxtPara;
	m_pfwxd->MapCidToIndex(cid, &icls);
	int cSuccess = 0;
	StrAnsi staFmt;
	for (uint i = 0; i < cParamsProcessed; ++i)
	{
		staFmt.Clear();
		switch (stfi.vnParamStatus[i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			++cSuccess;
			break;
		case SQL_PARAM_ERROR:
			// "SQL_PARAM_ERROR creating %S (%d, %g, %d, %d, %d)"
			staFmt.Load(kstidXmlErrorMsg100);
			break;
		case SQL_PARAM_UNUSED:
			// "SQL_PARAM_UNUSED creating %S (%d, %g, %d, %d, %d)"
			staFmt.Load(kstidXmlErrorMsg101);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "SQL_PARAM_DIAG_UNAVAILABLE INFO creating %S (%d, %g, %d, %d, %d)"
			staFmt.Load(kstidXmlErrorMsg099);
			break;
		}
		if (staFmt.Length())
		{
			GUID guid;
			memset(&guid, 0, isizeof(guid));
			StrAnsi sta;
			sta.Format(staFmt.Chars(),
				m_pfwxd->ClassName(icls).Chars(),
				stfi.vhobj[iePass + i],
				&guid,
				stfi.vhobjOwner[iePass + i],
				stfi.vfid[iePass + i],
				stfi.vcpt[iePass + i]);
			LogMessage(sta.Chars());
		}
	}
#ifdef LOG_SQL
	// "    %d Parameter%s processed, %d successful, %d attempted"
	staFmt.Load(kstidXmlInfoMsg001);
	if (cParamsProcessed == 1)
		staParam.Load(kstidParameter);
	else
		staParam.Load(kstidParameters);
	sta.Format(staFmt.Chars(), cParamsProcessed, staParam.Chars(), cSuccess, cobjPass);
	LogMessage(sta.Chars());
#endif
}


/*----------------------------------------------------------------------------------------------
	Use the collected information about EntryOrSense links to create all the actual links
	in the database.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreEntryOrSenseLinks()
{
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		FillHashMapHvoToSenses("SELECT Src, Dst FROM LexEntry_Senses ORDER BY Src, Dst, Ord;",
			m_hmhvoEntryvhvoSenses);
		FillHashMapHvoToSenses("SELECT Src, Dst FROM LexSense_Senses ORDER BY Src, Dst, Ord;",
			m_hmhvoSensevhvoSenses);
		FillHashMapHeadwordToHvo(m_hmsuHeadwordHvo, m_vmesi[0].m_ws);
	}

	for (int i = 0; i < m_vmesi.Size(); ++i)
	{
		EntryOrSenseLinkInfo & mesi = m_vmesi[i];
		mesi.m_hvoTarget = EntryOrSenseHvo(mesi.m_hobj, mesi.m_fSense, mesi.m_staEntry, 0);
		if (!mesi.m_hvoTarget)
			continue;
		// Store this in a HashMap
		HvoVector hv;
		if (mesi.m_flid == kflidLexEntryRef_ComponentLexemes)
		{
			if (m_hmhvoEntryvhvoComponent.Retrieve(mesi.m_hobj, &hv))
			{
				hv.m_pvhvo->Push(mesi.m_hvoTarget);
				hv.m_pvhvo = NULL;
			}
			else
			{
				// Create a new Vector and add it to the HashMap.
				hv.m_pvhvo = new Vector<int>();
				hv.m_pvhvo->Push(mesi.m_hvoTarget);
				m_hmhvoEntryvhvoComponent.Insert(mesi.m_hobj, hv);
				hv.m_pvhvo = NULL;
			}
		}
		else
		{
			if (m_hmhvoEntryvhvoPrimary.Retrieve(mesi.m_hobj, &hv))
			{
				hv.m_pvhvo->Push(mesi.m_hvoTarget);
				hv.m_pvhvo = NULL;
			}
			else
			{
				// Create a new Vector and add it to the HashMap.
				hv.m_pvhvo = new Vector<int>();
				hv.m_pvhvo->Push(mesi.m_hvoTarget);
				m_hmhvoEntryvhvoPrimary.Insert(mesi.m_hobj, hv);
				hv.m_pvhvo = NULL;
			}
		}
	}

	// Fill the LexEntryRef_ComponentLexemes and LexEntryRef_PrimaryLexemes tables with the
	// accumulated link info.
	StoreEntryOrSenseLinks(m_hmhvoEntryvhvoComponent, "LexEntryRef_ComponentLexemes");
	StoreEntryOrSenseLinks(m_hmhvoEntryvhvoPrimary, "LexEntryRef_PrimaryLexemes");

	// Finish executing remaining commands
	if (m_staCmd.Length())
	{
		UpdateDatabaseObjects(m_sdb, __LINE__);
		m_staCmd.Clear();
	}
}

/*----------------------------------------------------------------------------------------------
	Look up the database id for the LexEntry or LexSense object given by staEntry.

	@param hmhvovhvo Reference to HashMap
	@param pszTable Name of the database table
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreEntryOrSenseLinks(HashMap<int, HvoVector> & hmhvovhvo,
	const char * pszTable)
{
	HashMap<int, HvoVector>::iterator hmit;
	for (hmit = hmhvovhvo.Begin(); hmit != hmhvovhvo.End(); ++hmit)
	{
		int hobj = hmit->GetKey();
		HvoVector & hv = hmit->GetValue();
		for (int i = 0; i < hv.m_pvhvo->Size(); ++i)
		{
			if(CURRENTDB == FB || CURRENTDB == MSSQL) {
				m_staCmd.FormatAppend("INSERT INTO %s (Src, Dst, Ord) VALUES (%d, %d, %d);%n",
					pszTable, hobj, (*hv.m_pvhvo)[i], i+1);
			}
			// Execute the accumulated commands if they get too long
			if (m_staCmd.Length() > 2000)
			{
				UpdateDatabaseObjects(m_sdb, __LINE__);
				m_staCmd.Clear();
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	For any newly created complex form entries, ensure that a sense exists.  See LT-9153.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateMissingSensesForComplexForms()
{
	StrAnsi staCmd;
	staCmd.Format("DECLARE @fCreate BIT%n"
		"SET @fCreate=0%n"
		"if object_id('LexEntry_EntryRefs') IS NULL BEGIN%n"
		"	SET @fCreate=1%n"
		"	CREATE TABLE LexEntry_EntryRefs (Src INT, Dst INT, Ord INT)%n"
		"	CREATE TABLE LexEntryRef_ComplexEntryTypes (Src INT, Dst INT, Ord INT)%n"
		"	CREATE TABLE LexEntryRef_PrimaryLexemes (Src INT, Dst INT, Ord INT)%n"
		"END%n"
		"SELECT DISTINCT r.Dst, r.Src%n"
		"FROM LexEntry_EntryRefs r%n"
		"LEFT OUTER JOIN LexEntry_Senses s ON s.Src=r.Src%n"
		"LEFT OUTER JOIN LexEntryRef_ComplexEntryTypes t ON t.Src=r.Dst%n"
		"LEFT OUTER JOIN LexEntryRef_PrimaryLexemes p ON p.Src=r.Dst%n"
		"WHERE s.Dst IS NULL AND (t.Dst IS NOT NULL OR p.Dst IS NOT NULL);%n"
		"IF @fCreate<>0 BEGIN%n"
		"    DROP TABLE LexEntry_EntryRefs%n"
		"    DROP TABLE LexEntryRef_ComplexEntryTypes%n"
		"    DROP TABLE LexEntryRef_PrimaryLexemes%n"
		"END;"
		);
	HashMap<int, int> hmhvoEntry;
	FillHashMapHvoToInt(staCmd.Chars(), hmhvoEntry);
	for (int i = 0; i < m_vmesi.Size(); ++i)
	{
		CreateEmptySenseForEntry(m_vmesi[i].m_hobj, hmhvoEntry);
	}
	for (Set<int>::iterator it = m_sethvoLexEntryRef.Begin();
		 it != m_sethvoLexEntryRef.End();
		 ++it)
	{
		CreateEmptySenseForEntry(it->GetValue(), hmhvoEntry);
	}
}

/*----------------------------------------------------------------------------------------------
	Create an empty sense for any entry that owns the LexEntryRef given by hvoRef.

	@param hvoRef database id of a LexEntryRef for a complex form
	@param hmhvoEntry mapping from LexEntryRef ids to their owning LexEntry ids
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateEmptySenseForEntry(int hvoRef, HashMap<int, int> & hmhvoEntry)
{
	int hvoEntry;
	if (hmhvoEntry.Retrieve(hvoRef, &hvoEntry))
	{
		// Create a sense for this entry.
		StrAnsi staCmd;
		int hvoSense = GenerateNextNewHvo();
		GUID guidSense;
		GenerateNewGuid(&guidSense);
		if (CURRENTDB == FB) {
			staCmd.Format(
			  "EXECUTE PROCEDURE CreateOwnedObject$ %<0>u, %<1>u, '%<2>g', %<3>u, %<4>u, %<5>u;",
				kclidLexSense, hvoSense, &guidSense, hvoEntry, kflidLexEntry_Senses,
				kcptOwningSequence);
		}
		if (CURRENTDB == MSSQL) {
			staCmd.Format("EXEC CreateOwnedObject$ %<0>u, %<1>u, '%<2>g', %<3>u, %<4>u, %<5>u;",
				kclidLexSense, hvoSense, &guidSense, hvoEntry, kflidLexEntry_Senses,
				kcptOwningSequence);
		}
		ExecuteSimpleSQL(staCmd.Chars(), __LINE__);
	}
}

/*----------------------------------------------------------------------------------------------
	Look up the database id for the LexEntry or LexSense object given by staEntry.

	@param hobj Database id of the current object.
	@param fSense Flag whether looking at a Sense or Entry.
	@param staEntry Contains value of the sense or entry Xml attribute.
	@param hvoLexRefType Database id of the relevant LexRefType, or zero if looking at
			LexEntryRef field.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::EntryOrSenseHvo(int hobj, bool fSense, StrAnsi & staEntry,
	int hvoLexRefType)
{
	StrUni stu;
	int ich = staEntry.ReverseFindCh(' ');
	if (ich < 0)
	{
		ich = staEntry.Length();
	}
	else
	{
		char ch = staEntry.GetAt(ich + 1);
		if (!isascii(ch) || !isdigit(ch))
			ich = staEntry.Length();
	}
	StrUtil::StoreUtf16FromUtf8(staEntry.Chars(), ich, stu);
	StrUtil::NormalizeStrUni(stu, UNORM_NFD);
	int hvoEntry;
	if (!m_hmsuHeadwordHvo.Retrieve(stu, &hvoEntry))
	{
		// Create a dummy entry (and possibly sense) with the given lexeme form.
		return CreateDummyEntry(staEntry.Left(ich).Chars(), fSense);
	}
	int hvoTarget;
	if (fSense)
	{
		const char * pszSenseNum;
		if (ich < staEntry.Length())
		{
			pszSenseNum = staEntry.Chars() + ich + 1;
			if (!isascii(*pszSenseNum) || !isdigit(*pszSenseNum))
			{
				// report this error and continue
				AppendImportResidue(hobj, fSense, staEntry, hvoLexRefType);
				return 0;
			}
		}
		else
		{
			pszSenseNum = "1";
		}
		hvoTarget = TraceSenseNumber(hvoEntry, pszSenseNum, 0);
	}
	else
	{
		hvoTarget = hvoEntry;
	}

	if (hvoTarget == 0 || hvoTarget == -1)
	{
		// report this error and continue
		AppendImportResidue(hobj, fSense, staEntry, hvoLexRefType);
		return 0;
	}

	return hvoTarget;
}

/*----------------------------------------------------------------------------------------------
	Create a dummy entry with the given form, and an appropriate comment in the ImportResidue.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::CreateDummyEntry(const char * pszForm, bool fSense)
{
	StrAnsi staCmd;
	bool fIsNull;
	int hobjLexDb;
	const char * pszTable = (m_pfwxd->DbVersion() <= 200202) ? "LexicalDatabase" : "LexDb";
	if (CURRENTDB == FB)
		staCmd.Format("SELECT FIRST 1 Id FROM %s;", pszTable);
	else if (CURRENTDB == MSSQL)
		staCmd.Format("SELECT TOP 1 Id FROM %s;", pszTable);
	hobjLexDb = ReadOneIntFromSQL(staCmd.Chars(), __LINE__, fIsNull);
	StrAnsi sta;
	if (fIsNull)
	{
		// "Error obtaining LexDb id from the database!?\n"
		sta.Load(kstidXmlErrorMsg166);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
	int hobjEntry = GenerateNextNewHvo();
	int hobjForm = GenerateNextNewHvo();
	GUID guid;
	GUID guidForm;
	GenerateNewGuid(&guid);
	GenerateNewGuid(&guidForm);
	if (CURRENTDB == FB)
	{
		staCmd.Format("EXECUTE PROCEDURE CreateObject$ %<0>u, %<1>u, '%<2>g';%n"
			"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u WHERE Id=%<1>u;%n"
			"EXECUTE PROCEDURE CreateObject$ %<5>u, %<6>u, '%<7>g';%n"
			"UPDATE CmObject SET Owner$=%<1>u, OwnFlid$=%<8>u WHERE Id=%<6>u;%n"
			"INSERT INTO MoForm_Form (Obj, Ws, Txt) VALUES (%<6>u, %<9>d, ?);%n",
			kclidLexEntry, hobjEntry, &guid, hobjLexDb, kflidLexDb_Entries,
			kclidMoStemAllomorph, hobjForm, &guidForm, kflidLexEntry_LexemeForm, m_wsVern);
	}
	else if (CURRENTDB == MSSQL)
	{
		staCmd.Format("EXEC CreateObject$ %<0>u, %<1>u, '%<2>g';%n"
			"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u WHERE Id=%<1>u;%n"
			"EXEC CreateObject$ %<5>u, %<6>u, '%<7>g';%n"
			"UPDATE CmObject SET Owner$=%<1>u, OwnFlid$=%<8>u WHERE Id=%<6>u;%n"
			"INSERT INTO MoForm_Form (Obj, Ws, Txt) VALUES (%<6>u, %<9>d, ?);%n",
			kclidLexEntry, hobjEntry, &guid, hobjLexDb, kflidLexDb_Entries,
			kclidMoStemAllomorph, hobjForm, &guidForm, kflidLexEntry_LexemeForm, m_wsVern);
	}
	ExecuteParameterizedSQL(staCmd.Chars(), __LINE__, pszForm);

	// Make this available for any other cross references.
	StrUni stu;
	StrUtil::StoreUtf16FromUtf8(pszForm, strlen(pszForm), stu);
	StrUtil::NormalizeStrUni(stu, UNORM_NFD);
	m_hmsuHeadwordHvo.Insert(stu, hobjEntry);

	// Create a sense for the entry.
	int hobjSense = GenerateNextNewHvo();
	GUID guidSense;
	GenerateNewGuid(&guidSense);
	if (CURRENTDB == FB)
	{
		staCmd.Format("EXECUTE PROCEDURE CreateObject$ %<0>u, %<1>u, '%<2>g';%n"
			"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u, OwnOrd$=1 WHERE Id=%<1>u;%n",
			kclidLexSense, hobjSense, &guidSense, hobjEntry, kflidLexEntry_Senses);
	}
	else if (CURRENTDB == MSSQL)
	{
		staCmd.Format("EXEC CreateObject$ %<0>u, %<1>u, '%<2>g';%n"
			"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u, OwnOrd$=1 WHERE Id=%<1>u;%n",
			kclidLexSense, hobjSense, &guidSense, hobjEntry, kflidLexEntry_Senses);
	}
	ExecuteSimpleSQL(staCmd.Chars(), __LINE__);

	// Create an unspecified MoStemMsa in the entry, and link the sense to it.
	int hobjMsa = GenerateNextNewHvo();
	GUID guidMsa;
	GenerateNewGuid(&guidMsa);
	if (CURRENTDB == FB)
	{
		staCmd.Format("EXECUTE PROCEDURE CreateObject$ %<0>u, %<1>u, '%<2>g';%n"
			"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u WHERE Id=%<1>u;%n"
			"UPDATE LexSense SET MorphoSyntaxAnalysis=%<1>u WHERE Id=%<5>u",
			kclidMoStemMsa, hobjMsa, &guidMsa, hobjEntry, kflidLexEntry_MorphoSyntaxAnalyses,
			hobjSense);
	}
	else if (CURRENTDB == MSSQL)
	{
		staCmd.Format("EXEC CreateObject$ %<0>u, %<1>u, '%<2>g';%n"
			"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u WHERE Id=%<1>u;%n"
			"UPDATE LexSense SET MorphoSyntaxAnalysis=%<1>u WHERE Id=%<5>u",
			kclidMoStemMsa, hobjMsa, &guidMsa, hobjEntry, kflidLexEntry_MorphoSyntaxAnalyses,
			hobjSense);
	}
	ExecuteSimpleSQL(staCmd.Chars(), __LINE__);

	// Add import residue to the entry and sense, and note in the log file that we created this
	// entry for a cross reference (or lexical relation).
	StrUni stuResidue;
	Vector<byte> vbFmt;
	CreateSimpleFormat(DefaultAnalysisWritingSystem(), vbFmt);
	if (fSense)
	{
		stuResidue.Assign(
	  "This was automatically created to satisfy a lexical relation, and it should be checked.");
		sta.Format(
			"%<0>s:%<1>d: Created an entry for \"%<2>s\" to satisfy a lexical relation.\n",
			m_stabpFile.Chars(), hobjEntry, pszForm);
	}
	else
	{
		stuResidue.Assign(
	   "This was automatically created to satisfy a cross reference, and it should be checked.");
		sta.Format("%<0>s:%<1>d: Created an entry for \"%<2>s\" to satisfy a cross reference.\n",
			m_stabpFile.Chars(), hobjEntry, pszForm);
	}
	StoreImportResidue(stuResidue, vbFmt, hobjEntry, "LexEntry");
	StoreImportResidue(stuResidue, vbFmt, hobjSense, "LexSense");
	LogMessage(sta.Chars());

	if (fSense)
		return hobjSense;
	else
		return hobjEntry;
}

/*----------------------------------------------------------------------------------------------
	Use the collected information about lexical relations to create all the LexReference objects
	in the database.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreLexicalReferences()
{
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		FillHashMapHvoToSenses("SELECT Src, Dst FROM LexEntry_Senses ORDER BY Src, Dst, Ord;",
			m_hmhvoEntryvhvoSenses);
		FillHashMapHvoToSenses("SELECT Src, Dst FROM LexSense_Senses ORDER BY Src, Dst, Ord;",
			m_hmhvoSensevhvoSenses);
		FillHashMapHeadwordToHvo(m_hmsuHeadwordHvo, m_vlri[0].m_wsv);
		FillHashMapHvoToInt("SELECT Id, MappingType FROM LexRefType;", m_hmhvoTypeMappingType);
	}
	for (int i = 0; i < m_vlri.Size(); ++i)
	{
		LexRelationInfo & lri = m_vlri[i];
		if (lri.m_hvoTarget != 0)
			continue;		// handled as part of a collection in an earlier pass

		lri.m_hvoTarget = EntryOrSenseHvo(lri.m_hobj, lri.m_fSense, lri.m_staSense,
			lri.m_hvoLexRefType);
		if (!lri.m_hvoTarget)
			continue;

		int nMappingType = 0;
		if (!m_hmhvoTypeMappingType.Retrieve(lri.m_hvoLexRefType, &nMappingType))
		{
			// report this error and continue
			AppendImportResidue(lri.m_hobj, lri.m_fSense, lri.m_staSense, lri.m_hvoLexRefType);
			continue;
		}
		// Chain all the members of a collection together.  They need to be handled as a set.
		// See LT-6522 for details.
		if (nMappingType == kmtSenseCollection ||
			nMappingType == kmtEntryCollection ||
			nMappingType == kmtEntryOrSenseCollection)
		{
			for (int j = i + 1, iPrev = i; j < m_vlri.Size(); ++j)
			{
				LexRelationInfo & lri2 = m_vlri[j];
				if (lri.m_hobj == lri2.m_hobj)
				{
					if (lri.m_hvoLexRefType == lri2.m_hvoLexRefType &&
						lri.m_fReverse == lri2.m_fReverse)
					{
						lri2.m_hvoTarget = EntryOrSenseHvo(lri2.m_hobj, lri2.m_fSense, lri2.m_staSense,
							lri2.m_hvoLexRefType);
						if (!lri2.m_hvoTarget)
							continue;
						m_vlri[iPrev].m_plriNext = &lri2;
						iPrev = j;
					}
				}
				else
				{
					break;
				}
			}
		}

		LexReferenceVec lrv;
		if (m_hmhvoTypeLexReferences.Retrieve(lri.m_hvoLexRefType, &lrv))
		{
			bool fDone = false;
			for (int ilr = 0; ilr < lrv.m_pvlr->Size(); ++ilr)
			{
				if (AddToLexReferenceIfPossible(nMappingType, (*lrv.m_pvlr)[ilr], lri))
				{
					fDone = true;
					break;
				}
			}
			if (!fDone)
			{
				// add another LexReference to the vector.
				LexReference lr;
				if (InitializeLexReference(nMappingType, lr, lri))
					lrv.m_pvlr->Push(lr);
			}
			lrv.m_pvlr = NULL; // Prevent Destructor from destroying data.
		}
		else
		{
			// Create the initial LexReference object and stick it in the Vector
			// and then in the HashMap.
			LexReference lr;
			if (InitializeLexReference(nMappingType, lr, lri))
			{
				LexReferenceVec lrvNew;
				lrvNew.m_pvlr = new Vector<LexReference>();
				lrvNew.m_pvlr->Push(lr);
				m_hmhvoTypeLexReferences.Insert(lri.m_hvoLexRefType, lrvNew);
				lrvNew.m_pvlr = NULL;
			}
		}
	}
	// iterate through the m_hmhvoTypeLexReferences HashMap
	// and then iterate through internal LexReference Vector to create all
	// the LexReference objects in the database.
	HashMap<int, LexReferenceVec>::iterator hmit;
	for (hmit = m_hmhvoTypeLexReferences.Begin(); hmit != m_hmhvoTypeLexReferences.End(); ++hmit)
	{
		int hvoLexRefType = hmit->GetKey();
		LexReferenceVec & lrv = hmit->GetValue();
		for (int i = 0; i < lrv.m_pvlr->Size(); ++i)
		{
			CreateDbLexReference(hvoLexRefType, (*lrv.m_pvlr)[i]);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Generate the next new database object id to use.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GenerateNextNewHvo()
{
	return m_hvoMin + m_cobj + m_cobjNewTargets++;
}

/*----------------------------------------------------------------------------------------------
	Initialize a LexReference with the information from one binary lexical relation.

	@param hvoLexRefType The database id of the LexRefType which will own the LexReference
	@param lr Reference to the LexReference struct containing the member database ids
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateDbLexReference(int hvoLexRefType, LexReference & lr)
{
	// Create a LexReference database object
	int hvoRef = GenerateNextNewHvo();
	GUID guid;
	GenerateNewGuid(&guid);
	if(CURRENTDB == FB) {
		//TODO:  not sure what to do about RETURNING_VALUES from CreateObject$
		m_staCmd.Format("EXECUTE PROCEDURE CreateObject$ (%<0>u, %<1>u, '%<2>g'); %n"
			"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u WHERE Id=%<1>u;",
			kclidLexReference, hvoRef, &guid, hvoLexRefType, kflidLexRefType_Members);
	}
	if(CURRENTDB == MSSQL) {
		m_staCmd.Format("EXEC CreateObject$ %<0>u, %<1>u, '%<2>g'; %n"
			"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u WHERE Id=%<1>u;",
			kclidLexReference, hvoRef, &guid, hvoLexRefType, kflidLexRefType_Members);
	}
	ExecuteSimpleSQL(m_staCmd.Chars(), __LINE__);

	int iOrd = 1;
	if (lr.m_hvoTop != 0)
	{
		// Insert the root element of a tree relation first.
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			m_staCmd.Format(
				"INSERT INTO LexReference_Targets (Src, Dst, Ord) VALUES (%d, %d, %d);%n",
				hvoRef, lr.m_hvoTop, iOrd++);
		}
	}
	else
	{
		m_staCmd.Clear();
	}
	if (lr.m_vhvo.Size())
	{
		Assert(lr.m_hvoTop == 0);
		Assert(lr.m_setHvo.Size() == 0);
		for (int i = 0; i < lr.m_vhvo.Size(); ++i)
		{
			// Insert another element in the LexReference
			if(CURRENTDB == FB || CURRENTDB == MSSQL) {
				m_staCmd.FormatAppend(
					"INSERT INTO LexReference_Targets (Src, Dst, Ord) VALUES (%d, %d, %d);%n",
					hvoRef, lr.m_vhvo[i], i+1);
			}

			// Execute the accumulated commands if they get too long
			if (m_staCmd.Length() > 2000)
			{
				UpdateDatabaseObjects(m_sdb, __LINE__);
				m_staCmd.Clear();
			}
		}
	}
	else
	{
		Set<int>::iterator it;
		for (it = lr.m_setHvo.Begin(); it != lr.m_setHvo.End(); ++it)
		{
			if (it->GetValue() != lr.m_hvoTop)
			{
				// Insert another element in the LexReference
				if(CURRENTDB == FB || CURRENTDB == MSSQL) {
					m_staCmd.FormatAppend(
						"INSERT INTO LexReference_Targets (Src, Dst, Ord) VALUES (%d, %d, %d);%n",
						hvoRef, it->GetValue(), iOrd++);
				}

				// Execute the accumulated commands if they get too long
				if (m_staCmd.Length() > 2000)
				{
					UpdateDatabaseObjects(m_sdb, __LINE__);
					m_staCmd.Clear();
				}
			}
		}
	}
	// Finish executing remaining commands
	if (m_staCmd.Length())
	{
		UpdateDatabaseObjects(m_sdb, __LINE__);
		m_staCmd.Clear();
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize a LexReference with the information from one binary lexical relation.

	@param nMappingType The type of lexical relation being processed
	@param lr Reference to an empty LexReference struct
	@param lri Reference to a binary lexical relation (which may be part of a collection or
				tree).

	@return true if the information is added okay, false if an error occurs.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::InitializeLexReference(int nMappingType, LexReference & lr,
	LexRelationInfo & lri)
{
	switch (nMappingType)
	{
	case kmtSenseCollection:
	case kmtEntryCollection:
	case kmtEntryOrSenseCollection:
		lr.m_setHvo.Insert(lri.m_hobj);
		for (LexRelationInfo * plri = &lri; plri; plri = plri->m_plriNext)
			lr.m_setHvo.Insert(plri->m_hvoTarget);
		return true;
	case kmtSensePair:
	case kmtEntryPair:
	case kmtEntryOrSensePair:
		lr.m_setHvo.Insert(lri.m_hobj);
		lr.m_setHvo.Insert(lri.m_hvoTarget);
		return true;
	case kmtSenseTree:
	case kmtEntryTree:
	case kmtEntryOrSenseTree:
	case kmtSenseAsymmetricPair:
	case kmtEntryAsymmetricPair:
	case kmtEntryOrSenseAsymmetricPair:
		lr.m_setHvo.Insert(lri.m_hobj);
		lr.m_setHvo.Insert(lri.m_hvoTarget);
		if (lri.m_fReverse)
			lr.m_hvoTop = lri.m_hvoTarget;
		else
			lr.m_hvoTop = lri.m_hobj;
		return true;
	case kmtSenseSequence:
	case kmtEntrySequence:
	case kmtEntryOrSenseSequence:
		Assert(lri.m_ord == 1);
		lr.m_vhvo.Push(lri.m_hvoTarget);
		return true;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Create a one-run, one-property (the writing system) format.  One size fits all!

	@param ws writing system database id
	@param vbFmt Reference to an array for holding the format (output)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateSimpleFormat(int ws, Vector<byte> & vbFmt)
{
	// Create default format based on the primary analysis writing system.
	vbFmt.Resize(19);
	int crun = 1;
	FwXml::BasicRunInfo bri = { 0, 0 };
	int cbBin = sizeof(crun);
	memcpy(vbFmt.Begin(), &crun, sizeof(int));
	memcpy(vbFmt.Begin() + cbBin, &bri, sizeof(bri));
	cbBin += sizeof(FwXml::BasicRunInfo);
	vbFmt[cbBin] = 1;			// Number of integer-valued properties.
	++cbBin;
	vbFmt[cbBin] = 0;			// Number of string-valued properties.
	++cbBin;
	vbFmt[cbBin] = kscpWs;		// kscpWs = SCP4(ktptWs)
	++cbBin;
	memcpy(vbFmt.Begin() + cbBin, &ws, sizeof(int));
	cbBin += sizeof(int);
	Assert(cbBin == vbFmt.Size());
}

/*----------------------------------------------------------------------------------------------
	Add the binary lexical relation info to the appropriate ImportResidue field in the database.
	TODO: Handle formatting more intelligently, on a per import field basis.

	@param hobj Database id of the current object.
	@param fSense Flag whether looking at a Sense or Entry.
	@param staEntry Contains value of the sense or entry Xml attribute.
	@param hvoLexRefType Database id of the relevant LexRefType, or zero if looking at
			LexEntryRef field.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::AppendImportResidue(int hobj, bool fSense, StrAnsi & staEntry,
	int hvoLexRefType)
{
	const char * pszTableName;
	if (fSense)
		pszTableName = "LexSense";
	else
		pszTableName = "LexEntry";

	// Get any existing ImportResidue info
	StrUni stuResidue;
	Vector<byte> vbFmt;
	LoadExistingImportResidue(hobj, pszTableName, stuResidue, vbFmt);
	if (vbFmt.Size() == 0)
		CreateSimpleFormat(DefaultAnalysisWritingSystem(), vbFmt);

	const wchar * pszAbbr = NULL;
	const wchar * pszRelation;
	const wchar * pszAttrName;
	if (hvoLexRefType)
	{
		// Append the LexicalRelations5016 or CrossReferences5002 information.
		if (fSense)
			pszRelation = L"LexicalRelation";
		else
			pszRelation = L"CrossReference";
	}
	else
	{
		pszRelation = L"LexEntryRef";
	}

	if (fSense)
		pszAttrName = L"sense";
	else
		pszAttrName = L"entry";

	if (hvoLexRefType)
	{
		pszAbbr = L"???";
		HashMapStrUni<int>::iterator hmitEnd = m_hmsuAbbrHvoLRType.End();
		HashMapStrUni<int>::iterator hmit;
		for (hmit = m_hmsuAbbrHvoLRType.Begin(); hmit != hmitEnd; ++hmit)
		{
			if (hmit->GetValue() == hvoLexRefType)
			{
				pszAbbr = hmit->GetKey();
				break;
			}
		}
	}
	StrUni stuEntry;
	StrUtil::StoreUtf16FromUtf8(staEntry.Chars(), staEntry.Length(), stuEntry);
	stuResidue.FormatAppend(L"{%s Link failed: ", pszRelation);
	if (pszAbbr)
		stuResidue.FormatAppend(L"abbr=\"%s\" ", pszAbbr);
	stuResidue.FormatAppend(L"%s=\"%s\".}", pszAttrName, stuEntry.Chars());

	StoreImportResidue(stuResidue, vbFmt, hobj, pszTableName);
}

/*----------------------------------------------------------------------------------------------
	Store the import residue for the given object into memory.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreImportResidue(StrUni & stuResidue, Vector<byte> & vbFmt, int hobj,
	const char * pszTableName)
{
	int cch = stuResidue.Length();
	SQLINTEGER cbTxt = cch * isizeof(wchar);
	SQLINTEGER cbTxtLine = cbTxt + isizeof(wchar);
	SQLINTEGER cbTxtResidue = cbTxt;
	SQLINTEGER cbFmt = vbFmt.Size();
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR,
		SQL_WLONGVARCHAR, cch+1, 0, const_cast<wchar *>(stuResidue.Chars()),
		cbTxtLine, &cbTxtResidue);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_BINARY,
		SQL_LONGVARBINARY, cbFmt, 0, vbFmt.Begin(), cbFmt, &cbFmt);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

	StrAnsiBuf stab;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		stab.Format("UPDATE %s SET ImportResidue=?, ImportResidue_Fmt=? WHERE Id=%d;", pszTableName, hobj);
	}
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
	sstmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Load any existing import residue from the given table for the given object into memory.

	@param hobj Database id of object possibly containing the import residue
	@param pszTableName name of the database table containing the import residue
	@param stuResidue Reference to string for holding the import residue (output)
	@param vbFmt Reference to an array for holding the import residue formatting (output)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::LoadExistingImportResidue(int hobj, const char * pszTableName,
	StrUni & stuResidue, Vector<byte> & vbFmt)
{
	StrAnsiBuf stab;
	SqlStatement sstmt;
	RETCODE rc;
	wchar rgch[4001];
	SDWORD cbT;
	SDWORD cbFmt;
	int cch = sizeof(rgch) / sizeof(wchar);
	int cch1;
	int cchTotal;

	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		stab.Format("SELECT ImportResidue, ImportResidue_Fmt FROM %s WHERE Id=%d;",
			pszTableName, hobj);
	}
	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
	rc = SQLFetch(sstmt.Hstmt());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	if (rc != SQL_NO_DATA)
	{
		rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_WCHAR, rgch, isizeof(rgch), &cbT);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_SUCCESS && cbT != SQL_NULL_DATA)
		{
			cchTotal = cbT / isizeof(SQLWCHAR);
			if (cch > cchTotal)
				cch = cchTotal;
			if (rgch[cch - 1])
				cch1 = cch;
			else
				cch1 = cch - 1;					// Don't include trailing NUL.
			stuResidue.Assign(rgch, cch1);
			// Get the rest of the string if necessary.
			if (cchTotal > cch)
			{
				Assert(rc == SQL_SUCCESS_WITH_INFO);
				Vector<wchar> vch;
				vch.Resize(cchTotal + 1);
				rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_BINARY, vch.Begin(),
					vch.Size() * isizeof(SQLWCHAR), &cbT);
				VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
				stuResidue.Append(vch.Begin(), cchTotal - cch1);
			}
			// Read the formatting data.
			vbFmt.Resize(400);
			rc = SQLGetData(sstmt.Hstmt(), 2, SQL_C_BINARY, vbFmt.Begin(),
				vbFmt.Size(), &cbFmt);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			if (rc == SQL_NO_DATA || cbFmt == SQL_NULL_DATA || !cbFmt)
			{
				vbFmt.Clear();
				return;		// This shouldn't happen if ImportResidue itself isn't empty!
			}
			cbT = vbFmt.Size();
			if (cbT > cbFmt)
				cbT = cbFmt;
			vbFmt.Resize(cbFmt);
			// Get the rest of the formatting data if necessary.
			if (cbFmt > cbT)
			{
				Assert(rc == SQL_SUCCESS_WITH_INFO);
				rc = SQLGetData(sstmt.Hstmt(), 2, SQL_C_BINARY, vbFmt.Begin() + cbT,
					cbFmt - cbT, &cbFmt);
				VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			}
		}
	}
	sstmt.Clear();

	if (stuResidue.Length())
	{
		stuResidue.Append(L"\x2028");	// Unicode Line Separator char.  (See LT-5389.)
	}
}


/*----------------------------------------------------------------------------------------------
	Add one binary lexical relation to an existing LexReference, if possible.

	@param nMappingType The type of lexical relation being processed
	@param lr Reference to an existing LexReference struct
	@param lri Reference to a binary lexical relation (which may be part of a collection or
				tree).

	@return true if the binary relation has been handled, false if more work remains.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::AddToLexReferenceIfPossible(int nMappingType, LexReference & lr,
	LexRelationInfo & lri)
{
	switch (nMappingType)
	{
	case kmtSenseCollection:
	case kmtEntryCollection:
	case kmtEntryOrSenseCollection:
		return MergeLexReferenceCollections(lr, lri);
	case kmtSensePair:
	case kmtEntryPair:
	case kmtEntryOrSensePair:
		if (lr.m_setHvo.IsMember(lri.m_hobj) && lr.m_setHvo.IsMember(lri.m_hvoTarget))
			return true;
		break;
	case kmtSenseTree:
	case kmtEntryTree:
	case kmtEntryOrSenseTree:
		if (lri.m_fReverse && lri.m_hvoTarget == lr.m_hvoTop)
		{
			// m_hobj is a part
			lr.m_setHvo.Insert(lri.m_hobj);
			return true;
		}
		else if (!lri.m_fReverse && lri.m_hobj == lr.m_hvoTop)
		{
			// m_hvoTarget is a part
			lr.m_setHvo.Insert(lri.m_hvoTarget);
			return true;
		}
		break;
	case kmtSenseSequence:
	case kmtEntrySequence:
	case kmtEntryOrSenseSequence:
		if (lr.m_vhvo.Size() < lri.m_ord)
		{
			lr.m_vhvo.Push(lri.m_hvoTarget);
		}
		else if (lr.m_vhvo[lri.m_ord - 1] != lri.m_hvoTarget)
		{
			// Need to do something here...
			Assert(lri.m_ord == 1);
			return false;
		}
		return true;
	case kmtSenseAsymmetricPair:
	case kmtEntryAsymmetricPair:
	case kmtEntryOrSenseAsymmetricPair:
		if (lr.m_setHvo.IsMember(lri.m_hobj) && lr.m_setHvo.IsMember(lri.m_hvoTarget))
		{
			if (lri.m_fReverse)
			{
				if (lri.m_hvoTarget == lr.m_hvoTop)
					return true;
			}
			else
			{
				if (lri.m_hobj == lr.m_hvoTop)
					return true;
			}
		}
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Add the collection lexical relation to an existing LexReference, if possible.

	@param lr Reference to an existing LexReference struct
	@param lri Reference to a binary lexical relation (which may be part of a collection)

	@return true if the binary relation has been handled, false if more work remains.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::MergeLexReferenceCollections(LexReference & lr, LexRelationInfo & lri)
{
	// Check whether the new collection (the chain starting at lri) is a subset of the
	// existing collection.  If so, we don't need to do anything more.
	// Along the way, put the new collection into a set, and save any hvo values that are not
	// in the existing collection.
	Vector<int> vhvoMissing;
	Set<int> setRel;
	setRel.Insert(lri.m_hobj);
	if (!lr.m_setHvo.IsMember(lri.m_hobj))
		vhvoMissing.Push(lri.m_hobj);
	for (LexRelationInfo * plri = &lri; plri; plri = plri->m_plriNext)
	{
		setRel.Insert(plri->m_hvoTarget);
		if (!lr.m_setHvo.IsMember(plri->m_hvoTarget))
			vhvoMissing.Push(plri->m_hvoTarget);
	}
	if (vhvoMissing.Size() == 0)
		return true;

	// Check whether the existing collection is a subset of the new collection.  If so, add the
	// missing values and return true.  If not, return false.
	for (Set<int>::iterator it = lr.m_setHvo.Begin(); it != lr.m_setHvo.End(); ++it)
	{
		if (!setRel.IsMember(it->GetValue()))
			return false;
	}
	for (int i = 0; i < vhvoMissing.Size(); ++i)
		lr.m_setHvo.Insert(vhvoMissing[i]);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the sense owned by hvoOwner, recursively following the sense number in pszSenseNum.

	@param hvoOwner Database id of a LexEntry or LexSense
	@param pszSenseNum String containing the sense number, for example "1" or "1.2.2"
	@param depth recursion depth: 0 means hvoOwner is LexEntry, >0 means hvoOwner is LexSense

	@return Database id of the desired LexSense or 0 if an error occurs.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::TraceSenseNumber(int hvoOwner, const char * pszSenseNum, int depth)
{
	char * pszNext;
	int iSense = strtol(pszSenseNum, &pszNext, 10);
	if (iSense < 1)
		return -1;
	HvoVector hv;
	int hvoSense;
	if (depth == 0)
	{
		if (!m_hmhvoEntryvhvoSenses.Retrieve(hvoOwner, &hv))
			return -1;
		if (iSense <= hv.m_pvhvo->Size())
			hvoSense = (*hv.m_pvhvo)[iSense - 1];
		hv.m_pvhvo = NULL;
	}
	else
	{
		if (!m_hmhvoSensevhvoSenses.Retrieve(hvoOwner, &hv))
			return -1;
		if (iSense <= hv.m_pvhvo->Size())
			hvoSense = (*hv.m_pvhvo)[iSense - 1];
		hv.m_pvhvo = NULL;
	}
	if (*pszNext)
		return TraceSenseNumber(hvoSense, pszNext + 1, depth + 1);
	else
		return hvoSense;
}

/*----------------------------------------------------------------------------------------------
	Fill a HashMap to go from a "Headword" to the corresponding LexEntry database id. The
	"Headword" contains the allomorph string decorated with affix markers (if any) and ending
	with a homograph number if nonzero.

	@param hmsuhvo Reference to HashMap
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::FillHashMapHeadwordToHvo(HashMapStrUni<int> & hmsuhvo, int ws)
{
	if (hmsuhvo.Size())
		return;				// already full!
	SqlStatement sstmt;
	RETCODE rc;
	int hobj = 0;
	int nHomograph;
	SDWORD cbhobj;
	SDWORD cbHomograph;
	SDWORD cbForm;
	SDWORD cbPostfix;
	SDWORD cbPrefix;
	SDWORD cbCitation;
	wchar rgchForm[4001];
	wchar rgchPostfix[4001];
	wchar rgchPrefix[4001];
	wchar rgchCitation[4001];

	StrAnsiBufBig stab;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		stab.Format("SELECT e.Id, e.HomographNumber, f.Txt, t.Postfix, t.Prefix, cf.Txt %n"
			"FROM LexEntry e %n"
			"LEFT OUTER JOIN LexEntry_CitationForm cf ON cf.Obj=e.Id AND cf.Ws=%<0>d %n"
			"LEFT OUTER JOIN LexEntry_LexemeForm a ON a.Src=e.Id %n"
			"LEFT OUTER JOIN MoForm m ON m.id=a.Dst %n"
			"LEFT OUTER JOIN MoForm_Form f ON f.Obj=a.Dst AND f.Ws=%<0>d %n"
			"LEFT OUTER JOIN MoMorphType t ON t.id=m.MorphType;", ws);
	}

	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(stab.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stab.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbhobj);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &nHomograph, isizeof(nHomograph), &cbHomograph);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_WCHAR, &rgchForm, isizeof(rgchForm), &cbForm);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 4, SQL_C_WCHAR, &rgchPostfix, isizeof(rgchPostfix), &cbPostfix);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 5, SQL_C_WCHAR, &rgchPrefix, isizeof(rgchPrefix), &cbPrefix);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 6, SQL_C_WCHAR, &rgchCitation, isizeof(rgchCitation),
		&cbCitation);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);

	StrUni stuCit;
	StrUni stuLex;
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbhobj == SQL_NULL_DATA || !cbhobj)
			continue;
		if (cbHomograph == SQL_NULL_DATA || !cbHomograph)
			continue;
		if (cbCitation != SQL_NULL_DATA && cbCitation != 0)
		{
			stuCit.Clear();
			if (cbPrefix != SQL_NULL_DATA && cbPrefix != 0)
				stuCit.Assign(rgchPrefix);
			stuCit.Append(rgchCitation);
			if (cbPostfix != SQL_NULL_DATA && cbPostfix != 0)
				stuCit.Append(rgchPostfix);
			if (nHomograph != 0)
				stuCit.FormatAppend(L"%d", nHomograph);
			hmsuhvo.Insert(stuCit, hobj, true);
		}
		if (cbForm != SQL_NULL_DATA && cbForm != 0)
		{
			stuLex.Clear();
			if (cbPrefix != SQL_NULL_DATA && cbPrefix != 0)
				stuLex.Assign(rgchPrefix);
			stuLex.Append(rgchForm);
			if (cbPostfix != SQL_NULL_DATA && cbPostfix != 0)
				stuLex.Append(rgchPostfix);
			if (nHomograph != 0)
				stuLex.FormatAppend(L"%d", nHomograph);
			int hvoAlready = 0;
			if (!hmsuhvo.Retrieve(stuLex, &hvoAlready))
				hmsuhvo.Insert(stuLex, hobj, true);
		}
	}
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Fill a HashMap to go from a LexEntry or LexSense database id to a vector of database ids
	of the LexSense objects owned by the LexEntry or LexSense.

	@param pszSQL The SQL command to load the necessary data
	@param hmhvovhvo Reference to HashMap
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::FillHashMapHvoToSenses(const char * pszSQL, HashMap<int, HvoVector> & hmhvovhvo)
{
	if (hmhvovhvo.Size())
		return;				// already full!
	SqlStatement sstmt;
	RETCODE rc;
	int hobj = 0;
	int hvoSense;
	SDWORD cbhobj;
	SDWORD cbSense;
	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pszSQL)), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pszSQL);
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbhobj);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &hvoSense, isizeof(hvoSense), &cbSense);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	HvoVector hv;
	hv.m_pvhvo = new Vector<int>();
	int hobjPrev = 0;
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbhobj == SQL_NULL_DATA || !cbhobj)
			continue;
		if (cbSense == SQL_NULL_DATA || !cbSense)
			continue;
		if (hobj != hobjPrev && hv.m_pvhvo->Size() != 0)
		{
			hmhvovhvo.Insert(hobjPrev, hv);
			hv.m_pvhvo = new Vector<int>();
		}
		hv.m_pvhvo->Push(hvoSense);
		hobjPrev = hobj;
	}
	if (hv.m_pvhvo->Size() != 0)
	{
		hmhvovhvo.Insert(hobjPrev, hv);
	}
	else
	{
		delete hv.m_pvhvo;
	}
	hv.m_pvhvo = NULL;

	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Fill a HashMap to map from one integer (presumably a database id) to another.

	@param pszSQL The SQL command to load the necessary data
	@param hmhvon Reference to HashMap
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::FillHashMapHvoToInt(const char * pszSQL, HashMap<int, int> & hmhvon)
{
	SqlStatement sstmt;
	RETCODE rc;
	int hobj = 0;
	int nType;
	SDWORD cbhobj;
	SDWORD cbType;
	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pszSQL)), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pszSQL);
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbhobj);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &nType, isizeof(nType), &cbType);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbhobj == SQL_NULL_DATA || !cbhobj)
			continue;
		if (cbType == SQL_NULL_DATA || !cbType)
			continue;
		hmhvon.Insert(hobj, nType);
	}
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Store all the accumulated object attribute values.

	@param padvi Optional pointer to a progress report object
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreData(IAdvInd * padvi, int nPercent)
{
	//fprintf(stdout,"begin StoreData\n");
	TimeStampObjects();
	if (padvi)
		padvi->Step(1);		// now at 46%.

	unsigned long cStepNew;

	int ifld;
	if (padvi)
	{
		m_cBlkTotal = 0;
		for (ifld = 0; ifld < m_vstda.Size(); ++ifld)
			m_cBlkTotal += m_vstda[ifld].m_vhobj.Size();
	}

	for (ifld = 0; ifld < m_vstda.Size(); ++ifld)
	{
		//fprintf(stdout,"begin loop iter=%d\n",ifld);
		if (m_vstda[ifld].m_vhobj.Size())
		{
			//fprintf(stdout,"before switch\n");
			switch (m_pfwxd->FieldInfo(ifld).cpt)
			{
			case kcptReferenceAtom:
				StoreReferenceAtoms(ifld);
				//fprintf(stdout,"case 1 iter=%d\n",ifld);
				break;
			case kcptReferenceCollection:
				StoreReferenceCollections(ifld);
				//fprintf(stdout,"case 2 iter=%d\n",ifld);
				break;
			case kcptReferenceSequence:
				StoreReferenceSequences(ifld);
				//fprintf(stdout,"case 3 iter=%d\n",ifld);
				break;
			case kcptUnicode:
			case kcptBigUnicode:
				StoreUnicodeData(ifld);
				//fprintf(stdout,"case 4 iter=%d\n",ifld);
				break;
			case kcptString:
			case kcptBigString:
				StoreStringData(ifld);
				//fprintf(stdout,"case 5 iter=%d\n",ifld);
				break;
			default:
				// "ERROR! BUG! Invalid field data type storing data?? (%d)"
				ThrowWithLogMessage(kstidXmlErrorMsg030, (void *)m_pfwxd->FieldInfo(ifld).cpt);
				break;
			}
		}
		if (padvi && m_cBlkTotal)
		{
			// Storing data is 15% of the progress to report for LoadXml.
			m_cBlk += m_vstda[ifld].m_vhobj.Size();
			cStepNew = m_cBlk * nPercent / m_cBlkTotal;
			if (cStepNew > m_cStep)
			{
				padvi->Step(cStepNew - m_cStep);
				m_cStep = cStepNew;
			}
		}
	}
	m_vstda.Clear();
	//fprintf(stdout,"after for loop\n");
	StoreMultiUnicode();
	StoreMultiBigUnicode();
	StoreMultiString();
	StoreMultiBigString();

	XML_ParserFree(m_parser);
	m_parser = 0;
	if (m_vlri.Size())
		StoreLexicalReferences();
	if (m_vmesi.Size())
	{
		StoreEntryOrSenseLinks();
		RemoveRedundantEntries();
	}
	DeleteObjects(m_vhobjDel);
	CreateMissingSensesForComplexForms();
}

/*----------------------------------------------------------------------------------------------
	Delete all the database objects in the given vector.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::DeleteObjects(Vector<int> & vhobj, IAdvInd * padvi, int nPercent)
{
	if (vhobj.Size() == 0)
		return;
	StrUni stuHvoList;
	int cdel = 0;
	for (int i = 0; i < vhobj.Size(); ++i)
	{
		int hvoDel = vhobj[i];
		if (stuHvoList.Length() == 0)
			stuHvoList.Format(L"%u", hvoDel);
		else
			stuHvoList.FormatAppend(L",%u", hvoDel);
		++cdel;
		// Try not to do too many at once: it causes problems.  (Even though the input parameter
		// is defined as a "text", the stored procedure still complains if its length gets too
		// great for nvarchar.)
		if (stuHvoList.Length() > 3980)
		{
			DeleteObjects(stuHvoList);
			stuHvoList.Clear();
			if (padvi && nPercent)
				padvi->Step((cdel * nPercent) / vhobj.Size());
			cdel = 0;
		}
	}
	if (stuHvoList.Length() > 0)
	{
		DeleteObjects(stuHvoList);
		stuHvoList.Clear();
			if (padvi && nPercent)
				padvi->Step((cdel * nPercent) / vhobj.Size());
	}
}

/*----------------------------------------------------------------------------------------------
	Delete all the database objects in the given comma separated list.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::DeleteObjects(const StrUni & stuHvoList)
{
	SQLINTEGER cchParam = stuHvoList.Length();
	SQLINTEGER cbParam = cchParam * sizeof(wchar);
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc;
	StrAnsi staCmd;
	if(CURRENTDB == FB) {
		staCmd = "EXECUTE PROCEDURE DeleteObjects ?;";
	}
	if(CURRENTDB == MSSQL) {
		staCmd = "EXEC DeleteObjects ?;";
	}
	rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_WCHAR, SQL_WLONGVARCHAR,
		cchParam+1, 0, const_cast<wchar *>(stuHvoList.Chars()), cbParam + sizeof(wchar),
		&cbParam);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, staCmd.Chars());
	// Skip past all the row count info generated by the stored procedure.  (Without this, the
	// stored procedure may claim to work, but may not actually do anything!)
	do
	{
		rc = SQLMoreResults(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	} while (rc != SQL_NO_DATA);
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	The import process may leave duplicate minor entries and subentries. Go through all the
	EntryRefs source objects and remove any duplicates found.  Try not to lose any
	data in the process, however.  (LT-4094)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::RemoveRedundantEntries()
{
	StrAnsi sta;
	if(CURRENTDB == FB) {
		sta.Format(
			"select distinct le.id, le2.id from MoForm_Form mff %n"
			"join MoForm_Form mff2 on mff2.txt = mff.txt and mff2.ws = mff.ws and mff2.obj <> mff.obj %n"
			"join MoForm mf on mf.id = mff.obj %n"
			"join MoForm mf2 on mf2.id = mff2.obj and mf2.MorphType = mf.MorphType %n"
			"left outer join LexEntry_LexemeForm lf on lf.dst = mf.id %n"
			"left outer join LexEntry_LexemeForm lf2 on lf2.dst = mf2.id %n"
			"join LexEntry le on le.id = lf.src %n"
			"join LexEntry le2 on le2.id = lf2.src and le2.HomographNumber = le.HomographNumber %n"
			"left outer join LexEntry_EntryRefs er on er.src = le.id %n"
			"left outer join LexEntryRef_ComponentLexemes cl on cl.src = er.dst %n"
			"left outer join LexEntryRef_VariantEntryTypes vt on vt.src = er.dst %n"
			"left outer join LexEntryRef_ComplexEntryTypes ct on ct.src = er.dst %n"
			"left outer join LexEntry_EntryRefs er2 on er2.src = le2.id %n"
			"left outer join LexEntryRef_ComponentLexemes cl2 on cl2.src = er2.dst and cl2.dst = cl.dst %n"
			"left outer join LexEntryRef_VariantEntryTypes vt2 on vt2.src = er2.dst %n"
			"left outer join LexEntryRef_ComplexEntryTypes ct2 on ct2.src = er2.dst %n"
			"where ct.dst is null and vt.dst is null and (ct2.dst is not null or vt2.dst is not null) %n"
			);
	}
	if(CURRENTDB == MSSQL) {
		sta.Format(
			"select distinct le.id, le2.id from MoForm_Form mff %n"
			"join MoForm_Form mff2 on mff2.txt = mff.txt and mff2.ws = mff.ws and mff2.obj <> mff.obj %n"
			"join MoForm mf on mf.id = mff.obj %n"
			"join MoForm mf2 on mf2.id = mff2.obj and mf2.MorphType = mf.MorphType %n"
			"left outer join LexEntry_LexemeForm lf on lf.dst = mf.id %n"
			"left outer join LexEntry_LexemeForm lf2 on lf2.dst = mf2.id %n"
			"join LexEntry le on le.id = lf.src %n"
			"join LexEntry le2 on le2.id = lf2.src and le2.HomographNumber = le.HomographNumber %n"
			"left outer join LexEntry_EntryRefs er on er.src = le.id %n"
			"left outer join LexEntryRef_ComponentLexemes cl on cl.src = er.dst %n"
			"left outer join LexEntryRef_VariantEntryTypes vt on vt.src = er.dst %n"
			"left outer join LexEntryRef_ComplexEntryTypes ct on ct.src = er.dst %n"
			"left outer join LexEntry_EntryRefs er2 on er2.src = le2.id %n"
			"left outer join LexEntryRef_ComponentLexemes cl2 on cl2.src = er2.dst and cl2.dst = cl.dst %n"
			"left outer join LexEntryRef_VariantEntryTypes vt2 on vt2.src = er2.dst %n"
			"left outer join LexEntryRef_ComplexEntryTypes ct2 on ct2.src = er2.dst %n"
			"where ct.dst is null and vt.dst is null and (ct2.dst is not null or vt2.dst is not null) %n"
			);
	}

	SqlStatement sstmt;
	RETCODE rc;
	int hobjOld = 0;
	SDWORD cbhobjOld;
	int hobjNew = 0;
	SDWORD cbhobjNew;

	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(sta.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, sta.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobjOld, isizeof(hobjOld), &cbhobjOld);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &hobjNew, isizeof(hobjNew), &cbhobjNew);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	Vector<int> vhobjOld;
	Vector<int> vhobjNew;

	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbhobjOld == SQL_NULL_DATA || !cbhobjOld)
			continue;
		if (cbhobjNew == SQL_NULL_DATA || !cbhobjNew)
			continue;
		vhobjOld.Push(hobjOld);
		vhobjNew.Push(hobjNew);
	}
	sstmt.Clear();
	// Copy any data found in the objects to delete that is missing from the related objects.
	for (int i = 0; i < vhobjOld.Size(); ++i)
		MergeLexEntries(vhobjOld[i], vhobjNew[i]);
	DeleteObjects(vhobjOld);
}


/*----------------------------------------------------------------------------------------------
	Merge data from LexEntry hobjOld which is missing from LexEntry hobjNew into hobjNew.  This
	may or may not result in the data being removed from LexEntry hobjOld.  If the model of a
	LexEntry changes, the embedded SQL below may have to change also!
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::MergeLexEntries(int hobjOld, int hobjNew)
{
	StrAnsi sta;
	//TODO (steve miller): correnct assignment to variable for Firebird
	if(CURRENTDB == FB){
		sta.Format("DECLARE VARIABLE hvoOld INT; DECLARE VARIABLE hvoNew INT;%n"
			"DECLARE VARIABLE hvoValOld INT; DECLARE VARIABLE hvoValNew INT; DECLARE VARIABLE maxord INT;%n"
			"DECLARE VARIABLE nvcOld VARCHAR(4000) CHARACTER SET UTF8; DECLARE VARIABLE vbOld BLOB;%n"
			"hvoOld=%<0>d;%n"
			"hvoNew=%<1>d;%n"
			"SELECT ImportResidue, ImportResidue_Fmt FROM LexEntry WHERE Id=hvoOld INTO :nvcOld, :vbOld;%n"
			"IF (nvcOld IS NOT NULL) THEN%n"
			"	UPDATE LexEntry%n"
			"	SET ImportResidue=nvcOld, ImportResidue_Fmt=vbOld%n"
			"	WHERE Id=hvoNew AND ImportResidue IS NULL;%n"
			"UPDATE LexEntry_Bibliography%n"
			"SET Obj=hvoNew%n"
			"WHERE Obj=hvoOld AND Ws NOT IN (SELECT Ws FROM LexEntry_Bibliography WHERE Obj=hvoNew);%n"
			"UPDATE LexEntry_CitationForm%n"
			"SET Obj=hvoNew%n"
			"WHERE Obj=hvoOld AND Ws NOT IN (SELECT Ws FROM LexEntry_CitationForm WHERE Obj=hvoNew);%n"
			"UPDATE LexEntry_Comment%n"
			"SET Obj=hvoNew%n"
			"WHERE Obj=hvoOld AND Ws NOT IN (SELECT Ws FROM LexEntry_Comment WHERE Obj=hvoNew);%n"
			"UPDATE LexEntry_LiteralMeaning%n"
			"SET Obj=hvoNew%n"
			"WHERE Obj=hvoOld AND Ws NOT IN (SELECT Ws FROM LexEntry_LiteralMeaning WHERE Obj=hvoNew);%n"
			"UPDATE LexEntry_Restrictions%n"
			"SET Obj=hvoNew%n"
			"WHERE Obj=hvoOld AND Ws NOT IN (SELECT Ws FROM LexEntry_Restrictions WHERE Obj=hvoNew);%n"
			"UPDATE LexEntry_SummaryDefinition%n"
			"SET Obj=hvoNew%n"
			"WHERE Obj=hvoOld AND Ws NOT IN (SELECT Ws FROM LexEntry_SummaryDefinition WHERE Obj=hvoNew);%n"
			"SELECT Id FROM LexEtymology_ WHERE Owner$=hvoOld INTO :hvoValOld;%n"
			"SELECT Id FROM LexEtymology_ WHERE Owner$=hvoNew INTO :hvoValNew;%n"
			"IF (hvoValNew IS NULL AND hvoValOld IS NOT NULL) THEN%n"
			"	UPDATE LexEtymology_%n"
			"	SET Owner$=hvoNew%n"
			"	WHERE Owner$=hvoOld;%n"
			"SELECT MAX(Ord) FROM LexEntry_AlternateForms WHERE Src=@hvoNew INTO :maxord;%n"
			"UPDATE LexEntry_AlternateForms%n"
			"SET Src = hvoNew, Ord=ISNULL(maxord,0) + d.Ord%n"
			"FROM LexEntry_AlternateForms d%n"
			"WHERE d.Src=hvoOld;%n"
			"UPDATE LexEntry_MorphoSyntaxAnalyses%n"
			"SET Src = hvoNew%n"
			"FROM LexEntry_MorphoSyntaxAnalyses d%n"
			"WHERE d.Src=hvoOld;%n"
			"SELECT maxord=MAX(Ord) FROM LexEntry_Pronunciations WHERE Src=hvoNew;%n"
			"UPDATE LexEntry_Pronunciations%n"
			"SET Src = hvoNew, Ord=ISNULL(@maxord,0) + d.Ord%n"
			"FROM LexEntry_Pronunciations d%n"
			"WHERE d.Src=hvoOld;%n"
			"SELECT maxord=MAX(Ord) FROM LexEntry_Senses WHERE Src=@hvoNew;%n"
			"UPDATE LexEntry_Senses%n"
			"SET Src = hvoNew, Ord=ISNULL(maxord,0) + d.Ord%n"
			"FROM LexEntry_Senses d%n"
			"WHERE d.Src=hvoOld;",
			hobjOld, hobjNew);
	}
	if(CURRENTDB == MSSQL){
		sta.Format("DECLARE @hvoOld INT, @hvoNew INT%n"
			"SET @hvoOld=%<0>d%n"
			"SET @hvoNew=%<1>d%n"
			"DECLARE @hvoValOld INT, @hvoValNew INT, @maxord INT%n"
			"DECLARE @nvcOld nvarchar(4000), @vbOld varbinary(8000)%n"
			"SELECT @nvcOld=ImportResidue, @vbOld=ImportResidue_Fmt FROM LexEntry WHERE Id=@hvoOld%n"
			"IF @nvcOld IS NOT NULL%n"
			"	UPDATE LexEntry%n"
			"	SET ImportResidue=@nvcOld, ImportResidue_Fmt=@vbOld%n"
			"	WHERE Id=@hvoNew AND ImportResidue IS NULL%n"
			"UPDATE LexEntry_Bibliography%n"
			"SET Obj=@hvoNew%n"
			"WHERE Obj=@hvoOld AND Ws NOT IN (SELECT Ws FROM LexEntry_Bibliography WHERE Obj=@hvoNew)%n"
			"UPDATE LexEntry_CitationForm%n"
			"SET Obj=@hvoNew%n"
			"WHERE Obj=@hvoOld AND Ws NOT IN (SELECT Ws FROM LexEntry_CitationForm WHERE Obj=@hvoNew)%n"
			"UPDATE LexEntry_Comment%n"
			"SET Obj=@hvoNew%n"
			"WHERE Obj=@hvoOld AND Ws NOT IN (SELECT Ws FROM LexEntry_Comment WHERE Obj=@hvoNew)%n"
			"UPDATE LexEntry_LiteralMeaning%n"
			"SET Obj=@hvoNew%n"
			"WHERE Obj=@hvoOld AND Ws NOT IN (SELECT Ws FROM LexEntry_LiteralMeaning WHERE Obj=@hvoNew)%n"
			"UPDATE LexEntry_Restrictions%n"
			"SET Obj=@hvoNew%n"
			"WHERE Obj=@hvoOld AND Ws NOT IN (SELECT Ws FROM LexEntry_Restrictions WHERE Obj=@hvoNew)%n"
			"UPDATE LexEntry_SummaryDefinition%n"
			"SET Obj=@hvoNew%n"
			"WHERE Obj=@hvoOld AND Ws NOT IN (SELECT Ws FROM LexEntry_SummaryDefinition WHERE Obj=@hvoNew)%n"
			"SELECT @hvoValOld=Id FROM LexEtymology_ WHERE Owner$=@hvoOld%n"
			"SELECT @hvoValNew=Id FROM LexEtymology_ WHERE Owner$=@hvoNew%n"
			"IF @hvoValNew IS NULL AND @hvoValOld IS NOT NULL%n"
			"	UPDATE LexEtymology_%n"
			"	SET Owner$=@hvoNew%n"
			"	WHERE Owner$=@hvoOld%n"
			"SELECT @maxord=MAX(Ord) FROM LexEntry_AlternateForms WHERE Src=@hvoNew%n"
			"UPDATE LexEntry_AlternateForms%n"
			"SET Src = @hvoNew, Ord=ISNULL(@maxord,0) + d.Ord%n"
			"FROM LexEntry_AlternateForms d%n"
			"WHERE d.Src=@hvoOld%n"
			"UPDATE LexEntry_MorphoSyntaxAnalyses%n"
			"SET Src = @hvoNew%n"
			"FROM LexEntry_MorphoSyntaxAnalyses d%n"
			"WHERE d.Src=@hvoOld%n"
			"SELECT @maxord=MAX(Ord) FROM LexEntry_Pronunciations WHERE Src=@hvoNew%n"
			"UPDATE LexEntry_Pronunciations%n"
			"SET Src = @hvoNew, Ord=ISNULL(@maxord,0) + d.Ord%n"
			"FROM LexEntry_Pronunciations d%n"
			"WHERE d.Src=@hvoOld%n"
			"SELECT @maxord=MAX(Ord) FROM LexEntry_Senses WHERE Src=@hvoNew%n"
			"UPDATE LexEntry_Senses%n"
			"SET Src = @hvoNew, Ord=ISNULL(@maxord,0) + d.Ord%n"
			"FROM LexEntry_Senses d%n"
			"WHERE d.Src=@hvoOld;",
			hobjOld, hobjNew);
	}
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(sta.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, sta.Chars());
	sstmt.Clear();
	CopyCustomFieldData(hobjOld, hobjNew);
}

/*----------------------------------------------------------------------------------------------
	Copy custom data from LexEntry hobjOld which is missing from LexEntry hobjNew into hobjNew.
	This may or may not result in the data being removed from LexEntry hobjOld.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CopyCustomFieldData(int hobjOld, int hobjNew)
{
	// Copy data in custom fields.
	const int kcchMax = 4000;
	SQLWCHAR rgchField[kcchMax];
	SDWORD cbField;
	int nType;
	SDWORD cbT;
	StrAnsi sta;
	sta.Format("SELECT Name, Type FROM Field$ WHERE Custom<>0 AND Class=%<0>d", kclidLexEntry);
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(sta.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, sta.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_WCHAR, &rgchField, isizeof(rgchField),
		&cbField);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &nType, isizeof(nType), &cbT);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	sta.Clear();
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbT == SQL_NULL_DATA || cbField == SQL_NULL_DATA)
			continue;
		int cchField = cbField / isizeof(SQLWCHAR);
		if (!rgchField[cchField - 1])
			--cchField;			// Don't include trailing NUL.
		StrAnsi staField(rgchField, cchField);
		switch (nType)
		{
		case kcptString:
		case kcptBigString:
			sta.FormatAppend(" UPDATE LexEntry"
				" SET %<0>s=(SELECT %<0>s FROM LexEntry WHERE Id=%<1>d),"
				" %<0>s_fmt=(SELECT %<0>s_fmt FROM LexEntry WHERE Id=%<1>d)"
				" WHERE Id=%<2>d AND %<0>s IS NULL;%n",
				staField.Chars(), hobjOld, hobjNew);
			break;
		case kcptMultiUnicode:
		case kcptMultiBigUnicode:
			sta.FormatAppend(" INSERT INTO LexEntry_%<0>s (Obj, Ws, Txt)"
				" SELECT %<2>d, Ws, Txt FROM LexEntry_%<0>s WHERE Obj=%<1>d;%n",
				staField.Chars(), hobjOld, hobjNew);
			break;
		default:
			break;
		}
	}
	sstmt.Clear();
	if (sta.Length() > 0)
	{
		sstmt.Init(m_sdb);
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(sta.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, sta.Chars());
		sstmt.Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	Handle XML start tags inside <Prop> elements during the second pass.

	This static method is passed to the expat XML parser as a callback function.  It is used
	when the start tag for <Prop> or <WsStyles9999> is detected.  See the comments in xmlparse.h
	for the XML_StartElementHandler typedef for the documentation such as it is.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandlePropStartTag2(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);

	pxid->ProcessPropStartTag2(pszName, prgpszAtts);
}


/*----------------------------------------------------------------------------------------------
	Handle XML start tags inside <Prop> elements during the second pass.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessPropStartTag2(const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	StrAnsi sta;
	try
	{
		if (m_fInWsStyles)
		{
			if (!strcmp(pszName, "WsProp"))
			{
				m_vtxipWs.Clear();		// Scalar-valued properties for this property.
				m_vtxspWs.Clear();		// Text-valued properties for this property
				// Handle the attributes for this Prop element.
				ProcessPropAttributes(pszName, prgpszAtts, m_vtxipWs, m_vtxspWs);
			}
			else
			{
				// This has already been handled: SHOULD NEVER REACH HERE!
				// "<%s> not recognized nested within <WsStyles9999>!"
				ThrowWithLogMessage(kstidXmlErrorMsg137, pszName);
			}
		}
		else if (m_fInRuleProp)
		{
			if (!strcmp(pszName, "WsStyles9999"))
			{
				m_fInWsStyles = true;
				m_vesi.Clear();
			}
			else if (!strcmp(pszName, "BulNumFontInfo"))
			{
				m_vtxipBNFI.Clear();
				m_vtxspBNFI.Clear();
				ProcessPropAttributes(pszName, prgpszAtts, m_vtxipBNFI, m_vtxspBNFI);
			}
			else if (!strcmp(pszName, "Prop"))
			{
				if (m_vtxip.Size() || m_vtxsp.Size())
				{
					// "<%s> not recognized nested within <Prop>!"
					ThrowWithLogMessage(kstidXmlErrorMsg138, pszName);
				}
				// Handle the attributes for this Prop element.
				ProcessPropAttributes(pszName, prgpszAtts, m_vtxip, m_vtxsp);
			}
			else
			{
				// This has already been handled: SHOULD NEVER REACH HERE!
				// "<%s> not recognized nested within <Prop>!"
				ThrowWithLogMessage(kstidXmlErrorMsg138, pszName);
			}
		}
		else
		{
			Assert(false);		// THIS SHOULD NEVER HAPPEN!
		}
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		StrAnsi staFmt(kstidXmlDebugMsg003);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		StrAnsi staFmt(kstidXmlDebugMsg005);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Store the properties for one writing system of a WsStyle property.

	This method must be kept in sync with FwStyledText::EncodeFontPropsString().
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreWsProps()
{
	WsStyleInfo esi;
	bool fOk = false;
	for (int itxip = 0; itxip < m_vtxipWs.Size(); ++itxip)
	{
		switch (m_vtxipWs[itxip].m_scp)
		{
		case kscpWs:
			esi.m_ws = m_vtxipWs[itxip].m_nVal;
			fOk = true;
			break;
		case kscpFontSize:
			Assert(m_vtxipWs[itxip].m_nVar == ktpvMilliPoint);
			esi.m_mpSize = m_vtxipWs[itxip].m_nVal;
			break;
		case kscpOffset:
			Assert(m_vtxipWs[itxip].m_nVar == ktpvMilliPoint);
			esi.m_mpOffset = m_vtxipWs[itxip].m_nVal;
			break;
		case kscpBold:
			esi.m_fBold = m_vtxipWs[itxip].m_nVal;
			break;
		case kscpItalic:
			esi.m_fItalic = m_vtxipWs[itxip].m_nVal;
			break;
		case kscpSuperscript:
			esi.m_ssv = m_vtxipWs[itxip].m_nVal;
			break;
		case kscpUnderline:
			esi.m_unt = m_vtxipWs[itxip].m_nVal;
			break;
		case kscpForeColor:
			esi.m_clrFore = m_vtxipWs[itxip].m_nVal;
			break;
		case kscpBackColor:
			esi.m_clrBack = m_vtxipWs[itxip].m_nVal;
			break;
		case kscpUnderColor:
			esi.m_clrUnder = m_vtxipWs[itxip].m_nVal;
			break;
		default:
			Warn("Unexpected WsStyle property found");
			break;
		}
	}
	for (int itxsp = 0; itxsp < m_vtxspWs.Size(); ++itxsp)
	{
		switch (m_vtxspWs[itxsp].m_tpt)
		{
		case kstpFontFamily:
			esi.m_stuFontFamily = m_vtxspWs[itxsp].m_stuVal;
			break;
		case kstpFontVariations:
			esi.m_stuFontVar = m_vtxspWs[itxsp].m_stuVal;
			break;
		default:
			Warn("Unexpected WsStyle property found");
			break;
		}
	}
	if (fOk)
	{
		// Store style values sorted by writing system to ensure proper behavior.
		int iv;
		int ivLim;
		for (iv = 0, ivLim = m_vesi.Size(); iv < ivLim; )
		{
			int ivMid = (iv + ivLim) / 2;
			if ((unsigned)m_vesi[ivMid].m_ws < (unsigned)esi.m_ws)
				iv = ivMid + 1;
			else
				ivLim = ivMid;
		}
		m_vesi.Insert(iv, esi);
	}
	m_vtxipWs.Clear();
	m_vtxspWs.Clear();
}

/*----------------------------------------------------------------------------------------------
	Convert the vectors of integer valued and string valued properties into their combined
	binary representation.

	@param vbProps
	@param vtxip
	@param vtxsp
	@param pxid Pointer to the XML import data.
----------------------------------------------------------------------------------------------*/
static void SerializeStyleProps(Vector<byte> & vbProps, Vector<TextProps::TextIntProp> & vtxip,
	Vector<TextProps::TextStrProp> & vtxsp, FwXmlImportData * pxid)
{
	Assert(vtxip.Size() < 256);
	Assert(vtxsp.Size() < 256);

	int iv;
	byte rgbTmp[8000];
	DataWriterRgb dwr(rgbTmp, isizeof(rgbTmp));

	byte ctip = static_cast<byte>(vtxip.Size());
	dwr.WriteBuf(&ctip, 1);
	byte ctsp = static_cast<byte>(vtxsp.Size());
	dwr.WriteBuf(&ctsp, 1);

	TextProps::TextIntProp txip;
	for (iv = 0; iv < vtxip.Size(); ++iv)
	{
		txip.m_scp = vtxip[iv].m_scp;
		txip.m_nVal = vtxip[iv].m_nVal;
		txip.m_nVar = vtxip[iv].m_nVar;
		TextProps::WriteTextIntProp(&dwr, &txip);
	}
	TextProps::TextStrProp txsp;
	for (iv = 0; iv < vtxsp.Size(); ++iv)
	{
		txsp.m_tpt = vtxsp[iv].m_tpt;
		txsp.m_stuVal = vtxsp[iv].m_stuVal;
		TextProps::WriteTextStrProp(&dwr, &txsp);
	}
	vbProps.Replace(vbProps.Size(), vbProps.Size(), rgbTmp, dwr.IbCur());
	vtxip.Clear();
	vtxsp.Clear();
}

/*----------------------------------------------------------------------------------------------
	Handle XML end tags inside <Prop> elements during the second pass.

	This static method is passed to the expat XML parser as a callback function.  It is used
	when the end tag for <Prop> or <WsStyles9999> is detected.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandlePropEndTag2(void * pvUser, const XML_Char * pszName)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);

	pxid->ProcessPropEndTag2(pszName);
}


/*----------------------------------------------------------------------------------------------
	Handle XML end tags inside <Prop> elements during the second pass.

	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessPropEndTag2(const XML_Char * pszName)
{
	StrAnsi sta;
	StrAnsi staFmt;
	try
	{
		if (m_fInWsStyles)
		{
			ProcessWsStylesProp(pszName);
		}
		else if (m_fInRuleProp)
		{
			ProcessRuleProp(pszName);
		}
		else
		{
			Assert(false);		// THIS SHOULD NEVER HAPPEN!
		}
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		staFmt.Load(kstidXmlDebugMsg003);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		staFmt.Load(kstidXmlDebugMsg005);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Process the </WsStyles9999> and </WsProp> end tags.

	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessWsStylesProp(const XML_Char * pszName)
{
	if (!strcmp(pszName, "WsStyles9999"))
	{
		// kstpWsStyle = ktptWsStyle,
		TextProps::TextStrProp txsp;
		txsp.m_tpt = kstpWsStyle;
		txsp.m_stuVal = FwStyledText::EncodeFontPropsString(m_vesi, false);
		bool fFound = false;
		for (int i = 0; i < m_vtxsp.Size(); ++i)
		{
			if (m_vtxsp[i].m_tpt == txsp.m_tpt)
			{
				m_vtxsp[i].m_stuVal = txsp.m_stuVal;
				fFound = true;
				break;
			}
		}
		if (!fFound && txsp.m_stuVal.Length())
			m_vtxsp.Push(txsp);
		m_fInWsStyles = false;
	}
	else if (!strcmp(pszName, "WsProp"))
	{
		StoreWsProps();
	}
	else
	{
		// This error has already been handled: SHOULD NEVER REACH HERE!
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
}


/*----------------------------------------------------------------------------------------------
	Process the </Prop> and </BulNumFontInfo> end tags.

	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessRuleProp(const XML_Char * pszName)
{
	if (!strcmp(pszName, "Prop"))
	{
		// Convert prop vectors to binary.
		m_vbProp.Clear();
		SerializeStyleProps(m_vbProp, m_vtxip, m_vtxsp, this);
		// Return to the normal processing for pass two.
		XML_SetElementHandler(m_parser, m_startOuterHandler, m_endOuterHandler);
		(*m_endOuterHandler)(this, pszName);
	}
	else if (!strcmp(pszName, "BulNumFontInfo"))
	{
		int ip;
		int ich;
		Vector<wchar> vchw;				// Binary value for BulNumFOntInfo property.
		vchw.Resize(3 * m_vtxipBNFI.Size());
		int tpt;
		int nVal;
		for (ich = 0, ip = 0; ip < m_vtxipBNFI.Size(); ++ip)
		{
			tpt = m_vtxipBNFI[ip].m_scp >> 2;		// Fix for SCPn(tpt).
			vchw[ich++] = (wchar)(tpt);
			nVal = m_vtxipBNFI[ip].m_nVal;
			vchw[ich++] = (wchar)(nVal & 0xFFFF);
			vchw[ich++] = (wchar)((nVal >> 16) & 0xFFFF);
		}
		for (ip = 0; ip < m_vtxspBNFI.Size(); ++ip)
		{
			ich = vchw.Size();
			vchw.Resize(ich + 1 + m_vtxspBNFI[ip].m_stuVal.Length());
			vchw[ich++] = (wchar)(m_vtxspBNFI[ip].m_tpt);
			memcpy(&vchw[ich], m_vtxspBNFI[ip].m_stuVal.Chars(),
				m_vtxspBNFI[ip].m_stuVal.Length() * sizeof(wchar));
		}
		if (vchw.Size())
		{
			SetStringProp(m_vtxsp, kstpBulNumFontInfo,
				reinterpret_cast<const char *>(vchw.Begin()),
				vchw.Size() * isizeof(wchar));
		}
	}
	else
	{
		// This error has already been handled: SHOULD NEVER REACH HERE!
		ThrowHr(WarnHr(E_UNEXPECTED));
	}
}


/*----------------------------------------------------------------------------------------------
	Handle XML start elements during the first pass.  All objects are created during this pass.

	This static method is passed to the expat XML parser as a callback function.  See the
	comments in xmlparse.h for the XML_StartElementHandler typedef for the documentation
	such as it is.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ImportStartTag1(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);

	pxid->ProcessImportStartTag1(pszName, prgpszAtts);
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements during the first pass.  All objects are created during this pass.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessImportStartTag1(const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	StrAnsi sta;
	StrAnsi staFmt;
	if (m_celemStart == 0)
	{
		if (m_staBeginTag != pszName)
		{
			if (m_staOwnerBeginTag == pszName)
			{
				++m_celemStart;
				return;
			}
			m_fError = true;
			m_hr = E_INVALIDARG;
			// "Invalid start tag <%<0>s> for XML file: expected <%<1>s>.\n"
			staFmt.Load(kstidXmlErrorMsg311);
			sta.Format(staFmt.Chars(), pszName, m_staBeginTag.Chars());
			LogMessage(sta.Chars());
			ThrowHr(WarnHr(m_hr));
		}
	}
	++m_celemStart;
	StrAnsiBuf stabCustom;
	const char * pszProp = NULL;
	try
	{
		ElemTypeInfo eti = GetElementType(pszName);
		if (!m_vetiOpen.Size() && m_staBeginTag == pszName)
		{
			if (eti.m_elty == keltyPropName)
			{
				// Store information needed for sequence properties.
				PushSeqPropInfo(eti);
				// Flag as outermost element, but keep the field index for later use.
				eti.m_elty = keltyDatabase;
				m_vetiOpen.Push(eti);
				return;
			}
			else if (eti.m_elty == keltyObject && m_flid == 0)
			{
				// Flag as outermost element, but keep the class index for later use.
				ElemTypeInfo eti2;
				eti2.m_elty = keltyDatabase;
				eti2.m_icls = eti.m_icls;
				m_vetiOpen.Push(eti2);
				// Store the object information.
				m_vetiOpen.Push(eti);
				m_vhobjOpen.Push(m_hvoOwner);
				return;
			}
			// We're sure to blow up somewhere below if we haven't returned by now...
		}
		switch (eti.m_elty)
		{
		case keltyObject:
			StartObject1(eti, pszName, prgpszAtts);
			break;
		case keltyCustomProp:
			pszProp = FwXml::GetAttributeValue(prgpszAtts, "name");
			if (!pszProp)
			{
				// "Missing %<0>s attribute for %<1>s element!"
				ThrowWithLogMessage(kstidXmlErrorMsg079, "name", pszName);
			}
			else
			{
				stabCustom.Format("%s name=\"%s\"", pszName, pszProp);
				pszName = stabCustom.Chars();	// For error messages in StartPropName1().
			}
			StartPropName1(eti, pszName, pszProp);
			break;
		case keltyPropName:
			StartPropName1(eti, pszName);
			break;
		case keltyVirtualProp:
			StartPropName1(eti, pszName);
			break;
		case keltyBasicProp:
			StartBasicProp1(eti, pszName);
			break;
		default:	// includes keltyDatabase, keltyAddProps, and keltyDefineProp
			// "Unknown XML start tag: \"%s\""
			ThrowWithLogMessage(kstidXmlErrorMsg128, pszName);
			break;
		}
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		staFmt.Load(kstidXmlDebugMsg003);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		staFmt.Load(kstidXmlDebugMsg005);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}


/*----------------------------------------------------------------------------------------------
	Handle XML start elements during the second pass.

	This static method is passed to the expat XML parser as a callback function.  See the
	comments in xmlparse.h for the XML_StartElementHandler typedef for the documentation
	such as it is.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ImportStartTag2(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);

	pxid->ProcessImportStartTag2(pszName, prgpszAtts);
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements during the second pass.  All objects must have been created during
	the first pass.  Also, all structural errors must have been detected so that we can assert
	that everything is okay during this pass.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessImportStartTag2(const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	if (m_celemStart == 0 && m_iclsOwner != -1 && m_staOwnerBeginTag == pszName)
	{
		// If importing an object, ignore an outermost object element that matches the
		// owner's class.
		++m_celemStart;
		return;
	}
	++m_celemStart;
	StrAnsiBuf stabCmd;
	StrAnsi sta;
	StrAnsi staFmt;
	try
	{
		ElemTypeInfo eti = GetElementType(pszName);
		if (!m_vetiOpen.Size() && m_staBeginTag == pszName)
		{
			if (eti.m_elty == keltyPropName)
			{
				// Flag as outermost element, but keep the field index for later use.
				eti.m_elty = keltyDatabase;
				m_vetiOpen.Push(eti);
				return;
			}
			else if (eti.m_elty == keltyObject && m_flid == 0)
			{
				// Flag as outermost element, but keep the class index for later use.
				ElemTypeInfo eti2;
				eti2.m_elty = keltyDatabase;
				eti2.m_icls = eti.m_icls;
				m_vetiOpen.Push(eti2);
				// Store the object information.
				m_vetiOpen.Push(eti);
				m_vhobjOpen.Push(m_hvoOwner);
				return;
			}
			// We're sure to blow up somewhere below if we haven't returned by now...
		}
		switch (eti.m_elty)
		{
		case keltyObject:
			StartObject2(eti, pszName);
			break;
		case keltyCustomProp:
			StartCustomProp2(eti, pszName, prgpszAtts);
			break;
		case keltyPropName:
			StartPropName2(eti);
			break;
		case keltyVirtualProp:
			StartVirtualProp2(eti, pszName);
			break;
		case keltyBasicProp:
			StartBasicProp2(eti, pszName, prgpszAtts, stabCmd);
			break;
		default:	// includes keltyDatabase, keltyAddProps, and keltyDefineProp
			// THIS SHOULD NEVER HAPPEN! -- "Unknown XML start tag: \"%s\""
			ThrowWithLogMessage(kstidXmlErrorMsg128, pszName);
			break;
		}
		BatchSqlCommand(stabCmd);
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		staFmt.Load(kstidXmlDebugMsg003);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		staFmt.Load(kstidXmlDebugMsg005);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Set the toplevel element information for the parser to use.

	@param hvoOwner database object id
	@param flid Field id of field belonging to hvoOwner
	@param icls Class id of hvoOwner's class
	@param pszBeginTag XML tag of expected outermost element in input file
	@param hvoObj database object id of object belonging to hvoOwner in field flid, or 0
	@param guidObj guid of object belonging to hvoOwner in field flid, or GUID_NULL
	@param hvoMin one less than the lowest database object id which is available for assignment
					to new objects
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::InitializeTopElementInfo(int hvoOwner, int flid, int icls,
	const wchar * pszBeginTag, int hvoObj, const GUID guidObj, int hvoMin)
{
	m_hvoOwner = hvoOwner;
	m_flid = flid;
	m_iclsOwner = icls;
	m_staBeginTag.Assign(pszBeginTag);
	m_hvoObj = hvoObj;
	m_guidObj = guidObj;
	m_hvoMin = hvoMin;
	if (icls != -1 && m_pfwxd->ClassName(icls) != pszBeginTag)
		m_staOwnerBeginTag.Assign(m_pfwxd->ClassName(icls).Chars());
	else
		m_staOwnerBeginTag.Clear();
}

/*----------------------------------------------------------------------------------------------
	Fill in the hashmap of GUIDs to object ids.  This is needed to detect duplication in the
	input file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::MapGuidsToHobjs()
{
	int hobj;
	GUID guidObj;
	SDWORD cbGuid;
	SDWORD cbT;
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	StrAnsi staCmd;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd = "SELECT Id, Guid$ FROM CmObject;";
	}
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbT);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_GUID, &guidObj, isizeof(guidObj), &cbGuid);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbGuid == SQL_NULL_DATA)
			continue;
		m_hmguidhobj.Insert(guidObj, hobj);
	}
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Fill in the hashmap of writing system strings (ICU Locales) to ws ints.  This is needed
	for any string data that specifies a writing system.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::MapIcuLocalesToWsHobjs()
{
	int ws;
	const int kcchMax = 4000;
	SQLWCHAR rgchIcuLocale[kcchMax];
	SDWORD cbLocale;
	SDWORD cbT;
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	StrAnsi staCmd;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd = "SELECT Id, ICULocale FROM LgWritingSystem;";
	}
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &ws, isizeof(ws), &cbT);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_WCHAR, &rgchIcuLocale, isizeof(rgchIcuLocale),
		&cbLocale);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbT == SQL_NULL_DATA || cbLocale == SQL_NULL_DATA)
			continue;
		int cch = cbLocale / isizeof(SQLWCHAR);
		if (!rgchIcuLocale[cch - 1])
			--cch;			// Don't include trailing NUL.
		StrAnsi staT(rgchIcuLocale, cch);
		staT.ToLower();
		m_hmcws.Insert(staT.Chars(), ws, true);
	}
	sstmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Open the input file and log file, basing the name of the log file on that of the input file.

	@param bstrFile - Name of the input XML file.
	@param pstatFile - pointer to a STATSTG struct with information about the input file
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::OpenFiles(BSTR bstrFile, STATSTG * pstatFile)
{
	FileStream::Create(bstrFile, STGM_READ, &m_qstrm);
	CheckHr(m_qstrm->Stat(pstatFile, STATFLAG_NONAME));
	m_stabpFile = bstrFile;
	m_stabpLog = bstrFile;
	int ich = m_stabpLog.ReverseFindCh('.');
	if (ich != -1)
		m_stabpLog.SetLength(ich);
	m_stabpLog.Append("-Import.log");
	fopen_s(&m_pfileLog, m_stabpLog.Chars(), "w");
}

/*----------------------------------------------------------------------------------------------
	Set the outer handlers for starting and ending XML elements.

	@param startFunc - function for handling XML start elements (<xyz>)
	@param endFunc - function for handling XML end elements (</xyz>)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetOuterHandlers(XML_StartElementHandler startFunc,
	XML_EndElementHandler endFunc)
{
	m_startOuterHandler = startFunc;
	m_endOuterHandler = endFunc;
}


/*----------------------------------------------------------------------------------------------
	Write messages that were repeated a number of times to the log file.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::LogRepeatedMessages()
{
	if (m_hmcMsgcMore.Size())
	{
		// Log repeated message counts.
		// "%<0>s  [repeated %<1>d more times in the XML file]\n"
		StrAnsi staFmt(kstidXmlInfoMsg107);
		StrAnsi sta;
		HashMapChars<int>::iterator it;
		for (it = m_hmcMsgcMore.Begin(); it != m_hmcMsgcMore.End(); ++it)
		{
			// Erase any trailing newline chars from the original message.
			StrAnsi staT(it->GetKey());
			int ich = staT.ReverseFindCh('\r');
			if (ich == -1)
				ich = staT.ReverseFindCh('\n');
			if (ich != -1)
				staT.Replace(ich, staT.Length(), "");
			sta.Format(staFmt.Chars(), staT.Chars(), it->GetValue());
			LogMessage(sta.Chars());
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Check that the database is readable, but empty.  This throws if the database cannot be read
	or if the database is not empty.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::EnsureEmptyDatabase()
{
	bool fIsNull;
	int crow;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		crow = ReadOneIntFromSQL("SELECT COUNT(*) FROM CmObject;", __LINE__, fIsNull);
	}
	if (!fIsNull && crow != 0)
	{
		// "The database is not empty!"
		ThrowWithLogMessage(kstidXmlErrorMsg102);
	}
}

/*----------------------------------------------------------------------------------------------
	Log the time consumed by some action.

	@param timDelta number of seconds elapsed during the first pass
	@param stid message id for the report
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ReportTime(long timDelta, int stid)
{
	StrAnsi staFmt(stid);
	StrAnsi staSecond;
	if (timDelta == 1)
		staSecond.Load(kstidSecond);
	else
		staSecond.Load(kstidSeconds);
	StrAnsi sta;
	sta.Format(staFmt.Chars(), timDelta, staSecond.Chars());
	LogMessage(sta.Chars());
}


/*----------------------------------------------------------------------------------------------
	Log the time consumed by the first pass of reading the XML file.

	@param timDelta number of seconds elapsed during the first pass
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ReportFirstPassTime(long timDelta)
{
	// "First pass of reading the XML file took %d second%s."
	ReportTime(timDelta, kstidXmlInfoMsg003);
}


/*----------------------------------------------------------------------------------------------
	Log the number of custom fields created.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ReportCustomFields()
{
	// "%d custom field%s have been added to the database schema.\n",
	StrAnsi staFmt(kstidXmlInfoMsg002);
	StrAnsi staField;
	if (m_cCustom == 1)
		staField.Load(kstidField);
	else
		staField.Load(kstidFields);
	StrAnsi sta;
	sta.Format(staFmt.Chars(), m_cCustom, staField.Chars());
	LogMessage(sta.Chars());
}


/*----------------------------------------------------------------------------------------------
	Log the time consumed by creating objects after the first pass of reading the XML file.

	@param timDelta number of seconds elapsed while creating objects in the database
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ReportCreatingObjects(long timDelta)
{
	// "Creating %d objects after the first pass took %d second%s."
	StrAnsi staFmt(kstidXmlInfoMsg007);
	StrAnsi staSecond;
	if (timDelta == 1)
		staSecond.Load(kstidSecond);
	else
		staSecond.Load(kstidSeconds);
	StrAnsi sta;
	sta.Format(staFmt.Chars(), m_cobj - 1, timDelta, staSecond.Chars());
	LogMessage(sta.Chars());
}


/*----------------------------------------------------------------------------------------------
	Log the time consumed by the second pass of reading the XML file.

	@param timDelta number of seconds elapsed during the second pass
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ReportSecondPassTime(long timDelta)
{
	// "Second pass of reading the XML file took %d %s."
	ReportTime(timDelta, kstidXmlInfoMsg009);
}


/*----------------------------------------------------------------------------------------------
	Log the time consumed by storing data after the second pass of reading the XML file.

	@param timDelta number of seconds elapsed while storing data
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ReportDataStorageStats(long timDelta)
{
	// "Storing data after the second pass took %d second%s."
	ReportTime(timDelta, kstidXmlInfoMsg004);
	// "Storing the data into the database took %d SQL command%s."
	StrAnsi staFmt(kstidXmlInfoMsg005);
	StrAnsi staCommand;
	if (m_cSql == 1)
		staCommand.Load(kstidCommand);
	else
		staCommand.Load(kstidCommands);
	StrAnsi sta;
	sta.Format(staFmt.Chars(), m_cSql, staCommand.Chars());
	LogMessage(sta.Chars());
}


/*----------------------------------------------------------------------------------------------
	Log the total amount of time consumed by loading the database from the XML file.

	@param timDelta total number of seconds elapsed while loading the database
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ReportTotalTime(long timDelta)
{
	// "Loading the XML file into the database took %d second%s."
	ReportTime(timDelta, kstidXmlInfoMsg006);
}


/*----------------------------------------------------------------------------------------------
	Verify that hvoOwner and flid are valid (and compatible), and set ifld and icls properly.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::EnsureValidImportArguments(int hvoOwner, int flid, int & ifld, int & icls)
{
	// Verify that hvoOwner is valid.
	StrAnsi staCmd;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd.Format("SELECT Class$ FROM CmObject WHERE Id = %d;", hvoOwner);
	}
	bool fIsNull;
	int clid = ReadOneIntFromSQL(staCmd.Chars(), __LINE__, fIsNull);
	if (fIsNull)
	{
		// "Invalid hvoOwner passed to ImportXmlObject method: %<0>d"
		StrAnsi staFmt(kstidXmlErrorMsg301);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), hvoOwner);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_INVALIDARG));
	}
	// Double check that we're sane.
	icls = -1;
	if (!m_pfwxd->MapCidToIndex(clid, &icls))
	{
		// "Unknown clid retrieved for hvoOwner (%<0>d): SOMETHING IS VERY WRONG!"
		StrAnsi sta(kstidXmlErrorMsg302);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_INVALIDARG));
	}
	// Check that flid is valid.
	ifld = -1;
	if (!m_pfwxd->MapFidToIndex(flid, &ifld))
	{
		// "Unknown flid passed to ImportXmlObject method: %<0>d"
		StrAnsi sta(kstidXmlErrorMsg303);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_INVALIDARG));
	}
	// Check that flid is compatible with hvoOwner.
	int clidFlid = MAKECLIDFROMFLID(flid);
	if (clidFlid != clid)
	{
		// The flid might be from a base class, so work down the inheritance chain.
		int clidBase = m_pfwxd->ClassInfo(icls).cidBase;
		bool fOk = false;
		while (clidBase)
		{
			fOk = (clidFlid == clidBase);
			if (fOk)
				break;
			int iclsBase = -1;
			if (!m_pfwxd->MapCidToIndex(clid, &iclsBase))
				break;		// This shouldn't ever happen, but ...
			clidBase = m_pfwxd->ClassInfo(iclsBase).cidBase;
		}
		if (!fOk)
		{
			// "Invalid flid passed to ImportXmlObject method: flid = %<0>d, but class = %<1>d"
			StrAnsi sta(kstidXmlErrorMsg304);
			LogMessage(sta.Chars());
			ThrowHr(WarnHr(E_INVALIDARG));
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Verify that hvoOwner is valid, and return the index into the class tables of the class of
	hvoOwner.

	@param hvoOwner database object id
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetClassIndexOfObject(int hvoOwner)
{
	// Verify that hvoOwner is valid.
	StrAnsi staCmd;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd.Format("SELECT Class$ FROM CmObject WHERE Id = %d;", hvoOwner);
	}
	bool fIsNull;
	int clid = ReadOneIntFromSQL(staCmd.Chars(), __LINE__, fIsNull);
	if (fIsNull)
	{
		// "Invalid hvoOwner passed to ImportXmlObject method: %<0>d"
		StrAnsi sta(kstidXmlErrorMsg301);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_INVALIDARG));
	}
	// Double check that we're sane.
	int icls = -1;
	if (!m_pfwxd->MapCidToIndex(clid, &icls))
	{
		// "Unknown clid retrieved for hvoOwner (%<0>d): SOMETHING IS VERY WRONG!"
		StrAnsi sta(kstidXmlErrorMsg302);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_INVALIDARG));
	}
	return icls;
}


/*----------------------------------------------------------------------------------------------
	For an atomic field, the object may already exist in an uninitialized form:  if so,
	record its database object id so that we won't try to create it again.

----------------------------------------------------------------------------------------------*/
void FwXmlImportData::LoadPossibleExistingObjectId(int hvoOwner, int flid, int & hvoObj,
	GUID & guidObj)
{
	SqlStatement sstmt;
	StrAnsi staCmd;
	SDWORD cbT;
	sstmt.Init(m_sdb);
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd.Format("SELECT Id, Guid$ FROM CmObject WHERE Owner$ = %d AND OwnFlid$ = %d;",
			hvoOwner, flid);
	}
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, staCmd.Chars());
	rc = SQLFetch(sstmt.Hstmt());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_SLONG, &hvoObj, isizeof(hvoObj), &cbT);
	if (rc != SQL_SUCCESS || cbT == SQL_NULL_DATA)
	{
		hvoObj = 0;
	}
	else
	{
		rc = SQLGetData(sstmt.Hstmt(), 2, SQL_C_GUID, &guidObj, isizeof(guidObj), &cbT);
		if (rc != SQL_SUCCESS || cbT == SQL_NULL_DATA)
		{
			hvoObj = 0;
			guidObj = GUID_NULL;
		}
	}
	sstmt.Clear();
}


/*----------------------------------------------------------------------------------------------
	Get the maximum existing object id from the database to use for creating new objects.

----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetNextRealObjectId()
{
	bool fIsNull;
	int hvoMin;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		hvoMin = ReadOneIntFromSQL("SELECT MAX([Id]) FROM CmObject;", __LINE__, fIsNull);
	}
	if (fIsNull || !hvoMin)
	{
		// "Empty database: THIS SHOULD NEVER HAPPEN!"
		StrAnsi sta(kstidXmlErrorMsg305);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_INVALIDARG));
	}
	++hvoMin;		// Start one greater than existing maximum id value.
	return hvoMin;
}


/*----------------------------------------------------------------------------------------------
	Process the XML file (first of two passes).

	@param statFile Reference to the XML file info structure.
	@param padvi Pointer to progress bar interface (may be NULL).
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ParseXmlPhaseOne(int cFld, STATSTG & statFile, IAdvInd * padvi,
	int nPercent)
{
	//fprintf(stdout,"begin ParseXmlPhaseOne\n");
	StrAnsi sta;
	const LARGE_INTEGER libMove = {0,0};
	ULARGE_INTEGER ulibPos;
	ulong cbRead;
	//  PHASE ONE.
	//  Create a parser to scan over the file(s) to create the basic objects/ids.
	m_parser = XML_ParserCreate(NULL);
	XML_SetUserData(m_parser, this);
	if (!XML_SetBase(m_parser, m_stabpFile.Chars()))
	{
		// "Out of memory before parsing anything!"
		sta.Load(kstidXmlErrorMsg095);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_OUTOFMEMORY));
	}
	XML_SetExternalEntityRefHandler(m_parser, FwXmlImportData::HandleExternalEntityRef);
	XML_SetElementHandler(m_parser, m_startOuterHandler, m_endOuterHandler);
	XML_SetCharacterDataHandler(m_parser, FwXmlImportData::HandleCharData1);
	m_vcfld.Resize(cFld);
	m_cBlkTotal = statFile.cbSize.LowPart / READ_SIZE;
	if (!m_cBlkTotal)
		m_cBlkTotal = 1;

	for (int cblk = 0; ; ++cblk)
	{
		void * pBuffer = XML_GetBuffer(m_parser, READ_SIZE);
		if (!pBuffer)
		{
			// "Cannot get buffer from the XML parser [pass 1]!  (Out of memory?)"
			ThrowWithLogMessage(kstidXmlErrorMsg013);
		}
		CheckHr(m_qstrm->Read(pBuffer, READ_SIZE, &cbRead));
		//fprintf(stdout,"int cbRead=%d,  cbRead==0=%d\t", cbRead, cbRead==0);
		if (cblk == 0)
			CheckForBOM(pBuffer, cbRead);

		if (!XML_ParseBuffer(m_parser, int(cbRead), int(cbRead == 0)))
		{
			//fprintf(stdout,"xml before sta.Load\t");
			// "XML parser detected an XML syntax error [pass 1]!"
			sta.Load(kstidXmlErrorMsg135);
			LogMessage(sta.Chars());
			ThrowHr(WarnHr(E_FAIL));
			//fprintf(stdout,"end of !XML_ParseBuffer\t");
		}
		//else
			//fprintf(stdout,"XML_ParseBuffer\t");

		if (m_fError)
		{
			//fprintf(stdout,"m_fError=%d before sta.Load\t", m_fError);
			// "Error detected while parsing XML file [pass 1]!"
			sta.Load(kstidXmlErrorMsg037);
			LogMessage(sta.Chars());
			//fprintf(stdout,"after hr=%s\t", AsciiHresult(m_hr));
			ThrowHr(WarnHr(m_hr));//error thrown here, came from int XML_ParseBuffer() in fw\lib\xmlparse\xmlparse.c
		}
		CheckHr(m_qstrm->Seek(libMove, STREAM_SEEK_CUR, &ulibPos));
		if (padvi)
		{
			//fprintf(stdout,"padvi\t");
			// Parsing the first pass is about 2% of the progress to report for LoadXml.
			++m_cBlk;
			unsigned long cStepNew = m_cBlk * nPercent / m_cBlkTotal;
			if (cStepNew > m_cStep)
			{
				padvi->Step(cStepNew - m_cStep);
				m_cStep = cStepNew;
			}
		}
		if ((ulibPos.HighPart == statFile.cbSize.HighPart) &&
			(ulibPos.LowPart == statFile.cbSize.LowPart))
		{
			// Successfully processed the XML file the first time through.
			Assert(m_celemStart == m_celemEnd);
			break;
		}
		//fprintf(stdout,"end of loop iter=%d\n",cblk);
	}
	XML_ParserFree(m_parser);
	m_parser = 0;
}


/*----------------------------------------------------------------------------------------------
	Process the XML file (second of two passes).

	@param statFile Reference to the XML file info structure.
	@param padvi Pointer to progress bar interface (may be NULL).
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ParseXmlPhaseTwo(int cFld, STATSTG & statFile, IAdvInd * padvi,
	int nPercent)
{
	//fprintf(stdout,"inside ParseXmlPhaseTwo\n");
	const LARGE_INTEGER libMove = {0,0};
	ULARGE_INTEGER ulibPos;
	ulong cbRead;

	// PHASE TWO.
	// Create another parser to read all of the data into the database, first rewinding the
	// input file back to the beginning.

	CheckHr(m_qstrm->Seek(libMove, STREAM_SEEK_SET, NULL));
	m_parser = XML_ParserCreate(NULL);
	XML_SetUserData(m_parser, this);
	if (!XML_SetBase(m_parser, m_stabpFile.Chars()))
	{
		// "Out of memory after first pass through XML file!"
		StrAnsi sta(kstidXmlErrorMsg094);
		LogMessage(sta.Chars());
		ThrowHr(WarnHr(E_OUTOFMEMORY));
	}
	XML_SetExternalEntityRefHandler(m_parser, FwXmlImportData::HandleExternalEntityRef);
	XML_SetElementHandler(m_parser, m_startOuterHandler, m_endOuterHandler);
	XML_SetCharacterDataHandler(m_parser, FwXml::HandleCharData);
	m_celemStart = 0;
	m_celemEnd = 0;
	m_vstda.Resize(cFld);
	m_cBlkTotal = statFile.cbSize.LowPart / READ_SIZE;
	if (!m_cBlkTotal)
		m_cBlkTotal = 1;

	// Process the XML file (Pass Two).
	//fprintf(stdout,"before for loop\n");
	for (int cblk = 0; ; ++cblk)
	{
		void * pBuffer = XML_GetBuffer(m_parser, READ_SIZE);
		if (!pBuffer)
		{
			// "Cannot get buffer from the XML parser [pass 2]!  (Out of memory?)"
			ThrowWithLogMessage(kstidXmlErrorMsg014);
		}
		//fprintf(stdout,"XML_GetBuffer\t");
		CheckHr(m_qstrm->Read(pBuffer, READ_SIZE, &cbRead));
		if (cblk == 0)
			CheckForBOM(pBuffer, cbRead);
		if (!XML_ParseBuffer(m_parser, cbRead, cbRead == 0))
		{
			// "XML parser detected an XML syntax error [pass 2]!"
			StrAnsi sta(kstidXmlErrorMsg136);
			LogMessage(sta.Chars());
			ThrowHr(WarnHr(E_FAIL));
		}
		//else
			//fprintf(stdout,"XML_ParseBuffer\t");
		if (m_fError)
		{
			// "Error detected while parsing XML file [pass 2]!"
			StrAnsi sta(kstidXmlErrorMsg038);
			LogMessage(sta.Chars());
			//fprintf(stdout,"m_fError=%d, m_hr=%s\t",m_fError, AsciiHresult(m_hr));
			ThrowHr(WarnHr(m_hr));
		}
		CheckHr(m_qstrm->Seek(libMove, STREAM_SEEK_CUR, &ulibPos));
		if (padvi)
		{
			// Parsing the second pass is 36% of the progress to report for LoadXml.
			++m_cBlk;
			unsigned long cStepNew = m_cBlk * nPercent / m_cBlkTotal;
			if (cStepNew > m_cStep)
			{
				padvi->Step(cStepNew - m_cStep);
				m_cStep = cStepNew;
			}
		}
		if ((ulibPos.HighPart == statFile.cbSize.HighPart) &&
			(ulibPos.LowPart == statFile.cbSize.LowPart))
		{
			// Successfully processed the XML file the second time through.
			Assert(ObjectCount() == ObjectCount_Pass2());
			break;
		}
		//fprintf(stdout,"end of loop iter=%d\n",cblk);
	}
	if (m_staCmd.Length())
		UpdateDatabaseObjects(m_sdb, __LINE__);
}


/*----------------------------------------------------------------------------------------------
	Initialize the data used to control merging into existing atomic objects.

	@param hvoOwner Database id of the top-level object for importing/merging.
	@param icls Index into class tables for the class of the top-level object.
	@param hvoMax Largest existing database object id.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::InitializeForMerging(int hvoOwner, int icls, int hvoLim)
{
	m_fMerge = true;
	m_fAllowNewWritingSystems = true;

	m_veod.Clear();
	// Note that the owner and fid are irrelevant for the top-level object (and may be NULL).
	PushExistingObjectInfo(hvoOwner, icls, -1, -1);

	m_hvoFirst = hvoLim;		// Existing objects have id < m_hvoFirst.
}


/*----------------------------------------------------------------------------------------------
	Check whether the indicated object already exists.  If so, store the necessary information
	for later use.

	@param eti type information for the object (derived from the XML start tag)
	@param hobjOwner database id of the object's owner
	@return true if the object already exists, otherwise false
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::CheckForExistingObject(const ElemTypeInfo & eti, int hobjOwner)
{
	if (hobjOwner >= m_hvoFirst)
		return false;		// Owner is being created, so this must be created as well.

	ElemTypeInfo & etiProp = m_vetiOpen[m_vetiOpen.Size() - 1];
	Assert((unsigned)etiProp.m_ifld < (unsigned)m_pfwxd->FieldCount());
	int cpt = m_pfwxd->FieldInfo(etiProp.m_ifld).cpt;
	if (cpt != kcptOwningAtom)
	{
		Assert(cpt == kcptOwningCollection || cpt == kcptOwningSequence);
		return false;		// We always add to sequences or collections.
	}
	// We have a candidate: the owner already exists, and it's in a OwningAtom field.
	int hobj;
	int cidObj;
	SqlStatement sstmt;
	StrAnsi staCmd;
	SDWORD cbHobj;
	SDWORD cbCid;
	sstmt.Init(m_sdb);
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd.Format("SELECT Id, Class$ FROM CmObject WHERE Owner$ = %d AND OwnFlid$ = %d;",
			hobjOwner, m_pfwxd->FieldInfo(etiProp.m_ifld).fid);
	}
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, staCmd.Chars());
	rc = SQLFetch(sstmt.Hstmt());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbHobj);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLGetData(sstmt.Hstmt(), 2, SQL_C_SLONG, &cidObj, isizeof(cidObj), &cbCid);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	sstmt.Clear();
	if (cbHobj == SQL_NULL_DATA || cbCid == SQL_NULL_DATA)
	{
		Assert(cbHobj == cbCid);	// They should both be SQL_NULL_DATA.
		return false;
	}
	m_vetiOpen.Push(eti);
	m_vhobjOpen.Push(hobj);
	PushExistingObjectInfo(hobj, eti.m_icls, hobjOwner, m_pfwxd->FieldInfo(etiProp.m_ifld).fid);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Store the information for an existing object, and store a mapping from its owner and field
	to that stored information.

	@param hobj database id of the object
	@param icls index into the class tables for the object
	@param hobjOwner database id of the object's owner
	@param fid field id relating the object to its owner
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::PushExistingObjectInfo(int hobj, int icls, int hobjOwner, int fid)
{
	int idx = m_veod.Size();
	ExistingObjData eod = { hobj, icls, hobjOwner, fid };
	m_veod.Push(eod);
	OwnerField of = { hobjOwner, fid };
	m_hmofieod.Insert(of, idx);
}


/*----------------------------------------------------------------------------------------------
	Using the open element stack and the type information derived from the XML start tag, check
	whether the object is one that existed before this import operation started.  Return the
	object id in hobj if so.

	@param eti type information for the object (derived from the XML start tag)
	@param pszName XML start tag
	@param hobj reference to the object's database id (output)
	@return true if the object already exists and hobj is set, otherwise false
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::GetExistingObject(const ElemTypeInfo & eti, const XML_Char * pszName,
	int & hobj)
{
	ElemTypeInfo & etiProp = m_vetiOpen[m_vetiOpen.Size() - 1];
	int hobjOwner = GetOwnerOfCurrentObject(etiProp, pszName);
	if (hobjOwner >= m_hvoFirst)
		return false;		// Owner is being created, so this must be created as well.

	Assert((unsigned)etiProp.m_ifld < (unsigned)m_pfwxd->FieldCount());
	int cpt = m_pfwxd->FieldInfo(etiProp.m_ifld).cpt;
	if (cpt != kcptOwningAtom)
	{
		Assert(cpt == kcptOwningCollection || cpt == kcptOwningSequence);
		return false;		// We always add to sequences or collections.
	}
	// We have a candidate: the owner already exists, and it's in a OwningAtom field.
	int fid = m_pfwxd->FieldInfo(etiProp.m_ifld).fid;
	OwnerField of = { hobjOwner, fid };
	int idx;
	if (m_hmofieod.Retrieve(of, &idx))
	{
		hobj = m_veod[idx].hobj;
		return true;
	}
	else
	{
		hobj = 0;
		return false;
	}
}


/*----------------------------------------------------------------------------------------------
	Add the existing objects to the CreatedObjectSet.

	@param cos reference to a set of created objects (input/output)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::RecordExistingObjects(CreatedObjectSet & cos)
{
	for (int i = 0; i < m_veod.Size(); ++i)
		cos.AddObject(m_veod[i].hobj);
}


/*----------------------------------------------------------------------------------------------
	Handle a link element which lacks a target attribute, but which has a ws attribute.
	This attribute implies a possible ReversalIndex or ReversalIndexEntry object, at
	least if the information from FieldInfo(ifld) agrees.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param ifld Index into m_vfdfi and m_vstufld for the field of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param ws Database id of the desired Writing system.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::StoreWritingSystemReference(const XML_Char * pszName,
	const XML_Char ** prgpszAtts, int hobjOwner, int ifld, int icls, int ws)
{
	switch (m_pfwxd->FieldInfo(ifld).fid)
	{
	case kflidReversalIndex_WritingSystem:
	case kflidReversalIndexEntry_WritingSystem:
		break;
	default:
		return false;
	}
	// Check whether we already have a ReversalIndex with this same writing system.  If so,
	// we want to reuse the existing object, and discard whatever we've stored for the newly
	// created object.
	if (m_pfwxd->FieldInfo(ifld).fid == kflidReversalIndex_WritingSystem)
	{
		Assert(hobjOwner >= m_hvoFirst);
		StrAnsi staCmd;
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			staCmd.Format(
				"SELECT Id FROM ReversalIndex WHERE WritingSystem = %<0>u AND Id < %<1>u;",
				ws, m_hvoFirst);
		}
		bool fIsNull;
		int hobjOld = ReadOneIntFromSQL(staCmd.Chars(), __LINE__, fIsNull);
		if (!fIsNull && hobjOld > 0)
		{
			Assert(*m_vhobjOpen.Top() == hobjOwner);
			*m_vhobjOpen.Top() = hobjOld;
			Assert(m_pfwxd->ClassInfo(icls).cid == kclidReversalIndex);
			ChangeOwners(hobjOwner, hobjOld, icls);
			m_vhobjDel.Push(hobjOwner);
		}
	}

	// Handling this kind of reference is rather trivial apart from wanting to reuse
	// previously existing ReversalIndex entries with the same writing system...
	StoreReference(prgpszAtts, hobjOwner, ifld, ws);
	return true;
}


/*----------------------------------------------------------------------------------------------
	Change objects in owning collection/sequence fields to be owned by hobjNew instead of
	hobjOld.  For atomic owning fields, recursively change their owning collection/sequence
	fields to be owned by the corresponding object owned (possibly indirectly) by hobjNew.

	@param hobjOld previous owner (soon to be deleted)
	@param hobjNew new owner
	@param icls index into the class tables
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ChangeOwners(int hobjOld, int hobjNew, int icls)
{
	Vector<int> & vifld = m_pfwxd->m_mpclsflds[icls];
	for (int i = 0; i < vifld.Size(); ++i)
	{
		int ifld = vifld[i];
		const FwDbFieldInfo & fdfi = m_pfwxd->FieldInfo(ifld);
		switch (fdfi.cpt)
		{
		case kcptOwningAtom:
			UpdateAtomSubListsOwner(hobjOld, hobjNew, icls, ifld);
			break;
		case kcptOwningCollection:
			UpdateCollectionOwner(hobjOld, hobjNew, icls, ifld);
			break;
		case kcptOwningSequence:
			UpdateSequenceOwner(hobjOld, hobjNew, icls, ifld);
			break;
		// Ignore references for now...
		case kcptReferenceAtom:
			break;
		case kcptReferenceCollection:
			break;
		case kcptReferenceSequence:
			break;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	If the atomic owning field belonging to hobjNew is empty, change the owner of the one
	belonging to hobjOld (if it exists).  Otherwise if both objects exist, recursively check
	any objects owned by those objects for transference of ownership.

	@param hobjOld previous owner (soon to be deleted)
	@param hobjNew new owner
	@param icls index into the class tables
	@param ifld index into the field tables
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::UpdateAtomSubListsOwner(int hobjOld, int hobjNew, int icls, int ifld)
{
	int flid = m_pfwxd->FieldInfo(ifld).fid;
	StrAnsi staCmd;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd.Format("SELECT Id FROM CmObject WHERE Owner$=%<0>u AND OwnFlid$=%<1>u;",
			hobjOld, flid);
	}
	bool fIsNull;
	int hobjOldAtom = ReadOneIntFromSQL(staCmd.Chars(), __LINE__, fIsNull);
	if (fIsNull)
		return;		// nothing to update.
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd.Format("SELECT Id FROM CmObject WHERE Owner$=%<0>u AND OwnFlid$=%<1>u;",
			hobjNew, flid);
	}
	int hobjNewAtom = ReadOneIntFromSQL(staCmd.Chars(), __LINE__, fIsNull);
	if (fIsNull)
	{
		// The value wasn't filled in for the new owner, so we'll just ease in the value from
		// the old owner by changing ownership.
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			staCmd.Format("UPDATE CmObject SET Owner$=%<0>u WHERE Owner$=%<1>u AND OwnFlid$=%<2>u;",
				hobjNew, hobjOld, flid);
		}
		ExecuteSimpleSQL(staCmd.Chars(), __LINE__);
	}
	else
	{
		// We have to recursively change anything owned by hobjOldAtom to become owned by the
		// corresponding field belonging to hobjNewAtom.
		int iclsAtom;
		int cidAtom = m_pfwxd->FieldInfo(ifld).cidDst;
		if (!m_pfwxd->MapCidToIndex(cidAtom, &iclsAtom))
		{
			// ERROR--should never happen!
		}
		ChangeOwners(hobjOldAtom, hobjNewAtom, iclsAtom);
	}
}


/*----------------------------------------------------------------------------------------------
	Change objects in owning collection fields to be owned by hobjNew instead of hobjOld.

	@param hobjOld previous owner (soon to be deleted)
	@param hobjNew new owner
	@param icls index into the class tables
	@param ifld index into the field tables
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::UpdateCollectionOwner(int hobjOld, int hobjNew, int icls, int ifld)
{
	StrAnsi staCmd;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd.Format("UPDATE CmObject SET Owner$=%<0>u WHERE Owner$=%<1>u AND OwnFlid$=%<2>u;",
			hobjNew, hobjOld, m_pfwxd->FieldInfo(ifld).fid);
	}
	ExecuteSimpleSQL(staCmd.Chars(), __LINE__);
}


/*----------------------------------------------------------------------------------------------
	Change objects in owning sequence fields to be owned by hobjNew instead of hobjOld.

	@param hobjOld previous owner (soon to be deleted)
	@param hobjNew new owner
	@param icls index into the class tables
	@param ifld index into the field tables
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::UpdateSequenceOwner(int hobjOld, int hobjNew, int icls, int ifld)
{
	int flid = m_pfwxd->FieldInfo(ifld).fid;
	StrAnsi staCmd;
	bool fIsNull;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd.Format("SELECT MAX(OwnOrd$) FROM CmObject WHERE Owner$=%<0>u AND OwnFlid$=%<1>u;",
			hobjNew, flid);
	}
	int ordMin = ReadOneIntFromSQL(staCmd.Chars(), __LINE__, fIsNull);
	if (fIsNull)
		ordMin = 0;
	else
		++ordMin;	// ensure no overlap.
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd.Format("UPDATE CmObject SET Owner$=%<0>u, OwnOrd$=OwnOrd$+%<1>u "
			"WHERE Owner$=%<2>u AND OwnFlid$=%<3>u;",
			hobjNew, ordMin, hobjOld, flid);
	}
	ExecuteSimpleSQL(staCmd.Chars(), __LINE__);
}


/*----------------------------------------------------------------------------------------------
	Check whether the given file exists.

	@param pszPath possible file pathname
	@return true if the file exists, otherwise false
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::FileExists(const char * pszPath)
{
	DWORD dwAtts = ::GetFileAttributesA(pszPath);
	if (dwAtts == INVALID_FILE_ATTRIBUTES)
		return false;
	else
		return true;
}


/*----------------------------------------------------------------------------------------------
	Ensure that the given directory exists.

	@param pszDir directory pathname
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::EnsureDirectoryExists(const char * pszDir)
{
	BOOL fOk = ::CreateDirectoryA(pszDir, NULL);
	if (!fOk)
	{
		DWORD dwErr = ::GetLastError();
		if (dwErr != ERROR_ALREADY_EXISTS)
		{
			char rgchMsg[MAX_PATH+1];
			DWORD cch = ::FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM, NULL, dwErr, 0, rgchMsg,
				MAX_PATH, NULL);
			rgchMsg[cch] = 0;
			// "Cannot create directory ""%<0>s"".\n"
			StrAnsi staFmt(kstidXmlErrorMsg314);
			StrAnsi sta;
			sta.Format(staFmt.Chars(), pszDir);
			LogMessage(sta.Chars());
			LogMessage(rgchMsg);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Handle a link element which lacks a target attribute, but which has a path attribute and not
	a ws attribute.  The form attribute implies a possible MoAffixAllomorph or MoStemAllomorph
	object, at least if the information from FieldInfo(ifld) agrees.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param hobjOwner Database id of the owning object.
	@param ifld Index into m_vfdfi and m_vstufld for the field of the owning object.
	@param icls Index into m_vfdci and m_vstucls for the class of the owning object.
	@param pszPath possible picture file pathname
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::StoreFilePathReference(const XML_Char * pszName,
	const XML_Char ** prgpszAtts, int hobjOwner, int ifld, int icls, const char * pszPath)
{
	if (m_pfwxd->FieldInfo(ifld).cidDst != kclidCmFile)
		return false;
	int cpt = m_pfwxd->FieldInfo(ifld).cpt;
	if (cpt != kcptReferenceAtom &&
		cpt != kcptReferenceCollection &&
		cpt != kcptReferenceSequence)
	{
		return false;
	}
	int fid = m_pfwxd->FieldInfo(ifld).fid;
	// First, see if the file exists.  If not, try adding a leading directory path.
	StrAnsi staSrcPath;
	if (!FileExists(pszPath))
	{
		bool fFileExists = false;
		if (m_pfwxd->m_stuBaseImportDirectory.Length())
		{
			staSrcPath.Format("%S\\%s", m_pfwxd->m_stuBaseImportDirectory.Chars(), pszPath);
			fFileExists = FileExists(staSrcPath.Chars());
			if (fFileExists)
				pszPath = staSrcPath.Chars();	// change it to the full path for storing.
		}
		if (!fFileExists)
		{
			StrAnsi staFmt;
			StrAnsi sta;
			StrUni stuMsg;
			if (fid == kflidCmPicture_PictureFile)
			{
				staFmt.Load(kstidXmlErrorMsg308);	// "Picture file ""%<0>s"" does not exist."
			}
			else
			{
				Assert(fid == kflidCmMedia_MediaFile);
				staFmt.Load(kstidXmlErrorMsg312);	// "Media file ""%<0>s"" does not exist."
			}
			sta.Format(staFmt.Chars(), pszPath);
			LogMessage(sta.Chars());
		}
	}
	int hobjTarget = CreateCmFileForLangProject(fid, pszPath);
	StoreReference(prgpszAtts, hobjOwner, ifld, hobjTarget);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Store a message in a multi-string field using the user interface writing system.

	@param hobjOwner Database id of the owning object.
	@param flid Field id within the owning object
	@param stuMsg String to store as multi-lingual formatted string
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::StoreMessageInMultiStringField(int hobjOwner, int flid, StrUni & stuMsg)
{
	// Get the user interface writing system, adding it to the analysis writing systems if it's
	// not already there.
	StrAnsi staWs(kstidXmlUserWs);
	// "Cannot convert \"%s\" into a Language Writing system code."
	int wsUser = GetWsFromIcuLocale(staWs.Chars(), kstidXmlErrorMsg011);
	Assert(wsUser);
	// Get the index for the field id.
	bool fFlidOk;
	int ifldMsg;
	fFlidOk = m_pfwxd->MapFidToIndex(flid, &ifldMsg);
	Assert(fFlidOk);
	// Generate minimal formatting information for the string.
	Vector<FwXml::BasicRunInfo> vbri;
	FwXml::BasicRunInfo bri = {0, 0};
	vbri.Push(bri);
	Vector<FwXml::RunPropInfo> vrpi;
	FwXml::RunPropInfo rpi;
	rpi.m_ctip = 1;
	rpi.m_ctsp = 0;
	rpi.m_vbRawProps.Resize(32);
	DataWriterRgb dwr(rpi.m_vbRawProps.Begin(), rpi.m_vbRawProps.Size());
	TextProps::TextIntProp txip;
	txip.m_scp = kscpWs;
	txip.m_nVal = wsUser;
	txip.m_nVar = 0;
	TextProps::WriteTextIntProp(&dwr, &txip);
	rpi.m_vbRawProps.Resize(dwr.IbCur());		// Shrink to fit.
	vrpi.Push(rpi);
	StoreMultiStringData(hobjOwner, ifldMsg, wsUser, stuMsg, vbri, vrpi);
}

/************************************************************************
 * NAME
 *    strrpbrk
 * ARGUMENTS
 *    psz - address of NUL-terminated character string
 *    pszSet - address of NUL-terminated set of characters to search for
 * DESCRIPTION
 *    strrpbrk() searches the NUL-terminated string psz for occurrences of
 *    characters from the NUL-terminated string pszSet.  The second argument
 *    is regarded as a set of characters; the order of the characters, or
 *    whether there are duplications, does not matter.  If such a character
 *    is found within psz, then a pointer to the last such character is
 *    returned.  If no character within psz occurs in pszSet, then a null
 *    character pointer (NULL) is returned.  See also strpbrk(), which
 *    searches for the first character in psz that is also in pszSet.
 * RETURN VALUE
 *    address of the last occurrence in psz of any character from pszSet,
 *    or NULL if no character from pszSet occurs in psz
 */
const char * strrpbrk(const char * psz, const char * pszSet)
{
	const char * pch;
	char * pszRet;
	char ch;

	if (!psz || !pszSet)
		return NULL;

	for (pszRet = NULL; *psz; ++psz)
	{
		for (pch = pszSet, ch = *psz; *pch; ++pch)
		{
			if (*pch == ch)
			{
				pszRet = (char *)psz;
				break;
			}
		}
	}
	return pszRet;
}


/*----------------------------------------------------------------------------------------------
	Set the FieldWorks base path member variable from the stored registry setting.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetFwDataPath()
{
	if (!m_strbpFwDataPath.Length())
		m_strbpFwDataPath.Assign(DirectoryFinder::FwRootDataDir().Chars());
}


/*----------------------------------------------------------------------------------------------
	Given the original and internal file paths, create a new CmFile object in the CmFolder
	named "Local Pictures" in the Pictures field of the LangProject.

	@param fid Either kflidCmPicture_PictureFile or (probably) kflidCmMedia_MediaFile
	@param pszPathname pathname of the picture/media file
	@return database id of the new CmFile object
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::CreateCmFileForLangProject(int fid, const char * pszPathname)
{
	int hobjFolder;
	if (fid == kflidCmPicture_PictureFile)
		hobjFolder = FindOrCreateFolder(kflidLangProject_Pictures, "Local Pictures");
	else
		hobjFolder = FindOrCreateFolder(kflidLangProject_Media, "Local Media");
	if (hobjFolder == 0)
		ThrowHr(WarnHr(E_FAIL));

	int hobj = GenerateNextNewHvo();
	GUID guid;
	GenerateNewGuid(&guid);
	StrAnsi staCmd;
	if(CURRENTDB == FB) {
		staCmd.Format("EXECUTE PROCEDURE CreateObject$ %<0>u, %<1>u, '%<2>g';%n"
			"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u WHERE Id=%<1>u;%n"
			"UPDATE CmFile SET InternalPath=? WHERE [Id]=%<1>u;",
			kclidCmFile, hobj, &guid, hobjFolder, kflidCmFolder_Files);
	}
	if(CURRENTDB == MSSQL) {
		staCmd.Format("EXEC CreateObject$ %<0>u, %<1>u, '%<2>g';%n"
			"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u WHERE Id=%<1>u;%n"
			"UPDATE CmFile SET InternalPath=? WHERE [Id]=%<1>u;",
			kclidCmFile, hobj, &guid, hobjFolder, kflidCmFolder_Files);
	}
	ExecuteParameterizedSQL(staCmd.Chars(), __LINE__, pszPathname);
	return hobj;
}


/*----------------------------------------------------------------------------------------------
	Find or create the folder in the given field which has the given name.

	@param flid id of field that contains folders
	@param pszFolderName name of the desired folder
	@return database id of the folder, either previously existing or newly created
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::FindOrCreateFolder(int flid, const char * pszFolderName)
{
	int ifld;
	if (!m_pfwxd->MapFidToIndex(flid, &ifld))
	{
		// "Unknown flid for folders: %<0>d"
		ThrowWithLogMessage(kstidXmlErrorMsg310);
	}
	int icls = GetClassIndexFromFieldIndex(ifld);
	StrAnsi staCmd;
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd.Format("SELECT p.Dst "
			"FROM %<0>S_%<1>S p "
			"JOIN CmFolder_Name n ON n.Obj = p.Dst AND n.Txt = ?;",
			m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars());
	}
	bool fIsNull;
	int hobj = ReadOneIntFromParameterizedSQL(staCmd.Chars(), __LINE__, pszFolderName, fIsNull);
	if (!fIsNull)
		return hobj;

	// We have to create the folder.
	// First get the database id of the language project.
	int hobjLP;
	const char * pszTable = (m_pfwxd->DbVersion() <= 200202) ? "LanguageProject" : "LangProject";
	if(CURRENTDB == FB)
	{
		staCmd.Format("SELECT FIRST 1 Id FROM %s;", pszTable);
	}
	if(CURRENTDB == MSSQL)
	{
		staCmd.Format("SELECT TOP 1 [Id] FROM %s;", pszTable);
	}
	hobjLP = ReadOneIntFromSQL(staCmd.Chars(), __LINE__, fIsNull);
	Assert(!fIsNull);

	// Create a CmFolder database object
	hobj = GenerateNextNewHvo();
	int wsUser = GetUserWs();
	GUID guid;
	GenerateNewGuid(&guid);
	if(CURRENTDB == FB) {
		staCmd.Format("EXECUTE PROCEDURE CreateObject$ %<0>u, %<1>u, '%<2>g';%n"
			"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u WHERE Id=%<1>u;%n"
			"INSERT INTO CmFolder_Name (Obj, Ws, Txt) VALUES (%<1>u, %<5>u, ?);",
			kclidCmFolder, hobj, &guid, hobjLP, flid, wsUser);
	}
	if(CURRENTDB == MSSQL) {
		staCmd.Format("EXEC CreateObject$ %<0>u, %<1>u, '%<2>g';%n"
			"UPDATE CmObject SET Owner$=%<3>u, OwnFlid$=%<4>u WHERE Id=%<1>u;%n"
			"INSERT INTO CmFolder_Name (Obj, Ws, Txt) VALUES (%<1>u, %<5>u, ?);",
			kclidCmFolder, hobj, &guid, hobjLP, flid, wsUser);
	}
	ExecuteParameterizedSQL(staCmd.Chars(), __LINE__, pszFolderName);

	return hobj;
}

/*----------------------------------------------------------------------------------------------
	Store the contents of m_setExtraWsUsed in LangProject_AnalysisWss and
	LangProject_CurAnalysisWss.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::AddExtraAnalysisWss()
{
	bool fIsNull;
	int hvoLP;
	StrAnsi sta;
	int nDbVer = m_pfwxd->DbVersion();
	const char * pszTable = (nDbVer <= 200202) ? "LanguageProject" : "LangProject";
	if(CURRENTDB == FB)
	{
		sta.Format("SELECT FIRST 1 Id FROM %s;", pszTable);
	}
	if(CURRENTDB == MSSQL)
	{
		sta.Format("SELECT TOP 1 Id FROM %s;", pszTable);
	}
	hvoLP = ReadOneIntFromSQL(sta.Chars(), __LINE__, fIsNull);
	Assert(!fIsNull);

	int nOrd;
	const char * pszCurAnalysisWss = (nDbVer <= 200202) ? "LanguageProject_CurrentAnalysisWritingSystems"
		: "LangProject_CurAnalysisWss";
	const char * pszAnalysisWss = (nDbVer <= 200202) ? "LanguageProject_AnalysisWritingSystems"
		: "LangProject_AnalysisWss";
	sta.Format("SELECT MAX(Ord) FROM %s;", pszCurAnalysisWss);
	if(CURRENTDB == FB || CURRENTDB == MSSQL)
	{
		nOrd = ReadOneIntFromSQL(sta.Chars(),__LINE__, fIsNull);
	}
	Assert(!fIsNull);
	StrAnsi staCmd;
	Set<int>::iterator it;
	m_staCmd.Clear();
	for (it = m_setExtraWsUsed.Begin(); it != m_setExtraWsUsed.End(); ++it)
	{
		int hvoWs = it->GetValue();
		if (CURRENTDB == FB || CURRENTDB == MSSQL)
		{
			staCmd.Format("SELECT COUNT(*) FROM %s WHERE Dst=%u;", pszAnalysisWss, hvoWs);
		}
		int fAlready = ReadOneIntFromSQL(staCmd.Chars(), __LINE__, fIsNull);
		Assert(!fIsNull);
		if (fAlready == 0)
		{
			if (CURRENTDB == FB || CURRENTDB == MSSQL)
			{
				m_staCmd.FormatAppend("INSERT INTO %<0>s (Src,Dst) VALUES (%<1>u,%<2>u);%n",
					pszAnalysisWss, hvoLP, hvoWs);
			}
		}
		if (CURRENTDB == FB || CURRENTDB == MSSQL)
		{
			staCmd.Format(
				"SELECT COUNT(*) FROM %s WHERE Dst=%u;", pszCurAnalysisWss, hvoWs);
		}
		fAlready = ReadOneIntFromSQL(staCmd.Chars(), __LINE__, fIsNull);
		Assert(!fIsNull);
		if (fAlready == 0)
		{
			if (CURRENTDB == FB || CURRENTDB == MSSQL)
			{
				m_staCmd.FormatAppend("INSERT INTO %<0>s (Src,Dst,Ord) VALUES (%<1>u,%<2>u,%<3>u);%n",
					pszCurAnalysisWss, hvoLP, hvoWs, ++nOrd);
			}
		}
	}
	if (m_staCmd.Length() > 0)
		ExecuteSimpleSQL(m_staCmd.Chars(), __LINE__);
}

/*----------------------------------------------------------------------------------------------
	Load the database from the given XML file.  The database must be initialized but empty.
	This is an IFwXmlData interface method.

	@param bstrFile Name of the input XML file.

	@return S_OK, E_INVALIDARG, E_UNEXPECTED, E_OUTOFMEMORY, E_FAIL, or possibly another
					COM error code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwXmlData::LoadXml(BSTR bstrFile, IAdvInd * padvi)
{
	//fprintf(stdout,"begin ::LoadXml\n");
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrFile);
	ChkComArgPtrN(padvi);
	Assert(m_sdb.IsOpen());
	if (!m_sdb.IsOpen())
		ThrowHr(E_UNEXPECTED);
	FwXmlImportData xid(this);

	time_t timBegin = time(0);
	// Moved this from CreateCustomFields() and FixCUstomListRefFields() in attempt to fix
	// intermittent bizarre build bugs on the build machines.

	xid.SetSingleUserDb(true);

	// Initialize the ICU setup.  (Needed for Unicode normalization)
	StrUtil::InitIcuDataDir();

	// Open the input file and create the log file.
	STATSTG statFile;
	xid.OpenFiles(bstrFile, &statFile);

	// Set the toplevel element information for the parser to use.
	xid.InitializeTopElementInfo(-1, 0, -1, L"FwDatabase", 0, GUID_NULL, 0);

	// Verify that the database is empty.
	xid.EnsureEmptyDatabase();

	// Allow creating objects with full information.
	//IDENTITY_INSERT is taken care of in T_BI0_CmObject
	/*if(CURRENTDB == FB) {
		xid.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject ON;", __LINE__);
	}*/
	if(CURRENTDB == MSSQL) {
		xid.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject ON;", __LINE__);
	}

	// Process the XML file (Pass One of Two).
	xid.SetOuterHandlers(FwXmlImportData::HandleStartTag1, FwXmlImportData::HandleEndTag1);
	xid.ParseXmlPhaseOne(m_vstufld.Size(), statFile, padvi);// E_UNEXPECTED in here
	//fprintf(stdout,"after ParseXmlPhaseOne\n");

	time_t timMid = time(0);
	long timDelta = (long)(timMid - timBegin);
	xid.ReportFirstPassTime(timDelta);
	// possible firebird syntax errors here, no custom fields to create right now, so no problem
	xid.CreateCustomFields();
	//fprintf(stdout,"after CreateCustomFields\n");
	xid.CreateObjects(padvi);
	//fprintf(stdout,"after CreateObjects\n");
	xid.FixCustomListRefFields();
	if (xid.CustomCount())
		xid.ReportCustomFields();
	time_t timMid1 = time(0);
	timDelta = (long)(timMid1 - timMid);
	xid.ReportCreatingObjects(timDelta);
	time_t timMid2 = timMid1;

	// Process the XML file (Pass Two of Two).

	xid.SetOuterHandlers(FwXmlImportData::HandleStartTag2, FwXmlImportData::HandleEndTag2);
	xid.ParseXmlPhaseTwo(m_vstufld.Size(), statFile, padvi);
	//fprintf(stdout,"AFTER XML PHASE TWO\n");
	time_t timMid3 = time(0);
	timDelta = (long)(timMid3 - timMid2);
	xid.ReportSecondPassTime(timDelta);
	xid.StoreData(padvi);

	// Add empty structured text objects as needed, fix dates, colors, and null StStyle rules.
	xid.CreateEmptyTextFields();
	xid.UpdateDttm();
	xid.SetCmPossibilityColors();
	xid.FixStStyle_Rules();
	if (padvi)
		padvi->Step(1);		// now at 100%

	// Restore normal insertion state for objects.
	//IDENTITY_INSERT is taken care of in T_BI0_CmObject
	/*if(CURRENTDB == FB) {
		xid.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject OFF;", __LINE__);
	}*/
	if(CURRENTDB == MSSQL) {
		xid.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject OFF;", __LINE__);
	}

	// Moved this from CreateCustomFields() and FixCUstomListRefFields() in attempt to fix
	// intermittent bizarre build bugs on the build machines.
	xid.SetSingleUserDb(false);

	timMid1 = time(0);
	timDelta = (long)(timMid1 - timMid3);
	xid.ReportDataStorageStats(timDelta);
	timDelta = (long)(time(0) - timBegin);
	xid.ReportTotalTime(timDelta);

	END_COM_METHOD(s_fact, IID_IFwXmlData);
}


/*----------------------------------------------------------------------------------------------
	Load an object into the database from the given XML file.  The database must be initialized,
	with the owning object already existing.  The field given by flid must either be empty, or
	a sequence/collection type field.  This is an IFwXmlData2 interface method.

	@param bstrFile Name of the input XML file.
	@param hvoOwner Database id of the object's owner.
	@param flid Field id of the object.
	@param padvi Pointer to progress bar interface (may be NULL).

	@return S_OK, E_INVALIDARG, E_UNEXPECTED, E_OUTOFMEMORY, E_FAIL, or possibly another
					COM error code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwXmlData::ImportXmlObject(BSTR bstrFile, int hvoOwner, int flid, IAdvInd * padvi)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrFile);
	ChkComArgPtrN(padvi);
	if (!hvoOwner || !flid)
		ThrowHr(E_INVALIDARG);
	Assert(m_sdb.IsOpen());
	if (!m_sdb.IsOpen())
		ThrowHr(E_UNEXPECTED);

	FwXmlImportData xid(this);
	time_t timBegin = time(0);

	// Initialize the ICU setup.  (Needed for Unicode normalization)
	StrUtil::InitIcuDataDir();

	// Open the input file and create the log file.
	STATSTG statFile;
	xid.OpenFiles(bstrFile, &statFile);

	// Set the toplevel element information for the parser to use, first calculating a few of
	// the values we need.
	int ifld;
	int icls;
	xid.EnsureValidImportArguments(hvoOwner, flid, ifld, icls);
	int hvoObj = 0;
	GUID guidObj = GUID_NULL;
	if (FieldInfo(ifld).cpt == kcptOwningAtom)
		xid.LoadPossibleExistingObjectId(hvoOwner, flid, hvoObj, guidObj);
	int hvoMin = xid.GetNextRealObjectId();
	xid.InitializeTopElementInfo(hvoOwner, flid, icls, FieldXmlName(ifld).Chars(),
		hvoObj, guidObj, hvoMin);
	xid.AllowNewWritingSystems();

	// Load some data we need from the database.  Allow creating objects with full information.
	xid.MapGuidsToHobjs();
	xid.MapIcuLocalesToWsHobjs();
	//IDENTITY_INSERT is taken care of in T_BI0_CmObject
	/*if(CURRENTDB == FB) {
		xid.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject ON;", __LINE__);
	}*/
	if(CURRENTDB == MSSQL) {
		xid.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject ON;", __LINE__);
	}

	// Process the XML file (Pass One of Two).

	xid.SetOuterHandlers(FwXmlImportData::ImportStartTag1, FwXmlImportData::HandleEndTag1);
	xid.ParseXmlPhaseOne(m_vstufld.Size(), statFile, padvi);

	time_t timMid = time(0);
	long timDelta = (long)(timMid - timBegin);
	xid.ReportFirstPassTime(timDelta);
	xid.CreateObjects(padvi);
	time_t timMid1 = time(0);
	timDelta = (long)(timMid1 - timMid);
	xid.ReportCreatingObjects(timDelta);
	time_t timMid2 = timMid1;

	// Process the XML file (Pass Two of Two).

	xid.SetOuterHandlers(FwXmlImportData::ImportStartTag2, FwXmlImportData::HandleEndTag2);
	xid.ParseXmlPhaseTwo(m_vstufld.Size(), statFile, padvi);

	time_t timMid3 = time(0);
	timDelta = (long)(timMid3 - timMid2);
	xid.ReportSecondPassTime(timDelta);
	xid.StoreData(padvi);

	// Fix LexSense objects to always have an appropriate MSA attached to them.
	// We don't want to add MSAs to all entries because variants typically don't have senses.
	xid.FixLexSenseMSAs();
	// Add empty structured text objects as needed, fix dates, fix colors, and fix null StStyle
	// Rules.
	xid.CreateEmptyTextFields();
	xid.UpdateDttm();
	xid.SetCmPossibilityColors();
	xid.FixStStyle_Rules();
	xid.AddExtraAnalysisWss();

	if (padvi)
		padvi->Step(1);		// now at 100%

	// Restore normal insertion state for objects.
	//IDENTITY_INSERT is taken care of in T_BI0_CmObject
	/*if(CURRENTDB == FB) {
		xid.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject OFF;", __LINE__);
	}*/
	if(CURRENTDB == MSSQL) {
		xid.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject OFF;", __LINE__);
	}
	timMid1 = time(0);
	xid.LogRepeatedMessages();
	timDelta = (long)(timMid1 - timMid3);
	xid.ReportDataStorageStats(timDelta);
	timDelta = (long)(time(0) - timBegin);
	xid.ReportTotalTime(timDelta);

	END_COM_METHOD(s_fact, IID_IFwXmlData);
}


/*----------------------------------------------------------------------------------------------
	Load possibly multiple objects into possibly multiple lists in the database from the given
	XML file.  The database must be initialized, with the owning object already existing.  The
	fields specified in bstrFlidLists must all be sequence/collection type fields.  This is an
	IFwXmlData2 interface method.

	@param bstrFile Name of the input XML file.
	@param hvoOwner Database id of the object's owner.
	@param padvi Pointer to progress bar interface (may be NULL).

	@return S_OK, E_INVALIDARG, E_UNEXPECTED, E_OUTOFMEMORY, E_FAIL, or possibly another
					COM error code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwXmlData::ImportMultipleXmlFields(BSTR bstrFile, int hvoOwner, IAdvInd * padvi)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrFile);
	ChkComArgPtrN(padvi);
	if (!hvoOwner)
		ThrowHr(E_INVALIDARG);
	Assert(m_sdb.IsOpen());
	if (!m_sdb.IsOpen())
		ThrowHr(E_UNEXPECTED);

	FwXmlImportData xid(this);
	time_t timBegin = time(0);

	// Initialize the ICU setup.  (Needed for Unicode normalization)
	StrUtil::InitIcuDataDir();

	// Open the input file and create the log file.
	STATSTG statFile;
	xid.OpenFiles(bstrFile, &statFile);

	// Set the toplevel element information for the parser to use, first calculating a few of
	// the values we need.
	int icls = xid.GetClassIndexOfObject(hvoOwner);
	if (icls < 0)
		ReturnHr(E_INVALIDARG);
	int hvoMin = xid.GetNextRealObjectId();
	xid.InitializeTopElementInfo(hvoOwner, 0, icls, ClassName(icls).Chars(),
		0, GUID_NULL, hvoMin);
	xid.InitializeForMerging(hvoOwner, icls, hvoMin);

	// Load some data we need from the database.  Allow creating objects with full information.
	xid.MapGuidsToHobjs();
	xid.MapIcuLocalesToWsHobjs();
	//IDENTITY_INSERT is taken care of in T_BI0_CmObject
	/*if(CURRENTDB == FB) {
		xid.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject ON;", __LINE__);
	}*/
	if(CURRENTDB == MSSQL) {
		xid.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject ON;", __LINE__);
	}
	// Process the XML file (Pass One of Two).

	xid.SetOuterHandlers(FwXmlImportData::ImportStartTag1, FwXmlImportData::HandleEndTag1);
	xid.ParseXmlPhaseOne(m_vstufld.Size(), statFile, padvi);

	time_t timMid = time(0);
	long timDelta = (long)(timMid - timBegin);
	xid.ReportFirstPassTime(timDelta);
	xid.CreateObjects(padvi);
	time_t timMid1 = time(0);
	timDelta = (long)(timMid1 - timMid);
	xid.ReportCreatingObjects(timDelta);
	time_t timMid2 = timMid1;

	// Process the XML file (Pass Two of Two).

	xid.SetOuterHandlers(FwXmlImportData::ImportStartTag2, FwXmlImportData::HandleEndTag2);
	xid.ParseXmlPhaseTwo(m_vstufld.Size(), statFile, padvi);

	time_t timMid3 = time(0);
	timDelta = (long)(timMid3 - timMid2);
	xid.ReportSecondPassTime(timDelta);
	xid.StoreData(padvi);

	// Fix LexEntry (and LexSense) objects to always have an appropriate MSA attached to them.
	xid.FixLexSenseMSAs();
	// Add empty structured text objects as needed, fix dates, fix colors, and fix null StStyle
	// Rules.
	xid.CreateEmptyTextFields();
	xid.UpdateDttm();
	xid.SetCmPossibilityColors();
	xid.FixStStyle_Rules();

	if (padvi)
		padvi->Step(1);		// now at 100%

	// Restore normal insertion state for objects.
	//IDENTITY_INSERT is taken care of in T_BI0_CmObject
	/*if(CURRENTDB == FB) {
		xid.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject OFF;", __LINE__);
	}*/
	if(CURRENTDB == MSSQL) {
		xid.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject OFF;", __LINE__);
	}
	timMid1 = time(0);
	xid.LogRepeatedMessages();
	timDelta = (long)(timMid1 - timMid3);
	xid.ReportDataStorageStats(timDelta);
	timDelta = (long)(time(0) - timBegin);
	xid.ReportTotalTime(timDelta);

	END_COM_METHOD(s_fact, IID_IFwXmlData);
}


/*----------------------------------------------------------------------------------------------
	Constructor.

	@param pfwxd Pointer to an FwXmlData object that encapsulates the database we are wanting
				to fill with imported data.
----------------------------------------------------------------------------------------------*/
FwXmlUpdateData::FwXmlUpdateData(FwXmlData * pfwxd)
	: FwXmlImportData(pfwxd)
{
	m_cobjNew = 0;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwXmlUpdateData::~FwXmlUpdateData()
{
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements during the first pass.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ProcessStartTag1(const XML_Char * pszName, const XML_Char ** prgpszAtts)
{
	StrAnsi sta;
	StrAnsi staFmt;
	if (m_celemStart == 0)
	{
		if (m_staBeginTag != pszName)
		{
			if (m_staOwnerBeginTag == pszName)
			{
				++m_celemStart;
				return;
			}
			m_fError = true;
			m_hr = E_INVALIDARG;
			// "Invalid start tag <%<0>s> for XML file: expected <%<1>s>.\n"
			staFmt.Load(kstidXmlErrorMsg311);
			sta.Format(staFmt.Chars(), pszName, m_staBeginTag.Chars());
			LogMessage(sta.Chars());
			ThrowHr(WarnHr(m_hr));
		}
	}
	++m_celemStart;
	StrAnsiBuf stabCustom;
	try
	{
		ElemTypeInfo eti = GetElementType(pszName);
		if (!m_vetiOpen.Size() && m_staBeginTag == pszName)
		{
			if (eti.m_elty == keltyPropName)
			{
				// Store information needed for sequence properties.
				PushSeqPropInfo(eti);
				// Flag as outermost element, but keep the field index for later use.
				eti.m_elty = keltyDatabase;
				m_vetiOpen.Push(eti);
				return;
			}
			else if (eti.m_elty == keltyObject && m_flid == 0)
			{
				// Flag as outermost element, but keep the class index for later use.
				ElemTypeInfo eti2;
				eti2.m_elty = keltyDatabase;
				eti2.m_icls = eti.m_icls;
				m_vetiOpen.Push(eti2);
				// Store the object information.
				m_vetiOpen.Push(eti);
				m_vhobjOpen.Push(m_hvoOwner);
				return;
			}
			// We're sure to blow up somewhere below if we haven't returned by now...
		}
		switch (eti.m_elty)
		{
		case keltyObject:
			StartObject1(eti, pszName, prgpszAtts);
			break;
		case keltyPropName:
			StartPropName1(eti, pszName);
			break;
		case keltyBasicProp:
			StartBasicProp1(eti, pszName);
			break;
		case keltyUpdateMerge:
			StartMergeElem1(eti, pszName, prgpszAtts);
			break;
		case keltyUpdateDelete:
			StartDeleteElem1(eti, pszName, prgpszAtts);
			break;
		default:	// includes keltyDatabase, keltyAddProps, and keltyDefineProp
			// "Unknown XML start tag: \"%s\""
			ThrowWithLogMessage(kstidXmlErrorMsg128, pszName);
			break;
		}
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		staFmt.Load(kstidXmlDebugMsg003);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		staFmt.Load(kstidXmlDebugMsg005);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML end elements during the first pass.

	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ProcessEndTag1(const XML_Char * pszName)
{
	++m_celemEnd;
	ElemTypeInfo eti;
	if (!m_vetiOpen.Pop(&eti))
	{
		if (m_iclsOwner != -1 && m_staOwnerBeginTag == pszName)
		{
			// If importing an object, ignore an outermost object element that matches the
			// owner's class.
			return;
		}
		// THIS SHOULD NEVER HAPPEN! -- "Unbalanced XML element stack!?"
		SetErrorWithMessage(kstidXmlErrorMsg123);
	}
	Assert(!m_fSingle);
	switch (eti.m_elty)
	{
	case keltyDatabase:
		Assert(!m_vetiOpen.Size());
		break;
	case keltyObject:
		if (!m_vhobjOpen.Pop())
		{
			// THIS SHOULD NEVER HAPPEN! -- "Unbalanced object id stack!?"
			SetErrorWithMessage(kstidXmlErrorMsg125);
		}
		break;
	case keltyPropName:
		if (!m_vspiOpen.Pop())
		{
			// THIS SHOULD NEVER HAPPEN! -- "Unbalanced property name stack!?"
			SetErrorWithMessage(kstidXmlErrorMsg126);
		}
		break;
	case keltyBasicProp:
		Assert(m_vetiOpen.Size() >= 3);
		Assert(!m_fIcuLocale);
		Assert(!m_fInRuleProp);
		break;
	case keltyUpdateMerge:
	case keltyUpdateDelete:
		// Nothing more to do with these elements.
		break;
	default:
		// THIS SHOULD NEVER HAPPEN! -- "INTERNAL XML ELEMENT STACK CORRUPTED!?"
		SetErrorWithMessage(kstidXmlErrorMsg041);
		break;
	}
	// If importing objects, pop off the extra element pushed for the start tag.
	if (m_iclsOwner != -1 && m_vetiOpen.Size() == 1 && m_staBeginTag == pszName)
	{
		m_vetiOpen.Pop(&eti);
		Assert(eti.m_elty == keltyDatabase);
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML character data during the first pass.

	@param prgch Pointer to an array of character data; not NUL-terminated.
	@param cch Number of characters (bytes) in prgch.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ProcessCharData1(const XML_Char * prgch, int cch)
{
	// We don't do anything with character data in pass 1.
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements during the second pass.

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ProcessStartTag2(const XML_Char * pszName, const XML_Char ** prgpszAtts)
{
	if (m_celemStart == 0 && m_iclsOwner != -1 && m_staOwnerBeginTag == pszName)
	{
		// If importing an object, ignore an outermost object element that matches the
		// owner's class.
		++m_celemStart;
		return;
	}
	++m_celemStart;
	StrAnsiBuf stabCmd;
	StrAnsi sta;
	StrAnsi staFmt;
	try
	{
		ElemTypeInfo eti = GetElementType(pszName);
		if (!m_vetiOpen.Size() && m_staBeginTag == pszName)
		{
			if (eti.m_elty == keltyPropName)
			{
				// Flag as outermost element, but keep the field index for later use.
				eti.m_elty = keltyDatabase;
				m_vetiOpen.Push(eti);
				return;
			}
			else if (eti.m_elty == keltyObject && m_flid == 0)
			{
				// Flag as outermost element, but keep the class index for later use.
				ElemTypeInfo eti2;
				eti2.m_elty = keltyDatabase;
				eti2.m_icls = eti.m_icls;
				m_vetiOpen.Push(eti2);
				// Store the object information.
				m_vetiOpen.Push(eti);
				m_vhobjOpen.Push(m_hvoOwner);
				return;
			}
			// We're sure to blow up somewhere below if we haven't returned by now...
		}
		switch (eti.m_elty)
		{
		case keltyObject:
			StartObject2(eti, pszName);
			break;
		case keltyPropName:
			StartPropName2(eti);
			break;
		case keltyBasicProp:
			StartBasicProp2(eti, pszName, prgpszAtts, stabCmd);
			break;
		case keltyUpdateMerge:
		case keltyUpdateDelete:
			m_vetiOpen.Push(eti);	// Nothing else to do with these on pass 2.
			break;
		default:	// includes keltyDatabase, keltyAddProps, and keltyDefineProp
			// THIS SHOULD NEVER HAPPEN! -- "Unknown XML start tag: \"%s\""
			ThrowWithLogMessage(kstidXmlErrorMsg128, pszName);
			break;
		}
		BatchSqlCommand(stabCmd);
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		staFmt.Load(kstidXmlDebugMsg003);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		staFmt.Load(kstidXmlDebugMsg005);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML end elements during the second pass.

	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ProcessEndTag2(const XML_Char * pszName)
{
	try
	{
		++m_celemEnd;
		ElemTypeInfo eti;
		int hobj;
		if (!m_vetiOpen.Pop(&eti))
		{
			if (m_iclsOwner != -1 && m_staOwnerBeginTag == pszName)
			{
				// If importing an object, ignore an outermost object element that matches the
				// owner's class.
				return;
			}
			// THIS SHOULD NEVER HAPPEN! -- "Unbalanced element stack!?"
			ThrowWithLogMessage(kstidXmlErrorMsg124);
		}
#ifdef DEBUG
		ElemTypeInfo etiEnd = GetElementType(pszName);
		Assert(etiEnd.m_elty == eti.m_elty || (m_flid && eti.m_elty == keltyDatabase));
		Assert(etiEnd.m_icls == eti.m_icls || eti.m_elty == keltyCustomProp);
#endif
		switch (eti.m_elty)
		{
		case keltyDatabase:
			// REVIEW SteveMc: is there anything to do here?
			break;
		case keltyObject:
			if (!m_vhobjOpen.Pop(&hobj))
			{
				// THIS SHOULD NEVER HAPPEN! -- "Unbalanced object id stack!?"
				ThrowWithLogMessage(kstidXmlErrorMsg125);
			}
			break;
		case keltyPropName:
		case keltyUpdateMerge:
		case keltyUpdateDelete:
			// Nothing more to do with these elements.
			break;
		case keltyBasicProp:
			ProcessBasicPropPass2(pszName, eti);
			break;
		default:
			// THIS SHOULD NEVER HAPPEN!
			// "Unknown XML end tag: \"%s\""
			ThrowWithLogMessage(kstidXmlErrorMsg127, pszName);
			break;
		}
		// If importing objects, pop off the extra element pushed for the start tag.
		if (m_iclsOwner != -1 && m_vetiOpen.Size() == 1 && m_staBeginTag == pszName)
		{
			m_vetiOpen.Pop(&eti);
			Assert(eti.m_elty == keltyDatabase);
		}
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		StrAnsi staFmt(kstidXmlDebugMsg003);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		StrAnsi staFmt(kstidXmlDebugMsg005);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}


/*----------------------------------------------------------------------------------------------
	Calculate the type of XML element we have here.  Update has four special elements that other
	FieldWorks XML files don't have.  Handle those here, then pass off to the base method.

	@param pszElement XML element name read from the input file.

	@return Xml element type: basic, custom, object, etc.
----------------------------------------------------------------------------------------------*/
ElemTypeInfo FwXmlUpdateData::GetElementType(const char * pszElement)
{
	AssertPsz(pszElement);
	ElemTypeInfo eti;
	if (m_hmceti.Retrieve(pszElement, &eti))
	{
		return eti;
	}
	else if (!strcmp(pszElement, "MergedItems") || !strcmp(pszElement, "Merge"))
	{
		eti.m_elty = keltyUpdateMerge;
	}
	else if (!strcmp(pszElement, "DeletedItems") || !strcmp(pszElement, "Delete"))
	{
		eti.m_elty = keltyUpdateDelete;
	}
	else
	{
		return FwXmlImportData::GetElementType(pszElement);
	}
	eti.m_icls = -1;
	m_hmceti.Insert(pszElement, eti);
	return eti;
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements for objects during the first pass.

	@param eti Reference to the basic element type information structure.
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::StartObject1(const ElemTypeInfo & eti, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	UpdateObjInfo uoi;
	const ElemTypeInfo & etiProp = CheckObjectNesting(pszName);
	const FwDbClassInfo & fdci = m_pfwxd->ClassInfo(eti.m_icls);
	const FwDbFieldInfo & fdfi = m_pfwxd->FieldInfo(etiProp.m_ifld);
	uoi.m_hvoOwner = GetOwnerOfCurrentObject(etiProp, pszName);
	uoi.m_cidObj = fdci.cid;
	uoi.m_nDepth = (m_vetiOpen.Size() / 2);
	uoi.m_cidOwner = fdfi.cid;
	uoi.m_fidOwner = fdfi.fid;
	if (fdfi.cpt == kcptOwningSequence)
	{
		Assert(m_vspiOpen.Size());
		uoi.m_ordRel = m_vspiOpen.Top()->m_ord;
	}
	else
	{
		uoi.m_ordRel = -1;
	}
	uoi.m_cptRel = fdfi.cpt;
	// Parse the XML ID string, and get the GUID if it is GUID-based.
	uoi.m_guidObj = GUID_NULL;
	uoi.m_hvoObj = GetObjectIdAndGuid(prgpszAtts, uoi);

	int iuoi = m_vuoiRevised.Size();
	m_vuoiRevised.Push(uoi);
	m_hmguidiuoiRevised.Insert(uoi.m_guidObj, iuoi);
	m_hmhvoiuoiRevised.Insert(uoi.m_hvoObj, iuoi);
	m_vetiOpen.Push(eti);
	m_vhobjOpen.Push(uoi.m_hvoObj);
	if (uoi.m_hvoOwner < 0)
	{
		PushRootObjectInfo(uoi.m_hvoObj, uoi.m_guidObj, eti.m_icls);
	}
	else
	{
		PushOwnedObjectInfo(uoi.m_hvoObj, uoi.m_guidObj, eti.m_icls, uoi.m_hvoOwner,
			etiProp.m_ifld, pszName, prgpszAtts);
	}
	++m_cobj;
}

/*----------------------------------------------------------------------------------------------
	Get an object's id string, and calculate its guid from that if possible.  If it doesn't have
	an id string, try to match it against a corresponding object in the current data.  If that
	fails, generate a new database id and guid.  Store the mappings from guid to hobj, and from
	the id string to hobj.

	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param uoi reference to the UpdateObjInfo struct, which includes the database id and guid

	@return database id of the object
----------------------------------------------------------------------------------------------*/
int FwXmlUpdateData::GetObjectIdAndGuid(const XML_Char ** prgpszAtts, UpdateObjInfo & uoi)
{
	if (m_hvoObj && m_vetiOpen.Size() == 1 &&
		m_pfwxd->FieldInfo(m_vetiOpen[0].m_ifld).fid == m_flid)
	{
		// The object already exists.
		uoi.m_guidObj = m_guidObj;
		return m_hvoObj;
	}
	const char * pszId = FwXml::GetAttributeValue(prgpszAtts, "id");
	int hobj;
	StrAnsiBufSmall stabs;
	if (pszId != NULL)
	{
		if (FwXml::ParseGuid(pszId + 1, &uoi.m_guidObj))
		{
			if (!m_hmguidhobj.Retrieve(uoi.m_guidObj, &hobj))
			{
				// We have a valid GUID, generate the hobj, and store the mappings.
				hobj = m_cobjNew + m_hvoMin;
				++m_cobjNew;
				m_hmguidhobj.Insert(uoi.m_guidObj, hobj);
			}
			if (!m_chGuid)
			{
				m_chGuid = *pszId;
				m_cchGuid = 1;
			}
			else if (m_chGuid != *pszId)
			{
				if (m_cchGuid == 1)
				{
					// "WARNING: GUID-based id strings do not all begin with the same letter!"
					StrAnsi sta(kstidXmlErrorMsg130);
					LogMessage(sta.Chars());
				}
				++m_cchGuid;
			}
			return hobj;
		}
	}
	else
	{
		// No ID: manufacture one.
		stabs.Format(":%d", m_celemStart);
		pszId = stabs.Chars();
	}
	if (m_hmcidhobj.Retrieve(pszId, &hobj))
	{
		// "Repeated object ID"
		ThrowWithLogMessage(kstidXmlErrorMsg098);
	}
	// Match against an existing object if possible.
	hobj = FindMatchingObject(uoi);
	if (hobj == 0)
	{
		// Generate a guid and hobj, store the mapping from id to hobj.
		GenerateNewGuid(&uoi.m_guidObj);
		hobj = m_cobjNew + m_hvoMin;
		++m_cobjNew;
	}
	m_hmcidhobj.Insert(pszId, hobj);
	return hobj;
}


/*----------------------------------------------------------------------------------------------
	Try to match the given new object against a corresponding object in the current data.

	@param uoi reference to the UpdateObjInfo struct, which includes the database id and guid

	@return database id of the object, or 0 if no match is found.
----------------------------------------------------------------------------------------------*/
int FwXmlUpdateData::FindMatchingObject(UpdateObjInfo & uoi)
{
	int iOldOwner;
	if (!m_hmhvoiuoiOrig.Retrieve(uoi.m_hvoOwner, &iOldOwner))
		return 0;
	int iOldLim = FindLimitOfOwner(m_vuoiOrig, iOldOwner, m_hmhvoiuoiOrig);
	for (int i = iOldOwner + 1; i < iOldLim; ++i)
	{
		const UpdateObjInfo & uoiOld = m_vuoiOrig[i];
		if (uoi.m_cidObj == uoiOld.m_cidObj &&
			uoi.m_hvoOwner == uoiOld.m_hvoOwner &&
			uoi.m_cidOwner == uoiOld.m_cidOwner &&
			uoi.m_fidOwner == uoiOld.m_fidOwner &&
			uoi.m_ordRel == uoiOld.m_ordRel &&
			uoi.m_cptRel == uoiOld.m_cptRel)
		{
			uoi.m_hvoObj = uoiOld.m_hvoObj;
			uoi.m_guidObj = uoiOld.m_guidObj;
			return uoi.m_hvoObj;
		}
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Find the limit in the vector of objects owned directly or indirectly by the given object.

	@param vuoi reference to a vector of UpdateObjInfo structs
	@param iOwner index into vuoi of the owning object of interest
	@param hmhvoiuoi reference to a hashmap from database id to index into vuoi

	@return database id of the object, or 0 if no match is found.
----------------------------------------------------------------------------------------------*/
int FwXmlUpdateData::FindLimitOfOwner(Vector<UpdateObjInfo> & vuoi, int iOwner,
	HashMap<int,int> & hmhvoiuoi)
{
	int hvoOwner = vuoi[iOwner].m_hvoObj;
	for (int i = iOwner + 1; i < vuoi.Size(); ++i)
	{
		int hvo = vuoi[i].m_hvoOwner;
		if (hvo == hvoOwner)
			continue;
		else if (!IsOwnedBy(hvo, hvoOwner, vuoi, hmhvoiuoi, kclidCmPossibilityList))
			return i;
	}
	return vuoi.Size();
}

/*----------------------------------------------------------------------------------------------
	Check whether the given object is owned directly or indirectly by the given owner.  Stop
	looking when an object of the given class is found.

	@param hvoObj database id of the object
	@param hvoOwner database id of a desired owner
	@param vuoi reference to a vector of UpdateObjInfo structs
	@param hmhvoiuoi reference to a hashmap from database id to index into vuoi
	@param cidParent

	@return database id of the object, or 0 if no match is found.
----------------------------------------------------------------------------------------------*/
bool FwXmlUpdateData::IsOwnedBy(int hvoObj, int hvoOwner, Vector<UpdateObjInfo> & vuoi,
	HashMap<int,int> & hmhvoiuoi, int cidParent)
{
	int iuoi;
	if (!hmhvoiuoi.Retrieve(hvoObj, &iuoi))
		return false;
	int hvo = vuoi[iuoi].m_hvoOwner;
	if (hvo == hvoOwner)
		return true;
	else if (hvo == -1)
		return false;
	else if (vuoi[iuoi].m_cidOwner == cidParent)
		return false;
	else
		return IsOwnedBy(hvo, hvoOwner, vuoi, hmhvoiuoi, cidParent);
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements for object properties during the first pass.

	@param eti Reference to the basic element type information structure.
	@param pszName XML element name read from the input file.
	@param pszProp custom field name (default is NULL)
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::StartPropName1(ElemTypeInfo & eti, const XML_Char * pszName,
	const char * pszProp)
{
	CheckPropNameNesting(pszName);
	Assert(!pszProp);
	m_vetiOpen.Push(eti);
	PushSeqPropInfo(eti);
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements for basic type properties during the first pass.

	@param eti Reference to the basic element type information structure.
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::StartBasicProp1(const ElemTypeInfo & eti, const XML_Char * pszName)
{
	const ElemTypeInfo & etiProp = CheckBasicPropNesting(pszName);
	m_vetiOpen.Push(eti);
	if (eti.m_cpt == kcptString || eti.m_cpt == kcptMultiString)
	{
		XML_SetElementHandler(m_parser, FwXmlImportData::HandleStringStartTag1,
			FwXmlImportData::HandleStringEndTag1);
	}
	m_vcfld[etiProp.m_ifld]++;
}


/*----------------------------------------------------------------------------------------------
	Handle XML start elements for merge items during the first pass.

	@param eti Reference to the basic element type information structure.
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::StartMergeElem1(const ElemTypeInfo & eti, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	CheckUpdatePropNesting(pszName);
	m_vetiOpen.Push(eti);
	if (!strcmp(pszName, "MergedItems"))
		return;
	Assert(!strcmp(pszName, "Merge"));
	const char * pszFrom = FwXml::GetAttributeValue(prgpszAtts, "from");
	if (pszFrom == NULL)
	{
		// "Missing %<0>s attribute for %<1>s element!"
		ThrowWithLogMessage(kstidXmlErrorMsg079, "from", pszName);
	}
	UpdateMergeInfo umi;
	if (!FwXml::ParseGuid(pszFrom + 1, &umi.m_guidFrom))
	{
		// "Invalid %<0>s attribute value for %<1>s element: not GUID-based."
		ThrowWithLogMessage(kstidXmlErrorMsg315, "from", pszName);
	}
	if (!m_hmguidhobj.Retrieve(umi.m_guidFrom, &umi.m_hvoFrom))
	{
		// "Invalid %<0>s attribute value for %<1>s element: bad GUID value."
		ThrowWithLogMessage(kstidXmlErrorMsg316, pszName);
	}
	const char * pszTo = FwXml::GetAttributeValue(prgpszAtts, "to");
	if (pszTo == NULL)
	{
		// "Missing %<0>s attribute for %<1>s element!"
		ThrowWithLogMessage(kstidXmlErrorMsg079, "to", pszName);
	}
	if (!FwXml::ParseGuid(pszTo + 1, &umi.m_guidTo))
	{
		// "Invalid %<0>s attribute value for %<1>s element: not GUID-based."
		ThrowWithLogMessage(kstidXmlErrorMsg315, "to", pszName);
	}
	if (!m_hmguidhobj.Retrieve(umi.m_guidTo, &umi.m_hvoTo))
	{
		umi.m_hvoTo = 0;	// GUID must be for a new list item.
	}
	int iumi = m_vumi.Size();
	m_vumi.Push(umi);
	m_hmguidiumi.Insert(umi.m_guidFrom, iumi);
	m_hmhvoFromhvoTo.Insert(umi.m_hvoFrom, umi.m_hvoTo);
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements for delete items during the first pass.

	@param eti Reference to the basic element type information structure.
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::StartDeleteElem1(const ElemTypeInfo & eti, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	CheckUpdatePropNesting(pszName);
	m_vetiOpen.Push(eti);
	if (!strcmp(pszName, "DeletedItems"))
		return;
	Assert(!strcmp(pszName, "Delete"));
	const char * pszId = FwXml::GetAttributeValue(prgpszAtts, "id");
	if (pszId == NULL)
	{
		// "Missing %<0>s attribute for %<1>s element!"
		ThrowWithLogMessage(kstidXmlErrorMsg079, "id", pszName);
	}
	UpdateDeleteInfo udi;
	if (!FwXml::ParseGuid(pszId + 1, &udi.m_guid))
	{
		// "Invalid %<0>s attribute value for %<1>s element: not GUID-based."
		ThrowWithLogMessage(kstidXmlErrorMsg315, "id", pszName);
	}
	if (!m_hmguidhobj.Retrieve(udi.m_guid, &udi.m_hvo))
	{
		// "Invalid %<0>s attribute value for %<1>s element: bad GUID value."
		ThrowWithLogMessage(kstidXmlErrorMsg316, "id", pszName);
	}
	int iudi = m_vudi.Size();
	m_vudi.Push(udi);
	m_hmguidiudi.Insert(udi.m_guid, iudi);
}

/*----------------------------------------------------------------------------------------------
	Check the nesting for XML start elements for special update items during the first pass.

	@param pszName XML element name read from the input file.

	@return ElemTypeInfo value for the containing property
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::CheckUpdatePropNesting(const XML_Char * pszName)
{
	// Check for proper nesting.
	if (!m_vetiOpen.Size())
	{
		// "<%<0>s> must be nested inside <%<1>s>...</%<1>s>!"
		ThrowWithLogMessage(kstidXmlErrorMsg004, pszName, m_staBeginTag.Chars());
	}
	const ElemTypeInfo & etiOuter = m_vetiOpen[m_vetiOpen.Size() - 1];
	if (!strcmp(pszName, "MergedItems") || !strcmp(pszName, "DeletedItems"))
	{
		if (etiOuter.m_elty != keltyDatabase || m_vetiOpen.Size() != 1)
		{
#if 99-99
			ShowElemTypeStack();
#endif
			// "<%<0>s> must be nested inside <%<1>s>...</%<1>s>!"
			ThrowWithLogMessage(kstidXmlErrorMsg004, pszName, m_staBeginTag.Chars());
		}
	}
	else if (!strcmp(pszName, "Merge"))
	{
		if (etiOuter.m_elty != keltyUpdateMerge || m_vetiOpen.Size() != 2)
		{
#if 99-99
			ShowElemTypeStack();
#endif
			// "<%<0>s> must be nested inside <%<1>s>...</%<1>s>!"
			ThrowWithLogMessage(kstidXmlErrorMsg004, pszName, "MergedItems");
		}
	}
	else
	{
		Assert(!strcmp(pszName, "Delete"));
		if (etiOuter.m_elty != keltyUpdateDelete || m_vetiOpen.Size() != 2)
		{
#if 99-99
			ShowElemTypeStack();
#endif
			// "<%<0>s> must be nested inside <%<1>s>...</%<1>s>!"
			ThrowWithLogMessage(kstidXmlErrorMsg004, pszName, "DeletedItems");
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Load the basic information for all objects owned by the designated object, which is
	typically a list.

	@param hvoOwner Database id of the object's owner.
	@param flid Field id of the object.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::InitializeForUpdate(int hvoOwner, int flid)
{
	StrAnsi staCmd;
	bool fIsNull;
	int nDbVer = m_pfwxd->DbVersion();

	if(CURRENTDB == FB) {
		staCmd.Format("SELECT FIRST 1 Id FROM CmObject WHERE Owner$=%<0>d AND OwnFlid$=%<1>d;",
			hvoOwner, flid);
	}
	else if(CURRENTDB == MSSQL) {
		staCmd.Format("SELECT TOP 1 Id FROM CmObject WHERE Owner$=%<0>d AND OwnFlid$=%<1>d;",
			hvoOwner, flid);
	}
	m_hvoList = ReadOneIntFromSQL(staCmd.Chars(), __LINE__, fIsNull);
	if (fIsNull)
		return;		// We're updating something that doesn't even exist yet!

	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd.Format("SELECT ItemClsId FROM CmPossibilityList WHERE Id=%<0>d;", m_hvoList);
	}
	m_cidItem = ReadOneIntFromSQL(staCmd.Chars(), __LINE__, fIsNull);
	Assert(!fIsNull);

	//TODO (steve miller): temp tables in Firebird
	if(CURRENTDB == FB) {
		staCmd.Format("CREATE TABLE TObjInfoTbl$%n"
			"(%n"
			"   ObjId           INT     NOT NULL,%n"
			"   ObjClass        INT     NULL,%n"
			"   InheritDepth    INT     NULL        DEFAULT(0),%n"
			"   OwnerDepth      INT     NULL        DEFAULT(0),%n"
			"   RelObjId        INT     NULL,%n"
			"   RelObjClass     INT     NULL,%n"
			"   RelObjField     INT     NULL,%n"
			"   RelOrder        INT     NULL,%n"
			"   RelType         INT     NULL,%n"
			"   OrdKey			BLOB	NULL        DEFAULT(0)%n"
			");%n"
			"CREATE NONCLUSTERED INDEX tObjInfoTblObjId ON tObjInfoTbl$ (ObjId);%n");
		staCmd.FormatAppend(nDbVer < 200236 ?
			"EXECUTE PROCEDURE GetLinkedObjs$ %<0>d, null, %<1>d, 0, 0, 1, 0, NULL, 1;%n" :
			"EXECUTE PROCEDURE GetLinkedObjs$ '%<0>d', %<1>d, 0, 0, 1, 0, NULL, 1;%n",
			m_hvoList, kgrfcptOwning);
		staCmd.FormatAppend(
			"SELECT o.ObjId, co.Guid$, o.ObjClass, o.OwnerDepth,%n"
			"   o.RelObjId, o.RelObjClass, o.RelObjField, o.RelOrder, o.RelType%n"
			"FROM TObjInfoTbl$ o%n"
			"JOIN CmObject co ON co.Id=o.ObjId%n"
			"ORDER BY o.OrdKey;%n"
			"DELETE FROM TObjInfoTbl$;%n");
		staCmd.FormatAppend(nDbVer < 200236 ?
			"EXECUTE PROCEDURE GetLinkedObjs$ %<0>d, null, %<1>d, 0, 0, 1, 0, NULL, 1;%n" :
			"EXECUTE PROCEDURE GetLinkedObjs$ '%<0>d', %<1>d, 0, 0, 1, 0, NULL, 0;%n",
			m_hvoList, kgrfcptReference);
		staCmd.FormatAppend(
			"SELECT DISTINCT o.ObjId,o.RelObjId,o.RelObjField,o.RelOrder,o.RelType%n"
			"FROM TObjInfoTbl$ o%n"
			"ORDER BY o.RelObjId;%n"
			"DROP TABLE TObjInfoTbl$;%n");
	}
	else if(CURRENTDB == MSSQL) {
		staCmd.Format("CREATE TABLE #ObjInfoTbl$%n"
			"(%n"
			"   ObjId           INT     NOT NULL,%n"
			"   ObjClass        INT     NULL,%n"
			"   InheritDepth    INT     NULL        DEFAULT(0),%n"
			"   OwnerDepth      INT     NULL        DEFAULT(0),%n"
			"   RelObjId        INT     NULL,%n"
			"   RelObjClass     INT     NULL,%n"
			"   RelObjField     INT     NULL,%n"
			"   RelOrder        INT     NULL,%n"
			"   RelType         INT     NULL,%n"
			"   OrdKey  VARBINARY(250)  NULL        DEFAULT(0)%n"
			")%n"
			"CREATE NONCLUSTERED INDEX #ObjInfoTblObjId ON #ObjInfoTbl$ (ObjId)%n");
		staCmd.FormatAppend(nDbVer < 200236 ?
			"EXEC GetLinkedObjs$ %<0>d, null, %<1>d, 0, 0, 1, 0, NULL, 1%n" :
			"EXEC GetLinkedObjs$ '%<0>d', %<1>d, 0, 0, 1, 0, NULL, 1%n",
			m_hvoList, kgrfcptOwning);
		staCmd.FormatAppend(
			"SELECT o.ObjId,co.Guid$, o.ObjClass,o.OwnerDepth,%n"
			"   o.RelObjId,o.RelObjClass,o.RelObjField,o.RelOrder,o.RelType%n"
			"FROM #ObjInfoTbl$ o%n"
			"JOIN CmObject co on co.Id=o.ObjId%n"
			"ORDER BY o.OrdKey%n"
			"DELETE FROM #ObjInfoTbl$%n");
		staCmd.FormatAppend(nDbVer < 200236 ?
			"EXEC GetLinkedObjs$ %<0>d, null, %<1>d, 0, 0, 1, 0, NULL, 1%n" :
			"EXEC GetLinkedObjs$ '%<0>d', %<1>d, 0, 0, 1, 0, NULL, 0%n",
			m_hvoList, kgrfcptReference);
		staCmd.FormatAppend(
			"SELECT DISTINCT o.ObjId,o.RelObjId,o.RelObjField,o.RelOrder,o.RelType%n"
			"FROM #ObjInfoTbl$ o%n"
			"ORDER BY o.RelObjId%n"
			"DROP TABLE #ObjInfoTbl$%n;");
	}
	SqlStatement sstmt;
	sstmt.Init(m_sdb);
	// Note that this command retrieves TWO (2) row sets, with a row count between them.
	RETCODE rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, staCmd.Chars());

	// Read the first row set.
	StoreUpdateObjInfo(sstmt.Hstmt());
	// Try for the second row set, first getting any row counts that appear between the
	// row sets.
	rc = SQLMoreResults(sstmt.Hstmt());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	while (rc == SQL_SUCCESS || rc == SQL_SUCCESS_WITH_INFO)
	{
		// Skip past the row count info generated by the GetLinkedObjs$ stored procedure.
		SQLINTEGER crows = 0;
		rc = SQLRowCount(sstmt.Hstmt(), &crows);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (crows == -1)
			break;
		rc = SQLMoreResults(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	}
	if (rc == SQL_SUCCESS || rc == SQL_SUCCESS_WITH_INFO)
	{
		// Read the second row set.
		StoreUpdateLinkInfo(sstmt.Hstmt());
	}
	sstmt.Clear();
}

/*----------------------------------------------------------------------------------------------
	Store the data retrieved for the UpdateObjInfo table.

	@param hstmt handle to an ODBC SQL statement object
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::StoreUpdateObjInfo(SQLHSTMT hstmt)
{
	m_vuoiOrig.Clear();
	m_vuoiRevised.Clear();
	m_vuli.Clear();
	m_vumi.Clear();
	m_vudi.Clear();
	m_hmguidiuoiOrig.Clear();
	m_hmhvoiuoiOrig.Clear();
	m_hmguidiuoiRevised.Clear();
	m_hmhvoiuoiRevised.Clear();

	UpdateObjInfo uoi;
	SDWORD cbT;
	RETCODE rc;
	for (;;)
	{
		rc = SQLFetch(hstmt);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS && rc != SQL_SUCCESS_WITH_INFO)
			ThrowHr(WarnHr(E_UNEXPECTED));
		rc = SQLGetData(hstmt, 1, SQL_C_SLONG, &uoi.m_hvoObj, sizeof(uoi.m_hvoObj), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			continue;
		rc = SQLGetData(hstmt, 2, SQL_C_GUID, &uoi.m_guidObj, sizeof(uoi.m_guidObj), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			continue;
		rc = SQLGetData(hstmt, 3, SQL_C_SLONG, &uoi.m_cidObj, sizeof(uoi.m_cidObj), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			continue;
		rc = SQLGetData(hstmt, 4, SQL_C_SLONG, &uoi.m_nDepth, sizeof(uoi.m_nDepth), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			continue;
		rc = SQLGetData(hstmt, 5, SQL_C_SLONG, &uoi.m_hvoOwner, sizeof(uoi.m_hvoOwner), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			uoi.m_hvoOwner = -1;	// NULL is a possible result for this column in first row.
		rc = SQLGetData(hstmt, 6, SQL_C_SLONG, &uoi.m_cidOwner, sizeof(uoi.m_cidOwner), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			uoi.m_cidOwner = -1;	// NULL is a possible result for this column in first row.
		rc = SQLGetData(hstmt, 7, SQL_C_SLONG, &uoi.m_fidOwner, sizeof(uoi.m_fidOwner), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			uoi.m_fidOwner = -1;	// NULL is a possible result for this column in first row.
		rc = SQLGetData(hstmt, 8, SQL_C_SLONG, &uoi.m_ordRel, sizeof(uoi.m_ordRel), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			uoi.m_ordRel = -1;		// NULL is a likely result for this column.
		rc = SQLGetData(hstmt, 9, SQL_C_SLONG, &uoi.m_cptRel, sizeof(uoi.m_cptRel), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			uoi.m_cptRel = -1;		// NULL is a possible result for this column in first row.
		int iuoi = m_vuoiOrig.Size();
		m_vuoiOrig.Push(uoi);
		m_hmguidiuoiOrig.Insert(uoi.m_guidObj, iuoi);
		m_hmhvoiuoiOrig.Insert(uoi.m_hvoObj, iuoi);
	}
}

/*----------------------------------------------------------------------------------------------
	Store the data retrieved for the UpdateLinkInfo table.

	@param hstmt handle to an ODBC SQL statement object
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::StoreUpdateLinkInfo(SQLHSTMT hstmt)
{
	UpdateLinkInfo uli;
	SDWORD cbT;
	RETCODE rc;
	for (;;)
	{
		rc = SQLFetch(hstmt);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS && rc != SQL_SUCCESS_WITH_INFO)
			ThrowHr(WarnHr(E_UNEXPECTED));
		rc = SQLGetData(hstmt, 1, SQL_C_SLONG, &uli.m_hvoDst, sizeof(uli.m_hvoDst), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			continue;
		rc = SQLGetData(hstmt, 2, SQL_C_SLONG, &uli.m_hvoSrc, sizeof(uli.m_hvoSrc), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			continue;
		rc = SQLGetData(hstmt, 3, SQL_C_SLONG, &uli.m_fidSrc, sizeof(uli.m_fidSrc), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			continue;
		rc = SQLGetData(hstmt, 4, SQL_C_SLONG, &uli.m_ordSrc, sizeof(uli.m_ordSrc), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			uli.m_ordSrc = -1;		// NULL is a likely result for this column.
		rc = SQLGetData(hstmt, 5, SQL_C_SLONG, &uli.m_cptSrc, sizeof(uli.m_cptSrc), &cbT);
		VerifySqlRc(rc, hstmt, __LINE__);
		if (cbT == SQL_NULL_DATA)
			continue;
		m_vuli.Push(uli);
	}
}

/*----------------------------------------------------------------------------------------------
	Get the ListItem objects from m_vuoiOrig and m_vuoiRevised.  Also fill in the table of
	custom list objects (m_vuoiCustom).
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ProcessUpdateObjectInfo()
{
	m_vuoiCustom.Clear();
	m_hmguidiuoiCustom.Clear();
	m_viuoiItemOrig.Clear();
	m_viuoiItemRevised.Clear();
#if 99
	m_viuoiItemNew.Clear();
#endif

	for (int i = 0; i < m_vuoiOrig.Size(); ++i)
	{
		UpdateObjInfo & uoi = m_vuoiOrig[i];
		if (uoi.m_cidObj == m_cidItem)
		{
			m_viuoiItemOrig.Push(i);
			// Check whether this item is a known item, or a custom item created by the user.
			int idx;
			if (m_hmguidiuoiRevised.Retrieve(uoi.m_guidObj, &idx))
				continue;
			if (m_hmguidiumi.Retrieve(uoi.m_guidObj, &idx))
				continue;
			if (m_hmguidiudi.Retrieve(uoi.m_guidObj, &idx))
				continue;
			// It must be a custom item.
			idx = m_vuoiCustom.Size();
			m_vuoiCustom.Push(uoi);
			m_hmguidiuoiCustom.Insert(uoi.m_guidObj, idx);
			m_hmhvoiuoiCustom.Insert(uoi.m_hvoObj, idx);
		}
	}
	for (int i = 0; i < m_vuoiRevised.Size(); ++i)
	{
		UpdateObjInfo & uoi = m_vuoiRevised[i];
		if (uoi.m_cidObj == m_cidItem)
		{
			m_viuoiItemRevised.Push(i);
			// Check whether this item is a new item.
			int idx;
			if (m_hmguidiuoiOrig.Retrieve(uoi.m_guidObj, &idx))
				continue;
			if (m_hmguidiumi.Retrieve(uoi.m_guidObj, &idx))
			{
				// "Object id being merged (%g) should not appear in the updated list."
				ThrowWithLogMessage(kstidXmlErrorMsg317, &uoi.m_guidObj);
			}
			else if (m_hmguidiudi.Retrieve(uoi.m_guidObj, &idx))
			{
				// "Object id being deleted (%g) should not appear in the updated list."
				ThrowWithLogMessage(kstidXmlErrorMsg318, &uoi.m_guidObj);
			}
#if 99
			// It must be a new item.
			m_viuoiItemNew.Push(i);
#endif
		}
	}

}

/*----------------------------------------------------------------------------------------------
	Find a valid owner, either the given one or its owner if the given one is being deleted.

	@param hvoOwner Old owner, which may or may not be getting deleted.
	@param pfidOwner Pointer to the old OwnFlid$ value, which may need to change as well
	@param pcidOwner Pointer to the old owner's Class$ value, which may need to change as well
	@param pcptRel Pointer to the old Owner's OwnFlid$'s type, which may need to change as well
----------------------------------------------------------------------------------------------*/
int FwXmlUpdateData::FindValidOwner(int hvoOwner, int * pfidOwner, int * pcidOwner,
	int * pcptRel)
{
	for (int i = 0; i < m_vumi.Size(); ++i)
	{
		if (hvoOwner == m_vumi[i].m_hvoFrom)
		{
			int iuoi;
			if (m_hmguidiuoiOrig.Retrieve(m_vumi[i].m_guidFrom, &iuoi))
			{
				*pfidOwner = m_vuoiOrig[iuoi].m_fidOwner;
				*pcidOwner = m_vuoiOrig[iuoi].m_cidOwner;
				*pcptRel = m_vuoiOrig[iuoi].m_cptRel;
				return FindValidOwner(m_vuoiOrig[iuoi].m_hvoOwner, pfidOwner, pcidOwner,
					pcptRel);
			}
			// crash, burn?
			break;
		}
	}
	for (int i = 0; i < m_vudi.Size(); ++i)
	{
		if (hvoOwner == m_vudi[i].m_hvo)
		{
			int iuoi;
			if (m_hmguidiuoiOrig.Retrieve(m_vudi[i].m_guid, &iuoi))
			{
				*pfidOwner = m_vuoiOrig[iuoi].m_fidOwner;
				*pcidOwner = m_vuoiOrig[iuoi].m_cidOwner;
				*pcptRel = m_vuoiOrig[iuoi].m_cptRel;
				return FindValidOwner(m_vuoiOrig[iuoi].m_hvoOwner, pfidOwner, pcidOwner,
					pcptRel);
			}
			// crash, burn?
			break;
		}
	}
	return hvoOwner;	// Current owner is okay.
}

/*----------------------------------------------------------------------------------------------
	The ord values for existing objects may have been changed to something out of the way to
	facilitate moving the ownership around.  Reset those which need it to the values they should
	have.

	@param viuoiChg Reference to a vector of indexes into m_vuoiRevised
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::AdjustStandardOrdValues(Vector<int> &viuoiChg)
{
	// Fix the Ord values for the standard list items.
	StrAnsi staCmd;
	for (int i = 0; i < viuoiChg.Size(); ++i)
	{
		UpdateObjInfo & uoiChg = m_vuoiRevised[ viuoiChg[i] ];
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			staCmd.Format("UPDATE CmObject SET OwnOrd$=%<0>u WHERE [Id]=%<1>u;",
				uoiChg.m_ordRel, uoiChg.m_hvoObj);
		}
		ExecuteSimpleSQL(staCmd.Chars(), __LINE__);
	}
}

/*----------------------------------------------------------------------------------------------
	The custom items have been assigned bogus ord values.  Fix them to something reasonable,
	both in the memory vector and in the database.

	@param viuoiCustomChg Reference to a vector of indexes into m_vuoiCustom
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::AdjustCustomItemOrdValues(Vector<int> & viuoiCustomChg)
{
	StrAnsi staCmd;
	for (int i = 0; i < viuoiCustomChg.Size(); ++i)
	{
		UpdateObjInfo & uoi = m_vuoiCustom[ viuoiCustomChg[i] ];
		// Calculate an ord value based on the content of m_vuoiRevised and m_vuoiCustom.
		// These linear searches aren't very efficient, but they shouldn't be used that often...
		int nOrdMax = 0;
		for (int i2 = 0; i2 < m_viuoiItemRevised.Size(); ++i2)
		{
			UpdateObjInfo & uoiRevised = m_vuoiRevised[ m_viuoiItemRevised[i2] ];
			Assert(uoiRevised.m_hvoObj != uoi.m_hvoObj);
			Assert(uoiRevised.m_guidObj != uoi.m_guidObj);
			Assert(uoiRevised.m_cidObj == uoi.m_cidObj);
			if (uoiRevised.m_hvoOwner != uoi.m_hvoOwner ||
				uoiRevised.m_fidOwner != uoi.m_fidOwner)
			{
				continue;
			}
			Assert(uoiRevised.m_cidOwner == uoi.m_cidOwner);
			Assert(uoiRevised.m_cptRel == uoi.m_cptRel);
			if (nOrdMax < uoiRevised.m_ordRel)
				nOrdMax = uoiRevised.m_ordRel;
		}
		// Just in case multiple custom items have the same owner...
		for (int i2 = 0; i2 < m_vuoiCustom.Size(); ++i2)
		{
			UpdateObjInfo & uoiCustom = m_vuoiCustom[i2];
			if (uoiCustom == uoi)
				break;
			if (uoiCustom.m_hvoOwner != uoi.m_hvoOwner ||
				uoiCustom.m_fidOwner != uoi.m_fidOwner)
			{
				continue;
			}
			Assert(uoiCustom.m_hvoObj != uoi.m_hvoObj);
			Assert(uoiCustom.m_guidObj != uoi.m_guidObj);
			Assert(uoiCustom.m_cidObj == uoi.m_cidObj);
			Assert(uoiCustom.m_cidOwner == uoi.m_cidOwner);
			Assert(uoiCustom.m_cptRel == uoi.m_cptRel);
			if (nOrdMax < uoiCustom.m_ordRel)
				nOrdMax = uoiCustom.m_ordRel;
		}
		uoi.m_ordRel = nOrdMax + 1;
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			staCmd.Format("UPDATE CmObject SET OwnOrd$=%<0>u WHERE [Id]=%<1>u;",
				uoi.m_ordRel, uoi.m_hvoObj);
		}
		ExecuteSimpleSQL(staCmd.Chars(), __LINE__);
	}
}

/*----------------------------------------------------------------------------------------------
	Set the owner, flid, and ord for new items.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::FinishCreatingNewListItems()
{
	StrAnsi staCmd;
	for (int i = 0; i < m_vuoiRevised.Size(); ++i)
	{
		UpdateObjInfo & uoi = m_vuoiRevised[i];
		if (uoi.m_hvoObj >= m_hvoMin)
		{
			if(CURRENTDB == FB || CURRENTDB == MSSQL) {
				staCmd.Format("UPDATE CmObject SET Owner$=%<0>u, OwnFlid$=%<1>u, OwnOrd$=",
					uoi.m_hvoOwner, uoi.m_fidOwner);
				if (uoi.m_cptRel == kcptOwningSequence)
					staCmd.FormatAppend("%<0>u", uoi.m_ordRel);
				else
					staCmd.Append("NULL");
				staCmd.FormatAppend(" WHERE Id=%<0>u;", uoi.m_hvoObj);
			}
			ExecuteSimpleSQL(staCmd.Chars(), __LINE__);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	If the owner or ord of an existing item changes in the update, change it in the database.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::UpdateOwnerAndOrd()
{
	// First, change owners, adjusting ord if needed to avoid conflicts.
	Vector<int> viuoiChg;
	Vector<int> viuoiCustomChg;
	StrAnsi staCmd;
	for (int i = 0; i < m_viuoiItemOrig.Size(); ++i)
	{
		UpdateObjInfo & uoiOrig = m_vuoiOrig[ m_viuoiItemOrig[i] ];
		int i2;
		int hvoOwnerNew = 0;
		int fidOwnerNew = 0;
		if (m_hmguidiuoiRevised.Retrieve(uoiOrig.m_guidObj, &i2))
		{
			UpdateObjInfo & uoiRevised = m_vuoiRevised[i2];
			if (uoiOrig.m_hvoOwner == uoiRevised.m_hvoOwner &&
				uoiOrig.m_ordRel == uoiRevised.m_ordRel)
			{
				continue;
			}
			Assert(uoiOrig.m_hvoObj == uoiRevised.m_hvoObj);
			Assert(uoiOrig.m_cidObj == uoiRevised.m_cidObj);
			Assert(uoiOrig.m_cptRel == uoiRevised.m_cptRel);
			if (uoiOrig.m_cptRel == kcptOwningSequence)
				viuoiChg.Push(i2);
			hvoOwnerNew = uoiRevised.m_hvoOwner;
			fidOwnerNew = uoiRevised.m_fidOwner;
		}
		else if (m_hmguidiudi.Retrieve(uoiOrig.m_guidObj, &i2))
		{
			// Being deleted, don't bother changing owner.
			hvoOwnerNew = uoiOrig.m_hvoOwner;
			fidOwnerNew = uoiOrig.m_fidOwner;
		}
		else if (m_hmguidiumi.Retrieve(uoiOrig.m_guidObj, &i2))
		{
			// Being merged, don't bother changing owner.
			hvoOwnerNew = uoiOrig.m_hvoOwner;
			fidOwnerNew = uoiOrig.m_fidOwner;
		}
		else if (m_hmguidiuoiCustom.Retrieve(uoiOrig.m_guidObj, &i2))
		{
			// custom field: retain field, but its owner may be getting deleted...
			fidOwnerNew = uoiOrig.m_fidOwner;
			int cidOwnerNew = uoiOrig.m_cidOwner;
			int cptRelNew = uoiOrig.m_cptRel;
			hvoOwnerNew = FindValidOwner(uoiOrig.m_hvoOwner, &fidOwnerNew, &cidOwnerNew,
				&cptRelNew);
			m_vuoiCustom[i2].m_hvoOwner = hvoOwnerNew;
			m_vuoiCustom[i2].m_fidOwner = fidOwnerNew;
			m_vuoiCustom[i2].m_cidOwner = cidOwnerNew;
			m_vuoiCustom[i2].m_cptRel = cptRelNew;
			if (uoiOrig.m_cptRel == kcptOwningSequence)
			{
				m_vuoiCustom[i2].m_ordRel =
					uoiOrig.m_hvoObj + m_vuoiOrig.Size() + m_vuoiRevised.Size();
			}
			if (uoiOrig.m_cptRel == kcptOwningSequence)
				viuoiCustomChg.Push(i2);
		}
		Assert(hvoOwnerNew);
		if (uoiOrig.m_cptRel == kcptOwningSequence)
		{
			int ordOwnerNew = uoiOrig.m_hvoObj + m_vuoiOrig.Size() + m_vuoiRevised.Size();
			if(CURRENTDB == FB || CURRENTDB == MSSQL) {
				staCmd.Format("UPDATE CmObject"
					" SET Owner$=%<0>u, OwnFlid$=%<1>u, OwnOrd$=%<2>u WHERE [Id]=%<3>u;",
					hvoOwnerNew, fidOwnerNew, ordOwnerNew, uoiOrig.m_hvoObj);
			}
		}
		else
		{
			if(CURRENTDB == FB || CURRENTDB == MSSQL) {
				staCmd.Format("UPDATE CmObject"
					" SET Owner$=%<0>u, OwnFlid$=%<1>u, OwnOrd$=NULL WHERE [Id]=%<2>u;",
					hvoOwnerNew, fidOwnerNew, uoiOrig.m_hvoObj);
			}
		}
		ExecuteSimpleSQL(staCmd.Chars(), __LINE__);
	}
	AdjustStandardOrdValues(viuoiChg);
	FinishCreatingNewListItems();	// fill in owner, flid, and ord
	AdjustCustomItemOrdValues(viuoiCustomChg);
}

/*----------------------------------------------------------------------------------------------
	Create the object specified by the UpdateObjInfo struct.

	@param uoi Reference to an UpdateObjInfo from m_vuoiRevised which specifies a new object.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::CreateNewListObjects()
{
	for (int i = 0; i < m_vuoiRevised.Size(); ++i)
	{
		UpdateObjInfo & uoi = m_vuoiRevised[i];
		if (uoi.m_hvoObj >= m_hvoMin)
		{
			StrAnsi staCmd;
			if(CURRENTDB == FB) {
				staCmd.Format("EXECUTE PROCEDURE CreateObject$ %<0>u, %<1>u, '%<2>g';",
					uoi.m_cidObj, uoi.m_hvoObj, &uoi.m_guidObj);
			}
			if(CURRENTDB == MSSQL) {
				staCmd.Format("EXEC CreateObject$ %<0>u, %<1>u, '%<2>g';",
					uoi.m_cidObj, uoi.m_hvoObj, &uoi.m_guidObj);
			}
			ExecuteSimpleSQL(staCmd.Chars(), __LINE__);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Use the information stored in m_vuli and m_vumi to update links in the database.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::FixLinksToMergedItems()
{
	StrAnsi staCmd;
	for (int i = 0; i < m_vuli.Size(); ++i)
	{
		UpdateLinkInfo & uli = m_vuli[i];
		int hvoTo;
		if (m_hmhvoFromhvoTo.Retrieve(uli.m_hvoDst, &hvoTo))
		{
			// We have an update to perform.
			int ifld = m_pfwxd->IndexOfFid(uli.m_fidSrc);
			const FwDbFieldInfo & fdfi = m_pfwxd->FieldInfo(ifld);
			int icls = m_pfwxd->IndexOfCid(fdfi.cid);
			if(CURRENTDB == FB || CURRENTDB == MSSQL) {
				staCmd.Format("UPDATE %<0>S_%<1>S SET Dst=%<2>u WHERE Src=%<3>u AND Dst=%<4>u;",
					m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
					hvoTo, uli.m_hvoSrc, uli.m_hvoDst);
			}
			ExecuteSimpleSQL(staCmd.Chars(), __LINE__);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Use the information stored in m_vudi and m_vumi to delete list items which have been
	merged or deleted.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::DeleteObsoleteItems(IAdvInd * padvi, int nPercent)
{
	time_t tim0 = time(0);
	m_vhobjDel.Clear();
	CollectDeletedObjects();
	time_t tim1 = time(0);
	// "Collecting obsolete list items to delete took %<0>d %<1>s."
	ReportTime((long)(tim1 - tim0), kstidXmlInfoMsg308);
	// "Need to delete %<0>d obsolete %<1>s."
	StrAnsi staFmt(kstidXmlInfoMsg309);
	StrAnsi staObj;
	// "object" / "objects"
	if (m_vhobjDel.Size() == 1)
		staObj.Load(kstidObject);
	else
		staObj.Load(kstidObjects);
	StrAnsi sta;
	sta.Format(staFmt.Chars(), m_vhobjDel.Size(), staObj.Chars());
	LogMessage(sta.Chars());
	if (m_vhobjDel.Size() == 0)
		return;

	DeleteObjects(m_vhobjDel, padvi, nPercent);
	time_t tim2 = time(0);
	// "Deleting obsolete list items took %<0>d %<1>s."
	ReportTime((long)(tim2 - tim1), kstidXmlInfoMsg306);
}

/*----------------------------------------------------------------------------------------------
	Find any objects that are effectively deleted in the revised list, and add them to
	m_vhobjDel.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::CollectDeletedObjects()
{
	Set<int> sethvoDel;
	for (int i = 0; i < m_vudi.Size(); ++i)
	{
		m_vhobjDel.Push(m_vudi[i].m_hvo);
		sethvoDel.Insert(m_vudi[i].m_hvo);
	}
	for (int i = 0; i < m_vumi.Size(); ++i)
	{
		m_vhobjDel.Push(m_vumi[i].m_hvoFrom);
		sethvoDel.Insert(m_vumi[i].m_hvoFrom);
	}
	for (int i = 0; i < m_vuoiOrig.Size(); ++i)
	{
		UpdateObjInfo & uoi = m_vuoiOrig[i];
		int iuoi;
		if (sethvoDel.IsMember(uoi.m_hvoObj))
			continue;
		if (m_hmhvoiuoiRevised.Retrieve(uoi.m_hvoObj, &iuoi))
			continue;
		if (m_hmguidiuoiCustom.Retrieve(uoi.m_guidObj, &iuoi))
		{
			// Skip everything owned by a custom object.
			int iLim = FindLimitOfOwner(m_vuoiOrig, i, m_hmhvoiuoiOrig);
			i = iLim - 1;	// allow for loop increment
			continue;
		}
		// We want to delete it, but don't add it to the set if its owner is being deleted.
		if (sethvoDel.IsMember(uoi.m_hvoOwner))
			continue;
		int hvoOwner = uoi.m_hvoOwner;
		int cidOwner = uoi.m_cidOwner;
		bool fSkip = false;
		while (cidOwner != m_cidItem && m_hmhvoiuoiOrig.Retrieve(hvoOwner, &iuoi))
		{
			hvoOwner = m_vuoiOrig[iuoi].m_hvoOwner;
			if (hvoOwner == -1)
				break;
			if (sethvoDel.IsMember(hvoOwner))
			{
				fSkip = true;
				break;
			}
			cidOwner = m_vuoiOrig[iuoi].m_cidOwner;
		}
		if (fSkip)
			continue;
		m_vhobjDel.Push(uoi.m_hvoObj);
		sethvoDel.Insert(uoi.m_hvoObj);
	}
}


/*----------------------------------------------------------------------------------------------
	Use the information stored in m_vuoiOrig, m_vuoiRevised, m_vuli, m_vumi, and m_vudi
	to merge, delete, and create objects, moving ownership around as necessary and updating
	links in the database as well as updating GUIDs and hvos in m_vuoiRevised.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::MergeDeleteAndCreateObjects(IAdvInd * padvi)
{
	time_t tim0 = time(0);
	// Fill in the arrays of new and custom list items.
	ProcessUpdateObjectInfo();
	time_t tim1 = time(0);
	// "Processing the update information took %<0>d %<1>s."
	ReportTime((long)(tim1 - tim0), kstidXmlInfoMsg302);

	// Create the new objects.
	CreateNewListObjects();
	time_t tim2 = time(0);
	// "Creating new objects took %<0>d %<1>s."
	ReportTime((long)(tim2 - tim1), kstidXmlInfoMsg303);
	if (padvi)
		padvi->Step(2);

	// Update the Owner$, OwnFlid$, and OwnOrd$ fields as needed to reflect the changes to the
	// list.
	UpdateOwnerAndOrd();
	time_t tim3 = time(0);
	// "Updating owners and sequence positions took %<0>d %<1>s."
	ReportTime((long)(tim3 - tim2), kstidXmlInfoMsg304);
	if (padvi)
		padvi->Step(2);

	// Fix the links for any items that are being merged.
	FixLinksToMergedItems();
	time_t tim4 = time(0);
	// "Fixing links to merged or deleted list items took %<0>d %<1>s."
	ReportTime((long)(tim4 - tim3), kstidXmlInfoMsg305);

	// Delete any items that are being deleted or merged.
	DeleteObsoleteItems(padvi, 30);		// Deleting obsolete objects = ~30%
	time_t tim5 = time(0);

	// Remove any obsolete data in various fields.
	RemoveObsoleteData(m_cidItem, padvi, 17);
	time_t tim6 = time(0);
	// "Removing possibly obsolete data from list items took %<0>d %<1>s."
	ReportTime((long)(tim6 - tim5), kstidXmlInfoMsg307);

#if 99-99
	DumpDebugInfo();
#endif
}


/*----------------------------------------------------------------------------------------------
	Dump internal data that is useful for developing/debugging the update methods.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::DumpDebugInfo()
{
#if 99-99
	FILE * fp = fopen("C:/FW/Src/Cellar/Test/UpdateTestData.xxx", "w");
	StrAnsi sta;

	fprintf(fp, "TABLE OF EXISTING LIST OBJECTS\n");
	for (int i = 0; i < m_vuoiOrig.Size(); ++i)
	{
		sta.Format("%5d {%g} %5d, %5d %5d %5d %7d %5d %3d",
			m_vuoiOrig[i].m_hvoObj, &m_vuoiOrig[i].m_guidObj, m_vuoiOrig[i].m_cidObj,
			m_vuoiOrig[i].m_nDepth, m_vuoiOrig[i].m_hvoOwner, m_vuoiOrig[i].m_cidOwner,
			m_vuoiOrig[i].m_fidOwner, m_vuoiOrig[i].m_ordRel, m_vuoiOrig[i].m_cptRel);
		fprintf(fp, "%5d] %s\n", i+1, sta.Chars());
	}

	fprintf(fp, "\nTABLE OF EXISTING LIST OBJECT LINKS\n");
	for (int i = 0; i < m_vuli.Size(); ++i)
	{
		sta.Format("%5d, %5d %7d %5d %3d",
			m_vuli[i].m_hvoDst,
			m_vuli[i].m_hvoSrc, m_vuli[i].m_fidSrc, m_vuli[i].m_ordSrc, m_vuli[i].m_cptSrc);
		fprintf(fp, "%5d] %s\n", i+1, sta.Chars());
	}

	fprintf(fp, "\nTABLE OF LIST OBJECTS TO MERGE\n");
	for (int i = 0; i < m_vumi.Size(); ++i)
	{
		sta.Format("%5d {%g}, %5d {%g}",
			m_vumi[i].m_hvoFrom, &m_vumi[i].m_guidFrom, m_vumi[i].m_hvoTo, &m_vumi[i].m_guidTo);
		fprintf(fp, "%5d] %s\n", i+1, sta.Chars());
	}

	fprintf(fp, "\nTABLE OF LIST OBJECTS TO DELETE\n");
	for (int i = 0; i < m_vudi.Size(); ++i)
	{
		sta.Format("%5d {%g}", m_vudi[i].m_hvo, &m_vudi[i].m_guid);
		fprintf(fp, "%5d] %s\n", i+1, sta.Chars());
	}

	fprintf(fp, "\nTABLE OF REVISED LIST OBJECTS\n");
	for (int i = 0; i < m_vuoiRevised.Size(); ++i)
	{
		sta.Format("%5d {%g} %5d, %5d %5d %5d %7d %5d %3d",
			m_vuoiRevised[i].m_hvoObj, &m_vuoiRevised[i].m_guidObj, m_vuoiRevised[i].m_cidObj,
			m_vuoiRevised[i].m_nDepth, m_vuoiRevised[i].m_hvoOwner, m_vuoiRevised[i].m_cidOwner,
			m_vuoiRevised[i].m_fidOwner, m_vuoiRevised[i].m_ordRel, m_vuoiRevised[i].m_cptRel);
		fprintf(fp, "%5d] %s\n", i+1, sta.Chars());
	}

	fprintf(fp, "\n(VIRTUAL) TABLE OF EXISTING LIST ITEMS\n");
	for (int i = 0; i < m_viuoiItemOrig.Size(); ++i)
	{
		int iuoi = m_viuoiItemOrig[i];
		sta.Format("%5d {%g} %5d, %5d %5d %5d %7d %5d %3d",
			m_vuoiOrig[iuoi].m_hvoObj, &m_vuoiOrig[iuoi].m_guidObj,
			m_vuoiOrig[iuoi].m_cidObj, m_vuoiOrig[iuoi].m_nDepth,
			m_vuoiOrig[iuoi].m_hvoOwner, m_vuoiOrig[iuoi].m_cidOwner,
			m_vuoiOrig[iuoi].m_fidOwner, m_vuoiOrig[iuoi].m_ordRel,
			m_vuoiOrig[iuoi].m_cptRel);
		fprintf(fp, "%5d] %s\n", i+1, sta.Chars());
	}

	fprintf(fp, "\n(VIRTUAL) TABLE OF REVISED LIST ITEMS\n");
	for (int i = 0; i < m_viuoiItemRevised.Size(); ++i)
	{
		int iuoi = m_viuoiItemRevised[i];
		sta.Format("%5d {%g} %5d, %5d %5d %5d %7d %5d %3d",
			m_vuoiRevised[iuoi].m_hvoObj, &m_vuoiRevised[iuoi].m_guidObj,
			m_vuoiRevised[iuoi].m_cidObj, m_vuoiRevised[iuoi].m_nDepth,
			m_vuoiRevised[iuoi].m_hvoOwner, m_vuoiRevised[iuoi].m_cidOwner,
			m_vuoiRevised[iuoi].m_fidOwner, m_vuoiRevised[iuoi].m_ordRel,
			m_vuoiRevised[iuoi].m_cptRel);
		fprintf(fp, "%5d] %s\n", i+1, sta.Chars());
	}

	fprintf(fp, "\nTABLE OF CUSTOM LIST ITEMS\n");
	for (int i = 0; i < m_vuoiCustom.Size(); ++i)
	{
		sta.Format("%5d {%g} %5d, %5d %5d %5d %7d %5d %3d",
			m_vuoiCustom[i].m_hvoObj, &m_vuoiCustom[i].m_guidObj, m_vuoiCustom[i].m_cidObj,
			m_vuoiCustom[i].m_nDepth, m_vuoiCustom[i].m_hvoOwner, m_vuoiCustom[i].m_cidOwner,
			m_vuoiCustom[i].m_fidOwner, m_vuoiCustom[i].m_ordRel, m_vuoiCustom[i].m_cptRel);
		fprintf(fp, "%5d] %s\n", i+1, sta.Chars());
	}

	fprintf(fp, "\n(VIRTUAL) TABLE OF NEW LIST ITEMS\n");
	for (int i = 0; i < m_viuoiItemNew.Size(); ++i)
	{
		int iuoi = m_viuoiItemNew[i];
		sta.Format("%5d {%g} %5d, %5d %5d %5d %7d %5d %3d",
			m_vuoiRevised[iuoi].m_hvoObj, &m_vuoiRevised[iuoi].m_guidObj,
			m_vuoiRevised[iuoi].m_cidObj, m_vuoiRevised[iuoi].m_nDepth,
			m_vuoiRevised[iuoi].m_hvoOwner, m_vuoiRevised[iuoi].m_cidOwner,
			m_vuoiRevised[iuoi].m_fidOwner, m_vuoiRevised[iuoi].m_ordRel,
			m_vuoiRevised[iuoi].m_cptRel);
		fprintf(fp, "%5d] %s\n", i+1, sta.Chars());
	}
	fclose(fp);
#endif
}


/*----------------------------------------------------------------------------------------------
	Handle XML start elements for objects during the second pass.

	@param eti Reference to the basic element type information structure.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::StartObject2(ElemTypeInfo & eti, const XML_Char * pszName)
{
	Assert(m_vetiOpen.Size());
	// m_cobj2 starts at 1, but we use it to index into m_vuoiRevised. hence the minus one.
	int hobj = m_vuoiRevised[m_cobj2 - 1].m_hvoObj;
	Assert(m_vuoiRevised[m_cobj2 - 1].m_cidObj == m_pfwxd->ClassInfo(eti.m_icls).cid);
	if (m_vuoiRevised[m_cobj2 - 1].m_cidObj != m_pfwxd->ClassInfo(eti.m_icls).cid)
	{
		// "A list update XML file must begin with a field tag, not a class tag!"
		ThrowWithLogMessage(kstidXmlErrorMsg319);
	}
	m_vetiOpen.Push(eti);
	m_vhobjOpen.Push(hobj);
	++m_cobj2;
}


/*----------------------------------------------------------------------------------------------
	Store all the accumulated object attribute values.

	@param padvi Optional pointer to a progress report object
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::StoreData(IAdvInd * padvi, int nPercent)
{
	ClearReferenceCollections();
	ClearReferenceSequences();

	TimeStampObjects();
	if (padvi)
		padvi->Step(1);		// now at 46%.

	unsigned long cStepNew;

	int ifld;
	if (padvi)
	{
		m_cBlkTotal = 0;
		for (ifld = 0; ifld < m_vstda.Size(); ++ifld)
			m_cBlkTotal += m_vstda[ifld].m_vhobj.Size();
	}

	for (ifld = 0; ifld < m_vstda.Size(); ++ifld)
	{
		if (m_vstda[ifld].m_vhobj.Size())
		{
			switch (m_pfwxd->FieldInfo(ifld).cpt)
			{
			case kcptReferenceAtom:
				StoreReferenceAtoms(ifld);
				break;
			case kcptReferenceCollection:
				StoreReferenceCollections(ifld);
				break;
			case kcptReferenceSequence:
				StoreReferenceSequences(ifld);
				break;
			case kcptUnicode:
			case kcptBigUnicode:
				StoreUnicodeData(ifld);
				break;
			case kcptString:
			case kcptBigString:
				StoreStringData(ifld);
				break;
			default:
				// "ERROR! BUG! Invalid field data type storing data?? (%d)"
				ThrowWithLogMessage(kstidXmlErrorMsg030, (void *)m_pfwxd->FieldInfo(ifld).cpt);
				break;
			}
		}
		if (padvi && m_cBlkTotal)
		{
			// Storing data is 15% of the progress to report.
			m_cBlk += m_vstda[ifld].m_vhobj.Size();
			cStepNew = m_cBlk * 15 / m_cBlkTotal;
			if (cStepNew > m_cStep)
			{
				padvi->Step(cStepNew - m_cStep);
				m_cStep = cStepNew;
			}
		}
	}
	m_vstda.Clear();
	FixReferenceSequences();

	StoreMultiUnicode();
	StoreMultiBigUnicode();
	StoreMultiString();
	StoreMultiBigString();

	XML_ParserFree(m_parser);
	m_parser = 0;
}

/*----------------------------------------------------------------------------------------------
	Delete any stored ReferenceCollection data for the updated objects from the database,
	except for custom list items, and references to custom list items.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ClearReferenceCollections()
{
	for (int i = 0; i < m_vuoiRevised.Size(); ++i)
	{
		UpdateObjInfo & uoi = m_vuoiRevised[i];
		if (uoi.m_hvoObj >= m_hvoMin)
			continue;	// no data to delete.
		int icls = m_pfwxd->IndexOfCid(uoi.m_cidObj);
		const Vector<int> & viflds = m_pfwxd->ClassFields(icls);
		for (int i2 = 0; i2 < viflds.Size(); ++i2)
		{
			if (m_pfwxd->FieldInfo(viflds[i2]).cpt == kcptReferenceCollection)
				ClearReferenceCollection(uoi.m_hvoObj, viflds[i2]);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Delete any stored ReferenceSequence data for the updated objects from the database,
	except for custom list items, and references to custom list items.  Adjust the ord value
	for references to custom list items to ridiculously large values to allow inserting other
	references first.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ClearReferenceSequences()
{
	for (int i = 0; i < m_vuoiRevised.Size(); ++i)
	{
		UpdateObjInfo & uoi = m_vuoiRevised[i];
		if (uoi.m_hvoObj >= m_hvoMin)
			continue;	// no data to delete.
		int icls = m_pfwxd->IndexOfCid(uoi.m_cidObj);
		const Vector<int> & viflds = m_pfwxd->ClassFields(icls);
		for (int i2 = 0; i2 < viflds.Size(); ++i2)
		{
			if (m_pfwxd->FieldInfo(viflds[i2]).cpt == kcptReferenceSequence)
				ClearReferenceSequence(uoi.m_hvoObj, viflds[i2]);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	For any references to custom list items in stored ReferenceSequence data, fix the ord value
	back to something reasonable.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::FixReferenceSequences()
{
	if (m_vuoiCustom.Size() == 0)
		return;			// Noncustom items have proper ord values set.

	for (int i = 0; i < m_vuli.Size(); ++i)
	{
		UpdateLinkInfo & uli = m_vuli[i];
		int ifld = m_pfwxd->IndexOfFid(uli.m_fidSrc);
		const FwDbFieldInfo & fdfi = m_pfwxd->FieldInfo(ifld);
		if (fdfi.cpt == kcptReferenceSequence && fdfi.cidDst == m_cidItem)
		{
			int iuoi;
			if (m_hmhvoiuoiCustom.Retrieve(uli.m_hvoDst, &iuoi))
				FixReferenceSequence(uli, fdfi);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Clear out all references for this source object in this field which do not point to a custom
	list item.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ClearReferenceCollection(int hobjSrc, int ifld)
{
	StrAnsi staCmd;
	int icls = m_pfwxd->IndexOfCid( m_pfwxd->FieldInfo(ifld).cid );
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd.Format("DELETE FROM %<0>S_%<1>S WHERE Src = %<2>u",
			m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(), hobjSrc);
	}
	//TODO: append closing paren and semi colon after vhobj
	if (m_vuoiCustom.Size() != 0)
	{
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			staCmd.Append(" AND Dst NOT IN (");
		}
		Vector<int> vhobj;
		vhobj.Resize(m_vuoiCustom.Size());
		for (int i = 0; i < m_vuoiCustom.Size(); ++i)
			vhobj[i] = m_vuoiCustom[i].m_hvoObj;
		ExpandAndExecuteCommand(staCmd.Chars(), vhobj);
	}
	else
	{
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			staCmd.Append(";"); //added this line to guarantee the query will end with ';'
		}
		ExecuteSimpleSQL(staCmd.Chars(), __LINE__);
	}
}

/*----------------------------------------------------------------------------------------------
	Clear out all references for this source object in this field which do not point to a custom
	list item.  Adjust the ord values for any remaining references to something ridiculously
	large.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ClearReferenceSequence(int hobjSrc, int ifld)
{
	ClearReferenceCollection(hobjSrc, ifld);
	if (m_vuoiCustom.Size() != 0)
	{
		StrAnsi staCmd;
		int icls = m_pfwxd->IndexOfCid( m_pfwxd->FieldInfo(ifld).cid );
		if(CURRENTDB == FB || CURRENTDB == MSSQL) {
			staCmd.Format("UPDATE %<0>S_%<1>S SET Ord = Ord + %<2>u WHERE Src = %<3>u;",
				m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(),
				m_hvoMin + m_cobj, hobjSrc);
		}
		ExecuteSimpleSQL(staCmd.Chars(), __LINE__);
	}
}

/*----------------------------------------------------------------------------------------------
	Adjust the ord values for this field with this source object to all be reasonable.
	(Note that nothing that references CmSemanticDomain is a sequence, so this code is not
	yet written since there's no way to test it!)
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::FixReferenceSequence(UpdateLinkInfo & uli, const FwDbFieldInfo & fdfi)
{
	Assert(fdfi.cpt != kcptReferenceSequence || fdfi.cidDst != m_cidItem);
}

/*----------------------------------------------------------------------------------------------
	Return the index into vmlf of the entry whose m_fid and m_ws match fid and ws.  Add a new
	entry if needed.

	@param vmlf reference to a vector of MultilingualField structs
	@param fid field id to find in vmlf
	@param ws writing system id to find i vmlf together with fid (defaults to zero)

	@return index into vfmw of matching entry (which may be newly created)
----------------------------------------------------------------------------------------------*/
int FwXmlUpdateData::GetMultilingualFieldIndex(Vector<MultilingualField> & vmlf, int fid,
	int ws)
{
	for (int i = 0; i < vmlf.Size(); ++i)
	{
		if (vmlf[i].m_fid == fid && vmlf[i].m_ws == ws)
			return i;
	}
	MultilingualField mlf;
	mlf.m_fid = fid;
	mlf.m_ws = ws;
	vmlf.Push(mlf);
	return vmlf.Size() - 1;
}

/*----------------------------------------------------------------------------------------------
	Delete any conflicting data in the given table so that an INSERT will work.

	@param vmlf reference to a vector of MultilingualField structs
	@param pszTable name of the database table (either "MultiBigTxt$" or "MultiBigStr$"
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ClearMultiTable(Vector<MultilingualField> & vmlf, const char * pszTable)
{
	StrAnsi staCmd;
	for (int i = 0; i < vmlf.Size(); ++i)
	{
		//TODO: append closing paren and semi colon after vhobj
		if (vmlf[i].m_ws != 0)
		{
			if(CURRENTDB == FB || CURRENTDB == MSSQL) {
				staCmd.Format("DELETE FROM %s WHERE Flid=%u AND Ws=%u AND Obj IN (",
					pszTable, vmlf[i].m_fid, vmlf[i].m_ws);
			}
		}
		else
		{
			if(CURRENTDB == FB || CURRENTDB == MSSQL) {
				staCmd.Format("DELETE FROM %s WHERE Flid=%u AND Obj IN (",
					pszTable, vmlf[i].m_fid);
			}
		}
		ExpandAndExecuteCommand(staCmd.Chars(), vmlf[i].m_vhobj);
	}
}

/*----------------------------------------------------------------------------------------------
	Execute a command with one parameter and check it for sucess.

	@param pszCmd the SQL command executed
	@param vhobj the vector of database ids used as a parameter
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ExpandAndExecuteCommand(const char * pszCmd, Vector<int> & vhobj)
{
#if 1
	if (vhobj.Size() == 0)
		return;
	RETCODE rc;
	SqlStatement sstmt;
	sstmt.Init(m_sdb);

	StrAnsiBufHuge stabCmd;
	for (int i = 0; i < vhobj.Size(); ++i)
	{
		if (stabCmd.Length() > 0)
			stabCmd.FormatAppend(",%u", vhobj[i]);
		else
			stabCmd.Format("%s%u", pszCmd, vhobj[i]);
		if (stabCmd.Length() >= 2000)
		{
			stabCmd.Append(");");
			rc = SQLExecDirectA(sstmt.Hstmt(),
				reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
			VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
			stabCmd.Clear();
		}
	}
	if (stabCmd.Length() > 0)
	{
		stabCmd.Append(");");
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(stabCmd.Chars())), SQL_NTS);
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, stabCmd.Chars());
	}
	sstmt.Clear();
#else
	// 1. Set the SQL_ATTR_PARAM_BIND_TYPE statement attribute to use column-wise binding.
	// 2. Specify the number of elements in each parameter array.
	// 3. Specify an array in which to return the status of each set of parameters.
	// 4. Specify an SQLUINTEGER value in which to return the number of sets of parameters
	//    processed.
	// 5. Bind the input parameters to vhobj.
	// 6. Execute the command and check the results.
	int cParams = vhobj.Size();
	Vector<SQLUSMALLINT> vnParamStatus;
	SQLUINTEGER cParamsProcessed;
	Vector<SQLINTEGER> vcbhobj;
	RETCODE rc;
	SqlStatement sstmt;

	vcbhobj.Resize(cParams);
	vnParamStatus.Resize(cParams);
	sstmt.Init(m_sdb);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMSET_SIZE,
		reinterpret_cast<void *>(cParams), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_STATUS_PTR, vnParamStatus.Begin(), 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMS_PROCESSED_PTR, &cParamsProcessed, 0);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0,
		vhobj.Begin(), 0, vcbhobj.Begin());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pszCmd)), SQL_NTS);
	CheckDeleteWithParamForSuccess(cParamsProcessed, vnParamStatus.Begin(), vhobj, pszCmd);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pszCmd);
	sstmt.Clear();
#endif
}

/*----------------------------------------------------------------------------------------------
	Check an array of parameter status codes for successful processing in a DELETE operation.

	@param cParamsProcessed number of parameters processed
	@param rgnParamStatus array of status codes for the parameter
	@param vhobj the vector of database ids used as a parameter
	@param pszCmd the SQL command executed
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::CheckDeleteWithParamForSuccess(SQLUINTEGER cParamsProcessed,
	SQLUSMALLINT * rgnParamStatus, const Vector<int> & vhobj, const char * pszCmd)
{
	StrAnsi sta;
	StrAnsi staFmt;
	for (uint i = 0; i < cParamsProcessed; ++i)
	{
		staFmt.Clear();
		switch (rgnParamStatus[i])
		{
		case SQL_PARAM_SUCCESS:
		case SQL_PARAM_SUCCESS_WITH_INFO:
			break;
		case SQL_PARAM_ERROR:
			// "ERROR in ""%s"" [param=%d]."
			staFmt.Load(kstidXmlErrorMsg320);
			break;
		case SQL_PARAM_UNUSED:
			// "UNUSED in ""%s"" [param=%d]."
			staFmt.Load(kstidXmlErrorMsg321);
			break;
		case SQL_PARAM_DIAG_UNAVAILABLE:
			// "UNAVAIL INFO in ""%s"" [param=%d]."
			staFmt.Load(kstidXmlErrorMsg322);
			break;
		}
		if (staFmt.Length())
		{
			sta.Format(staFmt.Chars(), pszCmd, vhobj[i]);
			LogMessage(sta.Chars());
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Update the database tables to remove data that may be getting replaced or removed in the
	updated list.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::RemoveObsoleteData(int cid, IAdvInd * padvi, int nPercent)
{
	int icls = m_pfwxd->IndexOfCid(cid);
	const Vector<int> & viflds = m_pfwxd->ClassFields(icls);
	int cStep = 0;
	int cStepNew = 0;
	for (int i = 0; i < viflds.Size(); ++i)
	{
		const FwDbFieldInfo & fdfi = m_pfwxd->FieldInfo(viflds[i]);
		switch (fdfi.cpt)
		{
		case kcptBoolean:
		case kcptInteger:
		case kcptNumeric:
		case kcptFloat:
		case kcptGenDate:
			// Set these to zero.
			ClearObsoleteBasicData(fdfi, "0");
			break;

		case kcptTime:
		case kcptGuid:
		case kcptImage:
		case kcptBinary:
		case kcptString:
		case kcptUnicode:
		case kcptBigString:
		case kcptBigUnicode:
			// Set these to null
			ClearObsoleteBasicData(fdfi, "null");
			break;

		case kcptMultiString:
		case kcptMultiUnicode:
		case kcptMultiBigString:
		case kcptMultiBigUnicode:
			ClearObsoleteMultilingualData(fdfi);
			break;

		case kcptReferenceAtom:
		case kcptReferenceCollection:
		case kcptReferenceSequence:
			// These are handled separately.
			break;

		case kcptOwningAtom:
		case kcptOwningCollection:
		case kcptOwningSequence:
			if (fdfi.cidDst != cid)
				RemoveObsoleteData(fdfi.cidDst, NULL, 0);
			break;
		}
		if (padvi && nPercent)
		{
			cStepNew = ((i + 1) * nPercent) / viflds.Size();
			if (cStepNew > cStep)
			{
				padvi->Step(cStepNew - cStep);
				cStep = cStepNew;
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Clear any data in the given field belonging to items in the list.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ClearObsoleteBasicData(const FwDbFieldInfo & fdfi, const char * pszValue)
{
	Vector<int> vhobj;
	for (int i = 0; i < m_vuoiRevised.Size(); ++i)
	{
		UpdateObjInfo & uoi = m_vuoiRevised[i];
		if (uoi.m_cidObj != fdfi.cid && !m_pfwxd->IsSubclass(uoi.m_cidObj, fdfi.cid))
			continue;		// wrong kind of object
		if (uoi.m_hvoObj >= m_hvoMin)
			continue;		// no old data to delete.
		vhobj.Push(uoi.m_hvoObj);
	}
	if (vhobj.Size() == 0)
		return;
	int ifld = m_pfwxd->IndexOfFid(fdfi.fid);
	int icls = m_pfwxd->IndexOfCid(fdfi.cid);
	StrAnsi staCmd;
	//TODO: append closing paren and semi colon after vhobj
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		staCmd.Format("UPDATE %S SET %S=%s WHERE Id IN (",
			m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars(), pszValue);
	}
	ExpandAndExecuteCommand(staCmd.Chars(), vhobj);
}


/*----------------------------------------------------------------------------------------------
	Clear any relevant MultiUnicode data.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ClearObsoleteMultilingualData(const FwDbFieldInfo & fdfi)
{
	if (fdfi.fid == kflidCmPossibility_Name)
		return;
	if (fdfi.fid == kflidCmPossibility_Abbreviation)
		return;
	if (fdfi.fid == kflidCmPossibility_Description)
		return;
	Vector<int> vhobj;
	for (int i = 0; i < m_vuoiRevised.Size(); ++i)
	{
		UpdateObjInfo & uoi = m_vuoiRevised[i];
		if (uoi.m_cidObj != fdfi.cid && !m_pfwxd->IsSubclass(uoi.m_cidObj, fdfi.cid))
			continue;		// wrong kind of object
		if (uoi.m_hvoObj >= m_hvoMin)
			continue;		// no data to delete.
		vhobj.Push(uoi.m_hvoObj);
	}
	if (vhobj.Size() == 0)
		return;
	int ifld, icls;
	StrAnsi staCmd;
	//TODO: append closing paren and semi colon after vhobj
	if(CURRENTDB == FB || CURRENTDB == MSSQL) {
		switch (fdfi.cpt)
		{
		case kcptMultiString:
			staCmd.Format("DELETE FROM MultiStr$ WHERE Flid=%u AND Obj IN (", fdfi.fid);
			break;
		case kcptMultiUnicode:
			ifld = m_pfwxd->IndexOfFid(fdfi.fid);
			icls = m_pfwxd->IndexOfCid(fdfi.cid);
			staCmd.Format("DELETE FROM %S_%S WHERE Obj IN (",
				m_pfwxd->ClassName(icls).Chars(), m_pfwxd->FieldName(ifld).Chars());
			break;
		case kcptMultiBigString:
			staCmd.Format("DELETE FROM MultiBigStr$ WHERE Flid=%u AND Obj IN (", fdfi.fid);
			break;
		case kcptMultiBigUnicode:
			staCmd.Format("DELETE FROM MultiBigUnicode$ WHERE Flid=%u AND Obj IN (", fdfi.fid);
			break;
		}
	}
	ExpandAndExecuteCommand(staCmd.Chars(), vhobj);
}

/*----------------------------------------------------------------------------------------------
	Store the accumulated MultiBigUnicode data, first clearing any existing data.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::StoreMultiBigUnicode()
{
	ClearMultiBigUnicode();
	FwXmlImportData::StoreMultiBigUnicode();
}

/*----------------------------------------------------------------------------------------------
	Clear any relevant MultiBigUnicode data.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ClearMultiBigUnicode()
{
	Vector<MultilingualField> vmlf;
	for (int i = 0; i < m_mtdBig.m_vhobj.Size(); ++i)
	{
		int i2 = GetMultilingualFieldIndex(vmlf, m_mtdBig.m_vfid[i]);
		vmlf[i2].m_vhobj.Push(m_mtdBig.m_vhobj[i]);
	}
	ClearMultiTable(vmlf, "MultiBigTxt$");
}


/*----------------------------------------------------------------------------------------------
	Store the accumulated MultiBigString data, first clearing any relevant data.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::StoreMultiBigString()
{
	ClearMultiBigString();
	FwXmlImportData::StoreMultiBigString();
}

/*----------------------------------------------------------------------------------------------
	Clear any relevant MultiBigString data.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ClearMultiBigString()
{
	Vector<MultilingualField> vmlf;
	int i2;
	for (int i = 0; i < m_msdBig.m_vhobj.Size(); ++i)
	{
		// Retain any existing translated data for the description not matched in updated list.
		if (m_msdBig.m_vfid[i] == kflidCmPossibility_Description)
			i2 = GetMultilingualFieldIndex(vmlf, m_msdBig.m_vfid[i], m_msdBig.m_vws[i]);
		else
			i2 = GetMultilingualFieldIndex(vmlf, m_msdBig.m_vfid[i]);
		vmlf[i2].m_vhobj.Push(m_msdBig.m_vhobj[i]);
	}
	ClearMultiTable(vmlf, "MultiBigStr$");
}


/*----------------------------------------------------------------------------------------------
	Save the status of the progress report indicator.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::SaveProgressReportStatus(IAdvInd * padvi)
{
	if (padvi == NULL)
		return;
	IAdvInd4Ptr qadvi4;
	try
	{
		CheckHr(padvi->QueryInterface(IID_IAdvInd4, (void **)&qadvi4));
	}
	catch (Throwable& thr)
	{
		if (thr.Result() == E_NOINTERFACE)
			return;
		throw thr;
	}
	CheckHr(qadvi4->get_Position(&m_nPosOrig));
	CheckHr(qadvi4->GetRange(&m_nMinOrig, &m_nMaxOrig));
	CheckHr(qadvi4->SetRange(0, 100));
	CheckHr(qadvi4->put_Position(0));
}


/*----------------------------------------------------------------------------------------------
	Restore the original status of the progress report indicator.  This must be preceded by a
	call to SaveProgressReportStatus.
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::RestoreProgressReportStatus(IAdvInd * padvi)
{
	if (padvi == NULL)
		return;
	IAdvInd3Ptr qadvi3;
	try
	{
		CheckHr(padvi->QueryInterface(IID_IAdvInd3, (void **)&qadvi3));
	}
	catch (Throwable& thr)
	{
		if (thr.Result() == E_NOINTERFACE)
			return;
		throw thr;
	}
	CheckHr(qadvi3->SetRange(m_nMinOrig, m_nMaxOrig));
	CheckHr(qadvi3->put_Position(m_nPosOrig));
}


/*----------------------------------------------------------------------------------------------
	Log the amount of time consumed by initialization before reading the XML file.

	@param timDelta total number of seconds elapsed while initializing
----------------------------------------------------------------------------------------------*/
void FwXmlUpdateData::ReportInitializationTime(long timDelta)
{
	// "Initializing from the database before reading the XML file took %<0>d %<1>s."
	ReportTime(timDelta, kstidXmlInfoMsg301);
}


/*
Timings for running UpdateListFromXml() extracted from log files during development:

(Special init -  12  12  12  12  |   12    12    ??)

Initializing  -  12  13  12  12  |   12    12    12
First pass    -   3   2   3   3  |    3     3     3
Processing    -   0   0   0   0  |    0     0     0
Creating new  -   1   2   2   1  |    1     2     2
Updating new  -   2   1   1   2  |    2     1     1
Fixing links  -   0   0   0   0  |    0     0     0
Collect del.  -   0   0   0   0  |   82     0    83
Deleting 1817 -  21  23  22  21  |   22          21
Deleting 3909 -                  |         24
Removing data -  12  11  11  11  |   11    11    11
Second pass   -  10  10  10  10  |   10    10    10
Storing data  -   7   8   7   7  |    7     7     8
Loading XML   -  68  70  68  67  |  150    70   151
 */


/*----------------------------------------------------------------------------------------------
	Update a list from an XML file which contains the set of deleted items and merged items, as
	well as the entire contents of the list.

	@param bstrFile Name of the input XML file.
	@param hvoOwner Database id of the object's owner.
	@param flid Field id of the object.
	@param padvi Pointer to progress bar interface (may be NULL).

	@return S_OK, E_INVALIDARG, E_UNEXPECTED, E_OUTOFMEMORY, E_FAIL, or possibly another
					COM error code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwXmlData::UpdateListFromXml(BSTR bstrFile, int hvoOwner, int flid,
	IAdvInd * padvi)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrFile);
	ChkComArgPtrN(padvi);
	if (!hvoOwner || !flid)
		ThrowHr(E_INVALIDARG);
	Assert(m_sdb.IsOpen());
	if (!m_sdb.IsOpen())
		ThrowHr(E_UNEXPECTED);

	FwXmlUpdateData xud(this);
	xud.SaveProgressReportStatus(padvi);
	time_t timBegin = time(0);

	// Initialize the ICU setup.  (Needed for Unicode normalization)
	StrUtil::InitIcuDataDir();

	// Open the input file and create the log file.
	STATSTG statFile;
	xud.OpenFiles(bstrFile, &statFile);

	// Set the toplevel element information for the parser to use, first calculating a few of
	// the values we need.
	int ifld;
	int icls;
	xud.EnsureValidImportArguments(hvoOwner, flid, ifld, icls);
	int hvoObj = 0;
	GUID guidObj = GUID_NULL;
	if (FieldInfo(ifld).cpt == kcptOwningAtom)
		xud.LoadPossibleExistingObjectId(hvoOwner, flid, hvoObj, guidObj);
	int hvoMin = xud.GetNextRealObjectId();
	xud.InitializeTopElementInfo(hvoOwner, flid, icls, FieldXmlName(ifld).Chars(),
		hvoObj, guidObj, hvoMin);
	xud.InitializeForMerging(hvoOwner, icls, hvoMin);
	if (padvi)
		padvi->Step(1);
	//time_t tim0 = time(0);
	xud.InitializeForUpdate(hvoOwner, flid);
	//time_t tim1 = time(0);
	if (padvi)
		padvi->Step(17);	// "EXEC GetLinkedObjs$" takes all the time here...

	// Load some data we need from the database.  Allow creating objects with full information.
	xud.MapGuidsToHobjs();
	xud.MapIcuLocalesToWsHobjs();
	//IDENTITY_INSERT is taken care of in T_BI0_CmObject
	/*if(CURRENTDB == FB) {
		xud.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject ON;", __LINE__);
	}*/
	if(CURRENTDB == MSSQL) {
		xud.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject ON", __LINE__);
	}
	time_t timInit = time(0);
	long timDelta = (long)(timInit - timBegin);
	xud.ReportInitializationTime(timDelta);
	// "(Special initializing for update alone took %<0>d %<1>s.)"
	//xud.ReportTime(tim1 - tim0, kstidXmlInfoMsg310);

	// Process the XML file (Pass One of Two).

	xud.SetOuterHandlers(FwXmlImportData::HandleStartTag1, FwXmlImportData::HandleEndTag1);
	xud.ParseXmlPhaseOne(m_vstufld.Size(), statFile, padvi, 4);

	time_t timMid1 = time(0);
	timDelta = (long)(timMid1 - timInit);
	xud.ReportFirstPassTime(timDelta);

	// This next method takes about half the time for updating the list!
	xud.MergeDeleteAndCreateObjects(padvi);

	time_t timMid2 = time(0);

	// Process the XML file (Pass Two of Two).

	xud.SetOuterHandlers(FwXmlImportData::HandleStartTag2, FwXmlImportData::HandleEndTag2);
	xud.ParseXmlPhaseTwo(m_vstufld.Size(), statFile, padvi, 15);

	time_t timMid3 = time(0);
	timDelta = (long)(timMid3 - timMid2);
	xud.ReportSecondPassTime(timDelta);
	xud.StoreData(padvi, 11);

	// Add empty structured text objects as needed, fix dates, fix colors, and fix null StStyle
	// Rules.
	xud.CreateEmptyTextFields();
	xud.UpdateDttm();
	xud.SetCmPossibilityColors();
	xud.FixStStyle_Rules();
	if (padvi)
		padvi->Step(1);		// now at 100%

	// Restore normal insertion state for objects.
	//IDENTITY_INSERT is taken care of in T_BI0_CmObject
	/*if(CURRENTDB == FB) {
		xud.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject OFF;", __LINE__);
	}*/
	if(CURRENTDB == MSSQL) {
		xud.ExecuteSimpleSQL("SET IDENTITY_INSERT CmObject OFF;", __LINE__);
	}
	time_t timMid4 = time(0);
	xud.LogRepeatedMessages();
	timDelta = (long)(timMid4 - timMid3);
	xud.ReportDataStorageStats(timDelta);
	timDelta = (long)(time(0) - timBegin);
	xud.ReportTotalTime(timDelta);
	xud.RestoreProgressReportStatus(padvi);

	END_COM_METHOD(s_fact, IID_IFwXmlData);
}


/*----------------------------------------------------------------------------------------------
	Set the base directory for an import operation.  This may be needed to access auxiliary
	files such as pictures or media.

	@param bstrDir Name of the input XML file.

	@return S_OK or an appropriate COM error code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwXmlData::put_BaseImportDirectory(BSTR bstrDir)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrDir);

	m_stuBaseImportDirectory.Assign(bstrDir);

	END_COM_METHOD(s_fact, IID_IFwXmlData);
}

/*----------------------------------------------------------------------------------------------
	Get the (previously set) base directory for an import operation.

	@param pbstrDir address of the BSTR which receives the directory pathname.

	@return S_OK or an appropriate COM error code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwXmlData::get_BaseImportDirectory(BSTR * pbstrDir)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstrDir);

	m_stuBaseImportDirectory.GetBstr(pbstrDir);

	END_COM_METHOD(s_fact, IID_IFwXmlData);
}


// Implement the FwXml methods for our FwXmlImportData struct.
#include "FwXmlString.cpp"

// Explicit instantiation.
#include "Vector_i.cpp"
#include "HashMap_i.cpp"
#include "Set_i.cpp"

// Local Variables:
// compile-command:"cmd.exe /E:4096 /C c:\\FW\\Bin\\mkcel.bat"
// End:
