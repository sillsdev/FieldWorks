/* Print color coded message to stderr (and possibly stdout), based on the environment variable
 * given on the command line.
 *
 * greenbar FW_BUILD_ERROR
 *		displays a message about the success or failure of the overall build.
 *
 * greenbar [FW_TEST_ERROR]
 *		displays a message about the success or failure of the C++ unit tests.
 *
 * This program works when compiled with cygwin gcc, but not with MSVC/C++.  (The cygwin runtime
 * appears to have the equivalent of the old ANSI.SYS built in.)
 */
#include <stdio.h>
#include <stdlib.h>
/***********************************************************************************************
 * NAME
 *    main
 * DESCRIPTION
 *    program to display colorful message about error status or lack thereof.
 * RETURN VALUE
 *    0 if environment variable was not set, or set to 0; otherwise 1.
 */
int main(int argc, char **argv)
{
	int i;
	int ret;
	const char * pszColor;
	const char * pszMsg;
	const char * pszRed = "\033[1;31m";		/* empirically determined values */
	const char * pszGreen = "\033[1;32m";
	const char * pszYellow = "\033[1;33m";
	const char * pszEndColor = "\033[0m";
	int cch;
	const char * pszEnvVar = "FW_BUILD_ERROR";
	if (argc > 1)
		pszEnvVar = argv[1];

	char * pszErr = getenv(pszEnvVar);

	if (!pszErr || !*pszErr || !strcmp(pszErr, "0"))
	{
		/* GREEN BAR! */
		pszColor = pszGreen;
		if (!strcmp(pszEnvVar, "FW_BUILD_ERROR"))
			pszMsg = "Success!                                                  ";
		else
			pszMsg = "***  All C++ Unit Tests PASSED!!! :-)  ***";
		ret = 0;
	}
	else if (!strcmp(pszEnvVar, "FW_TEST_ERROR") && !strcmp(pszErr, "2"))
	{
		/* YELLOW BAR! */
		pszColor = pszYellow;
		pszMsg = "*** Some C++ Unit Test failed to compile?? :-( ***";
		ret = 0;
	}
	else
	{
		/* RED BAR! */
		pszColor = pszRed;
		if (!strcmp(pszEnvVar, "FW_BUILD_ERROR"))
			pszMsg = "WARNING: There was at least one error somewhere.          ";
		else
			pszMsg = "***  Some C++ Unit Test FAILED!!! :-(  ***";
		ret = 1;
	}
	if (isatty(fileno(stderr)))
		fputs(pszColor, stderr);
	cch = strlen(pszMsg);
	for (i = 0; i < cch; ++i)
	{
		if (isatty(fileno(stderr)))
			fputc('*', stderr);
		if (!isatty(fileno(stdout)))
			fputc('*', stdout);
	}
	if (isatty(fileno(stderr)))
		fprintf(stderr, "\n%s\n", pszMsg);
	if (!isatty(fileno(stdout)))
		fprintf(stdout, "\n%s\n", pszMsg);
	for (i = 0; i < cch; ++i)
	{
		if (isatty(fileno(stderr)))
			fputc('*', stderr);
		if (!isatty(fileno(stdout)))
			fputc('*', stdout);
	}
	if (isatty(fileno(stderr)))
		fputc('\n', stderr);
	if (!isatty(fileno(stdout)))
		fputc('\n', stdout);
	if (isatty(fileno(stderr)))
		fputs(pszEndColor, stderr);
	return ret;
}

/* Local Variables:
 * compile-command:"gcc -o greenbar.exe greenbar.c"
 * mode:C++
 * End:
 */
