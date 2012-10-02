/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2002, SIL International. All rights reserved.

File: RnDocSplitChild.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	This file contains RnDocSplitChild, which exists to handle the extra menu item
	Find In Dictionary.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE
	// Caution: Using ON_CID_ALL instead of ON_CID_CHILD will likely cause undesirable
	// effects when multiple windows are open. For example, if you open a second window, then
	// go back to the first window and change the font size using the dropdown menu and the
	// mouse, ON_CID_ALL results in the cursor jumping to the second window instead of
	// returning to the active window.
BEGIN_CMD_MAP(RnDocSplitChild)
	ON_CID_CHILD(kcidFindInDictionary, &RnDocSplitChild::CmdFindInDictionary, &RnDocSplitChild::CmsFindInDictionary)
END_CMD_MAP_NIL()


// Implements the Find In Dictionary command.
bool RnDocSplitChild::CmdFindInDictionary(Cmd * pcmd)
{
	if (!m_qrootb)
		return false;
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);
	return prmw->FindInDictionary(m_qrootb);
}

// Handle enabling the Find In Dictionary command.
bool RnDocSplitChild::CmsFindInDictionary(CmdState & cms)
{
	RecMainWnd * prmw = dynamic_cast<RecMainWnd *>(MainWindow());
	AssertPtr(prmw);
	return prmw->EnableCmdIfVernacularSelection(m_qrootb, cms);
}

void RnDocSplitChild::AddExtraContextMenuItems(HMENU hmenuPopup)
{
	StrApp strLabel(kstidFindInDictionary);
	::AppendMenu(hmenuPopup, MF_STRING, kcidFindInDictionary, strLabel.Chars());
}

bool RnDocSplitChild::OnContextMenu(HWND hwnd, Point pt)
{
	RnMainWnd * prmw = dynamic_cast<RnMainWnd *>(MainWindow());
	AssertPtr(prmw);
	return prmw->HandleContextMenu(hwnd, pt, m_qrootb, this);
}
