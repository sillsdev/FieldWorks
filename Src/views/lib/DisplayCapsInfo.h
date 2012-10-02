/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2009 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

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
