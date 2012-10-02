/*******************************************************************************
* spx_External_UDFs
*
* Description:
*   Miscellaneous functions for Firebird that don't come out of the box.
*   Includes declarations of UDFs from udf_SIL library from Neil Mahyew
*
* Dependencies:
*   spx_Drop_External
*
* Revision History:
*   2 August 2007, Yves Blouin: Created
*   7 May 2008, Steve Miller: Added this header; added commits to avoid the
*     error "Too many concurrent executions of the same request".
*******************************************************************************/

/*****************************************
 *
 *	a s c i i _ c h a r
 *
 *****************************************
 *
 * Functional description:
 *	Returns the ASCII character corresponding
 *	with the value passed in.
 *
 *****************************************/

EXECUTE PROCEDURE spx_Drop_External('udf_ASCII_Char');
COMMIT;
DECLARE EXTERNAL FUNCTION udf_ASCII_Char
	INTEGER
	RETURNS CSTRING(1) FREE_IT
	ENTRY_POINT 'IB_UDF_ascii_char' MODULE_NAME 'ib_udf';
COMMIT;

/*****************************************
 *
 *	l t r i m
 *
 *****************************************
 *
 * Functional description:
 *	Removes leading spaces from the input
 *	string.
 *	Note: This function is NOT limited to
 *	receiving and returning only 255 characters,
 *	rather, it can use as long as 32765
 * 	characters which is the limit on an
 *	INTERBASE character string.
 *
 *****************************************/

EXECUTE PROCEDURE spx_Drop_External('udf_LTrim');
COMMIT;
DECLARE EXTERNAL FUNCTION udf_LTrim
	CSTRING(32765)
	RETURNS CSTRING(32765) FREE_IT
	ENTRY_POINT 'IB_UDF_ltrim' MODULE_NAME 'ib_udf';
COMMIT;

/*****************************************
 *
 *	r t r i m
 *
 *****************************************
 *
 * Functional description:
 *	Removes trailing spaces from the input
 *	string.
 *	Note: This function is NOT limited to
 *	receiving and returning only 80 characters,
 *	rather, it can use as long as 32765
 * 	characters which is the limit on an
 *	INTERBASE character string.
 *
 *****************************************/

EXECUTE PROCEDURE spx_Drop_External('udf_RTrim');
COMMIT;
DECLARE EXTERNAL FUNCTION udf_RTrim
	CSTRING(32765)
	RETURNS CSTRING(32765) FREE_IT
	ENTRY_POINT 'IB_UDF_rtrim' MODULE_NAME 'ib_udf';
COMMIT;


/*****************************************
 *
 *	s t r l e n
 *
 *****************************************
 *
 * Functional description:
 *	Returns the length of a given string.
 *
 *****************************************/

EXECUTE PROCEDURE spx_Drop_External('udf_StrLen');
COMMIT;
DECLARE EXTERNAL FUNCTION udf_StrLen
	CSTRING(32765)
	RETURNS INTEGER BY VALUE
	ENTRY_POINT 'IB_UDF_strlen' MODULE_NAME 'ib_udf';
COMMIT;

/*****************************************
 *
 *	s u b s t r l e n
 *
 *****************************************
 *
 * Functional description:
 *	substr(s,i,l) returns the substring
 *	of s which starts at position i and
 *	ends at position i+l-1, being l the length.
 *	Note: This function is NOT limited to
 *	receiving and returning only 80 characters,
 *	rather, it can use as long as 32765
 * 	characters which is the limit on an
 *	INTERBASE character string.
 *
 *****************************************/

EXECUTE PROCEDURE spx_Drop_External('udf_SubStrLen');
COMMIT;
DECLARE EXTERNAL FUNCTION udf_SubStrLen
	CSTRING(32765), SMALLINT, SMALLINT
	RETURNS CSTRING(32765) FREE_IT
	ENTRY_POINT 'IB_UDF_substrlen' MODULE_NAME 'ib_udf';
COMMIT;

/*
 *	udf_SIL.sql
 *
 *	Declarations of UDFs from udf_SIL library
 *
 *	Neil Mayhew - 01 Dec 2004
 *
 *	$Id: External_UDFs.psql,v 1.3 2007-07-23 21:12:13 blouiny Exp $
 */

--FBUDF_API paramdsc* udf_Char_To_GUID(paramdsc* v)
EXECUTE PROCEDURE spx_Drop_External('udf_Varchar_To_GUID');
COMMIT;
DECLARE EXTERNAL FUNCTION udf_Varchar_To_GUID
CHAR(36) BY DESCRIPTOR
RETURNS CHAR(16) CHARACTER SET OCTETS BY DESCRIPTOR
ENTRY_POINT 'udf_Char_To_GUID' MODULE_NAME 'udf_SIL';
COMMIT;

--FBUDF_API paramdsc* udf_GUID_To_Char(paramdsc* v)
EXECUTE PROCEDURE spx_Drop_External('udf_GUID_To_Varchar');
COMMIT;
DECLARE EXTERNAL FUNCTION udf_GUID_To_Varchar
CHAR(16) CHARACTER SET OCTETS BY DESCRIPTOR
RETURNS CHAR(36) BY DESCRIPTOR
ENTRY_POINT 'udf_GUID_To_Char' MODULE_NAME 'udf_SIL';
COMMIT;

--FBUDF_API paramdsc* udf_New_GUID(paramdsc* v)
EXECUTE PROCEDURE spx_Drop_External('udf_New_GUID');
COMMIT;
DECLARE EXTERNAL FUNCTION udf_New_GUID
CHAR(16) CHARACTER SET OCTETS BY DESCRIPTOR
RETURNS PARAMETER 1
ENTRY_POINT 'udf_New_GUID' MODULE_NAME 'udf_SIL';
COMMIT;

--FBUDF_API paramdsc* udf_Tinyint(paramdsc* v)
EXECUTE PROCEDURE spx_Drop_External('udf_Tinyint');
COMMIT;
declare external function udf_Tinyint
INTEGER BY descriptor
returns CHAR(1) character set octets BY descriptor
entry_point 'udf_Tinyint' module_name 'udf_SIL';
COMMIT;

--FBUDF_API paramdsc* udf_Bit(paramdsc* v)
EXECUTE PROCEDURE spx_Drop_External('udf_Bit');
COMMIT;
declare external function udf_Bit
INTEGER BY descriptor
returns CHAR(2) character set octets BY descriptor
entry_point 'udf_Bit' module_name 'udf_SIL';
COMMIT;

--FBUDF_API paramdsc* udf_Char_To_Int(paramdsc* v)
EXECUTE PROCEDURE spx_Drop_External('udf_Char_To_Int');
COMMIT;
declare external function udf_Char_To_Int
CHAR(2) character set octets BY descriptor
returns INTEGER BY descriptor
entry_point 'udf_Char_To_Int' module_name 'udf_SIL';
COMMIT;

EXECUTE PROCEDURE spx_Drop_External('udf_StrPos');
COMMIT;
DECLARE EXTERNAL FUNCTION udf_StrPos
CSTRING(32765), CSTRING(32765)
RETURNS Integer BY VALUE
ENTRY_POINT 'fn_strpos'
MODULE_NAME 'rfunc';
COMMIT;
