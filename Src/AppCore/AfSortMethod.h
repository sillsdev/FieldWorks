/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfSortMethod.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Header file for the Sort Method tab in the Tools/Options dialog.

	struct SortKeyInfo
	namespace SortMethodUtil
	class TlsOptDlgSort
	class AfSortMethodTurnOffDlg
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef SORT_METHOD_DLG_H
#define SORT_METHOD_DLG_H 1

class TlsOptDlgSort;
typedef GenSmartPtr<TlsOptDlgSort> TlsOptDlgSortPtr;

typedef enum
{
	kcollWin = 1,
	kcollICU,
} CollationScheme;

typedef enum
{
	ksrtDefault = 1,
	ksrtCaseInsensitive,
	ksrtAccentInsensitive,
	ksrtCaseAccentInsensitive
} WinCollationAttribute;

/*----------------------------------------------------------------------------------------------
	This data structure stores the information for one sort key of a sort method.
	Hungarian: ski
----------------------------------------------------------------------------------------------*/
struct SortKeyInfo
{
	Vector<int> m_vflid;	// Path to the sort key field (m_vflid[0] is actually a clid).
	int m_ws;				// Writing system of the sort key (zero for non-text fields).
	int m_coll;				// Collation choice of the sort key (zero for non-text fields).
	bool m_fReverse;		// Flag whether sort order is reversed.

	SortKeyInfo()
	{
		m_ws = 0;
		m_coll = 0;
		m_fReverse = 0;
	}
};

/*----------------------------------------------------------------------------------------------
	This namespace provides a central location for general utility methods related to sort
	methods.

	Hungarian: smu
----------------------------------------------------------------------------------------------*/
namespace SortMethodUtil
{
	bool LoadSortMethods(AfDbInfo * pdbi, const GUID * pguidApp);
	void ParseFieldPath(StrUni & stuFieldPath, Vector<int> & vflidPath);
	void CreateFieldPath(Vector<int> & vflidPath, StrUni & stuFieldPath);
	void BuildSqlPieces(AppSortInfo * pasi, const wchar * pszId, IFwMetaDataCache * pmdc,
		ILgWritingSystemFactory * pwsf, AppSortInfo * pasiXref, AppSortInfo * pasiDef,
		StrUni & stuTable, StrUni & stuAddSel, StrUni & stuJoin, StrUni & stuOrder);
	void GetSortQuery(AfLpInfo * plpi, AppSortInfo * pasi, HVO hvoTopLevel, int flidTop,
		int flidSub, AppSortInfo * pasiXref, AppSortInfo * pasiDef, StrUni & stuQuery);
	void GenerateStatusStrings(AppSortInfo * pasi, SortKeyHvos & skh, SortMenuNodeVec & vsmn,
		IVwCacheDa * pcvd, AfLpInfo * plpi, AppSortInfo * pasiXref,
		StrUni & stuKeyValue, StrUni & stuKeyName);
	void AdjustSortMethodForCrossReferences(AppSortInfo * pasiXref, IFwMetaDataCache * pmdc,
		Vector<SortKeyInfo> & vski);
	void CheckMultiOutput(AfDbInfo * pdbi, AppSortInfo & asi);
};



/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Sort Method dialog pane in the Tools Option
	dialog.
	Hungarian: tods
----------------------------------------------------------------------------------------------*/
class TlsOptDlgSort : public AfDialogView
{
typedef AfDialogView SuperClass;

public:
	TlsOptDlgSort(TlsOptDlg * ptod);
	~TlsOptDlgSort();

	/*------------------------------------------------------------------------------------------
		This enumerates the possible states of a sort method definition.
		Hungarian: sms
	------------------------------------------------------------------------------------------*/
	typedef enum
	{
		ksmsNormal = 0,
		ksmsModified,
		ksmsInserted,
		ksmsDeleted,
	} SortMethodState;

	// Define the of levels of sort keys.
	enum
	{
		kiskiPrimary = 0,
		kiskiSecondary = 1,
		kiskiTertiary = 2,
		kcski = 3
	};
	/*------------------------------------------------------------------------------------------
		This data structure stores the information for defining one sort method.
		Hungarian: smi
	------------------------------------------------------------------------------------------*/
	class SortMethodInfo
	{
	public:
		SortMethodInfo()
		{
			m_hvo = m_hvoOld = 0;
			m_sms = ksmsNormal;
		}
		StrUni m_stuName;
		bool m_fIncludeSubfields;	// Flag whether sorting/indexing includes subfields as well.

		SortKeyInfo m_rgski[kcski];	// Multilevel sort keys: m_rgski[0] = primary key.

		HVO m_hvo;					// The ID of the sort method in the dummy cache.
		HVO m_hvoOld;				// The ID of the sort method in the database.
		SortMethodState m_sms;
	};

	void SetDialogValues(RecMainWnd * prmwMain, int isrtInitial);
	void GetDialogValues(Vector<SortMethodInfo> & vsmi);
	bool WasModified()
	{
		return m_fModified;
	}

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool Apply();
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnEndLabelEdit(NMLVDISPINFO * plvdi, long & lnRet);

	//:>****************************************************************************************
	//:>	Command functions.
	//:>****************************************************************************************
	virtual bool CmdSortByFirstPopup(Cmd * pcmd);
	virtual bool CmdSortBySecondPopup(Cmd * pcmd);
	virtual bool CmdSortByThirdPopup(Cmd * pcmd);

	HIMAGELIST GetImageList();
	HMENU CreatePopupMenu(AfLpInfo * plpi, AfMainWnd * pafw);
	void InsertMenuNode(HMENU hmenu, SortMenuNode * psmn, int & cid, SortMenuNodeVec & vsmnFlat,
		bool fPushNodes);
	void BuildColumnVector(SortMenuNodeVec & vsmnFlat, int ismnFlat, SortMenuNodeVec & vsmn,
		bool fClear = true);
	void GetColumnName(SortMenuNodeVec & vsmn, SortMenuNodeType smntMin,
		SortMenuNodeType smntMax, StrApp & str);
	SortMenuNode * FindMenuNode(SortMenuNodeVec * pvsmn, int * rgflid, int cflid);
	void InitializeWithSortMethod(int isrt);
	void BuildPathNames(SortKeyInfo & ski, StrApp & strField, StrApp & strPath,
		StrApp & strWsColl);
	void UpdateSortMethodList();
	void InsertSortMethod(const wchar * pszName, SortMethodInfo * psmi);
	void DeleteSortMethod();
	void UpdateSortKeyInfo(SortMenuNodeVec & vsmn, SortKeyInfo & ski);
	void GetUpdateIdStrings(SortKeyInfo ski, StrUni & stuWs, StrUni & stuColl);
	int GetCollationIndex(int ws, int hvoColl);

	// Pointer to the enclosing Tools/Options dialog.
	TlsOptDlg * m_ptod;
	// Pointer to the application's main window, used mostly for getting the language project
	// information.
	RecMainWnd * m_prmwMain;
	// Vector of sort method information loaded from the database or created interactively by
	// the user, and saved to the database.
	Vector<SortMethodInfo> m_vsmi;
	// Index into m_vsmi for the initial sort method chosen for editing.
	int m_isrtInitial;
	// Index into m_vsmi for the current sort method chosen for editing.
	int m_isrtCurrent;
	// Flag that the sort methods have been modified, and thus need to be saved to the database.
	bool m_fModified;
	// Pointer to the private cache that contains all modifications to the set of sort methods.
	IVwCacheDaPtr m_qvcd;
	// Temporary cache used for loading/saving from/to the database.
	IVwOleDbDaPtr m_qodde;
	// Image list for storing sort method dialog images.
	HIMAGELIST m_himl;
	// Handle to the popup menu that allows choosing what we sort on.
	HMENU m_hmenuPopup;
	HMENU m_hmenuPopup2;
	// Handles to tooltips that show complete menu path selection for the Sort By buttons.
	HWND m_hwndToolTip;
	HWND m_hwndToolTip2;
	HWND m_hwndToolTip3;
	// Writing system factory used throughout the dialog code.
	ILgWritingSystemFactoryPtr m_qwsf;

	CMD_MAP_DEC(TlsOptDlgSort);
};

/*----------------------------------------------------------------------------------------------
	This class implements a modal dialog that should appear when the user performs an action
	that cannot be performed while any user defined sort methods are active.  After showing
	this dialog, the default sort methods is selected if the modal return value is "kctidOk".
	filters should be turned off.

	Hungarian: sto
----------------------------------------------------------------------------------------------*/
class AfSortMethodTurnOffDlg : public AfDialog
{
typedef AfDialog SuperClass;

public:
	AfSortMethodTurnOffDlg();

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
};
typedef GenSmartPtr<AfSortMethodTurnOffDlg> AfSortMethodTurnOffDlgPtr;

// Local Variables:
// mode:C++
// End: (These 3 lines are useful to Steve McConnel.)

#endif // !SORT_METHOD_DLG_H
