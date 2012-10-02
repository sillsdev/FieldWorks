#include <stdio.h>
#include <string.h>
/*----------------------------------------------------------------------------------------------
	Here's something you've all been waiting for:  the AT&T public domain source for getopt(3).
	It is the code which was given out at the 1985 UNIFORUM conference in Dallas.  I obtained it
	by electronic mail directly from AT&T.	The people there assure me that it is indeed in the
	public domain.
----------------------------------------------------------------------------------------------*/
int	opterr = 1;
int	optind = 1;
int	optopt;
char * optarg;
int getopt(int argc, char * const argv[], const char * opts)
{
	static const char szErrFmt[] = "%s: %s -- %c\n";
	static int ich = 1;
	int ch;
	char * pchOpt;

	if (ich == 1)
	{
		if (optind >= argc || argv[optind][0] != '-' || argv[optind][1] == '\0')
			return EOF;
		else if (strcmp(argv[optind], "--") == 0)
		{
			optind++;
			return EOF;
		}
	}
	optopt = ch = argv[optind][ich];
	if (ch == ':' || (pchOpt=strchr(opts, ch)) == NULL)
	{
		if (opterr)
			fprintf(stderr, szErrFmt, argv[0], "illegal option", ch);
		if (argv[optind][++ich] == '\0')
		{
			optind++;
			ich = 1;
		}
		return '?';
	}
	if (*++pchOpt == ':')
	{
		if (argv[optind][ich+1] != '\0')
			optarg = &argv[optind++][ich+1];
		else if (++optind >= argc)
		{
			if (opterr)
				fprintf(stderr, szErrFmt, argv[0], "option requires an argument", ch);
			ich = 1;
			return '?';
		}
		else
			optarg = argv[optind++];
		ich = 1;
	}
	else
	{
		if (argv[optind][++ich] == '\0')
		{
			ich = 1;
			optind++;
		}
		optarg = NULL;
	}
	return ch;
}
