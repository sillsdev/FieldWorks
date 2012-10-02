/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwRsOdbcDa.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Subclass of VwCacheDa for data from ODBC record sets
	Requires $(OBJ_GEN_DATA) to be included in OBJ_ALL in the make file; also odbc32.lib in
	LINK_LIBS, and GENERIC_SRC to be one of the source directories.
Note:
	This file is not currently used. I (JohnT) have changed all TO DO items to TOxDO so they
	don't show up in searches.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE
#include "HashMap.h"			//  TOxDO PaulP:  Note to self.  Including these 2 files
#include "HashMap_i.cpp"		//		will lead to code bloat.


static DummyFactory g_fact("SIL.Views.VwRsOdbcDa");


/***********************************************************************************************
	Constructors/Destructor
***********************************************************************************************/

VwRsOdbcDa::VwRsOdbcDa()
{
	// TOxDO PaulP(JohnT): this gives us numbers way out of range of current IDs, which is
	// good enough until we implement Save for newly created objects. The real version of this
	// code should obtain new object IDs from the database, and therefore not need m_hvoNext.
	m_hvoNext = 100000000;
}

/***********************************************************************************************
	Methods for loading row sets
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Read a binary data field out of the column icol, and set prgbData to point at the result.
	The caller supplies a buffer prgbBuf of size cbMaxBuf into which a small result can be
	placed, and also a Vector<byte> into which a larger result will be placed.
	Note: call from inside try/catch block; may throw exceptions.
----------------------------------------------------------------------------------------------*/
void VwRsOdbcDa::ReadBinary(SQLHSTMT hstmt, int icol, byte * prgbBuf, int cbMaxBuf,
	Vector<byte> & vbData, byte * & prgbData, long & cbRet)
{
	long cbData;
	RETCODE rc;
	prgbData = prgbBuf; // by default (short strings) data is returned in the buffer.
	// Read the formatting data.
	rc = CheckSqlRc(SQLGetData(hstmt, (unsigned short)(icol), SQL_C_BINARY, prgbBuf,
		cbMaxBuf, &cbData));
	if (cbData < 0)
	{
		// Null field; return 0 length
		cbData = 0;
	}
	// REVIEW ShonK (JohnT): Should we check the SQLSTATE for 01004? If so, how?
	if (rc == SQL_SUCCESS_WITH_INFO)
	{
		vbData.Clear(); // in case reused from an earlier call
		do
		{
			if ((uint)cbData > (uint)cbMaxBuf)
				cbData = cbMaxBuf; // Do NOT subtract 1, binary data is not null terminated.
			vbData.Replace(vbData.Size(), vbData.Size(), prgbBuf, cbData);
		} while ((rc = CheckSqlRc(SQLGetData(hstmt, (unsigned short)(icol), SQL_C_BINARY,
			prgbBuf, cbMaxBuf, &cbData))) != SQL_NO_DATA);
		cbData = vbData.Size();
		prgbData = vbData.Begin();
	}
	else if (rc != SQL_SUCCESS)
	{
		CheckHr(WarnHr(E_UNEXPECTED));
	}
	Assert(prgbData == vbData.Begin() && cbData == vbData.Size() ||
		prgbData == prgbBuf && cbData <= cbMaxBuf);
	cbRet = cbData;
}

/*----------------------------------------------------------------------------------------------
	Read Unicode data field out of the column icol, and set prgchData to point at the result.
	The caller supplies a buffer prgccBuf of size cchMaxBuf into which a small result can be
	placed, and also a Vector<wchar> into which a larger result will be placed.
	Note: call from inside try/catch block; may throw exceptions.
----------------------------------------------------------------------------------------------*/
void VwRsOdbcDa::ReadUnicode(SQLHSTMT hstmt, int icol, wchar * prgchBuf, int cchMaxBuf,
	Vector<wchar> & vchData, wchar * & prgchData, long & cchRet)
{
	long cbData;
	RETCODE rc;
	prgchData = prgchBuf; // by default (short strings) data is returned in the buffer.
	// Read the formatting data.
	rc = CheckSqlRc(SQLGetData(hstmt, (unsigned short)(icol), SQL_C_WCHAR, prgchBuf,
		cchMaxBuf * 2, &cbData));
	if (cbData < 0)
	{
		// NULL
		cbData = 0;
	}
	// REVIEW ShonK (JohnT): Should we check the SQLSTATE for 01004? If so, how?
	if (rc == SQL_SUCCESS_WITH_INFO)
	{
		vchData.Clear();  // forget anything from previous property
		do
		{
			if ((uint)cbData > (uint)cchMaxBuf * 2)
				cbData = cchMaxBuf * 2 - 2; //-2 allows for terminating null)
			vchData.Replace(vchData.Size(), vchData.Size(), prgchBuf, cbData / 2);
		} while ((rc = CheckSqlRc(SQLGetData(hstmt, (unsigned short)(icol),
			SQL_C_WCHAR, prgchBuf, cchMaxBuf * 2, &cbData))) != SQL_NO_DATA);
		cbData = vchData.Size() * 2;
		prgchData = vchData.Begin();
	}
	else if (rc != SQL_SUCCESS)
	{
		CheckHr(WarnHr(E_UNEXPECTED));
		// TOxDO JohnT: throw exception of some sort...
	}
	Assert(prgchData == vchData.Begin() && cbData == vchData.Size() * 2 ||
		prgchData == prgchBuf && cbData <= cchMaxBuf * 2);
	cchRet = cbData / 2;
}

/*----------------------------------------------------------------------------------------------
	Load data into the cache from the record set defined by hstmt, according to the specs
	in prgocs/cocs. Columns with m_icolID = 0 give properties of hvoBase.
	Load properties of at most crowMax objects; this may only be used if there is no vector
	property being loaded, since we could not be sure of having a complete record of the
	value of a vector without loading the next row. If crowMax is zero, load everything.
	Note: call from inside try/catch block; may throw exceptions.
	Note that prgocs[i] describes the column which ODBC indexes as [i+1].
----------------------------------------------------------------------------------------------*/
void VwRsOdbcDa::Load(SQLHSTMT hstmt, OdbcColSpec * prgocs, int cocs, HVO hvoBase,
	int crowMax)
{
	AssertArray(prgocs, cocs);
	Assert((uint)cocs <= (uint) 200); // limit because of size of rghvoBaseIds
	Assert(crowMax >= 0);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	ITsPropsFactoryPtr qtpf;
	qtpf.CreateInstance(CLSID_TsPropsFactory);

	// Block of variables for binary fields
	Vector<byte> vbData; // used to buffer data from binary fields
	const int kcbMaxData = 1000; // amount of binary data to read in one go
	byte rgbData[kcbMaxData];  // buffer for short binary data fields
	long cbData; // how many bytes in prgbData hold valid data
	byte * prgbData; // points to rgbData or vbData.Begin(), as appropriate

	// Similar block for Unicode text
	Vector<wchar> vchData;
	const int kcchMaxData = 1000;
	wchar rgchData[kcchMaxData];
	long cchData;
	wchar * prgchData;

	Vector<HVO> vhvo; // accumulate objects for sequence property
	int nrows = 0;

	if (crowMax == 0)
		crowMax = INT_MAX;


	HVO rghvoBaseIds[200];

	int icolVec = -1; // index of (one and only) column of type koctObjVec
	int hvoVecBase; // object that is base of vector property

	while (CheckSqlRc(SQLFetch(hstmt)) != SQL_NO_DATA)
	{
		// We have a record.
		for (int icol = 0; icol < cocs; icol++)
		{
			int nVal;
			HVO hvoVal;
			ITsStringPtr qtssVal;
			// TOxDO JohnT: fill this in...
			HVO hvoCurBase; // object whose property we will read.
			if (prgocs[icol].m_icolID == 0)
				hvoCurBase = hvoBase;
			else
			{
				// Must refer to a previous column; use <= because m_icolID is 1-based, so
				// if equal to i, it refers to the immediate previous column.
				Assert(prgocs[icol].m_icolID <= icol);
				hvoCurBase = rghvoBaseIds[prgocs[icol].m_icolID - 1];
			}
			switch (prgocs[icol].m_oct)
			{
			default:
				Assert(false);
				ThrowHr(WarnHr(E_UNEXPECTED));
			case koctInt:
				CheckSqlRc(SQLGetData(hstmt, (unsigned short)(icol + 1), SQL_C_SLONG, &nVal, 4, NULL));
				CacheIntProp(hvoCurBase, prgocs[icol].m_tag, nVal);
				break;
			case koctUnicode:
				ReadUnicode(hstmt, icol + 1, rgchData, kcchMaxData,
					vchData, prgchData, cchData);
				CacheUnicodeProp(hvoCurBase, prgocs[icol].m_tag, prgchData, cchData);
				break;
			case koctString:
			case koctMlsAlt:
			case koctMltAlt:
				// Next column must give format; both are for the same property
				ReadUnicode(hstmt, icol + 1, rgchData, kcchMaxData,
					vchData, prgchData, cchData);
				if (koctMltAlt != prgocs[icol].m_oct)
				{
					Assert(icol < cocs - 1 && prgocs[icol + 1].m_oct == koctFmt);
					Assert(prgocs[icol].m_tag == prgocs[icol + 1].m_tag);
					// Leave the data in prgchData and cchData, to be processed next iteration
					// when we read the format.
					break;
				}
				// A MS alt without a FMT column, use the specified writing system both for the string
				// formatting and to indicate the alternative.
				CheckHr(qtsf->MakeStringRgch(prgchData, cchData, prgocs[icol].m_ws, &qtssVal));
				CacheStringAlt(hvoCurBase, prgocs[icol].m_tag,
						prgocs[icol].m_ws, qtssVal);
				break;
			case koctFmt:
				// Previous column must be string or multistring; we have already checked same tag.
				Assert(icol > 0 &&
					(prgocs[icol - 1].m_oct == koctString || prgocs[icol - 1].m_oct == koctMlsAlt));
				ReadBinary(hstmt, icol + 1, rgbData, kcbMaxData,
					vbData, prgbData, cbData);
				int cbDataInt;
				cbDataInt = cbData;
				int cchDataInt;
				cchDataInt = cchData;
				if (cchDataInt == 0 && cbDataInt == 0)
					CheckHr(qtsf->MakeStringRgch(NULL, 0, prgocs[icol - 1].m_ws, &qtssVal));
				else
					CheckHr(qtsf->DeserializeStringRgch(prgchData, &cchDataInt, prgbData,
						&cbDataInt, &qtssVal));
				if (prgocs[icol - 1].m_oct == koctString)
				{
					CacheStringProp(hvoCurBase, prgocs[icol].m_tag, qtssVal);
				}
				else
				{
					CacheStringAlt(hvoCurBase, prgocs[icol].m_tag,
						prgocs[icol - 1].m_ws, qtssVal);
				}
				break;
			case koctObj:
			case koctBaseId:
				long nIndicator;
				CheckSqlRc(SQLGetData(hstmt, (unsigned short)(icol + 1), SQL_C_SLONG,
					&hvoVal, 4, &nIndicator));
				// Treat null as zero.
				if (nIndicator == SQL_NULL_DATA)
					hvoVal = 0;
				if (prgocs[icol].m_oct == koctObj)
					CacheObjProp(hvoCurBase, prgocs[icol].m_tag, hvoVal);
				rghvoBaseIds[icol] = hvoVal;
				break;
			case koctObjVec:
				CheckSqlRc(SQLGetData(hstmt, (unsigned short)(icol + 1), SQL_C_SLONG,
					&hvoVal, 4, NULL));
				rghvoBaseIds[icol] = hvoVal;
				// See if there has been a change in the base column, if so, record value and
				// start a new one.
				if (icolVec < 0)
				{
					// First iteration, ignore previous object
					icolVec = icol;
					hvoVecBase = hvoCurBase;
				}
				else
				{
					// Only one vector column allowed!
					Assert(icolVec == icol);
					if (hvoVecBase != hvoCurBase)
					{
						// Started a new vector! Record the old one
						CacheVecProp(hvoVecBase, prgocs[icolVec].m_tag, vhvo.Begin(),
							vhvo.Size());
						// clear the list out and note new base object
						vhvo.Clear();
						hvoVecBase = hvoCurBase;
					}
				}
				vhvo.Push(hvoVal);
				break;
			case koctTtp:
				ReadBinary(hstmt, icol + 1, rgbData, kcbMaxData,
					vbData, prgbData, cbData);
				if (cbData > 0) // otherwise field is null, cache nothing
				{
					cbDataInt = cbData;
					ITsTextPropsPtr qttp;
					qtpf->DeserializePropsRgb(prgbData, &cbDataInt, &qttp);
					CacheUnknown(hvoCurBase, prgocs[icol].m_tag, qttp);
				}
				break;
			}
		}

		// Stop if we have processed the requested number of rows.
		nrows++;
		if (nrows >= crowMax)
			break;
	}
	// If we are processing a vector, we need to fill in the last occurrence
	if (icolVec >= 0)
	{
		CacheVecProp(hvoVecBase, prgocs[icolVec].m_tag, vhvo.Begin(),
			vhvo.Size());
	}
}


/*----------------------------------------------------------------------------------------------
	If rc indicates an error (or partial success), retrieve and log more specific information.
	(This is identical to FwXmlExportData::VerifySqlRc except for calling a different LogMessage
	method.)
----------------------------------------------------------------------------------------------*/
void VerifySqlRc(RETCODE rc, SQLHANDLE hstmt, int cline, const char * pszCmd)
{
#ifdef DELETE_THIS
	if (pszCmd && *pszCmd)
	{
#ifdef LOG_SQL
		LogMessage("SQL[%d]: %s\n", cline, pszCmd);
#endif /*LOG_SQL*/
		++m_cSql;
	}
	if (rc == SQL_ERROR || rc == SQL_SUCCESS_WITH_INFO)
	{
		SQLCHAR sqst[6];
		SQLINTEGER ntverr;
		SQLCHAR szBuf[512];
		SQLSMALLINT cb;
		SQLGetDiagRec(SQL_HANDLE_STMT, hstmt, 1, sqst, &ntverr, szBuf, isizeof(szBuf) - 1, &cb);
		sqst[5] = 0;
		szBuf[isizeof(szBuf) - 1] = 0;
		if (rc == SQL_ERROR)
		{
			if (pszCmd && *pszCmd)
			{
#ifndef LOG_SQL
				LogMessage("SQL[%d]: %s\n", cline, pszCmd);
#endif /*LOG_SQL*/
				LogMessage("ERROR %s executing SQL command:\n    %s\n", sqst, szBuf);
			}
			else
			{
				LogMessage("ERROR %s executing SQL function:\n    %s\n", sqst, szBuf);
			}
		}
		else
		{
			// Remove leading cruft from the information string.
			const char * psz;
			psz = reinterpret_cast<char *>(szBuf);
			if (!strncmp(psz, "[Microsoft]", 11))
				psz += 11;
			if (!strncmp(psz, "[ODBC SQL Server Driver]", 24))
				psz += 24;
			if (!strncmp(psz, "[SQL Server]", 12))
				psz += 12;
			LogMessage("    %s - %s\n", sqst, psz);
		}
	}
	CheckSqlRc(rc);
#endif
}

/*----------------------------------------------------------------------------------------------
	Answer whether anything needs saving.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRsOdbcDa::IsDirty(ComBool * pf)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pf);

	*pf = m_soprMods.Size() > 0 ||
		m_soperMods.Size() > 0;
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}


/*----------------------------------------------------------------------------------------------
	Save data in the cache, to the database.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwRsOdbcDa::Save(SqlDb & sdb)
{
	BEGIN_COM_METHOD

	const kcchErrMsgBuf = 1024;
	const kcbFmtBufMax = 1024;
	const kcchMaxColNameSize = 200;

	SQLINTEGER cbColVal;
	SDWORD cbClassName;
	long cbHvo;
	SDWORD cbFieldName;
	SDWORD cbFlid;
	int cbFmtBufSize = kcbFmtBufMax;
	int cbFmtSpaceTaken;
	SDWORD cbType;
	SQLUSMALLINT cParamsProcessed = 0;
	HRESULT hr;
	FieldMetaDataMap fmdm;		//  REVIEW PaulP:  Would be nice to have this kept globally.
	FieldMetaDataRec fmdr;
	int nColVal;
	int nFlid;
	ITsStringPtr qtssColVal;
	RETCODE rc;
	byte * rgbFmt = NewObj byte[kcbFmtBufMax]; //[kcbFmtBufMax];
	SmartBstr sbstr;
	SQLCHAR sqlchSqlState[5];
	SQLCHAR sqlchMsgTxt[kcchErrMsgBuf];
	SQLINTEGER sqlnNativeErrPtr;
	SQLSMALLINT sqlnTextLength;
	SQLRETURN sqlr;
	SqlStatement sstmt;
	StrUni suColumnName;
	StrUni suFieldMetaDataSql;
	StrUni suSql;
	StrUni suTableName;
	wchar wchClassName[kcchMaxColNameSize];
	wchar wchFieldName[kcchMaxColNameSize];


	Assert(sdb.IsOpen());


	/*------------------------------------------------------------------------------------------
		If a pointer to a HashMap of field$ metadata information was not passed in as a
		parameter, create the HashMap.  The metaData includes all field names, field types,
		class names, and destination class name from the field$ and class$ tables.
		This information is needed since only store the "flid" value is stored in the cache
		(as the "tag") so when it comes time to do SQL "insert" and "update" statements,
		one need's to know what table and column needs to be affected.

		REVIEW PaulP:  Should really cache this information globally so that it does not have
		to be reloaded each time the view cache is saved.  The only time this info would have
		to be reloaded is when custom fields are added.  Perhaps we could have a table with
		a single record which indicates the last date/time a change to the database was made.
		This could be compared with the currently loaded date each time the cache is saved to
		see if there is a need to reload the field$ meta information.  Of course, if we
		require that only one user is allowed to be connected for a custom field addition,
		this would not be necessary.

		TOxDO PaulP:  Cleanup on any error exit, including db rollback.
						CheckSqlRc(SQLEndTran(SQL_HANDLE_DBC, sdb.Hdbc(), SQL_ROLLBACK));
	------------------------------------------------------------------------------------------*/
	try
	{
		//  Set up SQL statment to obtain meta data.
		suFieldMetaDataSql.Format(
			L"select f.Id FLID, Type, f.Name FLD_NAME,"
			L" c.Name CLS_NAME"
			L" from Field$ f"
			L" join Class$ c on c.id = f.Class"
			L" order by f.id");
		sstmt.Init(sdb);
		CheckSqlRc(SQLExecDirectW(sstmt.Hstmt(), \
			const_cast<wchar *>(suFieldMetaDataSql.Chars()), SQL_NTS));

		//  Bind Columns.
		rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAMS_PROCESSED_PTR, &cParamsProcessed, 0);
		CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 1, SQL_C_SLONG, &nFlid, isizeof(int),
			&cbFlid));
		CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 2, SQL_C_SLONG, &fmdr.m_iType,
			isizeof(fmdr.m_iType), &cbType));
		CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 3, SQL_C_WCHAR, &wchFieldName,
			kcchMaxColNameSize, &cbFieldName));
		CheckSqlRc(SQLBindCol(sstmt.Hstmt(), 4, SQL_C_WCHAR, &wchClassName,
			kcchMaxColNameSize, &cbClassName));

		//  Put info from the columns into a FieldMetaDataRec record and insert
		//  this in the FieldMetaData HashMap.
		for (;;)
		{
			rc = SQLFetch(sstmt.Hstmt());
			if (rc != SQL_SUCCESS)
				break;
			// TOxDO PaulP:  Change this so it just gets a null terminated string!!!
			fmdr.m_suFieldName.Assign(wchFieldName, kcchMaxColNameSize);
			fmdr.m_suClassName.Assign(wchClassName, kcchMaxColNameSize);
			fmdm.Insert(nFlid, fmdr);
		}
		if (rc != SQL_NO_DATA)
		{
			//  REVIEW PaulP (SteveMc): Handle possible error message?
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		sstmt.Clear();
	}
	catch (...)
	{
		sstmt.Clear();
		delete rgbFmt;
		return E_UNEXPECTED;
	}


	/*------------------------------------------------------------------------------------------
		For every key in the "m_soprMods" ObjPropSet set (ie. all the object properties
		that have been updated), look up that key in the appropriate HashMap and form an SQL
		"update" statement according to the field$ "Type".

		This begins a single database transaction.  All the data in the application view cache
		must either be inserted/updated to the database successfully or the action should fail.
		This necessitates that the database connection is in manual-commit mode.

		REVIEW PaulP:  It is probably faster to collect all the properties for a given object
		type and update the table in the database all with one SQL statement but this will
		do for now until the OLE DB stuff is done.
	------------------------------------------------------------------------------------------*/
	try
	{
		ObjPropSet::iterator itops;
		for (itops = m_soprMods.Begin(); itops != m_soprMods.End(); ++itops)
		{
			//  Get the oprKey from the "update object property" Set.
			ObjPropRec & oprKey = itops.GetValue();

			//  Get the field$ metadata information based on the tag of that oprKey.
			fmdm.Retrieve(oprKey.m_tag, &fmdr);

			//  Initialize and form the SQL statment based on the field "type".
			sstmt.Init(sdb);
			switch (fmdr.m_iType)
			{
				case kcptNil:
					ThrowHr(WarnHr(E_NOTIMPL));
					break;

				case kcptBoolean:
					ThrowHr(WarnHr(E_NOTIMPL));
					break;

				case kcptInteger:
					//  Prepare the SQL statement.  (Uses parameters).
					suSql.Format(L"update %s set %s=? where id=?", fmdr.m_suClassName.Chars(),
						fmdr.m_suFieldName.Chars());
					rc = SQLPrepareW(sstmt.Hstmt(), const_cast<wchar *>(suSql.Chars()),
						SQL_NTS);

					//  Get the integer value from the appropriate HashMap.
					if (m_hmoprn.Retrieve(oprKey, &nColVal))
					{
						//  Bind integer value as the first parameter.
						if (nColVal)
						{
							rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT,
								SQL_C_SLONG, SQL_INTEGER, 0, 0, &nColVal, 0, &cbColVal);
						}
						else
						{
							// REVIEW JohnT:  How do we indicate in the HashMap that the user
							//		wants to set an integer to NULL?
							cbColVal = SQL_NULL_DATA;
							rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT,
								SQL_C_SLONG, SQL_INTEGER, 0, 0, NULL, 0, &cbColVal);
						}

						//  Bind the HVO (Id) as the second parameter.
						rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT,
							SQL_C_SLONG, SQL_INTEGER, 0, 0, &(oprKey.m_hvo), 0, &cbHvo);
					}
					else
					{
						ThrowHr(WarnHr(E_UNEXPECTED));
					}
					break;


				case kcptNumeric:
					ThrowHr(WarnHr(E_NOTIMPL));
					break;
				case kcptFloat:
					ThrowHr(WarnHr(E_NOTIMPL));
					break;
				case kcptTime:
					ThrowHr(WarnHr(E_NOTIMPL));
					break;
				case kcptGuid:
					ThrowHr(WarnHr(E_NOTIMPL));
					break;
				case kcptImage:
					ThrowHr(WarnHr(E_NOTIMPL));
					break;
				case kcptGenDate:
					ThrowHr(WarnHr(E_NOTIMPL));
					break;
				case kcptBinary:
					ThrowHr(WarnHr(E_NOTIMPL));
					break;


				case kcptString:
				case kcptBigString:

					//  Prepare the SQL statement.  (Uses parameters.)
					//  REVIEW PaulP:  Should I be hard-coding "_Fmt" in like this?
					suSql.Format(L"update %s set %s=?, %s_Fmt=? where id=?",
						fmdr.m_suClassName.Chars(), fmdr.m_suFieldName.Chars(),
						fmdr.m_suFieldName.Chars());
					rc = SQLPrepareW(sstmt.Hstmt(), const_cast<wchar *>(suSql.Chars()),
						SQL_NTS);

					if (m_hmoprtss.Retrieve(oprKey, qtssColVal))
					{
						//  Obtain a SmartBstr from the COM smart pointer that points
						//  to the TsString.
						CheckHr(qtssColVal->get_Text(&sbstr));
						if (sbstr.Length())
						{
							//  Copy format information of the TsString to byte array rgbFmt.
							hr = qtssColVal->SerializeFmtRgb(rgbFmt, cbFmtBufSize,
								&cbFmtSpaceTaken);
							if (hr != S_OK)
							{
								if (hr == S_FALSE)
								{
									//  If the supplied buffer is too small, try it again with
									//  the value that cbFmtSpaceTaken was set to.  If this
									//   fails, throw error.
									delete rgbFmt;
									rgbFmt = NewObj byte[cbFmtSpaceTaken];
									cbFmtBufSize = cbFmtSpaceTaken;
									CheckHr(qtssColVal->SerializeFmtRgb(rgbFmt, cbFmtBufSize,
										&cbFmtSpaceTaken));
								}
								else
								{
									ThrowHr(WarnHr(E_UNEXPECTED));
								}
							}

							//  Bind the text and format parts of the string to the parameters.
							SQLINTEGER cbTextPart = sbstr.Length() * 2;
							rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT,
								SQL_C_WCHAR, SQL_WVARCHAR, cbTextPart, 0,
								const_cast<wchar *>(sbstr.Chars()), cbTextPart, &cbTextPart);
							SQLINTEGER cbFmtPart = cbFmtSpaceTaken;
							rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT,
								SQL_C_BINARY, SQL_VARBINARY, cbFmtPart, 0, rgbFmt, cbFmtPart,
								&cbFmtPart);
						}
						else
						{
							//  Since the string had no length, bind NULL to the parameters.
							SQLINTEGER cbTextPart = SQL_NULL_DATA;
							rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT,
								SQL_C_WCHAR, SQL_WVARCHAR, 1, 0, NULL, 0, &cbTextPart);
							//  REVIEW PaulP:  Should we set the Fmt info to NULL for NULL
							//     strings or should we use the Fmt info returned from the
							//     TsString?
							SQLINTEGER cbFmtPart = SQL_NULL_DATA;
							rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT,
								SQL_C_BINARY, SQL_VARBINARY, 1, 0, NULL, 0, &cbFmtPart);
						}
					}
					else
					{
						ThrowHr(WarnHr(E_UNEXPECTED));
					}

					//  Bind the HVO (ie. Id) value.
					rc = SQLBindParameter(sstmt.Hstmt(), 3, SQL_PARAM_INPUT, SQL_C_SLONG,
						SQL_INTEGER, 0, 0, &(oprKey.m_hvo), 0, &cbHvo);
					break;


				case kcptBigUnicode:
				case kcptUnicode:

					//  Prepare the SQL statement.  (Uses parameters.)
					suSql.Format(L"update %s set %s=? where id=?", fmdr.m_suClassName.Chars(),
						fmdr.m_suFieldName.Chars());
					rc = SQLPrepareW(sstmt.Hstmt(), const_cast<wchar *>(suSql.Chars()),
						SQL_NTS);

					if (m_hmoprtss.Retrieve(oprKey, qtssColVal))
					{
						//  Obtain a SmartBstr from the COM smart pointer that
						//  points to a TsString.
						CheckHr(qtssColVal->get_Text(&sbstr));
						if (sbstr.Length())
						{
							//  Bind the string parameter.
							SQLINTEGER cbTextPart = sbstr.Length() * 2;
							rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT,
								SQL_C_WCHAR, SQL_WVARCHAR, cbTextPart, 0,
								const_cast<wchar *>(sbstr.Chars()), cbTextPart, &cbTextPart);
						}
						else
						{
							//  If the string had no length, bind NULL to the parameter.
							SQLINTEGER cbTextPart = SQL_NULL_DATA;
							rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT,
								SQL_C_WCHAR, SQL_WVARCHAR, 1, 0, NULL, 0, &cbTextPart);
						}
					}
					else
					{
						ThrowHr(WarnHr(E_UNEXPECTED));
					}

					//  Bind the HVO (ie. Id) value.
					rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_SLONG,
						SQL_INTEGER, 0, 0, &(oprKey.m_hvo), 0, &cbHvo);
					break;

				case kcptOwningAtom:
					//  Update owning field.
					//  Update Owner$ object of CmObject.
					ThrowHr(WarnHr(E_NOTIMPL));
					break;
				case kcptReferenceAtom:
					//  This is basically the same as the Integer case except we obtain the
					//  value from the m_hmoprobj HashMap rather than the m_hmoprn HashMap.
					ThrowHr(WarnHr(E_NOTIMPL));
					break;
				case kcptOwningCollection:
					ThrowHr(WarnHr(E_NOTIMPL));
					break;
				case kcptReferenceCollection:
					//  Affect the joiner table.
					ThrowHr(WarnHr(E_NOTIMPL));
					break;
				case kcptOwningSequence:
					//  Need to update.
					ThrowHr(WarnHr(E_NOTIMPL));
					break;
				case kcptReferenceSequence:
					//  Affect the joiner table.
					ThrowHr(WarnHr(E_NOTIMPL));
					break;
				default:
					ThrowHr(WarnHr(E_UNEXPECTED));
					break;
			}

			//  Execute the SQL update command.
			//rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN, 0);
			rc = SQLExecute(sstmt.Hstmt());
			if (rc == SQL_ERROR || rc == SQL_SUCCESS_WITH_INFO)
			{
				//  TOxDO PaulP:  Error information can be obtained from sqlchMsgTxt.
				sqlr = SQLGetDiagRec(SQL_HANDLE_STMT, sstmt.Hstmt(), 1, sqlchSqlState,
					&sqlnNativeErrPtr, sqlchMsgTxt, kcchErrMsgBuf, &sqlnTextLength);
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			sstmt.Clear();
		}


		/*--------------------------------------------------------------------------------------
			Go through the m_soperMods ObjPropSet set and update columns in the MultiX tables.
		--------------------------------------------------------------------------------------*/
		ObjPropEncSet::iterator itopes;
		for (itopes = m_soperMods.Begin(); itopes != m_soperMods.End(); ++itopes)
		{
			//  Get the operKey from the "update MSA" Set.
			ObjPropEncRec & operKey = itopes.GetValue();

			//  Get the field$ metadata information based on the tag of that operKey.
			fmdm.Retrieve(operKey.m_tag, &fmdr);

			//  Initialize and form the SQL statment.
			sstmt.Init(sdb);
			if (m_hmopertss.Retrieve(operKey, qtssColVal))
			{
				//  Obtain a SmartBstr from the COM smart pointer that points to the TsString.
				CheckHr(qtssColVal->get_Text(&sbstr));
				if (sbstr.Length())
				{
					//  Get the format of the TsString
					hr = qtssColVal->SerializeFmtRgb(rgbFmt, cbFmtBufSize, &cbFmtSpaceTaken);
					if (hr != S_OK)
					{
						if (hr == S_FALSE)
						{
							//  If the supplied buffer is too small, try it again with the value
							//  that cbFmtSpaceTaken was set to.  If this fails, signal error.
							delete rgbFmt;
							rgbFmt = NewObj byte[cbFmtSpaceTaken];
							cbFmtBufSize = cbFmtSpaceTaken;
							CheckHr(qtssColVal->SerializeFmtRgb(rgbFmt, cbFmtBufSize,
								&cbFmtSpaceTaken));
						}
						else
						{
							ThrowHr(WarnHr(E_UNEXPECTED));
						}
					}

					//  Execute stored procedure to set MSA value.
					//  REVIEW PaulP:  This technique isn't going to work for custom fields
					//		since there will likely not be any "Set" stored procedure.
					suSql.Format(L"exec Set_%s_%s %d, %d, ?, ?", fmdr.m_suClassName.Chars(),
						fmdr.m_suFieldName.Chars(), operKey.m_hvo, operKey.m_ws);
					rc = SQLPrepareW(sstmt.Hstmt(), const_cast<wchar *>(suSql.Chars()),
						SQL_NTS);

					//  Bind the text and format parts of the string to the parameters.
					SQLINTEGER cbTextPart = sbstr.Length() * 2;
					rc = SQLBindParameter(sstmt.Hstmt(), 1, SQL_PARAM_INPUT,
						SQL_C_WCHAR, SQL_WVARCHAR, cbTextPart, 0,
						const_cast<wchar *>(sbstr.Chars()), cbTextPart, &cbTextPart);

					SQLINTEGER cbFmtPart = cbFmtSpaceTaken;
					rc = SQLBindParameter(sstmt.Hstmt(), 2, SQL_PARAM_INPUT, SQL_C_BINARY,
					SQL_VARBINARY, cbFmtPart, 0, rgbFmt, cbFmtPart, &cbFmtPart);
				}
				else
				{
					//  REVIEW PaulP:  This technique isn't going to work for custom fields
					//		since there will likely not be any "Set" stored procedure.
					suSql.Format(L"exec Set_%s_%s %d, %d, NULL, NULL",
						fmdr.m_suClassName.Chars(), fmdr.m_suFieldName.Chars(), operKey.m_hvo,
						operKey.m_ws);
					rc = SQLPrepareW(sstmt.Hstmt(), const_cast<wchar *>(suSql.Chars()),
						SQL_NTS);
				}

				//  Execute the SQL update command.
				//rc = SQLSetStmtAttr(sstmt.Hstmt(), SQL_ATTR_PARAM_BIND_TYPE, SQL_PARAM_BIND_BY_COLUMN, 0);
				rc = SQLExecute(sstmt.Hstmt());
				if (rc == SQL_ERROR || rc == SQL_SUCCESS_WITH_INFO)
				{
					//  TOxDO PaulP:  Error information can be obtained from sqlchMsgTxt.
					sqlr = SQLGetDiagRec(SQL_HANDLE_STMT, sstmt.Hstmt(), 1, sqlchSqlState,
						&sqlnNativeErrPtr, sqlchMsgTxt, kcchErrMsgBuf, &sqlnTextLength);
					ThrowHr(WarnHr(E_UNEXPECTED));
				}
				sstmt.Clear();
			}
			else
			{
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
		}


		/*--------------------------------------------------------------------------------------
			Commit the database transaction.
			REVIEW PaulP:  The DataSource MUST be in manual commit mode.
		--------------------------------------------------------------------------------------*/
		CheckSqlRc(SQLEndTran(SQL_HANDLE_DBC, sdb.Hdbc(), SQL_COMMIT));

	}
	catch (...)
	{
		sstmt.Clear();
		delete rgbFmt;
		CheckSqlRc(SQLEndTran(SQL_HANDLE_DBC, sdb.Hdbc(), SQL_ROLLBACK));
		return E_UNEXPECTED;
	}


	/*------------------------------------------------------------------------------------------
		Clear the two Sets containing records of modified properties and MBA's.
	------------------------------------------------------------------------------------------*/
	m_soprMods.Clear();
	m_soperMods.Clear();
	delete rgbFmt; // REVIEW: Shouldn't this be delete[]?

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwCacheDa);
}
/***********************************************************************************************
	Miscellaneous methods
***********************************************************************************************/

// Explicit instantiation
#include <Vector_i.cpp>
template Vector<byte>;
template Vector<wchar>;
template Vector<HVO>;
