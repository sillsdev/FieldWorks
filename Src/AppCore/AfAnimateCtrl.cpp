/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfAnimateCtrl.cpp
Responsibility: Steve McConnel
Last reviewed:
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE
//:End Ignore

//:>********************************************************************************************
//:>	AfAnimateCtrl methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.  This creates and displays the indicated animation at the indicated location
	on the parent window.
----------------------------------------------------------------------------------------------*/
AfAnimateCtrl::AfAnimateCtrl(HWND hwndParent, int kridAviClip, int cx, int cy)
{
	m_hwndCtrl = Animate_Create(hwndParent, kridAviClip,
		WS_CHILD | WS_VISIBLE | ACS_AUTOPLAY | ACS_CENTER | ACS_TRANSPARENT,
		ModuleEntry::GetModuleHandle());
	if (NULL == m_hwndCtrl)
		return;
	::SetWindowPos(m_hwndCtrl, NULL, cx, cy, 60, 60, SWP_NOZORDER | SWP_DRAWFRAME);
	if (0 == Animate_Open(m_hwndCtrl, MAKEINTRESOURCE(kridAviClip)))
	{
		::DestroyWindow(m_hwndCtrl);
		m_hwndCtrl = NULL;
		return;
	}
	if (0 == Animate_Play(m_hwndCtrl, 0, -1, -1))
	{
		::DestroyWindow(m_hwndCtrl);
		m_hwndCtrl = NULL;
		return;
	}
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
AfAnimateCtrl::~AfAnimateCtrl()
{
	StopAndRemove();
}

/*----------------------------------------------------------------------------------------------
	Remove the animation from the screen.  This is essentially what the destructor does.
----------------------------------------------------------------------------------------------*/
void AfAnimateCtrl::StopAndRemove()
{
	if (m_hwndCtrl)
	{
		Animate_Stop(m_hwndCtrl);
		Animate_Close(m_hwndCtrl);
		::DestroyWindow(m_hwndCtrl);
		m_hwndCtrl = NULL;
	}
}
