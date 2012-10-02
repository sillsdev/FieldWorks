/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TreeDragDrop.cpp
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	This file contains code for the following classes:
		TreeDragDrop - A bolt-on package for any window which contains a treeview control.
		PossListDragDrop - A bolt-on package for any window which contains a Possibility List
			treeview control.

-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE


/***********************************************************************************************
	TreeDragDropBase methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
TreeDragDropBase::TreeDragDropBase()
{
	m_hwndTV = NULL;
	m_hwndParent = NULL;
	m_fDragging = false;
	m_himlDrag = NULL;
	m_nDragInitOffsetY = 0;
	m_fDropZoneActive = 0;
	InitDraggedItem();
	m_htiSource = NULL;
	m_htiTarget = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
TreeDragDropBase::~TreeDragDropBase()
{
	if (m_himlDrag)
	{
		AfGdi::ImageList_Destroy(m_himlDrag);
		m_himlDrag = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Initialization.
	@param hwndTV Tree view control window.
	@param hwndPar Parent window/dialog of tree view control.
	@param fDropOnTopAllowed True if dropzone 'on top' is allowed, as well as above or below.
----------------------------------------------------------------------------------------------*/
void TreeDragDropBase::Init(HWND hwndTV, HWND hwndPar, bool fDropOnTopAllowed)
{
	Assert(hwndTV);
	Assert(hwndPar);

	m_hwndTV = hwndTV;
	m_hwndParent = hwndPar;
	m_fDropOnTopAllowed = fDropOnTopAllowed;
}


/*----------------------------------------------------------------------------------------------
	Set the flag to say if an item can be dropped "on top" of another item i.e. whether the
	dropped item can be made a child of the target item, as opposed to forcing the dropped item
	to go before or after the target item.
	@param fDropOnTopAllowed True if dropzone 'on top' is allowed (as well as above or below).
----------------------------------------------------------------------------------------------*/
void TreeDragDropBase::SetDropOnTop(bool fDropOnTopAllowed)
{
	m_fDropOnTopAllowed = fDropOnTopAllowed;
}


/*----------------------------------------------------------------------------------------------
	Commence drag operation.
	@param pnmh Pointer to NMHDR structure given in TVN_BEGINDRAG notification message.
	@return True if dragging was started satisfactorily, otherwise false.
----------------------------------------------------------------------------------------------*/
bool TreeDragDropBase::BeginDrag(NMHDR * pnmh)
{
	AssertPtr(pnmh);
	Assert(pnmh->hwndFrom == m_hwndTV);

	NMTREEVIEW * pnmv = (NMTREEVIEW *)pnmh;
	if (!GetDraggedItem(pnmv, m_hwndTV))
		return false;

	// Get the bounding rectangle of the item being dragged (TreeView client coordinates):
	TreeView_GetItemRect(m_hwndTV, GetDraggedItemHandle(), &m_rectLastDrag, true);

	// Tell the tree view control to create an image to use for dragging:
	if (m_himlDrag)
	{
		AfGdi::ImageList_Destroy(m_himlDrag);
		m_himlDrag = NULL;
	}
	m_himlDrag = AfGdi::TreeView_CreateDragImageZ(m_hwndTV, GetDraggedItemHandle());
	if (!m_himlDrag)
	{
		// The tree view does not have its own images, so create a bitmap to look like the
		// selected item. This is done by getting the tree view to paint the relevant
		// portion of itself into our bitmap:
		HDC hdcTV = ::GetDC(m_hwndTV);
		if (hdcTV)
		{
			Rect rectTV;
			::GetClientRect(m_hwndTV, &rectTV);
			HDC hdcMem = AfGdi::CreateCompatibleDC(hdcTV);
			if (hdcMem)
			{
				// Create a bitmap the same size as the item to be dragged:
				HBITMAP hbmp = AfGdi::CreateCompatibleBitmap(hdcTV, m_rectLastDrag.Width(),
					m_rectLastDrag.Height());
				if (hbmp)
				{
					HBITMAP hbmpOld = AfGdi::SelectObjectBitmap(hdcMem, hbmp);
					// Redraw relevant bit of treeview into our bitmap, minus any tracking
					// highlighting:
					LONG nOldStyle = ::GetWindowLong(m_hwndTV, GWL_STYLE);
					::SetWindowLong(m_hwndTV, GWL_STYLE, nOldStyle & ~TVS_TRACKSELECT);
					::SetWindowOrgEx(hdcMem, m_rectLastDrag.left, m_rectLastDrag.top, NULL);
					::SendMessage(m_hwndTV, WM_PRINT, (WPARAM)hdcMem, (LPARAM)PRF_CLIENT);
					::SetWindowLong(m_hwndTV, GWL_STYLE, nOldStyle);
					// ENHANCE AlistairI: The bitmap will appear highlighted if the dragged item
					// was already selected, so figure out a way of undoing highlighting.

					// Disconnect our bitmap:
					AfGdi::SelectObjectBitmap(hdcMem, hbmpOld, AfGdi::OLD);

					// Make image list containing our bitmap:
					Assert(m_himlDrag == NULL);
					m_himlDrag = AfGdi::ImageList_Create(m_rectLastDrag.Width(),
						m_rectLastDrag.Height(), ILC_COLORDDB | ILC_MASK, 1, 1);
					if (m_himlDrag)
						ImageList_AddMasked(m_himlDrag, hbmp, kclrWhite);

					// Clean up:
					BOOL fSuccess;
					fSuccess = AfGdi::DeleteObjectBitmap(hbmp);
					Assert(fSuccess);
				}
				BOOL fSuccess;
				fSuccess = AfGdi::DeleteDC(hdcMem);
				Assert(fSuccess);
			}
			int iSuccess;
			iSuccess = ::ReleaseDC(m_hwndTV, hdcTV);
			Assert(iSuccess);
		}
	}
	if (!m_himlDrag)
	{
		FlushDraggedItem();
		return false;
	}

	// Start the drag operation. Set the hotspot of the dragged image to be the offset of the
	// mouse into the source item's rectangle (pnmv->ptDrag is in TreeView client coords):
	m_ptDragHotSpot.x = pnmv->ptDrag.x - m_rectLastDrag.left;
	m_ptDragHotSpot.y = pnmv->ptDrag.y - m_rectLastDrag.top;
	ImageList_BeginDrag(m_himlDrag, 0, m_ptDragHotSpot.x, m_ptDragHotSpot.y);
	// Show the image:
	ImageList_DragEnter(m_hwndTV, pnmv->ptDrag.x, pnmv->ptDrag.y);
	// Set the amount by which the mouse was off-center from the source item in the Y axis:
	m_nDragInitOffsetY = pnmv->ptDrag.y - m_rectLastDrag.top - m_rectLastDrag.Height() / 2;

	// Hide the mouse cursor, and capture mouse input:
	ShowCursor(false);
	SetCapture(m_hwndParent);
	m_fDragging = true;

	return true;
}


/*----------------------------------------------------------------------------------------------
	Respond to mouse movement during drag operation.
	@param nMouseX X coordinate of mouse position, given in WM_MOUSEMOVE notification message.
	@param nMouseY Y coordinate of mouse position, given in WM_MOUSEMOVE notification message.
	(WM_MOUSEMOVE coordinates are relative to parent window's client area.)
----------------------------------------------------------------------------------------------*/
void TreeDragDropBase::MouseMove(int nMouseX, int nMouseY)
{
	if (!m_fDragging)
		return;

	// Find out if the cursor is on an item. If it is, highlight the item as a drop
	// target:
	HTREEITEM htiTarget;  // handle to target item
	PossItemLocation pil; // Location of mouse relative to target item
	POINT ptWnd; // Mouse coordinates relative to treeview control window (not its client)
	POINT ptClnt; // Mouse coordinates relative to treeview control client
	Rect rect; // Bounding rectangle of target item
	Rect * prect = NULL;
	if (GetTargetItem(nMouseX, nMouseY, &htiTarget, &pil, &ptWnd, &ptClnt, &rect))
	{
		prect = &rect;
		// Modify area to be highlighted, depending on whether selection was towards top,
		// middle or bottom of target item:
		switch (pil)
		{
		case kpilBefore:
			rect.bottom = rect.top + 2;
			rect.top -= 1;
			break;
		case kpilUnder:
			break;
		case kpilAfter:
			rect.top = rect.bottom - 1;
			rect.bottom += 2;
			break;
		default:
			Assert(false);
			break;
		}
	}
	// Calculate rectangle for next drag image, in TreeView client coordinates:
	Rect rectNewDrag;
	rectNewDrag.left = ptClnt.x - m_ptDragHotSpot.x;
	rectNewDrag.top = ptClnt.y - m_ptDragHotSpot.y;
	rectNewDrag.right = rectNewDrag.left + m_rectLastDrag.Width();
	rectNewDrag.bottom = rectNewDrag.top + m_rectLastDrag.Height();

	// Update image:
	SetDropZone(prect, &m_rectLastDrag, &rectNewDrag);
	m_rectLastDrag = rectNewDrag;

	ImageList_DragMove(ptWnd.x, ptWnd.y);
}


/*----------------------------------------------------------------------------------------------
	Refresh the highlighted drop zone, if any.
	@param hdc The device context to draw into.
----------------------------------------------------------------------------------------------*/
void TreeDragDropBase::Paint(HDC hdc)
{
	if (m_fDragging && m_fDropZoneActive)
	{
		// Add drop zone highlighting (rect is in TreeView client coordinates):
		AfGfx::InvertRect(hdc, m_rectDropZone);
	}
}


/*----------------------------------------------------------------------------------------------
	Complete the drag operation by finding out which item the dragged item was dropped onto, and
	whereabouts on the dropped item it was dropped.
	@param nMouseX X coordinate of mouse position at drop (as in WM_LBUTTONUP message).
	@param nMouseY Y coordinate of mouse position at drop (as in WM_LBUTTONUP message).
	@return True if drop zone is a valid item.
	(WM_LBUTTONUP coordinates are relative to parent window's client area.)
----------------------------------------------------------------------------------------------*/
bool TreeDragDropBase::EndDrag(int nMouseX, int nMouseY)
{
	if (!m_fDragging)
		return false;

	Assert(GetDraggedItemHandle());
	m_htiSource = GetDraggedItemHandle();

	KillDrag();

	if (GetTargetItem(nMouseX, nMouseY, &m_htiTarget, &m_pilLocation))
	{
		if (m_htiTarget != m_htiSource)
			return true;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Terminate the dragging operation, and return display to normal.
----------------------------------------------------------------------------------------------*/
void TreeDragDropBase::KillDrag()
{
	if (m_fDragging)
	{
		Assert(m_hwndTV);
		ImageList_DragLeave(m_hwndTV);
		ImageList_EndDrag();
		SetDropZone(NULL);
		if (m_himlDrag)
		{
			AfGdi::ImageList_Destroy(m_himlDrag);
			m_himlDrag = NULL;
		}
		ReleaseCapture();
		ShowCursor(true);
		FlushDraggedItem();
		m_fDragging = false;
	}
}


/*----------------------------------------------------------------------------------------------
	Determines which item, if any, lies at the current mouse coordinates, and whether the item
	has been targeted in its upper, middle or lower region.
	@param nMouseX [in] X coordinate of mouse (WM_MOUSEMOVE message, parent's client coords).
	@param nMouseY [in] Y coordinate of mouse (WM_MOUSEMOVE message, parent's client coords).
	@param phtiTarget [out] Handle of targeted item.
	@param ppil [out] Position of coordinates relative to target.
	@param pptW [out] Mouse coordinates relative to treeview control window (not its client).
	@param pptC [out] Mouse coordinates relative to treeview control client.
	@param prect [out] Bounding rectangle of target item relative to treeview control's client.
	@return True if an item was targeted, false otherwise.

	If the return value is false, all output except *pptW and *pptC is undefined.
	Any of the [out] arguments may be set to NULL (or omitted) if that information is not
	required.
----------------------------------------------------------------------------------------------*/
bool TreeDragDropBase::GetTargetItem(int nMouseX, int nMouseY, HTREEITEM * phtiTarget,
	PossItemLocation * ppil, POINT * pptW, POINT * pptC, Rect * prect)
{
	Assert(m_hwndTV);

	TVHITTESTINFO tvht;  // hit test information

	// The mouse coordinates are relative to the origin of the parent's client area, so get
	// position of Treeview window and its client, relative to the client space of parent:
	Rect rectTV;
	::GetWindowRect(m_hwndTV, &rectTV);
	POINT ptOrgTV = { 0, 0 };
	::ClientToScreen(m_hwndTV, &ptOrgTV);
	POINT ptOrgParent = { 0, 0 };
	::ClientToScreen(m_hwndParent, &ptOrgParent);
	int nOrgTVClientX = ptOrgTV.x - ptOrgParent.x;
	int nOrgTVClientY = ptOrgTV.y - ptOrgParent.y;
	int nOrgTVWinX = rectTV.left - ptOrgParent.x;
	int nOrgTVWinY = rectTV.top - ptOrgParent.y;

	if (pptW)
	{
		// Calculate mouse coordinates relative to Treeview control's window:
		pptW->x = nMouseX - nOrgTVWinX;
		pptW->y = nMouseY - nOrgTVWinY;
	}
	int nMouseClientX = nMouseX - nOrgTVClientX;
	int nMouseClientY = nMouseY - nOrgTVClientY;
	if (pptC)
	{
		// Calculate mouse coordinates relative to Treeview control's window client:
		pptC->x = nMouseClientX;
		pptC->y = nMouseClientY;
	}
	// Find out if the cursor is on an item. For this, we need mouse coordinates relative to
	// client of treeview window:
	tvht.pt.x = nMouseClientX;
	// Make it appear that dragged item's center Y is the hot spot:
	tvht.pt.y = nMouseClientY - m_nDragInitOffsetY;

	HTREEITEM htiTarget = TreeView_HitTest(m_hwndTV, &tvht);
	if (phtiTarget)
		*phtiTarget = htiTarget;

	if (htiTarget == NULL)
		return false;

	// Get the bounding rectangle of the target item:
	Rect rect;
	if (TreeView_GetItemRect(m_hwndTV, htiTarget, &rect, true))
	{
		if (prect)
			*prect = rect;

		int nTargetHeight = rect.Height();

		// See if drop was near top, middle or bottom of target:
		PossItemLocation pil;
		if (DropOnTopAllowed())
		{
			// Allow drop before, after or on top of target:
			if (tvht.pt.y <= rect.top + nTargetHeight / 3)
			{
				// Top 1/3:
				pil = kpilBefore;
			}
			else if (tvht.pt.y >= rect.top + 2 * nTargetHeight / 3)
			{
				// Bottom 1/3:
				pil = kpilAfter;
			}
			else
			{
				// Middle 1/3:
				pil = kpilUnder;
			}
		}
		else
		{
			// Only allow drop before or after target:
			if (tvht.pt.y <= rect.top + nTargetHeight / 2)
			{
				// Top 1/2:
				pil = kpilBefore;
			}
			else
			{
				// Bottom 1/2:
				pil = kpilAfter;
			}
		}
		if (ppil)
			*ppil = pil;
	}
	return true;
}


/*----------------------------------------------------------------------------------------------
	Update the region which shows where the drop target is. If either the previous region or the
	new region intersects one of the given drag rectangles, then the drag image is temporarily
	hidden during the update.
	@param prect The rectangle to be highlighted as the drop zone, NULL if zone is cleared.
	@param prectOldDrag The current position of the drag image. May be NULL.
	@param prectNewDrag The next position for the drag image. May be NULL.
	@return True if specified Rect was different from previous Rect.

	Note that the rectangles are in TreeView client coordinates.
----------------------------------------------------------------------------------------------*/
bool TreeDragDropBase::SetDropZone(Rect * prect, Rect * prectOldDrag, Rect * prectNewDrag)
{
	// Check if anything's changed:
	if (prect && m_fDropZoneActive)
		if (*prect == m_rectDropZone)
			return false;

	if (!prect && !m_fDropZoneActive)
		return false;

	// Check if given rectangle intersects Drag rectangles:
	bool fRemove = false;
	if (prect)
	{
		Rect rectDummy;
		if (prectOldDrag)
		{
			if (rectDummy.Intersect(*prect, *prectOldDrag))
				fRemove = true;
		}
		if (!fRemove && prectNewDrag)
		{
			if (rectDummy.Intersect(*prect, *prectNewDrag))
				fRemove = true;
		}
	}
	// Check if previous rectangle intersects Drag rectangles:
	if (m_fDropZoneActive)
	{
		Rect rectDummy;
		if (prectOldDrag)
		{
			if (rectDummy.Intersect(m_rectDropZone, *prectOldDrag))
				fRemove = true;
		}
		if (!fRemove && prectNewDrag)
		{
			if (rectDummy.Intersect(m_rectDropZone, *prectNewDrag))
				fRemove = true;
		}
	}

	if (fRemove)
	{
		// Remove drag image:
		ImageList_DragShowNolock(false);
	}

	// Check if zone was previously active:
	if (m_fDropZoneActive)
	{
		// Restore the previously highlighted zone:
		m_fDropZoneActive = false;
		Assert(m_hwndTV);
		::InvalidateRect(m_hwndTV, &m_rectDropZone, false);
		::UpdateWindow(m_hwndTV);
	}
	if (prect)
	{
		// Highlight the required area:
		m_fDropZoneActive = true;
		m_rectDropZone = *prect;
		Assert(m_hwndTV);
		HDC hdcTV = ::GetDC(m_hwndTV);
		AfGfx::InvertRect(hdcTV, m_rectDropZone);
		int iSuccess;
		iSuccess = ::ReleaseDC(m_hwndTV, hdcTV);
		Assert(iSuccess);
	}

	if (fRemove)
	{
		// Restore drag image:
		ImageList_DragShowNolock(true);
	}
	return true;
}



/***********************************************************************************************
	TreeDragDrop methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Initialize our data member.
----------------------------------------------------------------------------------------------*/
void TreeDragDrop::InitDraggedItem()
{
	m_tviDragged.pszText = NULL;
	m_tviDragged.hItem = NULL;
}

/*----------------------------------------------------------------------------------------------
	Reset our data member.
----------------------------------------------------------------------------------------------*/
void TreeDragDrop::FlushDraggedItem()
{
	delete[] m_tviDragged.pszText;
	InitDraggedItem();
}

/*----------------------------------------------------------------------------------------------
	Fill our data member from the specified tree, in response to a BeginDrag message.
	@param pnmv TreeView notification message received in BeginDrag message.
	@param hwndTV Handle to TreeView control.
----------------------------------------------------------------------------------------------*/
bool TreeDragDrop::GetDraggedItem(NMTREEVIEW * pnmv, HWND hwndTV)
{
	TVITEM *ptvi = &pnmv->itemNew;
	m_tviDragged.mask = TVIF_TEXT | TVIF_IMAGE | TVIF_PARAM | TVIF_STATE | TVIF_HANDLE |
		TVIF_SELECTEDIMAGE | TVIF_CHILDREN;
	m_tviDragged.hItem = ptvi->hItem;
	const int nBufSize = 256;
	m_tviDragged.pszText = NewObj achar[nBufSize];
	m_tviDragged.cchTextMax = nBufSize;
	if (!TreeView_GetItem(hwndTV, &m_tviDragged))
	{
		delete[] m_tviDragged.pszText;
		m_tviDragged.pszText = NULL;
		return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Get Tree Item handle from our our data member.
----------------------------------------------------------------------------------------------*/
HTREEITEM TreeDragDrop::GetDraggedItemHandle()
{
	return m_tviDragged.hItem;
}

/*----------------------------------------------------------------------------------------------
	Get Tree Item Lparam from our our data member.
----------------------------------------------------------------------------------------------*/
LPARAM TreeDragDrop::GetDraggedItemLParam()
{
	return m_tviDragged.lParam;
}



/***********************************************************************************************
	TssTreeDragDrop methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Initialize our data member.
----------------------------------------------------------------------------------------------*/
void TssTreeDragDrop::InitDraggedItem()
{
	m_fwtviDragged.hItem = NULL;
}

/*----------------------------------------------------------------------------------------------
	Reset our data member.
----------------------------------------------------------------------------------------------*/
void TssTreeDragDrop::FlushDraggedItem()
{
	m_fwtviDragged.qtss.Clear();
	InitDraggedItem();
}

/*----------------------------------------------------------------------------------------------
	Fill our data member from the specified tree, in response to a BeginDrag message.
	@param pnmv TreeView notification message received in BeginDrag message.
	@param hwndTV Handle to TreeView control.
----------------------------------------------------------------------------------------------*/
bool TssTreeDragDrop::GetDraggedItem(NMTREEVIEW * pnmv, HWND hwndTV)
{
	FW_TVITEM *pfwtvi = reinterpret_cast <FW_TVITEM *> (&pnmv->itemNew);
	m_fwtviDragged.mask = TVIF_TEXT | TVIF_IMAGE | TVIF_PARAM | TVIF_STATE | TVIF_HANDLE |
		TVIF_SELECTEDIMAGE | TVIF_CHILDREN;
	m_fwtviDragged.hItem = pfwtvi->hItem;
	if (!TreeView_GetItem(hwndTV, &m_fwtviDragged))
		return false;

	return true;
}

/*----------------------------------------------------------------------------------------------
	Get Tree Item handle from our our data member.
----------------------------------------------------------------------------------------------*/
HTREEITEM TssTreeDragDrop::GetDraggedItemHandle()
{
	return m_fwtviDragged.hItem;
}

/*----------------------------------------------------------------------------------------------
	Get Tree Item Lparam from our our data member.
----------------------------------------------------------------------------------------------*/
LPARAM TssTreeDragDrop::GetDraggedItemLParam()
{
	FwTreeItem * pfti = (FwTreeItem *)(m_fwtviDragged.lParam);
	return pfti->lParam;
}



/***********************************************************************************************
	PossListDragDrop methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
PossListDragDrop::PossListDragDrop()
{
	m_ppli = NULL;
}


/*----------------------------------------------------------------------------------------------
	Initialization.
	@param ppli Possibility list to be used as data source.
	@param fLParamIsHVO True if Tree Item's lParam member is set to be data item's HVO.
	@param hwndTV Tree view control window.
	@param hwndPar Parent window/dialog of tree view control.

	If fLParamIsHVO is false, Tree Item's lParam member is assumed to be the index of the data
	item in the PossListInfo structure.
----------------------------------------------------------------------------------------------*/
void PossListDragDrop::Init(PossListInfo * ppli, bool fLParamIsHVO, HWND hwndTV, HWND hwndPar)
{
	AssertPtr(ppli);
	m_ppli = ppli;
	m_fLParamIsHVO = fLParamIsHVO;
	// See if the possibility list is to remain flat:
	bool fFlatList = (m_ppli->GetDepth() == 1);
	SuperClass::Init(hwndTV, hwndPar, !fFlatList);
}


/*----------------------------------------------------------------------------------------------
	Commence drag operation.
	@param pnmh Pointer to NMHDR structure given in TVN_BEGINDRAG notification message.
	@return True if dragging was started satisfactorily, otherwise false.
----------------------------------------------------------------------------------------------*/
bool PossListDragDrop::BeginDrag(NMHDR * pnmh)
{
	AssertPtr(m_ppli);

	// We mustn't allow drag and drop in non-editable or non-hierarchical sorted lists:
	if (m_ppli->GetIsClosed())
		return false;
	if (m_ppli->GetIsSorted() && m_ppli->GetDepth() == 1)
		return false;

	return SuperClass::BeginDrag(pnmh);
}


/*----------------------------------------------------------------------------------------------
	Complete the drag operation by moving the dropped item to its new place in the list.
	@param nMouseX X coordinate of mouse position at drop.
	@param nMouseY Y coordinate of mouse position at drop.
	@return True if data was altered.
----------------------------------------------------------------------------------------------*/
bool PossListDragDrop::EndDrag(int nMouseX, int nMouseY)
{
	if (SuperClass::EndDrag(nMouseX, nMouseY))
	{
		FW_TVITEM fwtvi;
		fwtvi.mask = TVIF_PARAM;
		fwtvi.hItem =  m_htiTarget;
		if (TreeView_GetItem(m_hwndTV, &fwtvi))
		{
			FwTreeItem * pfti = (FwTreeItem *)(fwtvi.lParam);
			if (m_fLParamIsHVO)
			{
				m_hvoTarget = pfti->lParam;
				m_hvoSource = GetDraggedItemLParam();
			}
			else
			{
				PossItemInfo * pii = m_ppli->GetPssFromIndex(pfti->lParam);
				m_hvoTarget = pii->GetPssId();
				pii = m_ppli->GetPssFromIndex(GetDraggedItemLParam());
				m_hvoSource = pii->GetPssId();
			}

			// Alter data to reflect change:
			return m_ppli->MoveItem(m_hvoSource, m_hvoTarget, m_pilLocation);
		}
	}
	return false;
}
