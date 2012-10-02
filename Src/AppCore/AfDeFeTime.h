/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeTime.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	Defines the AfDeFeTime class for displaying date and time.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDEFE_TIME_INCLUDED
#define AFDEFE_TIME_INCLUDED 1

/*----------------------------------------------------------------------------------------------
	This class provides functionality for a field editor to display the date and time in
	an uneditable field. It follows user locale information to format the date and time.
	Since the field is uneditable, the background is gray.
	@h3{Hungarian: deti}
----------------------------------------------------------------------------------------------*/
class AfDeFeTime : public AfDeFieldEditor
{
public:
	typedef AfDeFieldEditor SuperClass;

	AfDeFeTime();
	~AfDeFeTime();

	virtual void UpdateField();
	virtual void Init();
	void Draw(HDC hdc, const Rect & rcpClip);
	int SetHeightAt(int dxpWidth);
};

#endif // AFDEFE_TIME_INCLUDED
