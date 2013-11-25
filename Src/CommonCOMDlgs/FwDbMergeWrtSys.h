/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 2004-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: FwDbMergeWrtSys.h
Responsibility: Steve McConnel
Last reviewed: never

Description:
	Define the dialog classes that support the File / Language Project Properties menu command.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef FWDBMERGEWRTSYS_H_INCLUDED
#define FWDBMERGEWRTSYS_H_INCLUDED 1

//:End Ignore

/*----------------------------------------------------------------------------------------------
	String crawler for changing (merging) writing systems.  This also implements a COM interface
	that supports the same operation on other places that writing systems are stored in the
	database, not just the formatted string binary format fields.
----------------------------------------------------------------------------------------------*/
class FwDbMergeWrtSys : public DbStringCrawler, IFwDbMergeWrtSys
{
	typedef DbStringCrawler SuperClass;
public:
	// DbStringCrawler methods.
	virtual bool ProcessFormatting(ComVector<ITsTextProps> & vqttp)
	{
		Assert(false);
		return false;
	}
	virtual bool ProcessBytes(Vector<byte> & vbFmt);

	//:> Standard COM creation method.
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods.
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	// IFwDbMergeWrtSys methods.
	STDMETHOD(Initialize)(IFwTool * pfwt, BSTR bstrServer, BSTR bstrDatabase,
		IStream * pstrmLog, int hvoProj, int hvoRootObj, int wsUser);
	STDMETHOD(Process)(int wsOld, BSTR bstrOldName, int wsNew, BSTR bstrNewName);

protected:
	FwDbMergeWrtSys();
	~FwDbMergeWrtSys();
	void MergeWsInMultilingualData(int wsOld, int wsNew);
	void GetMultiTxtTables(Vector<StrUni> & vstuTable);
	void MergeWsDataInTable(int wsOld, int wsNew, const OLECHAR * pszTable, bool fHasFmt);
	bool ReadIntValue(int iCol, int & val);
	int ReadTextValue(int iCol, Vector<wchar> & vchTxt);
	int ReadFmtValue(int iCol, Vector<byte> & vbFmt);

	bool ChangeStyleWs(Vector<byte> & vbRule, int wsOld, int wsNew);
	void UpdateWsList(const OLECHAR * pszTable);

	IFwToolPtr m_qfwt;
	ITsStrFactoryPtr m_qtsf;
	AfProgressDlgPtr m_qprog;
	int m_cref;
	int m_wsOld;
	int m_wsNew;
	int m_hvoProj;
	int m_hvoRoot;
	int m_wsUser;
};


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*FWDBMERGEWRTSYS_H_INCLUDED*/
