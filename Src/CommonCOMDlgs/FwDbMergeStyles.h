/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2005 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwDbMergeStyles.h
Responsibility: FW Team (Written by TE Team, but probably more understandable by Steve Mc)
Last reviewed: never

Description:
	Define the dialog classes that support the File / Language Project Properties menu command.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef FWDBMERGESTYLES_H_INCLUDED
#define FWDBMERGESTYLES_H_INCLUDED 1

//:End Ignore

/*----------------------------------------------------------------------------------------------
	String crawler for changing (merging) styles. This also implements a COM interface
	that supports the same operation on other places that styles are stored in the
	database, not just the formatted string binary format fields.
	Hungarian: fdms
----------------------------------------------------------------------------------------------*/
class FwDbMergeStyles : public DbStringCrawler, IFwDbMergeStyles
{
	typedef DbStringCrawler SuperClass;
//	friend class AfStylesDlg;
public:
	// DbStringCrawler methods.
	virtual bool ProcessFormatting(ComVector<ITsTextProps> & vqttp);
	virtual bool ProcessBytes(Vector<byte> & vb)
	{
		Assert(false);
		return false;
	}

	//:> Standard COM creation method.
	static void CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods.
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	// IFwDbMergeStyles methods.
	STDMETHOD(Initialize)(BSTR bstrServer, BSTR bstrDatabase,
		IStream * pstrmLog, int hvoRootObj, const GUID * pclsidApp);
	STDMETHOD(AddStyleReplacement)(BSTR bstrOldStyleName, BSTR bstrNewStyleName);
	STDMETHOD(AddStyleDeletion)(BSTR bstrDeleteStyleName);

	STDMETHOD(Process)(DWORD hWnd);

	STDMETHOD(InitializeEx)(IOleDbEncap * pode, IStream * pstrmLog, int hvoRootObj,
		const GUID * pclsidApp);

protected:
	FwDbMergeStyles();
	~FwDbMergeStyles();

	AfProgressDlgPtr m_qprog;
	int m_cref;
	int m_hvoRoot;
	const GUID * m_pclsidApp;

	Vector<StrUni> m_vstuOldNames;
	Vector<StrUni> m_vstuNewNames;
	Vector<StrUni> m_vstuDelNames;

	bool Delete(StrUni &);
	bool Rename(StrUni & stuOld, StrUni & stuNew);
	bool ProcessFormatting(ComVector<ITsTextProps> & vqttp, StrUni stuDelete);
};


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*FWDBMERGESTYLES_H_INCLUDED*/
