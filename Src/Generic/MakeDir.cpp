/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: MakeDir.h
Responsibility:
Last reviewed:
	Function to create a directory, possibly creating a series of directories along the given
	path.  If any part of the path already exists, that is not a problem.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"

#include <sys/stat.h>
#include <sys/types.h>

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
#if WIN32
	StrAppBufPath strbp(pszPath);

	// Look for last backslash:
	StrAppBuf strbSlash("\\");
#else
	StrAnsiBufPath strbp(pszPath);

	// Look for last slash:
	StrAnsiBuf strbSlash("/");
#endif
	int ichStart = strbp.ReverseFindCh(strbSlash[0]);
	if (ichStart > 0)
	{
		// Make path comprising all except last component:
		StrAppBufPath strbpSmaller;
		strbpSmaller.Assign(strbp.Left(ichStart).Chars());

		// Check for recursion base case - no more backslashes:
		ichStart = strbpSmaller.ReverseFindCh(strbSlash[0]);
		if (ichStart > 0)
		{
			if (!MakeDir(strbpSmaller))
				return false;
		}
	}
	// If this next call fails, it may only be because the path already exists, so we will check
	// our overall success afterwards:
#if WIN32
	_tmkdir(strbp.Chars());
	DWORD nFlags = GetFileAttributes(strbp.Chars());
	if (nFlags == INVALID_FILE_ATTRIBUTES || !(nFlags & FILE_ATTRIBUTE_DIRECTORY))
		return false;
#else
	mkdir(strbp.Chars(), 0777);

	struct stat filestats;
	bool statfailed = stat(strbp.Chars(), &filestats);
	if (!statfailed && !S_ISDIR(filestats.st_mode))
		return false;
#endif
	return true;
}
