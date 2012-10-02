/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilSil2.cpp
Responsibility: Steve McConnel
Last reviewed:

Description:
	Code for SIL utility functions that use the Ethnologue database.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	Convert the Ethnologue language code string to an equivalent ISO language code string, or at
	least to something that doesn't conflict with the defined set of ISO codes.

	The older versions of FieldWorks imposed a 3-letter all uppercase code on all languages
	(writing systems).  This was supposed to be the Ethnologue code for the language, although
	any three letters would be accepted.  The newer versions want to use the ISO 639 values,
	which allow either 2 or 3 letter codes, all lowercase.  Using more than three letters is
	okay for our purposes, and prevents conflicts with the existing standard as of 2003.

	@param pszEthCode language code string, possibly from the Ethnologue.

	@return Pointer to an equivalent ISO language code (or private code), or possibly the input
				string if it looks okay already.  This value must be stored by the caller before
				anyone calls this function again.
----------------------------------------------------------------------------------------------*/
const wchar * SilUtil::ConvertEthnologueToISO(const wchar * pszEthCode)
{
	// If it's not at least 3 letters long, it can't be a FieldWorks Ethnologue code.
	int cchEthCode = wcslen(pszEthCode);
	if (cchEthCode < 3)
		return pszEthCode;
	// If the first 3 chars are not all caps, it can't be a FieldWorks Ethnologue code.
	if (!iswupper(pszEthCode[0]) || !iswupper(pszEthCode[1]) || !iswupper(pszEthCode[2]))
		return pszEthCode;

#define MAXETHCODESIZE 32		// in theory, should be <= 8 anyway.
	static wchar rgchT[MAXETHCODESIZE+2];	// + 1 for leading 'e', + 1 for terminating NUL.

	if (cchEthCode == 3)
	{
		IOleDbEncapPtr qode;
		qode.CreateInstance(CLSID_OleDbEncap);
		StrUni stuSvrName(SilUtil::LocalServerName());
		StrUni stuDbName(L"Ethnologue");
		HRESULT hr;
		try
		{
			CheckHr(hr = qode->Init(stuSvrName.Bstr(), stuDbName.Bstr(), NULL, koltMsgBox,
				koltvForever));
		}
		catch (Throwable& thr)
		{
			hr = thr.Result();
		}
		if (SUCCEEDED(hr))
		{
			IOleDbCommandPtr qodc;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;

			CheckHr(qode->CreateCommand(&qodc));
			StrUni stuSql;
			stuSql.Format(L"declare @nvcIcuCode nvarchar(4);%n"
				L"exec GetIcuCode N'%s', @nvcIcuCode output;%n"
				L"select @nvcIcuCode", pszEthCode);
			CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (fMoreRows)
			{
				// Fetch a NUL-terminated string value.
				CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(rgchT),
					isizeof(rgchT), &cbSpaceTaken, &fIsNull, 2));
				if (!fIsNull)
				{
					// Remove trailing spaces from the returned string.
					int cch = cbSpaceTaken / isizeof(wchar);
					int ich;
					for (ich = cch - 1; ich >= 0; --ich)
					{
						if (rgchT[ich])
						{
							if (iswspace(rgchT[ich]))
								rgchT[ich] = 0;
							else
								break;
						}
					}
					if (iswupper(rgchT[0]))
					{
						for (ich = 0; ich < cch && rgchT[ich]; ++ich)
							rgchT[ich] = towlower(rgchT[ich]);
					}
					return rgchT;
				}
			}
		}
	}

	// convert the input string to lower-case, and prepend an 'x'.  We can hope this won't
	// conflict with any existing codes since this occurs only at the time we changing from
	// the old Ethnologue based codes to the new ISO based codes.
	Assert(cchEthCode <= MAXETHCODESIZE);
	if (cchEthCode > MAXETHCODESIZE)
		cchEthCode = MAXETHCODESIZE;
	rgchT[0] = 'x';
	for (int ich = 0; ich < cchEthCode; ++ich)
		rgchT[ich+1] = towlower(pszEthCode[ich]);
	rgchT[cchEthCode + 1] = 0;
	return rgchT;
}
