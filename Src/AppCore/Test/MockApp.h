/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: MockApp.h
Responsibility:
Last reviewed:

	Mock App class to support unit tests for the AppCore library.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef MOCKAPP_H_INCLUDED
#define MOCKAPP_H_INCLUDED

#pragma once

namespace TestAfLib
{
	class MockDbInfo : public AfDbInfo
	{
	public:
		MockDbInfo();
		~MockDbInfo();
		virtual AfLpInfo * GetLpInfo(HVO hvoLp);

	//	StrUni m_stuSvrName;			// Currently connected server (DBPROP_INIT_DATASOURCE)
	//	StrUni m_stuDbName;				// Current database (DBPROP_INIT_CATALOG)
	//	IOleDbEncapPtr m_qode;			// Current data access object.
	//	ComSmartPtr<IFwMetaDataCache> m_qmdc;	// Current meta data cache.
	//	Vector<AppFilterInfo> m_vafi;	// Vector of loaded (cached) filters.
	//	Vector<AppSortInfo> m_vasi;		// Vector of loaded (cached) sort methods.
	//	Vector<AfLpInfoPtr> m_vlpi;		// Vector of loaded (cached) language projects.
	//	UserViewSpecVec m_vuvs;			// Specs of user-customizeable views; see TlsOptDlg...
	//	ILgWritingSystemFactoryPtr m_qwsf;
	};
	typedef GenSmartPtr<MockDbInfo> MockDbInfoPtr;

	class MockLpInfo : public AfLpInfo
	{
	public:
		MockLpInfo();
		~MockLpInfo();
		virtual bool OpenProject();
		virtual bool LoadProjBasics();

	//	StrUni m_stuPrjName; // Name of the current language project.
	//	ComSmartPtr<AfDbStylesheet> m_qdsts;
	//	Vector<int> m_vwsAnal; // Current analysis encodings.
	//	Vector<int> m_vwsVern; // Current vernacular encodings.
	//	Vector<int> m_vwsAllAnal; // Possible analysis encodings for project.
	//	Vector<int> m_vwsAllVern; // Possible vernacular encodings for project.
	//	Vector<AppOverlayInfo> m_vaoi; // Vector of loaded (cached) overlays.
	//	HashMap<HVO, int> m_hmPssWs;
	//	Vector<PossListInfoPtr> m_vqpli; // Vector of loaded (cached) possibility lists.
	//	Vector<HVO> m_vhvoPsslIds; // Ids of possibility lists for current project.
	//	HVO m_hvoLp; // Id of the language project itself.
	//	AfDbInfoPtr m_qdbi; // Points to the database containing this language project.
	//	CustViewDaPtr m_qcvd; // Holds the data access cache for this project.
	//	IActionHandlerPtr m_qacth; // Points to the undo/redo action handler
	//	StrUni m_stuExtLinkRoot;  // External Link root
	//	GUID m_guidSync; // Unique ID used in Synch$ for changes made by this application.
	//	int m_nLastSync; // The last ID from Synch$ when we synchronized data with the database.
	};
	typedef GenSmartPtr<MockLpInfo> MockLpInfoPtr;

	class MockApp : public AfDbApp
	{
		typedef AfDbApp SuperClass;
	public:
		//:>************************************************************************************
		//:>	public methods.
		//:>************************************************************************************

		MockApp();
		~MockApp();
		virtual AfDbInfo * GetDbInfo(const OLECHAR * pszDbName, const OLECHAR * pszSvrName);
		virtual SilTime DropDeadDate()
		{
			return SilTime(3000, 1, 1);
		}

		STDMETHOD(GetDefaultBackupDirectory)(BSTR * pbstrDefBackupDir);

	//	StrApp m_strFwPath;
	//	StrApp m_strHelpFilename;
	//	static int s_wsUser;
	//	static AfApp * s_papp;
	//	static int s_cmsgSinceIdle;
	//	FwSettings s_fws;
	//	HINSTANCE m_hinst;
	//	Pcsz m_pszCmdLine;
	//	HashMapStrUni<Vector<StrUni> > m_hmsuvstuCmdLine;
	//	int m_nShow;
	//	CmdExec m_cex;
	//	bool m_fQuit;
	//	Vector<AfMainWndPtr> m_vqafw;
	//	AfMainWndPtr m_qafwCur;
	//	MsrSysType m_nMsrSys;
	//	DWORD m_dwRegister;
	//	IStreamPtr m_qfistLog;				// Log file for errors.
	//	long m_cunkExport;
	//	IVwPatternPtr m_qxpat;
	////////////////////////////////////////////////////////////////////////////////////////////
	//	Vector<AfDbInfoPtr> m_vdbi;		// Vector of database connections.
	//	StrUni m_stuLocalServer;		// The name of the local server (e.g., ls-zook\\SILFW).
	};

	extern MockApp g_app;
}

#endif /*MOCKAPP_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkaflib-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
