/*----------------------------------------------------------------------------------------------
OleDbEncapClientCode.cpp

Sample client code for the OleDbEncap class (ie the one that encapsulates the OLE DB access
to the SQL Server 7 database).

To build this test application, open a DOS screen.  Change directories to
<whatever>\fw\src\DbAccess\  and execute the following command:
	midl iOleDbEncap.idl
----------------------------------------------------------------------------------------------*/

#include <stdio.h>
#include "..\..\src\DbAccess\main.h"



#define DBINITCONSTANTS   // Store all OLE DB consts inside this .obj file.


typedef ULONG HVO;



int main()
{
	ComBool fIsNull = TRUE;
	ComBool fMoreRows;
	HRESULT hr;
	long lTemp;
	ULONG luId;
	ULONG luSpaceTaken;
	int nId;
	wchar_t * pwszTemp = NULL;
	IOleDbEncapPtr qode;
	IOleDbCommandPtr qodc;
	IOleDbCommandPtr qodc2;
	StrUni suDatabase = L"TestLangProj";
	StrUni suEthnologueCode = L"ABC";
	StrUni suEthnologueCode2 = L"XYZ";
	StrUni suServer = L""; // (local)
	StrUni suParamName = L"@id";
	StrUni suSqlDelete = L"delete cmobject where Id=9999998";
	StrUni suSqlDelete2 = L"delete MultiBigStr$ where obj=9999998";
	StrUni suSqlInsert = L"insert into cmobject (Guid$, Class$, Owner$, OwnFlid$) " \
		L"values(newid(), 1, NULL, NULL)";
	StrUni suSqlInsert2 = L"insert into MultiBigStr$ (Flid, Obj, Enc, Txt, Fmt) values("\
		L"5016005, 9999998, 740664001, '-', 0x00)";
	StrUni suSqlSelect0 = L"select contents a, contents b from StTxtPara where id=1351";
	StrUni suSqlSelect1 = L"select ethnologuecode from LangProject";
	StrUni suSqlSelect2 = L"select cast('zzz' as nvarchar) from LangProject";
	StrUni suSqlSelect3 = L"select obj, 'Hello' + cast(obj as nvarchar) GREETING, txt " \
		L"from MultiBigStr$ where obj=17060 or obj=1707";
	StrUni suSqlSelect4 = L"select ethnologuecode from LangProject where id = ?";
	StrUni suSqlSelect5 = L"select getDate()";
	StrUni suSqlSelect6 = L"select itm.id, itm3.Dst, itm.Confidence, itm5.Txt, itm.DateCreated, "
		L"itm.DateModified from CmPossibility_ as itm "
		L"left outer join CmPossibility_Discussion as itm3 on itm3.Src = itm.id "
		L"left outer join CmPossibility_Name as itm5 on itm5.obj = itm.Confidence "
		L"and itm5.enc = 740664001 "
		L"where itm.id in (315); "
		L"select itm.obj, itm.txt, itm.flid, itm.enc, itm.fmt "
		L"from CmPossibility_Name itm "
		L"where itm.obj in (315) and itm.enc in (740664001,931905001) "
		L"union all "
		L"select itm.obj, itm.txt, itm.flid, itm.enc, itm.fmt "
		L"from CmPossibility_Abbreviation itm where itm.obj in (315) and itm.enc in "
		L"(740664001,931905001) "
		L"union all "
		L"select itm.obj, itm.txt, itm.flid, itm.enc, itm.fmt "
		L"from CmPossibility_Description itm "
		L"where itm.obj in (315) and itm.enc in (740664001,931905001)";
	StrUni suSqlStoredProcMultiRowset = L"exec TestMultiRowsets$ ?, ? output";
	StrUni suSqlStoredProcWithOutputParam = L"exec newobjid$ ? output";
	StrUni suSqlStoredProcWithOutputParam2 = L"exec TestIOParam$ ?, ? output, ? output";
	StrUni suSqlUpdate = L"update LangProject set EthnologueCode='ZZZ' where Id<>1";
	StrUni suSqlUpdateWithParam = L"update LangProject set EthnologueCode=? where Id=?";
	StrUni suSqlUpdateWithParam2 = L"update MultiBigStr$ set txt=? where obj=?";
	char * szBigBlob;
	char szParamOut[20] = "QQQ";
	char szTemp[1024] = "LMN";
	WCHAR wszParamInOut[20] = L"Hello";
	WCHAR wszTemp[1024];
	IUnknown * pIOleDbCommand = NULL;


	//  Initialize OLE
	hr = CoInitialize(NULL);
	if (FAILED(hr))	return EXIT_FAILURE;

	//  Create Instance of OleDbEncap object and open DataSource/Session.
	qode.CreateInstance(CLSID_OleDbEncap);
	qode->Init(suServer.Bstr(), suDatabase.Bstr());


	/*------------------------------------------------------------------------------------------
	EXAMPLE 1:
	Simple SQL select statement retrieving a single column (a wide character string).
		select ethnologuecode from LangProject
	------------------------------------------------------------------------------------------*/
	printf("\n===============================\nEXAMPLE 1\n===============================\n");
	qode->CreateCommand(&qodc);
	qodc->ExecCommand(suSqlSelect1.Bstr(), knSqlStmtSelectWithOneRowset);
	qodc->GetRowset(1);
	qodc->NextRow(&fMoreRows);
	pwszTemp = reinterpret_cast<WCHAR *> (CoTaskMemAlloc(sizeof(WCHAR) * 20));
	while (fMoreRows)
	{
		luSpaceTaken = 0;
		qodc->GetColValue(1, reinterpret_cast <ULONG *>(pwszTemp), sizeof(WCHAR) * 20,
			&luSpaceTaken, &fIsNull, 2);
		if (luSpaceTaken)
		{
			//  Not a NULL string.
			wprintf(L"Value: %s\n", pwszTemp);
		}
		else
		{
			// NULL string
			wprintf(L"Id: <null>\n");
		}
		qodc->NextRow(&fMoreRows);
	}


	/*------------------------------------------------------------------------------------------
	EXAMPLE 2:
	Simple SQL select statement retrieving the current date and time in a single column
	(ie. a datetime column).
		select getDate()
	------------------------------------------------------------------------------------------*/
	printf("\n===============================\nEXAMPLE 2\n===============================\n");
	qode->CreateCommand(&qodc);
	qodc->ExecCommand(suSqlSelect5.Bstr(), knSqlStmtSelectWithOneRowset);
	qodc->GetRowset(knRowsetBufferDefaultRows);
	qodc->NextRow(&fMoreRows);
	pwszTemp = reinterpret_cast<WCHAR *> (CoTaskMemAlloc(sizeof(DBTIMESTAMP)));
	while (fMoreRows)
	{
		luSpaceTaken = 0;
		qodc->GetColValue(1, reinterpret_cast <ULONG *>(pwszTemp), sizeof(DBTIMESTAMP),
			&luSpaceTaken, &fIsNull, 0);
		if (luSpaceTaken)
		{
			//  Not a NULL string.
			wprintf(L"DATE - Year: %d  | ", pwszTemp[0]);
			wprintf(L"Month: %d  | ", pwszTemp[1]);
			wprintf(L"Day: %d  | ", pwszTemp[2]);
			wprintf(L"Hour: %d  | ", pwszTemp[3]);
			wprintf(L"Minute: %d  | ", pwszTemp[4]);
			wprintf(L"Second: %d\n", pwszTemp[5]);
		}
		else
		{
			// NULL string
			wprintf(L"DATE - <null>\n");
		}
		qodc->NextRow(&fMoreRows);
	}


	/*------------------------------------------------------------------------------------------
	EXAMPLE 3:
	Simple SQL select statement retrieving a single column (a wide character string).
		select contents a, contents b from StTxtPara where id=1351
	------------------------------------------------------------------------------------------*/
	printf("\n===============================\nEXAMPLE 3\n===============================\n");
	qode->CreateCommand(&qodc);
	qodc->ExecCommand(suSqlSelect0.Bstr(), knSqlStmtSelectWithOneRowset);
	qodc->GetRowset(1);
	qodc->NextRow(&fMoreRows);
	pwszTemp = reinterpret_cast<WCHAR *> (CoTaskMemAlloc(sizeof(WCHAR) * 4000));
	byte * pbTemp = reinterpret_cast<byte *> (CoTaskMemAlloc(sizeof(byte) * 10000));
	while (fMoreRows)
	{
		luSpaceTaken = 0;
		qodc->GetColValue(1, reinterpret_cast <ULONG *>(pwszTemp), sizeof(WCHAR) * 4000,
			&luSpaceTaken, &fIsNull, 2);
		if (luSpaceTaken)
		{
			//  Not a NULL string.
			wprintf(L"Value: %s\n", pwszTemp);
		}
		else
		{
			// NULL string
			wprintf(L"Id: <null>\n");
		}

		qodc->GetColValue(2, reinterpret_cast <ULONG *>(pbTemp), sizeof(byte) * 10000,
			&luSpaceTaken, &fIsNull, 2);
		if (luSpaceTaken)
		{
			//  Not a NULL string.
			wprintf(L"Value: %s\n", pbTemp);
		}
		else
		{
			// NULL string
			wprintf(L"Id: <null>\n");
		}

		qodc->NextRow(&fMoreRows);
	}


	/*------------------------------------------------------------------------------------------
	EXAMPLE 4:
	Simple SQL select statement retrieving multiple columns.
		int (int)
		unicode string (varchar)
		BLOB (ntext)
	This also demonstrates what to do if the client code (ie. this code) allocates too small
	a buffer for the data.
		select obj, 'Hello' + cast(obj as nvarchar) GREETING, txt from MultiBigStr$
		where obj=1650 or obj=1651
	------------------------------------------------------------------------------------------*/
	printf("\n===============================\nEXAMPLE 4\n===============================\n");
	//  Execute SQL Command and obtain Rowset.
	qode->CreateCommand(&qodc);
	qodc->ExecCommand(suSqlSelect3.Bstr(), knSqlStmtSelectWithOneRowset);
	qodc->GetRowset(knRowsetBufferDefaultRows);

	// Go through the rowset output the data
	qodc->NextRow(&fMoreRows);
	while (fMoreRows)
	{
		//--------------------------------------------------------------------------------------
		//	Print out an integer
		//--------------------------------------------------------------------------------------
		qodc->GetColValue(1, (ULONG *) &nId, sizeof(int), &luSpaceTaken, &fIsNull, 0);
		if (luSpaceTaken)
		{
			printf("Id: %d  | ", nId);
		}
		else
		{
			printf("Id: <NULL> | ");
		}

		//--------------------------------------------------------------------------------------
		//	Print out a null terminated string.  Intentionally use a small buffer first, then
		//  reallocate memory after fail and try again.
		//--------------------------------------------------------------------------------------
		luSpaceTaken = 0;
		pwszTemp = reinterpret_cast<WCHAR *> (CoTaskMemAlloc(sizeof(WCHAR) * 2));
		qodc->GetColValue(2, reinterpret_cast <ULONG *>(pwszTemp), 2, &luSpaceTaken, &fIsNull,
			2);
		if (luSpaceTaken > 2)
		{
			//  The data buffer was too small for the string so free it and create a larger
			//  one and try again.
			CoTaskMemFree(pwszTemp);
			pwszTemp = reinterpret_cast<WCHAR *> (CoTaskMemAlloc(sizeof(WCHAR) * luSpaceTaken));
			qodc->GetColValue(2, reinterpret_cast <ULONG *>(pwszTemp), luSpaceTaken,
				&luSpaceTaken, &fIsNull, 2);
		}
		if (luSpaceTaken)
		{
			//  Not a NULL string.
			wprintf(L"Value: %s  | ", pwszTemp);
		}
		else
		{
			// NULL string
			wprintf(L"Value: <null> | ");
		}

		//--------------------------------------------------------------------------------------
		//	Print out a blob (eg. ntext).  Intentionally use a small buffer first, then
		//  reallocate memory after fail and try again.
		//--------------------------------------------------------------------------------------
		luSpaceTaken = 0;
		pwszTemp = reinterpret_cast<WCHAR *> (CoTaskMemAlloc(sizeof(WCHAR) * 1));
		qodc->GetColValue(3, reinterpret_cast <ULONG *>(pwszTemp), sizeof(WCHAR) * 1,
			&luSpaceTaken, &fIsNull, 2);
		if (luSpaceTaken >= 2)
		{
			//  The data buffer was too small for the string so free it and create a larger
			//  one and try again.
			CoTaskMemFree(pwszTemp);
			pwszTemp = reinterpret_cast<WCHAR *> (CoTaskMemAlloc(luSpaceTaken));
			qodc->GetColValue(3, reinterpret_cast <ULONG *>(pwszTemp), luSpaceTaken,
				&luSpaceTaken, &fIsNull, 2);
		}
		if (luSpaceTaken)
		{
			//  Not a NULL string.
			wprintf(L"Value: %s\n\n", pwszTemp);
		}
		else
		{
			// NULL string
			wprintf(L"Value: <null>\n\n");
		}
		wprintf(L"-------------------------------------------------------------------------\n");

		//  Get the next row if there are any.
		qodc->NextRow(&fMoreRows);
	}


	/*------------------------------------------------------------------------------------------
	EXAMPLE 5:
	Multiple SQL select statements.  Note the use of the semi-colon to separate the two
	statements.
		select x from table1; select y from table2
	------------------------------------------------------------------------------------------*/
	printf("\n===============================\nEXAMPLE 5\n===============================\n");
	qode->CreateCommand(&qodc);
	qodc->ExecCommand(suSqlSelect6.Bstr(), knSqlStmtStoredProcedure);
	qodc->GetRowset(1);
	qodc->NextRow(&fMoreRows);
	while (fMoreRows)
	{
		ULONG iNow;
		qodc->GetColValue(1, reinterpret_cast <ULONG *>(&iNow), sizeof(ULONG), &luSpaceTaken,
			&fIsNull, 0);
		if (luSpaceTaken)
		{
			printf("Id: %d  | ", iNow);
		}
		else
		{
			printf("Id: <NULL> | ");
		}
		qodc->NextRow(&fMoreRows);
	}
	printf("\n");
	qodc->GetRowset(1);
	qodc->NextRow(&fMoreRows);
	while (fMoreRows)
	{
		ULONG iNow;
		qodc->GetColValue(1, reinterpret_cast <ULONG *>(&iNow), sizeof(ULONG), &luSpaceTaken,
			&fIsNull, 0);
		if (luSpaceTaken)
		{
			printf("Id: %d  | ", iNow);
		}
		else
		{
			printf("Id: <NULL> | ");
		}
		qodc->NextRow(&fMoreRows);
	}


	/*------------------------------------------------------------------------------------------
	EXAMPLE 6:
	Simple SQL select statement within another SQL select statement.  Both retrieve a single
	column (a wide character string).
		select ethnologuecode from LangProject
		select cast('zzz' as nvarchar) from LangProject
	------------------------------------------------------------------------------------------*/
	printf("\n===============================\nEXAMPLE 6\n===============================\n");
	qode->CreateCommand(&qodc);
	qodc->ExecCommand(suSqlSelect1.Bstr(), knSqlStmtSelectWithOneRowset);
	qodc->GetRowset(knRowsetBufferDefaultRows);
	qodc->NextRow(&fMoreRows);
	pwszTemp = reinterpret_cast<WCHAR *> (CoTaskMemAlloc(sizeof(WCHAR) * 8));
	while (fMoreRows)
	{
		luSpaceTaken = 0;
		qodc->GetColValue(1, reinterpret_cast <ULONG *>(pwszTemp), sizeof(WCHAR) * 8,
			&luSpaceTaken, &fIsNull, 2);
		if (luSpaceTaken)
		{
			//  Not a NULL string.
			wprintf(L"OUTER - Value: %s\n", pwszTemp);
		}
		else
		{
			// NULL string
			wprintf(L"OUTER - Value: <null>\n");
		}

		//  Here is the inner SQL statement.
		qode->CreateCommand(&qodc2);
		qodc2->ExecCommand(suSqlSelect2.Bstr(), knSqlStmtSelectWithOneRowset);
		qodc2->GetRowset(knRowsetBufferDefaultRows);
		qodc2->NextRow(&fMoreRows);
		while (fMoreRows)
		{
			luSpaceTaken = 0;
			qodc2->GetColValue(1, reinterpret_cast <ULONG *>(pwszTemp), sizeof(WCHAR) * 8,
				&luSpaceTaken, &fIsNull, 2);
			if (luSpaceTaken)
			{
				//  Not a NULL string.
				wprintf(L"    INNER - Value: %s\n", pwszTemp);
			}
			else
			{
				// NULL string
				wprintf(L"    INNER - Value: <null>\n");
			}
			qodc2->NextRow(&fMoreRows);
		}
		qodc->NextRow(&fMoreRows);
	}


	/*------------------------------------------------------------------------------------------
	EXAMPLE 7:
	Simple insert, update, and delete SQL statements contained in a single transaction.
	Can demonstrate either commit or rollback.
		insert into cmobject (Guid$, Class$, Owner$, OwnFlid$)
			values(newid(), 1, NULL, NULL)
		update LangProject set EthnologueCode='ZZZ' where Id<>1
		delete cmobject where Id=9999998
	!NOTE1:  You could put all of these SQL statments in one command string, however, you
	 cannot do that	if they have parameters.
	------------------------------------------------------------------------------------------*/
	printf("\n===============================\nEXAMPLE 7\n===============================\n");
	qode->BeginTrans();

	//  Insert a row
	qode->CreateCommand(&qodc);
	qodc->ExecCommand(suSqlInsert.Bstr(), knSqlStmtNoResults);

	//  Update a row
	qode->CreateCommand(&qodc);
	qodc->ExecCommand(suSqlUpdate.Bstr(), knSqlStmtNoResults);

	//  Delete a row
	qode->CreateCommand(&qodc);
	qodc->ExecCommand(suSqlDelete.Bstr(), knSqlStmtNoResults);

	qode->RollbackTrans();
//	qode->CommitTrans();


	/*------------------------------------------------------------------------------------------
	EXAMPLE 8:
	SQL select statement with parameters that retrieves a single column (a wide char string).
		select ethnologuecode from LangProject where id = ?
	------------------------------------------------------------------------------------------*/
	printf("\n===============================\nEXAMPLE 8\n===============================\n");
	qode->CreateCommand(&qodc);
	lTemp = 1;
	qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT | DBPARAMFLAGS_ISSIGNED, NULL,
		DBTYPE_I4, (ULONG *) &lTemp, sizeof(long));
	qodc->ExecCommand(suSqlSelect4.Bstr(), knSqlStmtSelectWithOneRowset);
	qodc->GetRowset(knRowsetBufferDefaultRows);
	qodc->NextRow(&fMoreRows);
	pwszTemp = reinterpret_cast<WCHAR *> (CoTaskMemAlloc(sizeof(WCHAR) * 20));
	while (fMoreRows)
	{
		luSpaceTaken = 0;
		qodc->GetColValue(1, reinterpret_cast <ULONG *>(pwszTemp), sizeof(WCHAR) * 20,
			&luSpaceTaken, &fIsNull, 2);
		if (luSpaceTaken)
		{
			//  Not a NULL string.
			wprintf(L"Value: %s\n", pwszTemp);
		}
		else
		{
			// NULL string
			wprintf(L"Value: <null>\n");
		}
		qodc->NextRow(&fMoreRows);
	}


	/*------------------------------------------------------------------------------------------
	EXAMPLE 9:
	Parameterized SQL update statement with 2 parameters (wide string, long int) inside a
	transaction.  Values are bound to the parameters and then the command is executed.
	This also demonstrates how to set a field to NULL.

	After the command is executed, a different value is set for the second parameter and the
	command is re-executed.

	Finally, new values are given for both parameters and the statement	is executed again.

		update LangProject set EthnologueCode=? where Id=?
	------------------------------------------------------------------------------------------*/
	printf("\n===============================\nEXAMPLE 9\n===============================\n");
	qode->BeginTrans();
	qode->CreateCommand(&qodc);

	//  Bind values to the parameters and execute the command.
	qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR, NULL, 0);
	lTemp = 1;
	qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4,
		(ULONG *) &lTemp, sizeof(long));
	qodc->ExecCommand(suSqlUpdateWithParam.Bstr(), knSqlStmtNoResults);

	//  Bind new value for second parameter and re-execute.
	lTemp = 1663;
	qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT | DBPARAMFLAGS_ISSIGNED, NULL,
		DBTYPE_I4, (ULONG *) &lTemp, sizeof(long));
	qodc->ExecCommand(suSqlUpdateWithParam.Bstr(), knSqlStmtNoResults);

	//  Bind new values to the parameters and re-execute the command.
	//  Note that we first Reset the command to free memory, etc that was allocated for
	//  the previous parameters, etc.
	qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
		(ULONG *) suEthnologueCode2.Chars(), 6);
	lTemp = 1;
	qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT | DBPARAMFLAGS_ISSIGNED, NULL,
		DBTYPE_I4, (ULONG *) &lTemp, sizeof(long));
	qodc->ExecCommand(suSqlUpdateWithParam.Bstr(), knSqlStmtNoResults);

	qode->CommitTrans();


	/*------------------------------------------------------------------------------------------
	EXAMPLE 10:
	Parameterized SQL update statement with 2 parameters (BLOB string, long int)
		update MultiBigStr$ set txt=? where obj=?
	!NOTE:  This transaction is rolled back so the changes will not show up in the database.
	------------------------------------------------------------------------------------------*/
	printf("\n===============================\nEXAMPLE 10\n===============================\n");
	szBigBlob = reinterpret_cast<char *> (CoTaskMemAlloc(20000));
	memset(szBigBlob, 70, 19999);

	qode->BeginTrans();
	qode->CreateCommand(&qodc);
	qodc->ExecCommand(suSqlInsert.Bstr(), knSqlStmtNoResults);

	qode->CreateCommand(&qodc);
	qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT | DBPARAMFLAGS_ISLONG, NULL, DBTYPE_STR,
		(ULONG *) szBigBlob, 20000);
	lTemp = 9999998;
	qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT | DBPARAMFLAGS_ISSIGNED, NULL, DBTYPE_I4,
		(ULONG *) &lTemp, sizeof(long));
	qodc->ExecCommand(suSqlUpdateWithParam2.Bstr(), knSqlStmtNoResults);

	qode->CreateCommand(&qodc);
	qodc->ExecCommand(suSqlDelete2.Bstr(), knSqlStmtNoResults);
	qode->CreateCommand(&qodc);
	qodc->ExecCommand(suSqlDelete.Bstr(), knSqlStmtNoResults);
	qode->RollbackTrans();

	CoTaskMemFree(szBigBlob);


	/*------------------------------------------------------------------------------------------
	EXAMPLE 11:
	Simple stored procedure call with (long integer) output parameter.
	Note that in this example, a name is given for the parameter "@id", however, this is not
	necessary, since we know the ordinal value.  One could just pass in NULL for the name.
		exec newobjid$ ? output
	------------------------------------------------------------------------------------------*/
	printf("\n===============================\nEXAMPLE 11\n===============================\n");
	qode->CreateCommand(&qodc);
	qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, suParamName.Bstr(), DBTYPE_I4, (ULONG *)
		&luId, sizeof(luId));
	qodc->ExecCommand(suSqlStoredProcWithOutputParam.Bstr(), knSqlStmtStoredProcedure);
	qodc->GetParameter(1, &luId, sizeof(luId), &fIsNull);
	if (fIsNull)
	{
		printf("NewObjId$ Value: <NULL>\n");
	}
	else
	{
		printf("NewObjId$ Value: %u\n", luId);
	}


	/*------------------------------------------------------------------------------------------
	EXAMPLE 12:
	Stored procedure call with (wide string) input parameter and (wide string) output parameter.
		exec TestIOParam$ ?, ? output, ? output
	!NOTE:   This requires that you have a stored procedure in the database as follows:
		CREATE proc [TestIOParam$]
			@nInOnlyValue int,
			@szInOutValue varchar(20) output,
			@wszInOutValue nvarchar(20) output
		as
			update LangProject set EthnologueCode = @wszInOutValue where id=@nInOnlyValue
			//-- Must use substring otherwise you get a truncation error --
			set @szInOutValue = substring(cast(@wszInOutValue as varchar), 1, 19)
			set @wszInOutValue = 'It works'
			return 0
	------------------------------------------------------------------------------------------*/
	printf("\n===============================\nEXAMPLE 12\n===============================\n");
	qode->CreateCommand(&qodc);
	luId = 1;
	qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_I4, (ULONG *) &luId,
		sizeof(luId));
	qodc->SetParameter(2, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_STR, (ULONG *) szParamOut, 20);
	qodc->SetParameter(3, DBPARAMFLAGS_ISINPUT | DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_WSTR,
		(ULONG *) wszParamInOut, 40);
	qodc->ExecCommand(suSqlStoredProcWithOutputParam2.Bstr(), knSqlStmtStoredProcedure);
	qodc->GetParameter(2, (ULONG *) szParamOut, 40, &fIsNull);
	if (fIsNull)
	{
		printf("Output Only String (Parmam 2): <NULL>\n");
	}
	else
	{
		printf("Output Only String (Parmam 2): %s\n", szParamOut);
	}
	qodc->GetParameter(3, (ULONG *) wszParamInOut, 40, &fIsNull);
	if (fIsNull)
	{
		printf("Output Only String (Parmam 3): <NULL>\n");
	}
	else
	{
		wprintf(L"Output Only String (Parmam 3): %s\n", wszParamInOut);
	}


	/*------------------------------------------------------------------------------------------
	EXAMPLE 13:
	Stored procedure call with multiple (2) rowsets.

	!NOTE:  This requires that you have a stored procedure in the database as follows:
		CREATE proc [TestMultiRowsets$]
			@wszInOutValue nvarchar(20),
			@cRows  int output
		as
			update LangProject set EthnologueCode = @wszInOutValue
			select top 10 obj, txt from MultiBigStr$;
			select id, EthnologueCode from LangProject union all
				select id, EthnologueCode from LangProject;
			set @cRows = @@rowcount;
			return 0
	-------------------------------------------------------------------------------------------*/
	printf("\n===============================\nEXAMPLE 13\n===============================\n");
	qode->CreateCommand(&qodc);
	qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR, (ULONG *) wszParamInOut, 20);
	luId=0;
	qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT | DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4,
		(ULONG *) &luId, sizeof(luId));
	qodc->ExecCommand(suSqlStoredProcMultiRowset.Bstr(), knSqlStmtStoredProcedure);

	//  Go through the first rowset
	qodc->GetRowset(knRowsetBufferDefaultRows);
	qodc->NextRow(&fMoreRows);
	while (fMoreRows)
	{
		//  Get data from the rowset and do something with it here.
		qodc->GetColValue(1, (ULONG *) &nId, sizeof(int), &luSpaceTaken, &fIsNull, 0);
		if (luSpaceTaken)
		{
			printf("Id: %d  | ", nId);
		}
		else
		{
			printf("Id: <NULL>  | ");
		}

		qodc->GetColValue(2, reinterpret_cast <ULONG *>(wszTemp), 1024, &luSpaceTaken,
			&fIsNull, 2);
		if (luSpaceTaken)
		{
			wprintf(L"Txt: %s\n", wszTemp);
		}
		else
		{
			wprintf(L"Txt: <NULL>\n");
		}

		//  Get more rows.
		qodc->NextRow(&fMoreRows);
	}

	//  Go through the second rowset
	printf("------ Second Rowset ------\n");
	qodc->GetRowset(knRowsetBufferDefaultRows);
	qodc->NextRow(&fMoreRows);
	while (fMoreRows)
	{
		//  Get data from the rowset and do something with it here.
		qodc->GetColValue(1, (ULONG *) &nId, sizeof(int), &luSpaceTaken, &fIsNull, 0);
		if (luSpaceTaken)
		{
			printf("Id: %d  | ", nId);
		}
		else
		{
			printf("Id: <NULL>  | ");
		}


		qodc->GetColValue(2, (ULONG *) wszTemp, sizeof(WCHAR) * 1024, &luSpaceTaken, &fIsNull,
			2);
		if (luSpaceTaken)
		{
			wprintf(L"EthnologueCode: %s\n", wszTemp);
		}
		else
		{
			wprintf(L"EthnologueCode: <NULL>\n");
		}

		//  Get more rows.
		qodc->NextRow(&fMoreRows);
	}
	qodc->GetRowset(knRowsetBufferDefaultRows);
	qodc->GetParameter(2, (ULONG *) &luId, sizeof(luId), &fIsNull);
	wprintf(L"---Number of Rows: %d\n", luId);


	/*------------------------------------------------------------------------------------------
	FINAL EXAMPLE:
	Set the LangProject EthnologueCode's back to the original values.
	------------------------------------------------------------------------------------------*/
	printf("\n===============================\nFINAL EXAMPLE\n===============================\n");
	qode->CreateCommand(&qodc);
	qodc->ExecCommand(L"update LangProject set ethnologueCode='FRN' where id=1",
		knSqlStmtNoResults);
	qode->CreateCommand(&qodc);
	qodc->ExecCommand(L"update LangProject set ethnologueCode='GRM' where id=1663",
		knSqlStmtNoResults);


	//  Cleanup.
	//  TODO PaulP:  cleanup the cleanup.
	if (pwszTemp != NULL)
	{
		CoTaskMemFree(pwszTemp);
	}
	CoUninitialize();
	if (FAILED(hr))
	{
		return EXIT_FAILURE;
	}
	return EXIT_SUCCESS;
}