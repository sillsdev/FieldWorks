/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TlsOptCust.h
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Header file for the Custom tab in the Tools Options Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TLSOPTCUST_H_INCLUDED
#define TLSOPTCUST_H_INCLUDED

class TlsOptDlg;

typedef enum CapType
{
	kftOne = 0,		//One Item
	kftMulti,		//Multiple Items
	kftCapLim		//Count of CapacityTypes
};

typedef enum CstFldType
{
	kcftSingle = 0, // Multilingual or Monolingual string.
	kcftTxt, // StText object.
	kcftList, // Reference to a PossibilityItem set via a combo box.
	kcftDate, // Date
	kcftInt, // Integer.
} CstFldType; // Hungarian cft.


/*----------------------------------------------------------------------------------------------
	This class provides the functionality particular to the Custom tab for paragraphs.
	@h3{Hungarian: brdp}
----------------------------------------------------------------------------------------------*/
class TlsOptDlgCst : public AfDialogView
{
typedef AfDialogView SuperClass;
public:
	TlsOptDlgCst(TlsOptDlg * ptod);

void SetDialogValues(UserViewSpecVec & vuvs, Set<int> * psiwndClientDel,
	Set<int> * psiCustFldDel);

protected:
	// Member variables.
	int m_iuvs;
	int m_ito;
	BlockVec m_vpbsp;
	HWND m_hwndCstDfn;
	HWND m_hwndCstFld;
	HWND m_hwndCstDesc;
	HWND m_hwndCstType;
	HWND m_hwndCstLimit;
	HWND m_hwndCstLists;
	HWND m_hwndCstWS;
	IOleDbEncapPtr m_qode;
	HVO m_hvoPssl;

	bool m_dirty;
	int m_iCurSel; // This holds the current tree location when an edit to Desc begins.
	ITsStringPtr m_qtssNewDesc;  // tss of the text in the Desc Edit box during an edit.

	// Following variable is required to get the listview label edit to work correctly
	int m_iEditlParam; // lParam of item that is being label edited

	int m_iCboTyp;	// this hold the current selection of the Type combo.
	int m_nCboLimit; // this hold the current selection of the Limit combo.
	int m_nCboWS; // this hold the current selection of the Writing System combo.
	Vector<int> m_vCboWS;

	TlsOptDlg * m_ptod;
	Set<int> * m_psiwndClientDel;
	Set<int> * m_psiCustFldDel;
	int m_wsUser;		// user interface writing system id.

	// Methods
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	bool OnDefChange();
	bool OnAddFld(bool fCopyFld);
	bool OnDelFld();
	int TlsOptDlgCst::getWsIndex(int ws);
	bool UpdateCtrls();
	bool UpdateProperties(bool fDelete);
	void LoadListsCbo();
};

typedef GenSmartPtr<TlsOptDlgCst> TlsOptDlgCstPtr;


#endif  // !TLSOPTCUST_H_INCLUDED
