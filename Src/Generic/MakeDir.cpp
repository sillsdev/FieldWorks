/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: MakeDir.h
Responsibility:
Last reviewed:
	Function to create a directory, possibly creating a series of directories along the given
	path.  If any part of the path already exists, that is not a problem.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	This is called recursively to create all the directories in the given path. If any part of
	the path already exists, then that is not a problem.

	@param pszPath Path whose directories are to be created.
	@return False if path did not exist and could not be created, otherwise True.
----------------------------------------------------------------------------------------------*/
bool MakeDir(const achar * pszPath)
{
	StrAppBufPath strbp(pszPath);

	// Look for last backslash:
	StrAppBuf strbSlash("\\");
	int ichStart = strbp.ReverseFindCh(strbSlash[0]);
	if (ichStart != -1)
	{
		// Make path comprising all except last component:
		StrAppBufPath strbpSmaller;
		strbpSmaller.Assign(strbp.Left(ichStart).Chars());

		// Check for recursion base case - no more backslashes:
		ichStart = strbpSmaller.ReverseFindCh(strbSlash[0]);
		if (ichStart != -1)
		{
			// Not base case, so continue:
			if (!MakeDir(strbpSmaller))
				return false;
		}
	}
	// If this next call fails, it may only be because the path already exists, so we will check
	// our overall success afterwards:
	_tmkdir(strbp.Chars());
	DWORD nFlags = GetFileAttributes(strbp.Chars());
	if (nFlags == INVALID_FILE_ATTRIBUTES || !(nFlags & FILE_ATTRIBUTE_DIRECTORY))
		return false;

	return true;
}
