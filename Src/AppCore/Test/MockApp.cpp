/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: MockApp.cpp
Responsibility:
Last reviewed:

	Mock App class to support unit tests for the AppCore library.
-------------------------------------------------------------------------------*//*:End Ignore*/


#include "testAfLib.h"

namespace unitpp
{
	void GlobalSetup(bool verbose)
	{
		::OleInitialize(NULL);
		StrUtil::InitIcuDataDir();
	}
	void GlobalTeardown()
	{
		//::OleUninitialize();	// hangs on exit for some reason when this is called??
	}
}

namespace TestAfLib
{
	MockDbInfo::MockDbInfo()
	{
	}
	MockDbInfo::~MockDbInfo()
	{
	}
	AfLpInfo * MockDbInfo::GetLpInfo(HVO hvoLp)
	{
		int clpi = m_vlpi.Size();
		for (int ilpi = 0; ilpi < clpi; ilpi++)
		{
			if (hvoLp == m_vlpi[ilpi]->GetLpId())
				return m_vlpi[ilpi];
		}
		// We didn't find it in the cache, so create it now.
		MockLpInfoPtr qlpi;
		qlpi.Create();
		qlpi->Init(this, hvoLp);
		qlpi->OpenProject();
		m_vlpi.Push(dynamic_cast<AfLpInfo *>(qlpi.Ptr()));
		return dynamic_cast<AfLpInfo *>(qlpi.Ptr());
	}

	MockLpInfo::MockLpInfo()
	{
	}
	MockLpInfo::~MockLpInfo()
	{
	}
	bool MockLpInfo::OpenProject()
	{
		return false;
	}
	bool MockLpInfo::LoadProjBasics()
	{
		return false;
	}

	MockApp::MockApp()
	{
		s_fws.SetRoot(_T("Data Notebook"));//"Software\\SIL\\FieldWorks\\Data Notebook"
		AfApp * papp = AfApp::Papp();
		StrApp strHelp = papp->GetFwCodePath().Chars();
		strHelp.Append(_T("\\Helps\\FieldWorks_Data_Notebook_Help.chm"));
		papp->SetHelpFilename(strHelp);
	////////////////////////////////////////////////////////////////////////////////////////////
	//	s_fws.SetRoot(_T("CLE"));//"Software\\SIL\\FieldWorks\\CLE";
	//	AfApp * papp = AfApp::Papp();
	//	StrApp strHelp = papp->GetFwCodePath().Chars();
	//	strHelp.Append(_T("\\Helps\\ListEditorHelps.chm"));
	//	papp->SetHelpFilename(strHelp);
	}

	MockApp::~MockApp()
	{
	}

	AfDbInfo * MockApp::GetDbInfo(const OLECHAR * pszDbName, const OLECHAR * pszSvrName)
	{
		int cdbi = m_vdbi.Size();
		for (int idbi = 0; idbi < cdbi; idbi++)
		{
			if (wcscmp(pszDbName, m_vdbi[idbi]->DbName()) == 0 &&
				wcscmp(pszSvrName, m_vdbi[idbi]->ServerName()) == 0)
			{
				return m_vdbi[idbi];
			}
		}
		// We didn't find it in the cache, so load it now.
		MockDbInfoPtr qdbi;
		qdbi.Create();
		qdbi->Init(pszSvrName, pszDbName, m_qfistLog);
		m_vdbi.Push(dynamic_cast<AfDbInfo *>(qdbi.Ptr()));
		return dynamic_cast<AfDbInfo *>(qdbi.Ptr());
	}

	/*----------------------------------------------------------------------------------------------
	Gets the default backup directory.
	----------------------------------------------------------------------------------------------*/
	STDMETHODIMP MockApp::GetDefaultBackupDirectory(BSTR * pbstrDefBackupDir)
	{
		// TODO: return the default backup directory (My Documents/My FieldWorks/Backups)
		*pbstrDefBackupDir = NULL;
		return S_OK;
	}

	MockApp g_app;
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkaflib-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
