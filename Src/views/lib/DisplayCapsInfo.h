/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2009-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: DisplayCapsInfo.h
Responsibility: Calgary
Last reviewed: Not yet.

Description:
	Provides fuctions to allow C++ access to graphics device capabilities.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once

// return current graphics devices Y dpi.
int GetDpiY(HDC hdc);

// return current graphics devices X dpi.
int GetDpiX(HDC hdc);
