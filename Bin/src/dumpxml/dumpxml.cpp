/*----------------------------------------------------------------------------------------------
Copyright 2001 by SIL International. All rights reserved.

File: dumpxml.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	This Win32 console application dumps an XML file from a FieldWorks database.
----------------------------------------------------------------------------------------------*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "Main.h"
#pragma hdrstop

extern "C" char * optarg;
extern "C" int getopt(int argc, char * const argv[], const char * opts);

#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	Connect to the database and save it as an XML file.  Return 0 if successful.
----------------------------------------------------------------------------------------------*/
int DumpDB(const wchar * pwszServer, const wchar * pwszDB, const wchar * pwszXmlFile)
{
	StrUniBuf stubServer(pwszServer);
	StrUniBuf stubDatabase(pwszDB);
	StrUniBuf stubXmlOut(pwszXmlFile);
	if (stubServer.Overflow() || stubDatabase.Overflow() || stubXmlOut.Overflow())
	{
		fwprintf(stderr, L"Out of memory filling static buffers??\n");
		return __LINE__;
	}
	IFwXmlDataPtr qfwxd;
	// Open the database object.
	try
	{
		qfwxd.CreateInstance(CLSID_FwXmlData, CLSCTX_INPROC_SERVER);
		CheckHr(qfwxd->Open(stubServer.Bstr(), stubDatabase.Bstr()));
	}
	catch (...)
	{
		fwprintf(stderr, L"\nCannot open database to write \"%s\".\n", pwszXmlFile);
		fwprintf(stderr, L"    (server=\"%s\", database=\"%s\")\n", pwszServer, pwszDB);
		return __LINE__;
	}
	// Create the writing system factory for the database.
	ILgWritingSystemFactoryPtr qwsf;
	try
	{
		ILgWritingSystemFactoryBuilderPtr qwsfb;
		qwsfb.CreateInstance(CLSID_LgWritingSystemFactoryBuilder);
		CheckHr(qwsfb->GetWritingSystemFactoryNew(stubServer.Bstr(), stubDatabase.Bstr(),
			NULL, &qwsf));
	}
	catch (...)
	{
		fwprintf(stderr,
			L"\nCannot open writing system factory for server \"%s\", database \"%s\"\n",
			pwszServer, pwszDB);
		return __LINE__;
	}
	// Dump the database in XML format.
	try
	{
		CheckHr(qfwxd->SaveXml(stubXmlOut.Bstr(), qwsf, NULL));
	}
	catch (Throwable & thr)
	{
		fwprintf(stderr, L"\nError %S caught writing XML file \"%s\"\n",
			AsciiHresult(thr.Error()), pwszXmlFile);
		fwprintf(stderr, L"    (server=\"%s\", database=\"%s\")\n", pwszServer, pwszDB);
		return __LINE__;
	}
	catch (...)
	{
		fwprintf(stderr, L"\nError caught writing XML file \"%s\"\n", pwszXmlFile);
		fwprintf(stderr, L"    (server=\"%s\", database=\"%s\")\n", pwszServer, pwszDB);
		return __LINE__;
	}
	// Shut down the writing system factory.
	try
	{
		CheckHr(qwsf->Shutdown());
	}
	catch (...)
	{
		fwprintf(stderr,
			L"\nError caught shutting down the writing system factory\n"
			L"\t(server = \"%s\", database = \"%s\")\n",
			pwszServer, pwszDB);
		return __LINE__;
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Main procedure for this console application: dumping a FieldWorks database to XML.
----------------------------------------------------------------------------------------------*/
int _tmain(int argc, wchar * argv[])
{
	// Temporary (?) hack to sidestep bug in Microsoft's C runtime library.
	_set_sbh_threshold(0);

	// Check for memory leaks
	_CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);

	StrUni stuServer;
	StrUni stuInputDB;
	StrUni stuOutputFile;
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
			stuInputDB = stuTemp;
			break;
		case L's':
			stuServer = stuTemp;
			break;
		case L'o':
			stuOutputFile = stuTemp;
			break;
		default:
			fError = true;
			break;
		}
	}

	if (!stuInputDB.Length() || fError)
	{
		printf("\
Usage: dumpxml -d inputdb [-s server] [-o output.xml]\n\
   -d inputdb    - the input database (required)\n\
   -s server     - server where the db is located (default: .\\SILFW)\n\
   -o output.xml - the output filename (default: <inputdb> with .xml appended)\n\
");
		exit(1);
	}

	int ich = stuInputDB.FindStr(L".");
	if ((ich != -1) && (stuInputDB.Right(4) == L".xml"))
	{
		fwprintf(stderr, L"Illegal input database \"%s\": the \".xml\" file is the output!\n", stuInputDB.Chars());
		exit(1);
	}

	if (!stuOutputFile.Length())
	{
		// copy input filename to create output filename
		stuOutputFile = stuInputDB;
		if (ich == -1 || (stuInputDB.Right(4) != L".xml"))
			stuOutputFile.Append(L".xml");
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

	int cErrors = DumpDB(stuServer.Chars(), stuInputDB.Chars(), stuOutputFile.Chars());

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
// compile-command:"cmd.exe /e:4096 /c mkdump.bat "
// End:
