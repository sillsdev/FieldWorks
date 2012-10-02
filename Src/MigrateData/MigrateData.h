/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, 2005, SIL International. All rights reserved.

File: MigrateData.h
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	Header file for the MigrateData data upgrade module.

	This file contains class declarations for the following class:
		MigrateData - Used to change the version of a database, while maintaining its
						integrity.

-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef __MIGRATEDATA_H__
#define __MIGRATEDATA_H__


/*----------------------------------------------------------------------------------------------
	Standard implementation of the IMigrateData interface.

	Cross Reference: ${IMigrateData}
----------------------------------------------------------------------------------------------*/
class MigrateData : public IMigrateData
{
protected:
	long m_cref;

public:
	MigrateData();
	~MigrateData();

	//:> Public static methods.
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	//:> IUnknown interface methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);
	STDMETHOD_(ULONG, AddRef)(void);

	//:> IMigrateData interface methods.
	STDMETHOD(Migrate)(BSTR bstrDbName, int nDestVersion, IStream* pfist);

	//:> These are public so that they can be called from a DLL callback function.
	void MigrateData::ErrorBox(int rid, ...);
	HRESULT _Migrate(BSTR bstrDbName, int nDestVersion, int * pnSourceVersion = NULL);

protected:
	bool M5toM6(IOleDbEncap * pode);
	void PreScriptUpdateM5toM6(StrUni & stuServer, StrUni & stuDB, IStream * pstrmLog);
	bool RunSql(IOleDbEncap * pode, const wchar * pszSql);
	void PostScriptUpdateEncTows(IOleDbEncap * pode);
	bool MigrateEncToWs(IOleDbEncapPtr & qode, BSTR bstrDbName);
	bool LoadVersion2_6Data(IOleDbEncapPtr & qode, BSTR bstrOldDbName);
	bool NormalizeUnicode(IOleDbEncap * pode);
	bool MigrateWsCodeToId(IOleDbEncapPtr & qode, BSTR bstrDbName);
	bool UpdateFrom200000To200006(IOleDbEncapPtr & qode, BSTR bstrDbName);
	void PostScriptUpdateFrom200000(IOleDbEncap * pode);
	void PostScriptUpdateWsCodeToId(BSTR bstrNewDbName, BSTR bstrOldDbName);
	bool UpdateRulesWsCodeToId(BYTE * pbRule, ULONG cbRule, Vector<BYTE> & vbRule,
		HashMap<int,int> & hmwsid, bool fForPara);
	bool CreateNewDb(const wchar * pszInitScript, const wchar * pszDbName);
	bool InstallLanguages(IOleDbEncap * pode);
	HRESULT GetVersionNumber(IOleDbCommand * podc, int * nVersion);
	void ProcessPossibleXmlUpdateFiles(IOleDbEncap * pode, int nVersion);
	void ProcessXmlUpdateFile(IOleDbEncap * pode, const StrUni & stuFile);
	int ReadOneIntFromDatabase(IOleDbEncap * pode, BSTR bstrQuery);
	bool FixBulNumFontInfoFontSize(IOleDbEncap * pode);
	bool UpdateBulNumFontInfoFontSize(BYTE * pbRule, ULONG cbRule, Vector<BYTE> & vbRule);

	StrUni m_stuFwData;
	StrUni m_stuServer;
	IStreamPtr m_qfist;
	AfProgressDlgPtr m_qprog;
};

typedef ComSmartPtr<IMigrateData> IMigrateDataPtr;

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkmig.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif   // __MIGRATEDATA_H__
