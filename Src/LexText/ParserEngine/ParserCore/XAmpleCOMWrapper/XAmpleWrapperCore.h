/*:Ignore----------------------------------------------------------------------------------------------
Copyright 2001, SIL International. All rights reserved.

File: XAmpleDLLWrapper.cpp
Responsibility: John Hatton
Last reviewed: never

Description:
	This class wraps the Ample DLL calls so Ample looks like and object.
----------------------------------------------------------------------------------------------*/
//:End Ignore

#if !defined(AFX_AMPLEDLLWRAPPER_H__FF6E4D21_B522_11D2_864F_0080C88B8417__INCLUDED_)
#define AFX_AMPLEDLLWRAPPER_H__FF6E4D21_B522_11D2_864F_0080C88B8417__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
#pragma warning(disable: 4996)

extern "C" {

	typedef void* LPAmpleSetup ;
	typedef LPAmpleSetup (CALLBACK * SPECAmpleCreateSetup) (void);
	typedef const char * (/*CALLBACK*/ * SPECAmpleDeleteSetup)(LPAmpleSetup pSetup_io);

	typedef const char * (/*CALLBACK*/ * SPECAmpleLoadControlFiles)(
						   LPAmpleSetup pSetup_io,
						   const char * pszAnalysisDataFile_in,
							const char * pszDictCodeTable_in,
							const char * pszDictOrthoChangeTable_in,
							const char * pszTextInputControlFile_in);

	typedef const char * (/*CALLBACK*/ * SPECAmpleLoadDictionary)(
											   LPAmpleSetup pSetup_io,
												const char *pszFilePath_in,
												const char* pszDictType);

	typedef const char * (/*CALLBACK*/ * SPECAmpleLoadGrammarFile)(	LPAmpleSetup pSetup_io,
													const char *pszGrammarFile_in);

	typedef const char * (/*CALLBACK*/ * SPECAmpleParseFile)(	LPAmpleSetup pSetup_io,
													const char *pszInFilePath_in,
													const char *pszOutFilePath_in);

	typedef const char * (/*CALLBACK*/ * SPECAmpleParseText)(	LPAmpleSetup pSetup_io,
													const char *pszInputText_in,
													const char *pszUseTextIn);


	typedef const char * (/*CALLBACK*/ * SPECAmpleGetAllAllomorphs)(
						  LPAmpleSetup pSetup_io,
									  const char *pszRestOfWord_in,
						  const char *pszState_in);


	typedef const char * (/*CALLBACK*/ * SPECAmpleApplyInputChangesToWord)(
						  LPAmpleSetup pSetup_io,
									  const char *pszWord_in);


	typedef const char * (/*CALLBACK*/ * SPECAmpleSetParameter)(
							LPAmpleSetup pSetup_io,
							const char * pszName_in,
							const char * pszValue_in);

	typedef const char * (/*CALLBACK*/ * SPECAmpleAddSelectiveAnalysisMorphs)(LPAmpleSetup pSetup_io,
																	const char * pszMorphs_in);
	typedef const char * (/*CALLBACK*/ * SPECAmpleRemoveSelectiveAnalysisMorphs)(LPAmpleSetup pSetup_io);
	typedef const char * (/*CALLBACK*/ * SPECAmpleReset)(LPAmpleSetup pSetup_io);

	typedef const char * (/*CALLBACK*/ * SPECAmpleReportVersion)(	LPAmpleSetup pSetup_io);

  typedef const char * (/*CALLBACK*/ * SPECAmpleInitializeMorphChecking)(	LPAmpleSetup pSetup_io);
  typedef const char * (/*CALLBACK*/ * SPECAmpleCheckMorphReferences)(	LPAmpleSetup pSetup_io);

  typedef const char * (/*CALLBACK*/ * SPECAmpleInitializeTrace)(	LPAmpleSetup pSetup_io);
  typedef const char * (/*CALLBACK*/ * SPECAmpleGetTrace)(	LPAmpleSetup pSetup_io);
  typedef int (/*CALLBACK*/ * SPECAmpleThreadId)();

}

class CAmpleOptions
{
public:
	CAmpleOptions();
	virtual ~CAmpleOptions();

//	CPathDescriptor m_pathXMLOutputPath; 	//jdh 13june2000
	char* m_sOutputStyle;			//jdh 13june2000
	int m_iMaxMorphnameLength;
	int m_iMaxAnalysesToReturn;	//jdh feb2003
	BOOL m_bOutputRootGlosses;
	BOOL m_bReportAmbiguityPercentages;
	BOOL m_bCheckMorphnames;
	BOOL m_bPrintTestParseTrees;
	BOOL m_bWriteDecompField;
	BOOL m_bWritePField;
	BOOL m_bWriteWordField;
	BOOL m_bTrace;
	//char* m_sTraceMorphs;
};

class CXAmpleDLLWrapper
{
public:
	CXAmpleDLLWrapper();
	virtual ~CXAmpleDLLWrapper();

	SPECAmpleParseFile m_pfAmpleParseFile;
	SPECAmpleParseText m_pfAmpleParseText;
	SPECAmpleGetAllAllomorphs m_pfAmpleGetAllAllomorphs;
	SPECAmpleApplyInputChangesToWord m_pfAmpleApplyInputChangesToWord;
	SPECAmpleSetParameter m_pfAmpleSetParameter;
	SPECAmpleAddSelectiveAnalysisMorphs m_pfAmpleAddSelectiveAnalysisMorphs;
	SPECAmpleRemoveSelectiveAnalysisMorphs m_pfAmpleRemoveSelectiveAnalysisMorphs;
	char m_cComment;
	BOOL m_bLastRunHadErrors;

	const char* CXAmpleDLLWrapper::ParseString(const char* sInput);
	const char* CXAmpleDLLWrapper::TraceString(const char* sInput, const char* sSelectedMorphs);
	void Init(const char* lpszFolderContainingXAmpleDll);
	void LoadFiles(const char* lspzFixedFilesDir, const char* lspzDynamicFilesDir, char *lspzDatabaseName);
	void SetParameter(const char* lspzName, const char* lspzValue);
	LPAmpleSetup GetSetup();
	void SetLogFile(LPCTSTR lpszPath = NULL);
	int AmpleThreadId();

protected:
	SPECAmpleReset m_pfAmpleReset;
	SPECAmpleLoadControlFiles m_pfAmpleLoadControlFiles;
	SPECAmpleLoadDictionary	m_pfAmpleLoadDictionary;
	SPECAmpleCreateSetup m_pfAmpleCreateSetup;
	SPECAmpleDeleteSetup m_pfAmpleDeleteSetup;
	SPECAmpleReportVersion m_pfAmpleReportVersion;
	SPECAmpleInitializeMorphChecking m_pfAmpleInitializeMorphChecking;
	SPECAmpleCheckMorphReferences m_pfAmpleCheckMorphReferences;
	SPECAmpleLoadGrammarFile m_pfAmpleLoadGrammarFile;
	SPECAmpleInitializeTrace m_pfAmpleInitializeTrace;
	SPECAmpleGetTrace m_pfAmpleGetTrace;
	SPECAmpleThreadId m_pfAmpleThreadId;
	char m_lpszLogPath[MAX_PATH+1];
	void CheckLogForErrors();
	CAmpleOptions * m_pOptions;
	HMODULE m_hAmpleLib;
	LPAmpleSetup m_pSetup;

	void ThrowIfError(const char* sResult);
	void RemoveSetup();
	void SetOptions();
};

#endif // !defined(AFX_AMPLEDLLWRAPPER_H__FF6E4D21_B522_11D2_864F_0080C88B8417__INCLUDED_)
