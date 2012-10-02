/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TreeDragDrop.h
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	This file contains declarations for the following classes to assist with Drag and Drop:
		TreeDragItem - A pure virtual base class for handling Windows and FW Widgets versions
		TreeDragDropBase - a generic base class to handle most of the work
		TreeDragDrop - A bolt-on package for any window which contains a Windows treeview
		TssTreeDragDrop - A bolt-on package for any window which contains an FW TssTreeView
		PossListDragDrop - A bolt-on package for any window which contains Possibility List tree

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TREE_DRAG_DROP_H
#define TREE_DRAG_DROP 1

#include "TssWidgets.h"


/*----------------------------------------------------------------------------------------------
	This pure virtual base class provides functionality for dealing with a dragged item
	structure.
----------------------------------------------------------------------------------------------*/
class TreeDragItem
{
public:
	virtual void InitDraggedItem() { }
	virtual void FlushDraggedItem() = 0;
	virtual bool GetDraggedItem(NMTREEVIEW * pnmv, HWND hwndTV) = 0;
	virtual HTREEITEM GetDraggedItemHandle() = 0;
	virtual LPARAM GetDraggedItemLParam() = 0;
};


/*----------------------------------------------------------------------------------------------
	This class provides generic drag and drop functionality.
----------------------------------------------------------------------------------------------*/
class TreeDragDropBase : public TreeDragItem
{
public:
	TreeDragDropBase();
	~TreeDragDropBase();
	void Init(HWND hwndTV, HWND hwndPar, bool fDropOnTopAllowed);
	void SetDropOnTop(bool fDropOnTopAllowed);
	virtual bool BeginDrag(NMHDR * pnmh);
	virtual void MouseMove(int nMouseX, int nMouseY);
	virtual void Paint(HDC hdc);
	virtual bool EndDrag(int nMouseX, int nMouseY);
	virtual void KillDrag();
	bool IsDragging()
	{
		return m_fDragging;
	}
	HTREEITEM GetSourceItem()
	{
		return m_htiSource;
	}
	HTREEITEM GetTargetItem()
	{
		return m_htiTarget;
	}
	PossItemLocation GetDropLocation()
	{
		return m_pilLocation;
	}
	virtual bool DropOnTopAllowed()
	{
		return m_fDropOnTopAllowed;
	}

protected:
	bool GetTargetItem(int nMouseX, int nMouseY, HTREEITEM * phtiTarget = NULL,
		PossItemLocation * ppil = NULL, POINT * pptW = NULL, POINT * pptC = NULL,
		Rect * prect = NULL);
	bool SetDropZone(Rect * prect, Rect * prectOldDrag = NULL, Rect * prectNewDrag = NULL);

	HWND m_hwndTV; // Handle to tree view control.
	HWND m_hwndParent; // Handle to parent window of tree view control.
	bool m_fDragging; // Flag to say if dragging operation is underway.
	HIMAGELIST m_himlDrag; // Image list containing dragged image.
	int m_nDragInitOffsetY; // Offset to make drag hotspot appear in vertical center of item.
	POINT m_ptDragHotSpot; // Offest of drag image hotspot from top left corner of image.
	Rect m_rectLastDrag; // Last position drag image was drawn (client coords).
	Rect m_rectDropZone; // Rectangle highlighting where drag will be dropped (client coords).
	bool m_fDropZoneActive; // Flag to say if a valid drop zone has been highlighted.
	HTREEITEM m_htiSource; // Handle of dragged item
	HTREEITEM m_htiTarget; // Handle of item dropped onto.
	PossItemLocation m_pilLocation; // Relative location of dropped item within drop target.
	bool m_fDropOnTopAllowed; // Flag to say if items can be dropped on top of others
};


/*----------------------------------------------------------------------------------------------
	This class provides drag and drop functionality for an arbitrary Windows tree view control.
	@h3{Hungarian: tdd}
----------------------------------------------------------------------------------------------*/
class TreeDragDrop : public TreeDragDropBase
{
public:
	virtual void InitDraggedItem();
	virtual void FlushDraggedItem();
	virtual bool GetDraggedItem(NMTREEVIEW * pnmv, HWND hwndTV);
	virtual HTREEITEM GetDraggedItemHandle();
	virtual LPARAM GetDraggedItemLParam();

protected:
	TVITEM m_tviDragged;
};


/*----------------------------------------------------------------------------------------------
	This class provides drag and drop functionality for an arbitrary TssTreeView control.
	@h3{Hungarian: tdd}
----------------------------------------------------------------------------------------------*/
class TssTreeDragDrop : public TreeDragDropBase
{
public:
	virtual void InitDraggedItem();
	virtual void FlushDraggedItem();
	virtual bool GetDraggedItem(NMTREEVIEW * pnmv, HWND hwndTV);
	virtual HTREEITEM GetDraggedItemHandle();
	virtual LPARAM GetDraggedItemLParam();

protected:
	FW_TVITEM m_fwtviDragged;
};


/*----------------------------------------------------------------------------------------------
	This class provides drag and drop functionality for Possibility List tree view control.
	@h3{Hungarian: pldd}
----------------------------------------------------------------------------------------------*/
class PossListDragDrop : public TssTreeDragDrop
{
	typedef TreeDragDropBase SuperClass;

public:
	PossListDragDrop();
	void Init(PossListInfo * ppli, bool fLParamIsHVO, HWND hwndTV, HWND hwndPar);
	virtual bool BeginDrag(NMHDR * pnmh);
	virtual bool EndDrag(int nMouseX, int nMouseY);
	HVO GetSourceHvo() { return m_hvoSource; }
	HVO GetTargetHvo() { return m_hvoTarget; }
	virtual bool DropOnTopAllowed()
	{
		return m_ppli->GetDepth() != 1;
	}

protected:
	PossListInfo * m_ppli; // Possibility list cache.
	bool m_fLParamIsHVO; // True if TreeView data item's lParam member is data HVO.
	HVO m_hvoSource; // Database Id of dragged item.
	HVO m_hvoTarget; // Database Id of receiving item (drop zone).
};

#endif // !TREE_DRAG_DROP_H
