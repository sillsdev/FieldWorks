/*:Ignore----------------------------------------------------------------------------------------------
// Copyright (c) 2001-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

File: XAmpleDLLWrapper.cpp
Responsibility: John Hatton
Last reviewed: never

Description:
	This class wraps the Ample DLL calls so Ample looks like and object.
----------------------------------------------------------------------------------------------*/
//:End Ignore

// CXAmpleDLLWrapper.cpp: implementation of the CXAmpleDLLWrapper class.

#include "stdafx.h"
#include "XAmpleWrapperCore.h"
#include <fstream>
using namespace std;


#ifdef _DEBUG
#undef pProcess_FILE
static char pProcess_FILE[]=__FILE__;
//#define new DEBUG_NEW
#endif

#define AMPLE_DLL_NAME "XAMPLE.DLL"

#define CHECK(_b) if (false == (bool)_b) {char sErr[1000]; sprintf(sErr, "Failed Check in file %s on line %d.",__FILE__,__LINE__); throw sErr;}
#define CHECKPtr(_b) CHECK((_b != NULL))

CAmpleOptions::CAmpleOptions()
:	m_bCheckMorphnames(	0),
	m_iMaxMorphnameLength(	40),
	m_iMaxAnalysesToReturn(20),
	m_bOutputRootGlosses( 0),
	m_bPrintTestParseTrees(	0),
	m_bReportAmbiguityPercentages( 1),
	m_bWriteDecompField(1),
	m_bWritePField(1),
	m_bWriteWordField(1),
	m_bTrace (FALSE),	// jdh 8/27/99 was defaulting to trace on
	m_sOutputStyle ("FWParse") // jdh june 13 2000 add switch to get xml output from parseFile
{
}

CAmpleOptions::~CAmpleOptions()
{
}

// Makes sure the dll is ready to use.  Does not load any data in.
// EXCEPTIONS: CProcessFailure

CXAmpleDLLWrapper::CXAmpleDLLWrapper()//const CCarlaLanguage* pLang)
:	m_pOptions(new CAmpleOptions),
	m_cComment('|'),
	m_hAmpleLib(NULL),
	m_pSetup(NULL),
	m_pfAmpleCreateSetup(NULL),
	m_pfAmpleDeleteSetup(NULL),
	m_pfAmpleLoadControlFiles(NULL),
	m_pfAmpleLoadDictionary(NULL),
	m_pfAmpleLoadGrammarFile(NULL),
	m_pfAmpleParseFile(NULL),
	m_pfAmpleParseText(NULL),
	m_pfAmpleGetAllAllomorphs(NULL),
	m_pfAmpleApplyInputChangesToWord(NULL),
	m_pfAmpleSetParameter(NULL),
	m_pfAmpleAddSelectiveAnalysisMorphs(NULL),
	m_pfAmpleRemoveSelectiveAnalysisMorphs(NULL),
	m_pfAmpleReportVersion(NULL),
	m_pfAmpleReset(NULL),
	m_pfAmpleInitializeMorphChecking(NULL),	/* hab 1999.06.25 */
	m_pfAmpleCheckMorphReferences(NULL),	/* hab 1999.06.25 */
	m_pfAmpleInitializeTrace(NULL),
	m_pfAmpleGetTrace(NULL),
	m_bLastRunHadErrors(FALSE)
{
}

CXAmpleDLLWrapper::~CXAmpleDLLWrapper()
{
	if (m_hAmpleLib != 0)
	{
		if(m_pSetup)
			RemoveSetup();
		FreeLibrary(m_hAmpleLib);	// decrements the reference counter and frees if zero
		m_hAmpleLib = NULL;
	}
	if (m_pOptions != 0)
	{
		delete m_pOptions;
		m_pOptions = NULL;
	}
}

// Exceptions: char*
void CXAmpleDLLWrapper::Init(const char* lpszFolderContainingXAmpleDll)
{
	CHECK(!m_hAmpleLib); // Shouldn't call Init() more than once.

	try
	{
		char sPath[MAX_PATH+1];
		strcpy(sPath, lpszFolderContainingXAmpleDll);
		strcat(sPath, "\\");
		strcat(sPath, AMPLE_DLL_NAME);
		m_hAmpleLib = LoadLibrary(sPath);
		// REVIEW JohnH(RandyR): This blocked line caused a compile warning,
		// which will cause the entire build to fail, when it gets done by NAnt.
		// If it can't find the DLL, then m_hAmpleLib is NULL,`
		// according to the docs on LoadLibrary, and according to actual testing,
		// so I just tested for it being NULL.
		//if ((unsigned long)m_hAmpleLib < 32)
		if (m_hAmpleLib == NULL)
		{
			throw "Couldn't load XAMPLE DLL";
		}

		m_pfAmpleCreateSetup = (SPECAmpleCreateSetup)GetProcAddress(m_hAmpleLib,
													  "AmpleCreateSetup");
		if (m_pfAmpleCreateSetup == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleCreateSetup in Ample DLL");
		}

		m_pfAmpleDeleteSetup = (SPECAmpleDeleteSetup)GetProcAddress(m_hAmpleLib,
													  "AmpleDeleteSetup");
		if (m_pfAmpleDeleteSetup == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleDeleteSetup in Ample DLL");
		}

		m_pfAmpleLoadControlFiles = (SPECAmpleLoadControlFiles)GetProcAddress(m_hAmpleLib,
													  "AmpleLoadControlFiles");
		if (m_pfAmpleLoadControlFiles == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleLoadControlFiles in Ample DLL");
		}

		m_pfAmpleLoadDictionary = (SPECAmpleLoadDictionary)GetProcAddress(m_hAmpleLib,
													  "AmpleLoadDictionary");
		if (m_pfAmpleLoadDictionary == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleLoadDictionary in Ample DLL");
		}

		m_pfAmpleLoadGrammarFile = (SPECAmpleLoadGrammarFile)GetProcAddress(m_hAmpleLib,
													  "AmpleLoadGrammarFile");
		if (m_pfAmpleLoadGrammarFile == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleLoadGrammarFile in Ample DLL");
		}

		m_pfAmpleParseFile = (SPECAmpleParseFile)GetProcAddress(m_hAmpleLib,
													  "AmpleParseFile");
		if (m_pfAmpleParseFile == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleParseFile in Ample DLL");
		}

		m_pfAmpleParseText = (SPECAmpleParseText)GetProcAddress(m_hAmpleLib,
													  "AmpleParseText");
		if (m_pfAmpleParseText == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleParseText in Ample DLL");
		}

		m_pfAmpleGetAllAllomorphs = (SPECAmpleGetAllAllomorphs)GetProcAddress(m_hAmpleLib,
													  "AmpleGetAllAllomorphs");
		if (m_pfAmpleGetAllAllomorphs == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleGetAllAllomorphs in Ample DLL");
		}

		m_pfAmpleApplyInputChangesToWord = (SPECAmpleApplyInputChangesToWord)GetProcAddress(m_hAmpleLib,
													"AmpleApplyInputChangesToWord");
		if (m_pfAmpleApplyInputChangesToWord == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleApplyInputChangesToWord in Ample DLL");
		}

		m_pfAmpleSetParameter = (SPECAmpleSetParameter)GetProcAddress(m_hAmpleLib,
													  "AmpleSetParameter");
		if(m_pfAmpleSetParameter == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleSetParameter in Ample DLL");
		}

		m_pfAmpleAddSelectiveAnalysisMorphs = (SPECAmpleAddSelectiveAnalysisMorphs)GetProcAddress(m_hAmpleLib,
													  "AmpleAddSelectiveAnalysisMorphs");
		if(m_pfAmpleAddSelectiveAnalysisMorphs == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleAddSelectiveAnalysisMorphs in Ample DLL");
		}

		m_pfAmpleRemoveSelectiveAnalysisMorphs = (SPECAmpleRemoveSelectiveAnalysisMorphs)GetProcAddress(m_hAmpleLib,
													  "AmpleRemoveSelectiveAnalysisMorphs");
		if(m_pfAmpleRemoveSelectiveAnalysisMorphs == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleRemoveSelectiveAnalysisMorphs in Ample DLL");
		}

		m_pfAmpleReset = (SPECAmpleReset)GetProcAddress(m_hAmpleLib,
													  "AmpleReset");
		if(m_pfAmpleReset == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleReset in Ample DLL");
		}

		m_pfAmpleReportVersion = (SPECAmpleReportVersion)GetProcAddress(m_hAmpleLib,
													  "AmpleReportVersion");
		if(m_pfAmpleReportVersion == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleReportVersion in Ample DLL");
		}

		m_pfAmpleInitializeTrace = (SPECAmpleReportVersion)GetProcAddress(m_hAmpleLib,
													  "AmpleInitializeTraceString");
		if(m_pfAmpleInitializeTrace == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleInitializeTrace in Ample DLL");
		}

		m_pfAmpleGetTrace = (SPECAmpleReportVersion)GetProcAddress(m_hAmpleLib,
													  "AmpleGetTraceString");
		if(m_pfAmpleGetTrace == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleGetTrace in Ample DLL");
		}

		/* hab 1999.06.25 */
		m_pfAmpleInitializeMorphChecking = (SPECAmpleInitializeMorphChecking)GetProcAddress(m_hAmpleLib,
													  "AmpleInitializeMorphChecking");
		if(m_pfAmpleInitializeMorphChecking == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleInitializeMorphChecking in Ample DLL");
		}

		m_pfAmpleCheckMorphReferences = (SPECAmpleCheckMorphReferences)GetProcAddress(m_hAmpleLib,
													  "AmpleCheckMorphReferences");
		if(m_pfAmpleCheckMorphReferences == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleCheckMorphReferences in Ample DLL");
		}
		/* hab 1999.06.25 */

		m_pfAmpleThreadId = (SPECAmpleThreadId)GetProcAddress(m_hAmpleLib,
													  "AmpleThreadId");
		if(m_pfAmpleThreadId == NULL)
		{
			FreeLibrary(m_hAmpleLib);
			m_hAmpleLib = NULL;
			throw ( "Cannot find AmpleThreadId in Ample DLL");
		}
	}
	// don't catch CStrings. The caller will catch them.  //catch(char* sError)
	catch(LPCTSTR lpszError)
	{
//		char* sMsg;
//		sMsg.Format("There was a problem setting up the Ample32 DLL.  Make sure you have the latest version of the DLL.\r\nThe message was: \r\n%s", lpszError);
//		throw(sMsg);
		throw lpszError;
	}

	// MAKE THE SETUP
	if(m_pSetup)
		RemoveSetup();
	//no no no we don't own it	//	delete m_pSetup;

#ifdef RELEASE_DEBUG
	AfxMessageBox("create setup");
#endif
	m_pSetup = (*m_pfAmpleCreateSetup)();
	if(m_pSetup == NULL)
	{
		m_hAmpleLib = NULL;
		throw ("Could not create and Ample DLL Setup");
	}
	//SetLogFile(lpszLogPath);

	#ifdef RELEASE_DEBUG
	AfxMessageBox("end create");
	#endif
}

// Exceptions: char*
//note: the strings are not making it through to the managed clients.
//after looking through a lot of help files, I just can't figure out how to make it work.
// I'm left with the suspicion that the atl code that is the error arbitration doesn't
// recognize throwing strings as exceptions. However, the only exception class that atl provides
// only takes an HResult.
void CXAmpleDLLWrapper::ThrowIfError(const char* sResult)
{
	// REVIEW JohnH(RandyR):
	// Isn't the none in the XML supposed to have quote marks around it
	// in order to be valid XML?
	// Answer: much of the ample stuff is actually SGML. I don't have the code to check, but
	//chances are I wrote this right to first-time and it's just not good XML.
	if ( (0 != strstr(sResult, "<error"))	//if there is an error tag
		 && (NULL == strstr(sResult, "<error code=none>") )	)// but doesn't say "none"
	{
		char* t = _tempnam("C:\\", "XAmpleError");
		FILE *stream = fopen(t,"w");
		fwrite(sResult, strlen(sResult), 1, stream);
		fclose(stream);
		free(t);
		throw sResult;
	}
}

// Exceptions: char*
void CXAmpleDLLWrapper::RemoveSetup()
{
	if (!m_pSetup)
		return;

	AtlTrace("AmpleDLLWrapper Removing Setup\n");
	const char* sResult;
	if (m_pfAmpleReset != NULL)
	{
		sResult = (*m_pfAmpleReset)(m_pSetup);
		ThrowIfError(sResult);
	}
	if (m_pfAmpleDeleteSetup != NULL)
	{
		sResult = (*m_pfAmpleDeleteSetup)(m_pSetup);
		ThrowIfError(sResult);
	}
	m_pSetup = 0;
}

/*----------------------------------------------------------------------------------------------
	Called before parsing, and before the LoadFiles method.
	Sets the various parameters for XAmple.
  ----------------------------------------------------------------------------------------------*/
void CXAmpleDLLWrapper::SetParameter(const char* lspzName, const char* lspzValue)
{
	if (strcmp(lspzName, "MaxAnalysesToReturn") == 0)
	{
		m_pOptions->m_iMaxAnalysesToReturn = atoi(lspzValue);
	}
}

/*----------------------------------------------------------------------------------------------
	called before parsing, makes sure the dicts and control files are loaded and up to date
  ----------------------------------------------------------------------------------------------*/
void CXAmpleDLLWrapper::LoadFiles(const char* lspzFixedFilesDir, const char* lspzDynamicFilesDir, char* lspzDatabaseName)
{
	CHECKPtr(m_hAmpleLib);
	const char * sResult;

	BOOL bUpdateCtrlFiles = FALSE;
	BOOL bUpdateDicts = FALSE;

	char lpszDict[MAX_PATH+1];
	char lpszGram[MAX_PATH+1];
	char lpszADCtl[MAX_PATH+1];
	char lpszCDTable[MAX_PATH+1];

	sprintf(lpszCDTable, "%s\\cd.tab",lspzFixedFilesDir);

	sprintf(lpszADCtl, "%s\\%sadctl.txt", lspzDynamicFilesDir, lspzDatabaseName);
	sprintf(lpszGram, "%s\\%sgram.txt", lspzDynamicFilesDir, lspzDatabaseName);
	sprintf(lpszDict, "%s\\%slex.txt", lspzDynamicFilesDir, lspzDatabaseName);


	bUpdateCtrlFiles = bUpdateDicts = TRUE;
	(*m_pfAmpleReset)(m_pSetup);
	//SetLogFile(NULL);

	SetOptions();

	// LOAD THE CONTROL FILES
	sResult = (*m_pfAmpleLoadControlFiles)(m_pSetup,
										lpszADCtl,
										lpszCDTable,
									   (LPCTSTR)NULL, // ortho
									   NULL ); // INTX

	ThrowIfError(sResult);

	//LOAD ROOT DICTIONARIES
	sResult = (*m_pfAmpleLoadDictionary)(m_pSetup, lpszDict, "u");
	ThrowIfError(sResult);

	//LOAD GRAMMAR FILE
	sResult = (*m_pfAmpleLoadGrammarFile)(m_pSetup, lpszGram);
	ThrowIfError(sResult);

}

void CXAmpleDLLWrapper::SetOptions()
{
	CHECKPtr(m_pOptions);
	char buffer[20];
	const char* sResult;
	char lpszComment[2];
	sprintf(lpszComment, "%c", m_cComment);
	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "BeginComment", lpszComment);
	ThrowIfError(sResult);
	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "MaxMorphnameLength", itoa(m_pOptions->m_iMaxMorphnameLength, buffer, 10));
	ThrowIfError(sResult);
	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "MaxTrieDepth", "3");
	ThrowIfError(sResult);
	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "RootGlosses", m_pOptions->m_bOutputRootGlosses?"TRUE":"FALSE" );
	ThrowIfError(sResult);
	//char* sResult = (*m_pfAmpleSetParameter)(m_pSetup, "OutputStyle", xxx);// Ana, AResult, Ptext
	//ThrowIfError(sResult);

	sResult = (*m_pfAmpleRemoveSelectiveAnalysisMorphs)(m_pSetup);
	ThrowIfError(sResult);

	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "TraceAnalysis", m_pOptions->m_bTrace?"XML":"OFF");//?"ON":"OFF");
	ThrowIfError(sResult);

	/* hab 1999.06.25 */
	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "CheckMorphReferences", m_pOptions->m_bCheckMorphnames?"TRUE":"FALSE");
	ThrowIfError(sResult);
	/* hab 1999.06.25 */

/*	if(m_pOptions->m_bTrace && m_pOptions->m_sTraceMorphs.GetLength())
	{
		sResult = (*m_pfAmpleAddSelectiveAnalysisMorphs)(m_pSetup, m_pOptions->m_sTraceMorphs);
		ThrowIfError(sResult);
	}
*/
	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "OutputDecomposition", m_pOptions->m_bWriteDecompField?"TRUE":"FALSE" );
	ThrowIfError(sResult);

	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "OutputOriginalWord", m_pOptions->m_bWriteWordField?"TRUE":"FALSE" );
	ThrowIfError(sResult);

	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "OutputProperties", m_pOptions->m_bWritePField?"TRUE":"FALSE" );
	ThrowIfError(sResult);

	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "ShowPercentages", m_pOptions->m_bReportAmbiguityPercentages?"TRUE":"FALSE" );
	ThrowIfError(sResult);

//jdh june 13 2000

	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "OutputStyle", m_pOptions->m_sOutputStyle);
	ThrowIfError(sResult);


	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "AllomorphIds", "TRUE");
	ThrowIfError(sResult);


	//jdh feb 2003
	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "MaxAnalysesToReturn", itoa(m_pOptions->m_iMaxAnalysesToReturn, buffer, 10));
	ThrowIfError(sResult);
	// hab 07 dec 2005
	sResult = (*m_pfAmpleSetParameter)(m_pSetup, "RecognizeOnly", "TRUE");
	ThrowIfError(sResult);
}

/*
void CXAmpleDLLWrapper::SetLogFile(LPCTSTR lpszPath) // = NULL
{
	if(lpszPath)
	{
		// if we tell the dll to set the log to what it already is,
		// then we will loose any errors encountered when the Dict and
		// control files were loaded
		//if(m_lpszLogPath == lpszPath
		//	return;
		//else
			m_lpszLogPath = lpszPath;	// can also be called with NULL to use the path we already have
	}

	if(m_lpszLogPath.IsEmpty())
		return;

	CHECK(m_pfAmpleSetParameter && m_pSetup);
	if(m_pfAmpleSetParameter && m_pSetup)
	{
		AtlTrace ("AmpleDLLWrapper Setting LogFile to %s\n", m_lpszLogPath.getFullPath());
		char* sResult = (*m_pfAmpleSetParameter)(m_pSetup, "LogFile", m_lpszLogPath.getFullPath());
		ThrowIfError(sResult);
	}
}
*/

LPAmpleSetup CXAmpleDLLWrapper::GetSetup()
{
	CHECKPtr(m_pSetup);
	return m_pSetup;
}


void CXAmpleDLLWrapper::CheckLogForErrors()
{
	char* errorStrings[] = {"error", "Error", "Expected", "Undefined", "Cannot", "Empty", "WARNING", "MORPH_CHECK", "in entry:"}; // hab 1999.06.30 added MORPH_CHECK and "in entry:"};
	ifstream fin(m_lpszLogPath);
	if(!fin.is_open())
	{
		throw "Couldn't open ampledll log path";
	}

	#define kCheckSize 256
	char buff[kCheckSize+1];	// search the first kCheckSize characters of the log
	buff[0] = '\0';
	fin.read(buff, kCheckSize);
	m_bLastRunHadErrors = FALSE;
	for(int i = 0; i<10; i++)	// increased 9 to 10
	{
		if(strstr(buff, errorStrings[i]))	//pbBuff isn't zero terminated, I think, which is why I'm doing this slower way
		{
			m_bLastRunHadErrors=TRUE;
			break;
		}
	}
}
const char* CXAmpleDLLWrapper::ParseString(const char* sInput)
{
	const char* lpszResult = (*m_pfAmpleSetParameter)(m_pSetup, "TraceAnalysis", "OFF");//Guarantee that tracing has been turned off
	ThrowIfError(lpszResult);
	lpszResult = (*m_pfAmpleParseText)(GetSetup(), sInput, "n");
	ThrowIfError(lpszResult);
		//CheckLogForErrors();
	return lpszResult;
}

const char* CXAmpleDLLWrapper::TraceString(const char* pszInput, const char* pszSelectedMorphs)
{
#ifdef Orig
	// Initialize
	m_pOptions->m_bTrace = true;
	SetOptions();
	m_pOptions->m_bTrace = false;
#endif
	//Guarantee that tracing has been turned on to XML form
	const char*	lpszResult  = (*m_pfAmpleSetParameter)(m_pSetup, "TraceAnalysis", "XML");
	ThrowIfError(lpszResult);
	// add any selected morphs
	lpszResult = (*m_pfAmpleAddSelectiveAnalysisMorphs)(m_pSetup, pszSelectedMorphs);
	ThrowIfError(lpszResult);
	// Do trace
	(*m_pfAmpleInitializeTrace)(GetSetup());
	lpszResult = (*m_pfAmpleParseText)(GetSetup(), pszInput, "n");
	//don't bother returning the result, just the trace
	ThrowIfError(lpszResult);
	// remove any selected morphs
	lpszResult = (*m_pfAmpleRemoveSelectiveAnalysisMorphs)(m_pSetup);
	ThrowIfError(lpszResult);
	//Guarantee that tracing has been turned off
	lpszResult = (*m_pfAmpleSetParameter)(m_pSetup, "TraceAnalysis", "OFF");
	ThrowIfError(lpszResult);
	return (*m_pfAmpleGetTrace)(GetSetup());
}

/*
 *  get the current thread id for the XAMPLE DLL.
 */
int CXAmpleDLLWrapper::AmpleThreadId()
{
	return (*m_pfAmpleThreadId)();
}
