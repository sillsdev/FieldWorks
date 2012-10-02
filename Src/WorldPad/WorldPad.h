/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WorldPad.h
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Core classes for the WorldPad application.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef WPMAIN_INCLUDED
#define WPMAIN_INCLUDED 1


const int kdxpMin = 300; // Minimum width for window.

class WpApp;
class WpMainWnd;
class WpSplitWnd;
class WpChildWnd;
typedef GenSmartPtr<WpApp> WpAppPtr;
typedef GenSmartPtr<WpMainWnd> WpMainWndPtr;
typedef GenSmartPtr<WpSplitWnd> WpSplitWndPtr;
typedef GenSmartPtr<WpChildWnd> WpChildWndPtr;

//:End Ignore

/*----------------------------------------------------------------------------------------------
	Our main WorldPad application class.
----------------------------------------------------------------------------------------------*/
class WpApp : public AfApp
{
public:

	//	Constructor:
	WpApp();

	//	Other public methods:
	virtual void CleanUp();

	int ComplexKeyBehavior(int chw, VwShiftStatus ss);
	void SetGraphiteLoggingForAllEnc(bool f);

	bool LogicalArrow()					{ return m_fLogicalArrow; }
	bool LogicalShiftArrow()			{ return m_fLogicalShiftArrow; }
	bool LogicalHomeEnd()				{ return m_fLogicalHomeEnd; }
	bool GraphiteLogging()				{ return m_fGraphiteLog; }

	void SetLogicalArrow(bool f)		{ m_fLogicalArrow = f; }
	void SetLogicalShiftArrow(bool f)	{ m_fLogicalShiftArrow = f; }
	void SetLogicalHomeEnd(bool f)		{ m_fLogicalHomeEnd = f; }
	void SetGraphiteLogging(bool f)		{ m_fGraphiteLog = f; }

	static bool FindDefaultTemplate(StrAnsi * pstaDefaultTemplate);

	virtual bool OnStyleNameChange(IVwStylesheet * psts, ISilDataAccess * psda);

	virtual void NewMainWnd(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
		int hvoMainObj, int encUi, int nTool, int nParam, DWORD dwRegister);

	virtual const CLSID * GetAppClsid()
	{
		return &CLSID_WorldPad;
	}

	static WpApp * MainApp();

	ILgWritingSystemFactory * GetWsFactory()
	{
		return m_qwsf;
	}
	void SetWsFactory(ILgWritingSystemFactory * pwsf)
	{
#ifdef DEBUG // Don't break release build.
		if (m_qwsf)
			Assert(pwsf == m_qwsf);
#endif
		m_qwsf = pwsf;
	}
	void ClearWsFactory()
	{
		m_qwsf.Clear();
	}

	bool Vertical()
	{
		return m_fVertical;
	}

protected:
	//	instance variables:
	// Note: currently the user interface only allows the capability of setting all the
	// following three options to the same value:
	bool m_fLogicalArrow;			// true if arrow keys move logically
	bool m_fLogicalShiftArrow;
	bool m_fLogicalHomeEnd;
	bool m_fGraphiteLog;		// output log of Graphite transduction for debugging
	bool m_fVertical;

	ILgWritingSystemFactoryPtr m_qwsf; // to make sure there is ever only one!

	virtual void Init(void);

	virtual void OnIdle();

	virtual const achar * GetHelpFile()
	{
		return AfApp::GetHelpFilename().Chars();
	}
	//virtual char * GetHelpFile()
	//{
	//	return "WorldPadUserHelp.chm";
	//}

	//	Command handlers:
	virtual bool CmdFileOpen(Cmd * pcmd);
	virtual bool CmdFileSave(Cmd * pcmd);
	virtual bool CmdFileSaveAs(Cmd * pcmd);
	virtual bool CmdHelpAbout(Cmd * pcmd);

	virtual bool CmsFileSave(CmdState & cms);

	void RunInstall();
	void RunUninstall();
	void DeleteAllEncodings();

	CMD_MAP_DEC(WpApp);
};

/*----------------------------------------------------------------------------------------------
	Our main window frame class. This handles the toolbars and status bar. All the action
	is in the embedded WpSplitWnd.
----------------------------------------------------------------------------------------------*/
class WpMainWnd : public AfMainWnd
{
public:
	typedef AfMainWnd SuperClass;

	WpMainWnd();
	~WpMainWnd();

	virtual void OnReleasePtr();

	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);

	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	virtual void SaveSettings(const achar * pszRoot, bool fRecursive = true);
	bool DoOpenFile(char * szFile, int nFilterIndex);
	bool NewWindow(const char * szFileName, int fiet);
	bool CmdToolsOpts(Cmd * pcmd);
	bool CmdToolsWritingSysProps(Cmd * pcmd);
	void ActivateDefaultKeyboard();
	void RestoreCurrentKeyboard();
	bool AddFileExt(StrApp & strFileName, int fiet);
	static int GuessFileType(StrApp strFileName);
	StrApp RemoveStdExt(StrApp strFileName);
	void ReplaceWithEmptyWindow();
	void SetStatusBarText()
	{
//		m_qstbr->StoreHelpText("    ");
//		m_qstbr->DisplayHelpText();
		if (m_qstbr)
			::ShowWindow(m_qstbr->Hwnd(), SW_HIDE);
	}

	virtual bool CmdWndClose(Cmd *pcmd);
	virtual bool CmsWndClose(CmdState & cms);

	virtual bool CmdWndSplit(Cmd *pcmd);
	virtual bool CmsWndSplit(CmdState & cms);
	virtual void FixMenu(HMENU hmenu);

	virtual bool OnSize(int wst, int dxp, int dyp);
	virtual void OnToolBarButtonAdded(AfToolBar * ptlbr, int ibtn, int cid);
	virtual int GetMinHeight()
	{
		return kdypMin;
	}
	virtual int GetMinWidth()
	{
		return kdxpMin;
	}
	HWND Hwnd()
	{
		return m_hwnd;
	}

	virtual void UpdateToolBarWrtSysControl();
	void UpdateFontControlForWs();

	long OnDropFiles(HDROP hDropInfo, BOOL fContext);

	virtual void RenameAndDeleteStyles(Vector<StrUni> & vstuOldNames,
		Vector<StrUni> & vstuNewNames, Vector<StrUni> & vstuDelNames);

	WpSplitWnd * SplitWnd()
	{
		return m_qwsw;
	}

	WpDa * DataAccess();

	virtual AfStylesheet * GetStylesheet()
	{
		return m_qwpsts;
	}

	StrAnsi FileName()
	{
		return m_staFileName;
	}
	void SetFileName(const char * pszFile, int fiet, StrApp * pstaWindowTitle, bool fSaving);
	void AdjustNameOfPartialFile();

	int FileType()
	{
		return m_fiet;
	}
	void SetFileType(int fiet)
	{
		m_fiet = fiet;
	}

	void TempSetFileName(const char * pszFile)
	{
		m_staFileName = pszFile;
	}

	bool AutoDefault()
	{
		return m_fAutoDefault;
	}
	void SetAutoDefault(bool f)
	{
		m_fAutoDefault = f;
	}

	bool FileSave();
	bool FileSaveAs();

	void SetLauncherWindow(WpMainWnd * pwpwnd)
	{
		m_qwpwndLauncher = pwpwnd;
	}
	WpMainWnd * LauncherWindow()
	{
		return m_qwpwndLauncher;
	}

	virtual void GetLgWritingSystemFactory(ILgWritingSystemFactory ** ppwsf);
	virtual int UserWs();

	//	For importing and exporting the page setup information in an XML file:
	int TopMargin()								{ return m_dympTopMargin; }
	int BottomMargin()							{ return m_dympBottomMargin; }
	int LeftMargin()							{ return m_dxmpLeftMargin; }
	int RightMargin()							{ return m_dxmpRightMargin; }
	int HeaderMargin()							{ return m_dympHeaderMargin; }
	int FooterMargin()							{ return m_dympFooterMargin; }
	int PageSize()								{ return (int)m_sPgSize; }
	int PageHeight()							{ return m_dympPageHeight; }
	int PageWidth()								{ return m_dxmpPageWidth; }
	int PageOrientation()						{ return (int)m_nOrient; }
	ITsString * PageHeader()					{ return m_qtssHeader; }
	ITsString * PageFooter()					{ return m_qtssFooter; }

	void SetTopMargin(int dymp)					{ m_dympTopMargin = dymp; }
	void SetBottomMargin(int dymp)				{ m_dympBottomMargin = dymp; }
	void SetLeftMargin(int dxmp)				{ m_dxmpLeftMargin = dxmp; }
	void SetRightMargin(int dxmp)				{ m_dxmpRightMargin = dxmp; }
	void SetHeaderMargin(int dymp)				{ m_dympHeaderMargin = dymp; }
	void SetFooterMargin(int dymp)				{ m_dympFooterMargin = dymp; }
	void SetPageSize(int s)						{ m_sPgSize = (PgSizeType)s; }
	void SetPageHeight(int dymp)				{ m_dympPageHeight = dymp; }
	void SetPageWidth(int dxmp)					{ m_dxmpPageWidth = dxmp; }
	void SetPageOrientation(int n)				{ m_nOrient = (POrientType)n; }
	void SetPageHeader(ITsString * ptss)		{ m_qtssHeader = ptss; }
	void SetPageFooter(ITsString * ptss)		{ m_qtssFooter = ptss; }

protected:
	// Member variables
	WpSplitWndPtr m_qwsw;

	WpStylesheetPtr m_qwpsts;

	StrAnsi m_staFileName;
	int m_fiet;	// file-type
	bool m_fAutoDefault;	// was this doc was automatically initialized from the default template

	WpMainWndPtr m_qwpwndLauncher;	// window from which this window was launched; this is used
									// for making file-load error messages properly modal

	StrUni m_stuXsltTmp;	// temporary file for XSLT processing
	SmartBstr m_sbstrXsltErr;
	long m_nErrorCode;
	long m_nErrorLine;

	int m_wsUser;		// user interface writing system id.

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void PostAttach(void);

	virtual IActionHandler * GetActionHandler()
	{
		if (!DataAccess())
			return NULL;
		IActionHandlerPtr qacth;
		DataAccess()->GetActionHandler(&qacth);
		return qacth;
	}

	virtual void LoadDefaultToolbarFlags(Vector<DWORD> & vflag, DWORD & dwBarFlags);

	void FindXslTransforms(Vector<AfExportStyleSheet> & vess);
	bool RunXslTransform(Vector<AfExportStyleSheet> & vess, int iess, StrApp strFileOut);
	void LoadDOM(IXMLDOMDocument * pDOM, BSTR bstrFile);
	void ProcessXsl(StrUni & stuInput, StrUni & stuStylesheet, StrUni & stuOutput, int iXsl);

	//	Message handlers:
	virtual bool OnClientSize(void);
	virtual bool OnClose();

	bool OfferToSave();

	//	Command handlers:
	virtual bool CmdHelpMode(Cmd * pcmd);

	bool m_fDirty;
	virtual bool CmdFilePageSetup(Cmd * pcmd);

	virtual bool CmdHelpFw(Cmd * pcmd)
	{
		// c:\\fw\\DistFiles\\Helps\FieldWorksSuite.chm
		return AfApp::Papp()->ShowTrainingFile(_T("\\Helps\\FieldWorksSuite.chm"));
	}
	virtual bool CmdHelpApp(Cmd * pcmd)
	{
		return AfApp::Papp()->ShowHelpFile();
	}
//	virtual bool CmdTutorial(Cmd * pcmd)
//	{
//		// c:\\fw\\DistFiles\\WorldPad\\Tutorials\\WorldPad Tutorials.pdf
//		char * pFilespec = "\\WorldPad\\Tutorials\\WorldPad Tutorials.pdf";
//
//		return AfApp::Papp()->ShowTrainingFile(pFilespec);
//	}

	virtual bool CmdWndNew(Cmd * pcmd);

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
	WpSplitWnd(bool fScrollHoriz = false);

	virtual void OnReleasePtr();
	virtual void CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew);
	void Init(WpMainWnd * pwpwnd)
	{
		m_qwpwnd = pwpwnd;
	}
	WpMainWnd * MainWnd()
	{
		return m_qwpwnd;
	}
	WpChildWnd * ChildWnd();

	WpDa * DataAccess();

protected:
	WpMainWndPtr m_qwpwnd;
};

/*----------------------------------------------------------------------------------------------
	The basic document window for WorldPad, holds a structured text. Embedded in a WpSplitWnd.
	Hungarian: wcw.
----------------------------------------------------------------------------------------------*/
class WpChildWnd : public AfVwSplitChild // subclass of AfVwRootSite
{
public:
	WpChildWnd(bool fScrollHoriz = false, bool fVerticalOrientation = false) : AfVwSplitChild(fScrollHoriz, fVerticalOrientation)
	{
	}
	virtual void OnReleasePtr();

	typedef AfVwSplitChild SuperClass;

	virtual void Init(StrAnsi staFileName, int * pft, WpMainWnd * pwpwnd);

	// IDropTarget methods
	STDMETHOD(DragEnter)(IDataObject * pdo, DWORD grfKeyState, POINTL ptl, DWORD * pdwEffect);
	STDMETHOD(DragLeave)();
	STDMETHOD(DragOver)(DWORD grfKeyState, POINTL ptl, DWORD * pdwEffect);
	STDMETHOD(Drop)(IDataObject * pdo, DWORD grfKeyState, POINTL ptl, DWORD * pdwEffect);

	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb, bool fPrintingSel);
	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb)
	{
		MakeRoot(pvg, pwsf, pprootb, false);
	}
	void RestoreCurrentKeyboard();
	//virtual bool UpdateParaDirection(IVwSelection * pvwselNew);

	bool CommitSelection();

	//bool Relayout();

	int GetHorizMargin()
	{
		return 6;	// white space at left and right edges of window
	}
	// Allow printing only the selection.
	virtual bool CanPrintOnlySelection()
	{
	// UNCOMMENT THIS CODE TO ENABLE TRYING TO PRINT SELECTIONS.
	//	// We need to verify that the selection exists, and that it is a range, not merely an
	//	// insertion point.
	//	IVwSelectionPtr qsel;
	//	CheckHr(m_qrootb->get_Selection(&qsel));
	//	if (qsel)
	//	{
	//		ComBool fIsRange;
	//		CheckHr(qsel->get_IsRange(&fIsRange));
	//		if (fIsRange)
	//			return true;
	//	}
		return false;
	}
	virtual void CacheSelectedObjects()
	{
		// No such thing as "Objects" in WorldPad, so nothing to do.
	}

	StVc * ViewConstructor()
	{
		return m_qvc;
	}

	WpDa * DataAccess()
	{
		return m_qda;
	}

	WpMainWnd * MainWnd()
	{
		if (!m_qwpwnd)
		{
			// Bottom half of split window: ask the split frame.
			WpSplitWndPtr qwpsplf = dynamic_cast<WpSplitWnd *>(m_qsplf.Ptr());
			Assert(qwpsplf);
			return qwpsplf->MainWnd();
		}
		return m_qwpwnd;
	}

	void SetMainWnd(WpMainWnd * pwpwnd)
	{
		m_qwpwnd = pwpwnd;
	}

	void UpdateView(int c)
	{
		CheckHr(m_qrootb->Reconstruct());
	}

	virtual bool OuterRightToLeft()
	{
		int nRtl = m_qda->DocRightToLeft();
		return (bool)nRtl;
	}

	virtual int OnCreate(CREATESTRUCT * pcs);

//	bool CmsFmtStyle(CmdState & cms)
//	{
//		cms.Enable(false);
//		return true;
//	}

	bool CmdFmtDoc(Cmd * pcmd);

	virtual bool CmdEditUndo(Cmd * pcmd);
	virtual bool CmdEditRedo(Cmd * pcmd);
	virtual bool CmsEditUndo(CmdState & cms);
	virtual bool CmsEditRedo(CmdState & cms);

	bool CmsEditSelAll1(CmdState & cms)
	{
		bool fEmpty = m_qda->IsEmpty();
		cms.Enable(!fEmpty);
		return true;
	}
	bool CmdEditSelAll1(Cmd * pcmd);

	void MakeInitialSel();

	void ReconstructAndReplaceSel();

	void GetCurrentFontChoices(Vector<StrApp> & vstrFonts);
	virtual void CopyRootTo(AfVwSplitChild * pavscOther);
	void GetWsOfSelection(int * pws);
	void GetWsOfSelection(IVwSelection * pvwsel, int * pws);
	void GetPropsOfSelection(ITsTextProps ** ppttpRet);

	virtual int ComplexKeyBehavior(int chw, VwShiftStatus ss)
	{
		AfApp * papp = AfApp::Papp();
		WpApp * wpapp = dynamic_cast<WpApp *>(papp);
		if (wpapp)
			return wpapp->ComplexKeyBehavior(chw, ss);
		else
			return 0;
	}

	void GetWsFactory(ILgWritingSystemFactory ** ppwsf)
	{
		GetLgWritingSystemFactory(ppwsf);
	}

protected:
	StVcPtr m_qvc;
	WpDaPtr m_qda;
	WpMainWndPtr m_qwpwnd;
	// for drag/drop:
	bool m_fDropFile;
	DWORD m_dwEffect;
	DWORD m_grfKeyState;
	HKL m_hklCurr;

	CMD_MAP_DEC(WpChildWnd);

	bool AutoKeyboardSupport();

	virtual bool OpenFormatStylesDialog(HWND hwnd, bool fCanDoRtl, bool fOuterRtl,
		IVwStylesheet * past, TtpVec & vqttpPara, TtpVec & vqttpChar, bool fCanFormatChar,
		StrUni * pstuStyleName, bool & fStylesChanged, bool & fApply, bool & fReloadDb);

	virtual bool AllowFontFeatures()
	{
		return true;
	}

	virtual bool OneDefaultFont()
	{
		return true;
	}
};


/*----------------------------------------------------------------------------------------------
	Save Plain Text dialog - gives them the option to save UTF-16, UTF-8, or ANSI.
	Also used for reading a plain text file when there are no byte-order marks.
----------------------------------------------------------------------------------------------*/
class WpSavePlainTextDlg;
typedef GenSmartPtr<WpSavePlainTextDlg> WpSavePlainTextDlgPtr;

class WpSavePlainTextDlg : public AfDialog
{
	typedef AfDialog SuperClass;

public:
	WpSavePlainTextDlg()
	{
		m_rid = kridSavePlainTextDlg;
		m_pszHelpUrl = _T("User_Interface\\Menus\\File\\Saving_as_Plain_Text.htm");
		m_fSaving = true;
		m_fUtf16 = true;
	}
	void Init(bool fSaving, bool fUtf16)
	{
		m_fSaving = fSaving;
		if (m_fSaving)
			m_pszHelpUrl = _T("User_Interface\\Menus\\File\\Saving_as_Plain_Text.htm");
		else
			m_pszHelpUrl = _T("User_Interface\\Menus\\File\\Opening_as_Plain_Text.htm");
		m_fUtf16 = fUtf16;
	}

protected:

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);

	virtual bool OnApply(bool fClose)
	{
		return SuperClass::OnApply(fClose);
	}

	virtual bool OnCancel()
	{
		return SuperClass::OnCancel();
	}

	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);

public:
	int SelectedEncoding()
	{
		return m_iSel;
	}

protected:
	bool m_fSaving;
	bool m_fUtf16;	// do we want to offer it?
	int m_cTextEnc;
	int m_iSel;
};


#endif // WPMAIN_INCLUDED
