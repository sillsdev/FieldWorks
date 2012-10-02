/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, SIL International. All rights reserved.

File: RnDocSplitChild.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This file contains RnDocSplitChild.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RnDocSplitChild_H_INCLUDED
#define RnDocSplitChild_H_INCLUDED 1

/*----------------------------------------------------------------------------------------------
This class implements RnDocSplitChild
@h3{Hungarian: dsc}
----------------------------------------------------------------------------------------------*/
class RnDocSplitChild : public AfDocSplitChild
{
	typedef AfDocSplitChild SuperClass;
public:
	bool CmdFindInDictionary(Cmd * pcmd);
	bool CmsFindInDictionary(CmdState & cms);
	virtual void AddExtraContextMenuItems(HMENU hmenuPopup);
	virtual bool OnContextMenu(HWND hwnd, Point pt);

protected:
	CMD_MAP_DEC(AfVwSplitChild);
};


#endif // RnDocSplitChild_H_INCLUDED
