/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: DbStringCrawler.h
Responsibility: Steve McConnel
Last reviewed: never

Description:
	A DbStringWalker is an object that can walk through the database and do something to
	every instance of text.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef STRCRAWL_H_INCLUDED
#define STRCRAWL_H_INCLUDED 1

#define HVO long

//:End Ignore

/*----------------------------------------------------------------------------------------------
	Language project properties dialog.

	@h3{Hungarian: dsc}
----------------------------------------------------------------------------------------------*/
class DbStringCrawler
{
public:
	DbStringCrawler(bool fPlain, bool fFormatting, bool fProcessBytes, bool fCharacters);
	~DbStringCrawler();

	static void FillPhraseTagsTable(IOleDbCommand * podc, const OLECHAR * pszClass,
		const OLECHAR * pszField, HashMap<GUID,HVO> & hmguidhvoPss, HVO hvo);
	static void UpdatePhraseTagsTable(int flidTable, bool fInsert, IOleDbEncap * pode,
		HVO hvoTag, HVO hvoEnd, HVO hvoAnchor);
	static void GetFieldsForTypes(IOleDbCommand * podc, const int * rgnTypes, int cTypes,
		Vector<StrUni> & vstuClass, Vector<StrUni> & vstuField,
		const int * rgflidToIgnore = NULL, const int cflidToIgnore = 0);

	bool Init(StrUni stuServerName, StrUni stuDatabase, IStream * pstrmLog,
		IAdvInd3 * padvi3 = NULL);
	bool Init(IOleDbEncap * pode, IStream *pstrmLog, IAdvInd3 * padvi3 = NULL);
	void ResetConnection();
	void Terminate(HVO hvoRootObj);
	void DoAll(int stid1, int stid2, bool fTrans = true, const OLECHAR * pszIdsTable = NULL);

	virtual bool ProcessFormatting(ComVector<ITsTextProps> & vpttp) = 0;
	virtual bool ProcessBytes(Vector<byte> & vb) = 0;

	IOleDbEncap * GetOleDbEncap()
	{
		return m_qode;
	}

	IOleDbCommand * GetOleDbCommand()
	{
		return m_qodc;
	}

	void SetProgDlgTitle(StrApp str)
	{
		m_strProgDlgTitle = str;
		if (m_qadvi3)
		{
			StrUni stu(str);
			m_qadvi3->put_Title(stu.Bstr());
		}
	}

	void SetProgDlgActionStr(StrApp str)
	{
		if (m_qadvi3)
		{
			StrUni stu(str);
			m_qadvi3->put_Message(stu.Bstr());
		}
	}

	void SetPercentComplete(int nPercent)
	{
		if (m_qadvi3)
			m_qadvi3->put_Position(nPercent);
	}

	IStream * LogFile()
	{
		// Return the log file so the client can write to it.
		return m_qstrmLog;
	}

	StrUni ServerName()
	{
		return m_stuServerName;
	}

	StrUni DbName()
	{
		return m_stuDatabase;
	}

	void BeginTrans();
	void CommitTrans();
	void CreateCommand();

	void SetAntique(bool fAntique = true)
	{
		m_fAntique = fAntique;
	}

protected:
	StrUni m_stuServerName;
	StrUni m_stuDatabase;
	IAdvInd3Ptr m_qadvi3;

	IOleDbEncapPtr m_qodeDb;		// connection to DB passed in from the caller.
	IOleDbEncapPtr m_qode; // Declare before m_qodc.
	IOleDbCommandPtr m_qodc;
	IStreamPtr m_qstrmLog;
	StrAnsi m_staLog;
	StrApp m_strProgDlgTitle;

	// flag that a transaction was opened by the caller
	ComBool m_fTransOpenAlready;
	// Do we want to handle plain text?
	bool m_fPlain;
	// Do we want to work with the formatting information? (For plain text this
	// would be the bare writing system.)
	bool m_fFormatting;
	// Can we process the formatting information directly in the byte buffer? (This is not
	// recommended except where NEEDED for efficiency.  DO NOT USE THIS OPTION UNLESS THE
	// CHANGES ARE GUARANTEED TO LEAVE THE FORMATTING INFORMATION IN CANONICAL FORM!!!)
	bool m_fProcessBytes;
	// Do we want to work with the text itself?
	bool m_fCharacters;
	// True if we want to delay closing the main window.
	bool m_fDontCloseMainWnd;

	// We successfully closed down the application and need to restart when the process
	// finishes.
	bool m_fNeedRelaunch;

	// Do we want a smart progress indicator dialog (but possibly a slower process)?
	// ENHANCE (SharonC): make this an parameter to the string crawler.
	bool m_fExactCount;

	// Flag to use "Enc" instead of "Ws" as column name for SQL queries.
	bool m_fAntique;

	void MergeRuns(ComVector<ITsTextProps> & vpttp, Vector<int> & vich);
	void MergeRuns(Vector<byte> & vbFmt);
};



// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\MkCustomNb.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif // !STRCRAWL_H_INCLUDED
