/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ListsPropDlg.h
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Header file for the Lists Properties Dialog class.

	ListsPropDlg : PropertiesDlg
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef ListsPropDLG_H_INCLUDED
#define ListsPropDLG_H_INCLUDED

/*----------------------------------------------------------------------------------------------
	Language project properties dialog.

	@h3{Hungarian: lppd}
----------------------------------------------------------------------------------------------*/
class ListsPropDlg : public PropertiesDlg
{
	typedef PropertiesDlg SuperClass;
public:
	ListsPropDlg();
	~ListsPropDlg();

	bool CheckName(const achar * pszName);
	void SetViewParams(int ilist, int iview)
	{
		m_ilist = ilist;
		m_iview = iview;
	}

	int GetListIndex()
	{
		return m_ilist;
	}

	int GetViewIndex()
	{
		return m_iview;
	}

protected:
	enum
	{
		kidlgGeneral = 0,
		kidlgWrtSys = 1,
	};

	int m_ilist;
	int m_iview;

	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
//	virtual bool OnHelpInfo(HELPINFO * phi);
	virtual bool OnApply(bool fClose);
	bool OnHelp();
};
typedef GenSmartPtr<ListsPropDlg> ListsPropDlgPtr;

/*----------------------------------------------------------------------------------------------
	This class provides the functionality particular to the Details tab for properties dialog.

	@h3{Hungarian: dtlp}
----------------------------------------------------------------------------------------------*/
class DetailsPropDlgTab : public AfDialogView
{
	typedef AfDialogView SuperClass;
public:
	DetailsPropDlgTab(PropertiesDlg * ppropd);
	~DetailsPropDlgTab();

	void EnableLocation(bool fEnable)
	{
		m_fLocationEnb = fEnable;
	}

	void EnableSize(bool fEnable)
	{
		m_fSizeEnb = fEnable;
	}

	void EnableModified(bool fEnable)
	{
		m_fModifiedEnb = fEnable;
	}

	void EnableDescription(bool fEnable)
	{
		m_fDescriptionEnb = fEnable;
	}

	virtual bool Apply();

protected:
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	void OnBrws();

	PropertiesDlg * m_ppropd;
	HFONT m_hfontLarge;
	bool m_fInitialized;
	bool m_fLocationEnb;
	bool m_fSizeEnb;
	bool m_fModifiedEnb;
	bool m_fDescriptionEnb;
};
typedef GenSmartPtr<DetailsPropDlgTab> DetailsPropDlgTabPtr;


#endif // !LISTSPROPDLG_H_INCLUDED
