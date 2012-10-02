/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestDbAdmin.h
Responsibility:
Last reviewed:

	Unit tests for the DbAdmin class.
	Not yet hooked up (need to do something to get the tests included)
	as the tested class is not yet implemented.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTDBDMIN_H_INCLUDED
#define TESTDBDMIN_H_INCLUDED

#pragma once

#include "testDbAccess.h"

namespace TestDbAccess
{
	class TestDbAdmin : public unitpp::suite
	{
		IDbAdminPtr m_qdba;

		void testNullArgs()
		{
			unitpp::assert_true("Non-null m_qdba after setup", m_qdba.Ptr() != 0);
			HRESULT hr;
			IgnoreHr(hr = m_qdba->CopyDatabase(NULL, NULL));
			unitpp::assert_eq("CopyDatabase(NULL, NULL) HRESULT", E_POINTER, hr);
			IgnoreHr(hr = m_qdba->AttachDatabase(NULL, NULL));
			unitpp::assert_eq("AttachDatabase(NULL, NULL) HRESULT", E_POINTER, hr);
			IgnoreHr(hr = m_qdba->DetachDatabase(NULL));
			unitpp::assert_eq("DetachDatabase(NULL) HRESULT", E_POINTER, hr);
			IgnoreHr(hr = m_qdba->RenameDatabase(NULL, NULL, NULL, false, false));
			unitpp::assert_eq("RenameDatabase(NULL) HRESULT", E_POINTER, hr);
			IgnoreHr(hr = m_qdba->get_FwRootDir(NULL));
			unitpp::assert_eq("get_FwRootDir(NULL) HRESULT", E_POINTER, hr);
			IgnoreHr(hr = m_qdba->get_FwMigrationScriptDir(NULL));
			unitpp::assert_eq("get_FwMigrationScriptDir(NULL) HRESULT", E_POINTER, hr);
			IgnoreHr(hr = m_qdba->get_FwDatabaseDir(NULL));
			unitpp::assert_eq("get_FwDatabaseDir(NULL) HRESULT", E_POINTER, hr);
			IgnoreHr(hr = m_qdba->get_FwTemplateDir(NULL));
			unitpp::assert_eq("get_FwTemplateDir(NULL) HRESULT", E_POINTER, hr);
		}

		void testDirectories()
		{
			unitpp::assert_true("Non-null m_qdba after setup", m_qdba.Ptr() != 0);
			HRESULT hr;

			// FwRootDir.
			SmartBstr sbstrRoot;
			hr = m_qdba->get_FwRootDir(&sbstrRoot);
			StrUni stuFwZip = sbstrRoot.Chars();
			stuFwZip += L"\\XceedZip.dll";
			unitpp::assert_true("Expected file found in FW root directory",
				::GetFileAttributes(stuFwZip.Chars()) !=INVALID_FILE_ATTRIBUTES );

			//FwMigrationScriptDir();

			SmartBstr sbstrMig;
			hr = m_qdba->get_FwMigrationScriptDir(&sbstrMig);
			StrUni stuMigFile = sbstrMig.Chars();
			stuMigFile += L"\\M5toM6.sql";
			unitpp::assert_true("Expected file found in FW migration directory",
				::GetFileAttributes(stuMigFile.Chars()) !=INVALID_FILE_ATTRIBUTES );

			// FwDatabaseDir();
			SmartBstr sbstrDb;
			hr = m_qdba->get_FwDatabaseDir(&sbstrDb);
			// At this point I don't know any predictable file I can expect to find here.
			//StrUni stuDbFile = sbstrDb.Chars();
			//stuDbFile += L"\\Something.mdf";
			//unitpp::assert_true("Expected file found in FW database directory",
			//	::GetFileAttributes(stuDbFile.Chars()) !=INVALID_FILE_ATTRIBUTES );

			//FwTemplateDir();
			SmartBstr sbstrTemplateDir;
			hr = m_qdba->get_FwTemplateDir(&sbstrTemplateDir);
			// BlankLangProj.mdf is not available on the build machine in this directory.
			StrUni stuNewLangProjPath = sbstrTemplateDir.Chars();
			stuNewLangProjPath += L"\\NewLangProj.xml";
			unitpp::assert_true("Expected file found in FW template directory",
				::GetFileAttributes(stuNewLangProjPath.Chars()) !=INVALID_FILE_ATTRIBUTES );
		}
		void testDbOps()
		{
			// Do some clean-up. Without it we got a lot of failing tests because these
			// files were left around for whatever reason.

			unitpp::assert_true("Non-null m_qdba after setup", m_qdba.Ptr() != 0);
			HRESULT hr;

			// CopyDatabase (part 1)
			SmartBstr sbstrTemplateDir;
			hr = m_qdba->get_FwTemplateDir(&sbstrTemplateDir);

			StrUni stuBlankLangProjPath = sbstrTemplateDir.Chars();
			stuBlankLangProjPath += L"\\BlankLangProj"; // nb without .mdf

			// On dev machines BlankLangProj is in the distfiles\templates directory.
			// On the build machine BlankLangProj is in the output\templates directory.
			StrApp strTemp = stuBlankLangProjPath.Chars();
			strTemp += _T(".mdf");
			if (::GetFileAttributes(strTemp.Chars()) == INVALID_FILE_ATTRIBUTES)
			{
				int ich = stuBlankLangProjPath.FindStrCI(L"\\distfiles\\");
				Assert(ich > -1);
				stuBlankLangProjPath.Replace(ich + 1, stuBlankLangProjPath.Length(),
					L"output\\templates");
				sbstrTemplateDir = stuBlankLangProjPath.Chars();
				stuBlankLangProjPath += L"\\BlankLangProj"; // nb without .mdf
			}

			StrUni stuCopiedDbName = L"Copy'Of Bla\x0301nkL\x00e3ng\x3600Proj";

			StrUni stuCopiedDbPath = sbstrTemplateDir.Chars();
			stuCopiedDbPath += L"\\";
			stuCopiedDbPath += stuCopiedDbName;

			StrUni stuBlankLangProjDbPathMdf = stuBlankLangProjPath;
			stuBlankLangProjDbPathMdf += L".mdf";

			StrUni stuCopiedDbPathMdf = stuCopiedDbPath;
			stuCopiedDbPathMdf += L".mdf";

			hr = m_qdba->CopyDatabase(stuBlankLangProjPath.Bstr(), stuCopiedDbPath.Bstr());
			unitpp::assert_true("Copy database produced the expected file",
				::GetFileAttributes(stuCopiedDbPathMdf.Chars()) !=INVALID_FILE_ATTRIBUTES );

			// AttachDatabase
			StrUni stuDbInternalName(L"DummyDb");
			hr = m_qdba->AttachDatabase(stuDbInternalName.Bstr(), stuCopiedDbPath.Bstr());
			// Note: This attach will fail if something left BlankLangProj_log.ldf in
			// C:\Program Files\Microsoft SQL Server\MSSQL.2\MSSQL\Data (or wherever
			// SILFW is installed on your machine.
			// test\MakeBlankLP.bat should have cleared out that file.
			// This attach looks for the above file, and not finding it, it creates
			// DummyDb_log.ldf in fw\distfiles\Templates.
			// The error in the above case is:
			// SQL error is 5173: Cannot associate files with different databases.
			unitpp::assert_eq("AttachDatabase() HRESULT", S_OK, hr);
			IOleDbEncapPtr qode;
			qode.CreateInstance(CLSID_OleDbEncap);
			StrUni stuServer(L".\\SilFw");
			hr = qode->Init(stuServer.Bstr(), stuDbInternalName.Bstr(), NULL,
				koltReturnError, 10000);
			unitpp::assert_eq("AttachDatabase() successful", S_OK, hr);
			qode.Clear();


			// Detach database
			hr = m_qdba->DetachDatabase(stuDbInternalName.Bstr());
			qode.CreateInstance(CLSID_OleDbEncap);
			try
			{
				CheckHr(hr = qode->Init(stuServer.Bstr(), stuDbInternalName.Bstr(), NULL,
					koltReturnError, 10000));
				qode.Clear();
				unitpp::assert_eq("DetachDatabase() makes it unavailable", E_FAIL, hr);
			}
			catch(Throwable& thr)
			{
				qode.Clear();
				unitpp::assert_eq("DetachDatabase() makes it unavailable", E_FAIL, thr.Result());
			}
			// Rename database

			// First attach it, since that makes it a harder test. Our Rename with auto-detach
			// requires that the old internal and old file names match. These will both be
			// stuCopiedDbPath, the name of the copy.
			hr = m_qdba->AttachDatabase(stuCopiedDbName.Bstr(), stuCopiedDbPath.Bstr());

			StrUni stuRenamedName(L"RenamedCopyOfBlankLangProj");
			StrUni stuRenamedPath = sbstrTemplateDir.Chars();
			stuRenamedPath += L"\\";
			stuRenamedPath += stuRenamedName;

			hr = m_qdba->RenameDatabase(sbstrTemplateDir, stuCopiedDbName.Bstr(),
				stuRenamedName.Bstr(), true, true);
			qode.CreateInstance(CLSID_OleDbEncap);
			hr = qode->Init(stuServer.Bstr(), stuRenamedName.Bstr(), NULL,
				koltReturnError, 10000);
			qode.Clear();
			unitpp::assert_eq("RenameDatabase() successful", S_OK, hr);

			StrUni stuRenamedPathMdf = stuRenamedPath;
			stuRenamedPathMdf += ".mdf";

			unitpp::assert_true("Rename database produced the expected file",
				::GetFileAttributes(stuRenamedPathMdf.Chars()) !=INVALID_FILE_ATTRIBUTES );

			// old log file is gone
			StrUni stuOldLogPathLdf = stuCopiedDbPath;
			stuOldLogPathLdf += L"_log.ldf";
			unitpp::assert_true("Rename database renamed or removed old log file",
				::GetFileAttributes(stuOldLogPathLdf.Chars()) ==INVALID_FILE_ATTRIBUTES );
			hr = m_qdba->DetachDatabase(stuRenamedName.Bstr());
			unitpp::assert_eq("Final detach successful", S_OK, hr);

			// Clean up (for future test!)
			::DeleteFile(stuRenamedPathMdf);
			StrUni stuRenamedPathLdf = stuRenamedPath;
			stuRenamedPathLdf += L"_log.ldf";
			::DeleteFile(stuRenamedPathLdf);
			// The generated log file name is based on the database internal name.
			// So the original log file when we attached the first time is this name.
			StrUni stuInternalLogLdf = sbstrTemplateDir.Chars();
			stuInternalLogLdf += L"\\";
			stuInternalLogLdf += stuDbInternalName;
			stuInternalLogLdf += L"_Log.ldf";
			::DeleteFile(stuInternalLogLdf);
		}
	public:
		TestDbAdmin();

		virtual void Setup()
		{
			m_qdba.CreateInstance(CLSID_DbAdmin);
		}
		virtual void Teardown()
		{
			m_qdba.Clear();
		}
	};
}

#endif /*TESTDBDMIN_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkdba-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
