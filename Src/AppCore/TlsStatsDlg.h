/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TlsStatsDlg.h
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Header file for the Tool Reports Dialog class.

	TlsStatsDlg : AfDialog
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TlsStatsDlg_H_INCLUDED
#define TlsStatsDlg_H_INCLUDED

#include <map>
#include <algorithm>

class TlsStatsDlg;
class PossListInfo;
class PossItemInfo;
class AfLpInfo;
typedef GenSmartPtr<TlsStatsDlg> TlsStatsDlgPtr;
typedef GenSmartPtr<PossListInfo> PossListInfoPtr;

enum FragmentsStatList
{
	kfrTitle,
	kfrTableHeader,
	kfrTableData,
};
enum StatsOrderBy
{
	ksobAbbr,
	ksobName,
	ksobCount,
	ksobDefault
};
/*----------------------------------------------------------------------------------------------
	Implements the view constructor for the preview window
	@h3{Hungarian: frlpvc}
----------------------------------------------------------------------------------------------*/
class TlsStatsListVc : public VwBaseVc
{
	friend class PossItemComparer;
public:
	void InitValues(ITsTextProps * pttpFirst, ITsTextProps * pttpOther, TlsStatsDlg * ptsd)
	{
		AssertPtrN(pttpFirst);
		AssertPtrN(pttpOther);
		AssertPtr(ptsd);
		m_pttpFirst = pttpFirst;
		m_pttpOther = pttpOther;
		m_ptsd = ptsd;
	}
	STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag);
protected:
	ITsTextProps * m_pttpFirst; // TsTextProps for the first numbered para (may have start-at)
	ITsTextProps * m_pttpOther; // TsTextProps for other paragraphs (should not have start-at)
	TlsStatsDlg * m_ptsd; // Access to TlsStatsDlg data
	static int ItemsFor(PossItemInfo * ppii, TlsStatsDlg * ptsd);
};

DEFINE_COM_PTR(TlsStatsListVc);

/*----------------------------------------------------------------------------------------------
	@h3{Hungarian: tsl}
----------------------------------------------------------------------------------------------*/
class  TlsStatsList : public AfVwScrollWnd
{
	friend class TlsStatsListVc;
public:
	TlsStatsList(void);
	void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf, IVwRootBox ** pprootb);
	void InitValues(ITsTextProps * pttpFirst, ITsTextProps * pttpOther, TlsStatsDlg * ptsd)
	{
		AssertPtrN(pttpFirst);
		AssertPtrN(pttpOther);
		AssertPtr(ptsd);
		m_pttpFirst = pttpFirst;
		m_pttpOther = pttpOther;
		m_ptsd = ptsd;
	}
	bool GetShowAbbr();
	void SetShowAbbr(bool f);
	bool GetShowZero();
	void SetShowZero(bool f);
	bool GetIncludeSubitems();
	void SetIncludeSubitems(bool f);
	bool GetStatSortAsc();
	void SetStatSortAsc(bool f);

	void Create(HWND hwndPar, int wid, IVwCacheDa * pvcd, int wsUser);
	void Redraw();
protected:
	IVwCacheDaPtr m_qvcd;
	bool m_fShowAbbr;
	bool m_fDoNotShowZero;
	bool m_fIncludeSubitems; // Include List Subitems in Count // kcidTlsStatSub
	bool m_fStatSortAsc;
	bool m_fColumnsModified;
	ITsTextProps * m_pttpFirst; // TsTextProps for the first numbered para (may have start-at)
	ITsTextProps * m_pttpOther; // TsTextProps for other paragraphs (should not have start-at)
	TlsStatsListVcPtr m_qtslvc;
	TlsStatsDlg * m_ptsd;
};

DEFINE_COM_PTR(TlsStatsList);


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Reports Dialog.
	Hungarian: ptsd.
----------------------------------------------------------------------------------------------*/

class TlsStatsDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	TlsStatsDlg();
	void SetCanDoRtl(bool fCanDoRtl);
	void SetOuterRtl(bool f)
	{
		m_fOuterRtl = f;
	}

	~TlsStatsDlg()
	{
		ClearMap();
	}
	void SetDialogValues(int ws, Vector<HVO> & vpsslId, Vector<bool> & vfDisplaySettings);
	void GetDialogValues(HVO * psslSelId);
	virtual bool Synchronize(SyncInfo & sync);

protected:
	virtual bool OnSize(int wst, int dxp, int dyp);
	virtual bool OnSizing(int wse, RECT * prc);
	void GetCounts();
	void ClearMap();
	void UpdatePreview(DRAWITEMSTRUCT * pdis);
	bool OnDrawChildItem(DRAWITEMSTRUCT * pdis);
	int GetWs(HVO hvopssl);
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool LoadListCombo();
	bool LoadOrderByCombo();
	bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	bool FixDbName(StrUni stu, StrUni & stuDbName);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	struct StItemInfo
	{
		int cItems;
		int cChildren;
		HVO hvoParent;
	};

	// Member variables.
	IVwCacheDaPtr m_qvcd;
	int m_wsUser;
	bool m_fDummyItemDeleted;
	bool m_fOuterRtl;
	TlsStatsListPtr m_qtsl; // Actual item list window.
	HVO m_hvopsslSel;	// HVO of the selected list
	int m_wsMagic;			// Writing system
	Vector<HVO> m_vfactId; // Vector of the factory lists.
	Vector<bool> m_vfDisplaySettings;
	Vector<bool> m_vfDisplaySettings2;
	IOleDbEncapPtr m_qode;
	std::map<HVO, StItemInfo *> m_mapCount; // Holder for items in list with counts.
	AfLpInfo * m_plpi;
	StatsOrderBy m_sobStatOrderBy;

	HWND m_hwndList;	// Handle to the list box
	HWND m_hwndGrip;
	int m_yButtons; // distance from bottom of dialog to top of button
	int m_dxGroupBox; // total (right and left) distance to outside of dialog from group box
	int m_dyGroupBox; // distance from bottom of dialog to bottom of group box
	int m_dxStatList; // total (right and left) distance to outside of dialog from stats list
	int m_dyStatList; // distance from bottom of dialog to bottom of stats list
	int m_yMin;
	int m_xMin;

	friend TlsStatsListVc;
	friend class PossItemComparer;
};
#endif  // TlsStatsDlg_H_INCLUDED
