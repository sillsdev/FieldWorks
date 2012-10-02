#include <tchar.h>

extern _TCHAR * my_strdup(const _TCHAR * pszOriginal);
extern _TCHAR * new_vsprintf(const _TCHAR * pszFormat, const va_list arglist);
extern _TCHAR * new_sprintf(const _TCHAR * pszFormat, ...);
extern void new_vsprintf_concat(_TCHAR *& rpszMain, int ctInsertNewline,
								const _TCHAR * pszAddendumFmt, const va_list arglist);
extern void new_sprintf_concat(_TCHAR *& rpszMain, int ctInsertNewline,
							   const _TCHAR * pszAddendumFmt, ...);
extern _TCHAR * new_ind_sprintf(int nIndent, const _TCHAR * pszFormat, ...);
