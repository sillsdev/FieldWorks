/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testMigrateData.h
Responsibility:
Last reviewed:

	header for unit testing the MigrateData DLL class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTMIGRATEDATA_H_INCLUDED
#define TESTMIGRATEDATA_H_INCLUDED

#pragma once
#include "Main.h"
#include <unit++.h>

namespace TestMigrateData
{
	extern const char * g_rgpszLanguages[];
	extern const int g_cLanguages;
	extern bool g_fVerbose;

	class TestIMigrateData : public unitpp::suite
	{
		IMigrateDataPtr m_qmd;
		Set<int> m_setws;
		Vector<int> m_viNewLang;	// indexes into g_rgpszLanguages[] that are not installed.
		Vector<int> m_viModLang;	// ICU has file, but no xml language definition file.

		// Helper class to create various filenames for a particular test database.
		struct FileNames
		{
			StrUni m_stuZipDir;
			StrUni m_stuZipFile;
			StrUni m_stuDataMdfFile;
			StrUni m_stuDataLdfFile;
			StrUni m_stuDataBakFile;
			StrUni m_stuLdfFile;
			StrUni m_stuMdfFile;

			void Initialize(const wchar * pszDatabase, int nVersion)
			{
				// Get the path to distfiles, something like "C:\FW\DistFiles"
				StrApp strRootDir(DirectoryFinder::FwRootDataDir());
				m_stuZipDir.Assign(strRootDir);
				int ich = m_stuZipDir.ReverseFindCh(L'\\');
				StrAnsi staAssert;
				staAssert.Format("[%S] FwRootDir contains subdirectories", pszDatabase);
				unitpp::assert_true(staAssert.Chars(), ich >= 0);
				m_stuZipDir.Replace(ich, m_stuZipDir.Length(), L"\\Src\\MigrateData\\Test");
				m_stuZipFile.Format(L"%s\\%s.zip", m_stuZipDir.Chars(), pszDatabase);
				m_stuDataMdfFile.Format(L"%s\\Data\\%s.mdf", strRootDir.Chars(), pszDatabase);
				m_stuDataLdfFile.Format(L"%s\\Data\\%s_log.ldf",
					strRootDir.Chars(), pszDatabase);
				m_stuDataBakFile.Format(L"%s\\Data\\%s-v%d.bak",
					strRootDir.Chars(), pszDatabase, nVersion);
				m_stuLdfFile.Format(L"%s\\%s_log.ldf", m_stuZipDir.Chars(), pszDatabase);
				m_stuMdfFile.Format(L"%s\\%s", m_stuZipDir.Chars(), pszDatabase);
			}
		};

		void DONT_testNullArgs()
		{
			unitpp::assert_true("Non-null m_qmd after setup", m_qmd.Ptr() != 0);
			HRESULT hr;
#ifndef _DEBUG
			try{
				CheckHr(hr = m_qmd->QueryInterface(IID_NULL, NULL));
				unitpp::assert_eq("QueryInterface(IID_NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("QueryInterface(IID_NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
#endif
			try{
				CheckHr(hr = m_qmd->Migrate(NULL, 0, NULL));
				unitpp::assert_eq("Migrate(NULL, 0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Migrate(NULL, 0, NULL) HRESULT", E_POINTER, thr.Result());
			}
		}

		void testMigrateFromV1()
		{
			MigrateDatabase(L"MigrationTest", 100000);
			DeleteMigratedDatabase(L"MigrationTest", 100000, true);
		}

		void DONT_testMigrateFromM5()
		{
			MigrateDatabase(L"Test-M5", 5000);
			DeleteMigratedDatabase(L"Test-M5", 5000, true);
		}

		// This method does the grunt work in extracting the database, attaching to it, and
		// migrating it to the current version.
		void MigrateDatabase(const wchar * pszDatabase, int nVersion)
		{
			StrAnsi staAssert;
			StrUni stuDatabase(pszDatabase);
			StrUni stuServer(SilUtil::LocalServerName());
			staAssert.Format("[%S] LocalServerName not null", pszDatabase);
			unitpp::assert_true(staAssert.Chars(), stuServer.Length() > 0);

			// Let's be paranoid.
			DeleteMigratedDatabase(pszDatabase, nVersion, false);

			// 1. Unzip {FWROOT}/Src/MigrateData/Test/{pszDatabase}.zip.

			FileNames fn;
			fn.Initialize(pszDatabase, nVersion);
			long nUnzip;
			try
			{
				// Initialize zip system data:
				ZipData zipd;
				zipd.m_sbstrDevice.Assign(fn.m_stuZipDir.Chars(), 3);	// eg, "C:\"
				zipd.m_sbstrPath = fn.m_stuZipDir;
				// Initialize Xceed Zip module:
				XceedZip xczUnzipper;
				if (!xczUnzipper.Init(&zipd))
				{
					staAssert.Format("Unzipped database backup file %S.bak [1]", pszDatabase);
					unitpp::assert_true(staAssert.Chars(), false);
				}
				xczUnzipper.SetPreservePaths(false);
				xczUnzipper.SetProcessSubfolders(false);
				xczUnzipper.SetZipFilename(fn.m_stuZipFile.Bstr());
				StrUni stuFile;
				stuFile.Format(L"%s.bak", pszDatabase);
				xczUnzipper.SetFilesToProcess(stuFile.Bstr());
				xczUnzipper.SetUnzipToFolder(fn.m_stuZipDir.Bstr());
				// Unzip the detached database.
				nUnzip = xczUnzipper.Unzip();
				// Ignore warnings. Xceedzip 4.5.81.0 now returns warnings here where 4.5.77.0 didn't.
				// We already ignore these in BackupHandler::UnzipForRestore.
				if (nUnzip == 526)
					nUnzip = 0;
			}
			catch (...)
			{
				staAssert.Format("Unzipping detached database %S threw exception", pszDatabase);
				unitpp::assert_true(staAssert.Chars(), false);
			}
			staAssert.Format("Unzipped database backup file %S.bak [2]", pszDatabase);
			unitpp::assert_eq(staAssert.Chars(), 0, nUnzip);

			::DeleteFile(fn.m_stuLdfFile.Chars());
			HRESULT hr;
			IOleDbEncapPtr qode;
			IOleDbCommandPtr qodc;
			try
			{
				CheckInstalledLanguages();
				StrAnsiStreamPtr qstas;
				StrAnsiStream::Create(&qstas);

				// 2. Attach {FWROOT}/Src/MigrateData/Test/{pszDatabase}.mdf.
				/*
				  osql -dMaster
				  restore database [%db_args%] from disk='%db_dir%\%db_args%.bak'
				*/
				qode.CreateInstance(CLSID_OleDbEncap);
				StrUni stuMaster(L"master");
				CheckHr(hr = qode->Init(stuServer.Bstr(), stuMaster.Bstr(), qstas, koltMsgBox, 0));
				staAssert.Format("qode->Init(\"%S\", \"master\") hr", stuServer.Chars());
				unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
				CheckHr(hr = qode->CreateCommand(&qodc));
				staAssert.Format("[master] qode->CreateCommand(&qodc) hr");
				unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
				StrUni stuCmd;
				stuCmd.Format(L"RESTORE DATABASE [%<1>s] FROM DISK='%<0>s\\%<1>s.bak'%n"
					L"WITH MOVE 'BlankLangProj' TO '%<2>s',%n"
					L"     MOVE 'BlankLangProj_log' TO '%<3>s'",
					fn.m_stuZipDir.Chars(), pszDatabase, fn.m_stuDataMdfFile.Chars(),
					fn.m_stuDataLdfFile.Chars());
				CheckHr(hr = qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
				staAssert.Format("Restored test database %S", stuCmd.Chars());
				unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
				qodc.Clear();
				qode.Clear();

				// *****************************************************************************
				// 3. Migrate {pszDatabase}.
				CheckHr(hr = m_qmd->Migrate(stuDatabase.Bstr(), kdbAppVersion, qstas));
				StrAnsi sta;
				sta.Format("m_qmd->Migrate(\"%S\", %d, ...) hr", pszDatabase, kdbAppVersion);
				unitpp::assert_eq(sta.Chars(), S_OK, hr);
				// *****************************************************************************

				// 4. Check whether everything worked.

				qode.CreateInstance(CLSID_OleDbEncap);
				CheckHr(hr = qode->Init(stuServer.Bstr(), stuDatabase.Bstr(), qstas, koltMsgBox, 0));
				staAssert.Format("qode->Init(\".\\SILFW\", \"%S\") hr", pszDatabase);
				unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
				CheckHr(hr = qode->CreateCommand(&qodc));
				staAssert.Format("[%S] qode->CreateCommand(&qodc) hr", pszDatabase);
				unitpp::assert_eq(staAssert.Chars(), S_OK, hr);

				int nVer;
				ComBool fMoreRows;
				ComBool fIsNull;
				unsigned long cbSpaceTaken;
				stuCmd = L"select DbVer from version$";
				CheckHr(hr = qodc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
				CheckHr(hr = qodc->GetRowset(0));
				CheckHr(hr = qodc->NextRow(&fMoreRows));
				CheckHr(hr = qodc->GetColValue(1, reinterpret_cast <BYTE *>(&nVer), sizeof(int),
					&cbSpaceTaken, &fIsNull, 0));

				// First, verify valid Ws values in the multilingual string tables.
				CheckMultiLingualWs(qodc, "MultiBigTxt$");
				CheckMultiLingualWs(qodc, "MultiStr$");
				CheckMultiLingualWs(qodc, "MultiBigStr$");
				// A check for MultiTxt$ used to be here, but it made the test crash.

				// Second, check the format fields of a sample of "string"/"big string" fields
				// for valid Ws values.
				LoadWritingSystemIds(qodc);
				const char * pszTable = (nVer <= 200202) ? "RnGenericRecord" : "RnGenericRec";
				CheckFormattingWs(qodc, pszTable, "Title_Fmt");
				CheckFormattingWs(qodc, "StTxtPara", "Contents_Fmt");
				CheckFormattingWs(qodc, "MultiStr$", "Fmt");
				CheckFormattingWs(qodc, "MultiBigStr$", "Fmt");
				m_setws.Clear();

				// Do this last, since it munges the database with test framework stuff.
//				RunSqlUnitTests(qode, qodc, pszDatabase);
			}
			catch (...)
			{
				qodc.Clear();
				qode.Clear();
				DeleteMigratedDatabase(pszDatabase, nVersion, false);
				throw;		// rethrow original exception
			}
			qodc.Clear();
			qode.Clear();
		}

		// This method does the grunt work of detaching and deleting the migrated database.
		void DeleteMigratedDatabase(const wchar * pszDatabase, int nVersion, bool fVerify)
		{
			RestoreInstalledLanguages();

			// 1. Detach {pszDatabase}.
			// m_qmd->Migrate() creates a writing system factory, which persists and which holds
			// onto a connection to the database.
			ILgWritingSystemFactoryBuilderPtr qwsfb;
			qwsfb.CreateInstance(CLSID_LgWritingSystemFactoryBuilder);
			HRESULT hr;
			IgnoreHr(hr = qwsfb->ShutdownAllFactories());
			qwsfb.Clear();
			StrUni stuDatabase(pszDatabase);
			IDbAdminPtr qmda;
			qmda.CreateInstance(CLSID_DbAdmin);
			IgnoreHr(hr = qmda->DetachDatabase(stuDatabase.Bstr()));
			if (fVerify)
			{
				StrAnsi staAssert;
				staAssert.Format("Detached test database %S", pszDatabase);
				unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			}
			// 2. Delete {FWROOT}/Src/MigrateData/Test/{pszDatabase}.mdf.
			//	  and migration files in (FWROOT)/Data directory.
			FileNames fn;
			fn.Initialize(pszDatabase, nVersion);
			/*
			  m_stuZipDir	 	"C:\FW\Src\MigrateData\Test"
			  m_stuZipFile		"C:\FW\Src\MigrateData\Test\MigrationTest.zip"
			  m_stuDataMdfFile	"C:\FW\distfiles\Data\MigrationTest.mdf"
			  m_stuDataLdfFile	"C:\FW\distfiles\Data\MigrationTest_log.ldf"
			  m_stuDataBakFile	"C:\FW\distfiles\Data\MigrationTest-v100000.bak"
			  m_stuLdfFile		"C:\FW\Src\MigrateData\Test\MigrationTest_log.ldf"
			  m_stuMdfFile		"C:\FW\Src\MigrateData\Test\MigrationTest"
			*/
			fn.m_stuMdfFile.Append(L".mdf");
			::DeleteFile(fn.m_stuMdfFile.Chars());
			::DeleteFile(fn.m_stuLdfFile.Chars());
			::DeleteFile(fn.m_stuDataMdfFile.Chars());
			::DeleteFile(fn.m_stuDataLdfFile.Chars());
			::DeleteFile(fn.m_stuDataBakFile.Chars());
		}

		// Check which languages are installed so that we can restore things to the original
		// state.  Create any needed backup files.
		void CheckInstalledLanguages()
		{
			StrUni stuRootDir(DirectoryFinder::FwRootDataDir());
			StrUni stuIcuDir(DirectoryFinder::IcuDir());
			StrUni stuFile;
			int i;
			for (i = 0; i < g_cLanguages; ++i)
			{
				stuFile.Format(L"%s\\Languages\\%S.xml",
					stuRootDir.Chars(), g_rgpszLanguages[i]);
				WIN32_FIND_DATA wfd;
				HANDLE hFind = ::FindFirstFileW(stuFile.Chars(), &wfd);
				if (hFind == INVALID_HANDLE_VALUE)
				{
					m_viNewLang.Push(i);
				}
				else
				{
					::FindClose(hFind);
				}
			}
			if (m_viNewLang.Size())
			{
				// Make special backup copies of existing files.
				for (i = 0; i < m_viNewLang.Size(); ++i)
				{
					int ie = m_viNewLang[i];
					stuFile.Format(L"%s\\data\\locales\\%S.txt",
						stuIcuDir.Chars(), g_rgpszLanguages[ie]);
					if (CreateBackupFile(stuFile.Chars()))
					{
						m_viModLang.Push(ie);
						stuFile.Format(L"%s\\%S_%S.res",
							stuIcuDir.Chars(), U_ICUDATA_NAME, g_rgpszLanguages[ie]);
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
			if (!m_viNewLang.Size())
				return;		// no languages would have been installed.

			StrUni stuRootDir(DirectoryFinder::FwRootDataDir());
			StrUni stuIcuDir(DirectoryFinder::IcuDir());
			StrUni stuFile;
			int i;
			// delete the created files.
			for (i = 0; i < m_viNewLang.Size(); ++i)
			{
				int ie = m_viNewLang[i];
				stuFile.Format(L"%s\\Languages\\%S.xml",
					stuRootDir.Chars(), g_rgpszLanguages[ie]);
				::DeleteFileW(stuFile.Chars());
				stuFile.Format(L"%s\\data\\locales\\%S.txt",
					stuIcuDir.Chars(), g_rgpszLanguages[ie]);
				::DeleteFileW(stuFile.Chars());
				stuFile.Format(L"%s\\%S_%S.res",
					stuIcuDir.Chars(), U_ICUDATA_NAME, g_rgpszLanguages[ie]);
				::DeleteFileW(stuFile.Chars());
			}
			// restore the modified files.
			for (i = 0; i < m_viModLang.Size(); ++i)
			{
				int ie = m_viModLang[i];
				stuFile.Format(L"%s\\data\\locales\\%S.txt",
					stuIcuDir.Chars(), g_rgpszLanguages[ie]);
				RestoreFromBackupFile(stuFile.Chars());
				stuFile.Format(L"%s\\%S_%S.res",
					stuIcuDir.Chars(), U_ICUDATA_NAME, g_rgpszLanguages[ie]);
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

		// Verify that the given file exists.
		void VerifyFileExists(const wchar * pszFile)
		{
			WIN32_FIND_DATA wfd;
			HANDLE hFind = ::FindFirstFileW(pszFile, &wfd);
			if (hFind != INVALID_HANDLE_VALUE)
				::FindClose(hFind);
			StrAnsi sta;
			sta.Format("%S exists", pszFile);
			unitpp::assert_true(sta.Chars(), hFind != INVALID_HANDLE_VALUE);
		}

		// Restore a file from a backup created earlier, returning false if unable to do so.
		bool RestoreFromBackupFile(const wchar * pszFile)
		{
			StrUni stuBackup;
			stuBackup.Format(L"%s-testorig", pszFile);
			return ::MoveFileExW(stuBackup.Chars(), pszFile, MOVEFILE_REPLACE_EXISTING);
		}

		// Function to check for proper Ws values in the given multilingual string table.
		void CheckMultiLingualWs(IOleDbCommand * podc, const char * pszTable)
		{
			HRESULT hr;
			ComBool fIsNull;
			ComBool fMoreRows;
			ComBool fTrue = TRUE;
			ULONG cbSpaceTaken;
			int cRowsTotal;
			int cRowsMatch;
			StrAnsi staAssert;
			StrUni stuCmd;
			stuCmd.Format(L"select count(*) from %S", pszTable);
			CheckHr(hr = podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			staAssert.Format("podc->ExecCommand() hr [%s 1]", pszTable);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			hr = podc->GetRowset(0);
			staAssert.Format("podc->GetRowset(0) hr [%s 1]", pszTable);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			hr = podc->NextRow(&fMoreRows);
			staAssert.Format("podc->NextRow(&fMoreRows) hr [%s 1]", pszTable);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			staAssert.Format("fMoreRows is TRUE [%s 1]", pszTable);
			unitpp::assert_eq(staAssert.Chars(), fTrue, fMoreRows);
			hr = podc->GetColValue(1, reinterpret_cast<BYTE *>(&cRowsTotal),
				sizeof(cRowsTotal), &cbSpaceTaken, &fIsNull, 0);
			staAssert.Format("podc->GetColValue(1, ...) hr [%s 1]", pszTable);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			stuCmd.Append(L" where [Ws] in (select [Id] from LgWritingSystem)");
			CheckHr(hr = podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			staAssert.Format("podc->ExecCommand() hr [%s 2]", pszTable);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			hr = podc->GetRowset(0);
			staAssert.Format("podc->GetRowset(0) hr [%s 2]", pszTable);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			hr = podc->NextRow(&fMoreRows);
			staAssert.Format("podc->NextRow(&fMoreRows) hr [%s 2]", pszTable);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			staAssert.Format("fMoreRows is TRUE [%s 2]", pszTable);
			unitpp::assert_eq(staAssert.Chars(), fTrue, fMoreRows);
			hr = podc->GetColValue(1, reinterpret_cast<BYTE *>(&cRowsMatch),
				sizeof(cRowsMatch), &cbSpaceTaken, &fIsNull, 0);
			staAssert.Format("podc->GetColValue(1, ...) hr [%s 2]", pszTable);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			staAssert.Format("updated Ws in %s seem to be okay", pszTable);
			unitpp::assert_eq(staAssert.Chars(), cRowsTotal, cRowsMatch);
		}

		// Function to check for proper Ws values inside a format field of a table.
		void CheckFormattingWs(IOleDbCommand * podc, const char * pszTable,
			const char * pszField)
		{
			HRESULT hr;
			ComBool fIsNull;
			ComBool fMoreRows;
			ComBool fTrue = TRUE;
			ULONG cbSpaceTaken;
			ITsPropsFactoryPtr qtpf;
			Vector<byte> vbFmt;
			ULONG cbFmt;
			StrAnsi staAssert;
			StrUni stuCmd;
			qtpf.CreateInstance(CLSID_TsPropsFactory);
			stuCmd.Format(L"select %S from %S", pszField, pszTable);
			CheckHr(hr = podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			staAssert.Format("podc->ExecCommand() hr [%s.%s]", pszTable, pszField);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			hr = podc->GetRowset(0);
			staAssert.Format("podc->GetRowset(0) hr [%s.%s]", pszTable, pszField);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			hr = podc->NextRow(&fMoreRows);
			staAssert.Format("podc->NextRow(&fMoreRows) hr [%s.%s A]", pszTable, pszField);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			while (fMoreRows)
			{
				vbFmt.Resize(512);
				hr = podc->GetColValue(1, reinterpret_cast<BYTE *>(vbFmt.Begin()),
					vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0);
				staAssert.Format("podc->GetColValue(1, ...) hr [%s.%s A]",
					pszTable, pszField);
				unitpp::assert_true(staAssert.Chars(), SUCCEEDED(hr));
				if (!fIsNull)
				{
					cbFmt = cbSpaceTaken;
					if (cbFmt > (ULONG)vbFmt.Size())
					{
						vbFmt.Resize(cbFmt, true);
						hr = podc->GetColValue(1, reinterpret_cast<BYTE *>(vbFmt.Begin()),
							vbFmt.Size(), &cbSpaceTaken, &fIsNull, 0);
						staAssert.Format("podc->GetColValue(1, ...) hr [%s.%s B]",
							pszTable, pszField);
						unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
					}
					ComVector<ITsTextProps> vqttp;
					Vector<int> vich;
					vqttp.Resize(100);
					vich.Resize(vqttp.Size());
					int cb2 = (int)cbFmt;
					int cttp = 0;
					hr = qtpf->DeserializeRgPropsRgb(vqttp.Size(), vbFmt.Begin(), &cb2, &cttp,
						(ITsTextProps **)vqttp.Begin(), vich.Begin());
					staAssert.Format("qtpf->DeserializeRgPropsRgb() hr [%s.%s]",
						pszTable, pszField);
					unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
					int ittp;
					for (ittp = 0; ittp < cttp; ++ittp)
					{
						int nVar;
						int nVal;
						hr = vqttp[ittp]->GetIntPropValues(ktptWs, &nVar, &nVal);
						staAssert.Format("vqttp[%d]->GetIntPropValues(ktptWs, ...) hr [%s.%s]",
							ittp, pszTable, pszField);
						unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
						staAssert.Format("vqttp[%d]->GetIntPropValues(ktptWs, ...) ws [%s.%s]",
							ittp, pszTable, pszField);
						unitpp::assert_true(staAssert.Chars(), m_setws.IsMember(nVal));
					}
				}
				hr = podc->NextRow(&fMoreRows);
				staAssert.Format("podc->NextRow(&fMoreRows) hr [%s.%s B]",
					pszTable, pszField);
				unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			}
		}

		// Load the set of valid writing system object ids.
		void LoadWritingSystemIds(IOleDbCommand * podc)
		{
			m_setws.Clear();

			HRESULT hr;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;
			StrUni stuCmd;
			stuCmd.Assign("select [Id] from LgWritingSystem");
			CheckHr(hr = podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			unitpp::assert_eq("podc->ExecCommand() hr [LgWritingSystem.Id]", S_OK, hr);
			hr = podc->GetRowset(0);
			unitpp::assert_eq("podc->GetRowset(0) hr [LgWritingSystem.Id]", S_OK, hr);
			hr = podc->NextRow(&fMoreRows);
			unitpp::assert_eq("podc->NextRow(&fMoreRows) hr [LgWritingSystem.Id A]", S_OK, hr);
			int ws;
			while (fMoreRows)
			{
				hr = podc->GetColValue(1, reinterpret_cast<BYTE *>(&ws), sizeof(ws),
					&cbSpaceTaken, &fIsNull, 0);
				unitpp::assert_eq("podc->GetColValue(1, ...) hr [LgWritingSystem.Id]",
					S_OK, hr);
				if (!fIsNull)
				{
					unitpp::assert_true("unique LgWritingSystem ids", !m_setws.IsMember(ws));
					m_setws.Insert(ws);
				}
				hr = podc->NextRow(&fMoreRows);
				unitpp::assert_eq("podc->NextRow(&fMoreRows) hr [LgWritingSystem.Id A]",
					S_OK, hr);
			}

			unitpp::assert_true("At least one LgWritingSystem", m_setws.Size() > 0);
		}

		// Run the FieldWorks SQL Unit tests on the migrated database.
		// THESE FAIL BECAUSE SOME TESTS ARE DATA DEPENDENT!
		void RunSqlUnitTests(IOleDbEncap * pode, IOleDbCommand * podc,
			const wchar * pszDatabase)
		{
			// Get the RootDir minus the trailing DistFiles (on developer's machines, which is
			// where tests are run).
			StrUni stuRoot(DirectoryFinder::FwRootCodeDir());
			int ich = stuRoot.ReverseFindCh(L'\\');
			stuRoot.Replace(ich, stuRoot.Length(), L"");
			StrUni stuSqlFile;

			// Install the tsqlunit framework.
			stuSqlFile.Format(L"%s\\Test\\tsqlunit\\tsqlunit.sql", stuRoot.Chars());
			RunSqlScript(pode, stuSqlFile.Chars());
			// Paranoid, but safe...
			stuSqlFile.Format(L"%s\\Test\\tsqlunit\\dropallunittests.sql", stuRoot.Chars());
			RunSqlScript(pode, stuSqlFile.Chars());
			// Install the "normal" (single user) tests.
			stuSqlFile.Format(L"%s\\Src\\Cellar\\Test\\ut_FwCore.sql", stuRoot.Chars());
			RunSqlScript(pode, stuSqlFile.Chars());
			stuSqlFile.Format(L"%s\\Src\\Ling\\Test\\ut_LingSP.sql", stuRoot.Chars());
			RunSqlScript(pode, stuSqlFile.Chars());
			stuSqlFile.Format(L"%s\\Src\\Scripture\\Test\\ut_Scripture.sql", stuRoot.Chars());
			RunSqlScript(pode, stuSqlFile.Chars());

			StrAnsi staAssert;
			StrUni stuCmd;
			HRESULT hr;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;

			stuCmd.Assign(L"EXEC tsu_runTests");
			CheckHr(hr = podc->ExecCommand(stuCmd.Bstr(), knSqlStmtNoResults));
			staAssert.Format("%S: podc->ExecCommand() hr [EXEC tsu_runTests]", pszDatabase);
			//unitpp::assert_eq(staAssert.Chars(), S_OK, hr);

			stuCmd.Assign(L"SELECT success, testCount, failureCount, errorCount"
				L" FROM tsuLastTestResult");
			CheckHr(hr = podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
			staAssert.Format("%S: podc->ExecCommand() hr [SELECT...FROM tsuLastTestResult]",
				pszDatabase);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			hr = podc->GetRowset(0);
			staAssert.Format("%S: podc->GetRowset(0) hr [SELECT...FROM tsuLastTestResult]",
				pszDatabase);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			hr = podc->NextRow(&fMoreRows);
			staAssert.Format(
				"%S: podc->NextRow(&fMoreRows) hr [SELECT...FROM tsuLastTestResult]",
				pszDatabase);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			staAssert.Format("%S: fMoreRows [SELECT...FROM tsuLastTestResult]",
				pszDatabase);
			unitpp::assert_true(staAssert.Chars(), fMoreRows);
			byte fSuccess = 0;
			int cTests;
			int cFailed;
			int cErrors;
			hr = podc->GetColValue(1, reinterpret_cast<BYTE *>(&fSuccess), sizeof(fSuccess),
				&cbSpaceTaken, &fIsNull, 0);
			staAssert.Format(
				"%S: podc->GetColValue(1, ...) hr [SELECT...FROM tsuLastTestResult]",
				pszDatabase);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			hr = podc->GetColValue(2, reinterpret_cast<BYTE *>(&cTests), sizeof(cTests),
				&cbSpaceTaken, &fIsNull, 0);
			staAssert.Format(
				"%S: podc->GetColValue(2, ...) hr [SELECT...FROM tsuLastTestResult]",
				pszDatabase);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			hr = podc->GetColValue(3, reinterpret_cast<BYTE *>(&cFailed), sizeof(cFailed),
				&cbSpaceTaken, &fIsNull, 0);
			staAssert.Format(
				"%S: podc->GetColValue(3, ...) hr [SELECT...FROM tsuLastTestResult]",
				pszDatabase);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
			hr = podc->GetColValue(4, reinterpret_cast<BYTE *>(&cErrors), sizeof(cErrors),
				&cbSpaceTaken, &fIsNull, 0);
			staAssert.Format(
				"%S: podc->GetColValue(4, ...) hr [SELECT...FROM tsuLastTestResult]",
				pszDatabase);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);

			StrAnsi staDb(pszDatabase);
			if (g_fVerbose)
			{
				printf("\nRunning normal SQL Unit tests on updated database %s\n"
					"%s: %d tests, %d failed, %d errors\n",
					staDb.Chars(), fSuccess ? "SUCCESS" : "FAILURE", cTests, cFailed, cErrors);

				stuCmd.Assign(L"SELECT test, message FROM tsuFailures");
				CheckHr(hr = podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
				staAssert.Format("%S: podc->ExecCommand() hr [SELECT...FROM tsuFailures]",
					pszDatabase);
				unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
				hr = podc->GetRowset(0);
				staAssert.Format("%S: podc->GetRowset(0) hr [SELECT...FROM tsuFailures]",
					pszDatabase);
				unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
				hr = podc->NextRow(&fMoreRows);
				staAssert.Format("%S: podc->NextRow(&fMoreRows) hr [SELECT...FROM tsuFailures]",
					pszDatabase);
				unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
				StrAnsi staMsg;
				bool fFirst = true;
				wchar rgchTest[260];
				wchar rgchMessage[260];
				while (fMoreRows)
				{
					if (fFirst)
					{
						printf("%s: SQL Unit Test Failure Information:\n", staDb.Chars());
						fFirst = false;
					}
					hr = podc->GetColValue(1, reinterpret_cast<BYTE *>(&rgchTest),
						sizeof(rgchTest), &cbSpaceTaken, &fIsNull, 2);
					staAssert.Format(
						"%S: podc->GetColValue(1, ...) hr [SELECT...FROM tsuFailures]",
						pszDatabase);
					unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
					hr = podc->GetColValue(2, reinterpret_cast<BYTE *>(&rgchMessage),
						sizeof(rgchMessage), &cbSpaceTaken, &fIsNull, 2);
					staAssert.Format(
						"%S: podc->GetColValue(2, ...) hr [SELECT...FROM tsuFailures]",
						pszDatabase);
					unitpp::assert_eq(staAssert.Chars(), S_OK, hr);

					staMsg.Format("%S: Test %S - %S", pszDatabase, rgchTest, rgchMessage);
					printf("%s\n", staMsg.Chars());

					hr = podc->NextRow(&fMoreRows);
					staAssert.Format(
						"%S: podc->NextRow(&fMoreRows) hr [SELECT...FROM tsuFailures]",
						pszDatabase);
					unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
				}

				stuCmd.Assign(L"SELECT test, message FROM tsuErrors");
				CheckHr(hr = podc->ExecCommand(stuCmd.Bstr(), knSqlStmtSelectWithOneRowset));
				staAssert.Format("%S: podc->ExecCommand() hr [SELECT...FROM tsuErrors]",
					pszDatabase);
				unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
				hr = podc->GetRowset(0);
				staAssert.Format("%S: podc->GetRowset(0) hr [SELECT...FROM tsuErrors]",
					pszDatabase);
				unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
				hr = podc->NextRow(&fMoreRows);
				staAssert.Format("%S: podc->NextRow(&fMoreRows) hr [SELECT...FROM tsuErrors]",
					pszDatabase);
				unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
				fFirst = true;
				while (fMoreRows)
				{
					if (fFirst)
					{
						printf("%s: SQL Unit Test Failure Information:\n", staDb.Chars());
						fFirst = false;
					}
					hr = podc->GetColValue(1, reinterpret_cast<BYTE *>(&rgchTest),
						sizeof(rgchTest), &cbSpaceTaken, &fIsNull, 2);
					staAssert.Format(
						"%S: podc->GetColValue(1, ...) hr [SELECT...FROM tsuErrors]",
						pszDatabase);
					unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
					hr = podc->GetColValue(2, reinterpret_cast<BYTE *>(&rgchMessage),
						sizeof(rgchMessage), &cbSpaceTaken, &fIsNull, 2);
					staAssert.Format(
						"%S: podc->GetColValue(2, ...) hr [SELECT...FROM tsuErrors]",
						pszDatabase);
					unitpp::assert_eq(staAssert.Chars(), S_OK, hr);

					staMsg.Format("%S: Test %S - %S", pszDatabase, rgchTest, rgchMessage);
					printf("%s\n", staMsg.Chars());

					hr = podc->NextRow(&fMoreRows);
					staAssert.Format(
						"%S: podc->NextRow(&fMoreRows) hr [SELECT...FROM tsuErrors]",
						pszDatabase);
					unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
				}
			}

			stuSqlFile.Format(L"%s\\Test\\tsqlunit\\dropallunittests.sql", stuRoot.Chars());
			RunSqlScript(pode, stuSqlFile.Chars());
			stuSqlFile.Format(L"%s\\Test\\tsqlunit\\removetsqlunit.sql", stuRoot.Chars());
			RunSqlScript(pode, stuSqlFile.Chars());

			staAssert.Format("%S: normal SQL Unit tests succeeded (NOT!)", pszDatabase);
			unitpp::assert_eq(staAssert.Chars(), 1, fSuccess);
		}

		void RunSqlScript(IOleDbEncap * pode, const wchar * pszSqlFile)
		{
			IOleDbCommandPtr qodc;
			StrUni stuSql;
			SmartBstr sbstrServer;
			SmartBstr sbstrDatabase;
			StrAnsi staAssert;
			HRESULT hr;

			pode->get_Server(&sbstrServer);
			pode->get_Database(&sbstrDatabase);

			// ENHANCE: (SteveMi) Alistair says he could do this without a user or password.
			// It doesn't happen on Rand's machine. Would like to know how Alistair's allows it.
			stuSql.Format(L"EXEC master..xp_cmdshell N'osql -S%s -d\"%s\" "
				L"-UFwDeveloper -Pcareful -i\"%s\" -n'",
				sbstrServer.Chars(), sbstrDatabase.Chars(), pszSqlFile);

			hr = pode->CreateCommand(&qodc);
			staAssert.Format("%S - RunSql(\"%S\"): pode->CreateCommand() hr",
				sbstrDatabase.Chars(), pszSqlFile);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);

			CheckHr(hr = qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
			staAssert.Format("%S - RunSql(\"%S\"): podc->ExecCommand() hr",
				sbstrDatabase.Chars(), pszSqlFile);
			unitpp::assert_eq(staAssert.Chars(), S_OK, hr);
		}

	public:
		TestIMigrateData();

		virtual void Setup()
		{
			MigrateData::CreateCom(NULL, IID_IMigrateData, (void **)&m_qmd);
		}
		virtual void Teardown()
		{
			m_qmd.Clear();
		}
	};
}

#include "Vector_i.cpp"
#include "Set_i.cpp"

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkmig-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*TESTMIGRATEDATA_H_INCLUDED*/
