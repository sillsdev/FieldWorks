/***********************************************************************************************
	This program routes everything from the stdin stream to a given log file. It then passes
		everything to either the stdout stream or the stderr stream (if -e is given on the
		command line).

	Usage: Tee [-e] logfile

	If no log file is given, the output will still be redirected as specified but no log file
		will be created.
/**********************************************************************************************/
#include <stdio.h>

int main(int argc, char ** argv)
{
	FILE * pfile = NULL;
	bool fToError = false;

	if (argc > 1 && argc <= 3)
	{
		int iarg = 1;
		if ((argv[1][0] == '-' || argv[1][0] == '/') &&
			(argv[1][1] == 'e' || argv[1][1] == 'E'))
		{
			fToError = true;
			iarg++;
		}

		if (iarg < argc)
			pfile = fopen(argv[iarg], "w");
	}

	if (!pfile)
	{
		// Print a usage message, and pass everything from stdin to stdout without
		// logging anything to a file.
		printf("\n******************************************************\n");
		printf("Usage for Tee.exe:\n");
		printf("  xxx | Tee.exe [-e] logfile\n\n");
		printf("  -e        route stdin to stderr instead of stdout\n");
		printf("  logfile   filename to log stdin to\n");
		printf("******************************************************\n\n\n");
	}

	char ch;
	while ((ch = fgetc(stdin)) != EOF)
	{
		if (pfile)
			fputc(ch, pfile);
		if (fToError)
			fputc(ch, stderr);
		else
			fputc(ch, stdout);
	}

	if (pfile)
		fclose(pfile);
	return 0;
}