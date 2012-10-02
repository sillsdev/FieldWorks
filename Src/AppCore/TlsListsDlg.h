/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TlsListsDlg.h
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Header file for the Tool Lists Dialog class.

	TlsListsDlg : AfDialog
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TlsListsDLG_H_INCLUDED
#define TlsListsDLG_H_INCLUDED

class TlsListsDlg;
class PossListInfo;
typedef GenSmartPtr<TlsListsDlg> TlsListsDlgPtr;
typedef GenSmartPtr<PossListInfo> PossListInfoPtr;

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Lists Dialog.
	Hungarian: ptlst.
----------------------------------------------------------------------------------------------*/

//class TlsListsDlg : public AfDialogView

class TlsListsDlg : public AfDialog
{
typedef AfDialog SuperClass;
public:
	TlsListsDlg();

	~TlsListsDlg()
	{
	}
	void SetDialogValues(HVO psslSelId, int ws, bool ffromCustFldDlg, Vector<HVO> & vpsslId);
	void GetDialogValues(HVO * psslSelId);
	virtual bool Synchronize(SyncInfo & sync);

protected:
	// Member variables.
	bool m_fPreventCancel; // This is used to prevent closing the window during a copy list.
	HWND m_hwndList;	// Handle to the list box
	HVO m_hvopsslSel;	// HVO of the selected list
	int m_wsMagic;			// Writing system
	bool m_ffromCustFldDlg; // True if this dialog was lanched from Custom fields dialog.
	Vector<HVO> m_vfactId; // Vector of the factory lists.
	IOleDbEncapPtr m_qode;
	bool m_fCustFldDirty; // Dirty flag for a custom field or custom list.

	int GetWs(HVO hvopssl);
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool LoadList();
	void UpdateCtrls();
	bool OnAddList(bool fcopyList);
	bool OnDelList();
	bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	bool FixDbName(StrUni stu, StrUni & stuDbName);
	void OnModify();
	void OnProperty();
	bool OnApply(bool fClose);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnCancel();
};

#endif  // !TlsListsDLG_H
