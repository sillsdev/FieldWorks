/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2000, SIL International. All rights reserved.

File: NoteBk.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This file provides the base for Research Notebook functions.  It contains class
	definitions for the following classes:

		RnDbInfo : AfDbInfo - This class is overridden to allow us to create an RnLpInfo
			structure.
		RnLpInfo : AfLpInfo - This class contains Research Notebook information about a
			language project.
		RnApp : AfDbApp - The Research Notebook's application class.
		RnMainWnd : RecMainWnd - The Research Notebook's main window frame class.
		RnOverlayListBar : AfOverlayListBar - Overlay listbar class for use in the view bar.
		RnTagOverlayTool : AfTagOverlayTool - This class is necessary so that we can show the
			Tools/Options dialog when configure is chosen from the popup menu or the button
			on the overlay tool window.
		RnListBar : AfListBar - Default RN listbar class for use in the view bar.
		RnCaptionBar : AfCaptionBar - The Research Notebook's caption bar to get multiple
			icons and context popup menus.
		RnFilterNoMatchDlg : FwFilterNoMatchDlg - It gets shown when the user has selected a
			filter that results in no records being shown.
		RnFromQueryBuilder : FromQueryBuilder - provides specialized functions for handling
			cross reference fields.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef NOTEBK_INCLUDED
#define NOTEBK_INCLUDED 1

#include "FwAppVersion.h"

class RnApp;
class RnMainWnd;
class RnOverlayListBar;
class RnTagOverlayTool;
class RnFilterNoMatchDlg;
class RnListBar;
class RnCaptionBar;
typedef GenSmartPtr<RnApp> RnAppPtr;
typedef GenSmartPtr<RnMainWnd> RnMainWndPtr;
typedef GenSmartPtr<RnOverlayListBar> RnOverlayListBarPtr;
typedef GenSmartPtr<RnTagOverlayTool> RnTagOverlayToolPtr;
typedef GenSmartPtr<RnFilterNoMatchDlg> RnFilterNoMatchDlgPtr;
typedef GenSmartPtr<RnListBar> RnListBarPtr;
typedef GenSmartPtr<RnCaptionBar> RnCaptionBarPtr;

// class AfDeRecSplitChild;	// Forward reference.
// class RnDeSplitChild;

/*
// Type of event -- stored in Type field of RnEvent in database.
typedef enum {
	kevtObs = 0, // Observation
	kevtAlm = 1, // Almanac
	kevtConv = 2, // Conversation
	kevtPerf = 3, // Performance
	kevtLitSum = 4, // Literature Summary
	kevtLim,
} EventType;
*/


/*----------------------------------------------------------------------------------------------
	This class provides functions for dealing with cross reference fields.  These functions are
	application dependent.

	Hungarian: rxref.
----------------------------------------------------------------------------------------------*/
class RnFilterXrefUtil : public FwFilterXrefUtil
{
public:
	virtual bool ProcessCrossRefColumn(int flid, bool fJoin, int & ialias, int ialiasLastClass,
		IFwMetaDataCache * pmdc, StrUni & stuFromClause, StrUni & stuWJoin,
		SmartBstr & sbstrClass, SmartBstr & sbstrField,
		StrUni & stuAliasText, StrUni & stuAliasId);
	virtual bool ProcessCrossRefListColumn(int flid, int & ialias, int ialiasLastClass,
		IFwMetaDataCache * pmdc, StrUni & stuFromClause, StrUni & stuWJoin,
		SmartBstr & sbstrClass, SmartBstr & sbstrField,
		StrUni & stuAliasText, StrUni & stuAliasId);
	virtual bool FixCrossRefTitle(StrUni & stuAliasText, int flid);
};

/*----------------------------------------------------------------------------------------------
	This class provides the user with a dialog that gives them a choice of turning off all
	filters, modifying the current filter, or selecting a new filter to use. It gets shown
	when the user has selected a filter that results in no records being shown.

	@h3{Hungarian: fltnm}
----------------------------------------------------------------------------------------------*/
class RnFilterNoMatchDlg : public FwFilterNoMatchDlg
{
protected:
	virtual void GetTlsOptDlg(TlsOptDlg ** pptod);
	virtual void SelectNewMenu(HMENU hmenuPopup)
	{
		::AppendMenu(hmenuPopup, MF_STRING, kcidExpFilters, NULL);
	}
};


/*----------------------------------------------------------------------------------------------
	This class is overridden to allow us to create an RnLpInfo structure.

	@h3{Hungarian: dbi}
----------------------------------------------------------------------------------------------*/
class RnDbInfo : public AfDbInfo
{
public:
	RnDbInfo();

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
			&CLSID_ResearchNotebook);
	}
	virtual void LoadFilters()
	{
		FilterUtil::LoadFilters(reinterpret_cast<AfDbInfo *>(this), &CLSID_ResearchNotebook);
	}
	virtual HRESULT GetLogPointer(IStream ** ppfist)
	{
		return AfApp::Papp()->GetLogPointer(ppfist);
	}
	virtual void CompleteBrowseRecordSpec(UserViewSpec * puvs);
};

typedef GenSmartPtr<RnDbInfo> RnDbInfoPtr;


/*----------------------------------------------------------------------------------------------
	This class contains Research Notebook information about a language project.

	@h3{Hungarian: lpi}
----------------------------------------------------------------------------------------------*/
class RnLpInfo : public AfLpInfo
{
public:
	RnLpInfo();

	virtual bool OpenProject();
	virtual bool LoadProjBasics();
	virtual bool StoreAndSync(SyncInfo & sync);

	HVO GetRnId()
	{
		return m_hvoRn;
	}

	StrUni GetRnName()
	{
		return m_stuRNName;
	}

	void SetRnName(StrUni stuRnName)
	{
		m_stuRNName = stuRnName;
	}

	/*------------------------------------------------------------------------------------------
		Return the name of the current top major project, or NULL if none available.
	------------------------------------------------------------------------------------------*/
	virtual const OLECHAR * ObjName()
	{
		return m_stuRNName.Chars();
	}
	/*------------------------------------------------------------------------------------------
		Return the database id of the current top major project, or 0 if none available.
	------------------------------------------------------------------------------------------*/
	virtual HVO ObjId()
	{
		return m_hvoRn;
	}

	enum
	{
		kpidPsslCon = 0, //ConfidenceLevels
		kpidPsslRes,	//Restrictions
		kpidPsslWea,	//WeatherConditions
		kpidPsslRol,	//Roles
		kpidPsslPeo,	//People
		kpidPsslLoc,	//Locations
		kpidPsslAna,	//AnalysisStatus
		kpidPsslTyp,	//EventTypes
		kpidPsslTim,	//TimeOfDay
		kpidPsslEdu,	//Education
		kpidPsslPsn,	//Positions
		kpidPsslAnit,	//AnthroList

		kpidPsslLim,
	};

protected:
	HVO m_hvoRn;
	StrUni m_stuRNName;
};

typedef GenSmartPtr<RnLpInfo> RnLpInfoPtr;


/*----------------------------------------------------------------------------------------------
	The Research Notebook's application class.

	@h3{Hungarian: app}
----------------------------------------------------------------------------------------------*/
class RnApp : public AfDbApp
{
	typedef AfDbApp SuperClass;

public:
	//:>****************************************************************************************
	//:>	public methods.
	//:>****************************************************************************************

	RnApp();

	virtual bool GetAppVer(int & nAppVer, int & nErlyVer, int & nLastVer);
	virtual int GetAppNameId();
	virtual int GetAppPropNameId();
	virtual int GetAppIconId();
	STDMETHOD(GetDefaultBackupDirectory)(BSTR * pbstrDefBackupDir);

	virtual void NewMainWnd(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
		int hvoMainObj, int encUi, int nTool, int nParam, DWORD dwRegister);
	virtual void NewMainWndWithSel(BSTR bstrServerName, BSTR bstrDbName, int hvoLangProj,
		int hvoMainObj, int encUi, int nTool, int nParam, const HVO * prghvo, int chvo,
		const int * prgflid, int cflid, int ichCur, int nView, DWORD dwRegister);
	virtual void ReopenDbAndOneWindow(const OLECHAR * pszDbName, const OLECHAR * pszSvrName,
		HVO hvo = 0);

	virtual const CLSID * GetAppClsid()
	{
		return &CLSID_ResearchNotebook;
	}

	virtual AfDbInfo * GetDbInfo(const OLECHAR * pszDbName, const OLECHAR * pszSvrName);

	virtual const achar * GetHelpFile()
	{
		return AfApp::GetHelpFilename().Chars();
	}
	//virtual char * GetHelpFile()
	//{
	//	return "DataNotebookUserHelp.chm";
	//}

	virtual HVO NbkIdFromLpId(HVO hvoLpId, const StrUni& stuServer, const StrUni& stuDatabase);

	// For the Australia version, we have a dropdead date of June 30, 2002
	virtual SilTime DropDeadDate()
	{
		return SilTime(3000, 1, 1);
	}


protected:
	virtual void Init(void);
	virtual RecMainWnd * CreateMainWnd(WndCreateStruct & wcs, FileOpenProjectInfo * pfopi);

	//:>****************************************************************************************
	//:>	Command handlers.
	//:>****************************************************************************************

	//:>****************************************************************************************
	//:>	Member variables.
	//:>****************************************************************************************

	CMD_MAP_DEC(RnApp);
};


/*----------------------------------------------------------------------------------------------
	The Research Notebook's main window frame class.

	@h3{Hungarian: rnmw}
----------------------------------------------------------------------------------------------*/
class RnMainWnd : public RecMainWnd
{
public:
	typedef RecMainWnd SuperClass;

	RnMainWnd();
	~RnMainWnd();

	void GetStatsQuery(int iList, StrUni * pstuQuery);
	virtual void OnDeleteRecord(int flid, HVO hvoDel, bool fCheckEmpty = true);
	void GetDragText(HVO hvo, int clid, ITsString ** pptss);
	virtual void EnableWindow(bool fEnable);
	virtual bool OnViewBarChange(int ilist, Set<int> & siselOld, Set<int> & siselNew);
//	void MergeOverlays(Set<int> & sisel);
	void OnChangeOverlay(int iovr);
	virtual void LoadSettings(const achar * pszRoot, bool fRecursive = true);
	void MakeNewView(UserViewType vwt, AfLpInfo * plpi, UserViewSpec * puvs,
		UserViewSpecVec * pvuvs = NULL);
	virtual bool IsFilterActive();
	virtual bool IsSortMethodActive();
	virtual HVO CreateUndoableObjectCore(HVO hvoOwner, int flidOwn, int clsid, int ihvo);

	// This is public because other classes can use this to automatically get an expandable
	// menu showing the views, filters, sort methods, or overlays.
	virtual bool CmdViewExpMenu(Cmd * pcmd);
	virtual bool CmsViewExpMenu(CmdState & cms);
	virtual void SetCaptionBarIcons(AfCaptionBar * pcpbr);
	virtual FilterMenuNodeVec * GetFilterMenuNodes(AfLpInfo * plpi);
	virtual SortMenuNodeVec * GetSortMenuNodes(AfLpInfo * plpi);
	virtual HVO GetRootObj()
	{
		return dynamic_cast<RnLpInfo *>(m_qlpi.Ptr())->GetRnId();
	}

	virtual StrApp GetRootObjName()
	{
		Assert(m_qlpi);
		StrApp str = dynamic_cast<RnLpInfo *>(m_qlpi.Ptr())->GetRnName();
		return str;
	}

	// Return the base class id of the items being edited.
	virtual int GetRecordClid()
	{
		return kclidRnGenericRec;
	}
	// Override this to return proper class ID.
	virtual int GetDocumentClsid()
	{
		// TODO: Needs to depend on the kind of class we're showing.
		return kclidRnEvent;
	}

	virtual void CreateClient(UserViewType vwt, Vector<AfClientWndPtr> & vqafcw, int wid);
	virtual void LoadData();
	virtual void ShowAllOverlays(bool fShow, bool fRemerge = false);

	// Override method to create appropriate subclass of AfSplitChild.
	virtual AfSplitChild * CreateNewSplitChild();
	// Override method to create appropriate subclass of VwCustBrowseVc.
	virtual VwCustBrowseVc * CreateCustBrowseVc(UserViewSpec * puvs,
		int dypHeader, int nMaxLines, HVO hvoRootObjId);
	// Override to create proper document view constructor class.
	virtual VwCustDocVc * CreateCustDocVc(UserViewSpec * puvs);
	virtual void GetTagFlids(TagFlids & tf)
	{
		tf.fRecurse = true;
		tf.flidRoot = kflidRnResearchNbk_Records;
		tf.flidMain = kflidRnGenericRec_Title;
		tf.flidCreated = kflidRnGenericRec_DateCreated;
		tf.flidModified = kflidRnGenericRec_DateModified;
		tf.flidSubitems = kflidRnGenericRec_SubRecords;
	}

	virtual int GetWhatsThisStid()
	{
		return kstidEntryTypeIconWhatsThisHelp;
	}
	HVO GetCurRec();
	virtual bool CmdDelete(Cmd * pcmd);
	virtual bool CmdDelete1(Cmd * pcmd, HVO hvo);
	virtual void LoadViewBar();
	virtual AfDbInfo * CheckEmptyRecords(AfDbInfo * pdbi, StrUni stuProject, bool & fCancel);
	// Override to provide proper class check.
	virtual bool IsJumpableClid(int clid)
	{
		return (clid == kclidRnEvent || clid == kclidRnAnalysis);
	}
	// Override to return RnTlsOptDlg.
	virtual TlsOptDlg * GetTlsOptDlg()
	{
		return NewObj RnTlsOptDlg();
	}

	virtual bool FindInDictionary(IVwRootBox * prootb);
	virtual bool EnableCmdIfVernacularSelection(IVwRootBox * prootb, CmdState & cms);
	bool HandleContextMenu(HWND hwnd, Point pt, IVwRootBox * prootb,
		AfVwRecSplitChild * prsc);

protected:

	// Override method to return the proper resource ID for the given HVO.
	virtual StrApp GetCaptionBarClasslabel(HVO hvoCurRec)
	{
		int clid;
		HVO hvoOwn;
		CheckHr(m_qcvd->get_ObjOwner(hvoCurRec, &hvoOwn));
		CheckHr(m_qcvd->get_ObjClid(hvoCurRec, &clid));
		RnLpInfoPtr qrlpi = dynamic_cast<RnLpInfo *>(m_qlpi.Ptr());
		Assert(qrlpi);

		// Set fSub to indicate whether we have a subrecord or not.
		bool fSub = (hvoOwn != qrlpi->GetRnId());
		int stid;
		if (clid == kclidRnEvent)
			stid = fSub ? kstidEventSubentry : kstidEvent;
		else
			stid = fSub ? kstidAnalSubentry : kstidAnalysis;
		StrApp str(stid);
		str.Append(_T(" - "));
		return str;
	}

	// Override method to provide special behavior.
	virtual SmartBstr GetStatusBarPaneOneTitle(HVO hvoCurRec)
	{
		ITsStringPtr qtss;
		CheckHr(m_qcvd->get_StringProp(hvoCurRec, kflidRnGenericRec_Title, &qtss));
		SmartBstr sbstrTitle;
		CheckHr(qtss->get_Text(&sbstrTitle));
		return sbstrTitle;
	}
	// Override method to provide special behavior.
	// TODO RandyR: compare with public LoadData method.
	virtual void LoadMainData();
	void GetFilterMenuRoles(FilterMenuNodeVec & vfmnRoles, AfLpInfo * plpi);
	void GetSortMenuRoles(SortMenuNodeVec & vsmnRoles, AfLpInfo * plpi);
	virtual void LoadDefaultToolbarFlags(Vector<DWORD> & vflag, DWORD & dwBarFlags);
	// Override to always set pnLevel to 0.
	virtual void GetCurClsLevel(int * pclsid, int * pnLevel)
	{
		SuperClass::GetCurClsLevel(pclsid, pnLevel);
		*pnLevel = 0;
	}
	// Override to return RnFilterNoMatchDlg.
	virtual FwFilterNoMatchDlg * CreateFilterNoMatchDlg()
	{
		return NewObj RnFilterNoMatchDlg();
	}

	// Subclasses should override this to return their respective
	// subclass of AfRecCaptionBar.
	virtual AfRecCaptionBar * CreateCaptionBar();
	// Override to create a new window.
	virtual void MakeJumpWindow(Vector<HVO> & vhvo, Vector<int> & vflid, int nView);
	void CheckForNoRecords(const achar * pszProjName);

//-	virtual void PostAttach();
	void CheckAnthroList();

	//:>****************************************************************************************
	//:>	Message handlers.
	//:>****************************************************************************************

	/*******************************************************************************************
		Command handlers.
	*******************************************************************************************/

	virtual bool CmdHelpFw(Cmd * pcmd)
	{
		// c:\\fw\\DistFiles\\Helps\FieldWorksSuite.chm
		return AfApp::Papp()->ShowTrainingFile(_T("\\Helps\\FieldWorksSuite.chm"));
	}
	virtual bool CmdHelpApp(Cmd * pcmd)
	{
		return AfApp::Papp()->ShowHelpFile();
	}
	virtual bool CmdTraining(Cmd * pcmd)
	{
		achar * pszFilespec;
		switch(pcmd->m_cid)
		{
		case kcidHelpStudentManual:
			pszFilespec = _T("\\Data Notebook\\Training\\DN Student Manual.doc");
			break;
		case kcidHelpExercises:
			pszFilespec = _T("\\Data Notebook\\Training\\DN Exercises.doc");
			break;
		case kcidHelpInstructorGuide:
			pszFilespec = _T("\\Data Notebook\\Training\\DN Instructor Guide.doc");
			break;
		default:
			Assert(false);
			pszFilespec = _T("Invalid Training File");
		}

		return AfApp::Papp()->ShowTrainingFile(pszFilespec);
	}

	virtual bool CmdWndNew(Cmd * pcmd);
	virtual bool CmdInsertEntry(Cmd * pcmd);

	virtual bool CmdFileImport(Cmd * pcmd);
	virtual bool CmdFileExport(Cmd * pcmd);
	virtual bool CmdFileNewProj(Cmd * pcmd);
	virtual bool CmdFileDnProps(Cmd * pcmd);
	virtual bool CmdStats(Cmd * pcmd);

	CMD_MAP_DEC(RnMainWnd);
};


/*----------------------------------------------------------------------------------------------
	Overlay listbar class for use in the view bar.
	This is needed to allow multiple selections in this listbar.

	@h3{Hungarian: rolb}
----------------------------------------------------------------------------------------------*/
class RnOverlayListBar : public AfOverlayListBar
{
	typedef AfOverlayListBar SuperClass;

public:
	RnOverlayListBar()
	{
		m_hwndOwner = NULL;
	}
	void Init(HWND hwndOwner, RnLpInfo * plpi)
	{
		AssertPtr(plpi);
		m_hwndOwner = hwndOwner;
		m_qlpi = plpi;
	}

	virtual void CreateOverlayTool(AfTagOverlayTool ** pprtot);

protected:
	virtual int GetConfigureCid();
	virtual bool CmdToolsOpts(Cmd * pcmd);

	CMD_MAP_DEC(RnOverlayListBar);
};


/*----------------------------------------------------------------------------------------------
	This class is necessary so that we can show the Tools/Options dialog when configure is
	chosen from the popup menu or the button on the overlay tool window.

	@h3{Hungarian: tot}
----------------------------------------------------------------------------------------------*/
class RnTagOverlayTool : public AfTagOverlayTool
{
typedef AfTagOverlayTool SuperClass;

public:
	virtual bool OnConfigureTag(int iovr, int itag);
	virtual bool OnChangeOverlay(int iovr);
};


/*----------------------------------------------------------------------------------------------
	Default RN listbar class for use in the view bar.

	@h3{Hungarian: rlb}
----------------------------------------------------------------------------------------------*/
class RnListBar : public AfRecListBar
{
	typedef AfRecListBar SuperClass;

public:

protected:
	virtual bool CmdToolsOpts(Cmd * pcmd);

	CMD_MAP_DEC(RnListBar);
};


/*----------------------------------------------------------------------------------------------
	The Research Notebook's caption bar to get multiple icons and context popup menus.

	@h3{Hungarian: ccpbr}
----------------------------------------------------------------------------------------------*/
class RnCaptionBar : public AfRecCaptionBar
{
	typedef AfCaptionBar SuperClass;

public:
	RnCaptionBar(RecMainWnd * prmwMain);

protected:
	virtual void ShowContextMenu(int ibtn, Point pt);
};


const int kcchEncoding = 8;


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Empty Notebook Dialog, allowing the user to
	enter the first record or quit.

	Hungarian: remp.
----------------------------------------------------------------------------------------------*/
class RnEmptyNotebookDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	RnEmptyNotebookDlg()
	{
		m_rid = kridEmptyNotebook;
		m_hfontLarge = NULL;
	}
	~RnEmptyNotebookDlg()
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

typedef GenSmartPtr<RnEmptyNotebookDlg> RnEmptyNotebookDlgPtr;

#endif // NOTEBK_INCLUDED
