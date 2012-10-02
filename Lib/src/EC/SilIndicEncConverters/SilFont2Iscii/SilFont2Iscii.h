// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the SILFONT2ISCII_EXPORTS
// symbol defined on the command line. this symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// SILFONT2ISCII_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef SILFONT2ISCII_EXPORTS
#define SILFONT2ISCII_API __declspec(dllexport)
#else
#define SILFONT2ISCII_API __declspec(dllimport)
#endif

typedef struct
{
	char    table[1300000];
	int     table_size;
} Font2IsciiInstanceData;

typedef struct
{
	char    table[1000000];
	int     table_size;
	char    roman_fnt_nm[100];
	char    indian_fnt_nm[100];
	char    punc_lst[100];
} Iscii2FontInstanceData;

#ifdef __cplusplus
extern "C"
{
#endif

SILFONT2ISCII_API int SilFont2IsciiOpenMapTable(LPCSTR lpszMapFileName, Font2IsciiInstanceData** pInstanceData);
SILFONT2ISCII_API int SilFont2IsciiCloseMapTable(LPVOID instanceData);
SILFONT2ISCII_API int SilFont2IsciiDoConversion(LPVOID instanceData, LPCSTR szInput, int nInputLen, LPSTR szOutput, int* nOutputLen);

SILFONT2ISCII_API int SilIscii2FontOpenMapTable(LPCSTR lpszMapFilePath, LPCSTR lpszMapFileName, Iscii2FontInstanceData** pInstanceData);
SILFONT2ISCII_API int SilIscii2FontCloseMapTable(LPVOID instanceData);
SILFONT2ISCII_API int SilIscii2FontDoConversion(LPVOID instanceData, LPCSTR szInput, int nInputLen, LPSTR szOutput, int* nOutputLen);

// internal defines of "C" functions
extern void string2glyphs(char *roman_fnt_nm,char *indian_fnt_nm,char *table,int *sizeof_table,const char *input_str,char *output_str,char *punc_lst,int *html_flag_ptr,int *head_flag_ptr,int *body_flag_ptr,int *font_flag_ptr,int *form_flag_ptr);
extern int get_fnt_spec(const char *filepath,const char *filename,char *roman_fnt_nm,char *indian_fnt_nm,char *table,char *punc_lst);

#ifdef __cplusplus
}
#endif
