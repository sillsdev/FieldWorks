/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: HelloWorld.h
Responsibility: Darrell Zook
Last reviewed: never

Description:
	This file contains the base classes for Hello World.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef HELLOWORLD_INCLUDED
#define HELLOWORLD_INCLUDED 1

class HwApp;
class HwMainWnd;
class HwClientWnd;
class HwClientWnd0;
class HwClientWnd1;
class HwClientWnd2;
class HwClientWnd2;
class HwSplitChild;
class HwCaptionBar;
typedef GenSmartPtr<HwApp> HwAppPtr;
typedef GenSmartPtr<HwMainWnd> HwMainWndPtr;
typedef GenSmartPtr<HwClientWnd> HwClientWndPtr;
typedef GenSmartPtr<HwSplitChild> HwSplitChildPtr;
typedef GenSmartPtr<HwCaptionBar> HwCaptionBarPtr;


const int kwidChildBase = 1003;


/*----------------------------------------------------------------------------------------------
	Our Hello World application class.
----------------------------------------------------------------------------------------------*/
class HwApp : public AfApp
{
	typedef AfApp SuperClass;

public:
	HwApp();
	virtual ~HwApp()
		{ } // Do nothing.

	int GetAppNameId()
	{
		return kstidAppName;
	}

protected:
	virtual void Init(void);

	CMD_MAP_DEC(HwApp);
};


/*----------------------------------------------------------------------------------------------
	Our main window frame class.
----------------------------------------------------------------------------------------------*/
class HwMainWnd : public AfRecMainWnd
{
	typedef AfRecMainWnd SuperClass;

public:
	virtual ~HwMainWnd();

	virtual void OnReleasePtr();

	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	virtual void SaveSettings(const achar * pszRoot, bool fRecursive = true);

	enum
	{
		kiViewsList  = 0,
		kiGroup0List = 1,
		kiGroup1List = 2,
		kiListMax,

		kimagView0  = 0,
		kimagView1  = 1,
		kimagView2  = 2,
		kimagView3  = 3,
		kimagGroup0 = 4,
		kimagGroup1 = 5,
	};

	virtual bool OnViewBarChange(int ilist, Set<int> & siselOld, Set<int> & siselNew);
	virtual void RenameAndDeleteStyles(Vector<StrUni> & vstuOldNames,
		Vector<StrUni> & vstuNewNames,
		Vector<StrUni> & vstuDeletedNames)
	{
	}

protected:
	virtual void PostAttach(void);
	virtual void InitMdiClient();

	/*------------------------------------------------------------------------------------------
		Command handlers.
	------------------------------------------------------------------------------------------*/
	virtual bool CmdWndNew(Cmd * pcmd);
	virtual bool CmdWndSplit(Cmd * pcmd);
	virtual bool CmsWndSplit(CmdState & cms);
	virtual bool CmdHelpMode(Cmd * pcmd);
	virtual bool CmdVbToggle(Cmd * pcmd);
	virtual bool CmsVbUpdate(CmdState & cms);
	virtual bool CmdViewExpMenu(Cmd * pcmd);
	virtual bool CmsViewExpMenu(CmdState & cms);

	enum
	{
		kmskShowLargeViewIcons    = 0x0001,
		kmskShowLargeGroup0Icons  = 0x0002,
		kmskShowLargeGroup1Icons  = 0x0004,
		kmskShowViewBar           = 0x0020,

		kmskShowMenuBar           = 0x0001,
		kmskShowStandardToolBar   = 0x0002,
		kmskShowInsertToolBar     = 0x0004,
		kmskShowToolsToolBar      = 0x0008,
		kmskShowWindowToolBar     = 0x0010,
	};

	CMD_MAP_DEC(HwMainWnd);
};


/*----------------------------------------------------------------------------------------------
	Our Hello World client base class.
	Hungarian: hwcw
----------------------------------------------------------------------------------------------*/
class HwClientWnd : public AfClientWnd
{
	typedef AfClientWnd SuperClass;

public:
	virtual ~HwClientWnd()
		{ } // Do nothing.
	virtual void CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew) = 0;
};


/*----------------------------------------------------------------------------------------------
	Our Hello World 0 client class.
	Hungarian: hwcw0
----------------------------------------------------------------------------------------------*/
class HwClientWnd0 : public HwClientWnd
{
	typedef HwClientWnd SuperClass;

public:
	virtual ~HwClientWnd0()
		{ } // Do nothing.
	virtual void CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew);
};


/*----------------------------------------------------------------------------------------------
	Our Hello World 1 client class.
	Hungarian: hwcw1
----------------------------------------------------------------------------------------------*/
class HwClientWnd1 : public HwClientWnd
{
	typedef HwClientWnd SuperClass;

public:
	virtual ~HwClientWnd1()
		{ } // Do nothing.
	virtual void CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew);
};


/*----------------------------------------------------------------------------------------------
	Our Hello World 2 client class.
	Hungarian: hwcw2
----------------------------------------------------------------------------------------------*/
class HwClientWnd2 : public HwClientWnd
{
	typedef HwClientWnd SuperClass;

public:
	virtual ~HwClientWnd2()
		{ } // Do nothing.
	virtual void CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew);
};


/*----------------------------------------------------------------------------------------------
	Our Hello World 3 client class.
	Hungarian: hwcw3
----------------------------------------------------------------------------------------------*/
class HwClientWnd3 : public HwClientWnd
{
	typedef HwClientWnd SuperClass;

public:
	virtual ~HwClientWnd3()
		{ } // Do nothing.
	virtual void CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew);
};


/*----------------------------------------------------------------------------------------------
	Our Hello World view window class.
	Hungarian: hwsc
----------------------------------------------------------------------------------------------*/
class HwSplitChild : public AfSplitChild
{
	typedef AfSplitChild SuperClass;

public:
	HwSplitChild(int itype)
	{
		m_itype = itype;
	}

protected:
	virtual bool OnPaint(HDC hdcDef);

	int m_itype;
};


/*----------------------------------------------------------------------------------------------
	Our caption bar to get multiple icons and context popup menus.
----------------------------------------------------------------------------------------------*/
class HwCaptionBar : public AfCaptionBar
{
	typedef AfCaptionBar SuperClass;

public:
	HwCaptionBar(HwMainWnd * pwndMain)
	{
		AssertPtr(pwndMain);
		m_pwndMain = pwndMain;
	}
	virtual void Create(HWND hwndPar, int wid, HIMAGELIST himl);

protected:
	virtual void ShowContextMenu(int ibtn, Point pt);
	virtual void GetIconName(int ibtn, StrApp & str);

	HwMainWnd * m_pwndMain;
};


#endif // HELLOWORLD_INCLUDED