/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2000-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrFontInst.cpp
Responsibility: Alan Ward
Last reviewed: Not yet.

Description:
	Font installer for Graphite.
----------------------------------------------------------------------------------------------*/

#include "stdafx.h"
//:End Ignore

// font information
char * g_pszFontFile = NULL;
char * g_pszFontName = NULL;
char * g_pszFamilyName = NULL;
enum eStyle {eNoStyle, eRegular, eBold, eItalic, eBolditalic};
eStyle g_eStyle = eNoStyle; // style on command line or from font

// command line switches
bool g_fInstall = true; // install or uninstall font?
bool g_fGraphiteOnly = false; // only install as Graphite font, not Windows font
bool g_fSilent = false; // output msgs? - overridden on bad switch
bool g_fList = false;	// list registered fonts - overrides all other switches
bool g_fSilTest = true; // debug switch - disable Silf table test

// string constants
const char * g_pszGraphiteFontKey = "SOFTWARE\\SIL\\GraphiteFonts";
const char * g_pszRegularValue = "regular";
const char * g_pszBoldValue = "bold";
const char * g_pszItalicValue = "italic";
const char * g_pszBoldItalicValue = "bolditalic";

// registry information
const char * g_pszW98FontKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Fonts";
const char * g_pszWNTFontKey = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Fonts";
const char * g_pszTTLabel = " (TrueType)";
const char * g_pFontKey = NULL;

typedef unsigned char BYTE;

// wrapper for printf that supports silent mode
int Printf(const char * format, ...)
{
	va_list marker;

	if (g_fSilent)
		return 0;

	va_start(marker, format);
	return vprintf(format, marker);
}

// wrapper function for malloc to handle out of memory error
void * MyMalloc(size_t n)
{
	void * p = malloc(n);
	if (p)
	{
		return p;
	}
	else
	{
		Printf("Memory allocation failure.\n");
		abort();
	}
}

// Print syntax on screen
void PrintUsage(void)
{
	Printf("usage: GrFontInst [/l] | [[/u] [/g] [/s] [/r | /b | /i | /c] <font file name>]\n");
	Printf("l-list  u-uninstall  g-only install to Graphite key  s-silent mode\n");
	Printf("r-regular  b-bold  i-italic  c-bolditalic  (normally determined from font)\n");
}

// Print copyright notice on screen
void PrintCopyright(void)
{
	Printf("Graphite Font Installer, version 0.96\n");
	Printf("Copyright © 2002-2003, SIL International.  All rights reserved.\n");
}

// set second arg to full path name of first arg. print error msg on failure
bool SetFullFileName(const char * pFileName, char *& pFullFileName)
{
	//this allocates the memory with malloc and fills in the absolute path
	pFullFileName = _fullpath(NULL, pFileName, _MAX_PATH);
	if (pFullFileName)
		return true;
	else
	{
		Printf("File name is invalid: %s\n", pFileName);
		return false;
	}
}

// convert style enumeration to string
const char * GetStyleName(eStyle eStyle)
{
	const char * pName = NULL;

	switch (eStyle)
	{
	case eRegular		:	pName = g_pszRegularValue; break;
	case eBold			:	pName = g_pszBoldValue; break;
	case eItalic		:	pName = g_pszItalicValue; break;
	case eBolditalic	:	pName = g_pszBoldItalicValue; break;
	default				:	break; // do nothing
	}

	return pName;

}

// append TTF extension to name passed as arg for label to use in Fonts registery key
char * GetFontNameTT(char * pszFontName)
{
	int nFontNameSize = strlen(pszFontName) + strlen(g_pszTTLabel);
	char * pFontNameTT = (char *)MyMalloc(nFontNameSize + 1 * sizeof(char));
	strcpy(pFontNameTT, pszFontName);
	strcat(pFontNameTT, g_pszTTLabel);
	return pFontNameTT;
}

// extract from pFontFile: the font's name, family, and style
bool SetFontNamesAndStyle(const char * pFontFile, char *& pFontName, char *& pFamilyName,
						  eStyle & eRegStyle)
{	/*** Get the full font name from the TTF file ***/
	FILE * pFont;
	pFont = fopen(pFontFile, "rb");
	if (!pFont)
	{
		Printf("Invalid file name: %s\n", pFontFile);
		return false;
	}

	//TTF header
	long lOffset, lSize;
	TtfUtil::GetHeaderInfo(lOffset, lSize);
	BYTE * pHdr;
	pHdr = (BYTE *)MyMalloc(lSize * sizeof(BYTE));
	fseek(pFont, lOffset, SEEK_SET);
	fread(pHdr, lSize, 1, pFont);
	if (!TtfUtil::CheckHeader(pHdr))
	{
		Printf("File has an invalid TTF header: %s\n", pFontFile);
		free(pHdr);
		return false;
	}

	//TTF table directory
	TtfUtil::GetTableDirInfo(pHdr, lOffset, lSize);
	BYTE * pDir;
	pDir = (BYTE *)MyMalloc(lSize * sizeof(BYTE));
	fseek(pFont, lOffset, SEEK_SET);
	fread(pDir, lSize, 1, pFont);

	// verify TTF Silf table exists
	if (g_fSilTest)
	{
		if (!TtfUtil::GetTableInfo(ktiSilf, pHdr, pDir, lOffset, lSize))
		{
			Printf("File is NOT a Graphite font: %s\n", pFontFile);
			free(pHdr); free(pDir);
			return false;
		}
	}

	//TTF name table
	TtfUtil::GetTableInfo(ktiName, pHdr, pDir, lOffset, lSize);
	BYTE * pNameTbl;
	pNameTbl = (BYTE *)MyMalloc(lSize * sizeof(BYTE));
	fseek(pFont, lOffset, SEEK_SET);
	fread(pNameTbl, lSize, 1, pFont);
	if (!TtfUtil::CheckTable(ktiName, pNameTbl, lSize))
	{
		Printf("File has an invalid name table: %s\n", pFontFile);
		free(pHdr); free(pDir);	free(pNameTbl);
		return false;
	}

	// get full font name in name table (Unicode encoded)
	if (!TtfUtil::Get31EngFullFontInfo(pNameTbl, lOffset, lSize)) //lOffset is within pNameTbl
	{
		if (!TtfUtil::Get30EngFullFontInfo(pNameTbl, lOffset, lSize))
		{ // try Symbol encoded instead
			Printf("Could not find full font name in font file: %s\n", pFontFile);
			free(pHdr); free(pDir); free(pNameTbl);
			return false;
		}
	}
	pFontName = (char *)MyMalloc((lSize / 2 + 1) * sizeof(char));
	int i;
	for (i = 0; i < lSize / 2; i++)
	{ // convert to 8 bit string
		pFontName[i] = pNameTbl[lOffset + 2 * i + 1]; //string is double byte with MSB first
	}
	pFontName[lSize / 2] = '\0';

	// get font family name in name table (Unicode encoded)
	if (!TtfUtil::Get31EngFamilyInfo(pNameTbl, lOffset, lSize)) //lOffset is within pNameTbl
	{
		if (!TtfUtil::Get30EngFamilyInfo(pNameTbl, lOffset, lSize))
		{ // try Symbol encoded instead
			Printf("Could not find font family name in font file: %s\n", pFontFile);
			free(pHdr); free(pDir); free(pNameTbl);
			return false;
		}
	}
	pFamilyName = (char *)MyMalloc((lSize / 2 + 1) * sizeof(char));
	for (i = 0; i < lSize / 2; i++)
	{ // convert to 8 bit string
		pFamilyName[i] = pNameTbl[lOffset + 2 * i + 1]; //string is double byte with MSB first
	}
	pFamilyName[lSize / 2] = '\0';

	free(pNameTbl); pNameTbl = NULL;

	/*** Get the font style from the TTF file ***/
	// TTF OS/2 table
	TtfUtil::GetTableInfo(ktiOs2, pHdr, pDir, lOffset, lSize);
	BYTE * pOs2Tbl;
	pOs2Tbl = (BYTE *)MyMalloc(lSize * sizeof(BYTE));
	fseek(pFont, lOffset, SEEK_SET);
	fread(pOs2Tbl, lSize, 1, pFont);
	if (!TtfUtil::CheckTable(ktiOs2, pOs2Tbl, lSize))
	{
		Printf("File has an invalid OS/2 table: %s\n", pFontFile);
		free(pHdr); free(pDir); free(pOs2Tbl);
		return false;
	}

	// determine the font style
	bool fFontBold, fFontItalic;
	TtfUtil::FontOs2Style(pOs2Tbl, fFontBold, fFontItalic);
	eStyle eFontStyle;
	eFontStyle = fFontBold ? (fFontItalic ? eBolditalic : eBold) : (fFontItalic ? eItalic : eRegular);

	// if no style specified on the command line, set it
	if (eRegStyle == eNoStyle)
	{
		eRegStyle = eFontStyle;
	}
	else
	{ // compare the style on the command line to the style in the font and warn if mismatch
		if (eRegStyle != eFontStyle)
		{
			Printf("Style on command line does NOT match style in font's OS/2 table.\n");
			Printf("Style on command line will be used.\n");
		}
	}

	free(pOs2Tbl); pOs2Tbl = NULL;

	/*** Clean up TTF handling ***/
	fclose(pFont); pFont = NULL;
	free(pHdr); pHdr = NULL;
	free(pDir); pDir = NULL;

	return true;
}

// set a string named value in the registry
bool SetRegistryString(const char * pKey, const char * pName, const char * pValue)
{
	HKEY hKey;
	DWORD dwDisp;

	// this works even if key already exists by just opening the key
	if (::RegCreateKeyEx(HKEY_LOCAL_MACHINE, pKey, NULL, NULL, REG_OPTION_NON_VOLATILE,
		KEY_WRITE, NULL, &hKey, &dwDisp) != ERROR_SUCCESS)
	{
		Printf("Could not open or create registry key: %s\n", pKey);
		return false;
	}
	if (::RegSetValueEx(hKey, pName, NULL, REG_SZ, (BYTE *)pValue, strlen(pValue)) !=
		ERROR_SUCCESS)
	{
		::RegCloseKey(hKey);
		Printf("Could not set registry value for: %s\n", pName);
		return false;
	}
	::RegCloseKey(hKey);
	return true;
}

// clear a named value from the registry
bool ClearRegistryString(const char *pKey, const char * pName)
{
	HKEY hKey;

	if (::RegOpenKeyEx(HKEY_LOCAL_MACHINE, pKey, NULL, KEY_WRITE, &hKey) != ERROR_SUCCESS)
	{
		Printf("Could not open registry key: %s\n", pKey);
		return false;
	}
	if (::RegDeleteValue(hKey, pName) != ERROR_SUCCESS)
	{
		::RegCloseKey(hKey);
		Printf("Could not delete registry value for: %s\n", pName);
		return false;
	}
	::RegCloseKey(hKey);
	return true;
}

// either set or clear the style named value under the font name key
enum eModRegistry {eSetRegistry, eClearRegistry};
bool RegistryStyle(eModRegistry eReg, const char * pFontName, eStyle eStyle, const char * pFontFile)
{
	// create key based on font name
	int nKeySz = strlen(g_pszGraphiteFontKey) + strlen(pFontName) + 2; // 2 - for backslash and null
	char * pKey = (char *)MyMalloc(nKeySz * sizeof(char));
	strcpy(pKey, g_pszGraphiteFontKey);
	strcat(pKey, "\\");
	strcat(pKey, pFontName);

	// convert style enumeration to string for name to put in key
	const char * pName = GetStyleName(eStyle);
	if (!pName)
	{
		Printf("Style for registry is invalid.\n");
		return false;
	}

	// set or clear registry entry
	bool fRtn;
	switch (eReg)
	{
	case eSetRegistry	:	fRtn = SetRegistryString(pKey, pName, pFontFile); break;
	case eClearRegistry	:	fRtn = ClearRegistryString(pKey, pName); break;
	default				:	free(pKey); Printf("Invalid registry modification.\n"); return false;
	}
	free(pKey);
	return fRtn;
}

// list the Graphite fonts in the registry by font name, font style, and font file
bool ListRegistryValues(const char * pGrFontKey)
{
	HKEY hGrFontKey;

	// open Graphite Fonts key
	if (::RegOpenKeyEx(HKEY_LOCAL_MACHINE, pGrFontKey, NULL, KEY_READ, &hGrFontKey) != ERROR_SUCCESS)
	{
		Printf("No fonts registered with Graphite.\n");
		return true;
	}

	// query Graphite Fonts key to prepare for enumerating family subkeys
	DWORD dwFamilyKeyCt, dwFamilyKeyMaxSz, dwNameCt, dwNameMaxSz, dwValueMaxSz;
	if (::RegQueryInfoKey(hGrFontKey, NULL, NULL, NULL, &dwFamilyKeyCt, &dwFamilyKeyMaxSz, NULL, &dwNameCt,
		&dwNameMaxSz, &dwValueMaxSz, NULL, NULL) != ERROR_SUCCESS)
	{
		Printf("Could not query registry key: %s\n", pGrFontKey);
		return false;
	}

	unsigned int i;
	for (i = 0; i < dwFamilyKeyCt; i++)
	{ // enumerate family subkeys under Graphite Fonts key
		char * pFamilyKeyName = (char *)MyMalloc((dwFamilyKeyMaxSz + 1) * sizeof(char)); // 1 - null
		DWORD dwFamilyKeySz = dwFamilyKeyMaxSz + 1;
		long l = ::RegEnumKeyEx(hGrFontKey, i, pFamilyKeyName, &dwFamilyKeySz, NULL, NULL, NULL, NULL);
		if (l != ERROR_SUCCESS && l != ERROR_NO_MORE_ITEMS)
		{
			Printf("Could not enumerate registry key: %s\n.", pGrFontKey);
			break;
		}

		// open family subkey
		HKEY hFamilyKey;
		if (::RegOpenKeyEx(hGrFontKey, pFamilyKeyName, NULL, KEY_READ, &hFamilyKey) != ERROR_SUCCESS)
		{
			Printf("Could not open registry key: %s\n.", pFamilyKeyName);
			continue;
		}

		// query family subkey to prepare for enumerating style named values
		DWORD dwSubKeyCt, dwSubKeyMaxSz, dwStyleNameCt, dwStyleNameMaxSz, dwFontFileMaxSz;
		if (::RegQueryInfoKey(hFamilyKey, NULL, NULL, NULL, &dwSubKeyCt, &dwSubKeyMaxSz, NULL, &dwStyleNameCt,
			&dwStyleNameMaxSz, &dwFontFileMaxSz, NULL, NULL) != ERROR_SUCCESS)
		{
			Printf("Could not query registry key: %s\n", pFamilyKeyName);
			continue;
		}

		char * pStyleName = (char *)MyMalloc((dwStyleNameMaxSz + 1)* sizeof(char));
		BYTE * pFontFile = (BYTE *)MyMalloc((dwFontFileMaxSz + 1)* sizeof(BYTE));
		unsigned int j;
		for (j = 0; j < dwStyleNameCt; j++)
		{ // enumerate style named values and font file names
			DWORD dwType;
			DWORD dwStyleNameSz = dwStyleNameMaxSz + 1;
			DWORD dwFileNameSz = dwFontFileMaxSz + 1;
			if (::RegEnumValue(hFamilyKey, j, pStyleName, &dwStyleNameSz, NULL, &dwType,
				pFontFile, &dwFileNameSz) != ERROR_SUCCESS)
			{
				Printf("Could not enumerate registry value: %s\n", pFamilyKeyName);
				break;
			}
			// print font family, style, and file
			Printf("%s %s: %s\n", pFamilyKeyName, pStyleName, (char *)pFontFile);
		}

		free(pStyleName); pStyleName = NULL;
		free(pFontFile); pFontFile = NULL;

		::RegCloseKey(hFamilyKey);
		free(pFamilyKeyName); pFamilyKeyName = NULL;
	}

	::RegCloseKey(hGrFontKey);

	return true;
}

// delete empty family subkeys and also Graphite Fonts key if it is empty
bool DeleteEmptyRegistryKeys(const char *pGrFontKey)
{
	HKEY hGrFontKey;

	// open Graphite Fonts key
	if (::RegOpenKeyEx(HKEY_LOCAL_MACHINE, pGrFontKey, NULL, KEY_READ, &hGrFontKey) != ERROR_SUCCESS)
	{
		return true;
	}

	DWORD dwFamilyKeyCt, dwFamilyKeyMaxSz, dwNameCt, dwNameMaxSz, dwValueMaxSz;
	if (::RegQueryInfoKey(hGrFontKey, NULL, NULL, NULL, &dwFamilyKeyCt, &dwFamilyKeyMaxSz, NULL, &dwNameCt,
		&dwNameMaxSz, &dwValueMaxSz, NULL, NULL) != ERROR_SUCCESS)
	{
		Printf("Could not query registry key: %s\n", pGrFontKey);
		return false;
	}

	// store array of Font Name subkeys
	// not allowed to delete family key while GrFont key is being enumerated according to MSDN Lib
	char ** pFamilyKeyNames = (char **)MyMalloc(dwFamilyKeyCt * sizeof(char *));
	unsigned int i;
	for (i = 0; i < dwFamilyKeyCt; i++)
	{
		char * pFamilyKeyName = (char *)MyMalloc((dwFamilyKeyMaxSz + 1) * sizeof(char)); // 1 - null
		DWORD dwSubKeySz = dwFamilyKeyMaxSz + 1;
		long l = ::RegEnumKeyEx(hGrFontKey, i, pFamilyKeyName, &dwSubKeySz, NULL, NULL, NULL, NULL);
		if (l != ERROR_SUCCESS && l != ERROR_NO_MORE_ITEMS)
		{
			Printf("Could not enumerate registry key: %s\n.", pGrFontKey);
			goto lCleanUp;
		}
		pFamilyKeyNames[i] = pFamilyKeyName;
	}

	// delete Font Name subkeys if they are empty
	unsigned int j;
	for (j = 0; j < dwFamilyKeyCt; j++)
	{
		HKEY hFamilyKey;
		if (::RegOpenKeyEx(hGrFontKey, pFamilyKeyNames[j], NULL, KEY_READ, &hFamilyKey) != ERROR_SUCCESS)
		{
			Printf("Could not open registry key: %s\n.", pFamilyKeyNames[j]);
			continue;
		}

		DWORD dwSubKeyCt, dwSubKeyMaxSz, dwStyleNameCt, dwStyleNameMaxSz, dwValueMaxSz;
		if (::RegQueryInfoKey(hFamilyKey, NULL, NULL, NULL, &dwSubKeyCt, &dwSubKeyMaxSz, NULL, &dwStyleNameCt,
			&dwStyleNameMaxSz, &dwValueMaxSz, NULL, NULL) != ERROR_SUCCESS)
		{
			Printf("Could not query registry key: %s\n", pFamilyKeyNames[j]);
			continue;
		}
		::RegCloseKey(hFamilyKey);
		if (dwStyleNameCt == 0 && dwSubKeyCt == 0)
		{
			::RegDeleteKey(hGrFontKey, pFamilyKeyNames[j]);
		}
	}

	// delete Graphite Font key if it is empty now
	::RegQueryInfoKey(hGrFontKey, NULL, NULL, NULL, &dwFamilyKeyCt, &dwFamilyKeyMaxSz, NULL, &dwNameCt,
		&dwNameMaxSz, &dwValueMaxSz, NULL, NULL);
	::RegCloseKey(hGrFontKey);
	if (dwFamilyKeyCt == 0 && dwNameCt == 0)
	{
		::RegDeleteKey(HKEY_LOCAL_MACHINE, pGrFontKey);
	}

lCleanUp:
	unsigned int k;
	for (k = 0; k < i; k++)
	{
		free(pFamilyKeyNames[k]);
	}
	free(pFamilyKeyNames); pFamilyKeyNames = NULL;

	return true;
}

int main(int argc, char* argv[])
{
	/*** Find OS version and set registry font key ***/
	OSVERSIONINFO osvi;
	osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);
	if (!GetVersionEx(&osvi))
	{
		Printf("Unable to find OS platform. Assuming Win NT.\n");
		g_pFontKey = g_pszWNTFontKey;
	}
	else
	{
		if (osvi.dwPlatformId == VER_PLATFORM_WIN32_WINDOWS)
			g_pFontKey = g_pszW98FontKey;
		else if (osvi.dwPlatformId == VER_PLATFORM_WIN32_NT)
			g_pFontKey = g_pszWNTFontKey;
		else
		{
			Printf("OS platform is not recognized. Assuming Win NT.\n");
			g_pFontKey = g_pszWNTFontKey;
		}
	}

	/*** Parse the command line ***/
	bool fCmdLineOK = true;
	for (int nArg = 1; nArg < argc && fCmdLineOK; nArg++) // argv[0] is program's on file name
	{
		if (argv[nArg][0] == '/' || argv[nArg][0] == '-')
		{ // handle command line switches
			switch (argv[nArg][1])
			{
			case 'U'	:	// fall thru
			case 'u'	:	g_fInstall = false; break;
			case 'G'	:	// fall thru
			case 'g'	:	g_fGraphiteOnly = true; break;
			case 'S'	:	// fall thru
			case 's'	:	g_fSilent = true; break;
			case 'L'	:	// fall thru
			case 'l'	:	g_fList = true; break;
			case 'R'	:	// fall thru
			case 'r'	:	g_eStyle = eRegular; break;
			case 'B'	:	// fall thru
			case 'b'	:	g_eStyle = eBold; break;
			case 'I'	:	// fall thru
			case 'i'	:	g_eStyle = eItalic; break;
			case 'C'	:	// fall thru
			case 'c'	:	g_eStyle = eBolditalic; break;
			// undocumented switch to bypass Silf table test for debugging
			case 'o'	:	g_fSilTest = false; break;
			case 'H'	:	// fall thru
			case 'h'	:	// fall thru
			default		:	fCmdLineOK = false; g_fSilent = false; break; // illegal switch
			}
		}
		else
		{ // handle font file name
			SetFullFileName(argv[nArg], g_pszFontFile);
		}
	}
	if (fCmdLineOK && argc > 1)
	{
		PrintCopyright();
	}
	else
	{ // bad command line or help requested or no command line args
		PrintCopyright();
		PrintUsage();
		goto lEnd;
	}

	/*** Enumerate the Graphite fonts in the registry ***/
	if (g_fList)
	{
		ListRegistryValues(g_pszGraphiteFontKey);
		goto lEnd;
	}

	/*** get the font name and style from the file ***/
	if (!SetFontNamesAndStyle(g_pszFontFile, g_pszFontName, g_pszFamilyName, g_eStyle))
	{
		goto lEnd;
	}

	/*** Install the font ***/
	if (g_fInstall)
	{
		/*** Create the key and values in the GraphiteFonts registry key ***/
		if (!RegistryStyle(eSetRegistry, g_pszFamilyName, g_eStyle, g_pszFontFile))
		{
			Printf("Error installing font into Graphite Fonts registry.\n");
			goto lEnd;
		}
		Printf("Font %s was successfully registered (as %s) with Graphite.\n",
			g_pszFamilyName, GetStyleName(g_eStyle));

		if (g_fGraphiteOnly)
		{ // skip Windows font install
			Printf("Font %s was NOT installed into Windows.\n", g_pszFontName);
			goto lEnd;
		}

		// Add the font to Windows list of installed fonts.
		// It appears CreateScalableFontResource  isn't needed anymore.
		if (::AddFontResource(g_pszFontFile) == 1)
		{
			// SendNotifyMessage posts the message to other threads and returns immediately.
			// SendMessage can hang if there are top-level windows created by other
			// threads that can't process the message immediately.
			// SendMessageTimeout is another alternate to SendMessage.
			::SendNotifyMessage(HWND_BROADCAST, WM_FONTCHANGE, 0, 0);
		}
		else
		{
			Printf("Error adding font to Windows.\n");
			Printf("Please install it with the Fonts control panel manually.\n");
			if (!g_fSilent)
				goto lEnd;
		}

		// add the font to the registry so it will still be installed after a reboot
		char * pFontNameTT = GetFontNameTT(g_pszFontName);
		if (!pFontNameTT)
			goto lEnd;
		if (!SetRegistryString(g_pFontKey, pFontNameTT, g_pszFontFile))
		{
			Printf("Error installing font into Windows Fonts registry.\n");
			free(pFontNameTT);
			goto lEnd;
		}
		free(pFontNameTT);

		Printf("Font %s was successfully installed into Windows.\n", g_pszFontName);
	}
	/*** Uninstall the font ***/
	else
	{ /*** Remove the value from the GraphiteFonts key ***/
		if (!RegistryStyle(eClearRegistry, g_pszFamilyName, g_eStyle, g_pszFontFile))
		{
			Printf("Error deleting font from Graphite Fonts registry.\n");
		}
		else
		{
			Printf("Font %s was successfully unregistered (as %s) with Graphite.\n",
				g_pszFamilyName, GetStyleName(g_eStyle));
		}

		// delete empty font name keys if a key is empty.
		// also delete empty Graphite Fonts key if it is empty
		DeleteEmptyRegistryKeys(g_pszGraphiteFontKey);

		if (g_fGraphiteOnly)
		{ // skip Windows font uninstall
			Printf("Font %s was NOT uninstalled from Windows.\n", g_pszFontName);
			goto lEnd;
		}

		// Remove the font from the Windows list of installed fonts.
		if (::RemoveFontResource(g_pszFontFile))
		{
			::SendNotifyMessage(HWND_BROADCAST, WM_FONTCHANGE, 0, 0);
		}
		else
		{
			Printf("Error removing font from Windows.\n");
			Printf("Please remove it with the Fonts control panel manually.\n");
			if (!g_fSilent)
				goto lEnd;
		}

		// delete registry key for this font
		char * pFontNameTT = GetFontNameTT(g_pszFontName);
		if (!pFontNameTT)
			goto lEnd;
		if (!ClearRegistryString(g_pFontKey, pFontNameTT))
		{
			Printf("Error deleting font from Windows Fonts registry.\n");
			free(pFontNameTT);
			goto lEnd;
		}
		free(pFontNameTT);

		Printf("Font %s was successfully uninstalled from Windows.\n", g_pszFontName);
		Printf("(The font uninstall will complete on rebooting.)\n");
	}

lEnd:
	if (g_pszFontFile)
		free(g_pszFontFile);
	if (g_pszFontName)
		free(g_pszFontName);
	if (g_pszFamilyName)
		free(g_pszFamilyName);
	return 0;
}
