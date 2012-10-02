/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfAnimateCtrl.h
Responsibility: Steve McConnel
Last reviewed: never

Description:
	Animation control class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFANIMATECTRL_H
#define AFANIMATECTRL_H 1

/*----------------------------------------------------------------------------------------------
	Object of this class play things like the roving flashlight animation.

	Hungarian: anim
----------------------------------------------------------------------------------------------*/
class AfAnimateCtrl
{
public:
	AfAnimateCtrl(HWND hwndParent, int kridAviClip, int cx, int cy);
	~AfAnimateCtrl();
	void StopAndRemove();
protected:
	HWND m_hwndCtrl;
};

#endif /*AFANIMATECTRL_H*/
