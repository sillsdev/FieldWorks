/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TlsOptDlg.h
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Header file for the core Tools Options Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TLSOPTDLG_H_INCLUDED
#define TLSOPTDLG_H_INCLUDED


// These objects are used to define the types of objects that will be displayed in the
// "Fields in" combo boxes in several displays.
struct TlsObject
{
	int m_clsid; // Class of the object.
	int m_nLevel; // Level of the object (e.g., 0 = main item, 1 = subitem).
	StrApp m_strName; // Name used in the combo boxes.
	StrApp m_strClsName; // Class Name String.
};


// These objects are used to define core display types that will be displayed in the
// "Views Add" options.
struct TlsView
{
	UserViewType m_vwt; // Identifies the view.
	// This item contains the master list of fields. (only one should have this.)
	bool m_fMaster;
};

// These objects are used to pass in dialog options.
struct TlsDlgValue
{
	int itabInitial;
	int clsid;
	int nLevel;
	int iv1;
	int iv2;
};


/*----------------------------------------------------------------------------------------------
	This class provides the core functionality for the Tools Options Dialog.
	@h3{Hungarian: tod}
----------------------------------------------------------------------------------------------*/


class TlsOptDlg : public AfDialog
{
	friend class TlsOptDlgVw;
	friend class TlsOptDlgCst;
	typedef AfDialog SuperClass;
public:
	TlsOptDlg();
	~TlsOptDlg();

	void SetVuvsCopy();
	virtual void SaveDialogValues() = 0;
	virtual void GetBlockVec(UserViewSpecVec & vuvs, int ivuvs, int vrt, RecordSpec ** pprsp);
	virtual void SetDialogValues(TlsDlgValue tgv);
	virtual FldVis GetCustVis(int vwt, int nrt)
	{
		return kFTVisAlways;
	}
	virtual bool CheckReqList(int flid)
	{
		return true;
	}
	virtual StrApp GetIncludeLabel()
	{
		StrApp str("");
		return str;
	}

	Vector<TlsObject> & ObjectVec()
	{
		return m_vto;
	}
	Vector<TlsObject> & CustDefInVec()
	{
		return m_vcdi;
	}
	Vector<TlsView> & ViewVec()
	{
		return m_vtv;
	}
	RecMainWnd * MainWnd()
	{
		return m_qrmw;
	}
	UserViewSpecVec & ViewSpecVec()
	{
		return m_vuvs;
	}
	int CurObjVecIndex()
	{
		return m_ivto;
	}
	void SetCurObjVecIndex(int ivto)
	{
		Assert((uint)ivto < (uint)m_vto.Size());
		m_ivto = ivto;
	}
	void SetCustFldDirty(bool fDirty)
	{
		m_fCustFldDirty = fDirty;
	}
	bool GetCustFldDirty()
	{
		return m_fCustFldDirty;
	}
	void FixName(StrApp & strName, HWND hwndList, bool fCopy);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	// Subclasses should override to return more appropriate index.
	virtual int GetInitialTabIndex(int cid)
	{
		return 0;
	}
	void SetMsrSys(MsrSysType nMsrSys)
	{
		m_nMsrSys = nMsrSys;
	}
	MsrSysType GetMsrSys()
	{
		return m_nMsrSys;
	}
	bool GetfShowFInCbo()
	{
		return m_vto.Size() > 1;
	}
	bool GetfShowCstDfnInCbo()
	{
		return m_vcdi.Size() > 2;
	}
	virtual int DefaultCstDfnIdx()
	{
		return m_iDefaultCstDfn;
	}
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
	SyncMsg GetSync()
	{
		return m_sync;
	}

protected:
	virtual void ProcessBrowseSpec(UserViewSpec * puvs, AfLpInfo * plpi);
	void CompleteBrowseRecordSpec(UserViewSpec * puvs, AfLpInfo * plpi);
	virtual void SetUserViewSpecs(UserViewSpecVec * pvuvs);
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool ShowChildDlg(int itab);
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnApply(bool fClose);
	virtual bool OnCancel();
	virtual bool OnHelp();
	virtual bool OnCommand(int cid, int nc, HWND hctl);

	virtual void SaveCustFlds();
	virtual void SaveViewValues(int iViewTab);
	virtual void SaveFilterValues(int imagFilterSimple, int imagFilterFull, int idlgFilters);
	virtual void SaveSortValues(int imagSort, int idlgSort);
	virtual void SaveOverlayValues(int imagOverlay, int idlgOverlays);
	bool FixDbName(StrUni stu, StrUni & stuDbName);

	// Member variables.
	int m_itabCurrent;
	Vector<AfDialogViewPtr> m_vdlgv;
	HWND m_hwndTab;
	int m_dxsClient;
	int m_dysClient;
	UserViewSpecVec m_vuvs;
	UserViewSpecVec m_vuvsOld;
	Set<int> m_siwndClientDel; // Index of client windows that were deleted/modified.
	Set<int> m_siCustFldDel; // Index of custom fields that were deleted.
	bool m_fCustFldDirty; // Flag true if custom fields have been modified;
	TlsDlgValue m_tgv;
	Vector<TlsObject> m_vcdi; // Custom field list of Define In items. .
	Vector<TlsObject> m_vto; // List of objects.
	int m_ivto; // The currently selected item in m_vto.
	Vector<TlsView> m_vtv; // List of view types.
	int m_cTabs; // The number of tabs in the dialog.
	RecMainWndPtr m_qrmw;
	// A map of RecordSpecs that define the blocks to show for each type of record.
	ClevRspMap m_hmclevrsp;
	int m_atid; // identifies the special accelerator table loaded to handle edit commands.
	// Measurement system that is used in this App.
	MsrSysType m_nMsrSys;  // temp copy to save value unless/until ok button is hit
	int m_iDefaultCstDfn; // Index to the default custom field "Define In" combo box.
	static LRESULT CALLBACK GetMsgProc(int code, WPARAM wParam, LPARAM lParam);
	static HHOOK s_hhook;
	SyncMsg m_sync; // Sync message depending on how serious a change we made.

	Vector<int> m_vifltNew;
	Vector<AfViewBarShell *> m_vpvwbrsFlt;
};

typedef GenSmartPtr<TlsOptDlg> TlsOptDlgPtr;

#endif  // !TLSOPTDLG_H_INCLUDED
