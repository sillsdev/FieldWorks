#include <windows.h>
#include <stdio.h>
#include <string.h>

// Macro to remove newlines and returns from end of strings:
#define crop(s) \
{ \
	int nLen = strlen(s); \
	while (s[nLen-1] == 10 || s[nLen-1] == 13) \
		s[nLen-1] = 0; \
}


// Variables needed throughout:
const char * pszTempFile = "_j_u_n_k";
char * pszClientFileName;
const int cchGeneral = 1024;
char rgchDepotFileName[cchGeneral] = { 0 };
int nHeadRevision;
char rgchCommand[cchGeneral];
char rgchParseLine[cchGeneral];

// Internal errors:
enum
{
	ErrHalt = -1000,
	ErrFlow = -1001,
	ErrRevMax = -1002,
	ErrTempFile = -1003,
};

// Structure containing line numbers where our substring exists in older versions:
struct OlderVersionData
{
	int * rgnLines;
	char ** rgpszLines;
	int nLines;

	OlderVersionData() : rgnLines(NULL), rgpszLines(NULL), nLines(0) { }
	~OlderVersionData() { Clear(); }
	void Clear()
	{
		for (int i = 0; i < nLines; i++)
			delete[] rgpszLines[i];
		delete[] rgpszLines;
		delete[] rgnLines;
		rgnLines = NULL;
		rgpszLines = NULL;
		nLines = 0;
	}
	void AddLine(int nLine, const char * str)
	{
		int * rgnTemp = new int [1 + nLines];
		char ** rgpszTemp = new char * [1 + nLines];
		int i;
		bool fRepeat = false;
		for (i = 0; i < nLines; i++)
		{
			if (rgnLines[i] == nLine)
				fRepeat = true;
			rgnTemp[i] = rgnLines[i];
			rgpszTemp[i] = rgpszLines[i];
		}
		delete[] rgnLines;
		delete[] rgpszLines;
		rgnLines = rgnTemp;
		rgpszLines = rgpszTemp;
		if (!fRepeat)
		{
			rgnLines[nLines] = nLine;
			int cch = 1 + strlen(str);
			rgpszLines[nLines] = new char [cch];
			strcpy_s(rgpszLines[nLines], cch, str);
			nLines++;
		}
	}
};

// Test if our text line appears in a specified revision of the file:
bool TextIsInByRevision(int nRev, const char * pszSource, int nLine, int nOldRevision, const char * pszSubString = NULL, OlderVersionData * pOld = NULL)
{
	if (nOldRevision)
	{
		// Get descriptions of all differences between this revision and nOldRevision:
		sprintf_s(rgchCommand, cchGeneral, "p4 diff2 \"%s\"#%d \"%s\"#%d >%s", rgchDepotFileName, nRev, rgchDepotFileName, nOldRevision, pszTempFile);
	}
	else
	{
		// Get descriptions of all differences between this revision and the one we have:
		sprintf_s(rgchCommand, cchGeneral, "p4 diff -f \"%s\"#%d >%s", rgchDepotFileName, nRev, pszTempFile);
	}
	system(rgchCommand);

	// Analyze descriptions:
	FILE * file;
	if (0 != fopen_s(&file, pszTempFile, "rt"))
	{
		printf("Cannot open temporary file (1) %s\n", pszTempFile);
		return false;
	}
	char rgchDiffText[1024];
	while (fgets(rgchDiffText, 1024, file))
	{
		crop(rgchDiffText);
		// Change descriptors typically look like this example:
		// 2,4c6,10  - Lines 2 thru 4 are changed to new lines 6 thru 10
		// There are variations, such as 'a' for addition instead of 'c' for change,
		// and it is possible to have fewer numbers.
		int n1, n2, n3, n4;
		char c1, c2, c3;
		int nParams = sscanf_s(rgchDiffText, "%d%c%d%c%d%c%d", &n1, &c1, sizeof(char), &n2, &c2, sizeof(char), &n3, &c3, sizeof(char), &n4);
		if (nParams >= 3)
		{
			// We have a change descriptor:
			int nStartOldLine = -1;
			int nEndOldLine = -1;
			int nStartNewLine = -1;
			int nEndNewLine = -1;
			bool fChange = false;

			// See which lines are invloved (only if adding or changing):
			if (nParams == 3 && (c1 == 'a' || c1 == 'c'))
			{
				nStartOldLine = nEndOldLine = n1;
				nStartNewLine = nEndNewLine = n2;
				if (c1 == 'c')
					fChange = true;
			}
			else if (nParams == 5 && (c2 == 'a' || c2 == 'c'))
			{
				nStartOldLine = n1;
				nEndOldLine = n2;
				nStartNewLine = nEndNewLine = n3;
				if (c2 == 'c')
					fChange = true;
			}
			else if (nParams == 5 && (c1 == 'a' || c1 == 'c'))
			{
				nStartOldLine = nEndOldLine = n1;
				nStartNewLine = n2;
				nEndNewLine = n3;
				if (c1 == 'c')
					fChange = true;
			}
			else if (nParams == 7 && (c2 == 'a' || c2 == 'c'))
			{
				nStartOldLine = n1;
				nEndOldLine = n2;
				nStartNewLine = n3;
				nEndNewLine = n4;
				if (c2 == 'c')
					fChange = true;
			}
			// See if these lines include ours:
			if (nStartNewLine <= nLine && nEndNewLine >= nLine)
			{
				int nLineCount;
				// Get past the deleted lines in this description:
				if (fChange)
				{
					nLineCount = nStartOldLine;
					while (nLineCount <= nEndOldLine)
					{
						fgets(rgchDiffText, 1024, file);
						// Assuming that we will discover our text in this change description,
						// Record any line numbers where the substring also appears:
						if (pszSubString && pOld)
						{
							if (strstr(rgchDiffText, pszSubString))
							{
								crop(rgchDiffText);
								int nEndPosTest = strlen(rgchDiffText) - 3;
								char ch = rgchDiffText[nEndPosTest];
								if (strcmp(&rgchDiffText[nEndPosTest], "---") == 0)
									rgchDiffText[nEndPosTest] = 0;
								pOld->AddLine(nLineCount, &rgchDiffText[2]);
								rgchDiffText[nEndPosTest] = ch;
							}
						}
						nLineCount++;
					}
					// Deleted lines description ends with a "---" on its own line. Sometimes this appears
					// at the end of the previous line. (This may bwe a bug in the P4 diff program.)
					// First see if the "---" is at the end of the previous line:
					crop(rgchDiffText);
					if (strcmp(&rgchDiffText[strlen(rgchDiffText) - 3], "---") != 0)
					{
						fgets(rgchDiffText, 1024, file);
						crop(rgchDiffText);
						if (strcmp(rgchDiffText, "---") != 0)
							printf(" Possible error analyzing #%d ", nRev);
					}
				}

				// Move to the required line number. We will also see if there are any changes
				// indicating our line was removed, because a substantial re-ordering of a file
				// can confuse the diff engine into thinking a line was removed from one part and
				// at the same time added in another:
				nLineCount = nStartNewLine;
				bool fRemoved = false;
				while (nLineCount <= nLine)
				{
					fgets(rgchDiffText, 1024, file);
					crop(rgchDiffText);
					if (rgchDiffText[0] == '<' && strcmp(&rgchDiffText[2], pszSource) == 0)
						fRemoved = true;
					nLineCount++;
				}
				// See if this line starts with a '>' and then contains our text:
				crop(rgchDiffText);
				if (rgchDiffText[0] == '>' && strcmp(&rgchDiffText[2], pszSource) == 0)
				{
					// We've found where our text is described as 'inserted later than this revision'.
					// Continue the test to see if our line was removed from somewhere else:
					while (fgets(rgchDiffText, 1024, file))
					{
						crop(rgchDiffText);
						if (rgchDiffText[0] == '<' && strcmp(&rgchDiffText[2], pszSource) == 0)
						{
							fRemoved = true;
							break;
						}
					}
					fclose(file);
					file = NULL;
					DeleteFile(pszTempFile);

					// If we detected that the line was also removed, then it can't have been new in
					// this revision:
					if (fRemoved)
						return true;

					// We found the line was inserted later than this revision, so it isn't in this one:
					return false;
				}
			}
			if (nEndNewLine > nLine)
			{
				// We've already gone past our line:
				break;
			}
		} // End if we found a line descriptor
	} // Next line
	fclose(file);
	file = NULL;
	DeleteFile(pszTempFile);

	// We didn't find a descriptor saying our text was inserted later, so it must be in now:
	return true;
}

// Recursive function to perform binary-chop search of file revisions:
int SearchRevisions(int nLo, int nHi, const char * pszSource, int nLine, bool fDirectionDown, int nOldRevision, const char * pszSubString, OlderVersionData * pOld)
{
	if (nHi < nLo)
		return nLo; // Not sure if this is correct!

	if (nHi == nLo)
	{
		printf(" #%d;", nLo);
		fflush(stdout);

		// (near) base case:
		pOld->Clear();
		if (TextIsInByRevision(nLo, pszSource, nLine, nOldRevision, pszSubString, pOld))
			return nLo;

		nHi++;
		pOld->Clear();
		if (TextIsInByRevision(nHi, pszSource, nLine, nOldRevision, pszSubString, pOld))
			return nHi;
	}

	// We're only interested in older versions if this is the last search, so clear previous data:
	pOld->Clear();

	int nMid = (nLo + nHi) / 2;

	printf(" #%d;", nMid);
	fflush(stdout);

	if (TextIsInByRevision(nMid, pszSource, nLine, nOldRevision, pszSubString, pOld))
		return SearchRevisions(nLo, nMid-1, pszSource, nLine, true, nOldRevision, pszSubString, pOld);
	else
		return SearchRevisions(nMid+1, nHi, pszSource, nLine, true, nOldRevision, pszSubString, pOld);
}

// Perform a recursive, binary search of past revisions to find when rgchSouceLine was inserted:
int FindAndReportEarliest(int nHighestRevision, const char * rgchSouceLine, int nLineNumber, int nOldRevision, const char * pszSubString, const char * pszRecursionIndex)
{
	printf("Searching");
	OlderVersionData Old;
	int nDebutRevision = SearchRevisions(1, nHighestRevision, rgchSouceLine, nLineNumber, false, nOldRevision, pszSubString, &Old);
	bool fThereAtStart = false;
	if (nDebutRevision == ErrRevMax)
	{
		printf("\nYou've added or altered this line, and you haven't checked it in, yet!\n");
		return ErrHalt;
	}
	if (nDebutRevision == ErrFlow)
	{
		printf(" The diff files are confusing, but the first appearrance of the text is somewhere here.\n");
		return ErrHalt;
	}
	if (nDebutRevision == 1)
	{
		// We couldn't find the insertion point, so it must have been there from the start:
		fThereAtStart = true;
	}

	// We now have a revision number when our text appeared.
	// Create file containing changlist description for our revision number:
	sprintf_s(rgchCommand, cchGeneral, "p4 fstat -s \"%s\"#%d >%s", pszClientFileName, nDebutRevision, pszTempFile);
	system(rgchCommand);

	// Parse the temp file for the changelist number:
	FILE * file;
	if (0 != fopen_s(&file, pszTempFile, "rt"))
	{
		printf("Cannot open temporary file (3) %s\n", pszTempFile);
		return ErrTempFile;
	}
	int nChangeNumber = -1;
	char *next_token;
	while (nChangeNumber == -1 && fgets(rgchParseLine, 1024, file))
	{
		crop(rgchParseLine);
		char * pszToken = strtok_s(rgchParseLine, ". \t", &next_token);
		if (strcmp(pszToken, "headChange") == 0)
			nChangeNumber = atoi(strtok_s(NULL, " \t\r\n", &next_token));
	}
	fclose(file);
	file = NULL;
	DeleteFile(pszTempFile);

	// Describe the changelist for our nChangeNumber:
	sprintf_s(rgchCommand, cchGeneral, "p4 describe -s %d >%s", nChangeNumber, pszTempFile);
	system(rgchCommand);

	// Retrieve the first line of this description:
	if (0 != fopen_s(&file, pszTempFile, "rt"))
	if (!file)
	{
		printf("Cannot open temporary file (4) %s\n", pszTempFile);
		return ErrTempFile;
	}
	if (fgets(rgchParseLine, 1024, file))
	{
		printf("\n");
		if (fThereAtStart)
			printf("Line was present from the first check-in.\n");
		else
		{
			if (Old.nLines == 0)
				printf("Line first appeared (in its entirety) in revision #%d.\n", nDebutRevision);
			else if (nOldRevision)
				printf("Line was previously altered in revision #%d.\n", nDebutRevision);
			else
				printf("Line was last altered in revision #%d.\n", nDebutRevision);
		}
		printf("%s\n", rgchParseLine);
	}
	fclose(file);
	file = NULL;
	DeleteFile(pszTempFile);

	// Now hunt for the occurrence of the substring in older revisions at or near the same line:
	if (Old.nLines > 0)
		printf("Searching for selected text \"%s\" in older versions (depth first):\n", pszSubString);
	int i;
	for (i = 0; i < Old.nLines; i++)
	{
		const int cch = 16;
		char buf[cch];
		sprintf_s(buf, cch, "%s%d.", pszRecursionIndex, i + 1);
		printf("Branch %s - ", buf);
		printf("Text may previously have existed in line %d: \"%s\".\n", Old.rgnLines[i], Old.rgpszLines[i]);
		FindAndReportEarliest(nDebutRevision - 1, Old.rgpszLines[i], Old.rgnLines[i], nDebutRevision - 1, pszSubString, buf);
	}
	return ErrHalt;
}


int main(int argc, char* argv[])
{
	// Sort out file name and line number from command line arguments:
	if (argc < 3)
	{
		printf("\nUsage: P4WhoDunnit <file-name> <line-number> [substring]\n\n");
		printf("P4WhoDunnit is a tool for finding out who wrote a particular piece of\n");
		printf("Perforce-controlled source code. It is designed to be set up as an\n");
		printf("External Tool in Microsoft Visual Studio. To do this, configure it\n");
		printf("with Arguments: $(ItemPath) $(CurLine) $(CurText), and Use Output Window.\n");
		return 0;
	}
	pszClientFileName = argv[1];
	int nLineNumber = atoi(argv[2]);

	// Read the relevant line from the file:
	FILE * file;
	if (0 != fopen_s(&file, pszClientFileName, "rt"))
	if (!file)
	{
		printf("ERROR: Cannot open file %s.\n", pszClientFileName);
		return 0;
	}
	char rgchSouceLine[cchGeneral];
	int n;
	for (n=0; n<nLineNumber; n++)
		fgets(rgchSouceLine, cchGeneral, file);
	fclose(file);
	file = NULL;
	DeleteFile(pszTempFile);
	crop(rgchSouceLine);

	// Create file containing depot file name and our file's revision number:
	sprintf_s(rgchCommand, cchGeneral, "p4 fstat -s \"%s\" >%s", pszClientFileName, pszTempFile);
	system(rgchCommand);

	// Parse the temp file for depot file name and our file's revision number:
	if (0 != fopen_s(&file, pszTempFile, "rt"))
	{
		printf("Cannot open temporary file (2) %s\n", pszTempFile);
		return 0;
	}
	nHeadRevision = -1;
	char *next_token;
	while ((rgchDepotFileName[0] == 0 || nHeadRevision == -1) && fgets(rgchParseLine, 1024, file))
	{
		crop(rgchParseLine);
		char * pszToken = strtok_s(rgchParseLine, ". \t", &next_token);
		if (strcmp(pszToken, "depotFile") == 0)
			strcpy_s(rgchDepotFileName, cchGeneral, next_token);
		else if (strcmp(pszToken, "haveRev") == 0)
			nHeadRevision = atoi(strtok_s(NULL, " \t\r\n", &next_token));
	}
	fclose(file);
	file = NULL;
	DeleteFile(pszTempFile);

	if (rgchDepotFileName[0] == 0 || nHeadRevision == -1)
	{
		printf("This file is not in the Depot.\n");
		return 0;
	}

	char *pszSubString = NULL;
	if (argc > 3)
		pszSubString = argv[3];
	printf("Searching file \"%s\" for line %d: \"%s\".\n", pszClientFileName, nLineNumber, rgchSouceLine);
	FindAndReportEarliest(nHeadRevision, rgchSouceLine, nLineNumber, 0, pszSubString, "");

	printf("Finished.\n");

	return 0;
}
