/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: UtilSil.h
Responsibility:
Last reviewed:

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef UTILSIL_H
#define UTILSIL_H 1


/***********************************************************************************************
	The SilUtil namespace provides a place for SIL specific utility functions to live without
	having to be global functions.
***********************************************************************************************/
namespace SilUtil
{
	const wchar * LocalServerName();
	const wchar * ConvertEthnologueToISO(const wchar * pszEthCode);

	bool ExecCmd(LPCOLESTR pszCmd, bool fInvisible, bool fWaitTillExit, DWORD * pdwExitCode);

	bool CompareTimesWithinXXSeconds(SYSTEMTIME stA, SYSTEMTIME stB, int XXseconds);
	bool IsAdminUser();
	bool IsPowerUser();

	const wchar * PathCombine(const wchar * pszRootDir, const wchar * pszFile);
	bool IsPathRooted(const wchar * pszPath);
	bool FileExists(const wchar * pszPath);

	const Normalizer2* GetIcuNormalizer(UNormalizationMode mode);

}; //:> end namespace SilUtil

#endif /*UTILSIL_H*/
