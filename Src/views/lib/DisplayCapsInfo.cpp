/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2009 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: DisplayCapsInfo.h
Responsibility: Calgary
Last reviewed: Not yet.

Description:
	Provides fuctions to allow C++ access to graphics device capabilities.
	Currently just stubs:
	TODO-Linux: Implement in terms of Win32 DC and cairo/X11.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#include <stdlib.h>

#define DEFAULT_X_DPI 96
#define DEFAULT_Y_DPI 96

#define ENV_X_DPI "DPIX"
#define ENV_Y_DPI "DPIY"

// Helper function to attempt to read number from an environement variable
int CheckNumEnv(const char * strEnv, int def)
{
	const char * strDpi = getenv(strEnv);
	if (strDpi != NULL)
	{
		int dpi = atoi(strDpi);
		if (dpi > 0)
			return dpi;
	}

	return def;
}

// return current graphics devices X dpi.
// HDC param is for future use.
int GetDpiX(HDC hdc)
{
	return CheckNumEnv(ENV_X_DPI, DEFAULT_X_DPI);
}

// return current graphics devices Y dpi.
// HDC param is for future use.
int GetDpiY(HDC hdc)
{
	return CheckNumEnv(ENV_Y_DPI, DEFAULT_Y_DPI);
}
