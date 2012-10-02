/***********************************************************************************************
	This program creates an include file used by application resource files to form the
	version of the application. The form of the version number is as follows:
		First field: Major version
		Second Field:	Minor version
		Third Field:	Year, four digits.
		Fifth Field:	two digits for month, twodigits for day of month, one digit for
						build index.

		Example: 0.1.2000.04192

	The new file name, including path, and the build number is passed on the command line.
	The file is created and #defines are added to specify the information need in the
	resource file.

	Usage: mkverinc filename major_version minor_version build_index
/**********************************************************************************************/
#include <windows.h>
#include <stdio.h>

int main(int argc, char ** argv)
{
	int	ver[3] = {0,0,9}; // Default the build level to "developer build" - 9.

	// scan for the version parameters. Assumes Major, minor, bld_lvl in order.
	int cVerFound = 0;
	for (int iarg = 2; iarg < argc && cVerFound < 3; iarg++)
	{
		int verT;
		if (sscanf(argv[iarg], "%d", &verT) == 1)
			ver[cVerFound++] = verT;
	}

	if (argc < 4)
	{
		// Print a usage message, and pass everything from stdin to stdout without
		// logging anything to a file.
		printf("\n**********************************************************************\n");
		printf("Usage for mkverinc.exe:\n");
		printf("  mkverinc.exe - filename major_version minor_version build_index\n\n");
		printf("  filename - file name with full path of the file to create\n");
		printf("  major version[Optional]\n");
		printf("  minor version[Optional]\n");
		printf("  build_index[Optional] - index to add to the end of the version number\n");
		printf("**********************************************************************\n\n\n");
		exit(1);
	}

	SYSTEMTIME st;
	GetLocalTime(&st);
	FILE *fp;

	fp=fopen(argv[1],"w+");
	if (NULL == fp)
	{
		printf("Error: unable to create file : %s\n", argv[0]);
		exit(1);
	}

	// Write the data to the file
	fprintf(fp, "#define MAJOR_VERSION %02d\n", ver[0]);
	fprintf(fp, "#define MINOR_VERSION %02d\n", ver[1]);
	fprintf(fp, "#define YEAR %4d\n", st.wYear);
	fprintf(fp, "#define COPYRIGHT \"Copyright © 2002-%4d, SIL International\\0\"\n", st.wYear);
	fprintf(fp, "#define COPYRIGHTRESERVED \"Copyright © 2002-%4d, SIL International.  All rights reserved.\"\n", st.wYear);
	fprintf(fp, "#define DAY_MONTH_BUILDLVL %02d%02d%d\n", st.wMonth, st.wDay, ver[2]);
	fprintf(fp, "#define STR_PRODUCT \"%02d.%02d.%4d.%02d%02d%d\\0\"\n",
				ver[0], ver[1], st.wYear, st.wMonth, st.wDay, ver[2]);

	// Clean up.
	fclose(fp);
	return 0;
}