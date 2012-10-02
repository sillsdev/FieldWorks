/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testLanguage.cpp
Responsibility:
Last reviewed:

	Global initialization/cleanup for unit testing the Language DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "testLanguage.h"

namespace unitpp
{
	void GlobalSetup(bool verbose)
	{
		ModuleEntry::DllMain(0, DLL_PROCESS_ATTACH);
		CheckHr(::OleInitialize(NULL));
		StrUtil::InitIcuDataDir();	// needed for the normalize routines (ICU)
		TestLanguage::CheckInstalledLanguages();
	}
	void GlobalTeardown()
	{
		TestLanguage::RestoreInstalledLanguages();
		ModuleEntry::DllMain(0, DLL_PROCESS_DETACH);
		::OleUninitialize();
	}
}

namespace TestLanguage
{
	// These are the languages (ICU locales) that result from the language tests.
	const char * rgpszLanguages[] = { "en", "test", "tst2", "en_GB_EURO", "fr__EURO" };
	const int cLanguages = isizeof(rgpszLanguages) / isizeof(const char *);
	Vector<int> viNewLang;	// indexes into rgpszLanguages[] that are not installed.
	Vector<int> viModLang;	// ICU has file, but no xml language definition file.

	// Check which languages are installed so that we can restore things to the original
	// state.  Create any needed backup files.
	void CheckInstalledLanguages()
	{
		StrUni stuRootDir(DirectoryFinder::FwRootDataDir());
		StrUni stuIcuDir(DirectoryFinder::IcuDir());
		StrUni stuFile;
		int i;
		for (i = 0; i < cLanguages; ++i)
		{
			stuFile.Format(L"%s\\Languages\\%S.xml",
				stuRootDir.Chars(), rgpszLanguages[i]);
			WIN32_FIND_DATA wfd;
			HANDLE hFind = ::FindFirstFileW(stuFile.Chars(), &wfd);
			if (hFind == INVALID_HANDLE_VALUE)
			{
				viNewLang.Push(i);
			}
			else
			{
				::FindClose(hFind);
			}
		}
		if (viNewLang.Size())
		{
			// Make special backup copies of existing txt/res files.
			for (i = 0; i < viNewLang.Size(); ++i)
			{
				int ie = viNewLang[i];
				stuFile.Format(L"%s\\data\\locales\\%S.txt",
					stuIcuDir.Chars(), rgpszLanguages[ie]);
				if (CreateBackupFile(stuFile.Chars()))
				{
					viModLang.Push(ie);
					stuFile.Format(L"%s\\%S_%S.res",
						stuIcuDir.Chars(), U_ICUDATA_NAME, rgpszLanguages[ie]);
					CreateBackupFile(stuFile.Chars());
				}
			}
			stuFile.Format(L"%s\\data\\locales\\root.txt", stuIcuDir.Chars());
			CreateBackupFile(stuFile.Chars());
			stuFile.Format(L"%s\\%S_root.res", stuIcuDir.Chars(), U_ICUDATA_NAME);
			CreateBackupFile(stuFile.Chars());
			stuFile.Format(L"%s\\data\\locales\\res_index.txt", stuIcuDir.Chars());
			CreateBackupFile(stuFile.Chars());
			stuFile.Format(L"%s\\%S_res_index.res", stuIcuDir.Chars(), U_ICUDATA_NAME);
			CreateBackupFile(stuFile.Chars());
		}
	}

	// Create a backup file, returning false if unable to do so (probably because the
	// original file doesn't exist).
	bool CreateBackupFile(const wchar * pszFile)
	{
		StrUni stuBackup;
		stuBackup.Format(L"%s-testorig", pszFile);
		// If the backup is already there from a previous failed test,
		// we want to overwrite it.
		return ::CopyFileW(pszFile, stuBackup.Chars(), true);
	}

	// Restore installed languages to the original state.
	void RestoreInstalledLanguages()
	{
		if (!viNewLang.Size())
			return;		// no languages would have been installed.

		StrUni stuRootDir(DirectoryFinder::FwRootDataDir());
		StrUni stuIcuDir(DirectoryFinder::IcuDir());
		StrUni stuFile;
		int i;
		// delete the created files.
		for (i = 0; i < viNewLang.Size(); ++i)
		{
			int ie = viNewLang[i];
			stuFile.Format(L"%s\\Languages\\%S.xml",
				stuRootDir.Chars(), rgpszLanguages[ie]);
			::DeleteFileW(stuFile.Chars());
			stuFile.Format(L"%s\\data\\locales\\%S.txt",
				stuIcuDir.Chars(), rgpszLanguages[ie]);
			::DeleteFileW(stuFile.Chars());
			stuFile.Format(L"%s\\%S_%S.res",
				stuIcuDir.Chars(), U_ICUDATA_NAME, rgpszLanguages[ie]);
			::DeleteFileW(stuFile.Chars());
		}
		// restore the modified files.
		for (i = 0; i < viModLang.Size(); ++i)
		{
			int ie = viModLang[i];
			stuFile.Format(L"%s\\data\\locales\\%S.txt",
				stuIcuDir.Chars(), rgpszLanguages[ie]);
			RestoreFromBackupFile(stuFile.Chars());
			stuFile.Format(L"%s\\%S_%S.res",
				stuIcuDir.Chars(), U_ICUDATA_NAME, rgpszLanguages[ie]);
			RestoreFromBackupFile(stuFile.Chars());
		}
		stuFile.Format(L"%s\\data\\locales\\root.txt", stuIcuDir.Chars());
		RestoreFromBackupFile(stuFile.Chars());
		stuFile.Format(L"%s\\%S_root.res", stuIcuDir.Chars(), U_ICUDATA_NAME);
		RestoreFromBackupFile(stuFile.Chars());
		stuFile.Format(L"%s\\data\\locales\\res_index.txt", stuIcuDir.Chars());
		RestoreFromBackupFile(stuFile.Chars());
		stuFile.Format(L"%s\\%S_res_index.res", stuIcuDir.Chars(), U_ICUDATA_NAME);
		RestoreFromBackupFile(stuFile.Chars());
	}

	// Restore a file from a backup created earlier, returning false if unable to do so.
	bool RestoreFromBackupFile(const wchar * pszFile)
	{
		StrUni stuBackup;
		stuBackup.Format(L"%s-testorig", pszFile);
		return ::MoveFileExW(stuBackup.Chars(), pszFile, MOVEFILE_REPLACE_EXISTING);
	}

	/*------------------------------------------------------------------------------------------
		Add one writing system to a test writing system factory.
	------------------------------------------------------------------------------------------*/
	HRESULT CreateTestWritingSystem(ILgWritingSystemFactory * pwsf, int ws, const wchar * pszWs)
	{
		if (pwsf && ws && pszWs && *pszWs)
		{
			WritingSystemPtr qzws;
			qzws.Attach(NewObj WritingSystem);
			qzws->SetHvo(ws);
			qzws->putref_WritingSystemFactory(pwsf);
			SmartBstr sbstrWs(pszWs);
			qzws->put_IcuLocale(sbstrWs);
			return pwsf->AddEngine(qzws);
		}
		return S_FALSE;
	}

	/*------------------------------------------------------------------------------------------
		Create a test writing system factory containing one writing system.
	------------------------------------------------------------------------------------------*/
	void CreateTestWritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
	{
		LgWritingSystemFactoryPtr qzwsf;
		qzwsf.Attach(NewObj LgWritingSystemFactory);
		CreateTestWritingSystem(qzwsf, kwsEng, kszEng);
		*ppwsf = qzwsf.Detach();
	}
}

#include "Vector_i.cpp"
template Vector<bool>;

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
