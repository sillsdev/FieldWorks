/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: RecMainWnd.h
Responsibility: Ken Zook
Last reviewed:

Description:
	This file contains class declarations for the following classes:
		RecMainWnd : AfMainWnd - This class supports record-based data in a language project.
			This class also supports a view bar and MDI client window as well as the
			functionality supported by AfMainWnd.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RecMainWnd_H
#define RecMainWnd_H 1

const int kwidMdiClient = 1000;
const int kwidViewBar = 1001;
const int kwidChildBase = 1003;

class AfDeRecSplitChild;	// Forward reference.
class VwCustBrowseVc;
class VwCustDocVc;

class ActionHandler;
typedef ComSmartPtr<ActionHandler> ActionHandlerPtr;

/*----------------------------------------------------------------------------------------------
	Supported viewbar list type indices.

	@h3{Hungarian: vblt}
----------------------------------------------------------------------------------------------*/
typedef enum
{
	kvbltNoValue = -1,
	kvbltView = 0,
	kvbltFilter,
	kvbltSort,
	kvbltOverlay,
	kvbltTree,

	kvbltLim	// Limit
} ViewbarListType;

/*----------------------------------------------------------------------------------------------
	RecMainWnd main window frame class for record-based applications.

	@h3{Hungarian: rmw}
----------------------------------------------------------------------------------------------*/
class RecMainWnd : public AfMainWnd
{
public:
	typedef AfMainWnd SuperClass;

	RecMainWnd(void);
	virtual ~RecMainWnd(void);

	virtual void OnActivate(bool fActivating, HWND hwnd);
	HvoClsidVec & Records()
	{
		return m_vhcFilteredRecords;
	}

	virtual void SetCurRecIndex(int ihvo)
	{
		m_ihvoCurr = ihvo;
	}

	virtual HvoClsid GetCurRecord()
	{
		return m_vhcFilteredRecords[m_ihvoCurr];
	}

	int CurRecIndex()
	{
		return m_ihvoCurr;
	}

	int RecordCount()
	{
		return m_vhcFilteredRecords.Size();
	}

	HvoClsidVec & RawRecords()		// May be needed for data export, even if not for display.
	{
		return m_vhcRecords;
	}

	virtual int RawRecordCount()
	{
		return m_vhcRecords.Size();
	}

	virtual int UserWs();

	// This method returns the ID for the main window that can be used to get the vector
	// of filtered records that is stored in the cache.
	HVO GetFilterId()
	{
		return m_hvoFilterVec;
	}

	HVO GetPrintSelId()
	{
		return m_hvoPrintSelVec;
	}

	// This method returns the ID for the main window that can be used to get the vector
	// of filtered records that is stored in the cache.
	// Returns the flid used in the cache to store a vector of filtered records. Any value
	// should work, but typically it is set to the master records flid
	// (e.g., RnResearchNbk_Records).
	int GetFilterFlid()
	{
		return m_flidFilteredRecords;
	}

	// If a window supports filtering, this method needs to be overloaded to return true when
	// a filter is active and false otherwise.
	virtual bool IsFilterActive()
	{
		return false;
	}
	// If a window supports sort methods, this method needs to be overloaded to return true when
	// a sort method is active, and false otherwise.
	virtual bool IsSortMethodActive()
	{
		return false;
	}

	virtual int OnInsertRecord(int clid, HVO hvoNew);
	virtual int AddMainRecord(HVO hvo, int clid);
	virtual void DeleteMainRecord(HVO hvo, bool fUpdateCache);
	// Subclasses need to override this.
	virtual void OnDeleteRecord(int flid, HVO hvoDel, bool fCheckEmpty = true) {}
	virtual void OnDeleteSubitem(int flid, HVO hvoDel, bool fCheckEmpty = true);
	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	virtual void SaveSettings(const achar * pszRoot, bool fRecursive = true);
	virtual void GetDragText(HVO hvo, int clid, ITsString ** pptss);

	// Generic menu enabler for items that depend on having at least one record.
	// It's public, because it gets called from split children.
	virtual bool CmsHaveRecord(CmdState & cms);

	// Command handler: This is public so the ListBar subclasses can use it.
	virtual bool CmdToolsOpts(Cmd * pcmd);

	// Called by CmdToolsOpts and others (e.g. CleListBar::CmdToolsOpts) which run the TlsOptDlg.
	virtual void RunTlsOptDlg(TlsOptDlg * ptod, TlsDlgValue & tgv);
	virtual AfSplitChild * CreateNewSplitChild();
	// Subclasses must override this.
	virtual VwCustBrowseVc * CreateCustBrowseVc(UserViewSpec * puvs,
		int dypHeader, int nMaxLines, HVO hvoRootObjId)
	{
		Assert(false);
		return NULL;
	}
	virtual int GetBrowseDummyClass()
	{ return 0; }
	// Subclasses must override this.
	virtual VwCustDocVc * CreateCustDocVc(UserViewSpec * puvs)
	{
		Assert(false);
		return NULL;
	}

	// Subclasses should override this to get proper flids.
	virtual void GetTagFlids(TagFlids & tf)
	{
		tf.fRecurse = true;
		tf.flidRoot = 0;
		tf.flidMain = 0;
		tf.flidCreated = 0;
		tf.flidModified = 0;
		tf.flidSubitems = 0;
	}
	// Subclasses should override to get the right string resource id.
	virtual int GetWhatsThisStid()
	{
		return 0;
	}
	// Subclasses should override this to return proper class ID.
	virtual int GetDocumentClsid()
	{ return 0; }
	void UpdateRecordDate(HVO hvo = 0);
	virtual bool FullRefresh();
	void ReloadRecordsFromDatabase();
	virtual bool Synchronize(SyncInfo & sync);
	virtual bool PreSynchronize(SyncInfo & sync);
	virtual void ApplyFilterAndSort(int iflt, bool & fCancel, int isrt,
		FwFilterXrefUtil * pfxref);
	virtual void DisableFilterAndSort();

	virtual bool CmdEditUndo(Cmd * pcmd);
	virtual bool CmdEditRedo(Cmd * pcmd);
	virtual bool CmsEditUndo(CmdState & cms);
	virtual bool CmsEditRedo(CmdState & cms);
	// Subclasses should override this, if they have something specific to
	// do when there are no records to work on.
	// Set fCancel to "true", if the caller should cancel the operation,
	// Otherwise set it to "false" for caller to continue.
	// The override of this may return a new AfDbInfo, or the same, as needed.
	// (cf. RN for an example of a new one.)
	virtual AfDbInfo * CheckEmptyRecords(AfDbInfo * pdbi, StrUni stuProject, bool & fCancel)
	{
		fCancel = false;
		return pdbi;
	}

	virtual void JumpTo(HVO hvo);
	// Subclasses should override this, if they have classes they can jump to.
	virtual bool IsJumpableClid(int clid)
	{
		return false;
	}
	CustViewDa * MainDa()
	{
		return m_qcvd;
	}
	int TreeWidth()
	{
		return m_dxpDeTreeWidth;
	}
	void SetTreeWidth(int dxp)
	{
		m_dxpDeTreeWidth = dxp;
	}
	virtual void UpdateCaptionBar();
	virtual void SetCaptionBarIcons(AfCaptionBar * pcpbr)
		{ }
	void MergeOverlays(Set<int> & sisel);
	void SetStartupInfo(const HVO * prghvo, int chvo, const int * prgflid, int cflid,
		int ichCur, int nView);

	// Return the vector of path HVOs.
	Vector<HVO> & GetHvoPath()
	{
		return m_vhvoPath;
	}
	// Return the vector of path flids.
	Vector<int> & GetFlidPath()
	{
		return m_vflidPath;
	}
	// Get the cursor index within a field.
	int GetCursorIndex()
	{
		return m_ichCur;
	}
	// Set the cursor index within a field.
	void SetCursorIndex(int ich)
	{
		m_ichCur = ich;
	}
	// This method needs to be defined for each subclass.
	// @return Id of the root object we are displaying (e.g., RnResearchNbk,
	// PossibilityList, etc.)   This is assumed to be a subclass of CmMajorObject.
	virtual HVO GetRootObj()
	{
		return dynamic_cast<AfLpInfo *>(m_qlpi.Ptr())->ObjId();
	}
	// This method needs to be defined for each subclass.
	// @return Name of the root object we are displaying (e.g., RnResearchNbk,
	// PossibilityList, etc.)
	virtual StrApp GetRootObjName()
	{
		Assert(false);
		return "";
	}
	// Each subclass should override this method, if they want special behavior.
	// TODO RandyR: compare with potected LoadMainData method.
	virtual void LoadData()
		{ }
	virtual HVO CreateUndoableObject(HVO hvoOwner, int flidOwn, int flidOwnerModified,
			int clsid, int kstid, int ihvo);
	virtual HVO CreateUndoableObjectCore(HVO hvoOwner, int flidOwn, int clsid, int ihvo);
	virtual void DeleteUndoableObject(HVO hvoOwner, int flidOwn, int flidOwnerModified,
			HVO hvoObj, int kstid, int ihvo, bool fHasRefs);
	virtual void DeleteUndoableObjectCore(HVO hvoOwner, int flidOwn, HVO hvoObj, int ihvo);
	virtual void RenameAndDeleteStyles(Vector<StrUni> & vstuOldNames,
		Vector<StrUni> & vstuNewNames, Vector<StrUni> & vstuDelNames);
	virtual void UpdateToolBarWrtSysControl();
	virtual void SaveData();
	void UpdateDateModified(HVO hvo, int flid);
	virtual void LoadPageSetup();
	virtual void SavePageSetup();
	virtual void OnChangeOverlay(int iovr)
		{ /* Defined on derived classes. */ }

	virtual AfLpInfo * GetLpInfo()
	{
		return m_qlpi;
	}

	/*------------------------------------------------------------------------------------------
		Return the basic database class id for the records loaded into this main window.
		Override this in the application main window subclass.
	------------------------------------------------------------------------------------------*/
	virtual int GetRecordClid()
	{
		return 0;
	}

	virtual AfStylesheet * GetStylesheet()
	{
		if (!m_qlpi)
			return NULL;
		return m_qlpi->GetAfStylesheet();
	}

	virtual IActionHandler * GetActionHandler()
	{
		if (!m_qlpi)
			return NULL;
		IActionHandlerPtr qacth;
		m_qlpi->GetActionHandler(&qacth);
		return qacth.Ptr();
	}

	void SetImageLists(HIMAGELIST himlLarge, HIMAGELIST himlSmall)
	{
		m_rghiml[0] = himlLarge;
		m_rghiml[1] = himlSmall;
	}
	HIMAGELIST GetImageList(bool fSmall)
		{ return m_rghiml[fSmall]; }
	AfMdiClientWnd * GetMdiClientWnd()
	{
		return m_qmdic;
	}

	AfViewBarShell * GetViewBarShell()
	{
		return m_qvwbrs;
	}

	void RegisterRootBox(IVwRootBox * prootb);
	void SetCurrentOverlay(IVwOverlay * pvo);
	void GetCurrentOverlay(IVwOverlay ** ppvo)
	{
		AssertPtr(ppvo);
		*ppvo = m_qvo;
		AddRefObj(*ppvo);
	}
	virtual void OnToolBarButtonAdded(AfToolBar * ptlbr, int ibtn, int cid);
	void FillWrtSysButton(AfToolBarCombo * ptbc);
	virtual bool IsOkToChange(bool fChkReq = false);
	// Overrides.
	virtual void OnIdle();
	virtual int GetMinWidth();
	virtual void UpdateStatusBar();
	virtual void UpdateTitleBar();
	virtual void Init(AfLpInfo * plpi);
	virtual void InitMdiClient();
	// The subclass should return false if it doesn't want the selection to change.
	virtual bool OnViewBarChange(int ilist, Set<int> & siselOld, Set<int> & siselNew)
		{ return true; } // Do nothing.
	virtual void OnTreeBarChange(HTREEITEM hItem, HVO hvoItem) // Do nothing.
		{ }
	virtual void OnTreeMenuChange(Cmd * pcmd) // Do nothing.
		{ }
	virtual void MakeNewView(UserViewType vwt, AfLpInfo * plpi,
		UserViewSpec * puvs, UserViewSpecVec * pvuvs = NULL) // Do nothing
		{ }

	int GetViewbarListIndex(ViewbarListType vblt)
	{
		Assert(vblt >= kvbltView && vblt < kvbltLim);
		return m_ivblt[vblt];
	}
	int GetViewbarListType(int iValue)
	{
		Assert(iValue >= 0 && iValue < kvbltLim);
		int ivblt;
		for (ivblt = kvbltView; ivblt < kvbltLim; ++ivblt)
			if (m_ivblt[ivblt] == iValue)
				break;
		return ivblt;
	}

	virtual void SetWindowMode(bool fFullWindow);
	// Loads user views from the database.
	virtual bool LoadUserViews();
	virtual void CreateClient(UserViewType vwt, Vector<AfClientWndPtr> & vqafcw, int wid)
		{ } // Do nothing.
	virtual void OnPreActivate(bool fActivating);
	AfDeSplitChild * CurrentDeWnd();
	// Clear and load the view bar. This needs to be defined on subclasses.
	virtual void LoadViewBar()
		{ }
	bool CloseActiveEditors();
	bool CloseAllEditors(bool fForce = false);
	virtual void GetLgWritingSystemFactory(ILgWritingSystemFactory ** ppwsf);
	// Update the Tree View display.
	// This will only do something in classes that implement a tree view,
	// such as CLE, and then only if it overrides this method.
	virtual void RefreshTreeView(HVO hvoSel)
		{ }
	bool EnsureSafeSort();

	enum
	{
		kdxpSplitter = 3,
	};

	// Subclasses should override this to return their respective subclass of TlsOptDlg.
	virtual TlsOptDlg * GetTlsOptDlg()
	{
		Assert(false);
		return NULL;
	}

		// This is an enum that tells ProcessExcessiveChgs what type of message to put
		// in it's messagebox to the user.
	typedef enum
	{
		kSaveWarnTyping = 0,
		kSaveWarnReplace,
	} SaveWarningType;

	bool ProcessExcessiveChgs(SaveWarningType nWarningType, bool fAlwaysSave, bool * pfCancelOp, ISilDataAccess * psda = NULL);
	bool IsPossibilityDeletable(HVO hvoPss, int nMsg);

	// NOTE: All of these values are used as indexes, so change them very carefully.
	enum
	{
		// Indices into AfViewBarImages_small.bmp and AfViewBarImages_large.bmp.
		kimagDataEntry    = 0,
		kimagDocument     = 1,
		kimagBrowse       = 2,
		kimagFilterNone   = 3,
		kimagFilterSimple = 4,
		kimagFilterFull   = 5,
		kimagSort         = 6,
		kimagOverlayNone  = 7,
		kimagOverlay      = 8,
		kimagTree         = 9,
	};

	virtual bool FindInDictionary(IVwRootBox * prootb)
	{
		return false;
	}

	virtual bool EnableCmdIfVernacularSelection(IVwRootBox * prootb, CmdState & cms)
	{
		cms.Enable(false);
		return true;
	}

protected:
	int m_wIdOldSel;
	StrUni m_stuOldSel;

	// Member variables
	int m_ivblt[kvbltLim];
	int m_ivbltMax;

	// This is a data access object that keeps track of all the data we have loaded
	// to support current views.
	CustViewDaPtr m_qcvd;
	IActionHandlerPtr m_qacth;
	int m_dxpDeTreeWidth; // The tree width for data entry views.
	// These next three variables define view and a path to get to an object to display when
	// the window is first opened. For example, if subentry 1582 is to be displayed, these
	// vectors would have:
	// vhvoPath: 1579, 1581, 1582
	// vflidPath: 4004009, 4004009
	// 1579 is the main record. 1581 is a subentry in the 4004009 flid of 1579. 1582 is a
	// subentry in the 4004009 flid of 1581. When the window opens, we try to display the
	// cursor in the 1582 record. If a third flid is provided, then we try to place the
	// cursor in that field at m_ichCur.
	Vector<HVO> m_vhvoPath; // Ownership path of HVOs from a main item to the target item.
	Vector<int> m_vflidPath; // Path of flids on each HVO to get to the next HVO.
	int m_ichCur; // The cursor index in the final field specified in m_vflidPath.
	int m_nView; // Specifies the view for the target item.
	// Indicates we've made changes that should update the modification date on the root obj.
	bool m_fIsDateDirty;
	// Set of view types which have at least one export option available.
	Set<UserViewType> m_setvwtExportable;
	// Information needed to sort by crossreference fields.
	AppSortInfo m_asiXref;
	bool m_fCheckRequired;
	// Sorted vector of top-level records in the database and their clsids (e.g., the
	// contents of RnResearchNbk_Records). This is loaded from the database and
	// updated when main records are added or deleted.
	HvoClsidVec m_vhcRecords;
	// The list of records we are currently displaying in DE views. This is a
	// sorted filtered list that may contain subrecords as well as records. This is
	// initialized from m_vhcRecords when filters are inactive, and is set by filter
	// code when filters are active. Doc and Browse views actually use a fake property
	// in the cache (m_hvoFilterVec) which is a copy of this list. Both of these lists
	// must be maintained as records are added and deleted. AddMainRecord and
	// DeleteMainRecord are methods that keep all three vectors in sync.
	// When filters are not enabled, this list should be identical to m_vhcRecords.
	HvoClsidVec m_vhcFilteredRecords;
	// Indexes the "current" record. In some views this is the only one displayed;
	// in other views it is the one containing the selection anchor. Particular
	// views need not keep it up to date except when needed.
	int m_ihvoCurr;
	// The fake HVO ID in the cache used to store a vector of filtered records.
	// The key for the cache is (m_hvoFilterVec, m_flidFilteredRecords).
	HVO m_hvoFilterVec;
	// The flid used in the cache to store a vector of filtered records. Any value should
	// work, but typically it is set to the master records flid (e.g.,
	// RnResearchNbk_Records).
	int m_flidFilteredRecords;
	// The fake HVO ID in the cache used to store a vector of the currently selected
	// records for printing. Like m_hvoFilterVec, it uses m_flidFilteredRecords
	// as the property tag.
	HVO m_hvoPrintSelVec;
	// This vector maps 1:1 onto m_vhcFilteredRecords, giving the HVO for each element of the
	// sort key for each record.
	// REVIEW: Should there be a copy of this in the cache?
	Vector<SortKeyHvos> m_vskhSortKeys;
	// This flags whether a filter has been cancelled, so that restoring the old filter can
	// occur without any further prompting of the user.
	bool m_fFilterCancelled;
	UserViewType m_vwt;
	AfMdiClientWndPtr m_qmdic;
	AfViewBarShellPtr m_qvwbrs;
	HIMAGELIST m_rghiml[2];		// View Bar Image Lists:  [0] Large, [1] Small.
	bool m_fResizing;
	int m_dxpLeft;
	IVwOverlayPtr m_qvo;
	// REVIEW SteveMc(KenZ): Note, when we clear m_qlpi we may also need to remove
	// AfDbInfo from the application cache, but this is only available on AfDbApp.
	AfLpInfoPtr m_qlpi;
	// This is used to determine what the previous state of the viewbar was
	// if we're in Full Window mode.
	bool m_fOldViewbarVisible;

	// Count of undo actions when the user last told us not to save after a warning.
	// 0 if he saved or has not done enough actions to get a warning.
	int m_cActUndoLastChange;

	enum
	{
		kmskShowLargeViewIcons    = 0x0001,
		kmskShowLargeFilterIcons  = 0x0002,
		kmskShowLargeSortIcons    = 0x0004,
		kmskShowLargeOverlayIcons = 0x0008,
		kmskShowViewBar           = 0x0010,
	};

	// Subclasses should override this,
	// if they want something to appear before the project name
	// in the window title.
	virtual StrApp GetTitlePrefix()
	{
		StrApp str(_T(""));
		return str;
	}

	// Subclasses should override this to return the proper resource ID
	// for the given HVO.
	virtual StrApp GetCaptionBarClasslabel(HVO hvoCurRec)
	{
		StrApp str(kstidUnknownClass);
		str.Append(_T(" - "));
		return str;
	}

	// Subclasses should override to get a better string.
	virtual SmartBstr GetStatusBarPaneOneTitle(HVO hvoCurRec)
	{
		SmartBstr sbstr(L"");
		return sbstr;
	}

	// Each subclass should override this method, if they want special behavior.
	// TODO RandyR: compare with public LoadData method.
	virtual void LoadMainData()
		{ }

	// Subclasses must override this, to create a new window.
	virtual void MakeJumpWindow(Vector<HVO> & vhvo, Vector<int> & vflid, int nView)
	{ Assert(false); }

	// Subclasses should override this to return their respective
	// subclass of FwFilterNoMatchDlg.
	virtual FwFilterNoMatchDlg * CreateFilterNoMatchDlg()
	{
		Assert(false);
		return NULL;
	}

	// Subclasses should override this to return their respective
	// subclass of AfRecCaptionBar.
	virtual AfRecCaptionBar * CreateCaptionBar()
	{
		Assert(false);
		return NULL;
	}

	virtual AppSortInfo * GetCrossReferenceSortInfo()
	{
		return &m_asiXref;
	}
	void FindExportableViews();
	virtual void PostAttach(void);
	virtual void GetClientRect(Rect & rc);
	virtual bool OnClientSize(void);
	void StartUndoableTask(HVO hvoOwner, int flidOwnerModified, int kstid);
	virtual void FixMenu(HMENU hmenu);
	void ClearViewBarImageLists();
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual DWORD GetViewbarSaveFlags();
	bool SwitchToDataEntryView(int kimagDataEntryIn);
	bool EnsureNoFilter();
	virtual void NewWindow(RecMainWnd ** pprmw)
		{ }
	virtual void GetCurClsLevel(int * pclsid, int * pnLevel);

	//:>****************************************************************************************
	//:>	Message handlers.
	//:>****************************************************************************************
	virtual bool OnClose();
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	virtual void OnReleasePtr();

	/*******************************************************************************************
		Command handlers.
	*******************************************************************************************/
	virtual bool CmsInsertEntry(CmdState & cms);
	virtual bool CmdRecSel(Cmd * pcmd);
	virtual bool CmsRecSelUpdate(CmdState & cms);
	virtual bool CmdFileSave(Cmd * pcmd);
	virtual bool CmsFileSave(CmdState & cms);
	virtual bool CmsFileExport(CmdState & cms);
	virtual bool CmdHelpMode(Cmd * pcmd);
	virtual bool CmdWndSplit(Cmd * pcmd);
	virtual bool CmsWndSplit(CmdState & cms);
	virtual bool CmdVbToggle(Cmd * pcmd);
	virtual bool CmsVbUpdate(CmdState & cms);
	virtual bool CmdSbToggle(Cmd * pcmd);
	virtual bool CmsSbUpdate(CmdState & cms);
	virtual bool CmdSettingChange(Cmd * pcmd);
	virtual bool CmdViewFullWindow(Cmd * pcmd);
	virtual bool CmsViewFullWindow(CmdState & cms);
	virtual bool CmdFileProjProps(Cmd * pcmd);
	virtual bool CmdTlsLists(Cmd * pcmd);
	virtual bool CmdStats(Cmd * pcmd);
	virtual bool CmdExternalLink(Cmd * pcmd);
	virtual bool CmdWndNew(Cmd * pcmd);
	virtual achar * HowToSaveHelpString();


	// This data structure stores the relevant information from the Field$ and UserViewField
	// tables in the language project database for use by GetFilterMenuNodes() and
	// GetSortMenuNodes().
	// Hungarian: fd
	struct FieldData
	{
		int flid;
		int type;
		int clid;
		int clidDest;		// stores 0 for NULL.
	};

	// This data structure stores the relevant information from the MultiTxt$, UserViewField,
	// and UserViewRec tables in the language project database for use by GetFilterMenuNodes()
	// and GetSortMenuNodes().
	// Hungarian: ld
	struct LabelData
	{
		int flid;
		int clid;
		StrUni stuLabel;
		int wsMagic;			// Writing system value from UserViewField.
	};

	void _AddLanguageChoices(AfLpInfo * plpi, HashMap<int,FieldData> & hmflidfd,
		SortMenuNodeVec & vsmnLang, SortMenuNode * psmn, int wsDefault);
	bool HandleUndoRedoResults(HRESULT hr, UndoResult ures, bool fPrivate);

	// This data structure stores the relevant information for CmFile object whose files can be
	// moved or copied due to a change in LangProject_ExtLinksRootDir (or whose InternalPath
	// values must be fixed).
	// Hungarian: mov
	struct MovableFile
	{
		int hvoCmFile;
		StrUni stuInternalPath;
	};
	void CollectMovableFiles(Vector<MovableFile> & vstuFiles,
		const StrUni & stuOldExtLinkRoot, const StrUni & stuNewExtLinkRoot);
	void CollectMovableFilesFromFolder(IOleDbEncap * pode, int hvoFolder,
		Vector<MovableFile> & vstuFiles, const StrUni & stuOldExtLinkRoot,
		const StrUni & stuNewExtLinkRoot);
	void ExpandToFullPath(Vector<MovableFile> & vmovFiles, const StrUni & stuRootDir);

private:
	// Registry location for all FieldWorks applications
	FwSettings * m_allFws;
};

#endif // !RecMainWnd_H
