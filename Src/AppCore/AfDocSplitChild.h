/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, SIL International. All rights reserved.

File: AfDocSplitChild.h
Responsibility: Randy Regnier
Last reviewed: never

Description:
	This file contains AfDocSplitChild.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDOCSPLITCHILD_H_INCLUDED
#define AFDOCSPLITCHILD_H_INCLUDED 1

/*----------------------------------------------------------------------------------------------
This class implements AfDocSplitChild
@h3{Hungarian: dsc}
----------------------------------------------------------------------------------------------*/
class AfDocSplitChild : public AfVwRecSplitChild
{
public:
	typedef AfVwSplitChild SuperClass;

	AfDocSplitChild(bool fScrollHoriz = false);
	virtual ~AfDocSplitChild();

	virtual bool CloseProj();
	virtual void OnReleasePtr();
	virtual bool CanPrintOnlySelection()
	{
		return true;
	}
	STDMETHOD(SelectionChanged)(IVwRootBox * prootb, IVwSelection * pvwselNew);
	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb, bool fPrintingSel);
	virtual void PrepareToShow();
	virtual void PrepareToHide();

protected:
	virtual void CallMouseDown(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
};


#endif // AFDOCSPLITCHILD_H_INCLUDED
