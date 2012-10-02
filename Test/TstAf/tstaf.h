/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: tstaf.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a basic test for the AfApp core functions.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef TSTAF_INCLUDED
#define TSTAF_INCLUDED 1


const int kwidClient = 1000;


/*----------------------------------------------------------------------------------------------
	Our test application class.
----------------------------------------------------------------------------------------------*/
class TstAf : public AfApp
{
public:
protected:
	virtual void Init(void);
};


/*----------------------------------------------------------------------------------------------
	Our test client class.
----------------------------------------------------------------------------------------------*/
class TstClientWnd : public AfWnd
{
public:
	typedef AfWnd SuperClass;

protected:
	virtual bool OnSize(int wst, int dxs, int dys);
	virtual bool OnPaint(HDC hdc);
};

typedef AfSmartPtr<TstClientWnd> TstClientWndPtr;


/*----------------------------------------------------------------------------------------------
	Our test frame class.
----------------------------------------------------------------------------------------------*/
class TstMainWnd : public AfMainWnd
{
public:
	typedef AfMainWnd SuperClass;

protected:
	TstClientWndPtr m_qtcw;

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void PostAttach(void);

	/*******************************************************************************************
		Message handlers.
	*******************************************************************************************/
	virtual bool OnClientSize(void);

	/*******************************************************************************************
		Command handlers.
	*******************************************************************************************/
	virtual bool CmdFmtBorder(Cmd * pcmd);

	virtual bool OnToolBarsStandard(Cmd * pcmd);
	virtual bool OnToolBarsFormatting(Cmd * pcmd);
	virtual bool OnToolBarsData(Cmd * pcmd);
	virtual bool OnToolBarsView(Cmd * pcmd);
	virtual bool OnToolBarsInsert(Cmd * pcmd);
	virtual bool OnToolBarsTools(Cmd * pcmd);
	virtual bool OnToolBarsWindow(Cmd * pcmd);
	virtual bool OnNotify(int id, NMHDR * pnmh);

	CMD_MAP_DEC(TstMainWnd);
};

typedef AfSmartPtr<TstMainWnd> TstMainWndPtr;


#endif // TSTAF_INCLUDED
