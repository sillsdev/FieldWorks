/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: OpenProjDlg.h
Responsibility: Steve McConnel (was John Wimbish)
Last reviewed: Not yet.

Description:
	Header file for the File / Open Project dialog classes.

	OpenProjDlg : AfDialog
	ConfirmRemoveProjectDlg : AfDialog
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef OPENPROJDLG_H_INCLUDED
#define OPENPROJDLG_H_INCLUDED

achar * CreateDisplayableNetworkName(achar * pszSrc);

/*----------------------------------------------------------------------------------------------
	Dialog class for choosing a language project stored in a database.

	Hungarian: opd
----------------------------------------------------------------------------------------------*/
class OpenProjDlg : public AfDialog
{
	typedef AfDialog SuperClass;
public:
	OpenProjDlg();
	virtual ~OpenProjDlg()
	{
		if (m_himlProj)
		{
			AfGdi::ImageList_Destroy(m_himlProj);
			m_himlProj = NULL;
		}
		if (m_himlSubItem)
		{
			AfGdi::ImageList_Destroy(m_himlSubItem);
			m_himlSubItem = NULL;
		}
		m_vqstuDatabases.Clear();
	}
	void Init(IStream * fist, BSTR bstrCurrentServer, BSTR bstrLocalServer,
		BSTR bstrUserWs, ComBool fAllowMenu, int clidSubitem, BSTR bstrHelpFullUrl, int rid,
		ILgWritingSystemFactory * pwsf);
	bool GetSelectedProject(int & hvoProj, StrUni & stuProject, StrUni & stuDatabase,
		StrUni & stuMachine, GUID * guid);
	void OpenSubitem(int clid)
	{
		m_rid = kridOpenProjSubitemDlg;
		m_clidSubitem = clid;
	}
	bool GetSelectedSubitem(HVO & hvo, StrUni & stuName);
	void SetDlgValues(bool fAllowMenu)
	{
		m_fAllowMenu = fAllowMenu;
	}
	virtual bool OnHelp();
	virtual bool OnHelpInfo(HELPINFO * phi);

protected:
	/*------------------------------------------------------------------------------------------
		Class for building the list of subitems (typically Topics Lists).

		Hungarian: si
	------------------------------------------------------------------------------------------*/
	class SubitemInfo
	{
	public:
		SubitemInfo(HVO hvo, OLECHAR * pszName, HVO hvoOwner);
		SubitemInfo(const SubitemInfo & liSource);
		virtual ~SubitemInfo() {}
		int GetHvo() { return m_hvo; }
		const StrUni & GetName() { return m_stuName;}
		int GetOwner() { return m_hvoOwner; }
	protected:
		HVO m_hvo;
		StrUni m_stuName;
		HVO m_hvoOwner;
	};

	NetworkTreeViewPtr m_qntv;			// The network tree control
	HWND m_hwndProjList;				// For convenience, the project list control's handle.
	HWND m_hwndListList;				// For convenience, the list list control's handle.
	int m_clidSubitem;
	achar m_szLocalMachineName[256];	// For example, "LS-McConnelS".
	bool m_fConnected;					// If False, we couldn't connect to target machine.
	achar m_szServerName[256];			// The machine currently selected in the tree ctrl.
	StrUni m_stuServerName;				// Database server name based on m_szServerName.

	int m_hvoProj;						// Id of currently-selected project
	StrUni m_stuDatabase;				// Database name of currently-selected project
	StrUni m_stuProject;				// Name of the currently-selected project
	StrUni m_stuMachine;				// Machine containing currently-selected project
	GUID   m_guid;						// Guid of the currently-selected project
	bool m_fProjectIsSelected;			// If true, we've got a project in the list box
	bool m_fAllowMenu;
	IStreamPtr m_qfist;
	StrUni m_stuCurrentServer;
	StrUni m_stuLocalServer;
	StrUni m_stuUserWs;
	HIMAGELIST m_himlProj;				// Image list for project list box.
	HIMAGELIST m_himlSubItem;			// Image list for sub-item list box.

	HVO m_hvoSubitem;					// Database id of currently-selected subitem.
	StrUni m_stuSubitemName;			// Name of the currently-selected subitem.

	int m_iProj;							// Project selected for right-click popup menu.
	Vector<SubitemInfo> m_vsi;
	int m_iSubitem;
	virtual bool SetRemoveProjectState(HMENU hmenu);
	virtual bool OnRemoveProject();
	StrApp m_strHelpFullUrl; // Holds URL long enough to use it.
	ILgWritingSystemFactoryPtr m_qwsf;
	Vector<StrUni> m_vqstuDatabases;		// Databases on this machine.

	// nonpublic methods

	virtual bool OnRemoveList()
	{
		bool b;
		b = true;
		return false;
	};
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual void DoDataExchange(AfDataExchange * padx);

	void FillListControl(HTREEITEM hItem);
	void FillSubitemList(BSTR bstrServer, BSTR bstrDatabase);
	OLECHAR * GetWsColumnNameAndUserWs(IOleDbEncap * pode, int * pwsUser);
	OLECHAR * GetWsColumnNameAndUserWs(IOleDbEncap * pode, int * pwsUser, int * pnVer);
	void InsertSubitem(SubitemInfo & siT);
};
typedef GenSmartPtr<OpenProjDlg> OpenProjDlgPtr;

/*----------------------------------------------------------------------------------------------
	Dialog class for choosing a topics list from a language project stored in a database.

	Hungarian: copd
----------------------------------------------------------------------------------------------*/
class CleOpenProjDlg : public OpenProjDlg
{
typedef OpenProjDlg SuperClass;
public:
	CleOpenProjDlg() {}
	virtual ~CleOpenProjDlg() {}
protected:
	virtual bool SetRemoveListState(HMENU hmenu);
	virtual bool OnRemoveList();
};
typedef GenSmartPtr<CleOpenProjDlg> CleOpenProjDlgPtr;

/*----------------------------------------------------------------------------------------------
	Dialog class for confirming that the user wants to delete a language project database.  If
	it didn't have so much text, and a help button, we could almost get away with calling
	::MessageBox instead of having a special dialog.

	Hungarian: crp.
----------------------------------------------------------------------------------------------*/
class ConfirmRemoveProjectDlg : public AfDialog
{
	typedef AfDialog SuperClass;
public:
	ConfirmRemoveProjectDlg();
	void SetProjectName(const achar * pszProject)
	{
		m_strProject = pszProject;
	}
	void SetHelpFilename(const achar * pszHelpFilename)
	{
		m_strHelpFilename = pszHelpFilename;
	}

protected:
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnHelp();

	StrApp m_strProject;
	StrApp m_strHelpFilename;
};

typedef GenSmartPtr<ConfirmRemoveProjectDlg> ConfirmRemoveProjectDlgPtr;

/*----------------------------------------------------------------------------------------------
	Dialog class for confirming that the user wants to delete a subitem from a language project
	database.  If it didn't have so much text, and a help button, we could almost get away with
	calling ::MessageBox instead of having a special dialog.

	Hungarian: crps.
----------------------------------------------------------------------------------------------*/
class ConfirmRemoveProjectSubitemDlg : public AfDialog
{
	typedef AfDialog SuperClass;
public:
	ConfirmRemoveProjectSubitemDlg();
	void Initialize(const achar * pszProject, const achar * pszSubitem)
	{
		m_strProject = pszProject;
		m_strSubitem = pszSubitem;
	}

protected:
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);

	StrApp m_strProject;
	StrApp m_strSubitem;
};

typedef GenSmartPtr<ConfirmRemoveProjectSubitemDlg> ConfirmRemoveProjectSubitemDlgPtr;


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif	/*!OPENPROJDLG_H_INCLUDED*/
