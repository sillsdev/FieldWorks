/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2007, SIL International. All rights reserved.

File: RnBrowseSplitChild.h
Responsibility: Steve McConnel
Last reviewed: never

Description:
	This file contains RnBrowseSplitChild.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RnBrowseSplitChild_H_INCLUDED
#define RnBrowseSplitChild_H_INCLUDED 1

/*----------------------------------------------------------------------------------------------
This class implements RnBrowseSplitChild
@h3{Hungarian: rbsc}
----------------------------------------------------------------------------------------------*/
class RnBrowseSplitChild : public AfBrowseSplitChild
{
	typedef AfBrowseSplitChild SuperClass;
public:
	bool CmdFindInDictionary(Cmd * pcmd);
	bool CmsFindInDictionary(CmdState & cms);
	virtual void AddExtraContextMenuItems(HMENU hmenuPopup);
	virtual bool OnContextMenu(HWND hwnd, Point pt);

protected:
	CMD_MAP_DEC(AfBrowseSplitChild);
};


#endif // RnBrowseSplitChild_H_INCLUDED
