/*----------------------------------------------------------------------------------------------
Copyright 2001 by SIL International. All rights reserved.

File: loadxml.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This Win32 console application loads an XML file into a FieldWorks database.
----------------------------------------------------------------------------------------------*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	Connect to the database and load it from the XML file.  Return 0 if successful.
----------------------------------------------------------------------------------------------*/
int LoadDB(const wchar * pwszXmlFile, const wchar * pwszServer, const wchar * pwszDB)
{
	StrUniBuf stubServer(pwszServer);
	StrUniBuf stubDatabase(pwszDB);
	StrUniBuf stubXmlOut(pwszXmlFile);
	if (stubServer.Overflow() || stubDatabase.Overflow() || stubXmlOut.Overflow())
	{
		fwprintf(stderr, L"Out of memory filling static buffers??\n");
		return __LINE__;
	}
	try
	{
		IFwXmlDataPtr qfwxd;
		qfwxd.CreateInstance(CLSID_FwXmlData, CLSCTX_INPROC_SERVER);
		fwprintf(stdout,L"finished CreateInstance\n");
		CheckHr(qfwxd->Open(stubServer.Bstr(), stubDatabase.Bstr()));
		fwprintf(stdout,L"finished qfwxd->Open\n");
		CheckHr(qfwxd->LoadXml(stubXmlOut.Bstr(), NULL));
		fwprintf(stdout,L"finished qfwxd->LoadXml\n");
	}
	catch (Throwable & thr)
	{
		fwprintf(stderr, L"\nError %S caught loading XML file \"%s\" in LoadDB\n",
			AsciiHresult(thr.Error()), stubXmlOut.Chars());
		if (CURRENTDB == MSSQL)
			fwprintf(stderr, L"    (server=\"%s\", database=\"%s\")\n", stubServer.Chars(), stubDatabase.Chars());
		else if (CURRENTDB == FB)
			fwprintf(stderr, L"    (database=\"%s\")\n", stubDatabase.Chars());
		return __LINE__;
	}
	catch (...)
	{
		fwprintf(stderr, L"\nError caught loading XML file \"%s\" in LoadDB\n", stubXmlOut.Chars());
		if (CURRENTDB == MSSQL)
			fwprintf(stderr, L"    (server=\"%s\", database=\"%s\")\n", stubServer.Chars(), stubDatabase.Chars());
		else if (CURRENTDB == FB)
			fwprintf(stderr, L"    (database=\"%s\")\n", stubDatabase.Chars());
		return __LINE__;
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Main procedure for this console application: loading a FieldWorks database from XML.
----------------------------------------------------------------------------------------------*/
int _tmain(int argc, wchar * argv[])
{
	// Temporary (?) hack to sidestep bug in Microsoft's C runtime library.
	_set_sbh_threshold(0);

	// Check for memory leaks
	_CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);

	StrUni stuInputFile = NULL;
	StrUni stuOutputDB = NULL;
	StrUni stuServer = NULL;
	StrUni stuTemp = NULL;

	wchar_t ch;
	bool fError = argc < 2;
	for (int i = 1; i < argc; ++i)
	{
		ch = argv[i][0];
		if (ch != L'-' || argv[i][2] == 0 && i >= argc - 1)
		{
			fError = true;
			break;
		}
		ch = argv[i][1];
		argv[i][2] == 0 ? stuTemp = argv[++i] : stuTemp = argv[i] + 2;
		switch (ch)
		{
		case L'd':
			stuOutputDB = stuTemp;
			break;
		case L'i':
			stuInputFile = stuTemp;
			break;
		case L's':
			stuServer = stuTemp;
			break;
		default:
			fError = true;
			break;
		}
	}

	if (!stuInputFile.Length() || fError)
	{
		printf("\
Usage: loadxml -i input.xml [-d outputdb] [-s server]\n\
   -i input.xml - the input XML file (required)\n\
   -d outputdb  - the output database (default is input with \".xml\" removed)\n\
				  This must be initialized to the correct conceptual model,\n\
				  but otherwise empty.\n\
   -s server    - server where the db is located (default: .\\SILFW)\n\
");
		exit(1);
	}

	if (!stuOutputDB.Length())
	{
		// Copy input filename to create output filename.
		// Find the base filename, allowing either forward or backward slashes as directory
		// delimiters.
		int ich2 = stuInputFile.ReverseFindCh(L'.');
		if (ich2 == -1)
			ich2 = stuInputFile.Length();
		else
			--ich2;
		int ich1 = stuInputFile.ReverseFindCh(L'/', ich2);
		if (ich1 == -1)
			ich1 = stuInputFile.ReverseFindCh(L'\\', ich2);
		stuOutputDB = stuInputFile.Mid(ich1 + 1, ich2 - ich1);
	}
	else
	{
		if (stuOutputDB.Right(4) == L".xml")
		{
			fwprintf(stderr,
				L"Illegal output database \"%s\": the \".xml\" file is the input!\n", stuOutputDB.Chars());
			exit(1);
		}
	}

	if (!stuServer.Length())
	{
		achar psz[MAX_COMPUTERNAME_LENGTH + 1];
		ulong cch = isizeof(psz);
		::GetComputerName(psz, &cch);
		stuServer.Assign(psz);
		stuServer.Append("\\SILFW");
	}

	if (FAILED(CoInitialize(NULL)))
	{
		fwprintf(stderr, L"Cannot initialize COM subsystem! (CoInitialize() failed)\n");
		exit(1);
	}

	int cErrors = LoadDB(stuInputFile.Chars(), stuServer.Chars(), stuOutputDB.Chars());

	CoUninitialize();

	return cErrors;
}

/*----------------------------------------------------------------------------------------------
	Dummy method to keep linker happy.  ModuleEntry assumes a true Win32 app using WinMain(),
	not a console app with main().
----------------------------------------------------------------------------------------------*/
int ModuleEntry::Run(HINSTANCE hinst, LPSTR pszCmdLine, int nShowCmd)
{
	return 0;
}


#include "Vector_i.cpp"
template Vector<char>;

// Local Variables:
// compile-command:"cmd.exe /e:4096 /c mkload.bat "
// End:
