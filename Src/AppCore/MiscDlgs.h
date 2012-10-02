/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: MiscDlgs.h
Responsibility: Rand Burgett
Last reviewed: never

Description:
	Misc small dialogs that need a home.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef MISC_DLGS_H
#define MISC_DLGS_H 1

/*----------------------------------------------------------------------------------------------
	This class is the Poss list Merge dialog.
----------------------------------------------------------------------------------------------*/
class PossChsrMrg : public AfDialogView
{
	typedef AfDialogView SuperClass;
public:
	PossChsrMrg();

	virtual bool OnApply(bool fClose);
	void SetDialogValues(PossListInfo * ppli, HVO hvoSel);
	HVO PossChsrMrg::GetSelHvo()
	{
		return m_hvoDst;
	}

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnGetDispInfo(NMTVDISPINFO * pntdi);

	// Member variables.
	HVO m_hvoDst;
	HVO m_hvoSel;
	PossListInfoPtr m_qpli;
};

typedef GenSmartPtr<PossChsrMrg> PossChsrMrgPtr;

/*----------------------------------------------------------------------------------------------
	This class is the Missing Data dialog.
----------------------------------------------------------------------------------------------*/
class MssngDt : public AfDialogView
{
	typedef AfDialogView SuperClass;
public:
	MssngDt();
	/*------------------------------------------------------------------------------------------
		Sets the initial values for the dialog variables, prior to displaying the dialog. This
		method should be called after creating, but prior to calling DoModal.
	------------------------------------------------------------------------------------------*/
	void SetDialogValues(const achar * pszText, const achar * pszTitle)
	{
		m_strText = pszText;
		m_strTitle = pszTitle;
	}

	HVO GetButtonHvo()
	{
		return m_hvoButton;
	}

protected:
	// Methods
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);

	// Member variables.
	HVO m_hvoButton;
	StrApp m_strText;
	StrApp m_strTitle;
};

typedef GenSmartPtr<MssngDt> MssngDtPtr;


/*----------------------------------------------------------------------------------------------
	This class is the Poss list Delete dialog.
----------------------------------------------------------------------------------------------*/
class DeleteDlg : public AfDialogView
{
	typedef AfDialogView SuperClass;
public:
	DeleteDlg();

	void SetDialogValue(StrApp str)
	{
		m_str = str;
	}
	int GetDialogValue()
	{
		return m_nSel;
	}

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);

	// Member variables.
	int m_nSel;
	StrApp m_str;
};

typedef GenSmartPtr<DeleteDlg> DeleteDlgPtr;


/*----------------------------------------------------------------------------------------------
	This class is the Poss list Delete Object dialog.
----------------------------------------------------------------------------------------------*/
class DeleteObjDlg : public AfDialogView
{
	typedef AfDialog SuperClass;
public:
	DeleteObjDlg();

	void SetDialogValues(HVO hvo, ITsString * ptss, StrApp strObject, StrApp staSubObject,
		int clid, AfDbInfoPtr qdbi);

protected:
	// Methods
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);

	// Member variables.
	HVO m_hvoObj;
	ITsStringPtr m_qtss;
	StrApp m_strObject;
	StrApp m_strSubObject;
	int m_clid;
	AfDbInfoPtr m_qdbi;
};

typedef GenSmartPtr<DeleteObjDlg> DeleteObjDlgPtr;


/*----------------------------------------------------------------------------------------------
	This class is the Confirm Delete dialog.
----------------------------------------------------------------------------------------------*/
class ConfirmDeleteDlg : public AfDialog
{
	typedef AfDialog SuperClass;
public:
	ConfirmDeleteDlg();

	void SetTitle(StrApp str)
	{
		m_strTitle = str;
	}

	void SetPrompt(StrApp str)
	{
		m_strPrompt = str;
	}

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnActivate(bool fActivating, LPARAM lp);

	// Member variables
	StrApp m_strTitle;
	StrApp m_strPrompt;
};

typedef GenSmartPtr<ConfirmDeleteDlg> ConfirmDeleteDlgPtr;

#endif /*MISC_DLGS_H*/
