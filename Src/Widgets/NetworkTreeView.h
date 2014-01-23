/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: NetworkTreeView.h
Responsibility: John Wimbish
Last reviewed: Never

	This is a tree-view that shows the host computer's network environment.

	The tree expands as follows:

		Ac-wimbishj  (the host computer)
		Network Neighborhood
			Entire Network
				(Workgroup #1)
				(Workgroup #2)
				(Workgroup ...)
				(Workgroup #N
			(local workgroup computer #1)
			(local workgroup computer #2)
			(local workgroup computer ...)
			(local workgroup computer #N)

	On Windows 98, this is exactly the way that the network neighborhood is presented to the
	user (e.g., in the Windows Explorer.) In Windows 2000, unfortunately, there is a third
	node between "Entire Network" and the workgroups listing, called "Microsoft Windows
	Network". This NetworkTreeView control purposfully swallows up that extra node when
	running in Windows 2000, and thus presents the same tree whether running on Win98 or
	Windows 2000.

	As a side issue, there may be other types of networks that may yield different results,
	for which this code may someday need to be debugged.

	To use the class:

	1. Define a resource value in your Resourse.h that will represent the tree control, e.g.,

		#define kridWizProjNetworkTree 7435

	2. Define a tree control in your resource (*.rc) file, e.g.,

		CONTROL "Tree", kridWizProjNetworkTree, "SysTreeView32",
			TVS_HASLINES | TVS_DISABLEDRAGDROP | TVS_HASBUTTONS |
			TVS_SHOWSELALWAYS | WS_BORDER | TVS_LINESATROOT,
			92,36,170,100

	3. In your resource file, you'll need to include the resources needed for the control.
	Thus near the top of the file, add the following line:

		#include "..\..\Widgets\Res\NetworkTreeView.rc"

	4. Typically in your OnInitDlg method for your dialog, insert code that subclasses the
	standard tree view control, e.g.,

		HWND hwndNetworkTree = GetDlgItem(Hwnd(), kridWizProjNetworkTree);
		Assert(NULL != hwndNetworkTree);
		NetworkTreeViewPtr qntv;
		qntv.Create();
		qntv->SubclassTreeView(hwndNetworkTree);

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef NETWORKTREEVIEW_H
#define NETWORKTREEVIEW_H 1

/*----------------------------------------------------------------------------------------------
	Subclass of the Windows TreeView control a tree-view that shows the host computer's
	network environment.

	Hungarian: ntv.
----------------------------------------------------------------------------------------------*/
class NetworkTreeView : public AfWnd
{
typedef AfWnd SuperClass;
public:
	NetworkTreeView();
	~NetworkTreeView();
	virtual void SubclassTreeView(HWND hwnd);

	HTREEITEM InsertItem(HTREEITEM hParent, achar * pszText, int iImage = -1);
	HTREEITEM InsertItem(HTREEITEM hParent, uint rid, int iImage = -1);
	uint GetCount();

	static bool GetLocalMachineName(achar * pszLocalMachineName, uint c);

protected:
	void PreCreateHwnd(CREATESTRUCT & cs);
	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	bool OnNotifyThis(int id, NMHDR * pnmh, long & lnRet);

	bool _GetLocalMachineName();
	bool _PopulateTopNetworkNodes();
	bool _PopulateWorkgroups(HTREEITEM hTreeNeighborhood, NETRESOURCE *pnetrParent);
	void _ExpandGroupNode(TVITEM *item);
	void _GetItemText(HTREEITEM hItem, achar * pszText, int cText);
	bool _FindChildNetResource(achar * pszTarget, NETRESOURCE * pnetrParent,
		achar * pszBuffer, DWORD * cBufferSize);
	achar * _CreateDisplayableNetworkName(achar * pszSrc);

	HIMAGELIST m_himlTree;					// Image list used in the tree control
	achar m_szLocalMachineName[256];		// E,g, Ac-wimbishj
	HTREEITEM m_hTreeNeighborhood;
	HTREEITEM m_hTreeEntireNetwork;
	bool m_fNetworkPopulated;
};
typedef GenSmartPtr<NetworkTreeView> NetworkTreeViewPtr;

#endif //!NETWORKTREEVIEW_H
