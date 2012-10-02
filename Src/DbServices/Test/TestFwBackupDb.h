/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestFwBackupDb.h
Responsibility:
Last reviewed:

	Unit tests for the FwBackupDb class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTFWBACKUPDB_H_INCLUDED
#define TESTFWBACKUPDB_H_INCLUDED

#pragma once

#include "testDbServices.h"

namespace TestDbServices
{
	static StrAppBuf s_rgstrbProjDisplayNames[] = {
		_T("BackupTest20080408"),
		_T("TestBackup20080408")
	};
	static StrAppBuf s_rgstrbBackupFileNames[] = {
		_T("BackupTest20080408 2008-04-08 1614.zip"),
		_T("TestBackup20080408 2008-04-08 1615.zip")
	};
	static StrAppBuf s_rgstrbVersionNames[] = {
		_T("BackupTest20080408 - 2008-04-08 16:14"),
		_T("TestBackup20080408 - 2008-04-08 16:15")
	};
	static StrUniBufSmall s_rgstubsVernLocale[] = {
		L"xkal",
		L"xfwt"
	};

//		L"from LangProject_CurVernWss cv "
	static wchar * s_pszSqlGetVernIcuLocale =
		L"select Top 1 ws.ICULocale "
		L"from LangProject_CurVernWss cv "
		L"join LgWritingSystem ws on ws.Id = cv.Dst "
		L"order by cv.Ord";

	class TestFwBackupDb : public unitpp::suite
	{
		MockBackupDelegates m_bd;
		DIFwBackupDbPtr m_qbkup;

		void DONT_testNullArgs()
		{
			unitpp::assert_true("Non-null m_qbkup after setup", m_qbkup.Ptr() != 0);
			HRESULT hr;
#ifndef _DEBUG
			hr = m_qbkup->QueryInterface(IID_NULL, NULL);
			unitpp::assert_eq("QueryInterface(IID_NULL, NULL) HRESULT", E_POINTER, hr);
#endif
			hr = m_qbkup->Init(NULL, 0);
			unitpp::assert_eq("Init(NULL, 0) HRESULT", E_POINTER, hr);
			hr = m_qbkup->UserConfigure(NULL, FALSE, NULL);
			unitpp::assert_eq("UserConfigure(FALSE, NULL) HRESULT", E_POINTER, hr);
		}

		void testInitAndClose()
		{
			HRESULT hr;
			hr = m_qbkup->Init(&m_bd, 0);
			unitpp::assert_eq("Init(&m_bd, 0) HRESULT", S_OK, hr);
			FwBackupDb * pzbkup = dynamic_cast<FwBackupDb *>(m_qbkup.Ptr());
			unitpp::assert_true("Testing FwBackupDb implementation", pzbkup != NULL);
			StrUni stuServer = BackupHandler::GetLocalServer();
			unitpp::assert_true("Init() set the local server", stuServer.Length() > 0);
			unitpp::assert_true("Init() set the InitDone flag", pzbkup->m_fInitDone);
			hr = m_qbkup->Close();
			unitpp::assert_eq("Close() HRESULT", S_OK, hr);
			stuServer = BackupHandler::GetLocalServer();
			unitpp::assert_true("Close() cleared the local server", stuServer.Length() == 0);
		}

	public:
		TestFwBackupDb();

		virtual void Setup()
		{
			FwBackupDb::CreateCom(NULL, IID_DIFwBackupDb, (void **)&m_qbkup);
		}
		virtual void Teardown()
		{
			m_qbkup.Clear();
			// Clean up any leftover debris in the system error handling stuff.  Without this,
			// we can get an assertion in a later test that error information unexpectedly
			// exists!
			IErrorInfo * pIErrorInfo = NULL;
			HRESULT hr;
			hr = ::GetErrorInfo(0, &pIErrorInfo);
		}
	};


	class TestAvailableProjects : public unitpp::suite
	{
		AvailableProjects m_avprj;

		void testCollectProjectBackupFiles()
		{
			StrAppBufPath strbpPath(g_strRootDir.Chars());
			StrAppBufPath strbpHighlight;
			m_avprj.CollectProjectBackupFiles(strbpPath, strbpHighlight);
			unitpp::assert_eq("two backup files available", 2, m_avprj.m_vprj.Size());
			unitpp::assert_eq("one version of 1st file", 1, m_avprj.m_vprj[0].m_vver.Size());
			unitpp::assert_eq("one version of 2nd file", 1, m_avprj.m_vprj[1].m_vver.Size());

			unitpp::assert_eq("correct DirectoryPath[0]",
				strbpPath, m_avprj.m_vprj[0].m_strbpDirectoryPath);
			unitpp::assert_eq("correct DirectoryPath[1]",
				strbpPath, m_avprj.m_vprj[1].m_strbpDirectoryPath);

			if (_tcscmp(m_avprj.m_vprj[0].m_strbDatabaseName.Chars(),
				m_avprj.m_vprj[1].m_strbDatabaseName.Chars()) < 0)
			{
				// sorted order as above
				unitpp::assert_eq("correct Project DisplayName[0]",
					s_rgstrbProjDisplayNames[0], m_avprj.m_vprj[0].m_strbDatabaseName);
				unitpp::assert_eq("correct Project DisplayName[1]",
					s_rgstrbProjDisplayNames[1], m_avprj.m_vprj[1].m_strbDatabaseName);
				unitpp::assert_eq("correct Version FileName[0][0]",
					s_rgstrbBackupFileNames[0], m_avprj.m_vprj[0].m_vver[0].m_strbFileName);
				unitpp::assert_eq("correct Version FileName[1][0]",
					s_rgstrbBackupFileNames[1], m_avprj.m_vprj[1].m_vver[0].m_strbFileName);
				unitpp::assert_eq("correct Version DisplayName[0][0]",
					s_rgstrbVersionNames[0], m_avprj.m_vprj[0].m_vver[0].m_strbDisplayName);
				unitpp::assert_eq("correct Version DisplayName[1][0]",
					s_rgstrbVersionNames[1], m_avprj.m_vprj[1].m_vver[0].m_strbDisplayName);
			}
			else
			{
				// reverse sorted order
				unitpp::assert_eq("correct Project DisplayName[0]",
					s_rgstrbProjDisplayNames[1], m_avprj.m_vprj[0].m_strbDatabaseName);
				unitpp::assert_eq("correct Project DisplayName[1]",
					s_rgstrbProjDisplayNames[0], m_avprj.m_vprj[1].m_strbDatabaseName);
				unitpp::assert_eq("correct Version FileName[0][0]",
					s_rgstrbBackupFileNames[1], m_avprj.m_vprj[0].m_vver[0].m_strbFileName);
				unitpp::assert_eq("correct Version FileName[1][0]",
					s_rgstrbBackupFileNames[0], m_avprj.m_vprj[1].m_vver[0].m_strbFileName);
				unitpp::assert_eq("correct Version DisplayName[0][0]",
					s_rgstrbVersionNames[1], m_avprj.m_vprj[0].m_vver[0].m_strbDisplayName);
				unitpp::assert_eq("correct Version DisplayName[1][0]",
					s_rgstrbVersionNames[0], m_avprj.m_vprj[1].m_vver[0].m_strbDisplayName);
			}
		}

	public:
		TestAvailableProjects();

		virtual void SuiteSetup()
		{
			SetRootDir();
		}
	};

	class TestBackupHandler : public unitpp::suite
	{
		MockBackupDelegates m_bd;
		BackupHandler * m_pbkph;
		Vector<StrUni> m_vstuFilesToDelete;
		StrUni m_stuServer;

		void testRestoreMethods1()
		{
			unitpp::assert_true("Created BackupHandler object", m_pbkph != NULL);
			m_pbkph->Init(NULL);
			m_pbkph->ConnectToMasterDb();
			BackupHandler::SetInstanceHandle();

			BackupProgressDlg bkpprg(0, true);
			m_pbkph->m_pbkpprg = &bkpprg;

			m_pbkph->m_bkpi.m_strbDeviceName.Assign(g_strRootDir.Chars(), 1);
			m_pbkph->m_bkpi.m_strbDirectoryPath = g_strRootDir.Chars();
			m_pbkph->m_bkpi.m_strZipFileName = s_rgstrbBackupFileNames[0].Chars();
			m_pbkph->m_bkpi.m_strProjectFullName = s_rgstrbProjDisplayNames[0].Chars();

			int nErrorResult = m_pbkph->InitializeFileZipper();
			unitpp::assert_eq("InitializeFileZipper() succeeded", 1, nErrorResult);

			int nRestoreOptions = 0;
			StrUni stuNewDatabaseName;
			StrUni stuSource; // Full path of unzipped file
			StrUni stuSourceXml;
			nErrorResult = m_pbkph->GenerateRestoreNames(nRestoreOptions, stuNewDatabaseName,
				stuSource, stuSourceXml);
			unitpp::assert_eq("GenerateRestoreNames() succeeded", 1, nErrorResult);
			// Note not all temp directories are TEMP!
			int ich = stuSource.FindStrCI(L"\\SILFwBackupBuffer\\BackupTest20080408.bak");
			unitpp::assert_true("db backup (.BAK) filename is ok", ich > 0);
			ich = stuSourceXml.FindStrCI(L"\\SILFwBackupBuffer\\BackupTest20080408.xml");
			unitpp::assert_true("XML backup filename is ok", ich > 0);

			bool fDbAlreadyExists = false;
			nErrorResult = m_pbkph->SetupWithRestoreOptions(nRestoreOptions, stuNewDatabaseName,
				fDbAlreadyExists);
			unitpp::assert_eq("SetupWithRestoreOptions() succeeded", 1, nErrorResult);
			unitpp::assert_true("database does not already exist", !fDbAlreadyExists);

			bkpprg.EnableAbortButton(false);
			m_pbkph->m_bkpi.m_fXml = false;
			bool fUseXml = true;
			int nRes = m_pbkph->UnzipForRestore(0, stuSource, stuSourceXml, fUseXml);
			WIN32_FIND_DATA wfd;
			HANDLE hFind = ::FindFirstFileW(stuSource.Chars(), &wfd);
			if (hFind != INVALID_HANDLE_VALUE)
			{
				::FindClose(hFind);
				m_vstuFilesToDelete.Push(stuSource);
			}
			HANDLE hFindXml = ::FindFirstFileW(stuSourceXml.Chars(), &wfd);
			if (hFindXml != INVALID_HANDLE_VALUE)
			{
				::FindClose(hFindXml);
				::DeleteFileW(stuSourceXml.Chars());
			}
			unitpp::assert_eq("UnzipForRestore() succeeded", 1, nRes);
			unitpp::assert_true("fUseXml is false", !fUseXml);
			unitpp::assert_true("db backup (.BAK) file exists",
				hFind != INVALID_HANDLE_VALUE);
			unitpp::assert_true("XML backup file does not exist",
				hFindXml == INVALID_HANDLE_VALUE);

			nErrorResult = m_pbkph->RestoreFromBak(stuSource, nRestoreOptions);
			unitpp::assert_eq("RestoreFromBak() succeeded", 1, nErrorResult);

			VerifyExpectedIcuLocale(0);
		}

		void VerifyExpectedIcuLocale(int idx)
		{
			StrAnsi staMsg;
			// Try to connect to the restored database.
			IOleDbEncapPtr qode;	// declare this before qodc, so it gets destructed after.
			IOleDbCommandPtr qodc;
			qode.CreateInstance(CLSID_OleDbEncap);
			staMsg.Format("qode->Init() succeeded [%d]", idx + 1);
			SmartBstr sbstrServer;
			GetLocalServer(&sbstrServer);
			StrUni stuDatabase(s_rgstrbProjDisplayNames[idx].Chars());
			HRESULT hr = qode->Init(sbstrServer, stuDatabase.Bstr(), NULL, koltReturnError,
				koltvFwDefault);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);

			staMsg.Format("qode->CreateCommand() succeeded [%d]", idx + 1);
			hr = qode->CreateCommand(&qodc);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);

			staMsg.Format("qodc->ExecCommand() succeeded [%d]", idx + 1);
			StrUni stuSql(s_pszSqlGetVernIcuLocale);
			hr = qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);

			staMsg.Format("qodc->GetRowset() succeeded [%d]", idx + 1);
			hr = qodc->GetRowset(0);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);

			staMsg.Format("qodc->NextRow() succeeded [%d]", idx + 1);
			ComBool fMoreRows;
			hr = qodc->NextRow(&fMoreRows);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("qodc->NextRow() has another row [%d]", idx + 1);
			unitpp::assert_true(staMsg.Chars(), fMoreRows);

			staMsg.Format("qodc->GetColValue(1) succeeded [%d]", idx + 1);
			OLECHAR rgchIcuLocale[MAX_PATH];
			ComBool fIsNull;
			UINT luSpaceTaken;
			hr = qodc->GetColValue(1, reinterpret_cast<BYTE *>(rgchIcuLocale),
				isizeof(OLECHAR) * MAX_PATH, &luSpaceTaken, &fIsNull, 2);
			unitpp::assert_true(staMsg.Chars(), fMoreRows);
			staMsg.Format("vernacular ws is ok [%d]", idx + 1);
			unitpp::assert_eq(staMsg.Chars(), s_rgstubsVernLocale[idx], rgchIcuLocale);
			qodc.Clear();
			qode.Clear();
		}

		void testRestoreMethods2()
		{
			unitpp::assert_true("Created BackupHandler object [2]", m_pbkph != NULL);
			m_pbkph->Init(NULL);
			m_pbkph->ConnectToMasterDb();
			BackupHandler::SetInstanceHandle();

			BackupProgressDlg bkpprg(0, true);
			m_pbkph->m_pbkpprg = &bkpprg;

			m_pbkph->m_bkpi.m_strbDeviceName.Assign(g_strRootDir.Chars(), 1);
			m_pbkph->m_bkpi.m_strbDirectoryPath = g_strRootDir.Chars();
			m_pbkph->m_bkpi.m_strZipFileName = s_rgstrbBackupFileNames[1].Chars();
			m_pbkph->m_bkpi.m_strProjectFullName = s_rgstrbProjDisplayNames[1].Chars();

			int nErrorResult = m_pbkph->InitializeFileZipper();
			unitpp::assert_eq("InitializeFileZipper() succeeded [2]", 1, nErrorResult);

			int nRestoreOptions = 0;
			StrUni stuNewDatabaseName;
			StrUni stuSource; // Full path of unzipped file
			StrUni stuSourceXml;
			nErrorResult = m_pbkph->GenerateRestoreNames(nRestoreOptions, stuNewDatabaseName,
				stuSource, stuSourceXml);
			unitpp::assert_eq("GenerateRestoreNames() succeeded [2]", 1, nErrorResult);
			// Note not all temp directories are TEMP
			int ich = stuSource.FindStrCI(L"\\SILFwBackupBuffer\\TestBackup20080408.bak");
			unitpp::assert_true("db backup (.BAK) filename is ok [2]", ich > 0);
			ich = stuSourceXml.FindStrCI(L"\\SILFwBackupBuffer\\TestBackup20080408.xml");
			unitpp::assert_true("XML backup filename is ok [2]", ich > 0);

			bool fDbAlreadyExists = false;
			nErrorResult = m_pbkph->SetupWithRestoreOptions(nRestoreOptions, stuNewDatabaseName,
				fDbAlreadyExists);
			unitpp::assert_eq("SetupWithRestoreOptions() succeeded [2]", 1, nErrorResult);
			unitpp::assert_true("database does not already exist [2]", !fDbAlreadyExists);

			bkpprg.EnableAbortButton(false);
			m_pbkph->m_bkpi.m_fXml = true;
			bool fUseXml = true;
			int nRes = m_pbkph->UnzipForRestore(0, stuSource, stuSourceXml, fUseXml);
			WIN32_FIND_DATA wfd;
			HANDLE hFind = ::FindFirstFileW(stuSource.Chars(), &wfd);
			if (hFind != INVALID_HANDLE_VALUE)
			{
				::FindClose(hFind);
				::DeleteFileW(stuSource.Chars());
			}
			HANDLE hFindXml = ::FindFirstFileW(stuSourceXml.Chars(), &wfd);
			if (hFindXml != INVALID_HANDLE_VALUE)
			{
				::FindClose(hFindXml);
				m_vstuFilesToDelete.Push(stuSourceXml);
			}
			unitpp::assert_eq("UnzipForRestore() succeeded [2]", 1, nRes);
			unitpp::assert_true("fUseXml is true [2]", fUseXml);
			unitpp::assert_true("db backup (.BAK) file exists [2]",
				hFind != INVALID_HANDLE_VALUE);
			unitpp::assert_true("XML backup file exists [2]", hFindXml != INVALID_HANDLE_VALUE);

			// Migration 203 changed the length of some class and attribute names.  Because of this,
			// I updated the version of the file used in these tests.
			//
			nErrorResult = m_pbkph->RestoreFromXml(stuSourceXml);
			unitpp::assert_eq("RestoreFromXml() failed", 1, nErrorResult);
			VerifyExpectedIcuLocale(1);
		}

		void testBackupMethods()
		{
			unitpp::assert_true("Created BackupHandler object [3]", m_pbkph != NULL);
			m_bd.AddOpenDb(L"TestLangProj");
			m_pbkph->Init(&m_bd);
			m_pbkph->ConnectToMasterDb();
			m_pbkph->LimitBackupToActive();
			BackupHandler::SetInstanceHandle();

			m_pbkph->m_bkpi.m_strbDeviceName.Assign(g_strRootDir.Chars(), 1);
			m_pbkph->m_bkpi.m_strbDirectoryPath = g_strRootDir.Chars();
			BackupHandler::LogFile log;
			bool fFinished = false;
			bool fBackupOk = true;
			bool fAuto = m_pbkph->TryAutomaticBackup(BackupHandler::kManual, 0, NULL, NULL, log,
				fFinished, fBackupOk);
			unitpp::assert_true("TryAutomaticBackup() succeeded", fBackupOk);
			unitpp::assert_true("manual implies not automatic", !fAuto);
			unitpp::assert_true("backup not finished after TryAutomaticBackup()", !fFinished);

			int nDummy = 0;
			fBackupOk = m_pbkph->m_bkpi.AutoSelectBackupProjects(nDummy);
			unitpp::assert_true("AutoSelectBackupProjects() succeeded", fBackupOk);

			int cNeedBackup = 0;
			for (int i = 0; i < m_pbkph->m_bkpi.m_vprojd.Size(); ++i)
			{
				BackupInfo::ProjectData & projd = m_pbkph->m_bkpi.m_vprojd[i];
				if (projd.m_fBackup)
				{
					++cNeedBackup;
				}
				else if (projd.m_stuDatabase == L"TestLangProj")
				{
					// It's conceivably possible that the test is run at a time when
					// TestLangProj doesn't need to be backed up...
					projd.m_fBackup = true;
					++cNeedBackup;
				}
			}
			// (REVIEW: SteveMiller/SteveMcConnel): This assert will fail if any
			// non-FW database exists.
			unitpp::assert_eq("one db to backup", 1, cNeedBackup);
			for (int i = 0; fBackupOk && i < m_pbkph->m_bkpi.m_vprojd.Size(); ++i)
			{
				BackupInfo::ProjectData & projd = m_pbkph->m_bkpi.m_vprojd[i];
				StrUni stuName(projd.m_stuProject);
				if (!projd.m_stuProject.EqualsCI(projd.m_stuDatabase))
					stuName.FormatAppend(L" (%s)", projd.m_stuDatabase.Chars());
				if (projd.m_fBackup)
				{
					SYSTEMTIME systTime;
					::GetLocalTime(&systTime);
					fBackupOk = m_pbkph->BackupProject(projd, stuName, 0, systTime, log, true);
					HANDLE hFile = ::CreateFileW(m_pbkph->m_stuCurrentZipFile.Chars(),
						GENERIC_READ, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
					DWORD dwSizeHigh;
					DWORD dwSize = ::GetFileSize(hFile, &dwSizeHigh);
					BOOL fCloseOk = ::CloseHandle(hFile);
					// cleanup after ourselves.
					BOOL fDeleteOk = ::DeleteFileW(m_pbkph->m_stuCurrentZipFile.Chars());

					unitpp::assert_true("BackupProject() succeeded", fBackupOk);
					unitpp::assert_true("Backup file exists", hFile != INVALID_HANDLE_VALUE);
					unitpp::assert_true("Reasonable backup file size [1]",
						dwSize > (DWORD)5000000L);
					unitpp::assert_eq("Reasonable backup file size [2]", (DWORD)0, dwSizeHigh);
					unitpp::assert_true("Closed file handle ok", fCloseOk != 0);
					unitpp::assert_true("Deleted backup file ok", fDeleteOk != 0);
				}
			}
		}

		void DropTestDatabase(const achar * pszProjName, const achar * pszMdfFile,
			const achar * pszLdfFile)
		{
			try
			{
				// Delete the UnitTestProj database.
				StrUni stuDatabase(L"master");
				IOleDbEncapPtr qode; // Declare before qodc.
				IOleDbCommandPtr qodc;
				qode.CreateInstance(CLSID_OleDbEncap);
				CheckHr(qode->Init(m_stuServer.Bstr(), stuDatabase.Bstr(), NULL,
					koltReturnError, 1000));
				CheckHr(qode->CreateCommand(&qodc));

				StrUni stuQuery;
				StrUni stuProjName(pszProjName);
				stuQuery.Format(L"DROP DATABASE [%s]", stuProjName.Chars());
				CheckHr(qode->CreateCommand(&qodc));
				qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults);
				qodc.Clear();
				qode.Clear();
			}
			catch (...)
			{
			}
			// Just in case, try to delete the relevant files.
			if (pszMdfFile && *pszMdfFile)
				::DeleteFile(pszMdfFile);
			if (pszLdfFile && *pszLdfFile)
				::DeleteFile(pszLdfFile);
		}

	public:
		TestBackupHandler();

		virtual void Setup()
		{
			m_pbkph = new BackupHandler;
		}

		virtual void Teardown()
		{
			delete m_pbkph;
			m_pbkph = NULL;
		}

		virtual void SuiteSetup()
		{
			SetRootDir();

			// Get the local server name.
			SmartBstr sbstr;
			GetLocalServer(&sbstr);
			m_stuServer.Assign(sbstr.Chars());
			BackupHandler::SetLocalServer(sbstr.Chars());
		}
		virtual void SuiteTeardown()
		{
			// delete any databases we restored, or their intermediate backup files.
			for (int i = 0; i < m_vstuFilesToDelete.Size(); ++i)
				::DeleteFileW(m_vstuFilesToDelete[i].Chars());
			DropTestDatabase(_T("BackupTest20080408"), _T(""), _T(""));
			DropTestDatabase(_T("TestBackup20080408"), _T(""), _T(""));
		}
	};

}

#endif /*TESTFWBACKUPDB_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkDbSvcs-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
