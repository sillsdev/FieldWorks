/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ParserTreeWalker.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Methods to implement the parser, that is, those that call the ANTLR parser and then
	walk the syntax tree to create the objects (Gdl and Grc).
-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "Grp.h"
#include "main.h"
#ifdef _WIN32
#include <io.h> // needed for _dup & _dup2
#else
#include <unistd.h>
#include <sys/wait.h>
#include <errno.h>
#endif

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Forward declarations
***********************************************************************************************/
using std::cout;
using std::endl;
/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/

/***********************************************************************************************
	Methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Run the parser over the file with the given name. Record an error if the file does not
	exist.
----------------------------------------------------------------------------------------------*/
bool GrcManager::Parse(StrAnsi staFileName)
{
	StrAnsi staFilePreProc;
	if (!RunPreProcessor(staFileName, &staFilePreProc))
		return false;

	std::ifstream strmIn;
	strmIn.open(staFilePreProc.Chars());
	if (strmIn.fail())
	{
		g_errorList.AddError(1105, NULL,
			"File ",
			staFileName,
			" does not exist--compilation aborted");
		return false;
	}

	return ParseFile(strmIn, staFilePreProc);
}

/*----------------------------------------------------------------------------------------------
	Run the C pre-processor over the GDL file. Return the name of the resulting file.
----------------------------------------------------------------------------------------------*/
bool GrcManager::RunPreProcessor(StrAnsi staFileName, StrAnsi * pstaFilePreProc)
{
#ifdef _WIN32
	STARTUPINFO sui = {isizeof(sui)};
	//Apparently not needed for DOS programs:
	//sui.wShowWindow = SW_HIDE;
	//sui.dwFlags = STARTF_USESHOWWINDOW;

	// create temporary file and redirect stderr into it
	// ENHANCE AlanW: do this properly? - learn about NT child process inheritance, pipe creation,
	//       and security
	// Search MSDN for "Creating a Child Process with Redirected Input and Output" for
	// more proper way. Using the C RTL is much easier than the proper way with the Win API.
	char * pszPreProcErr = new char[L_tmpnam + strlen("c:")];
	if (!pszPreProcErr)
	{
		g_errorList.AddError(1106, NULL, "Out of memory");
		return false;
	}
	strcpy(pszPreProcErr, "c:");
	tmpnam(pszPreProcErr + strlen("c:"));
	FILE * pFilePreProcErr = fopen(pszPreProcErr, "w+");
	if (!pFilePreProcErr)
	{
		g_errorList.AddError(1107, NULL, "Could not create temporary file to run pre-processor");
		delete[] pszPreProcErr;
		return false;
	}

	// fprintf(stderr, "hello stderr 1\n");
	int nOrigStderr = _dup(2); // save original stderr, 2 is stderr file handle
	if (-1 == _dup2(_fileno(pFilePreProcErr), 2)) //stderr now refers to tmp file opened above
	{
		g_errorList.AddError(1108, NULL, "Could not redirect stderr.");
	}

	PROCESS_INFORMATION procinfo = {0};

	achar rgchErrorCode[20];

	StrApp strCommandLine = _T("gdlpp ");
	if (m_verbose)
		 strCommandLine = _T("gdlpp -V ");
	strCommandLine += staFileName;
	strCommandLine += _T(" $_temp.gdl");	// output file

	// needs to be changed to achar but we then need to make sure that this works with
	// memset and memcpy. This has the possibility of creating a very nasty bug
	achar rgchCommandLine[200];
	_tcscpy(rgchCommandLine, strCommandLine.Chars());
	//memset(rgchCommandLine, 0, 200);
	//memcpy(rgchCommandLine, staCommandLine.Chars(), staCommandLine.Length());

	BOOL f = CreateProcess(NULL,
		rgchCommandLine,
		NULL, NULL, TRUE, 0, NULL, NULL,
		&sui, &procinfo);
	DWORD d = GetLastError();
	if (f == FALSE)
	{
		_itot((int)d, rgchErrorCode, 10);
		g_errorList.AddWarning(1502, NULL,
			"Could not create process to run pre-processor gdlpp.exe (error = ",
			rgchErrorCode,
			"); compiling ",
			staFileName);
		*pstaFilePreProc = staFileName;
		return true;
	}

	HANDLE h = procinfo.hProcess;
	WaitForSingleObject(h, INFINITE);

	// read any errors from gdlpp
	fflush(pFilePreProcErr);
	if (fseek(pFilePreProcErr, 0, SEEK_SET)) // returns 0 on success
	{
		g_errorList.AddError(1109, NULL, "Error in pre-processor temporary file");
		return false;
	}
	RecordPreProcessorErrors(pFilePreProcErr);

	// clean up stderr file & delete tmp file used to catch errors
	if (-1 == _dup2(nOrigStderr, 2)) // restore stderr
	{
		g_errorList.AddError(1110, NULL, "Could not restore stderr from being redirected.");
	}
	// fprintf(stderr, "hello stderr 2\n");
	fclose(pFilePreProcErr);
	unlink(pszPreProcErr);
	delete [] pszPreProcErr;

	ULONG exitCode = 0;
	GetExitCodeProcess(h, &exitCode);
	if (exitCode != 0)
	{
		GrpLineAndFile lnf(0, kMaxFileLineNumber, "");	// make this message come last
		_itot((int)exitCode, rgchErrorCode, 10);
		g_errorList.AddError(1111, NULL,
			"Fatal error in pre-processor gdlpp.exe--compilation aborted",
			lnf);
		g_errorList.SetLastMsgIncludesFatality(true);
		return false;
	}
	*pstaFilePreProc = "$_temp.gdl";
#else
	char tmpgdl[15] = "/tmp/gdlXXXXXX";
	if (mkstemp(tmpgdl)==-1)
	{
		g_errorList.AddError(1112, NULL,
			"Could not create process to run pre-processor gdlpp",
			"compiling ",
			staFileName);
		return false;
	}
	pid_t pid;
	int status, died, testexec;
	switch(pid=fork()){
		case -1: cout << "can't fork\n";
		exit(-1);
		case 0 :
			if (m_verbose)
				testexec = execlp("gdlpp","gdlpp","-V",staFileName.Chars(),tmpgdl,0); // this is the code the child runs
			else
				testexec = execlp("gdlpp","gdlpp",staFileName.Chars(),tmpgdl,0); // this is
			cout << "// exec retval:" << testexec << ", errno:" << strerror(errno) << "(" << errno << ")\n";
			cout << "// tmpfile " << tmpgdl << endl;
			cout << "// file " << staFileName.Chars() << endl;
		g_errorList.AddError(1113, NULL,
			"Failed to run pre-processor gdlpp",
			"compiling ",
			staFileName);
		return false;
		default: died= wait(&status); // this is the code the parent runs
	}
	*pstaFilePreProc = tmpgdl;
#endif

	return true;
}

/*----------------------------------------------------------------------------------------------
	Process the output of the pre-processor--parse any error messages and record them in
	the standard way.
----------------------------------------------------------------------------------------------*/
void GrcManager::RecordPreProcessorErrors(FILE * pFilePreProcErr)
{
	schar rgch[4096];
	size_t cbRead;
	cbRead = fread(rgch, 1, 4096, pFilePreProcErr);

	schar * pch = rgch;
	//	Skip the first 4 lines--copyright info, etc.
	unsigned int nLinesSkipped = 0;
	while (nLinesSkipped < 4 && (unsigned int)(pch - rgch) < cbRead)
	{
		if (*pch == 0x0D && *(pch+1) == 0x0A)
		{
			nLinesSkipped++;
			pch++;
		}
		pch++;
	}
	schar * pchFileMin;
	schar * pchFileLim;
	schar * pchLineNoMin;
	schar * pchLineNoLim;
	schar * pchMsgMin;
	schar * pchMsgLim;

	int nLineNo;
	StrAnsi staMsg;
	GrpLineAndFile lnf;
	lnf.SetPreProcessedLine(0);

	while ((unsigned int)(pch - rgch) < cbRead)
	{
		pchFileMin = pch;
		while (*pch != '(')
		{
			pch++;
			if ((unsigned int)(pch - rgch) >= cbRead)
			{
				Assert(false);
				goto LNextLine;
			}
		}
		pchFileLim = pch;
		pch++;	// skip the '('
		pchLineNoMin = pch;
		while (*pch != ')')
		{
			pch++;
			if ((unsigned int)(pch - rgch) >= cbRead)
			{
				Assert(false);
				goto LNextLine;
			}
		}
		pchLineNoLim = pch;
		pch++;	// skip the ')'

		Assert(*pch == ' ');
		Assert(*(pch + 1) == ':');
		Assert(*(pch + 2) == ' ');

		while (*pch == ' ' || *pch == ':')
			pch++;

		bool fFatal;

		if (*pch == 'w')
		{
			Assert(*(pch+1) == 'a');
			Assert(*(pch+2) == 'r');
			fFatal = false;
		}
		else
		{
			Assert((*pch == 'e' && *(pch+1) == 'r' && *(pch+2) == 'r') ||	// error
				(*pch == 'f' && *(pch+1) == 'a' && *(pch+2) == 't'));		// fatal error
			fFatal = true;
		}

		pchMsgMin = pch;
		while (*pch != 0x0D)
			pch++;
		pchMsgLim = pch;

		// Record error message.
		nLineNo = atoi((const char *)pchLineNoMin);
		lnf.SetFile(StrAnsi(pchFileMin, pchFileLim - pchFileMin));
		lnf.SetOriginalLine(nLineNo);
		if (fFatal)
			g_errorList.AddError(1114, NULL,
				"Gdlpp.exe ",
				StrAnsi(pchMsgMin, pchMsgLim - pchMsgMin),
				lnf);
		else
			g_errorList.AddWarning(1503, NULL,
				"Gdlpp.exe ",
				StrAnsi(pchMsgMin, pchMsgLim - pchMsgMin),
				lnf);
		g_errorList.SetLastMsgIncludesFatality(true);


LNextLine:
		while (*pch != 0x0A)
			pch++;
		pch++;
	}
}


/*----------------------------------------------------------------------------------------------
	Given the name of a file (possibly including path) answer the name of the corresponding
	file output by the pre-processor (not including path). If the name of the file is
	'abc.gdl', the name of outfile file will be 'abc.i'.
	OBSOLETE now that we are not using CL.EXE.
----------------------------------------------------------------------------------------------*/
StrAnsi GrcManager::PreProcName(StrAnsi sta)
{
	int ichMin;
	int ichLim = sta.Length();

	for ( ; ichLim >= 0 && sta.GetAt(ichLim) != '\\' && sta.GetAt(ichLim) != '.'; ichLim--)
		;

	if (sta.GetAt(ichLim) == '.')
	{
		for (ichMin = ichLim; ichMin >= 0 && sta.GetAt(ichMin) != '\\'; ichMin--)
			;
		ichMin++;
	}
	else
	{
		ichMin = ichLim;
		ichLim = sta.Length();
	}

	char rgch[100];
	memset(rgch, 0, 100);
	memcpy(rgch, sta.Chars() + ichMin, (ichLim - ichMin));
	StrAnsi staRet = rgch;
	staRet += ".i";
	return staRet;
}


/*----------------------------------------------------------------------------------------------
	Run the parser over the input file, generating an ANTLR tree, then walk the tree to
	extract the associated data.
	Assumes the input file exists and has been successfully opened.
----------------------------------------------------------------------------------------------*/
bool GrcManager::ParseFile(std::ifstream & strmIn, StrAnsi staFileName)
{
	try
	{
		GrpLexer lexer(strmIn);

		GrpTokenStreamFilter tsf(lexer);
		lexer.init(tsf);
		tsf.init(staFileName);

		GrpParser parser(tsf);
		parser.init(tsf);

		parser.renderDescription();
		RefAST ast = parser.getAST();

		if (g_errorList.AnyFatalErrors())
			return false;

		InitPreDefined();

		WalkParseTree(ast);
	}
	catch(ANTLRException & e)
	{
		// Handle exceptions that happen during parsing.
		g_errorList.AddError(1115, NULL,
			e.getMessage().c_str());
			//e.getLine());
	}

	return true;
}


/*----------------------------------------------------------------------------------------------
	Initialize the data structures with pre-defined stuff. Specifically the ANY class.
----------------------------------------------------------------------------------------------*/
void GrcManager::InitPreDefined()
{
	AddGlyphClass(GrpLineAndFile(), "ANY");
}

/*----------------------------------------------------------------------------------------------
	Walk the parse tree to extract the data and fill in the GdlRenderer.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkParseTree(RefAST ast)
{
	if (ast == NULL)
	{
		g_errorList.AddError(1116, NULL,
			"Invalid input file--compilation aborted");
		return;
	}

	if (OutputDebugFiles())
		DebugParseTree(ast);

	Assert(ast->getType() == Ztop);
	if (ast->getType() != Ztop)
		return;

	WalkTopTree(ast);

	Assert(m_venv.Size() == 1);
}

/*----------------------------------------------------------------------------------------------
	Process the entire parse tree.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkTopTree(RefAST ast)
{
	Assert(ast->getType() == Ztop);
	Assert(ast->getNextSibling() == NULL);

	RefAST astChild = ast->getFirstChild();

	while (astChild)
	{
		int nodetyp = astChild->getType();
		switch (nodetyp)
		{
		case LITERAL_environment:
			WalkEnvTree(astChild, ktbltNone, NULL, NULL);
			break;
		case LITERAL_table:
			WalkTableTree(astChild);
			break;
		case OP_EQ:
		case OP_PLUSEQUAL:
			ProcessGlobalSetting(astChild);
			break;
		default:
			Assert(false);
		}

		astChild = astChild->getNextSibling();
	}
}

/*----------------------------------------------------------------------------------------------
	Process a global setting statement.
	Review: should we also allow directives?
----------------------------------------------------------------------------------------------*/
void GrcManager::ProcessGlobalSetting(RefAST ast)
{
	Assert(ast->getType() == OP_EQ || ast->getType() == OP_PLUSEQUAL);

	RefAST astName = ast->getFirstChild();
	Assert(astName);
	StrAnsi staName = astName->getText().c_str();

	Symbol psym = SymbolTable()->FindSymbol(staName);
	if (!psym || !psym->FitsSymbolType(ksymtGlobal))
	{
		g_errorList.AddError(1117, NULL,
			"Invalid global: ",
			staName,
			LineAndFile(ast));
		return;
	}

	RefAST astValue = astName->getNextSibling();
	Assert(astValue);

	if (staName == "AutoPseudo")
	{
		if (ast->getType() == OP_PLUSEQUAL)
		{
			g_errorList.AddError(1118, NULL,
				"Inappropriate use of += operator",
				LineAndFile(ast));
			return;
		}

		if (astValue->getType() == LITERAL_true)
			m_prndr->SetAutoPseudo(true);
		else if (astValue->getType() == LITERAL_false)
			m_prndr->SetAutoPseudo(false);
		else if (astValue->getType() == LIT_INT)
		{
			int nValue;
			bool fM;
			nValue = NumericValue(astValue, &fM);
			if (fM)
				g_errorList.AddError(1119, NULL,
					"The AutoPseudo global does not expect a scaled number",
					LineAndFile(astValue));
			else
				m_prndr->SetAutoPseudo(nValue != 0);
		}

		if (astValue->getNextSibling())
			g_errorList.AddError(1120, NULL,
				"The AutoPseudo global cannot take multiple values",
				LineAndFile(ast));
	}

	else if (staName == "Bidi")
	{
		if (ast->getType() == OP_PLUSEQUAL)
		{
			g_errorList.AddError(1121, NULL,
				"Inappropriate use of += operator",
				LineAndFile(ast));
			return;
		}

		if (astValue->getType() == LITERAL_true)
			m_prndr->SetBidi(true);
		else if (astValue->getType() == LITERAL_false)
			m_prndr->SetBidi(false);
		else if (astValue->getType() == LIT_INT)
		{
			int nValue;
			bool fM;
			nValue = NumericValue(astValue, &fM);
			if (fM)
				g_errorList.AddError(1122, NULL,
					"The Bidi global does not expect a scaled number",
					LineAndFile(astValue));
			else
				m_prndr->SetBidi(nValue != 0);
		}

		if (astValue->getNextSibling())
			g_errorList.AddError(1123, NULL,
				"The Bidi global cannot take multiple values",
				LineAndFile(ast));
	}

	else if (staName == "ExtraAscent" || staName == "ExtraDescent")
	{
		bool fDescent = (staName == "ExtraDescent");
		GdlNumericExpression * pexpOld
			= (fDescent) ? m_prndr->ExtraDescent() : m_prndr->ExtraAscent();
		if (ast->getType() == OP_EQ)
		{
			if (pexpOld)
			{
				g_errorList.AddWarning(1504, NULL,
					"The ", staName, " global setting overrode a previous value",
					LineAndFile(ast));
				delete pexpOld;
			}
			(fDescent) ? m_prndr->SetExtraDescent(NULL) : m_prndr->SetExtraAscent(NULL);
			pexpOld = NULL;
		}
		if (astValue->getType() != LIT_INT)
		{
			g_errorList.AddError(1124, NULL,
				"Invalid value for ", staName, " global",
				LineAndFile(ast));
		}
		else
		{
			int nValue;
			bool fM;
			nValue = NumericValue(astValue, &fM);
			if (!fM)
				g_errorList.AddWarning(1505, NULL,
					"The ", staName, " global setting expects a scaled number",
					LineAndFile(astValue));

			if (pexpOld && (pexpOld->Units() != MUnits()))
			{
				g_errorList.AddError(1125, NULL,
					"Cannot combine the new value of the ", staName,
					"global with the previous because the units are different",
					LineAndFile(ast));
			}
			else
			{
				GdlNumericExpression * pexpNew;
				if (fM)
					pexpNew = new GdlNumericExpression(nValue, MUnits());
				else
					pexpNew = new GdlNumericExpression(nValue);

				(fDescent) ? m_prndr->SetExtraDescent(pexpNew) : m_prndr->SetExtraAscent(pexpNew);
				if (pexpOld)
					delete pexpOld;
			}
		}

		if (astValue->getNextSibling())
			g_errorList.AddError(1126, NULL,
				"The ", staName, " global cannot take multiple values",
				LineAndFile(ast));
	}

	else if (staName == "ScriptDirection" || staName == "ScriptDirections")
	{
		if (ast->getType() == OP_EQ)
		{
			bool fNotEmpty = m_prndr->ClearScriptDirections();
			if (fNotEmpty)
				g_errorList.AddWarning(1506, NULL,
					staName, " global setting overrode a previous value",
					LineAndFile(ast));
		}

		while (astValue)
		{
			if (astValue->getType() == LIT_INT || astValue->getType() == OP_PLUS)
			{
				int nValue;
				GdlExpression * pexp = WalkExpressionTree(astValue);
				if (!pexp->ResolveToInteger(&nValue, false))
				{
					g_errorList.AddError(1128, pexp,
						"Invalid value for ScriptDirection");
				}
				else
					m_prndr->AddScriptDirection(nValue);
				delete pexp;
			}
			else
				g_errorList.AddError(1129, NULL,
					"Invalid value for ScriptDirection",
					LineAndFile(ast));

			astValue = astValue->getNextSibling();
		}
	}

	else if (staName == "ScriptTags" || staName == "ScriptTag")
	{
		if (ast->getType() == OP_EQ)
		{
			bool fNotEmpty = m_prndr->ClearScriptTags();
			if (fNotEmpty)
				g_errorList.AddWarning(1507, NULL,
					staName, " global setting overrode a previous value",
					LineAndFile(ast));
		}

		while (astValue)
		{
			if (astValue->getType() != LIT_STRING)
			{
				g_errorList.AddError(1130, NULL,
					"The ScriptTags global expects a string value",
					LineAndFile(astValue));
				return;
			}

			StrAnsi sta = astValue->getText().c_str();
			int cb = sta.Length();
			if (cb > 4)
			{
				g_errorList.AddError(1131, NULL,
					"Invalid script tag value--must be a 4-byte string",
					LineAndFile(astValue));
				return;
			}
			else if (cb < 4)
				g_errorList.AddWarning(1508, NULL,
					"Unexpected script tag value--should be a 4-byte string",
					LineAndFile(astValue));

			byte b1, b2, b3, b4;
			b1 = (cb > 0) ? sta[0] : 0;
			b2 = (cb > 1) ? sta[1] : 0;
			b3 = (cb > 2) ? sta[2] : 0;
			b4 = (cb > 3) ? sta[3] : 0;
			int nValue = (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;
			m_prndr->AddScriptTag(nValue);

			astValue = astValue->getNextSibling();
		}

		if (m_prndr->NumScriptTags() > kMaxScriptTags)
		{
			char rgch1[20];
			char rgch2[20];
			itoa(m_prndr->NumScriptTags(), rgch1, 10);
			itoa(kMaxScriptTags, rgch2, 10);
			g_errorList.AddError(1132, NULL,
				"Number of script tags (",
				rgch1,
				") exceeds maximum of ",
				rgch2);
		}
	}

	else
		Assert(false);
}


/*----------------------------------------------------------------------------------------------
	Process an "environment" statement.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkEnvTree(RefAST ast, TableType tblt,
	GdlRuleTable * prultbl, GdlPass * ppass)
{
	Assert(ast->getType() == LITERAL_environment);

	GrpLineAndFile lnf = LineAndFile(ast);
	PushGeneralEnv(lnf);

	RefAST astDirectives = ast->getFirstChild();
	RefAST astContents = astDirectives;
	if (astDirectives && astDirectives->getType() == Zdirectives)
	{
		WalkDirectivesTree(astDirectives);
		astContents = astDirectives->getNextSibling();
	}

	while (astContents)
	{
		int nodetyp = astContents->getType();
		switch (nodetyp)
		{
		case OP_EQ:
		case OP_PLUSEQUAL:
			ProcessGlobalSetting(astContents);
			break;
		default:
			WalkTableElement(astContents, tblt, prultbl, ppass);
		}
		astContents = astContents->getNextSibling();
	}

	PopEnv(lnf, "environment");
}

/*----------------------------------------------------------------------------------------------
	Process a list of environment directives.
	Assumes that parser does not permit a directive to take a list of values.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkDirectivesTree(RefAST ast)
{
	Assert(ast->getType() == Zdirectives);

	RefAST astDirective = ast->getFirstChild();
	while (astDirective)
	{
		StrAnsi staName = astDirective->getFirstChild()->getText().c_str();
		//	For now, all directives have numeric or boolean values.
		RefAST astValue = astDirective->getFirstChild()->getNextSibling();
		Assert(astValue);
		int nValue;
		bool fM = false;
		if (astValue->getType() == LIT_INT)
			nValue = NumericValue(astValue, &fM);
		else if (astValue->getType() == LITERAL_false)
			nValue = 0;
		else if (astValue->getType() == LITERAL_true)
			nValue = 1;
		else
			Assert(false);

		Symbol psym = SymbolTable()->FindSymbol(staName);
		if (!psym || !psym->FitsSymbolType(ksymtDirective))
			g_errorList.AddError(1133, NULL,
				"Invalid directive: ",
				staName,
				LineAndFile(ast));
		else
		{
			if (fM && psym->ExpType() != kexptMeas)
				g_errorList.AddError(1134, NULL,
					"The ",
					staName,
					" directive does not expect a scaled value",
					LineAndFile(ast));

			if (staName == "AttributeOverride")
				SetAttrOverride(nValue != 0);

			else if (staName == "CodePage")
				SetCodePage(nValue);

			else if (staName == "MaxRuleLoop")
				SetMaxRuleLoop(nValue);

			else if (staName == "MaxBackup")
				SetMaxBackup(nValue);

			else if (staName == "MUnits")
				SetMUnits(nValue);

			else if (staName == "PointRadius")
			{
				Assert(psym->ExpType() == kexptMeas);
				if (!fM)
					g_errorList.AddError(1135, NULL,
						"The PointRadius directive requires a scaled value",
						LineAndFile(ast));
				SetPointRadius(nValue, MUnits());
			}
			else
				Assert(false);
		}

		astDirective = astDirective->getNextSibling();
	}
}

/*----------------------------------------------------------------------------------------------
	Process a "table" statement.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkTableTree(RefAST ast)
{
	Assert(ast->getType() == LITERAL_table);

	RefAST astTableType = ast->getFirstChild();
	Assert(astTableType);

	int nodetyp = astTableType->getType();
	switch (nodetyp)
	{
	case LITERAL_glyph:
		WalkGlyphTableTree(ast);
		break;
	case LITERAL_feature:
		WalkFeatureTableTree(ast);
		break;
	case LITERAL_language:
		WalkLanguageTableTree(ast);
		break;
	case LITERAL_name:
		WalkNameTableTree(ast);
		break;
	case LITERAL_substitution:
	case LITERAL_linebreak:
	case LITERAL_position:
	case LITERAL_positioning:
	case LITERAL_justification:
		WalkRuleTableTree(ast, nodetyp);
		break;
	case IDENT:
		g_errorList.AddWarning(1509, NULL,
			"Skipping '",
			astTableType->getText().c_str(),
			"' table--unrecognized table name",
			LineAndFile(ast));
		break;
	default:
		Assert(false);
	}
}

/*----------------------------------------------------------------------------------------------
	Process an element of a table: a -pass-, -table-, -if-. or -environment- statement,
	assignment, or rule.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkTableElement(RefAST ast, TableType tblt,
	GdlRuleTable * prultbl, GdlPass * ppass)
{
	int nodetyp = ast->getType();
	switch (nodetyp)
	{
	case LITERAL_environment:
		WalkEnvTree(ast, tblt, prultbl, ppass);
		break;
	case LITERAL_table:
		WalkTableTree(ast);
		break;
	case Zdirectives:
		WalkDirectivesTree(ast);
		break;
	case LITERAL_pass:
		Assert(prultbl);
		Assert(ktbltRule == tblt);
		WalkPassTree(ast, prultbl, ppass);
		break;
	case ZifStruct:
		Assert(prultbl);
		Assert(ktbltRule == tblt);
		WalkIfTree(ast, prultbl, ppass);
		break;
	case Zrule:
		Assert(prultbl);
		Assert(ktbltRule == tblt);
		WalkRuleTree(ast, prultbl, ppass);
		break;
	default:
		//	Assignment of some sort.
		switch (tblt)
		{
		case ktbltFeature:
			WalkFeatureTableElement(ast);
			break;
		case ktbltLanguage:
			WalkLanguageTableElement(ast);
			break;
		case ktbltName:
			WalkNameTableElement(ast);
			break;
		case ktbltGlyph:
			WalkGlyphTableElement(ast);
			break;
		case ktbltRule:
		default:
			Assert(false);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Process the glyph table.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkGlyphTableTree(RefAST ast)
{
	Assert(ast->getType() == LITERAL_table);
	Assert(ast->getFirstChild()->getType() == LITERAL_glyph);

	RefAST astContents = ast->getFirstChild()->getNextSibling();
	while (astContents)
	{
		int nodetyp = astContents->getType();
		switch (nodetyp)
		{
		case OP_EQ:
		case OP_PLUSEQUAL:
			WalkGlyphTableElement(astContents);
			break;

		default:
			WalkTableElement(astContents, ktbltGlyph, NULL, NULL);
		}

		astContents = astContents->getNextSibling();
	}
}

/*----------------------------------------------------------------------------------------------
	Process a top-level glyph table element.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkGlyphTableElement(RefAST ast)
{
	Assert(ast->getType() == OP_EQ || ast->getType() == OP_PLUSEQUAL);

	Vector<StrAnsi> vsta;
	GdlGlyphClassDefn * pglfc;
	StrAnsi staClassName = ast->getFirstChild()->getText().c_str();
	Symbol psymClass = SymbolTable()->FindSymbol(staClassName);
	if (!psymClass)
	{
		//	Create the class.
		psymClass = SymbolTable()->AddClassSymbol(GrcStructName(staClassName), LineAndFile(ast));
		pglfc = psymClass->GlyphClassDefnData();
		Assert(pglfc);
		pglfc->SetName(staClassName);
		m_prndr->AddGlyphClass(pglfc);
	}
	else
	{
		if (!psymClass->FitsSymbolType(ksymtClass))
		{
			g_errorList.AddError(1136, NULL,
				"Name conflict: '",
				staClassName,
				"' cannot be used as a glyph class name",
				LineAndFile(ast));
			return;
		}
		pglfc = psymClass->GlyphClassDefnData();
		Assert(pglfc);
		if (ast->getType() == OP_EQ)
		{
			g_errorList.AddError(1137, NULL,
				"Duplicate definition of class ",
				staClassName,
				LineAndFile(ast));
			return;
		}
	}
	WalkGlyphClassTree(ast, pglfc);
}

/*----------------------------------------------------------------------------------------------
	Process a single glyph class definition.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkGlyphClassTree(RefAST ast, GdlGlyphClassDefn * pglfc)
{
	//	Skip the class name.
	RefAST astContents = ast->getFirstChild()->getNextSibling();
	while (astContents)
	{
		if (astContents->getType() == Zattrs)
		{
			//	Attributes.
			Vector<StrAnsi> vsta;
			vsta.Push(pglfc->Name());
			RefAST astT = astContents->getFirstChild();
			while (astT)
			{
				WalkGlyphAttrTree(astT, vsta);
				Assert(vsta.Size() == 1);
				astT = astT->getNextSibling();
			}
		}
		else
			//	Class member
			ProcessGlyphClassMember(astContents, pglfc, NULL);

		astContents = astContents->getNextSibling();
	}
}

/*----------------------------------------------------------------------------------------------
	Traverse the glyph attribute assignment tree, adding the assignments to the symbol table
	and master glyph attribute table.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkGlyphAttrTree(RefAST ast, Vector<StrAnsi> & vsta)
{
	if (!ast)
		return;

	RefAST astNextID = ast->getFirstChild();
	vsta.Push(astNextID->getText().c_str());

	int nodetyp = ast->getType();

	if (nodetyp == OP_EQ)
	{
		//	Assignment.
		Symbol psymBase = SymbolTable()->FindSymbol(vsta[1]);
		if (psymBase && !psymBase->FitsSymbolType(ksymtGlyphAttr) &&
			!psymBase->FitsSymbolType(ksymtInvalid))
		{
			g_errorList.AddError(1138, NULL,
				"Invalid glyph attribute name: ",
				psymBase->FullName(),
				LineAndFile(ast));
		}
		else
		{
			RefAST astValue = astNextID->getNextSibling();
			if (astValue->getType() == Zfunction)
				ProcessFunction(astValue, vsta, false);
			else
			{
				GdlExpression * pexpValue = WalkExpressionTree(astValue);

				Symbol psym = SymbolTable()->AddGlyphAttrSymbol(GrcStructName(vsta),
					LineAndFile(ast), pexpValue->ExpType());

				if (psym)
					m_mtbGlyphAttrs->AddItem(psym, pexpValue,
						PointRadius(), PointRadiusUnits(), AttrOverride(), LineAndFile(ast),
						"glyph attr assignment");
			}
		}
	}
	else if (nodetyp == OP_PLUSEQUAL || nodetyp == OP_MINUSEQUAL ||
		nodetyp == OP_MULTEQUAL || nodetyp == OP_DIVEQUAL)
	{
		StrAnsi staOp = ast->getText().c_str();
		g_errorList.AddError(1139, NULL,
			"Cannot assign a glyph attribute with ",
			staOp,
			LineAndFile(ast));
	}
	else
	{
		//	Dot or brace.
		Assert(ast->getType() == OP_DOT || ast->getType() == ZdotStruct);
		RefAST astT = astNextID->getNextSibling();
		while (astT)
		{
			WalkGlyphAttrTree(astT, vsta);
			astT = astT->getNextSibling();
		}
	}

	vsta.Pop();
}

/*----------------------------------------------------------------------------------------------
	Process an attribute whose value is a function: box, point, gpoint, gpath.
	Arguments:
		fSlotAttr			- true if this is a slot attribute, false if it is a glyph attr
		prit, psymOp		- only used for slot attributes
----------------------------------------------------------------------------------------------*/
void GrcManager::ProcessFunction(RefAST ast, Vector<StrAnsi> & vsta,
	bool fSlotAttr, GdlRuleItem * prit, Symbol psymOp)
{
	Assert(ast->getType() == Zfunction);
	Assert(!fSlotAttr || (prit && psymOp));

	RefAST astName = ast->getFirstChild();
	StrAnsi staName = astName->getText().c_str();

	int nPR = PointRadius();
	int mPRUnits = PointRadiusUnits();
	bool fOverride = AttrOverride();
	GrpLineAndFile lnf = LineAndFile(ast);

	RefAST astX1;
	RefAST astX2;
	RefAST astX3;
	RefAST astX4;

	GdlExpression * pexp1 = NULL;
	GdlExpression * pexp2 = NULL;
	GdlExpression * pexp3 = NULL;
	GdlExpression * pexp4 = NULL;

	StrAnsi sta1;
	StrAnsi sta2;
	StrAnsi sta3;
	StrAnsi sta4;

	ExpressionType expt1, expt2, expt3, expt4;

	if (staName == "box")
	{
		sta1 = "left";
		sta2 = "bottom";
		sta3 = "right";
		sta4 = "top";
		expt1 = kexptMeas;
		expt2 = kexptMeas;
		expt3 = kexptMeas;
		expt4 = kexptMeas;
		astX1 = astName->getNextSibling();
		if (astX1)
		{
			pexp1 = WalkExpressionTree(astX1);	// left
			astX2 = astX1->getNextSibling();
			if (astX2)
			{
				pexp2 = WalkExpressionTree(astX2);	// bottom
				astX3 = astX2->getNextSibling();
				if (astX3)
				{
					pexp3 = WalkExpressionTree(astX3);	// right
					astX4 = astX3->getNextSibling();
					if (astX4)
					{
						pexp4 = WalkExpressionTree(astX4);	// top
						if (astX4->getNextSibling())
							BadFunctionError(lnf, staName, "4");
					}
					else
						BadFunctionError(lnf, staName, "4");
				}
				else
					BadFunctionError(lnf, staName, "4");
			}
			else
				BadFunctionError(lnf, staName, "4");
		}
		else
			BadFunctionError(lnf, staName, "4");
	}

	else if (staName == "point")
	{
		sta1 = "x";
		sta2 = "y";
		sta3 = "xoffset";
		sta4 = "yoffset";
		expt1 = kexptMeas;
		expt2 = kexptMeas;
		expt3 = kexptMeas;
		expt4 = kexptMeas;
		astX1 = astName->getNextSibling();
		if (astX1)
		{
			pexp1 = WalkExpressionTree(astX1);	// x
			astX2 = astX1->getNextSibling();
			if (astX2)
			{
				pexp2 = WalkExpressionTree(astX2);	// y
				astX3 = astX2->getNextSibling();
				if (astX3)
				{
					pexp3 = WalkExpressionTree(astX3);	// xoffset
					astX4 = astX3->getNextSibling();
					if (astX4)
					{
						pexp4 = WalkExpressionTree(astX4);	// yoffset
						if (astX4->getNextSibling())
							BadFunctionError(lnf, staName, "2 or 4");
					}
					else
						BadFunctionError(lnf, staName, "2 or 4");
				}
				else
				{
					pexp3 = new GdlNumericExpression(0);	// xoffset
					pexp4 = new GdlNumericExpression(0);	// yoffset
				}
			}
			else
				BadFunctionError(lnf, staName, "2 or 4");
		}
		else
			BadFunctionError(lnf, staName, "2 or 4");
	}

	else if (staName == "gpoint" || staName == "gpath")
	{
		sta1 = staName;
		sta2 = "xoffset";
		sta3 = "yoffset";
		expt1 = kexptNumber;
		expt2 = kexptMeas;
		expt3 = kexptMeas;
		astX1 = astName->getNextSibling();
		if (astX1)
		{
			pexp1 = WalkExpressionTree(astX1);	// gpath or gpoint
			astX2 = astX1->getNextSibling();
			if (astX2)
			{
				pexp2 = WalkExpressionTree(astX2);	// xoffset
				astX3 = astX2->getNextSibling();
				if (astX3)
				{
					pexp3 = WalkExpressionTree(astX3);	// yoffset
					if (astX3->getNextSibling())
						BadFunctionError(lnf, staName, "1 or 3");
				}
				else
					BadFunctionError(lnf, staName, "1 or 3");
			}
			else
			{
				pexp2 = new GdlNumericExpression(0);	// xoffset
				pexp3 = new GdlNumericExpression(0);	// yoffset
			}
		}
		else
			BadFunctionError(lnf, staName, "1 or 3");
	}

	else
	{
		g_errorList.AddError(1140, NULL,
			"Undefined glyph attribute function: ",
			staName,
			LineAndFile(ast));
		return;
	}

	if (pexp1)
	{
		vsta.Push(sta1);
		ProcessFunctionArg(fSlotAttr, GrcStructName(vsta),
			nPR, mPRUnits, fOverride, lnf,
			expt1, prit, psymOp, pexp1);
		vsta.Pop();
	}
	if (pexp2)
	{
		vsta.Push(sta2);
		ProcessFunctionArg(fSlotAttr, GrcStructName(vsta),
			nPR, mPRUnits, fOverride, lnf,
			expt2, prit, psymOp, pexp2);
		vsta.Pop();
	}
	if (pexp3)
	{
		vsta.Push(sta3);
		ProcessFunctionArg(fSlotAttr, GrcStructName(vsta),
			nPR, mPRUnits, fOverride, lnf,
			expt3, prit, psymOp, pexp3);
		vsta.Pop();
	}
	if (pexp4)
	{
		vsta.Push(sta4);
		ProcessFunctionArg(fSlotAttr, GrcStructName(vsta),
			nPR, mPRUnits, fOverride, lnf,
			expt4, prit, psymOp, pexp4);
		vsta.Pop();
	}
}


/*----------------------------------------------------------------------------------------------
	Process a single argument of a function (point, box, gpath, gpoint) either as a glyph
	attribute or a slot attribute.
----------------------------------------------------------------------------------------------*/
void GrcManager::ProcessFunctionArg(bool fSlotAttr, GrcStructName const& xns,
	int nPR, int mPRUnits, bool fOverride, GrpLineAndFile const& lnf,
	ExpressionType expt, GdlRuleItem * prit, Symbol psymOp, GdlExpression * pexpValue)
{
	if (fSlotAttr)
	{
		Symbol psymSlotAttr = SymbolTable()->FindSlotAttr(xns, lnf);
		if (!psymSlotAttr)
			g_errorList.AddError(1141 ,NULL,
				"Invalid slot attribute: ",
				xns.FullString(),
				lnf);
		else
		{
			GdlAttrValueSpec * pavs = new GdlAttrValueSpec(psymSlotAttr, psymOp, pexpValue);
			prit->AddAttrValueSpec(pavs);
		}
	}
	else
	{
		Symbol psymGlyphAttr = SymbolTable()->AddGlyphAttrSymbol(xns, lnf, expt);
		m_mtbGlyphAttrs->AddItem(psymGlyphAttr, pexpValue, nPR, mPRUnits, fOverride, lnf,
			"glyph attr assignment");
	}
}


/*----------------------------------------------------------------------------------------------
	Record an error indicating that a function has the wrong number of arguments.
----------------------------------------------------------------------------------------------*/
void GrcManager::BadFunctionError(GrpLineAndFile & lnf, StrAnsi staFunction,
	StrAnsi staArgsExpected)
{
	g_errorList.AddError(1142, NULL,
		"Invalid number of arguments for '",
		staFunction,
		"'; ",
		staArgsExpected,
		" expected",
		lnf);
}

/*----------------------------------------------------------------------------------------------
	Process a member of a glyph class; either add the member to the class or return it
	to be used in creating a pseudo-glyph.
	Arguments:
		pglfc			- class to add to
		pglfRet			- value to return to embed inside of pseudo;
								one or the other of these arguments will be NULL
----------------------------------------------------------------------------------------------*/
void GrcManager::ProcessGlyphClassMember(RefAST ast,
	GdlGlyphClassDefn * pglfc, GdlGlyphDefn ** ppglfRet)
{
	Assert(!pglfc || !ppglfRet);
	Assert(pglfc || ppglfRet);

	RefAST astItem;
	int nCodePage;
	int nPseudoInput;
	GlyphType glft;
	StrAnsi staSubClassName;
	Symbol psymSubClass;

	GdlGlyphDefn * pglfT;

	GdlGlyphClassDefn * pglfcSub;
	GdlGlyphDefn * pglfForPseudo;

	int nodetyp = ast->getType();
	switch (nodetyp)
	{
	case LITERAL_unicode:		glft = kglftUnicode; break;
	case ZuHex:					glft = kglftUnicode; break;
	case LITERAL_glyphid:		glft = kglftGlyphID; break;
	case LITERAL_codepoint:		glft = kglftCodepoint; break;
	case LITERAL_postscript:	glft = kglftPostscript; break;
	case LITERAL_pseudo:		glft = kglftPseudo; break;
	case IDENT:					break;
	default:
		Assert(false);
	}

	switch (nodetyp)
	{
	case LITERAL_unicode:
	case ZuHex:
	case LITERAL_glyphid:
	case LITERAL_postscript:
		astItem = ast->getFirstChild();
		Assert(astItem->getType() != Zcodepage);
		while (astItem)
		{
			pglfT = ProcessGlyph(astItem, glft);
			if (pglfc)
				pglfc->AddMember(pglfT);
			else
				*ppglfRet = pglfT;	// return for pseudo

			astItem = astItem->getNextSibling();
			if (astItem && !pglfc)
			{
				//	Can't put more than one output glyph in a pseudo.
				g_errorList.AddError(1143, NULL,
					"Pseudo-glyph -> glyph ID mapping contains more than one glyph",
					LineAndFile(astItem));
				break;
			}
		}
		break;

	case LITERAL_codepoint:
		astItem = ast->getFirstChild();
		if (astItem->getType() == Zcodepage)
		{
			Assert(astItem->getFirstChild()->getType() == LIT_INT);
			nCodePage = NumericValue(astItem->getFirstChild());
			astItem = astItem->getNextSibling();
		}
		else
			nCodePage = CodePage();

		while (astItem)
		{
			pglfT = ProcessGlyph(astItem, kglftCodepoint, nCodePage);
			if (pglfc)
				pglfc->AddMember(pglfT);
			else
				*ppglfRet = pglfT;	// return for pseudo

			astItem = astItem->getNextSibling();
			if (astItem && !pglfc)
			{
				//	Can't put more than one output glyph in a pseudo.
				g_errorList.AddError(1144, NULL,
					"Pseudo-glyph -> glyph ID mapping contains more than one glyph",
					LineAndFile(astItem));
				break;
			}
		}

		break;

	case LITERAL_pseudo:
		Assert(pglfc && !ppglfRet);	// can't put a pseudo inside a pseudo

		astItem = ast->getFirstChild();
		ProcessGlyphClassMember(astItem, NULL, &pglfForPseudo);

		if (astItem->getNextSibling())
		{
			RefAST astInput = astItem->getNextSibling();
			nPseudoInput = NumericValue(astInput);
			pglfT = new GdlGlyphDefn(kglftPseudo, pglfForPseudo, nPseudoInput);
		}
		else
			pglfT = new GdlGlyphDefn(kglftPseudo, pglfForPseudo);
		pglfT->SetLineAndFile(LineAndFile(ast));
		pglfc->AddMember(pglfT);

		break;

	case IDENT:
		Assert(!ast->getFirstChild());
		Assert(pglfc && !ppglfRet);	// can't put a subclass identifer inside a pseudo
		staSubClassName = ast->getText().c_str();
		psymSubClass = SymbolTable()->FindSymbol(staSubClassName);
		if (!psymSubClass)
		{
			g_errorList.AddError(1145, NULL,
				"Undefined glyph class: ",
				staSubClassName,
				LineAndFile(ast));
			return;
		}
		if (!psymSubClass->FitsSymbolType(ksymtClass))
		{
			g_errorList.AddError(1146, NULL,
				"Name conflict: '",
				staSubClassName,
				"' cannot be used as a glyph class name",
				LineAndFile(ast));
			return;
		}
		pglfcSub = psymSubClass->GlyphClassDefnData();
		Assert(pglfcSub);
		pglfc->AddMember(pglfcSub);

		break;

	default:
		Assert(false);
	}
}

/*----------------------------------------------------------------------------------------------
	Process the contents of a glyph function ("unicode", "glyphid", etc); return the
	resulting glyph.
----------------------------------------------------------------------------------------------*/
GdlGlyphDefn * GrcManager::ProcessGlyph(RefAST astGlyph, GlyphType glft, int nCodePage)
{
	RefAST ast1;
	RefAST ast2;

	int n1, n2;
	utf16 w1;
	StrAnsi sta;
	utf16 wCodePage = (utf16)nCodePage;

	GdlGlyphDefn * pglfRet;

	int nodetyp = astGlyph->getType();
	switch (nodetyp)
	{
	case OP_DOTDOT:
		//	Range: 0x1111..0x2222, 'a'..'z', U+89ab..U+89ff
		ast1 = astGlyph->getFirstChild();
		Assert(ast1);
		ast2 = ast1->getNextSibling();
		Assert(ast2);
		Assert(!ast2->getNextSibling());
		if (ast1->getType() == LIT_INT || ast1->getType() == LIT_UHEX)
			n1 = NumericValue(ast1);
		else if (ast1->getType() == LIT_CHAR)
			n1 = *(ast1->getText().c_str());
		else
			Assert(false);

		if (ast2->getType() == LIT_INT || ast1->getType() == LIT_UHEX)
			n2 = NumericValue(ast2);
		else if (ast2->getType() == LIT_CHAR)
			n2 = *(ast2->getText().c_str());
		else
			Assert(false);

		pglfRet = (nCodePage == -1) ?
			new GdlGlyphDefn(glft, n1, n2) :
			new GdlGlyphDefn(glft, n1, n2, wCodePage);

		break;

	case LIT_INT:
	case LIT_UHEX:
		n1 = NumericValue(astGlyph);
		pglfRet = (nCodePage == -1) ?
			new GdlGlyphDefn(glft, n1) :
			new GdlGlyphDefn(glft, n1, nCodePage);
		break;

	case LIT_CHAR:
		w1 = (astGlyph->getText())[0];
		pglfRet = (nCodePage == -1) ?
			new GdlGlyphDefn(glft, w1) :
			new GdlGlyphDefn(glft, w1, nCodePage);
		break;

	case LIT_STRING:
		sta = astGlyph->getText().c_str();
		pglfRet = (nCodePage == -1) ?
			new GdlGlyphDefn(glft, sta) :
			new GdlGlyphDefn(glft, sta, wCodePage);
		break;

	default:
		Assert(false);
	}

	pglfRet->SetLineAndFile(LineAndFile(astGlyph));

	return pglfRet;
}

/*----------------------------------------------------------------------------------------------
	Process the feature table.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkFeatureTableTree(RefAST ast)
{
	Assert(ast->getType() == LITERAL_table);
	Assert(ast->getFirstChild()->getType() == LITERAL_feature);

	RefAST astContents = ast->getFirstChild()->getNextSibling();
	while (astContents)
	{
		int nodetyp = astContents->getType();
		switch (nodetyp)
		{
		case OP_DOT:
		case ZdotStruct:
		case OP_EQ:
			WalkFeatureTableElement(astContents);
			break;

		default:
			WalkTableElement(astContents, ktbltFeature, NULL, NULL);
		}

		astContents = astContents->getNextSibling();
	}
}

/*----------------------------------------------------------------------------------------------
	Process a top level element in the feature table.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkFeatureTableElement(RefAST ast)
{
	Vector<StrAnsi> vsta;
	StrAnsi staFeatureName = ast->getFirstChild()->getText().c_str();
	Symbol psymFeat = SymbolTable()->FindSymbol(staFeatureName);
	if (!psymFeat)
	{
		if (staFeatureName == "lang")
		{
			g_errorList.AddError(1147, NULL,
				"Attempt to redefine reserved feature: 'lang'",
				LineAndFile(ast));
			return;
		}
		psymFeat = SymbolTable()->AddFeatureSymbol(GrcStructName(staFeatureName),
			LineAndFile(ast));
		GdlFeatureDefn * pfeat = psymFeat->FeatureDefnData();
		Assert(pfeat);
		pfeat->SetName(staFeatureName);
		m_prndr->AddFeature(pfeat);
	}
	else if (!psymFeat->FitsSymbolType(ksymtFeature))
	{
		g_errorList.AddError(1148, NULL,
			"Identifier conflict: '",
			staFeatureName,
			"' cannot be used as a feature name",
			LineAndFile(ast));
		return;
	}

	vsta.Clear();
	WalkFeatureSettingsTree(ast, vsta);
}

/*----------------------------------------------------------------------------------------------
	Traverse the features identifier tree, adding the assignments to the symbol table
	and master features table.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkFeatureSettingsTree(RefAST ast, Vector<StrAnsi> & vsta)
{
	RefAST astNextID = ast->getFirstChild();
	vsta.Push(astNextID->getText().c_str());

	if (ast->getType() == OP_EQ)
	{
		if (vsta.Size() < 2)
		{
			g_errorList.AddError(1149, NULL,
				"Invalid feature statement",
				LineAndFile(ast));
			return;
		}

		Symbol psym = SymbolTable()->AddSymbol(GrcStructName(vsta), ksymtFeatSetting,
			LineAndFile(ast));

		RefAST astValue = astNextID->getNextSibling();
		if (!astValue)
			return;

		GdlExpression *pexpValue;
		if (astValue->getType() == IDENT)
		{
			//	A kludge, because this isn't a slot-ref expression, but that is the
			//	most convenient thing to hold a simple identifier until we can process it
			//	further (see the master table function that processes the features).
			pexpValue = new GdlSlotRefExpression(astValue->getText().c_str());
			pexpValue->SetLineAndFile(LineAndFile(ast));
		}
		else
		{
			pexpValue = WalkExpressionTree(astValue);
		}
		m_mtbFeatures->AddItem(psym, pexpValue,
			PointRadius(), PointRadiusUnits(), AttrOverride(), LineAndFile(ast),
			"feature setting");
	}
	else
	{
		Assert(ast->getType() == OP_DOT || ast->getType() == ZdotStruct);
		RefAST astT = astNextID->getNextSibling();
		while (astT)
		{
			WalkFeatureSettingsTree(astT, vsta);
			astT = astT->getNextSibling();
		}
	}

	vsta.Pop();
}

/*----------------------------------------------------------------------------------------------
	Process the language table.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkLanguageTableTree(RefAST ast)
{
	Assert(ast->getType() == LITERAL_table);
	Assert(ast->getFirstChild()->getType() == LITERAL_language);

	RefAST astContents = ast->getFirstChild()->getNextSibling();
	while (astContents)
	{
		int nodetyp = astContents->getType();
		switch (nodetyp)
		{
		case OP_DOT:
		case ZdotStruct:
			WalkLanguageTableElement(astContents);
			break;

		default:
			WalkTableElement(astContents, ktbltLanguage, NULL, NULL);
		}

		astContents = astContents->getNextSibling();
	}
}

/*----------------------------------------------------------------------------------------------
	Process a top level element in the language table.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkLanguageTableElement(RefAST ast)
{
	RefAST astLabel = ast->getFirstChild();
	StrAnsi staLabel = astLabel->getText().c_str();

	RefAST astItem = astLabel->getNextSibling();

	// Find or create the language class with that name:
	GdlLangClass * plcls;
  int ilcls;
	for (ilcls = 0; ilcls < m_vplcls.Size(); ilcls++)
	{
		if (m_vplcls[ilcls]->m_staLabel == staLabel)
		{
			plcls = m_vplcls[ilcls];
			break;
		}
	}
	if (ilcls >= m_vplcls.Size())
	{
		plcls = new GdlLangClass(staLabel);
		m_vplcls.Push(plcls);
	}

	WalkLanguageItem(astItem, plcls);
}

/*----------------------------------------------------------------------------------------------
	Process one assignment in the language table.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkLanguageItem(RefAST ast, GdlLangClass * plcls)
{
	RefAST astItem = ast;
	while (astItem)
	{
		Assert(astItem->getType() == OP_EQ);

		RefAST astLhs = astItem->getFirstChild();
		StrAnsi staLhs = astLhs->getText().c_str();
		if (staLhs == "language" || staLhs == "languages")
		{
			if (plcls->m_vplang.Size() > 0)
			{
				g_errorList.AddError(1150, NULL,
					"Redefining list of languages for language group '", plcls->m_staLabel, "'",
					LineAndFile(astLhs));
			}

			RefAST astLangList = astLhs->getNextSibling();
			WalkLanguageCodeList(astLangList, plcls);
		}
		else // feature setting
		{
			RefAST astValue = astLhs->getNextSibling();
			StrAnsi staValue = astValue->getText().c_str();
			GdlExpression * pexpVal = NULL;
			if (astValue->getType() != IDENT)
				pexpVal = WalkExpressionTree(astValue);
			plcls->AddFeatureValue(staLhs, staValue, pexpVal, LineAndFile(astValue));
		}

		astItem = astItem->getNextSibling();
	}
}

/*----------------------------------------------------------------------------------------------
	Process one line in the language table.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkLanguageCodeList(RefAST astList, GdlLangClass * plcls)
{
	RefAST astNext = astList;
	while (astNext && astNext->getType() == LIT_STRING)
	{
		StrAnsi staLang = astNext->getText().c_str();
		if (staLang.Length() > 4)
		{
			g_errorList.AddError(1151, NULL,
				"Language codes may contain a maximum of 4 characters",
				LineAndFile(astNext));
		}
		// Create a language.
		GrcStructName xns(staLang);
		GrpLineAndFile lnf;
		Symbol psymLang = SymbolTable()->AddLanguageSymbol(xns, lnf);
		GdlLanguageDefn * plang = psymLang->LanguageDefnData();
		Assert(plang);
		plang->SetCode(staLang);
		m_prndr->AddLanguage(plang);
		plcls->AddLanguage(plang);

		astNext = astNext->getNextSibling();
	}
}

/*----------------------------------------------------------------------------------------------
	Process the name table.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkNameTableTree(RefAST ast)
{
	Assert(ast->getType() == LITERAL_table);
	Assert(ast->getFirstChild()->getType() == LITERAL_name);

	RefAST astContents = ast->getFirstChild()->getNextSibling();
	while (astContents)
	{
		int nodetyp = astContents->getType();
		switch (nodetyp)
		{
		case OP_DOT:
		case ZdotStruct:
		case OP_EQ:
		case OP_PLUSEQUAL:
			WalkNameTableElement(astContents);
			break;

		default:
			WalkTableElement(astContents, ktbltName, NULL, NULL);
		}
		astContents = astContents->getNextSibling();
	}
}

/*----------------------------------------------------------------------------------------------
	Process a top-level name table entry.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkNameTableElement(RefAST ast)
{
	//	Nothing special to do.
	Vector<StrAnsi> vsta;
	WalkNameIDTree(ast, vsta);
	Assert(vsta.Size() == 0);
}

/*----------------------------------------------------------------------------------------------
	Process a name assignment.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkNameIDTree(RefAST ast, Vector<StrAnsi> & vsta)
{
	RefAST astNextID = ast->getFirstChild();
	vsta.Push(astNextID->getText().c_str());

	if (ast->getType() == OP_EQ || ast->getType() == OP_PLUSEQUAL)
	{
		Symbol psym = SymbolTable()->AddSymbol(GrcStructName(vsta), ksymtNameID,
			LineAndFile(ast));

		RefAST astValue = astNextID->getNextSibling();
		GdlExpression *pexpValue;
		pexpValue = WalkExpressionTree(astValue);
		m_mvlNameStrings->AddItem(psym, pexpValue,
			PointRadius(), PointRadiusUnits(), AttrOverride(), LineAndFile(ast),
			"name assignment");
	}
	else
	{
		Assert(ast->getType() == OP_DOT || ast->getType() == ZdotStruct);
		RefAST astT = astNextID->getNextSibling();
		while (astT)
		{
			WalkNameIDTree(astT, vsta);
			astT = astT->getNextSibling();
		}
	}

	vsta.Pop();
}

/*----------------------------------------------------------------------------------------------
	Process a table that includes rules (substitution, positioning, linebreak).
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkRuleTableTree(RefAST ast, int nodetyp)
{
	GrpLineAndFile lnf = LineAndFile(ast);

	if (nodetyp == LITERAL_substitution)
		PushTableEnv(lnf, "substitution");
	else if (nodetyp == LITERAL_linebreak)
		PushTableEnv(lnf, "linebreak");
	else if (nodetyp == LITERAL_position)
		PushTableEnv(lnf, "positioning");
	else if (nodetyp == LITERAL_positioning)
		PushTableEnv(lnf, "positioning");
	else if (nodetyp == LITERAL_justification)
		PushTableEnv(lnf, "justification");
	else
		Assert(false);

	RefAST astDirectives = ast->getFirstChild()->getNextSibling();
	RefAST astContents = astDirectives;
	if (astDirectives && astDirectives->getType() == Zdirectives)
	{
		WalkDirectivesTree(astDirectives);
		astContents = astDirectives->getNextSibling();
	}

	GdlRuleTable * prultbl = RuleTable(lnf);
	if (nodetyp == LITERAL_substitution || nodetyp == LITERAL_justification)
		prultbl->SetSubstitution(true);
	GdlPass * ppass = prultbl->GetPass(lnf, Pass(), MaxRuleLoop(), MaxBackup());

	while (astContents)
	{
		WalkTableElement(astContents, ktbltRule, prultbl, ppass);
		astContents = astContents->getNextSibling();
	}

	PopEnv(lnf, "table");
}

/*----------------------------------------------------------------------------------------------
	Process a "pass" statement and its contents.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkPassTree(RefAST ast, GdlRuleTable * prultbl, GdlPass * ppassPrev)
{
	RefAST astPassNumber = ast->getFirstChild();
	Assert(astPassNumber->getType() == LIT_INT);
	bool fM;
	int nPassNumber = NumericValue(astPassNumber, &fM);
	if (fM)
		g_errorList.AddError(1152, NULL,
			"Pass number should not be a scaled number",
			LineAndFile(ast));

	GrpLineAndFile lnf = LineAndFile(ast);
	PushPassEnv(lnf, nPassNumber);

	RefAST astDirectives = ast->getFirstChild()->getNextSibling();
	RefAST astContents = astDirectives;
	if (astDirectives && astDirectives->getType() == Zdirectives)
	{
		WalkDirectivesTree(astDirectives);
		astContents = astDirectives->getNextSibling();
	}

	GdlPass * ppass = prultbl->GetPass(lnf, Pass(), MaxRuleLoop(), MaxBackup());

	//	Copy the conditional(s) from the -if- statement currently in effect as a pass-level
	//	constraint.
	for (int ipexp = 0; ipexp < m_vpexpPassConstraints.Size(); ipexp++)
		ppass->AddConstraint(m_vpexpPassConstraints[ipexp]->Clone());

	while (astContents)
	{
		WalkTableElement(astContents, ktbltRule, prultbl, ppass);
		astContents = astContents->getNextSibling();
	}

	PopEnv(lnf, "pass");
}

/*----------------------------------------------------------------------------------------------
	Process an "if" statement.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkIfTree(RefAST ast, GdlRuleTable * prultbl, GdlPass * ppass)
{
	Assert(ast->getType() == ZifStruct);
	int cConds = 0;

	bool fPassConstraint = false; /// AllContentsArePasses(ast);

	//	Only the most immediate pass constraints apply to the embedded passes, so
	//	temporarily remove any that are hanging around at this point.
	Vector<GdlExpression *> vpexpSavePassConstr;
	int ipexp;
	for (ipexp = 0; ipexp < m_vpexpPassConstraints.Size(); ipexp++)
		vpexpSavePassConstr.Push(m_vpexpPassConstraints[ipexp]);
	m_vpexpPassConstraints.Clear();

	Symbol psymNot = SymbolTable()->FindSymbol("!");
	Assert(psymNot);

	RefAST astCond;
	GdlExpression * pexpCond = NULL;

	RefAST astNext = ast->getFirstChild();
	while (astNext)
	{
		if (pexpCond)
		{
			//	The else branch: push the negation of the previous condition on the stack.
			GdlExpression * pexpNot = new GdlUnaryExpression(psymNot, pexpCond);
			if (fPassConstraint)
			{
				m_vpexpPassConstraints.Pop();
				m_vpexpPassConstraints.Push(pexpNot);
			}
			else
			{
				m_vpexpConditionals.Pop();
				m_vpexpConditionals.Push(pexpNot);
			}
		}

		RefAST astContents = astNext->getFirstChild();

		int nodetyp = astNext->getType();
		switch (nodetyp)
		{
		case LITERAL_if:
		case LITERAL_elseif:
		case Zelseif:
			astCond = astContents;
			astContents = astContents->getNextSibling();
			break;
		case LITERAL_else:
			Assert(!astNext->getNextSibling());
			astCond = RefAST(NULL);
			break;
		default:
			Assert(false);
		}

		if (astCond)
		{
			//	Push the current condition on the stack.
			pexpCond = WalkExpressionTree(astCond);
			if (fPassConstraint)
				m_vpexpPassConstraints.Push(pexpCond);
			else
				m_vpexpConditionals.Push(pexpCond);
			cConds++;
		}

		while (astContents)
		{
			WalkTableElement(astContents, ktbltRule, prultbl, ppass);
			astContents = astContents->getNextSibling();
		}

		astNext = astNext->getNextSibling();
	}

	//	Delete all the conditions that were pushed (copies of them have been stored in
	//	the rules or passes themselves).
	for (int i = 0; i < cConds; i++)
	{
		if (fPassConstraint)
		{
			delete *m_vpexpPassConstraints.Top();
			m_vpexpPassConstraints.Pop();
		}
		else
		{
			delete *m_vpexpConditionals.Top();
			m_vpexpConditionals.Pop();
		}
	}

	//	Put the pass constraint list back the way it was.
	Assert(m_vpexpPassConstraints.Size() == 0);
	for (ipexp = 0; ipexp < vpexpSavePassConstr.Size(); ipexp++)
		m_vpexpPassConstraints.Push(vpexpSavePassConstr[ipexp]);
}

/*----------------------------------------------------------------------------------------------
	Return true if all the children of the -if- structure are pass structures. If there is
	a mixture, return false and give a warning.
----------------------------------------------------------------------------------------------*/
bool GrcManager::AllContentsArePasses(RefAST ast)
{
	bool fOne = false;
	bool fAll = true;

	RefAST astNext = ast->getFirstChild();
	while (astNext)
	{
		RefAST astContents = astNext->getFirstChild();
		int nodetyp = astNext->getType();
		switch (nodetyp)
		{
		case LITERAL_if:
		case LITERAL_elseif:
		case Zelseif:
			// First item in conditional.
			astContents = astContents->getNextSibling();
			break;
		case LITERAL_else:
			break;
		default:
			Assert(false);
		}

		while (astContents)
		{
			int nodetyp = astContents->getType();
			if (nodetyp == LITERAL_pass)
				fOne = true;
			else
				fAll = false;
			astContents = astContents->getNextSibling();
		}
		astNext = astNext->getNextSibling();
	}

	if (fOne && !fAll)
	{
		g_errorList.AddWarning(1501, NULL,
			"Not all elements of if statement are passes; test will be applied at rule level.",
			LineAndFile(ast));
	}
	return fAll;
}

/*----------------------------------------------------------------------------------------------
	Process a rule.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkRuleTree(RefAST ast, GdlRuleTable * prultbl, GdlPass * ppass)
{
	GrpLineAndFile lnf = LineAndFile(ast);

	GdlRule * prule = ppass->NewRule(lnf);

	RefAST astLhs = ast->getFirstChild();
	Assert(astLhs);
	if (!astLhs)
		return;
	RefAST astRhs;
	RefAST astContext;
	if (astLhs->getType() == Zrhs)
	{
		astRhs = astLhs;
		astLhs = RefAST(NULL);
	}
	else
	{
		Assert(astLhs->getType() == Zlhs);
		astRhs = astLhs->getNextSibling();
		if (!astRhs || astRhs->getType() == Zcontext)
		{
			//	Substitution rule with no lhs: make what is labeled Zlhs be the rhs.
			astRhs = astLhs;
			astLhs = RefAST(NULL);
		}
		else
		{
			Assert(astRhs);
			Assert(astRhs->getType() == Zrhs);
		}
	}
	astContext = astRhs->getNextSibling();	// may be NULL

	//	Process the context first, then the RHS, then the LHS.

	RefAST astItem;

	int irit = 0;
	if (astContext)
	{
		Assert(astContext->getType() == Zcontext);
		astItem = astContext->getFirstChild();
		while (astItem)
		{
			ProcessItemRange(astItem, prultbl, ppass, prule, &irit, 0, false);
			astItem = astItem->getNextSibling();
		}
	}

	astItem = astRhs->getFirstChild();
	irit = 0;
	while (astItem)
	{
		ProcessItemRange(astItem, prultbl, ppass, prule, &irit, 1, (astLhs != NULL));
		astItem = astItem->getNextSibling();
	}

	if (astLhs)
	{
		astItem = astLhs->getFirstChild();
		irit = 0;
		while (astItem)
		{
			ProcessItemRange(astItem, prultbl, ppass, prule, &irit, 2, false);
			astItem = astItem->getNextSibling();
		}
	}

	//	Copy the conditionals from the -if- statements currently in effect.
	for (int ipexp = 0; ipexp < m_vpexpConditionals.Size(); ipexp++)
	{
		prule->AddConstraint(m_vpexpConditionals[ipexp]->Clone());
	}

	//	Last step:
	prule->InitialChecks();
}

/*----------------------------------------------------------------------------------------------
	Process a rule item or optional range.
----------------------------------------------------------------------------------------------*/
void GrcManager::ProcessItemRange(RefAST astItem, GdlRuleTable * prultbl, GdlPass * ppass,
	GdlRule * prule, int * pirit, int lrc, bool fHasLhs)
{
	if (astItem->getType() == OP_QUESTION)
	{
		//	Optional range.
		int iritStart = *pirit;
		RefAST astSubItem = astItem->getFirstChild();
		while (astSubItem)
		{
			ProcessItemRange(astSubItem, prultbl, ppass, prule, pirit, lrc, fHasLhs);
			astSubItem = astSubItem->getNextSibling();
		}
		prule->AddOptionalRange(iritStart, (*pirit - iritStart), (lrc == 0));
	}
	else if (astItem->getType() == OP_CARET)
	{
		if (prule->ScanAdvance() == -1)
			prule->SetScanAdvance(*pirit);
		else
			g_errorList.AddError(1153, NULL,
				"Cannot have more than one scan advance marker per rule",
				LineAndFile(astItem));
	}
	else
	{
		//	Single item
		ProcessRuleItem(astItem, prultbl, ppass, prule, pirit, lrc, fHasLhs);
		(*pirit)++;
	}
}

/*----------------------------------------------------------------------------------------------
	Process a single rule item.
		lrc			- 0 = context, 1 = rhs, 2 = lhs
		fHasLhs		- if we are in the rhs, and there is in fact a lhs (irrelevant
						if we are not in the rhs)
----------------------------------------------------------------------------------------------*/
void GrcManager::ProcessRuleItem(RefAST astItem, GdlRuleTable * prultbl, GdlPass * ppass,
	GdlRule * prule, int * pirit, int lrc, bool fHasLhs)
{
	GrpLineAndFile lnf = LineAndFile(astItem);

	Assert(astItem->getType() == ZruleItem);

	RefAST astNext = astItem->getFirstChild();

	RefAST astSel = RefAST(NULL);
	RefAST astSelValue = RefAST(NULL);
	RefAST astClass = RefAST(NULL);
	bool fSel = false;
	bool fAssocs = false;
	GdlAlias aliasSel;
	StrAnsi staAlias;
	StrAnsi staClass;
	GdlRuleItem * prit;

	if (astNext->getType() == OP_AT)
	{
		fSel = true;
		staClass = "@";
		astSel = astNext->getNextSibling();

		if (lrc == 2)
		{
			g_errorList.AddError(1154, NULL,
				"@ not permitted in lhs",
				lnf);
			staClass = "_";
		}

		if (astSel && astSel->getType() == Zselector)
		{
			if (lrc == 2)
				g_errorList.AddError(1155, NULL,
					"Cannot specify a selector in the lhs",
					lnf);
			else if (lrc == 1 && !fHasLhs)
				g_errorList.AddError(1156, NULL,
					"Selectors not permitted in a rule with no lhs",
					lnf);
			else
			{
				astSelValue = astSel->getFirstChild();
				Assert(astSelValue);
				ProcessSlotIndicator(astSelValue, &aliasSel);
				astNext = astSel->getNextSibling();
			}
		}
		else
		{
			aliasSel.SetIndex(0);
			astNext = astNext->getNextSibling();
		}
	}
	else
	{
		astClass = astNext;	// class name, glyph spec, _, #
		if (// astClass->getType() == IDENT ||
			astClass->getType() == OP_HASH ||
			astClass->getType() == OP_UNDER)
		{
			staClass = astClass->getText().c_str();
			astNext = astClass->getNextSibling();
		}
		else
		{
			//	Class name, list, or anonymous class
			staClass = ProcessClassList(astClass, &astNext);
		}
	}

	if (astNext && astNext->getType() == Zselector)
	{
		if (lrc == 2)
			g_errorList.AddError(1157, NULL,
				"Cannot specify a selector in the lhs",
				lnf);
		else if (lrc == 1 && !fHasLhs)
			g_errorList.AddError(1158, NULL,
				"Selectors not permitted in a rule with no lhs",
				lnf);
		else
		{
			fSel = true;
			astSelValue = astNext->getFirstChild();
			ProcessSlotIndicator(astSelValue, &aliasSel);
		}
		astNext = astNext->getNextSibling();
	}

	if (astNext && astNext->getType() == Zalias)
	{
		staAlias = astNext->getFirstChild()->getText().c_str();
		astNext = astNext->getNextSibling();
	}

	//	If we are in the RHS and there is a LHS, make substitution items.
	bool fMakeSubItems = ((lrc == 1) && fHasLhs); //// && prultbl->Substitution();

	switch (lrc)
	{
	case 0:	// context
		if (fSel)
		{
			prit = (aliasSel.Index() == -1) ?
				prule->ContextSelectorItemAt(lnf, *pirit, staClass, aliasSel.Name(), staAlias) :
				prule->ContextSelectorItemAt(lnf, *pirit, staClass, aliasSel.Index(), staAlias);
		}
		else
			prit = prule->ContextItemAt(lnf, *pirit, staClass, staAlias);
		break;

	case 1:	// rhs
		if (fSel)
		{
			prit = (aliasSel.Index() == -1) ?
				prule->RhsSelectorItemAt(lnf, *pirit, staClass, aliasSel.Name(), staAlias) :
				prule->RhsSelectorItemAt(lnf, *pirit, staClass, aliasSel.Index(), staAlias);
		}
		else
			prit = prule->RhsItemAt(lnf, *pirit, staClass, staAlias, fMakeSubItems);
		break;

	case 2:	// lhs
		if (fSel)
		{
			prit = (aliasSel.Index() == -1) ?
				prule->LhsSelectorItemAt(lnf, *pirit, staClass, aliasSel.Name(), staAlias) :
				prule->LhsSelectorItemAt(lnf, *pirit, staClass, aliasSel.Index(), staAlias);
		}
		else
			prit = prule->LhsItemAt(lnf, *pirit, staClass, staAlias);
		break;

	default:
		Assert(false);
	}

	if (prit)
	{
		if (astNext && astNext->getType() == Zassocs)
		{
			ProcessAssociations(astNext, prultbl, prit, lrc);
			astNext = astNext->getNextSibling();
		}

		if (astNext)
		{
			if (lrc == 0) // context--constraint
			{
				Assert(astNext->getType() == Zconstraint);
				RefAST astConstraint = astNext->getFirstChild();
				if (astConstraint)
				{
					Assert(!astConstraint->getNextSibling());
					GdlExpression * pexpConstraint = WalkExpressionTree(astConstraint);
					prit->SetConstraint(pexpConstraint);
				}
			}
			else if (lrc == 2)
				g_errorList.AddError(1159, NULL,
					"Cannot set attributes in the lhs",
					lnf);
			else
			{
				//	attributes
				Assert(lrc == 1);
				Assert(astNext->getType() == Zattrs);
				Vector<StrAnsi> vsta;
				RefAST astAttr = astNext->getFirstChild();
				while (astAttr)
				{
					WalkSlotAttrTree(astAttr, prit, vsta);
					Assert(vsta.Size() == 0);
					astAttr = astAttr->getNextSibling();
				}
			}

			Assert(!astNext->getNextSibling());
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Create a new class corresponding to an anonymous class in a rule. Return its name.
----------------------------------------------------------------------------------------------*/
StrAnsi GrcManager::ProcessClassList(RefAST ast, RefAST * pastNext)
{
	if (ast->getType() == IDENT)
	{
		*pastNext = ast->getNextSibling();
		//	If only one class in the list, just return its name.
		if (!*pastNext)
			return ast->getText().c_str();
		int nextNodeTyp = ast->getNextSibling()->getType();
		if (nextNodeTyp != IDENT && nextNodeTyp != LITERAL_glyphid &&
			nextNodeTyp != LITERAL_unicode && nextNodeTyp != LITERAL_codepoint &&
			nextNodeTyp != LITERAL_postscript && nextNodeTyp != LITERAL_pseudo)
		{
			return ast->getText().c_str();
		}
	}

	Symbol psymClass = SymbolTable()->AddAnonymousClassSymbol(LineAndFile(ast));

	StrAnsi staClassName = psymClass->FullName();
	GdlGlyphClassDefn * pglfc = psymClass->GlyphClassDefnData();
	Assert(pglfc);
	pglfc->SetName(staClassName);
	m_prndr->AddGlyphClass(pglfc);

	int nodetyp;
	RefAST astGlyph = ast;
	while (astGlyph &&
		((nodetyp = astGlyph->getType()) == IDENT ||
			nodetyp == LITERAL_glyphid ||
			nodetyp == LITERAL_unicode || nodetyp == LITERAL_codepoint || nodetyp == ZuHex ||
			nodetyp == LITERAL_postscript || nodetyp == LITERAL_pseudo))
	{
		ProcessGlyphClassMember(astGlyph, pglfc, NULL);
		astGlyph = astGlyph->getNextSibling();
	}
	*pastNext = astGlyph;

	return staClassName;
}

/*----------------------------------------------------------------------------------------------
	Create a new class corresponding to an anonymous class in a rule. Return its name.
----------------------------------------------------------------------------------------------*/
StrAnsi GrcManager::ProcessAnonymousClass(RefAST ast, RefAST * pastNext)
{
	Symbol psymClass = SymbolTable()->AddAnonymousClassSymbol(LineAndFile(ast));

	StrAnsi staClassName = psymClass->FullName();
	GdlGlyphClassDefn * pglfc = psymClass->GlyphClassDefnData();
	Assert(pglfc);
	pglfc->SetName(staClassName);
	m_prndr->AddGlyphClass(pglfc);

	//	Note that there might possibly be a chain of glyphs, not just a single one.
	int nodetyp;
	RefAST astGlyph = ast;
	while (astGlyph &&
		((nodetyp = astGlyph->getType()) == LITERAL_glyphid ||
			nodetyp == LITERAL_unicode || nodetyp == LITERAL_codepoint || nodetyp == ZuHex ||
			nodetyp == LITERAL_postscript || nodetyp == LITERAL_pseudo))
	{
		ProcessGlyphClassMember(astGlyph, pglfc, NULL);
		astGlyph = astGlyph->getNextSibling();
	}
	*pastNext = astGlyph;

	return staClassName;
}


/*----------------------------------------------------------------------------------------------
	Process a node that is a slot reference, either an identifer or a number. Fill in the
	given GdlAlias with the result.
----------------------------------------------------------------------------------------------*/
void GrcManager::ProcessSlotIndicator(RefAST ast, GdlAlias * palias)
{
	if (ast->getType() == LIT_INT)
	{
		bool fM;
		int sr = NumericValue(ast, &fM);
		if (fM)
			g_errorList.AddError(1160, NULL,
				"Slot indicators should not be scaled numbers",
				LineAndFile(ast));
		palias->SetIndex(sr);
	}
	else if (ast->getType() == IDENT || ast->getType() == Qalias)
	{
		palias->SetName(ast->getText().c_str());
	}
	else
		Assert(false);
}

/*----------------------------------------------------------------------------------------------
	Process a list of associations.
----------------------------------------------------------------------------------------------*/
void GrcManager::ProcessAssociations(RefAST ast, GdlRuleTable *prultbl, GdlRuleItem * prit,
	int lrc)
{
	GrpLineAndFile lnf = LineAndFile(ast);

	if (lrc == 0)	// context
		g_errorList.AddError(1161, NULL,
			"Cannot set associations in the context",
			lnf);
	else if (lrc == 2)	// lhs
		g_errorList.AddError(1162, NULL,
			"Cannot set associations in the lhs",
			lnf);
	else if (!prultbl->Substitution())
		g_errorList.AddError(1163, NULL,
			"Cannot set associations in the ",
			prultbl->NameSymbol()->FullName(),
			" table",
			lnf);
	else
	{
		GdlSubstitutionItem * pritsub =	dynamic_cast<GdlSubstitutionItem *>(prit);
		if (pritsub)
		{
			RefAST astAssoc = ast->getFirstChild();
			while (astAssoc)
			{
				GdlAlias aliasAssoc;
				ProcessSlotIndicator(astAssoc, &aliasAssoc);
				(aliasAssoc.Index() == -1)?
					pritsub->AddAssociation(lnf, aliasAssoc.Name()) :
					pritsub->AddAssociation(lnf, aliasAssoc.Index());
				astAssoc = astAssoc->getNextSibling();
			}
		}
		else
		{
			g_errorList.AddError(1164, NULL,
				"Cannot set associations in a rule with no lhs",
				lnf);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Traverse the slot attribute assignment tree, adding the assignments to the rule item.
----------------------------------------------------------------------------------------------*/
void GrcManager::WalkSlotAttrTree(RefAST ast, GdlRuleItem * prit, Vector<StrAnsi> & vsta)
{
	if (!ast)
		return;

	RefAST astNextID = ast->getFirstChild();
	vsta.Push(astNextID->getText().c_str());

	int nodetyp = ast->getType();

	if (nodetyp == OP_EQ || nodetyp == OP_PLUSEQUAL || nodetyp == OP_MINUSEQUAL ||
		nodetyp == OP_MULTEQUAL || nodetyp == OP_DIVEQUAL)
	{
		Symbol psymOp = SymbolTable()->FindSymbol(ast->getText().c_str());
		Assert(psymOp);

		Symbol psymSlotAttr = SymbolTable()->FindSlotAttr(GrcStructName(vsta), LineAndFile(ast));
		if (!psymSlotAttr)
			g_errorList.AddError(1165, NULL,
				"Invalid slot attribute: ",
				GrcStructName(vsta).FullString(),
				LineAndFile(ast));
		else
		{
			RefAST astValue = astNextID->getNextSibling();
			if (astValue->getType() == Zfunction)
			{
				ProcessFunction(astValue, vsta, true, prit, psymOp);
			}
			else
			{
				GdlExpression * pexpValue = WalkExpressionTree(astValue);
				GdlAttrValueSpec * pavs = new GdlAttrValueSpec(psymSlotAttr, psymOp, pexpValue);
				prit->AddAttrValueSpec(pavs);
			}
		}
	}
	else
	{
		Assert(ast->getType() == OP_DOT || ast->getType() == ZdotStruct);
		RefAST astT = astNextID->getNextSibling();
		while (astT)
		{
			WalkSlotAttrTree(astT, prit, vsta);
			astT = astT->getNextSibling();
		}
	}

	vsta.Pop();
}

/*----------------------------------------------------------------------------------------------
	Process an expression.
----------------------------------------------------------------------------------------------*/
GdlExpression * GrcManager::WalkExpressionTree(RefAST ast)
{
	GdlExpression * pexpRet;
	GdlExpression * pexp1;
	GdlExpression * pexp2;
	GdlExpression * pexpCond;
	GdlExpression * pexpSel;
	GdlSlotRefExpression * pexpsrSel;
	Symbol psymOp;
	Symbol psymName;

	RefAST astOperand1;
	RefAST astOperand2;
	RefAST astOperand3;
	RefAST astT;
	RefAST astCond;
	RefAST astSel;
	RefAST astName;
	RefAST astCluster;
	RefAST astText;
	RefAST astCodePage;

	int nValue;
	int nCluster;
	bool fM;
	Vector<StrAnsi> vsta;

	int nodetyp = ast->getType();
	switch (nodetyp)
	{
	case OP_EQ:
		g_errorList.AddError(1166, NULL,
			"'=' cannot be used in an expression (use '==')",
			LineAndFile(ast));
		// fall through and treat as if it were ==
	case OP_EQUALEQUAL:
	case OP_NE:
	case OP_GE:
	case OP_GT:
	case OP_LT:
	case OP_LE:
	case OP_PLUS:
	case OP_MULT:
	case OP_DIV:
	case OP_AND:
	case OP_OR:
		if (nodetyp == OP_EQ)
			psymOp = SymbolTable()->FindSymbol("==");
		else
			psymOp = SymbolTable()->FindSymbol(ast->getText().c_str());
		Assert(psymOp);

		astOperand1 = ast->getFirstChild();
		Assert(astOperand1);
		astOperand2 = astOperand1->getNextSibling();
		Assert(astOperand2);
		Assert(!astOperand2->getNextSibling());

		pexp1 = WalkExpressionTree(astOperand1);
		pexp2 = WalkExpressionTree(astOperand2);
		pexpRet = new GdlBinaryExpression(psymOp, pexp1, pexp2);
		break;

	case LITERAL_min:
	case LITERAL_max:
		psymOp = SymbolTable()->FindSymbol(ast->getText().c_str());
		Assert(psymOp);

		astOperand1 = ast->getFirstChild();
		if (!astOperand1)
		{
			g_errorList.AddError(1167, NULL,
				"No arguments for ",
				ast->getText().c_str(),
				" function",
				LineAndFile(ast));
			return NULL;
		}
		pexp1 = WalkExpressionTree(astOperand1);
		astOperand2 = astOperand1->getNextSibling();
		while (astOperand2)
		{
			pexp2 = WalkExpressionTree(astOperand2);
			pexp1 = new GdlBinaryExpression(psymOp, pexp1, pexp2);
			astOperand2 = astOperand2->getNextSibling();
		}
		pexpRet = pexp1;
		break;

	case OP_MINUS:
		psymOp = SymbolTable()->FindSymbol(ast->getText().c_str());
		Assert(psymOp);

		astOperand1 = ast->getFirstChild();
		Assert(astOperand1);
		astOperand2 = astOperand1->getNextSibling();
		if (astOperand2)
		{
			Assert(!astOperand2->getNextSibling());

			pexp1 = WalkExpressionTree(astOperand1);
			pexp2 = WalkExpressionTree(astOperand2);
			pexpRet = new GdlBinaryExpression(psymOp, pexp1, pexp2);
			break;
		}
		else
		{	// fall through
		}

	case OP_NOT:
		psymOp = SymbolTable()->FindSymbol(ast->getText().c_str());
		Assert(psymOp);

		astOperand1 = ast->getFirstChild();
		Assert(astOperand1);
		Assert(!astOperand1->getNextSibling());

		pexp1 = WalkExpressionTree(astOperand1);
		pexpRet = new GdlUnaryExpression(psymOp, pexp1);
		break;

	case OP_QUESTION:
		astCond = ast->getFirstChild();
		Assert(astCond);
		astOperand1 = astCond->getNextSibling();
		Assert(astOperand1);
		Assert(astOperand1->getNextSibling()->getType() == OP_COLON);
		astOperand2 = astOperand1->getNextSibling()->getNextSibling();
		Assert(astOperand2);

		pexpCond = WalkExpressionTree(astCond);
		pexp1 = WalkExpressionTree(astOperand1);
		pexp2 = WalkExpressionTree(astOperand2);
		pexpRet = new GdlCondExpression(pexpCond, pexp1, pexp2);
		break;

	case Zlookup:
		astT = ast->getFirstChild();
		astSel = nullAST;
		pexpSel = NULL;
		if (astT->getType() == OP_AT)
		{
			astSel = astT;
			astT = astT->getNextSibling();
			pexpSel = WalkExpressionTree(astSel);
			pexpsrSel = dynamic_cast<GdlSlotRefExpression *>(pexpSel);
			Assert(pexpsrSel);
		}
		astName = astT;
		vsta.Clear();
		psymName = IdentifierSymbol(astName, vsta);
		Assert(psymName);
//		GdlLookupExpression::LookupType lookType;
//		if (psymName->FitsSymbolType(ksymtFeature))
//			lookType = GdlLookupExpression::klookFeature;
//		else if (psymName->FitsSymbolType(ksymtGlyphAttr))
//			lookType = GdlLookupExpression::klookGlyph;
//		else if (psymName->FitsSymbolType(ksymtGlyphMetric))
//			lookType = GdlLookupExpression::klookGlyph;
//		else if (psymName->FitsSymbolType(ksymtSlotAttr))
//			lookType = GdlLookupExpression::klookSlot;
//		else
//			lookType = GdlLookupExpression::klookUnknown;

		astCluster = astName->getNextSibling();
		nCluster = 0;	// default
		if (astCluster)
		{
			Assert(astCluster->getType() == Zcluster);
			Assert(astCluster->getFirstChild());
			nCluster = NumericValue(astCluster->getFirstChild(), &fM);
			Assert(!fM);
		}

		if (pexpSel)
			pexpRet = new GdlLookupExpression(psymName, pexpsrSel, nCluster);
		else
			pexpRet = new GdlLookupExpression(psymName, -1, nCluster);

		break;

	case OP_AT:
		astT = ast->getFirstChild();
		Assert(!astT->getNextSibling());

		if (astT->getType() == IDENT || astT->getType() == Qalias)
			pexpRet = new GdlSlotRefExpression(astT->getText().c_str());
		else if (astT->getType() == LITERAL_true || astT->getType() == LITERAL_false)
		{
			Assert(false);	// grammar should not allow this
			g_errorList.AddError(1168, NULL,
				"Slot reference cannot contain a boolean",
				LineAndFile(astT));
			pexpRet = new GdlSlotRefExpression(ast->getType() == LITERAL_true);
		}
		else
		{
			if (astT->getType() == OP_MINUS)
			{
				Assert(false);	// grammar should not allow this
				nValue = NumericValue(astT->getFirstChild(), &fM);
				nValue *= -1;
			}
			else
				nValue = NumericValue(astT, &fM);
			if (fM)
			{
				Assert(false);	// grammar should not allow this
				g_errorList.AddError(1169, NULL,
					"Slot reference cannot contain a scaled value",
					LineAndFile(astT));
			}
			pexpRet = new GdlSlotRefExpression(nValue);
		}
		break;

	case LIT_INT:
		nValue = NumericValue(ast, &fM);
		if (fM)
			pexpRet = new GdlNumericExpression(nValue, MUnits());
		else
			pexpRet = new GdlNumericExpression(nValue);
		break;

	case LITERAL_true:
		pexpRet = new GdlNumericExpression(1);
		break;

	case LITERAL_false:
		pexpRet = new GdlNumericExpression(0);
		break;

	case LIT_STRING:
		pexpRet = new GdlStringExpression(ast->getText().c_str(), CodePage());
		break;

	case LITERAL_string:
		// string function
		astText = ast->getFirstChild();
		astCodePage = astText->getNextSibling();
		if (astCodePage)
		{
			Assert(astCodePage->getType() == LIT_INT);
			nValue = NumericValue(astCodePage, &fM);
			pexpRet = new GdlStringExpression(astText->getText().c_str(), nValue);
		}
		else
			pexpRet = new GdlStringExpression(astText->getText().c_str(), CodePage());
		break;

	default:
		Assert(false);
	}

	pexpRet->SetLineAndFile(LineAndFile(ast));

	return pexpRet;

}	// end of WalkExpressionTree


/*----------------------------------------------------------------------------------------------
	Return the line information the tree node is associated with.
----------------------------------------------------------------------------------------------*/
GrpLineAndFile GrcManager::LineAndFile(RefAST ast)
{
	GrpASTNode * wrNode = dynamic_cast<GrpASTNode *>(ast->getNode());
	Assert(wrNode);
	if (!wrNode)
		return GrpLineAndFile(0, 0, "");
	GrpLineAndFile lnf = wrNode->LineAndFile();
	if (!lnf.NotSet())
		return lnf;

	if (ast->getFirstChild())
		return LineAndFile(ast->getFirstChild());

	if (ast->getNextSibling())
		return LineAndFile(ast->getNextSibling());

	return GrpLineAndFile(0, 0, "");
}


/*----------------------------------------------------------------------------------------------
	Return the numeric value that is the equivalent of the text in the node. Return a
	flag indicating if there is an "m" on the end. The following are legal strings:
		123
		0x89AB
		0x89abcde
		123m
		123M
		U+89ab
	Return 0 if the value is not a legal numeric string.
----------------------------------------------------------------------------------------------*/
int GrcManager::NumericValue(RefAST ast, bool * pfM)
{
	Assert(ast->getType() == LIT_INT || ast->getType() == LIT_UHEX);

	std::string str = ast->getText();

	int nRet = 0;

	unsigned int ich = 0;
	int nBase = 10;

	if (ast->getType() == LIT_UHEX)
	{
		if (str[0] == 'U' && str[1] == '+')
			ich = 2;
		nBase = 16;
	}
	else if (str[0] == '0' && str[1] == 'x')
	{
		ich = 2;
		nBase = 16;
	}
	else if (str[0] == 'x')
	{
		ich = 1;
		nBase = 16;
	}

	while (ich < str.length())
	{
		if (str[ich] >= 0 && str[ich] <= '9')
			nRet = nRet * nBase + (str[ich] - '0');
		else if (nBase == 16 && str[ich] >= 'A' && str[ich] <= 'F')
			nRet = nRet * nBase + (str[ich] - 'A' + 10);
		else if (nBase == 16 && str[ich] >= 'a' && str[ich] <= 'f')
			nRet = nRet * nBase + (str[ich] - 'a' + 10);
		else
			break;
		ich++;
	}

	*pfM = (str[ich] == 'm' || str[ich] == 'M');

	Assert(!*pfM || ast->getType() != LIT_UHEX);

	return nRet;
}

int GrcManager::NumericValue(RefAST ast)
{
	bool fM;
	int nRet = NumericValue(ast, &fM);
	Assert(!fM);
	return nRet;
}

/*----------------------------------------------------------------------------------------------
	Return the symbol corresponding to the dotted identifier.
----------------------------------------------------------------------------------------------*/
Symbol GrcManager::IdentifierSymbol(RefAST ast, Vector<StrAnsi> & vsta)
{
	if (ast->getType() == OP_DOT)
	{
		RefAST ast1 = ast->getFirstChild();
		vsta.Push(ast1->getText().c_str());
		return IdentifierSymbol(ast1->getNextSibling(), vsta);
	}

	Assert(ast->getType() == IDENT);
	vsta.Push(ast->getText().c_str());
	Symbol psymRet = SymbolTable()->FindSymbol(GrcStructName(vsta));
	if (!psymRet)
	{
		//	If the symbol is of the form <class>.<predefined-glyph-attr>, add the symbol
		//	as a glyph attribute.
		ExpressionType expt;
		SymbolType symt;
		GrcStructName xns(vsta);
		if (ClassPredefGlyphAttr(vsta, &expt, &symt))
			psymRet = SymbolTable()->AddGlyphAttrSymbol(xns, LineAndFile(ast), expt,
				(symt == ksymtGlyphMetric));
		else
			psymRet = SymbolTable()->AddSymbol(xns, ksymtInvalid, LineAndFile(ast));
	}
	return psymRet;
}

/*----------------------------------------------------------------------------------------------
	Return true if the given array of symbol names is of the form
	<class>.<predefined-glyph-attr>.
----------------------------------------------------------------------------------------------*/
bool GrcManager::ClassPredefGlyphAttr(Vector<StrAnsi> & vsta,
	ExpressionType * pexpt, SymbolType * psymt)
{
	StrAnsi sta1 = vsta[0];
	Symbol psymClass = SymbolTable()->FindSymbol(sta1);
	if (!psymClass || !psymClass->FitsSymbolType(ksymtClass))
		return false;

	Vector<StrAnsi> vstaMinusClass;
	int ista;
	for (ista = 1; ista < vsta.Size(); ista++)
		vstaMinusClass.Push(vsta[ista]);
	Symbol psymGlyphAttr = SymbolTable()->FindSymbol(GrcStructName(vstaMinusClass));
	if (!psymGlyphAttr)
		return false;

	if (psymGlyphAttr->FitsSymbolType(ksymtGlyphAttr) && !psymGlyphAttr->IsUserDefined())
	{
		*pexpt = psymGlyphAttr->ExpType();
		*psymt = ksymtGlyphAttr;
		return true;
	}
	else if (psymGlyphAttr->FitsSymbolType(ksymtGlyphMetric))
	{
		*pexpt = psymGlyphAttr->ExpType();
		*psymt = ksymtGlyphMetric;
		return true;
	}

	return false;
}


/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Perform initial checking and clean-up on a newly added rule.
----------------------------------------------------------------------------------------------*/
void GdlRule::InitialChecks()
{
	CheckInputClass();
	ConvertLhsOptRangesToContext();
}


/*----------------------------------------------------------------------------------------------
	Check that there are no unused underscores in the context or rhs, or that an item didn't
	show up as an underscore in both the lhs and rhs. In other words, check that all items
	have either an input class or an output class.
----------------------------------------------------------------------------------------------*/
void GdlRule::CheckInputClass()
{
	for (int iritT = 0; iritT < m_vprit.Size(); iritT++)
	{
		Symbol psymInput = m_vprit[iritT]->m_psymInput;

		if (!psymInput)
		{
			g_errorList.AddError(1170, this,
				"Mismatched items between lhs and rhs");
			psymInput = g_cman.SymbolTable()->FindSymbol("_");
			m_vprit[iritT]->m_psymInput = psymInput;
		}

		if (!psymInput->FitsSymbolType(ksymtClass) &&
			!psymInput->FitsSymbolType(ksymtSpecialLb))
		{
			//	The only other thing it can be:
			Assert(psymInput->FitsSymbolType(ksymtSpecialUnderscore));

			GdlSubstitutionItem * pritsub = dynamic_cast<GdlSubstitutionItem*>(m_vprit[iritT]);
			if (pritsub)
			{
				Symbol psymOutput = pritsub->OutputSymbol();
				if (!psymOutput ||
					(!psymOutput->FitsSymbolType(ksymtClass) &&
						(!psymOutput->FitsSymbolType(ksymtSpecialAt))))
					g_errorList.AddError(1171, this,
						"Item ",
						pritsub->PosString(),
						": no input or output class specified");
			}
			else
				g_errorList.AddError(1172, this,
					"Mismatched items among context, lhs and/or rhs");
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Convert the optional ranges that are relative to the lhs to be relative to the
	context.

	If there is a conflict between the lhs ranges and the context ranges, record an
	error and clear the optional ranges.

	Note that Lhs and Rhs items are always GdlSetAttrItems or GdlSubstitutionItems.
----------------------------------------------------------------------------------------------*/
void GdlRule::ConvertLhsOptRangesToContext()
{
	Assert(m_viritOptRangeStart.Size() == m_viritOptRangeEnd.Size());
	Assert(m_viritOptRangeStart.Size() == m_vfOptRangeContext.Size());

	if (m_viritOptRangeStart.Size() == 0)
		return;

	//	Make a mapping from lhs/rhs items to corresponding indices in the context.
	Vector<int> viritToContext;
	for (int iritT = 0; iritT < m_vprit.Size(); iritT++)
	{
		GdlSetAttrItem * pritset = dynamic_cast<GdlSetAttrItem*>(m_vprit[iritT]);
		if (pritset)
		{
			//	Left-hand-side item.
			viritToContext.Push(iritT);
		}
	}

	//	Check that there is no overlap or interference between lhs ranges and
	//	context ranges.

	int iirit;
	for (iirit = 0; iirit < m_viritOptRangeStart.Size(); iirit++)
	{
		if (!m_vfOptRangeContext[iirit])
		{
			int iiritLhs = iirit;
			int iritLhs1 = m_viritOptRangeStart[iiritLhs];
			int iritLhs2 = m_viritOptRangeEnd[iiritLhs];
			if (iritLhs1 < viritToContext.Size() && iritLhs2 < viritToContext.Size())
			{
				iritLhs1 = viritToContext[iritLhs1];
				iritLhs2 = viritToContext[iritLhs2];

				for (int iiritC = 0; iiritC < m_viritOptRangeStart.Size(); iiritC++)
				{
					if (m_vfOptRangeContext[iiritC])
					{
						int iritC1 = m_viritOptRangeStart[iiritC];
						int iritC2 = m_viritOptRangeEnd[iiritC];
						if ((iritLhs1 >= iritC1 && iritLhs1 <= iritC2) ||
							(iritLhs2 >= iritC1 && iritLhs2 <= iritC2))
						{
							g_errorList.AddError(1127, this,
								"Conflict between optional ranges in lhs and context");
							m_viritOptRangeStart.Clear();
							m_viritOptRangeEnd.Clear();
							m_vfOptRangeContext.Clear();
							return;
						}
					}
				}
			}
			else
			{
				//	Error condition previous handled--ignore this optional range.
				m_vfOptRangeContext.Delete(iirit);
				m_viritOptRangeStart.Delete(iirit);
				m_viritOptRangeEnd.Delete(iirit);
				--iirit;
			}
		}
	}

	//	Now do the conversion.

	for (iirit = 0; iirit < m_viritOptRangeStart.Size(); iirit++)
	{
		if (!m_vfOptRangeContext[iirit])
		{
			int irit1 = m_viritOptRangeStart[iirit];
			int irit2 = m_viritOptRangeEnd[iirit];
			Assert(irit1 < viritToContext.Size());
			Assert(irit2 < viritToContext.Size());

			m_viritOptRangeStart[iirit] = viritToContext[irit1];
			m_viritOptRangeEnd[iirit] = viritToContext[irit2];
			//	Just to keep things tidy:
			m_vfOptRangeContext[iirit] = true;
		}
	}
}


/***********************************************************************************************
	Debuggers
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Write a text version of the parse tree to a file.
----------------------------------------------------------------------------------------------*/
void GrcManager::DebugParseTree(RefAST ast)
{
	std::ofstream strmOut;
	strmOut.open("dbg_parsetree.txt");
	if (strmOut.fail())
	{
		g_errorList.AddError(6106, NULL,
			"Error in writing to file ",
			"dbg_parsetree.txt");
		return;
	}

	strmOut << "PARSE TREE\n\n";

	ast->Trace(strmOut, 0, 0);

	strmOut.close();
}
