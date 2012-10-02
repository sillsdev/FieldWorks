/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: NetworkTreeView.cpp
Responsibility: John Wimbish
Last reviewed: Never

	Implementation of NetworkTreeView. Refer the the header file for details.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

#define kMax 512   // Size for work/temprary string buffers

// ENHANCE: Ought to move this CWaitCursor class somewhere generic.
/*----------------------------------------------------------------------------------------------
	Sets the mouse to the hourglass wait cursor. The wait cursor stays in effect until
	the CWaitCursor object goes out of scope, or until RestoreCursor() is called.

	Hungarian: wait
----------------------------------------------------------------------------------------------*/
class CWaitCursor
{
public:
	CWaitCursor()
	{
		HCURSOR hWaitCursor = LoadCursor(NULL, IDC_WAIT);
		m_hOldCursor = (hWaitCursor != NULL) ? SetCursor(hWaitCursor) : NULL;
	}
	~CWaitCursor()
	{
		RestoreCursor();
	}
	void RestoreCursor()
	{
		if (m_hOldCursor)
			SetCursor(m_hOldCursor);
		m_hOldCursor = NULL;
	}
protected:
	HCURSOR m_hOldCursor;
};



/*----------------------------------------------------------------------------------------------
	Constructor.  Make sure the system supports the treeview control.
----------------------------------------------------------------------------------------------*/
NetworkTreeView::NetworkTreeView()
{
	Assert(_WIN32_IE >= 0x0300);
	m_himlTree = NULL;
	m_hTreeNeighborhood = NULL;
	m_hTreeEntireNetwork = NULL;
	m_fNetworkPopulated = false;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
NetworkTreeView::~NetworkTreeView()
{
	if (m_himlTree)
	{
		AfGdi::ImageList_Destroy(m_himlTree);
		m_himlTree = NULL;
	}
	m_hTreeNeighborhood = NULL;
	m_hTreeEntireNetwork = NULL;
}


/*----------------------------------------------------------------------------------------------
	Retrieves the name of the local host, placing it into the class member variable.

	Returns true if successful, false otherwise.
----------------------------------------------------------------------------------------------*/
bool NetworkTreeView::_GetLocalMachineName()
{
	return GetLocalMachineName(m_szLocalMachineName, sizeof(m_szLocalMachineName));
}


/*----------------------------------------------------------------------------------------------
	Returns the name of the local host. (E.g., retrieves "AC-WIMBISHJ".)

	Returns true if successful, false otherwise.
----------------------------------------------------------------------------------------------*/
bool NetworkTreeView::GetLocalMachineName(achar * pszLocalMachineName, uint c)
{
	WSADATA data;
	if (0 == WSAStartup(MAKEWORD(2, 0), &data))
	{
		Vector<char> vch;
		vch.Resize(c);
		int nResult = gethostname(vch.Begin(), c);
		if (WSACleanup() || nResult)
		{
			*pszLocalMachineName = '\0';
			return false;
		}
		if (sizeof(achar) == sizeof(char))
		{
			memcpy(pszLocalMachineName, vch.Begin(), c);
		}
		else
		{
			Assert(sizeof(achar) == sizeof(wchar));
			// Convert name to wide characters.
			StrUni stu(vch.Begin());
			int cch = stu.Length();
			if ((unsigned)stu.Length() >= c)
				cch = c - 1;
			memcpy(pszLocalMachineName, stu.Chars(), cch * sizeof(achar));
			pszLocalMachineName[cch] = 0;
		}
		return true;
	}
	return false;

}


/*----------------------------------------------------------------------------------------------
	This function must be called before the treeview control has been initialized with any data.
	(i.e. before any items are inserted into it.)
----------------------------------------------------------------------------------------------*/
void NetworkTreeView::SubclassTreeView(HWND hwnd)
{
	SubclassHwnd(hwnd);
	Assert(GetCount() == 0);

	// Initialize image list into the tree control
	if (!m_himlTree)
		m_himlTree = AfGdi::ImageList_Create(17, 17, ILC_COLORDDB | ILC_MASK, 5, 5);
	HBITMAP hbmpImageTree = AfGdi::LoadBitmap(ModuleEntry::GetModuleHandle(),
		MAKEINTRESOURCE(kridImagesNetwork));
	ImageList_AddMasked(m_himlTree, hbmpImageTree, RGB(255,255,255));
	AfGdi::DeleteObjectBitmap(hbmpImageTree);
	HIMAGELIST himlProjOld = TreeView_SetImageList(Hwnd(), m_himlTree, TVSIL_NORMAL);
	if (himlProjOld)
		AfGdi::ImageList_Destroy(himlProjOld);

	// Populate the network tree
	_PopulateTopNetworkNodes();
}

/*----------------------------------------------------------------------------------------------
	The top node of the network tree view consists of two items:
	1. The name of the computer,
	2. The Network Neighborhood node. This node  has two subnodes:
		a. The Entire Network node,
		b. The local context machines (e.g., machines in this workgroup).

	This method inserts these nodes, both the top and second levels.
----------------------------------------------------------------------------------------------*/
bool NetworkTreeView::_PopulateTopNetworkNodes()
{
	// Retrieve the local machine's name and insert into the tree
	_GetLocalMachineName();
	Assert(m_szLocalMachineName[0]);
	InsertItem(TVI_ROOT, m_szLocalMachineName, kridImageComputer);

	// Insert the Network Neighborhood node.
	m_hTreeNeighborhood = InsertItem(TVI_ROOT, kridNetNeighboorhood,
		kridImageNeighborhood);

	// Insert the Entire Network node.
	m_hTreeEntireNetwork = InsertItem(m_hTreeNeighborhood,kridNetEntireNetwork,
		kridImageEntireNetwork);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Fill in the items in the tree control with respect to the Entire Network node
	of the network hierarchy.
----------------------------------------------------------------------------------------------*/
bool NetworkTreeView::_PopulateWorkgroups(HTREEITEM hTreeEntireNetwork,
	NETRESOURCE * pnetrParent)
{
	// In Windows 2000 (unlike Windows 98) there is a "Micorosft Windows Network" node
	// in-between the "Entire Network" node and the various workgroups. Bother. Consistency
	// is too much to ask for. Anyway, we have to test for this, and if we find it we
	// call this method recursively so that we can just skip over the node and not have
	// it take up another mouse click in the UI.
	achar szMicrosoftWindowsNetwork[kMax];
	LoadString(ModuleEntry::GetModuleHandle(), kridNetWindowsNetwork,
		szMicrosoftWindowsNetwork, sizeof(szMicrosoftWindowsNetwork));

	// Enumerate the workgroups and insert into the tree
	HANDLE hEnum;
	if (NO_ERROR == WNetOpenEnum(RESOURCE_GLOBALNET, RESOURCETYPE_ANY, 0, pnetrParent, &hEnum))
	{
		DWORD dwCount = 1;
		char szBuffer[512];
		char *psz = szBuffer;
		DWORD dwBufferSize = sizeof(szBuffer);
		HTREEITEM hNewNode;

		while (NO_ERROR == WNetEnumResource(hEnum, &dwCount, &szBuffer, &dwBufferSize))
		{
			NETRESOURCE * pnetResource = (NETRESOURCE*)psz;

			if (NULL != pnetResource->lpRemoteName && * pnetResource->lpRemoteName)
			{
				if (0 == _tcscmp(pnetResource->lpRemoteName, szMicrosoftWindowsNetwork))
				{
					_PopulateWorkgroups(hTreeEntireNetwork, pnetResource);
				}
				else
				{
					hNewNode = InsertItem(hTreeEntireNetwork,
						_CreateDisplayableNetworkName(pnetResource->lpRemoteName),
						kridImageWorkgroup);

					InsertItem(hNewNode, _T("placeholder"), -1);
				}
			}
			dwBufferSize = sizeof(szBuffer);
		}
		WNetCloseEnum(hEnum);
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Given a tree item, retrieves the text of that item.

	Parameters:
		hItem   [in] The tree item whose text we want.
		pszText [out] The buffer into which to put the text.
		cText   [in] The size of the buffer which will receive the text.
----------------------------------------------------------------------------------------------*/
void NetworkTreeView::_GetItemText(HTREEITEM hItem, achar * pszText, int cText)
{
	Assert(hItem);
	TVITEM item;
	item.hItem = hItem;
	item.mask = TVIF_TEXT;
	item.pszText = pszText;
	item.cchTextMax = cText;
	TreeView_GetItem(Hwnd(), &item);
}

/*----------------------------------------------------------------------------------------------
	Given a target resource name, searches the parent node for a match, returning the results
	in the buffer as a NETRESOURCE. Usually the item we are searching for is in the
	lpRemoteName, but in some cases it is in the lpComment field of the NETRESOURCE.

	Parameters:
		pszTarget   [in] The target for which we are searching.
		pnetrParent [in] A pointer to the parent node within which we are searching. If this
							if NULL, then we search at the root of the network.
		pszBuffer   [out] A buffer into which to place the results, if found. The results
							are in the form of a NETRESOURCE structure.
		cBufferSize [in] The size of the pszBuffer.
					[out]The amount of pszBuffer actually occupied by the data (provided the
							target is found, of course.)

	Returns true if the target is found, false otherwise.
----------------------------------------------------------------------------------------------*/
bool NetworkTreeView::_FindChildNetResource(achar * pszTarget, NETRESOURCE * pnetrParent,
	achar * pszBuffer, DWORD * cBufferSize)
{
	bool bResult = false;
	DWORD dwScope = (pnetrParent == NULL) ? RESOURCE_CONTEXT : RESOURCE_GLOBALNET;
	HANDLE hEnum;

	if (NO_ERROR == WNetOpenEnum(dwScope, RESOURCETYPE_ANY, 0, pnetrParent, &hEnum))
	{
		DWORD dwCount = 1;
		int cBufferSizeOriginal = *cBufferSize;

		while (NO_ERROR == WNetEnumResource(hEnum, &dwCount, pszBuffer, cBufferSize))
		{
			NETRESOURCE *netResource = (NETRESOURCE*)pszBuffer;
			if ((netResource->lpRemoteName && !_tcsicmp(pszTarget, netResource->lpRemoteName))
				|| (netResource->lpComment && !_tcsicmp(pszTarget, netResource->lpComment)))
			{
				bResult = true;
				break;
			}
			*cBufferSize = cBufferSizeOriginal;
		}
		WNetCloseEnum(hEnum);
	}
	return bResult;
}


/*----------------------------------------------------------------------------------------------
	Called in response to the user clicking on a Group within the tree view of the network.
	The idea is to wait until the user clicks before filling in the contents of the node.
	Otherwise we'd have to fill in the entire tree, and this can take a very, very long time.
	The function tests to see if we have filled in the contents yet; and if not, does the
	work.
----------------------------------------------------------------------------------------------*/
void NetworkTreeView::_ExpandGroupNode(TVITEM *item)
{
	// Check to see if the item's image is a Group image. Otherwise, we are not at any node
	// that this function is concerned with.
	if (kridImageWorkgroup != item->iImage)
		return;
	CWaitCursor wait;

	// Now retrieve the first child. If there is no child, or if the child is not a
	// "placeholder", then we have nothing to do. If it is a placeholder, then get rid of it.
	HTREEITEM hChild = TreeView_GetChild(Hwnd(), item->hItem);
	if (NULL == hChild)
		return;
	achar szItemText[512];
	_GetItemText(hChild, szItemText, sizeof(szItemText));
	if (0 != _tcscmp(szItemText, _T("placeholder")))
		return;
	TreeView_DeleteItem(Hwnd(), hChild);

	// Get the text of the Group that we'll be expanding.
	_GetItemText(item->hItem, szItemText, sizeof(szItemText));

	// Get the parent netresource, by scanning down from the top of the hierarchy.
	achar szNetwork[512];
	DWORD cNetwork = sizeof(szNetwork);
	if (false == _FindChildNetResource(_T("Entire Network"), NULL, szNetwork, &cNetwork))
		return;

	achar szGroup[512];
	DWORD cGroup = sizeof(szGroup);
	if (false == _FindChildNetResource(szItemText, (NETRESOURCE*)szNetwork, szGroup, &cGroup))
	{
		// If we failed, we might be in Windows 2000; so try again, this time with the
		// "Microsoft Windows Network" as an intermediate node.
		achar szMicrosoftWindowsNetwork[512];
		DWORD cMicrosoftWindowsNetwork = sizeof(szMicrosoftWindowsNetwork);
		if (!_FindChildNetResource(_T("Microsoft Windows Network"), (NETRESOURCE *)szNetwork,
			szMicrosoftWindowsNetwork, &cMicrosoftWindowsNetwork))
		{
			return;
		}
		if (!_FindChildNetResource(szItemText, (NETRESOURCE*)szMicrosoftWindowsNetwork,
			szGroup, &cGroup))
		{
			return;
		}
	}

	// Finally, enumerate the machines into the tree node
	HANDLE hEnum;
	if (NO_ERROR == WNetOpenEnum(RESOURCE_GLOBALNET, RESOURCETYPE_ANY, 0,
		(NETRESOURCE*)szGroup, &hEnum))
	{
		DWORD dwCount = 1;
		char szBuffer[512];
		DWORD dwBufferSize = sizeof(szBuffer);

		while (NO_ERROR == WNetEnumResource(hEnum, &dwCount, &szBuffer, &dwBufferSize))
		{
			NETRESOURCE *netResource = (NETRESOURCE*)szBuffer;

			if (NULL != netResource->lpRemoteName && *netResource->lpRemoteName)
			{
				InsertItem(item->hItem,
					_CreateDisplayableNetworkName(netResource->lpRemoteName),
					kridImageComputer);
			}
			dwBufferSize = sizeof(szBuffer);
		}
		WNetCloseEnum(hEnum);
	}
}


/*----------------------------------------------------------------------------------------------
	Make certain we are creating it as a child window.
----------------------------------------------------------------------------------------------*/
void NetworkTreeView::PreCreateHwnd(CREATESTRUCT & cs)
{
	cs.style |= WS_CHILD;
}


/*----------------------------------------------------------------------------------------------
	Message handler
----------------------------------------------------------------------------------------------*/
bool NetworkTreeView::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	switch (wm)
	{
	case TVM_GETCOUNT:
		lnRet = GetCount();
		return true;

	default:
		return false;
	}
}


/*----------------------------------------------------------------------------------------------
	Notifications
----------------------------------------------------------------------------------------------*/
bool NetworkTreeView::OnNotifyThis(int id, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);
	Assert(pnmh->hwndFrom == m_hwnd);

	switch (pnmh->code)
	{
	// The user has clicked to expand a tree item. If the item is a Group in the network,
	// we need to populate that node of the tree before showing it to the user.
	case TVN_ITEMEXPANDING:
		{
			NMTREEVIEW * pntv = reinterpret_cast<NMTREEVIEW *>(pnmh);
			if (TVE_EXPAND == pntv->action)
			{
				if (!m_fNetworkPopulated && m_hTreeNeighborhood && m_hTreeEntireNetwork)
				{
					m_fNetworkPopulated = true;
					// Enumerate that Network Neighborhood node and insert into the tree
					HANDLE hEnum;
					if (WNetOpenEnum(RESOURCE_CONTEXT, RESOURCETYPE_ANY, 0, NULL, &hEnum) ==
						NO_ERROR)
					{
						DWORD dwCount = 1;
						char szBuffer[kMax];
						char *psz = szBuffer;
						DWORD dwBufferSize = sizeof(szBuffer);

						achar szEntireNetwork[kMax];
						LoadString(ModuleEntry::GetModuleHandle(), kridNetEntireNetwork,
							szEntireNetwork, sizeof(szEntireNetwork));

						while (WNetEnumResource(hEnum, &dwCount, &szBuffer, &dwBufferSize) ==
							NO_ERROR)
						{
							NETRESOURCE * pnetResource = (NETRESOURCE*)psz;

							// Recognize the Entire Network node and populate it.
							if (pnetResource->lpComment && !_tcscmp(szEntireNetwork,
								pnetResource->lpComment))
							{
								_PopulateWorkgroups(m_hTreeEntireNetwork, pnetResource);
							}

							// Otherwise populate the machines in this local context
							else if (pnetResource->lpRemoteName && *pnetResource->lpRemoteName)
							{
								InsertItem(m_hTreeNeighborhood,
									_CreateDisplayableNetworkName(pnetResource->lpRemoteName),
									kridImageComputer);
							}
							dwBufferSize = sizeof(szBuffer);
						}
						WNetCloseEnum(hEnum);
					}
					m_hTreeNeighborhood = NULL;
					m_hTreeEntireNetwork = NULL;
				}
				_ExpandGroupNode(&pntv->itemNew);
			}
		}
		break;

	default:
		return false;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Call this function to retrieve a count of the items in a tree view control.
----------------------------------------------------------------------------------------------*/
uint NetworkTreeView::GetCount()
{
	return SuperClass::DefWndProc(TVM_GETCOUNT, 0, 0);
}


/*----------------------------------------------------------------------------------------------
	Adding something to the tree. It inserts iImage and pszText under
	the hParent node as the last sibling.

	Parameters:
		hParent [in] The tree node that will be the parent of the item being inserted.
		pszText [in] The text of the item to insert.
		iImage  [in] The image of the item to insert. A value of -1 means that no image is
						inserted into the tree.
		rid     [in] The string resource to load.

	Returns the HTREEITEM of the newly inserted item, or NULL if it fails.
----------------------------------------------------------------------------------------------*/
HTREEITEM NetworkTreeView::InsertItem(HTREEITEM hParent, achar * pszText, int iImage)
{
	TV_INSERTSTRUCT is;
	is.hParent = hParent;
	is.hInsertAfter = TVI_LAST;
	is.item.mask = TVIF_TEXT;
	if (-1 != iImage)
		is.item.mask |= (TVIF_IMAGE | TVIF_SELECTEDIMAGE);
	is.item.pszText = pszText;
	is.item.cchTextMax = _tcslen(pszText);
	is.item.iImage = iImage;
	is.item.iSelectedImage = iImage;
	return TreeView_InsertItem(Hwnd(), &is);
}

HTREEITEM NetworkTreeView::InsertItem(HTREEITEM hParent, uint rid, int iImage)
{
	achar szBuffer[kMax];
	LoadString(ModuleEntry::GetModuleHandle(), rid, szBuffer, sizeof(szBuffer));
	return InsertItem(hParent, szBuffer, iImage);
}


/*----------------------------------------------------------------------------------------------
	For purposes of displaying in the tree, we remove the leading backslashes, and convert
	the name to lowercase (except for the initial letter.) The source string is actually
	converted, so its original (uppercase) state is lost.
----------------------------------------------------------------------------------------------*/
achar * NetworkTreeView::_CreateDisplayableNetworkName(achar * pszSrc)
{
	achar szBuffer[512];
	achar * pszStart = pszSrc;
	Assert(sizeof(szBuffer) > _tcslen(pszSrc));
	achar * pszDest = szBuffer;

	// Get rid of leading backslashes
	while (*pszSrc == '\\')
		++pszSrc;

	// Preserve the state of the first letter
	if (*pszSrc)
	{
		*pszDest++ = isascii(*pszSrc) ? (achar)toupper(*pszSrc) : *pszSrc;
		pszSrc++;
	}

	// Convert all of the remaining letters to lower case
	while (*pszSrc)
	{
		*pszDest++ = isascii(*pszSrc) ? (achar)tolower(*pszSrc) : *pszSrc;
		pszSrc++;
	}
	*pszDest = '\0';

	// The destination string cannot have grown, so this copy operation is safe.
	Assert(_tcslen(szBuffer) <= _tcslen(pszStart));
	_tcscpy_s(pszStart, _tcslen(pszStart) + 1, szBuffer);
	return pszStart;
}
