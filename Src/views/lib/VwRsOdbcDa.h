/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwRsOdbcDa.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This file subclasses VwCacheDa to provide mechanisms for loading the cache from ODBC
	record sets and for saving modified data in the cache to the database.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VwRsOdbcDa_INCLUDED
#define VwRsOdbcDa_INCLUDED

// These data structures control the process of loading the cache from a record set.
// Each column is described by a OdbcColSpec object.
// Most types indicate data that is in 1:1 relationship with the object.
// Each record set may contain information about one sequence property; it may possibly
// contain information about multiple occurrences of it.
// A record set for a single object may contain one vector column with m_icolID 0
// to indicate it is information about the objects stored in that vector property of
// the base object passed to the load method.
// Or, a record set may contain a column of OIDs and another column of OIDs for objects
// in sequence properties of the first group. In this case, the record set must be
// ordered by the source object OID; the value of an instance of the vector is obtained
// by concatenating the OIDs in the second column for all sequential occurrences of the
// same ID in the first column.


struct OdbcColSpec // Hungarian ocs
{
	OdbcColSpec()
	{
	}
	OdbcColSpec(DbColType oct, int icolID, PropTag tag, int ws)
		:m_oct(oct), m_icolID(icolID), m_tag(tag), m_ws(ws)
	{
	}
	DbColType  m_oct; // The type of info in the column
	// index of column containing ID of object whose property is in this column
	// indexes are 1-based (1 = 1st column), as in ODBC.
	// index 0 may be used if the property is for the object whose ID is passed as an
	// argument to the load method.
	int m_icolID;
	PropTag m_tag; // indicates which property of that object is in this column
	int m_ws; // writing system, if column contains a MultiString alternative
};

typedef Vector<OdbcColSpec> OdbcCsVec; // Hungarian vocv



struct FieldMetaDataRec  // Hungarian fmdr; Used for saving cache to db.
{
	int m_iType;
	StrUni m_suFieldName;
	StrUni m_suClassName;
};

typedef HashMap<PropTag, FieldMetaDataRec> FieldMetaDataMap;



class VwRsOdbcDa : public VwCacheDa
{
public:
	VwRsOdbcDa();
	// IUnknown methods are inherited unchanged

	// ISilDataAccess methods are inherited unchanged

	// Other public methods
	void Load(SQLHSTMT hstmt, OdbcColSpec * prgocs, int cocs, HVO hvoBase,
		int nrowMax);
	STDMETHOD(Save)(SqlDb & sdb);
	STDMETHOD(IsDirty)(ComBool * pf);

protected:
	void ReadBinary(SQLHSTMT hstmt, int icol, byte * prgbBuf, int cbMaxBuf,
		Vector<byte> & vbData, byte * & prgbData, long & cbRet);
	void ReadUnicode(SQLHSTMT hstmt, int icol, wchar * prgchBuf, int cchMaxBuf,
		Vector<wchar> & vchData, wchar * & prgchData, long & cchRet);

	int m_hvoNext;
};



DEFINE_COM_PTR(VwRsOdbcDa);
#endif // VwRsOdbcDa_INCLUDED