// SilFont2Iscii.cpp : Defines the entry point for the DLL application.
//

#include "stdafx.h"
#include "SilFont2Iscii.h"
#include <stdio.h>
#include <search.h>
#include <stdlib.h>

SILFONT2ISCII_API int SilIscii2FontOpenMapTable(LPCSTR lpszMapFilePath, LPCSTR lpszMapFileName, Iscii2FontInstanceData** pInstanceData)
{
	*pInstanceData = new Iscii2FontInstanceData;

	(*pInstanceData)->table_size = get_fnt_spec(
		lpszMapFilePath,
		lpszMapFileName,
		(*pInstanceData)->roman_fnt_nm,
		(*pInstanceData)->indian_fnt_nm,
		(*pInstanceData)->table,
		(*pInstanceData)->punc_lst);

	if( (*pInstanceData)->table_size == -1 )
		return /* NameNotFound = */ -7;
	else
		return 0;
}

SILFONT2ISCII_API int SilIscii2FontCloseMapTable(LPVOID instanceData)
{
	delete instanceData;
	return 0;
}

#include <malloc.h> // for alloca

SILFONT2ISCII_API int SilIscii2FontDoConversion(LPVOID instanceData, LPCSTR szInput, int nInputLen, LPSTR szOutput, int* pnOutputLen)
{
	Iscii2FontInstanceData* pInstanceData = (Iscii2FontInstanceData*)instanceData;

	int head_flag,html_flag,body_flag,font_flag,form_flag;
	head_flag = html_flag = body_flag = font_flag = form_flag = 0;

	char* output = (char*)alloca(100000);
	string2glyphs(
		pInstanceData->roman_fnt_nm,
		pInstanceData->indian_fnt_nm,
		pInstanceData->table,
		&pInstanceData->table_size,
		szInput,
		output,
		pInstanceData->punc_lst,
		&html_flag,&head_flag,&body_flag,&font_flag,&form_flag);

	// printf("%s",output);
	strncpy(szOutput,output,*pnOutputLen);
	*pnOutputLen = __min((int)strlen(output), *pnOutputLen);

/*
	if(font_flag)
		printf("</FONT>");
*/

	return 0;
}
