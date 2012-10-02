/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, SIL International. All rights reserved.

File: AfBrowseSplitChild.h
Responsibility: Randy Regnier
Last reviewed: never

Description:
	This file contains AfBrowseSplitChild.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AF_BROWSE_SPLIT_CHILD_H_INCLUDED
#define AF_BROWSE_SPLIT_CHILD_H_INCLUDED 1

class AfBrowseSplitChild;
typedef GenSmartPtr<AfBrowseSplitChild> AfBrowseSplitChildPtr;

/*----------------------------------------------------------------------------------------------
This class implements AfBrowseSplitChild
@h3{Hungarian: bsc}
----------------------------------------------------------------------------------------------*/
class AfBrowseSplitChild : public AfVwRecSplitChild
{
public:
	typedef AfVwSplitChild SuperClass;

	AfBrowseSplitChild(bool fScrollHoriz = false);
	virtual ~AfBrowseSplitChild();

	virtual void PrepareToShow();
	virtual void PrepareToHide();
	virtual void PostAttach();
	virtual bool CloseProj();
	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb, bool fPrintingSel);
	virtual int GetPaneIndex();
	// Browse views don't need an overall horizontal margin because there is internal padding
	// in the cells. Also, it messes up the alignment with the column headers.
	virtual int GetHorizMargin()
	{
		return 0;
	}
	virtual bool CanPrintOnlySelection()
	{
		return true;
	}
	virtual void MakeSelVisAfterResize(bool fSelVis);
	virtual int SetRootSiteScrollInfo(int nBar, SCROLLINFO * psi, bool fRedraw);
	STDMETHOD(SelectionChanged)(IVwRootBox * prootb, IVwSelection * pvwselNew);
	virtual void GetScrollRect(int dx, int dy, Rect & rc);
	void OnHeaderTrack(NMHEADER * pnmh);
	virtual void SetPrimarySortKeyFlid(int flid)
	{
		m_qvcvc->SetPrimarySortKeyFlid(flid);
	}


protected:
	HWND m_hwndHeader;
	bool m_fColumnsModified;
	RecordSpecPtr m_qrsp;

	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void CallMouseDown(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot);
};


#endif // AF_BROWSE_SPLIT_CHILD_H_INCLUDED
