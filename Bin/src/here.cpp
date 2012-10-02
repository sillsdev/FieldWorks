// here.cpp - source for the 'here' program
// Steve McConnel, 15-Jan-1998 (as requested by Shon Katzenberger)

#include <windows.h>
#include <stdio.h>
#include <string.h>
#include <assert.h>

int main(int argc, char ** argv)
{
	if ((argc < 2) || ((argc == 2) && (strcmp(argv[1], "?") == 0)))
	{
		fputs("\
usage: here relpath [\"set FOO=\"]\n\
	relpath is a relative file pathname that is appended to the absolute\n\
	pathname of the executable program (HERE.EXE).\n\
	The second argument is an optional string, usually a command for setting\n\
	an environment variable.\n\
	The concatenation of the (optional) second argument followed by the\n\
	full pathname incorporating the program location and the first argument\n\
	is written to the standard output, and can thus be redirected to a file.\n\
", stderr);
		return 1;
	}

	// don't compile this for Unicode!!

	assert(sizeof(TCHAR) == sizeof(char));

	// get the path to this executable

	char rgchHerePath[MAX_PATH+1];
	DWORD ctPathLength = GetModuleFileName(NULL, rgchHerePath, MAX_PATH);
	if (ctPathLength == 0)
	{
		fprintf(stderr, "Cannot determine \"here\" pathname\n");
		return 1;
	}
	rgchHerePath[ctPathLength] = '\0';

	// erase the last backslash in the path, effectively removing "\\HERE.EXE"

	char * p = strrchr(rgchHerePath, '\\');
	if (p != NULL)
		*p = '\0';

	// convert forward slashes in the argument to backslashes
	// (probably not needed, but easy to do)

	char * pszArg = argv[1];
	while ((p = strchr(pszArg, '/')) != NULL)
		*p = '\\';

	// put the "here" path and the argument together

	if (pszArg[0] != '\\')
		strncat(rgchHerePath, "\\", MAX_PATH);
	strncat(rgchHerePath, pszArg, MAX_PATH);
	rgchHerePath[MAX_PATH] = '\0';

	// fix the combined full path to remove any relative path elements

	char rgchFixedPath[MAX_PATH+1];
	ctPathLength = GetFullPathName(rgchHerePath, MAX_PATH, rgchFixedPath, &p);
	rgchFixedPath[ctPathLength] = '\0';

	// write the second argument followed by the absolute full pathname

	printf("%s%s\n", (argc > 2) ? argv[2] : "", rgchFixedPath);

	return 0;
}

// File settings for GNU Emacs (Please leave for Steve McConnel's sake!)
// Local Variables:
// mode:C++
// c-file-style:"cellar"
// compile-command:"nmake /nologo -f here.mak"
// tab-width:4
// End:
