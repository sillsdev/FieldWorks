/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

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

	bool ExecCmd(LPCTSTR pszCmd, bool fInvisible, bool fWaitTillExit, DWORD * pdwExitCode);

	bool CompareTimesWithinXXSeconds(SYSTEMTIME stA, SYSTEMTIME stB, int XXseconds);
	bool IsAdminUser();
	bool IsPowerUser();

	const wchar * PathCombine(const wchar * pszRootDir, const wchar * pszFile);
	bool IsPathRooted(const wchar * pszPath);
	bool FileExists(const wchar * pszPath);

}; //:> end namespace SilUtil

#endif /*UTILSIL_H*/
