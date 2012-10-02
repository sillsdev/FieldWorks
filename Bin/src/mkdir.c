#include <stdio.h>
#include <ctype.h>
#include <direct.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>

void usage()
{
printf("Usage: mkdir [-p] DIRECTORY ...\n");
exit( 1 );
}

/*****************************************************************************
 * NAME
 *    makedir
 * DESCRIPTION
 *    walk down a directory path, creating each element in turn, ignoring
 *    "already exists" type errors until the end of the path
 * RETURN VALUE
 *    0 if successful, 1 if the path does not exist when finished
 */
int makedir(char * pszPath)
{
int    iRet;
char * pSlash;
/*
 *  convert forward slashes to backslashes for simplicity later on
 */
while ((pSlash = strchr(pszPath, '/')) != NULL)
	*pSlash = '\\';

pSlash = strchr(pszPath, '\\');
if (	(pSlash != NULL)          &&
	((pSlash - pszPath) == 2) &&
	(pszPath[1] == ':')       &&
	isascii(pszPath[0])       &&
	isalpha(pszPath[0]))
	pSlash = strchr(pszPath+3, '\\');
while (pSlash)
	{
	*pSlash = '\0';
	if (pSlash[1] == '\0')
	break;
	iRet = _mkdir(pszPath);
	*pSlash = '\\';
	if ((iRet != 0) && (errno != EEXIST))
	goto bad_path;
	pSlash = strchr(pSlash+1, '\\');
	}
if (_mkdir(pszPath) == 0)
	return 0;

bad_path:
iRet = (errno == EEXIST) ? 0 : 1;
printf("mkdir cannot create directory %s: %s\n", pszPath, strerror(errno));
return 1;
}

/*****************************************************************************
 * NAME
 *    main
 * DESCRIPTION
 *    create one or more directories, even going multiple levels deep
 * RETURN VALUE
 *    number of errors encountered
 */
int main (int argc, char ** argv)
{
int i;
int errors = 0;
int idx;

if (argc < 2)
	usage();
idx = 1;
if (strcmp(argv[1], "-p") == 0)
	{
	idx = 2;
	if (argc < 3)
	usage();
	}
for ( i = idx ; i < argc ; ++i )
	errors += makedir( argv[i] );

return errors;
}
