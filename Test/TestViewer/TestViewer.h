/*----------------------------------------------------------------------------------------------
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestViewer.h
Responsibility: Luke Ulrich
Last reviewed: never

Description:
	Core classes for the TestViewer application.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef WPMAIN_INCLUDED
#define WPMAIN_INCLUDED 1



class WpApp;
class WpMainWnd;
class RnStdToolBar;
class RnFmtToolBar;
class WpSplitWnd;
class WpChildWnd;
typedef GenSmartPtr<WpApp> WpAppPtr;
typedef GenSmartPtr<WpMainWnd> WpMainWndPtr;
typedef GenSmartPtr<RnStdToolBar> RnStdToolBarPtr;
typedef GenSmartPtr<RnFmtToolBar> RnFmtToolBarPtr;
typedef GenSmartPtr<WpSplitWnd> WpSplitWndPtr;
typedef GenSmartPtr<WpChildWnd> WpChildWndPtr;

/*----------------------------------------------------------------------------------------------
	Our main TestViewer application class.
----------------------------------------------------------------------------------------------*/
class WpApp : public AfApp
{
public:

	//	Constructor:
	WpApp();

	//	Other public methods:
	virtual void CleanUp();

protected:
	virtual void Init(void);

	virtual void OnIdle();

	//	Command handlers:
	virtual bool CmdFileOpenTest(Cmd * pcmd);
	virtual bool CmdFileDone(Cmd * pcmd);
	virtual bool CmdFileSaveAs(Cmd * pcmd);
	virtual bool CmdFileExit(Cmd * pcmd);
	virtual bool CmdHelpAbout(Cmd * pcmd);
	virtual bool CmdWndCascade(Cmd * pcmd);
	virtual bool CmdWndTileHoriz(Cmd * pcmd);
	virtual bool CmdWndTileVert(Cmd * pcmd);


	CMD_MAP_DEC(WpApp);
};

/*----------------------------------------------------------------------------------------------
	Our main window frame class. This handles the toolbars and status bar. All the action
	is in the embedded WpSplitWnd.
----------------------------------------------------------------------------------------------*/
class WpMainWnd : public AfFrameWnd
{
public:
	typedef AfFrameWnd SuperClass;

	WpMainWnd();
	~WpMainWnd();

	void OnIdle();

	virtual void OnReleasePtr();

	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	virtual void SaveSettings(const achar * pszRoot, bool fRecursive = true);
	void UpdateToolBarIcon(int idToolBar, int idButton, COLORREF clr);
	int ToolIdFromMenuId(int rid);
	bool NewWindow(char * szFileName);

	virtual bool OnSize(int wst, int dxp, int dyp);

	WpSplitWnd * SplitWnd()
	{
		return m_qwsw;
	}
	StrAnsi FileName()
	{
		return m_staFileName;
	}
	void SetFileName(StrAnsi sta)
	{
		m_staFileName = sta;
	}

protected:
	// Member variables
	WpSplitWndPtr m_qwsw;

	StrAnsi m_staFileName;

	enum
	{
		kmskShowMenuBar           = 0x0001,
		kmskShowStandardToolBar   = 0x0002,
		kmskShowFormatToolBar     = 0x0004,
		kmskShowInsertToolBar     = 0x0008,
		kmskShowWindowToolBar     = 0x0010,
	};

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void PostAttach(void);

	HRESULT DispProjName(); // Todo JohnT: can we adapt this to disp doc name?

	//	Message handlers:
	virtual bool OnClientSize(void);
	virtual bool OnContextMenu(HWND hwnd, Point pt);
	virtual bool OnClose();

	// Function to determine whether a menu item is enabled/disabled
	bool ScriptEnable();

	//	Command handlers:
	virtual bool CmdTbToggle(Cmd * pcmd);
	virtual bool CmsTbUpdate(CmdState & cms);
	virtual bool CmdSbToggle(Cmd * pcmd);
	virtual bool CmsSbUpdate(CmdState & cms);
	virtual bool CmdFilePageSetup(Cmd * pcmd);
	virtual bool CmdFmtStyles(Cmd * pcmd);
	virtual bool CmdHelpMode(Cmd * pcmd);
	virtual bool CmdHelpContents(Cmd * pcmd);
	virtual bool CmdWndNewTest(Cmd * pcmd);
	virtual bool CmdWndFileScript(Cmd *pcmd);

	CMD_MAP_DEC(WpMainWnd);
};


/*----------------------------------------------------------------------------------------------
	This forms the main body of the WpMainWnd. It supports a horizontal splitter bar. The
	superclass does most of the work; we just have to know how to create a child window.
----------------------------------------------------------------------------------------------*/
class WpSplitWnd : public AfSplitFrame
{
	typedef AfSplitFrame SuperClass;
public:
	WpSplitWnd();
	virtual void OnReleasePtr();
	virtual void CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew);
	void Init(WpMainWnd * pwmain)
	{
		m_pwmain = pwmain;
	}
	WpMainWnd * MainWnd()
	{
		return m_pwmain;
	}
	WpChildWnd * ChildWnd()
	{
		return m_qwcw;
	}
protected:
	WpMainWnd * m_pwmain;
	WpChildWndPtr m_qwcw;
	// Todo JohnT: we probably need a second one for the other child if split?
};

/*----------------------------------------------------------------------------------------------
	The basic document window for TestViewer, holds a structured text. Embedded in a WpSplitWnd.
	Hungarian: wcw.
----------------------------------------------------------------------------------------------*/
class WpChildWnd : public AfVwSplitWnd
{
public:
	typedef AfVwSplitWnd SuperClass;
	virtual void Init(StrAnsi staFileName);
	virtual void MakeRoot(IVwGraphics * pvg, IVwRootBox ** pprootb);
	bool CommitSelection();

	WpDa * DataAccess()
	{
		return m_qda;
	}
	void ChangeNumberOfStrings(int c)
	{
		CheckHr(m_qrootb->PropChanged(1, kflidStText_Paragraphs, 0, c, 1));
	}
	virtual int OnCreate(CREATESTRUCT * pcs);
	virtual void OnReleasePtr();

	// Allow special access
	friend class TstScript;
	// Override the normal AfVwRootSite Graphics Init function in order to pass custom
	// tailored graphics object that will enable us to log changes and monitor graphical
	// operations
	virtual void InitGraphics();

	// Incore formatting - quickly assemble a string from several various data types
	// Used to collect function arguments and pass them as a string to the TstScriptDlg
	ostringstream outstr;

	// Utility functions
	//
//	VwGraphicsPtr GetVwGraphicsPtr();
	StrAnsi GetRectChar(RECT &rcRect);
	void ShowScriptDialog();

	// Override the various functions here

	// Wrapper functions for VwGraphics functions
									// ID: 6
	void CallOnTyping(VwGraphicsPtr qvg, SmartBstr _bstr, int cchBackspace, int cchDelForward, OLECHAR oleChar,
								RECT rcSrc, RECT rcDst);
//	void testOnChar(int chw);		// ID: 7
	void CallOnSysChar(int chw);	// ID: 8
	void CallOnExtendedKey(int chw, VwShiftStatus ss);	// ID: 9
									// ID: 11
	void CallMouseDown(int xd, int yd, RECT rcSrc, RECT rcDst);
									// ID: 12
	void CallMouseMoveDrag(int xd, int yd, RECT rcSrc, RECT rcDst);
									// ID: 13
	void CallMouseDownExtended(int xd, int yd, RECT rcSrc, RECT rcDst);
									// ID: 14
	void CallMouseUp(int xd, int yd, RECT rcSrc, RECT rcDst);

protected:
	// Special members
	// Pointer to Script dialog
	TstScriptPtr m_qtsd;

	// Hijacked TestBase baselining pointers
	ISilTestSitePtr m_qst;
	SilTestSite *m_psts;
	// Viewclass pointer
	TestVwRoot *m_tvr;

	StVcPtr m_qvc;
	WpDaPtr m_qda;
};
/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/


/*----------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------*/

/*----------------------------------------------------------------------------------------------
	Standard toolbar window class.
	Hungarian: stlbr
----------------------------------------------------------------------------------------------*/
class RnStdToolBar : public AfToolBar
{
	virtual void Create(AfFrameWnd * pwnd);
};


/*----------------------------------------------------------------------------------------------
	Format toolbar window class.
	Hungarian: ftlbr
----------------------------------------------------------------------------------------------*/
class RnFmtToolBar : public AfToolBar
{
	virtual void Create(AfFrameWnd * pwnd);
};

const int kcchEncoding = 8;

#endif // WPMAIN_INCLUDED
