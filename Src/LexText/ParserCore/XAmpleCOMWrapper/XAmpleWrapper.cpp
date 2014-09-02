/*:Ignore---------------------------------------------------------------------------------------
// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

File: XAmpleWrapper.cpp
Responsibility: John Hatton
Last reviewed: never

Description:
	Provides a COM wrapper around the C++ wrapper, which in turn wraps the simple XAmple DLL.

	The custom-build process for this DLL includes the creation of a .Net interop assembly.
----------------------------------------------------------------------------------------------*/
//:End Ignore

#include "stdafx.h"
#include "XAmpleWrapper.h"
#pragma comment(lib, "comsuppw.lib")
#include "comutil.h"

inline int BstrLen(BSTR bstr)
{
	if (!bstr)
		return 0;
	return ((int *)bstr)[-1] / sizeof(wchar_t);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP CXAmpleWrapper::Init(BSTR bstrFolderContainingXampleDll)
{
	m_pXample = new CXAmpleDLLWrapper();
	_bstr_t path = bstrFolderContainingXampleDll;
	m_pXample->Init(path);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP CXAmpleWrapper::ParseWord(BSTR bstrWordform, /*out*/ BSTR* pbstrXmlResult)
{
	const int kBaseWordBufsize = 1000;
	const int kBaseResultBufsize = 20000;
	char szBuffer[kBaseWordBufsize];
	wchar_t wszBuffer[kBaseResultBufsize];
	wchar_t *pszBuffer = wszBuffer;
	size_t iParseResultBufSize = kBaseResultBufsize;

	int iResult = WideCharToMultiByte(CP_UTF8, 0, bstrWordform, -1, szBuffer, kBaseWordBufsize,
		NULL, NULL);
	const char * result = m_pXample->ParseString(szBuffer);
	if ((strlen(result) + 1) >= iParseResultBufSize)
	{
		// it won't fit, so increase the buffer size
		iParseResultBufSize = strlen(result) + 100; // add a little padding
		pszBuffer = new wchar_t[iParseResultBufSize];
	}
	iResult = MultiByteToWideChar(CP_UTF8, 0, result, -1, pszBuffer, (int)iParseResultBufSize);

	*pbstrXmlResult = ::SysAllocStringLen(pszBuffer, iResult-1);
	if (iParseResultBufSize != kBaseResultBufsize)
		delete pszBuffer; // we've already expanded it; avoid memory leak
	return S_OK;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP CXAmpleWrapper::TraceWord(BSTR bstrWordform, BSTR bstrSelectedMorphs, /*out*/ BSTR* pbstrXmlResult)
{
	char szBuffer[1000];
	char szBuffer2[1000];

	int iResult = WideCharToMultiByte(CP_UTF8, 0, bstrWordform, -1, szBuffer, 1000, NULL, NULL);
	iResult = WideCharToMultiByte(CP_UTF8, 0, bstrSelectedMorphs, -1, szBuffer2, 1000, NULL, NULL);
	if (bstrSelectedMorphs == NULL)
	{ // force a space; XAmple will treat this properly as nothing
		szBuffer2[0] = ' ';
		szBuffer2[1] = '\0';
	}
	const char * result = m_pXample->TraceString(szBuffer, szBuffer2);
	iResult = MultiByteToWideChar(CP_UTF8, 0, result, -1, NULL, 0) - 1; // don't want NUL at end.
	*pbstrXmlResult = ::SysAllocStringLen(NULL, iResult);
	iResult = MultiByteToWideChar(CP_UTF8, 0, result, -1, *pbstrXmlResult, iResult);
	return S_OK;
}

//STDMETHODIMP CXAmpleWrapper::GetErrorString(/*out*/ BSTR* pbstrResult)
//{
//	*pbstrResult =
//
//	return S_OK;
//}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP CXAmpleWrapper::LoadFiles(BSTR bstrFixedFilesDir, BSTR bstrDynamicFilesDir,
	BSTR bstrDatabaseName)
{
	m_pXample->LoadFiles(_bstr_t(bstrFixedFilesDir), _bstr_t(bstrDynamicFilesDir),
		_bstr_t(bstrDatabaseName));
	return S_OK;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP CXAmpleWrapper::SetParameter(BSTR name, BSTR value)
{
	m_pXample->SetParameter(_bstr_t(name), _bstr_t(value));
	return S_OK;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP CXAmpleWrapper::get_AmpleThreadId(int * pid)
{
	if (pid == NULL)
		return E_POINTER;
	if (m_pXample == NULL)
		return E_UNEXPECTED;
	*pid = m_pXample->AmpleThreadId();
	return S_OK;
}
