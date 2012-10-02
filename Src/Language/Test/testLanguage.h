/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testLanguage.h
Responsibility:
Last reviewed:

	Global header for unit testing the Language DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLANGUAGE_H_INCLUDED
#define TESTLANGUAGE_H_INCLUDED

#pragma once
#include "Main.h"
#include <unit++.h>

namespace TestLanguage
{
	const int kwsEng = 345;	// Arbitrary random value.
	const int kwsFrn = 123;
	const int kwsTest = 1; // arbitrary alternative to identify which ML name we want.
	const int kwsTest2 = 2; // Another arbitrary alternative ws code.
	const wchar kszEng[] = L"en";
	const wchar kszFrn[] = L"fr";
	const wchar kszTest[] = L"test";
	const wchar kszTest2[] = L"tst2";

	HRESULT CreateTestWritingSystem(ILgWritingSystemFactory * pwsf, int ws,
		const wchar * pszWs);
	void CreateTestWritingSystemFactory(ILgWritingSystemFactory ** ppwsf);
	void CheckInstalledLanguages();
	bool CreateBackupFile(const wchar * pszFile);
	void RestoreInstalledLanguages();
	bool RestoreFromBackupFile(const wchar * pszFile);
}


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*TESTLANGUAGE_H_INCLUDED*/
