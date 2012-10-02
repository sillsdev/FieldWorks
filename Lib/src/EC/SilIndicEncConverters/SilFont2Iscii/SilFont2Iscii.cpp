// SilFont2Iscii.cpp : Defines the entry point for the DLL application.
//

#include "stdafx.h"
#include "SilFont2Iscii.h"
#include <stdio.h>
#include <search.h>
#include <stdlib.h>

BOOL APIENTRY DllMain( HANDLE hModule,
					   DWORD  ul_reason_for_call,
					   LPVOID lpReserved
					 )
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

#define MAXSZ 1000
#define MAXBUFF 40

void wrd_glyphs2iscii(char *table,int *sizeof_table, const char *input_str, char* szOutput, int* pnOutputLen)
{
	size_t i;
	int found;
	char test_wrd[MAXSZ],curr_wrd[MAXSZ];
	char *tag;
	size_t lenCurOutput = 0;
	strcpy(curr_wrd,input_str);
	while(curr_wrd[0] != '\0')
	{
		found = 0;
		for(i = strlen(curr_wrd); i>0 && !found; i--)
		{
			strncpy(test_wrd,curr_wrd,i);
			test_wrd[i] = '\0';
			tag = (char *)bsearch(test_wrd,table,*sizeof_table,2*MAXBUFF,(int (__cdecl *)(const void *, const void *))strcmp);
			if(tag != NULL)
			{
				sprintf(test_wrd, "%s", tag+MAXBUFF);
				size_t len = strlen(test_wrd);
				if( (lenCurOutput + len) < (size_t)(*pnOutputLen) )
				{
					strcat(szOutput,test_wrd);
					lenCurOutput += len;
				}
				else
				{
					len = __min(len, (*pnOutputLen - lenCurOutput) );
					strncat(szOutput, test_wrd, len);
					lenCurOutput += len;
					break;
				}

				strcpy(curr_wrd,curr_wrd+i);
				found = 1;
			}
		}

		if(!found)
		{
			szOutput[lenCurOutput++] = curr_wrd[0];
			strcpy(curr_wrd,curr_wrd+1);
		}
	}

	*pnOutputLen = (int)lenCurOutput;
}

int my_getword(char s[],int lim, const char* szInput, int nInputLen)
{
	int c;
	int i = 0;
	for(;
			(i < lim - 1)
		&&  (i < nInputLen)
		&&  ((c = szInput[i]) != EOF)
		&&  (!isspace(c))
		&&  (c != '<')
		&&  (c != '>');
		++i)
			s[i] = c;

	if(c != EOF )
	{
		s[i++] = c;
	}

	s[i] = 0;   // null terminate
	return i;
}

void fgetline(char* s_ptr, FILE* fp)
{
/*   Collects the string from the FILE fp, into the string s_ptr */
	char c;
	while((c = getc(fp)) != '\n' && !feof(fp)) *s_ptr++ = c;
	*s_ptr = '\0';
}

int load_map_table(const char* filename, const char* delimit, char* table)
{
  FILE *fp;
  int i=0;
  size_t len;
  char inpt[2*MAXBUFF];
  char tmp[2*MAXBUFF];

  if ((fp = fopen(filename,"r")) == NULL)
  {
	perror(filename);
	return -1;
  }

  tmp[0] = '\0';
  while(!feof(fp))
  {
	fgetline(inpt,fp);
	if(inpt[0] != '\0')
	{
	strcpy(tmp,strtok(inpt,delimit));
	len = strlen(tmp);
	table[2*MAXBUFF*i] = '\0';
	strncat(table+2*MAXBUFF*i,tmp,len);
	table[2*MAXBUFF*i+len] = '\0';
	strcpy(tmp,strtok(NULL,delimit));
	len = strlen(tmp);
	table[MAXBUFF+2*MAXBUFF*i] = '\0';
	strncat(table+MAXBUFF+2*MAXBUFF*i,tmp,len);
	table[MAXBUFF+2*MAXBUFF*i+len] = '\0';
	i++;
   }
  }
  return i;
}

SILFONT2ISCII_API int SilFont2IsciiOpenMapTable(LPCSTR lpszMapFileName, Font2IsciiInstanceData** pInstanceData)
{
	*pInstanceData = new Font2IsciiInstanceData;
	(*pInstanceData)->table_size = load_map_table(lpszMapFileName, " ", (*pInstanceData)->table);
	if( (*pInstanceData)->table_size == -1 )
		return /* NameNotFound = */ -7;
	else
		return 0;
}

SILFONT2ISCII_API int SilFont2IsciiCloseMapTable(LPVOID instanceData)
{
	delete instanceData;
	return 0;
}

SILFONT2ISCII_API int SilFont2IsciiDoConversion(LPVOID instanceData, LPCSTR szInput, int nInputLen, LPSTR szOutput, int* pnOutputLen)
{
	Font2IsciiInstanceData* pInstanceData = (Font2IsciiInstanceData*)instanceData;

	wrd_glyphs2iscii(pInstanceData->table,&pInstanceData->table_size,szInput, szOutput, pnOutputLen);

	return 0;
}
