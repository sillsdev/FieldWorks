/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, SIL International. All rights reserved.

File: FwXmlExport.cpp
Responsibility: Steve McConnel
Last reviewed:

	This file contains the XML export methods for the FwXmlData class/interface.

	STDMETHODIMP FwXmlData::SaveXml(BSTR bstrFile, ILgWritingSystemFactory * pwsf,
		IAdvInd * padvi)

	const wchar * FwXmlData::WriteFieldStartTag(IStream * pstrm, int ifld)
	void FwXmlData::WriteObjectData(IStream * pstrm, int iobj, FwXmlExportData * pxed)
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include "FwXml.h"
#include "xmlparse.h"

#undef THIS_FILE
DEFINE_THIS_FILE

//#define LOG_SQL 1

//:End Ignore

// User Interface writing system.
static int g_wsUser = 0;
static StrAnsi g_staWsUser;

//:> Static Methods.
static void WriteXmlString(IStream * pstrm, int ws, const OLECHAR * prgchTxt, int cchTxt,
	const BYTE * prgbFmt, int cbFmt, FwXmlExportData * pxed);

/*----------------------------------------------------------------------------------------------
	This data structure stores one row of basic attribute data retrieved from the database.

	Hungarian: bdr
----------------------------------------------------------------------------------------------*/
struct BasicDataRow
{
	int m_hobj;			// Database object id for this attribute's owner.
	int m_fid;			// Field id for this attribute.
	StrAnsi m_staValue;	// Value loaded from the database for this attribute.
};

/*----------------------------------------------------------------------------------------------
	This data structure stores one row of "Binary" or "Image" attribute data retrieved from the
	database.

	Hungarian: bir
----------------------------------------------------------------------------------------------*/
struct BinaryDataRow
{
	int m_hobj;			// Database object id for this attribute's owner.
	int m_fid;			// Field id for this attribute.
	Vector<byte> m_vbValue;	// Value loaded from the database for this attribute.
};

/*----------------------------------------------------------------------------------------------
	This data structure stores one row of "Unicode" or "BigUnicode" attribute data retrieved
	from the database.

	Hungarian: udr
----------------------------------------------------------------------------------------------*/
struct UnicodeDataRow
{
	int m_hobj;			// Database object id for this attribute's owner.
	int m_fid;			// Field id for this attribute.
	StrUni m_stuValue;	// Value loaded from the database for this attribute.
};

/*----------------------------------------------------------------------------------------------
	This data structure stores one row of "String" or "BigString" attribute data retrieved
	from the database.

	Hungarian: sdr
----------------------------------------------------------------------------------------------*/
struct StringDataRow
{
	int m_hobj;			// Database object id for this attribute's owner.
	int m_fid;			// Field id for this attribute.
	StrUni m_stuValue;		// Unicode string loaded from the database for this attribute.
	Vector<byte> m_vbFmt;	// Formatting data loaded from the database for this attribute.
};

/*----------------------------------------------------------------------------------------------
	This data structure stores one row of "MultiUnicode" or "MultiBigUnicode" attribute data
	retrieved from the database.

	Hungarian: mur
----------------------------------------------------------------------------------------------*/
struct MultiUnicodeDataRow
{
	int m_hobj;	// Database object id for this attribute's owner.
	int m_fid;	// Field id for this attribute.
	int m_ws;	// Writing system of this string loaded from the database for this attribute.
	StrUni m_stuValue;	// Unicode string loaded from the database for this attribute.
};

/*----------------------------------------------------------------------------------------------
	This data structure stores one row of "MultiString" or "MultiBigString" attribute data
	retrieved from the database.

	Hungarian: msr
----------------------------------------------------------------------------------------------*/
struct MultiStringDataRow
{
	int m_hobj;	// Database object id for this attribute's owner.
	int m_fid;	// Field id for this attribute.
	int m_ws;	// Writing system of this string loaded from the database for this attribute.
	StrUni m_stuValue;		// Unicode string loaded from the database for this attribute.
	Vector<byte> m_vbFmt;	// Formatting data loaded from the database for this attribute.
};

/*----------------------------------------------------------------------------------------------
	This data structure stores one row of "ReferenceCollection" or "ReferenceSequence"
	attribute data retrieved from the database.

	Hungarian: rcd
----------------------------------------------------------------------------------------------*/
struct ReferenceCollectionDataRow
{
	int m_hobj;			// Database object id for this attribute's owner.
	int m_fid;			// Field id for this attribute.
	int m_hobjDst;		// Database object id for this attribute's referenced object.
	int m_ord;			// Sorting key for ordering the collection.
};

/*----------------------------------------------------------------------------------------------
	This data structure stores one row of "ReferenceAtom" attribute data retrieved from the
	database.

	Hungarian: rad
----------------------------------------------------------------------------------------------*/
struct ReferenceAtomDataRow
{
	int m_hobj;			// Database object id for this attribute's owner.
	int m_fid;			// Field id for this attribute.
	int m_hobjDst;		// Database object id for this attribute's referenced object.
};

/*----------------------------------------------------------------------------------------------
	This data structure stores one row of the constructed "ObjHierarchy" table.

	Hungarian: ohd
----------------------------------------------------------------------------------------------*/
struct ObjHierarchyDataRow
{
	int m_depth;		// Depth in the ownership hierarchy (1 for root).
	int m_fidOwner;		// Field id for the object's owner (ignored for root).
	int m_hobj;			// Database id for the object.
	int m_cid;			// Class id for the object.
	GUID m_guid;		// Assigned GUID for the object.
};

/*----------------------------------------------------------------------------------------------
	This data structure is used to build a stack of open XML tags.

	Hungarian: xtag
----------------------------------------------------------------------------------------------*/
struct XmlTagStack
{
	int m_depth;		// Depth in the ownership hierarchy for the open element.
	bool m_fFieldTag;	// Flag whether this XML tag represents a field.
	union {
		// If !m_fFieldTag, index into the class meta-data table of FwXmlData (m_vstucls).
		int m_icls;
		// If m_fFieldTag, index into the field meta-data tables (m_vfdfi, m_vstufld and
		// m_vstufldXml) of FwXmlData.
		int m_ifld;
	};
	const wchar * m_pszName;	// Element name string, saved for XML end tag.
};

/*----------------------------------------------------------------------------------------------
	This data structure stores the information for a link to a LexEntry or LexSense.

	Hungarian: hwd
----------------------------------------------------------------------------------------------*/
struct HeadwordData
{
	bool m_fSense;
	StrUni m_stuHeadword;
};

/*----------------------------------------------------------------------------------------------
	This contains data used throughout XML export (including several scratchpad buffers), plus a
	couple of useful functions.

	Hungarian: xed
----------------------------------------------------------------------------------------------*/
class FwXmlExportData
{
public:
	//:> Constructor and Destructor.
	FwXmlExportData(FwXmlData * pfxd);
	~FwXmlExportData();

	//:> Other methods.
	void LogMessage(const char * pszMsg);
	void VerifySqlRc(RETCODE rc, SQLHANDLE hstmt, int cline, const char * pszCmd = NULL);

	void StoreOwnersNameAbbr(int hobj, int ws, int fidAbbrOwner, int fidNameOwner);
	void BuildReversalIndexHierForm(int iudr, StrUni & stu);
	void LoadObjHierarchy();
	void LoadOwners();
	void LoadClassMap();
	int ReadAnalWs();
	int ReadVernWs();
	int ReadWsFromDb(const char * pszWsColumn, int & ws, StrAnsi & staWs);
	void StoreLinkInfo();
	void StoreEntryOrSenseInfo();
	void WriteXmlTextProps(IStream * pstrm, DataReader * pdrdr, int flid, int hobj);
	const wchar * FindUnicode(int hobj, int flid);
	FwXmlData * m_pfxd;

	// With increased fields in our DB CM, the total query became too large for a
	// StrAnsiBufHuge, so it was switched to a plain StrAnsi.
	//StrAnsiBufHuge m_stabCmd;			// used for building SQL commands.
	StrAnsi m_staCmd;						// used for building SQL commands.

	Vector<BasicDataRow> m_vbdr;			// Basic attribute data loaded from the database.
	BasicDataRow * m_pbdr;					// Iterator for basic attribute data.

	Vector<BinaryDataRow> m_vbirBinary;		// "Binary" data loaded from the database.
	BinaryDataRow * m_pbirBinary;			// Iterator for "Binary" data.

	Vector<BinaryDataRow> m_vbirImage;		// "Image" data loaded from the database.
	BinaryDataRow * m_pbirImage;			// Iterator for "Image" data.

	Vector<UnicodeDataRow> m_vudr;			// "Unicode" data loaded from the database.
	UnicodeDataRow * m_pudr;				// Iterator for "Unicode" data.

	Vector<UnicodeDataRow> m_vudrBig;		// "BigUnicode" data loaded from the database.
	UnicodeDataRow * m_pudrBig;				// Iterator for "BigUnicode" data.

	Vector<StringDataRow> m_vsdr;			// "String" data loaded from the database.
	StringDataRow * m_psdr;					// Iterator for "String" data.

	Vector<StringDataRow> m_vsdrBig;		// "BigString" data loaded from the database.
	StringDataRow * m_psdrBig;				// Iterator for "BigString" data.

	Vector<MultiUnicodeDataRow> m_vmur;		// "MultiUnicode" data loaded from the database.
	MultiUnicodeDataRow * m_pmur;			// Iterator for "MultiUnicode" data.

	Vector<MultiUnicodeDataRow> m_vmurBig;	// "BigMultiUnicode" data loaded from the database.
	MultiUnicodeDataRow * m_pmurBig;		// Iterator for "BigMultiUnicode" data.

	Vector<MultiStringDataRow> m_vmsr;		// "MultiString" data loaded from the database.
	MultiStringDataRow * m_pmsr;			// Iterator for "MultiString" data.

	Vector<MultiStringDataRow> m_vmsrBig;	// "BigMultiString" data loaded from the database.
	MultiStringDataRow * m_pmsrBig;			// Iterator for "BigMultiString" data.

				// "ReferenceCollection" and "ReferenceSequence" data loaded from the database.
	Vector<ReferenceCollectionDataRow> m_vrcd;
				// Iterator for "ReferenceCollection" and "ReferenceSequence" data.
	ReferenceCollectionDataRow * m_prcd;

	Vector<ReferenceAtomDataRow> m_vrad;	// "ReferenceAtom" data loaded from the database.
	ReferenceAtomDataRow * m_prad;			// Iterator for "ReferenceAtom" data.

	Vector<ObjHierarchyDataRow> m_vohd;	// Object Hierarchy table loaded from the database.

	int m_cobj;							// Number of rows in CmObject table (m_vohd.Size()).
	Vector<GUID *> m_mphobjguid;		// Maps hobj to GUID, for hobj < m_cobj.
	HashMap<int,GUID *> m_hmhobjguid;	// Maps hobj to GUID, for hobj >= m_cobj.

	HashMap<int, int> m_hmhobjimurAbbr;			// map hobj to multilingual abbreviation string
	HashMap<int, int> m_hmhobjimurName;			// map hobj to multilingual name string
	HashMap<int, int> m_hmhobjOwner;			// map hobj to its owner
	HashMap<int, int> m_hmhobjClid;				// map hobj to its class
	HashMap<int, int> m_hmhobjimurAbbrOwner;	// map hobj to owner's multilingual abbr. string
	HashMap<int, int> m_hmhobjimurNameOwner;	// map hobj to owner's multilingual name string
	HashMap<int, int> m_hmhobjisdr;				// map hobj to formatted string
	HashMap<int, int> m_hmhobjiudr;				// map hobj to string
	HashMap<int, int> m_hmhobjirad;				// map hobj to an atomic (writing system) ref.

	Vector<SQLWCHAR> m_vchTxt;		// Used for retrieving string text from database.
	Vector<SQLCHAR> m_vbFmt;		// Used for retrieving string formatting from database.

	StrAnsiBufPath m_stabpLog;			// Name of the log output file.
	FILE * m_pfileLog;					// Old fashioned C FILE pointer for log output file.

	int m_hobjLP;						// Database id of the language project.

	int m_cSql;							// Number of SQL commands executed.

	ITsStrFactoryPtr m_qtsf;			// TsString factory used for getting ITsString objects
										// which then write themselves as XML.
	ILgWritingSystemFactoryPtr m_qwsf;	// WritingSystem factory used in obtaining writing
										// system codes for writing TsStrings to XML.
	int m_wsAnal;
	StrAnsi m_staWsAnal;
	int m_wsVern;
	StrAnsi m_staWsVern;
	// Map hobj to corresponding LexEntry or LexSense headword representation.
	HashMap<int, HeadwordData> m_hmhobjHeadword;
};

/*----------------------------------------------------------------------------------------------
	If rc indicates an error (or partial success), retrieve and log more specific information.
	(This is identical to FwXmlImportData::VerifySqlRc except for calling a different LogMessage
	method.)

	@param rc ODBC/SQL function return code.
	@param hstmt Allocated handle to an ODBC/SQL statement.
	@param cline Number of the line in the source file where the call to this function occurs.
	@param pszCmd String containing the SQL command that produced rc.
----------------------------------------------------------------------------------------------*/
void FwXmlExportData::VerifySqlRc(RETCODE rc, SQLHANDLE hstmt, int cline, const char * pszCmd)
{
	StrAnsi staFmt;
	StrAnsi staMsg;
	if (pszCmd && *pszCmd)
	{
#ifdef LOG_SQL
		// "SQL[%d]: %s\n"
		staFmt.Load(kstidXmlInfoMsg219);
		staMsg.Format(staFmt.Chars(), cline, pszCmd);
		LogMessage(staMsg.Chars());
#endif /*LOG_SQL*/
		++m_cSql;
	}
	if (rc == SQL_ERROR || rc == SQL_SUCCESS_WITH_INFO)
	{
		SQLCHAR sqst[6];
		SQLINTEGER ntverr;
		SQLCHAR rgchBuf[256];
		SQLSMALLINT cb;
		SQLGetDiagRecA(SQL_HANDLE_STMT, hstmt, 1, sqst, &ntverr, rgchBuf,
			sizeof(rgchBuf)/sizeof(SQLCHAR), &cb);
		sqst[5] = 0;
		rgchBuf[(sizeof(rgchBuf)/sizeof(SQLCHAR)) - 1] = 0;
		StrAnsi staSqst((char *)&sqst);
		if (rc == SQL_ERROR)
		{
			StrAnsi staBuf((char *)&rgchBuf);
			if (pszCmd && *pszCmd)
			{
#ifndef LOG_SQL
				// "SQL[%d]: %s\n"
				staFmt.Load(kstidXmlInfoMsg219);
				staMsg.Format(staFmt.Chars(), cline, pszCmd);
				LogMessage(staMsg.Chars());
#endif /*!LOG_SQL*/
				// "ERROR %s executing SQL command:\n    %s\n"
				staFmt.Load(kstidXmlErrorMsg203);
				staMsg.Format(staFmt.Chars(), staSqst.Chars(), staBuf.Chars());
				LogMessage(staMsg.Chars());
			}
			else
			{
				// "ERROR %s executing SQL function on line %d of %s:\n    %s\n"
				staFmt.Load(kstidXmlErrorMsg204);
				staMsg.Format(staFmt.Chars(), staSqst.Chars(), cline, __FILE__, staBuf.Chars());
				LogMessage(staMsg.Chars());
			}
		}
		else
		{
#ifdef VERBOSE_EXPORT_LOGGING
			// Remove leading cruft from the information string.
			const achar * psz = reinterpret_cast<achar *>(rgchBuf);
			if (!_tcsncmp(psz, _T("[Microsoft]"), 11))
				psz += 11;
			if (!_tcsncmp(psz, _T("[ODBC SQL Server Driver]"), 24))
				psz += 24;
			if (!_tcsncmp(psz, _T("[SQL Server]"), 12))
				psz += 12;
			StrAnsi staBuf(psz);
			staFmt.Load(kstidXmlErrorMsg201);		// "    %s - %s\n"
			staMsg.Format(staFmt.Chars(), staSqst.Chars(), staBuf.Chars());
			LogMessage(staMsg.Chars());
#endif /*VERBOSE_EXPORT_LOGGING*/
		}
	}
	CheckSqlRc(rc);
}

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwXmlExportData::FwXmlExportData(FwXmlData * pfxd)
{
	m_pfileLog = NULL;
	m_cSql = 0;
	m_vchTxt.Resize(4001);
	m_vbFmt.Resize(8000);
	m_wsAnal = 0;
	m_wsVern = 0;
	m_pfxd = pfxd;

	m_qtsf.CreateInstance(CLSID_TsStrFactory);
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwXmlExportData::~FwXmlExportData()
{
	if (m_pfileLog)
	{
		fclose(m_pfileLog);
		m_pfileLog = NULL;
	}
}

/*----------------------------------------------------------------------------------------------
	Write a message to the log file (if one is open).

	@param pszMsg NUL-terminated message string.
----------------------------------------------------------------------------------------------*/
void FwXmlExportData::LogMessage(const char * pszMsg)
{
	AssertPsz(pszMsg);
	if (m_pfileLog)
		fputs(pszMsg, m_pfileLog);
}

/*----------------------------------------------------------------------------------------------
	Store the information needed to output the auxiliary attributes for a Link element that
	points to the given object.

	@param hobj database object id of a list object owned by a PartOfSpeech object.
	@param ws writing system desired for output (analysis in all likelihood)
----------------------------------------------------------------------------------------------*/
void FwXmlExportData::StoreOwnersNameAbbr(int hobj, int ws, int fidAbbrOwner, int fidNameOwner)
{
	int hobjOwner;
	if (!m_hmhobjOwner.Retrieve(hobj, &hobjOwner))
		return;		// should never happen, but ignore it if it does...

	int iabbrPos;
	if (fidAbbrOwner && !m_hmhobjimurAbbrOwner.Retrieve(hobj, &iabbrPos))
	{
		if (m_hmhobjimurAbbr.Retrieve(hobjOwner, &iabbrPos))
		{
			m_hmhobjimurAbbrOwner.Insert(hobj, iabbrPos);
		}
		else
		{
	// Owner of object %<0>d does not have an abbreviation in the default analysis language."
			StrAnsi staFmt(kstidXmlInfoMsg225);
			StrAnsi sta;
			sta.Format(staFmt.Chars(), hobj);
			LogMessage(sta.Chars());
		}
	}
	int inamePos;
	if (fidNameOwner && !m_hmhobjimurNameOwner.Retrieve(hobj, &inamePos))
	{
		if (m_hmhobjimurName.Retrieve(hobjOwner, &inamePos))
		{
			m_hmhobjimurNameOwner.Insert(hobj, inamePos);
		}
		else
		{
			// "Owner of object %<0>d does not have a name in the default analysis language."
			StrAnsi staFmt(kstidXmlInfoMsg224);
			StrAnsi sta;
			sta.Format(staFmt.Chars(), hobj);
			LogMessage(sta.Chars());
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Create the hierarchical value to store for the implicit target attribute for a Link to a
	ReversalIndexEntry object.  This is recursively up the ownership tree to the toplevel
	ReversalIndexEntry.  For a flat ReversalIndex, it won't actually recurse at all!

	@param iudr index into m_vudr for a ReversalIndexEntry object's Form value.
	@param stu Reference to a StrUni object to receive the result.
----------------------------------------------------------------------------------------------*/
void FwXmlExportData::BuildReversalIndexHierForm(int iudr, StrUni & stu)
{
	if (m_vudr[iudr].m_fid != kflidReversalIndexEntry_Form)
		return;		// This is not a value we're interested in.
	int hobjOwner;
	if (!m_hmhobjOwner.Retrieve(m_vudr[iudr].m_hobj, &hobjOwner))
		return;		// This should never happen, but ignore it if it does...
	int iudrOwner;
	if (m_hmhobjiudr.Retrieve(hobjOwner, &iudrOwner))
		BuildReversalIndexHierForm(iudrOwner, stu);		// Recurse to insert owner first.
	if (stu.Length())
		stu.Append(L"|");
	stu.Append(m_vudr[iudr].m_stuValue);
}


/***********************************************************************************************
 * Many (even most or maybe all) of the static functions below could be made methods of the
 * FwXmlExportData class.  Too bad I'll never have time to do that!  (SteveMc)
 **********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Construct a complex query in pxed->m_staCmd for getting all basic attributes of the given
	types in a single trip to the database.

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
	@param pszInnerCmd Select command for obtaining specific types of attribute, and building
					(part of) another select command from the results.
	@param pszCmdHead The very beginning of the SQL query for a specific attribute type.
	@param pszFields Set of fields used by pszCmdHead.
	@param pszUnion The desired union operator, either " union " or " union all ".
	@param pszCmdTail The very end of the SQL query for a specific attribute type.

	@return Number of fields stuffed into the join.
----------------------------------------------------------------------------------------------*/
static int BuildComplexQuery(FwXmlExportData * pxed, SqlDb & sdb, const char * pszInnerCmd,
	const char * pszCmdHead, const char * pszFields, const char * pszUnion,
	const char * pszCmdTail)
{
	SqlStatement sstmt;
	RETCODE rc;
	unsigned char szBuf[StrAnsiBufBig::kcchMaxStr];
	SDWORD cbTxt;
	StrAnsi staFmt;
	StrAnsi staMsg;

	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pszInnerCmd)), SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pszInnerCmd);
	int cFld = 0;
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		rc = SQLGetData(sstmt.Hstmt(), 1, SQL_C_CHAR, &szBuf, isizeof(szBuf), &cbTxt);
		if (rc == SQL_NO_DATA)
			continue;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (pxed->m_staCmd.Length())
		{
			pxed->m_staCmd.Append(pszUnion);
		}
		else
		{
			pxed->m_staCmd.Format(pszCmdHead, pszFields);
		}
		pxed->m_staCmd.Append(reinterpret_cast<char *>(szBuf), cbTxt);
		++cFld;
	}
	sstmt.Clear();
	if (pszCmdTail)
	{
		pxed->m_staCmd.Append(pszCmdTail);
		/* Switched to StrAnsi
		if (pxed->m_staCmd.Overflow())
		{
			// "ERROR [%d]: Constructed Query overflowed its buffer!\n\"%s\"\n"
			staFmt.Load(kstidXmlErrorMsg220);
			staMsg.Format(staFmt.Chars(), __LINE__, pxed->m_staCmd.Chars());
			pxed->LogMessage(staMsg.Chars());
			ThrowHr(WarnHr(E_OUTOFMEMORY));
		} */
	}
	return cFld;
}

static const char g_szBasicCmdHead[] = "select %s from ObjHierarchy$ oh join classpar$ cp on"
	" cp.src=oh.class join (";
static const char g_szBasicCmdTail[] = ") tc on tc.CLASS_ID=cp.dst and tc.OBJ_ID=oh.id where"
	" cp.dst <> 0 and tc.FIELD_VALUE is not NULL order by strDepth, oh.id, tc.FIELD_ID";
static const char g_szGetMostCmd[] = "select 'select id OBJ_ID, ' + cast(f.id as varchar) + ' "
	"FIELD_ID, ' + cast(c.id as varchar) + ' CLASS_ID, cast([' + f.name + '] as varchar(50))"
	" FIELD_VALUE from ' + c.name STR_SQL from field$ f join class$ c on f.class=c.id"
	" where type in (0,1,2,3,4,6,8)";
static const char g_szGetTimeCmd[] = "select 'select id OBJ_ID, ' + cast(f.id as varchar) + "
	"' FIELD_ID, ' + cast(c.id as varchar) + ' CLASS_ID, convert(varchar,[' + f.name + '],121) "
	"FIELD_VALUE from ' + c.name STR_SQL from field$ f join class$ c on f.class=c.id where "
	"type = 5";
static const char g_szFieldsWithType[] = "oh.id, tc.FIELD_ID, FIELD_VALUE";

/*----------------------------------------------------------------------------------------------
	Load all of the basic attributes from the database.
@line		0 = kcptNil
@line		1 = kcptBoolean
@line		2 = kcptInteger
@line		3 = kcptNumeric
@line		4 = kcptFloat
@line		6 = kcptGuid
@line		8 = kcptGenDate
@line		5 = kcptTime (handled a little differently)

		@param pxed Pointer to the XML export data.
		@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
static void LoadBasicAttributeRows(FwXmlExportData * pxed, SqlDb & sdb)
{
	SqlStatement sstmt;
	RETCODE rc;
	char szValue[200];	// FIELD_VALUE
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbValue;
	BasicDataRow bdr;
	int cFld;

	pxed->m_staCmd.Clear();
	cFld = BuildComplexQuery(pxed, sdb, g_szGetMostCmd, g_szBasicCmdHead, g_szFieldsWithType,
		" union ", NULL);
	cFld += BuildComplexQuery(pxed, sdb, g_szGetTimeCmd, g_szBasicCmdHead, g_szFieldsWithType,
		" union ", g_szBasicCmdTail);
	if (!cFld)
		return;

	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pxed->m_staCmd.Chars())), SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pxed->m_staCmd.Chars());
	// Read the entire recordset into memory.
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &bdr.m_hobj, isizeof(bdr.m_hobj), &cbObjId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &bdr.m_fid, isizeof(bdr.m_fid), &cbFieldId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_CHAR, &szValue, isizeof(szValue), &cbValue);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	pxed->m_vbdr.EnsureSpace(10000);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbValue == SQL_NULL_DATA || !cbValue)
			continue;
		bdr.m_staValue.Assign(szValue, cbValue);
		pxed->m_vbdr.Push(bdr);
	}
	pxed->m_vbdr.EnsureSpace(0, true);
	sstmt.Clear();
	pxed->m_pbdr = pxed->m_vbdr.Begin();
}

static const char g_szGetBinaryCmd[] = "select 'select id OBJ_ID, ' + cast(f.id as varchar) + "
	"' FIELD_ID, ' + cast(c.id as varchar) + ' CLASS_ID, [' + "
	"f.name + '] FIELD_VALUE from ' + c.name STR_SQL from field$ f join class$ c on "
	"f.class=c.id where type = 9";
static char g_szFields[] = "oh.id, tc.FIELD_ID, FIELD_VALUE";

/*----------------------------------------------------------------------------------------------
	Load all of the "Binary" attributes from the database.
@line		 9 = kcptBinary

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
static void LoadBinaryAttributeRows(FwXmlExportData * pxed, SqlDb & sdb)
{
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbValue;
	BinaryDataRow bir;

	pxed->m_staCmd.Clear();
	if (!BuildComplexQuery(pxed, sdb, g_szGetBinaryCmd, g_szBasicCmdHead, g_szFields,
		" union all ", g_szBasicCmdTail))
	{
		return;
	}
	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pxed->m_staCmd.Chars())), SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pxed->m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &bir.m_hobj, isizeof(bir.m_hobj), &cbObjId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &bir.m_fid, isizeof(bir.m_fid), &cbFieldId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_BINARY, pxed->m_vbFmt.Begin(), pxed->m_vbFmt.Size(),
		&cbValue);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	pxed->m_vbirBinary.EnsureSpace(100);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbValue == SQL_NULL_DATA || !cbValue)
			continue;
		pxed->m_vbirBinary.Push(bir);
		pxed->m_vbirBinary.Top()->m_vbValue.Resize(cbValue);
		memcpy(pxed->m_vbirBinary.Top()->m_vbValue.Begin(), pxed->m_vbFmt.Begin(), cbValue);
	}
	pxed->m_vbirBinary.EnsureSpace(0, true);
	sstmt.Clear();
	pxed->m_pbirBinary = pxed->m_vbirBinary.Begin();
}

static const char g_szGetImageCmd[] = "select 'select id OBJ_ID, ' + cast(f.id as varchar) + ' "
	"FIELD_ID, ' + cast(c.id as varchar) + ' CLASS_ID, [' + "
	"f.name + '] FIELD_VALUE from ' + c.name STR_SQL from field$ f join class$ c on "
	"f.class=c.id where type = 7";

/*----------------------------------------------------------------------------------------------
	Load all of the "Image" attributes from the database.
@line		 7 = kcptImage

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
static void LoadImageAttributeRows(FwXmlExportData * pxed, SqlDb & sdb)
{
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbTotal;
	BinaryDataRow bir;

	pxed->m_staCmd.Clear();
	if (!BuildComplexQuery(pxed, sdb, g_szGetImageCmd, g_szBasicCmdHead, g_szFields,
		" union all ", g_szBasicCmdTail))
	{
		return;
	}
	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pxed->m_staCmd.Chars())), SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pxed->m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &bir.m_hobj, isizeof(bir.m_hobj), &cbObjId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &bir.m_fid, isizeof(bir.m_fid), &cbFieldId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	int cbT;
	byte * pbBuf;
	pxed->m_vbirImage.EnsureSpace(100);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		rc = SQLGetData(sstmt.Hstmt(), 3, SQL_C_BINARY, pxed->m_vbFmt.Begin(),
			pxed->m_vbFmt.Size(), &cbTotal);
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA || cbTotal == SQL_NULL_DATA || !cbTotal)
			continue;
		cbT = pxed->m_vbFmt.Size();
		if (cbT > cbTotal)
			cbT = cbTotal;
		pxed->m_vbirImage.Push(bir);
		pxed->m_vbirImage.Top()->m_vbValue.Resize(cbTotal);
		pbBuf = pxed->m_vbirImage.Top()->m_vbValue.Begin();
		memcpy(pbBuf, pxed->m_vbFmt.Begin(), cbT);
		// Get the rest of the data if necessary.
		if (cbTotal > cbT)
		{
			rc = SQLGetData(sstmt.Hstmt(), 3, SQL_C_BINARY, pbBuf + cbT, cbTotal - cbT,
				&cbTotal);
			pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		}
	}
	pxed->m_vbirImage.EnsureSpace(0, true);
	sstmt.Clear();
	pxed->m_pbirImage = pxed->m_vbirImage.Begin();
}

static const char g_szGetUnicodeCmd[] = "select 'select id OBJ_ID, ' + cast(f.id as varchar) +"
	" ' FIELD_ID, ' + cast(c.id as varchar) + ' CLASS_ID, [' + "
	"f.name + '] FIELD_VALUE from ' + c.name STR_SQL from field$ f join class$ c on "
	"f.class=c.id where type = 15";

/*----------------------------------------------------------------------------------------------
	Load all of the "Unicode" attributes from the database.
@line		15 = kcptUnicode

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
static void LoadUnicodeRows(FwXmlExportData * pxed, SqlDb & sdb)
{
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbValue;
	UnicodeDataRow udr;

	pxed->m_staCmd.Clear();
	if (!BuildComplexQuery(pxed, sdb, g_szGetUnicodeCmd, g_szBasicCmdHead, g_szFields,
		" union all ", g_szBasicCmdTail))
	{
		return;
	}
	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pxed->m_staCmd.Chars())), SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pxed->m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &udr.m_hobj, isizeof(udr.m_hobj), &cbObjId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &udr.m_fid, isizeof(udr.m_fid), &cbFieldId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_WCHAR, pxed->m_vchTxt.Begin(),
		pxed->m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	pxed->m_vudr.EnsureSpace(1000);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbValue == SQL_NULL_DATA || !cbValue)
			continue;
		pxed->m_vudr.Push(udr);
		pxed->m_vudr.Top()->m_stuValue.Assign(pxed->m_vchTxt.Begin(), cbValue/isizeof(wchar));
	}
	pxed->m_vudr.EnsureSpace(0, true);
	sstmt.Clear();
	pxed->m_pudr = pxed->m_vudr.Begin();
}

static const char g_szGetBigUnicodeCmd[] = "select 'select id OBJ_ID, ' + cast(f.id as varchar)"
	" +  'FIELD_ID, ' + cast(c.id as varchar) + ' CLASS_ID, [' + "
	"f.name + '] FIELD_VALUE from ' + c.name STR_SQL from field$ f join class$ c on "
	"f.class=c.id where type = 19";

/*----------------------------------------------------------------------------------------------
	Load all of the "BigUnicode" attributes from the database.
@line		19 = kcptBigUnicode

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
static void LoadBigUnicodeRows(FwXmlExportData * pxed, SqlDb & sdb)
{
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbValue;
	UnicodeDataRow udr;

	pxed->m_staCmd.Clear();
	if (!BuildComplexQuery(pxed, sdb, g_szGetBigUnicodeCmd, g_szBasicCmdHead, g_szFields,
		" union all ", g_szBasicCmdTail))
	{
		return;
	}
	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pxed->m_staCmd.Chars())), SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pxed->m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &udr.m_hobj, isizeof(udr.m_hobj), &cbObjId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &udr.m_fid, isizeof(udr.m_fid), &cbFieldId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	int cch;
	int cch1;
	int cchTotal;
	pxed->m_vudrBig.EnsureSpace(100);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		rc = SQLGetData(sstmt.Hstmt(), 3, SQL_C_WCHAR, pxed->m_vchTxt.Begin(),
			pxed->m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA || cbValue == SQL_NULL_DATA || !cbValue)
			continue;
		cch = pxed->m_vchTxt.Size();
		cchTotal = cbValue / isizeof(SQLWCHAR);
		if (cch > cchTotal)
			cch = cchTotal;
		if (!pxed->m_vchTxt[cch - 1])
			cch1 = cch - 1;							// Don't include trailing NUL.
		else
			cch1 = cch;
		pxed->m_vudrBig.Push(udr);
		pxed->m_vudrBig.Top()->m_stuValue.Assign(pxed->m_vchTxt.Begin(), cch1);

		// Get the rest of the string if necessary.
		if (cchTotal > cch)
		{
			Assert(rc == SQL_SUCCESS_WITH_INFO);
#ifdef VERBOSE_EXPORT_LOGGING
			// "    Reading %<0>d additional characters of string data.\n"
			StrAnsi staFmt(kstidXmlInfoMsg221);
			StrAnsi staMsg;
			staMsg.Format(staFmt.Chars(), cchTotal - cch1);
			pxed->LogMessage(staMsg.Chars());
#endif /*VERBOSE_EXPORT_LOGGING*/
			pxed->m_vchTxt.Resize(cchTotal + 1);
			rc = SQLGetData(sstmt.Hstmt(), 3, SQL_C_WCHAR, pxed->m_vchTxt.Begin(),
				pxed->m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
			pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			pxed->m_vudrBig.Top()->m_stuValue.Append(pxed->m_vchTxt.Begin(), cchTotal - cch1);
		}
	}
	pxed->m_vudrBig.EnsureSpace(0, true);
	sstmt.Clear();
	pxed->m_pudrBig = pxed->m_vudrBig.Begin();
}

static const char g_szGetStringCmd[] = "select 'select id OBJ_ID, ' + cast(f.id as varchar) + "
	"' FIELD_ID, ' + cast(c.id as varchar) + ' CLASS_ID, [' + f.name + '] FIELD_VALUE, ' + "
	"f.name + '_Fmt FIELD_FMT from ' + c.name STR_SQL from field$ f"
	" join class$ c on f.class=c.id where type = 13";
static char g_szFieldsWithFormat[] = "oh.id, tc.FIELD_ID, FIELD_VALUE, FIELD_FMT";

/*----------------------------------------------------------------------------------------------
	Load all of the "String" attributes from the database.
@line		13 = kcptString

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
static void LoadStringRows(FwXmlExportData * pxed, SqlDb & sdb)
{
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbValue;
	SDWORD cbFmt;
	StringDataRow sdr;

	pxed->m_staCmd.Clear();
	if (!BuildComplexQuery(pxed, sdb, g_szGetStringCmd, g_szBasicCmdHead, g_szFieldsWithFormat,
		" union all ", g_szBasicCmdTail))
	{
		return;
	}
	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pxed->m_staCmd.Chars())), SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pxed->m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &sdr.m_hobj, isizeof(sdr.m_hobj), &cbObjId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &sdr.m_fid, isizeof(sdr.m_fid), &cbFieldId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_WCHAR, pxed->m_vchTxt.Begin(),
		pxed->m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 4, SQL_C_BINARY, pxed->m_vbFmt.Begin(), pxed->m_vbFmt.Size(),
		&cbFmt);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	pxed->m_vsdr.EnsureSpace(1000);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbValue == SQL_NULL_DATA || !cbValue)
			continue;
		pxed->m_vsdr.Push(sdr);
		pxed->m_vsdr.Top()->m_stuValue.Assign(pxed->m_vchTxt.Begin(), cbValue/isizeof(wchar));
		if (cbFmt && cbFmt != SQL_NULL_DATA)
		{
			pxed->m_vsdr.Top()->m_vbFmt.Resize(cbFmt);
			memcpy(pxed->m_vsdr.Top()->m_vbFmt.Begin(), pxed->m_vbFmt.Begin(), cbFmt);
		}
	}
	pxed->m_vsdr.EnsureSpace(0, true);
	sstmt.Clear();
	pxed->m_psdr = pxed->m_vsdr.Begin();
}

static const char g_szGetBigStringCmd[] = "select 'select id OBJ_ID, ' + cast(f.id as varchar)"
	" + ' FIELD_ID, ' + cast(c.id as varchar) + ' CLASS_ID, [' + f.name + '] FIELD_VALUE, ' + "
	"f.name + '_Fmt FIELD_FMT from ' + c.name STR_SQL from field$ f"
	" join class$ c on f.class=c.id where type = 17";

/*----------------------------------------------------------------------------------------------
	Load all of the "BigString" attributes from the database.
@line		17 = kcptBigString

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
static void LoadBigStringRows(FwXmlExportData * pxed, SqlDb & sdb)
{
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbValue;
	SDWORD cbFmt;
	StringDataRow sdr;

	pxed->m_staCmd.Clear();
	if (!BuildComplexQuery(pxed, sdb, g_szGetBigStringCmd, g_szBasicCmdHead,
		g_szFieldsWithFormat, " union all ", g_szBasicCmdTail))
	{
		return;
	}
	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pxed->m_staCmd.Chars())), SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pxed->m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &sdr.m_hobj, isizeof(sdr.m_hobj), &cbObjId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &sdr.m_fid, isizeof(sdr.m_fid), &cbFieldId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	int cch;
	int cch1;
	int cchTotal;
	int cbT;
	byte * pbBuf;
	pxed->m_vsdrBig.EnsureSpace(100);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		// Read the string data.
		rc = SQLGetData(sstmt.Hstmt(), 3, SQL_C_WCHAR, pxed->m_vchTxt.Begin(),
			pxed->m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA || cbValue == SQL_NULL_DATA || !cbValue)
			continue;
		cch = pxed->m_vchTxt.Size();
		cchTotal = cbValue / isizeof(SQLWCHAR);
		if (cch > cchTotal)
			cch = cchTotal;
		if (!pxed->m_vchTxt[cch - 1])
			cch1 = cch - 1;							// Don't include trailing NUL.
		else
			cch1 = cch;
		pxed->m_vsdrBig.Push(sdr);
		pxed->m_vsdrBig.Top()->m_stuValue.Assign(pxed->m_vchTxt.Begin(), cch1);

		// Get the rest of the string if necessary.
		if (cchTotal > cch)
		{
			Assert(rc == SQL_SUCCESS_WITH_INFO);
#ifdef VERBOSE_EXPORT_LOGGING
			// "    Reading %<0>d additional characters of string data.\n"
			StrAnsi staFmt(kstidXmlInfoMsg221);
			StrAnsi staMsg;
			staMsg.Format(staFmt.Chars(), cchTotal - cch1);
			pxed->LogMessage(staMsg.Chars());
#endif /*VERBOSE_EXPORT_LOGGING*/
			pxed->m_vchTxt.Resize(cchTotal + 1);
			rc = SQLGetData(sstmt.Hstmt(), 3, SQL_C_BINARY, pxed->m_vchTxt.Begin(),
				pxed->m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
			pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			pxed->m_vsdrBig.Top()->m_stuValue.Append(pxed->m_vchTxt.Begin(), cchTotal - cch1);
		}
		// Read the formatting data.
		rc = SQLGetData(sstmt.Hstmt(), 4, SQL_C_BINARY, pxed->m_vbFmt.Begin(),
			pxed->m_vbFmt.Size(), &cbFmt);
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA || cbFmt == SQL_NULL_DATA || !cbFmt)
			continue;
		cbT = pxed->m_vbFmt.Size();
		if (cbT > cbFmt)
			cbT = cbFmt;
		pxed->m_vsdrBig.Top()->m_vbFmt.Resize(cbFmt);
		pbBuf = pxed->m_vsdrBig.Top()->m_vbFmt.Begin();
		memcpy(pbBuf, pxed->m_vbFmt.Begin(), cbT);
		// Get the rest of the formatting data if necessary.
		if (cbFmt > cbT)
		{
			Assert(rc == SQL_SUCCESS_WITH_INFO);
#ifdef VERBOSE_EXPORT_LOGGING
			// "    Reading %<0>d additional bytes of format data.\n"
			StrAnsi staFmt(kstidXmlInfoMsg222);
			StrAnsi staMsg;
			staMsg.Format(staFmt.Chars(), cbFmt - cbT);
			pxed->LogMessage(staMsg.Chars());
#endif /*VERBOSE_EXPORT_LOGGING*/
			rc = SQLGetData(sstmt.Hstmt(), 4, SQL_C_BINARY, pbBuf + cbT, cbFmt - cbT, &cbFmt);
			pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			pxed->m_vbFmt.Resize(cbFmt);
		}
	}
	pxed->m_vsdrBig.EnsureSpace(0, true);
	sstmt.Clear();
	pxed->m_psdrBig = pxed->m_vsdrBig.Begin();
}

/*static SQLCHAR g_szMultiTxtCmd[] = "select oh.id, Flid, ws, Txt from ObjHierarchy$ oh"
	" join multiTxt$ tc on tc.obj=oh.id order by strDepth, oh.id, Flid, ws";*/

static const char g_szGetMultiUnicode[] =
	"select 'select ' + "
		"cast(f.Class as varchar) + ' AS ClassId, ' + "
		"cast(f.id as varchar) + ' AS FieldId, Obj, Ws, Txt "
	"from ' + c.name + '_' + f.name AS StrSql from field$ f "
	"join class$ c on f.class=c.id "
	"where f.Type = 16 ";
static const char g_szMultiUnicodeFields[] =
	"oh.id, FieldId, Ws, Txt";
static const char g_szMultiTxtCmdTail[] = ") tc on tc.ClassId = cp.dst and tc.Obj = oh.id "
	"where cp.dst <> 0 and tc.Txt is not NULL "
	"order by strDepth, oh.id, tc.FieldId";


/*----------------------------------------------------------------------------------------------
	Load all of the "MultiUnicode" attributes from the database.
@line		16 = kcptMultiUnicode

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
void FwXmlData::LoadMultiUnicodeRows(FwXmlExportData * pxed, SqlDb & sdb)
{

	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbValue;
	SDWORD cbWs;
	MultiUnicodeDataRow mur;

	pxed->m_staCmd.Clear();
	if (!BuildComplexQuery(pxed, sdb, g_szGetMultiUnicode, g_szBasicCmdHead, g_szMultiUnicodeFields,
		" union all ", g_szMultiTxtCmdTail))
	{
		return;
	}
	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pxed->m_staCmd.Chars())), SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pxed->m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &mur.m_hobj, isizeof(mur.m_hobj), &cbObjId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &mur.m_fid, isizeof(mur.m_fid), &cbFieldId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_SLONG, &mur.m_ws, isizeof(mur.m_ws), &cbWs);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 4, SQL_C_WCHAR, pxed->m_vchTxt.Begin(),
		pxed->m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	pxed->m_vmur.EnsureSpace(1000);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbValue == SQL_NULL_DATA || !cbValue)
			continue;
		pxed->m_vmur.Push(mur);
		pxed->m_vmur.Top()->m_stuValue.Assign(pxed->m_vchTxt.Begin(), cbValue/isizeof(wchar));
	}
	pxed->m_vmur.EnsureSpace(0, true);
	sstmt.Clear();
	pxed->m_pmur = pxed->m_vmur.Begin();

/*
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbWs;
	SDWORD cbValue;
	MultiUnicodeDataRow mur;

	for {int ifld = 0; ifld < m_vfdfi.Size(); ++ifld}
	{
		if (m_vfdfi[ifld].cpt != kcptMultiUnicode)
			continue;
		int icls;
		if (!m_hmcidicls.Retrieve(m_vfdfi[ifld].cid, icls))
			continue; // Perhaps we should complain...
		StrAnsi staQuery;
		staQuery.Format("SELECT Obj, Ws, Txt FROM %S_%S",
			m_vstucls[icls].Chars(), m_vstufld[ifld].Chars());

		sstmt.Init(sdb);
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(staQuery.Chars())), SQL_NTS);
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, staQuery.Chars());
		rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &mur.m_hobj, isizeof(mur.m_hobj), &cbObjId);
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &mur.m_ws, isizeof(mur.m_ws), &cbWs);
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_WCHAR, pxed->m_vchTxt.Begin(),
			pxed->m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		pxed->m_vmur.EnsureSpace(1000);

		mur.m_fid = m_vfdfi[ifld].fid;

		for (;;)
		{
			rc = SQLFetch(sstmt.Hstmt());
			pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			if (rc == SQL_NO_DATA)
				break;
			if (rc != SQL_SUCCESS)
				ThrowHr(WarnHr(E_UNEXPECTED));
			if (cbValue == SQL_NULL_DATA || !cbValue)
				continue;

			pxed->m_vmur.Push(mur);
			pxed->m_vmur.Top()->m_stuValue.Assign(pxed->m_vchTxt.Begin(), cbValue/isizeof(wchar));
		}
		sstmt.Clear();
	}
	pxed->m_vmur.EnsureSpace(0, true);
	pxed->m_pmur = pxed->m_vmur.Begin();
	*/
}

static SQLCHAR g_szMultiBigTxtCmd[] = "select oh.id, Flid, ws, Txt from ObjHierarchy$ oh"
	" join multiBigTxt$ tc on tc.obj=oh.id order by strDepth, oh.id, Flid, tc.ws";

/*----------------------------------------------------------------------------------------------
	Load all of the "MultiBigUnicode" attributes from the database.
@line		20 = kcptMultiBigUnicode

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
static void LoadMultiBigUnicodeRows(FwXmlExportData * pxed, SqlDb & sdb)
{
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbWs;
	SDWORD cbValue;
	MultiUnicodeDataRow mur;

	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(), g_szMultiBigTxtCmd, SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__,
		reinterpret_cast<char *>(g_szMultiBigTxtCmd));
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &mur.m_hobj, isizeof(mur.m_hobj), &cbObjId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &mur.m_fid, isizeof(mur.m_fid), &cbFieldId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_SLONG, &mur.m_ws, isizeof(mur.m_ws), &cbWs);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	pxed->m_vmurBig.EnsureSpace(1000);
	int cch;
	int cch1;
	int cchTotal;
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		rc = SQLGetData(sstmt.Hstmt(), 4, SQL_C_WCHAR, pxed->m_vchTxt.Begin(),
			pxed->m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA || cbValue == SQL_NULL_DATA || !cbValue)
			continue;
		cch = pxed->m_vchTxt.Size();
		cchTotal = cbValue / isizeof(SQLWCHAR);
		if (cch > cchTotal)
			cch = cchTotal;
		if (!pxed->m_vchTxt[cch - 1])
			cch1 = cch - 1;							// Don't include trailing NUL.
		else
			cch1 = cch;
		pxed->m_vmurBig.Push(mur);
		pxed->m_vmurBig.Top()->m_stuValue.Assign(pxed->m_vchTxt.Begin(), cch1);

		// Get the rest of the string if necessary.
		if (cchTotal > cch)
		{
			Assert(rc == SQL_SUCCESS_WITH_INFO);
#ifdef VERBOSE_EXPORT_LOGGING
			// "    Reading %<0>d additional characters of string data.\n"
			StrAnsi staFmt(kstidXmlInfoMsg221);
			StrAnsi staMsg;
			staMsg.Format(staFmt.Chars(), cchTotal - cch1);
			pxed->LogMessage(staMsg.Chars());
#endif /*VERBOSE_EXPORT_LOGGING*/
			pxed->m_vchTxt.Resize(cchTotal + 1);
			rc = SQLGetData(sstmt.Hstmt(), 3, SQL_C_BINARY, pxed->m_vchTxt.Begin(),
				pxed->m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
			pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			pxed->m_vmurBig.Top()->m_stuValue.Append(pxed->m_vchTxt.Begin(), cchTotal - cch1);
		}
	}
	pxed->m_vmurBig.EnsureSpace(0, true);
	sstmt.Clear();
	pxed->m_pmurBig = pxed->m_vmurBig.Begin();
}

static SQLCHAR g_szMultiStrCmd[] = "select oh.id, Flid, Ws, Txt, Fmt from "
	"ObjHierarchy$ oh join multiStr$ tc on tc.obj=oh.id order by strDepth, oh.id, Flid, ws";

/*----------------------------------------------------------------------------------------------
	Load all of the "MultiString" attributes from the database.
@line		14 = kcptMultiString

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
static void LoadMultiStringRows(FwXmlExportData * pxed, SqlDb & sdb)
{
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbWs;
	SDWORD cbValue;
	SDWORD cbFmt;
	MultiStringDataRow msr;

	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(), g_szMultiStrCmd, SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, reinterpret_cast<char *>(g_szMultiStrCmd));
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &msr.m_hobj, isizeof(msr.m_hobj), &cbObjId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &msr.m_fid, isizeof(msr.m_fid), &cbFieldId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_SLONG, &msr.m_ws, isizeof(msr.m_ws), &cbWs);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 4, SQL_C_WCHAR, pxed->m_vchTxt.Begin(),
		pxed->m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 5, SQL_C_BINARY, pxed->m_vbFmt.Begin(), pxed->m_vbFmt.Size(),
		&cbFmt);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	pxed->m_vmsr.EnsureSpace(1000);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbValue == SQL_NULL_DATA || !cbValue)
			continue;
		pxed->m_vmsr.Push(msr);
		pxed->m_vmsr.Top()->m_stuValue.Assign(pxed->m_vchTxt.Begin(), cbValue/isizeof(wchar));
		if (cbFmt && cbFmt != SQL_NULL_DATA)
		{
			pxed->m_vmsr.Top()->m_vbFmt.Resize(cbFmt);
			memcpy(pxed->m_vmsr.Top()->m_vbFmt.Begin(), pxed->m_vbFmt.Begin(), cbFmt);
		}
	}
	pxed->m_vmsr.EnsureSpace(0, true);
	sstmt.Clear();
	pxed->m_pmsr = pxed->m_vmsr.Begin();
}

static SQLCHAR g_szMultiBigStrCmd[] = "select oh.id, Flid, Ws, Txt, Fmt from "
	"ObjHierarchy$ oh join multiBigStr$ tc on tc.obj=oh.id order by strDepth, oh.id, Flid, ws";

/*----------------------------------------------------------------------------------------------
	Load all of the "MultiBigString" attributes from the database.
@line		18 = kcptMultiBigString

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
static void LoadMultiBigStringRows(FwXmlExportData * pxed, SqlDb & sdb)
{
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbWs;
	SDWORD cbValue;
	SDWORD cbFmt;
	MultiStringDataRow msr;

	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(), g_szMultiBigStrCmd, SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__,
		reinterpret_cast<char *>(g_szMultiBigStrCmd));
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &msr.m_hobj, isizeof(msr.m_hobj), &cbObjId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &msr.m_fid, isizeof(msr.m_fid), &cbFieldId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_SLONG, &msr.m_ws, isizeof(msr.m_ws), &cbWs);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	pxed->m_vmsrBig.EnsureSpace(1000);
	int cch;
	int cch1;
	int cchTotal;
	int cbT;
	byte * pbBuf;
	pxed->m_vmsrBig.EnsureSpace(1000);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		// Read the string data.
		rc = SQLGetData(sstmt.Hstmt(), 4, SQL_C_WCHAR, pxed->m_vchTxt.Begin(),
			pxed->m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA || cbValue == SQL_NULL_DATA || !cbValue)
			continue;
		cch = pxed->m_vchTxt.Size();
		cchTotal = cbValue / isizeof(SQLWCHAR);
		if (cch > cchTotal)
			cch = cchTotal;
		if (!pxed->m_vchTxt[cch - 1])
			cch1 = cch - 1;							// Don't include trailing NUL.
		else
			cch1 = cch;
		pxed->m_vmsrBig.Push(msr);
		pxed->m_vmsrBig.Top()->m_stuValue.Assign(pxed->m_vchTxt.Begin(), cch1);

		// Get the rest of the string if necessary.
		if (cchTotal > cch)
		{
			Assert(rc == SQL_SUCCESS_WITH_INFO);
#ifdef VERBOSE_EXPORT_LOGGING
			// "    Reading %<0>d additional characters of string data.\n"
			StrAnsi staFmt(kstidXmlInfoMsg221);
			StrAnsi staMsg;
			staMsg.Format(staFmt.Chars(), cchTotal - cch1);
			pxed->LogMessage(staMsg.Chars());
#endif /*VERBOSE_EXPORT_LOGGING*/
			pxed->m_vchTxt.Resize(cchTotal + 1);
			rc = SQLGetData(sstmt.Hstmt(), 4, SQL_C_BINARY, pxed->m_vchTxt.Begin(),
				pxed->m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
			pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			pxed->m_vmsrBig.Top()->m_stuValue.Append(pxed->m_vchTxt.Begin(), cchTotal - cch1);
		}
		// Read the formatting data.
		rc = SQLGetData(sstmt.Hstmt(), 5, SQL_C_BINARY, pxed->m_vbFmt.Begin(),
			pxed->m_vbFmt.Size(), &cbFmt);
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA || cbFmt == SQL_NULL_DATA || !cbFmt)
			continue;
		cbT = pxed->m_vbFmt.Size();
		if (cbT > cbFmt)
			cbT = cbFmt;
		pxed->m_vmsrBig.Top()->m_vbFmt.Resize(cbFmt);
		pbBuf = pxed->m_vmsrBig.Top()->m_vbFmt.Begin();
		memcpy(pbBuf, pxed->m_vbFmt.Begin(), cbT);
		// Get the rest of the formatting data if necessary.
		if (cbFmt > cbT)
		{
			Assert(rc == SQL_SUCCESS_WITH_INFO);
#ifdef VERBOSE_EXPORT_LOGGING
			// "    Reading %<0>d additional bytes of format data.\n"
			StrAnsi staFmt(kstidXmlInfoMsg222);
			StrAnsi staMsg;
			staMsg.Format(staFmt.Chars(), cbFmt - cbT);
			pxed->LogMessage(staMsg.Chars());
#endif /*VERBOSE_EXPORT_LOGGING*/
			rc = SQLGetData(sstmt.Hstmt(), 5, SQL_C_BINARY, pbBuf + cbT, cbFmt - cbT, &cbFmt);
			pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			pxed->m_vbFmt.Resize(cbFmt);
		}
	}
	pxed->m_vmsrBig.EnsureSpace(0, true);
	sstmt.Clear();
	pxed->m_pmsrBig = pxed->m_vmsrBig.Begin();
}


static const char g_szGetRefCollectionCmd[] =
	"select 'select ' + cast(f.id as varchar) + ' FIELD_ID, src, dst, -1 ORD_VAL from '"
	" + csrc.name + '_' + f.name STR_SQL from field$ f "
	"join class$ cSrc on f.class=cSrc.id "
	"join class$ cDst on f.dstcls=cDst.id "
	"where f.Type = 26 "
	"union "
	"select 'select ' + cast(f.id as varchar) + ' FIELD_ID, src, dst, ord ORD_VAL from '"
	" + csrc.name + '_' + f.name STR_SQL from field$ f "
	"join class$ cSrc on f.class=cSrc.id "
	"join class$ cDst on f.dstcls=cDst.id "
	"where f.Type = 28 "
	"order by STR_SQL";
static const char g_szRefSeqFields[] = "oh.id, FIELD_ID, Dst, ORD_VAL";
static const char g_szRefCmdHead[] = "select %s from ObjHierarchy$ oh join (";
static const char g_szRefSeqCmdTail[] = ") REFS on oh.id=REFS.src "
	"order by strDepth, oh.id, REFS.FIELD_ID, REFS.ORD_VAL";

/*----------------------------------------------------------------------------------------------
	Load all of the "ReferenceCollection" and "ReferenceSequence" attributes from the database.
@line		26 = kcptReferenceCollection
@line		28 = kcptReferenceSequence

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
static void LoadRefCollectionRows(FwXmlExportData * pxed, SqlDb & sdb)
{
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbDst;
	SDWORD cbOrd;
	ReferenceCollectionDataRow rcd;

	pxed->m_staCmd.Clear();
	if (!BuildComplexQuery(pxed, sdb, g_szGetRefCollectionCmd, g_szRefCmdHead, g_szRefSeqFields,
		" union ", g_szRefSeqCmdTail))
	{
		return;
	}
	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pxed->m_staCmd.Chars())), SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pxed->m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &rcd.m_hobj, isizeof(rcd.m_hobj), &cbObjId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &rcd.m_fid, isizeof(rcd.m_fid), &cbFieldId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_SLONG, &rcd.m_hobjDst, isizeof(rcd.m_hobjDst),
		&cbDst);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 4, SQL_C_SLONG, &rcd.m_ord, isizeof(rcd.m_ord), &cbOrd);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	pxed->m_vrcd.EnsureSpace(1000);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		pxed->m_vrcd.Push(rcd);
	}
	pxed->m_vrcd.EnsureSpace(0, true);
	sstmt.Clear();
	pxed->m_prcd = pxed->m_vrcd.Begin();
}

static const char g_szGetRefAtomCmd[] =
	"select 'select id, ' + cast(f.id as varchar) + ' FIELD_ID, [' + f.name + '] REF_VAL "
	"from ' + cSrc.name + ' where [' + f.name + '] is not NULL ' STR_SQL from field$ f "
	"join class$ cSrc on f.class=cSrc.id "
	"join class$ cDst on f.dstcls=cDst.id "
	"where f.Type = 24";
static const char g_szRefAtomFields[] = "oh.id, FIELD_ID, REF_VAL";
static const char g_szRefAtomCmdTail[] = ") REF_ATOMS on oh.id=REF_ATOMS.id "
	"order by strDepth, oh.id, REF_ATOMS.FIELD_ID";

/*----------------------------------------------------------------------------------------------
	Load all of the "ReferenceAtom" attributes from the database.
@line		24 = kcptReferenceAtom

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
static void LoadRefAtomRows(FwXmlExportData * pxed, SqlDb & sdb)
{
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbFieldId;
	SDWORD cbDst;
	ReferenceAtomDataRow rad;

	pxed->m_staCmd.Clear();
	if (!BuildComplexQuery(pxed, sdb, g_szGetRefAtomCmd, g_szRefCmdHead, g_szRefAtomFields,
		" union ", g_szRefAtomCmdTail))
	{
		return;
	}
	sstmt.Init(sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(pxed->m_staCmd.Chars())), SQL_NTS);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, pxed->m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &rad.m_hobj, isizeof(rad.m_hobj), &cbObjId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &rad.m_fid, isizeof(rad.m_fid), &cbFieldId);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_SLONG, &rad.m_hobjDst, isizeof(rad.m_hobjDst),
		&cbDst);
	pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	pxed->m_vrad.EnsureSpace(1000);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		pxed->VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		pxed->m_vrad.Push(rad);
	}
	pxed->m_vrad.EnsureSpace(0, true);
	sstmt.Clear();
	pxed->m_prad = pxed->m_vrad.Begin();
}

/*----------------------------------------------------------------------------------------------
	Load the entire object ownership hierarchy table from the database.
@line		23 = kcptOwningAtom
@line		25 = kcptOwningCollection
@line		27 = kcptOwningSequence

	@param pxed Pointer to the XML export data.
	@param sdb Reference to the open SQL database ODBC connection.
----------------------------------------------------------------------------------------------*/
void FwXmlExportData::LoadObjHierarchy()
{
	SqlStatement sstmt;
	RETCODE rc;
	ObjHierarchyDataRow ohd;
	SDWORD cbDepth;
	SDWORD cbFidOwner;
	SDWORD cbHobj;
	SDWORD cbCid;
	SDWORD cbGuid;

	m_staCmd = "select intDepth, ownFlid, id, class, guid from ObjHierarchy$ "
		"order by strDepth, id, ownFlid";
	sstmt.Init(m_pfxd->m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(m_staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &ohd.m_depth, isizeof(ohd.m_depth),
		&cbDepth);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &ohd.m_fidOwner, isizeof(ohd.m_fidOwner),
		&cbFidOwner);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_SLONG, &ohd.m_hobj, isizeof(ohd.m_hobj), &cbHobj);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 4, SQL_C_SLONG, &ohd.m_cid, isizeof(ohd.m_cid), &cbCid);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 5, SQL_C_GUID, &ohd.m_guid, isizeof(ohd.m_guid), &cbGuid);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	m_vohd.EnsureSpace(10000);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (ohd.m_cid == kclidLangProject)
			m_hobjLP = ohd.m_hobj;
		m_vohd.Push(ohd);
	}
	m_vohd.EnsureSpace(0, true);
	sstmt.Clear();

	// cache the mapping from object id to object guid
	m_cobj = m_vohd.Size();
	m_mphobjguid.Resize(m_cobj);
	m_hmhobjguid.Clear();
	int hobj;
	GUID * pguid;
	for (int i = 0; i < m_cobj; ++i)
	{
		hobj = m_vohd[i].m_hobj;
		pguid = &m_vohd[i].m_guid;
		if (hobj < m_cobj)
			m_mphobjguid[hobj] = pguid;
		else
			m_hmhobjguid.Insert(hobj, pguid);
	}
}

/*----------------------------------------------------------------------------------------------
	Load the object ids and owner ids from the database that may be needed to provide the
	extra information for Link element attributes.
----------------------------------------------------------------------------------------------*/
void FwXmlExportData::LoadOwners()
{
	SqlStatement sstmt;
	RETCODE rc;
	int hobj;
	int hobjOwner;
	SDWORD cbhobj;
	SDWORD cbOwner;

	m_staCmd.Format("SELECT Id, Owner$%n"
		"FROM CmObject%n"
		"WHERE Class$ in (SELECT DstCls FROM Field$) AND OwnFlid$ != %d AND OwnFlid$ != %d",
		kflidCmPossibilityList_Possibilities, kflidCmPossibility_SubPossibilities);
	sstmt.Init(m_pfxd->m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(m_staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbhobj);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &hobjOwner, isizeof(hobjOwner), &cbOwner);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// cache the mapping from object id to its owner.
	m_hmhobjOwner.Clear();
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		m_hmhobjOwner.Insert(hobj, hobjOwner);
	}
}

/*----------------------------------------------------------------------------------------------
	Load the object ids and class ids from the database that may be needed to provide the
	extra information for Link element attributes.
----------------------------------------------------------------------------------------------*/
void FwXmlExportData::LoadClassMap()
{
	SqlStatement sstmt;
	RETCODE rc;
	int hobj;
	int clid;
	SDWORD cbhobj;
	SDWORD cbClass;

	m_staCmd.Format("if object_id('LexReference_Targets') is not null %n"
		"Begin %n"
		"SELECT id, class$ FROM CmObject WHERE id in ( %n"
		"	SELECT DISTINCT Src FROM LexReference_Targets %n"
		"	UNION %n"
		"	SELECT DISTINCT Src FROM LexEntryRef_ComponentLexemes) %n"
		"End%n"
		"Else Begin%n"
		"	SELECT NULL, NULL%n"
		"End");
	sstmt.Init(m_pfxd->m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(m_staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbhobj);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &clid, isizeof(clid), &cbClass);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	// cache the mapping from object id to its class.
	m_hmhobjClid.Clear();
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbhobj == SQL_NULL_DATA || cbhobj == 0)
			continue;
		m_hmhobjClid.Insert(hobj, clid);
	}
}

/*----------------------------------------------------------------------------------------------
	Read the primary analysis writing system from the database.  If not set, use g_wsUser by
	default.

	@return Analysis writing system database id.
----------------------------------------------------------------------------------------------*/
int FwXmlExportData::ReadAnalWs()
{
	if (m_wsAnal)
		return m_wsAnal;
	else
		return ReadWsFromDb("CurAnalysisWss", m_wsAnal, m_staWsAnal);
}

/*----------------------------------------------------------------------------------------------
	Read the primary vernacular writing system from the database.  If not set, use g_wsUser by
	default.

	@return Vernacular writing system database id.
----------------------------------------------------------------------------------------------*/
int FwXmlExportData::ReadVernWs()
{
	if (m_wsVern)
		return m_wsVern;
	else
		return ReadWsFromDb("CurVernWss", m_wsVern, m_staWsVern);
}

/*----------------------------------------------------------------------------------------------
	Read the indicated writing system information from the database (if possible).

	@param pszWsColumn Either "CurAnalysisWss" or
			"CurVernWss".
	@param ws Reference to the writing system database id integer variable.
	@param staWs Reference to the writing system ICULocale string variable.

	@return Writing system database id.
----------------------------------------------------------------------------------------------*/
int FwXmlExportData::ReadWsFromDb(const char * pszWsColumn, int & wsFromDb, StrAnsi & staWs)
{
	SqlStatement sstmt;
	RETCODE rc;
	int ws;
	SDWORD cb;

	m_staCmd.Format("select top 1 lpc.Dst, lws.ICULocale "
		"from LangProject_%s lpc "
		"join LgWritingSystem lws on lws.Id = lpc.Dst "
		"where lpc.Src = %d order by lpc.Ord", pszWsColumn, m_hobjLP);
	sstmt.Init(m_pfxd->m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(m_staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &ws, isizeof(ws), &cb);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	if (m_vchTxt.Size() < 4000)
		m_vchTxt.Resize(4000);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_WCHAR, m_vchTxt.Begin(),
		m_vchTxt.Size() * isizeof(SQLWCHAR), &cb);
	rc = SQLFetch(sstmt.Hstmt());
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	sstmt.Clear();
	if (rc == SQL_NO_DATA)
	{
		return g_wsUser;
	}
	else
	{
		wsFromDb = ws;
		int cch = cb / sizeof(SQLWCHAR);
		Assert(cch < m_vchTxt.Size());
		// This assumes default mapping from UTF-16 to UTF-8 works for this case.
		staWs.Assign(m_vchTxt.Begin(), cch);
		return ws;
	}
}



/*----------------------------------------------------------------------------------------------
	Report an invalid Class value for a database object.

	@param pxed Pointer to the XML export data.
	@param iobj index into the basic object vector (pxed->m_vohd).
----------------------------------------------------------------------------------------------*/
static void ReportInvalidObjectClass(FwXmlExportData * pxed, int iobj)
{
	StrAnsi staMsg;
	staMsg.Format("INVALID DATABASE: Unknown Class Id for a CmObject.\n"
		"       Obj Id = %d, BAD Class Id = %d\n",
		pxed->m_vohd[iobj].m_hobj, pxed->m_vohd[iobj].m_cid);
	pxed->LogMessage(staMsg.Chars());
}


/*----------------------------------------------------------------------------------------------
	Report an invalid Owner Field value for a database object.

	@param pxed Pointer to the XML export data.
	@param hobj Database object id
	@param fidOwner the supposed Database field id of the object's owner
----------------------------------------------------------------------------------------------*/
static void ReportInvalidOwnerField(FwXmlExportData * pxed, int hobj, int fidOwner)
{
	StrAnsi staMsg;
	staMsg.Format("INVALID DATABASE: Unknown Owner Field Id for a CmObject.\n"
		"       Obj Id = %d, BAD Owner Field Id = %d\n",
		hobj, fidOwner);
	pxed->LogMessage(staMsg.Chars());
}


/*----------------------------------------------------------------------------------------------
	Save the database to the given XML file.
	This is an IFwXmlData interface method.

	@param bstrFile Name of the output XML file.
	@param pwsf Pointer to the relevant writing system factory.
	@param padvi Optional pointer to a progress report object.

	@return S_OK, E_INVALIDARG, E_UNEXPECTED, E_FAIL, or possibly another COM error code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwXmlData::SaveXml(BSTR bstrFile, ILgWritingSystemFactory * pwsf, IAdvInd * padvi)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrFile);
	if (!bstrFile)
		ReturnHr(E_INVALIDARG);
	ChkComArgPtr(pwsf);
	ChkComArgPtrN(padvi);
	Assert(m_sdb.IsOpen());
	if (!m_sdb.IsOpen())
		ThrowHr(E_UNEXPECTED);

	SqlStatement sstmt;
	SDWORD cb;
	RETCODE rc;
	FwXmlExportData xed(this);		// minimize stack/heap consumption of scratchpad memory
	xed.m_qwsf = pwsf;

	StrAnsi staFmt;
	StrAnsi staMsg;
	time_t timBegin = time(0);
	Vector<int> vhobjRoot;

	// Set g_wsUser.
	StrAnsi staWs(kstidXmlUserWs);
	g_staWsUser = staWs;
	xed.m_staCmd.Format("select top 1 lws.Id from LgWritingSystem lws "
		"where lws.ICULocale = '%s'", staWs.Chars());
	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(xed.m_staCmd.Chars())), SQL_NTS);
	xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, xed.m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &g_wsUser, isizeof(g_wsUser), &cb);
	xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLFetch(sstmt.Hstmt());
	xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	sstmt.Clear();
	if (rc == SQL_NO_DATA)
	{
		// Settle for anything...
		xed.m_staCmd.Assign("select top 1 lws.Id from LgWritingSystem lws");
		sstmt.Init(m_sdb);
		rc = SQLExecDirectA(sstmt.Hstmt(),
			reinterpret_cast<SQLCHAR *>(const_cast<char *>(xed.m_staCmd.Chars())),
			SQL_NTS);
		xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, xed.m_staCmd.Chars());
		rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &g_wsUser, isizeof(g_wsUser), &cb);
		xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		rc = SQLFetch(sstmt.Hstmt());
		xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		sstmt.Clear();
		Assert(rc != SQL_NO_DATA);
	}

	// Open the output file.
	IStreamPtr qstrm;
	FileStream::Create(bstrFile, STGM_WRITE | STGM_CREATE, &qstrm);

	// Open the log file.
	xed.m_stabpLog = bstrFile;
	int ich = xed.m_stabpLog.ReverseFindCh('.');
	if (ich != -1)
		xed.m_stabpLog.SetLength(ich);
	xed.m_stabpLog.Append("-Export.log");
	fopen_s(&xed.m_pfileLog, xed.m_stabpLog, "w");

	// Write the XML header information.
	FormatToStream(qstrm, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>%n");
	FormatToStream(qstrm, "<!DOCTYPE FwDatabase SYSTEM \"FwDatabase.dtd\">%n");

	// Get the version number of the database to write out in the top element.
	int nVersion;
#ifdef CHANGE_DB_MAXSIZE
	// KenZ: This should no longer be needed since we have the log maxsize set to unlimited.
	// JohnT: added stuff to make sure the log file is big enough for success.
	xed.m_staCmd =
		"declare @newmaxsize int "
		"set @newmaxsize = FileProperty(file_name(1), 'SpaceUsed') * 8 / 1024 * 2 "
		"if (@newmaxsize > 25) "
		"begin "
			"declare @stmt nvarchar(1000) "
			"set @stmt = '"
			"alter database [' + db_name() + '] "
				"modify file (name = [' + file_name(2) + '], maxsize = ' + "
					"convert(char(20), @newmaxsize) + 'MB)' "
			"exec (@stmt) "
		"end "
		"select max(DbVer) from Version$";
#else
	xed.m_staCmd = "select max(DbVer) from Version$";
#endif
	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(xed.m_staCmd.Chars())), SQL_NTS);
	xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, xed.m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &nVersion, isizeof(nVersion), &cb);
	xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLFetch(sstmt.Hstmt());
	xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	sstmt.Clear();
	if (rc == SQL_NO_DATA)
	{
		// "Cannot get version number from the database!?"
		StrAnsi staMsg(kstidXmlErrorMsg223);
		xed.LogMessage(staMsg.Chars());
		FormatToStream(qstrm, "<FwDatabase><!-- unknown version?? -->%n");
	}
	else
	{
		FormatToStream(qstrm, "<FwDatabase version=\"%d\">%n", nVersion);
	}

	// Write the definitions of any custom fields.
	int icls;
	int ifld;
	int cCustom = 0;
	char * pszType;
	bool fTarget = false;
	for (ifld = 0; ifld < m_vfdfi.Size(); ++ifld)
	{
		if (!m_vfdfi[ifld].fCustom)
			continue;
		if (!m_hmcidicls.Retrieve(m_vfdfi[ifld].cid, &icls))
		{
			staMsg.Format("INVALID DATABASE: Unknown class id in field definition.\n"
				"       Field Id = %d, name = %S, BAD Class Id = %d\n",
				m_vfdfi[ifld].fid, m_vstufld[ifld].Chars(), m_vfdfi[ifld].cid);
			xed.LogMessage(staMsg.Chars());
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		if (!cCustom)
			FormatToStream(qstrm, "<AdditionalFields>%n");
		++cCustom;
		switch (m_vfdfi[ifld].cpt)
		{
		case kcptBoolean:			pszType = "Boolean";			break;
		case kcptInteger:			pszType = "Integer";			break;
		case kcptNumeric:			pszType = "Numeric";			break;
		case kcptFloat:				pszType = "Float";				break;
		case kcptTime:				pszType = "Time";				break;
		case kcptGuid:				pszType = "Guid";				break;
		case kcptImage:				pszType = "Image";				break;
		case kcptGenDate:			pszType = "GenDate";			break;
		case kcptBinary:			pszType = "Binary";				break;
		case kcptString:			pszType = "String";				break;
		case kcptUnicode:			pszType = "Unicode";			break;
		case kcptBigString:			pszType = "BigString";			break;
		case kcptBigUnicode:		pszType = "BigUnicode";			break;
		case kcptMultiString:		pszType = "MultiString";		break;
		case kcptMultiUnicode:		pszType = "MultiUnicode";		break;
		case kcptMultiBigString:	pszType = "MultiBigString";		break;
		case kcptMultiBigUnicode:	pszType = "MultiBigUnicode";	break;
		case kcptOwningAtom:		pszType = "OwningAtom";			fTarget = true;		break;
		case kcptOwningCollection:	pszType = "OwningCollection";	fTarget = true;		break;
		case kcptOwningSequence:	pszType = "OwningSequence";		fTarget = true;		break;
		case kcptReferenceAtom:		pszType = "ReferenceAtom";		fTarget = true;		break;
		case kcptReferenceCollection: pszType = "ReferenceCollection"; fTarget = true;	break;
		case kcptReferenceSequence:	pszType = "ReferenceSequence";	fTarget = true;		break;
		default:
			Assert(false);			// THIS SHOULD NEVER HAPPEN!!
			ThrowHr(WarnHr(E_UNEXPECTED));
			break;
		}
		FormatToStream(qstrm, "<CustomField name=\"");
		WriteXmlUnicode(qstrm, m_vstufld[ifld].Chars(), m_vstufld[ifld].Length());
		FormatToStream(qstrm, "\" flid=\"%d\" class=\"%S\" type=\"%s\"",
			m_vfdfi[ifld].fid, m_vstucls[icls].Chars(), pszType);
		if (fTarget)
		{
			int iclsDst;
			if (!m_hmcidicls.Retrieve(m_vfdfi[ifld].cidDst, &iclsDst))
			{
				staMsg.Format("INVALID DATABASE: unknown Dest class id in field definition.\n"
					"       Field Id = %d, name = %S, BAD Dest Class Id = %d\n",
					m_vfdfi[ifld].fid, m_vstufld[ifld].Chars(), m_vfdfi[ifld].cidDst);
				xed.LogMessage(staMsg.Chars());
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			FormatToStream(qstrm, " target=\"%S\"", m_vstucls[iclsDst].Chars());
			fTarget = false;
		}
		if (!m_vfdfi[ifld].fNullMin)
			// This is an _int64, but isn't int32 big enough in practice?
			FormatToStream(qstrm, " min=\"%d\"", (int)m_vfdfi[ifld].nMin);
		if (!m_vfdfi[ifld].fNullMax)
			// This is an _int64, but isn't int32 big enough in practice?
			FormatToStream(qstrm, " max=\"%d\"", (int)m_vfdfi[ifld].nMax);
		if (!m_vfdfi[ifld].fNullBig)
			FormatToStream(qstrm, " big=\"%d\"", m_vfdfi[ifld].fBig ? 1 : 0);
		if (m_vfdfi[ifld].nListRootId)
		{
			// Map from database id to the corresponding GUID.  At this point, since we usually
			// have only a few custom fields anyway, we'll get it direct from the database.
			GUID guidListRoot;
			SDWORD cbGuid;
			xed.m_staCmd.Format("SELECT Guid$ from CmObject WHERE Id = %d",
				m_vfdfi[ifld].nListRootId);
			sstmt.Init(m_sdb);
			rc = SQLExecDirectA(sstmt.Hstmt(),
				reinterpret_cast<SQLCHAR *>(const_cast<char *>(xed.m_staCmd.Chars())), SQL_NTS);
			xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, xed.m_staCmd.Chars());
			rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_GUID, &guidListRoot, isizeof(guidListRoot),
				&cbGuid);
			xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			rc = SQLFetch(sstmt.Hstmt());
			xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
			FormatToStream(qstrm, " listRootId=\"I%g\"", &guidListRoot);
		}
		if (m_vfdfi[ifld].nWsSelector)
			FormatToStream(qstrm, " wsSelector=\"%d\"", m_vfdfi[ifld].nWsSelector);
		if (m_vstufldUserLabel[ifld].Length())
		{
			FormatToStream(qstrm, " userLabel=\"");
			WriteXmlUnicode(qstrm, m_vstufldUserLabel[ifld].Chars(),
				m_vstufldUserLabel[ifld].Length());
			FormatToStream(qstrm, "\"");
		}
		if (m_vstufldHelpString[ifld].Length())
		{
			FormatToStream(qstrm, " helpString=\"");
			WriteXmlUnicode(qstrm, m_vstufldHelpString[ifld].Chars(),
				m_vstufldHelpString[ifld].Length());
			FormatToStream(qstrm, "\"");
		}
		if (m_vstufldXmlUI[ifld].Length())
		{
			FormatToStream(qstrm, ">");
			WriteXmlUnicode(qstrm, m_vstufldXmlUI[ifld].Chars(), m_vstufldXmlUI[ifld].Length());
			FormatToStream(qstrm, "</CustomField>%n");
		}
		else
		{
			FormatToStream(qstrm, "/>%n");
		}
	}
	if (cCustom)
	{
		FormatToStream(qstrm, "</AdditionalFields>%n");
		// "%u custom field definition%s written.\n"
		staFmt.Load(kstidXmlInfoMsg201);
		StrAnsi staDefinition;
		if (cCustom == 1)
			staDefinition.Load(kstidDefinition);
		else
			staDefinition.Load(kstidDefinitions);
		staMsg.Format(staFmt.Chars(), cCustom, staDefinition.Chars());
		xed.LogMessage(staMsg.Chars());
	}

	// Rebuild the object hierarchy table.
	xed.m_staCmd = "exec UpdateHierarchy";
	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(xed.m_staCmd.Chars())), SQL_NTS);
	xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, xed.m_staCmd.Chars());
	sstmt.Clear();
	time_t timEnd1 = time(0);
	long timDelta1 = (long)(timEnd1 - timBegin);
	// "Rebuilding the object hierarchy table took %ld second%s.\n"
	StrAnsi staSecond(kstidSecond);
	StrAnsi staSeconds(kstidSeconds);
	staFmt.Load(kstidXmlInfoMsg218);
	staMsg.Format(staFmt.Chars(), timDelta1,
		timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	int cTotal = 55;
	if (padvi)
	{
		// Rebuilding the object hierarchy is almost always over half the export time.
		padvi->Step(cTotal);
	}

	LoadBasicAttributeRows(&xed, m_sdb);
	time_t timEnd2 = time(0);
	timDelta1 = (long)(timEnd2 - timEnd1);
	// "Loading %u row%s of basic attribute data took %ld second%s.\n"
	StrAnsi staRow(kstidRow);
	StrAnsi staRows(kstidRows);
	staFmt.Load(kstidXmlInfoMsg216);
	staMsg.Format(staFmt.Chars(), xed.m_vbdr.Size(),
		xed.m_vbdr.Size() == 1 ? staRow.Chars() : staRows.Chars(),
		timDelta1, timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	LoadBinaryAttributeRows(&xed, m_sdb);
	timEnd1 = time(0);
	timDelta1 = (long)(timEnd1 - timEnd2);
	// "Loading %u row%s of \"Binary\" data took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg206);
	staMsg.Format(staFmt.Chars(), xed.m_vbirBinary.Size(),
		xed.m_vbirBinary.Size() == 1 ? staRow.Chars() : staRows.Chars(), timDelta1,
		timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	LoadImageAttributeRows(&xed, m_sdb);
	timEnd2 = time(0);
	timDelta1 = (long)(timEnd2 - timEnd1);
	// "Loading %u row%s of \"Image\" data took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg207);
	staMsg.Format(staFmt.Chars(), xed.m_vbirImage.Size(),
		xed.m_vbirImage.Size() == 1 ? staRow.Chars() : staRows.Chars(), timDelta1,
		timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	LoadUnicodeRows(&xed, m_sdb);
	timEnd1 = time(0);
	timDelta1 = (long)(timEnd1 - timEnd2);
	// "Loading %u row%s of \"Unicode\" data took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg215);
	staMsg.Format(staFmt.Chars(), xed.m_vudr.Size(),
		xed.m_vudr.Size() == 1 ? staRow.Chars() : staRows.Chars(),
		timDelta1, timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	LoadBigUnicodeRows(&xed, m_sdb);
	timEnd2 = time(0);
	timDelta1 = (long)(timEnd2 - timEnd1);
	// "Loading %u row%s of \"BigUnicode\" data took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg205);
	staMsg.Format(staFmt.Chars(), xed.m_vudrBig.Size(),
		xed.m_vudrBig.Size() == 1 ? staRow.Chars() : staRows.Chars(),
		timDelta1, timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	LoadStringRows(&xed, m_sdb);
	timEnd1 = time(0);
	timDelta1 = (long)(timEnd1 - timEnd2);
	// "Loading %u row%s of \"String\" data took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg214);
	staMsg.Format(staFmt.Chars(), xed.m_vsdr.Size(),
		xed.m_vsdr.Size() == 1 ? staRow.Chars() : staRows.Chars(),
		timDelta1, timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	LoadBigStringRows(&xed, m_sdb);
	timEnd2 = time(0);
	timDelta1 = (long)(timEnd2 - timEnd1);
	// "Loading %u row%s of \"BigString\" data took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg204);
	staMsg.Format(staFmt.Chars(), xed.m_vsdrBig.Size(),
		xed.m_vsdrBig.Size() == 1 ? staRow.Chars() : staRows.Chars(),
		timDelta1, timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	LoadMultiUnicodeRows(&xed, m_sdb);
	timEnd1 = time(0);
	timDelta1 = (long)(timEnd1 - timEnd2);
	// "Loading %u row%s of \"MultiUnicode\" data took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg211);
	staMsg.Format(staFmt.Chars(), xed.m_vmur.Size(),
		xed.m_vmur.Size() == 1 ? staRow.Chars() : staRows.Chars(),
		timDelta1, timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	LoadMultiBigUnicodeRows(&xed, m_sdb);
	timEnd2 = time(0);
	timDelta1 = (long)(timEnd2 - timEnd1);
	// "Loading %u row%s of \"MultiBigUnicode\" data took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg209);
	staMsg.Format(staFmt.Chars(), xed.m_vmurBig.Size(),
		xed.m_vmurBig.Size() == 1 ? staRow.Chars() : staRows.Chars(),
		timDelta1, timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	LoadMultiStringRows(&xed, m_sdb);
	timEnd1 = time(0);
	timDelta1 = (long)(timEnd1 - timEnd2);
	// "Loading %u row%s of \"MultiString\" data took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg210);
	staMsg.Format(staFmt.Chars(), xed.m_vmsr.Size(),
		xed.m_vmsr.Size() == 1 ? staRow.Chars() : staRows.Chars(),
		timDelta1, timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	LoadMultiBigStringRows(&xed, m_sdb);
	timEnd2 = time(0);
	timDelta1 = (long)(timEnd2 - timEnd1);
	// "Loading %u row%s of \"MultiBigString\" data took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg208);
	staMsg.Format(staFmt.Chars(), xed.m_vmsrBig.Size(),
		xed.m_vmsrBig.Size() == 1 ? staRow.Chars() : staRows.Chars(),
		timDelta1, timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	LoadRefCollectionRows(&xed, m_sdb);
	timEnd1 = time(0);
	timDelta1 = (long)(timEnd1 - timEnd2);
	// "Loading %u row%s of \"ReferenceCollection/Sequence\" data took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg213);
	staMsg.Format(staFmt.Chars(), xed.m_vrcd.Size(),
		xed.m_vrcd.Size() == 1 ? staRow.Chars() : staRows.Chars(),
			timDelta1, timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	LoadRefAtomRows(&xed, m_sdb);
	timEnd2 = time(0);
	timDelta1 = (long)(timEnd2 - timEnd1);
	// "Loading %u row%s of \"ReferenceAtom\" data took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg212);
	staMsg.Format(staFmt.Chars(), xed.m_vrad.Size(),
		xed.m_vrad.Size() == 1 ? staRow.Chars() : staRows.Chars(),
		timDelta1, timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	xed.LoadObjHierarchy();
	xed.LoadOwners();
	xed.LoadClassMap();

	// Store mapping information to allow enhanced Link element attributes as appropriate.
	xed.StoreLinkInfo();
	xed.StoreEntryOrSenseInfo();
	timEnd1 = time(0);
	timDelta1 = (long)(timEnd1 - timEnd2);
	// "Loading %u row%s of Object Ownership Hierarchy data took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg203);
	staMsg.Format(staFmt.Chars(), xed.m_vohd.Size(),
		xed.m_vohd.Size() == 1 ? staRow.Chars() : staRows.Chars(),
			timDelta1, timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && timDelta1)
	{
		padvi->Step(2);
		cTotal += 2;
	}

	XmlTagStack xtag;
	Vector<XmlTagStack> vxtagStack;
	int depth;
	int depthPrev = 1;
	bool fChangeFields;
	int cRange = 100 - cTotal;
	while (cRange <= 0)
		cRange += 100;
	for (int iobj = 0; iobj < xed.m_cobj; ++iobj)
	{
		if (!m_hmcidicls.Retrieve(xed.m_vohd[iobj].m_cid, &icls))
		{
			ReportInvalidObjectClass(&xed, iobj);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		depth = xed.m_vohd[iobj].m_depth;
		if (depth > 1)
		{
			if (!m_hmfidifld.Retrieve(xed.m_vohd[iobj].m_fidOwner, &ifld))
			{
				ReportInvalidOwnerField(&xed, xed.m_vohd[iobj].m_hobj,
					xed.m_vohd[iobj].m_fidOwner);
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
		}
		else
		{
			ifld = -1;
		}
		fChangeFields = false;
		if (depth > depthPrev)
		{
			Assert(ifld >= 0);
			// Write the field start tag and save the name for the end tag later.
			xtag.m_pszName = WriteFieldStartTag(qstrm, ifld);
			FormatToStream(qstrm, "%n"/*, xtag.m_pszName*/);
			xtag.m_fFieldTag = true;
			xtag.m_ifld = ifld;
			xtag.m_depth = depth;
			vxtagStack.Push(xtag);
		}
		else if (depth < depthPrev)
		{
			Assert(vxtagStack.Size() >= 2);
			// Write any necessary end tags.
			while (vxtagStack.Size() && depth < vxtagStack.Top()->m_depth)
			{
				FormatToStream(qstrm, "</%S>%n", vxtagStack.Top()->m_pszName);
				vxtagStack.Pop();
			}
			if (vxtagStack.Size())
			{
				Assert(depth == vxtagStack.Top()->m_depth);
				Assert(!vxtagStack.Top()->m_fFieldTag);
				FormatToStream(qstrm, "</%S>%n", vxtagStack.Top()->m_pszName);
				vxtagStack.Pop();
			}
			if (vxtagStack.Size())
			{
				Assert(depth == vxtagStack.Top()->m_depth);
				Assert(vxtagStack.Top()->m_fFieldTag);
				if (ifld != vxtagStack.Top()->m_ifld)
					fChangeFields = true;
			}
		}
		else if (ifld != -1)
		{
			// Close the open object.
			Assert(vxtagStack.Size() >= 2);
			Assert(!vxtagStack.Top()->m_fFieldTag);
			FormatToStream(qstrm, "</%S>%n", vxtagStack.Top()->m_pszName);
			vxtagStack.Pop();
			Assert(vxtagStack.Top()->m_fFieldTag);
			if (ifld != vxtagStack.Top()->m_ifld)
				fChangeFields = true;
		}
		else if (iobj != 0)
		{
			// We have a top-level object: empty the tag stack.
			while (vxtagStack.Size())
			{
				FormatToStream(qstrm, "</%S>%n", vxtagStack.Top()->m_pszName);
				vxtagStack.Pop();
			}
		}
		if (fChangeFields)
		{
			// Changing fields in this object: close the old field.
			FormatToStream(qstrm, "</%S>%n", vxtagStack.Top()->m_pszName);
			vxtagStack.Pop();
			// Write the field start tag and save the name for the end tag later.
			xtag.m_pszName = WriteFieldStartTag(qstrm, ifld);
			FormatToStream(qstrm, "%n");
			xtag.m_fFieldTag = true;
			xtag.m_ifld = ifld;
			xtag.m_depth = depth;
			vxtagStack.Push(xtag);
		}
		// Write the start element for the object, write the non-owned data, and push the
		// object name onto the stack for writing the end tag later.
		xtag.m_pszName = m_vstucls[icls].Chars();
		FormatToStream(qstrm, "<%S id=\"I%g\">%n",
			xtag.m_pszName, &xed.m_vohd[iobj].m_guid);
		WriteObjectData(qstrm, iobj, &xed);
		xtag.m_fFieldTag = false;
		xtag.m_icls = icls;
		xtag.m_depth = depth;
		vxtagStack.Push(xtag);
		depthPrev = depth;
		if (padvi)
		{
			int cStep = (iobj + 1) * cRange / xed.m_cobj;
			if (cStep)
			{
				padvi->Step(cStep);
				cTotal += cStep;
			}
		}
	}
	while (vxtagStack.Size())
	{
		FormatToStream(qstrm, "</%S>%n", vxtagStack.Top()->m_pszName);
		vxtagStack.Pop();
	}

	timEnd2 = time(0);
	timDelta1 = (long)(timEnd2 - timEnd1);
	// "Writing the XML file took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg220);
	staMsg.Format(staFmt.Chars(), timDelta1,
		timDelta1 == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());

	FormatToStream(qstrm, "</FwDatabase>%n");

	// We're through with the contents of the object hierarchy table.
	xed.m_staCmd = "TRUNCATE TABLE ObjHierarchy$";
	sstmt.Init(m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(xed.m_staCmd.Chars())), SQL_NTS);
	xed.VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, xed.m_staCmd.Chars());
	sstmt.Clear();

	time_t timEnd = time(0);
	long timDelta = (long)(timEnd - timBegin);
	// "Loading the data from the database took %ld SQL command%s.\n"
	staFmt.Load(kstidXmlInfoMsg217);
	StrAnsi staCommand;
	if (xed.m_cSql == 1)
		staCommand.Load(kstidCommand);
	else
		staCommand.Load(kstidCommands);
	staMsg.Format(staFmt.Chars(), xed.m_cSql, staCommand.Chars());
	xed.LogMessage(staMsg.Chars());
	// "Dumping the XML file from the database took %ld second%s.\n"
	staFmt.Load(kstidXmlInfoMsg202);
	staMsg.Format(staFmt.Chars(), timDelta,
		timDelta == 1 ? staSecond.Chars() : staSeconds.Chars());
	xed.LogMessage(staMsg.Chars());
	if (padvi && cTotal < 100)
		padvi->Step(100 - cTotal);

	END_COM_METHOD(s_fact, IID_IFwXmlData);
}

/*----------------------------------------------------------------------------------------------
	Store the information needed to enhance the cross reference links with target names in
	addition to the guid based ids.
----------------------------------------------------------------------------------------------*/
void FwXmlExportData::StoreLinkInfo()
{
	int wsAnal = ReadAnalWs();
	int i;
	for (i = 0; i < m_vmur.Size(); ++i)
	{
		if (m_vmur[i].m_ws == wsAnal)
		{
			if (m_vmur[i].m_fid == kflidCmPossibility_Name)
			{
				m_hmhobjimurName.Insert(m_vmur[i].m_hobj, i);
			}
			else if (m_vmur[i].m_fid == kflidCmPossibility_Abbreviation)
			{
				m_hmhobjimurAbbr.Insert(m_vmur[i].m_hobj, i);
			}
			else if (m_vmur[i].m_fid == kflidMoInflClass_Name)
			{
				m_hmhobjimurName.Insert(m_vmur[i].m_hobj, i);
				StoreOwnersNameAbbr(m_vmur[i].m_hobj, wsAnal,
					kflidCmPossibility_Abbreviation, kflidCmPossibility_Name);
			}
			else if (m_vmur[i].m_fid == kflidMoInflClass_Abbreviation)
			{
				m_hmhobjimurAbbr.Insert(m_vmur[i].m_hobj, i);
				StoreOwnersNameAbbr(m_vmur[i].m_hobj, wsAnal,
					kflidCmPossibility_Abbreviation, kflidCmPossibility_Name);
			}
			else if (m_vmur[i].m_fid == kflidMoInflAffixSlot_Name)
			{
				m_hmhobjimurName.Insert(m_vmur[i].m_hobj, i);
				StoreOwnersNameAbbr(m_vmur[i].m_hobj, wsAnal,
					kflidCmPossibility_Abbreviation, kflidCmPossibility_Name);
			}
			/*
			  We need to distinguish these from CmPossibility objects!

			else if (m_vmur[i].m_fid == kflidMoStemName_Abbreviation)
				m_hmhobjimurAbbr.Insert(m_vmur[i].m_hobj, i);
			else if (m_vmur[i].m_fid == kflidMoStemName_Name)
				m_hmhobjimurName.Insert(m_vmur[i].m_hobj, i);
			else if (m_vmur[i].m_fid == kflidMoStratum_Abbreviation)
				m_hmhobjimurAbbr.Insert(m_vmur[i].m_hobj, i);
			else if (m_vmur[i].m_fid == kflidMoStratum_Name)
				m_hmhobjimurName.Insert(m_vmur[i].m_hobj, i);
			else if (m_vmur[i].m_fid == kflidMoGlossItem_Abbreviation)
				m_hmhobjimurAbbr.Insert(m_vmur[i].m_hobj, i);
			else if (m_vmur[i].m_fid == kflidMoGlossItem_Name)
				m_hmhobjimurName.Insert(m_vmur[i].m_hobj, i);
			else if (m_vmur[i].m_fid == kflidPhNaturalClass_Abbreviation)
				m_hmhobjimurAbbr.Insert(m_vmur[i].m_hobj, i);
			else if (m_vmur[i].m_fid == kflidPhNaturalClass_Name)
				m_hmhobjimurName.Insert(m_vmur[i].m_hobj, i);

			  other multilingual names (but no abbreviation):
			  5030001	16	5030	MoCompoundRule_Name
			  5037001	16	5037	MoInflAffixTemplate_Name
			  5044001	16	5044	MoReferralRule_Name
			  5081001	16	5081	PhPhonContext_Name
			  5089001	16	5089	PhPhonemeSet_Name
			  5090001	16	5090	PhTerminalUnit_Name
			  5097001	16	5097	PhEnvironment_Name
			  5110001	16	5110	MoAdhocProhibGr_Name
			*/
		}
	}
	for (i = 0; i < m_vudr.Size(); ++i)
	{
		if (m_vudr[i].m_fid == kflidReversalIndexEntry_Form ||
			m_vudr[i].m_fid == kflidPhEnvironment_StringRepresentation)
		{
			m_hmhobjiudr.Insert(m_vudr[i].m_hobj, i);
		}
	}
	for (i = 0; i < m_vrad.Size(); ++i)
	{
		if (m_vrad[i].m_fid == kflidReversalIndexEntry_WritingSystem)
			m_hmhobjirad.Insert(m_vrad[i].m_hobj, i);
	}
}

/*----------------------------------------------------------------------------------------------
	Store the information needed to enhance the cross reference links to LexEntry or LexSense
	objects.
----------------------------------------------------------------------------------------------*/
void FwXmlExportData::StoreEntryOrSenseInfo()
{
	SqlStatement sstmt;
	RETCODE rc;
	SDWORD cbObjId;
	SDWORD cbClassId;
	SDWORD cbValue = 0;
	int hobj;
	int clid = 0;
	HeadwordData hwd;

	m_staCmd.Format("if object_id('GetHeadwordsForEntriesOrSenses') is not null %n"
		"	SELECT ObjId, ClassId, Headword FROM dbo.GetHeadwordsForEntriesOrSenses () %n"
		"else %n"
		"	SELECT null, null, null");
	sstmt.Init(m_pfxd->m_sdb);
	rc = SQLExecDirectA(sstmt.Hstmt(),
		reinterpret_cast<SQLCHAR *>(const_cast<char *>(m_staCmd.Chars())), SQL_NTS);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__, m_staCmd.Chars());
	rc = SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &hobj, isizeof(hobj), &cbObjId);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &clid, isizeof(clid), &cbClassId);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	rc = SQLBindCol(sstmt.Hstmt(), 3, SQL_C_WCHAR, m_vchTxt.Begin(),
		m_vchTxt.Size() * isizeof(SQLWCHAR), &cbValue);
	VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
	for (;;)
	{
		rc = SQLFetch(sstmt.Hstmt());
		VerifySqlRc(rc, sstmt.Hstmt(), __LINE__);
		if (rc == SQL_NO_DATA)
			break;
		if (rc != SQL_SUCCESS)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (cbValue == SQL_NULL_DATA || !cbValue)
			continue;
		hwd.m_stuHeadword.Assign(m_vchTxt.Begin(), cbValue/sizeof(wchar));
		hwd.m_fSense = clid == kclidLexSense;
		m_hmhobjHeadword.Insert(hobj, hwd);
	}
	sstmt.Clear();

	// Get the vernacular writing system string for later.
	ReadVernWs();
}

/*----------------------------------------------------------------------------------------------
	Write the XML start tag for the given field to an XML file.

	@param pstrm IStream interface pointer.
	@param ifld Index into the field meta-data tables (m_vfdfi, m_vstufld and m_vstufldXml).

	@return Pointer to the field name for use later in the end tag.
----------------------------------------------------------------------------------------------*/
const wchar * FwXmlData::WriteFieldStartTag(IStream * pstrm, int ifld)
{
	const wchar * pszName;
	if (m_vfdfi[ifld].fCustom)
	{
		switch (m_vfdfi[ifld].cpt)
		{
		case kcptBoolean:
		case kcptInteger:
		case kcptNumeric:
		case kcptFloat:
		case kcptTime:
		case kcptGuid:
		case kcptImage:
		case kcptGenDate:
		case kcptBinary:
		case kcptString:
		case kcptUnicode:
		case kcptBigString:
		case kcptBigUnicode:
			pszName = L"Custom";
			break;
		case kcptMultiString:
		case kcptMultiUnicode:
		case kcptMultiBigString:
		case kcptMultiBigUnicode:
			pszName = L"CustomStr";
			break;
		case kcptOwningAtom:
		case kcptOwningCollection:
		case kcptOwningSequence:
			pszName = L"CustomObj";
			break;
		case kcptReferenceAtom:
		case kcptReferenceCollection:
		case kcptReferenceSequence:
			pszName = L"CustomLink";
			break;
		default:
			Assert(false);			// THIS SHOULD NEVER HAPPEN!!
			ThrowHr(WarnHr(E_UNEXPECTED));
			break;
		}
		FormatToStream(pstrm, "<%S name=\"%S\">", pszName, m_vstufld[ifld].Chars());
	}
	else
	{
		pszName = m_vstufldXml[ifld].Chars();
		FormatToStream(pstrm, "<%S>", pszName);
	}
	return pszName;
}

/*----------------------------------------------------------------------------------------------
	Write the given (formatted) string to an XML file.

	@param pstrm IStream interface pointer.
	@param ws Writing system of multi-lingual string, or 0 if monolingual.
	@param prgchTxt Array of Unicode character data from a database entry.
	@param cchTxt Number of 16-bit characters in prgchTxt.
	@param prgbFmt Array of binary formatting data associated with prgchTxt.
	@param cbFmt Number of bytes in prgbFmt.
	@param pxed Pointer to the XML export data.
----------------------------------------------------------------------------------------------*/
static void WriteXmlString(IStream * pstrm, int ws, const OLECHAR * prgchTxt, int cchTxt,
	const BYTE * prgbFmt, int cbFmt, FwXmlExportData * pxed)
{
	AssertPtr(pstrm);
	AssertArray(prgchTxt, cchTxt);
	AssertArrayN(prgbFmt, cbFmt);
	AssertPtr(pxed);
	AssertPtr(pxed->m_qtsf.Ptr());
	StrAnsi sta;

	// Convert data to ITsString object, then WriteAsXml.
	ITsStringPtr qtss;
	int cchTxt2 = cchTxt;
	int cbFmt2 = cbFmt;
	HRESULT hr;
	IgnoreHr(hr = pxed->m_qtsf->DeserializeStringRgch(prgchTxt, &cchTxt2, prgbFmt, &cbFmt2,
		&qtss));
	if (hr == S_FALSE)
	{
		sta.Format("QUESTIONABLE DATA: cchTxt = %d, cchTxt2 = %d, cbFmt = %d, cbFmt2 = %d.\n",
			cchTxt, cchTxt2, cbFmt, cbFmt2);
		pxed->LogMessage(sta.Chars());
	}
	else
	{
		Assert(hr == S_OK);
		Assert(cchTxt == cchTxt2);
		Assert(cbFmt == cbFmt2);
	}
	IgnoreHr(hr = qtss->WriteAsXml(pstrm, pxed->m_qwsf, 0, ws, true));
	Assert(hr == S_OK);

	// Check that each run of the string had valid properties.
	int crun;
	CheckHr(qtss->get_RunCount(&crun));
	for (int irun = 0; irun < crun; ++irun)
	{
		ITsTextPropsPtr qttp;
		CheckHr(qtss->get_Properties(irun, &qttp));
		int ctip;
		int ctsp;
		CheckHr(qttp->get_IntPropCount(&ctip));
		CheckHr(qttp->get_StrPropCount(&ctsp));
		if (ctip + ctsp == 0)
		{
			LARGE_INTEGER move;
			move.HighPart = 0;
			move.LowPart = 0;
			ULARGE_INTEGER posCur;
			CheckHr(pstrm->Seek(move, STREAM_SEEK_CUR, &posCur));
			sta.Format("BAD DATA: <Run> %<0>d has no properties for string "
				"ending before file position %<1>u.\n", irun + 1, posCur.LowPart);
			pxed->LogMessage(sta.Chars());
		}
		else
		{
			int nVar, nVal;
			CheckHr(qttp->GetIntPropValues(ktptWs, &nVar, &nVal));
			if (nVar == -1 && nVal == -1)
			{
				LARGE_INTEGER move;
				move.HighPart = 0;
				move.LowPart = 0;
				ULARGE_INTEGER posCur;
				CheckHr(pstrm->Seek(move, STREAM_SEEK_CUR, &posCur));
				sta.Format("BAD DATA: <Run> %<0>d has no writing system property for string "
					"ending before file position %<1>u.\n", irun + 1, posCur.LowPart);
				pxed->LogMessage(sta.Chars());
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Write the given text properties, which are part of a style specification, in XML format.

	@param pstrm IStream interface pointer.
	@param pdrdr Pointer to an object containing binary formatting information.
	@param flid Id of field being written to XML.
	@param hobj Id of object being written to XML.
----------------------------------------------------------------------------------------------*/
void FwXmlExportData::WriteXmlTextProps(IStream * pstrm, DataReader * pdrdr, int flid, int hobj)
{
	AssertPtr(pstrm);
	AssertPtr(pdrdr);

	byte rgbCnt[2];
	pdrdr->ReadBuf(rgbCnt, 2);
	int ctip = rgbCnt[0];
	int ctsp = rgbCnt[1];
	Assert((int)(byte)ctip == ctip);
	Assert((int)(byte)ctsp == ctsp);

	if (ctip + ctsp > 0)
	{
		FormatToStream(pstrm, "<Prop");
		if (ctip)
		{
			TextProps::TextIntProp txip;
			for (int itip = 0; itip < ctip; itip++)
			{
				if (pdrdr->IbCur() >= pdrdr->Size())
				{
					int cMissing = ctip - itip;
					StrAnsi staFmt;
					StrUni stuName;
					if (flid == kflidStStyle_Rules)
					{
						if (cMissing == 1)
							staFmt.Load(kstidXmlErrorMsg225);
						else
							staFmt.Load(kstidXmlErrorMsg226);
						stuName = FindUnicode(hobj, kflidStStyle_Name);
					}
					else
					{
						if (cMissing == 1)
							staFmt.Load(kstidXmlErrorMsg227);
						else
							staFmt.Load(kstidXmlErrorMsg228);
					}
					if (stuName.Length() == 0)
						stuName.Load(kstidUNKNOWN);
					StrAnsi sta;
					sta.Format(staFmt.Chars(), cMissing, stuName.Chars());
					LogMessage(sta.Chars());
					break;
				}
				TextProps::ReadTextIntProp(pdrdr, &txip);
				FwXml::WriteIntTextProp(pstrm, m_qwsf, txip.m_tpt, txip.m_nVar, txip.m_nVal);
			}
		}
		StrUni stuWsStyles;
		StrUni stuBulNumFontInfo;
		if (ctsp)
		{
			TextProps::TextStrProp txsp;
			for (int itsp = 0; itsp < ctsp; itsp++)
			{
				if (pdrdr->IbCur() >= pdrdr->Size())
				{
					int cMissing = ctsp - itsp;
					StrAnsi staFmt;
					StrUni stuName;
					if (flid == kflidStStyle_Rules)
					{
						if (cMissing == 1)
							staFmt.Load(kstidXmlErrorMsg229);
						else
							staFmt.Load(kstidXmlErrorMsg230);
						stuName = FindUnicode(hobj, kflidStStyle_Name);
					}
					else
					{
						if (cMissing == 1)
							staFmt.Load(kstidXmlErrorMsg231);
						else
							staFmt.Load(kstidXmlErrorMsg232);
					}
					if (stuName.Length() == 0)
						stuName.Load(kstidUNKNOWN);
					StrAnsi sta;
					sta.Format(staFmt.Chars(), cMissing, stuName.Chars());
					LogMessage(sta.Chars());
					break;
				}
				TextProps::ReadTextStrProp(pdrdr, &txsp);
				if (txsp.m_tpt == ktptWsStyle)
					stuWsStyles = txsp.m_stuVal;
				else if (txsp.m_tpt == ktptBulNumFontInfo)
					stuBulNumFontInfo = txsp.m_stuVal;
				else
					FwXml::WriteStrTextProp(pstrm, txsp.m_tpt, txsp.m_stuVal.Bstr());
			}
		}
		if (stuWsStyles.Length() || stuBulNumFontInfo.Length())
		{
			FormatToStream(pstrm, ">%n");
			if (stuBulNumFontInfo.Length())
				FwXml::WriteBulNumFontInfo(pstrm, stuBulNumFontInfo.Bstr());
			if (stuWsStyles.Length())
				FwXml::WriteWsStyles(pstrm, m_qwsf, stuWsStyles.Bstr());
			FormatToStream(pstrm, "</Prop>%n");
		}
		else
		{
			FormatToStream(pstrm, "/>%n");
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Write the data associated with the given object to the output stream in XML format.

	@param hobj
	@param flid
----------------------------------------------------------------------------------------------*/
const wchar * FwXmlExportData::FindUnicode(int hobj, int flid)
{
	UnicodeDataRow * pudr;
	for (pudr = m_pudr; pudr < m_vudr.End(); ++pudr)
	{
		if (pudr->m_hobj != hobj)
			break;
		if (pudr->m_fid == flid)
		{
			return pudr->m_stuValue.Chars();
		}
	}
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Write the data associated with the given object to the output stream in XML format.

	@param pstrm IStream interface pointer.
	@param iobj Index into pxed->m_vohd, the hierarchy ordered table of object information.
	@param pxed Pointer to the XML export data.
----------------------------------------------------------------------------------------------*/
void FwXmlData::WriteObjectData(IStream * pstrm, int iobj, FwXmlExportData * pxed)
{
	int hobj;
	int ifld;
	const wchar * pszwXmlName;
	BasicDataRow * pbdr;
	BinaryDataRow * pbirBinary;
	BinaryDataRow * pbirImage;
	UnicodeDataRow * pudr;
	UnicodeDataRow * pudrBig;
	StringDataRow * psdr;
	StringDataRow * psdrBig;
	MultiUnicodeDataRow * pmur;
	MultiUnicodeDataRow * pmurBig;
	MultiStringDataRow * pmsr;
	MultiStringDataRow * pmsrBig;
	ReferenceCollectionDataRow * prcd;
	ReferenceAtomDataRow * prad;
	bool fZero;
	int ich;

	hobj = pxed->m_vohd[iobj].m_hobj;

	// Write the simple basic data values.
	for (pbdr = pxed->m_pbdr; pbdr < pxed->m_vbdr.End(); ++pbdr)
	{
		if (pbdr->m_hobj != hobj)
			break;
		// Don't bother saving zero values -- these are usually the default.
		fZero = true;
		for (ich = 0; ich < pbdr->m_staValue.Length(); ++ich)
		{
			if (pbdr->m_staValue[ich] != '0' && pbdr->m_staValue[ich] != '.')
			{
				fZero = false;
				break;
			}
		}
		if (fZero)
			continue;
		if (!m_hmfidifld.Retrieve(pbdr->m_fid, &ifld))
		{
			ReportInvalidOwnerField(pxed, pbdr->m_hobj, pbdr->m_fid);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		pszwXmlName = WriteFieldStartTag(pstrm, ifld);
		switch (m_vfdfi[ifld].cpt)
		{
		case kcptBoolean:
			FormatToStream(pstrm, "<Boolean val=\"true\"/>");
			break;
		case kcptInteger:
			FormatToStream(pstrm, "<Integer val=\"%s\"/>", pbdr->m_staValue.Chars());
			break;
		case kcptNumeric:
			FormatToStream(pstrm, "<Numeric val=\"%s\"/>", pbdr->m_staValue.Chars());
			break;
		case kcptFloat:
			FormatToStream(pstrm, "<Float val=\"%s\"/>", pbdr->m_staValue.Chars());
			break;
		case kcptTime:
			FormatToStream(pstrm, "<Time val=\"%s\"/>", pbdr->m_staValue.Chars());
			break;
		case kcptGuid:
			FormatToStream(pstrm, "<Guid val=\"%s\"/>", pbdr->m_staValue.Chars());
			break;
		case kcptGenDate:
			FormatToStream(pstrm, "<GenDate val=\"%s\"/>", pbdr->m_staValue.Chars());
			break;
		default:
			Assert(false);			// THIS SHOULD NEVER HAPPEN!!
			break;
		}
		FormatToStream(pstrm, "</%S>%n", pszwXmlName);
		pbdr->m_staValue.Clear();
	}
	pxed->m_pbdr = pbdr;

	// Write the short binary data values.
	const byte * pb;
	int cb;
	for (pbirBinary = pxed->m_pbirBinary; pbirBinary < pxed->m_vbirBinary.End(); ++pbirBinary)
	{
		if (pbirBinary->m_hobj != hobj)
			break;
		if (!m_hmfidifld.Retrieve(pbirBinary->m_fid, &ifld))
		{
			ReportInvalidOwnerField(pxed, pbirBinary->m_hobj, pbirBinary->m_fid);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		Assert(m_vfdfi[ifld].cpt == kcptBinary);
		pszwXmlName = WriteFieldStartTag(pstrm, ifld);
		if (m_vfdfi[ifld].fid == kflidStPara_StyleRules ||
			m_vfdfi[ifld].fid == kflidStStyle_Rules)
		{
			DataReaderRgb drr(pbirBinary->m_vbValue.Begin(), pbirBinary->m_vbValue.Size());
			FormatToStream(pstrm, "%n");
			pxed->WriteXmlTextProps(pstrm, &drr, m_vfdfi[ifld].fid, hobj);
		}
		else
		{
			FormatToStream(pstrm, "<Binary>");
			// Write the binary data in packed hexadecimal format.
			cb = pbirBinary->m_vbValue.Size();
			pb = pbirBinary->m_vbValue.Begin();
			int ib;
			for (ib = 0; ib < cb; ++ib)
			{
				FormatToStream(pstrm, "%02x", pb[ib]);
				if (ib % 32 == 31)
					FormatToStream(pstrm, "%n");
			}
			if (ib % 32)
				FormatToStream(pstrm, "%n");
			FormatToStream(pstrm, "</Binary>");
		}
		FormatToStream(pstrm, "</%S>%n", pszwXmlName);
		pbirBinary->m_vbValue.Clear();
	}
	pxed->m_pbirBinary = pbirBinary;

	// Write the long binary data values.
	for (pbirImage = pxed->m_pbirImage; pbirImage < pxed->m_vbirImage.End(); ++pbirImage)
	{
		if (pbirImage->m_hobj != hobj)
			break;
		if (!m_hmfidifld.Retrieve(pbirImage->m_fid, &ifld))
		{
			ReportInvalidOwnerField(pxed, pbirImage->m_hobj, pbirImage->m_fid);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		Assert(m_vfdfi[ifld].cpt == kcptImage);
		pszwXmlName = WriteFieldStartTag(pstrm, ifld);
		FormatToStream(pstrm, "<Image>%n");
		// Write the binary data in packed hexadecimal format.
		cb = pbirImage->m_vbValue.Size();
		pb = pbirImage->m_vbValue.Begin();
		int ib;
		for (ib = 0; ib < cb; ++ib)
		{
			FormatToStream(pstrm, "%02x", pb[ib]);
			if (ib % 32 == 31)
				FormatToStream(pstrm, "%n");
		}
		if (ib % 32)
			FormatToStream(pstrm, "%n");
		FormatToStream(pstrm, "</Image></%S>%n", pszwXmlName);
		pbirImage->m_vbValue.Clear();
	}
	pxed->m_pbirImage = pbirImage;

	// Write the short Unicode strings.
	for (pudr = pxed->m_pudr; pudr < pxed->m_vudr.End(); ++pudr)
	{
		if (pudr->m_hobj != hobj)
			break;
		if (!m_hmfidifld.Retrieve(pudr->m_fid, &ifld))
		{
			ReportInvalidOwnerField(pxed, pudr->m_hobj, pudr->m_fid);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		Assert(m_vfdfi[ifld].cpt == kcptUnicode);
		pszwXmlName = WriteFieldStartTag(pstrm, ifld);
		FormatToStream(pstrm, "<Uni>");
		WriteXmlUnicode(pstrm, pudr->m_stuValue.Chars(), pudr->m_stuValue.Length());
		FormatToStream(pstrm, "</Uni></%S>%n", pszwXmlName);
		pudr->m_stuValue.Clear();
	}
	pxed->m_pudr = pudr;

	// Write the long Unicode strings.
	for (pudrBig = pxed->m_pudrBig; pudrBig < pxed->m_vudrBig.End(); ++pudrBig)
	{
		if (pudrBig->m_hobj != hobj)
			break;
		if (!m_hmfidifld.Retrieve(pudrBig->m_fid, &ifld))
		{
			ReportInvalidOwnerField(pxed, pudrBig->m_hobj, pudrBig->m_fid);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		Assert(m_vfdfi[ifld].cpt == kcptBigUnicode);
		pszwXmlName = WriteFieldStartTag(pstrm, ifld);
		FormatToStream(pstrm, "<Uni>");
		WriteXmlUnicode(pstrm, pudrBig->m_stuValue.Chars(),
			pudrBig->m_stuValue.Length());
		FormatToStream(pstrm, "</Uni></%S>%n", pszwXmlName);
		pudrBig->m_stuValue.Clear();
	}
	pxed->m_pudrBig = pudrBig;

	// Write the short formatted strings.
	for (psdr = pxed->m_psdr; psdr < pxed->m_vsdr.End(); ++psdr)
	{
		if (psdr->m_hobj != hobj)
			break;
		if (!m_hmfidifld.Retrieve(psdr->m_fid, &ifld))
		{
			ReportInvalidOwnerField(pxed, psdr->m_hobj, psdr->m_fid);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		Assert(m_vfdfi[ifld].cpt == kcptString);
		pszwXmlName = WriteFieldStartTag(pstrm, ifld);
		FormatToStream(pstrm, "%n");
		WriteXmlString(pstrm, 0, psdr->m_stuValue.Chars(), psdr->m_stuValue.Length(),
			psdr->m_vbFmt.Begin(), psdr->m_vbFmt.Size(), pxed);
		FormatToStream(pstrm, "</%S>%n", pszwXmlName);
		psdr->m_stuValue.Clear();
		psdr->m_vbFmt.Clear();
	}
	pxed->m_psdr = psdr;

	// Write the long formatted strings.
	for (psdrBig = pxed->m_psdrBig; psdrBig < pxed->m_vsdrBig.End(); ++psdrBig)
	{
		if (psdrBig->m_hobj != hobj)
			break;
		if (!m_hmfidifld.Retrieve(psdrBig->m_fid, &ifld))
		{
			ReportInvalidOwnerField(pxed, psdrBig->m_hobj, psdrBig->m_fid);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		Assert(m_vfdfi[ifld].cpt == kcptBigString);
		pszwXmlName = WriteFieldStartTag(pstrm, ifld);
		FormatToStream(pstrm, "%n");
		WriteXmlString(pstrm, 0, psdrBig->m_stuValue.Chars(), psdrBig->m_stuValue.Length(),
			psdrBig->m_vbFmt.Begin(), psdrBig->m_vbFmt.Size(), pxed);
		FormatToStream(pstrm, "</%S>%n", pszwXmlName);
		psdrBig->m_stuValue.Clear();
		psdrBig->m_vbFmt.Clear();
	}
	pxed->m_psdrBig = psdrBig;

	// Write the atomic reference links.

	int hobjDst;
	for (prad = pxed->m_prad; prad < pxed->m_vrad.End(); ++prad)
	{
		if (prad->m_hobj != hobj)
			break;
		if (!m_hmfidifld.Retrieve(prad->m_fid, &ifld))
		{
			ReportInvalidOwnerField(pxed, prad->m_hobj, prad->m_fid);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		Assert(m_vfdfi[ifld].cpt == kcptReferenceAtom);
		pszwXmlName = WriteFieldStartTag(pstrm, ifld);
		hobjDst = prad->m_hobjDst;
		WriteLink(pstrm, iobj, prad->m_hobj, hobjDst, pszwXmlName, pxed);
		FormatToStream(pstrm, "</%S>%n", pszwXmlName);
		prad->m_hobjDst = -1;		// Signal that we've already written this value.
	}
	pxed->m_prad = prad;

	// Write the short multilingual Unicode strings.
	int ifldPrev = -1;
	pszwXmlName = NULL;
	for (pmur = pxed->m_pmur; pmur < pxed->m_vmur.End(); ++pmur)
	{
		if (pmur->m_hobj != hobj)
			break;
		if (!m_hmfidifld.Retrieve(pmur->m_fid, &ifld))
		{
			ReportInvalidOwnerField(pxed, pmur->m_hobj, pmur->m_fid);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		Assert(m_vfdfi[ifld].cpt == kcptMultiUnicode);
		if (ifld != ifldPrev)
		{
			if (pszwXmlName)
				FormatToStream(pstrm, "</%S>%n", pszwXmlName);
			pszwXmlName = WriteFieldStartTag(pstrm, ifld);
			FormatToStream(pstrm, "%n");
			ifldPrev = ifld;
		}
		FormatToStream(pstrm, "<AUni ws=\"");
		SmartBstr sbstr;
		CheckHr(pxed->m_qwsf->GetStrFromWs(pmur->m_ws, &sbstr));
		WriteXmlUnicode(pstrm, sbstr.Chars(), sbstr.Length());
		FormatToStream(pstrm, "\">");
		WriteXmlUnicode(pstrm, pmur->m_stuValue.Chars(), pmur->m_stuValue.Length());
		FormatToStream(pstrm, "</AUni>%n");
	}
	pxed->m_pmur = pmur;
	if (pszwXmlName)
		FormatToStream(pstrm, "</%S>%n", pszwXmlName);

	// Write the long multilingual Unicode strings.
	ifldPrev = -1;
	pszwXmlName = NULL;
	for (pmurBig = pxed->m_pmurBig; pmurBig < pxed->m_vmurBig.End(); ++pmurBig)
	{
		if (pmurBig->m_hobj != hobj)
			break;
		if (!m_hmfidifld.Retrieve(pmurBig->m_fid, &ifld))
		{
			ReportInvalidOwnerField(pxed, pmurBig->m_hobj, pmurBig->m_fid);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		Assert(m_vfdfi[ifld].cpt == kcptMultiBigUnicode);
		if (ifld != ifldPrev)
		{
			if (pszwXmlName)
				FormatToStream(pstrm, "</%S>%n", pszwXmlName);
			pszwXmlName = WriteFieldStartTag(pstrm, ifld);
			FormatToStream(pstrm, "%n");
			ifldPrev = ifld;
		}
		FormatToStream(pstrm, "<AUni ws=\"");
		SmartBstr sbstr;
		CheckHr(pxed->m_qwsf->GetStrFromWs(pmurBig->m_ws, &sbstr));
		WriteXmlUnicode(pstrm, sbstr.Chars(), sbstr.Length());
		FormatToStream(pstrm, "\">");
		WriteXmlUnicode(pstrm, pmurBig->m_stuValue.Chars(),
			pmurBig->m_stuValue.Length());
		FormatToStream(pstrm, "</AUni>%n");
		pmurBig->m_stuValue.Clear();
	}
	pxed->m_pmurBig = pmurBig;
	if (pszwXmlName)
		FormatToStream(pstrm, "</%S>%n", pszwXmlName);

	// Write the short multilingual formatted strings.
	ifldPrev = -1;
	pszwXmlName = NULL;
	for (pmsr = pxed->m_pmsr; pmsr < pxed->m_vmsr.End(); ++pmsr)
	{
		if (pmsr->m_hobj != hobj)
			break;
		if (!m_hmfidifld.Retrieve(pmsr->m_fid, &ifld))
		{
			ReportInvalidOwnerField(pxed, pmsr->m_hobj, pmsr->m_fid);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		Assert(m_vfdfi[ifld].cpt == kcptMultiString);
		if (ifld != ifldPrev)
		{
			if (pszwXmlName)
				FormatToStream(pstrm, "</%S>%n", pszwXmlName);
			pszwXmlName = WriteFieldStartTag(pstrm, ifld);
			FormatToStream(pstrm, "%n");
			ifldPrev = ifld;
		}
		WriteXmlString(pstrm, pmsr->m_ws, pmsr->m_stuValue.Chars(), pmsr->m_stuValue.Length(),
			pmsr->m_vbFmt.Begin(), pmsr->m_vbFmt.Size(), pxed);
		pmsr->m_stuValue.Clear();
		pmsr->m_vbFmt.Clear();
	}
	pxed->m_pmsr = pmsr;
	if (pszwXmlName)
		FormatToStream(pstrm, "</%S>%n", pszwXmlName);

	// Write the long multilingual formatted strings.
	ifldPrev = -1;
	pszwXmlName = NULL;
	for (pmsrBig = pxed->m_pmsrBig; pmsrBig < pxed->m_vmsrBig.End(); ++pmsrBig)
	{
		if (pmsrBig->m_hobj != hobj)
			break;
		if (!m_hmfidifld.Retrieve(pmsrBig->m_fid, &ifld))
		{
			ReportInvalidOwnerField(pxed, pmsrBig->m_hobj, pmsrBig->m_fid);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		Assert(m_vfdfi[ifld].cpt == kcptMultiBigString);
		if (ifld != ifldPrev)
		{
			if (pszwXmlName)
				FormatToStream(pstrm, "</%S>%n", pszwXmlName);
			pszwXmlName = WriteFieldStartTag(pstrm, ifld);
			FormatToStream(pstrm, "%n");
			ifldPrev = ifld;
		}
		WriteXmlString(pstrm, pmsrBig->m_ws,
			pmsrBig->m_stuValue.Chars(), pmsrBig->m_stuValue.Length(),
			pmsrBig->m_vbFmt.Begin(), pmsrBig->m_vbFmt.Size(), pxed);
		pmsrBig->m_stuValue.Clear();
		pmsrBig->m_vbFmt.Clear();
	}
	pxed->m_pmsrBig = pmsrBig;
	if (pszwXmlName)
		FormatToStream(pstrm, "</%S>%n", pszwXmlName);

	// Write the reference link collections and sequences.
	ifldPrev = -1;
	pszwXmlName = NULL;
	for (prcd = pxed->m_prcd; prcd < pxed->m_vrcd.End(); ++prcd)
	{
		if (prcd->m_hobj != hobj)
			break;
		if (!m_hmfidifld.Retrieve(prcd->m_fid, &ifld))
		{
			ReportInvalidOwnerField(pxed, prcd->m_hobj, prcd->m_fid);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		Assert(m_vfdfi[ifld].cpt == kcptReferenceCollection ||
			m_vfdfi[ifld].cpt == kcptReferenceSequence);
		if (ifld != ifldPrev)
		{
			if (pszwXmlName)
				FormatToStream(pstrm, "</%S>%n", pszwXmlName);
			pszwXmlName = WriteFieldStartTag(pstrm, ifld);
			FormatToStream(pstrm, "%n");
			ifldPrev = ifld;
		}
		hobjDst = prcd->m_hobjDst;
		WriteLink(pstrm, iobj, prcd->m_hobj, hobjDst, pszwXmlName, pxed);
		FormatToStream(pstrm, "%n");
		prcd->m_hobjDst = -1;		// Signal that we've already written this value.
	}
	pxed->m_prcd = prcd;
	if (pszwXmlName)
		FormatToStream(pstrm, "</%S>%n", pszwXmlName);
}

/*----------------------------------------------------------------------------------------------
	Write a link target, including the abbreviation and name of the link target to the XML
	output.

	@param pstrm IStream interface pointer.
	@param iobj
	@param hobjSrc
	@param hobjDst Object id of the linked object.
	@param pszwXmlName
	@param pxed Pointer to the XML export data.
----------------------------------------------------------------------------------------------*/
void FwXmlData::WriteLink(IStream * pstrm, int iobj, int hobjSrc, int hobjDst,
	const wchar * pszwXmlName, FwXmlExportData * pxed)
{
	bool fValid = true;
	GUID * pguidDst;
	if (hobjDst < pxed->m_cobj)
		pguidDst = pxed->m_mphobjguid[hobjDst];
	else
		fValid = pxed->m_hmhobjguid.Retrieve(hobjDst, &pguidDst);
	if (!fValid || !pguidDst)
	{
		FormatToStream(pstrm, "<!-- INVALID Link target hobj=%d -->", hobjDst);
		//"Corrupted database: missing %<0>S object (hobj = %<1>d) for %<2>S (hobj = %<3>d)"
		int icls;
		if (!m_hmcidicls.Retrieve(pxed->m_vohd[iobj].m_cid, &icls))
		{
			ReportInvalidObjectClass(pxed, iobj);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		StrAnsi staFmt(kstidXmlErrorMsg224);
		StrAnsi staMsg;
		staMsg.Format(staFmt.Chars(),
			pszwXmlName, hobjDst, m_vstucls[icls].Chars(), hobjSrc);
		pxed->LogMessage(staMsg.Chars());
		return;
	}

	FormatToStream(pstrm, "<Link target=\"I%g\"", pguidDst);

	int imur;
	bool fWsWritten = false;
	StrAnsi staWsAnal = pxed->m_staWsAnal;
	if (staWsAnal.Length() == 0)
		staWsAnal = g_staWsUser;

	if (pxed->m_hmhobjimurAbbr.Retrieve(hobjDst, &imur))
	{
		FormatToStream(pstrm, " ws=\"%s\"", staWsAnal.Chars());
		fWsWritten = true;
		FormatToStream(pstrm, " abbr=\"");
		WriteXmlUnicode(pstrm, pxed->m_vmur[imur].m_stuValue.Chars(),
			pxed->m_vmur[imur].m_stuValue.Length());
		FormatToStream(pstrm, "\"");
	}
	if (pxed->m_hmhobjimurName.Retrieve(hobjDst, &imur))
	{
		if (!fWsWritten)
		{
			FormatToStream(pstrm, " ws=\"%s\"", staWsAnal.Chars());
			fWsWritten = true;
		}
		FormatToStream(pstrm, " name=\"");
		WriteXmlUnicode(pstrm, pxed->m_vmur[imur].m_stuValue.Chars(),
			pxed->m_vmur[imur].m_stuValue.Length());
		FormatToStream(pstrm, "\"");
	}
	if (pxed->m_hmhobjimurAbbrOwner.Retrieve(hobjDst, &imur))
	{
		if (!fWsWritten)
		{
			FormatToStream(pstrm, " ws=\"%s\"", staWsAnal.Chars());
			fWsWritten = true;
		}
		FormatToStream(pstrm, " abbrOwner=\"");
		WriteXmlUnicode(pstrm, pxed->m_vmur[imur].m_stuValue.Chars(),
			pxed->m_vmur[imur].m_stuValue.Length());
		FormatToStream(pstrm, "\"");
	}
	if (pxed->m_hmhobjimurNameOwner.Retrieve(hobjDst, &imur))
	{
		if (!fWsWritten)
		{
			FormatToStream(pstrm, " ws=\"%s\"", staWsAnal.Chars());
			fWsWritten = true;
		}
		FormatToStream(pstrm, " nameOwner=\"");
		WriteXmlUnicode(pstrm, pxed->m_vmur[imur].m_stuValue.Chars(),
			pxed->m_vmur[imur].m_stuValue.Length());
		FormatToStream(pstrm, "\"");
	}
	int iudr;
	if (!fWsWritten)
	{
		int irad;
		if (pxed->m_hmhobjirad.Retrieve(hobjDst, &irad) &&
			pxed->m_vrad[irad].m_fid == kflidReversalIndexEntry_WritingSystem &&
			pxed->m_hmhobjiudr.Retrieve(hobjDst, &iudr) &&
			pxed->m_vudr[iudr].m_fid == kflidReversalIndexEntry_Form)
		{
			FormatToStream(pstrm, " ws=\"");
			SmartBstr sbstr;
			CheckHr(pxed->m_qwsf->GetStrFromWs(pxed->m_vrad[irad].m_hobjDst, &sbstr));
			WriteXmlUnicode(pstrm, sbstr.Chars(), sbstr.Length());
			FormatToStream(pstrm, "\" form=\"");
			StrUni stuHier;
			pxed->BuildReversalIndexHierForm(iudr, stuHier);
			WriteXmlUnicode(pstrm, stuHier.Chars(), stuHier.Length());
			FormatToStream(pstrm, "\"");
		}
	}
	if (pxed->m_hmhobjiudr.Retrieve(hobjDst, &iudr) &&
		pxed->m_vudr[iudr].m_fid == kflidPhEnvironment_StringRepresentation)
	{
		FormatToStream(pstrm, " form=\"");
		WriteXmlUnicode(pstrm, pxed->m_vudr[iudr].m_stuValue.Chars(),
			pxed->m_vudr[iudr].m_stuValue.Length());
		FormatToStream(pstrm, "\"");
	}
	HeadwordData hwd;
	int clid;
	if (pxed->m_hmhobjHeadword.Retrieve(hobjDst, &hwd) &&
		pxed->m_hmhobjClid.Retrieve(hobjSrc, &clid))
	{
		if (clid == kclidLexReference)
		{
			if (pxed->m_hmhobjimurAbbrOwner.Retrieve(hobjSrc, &imur))
			{
				FormatToStream(pstrm, " wsa=\"%s\" abbr=\"", pxed->m_staWsAnal.Chars());
				WriteXmlUnicode(pstrm, pxed->m_vmur[imur].m_stuValue.Chars(),
					pxed->m_vmur[imur].m_stuValue.Length());
				FormatToStream(pstrm, "\"");
			}
			FormatToStream(pstrm, " wsv=\"%s\"", pxed->m_staWsVern.Chars());
		}
		else
		{
			FormatToStream(pstrm, " ws=\"%s\"", pxed->m_staWsVern.Chars());
		}

		if (hwd.m_fSense)
			FormatToStream(pstrm, " sense=\"");
		else
			FormatToStream(pstrm, " entry=\"");
		WriteXmlUnicode(pstrm, hwd.m_stuHeadword.Chars(), hwd.m_stuHeadword.Length());
		FormatToStream(pstrm, "\"");
	}
	FormatToStream(pstrm, "/>");
}

// Explicit instantiation.
#include "Vector_i.cpp"
#include "HashMap_i.cpp"
#include "Set_i.cpp"

// Local Variables:
// compile-command:"cmd.exe /E:4096 /C ..\\..\\Bin\\mkcel.bat"
// End:
