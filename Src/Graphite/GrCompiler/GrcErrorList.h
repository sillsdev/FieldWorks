/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrcErrorList.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Classes to implement the list of errors accumulated during the post-parser and pre-compiler.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef ERRORS_INCLUDED
#define ERRORS_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: GrcErrorItem
Description: An error generated during the post-parser or pre-compiler.
Hungarian: err
----------------------------------------------------------------------------------------------*/

class GrcErrorItem
{
	friend class GrcErrorList;

public:
	GrcErrorItem(bool fFatal, int nID, GrpLineAndFile & lnf, StrAnsi staMsg, GdlObject * pgdlObj)
		:	m_fFatal(fFatal),
			m_nID(nID),
			m_fMsgIncludesFatality(false),
			m_lnf(lnf),
			m_staMsg(staMsg),
			m_pgdlObject(pgdlObj)
	{
	}

	bool Equivalent(GrcErrorItem * perr)
	{
		return ( //// m_pgdlObject == perr->m_pgdlObject &&
			m_nID == perr->m_nID
			&& m_staMsg == perr->m_staMsg
			&& m_lnf == perr->m_lnf
			&& m_fFatal == perr->m_fFatal);
	}

	int PreProcessedLine()
	{
		return m_lnf.PreProcessedLine();
	}

protected:
	//	instance variables:
	int				m_nID;
	GdlObject *		m_pgdlObject;
	StrAnsi			m_staMsg;
	bool			m_fFatal;
	bool			m_fMsgIncludesFatality;		// don't add label "warning" or "error--the
												// message itself includes the information
	GrpLineAndFile	m_lnf;
};

/*----------------------------------------------------------------------------------------------
Class: GrcErrorList
Description: Database of errors accumulated during post-parser and pre-compiler. There is only
	a single instance of this class.
Hungarian:
----------------------------------------------------------------------------------------------*/

class GrcErrorList
{
	friend class GrcErrorItem;

public:
	GrcErrorList()
	{
		m_fFatalError = false;
	}

	~GrcErrorList()
	{
		for (int i = 0; i < m_vperr.Size(); ++i)
			delete m_vperr[i];
	}

	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, GrpLineAndFile const& lnf)
	{
		AddItem(true, nID, pgdlobj, &lnf, staMsg);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, GrpLineAndFile const& lnf)
	{
		AddItem(true, nID, pgdlobj, &lnf, &staMsg, &sta2, NULL, NULL, NULL, NULL, NULL, NULL);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3,
		GrpLineAndFile const& lnf)
	{
		AddItem(true, nID, pgdlobj, &lnf, &staMsg, &sta2, &sta3, NULL, NULL, NULL, NULL, NULL);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3, StrAnsi sta4,
		GrpLineAndFile const& lnf)
	{
		AddItem(true, nID, pgdlobj, &lnf, &staMsg, &sta2, &sta3, &sta4, NULL, NULL, NULL, NULL);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3, StrAnsi sta4,
		StrAnsi sta5,
		GrpLineAndFile const& lnf)
	{
		AddItem(true, nID, pgdlobj, &lnf, &staMsg, &sta2, &sta3, &sta4, &sta5, NULL, NULL, NULL);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3, StrAnsi sta4,
		StrAnsi sta5, StrAnsi sta6,
		GrpLineAndFile const& lnf)
	{
		AddItem(true, nID, pgdlobj, &lnf, &staMsg, &sta2, &sta3, &sta4, &sta5, &sta6, NULL, NULL);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3, StrAnsi sta4,
		StrAnsi sta5, StrAnsi sta6, StrAnsi sta7,
		GrpLineAndFile const& lnf)
	{
		AddItem(true, nID, pgdlobj, &lnf, &staMsg, &sta2, &sta3, &sta4, &sta5, &sta6, &sta7, NULL);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3, StrAnsi sta4,
		StrAnsi sta5, StrAnsi sta6, StrAnsi sta7, StrAnsi sta8,
		GrpLineAndFile const& lnf)
	{
		AddItem(true, nID, pgdlobj, &lnf, &staMsg, &sta2, &sta3, &sta4, &sta5, &sta6, &sta7, &sta8);
	}


	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg)
	{
		AddItem(true, nID, pgdlobj, NULL, staMsg);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2)
	{
		AddItem(true, nID, pgdlobj, NULL, &staMsg, &sta2, NULL, NULL, NULL, NULL, NULL, NULL);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3)
	{
		AddItem(true, nID, pgdlobj, NULL, &staMsg, &sta2, &sta3, NULL, NULL, NULL, NULL, NULL);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3, StrAnsi sta4)
	{
		AddItem(true, nID, pgdlobj, NULL, &staMsg, &sta2, &sta3, &sta4, NULL, NULL, NULL, NULL);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3, StrAnsi sta4,
		StrAnsi sta5)
	{
		AddItem(true, nID, pgdlobj, NULL, &staMsg, &sta2, &sta3, &sta4, &sta5, NULL, NULL, NULL);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3, StrAnsi sta4,
		StrAnsi sta5, StrAnsi sta6)
	{
		AddItem(true, nID, pgdlobj, NULL, &staMsg, &sta2, &sta3, &sta4, &sta5, &sta6, NULL, NULL);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3, StrAnsi sta4,
		StrAnsi sta5, StrAnsi sta6, StrAnsi sta7)
	{
		AddItem(true, nID, pgdlobj, NULL, &staMsg, &sta2, &sta3, &sta4, &sta5, &sta6, &sta7, NULL);
	}
	void AddError(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3, StrAnsi sta4,
		StrAnsi sta5, StrAnsi sta6, StrAnsi sta7, StrAnsi sta8)
	{
		AddItem(true, nID, pgdlobj, NULL, &staMsg, &sta2, &sta3, &sta4, &sta5, &sta6, &sta7, &sta8);
	}


	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, GrpLineAndFile const& lnf)
	{
		AddItem(false,nID,  pgdlobj, &lnf, staMsg);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, GrpLineAndFile const& lnf)
	{
		AddItem(false, nID, pgdlobj, &lnf, &staMsg, &sta2, NULL, NULL, NULL, NULL, NULL, NULL);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3,
		GrpLineAndFile const& lnf)
	{
		AddItem(false, nID, pgdlobj, &lnf, &staMsg, &sta2, &sta3, NULL, NULL, NULL, NULL, NULL);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3,
		StrAnsi sta4,
		GrpLineAndFile const& lnf)
	{
		AddItem(false, nID, pgdlobj, &lnf, &staMsg, &sta2, &sta3, &sta4, NULL, NULL, NULL, NULL);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3,
		StrAnsi sta4, StrAnsi sta5,
		GrpLineAndFile const& lnf)
	{
		AddItem(false, nID, pgdlobj, &lnf, &staMsg, &sta2, &sta3, &sta4, &sta5, NULL, NULL, NULL);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3,
		StrAnsi sta4, StrAnsi sta5, StrAnsi sta6,
		GrpLineAndFile const& lnf)
	{
		AddItem(false, nID, pgdlobj, &lnf, &staMsg, &sta2, &sta3, &sta4, &sta5, &sta6, NULL, NULL);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3,
		StrAnsi sta4, StrAnsi sta5, StrAnsi sta6, StrAnsi sta7,
		GrpLineAndFile const& lnf)
	{
		AddItem(false, nID, pgdlobj, &lnf, &staMsg, &sta2, &sta3, &sta4, &sta5, &sta6, &sta7, NULL);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3,
		StrAnsi sta4, StrAnsi sta5, StrAnsi sta6, StrAnsi sta7, StrAnsi sta8,
		GrpLineAndFile const& lnf)
	{
		AddItem(false, nID, pgdlobj, &lnf, &staMsg, &sta2, &sta3, &sta4, &sta5, &sta6, &sta7, &sta8);
	}


	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg)
	{
		AddItem(false, nID, pgdlobj, NULL, staMsg);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2)
	{
		AddItem(false, nID, pgdlobj, NULL, &staMsg, &sta2, NULL, NULL, NULL, NULL, NULL, NULL);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3)
	{
		AddItem(false, nID, pgdlobj, NULL, &staMsg, &sta2, &sta3, NULL, NULL, NULL, NULL, NULL);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3,
		StrAnsi sta4)
	{
		AddItem(false, nID, pgdlobj, NULL, &staMsg, &sta2, &sta3, &sta4, NULL, NULL, NULL, NULL);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3,
		StrAnsi sta4, StrAnsi sta5)
	{
		AddItem(false, nID, pgdlobj, NULL, &staMsg, &sta2, &sta3, &sta4, &sta5, NULL, NULL, NULL);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3,
		StrAnsi sta4, StrAnsi sta5, StrAnsi sta6)
	{
		AddItem(false, nID, pgdlobj, NULL, &staMsg, &sta2, &sta3, &sta4, &sta5, &sta6, NULL, NULL);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3,
		StrAnsi sta4, StrAnsi sta5, StrAnsi sta6, StrAnsi sta7)
	{
		AddItem(false, nID, pgdlobj, NULL, &staMsg, &sta2, &sta3, &sta4, &sta5, &sta6, &sta7, NULL);
	}
	void AddWarning(int nID, GdlObject * pgdlobj, StrAnsi staMsg, StrAnsi sta2, StrAnsi sta3,
		StrAnsi sta4, StrAnsi sta5, StrAnsi sta6, StrAnsi sta7, StrAnsi sta8)
	{
		AddItem(false, nID, pgdlobj, NULL, &staMsg, &sta2, &sta3, &sta4, &sta5, &sta6, &sta7, &sta8);
	}


	void AddItem(bool fFatal, int nID, GdlObject * pgdlobj, const GrpLineAndFile *, StrAnsi staMsg);
	void AddItem(bool fFatal, int nID, GdlObject * pgdlobj, const GrpLineAndFile *,
		StrAnsi * psta1, StrAnsi * psta2, StrAnsi * psta3, StrAnsi * psta4,
		StrAnsi * psta5, StrAnsi * psta6, StrAnsi * psta7, StrAnsi * psta8);

	void SortErrors();
	int NumberOfErrors()
	{
		return m_vperr.Size();
	}
	int ErrorsAtLine(int nLine);
	int ErrorsAtLine(int nLine, int * piperrFirst);

	bool AnyFatalErrors()
	{
		return m_fFatalError;
	}

	int NumberOfWarnings();
	int NumberOfWarningsGiven();

	bool IsFatal(int iperr)
	{
		return m_vperr[iperr]->m_fFatal;
	}

	void SetIgnoreWarning(int nID, bool f = true);
	bool IgnoreWarning(int nID);

	void SetLastMsgIncludesFatality(bool f)
	{
		(*m_vperr.Top())->m_fMsgIncludesFatality = f;
	}

	void WriteErrorsToFile(StrAnsi staGdlFile, StrAnsi staInputFontFile,
		StrAnsi staOutputFile, StrAnsi staOutputFamily, StrAnsi staVersion, int fSepCtrlFile)
	{
		WriteErrorsToFile("gdlerr.txt", staGdlFile, staInputFontFile, staOutputFile, staOutputFamily,
			staVersion, fSepCtrlFile);
	}
	void WriteErrorsToFile(StrAnsi staErrFile, StrAnsi staGdlFile, StrAnsi staInputFontFile,
		StrAnsi staOutputFile, StrAnsi staOutputFamily,
		StrAnsi staVersion, bool fSepCtrlFile);
	void WriteErrorsToStream(std::ostream&, StrAnsi staGdlFile, StrAnsi staInputFontFile,
		StrAnsi staOutputFile, StrAnsi staOutputFamily,
		StrAnsi staVersion, bool fSepCtrlFile);

protected:
	//	instance variables:
	bool m_fFatalError;

	Vector<GrcErrorItem *> m_vperr;

	Vector<int> m_vnIgnoreWarnings;

public:
	//	For test procedures:
	bool test_ErrorIs(int iperr, StrAnsi staMsg)
	{
		return (m_vperr[iperr]->m_staMsg == staMsg);
	}

	void test_Clear()
	{
		for (int i = 0; i < m_vperr.Size(); ++i)
		{
			delete m_vperr[i];
		}
		m_vperr.Clear();
		m_fFatalError = false;
	}
};


void AddGlobalError(bool fFatal, int nID, std::string msg, int nLine);
void AddGlobalError(bool fFatal, int nID, std::string msg, GrpLineAndFile const&);


#endif // ERRORS_INCLUDED
