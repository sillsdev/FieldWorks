/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: UniConvert.h
Responsibility: Darrell Zook
Last reviewed:

	Header file for UniConvert.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef _UNICONVERT_H_
#define _UNICONVERT_H_


class UniConvert
{
public:
	UniConvert(char * pszCmdLine);
	~UniConvert();
	DoConversion(HINSTANCE hInstance);

protected:
	ParseCommandLine(char * pszCmdLine);

	char m_szFiles[NUM_COMBOS][MAX_PATH];
	bool m_fSilent;
};

#endif // !_UNICONVERT_H_