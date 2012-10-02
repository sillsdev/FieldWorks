/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwFilterDlg.h
Responsibility: Steve McConnel (was Darrell Zook)
Last reviewed: Not yet.

Description:
	Header file for the Tools/Options filter dialog and supporting dialogs for it.

	This file contains class declarations for the following classes:
		FwFilterXrefUtil - 	This class provides virtual methods for dealing with cross reference
			fields.
		FilterUtil - This class provides a central location for global utility methods that are
			used by several of the filter dialogs.
		KeywordLookup - This class allows conversion between the FilterKeywordType type and the
			strings that correspond to each type.
		DateKeywordLookup - This class allows conversion between the index for the date scope
			combo box and the strings that correspond to each index.
		FwFilterDlg : AfDialogView - This is the main filter dialog window. It contains a list
			of filters on the left and a FwFilterSimpleShellDlg or a FwFilterFullDlg on the
			right, based on the type of the selected filter.
		FwFilterSimpleShellDlg : AfDialogView - This is the shell for the simple filter dialog.
			It contains prompt information as well for simple filters. It contains a
			FwFilterSimpleDlg and is embedded inside a FwFilterDlg.
		FwFilterBuilderShellDlg : AfDialog - This is the modeless criteria builder dialog. It
			contains a FwFilterSimpleDlg. It is a modeless top-level dialog that appears for
			complex ("full") filters.
		FwFilterSimpleDlg : AfDialogView - This is the dialog that allows the user to modify
			the settings for a simple filter. It is never used as a stand-alone dialog, but is
			embedded inside FwFilterSimpleShellDlg and FwFilterBuilderShellDlg dialogs.
		FwFilterFullDlg : AfDialogView - This is the dialog that allows the user to modify the
			settings for a complex ("full") filter. It is embedded inside a FwFilterDlg.  It
			also controls when the FwFilterBuilderShellDlg and FwFilterTipsDlg dialogs are
			shown.
		FwFilterFullShellDlg : AfDialog - This is a resizable modal dialog that contains an
			FwFilterFullDlg dialog. It is launched from the FwFilterFullDlg dialog.
		FwFilterHeader : AfWnd - This is the header control that is embedded inside a FilterWnd.
			It is used in complex ("full") filters to resize the columns within the filter.
		FwFilterPromptDlg : AfDialog - This is a modal dialog that is used for simple filters
			when the user wants to see a prompt every time they run a filter.
		FwFilterTipsDlg : AfDialog - This is a modeless dialog that shows tips for complex
			("full") filters.  It is called up from the FwFilterFullDlg dialog.
		FwFilterNoMatchDlg : AfDialog - This is a modal dialog that appears when the user
			selects a filter and there are no matches (for whatever reason; user error, program
			error, or no matches). It allows the user to turn off the filter, modify it, or
			choose another filter.
		FwFilterTurnOffDlg : AfDialog - This is the modal dialog that should appear when the
			user performs an action that cannot be performed while one or more filters are
			active. After showing this dialog, all filters should be turned off.
		FwFilterErrorMsgDlg : AfDialog - This is used instead of ::MessageBox() whenever a help
			button is needed.
		FwFilterLaunchBtn : AfWnd - This is a button that shows three dots (...). It is intended
			to show the user that clicking on it will bring up another dialog.
		FwFilterPssEdit : AfWnd - This is an edit control that gives typeahead capability for
			the items in a possibility list.
		FwFilterStatic : AfWnd - This class is used to keep a static control from responding
			to its hotkey. See the comment below at the class declaration.
		FwFilterButton : AfButton - This class is used to have a button control respond to a
			hotkey. See the comment below at the class declaration.
		FilterVc : VwBaseVc - This is the view constructor for complex ("full") filters. It is
			used by the FilterWnd class.
		FilterWnd : AfVwScrollWnd - This is the view window that is used for complex ("full")
			filters.  It contains an FwFilterHeader window and is embedded inside of an
			FwFilterFullDlg.
		FromQueryBuilder - This class creates a from clause for a SQL statement and a vector of
			aliases based on the columns that are added to it.
		WhereQueryBuilder - This class creates a where clause for a SQL statement based on the
			contents of the cells in the filter table specified in the constructor.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FW_FILTER_H
#define FW_FILTER_H 1

class FwFilterDlg;
class FwFilterSimpleShellDlg;
class FwFilterBuilderShellDlg;
class FwFilterSimpleDlg;
class FwFilterFullDlg;
class FwFilterFullShellDlg;
class FwFilterHeader;
class FwFilterPromptDlg;
class FwFilterTipsDlg;
class FwFilterNoMatchDlg;
class FwFilterTurnOffDlg;
class FwFilterErrorMsgDlg;
class FwFilterLaunchBtn;
class FwFilterPssEdit;
class FwFilterStatic;
class FwFilterButton;
class FilterVc;
class FilterWnd;
typedef GenSmartPtr<FwFilterDlg> FwFilterDlgPtr;
typedef GenSmartPtr<FwFilterSimpleShellDlg> FwFilterSimpleShellDlgPtr;
typedef GenSmartPtr<FwFilterBuilderShellDlg> FwFilterBuilderShellDlgPtr;
typedef GenSmartPtr<FwFilterSimpleDlg> FwFilterSimpleDlgPtr;
typedef GenSmartPtr<FwFilterFullDlg> FwFilterFullDlgPtr;
typedef GenSmartPtr<FwFilterFullShellDlg> FwFilterFullShellDlgPtr;
typedef GenSmartPtr<FwFilterHeader> FwFilterHeaderPtr;
typedef GenSmartPtr<FwFilterPromptDlg> FwFilterPromptDlgPtr;
typedef GenSmartPtr<FwFilterTipsDlg> FwFilterTipsDlgPtr;
typedef GenSmartPtr<FwFilterNoMatchDlg> FwFilterNoMatchDlgPtr;
typedef GenSmartPtr<FwFilterTurnOffDlg> FwFilterTurnOffDlgPtr;
typedef GenSmartPtr<FwFilterErrorMsgDlg> FwFilterErrorMsgDlgPtr;
typedef GenSmartPtr<FwFilterLaunchBtn> FwFilterLaunchBtnPtr;
typedef GenSmartPtr<FwFilterPssEdit> FwFilterPssEditPtr;
typedef GenSmartPtr<FwFilterStatic> FwFilterStaticPtr;
typedef GenSmartPtr<FwFilterButton> FwFilterButtonPtr;
typedef ComSmartPtr<FilterVc> FilterVcPtr;
typedef ComSmartPtr<FilterWnd> FilterWndPtr;


const int kdxpFilterDefColumn = 100;
const achar * const kpszFilterSubKey = _T("Filter Dialog");

enum
{
	kfrfiFilter,
	kfrfiRow,
	kfrfiCell,
};

typedef enum
{
	kfptStText = 10000,
	kfptPossList,
	kfptTagList,
	kfptEnumList,
	kfptEnumListReq,
	kfptBoolean,
	kfptCrossRef,
	kfptCrossRefList,
	kfptRoledParticipant,
} FilterPropType;

typedef enum
{
	kfktError,
	kfktEmpty,
	kfktNotEmpty,
	kfktContains,
	kfktDoesNotContain,
	kfktMatches,
	kfktDoesNotMatch,
	kfktEqual,
	kfktNotEqual,
	kfktGT,
	kfktLT,
	kfktGTE,
	kfktLTE,
	kfktAnd,
	kfktOr,
	kfktOpenParen,
	kfktCloseParen,
	kfktYes,
	kfktNo,
} FilterKeywordType;

typedef enum
{
	//:> These numbers are used as indexes, so they cannot change.
	kfftText = 0,
	kfftRef = 1,
	kfftRefText = 2,
	kfftDate = 3,
	kfftEnum = 4,
	kfftEnumReq = 5,
	kfftBoolean = 6,
	kfftCrossRef = 7,
	kfftNumber = 8,
	kfftLim,

	kfftNone = -1,
} FilterFieldType;


typedef enum
{
	//:> These numbers are used as indexes, so they cannot change.
	kfpmSpecial = 0,
	kfpmFormat = 1,
	kfpmContext = 2,
} FilterPopupMenu;

typedef struct
{
	StrUni m_stuName;
	StrUni m_stuAbbrev;
	StrUni m_stuReplace;
} FilterPatternInfo;


/*----------------------------------------------------------------------------------------------
	This class provides functions for dealing with cross reference fields.  These functions are
	application dependent.

	Hungarian: fxref.
----------------------------------------------------------------------------------------------*/
class FwFilterXrefUtil
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
	This class provides a central location for general utility methods that are used by several
	of the filter dialogs.

	Hungarian: fu
----------------------------------------------------------------------------------------------*/
class FilterUtil
{
public:
	static bool LoadFilters(AfDbInfo * pdbi, const GUID * pguidApp);
	static bool BuildColumnsVector(AfLpInfo * plpi, StrUni & stuColumns,
		Vector<FilterMenuNodeVec> & vvfmnColumns, AfMainWnd * pafw);
	static void BuildColumnsString(Vector<FilterMenuNodeVec> & vvfmnColumns,
		StrUni & stuColumns);
	static bool GetFilterQuery(AfLpInfo * plpi, int iflt, HWND hwnd, StrUni & stuQuery,
		HVO hvoTopLevel, int flidTop, int flidSub, AfMainWnd * pafw, AppSortInfo * pasi,
		AppSortInfo * pasiXref, bool fCancelPrev, FwFilterXrefUtil * pfxref);
	static void SkipWhiteSpace(const wchar *& psz)
	{
		AssertPtr(psz);
		while (*psz && iswspace(*psz))
			psz++;
	}
	static int ParseWritingSystem(const wchar *& prgchKey, ILgWritingSystemFactory * pwsf);
	static HMENU CreatePopupMenu(AfLpInfo * plpi, AfMainWnd * pafw);
	static void BuildColumnVector(FilterMenuNodeVec & vfmnFlat, int ifmnFlat,
		FilterMenuNodeVec & vfmn, bool fClear = true);
	static void GetColumnName(FilterMenuNodeVec & vfmn, bool fPath, StrApp & str);
	static void GetSimpleFilterPrompt(const wchar * prgchCell, ILgWritingSystemFactory * pwsf,
		FilterMenuNodeVec & vfmnColumn, StrApp & strPrompt, StrApp & strCondition,
		FilterKeywordType * pfkt = NULL, FilterFieldType * pfft = NULL,
		bool * pfSubitems = NULL, int * pws = NULL);

	static bool InsertHotLink(HWND hwndPar, AfDbInfo * pdbi, FilterMenuNodeVec & vfmn,
		IVwCacheDa * pvcd, ITsString * ptss, IVwRootBox * prootb, HVO hvoOwner, PropTag tag,
		int ichObj, bool fReplace, HVO hvoPss = NULL, FilterVc * pfvc = NULL);
	static FilterKeywordType GetKeywordType(FilterFieldType fft, HWND hwndSimpleDlg);
	static void FillDateScopeCombo(HWND hwndCombo, bool fExpanded);
	static void StringToReference(ITsString * ptss, FilterMenuNodeVec & vfmn, AfDbInfo * pdbi,
		HVO & hvoPssl, HVO & hvoPss);
	static void AddEnumToCombo(HWND hwndCombo, int stid, const achar * pszValue);
	static void GetFieldCaption(HDC hdc, const Rect & rc, FwFilterDlg * pfltdlg, int icol,
		StrApp & strCaption, bool fShowHotKey);

	FilterUtil();
	~FilterUtil();
	HIMAGELIST GetImageList();
	void GetSystemFont(StrUni & stuFont, int & dympFont);

	Vector<FilterPatternInfo> & GetLanguageVariables();

	/*------------------------------------------------------------------------------------------
		Get the pointer to a TssEdit widget.
	------------------------------------------------------------------------------------------*/
	TssEdit * GetTssEdit()
	{
		return m_qteSpecial;
	}
	/*------------------------------------------------------------------------------------------
		Set a pointer to a TssEdit widget.
	------------------------------------------------------------------------------------------*/
	void SetTssEdit(TssEdit * pte)
	{
		m_qteSpecial = pte;
	}

	/*------------------------------------------------------------------------------------------
		Establish a pointer to an writing system code.
	------------------------------------------------------------------------------------------*/
	void SetEncAddr(int * pws)
	{
		m_pwsSpecial = pws;
	}
	/*------------------------------------------------------------------------------------------
		Use the pointer to an writing system code (if set) to fetch the writing system code.
	------------------------------------------------------------------------------------------*/
	int GetSpecialEnc()
	{
		if (m_pwsSpecial)
			return *m_pwsSpecial;
		else
			return 0;
	}
	/*------------------------------------------------------------------------------------------
		Use the pointer to an writing system code (if set) to store an new writing system code.  Clear the
		pointer to prevent bogus reuse.
	------------------------------------------------------------------------------------------*/
	void SetSpecialEnc(int ws)
	{
		if (m_pwsSpecial)
		{
			*m_pwsSpecial = ws;
			m_pwsSpecial = NULL;	// Used only once for a menu invocation.
		}
	}

protected:
	HIMAGELIST m_himl;				// Image list for storing filter dialog button images.
	StrUni m_stuFont;				// Name of the default system font (DEFAULT_GUI_FONT).
	int m_dympFont;					// Height of the default system font in millipoints.
	TssEditPtr m_qteSpecial;		// Pointer to TssEdit widget containing text.

	int * m_pwsSpecial;			// Pointer to writing system used for a text field filter.

	static void InsertMenuNode(HMENU hmenu, FilterMenuNode * pfmn, int & cid,
		FilterMenuNodeVec & vfmnFlat, bool fPushNodes);
};


/*----------------------------------------------------------------------------------------------
	This class allows conversion between the FilterKeywordType type and the strings that
	correspond to each type.

	Hungarian: kl
----------------------------------------------------------------------------------------------*/
class KeywordLookup
{
public:
	KeywordLookup();
	FilterKeywordType GetTypeFromStr(const wchar *& pszKeyword);
	const wchar * GetStrFromType(FilterKeywordType fkt);

protected:
	typedef struct
	{
		int m_stid;
		FilterKeywordType m_fkt;
		const wchar * m_psz;
		int m_cch;
	} KeywordEntry;

	//:> This is not a Vector<KeywordEntry> because it is initialized from a static array.
	KeywordEntry * m_prgke;			// Pointer to array of keyword entries.
	int m_cke;						// Number of entries in m_prgke.

	// This gets filled with all the keyword strings loaded as resources, and separated by NUL
	// characters.
	static StrUni s_stuKeywords;
};


/*----------------------------------------------------------------------------------------------
	This class allows conversion between the index for the date scope combo box and the strings
	that correspond to each index.

	Hungarian: dkl
----------------------------------------------------------------------------------------------*/
class DateKeywordLookup
{
public:
	DateKeywordLookup();
	int GetIndexFromStr(const wchar *& pszKeyword);
	const wchar * GetStrFromIndex(int idke)
	{
		Assert((uint)idke < (uint)m_cdke);
		return m_prgdke[idke].m_psz;
	}
	int GetStidFromIndex(int idke)
	{
		Assert((uint)idke < (uint)m_cdke);
		return m_prgdke[idke].m_stid;
	}

protected:
	typedef struct
	{
		int m_stid;
		const wchar * m_psz;
		int m_cch;
	} DateKeywordEntry;

	//:> This is not a Vector<DateKeywordEntry> because it is initialized from a static array.
	DateKeywordEntry * m_prgdke;	// Pointer to array of date keyword entries.
	int m_cdke;						// Number of entries in m_prgdke.

	// This gets filled with all the date keyword strings loaded as resources, and separated by
	// NUL characters.
	static StrUni s_stuKeywords;
};


/*----------------------------------------------------------------------------------------------
	This class implements the Filter dialog pane in the Tools Option dialog.  It allows a user
	to create, modify, or delete filters.  It contains a list of filters on the left and a
	FwFilterSimpleShellDlg or a FwFilterFullDlg on the right, based on the type of the selected
	filter on the left.

	Hungarian: fltdlg
----------------------------------------------------------------------------------------------*/
class FwFilterDlg : public AfDialogView
{
	typedef AfDialogView SuperClass;

public:
	FwFilterDlg(TlsOptDlg * ptod);
	~FwFilterDlg();

	typedef enum
	{
		kfsNormal = 0,
		kfsModified,
		kfsInserted,
		kfsDeleted,
	} FilterState;

	class FilterInfo
	{
	public:
		FilterInfo()
		{
			m_fSimple = true;
			m_hvo = m_hvoOld = 0;
			m_fShowPrompt = false;
			m_fs = kfsNormal;
		}
		StrUni m_stuName;
		bool m_fSimple;
		HVO m_hvo; // The ID of the filter in the dummy cache.
		HVO m_hvoOld; // The ID of the filter in the database.
		StrUni m_stuColInfo;
		bool m_fShowPrompt;
		StrUni m_stuPrompt;
		int m_fs;
	};

	void SetDialogValues(RecMainWnd * prmwMain, int ifltInitial = 0);
	void GetDialogValues(Vector<FilterInfo> & vfi);
	bool WasModified()
		{ return m_fModified; }

	virtual bool Apply();

	FilterInfo & GetCurrentFilterInfo()
	{
		Assert((uint)m_ifltCurrent < (uint)m_vfi.Size());
		return m_vfi[m_ifltCurrent];
	}

	FilterInfo & LoadCurrentFilter(Vector<FilterMenuNodeVec> & vvfmnColumns);

	AfLpInfo * GetLpInfo();
	void ShowChildren(bool fShow);
	virtual bool QueryClose(QueryCloseType qct);
	bool SetActive();
	// Used only for passing a volatile pointer as a function argument -- no reference
	// counting is done!
	ILgWritingSystemFactory * WritingSystemFactory()
	{
		return m_qwsf.Ptr();
	}

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool OnEndLabelEdit(NMLVDISPINFO * plvdi, long & lnRet);

	bool ShowFilterSubDlg(int ifltr);

	void UpdateFilterList();
	void DeleteFilter();
	void InsertFilter(const wchar * pszName, bool fSimple, FilterInfo * pfiCopy);
	void _CopyFilterRows(HVO hvoFilterOld, HVO hvoFilterNew, IVwCacheDa * pvcdOld,
		IVwCacheDa * pvcdNew, bool fSimple);

	//:>****************************************************************************************
	//:>	Command functions.
	//:>****************************************************************************************
	virtual bool CmdAddFilter(Cmd * pcmd);
	virtual bool CmdFltrSpcExpand(Cmd * pcmd);
	virtual bool CmdFltrFormat(Cmd * pcmd);
	virtual bool CmdFltrFmtWrtSys(Cmd * pcmd);
	virtual bool CmsFltrFormat(CmdState & cms);

	// Pointer to embedded dialog for editing simple filters.
	FwFilterSimpleShellDlgPtr m_qfltss;
	// Pointer to embedded dialog for editing complex filters.
	FwFilterFullDlgPtr m_qfltf;
	// Pointer to the currently showing embedded dialog, either m_qfltss or m_qfltf.
	AfDialogView * m_pdlgvLast;
	// Index into m_vfi for the initial filter chosen for editing.
	int m_ifltInitial;
	// Index into m_vfi for the current filter chosen for editing.
	int m_ifltCurrent;
	// Pointer to the private cache that contains all modifications to the set of filters.
	IVwCacheDaPtr m_qvcd;
	// Temporary cache used for loading/saving from/to the database.
	IVwOleDbDaPtr m_qodde;
	// Pointer to the application's main window, used mostly for getting the language project
	// information.
	RecMainWnd * m_prmwMain;
	// Flag that the filters have been modified, and thus need to be saved to the database.
	bool m_fModified;
	// Pointer to the enclosing Tools/Options dialog.
	TlsOptDlg * m_ptod;
	// Vector of filter information loaded from the database or created interactively by the
	// user, and saved to the database.
	Vector<FilterInfo> m_vfi;
	int m_atid; // identifies the special accelerator table loaded to handle edit commands.

	// This stores the vector of writing systems of interest, used by the Format button menu.
	Vector<int> m_vws;
	ILgWritingSystemFactoryPtr m_qwsf;

	CMD_MAP_DEC(FwFilterDlg);
};


/*----------------------------------------------------------------------------------------------
	This class implements a dialog that allows the user to modify a simple filter.  It is
	never used as a stand-alone dialog, but is embedded inside FwFilterSimpleShellDlg and
	FwFilterBuilderShellDlg dialogs.

	Hungarian: flts
----------------------------------------------------------------------------------------------*/
class FwFilterSimpleDlg : public AfDialogView
{
typedef AfDialogView SuperClass;

public:
	FwFilterSimpleDlg();
	~FwFilterSimpleDlg();

	void SetDialogValues(AfLpInfo * plpi, FilterMenuNodeVec * pvfmn, ITsString * ptss);
	void GetDialogValues(ITsString ** pptss);
	void RefreshFilterDisplay(FilterMenuNodeVec * pvfmn, ITsString * ptss);

	void ShowType(int fptOld = kcptNil);

	virtual bool Apply();

	void RefreshPossibility(HVO hvo, PossNameType pnt);

	bool IsCriteriaPromptAllowed()
	{
		return m_fAllowCriteriaPrompt;
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);

	bool IsValidDate();

	// Vector of filter menu nodes that specify what this filter is looking at in the database.
	// These will identify a specific column in a specific table in the database, possibly
	// involving "joiner tables" in the process.
	FilterMenuNodeVec m_vfmn;
	// Basic filter field type (Text, Ref, RefText, Date, ...)
	FilterFieldType m_fft;
	// TsString containing the operator and filter value for this filter.  Remember, TsString
	// allows for embedded object links, which is useful for this purpose.
	ITsStringPtr m_qtss;
	// Pointer to the language project information, used mostly to get database information.
	AfLpInfoPtr m_qlpi;
	// Pointer to a special possibility item type-ahead edit box that is used if the value
	// type is either a Possibility List or a Tag List.
	FwFilterPssEditPtr m_qfpe;
	// This is set to true the first time a cell is being shown.  This usually means that the
	// contents of the other controls that are visible should be refreshed to the new
	// string/item/date/...  The reason we need this is because we don't want to reset the
	// other controls every time the user changes a condition, because the user might type
	// values in the other controls, and then decide just to change the condition from
	// 'Equals' to 'Does not Equal'.  The other controls should not be reset in this case.
	bool m_fIgnoreCurSel;
	// This is set to true by default, but set to false whenever the current criteria in the
	// filter does not logically allow for an interactive prompt.  For now, this is only
	// affected by certain scopes of date values.
	bool m_fAllowCriteriaPrompt;
	// This stores the full date value for a date related filter criterion.
	SYSTEMTIME m_systime;
	// This stores the writing system for a multilingual string related filter criterion.
	int m_ws;
	// Flag if the Condition combobox has been widened to accommodate long condition names.
	bool m_fWideCondition;
	// The control window for the edit box.
	TssEditPtr m_qte;
};


/*----------------------------------------------------------------------------------------------
	This class implements a shell containing the Field combobox, an FwFilterSimpleDlg dialog,
	and prompt information for the simple filter.  This allows the user to define or modify a
	simple filter.  It is embedded inside an FwFilterDlg, sharing the same screen real estate
	with an FwFilterFullDlg dialog view.

	Hungarian: fltss
----------------------------------------------------------------------------------------------*/
class FwFilterSimpleShellDlg : public AfDialogView
{
typedef AfDialogView SuperClass;

public:
	FwFilterSimpleShellDlg();
	~FwFilterSimpleShellDlg();

	void SetDialogValues(FwFilterDlg * pfltdlg, IVwCacheDa * pvcd);
	void RefreshFilterDisplay();

	virtual bool Apply();

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	//:>****************************************************************************************
	//:>	Command functions.
	//:>****************************************************************************************
	virtual bool CmdFieldPopup(Cmd * pcmd);

	// Pointer to the enclosing overall filter dialog pane.
	FwFilterDlg * m_pfltdlg;
	// Pointer to the private cache that contains all modifications to the set of filters.
	IVwCacheDaPtr m_qvcd;
	// Pointer to the embedded simple filter edit dialog.
	FwFilterSimpleDlgPtr m_qflts;
	// Handle to the popup menu that allows choosing what the filter operates on.
	HMENU m_hmenuPopup;
	// Flag that the user changed the text of the prompt used to interactively obtain the filter
	// value.
	bool m_fEditedPrompt;
	// Handle to the tooltip that shows that complete menu path selection for the filter.
	HWND m_hwndToolTip;

	CMD_MAP_DEC(FwFilterSimpleShellDlg);
};


/*----------------------------------------------------------------------------------------------
	This class implements a modeless top-level "Criteria Builder" dialog that (optionally)
	appears for editing a single field in a complex ("full") filter.  It embeds an
	FwFilterSimpleDlg which allows the user to modify conditions for a single field.

	Hungarian: fltblds
----------------------------------------------------------------------------------------------*/
class FwFilterBuilderShellDlg : public AfDialog
{
typedef AfDialog SuperClass;

public:
	FwFilterBuilderShellDlg();
	~FwFilterBuilderShellDlg();

	void SetDialogValues(FwFilterDlg * pfltdlg, HVO hvoFilter, IVwCacheDa * pvcd,
		FwFilterFullDlg * pfltf, int icol, int irow, int ichMin, int ichEnd);
	void RefreshFilterDisplay(HVO hvoFilter, int icol, int irow, int ichMin, int ichEnd);

	void Close()
	{
		OnCancel();
	}

	void OnEditSelChange(int icol, int irow, int ichAnchor, int ichEnd,
		bool fForceRefresh = false);

	void EnableInsertBtn(bool fCanInsert);

	/*------------------------------------------------------------------------------------------
		Refresh the possibility edit box in the criteria builder dialog to reflect the new
		choice for how to display possibilities in this list.

		@param hvo Database id of the possibility item.
		@param pnt Specifies how the possibility is to be displayed.
	------------------------------------------------------------------------------------------*/
	void RefreshDialogHotLink(HVO hvo, PossNameType pnt)
	{
		if (m_qflts)
			m_qflts->RefreshPossibility(hvo, pnt);
	}

	void RefreshPossibilityColumn(PossNameType pnt);

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool OnCancel();
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	typedef enum
	{
		kstNone =   0,
		kstBefore = 1,
		kstAfter =  2,
	} SeparatorType;

	HVO m_hvoFilter;				// Database/cache ID of the filter being edited.
	FwFilterDlg * m_pfltdlg;		// Pointer to the controlling overall filter dialog pane.
	// Pointer to the private cache that contains all modifications to the set of filters.
	IVwCacheDaPtr m_qvcd;
	FwFilterSimpleDlgPtr m_qflts;	// Pointer to the embedded simple filter edit dialog.
	// Pointer to the complex ("full") filter editing dialog this is coordinating with.
	FwFilterFullDlg * m_pfltf;
	int m_irow;						// Current cell in the current filter column.
	int m_icol;						// Current column (field) in the filter being edited.
	int m_ichAnchor;				// End point of the selection in the current cell string.
	int m_ichEnd;					// The other end point of the selection in the cell string.
	bool m_fValidCriterion;			// The new criterion is valid for inserting.
	bool m_fValidInsertionPt;		// The selection point is valid for inserting new criteria.
	// Specifies type of separator needed for an insertion: None, Before (new criterion, then
	// leading and/or), or After (trailing and/or, then new criterion)
	SeparatorType m_st;
};


/*----------------------------------------------------------------------------------------------
	This class implements a dialog that allows the user to modify the settings for a complex
	("full") filter.  It also controls when the FwFilterBuilderShellDlg and FwFilterTipsDlg
	dialogs are shown.  It is embedded inside an FwFilterDlg or an FwFilterFullShellDlg dialog.

	Hungarian: fltf
----------------------------------------------------------------------------------------------*/
class FwFilterFullDlg : public AfDialogView
{
typedef AfDialogView SuperClass;

public:
	FwFilterFullDlg();
	~FwFilterFullDlg();

	void SetDialogValues(FwFilterDlg * pfltdlg, IVwCacheDa * pvcd, FwFilterFullDlg * pfltfCopy);
	void RefreshFilterDisplay();

	virtual bool Apply();

	FilterWnd * GetFilterWnd()
		{ return m_qfltvw; }

	void ShowTips(bool fShow);
	void ShowBuilder(bool fShow);

	FilterMenuNodeVec & GetColumnVector(int icol)
	{
		Assert((uint)icol < (uint)m_vvfmnColumns.Size());
		return m_vvfmnColumns[icol];
	}
	PossNameType GetColumnLinkNameType(int icol);

	void OnEditSelChange(int icol, int irow, int ichAnchor, int ichEnd);
	void InsertIntoCell(ITsString * ptss);

	// This should only get called from the FwFilterFullShellDlg window when it closes.
	void PreCloseCopy()
	{
		AssertPtr(m_pfltfCopy);
		UpdateChildWindows(this, m_pfltfCopy);
	}

	/*------------------------------------------------------------------------------------------
		Refresh the possibility edit box in the criteria builder dialog to reflect the new
		choice for how to display possibilities in this list.

		@param hvo Database id of the possibility item.
		@param pnt Specifies how the possibility is to be displayed.
	------------------------------------------------------------------------------------------*/
	void RefreshCriteriaBuilderHotLink(HVO hvo, PossNameType pnt)
	{
		if (m_qfltblds)
			m_qfltblds->RefreshDialogHotLink(hvo, pnt);
	}

	void RefreshPossibilityColumn(PossNameType pnt);

	void ShowChildren(bool fShow);

protected:

	enum
	{
		kdzpBorder = 6,
	};

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool OnSize(int wst, int dxp, int dyp);

	void AddColumn(int icol, int ifmn, bool fCopy = false);
	void RemoveColumn(int icol, bool fCopy = false);

	void SaveColumnWidths();

	void UpdateChildWindows(FwFilterFullDlg * pfltfOld, FwFilterFullDlg * pfltfNew);

	/*******************************************************************************************
		Command functions.
	*******************************************************************************************/
	virtual bool CmdFieldPopup(Cmd * pcmd);
	virtual bool CmdDeleteCol(Cmd * pcmd);
	virtual bool CmsDeleteCol(CmdState & cms);

	// Horizontal offset of the embedded filter table window within this dialog window.
	int m_xpTable;
	// Horizontal offset of the "expanded" filter table window within its enclosing window.
	int m_xpExpandTable;
	// Height of the "expanded" filter table window.
	int m_dypExpandTable;
	// Width of the coordinated tips dialog window.
	int m_dxpShowTips;
	// Height of the coordinated tips dialog window.
	int m_dypShowTips;
	// Width of the coordinated criteria builder dialog window.  (It uses the same height as
	// the tips dialog window.)
	int m_dxpShowBuilder;
	// Database/cache ID of the filter being edited.
	HVO m_hvoFilter;
	// Pointer to the private cache that contains all modifications to the set of filters.
	IVwCacheDaPtr m_qvcd;
	// Handle to the popup menu that allows choosing what a column of the filter operates on.
	HMENU m_hmenuPopup;
	// Pointer to the embedded filter table window.
	FilterWndPtr m_qfltvw;
	// Vector of vectors of filter menu nodes that specify what each column of this filter is
	// looking at in the database.  These will identify specific columns in specific tables in
	// the database, possibly involving "joiner tables" in the process.
	Vector<FilterMenuNodeVec> m_vvfmnColumns;
	// Pointer to the enclosing (or controlling) overall filter dialog pane.
	FwFilterDlg * m_pfltdlg;
	// Pointer to the coordinated tips dialog window, which may or may not be visible.
	FwFilterTipsDlgPtr m_qfltt;
	// Pointer to the coordinated criteria builder dialog, which may or may not be visible.
	FwFilterBuilderShellDlgPtr m_qfltblds;
	// Pointer to the original copy of this dialog embedded in the filter dialog pane.
	// This is used only when the table is "expanded" to create an independent, resizable
	// dialog window for editing a complex ("full") filter.
	FwFilterFullDlg * m_pfltfCopy;

	CMD_MAP_DEC(FwFilterFullDlg);
};


/*----------------------------------------------------------------------------------------------
	This class implements a resizable modal dialog that contains an FwFilterFullDlg dialog.  It
	is launched from the FwFilterFullDlg dialog embedded inside the FwFilterDlg dialog pane.

	Hungarian: fltfs
----------------------------------------------------------------------------------------------*/
class FwFilterFullShellDlg : public AfDialog
{
typedef AfDialog SuperClass;

public:
	FwFilterFullShellDlg();
	~FwFilterFullShellDlg();

	void SetDialogValues(FwFilterDlg * pfltdlg);

	int CreateDlgShell(FwFilterFullDlg * pfltf, HWND hwndPar, FilterWnd * pfltvw);

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);
	virtual bool OnCancel();
	virtual bool OnSize(int wst, int dxp, int dyp);

	enum
	{
		kdzpMargin = 4,
	};

	// Pointer to the controlling overall filter dialog pane.
	FwFilterDlg * m_pfltdlg;
	// Pointer to embedded dialog for editing complex filters.
	FwFilterFullDlgPtr m_qfltf;
	// Pointer to the filter table window used for displaying complex ("full") filters in the
	// controlling filter dialog pane's embedded FwFilterFullDlg.
	FilterWnd * m_pfltvw;
	// Handle to the size grip control used for resizing this dialog's window.
	HWND m_hwndGrip;
	// identifies the special accelerator table loaded to handle edit commands.
	int m_atid;
};


/*----------------------------------------------------------------------------------------------
	This class implements the header control embedded inside a FilterWnd filter table window.
	It is used in complex ("full") filters to resize the width of the columns within the filter.

	Hungarian: flthdr
----------------------------------------------------------------------------------------------*/
class FwFilterHeader : public AfWnd
{
typedef AfWnd SuperClass;

public:
	FwFilterHeader();
	~FwFilterHeader();

	void Create(HWND hwndPar, int wid, Rect & rc, HMENU hmenuPopup, FwFilterDlg * pfltdlg);

	int GetContextColumn()
	{
		return s_icolContext;
	}

	void RecalcToolTip();

	void ShowPopupMenu(int icol);

protected:
	enum
	{
		kdxpMargin = 4,
	};

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	virtual bool OnDrawThisItem(DRAWITEMSTRUCT * pdis);

	// This must be static. In the advanced filter, there is a case where the GetContextColumn
	// message gets sent to a different header control than the one that showed the popup
	// menu. This can occur if someone chooses a menu item when the edit window is open in the
	// list view to the left of the FwFilterDlg window. When the edit window closes, the filter
	// can switch places (because the name changed), which creates a new view window, which
	// creates a new header control. So, this has to be static so we return the right index.
	static int s_icolContext;

	// Handle to the popup menu that allows choosing what a column of the filter operates on.
	HMENU m_hmenuPopup;
	// Handle to the tooltip that shows that complete menu path selection for the filter column.
	HWND m_hwndToolTip;
	// Pointer to the enclosing complex ("full") filter editing dialog.
	FwFilterFullDlg * m_pfltf;
	// Pointer to the enclosing (or controlling) overall filter dialog pane.
	FwFilterDlg * m_pfltdlg;
};


/*----------------------------------------------------------------------------------------------
	This class implements a modal dialog that allows a user to edit the value for a simple
	filter in response to a prompt.  This window dynamically resizes as needed when it first
	becomes visible to accept a long prompt string.  The dialog will not become visible and
	will be closed before the user sees anything if the condition of the simple filter is
	'Empty' or 'Not empty' because there is nothing for the user to enter in these two cases.

	If the user defines a simple filter to use this feature, the dialog pops up every time
	the filter is applied.

	Hungarian: fltp
----------------------------------------------------------------------------------------------*/
class FwFilterPromptDlg : public AfDialog
{
typedef AfDialog SuperClass;

public:
	FwFilterPromptDlg();
	~FwFilterPromptDlg();

	void SetDialogValues(wchar * pszPrompt, AfLpInfo * plpi, HVO hvoFilter,
		FilterMenuNodeVec & vfmnColumn);
	void GetDialogValues(ITsString ** pptss);

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnApply(bool fClose);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	// Prompt displayed to the user for obtaining the filter value.
	StrUni m_stuPrompt;
	// Pointer to the language project information, used mostly to get database information.
	AfLpInfoPtr m_qlpi;
	// Database/cache ID of the filter being edited.
	HVO m_hvoFilter;
	// Vector of filter menu nodes that specify what this filter is looking at in the database.
	// These will identify a specific column in a specific table in the database, possibly
	// involving "joiner tables" in the process.
	FilterMenuNodeVec m_vfmn;
	// Pointer to a special possibility item type-ahead edit box that is used if the value
	// type is either a Possibility List or a Tag List.
	FwFilterPssEditPtr m_qfpe;
	// TsString containing the operator and filter value for this filter.  (Remember, TsString
	// allows for embedded object links, which is useful for this purpose.)  Only the value
	// proper is changed by this dialog.
	ITsStringPtr m_qtss;
	// Basic filter field type (Text, Ref, RefText, Date, ...)
	FilterFieldType m_fft;
	// Basic filter operator type: Empty, NotEmpty, Contains, DoesNotContain, ...
	FilterKeywordType m_fkt;
	// Flag whether this filter matches on hierarchical list subitems.
	bool m_fSubitems;
	// Stores the writing system for a multilingual string operation.
	int m_ws;
	// This stores the full date value for a date related filter criterion.
	SYSTEMTIME m_systime;
	// The control window for the edit box.
	TssEditPtr m_qte;
	// identifies the special accelerator table loaded to handle edit commands.
	int m_atid;
};


/*----------------------------------------------------------------------------------------------
	This class implements a trivial modeless top-level dialog that shows the user tips on how to
	create a complex ("full") filter.

	Hungarian: fltt
----------------------------------------------------------------------------------------------*/
class FwFilterTipsDlg : public AfDialog
{
typedef AfDialog SuperClass;

public:
	FwFilterTipsDlg()
	{
		m_rid = kridFilterTips;
	}

	void SetDialogValues(FwFilterFullDlg * pfltf)
	{
		AssertPtr(pfltf);
		m_pfltf = pfltf;
	}

	void Close()
	{
		OnCancel();
	}

protected:
	virtual bool OnApply(bool fClose)
	{
		Assert(fClose);
		m_pfltf->ShowTips(false);
		::EndDialog(m_hwnd, kctidOk);
		return true;
	}
	virtual bool OnCancel()
	{
		m_pfltf->ShowTips(false);
		return SuperClass::OnCancel();
	}
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	// Pointer to the complex ("full") filter editing dialog this is coordinating with.
	FwFilterFullDlg * m_pfltf;
};


/*----------------------------------------------------------------------------------------------
	This class implements a modal dialog that appears when the user selects a filter and there
	are no	matches (for whatever reason; user error, program error, or no matches).  It allows
	the user to turn off all filters, modify the current filter, or choose another filter.

	Hungarian: fltnm
----------------------------------------------------------------------------------------------*/
class FwFilterNoMatchDlg : public AfDialog
{
typedef AfDialog SuperClass;

public:
	FwFilterNoMatchDlg();
	~FwFilterNoMatchDlg();

	void SetDialogValues(int iflt, RecMainWnd * prmwMain);
	void GetDialogValues(int & iflt);
	void ClearNewFilterIndexes()
	{
		m_vifltNew.Clear();
	}
	Vector<int> & GetNewFilterIndexes()
	{
		return m_vifltNew;
	}
	void ClearFilterViewBars()
	{
		m_vpvwbrsFlt.Clear();
	}
	Vector<AfViewBarShell *> & GetFilterViewBars()
	{
		return m_vpvwbrsFlt;
	}

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);

	virtual void GetTlsOptDlg(TlsOptDlg ** pptod) = 0;
	virtual void SelectNewMenu(HMENU hmenuPopup) = 0;

	// Index of the currently chosen filter.
	int m_iflt;
	// Pointer to the application's main window, used for getting the language project
	// information or the handle of the main window.
	RecMainWnd * m_prmwMain;
	// Handle to a large font (14pt Sans Serif) used in the dialog display to get the user's
	// attention.
	HFONT m_hfontLarge;

	Vector<int> m_vifltNew;
	Vector<AfViewBarShell *> m_vpvwbrsFlt;
};


/*----------------------------------------------------------------------------------------------
	This class implements a modal dialog that should appear when the user performs an action
	that cannot be performed while any filters are active.  After showing this dialog, all
	filters are turned off if the modal return value is "kctidOk".

	Hungarian: fto
----------------------------------------------------------------------------------------------*/
class FwFilterTurnOffDlg : public AfDialog
{
typedef AfDialog SuperClass;

public:
	FwFilterTurnOffDlg();

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
};


/*----------------------------------------------------------------------------------------------
	This class implements a modal dialog that replaces the use of ::MessageBox() whenever a
	help button is wanted.

	Hungarian: fmsg
----------------------------------------------------------------------------------------------*/
class FwFilterErrorMsgDlg : public AfDialog
{
typedef AfDialog SuperClass;

public:
	FwFilterErrorMsgDlg();
	void Initialize(const achar * pszCaption, const achar * pszMessage,
		const achar * pszHelpUrl);

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);

	StrApp m_strCaption;
	StrApp m_strMessage;
};


/*----------------------------------------------------------------------------------------------
	This class implements a button that shows three dots.  It launches another dialog when
	the user clicks on the button.

	Hungarian: flb
----------------------------------------------------------------------------------------------*/
class FwFilterLaunchBtn : public AfWnd
{
public:
	void SubclassButton(HWND hwndButton);

protected:
	bool OnDrawThisItem(DRAWITEMSTRUCT * pdis);
};


/*----------------------------------------------------------------------------------------------
	This class implements an edit control that gives type-ahead capability for the items in a
	possibility list.

	Hungarian: fpe
----------------------------------------------------------------------------------------------*/
class FwFilterPssEdit : public AfWnd
{
typedef AfWnd SuperClass;

public:
	FwFilterPssEdit();

	void SubclassEdit(HWND hwndEdit, AfLpInfo * plpi)
	{
		Assert(hwndEdit);
		AssertPtr(plpi);
		SubclassHwnd(hwndEdit);
		m_qlpi = plpi;
	}

	void SetPss(HVO hvoPss, int ichMin = 0, AppOverlayInfo * paoi = NULL);
	void SetPssl(HVO hvoPssl, HVO hvoPss = NULL, AppOverlayInfo * paoi = NULL);
	void SetPssFromIndex(int ipss, int ichMin = 0);
	void Refresh(HVO hvo, PossNameType pnt);

	HVO GetPss()
	{
		return m_hvoPss;
	}

	PossNameType GetPossNameType()
	{
		return m_pnt;
	}

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnNotifyThis(int ctidFrom, NMHDR * pnmh, long & lnRet);

	// Pointer to the language project information, used mostly to get database information.
	AfLpInfoPtr m_qlpi;
	// Pointer to the possibility list information.
	PossListInfoPtr m_qpli;
	// Database ID of the selected possibility list item.
	HVO m_hvoPss;
	// Internal flag set in SetPssFromIndex, used to keep type-ahead expansion from occurring in
	// OnNotifyThis.
	bool m_fIgnoreChange;
	// Internal flag set in FWndProc, used to handle type-ahead backspaces in OnNotifyThis.
	bool m_fExtraBackspace;
	// Control how this possibility is displayed.
	PossNameType m_pnt;
};


/*----------------------------------------------------------------------------------------------
	This class is needed for a static control in the Simple Shell dialog.  Basically it is to
	keep the control from responding to the hotkey assigned to it so the window that really
	handles the hotkey will get the message.  This class is used with FwFilterButton to
	accomplish this.

	Hungarian: ffs
----------------------------------------------------------------------------------------------*/
class FwFilterStatic : public AfWnd
{
typedef AfWnd SuperClass;

public:
	void SubclassStatic(HWND hwndStatic)
	{
		Assert(hwndStatic);
		SubclassHwnd(hwndStatic);
	}

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
};


/*----------------------------------------------------------------------------------------------
	This class is needed for a button control in the Simple Shell dialog.  Basically it is to
	make the control respond to a hotkey that is assigned to a static control.  This class is
	used with FwFilterStatic to accomplish this.

	Hungarian: fbtn
----------------------------------------------------------------------------------------------*/
class FwFilterButton : public AfButton
{
typedef AfButton SuperClass;

public:
	void Create(HWND hwndPar, int wid, FwFilterDlg * pfltdlg);

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void GetCaption(HDC hdc, const Rect & rc, StrApp & strCaption);

	// Pointer to the enclosing overall filter dialog pane.
	FwFilterDlg * m_pfltdlg;
};


/*----------------------------------------------------------------------------------------------
	This class implements the main view constructor for the filter table.

	Hungarian: fvc.
----------------------------------------------------------------------------------------------*/
class FilterVc : public VwBaseVc
{
	typedef VwBaseVc SuperClass;

public:
	FilterVc();
	~FilterVc();

	//:> IVwViewConstructor methods.
	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(GetStrForGuid)(BSTR bstrGuid, ITsString ** pptss);
	STDMETHOD(DoHotLinkAction)(BSTR bstrData, HVO hvoOwner, PropTag tag, ITsString * ptss,
		int ichObj);

	void Init(AfDbInfo * pdbi, FwFilterFullDlg * pfltf, HWND hwndHeader, int dypHeader,
		IVwCacheDa * pvcd, FilterWnd * pfltvw);
	void OnEditSelChange(int icol, int irow, int ich)
	{
		m_icol = icol;
	}

	//:> This is needed by FilterUtil::InsertHotLink, which is called by DoHotLinkAction.
	bool FlushHotLinkCache(GUID & guid)
	{
		return m_hmhli.Delete(guid);
	}

protected:
	// Handle to the window containing the header control for the filter table.
	HWND m_hwndHeader;
	// Height of the window containing the header control for the filter table.
	int m_dypHeader;
	// Pointer to the application's database information / connection.
	AfDbInfoPtr m_qdbi;
	// Pointer to the complex ("full") filter editing dialog that contains the filter table
	// window.
	FwFilterFullDlg * m_pfltf;
	// Pointer to the filter table window used for displaying complex ("full") filters in the
	// controlling filter dialog pane's embedded FwFilterFullDlg.  The view embedded in this
	// window is what this view constructor constructs.
	FilterWnd * m_pfltvw;
	// Pointer to the private cache that contains all modifications to the set of filters.
	IVwCacheDaPtr m_qvcd;
	// Current column (field) in the filter being edited/displayed.
	int m_icol;

	struct HotLinkInfo
	{
		HVO m_hvo;
		PossNameType m_pnt;
		ITsStringPtr m_qtss;
	};
	// Mapping from database object GUIDs to Hot Link information (database ID / string) used
	// in the display of possibility list field values.
	HashMap<GUID, HotLinkInfo> m_hmhli;
};


/*----------------------------------------------------------------------------------------------
	This class implements the basic filter table window.

	Hungarian: fltvw.
----------------------------------------------------------------------------------------------*/
class FilterWnd : public AfVwScrollWnd
{
	typedef AfVwScrollWnd SuperClass;

public:
	FilterWnd();
	~FilterWnd();

	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb);

	FwFilterHeader * GetHeader()
		{ return m_qflthdr; }

	void Create(HWND hwndPar, int wid, HVO hvoFilter, IVwCacheDa * pvcd, HMENU hmenuPopup,
		FwFilterFullDlg * pfltf, FwFilterDlg * pfltdlg);

	void Reconstruct()
	{
		AssertPtr(m_qrootb);
		m_qrootb->Reconstruct();
		::InvalidateRect(m_hwnd, NULL, true);
	}

	// No reference count returned.
	IVwRootBox * GetRootBox()
	{
		return m_qrootb.Ptr();
	}

	void CopySelection(FilterWnd * pfltvw);
	bool RemoveEmptyRows(int irow = -1);
	void InsertIntoCell(ITsString * ptss);
	void ForwardEditSelChange();

	void RefreshPossibilityColumn(PossNameType pnt);

	/*------------------------------------------------------------------------------------------
		Set the current editing column into the embedded view constructor.

		@param icol Index of the selected column.
	------------------------------------------------------------------------------------------*/
	void SetEditColumn(int icol)
	{
		m_qfvc->OnEditSelChange(icol, 0, 0);
	}

	virtual void SelectAll();

protected:

	enum
	{
//		kwidFilter = 1000,
	};

	virtual void PostAttach();
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void GetScrollRect(int dx, int dy, Rect & rc);
	virtual COLORREF GetWindowColor()
		{ return ::GetSysColor(COLOR_3DFACE); }
	virtual int GetHorizMargin()
		{ return 0; }

	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual void OnHeaderTrack(NMHEADER * pnmh);
	virtual void OnReleasePtr();
	virtual void OnChar(UINT nChar, UINT nRepCnt, UINT nFlags);
	virtual void CallMouseUp(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot);
	virtual bool CallOnExtendedKey(int chw, VwShiftStatus ss);
	virtual bool OnCommand(int cid, int nc, HWND hctl);

	void OnCellChanged(UINT ch = 0);

	/*------------------------------------------------------------------------------------------
		Command handlers.
	------------------------------------------------------------------------------------------*/
	virtual bool CmdEditCut(Cmd * pcmd)
	{
		bool fT = CmdEditCut1(pcmd);
		OnCellChanged(VK_DELETE);
		return fT;
	}
	virtual bool CmsEditCopy(CmdState & cms)
		{ return CmsEditCopy1(cms); }
	virtual bool CmdEditPaste(Cmd * pcmd)
	{
		bool fT = CmdEditPaste1(pcmd);
		OnCellChanged();
		return fT;
	}
	virtual bool CmsEditPaste(CmdState & cms)
		{ return CmsEditPaste1(cms); }

	// Pointer to the embedded table header control.
	FwFilterHeaderPtr m_qflthdr;
	// Pointer to the private cache that contains all modifications to the set of filters.
	IVwCacheDaPtr m_qvcd;
	// Database/cache ID of the filter being edited.
	HVO m_hvoFilter;
	// Handle to the popup menu that allows choosing what a column of the filter operates on.
	HMENU m_hmenuPopup;
	// Pointer to the complex ("full") filter editing dialog that contains the filter table
	// window.
	FwFilterFullDlg * m_pfltf;
	// Pointer to the application's database information / connection.
	AfDbInfoPtr m_qdbi;
	// Pointer to the view constructor used for the embedded view root box.
	FilterVcPtr m_qfvc;
	// This is used when the filter edit dialog is expanded to a separate resizable window.
	FilterWndPtr m_qfltvwCopy;
	// Virtual key code for the mnemonic for the popup menu on the embedded header.
	achar m_ch;
	// Pointer to the enclosing (or controlling) overall filter dialog pane.
	FwFilterDlg * m_pfltdlg;

	CMD_MAP_DEC(FilterWnd);
};


/*----------------------------------------------------------------------------------------------
	This class creates a from clause for a SQL statement and a vector of aliases based on the
	columns that are added to it.

	Hungarian: fqb.
----------------------------------------------------------------------------------------------*/
class FromQueryBuilder
{
public:
	FromQueryBuilder(int ws, AfDbInfo * pdbi, FilterMenuNodeVec * pvfmn, int clidTarget,
		bool fSimple, FwFilterXrefUtil * pfxref);

	bool AddColumn(FilterMenuNodeVec & vqfmn, PossNameType pnt);

	void GetFromClause(StrUni & stuFromClause, Vector<StrUni> & vstuAlias);
	void GetTagLists(Vector<int> & vhvoTagPossList);

protected:
	void ClearFilterMenuNodes(FilterMenuNodeVec * pvfmn);
	int FindSimpleTextField(int clidOwner);
	int FindStructuredTextField(int clidOwner);

	// String containing the contructed from clause for an SQL query.
	StrUni m_stuFromClause;
	// Internal subselect used inside the from clause which selects the base rows from the
	// CmObject and desired subclass tables.
	StrUni m_stuBaseRows;
	// Vector of sets of SQL aliases for each of the database tables joined together in the
	// from clause.
	Vector<StrUni> m_vstuAlias;
	// The desired language writing system for multilingual strings.
	int m_ws;
	// Counter used for building unique aliases for each of the database tables joined
	// together in the from clause.
	int m_ialias;
	// The value of m_ialias for the most recent Class node encountered in building the from
	// clause in AddColumn from the vector of filter menu nodes.
	int m_ialiasLastClass;
	// Pointer to the FieldWorks database metadata cache which describes the database schema.
	IFwMetaDataCachePtr m_qmdc;
	// Vector of database IDs for the possibility lists (or tag lists) referenced in the filter
	// definition and thus in the from clause.
	Vector<int> m_vhvoTagPossList;
	// Base class id of the filtered objects.
	int m_clidTarget;
	// Flag whether this is a simple (basic or monotype advanced) filter.
	bool m_fSingleClass;
	// Pointer to class containing functions for handling cross reference fields.
	FwFilterXrefUtil * m_pfxref;
	// Pointer to the database information object.
	AfDbInfoPtr m_qdbi;
};


/*----------------------------------------------------------------------------------------------
	This class creates a where clause for a SQL statement based on the contents of the cells in
	the filter table specified in the constructor.

	Hungarian: wqb.
----------------------------------------------------------------------------------------------*/
class WhereQueryBuilder
{
public:
	WhereQueryBuilder(Vector<StrUni> & vstuAlias, Vector<FilterMenuNodeVec> * pvvfmnColumns,
		Vector<PossNameType> & vpnt, AfLpInfo * plpi, int clidTarget,
		FwFilterXrefUtil * pfxref);

	bool BuildWhereClause(HVO hvoFilter);

	void GetWhereClause(StrUni & stuWhereClause);

	wchar * LookupSymbol(FilterKeywordType fkt);

protected:
	// String containing the constructed where clause for an SQL query.
	StrUni m_stuWhereClause;
	// Vector of sets of SQL aliases for each of the database tables joined together in the
	// from clause, which may need to be referenced in the where clause.
	Vector<StrUni> m_vstuAlias;
	// Pointer to a vector of vectors of filter menu nodes that specify what each column of this
	// filter is looking at in the database.  These will identify specific columns in specific
	// tables in the database, possibly involving "joiner tables" in the process.
	Vector<FilterMenuNodeVec> * m_pvvfmnColumns;
	// Vector of possibility list name types, one per column.  If the leaf node does not refer
	// to a possibility list item, the value is meaningless.
	Vector<PossNameType> & m_vpnt;
	// Internal object for converting between filter keyword types and their corresponding
	// strings.
	KeywordLookup m_kl;
	// Pointer to the language project information, used to get database information and to
	// load possibility lists.
	AfLpInfoPtr m_qlpi;
	// Base class id of the filtered objects.
	int m_clidTarget;

	bool GetCellSQL(ITsString * ptss, int icol, StrUni & stuCell);
	bool ParseConditionText(const wchar *& prgchKey, StrUni & stuText, bool & fMatchBegin,
		bool & fMatchEnd, int & ws);
	void ReparseConditionText(const wchar * prgchKey, StrUni & stuText, bool fBegin, bool fEnd);
	bool ParseEnumText(FilterMenuNode * pfmn, const wchar *& prgchKey, int * pisel);
	bool ParseDateText(const wchar *& prgchKey, int & nScope, int & nYear, int & nMonth, int & nDay);
	bool ParseIntegerText(const wchar *& prgchKey, int * pn);
	HVO GetNextObject(ITsString * ptss, const wchar * prgchMin, const wchar *& prgchKey);
	void AddItemsToString(StrUni & stuCell, HVO hvoPssl, FilterKeywordType fkt, HVO hvoPss);
	void GetSubitems(Vector<HVO> & vhvoSub, HVO hvoPssl, HVO hvoPss);
	// Pointer to class containing functions for handling cross reference fields.
	FwFilterXrefUtil * m_pfxref;
};

// Local Variables:
// mode:C++
// End: (These 3 lines are useful to Steve McConnel.)

#endif // !FW_FILTER_H
