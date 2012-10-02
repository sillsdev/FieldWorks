/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2007 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Database.h
Responsibility:
Last reviewed:

	Defines database specific stuff.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef Database_H
#define Database_H 1

#include <sql.h>
#include <sqlext.h>
#include <sqltypes.h>

enum RDBMS {
	MSSQL, // Microsoft SQL Server
	MYSQL // MySQL
};

const RDBMS CURRENTDB = MSSQL;

/*----------------------------------------------------------------------------------------------
		Generic ODBC type names.  They get defined to either MSSQL or MYSQL types.
		it would be nice to put this here, but there are errors right now
		need to #include the right files
----------------------------------------------------------------------------------------------*/
// REVIEW (SteveMiller): I just changed FB (Firebird) to MYSQL, but I'm not sure what good
// this file is doing here at this point.

/*class OdbcType
{
public:

	RDBMS cdb;
	SQLSMALLINT
		BINARY, BIT, CHAR, DOUBLE,
		FLOAT, SLONG, NUMERIC, SBIGINT, SHORT, DATE, TIME,
		TIMESTAMP, TINYINT, UBIGINT, UTINYINT, WCHAR;

	//constructor
	OdbcType::OdbcType(RDBMS);{
		cdb = db;
		if(cdb == MYSQL){
			BIT = SQL_C_CHAR; fprintf(stdout,"case MYSQL\n");
		}
		else if(cdb == MSSQL){
			BIT = SQL_C_BIT; fprintf(stdout,"case MSSQL\n");
		}
		else {
			BIT = SQL_C_DEFAULT; fprintf(stdout,"default\n");
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
};*/

#endif // !Database_H
