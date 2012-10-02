/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2000, SIL International. All rights reserved.

File: Cle.h
Responsibility: Rand Burgett
Last reviewed: never

Description:
	Provides the main functions of the Choices List Editor
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef CLE_INCLUDED
#define CLE_INCLUDED 1

#include "FwAppVersion.h"

class CleApp;
class CleMainWnd;
class CleBrowseWnd;
class CleTreeBar;
class CleOverlayListBar;
class CleTagOverlayTool;
class CleChangeWatcher;
class CleListBar;
class CleCaptionBar;
class CleFilterNoMatchDlg;
class CleCustDocVc;
typedef GenSmartPtr<CleApp> CleAppPtr;
typedef GenSmartPtr<CleMainWnd> CleMainWndPtr;
typedef GenSmartPtr<CleTreeBar> CleTreeBarPtr;
typedef GenSmartPtr<CleOverlayListBar> CleOverlayListBarPtr;
typedef GenSmartPtr<CleTagOverlayTool> CleTagOverlayToolPtr;
typedef GenSmartPtr<CleChangeWatcher> CleChangeWatcherPtr;
typedef GenSmartPtr<CleListBar> CleListBarPtr;
typedef GenSmartPtr<CleCaptionBar> CleCaptionBarPtr;
typedef GenSmartPtr<CleFilterNoMatchDlg> CleFilterNoMatchDlgPtr;
typedef GenSmartPtr<CleCustDocVc> ClsCustDocVcPtr;


/*----------------------------------------------------------------------------------------------
	This class provides the user with a dialog that gives them a choice of turning off all
	filters, modifying the current filter, or selecting a new filter to use. It gets shown
	when the user has selected a filter that results in no records being shown.

	@h3{Hungarian: fltnm}
----------------------------------------------------------------------------------------------*/
class CleFilterNoMatchDlg : public FwFilterNoMatchDlg
{
protected:
	virtual void GetTlsOptDlg(TlsOptDlg ** pptod);
	virtual void SelectNewMenu(HMENU hmenuPopup)
	{
		::AppendMenu(hmenuPopup, MF_STRING, kcidExpFilters, NULL);
	}
};

/*----------------------------------------------------------------------------------------------
	This class exists to receive notifications of changes to properties

	@h3{Hungarian: clcngw}
----------------------------------------------------------------------------------------------*/
class CleChangeWatcher : public IVwNotifyChange
{
public:
	CleChangeWatcher(ISilDataAccess * psda);
	~CleChangeWatcher();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// IVwNotifyChange methods

	STDMETHOD(PropChanged)(HVO vwobj, int tag, int ivMin, int cvIns, int cvDel);
protected:
	ISilDataAccessPtr m_qsda;
	long m_cref;
};

/*----------------------------------------------------------------------------------------------
	This class is overridden to allow us to create a CleLpInfo structure.

	@h3{Hungarian: dbi}
----------------------------------------------------------------------------------------------*/
class CleDbInfo : public AfDbInfo
{
public:
	CleDbInfo();

	virtual AfLpInfo * GetLpInfo(HVO hvoLp);
	virtual void SaveAllData()
	{
		AfMainWnd * pafw = AfApp::Papp()->GetCurMainWnd();
		AssertPtr(pafw);
		pafw->SaveData();
	}
	virtual void LoadSortMethods()
	{
		SortMethodUtil::LoadSortMethods(reinterpret_cast<AfDbInfo *>(this),
			&CLSID_ChoicesListEditor);
	}
	virtual void LoadFilters()
	{
		FilterUtil::LoadFilters(reinterpret_cast<AfDbInfo *>(this), &CLSID_ChoicesListEditor);
	}
	virtual HRESULT GetLogPointer(IStream ** ppfist)
	{
		return AfApp::Papp()->GetLogPointer(ppfist);
	}
	virtual void CompleteBrowseRecordSpec(UserViewSpec * puvs);
};

typedef GenSmartPtr<CleDbInfo> CleDbInfoPtr;


/*----------------------------------------------------------------------------------------------
	This class contains Choices List Editor information about a language project.

	@h3{Hungarian: clpi}
----------------------------------------------------------------------------------------------*/
class CleLpInfo : public AfLpInfo
{
public:
	CleLpInfo();

	virtual bool OpenProject();
	virtual bool LoadProjBasics();
	virtual bool StoreAndSync(SyncInfo & sync);

//	HVO GetCleId()
//	{
//		return m_hvoRn;
//	}

	enum
	{
		kpidPsslCon = 0,
		kpidPsslRes,
		kpidPsslPeo,
		kpidPsslLoc,
		kpidPsslAna,
		kpidPsslEdu,
		kpidPsslPsn,
		kpidPsslLim,
	};

protected:
};

typedef GenSmartPtr<CleLpInfo> CleLpInfoPtr;


/*----------------------------------------------------------------------------------------------
	The Choices List Editor's application class.

	@h3{Hungarian: app}
----------------------------------------------------------------------------------------------*/
class CleApp : public AfDbApp
{
	typedef AfDbApp SuperClass;

public:

	//:>****************************************************************************************
	//:>	public methods.
	//:>****************************************************************************************
	CleApp();

	bool GetAppVer(int & nAppVer, int & nErlyVer, int & nLastVer);
	int GetAppNameId();
	int GetAppPropNameId();
	virtual int GetAppIconId();
	STDMETHOD(GetDefaultBackupDirectory)(BSTR * pbstrDefBackupDir);

	virtual void NewMainWnd(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
		int hvoMainObj, int encUi, int nTool, int nParam, DWORD dwRegister);
	virtual void NewMainWndWithSel(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
		int hvoMainObj, int encUi, int nTool, int nParam, const HVO * prghvo, int chvo,
		const int * prgflid, int cflid, int ichCur, int nView, DWORD dwRegister);
	virtual const CLSID * GetAppClsid()
	{
		return &CLSID_ChoicesListEditor;
	}

	virtual AfDbInfo * GetDbInfo(const OLECHAR * pszDbName, const OLECHAR * pszSvrName);

	virtual const achar * GetHelpFile()
	{
		return AfApp::GetHelpFilename().Chars();
	}

	virtual void ReopenDbAndOneWindow(const OLECHAR * pszDbName, const OLECHAR * pszSvrName,
		HVO hvo = 0);

protected:
	virtual void Init(void);
	virtual RecMainWnd * CreateMainWnd(WndCreateStruct & wcs, FileOpenProjectInfo * pfopi);
	virtual void GetOpenProjHelpUrl(BSTR * pbstr)
	{
		StrApp str(m_strHelpFilename);
		str.Append("::/User_Interface/Menus/File/Open_a_topics_list.htm");
		str.GetBstr(pbstr);
	}
	virtual int GetOPSubitemClid()
	{ return kclidCmPossibilityList; }

	//:>****************************************************************************************
	//:>	Command handlers.
	//:>****************************************************************************************

	//:>****************************************************************************************
	//:>	Member variables.
	//:>****************************************************************************************

	CMD_MAP_DEC(CleApp);
};


/*----------------------------------------------------------------------------------------------
	The Choices List Editor's main window frame class.

	@h3{Hungarian: cmw}
----------------------------------------------------------------------------------------------*/
class CleMainWnd : public RecMainWnd
{
public:
	typedef RecMainWnd SuperClass;

	CleMainWnd();
	~CleMainWnd();

	void SetCurRecIndex(int ihvo);

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void OnActivate(bool fActivating, HWND hwnd);
	virtual bool OnViewBarChange(int ilist, Set<int> & siselOld, Set<int> & siselNew);
	virtual void OnTreeBarChange(HTREEITEM hItem, HVO hvoItem);
	virtual void OnTreeMenuChange(Cmd * pcmd);
	virtual void FixMenu(HMENU hmenu);
	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	void MakeNewView(UserViewType vwt, AfLpInfo * plpi, UserViewSpec * puvs,
		UserViewSpecVec * pvuvs = NULL);
	HTREEITEM GetCurTreeSel();
	void SetTreeSel(HTREEITEM hitem);
	HWND GethwndTrBr()
	{
		return m_hwndTrBr;
	}
	// Return the class id of the possibility list items being edited.
	virtual int GetRecordClid()
	{
		return m_ItemClsid;
	}
	// Override this to return proper class ID.
	virtual int GetDocumentClsid()
	{
		// TODO: Needs to depend on the kind of class we're showing.
		return kclidCmPossibility;
	}

	HVO GetHvoPssl()
	{
		return m_hvoPssl;
	}

	int GetWsPssl()
	{
		return m_wsPssl;
	}

	void SetHvoPssl(HVO hvoPssl)
	{
		m_hvoPssl = hvoPssl;
	}

	int AnalysisEnc()
	{
		return m_qlpi->AnalWs();
	}

	virtual bool IsFilterActive();
	virtual bool IsSortMethodActive();
	virtual void RefreshTreeView(HVO hvoSel);
	virtual void ReadBackTree(HWND hwndTree, HTREEITEM hti, HvoClsidVec &vhcNew,
		HvoClsidVec &vhcOld);

	// This is public because other classes can use this to automatically get an expandable
	// menu showing the views, filters, sort methods, or overlays.
	virtual bool CmdViewExpMenu(Cmd * pcmd);
	virtual bool CmsViewExpMenu(CmdState & cms);
	virtual void SetCaptionBarIcons(AfCaptionBar * pcpbr);
	virtual FilterMenuNodeVec * GetFilterMenuNodes(AfLpInfo * plpi);
	virtual SortMenuNodeVec * GetSortMenuNodes(AfLpInfo * plpi);

	PossListInfo * GetPossListInfoPtr()
	{
		PossListInfoPtr qpli;
		m_qlpi->LoadPossList(m_hvoPssl, m_wsPssl, &qpli);
		return qpli;
	}

	void InsertEntry(int pcmd);

	virtual HVO GetRootObj()
	{
		return m_hvoPssl;
	}

	virtual StrApp GetRootObjName()
	{
		return GetPossListInfoPtr()->GetName();
	}

	virtual void LoadData();
	virtual void CreateClient(UserViewType vwt, Vector<AfClientWndPtr> & vqafcw, int wid);
	// Override method to create appropriate subclass of AfSplitChild.
	virtual AfSplitChild * CreateNewSplitChild();
	virtual void GetTagFlids(TagFlids & tf)
	{
		tf.fRecurse = false;
		tf.flidRoot = kflidCmPossibilityList_Possibilities;
		tf.flidMain = kflidCmPossibility_Name;
		tf.flidCreated = 0;
		tf.flidModified = 0;
		tf.flidSubitems = kflidCmPossibility_SubPossibilities;
	}
	virtual void GetDragText(HVO hvo, int clid, ITsString ** pptss);
	HVO GetCurRec();
	virtual bool CmdEditDelete(Cmd * pcmd);
	virtual void LoadViewBar();
	virtual bool Synchronize(SyncInfo & sync);
	virtual bool CleMainWnd::FullRefresh();
	virtual AfDbInfo * CheckEmptyRecords(AfDbInfo * pdbi, StrUni stuProject, bool & fCancel);
	// Override to provide appropriate class check.
	virtual bool IsJumpableClid(int clid)
	{
		// TODO: Add other classes that CLE can deal with.
		// In short, this is CmPossibility and all of its subclasses.
		return clid == kclidCmPossibility;
	}
	// Override to create proper document view constructor class.
	virtual VwCustDocVc * CreateCustDocVc(UserViewSpec * puvs);
	// Override to return CleTlsOptDlg.
	virtual TlsOptDlg * GetTlsOptDlg()
	{
		return NewObj CleTlsOptDlg();
	}
	virtual achar * HowToSaveHelpString();

protected:
	// Member variables
	CleTreeBarPtr m_qtrbr;  // viewbar tree control
	HWND m_hwndTrBr;  // The hwnd to the treeBar.
	PossListDragDrop m_plddDragDrop; // Bolt-on package to handle drag and drop.
	Vector<int> m_vExtMnuIdx; // vector that allows ext menu items to be cross ref to menu positon.

	// Don't cache the pli, it can be replaced unexpectedly.
	// Instead save the HVO which identifies the list (m_hvoPssl below).
	//PossListInfoPtr m_qpli;

	int m_ItemClsid; // Class id of of the items in this list.
	HVO m_hvoPssl; // ID of the list we are editing.
	HVO m_wsPssl; // Encoding of the list we are editing.
	bool m_fSettingRecIndex; // Flag to keep OnTreeBarChange from displaying when it shouldn't.
	HVO m_hvoTarget; // Used to select correct item in tree view after a drag/insert.

	// Override to prepend list name in window title.
	virtual StrApp GetTitlePrefix()
	{
		StrApp str = GetPossListInfoPtr()->GetName();
		str.Append(_T(" - "));
		return str;
	}
	// Override method to return an empty string for the given HVO.
	virtual StrApp GetCaptionBarClasslabel(HVO hvoCurRec)
	{
		StrApp str(_T(""));
		return str;
	}
	// Override method to provide special behavior.
	virtual SmartBstr GetStatusBarPaneOneTitle(HVO hvoCurRec)
	{
		int aenc = m_qlpi->AnalWs();
		ITsStringPtr qtss;
		CheckHr(m_qcvd->get_MultiStringAlt(hvoCurRec,
			kflidCmPossibility_Name, aenc, &qtss));
		SmartBstr sbstr;
		CheckHr(qtss->get_Text(&sbstr));
		return sbstr;
	}
	virtual void PostAttach(void);
	// Override method to provide special behavior.
	// TODO RandyR: compare with public LoadData method.
	virtual void LoadMainData();
	virtual bool CmdRecSel(Cmd * pcmd);
	HTREEITEM FindItemFromId(HTREEITEM hitem, HVO hvoPss);
	bool CheckUnique(StrAnsi staName, StrAnsi staAbbr);

	virtual void LoadDefaultToolbarFlags(Vector<DWORD> & vflag, DWORD & dwBarFlags);

	// Override to return CleFilterNoMatchDlg.
	virtual FwFilterNoMatchDlg * CreateFilterNoMatchDlg()
	{
		return NewObj CleFilterNoMatchDlg();
	}
	// Subclasses should override this to return their respective
	// subclass of AfRecCaptionBar.
	virtual AfRecCaptionBar * CreateCaptionBar();
	// Override to create a new window.
	virtual void MakeJumpWindow(Vector<HVO> & vhvo, Vector<int> & vflid, int nView);
	// Override to return NULL, since CLE doesn't support cross references.
	virtual AppSortInfo * GetCrossReferenceSortInfo()
	{
		return NULL;
	}

	//:>****************************************************************************************
	//:>	Message handlers.
	//:>****************************************************************************************
	virtual bool OnClose()
	{
		m_fSettingRecIndex = true;	// Keeps it from trying to do a save, after DB closed.
		return SuperClass::OnClose();
	}

	//:>****************************************************************************************
	//:>	Command functions.
	//:>****************************************************************************************
	virtual bool CmdFileNewTList(Cmd * pcmd);
	virtual bool CmdFileNewProj(Cmd * pcmd);
	virtual bool CmdListsProps(Cmd * pcmd);
	virtual bool CmdFileExport(Cmd * pcmd);
	virtual bool CmdMerge();

	virtual bool CmdHelpFw(Cmd * pcmd)
	{
		// c:\\fw\\DistFiles\\Helps\FieldWorksSuite.chm
		return AfApp::Papp()->ShowTrainingFile(_T("\\Helps\\FieldWorksSuite.chm"));
	}
	virtual bool CmdHelpApp(Cmd * pcmd)
	{
		return AfApp::Papp()->ShowHelpFile();
	}
	/*virtual bool CmdTraining(Cmd * pcmd)
	{
		return AfApp::Papp()->ShowTutorialFile(
			_T("\\Topics List Editor\\Tutorials\\Topics List Editor Tutorials.pdf"));
	}*/

	virtual bool CmdWndNew(Cmd * pcmd);
	virtual bool CmdInsertEntry(Cmd * pcmd);
	virtual bool CmdViewTree(Cmd * pcmd);
	virtual bool CmsViewTree(CmdState & cms);

	CMD_MAP_DEC(CleMainWnd);
};

/*----------------------------------------------------------------------------------------------
	Tree bar (used in View bar). This class manages drawing the items in the list bar embedded
	within an AfViewBar.

	@h3{Hungarian: trbr}
----------------------------------------------------------------------------------------------*/
class CleTreeBar : public AfTreeBar
{
	typedef AfTreeBar SuperClass;

public:
	CleTreeBar();
	// JohnT: encAnalysis is not used, and should eventually be removed. The writing system displayed
	// is the one returned by plpi->AnalWs().
	void Init(int encAnalysis, HVO hvoPssl, AfLpInfo * plpi)
	{
		AssertPtr(plpi);
		m_hvoPssl = hvoPssl;
		m_qlpi = plpi;
	}
	virtual HTREEITEM AddTreeItem(HVO hvoItem, HTREEITEM hParent);
	virtual HVO DelTreeItem(HTREEITEM hItem);

	virtual int GetPossNameType()
	{
		return m_pnt;
	}

	virtual void SetPossNameType(int pnt)
	{
		m_pnt = pnt;
		// Redraw the window so the list items get updated.
		::InvalidateRect(m_hwnd, NULL, true);
		return;
	}
	void SetDragDropHandler(PossListDragDrop * pldd) { m_pplddDragDrop = pldd; }

protected:
	int AnalysisEnc() { return m_qlpi->AnalWs();}
	PossListInfo * GetPossListInfoPtr()
	{
		PossListInfoPtr qpli;
		CleMainWnd * pcmw = dynamic_cast<CleMainWnd *>(MainWindow());
		AssertPtr(pcmw);
		m_qlpi->LoadPossList(m_hvoPssl, pcmw->GetWsPssl(), &qpli);
		return qpli;
	}
	int m_pnt; // PossNameType;
	// Don't cache the pli, it can be replaced unexpectedly.
	// Instead save the HVO which identifies the list (m_hvoPssl below).
	//PossListInfoPtr m_qpli;
	HVO m_hvoPssl; // ID of the list we are editing.
	AfLpInfoPtr m_qlpi; // language project whose list we are editing.
	PossListDragDrop * m_pplddDragDrop; // Bolt-on package to handle drag and drop.
	bool m_fNoUpdate; // When set, do not update the screen.

	virtual bool OnContextMenu(HWND hwnd, Point pt);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	bool OnNotifyThis(int ctidFrom, NMHDR * pnmh, long & lnRet);

	virtual bool CmdTrbMenu(Cmd * pcmd);
	virtual bool CmsTrbMenu(CmdState & cms);

	CMD_MAP_DEC(CleTreeBar);
};


/*----------------------------------------------------------------------------------------------
	Default Cle listbar class for use in the view bar.

	@h3{Hungarian: clb}
----------------------------------------------------------------------------------------------*/
class CleListBar : public AfRecListBar
{
	typedef AfRecListBar SuperClass;

public:

protected:
	virtual bool CmdToolsOpts(Cmd * pcmd);

	CMD_MAP_DEC(CleListBar);
};


/*----------------------------------------------------------------------------------------------
	The Choices List Editor's caption bar to get multiple icons and context popup menus.

	@h3{Hungarian: ccpbr}
----------------------------------------------------------------------------------------------*/
class CleCaptionBar : public AfRecCaptionBar
{
	typedef AfCaptionBar SuperClass;

public:
	CleCaptionBar(RecMainWnd * prmwMain);

protected:
	virtual void ShowContextMenu(int ibtn, Point pt);
};


/*----------------------------------------------------------------------------------------------
	This class provides a dialog that shows any time there are no records in the list that the
	list editor is connected to.

	Hungarian: remp.
----------------------------------------------------------------------------------------------*/
class CleEmptyListDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	CleEmptyListDlg()
	{
		m_rid = kridEmptyList;
		m_hfontLarge = NULL;
	}
	~CleEmptyListDlg()
	{
		if (m_hfontLarge)
		{
			AfGdi::DeleteObjectFont(m_hfontLarge);
			m_hfontLarge = NULL;
		}
	}

	void SetProject(const achar * pszProject)
	{
		m_strProj = pszProject;
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);

	StrApp m_strProj;

	// Handle to a large font (16pt Sans Serif) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;
};

typedef GenSmartPtr<CleEmptyListDlg> CleEmptyListDlgPtr;


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\MkCustomCle.bat"
// End: (These 4 lines are useful to Steve McConnel.)

const int kcchEncoding = 8;

#endif // CLE_INCLUDED
