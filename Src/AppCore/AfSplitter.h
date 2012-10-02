/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfSplitter.h
Responsibility: Darrell Zook
Last reviewed:

Description:
	This file contains class declarations for the following classes:
		AfSplitFrame : AfWnd - This class embeds one or more AfSplitChild windows inside. It
			also creates a vertical scrollbar (AfSplitScroll) for each child window. It
			provides a way for the user to split a window horizontally.
		AfSplitChild : AfWnd - This window represents one pane inside of an AfSplitFrame window.
		AfSplitScroll: AfWnd - This class is used for the scrollbars inside an AfSplitFrame
			window.
	The scrollbars are not part of the AfSplitChild. Instead, they are managed by the
	AfSplitFrame window. When a window is split, a new AfSplitChild window is created as well
	as the corresponding AfSplitScroll. When the AfSplitChild window is destroyed, the
	corresponding AfSplitScroll window is destroyed as well.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFSPLITTER_H
#define AFSPLITTER_H 1

class AfSplitFrame;
class AfSplitChild;
class AfSplitScroll;
typedef GenSmartPtr<AfSplitFrame> AfSplitFramePtr;
typedef GenSmartPtr<AfSplitChild> AfSplitChildPtr;
typedef GenSmartPtr<AfSplitScroll> AfSplitScrollPtr;
class AfSplitterClientWnd;	// Forward reference.


/*----------------------------------------------------------------------------------------------
	The splitter child window. This abstract class must be overridden to provide the
	requested methods that allow the splitter frame to communicate with the child windows.

	Hungarian: splc
----------------------------------------------------------------------------------------------*/
class AfSplitChild : public AfWnd , public IDropSource, public IDropTarget
{
	typedef AfWnd SuperClass;

public:

	AfSplitChild(void);
	~AfSplitChild(void);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);

	// We have to be tricky here. There is an inherited reference count from AfWnd.
	// We want to go on existing as long as there are pointers to either interface.
	STDMETHOD_(ULONG, AddRef)(void)
	{
		AfWnd::AddRef();
		return m_cref;
	}

	STDMETHOD_(ULONG, Release)(void)
	{
		// AfDeFieldEditor::Release might delete this object, so we need to get the reference
		// count before calling it and subtract one.
		long cref = m_cref;
		AfWnd::Release();
		return ::InterlockedDecrement(&cref);
	}

	// IDropSource methods.
	STDMETHOD(QueryContinueDrag)(BOOL fEscapePressed, DWORD grfKeyState);
	STDMETHOD(GiveFeedback)(DWORD dwEffect);

	// IDropTarget methods.
	STDMETHOD(DragEnter)(IDataObject * pDataObject, DWORD grfKeyState, POINTL pt,
		DWORD * pdwEffect);
	STDMETHOD(DragOver)(DWORD grfKeyState, POINTL pt, DWORD * pdwEffect);
	STDMETHOD(DragLeave)(void);
	STDMETHOD(Drop)(IDataObject * pDataObject, DWORD grfKeyState, POINTL pt, DWORD * pdwEffect);

	virtual bool GetScrollInfo(int nBar, SCROLLINFO * psi);
	virtual int SetScrollInfo(int nBar, SCROLLINFO * psi, bool fRedraw);
	virtual bool CloseProj()
	{
		return true;
	}
	// This should be return false if it is not OK to save and close the window
	// or true otherwise. Prior to returning false, it should notify the user why it
	// is not OK to change.
	virtual bool IsOkToChange(bool fChkReq = false)
	{
		return true;
	}
	virtual void PrepareToShow() {}
	virtual void PrepareToHide() {}
	// This should be overridden if you need to do anything special to save data,
	// such as in data entry windows.
	// @return True if successful.
	virtual bool Save()
	{
		return true;
	}
	void GetScrollOffsets(int * pdxd, int * pdyd);

	enum
	{
		kdypMinPaneHeight = 15,
	};

	virtual int GetMinPaneHeight()
	{
		return kdypMinPaneHeight;
	}

	void ScrollBy(int dxdOffset, int dydOffset, Rect * prc);

	// Called when the frame window is gaining or losing activation.
	// @param fActivating true if gaining activation, false if losing activation.
	// @param hwnd handle to the frame window.
	virtual void OnPreActivate(bool fActivating) {}
	virtual void GetCurClsLevel(int * pclsid, int * pnLevel) {}
	virtual int GetLocation(Vector<HVO> & vhvo, Vector<int> & vflid)
	{
		return 0;
	}
	// Gets the object last edited.
	// @return ID of the last object edited.
	HVO GetLastObj()
	{
		return m_hvoLastObj;
	}

	// Sets the object last edited.
	// @param ID of the object last edited.
	void SetLastObj(HVO hvo)
	{
		m_hvoLastObj = hvo;
	}
	virtual bool Synchronize(SyncInfo & sync);
	virtual bool PreSynchronize(SyncInfo & sync)
	{
		return true;
	}
	virtual void SetPrimarySortKeyFlid(int flid) {}

protected:
	AfSplitFramePtr m_qsplf;

	//:>****************************************************************************************
	//:> Member variables used for drag and drop.
	//:>****************************************************************************************
	HVO m_hvoDrag; // The id of the object being dragged.
	int m_clidDrag; // The class id of the object being dragged.
	StrUni m_stuSvrDrag; // The name of the server holding m_hvoDrag.
	StrUni m_stuDbDrag; // The name of the database holding m_hvoDrag.
	ITsStringPtr m_qtssDrag; // The TsString representing m_hvoDrag.
	HIMAGELIST m_himlDrag; // Image list used for the drag image.
	int m_pidDrag; // The process id of the source process.
	// The object id for the previous field we had open (gets set by EndEdit).
	// This is primarily used to enable updating last date modified on records and subrecords.
	HVO m_hvoLastObj;

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void PostAttach(void);
	virtual bool OnSize(int wst, int dxp, int dyp);
	virtual void OnReleasePtr();
	AfSplitterClientWnd * GetSplitterClientWnd();
};


/*----------------------------------------------------------------------------------------------
	The splitter frame window. This class handles creating the child windows as needed. It also
	handles scroll bar details.

	Hungarian: splf
----------------------------------------------------------------------------------------------*/
class AfSplitFrame : public AfWnd
{
public:
	typedef AfWnd SuperClass;

	AfSplitFrame(bool fScrollHoriz = false);
	~AfSplitFrame(void);

	virtual void CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew) = 0;

	virtual bool GetScrollInfo(AfSplitChild * psplc, int nBar, SCROLLINFO * psi);
	virtual int SetScrollInfo(AfSplitChild * psplc, int nBar, SCROLLINFO * psi, bool fRedraw);
	virtual bool IsOkToChange(bool fChkReq = false)
	{
		return true;
	}
	void GetScrollOffsets(AfSplitChild * psplc, int * pdxd, int * pdyd);

	void SetCurrentPane(AfSplitChild * psplc);
	HWND GetScrollBarFromPane(AfSplitChild * psplc);

	inline AfSplitChild * GetPane(int iPane)
	{
		Assert(iPane == 0 || iPane == 1);
		return m_rgqsplc[iPane].Ptr();
	}
	inline AfSplitChild * CurrentPane()
	{
		return m_rgqsplc[m_iPaneWithFocus];
	}

	void SplitWindow(int ypSplit);
	void UnsplitWindow(bool fCloseBottom);

	int TopPaneHeight()
		{return m_dypTopPane;}

protected:
	AfSplitChildPtr m_rgqsplc[2];
	HWND m_rghwndScrollV[2];
	HWND m_hwndScrollH;
	bool m_fDragging;
	int m_ypLastDragPos;
	HBRUSH m_hbrHalfTone;
	int m_dypTopPane;
	int m_iPaneWithFocus;
	bool m_fScrollHoriz;

	enum
	{
		kdypSplitter = 7,
		kdypSplitterBar = 4,
		kypLastDragPosInvalid = -99,
	};

	void DrawGhostBar();

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void PostAttach(void);
	virtual bool OnSize(int wst, int dxp, int dyp);
	virtual bool OnPaint(HDC hdcDef);
	virtual bool OnMouseMove(UINT nFlags, Point pt);
	virtual bool OnLButtonDown(UINT nFlags, Point pt);
	virtual bool OnLButtonUp(UINT nFlags, Point pt);
	virtual bool OnHScroll(int wst, int yp, HWND hwndSbar);
	virtual bool OnVScroll(int wst, int yp, HWND hwndSbar);
	virtual bool OnKeyDown(WPARAM wp, LPARAM lp);

	virtual void OnReleasePtr();
};


/*----------------------------------------------------------------------------------------------
	This class provides a scroll bar for the splitter frame window. This is required because
	scrollbar controls do not pass scroll notifications to the parent window, even though MSDN
	says they do.

	Hungarian: spls
----------------------------------------------------------------------------------------------*/
class AfSplitScroll : public AfWnd
{
public:
	HWND Create(AfSplitFrame * psplf, DWORD dwStyle);

protected:
	AfSplitFramePtr m_qsplf;

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void OnReleasePtr();
};

#endif // !AFSPLITTER_H