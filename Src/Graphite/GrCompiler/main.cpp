/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: main.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Main function that runs the compiler.
-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"
// Version: bldinc.h holds the current copyright number and it is created by executing
// bin\mkverrsc.exe from within the bin\mkcle.bat file. The major and minor version
// numbers are hard-coded in mkcle.bat.
#ifdef GR_FW
#include "..\..\..\..\Output\Common\bldinc.h"
#endif // GR_FW

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Forward declarations
***********************************************************************************************/

/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/

/***********************************************************************************************
	Methods: Post-parser
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Run the compiler over the specified GDL and font files.
----------------------------------------------------------------------------------------------*/
int main(int argc, char * argv[])
{
#ifdef _WIN32
	// CRTDebug thing
	_CrtSetDbgFlag(_CrtSetDbgFlag(_CRTDBG_REPORT_FLAG) | _CRTDBG_LEAK_CHECK_DF);
	// COM thing
	::CoInitialize(NULL);
#endif

	char * pchGdlFile = NULL;
	char * pchFontFile = NULL;
	char rgchOutputFile[128];
	utf16 rgchwOutputFontFamily[128];
	memset(rgchOutputFile, 0, isizeof(char) * 128);
	memset(rgchwOutputFontFamily, 0, isizeof(utf16) * 128);

	int cargExtra = 0;

	g_cman.SetOutputDebugFiles(false);
	g_cman.SetFontTableVersion(g_cman.DefaultFontVersion(), false);
	g_cman.SetSeparateControlFile(false);
	g_cman.SetVerbose(true);

	// on linux systems an argument starting with a / may be a path
	// so use - for options. On Windows allow both / or -
#ifdef _WIN32
	while (argc >= 2 + cargExtra
		&& (argv[1 + cargExtra][0] == '/' || argv[1 + cargExtra][0] == '-'))
#else
	while (argc >= 2 + cargExtra && argv[1 + cargExtra][0] == '-')
#endif
	{
		HandleCompilerOptions(argv[1 + cargExtra]);
		cargExtra++;
	}
	if (g_cman.IsVerbose())
	{
#ifdef GR_FW
		std::cout << "Graphite Compiler Version 3.0\n"
			<< COPYRIGHTRESERVED << "\n";
#else
		std::cout << "Graphite Compiler Version 2.4\n"
			<< "Copyright © 2002-2006, by SIL International.  All rights reserved.\n";
#endif // GR_FW
	}

	if (argc < 3 + cargExtra)
	{
		std::cout << "\nusage: grcompiler [options] gdl-file input-font-file [output-font-file] [output-font-name]\n";
		std::cout << "\nOptions:\n";
		std::cout << "   -d     - output debugger files\n";
		std::cout << "   -g     - permit and ignore invalid glyph definitions\n";
		std::cout << "   -nNNNN - set name table start location\n";
		std::cout << "   -vN    - set Silf table version number\n";
		std::cout << "   -q     - quiet mode (no messages except on error)\n";
		return 2;
	}

	pchGdlFile = argv[1 + cargExtra];
	if (argc > 2 + cargExtra)
		pchFontFile = argv[2 + cargExtra];
	if (argc > 3 + cargExtra)
	{
		char * pch = argv[3 + cargExtra];
		strcpy(rgchOutputFile, pch);
		if (argc > 4 + cargExtra)
		{
			pch = argv[4 + cargExtra];
			Platform_AnsiToUnicode(pch, strlen(pch), rgchwOutputFontFamily, strlen(pch));
		}
		//else // nice idea, but don't do this for now
		//{
		//	// output file and no font family; but if the string looks more like a font name, switch
		//	if (LooksLikeFontFamily(rgchOutputFile))
		//	{
		//		//StrAnsi sta(pch);
		//		//StrUni stu = sta;
		//		//wcscpy(rgchwOutputFontFamily, stu.Chars());
		//		::MultiByteToWideChar(CP_ACP, 0, rgchOutputFile, -1,
		//			rgchwOutputFontFamily, strlen(rgchOutputFile));
		//		rgchOutputFile[0] = 0;
		//	}
		//}
	}

	if (rgchOutputFile[0] == 0)
	{
		// Calculate output file name from the input file name.
		if (g_cman.SeparateControlFile())
			GenerateOutputControlFileName(pchFontFile, rgchOutputFile); // gtf
		else
			GenerateOutputFontFileName(pchFontFile, rgchOutputFile);   // ttf
	}

	StrAnsi staVersion = VersionString(g_cman.FontTableVersion());

	GrcFont * pfont = new GrcFont(pchFontFile);
	int nFontError = pfont->Init(&g_cman);

	// Calculate output font-family name.
	utf16 rgchwInputFontFamily[128];
	if (utf16len(rgchwOutputFontFamily) > 0)
	{
	}
	else if (nFontError == 0)
	{
		pfont->GetFontFamilyName(rgchwInputFontFamily, 128);
		if (g_cman.SeparateControlFile())
			GenerateOutputControlFontFamily(rgchwInputFontFamily, rgchwOutputFontFamily);
		else
			utf16cpy(rgchwOutputFontFamily, rgchwInputFontFamily);
	}
	else
	{
		StrUni stu("unknown");
		utf16cpy(rgchwOutputFontFamily, (const utf16*)stu.Chars());
	}

	//StrAnsi staFamily((char*)rgchwOutputFontFamily);
	char rgchFamily[128];
	memset(rgchFamily, 0, sizeof(char) * 128);
	int cchw = 0;
	utf16 * pchw = rgchwOutputFontFamily;
	while (*pchw++)
		cchw++;
	Platform_UnicodeToANSI(rgchwOutputFontFamily, cchw, rgchFamily, 128);
	StrAnsi staFamily(rgchFamily);
	if (g_cman.IsVerbose())
	{
		std::cout << "GDL file: " << pchGdlFile << "\n"
			<< "Input TT file: " << (pchFontFile ? pchFontFile : "none") << "\n"
			<< "Output TT file: " << rgchOutputFile << "\n"
			<< "Output font name: " << staFamily.Chars() << "\n"
			<< "Silf table version " << (g_cman.UserSpecifiedVersion() ? "requested" : "(default)")
					<< ": " << staVersion.Chars() << "\n\n";
	}
	// simple test for illegal UTF encoding in file. GDL requires 7 bit codepoints
	byte bFirst, bSecond, bThird;
	bool fEncodingErr = false;
	std::ifstream strmGdl;
	strmGdl.open(pchGdlFile, std::ios_base::in | std::ios_base::binary);
	strmGdl >> bFirst >> bSecond >> bThird;
	strmGdl.close();

	if (bFirst == 0xFF && bSecond == 0xFE || bFirst == 0xFE && bSecond == 0xFF)
	{
		fEncodingErr = true;
		g_errorList.AddError(130, NULL, "Illegal encoding in GDL file - probably UTF-16 encoding.");
	}
	else if (bFirst == 0xEF && bSecond == 0xBB && bThird == 0xBF)
	{
		fEncodingErr = true;
		g_errorList.AddError(131, NULL, "Illegal encoding in GDL file - probably UTF-8 encoding.");
	}
	else if (bFirst & 0x80 || bSecond & 0x80 || bThird & 0x80)
	{ // not really a UTF check but might as well test for illegal values here
		fEncodingErr = true;
		g_errorList.AddError(132, NULL, "Illegal encoding in GDL file - only 7 bit characters are legal.");
	}

	if (!fEncodingErr)
	{
		if (g_cman.IsVerbose())
			std::cout << "Parsing file " << pchGdlFile << "...\n";
		if (g_cman.Parse(pchGdlFile))
		{
			if (g_cman.IsVerbose()) std::cout << "Initial processing...\n";
			if (g_cman.PostParse())
			{
				if (nFontError == 0)
				{
					if (g_cman.IsVerbose()) std::cout << "Checking for errors...\n";

					if (g_cman.FontTableVersion() > g_cman.MaxFontVersion())
					{
						g_errorList.AddError(133, NULL,
							"Invalid font table version: ",
							VersionString(g_cman.FontTableVersion()).Chars());
					}
					if (g_cman.NameTableStart() != -1
						&& (g_cman.NameTableStart() < g_cman.NameTableStartMin()
							|| g_cman.NameTableStart() > 32767))
					{
						char rgch[20];
						itoa(g_cman.NameTableStart(), rgch, 10);
						g_errorList.AddError(134, NULL,
							"Invalid name table start ID: ", rgch,
							"; must be in range 256 - 32767.");
					}

					if (g_cman.PreCompile(pfont) && !g_errorList.AnyFatalErrors())
					{
						if (g_cman.IsVerbose()) std::cout << "Compiling...\n";
						g_cman.Compile(pfont);
						if (g_cman.OutputDebugFiles())
						{
							g_cman.DebugEngineCode();
							g_cman.DebugRulePrecedence();
							g_cman.DebugGlyphAttributes();
							g_cman.DebugClasses();
							//g_cman.DebugOutput();
							g_cman.DebugCmap(pfont);
							if (g_cman.IsVerbose())
								std::cout << "Debug files generated.\n";
						}
						int nRet = g_cman.OutputToFont(pchFontFile, rgchOutputFile, rgchwOutputFontFamily,
							rgchwInputFontFamily);
						if (nRet == 0)
						{
							if (g_cman.IsVerbose())
								std::cout << "Compilation successful!\n";
						}
						else
						{
							std::cout << "ERROR IN WRITING FONT FILE.\n";
							char rgch[20];
							itoa(nRet, rgch, 10);
							g_errorList.AddError(135, NULL,
								"Error in writing font file--error code = ", rgch);
						}
					}
					else
					{
						std::cout << "Compilation failed.\n";
						g_errorList.AddError(136, NULL,
							"Compilation failed");
					}
				}
				else
				{
					if (nFontError == 7)
					{	// special case - want to avoid font copyright violations
						std::cout << "Font already contains Graphite table(s).\n";
						std::cout << "Please recompile with original (non-Graphite) font.\n";
						// similar error msg already in g_errorList
					}
					std::cout << "Could not open font--error code = " << nFontError << "\n";
					char rgch[20];
					itoa(nFontError, rgch, 10);
					g_errorList.AddError(137, NULL,
						"Could not open font--error code = ", rgch);
				}
			}
			else
			{
				std::cout << "Initial processing failed.\n";
				g_errorList.AddError(138, NULL,
					"Initial processing failed");
			}
		}
		else
		{
			std::cout << "Parsing failed.\n";
			g_errorList.AddError(139, NULL,
				"Parsing failed");
		}
	}
	else
	{
		std::cout << "Illegal encoding in GDL file.\n";
		g_errorList.AddError(140, NULL,
			"Illegal encoding in GDL file");
	}

	g_errorList.SortErrors();
	g_errorList.WriteErrorsToFile(StrAnsi(pchGdlFile), StrAnsi(pchFontFile),
		StrAnsi(rgchOutputFile), staFamily,
		VersionString(g_cman.FontTableVersion()), g_cman.SeparateControlFile());

	int cerrFatal = g_errorList.NumberOfErrors();
	int cerrWarning = g_errorList.NumberOfWarnings();
	int cerrWarningGiven = g_errorList.NumberOfWarningsGiven();	// ie, not ignored
	cerrFatal = cerrFatal - cerrWarning;
	int cerrWarningIgnored = cerrWarning - cerrWarningGiven;

	if (cerrFatal > 0)
	{
		std::cout << cerrFatal << " error" << (cerrFatal > 1 ? "s " : " ");
		if (cerrWarningGiven > 0)
			std::cout << "and " << cerrWarningGiven << " warning" << (cerrWarningGiven > 1 ? "s " : " ");
		std::cout << ((cerrFatal + cerrWarningGiven > 1) ? "have" : "has")
			<< " been output to gdlerr.txt";
		if (cerrWarningIgnored > 0)
			std::cout << " (" << cerrWarningIgnored
				<< ((cerrWarningIgnored > 1) ? " warnings" : " warning") << " ignored)";
		std::cout << ".\n";
	}
	else if (cerrWarningGiven > 0)
	{
		std::cout << cerrWarningGiven << " warning"
			<< (cerrWarningGiven > 1 ? "s have" : " has") << " been output to gdlerr.txt";
		if (cerrWarningIgnored > 0)
			std::cout << " (" << cerrWarningIgnored
				<< ((cerrWarningIgnored > 1) ? " warnings" : " warning") << " ignored)";
		std::cout << ".\n";
	}
	else if (cerrWarningIgnored > 0)
	{
		std::cout << cerrWarningIgnored
			<< ((cerrWarningIgnored > 1) ? " warnings" : " warning") << " ignored.\n";
	}

	delete pfont;

#ifdef _WIN32 // COM thing
	::CoUninitialize();
#endif

	if (g_errorList.AnyFatalErrors())
		return 1;
	else
		return 0;
}

/*----------------------------------------------------------------------------------------------
	Interpret the compiler options, which are preceded by slashes in the argument list.
----------------------------------------------------------------------------------------------*/
void HandleCompilerOptions(char * arg)
{
	if (arg[1] == 'd')
	{
		g_cman.SetOutputDebugFiles(true);
	}
	else if (arg[1] == 'g')
	{
		g_cman.SetIgnoreBadGlyphs(true);
	}
	else if (arg[1] == 'n' || arg[1] == 'v' || arg[1] == 'w')
	{
		int nValue = 0;
		char rgch[20];
		int i = 2;
		while (arg[i] >= '0' && arg[i] <= '9')
		{
			rgch[i - 2] = arg[i];
			nValue = (nValue * 10) + (arg[i] - '0');
			i++;
		}
		rgch[i - 2] = 0;

		if (arg[1] == 'n')
		{
			g_cman.SetNameTableStart(nValue);
		}
		else if (arg[1] == 'v')
		{
			int fxdVersion = nValue << 16; // put in "fixed" format

			// Give an error later if the version is invalid.

			g_cman.SetFontTableVersion(fxdVersion, true);
		}
		else if (arg[1] == 'w')
		{
			g_errorList.SetIgnoreWarning(nValue);
		}
	}
	else if (arg[1] == 'q')
	{
		g_cman.SetVerbose(false);
	}
	//else if (arg[1] == 's')
	//{
	//	g_cman.SetSeparateControlFile(true);
	//}
}

/*----------------------------------------------------------------------------------------------
	Calculate the default name of the output font, based on the input font. If the
	original font is name xyz.ttf, the Graphite version will be xyz_gr.ttf. If we are
	putting the Graphite font tables into a separate control file, the file name will
	be xyz.gtf.
----------------------------------------------------------------------------------------------*/
void GenerateOutputFontFileName(char * pchFontFile, char * pchOutputFont)
{
	char * pchIn = pchFontFile;
	while (*pchIn != 0)
		pchIn++;

	while (*pchIn != '\\' && *pchIn != ':' && pchIn >= pchFontFile)
		pchIn--;
	pchIn++;

	char * pchOut = pchOutputFont;
	while (*pchIn != '.' && *pchIn != 0)
	{
		*pchOut++ = *pchIn++;
	}

	*pchOut++ = '_';
	*pchOut++ = 'g';
	*pchOut++ = 'r';

	while (*pchIn != 0)
		*pchOut++ = *pchIn++;
	*pchOut = 0;
}

/*----------------------------------------------------------------------------------------------
	Calculate the default name of the output control file, based on the input font. We remove
	the extension and append ".gtf".
----------------------------------------------------------------------------------------------*/
void GenerateOutputControlFileName(char * pchFontFile, char * pchOutputFont)
{
	char * pchIn = pchFontFile;
	while (*pchIn != 0)
		pchIn++;

	while (*pchIn != '\\' && *pchIn != ':' && pchIn >= pchFontFile)
		pchIn--;
	pchIn++;

	char * pchOut = pchOutputFont;
	while (*pchIn != '.' && *pchIn != 0)
	{
		*pchOut++ = *pchIn++;
	}

	*pchOut++ = '.';
	*pchOut++ = 'g';
	*pchOut++ = 't';
	*pchOut++ = 'f';
	*pchOut = 0;
}

/*----------------------------------------------------------------------------------------------
	Calculate the font name that will be put into the output control file,
	based on the input font name. We append "Graphite" to the font name.
----------------------------------------------------------------------------------------------*/
void GenerateOutputControlFontFamily(utf16 * pchFontName, utf16 * pchOutputName)
{
	StrUni stu((wchar*)pchFontName);
	stu.Append(" Graphite");

	utf16cpy(pchOutputName, (const utf16*)stu.Chars());
	pchOutputName[stu.Length() + 1] = 0;
}

/*----------------------------------------------------------------------------------------------
	Return true if what is supposedly the output file name looks more like the family name;
	ie, if it has spaces in it and doesn't have '.' or '\'
----------------------------------------------------------------------------------------------*/
bool LooksLikeFontFamily(char * pch)
{
	bool fSpaces = false;
	while (*pch != 0)
	{
		if (*pch == ' ')
			fSpaces = true;
		else if (*pch == '\\' || *pch == '.')
			return false;
		pch++;
	}
	return fSpaces;
}

/*----------------------------------------------------------------------------------------------
	Generate a string containing the version number.
----------------------------------------------------------------------------------------------*/
StrAnsi VersionString(int fxdVersion)
{
	StrAnsi sta = "";
	char rgch[20];
	itoa(fxdVersion >> 16, rgch, 10);
	sta += rgch;
	sta += ".";
	itoa(fxdVersion & 0x0000FFFF, rgch, 10);
	sta += rgch;
	return sta;
}
